using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Dapr.Client;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.ServiceDefaults;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Tenants.Bootstrap;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Identity;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Bootstrap;

/// <summary>Exercises bootstrap policy isolation through real ServiceDefaults and HttpClientFactory composition.</summary>
public sealed class TenantBootstrapCommandTransportTests
{
    /// <summary>Retains ordinary resilience and the independent readiness timeout in either registration order.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GeneralClientsRetainRetriesAndAttemptDeadlines(bool bootstrapFirst)
    {
        HostApplicationBuilder builder = CreateBuilder(bootstrapFirst);
        int requests = 0;
        bool hold = false;
        builder.Services.AddHttpClient(string.Empty).ConfigurePrimaryHttpMessageHandler(() =>
            new BootstrapCommandHttpMessageHandler(async (_, token) =>
            {
                Interlocked.Increment(ref requests);
                if (hold) await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }));
        await using ServiceProvider provider = builder.Services.BuildServiceProvider();
        IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
        using HttpClient command = factory.CreateClient(TenantBootstrapServiceCollectionExtensions.CommandClientName);
        using HttpClient general = factory.CreateClient();
        using HttpClient credentials = factory.CreateClient(TenantBootstrapServiceCollectionExtensions.CredentialClientName);
        command.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        general.Timeout.ShouldBe(Timeout.InfiniteTimeSpan);
        credentials.Timeout.ShouldBe(Timeout.InfiniteTimeSpan);

        using HttpResponseMessage response = await general.GetAsync("http://127.0.0.1:3500/retry-control", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        requests.ShouldBe(2);

        requests = 0;
        hold = true;
        await Should.ThrowAsync<Polly.Timeout.TimeoutRejectedException>(() =>
            general.GetAsync("http://127.0.0.1:3500/deadline-control", TestContext.Current.CancellationToken));
        requests.ShouldBe(2);
    }

    /// <summary>Observes actual delegated sends, response receipts and scope disposal through the hosted bootstrap.</summary>
    [Theory]
    [InlineData(false, "accepted", true)]
    [InlineData(true, "accepted", true)]
    [InlineData(false, "unavailable", true)]
    [InlineData(true, "unavailable", true)]
    [InlineData(false, "denied", true)]
    [InlineData(true, "denied", true)]
    [InlineData(false, "exception", true)]
    [InlineData(true, "exception", true)]
    [InlineData(false, "deadline", true)]
    [InlineData(true, "deadline", true)]
    [InlineData(false, "shutdown", true)]
    [InlineData(true, "shutdown", true)]
    [InlineData(false, "accepted", false)]
    [InlineData(true, "accepted", false)]
    [InlineData(false, "unavailable", false)]
    [InlineData(true, "unavailable", false)]
    [InlineData(false, "exception", false)]
    [InlineData(true, "exception", false)]
    [InlineData(false, "shutdown", false)]
    [InlineData(true, "shutdown", false)]
    public async Task HostedCommandUsesOneAttemptAndRecordsOnlyTheActualResponse(bool bootstrapFirst, string scenario, bool boundWorkload)
    {
        using RSA proofKey = RSA.Create(2048);
        string directory = Directory.CreateTempSubdirectory("tenant-command-transport-").FullName;
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        using var started = new CancellationTokenSource();
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Returns(started.Token);
        lifetime.ApplicationStopping.Returns(stopping.Token);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var outcome = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        HttpRequestMessage? sent = null;
        bool sendCancelled = false;
        int sends = 0;
        HostApplicationBuilder builder = CreateBuilder(bootstrapFirst);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:Issuer"] = "bootstrap-origin",
            ["Authentication:JwtBearer:Audience"] = "bootstrap-reader",
            ["Authentication:JwtBearer:SigningKey"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            ["Authentication:JwtBearer:AllowedAlgorithms:0"] = "HS256",
            ["Authentication:JwtBearer:AllowInsecureSymmetricKey"] = "true",
            ["Authentication:JwtBearer:RequireHttpsMetadata"] = "false",
            ["Authentication:WorkloadIssuer:Workload"] = "tenants",
            ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(proofKey.ExportPkcs8PrivateKey()),
            ["Tenants:BootstrapBoundWorkloadEnabled"] = boundWorkload.ToString(),
            ["Tenants:BootstrapGlobalAdminUserId"] = "bootstrap-administrator",
            ["Tenants:BootstrapBoundWorkloadReceiptPath"] = Path.Combine(directory, "tenants.bootstrap.json"),
            ["EventStore:RouteCatalog:DevelopmentBootstrap:Directory"] = directory,
            ["EventStore:RouteCatalog:DevelopmentBootstrap:AttemptId"] = "factory-composition-attempt",
            ["EventStore:RouteCatalog:DevelopmentBootstrap:RootDigest"] = new string('a', 64),
        });
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            JwtBearerAuthenticationContract.Configure(options, builder.Configuration.GetSection("Authentication:JwtBearer").Get<JwtBearerAuthenticationOptions>()!));
        builder.Services.AddEventStoreWorkloadAssertionIssuer(builder.Configuration);
        builder.Services.Configure<TenantBootstrapOptions>(builder.Configuration.GetSection("Tenants"));
        builder.Services.AddSingleton<IHostApplicationLifetime>(lifetime);
        builder.Services.AddSingleton<ILogger<TenantBootstrapHostedService>>(new BootstrapCommandOutcomeLogger(outcome));
        builder.Services.AddScoped(_ => new BootstrapCommandScopeCompletion(completed));
        builder.Services.AddScoped(provider =>
        {
            _ = provider.GetRequiredService<BootstrapCommandScopeCompletion>();
            return new DaprClientBuilder().UseHttpEndpoint("http://127.0.0.1:3500").Build();
        });
        builder.Services.AddHttpClient(TenantBootstrapServiceCollectionExtensions.CommandClientName)
            .ConfigureHttpClient(client =>
            {
                client.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
                if (scenario == "deadline") client.Timeout = TimeSpan.FromMilliseconds(200);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new BootstrapCommandHttpMessageHandler(async (request, token) =>
            {
                Interlocked.Increment(ref sends);
                sent = request;
                request.Method.ShouldBe(HttpMethod.Post);
                request.RequestUri!.AbsoluteUri.ShouldStartWith("http://127.0.0.1:3500/v1.0/invoke/eventstore/method/");
                SubmitCommandRequest submitted;
                if (boundWorkload)
                {
                    request.RequestUri.AbsolutePath.ShouldEndWith("/api/v1/commands/bound/workload");
                    request.Headers.Authorization.ShouldBeNull();
                    request.Headers.GetValues(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName).ShouldHaveSingleItem().ShouldNotBeNullOrWhiteSpace();
                    request.Content!.Headers.ContentLength.ShouldNotBeNull().ShouldBeGreaterThan(0L);
                    byte[] bytes = await request.Content.ReadAsByteArrayAsync(token);
                    (await request.Content.ReadAsByteArrayAsync(token)).ShouldBe(bytes);
                    var bound = (await request.Content.ReadFromJsonAsync<BoundWorkloadCommandRequest>(token)).ShouldNotBeNull();
                    bound.PrincipalId.ShouldBe("bootstrap-administrator");
                    BoundWorkloadCommandProof.Verify(bound, request.Headers.GetValues(BoundWorkloadCommandProof.HeaderName).Single(),
                        "tenants", Convert.ToBase64String(proofKey.ExportSubjectPublicKeyInfo()), DateTimeOffset.UtcNow).ShouldBeTrue();
                    submitted = bound.Command;
                }
                else
                {
                    request.RequestUri.AbsolutePath.ShouldEndWith("/api/v1/commands");
                    request.Headers.Contains(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName).ShouldBeFalse();
                    request.Headers.Contains(BoundWorkloadCommandProof.HeaderName).ShouldBeFalse();
                    var authorization = request.Headers.Authorization.ShouldNotBeNull();
                    authorization.Scheme.ShouldBe("Bearer");
                    var validation = await new JsonWebTokenHandler().ValidateTokenAsync(authorization.Parameter.ShouldNotBeNull(),
                        new TokenValidationParameters
                        {
                            ValidIssuer = "bootstrap-origin",
                            ValidAudience = "bootstrap-reader",
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Authentication:JwtBearer:SigningKey"]!)),
                            ValidateIssuerSigningKey = true,
                            ValidateLifetime = true,
                        });
                    validation.IsValid.ShouldBeTrue();
                    validation.ClaimsIdentity.FindFirst("sub")!.Value.ShouldBe("bootstrap-administrator");
                    validation.ClaimsIdentity.FindFirst("global_admin")!.Value.ShouldBe("true");
                    submitted = (await request.Content!.ReadFromJsonAsync<SubmitCommandRequest>(token)).ShouldNotBeNull();
                }
                submitted.Tenant.ShouldBe(TenantIdentity.DefaultTenantId);
                submitted.Domain.ShouldBe(TenantIdentity.GlobalAdministratorsDomain);
                submitted.AggregateId.ShouldBe(TenantIdentity.GlobalAdministratorsAggregateId);
                submitted.CommandType.ShouldBe(nameof(BootstrapGlobalAdmin));
                submitted.Payload.Deserialize<BootstrapGlobalAdmin>(JsonSerializerOptions.Web)!.UserId.ShouldBe("bootstrap-administrator");
                entered.TrySetResult();
                if (scenario is "deadline" or "shutdown")
                {
                    try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
                    finally { sendCancelled = token.IsCancellationRequested; }
                }
                if (scenario == "exception") throw new HttpRequestException("Ambiguous synthetic command delivery.");
                // This exceeds the real general client's shortened one-second attempt limit.
                if (scenario == "accepted") await Task.Delay(TimeSpan.FromMilliseconds(1500), token);
                return new HttpResponseMessage(scenario switch
                {
                    "unavailable" => HttpStatusCode.ServiceUnavailable,
                    "denied" => HttpStatusCode.Forbidden,
                    _ => HttpStatusCode.Accepted,
                }) { Content = new StringContent("{}") };
            }));
        ServiceProvider? provider = null;
        bool bootstrapScheduled = false;
        Exception? testFailure = null;
        try
        {
            provider = builder.Services.BuildServiceProvider();
            var service = provider.GetServices<IHostedService>().OfType<TenantBootstrapHostedService>().Single();
            await service.StartAsync(TestContext.Current.CancellationToken);
            bootstrapScheduled = true;
            started.Cancel();
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            if (scenario == "shutdown") stopping.Cancel();
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            if (scenario != "shutdown")
            {
                int diagnostic = await outcome.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                diagnostic.ShouldBe(scenario switch { "accepted" => 2001, "unavailable" or "denied" => 2003, _ => 2002 });
            }
            else outcome.Task.IsCompleted.ShouldBeFalse();

            sends.ShouldBe(1);
            sendCancelled.ShouldBe(scenario is "deadline" or "shutdown");
            stopping.IsCancellationRequested.ShouldBe(scenario == "shutdown");
            if (boundWorkload)
            {
                var receipt = JsonSerializer.Deserialize<TenantBootstrapReceipt>(await File.ReadAllBytesAsync(
                    Path.Combine(directory, "tenants.bootstrap.json"), TestContext.Current.CancellationToken)).ShouldNotBeNull();
                receipt.Phase.ShouldBe(scenario switch
                {
                    "accepted" => "accepted",
                    "unavailable" or "denied" => "response-received",
                    _ => "attempted",
                });
                receipt.HttpStatus.ShouldBe(scenario switch
                {
                    "accepted" => 202,
                    "unavailable" => 503,
                    "denied" => 403,
                    _ => (int?)null,
                });
                receipt.PrincipalId.ShouldBe("bootstrap-administrator");
                receipt.MessageId.Length.ShouldBe(26);
                receipt.CorrelationId.Length.ShouldBe(26);
            }
            else File.Exists(Path.Combine(directory, "tenants.bootstrap.json")).ShouldBeFalse();
            await Should.ThrowAsync<ObjectDisposedException>(() => sent.ShouldNotBeNull().Content!.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }
        catch (Exception exception)
        {
            testFailure = exception;
            throw;
        }
        finally
        {
            try
            {
                stopping.Cancel();
                if (bootstrapScheduled) await completed.Task.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                if (provider is not null) await provider.DisposeAsync();
                Directory.Delete(directory, recursive: true);
            }
            // Preserve the original assertion/watchdog failure. If cancellation does not finish,
            // retain its dependencies and receipt directory rather than disposing them underneath it.
            catch (Exception) when (testFailure is not null) { }
        }
    }

    /// <summary>Cancels the actual factory transport when its caller cancels.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CallerCancellationReachesTheCommandTransportWithoutReplay(bool bootstrapFirst)
    {
        HostApplicationBuilder builder = CreateBuilder(bootstrapFirst);
        int sends = 0;
        bool cancelled = false;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        builder.Services.AddHttpClient(TenantBootstrapServiceCollectionExtensions.CommandClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new BootstrapCommandHttpMessageHandler(async (_, token) =>
            {
                Interlocked.Increment(ref sends);
                entered.TrySetResult();
                try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
                finally { cancelled = token.IsCancellationRequested; }
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            }));
        await using ServiceProvider provider = builder.Services.BuildServiceProvider();
        using HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(TenantBootstrapServiceCollectionExtensions.CommandClientName);
        client.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:3500/command");
        Task<HttpResponseMessage> send = client.SendAsync(request, caller.Token);
        Exception? testFailure = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            caller.Cancel();
            await Should.ThrowAsync<OperationCanceledException>(() => send);
            sends.ShouldBe(1);
            cancelled.ShouldBeTrue();
        }
        catch (Exception exception)
        {
            testFailure = exception;
            throw;
        }
        finally
        {
            caller.Cancel();
            try { await send; }
            catch (OperationCanceledException) when (caller.IsCancellationRequested) { }
            catch (Exception) when (testFailure is not null) { }
        }
    }

    /// <summary>Refuses command and credential redirects without replaying bodies or forwarding authority to another destination.</summary>
    [Theory]
    [InlineData(false, 307, false)]
    [InlineData(true, 307, false)]
    [InlineData(false, 308, false)]
    [InlineData(true, 308, false)]
    [InlineData(false, 307, true)]
    [InlineData(true, 307, true)]
    [InlineData(false, 308, true)]
    [InlineData(true, 308, true)]
    public async Task RedirectPoliciesDoNotReplayCommandsOrCredentials(bool bootstrapFirst, int status, bool crossOrigin)
    {
        var destinations = new ConcurrentQueue<(string Method, string Body, string Assertion, string Proof)>();
        var commands = new ConcurrentQueue<(string Body, string Assertion, string Proof)>();
        int originCalls = 0;
        async Task ObserveAsync(HttpContext context)
        {
            using var reader = new StreamReader(context.Request.Body);
            destinations.Enqueue((context.Request.Method, await reader.ReadToEndAsync(context.RequestAborted),
                context.Request.Headers[EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName].ToString(),
                context.Request.Headers[BoundWorkloadCommandProof.HeaderName].ToString()));
            context.Response.StatusCode = StatusCodes.Status200OK;
        }
        var destinationBuilder = WebApplication.CreateBuilder();
        destinationBuilder.Logging.ClearProviders();
        destinationBuilder.WebHost.UseUrls("http://127.0.0.1:0");
        await using WebApplication destination = destinationBuilder.Build();
        destination.MapPost("/destination", ObserveAsync);
        await destination.StartAsync(TestContext.Current.CancellationToken);
        string? redirect = null;
        var receiverBuilder = WebApplication.CreateBuilder();
        receiverBuilder.Logging.ClearProviders();
        receiverBuilder.WebHost.UseUrls("http://127.0.0.1:0");
        await using WebApplication receiver = receiverBuilder.Build();
        async Task RedirectAsync(HttpContext context)
        {
            Interlocked.Increment(ref originCalls);
            if (context.Request.Path == "/command")
            {
                using var reader = new StreamReader(context.Request.Body);
                commands.Enqueue((await reader.ReadToEndAsync(context.RequestAborted),
                    context.Request.Headers[EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName].ToString(),
                    context.Request.Headers[BoundWorkloadCommandProof.HeaderName].ToString()));
            }
            context.Response.StatusCode = status;
            context.Response.Headers.Location = redirect;
        }
        receiver.MapPost("/credential", RedirectAsync);
        receiver.MapPost("/command", RedirectAsync);
        receiver.MapPost("/destination", ObserveAsync);
        await receiver.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            string address = receiver.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            redirect = (crossOrigin
                ? destination.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()
                : address) + "/destination";
            HostApplicationBuilder builder = CreateBuilder(bootstrapFirst);
            await using ServiceProvider provider = builder.Services.BuildServiceProvider();
            IHttpClientFactory factory = provider.GetRequiredService<IHttpClientFactory>();
            using HttpClient credentials = factory.CreateClient(TenantBootstrapServiceCollectionExtensions.CredentialClientName);
            using HttpResponseMessage refused = await credentials.PostAsync(address + "/credential", new StringContent("synthetic-form"), TestContext.Current.CancellationToken);
            refused.StatusCode.ShouldBe((HttpStatusCode)status);
            destinations.ShouldBeEmpty();
            originCalls.ShouldBe(1);

            using RSA proofKey = RSA.Create(2048);
            var submitted = new SubmitCommandRequest("01ARZ3NDEKTSV4RRFFQ69G5FAV", TenantIdentity.DefaultTenantId,
                TenantIdentity.GlobalAdministratorsDomain, TenantIdentity.GlobalAdministratorsAggregateId, nameof(BootstrapGlobalAdmin),
                JsonSerializer.SerializeToElement(new BootstrapGlobalAdmin("bootstrap-administrator")), "01ARZ3NDEKTSV4RRFFQ69G5FAW");
            var bound = new BoundWorkloadCommandRequest(submitted, "bootstrap-administrator");
            string proof = BoundWorkloadCommandProof.Issue(bound, "tenants", Convert.ToBase64String(proofKey.ExportPkcs8PrivateKey()), DateTimeOffset.UtcNow);
            const string assertion = "synthetic-workload-assertion";
            using var request = new HttpRequestMessage(HttpMethod.Post, address + "/command") { Content = JsonContent.Create(bound) };
            await request.Content.LoadIntoBufferAsync(TestContext.Current.CancellationToken);
            string body = await request.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
            request.Headers.Add(BoundWorkloadCommandProof.HeaderName, proof);
            using HttpClient command = factory.CreateClient(TenantBootstrapServiceCollectionExtensions.CommandClientName);
            using HttpResponseMessage commandRefused = await command.SendAsync(request, TestContext.Current.CancellationToken);
            commandRefused.StatusCode.ShouldBe((HttpStatusCode)status);
            originCalls.ShouldBe(2);
            commands.ShouldHaveSingleItem().ShouldBe((body, assertion, proof));
            destinations.ShouldBeEmpty();

            using HttpClient general = factory.CreateClient();
            using HttpResponseMessage followed = await general.PostAsync(address + "/credential", new StringContent("synthetic-form"), TestContext.Current.CancellationToken);
            followed.StatusCode.ShouldBe(HttpStatusCode.OK);
            originCalls.ShouldBe(3);
            destinations.ShouldHaveSingleItem().ShouldBe(("POST", "synthetic-form", string.Empty, string.Empty));
        }
        finally
        {
            await receiver.StopAsync(CancellationToken.None);
            await destination.StopAsync(CancellationToken.None);
        }
    }

    private static HostApplicationBuilder CreateBuilder(bool bootstrapFirst)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true,
            EnvironmentName = Environments.Development,
        });
        if (bootstrapFirst) builder.Services.AddTenantBootstrap();
        builder.AddServiceDefaults();
        if (!bootstrapFirst) builder.Services.AddTenantBootstrap();
        builder.Logging.ClearProviders();
        builder.Services.ConfigureAll<HttpStandardResilienceOptions>(static options =>
        {
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(3);
            options.Retry.MaxRetryAttempts = 1;
            options.Retry.Delay = TimeSpan.FromMilliseconds(1);
            options.Retry.UseJitter = false;
        });
        return builder;
    }
}
