using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
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

    [Fact]
    public void Incomplete_current_absence_cannot_confirm_removal()
    {
        GlobalAdministratorRemoveCommandSnapshot pending = GlobalAdministratorRemoveCommandSnapshot
            .Idle()
            .Preview(new RemoveGlobalAdministrator("target-admin"), CurrentRows("target-admin", "other-admin"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));
        GlobalAdministratorsSnapshot incompleteAbsence = GlobalAdministratorsSnapshot.Ready(
            CurrentRows("other-admin"),
            nextCursor: "protected-next",
            hasMore: true,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current);

        GlobalAdministratorRemoveCommandSnapshot result = pending.ConfirmProjection(incompleteAbsence);

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.LastConfirmedProjection.ShouldBeNull();
        result.SafeMessage.ShouldBe("Tenants.GlobalAdministrators.Remove.Confirm.EvidenceRequired");
    }

    /// <summary>
    /// A page-scoped absence and a failed read both stay unverified, but must not read the same.
    /// </summary>
    /// <remarks>
    /// The re-query reads page one only, so on any deployment with more global administrators than one page
    /// holds, <c>HasMore</c> is permanently true and every successful removal reached the "complete
    /// projection evidence is required" arm -- reading as though something had gone wrong. A good, current,
    /// projection-backed page that simply does not span the population is page-scoped evidence, not a failed
    /// read. Collapsing both branches to the old single message survived the suite; no test anywhere
    /// contained the new string.
    /// </remarks>
    [Fact]
    public void Page_scoped_absence_is_distinguished_from_a_failed_verification_read()
    {
        GlobalAdministratorRemoveCommandSnapshot pending = GlobalAdministratorRemoveCommandSnapshot
            .Idle()
            .Preview(new RemoveGlobalAdministrator("target-admin"), CurrentRows("target-admin", "other-admin"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        // A healthy page-one read that simply cannot span the population.
        GlobalAdministratorsSnapshot pageScoped = GlobalAdministratorsSnapshot.Ready(
            CurrentRows("other-admin"),
            nextCursor: "protected-next",
            hasMore: true,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            ProjectionVersion = "projection-v1",
        };
        pageScoped.IsMutationEvidenceBacked.ShouldBeTrue();

        GlobalAdministratorRemoveCommandSnapshot pageScopedResult = pending.ConfirmProjection(pageScoped);

        pageScopedResult.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        pageScopedResult.SafeMessage.ShouldBe("Tenants.GlobalAdministrators.Remove.Confirm.PageScoped");

        // A read that could not be verified at all: same lifecycle state, different explanation.
        GlobalAdministratorsSnapshot failedRead = GlobalAdministratorsSnapshot.Ready(
            CurrentRows("other-admin"),
            nextCursor: "protected-next",
            hasMore: true,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Unknown);
        failedRead.IsMutationEvidenceBacked.ShouldBeFalse();

        GlobalAdministratorRemoveCommandSnapshot failedResult = pending.ConfirmProjection(failedRead);

        failedResult.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        failedResult.SafeMessage.ShouldBe("Tenants.GlobalAdministrators.Remove.Confirm.EvidenceRequired");
        failedResult.SafeMessage.ShouldNotBe(pageScopedResult.SafeMessage);

        // Neither may be mistaken for the confirmed outcome.
        pageScopedResult.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable);
        failedResult.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable);
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
        // Complete evidence: a single row IS the platform total, so the last-administrator stop is truthful.
        GlobalAdministratorRemoveCommandSnapshot lastAdmin = GlobalAdministratorRemoveCommandSnapshot
            .Idle()
            .Preview(
                new RemoveGlobalAdministrator("target-admin"),
                CurrentRows("target-admin"),
                isCompleteEvidence: true);

        lastAdmin.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        lastAdmin.SafeMessage.ShouldNotBeNull().ShouldContain("last global administrator", Case.Insensitive);
        lastAdmin.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);

        // Incomplete evidence: the same single row is one page of an unknown total, so claiming the target is
        // the last global administrator would state a platform-wide fact derived from a page. Without this
        // case the completeness gate could be deleted and this test would still pass.
        GlobalAdministratorRemoveCommandSnapshot incompleteEvidence = GlobalAdministratorRemoveCommandSnapshot
            .Idle()
            .Preview(
                new RemoveGlobalAdministrator("target-admin"),
                CurrentRows("target-admin"),
                isCompleteEvidence: false);

        incompleteEvidence.State.ShouldBe(TenantCommandLifecycleState.Previewed);
        incompleteEvidence.SafeMessage.ShouldBeNull();
        incompleteEvidence.PreviewIsCompleteEvidence.ShouldBeFalse();

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
            freshness: ReadModelFreshnessState.Current) with { IsCompleteEvidence = true };
}
