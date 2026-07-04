using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Commands;

[RestRoute(RestVerb.Post, "{tenantId}/users/{userId}/add", ApiScope = "tenants")]
public record AddUserToTenant(string TenantId, string UserId, TenantRole Role) : ICommandContract {
    public static string Domain => TenantIdentity.Domain;

    public static string CommandType => "add-user-to-tenant";

    public string AggregateId => TenantId;
}
