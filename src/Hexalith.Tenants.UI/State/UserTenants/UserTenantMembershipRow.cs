using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Tenants.UI.State.UserTenants;

public sealed record UserTenantMembershipRow(
    string TenantId,
    string Name,
    TenantStatus Status,
    TenantRole Role,
    ReadModelFreshnessState Freshness) {
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
