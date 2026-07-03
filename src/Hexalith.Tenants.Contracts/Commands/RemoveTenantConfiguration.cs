using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Commands;

[RestRoute(RestVerb.Post, "{tenantId}/configuration/{key}/remove")]
public record RemoveTenantConfiguration(string TenantId, string Key) : ICommandContract {
    public static string Domain => TenantIdentity.Domain;

    public static string CommandType => "remove-tenant-configuration";

    public string AggregateId => TenantId;
}
