using Hexalith.Tenants.UI.State.TenantAudit;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>One retained tenant correction and its aggregate admission.</summary>
internal sealed record TenantCorrectionAttempt(
    string TenantId,
    string MessageId,
    TenantCorrectionPreviewSnapshot Snapshot,
    TenantAggregateCommandLease Lease,
    DateTimeOffset StartedAtUtc)
{
    /// <summary>Matches the retained surface without confusing a changed role with a new attempt.</summary>
    internal bool Matches(TenantCorrectionStartIntent intent)
        => string.Equals(TenantId, intent.TenantScope, StringComparison.Ordinal)
            && string.Equals(Snapshot.OriginalAuditReference, intent.OriginalAuditReference, StringComparison.Ordinal)
            && (!IsTerminal || Snapshot.IntendedRole == intent.IntendedRole);

    /// <summary>Gets whether the attempt has explicit terminal evidence.</summary>
    internal bool IsTerminal => Snapshot.LifecycleState is TenantCommandLifecycleState.Confirmed
        or TenantCommandLifecycleState.Rejected
        or TenantCommandLifecycleState.AlreadyApplied
        or TenantCommandLifecycleState.Failed;
}
