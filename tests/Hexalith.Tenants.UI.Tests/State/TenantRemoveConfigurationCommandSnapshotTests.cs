using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantRemoveConfigurationCommandSnapshotTests
{
    [Fact]
    public void Configuration_remove_confirms_only_when_projection_no_longer_contains_literal_key()
    {
        var intent = new RemoveTenantConfigurationCommandRequest("tenant.alpha", "billing.mode");
        TenantRemoveConfigurationCommandSnapshot snapshot = TenantRemoveConfigurationCommandSnapshot
            .Idle(Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Previewed(intent, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));

        TenantRemoveConfigurationCommandSnapshot keyStillVisible = snapshot.ConfirmProjection(
            Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }));

        keyStillVisible.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        keyStillVisible.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
        keyStillVisible.LastConfirmedConfigurationProjection.ShouldNotBeNull()
            .Configuration.ContainsKey("billing.mode").ShouldBeTrue();

        TenantRemoveConfigurationCommandSnapshot confirmed = snapshot.ConfirmProjection(
            Detail("tenant.alpha", new Dictionary<string, string> { ["billing.other"] = "kept" }));

        confirmed.State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        confirmed.FocusTarget.ShouldBe(TenantCommandFocusTarget.Lifecycle);
        confirmed.LastConfirmedConfigurationProjection.ShouldNotBeNull()
            .Configuration.ContainsKey("billing.mode").ShouldBeFalse();
        confirmed.LastConfirmedConfigurationProjection.Configuration["billing.other"].ShouldBe("kept");
        confirmed.AuditState.ShouldBe(TenantCommandAuditState.AuditPending);
    }

    [Fact]
    public void Configuration_key_not_found_stays_rejected_even_when_projection_later_lacks_key()
    {
        var intent = new RemoveTenantConfigurationCommandRequest("tenant.alpha", "billing.mode");
        TenantRemoveConfigurationCommandSnapshot snapshot = TenantRemoveConfigurationCommandSnapshot
            .Idle(Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Previewed(intent, Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(
                CommandStatus.Rejected,
                "The configuration key was not found.",
                "ConfigurationKeyNotFound"));

        TenantRemoveConfigurationCommandSnapshot withAbsentProjection = snapshot.ConfirmProjection(
            Detail("tenant.alpha", new Dictionary<string, string>()));

        withAbsentProjection.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        withAbsentProjection.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        withAbsentProjection.RejectionCode.ShouldBe("ConfigurationKeyNotFound");
        withAbsentProjection.AuditState.ShouldBe(TenantCommandAuditState.AuditUnavailable);
    }

    [Fact]
    public void Signalr_nudge_never_removes_configuration_without_projection_evidence()
    {
        TenantRemoveConfigurationCommandSnapshot snapshot = TenantRemoveConfigurationCommandSnapshot
            .Idle(Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Previewed(
                new RemoveTenantConfigurationCommandRequest("tenant.alpha", "billing.mode"),
                Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"));

        TenantRemoveConfigurationCommandSnapshot nudged = snapshot.SignalRNudge();

        nudged.State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        nudged.FocusTarget.ShouldBe(TenantCommandFocusTarget.Refresh);
        nudged.LastConfirmedConfigurationProjection.ShouldNotBeNull()
            .Configuration["billing.mode"].ShouldBe("trial");
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
    public void Configuration_remove_status_results_remain_distinct_before_projection_confirmation(
        CommandStatus status,
        TenantCommandLifecycleState expectedState,
        TenantCommandAuditState expectedAudit,
        TenantCommandLiveRegionPoliteness expectedPoliteness)
    {
        TenantRemoveConfigurationCommandSnapshot snapshot = TenantRemoveConfigurationCommandSnapshot
            .Idle(Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .Previewed(
                new RemoveTenantConfigurationCommandRequest("tenant.alpha", "billing.mode"),
                Detail("tenant.alpha", new Dictionary<string, string> { ["billing.mode"] = "trial" }))
            .RequestSent()
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(new TenantCommandStatusResult(status, "Safe configuration status message.", "ConfigurationKeyNotFound"));

        snapshot.State.ShouldBe(expectedState);
        snapshot.AuditState.ShouldBe(expectedAudit);
        snapshot.LiveRegionPoliteness.ShouldBe(expectedPoliteness);
        snapshot.State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        snapshot.LastConfirmedConfigurationProjection.ShouldNotBeNull()
            .Configuration["billing.mode"].ShouldBe("trial");
    }

    private static TenantDetail Detail(string tenantId, Dictionary<string, string> configuration)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            [new TenantMember("owner-user", TenantRole.TenantOwner)],
            configuration,
            DateTimeOffset.Parse("2026-06-01T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
}
