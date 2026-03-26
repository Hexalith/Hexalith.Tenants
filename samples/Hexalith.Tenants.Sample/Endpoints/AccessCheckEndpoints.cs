using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Sample.Endpoints;

/// <summary>
/// Minimal API endpoints demonstrating projection-based access enforcement.
/// </summary>
public static class AccessCheckEndpoints {
    /// <summary>
    /// Maps the access check endpoint that queries the local tenant projection.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapAccessCheckEndpoints(this IEndpointRouteBuilder endpoints) {
        ArgumentNullException.ThrowIfNull(endpoints);
        _ = endpoints.MapGet("/access/{tenantId}/{userId}", CheckAccessAsync);
        return endpoints;
    }

    /// <summary>
    /// Checks whether a user has access to a tenant based on the local projection.
    /// </summary>
    public static async Task<IResult> CheckAccessAsync(
        string tenantId,
        string userId,
        ITenantProjectionStore store,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(store);
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(userId)) {
            return Results.BadRequest(new { Message = "tenantId and userId are required" });
        }

        TenantLocalState? state = await store.GetAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (state is null) {
            return Results.NotFound(new { TenantId = tenantId, Message = "Tenant not found in local projection" });
        }

        if (state.Status == TenantStatus.Disabled) {
            return Results.Ok(new { TenantId = tenantId, UserId = userId, Access = "denied", Reason = "Tenant is disabled" });
        }

        if (!state.Members.TryGetValue(userId, out TenantRole role)) {
            return Results.Ok(new { TenantId = tenantId, UserId = userId, Access = "denied", Reason = "User is not a member" });
        }

        return Results.Ok(new { TenantId = tenantId, UserId = userId, Access = "granted", Role = role.ToString() });
    }
}
