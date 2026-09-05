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
    /// <summary>Returns a support-safe description without identity or tracking values.</summary>
    /// <returns>A bounded diagnostic description.</returns>
    public override string ToString()
        => $"{nameof(GlobalAdministratorReconciliationState)} {{ ActionKind = {ActionKind}, LifecycleState = {LifecycleState}, HasGrantPreview = {GrantPreview is not null}, HasRemovePreview = {RemovePreview is not null}, HasCommandEventEvidence = {HasCommandEventEvidence}, IsSubmissionAmbiguous = {IsSubmissionAmbiguous} }}";
}
