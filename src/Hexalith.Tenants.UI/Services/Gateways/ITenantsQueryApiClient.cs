using Hexalith.EventStore.Client.Gateway;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal interface ITenantsQueryApiClient
{
    Task<EventStoreQueryResult<T>> SendAsync<T>(
        TenantsQueryApiRequest request,
        string? ifNoneMatch = null,
        CancellationToken cancellationToken = default);
}
