using Hexalith.Tenants.Authorization;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Seeds independent native authority for existing HTTP/persistence tests without consulting their projections.</summary>
internal static class GlobalAdministratorTestAuthority
{
    internal static ITenantsGlobalAdministratorAuthority Create(params string[] administrators)
    {
        var source = new MutableGlobalAdministratorSource(administrators.Length == 0 ? ["unrelated-authority-bootstrap"] : administrators);
        return new TenantsGlobalAdministratorAuthority(source, source, TimeProvider.System);
    }
}
