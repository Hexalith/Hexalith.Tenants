using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Layout;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Users;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TenantListSurfaceTests : BunitContext
{
    [Fact]
    public void Workspace_renders_grid_controls_stable_selectors_and_truth_state()
    {
        const string tenantId = "  tenant/%2F?x=é&glyph=о  ";
        TenantListSnapshot snapshot = ReadySnapshot(
            [
                Row(tenantId, "Alpha", TenantStatus.Active, ReadModelFreshnessState.Stale, TenantPendingState.None),
            ],
            nextCursor: "next-cursor",
            hasMore: true);
        RegisterServices(snapshot);
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler writeHandler = module.SetupVoid("writeText", tenantId).SetVoidResult();

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        cut.Find("[data-testid='tenants-list-refresh']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-list-search']").NodeName.ShouldBe("FLUENT-TEXT-INPUT");
        cut.Find("[data-testid='tenants-list-reset']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-list-sort-tenant']").Closest("fluent-button").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-list-sort-status']").Closest("fluent-button").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-list-detail-link']").GetAttribute("href").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-list-detail-link'] strong").TextContent.ShouldBe(tenantId);
        cut.Find("[data-testid='tenants-list-copy-reference']").GetAttribute("data-copy-kind").ShouldBe("TenantId");
        cut.Find("[data-testid='tenants-list-copy-reference']").TextContent.ShouldContain("Copy");
        cut.Find("[data-testid='tenants-copy-reference']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-list-truth-state']").TextContent.ShouldContain("Stale");
        cut.Markup.ShouldContain("No pending changes");

        cut.Find("[data-surface-testid='tenants-list-copy-reference']").Click();
        cut.WaitForAssertion(() => writeHandler.Invocations.Count.ShouldBe(1));
        writeHandler.Invocations.Single().Arguments[0].ShouldBe(tenantId);
    }

    [Fact]
    public void Grid_pins_all_three_safety_columns_to_logical_start()
    {
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Disabled, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)]));

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        TemplateColumn<TenantListRow>[] columns = cut.FindComponents<TemplateColumn<TenantListRow>>()
            .Select(component => component.Instance)
            .ToArray();

        columns.Single(column => column.ColumnId == "tenant-id").Pin.ShouldBe(DataGridColumnPin.Start);
        columns.Single(column => column.ColumnId == "tenant-status").Pin.ShouldBe(DataGridColumnPin.Start);
        columns.Single(column => column.ColumnId == "tenant-freshness").Pin.ShouldBe(DataGridColumnPin.Start);
        columns.Single(column => column.ColumnId == "tenant-id").Width.ShouldBe("220px");
        columns.Single(column => column.ColumnId == "tenant-status").Width.ShouldBe("130px");
        columns.Single(column => column.ColumnId == "tenant-freshness").Width.ShouldBe("160px");
        columns.Single(column => column.ColumnId == "tenant-audit").Width.ShouldBe("175px");

        FluentDataGrid<TenantListRow> grid = cut.FindComponent<FluentDataGrid<TenantListRow>>().Instance;
        TenantListRow row = Row("customer/West EU:01", "West", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None);
        grid.ItemKey.ShouldNotBeNull()(row).ShouldBe("customer/West EU:01");

        FluentBadge statusBadge = cut.FindComponents<FluentBadge>()
            .Select(component => component.Instance)
            .Single(badge => badge.Class?.Contains("tenant-data-grid__status", StringComparison.Ordinal) == true);
        statusBadge.Color.ShouldBe(BadgeColor.Severe);
        statusBadge.IconStart.ShouldNotBeNull().GetType().Name.ShouldBe("Power");
        statusBadge.IconStart.Size.ShouldBe(IconSize.Size20);
        statusBadge.IconLabel.ShouldBe("Disabled");

        FluentBadge pendingBadge = cut.FindComponents<FluentBadge>()
            .Select(component => component.Instance)
            .Single(badge => badge.Class?.Contains("tenant-data-grid__pending", StringComparison.Ordinal) == true);
        pendingBadge.Color.ShouldBe(BadgeColor.Important);
        pendingBadge.IconStart.ShouldNotBeNull().GetType().Name.ShouldBe("QuestionCircle");
        pendingBadge.IconStart.Size.ShouldBe(IconSize.Size20);
        pendingBadge.IconLabel.ShouldBe("Pending state unknown");
        cut.Find("[data-testid='tenants-list-status']").GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();
        cut.Find("[data-testid='tenants-list-pending']").GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();
        cut.FindAll("[data-testid='tenants-list-audit-entrypoint']").Count.ShouldBe(1);
    }

    [Fact]
    public void Workspace_is_composed_from_the_frontcomposer_aggregate_list_wrapper()
    {
        // cc-2026-06-21 extraction guard: the workspace reuses FcAggregateListPage<TItem> (the shared
        // FC-LST chrome) and declares the full-width measure through it, instead of a Tenants-local
        // FcPageLayout/FcPageHeader page shell. Keeps the rebase from silently regressing.
        RegisterServices(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Unknown));

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-workspace']");

        FcAggregateListPage<TenantListRow> wrapper = cut.FindComponent<FcAggregateListPage<TenantListRow>>().Instance;
        wrapper.LayoutMode.ShouldBe(FcPageLayoutMode.FullWidth);
    }

    [Fact]
    public async Task Page_local_filter_and_sort_preserve_row_bound_safety_markers()
    {
        TenantListRow alpha = Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None);
        TenantListRow beta = Row("tenant.beta", "Beta", TenantStatus.Disabled, ReadModelFreshnessState.Stale, TenantPendingState.Unknown);

        RegisterServices(ReadySnapshot([alpha, beta]));

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        await ChangeSelectAsync(cut, "tenants-list-status-filter", TenantStatus.Disabled.ToString());

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("tenant.beta");
            cut.Markup.ShouldNotContain("tenant.alpha");
        });
        cut.Find("[data-testid='tenants-list-truth-state']").TextContent.ShouldContain("Stale");
        cut.Markup.ShouldContain("Pending state unknown");

        await ChangeSelectAsync(cut, "tenants-list-status-filter", string.Empty);
        await ChangeSelectAsync(cut, "tenants-list-status-filter", TenantStatus.Active.ToString());

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("tenant.alpha");
            cut.Markup.ShouldNotContain("tenant.beta");
        });
        cut.Find("[data-testid='tenants-list-truth-state']").TextContent.ShouldContain("Current");
        cut.Markup.ShouldContain("No pending changes");

        await ChangeSelectAsync(cut, "tenants-list-status-filter", string.Empty);

        // Sorting moved from the (removed) sort/direction comboboxes to the FluentDataGrid sortable
        // column headers. The Tenant column sorts by display name; sorting it descending must place
        // "Beta" before "Alpha" while preserving the truth-state and pending safety markers.
        FluentDataGrid<TenantListRow> grid = cut.FindComponent<FluentDataGrid<TenantListRow>>().Instance;
        await cut.InvokeAsync(() => grid.SortByColumnAsync("Tenant", DataGridSortDirection.Descending));

        cut.WaitForAssertion(() =>
            cut.Markup.IndexOf("tenant.beta", StringComparison.Ordinal).ShouldBeLessThan(
                cut.Markup.IndexOf("tenant.alpha", StringComparison.Ordinal)));
        cut.Markup.ShouldContain("data-testid=\"tenants-list-truth-state\"");
        cut.Markup.ShouldContain("Pending state unknown");
    }

    [Fact]
    public async Task Search_term_round_trips_to_the_server_not_an_in_memory_filter()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            requests.Add(call.ArgAt<TenantListRequest>(0));
            return Task.FromResult(ReadySnapshot(
                [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)]));
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        await ChangeSearchAsync(cut, "acme");

        cut.WaitForAssertion(() => requests.ShouldContain(r => r.Search == "acme"));
    }

    [Fact]
    public async Task Search_change_updates_canonical_workspace_url_and_resets_cursor()
    {
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)],
            nextCursor: "next",
            hasMore: true));
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants?search=before&cursor=opaque-cursor");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        await ChangeSearchAsync(cut, "after");

        cut.WaitForAssertion(() =>
        {
            navigation.Uri.ShouldContain("search=after");
            navigation.Uri.ShouldNotContain("opaque-cursor");
        });
    }

    [Fact]
    public async Task Tenant_grid_sort_updates_canonical_workspace_url_and_resets_cursor()
    {
        RegisterServices(ReadySnapshot(
            [
                Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None),
                Row("tenant.beta", "Beta", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None),
            ],
            nextCursor: "next",
            hasMore: true));
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants?cursor=opaque-cursor");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        FluentDataGrid<TenantListRow> grid = cut.FindComponent<FluentDataGrid<TenantListRow>>().Instance;
        await cut.InvokeAsync(() => grid.SortByColumnAsync("Tenant", DataGridSortDirection.Descending));

        cut.WaitForAssertion(() =>
        {
            navigation.Uri.ShouldContain("sort=name");
            navigation.Uri.ShouldContain("desc=True");
            navigation.Uri.ShouldNotContain("opaque-cursor");
        });
    }

    [Fact]
    public async Task Query_identity_change_clears_the_previous_cursor_history()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            if (request.Search == "after")
            {
                return Task.FromResult(ReadySnapshot(
                    [Row("tenant.search", "Search", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)]));
            }

            return Task.FromResult(request.Cursor switch
            {
                null => ReadySnapshot(
                    [Row("tenant.one", "One", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)],
                    nextCursor: "cursor-two",
                    hasMore: true),
                "cursor-two" => ReadySnapshot(
                    [Row("tenant.two", "Two", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)],
                    nextCursor: "cursor-three",
                    hasMore: true),
                _ => ReadySnapshot(
                    [Row("tenant.three", "Three", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)]),
            });
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("tenant.two"));
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("tenant.three"));

        await ChangeSearchAsync(cut, "after");

        cut.WaitForAssertion(() =>
        {
            requests[^1].Search.ShouldBe("after");
            requests[^1].Cursor.ShouldBeNull();
            cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
        });
    }

    [Fact]
    public async Task Page_size_change_is_local_resets_cursor_history_and_requests_supported_size()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            return Task.FromResult(ReadySnapshot(
                [Row($"tenant.{requests.Count}", $"Tenant {requests.Count}", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None)],
                nextCursor: requests.Count == 1 ? "opaque-next-cursor" : null,
                hasMore: requests.Count == 1));
        });
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => requests[^1].Cursor.ShouldBe("opaque-next-cursor"));

        await ChangePageSizeAsync(cut, 50);

        cut.WaitForAssertion(() =>
        {
            requests[^1].PageSize.ShouldBe(50);
            requests[^1].Cursor.ShouldBeNull();
            cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
            navigation.Uri.ShouldNotContain("opaque-next-cursor");
            navigation.Uri.ShouldNotContain("pageSize", Case.Insensitive);
        });
    }

    [Fact]
    public void Page_one_recovery_clears_cursor_url_retains_rows_and_renders_polite_notice()
    {
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None)]) with
        {
            Notice = TenantListReason.ListRefreshed,
        });
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants?cursor=expired-protected-cursor");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();

        cut.WaitForElement("[data-testid='tenants-list-refreshed-notice']");
        cut.Markup.ShouldContain("tenant.alpha");
        AssertSingleNoticeLiveRegion(cut);
        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
        navigation.Uri.ShouldBe("http://localhost/tenants");
        cut.Markup.ShouldNotContain("expired-protected-cursor", Case.Insensitive);
    }

    [Fact]
    public void Tenant_row_links_never_render_the_current_paging_cursor()
    {
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None)]));
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            "/tenants?search=acme&status=Active&sort=name&cursor=opaque-protected-cursor");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        cut.Markup.ShouldNotContain("opaque-protected-cursor", Case.Insensitive);
        cut.Find("[data-testid='tenants-list-detail-link']").GetAttribute("href").ShouldNotBeNull().ShouldContain("search%3Dacme");
        cut.Find("[data-testid='tenants-list-detail-link']").GetAttribute("href").ShouldNotBeNull().ShouldContain("status%3DActive");
    }

    [Fact]
    public async Task Switching_to_users_does_not_carry_a_tenant_cursor()
    {
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)]));
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants?cursor=tenant-list-cursor");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        FluentTabs tabs = cut.FindComponent<FluentTabs>().Instance;

        await cut.InvokeAsync(() => tabs.ActiveTabIdChanged.InvokeAsync(TenantWorkspaceState.UsersTab));

        cut.WaitForAssertion(() =>
        {
            navigation.Uri.ShouldContain("tab=users");
            navigation.Uri.ShouldNotContain("tenant-list-cursor");
            cut.FindComponent<UserMembershipLookupPanel>().Instance.InitialCursor.ShouldBeNull();
        });
    }

    [Fact]
    public async Task Users_round_trip_preserves_and_reloads_the_tenant_query()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            return Task.FromResult(request.Search == "filtered"
                ? ReadySnapshot([Row("tenant.filtered", "Filtered", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)])
                : ReadySnapshot([Row("tenant.default", "Default", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)]));
        });
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        await ChangeSearchAsync(cut, "filtered");
        FluentTabs tabs = cut.FindComponent<FluentTabs>().Instance;
        await cut.InvokeAsync(() => tabs.ActiveTabIdChanged.InvokeAsync(TenantWorkspaceState.UsersTab));
        await cut.InvokeAsync(() => tabs.ActiveTabIdChanged.InvokeAsync(TenantWorkspaceState.TenantsTab));

        cut.WaitForAssertion(() =>
        {
            requests.Count.ShouldBeGreaterThanOrEqualTo(3);
            requests[^1].Search.ShouldBe("filtered");
            requests[^1].Cursor.ShouldBeNull();
            cut.Markup.ShouldContain("tenant.filtered");
            cut.Markup.ShouldNotContain("tenant.default");
        });
    }

    [Fact]
    public void Same_route_query_navigation_reapplies_workspace_state()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            string suffix = request.Search ?? "default";
            return Task.FromResult(ReadySnapshot(
                [Row($"tenant.{suffix}", suffix, TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)]));
        });
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants?search=first");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForAssertion(() => requests[^1].Search.ShouldBe("first"));

        navigation.NavigateTo("/tenants?search=second");

        cut.WaitForAssertion(() =>
        {
            requests[^1].Search.ShouldBe("second");
            cut.Markup.ShouldContain("tenant.second");
            cut.Markup.ShouldNotContain("tenant.first");
        });
    }

    [Fact]
    public void Canonical_workspace_url_does_not_navigate_to_itself()
    {
        RegisterServices(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Current));
        Bunit.TestDoubles.BunitNavigationManager navigation =
            (Bunit.TestDoubles.BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants");
        int navigationCount = navigation.History.Count;

        _ = Render<TenantsWorkspace>();

        navigation.History.Count.ShouldBe(navigationCount);
        navigation.Uri.ShouldBe("http://localhost/tenants");
    }

    [Fact]
    public void Disposing_workspace_does_not_navigate_to_the_remaining_tab()
    {
        RegisterServices(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Current));
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        Bunit.TestDoubles.BunitNavigationManager navigation =
            (Bunit.TestDoubles.BunitNavigationManager)Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        int navigationCount = navigation.History.Count;

        cut.Dispose();

        navigation.History.Count.ShouldBe(navigationCount);
        navigation.Uri.ShouldBe("http://localhost/tenants");
    }

    [Fact]
    public void Normalized_equal_invalid_query_is_replaced_with_the_canonical_url()
    {
        RegisterServices(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Current));
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants?tab=invalid");

        _ = Render<TenantsWorkspace>();

        navigation.Uri.ShouldBe("http://localhost/tenants");
    }

    [Fact]
    public void Canonical_deep_link_restores_the_grid_sort_direction()
    {
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)]));
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?sort=name&desc=true");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        FluentDataGrid<TenantListRow> grid = cut.FindComponent<FluentDataGrid<TenantListRow>>().Instance;

        cut.WaitForAssertion(() => grid.SortByAscending.ShouldBe(false));
    }

    [Fact]
    public void Same_route_sort_navigation_recreates_the_grid_with_the_new_sort_state()
    {
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)]));
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants?sort=name&desc=true");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        FluentDataGrid<TenantListRow> originalGrid = cut.FindComponent<FluentDataGrid<TenantListRow>>().Instance;

        navigation.NavigateTo("/tenants?sort=status");

        cut.WaitForAssertion(() =>
        {
            FluentDataGrid<TenantListRow> updatedGrid = cut.FindComponent<FluentDataGrid<TenantListRow>>().Instance;
            ReferenceEquals(updatedGrid, originalGrid).ShouldBeFalse();
            updatedGrid.SortByAscending.ShouldBe(true);
        });
    }

    [Fact]
    public async Task Search_returning_no_matches_renders_the_search_page_empty_surface()
    {
        // The production gateway no longer emits FilteredEmpty for a search page, so this scenario is
        // exercised through the surface state it actually ships.
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            return Task.FromResult(string.IsNullOrWhiteSpace(request.Search)
                ? ReadySnapshot([Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)])
                : SearchPageEmptySnapshot(hasMore: false));
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        await ChangeSearchAsync(cut, "nomatch");

        cut.WaitForElement("[data-testid='tenants-list-search-page-empty']");
        cut.Find("[data-testid='tenants-list-search-page-empty']").GetAttribute("role").ShouldBe("status");
        cut.FindAll("[data-testid='tenants-list-filtered-empty']").ShouldBeEmpty();
    }

    [Fact]
    public async Task Empty_search_page_copy_distinguishes_a_non_terminal_window_without_claiming_later_matches()
    {
        // An empty raw window can still advance when it was emptied by malformed/duplicate hits or by the
        // operator's own status recheck. The global no-match verdict is false in that case because later
        // pages remain unchecked. The non-terminal copy offers Next without promising that it contains a
        // visible match and without revealing why this page is empty.
        bool hasMore = true;
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            return Task.FromResult(string.IsNullOrWhiteSpace(request.Search)
                ? ReadySnapshot([Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)])
                : SearchPageEmptySnapshot(hasMore));
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        await ChangeSearchAsync(cut, "nomatch");

        cut.WaitForElement("[data-testid='tenants-list-search-page-empty']");
        string nonTerminalTitle = cut.Find("[data-testid='tenants-list-search-page-empty'] h2").TextContent;
        string nonTerminalMessage = cut.Find("[data-testid='tenants-list-search-page-empty'] p").TextContent;

        nonTerminalTitle.ShouldBe("No visible tenants on this search page");
        nonTerminalMessage.ShouldBe(
            "No authorized tenant results are visible on this page. "
            + "Continue to the next search page to check for more results, or clear the search to return to the full list.");
        nonTerminalMessage.ShouldNotContain("verified");
        cut.Find("[data-testid='tenants-list-next']").HasAttribute("disabled").ShouldBeFalse();

        // Once the same search is terminal, the global no-match verdict becomes honest.
        hasMore = false;
        cut.Find("[data-testid='tenants-list-refresh']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-list-search-page-empty'] h2").TextContent
                .ShouldBe("No tenants match this search");
            cut.Find("[data-testid='tenants-list-search-page-empty'] p").TextContent
                .ShouldBe(
                    "No tenants you can access match this search. "
                    + "Check the search term, or clear it to return to the full list.");
        });

        // The page is terminal for this search, so it must offer the way back to the full list.
        cut.Find("[data-testid='tenants-list-state-reset']").ShouldNotBeNull();
    }

    [Fact]
    public void Search_page_empty_never_renders_its_candidate_existence_reason()
    {
        RegisterServices(SearchPageEmptySnapshot(
            hasMore: false,
            reason: TenantListReason.RowEnrichmentUnavailable));
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-search-page-empty']");

        string message = cut.Find("[data-testid='tenants-list-search-page-empty'] p").TextContent;
        message.ShouldBe(
            "No tenants you can access match this search. "
            + "Check the search term, or clear it to return to the full list.");
        message.ShouldNotContain("counts are unavailable", Case.Insensitive);
    }

    [Fact]
    public async Task Search_unavailable_notice_renders_non_blocking_surface_over_the_list()
    {
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            return Task.FromResult(string.IsNullOrWhiteSpace(request.Search)
                ? ReadySnapshot([Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)])
                : ReadySnapshot(
                    [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)]) with
                    { Notice = TenantListReason.SearchUnavailable });
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        await ChangeSearchAsync(cut, "term");

        cut.WaitForElement("[data-testid='tenants-list-search-unavailable-notice']");
        cut.FindAll("[data-testid='tenants-list-degraded']").ShouldBeEmpty();
        cut.Markup.ShouldContain("tenant.alpha");
    }

    [Fact]
    public async Task Search_unavailable_empty_snapshot_keeps_honest_empty_state_with_notice()
    {
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            return Task.FromResult(string.IsNullOrWhiteSpace(request.Search)
                ? ReadySnapshot([Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)])
                : TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Unknown) with
                    { Notice = TenantListReason.SearchUnavailable });
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        await ChangeSearchAsync(cut, "term");

        cut.WaitForElement("[data-testid='tenants-list-search-unavailable-notice']");
        cut.WaitForElement("[data-testid='tenants-list-empty']");
        cut.FindAll("[data-testid='tenants-list-degraded']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-list-filtered-empty']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(TenantListSurfaceKind.Error)]
    [InlineData(TenantListSurfaceKind.Unauthorized)]
    public void Search_and_list_unavailable_renders_its_terminal_copy_and_stable_selector(
        TenantListSurfaceKind kind)
    {
        TenantListSnapshot terminal = kind switch
        {
            TenantListSurfaceKind.Error => TenantListSnapshot.Error(),
            TenantListSurfaceKind.Unauthorized => TenantListSnapshot.Unauthorized(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
        RegisterServices(terminal with { Notice = TenantListReason.SearchAndListUnavailable });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();

        var notice = cut.WaitForElement("[data-testid='tenants-list-search-and-list-unavailable-notice']");
        notice.TextContent.ShouldContain(
            "Protected whole-set search is temporarily unavailable, and the authorized tenant list could not be loaded either. Try again later.");
        cut.Find(kind is TenantListSurfaceKind.Error
            ? "[data-testid='tenants-list-error']"
            : "[data-testid='tenants-list-unauthorized']").ShouldNotBeNull();
        AssertSingleNoticeLiveRegion(cut);
    }

    [Fact]
    public void Authoritative_search_next_and_previous_use_only_server_held_search_cursors()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            return Task.FromResult(request.SearchCursor == "protected-page-two"
                ? AuthoritativeSnapshot(
                    [Row("tenant.two", "Two", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)])
                : AuthoritativeSnapshot(
                    [Row("tenant.one", "One", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)],
                    nextCursor: "protected-page-two",
                    hasMore: true));
        });
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants?search=needle");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("tenant.two"));

        requests[^1].SearchCursor.ShouldBe("protected-page-two");
        requests[^1].Cursor.ShouldBeNull();
        navigation.Uri.ShouldNotContain("cursor=", Case.Insensitive);
        cut.Markup.ShouldNotContain("protected-page-two", Case.Sensitive);

        cut.Find("[data-testid='tenants-list-previous']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("tenant.one"));

        requests[^1].SearchCursor.ShouldBeNull();
        requests[^1].Cursor.ShouldBeNull();
    }

    [Fact]
    public void Search_fallback_next_and_previous_use_only_the_server_held_ordinary_cursor()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            TenantListSnapshot page = request.Cursor == "ordinary-page-two"
                ? ReadySnapshot([Row("fallback.two", "Two", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None)])
                : ReadySnapshot(
                    [Row("fallback.one", "One", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None)],
                    nextCursor: "ordinary-page-two",
                    hasMore: true);
            return Task.FromResult(page with { Notice = TenantListReason.SearchUnavailable });
        });
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants?search=needle");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("fallback.two"));

        requests[^1].Cursor.ShouldBe("ordinary-page-two");
        requests[^1].SearchCursor.ShouldBeNull();
        navigation.Uri.ShouldNotContain("cursor=", Case.Insensitive);
        cut.Markup.ShouldNotContain("ordinary-page-two", Case.Sensitive);

        cut.Find("[data-testid='tenants-list-previous']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("fallback.one"));

        requests[^1].Cursor.ShouldBeNull();
        requests[^1].SearchCursor.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Crossing_the_authoritative_fallback_boundary_leaves_an_honest_previous_affordance(bool startAuthoritative)
    {
        int callCount = 0;
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            requests.Add(call.ArgAt<TenantListRequest>(0));
            callCount++;

            // Loads 1-2 run in the starting mode, load 3 crosses the boundary, and loads 4-6 cross back, so a
            // retained cursor from the first mode cannot silently reactivate on the LATER recovery.
            bool authoritative = callCount switch
            {
                <= 2 => startAuthoritative,
                3 => !startAuthoritative,
                _ => startAuthoritative,
            };
            TenantListRow row = Row(
                $"tenant.page-{callCount}",
                "Row",
                TenantStatus.Active,
                ReadModelFreshnessState.Unknown,
                authoritative ? TenantPendingState.Unknown : TenantPendingState.None);

            // Every page mints its own cursor. While each mode reused one shared literal, no assertion about
            // WHICH page a later request resumed from could fail, so the resurrection this test exists to
            // forbid was textually indistinguishable from the correct fresh cursor.
            return Task.FromResult(authoritative
                ? AuthoritativeSnapshot([row], nextCursor: $"protected-next-{callCount}", hasMore: true)
                : ReadySnapshot([row], nextCursor: $"ordinary-next-{callCount}", hasMore: true) with
                {
                    Notice = TenantListReason.SearchUnavailable,
                });
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        TenantSearchPagingState paging = Services.GetRequiredService<TenantSearchPagingState>();

        // The request field each mode pages through, and the cursor the given load handed out. Asserting the
        // starting mode's own field is what makes these checks falsifiable: the field belonging to the mode a
        // load is NOT running in is null by construction, so asserting only that can never fail.
        string StartingCursor(int call) => startAuthoritative ? $"protected-next-{call}" : $"ordinary-next-{call}";
        string CrossedCursor(int call) => startAuthoritative ? $"ordinary-next-{call}" : $"protected-next-{call}";
        string? StartingField(TenantListRequest request) => startAuthoritative ? request.SearchCursor : request.Cursor;
        string? CrossedField(TenantListRequest request) => startAuthoritative ? request.Cursor : request.SearchCursor;

        // Page two in the starting mode establishes retained history and a cursor for that mode only.
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => callCount.ShouldBe(2));
        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeFalse();
        StartingField(requests[1]).ShouldBe(StartingCursor(1));
        CrossedField(requests[1]).ShouldBeNull();
        (startAuthoritative ? paging.SearchCursor : paging.FallbackCursor).ShouldBe(StartingCursor(1));
        (startAuthoritative ? paging.FallbackCursor : paging.SearchCursor).ShouldBeNull();

        // Load three crosses the boundary; the incoming mode starts from its own first page and the retired
        // mode is cleared, with a mapped localized explanation rather than a silent jump backwards.
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => callCount.ShouldBe(3));

        // The crossing load is issued in the mode that was still active when the operator asked for it, from
        // the circuit-scoped cursor that mode owns -- not from a component field a detail return discards,
        // and not from nowhere. Only that one mode may contribute a cursor to the request.
        StartingField(requests[2]).ShouldBe(StartingCursor(2));
        CrossedField(requests[2]).ShouldBeNull();

        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
        paging.SearchCursor.ShouldBeNull();
        paging.FallbackCursor.ShouldBeNull();
        cut.Find("[data-testid='tenants-list-search-paging-restarted-notice']").TextContent.ShouldContain(
            "The available tenant source changed. Paging restarted from the first page.");
        AssertSingleNoticeLiveRegion(cut);

        // Load four crosses back. It runs in the mode load three entered, and carries the cursor load three
        // minted rather than anything the crossing retired.
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => callCount.ShouldBe(4));

        CrossedField(requests[3]).ShouldBe(CrossedCursor(3));
        StartingField(requests[3]).ShouldBeNull();
        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
        paging.SearchCursor.ShouldBeNull();
        paging.FallbackCursor.ShouldBeNull();

        // Load five is where the resurrection rule is actually decided: the starting mode is active again, so
        // had the crossing merely parked that mode's history instead of retiring it, this request would
        // resume StartingCursor(2) -- the page the operator left before the boundary. It must carry the
        // cursor load four handed out instead.
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => callCount.ShouldBe(5));

        StartingField(requests[4]).ShouldBe(StartingCursor(4));
        CrossedField(requests[4]).ShouldBeNull();

        // ...and Previous must walk back exactly one step, to page one, rather than into the retained depth
        // the first pass through this mode accumulated.
        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("[data-testid='tenants-list-previous']").Click();
        cut.WaitForAssertion(() => callCount.ShouldBe(6));

        StartingField(requests[5]).ShouldBeNull();
        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
        paging.HasPrevious(authoritative: true).ShouldBeFalse();
        paging.HasPrevious(authoritative: false).ShouldBeFalse();
    }

    [Fact]
    public async Task Search_query_identity_change_after_page_two_clears_protected_history()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            return Task.FromResult(AuthoritativeSnapshot(
                [Row(request.SearchCursor is null ? "tenant.one" : "tenant.two", "Tenant", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)],
                nextCursor: request.SearchCursor is null ? "protected-page-two" : null,
                hasMore: request.SearchCursor is null));
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=before");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => requests[^1].SearchCursor.ShouldBe("protected-page-two"));

        await ChangeSearchAsync(cut, "after");

        cut.WaitForAssertion(() =>
        {
            requests[^1].Search.ShouldBe("after");
            requests[^1].SearchCursor.ShouldBeNull();
            requests[^1].Cursor.ShouldBeNull();
            cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
        });
    }

    [Fact]
    public void Sparse_authoritative_search_page_keeps_next_paging_available_without_backfill()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            return Task.FromResult(request.SearchCursor == "protected-page-two"
                ? AuthoritativeSnapshot(
                    [Row("tenant.visible", "Visible", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)])
                : SearchPageEmptySnapshot(hasMore: true, nextCursor: "protected-page-two"));
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-search-page-empty']");

        cut.Find("[data-testid='tenants-list-next']").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("tenant.visible"));
        requests[^1].SearchCursor.ShouldBe("protected-page-two");
    }

    [Fact]
    public void Empty_final_authoritative_page_keeps_previous_only_paging_available()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            return Task.FromResult(request.SearchCursor == "protected-final-page"
                ? SearchPageEmptySnapshot(hasMore: false)
                : AuthoritativeSnapshot(
                    [Row("tenant.page-one", "One", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)],
                    nextCursor: "protected-final-page",
                    hasMore: true));
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForElement("[data-testid='tenants-list-search-page-empty']");

        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("[data-testid='tenants-list-next']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-list-previous']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("tenant.page-one"));
        requests[^1].SearchCursor.ShouldBeNull();
    }

    [Fact]
    public async Task Detail_round_trip_restores_protected_search_page_history_and_server_held_page_size()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            bool pageTwo = request.SearchCursor == "protected-page-two";
            return Task.FromResult(AuthoritativeSnapshot(
                [Row(pageTwo ? "tenant.page-two" : "tenant.page-one", pageTwo ? "Two" : "One", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)],
                nextCursor: pageTwo ? null : "protected-page-two",
                hasMore: !pageTwo));
        });
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants?search=needle");
        IRenderedComponent<TenantsWorkspace> first = Render<TenantsWorkspace>();
        first.WaitForElement("[data-testid='tenants-list-grid']");

        await ChangePageSizeAsync(first, 50);
        first.Find("[data-testid='tenants-list-next']").Click();
        first.WaitForAssertion(() => requests[^1].SearchCursor.ShouldBe("protected-page-two"));
        first.Dispose();

        navigation.NavigateTo(
            "/tenants?search=needle&selected=tenant.page-two&anchor=tenant-row-tenant.page-two");
        IRenderedComponent<TenantsWorkspace> returned = Render<TenantsWorkspace>();
        returned.WaitForElement("[data-testid='tenants-list-grid']");

        requests[^1].PageSize.ShouldBe(50);
        requests[^1].SearchCursor.ShouldBe("protected-page-two");
        returned.Markup.ShouldContain("tenant.page-two");
        returned.Markup.ShouldNotContain("protected-page-two", Case.Sensitive);
        navigation.Uri.ShouldNotContain("pageSize", Case.Insensitive);
        navigation.Uri.ShouldNotContain("cursor=", Case.Insensitive);
        returned.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeFalse();

        returned.Find("[data-testid='tenants-list-previous']").Click();
        returned.WaitForAssertion(() => returned.Markup.ShouldContain("tenant.page-one"));
        requests[^1].SearchCursor.ShouldBeNull();
        requests[^1].PageSize.ShouldBe(50);
    }

    [Fact]
    public async Task Crossing_is_still_detected_after_a_detail_return_recreates_the_workspace_component()
    {
        // The circuit-scoped paging service survives the tenant-detail round trip while the component does
        // not. If the active paging mode lived on the component it would be lost exactly here, the crossing
        // would be undetectable, and the retained protected cursor would resume a deep page under a notice
        // claiming a restart.
        List<TenantListRequest> requests = [];
        bool authoritative = true;
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            bool pageTwo = request.SearchCursor == "protected-page-two";
            return Task.FromResult(authoritative
                ? AuthoritativeSnapshot(
                    [Row(pageTwo ? "tenant.page-two" : "tenant.page-one", "Row", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)],
                    nextCursor: pageTwo ? null : "protected-page-two",
                    hasMore: !pageTwo)
                : ReadySnapshot(
                    [Row("tenant.fallback", "Fallback", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None)]) with
                {
                    Notice = TenantListReason.SearchUnavailable,
                });
        });
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants?search=needle");
        IRenderedComponent<TenantsWorkspace> first = Render<TenantsWorkspace>();
        first.WaitForElement("[data-testid='tenants-list-grid']");
        first.Find("[data-testid='tenants-list-next']").Click();
        first.WaitForAssertion(() => requests[^1].SearchCursor.ShouldBe("protected-page-two"));

        // Navigating to tenant detail disposes the workspace component while the circuit -- and the scoped
        // paging service with it -- survives.
        await DisposeComponentsAsync();

        // The operator opened tenant detail and came back; meanwhile Memories was lost.
        authoritative = false;
        navigation.NavigateTo("/tenants?search=needle&selected=tenant.page-two&anchor=tenant-row-tenant.page-two");
        IRenderedComponent<TenantsWorkspace> returned = Render<TenantsWorkspace>();
        returned.WaitForElement("[data-testid='tenants-list-search-unavailable-notice']");

        // The crossing load itself was issued against the retained protected cursor's mode, not the mode it
        // entered, so the restart notice is honest about the load that carried it.
        requests[^1].Cursor.ShouldBeNull();
        returned.Find("[data-testid='tenants-list-search-paging-restarted-notice']").ShouldNotBeNull();
        returned.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
        TenantSearchPagingState paging = Services.GetRequiredService<TenantSearchPagingState>();
        paging.SearchCursor.ShouldBeNull();

        // ... and when search recovers, the retired protected cursor cannot resume the deep page.
        authoritative = true;
        await returned.InvokeAsync(() => returned.Find("[data-testid='tenants-list-refresh']").Click());

        returned.WaitForAssertion(() => returned.Markup.ShouldContain("tenant.page-one"));
        requests[^1].SearchCursor.ShouldBeNull();
        returned.Markup.ShouldNotContain("tenant.page-two");
    }

    [Theory]
    [InlineData("")]
    [InlineData("&selected=tenant.page-two&anchor=unrelated-row")]
    public void Search_visit_without_a_valid_detail_return_context_does_not_restore_retained_protected_paging(
        string returnContextQuery)
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            bool pageTwo = request.SearchCursor == "protected-page-two";
            return Task.FromResult(AuthoritativeSnapshot(
                [Row(pageTwo ? "tenant.page-two" : "tenant.page-one", pageTwo ? "Two" : "One", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)],
                nextCursor: pageTwo ? null : "protected-page-two",
                hasMore: !pageTwo));
        });
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants?search=needle");
        IRenderedComponent<TenantsWorkspace> first = Render<TenantsWorkspace>();
        first.WaitForElement("[data-testid='tenants-list-grid']");
        first.Find("[data-testid='tenants-list-next']").Click();
        first.WaitForAssertion(() => requests[^1].SearchCursor.ShouldBe("protected-page-two"));
        first.Dispose();

        navigation.NavigateTo($"/tenants?search=needle{returnContextQuery}");
        IRenderedComponent<TenantsWorkspace> fresh = Render<TenantsWorkspace>();
        fresh.WaitForElement("[data-testid='tenants-list-grid']");

        requests[^1].SearchCursor.ShouldBeNull();
        fresh.Markup.ShouldContain("tenant.page-one");
        fresh.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();

        // Discarding retained paging is correct here -- the return context cannot be validated -- but it
        // must not be silent. Returning by browser Back previously dropped the operator from search page
        // two to page one with no bar explaining it, because the pending-recovery scope was armed only on
        // the eligible-return branch. A restart the operator can see is the whole point of the notice.
        fresh.WaitForAssertion(() =>
            fresh.Find("[data-testid='tenants-list-search-refreshed-notice']").ShouldNotBeNull());
    }

    [Theory]
    // The localized label is asserted alongside the class, per row. Asserting the class alone leans on
    // incidental markup, which project rules forbid relying on -- and it was how the badge's resource keys
    // went missing without any test noticing: the stub localizer echoed the key back, and "Stale" is a
    // substring of "Tenants.ProjectionLifecycle.Stale", so a substring assertion would have passed either
    // way. The expected labels below are the shipped EN strings.
    [InlineData(ProjectionLifecycleState.Stale, "projection-lifecycle-badge--stale", "Stale")]
    [InlineData(ProjectionLifecycleState.Rebuilding, "projection-lifecycle-badge--rebuilding", "Rebuilding")]
    [InlineData(ProjectionLifecycleState.Degraded, "projection-lifecycle-badge--degraded", "Degraded")]
    [InlineData(ProjectionLifecycleState.Unavailable, "projection-lifecycle-badge--unavailable", "Unavailable")]
    [InlineData(ProjectionLifecycleState.LocalOnly, "projection-lifecycle-badge--localonly", "Local only")]
    public void Row_lifecycle_reaches_the_independent_lifecycle_badge_on_the_tenant_list(
        ProjectionLifecycleState lifecycle,
        string expectedClass,
        string expectedLabel)
    {
        // Drive the real consumer so deleting the grid's Lifecycle binding cannot compile while a direct
        // component test still passes. Freshness remains independently visible in the adjacent column.
        RegisterServices(_ => Task.FromResult(ReadySnapshot(
        [
            Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)
                with { Lifecycle = lifecycle },
        ])));

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        cut.Find("[data-testid='tenants-projection-lifecycle']")
            .GetAttribute("class")
            .ShouldNotBeNull()
            .ShouldContain(expectedClass);
        cut.Find("[data-testid='tenants-projection-lifecycle']").TextContent.Trim().ShouldBe(expectedLabel);
        cut.Find("[data-testid='tenants-list-truth-state']").TextContent.ShouldContain("Current");
    }

    [Fact]
    public void Empty_tenant_collection_still_renders_snapshot_lifecycle_evidence()
    {
        RegisterServices(TenantListSnapshot.Empty(
            isAuthorizationScoped: true,
            ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Rebuilding,
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();

        cut.Find("[data-testid='tenants-list-empty']");
        cut.Find("[data-testid='tenants-list-projection-lifecycle-status']")
            .GetAttribute("class")
            .ShouldNotBeNull()
            .ShouldContain("projection-lifecycle-badge--rebuilding");
        cut.FindAll("[data-testid='tenants-list-grid']").ShouldBeEmpty();
    }

    [Fact]
    public void A_first_search_visit_with_nothing_retained_restarts_nothing_and_says_nothing()
    {
        // The complement of the test above: the notice is owed only when a real position was discarded.
        // Arming it whenever the return context is invalid would put a "paging restarted" bar on an
        // ordinary first visit, which restarted nothing.
        RegisterServices(call => Task.FromResult(AuthoritativeSnapshot(
            [Row("tenant.page-one", "One", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)],
            nextCursor: "protected-page-two",
            hasMore: true)));
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        cut.FindAll("[data-testid='tenants-list-search-refreshed-notice']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-list-search-paging-restarted-notice']").ShouldBeEmpty();
    }

    [Fact]
    public void Non_interactive_prerender_pass_neither_restores_nor_reports_retained_protected_paging()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            requests.Add(call.ArgAt<TenantListRequest>(0));
            return Task.FromResult(AuthoritativeSnapshot(
                [Row("tenant.page-one", "One", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)]));
        });

        // The scoped paging service resolved on a non-interactive render pass belongs to that request's
        // scope, not the circuit, so a retained protected position must be neither consumed nor cleared nor
        // reported there. The signal is the renderer's own interactivity, not a cascading HttpContext: that
        // value does not cross render-mode boundaries and is absent on both passes of this component.
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        TenantSearchPagingState paging = Services.GetRequiredService<TenantSearchPagingState>();
        paging.EnsureScope("retained-scope");
        paging.MoveNext(authoritative: true, "protected-page-two");
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            "/tenants?search=needle&selected=tenant.previous-page&anchor=tenant-row-tenant.previous-page");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        requests.ShouldNotBeEmpty();
        requests.ShouldAllBe(static request => request.SearchCursor == null);
        requests.ShouldAllBe(static request => request.Cursor == null);
        cut.FindAll("[data-testid='tenants-list-search-refreshed-notice']").ShouldBeEmpty();
        paging.MatchesScope("retained-scope").ShouldBeTrue();
        paging.SearchCursor.ShouldBe("protected-page-two");
    }

    [Fact]
    public void Missing_retained_search_page_on_detail_return_restarts_page_one_with_polite_notice()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            requests.Add(call.ArgAt<TenantListRequest>(0));
            return Task.FromResult(AuthoritativeSnapshot(
                [Row("tenant.page-one", "One", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)]));
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            "/tenants?search=needle&selected=tenant.previous-page&anchor=tenant-row-tenant.previous-page");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();

        cut.WaitForElement("[data-testid='tenants-list-search-refreshed-notice']");
        requests.ShouldHaveSingleItem().SearchCursor.ShouldBeNull();
        AssertSingleNoticeLiveRegion(cut);
        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Pending_recovery_notice_survives_a_superseding_load_for_the_same_search_scope()
    {
        int callCount = 0;
        RegisterServices(call =>
        {
            callCount++;
            CancellationToken cancellationToken = call.ArgAt<CancellationToken>(2);
            if (callCount == 1)
            {
                var pending = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
                _ = cancellationToken.Register(() => pending.TrySetCanceled(cancellationToken));
                return pending.Task;
            }

            return Task.FromResult(AuthoritativeSnapshot(
                [Row("tenant.page-one", "One", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)]));
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            "/tenants?search=needle&selected=tenant.previous-page&anchor=tenant-row-tenant.previous-page");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForAssertion(() => callCount.ShouldBe(1));

        // The decision was made by the first (superseded) load; it must still be reported by the load that
        // actually resolves for the same protected search scope.
        cut.Find("[data-testid='tenants-list-refresh']").Click();

        cut.WaitForElement("[data-testid='tenants-list-search-refreshed-notice']");
        cut.FindAll("[data-testid='tenants-list-search-refreshed-notice']").Count.ShouldBe(1);
    }

    [Fact]
    public async Task Pending_recovery_notice_is_bound_to_its_scope_and_is_never_delivered_to_another_one()
    {
        // The sibling test above proves the owed notice survives a superseding load for the SAME scope. On
        // its own that is equally satisfied by a plain "notice pending until any load resolves" flag, which
        // is not what ships: the decision is bound to the exact protected search scope that owed it. This is
        // the other half of that pair -- the case a scope-blind flag gets wrong.
        int callCount = 0;
        RegisterServices(call =>
        {
            callCount++;
            CancellationToken cancellationToken = call.ArgAt<CancellationToken>(2);
            if (callCount == 1)
            {
                var pending = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
                _ = cancellationToken.Register(() => pending.TrySetCanceled(cancellationToken));
                return pending.Task;
            }

            return Task.FromResult(AuthoritativeSnapshot(
                [Row("tenant.page-one", "One", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)]));
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            "/tenants?search=needle&selected=tenant.previous-page&anchor=tenant-row-tenant.previous-page");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForAssertion(() => callCount.ShouldBe(1));

        TenantSearchPagingState paging = Services.GetRequiredService<TenantSearchPagingState>();
        paging.PendingRecoveryScope.ShouldNotBeNull();

        // The operator retypes the term before the superseded load resolves. The owed copy says a protected
        // page of the PREVIOUS search restarted from page one; the new search restarted nothing, so it must
        // not inherit that statement, and the decision must be dropped rather than deferred to whichever
        // load resolves next.
        await ChangeSearchAsync(cut, "after");

        cut.WaitForAssertion(() => callCount.ShouldBe(2));
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        paging.PendingRecoveryScope.ShouldBeNull();
        cut.FindAll("[data-testid='tenants-list-search-refreshed-notice']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-list-search-paging-restarted-notice']").ShouldBeEmpty();
    }

    [Fact]
    public void Invalidation_on_a_terminal_surface_clears_protected_history_and_renders_its_notice()
    {
        int callCount = 0;
        RegisterServices(_ =>
        {
            callCount++;
            return Task.FromResult(callCount == 1
                ? TenantListSnapshot.Error()
                : TenantListSnapshot.Error() with
                {
                    PagingRecovered = true,
                    PagingNotice = TenantListReason.SearchRefreshed,
                });
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-error']");
        TenantSearchPagingState paging = Services.GetRequiredService<TenantSearchPagingState>();

        // Seeded as if the circuit still held a protected page when the terminal load resolves.
        paging.MoveNext(authoritative: true, "protected-page-two");

        cut.Find("[data-testid='tenants-list-refresh']").Click();

        // The bars render from the notice reasons alone and never consult Kind, so the terminal surface
        // carries the explanation together with the clearing. No deferred/pending mechanism is involved.
        cut.WaitForElement("[data-testid='tenants-list-search-refreshed-notice']");
        cut.Find("[data-testid='tenants-list-error']").ShouldNotBeNull();
        cut.WaitForAssertion(() =>
        {
            paging.SearchCursor.ShouldBeNull();
            paging.HasPrevious(authoritative: true).ShouldBeFalse();
        });
        callCount.ShouldBe(2);
    }

    [Fact]
    public async Task A_load_returning_after_disposal_mutates_no_paging_state()
    {
        TaskCompletionSource<TenantListSnapshot> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int callCount = 0;
        RegisterServices(_ =>
        {
            callCount++;
            return callCount == 1
                ? Task.FromResult(AuthoritativeSnapshot(
                    [Row("tenant.page-one", "One", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)],
                    nextCursor: "protected-page-two",
                    hasMore: true))
                : pending.Task;
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        TenantSearchPagingState paging = Services.GetRequiredService<TenantSearchPagingState>();
        paging.ActiveModeAuthoritative.ShouldBe(true);
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => callCount.ShouldBe(2));

        cut.Instance.Dispose();

        // The in-flight load now returns. Its post-await apply block must retire no paging mode and clear
        // no protected history behind a surface no operator can see.
        pending.SetResult(TenantListSnapshot.Error() with
        {
            PagingRecovered = true,
            PagingNotice = TenantListReason.SearchRefreshed,
        });
        await pending.Task;
        await Task.Delay(200);

        paging.SearchCursor.ShouldBe("protected-page-two");
        paging.HasPrevious(authoritative: true).ShouldBeTrue();
        paging.ActiveModeAuthoritative.ShouldBe(true);
    }

    [Fact]
    public void Transient_error_on_a_detail_return_still_reports_recovery_on_the_same_scope_retry()
    {
        int callCount = 0;
        RegisterServices(_ =>
        {
            callCount++;
            return Task.FromResult(callCount == 1
                ? TenantListSnapshot.Error()
                : AuthoritativeSnapshot(
                    [Row("tenant.page-one", "One", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)]));
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            "/tenants?search=needle&selected=tenant.previous-page&anchor=tenant-row-tenant.previous-page");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-error']");
        cut.FindAll("[data-testid='tenants-list-search-refreshed-notice']").ShouldBeEmpty();

        // Same search scope, retried after a transient failure: the pending decision must not have been
        // consumed by the error, so the operator still learns that paging restarted at page one.
        cut.Find("[data-testid='tenants-list-refresh']").Click();

        cut.WaitForElement("[data-testid='tenants-list-search-refreshed-notice']");
        callCount.ShouldBe(2);
    }

    [Fact]
    public async Task Failed_detail_return_does_not_defer_its_recovery_notice_to_a_later_search()
    {
        int callCount = 0;
        RegisterServices(call =>
        {
            callCount++;
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            return Task.FromResult(callCount == 1
                ? TenantListSnapshot.Error()
                : AuthoritativeSnapshot(
                    [Row($"tenant.{request.Search}", "Later", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)]));
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            "/tenants?search=before&selected=tenant.previous-page&anchor=tenant-row-tenant.previous-page");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-error']");
        cut.FindAll("[data-testid='tenants-list-search-refreshed-notice']").ShouldBeEmpty();

        await ChangeSearchAsync(cut, "after");

        cut.WaitForElement("[data-testid='tenants-list-grid']");
        cut.Markup.ShouldContain("tenant.after");
        cut.FindAll("[data-testid='tenants-list-search-refreshed-notice']").ShouldBeEmpty();
    }

    [Fact]
    public void Search_fallback_cursor_recovery_renders_both_non_blocking_support_safe_notices()
    {
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None)]) with
        {
            Notice = TenantListReason.SearchUnavailable,
            PagingNotice = TenantListReason.ListRefreshed,
            FallbackPagingRecovered = true,
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();

        cut.WaitForElement("[data-testid='tenants-list-search-unavailable-notice']");
        cut.WaitForElement("[data-testid='tenants-list-refreshed-notice']");
        AssertSingleNoticeLiveRegion(cut);
        cut.Markup.ShouldNotContain("cursor", Case.Insensitive);
    }

    [Fact]
    public async Task Discarded_ordinary_fallback_position_renders_the_source_changed_recovery_copy()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            bool pageTwo = request.Cursor == "ordinary-page-two";
            return Task.FromResult(ReadySnapshot(
                [Row(pageTwo ? "tenant.page-two" : "tenant.page-one", "Fallback", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None)],
                nextCursor: pageTwo ? null : "ordinary-page-two",
                hasMore: !pageTwo) with
            {
                Notice = TenantListReason.SearchUnavailable,
            });
        });
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants?search=needle");
        IRenderedComponent<TenantsWorkspace> first = Render<TenantsWorkspace>();
        first.WaitForElement("[data-testid='tenants-list-grid']");
        first.Find("[data-testid='tenants-list-next']").Click();
        first.WaitForAssertion(() => requests[^1].Cursor.ShouldBe("ordinary-page-two"));

        await DisposeComponentsAsync();
        navigation.NavigateTo("/tenants?search=needle");
        IRenderedComponent<TenantsWorkspace> returned = Render<TenantsWorkspace>();

        var notice = returned.WaitForElement("[data-testid='tenants-list-search-paging-restarted-notice']");
        notice.TextContent.ShouldContain(
            "The available tenant source changed. Paging restarted from the first page.");
        returned.FindAll("[data-testid='tenants-list-search-refreshed-notice']").ShouldBeEmpty();
        requests[^1].Cursor.ShouldBeNull();
        returned.Markup.ShouldContain("tenant.page-one");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Recovery_clears_only_the_applicable_search_paging_history(bool recoverAuthoritative)
    {
        int callCount = 0;
        RegisterServices(_ =>
        {
            callCount++;
            TenantListRow row = Row("tenant.row", "Row", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown);
            if (callCount == 1)
            {
                return Task.FromResult(recoverAuthoritative
                    ? AuthoritativeSnapshot([row])
                    : ReadySnapshot([row]) with { Notice = TenantListReason.SearchUnavailable });
            }

            return Task.FromResult(recoverAuthoritative
                ? AuthoritativeSnapshot([row]) with
                {
                    Notice = TenantListReason.SearchRefreshed,
                    PagingRecovered = true,
                }
                : ReadySnapshot([row]) with
                {
                    Notice = TenantListReason.SearchUnavailable,
                    PagingNotice = TenantListReason.ListRefreshed,
                    FallbackPagingRecovered = true,
                });
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        TenantSearchPagingState paging = Services.GetRequiredService<TenantSearchPagingState>();
        paging.MoveNext(authoritative: true, "protected-two");
        paging.MoveNext(authoritative: false, "ordinary-two");

        cut.Find("[data-testid='tenants-list-refresh']").Click();
        cut.WaitForAssertion(() => callCount.ShouldBe(2));

        cut.WaitForAssertion(() =>
        {
            paging.HasPrevious(recoverAuthoritative).ShouldBeFalse();
            (recoverAuthoritative ? paging.SearchCursor : paging.FallbackCursor).ShouldBeNull();
            paging.HasPrevious(!recoverAuthoritative).ShouldBeTrue();
            (recoverAuthoritative ? paging.FallbackCursor : paging.SearchCursor).ShouldNotBeNull();
        });
    }

    [Theory]
    [InlineData(TenantListReason.GatewayUnavailable)]
    [InlineData(TenantListReason.SearchPartiallyAvailable)]
    [InlineData(TenantListReason.ProjectionDegraded)]
    public void Unmapped_notice_reasons_never_render_a_blank_or_unaddressable_message_bar(TenantListReason unmapped)
    {
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None)]) with
        {
            Notice = unmapped,
            PagingNotice = unmapped,
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        cut.FindComponents<FluentMessageBar>().ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-list-notice']").ShouldBeEmpty();
        cut.FindAll("[data-testid='']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(TenantListReason.GatewayUnavailable)]
    [InlineData(TenantListReason.SearchPartiallyAvailable)]
    [InlineData(TenantListReason.ProjectionDegraded)]
    public void An_unmapped_paging_reason_is_stopped_by_the_secondary_bar_s_own_message_and_testid_guards(
        TenantListReason unmapped)
    {
        // The sibling above puts the same unmapped reason in both slots, so the secondary bar is already
        // suppressed by its duplicate-reason guard and its own empty-message and empty-testid guards are
        // never evaluated -- they could be deleted and that test would stay green. Here the primary slot
        // carries a mapped reason, so the duplicate guard cannot fire and those two guards are the only
        // thing between an unmapped paging reason and a blank, unaddressable second bar.
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None)]) with
        {
            Notice = TenantListReason.SearchUnavailable,
            PagingNotice = unmapped,
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        // Exactly the mapped primary bar, carrying real copy behind a real selector, and nothing beside it.
        cut.FindComponents<FluentMessageBar>().ShouldHaveSingleItem();
        cut.Find("[data-testid='tenants-list-search-unavailable-notice']").TextContent.ShouldContain(
            "Protected whole-set search is temporarily unavailable.");
        cut.FindAll("[data-testid='']").ShouldBeEmpty();
        AssertSingleNoticeLiveRegion(cut);
    }

    [Fact]
    public void Notice_live_region_exists_before_any_notice_populates_it()
    {
        int callCount = 0;
        RegisterServices(_ =>
        {
            callCount++;
            TenantListSnapshot ready = ReadySnapshot(
                [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None)]);
            return Task.FromResult(callCount == 1
                ? ready
                : ready with { Notice = TenantListReason.SearchUnavailable });
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        // A live region inserted in the same render that first populates it is routinely not announced, so
        // the region must already be in the DOM while it is still empty.
        AssertSingleNoticeLiveRegion(cut);
        cut.FindAll("[data-testid='tenants-list-notices'] [data-testid$='-notice']").ShouldBeEmpty();

        cut.Find("[data-testid='tenants-list-refresh']").Click();

        cut.WaitForElement("[data-testid='tenants-list-notices'] [data-testid='tenants-list-search-unavailable-notice']");
        AssertSingleNoticeLiveRegion(cut);
    }

    [Fact]
    public void Secondary_notice_equal_to_primary_renders_only_one_live_announcement()
    {
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None)]) with
        {
            Notice = TenantListReason.SearchUnavailable,
            PagingNotice = TenantListReason.SearchUnavailable,
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-search-unavailable-notice']");

        cut.FindAll("[data-testid='tenants-list-search-unavailable-notice']").Count.ShouldBe(1);
        cut.FindComponents<FluentMessageBar>().Count.ShouldBe(1);
    }

    [Fact]
    public void Partial_authoritative_search_keeps_verified_rows_and_whole_set_semantics()
    {
        RegisterServices(TenantListSnapshot.Degraded(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)],
            TenantListReason.SearchPartiallyAvailable) with
        {
            IsAuthoritativeSearch = true,
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();

        cut.WaitForElement("[data-testid='tenants-list-grid']");
        cut.WaitForElement("[data-testid='tenants-list-degraded']");
        cut.Find("[data-testid='tenants-list-degraded']").TextContent.ShouldContain(
            "Some search results could not be verified. Only authorized tenant rows that were verified are shown.");
        cut.Find("[data-testid='tenants-list-page-local-semantics']").TextContent.ShouldContain(
            "Search and status apply across indexed tenant candidates.");
        cut.FindComponents<FluentSelect<string, string>>()
            .Select(static component => component.Instance)
            .Single(instance => instance.AdditionalAttributes is { } attributes
                && attributes.TryGetValue("data-testid", out object? value)
                && string.Equals(value as string, "tenants-list-status-filter", StringComparison.Ordinal))
            .Label.ShouldBe("Status across indexed candidates");
        cut.Markup.ShouldContain("Pending state unknown");
    }

    [Fact]
    public async Task Rapid_search_change_cancels_obsolete_load_and_only_renders_the_latest_result()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool firstCanceled = false;
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            CancellationToken cancellationToken = call.ArgAt<CancellationToken>(2);
            if (request.Search == "first")
            {
                var pending = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
                _ = cancellationToken.Register(() =>
                {
                    firstCanceled = true;
                    pending.TrySetCanceled(cancellationToken);
                });
                firstStarted.TrySetResult();
                return pending.Task;
            }

            return Task.FromResult(request.Search == "second"
                ? AuthoritativeSnapshot(
                    [Row("tenant.second", "Second", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)])
                : ReadySnapshot(
                    [Row("tenant.initial", "Initial", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None)]));
        });
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        Task firstChange = ChangeSearchAsync(cut, "first");
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task secondChange = ChangeSearchAsync(cut, "second");
        await Task.WhenAll(firstChange, secondChange).WaitAsync(TimeSpan.FromSeconds(5));

        cut.WaitForAssertion(() =>
        {
            firstCanceled.ShouldBeTrue();
            cut.Markup.ShouldContain("tenant.second");
            cut.Markup.ShouldNotContain("tenant.initial");
        });
    }

    [Fact]
    public void Cursor_paging_passes_opaque_cursor_and_preserves_markers()
    {
        TenantListSnapshot firstPage = ReadySnapshot(
            [
                Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None),
            ],
            nextCursor: "opaque-next-cursor",
            hasMore: true);
        TenantListSnapshot secondPage = ReadySnapshot(
            [
                Row("tenant.beta", "Beta", TenantStatus.Disabled, ReadModelFreshnessState.Stale, TenantPendingState.Unknown),
            ]);
        Queue<TenantListSnapshot> snapshots = new([firstPage, secondPage, firstPage]);
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            requests.Add(call.ArgAt<TenantListRequest>(0));
            return Task.FromResult(snapshots.Dequeue());
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        cut.Markup.ShouldContain("tenant.alpha");

        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("tenant.beta"));

        requests[1].Cursor.ShouldBe("opaque-next-cursor");
        cut.Find("[data-testid='tenants-list-truth-state']").TextContent.ShouldContain("Stale");
        cut.Markup.ShouldContain("Pending state unknown");

        cut.Find("[data-testid='tenants-list-previous']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("tenant.alpha"));

        requests[2].Cursor.ShouldBeNull();
        cut.Find("[data-testid='tenants-list-truth-state']").TextContent.ShouldContain("Current");
        cut.Markup.ShouldContain("No pending changes");
    }

    [Theory]
    [InlineData(TenantListSurfaceKind.Loading, "tenants-list-loading", "status", "polite")]
    [InlineData(TenantListSurfaceKind.Empty, "tenants-list-empty", "status", "polite")]
    [InlineData(TenantListSurfaceKind.FilteredEmpty, "tenants-list-filtered-empty", "status", "polite")]
    [InlineData(TenantListSurfaceKind.SearchPageEmpty, "tenants-list-search-page-empty", "status", "polite")]
    [InlineData(TenantListSurfaceKind.Error, "tenants-list-error", "alert", "assertive")]
    [InlineData(TenantListSurfaceKind.Stale, "tenants-list-stale", "status", "polite")]
    [InlineData(TenantListSurfaceKind.Degraded, "tenants-list-degraded", "alert", "assertive")]
    public void Workspace_renders_each_distinct_list_state(
        TenantListSurfaceKind kind,
        string selector,
        string expectedRole,
        string expectedAriaLive)
    {
        TenantListSnapshot snapshot = kind switch
        {
            TenantListSurfaceKind.Loading => TenantListSnapshot.Loading(),
            TenantListSurfaceKind.Empty => TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Unknown),
            TenantListSurfaceKind.FilteredEmpty => TenantListSnapshot.FilteredEmpty(),
            TenantListSurfaceKind.SearchPageEmpty => SearchPageEmptySnapshot(hasMore: true),
            TenantListSurfaceKind.Error => TenantListSnapshot.Error(),
            TenantListSurfaceKind.Stale => TenantListSnapshot.Stale(
                [TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active))],
                "\"etag\""),
            TenantListSurfaceKind.Degraded => TenantListSnapshot.Degraded(
                [TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active))],
                TenantListReason.RowEnrichmentUnavailable),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
        RegisterServices(snapshot);

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement($"[data-testid='{selector}']");

        var state = cut.Find($"[data-testid='{selector}']");
        state.GetAttribute("role").ShouldBe(expectedRole);
        state.GetAttribute("aria-live").ShouldBe(expectedAriaLive);

        if (kind is TenantListSurfaceKind.FilteredEmpty)
        {
            cut.Find("[data-testid='tenants-list-state-reset']").ShouldNotBeNull();
        }
        else if (kind is TenantListSurfaceKind.SearchPageEmpty)
        {
            cut.Find($"[data-testid='{selector}'] h2").TextContent
                .ShouldBe("No visible tenants on this search page");
            cut.Find($"[data-testid='{selector}'] p").TextContent
                .ShouldBe(
                    "No authorized tenant results are visible on this page. "
                    + "Continue to the next search page to check for more results, or clear the search to return to the full list.");

            // Both paging forward and clearing the search are valid exits from a non-terminal empty window.
            cut.Find("[data-testid='tenants-list-state-reset']").ShouldNotBeNull();
            cut.Find("[data-testid='tenants-list-next']").HasAttribute("disabled").ShouldBeFalse();
        }
        else if (kind is TenantListSurfaceKind.Stale)
        {
            cut.Find("[data-testid='tenants-list-state-refresh']").ShouldNotBeNull();
        }
        else if (kind is TenantListSurfaceKind.Degraded)
        {
            cut.Find("[data-testid='tenants-list-grid']").ShouldNotBeNull();
        }
    }

    /// <remarks>
    /// <c>NotModifiedWithoutSnapshot</c> now has a producer on all four paged reads: the gateway assigns it
    /// when a supported 304 arrives with no retained snapshot to pair it with -- the server insisting
    /// nothing changed while there is nothing to show, which is neither an outage nor degraded evidence.
    /// That reachability is pinned at the gateway seam by
    /// <c>A_not_modified_response_with_nothing_retained_has_its_own_reason_on_every_paged_read</c>; this
    /// test owns the other half, that the shipped copy for the state is localized and support-safe.
    /// (Before that wiring the state had EN/FR resources and four enum members but no producer at all, so
    /// this test read as evidence of a live state that an operator could never reach.)
    /// </remarks>
    [Fact]
    public void Typed_error_reason_renders_only_localized_support_safe_copy()
    {
        RegisterServices(TenantListSnapshot.Error(TenantListReason.NotModifiedWithoutSnapshot));

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-error']");
        cut.Markup.ShouldContain("no confirmed server snapshot is available.");
        cut.Markup.ShouldNotContain(nameof(TenantListReason.NotModifiedWithoutSnapshot));
    }

    [Fact]
    public void Typed_degraded_reason_renders_only_localized_support_safe_copy()
    {
        RegisterServices(TenantListSnapshot.Degraded(
            [TenantListRow.FromSummary(new TenantSummary("customer/West EU:01", "West", TenantStatus.Active))],
            TenantListReason.RowEnrichmentUnavailable));

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-degraded']");
        cut.Markup.ShouldContain("Authorized tenant identity and lifecycle state remain usable.");
        cut.Markup.ShouldNotContain(nameof(TenantListReason.RowEnrichmentUnavailable));
    }

    [Fact]
    public void Workspace_exposes_current_page_filter_and_sort_semantics()
    {
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.None)]));

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-page-local-semantics']");

        cut.Find("[data-testid='tenants-list-status-filter']")
            .GetAttribute("aria-label")
            .ShouldBe("Status on current page");
        cut.Find("[data-testid='tenants-list-page-local-semantics']")
            .TextContent
            .ShouldContain("current authorized page");
    }

    [Fact]
    public void Tenant_list_component_has_no_browser_backend_http_or_token_storage()
    {
        string projectRoot = ProjectRoot();
        string[] componentFiles = Directory
            .GetFiles(Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components"), "*.razor", SearchOption.AllDirectories)
            .Where(static path => !path.EndsWith("App.razor", StringComparison.Ordinal))
            .ToArray();
        string combined = string.Join('\n', componentFiles.Select(File.ReadAllText));

        combined.ShouldNotContain("GET /api/tenants", Case.Insensitive);
        combined.ShouldNotContain("HttpClient");
        combined.ShouldNotContain("localStorage", Case.Insensitive);
        combined.ShouldNotContain("sessionStorage", Case.Insensitive);
        combined.ShouldNotContain("access_token", Case.Insensitive);
    }

    [Fact]
    public void Tenant_row_surface_has_no_cursor_etag_logging_or_telemetry_channel()
    {
        string projectRoot = ProjectRoot();
        string grid = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "TenantDataGrid.razor"));
        string navigation = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "State",
            "TenantList",
            "TenantListNavigationContext.cs"));
        string gateway = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Services",
            "Gateways",
            "TenantQueryGateway.cs"));

        grid.ShouldNotContain("Cursor", Case.Insensitive);
        grid.ShouldNotContain("ETag", Case.Insensitive);
        navigation.Split("Cursor = null", StringSplitOptions.None).Length.ShouldBe(3);
        gateway.ShouldNotContain("Activity.Current");
        gateway.ShouldNotContain("SetTag(");

        // Story 1.9 replaces the blanket logger ban with reason-code-only diagnostics. Whether that channel
        // ever discloses protected material is proven at runtime through a capturing logger, not by a source
        // pattern that cannot see LoggerMessage delegates or source-generated partials:
        // TenantQueryGatewayTests.Search_diagnostics_only_ever_emit_support_safe_reason_codes.
    }

    [Fact]
    public async Task Search_paging_never_reaches_browser_storage_and_never_puts_cursors_in_interop_or_the_url()
    {
        List<TenantListRequest> requests = [];
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            return Task.FromResult(request.SearchCursor == "protected-page-two"
                ? AuthoritativeSnapshot(
                    [Row("tenant.two", "Two", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)])
                : AuthoritativeSnapshot(
                    [Row("tenant.one", "One", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)],
                    nextCursor: "protected-page-two",
                    hasMore: true));
        });
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants?search=needle");
        BunitJSModuleInterop clipboard = JSInterop.SetupModule("./js/tenantsClipboard.js");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => requests[^1].SearchCursor.ShouldBe("protected-page-two"));
        cut.Find("[data-testid='tenants-list-previous']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("tenant.one"));

        // Exercise the one JS channel this surface genuinely uses, so the module invocation list below is
        // populated by the surface itself and not only by the control call.
        cut.Find("[data-surface-testid='tenants-list-copy-reference']").Click();
        cut.WaitForAssertion(() => clipboard.Invocations.Count.ShouldBeGreaterThan(0));

        // bUnit can observe calls into an imported module, but it does not execute the module body. Inspect
        // the shipped body as well so a storage write performed internally through indexedDB or cookies
        // cannot hide behind an innocent-looking writeText invocation identifier.
        string clipboardSource = File.ReadAllText(Path.Combine(
            ProjectRoot(),
            "src",
            "Hexalith.Tenants.UI",
            "wwwroot",
            "js",
            "tenantsClipboard.js"));
        clipboardSource.ShouldContain("navigator.clipboard.writeText");
        clipboardSource.ShouldNotContain("localStorage", Case.Insensitive);
        clipboardSource.ShouldNotContain("sessionStorage", Case.Insensitive);
        clipboardSource.ShouldNotContain("indexedDB", Case.Insensitive);
        clipboardSource.ShouldNotContain("document.cookie", Case.Insensitive);
        clipboardSource.ShouldNotContain("cookieStore", Case.Insensitive);

        // Runtime proof rather than a source scan: every JS interop call the surface actually made is
        // inspected for storage sinks and for the protected cursor value. Both channels are scanned. bUnit
        // records module invocations on the module's own handler, not on the root runtime, so a scan of
        // JSInterop.Invocations alone was blind to anything issued through the clipboard module this
        // surface imports -- the only JS module it uses, and therefore the likeliest place for a storage
        // write or a cursor argument to appear.
        List<JSRuntimeInvocation> invocations = [.. JSInterop.Invocations, .. clipboard.Invocations];
        invocations.ShouldAllBe(static invocation
            => !invocation.Identifier.Contains("localStorage", StringComparison.OrdinalIgnoreCase)
            && !invocation.Identifier.Contains("sessionStorage", StringComparison.OrdinalIgnoreCase)
            && !invocation.Identifier.Contains("indexedDB", StringComparison.OrdinalIgnoreCase)
            && !invocation.Identifier.Contains("cookie", StringComparison.OrdinalIgnoreCase));
        invocations
            .SelectMany(static invocation => invocation.Arguments)
            .Select(static argument => argument?.ToString() ?? string.Empty)
            .ShouldAllBe(static argument => !argument.Contains("protected-page-two", StringComparison.Ordinal));
        cut.Markup.ShouldNotContain("protected-page-two", Case.Sensitive);
        navigation.Uri.ShouldNotContain("cursor", Case.Insensitive);
        navigation.Uri.ShouldNotContain("protected-page-two", Case.Sensitive);

        // Control cases: each channel above genuinely does carry values that are present, so the
        // non-disclosure assertions are proven capable of failing rather than passing over an empty or
        // half-empty collection. The identifier channel gets one per interop route, because a module
        // identifier reaching the scanned set is exactly what the root-only version could not show.
        cut.Markup.ShouldContain("tenant.one", Case.Sensitive);
        navigation.Uri.ShouldContain("search=needle", Case.Sensitive);
        IJSObjectReference module = await cut.InvokeAsync(() => Services
            .GetRequiredService<IJSRuntime>()
            .InvokeAsync<IJSObjectReference>("import", "./js/tenantsClipboard.js")
            .AsTask());
        await cut.InvokeAsync(() => module
            .InvokeVoidAsync("tenantsListModuleControlChannel", "control-sentinel-value")
            .AsTask());
        await cut.InvokeAsync(() => Services
            .GetRequiredService<IJSRuntime>()
            .InvokeVoidAsync("tenantsListRootControlChannel", "control-sentinel-value")
            .AsTask());

        List<JSRuntimeInvocation> controlled = [.. JSInterop.Invocations, .. clipboard.Invocations];
        controlled.Select(static invocation => invocation.Identifier)
            .ShouldContain("tenantsListRootControlChannel");
        controlled.Select(static invocation => invocation.Identifier)
            .ShouldContain("tenantsListModuleControlChannel");
        controlled
            .SelectMany(static invocation => invocation.Arguments)
            .Select(static argument => argument?.ToString() ?? string.Empty)
            .ShouldContain("control-sentinel-value");
    }

    [Fact]
    public void List_tests_and_styles_avoid_incidental_fluent_implementation_selectors()
    {
        string projectRoot = ProjectRoot();
        string tests = File.ReadAllText(Path.Combine(
            projectRoot,
            "tests",
            "Hexalith.Tenants.UI.Tests",
            "Components",
            "TenantListSurfaceTests.cs"));
        string styles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "TenantDataGrid.razor.css"));

        tests.ShouldNotContain("[class" + "*=");
        tests.ShouldNotContain("[class" + "^=");
        tests.ShouldNotContain("fluent-data" + "-grid-");
        styles.ShouldNotContain("::" + "part(");
        styles.ShouldNotContain(".fui" + "-");
    }

    [Fact]
    public void Production_resources_do_not_retain_removed_select_sort_controls()
    {
        string resourceRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Resources");
        string resources = File.ReadAllText(Path.Combine(resourceRoot, "TenantsResources.resx"))
            + File.ReadAllText(Path.Combine(resourceRoot, "TenantsResources.fr.resx"));

        resources.ShouldNotContain("Tenants.List.SortLabel");
        resources.ShouldNotContain("Tenants.List.SortDirection");
        resources.ShouldNotContain("Tenants.List.Sort.Name");
        resources.ShouldNotContain("Tenants.List.Sort.Status");
        resources.ShouldNotContain("Tenants.List.Sort.TenantId");
    }

    [Fact]
    public void Styles_preserve_critical_columns_and_forced_colors_hooks()
    {
        string projectRoot = ProjectRoot();
        string styles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "TenantDataGrid.razor.css"));

        styles.ShouldContain("overflow-x: auto");
        styles.ShouldContain("min-width:");
        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain("tenants-critical");
        styles.ShouldContain("grid-template-columns: minmax(0, 1fr) auto");
        styles.ShouldContain("overflow-wrap: anywhere");
        styles.ShouldContain("white-space: break-spaces");
        styles.ShouldNotContain("animation:", Case.Insensitive);
        styles.ShouldNotContain("transition:", Case.Insensitive);
    }

    [Fact]
    public void Renderer_lifecycle_and_event_awaits_stay_on_the_blazor_dispatcher()
    {
        string workspace = File.ReadAllText(Path.Combine(
            ProjectRoot(),
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Pages",
            "TenantsWorkspace.razor"));

        workspace.ShouldNotContain("ConfigureAwait(false)");
    }

    [Fact]
    public void Domain_component_styles_use_direction_safe_layout_properties()
    {
        string componentsRoot = Path.Combine(ProjectRoot(), "src", "Hexalith.Tenants.UI", "Components");
        const string physicalDeclarationPattern =
            @"(?im)(?:^|[;{""']\s*)(?:left|right|margin-(?:left|right)|padding-(?:left|right)|border-(?:left|right)(?:-(?:color|style|width))?|border-(?:top|bottom)-(?:left|right)-radius|float)\s*:|text-align\s*:\s*(?:left|right)\b";
        string[] offenders = Directory
            .GetFiles(componentsRoot, "*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".razor", StringComparison.Ordinal)
                || path.EndsWith(".razor.css", StringComparison.Ordinal))
            .Where(path => System.Text.RegularExpressions.Regex.IsMatch(
                File.ReadAllText(path),
                physicalDeclarationPattern,
                System.Text.RegularExpressions.RegexOptions.CultureInvariant)
                || ContainsAsymmetricPhysicalShorthand(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(componentsRoot, path))
            .ToArray();

        offenders.ShouldBeEmpty("Component layout must use logical properties so RTL remains direction-safe.");
    }

    [Theory]
    [InlineData(".sample { margin: 0 1rem 0 2rem; }")]
    [InlineData(".sample { padding: 1px 2px 3px 4px; }")]
    [InlineData(".sample { border-width: 1px 2px 3px 4px; }")]
    [InlineData(".sample { border-radius: 2px 4px; }")]
    public void Direction_safety_guard_detects_asymmetric_physical_shorthands(string styles)
        => ContainsAsymmetricPhysicalShorthand(styles).ShouldBeTrue();

    private static bool ContainsAsymmetricPhysicalShorthand(string styles)
    {
        const string shorthandPattern =
            @"(?im)(?:^|[;{]\s*)(?<property>margin|padding|border-(?:width|style|color)|border-radius)\s*:\s*(?<value>[^;{}]+)";
        foreach (System.Text.RegularExpressions.Match declaration in System.Text.RegularExpressions.Regex.Matches(
            styles,
            shorthandPattern,
            System.Text.RegularExpressions.RegexOptions.CultureInvariant))
        {
            string property = declaration.Groups["property"].Value;
            string[] values = System.Text.RegularExpressions.Regex.Split(
                    declaration.Groups["value"].Value.Trim(),
                    @"\s+")
                .Where(value => value.Length > 0 && value != "/")
                .ToArray();

            if (string.Equals(property, "border-radius", StringComparison.OrdinalIgnoreCase))
            {
                if (values.Length > 1 && values.Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                {
                    return true;
                }

                continue;
            }

            if (values.Length == 4
                && !string.Equals(values[1], values[3], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // Drives a Fluent UI v5 FluentSelect the way a user selecting an option does: invoking the
    // component's ValueChanged callback. bUnit's .Change() targets the HTML 'onchange' event, but
    // FluentSelect emits its own 'ondropdownchange' event whose args type is internal, so the
    // component instance's callback is the stable, version-independent way to change a select's value.
    private static async Task ChangeSelectAsync(IRenderedComponent<TenantsWorkspace> cut, string testId, string value)
    {
        FluentSelect<string, string> select = cut.FindComponents<FluentSelect<string, string>>()
            .Select(rendered => rendered.Instance)
            .Single(instance => instance.AdditionalAttributes is { } attributes
                && attributes.TryGetValue("data-testid", out object? actual)
                && string.Equals(actual as string, testId, StringComparison.Ordinal));
        await cut.InvokeAsync(() => select.ValueChanged.InvokeAsync(value));
    }

    private static async Task ChangePageSizeAsync(IRenderedComponent<TenantsWorkspace> cut, int value)
    {
        FluentSelect<int, int> select = cut.FindComponents<FluentSelect<int, int>>()
            .Select(rendered => rendered.Instance)
            .Single(instance => instance.AdditionalAttributes is { } attributes
                && attributes.TryGetValue("data-testid", out object? actual)
                && string.Equals(actual as string, "tenants-list-page-size", StringComparison.Ordinal));
        await cut.InvokeAsync(() => select.ValueChanged.InvokeAsync(value));
    }

    // Drives the FluentTextInput search box by invoking its ValueChanged callback directly. The box uses
    // Immediate/ImmediateDelay (JS-debounced oninput) which bUnit's DOM-event helpers cannot exercise, so
    // the component instance's callback is the stable way to simulate a debounced search term.
    private static async Task ChangeSearchAsync(IRenderedComponent<TenantsWorkspace> cut, string value)
    {
        FluentTextInput search = cut.FindComponents<FluentTextInput>()
            .Select(rendered => rendered.Instance)
            .Single(instance => instance.AdditionalAttributes is { } attributes
                && attributes.TryGetValue("data-testid", out object? actual)
                && string.Equals(actual as string, "tenants-list-search", StringComparison.Ordinal));
        await cut.InvokeAsync(() => search.ValueChanged.InvokeAsync(value));
    }

    [Fact]
    public void Workspace_refuses_to_render_without_the_circuit_scoped_paging_service()
    {
        // The "resolved as a required service so a dropped registration fails loudly" contract was carried
        // by a code comment only. Every fixture pre-registers the service, and the composition tests call
        // GetRequiredService themselves on a raw container with no component involved, so reverting the
        // component to GetService(...) ?? new TenantSearchPagingState() -- which silently degrades to
        // per-component paging that cannot survive tenant-detail navigation -- broke nothing.
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ReadySnapshot([])));
        Services.AddSingleton(gateway);
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        userContext.UserId.Returns("operator-user");
        Services.AddSingleton(userContext);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        // Deliberately no AddScoped<TenantSearchPagingState>().
        // The message is asserted, not just the type: this hand-rolled fixture throws
        // InvalidOperationException for many unrelated reasons (a missing Fluent registration, a missing
        // localizer, a strict-mode interop failure), so a bare type assertion could pass while proving
        // nothing about the paging service.
        Should.Throw<InvalidOperationException>(() => Render<TenantsWorkspace>())
            .Message.ShouldContain(nameof(TenantSearchPagingState));
    }

    [Fact]
    public async Task Empty_search_page_reset_button_actually_clears_the_search()
    {
        // The button was rendered with OnClick="OnReset" while the only SearchPageEmpty call site never
        // passed OnReset, so it bound an unset EventCallback and every click was a silent no-op. Both prior
        // tests only asserted the element existed, which Find() already guarantees.
        List<string?> searches = [];
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            searches.Add(request.Search);
            return Task.FromResult(request.Search is null
                ? ReadySnapshot([Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)])
                : SearchPageEmptySnapshot(hasMore: false));
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        await ChangeSearchAsync(cut, "nomatch");
        cut.WaitForElement("[data-testid='tenants-list-search-page-empty']");
        searches.ShouldContain("nomatch");

        cut.Find("[data-testid='tenants-list-search-page-empty'] [data-testid='tenants-list-state-reset']").Click();

        // The operator is returned to the full authorized list, and the term is gone from state and URL.
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        searches[^1].ShouldBeNull();
        Services.GetRequiredService<NavigationManager>().Uri.ShouldNotContain("search=");
    }

    [Fact]
    public async Task A_second_pager_click_during_an_in_flight_load_is_refused()
    {
        // The in-flight guard was asserted only by its own comment: every pager interaction in this file is
        // followed by a WaitForAssertion before the next one, so nothing drove the double click the guard
        // exists for. Without it the second click recorded a second back-step against a stale cursor.
        TaskCompletionSource<TenantListSnapshot> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int callCount = 0;
        RegisterServices(_ =>
        {
            callCount++;
            return callCount == 1
                ? Task.FromResult(AuthoritativeSnapshot(
                    [Row("tenant.page-one", "One", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)],
                    nextCursor: "protected-page-two",
                    hasMore: true))
                : pending.Task;
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        TenantSearchPagingState paging = Services.GetRequiredService<TenantSearchPagingState>();

        // The pager unmounts for the duration of a load, so the callback is captured while it is still
        // mounted and then invoked twice. The first invocation blocks on the outstanding load; the second is
        // delivered while _pagingInFlight is set, which is exactly the race the guard exists for.
        EventCallback<MouseEventArgs> next = cut.FindComponents<FluentButton>()
            .Select(rendered => rendered.Instance)
            .Single(instance => instance.AdditionalAttributes is { } attributes
                && attributes.TryGetValue("data-testid", out object? actual)
                && string.Equals(actual as string, "tenants-list-next", StringComparison.Ordinal))
            .OnClick;

        Task first = cut.InvokeAsync(() => next.InvokeAsync(new MouseEventArgs()));
        cut.WaitForAssertion(() => callCount.ShouldBe(2));

        // A refused click completes immediately. WaitAsync supplies only a failure bound: if the click is
        // wrongly accepted it remains coupled to the deliberately outstanding load and the test fails with
        // a timeout instead of depending on an arbitrary scheduler delay.
        Task second = cut.InvokeAsync(() => next.InvokeAsync(new MouseEventArgs()));
        await second.WaitAsync(TimeSpan.FromSeconds(5));

        callCount.ShouldBe(2, "the second click must be refused while a load is in flight");
        second.IsCompleted.ShouldBeTrue("a refused click returns without starting a load");
        paging.SearchCursor.ShouldBe("protected-page-two");

        pending.SetResult(AuthoritativeSnapshot(
            [Row("tenant.page-two", "Two", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)]));
        await first;
        cut.WaitForAssertion(() => callCount.ShouldBe(2));

        // Exactly one back-step was recorded, so Previous returns to page one rather than to page two.
        paging.TryMovePrevious(authoritative: true).ShouldBeTrue();
        paging.SearchCursor.ShouldBeNull();
        paging.HasPrevious(authoritative: true).ShouldBeFalse();
    }

    [Fact]
    public void Next_is_disabled_when_the_list_reports_more_results_without_a_cursor()
    {
        // The ordinary list path passes the server's HasMore and Cursor through independently with no
        // consistency check, and enablement read HasMore alone while the handler requires a cursor. The
        // result was a live Next whose every click did nothing: no load, no re-render, no notice.
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)],
            nextCursor: null,
            hasMore: true));

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        cut.Find("[data-testid='tenants-list-next']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task An_over_long_search_term_is_reported_instead_of_silently_dropped()
    {
        // Normalization returns null past the bound, so the term was dropped, the input the operator was
        // typing into was blanked, the canonical URL lost the parameter and the unfiltered list loaded --
        // with no notice, the only silent degradation in this feature.
        List<string?> searches = [];
        RegisterServices(call =>
        {
            searches.Add(call.ArgAt<TenantListRequest>(0).Search);
            return Task.FromResult(ReadySnapshot(
                [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)]));
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        await ChangeSearchAsync(cut, new string('a', 257));

        cut.WaitForElement("[data-testid='tenants-list-search-term-too-long-notice']");
        searches[^1].ShouldBeNull();

        // A term inside the bound is applied and carries no notice.
        await ChangeSearchAsync(cut, new string('a', 256));
        cut.WaitForAssertion(() => searches[^1].ShouldBe(new string('a', 256)));
        cut.FindAll("[data-testid='tenants-list-search-term-too-long-notice']").ShouldBeEmpty();
    }

    [Fact]
    public void Over_long_parameter_navigation_is_recomputed_and_the_notice_is_not_reused_by_a_later_load()
    {
        int callCount = 0;
        List<string?> searches = [];
        RegisterServices(call =>
        {
            callCount++;
            searches.Add(call.ArgAt<TenantListRequest>(0).Search);
            return Task.FromResult(ReadySnapshot(
                [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)]));
        });
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        navigation.NavigateTo($"/tenants?search={new string('a', 257)}");

        cut.WaitForElement("[data-testid='tenants-list-search-term-too-long-notice']");
        searches[^1].ShouldBeNull();
        navigation.Uri.ShouldNotContain("search=", Case.Insensitive);
        int callsAfterRejection = callCount;

        navigation.NavigateTo("/tenants?sort=name");

        cut.WaitForAssertion(() => callCount.ShouldBeGreaterThan(callsAfterRejection));
        cut.FindAll("[data-testid='tenants-list-search-term-too-long-notice']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(TenantListSurfaceKind.Error)]
    [InlineData(TenantListSurfaceKind.Unauthorized)]
    public void Over_long_query_never_claims_the_authorized_list_is_shown_on_a_terminal_surface(
        TenantListSurfaceKind kind)
    {
        TenantListSnapshot terminal = kind switch
        {
            TenantListSurfaceKind.Error => TenantListSnapshot.Error(),
            TenantListSurfaceKind.Unauthorized => TenantListSnapshot.Unauthorized(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
        RegisterServices(terminal);
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            $"/tenants?search={new string('a', 257)}");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement(kind is TenantListSurfaceKind.Error
            ? "[data-testid='tenants-list-error']"
            : "[data-testid='tenants-list-unauthorized']");

        cut.FindAll("[data-testid='tenants-list-search-term-too-long-notice']").ShouldBeEmpty();
    }

    [Fact]
    public void The_search_input_bounds_its_own_length_so_the_rejection_is_unreachable_by_typing()
    {
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)]));

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        FluentTextInput search = cut.FindComponents<FluentTextInput>()
            .Select(rendered => rendered.Instance)
            .Single(instance => instance.AdditionalAttributes is { } attributes
                && attributes.TryGetValue("data-testid", out object? actual)
                && string.Equals(actual as string, "tenants-list-search", StringComparison.Ordinal));

        search.MaxLength.ShouldBe(256);
    }

    [Fact]
    public void Prerender_offers_no_previous_page_from_another_requests_retained_history()
    {
        // Retained protected paging belongs to the interactive circuit. The prerender pass must not offer a
        // Previous control backed by it: the guard was added but the one static-render test asserted only
        // requests and cursors, never the button's disabled state, so deleting the guard failed nothing.
        RegisterServices(AuthoritativeSnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)]));
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");
        TenantSearchPagingState paging = Services.GetRequiredService<TenantSearchPagingState>();
        paging.MoveNext(authoritative: true, "protected-page-two").ShouldBeTrue();
        SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task A_pending_recovery_notice_survives_the_disposal_a_detail_navigation_causes()
    {
        // Dispose deliberately stopped clearing the pending scope, but nothing pinned that: re-adding the
        // clear was green. The regression it guards is real -- notice armed, load fails, operator opens a
        // tenant and returns, and the search has silently restarted at page one with no explanation.
        int callCount = 0;
        RegisterServices(_ =>
        {
            callCount++;
            return Task.FromResult(callCount == 1
                ? TenantListSnapshot.Error()
                : AuthoritativeSnapshot(
                    [Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Unknown, TenantPendingState.Unknown)]));
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            "/tenants?search=needle&selected=tenant.previous-page&anchor=tenant-row-tenant.previous-page");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-error']");
        TenantSearchPagingState paging = Services.GetRequiredService<TenantSearchPagingState>();
        paging.PendingRecoveryScope.ShouldNotBeNull();

        // The component is destroyed exactly as a tenant-detail navigation destroys it.
        await DisposeComponentsAsync();
        paging.PendingRecoveryScope.ShouldNotBeNull();

        // A fresh instance on the same scope must still deliver the owed copy.
        IRenderedComponent<TenantsWorkspace> returned = Render<TenantsWorkspace>();
        returned.WaitForElement("[data-testid='tenants-list-search-refreshed-notice']");
    }

    private void RegisterServices(TenantListSnapshot snapshot)
        => RegisterServices(_ => Task.FromResult(snapshot));

    private void RegisterServices(Func<NSubstitute.Core.CallInfo, Task<TenantListSnapshot>> resultFactory)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(resultFactory);
        Services.AddSingleton(gateway);
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        userContext.UserId.Returns("operator-user");
        Services.AddSingleton(userContext);
        Services.AddScoped<TenantSearchPagingState>();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private static TenantListSnapshot ReadySnapshot(
        IReadOnlyList<TenantListRow> rows,
        string? nextCursor = null,
        bool hasMore = false)
        => TenantListSnapshot.Ready(
            rows,
            nextCursor,
            hasMore,
            eTag: "\"etag\"",
            freshness: rows.Any(row => row.Freshness == ReadModelFreshnessState.Stale)
                ? ReadModelFreshnessState.Stale
                : ReadModelFreshnessState.Current,
            isDegraded: false);

    // Both notices share exactly one polite live region: two sibling regions announcing together are
    // routinely coalesced or dropped by screen readers.
    private static void AssertSingleNoticeLiveRegion(IRenderedComponent<TenantsWorkspace> cut)
    {
        var region = cut.FindAll("[data-testid='tenants-list-notices']").ShouldHaveSingleItem();
        region.GetAttribute("aria-live").ShouldBe("polite");
        region.GetAttribute("role").ShouldBe("status");
        region.GetAttribute("aria-atomic").ShouldBe("true");

        // Every rendered notice bar must live inside that one region, so the announcement is made once.
        // Only rendered markup is inspected: a Fluent component's shadow root is never produced by bUnit,
        // so asserting attributes that live there could not fail and would certify nothing.
        cut.FindAll("[data-testid$='-notice']").Count
            .ShouldBe(cut.FindAll("[data-testid='tenants-list-notices'] [data-testid$='-notice']").Count);
    }

    private static TenantListSnapshot AuthoritativeSnapshot(
        IReadOnlyList<TenantListRow> rows,
        string? nextCursor = null,
        bool hasMore = false)
        => ReadySnapshot(rows, nextCursor, hasMore) with
        {
            IsAuthoritativeSearch = true,
        };

    /// <summary>Mirrors what the gateway emits for an authoritative search page with no visible rows.</summary>
    private static TenantListSnapshot SearchPageEmptySnapshot(
        bool hasMore,
        string? nextCursor = null,
        TenantListReason reason = TenantListReason.None)
        => new(
            TenantListSurfaceKind.SearchPageEmpty,
            [],
            hasMore && nextCursor is null ? "protected-next" : nextCursor,
            hasMore,
            ETag: null,
            ReadModelFreshnessState.Unknown,
            IsDegraded: false,
            IsAuthorizationScopedEmpty: true,
            Reason: reason,
            IsAuthoritativeSearch: true);

    private static TenantListRow Row(
        string tenantId,
        string name,
        TenantStatus status,
        ReadModelFreshnessState freshness,
        TenantPendingState pendingState)
        => TenantListRow.FromSummary(new TenantSummary(tenantId, name, status)) with
        {
            MemberCount = TenantCountValue.Known(2),
            OwnerCount = TenantCountValue.Known(1),
            PendingState = pendingState,
            Freshness = freshness,
        };

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        public LocalizedString this[string name] => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));

        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            // Without these, ProjectionLifecycleBadge rendered the literal resource key back through this
            // echoing stub, so the badge's label was never really asserted and only its CSS class was --
            // which project rules forbid relying on alone.
            ["Tenants.ProjectionLifecycle.Current"] = "Current",
            ["Tenants.ProjectionLifecycle.Stale"] = "Stale",
            ["Tenants.ProjectionLifecycle.Unknown"] = "Unknown",
            ["Tenants.ProjectionLifecycle.Rebuilding"] = "Rebuilding",
            ["Tenants.ProjectionLifecycle.Degraded"] = "Degraded",
            ["Tenants.ProjectionLifecycle.Unavailable"] = "Unavailable",
            ["Tenants.ProjectionLifecycle.LocalOnly"] = "Local only",
            ["Tenants.List.Title"] = "Tenants",
            ["Tenants.List.SearchLabel"] = "Search tenants",
            ["Tenants.List.SearchPlaceholder"] = "Search by tenant id or name",
            ["Tenants.List.PageSizeLabel"] = "Tenants per page",
            ["Tenants.List.PageSizeOption"] = "{0:N0} tenants",
            ["Tenants.List.StatusFilterLabel"] = "Status on current page",
            ["Tenants.List.PageLocalSemantics"] = "Status filters and column sorting apply to the current authorized page only.",
            ["Tenants.List.StatusFilter.All"] = "All statuses",
            ["Tenants.List.StatusFilter.Active"] = "Active",
            ["Tenants.List.StatusFilter.Disabled"] = "Disabled",
            ["Tenants.List.StatusFilter.Unknown"] = "Unknown",
            ["Tenants.List.Status.Active"] = "Active",
            ["Tenants.List.Status.Disabled"] = "Disabled",
            ["Tenants.List.Status.Unknown"] = "Unknown",
            ["Tenants.List.Refresh"] = "Refresh",
            ["Tenants.List.Reset"] = "Reset filters",
            ["Tenants.List.Previous"] = "Previous",
            ["Tenants.List.Next"] = "Next",
            ["Tenants.List.Column.Tenant"] = "Tenant",
            ["Tenants.List.Column.Status"] = "Status",
            ["Tenants.List.Column.Members"] = "Members",
            ["Tenants.List.Column.Owners"] = "Owners",
            ["Tenants.List.Column.Pending"] = "Pending",
            ["Tenants.List.Column.Freshness"] = "Freshness",
            ["Tenants.List.Count.Unknown"] = "Unknown",
            ["Tenants.List.DetailLinkLabel"] = "Open tenant details for {0}",
            ["Tenants.List.Pending.None"] = "No pending changes",
            ["Tenants.List.Pending.Unknown"] = "Pending state unknown",
            ["Tenants.List.Freshness.Current"] = "Current",
            ["Tenants.List.Freshness.Refreshing"] = "Refreshing",
            ["Tenants.List.Freshness.Aging"] = "Aging",
            ["Tenants.List.Freshness.Stale"] = "Stale",
            ["Tenants.List.Freshness.Unknown"] = "Unknown",
            ["Tenants.List.Notice.ListRefreshed"] = "The requested page was no longer available. The authorized first page has been refreshed.",
            ["Tenants.List.Notice.SearchUnavailable"] = "Protected whole-set search is temporarily unavailable. You can continue browsing the authorized tenant list.",
            ["Tenants.List.Notice.SearchRefreshed"] = "Protected search paging could not be restored. Search has restarted from the first page.",
            ["Tenants.List.Notice.SearchAndListUnavailable"] = "Protected whole-set search is temporarily unavailable, and the authorized tenant list could not be loaded either. Try again later.",
            ["Tenants.List.Notice.SearchTermTooLong"] = "The search term was too long to apply, so the full authorized tenant list is shown. Shorten the term and search again.",
            ["Tenants.List.Notice.SearchPagingRestarted"] = "The available tenant source changed. Paging restarted from the first page.",
            ["Tenants.List.State.SearchPageEmpty.Title"] = "No tenants match this search",
            ["Tenants.List.State.SearchPageEmpty.Message"] = "No tenants you can access match this search. Check the search term, or clear it to return to the full list.",
            ["Tenants.List.State.SearchPageEmpty.MoreTitle"] = "No visible tenants on this search page",
            ["Tenants.List.State.SearchPageEmpty.MoreMessage"] = "No authorized tenant results are visible on this page. Continue to the next search page to check for more results, or clear the search to return to the full list.",
            ["Tenants.List.StatusFilterLabel.Authoritative"] = "Status across indexed candidates",
            ["Tenants.List.AuthoritativeSearchSemantics"] = "Search and status apply across indexed tenant candidates. Only authorized, verified tenant rows are shown; sorting applies within this protected page.",
            ["Tenants.List.State.Loading.Title"] = "Loading tenants",
            ["Tenants.List.State.Loading.Message"] = "Tenant data is loading from the server-side query gateway.",
            ["Tenants.List.State.Empty.Title"] = "No visible tenants",
            ["Tenants.List.State.Empty.Message"] = "No tenants are visible for this operator. This is an authorized empty result, not a failure.",
            ["Tenants.List.State.FilteredEmpty.Title"] = "No tenants match filters",
            ["Tenants.List.State.FilteredEmpty.Message"] = "No visible tenants on the current authorized page match the active filters.",
            ["Tenants.List.State.Error.Title"] = "Tenants unavailable",
            ["Tenants.List.State.Error.Message"] = "Tenant data could not be loaded. The list is unavailable until the server-side query gateway is reachable.",
            ["Tenants.List.State.Stale.Title"] = "Tenant data is stale",
            ["Tenants.List.State.Stale.Message"] = "The latest freshness evidence says this list is stale. Refresh to check the projection again.",
            ["Tenants.List.State.Degraded.Title"] = "Tenant data is degraded",
            ["Tenants.List.State.Degraded.Message"] = "Some tenant evidence is unavailable. Visible tenant identity, lifecycle, pending, and truth-state columns remain shown.",
            ["Tenants.List.Reason.GatewayUnavailable"] = "The authorized tenant list could not be loaded. Try again later.",
            ["Tenants.List.Reason.NotModifiedWithoutSnapshot"] = "The list could not be verified because no confirmed server snapshot is available. Refresh to try again.",
            ["Tenants.List.Reason.ProjectionDegraded"] = "Projection freshness is unavailable. Authorized tenant rows that could be verified remain usable.",
            ["Tenants.List.Reason.RowEnrichmentUnavailable"] = "Member or owner counts are unavailable. Authorized tenant identity and lifecycle state remain usable.",
            ["Tenants.List.Reason.SearchPartiallyAvailable"] = "Some search results could not be verified. Only authorized tenant rows that were verified are shown.",
            ["Tenants.Copy.Action"] = "Copy",
            ["Tenants.Copy.Label.TenantId"] = "Copy tenant identifier {0}",
            ["Tenants.Copy.Feedback.Copied"] = "Copied.",
            ["Tenants.Copy.Feedback.Disconnected"] = "Clipboard disconnected. Copy was not completed. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Empty"] = "Nothing is available to copy.",
            ["Tenants.Copy.Feedback.Failed"] = "Copy failed. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Unavailable"] = "Clipboard unavailable. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Unsafe"] = "This value is not support-safe to copy.",
        };
    }
}
