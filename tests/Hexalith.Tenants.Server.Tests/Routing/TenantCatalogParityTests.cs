using System.Text.Json;
using Hexalith.EventStore.Client.Discovery;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Routing;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Server.Routing;
using Hexalith.Tenants.Telemetry;
using Microsoft.AspNetCore.Http.HttpResults;
using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Routing;

public class TenantCatalogParityTests
{
    [Fact]
    public void ActualQueryHandlerInventoryAndCustomProjectionMetadataMatchOwnerDeclarations()
    {
        DomainCatalogCapabilities.Queries(typeof(GetTenantQueryHandler).Assembly).Select(RouteCatalogCodec.ComputeDeclarationDigest).Order()
            .ShouldBe(TenantCatalogCapabilities.HandlerQueries.Select(RouteCatalogCodec.ComputeDeclarationDigest).Order());
        var response = new AdminOperationalIndexMetadata.Response(TenantCatalogDeclarations.Projections.Select(p =>
            new AdminOperationalIndexMetadata.DomainMetadata(p.Domain, [], [], [], [], [], [])).ToArray());
        var metadata = LegacyProjectionCatalogRegistration.Bind(response, [ProjectionDispatcher.CatalogRegistration]);
        metadata.DispatchVersion.ShouldBeNull();
        metadata.CatalogFingerprint.ShouldBeNull();
        foreach (var route in TenantCatalogDeclarations.Projections)
        {
            var domain = metadata.Domains.Single(d => d.Domain == route.Domain);
            domain.ProjectionNames.ShouldBe([route.ProjectionType!]);
            domain.LegacyProjectionOutputs![route.ProjectionType!].ShouldBe(route.Execution!.ProjectionOutputs);
            domain.NamedProjectionTypes.ShouldBeNull();
        }
        Should.Throw<InvalidOperationException>(() => LegacyProjectionCatalogRegistration.Bind(response, [ProjectionDispatcher.CatalogRegistration, ProjectionDispatcher.CatalogRegistration]));
    }

    [Theory]
    [InlineData("tenants")]
    [InlineData("global-administrators")]
    public async Task ActualDispatcherOutputsMatchCatalogAndRetainTheirStoredIdentity(string domain)
    {
        var store = new CatalogReadModelStore();
        using var diagnostics = new EventStoreDomainDiagnostics("tenants");
        var dispatcher = new ProjectionDispatcher(store, new TenantTelemetry(diagnostics));
        object payload = domain == "tenants" ? new TenantCreated("tenant-1", "Exact Name", null, DateTimeOffset.UtcNow) : new GlobalAdministratorSet("system", "admin-user");
        var eventDto = new ProjectionEventDto(payload.GetType().FullName!, JsonSerializer.SerializeToUtf8Bytes(payload), "json", 1,
            DateTimeOffset.UtcNow, "correlation", MessageId: "event-1", UserId: "actor-1");
        var result = (await dispatcher.DispatchAsync(new("system", domain, domain == "tenants" ? "tenant-1" : "global-administrators", [eventDto]), TestContext.Current.CancellationToken)).ShouldBeOfType<Ok<ProjectionResponse>>();
        var declared = TenantCatalogDeclarations.Projections.Single(p => p.Domain == domain);
        result.Value!.ProjectionType.ShouldBe(declared.ProjectionType);
        if (domain == "tenants")
        {
            store.Contents.Keys.Order().ShouldBe(new[] { "projection:tenants:tenant-1", "projection:tenant-index:singleton", "audit:tenant-1" }.Order());
            JsonSerializer.Deserialize<TenantReadModel>(store.Contents["projection:tenants:tenant-1"])!.Name.ShouldBe("Exact Name");
            declared.Execution!.ProjectionOutputs.ShouldBe(["tenants", "tenant-index", "tenant-audit"]);
        }
        else
        {
            store.Contents.Keys.Order().ShouldBe(new[] { "projection:global-administrators:singleton", TenantAuditStreams.GlobalAdministratorsKey }.Order());
            JsonSerializer.Deserialize<GlobalAdministratorReadModel>(store.Contents["projection:global-administrators:singleton"])!.Administrators.ShouldContain("admin-user");
            declared.Execution!.ProjectionOutputs.ShouldBe(["global-administrators", "global-administrator-audit"]);
        }
    }
}
