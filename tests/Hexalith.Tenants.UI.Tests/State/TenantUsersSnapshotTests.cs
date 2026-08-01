using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantUsers;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantUsersSnapshotTests
{
    [Fact]
    public void Factory_matrix_covers_every_non_collapsing_surface_kind()
    {
        TenantUsersSnapshot current = TenantUsersSnapshot.Ready(
            "tenant.alpha",
            [new TenantMember("user.alpha", TenantRole.TenantReader)],
            nextCursor: null,
            hasMore: false,
            eTag: "etag-current",
            projectionVersion: "version-current",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current);
        Dictionary<TenantUsersSurfaceKind, TenantUsersSnapshot> snapshots = new()
        {
            [TenantUsersSurfaceKind.Loading] = TenantUsersSnapshot.Loading("tenant.alpha"),
            [TenantUsersSurfaceKind.Ready] = current,
            [TenantUsersSurfaceKind.Empty] = TenantUsersSnapshot.Empty(
                "tenant.alpha",
                isAuthorizationScoped: true,
                eTag: "etag-empty",
                projectionVersion: "version-empty",
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current),
            [TenantUsersSurfaceKind.Stale] = TenantUsersSnapshot.Ready(
                "tenant.alpha",
                current.Rows,
                nextCursor: null,
                hasMore: false,
                eTag: "etag-stale",
                projectionVersion: "version-stale",
                ReadModelFreshnessState.Stale,
                ProjectionLifecycleState.Stale),
            [TenantUsersSurfaceKind.Degraded] = TenantUsersSnapshot.Degraded(
                "tenant.alpha",
                current,
                TenantUsersReason.ProjectionDegraded),
            [TenantUsersSurfaceKind.Unknown] = TenantUsersSnapshot.Ready(
                "tenant.alpha",
                current.Rows,
                nextCursor: null,
                hasMore: false,
                eTag: "etag-unknown",
                projectionVersion: "version-unknown",
                ReadModelFreshnessState.Unknown,
                ProjectionLifecycleState.Unknown),
            [TenantUsersSurfaceKind.Unauthorized] = TenantUsersSnapshot.Unauthorized("tenant.alpha"),
            [TenantUsersSurfaceKind.NotFound] = TenantUsersSnapshot.NotFound("tenant.alpha"),
            [TenantUsersSurfaceKind.Invalid] = TenantUsersSnapshot.Invalid("tenant.alpha"),
            [TenantUsersSurfaceKind.Unavailable] = TenantUsersSnapshot.Unavailable("tenant.alpha"),
            [TenantUsersSurfaceKind.Error] = TenantUsersSnapshot.Error("tenant.alpha"),
        };

        snapshots.Keys.ShouldBe(Enum.GetValues<TenantUsersSurfaceKind>(), ignoreOrder: true);
        snapshots.ShouldAllBe(pair => pair.Key == pair.Value.Kind);
        snapshots[TenantUsersSurfaceKind.Empty].IsAuthorizationScopedEmpty.ShouldBeTrue();
        snapshots[TenantUsersSurfaceKind.Degraded].Rows.ShouldBeSameAs(current.Rows);
        snapshots[TenantUsersSurfaceKind.Stale].Reason.ShouldBe(TenantUsersReason.ProjectionStale);
        snapshots[TenantUsersSurfaceKind.Unauthorized].Reason.ShouldBe(TenantUsersReason.Unauthorized);
        snapshots[TenantUsersSurfaceKind.NotFound].Reason.ShouldBe(TenantUsersReason.NotFound);
        snapshots[TenantUsersSurfaceKind.Invalid].Reason.ShouldBe(TenantUsersReason.InvalidCursor);
        snapshots[TenantUsersSurfaceKind.Unavailable].Reason.ShouldBe(TenantUsersReason.GatewayUnavailable);
        snapshots[TenantUsersSurfaceKind.Error].Reason.ShouldBe(TenantUsersReason.GatewayFailure);
    }

    [Fact]
    public void Every_reason_has_a_support_safe_diagnostic_representation()
    {
        foreach (TenantUsersReason reason in Enum.GetValues<TenantUsersReason>())
        {
            TenantUsersSnapshot snapshot = TenantUsersSnapshot.Loading("tenant.secret") with { Reason = reason };

            snapshot.Reason.ShouldBe(reason);
            snapshot.ToString().ShouldBe(
                $"TenantUsersSnapshot {{ Kind = Loading, RowCount = 0, HasMore = False, Freshness = Unknown, "
                + $"Lifecycle = Unknown, IsAuthorizationScopedEmpty = False, Reason = {reason}, IsRefreshing = False, "
                + $"PagingRecovered = False }}");
        }
    }

    [Theory]
    [InlineData(false, TenantUsersSurfaceKind.Degraded, TenantUsersReason.ProjectionDegraded)]
    [InlineData(true, TenantUsersSurfaceKind.Error, TenantUsersReason.GatewayFailure)]
    public void Retention_rejects_a_previous_snapshot_from_another_tenant(
        bool isError,
        TenantUsersSurfaceKind expectedKind,
        TenantUsersReason expectedReason)
    {
        TenantUsersSnapshot previous = TenantUsersSnapshot.Ready(
            "tenant.previous",
            [new TenantMember("user.secret", TenantRole.TenantOwner)],
            "cursor-secret",
            hasMore: true,
            "etag-secret",
            "version-secret",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current);

        TenantUsersSnapshot snapshot = isError
            ? TenantUsersSnapshot.Error("tenant.requested", previous)
            : TenantUsersSnapshot.Degraded("tenant.requested", previous, TenantUsersReason.ProjectionDegraded);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.TenantId.ShouldBe("tenant.requested");
        snapshot.Reason.ShouldBe(expectedReason);
        snapshot.Rows.ShouldBeEmpty();
        snapshot.NextCursor.ShouldBeNull();
        snapshot.ETag.ShouldBeNull();
        snapshot.ProjectionVersion.ShouldBeNull();
    }

    [Fact]
    public void Refreshing_retains_last_confirmed_members_and_independent_metadata()
    {
        TenantUsersSnapshot ready = TenantUsersSnapshot.Ready(
            "tenant.alpha",
            [new TenantMember("user.alpha", TenantRole.TenantOwner)],
            "next-secret",
            hasMore: true,
            "etag-secret",
            "version-secret",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current);

        TenantUsersSnapshot refreshing = TenantUsersSnapshot.Refreshing(ready);

        refreshing.IsRefreshing.ShouldBeTrue();
        refreshing.Rows.ShouldBeSameAs(ready.Rows);
        refreshing.ETag.ShouldBe("etag-secret");
        refreshing.ProjectionVersion.ShouldBe("version-secret");
        refreshing.Freshness.ShouldBe(ReadModelFreshnessState.Current);
        refreshing.Lifecycle.ShouldBe(ProjectionLifecycleState.Current);
    }

    [Fact]
    public void Diagnostics_omit_literal_scope_cursor_etag_and_projection_version()
    {
        TenantUsersRequest request = new("tenant.alpha", "cursor-secret", 20, "etag-secret");
        TenantUsersSnapshot snapshot = TenantUsersSnapshot.Ready(
            "tenant.alpha",
            [new TenantMember("user.alpha", TenantRole.TenantReader)],
            "cursor-secret",
            hasMore: true,
            "etag-secret",
            "version-secret",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current);

        string diagnostic = $"{request} {snapshot}";

        diagnostic.ShouldNotContain("tenant.alpha", Case.Sensitive);
        diagnostic.ShouldNotContain("user.alpha", Case.Sensitive);
        diagnostic.ShouldNotContain("cursor-secret", Case.Sensitive);
        diagnostic.ShouldNotContain("etag-secret", Case.Sensitive);
        diagnostic.ShouldNotContain("version-secret", Case.Sensitive);
    }

    [Fact]
    public void Degraded_retention_clears_paging_recovered_so_a_failed_read_is_not_announced_as_a_restart()
    {
        TenantUsersSnapshot recovered = TenantUsersSnapshot.Ready(
            "tenant.alpha",
            [new TenantMember("user.alpha", TenantRole.TenantReader)],
            nextCursor: null,
            hasMore: false,
            "\"etag\"",
            "version-1",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current) with
        {
            PagingRecovered = true,
            Reason = TenantUsersReason.ListRefreshed,
        };

        TenantUsersSnapshot degraded = TenantUsersSnapshot.Degraded(
            "tenant.alpha",
            recovered,
            TenantUsersReason.GatewayUnavailable);

        degraded.Rows.ShouldBeSameAs(recovered.Rows);
        degraded.Kind.ShouldBe(TenantUsersSurfaceKind.Degraded);
        degraded.PagingRecovered.ShouldBeFalse();

        // The retained rows come from a read that then FAILED, so the evidence that described them can no
        // longer be asserted. No test observed these two, so both downgrade lines were deletable --
        // MemberAccessReview binds Members.Lifecycle and Members.Freshness straight into its badges, so a
        // page retained from a failed read would render a green "Current" projection-lifecycle badge over
        // data the gateway could not re-verify.
        degraded.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
        degraded.Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
    }

    [Fact]
    public void Refreshing_retention_keeps_paging_recovered_because_the_rows_are_still_the_recovered_page()
    {
        TenantUsersSnapshot recovered = TenantUsersSnapshot.Ready(
            "tenant.alpha",
            [new TenantMember("user.alpha", TenantRole.TenantReader)],
            nextCursor: null,
            hasMore: false,
            "\"etag\"",
            "version-1",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current) with
        {
            PagingRecovered = true,
        };

        TenantUsersSnapshot.Refreshing(recovered).PagingRecovered.ShouldBeTrue();
    }

    [Fact]
    public void Error_retention_also_clears_paging_recovered()
    {
        TenantUsersSnapshot recovered = TenantUsersSnapshot.Ready(
            "tenant.alpha",
            [new TenantMember("user.alpha", TenantRole.TenantReader)],
            nextCursor: null,
            hasMore: false,
            "\"etag\"",
            "version-1",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current) with
        {
            PagingRecovered = true,
        };

        TenantUsersSnapshot.Error("tenant.alpha", recovered).PagingRecovered.ShouldBeFalse();
    }

    [Theory]
    [InlineData(ReadModelFreshnessState.Current, TenantUsersSurfaceKind.Empty)]
    [InlineData(ReadModelFreshnessState.Stale, TenantUsersSurfaceKind.Stale)]
    [InlineData(ReadModelFreshnessState.Unknown, TenantUsersSurfaceKind.Unknown)]
    public void Authorization_scoped_empty_survives_every_freshness(
        ReadModelFreshnessState freshness,
        TenantUsersSurfaceKind expectedKind)
    {
        // The kind is the freshness channel, matching Ready. IsAuthorizationScopedEmpty is the separate
        // absence channel and must survive intact, or a successful authorized-empty page at stale/unknown
        // freshness renders as a failed read.
        TenantUsersSnapshot snapshot = TenantUsersSnapshot.Empty(
            "tenant.alpha",
            isAuthorizationScoped: true,
            "\"etag\"",
            "version-1",
            freshness,
            ProjectionLifecycleState.Current);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.IsAuthorizationScopedEmpty.ShouldBeTrue();
        snapshot.Rows.ShouldBeEmpty();
    }

    [Fact]
    public void Failure_states_are_never_authorization_scoped_empty()
    {
        // The component keys its authorization-safe absence copy on this flag, so every failure factory
        // must leave it false or a failed read would render as "no visible members".
        TenantUsersSnapshot.Unauthorized("tenant.alpha").IsAuthorizationScopedEmpty.ShouldBeFalse();
        TenantUsersSnapshot.NotFound("tenant.alpha").IsAuthorizationScopedEmpty.ShouldBeFalse();
        TenantUsersSnapshot.Invalid("tenant.alpha").IsAuthorizationScopedEmpty.ShouldBeFalse();
        TenantUsersSnapshot.Unavailable("tenant.alpha").IsAuthorizationScopedEmpty.ShouldBeFalse();
        TenantUsersSnapshot.Error("tenant.alpha").IsAuthorizationScopedEmpty.ShouldBeFalse();
        TenantUsersSnapshot.Loading("tenant.alpha").IsAuthorizationScopedEmpty.ShouldBeFalse();
        TenantUsersSnapshot.Degraded("tenant.alpha", null, TenantUsersReason.GatewayFailure)
            .IsAuthorizationScopedEmpty.ShouldBeFalse();
    }
}
