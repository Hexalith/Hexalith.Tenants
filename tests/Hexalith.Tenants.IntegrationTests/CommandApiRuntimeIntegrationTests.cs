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
using Hexalith.EventStore.Contracts.Problems;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Models;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Commands;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Server.Aggregates;

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
    private const string DomainRejectionProblemTypeBase = "https://hexalith.io/problems/domain-rejections";
    private static readonly JsonSerializerOptions ProblemDetailsJsonOptions = new(JsonSerializerDefaults.Web);

    public static IEnumerable<object[]> TenantRejectionProblemDetailsExpectations() {
        yield return [typeof(TenantNotFoundRejection), HttpStatusCode.NotFound, "tenant-not-found-rejection"];
        yield return [typeof(GlobalAdministratorNotFoundRejection), HttpStatusCode.NotFound, "global-administrator-not-found-rejection"];
        yield return [typeof(TenantAlreadyExistsRejection), HttpStatusCode.Conflict, "tenant-already-exists-rejection"];
        yield return [typeof(TenantLifecycleStateAlreadySetRejection), HttpStatusCode.Conflict, "tenant-lifecycle-state-already-set-rejection"];
        yield return [typeof(UserAlreadyInTenantRejection), HttpStatusCode.Conflict, "user-already-in-tenant-rejection"];
        yield return [typeof(GlobalAdminAlreadyBootstrappedRejection), HttpStatusCode.Conflict, "global-admin-already-bootstrapped-rejection"];
        yield return [typeof(GlobalAdministratorAlreadyExistsRejection), HttpStatusCode.Conflict, "global-administrator-already-exists-rejection"];
        yield return [typeof(TenantDisabledRejection), HttpStatusCode.UnprocessableEntity, "tenant-disabled-rejection"];
        yield return [typeof(InsufficientPermissionsRejection), HttpStatusCode.UnprocessableEntity, "insufficient-permissions-rejection"];
        yield return [typeof(RoleEscalationRejection), HttpStatusCode.UnprocessableEntity, "role-escalation-rejection"];
        yield return [typeof(ConfigurationLimitExceededRejection), HttpStatusCode.UnprocessableEntity, "configuration-limit-exceeded-rejection"];
        yield return [typeof(ConfigurationKeyNotFoundRejection), HttpStatusCode.NotFound, "configuration-key-not-found-rejection"];
        yield return [typeof(UserNotInTenantRejection), HttpStatusCode.UnprocessableEntity, "user-not-in-tenant-rejection"];
        yield return [typeof(LastGlobalAdministratorRejection), HttpStatusCode.UnprocessableEntity, "last-global-administrator-rejection"];
    }

    public static IEnumerable<object[]> ReaderRejectedTenantStateChangingCommands() {
        yield return [
            nameof(UpdateTenant),
            JsonSerializer.SerializeToUtf8Bytes(new UpdateTenant("acme", "Acme Updated", "Reader attempt")),
            nameof(TenantUpdated),
        ];
        yield return [
            nameof(AddUserToTenant),
            JsonSerializer.SerializeToUtf8Bytes(new AddUserToTenant("acme", "new-user", TenantRole.TenantReader)),
            nameof(UserAddedToTenant),
        ];
        yield return [
            nameof(RemoveUserFromTenant),
            JsonSerializer.SerializeToUtf8Bytes(new RemoveUserFromTenant("acme", "contributor-user")),
            nameof(UserRemovedFromTenant),
        ];
        yield return [
            nameof(ChangeUserRole),
            JsonSerializer.SerializeToUtf8Bytes(new ChangeUserRole("acme", "contributor-user", TenantRole.TenantOwner)),
            nameof(UserRoleChanged),
        ];
        yield return [
            nameof(SetTenantConfiguration),
            JsonSerializer.SerializeToUtf8Bytes(new SetTenantConfiguration("acme", "feature.reader", "denied")),
            nameof(TenantConfigurationSet),
        ];
        yield return [
            nameof(RemoveTenantConfiguration),
            JsonSerializer.SerializeToUtf8Bytes(new RemoveTenantConfiguration("acme", "feature.enabled")),
            nameof(TenantConfigurationRemoved),
        ];
    }

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
    public async Task Process_endpoint_dispatches_RemoveUserFromTenant_with_current_state() {
        await using var factory = new CommandApiWebApplicationFactory(useTestAuthentication: true);
        using HttpClient client = factory.CreateClient();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Tenant from /process", DateTimeOffset.UtcNow));
        state.Apply(new UserAddedToTenant("acme", "owner-user", TenantRole.TenantOwner));
        state.Apply(new UserAddedToTenant("acme", "reader-user", TenantRole.TenantReader));

        var request = new DomainServiceRequest(
            new CommandEnvelope(
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                "system",
                "tenants",
                "acme",
                nameof(RemoveUserFromTenant),
                JsonSerializer.SerializeToUtf8Bytes(new RemoveUserFromTenant("acme", "reader-user")),
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                null,
                "owner-user",
                null),
            state);

        HttpResponseMessage response = await client.PostAsJsonAsync("/process", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DomainServiceWireResult? result = await response.Content.ReadFromJsonAsync<DomainServiceWireResult>();
        _ = result.ShouldNotBeNull();
        result.IsRejection.ShouldBeFalse();
        result.Events.Count.ShouldBe(1);
        result.Events[0].EventTypeName.ShouldEndWith(nameof(UserRemovedFromTenant));
        UserRemovedFromTenant? payload = JsonSerializer.Deserialize<UserRemovedFromTenant>(result.Events[0].Payload);
        _ = payload.ShouldNotBeNull();
        payload.TenantId.ShouldBe("acme");
        payload.UserId.ShouldBe("reader-user");
    }

    [Fact]
    public async Task Process_endpoint_dispatches_ChangeUserRole_with_current_state() {
        await using var factory = new CommandApiWebApplicationFactory(useTestAuthentication: true);
        using HttpClient client = factory.CreateClient();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Tenant from /process", DateTimeOffset.UtcNow));
        state.Apply(new UserAddedToTenant("acme", "owner-user", TenantRole.TenantOwner));
        state.Apply(new UserAddedToTenant("acme", "reader-user", TenantRole.TenantReader));

        var request = new DomainServiceRequest(
            new CommandEnvelope(
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                "system",
                "tenants",
                "acme",
                nameof(ChangeUserRole),
                JsonSerializer.SerializeToUtf8Bytes(new ChangeUserRole("acme", "reader-user", TenantRole.TenantContributor)),
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                null,
                "owner-user",
                null),
            state);

        HttpResponseMessage response = await client.PostAsJsonAsync("/process", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DomainServiceWireResult? result = await response.Content.ReadFromJsonAsync<DomainServiceWireResult>();
        _ = result.ShouldNotBeNull();
        result.IsRejection.ShouldBeFalse();
        result.Events.Count.ShouldBe(1);
        result.Events[0].EventTypeName.ShouldEndWith(nameof(UserRoleChanged));
        UserRoleChanged? payload = JsonSerializer.Deserialize<UserRoleChanged>(result.Events[0].Payload);
        _ = payload.ShouldNotBeNull();
        payload.TenantId.ShouldBe("acme");
        payload.UserId.ShouldBe("reader-user");
        payload.OldRole.ShouldBe(TenantRole.TenantReader);
        payload.NewRole.ShouldBe(TenantRole.TenantContributor);
    }

    [Theory]
    [MemberData(nameof(ReaderRejectedTenantStateChangingCommands))]
    public async Task Process_endpoint_rejects_reader_for_tenant_state_changing_commands(
        string commandType,
        byte[] payload,
        string successEventTypeName) {
        await using var factory = new CommandApiWebApplicationFactory(useTestAuthentication: true);
        using HttpClient client = factory.CreateClient();
        TenantState state = CreateRoleBehaviorState();
        DomainServiceRequest request = CreateProcessRequest(commandType, payload, state, "reader-user");

        HttpResponseMessage response = await client.PostAsJsonAsync("/process", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DomainServiceWireResult result = await ReadDomainResultAsync(response);
        result.IsRejection.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        result.Events[0].EventTypeName.ShouldEndWith(nameof(InsufficientPermissionsRejection));
        result.Events.ShouldNotContain(e => e.EventTypeName.EndsWith(successEventTypeName, StringComparison.Ordinal));
        InsufficientPermissionsRejection rejection = DeserializeEvent<InsufficientPermissionsRejection>(result);
        rejection.TenantId.ShouldBe("acme");
        rejection.ActorUserId.ShouldBe("reader-user");
        rejection.ActorRole.ShouldBe(TenantRole.TenantReader);
        rejection.CommandName.ShouldBe(commandType);
    }

    [Fact]
    public async Task Process_endpoint_applies_contributor_and_owner_role_boundaries() {
        await using var factory = new CommandApiWebApplicationFactory(useTestAuthentication: true);
        using HttpClient client = factory.CreateClient();

        DomainServiceWireResult contributorUpdate = await PostProcessAsync(
            client,
            CreateProcessRequest(
                nameof(UpdateTenant),
                JsonSerializer.SerializeToUtf8Bytes(new UpdateTenant("acme", "Acme Contributor Update", null)),
                CreateRoleBehaviorState(),
                "contributor-user"));
        contributorUpdate.IsRejection.ShouldBeFalse();
        contributorUpdate.Events[0].EventTypeName.ShouldEndWith(nameof(TenantUpdated));

        DomainServiceWireResult contributorMembership = await PostProcessAsync(
            client,
            CreateProcessRequest(
                nameof(AddUserToTenant),
                JsonSerializer.SerializeToUtf8Bytes(new AddUserToTenant("acme", "contributor-added-user", TenantRole.TenantReader)),
                CreateRoleBehaviorState(),
                "contributor-user"));
        contributorMembership.IsRejection.ShouldBeTrue();
        DeserializeEvent<InsufficientPermissionsRejection>(contributorMembership).ActorRole.ShouldBe(TenantRole.TenantContributor);

        DomainServiceWireResult contributorConfiguration = await PostProcessAsync(
            client,
            CreateProcessRequest(
                nameof(SetTenantConfiguration),
                JsonSerializer.SerializeToUtf8Bytes(new SetTenantConfiguration("acme", "feature.contributor", "denied")),
                CreateRoleBehaviorState(),
                "contributor-user"));
        contributorConfiguration.IsRejection.ShouldBeTrue();
        DeserializeEvent<InsufficientPermissionsRejection>(contributorConfiguration).ActorRole.ShouldBe(TenantRole.TenantContributor);

        DomainServiceWireResult ownerMembership = await PostProcessAsync(
            client,
            CreateProcessRequest(
                nameof(AddUserToTenant),
                JsonSerializer.SerializeToUtf8Bytes(new AddUserToTenant("acme", "owner-added-user", TenantRole.TenantReader)),
                CreateRoleBehaviorState(),
                "owner-user"));
        ownerMembership.IsRejection.ShouldBeFalse();
        DeserializeEvent<UserAddedToTenant>(ownerMembership).UserId.ShouldBe("owner-added-user");

        DomainServiceWireResult ownerConfiguration = await PostProcessAsync(
            client,
            CreateProcessRequest(
                nameof(SetTenantConfiguration),
                JsonSerializer.SerializeToUtf8Bytes(new SetTenantConfiguration("acme", "feature.owner", "allowed")),
                CreateRoleBehaviorState(),
                "owner-user"));
        ownerConfiguration.IsRejection.ShouldBeFalse();
        DeserializeEvent<TenantConfigurationSet>(ownerConfiguration).Key.ShouldBe("feature.owner");
    }

    [Fact]
    public async Task Process_endpoint_keeps_owner_authority_scoped_to_envelope_aggregate_tenant() {
        await using var factory = new CommandApiWebApplicationFactory(useTestAuthentication: true);
        using HttpClient client = factory.CreateClient();
        TenantState targetTenantState = CreateRoleBehaviorState();
        DomainServiceRequest request = CreateProcessRequest(
            nameof(AddUserToTenant),
            JsonSerializer.SerializeToUtf8Bytes(new AddUserToTenant("body-tenant", "scoped-user", TenantRole.TenantReader)),
            targetTenantState,
            "owner-user",
            aggregateId: "acme");

        DomainServiceWireResult result = await PostProcessAsync(client, request);

        result.IsRejection.ShouldBeFalse();
        UserAddedToTenant payload = DeserializeEvent<UserAddedToTenant>(result);
        payload.TenantId.ShouldBe("acme");
        payload.UserId.ShouldBe("scoped-user");
        targetTenantState.Users.ShouldNotContainKey("scoped-user");
    }

    [Fact]
    public async Task Process_endpoint_allows_trusted_global_admin_envelope_bypass_without_tenant_membership() {
        await using var factory = new CommandApiWebApplicationFactory(useTestAuthentication: true);
        using HttpClient client = factory.CreateClient();
        DomainServiceRequest request = CreateProcessRequest(
            nameof(SetTenantConfiguration),
            JsonSerializer.SerializeToUtf8Bytes(new SetTenantConfiguration("acme", "feature.global-admin", "allowed")),
            CreateRoleBehaviorState(),
            "external-global-admin",
            extensions: GlobalAdminExtensions());

        DomainServiceWireResult result = await PostProcessAsync(client, request);

        result.IsRejection.ShouldBeFalse();
        TenantConfigurationSet payload = DeserializeEvent<TenantConfigurationSet>(result);
        payload.TenantId.ShouldBe("acme");
        payload.Key.ShouldBe("feature.global-admin");
    }

    [Fact]
    public async Task Process_endpoint_rejects_101st_configuration_key_with_structured_limit_payload() {
        await using var factory = new CommandApiWebApplicationFactory(useTestAuthentication: true);
        using HttpClient client = factory.CreateClient();
        TenantState state = CreateRoleBehaviorStateWithConfigurationCount(100);
        DomainServiceRequest request = CreateProcessRequest(
            nameof(SetTenantConfiguration),
            JsonSerializer.SerializeToUtf8Bytes(new SetTenantConfiguration("acme", "feature.101", "denied")),
            state,
            "owner-user");

        DomainServiceWireResult result = await PostProcessAsync(client, request);

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldNotContain(e => e.EventTypeName.EndsWith(nameof(TenantConfigurationSet), StringComparison.Ordinal));
        ConfigurationLimitExceededRejection rejection = DeserializeEvent<ConfigurationLimitExceededRejection>(result);
        rejection.TenantId.ShouldBe("acme");
        rejection.LimitType.ShouldBe("KeyCount");
        rejection.CurrentCount.ShouldBe(100);
        rejection.MaxAllowed.ShouldBe(100);
        state.Configuration.Count.ShouldBe(100);
    }

    [Fact]
    public async Task Process_endpoint_rejects_oversized_configuration_key_with_structured_limit_payload() {
        await using var factory = new CommandApiWebApplicationFactory(useTestAuthentication: true);
        using HttpClient client = factory.CreateClient();
        string longKey = new('k', 257);
        DomainServiceRequest request = CreateProcessRequest(
            nameof(SetTenantConfiguration),
            JsonSerializer.SerializeToUtf8Bytes(new SetTenantConfiguration("acme", longKey, "value")),
            CreateRoleBehaviorState(),
            "owner-user");

        DomainServiceWireResult result = await PostProcessAsync(client, request);

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldNotContain(e => e.EventTypeName.EndsWith(nameof(TenantConfigurationSet), StringComparison.Ordinal));
        ConfigurationLimitExceededRejection rejection = DeserializeEvent<ConfigurationLimitExceededRejection>(result);
        rejection.TenantId.ShouldBe("acme");
        rejection.LimitType.ShouldBe("KeyLength");
        rejection.CurrentCount.ShouldBe(257);
        rejection.MaxAllowed.ShouldBe(256);
    }

    [Fact]
    public async Task Process_endpoint_rejects_oversized_configuration_value_without_storing_value() {
        await using var factory = new CommandApiWebApplicationFactory(useTestAuthentication: true);
        using HttpClient client = factory.CreateClient();
        string longValue = new('v', 1025);
        DomainServiceRequest request = CreateProcessRequest(
            nameof(SetTenantConfiguration),
            JsonSerializer.SerializeToUtf8Bytes(new SetTenantConfiguration("acme", "feature.large", longValue)),
            CreateRoleBehaviorState(),
            "owner-user");

        DomainServiceWireResult result = await PostProcessAsync(client, request);

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldNotContain(e => e.EventTypeName.EndsWith(nameof(TenantConfigurationSet), StringComparison.Ordinal));
        ConfigurationLimitExceededRejection rejection = DeserializeEvent<ConfigurationLimitExceededRejection>(result);
        rejection.TenantId.ShouldBe("acme");
        rejection.LimitType.ShouldBe("ValueSize");
        rejection.CurrentCount.ShouldBe(1025);
        rejection.MaxAllowed.ShouldBe(1024);
        Encoding.UTF8.GetString(result.Events[0].Payload).ShouldNotContain(longValue);
    }

    [Fact]
    public async Task Process_endpoint_removes_existing_tenant_configuration_for_owner() {
        await using var factory = new CommandApiWebApplicationFactory(useTestAuthentication: true);
        using HttpClient client = factory.CreateClient();
        DomainServiceRequest request = CreateProcessRequest(
            nameof(RemoveTenantConfiguration),
            JsonSerializer.SerializeToUtf8Bytes(new RemoveTenantConfiguration("acme", "feature.enabled")),
            CreateRoleBehaviorState(),
            "owner-user");

        DomainServiceWireResult result = await PostProcessAsync(client, request);

        result.IsRejection.ShouldBeFalse();
        TenantConfigurationRemoved payload = DeserializeEvent<TenantConfigurationRemoved>(result);
        payload.TenantId.ShouldBe("acme");
        payload.Key.ShouldBe("feature.enabled");
    }

    [Fact]
    public async Task Process_endpoint_rejects_missing_tenant_configuration_key_for_owner() {
        await using var factory = new CommandApiWebApplicationFactory(useTestAuthentication: true);
        using HttpClient client = factory.CreateClient();
        DomainServiceRequest request = CreateProcessRequest(
            nameof(RemoveTenantConfiguration),
            JsonSerializer.SerializeToUtf8Bytes(new RemoveTenantConfiguration("acme", "feature.missing")),
            CreateRoleBehaviorState(),
            "owner-user");

        DomainServiceWireResult result = await PostProcessAsync(client, request);

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldNotContain(e => e.EventTypeName.EndsWith(nameof(TenantConfigurationRemoved), StringComparison.Ordinal));
        ConfigurationKeyNotFoundRejection rejection = DeserializeEvent<ConfigurationKeyNotFoundRejection>(result);
        rejection.TenantId.ShouldBe("acme");
        rejection.Key.ShouldBe("feature.missing");
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
    public async Task Commands_endpoint_accepts_AddUserToTenant_and_routes_story_payload() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        SubmitPipelineCommand? capturedCommand = null;
        _ = router.RouteCommandAsync(Arg.Do<SubmitPipelineCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(true, null, "add-user-correlation"));

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
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateAddUserToTenantRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        CommandApiResponse? body = await response.Content.ReadFromJsonAsync<CommandApiResponse>();
        _ = body.ShouldNotBeNull();
        body.CorrelationId.ShouldBe(request.MessageId);
        _ = capturedCommand.ShouldNotBeNull();
        capturedCommand.Tenant.ShouldBe("system");
        capturedCommand.Domain.ShouldBe("tenants");
        capturedCommand.AggregateId.ShouldBe("acme");
        capturedCommand.CommandType.ShouldBe(nameof(AddUserToTenant));
        capturedCommand.UserId.ShouldBe("global-admin");
        capturedCommand.IsGlobalAdmin.ShouldBeTrue();
        AddUserToTenant? payload = JsonSerializer.Deserialize<AddUserToTenant>(capturedCommand.Payload);
        _ = payload.ShouldNotBeNull();
        payload.TenantId.ShouldBe("acme");
        payload.UserId.ShouldBe("alice");
        payload.Role.ShouldBe(TenantRole.TenantContributor);
    }

    [Fact]
    public async Task Commands_endpoint_accepts_RemoveUserFromTenant_and_routes_story_payload() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        SubmitPipelineCommand? capturedCommand = null;
        _ = router.RouteCommandAsync(Arg.Do<SubmitPipelineCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(true, null, "remove-user-correlation"));

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
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateRemoveUserFromTenantRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        CommandApiResponse? body = await response.Content.ReadFromJsonAsync<CommandApiResponse>();
        _ = body.ShouldNotBeNull();
        body.CorrelationId.ShouldBe(request.MessageId);
        _ = capturedCommand.ShouldNotBeNull();
        capturedCommand.Tenant.ShouldBe("system");
        capturedCommand.Domain.ShouldBe("tenants");
        capturedCommand.AggregateId.ShouldBe("acme");
        capturedCommand.CommandType.ShouldBe(nameof(RemoveUserFromTenant));
        capturedCommand.UserId.ShouldBe("global-admin");
        capturedCommand.IsGlobalAdmin.ShouldBeTrue();
        RemoveUserFromTenant? payload = JsonSerializer.Deserialize<RemoveUserFromTenant>(capturedCommand.Payload);
        _ = payload.ShouldNotBeNull();
        payload.TenantId.ShouldBe("acme");
        payload.UserId.ShouldBe("alice");
    }

    [Fact]
    public async Task Commands_endpoint_accepts_ChangeUserRole_and_routes_story_payload() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        SubmitPipelineCommand? capturedCommand = null;
        _ = router.RouteCommandAsync(Arg.Do<SubmitPipelineCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(true, null, "change-role-correlation"));

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
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateChangeUserRoleRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        CommandApiResponse? body = await response.Content.ReadFromJsonAsync<CommandApiResponse>();
        _ = body.ShouldNotBeNull();
        body.CorrelationId.ShouldBe(request.MessageId);
        _ = capturedCommand.ShouldNotBeNull();
        capturedCommand.Tenant.ShouldBe("system");
        capturedCommand.Domain.ShouldBe("tenants");
        capturedCommand.AggregateId.ShouldBe("acme");
        capturedCommand.CommandType.ShouldBe(nameof(ChangeUserRole));
        capturedCommand.UserId.ShouldBe("global-admin");
        capturedCommand.IsGlobalAdmin.ShouldBeTrue();
        ChangeUserRole? payload = JsonSerializer.Deserialize<ChangeUserRole>(capturedCommand.Payload);
        _ = payload.ShouldNotBeNull();
        payload.TenantId.ShouldBe("acme");
        payload.UserId.ShouldBe("alice");
        payload.NewRole.ShouldBe(TenantRole.TenantOwner);
    }

    [Fact]
    public async Task Commands_endpoint_accepts_SetTenantConfiguration_and_routes_story_payload() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        SubmitPipelineCommand? capturedCommand = null;
        _ = router.RouteCommandAsync(Arg.Do<SubmitPipelineCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(true, null, "set-config-correlation"));

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
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateSetTenantConfigurationRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        CommandApiResponse? body = await response.Content.ReadFromJsonAsync<CommandApiResponse>();
        _ = body.ShouldNotBeNull();
        body.CorrelationId.ShouldBe(request.MessageId);
        _ = capturedCommand.ShouldNotBeNull();
        capturedCommand.Tenant.ShouldBe("system");
        capturedCommand.Domain.ShouldBe("tenants");
        capturedCommand.AggregateId.ShouldBe("acme");
        capturedCommand.CommandType.ShouldBe(nameof(SetTenantConfiguration));
        capturedCommand.UserId.ShouldBe("global-admin");
        capturedCommand.IsGlobalAdmin.ShouldBeTrue();
        SetTenantConfiguration? payload = JsonSerializer.Deserialize<SetTenantConfiguration>(capturedCommand.Payload);
        _ = payload.ShouldNotBeNull();
        payload.TenantId.ShouldBe("acme");
        payload.Key.ShouldBe("billing.plan");
        payload.Value.ShouldBe("enterprise");
    }

    [Theory]
    [InlineData("", "value", "payload.Key")]
    [InlineData("feature.large", "vvvvvvvvvv", "payload.Value")]
    public async Task Commands_endpoint_rejects_invalid_SetTenantConfiguration_payload_before_routing(
        string key,
        string valueSeed,
        string expectedErrorKey) {
        ArgumentNullException.ThrowIfNull(valueSeed);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedErrorKey);

        ICommandRouter router = Substitute.For<ICommandRouter>();

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            Substitute.For<ICommandStatusStore>(),
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
        string value = expectedErrorKey == "payload.Value" ? new string(valueSeed[0], 1025) : valueSeed;
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request =
            CreateSetTenantConfigurationRequest(JsonSerializer.SerializeToElement(new SetTenantConfiguration("acme", key, value)));

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);
        string problemJson = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        problemJson.ShouldContain(expectedErrorKey);
        ProblemDetails? details = JsonSerializer.Deserialize<ProblemDetails>(problemJson, ProblemDetailsJsonOptions);
        _ = details.ShouldNotBeNull();
        details.Title.ShouldBe("Command Validation Failed");
        details.Status.ShouldBe(400);
        details.Type.ShouldBe("https://hexalith.io/problems/validation-error");
        await router.DidNotReceiveWithAnyArgs().RouteCommandAsync(default!, default);
    }

    [Fact]
    public async Task Commands_endpoint_returns_422_problem_details_for_AddUserToTenant_role_escalation() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<SubmitPipelineCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(false, "Domain rejection: RoleEscalationRejection", "add-user-role-escalation"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(
                CommandStatus.Rejected,
                DateTimeOffset.UtcNow,
                "acme",
                1,
                typeof(RoleEscalationRejection).FullName,
                null,
                null));

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            statusStore,
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        using HttpClient client = CreateJwtClient(factory);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateAddUserToTenantRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Title.ShouldBe("Role Escalation Rejection");
        details.Status.ShouldBe(422);
        details.Type.ShouldBe("https://hexalith.io/problems/domain-rejections/role-escalation-rejection");
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("role-escalation-rejection");
        details.Extensions.ShouldContainKey("rejectionType");
        details.Extensions["rejectionType"]?.ToString().ShouldBe(typeof(RoleEscalationRejection).FullName);
    }

    [Fact]
    public async Task Commands_endpoint_returns_422_problem_details_for_ChangeUserRole_role_escalation() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<SubmitPipelineCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(false, "Domain rejection: RoleEscalationRejection", "change-role-escalation"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(
                CommandStatus.Rejected,
                DateTimeOffset.UtcNow,
                "acme",
                1,
                typeof(RoleEscalationRejection).FullName,
                null,
                null));

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            statusStore,
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        using HttpClient client = CreateJwtClient(factory);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateChangeUserRoleRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Title.ShouldBe("Role Escalation Rejection");
        details.Status.ShouldBe(422);
        details.Type.ShouldBe("https://hexalith.io/problems/domain-rejections/role-escalation-rejection");
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("role-escalation-rejection");
        details.Extensions.ShouldContainKey("rejectionType");
        details.Extensions["rejectionType"]?.ToString().ShouldBe(typeof(RoleEscalationRejection).FullName);
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
    public async Task Commands_endpoint_accepts_DisableTenant_and_routes_story_payload() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        SubmitPipelineCommand? capturedCommand = null;
        _ = router.RouteCommandAsync(Arg.Do<SubmitPipelineCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(true, null, "disable-correlation"));

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
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateDisableTenantRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        CommandApiResponse? body = await response.Content.ReadFromJsonAsync<CommandApiResponse>();
        _ = body.ShouldNotBeNull();
        body.CorrelationId.ShouldBe(request.MessageId);
        _ = capturedCommand.ShouldNotBeNull();
        capturedCommand.Tenant.ShouldBe("system");
        capturedCommand.Domain.ShouldBe("tenants");
        capturedCommand.AggregateId.ShouldBe("acme");
        capturedCommand.CommandType.ShouldBe(nameof(DisableTenant));
        capturedCommand.UserId.ShouldBe("global-admin");
        capturedCommand.IsGlobalAdmin.ShouldBeTrue();
        DisableTenant? payload = JsonSerializer.Deserialize<DisableTenant>(capturedCommand.Payload);
        _ = payload.ShouldNotBeNull();
        payload.TenantId.ShouldBe("acme");
    }

    [Fact]
    public async Task Commands_endpoint_accepts_EnableTenant_and_routes_story_payload() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        SubmitPipelineCommand? capturedCommand = null;
        _ = router.RouteCommandAsync(Arg.Do<SubmitPipelineCommand>(c => capturedCommand = c), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(true, null, "enable-correlation"));

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
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateEnableTenantRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        CommandApiResponse? body = await response.Content.ReadFromJsonAsync<CommandApiResponse>();
        _ = body.ShouldNotBeNull();
        body.CorrelationId.ShouldBe(request.MessageId);
        _ = capturedCommand.ShouldNotBeNull();
        capturedCommand.Tenant.ShouldBe("system");
        capturedCommand.Domain.ShouldBe("tenants");
        capturedCommand.AggregateId.ShouldBe("acme");
        capturedCommand.CommandType.ShouldBe(nameof(EnableTenant));
        capturedCommand.UserId.ShouldBe("global-admin");
        capturedCommand.IsGlobalAdmin.ShouldBeTrue();
        EnableTenant? payload = JsonSerializer.Deserialize<EnableTenant>(capturedCommand.Payload);
        _ = payload.ShouldNotBeNull();
        payload.TenantId.ShouldBe("acme");
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
    public async Task Commands_endpoint_returns_409_problem_details_for_duplicate_AddUserToTenant() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<SubmitPipelineCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(false, "Domain rejection: UserAlreadyInTenantRejection", "add-user-duplicate"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(
                CommandStatus.Rejected,
                DateTimeOffset.UtcNow,
                "acme",
                1,
                typeof(UserAlreadyInTenantRejection).FullName,
                null,
                null));

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            statusStore,
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        using HttpClient client = CreateJwtClient(factory);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateAddUserToTenantRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Title.ShouldBe("User Already In Tenant Rejection");
        details.Status.ShouldBe(409);
        details.Type.ShouldBe("https://hexalith.io/problems/domain-rejections/user-already-in-tenant-rejection");
        details.Extensions.ShouldContainKey("correlationId");
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("user-already-in-tenant-rejection");
        details.Extensions.ShouldContainKey("rejectionType");
        details.Extensions["rejectionType"]?.ToString().ShouldBe(typeof(UserAlreadyInTenantRejection).FullName);
    }

    [Fact]
    public async Task Commands_endpoint_returns_422_problem_details_for_RemoveUserFromTenant_when_user_is_not_in_tenant() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<SubmitPipelineCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(false, "Domain rejection: UserNotInTenantRejection", "remove-user-not-member"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(
                CommandStatus.Rejected,
                DateTimeOffset.UtcNow,
                "acme",
                1,
                typeof(UserNotInTenantRejection).FullName,
                null,
                null));

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            statusStore,
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        using HttpClient client = CreateJwtClient(factory);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateRemoveUserFromTenantRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Title.ShouldBe("User Not In Tenant Rejection");
        details.Status.ShouldBe(422);
        details.Type.ShouldBe("https://hexalith.io/problems/domain-rejections/user-not-in-tenant-rejection");
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("user-not-in-tenant-rejection");
        details.Extensions.ShouldContainKey("rejectionType");
        details.Extensions["rejectionType"]?.ToString().ShouldBe(typeof(UserNotInTenantRejection).FullName);
    }

    [Fact]
    public async Task Commands_endpoint_returns_409_problem_details_for_duplicate_lifecycle_state() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<SubmitPipelineCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(false, "Domain rejection: TenantLifecycleStateAlreadySetRejection", "disable-duplicate"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(
                CommandStatus.Rejected,
                DateTimeOffset.UtcNow,
                "acme",
                1,
                typeof(TenantLifecycleStateAlreadySetRejection).FullName,
                null,
                null));

        await using var factory = new CommandApiWebApplicationFactory(
            router,
            statusStore,
            Substitute.For<ICommandArchiveStore>(),
            useTestAuthentication: false);
        using HttpClient client = CreateJwtClient(factory);
        Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest request = CreateDisableTenantRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Title.ShouldBe("Tenant Lifecycle State Already Set Rejection");
        details.Status.ShouldBe(409);
        details.Type.ShouldBe("https://hexalith.io/problems/domain-rejections/tenant-lifecycle-state-already-set-rejection");
        details.Extensions.ShouldContainKey("correlationId");
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("tenant-lifecycle-state-already-set-rejection");
        details.Extensions.ShouldContainKey("rejectionType");
        details.Extensions["rejectionType"]?.ToString().ShouldBe(typeof(TenantLifecycleStateAlreadySetRejection).FullName);
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
    public async Task Commands_endpoint_returns_422_problem_details_for_disabled_tenant_command() {
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<SubmitPipelineCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(false, "Domain rejection: TenantDisabledRejection", "update-disabled"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(
                CommandStatus.Rejected,
                DateTimeOffset.UtcNow,
                "acme",
                1,
                typeof(TenantDisabledRejection).FullName,
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

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Title.ShouldBe("Tenant Disabled Rejection");
        details.Status.ShouldBe(422);
        details.Type.ShouldBe("https://hexalith.io/problems/domain-rejections/tenant-disabled-rejection");
        details.Extensions.ShouldContainKey("reasonCode");
        details.Extensions["reasonCode"]?.ToString().ShouldBe("tenant-disabled-rejection");
        details.Extensions.ShouldContainKey("rejectionType");
        details.Extensions["rejectionType"]?.ToString().ShouldBe(typeof(TenantDisabledRejection).FullName);
    }

    [Fact]
    public void All_tenant_rejection_types_have_explicit_problem_details_expectation() {
        Type[] expectedRejectionTypes = TenantRejectionProblemDetailsExpectations()
            .Select(static expectation => (Type)expectation[0])
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();
        Type[] actualRejectionTypes = typeof(TenantNotFoundRejection).Assembly
            .GetTypes()
            .Where(static type =>
                type.IsClass
                && !type.IsAbstract
                && type.Namespace == "Hexalith.Tenants.Contracts.Events.Rejections"
                && typeof(Hexalith.EventStore.Contracts.Events.IRejectionEvent).IsAssignableFrom(type))
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        expectedRejectionTypes.ShouldBe(actualRejectionTypes);
    }

    [Theory]
    [MemberData(nameof(TenantRejectionProblemDetailsExpectations))]
    public async Task Commands_endpoint_returns_deterministic_problem_details_for_every_tenant_rejection(
        Type rejectionType,
        HttpStatusCode expectedStatusCode,
        string expectedReasonCode) {
        ArgumentNullException.ThrowIfNull(rejectionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedReasonCode);

        const string leakedDetail = "Domain rejection leaked payload {\"tenantId\":\"sensitive-tenant-999\",\"userId\":\"sensitive-user-999\"}; Authorization: Bearer secret-token-123; System.InvalidOperationException at /home/administrator/secret/path";
        ICommandRouter router = Substitute.For<ICommandRouter>();
        _ = router.RouteCommandAsync(Arg.Any<SubmitPipelineCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CommandProcessingResult(false, leakedDetail, "rejection-correlation"));

        ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
        _ = statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CommandStatusRecord(
                CommandStatus.Rejected,
                DateTimeOffset.UtcNow,
                "acme",
                1,
                rejectionType.FullName,
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
        string problemJson = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(expectedStatusCode);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemDetails? details = JsonSerializer.Deserialize<ProblemDetails>(problemJson, ProblemDetailsJsonOptions);
        _ = details.ShouldNotBeNull();
        details.Type.ShouldBe($"{DomainRejectionProblemTypeBase}/{expectedReasonCode}");
        details.Title.ShouldNotBeNullOrWhiteSpace();
        details.Status.ShouldBe((int)expectedStatusCode);
        details.Instance.ShouldBe("/api/v1/commands");
        GetProblemExtension(details, GatewayProblemDetailsExtensions.CorrelationId).ShouldNotBeNullOrWhiteSpace();
        GetProblemExtension(details, GatewayProblemDetailsExtensions.TenantId).ShouldBe("system");
        GetProblemExtension(details, GatewayProblemDetailsExtensions.ReasonCode).ShouldBe(expectedReasonCode);
        GetProblemExtension(details, GatewayProblemDetailsExtensions.RejectionType).ShouldBe(rejectionType.FullName);
        GetProblemExtension(details, GatewayProblemDetailsExtensions.CorrectiveAction).ShouldNotBeNullOrWhiteSpace();

        foreach (string forbiddenValue in SensitiveProblemDetailsLeakMarkers()) {
            problemJson.Contains(forbiddenValue, StringComparison.OrdinalIgnoreCase)
                .ShouldBeFalse($"ProblemDetails leaked '{forbiddenValue}' for {rejectionType.Name}");
        }
    }

    [Fact]
    public async Task Commands_endpoint_rejects_client_supplied_globalAdmin_extension_metadata() {
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

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        ProblemDetails? details = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _ = details.ShouldNotBeNull();
        details.Status.ShouldBe(400);
        details.Title.ShouldBe("Command Validation Failed");
        details.Detail.ShouldBe("Extension key contains invalid characters.");
        await router.DidNotReceiveWithAnyArgs().RouteCommandAsync(default!, default);
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

    private static TenantState CreateRoleBehaviorState() {
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Tenant from /process", DateTimeOffset.UtcNow));
        state.Apply(new UserAddedToTenant("acme", "owner-user", TenantRole.TenantOwner));
        state.Apply(new UserAddedToTenant("acme", "contributor-user", TenantRole.TenantContributor));
        state.Apply(new UserAddedToTenant("acme", "reader-user", TenantRole.TenantReader));
        state.Apply(new TenantConfigurationSet("acme", "feature.enabled", "true"));
        return state;
    }

    private static TenantState CreateRoleBehaviorStateWithConfigurationCount(int keyCount) {
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Tenant from /process", DateTimeOffset.UtcNow));
        state.Apply(new UserAddedToTenant("acme", "owner-user", TenantRole.TenantOwner));
        state.Apply(new UserAddedToTenant("acme", "contributor-user", TenantRole.TenantContributor));
        state.Apply(new UserAddedToTenant("acme", "reader-user", TenantRole.TenantReader));

        for (int i = 0; i < keyCount; i++) {
            state.Apply(new TenantConfigurationSet("acme", $"feature.{i:D3}", $"value-{i}"));
        }

        return state;
    }

    private static DomainServiceRequest CreateProcessRequest(
        string commandType,
        byte[] payload,
        TenantState? state,
        string userId,
        string aggregateId = "acme",
        Dictionary<string, string>? extensions = null)
        => new(
            new CommandEnvelope(
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                "system",
                "tenants",
                aggregateId,
                commandType,
                payload,
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                null,
                userId,
                extensions),
            state);

    private static async Task<DomainServiceWireResult> PostProcessAsync(HttpClient client, DomainServiceRequest request) {
        HttpResponseMessage response = await client.PostAsJsonAsync("/process", request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await ReadDomainResultAsync(response);
    }

    private static async Task<DomainServiceWireResult> ReadDomainResultAsync(HttpResponseMessage response) {
        DomainServiceWireResult? result = await response.Content.ReadFromJsonAsync<DomainServiceWireResult>();
        return result.ShouldNotBeNull();
    }

    private static TEvent DeserializeEvent<TEvent>(DomainServiceWireResult result)
        where TEvent : class {
        result.Events.Count.ShouldBe(1);
        TEvent? payload = JsonSerializer.Deserialize<TEvent>(result.Events[0].Payload);
        return payload.ShouldNotBeNull();
    }

    private static IEnumerable<string> SensitiveProblemDetailsLeakMarkers() {
        yield return "sensitive-tenant-999";
        yield return "sensitive-user-999";
        yield return "secret-token-123";
        yield return "Bearer";
        yield return "System.InvalidOperationException";
        yield return "/home/administrator";
        yield return "Acme Corp";
        yield return "Updated tenant metadata";
    }

    private static string? GetProblemExtension(ProblemDetails details, string key)
        => details.Extensions.TryGetValue(key, out object? value) ? value?.ToString() : null;

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

    private static Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest CreateAddUserToTenantRequest(
        JsonElement? payload = null) {
        JsonElement commandPayload = payload
            ?? JsonSerializer.SerializeToElement(new AddUserToTenant("acme", "alice", TenantRole.TenantContributor));
        return new Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "system",
            "tenants",
            "acme",
            nameof(AddUserToTenant),
            commandPayload);
    }

    private static Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest CreateRemoveUserFromTenantRequest() {
        JsonElement payload = JsonSerializer.SerializeToElement(new RemoveUserFromTenant("acme", "alice"));
        return new Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "system",
            "tenants",
            "acme",
            nameof(RemoveUserFromTenant),
            payload);
    }

    private static Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest CreateChangeUserRoleRequest() {
        JsonElement payload = JsonSerializer.SerializeToElement(new ChangeUserRole("acme", "alice", TenantRole.TenantOwner));
        return new Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "system",
            "tenants",
            "acme",
            nameof(ChangeUserRole),
            payload);
    }

    private static Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest CreateSetTenantConfigurationRequest(
        JsonElement? payload = null) {
        JsonElement commandPayload = payload
            ?? JsonSerializer.SerializeToElement(new SetTenantConfiguration("acme", "billing.plan", "enterprise"));
        return new Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "system",
            "tenants",
            "acme",
            nameof(SetTenantConfiguration),
            commandPayload);
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

    private static Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest CreateDisableTenantRequest() {
        JsonElement payload = JsonSerializer.SerializeToElement(new DisableTenant("acme"));
        return new Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "system",
            "tenants",
            "acme",
            nameof(DisableTenant),
            payload);
    }

    private static Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest CreateEnableTenantRequest() {
        JsonElement payload = JsonSerializer.SerializeToElement(new EnableTenant("acme"));
        return new Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            "system",
            "tenants",
            "acme",
            nameof(EnableTenant),
            payload);
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
