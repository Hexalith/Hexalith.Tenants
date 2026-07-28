namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

public enum GlobalAdministratorsReason {
    None,
    MissingAuthenticatedUser,
    Unauthorized,
    InvalidCursor,
    GatewayUnavailable,
    NotModifiedWithoutSnapshot,
    ProjectionDegraded,
    ProjectionStale,
    PageRecovered,
    MissingPayload,
    GatewayFailure,
}
