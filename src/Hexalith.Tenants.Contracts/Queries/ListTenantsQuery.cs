using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for listing tenants with cursor-based pagination.
/// </summary>
public sealed class ListTenantsQuery : IQueryContract {
    public static string QueryType => "list-tenants";

    public static string Domain => "tenants";

    public static string ProjectionType => "tenant-index";
}
