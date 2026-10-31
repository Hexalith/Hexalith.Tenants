using System.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using SearchResult = Hexalith.Memories.Contracts.V1.SearchResult;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;
using NSubstitute;
using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed partial class TenantQueryGatewayTests
{
    [Fact]
    public async Task EnrichmentTimerObservesThrowingProviderCancellationCallbacks()
    {
        var clock = new ManualTimeProvider();
        var callback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var held = new TaskCompletionSource<TenantsRestQueryResponse<TenantDetail>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = ReadSafetyDetails();
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(DirectResponse(new PaginatedResult<TenantSummary>([new("a", "A", Hexalith.Tenants.Contracts.Enums.TenantStatus.Active)], null, false)));
        client.GetTenantAsync(Arg.Any<GetTenantQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var token = call.ArgAt<CancellationToken>(2);
            token.Register(() => { held.TrySetCanceled(token); callback.TrySetResult(); throw new InvalidOperationException("enrichment callback"); });
            started.SetResult(); return held.Task;
        });
        var load = CreateGateway(client, searchClock: clock).ListTenantsAsync(new(), null);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        clock.Advance(TenantQueryGateway.MaximumEnrichmentDuration);
        await callback.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var result = await load.WaitAsync(TimeSpan.FromSeconds(5));
        result.IsDegraded.ShouldBeTrue(); result.Rows.ShouldHaveSingleItem().MemberCount.IsKnown.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BudgetTimerObservesThrowingProviderCallbacksAndPreservesCompletedFatal(bool fatal)
    {
        var clock = new ManualTimeProvider();
        var callback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var callbackEntered = new ManualResetEventSlim();
        var pending = new TaskCompletionSource<SearchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var original = new InvalidOperationException("original fatal wrapper", new OutOfMemoryException());
        var memories = new StubMemoriesClient
        {
            SearchHandler = (_, token) =>
        {
            token.Register(() => { callbackEntered.Set(); callback.TrySetResult(); throw new InvalidOperationException("provider callback"); });
            if (fatal)
            {
                clock.Advance(TimeSpan.FromSeconds(10));
                // Keep the provider active until its queued callback starts; completed-load disposal
                // may otherwise remove the registration before CancelAsync executes it.
                callbackEntered.Wait(TimeSpan.FromSeconds(5)).ShouldBeTrue();
                return Task.FromException<SearchResult>(original);
            }
            return pending.Task;
        }
        };
        Task<TenantListSnapshot> load = CreateGateway(ReadSafetyDetails(), memoriesClient: memories, searchClock: clock)
            .ListTenantsAsync(new(Search: "needle"), null);
        if (!fatal) clock.Advance(TimeSpan.FromSeconds(10));
        if (fatal) (await Should.ThrowAsync<InvalidOperationException>(() => load.WaitAsync(TimeSpan.FromSeconds(5)))).ShouldBeSameAs(original);
        else (await load.WaitAsync(TimeSpan.FromSeconds(5))).Kind.ShouldBe(TenantListSurfaceKind.Error);
        await callback.Task.WaitAsync(TimeSpan.FromSeconds(5));
        pending.TrySetResult(SearchResult("needle", 0));
    }

    [Theory]
    [InlineData("index")]
    [InlineData("fallback")]
    public async Task InvalidSearchCursorRecoverySurvivesBudgetExpiry(string seam)
    {
        var clock = new ManualTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var held = new TaskCompletionSource<SearchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var heldList = new TaskCompletionSource<TenantsRestQueryResponse<PaginatedResult<TenantSummary>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = ReadSafetyDetails();
        var memories = new StubMemoriesClient
        {
            SearchHandler = (_, _) =>
        {
            if (seam == "fallback") return Task.FromException<SearchResult>(new HttpRequestException());
            started.SetResult(); return held.Task;
        }
        };
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => { started.SetResult(); return heldList.Task; });
        var load = CreateGateway(client, memoriesClient: memories, searchClock: clock)
            .ListTenantsAsync(new(Search: "needle", SearchCursor: "invalid-protected-cursor"), null);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        clock.Advance(TimeSpan.FromSeconds(10));
        var result = await load.WaitAsync(TimeSpan.FromSeconds(5));
        result.Kind.ShouldBe(TenantListSurfaceKind.Error);
        result.PagingRecovered.ShouldBeTrue(); result.PagingNotice.ShouldBe(TenantListReason.SearchRefreshed);
        result.Rows.ShouldBeEmpty(); result.NextCursor.ShouldBeNull();
        held.TrySetResult(SearchResult("needle", 0));
        heldList.TrySetResult(DirectResponse(new PaginatedResult<TenantSummary>([], null, false)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OnlyBudgetExpiryEmitsTheIdentifierFreeWarning(bool callerCancelled)
    {
        var clock = new ManualTimeProvider();
        using var cancellation = new CancellationTokenSource();
        var logger = new CapturingLogger();
        var held = new TaskCompletionSource<SearchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var memories = new StubMemoriesClient { SearchHandler = (_, _) => held.Task };
        var load = CreateGateway(ReadSafetyDetails(), memoriesClient: memories, searchClock: clock, logger: logger)
            .ListTenantsAsync(new(Search: "private-search"), null, cancellation.Token);
        if (callerCancelled) cancellation.Cancel(); else clock.Advance(TimeSpan.FromSeconds(10));
        if (callerCancelled) await Should.ThrowAsync<OperationCanceledException>(() => load);
        else (await load).Kind.ShouldBe(TenantListSurfaceKind.Error);
        logger.WarningEvents.Count(value => value.Id == 1906).ShouldBe(callerCancelled ? 0 : 1);
        logger.WarningMessages.ShouldAllBe(message => message == "Tenant search load budget expired.");
        ShouldNotDisclose(logger, "private-search", "operator-user");
        held.SetResult(SearchResult("private-search", 0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FatalCompletionKeepsItsOriginalWrapperAcrossCallerRetirementAndCancellation(bool cancellation)
    {
        using var cancelled = new CancellationTokenSource();
        IUserContextAccessor caller = Substitute.For<IUserContextAccessor>();
        caller.UserId.Returns("operator-user");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completion = new TaskCompletionSource<SearchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var memories = new StubMemoriesClient { SearchHandler = (_, _) => { started.SetResult(); return completion.Task; } };
        var fatal = new OutOfMemoryException("original");
        var wrapper = new InvalidOperationException("wrapper", new AggregateException(fatal));
        Task<TenantListSnapshot> read = CreateGateway(ReadSafetyDetails(), memoriesClient: memories, userContextAccessor: caller)
            .ListTenantsAsync(new(Search: "needle"), null, cancelled.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        caller.UserId.Returns("new-caller");
        completion.SetException(wrapper);
        if (cancellation) cancelled.Cancel();
        (await Should.ThrowAsync<InvalidOperationException>(() => read)).ShouldBeSameAs(wrapper);
        wrapper.InnerException!.InnerException.ShouldBeSameAs(fatal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FatalCancellationWrapperKeepsItsIdentityAtTheCancellationBoundary(bool expiry)
    {
        using var callerCancellation = new CancellationTokenSource();
        var clock = new ManualTimeProvider();
        var wrapper = new OperationCanceledException("original cancellation wrapper", new OutOfMemoryException("original fatal"));
        async Task<SearchResult> CanceledProviderAsync()
        {
            await Task.CompletedTask;
            throw wrapper;
        }
        Task<SearchResult> provider = CanceledProviderAsync();
        provider.IsCanceled.ShouldBeTrue();
        provider.Exception.ShouldBeNull();
        var memories = new StubMemoriesClient
        {
            SearchHandler = (_, _) =>
        {
            if (expiry) clock.Advance(TimeSpan.FromSeconds(10));
            else callerCancellation.Cancel();
            return provider;
        }
        };
        Exception? observed = null;
        try
        {
            await CreateGateway(ReadSafetyDetails(), memoriesClient: memories, searchClock: clock)
                .ListTenantsAsync(new(Search: "needle"), null, callerCancellation.Token);
        }
        catch (Exception exception) { observed = exception; }
        observed.ShouldBeSameAs(wrapper);
    }

    [Fact]
    public async Task ThrowingCancellationCallbackCannotReplaceACompletedFatalLoad()
    {
        var wrapper = new InvalidOperationException("original wrapper", new OutOfMemoryException("original fatal"));
        bool callbackInvoked = false;
        var memories = new StubMemoriesClient
        {
            SearchHandler = (_, token) =>
        {
            token.Register(() =>
            {
                callbackInvoked = true;
                throw new InvalidOperationException("cancellation callback failure");
            });
            return Task.FromException<SearchResult>(wrapper);
        }
        };
        (await Should.ThrowAsync<InvalidOperationException>(() => CreateGateway(ReadSafetyDetails(), memoriesClient: memories)
            .ListTenantsAsync(new(Search: "needle"), null))).ShouldBeSameAs(wrapper);
        callbackInvoked.ShouldBeFalse();
    }

    [Theory]
    [InlineData("index")]
    [InlineData("hydration")]
    [InlineData("fallback")]
    public async Task OneSearchDeadlineCancelsEverySeamAndDiscardsLateEvidence(string seam)
    {
        var clock = new ManualTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lateIndex = new TaskCompletionSource<SearchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lateDetail = new TaskCompletionSource<TenantsRestQueryResponse<TenantDetail>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var lateList = new TaskCompletionSource<TenantsRestQueryResponse<PaginatedResult<TenantSummary>>>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken observed = default;
        var client = ReadSafetyDetails();
        var memories = ReadSafetyIndex(["a"]);
        if (seam == "index") memories = new StubMemoriesClient { SearchHandler = (_, token) => { observed = token; started.SetResult(); return lateIndex.Task; } };
        if (seam == "hydration") client.GetTenantAsync(Arg.Any<GetTenantQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(call => { observed = call.ArgAt<CancellationToken>(2); started.SetResult(); return lateDetail.Task; });
        if (seam == "fallback")
        {
            memories = new StubMemoriesClient { SearchHandler = (_, _) => Task.FromException<SearchResult>(new HttpRequestException()) };
            client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(call => { observed = call.ArgAt<CancellationToken>(2); started.SetResult(); return lateList.Task; });
        }
        var gateway = CreateGateway(client, memoriesClient: memories, searchClock: clock);
        var wall = Stopwatch.StartNew();
        Task<TenantListSnapshot> load = gateway.ListTenantsAsync(new(Search: "needle"), null);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        clock.Advance(TimeSpan.FromSeconds(10));
        TenantListSnapshot result = await load.WaitAsync(TimeSpan.FromSeconds(5));
        result.Kind.ShouldBe(TenantListSurfaceKind.Error); result.Rows.ShouldBeEmpty(); result.IsAuthoritativeSearch.ShouldBeFalse();
        observed.IsCancellationRequested.ShouldBeTrue();
        lateIndex.TrySetResult(SearchResult("needle", 1, Hit("tenant:a")));
        lateDetail.TrySetResult(DirectResponse(Detail("a")));
        lateList.TrySetResult(DirectResponse(new PaginatedResult<TenantSummary>([], null, false)));
        result.Rows.ShouldBeEmpty();
        TestContext.Current.TestOutputHelper!.WriteLine($"{seam}: 10 s virtual deadline returned in {wall.Elapsed.TotalMilliseconds:F2} ms wall time; provider cancellation observed.");
    }

    [Fact]
    public async Task CallerCancellationDoesNotWaitForANoncooperatingSearchProvider()
    {
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var late = new TaskCompletionSource<SearchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken observed = default;
        var memories = new StubMemoriesClient
        {
            SearchHandler = (_, token) =>
        { observed = token; started.SetResult(); return late.Task; }
        };
        var load = CreateGateway(ReadSafetyDetails(), memoriesClient: memories, searchClock: new ManualTimeProvider())
            .ListTenantsAsync(new(Search: "needle"), null, cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(() => load.WaitAsync(TimeSpan.FromSeconds(5)));
        observed.IsCancellationRequested.ShouldBeTrue();
        late.SetResult(SearchResult("needle", 0));
    }

    [Fact]
    public async Task SlowWindowsShareTheTenSecondLoadBudget()
    {
        var clock = new ManualTimeProvider();
        var logger = new CapturingLogger();
        var memories = new StubMemoriesClient
        {
            SearchHandler = (request, token) =>
        {
            clock.Advance(TimeSpan.FromSeconds(4));
            return Task.FromResult(SearchResult("needle", 5, Hit("tenant:hidden")));
        }
        };
        var client = ReadSafetyDetails();
        var page = await CreateGateway(client, memoriesClient: memories, searchClock: clock, logger: logger)
            .ListTenantsAsync(new(Search: "needle", PageSize: 1), null);
        page.Kind.ShouldBe(TenantListSurfaceKind.Error); page.Rows.ShouldBeEmpty();
        memories.SearchRequests.Count.ShouldBe(3);
        logger.Messages.Count(message => message.Contains("Stage=window", StringComparison.Ordinal)).ShouldBe(3);
        logger.Messages.Count(message => message.Contains("Stage=load", StringComparison.Ordinal)).ShouldBe(1);
        logger.Messages.ShouldAllBe(message => !message.Contains("needle", StringComparison.Ordinal));
        await client.DidNotReceive().ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
        TestContext.Current.TestOutputHelper!.WriteLine("Three 4 s windows exhausted one 10 s budget; no replacement fallback began.");
    }

    [Fact]
    public async Task FatalObservedAtTheDeadlineIsPropagatedWithoutReplacement()
    {
        var clock = new ManualTimeProvider();
        var fatal = new OutOfMemoryException("original");
        var wrapper = new InvalidOperationException("wrapper", fatal);
        var memories = new StubMemoriesClient
        {
            SearchHandler = (_, _) =>
        {
            clock.Advance(TimeSpan.FromSeconds(10));
            return Task.FromException<SearchResult>(wrapper);
        }
        };
        (await Should.ThrowAsync<InvalidOperationException>(() => CreateGateway(ReadSafetyDetails(), memoriesClient: memories,
            searchClock: clock).ListTenantsAsync(new(Search: "needle"), null))).ShouldBeSameAs(wrapper);
    }
}
