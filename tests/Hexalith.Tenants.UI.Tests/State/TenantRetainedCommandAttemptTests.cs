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
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AbandonedSameIdentityRemovalRetryRestoresOriginalEvidenceWithoutHoldingAdmission(string? correlationId)
    {
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate);
        object owner = new();
        TenantRemoveMemberCommandSnapshot pending = new(TenantCommandLifecycleState.RequestSent,
            new("tenant.alpha", "reader"), MessageId: "original", BaselineProjectionVersion: "tenant-sequence:1",
            AttemptStartedAtUtc: DateTimeOffset.UtcNow);
        tracker.TryBegin("tenant.alpha", owner, pending, out TenantRetainedCommandAttempt? original).ShouldBeTrue();
        tracker.TryStartDelivery(original!, owner).ShouldBeTrue();
        TenantRemoveMemberCommandSnapshot failed = pending with { State = TenantCommandLifecycleState.Failed, CorrelationId = correlationId };
        tracker.TryCompleteDelivery(original!, pending, failed).ShouldBeTrue();
        tracker.Detach(original, owner);
        TenantRemoveMemberCommandSnapshot retry = failed.RequestSent("tenant-sequence:2", attemptStartedAtUtc: failed.AttemptStartedAtUtc) with { CorrelationId = null };
        tracker.TryBegin("tenant.alpha", owner, retry, out TenantRetainedCommandAttempt? admission).ShouldBeTrue();
        tracker.Detach(admission, new object());
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
        tracker.Detach(admission, owner);
        tracker.Find<TenantRemoveMemberCommandSnapshot>("tenant.alpha").ShouldBeSameAs(failed);
        tracker.HasPendingOwnership("tenant.alpha").ShouldBeFalse();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
        object replacement = new();
        tracker.TryAdopt<TenantRemoveMemberCommandSnapshot>("tenant.alpha", replacement, out var restored, out var evidence).ShouldBeTrue();
        restored.ShouldBeSameAs(original);
        evidence.ShouldBeSameAs(failed);
        tracker.Detach(restored, replacement);
        tracker.TryBegin("tenant.alpha", replacement, retry, out var next).ShouldBeTrue();
        tracker.Detach(admission, owner);
        tracker.IsAttached(next!, replacement).ShouldBeTrue();
        tracker.TryCompleteDelivery(admission!, retry, failed).ShouldBeFalse();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
    }

    [Theory]
    [InlineData("message")]
    [InlineData("intent")]
    [InlineData("started")]
    [InlineData("correlation")]
    public void UnrelatedUndispatchedRemovalDoesNotRestoreOlderFailure(string mismatch)
    {
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate);
        object owner = new();
        TenantRemoveMemberCommandSnapshot failed = new(TenantCommandLifecycleState.Failed,
            new("tenant.alpha", "reader"), MessageId: "original", AttemptStartedAtUtc: DateTimeOffset.UtcNow);
        tracker.TryBegin("tenant.alpha", owner, failed, out var original).ShouldBeTrue();
        tracker.TryUpdate(original!, failed, failed).ShouldBeTrue();
        tracker.Detach(original, owner);
        TenantRemoveMemberCommandSnapshot retry = failed.RequestSent(attemptStartedAtUtc: failed.AttemptStartedAtUtc);
        retry = mismatch switch
        {
            "message" => retry with { MessageId = "different" },
            "intent" => retry with { Intent = new("tenant.alpha", "other-reader") },
            "started" => retry with { AttemptStartedAtUtc = retry.AttemptStartedAtUtc!.Value.AddMinutes(1) },
            "correlation" => retry with { CorrelationId = "known" },
            _ => throw new ArgumentException("Unknown mismatch.", nameof(mismatch)),
        };
        tracker.TryBegin("tenant.alpha", owner, retry, out var admission).ShouldBeTrue();
        tracker.Detach(admission, owner);
        tracker.Find<TenantRemoveMemberCommandSnapshot>("tenant.alpha").ShouldBeNull();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
    }

    [Fact]
    public void DispatchedRetryCannotRestorePriorTerminalFailureAfterRendererDetaches()
    {
        TenantAggregateCommandAdmissionGate gate = new();
        using TenantCommandAttemptTracker tracker = new(gate);
        object owner = new();
        TenantRemoveMemberCommandSnapshot failed = new(TenantCommandLifecycleState.Failed,
            new("tenant.alpha", "reader"), MessageId: "original", AttemptStartedAtUtc: DateTimeOffset.UtcNow);
        tracker.TryBegin("tenant.alpha", owner, failed, out var original).ShouldBeTrue();
        tracker.TryUpdate(original!, failed, failed).ShouldBeTrue();
        tracker.Detach(original, owner);
        TenantRemoveMemberCommandSnapshot retry = failed.RequestSent(attemptStartedAtUtc: failed.AttemptStartedAtUtc);
        tracker.TryBegin("tenant.alpha", owner, retry, out var admission).ShouldBeTrue();
        tracker.TryStartDelivery(admission!, owner).ShouldBeTrue();
        tracker.Detach(admission, owner);
        tracker.Find<TenantRemoveMemberCommandSnapshot>("tenant.alpha").ShouldBeSameAs(retry);
        tracker.HasPendingOwnership("tenant.alpha").ShouldBeTrue();
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeTrue();
        tracker.TryCompleteDelivery(admission!, retry, retry with { State = TenantCommandLifecycleState.Rejected }).ShouldBeTrue();
        tracker.Find<TenantRemoveMemberCommandSnapshot>("tenant.alpha")!.State.ShouldBe(TenantCommandLifecycleState.Rejected);
        gate.IsLocked(TenantCommandAggregateLock.ForTenant("tenant.alpha")).ShouldBeFalse();
    }

}
