using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.UI.State.UserTenants;

public sealed record UserTenantMembershipRow(
    string TenantId,
    string Name,
    TenantStatus Status,
    TenantRole Role,
    ReadModelFreshnessState Freshness,
    ProjectionLifecycleState Lifecycle = ProjectionLifecycleState.Unknown) {
    public static UserTenantMembershipRow FromMembership(UserTenantMembership membership) {
        ArgumentNullException.ThrowIfNull(membership);

        return new(
            membership.TenantId,
            membership.Name,
            membership.Status,
            membership.Role,
            ReadModelFreshnessState.Unknown);
    }
}
