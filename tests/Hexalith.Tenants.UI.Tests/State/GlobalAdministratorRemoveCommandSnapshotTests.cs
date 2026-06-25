using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class GlobalAdministratorRemoveCommandSnapshotTests
{
    [Fact]
    public void Completed_status_requires_absent_target_projection_evidence_before_confirmation()
    {
        var intent = new RemoveGlobalAdministrator("User/CaseSensitive.01");
        GlobalAdministratorRemoveCommandSnapshot snapshot = GlobalAdministratorRemoveCommandSnapshot
            .Idle()
            .Preview(intent, CurrentRows("User/CaseSensitive.01", "other-admin"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        GlobalAdministratorRemoveCommandSnapshot stillVisible = snapshot.ConfirmProjection(Ready("User/CaseSensitive.01", "other-admin"));

        stillVisible.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        stillVisible.LastConfirmedProjection.ShouldNotBeNull().UserId.ShouldBe("User/CaseSensitive.01");
        stillVisible.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);

        GlobalAdministratorRemoveCommandSnapshot confirmed = snapshot.ConfirmProjection(Ready("other-admin"));

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedProjection.ShouldBeNull();
    }

    [Theory]
    [InlineData("LastGlobalAdministrator", "last global administrator")]
    [InlineData("GlobalAdministratorNotFound", "not a global administrator")]
    public void Remove_rejections_stay_rejected_without_success_or_member_removal_copy(
        string rejectionCode,
        string safeMessage)
    {
        var intent = new RemoveGlobalAdministrator("target-admin");
        GlobalAdministratorRemoveCommandSnapshot snapshot = GlobalAdministratorRemoveCommandSnapshot
            .Idle()
            .Preview(intent, CurrentRows("target-admin", "other-admin"))
            .RequestSent()
            .ApplySubmission(TenantCommandSubmissionResult.Rejected(safeMessage, rejectionCode));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.AlreadyApplied);
        snapshot.RejectionCode.ShouldBe(rejectionCode);
        snapshot.SafeMessage.ShouldNotBeNull().ShouldNotContain("member", Case.Insensitive);
        snapshot.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
    }

    [Fact]
    public void Signalr_nudge_cannot_confirm_remove_or_audit_success()
    {
        var intent = new RemoveGlobalAdministrator("target-admin");
        GlobalAdministratorRemoveCommandSnapshot snapshot = GlobalAdministratorRemoveCommandSnapshot
            .Idle()
            .Preview(intent, CurrentRows("target-admin", "other-admin"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .SignalRNudge();

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Preview_blocks_when_required_projection_items_are_missing()
    {
        GlobalAdministratorRemoveCommandSnapshot lastAdmin = GlobalAdministratorRemoveCommandSnapshot
            .Idle()
            .Preview(new RemoveGlobalAdministrator("target-admin"), CurrentRows("target-admin"));

        lastAdmin.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        lastAdmin.SafeMessage.ShouldNotBeNull().ShouldContain("last global administrator", Case.Insensitive);
        lastAdmin.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);

        GlobalAdministratorRemoveCommandSnapshot missingTarget = GlobalAdministratorRemoveCommandSnapshot
            .Idle()
            .Preview(new RemoveGlobalAdministrator("missing-admin"), CurrentRows("target-admin", "other-admin"));

        missingTarget.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        missingTarget.SafeMessage.ShouldNotBeNull().ShouldContain("not visible", Case.Insensitive);
    }

    private static IReadOnlyList<GlobalAdministratorRow> CurrentRows(params string[] userIds)
        => userIds.Select(userId => new GlobalAdministratorRow(userId, ReadModelFreshnessState.Current)).ToArray();

    private static GlobalAdministratorsSnapshot Ready(params string[] userIds)
        => GlobalAdministratorsSnapshot.Ready(
            CurrentRows(userIds),
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current);
}
