using System.Globalization;
using System.Reflection;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Configuration;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class RemoveTenantConfigurationFlowTests : FluentBunitContext
{
    [Fact]
    public void Remove_configuration_flow_renders_complete_preview_with_stable_selectors_and_redacts_sensitive_value()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string>
            {
                ["billing.endpoint"] = "Bearer raw-token",
            }))
            .Add(p => p.TargetKey, "billing.endpoint")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-remove-flow']");
        cut.Find("[data-testid='tenants-config-remove-focus-start']");
        cut.Find("[data-testid='tenants-config-remove-focus-end']");
        cut.Find("[data-testid='tenants-config-remove-preview']");
        cut.FindAll("[data-testid='tenants-config-remove-preview-item']").Count.ShouldBe(11);
        cut.Find("[data-testid='tenants-config-remove-preview-namespace']").TextContent.ShouldBe("billing");
        cut.Find("[data-testid='tenants-config-remove-preview-key']").TextContent.ShouldContain("billing.endpoint");
        cut.Find("[data-testid='tenants-config-remove-preview-current-state']").TextContent.ShouldContain("Unavailable");
        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.endpoint");
        cut.Find("[data-testid='tenants-config-remove-submit']").GetAttribute("disabled").ShouldBeNull();
        cut.Find("[data-testid='tenants-config-remove-live-region']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find("[data-testid='tenants-config-remove-preview']").TextContent.ShouldNotContain("raw-token", Case.Insensitive);
        cut.Markup.ShouldNotContain("audit available", Case.Insensitive);
        cut.Markup.ShouldNotContain("receipt", Case.Insensitive);
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Missing_or_hidden_target_blocks_submission_without_gateway_call_or_success_state()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "security.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.FindAll("[data-testid='tenants-config-remove-preview']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-config-remove-preview-blocked']").TextContent
            .ShouldContain("not visible", Case.Insensitive);
        cut.Find("form").Submit();

        gateway.RemoveConfigurationCallCount.ShouldBe(0);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Confirmation_text_must_match_literal_key_before_gateway_submission()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"),
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-config-remove-submit']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("Billing.Mode");
        cut.Find("form").Submit();

        gateway.RemoveConfigurationCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-config-remove-validation']").TextContent.ShouldContain("billing.mode");
        cut.Find("[data-testid='tenants-config-remove-live-region']").GetAttribute("aria-live").ShouldBe("polite");

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("[data-testid='tenants-config-remove-submit']").GetAttribute("disabled").ShouldBeNull();
    }

    [Fact]
    public void Keyboard_submission_confirms_only_after_status_and_projection_absence()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(
                Detail(request.TenantId, new Dictionary<string, string> { ["billing.other"] = "kept" }))));

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        gateway.RemoveConfigurationCallCount.ShouldBe(1);
        gateway.LastRemoveConfigurationRequest.ShouldNotBeNull().ShouldBe(
            new RemoveTenantConfiguration("tenant.alpha", "billing.mode"));
        cut.Find("[data-testid='tenants-config-remove-state']").TextContent.ShouldContain("Projection confirmed");
        cut.Find("[data-testid='tenants-config-remove-live-region']").GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void Projection_that_still_contains_key_does_not_optimistically_delete_or_confirm()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);
        List<bool> commandActivity = [];

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.OnCommandActivityChanged, isActive => commandActivity.Add(isActive))
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(
                Detail(request.TenantId, new Dictionary<string, string> { [request.Key] = "trial" }))));

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending));
        cut.Instance.Snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        commandActivity.ShouldNotBeEmpty();
        commandActivity[^1].ShouldBeTrue();
        cut.Find("[data-testid='tenants-config-remove-state']").TextContent.ShouldContain("Projection pending");
        cut.Markup.ShouldContain("billing.mode");
    }

    [Fact]
    public void Gateway_unavailable_status_refresh_releases_configuration_command_activity_lock()
    {
        RegisterServices();
        List<bool> commandActivity = [];
        TenantDetail detail = Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" });

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, detail)
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.OnCommandActivityChanged, active => commandActivity.Add(active)));

        TenantRemoveConfigurationCommandSnapshot tracked = TenantRemoveConfigurationCommandSnapshot
            .Idle(detail)
            .Previewed(new RemoveTenantConfiguration("tenant.alpha", "billing.mode"), detail)
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"));
        SetPrivateField(cut.Instance, "_snapshot", tracked);
        SetPrivateField(cut.Instance, "_hasRaisedCommandActivity", true);
        cut.Render(parameters => parameters
            .Add(p => p.Detail, detail)
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.OnCommandActivityChanged, active => commandActivity.Add(active)));

        cut.Find("[data-testid='tenants-config-remove-refresh']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        commandActivity.ShouldBe([false]);
    }

    [Fact]
    public void Configuration_key_not_found_rejection_renders_safe_rejected_state()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-config-remove"),
            Status = new TenantCommandStatusResult(
                CommandStatus.Rejected,
                "The configuration key was not found.",
                "ConfigurationKeyNotFound"),
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(
                Detail(request.TenantId, new Dictionary<string, string>()))));

        cut.Find("[data-testid='tenants-config-remove-confirmation']").Change("billing.mode");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.Find("[data-testid='tenants-config-remove-state']").TextContent.ShouldContain("rejected", Case.Insensitive);
        cut.Find("[data-testid='tenants-config-remove-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation-config-remove", Case.Insensitive);
    }

    [Fact]
    public void Cancel_and_escape_close_without_committing_action()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);
        int closeCount = 0;

        IRenderedComponent<RemoveTenantConfigurationFlow> cut = Render<RemoveTenantConfigurationFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Add(p => p.TargetKey, "billing.mode")
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.OnCloseRequested, () => closeCount++));

        cut.Find("[data-testid='tenants-config-remove-cancel']").Click();
        cut.Find("[data-testid='tenants-config-remove-flow']").KeyDown("Escape");

        closeCount.ShouldBe(2);
        gateway.RemoveConfigurationCallCount.ShouldBe(0);
    }

    [Fact]
    public void Css_contains_forced_colors_focus_and_narrow_layout_hooks()
    {
        string styles = File.ReadAllText(Path.Combine(
            ProjectRoot(),
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Configuration",
            "RemoveTenantConfigurationFlow.razor.css"));

        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain(":focus-visible");
        styles.ShouldContain("tenants-config-remove__narrow");
    }

    private void RegisterServices(ITenantCommandGateway? gateway = null)
    {
        if (gateway is not null)
        {
            Services.AddSingleton(gateway);
        }

        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
    }

    private static void SetPrivateField<TComponent, TValue>(
        TComponent component,
        string fieldName,
        TValue value)
        where TComponent : class
    {
        FieldInfo field = typeof(TComponent).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingFieldException(typeof(TComponent).FullName, fieldName);
        field.SetValue(component, value);
    }

    private static TenantDetail Detail(string tenantId, Dictionary<string, string> configuration)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            [new TenantMember("owner-user", TenantRole.TenantOwner)],
            configuration,
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", CultureInfo.InvariantCulture));

    private static string ProjectRoot()
    {
        string directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (File.Exists(Path.Combine(directory, "Hexalith.Tenants.slnx")))
            {
                return directory;
            }

            directory = Directory.GetParent(directory)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Could not locate project root.");
    }

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public TenantCommandSubmissionResult Submission { get; init; }
            = TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable.");

        public TenantCommandStatusResult Status { get; init; }
            = TenantCommandStatusResult.Unknown("Command status is unavailable.");

        public RemoveTenantConfiguration? LastRemoveConfigurationRequest { get; private set; }

        public int RemoveConfigurationCallCount { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveTenantConfigurationAsync(RemoveTenantConfiguration request, CancellationToken cancellationToken = default)
        {
            RemoveConfigurationCallCount++;
            LastRemoveConfigurationRequest = request;
            return Task.FromResult(Submission);
        }

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
            => Task.FromResult(Status);
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Configuration.Value.Unavailable"] = "Unavailable",
            ["Tenants.Configuration.Remove.Title"] = "Remove configuration",
            ["Tenants.Configuration.Remove.Description"] = "Remove tenant {0} configuration.",
            ["Tenants.Configuration.Remove.Submit"] = "Confirm removal",
            ["Tenants.Configuration.Remove.Refresh"] = "Refresh status",
            ["Tenants.Configuration.Remove.Cancel"] = "Cancel",
            ["Tenants.Configuration.Remove.Confirmation.Label"] = "Type the full configuration key to confirm removal",
            ["Tenants.Configuration.Remove.Confirmation.Help"] = "Type {0} exactly. Cancel or Escape closes without submitting.",
            ["Tenants.Configuration.Remove.Lifecycle.Title"] = "Configuration removal lifecycle",
            ["Tenants.Configuration.Remove.Unavailable.Authorization"] = "You are not authorized to remove configuration for this tenant.",
            ["Tenants.Configuration.Remove.Unavailable.ProjectionState"] = "Tenant detail is unavailable or degraded.",
            ["Tenants.Configuration.Remove.Unavailable.Freshness"] = "Refresh current tenant detail before removing configuration.",
            ["Tenants.Configuration.Remove.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow configuration removal.",
            ["Tenants.Configuration.Remove.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.Configuration.Remove.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.Configuration.Remove.Unavailable.Identity"] = "Tenant identity is unavailable.",
            ["Tenants.Configuration.Remove.Unavailable.Scope"] = "No authorized namespace prefix evidence is available.",
            ["Tenants.Configuration.Remove.Unavailable.Target"] = "The selected configuration key is not visible in the current authorized projection.",
            ["Tenants.Configuration.Remove.Unavailable.Narrow"] = "Configuration removal is unavailable on narrow layouts.",
            ["Tenants.Configuration.Remove.Validation.KeyRequired"] = "Select a visible configuration key before previewing removal.",
            ["Tenants.Configuration.Remove.Validation.KeyVisible"] = "The selected key cannot be proven from the current authorized projection.",
            ["Tenants.Configuration.Remove.Validation.NamespaceScope"] = "The namespace prefix cannot be proven from the current authorized projection.",
            ["Tenants.Configuration.Remove.Validation.ConfirmationRequired"] = "Type {0} exactly before removing this configuration key.",
            ["Tenants.Configuration.Remove.Preview.Title"] = "Consequence preview",
            ["Tenants.Configuration.Remove.Preview.Blocked.Required"] = "Complete required preview inputs.",
            ["Tenants.Configuration.Remove.Preview.Tenant"] = "Tenant",
            ["Tenants.Configuration.Remove.Preview.Namespace"] = "Namespace",
            ["Tenants.Configuration.Remove.Preview.Key"] = "Full key",
            ["Tenants.Configuration.Remove.Preview.CurrentState"] = "Current known state",
            ["Tenants.Configuration.Remove.Preview.IntendedEffect"] = "Intended effect",
            ["Tenants.Configuration.Remove.Preview.IntendedEffect.Value"] = "The selected configuration key will be removed after projection proof.",
            ["Tenants.Configuration.Remove.Preview.Freshness"] = "Freshness evidence",
            ["Tenants.Configuration.Remove.Preview.Authorization"] = "Authorization and scope evidence",
            ["Tenants.Configuration.Remove.Preview.Authorization.Value"] = "The namespace prefix and key are visible in the authorized projection.",
            ["Tenants.Configuration.Remove.Preview.KnownConsequences"] = "Known consequences",
            ["Tenants.Configuration.Remove.Preview.KnownConsequences.Value"] = "Consumers may lose this configured value.",
            ["Tenants.Configuration.Remove.Preview.KnownUnknowns"] = "Known unknowns",
            ["Tenants.Configuration.Remove.Preview.KnownUnknowns.Value"] = "Downstream impact is not proven.",
            ["Tenants.Configuration.Remove.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.Configuration.Remove.Preview.AuditExpectation.Value"] = "Audit evidence is pending.",
            ["Tenants.Configuration.Remove.Preview.RecoveryPath"] = "Recovery path",
            ["Tenants.Configuration.Remove.Preview.RecoveryPath.Value"] = "Refresh tenant detail or submit a forward correction.",
            ["Tenants.Configuration.Remove.Freshness.Current"] = "Current",
            ["Tenants.Configuration.Remove.Freshness.Aging"] = "Aging",
            ["Tenants.Configuration.Remove.Freshness.Refreshing"] = "Refreshing",
            ["Tenants.Configuration.Remove.Freshness.Stale"] = "Stale",
            ["Tenants.Configuration.Remove.Freshness.Unknown"] = "Unknown",
            ["Tenants.Configuration.Remove.DuplicatePrevented.Message"] = "A configuration removal command is already in progress.",
            ["Tenants.Configuration.Remove.State.Idle"] = "No configuration removal command submitted.",
            ["Tenants.Configuration.Remove.State.Previewed"] = "Configuration removal preview ready.",
            ["Tenants.Configuration.Remove.State.RequestSent"] = "Configuration removal request sent.",
            ["Tenants.Configuration.Remove.State.Accepted"] = "Accepted by EventStore; waiting for configuration removal processing.",
            ["Tenants.Configuration.Remove.State.ProjectionPending"] = "Projection pending; the key is not confirmed removed yet.",
            ["Tenants.Configuration.Remove.State.Confirmed"] = "Projection confirmed the selected configuration key is removed.",
            ["Tenants.Configuration.Remove.State.Rejected"] = "Configuration removal command rejected.",
            ["Tenants.Configuration.Remove.State.AlreadyApplied"] = "Already applied.",
            ["Tenants.Configuration.Remove.State.DuplicatePrevented"] = "Duplicate configuration removal prevented.",
            ["Tenants.Configuration.Remove.State.Failed"] = "Configuration removal submission failed.",
            ["Tenants.Configuration.Remove.State.Degraded"] = "Configuration removal result is degraded.",
            ["Tenants.Configuration.Remove.State.UnableToVerify"] = "Unable to verify the configuration removal result.",
            ["Tenants.Configuration.Remove.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.Configuration.Remove.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.Configuration.Remove.Audit.AuditDelayed"] = "Audit evidence delayed.",
            ["Tenants.Configuration.Remove.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.Configuration.Remove.Audit.MissingSupport"] = "Audit evidence support is missing.",
            ["Tenants.Audit.EntryPoint.Accessible.Command"] = "Open audit evidence for {0} in tenant {1}",
            ["Tenants.Audit.EntryPoint.CommandReason"] = "Open the tenant audit list and use the visible audit state.",
            ["Tenants.Audit.EntryPoint.Label"] = "Audit evidence",
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope.",
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
            ["Tenants.Configuration.Remove.Recovery.Idle"] = "Choose a visible key when projection evidence is available.",
            ["Tenants.Configuration.Remove.Recovery.Previewed"] = "Confirm removal, cancel, or continue read-only.",
            ["Tenants.Configuration.Remove.Recovery.RequestSent"] = "Wait for command status and projection refresh.",
            ["Tenants.Configuration.Remove.Recovery.Accepted"] = "Wait, refresh status, or continue read-only.",
            ["Tenants.Configuration.Remove.Recovery.ProjectionPending"] = "Refresh tenant detail; do not display removal as complete.",
            ["Tenants.Configuration.Remove.Recovery.Confirmed"] = "Continue read-only or inspect audit later.",
            ["Tenants.Configuration.Remove.Recovery.Rejected"] = "Refresh projection evidence, request permission, start correction, or escalate.",
            ["Tenants.Configuration.Remove.Recovery.AlreadyApplied"] = "Refresh projection evidence before treating the missing key as already removed.",
            ["Tenants.Configuration.Remove.Recovery.DuplicatePrevented"] = "Wait for the in-flight command.",
            ["Tenants.Configuration.Remove.Recovery.Failed"] = "Retry after checking projection evidence.",
            ["Tenants.Configuration.Remove.Recovery.Degraded"] = "Wait, retry status lookup, or escalate.",
            ["Tenants.Configuration.Remove.Recovery.UnableToVerify"] = "Refresh, retry status lookup, or escalate.",
        };

        public LocalizedString this[string name]
            => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }
}
