using System.ComponentModel;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.McpCli.Abstractions;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for retrieving users in a specific tenant with their roles.
/// </summary>
[RestRoute(RestVerb.Get, "{tenantId}/users", ApiScope = "tenants")]
[RestQueryBinding(RestQueryBindingSource.Route, "tenantId", RestQueryBindingSource.Route, "tenantId")]
[HexalithQuery(
    "List the members of one tenant with their roles, ordered by user identifier. The caller must be a member of the tenant or a global administrator. Results are paged: pageSize takes 1 to 100 (default 20, larger values are reduced to 100); pass the returned nextCursor as cursor to read the next page.",
    AggregateIdProperty = nameof(TenantId),
    Example = """{"TenantId":"acme"}""")]
public sealed class GetTenantUsersQuery : IQueryContract {
    [HexalithIdentifier]
    [Description("Identifier of the tenant whose members are listed.")]
    public string TenantId { get; init; } = string.Empty;

    [Description("Not used by gateway calls, which page with the pageSize and cursor call arguments. Leave unset.")]
    public string? Cursor { get; init; }

    [Description("Not used by gateway calls, which page with the pageSize and cursor call arguments. Leave unset.")]
    public int PageSize { get; init; }

    public static string QueryType => "get-tenant-users";

    public static string Domain => "tenants";

    public static string ProjectionType => "tenants";
}
