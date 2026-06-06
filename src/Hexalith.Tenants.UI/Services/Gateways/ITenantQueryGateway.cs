using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.UserTenants;

namespace Hexalith.Tenants.UI.Services.Gateways;

public interface ITenantQueryGateway
{
    Task<TenantDetailSnapshot> GetTenantAsync(
        TenantDetailRequest request,
        TenantDetailSnapshot? previous,
        CancellationToken cancellationToken = default);

    Task<TenantListSnapshot> ListTenantsAsync(
        TenantListRequest request,
        TenantListSnapshot? previous,
        CancellationToken cancellationToken = default);

    Task<UserTenantMembershipSnapshot> GetMyTenantsAsync(
        UserTenantMembershipRequest request,
        UserTenantMembershipSnapshot? previous,
        CancellationToken cancellationToken = default);

    Task<UserTenantMembershipSnapshot> GetUserTenantsAsync(
        UserTenantMembershipRequest request,
        UserTenantMembershipSnapshot? previous,
        CancellationToken cancellationToken = default);
}
