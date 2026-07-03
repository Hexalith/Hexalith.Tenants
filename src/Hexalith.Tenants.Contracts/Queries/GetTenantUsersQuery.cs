using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Rest;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for retrieving users in a specific tenant with their roles.
/// </summary>
[RestRoute(RestVerb.Get, "{tenantId}/users")]
[RestQueryBinding(RestQueryBindingSource.Route, "tenantId", RestQueryBindingSource.Route, "tenantId")]
public sealed class GetTenantUsersQuery : IQueryContract {
    public string TenantId { get; init; } = string.Empty;

    public string? Cursor { get; init; }

    public int PageSize { get; init; }

    public static string QueryType => "get-tenant-users";

    public static string Domain => "tenants";

    public static string ProjectionType => "tenants";
}
