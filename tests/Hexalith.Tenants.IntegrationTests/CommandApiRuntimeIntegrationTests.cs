#pragma warning disable CA2007

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.Authentication;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Models;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Commands;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events.Rejections;

using CommandApiResponse = Hexalith.EventStore.Contracts.Commands.SubmitCommandResponse;
using SubmitPipelineCommand = Hexalith.EventStore.Server.Pipeline.Commands.SubmitCommand;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    private const string GlobalAdminExtensionKey = "actor:globalAdmin";

    [Fact]
    public async Task Process_endpoint_dispatches_create_tenant_command() {
        await using var factory = new CommandApiWebApplicationFactory(useTestAuthentication: true);
        using HttpClient client = factory.CreateClient();

        var request = new DomainServiceRequest(
            new CommandEnvelope(
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                "system",
                "tenants",
                "acme",
                nameof(CreateTenant),
                JsonSerializer.SerializeToUtf8Bytes(new CreateTenant("acme", "Acme Corp", "Tenant from /process")),
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                null,
                "test-user",
                GlobalAdminExtensions()),
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

        await using var factory = new CommandApiWebApplicationFactory(router, statusStore, archiveStore, useTestAuthentication: true);
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
        details.Title.ShouldBe("Global Admin Already Bootstrapped Rejection");
        details.Status.ShouldBe(409);
        details.Detail.ShouldNotBeNullOrWhiteSpace();
        details.Type.ShouldBe("https://hexalith.io/problems/domain-rejections/global-admin-already-bootstrapped-rejection");
        details.Extensions.ShouldContainKey("correlationId");
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("global-admin-already-bootstrapped-rejection");
        details.Extensions.ShouldContainKey("rejectionType");
        details.Extensions["rejectionType"]?.ToString().ShouldBe("Hexalith.Tenants.Contracts.Events.Rejections.GlobalAdminAlreadyBootstrappedRejection");
    }

    [Fact]
    public async Task Commands_endpoint_returns_202_when_jwt_has_eventstore_tenant_claim() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<SubmitPipelineCommand>(), Arg.Any<CancellationToken>())
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
            Arg.Is<SubmitPipelineCommand>(c => c != null && c.Tenant == "system"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Commands_endpoint_accepts_CreateTenant_and_routes_story_payload() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        SubmitPipelineCommand? capturedCommand = null;
        _ = router.RouteCommandAsync(Arg.Do<SubmitPipelineCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(true, null, "create-correlation"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(CommandStatus.Completed, DateTimeOffset.UtcNow, "acme", 1, null, null, null));

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            statusStore,
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        string token = CreateJwt(
            "global-admin",
            claims:
            [
                new Claim("eventstore:tenant", "system"),
                new Claim("global_admin", "true"),
            ]);
        using HttpClient client = CreateClientWithBearer(factory, token);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateCreateTenantRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.RetryAfter?.Delta.ShouldBe(TimeSpan.FromSeconds(1));
        CommandApiResponse? body = await response.Content.ReadFromJsonAsync<CommandApiResponse>();
        _ = body.ShouldNotBeNull();
        body.CorrelationId.ShouldBe(request.MessageId);
        _ = capturedCommand.ShouldNotBeNull();
        capturedCommand.Tenant.ShouldBe("system");
        capturedCommand.Domain.ShouldBe("tenants");
        capturedCommand.AggregateId.ShouldBe("acme");
        capturedCommand.CommandType.ShouldBe(nameof(CreateTenant));
        capturedCommand.UserId.ShouldBe("global-admin");
        capturedCommand.IsGlobalAdmin.ShouldBeTrue();
        CreateTenant? payload = JsonSerializer.Deserialize<CreateTenant>(capturedCommand.Payload);
        _ = payload.ShouldNotBeNull();
        payload.TenantId.ShouldBe("acme");
        payload.Name.ShouldBe("Acme Corp");
        payload.Description.ShouldBe("Tenant from command API");
    }

    [Fact]
    public async Task Commands_endpoint_accepts_UpdateTenant_and_routes_story_payload() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        SubmitPipelineCommand? capturedCommand = null;
        _ = router.RouteCommandAsync(Arg.Do<SubmitPipelineCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(true, null, "update-correlation"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(CommandStatus.Completed, DateTimeOffset.UtcNow, "acme", 1, null, null, null));

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            statusStore,
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        string token = CreateJwt(
            "global-admin",
            claims:
            [
                new Claim("eventstore:tenant", "system"),
                new Claim("global_admin", "true"),
            ]);
        using HttpClient client = CreateClientWithBearer(factory, token);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateUpdateTenantRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        CommandApiResponse? body = await response.Content.ReadFromJsonAsync<CommandApiResponse>();
        _ = body.ShouldNotBeNull();
        body.CorrelationId.ShouldBe(request.MessageId);
        _ = capturedCommand.ShouldNotBeNull();
        capturedCommand.Tenant.ShouldBe("system");
        capturedCommand.Domain.ShouldBe("tenants");
        capturedCommand.AggregateId.ShouldBe("acme");
        capturedCommand.CommandType.ShouldBe(nameof(UpdateTenant));
        capturedCommand.UserId.ShouldBe("global-admin");
        capturedCommand.IsGlobalAdmin.ShouldBeTrue();
        UpdateTenant? payload = JsonSerializer.Deserialize<UpdateTenant>(capturedCommand.Payload);
        _ = payload.ShouldNotBeNull();
        payload.TenantId.ShouldBe("acme");
        payload.Name.ShouldBe("Acme Updated");
        payload.Description.ShouldBe("Updated tenant metadata");
    }

    [Fact]
    public async Task Commands_endpoint_returns_409_problem_details_for_duplicate_CreateTenant() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<SubmitPipelineCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(false, "Domain rejection: TenantAlreadyExistsRejection", "create-duplicate"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(
                CommandStatus.Rejected,
                DateTimeOffset.UtcNow,
                "acme",
                1,
                typeof(TenantAlreadyExistsRejection).FullName,
                null,
                null));

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            statusStore,
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        using HttpClient client = CreateJwtClient(factory);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateCreateTenantRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Title.ShouldBe("Tenant Already Exists Rejection");
        details.Status.ShouldBe(409);
        details.Type.ShouldBe("https://hexalith.io/problems/domain-rejections/tenant-already-exists-rejection");
        details.Extensions.ShouldContainKey("correlationId");
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("tenant-already-exists-rejection");
        details.Extensions.ShouldContainKey("rejectionType");
        details.Extensions["rejectionType"]?.ToString().ShouldBe(typeof(TenantAlreadyExistsRejection).FullName);
    }

    [Fact]
    public async Task Commands_endpoint_returns_404_problem_details_for_missing_UpdateTenant() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<SubmitPipelineCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(false, "Domain rejection: TenantNotFoundRejection", "update-missing"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(
                CommandStatus.Rejected,
                DateTimeOffset.UtcNow,
                "acme",
                1,
                typeof(TenantNotFoundRejection).FullName,
                null,
                null));

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            statusStore,
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        using HttpClient client = CreateJwtClient(factory);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateUpdateTenantRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Title.ShouldBe("Tenant Not Found Rejection");
        details.Status.ShouldBe(404);
        details.Type.ShouldBe("https://hexalith.io/problems/domain-rejections/tenant-not-found-rejection");
        details.Extensions.ShouldContainKey("correlationId");
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("tenant-not-found-rejection");
        details.Extensions.ShouldContainKey("rejectionType");
        details.Extensions["rejectionType"]?.ToString().ShouldBe(typeof(TenantNotFoundRejection).FullName);
    }

    [Fact]
    public async Task Commands_endpoint_ignores_client_supplied_globalAdmin_extension_when_jwt_is_not_global_admin() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<SubmitPipelineCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(true, null, "test-correlation"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(CommandStatus.Completed, DateTimeOffset.UtcNow, "acme", 1, null, null, null));

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            statusStore,
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        string token = CreateJwt(
            "tenant-operator",
            claims:
            [
                new Claim("eventstore:tenant", "system"),
                new Claim("eventstore:permission", "commands:*"),
            ]);
        using HttpClient client = CreateClientWithBearer(factory, token);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateCreateTenantRequest(
            extensions: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                [GlobalAdminExtensionKey] = "true",
                ["client-correlation"] = "safe-metadata",
            });

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        _ = await router.Received(1).RouteCommandAsync(
            Arg.Is<SubmitPipelineCommand>(c =>
                c != null
                && !c.IsGlobalAdmin
                && c.Extensions != null
                && !c.Extensions.ContainsKey(GlobalAdminExtensionKey)
                && c.Extensions["client-correlation"] == "safe-metadata"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Commands_endpoint_marks_submit_command_global_admin_when_jwt_has_global_admin_claim() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<SubmitPipelineCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(true, null, "test-correlation"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(CommandStatus.Completed, DateTimeOffset.UtcNow, "acme", 1, null, null, null));

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            statusStore,
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        string token = CreateJwt(
            "global-admin",
            claims:
            [
                new Claim("eventstore:tenant", "system"),
                new Claim("global_admin", "true"),
            ]);
        using HttpClient client = CreateClientWithBearer(factory, token);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateCreateTenantRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        _ = await router.Received(1).RouteCommandAsync(
            Arg.Is<SubmitPipelineCommand>(c =>
                c != null
                && c.IsGlobalAdmin
                && c.UserId == "global-admin"
                && c.AggregateId == "acme"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Commands_endpoint_returns_202_when_jwt_uses_tenants_source_claim() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<SubmitPipelineCommand>(), Arg.Any<CancellationToken>())
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
            Arg.Is<SubmitPipelineCommand>(c => c != null && c.Tenant == "system"),
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
        details.Extensions.ShouldContainKey("reasonCode");
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
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("principal_not_member");
        await router.DidNotReceiveWithAnyArgs().RouteCommandAsync(default!, default);
    }

    // P10: triangulate the cross-tenant gate by varying which side mismatches. Both rows must yield
    // 403 + tenant_mismatch — proving the check site compares the claim against the request tenant,
    // not against the URL/route or against a hard-coded tenant.
    [Theory]
    [InlineData("tenant-a", "system")]
    [InlineData("system", "tenant-a")]
    public async Task Commands_endpoint_returns_403_when_jwt_tenant_claim_does_not_match_request_tenant(string claimTenant, string requestTenant) {
        ICommandRouter router = Substitute.For<ICommandRouter>();

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            Substitute.For<ICommandStatusStore>(),
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        string token = CreateJwt("admin-user", claims: [new Claim("eventstore:tenant", claimTenant)]);
        using HttpClient client = CreateClientWithBearer(factory, token);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateBootstrapRequest(tenant: requestTenant);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("tenant_mismatch");
        await router.DidNotReceiveWithAnyArgs().RouteCommandAsync(default!, default);
    }

    // P8: companion to the 403 cross-tenant theory — proves the validator does equality (claim
    // matches request tenant), not a hard-coded `system` whitelist. With a hard-coded check, the
    // `tenant-a` row would 403 even when the JWT carries `eventstore:tenant=tenant-a` and the
    // request body says `tenant=tenant-a`. The router is mocked to return 202 so any failure
    // localizes to the tenant validator.
    [Theory]
    [InlineData("system")]
    [InlineData("tenant-a")]
    public async Task Commands_endpoint_returns_202_when_jwt_tenant_claim_matches_request_tenant(string tenant) {
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
        string token = CreateJwt("admin-user", claims: [new Claim("eventstore:tenant", tenant)]);
        using HttpClient client = CreateClientWithBearer(factory, token);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateBootstrapRequest(tenant: tenant);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        _ = await router.Received(1).RouteCommandAsync(
            Arg.Is<Hexalith.EventStore.Server.Pipeline.Commands.SubmitCommand>(c => c != null && c.Tenant == tenant),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tenants_host_registers_eventstore_claims_transformation_as_transient() {
        // P8: lifetime check is part of the contract — ASP.NET Core invokes IClaimsTransformation
        // once per authenticated request, and a Singleton/Scoped registration would silently break
        // re-invocation semantics.
        // P7: in addition to the descriptor check (catches direct `services.Add*` lifetime drift),
        // also exercise the runtime contract directly — resolve the service twice from the built
        // provider and assert different instances. This catches a later `services.Replace(...)`
        // decorator that wraps the transformation as Singleton/Scoped, which the descriptor capture
        // inside ConfigureServices would miss.
        ServiceDescriptor? descriptor = null;
        await using var factory = new CommandApiWebApplicationFactory()
            .WithWebHostBuilder(b => b.ConfigureServices(services => {
                descriptor = services.FirstOrDefault(d =>
                    d.ServiceType == typeof(IClaimsTransformation)
                    && d.ImplementationType == typeof(EventStoreClaimsTransformation));
            }));
        _ = factory.CreateClient();

        IEnumerable<IClaimsTransformation> transformations = factory.Services.GetServices<IClaimsTransformation>();
        transformations.OfType<EventStoreClaimsTransformation>().ShouldNotBeEmpty();

        _ = descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Transient);

        EventStoreClaimsTransformation? firstInstance = factory.Services
            .GetServices<IClaimsTransformation>()
            .OfType<EventStoreClaimsTransformation>()
            .FirstOrDefault();
        EventStoreClaimsTransformation? secondInstance = factory.Services
            .GetServices<IClaimsTransformation>()
            .OfType<EventStoreClaimsTransformation>()
            .FirstOrDefault();
        _ = firstInstance.ShouldNotBeNull();
        _ = secondInstance.ShouldNotBeNull();
        firstInstance.ShouldNotBeSameAs(secondInstance);
    }

    [Fact]
    public void Tenants_host_keeps_jwt_bearer_map_inbound_claims_false() {
        // P9: spec Task line 33 requires verifying MapInboundClaims=false on the host-effective
        // JwtBearerOptions, not on a freshly constructed instance.
        using var factory = new CommandApiWebApplicationFactory();
        _ = factory.CreateClient();

        JwtBearerOptions options = factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        options.MapInboundClaims.ShouldBeFalse();
    }

    [Fact]
    public async Task Tenants_host_keeps_raw_sub_claim_under_real_jwt_pipeline() {
        // P6: companion to Tenants_host_keeps_jwt_bearer_map_inbound_claims_false — that test checks
        // the host-effective options bag; this test exercises the live JWT bearer middleware via an
        // HTTP request. CommandsController.cs:67 reads UserId via `User.FindFirst("sub")?.Value`.
        // With MapInboundClaims=true (regression), JWT bearer would consume the `sub` claim and
        // remap it to ClaimTypes.NameIdentifier, leaving `User.FindFirst("sub")` returning null and
        // the controller short-circuiting with an unauthorized response. The JWT also carries a
        // `name` claim with a different value to prove the controller does not fall back to `name`.
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
        string token = CreateJwt(
            "admin-user",
            claims:
            [
                new Claim("eventstore:tenant", "system"),
                new Claim("name", "different-display-name"),
            ]);
        using HttpClient client = CreateClientWithBearer(factory, token);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateBootstrapRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        _ = await router.Received(1).RouteCommandAsync(
            Arg.Is<Hexalith.EventStore.Server.Pipeline.Commands.SubmitCommand>(c =>
                c != null && c.UserId == "admin-user"),
            Arg.Any<CancellationToken>());
    }

    // AUTH-INT-001 — Pins the DAPR callback contract: AggregateActor invokes /process via
    // DAPR service-to-service after EventStore's auth boundary. Adding .RequireAuthorization()
    // on /process would silently stall the AggregateActor 5-step checkpoint at Step 4.
    // Source: 11-3 review deferred-work in _bmad-output/implementation-artifacts/deferred-work.md.
    [Fact]
    public async Task Process_endpoint_accepts_anonymous_request_to_preserve_dapr_callback_contract() {
        await using var factory = new CommandApiWebApplicationFactory(useTestAuthentication: false);
        using HttpClient client = factory.CreateClient();
        // NOTE: no Authorization header — proves the route does NOT enforce authentication.

        var request = new DomainServiceRequest(
            new CommandEnvelope(
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                "system",
                "tenants",
                "acme-anon",
                nameof(CreateTenant),
                JsonSerializer.SerializeToUtf8Bytes(new CreateTenant("acme-anon", "Acme Anonymous", "Anonymous DAPR callback path")),
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                null,
                "dapr-callback",
                GlobalAdminExtensions()),
            null);

        HttpResponseMessage response = await client.PostAsJsonAsync("/process", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DomainServiceWireResult? result = await response.Content.ReadFromJsonAsync<DomainServiceWireResult>();
        _ = result.ShouldNotBeNull();
        result.IsRejection.ShouldBeFalse();
        result.Events.Count.ShouldBe(1);
        result.Events[0].EventTypeName.ShouldEndWith("TenantCreated");
    }

    // AUTH-INT-002 — Pins docs/production-auth-claim-contract.md:13 ("Do not use `name` as the
    // trusted subject") at live JwtBearer pipeline tier. First-run finding (2026-05-20): a token
    // carrying only `name` (no `sub`) is rejected at authentication with 401, not at authorization
    // with 403. This is a STRONGER contract than the original test design — without a trusted
    // subject, the request never establishes an authenticated identity for the command pipeline.
    // AUTH-T2-001 covers the transformation/validator unit shape; this test exercises the live
    // JwtBearer middleware. Source: 11-2 review deferred-work.
    [Fact]
    public async Task Commands_endpoint_returns_401_when_jwt_carries_only_name_claim_without_sub() {
        ICommandRouter router = Substitute.For<ICommandRouter>();

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            Substitute.For<ICommandStatusStore>(),
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        string token = CreateJwtWithoutSub(claims: [new Claim("name", "display-only-user")]);
        using HttpClient client = CreateClientWithBearer(factory, token);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateBootstrapRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await router.DidNotReceiveWithAnyArgs().RouteCommandAsync(default!, default);
    }

    // AUTH-INT-003 — Pins the source-claim shapes documented at
    // docs/production-auth-claim-contract.md (space-delimited tenants, tenant_id direct, tid
    // fallback, tenant_id+tid precedence) through the live JwtBearer + EventStoreClaimsTransformation
    // pipeline. The JSON-array tenants shape is already covered by
    // Commands_endpoint_returns_202_when_jwt_uses_tenants_source_claim above.
    // TenantClaimContractTests.cs covers all shapes at unit tier; this Theory pins the live
    // middleware behavior. Source: 11-2 review deferred-work.
    [Theory]
    [InlineData("tenants", "system tenant-a", null, null)]
    [InlineData("tenant_id", "system", null, null)]
    [InlineData("tid", "system", null, null)]
    [InlineData("tenant_id", "system", "tid", "tenant-a")]
    public async Task Commands_endpoint_returns_202_when_jwt_uses_supported_source_claim_shape(
        string claim1Type, string claim1Value, string? claim2Type, string? claim2Value) {
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
        var claims = new List<Claim> { new(claim1Type, claim1Value) };
        if (claim2Type is not null) {
            claims.Add(new Claim(claim2Type, claim2Value!));
        }

        string token = CreateJwt("admin-user", claims: claims);
        using HttpClient client = CreateClientWithBearer(factory, token);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateBootstrapRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        _ = await router.Received(1).RouteCommandAsync(
            Arg.Is<Hexalith.EventStore.Server.Pipeline.Commands.SubmitCommand>(c => c != null && c.Tenant == "system"),
            Arg.Any<CancellationToken>());
    }

    // AUTH-INT-004 — Pins ClaimsRbacValidator.cs permission semantics through the live pipeline:
    // commands:* (wildcard), command:submit (category), and exact command-type token. Source:
    // 11-2 review deferred-work — wildcard handling untested in Tenants.
    [Theory]
    [InlineData("commands:*")]
    [InlineData("command:submit")]
    [InlineData(nameof(BootstrapGlobalAdmin))]
    public async Task Commands_endpoint_returns_202_when_jwt_carries_authorizing_permission_claim(string permission) {
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
        string token = CreateJwt(
            "admin-user",
            claims:
            [
                new Claim("eventstore:tenant", "system"),
                new Claim("eventstore:permission", permission),
            ]);
        using HttpClient client = CreateClientWithBearer(factory, token);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateBootstrapRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        _ = await router.Received(1).RouteCommandAsync(
            Arg.Any<Hexalith.EventStore.Server.Pipeline.Commands.SubmitCommand>(),
            Arg.Any<CancellationToken>());
    }

    // AUTH-INT-005 — Pins ClaimsRbacValidator.cs deny shape: an unrelated permission must not
    // authorize a different command type, and duplicate non-matching claims cannot accumulate
    // elevation (the validator uses boolean OR over case-insensitive equality). reasonCode
    // string is the canonical AuthorizationFailureReasonExtensions.InsufficientPermission
    // mapping ("insufficient_permission"), verified 2026-05-20 first run. Source: 11-2 review
    // deferred-work — "duplicate eventstore:permission do not accumulate elevation".
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Commands_endpoint_returns_403_when_jwt_carries_only_unrelated_permission_claims(bool duplicate) {
        ICommandRouter router = Substitute.For<ICommandRouter>();

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            Substitute.For<ICommandStatusStore>(),
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        var claims = new List<Claim> {
            new("eventstore:tenant", "system"),
            new("eventstore:permission", nameof(CreateTenant)),
        };
        if (duplicate) {
            claims.Add(new Claim("eventstore:permission", nameof(CreateTenant)));
        }

        string token = CreateJwt("admin-user", claims: claims);
        using HttpClient client = CreateClientWithBearer(factory, token);
        // Submit BootstrapGlobalAdmin while only the unrelated CreateTenant permission is granted.
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateBootstrapRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Status.ShouldBe(403);
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("insufficient_permission");
        await router.DidNotReceiveWithAnyArgs().RouteCommandAsync(default!, default);
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

    // Companion to CreateJwt — produces a token WITHOUT a `sub` claim so tests can pin the
    // contract "name is not promoted to trusted subject in the absence of sub". Kept separate
    // from CreateJwt so the default `sub`-included path stays single-purpose.
    private static string CreateJwtWithoutSub(IEnumerable<Claim>? claims = null) {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var tokenClaims = new List<Claim>();
        if (claims is not null) {
            tokenClaims.AddRange(claims);
        }

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: tokenClaims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest CreateBootstrapRequest(string tenant = "system") {
        JsonElement payload = JsonSerializer.SerializeToElement(new BootstrapGlobalAdmin("admin-1"));
        return new Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            tenant,
            "tenants",
            "global-administrators",
            nameof(BootstrapGlobalAdmin),
            payload);
    }

    private static Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest CreateCreateTenantRequest(
        Dictionary<string, string>? extensions = null) {
        JsonElement payload = JsonSerializer.SerializeToElement(new CreateTenant("acme", "Acme Corp", "Tenant from command API"));
        return new Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "system",
            "tenants",
            "acme",
            nameof(CreateTenant),
            payload,
            Extensions: extensions);
    }

    private static Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest CreateUpdateTenantRequest(
        Dictionary<string, string>? extensions = null) {
        JsonElement payload = JsonSerializer.SerializeToElement(new UpdateTenant("acme", "Acme Updated", "Updated tenant metadata"));
        return new Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "system",
            "tenants",
            "acme",
            nameof(UpdateTenant),
            payload,
            Extensions: extensions);
    }

    private static Dictionary<string, string> GlobalAdminExtensions()
        => new(StringComparer.OrdinalIgnoreCase) { [GlobalAdminExtensionKey] = "true" };

    // P15: default is `false` so any new tenant-claim-sensitive test that omits the flag exercises
    // the real JwtBearer + EventStoreClaimsTransformation pipeline. Existing tests that depend on
    // the hard-coded `eventstore:tenant=system` TestAuthHandler principal must pass
    // `useTestAuthentication: true` explicitly.
    private sealed class CommandApiWebApplicationFactory(
        ICommandRouter? router = null,
        ICommandStatusStore? statusStore = null,
        ICommandArchiveStore? archiveStore = null,
        bool useTestAuthentication = false) : WebApplicationFactory<TenantBootstrapOptions> {
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
