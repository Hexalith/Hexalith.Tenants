using System.Globalization;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;

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

        cut.Find("[data-testid='tenants-list-refresh']").GetAttribute("type").ShouldBe("button");
        cut.Find("[data-testid='tenants-list-search']").GetAttribute("type").ShouldBe("search");
        cut.Find("[data-testid='tenants-list-reset']").GetAttribute("type").ShouldBe("button");
        cut.Find("[data-testid='tenants-list-detail-link']").GetAttribute("href").ShouldNotBeNull().ShouldContain("/tenants/tenant.alpha");
        cut.Find("[data-testid='tenants-list-truth-state']").TextContent.ShouldContain("Stale");
        cut.Markup.ShouldContain("tenant.alpha");
        cut.Markup.ShouldContain("No pending changes");
    }

    [Fact]
    public void Search_filter_and_sort_preserve_safety_markers()
    {
        TenantListSnapshot snapshot = ReadySnapshot(
            [
                Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantFreshnessState.Current, TenantPendingState.None),
                Row("tenant.beta", "Beta", TenantStatus.Disabled, TenantFreshnessState.Stale, TenantPendingState.Unknown),
            ]);
        RegisterServices(snapshot);

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-grid']");

        cut.Find("[data-testid='tenants-list-search']").Change("beta");

        cut.Markup.ShouldContain("tenant.beta");
        cut.Markup.ShouldNotContain("tenant.alpha");
        cut.Find("[data-testid='tenants-list-truth-state']").TextContent.ShouldContain("Stale");
        cut.Markup.ShouldContain("Pending state unknown");

        cut.Find("[data-testid='tenants-list-reset']").Click();
        cut.Find("[data-testid='tenants-list-status-filter']").Change(TenantStatus.Active.ToString());

        cut.Markup.ShouldContain("tenant.alpha");
        cut.Markup.ShouldNotContain("tenant.beta");
        cut.Find("[data-testid='tenants-list-truth-state']").TextContent.ShouldContain("Current");
        cut.Markup.ShouldContain("No pending changes");

        cut.Find("[data-testid='tenants-list-status-filter']").Change(string.Empty);
        cut.Find("[data-testid='tenants-list-sort']").Change(TenantListSortColumns.Name);
        cut.Find("[data-testid='tenants-list-sort-direction']").Change(bool.TrueString);

        cut.Markup.IndexOf("tenant.beta", StringComparison.Ordinal).ShouldBeLessThan(
            cut.Markup.IndexOf("tenant.alpha", StringComparison.Ordinal));
        cut.Markup.ShouldContain("data-testid=\"tenants-list-truth-state\"");
        cut.Markup.ShouldContain("Pending state unknown");
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
        };
    }
}
