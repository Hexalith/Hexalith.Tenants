using System.Text;
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

/// <summary>
/// Verifies that present envelope paging alone governs every paged tenant query, that offset paging is refused,
/// that envelope pages report their paging metadata, and that the legacy REST payload paging stays unchanged.
/// </summary>
public sealed class EnvelopePagingQueryHandlerTests
{
    private const string Caller = "item-000";
    private const string AuditTenant = "tenant-1";

    /// <summary>Gets every paged query type served by the tenant query handlers.</summary>
    public static TheoryData<string> PagedQueryTypes => new(
        ListTenantsQuery.QueryType,
        GetTenantUsersQuery.QueryType,
        GetUserTenantsQuery.QueryType,
        GetTenantAuditQuery.QueryType,
        GetGlobalAdministratorsQuery.QueryType);

    /// <summary>An envelope page size serves that many rows and reports page size, next cursor and more rows.</summary>
    [Theory]
    [MemberData(nameof(PagedQueryTypes))]
    public async Task EnvelopePageSizeServesFirstPageWithPagingMetadataAsync(string queryType)
    {
        QueryResult result = await ExecuteAsync(queryType, "{}", new QueryPagingOptions(PageSize: 2));

        JsonElement page = ReadPage(result);
        ReadIds(page, queryType).ShouldBe([Id(queryType, 0), Id(queryType, 1)]);
        page.GetProperty("hasMore").GetBoolean().ShouldBeTrue();
        QueryResponseMetadata metadata = result.Metadata.ShouldNotBeNull();
        metadata.ETag.ShouldNotBeNullOrWhiteSpace("Paging metadata is merged into, not substituted for, the projection metadata.");
        metadata.Provenance.ShouldBe(QueryResponseProvenance.ProjectionBacked);
        QueryPagingMetadata paging = metadata.Paging.ShouldNotBeNull();
        paging.PageSize.ShouldBe(2);
        paging.HasMore.ShouldBe(true);
        paging.NextCursor.ShouldNotBeNullOrWhiteSpace();
        paging.NextCursor.ShouldBe(page.GetProperty("cursor").GetString());
        paging.Offset.ShouldBeNull();
        paging.TotalCount.ShouldBeNull();
    }

    /// <summary>The returned next cursor, sent back as envelope paging, serves the last row and ends the sequence.</summary>
    [Theory]
    [MemberData(nameof(PagedQueryTypes))]
    public async Task EnvelopeCursorContinuesToTheLastPageAsync(string queryType)
    {
        IQueryCursorCodec codec = CreateCodec();
        QueryResult first = await ExecuteAsync(queryType, "{}", new QueryPagingOptions(PageSize: 2), codec);
        string nextCursor = first.Metadata.ShouldNotBeNull().Paging.ShouldNotBeNull().NextCursor.ShouldNotBeNull();

        QueryResult next = await ExecuteAsync(queryType, "{}", new QueryPagingOptions(Cursor: nextCursor), codec);

        JsonElement page = ReadPage(next);
        ReadIds(page, queryType).ShouldBe([Id(queryType, 2)]);
        page.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        QueryPagingMetadata paging = next.Metadata.ShouldNotBeNull().Paging.ShouldNotBeNull();
        paging.PageSize.ShouldBe(DefaultPageSize(queryType));
        paging.HasMore.ShouldBe(false);
        paging.NextCursor.ShouldBeNull();
    }

    /// <summary>A bad envelope cursor keeps the existing invalid-cursor failure.</summary>
    [Theory]
    [MemberData(nameof(PagedQueryTypes))]
    public async Task EnvelopeInvalidCursorReturnsInvalidCursorAsync(string queryType)
    {
        IQueryCursorCodec codec = CreateCodec();
        string otherScope = codec.Encode(queryType, "other-scope", Id(queryType, 0));

        foreach (string cursor in new[] { "not-a-protected-cursor", otherScope })
        {
            QueryResult result = await ExecuteAsync(queryType, "{}", new QueryPagingOptions(PageSize: 2, Cursor: cursor), codec);

            result.Success.ShouldBeFalse();
            result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidCursor);
            result.PayloadBytes.ShouldBeNull();
            result.Metadata.ShouldBeNull();
        }
    }

    /// <summary>Present envelope paging ignores every legacy payload paging field, including a malformed cursor.</summary>
    [Theory]
    [MemberData(nameof(PagedQueryTypes))]
    public async Task EnvelopePagingWinsOverLegacyPayloadPagingAsync(string queryType)
    {
        IQueryCursorCodec codec = CreateCodec();
        string payloadCursor = JsonSerializer.Serialize(codec.Encode(queryType, Scope(queryType), Id(queryType, 0)));

        foreach (string payload in new[]
        {
            "{\"pageSize\":1}",
            $$"""{"pageSize":1,"cursor":{{payloadCursor}}}""",
            "{\"pageSize\":1,\"cursor\":\"\\ud800\"}",
        })
        {
            QueryResult result = await ExecuteAsync(queryType, payload, new QueryPagingOptions(PageSize: 2), codec);

            ReadIds(ReadPage(result), queryType).ShouldBe([Id(queryType, 0), Id(queryType, 1)]);
            result.Metadata.ShouldNotBeNull().Paging.ShouldNotBeNull().PageSize.ShouldBe(2);
        }
    }

    /// <summary>CLR-named payload paging members never govern paging; without envelope paging the default page applies.</summary>
    [Theory]
    [MemberData(nameof(PagedQueryTypes))]
    public async Task ClrPayloadPagingDoesNotGovernPagingAsync(string queryType)
    {
        QueryResult result = await ExecuteAsync(queryType, "{\"PageSize\":1,\"Cursor\":\"not-a-protected-cursor\"}", paging: null);

        JsonElement page = ReadPage(result);
        ReadIds(page, queryType).ShouldBe([Id(queryType, 0), Id(queryType, 1), Id(queryType, 2)]);
        page.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        result.Metadata?.Paging.ShouldBeNull();
    }

    /// <summary>Offset paging cannot be honored by keyset pages and is refused as an invalid page.</summary>
    [Theory]
    [MemberData(nameof(PagedQueryTypes))]
    public async Task EnvelopeOffsetIsRejectedAsInvalidPageAsync(string queryType)
    {
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();
        foreach (QueryPagingOptions paging in new[]
        {
            new QueryPagingOptions(Offset: 0),
            new QueryPagingOptions(PageSize: 2, Offset: 1),
            new QueryPagingOptions(Offset: 0, Cursor: "any-cursor"),
        })
        {
            QueryResult result = await ExecuteAsync(queryType, "{}", paging, codec);

            result.Success.ShouldBeFalse();
            result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidPage);
            result.PayloadBytes.ShouldBeNull();
            result.Metadata.ShouldBeNull();
        }

        codec.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Envelope page sizes keep the existing per-family bounds, and an omitted size uses the family default.</summary>
    [Theory]
    [MemberData(nameof(PagedQueryTypes))]
    public async Task EnvelopePageSizeKeepsExistingBoundsAsync(string queryType)
    {
        int maximum = queryType == GetTenantAuditQuery.QueryType
            ? TenantQueryPaginationPolicy.AuditMaximumPageSize
            : TenantQueryPaginationPolicy.StandardMaximumPageSize;
        foreach ((int? requested, int expected) in new (int?, int)[]
        {
            (null, DefaultPageSize(queryType)),
            (0, DefaultPageSize(queryType)),
            (-1, DefaultPageSize(queryType)),
            (1, 1),
            (200, Math.Min(200, maximum)),
            (int.MaxValue, maximum),
        })
        {
            QueryResult result = await ExecuteAsync(queryType, "{}", new QueryPagingOptions(PageSize: requested));

            ReadIds(ReadPage(result), queryType).Length.ShouldBe(Math.Min(3, expected));
            result.Metadata.ShouldNotBeNull().Paging.ShouldNotBeNull().PageSize.ShouldBe(expected);
        }
    }

    /// <summary>The legacy REST camelCase payload paging still pages and continues without paging metadata.</summary>
    [Theory]
    [MemberData(nameof(PagedQueryTypes))]
    public async Task LegacyRestPayloadPagingIsUnchangedAsync(string queryType)
    {
        IQueryCursorCodec codec = CreateCodec();
        QueryResult first = await ExecuteAsync(queryType, "{\"pageSize\":2}", paging: null, codec);

        JsonElement firstPage = ReadPage(first);
        ReadIds(firstPage, queryType).ShouldBe([Id(queryType, 0), Id(queryType, 1)]);
        firstPage.GetProperty("hasMore").GetBoolean().ShouldBeTrue();
        first.Metadata?.Paging.ShouldBeNull();
        string cursor = JsonSerializer.Serialize(firstPage.GetProperty("cursor").GetString().ShouldNotBeNull());

        QueryResult next = await ExecuteAsync(queryType, $$"""{"pageSize":2,"cursor":{{cursor}}}""", paging: null, codec);

        ReadIds(ReadPage(next), queryType).ShouldBe([Id(queryType, 2)]);
        next.Metadata?.Paging.ShouldBeNull();
    }

    /// <summary>
    /// A default-size read without any paging argument returns its next-page cursor only in the result document;
    /// sending that cursor back as envelope paging continues the same sequence.
    /// </summary>
    [Theory]
    [MemberData(nameof(PagedQueryTypes))]
    public async Task DefaultPageDocumentCursorContinuesAsEnvelopeCursorAsync(string queryType)
    {
        int defaultPageSize = DefaultPageSize(queryType);
        IReadModelStore store = CreateStore(defaultPageSize + 1);
        IQueryCursorCodec codec = CreateCodec();
        QueryResult first = await TenantQueryTestHarness.ExecuteAsync(store, codec, CreateEnvelope(queryType, "{}"));

        JsonElement firstPage = ReadPage(first);
        ReadIds(firstPage, queryType).Length.ShouldBe(defaultPageSize);
        firstPage.GetProperty("hasMore").GetBoolean().ShouldBeTrue();
        first.Metadata?.Paging.ShouldBeNull();
        string cursor = firstPage.GetProperty("cursor").GetString().ShouldNotBeNull();

        QueryResult next = await TenantQueryTestHarness.ExecuteAsync(
            store,
            codec,
            CreateEnvelope(queryType, "{}") with { Paging = new QueryPagingOptions(Cursor: cursor) });

        JsonElement nextPage = ReadPage(next);
        ReadIds(nextPage, queryType).ShouldBe([Id(queryType, defaultPageSize)]);
        nextPage.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        QueryPagingMetadata paging = next.Metadata.ShouldNotBeNull().Paging.ShouldNotBeNull();
        paging.HasMore.ShouldBe(false);
        paging.NextCursor.ShouldBeNull();
    }

    /// <summary>A missing projection still answers an envelope-paged read with an empty, terminal page.</summary>
    [Theory]
    [InlineData("list-tenants")]
    [InlineData("get-user-tenants")]
    [InlineData("get-tenant-audit")]
    public async Task EnvelopePagedEmptyReadReportsTerminalPagingAsync(string queryType)
    {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        if (queryType == GetTenantAuditQuery.QueryType)
        {
            _ = store.GetAsync<GlobalAdministratorReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.GlobalAdminProjectionKey,
                Arg.Any<CancellationToken>())
                .Returns(new ReadModelEntry<GlobalAdministratorReadModel>(
                    new GlobalAdministratorReadModel { Administrators = [Caller] }, "admin-etag"));
        }

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCodec(),
            CreateEnvelope(queryType, "{}") with { Paging = new QueryPagingOptions(PageSize: 2) });

        JsonElement page = ReadPage(result);
        page.GetProperty("items").GetArrayLength().ShouldBe(0);
        QueryPagingMetadata paging = result.Metadata.ShouldNotBeNull().Paging.ShouldNotBeNull();
        paging.PageSize.ShouldBe(2);
        paging.HasMore.ShouldBe(false);
        paging.NextCursor.ShouldBeNull();
    }

    private static int DefaultPageSize(string queryType)
        => queryType == GetTenantAuditQuery.QueryType
            ? TenantQueryPaginationPolicy.AuditDefaultPageSize
            : TenantQueryPaginationPolicy.StandardDefaultPageSize;

    private static string Id(string queryType, int index)
        => queryType == GetTenantAuditQuery.QueryType ? $"event-{index}" : $"item-{index:D3}";

    private static Task<QueryResult> ExecuteAsync(
        string queryType,
        string payload,
        QueryPagingOptions? paging,
        IQueryCursorCodec? codec = null)
        => TenantQueryTestHarness.ExecuteAsync(
            CreateStore(),
            codec ?? CreateCodec(),
            CreateEnvelope(queryType, payload) with { Paging = paging });

    private static QueryEnvelope CreateEnvelope(string queryType, string payload)
        => new(
            TenantIdentity.DefaultTenantId,
            queryType == GetGlobalAdministratorsQuery.QueryType ? GetGlobalAdministratorsQuery.Domain : ListTenantsQuery.Domain,
            queryType switch
            {
                "get-global-administrators" => TenantIdentity.GlobalAdministratorsAggregateId,
                "get-tenant-users" => "tenant.alpha",
                "get-tenant-audit" => AuditTenant,
                _ => "index",
            },
            queryType,
            Encoding.UTF8.GetBytes(payload),
            "correlation-1",
            Caller);

    private static string Scope(string queryType)
        => queryType switch
        {
            "list-tenants" => TenantQueryCursorScopes.ListTenants(Caller),
            "get-tenant-users" => TenantQueryCursorScopes.GetTenantUsers("tenant.alpha"),
            "get-user-tenants" => TenantQueryCursorScopes.GetUserTenants(Caller, Caller),
            "get-global-administrators" => TenantQueryCursorScopes.GetGlobalAdministrators(Caller),
            "get-tenant-audit" => TenantQueryCursorScopes.GetTenantAudit(Caller, AuditTenant, null, null, null),
            _ => throw new ArgumentOutOfRangeException(nameof(queryType)),
        };

    private static IQueryCursorCodec CreateCodec()
        => new QueryCursorCodec(new EphemeralDataProtectionProvider(), "Hexalith.Tenants.QueryCursor.v1");

    private static JsonElement ReadPage(QueryResult result)
    {
        result.Success.ShouldBeTrue(result.ErrorMessage);
        using var document = JsonDocument.Parse(result.PayloadBytes.ShouldNotBeNull());
        return document.RootElement.Clone();
    }

    private static string[] ReadIds(JsonElement page, string queryType)
    {
        string property = queryType switch
        {
            "get-tenant-users" or "get-global-administrators" => "userId",
            "get-tenant-audit" => "eventId",
            _ => "tenantId",
        };
        return page.GetProperty("items").EnumerateArray().Select(item => item.GetProperty(property).GetString()!).ToArray();
    }

    private static IReadModelStore CreateStore(int rows = 3)
    {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        var index = new TenantIndexReadModel();
        var tenant = new TenantReadModel { TenantId = "tenant.alpha", Name = "Tenant Alpha", Status = TenantStatus.Active };
        var admins = new GlobalAdministratorReadModel();
        Dictionary<string, TenantRole> memberships = new(StringComparer.Ordinal);
        index.UserTenants[Caller] = memberships;
        var audit = new TenantAuditReadModel();
        DateTimeOffset start = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        for (int i = 0; i < rows; i++)
        {
            string id = $"item-{i:D3}";
            index.Tenants[id] = new TenantIndexEntry(id, TenantStatus.Active);
            memberships[id] = TenantRole.TenantReader;
            tenant.Members[id] = TenantRole.TenantReader;
            admins.Administrators.Add(id);
            audit.Entries.Add(new TenantAuditEntry(
                $"event-{i}",
                "TenantUpdated",
                AuditEventCategory.Administrative,
                "actor-1",
                start.AddMinutes(i),
                AuditTenant,
                new Dictionary<string, string>()));
        }

        store.GetAsync<TenantIndexReadModel>(TenantQueryHandlerBase.StateStoreName, TenantQueryHandlerBase.TenantIndexProjectionKey, Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<TenantIndexReadModel>(index, "index-etag"));
        store.GetAsync<TenantReadModel>(TenantQueryHandlerBase.StateStoreName, "projection:tenants:tenant.alpha", Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<TenantReadModel>(tenant, "tenant-etag"));
        store.GetAsync<GlobalAdministratorReadModel>(TenantQueryHandlerBase.StateStoreName, TenantQueryHandlerBase.GlobalAdminProjectionKey, Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<GlobalAdministratorReadModel>(admins, "admin-etag"));
        store.GetAsync<TenantAuditReadModel>(TenantQueryHandlerBase.StateStoreName, TenantQueryHandlerBase.TenantAuditProjectionKeyPrefix + AuditTenant, Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<TenantAuditReadModel>(audit, "audit-etag"));
        return store;
    }
}
