#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Projections;

namespace Hexalith.Tenants.Client.Handlers;

/// <summary>
/// Durable-mode handler that routes a persisted tenant rejection event through the durable projection so it
/// advances the contiguous checkpoint without changing the projected state.
/// </summary>
/// <remarks>
/// Rejections are stored in the tenant stream with their own sequence. Without this handler the processor
/// acknowledges them as skipped, and the next projected event would always arrive as a gap.
/// </remarks>
/// <typeparam name="TEvent">The rejection event type.</typeparam>
/// <param name="store">The durable tenant projection.</param>
internal sealed class TenantRejectionCheckpointHandler<TEvent>(ITenantProjectionSnapshotStore store) : IEventStoreDomainEventHandler<TEvent>
    where TEvent : IRejectionEvent {
    private readonly ITenantProjectionSnapshotStore _store = store ?? throw new ArgumentNullException(nameof(store));

    /// <inheritdoc/>
    public async Task HandleAsync(TEvent @event, EventStoreDomainEventContext context, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);

        // A durable outcome returns (and is acknowledged); a retryable one throws so the delivery is retried.
        _ = await _store.ApplyAsync(@event, context, cancellationToken).ConfigureAwait(false);
    }
}
#endif
