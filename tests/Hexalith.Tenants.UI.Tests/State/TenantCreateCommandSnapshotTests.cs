using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantCreateCommandSnapshotTests
{
    [Fact]
    public void Completed_status_requires_projection_evidence_before_confirmation()
    {
        var intent = new CreateTenant("Tenant.Mixed-01", "Mixed Tenant", null);
        TenantCreateCommandSnapshot snapshot = TenantCreateCommandSnapshot
            .Idle()
            .RequestSent(intent, "projection-v1", baselineTenantAbsent: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        TenantCreateCommandSnapshot withoutEvidence = snapshot.ConfirmProjection(null, null);
        withoutEvidence.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        TenantCreateCommandSnapshot confirmed = snapshot.ConfirmProjection(
            new TenantSummary("Tenant.Mixed-01", "Mixed Tenant", TenantStatus.Active),
            null,
            "projection-v2");
        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedListEvidence.ShouldNotBeNull().TenantId.ShouldBe("Tenant.Mixed-01");
        confirmed.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
    }

    [Fact]
    public void Accepted_status_stays_distinct_from_projection_pending_when_requery_has_no_evidence()
    {
        var intent = new CreateTenant("tenant.alpha", "Alpha", null);
        TenantCreateCommandSnapshot accepted = TenantCreateCommandSnapshot
            .Idle()
            .RequestSent(intent, "projection-v1", baselineTenantAbsent: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Received));

        accepted.State.ShouldBe(TenantCommandLifecycleState.Accepted);

        // A re-query that cannot yet prove the tenant must not collapse the still-processing
        // Accepted state into ProjectionPending; AC4 requires the two states to remain distinct.
        TenantCreateCommandSnapshot stillAccepted = accepted.ConfirmProjection(null, null);

        stillAccepted.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        stillAccepted.State.ShouldNotBe(TenantCommandLifecycleState.ProjectionPending);
    }

    [Fact]
    public void Signalr_nudge_cannot_confirm_command_or_audit_success()
    {
        var intent = new CreateTenant("tenant.alpha", "Alpha", null);
        TenantCreateCommandSnapshot snapshot = TenantCreateCommandSnapshot
            .Idle()
            .RequestSent(intent, "projection-v1", baselineTenantAbsent: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .SignalRNudge();

        // A nudge is a freshness hint only: it must not manufacture ProjectionPending, which is the one
        // state ConfirmProjection trusts, or notification alone would stand in for command-status evidence.
        snapshot.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.ProjectionPending);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        snapshot.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
    }

    [Fact]
    public void Rejection_and_unable_to_verify_are_assertive_non_success_states()
    {
        TenantCreateCommandSnapshot rejected = TenantCreateCommandSnapshot
            .Idle()
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Rejected, "Tenant already exists.", "TenantAlreadyExists"));
        TenantCreateCommandSnapshot unable = TenantCreateCommandSnapshot
            .Idle()
            .ApplyStatus(TenantCommandStatusResult.Unknown("Status lookup failed."));

        rejected.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        rejected.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
        unable.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        unable.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
    }

    [Theory]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected)]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify)]
    public void Projection_evidence_cannot_convert_terminal_non_success_states_to_confirmed(
        CommandStatus status,
        TenantCommandLifecycleState expectedState)
    {
        var intent = new CreateTenant("tenant.alpha", "Alpha", null);
        TenantCreateCommandSnapshot snapshot = TenantCreateCommandSnapshot
            .Idle()
            .RequestSent(intent, "projection-v1", baselineTenantAbsent: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe non-success message."));

        TenantCreateCommandSnapshot withProjectionEvidence = snapshot.ConfirmProjection(
            new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active),
            null);

        withProjectionEvidence.State.ShouldBe(expectedState);
        withProjectionEvidence.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        withProjectionEvidence.LastConfirmedListEvidence.ShouldBeNull();
    }

    [Fact]
    public void Matching_metadata_without_projection_advancement_is_unable_to_verify()
    {
        var intent = new CreateTenant("tenant.alpha", "Alpha", null);
        TenantCreateCommandSnapshot snapshot = TenantCreateCommandSnapshot
            .Idle()
            .RequestSent(intent, "projection-v1", baselineTenantAbsent: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantCreateCommandSnapshot reconciled = snapshot.ConfirmProjection(
            new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active),
            null,
            "projection-v1");

        reconciled.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        reconciled.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Pre_existing_baseline_can_never_be_confirmed_as_create()
    {
        var intent = new CreateTenant("tenant.alpha", "Alpha", null);
        TenantCreateCommandSnapshot snapshot = TenantCreateCommandSnapshot
            .Idle()
            .RequestSent(intent, "projection-v1", baselineTenantAbsent: false)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantCreateCommandSnapshot reconciled = snapshot.ConfirmProjection(
            new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active),
            null,
            "projection-v2");

        reconciled.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        reconciled.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Non_null_description_requires_matching_detail_evidence_before_confirmation()
    {
        var intent = new CreateTenant("tenant.alpha", "Alpha", "Description");
        TenantCreateCommandSnapshot snapshot = TenantCreateCommandSnapshot
            .Idle()
            .RequestSent(intent, "projection-v1", baselineTenantAbsent: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantCreateCommandSnapshot listOnly = snapshot.ConfirmProjection(
            new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active),
            null,
            "projection-v2");
        listOnly.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

        TenantDetailSnapshot detail = TenantDetailSnapshot.Ready(
            new TenantDetail(
                "tenant.alpha",
                "Alpha",
                "Description",
                TenantStatus.Active,
                [],
                new Dictionary<string, string>(),
                DateTimeOffset.UtcNow),
            "\"etag\"",
            Hexalith.EventStore.Client.Projections.ReadModelFreshnessState.Current,
            projectionVersion: "projection-v2");
        TenantCreateCommandSnapshot confirmed = snapshot.ConfirmProjection(null, detail);

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Accepted_with_matching_evidence_and_advanced_version_is_never_confirmed()
    {
        // Pins the gate that acceptance alone can never satisfy. Widening ConfirmProjection back to
        // "Accepted or ProjectionPending" makes this test fail; without it that widening is invisible,
        // because every other evidence-bearing test first drives status to Completed.
        var intent = new CreateTenant("tenant.alpha", "Alpha", null);
        TenantCreateCommandSnapshot accepted = TenantCreateCommandSnapshot
            .Idle()
            .RequestSent(intent, "projection-v1", baselineTenantAbsent: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Received));

        accepted.State.ShouldBe(TenantCommandLifecycleState.Accepted);

        TenantCreateCommandSnapshot reconciled = accepted.ConfirmProjection(
            new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active),
            null,
            "projection-v2");

        reconciled.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        reconciled.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Completed_without_command_event_evidence_cannot_confirm()
    {
        var intent = new CreateTenant("tenant.alpha", "Alpha", null);
        TenantCreateCommandSnapshot snapshot = TenantCreateCommandSnapshot
            .Idle()
            .RequestSent(intent, "projection-v1", baselineTenantAbsent: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 0));

        TenantCreateCommandSnapshot reconciled = snapshot.ConfirmProjection(
            new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active),
            null,
            "projection-v2");

        reconciled.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        reconciled.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Detail_evidence_that_is_not_current_cannot_prove_creation()
    {
        var intent = new CreateTenant("tenant.alpha", "Alpha", "Description");
        TenantCreateCommandSnapshot snapshot = TenantCreateCommandSnapshot
            .Idle()
            .RequestSent(intent, "projection-v1", baselineTenantAbsent: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantDetailSnapshot staleDetail = TenantDetailSnapshot.Ready(
            new TenantDetail(
                "tenant.alpha",
                "Alpha",
                "Description",
                TenantStatus.Active,
                [],
                new Dictionary<string, string>(),
                DateTimeOffset.UtcNow),
            "\"etag\"",
            Hexalith.EventStore.Client.Projections.ReadModelFreshnessState.Stale,
            projectionVersion: "projection-v2");

        TenantCreateCommandSnapshot reconciled = snapshot.ConfirmProjection(null, staleDetail);

        reconciled.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        reconciled.LastConfirmedDetailEvidence.ShouldBeNull();
    }

    [Fact]
    public void Unable_to_verify_never_records_unproven_evidence_as_last_confirmed()
    {
        var intent = new CreateTenant("tenant.alpha", "Alpha", null);
        TenantCreateCommandSnapshot snapshot = TenantCreateCommandSnapshot
            .Idle()
            .RequestSent(intent, "projection-v1", baselineTenantAbsent: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantCreateCommandSnapshot reconciled = snapshot.ConfirmProjection(
            new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active),
            null,
            "projection-v1");

        reconciled.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        reconciled.LastConfirmedListEvidence.ShouldBeNull();
        reconciled.LastConfirmedDetailEvidence.ShouldBeNull();
    }

    [Fact]
    public void Cleared_description_still_reconciles_against_a_projection_without_one()
    {
        // A textarea typed into and then cleared submits "", while the projection stores null. Without
        // normalisation the attempt matched nothing and stayed ProjectionPending forever.
        var intent = new CreateTenant("tenant.alpha", "Alpha", string.Empty);
        TenantCreateCommandSnapshot snapshot = TenantCreateCommandSnapshot
            .Idle()
            .RequestSent(intent, "projection-v1", baselineTenantAbsent: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantCreateCommandSnapshot reconciled = snapshot.ConfirmProjection(
            new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active),
            null,
            "projection-v2");

        reconciled.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Blocked_with_tracking_keeps_the_handle_that_recovery_depends_on()
    {
        var intent = new CreateTenant("tenant.alpha", "Alpha", null);
        TenantCreateCommandSnapshot tracked = TenantCreateCommandSnapshot
            .Idle()
            .RequestSent(intent, "projection-v1", baselineTenantAbsent: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        TenantCreateCommandSnapshot blocked = tracked.BlockedWithTracking("Still tracking the previous attempt.");

        blocked.MessageId.ShouldBe("message-1");
        blocked.CorrelationId.ShouldBe("correlation-1");
        blocked.Intent.ShouldBe(intent);
        blocked.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        blocked.SafeMessage.ShouldBe("Still tracking the previous attempt.");
    }
}
