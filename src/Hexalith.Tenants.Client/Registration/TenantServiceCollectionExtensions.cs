using Hexalith.Tenants.Client.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Client.Registration;

/// <summary>
/// Extension methods for registering tenant client services in the dependency injection container.
/// </summary>
public static class TenantServiceCollectionExtensions
{
    /// <summary>
    /// Registers tenant client services in the dependency injection container with configuration bound from appsettings.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHexalithTenants(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        EnsureCoreRegistrations(services);

        // Opportunistic configuration binding
        IConfiguration? configuration = TryGetConfiguration(services);
        if (configuration is not null && !HasTenantOptionsConfiguration(services))
        {
            _ = services.Configure<HexalithTenantsOptions>(configuration.GetSection("Tenants"));
        }

        return services;
    }

    /// <summary>
    /// Registers tenant client services in the dependency injection container with explicit options configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">A delegate to configure <see cref="HexalithTenantsOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHexalithTenants(
        this IServiceCollection services,
        Action<HexalithTenantsOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        EnsureCoreRegistrations(services);

        // Idempotency: skip duplicate options configuration (same sentinel as parameterless overload)
        if (!HasTenantOptionsConfiguration(services))
        {
            _ = services.Configure(configureOptions);
        }

        return services;
    }

    private static void EnsureCoreRegistrations(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!services.Any(s => s.ServiceType == typeof(Dapr.Client.DaprClient)))
        {
            services.AddDaprClient();
        }

        _ = services.AddOptions<HexalithTenantsOptions>();
    }

    private static bool HasTenantOptionsConfiguration(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.Any(s => s.ServiceType == typeof(IConfigureOptions<HexalithTenantsOptions>));
    }

    private static IConfiguration? TryGetConfiguration(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        ServiceDescriptor? descriptor = services.LastOrDefault(static s => s.ServiceType == typeof(IConfiguration));
        if (descriptor?.ImplementationInstance is IConfiguration configurationInstance)
        {
            return configurationInstance;
        }

        if (descriptor is null)
        {
            return null;
        }

        using ServiceProvider tempProvider = services.BuildServiceProvider();
        return tempProvider.GetService<IConfiguration>();
    }
}
