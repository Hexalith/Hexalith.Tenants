using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

using Dapr.Client;

#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Routing;
#endif
using Hexalith.EventStore.ServiceDefaults.Authentication;
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
using Hexalith.EventStore.ServiceDefaults.Routing;
#endif
using Hexalith.Tenants.Bootstrap;
using Hexalith.Tenants.Configuration;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Bootstrap;

/// <summary>Observes real hosted-service ordering, one-use receipts and the original shared deadline before any command effects.</summary>
public sealed class TenantBootstrapAppReadinessDispatchTests
{
    /// <summary>Both hosted branches refuse early effects, and a single successful health admission permits one command.</summary>
    [Theory]
    [InlineData(false, false, "delayed")]
    [InlineData(true, false, "delayed")]
    [InlineData(false, true, "delayed")]
    [InlineData(true, true, "delayed")]
    [InlineData(false, false, "foreign")]
    [InlineData(true, true, "foreign")]
    [InlineData(false, false, "denied")]
    [InlineData(true, true, "denied")]
    [InlineData(false, false, "metadata-cancel")]
    [InlineData(true, true, "metadata-cancel")]
    [InlineData(false, false, "health-cancel")]
    [InlineData(true, true, "health-cancel")]
    [InlineData(false, false, "metadata-deadline")]
    [InlineData(true, true, "metadata-deadline")]
    [InlineData(false, false, "health-deadline")]
    [InlineData(true, true, "health-deadline")]
    [InlineData(false, false, "cancel-after-health")]
    [InlineData(true, true, "cancel-after-health")]
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
    [InlineData(false, true, "catalog-metadata-deadline")]
    [InlineData(true, true, "catalog-health-deadline")]
    [InlineData(false, true, "catalog-cancel")]
#endif
    public async Task ReadinessControlsAllEffectsWithinOneOriginalBudget(bool bootstrapFirst, bool bound, string scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        using RSA proofKey = RSA.Create(2048);
        string directory = Directory.CreateTempSubdirectory("tenant-inbound-readiness-").FullName;
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        using var started = new CancellationTokenSource();
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var lifetime = Substitute.For<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Returns(started.Token);
        lifetime.ApplicationStopping.Returns(stopping.Token);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var catalogEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCatalog = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseHealth = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Action? expire = null;
        int deadlineTimers = 0;
        int probes = 0;
        int commands = 0;
        int assertions = 0;
        int metadataCalls = 0;
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_ => DateTimeOffset.UtcNow);
        var timer = Substitute.For<ITimer>();
        clock.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>()).Returns(call =>
        {
            Interlocked.Increment(ref deadlineTimers);
            call.ArgAt<TimeSpan>(2).ShouldBe(TimeSpan.FromSeconds(3));
            call.ArgAt<TimeSpan>(3).ShouldBe(Timeout.InfiniteTimeSpan);
            expire = () => call.ArgAt<TimerCallback>(0)(call.ArgAt<object?>(1));
            return timer;
        });
        HostApplicationBuilder builder = TenantBootstrapCommandTransportTests.CreateBuilder(bootstrapFirst);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:Issuer"] = "readiness-origin",
            ["Authentication:JwtBearer:Audience"] = "readiness-reader",
            ["Authentication:JwtBearer:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ["Authentication:JwtBearer:AllowedAlgorithms:0"] = "HS256",
            ["Authentication:JwtBearer:AllowInsecureSymmetricKey"] = "true",
            ["Authentication:JwtBearer:RequireHttpsMetadata"] = "false",
            ["Authentication:WorkloadIssuer:Workload"] = "tenants-isolated",
            ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(proofKey.ExportPkcs8PrivateKey()),
            ["Tenants:BootstrapGlobalAdminUserId"] = "readiness-administrator",
            ["Tenants:BootstrapBoundWorkloadEnabled"] = bound.ToString(),
            ["Tenants:BootstrapBoundWorkloadReceiptPath"] = Path.Combine(directory, "tenants.bootstrap.json"),
            ["EventStore:RouteCatalog:DevelopmentBootstrap:Directory"] = directory,
            ["EventStore:RouteCatalog:DevelopmentBootstrap:AttemptId"] = "inbound-readiness-attempt",
            ["EventStore:RouteCatalog:DevelopmentBootstrap:RootDigest"] = new string('a', 64),
        });
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            JwtBearerAuthenticationContract.Configure(options, builder.Configuration.GetSection("Authentication:JwtBearer").Get<JwtBearerAuthenticationOptions>()!));
        builder.Services.Configure<TenantBootstrapOptions>(builder.Configuration.GetSection("Tenants"));
        builder.Services.AddSingleton<IHostApplicationLifetime>(lifetime);
        builder.Services.AddScoped(_ => new BootstrapCommandScopeCompletion(completed));
        var issuer = Substitute.For<IWorkloadAssertionIssuer>();
        issuer.IssueAsync(Arg.Any<WorkloadAssertionRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Interlocked.Increment(ref assertions);
            return new ValueTask<string?>("controlled-workload-assertion");
        });
        builder.Services.AddSingleton(issuer);
        using DaprClient dapr = BootstrapAppReadinessFixture.CreateDapr();
        dapr.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns(async call =>
        {
            Interlocked.Increment(ref metadataCalls);
            if (scenario.Contains("metadata", StringComparison.Ordinal))
            {
                entered.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>());
            }
            return new DaprMetadata(scenario == "foreign" ? "foreign-app" : BootstrapAppReadinessFixture.AppId,
                [], new Dictionary<string, string>(), []);
        });
        builder.Services.AddScoped(provider =>
        {
            _ = provider.GetRequiredService<BootstrapCommandScopeCompletion>();
            return dapr;
        });
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
        if (scenario.StartsWith("catalog", StringComparison.Ordinal))
        {
            var catalog = Substitute.For<IRouteCatalogAuthority>();
            catalog.ReadbackAsync(Arg.Any<CancellationToken>()).Returns(async call =>
            {
                catalogEntered.TrySetResult();
                await releaseCatalog.Task.WaitAsync(call.Arg<CancellationToken>());
                return new RouteCatalogGenerationReadback(1, 1, new string('a', 64), new string('b', 64), new string('c', 64), "Development", [], true);
            });
            builder.Services.AddSingleton(catalog);
        }
#endif
        builder.Services.AddSingleton(_ => new TenantBootstrapAppReadiness(new BootstrapCommandHttpMessageHandler(async (request, token) =>
            {
                request.Method.ShouldBe(HttpMethod.Get);
                request.RequestUri!.AbsolutePath.ShouldBe("/v1.0/invoke/tenants-physical-17/method/alive");
                request.Content.ShouldBeNull();
                request.Headers.Authorization.ShouldBeNull();
                request.Headers.Contains(BoundWorkloadCommandProof.HeaderName).ShouldBeFalse();
                request.Headers.Contains(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName).ShouldBeFalse();
                int current = Interlocked.Increment(ref probes);
                if (scenario == "denied") return new HttpResponseMessage(HttpStatusCode.Forbidden);
                if (scenario == "cancel-after-health")
                {
                    stopping.Cancel();
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                if (current <= 2) return new HttpResponseMessage(current == 1 ? HttpStatusCode.InternalServerError : HttpStatusCode.ServiceUnavailable);
                entered.TrySetResult();
                await releaseHealth.Task.WaitAsync(token);
                return new HttpResponseMessage(HttpStatusCode.OK);
            })));
        builder.Services.AddHttpClient(TenantBootstrapServiceCollectionExtensions.CommandClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new BootstrapCommandHttpMessageHandler((request, _) =>
            {
                Interlocked.Increment(ref commands);
                request.Method.ShouldBe(HttpMethod.Post);
                request.RequestUri!.AbsolutePath.ShouldEndWith(bound ? "/api/v1/commands/bound/workload" : "/api/v1/commands");
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            }));
        await using ServiceProvider provider = builder.Services.BuildServiceProvider();
        bool scheduled = false;
        try
        {
            // Only the bootstrap deadline uses the controlled clock; real HTTP/service-discovery
            // policies retain their production clocks and independently tested behavior.
            var service = new TenantBootstrapHostedService(provider.GetRequiredService<IServiceScopeFactory>(),
                provider.GetRequiredService<IOptions<TenantBootstrapOptions>>(), builder.Configuration, builder.Environment,
                lifetime, provider.GetRequiredService<ILogger<TenantBootstrapHostedService>>(), clock);
            await service.StartAsync(TestContext.Current.CancellationToken);
            scheduled = true;
            started.Cancel();
            if (scenario.StartsWith("catalog", StringComparison.Ordinal))
            {
                await catalogEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                deadlineTimers.ShouldBe(1);
                metadataCalls.ShouldBe(0);
                AssertNoEffects();
                if (scenario == "catalog-cancel") stopping.Cancel();
                else releaseCatalog.TrySetResult();
            }
            if (scenario is not ("foreign" or "denied" or "cancel-after-health" or "catalog-cancel"))
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                deadlineTimers.ShouldBe(1, "catalog and self readiness must share the original deadline");
                AssertNoEffects();
                if (scenario == "delayed") releaseHealth.TrySetResult();
                else if (scenario.EndsWith("deadline", StringComparison.Ordinal)) expire.ShouldNotBeNull().Invoke();
                else stopping.Cancel();
            }
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            if (scenario == "delayed")
            {
                probes.ShouldBe(3);
                commands.ShouldBe(1);
                assertions.ShouldBe(bound ? 1 : 0);
                if (bound)
                {
                    var receipt = JsonSerializer.Deserialize<TenantBootstrapReceipt>(await File.ReadAllBytesAsync(Path.Combine(directory, "tenants.bootstrap.json"), TestContext.Current.CancellationToken)).ShouldNotBeNull();
                    receipt.Phase.ShouldBe("accepted");
                    receipt.HttpStatus.ShouldBe(202);
                }
                else File.Exists(Path.Combine(directory, "tenants.bootstrap.json")).ShouldBeFalse();
            }
            else AssertNoEffects();
            if (scenario == "foreign") probes.ShouldBe(0);
            if (scenario == "denied") probes.ShouldBe(1);
            deadlineTimers.ShouldBe(1);
            timer.Received().Dispose();
        }
        finally
        {
            stopping.Cancel();
            if (scheduled) await completed.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
            Directory.Delete(directory, recursive: true);
        }

        void AssertNoEffects()
        {
            assertions.ShouldBe(0);
            commands.ShouldBe(0);
            File.Exists(Path.Combine(directory, "tenants.bootstrap.json")).ShouldBeFalse();
            dapr.DidNotReceive().CreateInvokeMethodRequest(HttpMethod.Post, Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
