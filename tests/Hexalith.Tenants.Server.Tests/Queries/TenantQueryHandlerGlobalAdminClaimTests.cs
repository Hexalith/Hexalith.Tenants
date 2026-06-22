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
/// Verifies that the trusted, JWT-derived <see cref="QueryEnvelope.IsGlobalAdmin"/> claim authorizes the
/// tenant query handlers even when the persisted <see cref="GlobalAdministratorReadModel"/> projection does
/// not list the caller.
/// <para>
/// This is the production failure mode behind the "No visible tenants" display bug: the global-administrator
/// bootstrap projection is empty, so before the fix a genuine global administrator was treated as an ordinary
/// user and saw an empty tenant list. Each claim-path test is paired with a claim-absent regression guard that
/// proves the persisted-projection fallback (the prior behavior) is unchanged.
/// </para>
/// </summary>
public sealed class TenantQueryHandlerGlobalAdminClaimTests {
    // A principal that is NOT a member of any tenant and is absent from the persisted admin projection.
    private const string ClaimAdmin = "claim-admin";
    private const string TargetUser = "target-user";

    [Fact]
    public async Task List_tenants_shows_all_tenants_to_global_admin_claim_when_bootstrap_projection_is_empty() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenantIndex(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            ListTenantsEnvelope(ClaimAdmin, isGlobalAdmin: true));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary> page = Deserialize<PaginatedResult<TenantSummary>>(result);
        page.Items.Select(static i => i.TenantId).ShouldBe(["tenant.alpha", "tenant.beta"], ignoreOrder: true);

        // The claim must authorize without depending on the (broken/empty) bootstrap projection at all.
        _ = await store.DidNotReceive().GetAsync<GlobalAdministratorReadModel>(
            Arg.Any<string>(),
            Arg.Any<string>(),
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

    [Fact]
    public async Task List_tenants_without_claim_falls_back_to_persisted_admin_projection() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenantIndex(store);
        SeedGlobalAdministrators(store, ClaimAdmin);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            ListTenantsEnvelope(ClaimAdmin, isGlobalAdmin: false));

        result.Success.ShouldBeTrue();
        PaginatedResult<TenantSummary> page = Deserialize<PaginatedResult<TenantSummary>>(result);
        page.Items.Select(static i => i.TenantId).ShouldBe(["tenant.alpha", "tenant.beta"], ignoreOrder: true);
    }

    [Fact]
    public async Task Get_tenant_authorizes_non_member_global_admin_claim() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenant(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantEnvelope(ClaimAdmin, isGlobalAdmin: true));

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task Get_tenant_forbids_non_member_without_claim() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenant(store);
        SeedEmptyGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantEnvelope(ClaimAdmin, isGlobalAdmin: false));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Forbidden");
    }

    [Fact]
    public async Task Get_tenant_with_claim_querying_missing_tenant_is_not_found_not_forbidden() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        // No tenant seeded: the model is null. A claim admin must be told "not found", not "Forbidden",
        // since the claim authorizes them to know the tenant does not exist.

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantEnvelope(ClaimAdmin, isGlobalAdmin: true));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Tenant not found");
    }

    [Fact]
    public async Task Get_tenant_without_claim_querying_missing_tenant_is_forbidden() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedEmptyGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantEnvelope(ClaimAdmin, isGlobalAdmin: false));

        // A non-admin must not be able to distinguish "missing" from "exists-but-unauthorized".
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
    }

    [Fact]
    public async Task Get_tenant_users_authorizes_non_member_global_admin_claim() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenant(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantUsersEnvelope(ClaimAdmin, isGlobalAdmin: true));

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task Get_tenant_users_forbids_non_member_without_claim() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenant(store);
        SeedEmptyGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantUsersEnvelope(ClaimAdmin, isGlobalAdmin: false));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Forbidden");
    }

    [Fact]
    public async Task Get_user_tenants_global_admin_claim_sees_other_users_memberships() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenantIndex(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetUserTenantsEnvelope(ClaimAdmin, TargetUser, isGlobalAdmin: true));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership> page = Deserialize<PaginatedResult<UserTenantMembership>>(result);
        page.Items.Select(static i => i.TenantId).ShouldBe(["tenant.alpha"]);
    }

    [Fact]
    public async Task Get_user_tenants_without_claim_cannot_see_other_users_memberships() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedTenantIndex(store);
        SeedEmptyGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetUserTenantsEnvelope(ClaimAdmin, TargetUser, isGlobalAdmin: false));

        result.Success.ShouldBeTrue();
        PaginatedResult<UserTenantMembership> page = Deserialize<PaginatedResult<UserTenantMembership>>(result);
        page.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_tenant_audit_authorizes_global_admin_claim() {
        IReadModelStore store = Substitute.For<IReadModelStore>();

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantAuditEnvelope(ClaimAdmin, isGlobalAdmin: true));

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task Get_tenant_audit_forbids_without_claim() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedEmptyGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetTenantAuditEnvelope(ClaimAdmin, isGlobalAdmin: false));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Forbidden");
    }

    [Fact]
    public async Task Get_global_administrators_claim_authorizes_user_absent_from_projection() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        // Projection exists but does NOT list the caller; the claim alone must authorize.
        SeedGlobalAdministrators(store, "someone-else");

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetGlobalAdministratorsEnvelope(ClaimAdmin, isGlobalAdmin: true));

        result.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task Get_global_administrators_claim_still_fails_closed_when_projection_is_missing() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SeedNoGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            GetGlobalAdministratorsEnvelope(ClaimAdmin, isGlobalAdmin: true));

        // A null projection means there is nothing to enumerate: deliberately Forbidden even for a claim admin.
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
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
