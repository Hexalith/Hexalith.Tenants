using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Tenants.Configuration;

using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Authorization;

/// <summary>Composes the owner-source client with the selected local Dapr coordinates and configured gateway.</summary>
internal static class TenantOwnerSourceGatewayRegistration
{
    /// <summary>Registers the bound owner read transport using the SDK's effective endpoint selection.</summary>
    /// <param name="services">The owner's services.</param>
    /// <returns>The production client builder for transport composition.</returns>
    internal static IHttpClientBuilder AddTenantOwnerSourceGateway(
        this IServiceCollection services)
        => services.AddHttpClient<TenantOwnerSourceGateway>((provider, client) =>
        {
            client.BaseAddress = new Uri(RouteCatalogStateCoordinates.Capture(
                provider.GetRequiredService<IConfiguration>(), provider.GetRequiredService<IOptions<RouteCatalogHostOptions>>().Value).HttpEndpoint);
        })
        .AddEventStoreDaprServiceInvocation(
            static provider => (AppId: TenantEventStoreTarget.AppId(provider.GetRequiredService<IConfiguration>()),
                Token: Environment.GetEnvironmentVariable("DAPR_API_TOKEN")),
            static state => state.AppId,
            static state => state.Token);
}
