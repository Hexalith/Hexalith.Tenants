namespace Hexalith.Tenants.UI.State.TenantAudit;

public enum TenantAuditReason {
    None,
    MissingTenantId,
    InvalidFilters,
    Unauthorized,
    InvalidCursor,
    ListRefreshed,
    GatewayUnavailable,
    NotModifiedWithoutSnapshot,
    ProjectionDegraded,
    ProjectionStale,
    MissingPayload,
    GatewayFailure,
}
