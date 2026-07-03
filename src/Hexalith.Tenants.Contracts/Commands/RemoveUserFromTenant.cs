using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Commands;

[RestRoute(RestVerb.Post, "{tenantId}/users/{userId}/remove")]
public record RemoveUserFromTenant(string TenantId, string UserId) : ICommandContract {
    public static string Domain => TenantIdentity.Domain;

    public static string CommandType => "remove-user-from-tenant";

    public string AggregateId => TenantId;
}
