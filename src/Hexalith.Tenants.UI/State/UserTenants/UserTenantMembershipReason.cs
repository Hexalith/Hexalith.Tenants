namespace Hexalith.Tenants.UI.State.UserTenants;

public enum UserTenantMembershipReason
{
    None,
    MissingAuthenticatedUser,
    Unauthorized,
    GatewayUnavailable,
    NotModifiedWithoutSnapshot,
    ProjectionDegraded,
    ProjectionStale,
    GatewayFailure,
}
