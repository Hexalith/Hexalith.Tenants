using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>
/// Provides the server-only typed Tenants REST read surface.
/// </summary>
public interface ITenantsRestQueryClient
{
    /// <summary>Reads the authorized tenant page.</summary>
    /// <param name="query">Authorized list request, including cursor and page size.</param>
    /// <param name="eTag">Previously retained strong validator, without quotes.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>The typed payload or a fixed support-safe failure result.</returns>
    Task<TenantsRestQueryResponse<PaginatedResult<TenantSummary>>> ListTenantsAsync(
        ListTenantsQuery query,
        string? eTag,
        CancellationToken cancellationToken = default);

    /// <summary>Reads one authorized tenant detail.</summary>
    /// <param name="query">Tenant-detail request carrying the literal tenant identifier.</param>
    /// <param name="eTag">Previously retained strong validator, without quotes.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>The typed payload or a fixed support-safe failure result.</returns>
    Task<TenantsRestQueryResponse<TenantDetail>> GetTenantAsync(
        GetTenantQuery query,
        string? eTag,
        CancellationToken cancellationToken = default);

    /// <summary>Reads one authorized tenant-members page.</summary>
    /// <param name="query">Tenant-members request, including tenant, cursor, and page size.</param>
    /// <param name="eTag">Previously retained strong validator, without quotes.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>The typed payload or a fixed support-safe failure result.</returns>
    Task<TenantsRestQueryResponse<PaginatedResult<TenantMember>>> GetTenantUsersAsync(
        GetTenantUsersQuery query,
        string? eTag,
        CancellationToken cancellationToken = default);

    /// <summary>Reads the authorized tenant-memberships page for one user.</summary>
    /// <param name="query">User-memberships request, including user, cursor, and page size.</param>
    /// <param name="eTag">Previously retained strong validator, without quotes.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>The typed payload or a fixed support-safe failure result.</returns>
    Task<TenantsRestQueryResponse<PaginatedResult<UserTenantMembership>>> GetUserTenantsAsync(
        GetUserTenantsQuery query,
        string? eTag,
        CancellationToken cancellationToken = default);

    /// <summary>Reads one authorized tenant audit page.</summary>
    /// <param name="query">Tenant-audit request, including tenant, filters, cursor, and page size.</param>
    /// <param name="eTag">Previously retained strong validator, without quotes.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>The typed payload or a fixed support-safe failure result.</returns>
    Task<TenantsRestQueryResponse<PaginatedResult<TenantAuditEntry>>> GetTenantAuditAsync(
        GetTenantAuditQuery query,
        string? eTag,
        CancellationToken cancellationToken = default);

    /// <summary>Reads the authorized fixed-scope global-administrator page.</summary>
    /// <param name="query">Global-administrator request, including cursor and page size.</param>
    /// <param name="eTag">Previously retained strong validator, without quotes.</param>
    /// <param name="cancellationToken">Caller cancellation token.</param>
    /// <returns>The typed payload or a fixed support-safe failure result.</returns>
    Task<TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>>> GetGlobalAdministratorsAsync(
        GetGlobalAdministratorsQuery query,
        string? eTag,
        CancellationToken cancellationToken = default);
}
