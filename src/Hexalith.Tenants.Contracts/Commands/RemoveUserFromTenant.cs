using System.ComponentModel;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.McpCli.Abstractions;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Commands;

[RestRoute(RestVerb.Post, "{tenantId}/users/{userId}/remove", ApiScope = "tenants")]
[HexalithCommand(
    "Remove a user's membership from an active tenant, revoking the user's role in that tenant. Allowed for a global administrator or a TenantOwner of the tenant; removing a user who is not a member is rejected.",
    Example = """{"TenantId":"acme","UserId":"alice@example.com"}""")]
public record RemoveUserFromTenant(
    [property: HexalithIdentifier]
    [property: Description("Identifier of the tenant to remove the user from.")]
    string TenantId,
    [property: HexalithIdentifier]
    [property: Description("Identifier of the member to remove, exactly as the identity provider issues it in the token subject.")]
    string UserId) : ICommandContract {
    public static string Domain => TenantIdentity.Domain;

    public static string CommandType => "remove-user-from-tenant";

    public string AggregateId => TenantId;
}
