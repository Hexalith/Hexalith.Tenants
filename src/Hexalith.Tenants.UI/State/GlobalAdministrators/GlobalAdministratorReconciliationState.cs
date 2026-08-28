using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

/// <summary>Describes the support-safe state required to resume one fixed-scope command reconciliation.</summary>
/// <param name="ActionKind">Fixed-scope action being reconciled.</param>
/// <param name="TargetUserId">Literal target identity, retained only inside the interactive circuit.</param>
/// <param name="MessageId">Opaque command message identifier.</param>
/// <param name="CorrelationId">Opaque command correlation identifier.</param>
/// <param name="LifecycleState">Latest monotonic lifecycle evidence.</param>
internal sealed record GlobalAdministratorReconciliationState(
    GlobalAdministratorActionKind ActionKind,
    string TargetUserId,
    string MessageId,
    string CorrelationId,
    TenantCommandLifecycleState LifecycleState)
{
    /// <summary>Returns a support-safe description without identity or tracking values.</summary>
    /// <returns>A bounded diagnostic description.</returns>
    public override string ToString()
        => $"{nameof(GlobalAdministratorReconciliationState)} {{ ActionKind = {ActionKind}, LifecycleState = {LifecycleState} }}";
}
