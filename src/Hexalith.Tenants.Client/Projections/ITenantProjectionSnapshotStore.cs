using Hexalith.EventStore.Client.Projections.Checkpointed;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Tenants.Client.Projections;

/// <summary>
/// A durable tenant projection whose state, health, and checkpoint are persisted together and read as one
/// snapshot.
/// </summary>
/// <remarks>
/// Implementations are opt-in additions beside <see cref="ITenantProjectionStore"/>. When the registered store
/// implements this interface, <c>TenantProjectionEventHandler</c> routes deliveries through
/// <see cref="ApplyAsync"/> instead of an unconditional save.
/// </remarks>
public interface ITenantProjectionSnapshotStore {
    /// <summary>
    /// Reads the tenant's state, health, and checkpoint from one persisted record without starting recovery.
    /// </summary>
    /// <param name="tenantId">The managed tenant identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The snapshot, or <see langword="null"/> when the tenant was never created (no delivery committed, or only
    /// events that precede its creation, such as rejections).
    /// </returns>
    Task<TenantProjectionSnapshot?> GetSnapshotAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Offers one delivered tenant event to the durable projection.
    /// </summary>
    /// <param name="event">The decoded event payload.</param>
    /// <param name="context">The processor context carrying the event identity and payload fingerprint.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The durable outcome, which may be acknowledged.</returns>
    /// <exception cref="CheckpointedProjectionUnavailableException">No durable outcome was reached; do not acknowledge.</exception>
    Task<CheckpointedProjectionOutcome> ApplyAsync(
        IEventPayload @event,
        EventStoreDomainEventContext context,
        CancellationToken cancellationToken = default);
}
