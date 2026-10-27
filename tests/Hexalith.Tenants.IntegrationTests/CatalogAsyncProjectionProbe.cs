using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Tenants.IntegrationTests;

internal sealed class CatalogAsyncProjectionProbe : IAsyncDomainProjectionHandler
{
    public string Domain => "extra";
    public string ProjectionType => "extra";
    public Task<DomainProjectionHandlerResult> ProjectAsync(ProjectionRequest request, string dispatchId, CancellationToken cancellationToken) => throw new NotSupportedException();
}
