using Hexalith.EventStore.Contracts.Identity;

namespace Hexalith.Tenants.Contracts.Identity;

public static class TenantIdentity {
    public const string DefaultTenantId = "system";
    public const string Domain = "tenants";
    public const string GlobalAdministratorsAggregateId = "global-administrators";

    public static AggregateIdentity ForTenant(string managedTenantId)
        => new(DefaultTenantId, Domain, managedTenantId);

    public static AggregateIdentity ForGlobalAdministrators()
        => new(DefaultTenantId, Domain, GlobalAdministratorsAggregateId);
}
