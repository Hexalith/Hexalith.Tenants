using System.Globalization;

using Bunit;

using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.State.TenantCommands;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class AuditAvailabilityStateTests : BunitContext
{
    [Theory]
    [InlineData(TenantCommandAuditState.AuditPending, "Audit pending", "polite")]
    [InlineData(TenantCommandAuditState.AuditDelayed, "Audit delayed", "polite")]
    [InlineData(TenantCommandAuditState.AuditUnavailable, "Audit unavailable", "assertive")]
    [InlineData(TenantCommandAuditState.MissingSupport, "Missing implementation support", "assertive")]
    public void Availability_control_renders_state_icon_selector_and_live_region(
        TenantCommandAuditState auditState,
        string expectedText,
        string expectedLiveRegion)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, auditState));

        cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("aria-live").ShouldBe(expectedLiveRegion);
        cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("aria-label").ShouldNotBeNull().ShouldContain(expectedText);
        cut.Find("[data-testid='tenants-audit-availability-state']").TextContent.ShouldContain(expectedText);
        cut.Find(".tenants-audit-availability__icon").TextContent.ShouldNotBeEmpty();
        cut.Markup.ShouldNotContain("Success", Case.Insensitive);
        cut.Markup.ShouldNotContain("AuditPending", Case.Insensitive);
        cut.Markup.ShouldNotContain("audit_pending", Case.Insensitive);
    }

    [Fact]
    public void Availability_control_invokes_recovery_callbacks_and_keeps_wait_passive()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        int refreshCount = 0;
        int continueCount = 0;
        int escalateCount = 0;

        IRenderedComponent<AuditAvailabilityState> unavailable = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(component => component.OnRefresh, () => refreshCount++)
            .Add(component => component.OnContinueReadOnly, () => continueCount++)
            .Add(component => component.OnEscalate, () => escalateCount++));

        unavailable.Find("[data-recovery-verb='refresh']").Click();
        unavailable.Find("[data-recovery-verb='continuereadonly']").Click();
        unavailable.Find("[data-recovery-verb='escalate']").Click();

        refreshCount.ShouldBe(1);
        continueCount.ShouldBe(1);
        escalateCount.ShouldBe(1);

        IRenderedComponent<AuditAvailabilityState> pending = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditPending)
            .Add(component => component.OnRefresh, () => refreshCount++));

        pending.Find("[data-recovery-verb='wait']").Click();
        refreshCount.ShouldBe(1);
    }

    [Fact]
    public void Availability_recovery_actions_are_native_keyboard_operable_controls()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable));

        cut.Find(".tenants-audit-availability__actions").GetAttribute("aria-label")
            .ShouldBe("Audit availability recovery actions");
        foreach (string verb in new[] { "continuereadonly", "refresh", "escalate" })
        {
            AngleSharp.Dom.IElement action = cut.Find($"[data-recovery-verb='{verb}']");

            action.TagName.ShouldBe("BUTTON");
            action.GetAttribute("type").ShouldBe("button");
            action.TextContent.ShouldNotBeNullOrWhiteSpace();
            action.HasAttribute("disabled").ShouldBeFalse();
            action.HasAttribute("tabindex").ShouldBeFalse();
        }
    }

    [Fact]
    public void Availability_control_renders_existing_inspect_audit_action_fragment()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        RenderFragment inspectAuditAction = builder =>
        {
            builder.OpenElement(0, "a");
            builder.AddAttribute(1, "href", "/tenants/tenant.alpha/audit?source=command-result");
            builder.AddAttribute(2, "data-testid", "tenants-command-audit-entrypoint");
            builder.AddContent(3, "Inspect audit");
            builder.CloseElement();
        };

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditDelayed)
            .Add(component => component.InspectAuditAction, inspectAuditAction));

        cut.Find("[data-testid='tenants-command-audit-entrypoint']").GetAttribute("href")
            .ShouldBe("/tenants/tenant.alpha/audit?source=command-result");
    }

    [Fact]
    public void Availability_css_preserves_focus_forced_colors_reduced_motion_and_stable_dimensions()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string css = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Audit",
            "AuditAvailabilityState.razor.css"));

        css.ShouldContain("@media (forced-colors: active)");
        css.ShouldContain("@media (prefers-reduced-motion: reduce)");
        css.ShouldContain(":focus-visible");
        css.ShouldContain("min-height");
        css.ShouldContain("flex: 0 0 1.75rem");
    }

    [Fact]
    public void Availability_control_does_not_render_for_not_started()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.NotStarted));

        cut.Markup.ShouldBeEmpty();
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
            ["Tenants.Audit.Availability.Accessible.Delayed"] = "Audit delayed; retry status lookup or inspect audit.",
            ["Tenants.Audit.Availability.Accessible.MissingSupport"] = "Missing implementation support; continue read-only or escalate.",
            ["Tenants.Audit.Availability.Accessible.Pending"] = "Audit pending; wait, retry status lookup, or inspect audit.",
            ["Tenants.Audit.Availability.Accessible.Unavailable"] = "Audit unavailable; continue read-only, retry status lookup, or escalate.",
            ["Tenants.Audit.Availability.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Availability.Action.Escalate"] = "Escalate",
            ["Tenants.Audit.Availability.Action.InspectAudit"] = "Inspect audit",
            ["Tenants.Audit.Availability.Action.Refresh"] = "Retry status lookup",
            ["Tenants.Audit.Availability.Action.Wait"] = "Wait",
            ["Tenants.Audit.Availability.ActionsLabel"] = "Audit availability recovery actions",
            ["Tenants.Audit.Availability.Reason.MissingSupport"] = "Continue read-only or escalate using support-safe information.",
            ["Tenants.Audit.Availability.Reason.Unavailable"] = "Continue read-only, retry status lookup, or escalate without raw diagnostics.",
            ["Tenants.Audit.Availability.State.Delayed"] = "Audit delayed",
            ["Tenants.Audit.Availability.State.MissingSupport"] = "Missing implementation support",
            ["Tenants.Audit.Availability.State.Pending"] = "Audit pending",
            ["Tenants.Audit.Availability.State.Unavailable"] = "Audit unavailable",
        };
    }
}
