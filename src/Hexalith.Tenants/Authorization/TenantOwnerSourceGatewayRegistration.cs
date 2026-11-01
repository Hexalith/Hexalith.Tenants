using Hexalith.EventStore.Client.Registration;
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
using Hexalith.EventStore.ServiceDefaults.Routing;

using Microsoft.Extensions.Options;
#endif

namespace Hexalith.Tenants.Authorization;

/// <summary>Composes the owner-source client with the selected local Dapr coordinates.</summary>
internal static class TenantOwnerSourceGatewayRegistration
{
    /// <summary>Registers the bound owner read transport using the SDK's effective endpoint selection.</summary>
    /// <param name="services">The owner's services.</param>
    /// <param name="configuration">The effective runtime configuration.</param>
    /// <returns>The production client builder for transport composition.</returns>
    internal static IHttpClientBuilder AddTenantOwnerSourceGateway(
        this IServiceCollection services, IConfiguration configuration)
        => services.AddHttpClient<TenantOwnerSourceGateway>((provider, client) =>
        {
#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
            client.BaseAddress = new Uri(RouteCatalogStateCoordinates.Capture(
                configuration, provider.GetRequiredService<IOptions<RouteCatalogHostOptions>>().Value).HttpEndpoint);
#else
            client.BaseAddress = new Uri(configuration["DAPR_HTTP_ENDPOINT"]
                ?? $"http://127.0.0.1:{configuration["DAPR_HTTP_PORT"] ?? "3500"}");
#endif
        })
        .AddEventStoreDaprServiceInvocation(
            static _ => Environment.GetEnvironmentVariable("DAPR_API_TOKEN"),
            static _ => "eventstore",
            static token => token);
}
