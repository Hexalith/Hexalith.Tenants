using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Contracts.Routing;

namespace Hexalith.Tenants.Server.Routing;

/// <summary>Packaged owner capability enrollment shared by the host and offline construction.</summary>
/// <remarks>The owning repository checks this enrollment against every actual query handler and dispatcher output.</remarks>
public static class TenantCatalogCapabilities
{
    /// <summary>Gets the exact handler queries consumed by each host query handler's declaration.</summary>
    public static IReadOnlyList<RouteContractDeclaration> HandlerQueries => TenantCatalogDeclarations.Queries;

    /// <summary>Gets the custom dispatcher's metadata-only enrollment; no named dispatch capability is granted.</summary>
    public static LegacyProjectionCatalogRegistration Projections { get; } = new(TenantCatalogDeclarations.Projections);
}
