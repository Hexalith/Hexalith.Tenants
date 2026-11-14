using System.Collections.Concurrent;

using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Events;

namespace Hexalith.Tenants.Client.Handlers;

/// <summary>
/// Built-in domain consumer handler that applies tenant events to <see cref="TenantLocalState"/>
/// projections. Implements the platform <see cref="IEventStoreDomainEventHandler{TEvent}"/> (A3) for all
/// tenant event types; the generic subscription/dedup plumbing is provided by the EventStore client SDK.
/// </summary>
/// <remarks>
/// Business events are applied through the pure <see cref="TenantProjectionTransition"/>.
/// Access-only refresh evidence never advances a legacy store's business-state sequence.
#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
/// When the registered <see cref="ITenantProjectionStore"/> also implements <see cref="ITenantProjectionSnapshotStore"/>, deliveries
/// route through its checkpointed write; otherwise the local store keeps its historical skip-older behavior.
#else
/// The local store keeps its historical skip-older behavior.
#endif
/// </remarks>
public class TenantProjectionEventHandler :
    IEventStoreDomainEventHandler<TenantCreated>,
    IEventStoreDomainEventHandler<TenantUpdated>,
    IEventStoreDomainEventHandler<TenantAccessRefreshed>,
    IEventStoreDomainEventHandler<TenantDisabled>,
    IEventStoreDomainEventHandler<TenantEnabled>,
    IEventStoreDomainEventHandler<UserAddedToTenant>,
    IEventStoreDomainEventHandler<UserRemovedFromTenant>,
    IEventStoreDomainEventHandler<UserRoleChanged>,
    IEventStoreDomainEventHandler<TenantConfigurationSet>,
    IEventStoreDomainEventHandler<TenantConfigurationRemoved> {
    private readonly ITenantProjectionStore _store;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _tenantLocks = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantProjectionEventHandler"/> class.
    /// </summary>
    /// <param name="store">The tenant projection store.</param>
    public TenantProjectionEventHandler(ITenantProjectionStore store) {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <inheritdoc/>
    public Task HandleAsync(TenantCreated @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(@event, context, cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantAccessRefreshed @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(@event, context, cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantUpdated @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(@event, context, cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantDisabled @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(@event, context, cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantEnabled @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(@event, context, cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(UserAddedToTenant @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(@event, context, cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(UserRemovedFromTenant @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(@event, context, cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(UserRoleChanged @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(@event, context, cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantConfigurationSet @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(@event, context, cancellationToken);

    /// <inheritdoc/>
    public Task HandleAsync(TenantConfigurationRemoved @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default)
        => ApplyAsync(@event, context, cancellationToken);

    private async Task ApplyAsync(
        IEventPayload @event,
        EventStoreDomainEventContext context,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);

#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
        if (_store is ITenantProjectionSnapshotStore durable) {
            // The durable projection owns identity, compare-and-swap, gaps, and recovery. A durable outcome
            // returns (and is acknowledged); a retryable one throws so the delivery is not acknowledged.
            _ = await durable.ApplyAsync(@event, context, cancellationToken).ConfigureAwait(false);
            return;
        }
#endif

        // Access-only evidence is not a complete TenantLocalState snapshot. Advancing the
        // legacy whole-state sequence here could hide a delayed membership revocation.
        if (@event is TenantAccessRefreshed) return;

        SemaphoreSlim tenantLock = _tenantLocks.GetOrAdd(context.AggregateId, static _ => new SemaphoreSlim(1, 1));
        await tenantLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            TenantLocalState state = await GetOrCreateStateAsync(context.AggregateId, cancellationToken).ConfigureAwait(false);
            if (state.LastEvent is { } lastEvent && lastEvent.LastSequenceNumber > context.SequenceNumber) {
                return;
            }

            TenantLocalState next = TenantProjectionTransition.Apply(
                state,
                context.AggregateId,
                @event,
                new TenantProjectionEventMetadata(
                    context.MessageId,
                    context.SequenceNumber,
                    context.Timestamp,
                    context.CorrelationId));
            await _store.SaveAsync(next, cancellationToken).ConfigureAwait(false);
        }
        finally {
            _ = tenantLock.Release();
        }
    }

    private async Task<TenantLocalState> GetOrCreateStateAsync(string tenantId, CancellationToken cancellationToken) {
        TenantLocalState? state = await _store.GetAsync(tenantId, cancellationToken).ConfigureAwait(false);
        state ??= new TenantLocalState { TenantId = tenantId };

        return state;
    }
}
