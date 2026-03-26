#pragma warning disable CA2007

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;

using Hexalith.EventStore.CommandApi.Models;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
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

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

public class CommandApiRuntimeIntegrationTests {
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
        var request = new SubmitCommandRequest(
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

    private sealed class CommandApiWebApplicationFactory(
        ICommandRouter? router = null,
        ICommandStatusStore? statusStore = null,
        ICommandArchiveStore? archiveStore = null) : WebApplicationFactory<TenantBootstrapOptions> {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureServices(services => {
            _ = services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

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
