namespace Hexalith.Tenants.UI.State.TenantAudit;

public enum TenantAuditSurfaceKind
{
    Loading,
    Ready,
    Empty,
    FilteredEmpty,
    Stale,
    Degraded,
    Unauthorized,
    InvalidCursor,
    ListRefreshed,
    Unavailable,
    Error,
}
