namespace Hexalith.Tenants.UI.State.TenantCommands;

public static class TenantCommandFlowGuard
{
    /// <summary>
    /// Returns whether the parent AggregateIdentity lock must stay raised for the current lifecycle state.
    /// Retention covers submission through projection reconciliation
    /// (<see cref="TenantCommandLifecycleState.RequestSent"/> /
    /// <see cref="TenantCommandLifecycleState.Accepted"/> /
    /// <see cref="TenantCommandLifecycleState.ProjectionPending"/>).
    /// Recoverable uncertain outcomes release the lock so continue-read-only recovery stays reachable.
    /// </summary>
    /// <param name="state">Current command lifecycle state.</param>
    /// <param name="isSubmitting">Whether a local submit call is still in progress.</param>
    /// <returns><see langword="true"/> when sibling command surfaces for the same aggregate stay locked.</returns>
    public static bool RetainsCommandActivity(TenantCommandLifecycleState state, bool isSubmitting = false)
        => isSubmitting
        || state is TenantCommandLifecycleState.RequestSent
            or TenantCommandLifecycleState.Accepted
            or TenantCommandLifecycleState.ProjectionPending;
}
