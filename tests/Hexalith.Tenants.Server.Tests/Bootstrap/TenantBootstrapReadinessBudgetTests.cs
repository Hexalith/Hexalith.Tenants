using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapr.Client;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Tenants.Bootstrap;
using Hexalith.Tenants.Contracts.Commands;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Bootstrap;

/// <summary>Uses the actual production HTTP defaults while preserving the finite readiness and installed bearer gates.</summary>
public sealed class TenantBootstrapReadinessBudgetTests
{
    /// <summary>Binds the installed options name to the public builder and observes a configured budget without changing transport settings.</summary>
    [Fact]
    public async Task ProductionDefaultPipelineProvidesItsActualConfiguredFiniteBudget()
    {
        string publicName = new ServiceCollection().AddHttpClient(Options.DefaultName).AddStandardResilienceHandler().PipelineName;
        publicName.ShouldBe(TenantBootstrapReadinessBudget.UnnamedStandardPipelineOptionsName);
        HostApplicationBuilder host = ProductionDefaults(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
        await using ServiceProvider services = host.Services.BuildServiceProvider();
        using HttpClient client = services.GetRequiredService<IHttpClientFactory>().CreateClient();
        client.Timeout.ShouldBe(Timeout.InfiniteTimeSpan);
        HttpStandardResilienceOptions configured = services.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>().Get(publicName);
        int originalRetries = configured.Retry.MaxRetryAttempts;
        TenantBootstrapReadinessBudget.Resolve(client, services).ShouldBe(TimeSpan.FromSeconds(3));
        client.Timeout.ShouldBe(Timeout.InfiniteTimeSpan);
        configured.Retry.MaxRetryAttempts.ShouldBe(originalRetries);
        configured.AttemptTimeout.Timeout.ShouldBe(TimeSpan.FromSeconds(1));
    }

    /// <summary>A finite client remains authoritative even when no resilience options are registered.</summary>
    [Fact]
    public async Task FiniteClientTimeoutKeepsItsExistingMeaning()
    {
        await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(7) };
        TenantBootstrapReadinessBudget.Resolve(client, services).ShouldBe(TimeSpan.FromSeconds(7));
    }

    /// <summary>Invalid, infinite and request-dependent resilience budgets never become readiness fallbacks.</summary>
    [Theory]
    [InlineData("zero")]
    [InlineData("negative")]
    [InlineData("infinite")]
    [InlineData("dynamic")]
    public async Task InvalidSelectedResilienceBudgetFailsClosed(string scenario)
    {
        var configured = new HttpStandardResilienceOptions();
        configured.TotalRequestTimeout.Timeout = scenario switch
        {
            "zero" => TimeSpan.Zero,
            "negative" => TimeSpan.FromSeconds(-2),
            "infinite" => Timeout.InfiniteTimeSpan,
            _ => TimeSpan.FromSeconds(3),
        };
        if (scenario == "dynamic") configured.TotalRequestTimeout.TimeoutGenerator = _ => ValueTask.FromResult(TimeSpan.FromSeconds(4));
        var monitor = Substitute.For<IOptionsMonitor<HttpStandardResilienceOptions>>();
        monitor.Get(TenantBootstrapReadinessBudget.UnnamedStandardPipelineOptionsName).Returns(configured);
        await using ServiceProvider services = new ServiceCollection().AddSingleton(monitor).BuildServiceProvider();
        using HttpClient client = new() { Timeout = Timeout.InfiniteTimeSpan };
        if (scenario == "dynamic") Should.Throw<InvalidOperationException>(() => TenantBootstrapReadinessBudget.Resolve(client, services));
        else Should.Throw<ArgumentOutOfRangeException>(() => TenantBootstrapReadinessBudget.Resolve(client, services));
    }

    /// <summary>Real bearer validation reaches a stalled catalog; configured expiry or caller cancellation stops it before assertion or proof issuance.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfiguredCatalogReadinessExpiresOrHonorsCancellationBeforeIssuance(bool cancelCaller)
    {
        HostApplicationBuilder host = ProductionDefaults(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200));
        string key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        using RSA proof = RSA.Create(2048);
        host.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:JwtBearer:Issuer"] = "bootstrap-budget-origin",
            ["Authentication:JwtBearer:Audience"] = "bootstrap-budget-reader",
            ["Authentication:JwtBearer:SigningKey"] = key,
            ["Authentication:JwtBearer:RequireHttpsMetadata"] = "false",
            ["Authentication:JwtBearer:AllowInsecureSymmetricKey"] = "true",
            ["Authentication:WorkloadIssuer:Workload"] = "tenants",
            ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(proof.ExportPkcs8PrivateKey()),
            ["EventStore:RouteCatalog:DevelopmentBootstrap:RootDigest"] = new string('a', 64),
        });
        host.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            JwtBearerAuthenticationContract.Configure(options, host.Configuration.GetSection("Authentication:JwtBearer").Get<JwtBearerAuthenticationOptions>()!));
        var authority = Substitute.For<IRouteCatalogAuthority>();
        var issuer = Substitute.For<IWorkloadAssertionIssuer>();
        int observations = 0;
        authority.ReadbackAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Interlocked.Increment(ref observations);
            return Task.FromResult(new RouteCatalogGenerationReadback(1, 1, new string('a', 64), new string('b', 64), new string('c', 64), "Development", [], false));
        });
        host.Services.AddSingleton(authority);
        host.Services.AddSingleton(issuer);
        await using ServiceProvider services = host.Services.BuildServiceProvider();
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        using HttpClient client = services.GetRequiredService<IHttpClientFactory>().CreateClient();
        TimeSpan budget = TenantBootstrapReadinessBudget.Resolve(client, scope.ServiceProvider);
        budget.ShouldBe(TimeSpan.FromSeconds(1));
        var jwt = new JwtSecurityToken("bootstrap-budget-origin", "bootstrap-budget-reader", [new Claim("sub", "administrator"), new Claim("global_admin", "true")],
            notBefore: DateTime.UtcNow.AddMinutes(-1), expires: DateTime.UtcNow.AddMinutes(2),
            signingCredentials: new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        string credential = new JwtSecurityTokenHandler().WriteToken(jwt);
        var command = new SubmitCommandRequest("01ARZ3NDEKTSV4RRFFQ69G5FAV", "system", "global-administrators", "global-administrators", nameof(BootstrapGlobalAdmin),
            JsonSerializer.SerializeToElement(new BootstrapGlobalAdmin("administrator")));
        using DaprClient dapr = new DaprClientBuilder().UseHttpEndpoint("http://127.0.0.1:3500").Build();
        using CancellationTokenSource caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        Task<HttpRequestMessage?> pending = TenantBootstrapBoundCommand.CreateAsync(scope.ServiceProvider, host.Configuration, dapr, command, "administrator", credential,
            TimeProvider.System, budget, caller.Token);
        if (cancelCaller)
        {
            while (Volatile.Read(ref observations) == 0 && !pending.IsCompleted) await Task.Yield();
            observations.ShouldBeGreaterThan(0);
            caller.Cancel();
        }
        await Should.ThrowAsync<OperationCanceledException>(async () => await pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        observations.ShouldBeGreaterThan(0);
        caller.IsCancellationRequested.ShouldBe(cancelCaller);
        await issuer.DidNotReceive().IssueAsync(Arg.Any<WorkloadAssertionRequest>(), Arg.Any<CancellationToken>());
    }

    private static HostApplicationBuilder ProductionDefaults(TimeSpan total, TimeSpan attempt)
    {
        HostApplicationBuilder host = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Development });
        host.AddServiceDefaults();
        host.Services.Configure<HttpStandardResilienceOptions>(TenantBootstrapReadinessBudget.UnnamedStandardPipelineOptionsName, options =>
        {
            options.TotalRequestTimeout.Timeout = total;
            options.AttemptTimeout.Timeout = attempt;
        });
        return host;
    }
}
