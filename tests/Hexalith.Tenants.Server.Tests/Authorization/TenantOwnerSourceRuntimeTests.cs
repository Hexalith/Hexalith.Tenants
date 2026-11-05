#if HEXALITH_TECHNICAL_HTTP_CATALOG_BINDING
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.EventStore.Client.Routing;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
#if PRIVATE_OWNER_SOURCE_READ_PROOF
using Hexalith.EventStore.Testing.OwnerSourceRead;
#endif
using Hexalith.Tenants.Authorization;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Contracts.Routing;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Authorization;

/// <summary>Uses the production source reader registration and real SDK catalog transport fence.</summary>
public sealed class TenantOwnerSourceRuntimeTests
{
#if DEBUG && HEXALITH_DEVELOPMENT_CATALOG_SOURCE_REFERENCE
    [Fact]
    public void PrivateSourceEnrollmentIsInertByDefaultAndProductionRefuses()
    {
        var services = new ServiceCollection();
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Production");
        TenantOwnerSourceReadDevelopmentEnrollment.Add(services, new ConfigurationBuilder().Build(), environment);
        services.ShouldBeEmpty();
        var selected = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["EventStore:Testing:OwnerSourceRead:Enabled"] = "true" }).Build();
        Should.Throw<InvalidOperationException>(() => TenantOwnerSourceReadDevelopmentEnrollment.Add(services, selected, environment));
        services.ShouldBeEmpty();
    }

#if PRIVATE_OWNER_SOURCE_READ_PROOF
    /// <summary>Gets whether the actual private IPC implementation is supported on this platform.</summary>
    public static bool LinuxIpcSupported => OperatingSystem.IsLinux();

    [Theory(Skip = "Private owner-source IPC requires Linux.", SkipUnless = nameof(LinuxIpcSupported))]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PrivateSourceHelperUsesActualOwnerIssuerKeyAndCatalogTransport(bool correctTarget)
    {
        string directory = Directory.CreateTempSubdirectory("tenant-owner-proof-").FullName;
        if (OperatingSystem.IsLinux()) File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        try
        {
            using RSA rsa = RSA.Create(2048);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DAPR_HTTP_ENDPOINT"] = "http://127.0.0.1:3500/",
                ["Authentication:WorkloadIssuer:Workload"] = "tenants",
                ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()),
                ["EventStore:Testing:OwnerSourceRead:Enabled"] = "true",
                ["EventStore:Testing:OwnerSourceRead:Directory"] = directory,
                ["EventStore:Testing:OwnerSourceRead:GatewayAppId"] = correctTarget ? "eventstore-isolated" : "foreign-target",
            }).Build();
            var coordinates = RouteCatalogStateCoordinates.Capture(configuration, new RouteCatalogHostOptions());
            var snapshot = Snapshot(coordinates, "tenants");
            var authority = Substitute.For<IRouteCatalogAuthority>();
            authority.AcquireAsync(Arg.Any<CancellationToken>()).Returns(_ => new RouteCatalogDispatchLease(snapshot, () => Task.CompletedTask));
            var issuer = Substitute.For<IWorkloadAssertionIssuer>();
            issuer.IssueAsync(Arg.Any<WorkloadAssertionRequest>(), Arg.Any<CancellationToken>()).Returns("owner-assertion");
            using var transport = new OwnerSourceTargetHttpHandler("eventstore-isolated", "tenants",
                Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()), new Uri(coordinates.HttpEndpoint), snapshot.RootDigest, snapshot.ActivationBindingDigest);
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(issuer);
            services.AddSingleton(TimeProvider.System);
            services.AddEventStoreRouteCatalog(configuration);
            services.AddSingleton(authority);
            services.Configure<RouteCatalogHostOptions>(options =>
            {
                options.DevelopmentFixture = true;
                options.Profile = "Development";
                options.RequireTechnicalHttpBinding = true;
                options.InvocationTransportFactory = _ => transport;
            });
            var environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns("Development");
            TenantOwnerSourceReadDevelopmentEnrollment.Add(services, configuration, environment);
            await using var provider = services.BuildServiceProvider();
            var worker = provider.GetServices<IHostedService>().Single(service => service.GetType().Name == "OwnerSourceReadWorker");
            await worker.StartAsync(TestContext.Current.CancellationToken);
            try
            {
                var client = new OwnerSourceReadClient(directory);
                if (correctTarget)
                {
                    var page = await client.ReadAsync(TestContext.Current.CancellationToken);
                    page.Tenant.ShouldBe("system");
                    page.Domain.ShouldBe("global-administrators");
                }
                else await Should.ThrowAsync<InvalidOperationException>(() => client.ReadAsync(TestContext.Current.CancellationToken));
                transport.Calls.ShouldBe(correctTarget ? 1 : 0);
                await issuer.Received(1).IssueAsync(Arg.Is<WorkloadAssertionRequest>(request =>
                    request.Audience == "eventstore" && request.Operation == "eventstore:stream-read"
                    && request.Bindings![EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType] == "platform:global-administrators"
                    && request.Bindings[EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType] == "global-administrators"), Arg.Any<CancellationToken>());
                await authority.Received(1).AcquireAsync(Arg.Any<CancellationToken>());
            }
            finally { await worker.StopAsync(TestContext.Current.CancellationToken); }
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
#else
    [Fact]
    public void UnselectedBuildRefusesPrivateSourceSelectionInDevelopment()
    {
        var services = new ServiceCollection();
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns("Development");
        var selected = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EventStore:Testing:OwnerSourceRead:Enabled"] = "true",
            ["Authentication:WorkloadIssuer:Workload"] = "tenants",
        }).Build();
        var refusal = Should.Throw<InvalidOperationException>(() => TenantOwnerSourceReadDevelopmentEnrollment.Add(services, selected, environment));
        refusal.Message.ShouldBe("Private owner source proof requires the explicitly selected Debug/source host build.");
        services.ShouldBeEmpty();
    }
#endif
#endif
    [Theory]
    [InlineData(null, null, "http://127.0.0.1:3500/", true)]
    [InlineData(null, "4567", "http://127.0.0.1:4567/", true)]
    [InlineData("http://localhost:5432", "9999", "http://localhost:5432/", true)]
    [InlineData("http://127.0.0.1:5432", "9999", "http://127.0.0.1:5432/", true)]
    [InlineData("http://localhost:5432", null, "http://127.0.0.1:5432/", false)]
    [InlineData("http://localhost:5432", null, "http://localhost:5433/", false)]
    [InlineData("http://localhost:5432", null, "https://localhost:5432/", false)]
    public async Task ActualSourceReaderPreservesRuntimeAddressAndRefusesDifferentCatalogCoordinates(
        string? endpoint, string? port, string observedEndpoint, bool accepted)
    {
        using RSA rsa = RSA.Create(2048);
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DAPR_HTTP_ENDPOINT"] = endpoint,
            ["DAPR_HTTP_PORT"] = port,
            ["Tenants:EventStoreAppId"] = "eventstore-isolated",
            ["Authentication:WorkloadIssuer:Workload"] = "tenants-isolated",
            ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()),
        }).Build();
        var coordinates = RouteCatalogStateCoordinates.Capture(configuration, new RouteCatalogHostOptions());
        if (accepted) coordinates.HttpEndpoint.ShouldBe(observedEndpoint);
        var snapshot = Snapshot(coordinates with { HttpEndpoint = observedEndpoint });
        var authority = Substitute.For<IRouteCatalogAuthority>();
        int releases = 0;
        authority.AcquireAsync(Arg.Any<CancellationToken>()).Returns(_ =>
            new RouteCatalogDispatchLease(snapshot, () => { releases++; return Task.CompletedTask; }));
        var issuer = Substitute.For<IWorkloadAssertionIssuer>();
        issuer.IssueAsync(Arg.Any<WorkloadAssertionRequest>(), Arg.Any<CancellationToken>()).Returns("owner-assertion");
        using var transport = new OwnerSourceTargetHttpHandler("eventstore-isolated", "tenants-isolated",
            Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()), new Uri(observedEndpoint),
            snapshot.RootDigest, snapshot.ActivationBindingDigest);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(issuer);
        services.AddSingleton(TimeProvider.System);
        services.AddEventStoreRouteCatalog(configuration);
        services.AddSingleton(authority);
        services.Configure<RouteCatalogHostOptions>(options => options.InvocationTransportFactory = _ => transport);
        services.AddTenantOwnerSourceGateway();
        await using var provider = services.BuildServiceProvider();
        var gateway = provider.GetRequiredService<TenantOwnerSourceGateway>();
        var request = new StreamReadRequest("system", "global-administrators", "global-administrators");

        if (accepted)
        {
            var page = await gateway.ReadStreamAsync(request, TestContext.Current.CancellationToken);
            page.Domain.ShouldBe(request.Domain);
        }
        else
        {
            var refusal = await Should.ThrowAsync<RouteCatalogUnavailableException>(() =>
                gateway.ReadStreamAsync(request, TestContext.Current.CancellationToken));
            refusal.Category.ShouldBe(RouteCatalogRefusalReason.Configuration);
            refusal.Message.ShouldBe("The v2 invocation uses a different Dapr runtime endpoint.");
        }

        transport.Calls.ShouldBe(accepted ? 1 : 0);
        await authority.Received(1).AcquireAsync(Arg.Any<CancellationToken>());
        releases.ShouldBe(1);
        await issuer.Received(1).IssueAsync(Arg.Is<WorkloadAssertionRequest>(value =>
            value.Audience == "eventstore" && value.Operation == "eventstore:stream-read"), Arg.Any<CancellationToken>());
    }

    private static CommittedRouteCatalogSnapshot Snapshot(RouteCatalogStateCoordinates coordinates, string owner = "tenants-isolated")
    {
        var declaration = TenantCatalogDeclarations.Queries.Single(query => query.MessageType == ResolveTenantOwnerAuthority.QueryType);
        var operation = RouteOperationSchema.Create(declaration, typeof(ResolveTenantOwnerAuthority),
            typeof(TenantOwnerAuthorityResult), "internal", "agent-eligible", JsonSerializerOptions.Web);
        var envelope = VersionedRouteCatalogBuilder.Build(new(1, "Development", [declaration], [operation],
            [new(declaration.RouteId, owner, "query", declaration.ContractVersion, false, "*")], []));
        byte[] bytes = RouteCatalogCodec.Encode(envelope);
        string digest = new('a', 64);
        var action = new RouteCatalogTechnicalHttpEndpoint("/api/v1/streams/read/workload", ["POST"],
            "test-installed-sdk-workload-action", digest, digest, ["DaprInternal:eventstore:stream-read"], digest, digest)
        {
            CredentialKind = "workload",
        };
        var activation = new RouteCatalogActivation("owner-runtime-test", bytes,
            Convert.ToHexStringLower(SHA256.HashData(bytes)), null,
            [new(owner, owner, digest, digest, digest),
             new("eventstore-isolated", "eventstore-isolated", digest, digest, digest)], "test-owner", "test-only")
        {
            TechnicalHttpReceivers = [new("eventstore-isolated", RouteCatalogTechnicalHttpPolicy.FenceVersion, [action])
            {
                Invocations = [new(owner, "/api/v1/streams/read/workload", ["POST"])],
            }],
        };
        return new(activation, 1, owner, coordinates);
    }
}
#endif
