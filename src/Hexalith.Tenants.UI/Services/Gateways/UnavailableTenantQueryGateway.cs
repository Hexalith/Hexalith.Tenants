using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.UserTenants;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class UnavailableTenantQueryGateway : ITenantQueryGateway
{
    public Task<TenantDetailSnapshot> GetTenantAsync(
        TenantDetailRequest request,
        TenantDetailSnapshot? previous,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantDetailSnapshot.Unavailable("Tenant query gateway configuration is missing."));

    public Task<TenantListSnapshot> ListTenantsAsync(
        TenantListRequest request,
        TenantListSnapshot? previous,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TenantListSnapshot.Error("Tenant query gateway configuration is missing."));

    public Task<UserTenantMembershipSnapshot> GetMyTenantsAsync(
        UserTenantMembershipRequest request,
        UserTenantMembershipSnapshot? previous,
        CancellationToken cancellationToken = default)
        => Task.FromResult(UserTenantMembershipSnapshot.Unavailable());
}
