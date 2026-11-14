using System.ComponentModel;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Rest;
using Hexalith.McpCli.Abstractions;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Commands;

/// <summary>Publishes current access evidence without changing tenant business state.</summary>
/// <param name="TenantId">The existing tenant whose consumer projections should recover.</param>
[RestRoute(RestVerb.Post, "{tenantId}/refresh-access", ApiScope = "tenants")]
[HexalithCommand(
    "Refresh tenant access projections from authoritative tenant status, memberships and configuration-key names. Requires a current TenantOwner or verified global administrator. Disabled tenants remain disabled. Retry one recovery with the same operation ID; use a fresh ID for a later recovery.",
    PlatformScope = TenantPlatformScopes.Tenants,
    Example = """{"TenantId":"acme"}""")]
public record RefreshTenantAccess(
    [property: HexalithIdentifier]
    [property: Description("Identifier of the existing tenant whose access projections should recover.")]
    string TenantId) : ICommandContract
{
    /// <inheritdoc/>
    public static string Domain => TenantIdentity.Domain;

    /// <inheritdoc/>
    public static string CommandType => "refresh-tenant-access";

    /// <inheritdoc/>
    public string AggregateId => TenantId;
}
