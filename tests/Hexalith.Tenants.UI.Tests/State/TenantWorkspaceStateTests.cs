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
        state.Cursor.ShouldBeNull();
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
    public void Active_search_rejects_any_incoming_cursor_and_keeps_it_out_of_the_canonical_url()
    {
        // Search paging is protected and server-held; the canonical URL owns only ordinary-list cursors.
        TenantWorkspaceState state = TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.TenantsTab,
            scope: TenantWorkspaceState.AllScope,
            userId: null,
            search: "needle",
            status: TenantStatus.Active.ToString(),
            sort: TenantListSortColumns.Name,
            sortDescending: "true",
            cursor: "opaque-ordinary-cursor",
            selectedTenantId: "tenant.alpha",
            anchor: "tenant-row-tenant.alpha");

        state.Search.ShouldBe("needle");
        state.Cursor.ShouldBeNull();

        string canonicalUrl = state.ToCanonicalUrl();
        canonicalUrl.ShouldContain("search=needle");
        canonicalUrl.ShouldNotContain("cursor", Case.Insensitive);
        canonicalUrl.ShouldNotContain("opaque-ordinary-cursor", Case.Sensitive);

        // Clearing the search term restores ordinary cursor retention for the same surface.
        TenantWorkspaceState ordinary = TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.TenantsTab,
            scope: TenantWorkspaceState.AllScope,
            userId: null,
            search: null,
            status: TenantStatus.Active.ToString(),
            sort: TenantListSortColumns.Name,
            sortDescending: "true",
            cursor: "opaque-ordinary-cursor",
            selectedTenantId: null,
            anchor: null);

        ordinary.Cursor.ShouldBe("opaque-ordinary-cursor");
        ordinary.ToCanonicalUrl().ShouldContain("cursor=opaque-ordinary-cursor");
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
        state.WithSort(state.Sort, descending: false).Cursor.ShouldBeNull();
        state.WithScope(TenantWorkspaceState.MyScope).Cursor.ShouldBeNull();
        state.WithTab(TenantWorkspaceState.UsersTab).Cursor.ShouldBeNull();

        TenantWorkspaceState users = TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.UsersTab,
            scope: null,
            userId: "user.one",
            search: null,
            status: null,
            sort: null,
            sortDescending: null,
            cursor: "user-cursor",
            selectedTenantId: null,
            anchor: null);
        users.WithUserId("user.two").Cursor.ShouldBeNull();
    }

    [Fact]
    public void Search_and_identifier_text_are_bounded_and_trimmed_like_opaque_cursors()
    {
        string search = new('s', 512);
        string cursor = new('c', 4096);
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

        // The search term is no longer unbounded. It reaches the canonical URL on every debounced
        // keystroke, the Memories query string, and the hashed cursor scope, so a 512-char term is
        // rejected exactly as an over-long user id or cursor is.
        state.Search.ShouldBeNull();
        state.Cursor.ShouldBeNull();
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

        // User identifiers now honor a 256-char cap and are trimmed for query/URL safety, so this 512-char id is rejected.
        users.UserId.ShouldBeNull();
        TenantWorkspaceState.NormalizeUserId("  user.target  ").ShouldBe("user.target");
        TenantWorkspaceState.NormalizeUserId(new string('u', 256)).ShouldNotBeNull();
        TenantWorkspaceState.NormalizeUserId(new string('u', 257)).ShouldBeNull();

        TenantWorkspaceState.NormalizeCursor(new string('c', 4097)).ShouldBeNull();

        // Trimming matters beyond tidiness: the term is one of the seven fields hashed into the protected
        // cursor scope, so an untrimmed trailing space minted a different scope and silently restarted
        // paging at page one.
        TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.TenantsTab,
            scope: TenantWorkspaceState.AllScope,
            userId: null,
            search: "  acme  ",
            status: null,
            sort: null,
            sortDescending: null,
            cursor: null,
            selectedTenantId: null,
            anchor: null).Search.ShouldBe("acme");

        TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.TenantsTab,
            scope: TenantWorkspaceState.AllScope,
            userId: null,
            search: new string('s', 256),
            status: null,
            sort: null,
            sortDescending: null,
            cursor: null,
            selectedTenantId: null,
            anchor: null).Search.ShouldNotBeNull();
    }

    [Fact]
    public void Descending_tenant_id_sort_is_normalized_away_without_a_grid_interaction()
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
        state.SortDescending.ShouldBeFalse();
        state.ToCanonicalUrl().ShouldBe("/tenants");
    }

    [Fact]
    public void Cursor_is_dropped_when_active_query_identity_is_missing_invalid_or_normalized()
    {
        TenantWorkspaceState missingUser = TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.UsersTab,
            scope: null,
            userId: null,
            search: null,
            status: null,
            sort: null,
            sortDescending: null,
            cursor: "opaque-cursor",
            selectedTenantId: null,
            anchor: null);
        missingUser.Cursor.ShouldBeNull();

        TenantWorkspaceState invalidSort = TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.TenantsTab,
            scope: TenantWorkspaceState.AllScope,
            userId: null,
            search: "alpha",
            status: null,
            sort: "unsupported",
            sortDescending: null,
            cursor: "opaque-cursor",
            selectedTenantId: null,
            anchor: null);
        invalidSort.Cursor.ShouldBeNull();

        TenantWorkspaceState normalizedStatus = TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.TenantsTab,
            scope: TenantWorkspaceState.AllScope,
            userId: null,
            search: "alpha",
            status: "active",
            sort: TenantListSortColumns.Name,
            sortDescending: bool.FalseString,
            cursor: "opaque-cursor",
            selectedTenantId: null,
            anchor: null);
        normalizedStatus.Cursor.ShouldBeNull();

        TenantWorkspaceState valid = TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.TenantsTab,
            scope: TenantWorkspaceState.AllScope,
            userId: null,
            search: "alpha",
            status: TenantStatus.Active.ToString(),
            sort: TenantListSortColumns.Name,
            sortDescending: bool.FalseString,
            cursor: "opaque-cursor",
            selectedTenantId: null,
            anchor: null);
        valid.Cursor.ShouldBeNull();
        valid.ToCanonicalUrl().ShouldNotContain("cursor=", Case.Sensitive);
    }

    [Fact]
    public void My_scope_carries_selection_and_anchor_return_context_and_round_trips_through_the_canonical_url()
    {
        // Story 1.4 AC5: returning from a My Tenants detail drill-in restores scope=mine plus selection and
        // the return-focus anchor. The MyScope branch now carries selected/anchor (previously dropped) and
        // the canonical URL emits them, so a redirect on return does not strip the selection.
        TenantWorkspaceState state = TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.TenantsTab,
            scope: TenantWorkspaceState.MyScope,
            userId: null,
            search: null,
            status: null,
            sort: null,
            sortDescending: null,
            cursor: null,
            selectedTenantId: "tenant.alpha",
            anchor: "tenants-my-row-tenant.alpha");

        state.Scope.ShouldBe(TenantWorkspaceState.MyScope);
        state.SelectedTenantId.ShouldBe("tenant.alpha");
        state.Anchor.ShouldBe("tenants-my-row-tenant.alpha");
        state.ToCanonicalUrl().ShouldBe(
            "/tenants?tab=tenants&scope=mine&selected=tenant.alpha&anchor=tenants-my-row-tenant.alpha");
    }

    [Fact]
    public void My_scope_detail_drill_in_resets_cursor_and_preserves_the_authorized_first_page_return()
    {
        // The shared navigation context builds the /tenants/{id} detail URL for a self-audit row. The return
        // URL carries scope=mine + selection + anchor and resets the cursor to the authorized first page,
        // mirroring the scope=all list detail behavior.
        TenantWorkspaceState myScope = TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.TenantsTab,
            scope: TenantWorkspaceState.MyScope,
            userId: null,
            search: null,
            status: null,
            sort: null,
            sortDescending: null,
            cursor: "opaque-cursor",
            selectedTenantId: null,
            anchor: null);

        string detailUrl = new TenantListNavigationContext(myScope)
            .ToDetailUrl("tenant.alpha", "tenants-my-row-tenant.alpha");

        detailUrl.ShouldBe(
            "/tenants/tenant.alpha?returnUrl="
            + Uri.EscapeDataString("/tenants?tab=tenants&scope=mine&selected=tenant.alpha&anchor=tenants-my-row-tenant.alpha"));
    }

    [Fact]
    public void My_scope_query_changing_transitions_null_stale_selection_and_anchor()
    {
        TenantWorkspaceState state = TenantWorkspaceState.FromQuery(
            tab: TenantWorkspaceState.TenantsTab,
            scope: TenantWorkspaceState.MyScope,
            userId: null,
            search: null,
            status: null,
            sort: null,
            sortDescending: null,
            cursor: null,
            selectedTenantId: "tenant.alpha",
            anchor: "tenants-my-row-tenant.alpha");

        // Switching scope (a query-changing transition) must drop the stale selection/anchor and cursor.
        TenantWorkspaceState allScope = state.WithScope(TenantWorkspaceState.AllScope);
        allScope.SelectedTenantId.ShouldBeNull();
        allScope.Anchor.ShouldBeNull();
        allScope.Cursor.ShouldBeNull();
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
            "/tenants?search=alpha%20beta&status=Disabled&sort=name&desc=True&selected=tenant.alpha&anchor=tenant-row-tenant.alpha");

        TenantWorkspaceState users = state.WithTab(TenantWorkspaceState.UsersTab).WithUserId("user/target");
        users.ToCanonicalUrl().ShouldBe("/tenants?tab=users&userId=user%2Ftarget&sort=name");
    }
}
