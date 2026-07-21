using System.Globalization;

using Bunit;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed class TenantsWorkspaceTests : BunitContext
{
    public TenantsWorkspaceTests()
        // The workspace now renders Fluent UI v5 components (FluentSelect/FluentTextInput/FluentButton)
        // which import their JS modules in OnAfterRenderAsync. Loose JSInterop lets bUnit no-op those
        // imports instead of throwing under the default Strict mode.
        => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Workspace_renders_gateway_error_without_mock_tenant_data()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Error()));
        Services.AddSingleton(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-error']");

        cut.Find("[data-testid='tenants-list-error']").GetAttribute("role").ShouldBe("alert");
        cut.Markup.ShouldContain("The authorized tenant list could not be loaded");
        cut.Markup.ShouldNotContain("tenant-1", Case.Insensitive);
        cut.Markup.ShouldNotContain("sample tenant", Case.Insensitive);
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Workspace_exposes_keyboard_reachable_controls()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Unknown)));
        Services.AddSingleton(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-refresh']");

        // Controls are Fluent UI v5 components (no raw HTML controls), so they render as the
        // corresponding custom elements. Asserting the tag also guards against regressing to raw HTML.
        cut.Find("[data-testid='tenants-list-refresh']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-list-reset']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-list-search']").NodeName.ShouldBe("FLUENT-TEXT-INPUT");
    }

    [Fact]
    public void Workspace_default_route_renders_page_local_tabs_and_tenant_list_controls()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Unknown)));
        Services.AddSingleton(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-workspace-tabs']");

        cut.Find("[data-testid='tenants-workspace-tabs']").TextContent.ShouldContain("Tenants");
        cut.Find("[data-testid='tenants-workspace-tabs']").TextContent.ShouldContain("Users");
        cut.Find("[data-testid='tenants-list-refresh']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-list-search']").NodeName.ShouldBe("FLUENT-TEXT-INPUT");
        cut.Find("[data-testid='tenants-workspace-scope']").NodeName.ShouldBe("FLUENT-DROPDOWN");
        cut.FindAll("[data-testid='tenants-user-lookup-input']").ShouldBeEmpty();
    }

    [Fact]
    public void Workspace_unknown_tab_query_normalizes_to_tenants_tab()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Unknown)));
        Services.AddSingleton(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?tab=unknown");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-refresh']");

        cut.Find("[data-testid='tenants-workspace-tabs']").TextContent.ShouldContain("Tenants");
        cut.FindAll("[data-testid='tenants-user-lookup-input']").ShouldBeEmpty();
        gateway.Received(1)
            .ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Workspace_tenants_tab_mine_scope_uses_self_audit_gateway_without_client_filtering_list_page()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Unknown)));
        gateway.GetMyTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(UserTenantMembershipSnapshot.Ready(
                [MembershipRow("tenant.alpha", "Alpha", TenantRole.TenantOwner)],
                nextCursor: null,
                hasMore: false,
                eTag: "\"etag\"",
                freshness: ReadModelFreshnessState.Current)));
        Services.AddSingleton(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?tab=tenants&scope=mine");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-my-list']");

        gateway.DidNotReceive()
            .ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>());
        gateway.Received(1)
            .GetMyTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>());
        cut.Find("[data-testid='tenants-my-tenant-id']").TextContent.ShouldContain("tenant.alpha");
        string? auditHref = cut.Find("[data-testid='tenants-audit-entrypoint']").GetAttribute("href");
        auditHref.ShouldNotBeNull();
        auditHref.ShouldContain("returnUrl=");
    }

    [Fact]
    public void Workspace_mine_scope_restores_return_context_after_a_detail_drill_in()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Unknown)));
        gateway.GetMyTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(UserTenantMembershipSnapshot.Ready(
                [MembershipRow("tenant.alpha", "Alpha", TenantRole.TenantOwner)],
                nextCursor: null,
                hasMore: false,
                eTag: "\"etag\"",
                freshness: ReadModelFreshnessState.Current)));
        Services.AddSingleton(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        // Simulate returning from the shared tenant-detail route: scope=mine plus the restored selection and
        // return-focus anchor (AC5).
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants?tab=tenants&scope=mine&selected=tenant.alpha&anchor=tenants-my-row-tenant.alpha");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-my-list']");

        // The scope=mine return-context banner renders (distinct testid from the scope=all banner) and names
        // the restored selection.
        cut.Find("[data-testid='tenants-my-return-context']").TextContent.ShouldContain("tenant.alpha");
        cut.FindAll("[data-testid='tenants-list-return-context']").ShouldBeEmpty();

        // The canonical URL preserves the selection/anchor so a redirect does not strip the restored context.
        Services.GetRequiredService<NavigationManager>().Uri.ShouldBe(
            "http://localhost/tenants?tab=tenants&scope=mine&selected=tenant.alpha&anchor=tenants-my-row-tenant.alpha");
    }

    [Fact]
    public void Workspace_users_tab_prefills_lookup_from_query_without_claiming_all_users_inventory()
    {
        List<UserTenantMembershipRequest> requests = [];
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Unknown)));
        gateway.GetUserTenantsAsync(Arg.Any<UserTenantMembershipRequest>(), Arg.Any<UserTenantMembershipSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                requests.Add(call.ArgAt<UserTenantMembershipRequest>(0));
                return Task.FromResult(UserTenantMembershipSnapshot.Ready(
                    [MembershipRow("tenant.beta", "Beta", TenantRole.TenantReader)],
                    nextCursor: null,
                    hasMore: false,
                    eTag: "\"etag\"",
                    freshness: ReadModelFreshnessState.Current,
                    targetUserId: "USER.Target-01"));
            });
        Services.AddSingleton(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/tenants?tab=users&userId={Uri.EscapeDataString("USER.Target-01")}&sort=role");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-user-lookup-results']");

        gateway.DidNotReceive()
            .ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>());
        requests.ShouldHaveSingleItem().TargetUserId.ShouldBe("USER.Target-01");
        cut.Find("[data-testid='tenants-user-lookup-input']").GetAttribute("value").ShouldBe("USER.Target-01");
        cut.Markup.ShouldContain("lookup", Case.Insensitive);
        cut.Markup.ShouldNotContain("all users", Case.Insensitive);
    }

    [Fact]
    public void Workspace_hosts_create_flow_in_a_collapsed_accordion_without_a_duplicate_title()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Unknown)));
        Services.AddSingleton(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-create-accordion']");

        // The create block is grouped in a FluentAccordion item (the "Page sections" UX rule), and the
        // create form is hosted inside it rather than as a tall card pushing the list down.
        cut.Find("[data-testid='tenants-create-accordion']").NodeName.ShouldBe("FLUENT-ACCORDION-ITEM");
        cut.Find("[data-testid='tenants-create-flow']");

        // Collapsed by default: the item must not be expanded, so the tenant list stays the primary content.
        string? expanded = cut.Find("[data-testid='tenants-create-accordion']").GetAttribute("expanded");
        (string.IsNullOrEmpty(expanded) || expanded == "false").ShouldBeTrue();

        // The accordion header already shows the title, so the inner <h2> must not be rendered (no duplicate).
        cut.FindAll("#tenants-create-heading").ShouldBeEmpty();
    }

    [Fact]
    public void Workspace_allows_create_flow_when_list_freshness_is_unknown()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Unknown)));
        Services.AddSingleton(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-create-submit']");

        // Unknown freshness is the empty/first-tenant bootstrap state (an empty index has no persisted
        // ProjectedAt, so no staleness header is emitted). It must NOT block creation, otherwise the first
        // tenant can never be created. Only an authoritative Stale classification blocks the command.
        cut.Find("[data-testid='tenants-create-submit']").HasAttribute("disabled").ShouldBeFalse();
        cut.FindAll("[data-testid='tenants-create-unavailable-reason']").ShouldBeEmpty();
    }

    [Fact]
    public void Workspace_blocks_create_flow_when_list_freshness_is_stale()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Empty(isAuthorizationScoped: true, ReadModelFreshnessState.Stale)));
        Services.AddSingleton(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-create-submit']");

        // A definite Stale classification must still gate the create command behind a Refresh.
        cut.Find("[data-testid='tenants-create-submit']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-create-unavailable-reason']")
            .TextContent.ShouldContain("Refresh tenant data before submitting a command.");
    }

    private static UserTenantMembershipRow MembershipRow(string tenantId, string name, TenantRole role)
        => new(tenantId, name, TenantStatus.Active, role, ReadModelFreshnessState.Current);

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Workspace.Eyebrow"] = "Tenant workspace",
            ["Tenants.Workspace.StatusBadge"] = "Not connected",
            ["Tenants.Workspace.StatusFocusLink"] = "Review status details",
            ["Tenants.Workspace.Title"] = "Tenants",
            ["Tenants.Workspace.Scope.All"] = "All tenants",
            ["Tenants.Workspace.Scope.Label"] = "Tenant view",
            ["Tenants.Workspace.Scope.Mine"] = "My tenants",
            ["Tenants.Workspace.Tabs.Label"] = "Tenant workspace sections",
            ["Tenants.Workspace.Tabs.Tenants"] = "Tenants",
            ["Tenants.Workspace.Tabs.Users"] = "Users",
            ["Tenants.Workspace.UnavailableHeading"] = "Tenant read surfaces are not connected yet",
            ["Tenants.Workspace.UnavailableMessage"] = "The workspace shell is available, but tenant lists, tenant details, and command flows are not implemented in this bootstrap.",
            ["Tenants.List.Column.Freshness"] = "Truth state",
            ["Tenants.List.Column.Members"] = "Members",
            ["Tenants.List.Column.Owners"] = "Owners",
            ["Tenants.List.Column.Pending"] = "Pending",
            ["Tenants.List.Column.Status"] = "Status",
            ["Tenants.List.Column.Tenant"] = "Tenant",
            ["Tenants.List.ControlsLabel"] = "Tenant list controls",
            ["Tenants.List.Count.Unknown"] = "Unknown",
            ["Tenants.List.DetailLinkLabel"] = "Open tenant details for {0}",
            ["Tenants.List.Freshness.Unknown"] = "Unknown",
            ["Tenants.List.Next"] = "Next",
            ["Tenants.List.PaginationLabel"] = "Tenant list pages",
            ["Tenants.List.Pending.None"] = "No pending changes",
            ["Tenants.List.Pending.Unknown"] = "Pending state unknown",
            ["Tenants.List.Previous"] = "Previous",
            ["Tenants.List.Refresh"] = "Refresh",
            ["Tenants.List.Reset"] = "Reset filters",
            ["Tenants.List.ReturnContext"] = "Returned from tenant {0}. Filters, sort, cursor, and selection were restored before rendering.",
            ["Tenants.List.Reason.GatewayUnavailable"] = "The authorized tenant list could not be loaded. Try again later.",
            ["Tenants.List.SearchLabel"] = "Search tenants",
            ["Tenants.List.SearchPlaceholder"] = "Search by tenant id or name",
            ["Tenants.List.Sort.Name"] = "Name",
            ["Tenants.List.Sort.Status"] = "Status",
            ["Tenants.List.Sort.TenantId"] = "Tenant id",
            ["Tenants.List.SortDirection.Ascending"] = "Ascending",
            ["Tenants.List.SortDirection.Descending"] = "Descending",
            ["Tenants.List.SortDirectionLabel"] = "Sort direction",
            ["Tenants.List.SortLabel"] = "Sort",
            ["Tenants.List.State.Empty.Message"] = "No tenants are visible for this operator.",
            ["Tenants.List.State.Empty.Title"] = "No visible tenants",
            ["Tenants.List.State.Error.Message"] = "Tenant data could not be loaded.",
            ["Tenants.List.State.Error.Title"] = "Tenants unavailable",
            ["Tenants.List.StatusFilter.Active"] = "Active",
            ["Tenants.List.StatusFilter.All"] = "All statuses",
            ["Tenants.List.StatusFilter.Disabled"] = "Disabled",
            ["Tenants.List.StatusFilter.Unknown"] = "Unknown",
            ["Tenants.List.StatusFilterLabel"] = "Status",
            ["Tenants.List.Title"] = "Tenants",
            ["Tenants.MyTenants.Column.Freshness"] = "Freshness",
            ["Tenants.MyTenants.Column.Role"] = "Role",
            ["Tenants.MyTenants.Column.Status"] = "Status",
            ["Tenants.MyTenants.Column.Tenant"] = "Tenant",
            ["Tenants.MyTenants.ControlsLabel"] = "My Tenants controls",
            ["Tenants.MyTenants.Description"] = "Read-only view of tenants visible to your signed-in account.",
            ["Tenants.MyTenants.DetailLinkLabel"] = "Open tenant details for {0}",
            ["Tenants.MyTenants.Freshness.Current"] = "Current",
            ["Tenants.MyTenants.Freshness.Stale"] = "Stale",
            ["Tenants.MyTenants.Freshness.Unknown"] = "Unknown",
            ["Tenants.MyTenants.Link"] = "My tenants",
            ["Tenants.MyTenants.Next"] = "Next",
            ["Tenants.MyTenants.PaginationLabel"] = "My Tenants pages",
            ["Tenants.MyTenants.Previous"] = "Previous",
            ["Tenants.MyTenants.Refresh"] = "Refresh",
            ["Tenants.MyTenants.ReturnContext"] = "Returned from tenant {0}. Your My Tenants view was restored on the authorized first page.",
            ["Tenants.MyTenants.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.MyTenants.Role.TenantReader"] = "Tenant reader",
            ["Tenants.MyTenants.Role.Unknown"] = "Unknown role",
            ["Tenants.MyTenants.RoleAccessible"] = "Role: {0}",
            ["Tenants.MyTenants.State.Loading.Message"] = "Your tenant memberships are loading from the server-side query gateway.",
            ["Tenants.MyTenants.State.Loading.Title"] = "Loading my tenants",
            ["Tenants.MyTenants.Status.Active"] = "Active",
            ["Tenants.MyTenants.Status.Unknown"] = "Unknown status",
            ["Tenants.MyTenants.StatusAccessible"] = "Status: {0}",
            ["Tenants.MyTenants.Summary"] = "Tenants shown: {0}",
            ["Tenants.MyTenants.Title"] = "My Tenants",
            ["Tenants.UserLookup.Announcement.Loading"] = "Looking up visible memberships for {0}.",
            ["Tenants.UserLookup.Announcement.Ready"] = "{0} visible memberships loaded for {1}.",
            ["Tenants.UserLookup.Clear"] = "Clear",
            ["Tenants.UserLookup.Column.Freshness"] = "Freshness",
            ["Tenants.UserLookup.Column.Role"] = "Role",
            ["Tenants.UserLookup.Column.Status"] = "Status",
            ["Tenants.UserLookup.Column.Tenant"] = "Tenant",
            ["Tenants.UserLookup.Description"] = "Read-only membership lookup for a caller-supplied user identifier.",
            ["Tenants.UserLookup.FormLabel"] = "User membership lookup controls",
            ["Tenants.UserLookup.Freshness.Current"] = "Current",
            ["Tenants.UserLookup.Freshness.Stale"] = "Stale",
            ["Tenants.UserLookup.Freshness.Unknown"] = "Unknown",
            ["Tenants.UserLookup.Initial.Message"] = "Enter a user identifier to run an authorization-scoped membership lookup.",
            ["Tenants.UserLookup.Initial.Title"] = "User membership lookup ready",
            ["Tenants.UserLookup.InputHelp"] = "Use the exact caller-supplied user identifier.",
            ["Tenants.UserLookup.InputLabel"] = "User identifier",
            ["Tenants.UserLookup.Link"] = "User lookup",
            ["Tenants.UserLookup.Next"] = "Next",
            ["Tenants.UserLookup.PaginationLabel"] = "User membership result pages",
            ["Tenants.UserLookup.Previous"] = "Previous",
            ["Tenants.UserLookup.Refresh"] = "Refresh",
            ["Tenants.UserLookup.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.UserLookup.Role.TenantReader"] = "Tenant reader",
            ["Tenants.UserLookup.Role.Unknown"] = "Unknown role",
            ["Tenants.UserLookup.RoleAccessible"] = "Role: {0}",
            ["Tenants.UserLookup.Sort.Name"] = "Name",
            ["Tenants.UserLookup.Sort.Role"] = "Role",
            ["Tenants.UserLookup.Sort.Status"] = "Status",
            ["Tenants.UserLookup.Sort.Tenant"] = "Tenant identifier",
            ["Tenants.UserLookup.SortLabel"] = "Sort results",
            ["Tenants.UserLookup.State.Ready.Title"] = "Visible memberships loaded",
            ["Tenants.UserLookup.Status.Active"] = "Active",
            ["Tenants.UserLookup.Status.Unknown"] = "Unknown status",
            ["Tenants.UserLookup.StatusAccessible"] = "Status: {0}",
            ["Tenants.UserLookup.Submit"] = "Look up",
            ["Tenants.UserLookup.TargetContext"] = "Lookup target: {0}",
            ["Tenants.UserLookup.Title"] = "User membership lookup",
            ["Tenants.Create.Title"] = "Create tenant",
            ["Tenants.Create.Description"] = "Submit a tenant creation command and wait for projection confirmation.",
            ["Tenants.Create.TenantId.Label"] = "Tenant id",
            ["Tenants.Create.TenantId.Help"] = "Use the exact caller-supplied tenant id.",
            ["Tenants.Create.Name.Label"] = "Name",
            ["Tenants.Create.Description.Label"] = "Description",
            ["Tenants.Create.Submit"] = "Create tenant",
            ["Tenants.Create.Refresh"] = "Refresh status",
            ["Tenants.Create.Lifecycle.Title"] = "Command lifecycle",
            ["Tenants.Create.Validation.TenantIdRequired"] = "Tenant id is required.",
            ["Tenants.Create.Validation.NameRequired"] = "Name is required.",
            ["Tenants.Create.Unavailable.Authorization"] = "You are not authorized to create tenants.",
            ["Tenants.Create.Unavailable.Freshness"] = "Refresh tenant data before submitting a command.",
            ["Tenants.Create.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.Create.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.Create.State.Idle"] = "No command submitted.",
            ["Tenants.Create.State.RequestSent"] = "Request sent.",
            ["Tenants.Create.State.Accepted"] = "Accepted by EventStore; waiting for processing.",
            ["Tenants.Create.State.ProjectionPending"] = "Projection pending; tenant is not confirmed visible yet.",
            ["Tenants.Create.State.Confirmed"] = "Projection confirmed the tenant exists.",
            ["Tenants.Create.State.Rejected"] = "Command rejected.",
            ["Tenants.Create.State.Failed"] = "Command submission failed.",
            ["Tenants.Create.State.Degraded"] = "Command result is degraded and needs review.",
            ["Tenants.Create.State.UnableToVerify"] = "Unable to verify command result.",
            ["Tenants.Create.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.Create.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.Create.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.Create.Audit.MissingSupport"] = "Audit support is missing for this flow.",
            ["Tenants.Audit.EntryPoint.Accessible.Command"] = "Open audit evidence for {0} in tenant {1}",
            ["Tenants.Audit.EntryPoint.CommandReason"] = "Open the tenant audit list and use the visible audit state.",
            ["Tenants.Audit.EntryPoint.Label"] = "Audit evidence",
            ["Tenants.MyTenants.AuditAccessibleLabel"] = "Open audit evidence for tenant {0}",
            ["Tenants.UserLookup.AuditAccessibleLabel"] = "Open audit evidence for user {0} in tenant {1}",
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope.",
        };

        public LocalizedString this[string name]
            => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(
                CultureInfo.CurrentCulture,
                Values.TryGetValue(name, out string? value) ? value : name,
                arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandStatusResult.Unknown("Tenant command status is unavailable."));
    }

    private sealed class StubTenantsBffComposition : ITenantsBffComposition
    {
        public bool IsReadSurfaceConnected => true;

        public bool IsCommandSurfaceConnected => true;
    }
}
