using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantSetConfigurationCommandSnapshotTests
{
    [Fact]
    public void Configuration_set_confirms_only_from_matching_tenant_proof()
    {
        TenantSetConfigurationCommandSnapshot snapshot = Pending(eventCount: 1);

        TenantSetConfigurationCommandSnapshot withoutEvidence = snapshot.ConfirmProjection(
            Proof("tenant.alpha", TenantConfigurationProjectionProofKind.SetNotConfirmed));
        TenantSetConfigurationCommandSnapshot wrongTenant = snapshot.ConfirmProjection(
            Proof("tenant.beta", TenantConfigurationProjectionProofKind.SetConfirmed));
        TenantSetConfigurationCommandSnapshot confirmed = snapshot.ConfirmProjection(
            Proof("tenant.alpha", TenantConfigurationProjectionProofKind.SetConfirmed));

        withoutEvidence.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        withoutEvidence.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
        withoutEvidence.LastConfigurationProof.ShouldNotBeNull().Kind.ShouldBe(TenantConfigurationProjectionProofKind.SetNotConfirmed);
        wrongTenant.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        wrongTenant.LastConfigurationProof.ShouldBeNull();
        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.FocusTarget.ShouldBe(TenantCommandFocusTarget.Lifecycle);
        confirmed.LastConfigurationProof.ShouldNotBeNull().Kind.ShouldBe(TenantConfigurationProjectionProofKind.SetConfirmed);
        confirmed.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
    }

    [Fact]
    public void Completed_without_events_is_already_applied_only_after_proof_confirms_value()
    {
        TenantSetConfigurationCommandSnapshot snapshot = Pending(eventCount: 0);

        TenantSetConfigurationCommandSnapshot withoutProof = snapshot.ConfirmProjection(
            Proof("tenant.alpha", TenantConfigurationProjectionProofKind.SetNotConfirmed));
        TenantSetConfigurationCommandSnapshot alreadyApplied = snapshot.ConfirmProjection(
            Proof("tenant.alpha", TenantConfigurationProjectionProofKind.SetConfirmed));

        withoutProof.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        alreadyApplied.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        alreadyApplied.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        alreadyApplied.SafeMessage.ShouldNotBeNull().ShouldContain("already applied", Case.Insensitive);
    }

    [Fact]
    public void Signalr_nudge_never_changes_configuration_truth_without_projection_proof()
    {
        TenantSetConfigurationCommandSnapshot snapshot = TenantSetConfigurationCommandSnapshot
            .Idle()
            .Previewed(Intent())
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        TenantSetConfigurationCommandSnapshot nudged = snapshot.SignalRNudge();

        nudged.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        nudged.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
        nudged.LastConfigurationProof.ShouldBeNull();
    }

    [Fact]
    public void Blocked_configuration_flow_sets_assertive_recovery_without_raw_state()
    {
        TenantSetConfigurationCommandSnapshot blocked = TenantSetConfigurationCommandSnapshot.Blocked(
            "Configuration scope evidence is unavailable.",
            TenantCommandFocusTarget.Namespace);

        blocked.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        blocked.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
        blocked.Intent.ShouldBeNull();
        blocked.LastConfigurationProof.ShouldBeNull();
        blocked.IsPreviewComplete.ShouldBeFalse();
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
    public void Configuration_status_results_remain_distinct_before_projection_confirmation(
        CommandStatus status,
        TenantCommandLifecycleState expectedState,
        TenantCommandAuditState expectedAudit,
        TenantCommandLiveRegionPoliteness expectedPoliteness)
    {
        TenantSetConfigurationCommandSnapshot snapshot = TenantSetConfigurationCommandSnapshot
            .Idle()
            .Previewed(Intent())
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe status.", "ConfigurationLimitExceeded"));

        snapshot.State.ShouldBe(expectedState);
        snapshot.AuditState.ShouldBe(expectedAudit);
        snapshot.LiveRegionPoliteness.ShouldBe(expectedPoliteness);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Theory]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected)]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify)]
    public void Projection_proof_cannot_convert_terminal_non_success_to_confirmed(
        CommandStatus status,
        TenantCommandLifecycleState expectedState)
    {
        TenantSetConfigurationCommandSnapshot snapshot = TenantSetConfigurationCommandSnapshot
            .Idle()
            .Previewed(Intent())
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe non-success.", "ConfigurationLimitExceeded"));

        TenantSetConfigurationCommandSnapshot withProof = snapshot.ConfirmProjection(
            Proof("tenant.alpha", TenantConfigurationProjectionProofKind.SetConfirmed));

        withProof.State.ShouldBe(expectedState);
        withProof.LastConfigurationProof.ShouldNotBeNull().Kind.ShouldBe(TenantConfigurationProjectionProofKind.SetConfirmed);
        withProof.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    private static TenantSetConfigurationCommandSnapshot Pending(int eventCount)
        => TenantSetConfigurationCommandSnapshot
            .Idle()
            .Previewed(Intent())
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: eventCount));

    private static SetTenantConfiguration Intent()
        => new("tenant.alpha", "billing.mode", "enterprise");

    private static TenantConfigurationProjectionProof Proof(
        string tenantId,
        TenantConfigurationProjectionProofKind kind)
        => TenantConfigurationProjectionProof.Create(tenantId, kind);
}
