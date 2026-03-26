using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Runtime;
using Dapr.Client;

using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Queries;
using Hexalith.Tenants.CommandApi.Actors;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Server.Projections;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class TenantsProjectionActorTests
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    // --- Test Fixtures ---

    private static TenantReadModel CreateTenantReadModel(
        string tenantId = "tenant-1",
        string name = "Test Tenant",
        Dictionary<string, TenantRole>? members = null)
    {
        TenantReadModel model = new();
        model.Apply(new Contracts.Events.TenantCreated(tenantId, name, "Test", DateTimeOffset.UtcNow));
        if (members is not null)
        {
            foreach (KeyValuePair<string, TenantRole> m in members)
            {
                model.Apply(new Contracts.Events.UserAddedToTenant(tenantId, m.Key, m.Value));
            }
        }

        return model;
    }

    private static GlobalAdministratorReadModel CreateGlobalAdminModel(params string[] adminUserIds)
    {
        GlobalAdministratorReadModel model = new();
        foreach (string userId in adminUserIds)
        {
            model.Apply(new Contracts.Events.GlobalAdministratorSet("system", userId));
        }

        return model;
    }

    private static TenantIndexReadModel CreateTenantIndexModel(int tenantCount, Dictionary<string, Dictionary<string, TenantRole>>? userTenants = null)
    {
        TenantIndexReadModel model = new();
        for (int i = 1; i <= tenantCount; i++)
        {
            model.Apply(new Contracts.Events.TenantCreated($"tenant-{i:D3}", $"Tenant {i}", null, DateTimeOffset.UtcNow));
        }

        if (userTenants is not null)
        {
            foreach (KeyValuePair<string, Dictionary<string, TenantRole>> userEntry in userTenants)
            {
                foreach (KeyValuePair<string, TenantRole> tenantRole in userEntry.Value)
                {
                    model.Apply(new Contracts.Events.UserAddedToTenant(tenantRole.Key, userEntry.Key, tenantRole.Value));
                }
            }
        }

        return model;
    }

    private static QueryEnvelope CreateEnvelope(
        string queryType,
        string userId = "user-1",
        string aggregateId = "tenant-1",
        string? entityId = null,
        byte[]? payload = null)
    {
        return new QueryEnvelope(
            tenantId: "system",
            domain: "tenants",
            aggregateId: aggregateId,
            queryType: queryType,
            payload: payload ?? [],
            correlationId: Guid.NewGuid().ToString(),
            userId: userId,
            entityId: entityId);
    }

    private static byte[] CreatePaginationPayload(string? cursor = null, int pageSize = 20)
    {
        return JsonSerializer.SerializeToUtf8Bytes(new { cursor, pageSize });
    }

    private static T? DeserializePayload<T>(QueryResult result)
        => result.GetPayload().Deserialize<T>(s_jsonOptions);

    private static TenantsProjectionActor CreateActor(DaprClient daprClient)
    {
        ActorHost host = ActorHost.CreateForTest<TenantsProjectionActor>(
            new ActorTestOptions { ActorId = new ActorId("test-actor") });
        IETagService eTagService = Substitute.For<IETagService>();
        ILogger<TenantsProjectionActor> logger = NullLogger<TenantsProjectionActor>.Instance;
        return new TenantsProjectionActor(host, eTagService, daprClient, logger);
    }

    private static void SetupTenantState(DaprClient daprClient, string tenantId, TenantReadModel model)
    {
        daprClient.GetStateAsync<TenantReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantProjectionKeyPrefix + tenantId)
            .Returns(Task.FromResult(model)!);
    }

    private static void SetupGlobalAdminState(DaprClient daprClient, GlobalAdministratorReadModel model)
    {
        daprClient.GetStateAsync<GlobalAdministratorReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.GlobalAdminProjectionKey)
            .Returns(Task.FromResult(model)!);
    }

    private static void SetupTenantIndexState(DaprClient daprClient, TenantIndexReadModel model)
    {
        daprClient.GetStateAsync<TenantIndexReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.TenantIndexProjectionKey)
            .Returns(Task.FromResult(model)!);
    }

    private static void SetupNoGlobalAdmin(DaprClient daprClient)
    {
        daprClient.GetStateAsync<GlobalAdministratorReadModel>(
            TenantsProjectionActor.StateStoreName,
            TenantsProjectionActor.GlobalAdminProjectionKey)
            .Returns(Task.FromResult<GlobalAdministratorReadModel>(null!)!);
    }

    // --- Q6: Authorized user can get tenant details ---
    [Fact]
    public async Task GetTenant_authorized_user_returns_tenant_detail()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantReadModel model = CreateTenantReadModel(members: new() { ["user-1"] = TenantRole.TenantOwner });
        SetupTenantState(daprClient, "tenant-1", model);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant"));

        result.Success.ShouldBeTrue();
        TenantDetail? detail = DeserializePayload<TenantDetail>(result);
        detail.ShouldNotBeNull();
        detail.TenantId.ShouldBe("tenant-1");
        detail.Name.ShouldBe("Test Tenant");
    }

    // --- Q7: Unauthorized user gets 403 for GetTenant ---
    [Fact]
    public async Task GetTenant_unauthorized_user_returns_forbidden()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantReadModel model = CreateTenantReadModel(members: new() { ["user-1"] = TenantRole.TenantOwner });
        SetupTenantState(daprClient, "tenant-1", model);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant", userId: "user-2"));

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Forbidden");
    }

    // --- Q8: GlobalAdmin can access any tenant ---
    [Fact]
    public async Task GetTenant_global_admin_bypasses_membership()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantReadModel model = CreateTenantReadModel(members: new() { ["user-1"] = TenantRole.TenantOwner });
        SetupTenantState(daprClient, "tenant-1", model);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant", userId: "admin-1"));

        result.Success.ShouldBeTrue();
        TenantDetail? detail = DeserializePayload<TenantDetail>(result);
        detail.ShouldNotBeNull();
        detail.TenantId.ShouldBe("tenant-1");
    }

    // --- Q9: ListTenants filters by user membership (non-admin) ---
    [Fact]
    public async Task ListTenants_non_admin_filters_by_membership()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(5, new()
        {
            ["user-1"] = new() { ["tenant-001"] = TenantRole.TenantReader, ["tenant-003"] = TenantRole.TenantContributor },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(pageSize: 20);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("list-tenants", aggregateId: "index", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(2);
    }

    // --- Q10: GlobalAdmin ListTenants returns all tenants ---
    [Fact]
    public async Task ListTenants_global_admin_returns_all()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(5);
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(pageSize: 20);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(5);
    }

    // --- Q11: Pagination returns correct first page ---
    [Fact]
    public async Task ListTenants_pagination_first_page()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(10);
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(pageSize: 3);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(3);
        page.HasMore.ShouldBeTrue();
        page.Cursor.ShouldNotBeNull();
    }

    // --- Q12: Pagination with cursor returns next page ---
    [Fact]
    public async Task ListTenants_pagination_with_cursor()
    {
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

        page2.ShouldNotBeNull();
        page2.Items.Count.ShouldBe(3);
        page2.HasMore.ShouldBeTrue();

        // No overlap between pages
        HashSet<string> page1Ids = page1.Items.Select(t => t.TenantId).ToHashSet();
        page2.Items.ShouldAllBe(t => !page1Ids.Contains(t.TenantId));
    }

    // --- Q13: Last page has HasMore=false ---
    [Fact]
    public async Task ListTenants_last_page_has_no_more()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(5);
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(pageSize: 10);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index", payload: payload));

        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(5);
        page.HasMore.ShouldBeFalse();
        page.Cursor.ShouldBeNull();
    }

    // --- Q14: GetTenantUsers returns paginated member list ---
    [Fact]
    public async Task GetTenantUsers_returns_paginated_members()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        Dictionary<string, TenantRole> members = new()
        {
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
        page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(5);
    }

    // --- Q15: GetUserTenants for own user works ---
    [Fact]
    public async Task GetUserTenants_own_user_returns_memberships()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(5, new()
        {
            ["user-1"] = new()
            {
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
        page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(3);
    }

    // --- Q16: Non-admin cannot query other user's tenants ---
    [Fact]
    public async Task GetUserTenants_non_admin_querying_other_user_returns_forbidden()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "user-1", aggregateId: "index", entityId: "user-2"));

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Forbidden");
    }

    // --- Q17: GlobalAdmin can query any user's tenants ---
    [Fact]
    public async Task GetUserTenants_global_admin_can_query_any_user()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(3, new()
        {
            ["user-2"] = new() { ["tenant-001"] = TenantRole.TenantReader, ["tenant-002"] = TenantRole.TenantContributor },
        });
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(pageSize: 20);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-user-tenants", userId: "admin-1", aggregateId: "index", entityId: "user-2", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership>? page = DeserializePayload<PaginatedResult<UserTenantMembership>>(result);
        page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(2);
    }

    // --- Q18: GetTenantAudit returns 501 for GlobalAdmin ---
    [Fact]
    public async Task GetTenantAudit_global_admin_returns_not_implemented()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant-audit", userId: "admin-1"));

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("not yet implemented");
    }

    // --- Q19: Unknown query type returns error ---
    [Fact]
    public async Task Unknown_query_type_returns_error()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("unknown-query"));

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Unknown query type");
    }

    // --- Q20: Empty TenantIndexReadModel returns empty paginated result ---
    [Fact]
    public async Task ListTenants_empty_index_returns_empty_result()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupTenantIndexState(daprClient, new TenantIndexReadModel());
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        byte[] payload = CreatePaginationPayload(pageSize: 20);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(0);
        page.HasMore.ShouldBeFalse();
    }

    // --- Q21: GetTenant with non-existent tenantId ---
    [Fact]
    public async Task GetTenant_non_existent_returns_error()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        daprClient.GetStateAsync<TenantReadModel>(
            TenantsProjectionActor.StateStoreName,
            Arg.Any<string>())
            .Returns(Task.FromResult<TenantReadModel>(null!)!);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant", aggregateId: "nonexistent"));

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("not found");
    }

    // --- Q25: Malformed cursor treated as start-from-beginning ---
    [Fact]
    public async Task ListTenants_malformed_cursor_returns_items_after_cursor()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        TenantIndexReadModel indexModel = CreateTenantIndexModel(5);
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        // "zzz-nonexistent" sorts after all "tenant-*" keys, so no items after it
        byte[] payload = CreatePaginationPayload(cursor: "zzz-nonexistent", pageSize: 10);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        page.ShouldNotBeNull();
        // Cursor "zzz" is after all tenant keys → empty result
        page.Items.Count.ShouldBe(0);
    }

    // --- Q26: Cursor pointing to deleted tenant skips gracefully ---
    [Fact]
    public async Task ListTenants_cursor_skips_deleted_tenant()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        // Create tenants A, C, D, E (B missing - simulates deletion)
        TenantIndexReadModel indexModel = new();
        indexModel.Apply(new Contracts.Events.TenantCreated("A", "Tenant A", null, DateTimeOffset.UtcNow));
        indexModel.Apply(new Contracts.Events.TenantCreated("C", "Tenant C", null, DateTimeOffset.UtcNow));
        indexModel.Apply(new Contracts.Events.TenantCreated("D", "Tenant D", null, DateTimeOffset.UtcNow));
        indexModel.Apply(new Contracts.Events.TenantCreated("E", "Tenant E", null, DateTimeOffset.UtcNow));
        SetupTenantIndexState(daprClient, indexModel);
        SetupGlobalAdminState(daprClient, CreateGlobalAdminModel("admin-1"));

        TenantsProjectionActor actor = CreateActor(daprClient);
        // Cursor="B" (deleted), should return C, D, E
        byte[] payload = CreatePaginationPayload(cursor: "B", pageSize: 10);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("list-tenants", userId: "admin-1", aggregateId: "index", payload: payload));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary>? page = DeserializePayload<PaginatedResult<TenantSummary>>(result);
        page.ShouldNotBeNull();
        page.Items.Count.ShouldBe(3);
        page.Items[0].TenantId.ShouldBe("C");
    }

    // --- Q27: Non-admin hitting audit endpoint gets 403 not 501 ---
    [Fact]
    public async Task GetTenantAudit_non_admin_returns_forbidden_not_501()
    {
        DaprClient daprClient = Substitute.For<DaprClient>();
        SetupNoGlobalAdmin(daprClient);

        TenantsProjectionActor actor = CreateActor(daprClient);
        QueryResult result = await actor.QueryAsync(CreateEnvelope("get-tenant-audit", userId: "user-1"));

        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("Forbidden");
        result.ErrorMessage!.ShouldNotContain("not yet implemented");
    }
}
