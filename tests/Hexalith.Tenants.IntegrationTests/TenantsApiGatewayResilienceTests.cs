extern alias TenantsApi;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
using System.Security.Cryptography;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Tenants.IntegrationTests.Fixtures;
#endif

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

using Polly.Timeout;
using Shouldly;

using ForwardingHandler = TenantsApi::Hexalith.Tenants.Api.Services.InboundBearerForwardingHandler;
using ReadinessCheck = TenantsApi::Hexalith.Tenants.Api.Services.EventStoreGatewayReadinessHealthCheck;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Exercises the production API gateway policy without a network listener or backend.</summary>
public sealed class TenantsApiGatewayResilienceTests
{
    /// <summary>Checks the real typed name, handler order and client isolation rather than duplicating registration.</summary>
    [Fact]
    public void ProductionGatewayHasOneScopedStandardPipelineAndPreservesOtherClients()
    {
        var capture = new ApiGatewayResilienceCapture((_, _) => Task.FromResult(Success()));
        using var factory = Factory(capture);
        _ = Gateway(factory, capture);
        IHttpClientFactory clients = factory.Services.GetRequiredService<IHttpClientFactory>();
        using HttpClient gatewayHttp = clients.CreateClient(capture.GatewayClientName);
        gatewayHttp.BaseAddress.ShouldBe(new Uri("http://localhost:4367"));
        using HttpClient readiness = clients.CreateClient(ReadinessCheck.ClientName);
        using HttpClient ordinary = clients.CreateClient("api-budget-unrelated-control");

        capture.GatewayClientName.ShouldNotBeNullOrWhiteSpace();
        Type[] handlers = capture.HandlerTypes[capture.GatewayClientName];
        handlers.Count(type => type == typeof(ResilienceHandler)).ShouldBe(1);
        int resilience = Array.FindIndex(handlers, type => type == typeof(ResilienceHandler));
        int forwarding = Array.FindIndex(handlers, type => type == typeof(ForwardingHandler));
        resilience.ShouldBe(0);
        forwarding.ShouldBe(handlers.Length - 2);
        handlers[^1].Name.ShouldBe("DaprServiceInvocationHandler");
        int discovery = Array.FindIndex(handlers, type => type.FullName!.Contains("ServiceDiscovery", StringComparison.Ordinal));
        discovery.ShouldBeGreaterThan(resilience);
        discovery.ShouldBeLessThan(forwarding);
        foreach (string name in new[] { ReadinessCheck.ClientName, "api-budget-unrelated-control" })
        {
            capture.HandlerTypes[name].Count(type => type == typeof(ResilienceHandler)).ShouldBe(1);
            capture.HandlerTypes[name].ShouldNotContain(typeof(ForwardingHandler));
        }

        IOptionsMonitor<HttpStandardResilienceOptions> options = factory.Services
            .GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>();
        // The framework names a standard pipeline from its actual client builder name.
        HttpStandardResilienceOptions gateway = options.Get(capture.GatewayClientName + "-standard");
        HttpStandardResilienceOptions inherited = options.Get("-standard");
        var defaults = new HttpStandardResilienceOptions();
        gateway.AttemptTimeout.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        gateway.TotalRequestTimeout.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        gateway.CircuitBreaker.SamplingDuration.ShouldBe(TimeSpan.FromSeconds(60));
        gateway.Retry.MaxRetryAttempts.ShouldBe(defaults.Retry.MaxRetryAttempts);
        gateway.Retry.Delay.ShouldBe(defaults.Retry.Delay);
        gateway.Retry.BackoffType.ShouldBe(defaults.Retry.BackoffType);
        gateway.Retry.UseJitter.ShouldBe(defaults.Retry.UseJitter);
        gateway.CircuitBreaker.FailureRatio.ShouldBe(defaults.CircuitBreaker.FailureRatio);
        gateway.CircuitBreaker.MinimumThroughput.ShouldBe(defaults.CircuitBreaker.MinimumThroughput);
        gateway.CircuitBreaker.BreakDuration.ShouldBe(defaults.CircuitBreaker.BreakDuration);
        gateway.RateLimiter.DefaultRateLimiterOptions.PermitLimit.ShouldBe(defaults.RateLimiter.DefaultRateLimiterOptions.PermitLimit);
        gateway.RateLimiter.DefaultRateLimiterOptions.QueueLimit.ShouldBe(defaults.RateLimiter.DefaultRateLimiterOptions.QueueLimit);
        inherited.AttemptTimeout.Timeout.ShouldBe(defaults.AttemptTimeout.Timeout);
        inherited.TotalRequestTimeout.Timeout.ShouldBe(defaults.TotalRequestTimeout.Timeout);
        inherited.CircuitBreaker.SamplingDuration.ShouldBe(defaults.CircuitBreaker.SamplingDuration);
    }

    /// <summary>Proves the real policy permits an actual send beyond the former ten-second attempt.</summary>
    [Fact(Timeout = 25000)]
    public async Task HeldGatewayQuerySurvivesTheOriginalAttemptBoundary()
    {
        int attempts = 0;
        var capture = new ApiGatewayResilienceCapture(async (_, token) =>
        {
            Interlocked.Increment(ref attempts);
            await Task.Delay(TimeSpan.FromSeconds(11), token);
            return Success();
        });
        using var factory = Factory(capture);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));

        EventStoreQueryResult result = await Gateway(factory, capture).SubmitQueryAsync(Query(), cancellationToken: deadline.Token);

        result.CorrelationId.ShouldBe("api-budget-result");
        attempts.ShouldBe(1);
    }

    /// <summary>Preserves standard transient retries and forwarding on the actual production client.</summary>
    [Fact(Timeout = 15000)]
    public async Task TransientGatewayFailureRetriesWithOwnedHeadersAndSucceeds()
    {
        int attempts = 0;
        ApiGatewayResilienceCapture capture = null!;
        capture = new ApiGatewayResilienceCapture((request, _) =>
        {
            request.RequestUri.ShouldBe(new Uri("http://localhost:4367/api/v1/queries"));
            request.Headers.Authorization!.Scheme.ShouldBe("Bearer");
            string.Equals(request.Headers.Authorization.Parameter, capture.BearerToken, StringComparison.Ordinal)
                .ShouldBeTrue("the production forwarding handler must retain the controlled caller bearer");
            request.Headers.GetValues("dapr-app-id").ShouldBe(["eventstore"]);
            request.Headers.GetValues("dapr-api-token").SequenceEqual([capture.SidecarToken], StringComparer.Ordinal)
                .ShouldBeTrue("the final Dapr handler must own the controlled sidecar credential");
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
            request.Headers.GetValues(RouteCatalogInvocationHandler.RootHeader).ShouldHaveSingleItem();
            request.Headers.GetValues(RouteCatalogInvocationHandler.EpochHeader).ShouldBe(["1"]);
#if HEXALITH_TECHNICAL_HTTP_CATALOG_BINDING
            request.Headers.GetValues(RouteCatalogInvocationHandler.BindingHeader).ShouldHaveSingleItem();
#endif
#endif
            return Task.FromResult(Interlocked.Increment(ref attempts) == 1
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : Success());
        });
        using var factory = Factory(capture);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(10));

        EventStoreQueryResult result = await Gateway(factory, capture).SubmitQueryAsync(Query(), cancellationToken: deadline.Token);

        result.CorrelationId.ShouldBe("api-budget-result");
        attempts.ShouldBe(2);
    }

    /// <summary>Caller cancellation reaches the active transport without replaying the canceled request.</summary>
    [Fact(Timeout = 10000)]
    public async Task CallerCancellationCancelsTheActualTransport()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int attempts = 0;
        var capture = new ApiGatewayResilienceCapture(async (_, token) =>
        {
            Interlocked.Increment(ref attempts);
            started.TrySetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            catch (OperationCanceledException) { canceled.TrySetResult(); throw; }
            return Success();
        });
        using var factory = Factory(capture);
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        caller.CancelAfter(TimeSpan.FromSeconds(5));
        Task<EventStoreQueryResult> request = Gateway(factory, capture).SubmitQueryAsync(Query(), cancellationToken: caller.Token);
        try
        {
            await started.Task.WaitAsync(caller.Token);
            caller.Cancel();
            await Should.ThrowAsync<OperationCanceledException>(() => request);
            await canceled.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            attempts.ShouldBe(1);
        }
        finally { await caller.CancelAsync(); }
    }

    /// <summary>The unchanged thirty-second total bound still cancels the actual transport.</summary>
    [Fact(Timeout = 40000)]
    public async Task TotalRequestBudgetCancelsTheActualTransport()
    {
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int attempts = 0;
        var capture = new ApiGatewayResilienceCapture(async (_, token) =>
        {
            Interlocked.Increment(ref attempts);
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            catch (OperationCanceledException) { canceled.TrySetResult(); throw; }
            return Success();
        });
        using var factory = Factory(capture);
        IEventStoreGatewayClient gateway = Gateway(factory, capture);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(35));

        long startedAt = Stopwatch.GetTimestamp();
        Task<EventStoreQueryResult> request = gateway.SubmitQueryAsync(Query(), cancellationToken: deadline.Token);
        await Should.ThrowAsync<TimeoutRejectedException>(() => request);
        Stopwatch.GetElapsedTime(startedAt).ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(29),
            "the actual transport must survive near the configured thirty-second boundary");

        await canceled.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        attempts.ShouldBe(1);
    }

    private static WebApplicationFactory<TenantsApi::Program> Factory(ApiGatewayResilienceCapture capture)
        => new WebApplicationFactory<TenantsApi::Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("DAPR_HTTP_ENDPOINT", "http://localhost:4367");
            builder.UseSetting("DAPR_API_TOKEN", capture.SidecarToken);
            builder.UseSetting("EventStore:Authentication:SigningKey", capture.SigningKey);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EventStore:Authentication:SigningKey"] = capture.SigningKey,
                    ["DAPR_HTTP_ENDPOINT"] = "http://localhost:4367",
                    ["DAPR_API_TOKEN"] = capture.SidecarToken,
                });
            });
            builder.ConfigureServices((context, services) =>
            {
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
                // Only the committed authority and terminal are isolated. Keep the actual SDK
                // primary guard, typed client, resilience, forwarding and Dapr handlers in place.
                services.AddEventStoreRouteCatalog(context.Configuration);
                var owner = TenantOwnerCatalogTestAuthority.CreateSnapshot("tenants-api");
#if HEXALITH_TECHNICAL_HTTP_CATALOG_BINDING
                string digest = Convert.ToHexStringLower(SHA256.HashData("isolated-api-budget-transport"u8));
                var activation = new RouteCatalogActivation("isolated-api-budget", owner.GetCanonicalBytes(), owner.RootDigest, null,
                    [new("tenants-api", "tenants-api", digest, digest, "isolated-test"),
                     new("tenants", "tenants", digest, digest, "isolated-test"),
                     new("eventstore", "eventstore", digest, digest, "isolated-test")], "isolated-api-budget", "isolated-test")
                {
                    TechnicalHttpReceivers =
                    [new("eventstore", RouteCatalogTechnicalHttpPolicy.FenceVersion,
                        [new("/api/v1/queries", ["POST"], "isolated-query-action", digest, digest,
                            ["EventStoreHuman"], digest, digest)])
                        { Invocations = [new("tenants-api", "/api/v1/queries", ["POST"])] }],
                };
                var snapshot = new CommittedRouteCatalogSnapshot(activation, owner.Epoch, "tenants-api",
                    RouteCatalogStateCoordinates.Capture(context.Configuration, new RouteCatalogHostOptions()));
#else
                var snapshot = new CommittedRouteCatalogSnapshot(owner.GetCanonicalBytes(), owner.RootDigest, owner.Epoch,
                    [new("eventstore", "api/v1/queries", "eventstore:query", "gateway")], localAppId: "tenants-api");
#endif
                services.AddSingleton(TenantOwnerCatalogTestAuthority.Create(snapshot));
                services.Configure<RouteCatalogHostOptions>(options =>
                {
#if HEXALITH_TECHNICAL_HTTP_CATALOG_BINDING
                    options.RequireTechnicalHttpBinding = true;
#endif
                    options.InvocationTransportFactory = _ => capture.CreateIndependentTransport();
                });
#endif
                // Register last so the SDK's outer filter wraps this terminal in its primary guard.
                services.AddSingleton<IHttpMessageHandlerBuilderFilter>(capture);
            });
        });

    private static IEventStoreGatewayClient Gateway(WebApplicationFactory<TenantsApi::Program> factory, ApiGatewayResilienceCapture capture)
    {
        factory.Services.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext();
        factory.Services.GetRequiredService<IHttpContextAccessor>().HttpContext!.Request.Headers.Authorization = "Bearer " + capture.BearerToken;
        return factory.Services.GetRequiredService<IEventStoreGatewayClient>();
    }

    private static SubmitQueryRequest Query() => new("system", "tenants", "index", "list-tenants", "tenant-index");

    private static HttpResponseMessage Success() => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new SubmitQueryResponse("api-budget-result", JsonSerializer.SerializeToElement(new { items = Array.Empty<object>() }))),
    };
}
