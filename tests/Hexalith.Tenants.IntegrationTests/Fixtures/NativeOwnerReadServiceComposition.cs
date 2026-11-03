using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.ServiceDefaults;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Tenants.Authorization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Composes production owner readers using only this fixture's credentials and independently owned catalog custody.</summary>
internal static class NativeOwnerReadServiceComposition
{
    /// <summary>Creates a fixture-owned ephemeral enrollment key without retaining an RSA handle.</summary>
    internal static string CreatePrivateKey()
    {
        using RSA rsa = RSA.Create(2048);
        return Convert.ToBase64String(rsa.ExportPkcs8PrivateKey());
    }

    /// <summary>Builds the exact owner-sidecar and Development issuer configuration; key material never enters process arguments.</summary>
    internal static IConfiguration CreateConfiguration(Uri sidecar, string privateKey)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DAPR_HTTP_ENDPOINT"] = sidecar.AbsoluteUri,
            ["DAPR_API_TOKEN"] = Environment.GetEnvironmentVariable("DAPR_API_TOKEN"),
            ["DAPR_APP_ID"] = "tenants",
            ["Tenants:EventStoreAppId"] = "eventstore",
            ["Authentication:JwtBearer:Issuer"] = "hexalith-dev",
            ["Authentication:JwtBearer:Audience"] = "hexalith-eventstore",
            ["Authentication:JwtBearer:SigningKey"] = AspireTopologyFixture.DemoSigningKey,
            ["Authentication:WorkloadIssuer:Workload"] = "tenants",
            ["Authentication:BoundStreamRead:PrivateKey"] = privateKey,
            ["EventStore:RouteCatalog:Profile"] = Environments.Development,
            ["EventStore:RouteCatalog:DevelopmentFixture"] = "true",
            ["EventStore:RouteCatalog:RequireTechnicalHttpBinding"] = "true",
            ["EventStore:RouteCatalog:StateStore"] = Environment.GetEnvironmentVariable("Tenants__DevelopmentRouteCatalog__AuthorityComponentName") ?? "route-catalog",
            ["EventStore:RouteCatalog:StateKey"] = Environment.GetEnvironmentVariable("Tenants__DevelopmentRouteCatalog__AuthorityStateKey") ?? "route-catalog-authority-v1",
        }).Build();

    /// <summary>Registers real issuer, transport, authority and bounded native readers without launching any hosted resources.</summary>
    internal static IServiceCollection CreateServices(IConfiguration configuration)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development,
            DisableDefaults = true,
        });
        builder.Configuration.AddConfiguration(configuration);
        builder.AddServiceDefaults();
        IServiceCollection services = builder.Services;
        services.AddSingleton(configuration);
        services.AddSingleton(TimeProvider.System);
        services.AddEventStoreWorkloadAssertionIssuer(configuration);
        services.AddEventStoreRouteCatalog(configuration);
        services.AddTenantOwnerSourceGateway();
        services.AddScoped<IAuthoritativeEventStreamReader>(provider => new AuthoritativeEventStreamReader(
            provider.GetRequiredService<TenantOwnerSourceGateway>(), TimeProvider.System,
            TimeSpan.FromSeconds(30), requireNativeProof: true));
        services.AddScoped<INativeSourceMetadataReader>(provider => new GatewayNativeSourceMetadataReader(
            provider.GetRequiredService<TenantOwnerSourceGateway>(), TimeProvider.System, TimeSpan.FromSeconds(8)));
        return services;
    }

    /// <summary>Attaches the actual committed Tenants owner observation to the real Dapr authority.</summary>
    internal static async Task<ServiceProvider> CreateAsync(Uri sidecar, string privateKey, CancellationToken cancellationToken)
    {
        IConfiguration configuration = CreateConfiguration(sidecar, privateKey);
        IServiceCollection services = CreateServices(configuration);
#if HEXALITH_DEVELOPMENT_CATALOG_COHORT
        if (AspireTopologyFixture.IsSourceCatalogProfile)
        {
            var (state, directory) = await SourceCatalogBootstrapEvidence.ReadCommittedAsync(cancellationToken);
            var activation = state.Committed ?? throw new InvalidOperationException("native-owner-catalog-uncommitted");
            RegisterCommittedObservation(services, configuration, activation, directory, cancellationToken);
        }
#endif
        return services.BuildServiceProvider();
    }

#if HEXALITH_DEVELOPMENT_CATALOG_COHORT
    /// <summary>Registers and rechecks one privately retained owner observation against its independently supplied custody.</summary>
    internal static void RegisterCommittedObservation(IServiceCollection services, IConfiguration configuration,
        RouteCatalogActivation activation, string directory, CancellationToken cancellationToken)
    {
        var expectedCoordinates = RouteCatalogStateCoordinates.Capture(configuration, new RouteCatalogHostOptions
        {
            StateStore = configuration["EventStore:RouteCatalog:StateStore"]!,
            StateKey = configuration["EventStore:RouteCatalog:StateKey"]!,
        });
        string path = Path.Combine(directory, "tenants.observation.json");
        RouteCatalogHostObservation Observe(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            RouteCatalogHostObservation observation = JsonSerializer.Deserialize<RouteCatalogHostObservation>(
                SourceCatalogBootstrapEvidence.ReadPrivate(path, 16 * 1024 * 1024),
                new JsonSerializerOptions(JsonSerializerDefaults.Web) { MaxDepth = 64 })
                ?? throw new InvalidOperationException("native-owner-observation-missing");
            var identity = ObservedRouteCatalogHost.Identity(observation);
            if (observation.HostId != "tenants" || observation.AppId != "tenants"
                || observation.AuthorityCoordinates != expectedCoordinates
                || !activation.RequiredHosts.Contains(identity)
                || !observation.RequiredHostIds.Order(StringComparer.Ordinal).SequenceEqual(
                    activation.RequiredHosts.Select(host => host.HostId).Order(StringComparer.Ordinal), StringComparer.Ordinal))
                throw new InvalidOperationException("native-owner-observation-mismatched");
            return observation;
        }
        var identity = ObservedRouteCatalogHost.Identity(Observe(cancellationToken));
        services.Configure<RouteCatalogHostOptions>(options => options.Identity = identity);
        services.AddEventStoreRouteCatalogObservation((_, token) => Task.FromResult(Observe(token)));
    }
#endif
}
