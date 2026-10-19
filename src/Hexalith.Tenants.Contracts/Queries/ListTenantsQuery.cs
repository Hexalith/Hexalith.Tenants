using System.ComponentModel;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.McpCli.Abstractions;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for listing tenants with cursor-based pagination.
/// </summary>
[RestRoute(RestVerb.Get, "", ApiScope = "tenants")]
[HexalithQuery(
    "List the tenants visible to the caller, ordered by tenant identifier: every tenant for a global administrator, otherwise only the tenants the caller is a member of. Each item gives the tenant identifier, name and status (Active or Disabled). Results are paged: pageSize takes 1 to 100 (default 20, larger values are reduced to 100); pass the returned nextCursor as cursor to read the next page.",
    AggregateId = "index",
    Example = "{}")]
public sealed class ListTenantsQuery : IQueryContract {
    [Description("Not used by gateway calls, which page with the pageSize and cursor call arguments. Leave unset.")]
    public string? Cursor { get; init; }

    [Description("Not used by gateway calls, which page with the pageSize and cursor call arguments. Leave unset.")]
    public int PageSize { get; init; }

    public static string QueryType => "list-tenants";

    public static string Domain => "tenants";

    public static string ProjectionType => "tenant-index";
}
