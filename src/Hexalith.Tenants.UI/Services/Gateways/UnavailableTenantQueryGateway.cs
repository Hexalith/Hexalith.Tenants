using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.UserTenants;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class UnavailableTenantQueryGateway : ITenantQueryGateway {
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

    public Task<UserTenantMembershipSnapshot> GetUserTenantsAsync(
        UserTenantMembershipRequest request,
        UserTenantMembershipSnapshot? previous,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(UserTenantMembershipSnapshot.Unavailable(targetUserId: request.TargetUserId));
    }

    public Task<GlobalAdministratorsSnapshot> GetGlobalAdministratorsAsync(
        GlobalAdministratorsRequest request,
        GlobalAdministratorsSnapshot? previous,
        CancellationToken cancellationToken = default)
        => Task.FromResult(GlobalAdministratorsSnapshot.Unavailable());

    public Task<TenantAuditSnapshot> GetTenantAuditAsync(
        TenantAuditRequest request,
        TenantAuditSnapshot? previous,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(TenantAuditSnapshot.Unavailable(request));
    }
}
