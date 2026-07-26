using System.Globalization;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class AuditEvidenceReceiptTests : FluentBunitContext
{
    [Fact]
    public void Receipt_component_renders_support_safe_fields_selectors_and_copy_button()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row(), supportSafeCommandReference: "command-safe-reference")));

        cut.Find("[data-testid='tenants-audit-receipt']").GetAttribute("role").ShouldBe("region");
        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldContain("event-safe-reference");
        cut.Find("[data-testid='tenants-audit-receipt-copy']").GetAttribute("data-copy-kind").ShouldBe("ApprovedReference");
        cut.Markup.ShouldContain("actor-user");
        cut.Markup.ShouldContain("target-user");
        cut.Markup.ShouldContain("tenant.alpha");
        cut.Markup.ShouldContain("UserAddedToTenant (Access)");
        cut.Markup.ShouldContain("2026-06-01 10:00:00 UTC");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Find("dl").TextContent.ShouldContain("Actor");
        cut.FindAll("dt").Count.ShouldBeGreaterThanOrEqualTo(7);
        cut.FindAll("dd").Count.ShouldBeGreaterThanOrEqualTo(7);
        cut.Find("[data-testid='tenants-audit-receipt']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find(".audit-evidence-receipt__action").NodeName.ShouldBe("FLUENT-BUTTON");
    }

    [Fact]
    public void Receipt_component_omits_copy_when_partial_receipt_has_no_safe_reference()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAuditReceipt receipt = TenantAuditReceipt.FromRow(Row(eventReference: string.Empty));
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, receipt));

        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-audit-receipt-copy-feedback']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("Success", Case.Insensitive);
    }

    [Fact]
    public void Receipt_component_recovery_actions_invoke_refresh_or_close_callbacks()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        int retryCount = 0;
        int closeCount = 0;
        IRenderedComponent<AuditEvidenceReceipt> pending = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row(), auditState: TenantCommandAuditState.AuditPending))
            .Add(component => component.OnRetry, () => retryCount++));

        pending.Find("[data-recovery-verb='refresh']").Click();

        retryCount.ShouldBe(1);

        IRenderedComponent<AuditEvidenceReceipt> ready = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row()))
            .Add(component => component.OnClose, () => closeCount++));

        ready.FindAll(".audit-evidence-receipt__action")
            .Single(button => button.TextContent.Contains("Inspect audit", StringComparison.Ordinal))
            .Click();

        closeCount.ShouldBe(1);
    }

    [Fact]
    public void Receipt_component_renders_available_correction_start_action_and_invokes_callback()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartIntent? startedIntent = null;
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row(eventType: "UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row(eventType: "UserRemovedFromTenant")))
            .Add(component => component.CorrectionIntent, intent)
            .Add(component => component.OnStartCorrection, value => startedIntent = value));

        var action = cut.Find("[data-testid='tenants-correction-start']");
        action.TextContent.ShouldContain("restore intended access");
        action.GetAttribute("aria-label").ShouldNotBeNull().ShouldContain("restore intended access");
        action.NodeName.ShouldBe("FLUENT-BUTTON");

        action.Click();

        startedIntent.ShouldBe(intent);
    }

    [Fact]
    public void Receipt_component_renders_unavailable_correction_reason_without_start_action()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row(eventType: "UserRemovedFromTenant")));

        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row(eventType: "UserRemovedFromTenant")))
            .Add(component => component.CorrectionIntent, intent));

        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.ShouldContain("Choose the intended role");
        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("undo", Case.Insensitive);
        cut.Markup.ShouldNotContain("rollback", Case.Insensitive);
        cut.Markup.ShouldNotContain("hidden edit", Case.Insensitive);
    }

    [Fact]
    public void Receipt_component_omits_correction_copy_for_uncorrectable_outcomes()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(Row()));

        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row()))
            .Add(component => component.CorrectionIntent, intent));

        intent.UnavailableReasons.ShouldContain(TenantCorrectionUnavailableReason.UnsupportedOutcome);
        cut.FindAll("[data-testid='tenants-correction-unavailable-reason']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
    }

    [Fact]
    public void Receipt_availability_continue_and_inspect_actions_return_focus_through_close_callback()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        int retryCount = 0;
        int closeCount = 0;
        IRenderedComponent<AuditEvidenceReceipt> unavailable = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row(), auditState: TenantCommandAuditState.AuditUnavailable))
            .Add(component => component.OnRetry, () => retryCount++)
            .Add(component => component.OnClose, () => closeCount++));

        unavailable.Find("[data-recovery-verb='continuereadonly']").Click();

        closeCount.ShouldBe(1);
        retryCount.ShouldBe(0);

        IRenderedComponent<AuditEvidenceReceipt> delayed = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row(), auditState: TenantCommandAuditState.AuditDelayed))
            .Add(component => component.OnRetry, () => retryCount++)
            .Add(component => component.OnClose, () => closeCount++));

        delayed.Find("[data-recovery-verb='inspectaudit']").Click();

        closeCount.ShouldBe(2);
        retryCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(TenantCommandAuditState.AuditPending, "Wait", "polite")]
    [InlineData(TenantCommandAuditState.AuditDelayed, "Inspect audit", "polite")]
    [InlineData(TenantCommandAuditState.AuditUnavailable, "Continue read-only", "assertive")]
    [InlineData(TenantCommandAuditState.MissingSupport, "Escalate", "assertive")]
    public void Receipt_component_renders_recovery_actions_without_success_copy(
        TenantCommandAuditState auditState,
        string expectedAction,
        string expectedLiveRegion)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row(), auditState: auditState)));

        cut.Markup.ShouldContain(expectedAction);
        cut.Markup.ShouldNotContain("Success", Case.Insensitive);
        cut.Find("[data-testid='tenants-audit-receipt']").GetAttribute("aria-live").ShouldBe(expectedLiveRegion);
        cut.Find("[data-testid='tenants-audit-availability']");
    }

    [Theory]
    [InlineData(TenantAuditReceiptState.Stale)]
    [InlineData(TenantAuditReceiptState.Degraded)]
    [InlineData(TenantAuditReceiptState.Unauthorized)]
    [InlineData(TenantAuditReceiptState.InvalidReference)]
    [InlineData(TenantAuditReceiptState.Partial)]
    public void Receipt_component_keeps_non_ready_states_distinct_from_success(TenantAuditReceiptState state)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAuditReceipt receipt = TenantAuditReceipt.Unavailable("requested-reference", "tenant.alpha") with { State = state };

        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, receipt));

        cut.Find("[data-testid='tenants-audit-receipt']").GetAttribute("class").ShouldNotBeNull().ShouldContain(state.ToString().ToLowerInvariant());
        cut.Markup.ShouldNotContain("Success", Case.Insensitive);
    }

    private static TenantCorrectionStartContext Context(TenantAuditRow row, TenantRole? intendedRole = null)
        => new(
            TenantAuditReceipt.FromRow(row),
            row,
            IsAuthorized: true,
            HasCurrentProjectionSnapshot: true,
            CurrentProjectionSnapshotReference: "tenant.alpha@current",
            IntendedRole: intendedRole);

    private static TenantAuditRow Row(string eventReference = "event-safe-reference", string eventType = "UserAddedToTenant")
        => new(
            eventReference,
            eventType,
            AuditEventCategory.Access,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "tenant.alpha",
            "target-user",
            "tenant.alpha",
            eventType,
            "userId: target-user",
            ReadModelFreshnessState.Current);

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        public LocalizedString this[string name] => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static value => new LocalizedString(value.Key, value.Value));

        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Audit.Freshness.Current"] = "Current",
            ["Tenants.Audit.Availability.Accessible.Delayed"] = "Audit evidence is delayed; retry status lookup or inspect audit before citing proof.",
            ["Tenants.Audit.Availability.Accessible.MissingSupport"] = "Audit evidence support is missing; continue read-only or escalate with support-safe information.",
            ["Tenants.Audit.Availability.Accessible.Pending"] = "Audit evidence is pending; wait, refresh status, or inspect audit before citing proof.",
            ["Tenants.Audit.Availability.Accessible.Unavailable"] = "Audit evidence is unavailable; continue read-only, retry status lookup, or escalate with support-safe information.",
            ["Tenants.Audit.Availability.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Availability.Action.Escalate"] = "Escalate",
            ["Tenants.Audit.Availability.Action.InspectAudit"] = "Inspect audit",
            ["Tenants.Audit.Availability.Action.Refresh"] = "Retry status lookup",
            ["Tenants.Audit.Availability.Action.Wait"] = "Wait",
            ["Tenants.Audit.Availability.ActionsLabel"] = "Audit availability recovery actions",
            ["Tenants.Audit.Availability.Reason.MissingSupport"] = "This flow cannot verify audit proof from the available implementation support. Continue read-only or escalate using only the visible support-safe reference.",
            ["Tenants.Audit.Availability.Reason.Unavailable"] = "Audit proof cannot be verified right now. Continue read-only, retry status lookup, or escalate without including raw diagnostics, tokens, payloads, or personal data.",
            ["Tenants.Audit.Availability.State.Delayed"] = "Audit delayed",
            ["Tenants.Audit.Availability.State.MissingSupport"] = "Missing implementation support",
            ["Tenants.Audit.Availability.State.Pending"] = "Audit pending",
            ["Tenants.Audit.Availability.State.Unavailable"] = "Audit unavailable",
            ["Tenants.Audit.Receipt.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Receipt.Action.Escalate"] = "Escalate with reference",
            ["Tenants.Audit.Receipt.Action.InspectAudit"] = "Inspect audit",
            ["Tenants.Audit.Receipt.Action.Refresh"] = "Refresh",
            ["Tenants.Audit.Receipt.Action.Retry"] = "Retry",
            ["Tenants.Audit.Receipt.Action.Wait"] = "Wait for audit evidence",
            ["Tenants.Audit.Receipt.Copy"] = "Copy audit receipt reference",
            ["Tenants.Audit.Receipt.Field.Actor"] = "Actor",
            ["Tenants.Audit.Receipt.Field.CommandReference"] = "Command reference",
            ["Tenants.Audit.Receipt.Field.Outcome"] = "Outcome",
            ["Tenants.Audit.Receipt.Field.ProjectionMarker"] = "Projection marker",
            ["Tenants.Audit.Receipt.Field.Reference"] = "Audit reference",
            ["Tenants.Audit.Receipt.Field.Scope"] = "Tenant scope",
            ["Tenants.Audit.Receipt.Field.Target"] = "Target",
            ["Tenants.Audit.Receipt.Field.Timestamp"] = "Timestamp",
            ["Tenants.Audit.Receipt.State.Degraded"] = "Audit evidence is degraded. Use the reference only with this limitation.",
            ["Tenants.Audit.Receipt.State.Delayed"] = "Audit evidence is delayed. Inspect audit or retry before citing proof.",
            ["Tenants.Audit.Receipt.State.InvalidReference"] = "The requested receipt reference is not loaded in the current tenant-scoped audit result.",
            ["Tenants.Audit.Receipt.State.MissingSupport"] = "Audit evidence support is missing. Escalate with the support-safe reference.",
            ["Tenants.Audit.Receipt.State.Partial"] = "Audit evidence is partial. The receipt cannot cite a complete proof.",
            ["Tenants.Audit.Receipt.State.Pending"] = "Audit evidence is pending. Wait or refresh before citing proof.",
            ["Tenants.Audit.Receipt.State.Ready"] = "Audit evidence is ready to cite.",
            ["Tenants.Audit.Receipt.State.Stale"] = "Audit evidence is stale. Refresh before treating it as current.",
            ["Tenants.Audit.Receipt.State.Unauthorized"] = "Audit evidence is not available for the current authorization scope.",
            ["Tenants.Audit.Receipt.State.Unavailable"] = "Audit evidence is unavailable. Continue read-only or retry later.",
            ["Tenants.Audit.Receipt.Title"] = "Audit evidence receipt",
            ["Tenants.Correction.Action.Start"] = "start correction",
            ["Tenants.Correction.Action.RestoreAccess"] = "restore intended access",
            ["Tenants.Correction.Action.StartAccessible"] = "start correction for audit evidence {0}",
            ["Tenants.Correction.Action.RestoreAccessAccessible"] = "restore intended access for audit evidence {0}",
            ["Tenants.Correction.Unavailable.ExplicitRoleRequired"] = "Choose the intended role before starting correction.",
            ["Tenants.Copy.Action"] = "Copy",
            ["Tenants.Copy.Feedback.Empty"] = "Nothing is available to copy.",
        };
    }
}
