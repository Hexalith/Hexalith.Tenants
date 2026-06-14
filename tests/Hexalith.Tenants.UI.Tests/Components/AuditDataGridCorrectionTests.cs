using System.Globalization;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TruthState;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class AuditDataGridCorrectionTests : BunitContext
{
    [Fact]
    public void Audit_grid_renders_row_level_correction_start_when_intent_is_available()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        TenantCorrectionStartIntent? startedIntent = null;
        TenantAuditRow row = Row("UserRemovedFromTenant");

        IRenderedComponent<AuditDataGrid> cut = Render<AuditDataGrid>(parameters => parameters
            .Add(component => component.Rows, [row])
            .Add(component => component.CorrectionIntentProvider, value => TenantCorrectionStartIntent.Evaluate(Context(value, TenantRole.TenantReader)))
            .Add(component => component.OnStartCorrection, value => startedIntent = value));

        var action = cut.Find("[data-testid='tenants-correction-start']");
        action.TextContent.ShouldContain("restore intended access");
        action.GetAttribute("aria-label").ShouldNotBeNull().ShouldContain("restore intended access");

        action.Click();

        startedIntent.ShouldNotBeNull();
        startedIntent.IntendedCommandType.ShouldBe(TenantCorrectionCommandType.AddUserToTenant);
    }

    [Fact]
    public void Audit_grid_renders_safe_unavailable_reason_for_correctable_blocked_row()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        TenantAuditRow row = Row("UserRemovedFromTenant");

        IRenderedComponent<AuditDataGrid> cut = Render<AuditDataGrid>(parameters => parameters
            .Add(component => component.Rows, [row])
            .Add(component => component.CorrectionIntentProvider, value => TenantCorrectionStartIntent.Evaluate(Context(value))));

        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.ShouldContain("Choose the intended role");
        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
    }

    [Fact]
    public void Audit_grid_does_not_render_correction_copy_for_unsupported_rows()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        TenantAuditRow row = Row("TenantConfigurationSet");

        IRenderedComponent<AuditDataGrid> cut = Render<AuditDataGrid>(parameters => parameters
            .Add(component => component.Rows, [row])
            .Add(component => component.CorrectionIntentProvider, value => TenantCorrectionStartIntent.Evaluate(Context(value))));

        cut.FindAll("[data-testid='tenants-correction-unavailable-reason']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
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
            AuditEventCategory.Access,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "tenant.alpha",
            "target-user",
            "tenant.alpha",
            eventType,
            eventType is "TenantConfigurationSet" ? "key: billing.mode" : "userId: target-user",
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
            ["Tenants.Audit.Category.Access"] = "Access",
            ["Tenants.Audit.Column.Actor"] = "Actor",
            ["Tenants.Audit.Column.Category"] = "Category",
            ["Tenants.Audit.Column.Correction"] = "Correction",
            ["Tenants.Audit.Column.Freshness"] = "Freshness",
            ["Tenants.Audit.Column.Outcome"] = "Outcome",
            ["Tenants.Audit.Column.Receipt"] = "Receipt",
            ["Tenants.Audit.Column.Reference"] = "Reference context",
            ["Tenants.Audit.Column.Scope"] = "Tenant scope",
            ["Tenants.Audit.Column.Target"] = "Target",
            ["Tenants.Audit.Column.Timestamp"] = "Timestamp",
            ["Tenants.Audit.Copy.EventReference"] = "Copy audit event reference {0}",
            ["Tenants.Audit.Freshness.Current"] = "Current",
            ["Tenants.Audit.Receipt.Open"] = "View receipt",
            ["Tenants.Correction.Action.RestoreAccess"] = "restore intended access",
            ["Tenants.Correction.Action.RestoreAccessAccessible"] = "restore intended access for audit evidence {0}",
            ["Tenants.Correction.Action.Start"] = "start correction",
            ["Tenants.Correction.Action.StartAccessible"] = "start correction for audit evidence {0}",
            ["Tenants.Correction.Unavailable.ExplicitRoleRequired"] = "Choose the intended role before starting correction.",
            ["Tenants.Copy.Action"] = "Copy",
        };
    }
}
