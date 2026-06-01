#pragma warning disable CA2007

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.EventStore.Server.Pipeline.Queries;
using Hexalith.EventStore.Server.Queries;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Contracts.Serialization;
using Hexalith.Tenants.Queries;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

public class TenantsQueryControllerIntegrationTests {
    // Test-only JWT constants. MUST match appsettings.Development.json — do not copy to production configs.
    private const string JwtAudience = "hexalith-tenants";
    private const string JwtIssuer = "hexalith-dev";
    private const string JwtSigningKey = "this-is-a-development-signing-key-minimum-32-chars";
    private const string SmokeJwtAudience = "hexalith-tenants";
    private const string SmokeJwtIssuer = "https://identity.smoke.example.test/realms/hexalith";
    private const string SmokeJwtSigningKey = "this-is-a-smoke-test-signing-key-minimum-32-chars";
    private static readonly JsonSerializerOptions s_queryJsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new TenantStatusJsonConverter(), new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task ListTenants_returns_401_when_authorization_header_is_missing() {
        IQueryRouter router = CreateRouter(
            "list-tenants",
            new QueryRouterResult(true, JsonSerializer.SerializeToElement(new { items = Array.Empty<object>() }), false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListTenants_returns_200_when_authorization_header_has_valid_jwt() {
        JsonElement payload = JsonSerializer.SerializeToElement(new { items = Array.Empty<object>(), cursor = (string?)null, hasMore = false });
        IQueryRouter router = CreateRouter(
            "list-tenants",
            new QueryRouterResult(true, payload, false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        using HttpClient client = CreateJwtClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.TryGetProperty("items", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task ListTenants_returns_standard_paginated_tenant_summary_shape_and_dispatches_index_query() {
        JsonElement payload = JsonSerializer.SerializeToElement(new {
            items = new[] { new { tenantId = "tenant-001", name = "Tenant One", status = "Disabled" } },
            cursor = (string?)null,
            hasMore = false,
        });
        List<SubmitQuery> routedQueries = [];
        IQueryRouter router = CreateCapturingRouter(
            ListTenantsQuery.QueryType,
            new QueryRouterResult(true, payload, false, ProjectionType: "tenant-index"),
            routedQueries);

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("items").GetArrayLength().ShouldBe(1);
        JsonElement firstItem = result.GetProperty("items")[0];
        firstItem.GetProperty("tenantId").GetString().ShouldBe("tenant-001");
        firstItem.GetProperty("name").GetString().ShouldBe("Tenant One");
        firstItem.GetProperty("status").GetString().ShouldBe("Disabled");
        result.GetProperty("cursor").ValueKind.ShouldBe(JsonValueKind.Null);
        result.GetProperty("hasMore").GetBoolean().ShouldBeFalse();

        SubmitQuery query = routedQueries.Single();
        query.Tenant.ShouldBe("system");
        query.Domain.ShouldBe(ListTenantsQuery.Domain);
        query.AggregateId.ShouldBe("index");
        query.QueryType.ShouldBe(ListTenantsQuery.QueryType);
        query.EntityId.ShouldBe("test-user");
        query.ProjectionType.ShouldBe(TenantProjectionRouting.ActorTypeName);
        ReadPayloadPageSize(query).ShouldBe(TenantQueryPaginationPolicy.StandardDefaultPageSize);
    }

    [Theory]
    [InlineData("", TenantQueryPaginationPolicy.StandardDefaultPageSize)]
    [InlineData("?pageSize=25", 25)]
    [InlineData("?pageSize=0", TenantQueryPaginationPolicy.StandardDefaultPageSize)]
    [InlineData("?pageSize=-5", TenantQueryPaginationPolicy.StandardDefaultPageSize)]
    [InlineData("?pageSize=101", TenantQueryPaginationPolicy.StandardMaximumPageSize)]
    public async Task ListTenants_forwards_bounded_page_size_in_query_payload(string queryString, int expectedPageSize) {
        JsonElement payload = JsonSerializer.SerializeToElement(new { items = Array.Empty<object>(), cursor = (string?)null, hasMore = false });
        List<SubmitQuery> routedQueries = [];
        IQueryRouter router = CreateCapturingRouter(
            ListTenantsQuery.QueryType,
            new QueryRouterResult(true, payload, false, ProjectionType: "tenant-index"),
            routedQueries);

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants" + queryString);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SubmitQuery query = routedQueries.Single();
        ReadPayloadPageSize(query).ShouldBe(expectedPageSize);
        ReadPayloadCursor(query).ShouldBeNull();
    }

    [Fact]
    public async Task ListTenants_returns_standard_empty_page_shape_without_treating_no_matches_as_error() {
        JsonElement payload = JsonSerializer.SerializeToElement(new { items = Array.Empty<object>(), cursor = (string?)null, hasMore = false });
        IQueryRouter router = CreateRouter(
            ListTenantsQuery.QueryType,
            new QueryRouterResult(true, payload, false, ProjectionType: "tenant-index"));

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("items").GetArrayLength().ShouldBe(0);
        result.GetProperty("cursor").ValueKind.ShouldBe(JsonValueKind.Null);
        result.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task ListTenants_returns_200_when_production_like_smoke_jwt_has_allowed_scope() {
        JsonElement payload = JsonSerializer.SerializeToElement(new { items = Array.Empty<object>(), cursor = (string?)null, hasMore = false });
        IQueryRouter router = CreateRouter(
            "list-tenants",
            new QueryRouterResult(true, payload, false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router, useSmokeAuthentication: true);
        using HttpClient client = CreateSmokeJwtClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = await router.Received(1).RouteQueryAsync(
            Arg.Is<SubmitQuery>(q => q != null && q.Tenant == "system"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListTenants_returns_200_when_production_like_smoke_jwt_uses_tenants_json_array_source_claim() {
        // P15: locks that the real JwtBearer middleware + EventStoreClaimsTransformation chain still
        // normalizes source `tenants` claims to `eventstore:tenant=system` under the production-like
        // smoke seam. Without this row, the smoke factory only exercises the direct-claim path.
        JsonElement payload = JsonSerializer.SerializeToElement(new { items = Array.Empty<object>(), cursor = (string?)null, hasMore = false });
        IQueryRouter router = CreateRouter(
            "list-tenants",
            new QueryRouterResult(true, payload, false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router, useSmokeAuthentication: true);
        string token = CreateSmokeJwt(
            "admin-user",
            claims: [new Claim("tenants", JsonSerializer.Serialize(new[] { "system" }))]);
        using HttpClient client = CreateClientWithBearer(factory, token);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = await router.Received(1).RouteQueryAsync(
            Arg.Is<SubmitQuery>(q => q != null && q.Tenant == "system"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("missing-token")]
    [InlineData("malformed-token")]
    [InlineData("invalid-signature")]
    [InlineData("wrong-issuer")]
    [InlineData("wrong-audience")]
    [InlineData("expired-token")]
    public async Task ListTenants_production_like_smoke_authentication_rejects_invalid_tokens_safely(string tokenCase) {
        IQueryRouter router = Substitute.For<IQueryRouter>();

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router, useSmokeAuthentication: true);
        using HttpClient client = factory.CreateClient();
        // P16: expired-token uses AddHours(-1) so the case is unambiguous even if a future refactor
        // moves ClockSkew off TokenValidationParameters; -10 minutes was inside the default 5-minute
        // skew window and could silently flip green for the wrong reason.
        string? token = tokenCase switch {
            "missing-token" => null,
            "malformed-token" => "not-a-jwt",
            "invalid-signature" => CreateSmokeJwt("admin-user", signingKey: "wrong-smoke-test-signing-key-minimum-32-chars"),
            "wrong-issuer" => CreateSmokeJwt("admin-user", issuer: "https://identity.other.example.test/realms/hexalith"),
            "wrong-audience" => CreateSmokeJwt("admin-user", audience: "wrong-audience"),
            "expired-token" => CreateSmokeJwt("admin-user", expires: DateTime.UtcNow.AddHours(-1)),
            _ => throw new InvalidOperationException($"Unknown token case '{tokenCase}'."),
        };

        if (token is not null) {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        // P3: a 401 from the JWT bearer handler must produce a ProblemDetails-shaped body so an
        // operator triaging the response sees structured failure context. A regression that drops
        // the ProblemDetails shape on 401 would still leave the status correct.
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        // P5: each 401 case must surface a distinct WWW-Authenticate signal so the failure layer is
        // identifiable from the response alone. The handler emits Bearer challenge headers; expired
        // tokens carry an `error_description` mentioning expiration, signature/issuer/audience
        // failures emit `error="invalid_token"`, and missing-token has no error attribute.
        AuthenticationHeaderValue? challenge = response.Headers.WwwAuthenticate.FirstOrDefault();
        switch (tokenCase) {
            case "missing-token":
                _ = challenge.ShouldNotBeNull();
                challenge.Scheme.ShouldBe("Bearer");
                (challenge.Parameter is null || !challenge.Parameter.Contains("error=", StringComparison.Ordinal)).ShouldBeTrue();
                break;
            case "expired-token":
                _ = challenge.ShouldNotBeNull();
                challenge.Scheme.ShouldBe("Bearer");
                _ = challenge.Parameter.ShouldNotBeNull();
                challenge.Parameter.ShouldContain("error=\"invalid_token\"");
                challenge.Parameter.ShouldContain("expired", Case.Insensitive);
                break;
            default:
                _ = challenge.ShouldNotBeNull();
                challenge.Scheme.ShouldBe("Bearer");
                _ = challenge.Parameter.ShouldNotBeNull();
                challenge.Parameter.ShouldContain("error=\"invalid_token\"");
                challenge.Parameter.ShouldNotContain("expired", Case.Insensitive);
                break;
        }

        string body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain(SmokeJwtSigningKey);
        // P4: only the cryptographic-token cases assert token redaction. The "missing-token" row has
        // no token to redact; the "malformed-token" row uses the literal `"not-a-jwt"` placeholder,
        // which could legitimately appear in a future ProblemDetails Detail like "Token does not
        // appear to be a JWT" and produce a confusing false-positive failure. For that case, assert
        // the `Bearer ` prefix is absent so any echo of the raw Authorization header is caught.
        if (tokenCase is "missing-token") {
            // Nothing to redact-check beyond the signing key.
        }
        else if (tokenCase is "malformed-token") {
            body.ShouldNotContain("Bearer ", Case.Insensitive);
        }
        else {
            _ = token.ShouldNotBeNull();
            body.ShouldNotContain(token);
        }

        await router.DidNotReceiveWithAnyArgs().RouteQueryAsync(default!, default);
    }

    [Theory]
    [InlineData("missing-tenant", "principal_not_member")]
    [InlineData("blank-tenant", "principal_not_member")]
    [InlineData("wrong-tenant", "tenant_mismatch")]
    public async Task ListTenants_production_like_smoke_authorization_rejects_tenant_claim_failures_safely(
        string tokenCase,
        string expectedReasonCode) {
        IQueryRouter router = Substitute.For<IQueryRouter>();

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router, useSmokeAuthentication: true);
        string token = tokenCase switch {
            "missing-tenant" => CreateSmokeJwt("admin-user", claims: Array.Empty<Claim>()),
            "blank-tenant" => CreateSmokeJwt("admin-user", claims: [new Claim("eventstore:tenant", " ")]),
            "wrong-tenant" => CreateSmokeJwt("admin-user", claims: [new Claim("eventstore:tenant", "tenant-a")]),
            _ => throw new InvalidOperationException($"Unknown token case '{tokenCase}'."),
        };
        using HttpClient client = CreateClientWithBearer(factory, token);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        string body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain(SmokeJwtSigningKey);
        body.ShouldNotContain(token);
        ProblemDetails? details = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        _ = details.ShouldNotBeNull();
        details.Status.ShouldBe(403);
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe(expectedReasonCode);
        await router.DidNotReceiveWithAnyArgs().RouteQueryAsync(default!, default);
    }

    [Fact]
    public async Task ListTenants_returns_200_when_jwt_uses_tenants_json_array_source_claim() {
        JsonElement payload = JsonSerializer.SerializeToElement(new { items = Array.Empty<object>(), cursor = (string?)null, hasMore = false });
        IQueryRouter router = CreateRouter(
            "list-tenants",
            new QueryRouterResult(true, payload, false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        string token = CreateJwt(
            "admin-user",
            claims: [new Claim("tenants", JsonSerializer.Serialize(new[] { "system" }))]);
        using HttpClient client = CreateClientWithBearer(factory, token);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = await router.Received(1).RouteQueryAsync(
            Arg.Is<SubmitQuery>(q => q != null && q.Tenant == "system"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListTenants_returns_200_when_jwt_uses_tenants_space_delimited_source_claim() {
        JsonElement payload = JsonSerializer.SerializeToElement(new { items = Array.Empty<object>(), cursor = (string?)null, hasMore = false });
        IQueryRouter router = CreateRouter(
            "list-tenants",
            new QueryRouterResult(true, payload, false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        string token = CreateJwt("admin-user", claims: [new Claim("tenants", "system tenant-a")]);
        using HttpClient client = CreateClientWithBearer(factory, token);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _ = await router.Received(1).RouteQueryAsync(
            Arg.Is<SubmitQuery>(q => q != null && q.Tenant == "system"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListTenants_returns_403_when_jwt_tenant_claim_is_missing() {
        IQueryRouter router = Substitute.For<IQueryRouter>();

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        string token = CreateJwt("admin-user", claims: Array.Empty<Claim>());
        using HttpClient client = CreateClientWithBearer(factory, token);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Status.ShouldBe(403);
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("principal_not_member");
        await router.DidNotReceiveWithAnyArgs().RouteQueryAsync(default!, default);
    }

    [Fact]
    public async Task ListTenants_returns_403_when_direct_eventstore_tenant_claim_is_blank_even_with_source_alias() {
        IQueryRouter router = Substitute.For<IQueryRouter>();

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        string token = CreateJwt(
            "admin-user",
            claims:
            [
                new Claim("eventstore:tenant", " "),
                new Claim("tenants", JsonSerializer.Serialize(new[] { "system" })),
            ]);
        using HttpClient client = CreateClientWithBearer(factory, token);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("principal_not_member");
        await router.DidNotReceiveWithAnyArgs().RouteQueryAsync(default!, default);
    }

    [Fact]
    public async Task ListTenants_returns_403_when_jwt_tenant_claim_targets_another_tenant() {
        IQueryRouter router = Substitute.For<IQueryRouter>();

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        string token = CreateJwt("admin-user", claims: [new Claim("eventstore:tenant", "tenant-a")]);
        using HttpClient client = CreateClientWithBearer(factory, token);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("tenant_mismatch");
        await router.DidNotReceiveWithAnyArgs().RouteQueryAsync(default!, default);
    }

    [Fact]
    public async Task GetTenant_returns_401_when_authorization_header_is_missing() {
        IQueryRouter router = CreateRouter(
            "get-tenant",
            new QueryRouterResult(true, JsonSerializer.SerializeToElement(new { tenantId = "tenant-1" }), false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant-1");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTenant_returns_typed_payload_shape_and_dispatches_tenant_query() {
        JsonElement payload = JsonSerializer.SerializeToElement(new TenantDetail(
            TenantId: "tenant-1",
            Name: "Tenant One",
            Description: "Primary tenant",
            Status: TenantStatus.Disabled,
            Members:
            [
                new TenantMember("member-user", TenantRole.TenantReader),
            ],
            Configuration: new Dictionary<string, string> {
                ["billing-plan"] = "enterprise",
            },
            CreatedAt: new DateTimeOffset(2026, 5, 15, 10, 0, 0, TimeSpan.Zero)), s_queryJsonOptions);
        List<SubmitQuery> routedQueries = [];
        IQueryRouter router = CreateCapturingRouter(
            GetTenantQuery.QueryType,
            new QueryRouterResult(true, payload, false, ProjectionType: "tenants"),
            routedQueries);

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        using HttpClient client = CreateJwtClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant-1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("tenantId").GetString().ShouldBe("tenant-1");
        result.GetProperty("name").GetString().ShouldBe("Tenant One");
        result.GetProperty("description").GetString().ShouldBe("Primary tenant");
        result.GetProperty("status").GetString().ShouldBe("Disabled");
        result.GetProperty("members").GetArrayLength().ShouldBe(1);
        result.GetProperty("members")[0].GetProperty("userId").GetString().ShouldBe("member-user");
        result.GetProperty("members")[0].GetProperty("role").GetString().ShouldBe("TenantReader");
        result.GetProperty("configuration").GetProperty("billing-plan").GetString().ShouldBe("enterprise");
        result.TryGetProperty("createdAt", out _).ShouldBeTrue();

        SubmitQuery query = routedQueries.Single();
        query.Tenant.ShouldBe("system");
        query.Domain.ShouldBe(GetTenantQuery.Domain);
        query.AggregateId.ShouldBe("tenant-1");
        query.QueryType.ShouldBe(GetTenantQuery.QueryType);
        query.EntityId.ShouldBe("tenant-1");
        query.UserId.ShouldBe("admin-user");
        query.ProjectionType.ShouldBe(TenantProjectionRouting.ActorTypeName);
        query.Payload.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTenant_returns_404_when_authorization_header_has_valid_jwt_and_tenant_is_unknown() {
        IQueryRouter router = CreateRouter(
            "get-tenant",
            new QueryRouterResult(false, null, false, "Tenant not found"));

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        using HttpClient client = CreateJwtClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/missing-tenant");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListTenants_returns_401_when_jwt_signature_is_invalid() {
        IQueryRouter router = CreateRouter(
            "list-tenants",
            new QueryRouterResult(true, JsonSerializer.SerializeToElement(new { items = Array.Empty<object>() }), false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        string token = CreateJwt("admin-user", signingKey: "wrong-signing-key-must-be-at-least-32-chars-long");
        using HttpClient client = CreateClientWithBearer(factory, token);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListTenants_returns_401_when_jwt_issuer_is_wrong() {
        IQueryRouter router = CreateRouter(
            "list-tenants",
            new QueryRouterResult(true, JsonSerializer.SerializeToElement(new { items = Array.Empty<object>() }), false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        string token = CreateJwt("admin-user", issuer: "rogue-issuer");
        using HttpClient client = CreateClientWithBearer(factory, token);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListTenants_returns_401_when_jwt_audience_is_wrong() {
        IQueryRouter router = CreateRouter(
            "list-tenants",
            new QueryRouterResult(true, JsonSerializer.SerializeToElement(new { items = Array.Empty<object>() }), false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        string token = CreateJwt("admin-user", audience: "wrong-audience");
        using HttpClient client = CreateClientWithBearer(factory, token);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListTenants_returns_401_when_jwt_is_expired() {
        IQueryRouter router = CreateRouter(
            "list-tenants",
            new QueryRouterResult(true, JsonSerializer.SerializeToElement(new { items = Array.Empty<object>() }), false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        string token = CreateJwt("admin-user", expires: DateTime.UtcNow.AddMinutes(-10));
        using HttpClient client = CreateClientWithBearer(factory, token);

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTenant_returns_403_problem_details_when_projection_forbids_access() {
        IQueryRouter router = CreateRouter(
            "get-tenant",
            new QueryRouterResult(false, null, false, "Forbidden"));

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant-1");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Status.ShouldBe(403);
        details.Title.ShouldBe("Forbidden");
        string body = await response.Content.ReadAsStringAsync();
        AssertProblemDetailsDoesNotLeakQueryData(body, allowCursorReasonText: false);
    }

    [Fact]
    public async Task GetTenant_returns_404_problem_details_when_projection_reports_not_found() {
        IQueryRouter router = CreateRouter(
            "get-tenant",
            new QueryRouterResult(false, null, false, "Tenant not found"));

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/missing-tenant");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Status.ShouldBe(404);
        details.Title.ShouldBe("Not Found");
        string body = await response.Content.ReadAsStringAsync();
        AssertProblemDetailsDoesNotLeakQueryData(body, allowCursorReasonText: false);
    }

    [Fact]
    public async Task GetTenantUsers_returns_401_when_authorization_header_is_missing() {
        IQueryRouter router = Substitute.For<IQueryRouter>();

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant-1/users");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await router.DidNotReceiveWithAnyArgs().RouteQueryAsync(default!, default);
    }

    [Fact]
    public async Task GetTenantUsers_returns_paginated_payload_shape_and_dispatches_tenant_query() {
        JsonElement payload = JsonSerializer.SerializeToElement(new PaginatedResult<TenantMember>(
            Items:
            [
                new("member-user", TenantRole.TenantOwner),
            ],
            Cursor: null,
            HasMore: false), s_queryJsonOptions);
        List<SubmitQuery> routedQueries = [];
        IQueryRouter router = CreateCapturingRouter(
            GetTenantUsersQuery.QueryType,
            new QueryRouterResult(true, payload, false, ProjectionType: "tenants"),
            routedQueries);

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant-1/users");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("items").GetArrayLength().ShouldBe(1);
        result.GetProperty("items")[0].GetProperty("userId").GetString().ShouldBe("member-user");
        result.GetProperty("items")[0].GetProperty("role").GetString().ShouldBe("TenantOwner");
        result.GetProperty("cursor").ValueKind.ShouldBe(JsonValueKind.Null);
        result.GetProperty("hasMore").GetBoolean().ShouldBeFalse();

        SubmitQuery query = routedQueries.Single();
        query.Tenant.ShouldBe("system");
        query.Domain.ShouldBe(GetTenantUsersQuery.Domain);
        query.AggregateId.ShouldBe("tenant-1");
        query.QueryType.ShouldBe(GetTenantUsersQuery.QueryType);
        query.EntityId.ShouldBe("tenant-1");
        query.UserId.ShouldBe("test-user");
        query.ProjectionType.ShouldBe(TenantProjectionRouting.ActorTypeName);
        ReadPayloadPageSize(query).ShouldBe(TenantQueryPaginationPolicy.StandardDefaultPageSize);
        ReadPayloadCursor(query).ShouldBeNull();
    }

    [Theory]
    [InlineData("", TenantQueryPaginationPolicy.StandardDefaultPageSize)]
    [InlineData("?pageSize=25", 25)]
    [InlineData("?pageSize=0", TenantQueryPaginationPolicy.StandardDefaultPageSize)]
    [InlineData("?pageSize=-5", TenantQueryPaginationPolicy.StandardDefaultPageSize)]
    [InlineData("?pageSize=101", TenantQueryPaginationPolicy.StandardMaximumPageSize)]
    public async Task GetTenantUsers_forwards_bounded_page_size_in_query_payload(string queryString, int expectedPageSize) {
        JsonElement payload = JsonSerializer.SerializeToElement(new { items = Array.Empty<object>(), cursor = (string?)null, hasMore = false });
        List<SubmitQuery> routedQueries = [];
        IQueryRouter router = CreateCapturingRouter(
            GetTenantUsersQuery.QueryType,
            new QueryRouterResult(true, payload, false, ProjectionType: "tenants"),
            routedQueries);

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant-1/users" + queryString);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SubmitQuery query = routedQueries.Single();
        ReadPayloadPageSize(query).ShouldBe(expectedPageSize);
        ReadPayloadCursor(query).ShouldBeNull();
    }

    [Fact]
    public async Task GetTenantUsers_forwards_valid_signed_cursor_in_query_payload() {
        JsonElement payload = JsonSerializer.SerializeToElement(new { items = Array.Empty<object>(), cursor = (string?)null, hasMore = false });
        List<SubmitQuery> routedQueries = [];
        IQueryRouter router = CreateCapturingRouter(
            GetTenantUsersQuery.QueryType,
            new QueryRouterResult(true, payload, false, ProjectionType: "tenants"),
            routedQueries);

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        ITenantQueryCursorCodec cursorCodec = factory.Services.GetRequiredService<ITenantQueryCursorCodec>();
        string cursor = cursorCodec.Encode(
            GetTenantUsersQuery.QueryType,
            TenantQueryCursorScopes.GetTenantUsers("tenant-1"),
            "member-user");
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/tenants/tenant-1/users?cursor={Uri.EscapeDataString(cursor)}&pageSize=25");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SubmitQuery query = routedQueries.Single();
        ReadPayloadPageSize(query).ShouldBe(25);
        ReadPayloadCursor(query).ShouldBe(cursor);
    }

    [Fact]
    public async Task GetTenantUsers_returns_403_problem_details_when_projection_forbids_access() {
        IQueryRouter router = CreateRouter(
            GetTenantUsersQuery.QueryType,
            new QueryRouterResult(false, null, false, "Forbidden"));

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant-1/users");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        string body = await response.Content.ReadAsStringAsync();
        AssertProblemDetailsDoesNotLeakQueryData(body, allowCursorReasonText: false);
    }

    [Fact]
    public async Task GetTenantUsers_returns_404_problem_details_when_projection_reports_not_found() {
        IQueryRouter router = CreateRouter(
            GetTenantUsersQuery.QueryType,
            new QueryRouterResult(false, null, false, "Tenant not found"));

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/missing-tenant/users");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        string body = await response.Content.ReadAsStringAsync();
        AssertProblemDetailsDoesNotLeakQueryData(body, allowCursorReasonText: false);
    }

    [Fact]
    public async Task GetTenantAudit_returns_403_problem_details_when_caller_is_not_global_admin() {
        // Task 7.4: keep+strengthen the integration assertion that non-admin gets 403, not 501,
        // and does not reveal audit data. The actor returns ErrorMessage="Forbidden" for
        // non-GlobalAdmin callers; the controller pipeline must map this to a 403 problem+json.
        IQueryRouter router = CreateRouter(
            "get-tenant-audit",
            new QueryRouterResult(false, null, false, "Forbidden"));

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant-1/audit");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Status.ShouldBe(403);
        details.Title.ShouldBe("Forbidden");
        string body = await response.Content.ReadAsStringAsync();
        AssertProblemDetailsDoesNotLeakQueryData(body, allowCursorReasonText: false);
    }

    [Fact]
    public async Task GetTenantAudit_returns_400_when_category_is_invalid() {
        IQueryRouter router = CreateRouter(
            "get-tenant-audit",
            new QueryRouterResult(true, JsonSerializer.SerializeToElement(new { items = Array.Empty<object>() }), false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant-1/audit?category=invalid");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("/api/tenants?cursor=not-a-protected-cursor")]
    [InlineData("/api/tenants/tenant-1/users?cursor=not-a-protected-cursor")]
    [InlineData("/api/users/user-2/tenants?cursor=not-a-protected-cursor")]
    [InlineData("/api/tenants/tenant-1/audit?cursor=not-a-protected-cursor")]
    public async Task Paginated_queries_return_400_problem_details_when_cursor_is_invalid(string path) {
        IQueryRouter router = Substitute.For<IQueryRouter>();

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        string body = await response.Content.ReadAsStringAsync();
        ProblemDetails? details = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        _ = details.ShouldNotBeNull();
        details.Status.ShouldBe(400);
        details.Detail.ShouldBe("Invalid cursor.");
        details.Extensions.ContainsKey("correlationId").ShouldBeTrue();
        details.Extensions["reasonCode"]?.ToString().ShouldBe("invalid-cursor");

        // AC3: no query state must leak in the rejection body — only ProblemDetails fields.
        using JsonDocument bodyDocument = JsonDocument.Parse(body);
        JsonElement root = bodyDocument.RootElement;
        root.TryGetProperty("items", out _).ShouldBeFalse();
        root.TryGetProperty("hasMore", out _).ShouldBeFalse();
        root.TryGetProperty("cursor", out _).ShouldBeFalse();
        AssertProblemDetailsDoesNotLeakQueryData(body, allowCursorReasonText: true);
        body.ShouldNotContain("not-a-protected-cursor");

        // The router must not be invoked when the cursor is rejected at the controller boundary.
        await router.DidNotReceiveWithAnyArgs().RouteQueryAsync(default!, default);
    }

    [Fact]
    public async Task Paginated_query_returns_400_before_routing_when_signed_cursor_query_type_does_not_match() {
        IQueryRouter router = Substitute.For<IQueryRouter>();

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        ITenantQueryCursorCodec cursorCodec = factory.Services.GetRequiredService<ITenantQueryCursorCodec>();
        string cursor = cursorCodec.Encode(
            GetTenantUsersQuery.QueryType,
            TenantQueryCursorScopes.ListTenants("test-user"),
            "tenant-secret");
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync($"/api/tenants?cursor={Uri.EscapeDataString(cursor)}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        string body = await response.Content.ReadAsStringAsync();
        ProblemDetails? details = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        _ = details.ShouldNotBeNull();
        details.Status.ShouldBe(400);
        details.Detail.ShouldBe("Invalid cursor.");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("invalid-cursor");
        body.ShouldNotContain(cursor);
        body.ShouldNotContain(GetTenantUsersQuery.QueryType);
        body.ShouldNotContain("tenant-secret");
        AssertProblemDetailsDoesNotLeakQueryData(body, allowCursorReasonText: true);
        await router.DidNotReceiveWithAnyArgs().RouteQueryAsync(default!, default);
    }

    [Fact]
    public async Task Paginated_query_returns_400_before_routing_when_cursor_key_was_rotated() {
        IQueryRouter router = Substitute.For<IQueryRouter>();

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        ITenantQueryCursorCodec rotatedKeyCodec = new TenantQueryCursorCodec(new EphemeralDataProtectionProvider());
        string cursor = rotatedKeyCodec.Encode(
            ListTenantsQuery.QueryType,
            TenantQueryCursorScopes.ListTenants("test-user"),
            "tenant-secret");
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync($"/api/tenants?cursor={Uri.EscapeDataString(cursor)}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        string body = await response.Content.ReadAsStringAsync();
        ProblemDetails? details = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        _ = details.ShouldNotBeNull();
        details.Status.ShouldBe(400);
        details.Detail.ShouldBe("Invalid cursor.");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("invalid-cursor");
        body.ShouldNotContain(cursor);
        body.ShouldNotContain("tenant-secret");
        AssertProblemDetailsDoesNotLeakQueryData(body, allowCursorReasonText: true);
        await router.DidNotReceiveWithAnyArgs().RouteQueryAsync(default!, default);
    }

    [Theory]
    [InlineData("/api/tenants?cursor={cursor}", "list-tenants", "user:other-user")]
    [InlineData("/api/tenants/tenant-1/users?cursor={cursor}", "get-tenant-users", "tenant:tenant-2")]
    [InlineData("/api/users/user-2/tenants?cursor={cursor}", "get-user-tenants", "requester:other-user|target-user:user-2")]
    [InlineData("/api/users/user-2/tenants?cursor={cursor}", "get-user-tenants", "requester:test-user|target-user:user-3")]
    [InlineData("/api/tenants/tenant-1/audit?cursor={cursor}", "get-tenant-audit", "tenant:tenant-2|from:|to:|category:")]
    [InlineData("/api/tenants/tenant-1/audit?category=Administrative&cursor={cursor}", "get-tenant-audit", "tenant:tenant-1|from:|to:|category:Access")]
    public async Task Paginated_queries_return_400_before_routing_when_signed_cursor_scope_does_not_match(
        string pathTemplate,
        string queryType,
        string foreignScope) {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathTemplate);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryType);
        ArgumentException.ThrowIfNullOrWhiteSpace(foreignScope);

        IQueryRouter router = Substitute.For<IQueryRouter>();

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        ITenantQueryCursorCodec cursorCodec = factory.Services.GetRequiredService<ITenantQueryCursorCodec>();
        string cursor = cursorCodec.Encode(queryType, foreignScope, "position-1");
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync(
            pathTemplate.Replace("{cursor}", Uri.EscapeDataString(cursor), StringComparison.Ordinal));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        string body = await response.Content.ReadAsStringAsync();
        ProblemDetails? details = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        _ = details.ShouldNotBeNull();
        details.Status.ShouldBe(400);
        details.Detail.ShouldBe("Invalid cursor.");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("invalid-cursor");

        using JsonDocument bodyDocument = JsonDocument.Parse(body);
        JsonElement root = bodyDocument.RootElement;
        root.TryGetProperty("items", out _).ShouldBeFalse();
        root.TryGetProperty("hasMore", out _).ShouldBeFalse();
        root.TryGetProperty("cursor", out _).ShouldBeFalse();
        AssertProblemDetailsDoesNotLeakQueryData(body, allowCursorReasonText: true);
        body.ShouldNotContain(cursor);
        body.ShouldNotContain(foreignScope);

        await router.DidNotReceiveWithAnyArgs().RouteQueryAsync(default!, default);
    }

    [Fact]
    public async Task GetTenantAudit_returns_payload_when_query_succeeds() {
        JsonElement payload = JsonSerializer.SerializeToElement(new { items = Array.Empty<object>(), cursor = (string?)null, hasMore = false });
        IQueryRouter router = CreateRouter(
            "get-tenant-audit",
            new QueryRouterResult(true, payload, false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant-1/audit");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.TryGetProperty("items", out _).ShouldBeTrue();
    }

    private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<TenantBootstrapOptions> factory) {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        return client;
    }

    private static IQueryRouter CreateRouter(string queryType, QueryRouterResult result) {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryType);

        IQueryRouter router = Substitute.For<IQueryRouter>();
        _ = router.RouteQueryAsync(
                Arg.Is<SubmitQuery>(q => q != null && string.Equals(q.QueryType, queryType, StringComparison.Ordinal)),
                Arg.Any<CancellationToken>())
            .Returns(result);
        return router;
    }

    private static IQueryRouter CreateCapturingRouter(
        string queryType,
        QueryRouterResult result,
        ICollection<SubmitQuery> routedQueries) {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryType);
        ArgumentNullException.ThrowIfNull(routedQueries);

        IQueryRouter router = Substitute.For<IQueryRouter>();
        _ = router.RouteQueryAsync(
                Arg.Is<SubmitQuery>(q => q != null && string.Equals(q.QueryType, queryType, StringComparison.Ordinal)),
                Arg.Any<CancellationToken>())
            .Returns(call => {
                SubmitQuery? routedQuery = call.Arg<SubmitQuery>();
                ArgumentNullException.ThrowIfNull(routedQuery);
                routedQueries.Add(routedQuery);
                return result;
            });
        return router;
    }

    private static int ReadPayloadPageSize(SubmitQuery query) {
        using JsonDocument document = JsonDocument.Parse(query.Payload);
        return document.RootElement.GetProperty("pageSize").GetInt32();
    }

    private static string? ReadPayloadCursor(SubmitQuery query) {
        using JsonDocument document = JsonDocument.Parse(query.Payload);
        JsonElement cursor = document.RootElement.GetProperty("cursor");
        return cursor.ValueKind == JsonValueKind.Null ? null : cursor.GetString();
    }

    private static void AssertProblemDetailsDoesNotLeakQueryData(string body, bool allowCursorReasonText) {
        body.ShouldNotContain("items", Case.Insensitive);
        if (!allowCursorReasonText) {
            body.ShouldNotContain("cursor", Case.Insensitive);
        }

        body.ShouldNotContain("hasMore", Case.Insensitive);
        body.ShouldNotContain("Tenant One");
        body.ShouldNotContain("tenant-secret");
        body.ShouldNotContain("member-user");
        body.ShouldNotContain("evt-secret");
        body.ShouldNotContain("Bearer ", Case.Insensitive);
        body.ShouldNotContain(JwtSigningKey);
        body.ShouldNotContain(SmokeJwtSigningKey);
    }

    private static HttpClient CreateJwtClient(WebApplicationFactory<TenantBootstrapOptions> factory)
        => CreateClientWithBearer(factory, CreateJwt("admin-user"));

    private static HttpClient CreateSmokeJwtClient(WebApplicationFactory<TenantBootstrapOptions> factory)
        => CreateClientWithBearer(factory, CreateSmokeJwt("admin-user"));

    private static HttpClient CreateClientWithBearer(WebApplicationFactory<TenantBootstrapOptions> factory, string token) {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string CreateSmokeJwt(
        string userId,
        string? issuer = null,
        string? audience = null,
        string? signingKey = null,
        DateTime? expires = null,
        IEnumerable<Claim>? claims = null)
        => CreateJwt(
            userId,
            issuer ?? SmokeJwtIssuer,
            audience ?? SmokeJwtAudience,
            signingKey ?? SmokeJwtSigningKey,
            expires,
            claims);

    private static string CreateJwt(
        string userId,
        string? issuer = null,
        string? audience = null,
        string? signingKey = null,
        DateTime? expires = null,
        IEnumerable<Claim>? claims = null) {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? JwtSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var tokenClaims = new List<Claim> { new("sub", userId) };
        if (claims is null) {
            tokenClaims.Add(new Claim("eventstore:tenant", "system"));
        }
        else {
            tokenClaims.AddRange(claims);
        }

        var token = new JwtSecurityToken(
            issuer: issuer ?? JwtIssuer,
            audience: audience ?? JwtAudience,
            claims: tokenClaims,
            expires: expires ?? DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static void ConfigureSmokeJwtBearer(JwtBearerOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        options.Authority = null;
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidIssuer = SmokeJwtIssuer,
            ValidateAudience = true,
            ValidAudience = SmokeJwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SmokeJwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
        };
    }

    private sealed class TenantsQueryJwtWebApplicationFactory(
        IQueryRouter router,
        bool useSmokeAuthentication = false) : WebApplicationFactory<TenantBootstrapOptions> {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
            => builder.ConfigureServices(services => {
                if (useSmokeAuthentication) {
                    services.PostConfigure<JwtBearerOptions>(
                        JwtBearerDefaults.AuthenticationScheme,
                        ConfigureSmokeJwtBearer);
                }

                _ = services.RemoveAll<IQueryRouter>();
                _ = services.AddSingleton(router);
            });
    }

    private sealed class TenantsQueryWebApplicationFactory(IQueryRouter router) : WebApplicationFactory<TenantBootstrapOptions> {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureServices(services => {
            _ = services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            _ = services.RemoveAll<IQueryRouter>();
            _ = services.AddSingleton(router);
        });
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder) {
        public const string SchemeName = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
            var identity = new ClaimsIdentity(
            [
                new Claim("sub", "test-user"),
                new Claim("eventstore:tenant", "system"),
            ],
            SchemeName);

            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
