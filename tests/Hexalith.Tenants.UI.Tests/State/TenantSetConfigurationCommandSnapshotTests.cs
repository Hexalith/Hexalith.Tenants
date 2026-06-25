using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantSetConfigurationCommandSnapshotTests
{
    [Fact]
    public void Configuration_set_confirms_only_when_projection_contains_literal_key_and_value()
    {
        var intent = new SetTenantConfiguration("tenant.alpha", "billing.mode", "enterprise");
        TenantSetConfigurationCommandSnapshot snapshot = TenantSetConfigurationCommandSnapshot
            .Idle(Detail("tenant.alpha", "billing.mode", "trial"))
            .Previewed(intent, Detail("tenant.alpha", "billing.mode", "trial"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantSetConfigurationCommandSnapshot withoutEvidence = snapshot.ConfirmProjection(Detail("tenant.alpha", "billing.mode", "trial"));

        withoutEvidence.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        withoutEvidence.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
        withoutEvidence.LastConfirmedConfigurationProjection.ShouldNotBeNull()
            .Configuration["billing.mode"].ShouldBe("trial");

        TenantSetConfigurationCommandSnapshot confirmed = snapshot.ConfirmProjection(Detail("tenant.alpha", "billing.mode", "enterprise"));

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.FocusTarget.ShouldBe(TenantCommandFocusTarget.Lifecycle);
        confirmed.LastConfirmedConfigurationProjection.ShouldNotBeNull()
            .Configuration["billing.mode"].ShouldBe("enterprise");
        confirmed.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
    }

    [Fact]
    public void Completed_without_events_is_already_applied_only_after_projection_still_proves_value()
    {
        var intent = new SetTenantConfiguration("tenant.alpha", "billing.mode", "enterprise");
        TenantSetConfigurationCommandSnapshot snapshot = TenantSetConfigurationCommandSnapshot
            .Idle(Detail("tenant.alpha", "billing.mode", "trial"))
            .Previewed(intent, Detail("tenant.alpha", "billing.mode", "trial"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 0));

        TenantSetConfigurationCommandSnapshot withoutProjectionProof = snapshot.ConfirmProjection(Detail("tenant.alpha", "billing.mode", "trial"));

        withoutProjectionProof.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        withoutProjectionProof.State.ShouldNotBe(TenantCommandLifecycleState.AlreadyApplied);

        TenantSetConfigurationCommandSnapshot alreadyApplied = snapshot.ConfirmProjection(Detail("tenant.alpha", "billing.mode", "enterprise"));

        alreadyApplied.State.ShouldBe(TenantCommandLifecycleState.AlreadyApplied);
        alreadyApplied.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        alreadyApplied.SafeMessage.ShouldNotBeNull().ShouldContain("already applied", Case.Insensitive);
        alreadyApplied.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Signalr_nudge_never_changes_configuration_truth_without_projection_evidence()
    {
        TenantSetConfigurationCommandSnapshot snapshot = TenantSetConfigurationCommandSnapshot
            .Idle(Detail("tenant.alpha", "billing.mode", "trial"))
            .Previewed(
                new SetTenantConfiguration("tenant.alpha", "billing.mode", "enterprise"),
                Detail("tenant.alpha", "billing.mode", "trial"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        TenantSetConfigurationCommandSnapshot nudged = snapshot.SignalRNudge();

        nudged.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        nudged.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
        nudged.LastConfirmedConfigurationProjection.ShouldNotBeNull()
            .Configuration["billing.mode"].ShouldBe("trial");
        nudged.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void Blocked_configuration_flow_sets_assertive_recovery_focus_without_preview_or_raw_intent()
    {
        TenantSetConfigurationCommandSnapshot blocked = TenantSetConfigurationCommandSnapshot.Blocked(
            "Configuration scope evidence is unavailable.",
            TenantCommandFocusTarget.Namespace);

        blocked.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        blocked.FocusTarget.ShouldBe(TenantCommandFocusTarget.Namespace);
        blocked.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Assertive);
        blocked.AuditState.ShouldBe(TenantCommandAuditState.MissingSupport);
        blocked.Intent.ShouldBeNull();
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
            .Idle(Detail("tenant.alpha", "billing.mode", "trial"))
            .Previewed(
                new SetTenantConfiguration("tenant.alpha", "billing.mode", "enterprise"),
                Detail("tenant.alpha", "billing.mode", "trial"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe configuration status message.", "ConfigurationLimitExceeded"));

        snapshot.State.ShouldBe(expectedState);
        snapshot.AuditState.ShouldBe(expectedAudit);
        snapshot.LiveRegionPoliteness.ShouldBe(expectedPoliteness);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        snapshot.LastConfirmedConfigurationProjection.ShouldNotBeNull()
            .Configuration["billing.mode"].ShouldBe("trial");
    }

    [Theory]
    [InlineData(CommandStatus.Rejected, TenantCommandLifecycleState.Rejected, TenantCommandAuditState.AuditUnavailable)]
    [InlineData(CommandStatus.PublishFailed, TenantCommandLifecycleState.Degraded, TenantCommandAuditState.AuditDelayed)]
    [InlineData(CommandStatus.TimedOut, TenantCommandLifecycleState.UnableToVerify, TenantCommandAuditState.AuditDelayed)]
    public void Projection_evidence_cannot_convert_terminal_configuration_non_success_states_to_confirmed(
        CommandStatus status,
        TenantCommandLifecycleState expectedState,
        TenantCommandAuditState expectedAudit)
    {
        TenantSetConfigurationCommandSnapshot snapshot = TenantSetConfigurationCommandSnapshot
            .Idle(Detail("tenant.alpha", "billing.mode", "trial"))
            .Previewed(
                new SetTenantConfiguration("tenant.alpha", "billing.mode", "enterprise"),
                Detail("tenant.alpha", "billing.mode", "trial"))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe non-success configuration message.", "ConfigurationLimitExceeded"));

        TenantSetConfigurationCommandSnapshot withProjectionEvidence = snapshot.ConfirmProjection(Detail("tenant.alpha", "billing.mode", "enterprise"));

        withProjectionEvidence.State.ShouldBe(expectedState);
        withProjectionEvidence.AuditState.ShouldBe(expectedAudit);
        withProjectionEvidence.LastConfirmedConfigurationProjection.ShouldNotBeNull()
            .Configuration["billing.mode"].ShouldBe("enterprise");
        withProjectionEvidence.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
    }

    private static TenantDetail Detail(string tenantId, string key, string value)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            [new TenantMember("owner-user", TenantRole.TenantOwner)],
            new Dictionary<string, string> { [key] = value },
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
}
