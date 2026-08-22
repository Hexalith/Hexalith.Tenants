using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
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

    /// <summary>
    /// Reads authoritative confirmation evidence for a metadata update. Returning the full snapshot keeps the
    /// detail and its projection version from a single read, so confirmation cannot pair stale evidence with
    /// an advanced version. Defaults to unavailable so implementors opt in.
    /// </summary>
    /// <param name="request">Metadata update whose tenant should be re-read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An authoritative tenant detail snapshot, or an unavailable snapshot.</returns>
    Task<TenantDetailSnapshot> GetUpdateMetadataProjectionProofAsync(
        UpdateTenant request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(TenantDetailSnapshot.Unavailable("Tenant metadata projection proof is unavailable."));
    }

    /// <summary>
    /// Reads one unconditional, authorization-scoped tenant detail snapshot for lifecycle submission or
    /// reconciliation proof. Detail, freshness, lifecycle, and projection version come from the same read.
    /// </summary>
    /// <param name="request">Lifecycle intent whose literal tenant id should be read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An authoritative tenant detail snapshot, or an unavailable snapshot.</returns>
    Task<TenantDetailSnapshot> GetLifecycleProjectionProofAsync(
        TenantLifecycleCommandRequest request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(TenantDetailSnapshot.Unavailable("Tenant lifecycle projection proof is unavailable."));
    }

    Task<TenantDetailSnapshot> GetTenantAsync(
        TenantDetailRequest request,
        TenantDetailSnapshot? previous,
        CancellationToken cancellationToken = default);

    /// <summary>Gets one authorization-scoped page of members for a tenant.</summary>
    /// <remarks>
    /// Abstract, like every other member. As a default implementation returning <c>Unavailable</c>, any
    /// gateway, decorator, or test double that forgot to implement it compiled and resolved cleanly while
    /// rendering "members cannot be loaded right now" — indistinguishable from a real outage.
    /// </remarks>
    /// <param name="request">Tenant and opaque paging scope to read.</param>
    /// <param name="previous">Last confirmed snapshot that may be retained only for the same request scope.</param>
    /// <param name="cancellationToken">Cancellation token for the server-side read.</param>
    /// <returns>The fail-closed tenant-members snapshot.</returns>
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
