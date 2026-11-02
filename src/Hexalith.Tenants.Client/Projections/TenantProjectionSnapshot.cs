#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
using Hexalith.EventStore.Client.Projections.Checkpointed;

namespace Hexalith.Tenants.Client.Projections;

/// <summary>
/// One consistent read of a durable tenant projection: state, health, and checkpoint from a single record.
/// </summary>
/// <param name="TenantId">The managed tenant identifier that was read.</param>
/// <param name="Health">The persisted projection health; only <see cref="CheckpointedProjectionHealth.Healthy"/> may authorize.</param>
/// <param name="State">A private copy of the projected state, or <see langword="null"/> when it cannot be trusted.</param>
/// <param name="Checkpoint">The last contiguous tenant-stream sequence applied to <paramref name="State"/>.</param>
/// <param name="RecordVersion">The persisted record version the snapshot was read from.</param>
public sealed record TenantProjectionSnapshot(
    string TenantId,
    CheckpointedProjectionHealth Health,
    TenantLocalState? State,
    long Checkpoint,
    long RecordVersion) {
    /// <summary>Gets a value indicating whether the snapshot is healthy and carries state for its tenant.</summary>
    public bool IsHealthy
        => Health == CheckpointedProjectionHealth.Healthy
        && State is not null
        && string.Equals(State.TenantId, TenantId, StringComparison.Ordinal);
}
#endif
