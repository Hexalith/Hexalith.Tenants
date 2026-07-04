using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Commands;

[RestRoute(RestVerb.Post, "{tenantId}/disable", ApiScope = "tenants")]
public record DisableTenant(string TenantId) : ICommandContract {
    public static string Domain => TenantIdentity.Domain;

    public static string CommandType => "disable-tenant";

    public string AggregateId => TenantId;
}
