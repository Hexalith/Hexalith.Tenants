#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE
using System.Reflection;
using System.Security.Claims;

using Shouldly;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.McpCli.Abstractions;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Queries;
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
        adapters.Count.ShouldBe(14);
        adapters.Select(adapter => adapter.Declaration.Route.RouteId).Order(StringComparer.Ordinal)
            .ShouldBe(TenantCatalogDeclarations.Commands.Select(command => command.Route.RouteId).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EveryRouteHasDigestCoveredSerializedShapesAndExplicitPolicy()
    {
        var operations = CompiledRouteCatalogDeclarations.ReadOperations([typeof(TenantCatalogDeclarations).Assembly]);
        operations.Count.ShouldBe(TenantCatalogDeclarations.Routes.Count);
        foreach (var operation in operations) operation.Digest.ShouldBe(RouteCatalogCodec.ComputeOperationContractDigest(operation));
        operations.Count(o => o.Ingress == "public" && o.Eligibility == "confirmation-required").ShouldBe(2);
        operations.Single(o => o.RouteId == "global-administrators-get-global-administrators").Eligibility.ShouldBe("UI-only");
        operations.Single(o => o.RouteId == "global-administrators-bootstrap-global-admin").Ingress.ShouldBe("internal");
        var synthetic = operations.Single(o => o.RouteId == "tenants-create-synthetic-tenant");
        synthetic.Ingress.ShouldBe("internal");
        var agent = new ClaimsPrincipal(new ClaimsIdentity([new Claim(RouteCatalogSurfacePolicy.ClaimType, "agent")], "test"));
        RouteCatalogSurfacePolicy.Allows(synthetic, agent).ShouldBeFalse();
        typeof(TenantIntentAdapters).Assembly.GetCustomAttributes<RouteCatalogAdapterSourceAttribute>()
            .Select(source => source.SourceType).ShouldContain(typeof(TenantIntentAdapters));
    }

    [Fact]
    public void AgentEligibleRoutesMatchTheDecoratedCommandsAndQueries()
    {
        Assembly owner = typeof(TenantCatalogDeclarations).Assembly;
        var agentRouteIds = CompiledRouteCatalogDeclarations.ReadOperations([owner])
            .Where(operation => operation.Ingress == "public" && operation.Eligibility == "agent-eligible")
            .Select(operation => operation.RouteId).ToHashSet(StringComparer.Ordinal);
        var agentRoutes = TenantCatalogDeclarations.Routes.Where(route => agentRouteIds.Contains(route.RouteId)).ToArray();
        var commands = agentRoutes.Where(route => route.Kind == "command")
            .Select(route => route.Execution!.HandlerType).Order(StringComparer.Ordinal).ToArray();
        var queries = agentRoutes.Where(route => route.Kind == "query")
            .Select(route => route.Execution!.HandlerType).Order(StringComparer.Ordinal).ToArray();

        agentRouteIds.Count.ShouldBe(15);
        commands.Length.ShouldBe(10);
        commands.ShouldContain(typeof(RefreshTenantAccess).FullName);
        commands.ShouldBe(owner.DefinedTypes.Where(type => type.IsDefined(typeof(HexalithCommandAttribute)))
            .Select(type => type.FullName).Order(StringComparer.Ordinal));
        queries.Length.ShouldBe(5);
        queries.ShouldBe(owner.DefinedTypes.Where(type => type.IsDefined(typeof(HexalithQueryAttribute)))
            .Select(type => type.FullName).Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("ui", true)]
    [InlineData("agent", false)]
    [InlineData("service", false)]
    public void GlobalAdministratorAuditAdmitsOnlyTheUiSurface(string surface, bool allowed)
    {
        string routeId = GetGlobalAdministratorAuditQuery.Domain + "-" + GetGlobalAdministratorAuditQuery.QueryType;
        var audit = CompiledRouteCatalogDeclarations.ReadOperations([typeof(TenantCatalogDeclarations).Assembly])
            .Single(operation => operation.RouteId == routeId);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(RouteCatalogSurfacePolicy.ClaimType, surface)], "test"));

        audit.Ingress.ShouldBe("public");
        RouteCatalogSurfacePolicy.Allows(audit, principal).ShouldBe(allowed);
        RouteCatalogSurfacePolicy.Allows(audit, principal, verifiedOriginatingProof: true).ShouldBe(allowed);
        audit.Eligibility.ShouldBe("UI-only");
    }
}
#endif
