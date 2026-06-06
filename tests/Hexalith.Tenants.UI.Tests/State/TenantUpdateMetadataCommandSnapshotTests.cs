using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantUpdateMetadataCommandSnapshotTests
{
    [Fact]
    public void Completed_status_requires_matching_metadata_projection_evidence_before_confirmation()
    {
        var intent = new UpdateTenantCommandRequest("Tenant.Mixed-01", "Updated", null);
        TenantUpdateMetadataCommandSnapshot snapshot = TenantUpdateMetadataCommandSnapshot
            .Idle("Original", "Original description", Detail("Tenant.Mixed-01", "Original", "Original description"))
            .RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        snapshot.LastConfirmedName.ShouldBe("Original");

        TenantUpdateMetadataCommandSnapshot withoutEvidence = snapshot.ConfirmProjection(Detail(
            "Tenant.Mixed-01",
            "Original",
            "Original description"));

        withoutEvidence.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        withoutEvidence.LastConfirmedName.ShouldBe("Original");
        withoutEvidence.LastConfirmedDetailProjection.ShouldNotBeNull().Name.ShouldBe("Original");

        TenantUpdateMetadataCommandSnapshot confirmed = snapshot.ConfirmProjection(Detail(
            "Tenant.Mixed-01",
            "Updated",
            null));

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedName.ShouldBe("Updated");
        confirmed.LastConfirmedDescription.ShouldBeNull();
        confirmed.LastConfirmedDetailProjection.ShouldNotBeNull().Name.ShouldBe("Updated");
    }

    [Fact]
    public void Accepted_status_stays_distinct_when_requery_has_no_matching_metadata()
    {
        var intent = new UpdateTenantCommandRequest("tenant.alpha", "Updated", "submitted");
        TenantUpdateMetadataCommandSnapshot accepted = TenantUpdateMetadataCommandSnapshot
            .Idle("Original", "Original description")
            .RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Received));

        TenantUpdateMetadataCommandSnapshot stillAccepted = accepted.ConfirmProjection(Detail(
            "tenant.alpha",
            "Original",
            "Original description"));

        stillAccepted.State.ShouldBe(TenantCommandLifecycleState.Accepted);
        stillAccepted.State.ShouldNotBe(TenantCommandLifecycleState.ProjectionPending);
        stillAccepted.LastConfirmedName.ShouldBe("Original");
    }

    [Fact]
    public void Signalr_nudge_cannot_confirm_metadata_or_audit_success()
    {
        var intent = new UpdateTenantCommandRequest("tenant.alpha", "Updated", "submitted");
        TenantUpdateMetadataCommandSnapshot snapshot = TenantUpdateMetadataCommandSnapshot
            .Idle("Original", "Original description")
            .RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .SignalRNudge();

        snapshot.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
        snapshot.LastConfirmedName.ShouldBe("Original");
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Completed_zero_event_status_still_waits_for_projection_because_update_tenant_is_not_noop_suppressed()
    {
        var intent = new UpdateTenantCommandRequest("tenant.alpha", "Alpha", "same description");
        TenantUpdateMetadataCommandSnapshot snapshot = TenantUpdateMetadataCommandSnapshot
            .Idle("Alpha", "same description")
            .RequestSent(intent)
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
        var intent = new UpdateTenantCommandRequest("tenant.alpha", "Updated", "submitted");
        TenantUpdateMetadataCommandSnapshot snapshot = TenantUpdateMetadataCommandSnapshot
            .Idle("Original", "Original description")
            .RequestSent(intent)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe non-success message."));

        TenantUpdateMetadataCommandSnapshot withProjectionEvidence = snapshot.ConfirmProjection(Detail(
            "tenant.alpha",
            "Updated",
            "submitted"));

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
