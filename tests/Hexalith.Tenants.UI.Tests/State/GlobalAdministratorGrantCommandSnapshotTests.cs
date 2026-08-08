using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
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
        // Ready() defaults IsCompleteEvidence=false, so absence hits the page-scoped arm.
        withoutEvidence.SafeMessage.ShouldBe("Tenants.GlobalAdministrators.Grant.Confirm.PageScoped");

        GlobalAdministratorGrantCommandSnapshot confirmed = snapshot.ConfirmProjection(Ready("User/CaseSensitive.01") with { IsCompleteEvidence = true });

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedProjection.ShouldNotBeNull().UserId.ShouldBe("User/CaseSensitive.01");
    }

    [Fact]
    public void Page_scoped_absence_is_distinguished_from_a_failed_verification_read()
    {
        GlobalAdministratorGrantCommandSnapshot pending = GlobalAdministratorGrantCommandSnapshot
            .Idle()
            .RequestSent(new SetGlobalAdministrator("target-admin"))
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        GlobalAdministratorsSnapshot pageScoped = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current)],
            nextCursor: "protected-next",
            hasMore: true,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            ProjectionVersion = "projection-v1",
        };
        pageScoped.IsMutationEvidenceBacked.ShouldBeTrue();

        GlobalAdministratorGrantCommandSnapshot pageScopedResult = pending.ConfirmProjection(pageScoped);

        pageScopedResult.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        pageScopedResult.SafeMessage.ShouldBe("Tenants.GlobalAdministrators.Grant.Confirm.PageScoped");

        GlobalAdministratorsSnapshot completeAbsence = Ready("other-admin") with { IsCompleteEvidence = true };
        GlobalAdministratorGrantCommandSnapshot completeResult = pending.ConfirmProjection(completeAbsence);

        completeResult.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        completeResult.SafeMessage.ShouldBe("Tenants.GlobalAdministrators.Grant.Confirm.DidNotConfirm");
        completeResult.SafeMessage.ShouldNotBe(pageScopedResult.SafeMessage);
    }

    // A projection re-query whose Current freshness came only from the legacy X-Hexalith-Is-Stale
    // compatibility signal carries no lifecycle evidence, so it cannot certify that the grant reached the
    // projection even when the target row is present.
    [Fact]
    public void Completed_status_without_projection_lifecycle_evidence_stays_unable_to_verify()
    {
        var intent = new SetGlobalAdministrator("target-admin");
        GlobalAdministratorGrantCommandSnapshot snapshot = GlobalAdministratorGrantCommandSnapshot
            .Idle()
            .RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        GlobalAdministratorGrantCommandSnapshot result = snapshot.ConfirmProjection(
            Ready("target-admin") with { Lifecycle = ProjectionLifecycleState.Unknown });

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.LastConfirmedProjection.ShouldBeNull();
        result.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
        result.SafeMessage.ShouldBe("Tenants.GlobalAdministrators.Grant.Confirm.EvidenceRequired");
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
    [InlineData(GlobalAdministratorsSurfaceKind.Stale)]
    [InlineData(GlobalAdministratorsSurfaceKind.Unknown)]
    [InlineData(GlobalAdministratorsSurfaceKind.Degraded)]
    public void Target_presence_on_non_current_projection_cannot_confirm_grant(GlobalAdministratorsSurfaceKind kind)
    {
        GlobalAdministratorGrantCommandSnapshot pending = GlobalAdministratorGrantCommandSnapshot
            .Idle()
            .RequestSent(new SetGlobalAdministrator("target-user"))
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));
        GlobalAdministratorRow[] rows = [new("target-user", ReadModelFreshnessState.Stale)];
        GlobalAdministratorsSnapshot evidence = kind switch
        {
            GlobalAdministratorsSurfaceKind.Stale => GlobalAdministratorsSnapshot.Stale(rows, null, false, "\"etag\""),
            GlobalAdministratorsSurfaceKind.Unknown => GlobalAdministratorsSnapshot.Unknown(rows, null, false, "\"etag\""),
            GlobalAdministratorsSurfaceKind.Degraded => GlobalAdministratorsSnapshot.Degraded(rows, GlobalAdministratorsReason.ProjectionDegraded, "\"etag\""),
            _ => throw new InvalidOperationException(),
        };

        GlobalAdministratorGrantCommandSnapshot result = pending.ConfirmProjection(evidence);

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.LastConfirmedProjection.ShouldBeNull();
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

    // Lifecycle is stated explicitly because ConfirmProjection gates on IsMutationEvidenceBacked: a snapshot
    // whose Current freshness carries no projection lifecycle evidence cannot certify the grant.
    private static GlobalAdministratorsSnapshot Ready(string userId)
        => GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow(userId, ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current };
}
