using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.TenantList;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantWorkspaceStateTests
{
    [Fact]
    public void Query_state_normalizes_to_the_active_surface_and_drops_foreign_values()
    {
        TenantWorkspaceState state = TenantWorkspaceState.FromQuery(
            tab: "users",
            scope: "mine",
            userId: " user.target ",
            search: "tenant",
            status: TenantStatus.Active.ToString(),
            sort: TenantListSortColumns.Name,
            sortDescending: "true",
            cursor: "lookup-cursor",
            selectedTenantId: "tenant.alpha",
            anchor: "tenant-row-tenant.alpha");

        state.Tab.ShouldBe(TenantWorkspaceState.UsersTab);
        state.Scope.ShouldBe(TenantWorkspaceState.AllScope);
        state.UserId.ShouldBe("user.target");
        state.Search.ShouldBeNull();
        state.Status.ShouldBeNull();
        state.Sort.ShouldBe(UserTenantMembershipSortColumns.Name);
        state.SortDescending.ShouldBeFalse();
        state.Cursor.ShouldBe("lookup-cursor");
        state.SelectedTenantId.ShouldBeNull();
        state.Anchor.ShouldBeNull();
    }

    [Theory]
    [InlineData(null, null, null, null, null, null, null, null)]
    [InlineData("invalid", "invalid", "\u0001", "\u0001", "Bogus", "Bogus", "not-bool", "\u0001")]
    public void Query_state_is_fail_safe_for_invalid_or_surface_inapplicable_values(
        string? tab,
        string? scope,
        string? userId,
        string? search,
        string? status,
        string? sort,
        string? sortDescending,
        string? cursor)
    {
        TenantWorkspaceState state = TenantWorkspaceState.FromQuery(
            tab,
            scope,
            userId,
            search,
            status,
            sort,
            sortDescending,
            cursor,
            selectedTenantId: null,
            anchor: null);

        state.Tab.ShouldBe(TenantWorkspaceState.TenantsTab);
        state.Scope.ShouldBe(TenantWorkspaceState.AllScope);
        state.UserId.ShouldBeNull();
        state.Search.ShouldBeNull();
        state.Status.ShouldBeNull();
        state.Sort.ShouldBe(TenantListSortColumns.TenantId);
        state.SortDescending.ShouldBeFalse();
        state.Cursor.ShouldBe(cursor is "cursor" ? "cursor" : null);
    }

    [Fact]
    public void State_transitions_reset_cursor_when_query_identity_changes()
    {
        TenantWorkspaceState state = TenantWorkspaceState.FromQuery(
            tab: "tenants",
            scope: "all",
            userId: null,
            search: "alpha",
            status: TenantStatus.Active.ToString(),
            sort: TenantListSortColumns.Name,
            sortDescending: "true",
            cursor: "opaque-cursor",
            selectedTenantId: "tenant.alpha",
            anchor: "tenant-row-tenant.alpha");

        state.WithSearch("beta").Cursor.ShouldBeNull();
        state.WithStatus(TenantStatus.Disabled.ToString()).Cursor.ShouldBeNull();
        state.WithSort(TenantListSortColumns.Status, descending: false).Cursor.ShouldBeNull();
        state.WithScope(TenantWorkspaceState.MyScope).Cursor.ShouldBeNull();
        state.WithTab(TenantWorkspaceState.UsersTab).Cursor.ShouldBeNull();
    }

    [Fact]
    public void Search_and_opaque_context_values_are_not_truncated_without_an_authoritative_limit()
    {
        string search = new('s', 512);
        string cursor = new('c', 8192);
        string selectedTenantId = new('t', 512);
        string anchor = new('a', 512);

        TenantWorkspaceState state = TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.TenantsTab,
            scope: TenantWorkspaceState.AllScope,
            userId: null,
            search,
            status: null,
            sort: null,
            sortDescending: null,
            cursor,
            selectedTenantId,
            anchor);

        state.Search.ShouldBe(search);
        state.Cursor.ShouldBe(cursor);
        state.SelectedTenantId.ShouldBe(selectedTenantId);
        state.Anchor.ShouldBe(anchor);

        string userId = new('u', 512);
        TenantWorkspaceState users = TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.UsersTab,
            scope: null,
            userId,
            search: null,
            status: null,
            sort: null,
            sortDescending: null,
            cursor: null,
            selectedTenantId: null,
            anchor: null);

        users.UserId.ShouldBe(userId);
    }

    [Fact]
    public void Descending_tenant_id_sort_round_trips_canonically()
    {
        TenantWorkspaceState state = TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.TenantsTab,
            scope: TenantWorkspaceState.AllScope,
            userId: null,
            search: null,
            status: null,
            sort: TenantListSortColumns.TenantId,
            sortDescending: bool.TrueString,
            cursor: null,
            selectedTenantId: null,
            anchor: null);

        state.Sort.ShouldBe(TenantListSortColumns.TenantId);
        state.SortDescending.ShouldBeTrue();
        state.ToCanonicalUrl().ShouldBe("/tenants?desc=True");
    }

    [Fact]
    public void Canonical_urls_are_deterministic_and_compatibility_safe()
    {
        TenantWorkspaceState state = TenantWorkspaceState.FromQuery(
            tab: "tenants",
            scope: "all",
            userId: null,
            search: "alpha beta",
            status: TenantStatus.Disabled.ToString(),
            sort: TenantListSortColumns.Name,
            sortDescending: "true",
            cursor: "opaque-cursor",
            selectedTenantId: "tenant.alpha",
            anchor: "tenant-row-tenant.alpha");

        state.ToCanonicalUrl().ShouldBe(
            "/tenants?search=alpha%20beta&status=Disabled&sort=name&desc=True&cursor=opaque-cursor&selected=tenant.alpha&anchor=tenant-row-tenant.alpha");

        TenantWorkspaceState users = state.WithTab(TenantWorkspaceState.UsersTab).WithUserId("user/target");
        users.ToCanonicalUrl().ShouldBe("/tenants?tab=users&userId=user%2Ftarget&sort=name");
    }
}
