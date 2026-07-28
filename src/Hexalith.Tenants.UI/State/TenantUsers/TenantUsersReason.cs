namespace Hexalith.Tenants.UI.State.TenantUsers;

/// <summary>
/// Identifies a support-safe tenant-members state reason.
/// </summary>
public enum TenantUsersReason
{
    /// <summary>No additional reason applies.</summary>
    None = 0,

    /// <summary>The literal tenant scope is missing.</summary>
    MissingTenantId,

    /// <summary>The read is not authorized.</summary>
    Unauthorized,

    /// <summary>The requested tenant was not found.</summary>
    NotFound,

    /// <summary>A conditional response had no reusable matching snapshot.</summary>
    NotModifiedWithoutSnapshot,

    /// <summary>The protected paging cursor was invalid.</summary>
    InvalidCursor,

    /// <summary>The invalid cursor was discarded and the first page was loaded.</summary>
    ListRefreshed,

    /// <summary>The projection returned degraded evidence.</summary>
    ProjectionDegraded,

    /// <summary>The projection returned stale evidence.</summary>
    ProjectionStale,

    /// <summary>The response payload was missing or malformed.</summary>
    MissingPayload,

    /// <summary>The configured Tenants read service is unavailable.</summary>
    GatewayUnavailable,

    /// <summary>The Tenants read failed operationally.</summary>
    GatewayFailure,
}
