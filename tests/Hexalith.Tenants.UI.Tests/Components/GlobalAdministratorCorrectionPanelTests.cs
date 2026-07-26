using System.Globalization;

using Bunit;

using Hexalith.EventStore.Client.Projections;
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
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class GlobalAdministratorCorrectionPanelTests : FluentBunitContext
{
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
        commandGateway.StatusHandles.ShouldHaveSingleItem().ShouldBe(new TenantCommandTrackingHandle("message-safe", "tracking-safe"));
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
                return Task.FromResult<GlobalAdministratorsSnapshot?>(Projection("admin-user", "other-admin"));
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
    public void Pre_submit_already_applied_live_updates_to_submittable_when_projection_refreshes_while_open()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        TenantCorrectionStartIntent intent = RestoreIntent();

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Projection("admin-user", "other-admin")));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
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
    public void Tracked_already_applied_status_survives_parent_re_render_without_re_arming_submit()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        StubTenantCommandGateway commandGateway = new()
        {
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 0),
        };
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        TenantCorrectionStartIntent intent = RestoreIntent();

        IRenderedComponent<GlobalAdministratorCorrectionPanel> cut = Render<GlobalAdministratorCorrectionPanel>(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        cut.Find("[data-testid='tenants-correction-confirm']").Click();
        cut.WaitForAssertion(() => cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.AlreadyApplied));
        cut.Instance.Snapshot!.HasCommandTracking.ShouldBeTrue();

        cut.Render(parameters => parameters
            .Add(component => component.Intent, intent)
            .Add(component => component.CurrentProjection, Projection("other-admin")));

        cut.Instance.Snapshot!.LifecycleState.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
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
            ReadModelFreshnessState.Current);

    private static GlobalAdministratorsSnapshot Projection(params string[] userIds)
        => GlobalAdministratorsSnapshot.Ready(
            userIds.Select(userId => new GlobalAdministratorRow(userId, ReadModelFreshnessState.Current)).ToArray(),
            nextCursor: null,
            hasMore: false,
            eTag: "\"ga-etag\"",
            freshness: ReadModelFreshnessState.Current);

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
            ReadModelFreshnessState.Current);

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public List<SetGlobalAdministrator> SetRequests { get; } = [];

        public List<RemoveGlobalAdministrator> RemoveRequests { get; } = [];

        public List<TenantCommandTrackingHandle> StatusHandles { get; } = [];

        public Task<TenantCommandSubmissionResult>? SetResultTask { get; init; }

        public Task<TenantCommandSubmissionResult>? RemoveResultTask { get; init; }

        public TenantCommandStatusResult Status { get; init; }
            = new(CommandStatus.Completed, EventCount: 1);

        public Task<TenantCommandSubmissionResult> SetGlobalAdministratorAsync(
            SetGlobalAdministrator request,
            CancellationToken cancellationToken = default)
        {
            SetRequests.Add(request);
            return SetResultTask ?? Task.FromResult(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"));
        }

        public Task<TenantCommandSubmissionResult> RemoveGlobalAdministratorAsync(
            RemoveGlobalAdministrator request,
            CancellationToken cancellationToken = default)
        {
            RemoveRequests.Add(request);
            return RemoveResultTask ?? Task.FromResult(TenantCommandSubmissionResult.Accepted("message-safe", "tracking-safe"));
        }

        public Task<TenantCommandStatusResult> GetStatusAsync(
            TenantCommandTrackingHandle handle,
            CancellationToken cancellationToken = default)
        {
            StatusHandles.Add(handle);
            return Task.FromResult(Status);
        }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubTenantQueryGateway(GlobalAdministratorsSnapshot projection, TenantAuditSnapshot audit) : ITenantQueryGateway
    {
        public List<GlobalAdministratorsRequest> GlobalAdminRequests { get; } = [];

        public List<TenantAuditRequest> AuditRequests { get; } = [];

        public Task<GlobalAdministratorsSnapshot> GetGlobalAdministratorsAsync(
            GlobalAdministratorsRequest request,
            GlobalAdministratorsSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            GlobalAdminRequests.Add(request);
            return Task.FromResult(projection);
        }

        public Task<TenantAuditSnapshot> GetTenantAuditAsync(
            TenantAuditRequest request,
            TenantAuditSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            AuditRequests.Add(request);
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
        };
    }
}
