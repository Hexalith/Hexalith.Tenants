using System.Globalization;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

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
            .Add(component => component.HasViewportMeasurement, true)
            .Add(component => component.IsCorrectionViewportSafe, true)
            .Add(component => component.CorrectionIntentProvider, value => TenantCorrectionStartIntent.Evaluate(Context(value, TenantRole.TenantReader)))
            .Add(component => component.OnStartCorrection, value => startedIntent = value));

        var action = cut.Find("[data-testid='tenants-correction-start']");
        action.TextContent.ShouldContain("restore intended access");
        action.GetAttribute("aria-label").ShouldNotBeNull().ShouldContain("restore intended access");
        cut.Find("[data-testid='tenants-correction-role']").GetAttribute("aria-label")
            .ShouldBe("Choose intended role for audit evidence event-safe-reference");

        action.Click();

        startedIntent.ShouldNotBeNull();
        startedIntent.IntendedCommandType.ShouldBe(TenantCorrectionCommandType.AddUserToTenant);
    }

    [Fact]
    public void Audit_grid_keeps_the_approved_event_reference_when_optional_context_has_a_field_boundary()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        TenantAuditRow row = Row("UserRemovedFromTenant") with
        {
            ReferenceContext = "userId: target-user; role: TenantReader",
            Narrative = new TenantAuditNarrative(UserId: "target-user", Role: TenantRole.TenantReader),
        };
        IRenderedComponent<AuditDataGrid> cut = Render<AuditDataGrid>(parameters => parameters
            .Add(component => component.Rows, [row])
            .Add(component => component.HasViewportMeasurement, true)
            .Add(component => component.IsCorrectionViewportSafe, true)
            .Add(component => component.CorrectionIntentProvider,
                value => TenantCorrectionStartIntent.Evaluate(Context(value, TenantRole.TenantReader))));

        cut.Find("[data-testid='tenants-audit-row-reference'] > .audit-data-grid__wrap")
            .TextContent.ShouldBe("event-safe-reference");
        cut.Find("[data-testid='tenants-audit-row-context']")
            .TextContent.ShouldBe("userId: target-user; role: TenantReader");
        cut.Find("[data-testid='tenants-audit-row']").GetAttribute("data-audit-reference")
            .ShouldBe("event-safe-reference");
        cut.Find("[data-testid='tenants-audit-receipt-open']");
        cut.Find("[data-testid='tenants-correction-start']");
    }

    [Fact]
    public void Receipt_launchers_are_named_and_focusable_by_their_approved_event_reference()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        TenantAuditRow first = Row("UserAddedToTenant") with { EventReference = "event-first" };
        TenantAuditRow second = Row("UserAddedToTenant") with { EventReference = "event-second" };

        IRenderedComponent<AuditDataGrid> cut = Render<AuditDataGrid>(parameters => parameters
            .Add(component => component.Rows, [first, second]));

        var launchers = cut.FindAll("[data-testid='tenants-audit-receipt-open']");
        launchers.Select(element => element.GetAttribute("aria-label"))
            .ShouldBe([
                "View receipt for audit event event-first",
                "View receipt for audit event event-second",
            ]);
        launchers.Select(element => element.GetAttribute("data-receipt-focus-reference"))
            .ShouldBe(["event-first", "event-second"]);
        launchers.Select(element => element.TextContent.Trim()).ShouldAllBe(label => label == "View receipt");
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
            .Add(component => component.HasViewportMeasurement, true)
            .Add(component => component.IsCorrectionViewportSafe, true)
            .Add(component => component.CorrectionIntentProvider, value => TenantCorrectionStartIntent.Evaluate(Context(value))));

        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.ShouldContain("Choose the intended role");
        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
    }

    [Fact]
    public void Audit_grid_hides_correction_start_for_untrusted_route_provenance()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        TenantAuditRow row = Row("UserRemovedFromTenant") with
        {
            Provenance = QueryResponseProvenance.HandlerComputed,
        };

        IRenderedComponent<AuditDataGrid> cut = Render<AuditDataGrid>(parameters => parameters
            .Add(component => component.Rows, [row])
            .Add(component => component.HasViewportMeasurement, true)
            .Add(component => component.IsCorrectionViewportSafe, true)
            .Add(component => component.CorrectionIntentProvider, value => TenantCorrectionStartIntent.Evaluate(Context(value, TenantRole.TenantReader))));

        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-correction-unavailable-reason']")
            .TextContent.ShouldContain("Refresh current evidence");
    }

    [Fact]
    public void Audit_grid_shows_only_unsupported_reason_for_uncorrectable_rows()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        TenantAuditRow row = Row("TenantConfigurationSet");

        IRenderedComponent<AuditDataGrid> cut = Render<AuditDataGrid>(parameters => parameters
            .Add(component => component.Rows, [row])
            .Add(component => component.CorrectionIntentProvider, value => TenantCorrectionStartIntent.Evaluate(Context(value))));

        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent
            .ShouldBe("This audit outcome is not supported for correction start.");
        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-mobile-read-only']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-viewport-pending']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false, "tenants-correction-viewport-pending")]
    [InlineData(true, "tenants-correction-mobile-read-only")]
    public void Audit_grid_distinguishes_unmeasured_and_phone_suppression_for_supported_rows(
        bool hasMeasurement,
        string expectedSelector)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        TenantAuditRow row = Row("UserRemovedFromTenant");

        IRenderedComponent<AuditDataGrid> cut = Render<AuditDataGrid>(parameters => parameters
            .Add(component => component.Rows, [row])
            .Add(component => component.HasViewportMeasurement, hasMeasurement)
            .Add(component => component.IsCorrectionViewportSafe, false)
            .Add(component => component.CorrectionIntentProvider, value => TenantCorrectionStartIntent.Evaluate(Context(value, TenantRole.TenantReader))));

        cut.Find($"[data-testid='{expectedSelector}']").TextContent.ShouldNotBeNullOrWhiteSpace();
        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-role']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-audit-row-timestamp']").TextContent.ShouldContain("UTC");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GlobalAdministratorRowsRespectTheMeasuredViewportGate(bool hasMeasurement)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        TenantAuditRow row = Row("GlobalAdministratorRemoved") with
        {
            TenantId = "system",
            Scope = "global-administrators",
        };
        IRenderedComponent<AuditDataGrid> cut = Render<AuditDataGrid>(parameters => parameters
            .Add(component => component.Rows, [row])
            .Add(component => component.HasViewportMeasurement, hasMeasurement)
            .Add(component => component.IsCorrectionViewportSafe, false)
            .Add(component => component.CorrectionIntentProvider, value => TenantCorrectionStartIntent.Evaluate(Context(value))));
        cut.Find(hasMeasurement
            ? "[data-testid='tenants-correction-mobile-read-only']"
            : "[data-testid='tenants-correction-viewport-pending']").TextContent.ShouldNotBeNullOrWhiteSpace();
        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
    }

    private static TenantCorrectionStartContext Context(TenantAuditRow row, TenantRole? intendedRole = null)
        => new(
            TenantAuditReceipt.FromRow(row),
            row,
            IsAuthorized: true,
            HasCurrentProjectionSnapshot: true,
            CurrentProjectionSnapshotReference: "tenant.alpha@current",
            IntendedRole: intendedRole,
            Projection: new TenantCorrectionProjection(row.TenantId, row.Narrative?.UserId ?? string.Empty,
                TenantStatus.Active, null, false, true, false, true, ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current, QueryResponseProvenance.ProjectionBacked));

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
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            QueryResponseProvenance.ProjectionBacked,
            new TenantAuditNarrative(UserId: "target-user"));

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
            ["Tenants.Audit.Mobile.ReadOnly"] = "This supported correction is read-only on a phone. Use a measured tablet or desktop viewport to continue.",
            ["Tenants.Audit.Receipt.Open"] = "View receipt",
            ["Tenants.Audit.Receipt.OpenAccessible"] = "View receipt for audit event {0}",
            ["Tenants.Audit.Viewport.Pending"] = "Correction controls remain read-only until the browser viewport is measured.",
            ["Tenants.Correction.Action.RestoreAccess"] = "restore intended access",
            ["Tenants.Correction.Action.RestoreAccessAccessible"] = "restore intended access for audit evidence {0}",
            ["Tenants.Correction.Action.Start"] = "start correction",
            ["Tenants.Correction.Action.StartAccessible"] = "start correction for audit evidence {0}",
            ["Tenants.Correction.RoleChoice.ForEvidence"] = "Choose intended role for audit evidence {0}",
            ["Tenants.Correction.Unavailable.ExplicitRoleRequired"] = "Choose the intended role before starting correction.",
            ["Tenants.Correction.Unavailable.FreshnessIndeterminate"] = "Refresh current evidence before starting correction.",
            ["Tenants.Correction.Unavailable.UnsupportedOutcome"] = "This audit outcome is not supported for correction start.",
            ["Tenants.Correction.Start.GlobalNotReady"] = "The high-impact global administrator correction flow is not ready here. Continue read-only or use the supported global administrator path.",
            ["Tenants.Copy.Action"] = "Copy",
        };
    }
}
