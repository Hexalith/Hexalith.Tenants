using System.Globalization;

using Bunit;

using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
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
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
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
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
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
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope.",
        };

        public LocalizedString this[string name]
            => new(name, Values[name]);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values[name], arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRoleCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfigurationCommandRequest request, CancellationToken cancellationToken = default)
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
