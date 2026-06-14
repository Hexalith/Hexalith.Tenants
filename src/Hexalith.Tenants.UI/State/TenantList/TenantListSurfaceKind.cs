namespace Hexalith.Tenants.UI.State.TenantList;

public enum TenantListSurfaceKind {
    Loading,
    Ready,
    Empty,
    FilteredEmpty,
    Error,
    Unauthorized,
    Stale,
    Degraded,
}
