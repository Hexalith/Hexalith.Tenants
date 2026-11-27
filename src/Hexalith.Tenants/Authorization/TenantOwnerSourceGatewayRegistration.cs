using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Client.Streams;
using Hexalith.Tenants.Configuration;
using Hexalith.EventStore.ServiceDefaults;
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.Extensions.Options;
#endif

namespace Hexalith.Tenants.Authorization;

/// <summary>Composes the owner-source client with the selected local Dapr coordinates and configured gateway.</summary>
internal static class TenantOwnerSourceGatewayRegistration
{
    /// <summary>Registers the owner source readers and native administrator authority used by the domain host.</summary>
    internal static void AddTenantOwnerSourceAuthority(this IServiceCollection services)
    {
        services.AddTenantOwnerSourceGateway();
        services.AddScoped<IAuthoritativeEventStreamReader>(provider =>
            new AuthoritativeEventStreamReader(provider.GetRequiredService<TenantOwnerSourceGateway>(),
                provider.GetRequiredService<TimeProvider>(), TimeSpan.FromSeconds(30)));
        services.AddKeyedScoped<IAuthoritativeEventStreamReader>("native-tenant-source", (provider, _) =>
            new AuthoritativeEventStreamReader(provider.GetRequiredService<TenantOwnerSourceGateway>(),
                provider.GetRequiredService<TimeProvider>(), TimeSpan.FromSeconds(30), requireNativeProof: true));
        // Native metadata uses the same enrolled bound workload transport as the full stream.
        services.AddScoped<INativeSourceMetadataReader>(provider =>
            new GatewayNativeSourceMetadataReader(provider.GetRequiredService<TenantOwnerSourceGateway>(),
                provider.GetRequiredService<TimeProvider>(), TimeSpan.FromSeconds(8)));
        services.AddScoped<ITenantsGlobalAdministratorAuthority, TenantsGlobalAdministratorAuthority>();
    }

    /// <summary>Registers the bound owner read transport using the SDK's effective endpoint selection.</summary>
    /// <param name="services">The owner's services.</param>
    /// <returns>The production client builder for transport composition.</returns>
    internal static IHttpClientBuilder AddTenantOwnerSourceGateway(this IServiceCollection services)
        => services.AddHttpClient<TenantOwnerSourceGateway>((provider, client) =>
        {
            IConfiguration configuration = provider.GetRequiredService<IConfiguration>();
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
            client.BaseAddress = new Uri(RouteCatalogStateCoordinates.Capture(
                configuration, provider.GetRequiredService<IOptions<RouteCatalogHostOptions>>().Value).HttpEndpoint);
#else
            client.BaseAddress = new Uri(configuration["DAPR_HTTP_ENDPOINT"]
                ?? $"http://127.0.0.1:{configuration["DAPR_HTTP_PORT"] ?? "3500"}");
#endif
        })
        .AddEventStoreBoundedInvocationResilience()
        .AddEventStoreDaprServiceInvocation(
            static provider => (AppId: TenantEventStoreTarget.AppId(provider.GetRequiredService<IConfiguration>()),
                Token: Environment.GetEnvironmentVariable("DAPR_API_TOKEN")),
            static state => state.AppId,
            static state => state.Token);
}
