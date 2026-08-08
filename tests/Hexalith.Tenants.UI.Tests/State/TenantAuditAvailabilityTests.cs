using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantAuditAvailabilityTests
{
    [Theory]
    [InlineData(TenantCommandAuditState.AuditPending, TenantAuditAvailabilityState.Pending, TenantCommandLiveRegionPoliteness.Polite, false)]
    [InlineData(TenantCommandAuditState.AuditDelayed, TenantAuditAvailabilityState.Delayed, TenantCommandLiveRegionPoliteness.Polite, false)]
    [InlineData(TenantCommandAuditState.AuditUnavailable, TenantAuditAvailabilityState.Unavailable, TenantCommandLiveRegionPoliteness.Assertive, false)]
    [InlineData(TenantCommandAuditState.AuditAvailable, TenantAuditAvailabilityState.Available, TenantCommandLiveRegionPoliteness.Polite, true)]
    [InlineData(TenantCommandAuditState.MissingSupport, TenantAuditAvailabilityState.MissingSupport, TenantCommandLiveRegionPoliteness.Assertive, false)]
    public void Availability_maps_command_audit_states(
        TenantCommandAuditState commandState,
        TenantAuditAvailabilityState expectedState,
        TenantCommandLiveRegionPoliteness expectedPoliteness,
        bool expectedAvailable)
    {
        TenantAuditAvailability availability = TenantAuditAvailability.FromCommandAuditState(commandState);

        availability.State.ShouldBe(expectedState);
        availability.ShouldRender.ShouldBeTrue();
        availability.IsAuditAvailable.ShouldBe(expectedAvailable);
        availability.LiveRegionPoliteness.ShouldBe(expectedPoliteness);
    }

    [Fact]
    public void Availability_keeps_not_started_non_rendered_and_non_success()
    {
        TenantAuditAvailability availability = TenantAuditAvailability.FromCommandAuditState(TenantCommandAuditState.NotStarted);

        availability.State.ShouldBeNull();
        availability.RecoveryVerbs.ShouldBeEmpty();
        availability.ShouldRender.ShouldBeFalse();
        availability.IsAuditAvailable.ShouldBeFalse();
        availability.LiveRegionPoliteness.ShouldBe(TenantCommandLiveRegionPoliteness.Polite);
    }

    [Theory]
    [InlineData(TenantCommandAuditState.AuditPending, new[] { TenantAuditRecoveryVerb.Wait, TenantAuditRecoveryVerb.Refresh, TenantAuditRecoveryVerb.InspectAudit })]
    [InlineData(TenantCommandAuditState.AuditDelayed, new[] { TenantAuditRecoveryVerb.Refresh, TenantAuditRecoveryVerb.InspectAudit })]
    [InlineData(TenantCommandAuditState.AuditUnavailable, new[] { TenantAuditRecoveryVerb.ContinueReadOnly, TenantAuditRecoveryVerb.Refresh, TenantAuditRecoveryVerb.Escalate })]
    [InlineData(TenantCommandAuditState.AuditAvailable, new[] { TenantAuditRecoveryVerb.InspectAudit, TenantAuditRecoveryVerb.ContinueReadOnly })]
    [InlineData(TenantCommandAuditState.MissingSupport, new[] { TenantAuditRecoveryVerb.ContinueReadOnly, TenantAuditRecoveryVerb.Escalate })]
    public void Availability_maps_canonical_recovery_verbs(
        TenantCommandAuditState commandState,
        TenantAuditRecoveryVerb[] expectedVerbs)
    {
        TenantAuditAvailability availability = TenantAuditAvailability.FromCommandAuditState(commandState);

        availability.RecoveryVerbs.ShouldBe(expectedVerbs);
    }
}
