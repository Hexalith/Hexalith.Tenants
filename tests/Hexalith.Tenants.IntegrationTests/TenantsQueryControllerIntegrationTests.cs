#pragma warning disable CA2007

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using Hexalith.EventStore.Server.Pipeline.Queries;
using Hexalith.EventStore.Server.Queries;
using Hexalith.Tenants.Configuration;

using Microsoft.AspNetCore.Authentication;
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
    public async Task GetTenant_returns_200_when_authorization_header_has_valid_jwt_and_tenant_exists() {
        JsonElement payload = JsonSerializer.SerializeToElement(new { tenantId = "tenant-1", name = "Tenant One" });
        IQueryRouter router = CreateRouter(
            "get-tenant",
            new QueryRouterResult(true, payload, false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryJwtWebApplicationFactory(router);
        using HttpClient client = CreateJwtClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant-1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("tenantId").GetString().ShouldBe("tenant-1");
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

        // The router must not be invoked when the cursor is rejected at the controller boundary.
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

    private static HttpClient CreateJwtClient(WebApplicationFactory<TenantBootstrapOptions> factory)
        => CreateClientWithBearer(factory, CreateJwt("admin-user"));

    private static HttpClient CreateClientWithBearer(WebApplicationFactory<TenantBootstrapOptions> factory, string token) {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string CreateJwt(
        string userId,
        string? issuer = null,
        string? audience = null,
        string? signingKey = null,
        DateTime? expires = null) {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey ?? JwtSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer ?? JwtIssuer,
            audience: audience ?? JwtAudience,
            claims:
            [
                new Claim("sub", userId),
                new Claim("eventstore:tenant", "system"),
            ],
            expires: expires ?? DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class TenantsQueryJwtWebApplicationFactory(IQueryRouter router) : WebApplicationFactory<TenantBootstrapOptions> {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureServices(services => {
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
