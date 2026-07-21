namespace Hexalith.Tenants.UI.State.TenantList;

/// <summary>
/// Identifies a support-safe tenant-list state reason or non-blocking notice.
/// </summary>
public enum TenantListReason
{
    /// <summary>No additional reason applies.</summary>
    None,

    /// <summary>The configured tenant query gateway is unavailable.</summary>
    GatewayUnavailable,

    /// <summary>A conditional response arrived without a reusable server snapshot.</summary>
    NotModifiedWithoutSnapshot,

    /// <summary>Projection-backed list data is degraded.</summary>
    ProjectionDegraded,

    /// <summary>One or more row counts could not be authoritatively enriched.</summary>
    RowEnrichmentUnavailable,

    /// <summary>An invalid cursor was discarded and the authorized first page was loaded.</summary>
    ListRefreshed,

    /// <summary>Protected whole-set search is unavailable and the ordinary cursor list is shown.</summary>
    SearchUnavailable,

    /// <summary>Some search candidates could not be verified operationally.</summary>
    SearchPartiallyAvailable,

    /// <summary>Protected search paging was invalidated and restarted from the first raw page.</summary>
    SearchRefreshed,
}
