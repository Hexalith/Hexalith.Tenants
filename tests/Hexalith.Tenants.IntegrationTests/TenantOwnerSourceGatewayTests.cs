using System.Diagnostics;
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
#endif
using Hexalith.EventStore.Client.Streams;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Tenants.Authorization;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Preserves the owner-source wire contract through the production HTTP registration.</summary>
[Collection("BootstrapReadiness")]
public sealed class TenantOwnerSourceGatewayTests
{
    [Fact]
    public async Task ActualProgramRegistersTheOwnerSourceClientWithTheSelectedEndpoint()
    {
        const string endpoint = "http://localhost:3579";
        using RSA key = RSA.Create(2048);
        var issuer = CreateIssuer();
        var terminal = new OwnerSourceCaptureHandler();
        string? previousEndpoint = Environment.GetEnvironmentVariable("DAPR_HTTP_ENDPOINT");
        try
        {
            Environment.SetEnvironmentVariable("DAPR_HTTP_ENDPOINT", endpoint);
            using var factory = new WebApplicationFactory<TenantOwnerSourceGateway>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Development");
                    builder.UseSetting("Tenants:BootstrapGlobalAdminEnabled", "false");
                    builder.UseSetting("Authentication:BoundStreamRead:PrivateKey", Convert.ToBase64String(key.ExportPkcs8PrivateKey()));
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<IWorkloadAssertionIssuer>();
                        services.AddSingleton(issuer);
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
                        ConfigureCatalogAuthorityControl(services, CreateConfiguration(endpoint), terminal);
#endif
                        // Configure only the primary transport; Program must supply the production endpoint and Dapr handlers.
                        services.AddHttpClient<TenantOwnerSourceGateway>().ConfigurePrimaryHttpMessageHandler(() => terminal);
                    });
                });
            await factory.Services.GetRequiredService<TenantOwnerSourceGateway>().ReadStreamAsync(
                new("system", "tenants", "owner-source"), TestContext.Current.CancellationToken);
            terminal.ReadCount.ShouldBe(1);
            terminal.Uri.ShouldBe(new Uri(endpoint + "/api/v1/streams/read/workload"));
            terminal.Headers["dapr-app-id"].ShouldBe(["eventstore"]);
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
            AssertCatalogHeaders(factory.Services, terminal);
#endif
        }
        finally
        {
            Environment.SetEnvironmentVariable("DAPR_HTTP_ENDPOINT", previousEndpoint);
        }
    }

    [Theory]
    [InlineData("http://localhost:3579", "tenants", "owner-source")]
    [InlineData("http://127.0.0.1:3579", "tenants", "owner-source")]
    [InlineData("https://localhost:4579", "tenants", "owner-source")]
    [InlineData("http://localhost:3579", "global-administrators", "global-administrators")]
    [InlineData("http://127.0.0.1:3579", "global-administrators", "global-administrators")]
    [InlineData("https://localhost:4579", "global-administrators", "global-administrators")]
    public async Task ProductionRegistration_PreservesBoundWorkloadRequestAndPerSendSidecarToken(string endpoint, string domain, string aggregate)
    {
        using RSA key = RSA.Create(2048);
        var issuer = Substitute.For<IWorkloadAssertionIssuer>();
        string assertion = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        issuer.IssueAsync(Arg.Any<WorkloadAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>(assertion));
        var terminal = new OwnerSourceCaptureHandler();
        string? previousToken = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");
        try
        {
            IConfiguration configuration = CreateConfiguration(endpoint, key);
            // Coordinates are selected independently of the production client registration.
            using ServiceProvider services = CreateProductionServices(configuration, CreateConfiguration(endpoint), issuer, terminal);
            TenantOwnerSourceGateway gateway = services.GetRequiredService<TenantOwnerSourceGateway>();
            string[]? previousProof = null;
            string?[] tokens = [Convert.ToHexString(RandomNumberGenerator.GetBytes(16)), Convert.ToHexString(RandomNumberGenerator.GetBytes(16)), null];
            foreach (string? token in tokens)
            {
                Environment.SetEnvironmentVariable("DAPR_API_TOKEN", token);
                var request = new StreamReadRequest("system", domain, aggregate, FromSequence: 7, ToSequence: 12, PageSize: 2)
                {
                    RequireNativeProof = true,
                };
                StreamReadPage page = await gateway.ReadStreamAsync(request, TestContext.Current.CancellationToken);
                page.AggregateId.ShouldBe(request.AggregateId);
                terminal.Method.ShouldBe(HttpMethod.Post);
                terminal.Uri.ShouldBe(new Uri(new Uri(endpoint), "api/v1/streams/read/workload"));
                terminal.Headers["dapr-app-id"].ShouldBe(["eventstore"]);
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
                AssertCatalogHeaders(services, terminal);
#endif
                if (token is null) terminal.Headers.ContainsKey("dapr-api-token").ShouldBeFalse();
                else terminal.Headers["dapr-api-token"].ShouldBe([token]);
                terminal.Headers[EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName].ShouldBe([assertion]);
                JsonSerializer.Deserialize<StreamReadRequest>(terminal.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web)).ShouldBe(request);
                string[] proof = terminal.Headers["X-Hexalith-Bound-Stream-Read-Proof"].Single().Split('.');
                key.VerifyData(Encoding.ASCII.GetBytes(proof[0]), Decode(proof[1]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).ShouldBeTrue();
                using JsonDocument payload = JsonDocument.Parse(Decode(proof[0]));
                payload.RootElement.GetProperty("version").GetInt32().ShouldBe(1);
                payload.RootElement.GetProperty("caller").GetString().ShouldBe("tenants");
                payload.RootElement.GetProperty("tenant").GetString().ShouldBe("system");
                payload.RootElement.GetProperty("domain").GetString().ShouldBe(domain);
                payload.RootElement.GetProperty("aggregateId").GetString().ShouldBe(aggregate);
                (payload.RootElement.GetProperty("expiresAt").GetInt64() - payload.RootElement.GetProperty("issuedAt").GetInt64()).ShouldBe(30);
                if (previousProof is not null) proof[0].ShouldNotBe(previousProof[0]);
                previousProof = proof;
            }

            await issuer.Received(3).IssueAsync(Arg.Is<WorkloadAssertionRequest>(request =>
                request.Audience == "eventstore" && request.Operation == "eventstore:stream-read" && request.Bindings == null), Arg.Any<CancellationToken>());
        }
        finally
        {
            Environment.SetEnvironmentVariable("DAPR_API_TOKEN", previousToken);
        }
    }

    [Theory]
    [InlineData("http://localhost:3579", "4679", "http://localhost:3579/")]
    [InlineData("http://127.0.0.1:3579", "4679", "http://127.0.0.1:3579/")]
    [InlineData(null, "4679", "http://127.0.0.1:4679/")]
    [InlineData(null, null, "http://127.0.0.1:3500/")]
    public async Task ProductionRegistration_UsesEffectiveConfigurationAndSdkPortFallback(
        string? endpoint, string? port, string selectedEndpoint)
    {
        string? previousEndpoint = Environment.GetEnvironmentVariable("DAPR_HTTP_ENDPOINT");
        string? previousPort = Environment.GetEnvironmentVariable("DAPR_HTTP_PORT");
        try
        {
            Environment.SetEnvironmentVariable("DAPR_HTTP_ENDPOINT", "https://stale-runtime.invalid:5999");
            Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", "5999");
            using RSA key = RSA.Create(2048);
            IConfiguration configuration = new ConfigurationBuilder().AddEnvironmentVariables()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DAPR_HTTP_ENDPOINT"] = endpoint,
                    ["DAPR_HTTP_PORT"] = port,
                    ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(key.ExportPkcs8PrivateKey()),
                }).Build();
            var issuer = CreateIssuer();
            var terminal = new OwnerSourceCaptureHandler();
            using ServiceProvider services = CreateProductionServices(configuration, CreateConfiguration(selectedEndpoint), issuer, terminal);
            await services.GetRequiredService<TenantOwnerSourceGateway>().ReadStreamAsync(
                new("system", "tenants", "owner-source"), TestContext.Current.CancellationToken);
            terminal.Uri.ShouldBe(new Uri(new Uri(selectedEndpoint), "api/v1/streams/read/workload"));
            terminal.ReadCount.ShouldBe(1);
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
            AssertCatalogHeaders(services, terminal);
#endif
        }
        finally
        {
            Environment.SetEnvironmentVariable("DAPR_HTTP_ENDPOINT", previousEndpoint);
            Environment.SetEnvironmentVariable("DAPR_HTTP_PORT", previousPort);
        }
    }

#if HEXALITH_TECHNICAL_HTTP_CATALOG_BINDING
    [Theory]
    [InlineData("http://localhost:3579", "http://127.0.0.1:3579")]
    [InlineData("http://127.0.0.1:3579", "http://localhost:3579")]
    [InlineData("http://localhost:3579", "http://localhost:3580")]
    [InlineData("http://localhost:3579", "https://localhost:3579")]
    public async Task ProductionRegistration_RefusesDifferentBoundCoordinatesBeforeTransport(
        string endpoint, string authorityEndpoint)
    {
        using RSA key = RSA.Create(2048);
        IConfiguration configuration = CreateConfiguration(endpoint, key);
        var issuer = CreateIssuer();
        var matched = new OwnerSourceCaptureHandler();
        using (ServiceProvider services = CreateProductionServices(configuration, CreateConfiguration(endpoint), issuer, matched))
        {
            await services.GetRequiredService<TenantOwnerSourceGateway>().ReadStreamAsync(
                new("system", "tenants", "owner-source"), TestContext.Current.CancellationToken);
            matched.ReadCount.ShouldBe(1);
            AssertCatalogHeaders(services, matched);
        }

        var refused = new OwnerSourceCaptureHandler();
        using ServiceProvider mismatched = CreateProductionServices(configuration, CreateConfiguration(authorityEndpoint), issuer, refused);
        var exception = await Should.ThrowAsync<RouteCatalogUnavailableException>(() =>
            mismatched.GetRequiredService<TenantOwnerSourceGateway>().ReadStreamAsync(
                new("system", "tenants", "owner-source"), TestContext.Current.CancellationToken));
        exception.Message.ShouldBe("The v2 invocation uses a different Dapr runtime endpoint.");
        refused.ReadCount.ShouldBe(0);
        refused.Headers.ShouldBeEmpty();
    }
#endif

    [Theory]
    [InlineData("http://localhost:3579/", "http://localhost:3579")]
    [InlineData("http://localhost:3579", "http://localhost:3579/")]
    [InlineData("http://LOCALHOST:3579", "http://localhost:3579")]
    [InlineData("http://localhost:3579", "http://LOCALHOST:3579")]
    [InlineData("http://localhost:80", "http://localhost")]
    [InlineData("http://localhost", "http://localhost:80")]
    public async Task ProductionRegistration_AcceptsEquivalentIndependentCoordinateSpellings(
        string endpoint, string authorityEndpoint)
    {
        using RSA key = RSA.Create(2048);
        var terminal = new OwnerSourceCaptureHandler();
        using ServiceProvider services = CreateProductionServices(
            CreateConfiguration(endpoint, key), CreateConfiguration(authorityEndpoint), CreateIssuer(), terminal);
        await services.GetRequiredService<TenantOwnerSourceGateway>().ReadStreamAsync(
            new("system", "tenants", "owner-source"), TestContext.Current.CancellationToken);
        terminal.ReadCount.ShouldBe(1);
        terminal.Uri.ShouldBe(new Uri(new Uri(endpoint), "api/v1/streams/read/workload"));
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
        AssertCatalogHeaders(services, terminal);
#endif
    }

    [Theory]
    [InlineData("system", "global-administrators", "another-aggregate")]
    [InlineData("another-tenant", "global-administrators", "global-administrators")]
    [InlineData("system", "another-domain", "global-administrators")]
    public async Task NeighboringAuthorityScopesAreRefusedBeforeAnyAssertionOrTransport(string tenant, string domain, string aggregate)
    {
        var issuer = Substitute.For<IWorkloadAssertionIssuer>();
        using var http = new HttpClient(new OwnerSourceCaptureHandler());
        using RSA scopeKey = RSA.Create(2048);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(scopeKey.ExportPkcs8PrivateKey()) }).Build();
        var gateway = new TenantOwnerSourceGateway(http, issuer, configuration, TimeProvider.System);
        await Should.ThrowAsync<InvalidOperationException>(() => gateway.ReadStreamAsync(new(tenant, domain, aggregate)));
        await issuer.DidNotReceiveWithAnyArgs().IssueAsync(default!);
    }

    [Fact]
    public async Task RepresentativeNativeReplayIncludesEverySignedHttpWindowAndFinalProof()
    {
        var source = new MutableGlobalAdministratorSource("admin", "keeper");
        for (int i = 0; i < 4999; i++)
        {
            source.Append(new GlobalAdministratorSet("system", "cost"));
            source.Append(new GlobalAdministratorRemoved("system", "cost"));
        }
        using RSA scopeKey = RSA.Create(2048);
        var issuer = Substitute.For<IWorkloadAssertionIssuer>();
        issuer.IssueAsync(Arg.Any<WorkloadAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("controlled-workload-assertion"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(scopeKey.ExportPkcs8PrivateKey()) }).Build();
        var terminal = new OwnerSourceCaptureHandler(source.Snapshot(), scopeKey);
        using var http = new HttpClient(terminal) { BaseAddress = new Uri("http://localhost/") };
        var gateway = new TenantOwnerSourceGateway(http, issuer, configuration, TimeProvider.System);
        var reader = new AuthoritativeEventStreamReader(gateway, TimeProvider.System, TimeSpan.FromSeconds(10), requireNativeProof: true);
        var authority = new TenantsGlobalAdministratorAuthority(reader, source, TimeProvider.System);
        var timer = Stopwatch.StartNew();
        (await authority.IsCurrentGlobalAdministratorAsync("admin", TestContext.Current.CancellationToken)).ShouldBeTrue();
        terminal.ReadCount.ShouldBe(102);
        TestContext.Current.TestOutputHelper!.WriteLine($"10000 native events through {terminal.ReadCount} signed/verified in-process HTTP windows, copy/digest validation, fold and final proof: {timer.Elapsed.TotalMilliseconds:F2} ms. External workload-token issuance and Dapr/network excluded.");
    }

    private static IConfiguration CreateConfiguration(string endpoint, RSA? key = null)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DAPR_HTTP_ENDPOINT"] = endpoint,
            ["Authentication:BoundStreamRead:PrivateKey"] = key is null ? null : Convert.ToBase64String(key.ExportPkcs8PrivateKey()),
        }).Build();

    private static IWorkloadAssertionIssuer CreateIssuer()
    {
        var issuer = Substitute.For<IWorkloadAssertionIssuer>();
        issuer.IssueAsync(Arg.Any<WorkloadAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("isolated-owner-source-assertion"));
        return issuer;
    }

    private static ServiceProvider CreateProductionServices(
        IConfiguration configuration, IConfiguration authorityConfiguration,
        IWorkloadAssertionIssuer issuer, OwnerSourceCaptureHandler terminal)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(issuer);
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
        services.AddEventStoreRouteCatalog(configuration);
        ConfigureCatalogAuthorityControl(services, authorityConfiguration, terminal);
#endif
        services.AddTenantOwnerSourceGateway(configuration)
            .ConfigurePrimaryHttpMessageHandler(() => terminal);
        return services.BuildServiceProvider();
    }

#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
    private static void ConfigureCatalogAuthorityControl(
        IServiceCollection services, IConfiguration authorityConfiguration, OwnerSourceCaptureHandler terminal)
    {
#if HEXALITH_TECHNICAL_HTTP_CATALOG_BINDING
        // Isolated endpoint-guard control: these authority/receiver identities are test doubles,
        // not native source proof or evidence of a deployed receiver inventory/activation.
        CommittedRouteCatalogSnapshot selected = CreateBoundSnapshot(authorityConfiguration);
        selected.TechnicalHttpReceivers.ShouldNotBeEmpty();
        selected.ActivationBindingDigest.ShouldNotBeNullOrWhiteSpace();
        selected.AuthorityCoordinates.ShouldNotBeNull();
#else
        // Supported older catalog SDKs retain their existing auxiliary workload inventory.
        CommittedRouteCatalogSnapshot owner = TenantOwnerCatalogTestAuthority.CreateSnapshot("tenants");
        var selected = new CommittedRouteCatalogSnapshot(owner.GetCanonicalBytes(), owner.RootDigest, owner.Epoch,
            [new("eventstore", "api/v1/streams/read/workload", "eventstore:stream-read", "gateway")], localAppId: "tenants");
#endif
        services.AddSingleton(selected);
        services.RemoveAll<IRouteCatalogAuthority>();
        services.AddSingleton(TenantOwnerCatalogTestAuthority.Create(selected));
        services.Configure<RouteCatalogHostOptions>(options =>
        {
#if HEXALITH_TECHNICAL_HTTP_CATALOG_BINDING
            options.RequireTechnicalHttpBinding = true;
#endif
            options.InvocationTransportFactory = _ => terminal.CreateIndependentTransport();
        });
    }
#endif

#if HEXALITH_TECHNICAL_HTTP_CATALOG_BINDING
    private static CommittedRouteCatalogSnapshot CreateBoundSnapshot(IConfiguration authorityConfiguration)
    {
        CommittedRouteCatalogSnapshot owner = TenantOwnerCatalogTestAuthority.CreateSnapshot("tenants");
        string digest = Convert.ToHexStringLower(SHA256.HashData("isolated-endpoint-guard"u8));
        var activation = new RouteCatalogActivation("isolated-owner-source-endpoint", owner.GetCanonicalBytes(),
            owner.RootDigest, null,
            [new("tenants", "tenants", digest, digest, "isolated-test"),
             new("eventstore", "eventstore", digest, digest, "isolated-test")],
            "isolated-endpoint-guard", "isolated-test")
        {
            TechnicalHttpReceivers =
            [new("eventstore", RouteCatalogTechnicalHttpPolicy.FenceVersion,
                [new("/api/v1/streams/read/workload", ["POST"], "isolated-workload-read-action", digest, digest,
                    ["DaprInternal:eventstore:stream-read"], digest, digest) { CredentialKind = "workload" }])
                { Invocations = [new("tenants", "/api/v1/streams/read/workload", ["POST"])] }],
        };
        return new CommittedRouteCatalogSnapshot(activation, owner.Epoch, "tenants",
            RouteCatalogStateCoordinates.Capture(authorityConfiguration, new RouteCatalogHostOptions()));
    }

#endif

#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
    private static void AssertCatalogHeaders(IServiceProvider services, OwnerSourceCaptureHandler terminal)
    {
        CommittedRouteCatalogSnapshot selected = services.GetRequiredService<CommittedRouteCatalogSnapshot>();
        terminal.Headers[RouteCatalogInvocationHandler.RootHeader].ShouldBe([selected.RootDigest]);
        terminal.Headers[RouteCatalogInvocationHandler.EpochHeader].ShouldBe(["1"]);
#if HEXALITH_TECHNICAL_HTTP_CATALOG_BINDING
        terminal.Headers[RouteCatalogInvocationHandler.BindingHeader].ShouldBe([selected.ActivationBindingDigest!]);
#endif
    }
#endif

    private static byte[] Decode(string value)
    {
        string encoded = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(encoded.PadRight((encoded.Length + 3) / 4 * 4, '='));
    }
}
