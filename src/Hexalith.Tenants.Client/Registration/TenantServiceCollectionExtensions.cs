using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Tenants.Client.Registration;

/// <summary>
/// Registers the Tenants domain consumer in a service that subscribes to tenant events.
/// </summary>
/// <remarks>
/// This is the domain-centric composition root: the generic event-subscription/dedup plumbing comes from
/// the EventStore client SDK (A3 — <see cref="EventStoreDomainEventsServiceCollectionExtensions"/>); only
/// the tenant-specific consumer (the local <see cref="TenantProjectionEventHandler"/> projection and its
/// <see cref="ITenantProjectionStore"/>) lives here.
/// </remarks>
public static class TenantServiceCollectionExtensions {
    private sealed class TenantProjectionRegistrationMarker;

    /// <summary>
    /// The DAPR pub/sub topic carrying tenant domain events.
    /// </summary>
    public const string TopicName = "tenants.events";

    /// <summary>
    /// The HTTP route the tenant subscription endpoint is mapped to.
    /// </summary>
    public const string SubscriptionRoute = "/tenants/events";

    /// <summary>
    /// Registers the Tenants domain consumer (subscription plumbing + the built-in local projection).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHexalithTenants(this IServiceCollection services)
        => services.AddHexalithTenants(static _ => { });

    /// <summary>
    /// Registers the Tenants domain consumer, allowing the caller to override the
    /// <see cref="EventStoreDomainEventsOptions"/> (e.g. the pub/sub component name).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">A delegate applied after the tenant defaults.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHexalithTenants(
        this IServiceCollection services,
        Action<EventStoreDomainEventsOptions> configureOptions) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        if (!services.Any(static s => s.ServiceType == typeof(Dapr.Client.DaprClient))) {
            services.AddDaprClient();
        }

        // Generic consumer plumbing (A3): the deduplicating processor + the tenant event-type registry,
        // configured for the tenant pub/sub topic and the payload→aggregate integrity check (the tenant
        // event payload's TenantId must equal the envelope AggregateId — the managed tenant ID).
        _ = services.AddEventStoreDomainEvents(
            typeof(TenantCreated).Assembly,
            options => {
                options.TopicName = TopicName;
                options.SubscriptionRoute = SubscriptionRoute;
                options.PayloadAggregateIdPropertyName = "TenantId";
                configureOptions(options);
            });

        // Domain-specific consumer: the tenant local projection and its store.
        EnsureTenantProjectionRegistrations(services);

        return services;
    }

    private static void EnsureTenantProjectionRegistrations(IServiceCollection services) {
        if (services.Any(static s => s.ServiceType == typeof(TenantProjectionRegistrationMarker))) {
            return;
        }

        _ = services.AddSingleton<TenantProjectionRegistrationMarker>();

        if (!services.Any(static s => s.ServiceType == typeof(ITenantProjectionStore))) {
            _ = services.AddSingleton<ITenantProjectionStore, InMemoryTenantProjectionStore>();
        }

        // The projection handler is a singleton so its per-tenant write locks are shared across every
        // consumed event; it is exposed through the platform handler interface for each tenant event type.
        _ = services.AddSingleton<TenantProjectionEventHandler>();
        RegisterProjectionHandler<TenantCreated>(services);
        RegisterProjectionHandler<TenantUpdated>(services);
        RegisterProjectionHandler<TenantDisabled>(services);
        RegisterProjectionHandler<TenantEnabled>(services);
        RegisterProjectionHandler<UserAddedToTenant>(services);
        RegisterProjectionHandler<UserRemovedFromTenant>(services);
        RegisterProjectionHandler<UserRoleChanged>(services);
        RegisterProjectionHandler<TenantConfigurationSet>(services);
        RegisterProjectionHandler<TenantConfigurationRemoved>(services);
    }

    // The TenantProjectionRegistrationMarker guard guarantees this runs once, so a plain AddSingleton is
    // safe (TryAddEnumerable rejects a factory whose return type equals the service type).
    private static void RegisterProjectionHandler<TEvent>(IServiceCollection services)
        where TEvent : IEventPayload
        => services.AddSingleton<IEventStoreDomainEventHandler<TEvent>>(
            static sp => (IEventStoreDomainEventHandler<TEvent>)sp.GetRequiredService<TenantProjectionEventHandler>());
}
