#pragma warning disable CA2007

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

using Hexalith.EventStore.Server.Pipeline.Queries;
using Hexalith.EventStore.Server.Queries;
using Hexalith.Tenants.CommandApi.Configuration;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

public class TenantsQueryControllerIntegrationTests {
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
    public async Task GetTenantAudit_returns_501_problem_details_when_projection_reports_not_implemented() {
        IQueryRouter router = CreateRouter(
            "get-tenant-audit",
            new QueryRouterResult(false, null, false, "Audit queries are not yet implemented (FR29). Planned for a future release."));

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant-1/audit");

        response.StatusCode.ShouldBe(HttpStatusCode.NotImplemented);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Status.ShouldBe(501);
        details.Title.ShouldBe("Not Implemented");
        _ = details.Detail.ShouldNotBeNull();
        details.Detail.ShouldContain("not yet implemented", Case.Insensitive);
    }

    [Fact]
    public async Task GetTenantAudit_returns_payload_when_query_succeeds() {
        JsonElement payload = JsonSerializer.SerializeToElement(new { entries = Array.Empty<object>() });
        IQueryRouter router = CreateRouter(
            "get-tenant-audit",
            new QueryRouterResult(true, payload, false, ProjectionType: "tenants"));

        await using var factory = new TenantsQueryWebApplicationFactory(router);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant-1/audit");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        JsonElement result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.TryGetProperty("entries", out _).ShouldBeTrue();
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
                Arg.Is<SubmitQuery>(q => string.Equals(q.QueryType, queryType, StringComparison.Ordinal)),
                Arg.Any<CancellationToken>())
            .Returns(result);
        return router;
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
