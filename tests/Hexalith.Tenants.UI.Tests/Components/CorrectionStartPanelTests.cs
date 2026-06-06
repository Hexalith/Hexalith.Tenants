using System.Globalization;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class CorrectionStartPanelTests : BunitContext
{
    [Fact]
    public void Panel_renders_original_evidence_current_snapshot_command_and_preview_handoff_without_submission()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent));

        cut.Find("[data-testid='tenants-correction-panel']").GetAttribute("role").ShouldBe("region");
        cut.Find("[data-testid='tenants-correction-original-evidence']").TextContent.ShouldContain("event-safe-reference");
        cut.Find("[data-testid='tenants-correction-current-snapshot']").TextContent.ShouldContain("tenant.alpha@current");
        cut.Find("[data-testid='tenants-correction-command']").TextContent.ShouldContain("Add user to tenant");
        cut.Find("[data-testid='tenants-correction-domain']").TextContent.ShouldContain("Tenants");
        cut.Find("[data-testid='tenants-correction-preview-data']").TextContent.ShouldContain("Tenant reader");
        cut.Find("[data-testid='tenants-correction-preview-handoff']").GetAttribute("type").ShouldBe("button");
        cut.Markup.ShouldNotContain("POST /api/v1/commands", Case.Insensitive);
        cut.Markup.ShouldNotContain("Success", Case.Insensitive);
    }

    [Fact]
    public void Panel_renders_blocked_global_admin_reason_without_preview_handoff()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(Row("GlobalAdministratorRemoved")));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent));

        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.ShouldContain("Global administrator correction commands are not connected");
        cut.Find("[data-testid='tenants-correction-original-evidence']").TextContent.ShouldContain("event-safe-reference");
        cut.FindAll("[data-testid='tenants-correction-preview-handoff']").ShouldBeEmpty();
    }

    [Fact]
    public void Panel_close_uses_callback_for_focus_return()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        bool closed = false;

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, TenantCorrectionStartIntent.Evaluate(Context(Row("UserRemovedFromTenant"), TenantRole.TenantReader)))
            .Add(component => component.OnClose, () => closed = true));

        cut.Find("[data-testid='tenants-correction-close']").Click();

        closed.ShouldBeTrue();
    }

    private static TenantCorrectionStartContext Context(TenantAuditRow row, TenantRole? intendedRole = null)
        => new(
            TenantAuditReceipt.FromRow(row),
            row,
            IsAuthorized: true,
            HasCurrentProjectionSnapshot: true,
            CurrentProjectionSnapshotReference: "tenant.alpha@current",
            IntendedRole: intendedRole);

    private static TenantAuditRow Row(string eventType)
        => new(
            "event-safe-reference",
            eventType,
            eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal) ? AuditEventCategory.Administrative : AuditEventCategory.Access,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "tenant.alpha",
            eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal) ? "admin-user" : "target-user",
            eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal) ? "global-administrators" : "tenant.alpha",
            eventType,
            eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal) ? "userId: admin-user" : "userId: target-user",
            TenantFreshnessState.Current);

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        public LocalizedString this[string name] => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static value => new LocalizedString(value.Key, value.Value));

        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Correction.Action.PreviewHandoff"] = "Continue to correction preview",
            ["Tenants.Correction.Close"] = "Close correction start",
            ["Tenants.Correction.Command.AddUserToTenant"] = "Add user to tenant",
            ["Tenants.Correction.Command.SetGlobalAdministrator"] = "Set global administrator",
            ["Tenants.Correction.Domain.GlobalAdministrators"] = "Global administrators",
            ["Tenants.Correction.Domain.Tenants"] = "Tenants",
            ["Tenants.Correction.Field.Command"] = "Intended command",
            ["Tenants.Correction.Field.CurrentSnapshot"] = "Current projection snapshot",
            ["Tenants.Correction.Field.Domain"] = "Command domain",
            ["Tenants.Correction.Field.OriginalEvidence"] = "Original evidence",
            ["Tenants.Correction.Field.PreviewData"] = "Required preview data",
            ["Tenants.Correction.PreviewInput.currentProjectionSnapshot"] = "Current projection snapshot",
            ["Tenants.Correction.PreviewInput.currentRole"] = "Current role",
            ["Tenants.Correction.PreviewInput.domain"] = "Domain",
            ["Tenants.Correction.PreviewInput.aggregateId"] = "Aggregate",
            ["Tenants.Correction.PreviewInput.intendedRole"] = "Intended role",
            ["Tenants.Correction.PreviewInput.originalAuditReference"] = "Original audit reference",
            ["Tenants.Correction.PreviewInput.tenantId"] = "Tenant",
            ["Tenants.Correction.PreviewInput.userId"] = "User",
            ["Tenants.Correction.Role.TenantReader"] = "Tenant reader",
            ["Tenants.Correction.Title"] = "Start correction",
            ["Tenants.Correction.Unavailable.GlobalAdministratorCommandSupportUnavailable"] = "Global administrator correction commands are not connected.",
        };
    }
}
