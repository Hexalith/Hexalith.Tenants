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
    public void CompiledRoutesAndAdaptersMatchTheCompleteOwnerInventory()
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "tools/catalog-package-consumer/approved-route-ids.txt"))) root = root.Parent;
        root.ShouldNotBeNull();
        string[] approved = File.ReadAllLines(Path.Combine(root.FullName, "tools/catalog-package-consumer/approved-route-ids.txt"));
        CompiledRouteCatalogDeclarations.Read([typeof(TenantCatalogDeclarations).Assembly])
            .Select(route => route.RouteId).Order(StringComparer.Ordinal).ShouldBe(approved.Order(StringComparer.Ordinal));
        var adapters = TenantIntentAdapters.Create();
        adapters.Count.ShouldBe(12);
        adapters.Select(adapter => adapter.Declaration.Route.RouteId).Order(StringComparer.Ordinal)
            .ShouldBe(TenantCatalogDeclarations.Commands.Select(command => command.Route.RouteId).Order(StringComparer.Ordinal));
    }

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
