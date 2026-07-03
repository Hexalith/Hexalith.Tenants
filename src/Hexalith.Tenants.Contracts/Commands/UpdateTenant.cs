using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Commands;

[RestRoute(RestVerb.Put, "{tenantId}")]
public record UpdateTenant(string TenantId, string Name, string? Description) : ICommandContract {
    public static string Domain => TenantIdentity.Domain;

    public static string CommandType => "update-tenant";

    public string AggregateId => TenantId;
}
