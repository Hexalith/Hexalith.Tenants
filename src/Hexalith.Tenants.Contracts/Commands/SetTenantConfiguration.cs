using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Commands;

[RestRoute(RestVerb.Put, "{tenantId}/configuration/{key}")]
public record SetTenantConfiguration(string TenantId, string Key, string Value) : ICommandContract {
    public static string Domain => TenantIdentity.Domain;

    public static string CommandType => "set-tenant-configuration";

    public string AggregateId => TenantId;
}
