using Hexalith.Tenants.UI.State.TenantAudit;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

/// <summary>Pins the currently observable availability numbers without inventing a historical wire migration.</summary>
public sealed class TenantAuditAvailabilityStateTests
{
    [Theory]
    [InlineData(TenantAuditAvailabilityState.Pending, 0)]
    [InlineData(TenantAuditAvailabilityState.Delayed, 1)]
    [InlineData(TenantAuditAvailabilityState.Unavailable, 2)]
    [InlineData(TenantAuditAvailabilityState.Available, 3)]
    [InlineData(TenantAuditAvailabilityState.MissingSupport, 4)]
    public void AvailabilityNumbersRemainStable(TenantAuditAvailabilityState state, int expected)
        => ((int)state).ShouldBe(expected);
}
