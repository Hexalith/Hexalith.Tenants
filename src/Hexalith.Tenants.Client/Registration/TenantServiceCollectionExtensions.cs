using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
using Hexalith.EventStore.Client.Projections.Checkpointed;
#endif
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Handlers;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Events;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

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

#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
    private sealed class TenantRejectionCheckpointRegistrationMarker;
#endif

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

#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
    /// <summary>
    /// Opts the tenant projection into the durable, checkpointed EventStore read-model record so every replica
    /// and every restart reads the same persisted state, health, and checkpoint.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registers <see cref="DurableTenantProjectionStore"/> as both <see cref="ITenantProjectionStore"/> and
    /// <see cref="ITenantProjectionSnapshotStore"/>, replacing any earlier projection store registration. Call it
    /// before or after <see cref="AddHexalithTenants(IServiceCollection)"/>; without this opt-in the default
    /// in-memory store is unchanged. Every tenant-domain rejection event is also handled, as a checkpoint-only transition.
    /// </para>
    /// <para>
    /// Persistence uses the platform <see cref="IReadModelStore"/> (registered when absent). Older-delivery
    /// verification and gap recovery read authoritative history through a registered
    /// <see cref="IEventStoreGatewayClient"/>, resolved in a new scope for every read; without one they stay
    /// retryable-unavailable and deny.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="stateStoreName">The read-model state-store component name.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHexalithTenantsDurableProjection(this IServiceCollection services, string stateStoreName) {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateStoreName);

        _ = services.AddEventStoreReadModelStore();
        _ = services.RemoveAll<DurableTenantProjectionOptions>();
        _ = services.AddSingleton(new DurableTenantProjectionOptions(stateStoreName));
        services.TryAddSingleton(static sp => {
            DurableTenantProjectionOptions options = sp.GetRequiredService<DurableTenantProjectionOptions>();

            // The singleton store never holds a typed gateway HttpClient: history resolves one per read in its own scope.
            bool hasGateway = sp.GetService<IServiceProviderIsService>()?.IsService(typeof(IEventStoreGatewayClient)) == true;
            return new DurableTenantProjectionStore(
                sp.GetRequiredService<IReadModelStore>(),
                options,
                hasGateway ? new CheckpointedProjectionHistory(sp.GetRequiredService<IServiceScopeFactory>(), options.Checkpoint) : null,
                sp.GetService<TimeProvider>(),
                sp.GetService<ILogger<DurableTenantProjectionStore>>());
        });
        _ = services.RemoveAll<ITenantProjectionStore>();
        _ = services.AddSingleton<ITenantProjectionStore>(static sp => sp.GetRequiredService<DurableTenantProjectionStore>());
        _ = services.RemoveAll<ITenantProjectionSnapshotStore>();
        _ = services.AddSingleton<ITenantProjectionSnapshotStore>(static sp => sp.GetRequiredService<DurableTenantProjectionStore>());
        RegisterRejectionCheckpointHandlers(services);
        return services;
    }

    // In durable mode every persisted tenant-domain rejection advances the checkpoint without changing state, so it never
    // leaves a gap in front of the next projected event.
    private static void RegisterRejectionCheckpointHandlers(IServiceCollection services) {
        if (services.Any(static s => s.ServiceType == typeof(TenantRejectionCheckpointRegistrationMarker))) {
            return;
        }

        _ = services.AddSingleton<TenantRejectionCheckpointRegistrationMarker>();
        foreach (Type rejection in TenantProjectionTransition.RejectionEventTypes) {
            Type handler = typeof(TenantRejectionCheckpointHandler<>).MakeGenericType(rejection);
            _ = services.AddSingleton(
                typeof(IEventStoreDomainEventHandler<>).MakeGenericType(rejection),
                sp => ActivatorUtilities.CreateInstance(sp, handler));
        }
    }
#endif

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
