using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

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

        // Prefer the container's configuration so an embedding host that composes its own root, or
        // passes a sub-section here, still reads the policy it actually runs on; fall back to the
        // supplied instance for hosts that register no IConfiguration.
        services.TryAddSingleton(serviceProvider => new TenantConfigurationReadPolicyProvider(
            serviceProvider.GetService<IConfiguration>() ?? configuration,
            serviceProvider.GetService<ILogger<TenantConfigurationReadPolicyProvider>>()));
        return services;
    }
}
