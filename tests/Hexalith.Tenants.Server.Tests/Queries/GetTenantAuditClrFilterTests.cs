using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Server.Tests.Support;

using Microsoft.AspNetCore.DataProtection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

/// <summary>
/// Verifies that the audit query honors the CLR-named <c>From</c>, <c>To</c> and <c>Category</c> filters sent by
/// generic gateway callers with the same validation as the camelCase REST names, and that the two spellings agree.
/// </summary>
public sealed class GetTenantAuditClrFilterTests
{
    /// <summary>CLR-named filters keep only the matching entries.</summary>
    [Theory]
    [InlineData("{\"From\":\"2026-05-14T10:00:00Z\",\"To\":\"2026-05-14T11:00:00Z\",\"Category\":\"Administrative\"}", "from,to")]
    [InlineData("{\"From\":\"2026-05-14T12:00:00+02:00\",\"To\":\"2026-05-14T11:00:00Z\"}", "from,access,to")]
    [InlineData("{\"From\":\"2026-05-14T10:00:00Z\"}", "from,access,to,after")]
    [InlineData("{\"To\":\"2026-05-14T11:00:00Z\"}", "before,from,access,to")]
    [InlineData("{\"Category\":\"Access\"}", "access")]
    [InlineData("{\"Category\":\"Administrative\"}", "before,from,to,after")]
    [InlineData("{\"From\":null,\"To\":null,\"Category\":null}", "before,from,access,to,after")]
    public async Task ClrFiltersReturnOnlyMatchingEntriesAsync(string payload, string expectedEventIds)
    {
        ArgumentNullException.ThrowIfNull(expectedEventIds);

        QueryResult result = await ExecuteAsync(CreateStore(), CreateCodec(), payload);

        ReadEventIds(result).ShouldBe(expectedEventIds.Split(','));
    }

    /// <summary>Both spellings may be combined when they do not contradict each other.</summary>
    [Theory]
    [InlineData("{\"from\":\"2026-05-14T10:00:00Z\",\"From\":\"2026-05-14T10:00:00Z\"}", "from,access,to,after")]
    [InlineData("{\"from\":\"2026-05-14T12:00:00+02:00\",\"From\":\"2026-05-14T10:00:00Z\"}", "from,access,to,after")]
    [InlineData("{\"to\":\"2026-05-14T11:00:00Z\",\"To\":\"2026-05-14T11:00:00Z\"}", "before,from,access,to")]
    [InlineData("{\"category\":\"access\",\"Category\":\"Access\"}", "access")]
    [InlineData("{\"from\":\"2026-05-14T10:00:00Z\",\"To\":\"2026-05-14T11:00:00Z\",\"category\":null,\"Category\":\"Administrative\"}", "from,to")]
    [InlineData("{\"from\":null,\"From\":\"2026-05-14T10:00:00Z\",\"category\":\"\",\"Category\":\"Access\"}", "access")]
    public async Task AgreeingSpellingsAreHonoredAsync(string payload, string expectedEventIds)
    {
        ArgumentNullException.ThrowIfNull(expectedEventIds);

        QueryResult result = await ExecuteAsync(CreateStore(), CreateCodec(), payload);

        ReadEventIds(result).ShouldBe(expectedEventIds.Split(','));
    }

    /// <summary>Contradicting spellings of one filter are an invalid request that returns no rows and reads no audit.</summary>
    [Theory]
    [InlineData("{\"from\":\"2026-05-14T10:00:00Z\",\"From\":\"2026-05-14T10:15:00Z\"}")]
    [InlineData("{\"to\":\"2026-05-14T11:00:00Z\",\"To\":\"2026-05-14T10:15:00Z\"}")]
    [InlineData("{\"category\":\"access\",\"Category\":\"Administrative\"}")]
    [InlineData("{\"category\":\"0\",\"Category\":\"Administrative\"}")]
    public async Task ContradictingSpellingsAreInvalidAsync(string payload)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();

        QueryResult result = await ExecuteAsync(store, codec, payload);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>CLR-named filters get the camelCase validation, including its failure messages.</summary>
    [Theory]
    [InlineData("{\"From\":\"not-a-date\"}", "invalid-envelope")]
    [InlineData("{\"From\":42}", "invalid-envelope")]
    [InlineData("{\"To\":true}", "invalid-envelope")]
    [InlineData("{\"To\":\"\\ud800\"}", "invalid-envelope")]
    [InlineData("{\"Category\":42}", "invalid-envelope")]
    [InlineData("{\"Category\":\"\\ud800\"}", "invalid-envelope")]
    [InlineData("{\"Category\":\"bogus\"}", "invalid-envelope: Invalid audit category: bogus")]
    [InlineData("{\"From\":\"2026-05-14T11:00:00Z\",\"To\":\"2026-05-14T10:00:00Z\"}", "invalid-envelope: Invalid audit query payload: 'from' must not be after 'to'.")]
    [InlineData("{\"from\":\"2026-05-14T11:00:00Z\",\"To\":\"2026-05-14T10:00:00Z\"}", "invalid-envelope: Invalid audit query payload: 'from' must not be after 'to'.")]
    public async Task ClrFiltersUseTheCamelCaseValidationAsync(string payload, string expectedError)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();

        QueryResult result = await ExecuteAsync(store, codec, payload);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(expectedError);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>Authorization still precedes CLR filter validation.</summary>
    [Theory]
    [InlineData("{\"From\":\"not-a-date\"}")]
    [InlineData("{\"from\":\"2026-05-14T10:00:00Z\",\"From\":\"2026-05-14T10:15:00Z\"}")]
    [InlineData("{\"Category\":\"Access\"}")]
    public async Task NonAdministratorIsForbiddenBeforeClrFilterValidationAsync(string payload)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();

        QueryResult result = await ExecuteAsync(store, codec, payload, "non-admin");

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>
    /// CLR filters bind the envelope-paged continuation: the filtered sequence pages with a non-default size, the
    /// next cursor continues it, and the same cursor under other filters is rejected.
    /// </summary>
    [Fact]
    public async Task ClrFiltersAndEnvelopePagingContinueTheFilteredSequenceAsync()
    {
        const string Filters = "{\"From\":\"2026-05-14T10:00:00Z\",\"To\":\"2026-05-14T11:00:00Z\",\"Category\":\"Administrative\"}";
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = CreateCodec();

        QueryResult first = await ExecuteAsync(store, codec, Filters, paging: new QueryPagingOptions(PageSize: 1));
        ReadEventIds(first).ShouldBe(["from"]);
        QueryPagingMetadata firstPaging = first.Metadata.ShouldNotBeNull().Paging.ShouldNotBeNull();
        firstPaging.PageSize.ShouldBe(1);
        firstPaging.HasMore.ShouldBe(true);
        string cursor = firstPaging.NextCursor.ShouldNotBeNull();

        QueryResult next = await ExecuteAsync(store, codec, Filters, paging: new QueryPagingOptions(PageSize: 1, Cursor: cursor));
        ReadEventIds(next).ShouldBe(["to"]);
        QueryPagingMetadata nextPaging = next.Metadata.ShouldNotBeNull().Paging.ShouldNotBeNull();
        nextPaging.HasMore.ShouldBe(false);
        nextPaging.NextCursor.ShouldBeNull();

        QueryResult camelCaseNext = await ExecuteAsync(
            store,
            codec,
            "{\"from\":\"2026-05-14T10:00:00Z\",\"to\":\"2026-05-14T11:00:00Z\",\"category\":\"administrative\"}",
            paging: new QueryPagingOptions(PageSize: 1, Cursor: cursor));
        ReadEventIds(camelCaseNext).ShouldBe(["to"]);

        QueryResult otherFilters = await ExecuteAsync(
            store,
            codec,
            "{\"From\":\"2026-05-14T10:00:00Z\",\"Category\":\"Administrative\"}",
            paging: new QueryPagingOptions(PageSize: 1, Cursor: cursor));
        otherFilters.Success.ShouldBeFalse();
        otherFilters.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidCursor);
        otherFilters.PayloadBytes.ShouldBeNull();
    }

    /// <summary>The camelCase REST filters keep working unchanged next to the new CLR spellings.</summary>
    [Fact]
    public async Task CamelCaseRestFiltersAreUnchangedAsync()
    {
        QueryResult result = await ExecuteAsync(
            CreateStore(),
            CreateCodec(),
            "{\"from\":\"2026-05-14T10:00:00Z\",\"to\":\"2026-05-14T11:00:00Z\",\"category\":\"administrative\",\"stream\":null,\"cursor\":null,\"pageSize\":0}");

        ReadEventIds(result).ShouldBe(["from", "to"]);
        result.Metadata?.Paging.ShouldBeNull();
    }

    private static Task<QueryResult> ExecuteAsync(
        IReadModelStore store,
        IQueryCursorCodec codec,
        string payload,
        string userId = "admin-1",
        QueryPagingOptions? paging = null)
        => TenantQueryTestHarness.ExecuteAsync(store, codec, new QueryEnvelope(
            "system",
            GetTenantAuditQuery.Domain,
            "tenant-1",
            GetTenantAuditQuery.QueryType,
            Encoding.UTF8.GetBytes(payload),
            "audit-clr-filters",
            userId,
            "tenant-1") with { Paging = paging });

    private static async Task AssertOnlyAuthorityWasReadAsync(IReadModelStore store)
    {
        _ = await store.Received(1).GetAsync<GlobalAdministratorReadModel>(
            TenantQueryHandlerBase.StateStoreName,
            TenantQueryHandlerBase.GlobalAdminProjectionKey,
            Arg.Any<CancellationToken>());
        store.ReceivedCalls().Count().ShouldBe(1);
    }

    private static string[] ReadEventIds(QueryResult result)
    {
        result.Success.ShouldBeTrue(result.ErrorMessage);
        result.ErrorMessage.ShouldBeNull();
        using JsonDocument document = JsonDocument.Parse(result.PayloadBytes.ShouldNotBeNull());
        return document.RootElement.GetProperty("items").EnumerateArray()
            .Select(entry => entry.GetProperty("eventId").GetString()!)
            .ToArray();
    }

    private static IReadModelStore CreateStore()
    {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        _ = store.GetAsync<GlobalAdministratorReadModel>(
            TenantQueryHandlerBase.StateStoreName,
            TenantQueryHandlerBase.GlobalAdminProjectionKey,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<GlobalAdministratorReadModel>(
                new GlobalAdministratorReadModel { Administrators = ["admin-1"] }, "authority-etag")));
        DateTimeOffset from = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        var model = new TenantAuditReadModel
        {
            Entries =
            [
                CreateEntry("before", from.AddTicks(-1)),
                CreateEntry("from", from),
                CreateEntry("access", from.AddMinutes(15), AuditEventCategory.Access),
                CreateEntry("foreign", from.AddMinutes(30)) with { TenantId = "other-tenant" },
                CreateEntry("to", from.AddHours(1)),
                CreateEntry("after", from.AddHours(1).AddTicks(1)),
            ],
        };
        _ = store.GetAsync<TenantAuditReadModel>(
            TenantQueryHandlerBase.StateStoreName,
            TenantQueryHandlerBase.TenantAuditProjectionKeyPrefix + "tenant-1",
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<TenantAuditReadModel>(model, "audit-etag")));
        return store;
    }

    private static TenantAuditEntry CreateEntry(
        string eventId,
        DateTimeOffset timestamp,
        AuditEventCategory category = AuditEventCategory.Administrative)
        => new(eventId, "TenantUpdated", category, "actor-1", timestamp, "tenant-1", new Dictionary<string, string>());

    private static IQueryCursorCodec CreateCodec()
        => new QueryCursorCodec(new EphemeralDataProtectionProvider(), "Hexalith.Tenants.QueryCursor.v1");
}
