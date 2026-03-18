using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for retrieving a specific tenant's full details.
/// </summary>
public sealed class GetTenantQuery : IQueryContract
{
    public static string QueryType => "get-tenant";

    public static string Domain => "tenants";

    public static string ProjectionType => "tenants";
}
