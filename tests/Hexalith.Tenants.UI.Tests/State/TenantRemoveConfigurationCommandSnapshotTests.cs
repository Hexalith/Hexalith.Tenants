using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantRemoveConfigurationCommandSnapshotTests
{
    [Fact]
    public void Configuration_remove_confirms_only_from_matching_tenant_proof()
    {
        TenantRemoveConfigurationCommandSnapshot snapshot = Pending();

        TenantRemoveConfigurationCommandSnapshot stillVisible = snapshot.ConfirmProjection(
            Proof("tenant.alpha", TenantConfigurationProjectionProofKind.RemoveNotConfirmed));
        TenantRemoveConfigurationCommandSnapshot wrongTenant = snapshot.ConfirmProjection(
            Proof("tenant.beta", TenantConfigurationProjectionProofKind.RemoveConfirmed));
        TenantRemoveConfigurationCommandSnapshot confirmed = snapshot.ConfirmProjection(
            Proof("tenant.alpha", TenantConfigurationProjectionProofKind.RemoveConfirmed));

        stillVisible.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        stillVisible.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
        stillVisible.LastConfigurationProof.ShouldNotBeNull().Kind.ShouldBe(TenantConfigurationProjectionProofKind.RemoveNotConfirmed);
        wrongTenant.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        wrongTenant.LastConfigurationProof.ShouldBeNull();
        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.FocusTarget.ShouldBe(TenantCommandFocusTarget.Lifecycle);
        confirmed.LastConfigurationProof.ShouldNotBeNull().Kind.ShouldBe(TenantConfigurationProjectionProofKind.RemoveConfirmed);
    }

    [Fact]
    public void Rejected_removal_stays_rejected_even_when_proof_later_reports_absence()
    {
        TenantRemoveConfigurationCommandSnapshot snapshot = TenantRemoveConfigurationCommandSnapshot
            .Idle()
            .Previewed(Intent())
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Rejected,
                "The configuration key was not found.",
                "ConfigurationKeyNotFound"));

        TenantRemoveConfigurationCommandSnapshot withProof = snapshot.ConfirmProjection(
            Proof("tenant.alpha", TenantConfigurationProjectionProofKind.RemoveConfirmed));

        withProof.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        withProof.RejectionCode.ShouldBe("ConfigurationKeyNotFound");
        withProof.LastConfigurationProof.ShouldNotBeNull().Kind.ShouldBe(TenantConfigurationProjectionProofKind.RemoveConfirmed);
    }

    [Fact]
    public void Signalr_nudge_never_removes_configuration_without_projection_proof()
    {
        TenantRemoveConfigurationCommandSnapshot snapshot = TenantRemoveConfigurationCommandSnapshot
            .Idle()
            .Previewed(Intent())
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        TenantRemoveConfigurationCommandSnapshot nudged = snapshot.SignalRNudge();

        nudged.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        nudged.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
        nudged.LastConfigurationProof.ShouldBeNull();
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
    public void Configuration_remove_status_results_remain_distinct_before_projection_confirmation(
        CommandStatus status,
        TenantCommandLifecycleState expectedState,
        TenantCommandAuditState expectedAudit,
        TenantCommandLiveRegionPoliteness expectedPoliteness)
    {
        TenantRemoveConfigurationCommandSnapshot snapshot = TenantRemoveConfigurationCommandSnapshot
            .Idle()
            .Previewed(Intent())
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe status.", "ConfigurationKeyNotFound"));

        snapshot.State.ShouldBe(expectedState);
        snapshot.AuditState.ShouldBe(expectedAudit);
        snapshot.LiveRegionPoliteness.ShouldBe(expectedPoliteness);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    private static TenantRemoveConfigurationCommandSnapshot Pending()
        => TenantRemoveConfigurationCommandSnapshot
            .Idle()
            .Previewed(Intent())
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

    private static RemoveTenantConfiguration Intent()
        => new("tenant.alpha", "billing.mode");

    private static TenantConfigurationProjectionProof Proof(
        string tenantId,
        TenantConfigurationProjectionProofKind kind)
        => TenantConfigurationProjectionProof.Create(tenantId, kind);
}
