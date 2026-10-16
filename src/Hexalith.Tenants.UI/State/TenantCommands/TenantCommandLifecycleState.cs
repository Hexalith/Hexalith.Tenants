namespace Hexalith.Tenants.UI.State.TenantCommands;

public enum TenantCommandLifecycleState {
    Idle,
    Previewed,
    RequestSent,
    Accepted,
    ProjectionPending,
    Confirmed,
    Rejected,
    AlreadyApplied,
    DuplicatePrevented,
    Failed,
    Degraded,
    UnableToVerify,
}
