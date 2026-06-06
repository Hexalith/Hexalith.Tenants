using System.Globalization;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Tenants.Lifecycle;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TenantLifecycleActionAvailabilityTests : BunitContext
{
    [Fact]
    public void Lifecycle_availability_renders_stable_selectors_visible_reasons_and_disabled_actions()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, TenantFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Indeterminate)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Unresolved));

        cut.Find("[data-testid='tenants-lifecycle-actions']");
        cut.Find("[data-testid='tenants-lifecycle-state']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-freshness']").TextContent.ShouldContain("Current");
        cut.Find("[data-testid='tenants-lifecycle-governance-gate']").TextContent.ShouldContain("Unresolved");
        cut.Find("[data-testid='tenants-lifecycle-enable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.FindAll("[data-testid='tenants-lifecycle-unavailable-reason']").Count.ShouldBe(2);
        cut.FindAll("[data-testid='tenants-lifecycle-unavailable-reason']")
            .ShouldAllBe(static reason => reason.GetAttribute("tabindex") == "0");
        cut.Find("[data-testid='tenants-lifecycle-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Markup.ShouldNotContain("<form", Case.Insensitive);
        cut.Markup.ShouldNotContain("type=\"submit\"", Case.Insensitive);
        cut.Markup.ShouldNotContain("Success", Case.Insensitive);
        cut.Markup.ShouldNotContain("accepted", Case.Insensitive);
        cut.Markup.ShouldNotContain("confirmed", Case.Insensitive);
    }

    [Fact]
    public void Governance_blocking_keeps_lifecycle_command_unavailable_with_high_impact_reason()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, TenantFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Unresolved));

        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldContain("high-impact flow not ready");
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldContain("platform gate");
    }

    [Fact]
    public void Disabled_tenant_keeps_disabled_projection_truth_and_blocks_enable_without_optimistic_transition()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.disabled")
            .Add(component => component.CurrentStatus, TenantStatus.Disabled)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, TenantFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Unresolved));

        cut.Find("[data-testid='tenants-lifecycle-state']").TextContent.ShouldContain("Disabled");
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Disabled");
        cut.Find("[data-testid='tenants-lifecycle-enable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("#tenants-lifecycle-enable-reason").TextContent.ShouldContain("high-impact flow not ready");
        cut.Find("#tenants-lifecycle-enable-reason").TextContent.ShouldContain("platform gate");
        cut.Find("#tenants-lifecycle-disable-reason").TextContent.ShouldContain("TenantLifecycleStateAlreadySet");
        cut.Find("#tenants-lifecycle-disable-reason").TextContent.ShouldContain("current projection already shows Disabled");
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldNotContain("current projection already shows Active");
        cut.Markup.ShouldNotContain("confirmed", Case.Insensitive);
    }

    [Fact]
    public void Same_state_action_names_TenantLifecycleStateAlreadySet_as_safe_domain_outcome()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, TenantFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready));

        cut.Find("[data-testid='tenants-lifecycle-enable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldContain("TenantLifecycleStateAlreadySet");
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldContain("current projection already shows Active");
        cut.Find("[data-testid='tenants-lifecycle-live-region']").GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void Narrow_safety_context_keeps_available_direction_unavailable_with_visible_mobile_reason()
    {
        RegisterServices();

        IRenderedComponent<TenantLifecycleActionAvailability> cut = Render<TenantLifecycleActionAvailability>(parameters => parameters
            .Add(component => component.TenantId, "tenant.alpha")
            .Add(component => component.CurrentStatus, TenantStatus.Active)
            .Add(component => component.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(component => component.Freshness, TenantFreshnessState.Current)
            .Add(component => component.IsCommandSurfaceConnected, true)
            .Add(component => component.AuthorizationReflection, TenantLifecycleAuthorizationReflectionState.Authorized)
            .Add(component => component.GovernanceReadiness, TenantLifecycleGovernanceReadiness.Ready)
            .Add(component => component.IsNarrowSafetyContext, true));

        cut.Find("[data-testid='tenants-lifecycle-disable']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-lifecycle-actions']").TextContent.ShouldContain("viewport cannot preserve");
        cut.Find("[data-testid='tenants-lifecycle-current-status']").TextContent.ShouldContain("Active");
        cut.Find("[data-testid='tenants-lifecycle-freshness']").TextContent.ShouldContain("Current");
    }

    [Fact]
    public void Lifecycle_component_source_has_no_command_gateway_or_submission_path()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string component = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Lifecycle",
            "TenantLifecycleActionAvailability.razor"));
        string commandGateway = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Services",
            "Gateways",
            "ITenantCommandGateway.cs"));

        component.ShouldNotContain("ITenantCommandGateway");
        component.ShouldNotContain("EnableTenantAsync");
        component.ShouldNotContain("DisableTenantAsync");
        component.ShouldNotContain("SubmitCommand");
        commandGateway.ShouldNotContain("EnableTenantAsync");
        commandGateway.ShouldNotContain("DisableTenantAsync");
    }

    private void RegisterServices()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        public LocalizedString this[string name] => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static value => new LocalizedString(value.Key, value.Value));

        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Lifecycle.Description"] = "Read-only high-impact lifecycle availability for tenant {0}. Commands remain unavailable until authority, freshness, command support, and governance are proven.",
            ["Tenants.Lifecycle.FactsLabel"] = "Tenant lifecycle safety facts",
            ["Tenants.Lifecycle.Freshness.Current"] = "Current",
            ["Tenants.Lifecycle.Freshness.Stale"] = "Stale",
            ["Tenants.Lifecycle.Freshness.Unknown"] = "Unknown",
            ["Tenants.Lifecycle.FreshnessLabel"] = "Freshness",
            ["Tenants.Lifecycle.Governance.Ready"] = "Ready",
            ["Tenants.Lifecycle.Governance.Unresolved"] = "Unresolved",
            ["Tenants.Lifecycle.GovernanceGateLabel"] = "Governance gate",
            ["Tenants.Lifecycle.Operation.DisableTenant"] = "Disable tenant",
            ["Tenants.Lifecycle.Operation.EnableTenant"] = "Enable tenant",
            ["Tenants.Lifecycle.StateLabel"] = "Lifecycle state",
            ["Tenants.Lifecycle.CurrentStatusLabel"] = "Current status",
            ["Tenants.Lifecycle.Status.Active"] = "Active",
            ["Tenants.Lifecycle.Status.Disabled"] = "Disabled",
            ["Tenants.Lifecycle.Status.Unknown"] = "Unknown",
            ["Tenants.Lifecycle.Title"] = "Lifecycle command availability",
            ["Tenants.Lifecycle.Unavailable.AlreadyActive"] = "{1} is unavailable for tenant {0} because the current projection already shows Active. If submitted by another surface, the safe domain outcome is {2}; continue read-only or refresh.",
            ["Tenants.Lifecycle.Unavailable.AlreadyDisabled"] = "{1} is unavailable for tenant {0} because the current projection already shows Disabled. If submitted by another surface, the safe domain outcome is {2}; continue read-only or refresh.",
            ["Tenants.Lifecycle.Unavailable.CommandSurface"] = "{1} is unavailable for tenant {0} because lifecycle command support is not connected. Continue read-only or escalate command-surface readiness.",
            ["Tenants.Lifecycle.Unavailable.Governance"] = "{1} is unavailable for tenant {0} because high-impact lifecycle governance is not ready. Continue read-only until the platform gate is cleared.",
            ["Tenants.Lifecycle.Unavailable.MissingPermission"] = "{1} is unavailable for tenant {0} because server-side global-administrator authority is not proven. Request permission or continue read-only.",
            ["Tenants.Lifecycle.Unavailable.Mobile"] = "{1} is unavailable for tenant {0} because this viewport cannot preserve the full high-impact safety context. Continue read-only on this view.",
            ["Tenants.Lifecycle.Unavailable.StaleFreshness"] = "{1} is unavailable for tenant {0} because tenant freshness is stale or unknown. Refresh before considering lifecycle action availability.",
            ["Tenants.Lifecycle.Unavailable.UnknownStatus"] = "{1} is unavailable for tenant {0} because the tenant lifecycle state is unknown. Continue read-only and refresh projection evidence.",
            ["Tenants.Lifecycle.UnavailableReason.HighImpactFlowNotReady"] = "high-impact flow not ready",
            ["Tenants.Lifecycle.UnavailableReason.MissingLifecycleSupport"] = "missing lifecycle support",
            ["Tenants.Lifecycle.UnavailableReason.MissingPermission"] = "missing permission",
            ["Tenants.Lifecycle.UnavailableReason.None"] = "available",
            ["Tenants.Lifecycle.UnavailableReason.StaleData"] = "stale data",
        };
    }
}
