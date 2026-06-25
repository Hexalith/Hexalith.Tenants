using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class GlobalAdministratorGrantCommandSnapshotTests
{
    [Fact]
    public void Completed_status_requires_target_projection_evidence_before_confirmation()
    {
        var intent = new SetGlobalAdministrator("User/CaseSensitive.01");
        GlobalAdministratorGrantCommandSnapshot snapshot = GlobalAdministratorGrantCommandSnapshot
            .Idle()
            .RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        GlobalAdministratorGrantCommandSnapshot withoutEvidence = snapshot.ConfirmProjection(Ready("other-user"));

        withoutEvidence.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        withoutEvidence.LastConfirmedProjection.ShouldBeNull();
        withoutEvidence.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);

        GlobalAdministratorGrantCommandSnapshot confirmed = snapshot.ConfirmProjection(Ready("User/CaseSensitive.01"));

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedProjection.ShouldNotBeNull().UserId.ShouldBe("User/CaseSensitive.01");
    }

    [Fact]
    public void Already_admin_rejection_stays_rejected_and_not_already_applied()
    {
        var intent = new SetGlobalAdministrator("existing-admin");
        GlobalAdministratorGrantCommandSnapshot snapshot = GlobalAdministratorGrantCommandSnapshot
            .Idle()
            .RequestSent(intent)
            .ApplySubmission(TenantCommandSubmissionResult.Rejected(
                "This user is already a global administrator.",
                "GlobalAdministratorAlreadyExists"));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.AlreadyApplied);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        snapshot.RejectionCode.ShouldBe("GlobalAdministratorAlreadyExists");
        snapshot.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
    }

    [Fact]
    public void Signalr_nudge_cannot_confirm_grant_or_audit_success()
    {
        var intent = new SetGlobalAdministrator("target-user");
        GlobalAdministratorGrantCommandSnapshot snapshot = GlobalAdministratorGrantCommandSnapshot
            .Idle()
            .RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .SignalRNudge();

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
        snapshot.LastConfirmedProjection.ShouldBeNull();
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Theory]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected, TenantCommandAuditState.AuditUnavailable)]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded, TenantCommandAuditState.AuditDelayed)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify, TenantCommandAuditState.AuditDelayed)]
    public void Projection_evidence_cannot_convert_terminal_non_success_states_to_confirmed(
        CommandStatus status,
        TenantCommandLifecycleState expectedState,
        TenantCommandAuditState expectedAudit)
    {
        var intent = new SetGlobalAdministrator("target-user");
        GlobalAdministratorGrantCommandSnapshot snapshot = GlobalAdministratorGrantCommandSnapshot
            .Idle()
            .RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe non-success message."));

        GlobalAdministratorGrantCommandSnapshot withProjectionEvidence = snapshot.ConfirmProjection(Ready("target-user"));

        withProjectionEvidence.State.ShouldBe(expectedState);
        withProjectionEvidence.AuditState.ShouldBe(expectedAudit);
        withProjectionEvidence.LastConfirmedProjection.ShouldBeNull();
    }

    private static GlobalAdministratorsSnapshot Ready(string userId)
        => GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow(userId, ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current);
}
