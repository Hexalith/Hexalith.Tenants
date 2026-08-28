using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantAggregateCommandAdmissionGateTests
{
    [Fact]
    public void LeaseRequiresDispatchAndExplicitTerminalEvidenceToRelease()
    {
        var gate = new TenantAggregateCommandAdmissionGate();
        string key = TenantCommandAggregateLock.ForGlobalAdministrators();

        gate.TryAcquireLease(key, new object(), out TenantAggregateCommandLease? lease).ShouldBeTrue();
        lease.ShouldNotBeNull();
        lease.TryReleaseTerminal(TenantCommandLifecycleState.Confirmed).ShouldBeFalse();
        lease.TryMarkDispatched().ShouldBeTrue();
        lease.TryMarkDispatched().ShouldBeFalse();
        lease.TryAbandonBeforeDispatch().ShouldBeFalse();
        lease.TryReleaseTerminal(TenantCommandLifecycleState.Degraded).ShouldBeFalse();
        gate.IsLocked(key).ShouldBeTrue();
        lease.TryReleaseTerminal(TenantCommandLifecycleState.Confirmed).ShouldBeTrue();
        gate.IsLocked(key).ShouldBeFalse();
    }

    [Fact]
    public void ThrowingSubscriberCannotOrphanLeaseOrPreventLaterObservers()
    {
        var gate = new TenantAggregateCommandAdmissionGate();
        int notifications = 0;
        gate.StateChanged += static (_, _) => throw new InvalidOperationException("observer failure");
        gate.StateChanged += (_, _) => notifications++;

        gate.TryAcquireLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            new object(),
            out TenantAggregateCommandLease? lease).ShouldBeTrue();
        notifications.ShouldBe(1);
        lease.ShouldNotBeNull();
        lease.TryAbandonBeforeDispatch().ShouldBeTrue();
        notifications.ShouldBe(2);
    }

    [Fact]
    public void Same_aggregate_is_exclusive_and_only_owner_can_release()
    {
        var gate = new TenantAggregateCommandAdmissionGate();
        string identity = TenantCommandAggregateLock.ForTenant("tenant.alpha");
        object owner = new();
        object contender = new();

        gate.TryAcquire(identity, owner).ShouldBeTrue();
        gate.IsLocked(identity).ShouldBeTrue();
        gate.IsOwnedBy(identity, owner).ShouldBeTrue();
        gate.IsOwnedBy(identity, contender).ShouldBeFalse();
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
