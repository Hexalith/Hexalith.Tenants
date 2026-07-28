using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TenantUsers;
using Hexalith.Tenants.UI.State.UserTenants;

namespace Hexalith.Tenants.UI.Services.Gateways;

public interface ITenantQueryGateway {
    Task<TenantConfigurationProjectionProof> GetSetConfigurationProjectionProofAsync(
        SetTenantConfiguration request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(TenantConfigurationProjectionProof.Unavailable(request.TenantId));
    }

    Task<TenantConfigurationProjectionProof> GetRemoveConfigurationProjectionProofAsync(
        RemoveTenantConfiguration request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(TenantConfigurationProjectionProof.Unavailable(request.TenantId));
    }

    Task<TenantDetailSnapshot> GetTenantAsync(
        TenantDetailRequest request,
        TenantDetailSnapshot? previous,
        CancellationToken cancellationToken = default);

    Task<TenantUsersSnapshot> GetTenantUsersAsync(
        TenantUsersRequest request,
        TenantUsersSnapshot? previous,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(TenantUsersSnapshot.Unavailable(request.TenantId));
    }

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

    Task<GlobalAdministratorsSnapshot> GetGlobalAdministratorsAsync(
        GlobalAdministratorsRequest request,
        GlobalAdministratorsSnapshot? previous,
        CancellationToken cancellationToken = default);

    Task<TenantAuditSnapshot> GetTenantAuditAsync(
        TenantAuditRequest request,
        TenantAuditSnapshot? previous,
        CancellationToken cancellationToken = default);
}
