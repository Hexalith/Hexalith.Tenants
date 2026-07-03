#pragma warning disable CA2007

extern alias TenantsApi;

using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

public sealed class TenantsApiGeneratedControllerTests
{
    private const string JwtAudience = "hexalith-eventstore";
    private const string JwtIssuer = "hexalith-dev";
    private const string JwtSigningKey = "this-is-a-generated-api-test-signing-key-minimum-32-chars";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task ListTenants_requires_authorization_before_gateway_submission()
    {
        CapturingEventStoreGatewayClient gateway = new();
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        gateway.SubmittedQueries.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListTenants_generated_route_submits_index_query_and_forwards_freshness_headers()
    {
        DateTimeOffset servedAt = DateTimeOffset.Parse("2026-07-03T05:00:00Z", CultureInfo.InvariantCulture);
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueQueryResult(
            new PaginatedResult<TenantSummary>(
                [new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)],
                "next-cursor",
                true),
            eTag: "index-etag-2",
            metadata: new QueryResponseMetadata(
                ETag: "index-etag-2",
                IsStale: true,
                ProjectionVersion: "index-v2",
                ServedAt: servedAt));
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants?cursor=opaque&pageSize=25");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull().Tag.ShouldBe("\"index-etag-2\"");
        response.Headers.GetValues("X-Hexalith-Projection-Version").ShouldHaveSingleItem().ShouldBe("index-v2");
        response.Headers.GetValues("X-Hexalith-Served-At").ShouldHaveSingleItem().ShouldBe(servedAt.ToString("O", CultureInfo.InvariantCulture));
        response.Headers.GetValues("X-Hexalith-Is-Stale").ShouldHaveSingleItem().ShouldBe("true");

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().ShouldBe(1);
        body.GetProperty("items")[0].GetProperty("tenantId").GetString().ShouldBe("tenant.alpha");

        SubmittedQuery query = gateway.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(ListTenantsQuery.Domain);
        query.Request.AggregateId.ShouldBe("index");
        query.Request.EntityId.ShouldBeNull();
        query.Request.QueryType.ShouldBe(ListTenantsQuery.QueryType);
        query.Request.ProjectionType.ShouldBe(ListTenantsQuery.ProjectionType);
        query.IfNoneMatch.ShouldBeNull();
        JsonElement payload = query.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("cursor").GetString().ShouldBe("opaque");
        payload.GetProperty("pageSize").GetInt32().ShouldBe(25);
    }

    [Fact]
    public async Task UserTenants_generated_absolute_route_submits_index_query_for_target_user_entity()
    {
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueQueryResult(new PaginatedResult<UserTenantMembership>([], null, false));
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/users/user.alpha/tenants?pageSize=12");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SubmittedQuery query = gateway.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetUserTenantsQuery.Domain);
        query.Request.AggregateId.ShouldBe("index");
        query.Request.EntityId.ShouldBe("user.alpha");
        query.Request.QueryType.ShouldBe(GetUserTenantsQuery.QueryType);
        query.Request.ProjectionType.ShouldBe(GetUserTenantsQuery.ProjectionType);
        query.Request.Payload.ShouldNotBeNull().GetProperty("pageSize").GetInt32().ShouldBe(12);
    }

    [Fact]
    public async Task GlobalAdministrators_generated_absolute_route_submits_fixed_platform_scope_query()
    {
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueQueryResult(new PaginatedResult<GlobalAdministratorSummary>([], null, false));
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/global-administrators?pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SubmittedQuery query = gateway.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetGlobalAdministratorsQuery.Domain);
        query.Request.AggregateId.ShouldBe("global-administrators");
        query.Request.EntityId.ShouldBe("global-administrators");
        query.Request.QueryType.ShouldBe(GetGlobalAdministratorsQuery.QueryType);
        query.Request.ProjectionType.ShouldBe(GetGlobalAdministratorsQuery.ProjectionType);
    }

    [Fact]
    public async Task TenantAudit_generated_route_submits_tenant_scoped_filter_payload()
    {
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueQueryResult(new PaginatedResult<TenantAuditEntry>([], null, false));
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync(
            "/api/tenants/tenant.alpha/audit?category=Access&cursor=audit-cursor&pageSize=30");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SubmittedQuery query = gateway.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetTenantAuditQuery.Domain);
        query.Request.AggregateId.ShouldBe("tenant.alpha");
        query.Request.EntityId.ShouldBe("tenant.alpha");
        query.Request.QueryType.ShouldBe(GetTenantAuditQuery.QueryType);
        query.Request.ProjectionType.ShouldBe(GetTenantAuditQuery.ProjectionType);
        JsonElement payload = query.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("category").GetString().ShouldBe(nameof(AuditEventCategory.Access));
        payload.GetProperty("cursor").GetString().ShouldBe("audit-cursor");
        payload.GetProperty("pageSize").GetInt32().ShouldBe(30);
    }

    [Fact]
    public async Task Generated_query_route_preserves_if_none_match_and_returns_not_modified_with_freshness_headers()
    {
        DateTimeOffset servedAt = DateTimeOffset.Parse("2026-07-03T05:15:00Z", CultureInfo.InvariantCulture);
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueNotModified(
            "index-etag-2",
            new QueryResponseMetadata(
                ETag: "index-etag-2",
                IsNotModified: true,
                IsStale: false,
                ProjectionVersion: "index-v2",
                ServedAt: servedAt));
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/tenants?pageSize=25");
        request.Headers.IfNoneMatch.ParseAdd("\"index-etag-1\"");

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        response.Headers.ETag.ShouldNotBeNull().Tag.ShouldBe("\"index-etag-2\"");
        response.Headers.GetValues("X-Hexalith-Projection-Version").ShouldHaveSingleItem().ShouldBe("index-v2");
        response.Headers.GetValues("X-Hexalith-Served-At").ShouldHaveSingleItem().ShouldBe(servedAt.ToString("O", CultureInfo.InvariantCulture));
        response.Headers.GetValues("X-Hexalith-Is-Stale").ShouldHaveSingleItem().ShouldBe("false");
        gateway.SubmittedQueries.ShouldHaveSingleItem().IfNoneMatch.ShouldBe("\"index-etag-1\"");
    }

    private static HttpClient CreateAuthenticatedClient(TenantsApiWebApplicationFactory factory)
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt("test-user"));
        return client;
    }

    private static string CreateJwt(string subject)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: [new Claim("sub", subject), new Claim("permissions", "queries:*")],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class TenantsApiWebApplicationFactory(CapturingEventStoreGatewayClient gateway)
        : WebApplicationFactory<TenantsApi::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _ = builder.UseEnvironment("Development");
            _ = builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EventStore:Authentication:Issuer"] = JwtIssuer,
                    ["EventStore:Authentication:Audience"] = JwtAudience,
                    ["EventStore:Authentication:SigningKey"] = JwtSigningKey,
                });
            });
            _ = builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEventStoreGatewayClient>();
                services.AddSingleton<IEventStoreGatewayClient>(gateway);
            });
        }
    }

    private sealed class CapturingEventStoreGatewayClient : IEventStoreGatewayClient
    {
        private readonly Queue<object> _responses = new();

        public List<SubmittedQuery> SubmittedQueries { get; } = [];

        public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult> SubmitQueryAsync(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
        {
            SubmittedQueries.Add(new SubmittedQuery(request, ifNoneMatch));
            object next = _responses.Dequeue();
            if (next is Exception exception)
            {
                throw exception;
            }

            return Task.FromResult((EventStoreQueryResult)next);
        }

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public void EnqueueQueryResult<T>(
            T payload,
            string? eTag = "etag",
            QueryResponseMetadata? metadata = null)
            => _responses.Enqueue(new EventStoreQueryResult(
                "correlation",
                JsonSerializer.SerializeToElement(payload, JsonOptions),
                IsNotModified: false,
                eTag)
            {
                Metadata = metadata ?? new QueryResponseMetadata(ETag: eTag, IsStale: false),
            });

        public void EnqueueNotModified(string? eTag, QueryResponseMetadata? metadata = null)
            => _responses.Enqueue(new EventStoreQueryResult(null, null, IsNotModified: true, eTag)
            {
                Metadata = metadata ?? new QueryResponseMetadata(ETag: eTag, IsNotModified: true),
            });
    }

    private sealed record SubmittedQuery(SubmitQueryRequest Request, string? IfNoneMatch);
}
