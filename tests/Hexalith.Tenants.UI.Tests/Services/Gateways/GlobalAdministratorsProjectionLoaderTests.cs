using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed class GlobalAdministratorsProjectionLoaderTests
{
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

    [Fact]
    public async Task LoadAsyncPropagatesCancellationBeforeStartingAnotherPage()
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
                return Task.FromResult(Page(["admin-a"], "page-2", hasMore: true, request: request));
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
        result.ToString().ShouldNotContain("sensitive-admin");
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
            Lifecycle = ProjectionLifecycleState.Current,
            ProjectionVersion = projectionVersion,
            RequestCursor = request?.Cursor,
            RequestPageSize = request?.PageSize ?? 20,
        };
}
