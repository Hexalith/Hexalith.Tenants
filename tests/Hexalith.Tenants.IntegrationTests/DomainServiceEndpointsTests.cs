using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Exercises the real Tenants host route composition over in-memory HTTP.</summary>
public sealed class DomainServiceEndpointsTests {
    private const string StateStoreName = "statestore";

    [Fact]
    public async Task Host_ServesProjectionQueryAndOperationalMetadataRoutes() {
        var store = new InMemoryReadModelStore();
        await using var baseFactory = new WebApplicationFactory<TenantBootstrapOptions>();
        await using WebApplicationFactory<TenantBootstrapOptions> factory = baseFactory.WithWebHostBuilder(
            builder => builder.ConfigureServices(services => {
                _ = services.RemoveAll<IReadModelStore>();
                _ = services.AddSingleton<IReadModelStore>(store);
            }));
        using HttpClient client = factory.CreateClient();

        DateTimeOffset timestamp = new(2026, 7, 16, 10, 0, 0, TimeSpan.Zero);
        var created = new TenantCreated("tenant-http", "HTTP Tenant", "route proof", timestamp);
        var projectionRequest = new ProjectionRequest(
            "tenant-http",
            "tenants",
            "tenant-http",
            [
                new ProjectionEventDto(
                    typeof(TenantCreated).FullName!,
                    JsonSerializer.SerializeToUtf8Bytes(created),
                    "json",
                    1,
                    timestamp,
                    "corr-http-project",
                    "event-http-1",
                    "user-http-1"),
            ]);

        using HttpResponseMessage projectionResponse = await client
            .PostAsJsonAsync("/project", projectionRequest)
            .ConfigureAwait(true);
        projectionResponse.EnsureSuccessStatusCode();

        TenantReadModel? detail = store.Snapshot<TenantReadModel>(StateStoreName, "projection:tenants:tenant-http");
        TenantIndexReadModel? index = store.Snapshot<TenantIndexReadModel>(StateStoreName, "projection:tenant-index:singleton");
        TenantAuditReadModel? audit = store.Snapshot<TenantAuditReadModel>(StateStoreName, "audit:tenant-http");
        detail.ShouldNotBeNull().Name.ShouldBe("HTTP Tenant");
        detail.ProjectedAt.ShouldNotBeNull();
        index.ShouldNotBeNull().Tenants.ShouldContainKey("tenant-http");
        index.ProjectedAt.ShouldNotBeNull();
        audit.ShouldNotBeNull().Entries.ShouldHaveSingleItem().EventId.ShouldBe("event-http-1");
        audit.ProjectedAt.ShouldNotBeNull();

        var query = new QueryEnvelope(
            "system",
            ListTenantsQuery.Domain,
            "tenant-index",
            ListTenantsQuery.QueryType,
            [],
            "corr-http-query",
            "global-admin-http",
            isGlobalAdmin: true);
        using HttpResponseMessage queryResponse = await client
            .PostAsJsonAsync("/query", query)
            .ConfigureAwait(true);
        queryResponse.EnsureSuccessStatusCode();
        QueryResult? queryResult = await queryResponse.Content
            .ReadFromJsonAsync<QueryResult>()
            .ConfigureAwait(true);
        queryResult.ShouldNotBeNull().Success.ShouldBeTrue();
        queryResult.ProjectionType.ShouldBe("tenant-index");
        queryResult.GetPayload().ToString().ShouldContain("tenant-http");

        var metadataRequest = new AdminOperationalIndexMetadata.Request(
            [GetTenantQuery.Domain, GetGlobalAdministratorsQuery.Domain]);
        using HttpResponseMessage metadataResponse = await client
            .PostAsJsonAsync("/admin/operational-index-metadata", metadataRequest)
            .ConfigureAwait(true);
        metadataResponse.EnsureSuccessStatusCode();
        AdminOperationalIndexMetadata.Response? metadata = await metadataResponse.Content
            .ReadFromJsonAsync<AdminOperationalIndexMetadata.Response>()
            .ConfigureAwait(true);

        metadata.ShouldNotBeNull().Domains
            .Single(domain => domain.Domain == GetTenantQuery.Domain)
            .QueryTypes.ShouldContain(ListTenantsQuery.QueryType);
        metadata.Domains
            .Single(domain => domain.Domain == GetGlobalAdministratorsQuery.Domain)
            .QueryTypes.ShouldContain(GetGlobalAdministratorsQuery.QueryType);
    }
}
