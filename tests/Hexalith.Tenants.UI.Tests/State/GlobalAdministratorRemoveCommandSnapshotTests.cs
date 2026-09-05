using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class GlobalAdministratorRemoveCommandSnapshotTests
{
    private const string MessageId = "01J00000000000000000000000";

    [Fact]
    public void Confirmation_requires_exact_event_evidence_absence_and_version_advancement()
    {
        GlobalAdministratorRemoveCommandSnapshot pending = Preview("ga-v1")
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted(MessageId, "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Completed,
                EventCount: 1,
                HasVerifiedCommandIdentity: true));

        GlobalAdministratorRemoveCommandSnapshot sameVersion = pending.ConfirmProjection(
            Ready("ga-v1", "other-admin"));
        sameVersion.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        sameVersion.SafeMessageKey.ShouldBe("Tenants.GlobalAdministrators.Remove.Confirm.VersionNotAdvanced");
        GlobalAdministratorRemoveCommandSnapshot stillPresent = pending.ConfirmProjection(
            Ready("ga-v2", "target-admin", "other-admin"));
        stillPresent.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        stillPresent.SafeMessageKey.ShouldBe("Tenants.GlobalAdministrators.Remove.Confirm.StillPresent");
        pending.ConfirmProjection(Ready("ga-v2", "other-admin")).State
            .ShouldBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Received_and_processing_never_create_event_evidence()
    {
        GlobalAdministratorRemoveCommandSnapshot accepted = Preview("ga-v1")
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted(MessageId, "correlation-1"));

        GlobalAdministratorRemoveCommandSnapshot received = accepted.ApplyStatus(
            new TenantCommandStatusResult(CommandStatus.Received, HasVerifiedCommandIdentity: true));
        GlobalAdministratorRemoveCommandSnapshot processing = received.ApplyStatus(
            new TenantCommandStatusResult(CommandStatus.Processing, HasVerifiedCommandIdentity: true));

        processing.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        processing.HasCommandEventEvidence.ShouldBeFalse();
    }

    [Fact]
    public void Completed_zero_and_unsupported_submission_fail_closed()
    {
        GlobalAdministratorRemoveCommandSnapshot sent = Preview("ga-v1").RequestSent();

        sent.ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Completed,
                EventCount: 0,
                HasVerifiedCommandIdentity: true))
            .State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        GlobalAdministratorRemoveCommandSnapshot unsupported = sent.ApplySubmission(
            new TenantCommandSubmissionResult(
                TenantCommandLifecycleState.AlreadyApplied,
                MessageId,
                "correlation-1"));
        unsupported.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        unsupported.MessageId.ShouldBe(MessageId);
        unsupported.CorrelationId.ShouldBeNull();
        unsupported.PreviewEvidence.ShouldBeSameAs(sent.PreviewEvidence);
        unsupported.IsSubmissionAmbiguous.ShouldBeTrue();
        unsupported.SafeRecoveryKey.ShouldBe(
            "Tenants.GlobalAdministrators.Remove.DeliveryRetry.Recovery");
    }

    [Fact]
    public void Ambiguous_delivery_retains_preview_and_exact_message_id()
    {
        GlobalAdministratorRemoveCommandSnapshot sent = Preview("ga-v1").RequestSent();
        GlobalAdministratorRemoveCommandSnapshot ambiguous = sent.ApplySubmission(
            TenantCommandSubmissionResult.Ambiguous(
                MessageId,
                "Tenants.GlobalAdministrators.Remove.SubmissionEvidence.Ambiguous"));

        ambiguous.State.ShouldBe(TenantCommandLifecycleState.RequestSent);
        ambiguous.MessageId.ShouldBe(MessageId);
        ambiguous.PreviewEvidence.ShouldNotBeNull().IsComplete.ShouldBeTrue();
        ambiguous.IsSubmissionAmbiguous.ShouldBeTrue();
    }

    [Fact]
    public void Signalr_is_only_a_nudge()
    {
        GlobalAdministratorRemoveCommandSnapshot accepted = Preview("ga-v1")
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted(MessageId, "correlation-1"));

        GlobalAdministratorRemoveCommandSnapshot nudged = accepted.SignalRNudge();

        nudged.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        nudged.HasCommandEventEvidence.ShouldBeFalse();
    }

    private static GlobalAdministratorRemoveCommandSnapshot Preview(string version)
        => GlobalAdministratorRemoveCommandSnapshot.Idle().Preview(
            GlobalAdministratorRemovePreview.Create(
                "target-admin",
                "operator-admin",
                Ready(version, "target-admin", "other-admin"),
                isAuthorized: true),
            MessageId);

    private static GlobalAdministratorsSnapshot Ready(string version, params string[] userIds)
        => GlobalAdministratorsSnapshot.Ready(
            userIds.Select(userId => new GlobalAdministratorRow(
                userId,
                ReadModelFreshnessState.Current)
            {
                Lifecycle = ProjectionLifecycleState.Current,
            }).ToArray(),
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            ProjectionVersion = version,
            IsCompleteEvidence = true,
        };
}
