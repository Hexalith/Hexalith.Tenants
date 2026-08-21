using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Queries;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Server.Tests.Support;

using Microsoft.AspNetCore.DataProtection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

public sealed class TenantQueryFreshnessTests {
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 13, 0, 0, TimeSpan.Zero);
    private static readonly ReadModelFreshnessOptions Thresholds = new() {
        Aging = TimeSpan.FromMinutes(10),
        Stale = TimeSpan.FromMinutes(30),
    };

    [Theory]
    [InlineData(5, false)]
    [InlineData(20, false)]
    [InlineData(40, true)]
    public async Task Get_tenant_classifies_projected_at_age_server_sideAsync(int projectedAgeMinutes, bool expectedIsStale) {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupTenant(store, "tenant-etag-1", Now - TimeSpan.FromMinutes(projectedAgeMinutes));
        SetupGlobalAdministrators(store, "admin-user");

        TenantQueryResult result = (await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            CreateEnvelope(GetTenantQuery.QueryType),
            freshnessOptions: Thresholds,
            timeProvider: new FixedTimeProvider(Now))).ShouldBeOfType<TenantQueryResult>();

        result.Metadata.ShouldNotBeNull().IsStale.ShouldBe(expectedIsStale);
        result.Metadata.ServedAt.ShouldBe(Now);
        result.Metadata.ProjectionVersion.ShouldBe("tenant-etag-1");
    }

    [Fact]
    public async Task Get_tenant_without_projected_at_reports_unknown_freshnessAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupTenant(store, "tenant-etag-1", projectedAt: null);
        SetupGlobalAdministrators(store, "admin-user");

        TenantQueryResult result = (await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            CreateEnvelope(GetTenantQuery.QueryType),
            freshnessOptions: Thresholds,
            timeProvider: new FixedTimeProvider(Now))).ShouldBeOfType<TenantQueryResult>();

        result.Metadata.ShouldNotBeNull().IsStale.ShouldBeNull();
        result.Metadata.ServedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task Get_tenant_with_projected_at_and_no_etag_still_classifies_freshnessAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupTenant(store, " ", Now - TimeSpan.FromMinutes(40));
        SetupGlobalAdministrators(store, "admin-user");

        TenantQueryResult result = (await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            CreateEnvelope(GetTenantQuery.QueryType),
            freshnessOptions: Thresholds,
            timeProvider: new FixedTimeProvider(Now))).ShouldBeOfType<TenantQueryResult>();

        result.Metadata.ShouldNotBeNull().ETag.ShouldBeNull();
        result.Metadata.IsStale.ShouldBe(true);
        result.Metadata.ServedAt.ShouldBe(Now);
        result.Metadata.ProjectionVersion.ShouldBeNull();
    }

    [Fact]
    public async Task Get_tenant_prefers_persisted_projection_version_over_etagAsync() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupTenant(
            store,
            "opaque-store-etag",
            Now - TimeSpan.FromMinutes(5),
            projectionVersion: "tenant-sequence:10");
        SetupGlobalAdministrators(store, "admin-user");

        TenantQueryResult result = (await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            CreateEnvelope(GetTenantQuery.QueryType),
            freshnessOptions: Thresholds,
            timeProvider: new FixedTimeProvider(Now))).ShouldBeOfType<TenantQueryResult>();

        result.Metadata.ShouldNotBeNull().ETag.ShouldBe("opaque-store-etag");
        result.Metadata.ProjectionVersion.ShouldBe("tenant-sequence:10");
    }

    [Theory]
    [InlineData("list-tenants", "projection:tenant-index:singleton", "index-etag-1", false)]
    [InlineData("get-user-tenants", "projection:tenant-index:singleton", "index-etag-1", false)]
    [InlineData("get-tenant", "projection:tenants:tenant.alpha", "tenant-etag-1", false)]
    [InlineData("get-tenant-users", "projection:tenants:tenant.alpha", "tenant-etag-1", false)]
    [InlineData("get-tenant-audit", "audit:tenant.alpha", "audit-etag-1", true)]
    [InlineData("get-global-administrators", "projection:global-administrators:singleton", "admin-etag-1", false)]
    public async Task Query_handlers_classify_from_primary_read_model_projected_atAsync(
        string queryType,
        string expectedPrimaryKey,
        string expectedETag,
        bool expectedIsStale) {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupTenantIndex(store, expectedPrimaryKey == TenantQueryHandlerBase.TenantIndexProjectionKey ? Now.AddMinutes(-5) : Now.AddMinutes(-40));
        SetupTenant(store, expectedPrimaryKey == TenantQueryHandlerBase.TenantProjectionKeyPrefix + "tenant.alpha" ? expectedETag : "tenant-etag-2", Now.AddMinutes(-5));
        SetupTenantAudit(store, expectedPrimaryKey == TenantQueryHandlerBase.TenantAuditProjectionKeyPrefix + "tenant.alpha" ? Now.AddMinutes(-40) : Now.AddMinutes(-5));
        SetupGlobalAdministrators(store, "admin-user", projectedAt: Now.AddMinutes(-5));

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            CreateEnvelope(queryType),
            freshnessOptions: Thresholds,
            timeProvider: new FixedTimeProvider(Now));

        TenantQueryResult tenantResult = result.ShouldBeOfType<TenantQueryResult>();
        tenantResult.Metadata.ShouldNotBeNull().ETag.ShouldBe(expectedETag);
        tenantResult.Metadata.IsStale.ShouldBe(expectedIsStale);
        tenantResult.Metadata.ServedAt.ShouldBe(Now);
    }

    private static QueryEnvelope CreateEnvelope(string queryType) {
        if (string.Equals(queryType, ListTenantsQuery.QueryType, StringComparison.Ordinal)) {
            return new QueryEnvelope(
                TenantIdentity.DefaultTenantId,
                ListTenantsQuery.Domain,
                "index",
                ListTenantsQuery.QueryType,
                JsonSerializer.SerializeToUtf8Bytes(new { cursor = (string?)null, pageSize = 20 }),
                "correlation-1",
                "admin-user",
                "admin-user");
        }

        if (string.Equals(queryType, GetUserTenantsQuery.QueryType, StringComparison.Ordinal)) {
            return new QueryEnvelope(
                TenantIdentity.DefaultTenantId,
                GetUserTenantsQuery.Domain,
                "index",
                GetUserTenantsQuery.QueryType,
                JsonSerializer.SerializeToUtf8Bytes(new { cursor = (string?)null, pageSize = 20 }),
                "correlation-1",
                "admin-user",
                "target-user");
        }

        if (string.Equals(queryType, GetTenantQuery.QueryType, StringComparison.Ordinal)) {
            return new QueryEnvelope(
                TenantIdentity.DefaultTenantId,
                GetTenantQuery.Domain,
                "tenant.alpha",
                GetTenantQuery.QueryType,
                [],
                "correlation-1",
                "test-user",
                "tenant.alpha");
        }

        if (string.Equals(queryType, GetTenantUsersQuery.QueryType, StringComparison.Ordinal)) {
            return new QueryEnvelope(
                TenantIdentity.DefaultTenantId,
                GetTenantUsersQuery.Domain,
                "tenant.alpha",
                GetTenantUsersQuery.QueryType,
                JsonSerializer.SerializeToUtf8Bytes(new { cursor = (string?)null, pageSize = 20 }),
                "correlation-1",
                "test-user",
                "tenant.alpha");
        }

        if (string.Equals(queryType, GetTenantAuditQuery.QueryType, StringComparison.Ordinal)) {
            return new QueryEnvelope(
                TenantIdentity.DefaultTenantId,
                GetTenantAuditQuery.Domain,
                "tenant.alpha",
                GetTenantAuditQuery.QueryType,
                JsonSerializer.SerializeToUtf8Bytes(new { cursor = (string?)null, pageSize = 20 }),
                "correlation-1",
                "admin-user",
                "tenant.alpha");
        }

        if (string.Equals(queryType, GetGlobalAdministratorsQuery.QueryType, StringComparison.Ordinal)) {
            return new QueryEnvelope(
                TenantIdentity.DefaultTenantId,
                GetGlobalAdministratorsQuery.Domain,
                TenantIdentity.GlobalAdministratorsAggregateId,
                GetGlobalAdministratorsQuery.QueryType,
                JsonSerializer.SerializeToUtf8Bytes(new { cursor = (string?)null, pageSize = 20 }),
                "correlation-1",
                "admin-user",
                TenantIdentity.GlobalAdministratorsAggregateId,
                isGlobalAdmin: true);
        }

        throw new ArgumentOutOfRangeException(nameof(queryType), queryType, "Unsupported query type.");
    }

    private static IQueryCursorCodec CreateCursorCodec()
        => new QueryCursorCodec(new EphemeralDataProtectionProvider(), "Hexalith.Tenants.QueryCursor.v1");

    private static void SetupGlobalAdministrators(
        IReadModelStore store,
        string administratorId,
        DateTimeOffset? projectedAt = null) {
        var model = new GlobalAdministratorReadModel {
            Administrators = [administratorId],
            ProjectedAt = projectedAt,
        };

        _ = store.GetAsync<GlobalAdministratorReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.GlobalAdminProjectionKey,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<GlobalAdministratorReadModel>(model, "admin-etag-1")));
    }

    private static void SetupTenantIndex(IReadModelStore store, DateTimeOffset? projectedAt) {
        var model = new TenantIndexReadModel {
            ProjectedAt = projectedAt,
            Tenants = {
                ["tenant.alpha"] = new TenantIndexEntry("Tenant Alpha", TenantStatus.Active),
            },
            UserTenants = {
                ["target-user"] = new Dictionary<string, TenantRole>(StringComparer.Ordinal) {
                    ["tenant.alpha"] = TenantRole.TenantReader,
                },
                ["admin-user"] = new Dictionary<string, TenantRole>(StringComparer.Ordinal),
            },
        };

        _ = store.GetAsync<TenantIndexReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.TenantIndexProjectionKey,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<TenantIndexReadModel>(model, "index-etag-1")));
    }

    private static void SetupTenant(
        IReadModelStore store,
        string eTag,
        DateTimeOffset? projectedAt,
        string? projectionVersion = null) {
        var model = new TenantReadModel {
            TenantId = "tenant.alpha",
            Name = "Tenant Alpha",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.Parse("2026-06-07T08:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            ProjectedAt = projectedAt,
            ProjectionVersion = projectionVersion,
            Members = {
                ["test-user"] = TenantRole.TenantReader,
            },
        };

        _ = store.GetAsync<TenantReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.TenantProjectionKeyPrefix + "tenant.alpha",
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<TenantReadModel>(model, eTag)));
    }

    private static void SetupTenantAudit(IReadModelStore store, DateTimeOffset? projectedAt) {
        var model = new TenantAuditReadModel {
            ProjectedAt = projectedAt,
            Entries = [
                new TenantAuditEntry(
                    "event-1",
                    "TenantCreated",
                    AuditEventCategory.Administrative,
                    "admin-user",
                    DateTimeOffset.Parse("2026-06-07T08:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
                    "tenant.alpha",
                    new Dictionary<string, string>(StringComparer.Ordinal)),
            ],
        };

        _ = store.GetAsync<TenantAuditReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.TenantAuditProjectionKeyPrefix + "tenant.alpha",
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<TenantAuditReadModel>(model, "audit-etag-1")));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
