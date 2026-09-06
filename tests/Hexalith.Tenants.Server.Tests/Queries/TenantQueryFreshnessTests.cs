using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Configuration;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Contracts.Projections;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Queries;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Server.Tests.Support;

using Microsoft.AspNetCore.DataProtection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

public sealed class TenantQueryFreshnessTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 13, 0, 0, TimeSpan.Zero);
    private const string GenuineSequenceVersion = TenantProjectionVersionFormat.SequencePrefix + "42";
    private static readonly ReadModelFreshnessOptions Thresholds = new()
    {
        Aging = TimeSpan.FromMinutes(10),
        Stale = TimeSpan.FromMinutes(30),
    };

    public static IEnumerable<object?[]> HandlerFreshnessCases()
    {
        (string QueryType, string PrimaryKey, string ETag)[] routes =
        [
            (ListTenantsQuery.QueryType, TenantQueryHandlerBase.TenantIndexProjectionKey, "index-etag-1"),
            (GetUserTenantsQuery.QueryType, TenantQueryHandlerBase.TenantIndexProjectionKey, "index-etag-1"),
            (GetTenantQuery.QueryType, TenantQueryHandlerBase.TenantProjectionKeyPrefix + "tenant.alpha", "tenant-etag-1"),
            (GetTenantUsersQuery.QueryType, TenantQueryHandlerBase.TenantProjectionKeyPrefix + "tenant.alpha", "tenant-etag-1"),
            (GetTenantAuditQuery.QueryType, TenantQueryHandlerBase.TenantAuditProjectionKeyPrefix + "tenant.alpha", "audit-etag-1"),
            (GetGlobalAdministratorsQuery.QueryType, TenantQueryHandlerBase.GlobalAdminProjectionKey, "admin-etag-1"),
        ];

        int?[] projectedAges = [5, 40, null];
        foreach ((string queryType, string primaryKey, string eTag) in routes)
        {
            foreach (int? projectedAge in projectedAges)
            {
                yield return [queryType, primaryKey, eTag, projectedAge];
            }
        }
    }

    [Theory]
    [MemberData(nameof(HandlerFreshnessCases))]
    public async Task Query_handlers_ignore_primary_read_model_timestamp_and_sequence_authorityAsync(
        string queryType,
        string expectedPrimaryKey,
        string expectedETag,
        int? projectedAgeMinutes)
    {
        DateTimeOffset? primaryProjectedAt = projectedAgeMinutes.HasValue
            ? Now - TimeSpan.FromMinutes(projectedAgeMinutes.Value)
            : null;
        IReadModelStore store = Substitute.For<IReadModelStore>();
        TenantIndexReadModel index = SetupTenantIndex(
            store,
            expectedPrimaryKey == TenantQueryHandlerBase.TenantIndexProjectionKey ? expectedETag : "index-etag-2",
            expectedPrimaryKey == TenantQueryHandlerBase.TenantIndexProjectionKey ? primaryProjectedAt : Now);
        TenantReadModel tenant = SetupTenant(
            store,
            expectedPrimaryKey == TenantQueryHandlerBase.TenantProjectionKeyPrefix + "tenant.alpha" ? expectedETag : "tenant-etag-2",
            expectedPrimaryKey == TenantQueryHandlerBase.TenantProjectionKeyPrefix + "tenant.alpha" ? primaryProjectedAt : Now);
        TenantAuditReadModel audit = SetupTenantAudit(
            store,
            expectedPrimaryKey == TenantQueryHandlerBase.TenantAuditProjectionKeyPrefix + "tenant.alpha" ? expectedETag : "audit-etag-2",
            expectedPrimaryKey == TenantQueryHandlerBase.TenantAuditProjectionKeyPrefix + "tenant.alpha" ? primaryProjectedAt : Now);
        GlobalAdministratorReadModel administrators = SetupGlobalAdministrators(
            store,
            expectedPrimaryKey == TenantQueryHandlerBase.GlobalAdminProjectionKey ? expectedETag : "admin-etag-2",
            expectedPrimaryKey == TenantQueryHandlerBase.GlobalAdminProjectionKey ? primaryProjectedAt : Now,
            "admin-user");

        AssertPrimaryReadModelInputsExist(
            expectedPrimaryKey,
            primaryProjectedAt,
            index,
            tenant,
            audit,
            administrators);

        TenantQueryResult result = (await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            CreateEnvelope(queryType),
            freshnessOptions: Thresholds,
            timeProvider: new FixedTimeProvider(Now))).ShouldBeOfType<TenantQueryResult>();

        await AssertPrimaryReadModelWasReadAsync(store, expectedPrimaryKey);
        AssertValidatorOnly(result, expectedETag);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"\"")]
    [InlineData("  \" \"  ")]
    public async Task Query_handler_omits_metadata_for_degenerate_etagAsync(string? eTag)
    {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupTenant(store, eTag, Now - TimeSpan.FromMinutes(40));
        SetupGlobalAdministrators(store, "admin-etag", Now, "admin-user");

        TenantQueryResult result = (await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            CreateEnvelope(GetTenantQuery.QueryType),
            freshnessOptions: Thresholds,
            timeProvider: new FixedTimeProvider(Now))).ShouldBeOfType<TenantQueryResult>();

        result.Metadata.ShouldBeNull();
    }

    private static void AssertValidatorOnly(TenantQueryResult result, string expectedETag)
    {
        result.Success.ShouldBeTrue();
        QueryResponseMetadata metadata = result.Metadata.ShouldNotBeNull();
        metadata.ETag.ShouldBe(expectedETag);
        metadata.IsNotModified.ShouldBe(false);
        metadata.ProjectionVersion.ShouldBeNull();
        metadata.IsStale.ShouldBeNull();
        metadata.IsDegraded.ShouldBeNull();
        metadata.ServedAt.ShouldBeNull();
        metadata.Provenance.ShouldBe(QueryResponseProvenance.Unknown);
        metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
    }

    private static void AssertPrimaryReadModelInputsExist(
        string expectedPrimaryKey,
        DateTimeOffset? primaryProjectedAt,
        TenantIndexReadModel index,
        TenantReadModel tenant,
        TenantAuditReadModel audit,
        GlobalAdministratorReadModel administrators)
    {
        IReadModelFreshness freshness;
        if (expectedPrimaryKey == TenantQueryHandlerBase.TenantIndexProjectionKey)
        {
            freshness = index;
        }
        else if (expectedPrimaryKey == TenantQueryHandlerBase.TenantProjectionKeyPrefix + "tenant.alpha")
        {
            freshness = tenant;
        }
        else if (expectedPrimaryKey == TenantQueryHandlerBase.TenantAuditProjectionKeyPrefix + "tenant.alpha")
        {
            freshness = audit;
        }
        else if (expectedPrimaryKey == TenantQueryHandlerBase.GlobalAdminProjectionKey)
        {
            freshness = administrators;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(expectedPrimaryKey), expectedPrimaryKey, "Unsupported primary key.");
        }

        freshness.ProjectedAt.ShouldBe(primaryProjectedAt);
        freshness.ProjectionVersion.ShouldBe(GenuineSequenceVersion);
    }

    private static async Task AssertPrimaryReadModelWasReadAsync(IReadModelStore store, string expectedPrimaryKey)
    {
        if (expectedPrimaryKey == TenantQueryHandlerBase.TenantIndexProjectionKey)
        {
            _ = await store.Received().GetAsync<TenantIndexReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                expectedPrimaryKey,
                Arg.Any<CancellationToken>());
            return;
        }

        if (expectedPrimaryKey == TenantQueryHandlerBase.TenantProjectionKeyPrefix + "tenant.alpha")
        {
            _ = await store.Received().GetAsync<TenantReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                expectedPrimaryKey,
                Arg.Any<CancellationToken>());
            return;
        }

        if (expectedPrimaryKey == TenantQueryHandlerBase.TenantAuditProjectionKeyPrefix + "tenant.alpha")
        {
            _ = await store.Received().GetAsync<TenantAuditReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                expectedPrimaryKey,
                Arg.Any<CancellationToken>());
            return;
        }

        if (expectedPrimaryKey == TenantQueryHandlerBase.GlobalAdminProjectionKey)
        {
            _ = await store.Received().GetAsync<GlobalAdministratorReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                expectedPrimaryKey,
                Arg.Any<CancellationToken>());
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(expectedPrimaryKey), expectedPrimaryKey, "Unsupported primary key.");
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

    private static GlobalAdministratorReadModel SetupGlobalAdministrators(
        IReadModelStore store,
        string eTag,
        DateTimeOffset? projectedAt,
        params string[] administratorIds)
    {
        var model = new GlobalAdministratorReadModel
        {
            Administrators = administratorIds.ToHashSet(StringComparer.Ordinal),
            ProjectedAt = projectedAt,
            ProjectionVersion = GenuineSequenceVersion,
        };

        _ = store.GetAsync<GlobalAdministratorReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.GlobalAdminProjectionKey,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<GlobalAdministratorReadModel>(model, eTag)));
        return model;
    }

    private static TenantIndexReadModel SetupTenantIndex(
        IReadModelStore store,
        string eTag,
        DateTimeOffset? projectedAt)
    {
        var model = new TenantIndexReadModel
        {
            ProjectedAt = projectedAt,
            ProjectionVersion = GenuineSequenceVersion,
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
        return model;
    }

    private static TenantReadModel SetupTenant(
        IReadModelStore store,
        string? eTag,
        DateTimeOffset? projectedAt)
    {
        var model = new TenantReadModel
        {
            TenantId = "tenant.alpha",
            Name = "Tenant Alpha",
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.Parse("2026-06-07T08:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
            ProjectedAt = projectedAt,
            ProjectionVersion = GenuineSequenceVersion,
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
        return model;
    }

    private static TenantAuditReadModel SetupTenantAudit(
        IReadModelStore store,
        string eTag,
        DateTimeOffset? projectedAt)
    {
        var model = new TenantAuditReadModel
        {
            ProjectedAt = projectedAt,
            ProjectionVersion = GenuineSequenceVersion,
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
        return model;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
