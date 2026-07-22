using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Registers the tenant configuration trust boundary for standalone and embedded hosts.
/// </summary>
internal static class TenantConfigurationServiceCollectionExtensions
{
    /// <summary>
    /// Adds idempotent principal and deployment-policy composition services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configuration">Current host configuration.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddTenantConfigurationReadPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services.AddHttpContextAccessor();
        services.TryAddSingleton<CircuitServicesAccessor>();
        services.TryAddScoped<ITenantConfigurationPrincipalResolver, TenantConfigurationPrincipalResolver>();
        services.TryAddSingleton(_ => new TenantConfigurationReadPolicyProvider(configuration));
        return services;
    }
}
