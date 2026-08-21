using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantUpdateMetadataCommandSnapshotTests
{
    [Fact]
    public void Completed_status_requires_matching_metadata_and_projection_version_advancement_before_confirmation()
    {
        var intent = new UpdateTenant("Tenant.Mixed-01", "Updated", null);
        TenantUpdateMetadataCommandSnapshot snapshot = TenantUpdateMetadataCommandSnapshot
            .Idle("Original", "Original description", Detail("Tenant.Mixed-01", "Original", "Original description"))
            .RequestSent(intent, baselineProjectionVersion: "v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        snapshot.LastConfirmedName.ShouldBe("Original");
        snapshot.BaselineProjectionVersion.ShouldBe("v1");

        TenantUpdateMetadataCommandSnapshot withoutEvidence = snapshot.ConfirmProjection(
            Detail("Tenant.Mixed-01", "Original", "Original description"),
            currentProjectionVersion: "v1");

        withoutEvidence.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        withoutEvidence.LastConfirmedName.ShouldBe("Original");
        withoutEvidence.LastConfirmedDetailProjection.ShouldNotBeNull().Name.ShouldBe("Original");

        TenantUpdateMetadataCommandSnapshot matchWithoutProvenance = snapshot.ConfirmProjection(
            Detail("Tenant.Mixed-01", "Updated", null),
            currentProjectionVersion: "v1");

        matchWithoutProvenance.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        matchWithoutProvenance.SafeMessageKey.ShouldBe("Tenants.EditMetadata.Confirm.UnableToVerify.MissingProvenance");
        matchWithoutProvenance.LastConfirmedName.ShouldBe("Original");
        matchWithoutProvenance.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
        matchWithoutProvenance.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
        matchWithoutProvenance.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);

        TenantUpdateMetadataCommandSnapshot confirmed = snapshot.ConfirmProjection(
            Detail("Tenant.Mixed-01", "Updated", null),
            currentProjectionVersion: "v2");

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedName.ShouldBe("Updated");
        confirmed.LastConfirmedDescription.ShouldBeNull();
        confirmed.LastConfirmedDetailProjection.ShouldNotBeNull().Name.ShouldBe("Updated");
    }

    [Fact]
    public void Projection_version_regression_or_opaque_churn_is_not_advancement()
    {
        // Causal provenance: a version that merely DIFFERS from the baseline is not proof this command
        // landed. A regression (v2 -> v1) and an unordered token swap must both fail closed.
        var intent = new UpdateTenant("tenant.alpha", "Updated", null);
        TenantUpdateMetadataCommandSnapshot snapshot = TenantUpdateMetadataCommandSnapshot
            .Idle("Original", "Original description", Detail("tenant.alpha", "Original", "Original description"))
            .RequestSent(intent, baselineProjectionVersion: "projection-v2")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantUpdateMetadataCommandSnapshot regressed = snapshot.ConfirmProjection(
            Detail("tenant.alpha", "Updated", null),
            currentProjectionVersion: "projection-v1");

        regressed.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        regressed.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        regressed.LastConfirmedName.ShouldBe("Original");

        TenantUpdateMetadataCommandSnapshot unorderedToken = snapshot.ConfirmProjection(
            Detail("tenant.alpha", "Updated", null),
            currentProjectionVersion: "an-unrelated-opaque-token");

        unorderedToken.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        unorderedToken.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        unorderedToken.LastConfirmedName.ShouldBe("Original");
    }

    [Fact]
    public void Advancing_version_without_command_event_evidence_does_not_confirm()
    {
        // Completed with no events proves nothing was produced by THIS command, so a concurrent unrelated
        // write that advances the tenant's projection version must not be borrowed as confirmation.
        var intent = new UpdateTenant("tenant.alpha", "Updated", null);
        TenantUpdateMetadataCommandSnapshot snapshot = TenantUpdateMetadataCommandSnapshot
            .Idle("Original", "Original description", Detail("tenant.alpha", "Original", "Original description"))
            .RequestSent(intent, baselineProjectionVersion: "projection-v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 0));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        snapshot.HasCommandEventEvidence.ShouldBeFalse();

        TenantUpdateMetadataCommandSnapshot borrowed = snapshot.ConfirmProjection(
            Detail("tenant.alpha", "Updated", null),
            currentProjectionVersion: "projection-v2");

        borrowed.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        borrowed.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        borrowed.LastConfirmedName.ShouldBe("Original");
    }

    [Fact]
    public void Signalr_elevated_projection_pending_cannot_confirm_without_event_evidence()
    {
        // SignalRNudge promotes Accepted to ProjectionPending with no status evidence. It must not become a
        // confirmation channel just because an unrelated write advanced the version.
        var intent = new UpdateTenant("tenant.alpha", "Updated", null);
        TenantUpdateMetadataCommandSnapshot nudged = TenantUpdateMetadataCommandSnapshot
            .Idle("Original", "Original description", Detail("tenant.alpha", "Original", "Original description"))
            .RequestSent(intent, baselineProjectionVersion: "projection-v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .SignalRNudge();

        nudged.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        nudged.HasCommandEventEvidence.ShouldBeFalse();

        TenantUpdateMetadataCommandSnapshot confirmed = nudged.ConfirmProjection(
            Detail("tenant.alpha", "Updated", null),
            currentProjectionVersion: "projection-v2");

        confirmed.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedName.ShouldBe("Original");
    }

    [Fact]
    public void Identical_submitted_metadata_confirms_with_provenance_and_never_becomes_already_applied()
    {
        var intent = new UpdateTenant("tenant.alpha", "Alpha", "same description");
        TenantUpdateMetadataCommandSnapshot snapshot = TenantUpdateMetadataCommandSnapshot
            .Idle("Alpha", "same description")
            .RequestSent(intent, baselineProjectionVersion: "v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantUpdateMetadataCommandSnapshot confirmed = snapshot.ConfirmProjection(
            Detail("tenant.alpha", "Alpha", "same description"),
            currentProjectionVersion: "v2");

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.State.ShouldNotBe(TenantCommandLifecycleState.AlreadyApplied);
        confirmed.SafeMessageKey.ShouldBeNull();
    }

    [Fact]
    public void Missing_baseline_fails_closed_to_unable_to_verify_when_metadata_matches()
    {
        var intent = new UpdateTenant("tenant.alpha", "Updated", "submitted");
        TenantUpdateMetadataCommandSnapshot snapshot = TenantUpdateMetadataCommandSnapshot
            .Idle("Original", "Original description")
            .RequestSent(intent, baselineProjectionVersion: null)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantUpdateMetadataCommandSnapshot result = snapshot.ConfirmProjection(
            Detail("tenant.alpha", "Updated", "submitted"),
            currentProjectionVersion: "v2");

        result.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        result.SafeMessageKey.ShouldBe("Tenants.EditMetadata.Confirm.UnableToVerify.MissingBaseline");
        result.LastConfirmedName.ShouldBe("Original");
        result.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Accepted_status_never_confirms_even_when_metadata_and_version_match()
    {
        var intent = new UpdateTenant("tenant.alpha", "Updated", "submitted");
        TenantUpdateMetadataCommandSnapshot accepted = TenantUpdateMetadataCommandSnapshot
            .Idle("Original", "Original description")
            .RequestSent(intent, baselineProjectionVersion: "v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Received));

        TenantUpdateMetadataCommandSnapshot stillAccepted = accepted.ConfirmProjection(
            Detail("tenant.alpha", "Updated", "submitted"),
            currentProjectionVersion: "v2");

        stillAccepted.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        stillAccepted.State.ShouldNotBe(TenantCommandLifecycleState.ProjectionPending);
        stillAccepted.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        stillAccepted.LastConfirmedName.ShouldBe("Original");
    }

    [Fact]
    public void Qualifying_audit_provenance_can_confirm_without_version_advancement()
    {
        var intent = new UpdateTenant("tenant.alpha", "Updated", null);
        TenantUpdateMetadataCommandSnapshot snapshot = TenantUpdateMetadataCommandSnapshot
            .Idle("Original", "Original description")
            .RequestSent(intent, baselineProjectionVersion: "v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantUpdateMetadataCommandSnapshot confirmed = snapshot.ConfirmProjection(
            Detail("tenant.alpha", "Updated", null),
            currentProjectionVersion: "v1",
            hasQualifyingAuditProvenance: true);

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Signalr_nudge_cannot_confirm_metadata_or_audit_success()
    {
        var intent = new UpdateTenant("tenant.alpha", "Updated", "submitted");
        TenantUpdateMetadataCommandSnapshot snapshot = TenantUpdateMetadataCommandSnapshot
            .Idle("Original", "Original description")
            .RequestSent(intent, baselineProjectionVersion: "v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .SignalRNudge();

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
        snapshot.LastConfirmedName.ShouldBe("Original");
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);

        TenantUpdateMetadataCommandSnapshot nudgedConfirm = snapshot.ConfirmProjection(
            Detail("tenant.alpha", "Updated", "submitted"),
            currentProjectionVersion: "v1");
        nudgedConfirm.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        nudgedConfirm.SafeMessageKey.ShouldBe("Tenants.EditMetadata.Confirm.UnableToVerify.MissingProvenance");
    }

    [Fact]
    public void Apply_status_progress_arms_clear_prior_safe_message_key()
    {
        var intent = new UpdateTenant("tenant.alpha", "Updated", null);
        TenantUpdateMetadataCommandSnapshot withStaleKey = TenantUpdateMetadataCommandSnapshot
            .Idle("Original", "Original description")
            .RequestSent(intent, baselineProjectionVersion: "v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            with {
                SafeMessage = "stale gateway text",
                SafeMessageKey = "Tenants.EditMetadata.Confirm.UnableToVerify.MissingProvenance",
            };

        TenantUpdateMetadataCommandSnapshot accepted = withStaleKey.ApplyStatus(
            new TenantCommandStatusResult(CommandStatus.Received));
        accepted.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        accepted.SafeMessage.ShouldBeNull();
        accepted.SafeMessageKey.ShouldBeNull();

        TenantUpdateMetadataCommandSnapshot pending = withStaleKey.ApplyStatus(
            new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));
        pending.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        pending.SafeMessage.ShouldBeNull();
        pending.SafeMessageKey.ShouldBeNull();
    }

    [Fact]
    public void Completed_zero_event_status_still_waits_for_projection_because_update_tenant_is_not_noop_suppressed()
    {
        var intent = new UpdateTenant("tenant.alpha", "Alpha", "same description");
        TenantUpdateMetadataCommandSnapshot snapshot = TenantUpdateMetadataCommandSnapshot
            .Idle("Alpha", "same description")
            .RequestSent(intent, baselineProjectionVersion: "v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 0));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.AlreadyApplied);
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
        var intent = new UpdateTenant("tenant.alpha", "Updated", "submitted");
        TenantUpdateMetadataCommandSnapshot snapshot = TenantUpdateMetadataCommandSnapshot
            .Idle("Original", "Original description")
            .RequestSent(intent, baselineProjectionVersion: "v1")
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe non-success message."));

        TenantUpdateMetadataCommandSnapshot withProjectionEvidence = snapshot.ConfirmProjection(
            Detail("tenant.alpha", "Updated", "submitted"),
            currentProjectionVersion: "v2");

        withProjectionEvidence.State.ShouldBe(expectedState);
        withProjectionEvidence.AuditState.ShouldBe(expectedAudit);
        withProjectionEvidence.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        withProjectionEvidence.LastConfirmedName.ShouldBe("Original");
    }

    private static TenantDetail Detail(string tenantId, string name, string? description)
        => new(
            tenantId,
            name,
            description,
            TenantStatus.Active,
            [new TenantMember("owner-user", TenantRole.TenantOwner)],
            new Dictionary<string, string>(),
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
}
