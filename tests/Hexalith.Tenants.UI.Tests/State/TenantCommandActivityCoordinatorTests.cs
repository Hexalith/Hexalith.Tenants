using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantCommandActivityCoordinatorTests
{
    [Fact]
    public async Task Held_activity_fault_after_disposal_does_not_resolve_detached_services()
    {
        var coordinator = new TenantCommandActivityCoordinator();
        using var tracker = new TenantCommandAttemptTracker(new TenantAggregateCommandAdmissionGate());
        object owner = new();
        TenantRetainedCommandAttempt? attempt = null;
        bool disposed = false;
        int detachedServiceAccesses = 0;
        var held = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool> pending = coordinator.SetAsync(true, () => disposed, () => 1, () => attempt,
            () => tracker.TryBegin("tenant.alpha", owner, TenantCreateCommandSnapshot.Idle(), out attempt),
            () => false, _ => throw new InvalidOperationException("Disposed activity cannot publish."), _ =>
            {
                detachedServiceAccesses++;
                throw new ObjectDisposedException("circuit services");
            }, _ => held.Task);

        disposed = true;
        held.SetException(new InvalidOperationException("Late provider fault"));

        (await pending).ShouldBeFalse();
        detachedServiceAccesses.ShouldBe(0);
    }

    [Fact]
    public async Task Late_successful_old_activity_cannot_overwrite_acquired_replacement_or_its_exact_release()
    {
        var coordinator = new TenantCommandActivityCoordinator();
        var gate = new TenantAggregateCommandAdmissionGate();
        using var tracker = new TenantCommandAttemptTracker(gate);
        object owner = new();
        TenantRetainedCommandAttempt? attempt = null;
        int generation = 1;
        bool raised = false;
        List<(int Generation, bool Active)> notifications = [];
        var held = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool> Change(bool active) => coordinator.SetAsync(active, () => false, () => generation, () => attempt,
            () => tracker.TryBegin("tenant.alpha", owner, TenantCreateCommandSnapshot.Idle(), out attempt),
            () => raised, value => raised = value, captured => tracker.Detach(captured, owner), value =>
            {
                notifications.Add((generation, value));
                return generation == 1 ? held.Task : Task.FromResult(true);
            });
        Task<bool> old = Change(true);
        tracker.Detach(attempt, owner);
        attempt = null;
        generation++;
        (await Change(true).WaitAsync(TimeSpan.FromSeconds(5))).ShouldBeTrue();
        TenantRetainedCommandAttempt replacement = attempt.ShouldNotBeNull();
        raised.ShouldBeTrue();

        held.SetResult(true);
        (await old).ShouldBeFalse();
        raised.ShouldBeTrue();
        tracker.IsAttached(replacement, owner).ShouldBeTrue();
        replacement.Lease.IsReleased.ShouldBeFalse();
        gate.TryAcquire(TenantCommandAggregateLock.ForTenant("tenant.alpha"), new object()).ShouldBeFalse();
        (await Change(false)).ShouldBeTrue();
        (await Change(false)).ShouldBeTrue();
        raised.ShouldBeFalse();
        notifications.ShouldBe([(1, true), (2, true), (2, false)]);
        tracker.Detach(replacement, owner);
        replacement.Lease.IsReleased.ShouldBeTrue();
    }

    [Fact]
    public async Task Repeated_held_activity_joins_one_parent_admission_and_releases_once()
    {
        var coordinator = new TenantCommandActivityCoordinator();
        using var tracker = new TenantCommandAttemptTracker(new TenantAggregateCommandAdmissionGate());
        object owner = new();
        TenantRetainedCommandAttempt? attempt = null;
        bool raised = false;
        int notifications = 0;
        var held = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool> Change(bool active) => coordinator.SetAsync(active, () => false, () => 1, () => attempt,
            () => tracker.TryBegin("tenant.alpha", owner, TenantCreateCommandSnapshot.Idle(), out attempt),
            () => raised, value => raised = value, captured => tracker.Detach(captured, owner),
            async value => { notifications++; return value ? await held.Task : true; });

        Task<bool> first = Change(true);
        Task<bool> repeated = Change(true);
        notifications.ShouldBe(1);
        repeated.IsCompleted.ShouldBeFalse();
        held.SetResult(true);
        (await first).ShouldBeTrue();
        (await repeated).ShouldBeTrue();
        notifications.ShouldBe(1);
        raised.ShouldBeTrue();
        (await Change(false)).ShouldBeTrue();
        (await Change(false)).ShouldBeTrue();
        notifications.ShouldBe(2);
        raised.ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Late_activity_result_cannot_release_replacement_attempt(bool failure)
    {
        var coordinator = new TenantCommandActivityCoordinator();
        var gate = new TenantAggregateCommandAdmissionGate();
        using var tracker = new TenantCommandAttemptTracker(gate);
        object owner = new();
        TenantRetainedCommandAttempt? attempt = null;
        int generation = 1;
        bool raised = false;
        var held = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool> pending = coordinator.SetAsync(true, () => false, () => generation, () => attempt,
            () => tracker.TryBegin("tenant.alpha", owner, TenantCreateCommandSnapshot.Idle(), out attempt),
            () => raised, value => raised = value, captured =>
            {
                tracker.Detach(captured, owner);
                if (ReferenceEquals(attempt, captured)) attempt = null;
            }, _ => held.Task);
        TenantRetainedCommandAttempt original = attempt.ShouldNotBeNull();
        tracker.Detach(original, owner);
        generation++;
        tracker.TryBegin("tenant.alpha", owner, TenantCreateCommandSnapshot.Idle(), out attempt).ShouldBeTrue();
        TenantRetainedCommandAttempt replacement = attempt.ShouldNotBeNull();
        if (failure) held.SetException(new InvalidOperationException("Held callback failed"));
        else held.SetResult(false);

        (await pending).ShouldBeFalse();

        attempt.ShouldBeSameAs(replacement);
        tracker.IsAttached(replacement, owner).ShouldBeTrue();
        replacement.Lease.IsReleased.ShouldBeFalse();
        gate.TryAcquire(TenantCommandAggregateLock.ForTenant("tenant.alpha"), new object()).ShouldBeFalse();
        raised.ShouldBeFalse();
    }
}
