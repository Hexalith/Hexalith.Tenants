using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Tenants.Configuration;

using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Authorization;

/// <summary>Preserves the owner source transport while selecting the actual configured physical gateway.</summary>
internal static class TenantOwnerSourceGatewayRegistration
{
    /// <summary>Registers the production owner reader and its SDK-owned final Dapr target selection.</summary>
    internal static IHttpClientBuilder AddTenantOwnerSourceGateway(this IServiceCollection services)
    {
        return services.AddHttpClient<TenantOwnerSourceGateway>((provider, client) =>
            {
                // Hostname aliases are different runtime identities. Use the SDK's exact configured coordinates.
                var coordinates = RouteCatalogStateCoordinates.Capture(provider.GetRequiredService<IConfiguration>(),
                    provider.GetRequiredService<IOptions<RouteCatalogHostOptions>>().Value);
                client.BaseAddress = new Uri(coordinates.HttpEndpoint);
            })
            .AddEventStoreDaprServiceInvocation(
                static provider => (AppId: TenantEventStoreTarget.AppId(provider.GetRequiredService<IConfiguration>()),
                    Token: Environment.GetEnvironmentVariable("DAPR_API_TOKEN")),
                static state => state.AppId,
                static state => state.Token);
    }
}
