using Hexalith.EventStore.Client.Queries;
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Queries;

/// <summary>
/// Builds the per-endpoint cursor scope strings that bind a tenant query cursor to the exact
/// endpoint/filter set it was issued for. Composed on the platform <see cref="QueryCursorScope"/>
/// builder (Epic A9) so the canonical strings — and therefore previously-issued cursors — stay
/// byte-for-byte identical to the retired hand-rolled codec.
/// </summary>
internal static class TenantQueryCursorScopes {
    public static string ListTenants(string userId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return QueryCursorScope.Create().Add("user", userId).Build();
    }

    public static string GetTenantUsers(string tenantId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return QueryCursorScope.Create().Add("tenant", tenantId).Build();
    }

    public static string GetUserTenants(string requesterUserId, string targetUserId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(requesterUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetUserId);
        return QueryCursorScope.Create()
            .Add("requester", requesterUserId)
            .Add("target-user", targetUserId)
            .Build();
    }

    public static string GetGlobalAdministrators(string requesterUserId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(requesterUserId);
        return QueryCursorScope.Create().Add("requester", requesterUserId).Build();
    }

    public static string GetTenantAudit(
        string tenantId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        AuditEventCategory? category) {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return QueryCursorScope.Create()
            .Add("tenant", tenantId)
            .Add("from", from)
            .Add("to", to)
            .Add("category", category?.ToString())
            .Build();
    }
}
