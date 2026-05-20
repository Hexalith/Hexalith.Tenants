#pragma warning disable CA2007

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using Hexalith.EventStore.Authentication;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Models;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Commands;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Commands;

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

public class CommandApiRuntimeIntegrationTests {
    private const string JwtAudience = "hexalith-tenants";
    private const string JwtIssuer = "hexalith-dev";
    private const string JwtSigningKey = "this-is-a-development-signing-key-minimum-32-chars";

    [Fact]
    public async Task Process_endpoint_dispatches_create_tenant_command() {
        await using var factory = new CommandApiWebApplicationFactory();
        using HttpClient client = factory.CreateClient();

        var request = new DomainServiceRequest(
            new CommandEnvelope(
                Guid.NewGuid().ToString(),
                "system",
                "tenants",
                "acme",
                nameof(CreateTenant),
                JsonSerializer.SerializeToUtf8Bytes(new CreateTenant("acme", "Acme Corp", "Tenant from /process")),
                Guid.NewGuid().ToString(),
                null,
                "test-user",
                null),
            null);

        HttpResponseMessage response = await client.PostAsJsonAsync("/process", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DomainServiceWireResult? result = await response.Content.ReadFromJsonAsync<DomainServiceWireResult>();
        _ = result.ShouldNotBeNull();
        result.IsRejection.ShouldBeFalse();
        result.Events.Count.ShouldBe(1);
        result.Events[0].EventTypeName.ShouldEndWith("TenantCreated");
    }

    [Fact]
    public async Task Commands_endpoint_returns_problem_details_for_domain_rejection() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<Hexalith.EventStore.Server.Pipeline.Commands.SubmitCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(false, "Domain rejection: GlobalAdminAlreadyBootstrappedRejection", "test-correlation"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(
                CommandStatus.Rejected,
                DateTimeOffset.UtcNow,
                "global-administrators",
                1,
                "Hexalith.Tenants.Contracts.Events.Rejections.GlobalAdminAlreadyBootstrappedRejection",
                null,
                null));

        ICommandArchiveStore archiveStore = Substitute.For<ICommandArchiveStore>();

        await using var factory = new CommandApiWebApplicationFactory(router, statusStore, archiveStore);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        JsonElement payload = JsonSerializer.SerializeToElement(new BootstrapGlobalAdmin("admin-1"));
        var request = new Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest(
            Guid.NewGuid().ToString(),
            "system",
            "tenants",
            "global-administrators",
            nameof(BootstrapGlobalAdmin),
            payload);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Title.ShouldBe("Conflict");
        details.Status.ShouldBe(409);
        details.Detail.ShouldNotBeNullOrWhiteSpace();
        details.Type.ShouldBe("Hexalith.Tenants.Contracts.Events.Rejections.GlobalAdminAlreadyBootstrappedRejection");
        details.Extensions.ShouldContainKey("correlationId");
    }

    [Fact]
    public async Task Commands_endpoint_returns_202_when_jwt_has_eventstore_tenant_claim() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<Hexalith.EventStore.Server.Pipeline.Commands.SubmitCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(true, null, "test-correlation"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(CommandStatus.Completed, DateTimeOffset.UtcNow, "global-administrators", 1, null, null, null));

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            statusStore,
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        using HttpClient client = CreateJwtClient(factory);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateBootstrapRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        _ = await router.Received(1).RouteCommandAsync(
            Arg.Is<Hexalith.EventStore.Server.Pipeline.Commands.SubmitCommand>(c => c != null && c.Tenant == "system"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Commands_endpoint_returns_202_when_jwt_uses_tenants_source_claim() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<Hexalith.EventStore.Server.Pipeline.Commands.SubmitCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(true, null, "test-correlation"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(CommandStatus.Completed, DateTimeOffset.UtcNow, "global-administrators", 1, null, null, null));

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            statusStore,
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        string token = CreateJwt("admin-user", claims: [new Claim("tenants", JsonSerializer.Serialize(new[] { "system" }))]);
        using HttpClient client = CreateClientWithBearer(factory, token);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateBootstrapRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        _ = await router.Received(1).RouteCommandAsync(
            Arg.Is<Hexalith.EventStore.Server.Pipeline.Commands.SubmitCommand>(c => c != null && c.Tenant == "system"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Commands_endpoint_returns_403_when_jwt_tenant_claim_is_missing() {
        ICommandRouter router = Substitute.For<ICommandRouter>();

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            Substitute.For<ICommandStatusStore>(),
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        string token = CreateJwt("admin-user", claims: Array.Empty<Claim>());
        using HttpClient client = CreateClientWithBearer(factory, token);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateBootstrapRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Status.ShouldBe(403);
        details.Extensions["reasonCode"]?.ToString().ShouldBe("principal_not_member");
        await router.DidNotReceiveWithAnyArgs().RouteCommandAsync(default!, default);
    }

    [Fact]
    public async Task Commands_endpoint_returns_403_when_direct_eventstore_tenant_claim_is_blank_even_with_source_alias() {
        ICommandRouter router = Substitute.For<ICommandRouter>();

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            Substitute.For<ICommandStatusStore>(),
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        string token = CreateJwt(
            "admin-user",
            claims:
            [
                new Claim("eventstore:tenant", " "),
                new Claim("tenants", JsonSerializer.Serialize(new[] { "system" })),
            ]);
        using HttpClient client = CreateClientWithBearer(factory, token);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateBootstrapRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Extensions["reasonCode"]?.ToString().ShouldBe("principal_not_member");
        await router.DidNotReceiveWithAnyArgs().RouteCommandAsync(default!, default);
    }

    [Fact]
    public async Task Commands_endpoint_returns_403_when_jwt_tenant_claim_targets_another_tenant() {
        ICommandRouter router = Substitute.For<ICommandRouter>();

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            Substitute.For<ICommandStatusStore>(),
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        string token = CreateJwt("admin-user", claims: [new Claim("eventstore:tenant", "tenant-a")]);
        using HttpClient client = CreateClientWithBearer(factory, token);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateBootstrapRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Extensions["reasonCode"]?.ToString().ShouldBe("tenant_mismatch");
        await router.DidNotReceiveWithAnyArgs().RouteCommandAsync(default!, default);
    }

    [Fact]
    public async Task Tenants_host_registers_eventstore_claims_transformation() {
        await using var factory = new CommandApiWebApplicationFactory();
        _ = factory.CreateClient();

        IEnumerable<IClaimsTransformation> transformations = factory.Services.GetServices<IClaimsTransformation>();

        transformations.OfType<EventStoreClaimsTransformation>().ShouldNotBeEmpty();
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

    private static Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest CreateBootstrapRequest() {
        JsonElement payload = JsonSerializer.SerializeToElement(new BootstrapGlobalAdmin("admin-1"));
        return new Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest(
            Guid.NewGuid().ToString(),
            "system",
            "tenants",
            "global-administrators",
            nameof(BootstrapGlobalAdmin),
            payload);
    }

    private sealed class CommandApiWebApplicationFactory(
        ICommandRouter? router = null,
        ICommandStatusStore? statusStore = null,
        ICommandArchiveStore? archiveStore = null,
        bool useTestAuthentication = true) : WebApplicationFactory<TenantBootstrapOptions> {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureServices(services => {
            if (useTestAuthentication) {
                _ = services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            }

            if (router is not null) {
                _ = services.RemoveAll<ICommandRouter>();
                _ = services.AddSingleton(router);
            }

            if (statusStore is not null) {
                _ = services.RemoveAll<ICommandStatusStore>();
                _ = services.AddSingleton(statusStore);
            }

            if (archiveStore is not null) {
                _ = services.RemoveAll<ICommandArchiveStore>();
                _ = services.AddSingleton(archiveStore);
            }
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
