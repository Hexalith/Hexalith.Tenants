using Hexalith.Tenants.Contracts.Identity;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>
/// Builds AggregateIdentity-shaped admission keys for Tenants UI command locking without constructing
/// EventStore <c>AggregateIdentity</c> values that reject some caller-supplied tenant ids.
/// </summary>
public static class TenantCommandAggregateLock
{
    /// <summary>
    /// Returns the circuit lock key for a tenant aggregate.
    /// </summary>
    /// <param name="managedTenantId">Caller-supplied tenant aggregate id.</param>
    /// <returns>An AggregateIdentity-shaped ordinal lock key.</returns>
    public static string ForTenant(string managedTenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(managedTenantId);
        return $"{TenantIdentity.DefaultTenantId}:{TenantIdentity.Domain}:{managedTenantId}";
    }

    /// <summary>
    /// Returns the circuit lock key for the global-administrators aggregate.
    /// </summary>
    /// <returns>An AggregateIdentity-shaped ordinal lock key.</returns>
    public static string ForGlobalAdministrators()
        => $"{TenantIdentity.DefaultTenantId}:{TenantIdentity.GlobalAdministratorsDomain}:{TenantIdentity.GlobalAdministratorsAggregateId}";
}
