using System.Globalization;

using AngleSharp.Dom;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Members;
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

public sealed class ChangeTenantMemberRoleFlowTests : FluentBunitContext
{
    [Fact]
    public void Change_role_flow_renders_stable_selectors_current_role_and_assignable_roles_only()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-change-role-flow']");
        cut.Find("[data-testid='tenants-change-role-user-id']").TextContent.ShouldContain("reader-user");
        cut.Find("[data-testid='tenants-change-role-current-role']").TextContent.ShouldContain("Tenant reader");
        cut.Find("[data-testid='tenants-change-role-new-role']").TextContent.ShouldContain("Tenant owner");
        cut.Find("[data-testid='tenants-change-role-new-role']").TextContent.ShouldContain("Tenant contributor");
        cut.Find("[data-testid='tenants-change-role-new-role']").TextContent.ShouldContain("Tenant reader");
        cut.Find("[data-testid='tenants-change-role-new-role']").TextContent.ShouldNotContain("Unknown");
        cut.Find("[data-testid='tenants-change-role-submit']").GetAttribute("disabled").ShouldBeNull();
        cut.Find("[data-testid='tenants-change-role-lifecycle']");
        cut.Find("[data-testid='tenants-change-role-state']");
        cut.Find("[data-testid='tenants-change-role-audit']");
        cut.Find("[data-testid='tenants-change-role-refresh']");
    }

    [Fact]
    public void Current_role_submission_records_already_applied_without_gateway_call_or_success_copy()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();

        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        gateway.ChangeRoleCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-change-role-state']").TextContent.ShouldContain("Already applied");
        cut.Find("[data-testid='tenants-change-role-safe-message']").TextContent.ShouldContain("already has role");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation", Case.Insensitive);
    }

    [Fact]
    public void Allowed_role_change_submits_literal_user_id_and_confirms_only_with_requested_projection_evidence()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-789"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed),
        };
        RegisterServices(gateway);

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("User/CaseSensitive.01", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(Detail(
                request.TenantId,
                [
                    new TenantMember("owner-user", TenantRole.TenantOwner),
                    new TenantMember(request.UserId, request.NewRole),
                ]))));

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantContributor));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.LastChangeRoleRequest.ShouldNotBeNull().UserId.ShouldBe("User/CaseSensitive.01"));
        gateway.LastChangeRoleRequest.ShouldNotBeNull().NewRole.ShouldBe(TenantRole.TenantContributor);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        cut.Find("[data-testid='tenants-change-role-live-region']").GetAttribute("aria-live").ShouldBe("polite");
        cut.Find("[data-testid='tenants-change-role-audit']").TextContent.ShouldContain("Audit evidence pending");
        cut.Markup.ShouldNotContain("correlation-789", Case.Insensitive);
    }

    [Fact]
    public void Completed_status_without_requested_role_evidence_stays_projection_pending_and_does_not_mutate_visible_current_role()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-789"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed),
        };
        RegisterServices(gateway);
        TenantDetail originalDetail = Detail("tenant.alpha");

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, originalDetail)
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, _ => Task.FromResult<TenantDetail?>(originalDetail)));

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantContributor));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending));
        cut.Find("[data-testid='tenants-change-role-current-role']").TextContent.ShouldContain("Tenant reader");
        cut.Find("[data-testid='tenants-change-role-state']").TextContent.ShouldContain("Projection pending");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Theory]
    [InlineData(false, TenantStatus.Active, "Tenant command support is unavailable")]
    [InlineData(true, TenantStatus.Disabled, "lifecycle state does not allow")]
    [InlineData(true, TenantStatus.Unknown, "lifecycle state does not allow")]
    public void Change_role_fails_closed_when_command_surface_or_tenant_lifecycle_is_unavailable(
        bool isCommandSurfaceAvailable,
        TenantStatus tenantStatus,
        string expectedReason)
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha") with { Status = tenantStatus })
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsCommandSurfaceAvailable, isCommandSurfaceAvailable));

        cut.Find("[data-testid='tenants-change-role-submit']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-change-role-unavailable-reason']").TextContent.ShouldContain(expectedReason, Case.Insensitive);
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantContributor));
        cut.Find("form").Submit();

        gateway.ChangeRoleCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-change-role-state']").TextContent.ShouldContain("Unable to verify");
        cut.Find("[data-testid='tenants-change-role-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
    }

    [Theory]
    [InlineData(TenantDetailSurfaceKind.Stale, ReadModelFreshnessState.Stale)]
    [InlineData(TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Unknown)]
    [InlineData(TenantDetailSurfaceKind.Degraded, ReadModelFreshnessState.Current)]
    [InlineData(TenantDetailSurfaceKind.Unavailable, ReadModelFreshnessState.Current)]
    [InlineData(TenantDetailSurfaceKind.Unknown, ReadModelFreshnessState.Current)]
    public void Change_role_fails_closed_for_stale_unknown_degraded_or_unavailable_projection_without_permission_copy(
        TenantDetailSurfaceKind surfaceKind,
        ReadModelFreshnessState freshness)
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, surfaceKind)
            .Add(p => p.Freshness, freshness));

        cut.Find("[data-testid='tenants-change-role-submit']").GetAttribute("disabled").ShouldNotBeNull();
        string reason = cut.Find("[data-testid='tenants-change-role-unavailable-reason']").TextContent;
        reason.ShouldContain("Refresh current tenant detail", Case.Insensitive);
        reason.ShouldNotContain("not authorized", Case.Insensitive);

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantContributor));
        cut.Find("form").Submit();

        gateway.ChangeRoleCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-change-role-state']").TextContent.ShouldContain("Unable to verify");
    }

    [Fact]
    public void Change_role_true_authorization_failure_still_renders_permission_reason()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsAuthorized, false));

        cut.Find("[data-testid='tenants-change-role-submit']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-change-role-unavailable-reason']").TextContent
            .ShouldContain("not authorized", Case.Insensitive);
    }

    [Fact]
    public void Owner_count_risk_is_visible_but_does_not_block_last_owner_role_loss()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-789"),
            Status = new TenantCommandStatusResult(CommandStatus.Received),
        };
        RegisterServices(gateway);
        TenantDetail detail = Detail("tenant.alpha", [new TenantMember("owner-user", TenantRole.TenantOwner)]);

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, detail)
            .Add(p => p.Member, new TenantMember("owner-user", TenantRole.TenantOwner))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantReader));

        cut.Find("[data-testid='tenants-change-role-risk']").TextContent.ShouldContain("reduce the visible owner count to zero");
        cut.Find("[data-testid='tenants-change-role-submit']").GetAttribute("disabled").ShouldBeNull();

        cut.Find("form").Submit();
        cut.WaitForAssertion(() => gateway.ChangeRoleCallCount.ShouldBe(1));
    }

    [Fact]
    public void Duplicate_submit_while_change_role_command_is_in_flight_is_blocked_before_gateway_submission()
    {
        TaskCompletionSource<TenantCommandSubmissionResult> pendingSubmission = new();
        StubTenantCommandGateway gateway = new()
        {
            ChangeRoleAsync = _ => pendingSubmission.Task,
        };
        RegisterServices(gateway);

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantContributor));
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => gateway.ChangeRoleCallCount.ShouldBe(1));

        cut.Find("form").Submit();

        gateway.ChangeRoleCallCount.ShouldBe(1);
        cut.Find("[data-testid='tenants-change-role-unavailable-reason']").TextContent.ShouldContain("already in progress");
        cut.Find("[data-testid='tenants-change-role-state']").TextContent.ShouldContain("Unable to verify");

        pendingSubmission.SetResult(TenantCommandSubmissionResult.Failed("Command submission cancelled by the test."));
    }

    [Fact]
    public void Rejection_uses_safe_copy_assertive_live_region_and_no_raw_internals()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Rejected(
                "The target user is not a visible member of this tenant. Refresh the member table before trying again.",
                "UserNotInTenant"),
        };
        RegisterServices(gateway);

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantContributor));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.Find("[data-testid='tenants-change-role-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Find("[data-testid='tenants-change-role-safe-message']").TextContent.ShouldContain("not a visible member");
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("\"payload\"", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("bearer ", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation", Case.Insensitive);
    }

    [Fact]
    public void Role_selection_is_labelled_and_associates_inline_reasons_with_the_control()
    {
        RegisterServices(new StubTenantCommandGateway());
        TenantDetail detail = Detail("tenant.alpha", [new TenantMember("owner-user", TenantRole.TenantOwner)]);

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, detail)
            .Add(p => p.Member, new TenantMember("owner-user", TenantRole.TenantOwner))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Stale)
            .Add(p => p.Freshness, ReadModelFreshnessState.Stale));

        IElement roleSelect = cut.Find("[data-testid='tenants-change-role-new-role']");
        cut.Find("label[for='tenants-change-role-new-role']").TextContent.ShouldContain("New role");
        roleSelect.GetAttribute("aria-describedby").ShouldBe("tenants-change-role-new-role-help tenants-change-role-unavailable");
        cut.Find("[data-testid='tenants-change-role-unavailable-reason']").TextContent.ShouldContain("Refresh current tenant detail");
        cut.Find("[data-testid='tenants-change-role-lifecycle']").GetAttribute("tabindex").ShouldBe("-1");
        cut.Find("[data-testid='tenants-change-role-lifecycle']").GetAttribute("aria-labelledby").ShouldBe("tenants-change-role-state-label");

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantReader));

        cut.Find("[data-testid='tenants-change-role-risk']").TextContent.ShouldContain("reduce the visible owner count to zero");
        cut.Find("[data-testid='tenants-change-role-new-role']").GetAttribute("aria-describedby")
            .ShouldBe("tenants-change-role-new-role-help tenants-change-role-risk tenants-change-role-unavailable");
    }

    [Fact]
    public void Spoofed_unknown_role_value_is_rejected_before_gateway_submission_with_inline_validation()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.Unknown));
        cut.Find("form").Submit();

        gateway.ChangeRoleCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-change-role-validation']").TextContent.ShouldContain("Select TenantOwner");
        cut.Find("[data-testid='tenants-change-role-new-role']").GetAttribute("aria-describedby")
            .ShouldBe("tenants-change-role-new-role-help tenants-change-role-validation");
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Idle);
    }

    [Fact]
    public void Manual_refresh_uses_status_lookup_and_projection_requery_before_confirming_requested_role()
    {
        int statusCalls = 0;
        int projectionCalls = 0;
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-789"),
            StatusAsync = _ => Task.FromResult(++statusCalls == 1
                ? new TenantCommandStatusResult(CommandStatus.Received)
                : new TenantCommandStatusResult(CommandStatus.Completed)),
        };
        RegisterServices(gateway);

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(++projectionCalls == 1
                ? Detail(request.TenantId)
                : Detail(
                    request.TenantId,
                    [
                        new TenantMember("owner-user", TenantRole.TenantOwner),
                        new TenantMember(request.UserId, request.NewRole),
                    ]))));

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantContributor));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted));
        cut.Find("[data-testid='tenants-change-role-refresh']").GetAttribute("disabled").ShouldBeNull();

        cut.Find("[data-testid='tenants-change-role-refresh']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        statusCalls.ShouldBe(2);
        projectionCalls.ShouldBe(2);
        cut.Find("[data-testid='tenants-change-role-state']").TextContent.ShouldContain("Projection confirmed");
    }

    [Theory]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected, "Change-role command rejected.", "assertive")]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded, "degraded", "assertive")]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify, "Unable to verify", "assertive")]
    public void Status_refresh_keeps_terminal_lifecycle_states_distinct_and_support_safe(
        CommandStatus status,
        TenantCommandLifecycleState expectedState,
        string expectedText,
        string expectedLiveRegion)
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-789"),
            Status = new TenantCommandStatusResult(status, "Safe status message.", "SafeCode"),
        };
        RegisterServices(gateway);

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(Detail(
                request.TenantId,
                [
                    new TenantMember("owner-user", TenantRole.TenantOwner),
                    new TenantMember(request.UserId, request.NewRole),
                ]))));

        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-change-role-new-role", nameof(TenantRole.TenantContributor));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(expectedState));
        cut.Find("[data-testid='tenants-change-role-state']").TextContent.ShouldContain(expectedText, Case.Insensitive);
        cut.Find("[data-testid='tenants-change-role-live-region']").GetAttribute("aria-live").ShouldBe(expectedLiveRegion);
        cut.Markup.ShouldNotContain("correlation-789", Case.Insensitive);
        cut.Markup.ShouldNotContain("raw payload", Case.Insensitive);
        cut.Markup.ShouldNotContain("\"payload\"", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("bearer ", Case.Insensitive);
        cut.Instance.Snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Close_requests_focus_recovery_through_parent_callback()
    {
        bool closeRequested = false;
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<ChangeTenantMemberRoleFlow> cut = Render<ChangeTenantMemberRoleFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.OnCloseRequested, () => closeRequested = true));

        cut.Find("[data-testid='tenants-change-role-cancel']").Click();

        closeRequested.ShouldBeTrue();
    }

    private void RegisterServices(StubTenantCommandGateway gateway)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);
    }

    private static TenantDetail Detail(string tenantId)
        => Detail(
            tenantId,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ]);

    private static TenantDetail Detail(string tenantId, IReadOnlyList<TenantMember> members)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            members,
            new Dictionary<string, string>(),
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", CultureInfo.InvariantCulture));

    private sealed class StubTenantCommandGateway : ITenantCommandGateway
    {
        public TenantCommandSubmissionResult Submission { get; init; }
            = TenantCommandSubmissionResult.Failed("Tenant command gateway is unavailable.");

        public TenantCommandStatusResult Status { get; init; }
            = TenantCommandStatusResult.Unknown("Command status is unavailable.");

        public Func<ChangeUserRole, Task<TenantCommandSubmissionResult>>? ChangeRoleAsync { get; init; }

        public Func<TenantCommandTrackingHandle, Task<TenantCommandStatusResult>>? StatusAsync { get; init; }

        public ChangeUserRole? LastChangeRoleRequest { get; private set; }

        public int ChangeRoleCallCount { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, CancellationToken cancellationToken = default)
        {
            ChangeRoleCallCount++;
            LastChangeRoleRequest = request;
            return ChangeRoleAsync is null ? Task.FromResult(Submission) : ChangeRoleAsync(request);
        }

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
            => StatusAsync is null ? Task.FromResult(Status) : StatusAsync(handle);
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.ChangeRole.Title"] = "Change tenant member role",
            ["Tenants.ChangeRole.Description"] = "Change the role for user {1} in tenant {0}. The current confirmed role is {2}.",
            ["Tenants.ChangeRole.UserId.Label"] = "User id",
            ["Tenants.ChangeRole.CurrentRole.Label"] = "Current confirmed role",
            ["Tenants.ChangeRole.OwnerContext.Label"] = "Owner context",
            ["Tenants.ChangeRole.OwnerContext.NoOwners"] = "0 visible owners; owner context is unavailable.",
            ["Tenants.ChangeRole.OwnerContext.LastOwner"] = "{0} visible owner; changing this owner can leave the tenant with zero visible owners.",
            ["Tenants.ChangeRole.OwnerContext.MultipleOwners"] = "{0} visible owners.",
            ["Tenants.ChangeRole.NewRole.Label"] = "New role",
            ["Tenants.ChangeRole.NewRole.Help"] = "Select TenantOwner, TenantContributor, or TenantReader. Selecting the current role records an already applied state.",
            ["Tenants.ChangeRole.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.ChangeRole.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.ChangeRole.Role.TenantReader"] = "Tenant reader",
            ["Tenants.ChangeRole.Submit"] = "Change role",
            ["Tenants.ChangeRole.Refresh"] = "Refresh status",
            ["Tenants.ChangeRole.Cancel"] = "Close",
            ["Tenants.ChangeRole.Lifecycle.Title"] = "Change role command lifecycle",
            ["Tenants.ChangeRole.Validation.RoleRequired"] = "Select TenantOwner, TenantContributor, or TenantReader before changing a role.",
            ["Tenants.ChangeRole.Unavailable.Authorization"] = "You are not authorized to change member roles in this tenant.",
            ["Tenants.ChangeRole.Unavailable.Freshness"] = "Refresh current tenant detail before changing a member role.",
            ["Tenants.ChangeRole.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow changing member roles.",
            ["Tenants.ChangeRole.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.ChangeRole.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.ChangeRole.Unavailable.UnknownRole"] = "The current role is unknown, so role change fails closed until projection evidence is refreshed.",
            ["Tenants.ChangeRole.OwnerRisk.LastOwner"] = "Warning: {0} visible owner remains. This change can reduce the visible owner count to zero, but the command is not blocked solely for that reason.",
            ["Tenants.ChangeRole.AlreadyApplied.Message"] = "User {0} already has role {1}; no role-change command was submitted.",
            ["Tenants.ChangeRole.State.Idle"] = "No change-role command submitted.",
            ["Tenants.ChangeRole.State.RequestSent"] = "Change-role request sent.",
            ["Tenants.ChangeRole.State.Accepted"] = "Accepted by EventStore; waiting for member role processing.",
            ["Tenants.ChangeRole.State.ProjectionPending"] = "Projection pending; the requested role is not confirmed visible yet.",
            ["Tenants.ChangeRole.State.Confirmed"] = "Projection confirmed the target user has the requested role.",
            ["Tenants.ChangeRole.State.Rejected"] = "Change-role command rejected.",
            ["Tenants.ChangeRole.State.AlreadyApplied"] = "Already applied; the confirmed role already matches the selected role.",
            ["Tenants.ChangeRole.State.Failed"] = "Change-role command submission failed.",
            ["Tenants.ChangeRole.State.Degraded"] = "Change-role command result is degraded and needs review.",
            ["Tenants.ChangeRole.State.UnableToVerify"] = "Unable to verify the change-role command result.",
            ["Tenants.ChangeRole.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.ChangeRole.Audit.AuditPending"] = "Audit evidence pending.",
            ["Tenants.ChangeRole.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.ChangeRole.Audit.MissingSupport"] = "Audit support is missing for this flow.",
            ["Tenants.Audit.EntryPoint.Accessible.Command"] = "Open audit evidence for {0} in tenant {1}",
            ["Tenants.Audit.EntryPoint.CommandReason"] = "Command-specific proof is not available here; open the tenant audit list and use the visible audit state.",
            ["Tenants.Audit.EntryPoint.Label"] = "Audit evidence",
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required before audit evidence can be opened.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope before opening audit evidence.",
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
        };

        public LocalizedString this[string name]
            => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }
}
