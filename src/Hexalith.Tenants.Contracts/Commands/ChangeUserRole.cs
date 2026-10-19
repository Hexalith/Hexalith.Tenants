using System.ComponentModel;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.McpCli.Abstractions;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Commands;

[RestRoute(RestVerb.Patch, "{tenantId}/users/{userId}/role", ApiScope = "tenants")]
[HexalithCommand(
    "Change the role of an existing member of an active tenant. Allowed for a global administrator or a TenantOwner of the tenant. A user who is not a member is rejected, and setting the role the member already has changes nothing.",
    Example = """{"TenantId":"acme","UserId":"alice@example.com","NewRole":"TenantOwner"}""")]
public record ChangeUserRole(
    [property: HexalithIdentifier]
    [property: Description("Identifier of the tenant whose member changes role.")]
    string TenantId,
    [property: HexalithIdentifier]
    [property: Description("Identifier of the member whose role changes, exactly as the identity provider issues it in the token subject.")]
    string UserId,
    [property: Description("Role that replaces the member's current role. TenantOwner manages members, roles and configuration; TenantContributor can also update the tenant's name and description; TenantReader can only read the tenant. Unknown is rejected.")]
    TenantRole NewRole) : ICommandContract {
    public static string Domain => TenantIdentity.Domain;

    public static string CommandType => "change-user-role";

    public string AggregateId => TenantId;
}
