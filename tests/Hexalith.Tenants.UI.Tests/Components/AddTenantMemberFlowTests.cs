using System.Globalization;

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

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class AddTenantMemberFlowTests : FluentBunitContext
{
    [Fact]
    public void Audit_entry_with_invalid_detail_return_stays_disabled_instead_of_using_bare_detail()
    {
        RegisterServices(new StubTenantCommandGateway());
        ITenantsBffComposition composition = Substitute.For<ITenantsBffComposition>();
        composition.IsReadSurfaceConnected.Returns(true);
        Services.AddSingleton(composition);
        IRenderedComponent<CascadingValue<bool>> wrapper = Render<CascadingValue<bool>>(parameters => parameters
            .Add(p => p.Name, "AuditReadAvailable")
            .Add(p => p.Value, true)
            .AddChildContent<AddTenantMemberFlow>(child => child
                .Add(p => p.Detail, Detail("tenant.alpha"))
                .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
                .Add(p => p.Freshness, ReadModelFreshnessState.Current)));
        IRenderedComponent<AddTenantMemberFlow> cut = wrapper.FindComponent<AddTenantMemberFlow>();
        typeof(AddTenantMemberFlow).GetField("_snapshot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(cut.Instance, TenantAddMemberCommandSnapshot.Idle() with
            {
                AuditState = TenantCommandAuditState.AuditPending,
            });
        cut.Render();

        AngleSharp.Dom.IElement entry = cut.Find("[data-testid='tenants-audit-entrypoint']");
        entry.GetAttribute("href").ShouldBeNull();
        cut.Find("#" + entry.GetAttribute("aria-describedby")).TextContent
            .ShouldContain("invalid", Case.Insensitive);
    }

    [Fact]
    public void Add_member_flow_renders_stable_selectors_and_assignable_roles_only()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-add-member-flow']");
        cut.Find("[data-testid='tenants-add-member-user-id']");
        cut.Find("[data-testid='tenants-add-member-role']");
        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("disabled").ShouldBeNull();
        cut.Find("[data-testid='tenants-add-member-lifecycle']");
        cut.Find("[data-testid='tenants-add-member-state']");
        cut.Find("[data-testid='tenants-add-member-audit']");
        cut.Find("[data-testid='tenants-add-member-refresh']");
        cut.Find("[data-testid='tenants-add-member-role']").TextContent.ShouldContain("Tenant owner");
        cut.Find("[data-testid='tenants-add-member-role']").TextContent.ShouldContain("Tenant contributor");
        cut.Find("[data-testid='tenants-add-member-role']").TextContent.ShouldContain("Tenant reader");
        cut.Find("[data-testid='tenants-add-member-role']").TextContent.ShouldNotContain("Unknown");
        cut.Markup.ShouldNotContain("invite", Case.Insensitive);
        cut.Markup.ShouldNotContain("email", Case.Insensitive);
        cut.Markup.ShouldNotContain("Users navigation", Case.Insensitive);
    }

    [Fact]
    public void Submit_preserves_literal_user_id_and_does_not_confirm_without_member_projection_evidence()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-456"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);
        TenantDetail originalDetail = Detail("tenant.alpha");

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, originalDetail)
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, _ => Task.FromResult<TenantDetail?>(originalDetail)));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("User/CaseSensitive.01");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantContributor));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.LastAddMemberRequest.ShouldNotBeNull().UserId.ShouldBe("User/CaseSensitive.01"));
        gateway.LastAddMemberRequest.ShouldNotBeNull().Role.ShouldBe(TenantRole.TenantContributor);
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        cut.Instance.Snapshot.LastConfirmedMemberProjection.ShouldBeNull();
        cut.Find("[data-testid='tenants-add-member-state']").TextContent.ShouldContain("Projection pending");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation-456", Case.Insensitive);
    }

    [Fact]
    public void Projection_evidence_confirms_requested_member_role_without_exposing_internal_correlation_id()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-456"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);

        string liveProjectionVersion = "v1";
        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
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
                        new TenantMember(request.UserId, request.Role),
                    ]));
            }));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Confirmed));
        cut.Find("[data-testid='tenants-add-member-live-region']").GetAttribute("aria-live").ShouldBe("polite");
        // Projection confirmation is not audit proof and this flow has no in-panel audit verification.
        cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        cut.Find("[data-testid='tenants-add-member-audit'] [data-testid='tenants-audit-availability']")
            .GetAttribute("data-state").ShouldBe("missingsupport");
        cut.Find("[data-testid='tenants-add-member-audit']").TextContent.ShouldContain("Missing implementation support");
        cut.Find("[data-testid='tenants-add-member-audit']").TextContent.ShouldNotContain("Audit available");
        cut.Markup.ShouldNotContain("correlation-456", Case.Insensitive);
    }

    [Fact]
    public async Task Signalr_nudge_requeries_status_without_confirming_from_notification_alone()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-456"),
            Status = new TenantCommandStatusResult(CommandStatus.Received),
        };
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1")
            .Add(p => p.ProjectionVersionProvider, () => "v2")
            .Add(p => p.ProjectionEvidenceProvider, _ => Task.FromResult<TenantDetail?>(Detail(
                "tenant.alpha",
                [
                    new TenantMember("owner-user", TenantRole.TenantOwner),
                    new TenantMember("literal-user", TenantRole.TenantReader),
                ]))));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted));
        // Baseline was captured via provider as v2 at submit; keep evidence matching to prove Received cannot confirm.
        int statusCallsBeforeNudge = gateway.StatusCallCount;

        await cut.InvokeAsync(() => cut.Instance.HandleAuthoritativeRefreshNudgeAsync());

        cut.WaitForAssertion(() => gateway.StatusCallCount.ShouldBe(statusCallsBeforeNudge + 1));
        // Status remains Received → Accepted; matching evidence alone must not confirm without Completed.
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        cut.Instance.Snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void In_flight_retry_with_tracking_reuses_status_lookup_and_does_not_dispatch_again()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-456"),
            Status = new TenantCommandStatusResult(CommandStatus.Received),
        };
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1"));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.AddMemberCallCount.ShouldBe(1));
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        int statusCallsAfterSubmit = gateway.StatusCallCount;

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => gateway.AddMemberCallCount.ShouldBe(1));
        gateway.StatusCallCount.ShouldBeGreaterThan(statusCallsAfterSubmit);
        cut.Instance.Snapshot.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
    }

    [Fact]
    public async Task SignalR_nudge_during_unresolved_submission_keeps_request_and_activity_in_flight()
    {
        TaskCompletionSource<TenantCommandSubmissionResult> submission = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StubTenantCommandGateway gateway = new()
        {
            AddMemberAsync = _ => submission.Task,
        };
        RegisterServices(gateway);
        List<bool> activity = [];

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1")
            .Add(p => p.CommandActivityLease, isActive =>
            {
                activity.Add(isActive);
                return Task.FromResult(true);
            }));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.RequestSent));

        await cut.InvokeAsync(() => cut.Instance.HandleAuthoritativeRefreshNudgeAsync());

        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.RequestSent);
        gateway.StatusCallCount.ShouldBe(0);
        activity.ShouldBe([true]);

        submission.SetResult(TenantCommandSubmissionResult.Failed("Command submission cancelled by the test."));
        cut.WaitForAssertion(() => activity.ShouldBe([true, false]));
    }

    [Fact]
    public async Task Independent_nudge_overlapping_status_refresh_is_coalesced_into_a_later_lookup()
    {
        TaskCompletionSource overlappingStatusStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseOverlappingStatus = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int statusCalls = 0;
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"),
            StatusAsync = async _ =>
            {
                if (Interlocked.Increment(ref statusCalls) == 2)
                {
                    overlappingStatusStarted.SetResult();
                    await releaseOverlappingStatus.Task.ConfigureAwait(false);
                }

                return new TenantCommandStatusResult(CommandStatus.Received);
            },
        };
        RegisterServices(gateway);
        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => statusCalls.ShouldBe(1));
        cut.Find("[data-testid='tenants-add-member-refresh']").Click();
        await overlappingStatusStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await cut.Instance.HandleAuthoritativeRefreshNudgeAsync();
        releaseOverlappingStatus.SetResult();

        cut.WaitForAssertion(() => statusCalls.ShouldBe(3));
    }

    [Fact]
    public async Task Lost_tracking_refresh_maps_to_unable_to_verify_without_second_dispatch()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("01ARZ3NDEKTSV4RRFFQ69G5FAV", "correlation-456"),
            Status = new TenantCommandStatusResult(CommandStatus.Received),
        };
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1"));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted));

        System.Reflection.FieldInfo snapshotField = typeof(AddTenantMemberFlow)
            .GetField("_snapshot", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        TenantAddMemberCommandSnapshot snapshot = (TenantAddMemberCommandSnapshot)snapshotField.GetValue(cut.Instance)!;
        snapshotField.SetValue(cut.Instance, snapshot with { MessageId = null, CorrelationId = null });

        await cut.InvokeAsync(async () =>
        {
            await cut.Instance.HandleAuthoritativeRefreshNudgeAsync().ConfigureAwait(false);
        });
        cut.Render();

        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        gateway.AddMemberCallCount.ShouldBe(1);
        TenantCommandFlowGuard.RetainsCommandActivity(cut.Instance.Snapshot.State).ShouldBeFalse();
        cut.Find("[data-testid='tenants-add-member-continue-read-only']");

        cut.Find("form").Submit();
        gateway.AddMemberCallCount.ShouldBe(1);
    }

    [Fact]
    public void Already_member_rejection_remains_rejected_without_success_copy()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Rejected(
                "This user is already a member of the tenant. Refresh the member table before trying another action.",
                "UserAlreadyInTenant"),
        };
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("owner-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantOwner));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected));
        cut.Find("[data-testid='tenants-add-member-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Find("[data-testid='tenants-add-member-safe-message']").TextContent.ShouldContain("already a member");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
        gateway.LastAddMemberRequest.ShouldNotBeNull().UserId.ShouldBe("owner-user");
        // A rejection stored nothing: no audit state is implied and the shared control stays hidden.
        cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.NotStarted);
        cut.FindAll("[data-testid='tenants-audit-availability']").ShouldBeEmpty();
    }

    [Fact]
    public void Validation_requires_user_id_and_explicit_assignable_role_before_gateway_submission()
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("form").Submit();

        cut.Find("[data-testid='tenants-add-member-validation']").TextContent.ShouldContain("User id is required");
        gateway.AddMemberCallCount.ShouldBe(0);

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("   ");
        cut.Find("form").Submit();

        cut.Find("[data-testid='tenants-add-member-validation']").TextContent.ShouldContain("User id is required");
        gateway.AddMemberCallCount.ShouldBe(0);

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        cut.Find("form").Submit();

        cut.Find("[data-testid='tenants-add-member-validation']").TextContent.ShouldContain("Select TenantOwner");
        gateway.AddMemberCallCount.ShouldBe(0);
    }

    [Fact]
    public void Ambiguous_failure_reuses_message_id_only_for_the_exact_same_intent()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Failed("Submission outcome is ambiguous.") with
            {
                MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            },
        };
        RegisterServices(gateway);
        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();
        cut.Find("form").Submit();

        gateway.LastAddMemberMessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("different-user");
        cut.Find("form").Submit();

        gateway.LastAddMemberMessageId.ShouldBeNull();
        gateway.AddMemberCallCount.ShouldBe(3);
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
        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify));
        int statusCalls = gateway.StatusCallCount;

        cut.Find("form").Submit();

        gateway.AddMemberCallCount.ShouldBe(1);
        gateway.StatusCallCount.ShouldBe(statusCalls + 1);
    }

    [Theory]
    [InlineData(false, TenantStatus.Active, "Tenant command support is unavailable")]
    [InlineData(true, TenantStatus.Disabled, "lifecycle state does not allow")]
    [InlineData(true, TenantStatus.Unknown, "lifecycle state does not allow")]
    public void Add_member_fails_closed_when_command_surface_or_tenant_lifecycle_is_unavailable(
        bool isCommandSurfaceAvailable,
        TenantStatus tenantStatus,
        string expectedReason)
    {
        StubTenantCommandGateway gateway = new();
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha") with { Status = tenantStatus })
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsCommandSurfaceAvailable, isCommandSurfaceAvailable));

        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-add-member-unavailable-reason']").TextContent.ShouldContain(expectedReason, Case.Insensitive);
        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();

        gateway.AddMemberCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-add-member-state']").TextContent.ShouldContain("Unable to verify");
        cut.Find("[data-testid='tenants-add-member-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
    }

    [Fact]
    public void Duplicate_submit_while_add_member_command_is_in_flight_is_blocked_before_gateway_submission()
    {
        TaskCompletionSource<TenantCommandSubmissionResult> pendingSubmission = new();
        StubTenantCommandGateway gateway = new()
        {
            AddMemberAsync = _ => pendingSubmission.Task,
        };
        RegisterServices(gateway);

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => gateway.AddMemberCallCount.ShouldBe(1));

        cut.Find("form").Submit();

        gateway.AddMemberCallCount.ShouldBe(1);
        cut.Find("[data-testid='tenants-add-member-unavailable-reason']").TextContent.ShouldContain("already in progress");
        cut.Find("[data-testid='tenants-add-member-state']").TextContent.ShouldContain("Unable to verify");
        gateway.LastAddMemberRequest.ShouldNotBeNull().UserId.ShouldBe("literal-user");
        cut.Instance.Snapshot.LastConfirmedMemberProjection.ShouldBeNull();

        pendingSubmission.SetResult(TenantCommandSubmissionResult.Failed("Command submission cancelled by the test."));
    }

    [Theory]
    [InlineData(TenantDetailSurfaceKind.Stale, ReadModelFreshnessState.Stale, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Ready, ReadModelFreshnessState.Unknown, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Degraded, ReadModelFreshnessState.Current, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Unavailable, ReadModelFreshnessState.Current, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Unknown, ReadModelFreshnessState.Current, "Refresh current tenant detail")]
    public void Add_member_fails_closed_when_truth_or_authorization_is_not_eligible(
        TenantDetailSurfaceKind surfaceKind,
        ReadModelFreshnessState freshness,
        string expectedReason)
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, surfaceKind)
            .Add(p => p.Freshness, freshness));

        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("disabled").ShouldNotBeNull();
        string reason = cut.Find("[data-testid='tenants-add-member-unavailable-reason']").TextContent;
        reason.ShouldContain(expectedReason, Case.Insensitive);
        reason.ShouldNotContain("not authorized", Case.Insensitive);
        cut.Find("[data-testid='tenants-add-member-lifecycle']").GetAttribute("tabindex").ShouldBe("-1");
    }

    [Fact]
    public void Add_member_true_authorization_failure_still_renders_permission_reason()
    {
        RegisterServices(new StubTenantCommandGateway());

        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.IsAuthorized, false));

        cut.Find("[data-testid='tenants-add-member-submit']").GetAttribute("disabled").ShouldNotBeNull();
        cut.Find("[data-testid='tenants-add-member-unavailable-reason']").TextContent
            .ShouldContain("not authorized", Case.Insensitive);
    }

    [Fact]
    public async Task Refresh_clicks_merged_into_a_running_lookup_wait_for_its_replay_and_never_exhaust_the_retry_limit()
    {
        TaskCompletionSource nudgeLookupStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseNudgeLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int statusCalls = 0;
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"),
            StatusAsync = async _ =>
            {
                if (Interlocked.Increment(ref statusCalls) == 2)
                {
                    nudgeLookupStarted.SetResult();
                    await releaseNudgeLookup.Task.ConfigureAwait(false);
                }

                return new TenantCommandStatusResult(CommandStatus.EventsStored);
            },
        };
        RegisterServices(gateway);
        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1"));

        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending));
        const string refresh = "[data-testid='tenants-add-member-audit'] [data-recovery-verb='refresh']";
        cut.WaitForAssertion(() => cut.Find(refresh), TimeSpan.FromSeconds(5));

        // A SignalR nudge starts a slow lookup; the shared control's Refresh then merges into it.
        Task nudge = cut.InvokeAsync(() => cut.Instance.HandleAuthoritativeRefreshNudgeAsync());
        await nudgeLookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cut.Find(refresh).Click();
        cut.Find(refresh).Click();
        cut.Find(refresh).Click();

        // The merged click waits for the running lookup and its replay, so the repeated clicks are one pending
        // recovery: none of them counted as an unchanged retry while the lookup was still running.
        cut.Find(refresh);
        cut.FindAll("[data-testid='tenants-audit-availability-retry-limit']").ShouldBeEmpty();

        releaseNudgeLookup.SetResult();
        await nudge.WaitAsync(TimeSpan.FromSeconds(5));
        SpinWait.SpinUntil(() => Volatile.Read(ref statusCalls) == 3, TimeSpan.FromSeconds(5)).ShouldBeTrue();
        cut.WaitForAssertion(() => cut.FindAll(refresh).ShouldHaveSingleItem(), TimeSpan.FromSeconds(5));
        cut.FindAll("[data-testid='tenants-audit-availability-retry-limit']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Refresh_is_offered_only_while_the_attempt_can_be_requeried(bool requeryable)
    {
        // A failure reported after the message was sent carries no correlation id, so nothing can be re-queried.
        StubTenantCommandGateway gateway = new()
        {
            Submission = requeryable
                ? TenantCommandSubmissionResult.Accepted("message-1", "correlation-1")
                : TenantCommandSubmissionResult.Failed("Submission failed before it could be verified.") with
                {
                    MessageId = "message-1",
                },
            Status = new TenantCommandStatusResult(CommandStatus.EventsStored),
        };
        RegisterServices(gateway);
        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.ProjectionVersion, "v1"));
        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Instance.Snapshot.AuditState.ShouldBe(requeryable
            ? TenantCommandAuditState.AuditPending
            : TenantCommandAuditState.AuditUnavailable));
        cut.WaitForAssertion(
            () => cut.FindAll("[data-testid='tenants-add-member-audit'] [data-recovery-verb='refresh']").Count.ShouldBe(requeryable ? 1 : 0),
            TimeSpan.FromSeconds(5));
        cut.Find("[data-testid='tenants-add-member-audit'] [data-testid='tenants-audit-availability']");
    }

    [Fact]
    public void Continue_read_only_after_confirmation_moves_focus_to_the_lifecycle_section()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1),
        };
        RegisterServices(gateway);
        string liveProjectionVersion = "v1";
        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
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
                        new TenantMember(request.UserId, request.Role),
                    ]));
            }));
        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport));

        cut.Find("[data-testid='tenants-add-member-audit'] [data-recovery-verb='continuereadonly']").Click();

        // Continue read-only unmounts the control and the focused button; focus lands on the lifecycle section.
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Idle));
        cut.FindAll("[data-testid='tenants-add-member-audit'] [data-testid='tenants-audit-availability']").ShouldBeEmpty();
        string lifecycleReferenceId = ((ElementReference)typeof(AddTenantMemberFlow)
            .GetField("_lifecycleElement", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(cut.Instance)!).Id;
        cut.WaitForAssertion(() => LastFocusedReferenceId().ShouldBe(lifecycleReferenceId));
    }

    [Theory]
    [InlineData(true, TenantCommandAuditState.AuditUnavailable)]
    [InlineData(false, TenantCommandAuditState.NotStarted)]
    public void Retry_refused_by_the_activity_lease_reports_the_possibly_delivered_attempt_as_unverifiable(
        bool isRetry,
        TenantCommandAuditState expectedAuditState)
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Failed("Submission outcome is ambiguous.") with
            {
                MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            },
        };
        RegisterServices(gateway);
        int acquisitions = 0;
        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.CommandActivityLease, active => Task.FromResult(!active || (isRetry && ++acquisitions != 2))));
        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));

        if (isRetry)
        {
            cut.Find("form").Submit();
            cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Failed));
            cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable);
        }

        // The retry reuses an identity that may already have reached the server; the refused lease blocks it
        // before dispatch, and the audit dimension reports the unknown status instead of "not started". A first
        // attempt refused before dispatch sent nothing, so no audit state is implied.
        cut.Find("form").Submit();

        // A refused retry keeps the failed attempt it retried, identity included; a refused first attempt is blocked.
        // Both carry the in-flight refusal the flow assigns, which the failed attempt's message never matches.
        TenantCommandLifecycleState expectedState = isRetry
            ? TenantCommandLifecycleState.Failed
            : TenantCommandLifecycleState.UnableToVerify;
        cut.WaitForAssertion(() =>
        {
            cut.Instance.Snapshot.State.ShouldBe(expectedState);
            cut.Instance.Snapshot.SafeMessage.ShouldBe("A tenant command is already in progress.");
        });
        cut.Instance.Snapshot.MessageId.ShouldBe(isRetry ? "01ARZ3NDEKTSV4RRFFQ69G5FAV" : null);
        cut.Instance.Snapshot.AuditState.ShouldBe(expectedAuditState);
        cut.FindAll("[data-testid='tenants-add-member-audit'] [data-testid='tenants-audit-availability']").Count
            .ShouldBe(isRetry ? 1 : 0);
        gateway.AddMemberCallCount.ShouldBe(isRetry ? 1 : 0);

        if (isRetry)
        {
            // Once the lease is granted again, the next submit re-dispatches the same identity instead of minting one.
            cut.Find("form").Submit();
            cut.WaitForAssertion(() => gateway.AddMemberCallCount.ShouldBe(2));
            gateway.LastAddMemberMessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        }
    }

    [Fact]
    public void Retry_whose_command_gateway_disappears_after_the_lease_reports_the_possibly_delivered_attempt_as_unverifiable()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Failed("Submission outcome is ambiguous.") with
            {
                MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            },
        };
        RegisterServices(gateway);
        bool gatewayAvailable = true;
        Services.AddTransient<ITenantCommandGateway>(_ => gatewayAvailable ? gateway : null!);
        int acquisitions = 0;
        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current)
            .Add(p => p.CommandActivityLease, active =>
            {
                // The retry takes the lease, and the command surface disappears before dispatch.
                if (active && ++acquisitions == 2)
                {
                    gatewayAvailable = false;
                }

                return Task.FromResult(true);
            }));
        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Failed));

        cut.Find("form").Submit();

        // The blocked retry keeps the failed attempt it retried: that identity may already have reached the
        // server, so its status is unknown and it stays the identity the next submit reuses.
        cut.WaitForAssertion(() =>
        {
            cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Failed);
            cut.Instance.Snapshot.SafeMessage.ShouldBe("Tenant command support is unavailable.");
        });
        cut.Instance.Snapshot.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable);
        gateway.AddMemberCallCount.ShouldBe(1);

        gatewayAvailable = true;
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => gateway.AddMemberCallCount.ShouldBe(2));
        gateway.LastAddMemberMessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
    }

    [Fact]
    public void Audit_refresh_that_resolves_to_not_started_moves_focus_to_the_lifecycle_section()
    {
        int statusCalls = 0;
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"),
            // The first lookup cannot be read; the user's Refresh then finds the command still processing.
            StatusAsync = _ => Task.FromResult(++statusCalls == 1
                ? TenantCommandStatusResult.Unknown("Command status is unavailable.")
                : new TenantCommandStatusResult(CommandStatus.Processing, HasVerifiedCommandIdentity: true)),
        };
        RegisterServices(gateway);
        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));
        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable));
        string lifecycleReferenceId = ((ElementReference)typeof(AddTenantMemberFlow)
            .GetField("_lifecycleElement", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(cut.Instance)!).Id;
        int focusCallsBeforeRefresh = FocusCallCount();

        cut.Find("[data-testid='tenants-add-member-audit'] [data-recovery-verb='refresh']").Click();

        // The polite NotStarted state unmounts the control and its focused Refresh button, so focus lands on the
        // lifecycle section instead of falling back to the document body.
        cut.WaitForAssertion(() => cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.NotStarted));
        cut.FindAll("[data-testid='tenants-add-member-audit'] [data-testid='tenants-audit-availability']").ShouldBeEmpty();
        cut.WaitForAssertion(() => FocusCallCount().ShouldBeGreaterThan(focusCallsBeforeRefresh));
        LastFocusedReferenceId().ShouldBe(lifecycleReferenceId);
    }

    [Fact]
    public void Retry_failed_before_dispatch_keeps_the_reused_identity_unverifiable()
    {
        int submissions = 0;
        StubTenantCommandGateway gateway = new()
        {
            // The first attempt failed after the message was sent; the retry of that identity then fails before
            // dispatch and reports no message id of its own.
            AddMemberAsync = _ => Task.FromResult(++submissions == 1
                ? TenantCommandSubmissionResult.Failed("Submission outcome is ambiguous.") with
                {
                    MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                }
                : TenantCommandSubmissionResult.FailedWithKey("Tenants.Commands.Unavailable.InvalidTrackingReference")),
        };
        RegisterServices(gateway);
        IRenderedComponent<AddTenantMemberFlow> cut = Render<AddTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, Detail("tenant.alpha"))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, ReadModelFreshnessState.Current));
        cut.Find("[data-testid='tenants-add-member-user-id']").Change("literal-user");
        FluentSelectInterop.ChangeFluentSelect(cut, "tenants-add-member-role", nameof(TenantRole.TenantReader));
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable));

        cut.Find("form").Submit();

        // The retry sent nothing, but the earlier attempt with the same identity may have reached the server: the
        // audit dimension stays unknown instead of disappearing as "not started".
        cut.WaitForAssertion(() => gateway.AddMemberCallCount.ShouldBe(2));
        gateway.LastAddMemberMessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        cut.Instance.Snapshot.State.ShouldBe(TenantCommandLifecycleState.Failed);
        cut.Instance.Snapshot.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        cut.Instance.Snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable);
        cut.Find("[data-testid='tenants-add-member-audit'] [data-testid='tenants-audit-availability']")
            .GetAttribute("data-state").ShouldBe("unavailable");
    }

    private int FocusCallCount()
        => JSInterop.Invocations.Count(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase));

    private string LastFocusedReferenceId()
        => JSInterop.Invocations
            .Where(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase))
            .Select(invocation => invocation.Arguments.FirstOrDefault())
            .OfType<ElementReference>()
            .LastOrDefault()
            .Id ?? string.Empty;

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

        public Func<TenantCommandTrackingHandle, Task<TenantCommandStatusResult>>? StatusAsync { get; init; }

        public Func<AddUserToTenant, Task<TenantCommandSubmissionResult>>? AddMemberAsync { get; init; }

        public AddUserToTenant? LastAddMemberRequest { get; private set; }

        public string? LastAddMemberMessageId { get; private set; }

        public int AddMemberCallCount { get; private set; }

        public int StatusCallCount { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenant request, string? messageId = null, CancellationToken cancellationToken = default)
        {
            AddMemberCallCount++;
            LastAddMemberRequest = request;
            LastAddMemberMessageId = messageId;
            return AddMemberAsync is null ? Task.FromResult(Submission) : AddMemberAsync(request);
        }

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRole request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenant request, string? messageId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfiguration request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
        {
            StatusCallCount++;
            return StatusAsync is null ? Task.FromResult(Status) : StatusAsync(handle);
        }
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.AddMember.Title"] = "Add tenant member",
            ["Tenants.AddMember.Description"] = "Add a literal user id to tenant {0}. Current visible owner count is {1}.",
            ["Tenants.AddMember.UserId.Label"] = "User id",
            ["Tenants.AddMember.UserId.Help"] = "Use the exact caller-supplied user id.",
            ["Tenants.AddMember.Role.Label"] = "Tenant role",
            ["Tenants.AddMember.Role.Placeholder"] = "Select a role",
            ["Tenants.AddMember.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.AddMember.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.AddMember.Role.TenantReader"] = "Tenant reader",
            ["Tenants.AddMember.Submit"] = "Add member",
            ["Tenants.AddMember.Refresh"] = "Refresh status",
            ["Tenants.AddMember.Lifecycle.Title"] = "Add member command lifecycle",
            ["Tenants.AddMember.Validation.UserIdRequired"] = "User id is required.",
            ["Tenants.AddMember.Validation.RoleRequired"] = "Select TenantOwner, TenantContributor, or TenantReader before adding a member.",
            ["Tenants.AddMember.Unavailable.Authorization"] = "You are not authorized to add members to this tenant.",
            ["Tenants.AddMember.Unavailable.Freshness"] = "Refresh current tenant detail before adding a member.",
            ["Tenants.AddMember.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow adding members.",
            ["Tenants.AddMember.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.AddMember.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.Members.Submit.TrackingLost"] = "This attempt can no longer be tracked, so it cannot be verified or resubmitted here. Refresh the tenant and check the member list before trying again.",
            ["Tenants.AddMember.State.Idle"] = "No add-member command submitted.",
            ["Tenants.AddMember.State.RequestSent"] = "Add-member request sent.",
            ["Tenants.AddMember.State.Accepted"] = "Accepted by EventStore; waiting for member processing.",
            ["Tenants.AddMember.State.ProjectionPending"] = "Projection pending; the member role is not confirmed visible yet.",
            ["Tenants.AddMember.State.Confirmed"] = "Projection confirmed the user is a tenant member with the requested role.",
            ["Tenants.AddMember.State.Rejected"] = "Add-member command rejected.",
            ["Tenants.AddMember.State.Failed"] = "Add-member command submission failed.",
            ["Tenants.AddMember.State.Degraded"] = "Add-member command result is degraded and needs review.",
            ["Tenants.AddMember.State.UnableToVerify"] = "Unable to verify the add-member command result.",
            ["Tenants.AddMember.Confirm.UnableToVerify.MissingProvenance"] = "Member projection already matched without provenance that this attempt advanced it. Refresh status or continue read-only.",
            ["Tenants.AddMember.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.EntryPoint.Accessible.Command"] = "Inspect audit for tenant {1} ({0})",
            ["Tenants.Audit.EntryPoint.CommandReason"] = "Command-specific proof is not available here; open the tenant audit list and use the visible audit state.",
            ["Tenants.Audit.EntryPoint.Label"] = "Audit evidence",
            ["Tenants.Audit.EntryPoint.Unavailable.ScopeRequired"] = "Tenant scope is required before audit evidence can be opened.",
            ["Tenants.Audit.EntryPoint.Unavailable.InvalidContext"] = "The audit context is invalid. Return to the originating page and select a current tenant.",
            ["Tenants.Audit.EntryPoint.Unavailable.Disconnected"] = "The audit read service is disconnected. Refresh when the connection returns.",
            ["Tenants.Audit.EntryPoint.Unavailable.StaleScope"] = "Refresh tenant scope before opening audit evidence.",
            ["Tenants.Audit.Availability.Action.ContinueReadOnly"] = "Continue read-only",
            ["Tenants.Audit.Availability.Action.InspectAudit"] = "Inspect audit",
            ["Tenants.Audit.Availability.Action.Refresh"] = "Retry status lookup",
            ["Tenants.Audit.Availability.ActionsLabel"] = "Audit availability recovery actions",
            ["Tenants.Audit.Availability.Reason.MissingSupport"] = "In-panel audit verification is not available for this command, so this panel cannot match an audit record to the attempt. The recorded outcome above is unchanged.",
            ["Tenants.Audit.Availability.Reason.Unavailable"] = "The audit status could not be read or verified after the command was sent. This does not mean the record does not exist, and no proof is claimed.",
            ["Tenants.Audit.Availability.State.Delayed"] = "Audit delayed",
            ["Tenants.Audit.Availability.State.MissingSupport"] = "Missing implementation support",
            ["Tenants.Audit.Availability.State.Pending"] = "Audit pending",
            ["Tenants.Audit.Availability.State.Unavailable"] = "Audit unavailable",
            ["Tenants.Audit.Availability.State.Available"] = "Audit available",
            ["Tenants.Audit.Availability.Reason.Pending"] = "The command's events are stored, but its audit record is not readable yet. It normally appears shortly, and no proof is claimed until it does.",
            ["Tenants.Audit.Availability.Reason.Delayed"] = "The audit record is taking longer than expected to become readable. No proof is claimed until it can be read.",
            ["Tenants.Audit.Availability.RetryLimit"] = "Repeated retries left this state unchanged, so retrying is no longer offered here.",
            ["Tenants.Audit.Receipt.Availability.Unavailable.Reason"] = "This audit read could not verify the requested evidence. This does not mean the record does not exist, and the recorded outcome is unchanged.",
            ["Tenants.Audit.Recovery.Action.Escalate"] = "Escalate without diagnostics",
        };

        public LocalizedString this[string name]
            => new(name, Values[name]);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(CultureInfo.CurrentCulture, Values[name], arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(v => new LocalizedString(v.Key, v.Value));
    }
}
