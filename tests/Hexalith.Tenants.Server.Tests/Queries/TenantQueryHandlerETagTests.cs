using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
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

public sealed class TenantQueryHandlerETagTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 14, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("list-tenants", "projection:tenant-index:singleton", "index-etag-1")]
    [InlineData("get-user-tenants", "projection:tenant-index:singleton", "index-etag-2")]
    [InlineData("get-tenant", "projection:tenants:tenant.alpha", "tenant-etag-1")]
    [InlineData("get-tenant-users", "projection:tenants:tenant.alpha", "tenant-etag-2")]
    [InlineData("get-tenant-audit", "audit:tenant.alpha", "audit-etag-1")]
    [InlineData("get-global-administrators", "projection:global-administrators:singleton", "admin-etag-1")]
    public async Task Query_handlers_surface_primary_read_model_etag_only_as_opaque_validator(
        string queryType,
        string expectedPrimaryKey,
        string expectedETag)
    {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupTenantIndex(store, expectedPrimaryKey == TenantQueryHandlerBase.TenantIndexProjectionKey ? expectedETag : "index-etag");
        SetupTenant(store, expectedPrimaryKey == TenantQueryHandlerBase.TenantProjectionKeyPrefix + "tenant.alpha" ? expectedETag : "tenant-etag");
        SetupTenantAudit(store, expectedPrimaryKey == TenantQueryHandlerBase.TenantAuditProjectionKeyPrefix + "tenant.alpha" ? expectedETag : "audit-etag");
        SetupGlobalAdministrators(
            store,
            expectedPrimaryKey == TenantQueryHandlerBase.GlobalAdminProjectionKey ? expectedETag : "admin-etag",
            "admin-user");

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            CreateEnvelope(queryType),
            timeProvider: new FixedTimeProvider(Now));

        result.Success.ShouldBeTrue();
        TenantQueryResult tenantResult = result.ShouldBeOfType<TenantQueryResult>();
        QueryResponseMetadata metadata = tenantResult.Metadata.ShouldNotBeNull();
        metadata.ETag.ShouldBe(expectedETag);
        metadata.IsNotModified.ShouldBe(false);
        metadata.ProjectionVersion.ShouldBeNull();
        metadata.IsStale.ShouldBeNull();
        metadata.IsDegraded.ShouldBeNull();
        metadata.ServedAt.ShouldBeNull();
        metadata.Provenance.ShouldBe(QueryResponseProvenance.Unknown);
        metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
    }

    private static QueryEnvelope CreateEnvelope(string queryType)
    {
        if (string.Equals(queryType, ListTenantsQuery.QueryType, StringComparison.Ordinal))
        {
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

        if (string.Equals(queryType, GetUserTenantsQuery.QueryType, StringComparison.Ordinal))
        {
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

        if (string.Equals(queryType, GetTenantQuery.QueryType, StringComparison.Ordinal))
        {
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

        if (string.Equals(queryType, GetTenantUsersQuery.QueryType, StringComparison.Ordinal))
        {
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

        if (string.Equals(queryType, GetTenantAuditQuery.QueryType, StringComparison.Ordinal))
        {
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

        if (string.Equals(queryType, GetGlobalAdministratorsQuery.QueryType, StringComparison.Ordinal))
        {
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
        string eTag,
        params string[] administratorIds)
    {
        var model = new GlobalAdministratorReadModel
        {
            Administrators = administratorIds.ToHashSet(StringComparer.Ordinal),
            ProjectedAt = Now,
        };

        _ = store.GetAsync<GlobalAdministratorReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.GlobalAdminProjectionKey,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<GlobalAdministratorReadModel>(model, eTag)));
    }

    private static void SetupTenantIndex(IReadModelStore store, string eTag)
    {
        var model = new TenantIndexReadModel
        {
            ProjectedAt = Now,
            Tenants =
            {
                ["tenant.alpha"] = new TenantIndexEntry("Tenant Alpha", TenantStatus.Active),
            },
            UserTenants =
            {
                ["target-user"] = new Dictionary<string, TenantRole>(StringComparer.Ordinal)
                {
                    ["tenant.alpha"] = TenantRole.TenantReader,
                },
                ["admin-user"] = new Dictionary<string, TenantRole>(StringComparer.Ordinal),
            },
        };

        _ = store.GetAsync<TenantIndexReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.TenantIndexProjectionKey,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<TenantIndexReadModel>(model, eTag)));
    }

    private static void SetupTenant(IReadModelStore store, string eTag)
    {
        var model = new TenantReadModel
        {
            TenantId = "tenant.alpha",
            Name = "Tenant Alpha",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.Parse("2026-06-07T08:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            ProjectedAt = Now,
            Members =
            {
                ["test-user"] = TenantRole.TenantReader,
            },
        };

        _ = store.GetAsync<TenantReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.TenantProjectionKeyPrefix + "tenant.alpha",
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<TenantReadModel>(model, eTag)));
    }

    private static void SetupTenantAudit(IReadModelStore store, string eTag)
    {
        var model = new TenantAuditReadModel
        {
            ProjectedAt = Now,
            Entries =
            [
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
            .Returns(Task.FromResult(new ReadModelEntry<TenantAuditReadModel>(model, eTag)));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
