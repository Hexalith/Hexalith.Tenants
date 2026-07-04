using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Commands;

[RestRoute(RestVerb.Post, "{tenantId}", ApiScope = "tenants")]
public record CreateTenant(string TenantId, string Name, string? Description) : ICommandContract {
    public static string Domain => TenantIdentity.Domain;

    public static string CommandType => "create-tenant";

    public string AggregateId => TenantId;
}
