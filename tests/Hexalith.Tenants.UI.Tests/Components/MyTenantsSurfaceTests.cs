using System.Globalization;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class MyTenantsSurfaceTests : BunitContext
{
    [Fact]
    public void My_tenants_route_renders_memberships_stable_selectors_and_no_mutation_controls()
    {
        const string tenantId = "  tenant/%2F?x=é&glyph=о  ";
        RegisterServices(ReadySnapshot(
            [
                Row(tenantId, "Alpha", TenantStatus.Active, TenantRole.TenantOwner, ReadModelFreshnessState.Current),
                Row("tenant.beta", "Beta", TenantStatus.Disabled, TenantRole.TenantReader, ReadModelFreshnessState.Unknown),
            ],
            nextCursor: "next",
            hasMore: true));
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler writeHandler = module.SetupVoid("writeText", tenantId).SetVoidResult();

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForElement("[data-testid='tenants-my-list']");

        cut.Find("[data-testid='tenants-my-refresh']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-my-back']").GetAttribute("href").ShouldBe("/tenants?tab=tenants&scope=mine");
        cut.FindAll("[data-testid='tenants-my-row']").Count.ShouldBe(2);
        cut.Find("[data-testid='tenants-my-tenant-id']").TextContent.ShouldBe(tenantId);
        cut.FindAll("[data-testid='tenants-my-copy-reference']").Count.ShouldBe(2);
        cut.Find("[data-testid='tenants-my-copy-reference']").GetAttribute("data-copy-kind").ShouldBe("TenantId");
        cut.Find("[data-testid='tenants-my-role']").TextContent.ShouldContain("Tenant owner");
        cut.Find("[data-testid='tenants-my-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-my-truth-state']").TextContent.ShouldContain("Current");
        cut.Find("[data-testid='tenants-my-summary']").TextContent.ShouldContain("2");
        cut.Markup.ShouldNotContain("Lifecycle", Case.Insensitive);
        cut.Markup.ShouldNotContain("remove", Case.Insensitive);
        cut.Markup.ShouldNotContain("change role", Case.Insensitive);
        cut.Markup.ShouldNotContain("command", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);

        cut.Find("[data-surface-testid='tenants-my-copy-reference']").Click();
        cut.WaitForAssertion(() => writeHandler.Invocations.Count.ShouldBe(1));
        writeHandler.Invocations.Single().Arguments[0].ShouldBe(tenantId);
    }

    [Fact]
    public void Tenants_workspace_toolbar_does_not_duplicate_shell_navigation_links()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Unknown)));
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        // The workspace now renders Fluent UI v5 components that import JS modules on first render.
        JSInterop.Mode = JSRuntimeMode.Loose;

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-refresh']");

        // 2026-06-25 ergonomic pass: "My tenants" and "User lookup" are reachable from the shell
        // navigation rail (registered in TenantsFrontComposerRegistration), so the list command bar no
        // longer duplicates them — it exposes list actions only (refresh/reset). Mirroring navigation
        // links inside a list toolbar is a professional-admin-UI anti-pattern.
        cut.FindAll("[data-testid='tenants-my-link']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-user-lookup-link']").ShouldBeEmpty();
    }

    [Fact]
    public void My_tenants_loading_state_is_distinct_and_accessible()
    {
        TaskCompletionSource<UserTenantMembershipSnapshot> pending = new();
        RegisterServices(_ => pending.Task);

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForElement("[data-testid='tenants-my-loading']");

        cut.Find("[data-testid='tenants-my-loading']").GetAttribute("role").ShouldBe("status");

        pending.SetResult(UserTenantMembershipSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Unknown, eTag: null));
    }

    [Theory]
    [InlineData(UserTenantMembershipSurfaceKind.Empty, "tenants-my-empty", "No visible memberships", "status")]
    [InlineData(UserTenantMembershipSurfaceKind.Unauthorized, "tenants-my-error", "My Tenants is unauthorized", "alert")]
    [InlineData(UserTenantMembershipSurfaceKind.Unavailable, "tenants-my-error", "My Tenants is unavailable", "alert")]
    public void My_tenants_renders_distinct_empty_unauthorized_and_unavailable_states(
        UserTenantMembershipSurfaceKind kind,
        string selector,
        string expectedTitle,
        string expectedRole)
    {
        UserTenantMembershipSnapshot snapshot = kind switch
        {
            UserTenantMembershipSurfaceKind.Empty => UserTenantMembershipSnapshot.Empty(
                isAuthorizationScoped: true,
                ReadModelFreshnessState.Unknown,
                eTag: null),
            UserTenantMembershipSurfaceKind.Unauthorized => UserTenantMembershipSnapshot.Unauthorized(),
            UserTenantMembershipSurfaceKind.Unavailable => UserTenantMembershipSnapshot.Unavailable(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
        RegisterServices(snapshot);

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForElement($"[data-testid='{selector}']");

        cut.Find($"[data-testid='{selector}']").GetAttribute("role").ShouldBe(expectedRole);
        cut.Markup.ShouldContain(expectedTitle);
        cut.Markup.ShouldNotContain("tenant.alpha");
    }

    [Theory]
    [InlineData(UserTenantMembershipSurfaceKind.Stale, "tenants-my-stale", "Stale")]
    [InlineData(UserTenantMembershipSurfaceKind.Degraded, "tenants-my-degraded", "Unknown")]
    public void My_tenants_preserves_visible_stale_and_degraded_markers(
        UserTenantMembershipSurfaceKind kind,
        string selector,
        string expectedFreshness)
    {
        UserTenantMembershipSnapshot snapshot = kind switch
        {
            UserTenantMembershipSurfaceKind.Stale => UserTenantMembershipSnapshot.Stale(
                [Row("tenant.alpha", "Alpha", TenantStatus.Disabled, TenantRole.TenantReader, ReadModelFreshnessState.Stale)],
                nextCursor: "next",
                hasMore: true,
                eTag: "\"etag\""),
            UserTenantMembershipSurfaceKind.Degraded => UserTenantMembershipSnapshot.Degraded(
                [Row("tenant.alpha", "Alpha", TenantStatus.Unknown, TenantRole.Unknown, ReadModelFreshnessState.Unknown)],
                UserTenantMembershipReason.ProjectionDegraded,
                eTag: "\"etag\"",
                nextCursor: "next",
                hasMore: true),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
        RegisterServices(snapshot);

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForElement($"[data-testid='{selector}']");

        cut.Find("[data-testid='tenants-my-list']").TextContent.ShouldContain("tenant.alpha");
        cut.Find("[data-testid='tenants-my-truth-state']").TextContent.ShouldContain(expectedFreshness);
        cut.Find("[data-testid='tenants-my-next']").GetAttribute("disabled").ShouldBeNull();
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void My_tenants_invalid_state_does_not_collapse_into_an_empty_grid()
    {
        RegisterServices(UserTenantMembershipSnapshot.Invalid());

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForElement("[data-testid='tenants-my-invalid']");

        cut.Find("[data-testid='tenants-my-invalid']").GetAttribute("role").ShouldBe("alert");
        cut.Markup.ShouldContain("Membership lookup is invalid");
        cut.FindAll("[data-testid='tenants-my-list']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-my-next']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void My_tenants_cursor_paging_passes_opaque_cursor_and_keeps_truth_state()
    {
        UserTenantMembershipSnapshot firstPage = ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantOwner, ReadModelFreshnessState.Current)],
            nextCursor: "opaque-next-cursor",
            hasMore: true);
        UserTenantMembershipSnapshot secondPage = UserTenantMembershipSnapshot.Stale(
            [Row("tenant.beta", "Beta", TenantStatus.Disabled, TenantRole.TenantReader, ReadModelFreshnessState.Stale)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"");
        Queue<UserTenantMembershipSnapshot> snapshots = new([firstPage, secondPage, firstPage]);
        List<UserTenantMembershipRequest> requests = [];
        List<string> requestUris = [];
        RegisterServices(call =>
        {
            requests.Add(call.ArgAt<UserTenantMembershipRequest>(0));
            requestUris.Add(Services.GetRequiredService<NavigationManager>().Uri);
            return Task.FromResult(snapshots.Dequeue());
        });

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForElement("[data-testid='tenants-my-list']");
        cut.Markup.ShouldContain("tenant.alpha");

        cut.Find("[data-testid='tenants-my-next']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("tenant.beta"));

        requests[1].Cursor.ShouldBe("opaque-next-cursor");
        requestUris[1].ShouldBe("http://localhost/tenants?tab=tenants&scope=mine&cursor=opaque-next-cursor");
        cut.Find("[data-testid='tenants-my-truth-state']").TextContent.ShouldContain("Stale");
        Services.GetRequiredService<NavigationManager>().Uri.ShouldBe(
            "http://localhost/tenants?tab=tenants&scope=mine&cursor=opaque-next-cursor");

        cut.Find("[data-testid='tenants-my-previous']").Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("tenant.alpha"));

        requests[2].Cursor.ShouldBeNull();
        requestUris[2].ShouldBe("http://localhost/tenants?tab=tenants&scope=mine");
        cut.Find("[data-testid='tenants-my-truth-state']").TextContent.ShouldContain("Current");
        Services.GetRequiredService<NavigationManager>().Uri.ShouldBe(
            "http://localhost/tenants?tab=tenants&scope=mine");
    }

    [Fact]
    public void My_tenants_scope_sends_no_browser_supplied_target_user_id()
    {
        // AC1 (UI level): the scope=mine surface builds its membership request with no TargetUserId, so the
        // authenticated identity is derived server-side by the gateway (GetMyTenantsAsync) and a browser can
        // never substitute a target from this surface.
        List<UserTenantMembershipRequest> requests = [];
        RegisterServices(call =>
        {
            requests.Add(call.ArgAt<UserTenantMembershipRequest>(0));
            return Task.FromResult(ReadySnapshot(
                [Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantOwner, ReadModelFreshnessState.Current)]));
        });

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForElement("[data-testid='tenants-my-list']");

        requests.ShouldNotBeEmpty();
        requests[0].TargetUserId.ShouldBeNull();
    }

    [Fact]
    public void My_tenants_rows_expose_a_shared_detail_drill_in_and_a_focus_anchor_id()
    {
        RegisterServices(ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantOwner, ReadModelFreshnessState.Current)]));

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForElement("[data-testid='tenants-my-list']");

        // AC5: the self-audit row drills into the shared /tenants/{tenantId} detail route (same component as
        // scope=all), carrying canonical scope=mine return context (selection + return-focus anchor) and
        // resetting the cursor to the authorized first page.
        var detailLink = cut.Find("[data-testid='tenants-my-detail-link']");
        detailLink.NodeName.ShouldBe("A");
        string? href = detailLink.GetAttribute("href");
        href.ShouldNotBeNull();
        href.ShouldStartWith("/tenants/tenant.alpha?returnUrl=");
        string decodedReturnUrl = Uri.UnescapeDataString(href!["/tenants/tenant.alpha?returnUrl=".Length..]);
        decodedReturnUrl.ShouldBe("/tenants?tab=tenants&scope=mine&selected=tenant.alpha&anchor=tenants-my-row-tenant.alpha");

        // AC7: the identity element carries the id the ReturnFocus anchor points at, so focus-on-return
        // resolves (previously the id was missing and focus was a no-op).
        cut.Find("[data-testid='tenants-my-row']").Id.ShouldBe("tenants-my-row-tenant.alpha");

        // The self-audit Role column is preserved alongside the new drill-in.
        cut.Find("[data-testid='tenants-my-role']").TextContent.ShouldContain("Tenant owner");
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
    }

    [Fact]
    public void My_tenants_components_have_no_browser_backend_http_or_token_storage()
    {
        string projectRoot = ProjectRoot();
        string[] componentFiles = Directory
            .GetFiles(Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components"), "*.razor", SearchOption.AllDirectories)
            .Where(static path => !path.EndsWith("App.razor", StringComparison.Ordinal))
            .ToArray();
        string combined = string.Join('\n', componentFiles.Select(File.ReadAllText));

        combined.ShouldNotContain("GET /api/users", Case.Insensitive);
        combined.ShouldNotContain("HttpClient");
        combined.ShouldNotContain("localStorage", Case.Insensitive);
        combined.ShouldNotContain("sessionStorage", Case.Insensitive);
        combined.ShouldNotContain("access_token", Case.Insensitive);
    }

    [Fact]
    public void My_tenants_styles_preserve_critical_columns_and_forced_colors_hooks()
    {
        string projectRoot = ProjectRoot();
        string styles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Users",
            "MyTenantsDataGrid.razor.css"));

        styles.ShouldContain("overflow-x: auto");
        styles.ShouldContain("min-width:");
        styles.ShouldContain("white-space: nowrap");
        styles.ShouldContain("white-space: break-spaces");
        styles.ShouldContain("tenants-my-critical");
        styles.ShouldContain("grid-template-columns: minmax(0, 1fr) auto");
    }

    private void RegisterServices(UserTenantMembershipSnapshot snapshot)
        => RegisterServices(_ => Task.FromResult(snapshot));

    private void RegisterServices(Func<NSubstitute.Core.CallInfo, Task<UserTenantMembershipSnapshot>> resultFactory)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetMyTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(resultFactory);
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
    }

    private static UserTenantMembershipSnapshot ReadySnapshot(
        IReadOnlyList<UserTenantMembershipRow> rows,
        string? nextCursor = null,
        bool hasMore = false)
        => UserTenantMembershipSnapshot.Ready(
            rows,
            nextCursor,
            hasMore,
            eTag: "\"etag\"",
            freshness: rows.Any(row => row.Freshness == ReadModelFreshnessState.Stale)
                ? ReadModelFreshnessState.Stale
                : ReadModelFreshnessState.Current);

    private static UserTenantMembershipRow Row(
        string tenantId,
        string name,
        TenantStatus status,
        TenantRole role,
        ReadModelFreshnessState freshness)
        => new(tenantId, name, status, role, freshness);

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
            ["Tenants.Workspace.Eyebrow"] = "Tenant workspace",
            ["Tenants.List.Column.Freshness"] = "Truth state",
            ["Tenants.List.Column.Members"] = "Members",
            ["Tenants.List.Column.Owners"] = "Owners",
            ["Tenants.List.Column.Pending"] = "Pending",
            ["Tenants.List.Column.Status"] = "Status",
            ["Tenants.List.Column.Tenant"] = "Tenant",
            ["Tenants.List.ControlsLabel"] = "Tenant list controls",
            ["Tenants.List.Count.Unknown"] = "Unknown",
            ["Tenants.List.Freshness.Unknown"] = "Unknown",
            ["Tenants.List.Pending.None"] = "No pending changes",
            ["Tenants.List.Pending.Unknown"] = "Pending state unknown",
            ["Tenants.List.Refresh"] = "Refresh",
            ["Tenants.List.Reset"] = "Reset filters",
            ["Tenants.List.SearchLabel"] = "Search tenants",
            ["Tenants.List.SearchPlaceholder"] = "Search by tenant id or name",
            ["Tenants.List.Sort.Name"] = "Name",
            ["Tenants.List.Sort.Status"] = "Status",
            ["Tenants.List.Sort.TenantId"] = "Tenant id",
            ["Tenants.List.SortDirection.Ascending"] = "Ascending",
            ["Tenants.List.SortDirection.Descending"] = "Descending",
            ["Tenants.List.SortDirectionLabel"] = "Sort direction",
            ["Tenants.List.SortLabel"] = "Sort",
            ["Tenants.List.StatusFilter.Active"] = "Active",
            ["Tenants.List.StatusFilter.All"] = "All statuses",
            ["Tenants.List.StatusFilter.Disabled"] = "Disabled",
            ["Tenants.List.StatusFilter.Unknown"] = "Unknown",
            ["Tenants.List.StatusFilterLabel"] = "Status",
            ["Tenants.List.Title"] = "Tenants",
            ["Tenants.MyTenants.Back"] = "Back to tenants",
            ["Tenants.MyTenants.Column.Freshness"] = "Freshness",
            ["Tenants.MyTenants.Column.Role"] = "Role",
            ["Tenants.MyTenants.Column.Status"] = "Status",
            ["Tenants.MyTenants.Column.Tenant"] = "Tenant",
            ["Tenants.MyTenants.ControlsLabel"] = "My Tenants controls",
            ["Tenants.MyTenants.Description"] = "Read-only view of tenants visible to your signed-in account.",
            ["Tenants.MyTenants.Freshness.Current"] = "Current",
            ["Tenants.MyTenants.Freshness.Stale"] = "Stale",
            ["Tenants.MyTenants.Freshness.Unknown"] = "Unknown",
            ["Tenants.MyTenants.Link"] = "My tenants",
            ["Tenants.UserLookup.Link"] = "User lookup",
            ["Tenants.MyTenants.Next"] = "Next",
            ["Tenants.MyTenants.PaginationLabel"] = "My Tenants pages",
            ["Tenants.MyTenants.Previous"] = "Previous",
            ["Tenants.MyTenants.Refresh"] = "Refresh",
            ["Tenants.MyTenants.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.MyTenants.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.MyTenants.Role.TenantReader"] = "Tenant reader",
            ["Tenants.MyTenants.Role.Unknown"] = "Unknown role",
            ["Tenants.MyTenants.RoleAccessible"] = "Role: {0}",
            ["Tenants.MyTenants.State.Degraded.Message"] = "Some membership evidence is degraded. Visible rows are not represented as current.",
            ["Tenants.MyTenants.State.Degraded.Title"] = "My Tenants data is degraded",
            ["Tenants.MyTenants.State.Empty.Message"] = "No tenants are visible for your signed-in account. This authorized empty result is not an error.",
            ["Tenants.MyTenants.State.Empty.Title"] = "No visible memberships",
            ["Tenants.MyTenants.State.Invalid.Message"] = "The membership request could not be validated. Enter a supported user identifier and try again.",
            ["Tenants.MyTenants.State.Invalid.Title"] = "Membership lookup is invalid",
            ["Tenants.MyTenants.State.Loading.Message"] = "Your tenant memberships are loading from the server-side query gateway.",
            ["Tenants.MyTenants.State.Loading.Title"] = "Loading my tenants",
            ["Tenants.MyTenants.State.Stale.Message"] = "The latest freshness evidence says these memberships are stale. Refresh to check the projection again.",
            ["Tenants.MyTenants.State.Stale.Title"] = "My Tenants data is stale",
            ["Tenants.MyTenants.State.Unauthorized.Message"] = "The signed-in user could not be verified for this self-audit view.",
            ["Tenants.MyTenants.State.Unauthorized.Title"] = "My Tenants is unauthorized",
            ["Tenants.MyTenants.State.Unavailable.Message"] = "My Tenants is unavailable until the server-side query gateway can be reached.",
            ["Tenants.MyTenants.State.Unavailable.Title"] = "My Tenants is unavailable",
            ["Tenants.MyTenants.Status.Active"] = "Active",
            ["Tenants.MyTenants.Status.Disabled"] = "Disabled",
            ["Tenants.MyTenants.Status.Unknown"] = "Unknown status",
            ["Tenants.MyTenants.StatusAccessible"] = "Status: {0}",
            ["Tenants.MyTenants.Summary"] = "Tenants shown: {0}",
            ["Tenants.MyTenants.Title"] = "My Tenants",
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
