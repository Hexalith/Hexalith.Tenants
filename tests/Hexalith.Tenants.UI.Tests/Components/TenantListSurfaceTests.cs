using System.Globalization;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TruthState;

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
                Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantFreshnessState.Stale, TenantPendingState.None),
            ],
            nextCursor: "next-cursor",
            hasMore: true);
        RegisterServices(snapshot);

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        cut.Find("[data-testid='tenants-list-refresh']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-list-search']").NodeName.ShouldBe("FLUENT-TEXT-INPUT");
        cut.Find("[data-testid='tenants-list-reset']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-list-detail-link']").GetAttribute("href").ShouldNotBeNull().ShouldContain("/tenants/tenant.alpha");
        cut.Find("[data-testid='tenants-list-copy-reference']").GetAttribute("data-copy-kind").ShouldBe("TenantId");
        cut.Find("[data-testid='tenants-list-copy-reference']").TextContent.ShouldContain("Copy");
        cut.Find("[data-testid='tenants-copy-reference']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-list-truth-state']").TextContent.ShouldContain("Stale");
        cut.Markup.ShouldContain("tenant.alpha");
        cut.Markup.ShouldContain("No pending changes");
    }

    [Fact]
    public async Task Search_filter_and_sort_preserve_safety_markers()
    {
        TenantListRow alpha = Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantFreshnessState.Current, TenantPendingState.None);
        TenantListRow beta = Row("tenant.beta", "Beta", TenantStatus.Disabled, TenantFreshnessState.Stale, TenantPendingState.Unknown);

        // Search is now a server round-trip: a "beta" term returns the beta-only cross-set match-set; an
        // empty term returns the full cursor list. Status stays a page-local filter applied client-side.
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            IReadOnlyList<TenantListRow> rows = string.Equals(request.Search, "beta", StringComparison.OrdinalIgnoreCase)
                ? [beta]
                : [alpha, beta];
            return Task.FromResult(ReadySnapshot(rows));
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        await ChangeSearchAsync(cut, "beta");

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("tenant.beta");
            cut.Markup.ShouldNotContain("tenant.alpha");
        });
        cut.Find("[data-testid='tenants-list-truth-state']").TextContent.ShouldContain("Stale");
        cut.Markup.ShouldContain("Pending state unknown");

        cut.Find("[data-testid='tenants-list-reset']").Click();
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
                [Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantFreshnessState.Current, TenantPendingState.None)]));
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        await ChangeSearchAsync(cut, "acme");

        cut.WaitForAssertion(() => requests.ShouldContain(r => r.Search == "acme"));
    }

    [Fact]
    public async Task Search_returning_no_matches_renders_filtered_empty_surface()
    {
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            return Task.FromResult(string.IsNullOrWhiteSpace(request.Search)
                ? ReadySnapshot([Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantFreshnessState.Current, TenantPendingState.None)])
                : TenantListSnapshot.FilteredEmpty());
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        await ChangeSearchAsync(cut, "nomatch");

        cut.WaitForElement("[data-testid='tenants-list-filtered-empty']");
        cut.Find("[data-testid='tenants-list-filtered-empty']").GetAttribute("role").ShouldNotBeNull();
    }

    [Fact]
    public async Task Search_unavailable_degraded_snapshot_renders_non_blocking_surface_over_the_list()
    {
        const string unavailable = "Tenant search is temporarily unavailable; showing the full tenant list.";
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            return Task.FromResult(string.IsNullOrWhiteSpace(request.Search)
                ? ReadySnapshot([Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantFreshnessState.Current, TenantPendingState.None)])
                : TenantListSnapshot.Degraded(
                    [Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantFreshnessState.Current, TenantPendingState.None)],
                    unavailable));
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        await ChangeSearchAsync(cut, "term");

        // Non-blocking: the degraded surface shows AND the (fallback) list rows remain visible.
        cut.WaitForElement("[data-testid='tenants-list-degraded']");
        cut.Markup.ShouldContain("tenant.alpha");
    }

    [Fact]
    public async Task Search_degraded_empty_snapshot_renders_degraded_not_filtered_empty()
    {
        RegisterServices(call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            return Task.FromResult(string.IsNullOrWhiteSpace(request.Search)
                ? ReadySnapshot([Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantFreshnessState.Current, TenantPendingState.None)])
                : TenantListSnapshot.Degraded([], "Search results could not be verified."));
        });

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        await ChangeSearchAsync(cut, "term");

        cut.WaitForElement("[data-testid='tenants-list-degraded']");
        cut.FindAll("[data-testid='tenants-list-filtered-empty']").ShouldBeEmpty();
    }

    [Fact]
    public void Cursor_paging_passes_opaque_cursor_and_preserves_markers()
    {
        TenantListSnapshot firstPage = ReadySnapshot(
            [
                Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantFreshnessState.Current, TenantPendingState.None),
            ],
            nextCursor: "opaque-next-cursor",
            hasMore: true);
        TenantListSnapshot secondPage = ReadySnapshot(
            [
                Row("tenant.beta", "Beta", TenantStatus.Disabled, TenantFreshnessState.Stale, TenantPendingState.Unknown),
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
    [InlineData(TenantListSurfaceKind.Loading, "tenants-list-loading")]
    [InlineData(TenantListSurfaceKind.Empty, "tenants-list-empty")]
    [InlineData(TenantListSurfaceKind.FilteredEmpty, "tenants-list-filtered-empty")]
    [InlineData(TenantListSurfaceKind.Error, "tenants-list-error")]
    [InlineData(TenantListSurfaceKind.Stale, "tenants-list-stale")]
    [InlineData(TenantListSurfaceKind.Degraded, "tenants-list-degraded")]
    public void Workspace_renders_distinct_six_list_states(TenantListSurfaceKind kind, string selector)
    {
        TenantListSnapshot snapshot = kind switch
        {
            TenantListSurfaceKind.Loading => TenantListSnapshot.Loading(),
            TenantListSurfaceKind.Empty => TenantListSnapshot.Empty(isAuthorizationScoped: true, TenantFreshnessState.Unknown),
            TenantListSurfaceKind.FilteredEmpty => TenantListSnapshot.FilteredEmpty(),
            TenantListSurfaceKind.Error => TenantListSnapshot.Error("Unavailable"),
            TenantListSurfaceKind.Stale => TenantListSnapshot.Stale(
                [TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active))],
                "\"etag\""),
            TenantListSurfaceKind.Degraded => TenantListSnapshot.Degraded(
                [TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active))],
                "Partial counts unavailable"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
        RegisterServices(snapshot);

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement($"[data-testid='{selector}']");

        cut.Find($"[data-testid='{selector}']").GetAttribute("role").ShouldNotBeNull();
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
            freshness: rows.Any(row => row.Freshness == TenantFreshnessState.Stale)
                ? TenantFreshnessState.Stale
                : TenantFreshnessState.Current,
            isDegraded: false);

    private static TenantListRow Row(
        string tenantId,
        string name,
        TenantStatus status,
        TenantFreshnessState freshness,
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
            ["Tenants.List.StatusFilterLabel"] = "Status",
            ["Tenants.List.StatusFilter.All"] = "All statuses",
            ["Tenants.List.StatusFilter.Active"] = "Active",
            ["Tenants.List.StatusFilter.Disabled"] = "Disabled",
            ["Tenants.List.StatusFilter.Unknown"] = "Unknown",
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
