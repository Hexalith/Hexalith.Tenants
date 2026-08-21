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

public sealed class AuditAvailabilityStateTests : FluentBunitContext
{
    [Theory]
    [InlineData(TenantCommandAuditState.AuditPending, "Audit pending", "Audit evidence is pending", "polite")]
    [InlineData(TenantCommandAuditState.AuditDelayed, "Audit delayed", "Audit evidence is delayed", "polite")]
    [InlineData(TenantCommandAuditState.AuditUnavailable, "Audit unavailable", "Audit evidence is unavailable", "assertive")]
    [InlineData(TenantCommandAuditState.MissingSupport, "Missing implementation support", "Audit evidence support is missing", "assertive")]
    public void Availability_control_renders_state_icon_selector_and_live_region(
        TenantCommandAuditState auditState,
        string expectedStateLabel,
        string expectedAccessibleDescription,
        string expectedLiveRegion)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, auditState));

        cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("aria-live").ShouldBe(expectedLiveRegion);
        cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("aria-label").ShouldNotBeNull()
            .ShouldContain(expectedAccessibleDescription);
        cut.Find("[data-testid='tenants-audit-availability-state']").TextContent.ShouldContain(expectedStateLabel);
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

        // Wait has no handler, so rendering it as a button gave the operator a live control that did nothing.
        // It must not render at all; waiting is conveyed by the state copy.
        IRenderedComponent<AuditAvailabilityState> pending = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditPending)
            .Add(component => component.OnRefresh, () => refreshCount++));

        pending.FindAll("[data-recovery-verb='wait']").ShouldBeEmpty();
        pending.Find("[data-recovery-verb='refresh']").Click();
        refreshCount.ShouldBe(2);
    }

    [Fact]
    public void Availability_recovery_actions_are_native_keyboard_operable_controls()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(component => component.OnRefresh, () => { })
            .Add(component => component.OnContinueReadOnly, () => { })
            .Add(component => component.OnEscalate, () => { }));

        cut.Find(".tenants-audit-availability__actions").GetAttribute("aria-label")
            .ShouldBe("Audit availability recovery actions");
        foreach (string verb in new[] { "continuereadonly", "refresh", "escalate" })
        {
            AngleSharp.Dom.IElement action = cut.Find($"[data-recovery-verb='{verb}']");

            action.NodeName.ShouldBe("FLUENT-BUTTON");
            action.TextContent.ShouldNotBeNullOrWhiteSpace();
            action.HasAttribute("disabled").ShouldBeFalse();
            action.HasAttribute("tabindex").ShouldBeFalse();
        }
    }

    /// <summary>
    /// The success state shipped with no glyph coverage: every other state is pinned by the theory above, so
    /// deleting the Available arm would fall through to string.Empty and render a blank icon beside "Audit
    /// available" with the whole suite still green.
    /// </summary>
    [Fact]
    public void Availability_control_renders_a_glyph_for_the_available_state()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditAvailable));

        cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("data-state").ShouldBe("available");
        cut.Find(".tenants-audit-availability__icon").TextContent.ShouldNotBeEmpty();

        // Non-localizable English must not leak through the glyph, which never passes through the localizer.
        cut.Find(".tenants-audit-availability__icon").TextContent.ShouldNotContain("OK", Case.Insensitive);
    }

    /// <summary>
    /// Recovery copy must name only controls that actually render. Nothing asserted the difference between
    /// the copy variants, so the reason paragraph and accessible label could promise an escalate or
    /// continue-read-only action with no such button present and the suite would not notice.
    /// </summary>
    [Fact]
    public void Availability_copy_names_only_the_recovery_actions_that_render()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        // Escalate is offered by the state but has no delegate: the copy must drop it.
        IRenderedComponent<AuditAvailabilityState> noEscalation = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(component => component.OnRefresh, () => { })
            .Add(component => component.OnContinueReadOnly, () => { }));

        noEscalation.FindAll("[data-recovery-verb='escalate']").ShouldBeEmpty();
        noEscalation.Find(".tenants-audit-availability__reason").TextContent.ShouldNotContain("escalate", Case.Insensitive);
        noEscalation.Find("[data-testid='tenants-audit-availability']").GetAttribute("aria-label").ShouldNotBeNull()
            .ShouldNotContain("escalate", Case.Insensitive);

        // With the delegate bound, the escalate verb and its copy both come back.
        IRenderedComponent<AuditAvailabilityState> withEscalation = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(component => component.OnRefresh, () => { })
            .Add(component => component.OnContinueReadOnly, () => { })
            .Add(component => component.OnEscalate, () => { }));

        withEscalation.Find("[data-recovery-verb='escalate']");
        withEscalation.Find(".tenants-audit-availability__reason").TextContent.ShouldContain("escalate", Case.Insensitive);
    }

    /// <summary>
    /// MissingSupport offers only ContinueReadOnly and Escalate. A surface binding neither renders no
    /// recovery button at all, so the copy must not tell the operator to continue read-only, and the empty
    /// labelled actions region must not render.
    /// </summary>
    [Fact]
    public void Availability_state_without_any_renderable_recovery_promises_none()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.MissingSupport));

        cut.FindAll("[data-recovery-verb]").ShouldBeEmpty();
        cut.FindAll(".tenants-audit-availability__actions").ShouldBeEmpty();
        cut.Find(".tenants-audit-availability__reason").TextContent
            .ShouldNotContain("Continue read-only", Case.Insensitive);
        cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("aria-label").ShouldNotBeNull()
            .ShouldNotContain("continue read-only", Case.Insensitive);
    }

    [Fact]
    public void Availability_control_hides_recovery_verbs_without_real_delegates()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<AuditAvailabilityState> cut = Render<AuditAvailabilityState>(parameters => parameters
            .Add(component => component.AuditState, TenantCommandAuditState.AuditUnavailable)
            .Add(component => component.OnContinueReadOnly, () => { }));

        cut.Find("[data-recovery-verb='continuereadonly']");
        cut.FindAll("[data-recovery-verb='refresh']").ShouldBeEmpty();
        cut.FindAll("[data-recovery-verb='escalate']").ShouldBeEmpty();
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
            ["Tenants.Audit.Availability.Accessible.Available"] = "Audit evidence is available; support-safe proof may be inspected or copied.",
            ["Tenants.Audit.Availability.Accessible.Delayed"] = "Audit evidence is delayed; retry status lookup or inspect audit before citing proof.",
            ["Tenants.Audit.Availability.Accessible.MissingSupport"] = "Audit evidence support is missing; continue read-only or escalate with support-safe information.",
            ["Tenants.Audit.Availability.Accessible.MissingSupport.NoEscalation"] = "Audit evidence support is missing; continue read-only.",
            ["Tenants.Audit.Availability.Accessible.Pending"] = "Audit evidence is pending; wait, refresh status, or inspect audit before citing proof.",
            ["Tenants.Audit.Availability.Accessible.Unavailable"] = "Audit evidence is unavailable; continue read-only, retry status lookup, or escalate with support-safe information.",
            ["Tenants.Audit.Availability.Accessible.Unavailable.NoEscalation"] = "Audit evidence is unavailable; continue read-only or retry status lookup.",
            ["Tenants.Audit.Availability.Accessible.MissingSupport.NoRecovery"] = "Audit evidence support is missing; no recovery action is available on this surface.",
            ["Tenants.Audit.Availability.Accessible.MissingSupport.RefreshOnly"] = "Audit evidence support is missing; retry status lookup.",
            ["Tenants.Audit.Availability.Accessible.Unavailable.NoRecovery"] = "Audit evidence is unavailable; no recovery action is available on this surface.",
            ["Tenants.Audit.Availability.Accessible.Unavailable.RefreshOnly"] = "Audit evidence is unavailable; retry status lookup.",
            ["Tenants.Audit.Availability.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Availability.Action.Escalate"] = "Escalate",
            ["Tenants.Audit.Availability.Action.InspectAudit"] = "Inspect audit",
            ["Tenants.Audit.Availability.Action.Refresh"] = "Retry status lookup",
            ["Tenants.Audit.Availability.Action.Wait"] = "Wait",
            ["Tenants.Audit.Availability.ActionsLabel"] = "Audit availability recovery actions",
            ["Tenants.Audit.Availability.Reason.MissingSupport.NoRecovery"] = "This flow cannot verify audit proof from the available implementation support, and no recovery action is available on this surface. The recorded outcome above is unchanged.",
            ["Tenants.Audit.Availability.Reason.MissingSupport.RefreshOnly"] = "This flow cannot verify audit proof from the available implementation support. Retry the status lookup.",
            ["Tenants.Audit.Availability.Reason.Unavailable.NoRecovery"] = "Audit proof cannot be verified right now, and no recovery action is available on this surface. The recorded outcome above is unchanged.",
            ["Tenants.Audit.Availability.Reason.Unavailable.RefreshOnly"] = "Audit proof cannot be verified right now. Retry the status lookup without including raw diagnostics, tokens, payloads, or personal data.",
            ["Tenants.Audit.Availability.Reason.MissingSupport"] = "This flow cannot verify audit proof from the available implementation support. Continue read-only or escalate using only the visible support-safe reference.",
            ["Tenants.Audit.Availability.Reason.MissingSupport.NoEscalation"] = "This flow cannot verify audit proof from the available implementation support. Continue read-only.",
            ["Tenants.Audit.Availability.Reason.Unavailable"] = "Audit proof cannot be verified right now. Continue read-only, retry status lookup, or escalate without including raw diagnostics, tokens, payloads, or personal data.",
            ["Tenants.Audit.Availability.Reason.Unavailable.NoEscalation"] = "Audit proof cannot be verified right now. Continue read-only or retry status lookup.",
            ["Tenants.Audit.Availability.State.Available"] = "Audit available",
            ["Tenants.Audit.Availability.State.Delayed"] = "Audit delayed",
            ["Tenants.Audit.Availability.State.MissingSupport"] = "Missing implementation support",
            ["Tenants.Audit.Availability.State.Pending"] = "Audit pending",
            ["Tenants.Audit.Availability.State.Unavailable"] = "Audit unavailable",
        };
    }
}
