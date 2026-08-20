using System.Globalization;
using System.Text.RegularExpressions;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class RemoveTenantMemberFlowTests : FluentBunitContext
{
    [Fact]
    public void Remove_flow_renders_complete_preview_with_stable_selectors_and_no_audit_receipt_claim()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-remove-member-flow']").GetAttribute("role").ShouldBe("dialog");
        cut.Find("[data-testid='tenants-remove-member-flow']").GetAttribute("aria-modal").ShouldBe("true");
        cut.Find("[data-testid='tenants-remove-member-focus-start']");
        cut.Find("[data-testid='tenants-remove-member-focus-end']");
        cut.Find("[data-testid='tenants-remove-member-preview']");
        cut.FindAll("[data-testid='tenants-remove-member-preview-item']").Count.ShouldBe(10);
        cut.Find("[data-testid='tenants-remove-member-target-user-id']").TextContent.ShouldContain("reader-user");
        cut.Find("[data-testid='tenants-remove-member-current-role']").TextContent.ShouldContain("Tenant reader");
        cut.Find("[data-testid='tenants-remove-member-owner-context']").TextContent.ShouldContain("2 visible owners");
        cut.Find("[data-testid='tenants-remove-member-platform-standing']").TextContent.ShouldContain("unproven");
        cut.Find("[data-testid='tenants-remove-member-consequences-versus-unknowns']").TextContent
            .ShouldContain("Known consequence");
        cut.FindAll("[data-testid='tenants-remove-member-global-admin-risk']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-remove-member-confirm']").GetAttribute("disabled").ShouldBeNull();
        cut.Markup.ShouldNotContain("audit available", Case.Insensitive);
        cut.Markup.ShouldNotContain("receipt", Case.Insensitive);
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Last_owner_warning_is_visible_but_does_not_block_destructive_confirmation()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-999"),
            Status = new TenantCommandStatusResult(CommandStatus.Received),
        };
        RegisterServices(gateway);
        TenantDetail detail = Detail("tenant.alpha", [new TenantMember("owner-user", TenantRole.TenantOwner)]);

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, detail)
            .Add(p => p.Member, new TenantMember("owner-user", TenantRole.TenantOwner))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-remove-member-owner-risk']").TextContent.ShouldContain("Last-owner");
        cut.FindAll("[data-testid='tenants-remove-member-global-admin-risk']").ShouldBeEmpty();
        cut.Markup.ShouldContain("Elevated risk: type the target user id exactly to confirm removal");
        cut.Find("#tenants-remove-member-confirmation-help").TextContent
            .ShouldContain("Elevated risk: type owner-user exactly");
        cut.Find("[data-testid='tenants-remove-member-confirm']").GetAttribute("disabled").ShouldBeNull();

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("Owner-User");
        cut.Find("form").Submit();
        cut.Find("[data-testid='tenants-remove-member-validation']").TextContent
            .ShouldContain("Elevated risk requires typing owner-user exactly");
        gateway.RemoveMemberCallCount.ShouldBe(0);

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("owner-user");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.RemoveMemberCallCount.ShouldBe(1));
    }

    [Fact]
    public void Global_admin_friction_is_visible_when_reflected_without_dispatching_global_admin_command()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.TargetGlobalAdministratorFriction, true)
            .Add(p => p.TargetPlatformStandingResolved, true));

        cut.Find("[data-testid='tenants-remove-member-platform-standing']").TextContent
            .ShouldContain("Also a global administrator");
        cut.Find("[data-testid='tenants-remove-member-global-admin-risk']").TextContent
            .ShouldContain("will not remove global-administrator authority");
        cut.Markup.ShouldContain("Elevated risk", Case.Insensitive);
        cut.Markup.ShouldNotContain("RemoveGlobalAdministrator", Case.Insensitive);
    }

    [Fact]
    public void Resolved_non_global_admin_platform_standing_does_not_raise_elevated_ga_friction()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.TargetPlatformStandingResolved, true));

        cut.Find("[data-testid='tenants-remove-member-platform-standing']").TextContent
            .ShouldContain("Not reflected as a global administrator");
        cut.FindAll("[data-testid='tenants-remove-member-global-admin-risk']").ShouldBeEmpty();
    }

    [Fact]
    public void Incomplete_preview_blocks_confirm_and_dispatch()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Stale)
            .Add(p => p.Freshness, ReadModelFreshnessState.Stale));

        cut.Find("[data-testid='tenants-remove-member-confirm']").GetAttribute("disabled").ShouldNotBeNull();
        string unavailable = cut.Find("[data-testid='tenants-remove-member-unavailable-reason']").TextContent;
        unavailable.ShouldContain("Refresh current tenant detail", Case.Insensitive);
        // Same reason must not also render as the preview-blocked banner.
        cut.FindAll("[data-testid='tenants-remove-member-preview-blocked']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-remove-member-preview-item']").ShouldBeEmpty();

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
        cut.Find("form").Submit();

        gateway.RemoveMemberCallCount.ShouldBe(0);
    }

    [Fact]
    public void Css_contains_focus_trap_and_narrow_layout_hooks()
    {
        string styles = File.ReadAllText(Path.Combine(
            RepoRoot(),
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Tenants",
            "Members",
            "RemoveTenantMemberFlow.razor.css"));

        styles.ShouldContain("tenants-remove-member-flow__focus-sentinel");
        styles.ShouldContain("tenants-remove-member-flow__narrow");
        int narrowMedia = styles.IndexOf("@media (max-width: 767px)", StringComparison.Ordinal);
        narrowMedia.ShouldBeGreaterThanOrEqualTo(0, "Expected a max-width: 767px media query for narrow fail-closed layout.");
        int nextMedia = styles.IndexOf("@media", narrowMedia + 1, StringComparison.Ordinal);
        string narrowBody = nextMedia < 0 ? styles[narrowMedia..] : styles[narrowMedia..nextMedia];
        Regex.IsMatch(
                narrowBody,
                @"\.tenants-remove-member-flow__form\s*\{\s*display:\s*none\s*;",
                RegexOptions.CultureInvariant)
            .ShouldBeTrue("Narrow media query must hide .tenants-remove-member-flow__form with display: none.");
    }

    [Fact]
    public void Confirmation_submits_literal_user_id_and_confirms_only_after_absent_projection_evidence()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-999"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);
        TenantDetail originalDetail = Detail("tenant.alpha");
        int projectionCalls = 0;
        string liveProjectionVersion = "v1";

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, originalDetail)
            .Add(p => p.Member, new TenantMember("User/CaseSensitive.01", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1")
            .Add(p => p.ProjectionVersionProvider, () => liveProjectionVersion)
            .Add(p => p.ProjectionEvidenceProvider, request =>
            {
                if (++projectionCalls == 1)
                {
                    return Task.FromResult<TenantDetail?>(Detail(
                        request.TenantId,
                        [
                            new TenantMember("owner-user", TenantRole.TenantOwner),
                            new TenantMember(request.UserId, TenantRole.TenantReader),
                        ]));
                }

                liveProjectionVersion = "v2";
                return Task.FromResult<TenantDetail?>(Detail(
                    request.TenantId,
                    [new TenantMember("owner-user", TenantRole.TenantOwner)]));
            }));

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("User/CaseSensitive.01");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending));
        gateway.LastRemoveMemberRequest.ShouldNotBeNull().UserId.ShouldBe("User/CaseSensitive.01");
        cut.Find("[data-testid='tenants-remove-member-target-user-id']").TextContent.ShouldContain("User/CaseSensitive.01");
        cut.Find("[data-testid='tenants-remove-member-state']").TextContent.ShouldContain("Projection pending");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);

        cut.Find("[data-testid='tenants-remove-member-refresh']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        projectionCalls.ShouldBe(2);
        cut.Find("[data-testid='tenants-remove-member-state']").TextContent.ShouldContain("Projection confirmed");
        cut.Markup.ShouldNotContain("correlation-999", Case.Insensitive);
    }

    [Fact]
    public void User_not_in_tenant_rejection_requires_absent_projection_before_already_applied()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-999"),
            Status = new TenantCommandStatusResult(CommandStatus.Rejected, "Target user not in tenant.", "UserNotInTenant"),
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(Detail(
                request.TenantId,
                [new TenantMember("owner-user", TenantRole.TenantOwner)]))));

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied));
        cut.Find("[data-testid='tenants-remove-member-state']").TextContent.ShouldContain("Already applied");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Submission_time_user_not_in_tenant_rejection_reconciles_to_already_applied_after_absent_projection()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Rejected("The target user is not a visible member of this tenant.", "UserNotInTenant"),
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(Detail(
                request.TenantId,
                [new TenantMember("owner-user", TenantRole.TenantOwner)]))));

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
        cut.Find("form").Submit();

        // A submission-time rejection carries no tracking handle, but the refresh recovery action
        // must still be reachable so projection evidence can reconcile it to already-applied (AC4).
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.Find("[data-testid='tenants-remove-member-refresh']").GetAttribute("disabled").ShouldBeNull();

        cut.Find("[data-testid='tenants-remove-member-refresh']").Click();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied));
        gateway.RemoveMemberCallCount.ShouldBe(1);
        cut.Find("[data-testid='tenants-remove-member-state']").TextContent.ShouldContain("Already applied");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Confirmation_text_must_match_target_before_gateway_submission()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("Reader-User");
        cut.Find("form").Submit();

        gateway.RemoveMemberCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-remove-member-validation']").TextContent.ShouldContain("reader-user");
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Previewed);
    }

    [Fact]
    public void Already_absent_target_before_submit_records_already_applied_without_gateway_submission()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha", [new TenantMember("owner-user", TenantRole.TenantOwner)]))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("form").Submit();

        gateway.RemoveMemberCallCount.ShouldBe(0);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        cut.Find("[data-testid='tenants-remove-member-safe-message']").TextContent.ShouldContain("already absent");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
    }

    [Fact]
    public void Duplicate_submit_while_remove_command_is_in_flight_is_blocked_before_gateway_submission()
    {
        TaskCompletionSource<TenantCommandSubmissionResult> pendingSubmission = new();
        StubTenantCommandGateway gateway = new()
        {
            RemoveMemberAsync = _ => pendingSubmission.Task,
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => gateway.RemoveMemberCallCount.ShouldBe(1));

        cut.Find("form").Submit();

        gateway.RemoveMemberCallCount.ShouldBe(1);
        cut.Find("[data-testid='tenants-remove-member-state']").TextContent.ShouldContain("Duplicate");
        cut.Find("[data-testid='tenants-remove-member-live-region']").GetAttribute("aria-live").ShouldBe("assertive");

        pendingSubmission.SetResult(TenantCommandSubmissionResult.Failed("Command submission cancelled by the test."));
    }

    [Fact]
    public void In_flight_retry_with_tracking_reuses_status_lookup_and_does_not_dispatch_again()
    {
        int closeCount = 0;
        List<bool> activity = [];
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-999"),
            Status = new TenantCommandStatusResult(CommandStatus.Received),
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1")
            .Add(p => p.CommandActivityLease, active =>
            {
                activity.Add(active);
                return Task.FromResult(true);
            })
            .Add(p => p.OnCloseRequested, () => closeCount++));

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.RemoveMemberCallCount.ShouldBe(1));
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        int statusCallsAfterSubmit = gateway.StatusCallCount;

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.RemoveMemberCallCount.ShouldBe(1));
        gateway.StatusCallCount.ShouldBeGreaterThan(statusCallsAfterSubmit);
        cut.Instance.Snapshot.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        cut.Find("[data-testid='tenants-remove-member-cancel']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-remove-member-flow']").KeyDown("Escape");
        closeCount.ShouldBe(0);
        activity.ShouldBe([true]);
    }

    [Fact]
    public void Ambiguous_failure_reuses_message_id_only_for_the_exact_same_removal()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Failed("Submission outcome is ambiguous.") with
            {
                MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            },
        };
        RegisterServices(gateway);
        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
        cut.Find("form").Submit();
        cut.Find("form").Submit();
        gateway.LastRemoveMemberMessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");

        cut.Render(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("second-owner", TenantRole.TenantOwner))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));
        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("second-owner");
        cut.Find("form").Submit();

        gateway.LastRemoveMemberMessageId.ShouldBeNull();
        gateway.RemoveMemberCallCount.ShouldBe(3);
    }

    [Fact]
    public void Programmatic_submit_while_unable_to_verify_recovers_status_without_dispatching()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"),
            Status = new TenantCommandStatusResult(CommandStatus.TimedOut),
        };
        RegisterServices(gateway);
        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));
        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        int statusCalls = gateway.StatusCallCount;

        cut.Find("form").Submit();

        gateway.RemoveMemberCallCount.ShouldBe(1);
        gateway.StatusCallCount.ShouldBe(statusCalls + 1);
    }

    [Theory]
    [InlineData(TenantDetailSurfaceKind.Stale, ReadModelFreshnessState.Stale, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Unknown, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Degraded, ReadModelFreshnessState.Current, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Unavailable, ReadModelFreshnessState.Current, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Unknown, ReadModelFreshnessState.Current, "Refresh current tenant detail")]
    public void Remove_flow_fails_closed_without_partial_preview_when_context_is_unavailable(
        TenantDetailSurfaceKind surfaceKind,
        ReadModelFreshnessState freshness,
        string expectedReason)
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, surfaceKind)
            .Add(p => p.Freshness, freshness));

        cut.Find("[data-testid='tenants-remove-member-confirm']").GetAttribute("disabled").ShouldNotBeNull();
        string reason = cut.Find("[data-testid='tenants-remove-member-unavailable-reason']").TextContent;
        reason.ShouldContain(expectedReason, Case.Insensitive);
        reason.ShouldNotContain("not authorized", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-remove-member-preview-item']").ShouldBeEmpty();

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
        cut.Find("form").Submit();

        gateway.RemoveMemberCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-remove-member-state']").TextContent.ShouldContain("Unable to verify");
    }

    [Fact]
    public void Remove_member_true_authorization_failure_still_renders_permission_reason()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsAuthorized, false));

        cut.Find("[data-testid='tenants-remove-member-confirm']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-remove-member-unavailable-reason']").TextContent
            .ShouldContain("not authorized", Case.Insensitive);
    }

    [Fact]
    public void Cancel_and_escape_request_close_without_submitting()
    {
        int closeCount = 0;
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.OnCloseRequested, () => closeCount++));

        cut.Find("[data-testid='tenants-remove-member-cancel']").Click();
        cut.Find("[data-testid='tenants-remove-member-flow']").KeyDown("Escape");

        closeCount.ShouldBe(2);
        gateway.RemoveMemberCallCount.ShouldBe(0);
    }

    [Fact]
    public void Escape_while_submitting_does_not_close_or_dispatch_again()
    {
        int closeCount = 0;
        TaskCompletionSource<TenantCommandSubmissionResult> pendingSubmission = new();
        StubTenantCommandGateway gateway = new()
        {
            RemoveMemberAsync = _ => pendingSubmission.Task,
        };
        RegisterServices(gateway);

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.OnCloseRequested, () => closeCount++));

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => gateway.RemoveMemberCallCount.ShouldBe(1));

        cut.Find("[data-testid='tenants-remove-member-flow']").KeyDown("Escape");
        cut.Find("[data-testid='tenants-remove-member-cancel']").GetAttribute("disabled").ShouldNotBeNull();

        closeCount.ShouldBe(0);
        gateway.RemoveMemberCallCount.ShouldBe(1);

        pendingSubmission.SetResult(TenantCommandSubmissionResult.Failed("Command submission cancelled by the test."));
    }

    [Fact]
    public void Missing_audit_proof_capability_fail_closes_before_dispatch()
    {
        RegisterServices(new StubTenantCommandGateway(), new UnavailableTenantQueryGateway());

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-remove-member-unavailable-reason']").TextContent
            .ShouldContain("missing audit proof");
        cut.Find("[data-testid='tenants-remove-member-confirm']").GetAttribute("disabled").ShouldNotBeNull();
    }

    [Fact]
    public void Confirmed_removal_with_matching_audit_row_renders_wp2a_receipt()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };

        ITenantQueryGateway queryGateway = Substitute.For<ITenantQueryGateway>();
        queryGateway.GetTenantAuditAsync(
                Arg.Any<TenantAuditRequest>(),
                Arg.Any<TenantAuditSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                TenantAuditRequest request = call.ArgAt<TenantAuditRequest>(0);
                DateTimeOffset from = request.From ?? DateTimeOffset.UtcNow.AddMinutes(-1);
                TenantAuditRow match = new(
                    "evt-remove-1",
                    "UserRemovedFromTenant",
                    AuditEventCategory.Access,
                    "actor-1",
                    from.AddSeconds(30),
                    request.TenantId,
                    "reader-user",
                    request.TenantId,
                    "removed",
                    "userId: reader-user",
                    ReadModelFreshnessState.Current);
                return TenantAuditSnapshot.Ready(
                    [match],
                    nextCursor: null,
                    hasMore: false,
                    eTag: "etag-1",
                    ReadModelFreshnessState.Current,
                    request);
            });
        RegisterServices(gateway, queryGateway);

        string liveProjectionVersion = "v1";
        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1")
            .Add(p => p.ProjectionVersionProvider, () => liveProjectionVersion)
            .Add(p => p.ProjectionEvidenceProvider, request =>
            {
                liveProjectionVersion = "v2";
                return Task.FromResult<TenantDetail?>(Detail(
                    request.TenantId,
                    [
                        new TenantMember("owner-user", TenantRole.TenantOwner),
                        new TenantMember("second-owner", TenantRole.TenantOwner),
                    ]));
            }));

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
            cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditAvailable);
        });
        cut.Find("[data-testid='tenants-audit-receipt']");
        cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("data-state").ShouldBe("available");
    }

    [Fact]
    public void Confirmed_removal_without_matching_audit_stays_pending_without_receipt()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);

        string liveProjectionVersion = "v1";
        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1")
            .Add(p => p.ProjectionVersionProvider, () => liveProjectionVersion)
            .Add(p => p.ProjectionEvidenceProvider, request =>
            {
                liveProjectionVersion = "v2";
                return Task.FromResult<TenantDetail?>(Detail(
                    request.TenantId,
                    [
                        new TenantMember("owner-user", TenantRole.TenantOwner),
                        new TenantMember("second-owner", TenantRole.TenantOwner),
                    ]));
            }));

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
            cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
        });
        cut.FindAll("[data-testid='tenants-audit-receipt']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("data-state").ShouldBe("pending");
    }

    [Fact]
    public void Confirmed_removal_with_unauthorized_audit_keeps_confirmed_without_available()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        ITenantQueryGateway queryGateway = Substitute.For<ITenantQueryGateway>();
        queryGateway.GetTenantAuditAsync(
                Arg.Any<TenantAuditRequest>(),
                Arg.Any<TenantAuditSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(call => TenantAuditSnapshot.Unauthorized(call.ArgAt<TenantAuditRequest>(0)));
        RegisterServices(gateway, queryGateway);

        string liveProjectionVersion = "v1";
        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.Member, new TenantMember("reader-user", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1")
            .Add(p => p.ProjectionVersionProvider, () => liveProjectionVersion)
            .Add(p => p.ProjectionEvidenceProvider, request =>
            {
                liveProjectionVersion = "v2";
                return Task.FromResult<TenantDetail?>(Detail(
                    request.TenantId,
                    [
                        new TenantMember("owner-user", TenantRole.TenantOwner),
                        new TenantMember("second-owner", TenantRole.TenantOwner),
                    ]));
            }));

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
            cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable);
        });
        cut.FindAll("[data-testid='tenants-audit-receipt']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-audit-availability']").GetAttribute("data-state").ShouldBe("unavailable");
    }

    private void RegisterServices(StubTenantCommandGateway gateway, ITenantQueryGateway? queryGateway = null)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton<ITenantCommandGateway>(gateway);
        Services.AddSingleton<ITenantQueryGateway>(queryGateway ?? CreateCapableQueryGateway());
    }

    private static ITenantQueryGateway CreateCapableQueryGateway(TenantAuditSnapshot? audit = null)
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetTenantAuditAsync(
                Arg.Any<TenantAuditRequest>(),
                Arg.Any<TenantAuditSnapshot?>(),
                Arg.Any<CancellationToken>())
            .Returns(call => audit ?? TenantAuditSnapshot.Empty(
                isAuthorizationScoped: true,
                Hexalith.EventStore.Client.Projections.ReadModelFreshnessState.Current,
                eTag: null,
                call.ArgAt<TenantAuditRequest>(0)));
        return gateway;
    }

    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Tenants.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root from test output.");
    }

    private static TenantDetail Detail(string tenantId)
        => Detail(
            tenantId,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("second-owner", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
                new TenantMember("User/CaseSensitive.01", TenantRole.TenantReader),
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

        public Func<RemoveUserFromTenant, Task<TenantCommandSubmissionResult>>? RemoveMemberAsync { get; init; }

        public RemoveUserFromTenant? LastRemoveMemberRequest { get; private set; }

        public string? LastRemoveMemberMessageId { get; private set; }

        public int RemoveMemberCallCount { get; private set; }

        public int StatusCallCount { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, string? messageId = null, CancellationToken cancellationToken = default)
        {
            RemoveMemberCallCount++;
            LastRemoveMemberRequest = request;
            LastRemoveMemberMessageId = messageId;
            return RemoveMemberAsync is null ? Task.FromResult(Submission) : RemoveMemberAsync(request);
        }

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
        {
            StatusCallCount++;
            return Task.FromResult(Status);
        }
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.RemoveMember.Title"] = "Remove tenant member",
            ["Tenants.RemoveMember.Description"] = "Preview removal of user {1} from tenant {0}. Current confirmed role is {2}.",
            ["Tenants.RemoveMember.Preview.Title"] = "Consequence preview",
            ["Tenants.RemoveMember.Preview.Tenant"] = "Tenant",
            ["Tenants.RemoveMember.Preview.TargetUser"] = "Target user",
            ["Tenants.RemoveMember.Preview.CurrentRole"] = "Current role",
            ["Tenants.RemoveMember.Preview.OwnerCount"] = "Owner count",
            ["Tenants.RemoveMember.Preview.AccessPath"] = "Affected access path",
            ["Tenants.RemoveMember.Preview.AccessPath.Value"] = "Tenant membership for the visible tenant only.",
            ["Tenants.RemoveMember.Preview.Freshness"] = "Freshness",
            ["Tenants.RemoveMember.Preview.RecoveryPath"] = "Recovery path",
            ["Tenants.RemoveMember.Preview.RecoveryPath.Value"] = "Wait, refresh, inspect audit when available, or submit a forward correction to restore intended access.",
            ["Tenants.RemoveMember.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.RemoveMember.Preview.AuditExpectation.Value"] = "Audit evidence is pending or unavailable until the Epic 5 evidence source exists.",
            ["Tenants.RemoveMember.Preview.PlatformStanding"] = "Platform standing",
            ["Tenants.RemoveMember.Preview.PlatformStanding.Known"] = "Also a global administrator. Tenant membership removal does not change platform administrator authority.",
            ["Tenants.RemoveMember.Preview.PlatformStanding.NotReflected"] = "Not reflected as a global administrator in the current complete projection.",
            ["Tenants.RemoveMember.Preview.PlatformStanding.Unknown"] = "Global-administrator standing is unproven in this view and is not guessed.",
            ["Tenants.RemoveMember.Preview.ConsequencesVersusUnknowns"] = "Known consequences versus unknowns",
            ["Tenants.RemoveMember.Preview.ConsequencesVersusUnknowns.Value"] = "Known consequence: tenant membership is removed only after projection confirmation proves the target user is absent. Known unknowns: session revocation, downstream enforcement, and token invalidation are not proven by this flow.",
            ["Tenants.RemoveMember.Preview.Blocked.Required"] = "Consequence preview is incomplete. Refresh current tenant detail before confirming removal.",
            ["Tenants.RemoveMember.Freshness.Current"] = "Current",
            ["Tenants.RemoveMember.Freshness.Stale"] = "Stale",
            ["Tenants.RemoveMember.Freshness.Unknown"] = "Unknown",
            ["Tenants.RemoveMember.OwnerContext.NoOwners"] = "0 visible owners; owner context is unavailable.",
            ["Tenants.RemoveMember.OwnerContext.LastOwner"] = "{0} visible owner; removing this member can leave zero visible owners.",
            ["Tenants.RemoveMember.OwnerContext.MultipleOwners"] = "{0} visible owners.",
            ["Tenants.RemoveMember.OwnerRisk.LastOwner"] = "Warning: {0} visible owner remains. Last-owner tenant membership removal is allowed, but it needs deliberate confirmation.",
            ["Tenants.RemoveMember.OwnerRisk.Accessible"] = "Elevated last-owner removal warning for {0} visible owner.",
            ["Tenants.RemoveMember.GlobalAdminRisk.Known"] = "Platform administrator authority is reflected for this user. This flow removes tenant membership only and will not remove global-administrator authority.",
            ["Tenants.RemoveMember.GlobalAdminRisk.Accessible"] = "Platform authority risk context.",
            ["Tenants.RemoveMember.Confirmation.Label"] = "Type the target user id to confirm removal",
            ["Tenants.RemoveMember.Confirmation.Elevated.Label"] = "Elevated risk: type the target user id exactly to confirm removal",
            ["Tenants.RemoveMember.Confirmation.Help"] = "Type {0} exactly. Cancel or Escape closes without submitting.",
            ["Tenants.RemoveMember.Confirmation.Elevated.Help"] = "Elevated risk: type {0} exactly. Cancel or Escape closes without submitting.",
            ["Tenants.RemoveMember.Confirm"] = "Remove member",
            ["Tenants.RemoveMember.Refresh"] = "Refresh status",
            ["Tenants.RemoveMember.Cancel"] = "Cancel",
            ["Tenants.RemoveMember.Lifecycle.Title"] = "Remove member command lifecycle",
            ["Tenants.RemoveMember.Validation.ConfirmationRequired"] = "Type {0} exactly before removing this member.",
            ["Tenants.RemoveMember.Validation.ElevatedConfirmationRequired"] = "Elevated risk requires typing {0} exactly before removing this member.",
            ["Tenants.RemoveMember.Unavailable.Authorization"] = "You are not authorized to remove members from this tenant.",
            ["Tenants.RemoveMember.Unavailable.Narrow"] = "Member removal is unavailable on narrow layouts because the complete preview, risk context, and lifecycle must remain visible together. Widen the viewport or continue read-only.",
            ["Tenants.RemoveMember.Unavailable.Freshness"] = "Refresh current tenant detail before removing a member.",
            ["Tenants.RemoveMember.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow removing members.",
            ["Tenants.RemoveMember.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.RemoveMember.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.RemoveMember.Unavailable.Identity"] = "Tenant id and target user id are required before the preview can open.",
            ["Tenants.RemoveMember.Unavailable.UnknownRole"] = "The current role is unknown, so remove member fails closed until projection evidence is refreshed.",
            ["Tenants.RemoveMember.Unavailable.TargetAbsent"] = "The target user is already absent from the current confirmed projection.",
            ["Tenants.Members.UnavailableReason.MissingAuditProof"] = "missing audit proof",
            ["Tenants.RemoveMember.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.RemoveMember.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.RemoveMember.Role.TenantReader"] = "Tenant reader",
            ["Tenants.RemoveMember.Role.Unknown"] = "Unknown role",
            ["Tenants.RemoveMember.AlreadyApplied.BeforeSubmit"] = "User {0} is already absent from the current confirmed projection; no remove command was submitted.",
            ["Tenants.RemoveMember.DuplicatePrevented.Message"] = "A remove-member command is already in progress for this flow.",
            ["Tenants.RemoveMember.Confirm.AlreadyApplied.PreExisting"] = "Projection evidence confirms the target user was already absent before this attempt; no removal success is asserted.",
            ["Tenants.RemoveMember.Confirm.AlreadyApplied.RejectedAbsence"] = "Projection evidence confirms the target user is already absent; no command result or audit proof is asserted.",
            ["Tenants.RemoveMember.Confirm.UnableToVerify.MissingBaseline"] = "Member absence matched without a pre-submit baseline, so this attempt cannot be confirmed.",
            ["Tenants.RemoveMember.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.RemoveMember.State.Idle"] = "No remove-member preview opened.",
            ["Tenants.RemoveMember.State.Previewed"] = "Consequence preview ready; no command has been submitted.",
            ["Tenants.RemoveMember.State.RequestSent"] = "Remove-member request sent.",
            ["Tenants.RemoveMember.State.Accepted"] = "Accepted by EventStore; waiting for member removal processing.",
            ["Tenants.RemoveMember.State.ProjectionPending"] = "Projection pending; the target user is not confirmed absent yet.",
            ["Tenants.RemoveMember.State.Confirmed"] = "Projection confirmed the target user is absent from the tenant members.",
            ["Tenants.RemoveMember.State.Rejected"] = "Remove-member command rejected.",
            ["Tenants.RemoveMember.State.AlreadyApplied"] = "Already applied; projection evidence shows the target user is absent.",
            ["Tenants.RemoveMember.State.DuplicatePrevented"] = "Duplicate remove-member submission prevented.",
            ["Tenants.RemoveMember.State.Failed"] = "Remove-member command submission failed.",
            ["Tenants.RemoveMember.State.Degraded"] = "Remove-member command result is degraded and needs review.",
            ["Tenants.RemoveMember.State.UnableToVerify"] = "Unable to verify the remove-member command result.",
            ["Tenants.RemoveMember.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.RemoveMember.Audit.AuditPending"] = "Audit evidence pending; removal is confirmed, wait or refresh for support-safe proof.",
            ["Tenants.RemoveMember.Audit.AuditDelayed"] = "Audit evidence delayed; retry status lookup or inspect audit for removal proof.",
            ["Tenants.RemoveMember.Audit.AuditUnavailable"] = "Audit evidence unavailable; confirmed removal stands without support-safe proof.",
            ["Tenants.RemoveMember.Audit.AuditAvailable"] = "Audit evidence available; support-safe removal proof is ready.",
            ["Tenants.RemoveMember.Audit.MissingSupport"] = "Audit evidence support is missing; continue read-only or escalate.",
            ["Tenants.Audit.EntryPoint.Accessible.Command"] = "Open audit evidence for {0} in tenant {1}",
            ["Tenants.Audit.EntryPoint.CommandReason"] = "Command-specific proof is not available here; open the tenant audit list and use the visible audit state.",
            ["Tenants.Audit.EntryPoint.Label"] = "Audit evidence",
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required before audit evidence can be opened.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope before opening audit evidence.",
            ["Tenants.Audit.Availability.Accessible.Delayed"] = "Audit evidence is delayed; retry status lookup or inspect audit before citing proof.",
            ["Tenants.Audit.Availability.Accessible.Available"] = "Audit evidence is available; support-safe proof may be inspected or copied.",
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
            ["Tenants.Audit.Availability.State.Available"] = "Audit available",
            ["Tenants.Audit.Availability.State.MissingSupport"] = "Missing implementation support",
            ["Tenants.Audit.Availability.State.Pending"] = "Audit pending",
            ["Tenants.Audit.Availability.State.Unavailable"] = "Audit unavailable",
            ["Tenants.Audit.Receipt.Title"] = "Audit evidence receipt",
            ["Tenants.Audit.Receipt.Copy"] = "Copy audit receipt reference",
            ["Tenants.Audit.Receipt.Field.Actor"] = "Actor",
            ["Tenants.Audit.Receipt.Field.Target"] = "Target",
            ["Tenants.Audit.Receipt.Field.Scope"] = "Tenant scope",
            ["Tenants.Audit.Receipt.Field.Outcome"] = "Outcome",
            ["Tenants.Audit.Receipt.Field.Timestamp"] = "Timestamp",
            ["Tenants.Audit.Receipt.Field.ProjectionMarker"] = "Projection marker",
            ["Tenants.Audit.Receipt.Field.Reference"] = "Audit reference",
            ["Tenants.Audit.Receipt.Field.CommandReference"] = "Command reference",
            ["Tenants.Audit.Receipt.ActionsLabel"] = "Audit receipt recovery actions",
            ["Tenants.Audit.Receipt.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Receipt.Action.Retry"] = "Retry",
            ["Tenants.Audit.Freshness.Current"] = "Current",
            ["Tenants.Audit.Freshness.Stale"] = "Stale",
            ["Tenants.Audit.Freshness.Unknown"] = "Unknown",
            ["Tenants.Audit.Receipt.State.Ready"] = "Audit evidence is ready to cite.",
            ["Tenants.RemoveMember.Recovery.Idle"] = "Open the preview when current projection evidence is available.",
            ["Tenants.RemoveMember.Recovery.Previewed"] = "Confirm deliberately, cancel, or continue read-only.",
            ["Tenants.RemoveMember.Recovery.RequestSent"] = "Wait for command status and projection refresh.",
            ["Tenants.RemoveMember.Recovery.Accepted"] = "Wait, refresh status, or continue read-only until projection confirms absence.",
            ["Tenants.RemoveMember.Recovery.ProjectionPending"] = "Refresh the member projection; do not treat the row as removed until absence is confirmed.",
            ["Tenants.RemoveMember.Recovery.Confirmed"] = "Continue read-only or inspect audit when evidence becomes available.",
            ["Tenants.RemoveMember.Recovery.Rejected"] = "Refresh projection evidence, request permission, start correction, or escalate.",
            ["Tenants.RemoveMember.Recovery.AlreadyApplied"] = "Continue read-only or restore intended access with a forward correction if needed.",
            ["Tenants.RemoveMember.Recovery.DuplicatePrevented"] = "Wait for the in-flight command, retry status lookup, or continue read-only.",
            ["Tenants.RemoveMember.Recovery.Failed"] = "Retry after checking current projection evidence or escalate.",
            ["Tenants.RemoveMember.Recovery.Degraded"] = "Wait, retry status lookup, inspect audit when available, or escalate.",
            ["Tenants.RemoveMember.Recovery.UnableToVerify"] = "Refresh, retry status lookup, continue read-only, or escalate.",
        };

        public LocalizedString this[string name]
            => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values.TryGetValue(name, out string? value) ? value : name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }
}
