using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>
/// Provides the server-only typed Tenants REST read surface.
/// </summary>
public interface ITenantsRestQueryClient
{
    /// <summary>Reads the authorized tenant page.</summary>
    Task<TenantsRestQueryResponse<PaginatedResult<TenantSummary>>> ListTenantsAsync(
        ListTenantsQuery query,
        string? eTag,
        CancellationToken cancellationToken = default);

    /// <summary>Reads one authorized tenant detail.</summary>
    Task<TenantsRestQueryResponse<TenantDetail>> GetTenantAsync(
        GetTenantQuery query,
        string? eTag,
        CancellationToken cancellationToken = default);

    /// <summary>Reads one authorized tenant-members page.</summary>
    Task<TenantsRestQueryResponse<PaginatedResult<TenantMember>>> GetTenantUsersAsync(
        GetTenantUsersQuery query,
        string? eTag,
        CancellationToken cancellationToken = default);

    /// <summary>Reads the authorized tenant-memberships page for one user.</summary>
    Task<TenantsRestQueryResponse<PaginatedResult<UserTenantMembership>>> GetUserTenantsAsync(
        GetUserTenantsQuery query,
        string? eTag,
        CancellationToken cancellationToken = default);

    /// <summary>Reads one authorized tenant audit page.</summary>
    Task<TenantsRestQueryResponse<PaginatedResult<TenantAuditEntry>>> GetTenantAuditAsync(
        GetTenantAuditQuery query,
        string? eTag,
        CancellationToken cancellationToken = default);

    /// <summary>Reads the authorized fixed-scope global-administrator page.</summary>
    Task<TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>>> GetGlobalAdministratorsAsync(
        GetGlobalAdministratorsQuery query,
        string? eTag,
        CancellationToken cancellationToken = default);
}
