using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Tenants.Bootstrap;

/// <summary>Registers the tenant bootstrap service and its credential-only transport policy.</summary>
internal static class TenantBootstrapServiceCollectionExtensions
{
    /// <summary>Gets the client name reserved for delegated credential acquisition.</summary>
    internal const string CredentialClientName = "TenantBootstrapCredentials";

    /// <summary>Registers bootstrap with a credential client that never follows redirects.</summary>
    /// <param name="services">The owning host service collection.</param>
    /// <returns>The supplied service collection.</returns>
    internal static IServiceCollection AddTenantBootstrap(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpClient(CredentialClientName)
            .ConfigurePrimaryHttpMessageHandler(static () => new SocketsHttpHandler { AllowAutoRedirect = false });
        services.AddHostedService<TenantBootstrapHostedService>();
        return services;
    }
}
