using System.Globalization;
using System.Xml.Linq;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Layout;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Tenants;
using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.Components.Users;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantUsers;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class AuditEvidenceEntryPointTests : BunitContext
{
    [Fact]
    public void Audit_entry_point_builds_scoped_link_with_return_and_user_context()
    {
        RegisterLocalizer();

        IRenderedComponent<AuditEvidenceEntryPoint> cut = Render<AuditEvidenceEntryPoint>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.TargetUserId, "user.alpha")
            .Add(component => component.SourceKind, "member-row")
            .Add(component => component.SourceTestId, "tenants-member-audit-entrypoint")
            .Add(component => component.ReturnUrl, "/tenants/tenant.alpha?returnUrl=%2Ftenants%3Fsearch%3Dalpha")
            .Add(component => component.ReturnFocus, "tenants-member-user.alpha")
            .Add(component => component.Label, "Audit evidence")
            .Add(component => component.AccessibleName, "Open audit evidence for user user.alpha in tenant tenant.alpha"));

        AngleSharp.Dom.IElement link = cut.Find("[data-testid='tenants-audit-entrypoint']");

        link.TagName.ShouldBe("FLUENT-ANCHOR-BUTTON");
        RequiredAttribute(link, "data-testid").ShouldContain("tenants-audit-entrypoint");
        string href = RequiredAttribute(EntryPointFromMarker(cut, "tenants-member-audit-entrypoint"), "href");
        href.ShouldContain("/tenants/tenant.alpha/audit?");
        href.ShouldContain("targetUserId=user.alpha");
        href.ShouldContain("source=member-row");
        href.ShouldContain("returnFocus=tenants-member-user.alpha");
        RequiredAttribute(link, "aria-label").ShouldBe("Open audit evidence for user user.alpha in tenant tenant.alpha");
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("CorrelationId", Case.Insensitive);
    }

    [Fact]
    public void Audit_entry_point_drops_unsafe_return_url_and_control_character_context()
    {
        RegisterLocalizer();

        IRenderedComponent<AuditEvidenceEntryPoint> cut = Render<AuditEvidenceEntryPoint>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.SupportSafeCommandReference, "command-safe\nraw")
            .Add(component => component.SourceKind, "command-result")
            .Add(component => component.SourceTestId, "tenants-command-audit-entrypoint")
            .Add(component => component.ReturnUrl, "https://example.test/tenants/tenant.alpha")
            .Add(component => component.ReturnFocus, "tenants-command-lifecycle\u0001raw")
            .Add(component => component.Label, "Audit evidence")
            .Add(component => component.AccessibleName, "Open audit evidence for command result in tenant tenant.alpha"));

        string auditHref = RequiredAttribute(EntryPointFromMarker(cut, "tenants-command-audit-entrypoint"), "href");

        auditHref.ShouldContain("/tenants/tenant.alpha/audit?");
        auditHref.ShouldContain("source=command-result");
        auditHref.ShouldNotContain("returnUrl=", Case.Insensitive);
        auditHref.ShouldNotContain("supportSafeCommandReference=", Case.Insensitive);
        auditHref.ShouldNotContain("returnFocus=", Case.Insensitive);
        auditHref.ShouldNotContain("example.test", Case.Insensitive);
        auditHref.ShouldNotContain("raw", Case.Insensitive);
    }

    [Fact]
    public void Audit_entry_point_fails_closed_without_tenant_scope()
    {
        RegisterLocalizer();

        IRenderedComponent<AuditEvidenceEntryPoint> cut = Render<AuditEvidenceEntryPoint>(parameters => parameters
            .Add(component => component.SourceTestId, "tenants-list-audit-entrypoint")
            .Add(component => component.Label, "Audit evidence")
            .Add(component => component.AccessibleName, "Open audit evidence for tenant tenant.alpha"));

        AngleSharp.Dom.IElement entryPoint = cut.Find("[data-testid='tenants-audit-entrypoint']");

        entryPoint.TagName.ShouldBe("FLUENT-BUTTON");
        entryPoint.HasAttribute("disabled").ShouldBeTrue();
        entryPoint.TextContent.ShouldContain("Tenant scope is required before audit evidence can be opened.");
        cut.FindAll("a").ShouldBeEmpty();
    }

    [Fact]
    public void Tenant_row_entry_point_preserves_existing_detail_link_and_list_return_context()
    {
        RegisterFluentServices();
        TenantListNavigationContext context = new(TenantWorkspaceState.FromQuery(
            tab: null,
            scope: null,
            userId: null,
            search: "alpha",
            status: TenantStatus.Active.ToString(),
            sort: TenantListSortColumns.Name,
            sortDescending: bool.TrueString,
            cursor: "opaque-cursor",
            selectedTenantId: null,
            anchor: null));
        TenantListRow row = TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)) with
        {
            MemberCount = TenantCountValue.Known(2),
            OwnerCount = TenantCountValue.Known(1),
            Freshness = ReadModelFreshnessState.Current,
        };

        IRenderedComponent<TenantDataGrid> cut = Render<TenantDataGrid>(parameters => parameters
            .Add(component => component.Rows, [row])
            .Add(component => component.DetailHref, selected => context.ToDetailUrl(selected))
            .Add(component => component.AuditHref, selected => context.ToAuditUrl(selected)));

        RequiredAttribute(cut.Find("[data-testid='tenants-list-detail-link']"), "href").ShouldStartWith("/tenants/tenant.alpha?returnUrl=");
        string auditHref = RequiredAttribute(EntryPointFromMarker(cut, "tenants-list-audit-entrypoint"), "href");
        auditHref.ShouldContain("/tenants/tenant.alpha/audit?");
        auditHref.ShouldContain("returnUrl=");
        Uri.UnescapeDataString(auditHref).ShouldContain("selected=tenant.alpha");
        auditHref.ShouldContain("source=tenant-list");
    }

    [Fact]
    public void User_lookup_grid_carries_target_user_context_without_primary_users_navigation()
    {
        RegisterFluentServices();
        UserTenantMembershipRow row = new("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader, ReadModelFreshnessState.Current);

        IRenderedComponent<MyTenantsDataGrid> cut = Render<MyTenantsDataGrid>(parameters => parameters
            .Add(component => component.Rows, [row])
            .Add(component => component.ResourcePrefix, "Tenants.UserLookup")
            .Add(component => component.SelectorPrefix, "tenants-user")
            .Add(component => component.TargetUserId, "user.alpha")
            .Add(component => component.ReturnUrl, "/tenants/users?userId=user.alpha&sort=role")
            .Add(component => component.SourceKind, "user-lookup"));

        string auditHref = RequiredAttribute(EntryPointFromMarker(cut, "tenants-user-audit-entrypoint"), "href");
        auditHref.ShouldContain("/tenants/tenant.alpha/audit?");
        auditHref.ShouldContain("targetUserId=user.alpha");
        auditHref.ShouldContain("source=user-lookup");
        cut.Markup.ShouldNotContain("data-testid=\"tenants-nav-users\"");
    }

    [Fact]
    public void Member_row_entry_point_uses_detail_tenant_and_member_user_scope()
    {
        RegisterFluentServices();
        TenantDetail detail = Detail("tenant.alpha");

        IRenderedComponent<MemberAccessReview> cut = Render<MemberAccessReview>(parameters => parameters
            .Add(component => component.Detail, detail)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, ReadModelFreshnessState.Current)
            .Add(component => component.Lifecycle, ProjectionLifecycleState.Current)
            .Add(component => component.ProjectionVersion, "v1")
            .Add(component => component.Members, TenantUsersSnapshot.Ready(
                detail.TenantId,
                detail.Members,
                nextCursor: null,
                hasMore: false,
                eTag: "members-etag",
                projectionVersion: "v1",
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current)));

        string auditHref = RequiredAttribute(EntryPointFromMarker(cut, "tenants-member-audit-entrypoint"), "href");
        auditHref.ShouldContain("/tenants/tenant.alpha/audit?");
        auditHref.ShouldContain("targetUserId=user.alpha");
        auditHref.ShouldContain("source=member-row");
    }

    [Fact]
    public void Command_result_entry_point_links_to_tenant_audit_without_claiming_command_proof()
    {
        RegisterLocalizer();

        IRenderedComponent<AuditEvidenceEntryPoint> cut = Render<AuditEvidenceEntryPoint>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.SourceKind, "command-result")
            .Add(component => component.SourceTestId, "tenants-command-audit-entrypoint")
            .Add(component => component.Label, "Audit evidence")
            .Add(component => component.AccessibleName, "Open audit evidence for audit evidence delayed in tenant tenant.alpha")
            .Add(component => component.ReturnUrl, "/tenants/tenant.alpha")
            .Add(component => component.ReturnFocus, "tenants-config-set-lifecycle")
            .Add(component => component.AvailabilityText, "Audit evidence delayed."));

        string auditHref = RequiredAttribute(EntryPointFromMarker(cut, "tenants-command-audit-entrypoint"), "href");

        auditHref.ShouldContain("/tenants/tenant.alpha/audit?");
        auditHref.ShouldContain("source=command-result");
        cut.Find("[data-testid='tenants-audit-entrypoint']").TextContent.ShouldContain("Audit evidence delayed.");
        cut.Markup.ShouldNotContain("receipt", Case.Insensitive);
        cut.Markup.ShouldNotContain("proof confirmed", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_accepts_context_without_changing_gateway_contract()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetTenantAuditAsync(Arg.Any<TenantAuditRequest>(), Arg.Any<TenantAuditSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(TenantAuditSnapshot.Empty(true, ReadModelFreshnessState.Current, null, call.ArgAt<TenantAuditRequest>(0))));
        RegisterFluentServices(gateway);
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo("/tenants/tenant.alpha/audit?targetUserId=user.alpha&source=member-row&returnUrl=%2Ftenants%2Ftenant.alpha&returnFocus=tenants-member-user.alpha");

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-context']");

        cut.Find("[data-testid='tenants-audit-context']").TextContent.ShouldContain("user.alpha");
        RequiredAttribute(cut.Find("[data-testid='tenants-audit-back']"), "href").ShouldBe("/tenants/tenant.alpha");
        gateway.Received(1).GetTenantAuditAsync(
            Arg.Is<TenantAuditRequest>(request => request != null && request.TenantId == "tenant.alpha" && request.Cursor == null),
            Arg.Any<TenantAuditSnapshot?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Tenant_audit_page_source_context_uses_localized_label_not_raw_token()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetTenantAuditAsync(Arg.Any<TenantAuditRequest>(), Arg.Any<TenantAuditSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(TenantAuditSnapshot.Empty(true, ReadModelFreshnessState.Current, null, call.ArgAt<TenantAuditRequest>(0))));
        RegisterFluentServices(gateway);
        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>()
            .NavigateTo("/tenants/tenant.alpha/audit?source=tenant-list");

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-context']");

        string context = cut.Find("[data-testid='tenants-audit-context']").TextContent;
        context.ShouldContain("the tenant list");
        context.ShouldNotContain("tenant-list");
    }

    [Fact]
    public void Audit_entry_point_resource_keys_have_english_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> english = ResourceKeys(Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources", "TenantsResources.resx"));
        HashSet<string> french = ResourceKeys(Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources", "TenantsResources.fr.resx"));
        string[] keys = english.Where(static key => key.StartsWith("Tenants.Audit.EntryPoint.", StringComparison.Ordinal)).ToArray();

        keys.ShouldNotBeEmpty();
        foreach (string key in keys)
        {
            french.ShouldContain(key);
        }
    }

    [Fact]
    public void Audit_entry_point_styles_include_focus_forced_colors_and_dense_layout_hooks()
    {
        string projectRoot = ProjectRoot();
        string styles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Audit",
            "AuditEvidenceEntryPoint.razor.css"));

        styles.ShouldContain(":focus-visible");
        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain("min-inline-size:");
        styles.ShouldContain("max-inline-size: 100%");
    }

    private void RegisterFluentServices(ITenantQueryGateway? gateway = null)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(gateway ?? Substitute.For<ITenantQueryGateway>());
        Services.AddSingleton<ITenantCommandGateway>(Substitute.For<ITenantCommandGateway>());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
    }

    private void RegisterLocalizer()
    {
        Services.AddFluentUIComponents();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
    }

    private static TenantDetail Detail(string tenantId)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            [new TenantMember("user.alpha", TenantRole.TenantOwner)],
            new Dictionary<string, string>(),
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture));

    private static string RequiredAttribute(AngleSharp.Dom.IElement element, string attributeName)
        => element.GetAttribute(attributeName) ?? throw new InvalidOperationException($"Expected {attributeName} attribute.");

    private static AngleSharp.Dom.IElement EntryPointFromMarker<TComponent>(IRenderedComponent<TComponent> cut, string testId)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
        => cut.Find($"[data-testid='{testId}']").ParentElement
            ?? throw new InvalidOperationException($"Expected parent entry point for {testId}.");

    private static HashSet<string> ResourceKeys(string path)
        => XDocument.Load(path)
            .Descendants("data")
            .Select(element => element.Attribute("name")?.Value)
            .Where(static key => !string.IsNullOrWhiteSpace(key))
            .Select(static key => key!)
            .ToHashSet(StringComparer.Ordinal);

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private sealed class StubTenantsBffComposition : ITenantsBffComposition
    {
        public bool IsReadSurfaceConnected => true;

        public bool IsCommandSurfaceConnected => true;

        public TenantLifecycleAuthorizationReflectionState GlobalAdministratorsAuthorizationReflection => TenantLifecycleAuthorizationReflectionState.Authorized;
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Audit.Back"] = "Back to tenant details",
            ["Tenants.Audit.Category.Access"] = "Access",
            ["Tenants.Audit.Category.Administrative"] = "Administrative",
            ["Tenants.Audit.Context.Command"] = "Command audit context {0} is selected. Tenant-scoped audit filters remain authoritative.",
            ["Tenants.Audit.Context.Source"] = "Audit opened from {0}. Tenant-scoped audit filters remain authoritative.",
            ["Tenants.Audit.Context.User"] = "Audit context for user {0} is selected. Tenant-scoped audit filters remain authoritative.",
            ["Tenants.Audit.Context.SourceKind.TenantList"] = "the tenant list",
            ["Tenants.Audit.Context.SourceKind.TenantDetail"] = "tenant detail",
            ["Tenants.Audit.Context.SourceKind.MemberRow"] = "a member row",
            ["Tenants.Audit.Context.SourceKind.UserLookup"] = "user lookup",
            ["Tenants.Audit.Context.SourceKind.MyTenants"] = "your tenants",
            ["Tenants.Audit.Context.SourceKind.CommandResult"] = "a command result",
            ["Tenants.Audit.Context.SourceKind.Default"] = "another tenant surface",
            ["Tenants.Audit.ControlsLabel"] = "Tenant audit filters and paging controls",
            ["Tenants.Audit.Description"] = "Read-only tenant audit evidence from the server-side query gateway.",
            ["Tenants.Audit.EntryPoint.Accessible.Command"] = "Open audit evidence for {0} in tenant {1}",
            ["Tenants.Audit.EntryPoint.Accessible.Member"] = "Open audit evidence for user {0} in tenant {1}",
            ["Tenants.Audit.EntryPoint.Accessible.Tenant"] = "Open audit evidence for tenant {0} from {1}",
            ["Tenants.Audit.EntryPoint.Label"] = "Audit evidence",
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required before audit evidence can be opened.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope before opening audit evidence.",
            ["Tenants.Audit.EntryPoint.CommandReason"] = "Command-specific proof is not available here; open the tenant audit list and use the visible audit state.",
            ["Tenants.Audit.Eyebrow"] = "Tenant audit trail",
            ["Tenants.Audit.Filter.Category"] = "Category",
            ["Tenants.Audit.Filter.Category.All"] = "All categories",
            ["Tenants.Audit.Filter.From"] = "From",
            ["Tenants.Audit.Filter.To"] = "To",
            ["Tenants.Audit.GridTitle"] = "Audit entries",
            ["Tenants.Audit.PaginationLabel"] = "Tenant audit pages",
            ["Tenants.Audit.Refresh"] = "Refresh",
            ["Tenants.Audit.Reset"] = "Reset filters",
            ["Tenants.Audit.ReturnContext"] = "Return context restored. Focus target: {0}",
            ["Tenants.Audit.State.Empty.Message"] = "No audit entries are visible for this tenant scope.",
            ["Tenants.Audit.State.Empty.Title"] = "No audit entries",
            ["Tenants.Audit.State.Loading.Message"] = "Audit entries are loading through the server-side query gateway.",
            ["Tenants.Audit.State.Loading.Title"] = "Loading audit entries",
            ["Tenants.Audit.Title"] = "Audit trail for {0}",
            ["Tenants.List.AuditAccessibleLabel"] = "Open audit evidence for tenant {0} from tenant list",
            ["Tenants.List.Column.Audit"] = "Audit",
            ["Tenants.List.Column.Freshness"] = "Truth state",
            ["Tenants.List.Column.Members"] = "Members",
            ["Tenants.List.Column.Owners"] = "Owners",
            ["Tenants.List.Column.Pending"] = "Pending",
            ["Tenants.List.Column.Status"] = "Status",
            ["Tenants.List.Column.Tenant"] = "Tenant",
            ["Tenants.List.Count.Unknown"] = "Unknown",
            ["Tenants.List.DetailLinkLabel"] = "Open tenant details for {0}",
            ["Tenants.List.Pending.None"] = "No pending changes",
            ["Tenants.List.Status.Active"] = "Active",
            ["Tenants.Members.Action.AddMember"] = "Add member",
            ["Tenants.Members.Action.ChangeRole"] = "Change role",
            ["Tenants.Members.Action.RemoveMember"] = "Remove member",
            ["Tenants.Members.AuditAccessibleLabel"] = "Open audit evidence for user {0} in tenant {1}",
            ["Tenants.Members.Column.Actions"] = "Action availability",
            ["Tenants.Members.Column.Audit"] = "Audit",
            ["Tenants.Members.Column.Freshness"] = "Freshness",
            ["Tenants.Members.Column.OwnerContext"] = "Owner context",
            ["Tenants.Members.Column.Role"] = "Role",
            ["Tenants.Members.Column.Status"] = "Tenant status",
            ["Tenants.Members.Column.UserId"] = "User id",
            ["Tenants.Members.Description"] = "Read-only member access context from the dedicated authorized member projection.",
            ["Tenants.Members.OwnerContext.LastOwner"] = "{0} visible owner; last-owner changes require a later high-impact flow.",
            ["Tenants.Members.ReasonCatalogLabel"] = "Canonical unavailable action reason categories",
            ["Tenants.Members.ReasonListLabel"] = "Unavailable action reasons for {0}",
            ["Tenants.Members.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.Members.RoleAccessible"] = "Role: {0}",
            ["Tenants.Members.ScopeNotice"] = "Visible members only. Orphan context is unavailable in this read model; disabled lifecycle is shown from tenant status.",
            ["Tenants.Members.Status.Active"] = "Active",
            ["Tenants.Members.StatusAccessible"] = "Tenant status {0}",
            ["Tenants.Members.Title"] = "Member access review",
            ["Tenants.Members.UnavailableReason.HighImpactFlowNotReady"] = "high-impact flow not ready",
            ["Tenants.Members.UnavailableReason.MissingAuditProof"] = "missing audit proof",
            ["Tenants.Members.UnavailableReason.MissingConsequencePreview"] = "missing consequence preview",
            ["Tenants.Members.UnavailableReason.MissingLifecycleSupport"] = "missing lifecycle support",
            ["Tenants.Members.UnavailableReason.MissingPermission"] = "missing permission",
            ["Tenants.Members.UnavailableReason.StaleData"] = "stale data",
            ["Tenants.Members.UserIdAccessible"] = "Literal member user identifier {0}",
            ["Tenants.MyTenants.AuditAccessibleLabel"] = "Open audit evidence for tenant {0}",
            ["Tenants.MyTenants.Column.Freshness"] = "Freshness",
            ["Tenants.MyTenants.Column.Role"] = "Role",
            ["Tenants.MyTenants.Column.Status"] = "Status",
            ["Tenants.MyTenants.Column.Tenant"] = "Tenant",
            ["Tenants.MyTenants.Freshness.Current"] = "Current",
            ["Tenants.MyTenants.Role.TenantReader"] = "Tenant reader",
            ["Tenants.MyTenants.RoleAccessible"] = "Role: {0}",
            ["Tenants.MyTenants.Status.Active"] = "Active",
            ["Tenants.MyTenants.StatusAccessible"] = "Status: {0}",
            ["Tenants.Navigation.AriaLabel"] = "Operations shell primary navigation",
            ["Tenants.Navigation.Audit"] = "Audit",
            ["Tenants.Navigation.AuditUnavailable"] = "Choose a tenant before opening audit evidence.",
            ["Tenants.Navigation.GlobalAdministrators"] = "Global Administrators",
            ["Tenants.Navigation.Tenants"] = "Tenants",
            ["Tenants.UserLookup.AuditAccessibleLabel"] = "Open audit evidence for user {0} in tenant {1}",
            ["Tenants.UserLookup.Column.Freshness"] = "Freshness",
            ["Tenants.UserLookup.Column.Role"] = "Role",
            ["Tenants.UserLookup.Column.Status"] = "Status",
            ["Tenants.UserLookup.Column.Tenant"] = "Tenant",
            ["Tenants.UserLookup.Freshness.Current"] = "Current",
            ["Tenants.UserLookup.Role.TenantReader"] = "Tenant reader",
            ["Tenants.UserLookup.RoleAccessible"] = "Role: {0}",
            ["Tenants.UserLookup.Status.Active"] = "Active",
            ["Tenants.UserLookup.StatusAccessible"] = "Status: {0}",
        };

        public LocalizedString this[string name]
            => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static value => new LocalizedString(value.Key, value.Value));
    }
}
