using System.ComponentModel;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.McpCli.Abstractions;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for retrieving tenants a specific user belongs to.
/// </summary>
[RestRoute(RestVerb.Get, "~/api/users/{userId}/tenants", ApiScope = "tenants")]
[RestQueryBinding(RestQueryBindingSource.Constant, "index", RestQueryBindingSource.Route, "userId")]
[HexalithQuery(
    "List the tenants one user is a member of, ordered by tenant identifier, with each tenant's name, status and the user's role. Callers always see their own memberships, a global administrator sees any user's, and a TenantOwner sees another user's memberships only in tenants the caller owns. Results are paged: pageSize takes 1 to 100 (default 20, larger values are reduced to 100); pass the returned nextCursor as cursor to read the next page.",
    AggregateId = "index",
    Example = """{"UserId":"alice@example.com"}""")]
public sealed class GetUserTenantsQuery : IQueryContract {
    [HexalithIdentifier]
    [Description("Identifier of the user whose memberships are listed, exactly as the identity provider issues it in the token subject. Always set it; use your own user identifier to list your memberships.")]
    public string UserId { get; init; } = string.Empty;

    [Description("Not used by gateway calls, which page with the pageSize and cursor call arguments. Leave unset.")]
    public string? Cursor { get; init; }

    [Description("Not used by gateway calls, which page with the pageSize and cursor call arguments. Leave unset.")]
    public int PageSize { get; init; }

    public static string QueryType => "get-user-tenants";

    public static string Domain => "tenants";

    public static string ProjectionType => "tenant-index";
}
