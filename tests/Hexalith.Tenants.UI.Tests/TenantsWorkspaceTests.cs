using System.Globalization;

using Bunit;

using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed class TenantsWorkspaceTests : BunitContext
{
    [Fact]
    public void Workspace_renders_gateway_error_without_mock_tenant_data()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Error("Tenant query gateway configuration is missing.")));
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-error']");

        cut.Find("[data-testid='tenants-list-error']").GetAttribute("role").ShouldBe("alert");
        cut.Markup.ShouldContain("Tenant query gateway configuration is missing");
        cut.Markup.ShouldNotContain("tenant-1", Case.Insensitive);
        cut.Markup.ShouldNotContain("sample tenant", Case.Insensitive);
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Workspace_exposes_keyboard_reachable_controls()
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantListSnapshot.Empty(isAuthorizationScoped: true, TenantFreshnessState.Unknown)));
        Services.AddSingleton(gateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-refresh']");

        cut.Find("[data-testid='tenants-list-refresh']").GetAttribute("type").ShouldBe("button");
        cut.Find("[data-testid='tenants-list-reset']").GetAttribute("type").ShouldBe("button");
        cut.Find("[data-testid='tenants-list-search']").GetAttribute("type").ShouldBe("search");
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Workspace.Eyebrow"] = "Tenant workspace",
            ["Tenants.Workspace.StatusBadge"] = "Not connected",
            ["Tenants.Workspace.StatusFocusLink"] = "Review status details",
            ["Tenants.Workspace.Title"] = "Tenants",
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
            ["Tenants.MyTenants.Link"] = "My tenants",
            ["Tenants.UserLookup.Link"] = "User lookup",
        };

        public LocalizedString this[string name]
            => new(name, Values[name]);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values[name], arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }
}
