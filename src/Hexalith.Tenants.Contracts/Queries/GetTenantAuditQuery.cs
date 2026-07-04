using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for tenant audit entry queries.
/// </summary>
[RestRoute(RestVerb.Get, "{tenantId}/audit", ApiScope = "tenants")]
[RestQueryBinding(RestQueryBindingSource.Route, "tenantId", RestQueryBindingSource.Route, "tenantId")]
public sealed class GetTenantAuditQuery : IQueryContract {
    public string TenantId { get; init; } = string.Empty;

    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    public AuditEventCategory? Category { get; init; }

    public string? Cursor { get; init; }

    public int PageSize { get; init; }

    public static string QueryType => "get-tenant-audit";

    public static string Domain => "tenants";

    public static string ProjectionType => "tenants";
}
