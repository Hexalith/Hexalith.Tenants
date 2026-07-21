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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TenantListSurfaceTests : BunitContext
{
    [Fact]
    public void Workspace_renders_grid_controls_stable_selectors_and_truth_state()
    {
        TenantListSnapshot snapshot = ReadySnapshot(
            [
                Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Stale, TenantPendingState.None),
            ],
            nextCursor: "next-cursor",
            hasMore: true);
        RegisterServices(snapshot);
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler writeHandler = module.SetupVoid("writeText", "tenant.alpha").SetVoidResult();

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        cut.Find("[data-testid='tenants-list-refresh']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-list-search']").NodeName.ShouldBe("FLUENT-TEXT-INPUT");
        cut.Find("[data-testid='tenants-list-reset']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-list-sort-tenant']").Closest("fluent-button").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-list-sort-status']").Closest("fluent-button").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-list-detail-link']").GetAttribute("href").ShouldNotBeNull().ShouldContain("/tenants/tenant.alpha");
        cut.Find("[data-testid='tenants-list-copy-reference']").GetAttribute("data-copy-kind").ShouldBe("TenantId");
        cut.Find("[data-testid='tenants-list-copy-reference']").TextContent.ShouldContain("Copy");
        cut.Find("[data-testid='tenants-copy-reference']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-list-truth-state']").TextContent.ShouldContain("Stale");
        cut.Markup.ShouldContain("tenant.alpha");
        cut.Markup.ShouldContain("No pending changes");

        cut.Find("[data-surface-testid='tenants-list-copy-reference']").Click();
        cut.WaitForAssertion(() => writeHandler.Invocations.Count.ShouldBe(1));
        writeHandler.Invocations.Single().Arguments[0].ShouldBe("tenant.alpha");
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
        cut.Find("[data-testid='tenants-list-refreshed-notice']").GetAttribute("aria-live").ShouldBe("polite");
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
    public async Task Search_returning_no_matches_renders_filtered_empty_surface()
    {
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            return Task.FromResult(string.IsNullOrWhiteSpace(request.Search)
                ? ReadySnapshot([Row("tenant.alpha", "Alpha", TenantStatus.Active, ReadModelFreshnessState.Current, TenantPendingState.None)])
                : TenantListSnapshot.FilteredEmpty());
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        await ChangeSearchAsync(cut, "nomatch");

        cut.WaitForElement("[data-testid='tenants-list-filtered-empty']");
        cut.Find("[data-testid='tenants-list-filtered-empty']").GetAttribute("role").ShouldNotBeNull();
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
    [InlineData(TenantListSurfaceKind.Error, "tenants-list-error", "alert", "assertive")]
    [InlineData(TenantListSurfaceKind.Stale, "tenants-list-stale", "status", "polite")]
    [InlineData(TenantListSurfaceKind.Degraded, "tenants-list-degraded", "alert", "assertive")]
    public void Workspace_renders_distinct_six_list_states(
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
        cut.Markup.ShouldContain("No confirmed server snapshot is available.");
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
        gateway.ShouldNotContain("ILogger<TenantQueryGateway>");
        gateway.ShouldNotContain("Activity.Current");
        gateway.ShouldNotContain("SetTag(");
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
            ["Tenants.List.SortLabel"] = "Sort",
            ["Tenants.List.Sort.TenantId"] = "Tenant id",
            ["Tenants.List.Sort.Name"] = "Name",
            ["Tenants.List.Sort.Status"] = "Status",
            ["Tenants.List.SortDirectionLabel"] = "Sort direction",
            ["Tenants.List.SortDirection.Ascending"] = "Ascending",
            ["Tenants.List.SortDirection.Descending"] = "Descending",
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
            ["Tenants.List.State.Loading.Title"] = "Loading tenants",
            ["Tenants.List.State.Loading.Message"] = "Tenant data is loading.",
            ["Tenants.List.State.Empty.Title"] = "No visible tenants",
            ["Tenants.List.State.Empty.Message"] = "No tenants are visible for this operator.",
            ["Tenants.List.State.FilteredEmpty.Title"] = "No tenants match filters",
            ["Tenants.List.State.FilteredEmpty.Message"] = "Reset filters to see visible tenants.",
            ["Tenants.List.State.Error.Title"] = "Tenants unavailable",
            ["Tenants.List.State.Error.Message"] = "Tenant data could not be loaded.",
            ["Tenants.List.State.Stale.Title"] = "Tenant data is stale",
            ["Tenants.List.State.Stale.Message"] = "Refresh to check the latest projection.",
            ["Tenants.List.State.Degraded.Title"] = "Tenant data is degraded",
            ["Tenants.List.State.Degraded.Message"] = "Some tenant evidence is unavailable.",
            ["Tenants.List.Reason.GatewayUnavailable"] = "The authorized tenant list could not be loaded. Try again later.",
            ["Tenants.List.Reason.NotModifiedWithoutSnapshot"] = "No confirmed server snapshot is available. Refresh to try again.",
            ["Tenants.List.Reason.ProjectionDegraded"] = "Projection freshness is unavailable. Authorized tenant rows remain usable.",
            ["Tenants.List.Reason.RowEnrichmentUnavailable"] = "Member or owner counts are unavailable. Authorized tenant identity and lifecycle state remain usable.",
            ["Tenants.Copy.Action"] = "Copy",
            ["Tenants.Copy.Label.TenantId"] = "Copy tenant identifier {0}",
            ["Tenants.Copy.Feedback.Copied"] = "Copied.",
            ["Tenants.Copy.Feedback.Disconnected"] = "Clipboard disconnected. Copy was not completed.",
            ["Tenants.Copy.Feedback.Empty"] = "Nothing is available to copy.",
            ["Tenants.Copy.Feedback.Failed"] = "Copy failed.",
            ["Tenants.Copy.Feedback.Unavailable"] = "Clipboard unavailable.",
            ["Tenants.Copy.Feedback.Unsafe"] = "This value is not support-safe to copy.",
        };
    }
}
