namespace Hexalith.Tenants.UI.State.TenantUsers;

/// <summary>
/// Identifies the non-collapsing tenant-members surface state.
/// </summary>
public enum TenantUsersSurfaceKind
{
    /// <summary>The first page has not resolved.</summary>
    Loading = 0,

    /// <summary>Authorized rows are available.</summary>
    Ready,

    /// <summary>The authorized page is empty.</summary>
    Empty,

    /// <summary>Rows are available but projection evidence is stale.</summary>
    Stale,

    /// <summary>Applicable last-confirmed rows are retained under degraded evidence.</summary>
    Degraded,

    /// <summary>Rows are readable but projection freshness cannot be established.</summary>
    Unknown,

    /// <summary>The read is not authorized.</summary>
    Unauthorized,

    /// <summary>The requested tenant was not found.</summary>
    NotFound,

    /// <summary>The request or protected cursor is invalid.</summary>
    Invalid,

    /// <summary>The configured Tenants read dependency is unavailable.</summary>
    Unavailable,

    /// <summary>The read failed operationally.</summary>
    Error,
}
