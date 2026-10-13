using Hexalith.EventStore.Client.Queries;
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Queries;

/// <summary>
/// Builds the per-endpoint cursor scope strings that bind a tenant query cursor to the exact
/// endpoint/filter set it was issued for. Composed on the platform <see cref="QueryCursorScope"/>
/// builder (Epic A9). Ordinary endpoint scopes retain compatibility; combined system audit scopes
/// bind the separated storage and paging generation.
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
        string requesterUserId,
        string tenantId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        AuditEventCategory? category,
        string? stream = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(requesterUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        QueryCursorScope scope = QueryCursorScope.Create()
            .Add("requester", requesterUserId)
            .Add("tenant", tenantId)
            .Add("from", from)
            .Add("to", to)
            .Add("category", category?.ToString());
        if (stream is not null)
        {
            scope.Add("stream", stream);
        }
        else if (tenantId == "system")
        {
            scope.Add("audit-generation", "separated-v1");
        }
        return scope.Build();
    }
}
