using System.Globalization;

using Bunit;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Tenants.Members;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TruthState;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

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
            .Add(p => p.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-remove-member-flow']");
        cut.Find("[data-testid='tenants-remove-member-preview']");
        cut.FindAll("[data-testid='tenants-remove-member-preview-item']").Count.ShouldBe(10);
        cut.Find("[data-testid='tenants-remove-member-target-user-id']").TextContent.ShouldContain("reader-user");
        cut.Find("[data-testid='tenants-remove-member-current-role']").TextContent.ShouldContain("Tenant reader");
        cut.Find("[data-testid='tenants-remove-member-owner-context']").TextContent.ShouldContain("2 visible owners");
        cut.Find("[data-testid='tenants-remove-member-global-admin-risk']").TextContent.ShouldContain("known unknown");
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
            .Add(p => p.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-remove-member-owner-risk']").TextContent.ShouldContain("Last-owner");
        cut.Find("[data-testid='tenants-remove-member-confirm']").GetAttribute("disabled").ShouldBeNull();

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
            .Add(p => p.Freshness, TenantFreshnessState.Current)
            .Add(p => p.TargetGlobalAdministratorFriction, true));

        cut.Find("[data-testid='tenants-remove-member-global-admin-risk']").TextContent
            .ShouldContain("will not remove global-administrator authority");
        cut.Markup.ShouldNotContain("RemoveGlobalAdministrator", Case.Insensitive);
    }

    [Fact]
    public void Confirmation_submits_literal_user_id_and_confirms_only_after_absent_projection_evidence()
    {
        StubTenantCommandGateway gateway = new()
        {
            Submission = TenantCommandSubmissionResult.Accepted("message-1", "correlation-999"),
            Status = new TenantCommandStatusResult(CommandStatus.Completed),
        };
        RegisterServices(gateway);
        TenantDetail originalDetail = Detail("tenant.alpha");
        int projectionCalls = 0;

        IRenderedComponent<RemoveTenantMemberFlow> cut = Render<RemoveTenantMemberFlow>(parameters => parameters
            .Add(p => p.Detail, originalDetail)
            .Add(p => p.Member, new TenantMember("User/CaseSensitive.01", TenantRole.TenantReader))
            .Add(p => p.SurfaceKind, TenantDetailSurfaceKind.Ready)
            .Add(p => p.Freshness, TenantFreshnessState.Current)
            .Add(p => p.ProjectionEvidenceProvider, request => Task.FromResult<TenantDetail?>(++projectionCalls == 1
                ? Detail(
                    request.TenantId,
                    [
                        new TenantMember("owner-user", TenantRole.TenantOwner),
                        new TenantMember(request.UserId, TenantRole.TenantReader),
                    ])
                : Detail(
                    request.TenantId,
                    [new TenantMember("owner-user", TenantRole.TenantOwner)]))));

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
            .Add(p => p.Freshness, TenantFreshnessState.Current)
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
            .Add(p => p.Freshness, TenantFreshnessState.Current)
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
            .Add(p => p.Freshness, TenantFreshnessState.Current));

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
            .Add(p => p.Freshness, TenantFreshnessState.Current));

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
            .Add(p => p.Freshness, TenantFreshnessState.Current));

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => gateway.RemoveMemberCallCount.ShouldBe(1));

        cut.Find("form").Submit();

        gateway.RemoveMemberCallCount.ShouldBe(1);
        cut.Find("[data-testid='tenants-remove-member-state']").TextContent.ShouldContain("Duplicate");
        cut.Find("[data-testid='tenants-remove-member-live-region']").GetAttribute("aria-live").ShouldBe("assertive");

        pendingSubmission.SetResult(TenantCommandSubmissionResult.Failed("Command submission cancelled by the test."));
    }

    [Theory]
    [InlineData(TenantDetailSurfaceKind.Stale, TenantFreshnessState.Stale, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Ready, TenantFreshnessState.Unknown, "Refresh current tenant detail")]
    [InlineData(TenantDetailSurfaceKind.Degraded, TenantFreshnessState.Unknown, "not authorized")]
    public void Remove_flow_fails_closed_without_partial_preview_when_context_is_unavailable(
        TenantDetailSurfaceKind surfaceKind,
        TenantFreshnessState freshness,
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
        cut.Find("[data-testid='tenants-remove-member-unavailable-reason']").TextContent.ShouldContain(expectedReason, Case.Insensitive);
        cut.FindAll("[data-testid='tenants-remove-member-preview-item']").ShouldBeEmpty();

        cut.Find("[data-testid='tenants-remove-member-confirmation']").Change("reader-user");
        cut.Find("form").Submit();

        gateway.RemoveMemberCallCount.ShouldBe(0);
        cut.Find("[data-testid='tenants-remove-member-state']").TextContent.ShouldContain("Unable to verify");
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
            .Add(p => p.Freshness, TenantFreshnessState.Current)
            .Add(p => p.OnCloseRequested, () => closeCount++));

        cut.Find("[data-testid='tenants-remove-member-cancel']").Click();
        cut.Find("[data-testid='tenants-remove-member-flow']").KeyDown("Escape");

        closeCount.ShouldBe(2);
        gateway.RemoveMemberCallCount.ShouldBe(0);
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

        public Func<RemoveUserFromTenantCommandRequest, Task<TenantCommandSubmissionResult>>? RemoveMemberAsync { get; init; }

        public RemoveUserFromTenantCommandRequest? LastRemoveMemberRequest { get; private set; }

        public int RemoveMemberCallCount { get; private set; }

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(CreateTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(AddUserToTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(ChangeUserRoleCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(RemoveUserFromTenantCommandRequest request, CancellationToken cancellationToken = default)
        {
            RemoveMemberCallCount++;
            LastRemoveMemberRequest = request;
            return RemoveMemberAsync is null ? Task.FromResult(Submission) : RemoveMemberAsync(request);
        }

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(UpdateTenantCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(SetTenantConfigurationCommandRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(TenantCommandSubmissionResult.Failed("Not used."));

        public Task<TenantCommandStatusResult> GetStatusAsync(TenantCommandTrackingHandle handle, CancellationToken cancellationToken = default)
            => Task.FromResult(Status);
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
            ["Tenants.RemoveMember.Preview.RecoveryPath.Value"] = "Wait, refresh, inspect audit, or submit a forward correction.",
            ["Tenants.RemoveMember.Preview.AuditExpectation"] = "Audit expectation",
            ["Tenants.RemoveMember.Preview.AuditExpectation.Value"] = "Audit evidence is pending or unavailable until Epic 5.",
            ["Tenants.RemoveMember.Preview.KnownConsequences"] = "Known consequences",
            ["Tenants.RemoveMember.Preview.KnownConsequences.Value"] = "Membership is removed only after projection confirmation.",
            ["Tenants.RemoveMember.Preview.KnownUnknowns"] = "Known unknowns",
            ["Tenants.RemoveMember.Preview.KnownUnknowns.Value"] = "Session revocation, downstream enforcement, token invalidation, and global-administrator evidence are not proven.",
            ["Tenants.RemoveMember.Freshness.Current"] = "Current",
            ["Tenants.RemoveMember.Freshness.Stale"] = "Stale",
            ["Tenants.RemoveMember.Freshness.Unknown"] = "Unknown",
            ["Tenants.RemoveMember.OwnerContext.NoOwners"] = "0 visible owners.",
            ["Tenants.RemoveMember.OwnerContext.LastOwner"] = "{0} visible owner; removing this member can leave zero visible owners.",
            ["Tenants.RemoveMember.OwnerContext.MultipleOwners"] = "{0} visible owners.",
            ["Tenants.RemoveMember.OwnerRisk.LastOwner"] = "Last-owner warning: {0} visible owner remains.",
            ["Tenants.RemoveMember.OwnerRisk.Accessible"] = "Elevated last-owner removal warning for {0} visible owner.",
            ["Tenants.RemoveMember.GlobalAdminRisk.Known"] = "Platform administrator authority is reflected; this flow will not remove global-administrator authority.",
            ["Tenants.RemoveMember.GlobalAdminRisk.Unknown"] = "Global-administrator authority is a known unknown in this view.",
            ["Tenants.RemoveMember.GlobalAdminRisk.Accessible"] = "Platform authority risk context.",
            ["Tenants.RemoveMember.Confirmation.Label"] = "Type the target user id to confirm removal",
            ["Tenants.RemoveMember.Confirmation.Help"] = "Type {0} exactly. Cancel or Escape closes without submitting.",
            ["Tenants.RemoveMember.Confirm"] = "Remove member",
            ["Tenants.RemoveMember.Refresh"] = "Refresh status",
            ["Tenants.RemoveMember.Cancel"] = "Cancel",
            ["Tenants.RemoveMember.Lifecycle.Title"] = "Remove member command lifecycle",
            ["Tenants.RemoveMember.Validation.ConfirmationRequired"] = "Type {0} exactly before removing this member.",
            ["Tenants.RemoveMember.Unavailable.Authorization"] = "You are not authorized to remove members from this tenant.",
            ["Tenants.RemoveMember.Unavailable.Freshness"] = "Refresh current tenant detail before removing a member.",
            ["Tenants.RemoveMember.Unavailable.TenantLifecycle"] = "This tenant lifecycle state does not allow removing members.",
            ["Tenants.RemoveMember.Unavailable.CommandSurface"] = "Tenant command support is unavailable.",
            ["Tenants.RemoveMember.Unavailable.InFlight"] = "A tenant command is already in progress.",
            ["Tenants.RemoveMember.Unavailable.Identity"] = "Tenant id and target user id are required before the preview can open.",
            ["Tenants.RemoveMember.Unavailable.UnknownRole"] = "The current role is unknown.",
            ["Tenants.RemoveMember.Unavailable.TargetAbsent"] = "The target user is already absent.",
            ["Tenants.RemoveMember.Role.TenantOwner"] = "Tenant owner",
            ["Tenants.RemoveMember.Role.TenantContributor"] = "Tenant contributor",
            ["Tenants.RemoveMember.Role.TenantReader"] = "Tenant reader",
            ["Tenants.RemoveMember.Role.Unknown"] = "Unknown role",
            ["Tenants.RemoveMember.AlreadyApplied.BeforeSubmit"] = "User {0} is already absent; no remove command was submitted.",
            ["Tenants.RemoveMember.DuplicatePrevented.Message"] = "A remove-member command is already in progress for this flow.",
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
            ["Tenants.RemoveMember.State.Degraded"] = "Remove-member command result is degraded.",
            ["Tenants.RemoveMember.State.UnableToVerify"] = "Unable to verify the remove-member command result.",
            ["Tenants.RemoveMember.Audit.NotStarted"] = "Audit evidence not started.",
            ["Tenants.RemoveMember.Audit.AuditPending"] = "Audit evidence pending; no receipt is available in this story.",
            ["Tenants.RemoveMember.Audit.AuditDelayed"] = "Audit evidence delayed.",
            ["Tenants.RemoveMember.Audit.AuditUnavailable"] = "Audit evidence unavailable.",
            ["Tenants.RemoveMember.Audit.MissingSupport"] = "Audit evidence support is missing until Epic 5.",
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
            ["Tenants.RemoveMember.Recovery.Idle"] = "Open the preview when projection evidence is available.",
            ["Tenants.RemoveMember.Recovery.Previewed"] = "Confirm deliberately, cancel, or continue read-only.",
            ["Tenants.RemoveMember.Recovery.RequestSent"] = "Wait for command status.",
            ["Tenants.RemoveMember.Recovery.Accepted"] = "Wait, refresh status, or continue read-only.",
            ["Tenants.RemoveMember.Recovery.ProjectionPending"] = "Refresh the member projection.",
            ["Tenants.RemoveMember.Recovery.Confirmed"] = "Continue read-only or inspect audit when evidence becomes available.",
            ["Tenants.RemoveMember.Recovery.Rejected"] = "Refresh projection evidence, request permission, start correction, or escalate.",
            ["Tenants.RemoveMember.Recovery.AlreadyApplied"] = "Continue read-only or restore intended access with a forward correction if needed.",
            ["Tenants.RemoveMember.Recovery.DuplicatePrevented"] = "Wait for the in-flight command.",
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
