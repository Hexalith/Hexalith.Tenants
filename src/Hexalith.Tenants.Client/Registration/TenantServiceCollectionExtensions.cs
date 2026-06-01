using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Configuration;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Client.Subscription;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Hexalith.Tenants.Client.Registration;

/// <summary>
/// Extension methods for registering tenant client services in the dependency injection container.
/// </summary>
public static class TenantServiceCollectionExtensions {
    private sealed class TenantEventHandlerRegistrationMarker<TEvent, THandler>
        where TEvent : IEventPayload
        where THandler : class, ITenantEventHandler<TEvent>;

    private sealed class TenantEventInfrastructureMarker;
    private sealed class TenantOptionsValidationMarker;

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
            _ = services.Configure<HexalithTenantsOptions>(configuration.GetSection(HexalithTenantsOptions.ConfigurationSectionName));
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

        _ = services.Configure(configureOptions);

        return services;
    }

    /// <summary>
    /// Registers a selected tenant event handler for the specified event payload type.
    /// </summary>
    /// <typeparam name="TEvent">The tenant event payload type to handle.</typeparam>
    /// <typeparam name="THandler">The handler implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTenantEventHandler<TEvent, THandler>(this IServiceCollection services)
        where TEvent : IEventPayload
        where THandler : class, ITenantEventHandler<TEvent> {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<THandler>();

        Type markerType = typeof(TenantEventHandlerRegistrationMarker<TEvent, THandler>);
        if (services.Any(s => s.ServiceType == markerType)) {
            return services;
        }

        _ = services.AddSingleton(markerType);
        _ = services.AddScoped<ITenantEventHandler<TEvent>>(sp => sp.GetRequiredService<THandler>());

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

        if (services.Any(s => s.ServiceType == typeof(TenantOptionsValidationMarker))) {
            return;
        }

        _ = services.AddOptions<HexalithTenantsOptions>().ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<HexalithTenantsOptions>, ValidateHexalithTenantsOptions>());
        _ = services.AddSingleton<TenantOptionsValidationMarker>();
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
