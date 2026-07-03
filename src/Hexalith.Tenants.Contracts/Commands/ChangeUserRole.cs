using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Commands;

[RestRoute(RestVerb.Patch, "{tenantId}/users/{userId}/role")]
public record ChangeUserRole(string TenantId, string UserId, TenantRole NewRole) : ICommandContract {
    public static string Domain => TenantIdentity.Domain;

    public static string CommandType => "change-user-role";

    public string AggregateId => TenantId;
}
