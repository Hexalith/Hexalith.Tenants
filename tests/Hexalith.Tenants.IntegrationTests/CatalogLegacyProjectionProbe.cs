using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

namespace Hexalith.Tenants.IntegrationTests;

internal sealed class CatalogLegacyProjectionProbe : IDomainProjectionHandler
{
    public string Domain => "extra";
    public ProjectionResponse Project(ProjectionRequest request) => throw new NotSupportedException();
}
