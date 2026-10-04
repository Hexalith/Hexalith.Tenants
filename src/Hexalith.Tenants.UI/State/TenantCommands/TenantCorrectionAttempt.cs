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
    /// <summary>Gets whether bounded admission expired while this attempt remains available for status lookup.</summary>
    internal bool IsExpired { get; init; }

    /// <summary>Gets whether this attempt still prevents a new correction on the tenant.</summary>
    internal bool BlocksAdmission => !IsTerminal && !IsExpired;

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
