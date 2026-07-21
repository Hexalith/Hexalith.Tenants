using System.Globalization;
using System.Xml.Linq;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TenantAuditPageTests : BunitContext
{
    [Fact]
    public void Tenant_audit_page_renders_grid_filters_paging_and_support_safe_rows()
    {
        TenantAuditSnapshot snapshot = ReadySnapshot(
            [
                Row("event-safe-reference", AuditEventCategory.Access, "userId: target-user; role: TenantReader"),
            ],
            nextCursor: "next-cursor",
            hasMore: true);
        StubTenantQueryGateway gateway = RegisterServices(snapshot);
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler writeHandler = module.SetupVoid("writeText", "event-safe-reference").SetVoidResult();

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        gateway.Requests.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        cut.Find("[data-testid='tenants-audit-filter-category']").GetAttribute("value").ShouldBeNull();
        cut.Find("[data-testid='tenants-audit-filter-from']").GetAttribute("type").ShouldBe("datetime-local");
        cut.Find("[data-testid='tenants-audit-filter-to']").GetAttribute("type").ShouldBe("datetime-local");
        cut.Find("[data-testid='tenants-audit-refresh']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-audit-next']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-audit-previous']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-audit-row']").GetAttribute("data-audit-reference").ShouldBe("event-safe-reference");
        cut.Find("[data-testid='tenants-audit-copy-reference']").GetAttribute("data-copy-kind").ShouldBe("ApprovedReference");
        cut.Find("[data-testid='tenants-audit-receipt-open']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Markup.ShouldContain("target-user");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("EventStore metadata", Case.Insensitive);

        cut.Find("[data-surface-testid='tenants-audit-copy-reference']").Click();
        cut.WaitForAssertion(() => writeHandler.Invocations.Count.ShouldBe(1));
        writeHandler.Invocations.Single().Arguments[0].ShouldBe("event-safe-reference");
    }

    [Fact]
    public void Tenant_audit_page_omits_grid_copy_for_an_unsafe_raw_event_reference()
    {
        RegisterServices(ReadySnapshot([Row("Bearer raw-token", AuditEventCategory.Access)]));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.FindAll("[data-testid='tenants-audit-copy-reference']").ShouldBeEmpty();
        cut.FindAll("[data-surface-testid='tenants-audit-copy-reference']").ShouldBeEmpty();
    }

    [Fact]
    public void Tenant_audit_page_opens_receipt_from_loaded_row_without_extra_backend_query()
    {
        StubTenantQueryGateway gateway = RegisterServices(ReadySnapshot([Row("event-safe-reference", AuditEventCategory.Access)]));
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants/tenant.alpha/audit?supportSafeCommandReference=command-safe-reference");
        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-audit-receipt-open']").Click();

        cut.WaitForElement("[data-testid='tenants-audit-receipt']");
        gateway.Requests.Count.ShouldBe(1);
        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldContain("event-safe-reference");
        cut.Find("[data-testid='tenants-audit-receipt-copy']").GetAttribute("data-copy-kind").ShouldBe("ApprovedReference");
        cut.Markup.ShouldContain("command-safe-reference");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("EventStore metadata", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_receipt_reference_query_fails_closed_when_row_is_not_loaded()
    {
        StubTenantQueryGateway gateway = RegisterServices(ReadySnapshot([Row("event-loaded", AuditEventCategory.Access)]));
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants/tenant.alpha/audit?receiptReference=event-not-loaded");
        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-receipt']");

        gateway.Requests.Count.ShouldBe(1);
        cut.Find("[data-testid='tenants-audit-receipt-state']").TextContent.ShouldContain("not loaded");
        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldContain("event-not-loaded");
        // Visible text only — avoids the Fluent success-color token false positive (see VisibleText).
        cut.VisibleText().ShouldNotContain("Success", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_receipt_reference_query_opens_loaded_row_without_extra_backend_query()
    {
        StubTenantQueryGateway gateway = RegisterServices(ReadySnapshot([Row("event-safe-reference", AuditEventCategory.Access)]));
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("/tenants/tenant.alpha/audit?receiptReference=event-safe-reference&supportSafeCommandReference=command-safe-reference");

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));

        cut.WaitForElement("[data-testid='tenants-audit-receipt']");
        gateway.Requests.Count.ShouldBe(1);
        cut.Find("[data-testid='tenants-audit-receipt-state']").TextContent.ShouldContain("ready");
        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldContain("event-safe-reference");
        cut.Find("[data-testid='tenants-audit-receipt-copy']").GetAttribute("data-copy-kind").ShouldBe("ApprovedReference");
        cut.Markup.ShouldContain("command-safe-reference");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_fails_closed_for_membership_correction_when_intended_role_is_missing()
    {
        StubTenantQueryGateway gateway = RegisterServices(ReadySnapshot(
            [
                Row(
                    "event-removed-member",
                    AuditEventCategory.Access,
                    referenceContext: "userId: target-user",
                    eventType: "UserRemovedFromTenant"),
            ]));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.ShouldContain("Choose the intended role");
        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-panel']").ShouldBeEmpty();
        gateway.Requests.Count.ShouldBe(1);
        cut.Markup.ShouldNotContain("POST /api/v1/commands", Case.Insensitive);
        // Visible text only — "undo" also appears inside the Fluent token --colorNeutralForegroundOnBrand.
        cut.VisibleText().ShouldNotContain("undo", Case.Insensitive);
        cut.Markup.ShouldNotContain("rollback", Case.Insensitive);
        cut.Markup.ShouldNotContain("hidden edit", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_receipt_flow_keeps_original_evidence_visible_when_correction_is_blocked()
    {
        StubTenantQueryGateway gateway = RegisterServices(ReadySnapshot(
            [
                Row(
                    "event-removed-member",
                    AuditEventCategory.Access,
                    referenceContext: "userId: target-user",
                    eventType: "UserRemovedFromTenant"),
            ]));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-audit-receipt-open']").Click();

        cut.WaitForElement("[data-testid='tenants-audit-receipt']");
        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldContain("event-removed-member");
        cut.FindAll("[data-testid='tenants-correction-unavailable-reason']")
            .Any(reason => reason.TextContent.Contains("Choose the intended role", StringComparison.Ordinal))
            .ShouldBeTrue();
        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-panel']").ShouldBeEmpty();
        gateway.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public void Tenant_audit_page_opens_fixed_global_administrator_correction_panel_for_authorized_evidence()
    {
        StubTenantQueryGateway gateway = RegisterGlobalAdminServices(
            authorized: true,
            GlobalAdmins("other-admin"),
            GlobalAdminAuditSnapshot("GlobalAdministratorRemoved", "admin-user"));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "system"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.WaitForElement("[data-testid='tenants-correction-start']").Click();

        cut.WaitForElement("[data-testid='tenants-correction-panel']");
        cut.Find("[data-testid='tenants-correction-domain']").TextContent.ShouldContain("Global administrators");
        // The fixed global-administrator correction never offers a tenant role selector (AC1/AC2).
        cut.FindAll("[data-testid='tenants-correction-role']").ShouldBeEmpty();
        gateway.GlobalAdminRequests.Count.ShouldBeGreaterThanOrEqualTo(1);
        cut.VisibleText().ShouldNotContain("tenant role", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_keeps_global_administrator_correction_fail_closed_when_unauthorized()
    {
        RegisterGlobalAdminServices(
            authorized: false,
            GlobalAdmins("other-admin"),
            GlobalAdminAuditSnapshot("GlobalAdministratorRemoved", "admin-user"));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "system"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-panel']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-unavailable-reason']").ShouldNotBeEmpty();
        cut.VisibleText().ShouldNotContain("Success", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_renders_timestamps_in_utc_independent_of_host_timezone()
    {
        RegisterServices(ReadySnapshot([Row("event-1", AuditEventCategory.Access)]));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        // Row timestamp is 2026-06-01T10:00:00Z; rendering must stay UTC, not shift to the server's tz.
        cut.Find("[data-testid='tenants-audit-row-timestamp']").TextContent.ShouldBe("2026-06-01 10:00:00 UTC");
    }

    [Fact]
    public void Tenant_audit_page_exposes_keyboard_native_controls_with_accessible_labels()
    {
        RegisterServices(ReadySnapshot([Row("event-1", AuditEventCategory.Access)], nextCursor: "next-cursor", hasMore: true));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.FindAll("label").Count.ShouldBeGreaterThanOrEqualTo(3);
        cut.Find("[data-testid='tenants-audit-filter-from']").ParentElement!.TextContent.ShouldContain("From");
        cut.Find("[data-testid='tenants-audit-filter-to']").ParentElement!.TextContent.ShouldContain("To");
        cut.Find("[data-testid='tenants-audit-filter-category']").ParentElement!.TextContent.ShouldContain("Category");
        cut.Find("[data-testid='tenants-audit-refresh']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-audit-reset']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-audit-next']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-audit-previous']").NodeName.ShouldBe("FLUENT-BUTTON");
        cut.Find("[data-testid='tenants-audit-previous']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-audit-next']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Date_and_category_filters_trigger_server_side_audit_query_and_clear_cursor()
    {
        StubTenantQueryGateway gateway = RegisterServices(
            ReadySnapshot([Row("event-1", AuditEventCategory.Access)], nextCursor: "opaque-next", hasMore: true),
            ReadySnapshot([Row("event-2", AuditEventCategory.Administrative)]),
            ReadySnapshot([Row("event-3", AuditEventCategory.Administrative)]));
        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-audit-next']").Click();
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(2));
        gateway.Requests[1].Cursor.ShouldBe("opaque-next");

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-audit-filter-category", AuditEventCategory.Administrative.ToString());
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(3));

        gateway.Requests[2].Cursor.ShouldBeNull();
        gateway.Requests[2].Category.ShouldBe(AuditEventCategory.Administrative);
    }

    [Fact]
    public void Date_filters_pass_absolute_values_to_gateway()
    {
        StubTenantQueryGateway gateway = RegisterServices(
            ReadySnapshot([Row("event-1", AuditEventCategory.Access)]),
            ReadySnapshot([Row("event-2", AuditEventCategory.Access)]),
            ReadySnapshot([Row("event-3", AuditEventCategory.Access)]));
        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-audit-filter-from']").Change("2026-06-01T10:15");
        cut.Find("[data-testid='tenants-audit-filter-to']").Change("2026-06-02T11:45");
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(3));

        gateway.Requests[1].From.ShouldNotBeNull();
        gateway.Requests[2].To.ShouldNotBeNull();
    }

    [Fact]
    public void Cursor_paging_passes_opaque_cursor_and_previous_history()
    {
        StubTenantQueryGateway gateway = RegisterServices(
            ReadySnapshot([Row("event-1", AuditEventCategory.Access)], nextCursor: "opaque-next", hasMore: true),
            ReadySnapshot([Row("event-2", AuditEventCategory.Access)]),
            ReadySnapshot([Row("event-1", AuditEventCategory.Access)], nextCursor: "opaque-next", hasMore: true));
        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        cut.Find("[data-testid='tenants-audit-next']").Click();
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(2));
        gateway.Requests[1].Cursor.ShouldBe("opaque-next");

        cut.Find("[data-testid='tenants-audit-previous']").Click();
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(3));
        gateway.Requests[2].Cursor.ShouldBeNull();
    }

    [Theory]
    [InlineData(TenantAuditSurfaceKind.Loading, "tenants-audit-loading")]
    [InlineData(TenantAuditSurfaceKind.Empty, "tenants-audit-empty")]
    [InlineData(TenantAuditSurfaceKind.FilteredEmpty, "tenants-audit-filtered-empty")]
    [InlineData(TenantAuditSurfaceKind.Stale, "tenants-audit-stale")]
    [InlineData(TenantAuditSurfaceKind.Degraded, "tenants-audit-degraded")]
    [InlineData(TenantAuditSurfaceKind.Unauthorized, "tenants-audit-unauthorized")]
    [InlineData(TenantAuditSurfaceKind.InvalidCursor, "tenants-audit-invalid-cursor")]
    [InlineData(TenantAuditSurfaceKind.ListRefreshed, "tenants-audit-list-refreshed")]
    [InlineData(TenantAuditSurfaceKind.Unavailable, "tenants-audit-unavailable")]
    [InlineData(TenantAuditSurfaceKind.Error, "tenants-audit-error")]
    public void Tenant_audit_page_renders_distinct_accessible_states(TenantAuditSurfaceKind kind, string selector)
    {
        RegisterServices(SnapshotFor(kind));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));
        cut.WaitForElement($"[data-testid='{selector}']");

        cut.Find($"[data-testid='{selector}']").GetAttribute("role").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-audit-live-region']").TextContent.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Tenant_audit_components_do_not_call_backend_or_use_browser_token_storage()
    {
        string projectRoot = ProjectRoot();
        string[] componentFiles =
        [
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Pages", "TenantAuditPage.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "AuditDataGrid.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "AuditEvidenceReceipt.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "CorrectionStartPanel.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "GlobalAdministratorCorrectionPanel.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "State", "TenantAudit", "TenantAuditReceipt.cs"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "State", "TenantAudit", "TenantCorrectionStartIntent.cs"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "State", "TenantAudit", "GlobalAdministratorCorrectionSnapshot.cs"),
        ];
        string combined = string.Join('\n', componentFiles.Select(File.ReadAllText));

        combined.ShouldNotContain("GET /api/tenants", Case.Insensitive);
        combined.ShouldNotContain("POST /api/v1/commands", Case.Insensitive);
        combined.ShouldNotContain("GET /api/v1/commands/status", Case.Insensitive);
        combined.ShouldNotContain("HttpClient", Case.Insensitive);
        combined.ShouldNotContain("localStorage", Case.Insensitive);
        combined.ShouldNotContain("sessionStorage", Case.Insensitive);
        combined.ShouldNotContain("access_token", Case.Insensitive);
        combined.ShouldNotContain("raw payload", Case.Insensitive);
        combined.ShouldNotContain("EventStore metadata", Case.Insensitive);
    }

    [Fact]
    public void Tenant_correction_copy_uses_forward_recovery_language_and_omits_diagnostic_markers()
    {
        string projectRoot = ProjectRoot();
        string[] files =
        [
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "AuditDataGrid.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "AuditEvidenceReceipt.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "CorrectionStartPanel.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Tenants", "Audit", "GlobalAdministratorCorrectionPanel.razor"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "State", "TenantAudit", "TenantCorrectionStartIntent.cs"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources", "TenantsResources.resx"),
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources", "TenantsResources.fr.resx"),
        ];
        string combined = string.Join('\n', files.Select(File.ReadAllText));

        combined.ShouldNotContain("undo", Case.Insensitive);
        combined.ShouldNotContain("rollback", Case.Insensitive);
        combined.ShouldNotContain("hidden edit", Case.Insensitive);
        combined.ShouldNotContain("Bearer ", Case.Insensitive);
        combined.ShouldNotContain("JWT", Case.Insensitive);
        combined.ShouldNotContain("stack trace", Case.Insensitive);
        combined.ShouldNotContain("correlation id", Case.Insensitive);
        combined.ShouldNotContain("MessageId", Case.Insensitive);
        combined.ShouldNotContain("protected cursor", Case.Insensitive);
        combined.ShouldNotContain("ETag", Case.Insensitive);
    }

    [Fact]
    public void Audit_styles_preserve_responsive_safety_and_accessibility_hooks()
    {
        string projectRoot = ProjectRoot();
        string styles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Audit",
            "AuditDataGrid.razor.css"));
        string pageStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Pages",
            "TenantAuditPage.razor.css"));

        styles.ShouldContain("overflow-x: auto");
        styles.ShouldContain("min-width:");
        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain("tenants-audit-critical");
        styles.ShouldContain("grid-template-columns: minmax(0, 1fr) auto");
        pageStyles.ShouldContain(":focus-visible");
        pageStyles.ShouldContain("@media (forced-colors: active)");

        string receiptStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Audit",
            "AuditEvidenceReceipt.razor.css"));

        receiptStyles.ShouldContain(":focus-visible");
        receiptStyles.ShouldContain("@media (forced-colors: active)");
        receiptStyles.ShouldContain("@media (prefers-reduced-motion: reduce)");
        receiptStyles.ShouldContain("grid-template-columns: repeat(auto-fit");

        string correctionStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Audit",
            "CorrectionStartPanel.razor.css"));

        correctionStyles.ShouldContain(":focus-visible");
        correctionStyles.ShouldContain("@media (forced-colors: active)");
        correctionStyles.ShouldContain("@media (prefers-reduced-motion: reduce)");

        string globalAdminCorrectionStyles = File.ReadAllText(Path.Combine(
            projectRoot,
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Audit",
            "GlobalAdministratorCorrectionPanel.razor.css"));

        globalAdminCorrectionStyles.ShouldContain(":focus-visible");
        globalAdminCorrectionStyles.ShouldContain("@media (forced-colors: active)");
        globalAdminCorrectionStyles.ShouldContain("@media (prefers-reduced-motion: reduce)");
    }

    [Fact]
    public void Audit_resource_keys_have_english_and_french_parity()
    {
        string projectRoot = ProjectRoot();
        HashSet<string> english = ResourceKeys(Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources", "TenantsResources.resx"));
        HashSet<string> french = ResourceKeys(Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources", "TenantsResources.fr.resx"));
        string[] auditKeys = english
            .Where(key => key.StartsWith("Tenants.Audit.", StringComparison.Ordinal)
                || key.StartsWith("Tenants.Correction.", StringComparison.Ordinal))
            .ToArray();

        auditKeys.ShouldNotBeEmpty();
        foreach (string key in auditKeys)
        {
            french.ShouldContain(key);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Tenant_audit_page_uses_localized_fallback_heading_for_blank_tenant_id(string blankTenantId)
    {
        RegisterServices(ReadySnapshot([Row("event-1", AuditEventCategory.Access)]));

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, blankTenantId));
        cut.WaitForElement("[data-testid='tenants-audit-grid']");

        // A blank/whitespace TenantId must render the localized fallback, never a dangling
        // "Audit trail for " heading (AC8 — cosmetic, not a crash fix).
        cut.Markup.ShouldContain("Audit trail for this tenant");
        cut.Markup.ShouldNotContain("Audit trail for <", Case.Insensitive);
    }

    [Fact]
    public void Tenant_audit_page_survives_global_administrator_projection_fault_during_load()
    {
        // A global-administrator audit row triggers a supplementary global-administrator projection read
        // during page load. If that read faults with anything the gateway does not map to a degraded
        // snapshot (here an HttpRequestException), the audit page must still render — the supplementary
        // evidence is best-effort and must not tear down the whole page.
        JSInterop.Mode = JSRuntimeMode.Loose;
        StubTenantQueryGateway gateway = new(GlobalAdminAuditSnapshot("GlobalAdministratorSet", "admin-user"))
        {
            GlobalAdminFault = new HttpRequestException("projection read failed"),
        };
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantsBffComposition>(new StubBffComposition(authorized: true));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();

        IRenderedComponent<TenantAuditPage> cut = Render<TenantAuditPage>(parameters => parameters
            .Add(p => p.TenantId, "tenant.alpha"));

        cut.WaitForElement("[data-testid='tenants-audit-grid']");
        gateway.GlobalAdminRequests.ShouldNotBeEmpty();
    }

    private StubTenantQueryGateway RegisterServices(params TenantAuditSnapshot[] snapshots)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        StubTenantQueryGateway gateway = new(snapshots);
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantsBffComposition>(new StubBffComposition());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        return gateway;
    }

    private StubTenantQueryGateway RegisterGlobalAdminServices(
        bool authorized,
        GlobalAdministratorsSnapshot globalAdministrators,
        params TenantAuditSnapshot[] snapshots)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        StubTenantQueryGateway gateway = new(snapshots) { GlobalAdministrators = globalAdministrators };
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantsBffComposition>(new StubBffComposition(authorized: authorized));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
        return gateway;
    }

    private static GlobalAdministratorsSnapshot GlobalAdmins(params string[] userIds)
        => GlobalAdministratorsSnapshot.Ready(
            userIds.Select(userId => new GlobalAdministratorRow(userId, ReadModelFreshnessState.Current)).ToArray(),
            nextCursor: null,
            hasMore: false,
            eTag: "\"ga-etag\"",
            freshness: ReadModelFreshnessState.Current);

    private static TenantAuditSnapshot GlobalAdminAuditSnapshot(string eventType, string targetUserId)
        => TenantAuditSnapshot.Ready(
            [
                new TenantAuditRow(
                    "event-global-admin",
                    eventType,
                    AuditEventCategory.Administrative,
                    "actor-user",
                    DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
                    "system",
                    targetUserId,
                    "global-administrators",
                    eventType,
                    $"userId: {targetUserId}",
                    ReadModelFreshnessState.Current),
            ],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current,
            new TenantAuditRequest("system"));

    private static TenantAuditSnapshot ReadySnapshot(
        IReadOnlyList<TenantAuditRow> rows,
        string? nextCursor = null,
        bool hasMore = false)
        => TenantAuditSnapshot.Ready(
            rows,
            nextCursor,
            hasMore,
            eTag: "\"etag\"",
            freshness: rows.Any(row => row.Freshness == ReadModelFreshnessState.Stale)
                ? ReadModelFreshnessState.Stale
                : ReadModelFreshnessState.Current,
            new TenantAuditRequest("tenant.alpha"));

    private static TenantAuditSnapshot SnapshotFor(TenantAuditSurfaceKind kind)
    {
        TenantAuditRequest request = new("tenant.alpha", Category: kind is TenantAuditSurfaceKind.FilteredEmpty ? AuditEventCategory.Access : null);
        return kind switch
        {
            TenantAuditSurfaceKind.Loading => TenantAuditSnapshot.Loading("tenant.alpha"),
            TenantAuditSurfaceKind.Empty => TenantAuditSnapshot.Empty(true, ReadModelFreshnessState.Current, "\"etag\"", request),
            TenantAuditSurfaceKind.FilteredEmpty => TenantAuditSnapshot.Empty(true, ReadModelFreshnessState.Current, "\"etag\"", request),
            TenantAuditSurfaceKind.Stale => TenantAuditSnapshot.Stale([Row("event-stale", AuditEventCategory.Access, freshness: ReadModelFreshnessState.Stale)], null, false, "\"etag\"", request),
            TenantAuditSurfaceKind.Degraded => TenantAuditSnapshot.Degraded([Row("event-degraded", AuditEventCategory.Access)], TenantAuditReason.ProjectionDegraded, request),
            TenantAuditSurfaceKind.Unauthorized => TenantAuditSnapshot.Unauthorized(request),
            TenantAuditSurfaceKind.InvalidCursor => TenantAuditSnapshot.InvalidCursor(request),
            TenantAuditSurfaceKind.ListRefreshed => TenantAuditSnapshot.ListRefreshed([Row("event-refreshed", AuditEventCategory.Access)], null, false, "\"etag\"", ReadModelFreshnessState.Current, request),
            TenantAuditSurfaceKind.Unavailable => TenantAuditSnapshot.Unavailable(request),
            TenantAuditSurfaceKind.Error => TenantAuditSnapshot.Error(request),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    private static TenantAuditRow Row(
        string eventReference,
        AuditEventCategory category,
        string referenceContext = "userId: target-user",
        ReadModelFreshnessState freshness = ReadModelFreshnessState.Current,
        string? eventType = null)
    {
        string outcome = eventType ?? (category is AuditEventCategory.Access ? "UserAddedToTenant" : "TenantConfigurationSet");

        return new(
            eventReference,
            outcome,
            category,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "tenant.alpha",
            "target-user",
            "tenant.alpha",
            outcome,
            referenceContext,
            freshness);
    }

    private static HashSet<string> ResourceKeys(string path)
        => XDocument.Load(path)
            .Root!
            .Elements("data")
            .Select(element => element.Attribute("name")?.Value)
            .Where(static name => name is not null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private sealed class StubTenantQueryGateway(params TenantAuditSnapshot[] snapshots) : ITenantQueryGateway
    {
        private readonly Queue<TenantAuditSnapshot> _snapshots = new(snapshots);

        public List<TenantAuditRequest> Requests { get; } = [];
        public List<TenantDetailRequest> DetailRequests { get; } = [];

        public Task<TenantDetailSnapshot> GetTenantAsync(
            TenantDetailRequest request,
            TenantDetailSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            DetailRequests.Add(request);
            TenantDetail detail = new(
                request.TenantId,
                "Tenant Alpha",
                null,
                TenantStatus.Active,
                [new TenantMember("target-user", TenantRole.TenantContributor)],
                new Dictionary<string, string>(StringComparer.Ordinal),
                DateTimeOffset.Parse("2026-06-01T09:00:00Z", CultureInfo.InvariantCulture));
            return Task.FromResult(TenantDetailSnapshot.Ready(detail, "\"detail-etag\"", ReadModelFreshnessState.Current));
        }

        public Task<TenantListSnapshot> ListTenantsAsync(
            TenantListRequest request,
            TenantListSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserTenantMembershipSnapshot> GetMyTenantsAsync(
            UserTenantMembershipRequest request,
            UserTenantMembershipSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserTenantMembershipSnapshot> GetUserTenantsAsync(
            UserTenantMembershipRequest request,
            UserTenantMembershipSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public GlobalAdministratorsSnapshot GlobalAdministrators { get; init; }
            = GlobalAdministratorsSnapshot.Empty(false, ReadModelFreshnessState.Current, "\"ga-etag\"");

        public List<GlobalAdministratorsRequest> GlobalAdminRequests { get; } = [];

        public Exception? GlobalAdminFault { get; init; }

        public Task<GlobalAdministratorsSnapshot> GetGlobalAdministratorsAsync(
            GlobalAdministratorsRequest request,
            GlobalAdministratorsSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            GlobalAdminRequests.Add(request);
            return GlobalAdminFault is not null
                ? throw GlobalAdminFault
                : Task.FromResult(GlobalAdministrators);
        }

        public Task<TenantAuditSnapshot> GetTenantAuditAsync(
            TenantAuditRequest request,
            TenantAuditSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(_snapshots.Dequeue());
        }
    }

    private sealed class StubBffComposition(
        bool readConnected = true,
        bool commandConnected = true,
        bool authorized = false) : ITenantsBffComposition
    {
        public bool IsReadSurfaceConnected => readConnected;

        public bool IsCommandSurfaceConnected => commandConnected;

        public TenantLifecycleAuthorizationReflectionState GlobalAdministratorsAuthorizationReflection
            => authorized
                ? TenantLifecycleAuthorizationReflectionState.Authorized
                : TenantLifecycleAuthorizationReflectionState.Indeterminate;
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        public LocalizedString this[string name] => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));

        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Audit.Back"] = "Back to tenant details",
            ["Tenants.Audit.Category.Access"] = "Access",
            ["Tenants.Audit.Category.Administrative"] = "Administrative",
            ["Tenants.Audit.Column.Actor"] = "Actor",
            ["Tenants.Audit.Column.Category"] = "Category",
            ["Tenants.Audit.Column.Correction"] = "Correction",
            ["Tenants.Audit.Column.Freshness"] = "Freshness",
            ["Tenants.Audit.Column.Outcome"] = "Outcome",
            ["Tenants.Audit.Column.Reference"] = "Reference context",
            ["Tenants.Audit.Column.Receipt"] = "Receipt",
            ["Tenants.Audit.Column.Scope"] = "Tenant scope",
            ["Tenants.Audit.Column.Target"] = "Target",
            ["Tenants.Audit.Column.Timestamp"] = "Timestamp",
            ["Tenants.Audit.ControlsLabel"] = "Tenant audit controls",
            ["Tenants.Audit.Copy.EventReference"] = "Copy audit event reference {0}",
            ["Tenants.Audit.Description"] = "Read-only tenant audit evidence.",
            ["Tenants.Audit.Eyebrow"] = "Tenant audit trail",
            ["Tenants.Audit.Filter.Category"] = "Category",
            ["Tenants.Audit.Filter.Category.All"] = "All categories",
            ["Tenants.Audit.Filter.From"] = "From",
            ["Tenants.Audit.Filter.To"] = "To",
            ["Tenants.Audit.Freshness.Current"] = "Current",
            ["Tenants.Audit.Freshness.Stale"] = "Stale",
            ["Tenants.Audit.Freshness.Unknown"] = "Unknown",
            ["Tenants.Audit.GridTitle"] = "Audit entries",
            ["Tenants.Audit.Next"] = "Next",
            ["Tenants.Audit.PaginationLabel"] = "Tenant audit pages",
            ["Tenants.Audit.Previous"] = "Previous",
            ["Tenants.Audit.Refresh"] = "Refresh",
            ["Tenants.Audit.Reset"] = "Reset filters",
            ["Tenants.Audit.Receipt.ActionsLabel"] = "Audit receipt recovery actions",
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
            ["Tenants.Audit.Receipt.Open"] = "View receipt",
            ["Tenants.Audit.Receipt.State.Degraded"] = "Audit evidence is degraded.",
            ["Tenants.Audit.Receipt.State.Delayed"] = "Audit evidence is delayed.",
            ["Tenants.Audit.Receipt.State.InvalidReference"] = "The requested receipt reference is not loaded.",
            ["Tenants.Audit.Receipt.State.MissingSupport"] = "Audit evidence support is missing.",
            ["Tenants.Audit.Receipt.State.Partial"] = "Audit evidence is partial.",
            ["Tenants.Audit.Receipt.State.Pending"] = "Audit evidence is pending.",
            ["Tenants.Audit.Receipt.State.Ready"] = "Audit evidence is ready.",
            ["Tenants.Audit.Receipt.State.Stale"] = "Audit evidence is stale.",
            ["Tenants.Audit.Receipt.State.Unauthorized"] = "Audit evidence is not authorized.",
            ["Tenants.Audit.Receipt.State.Unavailable"] = "Audit evidence is unavailable.",
            ["Tenants.Audit.Receipt.Title"] = "Audit evidence receipt",
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
            ["Tenants.Audit.State.Degraded.Message"] = "Audit evidence is degraded.",
            ["Tenants.Audit.State.Degraded.Title"] = "Audit data degraded",
            ["Tenants.Audit.State.Empty.Message"] = "No audit entries are visible.",
            ["Tenants.Audit.State.Empty.Title"] = "No audit entries",
            ["Tenants.Audit.State.Error.Message"] = "Audit data could not be loaded.",
            ["Tenants.Audit.State.Error.Title"] = "Audit data unavailable",
            ["Tenants.Audit.State.FilteredEmpty.Message"] = "No audit entries match filters.",
            ["Tenants.Audit.State.FilteredEmpty.Title"] = "No audit entries match filters",
            ["Tenants.Audit.State.InvalidCursor.Message"] = "The audit cursor is no longer valid.",
            ["Tenants.Audit.State.InvalidCursor.Title"] = "Audit page cursor invalid",
            ["Tenants.Audit.State.ListRefreshed.Message"] = "The list was refreshed from page one.",
            ["Tenants.Audit.State.ListRefreshed.Title"] = "Audit list refreshed",
            ["Tenants.Audit.State.Loading.Message"] = "Audit entries are loading.",
            ["Tenants.Audit.State.Loading.Title"] = "Loading audit entries",
            ["Tenants.Audit.State.Ready.Message"] = "Audit entries are loaded.",
            ["Tenants.Audit.State.Ready.Title"] = "Audit entries loaded",
            ["Tenants.Audit.State.Stale.Message"] = "Audit freshness is stale.",
            ["Tenants.Audit.State.Stale.Title"] = "Audit data stale",
            ["Tenants.Audit.State.Unauthorized.Message"] = "You are not authorized.",
            ["Tenants.Audit.State.Unauthorized.Title"] = "Audit access unavailable",
            ["Tenants.Audit.State.Unavailable.Message"] = "The tenant audit read surface is unavailable.",
            ["Tenants.Audit.State.Unavailable.Title"] = "Audit read surface unavailable",
            ["Tenants.Audit.Title"] = "Audit trail for {0}",
            ["Tenants.Audit.UnknownTenant"] = "this tenant",
            ["Tenants.Correction.Action.RestoreAccess"] = "restore intended access",
            ["Tenants.Correction.Action.RestoreAccessAccessible"] = "restore intended access for audit evidence {0}",
            ["Tenants.Correction.Action.Start"] = "start correction",
            ["Tenants.Correction.Action.StartAccessible"] = "start correction for audit evidence {0}",
            ["Tenants.Correction.Unavailable.ExplicitRoleRequired"] = "Choose the intended role before starting correction.",
            ["Tenants.Correction.Unavailable.AuthorizationIndeterminate"] = "Authorization for platform governance could not be confirmed.",
            ["Tenants.Correction.Unavailable.GlobalAdministratorCommandSupportUnavailable"] = "Global administrator correction commands are not connected.",
            ["Tenants.Correction.Domain.GlobalAdministrators"] = "Global administrators",
            ["Tenants.Correction.Command.SetGlobalAdministrator"] = "Set global administrator",
            ["Tenants.Copy.Action"] = "Copy",
            ["Tenants.Copy.Feedback.Copied"] = "Copied",
        };
    }
}
