namespace Hexalith.Tenants.UI.State.TenantCommands;

public static class TenantCommandFlowGuard
{
    public static bool RetainsCommandActivity(TenantCommandLifecycleState state, bool isSubmitting = false)
        => isSubmitting
        || state is TenantCommandLifecycleState.RequestSent
            or TenantCommandLifecycleState.Accepted
            or TenantCommandLifecycleState.ProjectionPending;
}
