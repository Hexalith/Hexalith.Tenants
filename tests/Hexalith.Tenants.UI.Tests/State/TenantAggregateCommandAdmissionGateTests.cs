using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantAggregateCommandAdmissionGateTests
{
    [Fact]
    public void Same_aggregate_is_exclusive_and_only_owner_can_release()
    {
        var gate = new TenantAggregateCommandAdmissionGate();
        string identity = TenantCommandAggregateLock.ForTenant("tenant.alpha");
        object owner = new();
        object contender = new();

        gate.TryAcquire(identity, owner).ShouldBeTrue();
        gate.IsLocked(identity).ShouldBeTrue();
        gate.IsLockedByAnother(identity, owner).ShouldBeFalse();
        gate.IsLockedByAnother(identity, contender).ShouldBeTrue();

        gate.TryAcquire(identity, owner).ShouldBeFalse();
        gate.TryAcquire(identity, contender).ShouldBeFalse();
        gate.Release(identity, contender);
        gate.IsLocked(identity).ShouldBeTrue();
        gate.Release(identity, owner);
        gate.IsLocked(identity).ShouldBeFalse();
    }

    [Fact]
    public void Unrelated_aggregates_may_proceed_while_one_aggregate_is_locked()
    {
        var gate = new TenantAggregateCommandAdmissionGate();
        string alpha = TenantCommandAggregateLock.ForTenant("tenant.alpha");
        string beta = TenantCommandAggregateLock.ForTenant("tenant.beta");
        object alphaOwner = new();
        object betaOwner = new();

        gate.TryAcquire(alpha, alphaOwner).ShouldBeTrue();
        gate.TryAcquire(beta, betaOwner).ShouldBeTrue();
        gate.IsLocked(alpha).ShouldBeTrue();
        gate.IsLocked(beta).ShouldBeTrue();

        gate.Release(alpha, alphaOwner);
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
