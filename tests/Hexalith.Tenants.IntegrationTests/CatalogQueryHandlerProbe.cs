using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Queries.Handlers;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Preserves an attached declaration while deliberately changing executable routing.</summary>
internal sealed class CatalogQueryHandlerProbe(string domain, string queryType) : IDomainQueryHandler
{
    public static RouteContractDeclaration CatalogDeclaration => GetTenantQueryHandler.CatalogDeclaration;
    public string Domain => domain;
    public string QueryType => queryType;
    public Task<QueryResult> ExecuteAsync(QueryEnvelope query, CancellationToken cancellationToken) => throw new NotSupportedException();
}
