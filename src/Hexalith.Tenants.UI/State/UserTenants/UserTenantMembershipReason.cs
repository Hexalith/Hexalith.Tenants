namespace Hexalith.Tenants.UI.State.UserTenants;

public enum UserTenantMembershipReason
{
    None,
    MissingTargetUser,
    InvalidTargetUser,
    MissingAuthenticatedUser,
    Unauthorized,
    GatewayUnavailable,
    NotModifiedWithoutSnapshot,
    ProjectionDegraded,
    ProjectionStale,
    GatewayFailure,
}
