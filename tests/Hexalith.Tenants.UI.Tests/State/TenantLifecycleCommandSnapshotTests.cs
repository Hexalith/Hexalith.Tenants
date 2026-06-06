using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantLifecycleCommandSnapshotTests
{
    [Fact]
    public void Disable_tenant_confirms_only_when_projection_shows_disabled()
    {
        var intent = new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant);
        TenantLifecycleCommandSnapshot snapshot = TenantLifecycleCommandSnapshot
            .Idle(Detail("tenant.alpha", TenantStatus.Active))
            .Previewed(intent, Detail("tenant.alpha", TenantStatus.Active))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantLifecycleCommandSnapshot withoutEvidence = snapshot.ConfirmProjection(Detail("tenant.alpha", TenantStatus.Active));

        withoutEvidence.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        withoutEvidence.LastConfirmedStatus.ShouldBe(TenantStatus.Active);

        TenantLifecycleCommandSnapshot confirmed = snapshot.ConfirmProjection(Detail("tenant.alpha", TenantStatus.Disabled));

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedStatus.ShouldBe(TenantStatus.Disabled);
        confirmed.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
    }

    [Fact]
    public void Enable_tenant_confirms_only_when_projection_shows_active()
    {
        var intent = new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.EnableTenant);
        TenantLifecycleCommandSnapshot snapshot = TenantLifecycleCommandSnapshot
            .Idle(Detail("tenant.alpha", TenantStatus.Disabled))
            .Previewed(intent, Detail("tenant.alpha", TenantStatus.Disabled))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantLifecycleCommandSnapshot withoutEvidence = snapshot.ConfirmProjection(Detail("tenant.alpha", TenantStatus.Disabled));

        withoutEvidence.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        withoutEvidence.LastConfirmedStatus.ShouldBe(TenantStatus.Disabled);

        TenantLifecycleCommandSnapshot confirmed = snapshot.ConfirmProjection(Detail("tenant.alpha", TenantStatus.Active));

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.LastConfirmedStatus.ShouldBe(TenantStatus.Active);
    }

    [Fact]
    public void Signalr_nudge_never_confirms_lifecycle_without_projection_evidence()
    {
        TenantLifecycleCommandSnapshot snapshot = TenantLifecycleCommandSnapshot
            .Idle(Detail("tenant.alpha", TenantStatus.Active))
            .Previewed(new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant), Detail("tenant.alpha", TenantStatus.Active))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        TenantLifecycleCommandSnapshot nudged = snapshot.SignalRNudge();

        nudged.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        nudged.LastConfirmedStatus.ShouldBe(TenantStatus.Active);
        nudged.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Theory]
    [InlineData(CommandStatus.Received, TenantCommandLifecycleState.Accepted, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite)]
    [InlineData(CommandStatus.Processing, TenantCommandLifecycleState.Accepted, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite)]
    [InlineData(CommandStatus.EventsStored, TenantCommandLifecycleState.ProjectionPending, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite)]
    [InlineData(CommandStatus.EventsPublished, TenantCommandLifecycleState.ProjectionPending, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite)]
    [InlineData(CommandStatus.Completed, TenantCommandLifecycleState.ProjectionPending, TenantCommandAuditState.AuditPending, TenantCommandLiveRegionPoliteness.Polite)]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected, TenantCommandAuditState.AuditUnavailable, TenantCommandLiveRegionPoliteness.Assertive)]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded, TenantCommandAuditState.AuditDelayed, TenantCommandLiveRegionPoliteness.Assertive)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify, TenantCommandAuditState.AuditDelayed, TenantCommandLiveRegionPoliteness.Assertive)]
    public void Lifecycle_status_results_remain_distinct_before_projection_confirmation(
        CommandStatus status,
        TenantCommandLifecycleState expectedState,
        TenantCommandAuditState expectedAudit,
        TenantCommandLiveRegionPoliteness expectedPoliteness)
    {
        TenantLifecycleCommandSnapshot snapshot = TenantLifecycleCommandSnapshot
            .Idle(Detail("tenant.alpha", TenantStatus.Active))
            .Previewed(new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant), Detail("tenant.alpha", TenantStatus.Active))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe lifecycle status message.", "TenantDisabled"));

        snapshot.State.ShouldBe(expectedState);
        snapshot.AuditState.ShouldBe(expectedAudit);
        snapshot.LiveRegionPoliteness.ShouldBe(expectedPoliteness);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        snapshot.LastConfirmedStatus.ShouldBe(TenantStatus.Active);
    }

    [Fact]
    public void Missing_command_status_is_unable_to_verify_with_unavailable_audit()
    {
        TenantLifecycleCommandSnapshot snapshot = TenantLifecycleCommandSnapshot
            .Idle(Detail("tenant.alpha", TenantStatus.Active))
            .Previewed(new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant), Detail("tenant.alpha", TenantStatus.Active))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(TenantCommandStatusResult.Unknown("Lifecycle command status is unavailable."));

        snapshot.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        snapshot.SafeMessage.ShouldBe("Lifecycle command status is unavailable.");
        snapshot.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable);
        snapshot.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
        snapshot.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
        snapshot.LastConfirmedStatus.ShouldBe(TenantStatus.Active);
    }

    [Fact]
    public void Blocked_and_duplicate_lifecycle_states_are_assertive_non_success_states()
    {
        TenantLifecycleCommandSnapshot blocked = TenantLifecycleCommandSnapshot.Blocked(
            "Lifecycle command support is unavailable.",
            TenantCommandFocusTarget.Submit);
        TenantLifecycleCommandSnapshot duplicate = TenantLifecycleCommandSnapshot
            .Idle(Detail("tenant.alpha", TenantStatus.Active))
            .Previewed(new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant), Detail("tenant.alpha", TenantStatus.Active))
            .DuplicatePrevented("A lifecycle command is already in progress.");

        blocked.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        blocked.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        blocked.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
        blocked.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);

        duplicate.State.ShouldBe(TenantCommandLifecycleState.DuplicatePrevented);
        duplicate.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        duplicate.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
        duplicate.LastConfirmedStatus.ShouldBe(TenantStatus.Active);
        duplicate.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Theory]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected, TenantCommandAuditState.AuditUnavailable)]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded, TenantCommandAuditState.AuditDelayed)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify, TenantCommandAuditState.AuditDelayed)]
    public void Projection_evidence_cannot_convert_terminal_lifecycle_non_success_states_to_confirmed(
        CommandStatus status,
        TenantCommandLifecycleState expectedState,
        TenantCommandAuditState expectedAudit)
    {
        TenantLifecycleCommandSnapshot snapshot = TenantLifecycleCommandSnapshot
            .Idle(Detail("tenant.alpha", TenantStatus.Active))
            .Previewed(new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant), Detail("tenant.alpha", TenantStatus.Active))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe non-success lifecycle message.", "TenantDisabled"));

        TenantLifecycleCommandSnapshot withProjectionEvidence = snapshot.ConfirmProjection(Detail("tenant.alpha", TenantStatus.Disabled));

        withProjectionEvidence.State.ShouldBe(expectedState);
        withProjectionEvidence.AuditState.ShouldBe(expectedAudit);
        withProjectionEvidence.LastConfirmedStatus.ShouldBe(TenantStatus.Disabled);
        withProjectionEvidence.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Rejected_lifecycle_state_remains_non_success_even_when_projection_matches()
    {
        TenantLifecycleCommandSnapshot snapshot = TenantLifecycleCommandSnapshot
            .Idle(Detail("tenant.alpha", TenantStatus.Active))
            .Previewed(new TenantLifecycleCommandRequest("tenant.alpha", TenantLifecycleOperation.DisableTenant), Detail("tenant.alpha", TenantStatus.Active))
            .RequestSent()
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Rejected,
                "The lifecycle already matches the requested state.",
                "TenantLifecycleStateAlreadySet"));

        TenantLifecycleCommandSnapshot afterProjection = snapshot.ConfirmProjection(Detail("tenant.alpha", TenantStatus.Disabled));

        afterProjection.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        afterProjection.LastConfirmedStatus.ShouldBe(TenantStatus.Disabled);
        afterProjection.SafeMessage.ShouldNotBeNull().ShouldContain("already matches");
    }

    private static TenantDetail Detail(string tenantId, TenantStatus status)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            status,
            [],
            new Dictionary<string, string>(),
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
}
