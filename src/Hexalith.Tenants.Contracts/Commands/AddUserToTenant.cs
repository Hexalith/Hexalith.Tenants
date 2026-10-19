using System.ComponentModel;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.McpCli.Abstractions;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Commands;

[RestRoute(RestVerb.Post, "{tenantId}/users/{userId}/add", ApiScope = "tenants")]
[HexalithCommand(
    "Add a user to an active tenant with a role. Allowed for a global administrator or a TenantOwner of the tenant; while a tenant has never had any member, its first member can be added without that role. A user who is already a member is rejected; use change-user-role to change an existing member's role.",
    Example = """{"TenantId":"acme","UserId":"alice@example.com","Role":"TenantContributor"}""")]
public record AddUserToTenant(
    [property: HexalithIdentifier]
    [property: Description("Identifier of the tenant to add the user to.")]
    string TenantId,
    [property: HexalithIdentifier]
    [property: Description("Identifier of the user to add, exactly as the identity provider issues it in the token subject.")]
    string UserId,
    [property: Description("Role to grant. TenantOwner manages members, roles and configuration; TenantContributor can also update the tenant's name and description; TenantReader can only read the tenant. Unknown is rejected.")]
    TenantRole Role) : ICommandContract {
    public static string Domain => TenantIdentity.Domain;

    public static string CommandType => "add-user-to-tenant";

    public string AggregateId => TenantId;
}
