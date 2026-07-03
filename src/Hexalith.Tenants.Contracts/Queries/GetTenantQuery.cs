using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Rest;

namespace Hexalith.Tenants.Contracts.Queries;

/// <summary>
/// Query contract for retrieving a specific tenant's full details.
/// </summary>
[RestRoute(RestVerb.Get, "{tenantId}")]
[RestQueryBinding(RestQueryBindingSource.Route, "tenantId", RestQueryBindingSource.Route, "tenantId")]
public sealed class GetTenantQuery : IQueryContract {
    public string TenantId { get; init; } = string.Empty;

    public static string QueryType => "get-tenant";

    public static string Domain => "tenants";

    public static string ProjectionType => "tenants";
}
