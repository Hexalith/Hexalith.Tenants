using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Configuration;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Client.Subscription;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Client.Registration;

/// <summary>
/// Extension methods for registering tenant client services in the dependency injection container.
/// </summary>
public static class TenantServiceCollectionExtensions {
    private sealed class TenantEventInfrastructureMarker;

    /// <summary>
    /// Registers tenant client services in the dependency injection container with configuration bound from appsettings.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHexalithTenants(this IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);

        EnsureCoreRegistrations(services);
        EnsureEventHandlerRegistrations(services);

        // Opportunistic configuration binding
        IConfiguration? configuration = TryGetConfiguration(services);
        if (configuration is not null && !HasTenantOptionsConfiguration(services)) {
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
        Action<HexalithTenantsOptions> configureOptions) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        EnsureCoreRegistrations(services);
        EnsureEventHandlerRegistrations(services);

        // Idempotency: skip duplicate options configuration (same sentinel as parameterless overload)
        if (!HasTenantOptionsConfiguration(services)) {
            _ = services.Configure(configureOptions);
        }

        return services;
    }

    private static IReadOnlyDictionary<string, Type> BuildEventTypeRegistry() => typeof(TenantCreated).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEventPayload).IsAssignableFrom(t))
            .ToDictionary(t => t.FullName!, t => t);

    private static void EnsureCoreRegistrations(IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);

        if (!services.Any(s => s.ServiceType == typeof(Dapr.Client.DaprClient))) {
            services.AddDaprClient();
        }

        _ = services.AddOptions<HexalithTenantsOptions>();
    }

    private static void EnsureEventHandlerRegistrations(IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);

        if (services.Any(s => s.ServiceType == typeof(TenantEventInfrastructureMarker))) {
            return;
        }

        if (!services.Any(s => s.ServiceType == typeof(ITenantProjectionStore))) {
            _ = services.AddSingleton<ITenantProjectionStore, InMemoryTenantProjectionStore>();
        }

        _ = services.AddSingleton<TenantProjectionEventHandler>();
        RegisterEventHandler<TenantCreated, TenantProjectionEventHandler>(services);
        RegisterEventHandler<TenantUpdated, TenantProjectionEventHandler>(services);
        RegisterEventHandler<TenantDisabled, TenantProjectionEventHandler>(services);
        RegisterEventHandler<TenantEnabled, TenantProjectionEventHandler>(services);
        RegisterEventHandler<UserAddedToTenant, TenantProjectionEventHandler>(services);
        RegisterEventHandler<UserRemovedFromTenant, TenantProjectionEventHandler>(services);
        RegisterEventHandler<UserRoleChanged, TenantProjectionEventHandler>(services);
        RegisterEventHandler<TenantConfigurationSet, TenantProjectionEventHandler>(services);
        RegisterEventHandler<TenantConfigurationRemoved, TenantProjectionEventHandler>(services);

        IReadOnlyDictionary<string, Type> registry = BuildEventTypeRegistry();
        _ = services.AddSingleton(registry);

        _ = services.AddSingleton<TenantEventProcessor>();
        _ = services.AddSingleton<TenantEventInfrastructureMarker>();
    }

    private static bool HasTenantOptionsConfiguration(IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);
        return services.Any(s => s.ServiceType == typeof(IConfigureOptions<HexalithTenantsOptions>));
    }

    private static void RegisterEventHandler<TEvent, THandler>(IServiceCollection services)
        where TEvent : IEventPayload
        where THandler : class, ITenantEventHandler<TEvent> => services.AddSingleton<ITenantEventHandler<TEvent>>(sp => sp.GetRequiredService<THandler>());

    private static IConfiguration? TryGetConfiguration(IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);
        ServiceDescriptor? descriptor = services.LastOrDefault(static s => s.ServiceType == typeof(IConfiguration));
        if (descriptor?.ImplementationInstance is IConfiguration configurationInstance) {
            return configurationInstance;
        }

        if (descriptor is null) {
            return null;
        }

        using ServiceProvider tempProvider = services.BuildServiceProvider();
        return tempProvider.GetService<IConfiguration>();
    }
}
