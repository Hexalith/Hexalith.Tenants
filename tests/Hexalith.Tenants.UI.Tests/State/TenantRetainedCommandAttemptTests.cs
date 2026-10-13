using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.State;

/// <summary>Verifies terminal truth and exact retained admission independently of renderer lifetime.</summary>
public sealed class TenantRetainedCommandAttemptTests
{
    [Theory]
    [InlineData(TenantCommandLifecycleState.Confirmed)]
    [InlineData(TenantCommandLifecycleState.Rejected)]
    [InlineData(TenantCommandLifecycleState.AlreadyApplied)]
    [InlineData(TenantCommandLifecycleState.Failed)]
    public void TerminalSnapshotsIgnoreEveryLateStatusShape(TenantCommandLifecycleState state)
    {
        TenantCreateCommandSnapshot create = new(state, MessageId: "original", CorrelationId: "correlation");
        TenantAddMemberCommandSnapshot add = new(state, MessageId: "original", CorrelationId: "correlation");
        TenantChangeRoleCommandSnapshot change = new(state, MessageId: "original", CorrelationId: "correlation");
        TenantUpdateMetadataCommandSnapshot metadata = new(state, MessageId: "original", CorrelationId: "correlation");
        TenantRemoveMemberCommandSnapshot remove = new(state, MessageId: "original", CorrelationId: "correlation");
        TenantCommandStatusResult[] statuses =
        [
            new(CommandStatus.Processing, HasVerifiedCommandIdentity: true),
            new(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true),
            new(CommandStatus.Rejected, HasVerifiedCommandIdentity: true),
            new(CommandStatus.TimedOut, HasVerifiedCommandIdentity: true),
            new(CommandStatus.PublishFailed, HasVerifiedCommandIdentity: true),
            TenantCommandStatusResult.Unknown("Unavailable"),
            TenantCommandStatusResult.Pending("Pending"),
        ];
        foreach (TenantCommandStatusResult status in statuses)
        {
            create.ApplyStatus(status).ShouldBeSameAs(create);
            add.ApplyStatus(status).ShouldBeSameAs(add);
            change.ApplyStatus(status).ShouldBeSameAs(change);
            metadata.ApplyStatus(status).ShouldBeSameAs(metadata);
            remove.ApplyStatus(status).ShouldBeSameAs(remove);
        }
    }

    [Fact]
    public void ObserverFailureCannotLoseOwnerlessTerminalDeliveryOrItsLeaseRelease()
    {
        TenantAggregateCommandAdmissionGate gate = new();
        TenantCommandAttemptTracker tracker = new(gate);
        object owner = new();
        TenantCreateCommandSnapshot expected = TenantCreateCommandSnapshot.Idle()
            .RequestSent(new("tenant.alpha", "Original", null), "tenant-sequence:1", true)
            with { MessageId = "original" };
        tracker.TryBegin("tenant.alpha", owner, expected, out TenantRetainedCommandAttempt? attempt).ShouldBeTrue();
        tracker.TryStartDelivery(attempt!, owner).ShouldBeTrue();
        tracker.Detach(attempt, owner);
        tracker.StateChanged += (_, _) => throw new ObjectDisposedException("renderer");
        TenantCreateCommandSnapshot terminal = expected with { State = TenantCommandLifecycleState.Rejected };
        tracker.TryCompleteDelivery(attempt!, expected, terminal).ShouldBeTrue();
        tracker.Find<TenantCreateCommandSnapshot>("tenant.alpha").ShouldBeSameAs(terminal);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
    }

    [Fact]
    public void TerminalProofEnrichmentPreservesAdmissionTruthAndRejectsAStalePreTerminalResult()
    {
        TenantAggregateCommandAdmissionGate gate = new();
        TenantCommandAttemptTracker tracker = new(gate);
        object owner = new();
        TenantRemoveMemberCommandSnapshot pending = new(TenantCommandLifecycleState.ProjectionPending,
            new("tenant.alpha", "reader"), MessageId: "original", CorrelationId: "correlation", HasCommandEventEvidence: true);
        tracker.TryBegin("tenant.alpha", owner, pending, out TenantRetainedCommandAttempt? attempt).ShouldBeTrue();
        tracker.TryStartDelivery(attempt!, owner).ShouldBeTrue();
        tracker.TryCompleteDelivery(attempt!, pending, pending).ShouldBeTrue();
        TenantRemoveMemberCommandSnapshot confirmed = pending with { State = TenantCommandLifecycleState.Confirmed };
        tracker.TryUpdate(attempt!, pending, confirmed).ShouldBeTrue();
        object replacementOwner = new();
        string key = TenantCommandAggregateLock.ForTenant("tenant.alpha");
        gate.TryAcquireLease(key, replacementOwner, out TenantAggregateCommandLease? replacement).ShouldBeTrue();
        TenantRemoveMemberCommandSnapshot proven = confirmed with { AuditState = TenantCommandAuditState.AuditAvailable };
        tracker.TryUpdate(attempt!, confirmed, proven).ShouldBeTrue();
        tracker.TryUpdate(attempt!, pending, pending with { State = TenantCommandLifecycleState.Accepted }).ShouldBeFalse();
        tracker.Find<TenantRemoveMemberCommandSnapshot>("tenant.alpha").ShouldBeSameAs(proven);
        gate.IsLocked(key).ShouldBeTrue();
        replacement!.TryAbandonBeforeDispatch(replacementOwner).ShouldBeTrue();
    }
}
