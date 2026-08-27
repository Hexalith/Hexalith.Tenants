using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantRemoveConfigurationCommandSnapshotTests
{
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

    [Fact]
    public void Matching_absence_requires_command_event_evidence_and_ordered_advancement()
    {
        TenantRemoveConfigurationIntent intent = Intent();
        TenantRemoveConfigurationCommandSnapshot pending = Pending(intent);

        pending.ConfirmProjection(Proof(intent, "tenant-sequence:40")).State
            .ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        pending.ConfirmProjection(Proof(intent, "tenant-sequence:41")).State
            .ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        pending.ConfirmProjection(Proof(intent, "tenant-sequence:42")).State
            .ShouldBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Proof_for_another_exact_intent_cannot_confirm()
    {
        TenantRemoveConfigurationIntent intent = Intent();
        TenantRemoveConfigurationIntent other = new(intent.TenantId, intent.NamespacePrefix, "billing.other");

        TenantRemoveConfigurationCommandSnapshot observed = Pending(intent)
            .ConfirmProjection(Proof(other, "tenant-sequence:42"));

        observed.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        // A proof for another exact intent must also not be retained as this attempt's evidence: the
        // attempt tracker's Merge orders a retained against an incoming snapshot by
        // LastConfigurationProof.ProjectionVersion, so keeping a foreign version skews that choice.
        observed.LastConfigurationProof.ShouldBeNull();
    }

    [Fact]
    public void Preexisting_absence_without_command_event_evidence_cannot_confirm()
    {
        TenantRemoveConfigurationIntent intent = Intent();
        TenantRemoveConfigurationCommandSnapshot accepted = RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted(MessageId, "correlation-1"));

        accepted.ConfirmProjection(Proof(intent, "tenant-sequence:42")).State
            .ShouldBe(TenantCommandLifecycleState.ProjectionPending);
    }

    [Fact]
    public void Configuration_key_not_found_rejection_stays_rejected_after_absence_proof()
    {
        TenantRemoveConfigurationIntent intent = Intent();
        TenantRemoveConfigurationCommandSnapshot rejected = RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted(MessageId, "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Rejected,
                RejectionCode: "ConfigurationKeyNotFound",
                HasVerifiedCommandIdentity: true));

        TenantRemoveConfigurationCommandSnapshot result = rejected.ConfirmProjection(Proof(intent, "tenant-sequence:42"));

        result.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        result.RejectionCode.ShouldBe("ConfigurationKeyNotFound");
    }

    [Fact]
    public void Signalr_is_only_a_refresh_nudge()
    {
        TenantRemoveConfigurationCommandSnapshot accepted = RequestSent(Intent())
            .Accepted(TenantCommandSubmissionResult.Accepted(MessageId, "correlation-1"));

        TenantRemoveConfigurationCommandSnapshot nudged = accepted.SignalRNudge();

        nudged.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        nudged.LastConfigurationProof.ShouldBeNull();
        nudged.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
    }

    [Theory]
    [InlineData(CommandStatus.Received, TenantCommandLifecycleState.Accepted)]
    [InlineData(CommandStatus.Processing, TenantCommandLifecycleState.Accepted)]
    [InlineData(CommandStatus.EventsStored, TenantCommandLifecycleState.ProjectionPending)]
    [InlineData(CommandStatus.EventsPublished, TenantCommandLifecycleState.ProjectionPending)]
    [InlineData(CommandStatus.Completed, TenantCommandLifecycleState.ProjectionPending)]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected)]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify)]
    public void Status_results_remain_distinct_before_projection_confirmation(
        CommandStatus status,
        TenantCommandLifecycleState expectedState)
    {
        TenantRemoveConfigurationCommandSnapshot snapshot = RequestSent(Intent())
            .Accepted(TenantCommandSubmissionResult.Accepted(MessageId, "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                status,
                EventCount: status is CommandStatus.Completed or CommandStatus.PublishFailed ? 1 : null,
                HasVerifiedCommandIdentity: true));

        snapshot.State.ShouldBe(expectedState);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Completed_without_event_evidence_fails_closed()
    {
        TenantRemoveConfigurationCommandSnapshot result = RequestSent(Intent())
            .Accepted(TenantCommandSubmissionResult.Accepted(MessageId, "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Completed,
                EventCount: 0,
                HasVerifiedCommandIdentity: true));

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.SafeMessageKey.ShouldBe("Tenants.Configuration.Remove.UnableToVerify.MissingEventEvidence");
    }

    [Fact]
    public void Status_without_verified_aggregate_identity_fails_closed()
    {
        TenantRemoveConfigurationCommandSnapshot result = RequestSent(Intent())
            .Accepted(TenantCommandSubmissionResult.Accepted(MessageId, "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.EventsPublished,
                HasVerifiedCommandIdentity: false));

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.SafeMessageKey.ShouldBe("Tenants.Configuration.Remove.UnableToVerify.TrackingMismatch");
    }

    [Fact]
    public void Explicit_abandon_releases_retained_identity()
    {
        TenantRemoveConfigurationCommandSnapshot result = RequestSent(Intent()).Abandon();

        result.RetainsAttempt.ShouldBeFalse();
        result.MessageId.ShouldBeNull();
        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
    }

    [Theory]
    [InlineData(CommandStatus.Rejected)]
    [InlineData(CommandStatus.TimedOut)]
    [InlineData(CommandStatus.Received)]
    [InlineData(CommandStatus.Processing)]
    public void Delayed_status_cannot_erase_stored_or_published_event_truth(CommandStatus delayedStatus)
    {
        TenantRemoveConfigurationCommandSnapshot projected = RequestSent(Intent())
            .Accepted(TenantCommandSubmissionResult.Accepted(MessageId, "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.EventsPublished,
                EventCount: 1,
                HasVerifiedCommandIdentity: true));

        TenantRemoveConfigurationCommandSnapshot result = projected.ApplyStatus(new TenantCommandStatusResult(
            delayedStatus,
            HasVerifiedCommandIdentity: true));

        result.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        result.HasCommandEventEvidence.ShouldBeTrue();
    }

    [Fact]
    public void Publish_failure_is_preserved_from_delayed_terminal_status_and_can_recover_from_stronger_event_evidence()
    {
        TenantRemoveConfigurationCommandSnapshot degraded = RequestSent(Intent())
            .Accepted(TenantCommandSubmissionResult.Accepted(MessageId, "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.PublishFailed,
                EventCount: 1,
                HasVerifiedCommandIdentity: true));

        TenantRemoveConfigurationCommandSnapshot delayed = degraded.ApplyStatus(new TenantCommandStatusResult(
            CommandStatus.Rejected,
            HasVerifiedCommandIdentity: true));
        TenantRemoveConfigurationCommandSnapshot recovered = delayed.ApplyStatus(new TenantCommandStatusResult(
            CommandStatus.EventsPublished,
            EventCount: 1,
            HasVerifiedCommandIdentity: true));

        delayed.State.ShouldBe(TenantCommandLifecycleState.Degraded);
        delayed.HasCommandEventEvidence.ShouldBeTrue();
        recovered.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        recovered.HasCommandEventEvidence.ShouldBeTrue();
    }

    [Fact]
    public void Projection_verification_failure_preserves_event_and_projection_pending_truth()
    {
        TenantRemoveConfigurationCommandSnapshot projected = Pending(Intent());

        TenantRemoveConfigurationCommandSnapshot result = projected.ProjectionVerificationFailed(
            "Tenants.Configuration.Remove.UnableToVerify.ProjectionProof");

        result.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        result.HasCommandEventEvidence.ShouldBeTrue();
        result.SafeMessageKey.ShouldBe("Tenants.Configuration.Remove.UnableToVerify.ProjectionProof");
        result.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
    }

    private static TenantRemoveConfigurationCommandSnapshot Pending(TenantRemoveConfigurationIntent intent)
        => RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted(MessageId, "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Completed,
                EventCount: 1,
                HasVerifiedCommandIdentity: true));

    private static TenantRemoveConfigurationCommandSnapshot RequestSent(TenantRemoveConfigurationIntent intent)
    {
        TenantRemoveConfigurationPreview preview = Preview(intent);
        return TenantRemoveConfigurationCommandSnapshot.Idle()
            .Previewed(preview)
            .RequestSent(preview, MessageId, DateTimeOffset.UtcNow);
    }

    private static TenantRemoveConfigurationIntent Intent()
        => new("tenant.alpha", "billing", "billing.mode");

    private static TenantRemoveConfigurationPreview Preview(TenantRemoveConfigurationIntent intent)
        => TenantRemoveConfigurationPreview.Create(
            intent,
            TenantStatus.Active,
            TenantRemoveConfigurationCurrentState.Present,
            ReadModelFreshnessState.Current,
            ProjectionLifecycleState.Current,
            "tenant-sequence:41",
            isAuthorized: true);

    private static TenantConfigurationProjectionProof Proof(
        TenantRemoveConfigurationIntent intent,
        string projectionVersion)
        => TenantConfigurationProjectionProof.Create(
            intent.TenantId,
            TenantConfigurationProjectionProofKind.RemoveConfirmed,
            projectionVersion,
            intent.AttemptFingerprint);
}
