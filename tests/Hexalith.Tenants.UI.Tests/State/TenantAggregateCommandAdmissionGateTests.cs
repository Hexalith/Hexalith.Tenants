using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.GlobalAdministrators;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantAggregateCommandAdmissionGateTests
{
    [Fact]
    public void LeaseRequiresDispatchAndExplicitTerminalEvidenceToRelease()
    {
        var gate = new TenantAggregateCommandAdmissionGate();
        string key = TenantCommandAggregateLock.ForGlobalAdministrators();
        object owner = new();

        gate.TryAcquireLease(key, owner, out TenantAggregateCommandLease? lease).ShouldBeTrue();
        lease.ShouldNotBeNull();
        lease.TryReleaseTerminal(owner, TenantCommandLifecycleState.Confirmed).ShouldBeFalse();
        lease.TryMarkDispatched(owner).ShouldBeTrue();
        lease.TryMarkDispatched(owner).ShouldBeFalse();
        lease.TryAbandonBeforeDispatch(owner).ShouldBeFalse();
        lease.TryReleaseTerminal(owner, TenantCommandLifecycleState.Degraded).ShouldBeFalse();
        gate.IsLocked(key).ShouldBeTrue();
        lease.TryReleaseTerminal(owner, TenantCommandLifecycleState.Confirmed).ShouldBeTrue();
        gate.IsLocked(key).ShouldBeFalse();
    }

    [Fact]
    public void ThrowingSubscriberCannotOrphanLeaseOrPreventLaterObservers()
    {
        var gate = new TenantAggregateCommandAdmissionGate();
        int notifications = 0;
        object owner = new();
        gate.StateChanged += static (_, _) => throw new InvalidOperationException("observer failure");
        gate.StateChanged += (_, _) => notifications++;

        gate.TryAcquireLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            owner,
            out TenantAggregateCommandLease? lease).ShouldBeTrue();
        notifications.ShouldBe(1);
        lease.ShouldNotBeNull();
        lease.TryAbandonBeforeDispatch(owner).ShouldBeTrue();
        notifications.ShouldBe(2);
    }

    [Fact]
    public void SameAggregateIsExclusiveAndOnlyOwnerCanRelease()
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
    public void UnrelatedAggregatesMayProceedWhileOneAggregateIsLocked()
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
    public void LockKeyPreservesLiteralTenantIdCharacters()
    {
        const string tenantId = "  tenant/%2F?x=é&glyph=о  ";
        TenantCommandAggregateLock.ForTenant(tenantId)
            .ShouldBe($"system:tenants:{tenantId}");
    }

    [Fact]
    public void RetainedReconciliationIsAdoptedByExactlyOneReplacementOwner()
    {
        var gate = new TenantAggregateCommandAdmissionGate();
        string key = TenantCommandAggregateLock.ForGlobalAdministrators();
        object originalOwner = new();
        object replacementOwner = new();
        object contender = new();
        var retained = new GlobalAdministratorReconciliationState(
            GlobalAdministratorActionKind.Remove,
            "admin-user",
            "message-safe",
            "correlation-safe",
            TenantCommandLifecycleState.Accepted);

        gate.TryAcquireLease(key, originalOwner, out TenantAggregateCommandLease? lease).ShouldBeTrue();
        lease.ShouldNotBeNull();
        lease.TryMarkDispatched(originalOwner).ShouldBeTrue();
        lease.TryRetainReconciliation(originalOwner, retained).ShouldBeTrue();

        gate.TryAdoptRetainedLease(key, replacementOwner, out TenantAggregateCommandLease? adopted, out GlobalAdministratorReconciliationState? state).ShouldBeTrue();
        adopted.ShouldBeSameAs(lease);
        state.ShouldBe(retained);
        gate.TryAdoptRetainedLease(key, contender, out _, out _).ShouldBeFalse();
        lease.TryAdvanceReconciliation(originalOwner, retained with { LifecycleState = TenantCommandLifecycleState.ProjectionPending }).ShouldBeFalse();
        lease.TryAdvanceReconciliation(replacementOwner, retained with { LifecycleState = TenantCommandLifecycleState.ProjectionPending }).ShouldBeTrue();
        lease.TryAdvanceReconciliation(replacementOwner, retained).ShouldBeFalse();
        lease.TryReleaseTerminal(originalOwner, TenantCommandLifecycleState.Confirmed).ShouldBeFalse();
        lease.TryReleaseTerminal(replacementOwner, TenantCommandLifecycleState.Confirmed).ShouldBeTrue();
        lease.TryRetainReconciliation(replacementOwner, retained).ShouldBeFalse();
        gate.IsLocked(key).ShouldBeFalse();
    }

    [Fact]
    public async Task RetainAndTerminalReleaseAreSerializedWithoutResurrectingAReleasedLease()
    {
        var gate = new TenantAggregateCommandAdmissionGate();
        string key = TenantCommandAggregateLock.ForGlobalAdministrators();
        object owner = new();
        var retained = new GlobalAdministratorReconciliationState(
            GlobalAdministratorActionKind.Grant,
            "admin-user",
            "message-safe",
            "correlation-safe",
            TenantCommandLifecycleState.Accepted);
        gate.TryAcquireLease(key, owner, out TenantAggregateCommandLease? lease).ShouldBeTrue();
        lease.ShouldNotBeNull();
        lease.TryMarkDispatched(owner).ShouldBeTrue();

        Task<bool> retain = Task.Run(() => lease.TryRetainReconciliation(owner, retained));
        Task<bool> release = Task.Run(() => lease.TryReleaseTerminal(owner, TenantCommandLifecycleState.Confirmed));
        bool[] outcomes = await Task.WhenAll(retain, release);

        outcomes.Count(static outcome => outcome).ShouldBe(1);
        if (outcomes[1])
        {
            gate.IsLocked(key).ShouldBeFalse();
            lease.TryRetainReconciliation(owner, retained).ShouldBeFalse();
        }
        else
        {
            gate.IsLocked(key).ShouldBeTrue();
            gate.TryAdoptRetainedLease(key, new object(), out _, out _).ShouldBeTrue();
        }
    }
}
