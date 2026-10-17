using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed class GlobalAdministratorsProjectionLoaderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("caller-two")]
    public async Task Complete_projection_walk_rejects_unbound_or_other_caller_pages(string? secondCaller)
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        gateway.GetGlobalAdministratorsAsync(Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                GlobalAdministratorsRequest request = call.Arg<GlobalAdministratorsRequest>();
                return ++calls == 1
                    ? Page(["private-admin"], "page-two", true, request: request) with
                    { CallerScope = TenantQueryGateway.AuditCallerScope("caller-one") }
                    : Page(["current-admin"], null, false, request: request) with
                    { CallerScope = secondCaller is null ? null : TenantQueryGateway.AuditCallerScope(secondCaller) };
            });

        GlobalAdministratorsSnapshot result = await GlobalAdministratorsProjectionLoader.LoadAsync(gateway);

        result.IsCompleteEvidence.ShouldBeFalse();
        result.Rows.ShouldBeEmpty();
        result.ETag.ShouldBeNull();
        result.ProjectionVersion.ShouldBeNull();
        calls.ShouldBe(2);
    }

    [Fact]
    public async Task LoadAsyncAggregatesValidPagesAndForwardsOpaqueCursors()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        var requests = new List<GlobalAdministratorsRequest>();
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                GlobalAdministratorsRequest request = call.ArgAt<GlobalAdministratorsRequest>(0);
                requests.Add(request);
                return Task.FromResult(request.Cursor is null
                    ? Page(["admin-a"], "opaque cursor/+", hasMore: true, eTag: "etag-page-1", request: request)
                    : Page(["admin-b"], null, hasMore: false, eTag: "etag-page-2", request: request));
            });

        GlobalAdministratorsSnapshot result = await GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            new GlobalAdministratorsRequest(PageSize: 7, ETag: "etag-page-1"),
            TestContext.Current.CancellationToken);

        result.IsCompleteEvidence.ShouldBeTrue();
        result.HasMore.ShouldBeFalse();
        result.NextCursor.ShouldBeNull();
        result.ETag.ShouldBeNull();
        result.Rows.Select(static row => row.UserId).ShouldBe(["admin-a", "admin-b"]);
        requests.ShouldBe(
        [
            new GlobalAdministratorsRequest(PageSize: 7, ETag: "etag-page-1"),
            new GlobalAdministratorsRequest("opaque cursor/+", PageSize: 7, ETag: null),
        ]);
    }

    [Theory]
    [InlineData("stale")]
    [InlineData("non-current-lifecycle")]
    [InlineData("missing-version")]
    [InlineData("mixed-version")]
    [InlineData("row-not-current")]
    public async Task LoadAsyncRejectsNonCurrentOrVersionInconsistentPages(string scenario)
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                calls++;
                GlobalAdministratorsRequest request = call.ArgAt<GlobalAdministratorsRequest>(0);
                GlobalAdministratorsSnapshot page = Page([calls == 1 ? "admin-a" : "admin-b"], calls == 1 ? "page-2" : null, calls == 1, request: request);
                return Task.FromResult(scenario switch
                {
                    "stale" => page with
                    {
                        Kind = GlobalAdministratorsSurfaceKind.Stale,
                        Freshness = ReadModelFreshnessState.Stale,
                    },
                    "non-current-lifecycle" => page with { Lifecycle = ProjectionLifecycleState.Rebuilding },
                    "missing-version" => page with { ProjectionVersion = " " },
                    "mixed-version" when calls == 2 => page with { ProjectionVersion = "v2" },
                    "row-not-current" => page with
                    {
                        Rows = [new GlobalAdministratorRow(
                            "admin-a",
                            ReadModelFreshnessState.Stale,
                            ProjectionLifecycleState.Stale)],
                    },
                    _ => page,
                });
            });

        GlobalAdministratorsSnapshot result = await GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsCompleteEvidence.ShouldBeFalse();
        calls.ShouldBe(scenario == "mixed-version" ? 2 : 1);
    }

    [Fact]
    public async Task LoadAsyncRejectsGatewayPageOneRecovery()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                GlobalAdministratorsRequest request = call.ArgAt<GlobalAdministratorsRequest>(0);
                return Task.FromResult(++calls == 1
                    ? Page(["admin-a"], "page-2", hasMore: true, request: request)
                    : Page(["admin-b"], null, hasMore: false, request: request) with { PagingRecovered = true });
            });

        GlobalAdministratorsSnapshot result = await GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsCompleteEvidence.ShouldBeFalse();
        AssertCanonicalIncomplete(result);
        calls.ShouldBe(2);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData("", 1)]
    [InlineData("cycle", 2)]
    public async Task LoadAsyncRejectsMissingOrRepeatedContinuations(string? continuation, int expectedCalls)
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                calls++;
                GlobalAdministratorsRequest request = call.ArgAt<GlobalAdministratorsRequest>(0);
                return Task.FromResult(Page(
                    [$"admin-{calls}"],
                    continuation == "cycle" ? "cycle" : continuation,
                    hasMore: true,
                    request: request));
            });

        GlobalAdministratorsSnapshot result = await GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            cancellationToken: TestContext.Current.CancellationToken);

        AssertCanonicalIncomplete(result);
        calls.ShouldBe(expectedCalls);
    }

    [Fact]
    public async Task LoadAsyncStopsAtPageCap()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                GlobalAdministratorsRequest request = call.ArgAt<GlobalAdministratorsRequest>(0);
                calls++;
                return Task.FromResult(Page([$"admin-{calls}"], $"page-{calls}", hasMore: true, request: request));
            });

        GlobalAdministratorsSnapshot result = await GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            cancellationToken: TestContext.Current.CancellationToken,
            maximumPageCount: 3);

        calls.ShouldBe(3);
        AssertCanonicalIncomplete(result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LoadAsyncPropagatesCancellationBeforeAcceptingAnyPage(bool hasMore)
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                calls++;
                source.Cancel();
                GlobalAdministratorsRequest request = call.ArgAt<GlobalAdministratorsRequest>(0);
                return Task.FromResult(Page(["admin-a"], hasMore ? "page-2" : null, hasMore, request: request));
            });

        await Should.ThrowAsync<OperationCanceledException>(() => GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            cancellationToken: source.Token));

        calls.ShouldBe(1);
    }

    [Fact]
    public async Task LoadAsyncCollapsesLaterPageUnauthorizedToCanonicalRedactedResult()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                GlobalAdministratorsRequest request = call.ArgAt<GlobalAdministratorsRequest>(0);
                return Task.FromResult(++calls == 1
                    ? Page(["sensitive-admin"], "page-2", hasMore: true, request: request)
                    : GlobalAdministratorsSnapshot.Unauthorized() with
                    {
                        Rows = [new GlobalAdministratorRow("must-not-survive", ReadModelFreshnessState.Current, ProjectionLifecycleState.Current)],
                        ProjectionVersion = "must-not-survive",
                    });
            });

        GlobalAdministratorsSnapshot result = await GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBe(GlobalAdministratorsSnapshot.Unauthorized());
        calls.ShouldBe(2);
    }

    [Theory]
    [InlineData("null-row")]
    [InlineData("empty-with-row")]
    [InlineData("ready-without-row")]
    [InlineData("oversized-page")]
    [InlineData("wrong-request-cursor")]
    [InlineData("wrong-request-page-size")]
    [InlineData("blank-identity")]
    [InlineData("control-character-identity")]
    [InlineData("oversized-identity")]
    [InlineData("lone-surrogate-identity")]
    public async Task LoadAsyncRejectsMalformedPageShapesWithCanonicalBoundedResult(string scenario)
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                calls++;
                GlobalAdministratorsRequest request = call.ArgAt<GlobalAdministratorsRequest>(0);
                GlobalAdministratorsSnapshot valid = Page(["admin-a"], null, hasMore: false, request: request);
                return Task.FromResult(scenario switch
                {
                    "null-row" => valid with { Rows = [null!] },
                    "empty-with-row" => valid with
                    {
                        Kind = GlobalAdministratorsSurfaceKind.Empty,
                        IsAuthorizationScopedEmpty = true,
                    },
                    "ready-without-row" => valid with { Rows = [] },
                    "oversized-page" => valid with
                    {
                        Rows = Enumerable.Range(0, request.PageSize + 1)
                            .Select(static index => new GlobalAdministratorRow(
                                $"admin-{index}",
                                ReadModelFreshnessState.Current,
                                ProjectionLifecycleState.Current))
                            .ToArray(),
                    },
                    "wrong-request-cursor" => valid with { RequestCursor = "unexpected" },
                    "wrong-request-page-size" => valid with { RequestPageSize = request.PageSize + 1 },
                    "blank-identity" => valid with { Rows = [CurrentRow(" ")] },
                    "control-character-identity" => valid with { Rows = [CurrentRow("admin\u0001")] },
                    "oversized-identity" => valid with
                    {
                        Rows = [CurrentRow(new string('a', GlobalAdministratorUserId.MaximumLength + 1))],
                    },
                    "lone-surrogate-identity" => valid with { Rows = [CurrentRow("admin\uD800")] },
                    _ => throw new InvalidOperationException($"Unknown malformed scenario '{scenario}'."),
                });
            });

        GlobalAdministratorsSnapshot result = await GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            cancellationToken: TestContext.Current.CancellationToken);

        AssertCanonicalIncomplete(result);
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task LoadAsyncDeduplicatesIdentitiesWithinAndAcrossPagesWithOrdinalComparison()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                calls++;
                GlobalAdministratorsRequest request = call.ArgAt<GlobalAdministratorsRequest>(0);
                return Task.FromResult(calls == 1
                    ? Page(["admin-a", "admin-a"], "page-2", hasMore: true, request: request)
                    : Page(["admin-a", "Admin-a", "admin-b"], null, hasMore: false, request: request));
            });

        GlobalAdministratorsSnapshot result = await GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsCompleteEvidence.ShouldBeTrue();
        result.Rows.Select(static row => row.UserId).ShouldBe(["admin-a", "Admin-a", "admin-b"]);
        calls.ShouldBe(2);
    }

    [Theory]
    [MemberData(nameof(ConflictingDuplicateEvidenceCases))]
    public async Task LoadAsyncRejectsConflictingDuplicateEvidenceWithoutReadingFurtherPages(
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle,
        bool acrossPages,
        bool conflictFirst,
        bool hasContinuation)
    {
        GlobalAdministratorRow current = CurrentRow("private-admin");
        GlobalAdministratorRow conflicting = current with
        {
            Freshness = freshness,
            Lifecycle = lifecycle,
        };
        GlobalAdministratorRow first = conflictFirst ? conflicting : current;
        GlobalAdministratorRow second = conflictFirst ? current : conflicting;
        var requests = new List<GlobalAdministratorsRequest>();
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                GlobalAdministratorsRequest request = call.ArgAt<GlobalAdministratorsRequest>(0);
                call.ArgAt<GlobalAdministratorsSnapshot?>(1).ShouldBeNull();
                call.ArgAt<CancellationToken>(2).ShouldBe(TestContext.Current.CancellationToken);
                requests.Add(request);
                bool firstPage = requests.Count == 1;
                bool hasMore = acrossPages && firstPage || hasContinuation;
                GlobalAdministratorsSnapshot page = Page(
                    ["private-admin"],
                    hasMore ? firstPage ? "opaque/page-two+" : "must-not-follow" : null,
                    hasMore,
                    request: request) with
                {
                    Rows = acrossPages
                        ? firstPage ? [CurrentRow("unrelated-admin"), first] : [second]
                        : [CurrentRow("unrelated-admin"), first, second],
                };
                return Task.FromResult(page);
            });

        GlobalAdministratorsSnapshot result = await GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            new GlobalAdministratorsRequest(PageSize: 7, ETag: "initial-validator"),
            TestContext.Current.CancellationToken);

        AssertCanonicalIncomplete(result);
        result.Reason.ShouldBe(GlobalAdministratorsReason.GatewayFailure);
        requests.Count.ShouldBe(acrossPages && !conflictFirst ? 2 : 1);
        requests[0].ShouldBe(new GlobalAdministratorsRequest(PageSize: 7, ETag: "initial-validator"));
        if (requests.Count == 2)
        {
            requests[1].ShouldBe(new GlobalAdministratorsRequest("opaque/page-two+", PageSize: 7));
        }
    }

    /// <summary>Supplies every non-current row state with both orderings, page locations and continuations.</summary>
    /// <returns>The conflicting row states and cursor-walk arrangements.</returns>
    public static IEnumerable<object[]> ConflictingDuplicateEvidenceCases()
    {
        IEnumerable<(ReadModelFreshnessState Freshness, ProjectionLifecycleState Lifecycle)> conflicts =
            Enum.GetValues<ReadModelFreshnessState>()
                .Where(static state => state is not ReadModelFreshnessState.Current)
                .Select(static state => (Freshness: state, Lifecycle: ProjectionLifecycleState.Current))
                .Concat(Enum.GetValues<ProjectionLifecycleState>()
                    .Where(static state => state is not ProjectionLifecycleState.Current)
                    .Select(static state => (Freshness: ReadModelFreshnessState.Current, Lifecycle: state)));
        foreach ((ReadModelFreshnessState freshness, ProjectionLifecycleState lifecycle) in conflicts)
        {
            foreach (bool acrossPages in new[] { false, true })
            {
                foreach (bool conflictFirst in new[] { false, true })
                {
                    foreach (bool hasContinuation in new[] { false, true })
                    {
                        yield return [freshness, lifecycle, acrossPages, conflictFirst, hasContinuation];
                    }
                }
            }
        }
    }

    private static void AssertCanonicalIncomplete(GlobalAdministratorsSnapshot result)
    {
        result.Kind.ShouldBe(GlobalAdministratorsSurfaceKind.Unavailable);
        result.Rows.ShouldBeEmpty();
        result.IsCompleteEvidence.ShouldBeFalse();
        result.HasMore.ShouldBeFalse();
        result.NextCursor.ShouldBeNull();
        result.ETag.ShouldBeNull();
        result.ProjectionVersion.ShouldBeNull();
        result.RequestCursor.ShouldBeNull();
        result.CallerScope.ShouldBeNull();
        result.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
        result.Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
        result.IsAuthorizationScopedEmpty.ShouldBeFalse();
        result.PagingRecovered.ShouldBeFalse();
        result.RequestPageSize.ShouldBe(new GlobalAdministratorsRequest().PageSize);
    }

    private static GlobalAdministratorsSnapshot Page(
        IReadOnlyList<string> userIds,
        string? nextCursor,
        bool hasMore,
        string? eTag = "etag",
        string projectionVersion = "v1",
        GlobalAdministratorsRequest? request = null)
        => GlobalAdministratorsSnapshot.Ready(
            userIds.Select(static userId => new GlobalAdministratorRow(
                userId,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current)).ToArray(),
            nextCursor,
            hasMore,
            eTag,
            ReadModelFreshnessState.Current) with
        {
            CallerScope = TenantQueryGateway.AuditCallerScope("operator-user"),
            Lifecycle = ProjectionLifecycleState.Current,
            ProjectionVersion = projectionVersion,
            RequestCursor = request?.Cursor,
            RequestPageSize = request?.PageSize ?? 20,
        };

    private static GlobalAdministratorRow CurrentRow(string userId)
        => new(userId, ReadModelFreshnessState.Current, ProjectionLifecycleState.Current);
}
