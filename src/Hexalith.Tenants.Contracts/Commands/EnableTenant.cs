using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Commands;

[RestRoute(RestVerb.Post, "{tenantId}/enable", ApiScope = "tenants")]
public record EnableTenant(string TenantId) : ICommandContract {
    public static string Domain => TenantIdentity.Domain;

    public static string CommandType => "enable-tenant";

    public string AggregateId => TenantId;
}
