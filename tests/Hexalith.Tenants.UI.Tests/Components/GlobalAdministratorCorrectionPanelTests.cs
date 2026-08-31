using System.Globalization;
using System.Security.Claims;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Audit;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantUsers;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Components.Authorization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class GlobalAdministratorCorrectionPanelTests : FluentBunitContext
{
    public GlobalAdministratorCorrectionPanelTests()
    {
        var viewport = new TenantHighImpactViewportObservation();
        viewport.Observe(ViewportTier.Desktop);
        Services.AddSingleton(viewport);
        Services.AddSingleton(new TenantAggregateCommandAdmissionGate());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition());
        Services.AddSingleton<AuthenticationStateProvider>(new StubAuthenticationStateProvider());
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            Projection("other-admin"),
            Audit("proof", "GlobalAdministratorSet")));
    }

    [Fact]
    public void Initial_correction_authorization_ignores_non_authoritative_synchronous_reflection_until_resolver_completes()
    {
        var pendingAuthorization = new TaskCompletionSource<TenantLifecycleAuthorizationReflectionState>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition
        {
            SynchronousReflection = TenantLifecycleAuthorizationReflectionState.Authorized,
            AuthorizationTask = pendingAuthorization.Task,
        });
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        cut.FindAll("[data-testid='tenants-correction-confirm']").ShouldBeEmpty();

        pendingAuthorization.SetResult(TenantLifecycleAuthorizationReflectionState.Authorized);
        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse());
    }

    [Fact]
    public void Restore_preview_shows_fixed_scope_command_and_no_tenant_role_selector()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        TenantCorrectionStartIntent intent = RestoreIntent();

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        cut.Find("[data-testid='tenants-correction-panel']").GetAttribute("role").ShouldBe("region");
        cut.Find("[data-testid='tenants-correction-domain']").TextContent.ShouldContain("Global administrators");
        cut.Find("[data-testid='tenants-correction-command']").TextContent.ShouldContain("Set global administrator");
        cut.Find("[data-testid='tenants-correction-scope']").TextContent.ShouldContain("global-administrators");
        cut.Find("[data-testid='tenants-correction-target-user']").TextContent.ShouldContain("admin-user");
        cut.Find("[data-testid='tenants-correction-current-state']").TextContent.ShouldContain("Absent");
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse();
        // Platform-governance correction must never offer a tenant role selector (AC1/AC2).
        cut.FindAll("[data-testid='tenants-correction-role']").ShouldBeEmpty();
        cut.VisibleText().ShouldNotContain("tenant role", Case.Insensitive);
        cut.VisibleText().ShouldNotContain("member", Case.Insensitive);
    }

    [Fact]
    public void Restore_submits_set_command_once_and_links_projection_confirmed_corrective_proof()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        StubTenantQueryGateway queryGateway = new(
            Projection("admin-user", "other-admin"),
            Audit("event-corrective", "GlobalAdministratorSet"));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => commandGateway.SetRequests.ShouldHaveSingleItem());
        commandGateway.SetRequests[0].UserId.ShouldBe("admin-user");
        commandGateway.RemoveRequests.ShouldBeEmpty();
        commandGateway.StatusHandles.ShouldHaveSingleItem().ShouldBe(new TenantCommandTrackingHandle(
            commandGateway.TrackedMessageIds.ShouldHaveSingleItem(),
            "tracking-safe",
            GlobalAdministratorGrantPreview.FixedAggregateId));
        queryGateway.GlobalAdminRequests.Count.ShouldBe(1);
        queryGateway.AuditRequests.ShouldHaveSingleItem().TenantId.ShouldBe("system");
        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed);
        cut.Find("[data-testid='tenants-correction-state']").TextContent.ShouldContain("Projection confirms the intended state", Case.Insensitive);
        cut.Find("[data-testid='tenants-correction-proof-link']").GetAttribute("href").ShouldBe("#audit-event-corrective");
        cut.Find("[data-testid='tenants-correction-proof-link']").TextContent.ShouldContain("2026-06-01 10:05:00 UTC");
        cut.Markup.ShouldNotContain("undone", Case.Insensitive);
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
    }

    [Fact]
    public void Restore_uses_projection_refresh_provider_without_second_global_admin_query_and_still_links_proof()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        StubTenantQueryGateway queryGateway = new(
            Projection("unused-admin"),
            Audit("event-corrective", "GlobalAdministratorSet"));
        int projectionRefreshCount = 0;
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin"))
            .Add(component => component.ProjectionRefreshProvider, () =>
            {
                projectionRefreshCount++;
                return Task.FromResult<GlobalAdministratorsSnapshot?>(ProjectionAfterGrant("admin-user", "other-admin"));
            }));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        projectionRefreshCount.ShouldBe(1);
        queryGateway.GlobalAdminRequests.ShouldBeEmpty();
        queryGateway.AuditRequests.ShouldHaveSingleItem().TenantId.ShouldBe("system");
        cut.Find("[data-testid='tenants-correction-state']").TextContent.ShouldContain("Projection confirms the intended state", Case.Insensitive);
        cut.Find("[data-testid='tenants-correction-proof-link']").GetAttribute("href").ShouldBe("#audit-event-corrective");
    }

    [Fact]
    public void Revoke_of_last_global_administrator_is_hard_blocked_before_submit()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RevokeIntent())
            .Add(component => component.CurrentProjection, Projection("admin-user")));

        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-safe-message']").TextContent.ShouldContain("last global administrator");
        cut.Find("[data-testid='tenants-correction-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        commandGateway.RemoveRequests.ShouldBeEmpty();
    }

    [Fact]
    public void Pre_submit_preview_live_updates_last_admin_hard_stop_when_projection_refreshes_while_open()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        TenantCorrectionStartIntent intent = RevokeIntent();

        // Open a revoke preview against a two-admin projection: submittable.
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Projection("admin-user", "other-admin")));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse();

        // While the panel stays open (same audit intent), the parent passes a refreshed projection in which
        // only the target remains. The last-administrator hard stop must re-engage instead of staying frozen
        // at open-time (the pre-submit preview lives; a submitted/terminal state would be preserved).
        cut.Render(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Projection("admin-user")));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Instance.Snapshot!.SafeMessageKey.ShouldBe("Tenants.Correction.GlobalAdmin.LastAdministrator");
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void ExistingGrantTargetFailsClosedThenLiveUpdatesWhenTargetBecomesAbsent()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        TenantCorrectionStartIntent intent = RestoreIntent();

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Projection("admin-user", "other-admin")));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Instance.Snapshot.SafeMessageKey.ShouldBe(
            "Tenants.GlobalAdministrators.Grant.Preview.Unavailable.TargetExists");
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();

        cut.Render(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Instance.Snapshot!.CanSubmit.ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Pre_submit_blocked_last_administrator_live_updates_to_submittable_when_projection_refreshes_while_open()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        TenantCorrectionStartIntent intent = RevokeIntent();

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Projection("admin-user")));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Instance.Snapshot!.SafeMessageKey.ShouldBe("Tenants.Correction.GlobalAdmin.LastAdministrator");
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();

        cut.Render(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Projection("admin-user", "other-admin")));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Previewed);
        cut.Instance.Snapshot!.CanSubmit.ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Revoke_submits_remove_command_and_confirms_only_on_absent_projection()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        StubTenantQueryGateway queryGateway = new(
            Projection("other-admin"),
            Audit("event-corrective", "GlobalAdministratorRemoved"));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RevokeIntent())
            .Add(component => component.CurrentProjection, Projection("admin-user", "other-admin")));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => commandGateway.RemoveRequests.ShouldHaveSingleItem());
        commandGateway.RemoveRequests[0].UserId.ShouldBe("admin-user");
        commandGateway.SetRequests.ShouldBeEmpty();
        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed);
        cut.Find("[data-testid='tenants-correction-proof-link']").GetAttribute("href").ShouldBe("#audit-event-corrective");
    }

    [Fact]
    public void Restore_confirmation_walks_all_pages_before_accepting_later_page_presence()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        StubTenantQueryGateway queryGateway = new(
            Projection("unused-admin"),
            Audit("event-corrective", "GlobalAdministratorSet"))
        {
            GlobalAdministratorProvider = request => request.Cursor is null
                ? ProjectionPage(["other-admin"], "page-2", hasMore: true)
                : ProjectionPage(["admin-user"], null, hasMore: false),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        queryGateway.GlobalAdminRequests.Select(static request => request.Cursor).ShouldBe([null, "page-2"]);
        cut.Instance.Snapshot!.LastConfirmedProjectionEvidence.ShouldNotBeNull()
            .IsCompleteEvidence.ShouldBeTrue();
    }

    [Fact]
    public void Revoke_confirmation_walks_all_pages_before_accepting_complete_absence()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        StubTenantQueryGateway queryGateway = new(
            Projection("unused-admin"),
            Audit("event-corrective", "GlobalAdministratorRemoved"))
        {
            GlobalAdministratorProvider = request => request.Cursor is null
                ? ProjectionPage(["other-admin"], "page-2", hasMore: true)
                : ProjectionPage(["third-admin"], null, hasMore: false),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RevokeIntent())
            .Add(component => component.CurrentProjection, Projection("admin-user", "other-admin")));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        queryGateway.GlobalAdminRequests.Select(static request => request.Cursor).ShouldBe([null, "page-2"]);
        cut.Instance.Snapshot!.TargetCurrentlyPresent.ShouldBeFalse();
        cut.Instance.Snapshot.CurrentAdministratorCount.ShouldBe(2);
    }

    [Fact]
    public void Rejected_remove_stays_rejected_without_false_success()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new()
        {
            RemoveResultTask = Task.FromResult(TenantCommandSubmissionResult.Rejected(
                "The last global administrator cannot be removed.",
                "LastGlobalAdministrator")),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RevokeIntent())
            .Add(component => component.CurrentProjection, Projection("admin-user", "other-admin")));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.Instance.Snapshot!.RejectionCode.ShouldBe("LastGlobalAdministrator");
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-correction-state']").TextContent
            .ShouldNotContain("Projection confirms the intended state", Case.Insensitive);
    }

    [Fact]
    public void TrackedZeroEventGrantStatusFailsClosedAcrossParentRerender()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new()
        {
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 0, HasVerifiedCommandIdentity: true),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            Projection("other-admin"),
            TenantAuditSnapshot.Unavailable(new TenantAuditRequest("system"))));
        TenantCorrectionStartIntent intent = RestoreIntent();

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        cut.Instance.Snapshot!.HasCommandTracking.ShouldBeTrue();

        cut.Render(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        cut.Instance.Snapshot!.HasCommandTracking.ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        commandGateway.SetRequests.ShouldHaveSingleItem();
    }

    [Fact]
    public void Malformed_original_timestamp_confirms_projection_but_does_not_link_corrective_proof()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new();
        StubTenantQueryGateway queryGateway = new(
            Projection("admin-user", "other-admin"),
            Audit("event-corrective", "GlobalAdministratorSet"));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, WithOriginalTimestamp(RestoreIntent(), "not-a-timestamp"))
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.Instance.Snapshot!.AuditState.ShouldBe(TenantCommandAuditState.AuditDelayed);
        cut.Instance.Snapshot!.ProofLink.ShouldBeNull();
        queryGateway.AuditRequests.ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-correction-proof-link']").ShouldBeEmpty();
    }

    [Fact]
    public void Rejected_correction_survives_a_parent_re_render_without_re_arming_submit()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new()
        {
            RemoveResultTask = Task.FromResult(TenantCommandSubmissionResult.Rejected(
                "The last global administrator cannot be removed.",
                "LastGlobalAdministrator")),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = RevokeIntent();

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Projection("admin-user", "other-admin")));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Rejected));

        // A parent re-render (for example an audit pager navigation that keeps this panel open)
        // re-passes the same intent with a refreshed projection. The terminal rejection must not reset
        // to a fresh, re-armed preview and must not discard the rejection evidence (AC4/AC8).
        cut.Render(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Projection("admin-user", "other-admin", "third-admin")));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Rejected);
        cut.Instance.Snapshot!.RejectionCode.ShouldBe("LastGlobalAdministrator");
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        commandGateway.RemoveRequests.ShouldHaveSingleItem();
    }

    [Fact]
    public void Prevents_duplicate_submission_while_command_is_in_flight()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TaskCompletionSource<TenantCommandSubmissionResult> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubTenantCommandGateway commandGateway = new() { SetResultTask = pending.Task };
        StubTenantQueryGateway queryGateway = new(
            Projection("admin-user", "other-admin"),
            Audit("event-corrective", "GlobalAdministratorSet"));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.WaitForAssertion(() => commandGateway.SetRequests.Count.ShouldBe(1));
        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        commandGateway.SetRequests.Count.ShouldBe(1);
        pending.SetResult(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"));
        cut.WaitForAssertion(() => commandGateway.StatusHandles.Count.ShouldBe(1));
        commandGateway.SetRequests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Superseded_accepted_correction_is_immediately_adopted_resumed_and_confirmed()
    {
        var pending = new TaskCompletionSource<TenantCommandSubmissionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commandGateway = new StubTenantCommandGateway { SetResultTask = pending.Task };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            Projection("admin-user", "other-admin"),
            Audit("event-corrective", "GlobalAdministratorSet")));
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        Task submit = cut.Find("[data-testid='tenants-correction-confirm']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.WaitForAssertion(() => commandGateway.SetRequests.ShouldHaveSingleItem());
        (Task rendererBlock, TaskCompletionSource releaseRenderer) = await BlockRendererAsync(cut);
        pending.SetResult(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"));
        for (int iteration = 0; iteration < 20; iteration++)
        {
            await Task.Yield();
        }

        IncrementPrivateGeneration(cut.Instance, "_operationGeneration");
        releaseRenderer.SetResult();
        await Task.WhenAll(rendererBlock, submit).WaitAsync(TimeSpan.FromSeconds(5));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed);
        var replacementOwner = new object();
        TenantAggregateCommandAdmissionGate admissionGate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        admissionGate.TryAcquireLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            replacementOwner,
            out TenantAggregateCommandLease? replacementLease).ShouldBeTrue();
        replacementLease!.TryAbandonBeforeDispatch(replacementOwner).ShouldBeTrue();
    }

    [Theory]
    [InlineData("rejected")]
    [InlineData("failed")]
    public async Task Superseded_terminal_correction_completion_releases_aggregate_lease(string outcome)
    {
        var pending = new TaskCompletionSource<TenantCommandSubmissionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commandGateway = new StubTenantCommandGateway { SetResultTask = pending.Task };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            Projection("admin-user", "other-admin"),
            Audit("event-corrective", "GlobalAdministratorSet")));
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        Task submit = cut.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.WaitForAssertion(() => commandGateway.SetRequests.ShouldHaveSingleItem());
        (Task rendererBlock, TaskCompletionSource releaseRenderer) = await BlockRendererAsync(cut);
        pending.SetResult(outcome == "rejected"
            ? TenantCommandSubmissionResult.Rejected("The correction was rejected.", "RejectedForTest")
            : TenantCommandSubmissionResult.Failed("The correction failed."));
        for (int iteration = 0; iteration < 20; iteration++)
        {
            await Task.Yield();
        }

        IncrementPrivateGeneration(cut.Instance, "_operationGeneration");
        releaseRenderer.SetResult();
        await Task.WhenAll(rendererBlock, submit).WaitAsync(TimeSpan.FromSeconds(5));

        TenantAggregateCommandAdmissionGate admissionGate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        var replacementOwner = new object();
        admissionGate.TryAcquireLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            replacementOwner,
            out TenantAggregateCommandLease? replacementLease).ShouldBeTrue();
        replacementLease!.TryAbandonBeforeDispatch(replacementOwner).ShouldBeTrue();
    }

    [Fact]
    public async Task AmbiguousGrantExceptionRetainsLeaseAndSameCommandIdentity()
    {
        var commandGateway = new StubTenantCommandGateway
        {
            SetResultTask = Task.FromException<TenantCommandSubmissionResult>(new HttpRequestException("transport detail")),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            Projection("admin-user", "other-admin"),
            Audit("event-corrective", "GlobalAdministratorSet")));
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        await cut.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.RequestSent);
        cut.Instance.Snapshot.IsSubmissionAmbiguous.ShouldBeTrue();
        string retainedMessageId = commandGateway.TrackedMessageIds.ShouldHaveSingleItem();
        retainedMessageId.ShouldBe(cut.Instance.Snapshot.MessageId);
        cut.Find("[data-testid='tenants-correction-refresh']").TextContent
            .ShouldContain("same tracked command", Case.Insensitive);
        cut.Markup.ShouldNotContain("transport detail", Case.Insensitive);
        TenantAggregateCommandAdmissionGate admissionGate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        var replacementOwner = new object();
        admissionGate.TryAcquireLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            replacementOwner,
            out _).ShouldBeFalse();

        commandGateway.SetResultTask = Task.FromResult(
            TenantCommandSubmissionResult.Accepted(retainedMessageId, "tracking-safe"));
        await cut.Find("[data-testid='tenants-correction-refresh']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState
            .ShouldBe(TenantCommandLifecycleState.Confirmed));
        commandGateway.TrackedMessageIds.ShouldBe([retainedMessageId, retainedMessageId]);
        commandGateway.SetRequests.ShouldAllBe(request => request.UserId == "admin-user");
        admissionGate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse();
    }

    [Fact]
    public async Task MismatchedGrantAcceptanceRetainsLeaseAndOffersSameIdentityDeliveryRetry()
    {
        var commandGateway = new StubTenantCommandGateway
        {
            SetResultTask = Task.FromResult(
                TenantCommandSubmissionResult.Accepted("different-message", "tracking-safe")),
            ReturnConfiguredTrackedIdentity = true,
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            Projection("admin-user", "other-admin"),
            Audit("event-corrective", "GlobalAdministratorSet")));
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        await cut.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        string callerOwnedMessageId = commandGateway.TrackedMessageIds.ShouldHaveSingleItem();
        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.RequestSent);
        cut.Instance.Snapshot.MessageId.ShouldBe(callerOwnedMessageId);
        cut.Instance.Snapshot.CorrelationId.ShouldBeNull();
        cut.Instance.Snapshot.IsSubmissionAmbiguous.ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-refresh']").TextContent
            .ShouldContain("same tracked command", Case.Insensitive);
        Services.GetRequiredService<TenantAggregateCommandAdmissionGate>()
            .IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeTrue();
    }

    [Fact]
    public void AdoptedAmbiguousGrantRendersItsSameCommandDeliveryRecovery()
    {
        var pendingRetry = new TaskCompletionSource<TenantCommandSubmissionResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var commandGateway = new StubTenantCommandGateway { SetResultTask = pendingRetry.Task };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            Projection("other-admin"),
            Audit("event-corrective", "GlobalAdministratorSet")));
        TenantAggregateCommandAdmissionGate gate =
            Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        var originalOwner = new object();
        gate.TryAcquireLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            originalOwner,
            out TenantAggregateCommandLease? lease).ShouldBeTrue();
        lease!.TryMarkDispatched(originalOwner).ShouldBeTrue();
        GlobalAdministratorGrantPreview preview = GlobalAdministratorGrantPreview.Create(
            "admin-user",
            Projection("other-admin"),
            isAuthorized: true);
        lease.TryRetainReconciliation(
            originalOwner,
            new GlobalAdministratorReconciliationState(
                GlobalAdministratorActionKind.Grant,
                "admin-user",
                "message-safe",
                CorrelationId: null,
                TenantCommandLifecycleState.RequestSent,
                preview,
                IsSubmissionAmbiguous: true,
                SafeMessageKey: "Tenants.GlobalAdministrators.Grant.SubmissionEvidence.Ambiguous",
                SafeRecoveryKey: "Tenants.GlobalAdministrators.Grant.DeliveryRetry.Recovery"))
            .ShouldBeTrue();

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut =
            Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
                .Add(component => component.Intent, RestoreIntent())
                .Add(component => component.CurrentProjection, Projection("other-admin")));

        cut.WaitForAssertion(() => commandGateway.SetRequests.ShouldHaveSingleItem());
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent
            .ShouldContain("ambiguous", Case.Insensitive);
        cut.Find("[data-testid='tenants-correction-unavailable-recovery']").TextContent
            .ShouldContain("retained command identity", Case.Insensitive);
        commandGateway.TrackedMessageIds.ShouldHaveSingleItem().ShouldBe("message-safe");
        commandGateway.SetRequests.ShouldHaveSingleItem().UserId.ShouldBe("admin-user");

        GlobalAdministratorCorrectionSnapshot adoptedSnapshot = cut.Instance.Snapshot!;
        pendingRetry.SetResult(TenantCommandSubmissionResult.Ambiguous(
            "message-safe",
            "Tenants.GlobalAdministrators.Grant.SubmissionEvidence.Ambiguous"));
        cut.WaitForAssertion(() => ReferenceEquals(cut.Instance.Snapshot, adoptedSnapshot).ShouldBeFalse());
    }

    [Fact]
    public async Task Superseded_status_completion_is_immediately_adopted_resumed_and_confirmed()
    {
        var pendingStatus = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commandGateway = new StubTenantCommandGateway { StatusResultTask = pendingStatus.Task };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            Projection("admin-user", "other-admin"),
            Audit("event-corrective", "GlobalAdministratorSet")));
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        Task submit = cut.Find("[data-testid='tenants-correction-confirm']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await commandGateway.StatusEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        (Task rendererBlock, TaskCompletionSource releaseRenderer) = await BlockRendererAsync(cut);
        pendingStatus.SetResult(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true));
        for (int iteration = 0; iteration < 20; iteration++)
        {
            await Task.Yield();
        }

        IncrementPrivateGeneration(cut.Instance, "_operationGeneration");
        releaseRenderer.SetResult();
        await Task.WhenAll(rendererBlock, submit).WaitAsync(TimeSpan.FromSeconds(5));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public async Task Superseded_projection_completion_queued_behind_the_renderer_cannot_confirm_correction()
    {
        var projectionEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingProjection = new TaskCompletionSource<GlobalAdministratorsSnapshot?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            Projection("other-admin"),
            Audit("event-corrective", "GlobalAdministratorSet")));
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin"))
            .Add(component => component.ProjectionRefreshProvider, async () =>
            {
                projectionEntered.SetResult();
                return await pendingProjection.Task;
            }));

        Task submit = cut.Find("[data-testid='tenants-correction-confirm']").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await projectionEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        (Task rendererBlock, TaskCompletionSource releaseRenderer) = await BlockRendererAsync(cut);
        pendingProjection.SetResult(Projection("admin-user", "other-admin"));
        for (int iteration = 0; iteration < 20; iteration++)
        {
            await Task.Yield();
        }

        IncrementPrivateGeneration(cut.Instance, "_operationGeneration");
        releaseRenderer.SetResult();
        await Task.WhenAll(rendererBlock, submit).WaitAsync(TimeSpan.FromSeconds(5));

        cut.Instance.Snapshot!.LifecycleState.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Correction_lease_arming_and_component_field_write_are_renderer_guarded()
    {
        string source = File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")),
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Audit",
            "GlobalAdministratorCorrectionPanel.razor"));
        int submitStart = source.IndexOf("private async Task SubmitAsync()", StringComparison.Ordinal);
        int submitEnd = source.IndexOf("private async Task RefreshStatusAsync()", submitStart, StringComparison.Ordinal);
        string submit = source[submitStart..submitEnd];
        int assignment = submit.IndexOf("_admissionLease = acquiredLease", StringComparison.Ordinal);
        int callback = submit.LastIndexOf("await InvokeAsync(() =>", assignment, StringComparison.Ordinal);
        int operationGuard = submit.IndexOf("CanApplyOperation(generation)", callback, StringComparison.Ordinal);
        int dispatchGuard = submit.IndexOf("acquiredLease.TryMarkDispatched(_admissionOwner)", callback, StringComparison.Ordinal);

        submitStart.ShouldBeGreaterThan(-1);
        submitEnd.ShouldBeGreaterThan(submitStart);
        callback.ShouldBeGreaterThan(-1);
        operationGuard.ShouldBeInRange(callback, assignment);
        dispatchGuard.ShouldBeInRange(callback, assignment);
        submit.ShouldContain("_ = acquiredLease.TryAbandonBeforeDispatch(_admissionOwner);");
        submit.ShouldNotContain("out _admissionLease");
    }

    [Fact]
    public void Close_uses_callback_for_focus_return()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        bool closed = false;

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin"))
            .Add(component => component.OnClose, () => closed = true));

        cut.Find("[data-testid='tenants-correction-close']").Click();

        closed.ShouldBeTrue();
    }

    [Fact]
    public void Unavailable_intent_blocks_submission_and_keeps_original_evidence_visible()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        TenantCorrectionStartIntent intent = RestoreIntent(hasCommandSupport: false);

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, intent));

        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.ShouldContain("not connected");
        cut.Find("[data-testid='tenants-correction-original-evidence']").TextContent.ShouldContain("event-safe-reference");
    }

    [Fact]
    public async Task SubmittedCorrectionIsAdoptedAndCompletedByGlobalAdministratorsPage()
    {
        bool commandApplied = false;
        var commandGateway = new StubTenantCommandGateway
        {
            Status = TenantCommandStatusResult.Unknown("Status is temporarily unavailable."),
        };
        var queryGateway = new StubTenantQueryGateway(Projection("other-admin"), Audit("proof", "GlobalAdministratorSet"))
        {
            GlobalAdministratorProvider = _ => commandApplied
                ? Projection("admin-user", "other-admin") with { ProjectionVersion = "ga-v2" }
                : Projection("other-admin"),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();

        IRenderedComponent<GlobalAdministratorCorrectionPanel> correction = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));
        correction.Find("[data-testid='tenants-correction-confirm']").Click();
        correction.WaitForAssertion(() => correction.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        correction.Instance.Dispose();
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeTrue();

        commandGateway.Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true);
        commandApplied = true;
        IRenderedComponent<GlobalAdministratorsPage> page = Render<GlobalAdministratorsPage>();

        page.WaitForAssertion(() => gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse());
        page.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain("Confirmed");
        commandGateway.SetRequests.ShouldHaveSingleItem().UserId.ShouldBe("admin-user");
    }

    [Fact]
    public void RendererReplacementRetainsPositiveGrantEventEvidenceAndReleasesOnQualifiedProjection()
    {
        var commandGateway = new StubTenantCommandGateway
        {
            Status = new TenantCommandStatusResult(
                CommandStatus.Received,
                EventCount: 0,
                HasVerifiedCommandIdentity: true),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            ProjectionAfterGrant("admin-user", "other-admin"),
            Audit("proof", "GlobalAdministratorSet")));
        TenantAggregateCommandAdmissionGate gate =
            Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        var originalOwner = new object();
        gate.TryAcquireLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            originalOwner,
            out TenantAggregateCommandLease? lease).ShouldBeTrue();
        lease!.TryMarkDispatched(originalOwner).ShouldBeTrue();
        GlobalAdministratorGrantPreview preview = GlobalAdministratorGrantPreview.Create(
            "admin-user",
            Projection("other-admin"),
            isAuthorized: true);
        lease.TryRetainReconciliation(
            originalOwner,
            new GlobalAdministratorReconciliationState(
                GlobalAdministratorActionKind.Grant,
                "admin-user",
                "message-safe",
                "tracking-safe",
                TenantCommandLifecycleState.ProjectionPending,
                preview,
                HasCommandEventEvidence: true)).ShouldBeTrue();

        IRenderedComponent<GlobalAdministratorCorrectionPanel> replacement =
            Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
                .Add(component => component.Intent, RestoreIntent())
                .Add(component => component.CurrentProjection, Projection("other-admin")));

        replacement.WaitForAssertion(() => replacement.Instance.Snapshot!.LifecycleState
            .ShouldBe(TenantCommandLifecycleState.Confirmed));
        replacement.Instance.Snapshot!.HasCommandEventEvidence.ShouldBeTrue();
        commandGateway.StatusHandles.ShouldHaveSingleItem().MessageId.ShouldBe("message-safe");
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse();
    }

    [Fact]
    public async Task SubmittedPageRemovalIsAdoptedAndCompletedByCorrectionPanel()
    {
        bool commandApplied = false;
        var commandGateway = new StubTenantCommandGateway
        {
            Status = TenantCommandStatusResult.Unknown("Status is temporarily unavailable."),
        };
        var queryGateway = new StubTenantQueryGateway(
            Projection("admin-user", "other-admin"),
            Audit("proof", "GlobalAdministratorRemoved"))
        {
            GlobalAdministratorProvider = _ => commandApplied
                ? Projection("other-admin")
                : Projection("admin-user", "other-admin"),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();

        IRenderedComponent<GlobalAdministratorsPage> page = Render<GlobalAdministratorsPage>();
        page.Find("[data-testid='tenants-global-admin-remove']").Click();
        page.Find("[data-testid='tenants-global-admin-remove-submit']").Click();
        page.WaitForAssertion(() => page.Find("[data-testid='tenants-global-admin-remove-state']").TextContent.ShouldContain("UnableToVerify"));
        await page.InvokeAsync(async () => await page.Instance.DisposeAsync());
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeTrue();

        commandGateway.Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true);
        commandApplied = true;
        IRenderedComponent<GlobalAdministratorCorrectionPanel> correction = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RevokeIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        correction.WaitForAssertion(() => correction.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse();
        commandGateway.RemoveRequests.ShouldHaveSingleItem().UserId.ShouldBe("admin-user");
    }

    [Theory]
    [InlineData("grant")]
    [InlineData("remove")]
    public void UnavailableQueryGatewayBlocksBothCorrectionActionsWithoutDispatch(string action)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        var commandGateway = new StubTenantCommandGateway();
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(new UnavailableTenantQueryGateway());
        TenantCorrectionStartIntent intent = action == "remove" ? RevokeIntent() : RestoreIntent();

        GlobalAdministratorsSnapshot projection = action == "remove"
            ? Projection("admin-user", "other-admin")
            : Projection("other-admin");
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, projection));

        AngleSharp.Dom.IElement confirm = cut.Find("[data-testid='tenants-correction-confirm']");
        confirm.HasAttribute("disabled").ShouldBeTrue();
        confirm.GetAttribute("aria-describedby").ShouldBe(
            "tenants-correction-unavailable tenants-correction-unavailable-recovery");
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent
            .ShouldContain("requery support is missing", Case.Insensitive);
        cut.Find("[data-testid='tenants-correction-unavailable-recovery']").TextContent
            .ShouldContain("Restore dispatch, status, and requery support", Case.Insensitive);
        commandGateway.SetRequests.ShouldBeEmpty();
        commandGateway.RemoveRequests.ShouldBeEmpty();
        commandGateway.StatusHandles.ShouldBeEmpty();
    }

    [Fact]
    public void CorrectiveProofFailurePreservesConfirmedProjectionTruth()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        var commandGateway = new StubTenantCommandGateway();
        var queryGateway = new StubTenantQueryGateway(
            Projection("admin-user", "other-admin"),
            Audit("unused", "GlobalAdministratorSet"))
        {
            AuditException = new HttpRequestException("audit transport detail"),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.Instance.Snapshot!.AuditState.ShouldBe(TenantCommandAuditState.AuditDelayed);
        cut.Find("[data-testid='tenants-correction-state']").TextContent
            .ShouldContain("Projection confirms", Case.Insensitive);
        cut.Markup.ShouldNotContain("audit transport detail", Case.Insensitive);
    }

    [Theory]
    [InlineData("grant", "gateway-dispatch")]
    [InlineData("grant", "gateway-status")]
    [InlineData("grant", "composition-dispatch")]
    [InlineData("grant", "composition-status")]
    [InlineData("grant", "composition-requery")]
    [InlineData("remove", "gateway-dispatch")]
    [InlineData("remove", "gateway-status")]
    [InlineData("remove", "composition-dispatch")]
    [InlineData("remove", "composition-status")]
    [InlineData("remove", "composition-requery")]
    public void EachDeclaredLifecycleSeamBlocksBothActionsIndependently(string action, string missingSeam)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        var commandGateway = new StubTenantCommandGateway
        {
            SupportsGlobalAdministratorDispatch = missingSeam != "gateway-dispatch",
            SupportsCommandStatusLookup = missingSeam != "gateway-status",
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition
        {
            IsGlobalAdministratorDispatchConnected = missingSeam != "composition-dispatch",
            IsGlobalAdministratorStatusConnected = missingSeam != "composition-status",
            IsGlobalAdministratorRequeryConnected = missingSeam != "composition-requery",
        });
        TenantCorrectionStartIntent intent = action == "remove" ? RevokeIntent() : RestoreIntent();
        GlobalAdministratorsSnapshot projection = action == "remove"
            ? Projection("admin-user", "other-admin")
            : Projection("other-admin");

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, projection));

        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent
            .ShouldContain("support is missing", Case.Insensitive);
        commandGateway.SetRequests.ShouldBeEmpty();
        commandGateway.RemoveRequests.ShouldBeEmpty();
        commandGateway.StatusHandles.ShouldBeEmpty();
    }

    [Fact]
    public void MismatchingCorrectionLeavesRetainedAttemptForMatchingReplacement()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        var commandGateway = new StubTenantCommandGateway();
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        object originalOwner = new();
        gate.TryAcquireLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            originalOwner,
            out TenantAggregateCommandLease? lease).ShouldBeTrue();
        lease.ShouldNotBeNull();
        lease.TryMarkDispatched(originalOwner).ShouldBeTrue();
        lease.TryRetainReconciliation(
            originalOwner,
            new(
                GlobalAdministratorActionKind.Remove,
                "admin-user",
                "message-safe",
                "tracking-safe",
                TenantCommandLifecycleState.Accepted)).ShouldBeTrue();

        IRenderedComponent<GlobalAdministratorCorrectionPanel> mismatch = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));
        commandGateway.StatusHandles.ShouldBeEmpty();
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeTrue();
        mismatch.Dispose();

        IRenderedComponent<GlobalAdministratorCorrectionPanel> match = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RevokeIntent())
            .Add(component => component.CurrentProjection, Projection("admin-user", "other-admin")));

        match.WaitForAssertion(() => match.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        commandGateway.StatusHandles.ShouldHaveSingleItem().ShouldBe(
            new TenantCommandTrackingHandle(
                "message-safe",
                "tracking-safe",
                GlobalAdministratorGrantPreview.FixedAggregateId));
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse();
    }

    [Theory]
    [InlineData("viewport")]
    [InlineData("gateway-dispatch")]
    [InlineData("gateway-status")]
    [InlineData("composition-dispatch")]
    [InlineData("composition-status")]
    [InlineData("composition-requery")]
    public async Task StatusGateChangePreventsProjectionIoAndLeavesAttemptAdoptable(string changedGate)
    {
        var pendingStatus = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commandGateway = new StubTenantCommandGateway { StatusResultTask = pendingStatus.Task };
        var composition = new StubTenantsBffComposition();
        var queryGateway = new StubTenantQueryGateway(
            Projection("admin-user", "other-admin"),
            Audit("proof", "GlobalAdministratorSet"));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        TenantHighImpactViewportObservation viewport = Services.GetRequiredService<TenantHighImpactViewportObservation>();
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        Task submit = cut.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await commandGateway.StatusEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        SetLiveGate(changedGate, enabled: false, commandGateway, composition, viewport);
        pendingStatus.SetResult(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true));
        await submit.WaitAsync(TimeSpan.FromSeconds(5));

        queryGateway.GlobalAdminRequests.ShouldBeEmpty();
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeTrue();
        cut.Instance.Dispose();

        SetLiveGate(changedGate, enabled: true, commandGateway, composition, viewport);
        commandGateway.StatusResultTask = null;
        IRenderedComponent<GlobalAdministratorCorrectionPanel> replacement = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));
        replacement.WaitForAssertion(() => replacement.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse();
    }

    [Theory]
    [InlineData("viewport")]
    [InlineData("gateway-dispatch")]
    [InlineData("gateway-status")]
    [InlineData("composition-dispatch")]
    [InlineData("composition-status")]
    [InlineData("composition-requery")]
    public void LiveGateChangeAfterOuterSubmitCheckPreventsDispatch(string changedGate)
    {
        var commandGateway = new StubTenantCommandGateway();
        var composition = new StubTenantsBffComposition();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantsBffComposition>(composition);
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        TenantHighImpactViewportObservation viewport = Services.GetRequiredService<TenantHighImpactViewportObservation>();
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse();

        bool changed = false;
        EventHandler changeAfterAcquisition = (_, _) =>
        {
            if (!changed)
            {
                changed = true;
                SetLiveGate(changedGate, enabled: false, commandGateway, composition, viewport);
            }
        };
        gate.StateChanged += changeAfterAcquisition;
        try
        {
            cut.Find("[data-testid='tenants-correction-confirm']").Click();
        }
        finally
        {
            gate.StateChanged -= changeAfterAcquisition;
        }

        changed.ShouldBeTrue();
        commandGateway.SetRequests.ShouldBeEmpty();
        commandGateway.RemoveRequests.ShouldBeEmpty();
        commandGateway.StatusHandles.ShouldBeEmpty();
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse();
    }

    [Fact]
    public void RetainedAttemptWaitsForConcreteRequeryAndRetriesAdoptionAfterSupportRestoration()
    {
        var commandGateway = new StubTenantCommandGateway();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(new UnavailableTenantQueryGateway());
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        SeedRetainedAttempt(gate, GlobalAdministratorActionKind.Grant, "admin-user");

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        commandGateway.StatusHandles.ShouldBeEmpty();
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeTrue();

        cut.Render(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin"))
            .Add(component => component.ProjectionRefreshProvider, () =>
                Task.FromResult<GlobalAdministratorsSnapshot?>(ProjectionAfterGrant("admin-user", "other-admin"))));

        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        commandGateway.StatusHandles.ShouldHaveSingleItem();
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("changed")]
    public async Task NullOrChangedIntentReturnsAcceptedTrackingToMatchingReplacement(string intentChange)
    {
        var pendingStatus = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commandGateway = new StubTenantCommandGateway { StatusResultTask = pendingStatus.Task };
        var queryGateway = new StubTenantQueryGateway(
            Projection("admin-user", "other-admin"),
            Audit("proof", "GlobalAdministratorSet"));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        Task submit = cut.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await commandGateway.StatusEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cut.Render(parameters => parameters
            .Add(component => component.Intent, intentChange == "null" ? null : RevokeIntent())
            .Add(component => component.CurrentProjection, Projection("admin-user", "other-admin")));
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeTrue();

        pendingStatus.SetResult(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true));
        await submit.WaitAsync(TimeSpan.FromSeconds(5));
        queryGateway.GlobalAdminRequests.ShouldBeEmpty();
        cut.Dispose();

        commandGateway.StatusResultTask = null;
        IRenderedComponent<GlobalAdministratorCorrectionPanel> replacement = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));
        replacement.WaitForAssertion(() => replacement.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse();
    }

    [Fact]
    public void FailingAuthenticationCannotClaimRetainedAttemptAndAuthorizedReplacementCompletesIt()
    {
        var commandGateway = new StubTenantCommandGateway();
        var authentication = new StubAuthenticationStateProvider
        {
            AuthenticationStateTask = Task.FromException<AuthenticationState>(new InvalidOperationException("auth detail")),
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            Projection("admin-user", "other-admin"),
            Audit("proof", "GlobalAdministratorSet")));
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        SeedRetainedAttempt(gate, GlobalAdministratorActionKind.Grant, "admin-user");

        IRenderedComponent<GlobalAdministratorCorrectionPanel> hidden = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));
        hidden.FindAll("[data-testid='tenants-correction-panel']").ShouldBeEmpty();
        commandGateway.StatusHandles.ShouldBeEmpty();
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeTrue();
        hidden.Dispose();

        authentication.AuthenticationStateTask = Task.FromResult(StubAuthenticationStateProvider.AuthenticatedState());
        IRenderedComponent<GlobalAdministratorCorrectionPanel> replacement = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));
        replacement.WaitForAssertion(() => replacement.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse();
        replacement.Markup.ShouldNotContain("auth detail", Case.Insensitive);
    }

    [Fact]
    public async Task SupersededAuthorizationCompletionCannotOverwriteNewerAuthorizedState()
    {
        var firstAuthorization = new TaskCompletionSource<TenantLifecycleAuthorizationReflectionState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondAuthorization = new TaskCompletionSource<TenantLifecycleAuthorizationReflectionState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int resolution = 0;
        var composition = new StubTenantsBffComposition
        {
            AuthorizationResolver = _ => Interlocked.Increment(ref resolution) switch
            {
                1 => ValueTask.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized),
                2 => AwaitAuthorization(firstAuthorization.Task, firstEntered),
                3 => AwaitAuthorization(secondAuthorization.Task, secondEntered),
                _ => ValueTask.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized),
            },
        };
        var authentication = new StubAuthenticationStateProvider();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));
        cut.Find("[data-testid='tenants-correction-panel']");

        authentication.Publish(Task.FromResult(StubAuthenticationStateProvider.AuthenticatedState()));
        await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='tenants-correction-panel']").ShouldBeEmpty());
        authentication.Publish(Task.FromResult(StubAuthenticationStateProvider.AuthenticatedState()));
        await secondEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        secondAuthorization.SetResult(TenantLifecycleAuthorizationReflectionState.Authorized);
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-correction-panel']"));
        firstAuthorization.SetResult(TenantLifecycleAuthorizationReflectionState.Indeterminate);
        for (int iteration = 0; iteration < 20; iteration++)
        {
            await Task.Yield();
        }

        cut.Find("[data-testid='tenants-correction-panel']");
        cut.Find("[data-testid='tenants-correction-confirm']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Theory]
    [InlineData("already-applied", TenantCommandLifecycleState.UnableToVerify, "Keep the confirmed rows")]
    [InlineData("confirmed", TenantCommandLifecycleState.Confirmed, "Keep the confirmed projection")]
    [InlineData("rejected", TenantCommandLifecycleState.Rejected, "Review the rejection")]
    [InlineData("failed", TenantCommandLifecycleState.Failed, "Refresh current evidence")]
    [InlineData("unavailable", TenantCommandLifecycleState.UnableToVerify, "Restore the blocked authorization")]
    public void DisabledLifecycleStatesRenderTruthfulAssociatedReasonAndRecovery(
        string scenario,
        TenantCommandLifecycleState expectedState,
        string expectedRecovery)
    {
        var commandGateway = new StubTenantCommandGateway
        {
            SetResultTask = scenario switch
            {
                "rejected" => Task.FromResult(TenantCommandSubmissionResult.Rejected("Rejected safely.", "RejectedForTest")),
                "failed" => Task.FromResult(TenantCommandSubmissionResult.Failed("Failed safely.")),
                _ => null,
            },
        };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            Projection("admin-user", "other-admin"),
            Audit("proof", "GlobalAdministratorSet")));
        TenantCorrectionStartIntent intent = scenario == "unavailable" ? RestoreIntent(hasCommandSupport: false) : RestoreIntent();
        GlobalAdministratorsSnapshot projection = scenario == "already-applied"
            ? Projection("admin-user", "other-admin")
            : Projection("other-admin");
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, projection));

        if (scenario is "confirmed" or "rejected" or "failed")
        {
            cut.Find("[data-testid='tenants-correction-confirm']").Click();
            cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(expectedState));
        }
        else
        {
            cut.Instance.Snapshot!.LifecycleState.ShouldBe(expectedState);
        }

        AngleSharp.Dom.IElement confirm = cut.Find("[data-testid='tenants-correction-confirm']");
        confirm.HasAttribute("disabled").ShouldBeTrue();
        confirm.GetAttribute("aria-describedby").ShouldBe(
            "tenants-correction-unavailable tenants-correction-unavailable-recovery");
        cut.Find("[data-testid='tenants-correction-unavailable-reason']").TextContent.ShouldNotBeNullOrWhiteSpace();
        cut.Find("[data-testid='tenants-correction-unavailable-recovery']").TextContent.ShouldContain(expectedRecovery);
    }

    [Fact]
    public async Task TrackedRefreshRefusalRendersAssociatedLiveRecovery()
    {
        var pendingStatus = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commandGateway = new StubTenantCommandGateway { StatusResultTask = pendingStatus.Task };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantHighImpactViewportObservation viewport = Services.GetRequiredService<TenantHighImpactViewportObservation>();
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        Task submit = cut.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await commandGateway.StatusEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        viewport.Observe(ViewportTier.Phone);

        cut.WaitForAssertion(() =>
        {
            AngleSharp.Dom.IElement refresh = cut.Find("[data-testid='tenants-correction-refresh']");
            refresh.HasAttribute("disabled").ShouldBeTrue();
            refresh.GetAttribute("aria-describedby").ShouldBe(
                "tenants-correction-unavailable tenants-correction-unavailable-recovery");
            cut.Find("[data-testid='tenants-correction-unavailable-recovery']").TextContent.ShouldNotBeNullOrWhiteSpace();
        });

        cut.Dispose();
        pendingStatus.SetResult(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true));
        await submit.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("close")]
    [InlineData("cancel")]
    [InlineData("escape")]
    public async Task NonterminalCloseInteractionsRemainLockedAndReplacementCanReconcile(string interaction)
    {
        var pendingStatus = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commandGateway = new StubTenantCommandGateway { StatusResultTask = pendingStatus.Task };
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            Projection("admin-user", "other-admin"),
            Audit("proof", "GlobalAdministratorSet")));
        TenantAggregateCommandAdmissionGate gate = Services.GetRequiredService<TenantAggregateCommandAdmissionGate>();
        bool closed = false;
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin"))
            .Add(component => component.OnClose, () => closed = true));

        Task submit = cut.Find("[data-testid='tenants-correction-confirm']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await commandGateway.StatusEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        if (interaction == "escape")
        {
            cut.Find("[data-testid='tenants-correction-panel']")
                .KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        }
        else
        {
            cut.Find($"[data-testid='tenants-correction-{interaction}']").Click();
        }

        closed.ShouldBeFalse();
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeTrue();
        cut.Instance.Dispose();
        IRenderedComponent<GlobalAdministratorCorrectionPanel> replacement = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, RestoreIntent())
            .Add(component => component.CurrentProjection, Projection("other-admin")));
        pendingStatus.SetResult(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true));
        await submit.WaitAsync(TimeSpan.FromSeconds(5));

        replacement.WaitForAssertion(() => replacement.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.Confirmed));
        gate.IsLocked(TenantCommandAggregateLock.ForGlobalAdministrators()).ShouldBeFalse();
    }

    private static async ValueTask<TenantLifecycleAuthorizationReflectionState> AwaitAuthorization(
        Task<TenantLifecycleAuthorizationReflectionState> authorization,
        TaskCompletionSource entered)
    {
        entered.TrySetResult();
        return await authorization.ConfigureAwait(false);
    }

    private static void SeedRetainedAttempt(
        TenantAggregateCommandAdmissionGate gate,
        GlobalAdministratorActionKind actionKind,
        string targetUserId)
    {
        object owner = new();
        gate.TryAcquireLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            owner,
            out TenantAggregateCommandLease? lease).ShouldBeTrue();
        lease.ShouldNotBeNull();
        lease.TryMarkDispatched(owner).ShouldBeTrue();
        GlobalAdministratorGrantPreview? preview = actionKind is GlobalAdministratorActionKind.Grant
            ? GlobalAdministratorGrantPreview.Create(
                targetUserId,
                Projection("other-admin"),
                isAuthorized: true)
            : null;
        lease.TryRetainReconciliation(
            owner,
            new(
                actionKind,
                targetUserId,
                "message-safe",
                "tracking-safe",
                TenantCommandLifecycleState.Accepted,
                preview)).ShouldBeTrue();
    }

    private static void SetLiveGate(
        string gate,
        bool enabled,
        StubTenantCommandGateway commandGateway,
        StubTenantsBffComposition composition,
        TenantHighImpactViewportObservation viewport)
    {
        switch (gate)
        {
            case "viewport":
                viewport.Observe(enabled ? ViewportTier.Desktop : ViewportTier.Phone);
                break;
            case "gateway-dispatch":
                commandGateway.SupportsGlobalAdministratorDispatch = enabled;
                break;
            case "gateway-status":
                commandGateway.SupportsCommandStatusLookup = enabled;
                break;
            case "composition-dispatch":
                composition.IsGlobalAdministratorDispatchConnected = enabled;
                break;
            case "composition-status":
                composition.IsGlobalAdministratorStatusConnected = enabled;
                break;
            case "composition-requery":
                composition.IsGlobalAdministratorRequeryConnected = enabled;
                break;
            default:
                throw new InvalidOperationException($"Unknown live gate '{gate}'.");
        }
    }

    private static TenantCorrectionStartIntent RestoreIntent(bool hasCommandSupport = true)
        => TenantCorrectionStartIntent.Evaluate(Context("GlobalAdministratorRemoved", hasCommandSupport));

    private static TenantCorrectionStartIntent RevokeIntent(bool hasCommandSupport = true)
        => TenantCorrectionStartIntent.Evaluate(Context("GlobalAdministratorSet", hasCommandSupport));

    private static TenantCorrectionStartIntent WithOriginalTimestamp(TenantCorrectionStartIntent intent, string value)
    {
        Dictionary<string, string> inputs = new(intent.RequiredPreviewInputs, StringComparer.Ordinal)
        {
            ["originalTimestamp"] = value,
        };

        return intent with { RequiredPreviewInputs = inputs };
    }

    private static TenantCorrectionStartContext Context(string eventType, bool hasCommandSupport)
        => new(
            TenantAuditReceipt.FromRow(Row(eventType)),
            Row(eventType),
            IsAuthorized: true,
            HasCurrentProjectionSnapshot: true,
            CurrentProjectionSnapshotReference: "Current global administrator projection is available.",
            HasTenantCommandSupport: false,
            HasGlobalAdministratorCommandSupport: hasCommandSupport);

    private static TenantAuditRow Row(string eventType)
        => new(
            "event-safe-reference",
            eventType,
            AuditEventCategory.Administrative,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:00:00Z", CultureInfo.InvariantCulture),
            "system",
            "admin-user",
            "global-administrators",
            eventType,
            "userId: admin-user",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            QueryResponseProvenance.ProjectionBacked);

    private static GlobalAdministratorsSnapshot Projection(params string[] userIds)
        => ProjectionPage(userIds, nextCursor: null, hasMore: false) with
        {
            IsCompleteEvidence = true,
        };

    private static GlobalAdministratorsSnapshot ProjectionAfterGrant(params string[] userIds)
        => Projection(userIds) with { ProjectionVersion = "ga-v2" };

    private static GlobalAdministratorsSnapshot ProjectionPage(
        IReadOnlyList<string> userIds,
        string? nextCursor,
        bool hasMore)
        => GlobalAdministratorsSnapshot.Ready(
            userIds.Select(userId => new GlobalAdministratorRow(
                userId,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current)).ToArray(),
            nextCursor,
            hasMore,
            eTag: "\"ga-etag\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            ProjectionVersion = "ga-v1",
        };

    private static TenantAuditSnapshot Audit(string eventReference, string eventType)
        => TenantAuditSnapshot.Ready(
            [CorrectiveRow(eventReference, eventType)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"audit-etag\"",
            freshness: ReadModelFreshnessState.Current,
            request: new TenantAuditRequest("system"));

    private static TenantAuditRow CorrectiveRow(string eventReference, string eventType)
        => new(
            eventReference,
            eventType,
            AuditEventCategory.Administrative,
            "actor-user",
            DateTimeOffset.Parse("2026-06-01T10:05:00Z", CultureInfo.InvariantCulture),
            "system",
            "admin-user",
            "global-administrators",
            eventType,
            "userId: admin-user",
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            QueryResponseProvenance.ProjectionBacked);

    private static void IncrementPrivateGeneration(GlobalAdministratorCorrectionPanel instance, string name)
    {
        System.Reflection.FieldInfo field = typeof(GlobalAdministratorCorrectionPanel)
            .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        field.SetValue(instance, (long)field.GetValue(instance)! + 1);
    }

    private static async Task<(Task RendererBlock, TaskCompletionSource ReleaseRenderer)> BlockRendererAsync(
        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task block = Task.Run(() => cut.InvokeAsync(() =>
        {
            entered.SetResult();
            release.Task.GetAwaiter().GetResult();
        }));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        return (block, release);
    }

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public bool SupportsGlobalAdministratorDispatch { get; set; } = true;

        public bool SupportsTrackedGlobalAdministratorDispatch { get; set; } = true;

        public bool SupportsCommandStatusLookup { get; set; } = true;

        public List<SetGlobalAdministrator> SetRequests { get; } = [];

        public List<string> TrackedMessageIds { get; } = [];

        public List<RemoveGlobalAdministrator> RemoveRequests { get; } = [];

        public List<TenantCommandTrackingHandle> StatusHandles { get; } = [];

        public Task<TenantCommandSubmissionResult>? SetResultTask { get; set; }

        public bool ReturnConfiguredTrackedIdentity { get; init; }

        public Task<TenantCommandSubmissionResult>? RemoveResultTask { get; init; }

        public TenantCommandStatusResult Status { get; set; }
            = new(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true);

        public Task<TenantCommandStatusResult>? StatusResultTask { get; set; }

        public TaskCompletionSource StatusEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<TenantCommandSubmissionResult> SetGlobalAdministratorAsync(
            SetGlobalAdministrator request,
            CancellationToken cancellationToken = default)
        {
            SetRequests.Add(request);
            return SetResultTask ?? Task.FromResult(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"));
        }

        public async Task<TenantCommandSubmissionResult> SetGlobalAdministratorTrackedAsync(
            SetGlobalAdministrator request,
            string messageId,
            CancellationToken cancellationToken = default)
        {
            SetRequests.Add(request);
            TrackedMessageIds.Add(messageId);
            TenantCommandSubmissionResult result = SetResultTask is null
                ? TenantCommandSubmissionResult.Accepted(messageId, "tracking-safe")
                : await SetResultTask.ConfigureAwait(false);
            return ReturnConfiguredTrackedIdentity ? result : result with { MessageId = messageId };
        }

        public Task<TenantCommandSubmissionResult> RemoveGlobalAdministratorAsync(
            RemoveGlobalAdministrator request,
            CancellationToken cancellationToken = default)
        {
            RemoveRequests.Add(request);
            return RemoveResultTask ?? Task.FromResult(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"));
        }

        public async Task<TenantCommandStatusResult> GetStatusAsync(
            TenantCommandTrackingHandle handle,
            CancellationToken cancellationToken = default)
        {
            StatusHandles.Add(handle);
            if (StatusResultTask is not null)
            {
                _ = StatusEntered.TrySetResult();
                return await StatusResultTask.ConfigureAwait(false);
            }

            return Status;
        }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, string? messageId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubAuthenticationStateProvider : AuthenticationStateProvider
    {
        public Task<AuthenticationState> AuthenticationStateTask { get; set; }
            = Task.FromResult(AuthenticatedState());

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => AuthenticationStateTask;

        public void Publish(Task<AuthenticationState> authenticationStateTask)
        {
            AuthenticationStateTask = authenticationStateTask;
            NotifyAuthenticationStateChanged(authenticationStateTask);
        }

        public static AuthenticationState AuthenticatedState()
            => new(new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "operator")], "test")));
    }

    private sealed class StubTenantsBffComposition : ITenantsBffComposition
    {
        public bool IsReadSurfaceConnected => true;

        public bool IsCommandSurfaceConnected => true;

        public bool IsGlobalAdministratorDispatchConnected { get; set; } = true;

        public bool IsGlobalAdministratorStatusConnected { get; set; } = true;

        public bool IsGlobalAdministratorRequeryConnected { get; set; } = true;

        public bool IsGlobalAdministratorGrantPreviewReady => true;

        public bool IsGlobalAdministratorRemovePreviewReady => true;

        public TenantLifecycleAuthorizationReflectionState SynchronousReflection { get; init; }
            = TenantLifecycleAuthorizationReflectionState.Authorized;

        public Task<TenantLifecycleAuthorizationReflectionState>? AuthorizationTask { get; init; }

        public Func<CancellationToken, ValueTask<TenantLifecycleAuthorizationReflectionState>>? AuthorizationResolver { get; init; }

        public TenantLifecycleAuthorizationReflectionState GlobalAdministratorsAuthorizationReflection
            => SynchronousReflection;

        public ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveGlobalAdministratorsAuthorizationAsync(
            CancellationToken cancellationToken = default)
            => AuthorizationResolver is not null
                ? AuthorizationResolver(cancellationToken)
                : AuthorizationTask is null
                ? ValueTask.FromResult(TenantLifecycleAuthorizationReflectionState.Authorized)
                : new ValueTask<TenantLifecycleAuthorizationReflectionState>(AuthorizationTask);
    }

    private sealed class StubTenantQueryGateway(GlobalAdministratorsSnapshot projection, TenantAuditSnapshot audit) : ITenantQueryGateway
    {
        /// <summary>
        /// Explicit because <c>ITenantQueryGateway.GetTenantUsersAsync</c> is no longer a default interface
        /// method. This stub returns <c>Unavailable</c> deliberately: these tests do not exercise the member
        /// read, and an unavailable member surface is the correct fail-closed shape for them. Note this is
        /// the same value the removed default interface method returned, so a member-read regression is NOT
        /// caught here -- it is caught by the member-specific suites in
        /// <c>TenantDetailSurfaceTests</c>. (An earlier version of this remark claimed the opposite.)
        /// </summary>
        public Task<TenantUsersSnapshot> GetTenantUsersAsync(
            TenantUsersRequest request,
            TenantUsersSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Task.FromResult(TenantUsersSnapshot.Unavailable(request.TenantId));
        }

        public List<GlobalAdministratorsRequest> GlobalAdminRequests { get; } = [];

        public List<TenantAuditRequest> AuditRequests { get; } = [];

        public Func<GlobalAdministratorsRequest, GlobalAdministratorsSnapshot>? GlobalAdministratorProvider { get; init; }

        public Exception? AuditException { get; init; }

        public Task<GlobalAdministratorsSnapshot> GetGlobalAdministratorsAsync(
            GlobalAdministratorsRequest request,
            GlobalAdministratorsSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            GlobalAdminRequests.Add(request);
            GlobalAdministratorsSnapshot result = GlobalAdministratorProvider?.Invoke(request) ?? projection;
            return Task.FromResult(result with
            {
                RequestCursor = request.Cursor,
                RequestPageSize = request.PageSize,
                ProjectionVersion = "ga-v2",
            });
        }

        public Task<TenantAuditSnapshot> GetTenantAuditAsync(
            TenantAuditRequest request,
            TenantAuditSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            AuditRequests.Add(request);
            if (AuditException is not null)
            {
                return Task.FromException<TenantAuditSnapshot>(AuditException);
            }

            return Task.FromResult(audit);
        }

        public Task<TenantDetailSnapshot> GetTenantAsync(TenantDetailRequest request, TenantDetailSnapshot? previous, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantListSnapshot> ListTenantsAsync(TenantListRequest request, TenantListSnapshot? previous, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserTenantMembershipSnapshot> GetMyTenantsAsync(UserTenantMembershipRequest request, UserTenantMembershipSnapshot? previous, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserTenantMembershipSnapshot> GetUserTenantsAsync(UserTenantMembershipRequest request, UserTenantMembershipSnapshot? previous, CancellationToken cancellationToken = default)
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
            ["Tenants.Correction.Title"] = "Start correction",
            ["Tenants.Correction.Close"] = "Close correction start",
            ["Tenants.Correction.Confirm.Submit"] = "Submit corrective command",
            ["Tenants.Correction.Confirm.Refresh"] = "Refresh status",
            ["Tenants.GlobalAdministrators.Grant.DeliveryRetry"] = "Retry delivery with the same tracked command",
            ["Tenants.Correction.Confirm.Cancel"] = "Cancel",
            ["Tenants.Correction.Field.OriginalEvidence"] = "Original evidence",
            ["Tenants.Correction.Field.Domain"] = "Command domain",
            ["Tenants.Correction.Field.Command"] = "Intended command",
            ["Tenants.Correction.Field.PreviewData"] = "Required preview data",
            ["Tenants.Correction.Lifecycle.Title"] = "Correction lifecycle",
            ["Tenants.Correction.Domain.GlobalAdministrators"] = "Global administrators",
            ["Tenants.Correction.Command.SetGlobalAdministrator"] = "Set global administrator",
            ["Tenants.Correction.Command.RemoveGlobalAdministrator"] = "Remove global administrator",
            ["Tenants.Correction.PreviewInput.userId"] = "User",
            ["Tenants.Correction.GlobalAdmin.Preview.Scope"] = "Platform authority scope",
            ["Tenants.Correction.GlobalAdmin.Preview.Scope.Value"] = "system / global-administrators / global-administrators",
            ["Tenants.Correction.GlobalAdmin.Preview.AdminCount"] = "Current global administrator count",
            ["Tenants.Correction.GlobalAdmin.Preview.CurrentState"] = "Target in current projection",
            ["Tenants.Correction.GlobalAdmin.Preview.CurrentState.Present"] = "Present in the current platform authority projection.",
            ["Tenants.Correction.GlobalAdmin.Preview.CurrentState.Absent"] = "Absent from the current platform authority projection.",
            ["Tenants.Correction.GlobalAdmin.Preview.LastAdminImpact"] = "Last-administrator impact",
            ["Tenants.Correction.GlobalAdmin.Preview.LastAdminImpact.Value"] = "The last global administrator cannot be removed; at least one global administrator must remain.",
            ["Tenants.Correction.GlobalAdmin.Preview.Consequences"] = "Known consequences",
            ["Tenants.Correction.GlobalAdmin.Preview.Consequence.Restore"] = "A new platform authority grant event may be appended when the fixed projection confirms the target is absent.",
            ["Tenants.Correction.GlobalAdmin.Preview.Consequence.Revoke"] = "A new platform authority removal event may be appended when the fixed projection confirms the target is present.",
            ["Tenants.Correction.GlobalAdmin.Preview.Unknowns"] = "Known unknowns",
            ["Tenants.Correction.GlobalAdmin.Preview.Unknowns.Value"] = "Status lookup and live notifications can prompt a re-query but never prove a platform authority change without fixed projection truth.",
            ["Tenants.Correction.GlobalAdmin.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.Correction.GlobalAdmin.Preview.AuditExpectation.Value"] = "Corrective system-scope audit evidence is expected after the command is accepted and the fixed projection confirms the intended state.",
            ["Tenants.Correction.GlobalAdmin.Preview.RecoveryPath"] = "Recovery path",
            ["Tenants.Correction.GlobalAdmin.Preview.RecoveryPath.Value"] = "Retry status lookup, inspect audit, continue read-only, or escalate using support-safe references.",
            ["Tenants.Correction.GlobalAdmin.AlreadyGranted"] = "The current platform authority projection already shows this user as a global administrator.",
            ["Tenants.Correction.GlobalAdmin.AlreadyRemoved"] = "The current platform authority projection already shows this user is not a global administrator.",
            ["Tenants.Correction.GlobalAdmin.LastAdministrator"] = "The last global administrator cannot be removed. Keep the current projection visible and add another global administrator before starting this correction.",
            ["Tenants.Correction.GlobalAdmin.State.AlreadyApplied"] = "The fixed projection already reflects the intended platform authority state; no correction success is asserted.",
            ["Tenants.Correction.GlobalAdmin.State.UnableToVerify"] = "The platform authority correction cannot be verified from current evidence.",
            ["Tenants.Correction.State.Previewed"] = "Preview is ready for deliberate confirmation.",
            ["Tenants.Correction.State.RequestSent"] = "Corrective command request was sent.",
            ["Tenants.Correction.State.Accepted"] = "Command accepted; projection confirmation is pending.",
            ["Tenants.Correction.State.ProjectionPending"] = "Command events are stored; projection confirmation is pending.",
            ["Tenants.Correction.State.Confirmed"] = "Projection confirms the intended state; waiting for corrective audit proof.",
            ["Tenants.Correction.State.AlreadyApplied"] = "Current projection already shows the intended state.",
            ["Tenants.Correction.State.Rejected"] = "Corrective command was rejected.",
            ["Tenants.Correction.State.Failed"] = "Corrective command failed before acceptance.",
            ["Tenants.Correction.State.Degraded"] = "Command processing is degraded; refresh status or inspect audit evidence.",
            ["Tenants.Correction.State.UnableToVerify"] = "Correction cannot be verified from current evidence.",
            ["Tenants.Correction.Audit.AuditPending"] = "Corrective audit evidence is pending.",
            ["Tenants.Correction.Audit.AuditDelayed"] = "Corrective audit evidence is delayed.",
            ["Tenants.Correction.Audit.AuditUnavailable"] = "Corrective audit evidence is unavailable.",
            ["Tenants.Correction.Audit.MissingSupport"] = "Corrective audit support is unavailable.",
            ["Tenants.Correction.Proof.Link"] = "View corrective proof from {0}",
            ["Tenants.Correction.Unavailable.GlobalAdministratorCommandSupportUnavailable"] = "Global administrator correction commands are not connected.",
            ["Tenants.Correction.Unavailable.CurrentProjectionUnavailable"] = "Current projection evidence is unavailable.",
            ["Tenants.Correction.Recovery.AlreadyApplied"] = "Close this correction; the current projection already reflects the intended state.",
            ["Tenants.Correction.Recovery.Confirmed"] = "Keep the confirmed projection visible while corrective audit evidence catches up.",
            ["Tenants.Correction.Recovery.Rejected"] = "Review the rejection, refresh current evidence, and start a new correction only if it is still required.",
            ["Tenants.Correction.Recovery.Failed"] = "Refresh current evidence before starting another correction attempt.",
            ["Tenants.Correction.Recovery.Tracked"] = "Refresh command status and the fixed projection until terminal evidence is available.",
            ["Tenants.Correction.Recovery.Unavailable"] = "Restore the blocked authorization, lifecycle, viewport, or projection evidence before retrying.",
            ["Tenants.GlobalAdministrators.Availability.Grant.Unavailable.MissingLifecycleSupport"] = "Grant is unavailable because dispatch, status, or requery support is missing.",
            ["Tenants.GlobalAdministrators.Availability.Remove.Unavailable.MissingLifecycleSupport"] = "Remove is unavailable because dispatch, status, or requery support is missing.",
            ["Tenants.GlobalAdministrators.Availability.Recovery.MissingLifecycleSupport"] = "Restore dispatch, status, and requery support, then retry.",
            ["Tenants.GlobalAdministrators.Grant.Preview.Unavailable.TargetExists"] = "The exact target is already present in the complete global-administrator projection, so no grant was dispatched.",
            ["Tenants.GlobalAdministrators.Grant.Preview.Recovery.TargetExists"] = "Keep the confirmed rows unchanged and choose a target absent from the complete projection.",
            ["Tenants.GlobalAdministrators.Grant.SubmissionEvidence.Ambiguous"] = "Grant delivery is ambiguous. Refresh or retry with the same retained command identity.",
            ["Tenants.GlobalAdministrators.Grant.DeliveryRetry.Recovery"] = "Retry only with the retained command identity; do not create a new grant attempt.",
            ["Tenants.GlobalAdministrators.Grant.UnableToVerify.TrackingMismatch"] = "Command status did not match the exact retained grant identity.",
            ["Tenants.GlobalAdministrators.Grant.UnableToVerify.EventEvidence"] = "Exact command status did not prove that the grant produced an event.",
        };
    }
}
