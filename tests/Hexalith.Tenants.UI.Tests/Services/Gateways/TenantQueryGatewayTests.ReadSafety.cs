using System.Net;

using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.AspNetCore.DataProtection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed partial class TenantQueryGatewayTests
{
    [Theory]
    [InlineData("decode", false)]
    [InlineData("decode", true)]
    [InlineData("encode", false)]
    [InlineData("encode", true)]
    [InlineData("index", false)]
    [InlineData("index", true)]
    [InlineData("hydration", false)]
    [InlineData("hydration", true)]
    [InlineData("forbidden-hydration", false)]
    [InlineData("forbidden-hydration", true)]
    public async Task SearchReadSafetyPreservesFatalWrappersAcrossRealGatewaySeams(string seam, bool nested)
    {
        var fatal = new OutOfMemoryException("original-fatal-evidence");
        Exception inner = nested ? new ArgumentException("nested-wrapper", fatal) : fatal;
        Exception wrapper = seam == "forbidden-hydration"
            ? new Hexalith.EventStore.Client.Gateway.EventStoreGatewayException((int)HttpStatusCode.Forbidden, "Forbidden", innerException: inner)
            : new InvalidOperationException("outer-wrapper", inner);
        StubMemoriesClient memories = ReadSafetyIndex(["a", "b"]);
        ITenantsRestQueryClient client = ReadSafetyDetails();
        ITenantSearchCursorCodec? codec = seam switch
        {
            "decode" => new ThrowingSearchCursorCodec(decodeFailure: () => wrapper),
            "encode" => new ThrowingSearchCursorCodec(encodeFailure: () => wrapper),
            _ => null,
        };
        if (seam == "index")
        {
            memories = new StubMemoriesClient();
            memories.Enqueue(wrapper);
        }
        if (seam is "hydration" or "forbidden-hydration")
        {
            client.GetTenantAsync(Arg.Any<GetTenantQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException<TenantsRestQueryResponse<TenantDetail>>(wrapper));
        }
        Exception observed = await Should.ThrowAsync<Exception>(() =>
            CreateGateway(client, memoriesClient: memories, searchCursorCodec: codec)
                .ListTenantsAsync(new(Search: "needle", PageSize: 1), null));
        observed.ShouldBeSameAs(wrapper);
        (nested ? observed.InnerException!.InnerException : observed.InnerException).ShouldBeSameAs(fatal);
        await client.DidNotReceive().ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchReadSafetyPreservesFatalSiblingHiddenByNonfatalConcurrentHydration()
    {
        var nonfatal = new NullReferenceException("nonfatal-first");
        var fatal = new OutOfMemoryException("original-fatal-sibling");
        Task<TenantsRestQueryResponse<TenantDetail>> first = Task.FromException<TenantsRestQueryResponse<TenantDetail>>(nonfatal);
        var second = new TaskCompletionSource<TenantsRestQueryResponse<TenantDetail>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var bothStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ITenantsRestQueryClient client = ReadSafetyDetails();
        int started = 0;
        client.GetTenantAsync(Arg.Any<GetTenantQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (Interlocked.Increment(ref started) == 2)
                {
                    bothStarted.SetResult();
                }
                return call.ArgAt<GetTenantQuery>(0).TenantId == "a" ? first : second.Task;
            });
        Task<TenantListSnapshot> read = CreateGateway(client, memoriesClient: ReadSafetyIndex(["a", "b"]))
            .ListTenantsAsync(new(Search: "needle", PageSize: 2), null);
        await bothStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        second.SetException(fatal);
        AggregateException observed = await Should.ThrowAsync<AggregateException>(() => read);
        observed.InnerExceptions.ShouldContain(nonfatal);
        observed.InnerExceptions.ShouldContain(fatal);
    }

    [Fact]
    public async Task SearchReadSafetyReplayConsumesBudgetAndIndexGrowthCannotOverrunIt()
    {
        var codec = new TenantSearchCursorCodec(new EphemeralDataProtectionProvider());
        string scope = TenantSearchCursorScopes.Create("operator-user", "needle", null, TenantListSortColumns.TenantId, false, 3);
        StubMemoriesClient memories = new()
        {
            SearchHandler = (request, cancellation) => Task.FromResult(SearchResult("needle",
                request.Offset == 999 ? 1002 : 1000)),
        };
        ITenantsRestQueryClient client = ReadSafetyDetails();
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(DirectResponse(new PaginatedResult<TenantSummary>([], null, false)));
        TenantListSnapshot result = await CreateGateway(client, memoriesClient: memories, searchCursorCodec: codec)
            .ListTenantsAsync(new(Search: "needle", PageSize: 3, SearchCursor: codec.Encode(scope, 0, 0)), null);
        result.IsAuthoritativeSearch.ShouldBeFalse();
        result.Notice.ShouldBe(TenantListReason.SearchUnavailable);
        memories.SearchRequests.Sum(query => query.MaxResults).ShouldBe(1000);
        memories.SearchRequests.Last().Offset.ShouldBe(999);
        memories.SearchRequests.Last().MaxResults.ShouldBe(1);
    }

    [Fact]
    public async Task SearchReadSafetyConsumedReplayPrefixStillCountsAgainstTheLoadBudget()
    {
        var codec = new TenantSearchCursorCodec(new EphemeralDataProtectionProvider());
        string scope = TenantSearchCursorScopes.Create("operator-user", "needle", null, TenantListSortColumns.TenantId, false, 3);
        string?[] raw = Enumerable.Repeat<string?>("hidden", 1002).ToArray();
        raw[0] = "a";
        raw[1] = "b";
        raw[1000] = "c";
        StubMemoriesClient memories = ReadSafetyIndex(raw);
        ITenantsRestQueryClient client = ReadSafetyDetails();
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(DirectResponse(new PaginatedResult<TenantSummary>([], null, false)));
        TenantListSnapshot result = await CreateGateway(client, memoriesClient: memories, searchCursorCodec: codec)
            .ListTenantsAsync(new(Search: "needle", PageSize: 3, SearchCursor: codec.Encode(scope, 0, 2)), null);
        result.IsAuthoritativeSearch.ShouldBeFalse();
        result.Rows.ShouldBeEmpty();
        memories.SearchRequests.Sum(query => query.MaxResults).ShouldBeLessThanOrEqualTo(1000);
        await client.DidNotReceive().GetTenantAsync(Arg.Is<GetTenantQuery>(q => q.TenantId == "a" || q.TenantId == "b" || q.TenantId == "c"),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchReadSafetyCancellationDuringBackfillPropagatesWithoutFallback()
    {
        using var cancellation = new CancellationTokenSource();
        StubMemoriesClient memories = new()
        {
            SearchHandler = (request, token) =>
            {
                if (request.Offset == 2)
                {
                    cancellation.Cancel();
                    return Task.FromCanceled<Hexalith.Memories.Contracts.V1.SearchResult>(token);
                }
                return Task.FromResult(SearchResult("needle", 4, Hit("tenant:hidden")));
            },
        };
        ITenantsRestQueryClient client = ReadSafetyDetails();
        await Should.ThrowAsync<OperationCanceledException>(() => CreateGateway(client, memoriesClient: memories)
            .ListTenantsAsync(new(Search: "needle", PageSize: 2), null, cancellation.Token));
        await client.DidNotReceive().ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchReadSafetyReplaysReturnedOrdinalsInsideFixedRawWindows()
    {
        string?[] raw = ["hidden", "a", null, "b", "c", "d", "e", "f", "hidden.tail"];
        StubMemoriesClient memories = ReadSafetyIndex(raw);
        ITenantsRestQueryClient client = ReadSafetyDetails();
        var codec = new TenantSearchCursorCodec(new EphemeralDataProtectionProvider());
        TenantQueryGateway gateway = CreateGateway(client, memoriesClient: memories, searchCursorCodec: codec);
        var request = new TenantListRequest(Search: "needle", PageSize: 3);

        TenantListSnapshot first = await gateway.ListTenantsAsync(request, null);
        first.Rows.Select(row => row.TenantId).ShouldBe(["a", "b", "c"]);
        first.HasMore.ShouldBeTrue();
        string scope = TenantSearchCursorScopes.Create("operator-user", "needle", null, TenantListSortColumns.TenantId, false, 3);
        codec.TryDecode(first.NextCursor, scope, out TenantSearchCursorPosition position).ShouldBeTrue();
        position.ShouldBe(new(3, 2));

        TenantListSnapshot second = await gateway.ListTenantsAsync(request with { SearchCursor = first.NextCursor }, null);
        second.Rows.Select(row => row.TenantId).ShouldBe(["d", "e", "f"]);
        second.HasMore.ShouldBeFalse();
        second.NextCursor.ShouldBeNull();
        memories.SearchRequests.Select(query => query.Offset).ShouldBe([0, 3, 3, 6]);
        memories.SearchRequests.ShouldAllBe(query => query.MaxResults == 3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task SearchReadSafetyEqualAuthorizedRowsHaveIdenticalRenderedPaging(int hiddenLayout)
    {
        string?[] raw = hiddenLayout switch
        {
            0 => ["a", "b", "c", "d", "e"],
            1 => ["hidden.1", "hidden.2", "a", "b", "c", "d", "e", "hidden.tail"],
            _ => [null, "malformed", "a", "hidden.1", "missing", "b", null, "c", "d", "hidden.2", "e", "hidden.tail"],
        };
        TenantQueryGateway gateway = CreateGateway(ReadSafetyDetails(), memoriesClient: ReadSafetyIndex(raw));
        string? cursor = null;
        var pages = new List<string>();
        do
        {
            TenantListSnapshot page = await gateway.ListTenantsAsync(new(Search: "needle", PageSize: 2, SearchCursor: cursor), null);
            page.IsAuthoritativeSearch.ShouldBeTrue();
            pages.Add(string.Join(',', page.Rows.Select(row => row.TenantId)) + ":" + page.HasMore);
            cursor = page.NextCursor;
            pages.Count.ShouldBeLessThan(5);
        } while (cursor is not null);
        pages.ShouldBe(["a,b:True", "c,d:True", "e:False"]);
    }

    [Theory]
    [InlineData(1, 1000, true)]
    [InlineData(3, 1000, true)]
    [InlineData(20, 1000, true)]
    [InlineData(100, 1000, true)]
    [InlineData(1, 1001, false)]
    [InlineData(3, 1001, false)]
    [InlineData(20, 1001, false)]
    [InlineData(100, 1001, false)]
    public async Task SearchReadSafetyBudgetIncludesHiddenPrefixesAndLookahead(int size, int total, bool complete)
    {
        string?[] raw = Enumerable.Repeat<string?>("hidden", total).ToArray();
        raw[0] = "a";
        StubMemoriesClient memories = ReadSafetyIndex(raw);
        ITenantsRestQueryClient client = ReadSafetyDetails();
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(DirectResponse(new PaginatedResult<TenantSummary>([], null, false)));
        var logger = new CapturingLogger();
        TenantListSnapshot page = await CreateGateway(client, memoriesClient: memories, logger: logger)
            .ListTenantsAsync(new(Search: "needle", PageSize: size), null);

        memories.SearchRequests.Sum(query => Math.Min(size, total - query.Offset)).ShouldBeLessThanOrEqualTo(1000);
        page.IsAuthoritativeSearch.ShouldBe(complete);
        page.HasMore.ShouldBeFalse();
        if (complete)
        {
            page.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("a");
            logger.Events.ShouldNotContain(TenantQueryGateway.SearchDegradedToOrdinaryListEvent);
        }
        else
        {
            page.Rows.ShouldBeEmpty();
            page.Notice.ShouldBe(TenantListReason.SearchUnavailable);
            logger.Messages.Where(message => message.Contains(TenantQueryGateway.SearchScanBudgetExceededReasonCode, StringComparison.Ordinal)).ShouldHaveSingleItem();
        }
    }

    [Fact]
    public async Task SearchReadSafetyHydrationGapDiscardsAnOtherwiseUsablePage()
    {
        ITenantsRestQueryClient client = ReadSafetyDetails();
        client.GetTenantAsync(Arg.Is<GetTenantQuery>(q => q.TenantId == "c"), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(FailureResponse<TenantDetail>(TenantsRestQueryFailureKind.Unavailable));
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(DirectResponse(new PaginatedResult<TenantSummary>([], null, false)));
        var logger = new CapturingLogger();
        TenantListSnapshot result = await CreateGateway(client, memoriesClient: ReadSafetyIndex(["a", "b", "c"]), logger: logger)
            .ListTenantsAsync(new(Search: "needle", PageSize: 2), null);
        result.IsAuthoritativeSearch.ShouldBeFalse();
        result.Rows.ShouldBeEmpty();
        result.Notice.ShouldBe(TenantListReason.SearchUnavailable);
        logger.Messages.Where(message => message.Contains(TenantQueryGateway.SearchHydrationUnavailableReasonCode, StringComparison.Ordinal)).ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 3)]
    [InlineData(0, 2)]
    public async Task SearchReadSafetyInvalidWindowOrConsumedPositionRecoversToPageOne(int window, int consumed)
    {
        var codec = new TenantSearchCursorCodec(new EphemeralDataProtectionProvider());
        string scope = TenantSearchCursorScopes.Create("operator-user", "needle", null, TenantListSortColumns.TenantId, false, 2);
        string cursor = codec.Encode(scope, window, consumed);
        TenantListSnapshot result = await CreateGateway(ReadSafetyDetails(), memoriesClient: ReadSafetyIndex(["a"]), searchCursorCodec: codec)
            .ListTenantsAsync(new(Search: "needle", PageSize: 2, SearchCursor: cursor), null);
        result.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("a");
        result.PagingRecovered.ShouldBeTrue();
        result.Notice.ShouldBe(TenantListReason.SearchRefreshed);
    }

    private static StubMemoriesClient ReadSafetyIndex(string?[] raw)
        => new()
        {
            SearchHandler = (request, cancellation) =>
            {
                cancellation.ThrowIfCancellationRequested();
                return Task.FromResult(SearchResult("needle", raw.Length,
                    raw.Skip(request.Offset).Take(request.MaxResults).Where(id => id is not null)
                        .Select(id => Hit(id == "malformed" ? "invalid" : "tenant:" + id)).ToArray()));
            },
        };

    private static ITenantsRestQueryClient ReadSafetyDetails()
    {
        var client = Substitute.For<ITenantsRestQueryClient>();
        client.GetTenantAsync(Arg.Any<GetTenantQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                string id = call.ArgAt<GetTenantQuery>(0).TenantId;
                return Task.FromResult(id.StartsWith("hidden", StringComparison.Ordinal)
                    ? FailureResponse<TenantDetail>(TenantsRestQueryFailureKind.Forbidden, (int)HttpStatusCode.Forbidden)
                    : id == "missing"
                        ? FailureResponse<TenantDetail>(TenantsRestQueryFailureKind.NotFound, (int)HttpStatusCode.NotFound)
                        : DirectResponse(Detail(id)));
            });
        return client;
    }
}
