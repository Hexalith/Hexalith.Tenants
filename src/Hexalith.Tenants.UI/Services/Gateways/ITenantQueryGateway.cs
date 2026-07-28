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

    /// <remarks>
    /// Abstract, like every other member. As a default implementation returning <c>Unavailable</c>, any
    /// gateway, decorator, or test double that forgot to implement it compiled and resolved cleanly while
    /// rendering "members cannot be loaded right now" — indistinguishable from a real outage.
    /// </remarks>
    Task<TenantUsersSnapshot> GetTenantUsersAsync(
        TenantUsersRequest request,
        TenantUsersSnapshot? previous,
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

    Task<GlobalAdministratorsSnapshot> GetGlobalAdministratorsAsync(
        GlobalAdministratorsRequest request,
        GlobalAdministratorsSnapshot? previous,
        CancellationToken cancellationToken = default);

    Task<TenantAuditSnapshot> GetTenantAuditAsync(
        TenantAuditRequest request,
        TenantAuditSnapshot? previous,
        CancellationToken cancellationToken = default);
}
