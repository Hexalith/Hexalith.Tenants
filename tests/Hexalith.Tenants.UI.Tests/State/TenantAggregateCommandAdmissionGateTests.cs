using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantAggregateCommandAdmissionGateTests
{
    [Fact]
    public void Same_aggregate_stays_locked_until_balanced_release()
    {
        var gate = new TenantAggregateCommandAdmissionGate();
        string identity = TenantCommandAggregateLock.ForTenant("tenant.alpha");

        gate.TryAcquire(identity).ShouldBeTrue();
        gate.IsLocked(identity).ShouldBeTrue();

        gate.TryAcquire(identity).ShouldBeTrue();
        gate.Release(identity);
        gate.IsLocked(identity).ShouldBeTrue();
        gate.Release(identity);
        gate.IsLocked(identity).ShouldBeFalse();
    }

    [Fact]
    public void Unrelated_aggregates_may_proceed_while_one_aggregate_is_locked()
    {
        var gate = new TenantAggregateCommandAdmissionGate();
        string alpha = TenantCommandAggregateLock.ForTenant("tenant.alpha");
        string beta = TenantCommandAggregateLock.ForTenant("tenant.beta");

        gate.TryAcquire(alpha).ShouldBeTrue();
        gate.TryAcquire(beta).ShouldBeTrue();
        gate.IsLocked(alpha).ShouldBeTrue();
        gate.IsLocked(beta).ShouldBeTrue();

        gate.Release(alpha);
        gate.IsLocked(alpha).ShouldBeFalse();
        gate.IsLocked(beta).ShouldBeTrue();
    }

    [Fact]
    public void Lock_key_preserves_literal_tenant_id_characters()
    {
        const string tenantId = "  tenant/%2F?x=é&glyph=о  ";
        TenantCommandAggregateLock.ForTenant(tenantId)
            .ShouldBe($"system:tenants:{tenantId}");
    }
}
