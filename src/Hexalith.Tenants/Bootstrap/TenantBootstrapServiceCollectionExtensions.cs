using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Hexalith.Tenants.Bootstrap;

/// <summary>Registers the tenant bootstrap service and its credential and command transport policies.</summary>
internal static class TenantBootstrapServiceCollectionExtensions
{
    /// <summary>Gets the client name reserved for delegated credential acquisition.</summary>
    internal const string CredentialClientName = "TenantBootstrapCredentials";

    /// <summary>Gets the client name reserved for one bounded application command submission.</summary>
    internal const string CommandClientName = "TenantBootstrapCommands";

    /// <summary>Registers bootstrap with separate credential and bounded command clients.</summary>
    /// <param name="services">The owning host service collection.</param>
    /// <returns>The supplied service collection.</returns>
    internal static IServiceCollection AddTenantBootstrap(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpClient(CredentialClientName)
            .ConfigurePrimaryHttpMessageHandler(static () => new SocketsHttpHandler { AllowAutoRedirect = false });
        // Standard resilience disables HttpClient.Timeout. Remove replay and restore the finite send budget.
        services.AddHttpClient(CommandClientName)
            .ConfigurePrimaryHttpMessageHandler(static () => new SocketsHttpHandler { AllowAutoRedirect = false })
            .ConfigureAdditionalHttpMessageHandlers(static (handlers, _) =>
            {
                for (int index = handlers.Count - 1; index >= 0; index--)
                {
                    if (handlers[index] is ResilienceHandler) handlers.RemoveAt(index);
                }
            })
            .ConfigureHttpClient(static client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddHostedService<TenantBootstrapHostedService>();
        return services;
    }
}
