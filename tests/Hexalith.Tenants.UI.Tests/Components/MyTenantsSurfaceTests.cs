using System.Globalization;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Tenants.Members;
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
    // Protected search paging is a required scoped circuit service; the workspace fails loudly without it.
    public MyTenantsSurfaceTests()
    {
        Services.AddScoped<TenantSearchPagingState>();    }

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
        cut.Find("[data-testid='tenants-my-projection-lifecycle']").TextContent.ShouldContain("Unknown");
        cut.Find("[data-testid='tenants-my-projection-lifecycle']").TextContent
            .ShouldNotContain("Tenants.ProjectionLifecycle", Case.Sensitive);
        cut.Find("[data-testid='tenants-my-summary']").TextContent.ShouldContain("2");
        // The self-audit surface is read-only. `tenants-lifecycle-actions` is emitted only by
        // TenantLifecycleActionAvailability, which nothing under Components/Users references, so asserting
        // it is absent is true by construction and cannot fail. Assert instead that the surface renders no
        // interactive control other than the ones it legitimately owns: the detail link, the support-safe
        // copy button, and the pager.
        // The previous sweep selected raw HTML tag names only. CSS type selectors match on local name, so no
        // Fluent custom element ever matched it -- and project rules forbid raw interactive HTML here, which
        // made it structurally blind to every affordance it was meant to catch. This test proves that above:
        // tenants-my-refresh is a FLUENT-BUTTON and is not allow-listed, yet the old assertion passed.
        // `tenants-my-retry` was also listed and exists nowhere in src/.
        string[] interactiveElements =
        [
            "BUTTON", "A", "INPUT", "SELECT", "TEXTAREA",
            "FLUENT-BUTTON", "FLUENT-ANCHOR", "FLUENT-ANCHOR-BUTTON", "FLUENT-MENU-BUTTON",
            "FLUENT-SELECT", "FLUENT-TEXT-FIELD", "FLUENT-TEXTAREA", "FLUENT-NUMBER-FIELD",
            "FLUENT-SEARCH", "FLUENT-COMBOBOX", "FLUENT-CHECKBOX", "FLUENT-RADIO",
            "FLUENT-SWITCH", "FLUENT-SLIDER",
        ];
        // Every one of these is read-only: navigation, support-safe copy, the pager, and the audit-evidence
        // entry point. `tenants-copy-reference` and `tenants-audit-entrypoint` are what the shared row
        // components actually stamp; the surface-scoped `tenants-my-*` ids sit on the same elements.
        string[] allowedControlIds =
        [
            "tenants-my-detail-link",
            "tenants-my-copy-reference",
            "tenants-copy-reference",
            "tenants-audit-entrypoint",
            "tenants-my-next",
            "tenants-my-previous",
            "tenants-my-refresh",
            "tenants-my-back",
        ];
        cut.FindAll("*")
            .Where(element => interactiveElements.Contains(element.NodeName, StringComparer.Ordinal)
                || string.Equals(element.GetAttribute("role"), "button", StringComparison.Ordinal))
            .Where(element =>
            {
                // Both attributes are consulted: a row control carries a shared `data-testid` and a
                // surface-scoped `data-surface-testid`, and either one identifying an allowed control is
                // enough. Reading only the first matched an id the allowlist does not use.
                string?[] ids = [element.GetAttribute("data-testid"), element.GetAttribute("data-surface-testid")];
                return !ids.Any(id => id is not null && allowedControlIds.Contains(id, StringComparer.Ordinal));
            })
            .Select(static element => element.GetAttribute("data-testid")
                ?? element.GetAttribute("data-surface-testid")
                ?? element.NodeName)
            .ShouldBeEmpty("The self-audit surface must expose no mutation affordance.");

        // Type-checked backstop. The sweep above depends on markup and on this list of element names staying
        // current with Fluent; these do not. If a membership command flow is ever composed onto the
        // self-audit surface, this fails regardless of what it renders.
        cut.FindComponents<AddTenantMemberFlow>().ShouldBeEmpty();
        cut.FindComponents<RemoveTenantMemberFlow>().ShouldBeEmpty();
        cut.FindComponents<ChangeTenantMemberRoleFlow>().ShouldBeEmpty();

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

        // The workspace only restores retained protected paging on an interactive render pass.
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));

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

    /// <summary>
    /// A protected cursor that expired must be reported, not silently swallowed.
    /// </summary>
    /// <remarks>
    /// epic-1-context.md: "invalid or stale cursor state restarts at page 1 with an honest localized
    /// notice". The panel reset _currentCursor/_cursorHistory and replaced the URL, but rendered from Kind
    /// alone, so a user on page 4 whose cursor expired saw page-1 rows announced as a normal result and
    /// believed they had advanced.
    /// </remarks>
    [Fact]
    public void My_tenants_page_one_recovery_is_announced_instead_of_restarting_silently()
    {
        RegisterServices(ReadySnapshot([Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader, ReadModelFreshnessState.Current)]) with
        {
            PagingRecovered = true,
            Reason = UserTenantMembershipReason.PageRecovered,
        });

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();

        AngleSharp.Dom.IElement notice = cut.WaitForElement("[data-testid='tenants-my-page-recovered']");
        notice.GetAttribute("aria-live").ShouldBe("polite");
        notice.TextContent.ShouldContain("restarted at the first page");
    }

    /// <summary>
    /// A page-one recovery must also reset the panel's own cursor state and URL.
    /// </summary>
    /// <remarks>
    /// The notice above is driven by <c>PagingRecovered</c> alone, so the three lines that actually restart
    /// paging -- clearing <c>_currentCursor</c>, clearing <c>_cursorHistory</c> and replacing the URL --
    /// could all be deleted with the test still green. A user would then be told the list restarted while
    /// Previous still walked the dead cursor chain and the address bar still carried the expired cursor.
    /// </remarks>
    [Fact]
    public void My_tenants_page_one_recovery_resets_the_cursor_history_and_the_url()
    {
        List<UserTenantMembershipRequest> requests = [];
        UserTenantMembershipSnapshot page = ReadySnapshot(
            [Row("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader, ReadModelFreshnessState.Current)],
            nextCursor: "page-2",
            hasMore: true);
        RegisterServices(call =>
        {
            requests.Add(call.Arg<UserTenantMembershipRequest>()!);

            // The second read -- the one the Next click issues -- comes back as a page-one recovery.
            return Task.FromResult(requests.Count == 1
                ? page
                : page with
                {
                    NextCursor = null,
                    HasMore = false,
                    PagingRecovered = true,
                    Reason = UserTenantMembershipReason.PageRecovered,
                });
        });

        IRenderedComponent<MyTenantsPage> cut = Render<MyTenantsPage>();
        cut.WaitForElement("[data-testid='tenants-my-next']").Click();
        cut.WaitForElement("[data-testid='tenants-my-page-recovered']");

        // Previous must be gone: the history was cleared, so page one is where the user actually is.
        cut.FindAll("[data-testid='tenants-my-previous']")
            .ShouldAllBe(static element => element.HasAttribute("disabled"));

        // ...and the URL no longer carries the dead cursor.
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.Uri.ShouldNotContain("page-2");
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

        // The workspace only restores retained protected paging on an interactive render pass.
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
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
            // ProjectionLifecycleBadge renders Localizer["Tenants.ProjectionLifecycle.{Lifecycle}"], and this
            // stub echoes any unknown key back. Without these entries the badge rendered the literal key
            // "Tenants.ProjectionLifecycle.Unknown", on which ShouldContain("Unknown") passes -- so a
            // localization break in the badge was invisible to every consumer test.
            ["Tenants.ProjectionLifecycle.Current"] = "Current",
            ["Tenants.ProjectionLifecycle.Stale"] = "Stale",
            ["Tenants.ProjectionLifecycle.Unknown"] = "Unknown",
            ["Tenants.ProjectionLifecycle.Rebuilding"] = "Rebuilding",
            ["Tenants.ProjectionLifecycle.Degraded"] = "Degraded",
            ["Tenants.ProjectionLifecycle.Unavailable"] = "Unavailable",
            ["Tenants.ProjectionLifecycle.LocalOnly"] = "Local only",
            ["Tenants.Workspace.Eyebrow"] = "Tenant workspace",
            ["Tenants.List.Column.Freshness"] = "Freshness",
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
            ["Tenants.List.StatusFilter.Active"] = "Active",
            ["Tenants.List.StatusFilter.All"] = "All statuses",
            ["Tenants.List.StatusFilter.Disabled"] = "Disabled",
            ["Tenants.List.StatusFilter.Unknown"] = "Unknown",
            ["Tenants.List.StatusFilterLabel"] = "Status on current page",
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
            ["Tenants.MyTenants.Recovery.PageRecovered"] = "The list restarted at the first page because the previous page reference expired.",
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
            ["Tenants.Copy.Feedback.Disconnected"] = "Clipboard disconnected. Copy was not completed. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Empty"] = "Nothing is available to copy.",
            ["Tenants.Copy.Feedback.Failed"] = "Copy failed. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Unavailable"] = "Clipboard unavailable. Select the value and copy it manually.",
            ["Tenants.Copy.Feedback.Unsafe"] = "This value is not support-safe to copy.",
        };
    }
}
