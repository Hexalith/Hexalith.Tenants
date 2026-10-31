#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
using System.Reflection;

using Shouldly;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Tenants.Contracts.Routing;
using Hexalith.Tenants.Server.Routing;

namespace Hexalith.Tenants.Contracts.Tests;

public class TenantCatalogTests
{
    [Fact]
    public void EveryRouteHasDigestCoveredSerializedShapesAndExplicitPolicy()
    {
        var operations = CompiledRouteCatalogDeclarations.ReadOperations([typeof(TenantCatalogDeclarations).Assembly]);
        operations.Count.ShouldBe(TenantCatalogDeclarations.Routes.Count);
        foreach (var operation in operations) operation.Digest.ShouldBe(RouteCatalogCodec.ComputeOperationContractDigest(operation));
        operations.Count(o => o.Ingress == "public" && o.Eligibility == "agent-eligible").ShouldBe(14);
        operations.Count(o => o.Ingress == "public" && o.Eligibility == "confirmation-required").ShouldBe(2);
        operations.Single(o => o.RouteId == "global-administrators-get-global-administrators").Eligibility.ShouldBe("UI-only");
        operations.Single(o => o.RouteId == "global-administrators-bootstrap-global-admin").Ingress.ShouldBe("internal");
        typeof(TenantIntentAdapters).Assembly.GetCustomAttributes<RouteCatalogAdapterSourceAttribute>()
            .Select(source => source.SourceType).ShouldContain(typeof(TenantIntentAdapters));
    }
}
#endif
