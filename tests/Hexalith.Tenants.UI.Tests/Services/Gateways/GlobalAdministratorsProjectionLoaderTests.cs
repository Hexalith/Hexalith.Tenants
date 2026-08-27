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
    public async Task LoadAsync_aggregates_pages_deduplicates_ids_and_forwards_opaque_cursors()
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
                    ? Page(["admin-a", "duplicate"], "opaque cursor/+", hasMore: true, eTag: "etag-page-1")
                    : Page(["duplicate", "admin-b"], null, hasMore: false, eTag: "etag-page-2"));
            });

        GlobalAdministratorsSnapshot result = await GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            new GlobalAdministratorsRequest(PageSize: 7, ETag: "etag-page-1"),
            TestContext.Current.CancellationToken);

        result.IsCompleteEvidence.ShouldBeTrue();
        result.HasMore.ShouldBeFalse();
        result.NextCursor.ShouldBeNull();
        result.ETag.ShouldBeNull();
        result.Rows.Select(static row => row.UserId).ShouldBe(["admin-a", "duplicate", "admin-b"]);
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
    public async Task LoadAsync_rejects_non_current_or_version_inconsistent_pages(string scenario)
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                GlobalAdministratorsSnapshot page = Page([], calls == 1 ? "page-2" : null, calls == 1);
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
    public async Task LoadAsync_rejects_gateway_page_one_recovery()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(++calls == 1
                ? Page(["admin-a"], "page-2", hasMore: true)
                : Page(["admin-a"], null, hasMore: false) with { PagingRecovered = true }));

        GlobalAdministratorsSnapshot result = await GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsCompleteEvidence.ShouldBeFalse();
        result.PagingRecovered.ShouldBeTrue();
        calls.ShouldBe(2);
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData("", 1)]
    [InlineData("cycle", 2)]
    public async Task LoadAsync_rejects_missing_or_repeated_continuations(string? continuation, int expectedCalls)
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(Page(
                [],
                continuation == "cycle" ? "cycle" : continuation,
                hasMore: true)));

        GlobalAdministratorsSnapshot result = await GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            cancellationToken: TestContext.Current.CancellationToken);

        result.IsCompleteEvidence.ShouldBeFalse();
        result.HasMore.ShouldBeTrue();
        result.NextCursor.ShouldBeNull();
        calls.ShouldBe(expectedCalls);
    }

    [Fact]
    public async Task LoadAsync_stops_at_page_cap()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(Page([], $"page-{++calls}", hasMore: true)));

        GlobalAdministratorsSnapshot result = await GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            cancellationToken: TestContext.Current.CancellationToken,
            maximumPageCount: 3);

        calls.ShouldBe(3);
        result.IsCompleteEvidence.ShouldBeFalse();
        result.HasMore.ShouldBeTrue();
    }

    [Fact]
    public async Task LoadAsync_propagates_cancellation_before_starting_another_page()
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        int calls = 0;
        gateway.GetGlobalAdministratorsAsync(
                Arg.Any<GlobalAdministratorsRequest>(),
                Arg.Any<GlobalAdministratorsSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                calls++;
                source.Cancel();
                return Task.FromResult(Page([], "page-2", hasMore: true));
            });

        await Should.ThrowAsync<OperationCanceledException>(() => GlobalAdministratorsProjectionLoader.LoadAsync(
            gateway,
            cancellationToken: source.Token));

        calls.ShouldBe(1);
    }

    private static GlobalAdministratorsSnapshot Page(
        IReadOnlyList<string> userIds,
        string? nextCursor,
        bool hasMore,
        string? eTag = "etag",
        string projectionVersion = "v1")
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
        };
}
