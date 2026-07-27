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

using Microsoft.AspNetCore.Components;
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
    public async Task Empty_search_page_copy_is_identical_whether_the_window_was_hidden_or_matched_nothing()
    {
        // Superseded the "promises later results" split: an authoritative window that yields no authorized
        // row now ends paging, so there is no non-final variant to promise anything. What must be proven
        // instead is that the two causes are indistinguishable, since the difference was the disclosure.
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
        string hiddenTitle = cut.Find("[data-testid='tenants-list-search-page-empty'] h2").TextContent;
        string hiddenMessage = cut.Find("[data-testid='tenants-list-search-page-empty'] p").TextContent;

        hiddenTitle.ShouldBe("No tenants match this search");
        hiddenMessage.ShouldBe(
            "No tenants you can access match this search. "
            + "Check the search term, or clear it to return to the full list.");

        // The copy must never state or imply that rows failed verification on this page: that is a claim
        // about hidden candidates, and it is false for the dominant no-match case.
        hiddenMessage.ShouldNotContain("verified");
        hiddenMessage.ShouldNotContain("later pages", Case.Insensitive);

        // The terminal window renders the same copy, so the operator cannot tell the causes apart.
        hasMore = false;
        cut.Find("[data-testid='tenants-list-refresh']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-list-search-page-empty'] h2").TextContent.ShouldBe(hiddenTitle);
            cut.Find("[data-testid='tenants-list-search-page-empty'] p").TextContent.ShouldBe(hiddenMessage);
        });

        // The page is terminal for this search, so it must offer the way back to the full list.
        cut.Find("[data-testid='tenants-list-state-reset']").ShouldNotBeNull();
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

            // Loads 1-2 run in the starting mode, load 3 crosses the boundary, and load 4 crosses back, so a
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
            return Task.FromResult(authoritative
                ? AuthoritativeSnapshot([row], nextCursor: "protected-next", hasMore: true)
                : ReadySnapshot([row], nextCursor: "ordinary-next", hasMore: true) with
                {
                    Notice = TenantListReason.SearchUnavailable,
                });
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        TenantSearchPagingState paging = Services.GetRequiredService<TenantSearchPagingState>();

        // Page two in the starting mode establishes retained history and a cursor for that mode only.
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => callCount.ShouldBe(2));
        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeFalse();
        (startAuthoritative ? paging.SearchCursor : paging.FallbackCursor).ShouldNotBeNull();
        (startAuthoritative ? paging.FallbackCursor : paging.SearchCursor).ShouldBeNull();

        // Load three crosses the boundary; the incoming mode starts from its own first page and the retired
        // mode is cleared, with a mapped localized explanation rather than a silent jump backwards.
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => callCount.ShouldBe(3));

        // A load that reports "paging restarted from the first page" must itself have been issued for the
        // first page: the crossing request may not carry the incoming mode's retained cursor.
        (startAuthoritative ? requests[2].Cursor : requests[2].SearchCursor).ShouldBeNull();

        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
        paging.SearchCursor.ShouldBeNull();
        paging.FallbackCursor.ShouldBeNull();
        cut.Find("[data-testid='tenants-list-search-paging-restarted-notice']").TextContent.ShouldContain(
            "The available tenant source changed. Paging restarted from the first page.");
        AssertSingleNoticeLiveRegion(cut);

        // Crossing back later must not resurrect the original mode's retained cursor: the request that
        // returns to it carries no cursor for that mode, so the operator cannot be teleported to a stale page.
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => callCount.ShouldBe(4));

        (startAuthoritative ? requests[^1].SearchCursor : requests[^1].Cursor).ShouldBeNull();
        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
        paging.SearchCursor.ShouldBeNull();
        paging.FallbackCursor.ShouldBeNull();
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
            // This surface tells the operator that later pages of the same search may still hold results,
            // so it must not also offer an action that ends that search.
            cut.Find($"[data-testid='{selector}'] h2").TextContent
                .ShouldBe("No tenants match this search");
            cut.Find($"[data-testid='{selector}'] p").TextContent
                .ShouldBe(
                    "No tenants you can access match this search. "
                    + "Check the search term, or clear it to return to the full list.");

            // The page can no longer promise later results, so it must offer the way out.
            cut.Find("[data-testid='tenants-list-state-reset']").ShouldNotBeNull();
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

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        cut.Find("[data-testid='tenants-list-next']").Click();
        cut.WaitForAssertion(() => requests[^1].SearchCursor.ShouldBe("protected-page-two"));
        cut.Find("[data-testid='tenants-list-previous']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("tenant.one"));

        // Runtime proof rather than a source scan: every JS interop call the surface actually made is
        // inspected for storage sinks and for the protected cursor value.
        JSInterop.Invocations.ShouldAllBe(static invocation
            => !invocation.Identifier.Contains("localStorage", StringComparison.OrdinalIgnoreCase)
            && !invocation.Identifier.Contains("sessionStorage", StringComparison.OrdinalIgnoreCase)
            && !invocation.Identifier.Contains("cookie", StringComparison.OrdinalIgnoreCase));
        JSInterop.Invocations
            .SelectMany(static invocation => invocation.Arguments)
            .Select(static argument => argument?.ToString() ?? string.Empty)
            .ShouldAllBe(static argument => !argument.Contains("protected-page-two", StringComparison.Ordinal));
        cut.Markup.ShouldNotContain("protected-page-two", Case.Sensitive);
        navigation.Uri.ShouldNotContain("cursor", Case.Insensitive);
        navigation.Uri.ShouldNotContain("protected-page-two", Case.Sensitive);

        // Control cases: each of the three channels above genuinely does carry values that are present, so
        // the non-disclosure assertions are proven capable of failing rather than passing vacuously.
        cut.Markup.ShouldContain("tenant.one", Case.Sensitive);
        navigation.Uri.ShouldContain("search=needle", Case.Sensitive);
        await cut.InvokeAsync(() => Services
            .GetRequiredService<IJSRuntime>()
            .InvokeVoidAsync("tenantsListControlChannel", "control-sentinel-value")
            .AsTask());
        JSInterop.Invocations
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
    private static TenantListSnapshot SearchPageEmptySnapshot(bool hasMore, string? nextCursor = null)
        => new(
            TenantListSurfaceKind.SearchPageEmpty,
            [],
            nextCursor,
            hasMore,
            ETag: null,
            ReadModelFreshnessState.Unknown,
            IsDegraded: false,
            IsAuthorizationScopedEmpty: true,
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
            ["Tenants.List.Column.Freshness"] = "Truth state",
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
            ["Tenants.List.Notice.SearchRefreshed"] = "The protected search page was no longer available. Search has restarted from the first page.",
            ["Tenants.List.Notice.SearchPagingRestarted"] = "The available tenant source changed. Paging restarted from the first page.",
            ["Tenants.List.State.SearchPageEmpty.Title"] = "No tenants match this search",
            ["Tenants.List.State.SearchPageEmpty.Message"] = "No tenants you can access match this search. Check the search term, or clear it to return to the full list.",
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
