using System.Globalization;
using System.Xml.Linq;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

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
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row()))
            .Add(component => component.OnInspectAudit, () => { }));

        cut.Find("[data-testid='tenants-audit-receipt']").GetAttribute("role").ShouldBe("region");
        cut.Find("#tenants-audit-receipt-heading").GetAttribute("tabindex").ShouldBe("-1");
        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldBe("event-safe-reference");
        cut.Find("[data-testid='tenants-audit-receipt-copy']").GetAttribute("data-copy-kind").ShouldBe("ApprovedReference");
        cut.Markup.ShouldContain("actor-user");
        cut.Markup.ShouldContain("target-user");
        cut.Markup.ShouldContain("tenant.alpha");
        cut.Markup.ShouldContain("User added to tenant");
        cut.Markup.ShouldContain("2026-06-01 10:00:00 UTC");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Find("dl").TextContent.ShouldContain("Actor");
        cut.FindAll("dt").Count.ShouldBe(7);
        cut.FindAll("dd").Count.ShouldBe(7);
        cut.Find("[data-testid='tenants-audit-receipt']").HasAttribute("aria-live").ShouldBeFalse();
        cut.Find("[data-testid='tenants-audit-receipt-state']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find(".audit-evidence-receipt__action").NodeName.ShouldBe("FLUENT-BUTTON");
    }

    [Fact]
    public void Receipt_component_copies_the_seven_field_named_summary()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string resourcePath = Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources", "TenantsResources.resx");
        string resourceFormat = XDocument.Load(resourcePath)
            .Root!
            .Elements("data")
            .Single(element => element.Attribute("name")?.Value is "Tenants.Audit.Receipt.Summary")
            .Element("value")!
            .Value;
        resourceFormat.ShouldContain("{auditReference}");

        string summary = resourceFormat
            .Replace("{actor}", "actor-user", StringComparison.Ordinal)
            .Replace("{target}", "target-user", StringComparison.Ordinal)
            .Replace("{scope}", "tenant.alpha", StringComparison.Ordinal)
            .Replace("{outcome}", "User added to tenant", StringComparison.Ordinal)
            .Replace("{timestamp}", "2026-06-01 10:00:00 UTC", StringComparison.Ordinal)
            .Replace("{projection}", "Current", StringComparison.Ordinal)
            .Replace("{auditReference}", "event-safe-reference", StringComparison.Ordinal);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler writeHandler = module.SetupVoid("writeText", summary).SetVoidResult();
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row())));

        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldBe("event-safe-reference");
        cut.Find("[data-surface-testid='tenants-audit-receipt-copy']").Click();

        cut.WaitForAssertion(() => writeHandler.Invocations.Count.ShouldBe(1));
        writeHandler.Invocations.Single().Arguments[0].ShouldBe(summary);
    }

    [Fact]
    public void French_resources_render_and_copy_the_complete_seven_field_summary()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            Services.AddLocalization();
            const string expected = "Acteur : actor-user | Cible : target-user | Portée locataire : tenant.alpha"
                + " | Résultat : Utilisateur ajouté au locataire | Horodatage : 2026-06-01 10:00:00 UTC"
                + " | Marqueur de projection : Actuel | Référence d’audit : event-safe-reference";
            BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
            JSRuntimeInvocationHandler write = module.SetupVoid("writeText", expected).SetVoidResult();
            IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
                .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row()))
                .Add(component => component.OnInspectAudit, () => { }));

            cut.Find("[data-testid='tenants-audit-receipt-outcome'] dd").TextContent.ShouldBe("Utilisateur ajouté au locataire");
            cut.Find(".audit-evidence-receipt__actions").GetAttribute("aria-label")
                .ShouldBe("Actions de reprise du reçu d’audit");
            const string inspectSelector = "[data-testid='tenants-audit-receipt-recovery-inspectaudit']";
            cut.Find(inspectSelector).TextContent.Trim().ShouldBe("Inspecter l’audit");
            IRenderedComponent<AuditEvidenceReceipt> unavailable = Render<AuditEvidenceReceipt>(parameters => parameters
                .Add(component => component.Receipt, TenantAuditReceipt.Unavailable(surfaceKind: TenantAuditSurfaceKind.Unavailable))
                .Add(component => component.OnInspectAudit, () => { }));
            unavailable.Find(inspectSelector).TextContent.Trim().ShouldBe(cut.Find(inspectSelector).TextContent.Trim());
            cut.Find("[data-surface-testid='tenants-audit-receipt-copy']").Click();
            cut.WaitForAssertion(() => write.Invocations.Count.ShouldBe(1));
            write.Invocations.Single().Arguments[0].ShouldBe(expected);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Theory]
    [InlineData("UserAddedToTenant", "User added to tenant", "Utilisateur ajouté au locataire")]
    [InlineData("UserRemovedFromTenant", "User removed from tenant", "Utilisateur retiré du locataire")]
    [InlineData("UserRoleChanged", "User role changed", "Rôle de l’utilisateur modifié")]
    [InlineData("GlobalAdministratorSet", "Global administrator granted", "Administrateur global accordé")]
    [InlineData("GlobalAdministratorRemoved", "Global administrator removed", "Administrateur global retiré")]
    [InlineData("TenantCreated", "Tenant created", "Locataire créé")]
    [InlineData("TenantUpdated", "Tenant updated", "Locataire modifié")]
    [InlineData("TenantDisabled", "Tenant disabled", "Locataire désactivé")]
    [InlineData("TenantEnabled", "Tenant enabled", "Locataire activé")]
    [InlineData("TenantConfigurationSet", "Tenant configuration set", "Configuration du locataire définie")]
    [InlineData("TenantConfigurationRemoved", "Tenant configuration removed", "Configuration du locataire supprimée")]
    public void Known_outcome_has_correct_localized_meaning_and_runtime_copy(
        string eventType, string english, string french)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        string key = $"Tenants.Audit.Receipt.Outcome.{eventType}";
        static string Value(string path, string resourceKey) => XDocument.Load(path).Root!
            .Elements("data").Single(element => element.Attribute("name")?.Value == resourceKey)
            .Element("value")!.Value;
        Value(Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources", "TenantsResources.resx"), key).ShouldBe(english);
        Value(Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources", "TenantsResources.fr.resx"), key).ShouldBe(french);

        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row(eventType: eventType))));
        cut.Find("[data-testid='tenants-audit-receipt-outcome'] dd").TextContent.ShouldBe(english);
        cut.Find("[data-surface-testid='tenants-audit-receipt-copy']");
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

    [Theory]
    [InlineData(TenantAuditReceiptState.Partial)]
    [InlineData(TenantAuditReceiptState.InvalidReference)]
    public void Receipt_component_omits_copy_for_direct_non_ready_receipts_with_safe_references(
        TenantAuditReceiptState state)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAuditReceipt receipt = DirectReceipt(state, "event-safe-reference");
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, receipt));

        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldBe("event-safe-reference");
    }

    [Theory]
    [InlineData(TenantAuditSurfaceKind.Ready)]
    [InlineData(TenantAuditSurfaceKind.Degraded)]
    [InlineData(TenantAuditSurfaceKind.Error)]
    [InlineData(TenantAuditSurfaceKind.Unauthorized)]
    public void Unmatched_url_hint_and_route_scope_are_not_labeled_as_event_evidence(TenantAuditSurfaceKind surface)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAuditReceipt receipt = TenantAuditReceipt.Unavailable(
            surfaceKind: surface, checkedPage: true);
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, receipt));

        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldBe("-");
        cut.Find("[data-testid='tenants-audit-receipt-scope'] dd").TextContent.ShouldBe("-");
        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("event-requested");
        cut.Markup.ShouldNotContain("tenant.alpha");
        cut.FindAll("[data-testid='tenants-audit-receipt-missing']").Count
            .ShouldBe(surface is TenantAuditSurfaceKind.Ready or TenantAuditSurfaceKind.Degraded ? 1 : 0);
    }

    [Fact]
    public void Receipt_component_omits_unsafe_reference_from_visible_and_copy_surfaces()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAuditReceipt receipt = DirectReceipt(TenantAuditReceiptState.Ready, "Bearer raw-token");
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, receipt));

        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldBe("-");
        cut.Markup.ShouldNotContain("raw-token", Case.Insensitive);
    }

    [Fact]
    public void Ready_direct_receipt_with_the_default_summary_renders_one_copy_control()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, DirectReceipt(TenantAuditReceiptState.Ready, "event-safe-reference")));

        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldHaveSingleItem();
        cut.FindAll("[data-testid='tenants-audit-receipt-copy-blocked']").ShouldBeEmpty();
    }

    [Fact]
    public void Receipt_component_rejects_an_unsafe_localized_summary()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer("Actor: Bearer {actor} | Target: {target} | Tenant scope: {scope} | Outcome: {outcome} | Timestamp: {timestamp} | Projection marker: {projection} | Audit reference: {auditReference}"));
        TenantAuditReceipt receipt = DirectReceipt(TenantAuditReceiptState.Ready, "event-safe-reference");
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, receipt));

        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-audit-receipt-copy-blocked']").GetAttribute("role").ShouldBe("alert");
        cut.Find("[data-testid='tenants-audit-receipt-outcome'] dd").TextContent.ShouldBe("User added to tenant");
        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldBe("event-safe-reference");
        cut.Markup.ShouldNotContain("Bearer", Case.Insensitive);
    }

    [Fact]
    public void Receipt_component_checks_template_safety_across_summary_segments()
    {
        // Each completed segment is safe alone; only checking the whole template detects b-e-a-r + e-r.
        string template = StubTenantsLocalizer.DefaultSummary
            .Replace("{actor} | Target:", "{actor} b-e-a-r | e-r Target:", StringComparison.Ordinal);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer(template));
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, DirectReceipt(TenantAuditReceiptState.Ready, "event-safe-reference")));

        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-audit-receipt-copy-blocked']").GetAttribute("role").ShouldBe("alert");
    }

    // Template text glued to a placeholder can complete a blocked fragment with an individually safe value.
    // Each case starts from a copyable FromRow receipt, so only the completed-segment re-check can block it,
    // and together the cases reach the actor, target, scope, and default (reference) branches.
    [Theory]
    [InlineData("Actor: {actor}", "Actor: to{actor}", "actor", "ken-user")]
    [InlineData("Actor: {actor}", "Actor: t-o{actor}", "actor", "ken-user")]
    [InlineData("Target: {target}", "Target: {target}ken", "target", "to")]
    [InlineData("Tenant scope: {scope}", "Tenant scope: {scope}ken", "scope", "to")]
    [InlineData("Audit reference: {auditReference}", "Audit reference: {auditReference}ken", "auditReference", "to")]
    public void Receipt_component_rechecks_the_completed_summary_across_placeholder_boundaries(
        string segment,
        string gluedSegment,
        string field,
        string value)
    {
        string template = StubTenantsLocalizer.DefaultSummary.Replace(segment, gluedSegment, StringComparison.Ordinal);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer(template));

        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(RowWith(field, value))));

        cut.Find("[data-testid='tenants-audit-receipt-state']").TextContent.ShouldContain("ready", Case.Insensitive);
        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-audit-receipt-copy-blocked']")
            .GetAttribute("role").ShouldBe("alert");
    }

    // The field label is trusted copy: its letters must not join a safe value's letters into a blocked
    // fragment ("Target: agnes" -> "targetagnes" contains "etag"; "Tenant scope: yjx" contains "eyj").
    [Theory]
    [InlineData("target", "agnes")]
    [InlineData("target", "agent-7")]
    [InlineData("scope", "tagco")]
    [InlineData("scope", "yjx-tenant")]
    [InlineData("auditReference", "tag-001")]
    public void Receipt_summary_copy_ignores_fragments_formed_only_with_the_field_label(string field, string value)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(RowWith(field, value))));

        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").Count.ShouldBe(1);
        cut.FindAll("[data-testid='tenants-audit-receipt-copy-blocked']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(256, true)]
    [InlineData(257, false)]
    public void Receipt_summary_copy_enforces_the_256_character_field_boundary(
        int referenceLength,
        bool expectedCopy)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAuditReceipt receipt = TenantAuditReceipt.FromRow(
            Row(eventReference: new string('r', referenceLength)));

        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, receipt));

        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").Count.ShouldBe(expectedCopy ? 1 : 0);
        cut.FindAll("[data-testid='tenants-audit-receipt-copy-blocked']").Count.ShouldBe(expectedCopy ? 0 : 1);
    }

    // A missing resource makes IStringLocalizer echo the key, which carries no placeholder and would
    // otherwise pass the safety policy as a reference-less literal on both the page and the clipboard.
    [Fact]
    public void Receipt_component_rejects_a_localized_summary_that_drops_the_audit_reference()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(
            new StubTenantsLocalizer("Tenants.Audit.Receipt.Summary"));
        TenantAuditReceipt receipt = DirectReceipt(TenantAuditReceiptState.Ready, "event-safe-reference");
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, receipt));

        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-audit-receipt-reference']").TextContent.ShouldBe("event-safe-reference");
    }

    [Theory]
    [InlineData("Actor: {actor} | Target: {target} | Tenant scope: {scope} | Outcome: {outcome} | Timestamp: {timestamp} | Projection marker: {projection} | Audit reference: {{auditReference}}")]
    [InlineData("Actor: {actor} | Target: {actor} | Tenant scope: {scope} | Outcome: {outcome} | Timestamp: {timestamp} | Projection marker: {projection} | Audit reference: {auditReference}")]
    [InlineData("Actor: {target} | Target: {actor} | Tenant scope: {scope} | Outcome: {outcome} | Timestamp: {timestamp} | Projection marker: {projection} | Audit reference: {auditReference}")]
    [InlineData("Actor: {actor} | Target: {target} | Tenant scope: {scope} | Outcome: {outcome} | Timestamp: {timestamp} | Projection marker: {projection} | Audit reference: {unknown}")]
    public void Named_summary_rejects_escaped_repeated_swapped_or_unknown_slots(string template)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer(template));
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row())));
        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-audit-receipt-copy-blocked']").GetAttribute("role").ShouldBe("alert");
    }

    [Theory]
    [InlineData("Tenants.Audit.Receipt.Outcome.UserAddedToTenant")]
    [InlineData("Tenants.Audit.Freshness.Current")]
    public void Unsafe_localized_fact_is_hidden_and_blocks_summary_copy(string resourceKey)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(
            new StubTenantsLocalizer(StubTenantsLocalizer.DefaultSummary, resourceKey, "Bearer secret"));
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row())));

        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-audit-receipt-copy-blocked']").GetAttribute("role").ShouldBe("alert");
        cut.Markup.ShouldNotContain("Bearer secret");
    }

    [Theory]
    [InlineData("Tenants.Audit.Receipt.Outcome.UserAddedToTenant")]
    [InlineData("Tenants.Audit.Freshness.Current")]
    public void Missing_localized_fact_blocks_summary_copy(string resourceKey)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(
            new StubTenantsLocalizer(StubTenantsLocalizer.DefaultSummary, resourceKey, resourceKey));
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row())));

        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-audit-receipt-copy-blocked']").GetAttribute("role").ShouldBe("alert");
        cut.Markup.ShouldNotContain(resourceKey);
    }

    [Fact]
    public void Ready_receipt_hides_inspect_action_without_a_real_inspect_delegate()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row())));

        cut.FindAll(".audit-evidence-receipt__action").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("Inspect audit", Case.Insensitive);
    }

    [Fact]
    public void Loading_receipt_is_polite_pending_and_has_no_recovery_action()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAuditReceipt receipt = TenantAuditReceipt.Unavailable(
            surfaceKind: TenantAuditSurfaceKind.Loading,
            checkedPage: false);
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, receipt)
            .Add(component => component.OnRetry, () => { })
            .Add(component => component.UseResetRecovery, true)
            .Add(component => component.OnReset, () => { }));

        var state = cut.Find("[data-testid='tenants-audit-receipt-state']");
        state.GetAttribute("role").ShouldBe("status");
        state.GetAttribute("aria-live").ShouldBe("polite");
        state.QuerySelector(".audit-evidence-receipt__state-icon")!.TextContent.ShouldBe("...");
        cut.FindAll(".audit-evidence-receipt__action").ShouldBeEmpty();
        cut.FindAll("[data-surface-testid='tenants-audit-receipt-copy']").ShouldBeEmpty();
    }

    [Fact]
    public void User_typed_target_that_strict_configuration_policy_rejects_stays_ready_and_copyable()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantAuditRow row = Row() with
        {
            Target = "infrastructure-admin",
            ReferenceContext = "userId: infrastructure-admin",
            Narrative = new TenantAuditNarrative(UserId: "infrastructure-admin"),
        };
        TenantAuditReceipt receipt = TenantAuditReceipt.FromRow(row);
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, receipt));

        receipt.State.ShouldBe(TenantAuditReceiptState.Ready);
        cut.Find("[data-testid='tenants-audit-receipt-target'] dd").TextContent.ShouldBe("infrastructure-admin");
        cut.Find("[data-surface-testid='tenants-audit-receipt-copy']");
    }

    [Fact]
    public void Receipt_component_recovery_actions_invoke_refresh_or_close_callbacks()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        int retryCount = 0;
        int closeCount = 0;
        int inspectCount = 0;
        IRenderedComponent<AuditEvidenceReceipt> pending = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row(), auditState: TenantCommandAuditState.AuditPending))
            .Add(component => component.OnRetry, () => retryCount++));

        pending.Find("[data-recovery-verb='refresh']").Click();

        retryCount.ShouldBe(1);

        IRenderedComponent<AuditEvidenceReceipt> ready = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row()))
            .Add(component => component.OnClose, () => closeCount++)
            .Add(component => component.OnInspectAudit, () => inspectCount++));

        ready.FindAll(".audit-evidence-receipt__action")
            .Single(button => button.TextContent.Contains("Inspect audit", StringComparison.Ordinal))
            .Click();
        ready.FindAll(".audit-evidence-receipt__action")
            .Single(button => button.TextContent.Contains("Continue read-only", StringComparison.Ordinal))
            .Click();

        inspectCount.ShouldBe(1);
        closeCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public void Receipt_escalates_only_when_its_host_does_not_already_escalate_the_failed_read(
        bool hostEscalatesFailedRead,
        int expectedEscalations)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<Hexalith.Tenants.UI.Services.Gateways.ITenantsBffComposition>(new EscalatingComposition());

        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt,
                TenantAuditReceipt.Unavailable(surfaceKind: TenantAuditSurfaceKind.Unavailable))
            .Add(component => component.HostEscalatesFailedRead, hostEscalatesFailedRead)
            .Add(component => component.OnRetry, () => { })
            .Add(component => component.OnClose, () => { }));

        cut.FindAll("[data-testid='tenants-audit-receipt-recovery-escalate']").Count.ShouldBe(expectedEscalations);
        if (expectedEscalations > 0)
        {
            cut.Find("[data-testid='tenants-audit-receipt-recovery-escalate']").GetAttribute("href")
                .ShouldBe("/support/audit-incident");
        }

        // The other recoveries of the failed read are unaffected by the suppression.
        cut.Find("[data-testid='tenants-audit-receipt-recovery-refresh']");
        cut.Find("[data-testid='tenants-audit-receipt-recovery-continuereadonly']");
    }

    [Fact]
    public void Unavailable_receipt_retry_names_audit_read_and_has_stable_recovery_selector()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        int retries = 0;
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt,
                TenantAuditReceipt.Unavailable(surfaceKind: TenantAuditSurfaceKind.Unavailable))
            .Add(component => component.OnRetry, () => retries++));

        var retry = cut.Find("[data-testid='tenants-audit-receipt-recovery-refresh']");
        retry.TextContent.ShouldContain("Retry audit read");
        cut.Find("[data-testid='tenants-audit-availability']").TextContent.ShouldNotContain("status lookup");
        retry.Click();
        retries.ShouldBe(1);
    }

    [Fact]
    public void French_unavailable_receipt_renders_receipt_specific_recovery_copy_and_retries()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            Services.AddLocalization();
            int retries = 0;
            IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
                .Add(component => component.Receipt,
                    TenantAuditReceipt.Unavailable(surfaceKind: TenantAuditSurfaceKind.Unavailable))
                .Add(component => component.OnRetry, () => retries++));

            var availability = cut.Find("[data-testid='tenants-audit-availability']");
            availability.GetAttribute("aria-label").ShouldBe("Audit indisponible");
            availability.QuerySelector(".tenants-audit-availability__reason")!.TextContent.ShouldBe(
                "Cette lecture de l’audit n’a pas pu vérifier la preuve demandée. Cela ne signifie pas que "
                + "l’enregistrement n’existe pas, et le résultat enregistré reste inchangé.");
            var retry = cut.Find("[data-testid='tenants-audit-receipt-recovery-refresh']");
            retry.TextContent.Trim().ShouldBe("Réessayer la lecture de l’audit");

            retry.Click();

            retries.ShouldBe(1);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
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
        action.ParentElement!.GetAttribute("class").ShouldNotBeNull()
            .ShouldContain("audit-evidence-receipt__correction");

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
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.ShouldNotBeNullOrWhiteSpace();
        cut.FindAll("[data-testid='tenants-correction-start']").ShouldBeEmpty();
    }

    [Fact]
    public void Receipt_availability_forwards_inspect_and_continue_to_their_real_callbacks()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        int retryCount = 0;
        int closeCount = 0;
        int inspectCount = 0;
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
            .Add(component => component.OnClose, () => closeCount++)
            .Add(component => component.OnInspectAudit, () => inspectCount++));

        delayed.Find("[data-recovery-verb='inspectaudit']").Click();

        inspectCount.ShouldBe(1);
        closeCount.ShouldBe(1);
        retryCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(TenantCommandAuditState.AuditPending, "Inspect audit", "polite")]
    [InlineData(TenantCommandAuditState.AuditDelayed, "Inspect audit", "polite")]
    [InlineData(TenantCommandAuditState.AuditUnavailable, "Continue read-only", "assertive")]
    [InlineData(TenantCommandAuditState.MissingSupport, "Continue read-only", "assertive")]
    public void Receipt_component_renders_recovery_actions_without_success_copy(
        TenantCommandAuditState auditState,
        string expectedAction,
        string expectedLiveRegion)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, TenantAuditReceipt.FromRow(Row(), auditState: auditState))
            .Add(component => component.OnRetry, () => { })
            .Add(component => component.OnClose, () => { })
            .Add(component => component.OnInspectAudit, () => { }));

        cut.Markup.ShouldContain(expectedAction);
        cut.Markup.ShouldNotContain("Success", Case.Insensitive);
        cut.Find("[data-testid='tenants-audit-receipt']").HasAttribute("aria-live").ShouldBeFalse();
        cut.Find("[data-testid='tenants-audit-availability-announcement']").GetAttribute("aria-live").ShouldBe(expectedLiveRegion);
        cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("data-audit-source").ShouldBe("audit-receipt");
        cut.FindAll("[data-recovery-verb='wait']").ShouldBeEmpty();
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
        TenantAuditReceipt receipt = TenantAuditReceipt.Unavailable() with { State = state };

        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, receipt));

        cut.Find("[data-testid='tenants-audit-receipt']").GetAttribute("class").ShouldNotBeNull().ShouldContain(state.ToString().ToLowerInvariant());
        cut.Markup.ShouldNotContain("Success", Case.Insensitive);
    }

    [Theory]
    [InlineData(TenantAuditReceiptState.Ready, "inspectaudit,continuereadonly", 0, 1, 1)]
    [InlineData(TenantAuditReceiptState.Partial, "refresh", 1, 0, 0)]
    [InlineData(TenantAuditReceiptState.Loading, "", 0, 0, 0)]
    [InlineData(TenantAuditReceiptState.Error, "refresh", 1, 0, 0)]
    [InlineData(TenantAuditReceiptState.Stale, "refresh", 1, 0, 0)]
    [InlineData(TenantAuditReceiptState.Degraded, "refresh", 1, 0, 0)]
    [InlineData(TenantAuditReceiptState.Unauthorized, "continuereadonly", 0, 1, 0)]
    [InlineData(TenantAuditReceiptState.InvalidCursor, "refresh", 1, 0, 0)]
    [InlineData(TenantAuditReceiptState.InvalidReference, "inspectaudit,refresh", 1, 0, 1)]
    public void Every_receipt_local_state_routes_only_its_named_recoveries(
        TenantAuditReceiptState state,
        string expectedActions,
        int expectedRetries,
        int expectedCloses,
        int expectedInspections)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        int retries = 0;
        int closes = 0;
        int inspections = 0;
        int resets = 0;
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, DirectReceipt(state, "event-safe-reference"))
            .Add(component => component.OnRetry, () => retries++)
            .Add(component => component.OnClose, () => closes++)
            .Add(component => component.OnInspectAudit, () => inspections++)
            .Add(component => component.OnReset, () => resets++));

        string[] expected = string.IsNullOrEmpty(expectedActions)
            ? []
            : expectedActions.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IElement[] actions = [.. cut.FindAll("[data-testid^='tenants-audit-receipt-recovery-']")];
        actions.Select(action => action.GetAttribute("data-testid")!["tenants-audit-receipt-recovery-".Length..])
            .ShouldBe(expected);
        if (actions.Length > 0)
        {
            IElement group = cut.Find(".audit-evidence-receipt__actions");
            group.GetAttribute("role").ShouldBe("group");
            group.GetAttribute("aria-label").ShouldBe("Audit receipt recovery actions");
        }
        else
        {
            cut.FindAll(".audit-evidence-receipt__actions").ShouldBeEmpty();
        }

        foreach (IElement action in actions)
        {
            action.Click();
        }

        retries.ShouldBe(expectedRetries);
        closes.ShouldBe(expectedCloses);
        inspections.ShouldBe(expectedInspections);
        resets.ShouldBe(0);
    }

    [Fact]
    public void Reset_override_replaces_receipt_local_recoveries_and_invokes_only_reset()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        int retries = 0;
        int closes = 0;
        int inspections = 0;
        int resets = 0;
        IRenderedComponent<AuditEvidenceReceipt> cut = Render<AuditEvidenceReceipt>(parameters => parameters
            .Add(component => component.Receipt, DirectReceipt(TenantAuditReceiptState.InvalidReference, "event-safe-reference"))
            .Add(component => component.UseResetRecovery, true)
            .Add(component => component.OnRetry, () => retries++)
            .Add(component => component.OnClose, () => closes++)
            .Add(component => component.OnInspectAudit, () => inspections++)
            .Add(component => component.OnReset, () => resets++));

        IElement reset = cut.Find("[data-testid='tenants-audit-receipt-recovery-reset']");
        cut.FindAll("[data-testid^='tenants-audit-receipt-recovery-']").ShouldHaveSingleItem();
        IElement group = cut.Find(".audit-evidence-receipt__actions");
        group.GetAttribute("role").ShouldBe("group");
        group.GetAttribute("aria-label").ShouldBe("Audit receipt recovery actions");

        reset.Click();

        resets.ShouldBe(1);
        retries.ShouldBe(0);
        closes.ShouldBe(0);
        inspections.ShouldBe(0);
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

    private static TenantAuditReceipt DirectReceipt(TenantAuditReceiptState state, string auditReference)
        => new(
            "actor-user",
            "target-user",
            "tenant.alpha",
            "UserAddedToTenant",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            ReadModelFreshnessState.Current,
            auditReference,
            null,
            state);

    private static TenantAuditRow Row(string eventReference = "event-safe-reference", string eventType = "UserAddedToTenant")
        => new(
            eventReference,
            eventType,
            eventType.StartsWith("Tenant", StringComparison.Ordinal)
                ? AuditEventCategory.Administrative : AuditEventCategory.Access,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "tenant.alpha",
            "target-user",
            "tenant.alpha",
            eventType,
            "userId: target-user",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            QueryResponseProvenance.ProjectionBacked,
            new TenantAuditNarrative(UserId: "target-user"));

    private static TenantAuditRow RowWith(string field, string value)
        => field switch
        {
            "actor" => Row() with { ActorId = value },
            "target" => Row() with { Target = value, Narrative = new TenantAuditNarrative(UserId: value) },
            "scope" => Row() with { TenantId = value, Scope = value },
            _ => Row(eventReference: value),
        };

    private sealed class EscalatingComposition : Hexalith.Tenants.UI.Services.Gateways.ITenantsBffComposition
    {
        public bool IsReadSurfaceConnected => true;

        public bool IsCommandSurfaceConnected => true;

        public string? AuditEscalationRecoveryHref => "/support/audit-incident";
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        internal const string DefaultSummary = "Actor: {actor} | Target: {target} | Tenant scope: {scope} | Outcome: {outcome} | Timestamp: {timestamp} | Projection marker: {projection} | Audit reference: {auditReference}";
        private readonly string _summaryFormat;
        private readonly string? _overrideKey;
        private readonly string? _overrideValue;

        public StubTenantsLocalizer()
            : this(DefaultSummary)
        {
        }

        public StubTenantsLocalizer(string summaryFormat, string? overrideKey = null, string? overrideValue = null)
        {
            _summaryFormat = summaryFormat;
            _overrideKey = overrideKey;
            _overrideValue = overrideValue;
        }

        public LocalizedString this[string name] => new(name,
            name == _overrideKey && _overrideValue is not null
                ? _overrideValue
                : name is "Tenants.Audit.Receipt.Summary" ? _summaryFormat : Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(
                name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    name is "Tenants.Audit.Receipt.Summary"
                        ? _summaryFormat
                        : Values.TryGetValue(name, out string? value) ? value : name,
                    arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static value => new LocalizedString(value.Key, value.Value));

        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Audit.Freshness.Current"] = "Current",
            ["Tenants.Audit.Availability.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Availability.Action.InspectAudit"] = "Inspect audit",
            ["Tenants.Audit.Availability.Action.Refresh"] = "Retry status lookup",
            ["Tenants.Audit.Availability.ActionsLabel"] = "Audit availability recovery actions",
            ["Tenants.Audit.Receipt.ActionsLabel"] = "Audit receipt recovery actions",
            ["Tenants.Audit.Availability.Reason.MissingSupport"] = "In-panel audit verification is not available for this command, so this panel cannot match an audit record to the attempt. The recorded outcome above is unchanged.",
            ["Tenants.Audit.Availability.Reason.Unavailable"] = "The audit status could not be read or verified after the command was sent. This does not mean the record does not exist, and no proof is claimed.",
            ["Tenants.Audit.Availability.State.Delayed"] = "Audit delayed",
            ["Tenants.Audit.Availability.State.MissingSupport"] = "Missing implementation support",
            ["Tenants.Audit.Availability.State.Pending"] = "Audit pending",
            ["Tenants.Audit.Availability.State.Unavailable"] = "Audit unavailable",
            ["Tenants.Audit.Receipt.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Receipt.Action.Escalate"] = "Escalate with reference",
            ["Tenants.Audit.Receipt.Action.InspectAudit"] = "Inspect audit",
            ["Tenants.Audit.Receipt.Action.Refresh"] = "Refresh",
            ["Tenants.Audit.Receipt.Action.RefreshAudit"] = "Retry audit read",
            ["Tenants.Audit.Receipt.Availability.Unavailable.Reason"] = "This audit read could not verify the requested evidence. This does not mean the record does not exist, and the recorded outcome is unchanged.",
            ["Tenants.Audit.Receipt.Action.Reset"] = "Reset audit filters",
            ["Tenants.Audit.Receipt.Action.Retry"] = "Retry",
            ["Tenants.Audit.Receipt.Action.Wait"] = "Wait for audit evidence",
            ["Tenants.Audit.Receipt.Copy"] = "Copy full audit receipt summary",
            ["Tenants.Audit.Receipt.Close"] = "Close receipt",
            ["Tenants.Audit.Receipt.Summary"] = "Actor: {actor} | Target: {target} | Tenant scope: {scope} | Outcome: {outcome} | Timestamp: {timestamp} | Projection marker: {projection} | Audit reference: {auditReference}",
            ["Tenants.Audit.Receipt.Outcome.UserAddedToTenant"] = "User added to tenant",
            ["Tenants.Audit.Receipt.Outcome.UserRemovedFromTenant"] = "User removed from tenant",
            ["Tenants.Audit.Receipt.Outcome.UserRoleChanged"] = "User role changed",
            ["Tenants.Audit.Receipt.Outcome.GlobalAdministratorSet"] = "Global administrator granted",
            ["Tenants.Audit.Receipt.Outcome.GlobalAdministratorRemoved"] = "Global administrator removed",
            ["Tenants.Audit.Receipt.Outcome.TenantCreated"] = "Tenant created",
            ["Tenants.Audit.Receipt.Outcome.TenantUpdated"] = "Tenant updated",
            ["Tenants.Audit.Receipt.Outcome.TenantDisabled"] = "Tenant disabled",
            ["Tenants.Audit.Receipt.Outcome.TenantEnabled"] = "Tenant enabled",
            ["Tenants.Audit.Receipt.Outcome.TenantConfigurationSet"] = "Tenant configuration set",
            ["Tenants.Audit.Receipt.Outcome.TenantConfigurationRemoved"] = "Tenant configuration removed",
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
            ["Tenants.Audit.Receipt.State.Loading"] = "The audit page is loading; this requested event has not been verified.",
            ["Tenants.Audit.Receipt.State.MissingSupport"] = "Audit evidence support is missing. Escalate with the support-safe reference.",
            ["Tenants.Audit.Receipt.State.Partial"] = "Audit evidence is partial. The receipt cannot cite a complete proof.",
            ["Tenants.Audit.Receipt.State.Pending"] = "Audit evidence is pending. Wait or refresh before citing proof.",
            ["Tenants.Audit.Receipt.State.Ready"] = "Audit evidence is ready to cite.",
            ["Tenants.Audit.Receipt.State.Stale"] = "Audit evidence is stale. Refresh before treating it as current.",
            ["Tenants.Audit.Receipt.State.Unauthorized"] = "Audit evidence is not available for the current authorization scope. The requested event could not be verified.",
            ["Tenants.Audit.Receipt.State.Unavailable"] = "Audit evidence is unavailable. Continue read-only or retry later.",
            ["Tenants.Audit.Receipt.Title"] = "Audit evidence receipt",
            ["Tenants.Correction.Action.Start"] = "start correction",
            ["Tenants.Correction.Action.RestoreAccess"] = "restore intended access",
            ["Tenants.Correction.Action.StartAccessible"] = "start correction for audit evidence {0}",
            ["Tenants.Correction.Action.RestoreAccessAccessible"] = "restore intended access for audit evidence {0}",
            ["Tenants.Correction.Unavailable.ExplicitRoleRequired"] = "Choose the intended role before starting correction.",
            ["Tenants.Copy.Action"] = "Copy",
            ["Tenants.Copy.Feedback.Empty"] = "Nothing is available to copy.",
            ["Tenants.Audit.Availability.State.Available"] = "Audit available",
            ["Tenants.Audit.Availability.Reason.Pending"] = "The command's events are stored, but its audit record is not readable yet. It normally appears shortly, and no proof is claimed until it does.",
            ["Tenants.Audit.Availability.Reason.Delayed"] = "The audit record is taking longer than expected to become readable. No proof is claimed until it can be read.",
            ["Tenants.Audit.Availability.RetryLimit"] = "Repeated retries left this state unchanged, so retrying is no longer offered here.",
            ["Tenants.Audit.Recovery.Action.Escalate"] = "Escalate without diagnostics",
        };
    }
}
