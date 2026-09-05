using Hexalith.Tenants.UI.State.GlobalAdministrators;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>
/// Circuit-local admission gate keyed by AggregateIdentity-shaped lock keys so one in-flight membership
/// command locks its aggregate through terminal evidence without serializing unrelated aggregates.
/// </summary>
/// <remarks>
/// Keys are opaque ordinal strings rather than constructed <c>AggregateIdentity</c> values because UI
/// tenant ids are caller-supplied meaningful strings that may include characters EventStore identity
/// validation rejects, while still needing per-aggregate lock scope.
/// </remarks>
public sealed class TenantAggregateCommandAdmissionGate
{
    private readonly object _sync = new();
    private readonly Dictionary<string, object> _ownerByKey = new(StringComparer.Ordinal);
    private long _nextReconciliationDispatchToken;

    /// <summary>Raised after an aggregate admission changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Acquires an ownership-safe lease for one aggregate attempt.</summary>
    /// <param name="aggregateLockKey">AggregateIdentity-shaped lock key.</param>
    /// <param name="owner">Stable command-surface owner.</param>
    /// <param name="lease">Acquired lease, or <see langword="null"/> when admission failed.</param>
    /// <returns><see langword="true"/> when the key was free.</returns>
    public bool TryAcquireLease(
        string aggregateLockKey,
        object owner,
        out TenantAggregateCommandLease? lease)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateLockKey);
        ArgumentNullException.ThrowIfNull(owner);

        var candidate = new TenantAggregateCommandLease(this, aggregateLockKey, owner);
        lock (_sync)
        {
            if (_ownerByKey.ContainsKey(aggregateLockKey))
            {
                lease = null;
                return false;
            }

            _ownerByKey.Add(aggregateLockKey, candidate);
            lease = candidate;
        }

        NotifyStateChanged();
        return true;
    }

    /// <summary>Adopts the retained reconciliation for an aggregate into exactly one replacement surface.</summary>
    /// <param name="aggregateLockKey">AggregateIdentity-shaped lock key.</param>
    /// <param name="owner">Replacement surface owner.</param>
    /// <param name="lease">Exclusively adopted lease.</param>
    /// <param name="reconciliation">Retained reconciliation evidence.</param>
    /// <returns><see langword="true"/> when an adoptable reconciliation was claimed.</returns>
    internal bool TryAdoptRetainedLease(
        string aggregateLockKey,
        object owner,
        out TenantAggregateCommandLease? lease,
        out GlobalAdministratorReconciliationState? reconciliation)
        => TryAdoptRetainedLeaseCore(
            aggregateLockKey,
            owner,
            expectedActionKind: null,
            expectedTargetUserId: null,
            out lease,
            out reconciliation);

    /// <summary>Adopts only retained reconciliation that matches one correction intent.</summary>
    /// <param name="aggregateLockKey">AggregateIdentity-shaped lock key.</param>
    /// <param name="owner">Replacement surface owner.</param>
    /// <param name="expectedActionKind">Expected fixed-scope action.</param>
    /// <param name="expectedTargetUserId">Expected literal target identity.</param>
    /// <param name="lease">Exclusively adopted lease.</param>
    /// <param name="reconciliation">Retained reconciliation evidence.</param>
    /// <returns><see langword="true"/> when matching retained reconciliation was claimed.</returns>
    internal bool TryAdoptRetainedLease(
        string aggregateLockKey,
        object owner,
        GlobalAdministratorActionKind expectedActionKind,
        string expectedTargetUserId,
        out TenantAggregateCommandLease? lease,
        out GlobalAdministratorReconciliationState? reconciliation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedTargetUserId);
        return TryAdoptRetainedLeaseCore(
            aggregateLockKey,
            owner,
            expectedActionKind,
            expectedTargetUserId,
            out lease,
            out reconciliation);
    }

    private bool TryAdoptRetainedLeaseCore(
        string aggregateLockKey,
        object owner,
        GlobalAdministratorActionKind? expectedActionKind,
        string? expectedTargetUserId,
        out TenantAggregateCommandLease? lease,
        out GlobalAdministratorReconciliationState? reconciliation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateLockKey);
        ArgumentNullException.ThrowIfNull(owner);

        bool adopted = false;
        lock (_sync)
        {
            if (_ownerByKey.TryGetValue(aggregateLockKey, out object? current)
                && current is TenantAggregateCommandLease candidate
                && candidate.IsRetainedForAdoption
                && candidate.CurrentOwner is null
                && candidate.Reconciliation is { } retained
                && (expectedActionKind is null
                    || retained.ActionKind == expectedActionKind
                        && string.Equals(retained.TargetUserId, expectedTargetUserId, StringComparison.Ordinal))
                && !candidate.IsReleased)
            {
                candidate.CurrentOwner = owner;
                candidate.IsRetainedForAdoption = false;
                lease = candidate;
                reconciliation = retained;
                adopted = true;
            }
            else
            {
                lease = null;
                reconciliation = null;
            }
        }

        if (adopted)
        {
            NotifyStateChanged();
        }

        return adopted;
    }

    /// <summary>
    /// Acquires the lock for <paramref name="aggregateLockKey"/> on behalf of <paramref name="owner"/>.
    /// Unrelated keys remain independently acquirable on the same circuit.
    /// </summary>
    /// <param name="aggregateLockKey">AggregateIdentity-shaped lock key for the command attempt.</param>
    /// <param name="owner">Stable owner token for the page or command surface holding the lock.</param>
    /// <returns>
    /// <see langword="true"/> when the key was free; otherwise <see langword="false"/>.
    /// </returns>
    public bool TryAcquire(string aggregateLockKey, object owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateLockKey);
        ArgumentNullException.ThrowIfNull(owner);

        bool acquired;
        lock (_sync)
        {
            if (_ownerByKey.ContainsKey(aggregateLockKey))
            {
                return false;
            }

            _ownerByKey.Add(aggregateLockKey, owner);
            acquired = true;
        }

        NotifyStateChanged();
        return acquired;
    }

    /// <summary>
    /// Releases <paramref name="aggregateLockKey"/> only when <paramref name="owner"/> owns it.
    /// </summary>
    /// <param name="aggregateLockKey">AggregateIdentity-shaped lock key that previously acquired the lock.</param>
    /// <param name="owner">Stable owner token supplied during acquisition.</param>
    public void Release(string aggregateLockKey, object owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateLockKey);
        ArgumentNullException.ThrowIfNull(owner);

        bool released = false;
        lock (_sync)
        {
            if (_ownerByKey.TryGetValue(aggregateLockKey, out object? currentOwner)
                && ReferenceEquals(currentOwner, owner))
            {
                _ = _ownerByKey.Remove(aggregateLockKey);
                released = true;
            }
        }

        if (released)
        {
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Returns whether <paramref name="aggregateLockKey"/> is currently locked by an in-flight command.
    /// </summary>
    /// <param name="aggregateLockKey">AggregateIdentity-shaped lock key to inspect.</param>
    /// <returns><see langword="true"/> when the key holds a circuit lock.</returns>
    public bool IsLocked(string aggregateLockKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateLockKey);

        lock (_sync)
        {
            return _ownerByKey.ContainsKey(aggregateLockKey);
        }
    }

    /// <summary>
    /// Returns whether <paramref name="aggregateLockKey"/> is owned by another command surface.
    /// </summary>
    /// <param name="aggregateLockKey">AggregateIdentity-shaped lock key to inspect.</param>
    /// <param name="owner">Owner token of the current page or command surface.</param>
    /// <returns><see langword="true"/> when another owner holds the key.</returns>
    public bool IsLockedByAnother(string aggregateLockKey, object owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateLockKey);
        ArgumentNullException.ThrowIfNull(owner);

        lock (_sync)
        {
            if (!_ownerByKey.TryGetValue(aggregateLockKey, out object? currentOwner))
            {
                return false;
            }

            return currentOwner is TenantAggregateCommandLease lease
                ? lease.IsRetainedForAdoption || !ReferenceEquals(lease.CurrentOwner, owner)
                : !ReferenceEquals(currentOwner, owner);
        }
    }

    /// <summary>
    /// Returns whether an aggregate lock is retained by the supplied stable owner.
    /// </summary>
    /// <param name="aggregateLockKey">AggregateIdentity-shaped lock key to inspect.</param>
    /// <param name="owner">Stable owner token expected to hold the lock.</param>
    /// <returns><see langword="true"/> when the owner still retains the lock.</returns>
    internal bool IsOwnedBy(string aggregateLockKey, object owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateLockKey);
        ArgumentNullException.ThrowIfNull(owner);

        lock (_sync)
        {
            return _ownerByKey.TryGetValue(aggregateLockKey, out object? currentOwner)
                && ReferenceEquals(currentOwner, owner);
        }
    }

    internal bool IsDispatchMarked(TenantAggregateCommandLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        lock (_sync)
        {
            return IsActiveLeaseCore(lease) && lease.DispatchMarked;
        }
    }

    internal bool IsReconciliationDispatchInFlight(TenantAggregateCommandLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        lock (_sync)
        {
            return IsActiveLeaseCore(lease) && lease.ActiveReconciliationDispatchToken != 0;
        }
    }

    internal bool TryBeginReconciliationDispatch(
        TenantAggregateCommandLease lease,
        object owner,
        GlobalAdministratorReconciliationState expected,
        out long completionToken)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(expected);

        lock (_sync)
        {
            if (!IsOwnedActiveLeaseCore(lease, owner)
                || !lease.DispatchMarked
                || lease.ActiveReconciliationDispatchToken != 0
                || !IsValidReconciliation(expected)
                || IsTerminal(expected.LifecycleState)
                || lease.Reconciliation is { } current
                    && !HasSameReconciliation(current, expected))
            {
                completionToken = 0;
                return false;
            }

            // Persist the exact delivery basis before any gateway call. This also repairs an older
            // correlationless RequestSent lease whose first renderer update raced retention.
            lease.Reconciliation = expected;

            completionToken = ++_nextReconciliationDispatchToken;
            if (completionToken == 0)
            {
                completionToken = ++_nextReconciliationDispatchToken;
            }

            lease.ActiveReconciliationDispatchToken = completionToken;
            return true;
        }
    }

    internal bool TryCompleteReconciliationDispatch(
        TenantAggregateCommandLease lease,
        long completionToken,
        GlobalAdministratorReconciliationState completion)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(completion);

        bool completed = false;
        lock (_sync)
        {
            if (completionToken == 0
                || !IsActiveLeaseCore(lease)
                || lease.ActiveReconciliationDispatchToken != completionToken
                || lease.Reconciliation is not { } current
                || !IsValidCompletion(completion)
                || !HasSameCommandIdentity(current, completion)
                || IsLifecycleRegression(current.LifecycleState, completion.LifecycleState))
            {
                return false;
            }

            lease.ActiveReconciliationDispatchToken = 0;
            lease.Reconciliation = completion;
            completed = true;
        }

        if (completed)
        {
            NotifyStateChanged();
        }

        return completed;
    }

    internal bool TryReadReconciliation(
        TenantAggregateCommandLease lease,
        object owner,
        out GlobalAdministratorReconciliationState? reconciliation)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(owner);

        lock (_sync)
        {
            if (IsOwnedActiveLeaseCore(lease, owner) && lease.Reconciliation is { } current)
            {
                reconciliation = current;
                return true;
            }

            reconciliation = null;
            return false;
        }
    }

    internal bool TryMarkDispatched(TenantAggregateCommandLease lease, object owner)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(owner);
        lock (_sync)
        {
            if (!IsOwnedActiveLeaseCore(lease, owner) || lease.DispatchMarked)
            {
                return false;
            }

            lease.DispatchMarked = true;
            return true;
        }
    }

    internal bool TryAdvanceReconciliation(
        TenantAggregateCommandLease lease,
        object owner,
        GlobalAdministratorReconciliationState reconciliation)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(reconciliation);

        lock (_sync)
        {
            if (!IsOwnedActiveLeaseCore(lease, owner)
                || !lease.DispatchMarked
                || !IsValidReconciliation(reconciliation)
                || IsTerminal(reconciliation.LifecycleState)
                || lease.Reconciliation is { } current
                    && (!HasSameCommandIdentity(current, reconciliation)
                        || IsLifecycleRegression(current.LifecycleState, reconciliation.LifecycleState)))
            {
                return false;
            }

            lease.Reconciliation = reconciliation;
            return true;
        }
    }

    internal bool TryRetainReconciliation(
        TenantAggregateCommandLease lease,
        object owner,
        GlobalAdministratorReconciliationState reconciliation)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(reconciliation);

        bool retained = false;
        lock (_sync)
        {
            if (!IsOwnedActiveLeaseCore(lease, owner)
                || !lease.DispatchMarked
                || !IsValidReconciliation(reconciliation)
                || IsTerminal(reconciliation.LifecycleState)
                || lease.Reconciliation is { } current
                    && (!HasSameCommandIdentity(current, reconciliation)
                        || IsLifecycleRegression(current.LifecycleState, reconciliation.LifecycleState)))
            {
                return false;
            }

            lease.Reconciliation = reconciliation;
            lease.CurrentOwner = null;
            lease.IsRetainedForAdoption = true;
            retained = true;
        }

        NotifyStateChanged();
        return retained;
    }

    internal bool TryAbandonBeforeDispatch(TenantAggregateCommandLease lease, object owner)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(owner);
        bool released;
        lock (_sync)
        {
            released = IsOwnedActiveLeaseCore(lease, owner)
                && !lease.DispatchMarked
                && _ownerByKey.Remove(lease.AggregateLockKey);
            if (released)
            {
                lease.IsReleased = true;
                lease.CurrentOwner = null;
                lease.ActiveReconciliationDispatchToken = 0;
            }
        }

        if (released)
        {
            NotifyStateChanged();
        }

        return released;
    }

    internal bool TryReleaseTerminal(
        TenantAggregateCommandLease lease,
        object owner,
        TenantCommandLifecycleState terminalState)
    {
        ArgumentNullException.ThrowIfNull(lease);
        ArgumentNullException.ThrowIfNull(owner);
        bool released;
        lock (_sync)
        {
            released = IsOwnedActiveLeaseCore(lease, owner)
                && lease.DispatchMarked
                && IsTerminal(terminalState)
                && _ownerByKey.Remove(lease.AggregateLockKey);
            if (released)
            {
                lease.IsReleased = true;
                lease.IsRetainedForAdoption = false;
                lease.CurrentOwner = null;
                lease.Reconciliation = null;
                lease.ActiveReconciliationDispatchToken = 0;
            }
        }

        if (released)
        {
            NotifyStateChanged();
        }

        return released;
    }

    /// <summary>
    /// Returns whether any aggregate is locked on this circuit.
    /// </summary>
    public bool HasActiveLock
    {
        get
        {
            lock (_sync)
            {
                return _ownerByKey.Count > 0;
            }
        }
    }

    private void NotifyStateChanged()
    {
        EventHandler? handlers = StateChanged;
        if (handlers is null)
        {
            return;
        }

        foreach (EventHandler handler in handlers.GetInvocationList().Cast<EventHandler>())
        {
            try
            {
                handler(this, EventArgs.Empty);
            }
            catch
            {
                // Admission ownership must not depend on the health of a UI observer.
            }
        }
    }

    private bool IsActiveLeaseCore(TenantAggregateCommandLease lease)
        => !lease.IsReleased
            && _ownerByKey.TryGetValue(lease.AggregateLockKey, out object? current)
            && ReferenceEquals(current, lease);

    private bool IsOwnedActiveLeaseCore(TenantAggregateCommandLease lease, object owner)
        => IsActiveLeaseCore(lease)
            && !lease.IsRetainedForAdoption
            && ReferenceEquals(lease.CurrentOwner, owner);

    private static bool IsValidReconciliation(
        GlobalAdministratorReconciliationState reconciliation)
    {
        if (string.IsNullOrWhiteSpace(reconciliation.TargetUserId)
            || string.IsNullOrWhiteSpace(reconciliation.MessageId))
        {
            return false;
        }

        return reconciliation.ActionKind switch
        {
            GlobalAdministratorActionKind.Grant
                => reconciliation.GrantPreview?.IsComplete == true
                    && (!string.IsNullOrWhiteSpace(reconciliation.CorrelationId)
                        || reconciliation.LifecycleState is TenantCommandLifecycleState.RequestSent
                            && reconciliation.IsSubmissionAmbiguous),
            GlobalAdministratorActionKind.Remove
                => reconciliation.RemovePreview?.IsComplete == true
                    && (!string.IsNullOrWhiteSpace(reconciliation.CorrelationId)
                        || reconciliation.LifecycleState is TenantCommandLifecycleState.RequestSent
                            or TenantCommandLifecycleState.UnableToVerify
                            && reconciliation.IsSubmissionAmbiguous),
            _ => false,
        };
    }

    private static bool IsValidCompletion(GlobalAdministratorReconciliationState reconciliation)
        => !string.IsNullOrWhiteSpace(reconciliation.TargetUserId)
            && !string.IsNullOrWhiteSpace(reconciliation.MessageId)
            && (reconciliation.ActionKind switch
            {
                GlobalAdministratorActionKind.Grant => reconciliation.GrantPreview?.IsComplete == true,
                GlobalAdministratorActionKind.Remove => reconciliation.RemovePreview?.IsComplete == true,
                _ => false,
            });

    private static bool HasSameReconciliation(
        GlobalAdministratorReconciliationState current,
        GlobalAdministratorReconciliationState expected)
        => HasSameCommandIdentity(current, expected)
            && current.LifecycleState == expected.LifecycleState
            && current.HasCommandEventEvidence == expected.HasCommandEventEvidence
            && current.IsSubmissionAmbiguous == expected.IsSubmissionAmbiguous;

    private static bool HasSameCommandIdentity(
        GlobalAdministratorReconciliationState current,
        GlobalAdministratorReconciliationState next)
        => current.ActionKind == next.ActionKind
            && string.Equals(current.TargetUserId, next.TargetUserId, StringComparison.Ordinal)
            && string.Equals(current.MessageId, next.MessageId, StringComparison.Ordinal)
            && (current.CorrelationId is null
                || string.Equals(current.CorrelationId, next.CorrelationId, StringComparison.Ordinal))
            && Equals(current.GrantPreview, next.GrantPreview)
            && Equals(current.RemovePreview, next.RemovePreview);

    private static bool IsLifecycleRegression(
        TenantCommandLifecycleState current,
        TenantCommandLifecycleState next)
        => next is TenantCommandLifecycleState.RequestSent
            && current is not TenantCommandLifecycleState.RequestSent
            || next is TenantCommandLifecycleState.Accepted
                && current is TenantCommandLifecycleState.ProjectionPending;

    private static bool IsTerminal(TenantCommandLifecycleState state)
        => state is TenantCommandLifecycleState.Confirmed
            or TenantCommandLifecycleState.Failed
            or TenantCommandLifecycleState.Rejected
            or TenantCommandLifecycleState.AlreadyApplied;
}
