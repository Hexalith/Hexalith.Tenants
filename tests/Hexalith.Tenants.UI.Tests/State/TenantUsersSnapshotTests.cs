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
}
