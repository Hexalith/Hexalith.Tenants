using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

/// <summary>Describes the support-safe state required to resume one fixed-scope command reconciliation.</summary>
/// <param name="ActionKind">Fixed-scope action being reconciled.</param>
/// <param name="TargetUserId">Literal target identity, retained only inside the interactive circuit.</param>
/// <param name="MessageId">Opaque command message identifier.</param>
/// <param name="CorrelationId">Opaque command correlation identifier, when acceptance returned one.</param>
/// <param name="LifecycleState">Latest monotonic lifecycle evidence.</param>
/// <param name="GrantPreview">Complete retained grant preview.</param>
/// <param name="HasCommandEventEvidence">Whether exact-command status proved positive event evidence.</param>
/// <param name="IsSubmissionAmbiguous">Whether transport left request delivery unresolved.</param>
/// <param name="SafeMessageKey">Support-safe localized lifecycle explanation retained across renderer replacement.</param>
/// <param name="SafeRecoveryKey">Support-safe localized recovery retained across renderer replacement.</param>
/// <param name="RemovePreview">Complete retained removal preview.</param>
/// <param name="RejectionCode">Structured rejection code retained with terminal rejection evidence.</param>
internal sealed record GlobalAdministratorReconciliationState(
    GlobalAdministratorActionKind ActionKind,
    string TargetUserId,
    string MessageId,
    string? CorrelationId,
    TenantCommandLifecycleState LifecycleState,
    GlobalAdministratorGrantPreview? GrantPreview = null,
    bool HasCommandEventEvidence = false,
    bool IsSubmissionAmbiguous = false,
    string? SafeMessageKey = null,
    string? SafeRecoveryKey = null,
    GlobalAdministratorRemovePreview? RemovePreview = null,
    string? RejectionCode = null)
{
    /// <summary>Hidden verified committed end sequence retained with the attempt.</summary>
    internal long? CommittedEventSequence { get; init; }

    internal DateTimeOffset? SubmittedAtUtc { get; init; }

    /// <summary>Complete projection that confirmed this exact command before the lease was released.</summary>
    internal GlobalAdministratorsSnapshot? ConfirmedProjectionEvidence { get; init; }

    /// <summary>Exact audit row that initiated this correction, retained only inside the circuit.</summary>
    internal string? SourceAuditReference { get; init; }

    /// <summary>UTC timestamp of the initiating audit row.</summary>
    internal DateTimeOffset? SourceAuditTimestampUtc { get; init; }

    /// <summary>Returns the durable reconciliation shape used by page presentation comparisons.</summary>
    internal GlobalAdministratorReconciliationState WithoutCircuitProofInputs()
        => this with
        {
            SubmittedAtUtc = null,
            CommittedEventSequence = null,
            ConfirmedProjectionEvidence = null,
            SourceAuditReference = null,
            SourceAuditTimestampUtc = null,
        };

    /// <summary>Retains circuit proof inputs for an update to the same command.</summary>
    internal GlobalAdministratorReconciliationState WithCarriedProofInputs(GlobalAdministratorReconciliationState? current)
        => current is not null
            && HasSameCommandIdentity(current, this)
            ? this with
            {
                SubmittedAtUtc = SubmittedAtUtc ?? current.SubmittedAtUtc,
                CommittedEventSequence = CommittedEventSequence ?? current.CommittedEventSequence,
                ConfirmedProjectionEvidence = ConfirmedProjectionEvidence ?? current.ConfirmedProjectionEvidence,
                SourceAuditReference = current.SourceAuditReference,
                SourceAuditTimestampUtc = current.SourceAuditTimestampUtc,
            }
            : this;

    /// <summary>Checks whether a reconciliation update still describes the same command and source evidence.</summary>
    /// <param name="current">
    /// Retained lease state. A null <c>CorrelationId</c> is tolerated only on this side, so a later
    /// acceptance can supply the correlation. Source-audit null tolerance does not apply here: a null
    /// <c>SourceAuditReference</c> or <c>SourceAuditTimestampUtc</c> on <paramref name="current"/> fails
    /// the match when <paramref name="next"/> carries that field.
    /// </param>
    /// <param name="next">
    /// Proposed update. A null <c>SourceAuditReference</c> or <c>SourceAuditTimestampUtc</c> is tolerated
    /// only on this side, so a writer that omits source evidence can still match the retained lease. A
    /// null <c>CorrelationId</c> on this side is not tolerated once <paramref name="current"/> has one.
    /// </param>
    /// <returns><see langword="true"/> when the retained command and the tolerated identity fields still match.</returns>
    internal static bool HasSameCommandIdentity(
        GlobalAdministratorReconciliationState current,
        GlobalAdministratorReconciliationState next)
        => current.ActionKind == next.ActionKind
            && string.Equals(current.TargetUserId, next.TargetUserId, StringComparison.Ordinal)
            && string.Equals(current.MessageId, next.MessageId, StringComparison.Ordinal)
            && (current.CorrelationId is null
                || string.Equals(current.CorrelationId, next.CorrelationId, StringComparison.Ordinal))
            && Equals(current.GrantPreview, next.GrantPreview)
            && Equals(current.RemovePreview, next.RemovePreview)
            && (next.SourceAuditReference is null
                || string.Equals(current.SourceAuditReference, next.SourceAuditReference, StringComparison.Ordinal))
            && (next.SourceAuditTimestampUtc is null
                || current.SourceAuditTimestampUtc == next.SourceAuditTimestampUtc);

    /// <summary>Returns a support-safe description without identity or tracking values.</summary>
    /// <returns>A bounded diagnostic description.</returns>
    public override string ToString()
        => $"{nameof(GlobalAdministratorReconciliationState)} {{ ActionKind = {ActionKind}, LifecycleState = {LifecycleState}, HasGrantPreview = {GrantPreview is not null}, HasRemovePreview = {RemovePreview is not null}, HasCommandEventEvidence = {HasCommandEventEvidence}, IsSubmissionAmbiguous = {IsSubmissionAmbiguous} }}";
}
