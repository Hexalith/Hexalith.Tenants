using System.Globalization;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TenantDetailSurfaceTests : BunitContext
{
    [Fact]
    public void Detail_page_loads_through_gateway_and_renders_operational_overview()
    {
        List<TenantDetailRequest> requests = [];
        RegisterServices(call =>
        {
            requests.Add(call.ArgAt<TenantDetailRequest>(0));
            return Task.FromResult(TenantDetailSnapshot.Ready(Detail("tenant.alpha"), "\"etag\"", TenantFreshnessState.Current));
        });

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants/tenant.alpha?returnUrl=%2Ftenants%3Fsearch%3Dalpha%26selected%3Dtenant.alpha");

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-identity']");

        requests.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        cut.Find("[data-testid='tenants-detail-back']").GetAttribute("href").ShouldBe("/tenants?search=alpha&selected=tenant.alpha");
        cut.Find("[data-testid='tenants-detail-truth-state']").TextContent.ShouldContain("Current");
        cut.Find("[data-testid='tenants-detail-identity']").TextContent.ShouldContain("tenant.alpha");
        cut.Find("[data-testid='tenants-detail-member-summary']").TextContent.ShouldContain("2 members");
        cut.Find("[data-testid='tenants-detail-configuration-summary']").TextContent.ShouldContain("1 configuration keys");
        cut.Markup.ShouldContain("aria-label=\"Full tenant identifier tenant.alpha\"");
        cut.Markup.ShouldContain("aria-label=\"Tenant status Active\"");
    }

    [Fact]
    public void Detail_page_displays_loading_until_gateway_completes()
    {
        TaskCompletionSource<TenantDetailSnapshot> detailResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        RegisterServices(_ => detailResult.Task);

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));

        cut.Find("[data-testid='tenants-detail-loading']").TextContent.ShouldContain("loading", Case.Insensitive);

        detailResult.SetResult(TenantDetailSnapshot.Ready(Detail("tenant.alpha"), "\"etag\"", TenantFreshnessState.Current));

        cut.WaitForElement("[data-testid='tenants-detail-identity']");
        cut.Find("[data-testid='tenants-detail-identity']").TextContent.ShouldContain("tenant.alpha");
    }

    [Theory]
    [InlineData("https%3A%2F%2Fexample.test%2Ftenants")]
    [InlineData("%2F%2Fevil.test%2Ftenants")]
    [InlineData("%2Fglobal-administrators")]
    public void Detail_page_rejects_unsafe_return_url(string encodedReturnUrl)
    {
        RegisterServices(_ => Task.FromResult(TenantDetailSnapshot.Ready(
            Detail("tenant.alpha"),
            "\"etag\"",
            TenantFreshnessState.Current)));

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/tenants/tenant.alpha?returnUrl={encodedReturnUrl}");

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-detail-identity']");

        cut.Find("[data-testid='tenants-detail-back']").GetAttribute("href").ShouldBe("/tenants");
    }

    [Theory]
    [InlineData(TenantDetailSurfaceKind.Stale, "tenants-detail-stale", "stale")]
    [InlineData(TenantDetailSurfaceKind.Degraded, "tenants-detail-degraded", "degraded")]
    [InlineData(TenantDetailSurfaceKind.Unauthorized, "tenants-detail-unauthorized", "authorized")]
    [InlineData(TenantDetailSurfaceKind.NotFound, "tenants-detail-error", "not found")]
    [InlineData(TenantDetailSurfaceKind.Unavailable, "tenants-detail-error", "unavailable")]
    public void Detail_page_renders_distinct_safe_states(
        TenantDetailSurfaceKind kind,
        string selector,
        string expectedText)
    {
        RegisterServices(_ => Task.FromResult(Snapshot(kind)));

        IRenderedComponent<TenantDetailPage> cut = Render<TenantDetailPage>(parameters => parameters
            .Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement($"[data-testid='{selector}']");

        cut.Find($"[data-testid='{selector}']").TextContent.ShouldContain(expectedText, Case.Insensitive);
        if (kind is TenantDetailSurfaceKind.Unauthorized or TenantDetailSurfaceKind.NotFound or TenantDetailSurfaceKind.Unavailable)
        {
            cut.Markup.ShouldNotContain("Tenant alpha description");
        }
    }

    [Fact]
    public void Workspace_detail_link_preserves_list_context_in_return_url()
    {
        TenantListSnapshot snapshot = TenantListSnapshot.Ready(
            [
                TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)),
            ],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: TenantFreshnessState.Current,
            isDegraded: false);
        RegisterServices(snapshot);

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants?search=alpha&status=Active&sort=name&desc=True&cursor=cursor-1");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-detail-link']");

        string href = cut.Find("[data-testid='tenants-list-detail-link']").GetAttribute("href").ShouldNotBeNull();
        href.ShouldStartWith("/tenants/tenant.alpha?returnUrl=");
        string decoded = Uri.UnescapeDataString(href[(href.IndexOf("returnUrl=", StringComparison.Ordinal) + "returnUrl=".Length)..]);
        decoded.ShouldContain("search=alpha");
        decoded.ShouldContain("status=Active");
        decoded.ShouldContain("sort=name");
        decoded.ShouldContain("desc=True");
        decoded.ShouldContain("cursor=cursor-1");
        decoded.ShouldContain("selected=tenant.alpha");
        decoded.ShouldContain("anchor=tenant-row-tenant.alpha");
    }

    [Fact]
    public void Workspace_restores_return_context_from_query_before_loading_list()
    {
        List<TenantListRequest> requests = [];
        TenantListSnapshot snapshot = TenantListSnapshot.Ready(
            [
                TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)),
                TenantListRow.FromSummary(new TenantSummary("tenant.beta", "Beta", TenantStatus.Disabled)),
            ],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: TenantFreshnessState.Current,
            isDegraded: false);
        RegisterListServices(call =>
        {
            requests.Add(call.ArgAt<TenantListRequest>(0));
            return Task.FromResult(snapshot);
        });

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants?search=beta&status=Disabled&sort=name&desc=True&cursor=cursor-1&selected=tenant.beta&anchor=tenant-row-tenant.beta");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-return-context']");

        TenantListRequest request = requests.ShouldHaveSingleItem();
        request.Search.ShouldBe("beta");
        request.Status.ShouldBe(TenantStatus.Disabled);
        request.SortColumn.ShouldBe(TenantListSortColumns.Name);
        request.SortDescending.ShouldBeTrue();
        request.Cursor.ShouldBe("cursor-1");
        cut.Find("[data-testid='tenants-list-return-context']").TextContent.ShouldContain("tenant.beta");
        cut.Markup.ShouldContain("tenant.beta");
        cut.Markup.ShouldNotContain("tenant.alpha");

        // The generated return anchor (anchor=tenant-row-tenant.beta) must resolve to a real DOM
        // target so focus/scroll restoration has something to land on.
        cut.Find("[id='tenant-row-tenant.beta']").ShouldNotBeNull();
        cut.Find("[id='tenants-list-heading']").GetAttribute("tabindex").ShouldBe("-1");
    }

    [Fact]
    public void Detail_styles_preserve_responsive_safety_and_forced_colors_hooks()
    {
        string projectRoot = ProjectRoot();
        string styles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Pages",
            "TenantDetailPage.razor.css"));

        styles.ShouldContain("overflow-wrap: anywhere");
        styles.ShouldContain("grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr))");
        styles.ShouldContain("@media (max-width: 767px)");
        styles.ShouldContain("grid-template-columns: minmax(0, 1fr)");
        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain(":focus-visible");
    }

    [Fact]
    public void French_resources_include_detail_keys()
    {
        string projectRoot = ProjectRoot();
        string frenchResources = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Resources",
            "TenantsResources.fr.resx"));

        frenchResources.ShouldContain("Tenants.Detail.Title");
        frenchResources.ShouldContain("Tenants.Detail.State.Unauthorized.Message");
        frenchResources.ShouldContain("Tenants.Detail.Configuration.Summary");
    }

    private void RegisterServices(TenantListSnapshot snapshot)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(snapshot));
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
    }

    private void RegisterListServices(Func<NSubstitute.Core.CallInfo, Task<TenantListSnapshot>> resultFactory)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(resultFactory);
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
    }

    private void RegisterServices(Func<NSubstitute.Core.CallInfo, Task<TenantDetailSnapshot>> detailFactory)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetTenantAsync(Arg.Any<TenantDetailRequest>(), Arg.Any<TenantDetailSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(detailFactory);
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
    }

    private static TenantDetailSnapshot Snapshot(TenantDetailSurfaceKind kind)
        => kind switch
        {
            TenantDetailSurfaceKind.Stale => TenantDetailSnapshot.Stale(Detail("tenant.alpha"), "\"etag\""),
            TenantDetailSurfaceKind.Degraded => TenantDetailSnapshot.Degraded(Detail("tenant.alpha"), "Tenant detail projection is degraded."),
            TenantDetailSurfaceKind.Unauthorized => TenantDetailSnapshot.Unauthorized("tenant.alpha"),
            TenantDetailSurfaceKind.NotFound => TenantDetailSnapshot.NotFound("tenant.alpha"),
            TenantDetailSurfaceKind.Unavailable => TenantDetailSnapshot.Unavailable("Tenant detail query gateway is unavailable."),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static TenantDetail Detail(string tenantId)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ],
            new Dictionary<string, string>
            {
                ["billing.mode"] = "trial",
            },
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", CultureInfo.InvariantCulture));

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        public LocalizedString this[string name] => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));

        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Detail.Back"] = "Back to tenants",
            ["Tenants.Detail.Configuration.Empty"] = "No configuration keys are available in this detail projection.",
            ["Tenants.Detail.Configuration.Summary"] = "{0} configuration keys across {1} safe groups.",
            ["Tenants.Detail.Configuration.Title"] = "Configuration summary",
            ["Tenants.Detail.CreatedAtLabel"] = "Created",
            ["Tenants.Detail.FreshnessLabel"] = "Freshness",
            ["Tenants.Detail.FullTenantIdLabel"] = "Full tenant identifier {0}",
            ["Tenants.Detail.IdentityLabel"] = "Tenant identity",
            ["Tenants.Detail.LifecycleLabel"] = "Lifecycle",
            ["Tenants.Detail.Members.Summary"] = "{0} members, including {1} owners.",
            ["Tenants.Detail.Members.Title"] = "Member summary",
            ["Tenants.Detail.OverviewLabel"] = "Tenant overview",
            ["Tenants.Detail.State.Degraded.Message"] = "Some tenant detail evidence is degraded.",
            ["Tenants.Detail.State.Degraded.Title"] = "Tenant detail is degraded",
            ["Tenants.Detail.State.Loading.Message"] = "Tenant detail is loading.",
            ["Tenants.Detail.State.Loading.Title"] = "Loading tenant detail",
            ["Tenants.Detail.State.NotFound.Message"] = "The requested tenant was not found.",
            ["Tenants.Detail.State.NotFound.Title"] = "Tenant not found",
            ["Tenants.Detail.State.Stale.Message"] = "The latest freshness evidence says this tenant detail is stale.",
            ["Tenants.Detail.State.Stale.Title"] = "Tenant detail is stale",
            ["Tenants.Detail.State.Unauthorized.Message"] = "This operator is not authorized.",
            ["Tenants.Detail.State.Unauthorized.Title"] = "Tenant detail unauthorized",
            ["Tenants.Detail.State.Unavailable.Message"] = "Tenant detail cannot be loaded because the gateway is unavailable.",
            ["Tenants.Detail.State.Unavailable.Title"] = "Tenant detail unavailable",
            ["Tenants.Detail.Status.Active"] = "Active",
            ["Tenants.Detail.Status.Disabled"] = "Disabled",
            ["Tenants.Detail.Status.Unknown"] = "Unknown",
            ["Tenants.Detail.StatusAccessibleLabel"] = "Tenant status {0}",
            ["Tenants.Detail.StatusLabel"] = "Status",
            ["Tenants.Detail.Title"] = "Tenant detail",
            ["Tenants.List.Column.Freshness"] = "Truth state",
            ["Tenants.List.Column.Members"] = "Members",
            ["Tenants.List.Column.Owners"] = "Owners",
            ["Tenants.List.Column.Pending"] = "Pending",
            ["Tenants.List.Column.Status"] = "Status",
            ["Tenants.List.Column.Tenant"] = "Tenant",
            ["Tenants.List.DetailLinkLabel"] = "Open tenant details for {0}",
            ["Tenants.List.Freshness.Current"] = "Current",
            ["Tenants.List.Pending.None"] = "No pending changes",
            ["Tenants.List.Pending.Unknown"] = "Pending state unknown",
            ["Tenants.List.ReturnContext"] = "Returned from tenant {0}. Filters, sort, cursor, and selection were restored before rendering.",
            ["Tenants.List.Title"] = "Tenants",
            ["Tenants.Workspace.Eyebrow"] = "Tenant workspace",
        };
    }
}
