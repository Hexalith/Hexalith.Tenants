using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Commands;

/// <summary>Creates the synthetic tenant for an environment if it does not already exist.</summary>
/// <param name="TenantId">The canonical synthetic tenant identifier.</param>
public record CreateSyntheticTenant(string TenantId) : ICommandContract
{
    /// <summary>Gets the domain that owns the tenant stream.</summary>
    public static string Domain => TenantIdentity.Domain;

    /// <summary>Gets the command discriminator.</summary>
    public static string CommandType => "create-synthetic-tenant";

    /// <summary>Gets the identifier of the tenant stream.</summary>
    public string AggregateId => TenantId;
}
