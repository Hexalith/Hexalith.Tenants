using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for tenant audit queries (MVP: returns 501 Not Implemented).
/// </summary>
public sealed class GetTenantAuditQuery : IQueryContract {
    public static string QueryType => "get-tenant-audit";

    public static string Domain => "tenants";

    public static string ProjectionType => "tenants";
}
