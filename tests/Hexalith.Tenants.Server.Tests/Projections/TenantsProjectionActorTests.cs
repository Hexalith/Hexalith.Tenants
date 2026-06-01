using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Runtime;
using Dapr.Client;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Queries;
using Hexalith.Tenants.Actors;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Contracts.Serialization;
using Hexalith.Tenants.Queries;
using Hexalith.Tenants.Server.Projections;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class TenantsProjectionActorTests {

    private static readonly JsonSerializerOptions _jsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new TenantStatusJsonConverter(), new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    // --- Test Fixtures ---

    [Theory]
    [InlineData("get-tenant", null)]
    [InlineData("get-tenant", "")]
    [InlineData("get-tenant", "   ")]
    [InlineData("list-tenants", null)]
    [InlineData("list-tenants", "")]
    [InlineData("list-tenants", "   ")]
    [InlineData("get-tenant-users", null)]
    [InlineData("get-tenant-users", "")]
    [InlineData("get-tenant-users", "   ")]
    [InlineData("get-user-tenants", null)]
    [InlineData("get-user-tenants", "")]
    [InlineData("get-user-tenants", "   ")]
    [InlineData("get-tenant-audit", null)]
    [InlineData("get-tenant-audit", "")]
    [InlineData("get-tenant-audit", "   ")]
    public async Task RoleSensitiveQuery_with_malformed_user_returns_forbidden_before_state_accessAsync(
        string queryType,
        string? malformedUserId) {
        DaprClient daprClient = Substitute.For<DaprClient>();
        var logger = new ListLogger<TenantsProjectionActor>();

        TenantsProjectionActor actor = CreateActor(daprClient, CreateCursorCodec(), logger);
        QueryEnvelope envelope = CreateEnvelope(queryType, aggregateId: GetAggregateIdForQuery(queryType), entityId: GetEntityIdForQuery(queryType))
            with {
                // Intentional: malformed deserialized/internal envelopes can bypass the public constructor guard.
                UserId = malformedUserId!,
            };

        QueryResult result = await actor.QueryAsync(envelope);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
        await AssertNoProjectionStateReadAsync(daprClient, queryType, envelope.AggregateId);
        logger.Entries.Count(e => e.EventId.Id == 1904).ShouldBe(1);
    }

    [Fact]
    public async Task RoleSensitiveQuery_with_malformed_user_logs_only_safe_contextAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        var logger = new ListLogger<TenantsProjectionActor>();

        TenantsProjectionActor actor = CreateActor(daprClient, CreateCursorCodec(), logger);
        QueryEnvelope envelope = CreateEnvelope("get-tenant", correlationId: "correlation-missing-user")
            with {
                UserId = null!,
            };

        QueryResult result = await actor.QueryAsync(envelope);

        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        LogEntry warning = logger.Entries.Single(e => e.EventId.Id == 1904);
        warning.Level.ShouldBe(LogLevel.Warning);
        warning.State["CorrelationId"].ShouldBe("correlation-missing-user");
        warning.State["QueryType"].ShouldBe("get-tenant");
        warning.State["FailureReason"].ShouldBe(QueryAdapterFailureReason.Forbidden);
        warning.State["Stage"].ShouldBe("TenantQueryEnvelopeAuthorization");
        warning.State.Keys.ShouldNotContain("TenantId");
        warning.State.Keys.ShouldNotContain("UserId");
        warning.State.Keys.ShouldNotContain("AggregateId");
        warning.State.Keys.ShouldNotContain("EntityId");
        warning.State.Keys.ShouldNotContain("Payload");
        warning.Message.ShouldNotContain("tenant-1");
        warning.Message.ShouldNotContain("user-1");
    }

    [Fact]
    public async Task ListTenants_with_malformed_user_and_invalid_cursor_returns_forbidden_before_cursor_validationAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupTenantIndexState(daprClient, CreateTenantIndexModel(1));
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryEnvelope envelope = CreateEnvelope(
            "list-tenants",
            userId: "admin-1",
            aggregateId: "index",
            payload: CreatePaginationPayload(cursor: "not-a-valid-cursor"))
            with {
                UserId = "",
            };

        QueryResult result = await actor.QueryAsync(envelope);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        result.ErrorMessage.ShouldNotBe("Invalid cursor.");
        await AssertNoProjectionStateReadAsync(daprClient, "list-tenants", "index");
    }

    [Fact]
    public async Task Unknown_query_with_malformed_user_keeps_unknown_query_resultAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("unknown-query") with { UserId = null! });

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Unknown query type");
        result.ErrorMessage.ShouldNotBe(QueryAdapterFailureReason.Forbidden);
    }

    [Fact]
    public async Task ListTenants_with_pre_cancelled_token_throws_before_state_accessAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantsProjectionActor actor = CreateActor(daprClient);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(
            () => actor.QueryAsync(CreateEnvelope("list-tenants", aggregateId: "index"), cancellation.Token));

        exception.CancellationToken.ShouldBe(cancellation.Token);
        await AssertNoProjectionStateReadAsync(daprClient, "list-tenants", "index");
    }

    [Fact]
    public async Task ListTenants_passes_received_token_to_projection_state_readsAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        using var cancellation = new CancellationTokenSource();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(2);
        _ = daprClient.GetStateAsync<TenantIndexReadModel>(
                TenantsProjectionActor.StateStoreName,
                TenantsProjectionActor.TenantIndexProjectionKey,
                cancellationToken: cancellation.Token)
            .Returns(Task.FromResult(indexModel)!);
        _ = daprClient.GetStateAsync<GlobalAdministratorReadModel>(
                TenantsProjectionActor.StateStoreName,
                TenantsProjectionActor.GlobalAdminProjectionKey,
                cancellationToken: cancellation.Token)
            .Returns(Task.FromResult(CreateGlobalAdminModel("admin-1"))!);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(
            CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index"),
            cancellation.Token);

        // Defense-in-depth: a wrong-token call would cause the mock to return null,
        // landing in the empty-index Success branch — assert non-empty payload so this
        // test cannot pass for the wrong reason if token identity drifts.
        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? payload = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        _ = payload.ShouldNotBeNull();
        payload.Items.Count.ShouldBe(2);

        _ = await daprClient.Received(1).GetStateAsync<TenantIndexReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantIndexProjectionKey,
            cancellationToken: cancellation.Token);
        _ = await daprClient.Received(1).GetStateAsync<GlobalAdministratorReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.GlobalAdminProjectionKey,
            cancellationToken: cancellation.Token);
    }

    [Fact]
    public async Task GetTenantAudit_cancellation_after_authorization_throws_before_audit_state_readAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        using var cancellation = new CancellationTokenSource();
        _ = daprClient.GetStateAsync<GlobalAdministratorReadModel>(
                TenantsProjectionActor.StateStoreName,
                TenantsProjectionActor.GlobalAdminProjectionKey,
                cancellationToken: cancellation.Token)
            .Returns(_ => {
                cancellation.Cancel();
                return Task.FromResult(CreateGlobalAdminModel("admin-1"))!;
            });

        TenantsProjectionActor actor = CreateActor(daprClient);

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => actor.QueryAsync(CreateEnvelope("get-tenant-audit", userId: "admin-1"), cancellation.Token));

        _ = await daprClient.DidNotReceive().GetStateAsync<TenantAuditReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantAuditProjectionKeyPrefix + "tenant-1",
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTenantAudit_cancellation_after_audit_state_read_does_not_return_partial_pageAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        using var cancellation = new CancellationTokenSource();
        _ = daprClient.GetStateAsync<GlobalAdministratorReadModel>(
                TenantsProjectionActor.StateStoreName,
                TenantsProjectionActor.GlobalAdminProjectionKey,
                cancellationToken: cancellation.Token)
            .Returns(Task.FromResult(CreateGlobalAdminModel("admin-1"))!);
        _ = daprClient.GetStateAsync<TenantAuditReadModel>(
                TenantsProjectionActor.StateStoreName,
                TenantsProjectionActor.TenantAuditProjectionKeyPrefix + "tenant-1",
                cancellationToken: cancellation.Token)
            .Returns(_ => {
                cancellation.Cancel();
                return Task.FromResult(CreateAuditModel(
                    CreateAuditEntry("evt-1", "TenantCreated", AuditEventCategory.Administrative),
                    CreateAuditEntry("evt-2", "TenantUpdated", AuditEventCategory.Administrative)))!;
            });

        TenantsProjectionActor actor = CreateActor(daprClient);

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => actor.QueryAsync(CreateEnvelope("get-tenant-audit", userId: "admin-1"), cancellation.Token));
    }

    // Defense-in-depth: pre-cancelled role-sensitive queries must surface as OperationCanceledException
    // — not as a successful empty payload, Forbidden, Tenant not found, Invalid cursor, ETag conflict,
    // or retry exhaustion (Story 10.3B Tasks line 61 / AC7).
    [Theory]
    [InlineData("get-tenant")]
    [InlineData("list-tenants")]
    [InlineData("get-tenant-users")]
    [InlineData("get-user-tenants")]
    [InlineData("get-tenant-audit")]
    public async Task RoleSensitiveQuery_pre_cancelled_throws_OCE_not_domain_errorAsync(string queryType) {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantsProjectionActor actor = CreateActor(daprClient);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        string aggregateId = GetAggregateIdForQuery(queryType);
        QueryEnvelope envelope = CreateEnvelope(
            queryType,
            userId: "admin-1",
            aggregateId: aggregateId,
            entityId: GetEntityIdForQuery(queryType));

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(
            () => actor.QueryAsync(envelope, cancellation.Token));

        exception.CancellationToken.ShouldBe(cancellation.Token);
        await AssertNoProjectionStateReadAsync(daprClient, queryType, aggregateId);
    }

    // Architectural boundary: CachingProjectionActor.QueryAsync(envelope, token) calls
    // ThrowIfCancellationRequested BEFORE delegating to the derived ExecuteQueryAsync
    // override (Hexalith.EventStore CachingProjectionActor.cs). The Tenants
    // malformed-user → Forbidden short-circuit (TenantsProjectionActor.cs:73-83)
    // precedes the in-actor cancellation checkpoint at line 83, so AC9 cheap-validation
    // precedence applies WHEN ExecuteQueryAsync is reached — but with a pre-cancelled
    // token the base class throws OCE first. This test pins that boundary so any change
    // to base-class precedence surfaces immediately. (Story 10.3B Task line 57: malformed-user
    // precedence is "not externally observable" against a pre-cancelled token.)
    [Theory]
    [InlineData("get-tenant", null)]
    [InlineData("get-tenant", "")]
    [InlineData("list-tenants", "")]
    [InlineData("get-tenant-users", "   ")]
    [InlineData("get-user-tenants", null)]
    [InlineData("get-tenant-audit", "")]
    public async Task RoleSensitiveQuery_pre_cancelled_with_malformed_user_throws_OCE_per_base_actor_precedenceAsync(
        string queryType,
        string? malformedUserId) {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantsProjectionActor actor = CreateActor(daprClient);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        string aggregateId = GetAggregateIdForQuery(queryType);
        QueryEnvelope envelope = CreateEnvelope(
            queryType,
            aggregateId: aggregateId,
            entityId: GetEntityIdForQuery(queryType))
            with {
                UserId = malformedUserId!,
            };

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(
            () => actor.QueryAsync(envelope, cancellation.Token));
        exception.CancellationToken.ShouldBe(cancellation.Token);
        await AssertNoProjectionStateReadAsync(daprClient, queryType, aggregateId);
    }

    [Fact]
    public async Task GetTenant_passes_received_token_to_projection_state_readsAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        using var cancellation = new CancellationTokenSource();
        TenantReadModel model = CreateTenantReadModel(members: new() { ["user-1"] = TenantRole.TenantOwner });
        _ = daprClient.GetStateAsync<TenantReadModel>(
                TenantsProjectionActor.StateStoreName,
                TenantsProjectionActor.TenantProjectionKeyPrefix + "tenant-1",
                cancellationToken: cancellation.Token)
            .Returns(Task.FromResult(model)!);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(
            CreateEnvelope("get-tenant"),
            cancellation.Token);

        result.Success.ShouldBeTrue();
        _ = await daprClient.Received(1).GetStateAsync<TenantReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantProjectionKeyPrefix + "tenant-1",
            cancellationToken: cancellation.Token);
    }

    [Fact]
    public async Task GetTenantUsers_passes_received_token_to_projection_state_readsAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        using var cancellation = new CancellationTokenSource();
        TenantReadModel model = CreateTenantReadModel(members: new() { ["user-1"] = TenantRole.TenantOwner });
        _ = daprClient.GetStateAsync<TenantReadModel>(
                TenantsProjectionActor.StateStoreName,
                TenantsProjectionActor.TenantProjectionKeyPrefix + "tenant-1",
                cancellationToken: cancellation.Token)
            .Returns(Task.FromResult(model)!);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(
            CreateEnvelope("get-tenant-users"),
            cancellation.Token);

        result.Success.ShouldBeTrue();
        _ = await daprClient.Received(1).GetStateAsync<TenantReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantProjectionKeyPrefix + "tenant-1",
            cancellationToken: cancellation.Token);
    }

    [Fact]
    public async Task GetUserTenants_passes_received_token_to_projection_state_readsAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        using var cancellation = new CancellationTokenSource();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(2);
        _ = daprClient.GetStateAsync<TenantIndexReadModel>(
                TenantsProjectionActor.StateStoreName,
                TenantsProjectionActor.TenantIndexProjectionKey,
                cancellationToken: cancellation.Token)
            .Returns(Task.FromResult(indexModel)!);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(
            CreateEnvelope("get-user-tenants", aggregateId: "index", entityId: "user-1"),
            cancellation.Token);

        result.Success.ShouldBeTrue();
        _ = await daprClient.Received(1).GetStateAsync<TenantIndexReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantIndexProjectionKey,
            cancellationToken: cancellation.Token);
    }

    [Fact]
    public async Task GetTenantAudit_passes_received_token_to_projection_state_readsAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        using var cancellation = new CancellationTokenSource();
        _ = daprClient.GetStateAsync<GlobalAdministratorReadModel>(
                TenantsProjectionActor.StateStoreName,
                TenantsProjectionActor.GlobalAdminProjectionKey,
                cancellationToken: cancellation.Token)
            .Returns(Task.FromResult(CreateGlobalAdminModel("admin-1"))!);
        _ = daprClient.GetStateAsync<TenantAuditReadModel>(
                TenantsProjectionActor.StateStoreName,
                TenantsProjectionActor.TenantAuditProjectionKeyPrefix + "tenant-1",
                cancellationToken: cancellation.Token)
            .Returns(Task.FromResult(CreateAuditModel(
                CreateAuditEntry("evt-1", "TenantCreated", AuditEventCategory.Administrative)))!);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(
            CreateEnvelope("get-tenant-audit", userId: "admin-1"),
            cancellation.Token);

        result.Success.ShouldBeTrue();
        _ = await daprClient.Received(1).GetStateAsync<GlobalAdministratorReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.GlobalAdminProjectionKey,
            cancellationToken: cancellation.Token);
        _ = await daprClient.Received(1).GetStateAsync<TenantAuditReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantAuditProjectionKeyPrefix + "tenant-1",
            cancellationToken: cancellation.Token);
    }

    // --- Q6: Authorized user can get tenant details ---
    [Fact]
    public async Task GetTenant_authorized_user_returns_tenant_detailAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantReadModel model = CreateTenantReadModel(members: new() { ["user-1"] = TenantRole.TenantOwner });
        SetupTenantState(daprClient, "tenant-1", model);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant"));

        result.Success.ShouldBeTrue();
        TenantDetail? detail = DeserializePayload<TenantDetail>(result);
        _ = detail.ShouldNotBeNull();
        detail.TenantId.ShouldBe("tenant-1");
        detail.Name.ShouldBe("Test Tenant");
    }

    // --- Q8: GlobalAdmin can access any tenant ---
    [Fact]
    public async Task GetTenant_global_admin_bypasses_membershipAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantReadModel model = CreateTenantReadModel(members: new() { ["user-1"] = TenantRole.TenantOwner });
        SetupTenantState(daprClient, "tenant-1", model);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant", userId: "admin-1"));

        result.Success.ShouldBeTrue();
        TenantDetail? detail = DeserializePayload<TenantDetail>(result);
        _ = detail.ShouldNotBeNull();
        detail.TenantId.ShouldBe("tenant-1");
    }

    // --- Q21: GetTenant with non-existent tenantId ---
    [Fact]
    public async Task GetTenant_non_admin_for_missing_tenant_returns_forbiddenAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        _ = daprClient.GetStateAsync<TenantReadModel>(
            TenantsProjectionActor.StateStoreName,
            Arg.Any<string>())
            .Returns(Task.FromResult<TenantReadModel>(null!)!);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant", aggregateId: "nonexistent"));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
    }

    [Fact]
    public async Task GetTenant_global_admin_for_missing_tenant_returns_not_foundAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        _ = daprClient.GetStateAsync<TenantReadModel>(
            TenantsProjectionActor.StateStoreName,
            Arg.Any<string>())
            .Returns(Task.FromResult<TenantReadModel>(null!)!);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant", userId: "admin-1", aggregateId: "nonexistent"));

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("not found");
    }

    // --- Q7: Unauthorized user gets 403 for GetTenant ---
    [Fact]
    public async Task GetTenant_unauthorized_user_returns_forbiddenAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantReadModel model = CreateTenantReadModel(members: new() { ["user-1"] = TenantRole.TenantOwner });
        SetupTenantState(daprClient, "tenant-1", model);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant", userId: "user-2"));

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Forbidden");
    }

    [Theory]
    [InlineData(TenantRole.TenantReader, true)]
    [InlineData(TenantRole.TenantContributor, true)]
    [InlineData(TenantRole.TenantOwner, true)]
    [InlineData(TenantRole.Unknown, false)]
    public async Task GetTenant_member_authorization_treats_only_concrete_roles_as_membersAsync(
        TenantRole role,
        bool expectedSuccess) {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantReadModel model = CreateTenantReadModel(members: new() { ["user-1"] = role });
        SetupTenantState(daprClient, "tenant-1", model);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant", userId: "user-1"));

        result.Success.ShouldBe(expectedSuccess);
        if (expectedSuccess) {
            TenantDetail? detail = DeserializePayload<TenantDetail>(result);
            _ = detail.ShouldNotBeNull();
            detail.TenantId.ShouldBe("tenant-1");
        }
        else {
            result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
            result.PayloadBytes.ShouldBeNull();
        }
    }

    [Fact]
    public async Task GetTenant_filters_unknown_role_rows_from_detail_membersAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantReadModel model = CreateTenantReadModel(members: new() {
            ["user-1"] = TenantRole.TenantOwner,
            ["hidden-user"] = TenantRole.Unknown,
        });
        SetupTenantState(daprClient, "tenant-1", model);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant", userId: "user-1"));

        result.Success.ShouldBeTrue();
        TenantDetail? detail = DeserializePayload<TenantDetail>(result);
        _ = detail.ShouldNotBeNull();
        detail.Members.Select(m => m.UserId).ShouldBe(["user-1"]);
        string body = result.GetPayload().GetRawText();
        body.ShouldNotContain("hidden-user");
        body.ShouldNotContain("Unknown");
    }

    // --- Q18: GetTenantAudit returns audit entries for GlobalAdmin ---
    [Fact]
    public async Task GetTenantAudit_global_admin_returns_audit_entriesAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));
        SetupAuditState(daprClient, "tenant-1", CreateAuditModel(
            CreateAuditEntry("evt-1", "TenantCreated", AuditEventCategory.Administrative)));

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant-audit", userId: "admin-1"));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantAuditEntry>? page = DeserializePayload<PaginatedResult<TenantAuditEntry>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(1);
        page.Items[0].EventId.ShouldBe("evt-1");
    }

    // --- Q27: Non-admin hitting audit endpoint gets 403 not 501 ---
    [Fact]
    public async Task GetTenantAudit_non_admin_returns_forbidden_not_501Async() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant-audit", userId: "user-1"));

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Forbidden");
        result.ErrorMessage!.ShouldNotContain("not yet implemented");
        _ = await daprClient.DidNotReceive().GetStateAsync<TenantAuditReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantAuditProjectionKeyPrefix + "tenant-1");
    }

    [Fact]
    public async Task GetTenantAudit_missing_state_returns_empty_pageAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));
        SetupAuditState(daprClient, "tenant-1", null);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant-audit", userId: "admin-1"));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantAuditEntry>? page = DeserializePayload<PaginatedResult<TenantAuditEntry>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.ShouldBeEmpty();
        page.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task GetTenantAudit_filters_by_date_range_and_categoryAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));
        DateTimeOffset start = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        SetupAuditState(daprClient, "tenant-1", CreateAuditModel(
            CreateAuditEntry("evt-1", "TenantCreated", AuditEventCategory.Administrative, start.AddMinutes(-1)),
            CreateAuditEntry("evt-2", "UserAddedToTenant", AuditEventCategory.Access, start.AddMinutes(1)),
            CreateAuditEntry("evt-3", "TenantUpdated", AuditEventCategory.Administrative, start.AddMinutes(2))));

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreateAuditPayload(from: start, to: start.AddMinutes(3), category: "administrative");
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant-audit", userId: "admin-1", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantAuditEntry>? page = DeserializePayload<PaginatedResult<TenantAuditEntry>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Select(e => e.EventId).ShouldBe(["evt-3"]);
    }

    [Fact]
    public async Task GetTenantAudit_paginates_after_filtering_with_stable_cursorAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        SetupAuditState(daprClient, "tenant-1", CreateAuditModel(
            CreateAuditEntry("evt-b", "TenantUpdated", AuditEventCategory.Administrative, timestamp),
            CreateAuditEntry("evt-a", "TenantCreated", AuditEventCategory.Administrative, timestamp),
            CreateAuditEntry("evt-c", "UserAddedToTenant", AuditEventCategory.Access, timestamp.AddMinutes(1))));

        ITenantQueryCursorCodec cursorCodec = CreateCursorCodec();
        TenantsProjectionActor actor = CreateActor(daprClient, cursorCodec);
        QueryResult firstResult = await actor.QueryAsync(CreateEnvelope(
            "get-tenant-audit",
            userId: "admin-1",
            payload: CreateAuditPayload(category: "administrative", pageSize: 1)));

        PaginatedResult<TenantAuditEntry>? firstPage = DeserializePayload<PaginatedResult<TenantAuditEntry>>(firstResult);
        _ = firstPage.ShouldNotBeNull();
        firstPage.Items.Select(e => e.EventId).ShouldBe(["evt-a"]);
        firstPage.HasMore.ShouldBeTrue();
        _ = firstPage.Cursor.ShouldNotBeNull();
        firstPage.Cursor.ShouldNotContain("evt-a");
        firstPage.Cursor.ShouldNotContain("000000");

        // Round-trip: the protected cursor must decode through the codec back to the audit-cursor
        // inner format (Ticks:D20:EventId), proving opacity is cryptographic and not just substring-coincidence.
        string expectedScope = TenantQueryCursorScopes.GetTenantAudit("tenant-1", null, null, AuditEventCategory.Administrative);
        cursorCodec.TryDecode(firstPage.Cursor, GetTenantAuditQuery.QueryType, expectedScope, out string? decodedAuditPosition, out _).ShouldBeTrue();
        _ = decodedAuditPosition.ShouldNotBeNull();
        decodedAuditPosition.ShouldEndWith(":evt-a");

        QueryResult secondResult = await actor.QueryAsync(CreateEnvelope(
            "get-tenant-audit",
            userId: "admin-1",
            payload: CreateAuditPayload(category: "administrative", cursor: firstPage.Cursor, pageSize: 1)));

        PaginatedResult<TenantAuditEntry>? secondPage = DeserializePayload<PaginatedResult<TenantAuditEntry>>(secondResult);
        _ = secondPage.ShouldNotBeNull();
        secondPage.Items.Select(e => e.EventId).ShouldBe(["evt-b"]);
        secondPage.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task GetTenantAudit_conflict_recovered_entries_remain_date_range_and_cursor_queryableAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        SetupAuditState(daprClient, "tenant-1", CreateAuditModel(
            CreateAuditEntry("evt-existing", "UserAddedToTenant", AuditEventCategory.Access, timestamp),
            CreateAuditEntry("evt-concurrent", "UserRemovedFromTenant", AuditEventCategory.Access, timestamp.AddMinutes(1)),
            CreateAuditEntry("evt-added", "UserAddedToTenant", AuditEventCategory.Access, timestamp.AddMinutes(2))));

        TenantsProjectionActor actor = CreateActor(daprClient, CreateCursorCodec());
        QueryResult firstResult = await actor.QueryAsync(CreateEnvelope(
            "get-tenant-audit",
            userId: "admin-1",
            payload: CreateAuditPayload(
                from: timestamp,
                to: timestamp.AddMinutes(3),
                category: "access",
                pageSize: 2)));

        PaginatedResult<TenantAuditEntry>? firstPage = DeserializePayload<PaginatedResult<TenantAuditEntry>>(firstResult);
        _ = firstPage.ShouldNotBeNull();
        firstPage.Items.Select(e => e.EventId).ShouldBe(["evt-existing", "evt-concurrent"]);
        firstPage.HasMore.ShouldBeTrue();
        _ = firstPage.Cursor.ShouldNotBeNull();

        QueryResult secondResult = await actor.QueryAsync(CreateEnvelope(
            "get-tenant-audit",
            userId: "admin-1",
            payload: CreateAuditPayload(
                from: timestamp,
                to: timestamp.AddMinutes(3),
                category: "access",
                cursor: firstPage.Cursor,
                pageSize: 2)));

        PaginatedResult<TenantAuditEntry>? secondPage = DeserializePayload<PaginatedResult<TenantAuditEntry>>(secondResult);
        _ = secondPage.ShouldNotBeNull();
        secondPage.Items.Select(e => e.EventId).ShouldBe(["evt-added"]);
        secondPage.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task GetTenantAudit_rejects_from_greater_than_toAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));
        DateTimeOffset start = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreateAuditPayload(from: start.AddDays(1), to: start);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant-audit", userId: "admin-1", payload: payload));

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("'from' must not be after 'to'");
    }

    [Fact]
    public async Task GetTenantAudit_rejects_malformed_cursorAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreateAuditPayload(cursor: "not-a-valid-cursor");
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant-audit", userId: "admin-1", payload: payload));

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Invalid cursor");
    }

    [Fact]
    public async Task GetTenantAudit_malformed_payload_returns_invalid_payload_before_audit_state_readAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope(
            "get-tenant-audit",
            userId: "admin-1",
            payload: "{ not json"u8.ToArray()));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Invalid audit query payload.");
        _ = await daprClient.DidNotReceive().GetStateAsync<TenantAuditReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantAuditProjectionKeyPrefix + "tenant-1");
        await AssertNoStateWriteAsync(daprClient);
    }

    [Fact]
    public async Task GetTenantAudit_drops_entries_with_mismatched_tenantIdAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));
        // NFR5 defense-in-depth: an entry persisted under audit:tenant-1 with a different
        // payload.TenantId must not leak to the caller. Simulates a hypothetical projection bug.
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        TenantAuditEntry foreign = new(
            "evt-foreign",
            "TenantUpdated",
            AuditEventCategory.Administrative,
            "actor-1",
            timestamp,
            "other-tenant",
            new Dictionary<string, string> { ["key"] = "value" });
        SetupAuditState(daprClient, "tenant-1", new TenantAuditReadModel {
            Entries = [CreateAuditEntry("evt-own", "TenantCreated", AuditEventCategory.Administrative, timestamp.AddMinutes(-1)), foreign],
        });

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant-audit", userId: "admin-1"));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantAuditEntry>? page = DeserializePayload<PaginatedResult<TenantAuditEntry>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Select(e => e.EventId).ShouldBe(["evt-own"]);
    }

    [Fact]
    public async Task GetTenantAudit_invalid_category_returns_errorAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope(
            "get-tenant-audit",
            userId: "admin-1",
            payload: CreateAuditPayload(category: "invalid")));

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Invalid audit category");
    }

    [Fact]
    public async Task GetTenantAudit_clamps_page_size_to_one_thousandAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        TenantAuditEntry[] entries = [.. Enumerable.Range(1, 1001)
            .Select(i => CreateAuditEntry($"evt-{i:D4}", "TenantUpdated", AuditEventCategory.Administrative, timestamp.AddSeconds(i)))];
        SetupAuditState(daprClient, "tenant-1", CreateAuditModel(entries));

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope(
            "get-tenant-audit",
            userId: "admin-1",
            payload: CreateAuditPayload(pageSize: 2000)));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantAuditEntry>? page = DeserializePayload<PaginatedResult<TenantAuditEntry>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(1000);
        page.HasMore.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetTenantAudit_clamps_non_positive_page_size_to_audit_defaultAsync(int requestedPageSize) {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));
        DateTimeOffset timestamp = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        TenantAuditEntry[] entries = [.. Enumerable.Range(1, 101)
            .Select(i => CreateAuditEntry($"evt-{i:D4}", "TenantUpdated", AuditEventCategory.Administrative, timestamp.AddSeconds(i)))];
        SetupAuditState(daprClient, "tenant-1", CreateAuditModel(entries));

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope(
            "get-tenant-audit",
            userId: "admin-1",
            payload: CreateAuditPayload(pageSize: requestedPageSize)));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantAuditEntry>? page = DeserializePayload<PaginatedResult<TenantAuditEntry>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(100);
        page.HasMore.ShouldBeTrue();
    }

    // --- Q14: GetTenantUsers returns paginated member list ---
    [Fact]
    public async Task GetTenantUsers_returns_paginated_membersAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        Dictionary<string, TenantRole> members = new() {
            ["user-1"] = TenantRole.TenantOwner,
            ["user-2"] = TenantRole.TenantContributor,
            ["user-3"] = TenantRole.TenantReader,
            ["user-4"] = TenantRole.TenantReader,
            ["user-5"] = TenantRole.TenantReader,
        };
        TenantReadModel model = CreateTenantReadModel(members: members);
        SetupTenantState(daprClient, "tenant-1", model);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(pageSize: 20);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant-users", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantMember>? page = DeserializePayload<PaginatedResult<TenantMember>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(5);
    }

    [Fact]
    public async Task GetTenantUsers_global_admin_bypasses_membershipAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        Dictionary<string, TenantRole> members = new() {
            ["user-1"] = TenantRole.TenantReader,
            ["user-2"] = TenantRole.TenantContributor,
        };
        TenantReadModel model = CreateTenantReadModel(members: members);
        SetupTenantState(daprClient, "tenant-1", model);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant-users", userId: "admin-1"));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantMember>? page = DeserializePayload<PaginatedResult<TenantMember>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Select(i => i.UserId).ShouldBe(["user-1", "user-2"]);
    }

    [Theory]
    [InlineData(TenantRole.TenantReader, true)]
    [InlineData(TenantRole.TenantContributor, true)]
    [InlineData(TenantRole.TenantOwner, true)]
    [InlineData(TenantRole.Unknown, false)]
    public async Task GetTenantUsers_member_authorization_treats_only_concrete_roles_as_membersAsync(
        TenantRole role,
        bool expectedSuccess) {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantReadModel model = CreateTenantReadModel(members: new() { ["user-1"] = role, ["user-2"] = TenantRole.TenantReader });
        SetupTenantState(daprClient, "tenant-1", model);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant-users", userId: "user-1"));

        result.Success.ShouldBe(expectedSuccess);
        if (expectedSuccess) {
            PaginatedResult<TenantMember>? page = DeserializePayload<PaginatedResult<TenantMember>>(result);
            _ = page.ShouldNotBeNull();
            page.Items.Select(i => i.UserId).ShouldContain("user-2");
        }
        else {
            result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
            result.PayloadBytes.ShouldBeNull();
        }
    }

    [Fact]
    public async Task GetTenantUsers_non_admin_for_missing_tenant_returns_forbidden_without_page_metadataAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        _ = daprClient.GetStateAsync<TenantReadModel>(
            TenantsProjectionActor.StateStoreName,
            Arg.Any<string>())
            .Returns(Task.FromResult<TenantReadModel>(null!)!);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant-users", aggregateId: "hidden-tenant"));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
    }

    [Fact]
    public async Task GetTenantUsers_filters_unknown_role_rows_before_paginationAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantReadModel model = CreateTenantReadModel(members: new() {
            ["user-1"] = TenantRole.TenantOwner,
            ["user-2"] = TenantRole.Unknown,
            ["user-3"] = TenantRole.TenantReader,
        });
        SetupTenantState(daprClient, "tenant-1", model);
        SetupNoGlobalAdmin(daprClient);

        ITenantQueryCursorCodec cursorCodec = CreateCursorCodec();
        TenantsProjectionActor actor = CreateActor(daprClient, cursorCodec);
        QueryResult firstResult = await actor.QueryAsync(CreateEnvelope(
            "get-tenant-users",
            payload: CreatePaginationPayload(pageSize: 1)));

        PaginatedResult<TenantMember>? firstPage = DeserializePayload<PaginatedResult<TenantMember>>(firstResult);
        _ = firstPage.ShouldNotBeNull();
        firstPage.Items.Select(i => i.UserId).ShouldBe(["user-1"]);
        firstPage.HasMore.ShouldBeTrue();
        _ = firstPage.Cursor.ShouldNotBeNull();

        QueryResult secondResult = await actor.QueryAsync(CreateEnvelope(
            "get-tenant-users",
            payload: CreatePaginationPayload(cursor: firstPage.Cursor, pageSize: 10)));

        PaginatedResult<TenantMember>? secondPage = DeserializePayload<PaginatedResult<TenantMember>>(secondResult);
        _ = secondPage.ShouldNotBeNull();
        secondPage.Items.Select(i => i.UserId).ShouldBe(["user-3"]);
        secondPage.Items.Select(i => i.UserId).ShouldNotContain("user-2");
        secondPage.HasMore.ShouldBeFalse();
        secondPage.Cursor.ShouldBeNull();
    }

    [Fact]
    public async Task GetTenantUsers_signed_cursor_resumes_from_same_logical_positionAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        Dictionary<string, TenantRole> members = new() {
            ["user-1"] = TenantRole.TenantOwner,
            ["user-2"] = TenantRole.TenantContributor,
            ["user-3"] = TenantRole.TenantReader,
        };
        TenantReadModel model = CreateTenantReadModel(members: members);
        SetupTenantState(daprClient, "tenant-1", model);
        SetupNoGlobalAdmin(daprClient);

        ITenantQueryCursorCodec cursorCodec = CreateCursorCodec();
        TenantsProjectionActor actor = CreateActor(daprClient, cursorCodec);
        QueryResult firstResult = await actor.QueryAsync(CreateEnvelope(
            "get-tenant-users",
            payload: CreatePaginationPayload(pageSize: 1)));

        PaginatedResult<TenantMember>? firstPage = DeserializePayload<PaginatedResult<TenantMember>>(firstResult);
        _ = firstPage.ShouldNotBeNull();
        firstPage.Items.Select(i => i.UserId).ShouldBe(["user-1"]);
        _ = firstPage.Cursor.ShouldNotBeNull();
        firstPage.Cursor.ShouldNotContain("user-1");

        // Round-trip: protected cursor must decode through the codec back to the exact inner key
        // ("user-1"), proving the wire cursor is cryptographically opaque rather than substring-obfuscated.
        string expectedScope = TenantQueryCursorScopes.GetTenantUsers("tenant-1");
        cursorCodec.TryDecode(firstPage.Cursor, GetTenantUsersQuery.QueryType, expectedScope, out string? decodedUserKey, out _).ShouldBeTrue();
        decodedUserKey.ShouldBe("user-1");

        QueryResult secondResult = await actor.QueryAsync(CreateEnvelope(
            "get-tenant-users",
            payload: CreatePaginationPayload(cursor: firstPage.Cursor, pageSize: 1)));

        PaginatedResult<TenantMember>? secondPage = DeserializePayload<PaginatedResult<TenantMember>>(secondResult);
        _ = secondPage.ShouldNotBeNull();
        secondPage.Items.Select(i => i.UserId).ShouldBe(["user-2"]);
    }

    [Theory]
    [InlineData("list-tenants")]
    [InlineData("get-tenant-users")]
    [InlineData("get-user-tenants")]
    public async Task Standard_paginated_queries_clamp_page_size_to_standard_maximumAsync(string queryType) {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantsProjectionActor actor = CreateActor(daprClient);

        QueryResult result = queryType switch {
            "list-tenants" => await QueryListTenantsWithOversizedPageAsync(daprClient, actor),
            "get-tenant-users" => await QueryTenantUsersWithOversizedPageAsync(daprClient, actor),
            "get-user-tenants" => await QueryUserTenantsWithOversizedPageAsync(daprClient, actor),
            _ => throw new ArgumentOutOfRangeException(nameof(queryType), queryType, null),
        };

        result.Success.ShouldBeTrue();
        CountPayloadItems(result).ShouldBe(TenantQueryPaginationPolicy.StandardMaximumPageSize);
        GetPayloadHasMore(result).ShouldBeTrue();
    }

    // --- Q17: GlobalAdmin can query any user's tenants ---
    [Fact]
    public async Task GetUserTenants_global_admin_can_query_any_userAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3, new() {
            ["user-2"] = new() { ["tenant-001"] = TenantRole.TenantReader, ["tenant-002"] = TenantRole.TenantContributor },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(pageSize: 20);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "admin-1", aggregateId: "index", entityId: "user-2", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetUserTenants_self_lookup_includes_disabled_tenant_statusAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(1, new() {
            ["user-1"] = new() { ["tenant-001"] = TenantRole.TenantReader },
        });
        indexModel.Apply(new Contracts.Events.TenantDisabled("tenant-001", DateTimeOffset.UtcNow));
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "user-1", aggregateId: "index", entityId: "user-1"));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(1);
        page.Items[0].TenantId.ShouldBe("tenant-001");
        page.Items[0].Status.ShouldBe(TenantStatus.Disabled);
    }

    [Fact]
    public async Task GetUserTenants_tenant_owner_lookup_includes_disabled_tenant_statusAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(1, new() {
            ["owner-1"] = new() { ["tenant-001"] = TenantRole.TenantOwner },
            ["user-2"] = new() { ["tenant-001"] = TenantRole.TenantReader },
        });
        indexModel.Apply(new Contracts.Events.TenantDisabled("tenant-001", DateTimeOffset.UtcNow));
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "owner-1", aggregateId: "index", entityId: "user-2"));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(1);
        page.Items[0].TenantId.ShouldBe("tenant-001");
        page.Items[0].Status.ShouldBe(TenantStatus.Disabled);
    }

    [Fact]
    public async Task GetUserTenants_global_admin_lookup_includes_disabled_tenant_statusAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(1, new() {
            ["user-2"] = new() { ["tenant-001"] = TenantRole.TenantReader },
        });
        indexModel.Apply(new Contracts.Events.TenantDisabled("tenant-001", DateTimeOffset.UtcNow));
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "admin-1", aggregateId: "index", entityId: "user-2"));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(1);
        page.Items[0].TenantId.ShouldBe("tenant-001");
        page.Items[0].Status.ShouldBe(TenantStatus.Disabled);
    }

    [Fact]
    public async Task GetUserTenants_filters_orphan_memberships_before_pagination_and_logs_warningAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3);
        indexModel.UserTenants["user-1"] = new(StringComparer.Ordinal) {
            ["tenant-001"] = TenantRole.TenantReader,
            ["tenant-002-orphan"] = TenantRole.TenantContributor,
            ["tenant-003"] = TenantRole.TenantOwner,
        };
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);
        var logger = new ListLogger<TenantsProjectionActor>();

        const string correlationId = "correlation-orphan-filter";
        TenantsProjectionActor actor = CreateActor(daprClient, CreateCursorCodec(), logger);
        QueryResult result = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "user-1",
            aggregateId: "index",
            entityId: "user-1",
            payload: CreatePaginationPayload(pageSize: 2),
            correlationId: correlationId));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Select(i => i.TenantId).ShouldBe(["tenant-001", "tenant-003"]);
        page.Items.ShouldAllBe(i => !string.IsNullOrWhiteSpace(i.Name));
        page.Items.ShouldAllBe(i => i.TenantId != "tenant-002-orphan");
        page.HasMore.ShouldBeFalse();
        page.Cursor.ShouldBeNull();

        LogEntry warning = logger.Entries.Single(e => e.Level == LogLevel.Warning);
        warning.EventId.Id.ShouldBe(1903);
        warning.State["CorrelationId"].ShouldBe(correlationId);
        warning.State["QueryType"].ShouldBe("get-user-tenants");
        warning.State["RequesterUserId"].ShouldBe("user-1");
        warning.State["TargetUserId"].ShouldBe("user-1");
        warning.State["OrphanTenantId"].ShouldBe("tenant-002-orphan");
    }

    [Fact]
    public async Task GetUserTenants_global_admin_filters_orphan_without_public_diagnosticsAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(1);
        indexModel.UserTenants["user-2"] = new(StringComparer.Ordinal) {
            ["tenant-001"] = TenantRole.TenantReader,
            ["tenant-002-orphan"] = TenantRole.TenantContributor,
        };
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));
        var logger = new ListLogger<TenantsProjectionActor>();

        TenantsProjectionActor actor = CreateActor(daprClient, CreateCursorCodec(), logger);
        QueryResult result = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "admin-1",
            aggregateId: "index",
            entityId: "user-2",
            payload: CreatePaginationPayload(pageSize: 20),
            correlationId: "correlation-admin-orphan"));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Select(i => i.TenantId).ShouldBe(["tenant-001"]);
        page.Items.ShouldAllBe(i => i.TenantId != "tenant-002-orphan");
        page.GetType().GetProperty("Orphans").ShouldBeNull();
        page.GetType().GetProperty("Diagnostics").ShouldBeNull();
        LogEntry adminWarning = logger.Entries.Single(e => e.Level == LogLevel.Warning && e.EventId.Id == 1903);
        adminWarning.State["RequesterUserId"].ShouldBe("admin-1");
        adminWarning.State["TargetUserId"].ShouldBe("user-2");
        adminWarning.State["OrphanTenantId"].ShouldBe("tenant-002-orphan");
    }

    [Fact]
    public async Task GetUserTenants_all_orphan_page_returns_empty_without_cursorAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(0);
        indexModel.UserTenants["user-1"] = new(StringComparer.Ordinal) {
            ["tenant-001-orphan"] = TenantRole.TenantReader,
        };
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "user-1",
            aggregateId: "index",
            entityId: "user-1",
            payload: CreatePaginationPayload(pageSize: 1)));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.ShouldBeEmpty();
        page.HasMore.ShouldBeFalse();
        page.Cursor.ShouldBeNull();
    }

    [Fact]
    public async Task GetUserTenants_stale_self_lookup_returns_current_projection_onlyAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(1);
        indexModel.UserTenants["user-1"] = new(StringComparer.Ordinal) {
            ["tenant-001"] = TenantRole.TenantReader,
        };
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "user-1", aggregateId: "index", entityId: "user-1"));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Select(i => i.TenantId).ShouldBe(["tenant-001"]);
        page.Items[0].Role.ShouldBe(TenantRole.TenantReader);
    }

    [Fact]
    public async Task GetUserTenants_cursor_anchor_now_orphan_advances_without_materializing_itAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3);
        indexModel.UserTenants["user-1"] = new(StringComparer.Ordinal) {
            ["tenant-001"] = TenantRole.TenantReader,
            ["tenant-002"] = TenantRole.TenantContributor,
            ["tenant-003"] = TenantRole.TenantOwner,
        };
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);
        var logger = new ListLogger<TenantsProjectionActor>();

        TenantsProjectionActor actor = CreateActor(daprClient, CreateCursorCodec(), logger);
        QueryResult firstResult = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "user-1",
            aggregateId: "index",
            entityId: "user-1",
            payload: CreatePaginationPayload(pageSize: 1)));

        firstResult.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? firstPage = DeserializePayload<PaginatedResult<UserTenantMembership>>(firstResult);
        _ = firstPage.ShouldNotBeNull();
        firstPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-001"]);
        firstPage.Cursor.ShouldNotBeNullOrWhiteSpace();
        logger.Entries.ShouldNotContain(e => e.EventId.Id == 1903);

        // The cursor anchor (tenant-001) disappears from the tenant index between pages.
        _ = indexModel.Tenants.Remove("tenant-001");

        QueryResult secondResult = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "user-1",
            aggregateId: "index",
            entityId: "user-1",
            payload: CreatePaginationPayload(cursor: firstPage.Cursor, pageSize: 1),
            correlationId: "correlation-anchor-orphan"));

        secondResult.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? secondPage = DeserializePayload<PaginatedResult<UserTenantMembership>>(secondResult);
        _ = secondPage.ShouldNotBeNull();
        secondPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-002"]);
        secondPage.Items.ShouldAllBe(i => i.TenantId != "tenant-001");
        secondPage.Items.ShouldAllBe(i => !string.IsNullOrWhiteSpace(i.Name));
        secondPage.HasMore.ShouldBeTrue();

        LogEntry anchorWarning = logger.Entries.Single(e => e.Level == LogLevel.Warning && e.EventId.Id == 1903);
        anchorWarning.State["OrphanTenantId"].ShouldBe("tenant-001");
        anchorWarning.State["TargetUserId"].ShouldBe("user-1");
        anchorWarning.State["CorrelationId"].ShouldBe("correlation-anchor-orphan");
    }

    [Fact]
    public async Task GetUserTenants_repeated_orphan_query_logs_warning_onceAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(1);
        indexModel.UserTenants["user-1"] = new(StringComparer.Ordinal) {
            ["tenant-001"] = TenantRole.TenantReader,
            ["tenant-002-orphan"] = TenantRole.TenantContributor,
        };
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);
        var logger = new ListLogger<TenantsProjectionActor>();

        TenantsProjectionActor actor = CreateActor(daprClient, CreateCursorCodec(), logger);
        QueryEnvelope envelope = CreateEnvelope(
            "get-user-tenants",
            userId: "user-1",
            aggregateId: "index",
            entityId: "user-1",
            payload: CreatePaginationPayload(pageSize: 10));

        _ = await actor.QueryAsync(envelope);
        _ = await actor.QueryAsync(envelope);
        _ = await actor.QueryAsync(envelope);

        logger.Entries.Count(e => e.Level == LogLevel.Warning && e.EventId.Id == 1903).ShouldBe(1);
    }

    // --- Q16: Non-owner cross-user lookup returns empty page ---
    [Fact]
    public async Task GetUserTenants_non_owner_querying_other_user_returns_empty_page() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(2, new() {
            ["user-1"] = new() { ["tenant-001"] = TenantRole.TenantReader },
            ["user-2"] = new() { ["tenant-001"] = TenantRole.TenantReader, ["tenant-002"] = TenantRole.TenantContributor },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "user-1", aggregateId: "index", entityId: "user-2"));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(0);
        page.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task GetUserTenants_excludes_unknown_roles_and_does_not_use_them_as_owner_authorityAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3, new() {
            ["owner-1"] = new() {
                ["tenant-001"] = TenantRole.TenantOwner,
                ["tenant-002"] = TenantRole.Unknown,
            },
            ["user-2"] = new() {
                ["tenant-001"] = TenantRole.TenantReader,
                ["tenant-002"] = TenantRole.TenantReader,
                ["tenant-003"] = TenantRole.Unknown,
            },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "owner-1", aggregateId: "index", entityId: "user-2"));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Select(i => i.TenantId).ShouldBe(["tenant-001"]);
        page.HasMore.ShouldBeFalse();
        page.Cursor.ShouldBeNull();
    }

    [Fact]
    public async Task GetUserTenants_rejects_cursor_issued_for_different_requesterAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3, new() {
            ["owner-1"] = new() { ["tenant-001"] = TenantRole.TenantOwner, ["tenant-002"] = TenantRole.TenantOwner },
            ["owner-2"] = new() { ["tenant-001"] = TenantRole.TenantOwner, ["tenant-002"] = TenantRole.TenantOwner },
            ["user-2"] = new() { ["tenant-001"] = TenantRole.TenantReader, ["tenant-002"] = TenantRole.TenantReader },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        ITenantQueryCursorCodec cursorCodec = CreateCursorCodec();
        string foreignRequesterCursor = cursorCodec.Encode(
            GetUserTenantsQuery.QueryType,
            TenantQueryCursorScopes.GetUserTenants("owner-2", "user-2"),
            "tenant-001");

        TenantsProjectionActor actor = CreateActor(daprClient, cursorCodec);
        QueryResult result = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "owner-1",
            aggregateId: "index",
            entityId: "user-2",
            payload: CreatePaginationPayload(cursor: foreignRequesterCursor)));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Invalid cursor.");
        result.PayloadBytes.ShouldBeNull();
    }

    [Fact]
    public async Task GetUserTenants_rejects_invalid_cursor_before_empty_missing_target_responseAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(1, new() {
            ["owner-1"] = new() { ["tenant-001"] = TenantRole.TenantOwner },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        ITenantQueryCursorCodec cursorCodec = CreateCursorCodec();
        string wrongTargetCursor = cursorCodec.Encode(
            GetUserTenantsQuery.QueryType,
            TenantQueryCursorScopes.GetUserTenants("owner-1", "other-target"),
            "tenant-001");

        TenantsProjectionActor actor = CreateActor(daprClient, cursorCodec);
        QueryResult result = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "owner-1",
            aggregateId: "index",
            entityId: "missing-target",
            payload: CreatePaginationPayload(cursor: wrongTargetCursor)));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Invalid cursor.");
        result.PayloadBytes.ShouldBeNull();
    }

    // --- Q15: GetUserTenants for own user works ---
    [Fact]
    public async Task GetUserTenants_own_user_returns_memberships() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(5, new() {
            ["user-1"] = new() {
                ["tenant-001"] = TenantRole.TenantOwner,
                ["tenant-002"] = TenantRole.TenantReader,
                ["tenant-004"] = TenantRole.TenantContributor,
            },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(pageSize: 20);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "user-1", aggregateId: "index", entityId: "user-1", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetUserTenants_missing_target_user_returns_empty_page() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(2, new() {
            ["user-1"] = new() { ["tenant-001"] = TenantRole.TenantOwner },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "user-1", aggregateId: "index", entityId: "user-2"));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(0);
        page.HasMore.ShouldBeFalse();

        // Timing-uniformity guarantee: cross-user lookups must perform the admin check
        // even when the target user is missing, so that the empty-page response from this
        // branch is timing-indistinguishable from the filtered-no-overlap branch.
        _ = await daprClient.Received(1).GetStateAsync<GlobalAdministratorReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.GlobalAdminProjectionKey);
    }

    [Fact]
    public async Task GetUserTenants_tenant_owner_querying_user_with_overlap_returns_owned_tenants_only() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3, new() {
            ["user-1"] = new() { ["tenant-001"] = TenantRole.TenantOwner, ["tenant-003"] = TenantRole.TenantReader },
            ["user-2"] = new() { ["tenant-001"] = TenantRole.TenantReader, ["tenant-002"] = TenantRole.TenantContributor },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "user-1", aggregateId: "index", entityId: "user-2"));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(1);
        page.Items[0].TenantId.ShouldBe("tenant-001");
        page.Items[0].Role.ShouldBe(TenantRole.TenantReader);
    }

    [Fact]
    public async Task GetUserTenants_tenant_owner_querying_user_without_overlap_returns_empty_page() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3, new() {
            ["user-1"] = new() { ["tenant-001"] = TenantRole.TenantOwner },
            ["user-2"] = new() { ["tenant-002"] = TenantRole.TenantReader },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "user-1", aggregateId: "index", entityId: "user-2"));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(0);
        page.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task GetUserTenants_tenant_owner_paginates_after_filtering() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(5, new() {
            ["user-1"] = new() {
                ["tenant-001"] = TenantRole.TenantOwner,
                ["tenant-002"] = TenantRole.TenantReader,
                ["tenant-003"] = TenantRole.TenantOwner,
                ["tenant-005"] = TenantRole.TenantOwner,
            },
            ["user-2"] = new() {
                ["tenant-001"] = TenantRole.TenantReader,
                ["tenant-002"] = TenantRole.TenantReader,
                ["tenant-003"] = TenantRole.TenantContributor,
                ["tenant-004"] = TenantRole.TenantReader,
                ["tenant-005"] = TenantRole.TenantReader,
            },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] firstPagePayload = CreatePaginationPayload(pageSize: 2);
        QueryResult firstResult = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "user-1", aggregateId: "index", entityId: "user-2", payload: firstPagePayload));

        firstResult.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? firstPage = DeserializePayload<PaginatedResult<UserTenantMembership>>(firstResult);
        _ = firstPage.ShouldNotBeNull();
        firstPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-001", "tenant-003"]);
        firstPage.HasMore.ShouldBeTrue();
        _ = firstPage.Cursor.ShouldNotBeNull();
        firstPage.Cursor.ShouldNotBe("tenant-003");
        firstPage.Cursor.ShouldNotContain("tenant-003");

        byte[] secondPagePayload = CreatePaginationPayload(cursor: firstPage.Cursor, pageSize: 2);
        QueryResult secondResult = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "user-1", aggregateId: "index", entityId: "user-2", payload: secondPagePayload));

        secondResult.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? secondPage = DeserializePayload<PaginatedResult<UserTenantMembership>>(secondResult);
        _ = secondPage.ShouldNotBeNull();
        secondPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-005"]);
        secondPage.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task GetUserTenants_self_lookup_removed_next_page_tenant_is_not_returnedAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3, new() {
            ["user-1"] = new() {
                ["tenant-001"] = TenantRole.TenantReader,
                ["tenant-002"] = TenantRole.TenantContributor,
                ["tenant-003"] = TenantRole.TenantOwner,
            },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult firstResult = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "user-1",
            aggregateId: "index",
            entityId: "user-1",
            payload: CreatePaginationPayload(pageSize: 1)));

        PaginatedResult<UserTenantMembership>? firstPage = DeserializePayload<PaginatedResult<UserTenantMembership>>(firstResult);
        _ = firstPage.ShouldNotBeNull();
        firstPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-001"]);
        _ = firstPage.Cursor.ShouldNotBeNull();
        firstPage.HasMore.ShouldBeTrue();

        indexModel.Apply(new Contracts.Events.UserRemovedFromTenant("tenant-002", "user-1"));

        QueryResult secondResult = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "user-1",
            aggregateId: "index",
            entityId: "user-1",
            payload: CreatePaginationPayload(cursor: firstPage.Cursor, pageSize: 10)));

        secondResult.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? secondPage = DeserializePayload<PaginatedResult<UserTenantMembership>>(secondResult);
        _ = secondPage.ShouldNotBeNull();
        secondPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-003"]);
        secondPage.Items.ShouldAllBe(i => i.TenantId != "tenant-002");
        secondPage.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task GetUserTenants_self_lookup_newly_visible_tenant_before_cursor_is_not_backfilledAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3, new() {
            ["user-1"] = new() {
                ["tenant-002"] = TenantRole.TenantReader,
                ["tenant-003"] = TenantRole.TenantContributor,
            },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult firstResult = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "user-1",
            aggregateId: "index",
            entityId: "user-1",
            payload: CreatePaginationPayload(pageSize: 1)));

        PaginatedResult<UserTenantMembership>? firstPage = DeserializePayload<PaginatedResult<UserTenantMembership>>(firstResult);
        _ = firstPage.ShouldNotBeNull();
        firstPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-002"]);
        _ = firstPage.Cursor.ShouldNotBeNull();
        firstPage.HasMore.ShouldBeTrue();

        indexModel.Apply(new Contracts.Events.UserAddedToTenant("tenant-001", "user-1", TenantRole.TenantReader));

        QueryResult secondResult = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "user-1",
            aggregateId: "index",
            entityId: "user-1",
            payload: CreatePaginationPayload(cursor: firstPage.Cursor, pageSize: 10)));

        secondResult.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? secondPage = DeserializePayload<PaginatedResult<UserTenantMembership>>(secondResult);
        _ = secondPage.ShouldNotBeNull();
        secondPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-003"]);
        secondPage.Items.ShouldAllBe(i => i.TenantId != "tenant-001");
    }

    [Fact]
    public async Task GetUserTenants_self_lookup_newly_visible_tenant_after_cursor_may_appearAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3, new() {
            ["user-1"] = new() {
                ["tenant-001"] = TenantRole.TenantReader,
                ["tenant-003"] = TenantRole.TenantContributor,
            },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult firstResult = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "user-1",
            aggregateId: "index",
            entityId: "user-1",
            payload: CreatePaginationPayload(pageSize: 1)));

        PaginatedResult<UserTenantMembership>? firstPage = DeserializePayload<PaginatedResult<UserTenantMembership>>(firstResult);
        _ = firstPage.ShouldNotBeNull();
        firstPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-001"]);
        _ = firstPage.Cursor.ShouldNotBeNull();
        firstPage.HasMore.ShouldBeTrue();

        indexModel.Apply(new Contracts.Events.UserAddedToTenant("tenant-002", "user-1", TenantRole.TenantReader));

        QueryResult secondResult = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "user-1",
            aggregateId: "index",
            entityId: "user-1",
            payload: CreatePaginationPayload(cursor: firstPage.Cursor, pageSize: 10)));

        secondResult.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? secondPage = DeserializePayload<PaginatedResult<UserTenantMembership>>(secondResult);
        _ = secondPage.ShouldNotBeNull();
        secondPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-002", "tenant-003"]);
    }

    [Fact]
    public async Task GetUserTenants_tenant_owner_target_membership_removed_between_pages_is_filteredAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3, new() {
            ["owner-1"] = new() {
                ["tenant-001"] = TenantRole.TenantOwner,
                ["tenant-002"] = TenantRole.TenantOwner,
                ["tenant-003"] = TenantRole.TenantOwner,
            },
            ["user-2"] = new() {
                ["tenant-001"] = TenantRole.TenantReader,
                ["tenant-002"] = TenantRole.TenantReader,
                ["tenant-003"] = TenantRole.TenantReader,
            },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult firstResult = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "owner-1",
            aggregateId: "index",
            entityId: "user-2",
            payload: CreatePaginationPayload(pageSize: 1)));

        PaginatedResult<UserTenantMembership>? firstPage = DeserializePayload<PaginatedResult<UserTenantMembership>>(firstResult);
        _ = firstPage.ShouldNotBeNull();
        firstPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-001"]);
        _ = firstPage.Cursor.ShouldNotBeNull();
        firstPage.HasMore.ShouldBeTrue();

        indexModel.Apply(new Contracts.Events.UserRemovedFromTenant("tenant-002", "user-2"));

        QueryResult secondResult = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "owner-1",
            aggregateId: "index",
            entityId: "user-2",
            payload: CreatePaginationPayload(cursor: firstPage.Cursor, pageSize: 10)));

        secondResult.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? secondPage = DeserializePayload<PaginatedResult<UserTenantMembership>>(secondResult);
        _ = secondPage.ShouldNotBeNull();
        secondPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-003"]);
        secondPage.Items.ShouldAllBe(i => i.TenantId != "tenant-002");
    }

    [Fact]
    public async Task GetUserTenants_tenant_owner_requester_demoted_between_pages_is_filteredAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3, new() {
            ["owner-1"] = new() {
                ["tenant-001"] = TenantRole.TenantOwner,
                ["tenant-002"] = TenantRole.TenantOwner,
                ["tenant-003"] = TenantRole.TenantOwner,
            },
            ["user-2"] = new() {
                ["tenant-001"] = TenantRole.TenantReader,
                ["tenant-002"] = TenantRole.TenantReader,
                ["tenant-003"] = TenantRole.TenantReader,
            },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult firstResult = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "owner-1",
            aggregateId: "index",
            entityId: "user-2",
            payload: CreatePaginationPayload(pageSize: 1)));

        PaginatedResult<UserTenantMembership>? firstPage = DeserializePayload<PaginatedResult<UserTenantMembership>>(firstResult);
        _ = firstPage.ShouldNotBeNull();
        firstPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-001"]);
        _ = firstPage.Cursor.ShouldNotBeNull();
        firstPage.HasMore.ShouldBeTrue();

        indexModel.Apply(new Contracts.Events.UserRoleChanged(
            "tenant-002",
            "owner-1",
            TenantRole.TenantOwner,
            TenantRole.TenantReader));

        QueryResult secondResult = await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "owner-1",
            aggregateId: "index",
            entityId: "user-2",
            payload: CreatePaginationPayload(cursor: firstPage.Cursor, pageSize: 10)));

        secondResult.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? secondPage = DeserializePayload<PaginatedResult<UserTenantMembership>>(secondResult);
        _ = secondPage.ShouldNotBeNull();
        secondPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-003"]);
        secondPage.Items.ShouldAllBe(i => i.TenantId != "tenant-002");
    }

    // --- Q26: Cursor anchor missing/hidden continues from lower bound ---
    [Fact]
    public async Task ListTenants_cursor_anchor_missing_continues_from_lower_boundAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        // Create tenants A, C, D, E (B missing - simulates deletion)
        TenantIndexReadModel indexModel = new();
        indexModel.Apply(new Contracts.Events.TenantCreated("A", "Tenant A", null, DateTimeOffset.UtcNow));
        indexModel.Apply(new Contracts.Events.TenantCreated("C", "Tenant C", null, DateTimeOffset.UtcNow));
        indexModel.Apply(new Contracts.Events.TenantCreated("D", "Tenant D", null, DateTimeOffset.UtcNow));
        indexModel.Apply(new Contracts.Events.TenantCreated("E", "Tenant E", null, DateTimeOffset.UtcNow));
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        ITenantQueryCursorCodec cursorCodec = CreateCursorCodec();
        TenantsProjectionActor actor = CreateActor(daprClient, cursorCodec);
        // Cursor="B" (deleted), should return C, D, E
        byte[] payload = CreatePaginationPayload(
            cursor: cursorCodec.Encode(ListTenantsQuery.QueryType, TenantQueryCursorScopes.ListTenants("admin-1"), "B"),
            pageSize: 10);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(3);
        page.Items[0].TenantId.ShouldBe("C");
    }

    [Fact]
    public async Task ListTenants_non_admin_membership_removed_between_pages_is_filteredAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3, new() {
            ["user-1"] = new() {
                ["tenant-001"] = TenantRole.TenantReader,
                ["tenant-002"] = TenantRole.TenantContributor,
                ["tenant-003"] = TenantRole.TenantOwner,
            },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult firstResult = await actor.QueryAsync(CreateEnvelope(
            "list-tenants",
            userId: "user-1",
            aggregateId: "index",
            payload: CreatePaginationPayload(pageSize: 1)));

        PaginatedResult<TenantSummary>? firstPage = DeserializePayload<PaginatedResult<TenantSummary>>(firstResult);
        _ = firstPage.ShouldNotBeNull();
        firstPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-001"]);
        _ = firstPage.Cursor.ShouldNotBeNull();
        firstPage.HasMore.ShouldBeTrue();

        indexModel.Apply(new Contracts.Events.UserRemovedFromTenant("tenant-002", "user-1"));

        QueryResult secondResult = await actor.QueryAsync(CreateEnvelope(
            "list-tenants",
            userId: "user-1",
            aggregateId: "index",
            payload: CreatePaginationPayload(cursor: firstPage.Cursor, pageSize: 10)));

        secondResult.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? secondPage = DeserializePayload<PaginatedResult<TenantSummary>>(secondResult);
        _ = secondPage.ShouldNotBeNull();
        secondPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-003"]);
        secondPage.Items.ShouldAllBe(i => i.TenantId != "tenant-002");
        secondPage.HasMore.ShouldBeFalse();
    }

    // --- Q20: Empty TenantIndexReadModel returns empty paginated result ---
    [Fact]
    public async Task ListTenants_empty_index_returns_empty_result() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupTenantIndexState(daprClient, new TenantIndexReadModel());
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(pageSize: 20);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(0);
        page.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task ListTenants_rejects_invalid_cursor_before_empty_index_responseAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        ITenantQueryCursorCodec cursorCodec = CreateCursorCodec();
        string wrongUserCursor = cursorCodec.Encode(
            ListTenantsQuery.QueryType,
            TenantQueryCursorScopes.ListTenants("other-user"),
            "tenant-001");

        TenantsProjectionActor actor = CreateActor(daprClient, cursorCodec);
        QueryResult result = await actor.QueryAsync(CreateEnvelope(
            "list-tenants",
            aggregateId: "index",
            payload: CreatePaginationPayload(cursor: wrongUserCursor)));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Invalid cursor.");
        result.PayloadBytes.ShouldBeNull();
        _ = await daprClient.DidNotReceive().GetStateAsync<TenantIndexReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantIndexProjectionKey,
            cancellationToken: Arg.Any<CancellationToken>());
    }

    // --- Q10: GlobalAdmin ListTenants returns all tenants ---
    [Fact]
    public async Task ListTenants_global_admin_returns_all() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(5);
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(pageSize: 20);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(5);
    }

    // --- Q13: Last page has HasMore=false ---
    [Fact]
    public async Task ListTenants_last_page_has_no_more() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(5);
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(pageSize: 10);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index", payload: payload));

        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(5);
        page.HasMore.ShouldBeFalse();
        page.Cursor.ShouldBeNull();
    }

    // --- Q25: Malformed cursor safely rejected ---
    [Fact]
    public async Task ListTenants_malformed_cursor_returns_invalid_cursor_error() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(5);
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(cursor: "zzz-nonexistent", pageSize: 10);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index", payload: payload));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Invalid cursor.");
    }

    [Fact]
    public async Task ListTenants_malformed_pagination_payload_uses_standard_default_first_pageAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(25);
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope(
            "list-tenants",
            userId: "admin-1",
            aggregateId: "index",
            payload: "{ not json"u8.ToArray()));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(20);
        page.HasMore.ShouldBeTrue();
        await AssertNoStateWriteAsync(daprClient);
    }

    [Fact]
    public async Task ListTenants_pagination_payload_omitting_page_size_uses_standard_defaultAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(25);
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope(
            "list-tenants",
            userId: "admin-1",
            aggregateId: "index",
            payload: "{}"u8.ToArray()));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(TenantQueryPaginationPolicy.StandardDefaultPageSize);
        page.HasMore.ShouldBeTrue();
    }

    // --- Q9: ListTenants filters by user membership (non-admin) ---
    [Fact]
    public async Task ListTenants_non_admin_filters_by_membership() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(5, new() {
            ["user-1"] = new() { ["tenant-001"] = TenantRole.TenantReader, ["tenant-003"] = TenantRole.TenantContributor },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(pageSize: 20);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("list-tenants", aggregateId: "index", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task ListTenants_non_admin_excludes_unknown_role_memberships_before_paginationAsync() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3, new() {
            ["user-1"] = new() {
                ["tenant-001"] = TenantRole.TenantReader,
                ["tenant-002"] = TenantRole.Unknown,
                ["tenant-003"] = TenantRole.TenantContributor,
            },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope(
            "list-tenants",
            aggregateId: "index",
            payload: CreatePaginationPayload(pageSize: 1)));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Select(i => i.TenantId).ShouldBe(["tenant-001"]);
        page.HasMore.ShouldBeTrue();
        page.Cursor.ShouldNotBeNullOrWhiteSpace();

        QueryResult secondResult = await actor.QueryAsync(CreateEnvelope(
            "list-tenants",
            aggregateId: "index",
            payload: CreatePaginationPayload(cursor: page.Cursor, pageSize: 10)));

        secondResult.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? secondPage = DeserializePayload<PaginatedResult<TenantSummary>>(secondResult);
        _ = secondPage.ShouldNotBeNull();
        secondPage.Items.Select(i => i.TenantId).ShouldBe(["tenant-003"]);
        secondPage.HasMore.ShouldBeFalse();
        secondPage.Cursor.ShouldBeNull();
    }

    // --- Q11: Pagination returns correct first page ---
    [Fact]
    public async Task ListTenants_pagination_first_page() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(10);
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(pageSize: 3);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        _ = page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(3);
        page.HasMore.ShouldBeTrue();
        _ = page.Cursor.ShouldNotBeNull();
        page.Cursor.ShouldNotContain("tenant-003");
    }

    // --- Q12: Pagination with cursor returns next page ---
    [Fact]
    public async Task ListTenants_pagination_with_cursor() {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(10);
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);

        // First page
        byte[] payload1 = CreatePaginationPayload(pageSize: 3);
        QueryResult result1 = await actor.QueryAsync(CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index", payload: payload1));
        PaginatedResult<TenantSummary>? page1 = DeserializePayload<PaginatedResult<TenantSummary>>(result1);

        // Second page with cursor
        byte[] payload2 = CreatePaginationPayload(cursor: page1!.Cursor, pageSize: 3);
        QueryResult result2 = await actor.QueryAsync(CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index", payload: payload2));
        PaginatedResult<TenantSummary>? page2 = DeserializePayload<PaginatedResult<TenantSummary>>(result2);

        _ = page2.ShouldNotBeNull();
        page2.Items.Count.ShouldBe(3);
        page2.HasMore.ShouldBeTrue();

        // No overlap between pages
        var page1Ids = page1.Items.Select(t => t.TenantId).ToHashSet();
        page2.Items.ShouldAllBe(t => !page1Ids.Contains(t.TenantId));
    }

    // --- Q19: Unknown query type returns error ---
    [Fact]
    public async Task Unknown_query_type_returns_error() {
        DaprClient daprClient = Substitute.For<DaprClient>();

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("unknown-query"));

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Unknown query type");
    }

    private static TenantsProjectionActor CreateActor(DaprClient daprClient)
        => CreateActor(daprClient, CreateCursorCodec());

    private static TenantsProjectionActor CreateActor(DaprClient daprClient, ITenantQueryCursorCodec cursorCodec) {
        return CreateActor(daprClient, cursorCodec, NullLogger<TenantsProjectionActor>.Instance);
    }

    private static TenantsProjectionActor CreateActor(
        DaprClient daprClient,
        ITenantQueryCursorCodec cursorCodec,
        ILogger<TenantsProjectionActor> logger) {
        var host = ActorHost.CreateForTest<TenantsProjectionActor>(
            new ActorTestOptions { ActorId = new ActorId("test-actor") });
        IETagService eTagService = Substitute.For<IETagService>();
        return new TenantsProjectionActor(host, eTagService, daprClient, cursorCodec, logger);
    }

    private static ITenantQueryCursorCodec CreateCursorCodec()
        => new TenantQueryCursorCodec(new EphemeralDataProtectionProvider());

    private static QueryEnvelope CreateEnvelope(
        string queryType,
        string userId = "user-1",
        string aggregateId = "tenant-1",
        string? entityId = null,
        byte[]? payload = null,
        string? correlationId = null) => new(
            tenantId: "system",
            domain: "tenants",
            aggregateId: aggregateId,
            queryType: queryType,
            payload: payload ?? [],
            correlationId: correlationId ?? Guid.NewGuid().ToString(),
            userId: userId,
            entityId: entityId);

    private static string GetAggregateIdForQuery(string queryType)
        => queryType is "list-tenants" or "get-user-tenants" ? "index" : "tenant-1";

    private static string? GetEntityIdForQuery(string queryType)
        => queryType == "get-user-tenants" ? "user-1" : null;

    private static async Task AssertNoProjectionStateReadAsync(
        DaprClient daprClient,
        string queryType,
        string aggregateId) {
        switch (queryType) {
            case "get-tenant":
                _ = await daprClient.DidNotReceive().GetStateAsync<TenantReadModel>(
                    TenantsProjectionActor.StateStoreName,
                    TenantsProjectionActor.TenantProjectionKeyPrefix + aggregateId,
                    cancellationToken: Arg.Any<CancellationToken>());
                _ = await daprClient.DidNotReceive().GetStateAsync<GlobalAdministratorReadModel>(
                    TenantsProjectionActor.StateStoreName,
                    TenantsProjectionActor.GlobalAdminProjectionKey,
                    cancellationToken: Arg.Any<CancellationToken>());
                break;
            case "list-tenants":
            case "get-user-tenants":
                _ = await daprClient.DidNotReceive().GetStateAsync<TenantIndexReadModel>(
                    TenantsProjectionActor.StateStoreName,
                    TenantsProjectionActor.TenantIndexProjectionKey,
                    cancellationToken: Arg.Any<CancellationToken>());
                _ = await daprClient.DidNotReceive().GetStateAsync<GlobalAdministratorReadModel>(
                    TenantsProjectionActor.StateStoreName,
                    TenantsProjectionActor.GlobalAdminProjectionKey,
                    cancellationToken: Arg.Any<CancellationToken>());
                break;
            case "get-tenant-users":
                _ = await daprClient.DidNotReceive().GetStateAsync<TenantReadModel>(
                    TenantsProjectionActor.StateStoreName,
                    TenantsProjectionActor.TenantProjectionKeyPrefix + aggregateId,
                    cancellationToken: Arg.Any<CancellationToken>());
                _ = await daprClient.DidNotReceive().GetStateAsync<GlobalAdministratorReadModel>(
                    TenantsProjectionActor.StateStoreName,
                    TenantsProjectionActor.GlobalAdminProjectionKey,
                    cancellationToken: Arg.Any<CancellationToken>());
                break;
            case "get-tenant-audit":
                _ = await daprClient.DidNotReceive().GetStateAsync<GlobalAdministratorReadModel>(
                    TenantsProjectionActor.StateStoreName,
                    TenantsProjectionActor.GlobalAdminProjectionKey,
                    cancellationToken: Arg.Any<CancellationToken>());
                _ = await daprClient.DidNotReceive().GetStateAsync<TenantAuditReadModel>(
                    TenantsProjectionActor.StateStoreName,
                    TenantsProjectionActor.TenantAuditProjectionKeyPrefix + aggregateId,
                    cancellationToken: Arg.Any<CancellationToken>());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(queryType), queryType, "Unhandled role-sensitive query type — add a state-read short-circuit case.");
        }
    }

    private static async Task AssertNoStateWriteAsync(DaprClient daprClient)
        => await daprClient.DidNotReceive().SaveStateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<object?>(),
            Arg.Any<Dapr.Client.StateOptions>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());

    private static GlobalAdministratorReadModel CreateGlobalAdminModel(params string[] adminUserIds) {
        GlobalAdministratorReadModel model = new();
        foreach (string userId in adminUserIds) {
            model.Apply(new Contracts.Events.GlobalAdministratorSet("system", userId));
        }

        return model;
    }

    private static async Task<QueryResult> QueryListTenantsWithOversizedPageAsync(
        DaprClient daprClient,
        TenantsProjectionActor actor) {
        SetupTenantIndexState(daprClient, CreateTenantIndexModel(101));
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        return await actor.QueryAsync(CreateEnvelope(
            "list-tenants",
            userId: "admin-1",
            aggregateId: "index",
            payload: CreatePaginationPayload(pageSize: 101))).ConfigureAwait(false);
    }

    private static async Task<QueryResult> QueryTenantUsersWithOversizedPageAsync(
        DaprClient daprClient,
        TenantsProjectionActor actor) {
        Dictionary<string, TenantRole> members = Enumerable.Range(1, 101)
            .ToDictionary(
                i => $"user-{i:D3}",
                i => i == 1 ? TenantRole.TenantOwner : TenantRole.TenantReader,
                StringComparer.Ordinal);
        TenantReadModel model = CreateTenantReadModel(members: members);
        SetupTenantState(daprClient, "tenant-1", model);
        SetupNoGlobalAdmin(daprClient);

        return await actor.QueryAsync(CreateEnvelope(
            "get-tenant-users",
            userId: "user-001",
            payload: CreatePaginationPayload(pageSize: 101))).ConfigureAwait(false);
    }

    private static async Task<QueryResult> QueryUserTenantsWithOversizedPageAsync(
        DaprClient daprClient,
        TenantsProjectionActor actor) {
        Dictionary<string, Dictionary<string, TenantRole>> userTenants = new(StringComparer.Ordinal) {
            ["user-1"] = Enumerable.Range(1, 101)
                .ToDictionary(i => $"tenant-{i:D3}", _ => TenantRole.TenantReader, StringComparer.Ordinal),
        };
        SetupTenantIndexState(daprClient, CreateTenantIndexModel(101, userTenants));
        SetupNoGlobalAdmin(daprClient);

        return await actor.QueryAsync(CreateEnvelope(
            "get-user-tenants",
            userId: "user-1",
            aggregateId: "index",
            entityId: "user-1",
            payload: CreatePaginationPayload(pageSize: 101))).ConfigureAwait(false);
    }

    private static int CountPayloadItems(QueryResult result) {
        using JsonDocument document = JsonDocument.Parse(result.PayloadBytes!);
        return document.RootElement.GetProperty("items").GetArrayLength();
    }

    private static bool GetPayloadHasMore(QueryResult result) {
        using JsonDocument document = JsonDocument.Parse(result.PayloadBytes!);
        return document.RootElement.GetProperty("hasMore").GetBoolean();
    }

    private static byte[] CreatePaginationPayload(string? cursor = null, int pageSize = TenantQueryPaginationPolicy.StandardDefaultPageSize)
        => JsonSerializer.SerializeToUtf8Bytes(new { cursor, pageSize });

    private static byte[] CreateAuditPayload(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        string? category = null,
        string? cursor = null,
        int pageSize = TenantQueryPaginationPolicy.AuditDefaultPageSize) =>
        JsonSerializer.SerializeToUtf8Bytes(new { from, to, category, cursor, pageSize });

    private static TenantAuditEntry CreateAuditEntry(
        string eventId,
        string eventType,
        AuditEventCategory category,
        DateTimeOffset? timestamp = null) =>
        new(
            eventId,
            eventType,
            category,
            "actor-1",
            timestamp ?? new DateTimeOffset(2026, 5, 14, 10, 0, 0, TimeSpan.Zero),
            "tenant-1",
            new Dictionary<string, string> { ["key"] = "value" });

    private static TenantAuditReadModel CreateAuditModel(params TenantAuditEntry[] entries) => new() {
        Entries = [.. entries],
    };

    private static TenantIndexReadModel CreateTenantIndexModel(int tenantCount, Dictionary<string, Dictionary<string, TenantRole>>? userTenants = null) {
        TenantIndexReadModel model = new();
        for (int i = 1; i <= tenantCount; i++) {
            model.Apply(new Contracts.Events.TenantCreated($"tenant-{i:D3}", $"Tenant {i}", null, DateTimeOffset.UtcNow));
        }

        if (userTenants is not null) {
            foreach (KeyValuePair<string, Dictionary<string, TenantRole>> userEntry in userTenants) {
                foreach (KeyValuePair<string, TenantRole> tenantRole in userEntry.Value) {
                    model.Apply(new Contracts.Events.UserAddedToTenant(tenantRole.Key, userEntry.Key, tenantRole.Value));
                }
            }
        }

        return model;
    }

    private static TenantReadModel CreateTenantReadModel(
                                                                                                        string tenantId = "tenant-1",
        string name = "Test Tenant",
        Dictionary<string, TenantRole>? members = null) {
        TenantReadModel model = new();
        model.Apply(new Contracts.Events.TenantCreated(tenantId, name, "Test", DateTimeOffset.UtcNow));
        if (members is not null) {
            foreach (KeyValuePair<string, TenantRole> m in members) {
                model.Apply(new Contracts.Events.UserAddedToTenant(tenantId, m.Key, m.Value));
            }
        }

        return model;
    }

    private static T? DeserializePayload<T>(QueryResult result)
        => result.GetPayload().Deserialize<T>(_jsonOptions);

    private static void SetupGlobalAdminState(DaprClient daprClient, GlobalAdministratorReadModel model) => daprClient.GetStateAsync<GlobalAdministratorReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.GlobalAdminProjectionKey)
            .Returns(Task.FromResult(model)!);

    private static void SetupNoGlobalAdmin(DaprClient daprClient) => daprClient.GetStateAsync<GlobalAdministratorReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.GlobalAdminProjectionKey)
            .Returns(Task.FromResult<GlobalAdministratorReadModel>(null!)!);

    private static void SetupTenantIndexState(DaprClient daprClient, TenantIndexReadModel model) => daprClient.GetStateAsync<TenantIndexReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantIndexProjectionKey)
            .Returns(Task.FromResult(model)!);

    private static void SetupTenantState(DaprClient daprClient, string tenantId, TenantReadModel model) => daprClient.GetStateAsync<TenantReadModel>(
                        TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantProjectionKeyPrefix + tenantId)
            .Returns(Task.FromResult(model)!);

    private static void SetupAuditState(DaprClient daprClient, string tenantId, TenantAuditReadModel? model) => daprClient.GetStateAsync<TenantAuditReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantAuditProjectionKeyPrefix + tenantId)
            .Returns(Task.FromResult(model)!);

    private sealed record LogEntry(
        LogLevel Level,
        EventId EventId,
        string Message,
        IReadOnlyDictionary<string, object?> State);

    private sealed class ListLogger<T> : ILogger<T> {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
            => NullLogger<T>.Instance.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            Dictionary<string, object?> stateMap = new(StringComparer.Ordinal);
            if (state is IEnumerable<KeyValuePair<string, object?>> pairs) {
                foreach (KeyValuePair<string, object?> pair in pairs) {
                    stateMap[pair.Key] = pair.Value;
                }
            }

            Entries.Add(new(logLevel, eventId, formatter(state, exception), stateMap));
        }
    }
}
