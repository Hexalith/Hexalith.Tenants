using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Rest;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for retrieving tenants a specific user belongs to.
/// </summary>
[RestRoute(RestVerb.Get, "~/api/users/{userId}/tenants")]
[RestQueryBinding(RestQueryBindingSource.Constant, "index", RestQueryBindingSource.Route, "userId")]
public sealed class GetUserTenantsQuery : IQueryContract {
    public string UserId { get; init; } = string.Empty;

    public string? Cursor { get; init; }

    public int PageSize { get; init; }

    public static string QueryType => "get-user-tenants";

    public static string Domain => "tenants";

    public static string ProjectionType => "tenant-index";
}
