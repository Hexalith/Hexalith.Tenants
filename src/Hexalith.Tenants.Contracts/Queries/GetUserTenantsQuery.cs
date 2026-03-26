using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for retrieving tenants a specific user belongs to.
/// </summary>
public sealed class GetUserTenantsQuery : IQueryContract {
    public static string QueryType => "get-user-tenants";

    public static string Domain => "tenants";

    public static string ProjectionType => "tenant-index";
}
