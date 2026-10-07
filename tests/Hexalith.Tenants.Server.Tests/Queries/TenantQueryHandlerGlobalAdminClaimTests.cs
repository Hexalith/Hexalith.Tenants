using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Server.Tests.Support;

using Microsoft.AspNetCore.DataProtection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

/// <summary>
/// EventStore Story 5.5 (FR28): the wire <see cref="QueryEnvelope.IsGlobalAdmin"/> hint never authorizes the tenant
/// query handlers on its own. Global-administrator authority is re-evaluated from the current persisted
/// <see cref="GlobalAdministratorReadModel"/>: a caller absent from it is treated as an ordinary user even when the
/// hint is set, and a caller listed in it is authorized even when the hint is absent.
/// </summary>
public sealed class TenantQueryHandlerGlobalAdminClaimTests {
    // A principal that is NOT a member of any tenant.
    private const string ClaimAdmin = "claim-admin";
    private const string TargetUser = "target-user";

    [Fact]
    public async Task List_tenants_wire_admin_hint_alone_does_not_reveal_tenants() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenantIndex(store);
        SeedEmptyGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            ListTenantsEnvelope(ClaimAdmin, isGlobalAdmin: true));

        result.Success.ShouldBeTrue();
        Deserialize<PaginatedResult<TenantSummary>>(result).Items.ShouldBeEmpty();
        _ = await store.Received().GetAsync<GlobalAdministratorReadModel>(
            TenantQueryHandlerBase.StateStoreName,
            TenantQueryHandlerBase.GlobalAdminProjectionKey,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_tenants_without_claim_and_without_membership_returns_authorized_empty() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenantIndex(store);
        SeedEmptyGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            ListTenantsEnvelope(ClaimAdmin, isGlobalAdmin: false));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary> page = Deserialize<PaginatedResult<TenantSummary>>(result);
        page.Items.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task List_tenants_current_global_administrator_sees_all_tenants(bool isGlobalAdmin) {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenantIndex(store);
        SeedGlobalAdministrators(store, ClaimAdmin);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            ListTenantsEnvelope(ClaimAdmin, isGlobalAdmin));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary> page = Deserialize<PaginatedResult<TenantSummary>>(result);
        page.Items.Select(static i => i.TenantId).ShouldBe(["tenant.alpha", "tenant.beta"], ignoreOrder: true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Get_tenant_forbids_non_member_who_is_not_a_current_global_administrator(bool isGlobalAdmin) {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenant(store);
        SeedEmptyGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantEnvelope(ClaimAdmin, isGlobalAdmin));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Forbidden");
    }

    [Fact]
    public async Task Get_tenant_authorizes_non_member_current_global_administrator() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenant(store);
        SeedGlobalAdministrators(store, ClaimAdmin);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantEnvelope(ClaimAdmin, isGlobalAdmin: false));

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task Get_tenant_wire_admin_hint_cannot_probe_missing_tenants() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedEmptyGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantEnvelope(ClaimAdmin, isGlobalAdmin: true));

        // Without current authority the caller must not distinguish "missing" from "exists-but-unauthorized".
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
    }

    [Fact]
    public async Task Get_tenant_current_global_administrator_querying_missing_tenant_is_not_found() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedGlobalAdministrators(store, ClaimAdmin);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantEnvelope(ClaimAdmin, isGlobalAdmin: false));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Tenant not found");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Get_tenant_users_forbids_non_member_who_is_not_a_current_global_administrator(bool isGlobalAdmin) {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenant(store);
        SeedEmptyGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantUsersEnvelope(ClaimAdmin, isGlobalAdmin));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Forbidden");
    }

    [Fact]
    public async Task Get_tenant_users_authorizes_current_global_administrator() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenant(store);
        SeedGlobalAdministrators(store, ClaimAdmin);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantUsersEnvelope(ClaimAdmin, isGlobalAdmin: false));

        result.Success.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Get_user_tenants_cannot_see_other_users_memberships_without_current_authority(bool isGlobalAdmin) {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenantIndex(store);
        SeedEmptyGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetUserTenantsEnvelope(ClaimAdmin, TargetUser, isGlobalAdmin));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership> page = Deserialize<PaginatedResult<UserTenantMembership>>(result);
        page.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_user_tenants_current_global_administrator_sees_other_users_memberships() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenantIndex(store);
        SeedGlobalAdministrators(store, ClaimAdmin);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetUserTenantsEnvelope(ClaimAdmin, TargetUser, isGlobalAdmin: false));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership> page = Deserialize<PaginatedResult<UserTenantMembership>>(result);
        page.Items.Select(static i => i.TenantId).ShouldBe(["tenant.alpha"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Get_tenant_audit_forbids_without_current_authority(bool isGlobalAdmin) {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedEmptyGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantAuditEnvelope(ClaimAdmin, isGlobalAdmin));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Forbidden");
    }

    [Fact]
    public async Task Get_tenant_audit_authorizes_current_global_administrator() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedGlobalAdministrators(store, ClaimAdmin);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantAuditEnvelope(ClaimAdmin, isGlobalAdmin: false));

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task Get_global_administrators_wire_admin_hint_alone_is_forbidden() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        // Projection exists but does NOT list the caller; the wire hint must not authorize.
        SeedGlobalAdministrators(store, "someone-else");

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetGlobalAdministratorsEnvelope(ClaimAdmin, isGlobalAdmin: true));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
    }

    [Fact]
    public async Task Get_global_administrators_fails_closed_when_projection_is_missing() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedNoGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetGlobalAdministratorsEnvelope(ClaimAdmin, isGlobalAdmin: true));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
    }

    [Fact]
    public async Task Get_global_administrators_authorizes_current_member() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedGlobalAdministrators(store, ClaimAdmin, "someone-else");

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetGlobalAdministratorsEnvelope(ClaimAdmin, isGlobalAdmin: false));

        result.Success.ShouldBeTrue();
    }

    private static QueryEnvelope ListTenantsEnvelope(string userId, bool isGlobalAdmin)
        => new(
            TenantIdentity.DefaultTenantId,
            ListTenantsQuery.Domain,
            "index",
            ListTenantsQuery.QueryType,
            JsonSerializer.SerializeToUtf8Bytes(new { cursor = (string?)null, pageSize = 20 }),
            "correlation-1",
            userId,
            userId,
            isGlobalAdmin);

    private static QueryEnvelope GetTenantEnvelope(string userId, bool isGlobalAdmin)
        => new(
            TenantIdentity.DefaultTenantId,
            GetTenantQuery.Domain,
            "tenant.alpha",
            GetTenantQuery.QueryType,
            [],
            "correlation-1",
            userId,
            "tenant.alpha",
            isGlobalAdmin);

    private static QueryEnvelope GetTenantUsersEnvelope(string userId, bool isGlobalAdmin)
        => new(
            TenantIdentity.DefaultTenantId,
            GetTenantUsersQuery.Domain,
            "tenant.alpha",
            GetTenantUsersQuery.QueryType,
            JsonSerializer.SerializeToUtf8Bytes(new { cursor = (string?)null, pageSize = 20 }),
            "correlation-1",
            userId,
            "tenant.alpha",
            isGlobalAdmin);

    private static QueryEnvelope GetUserTenantsEnvelope(string userId, string targetUserId, bool isGlobalAdmin)
        => new(
            TenantIdentity.DefaultTenantId,
            GetUserTenantsQuery.Domain,
            "index",
            GetUserTenantsQuery.QueryType,
            JsonSerializer.SerializeToUtf8Bytes(new { cursor = (string?)null, pageSize = 20 }),
            "correlation-1",
            userId,
            targetUserId,
            isGlobalAdmin);

    private static QueryEnvelope GetTenantAuditEnvelope(string userId, bool isGlobalAdmin)
        => new(
            TenantIdentity.DefaultTenantId,
            GetTenantAuditQuery.Domain,
            "tenant.alpha",
            GetTenantAuditQuery.QueryType,
            JsonSerializer.SerializeToUtf8Bytes(new { cursor = (string?)null, pageSize = 20 }),
            "correlation-1",
            userId,
            "tenant.alpha",
            isGlobalAdmin);

    private static QueryEnvelope GetGlobalAdministratorsEnvelope(string userId, bool isGlobalAdmin)
        => new(
            TenantIdentity.DefaultTenantId,
            GetGlobalAdministratorsQuery.Domain,
            TenantIdentity.GlobalAdministratorsAggregateId,
            GetGlobalAdministratorsQuery.QueryType,
            JsonSerializer.SerializeToUtf8Bytes(new { cursor = (string?)null, pageSize = 20 }),
            "correlation-1",
            userId,
            TenantIdentity.GlobalAdministratorsAggregateId,
            isGlobalAdmin);

    private static void SeedTenantIndex(IReadModelStore store) {
        var model = new TenantIndexReadModel {
            Tenants = {
                ["tenant.alpha"] = new TenantIndexEntry("Tenant Alpha", TenantStatus.Active),
                ["tenant.beta"] = new TenantIndexEntry("Tenant Beta", TenantStatus.Active),
            },
            UserTenants = {
                [TargetUser] = new Dictionary<string, TenantRole>(StringComparer.Ordinal) {
                    ["tenant.alpha"] = TenantRole.TenantReader,
                },
            },
        };

        _ = store.GetAsync<TenantIndexReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.TenantIndexProjectionKey,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<TenantIndexReadModel>(model, "index-etag")));
    }

    private static void SeedTenant(IReadModelStore store) {
        var model = new TenantReadModel {
            TenantId = "tenant.alpha",
            Name = "Tenant Alpha",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.Parse("2026-06-07T08:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            Members = {
                [TargetUser] = TenantRole.TenantReader,
            },
        };

        _ = store.GetAsync<TenantReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.TenantProjectionKeyPrefix + "tenant.alpha",
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<TenantReadModel>(model, "tenant-etag")));
    }

    private static void SeedGlobalAdministrators(IReadModelStore store, params string[] administratorIds) {
        var model = new GlobalAdministratorReadModel {
            Administrators = administratorIds.ToHashSet(StringComparer.Ordinal),
        };

        _ = store.GetAsync<GlobalAdministratorReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.GlobalAdminProjectionKey,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<GlobalAdministratorReadModel>(model, "admin-etag")));
    }

    private static void SeedEmptyGlobalAdministrators(IReadModelStore store)
        => SeedGlobalAdministrators(store);

    private static void SeedNoGlobalAdministrators(IReadModelStore store)
        => store.GetAsync<GlobalAdministratorReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.GlobalAdminProjectionKey,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<GlobalAdministratorReadModel>(null, null)));

    private static IQueryCursorCodec CreateCursorCodec()
        => new QueryCursorCodec(new EphemeralDataProtectionProvider(), "Hexalith.Tenants.QueryCursor.v1");

    private static T Deserialize<T>(QueryResult result)
        where T : class {
        _ = result.PayloadBytes.ShouldNotBeNull();
        T? payload = JsonSerializer.Deserialize<T>(
            result.PayloadBytes,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return payload.ShouldNotBeNull();
    }
}
