using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

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
            TenantCommandLifecycleState.Accepted,
            RemovePreview: RemovePreview());

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
    public void MismatchingClaimantLeavesRetainedReconciliationForMatchingReplacement()
    {
        var gate = new TenantAggregateCommandAdmissionGate();
        string key = TenantCommandAggregateLock.ForGlobalAdministrators();
        object originalOwner = new();
        var retained = new GlobalAdministratorReconciliationState(
            GlobalAdministratorActionKind.Remove,
            "admin-user",
            "message-safe",
            "correlation-safe",
            TenantCommandLifecycleState.Accepted,
            RemovePreview: RemovePreview());
        gate.TryAcquireLease(key, originalOwner, out TenantAggregateCommandLease? lease).ShouldBeTrue();
        lease.ShouldNotBeNull();
        lease.TryMarkDispatched(originalOwner).ShouldBeTrue();
        lease.TryRetainReconciliation(originalOwner, retained).ShouldBeTrue();

        gate.TryAdoptRetainedLease(
            key,
            new object(),
            GlobalAdministratorActionKind.Grant,
            "different-admin",
            out _,
            out _).ShouldBeFalse();

        object matchingOwner = new();
        gate.TryAdoptRetainedLease(
            key,
            matchingOwner,
            GlobalAdministratorActionKind.Remove,
            "admin-user",
            out TenantAggregateCommandLease? adopted,
            out GlobalAdministratorReconciliationState? reconciliation).ShouldBeTrue();
        adopted.ShouldBeSameAs(lease);
        reconciliation.ShouldBe(retained);
        adopted!.TryReleaseTerminal(matchingOwner, TenantCommandLifecycleState.Confirmed).ShouldBeTrue();
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

    [Fact]
    public void RequestSentGrantCanBeAdoptedAsSameIdAmbiguousWork()
    {
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        var gate = new TenantAggregateCommandAdmissionGate();
        object originalOwner = new();
        GlobalAdministratorGrantPreview preview = GlobalAdministratorGrantPreview.Create(
            "  CaseSensitive.Target  ",
            Complete("projection-v1", "existing-admin"),
            isAuthorized: true);
        var reconciliation = new GlobalAdministratorReconciliationState(
            GlobalAdministratorActionKind.Grant,
            preview.TargetUserId,
            messageId,
            CorrelationId: null,
            TenantCommandLifecycleState.RequestSent,
            preview,
            HasCommandEventEvidence: false,
            IsSubmissionAmbiguous: true);

        gate.TryAcquireLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            originalOwner,
            out TenantAggregateCommandLease? lease).ShouldBeTrue();
        lease.ShouldNotBeNull();
        lease.TryMarkDispatched(originalOwner).ShouldBeTrue();
        lease.TryRetainReconciliation(originalOwner, reconciliation).ShouldBeTrue();

        object replacementOwner = new();
        gate.TryAdoptRetainedLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            replacementOwner,
            out TenantAggregateCommandLease? adopted,
            out GlobalAdministratorReconciliationState? retained).ShouldBeTrue();

        adopted.ShouldBeSameAs(lease);
        retained.ShouldNotBeNull();
        retained.MessageId.ShouldBe(messageId);
        retained.TargetUserId.ShouldBe("  CaseSensitive.Target  ");
        retained.CorrelationId.ShouldBeNull();
        retained.IsSubmissionAmbiguous.ShouldBeTrue();
        retained.GrantPreview.ShouldBeSameAs(preview);
        retained.GrantPreview!.ProjectionVersion.ShouldBe("projection-v1");
    }

    [Fact]
    public void RetryCompletionTokenPublishesExactAcceptedEvidenceToReplacementOwner()
    {
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        var gate = new TenantAggregateCommandAdmissionGate();
        object originalOwner = new();
        GlobalAdministratorRemovePreview preview = RemovePreview();
        var delivery = new GlobalAdministratorReconciliationState(
            GlobalAdministratorActionKind.Remove,
            preview.TargetUserId,
            messageId,
            CorrelationId: null,
            TenantCommandLifecycleState.RequestSent,
            IsSubmissionAmbiguous: true,
            SafeMessageKey: "Tenants.GlobalAdministrators.Remove.SubmissionEvidence.Ambiguous",
            SafeRecoveryKey: "Tenants.GlobalAdministrators.Remove.DeliveryRetry.Recovery",
            RemovePreview: preview);
        gate.TryAcquireLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            originalOwner,
            out TenantAggregateCommandLease? lease).ShouldBeTrue();
        lease!.TryMarkDispatched(originalOwner).ShouldBeTrue();
        lease.TryAdvanceReconciliation(originalOwner, delivery).ShouldBeTrue();
        lease.TryBeginReconciliationDispatch(originalOwner, delivery, out long completionToken)
            .ShouldBeTrue();
        lease.IsReconciliationDispatchInFlight.ShouldBeTrue();
        lease.TryRetainReconciliation(originalOwner, delivery).ShouldBeTrue();

        object replacementOwner = new();
        gate.TryAdoptRetainedLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            replacementOwner,
            out TenantAggregateCommandLease? adopted,
            out _).ShouldBeTrue();
        var accepted = delivery with
        {
            CorrelationId = "correlation-retry",
            LifecycleState = TenantCommandLifecycleState.Accepted,
            IsSubmissionAmbiguous = false,
            SafeMessageKey = null,
            SafeRecoveryKey = null,
        };

        lease.TryCompleteReconciliationDispatch(completionToken + 1, accepted).ShouldBeFalse();
        lease.TryCompleteReconciliationDispatch(completionToken, accepted).ShouldBeTrue();
        adopted.ShouldBeSameAs(lease);
        adopted!.TryReadReconciliation(replacementOwner, out GlobalAdministratorReconciliationState? visible)
            .ShouldBeTrue();
        visible.ShouldBe(accepted);
        lease.TryAdvanceReconciliation(originalOwner, delivery).ShouldBeFalse();
        adopted.TryReleaseTerminal(replacementOwner, TenantCommandLifecycleState.Failed).ShouldBeTrue();
    }

    [Fact]
    public void InitialDispatchTokenPersistsIdentityBeforeIoAndPublishesAcceptedEvidenceToReplacement()
    {
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        var gate = new TenantAggregateCommandAdmissionGate();
        object originalOwner = new();
        GlobalAdministratorRemovePreview preview = RemovePreview();
        var requestSent = new GlobalAdministratorReconciliationState(
            GlobalAdministratorActionKind.Remove,
            preview.TargetUserId,
            messageId,
            CorrelationId: null,
            TenantCommandLifecycleState.RequestSent,
            IsSubmissionAmbiguous: false,
            RemovePreview: preview);
        gate.TryAcquireLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            originalOwner,
            out TenantAggregateCommandLease? lease).ShouldBeTrue();

        lease!.TryBeginInitialReconciliationDispatch(originalOwner, requestSent, out long completionToken)
            .ShouldBeTrue();
        lease.IsDispatchMarked.ShouldBeTrue();
        lease.IsReconciliationDispatchInFlight.ShouldBeTrue();
        lease.TryRetainReconciliation(originalOwner, requestSent).ShouldBeTrue();
        object replacementOwner = new();
        gate.TryAdoptRetainedLease(
            TenantCommandAggregateLock.ForGlobalAdministrators(),
            replacementOwner,
            out TenantAggregateCommandLease? adopted,
            out GlobalAdministratorReconciliationState? retained).ShouldBeTrue();
        retained.ShouldBe(requestSent);
        adopted!.TryBeginReconciliationDispatch(replacementOwner, requestSent, out _).ShouldBeFalse();

        GlobalAdministratorReconciliationState accepted = requestSent with
        {
            CorrelationId = "correlation-safe",
            LifecycleState = TenantCommandLifecycleState.Accepted,
        };
        lease.TryCompleteReconciliationDispatch(completionToken, accepted).ShouldBeTrue();
        adopted.TryReadReconciliation(replacementOwner, out GlobalAdministratorReconciliationState? visible)
            .ShouldBeTrue();
        visible.ShouldBe(accepted);
        adopted.IsReconciliationDispatchInFlight.ShouldBeFalse();
        adopted.TryReleaseTerminal(replacementOwner, TenantCommandLifecycleState.Failed).ShouldBeTrue();
    }

    [Fact]
    public void TerminalInitialCompletionBeforeAdoptionReleasesTheFixedAggregate()
    {
        const string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        var gate = new TenantAggregateCommandAdmissionGate();
        object originalOwner = new();
        GlobalAdministratorRemovePreview preview = RemovePreview();
        var requestSent = new GlobalAdministratorReconciliationState(
            GlobalAdministratorActionKind.Remove,
            preview.TargetUserId,
            messageId,
            CorrelationId: null,
            TenantCommandLifecycleState.RequestSent,
            RemovePreview: preview);
        string aggregateKey = TenantCommandAggregateLock.ForGlobalAdministrators();
        gate.TryAcquireLease(aggregateKey, originalOwner, out TenantAggregateCommandLease? lease).ShouldBeTrue();
        lease!.TryBeginInitialReconciliationDispatch(originalOwner, requestSent, out long completionToken)
            .ShouldBeTrue();
        lease.TryRetainReconciliation(originalOwner, requestSent).ShouldBeTrue();

        GlobalAdministratorReconciliationState rejected = requestSent with
        {
            LifecycleState = TenantCommandLifecycleState.Rejected,
            SafeMessageKey = "Tenants.GlobalAdministrators.Remove.Status.Rejected",
            SafeRecoveryKey = "Tenants.GlobalAdministrators.Remove.Recovery.Rejected",
        };
        lease.TryCompleteReconciliationDispatch(completionToken, rejected).ShouldBeTrue();

        gate.IsLocked(aggregateKey).ShouldBeFalse();
        gate.TryAdoptRetainedLease(aggregateKey, new object(), out _, out _).ShouldBeFalse();
        lease.IsReconciliationDispatchInFlight.ShouldBeFalse();
    }

    private static GlobalAdministratorsSnapshot Complete(string projectionVersion, params string[] userIds)
        => GlobalAdministratorsSnapshot.Ready(
            userIds.Select(static userId => new GlobalAdministratorRow(
                userId,
                ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Current)).ToArray(),
            nextCursor: null,
            hasMore: false,
            eTag: $"\"{projectionVersion}\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            ProjectionVersion = projectionVersion,
            IsCompleteEvidence = true,
        };

    private static GlobalAdministratorRemovePreview RemovePreview()
        => GlobalAdministratorRemovePreview.Create(
            "admin-user",
            "operator-user",
            Complete("projection-v1", "admin-user", "other-admin"),
            isAuthorized: true);
}
