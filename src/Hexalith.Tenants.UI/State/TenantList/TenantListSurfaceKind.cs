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

    /// <summary>
    /// An authoritative whole-set search page returned no visible authorized rows. This is an index or
    /// authorization outcome for one protected page, not a verdict on the operator's filters, and later
    /// pages of the same search may still contain results.
    /// </summary>
    SearchPageEmpty,
}
