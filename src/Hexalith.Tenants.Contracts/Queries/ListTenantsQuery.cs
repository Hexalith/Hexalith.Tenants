using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Rest;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for listing tenants with cursor-based pagination.
/// </summary>
[RestRoute(RestVerb.Get, "", ApiScope = "tenants")]
public sealed class ListTenantsQuery : IQueryContract {
    public string? Cursor { get; init; }

    public int PageSize { get; init; }

    public static string QueryType => "list-tenants";

    public static string Domain => "tenants";

    public static string ProjectionType => "tenant-index";
}
