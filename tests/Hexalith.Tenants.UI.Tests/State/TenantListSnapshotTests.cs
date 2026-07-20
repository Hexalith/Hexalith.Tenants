using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantList;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantListSnapshotTests
{
    [Fact]
    public void Error_and_degraded_outcomes_carry_only_typed_safe_reasons()
    {
        TenantListSnapshot error = TenantListSnapshot.Error();
        TenantListSnapshot degraded = TenantListSnapshot.Degraded(
            [],
            TenantListReason.RowEnrichmentUnavailable);

        error.Kind.ShouldBe(TenantListSurfaceKind.Error);
        error.Reason.ShouldBe(TenantListReason.GatewayUnavailable);
        degraded.Kind.ShouldBe(TenantListSurfaceKind.Degraded);
        degraded.Reason.ShouldBe(TenantListReason.RowEnrichmentUnavailable);
        typeof(TenantListSnapshot).GetProperty("ErrorMessage").ShouldBeNull();
    }

    [Fact]
    public void Summary_mapping_preserves_literal_identity_and_unknown_truth()
    {
        TenantListRow row = TenantListRow.FromSummary(
            new TenantSummary("customer/West EU:01", "West Europe", TenantStatus.Active));

        row.TenantId.ShouldBe("customer/West EU:01");
        row.MemberCount.IsKnown.ShouldBeFalse();
        row.OwnerCount.IsKnown.ShouldBeFalse();
        row.PendingState.ShouldBe(TenantPendingState.None);
        row.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Fact]
    public void Ready_snapshot_preserves_row_bound_pending_and_freshness_values()
    {
        TenantListRow row = TenantListRow.FromSummary(
            new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)) with
        {
            PendingState = TenantPendingState.Unknown,
            Freshness = ReadModelFreshnessState.Stale,
        };

        TenantListSnapshot snapshot = TenantListSnapshot.Ready(
            [row],
            nextCursor: null,
            hasMore: false,
            eTag: null,
            freshness: ReadModelFreshnessState.Stale,
            isDegraded: false);

        TenantListRow retained = snapshot.Rows.ShouldHaveSingleItem();
        retained.TenantId.ShouldBe("tenant.alpha");
        retained.PendingState.ShouldBe(TenantPendingState.Unknown);
        retained.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
    }
}
