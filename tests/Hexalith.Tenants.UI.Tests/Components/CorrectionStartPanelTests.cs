using System.Globalization;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class CorrectionStartPanelTests : FluentBunitContext
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
        cut.Find("[data-testid='tenants-correction-current-snapshot']").TextContent.ShouldContain("Current tenant projection");
        cut.Find("[data-testid='tenants-correction-command']").TextContent.ShouldContain("Add user to tenant");
        cut.Find("[data-testid='tenants-correction-domain']").TextContent.ShouldContain("Tenants");
        cut.Find("[data-testid='tenants-correction-preview-data']").TextContent.ShouldContain("Tenant reader");
        cut.Find("[data-testid='tenants-correction-preview-handoff']").NodeName.ShouldBe("FLUENT-BUTTON");
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

    [Fact]
    public void Panel_submits_restore_command_once_and_links_projection_confirmed_corrective_proof()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        StubTenantQueryGateway queryGateway = new(
            Detail(new TenantMember("target-user", TenantRole.TenantReader)),
            Audit("event-corrective", "UserAddedToTenant"));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));
        int focusInvocationCount = JSInterop.Invocations.Count(static invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => commandGateway.AddUserRequests.ShouldHaveSingleItem());
        commandGateway.AddUserRequests[0].TenantId.ShouldBe("tenant.alpha");
        commandGateway.AddUserRequests[0].UserId.ShouldBe("target-user");
        commandGateway.AddUserRequests[0].Role.ShouldBe(TenantRole.TenantReader);
        commandGateway.StatusHandles.ShouldHaveSingleItem().ShouldBe(new TenantCommandTrackingHandle("message-safe", "tracking-safe"));
        queryGateway.DetailRequests.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        queryGateway.AuditRequests.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        cut.Instance.Snapshot!.FocusTarget.ShouldBe(TenantCommandFocusTarget.Lifecycle);
        cut.Find("[data-testid='tenants-correction-state']").TextContent.ShouldContain("Projection confirmed");
        cut.WaitForAssertion(() => JSInterop.Invocations.Count(static invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase)).ShouldBeGreaterThan(focusInvocationCount));
        cut.Find("[data-testid='tenants-correction-proof-link']").GetAttribute("href").ShouldBe("#audit-event-corrective");
        cut.Find("[data-testid='tenants-correction-proof-link']").TextContent.ShouldContain("2026-06-01 10:05:00 UTC");
        cut.Markup.ShouldNotContain("event-original as undone", Case.Insensitive);
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
    }

    [Fact]
    public void Panel_uses_projection_refresh_provider_without_second_tenant_query_and_still_links_proof()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        StubTenantQueryGateway queryGateway = new(
            Detail(new TenantMember("unused-user", TenantRole.TenantOwner)),
            Audit("event-corrective", "UserAddedToTenant"));
        int projectionRefreshCount = 0;
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail())
            .Add(component => component.ProjectionRefreshProvider, () =>
            {
                projectionRefreshCount++;
                return Task.FromResult<TenantDetail?>(Detail(new TenantMember("target-user", TenantRole.TenantReader)));
            }));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        projectionRefreshCount.ShouldBe(1);
        queryGateway.DetailRequests.ShouldBeEmpty();
        queryGateway.AuditRequests.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        cut.Instance.Snapshot!.FocusTarget.ShouldBe(TenantCommandFocusTarget.Lifecycle);
        cut.Find("[data-testid='tenants-correction-state']").TextContent.ShouldContain("Projection confirmed");
        cut.Find("[data-testid='tenants-correction-proof-link']").GetAttribute("href").ShouldBe("#audit-event-corrective");
    }

    [Fact]
    public void Panel_provider_confirmed_correction_reports_audit_delayed_when_no_corrective_row_exists()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        // The audit surfaces only the original removal, never the corrective UserAddedToTenant row,
        // so proof lookup must run after projection confirmation and honestly report it as delayed
        // rather than linking unrelated evidence or collapsing the confirmed state into failure.
        StubTenantQueryGateway queryGateway = new(
            Detail(new TenantMember("unused-user", TenantRole.TenantOwner)),
            Audit("event-original", "UserRemovedFromTenant"));
        int projectionRefreshCount = 0;
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail())
            .Add(component => component.ProjectionRefreshProvider, () =>
            {
                projectionRefreshCount++;
                return Task.FromResult<TenantDetail?>(Detail(new TenantMember("target-user", TenantRole.TenantReader)));
            }));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        projectionRefreshCount.ShouldBe(1);
        queryGateway.DetailRequests.ShouldBeEmpty();
        queryGateway.AuditRequests.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        cut.Instance.Snapshot!.AuditState.ShouldBe(TenantCommandAuditState.AuditDelayed);
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-correction-state']").TextContent.ShouldContain("Projection confirmed");
    }

    [Fact]
    public void Panel_change_role_workflow_sends_change_role_command_and_requeries_projection()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        StubTenantQueryGateway queryGateway = new(
            Detail(new TenantMember("target-user", TenantRole.TenantReader)),
            Audit("event-corrective", "UserRoleChanged"));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRoleChanged", "userId: target-user; oldRole: TenantContributor; newRole: TenantReader"),
            currentRole: TenantRole.TenantContributor,
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail(new TenantMember("target-user", TenantRole.TenantContributor))));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => commandGateway.ChangeRoleRequests.ShouldHaveSingleItem());
        commandGateway.ChangeRoleRequests[0].TenantId.ShouldBe("tenant.alpha");
        commandGateway.ChangeRoleRequests[0].UserId.ShouldBe("target-user");
        commandGateway.ChangeRoleRequests[0].NewRole.ShouldBe(TenantRole.TenantReader);
        commandGateway.AddUserRequests.ShouldBeEmpty();
        queryGateway.DetailRequests.ShouldHaveSingleItem();
        cut.Find("[data-testid='tenants-correction-proof-link']").TextContent.ShouldContain("Corrective evidence linked");
    }

    [Fact]
    public void Panel_blocks_stale_restore_when_current_projection_has_different_role()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail(new TenantMember("target-user", TenantRole.TenantContributor))));

        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-safe-message']").TextContent.ShouldContain("role-change correction");
        cut.Find("[data-testid='tenants-correction-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
        commandGateway.AddUserRequests.ShouldBeEmpty();
    }

    [Fact]
    public void Panel_pre_submit_already_applied_live_updates_to_submittable_when_projection_refreshes_while_open()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail(new TenantMember("target-user", TenantRole.TenantReader))));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();

        cut.Render(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Instance.Snapshot!.CanSubmit.ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Panel_pre_submit_role_conflict_live_updates_to_submittable_when_projection_refreshes_while_open()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail(new TenantMember("target-user", TenantRole.TenantContributor))));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Instance.Snapshot!.SafeMessageKey.ShouldBe("Tenants.Correction.Unavailable.CurrentRoleConflict");
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();

        cut.Render(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Instance.Snapshot!.CanSubmit.ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Panel_tracked_already_applied_status_survives_parent_re_render_without_re_arming_submit()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new()
        {
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 0),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.AlreadyApplied));
        cut.Instance.Snapshot!.HasCommandTracking.ShouldBeTrue();

        cut.Render(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        cut.Instance.Snapshot!.HasCommandTracking.ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        commandGateway.AddUserRequests.ShouldHaveSingleItem();
    }

    [Fact]
    public void Panel_prevents_duplicate_submission_while_command_is_in_flight()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TaskCompletionSource<TenantCommandSubmissionResult> pendingSubmission = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubTenantCommandGateway commandGateway = new() { AddUserResultTask = pendingSubmission.Task };
        StubTenantQueryGateway queryGateway = new(
            Detail(new TenantMember("target-user", TenantRole.TenantReader)),
            Audit("event-corrective", "UserAddedToTenant"));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.WaitForAssertion(() => commandGateway.AddUserRequests.Count.ShouldBe(1));
        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        commandGateway.AddUserRequests.Count.ShouldBe(1);
        pendingSubmission.SetResult(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"));
        cut.WaitForAssertion(() => commandGateway.StatusHandles.Count.ShouldBe(1));
        commandGateway.AddUserRequests.Count.ShouldBe(1);
    }

    [Fact]
    public void Panel_failed_terminal_state_moves_focus_to_lifecycle_without_refresh_tracking()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new()
        {
            AddUserResultTask = Task.FromResult(TenantCommandSubmissionResult.Failed("Correction command failed before verification.")),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));
        int focusInvocationCount = JSInterop.Invocations.Count(static invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Failed));
        cut.Instance.Snapshot!.FocusTarget.ShouldBe(TenantCommandFocusTarget.Lifecycle);
        cut.Find("[data-testid='tenants-correction-state']").TextContent.ShouldContain("failed");
        cut.Find("[data-testid='tenants-correction-refresh']").HasAttribute("disabled").ShouldBeTrue();
        commandGateway.StatusHandles.ShouldBeEmpty();
        cut.WaitForAssertion(() => JSInterop.Invocations.Count(static invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase)).ShouldBeGreaterThan(focusInvocationCount));
    }

    [Fact]
    public void Failed_correction_survives_a_parent_re_render_without_re_arming_submit()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new()
        {
            AddUserResultTask = Task.FromResult(TenantCommandSubmissionResult.Failed("Correction command failed before verification.")),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Failed));

        // A parent re-render (for example an audit pager navigation or projection refresh that keeps this
        // panel open) re-passes the same intent with a refreshed projection that still shows the user
        // absent. The terminal failure must not reset to a fresh, re-armed preview (which would re-enable
        // Submit) and must not discard the failure evidence (AC4).
        cut.Render(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Failed);
        cut.Find("[data-testid='tenants-correction-state']").TextContent.ShouldContain("failed");
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Panel_rejected_terminal_state_moves_focus_to_lifecycle()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        // Submission is accepted, but the command status comes back Rejected. Before the terminal-focus
        // parity fix the panel moved focus to the lifecycle region only for Confirmed/Failed; every other
        // terminal state (Rejected here) silently left focus where it was. Now all terminal states move it,
        // matching the global-administrator correction panel.
        StubTenantCommandGateway commandGateway = new()
        {
            Status = new TenantCommandStatusResult(CommandStatus.Rejected, EventCount: 0),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));
        int focusInvocationCount = JSInterop.Invocations.Count(static invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.WaitForAssertion(() => JSInterop.Invocations.Count(static invocation =>
            invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase)).ShouldBeGreaterThan(focusInvocationCount));
    }

    [Fact]
    public void Panel_does_not_confirm_when_projection_refresh_provider_returns_no_fresh_projection()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);

        // The audit page provider returns null whenever the refreshed tenant projection is not Current
        // (Stale/Degraded/Unknown). A null provider result must fail closed: the correction stays
        // projection-pending, never confirming off stale evidence, focuses Refresh, and links no proof.
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail())
            .Add(component => component.ProjectionRefreshProvider, () => Task.FromResult<TenantDetail?>(null)));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => commandGateway.StatusHandles.ShouldHaveSingleItem());
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.ProjectionPending));
        cut.Instance.Snapshot!.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
    }

    [Fact]
    public void Panel_proof_lookup_ignores_audit_row_not_newer_than_the_original_event()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();

        // The only candidate corrective row shares the original event's timestamp (10:00:00); it is not
        // strictly newer, so it cannot be causal proof of THIS correction. The time-tie-back lookup must
        // reject it and report audit evidence as delayed rather than linking an unrelated historical row.
        StubTenantQueryGateway queryGateway = new(
            Detail(new TenantMember("target-user", TenantRole.TenantReader)),
            Audit("event-not-newer", "UserAddedToTenant"));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantCorrectionStartIntent intent = TenantCorrectionStartIntent.Evaluate(Context(
            Row("UserRemovedFromTenant"),
            intendedRole: TenantRole.TenantReader));

        IRenderedComponent<CorrectionStartPanel> cut = Render<CorrectionStartPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Detail()));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.Instance.Snapshot!.AuditState.ShouldBe(TenantCommandAuditState.AuditDelayed);
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();

        // The corrective-proof audit query is now lower-bounded by the original event timestamp.
        queryGateway.AuditRequests.ShouldHaveSingleItem().From.ShouldBe(
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture));
    }

    private static TenantCorrectionStartContext Context(
        TenantAuditRow row,
        TenantRole? intendedRole = null,
        TenantRole? currentRole = null)
        => new(
            TenantAuditReceipt.FromRow(row),
            row,
            IsAuthorized: true,
            HasCurrentProjectionSnapshot: true,
            CurrentProjectionSnapshotReference: "tenant.alpha@current",
            CurrentRole: currentRole,
            IntendedRole: intendedRole);

    private static TenantDetail Detail(params TenantMember[] members)
        => new(
            "tenant.alpha",
            "Tenant Alpha",
            null,
            TenantStatus.Active,
            members,
            new Dictionary<string, string>(StringComparer.Ordinal),
            DateTimeOffset.Parse("2026-06-01T09:00:00Z", CultureInfo.InvariantCulture));

    private static TenantAuditSnapshot Audit(string eventReference, string eventType)
        => TenantAuditSnapshot.Ready(
            [Row(eventType, eventReference: eventReference)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"audit-etag\"",
            freshness: ReadModelFreshnessState.Current,
            request: new TenantAuditRequest("tenant.alpha"));

    private static TenantAuditRow Row(
        string eventType,
        string referenceContext = "",
        string eventReference = "event-safe-reference")
        => new(
            eventReference,
            eventType,
            eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal) ? AuditEventCategory.Administrative : AuditEventCategory.Access,
            "actor-user",
            eventReference == "event-corrective"
                ? DateTimeOffset.Parse("2026-06-01T10:05:00Z", CultureInfo.InvariantCulture)
                : DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "tenant.alpha",
            eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal) ? "admin-user" : "target-user",
            eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal) ? "global-administrators" : "tenant.alpha",
            eventType,
            string.IsNullOrWhiteSpace(referenceContext)
                ? eventType.StartsWith("GlobalAdministrator", StringComparison.Ordinal) ? "userId: admin-user" : "userId: target-user"
                : referenceContext,
            ReadModelFreshnessState.Current);

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public List<AddUserToTenant> AddUserRequests { get; } = [];

        public List<ChangeUserRole> ChangeRoleRequests { get; } = [];

        public List<TenantCommandTrackingHandle> StatusHandles { get; } = [];

        public Task<TenantCommandSubmissionResult>? AddUserResultTask { get; init; }

        public TenantCommandStatusResult Status { get; init; }
            = new(CommandStatus.Completed, EventCount: 1);

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(
            AddUserToTenant request,
            CancellationToken cancellationToken = default)
        {
            AddUserRequests.Add(request);
            return AddUserResultTask ?? Task.FromResult(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"));
        }

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(
            ChangeUserRole request,
            CancellationToken cancellationToken = default)
        {
            ChangeRoleRequests.Add(request);
            return Task.FromResult(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"));
        }

        public Task<TenantCommandStatusResult> GetStatusAsync(
            TenantCommandTrackingHandle handle,
            CancellationToken cancellationToken = default)
        {
            StatusHandles.Add(handle);
            return Task.FromResult(Status);
        }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(
            CreateTenant request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(
            RemoveUserFromTenant request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(
            UpdateTenant request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(
            SetTenantConfiguration request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubTenantQueryGateway(TenantDetail detail, TenantAuditSnapshot audit) : ITenantQueryGateway
    {
        public List<TenantDetailRequest> DetailRequests { get; } = [];

        public List<TenantAuditRequest> AuditRequests { get; } = [];

        public Task<TenantDetailSnapshot> GetTenantAsync(
            TenantDetailRequest request,
            TenantDetailSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            DetailRequests.Add(request);
            return Task.FromResult(TenantDetailSnapshot.Ready(detail, "\"detail-etag\"", ReadModelFreshnessState.Current));
        }

        public Task<TenantAuditSnapshot> GetTenantAuditAsync(
            TenantAuditRequest request,
            TenantAuditSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            AuditRequests.Add(request);
            return Task.FromResult(audit);
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

        public Task<GlobalAdministratorsSnapshot> GetGlobalAdministratorsAsync(
            GlobalAdministratorsRequest request,
            GlobalAdministratorsSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
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
            ["Tenants.Correction.Action.PreviewHandoff"] = "Continue to correction preview",
            ["Tenants.Correction.Audit.AuditPending"] = "Corrective audit evidence is pending.",
            ["Tenants.Correction.Confirm.Cancel"] = "Cancel",
            ["Tenants.Correction.Confirm.Refresh"] = "Refresh status",
            ["Tenants.Correction.Confirm.Submit"] = "Submit corrective command",
            ["Tenants.Correction.Close"] = "Close correction start",
            ["Tenants.Correction.Command.AddUserToTenant"] = "Add user to tenant",
            ["Tenants.Correction.Command.ChangeUserRole"] = "Change user role",
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
            ["Tenants.Correction.Lifecycle.Title"] = "Correction lifecycle",
            ["Tenants.Correction.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.Correction.Preview.AuditExpectation.Text"] = "Audit evidence is expected after projection confirmation.",
            ["Tenants.Correction.Preview.Consequence.Membership"] = "A new membership event may be appended.",
            ["Tenants.Correction.Preview.Consequence.RoleChange"] = "A new role-change event may be appended.",
            ["Tenants.Correction.Preview.Consequence.Unsupported"] = "No corrective command will be submitted without support.",
            ["Tenants.Correction.Preview.Consequences"] = "Known consequences",
            ["Tenants.Correction.Preview.CurrentProjectionReady"] = "Current tenant projection is available for {0}.",
            ["Tenants.Correction.Preview.CurrentProjectionUnavailable"] = "Current tenant projection is unavailable.",
            ["Tenants.Correction.Preview.CurrentRole"] = "Current role",
            ["Tenants.Correction.Preview.IntendedRole"] = "Intended role",
            ["Tenants.Correction.Preview.RecoveryPath"] = "Recovery path",
            ["Tenants.Correction.Preview.RecoveryPath.Text"] = "Refresh status or inspect audit.",
            ["Tenants.Correction.Preview.Unknown.HistoricalRole"] = "Historical role evidence can be stale.",
            ["Tenants.Correction.Preview.Unknown.SignalR"] = "SignalR nudges do not prove success.",
            ["Tenants.Correction.Preview.Unknown.Unsupported"] = "Global administrator support is unavailable.",
            ["Tenants.Correction.Preview.Unknowns"] = "Known unknowns",
            ["Tenants.Correction.Proof.Link"] = "Corrective evidence linked at {0}.",
            ["Tenants.Correction.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.Correction.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.Correction.Role.TenantReader"] = "Tenant reader",
            ["Tenants.Correction.RoleChoice.Label"] = "Choose intended role",
            ["Tenants.Correction.RoleChoice.Placeholder"] = "Select role",
            ["Tenants.Correction.State.Accepted"] = "Command accepted; projection confirmation is pending.",
            ["Tenants.Correction.State.AlreadyApplied"] = "The intended state is already present.",
            ["Tenants.Correction.State.Confirmed"] = "Projection confirmed the correction.",
            ["Tenants.Correction.State.Degraded"] = "Correction status is degraded.",
            ["Tenants.Correction.State.Failed"] = "Correction command failed.",
            ["Tenants.Correction.State.Previewed"] = "Preview is ready for deliberate confirmation.",
            ["Tenants.Correction.State.ProjectionPending"] = "Projection confirmation is pending.",
            ["Tenants.Correction.State.Rejected"] = "Correction command was rejected.",
            ["Tenants.Correction.State.RequestSent"] = "Correction command was sent.",
            ["Tenants.Correction.State.UnableToVerify"] = "Correction cannot be verified from current evidence.",
            ["Tenants.Correction.Unavailable.CommandSupportUnavailable"] = "Tenant correction command support is unavailable.",
            ["Tenants.Correction.Title"] = "Start correction",
            ["Tenants.Correction.Unavailable.AlreadyApplied"] = "The current projection already shows the intended state.",
            ["Tenants.Correction.Unavailable.CurrentRoleConflict"] = "Current projection shows this user with a different role; start a role-change correction instead.",
            ["Tenants.Correction.Unavailable.GlobalAdministratorCommandSupportUnavailable"] = "Global administrator correction commands are not connected.",
        };
    }
}
