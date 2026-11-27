using System.Diagnostics;
using System.Runtime.CompilerServices;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using SearchResult = Hexalith.Memories.Contracts.V1.SearchResult;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed partial class TenantQueryGatewayTests
{
    [Theory]
    [InlineData(31)]
    [InlineData(75)]
    public async Task DefaultEnrichmentDeadlineAcceptsAuthoritativeDetailAtSupportedElapsedTimes(int seconds)
    {
        var clock = new ManualTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var held = new TaskCompletionSource<TenantsRestQueryResponse<TenantDetail>>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken detailCancellation = default;
        var client = ReadSafetyDetails();
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(DirectResponse(new PaginatedResult<TenantSummary>([new("a", "A", Hexalith.Tenants.Contracts.Enums.TenantStatus.Active)], null, false)));
        client.GetTenantAsync(Arg.Any<GetTenantQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            detailCancellation = call.ArgAt<CancellationToken>(2);
            started.SetResult();
            return held.Task;
        });
        var load = CreateGateway(client, searchClock: clock).ListTenantsAsync(new(), null);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        clock.Advance(TimeSpan.FromSeconds(seconds));
        detailCancellation.IsCancellationRequested.ShouldBeFalse();
        load.IsCompleted.ShouldBeFalse();
        held.SetResult(DirectResponse(Detail("a")));
        var result = await load.WaitAsync(TimeSpan.FromSeconds(5));
        result.Kind.ShouldBe(TenantListSurfaceKind.Ready);
        result.IsDegraded.ShouldBeFalse();
        var row = result.Rows.ShouldHaveSingleItem();
        row.MemberCount.ShouldBe(TenantCountValue.Known(2));
        row.OwnerCount.ShouldBe(TenantCountValue.Known(1));
        row.Lifecycle.ShouldBe(Hexalith.EventStore.Contracts.Queries.ProjectionLifecycleState.Current);
    }

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

    [Fact]
    public async Task EnrichmentDeadlineCompletesPromptlyWhenDetailTransportIgnoresCancellation()
    {
        var clock = new ManualTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var held = new TaskCompletionSource<TenantsRestQueryResponse<TenantDetail>>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken detailCancellation = default;
        var client = ReadSafetyDetails();
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(DirectResponse(new PaginatedResult<TenantSummary>(
                [
                    new("b", "B", TenantStatus.Active),
                    new("a", "A", TenantStatus.Active),
                ],
                null,
                false)));
        client.GetTenantAsync(Arg.Any<GetTenantQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            string tenantId = call.ArgAt<GetTenantQuery>(0).TenantId;
            if (tenantId == "b")
            {
                return Task.FromResult(DirectResponse(Detail("b")));
            }

            detailCancellation = call.ArgAt<CancellationToken>(2);
            started.TrySetResult();
            return held.Task;
        });

        using TenantQueryGateway gateway = CreateGateway(client, searchClock: clock);
        Task<TenantListSnapshot> load = gateway.ListTenantsAsync(new(), null);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        clock.Advance(TenantQueryGateway.MaximumEnrichmentDuration);

        TenantListSnapshot result = await load.WaitAsync(TimeSpan.FromSeconds(5));
        detailCancellation.IsCancellationRequested.ShouldBeTrue();
        result.Kind.ShouldBe(TenantListSurfaceKind.Degraded);
        result.IsDegraded.ShouldBeTrue();
        result.Reason.ShouldBe(TenantListReason.RowEnrichmentUnavailable);
        result.Rows.Count.ShouldBe(2);
        result.Rows[0].TenantId.ShouldBe("b");
        result.Rows[0].MemberCount.ShouldBe(TenantCountValue.Known(2));
        result.Rows[0].OwnerCount.ShouldBe(TenantCountValue.Known(1));
        result.Rows[1].TenantId.ShouldBe("a");
        result.Rows[1].MemberCount.IsKnown.ShouldBeFalse();
        result.Rows[1].OwnerCount.IsKnown.ShouldBeFalse();
        held.SetException(new InvalidOperationException("late uncooperative enrichment failure"));
    }

    [Fact]
    public async Task CallerCancellationDoesNotWaitForAnUncooperativeEnrichmentTransport()
    {
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var held = new TaskCompletionSource<TenantsRestQueryResponse<TenantDetail>>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken observed = default;
        var client = ReadSafetyDetails();
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(DirectResponse(new PaginatedResult<TenantSummary>([new("a", "A", TenantStatus.Active)], null, false)));
        client.GetTenantAsync(Arg.Any<GetTenantQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            observed = call.ArgAt<CancellationToken>(2);
            started.TrySetResult();
            return held.Task;
        });

        using TenantQueryGateway gateway = CreateGateway(client, searchClock: new ManualTimeProvider());
        Task<TenantListSnapshot> load = gateway.ListTenantsAsync(new(), null, cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => load.WaitAsync(TimeSpan.FromSeconds(5)));
        observed.IsCancellationRequested.ShouldBeTrue();
        held.SetException(new InvalidOperationException("late cancelled enrichment failure"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UncooperativeEnrichmentPreservesFatalSiblingWithOriginalIdentity(bool expireDeadline)
    {
        var clock = new ManualTimeProvider();
        var startedA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedB = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var held = new TaskCompletionSource<TenantsRestQueryResponse<TenantDetail>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var fatalRow = new TaskCompletionSource<TenantsRestQueryResponse<TenantDetail>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var wrapper = new InvalidOperationException("fatal enrichment wrapper", new OutOfMemoryException("original fatal"));
        var client = ReadSafetyDetails();
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(DirectResponse(new PaginatedResult<TenantSummary>(
                [
                    new("a", "A", TenantStatus.Active),
                    new("b", "B", TenantStatus.Active),
                ],
                null,
                false)));
        client.GetTenantAsync(Arg.Any<GetTenantQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (call.ArgAt<GetTenantQuery>(0).TenantId == "a")
            {
                startedA.TrySetResult();
                return held.Task;
            }

            startedB.TrySetResult();
            return fatalRow.Task;
        });

        using TenantQueryGateway gateway = CreateGateway(client, searchClock: clock);
        Task<TenantListSnapshot> load = gateway.ListTenantsAsync(new(), null);
        await Task.WhenAll(startedA.Task, startedB.Task).WaitAsync(TimeSpan.FromSeconds(5));
        fatalRow.SetException(wrapper);
        if (expireDeadline)
        {
            clock.Advance(TenantQueryGateway.MaximumEnrichmentDuration);
        }

        (await Should.ThrowAsync<InvalidOperationException>(() => load.WaitAsync(TimeSpan.FromSeconds(5))))
            .ShouldBeSameAs(wrapper);
        held.SetException(new InvalidOperationException("late uncooperative sibling failure"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ComposedFrontComposerGatewayAuthorizationHandlerPendingAuthCompletesPromptlyAtEnrichmentDeadline(
        bool circuitAuthStaysPending)
    {
        var clock = new ManualTimeProvider();
        var pendingAuth = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingAuthEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<AuthenticationState> readyState = Task.FromResult(new AuthenticationState(GatewayPrincipal("operator-user")));
        int authPending = 0;

        AuthenticationStateProvider provider = Substitute.For<AuthenticationStateProvider>();
        provider.GetAuthenticationStateAsync().Returns(_ =>
        {
            if (Volatile.Read(ref authPending) != 0)
            {
                if (!circuitAuthStaysPending)
                {
                    Volatile.Write(ref authPending, 0);
                }

                pendingAuthEntered.TrySetResult();
                return pendingAuth.Task;
            }

            return readyState;
        });

        ServiceCollection services = new();
        services.AddSingleton(provider);
        using ServiceProvider circuitServices = services.BuildServiceProvider();
        var circuitServicesAccessor = new CircuitServicesAccessor { Services = circuitServices };

        using var authHandler = new FrontComposerGatewayAuthorizationHandler(
            new HttpContextAccessor(),
            circuitServicesAccessor,
            new FrontComposerUserTokenStore())
        {
            InnerHandler = new StatusHandler(
                "{\"items\":[{\"tenantId\":\"a\",\"name\":\"A\",\"status\":\"Active\"}],\"cursor\":null,\"hasMore\":false}"),
        };
        using var interceptingHandler = new RequestInterceptingHandler(
            request =>
            {
                if (request.RequestUri?.AbsolutePath.Equals("/api/tenants/a", StringComparison.Ordinal) == true)
                {
                    Volatile.Write(ref authPending, 1);
                }
            },
            authHandler);
        using var httpClient = new HttpClient(interceptingHandler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://tenants.invalid"),
        };

        var restClient = new TenantsRestQueryClient(httpClient);
        using TenantQueryGateway gateway = CreateGateway(
            restClient,
            circuitServicesAccessor: circuitServicesAccessor,
            searchClock: clock);

        Task<TenantListSnapshot> load = gateway.ListTenantsAsync(new(), null);
        await pendingAuthEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        clock.Advance(TenantQueryGateway.MaximumEnrichmentDuration);

        TenantListSnapshot result = await load.WaitAsync(TimeSpan.FromSeconds(5));
        if (circuitAuthStaysPending)
        {
            result.Kind.ShouldBe(TenantListSurfaceKind.Unauthorized);
            result.Rows.ShouldBeEmpty();
        }
        else
        {
            result.Kind.ShouldBe(TenantListSurfaceKind.Degraded);
            result.IsDegraded.ShouldBeTrue();
            result.Reason.ShouldBe(TenantListReason.RowEnrichmentUnavailable);
            TenantListRow row = result.Rows.ShouldHaveSingleItem();
            row.TenantId.ShouldBe("a");
            row.MemberCount.IsKnown.ShouldBeFalse();
            row.OwnerCount.IsKnown.ShouldBeFalse();
        }

        pendingAuth.TrySetResult(new AuthenticationState(GatewayPrincipal("operator-user")));
    }

    [Fact]
    public async Task ListTenantsSkipsDetailEnrichmentWhenIncludeRowEnrichmentIsFalse()
    {
        new TenantListRequest().IncludeRowEnrichment.ShouldBeTrue();

        var client = ReadSafetyDetails();
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(DirectResponse(new PaginatedResult<TenantSummary>(
                [
                    new("a", "A", TenantStatus.Active),
                    new("b", "B", TenantStatus.Disabled),
                ],
                null,
                false)));

        using TenantQueryGateway gateway = CreateGateway(client);
        TenantListSnapshot result = await gateway.ListTenantsAsync(
            new TenantListRequest(IncludeRowEnrichment: false),
            null);

        result.Kind.ShouldBe(TenantListSurfaceKind.Ready);
        result.IsDegraded.ShouldBeFalse();
        result.Reason.ShouldBe(TenantListReason.None);
        result.Freshness.ShouldBe(ReadModelFreshnessState.Current);
        result.Lifecycle.ShouldBe(ProjectionLifecycleState.Current);
        result.Rows.Select(static row => row.TenantId).ShouldBe(["a", "b"]);
        result.Rows.ShouldAllBe(static row =>
            !row.MemberCount.IsKnown
            && !row.OwnerCount.IsKnown
            && row.Freshness == ReadModelFreshnessState.Current
            && row.Lifecycle == ProjectionLifecycleState.Current);
        await client.DidNotReceive().GetTenantAsync(
            Arg.Any<GetTenantQuery>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EnrichmentPreservesFatalSiblingHiddenByEarlierNonfatalSurfacingOrCancellation(bool callerCanceled)
    {
        var wrapper = new InvalidOperationException("fatal enrichment wrapper", new OutOfMemoryException("original fatal"));
        using var cancellation = new CancellationTokenSource();
        var startedA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedB = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var heldA = new TaskCompletionSource<TenantsRestQueryResponse<TenantDetail>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var heldB = new TaskCompletionSource<TenantsRestQueryResponse<TenantDetail>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = ReadSafetyDetails();
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(DirectResponse(new PaginatedResult<TenantSummary>(
                [
                    new("a", "A", TenantStatus.Active),
                    new("b", "B", TenantStatus.Active),
                ],
                null,
                false)));
        client.GetTenantAsync(Arg.Any<GetTenantQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            string tenantId = call.ArgAt<GetTenantQuery>(0).TenantId;
            if (tenantId == "a")
            {
                startedA.TrySetResult();
                return heldA.Task;
            }

            startedB.TrySetResult();
            return heldB.Task;
        });

        using TenantQueryGateway gateway = CreateGateway(client, searchClock: new ManualTimeProvider());
        Task<TenantListSnapshot> load = gateway.ListTenantsAsync(new(), null, cancellation.Token);
        await Task.WhenAll(startedA.Task, startedB.Task).WaitAsync(TimeSpan.FromSeconds(5));
        if (callerCanceled)
        {
            heldB.SetException(wrapper);
            cancellation.Cancel();
        }
        else
        {
            heldB.SetException(wrapper);
            heldA.SetException(new NullReferenceException("nonfatal earlier surfacing defect"));
        }

        (await Should.ThrowAsync<InvalidOperationException>(() => load.WaitAsync(TimeSpan.FromSeconds(5))))
            .ShouldBeSameAs(wrapper);
        heldA.TrySetResult(DirectResponse(Detail("a")));
    }

    [Fact]
    public async Task SurfacingDefectUnblocksUncooperativeEnrichmentSiblingPromptly()
    {
        var clock = new ManualTimeProvider();
        var startedA = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedB = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var heldA = new TaskCompletionSource<TenantsRestQueryResponse<TenantDetail>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var defectB = new TaskCompletionSource<TenantsRestQueryResponse<TenantDetail>>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken detailCancellation = default;
        var defect = new NullReferenceException("nonfatal enrichment surfacing defect");
        var client = ReadSafetyDetails();
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(DirectResponse(new PaginatedResult<TenantSummary>(
                [
                    new("a", "A", TenantStatus.Active),
                    new("b", "B", TenantStatus.Active),
                ],
                null,
                false)));
        client.GetTenantAsync(Arg.Any<GetTenantQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (call.ArgAt<GetTenantQuery>(0).TenantId == "a")
            {
                detailCancellation = call.ArgAt<CancellationToken>(2);
                startedA.TrySetResult();
                return heldA.Task;
            }

            startedB.TrySetResult();
            return defectB.Task;
        });

        using TenantQueryGateway gateway = CreateGateway(client, searchClock: clock);
        Task<TenantListSnapshot> load = gateway.ListTenantsAsync(new(), null);
        await Task.WhenAll(startedA.Task, startedB.Task).WaitAsync(TimeSpan.FromSeconds(5));
        defectB.SetException(defect);

        TenantListSnapshot result = await load.WaitAsync(TimeSpan.FromSeconds(5));
        result.Kind.ShouldBe(TenantListSurfaceKind.Error);
        result.Rows.ShouldBeEmpty();
        detailCancellation.IsCancellationRequested.ShouldBeTrue();
        heldA.SetException(new InvalidOperationException("late sibling failure after surfacing defect"));
    }

    [Fact]
    public async Task AbandonedEnrichmentDetailTaskObservesGenuineLateFaultWithoutUnobservedTaskException()
    {
        string marker = $"late-enrichment-fault-{Guid.NewGuid():N}";
        int unobserved = 0;
        EventHandler<UnobservedTaskExceptionEventArgs> handler = (_, args) =>
        {
            if (args.Exception.ToString().Contains(marker, StringComparison.Ordinal))
            {
                _ = Interlocked.Exchange(ref unobserved, 1);
                args.SetObserved();
            }
        };

        TaskScheduler.UnobservedTaskException += handler;
        try
        {
            await RunAbandonedLateFaultEnrichmentAsync(marker);
            for (int iteration = 0; iteration < 3; iteration++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                GC.WaitForPendingFinalizers();
            }

            Volatile.Read(ref unobserved).ShouldBe(0);
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= handler;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task RunAbandonedLateFaultEnrichmentAsync(string marker)
    {
        var clock = new ManualTimeProvider();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // Synchronous continuations ensure the abandoned LoadTenantDetailAsync task transitions to
        // Faulted (bypassing CallerBoundTenantsRestQueryClient's non-fatal cancellation replacement)
        // and runs its fault-observation continuation before this frame exits for GC.
        var held = new TaskCompletionSource<TenantsRestQueryResponse<TenantDetail>>(TaskCreationOptions.None);
        var client = ReadSafetyDetails();
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(DirectResponse(new PaginatedResult<TenantSummary>([new("a", "A", TenantStatus.Active)], null, false)));
        client.GetTenantAsync(Arg.Any<GetTenantQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            started.TrySetResult();
            return held.Task;
        });

        using TenantQueryGateway gateway = CreateGateway(client, searchClock: clock);
        Task<TenantListSnapshot> load = gateway.ListTenantsAsync(new(), null);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        clock.Advance(TenantQueryGateway.MaximumEnrichmentDuration);

        TenantListSnapshot result = await load.WaitAsync(TimeSpan.FromSeconds(5));
        result.Kind.ShouldBe(TenantListSurfaceKind.Degraded);
        result.Reason.ShouldBe(TenantListReason.RowEnrichmentUnavailable);

        held.SetException(new InvalidOperationException(marker, new OutOfMemoryException(marker)));
    }

    [Fact]
    public async Task EnrichedListReadDoesNotReuseUnenrichedPreviousSnapshotOnNotModifiedOrOutage()
    {
        var client = ReadSafetyDetails();
        var listETags = new List<string?>();
        int listCalls = 0;
        client.ListTenantsAsync(Arg.Any<ListTenantsQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            listETags.Add(call.ArgAt<string?>(1));
            int current = Interlocked.Increment(ref listCalls);
            return current switch
            {
                1 => DirectResponse(
                    new PaginatedResult<TenantSummary>([new("a", "A", TenantStatus.Active)], null, false),
                    eTag: "etag-1"),
                2 => NotModifiedResponse<PaginatedResult<TenantSummary>>("etag-1"),
                3 => DirectResponse(
                    new PaginatedResult<TenantSummary>([new("a", "A", TenantStatus.Active)], null, false),
                    eTag: "etag-1"),
                _ => FailureResponse<PaginatedResult<TenantSummary>>(TenantsRestQueryFailureKind.Unavailable),
            };
        });

        using TenantQueryGateway gateway = CreateGateway(client);
        TenantListSnapshot unenriched = await gateway.ListTenantsAsync(
            new TenantListRequest(IncludeRowEnrichment: false),
            null);
        unenriched.Kind.ShouldBe(TenantListSurfaceKind.Ready);
        unenriched.ETag.ShouldBe("etag-1");
        unenriched.IncludesRowEnrichment.ShouldBeFalse();
        unenriched.Rows.ShouldHaveSingleItem().MemberCount.IsKnown.ShouldBeFalse();

        TenantListSnapshot enriched = await gateway.ListTenantsAsync(
            new TenantListRequest(ETag: unenriched.ETag, IncludeRowEnrichment: true),
            unenriched);
        enriched.Kind.ShouldBe(TenantListSurfaceKind.Ready);
        enriched.IsDegraded.ShouldBeFalse();
        enriched.IncludesRowEnrichment.ShouldBeTrue();
        TenantListRow row = enriched.Rows.ShouldHaveSingleItem();
        row.MemberCount.ShouldBe(TenantCountValue.Known(2));
        row.OwnerCount.ShouldBe(TenantCountValue.Known(1));
        listETags.ShouldBe([null, null, null]);
        await client.Received(1).GetTenantAsync(
            Arg.Is<GetTenantQuery>(query => query.TenantId == "a"),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());

        TenantListSnapshot failed = await gateway.ListTenantsAsync(
            new TenantListRequest(ETag: unenriched.ETag, IncludeRowEnrichment: true),
            unenriched);
        failed.Kind.ShouldBe(TenantListSurfaceKind.Error);
        failed.Rows.ShouldBeEmpty();
    }
}
