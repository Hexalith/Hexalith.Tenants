using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Exercises the real Tenants host route composition over in-memory HTTP.</summary>
public sealed class DomainServiceEndpointsTests {
    private const string StateStoreName = "statestore";

    private const string AppId = "tenants";
    private const string ChannelToken = "tenants-story-5-5-channel";

    [Fact]
    public async Task Host_ServesProjectionQueryAndOperationalMetadataRoutes() {
        var store = new InMemoryReadModelStore();
        store.SeedRaw(
            StateStoreName,
            "projection:global-administrators:singleton",
            new GlobalAdministratorReadModel { Administrators = new HashSet<string>(["global-admin-http"], StringComparer.Ordinal) });
        await using var baseFactory = new WebApplicationFactory<TenantBootstrapOptions>();
        await using WebApplicationFactory<TenantBootstrapOptions> factory = baseFactory.WithWebHostBuilder(
            builder => builder
                .UseSetting("EventStore:DomainService:AppId", AppId)
                .UseSetting(DaprAppChannelToken.ConfigurationKey, ChannelToken)
                .ConfigureServices(services => {
                    _ = services.RemoveAll<IReadModelStore>();
                    _ = services.AddSingleton<IReadModelStore>(store);
                }));
        using HttpClient client = factory.CreateClient();
        IConfiguration configuration = factory.Services.GetRequiredService<IConfiguration>();

        // EventStore Story 5.5: the real host denies a forged internal call before any projection work.
        using (var forged = new HttpRequestMessage(HttpMethod.Post, "/project") {
            Content = JsonContent.Create(new { }),
        }) {
            forged.Headers.Add(DaprAppChannelToken.HeaderName, ChannelToken);
            forged.Headers.Add(EventStoreWorkloadAuthenticationDefaults.DaprCallerHeaderName, "eventstore");
            using HttpResponseMessage forgedResponse = await client.SendAsync(forged).ConfigureAwait(true);
            forgedResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
        }

        client.DefaultRequestHeaders.Add(DaprAppChannelToken.HeaderName, ChannelToken);

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

        using HttpResponseMessage projectionResponse = await SendInternalAsync(
            client,
            "/project",
            projectionRequest,
            Assertion(configuration, EventStoreWorkloadOperations.DomainServiceProject))
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
        using HttpResponseMessage queryResponse = await SendInternalAsync(
            client,
            "/query",
            query,
            Assertion(configuration, EventStoreWorkloadOperations.DomainServiceQuery))
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
        using HttpResponseMessage metadataResponse = await SendInternalAsync(
            client,
            "/admin/operational-index-metadata",
            metadataRequest,
            Assertion(configuration, EventStoreWorkloadOperations.DomainServiceMetadata))
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

    /// <summary>
    /// EventStore Story 5.5 (BS-B): once the bootstrap's <c>GlobalAdministratorSet</c> event is projected through the
    /// real <c>/project</c> route, the bootstrapped administrator's authority verifies from the global-administrators
    /// read model, and nobody else's does. Before the projection, the same administrator is not yet verified.
    /// </summary>
    [Fact]
    public async Task BootstrappedAdministrator_VerifiesFromTheProjectedReadModel() {
        var store = new InMemoryReadModelStore();
        await using var baseFactory = new WebApplicationFactory<TenantBootstrapOptions>();
        await using WebApplicationFactory<TenantBootstrapOptions> factory = baseFactory.WithWebHostBuilder(
            builder => builder
                .UseSetting("EventStore:DomainService:AppId", AppId)
                .UseSetting(DaprAppChannelToken.ConfigurationKey, ChannelToken)
                .ConfigureServices(services => {
                    _ = services.RemoveAll<IReadModelStore>();
                    _ = services.AddSingleton<IReadModelStore>(store);
                }));
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(DaprAppChannelToken.HeaderName, ChannelToken);
        IConfiguration configuration = factory.Services.GetRequiredService<IConfiguration>();
        var bootstrapped = new DomainServiceAdministratorClaim("system", "tenants", "bootstrap-admin", "corr-bootstrap");

        bool verifiedBefore = await VerifyAsync(factory.Services, bootstrapped).ConfigureAwait(true);
        DateTimeOffset timestamp = new(2026, 10, 7, 9, 0, 0, TimeSpan.Zero);
        var set = new GlobalAdministratorSet("system", "bootstrap-admin", "bootstrap-admin", timestamp);
        var projectionRequest = new ProjectionRequest(
            "system",
            "global-administrators",
            "global-administrators",
            [
                new ProjectionEventDto(
                    typeof(GlobalAdministratorSet).FullName!,
                    JsonSerializer.SerializeToUtf8Bytes(set),
                    "json",
                    1,
                    timestamp,
                    "corr-bootstrap",
                    "event-bootstrap-1",
                    "bootstrap-admin"),
            ]);
        using HttpResponseMessage projected = await SendInternalAsync(
            client,
            "/project",
            projectionRequest,
            Assertion(configuration, EventStoreWorkloadOperations.DomainServiceProject)).ConfigureAwait(true);
        projected.EnsureSuccessStatusCode();

        verifiedBefore.ShouldBeFalse();
        (await VerifyAsync(factory.Services, bootstrapped).ConfigureAwait(true)).ShouldBeTrue();
        (await VerifyAsync(factory.Services, bootstrapped with { UserId = "someone-else" }).ConfigureAwait(true)).ShouldBeFalse();
    }

    private static async Task<bool> VerifyAsync(IServiceProvider services, DomainServiceAdministratorClaim claim) {
        AsyncServiceScope scope = services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false)) {
            IDomainServiceAdministratorVerifier verifier = scope.ServiceProvider.GetRequiredService<IDomainServiceAdministratorVerifier>();
            return await verifier.IsCurrentGlobalAdministratorAsync(claim, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static async Task<HttpResponseMessage> SendInternalAsync<TBody>(
        HttpClient client,
        string route,
        TBody body,
        string assertion) {
        using var request = new HttpRequestMessage(HttpMethod.Post, route) {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName, assertion);
        return await client.SendAsync(request).ConfigureAwait(false);
    }

    private static string Assertion(IConfiguration configuration, string operation) {
        DateTime now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor {
            Issuer = configuration["Authentication:JwtBearer:Issuer"],
            Audience = AppId,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(2),
            Claims = new Dictionary<string, object>(StringComparer.Ordinal) {
                [EventStoreWorkloadAuthenticationDefaults.CallerClaimType] = "eventstore",
                [EventStoreWorkloadAuthenticationDefaults.OperationClaimType] = operation,
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Authentication:JwtBearer:SigningKey"]!)),
                SecurityAlgorithms.HmacSha256),
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
