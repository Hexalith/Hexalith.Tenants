using System.Globalization;

using Bunit;

using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed class TenantsWorkspaceTests : BunitContext
{
    [Fact]
    public void Workspace_renders_unavailable_status_without_mock_tenant_data()
    {
        ITenantsBffComposition composition = Substitute.For<ITenantsBffComposition>();
        composition.IsReadSurfaceConnected.Returns(false);
        Services.AddSingleton(composition);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();

        cut.Find("[data-testid='tenants-shell-status']").GetAttribute("role").ShouldBe("status");
        cut.Find("[data-testid='tenants-shell-status']").GetAttribute("data-connected").ShouldBe("false");
        cut.Markup.ShouldContain("Tenant read surfaces are not connected yet");
        cut.Markup.ShouldNotContain("tenant-1", Case.Insensitive);
        cut.Markup.ShouldNotContain("sample tenant", Case.Insensitive);
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Workspace_exposes_focusable_status_selector()
    {
        ITenantsBffComposition composition = Substitute.For<ITenantsBffComposition>();
        Services.AddSingleton(composition);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();

        cut.Find("[data-testid='tenants-shell-status-focus']")
            .GetAttribute("href")
            .ShouldBe("#tenants-workspace-status");

        // The in-page focus link targets the status region; the region must be programmatically
        // focusable (tabindex="-1") so the fragment link moves keyboard focus, not just the viewport.
        cut.Find("[data-testid='tenants-shell-status']")
            .GetAttribute("tabindex")
            .ShouldBe("-1");
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
        };

        public LocalizedString this[string name]
            => new(name, Values[name]);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values[name], arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }
}
