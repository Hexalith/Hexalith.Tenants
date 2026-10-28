using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.Authorization;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;

namespace Hexalith.Tenants.Server.Tests.Support;

/// <summary>Independent authority seam for existing query composition tests, seeded with their existing fixtures.</summary>
internal sealed class SeededGlobalAdministratorAuthority(IReadModelStore store) : ITenantsGlobalAdministratorAuthority
{
    public async Task<bool> IsCurrentGlobalAdministratorAsync(string userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entry = await store.GetAsync<GlobalAdministratorReadModel>(TenantQueryHandlerBase.StateStoreName,
            TenantQueryHandlerBase.GlobalAdminProjectionKey, cancellationToken);
        return entry?.Value?.Administrators.Contains(userId) == true;
    }
}
