using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for retrieving users in a specific tenant with their roles.
/// </summary>
public sealed class GetTenantUsersQuery : IQueryContract {
    public static string QueryType => "get-tenant-users";

    public static string Domain => "tenants";

    public static string ProjectionType => "tenants";
}
