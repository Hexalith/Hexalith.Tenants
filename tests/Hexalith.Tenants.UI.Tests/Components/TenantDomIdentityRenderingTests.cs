using AngleSharp.Dom;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Tenants;
using Hexalith.Tenants.UI.Components.Users;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TenantDomIdentityRenderingTests : BunitContext
{
    private static readonly string[] _identities =
    [
        "tenant.alpha", "tenant-alpha", "tenant alpha", "tenant\talpha", " tenant.alpha ",
        "tenant:alpha#['x']", "é", "e\u0301", "租户🙂", "id-dGVuYW50LmFscGhh", "Tenant.Alpha",
    ];

    public TenantDomIdentityRenderingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
        Services.AddFluentUIComponents();
        Services.AddScoped<TenantSearchPagingState>();
        Services.AddScoped<Hexalith.FrontComposer.Shell.Services.NavigationFailureNotifier>();
        ITenantsBffComposition composition = Substitute.For<ITenantsBffComposition>();
        composition.IsReadSurfaceConnected.Returns(true);
        Services.AddSingleton(composition);
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("tenants-my")]
    [InlineData("tenants-user")]
    public void Exact_identities_render_distinct_stable_selector_safe_anchors(string surface)
    {
        using var cut = RenderGrid(surface, _identities);
        string selector = surface == "tenant" ? "[data-testid='tenants-list-tenant-id']" : $"[data-testid='{surface}-row']";
        IElement[] rows = cut.FindAll(selector).ToArray();
        rows.Length.ShouldBe(_identities.Length);
        string[] anchors = rows.Select(row => row.Id.ShouldNotBeNull()).ToArray();
        string[] renderedIdentities = rows.Select(row => row.QuerySelector("strong")!.TextContent).ToArray();
        renderedIdentities.Order(StringComparer.Ordinal).ShouldBe(_identities.Order(StringComparer.Ordinal));
        anchors.Distinct(StringComparer.Ordinal).Count().ShouldBe(_identities.Length);
        foreach (IElement row in rows)
        {
            cut.FindAll("#" + row.Id).Count.ShouldBe(1);
            cut.Find("#" + row.Id).QuerySelector("strong")!.TextContent.ShouldBe(row.QuerySelector("strong")!.TextContent);
            row.Id.ShouldNotBeNull().ShouldMatch(@"^[A-Za-z][A-Za-z0-9_-]+$");
            string identity = row.QuerySelector("strong").ShouldNotBeNull().TextContent;
            _identities.ShouldContain(identity);
            if (!TenantAuditNavigationSafety.IsSafeTenantId(identity))
            {
                row.QuerySelector("a[href]").ShouldBeNull();
            }
        }

        if (cut is IRenderedComponent<TenantDataGrid> all)
        {
            all.Render();
        }
        else
        {
            ((IRenderedComponent<MyTenantsDataGrid>)cut).Render();
        }
        cut.FindAll(selector).Select(row => row.Id).ShouldBe(anchors);
        Export(surface + "-exotic", cut.Markup);
    }

    [Theory]
    [InlineData("tenant", null)]
    [InlineData("tenant", "")]
    [InlineData("tenant", " \t\r\n")]
    [InlineData("tenants-my", null)]
    [InlineData("tenants-my", "")]
    [InlineData("tenants-my", " \t\r\n")]
    [InlineData("tenants-user", "")]
    public void Blank_input_renders_without_anchor_or_navigation(string surface, string? tenantId)
        => AssertUnsupportedRow(surface, tenantId);

    [Theory]
    [InlineData("tenant")]
    [InlineData("tenants-my")]
    [InlineData("tenants-user")]
    public void Malformed_unicode_cannot_collapse_to_a_replacement_character_anchor(string surface)
        => AssertUnsupportedRow(surface, "\ud800");

    [Theory]
    [InlineData("tenant")]
    [InlineData("tenants-my")]
    [InlineData("tenants-user")]
    public void Rendered_audit_links_target_the_exact_origin_row_and_keep_route_identity(string surface)
    {
        string[] identities = ["tenant.alpha", "tenant-alpha", "Tenant.Alpha", new('a', 256)];
        using var cut = RenderGrid(surface, identities);
        IElement[] links = cut.FindAll("fluent-anchor-button[data-testid='tenants-audit-entrypoint']").ToArray();
        string[] markers = AuditEntryPointMarkers(surface);
        links.Length.ShouldBe(identities.Length * markers.Length);
        foreach (string identity in identities)
        {
            IElement[] originLinks = links.Where(link => Uri.UnescapeDataString(link.GetAttribute("href")!.Split('?')[0].Split('/')[2]) == identity).ToArray();
            originLinks.Length.ShouldBe(markers.Length);
            foreach (string marker in markers)
            {
                originLinks.Count(link => link.QuerySelector($"[data-testid='{marker}']") is not null).ShouldBe(1);
            }
            originLinks.Select(link => link.GetAttribute("href")).Distinct(StringComparer.Ordinal).Count().ShouldBe(1);
        }
        foreach (IElement link in links)
        {
            string href = link.GetAttribute("href").ShouldNotBeNull();
            string focus = Query(href, "returnFocus").ShouldNotBeNull();
            string returnUrl = Query(href, "returnUrl").ShouldNotBeNull();
            string identity = Uri.UnescapeDataString(href.Split('?')[0].Split('/')[2]);
            identities.ShouldContain(identity);
            IElement anchor = cut.Find("#" + focus);
            cut.FindAll("#" + focus).Count.ShouldBe(1);
            anchor.QuerySelector("strong").ShouldNotBeNull().TextContent.ShouldBe(identity);
            anchor.Closest("[role='row']").ShouldNotBeNull().Contains(link).ShouldBeTrue();
            TenantAuditNavigationSafety.SafeReturnUrlForTenant(returnUrl, identity, out _).ShouldBe(returnUrl);
            TenantAuditNavigationSafety.IsFocusForReturnUrl(returnUrl, focus).ShouldBeTrue();
        }

        Export(surface, cut.Markup);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void My_tenants_detail_navigation_and_return_focus_use_the_rendered_detail_id(bool workspace)
    {
        RegisterGateway(["tenant.alpha", "tenant-alpha"]);
        IRenderedComponent<MyTenantsPanel> cut = Render<MyTenantsPanel>(parameters => parameters
            .Add(p => p.UseWorkspaceRoute, workspace));
        cut.WaitForElement("[data-testid='tenants-my-detail-link']");
        IElement link = cut.FindAll("[data-testid='tenants-my-detail-link']")[1];
        string href = link.GetAttribute("href").ShouldNotBeNull();
        string returnUrl = Query(href, "returnUrl").ShouldNotBeNull();
        Query(returnUrl, "anchor").ShouldBe(link.Id);
        href.ShouldStartWith("/tenants/tenant-alpha?");
        Query(returnUrl, "cursor").ShouldBeNull();
        TenantAuditNavigationSafety.SafeListReturnUrl(returnUrl, out _).ShouldBe(returnUrl);
        if (workspace)
        {
            Query(returnUrl, "selected").ShouldBe("tenant-alpha");
        }

        Export(workspace ? "my-detail-workspace" : "my-detail-standalone", cut.Markup);

        Services.GetRequiredService<NavigationManager>().NavigateTo(returnUrl);
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsFocus.js");
        var restoration = module.Setup<string>("restoreDetailFocus", link.Id!, "tenants-my-page", workspace ? "tenants-list-heading" : "tenants-my-heading")
            .SetResult("found");
        if (workspace)
        {
            Render<TenantsWorkspace>();
        }
        else
        {
            Render<MyTenantsPage>();
        }
        cut.WaitForAssertion(() => restoration.Invocations.Count.ShouldBe(1));
    }

    [Fact]
    public void All_tenants_detail_return_preserves_selection_and_focuses_the_actual_heading()
    {
        RegisterGateway(["tenant.alpha", "tenant-alpha"]);
        using var grid = RenderGrid("tenant", ["tenant.alpha", "tenant-alpha"]);
        string detailHref = grid.FindAll("[data-testid='tenants-list-detail-link']")[1].GetAttribute("href")!;
        string returnUrl = Query(detailHref, "returnUrl")!;
        string anchor = Query(returnUrl, "anchor")!;
        Query(returnUrl, "selected").ShouldBe("tenant-alpha");
        Query(returnUrl, "cursor").ShouldBeNull();
        grid.Find("#" + anchor).QuerySelector("strong")!.TextContent.ShouldBe("tenant-alpha");
        Services.GetRequiredService<NavigationManager>().NavigateTo(returnUrl);
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        FcPageHeader header = cut.FindComponent<FcPageHeader>().Instance;
        ElementReference heading = (ElementReference)typeof(FcPageHeader)
            .GetField("_headingElement", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(header)!;
        cut.WaitForAssertion(() => JSInterop.Invocations.Count(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus"
            && invocation.Arguments[0] is ElementReference element && element.Id == heading.Id).ShouldBe(1));
        cut.Find("#tenants-list-heading").GetAttribute("tabindex").ShouldBe("-1");
        Export("tenant-detail-return", "<div data-detail-return-url=\"" + System.Net.WebUtility.HtmlEncode(returnUrl)
            + "\">" + cut.Markup + "</div>");
    }

    [Fact]
    public void Lookup_audit_return_passes_the_exact_encoded_anchor_and_subject_to_focus_interop()
    {
        RegisterGateway(["tenant.alpha", "tenant-alpha"]);
        using var grid = RenderGrid("tenants-user", ["tenant.alpha", "tenant-alpha"]);
        grid.FindAll("a[href]").ShouldBeEmpty();
        string href = grid.FindAll("fluent-anchor-button[data-testid='tenants-audit-entrypoint']")[1].GetAttribute("href")!;
        Query(href, "targetUserId").ShouldBe("operator-user");
        string focus = Query(href, "returnFocus")!;
        string returnUrl = Query(href, "returnUrl")!;
        Services.GetRequiredService<NavigationManager>().NavigateTo(returnUrl + "&auditFocus=" + focus);
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsFocus.js");
        var restoration = module.Setup<string>("restoreAuditFocus", focus, "tenants-user-lookup", "tenants-user-lookup-heading", "operator-user")
            .SetResult("found");
        IRenderedComponent<UserMembershipLookupPage> cut = Render<UserMembershipLookupPage>();
        cut.WaitForAssertion(() => restoration.Invocations.Count.ShouldBe(1));
        cut.Find("#" + focus).QuerySelector("strong")!.TextContent.ShouldBe("tenant-alpha");
    }

    [Theory]
    [InlineData("all", "tenants-list-audit-entrypoint")]
    [InlineData("all", "tenants-list-mobile-audit-entrypoint")]
    [InlineData("mine", "tenants-my-audit-entrypoint")]
    public void Workspace_audit_return_passes_the_rendered_anchor_to_focus_interop(string scope, string entryPointMarker)
    {
        RegisterGateway(["tenant.alpha", "tenant-alpha"]);
        using var grid = RenderGrid(scope == "all" ? "tenant" : "tenants-my", ["tenant.alpha", "tenant-alpha"]);
        string rowSelector = scope == "all" ? "[data-testid='tenants-list-tenant-id']" : "[data-testid='tenants-my-row']";
        IElement originRow = grid.FindAll(rowSelector).Single(row => row.QuerySelector("strong")!.TextContent == "tenant-alpha");
        IElement launcher = originRow.Closest("[role='row']").ShouldNotBeNull()
            .QuerySelector($"[data-testid='{entryPointMarker}']").ShouldNotBeNull().ParentElement.ShouldNotBeNull();
        string href = launcher.GetAttribute("href").ShouldNotBeNull();
        href.ShouldStartWith("/tenants/tenant-alpha/audit?");
        Query(href, "source").ShouldBe(scope == "all" ? "tenant-list" : "my-tenants");
        string focus = Query(href, "returnFocus")!;
        string returnUrl = Query(href, "returnUrl")!;
        focus.ShouldBe(originRow.Id);
        TenantAuditNavigationSafety.SafeReturnUrlForTenant(returnUrl, "tenant-alpha", out _).ShouldBe(returnUrl);
        TenantAuditNavigationSafety.IsFocusForReturnUrl(returnUrl, focus).ShouldBeTrue();
        Services.GetRequiredService<NavigationManager>().NavigateTo(returnUrl + (returnUrl.Contains('?') ? "&" : "?") + "auditFocus=" + focus);
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsFocus.js");
        string origin = scope == "all" ? "tenants-workspace-tenants-panel-content" : "tenants-my-page";
        const string heading = "tenants-list-heading";
        var restoration = module.Setup<string>("restoreAuditFocus", focus, origin, heading, null).SetResult("found");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForAssertion(() => restoration.Invocations.Count.ShouldBe(1));
        cut.FindAll("#" + focus).Count.ShouldBe(1);
        cut.Find("#" + focus).QuerySelector("strong").ShouldNotBeNull().TextContent.ShouldBe("tenant-alpha");
    }

    private void AssertUnsupportedRow(string surface, string? tenantId)
    {
        using var cut = RenderGrid(surface, [tenantId!, "tenant.alpha"]);
        string selector = surface == "tenant" ? "[data-testid='tenants-list-tenant-id']" : $"[data-testid='{surface}-row']";
        IElement invalid = cut.FindAll(selector).Single(row => row.QuerySelector("strong")!.TextContent != "tenant.alpha");
        invalid.HasAttribute("id").ShouldBeFalse();
        invalid.QuerySelector("a[href]").ShouldBeNull();
        IElement invalidRow = invalid.Closest("[role='row']").ShouldNotBeNull();
        invalidRow.QuerySelectorAll("a[href], fluent-anchor-button[href]").ShouldBeEmpty();
        string[] markers = AuditEntryPointMarkers(surface);
        foreach (string marker in markers)
        {
            IElement unavailable = invalidRow.QuerySelector($"[data-testid='{marker}']").ShouldNotBeNull().ParentElement.ShouldNotBeNull();
            unavailable.HasAttribute("disabled").ShouldBeTrue();
            unavailable.GetAttribute("href").ShouldBeNull();
            string reasonId = unavailable.GetAttribute("aria-describedby").ShouldNotBeNull();
            cut.FindAll("#" + reasonId).Count.ShouldBe(1);
        }
        IElement[] links = cut.FindAll("fluent-anchor-button[data-testid='tenants-audit-entrypoint']").ToArray();
        links.Length.ShouldBe(markers.Length);
        foreach (IElement link in links)
        {
            string href = link.GetAttribute("href").ShouldNotBeNull();
            href.ShouldStartWith("/tenants/tenant.alpha/audit?");
            string focus = Query(href, "returnFocus").ShouldNotBeNull();
            string returnUrl = Query(href, "returnUrl").ShouldNotBeNull();
            cut.FindAll("#" + focus).Count.ShouldBe(1);
            cut.Find("#" + focus).QuerySelector("strong").ShouldNotBeNull().TextContent.ShouldBe("tenant.alpha");
            TenantAuditNavigationSafety.SafeReturnUrlForTenant(returnUrl, "tenant.alpha", out _).ShouldBe(returnUrl);
            TenantAuditNavigationSafety.IsFocusForReturnUrl(returnUrl, focus).ShouldBeTrue();
        }
    }

    private static string[] AuditEntryPointMarkers(string surface)
        => surface == "tenant"
            ? ["tenants-list-audit-entrypoint", "tenants-list-mobile-audit-entrypoint"]
            : [$"{surface}-audit-entrypoint"];

    private IRenderedComponent<IComponent> RenderGrid(string surface, string[] identities)
    {
        TenantListNavigationContext navigation = new(TenantWorkspaceState.FromQuery(null,
            surface == "tenants-my" ? "mine" : null, null, null, null, null, null, null, null, null));
        if (surface == "tenant")
        {
            return Render<TenantDataGrid>(parameters => parameters
                .Add(p => p.Rows, identities.Select(ListRow).ToArray())
                .Add(p => p.DetailHref, navigation.ToDetailUrl)
                .Add(p => p.AuditHref, navigation.ToAuditUrl)
                .Add(p => p.AuditReadAvailable, true));
        }

        Func<UserTenantMembershipRow, string>? detailHref = surface == "tenants-my"
            ? row => navigation.ToDetailUrl(row.TenantId, TenantDomIdentity.ElementId(surface + "-detail-", row.TenantId))
            : null;
        return Render<MyTenantsDataGrid>(parameters => parameters
            .Add(p => p.Rows, identities.Select(MembershipRow).ToArray())
            .Add(p => p.SelectorPrefix, surface)
            .Add(p => p.DetailHref, detailHref)
            .Add(p => p.TargetUserId, surface == "tenants-user" ? "operator-user" : null)
            .Add(p => p.ReturnUrl, surface == "tenants-my" ? "/tenants?scope=mine" : "/tenants/users?userId=operator-user")
            .Add(p => p.SourceKind, surface == "tenants-my" ? "my-tenants" : "user-lookup")
            .Add(p => p.AuditReadAvailable, true));
    }

    private void RegisterGateway(string[] identities)
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetMyTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(UserTenantMembershipSnapshot.Ready(identities.Select(MembershipRow).ToArray(), null, false, "\"etag\"", ReadModelFreshnessState.Current)));
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Ready(identities.Select(ListRow).ToArray(), null, false, "\"etag\"", ReadModelFreshnessState.Current, false)));
        gateway.GetUserTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(UserTenantMembershipSnapshot.Ready(identities.Select(MembershipRow).ToArray(), null, false, "\"etag\"", ReadModelFreshnessState.Current, "operator-user")));
        Services.AddSingleton(gateway);
        IUserContextAccessor user = Substitute.For<IUserContextAccessor>();
        user.UserId.Returns("operator-user");
        Services.AddSingleton(user);
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private static TenantListRow ListRow(string tenantId)
        => new(tenantId, tenantId, TenantStatus.Active, TenantCountValue.Unknown, TenantCountValue.Unknown,
            TenantPendingState.None, ReadModelFreshnessState.Current, ProjectionLifecycleState.Current);

    private static UserTenantMembershipRow MembershipRow(string tenantId)
        => new(tenantId, tenantId, TenantStatus.Active, TenantRole.TenantOwner,
            ReadModelFreshnessState.Current, ProjectionLifecycleState.Current);

    private static string? Query(string href, string key)
        => TenantAuditNavigationSafety.ReturnQueryValue(href, key);

    private static void Export(string surface, string markup)
    {
        string? directory = Environment.GetEnvironmentVariable("TENANTS_IDENTITY_FIXTURE_DIRECTORY");
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, surface + ".html"), markup);
        }
    }
}
