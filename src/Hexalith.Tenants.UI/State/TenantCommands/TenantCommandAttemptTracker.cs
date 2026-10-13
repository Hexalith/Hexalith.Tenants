namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Retains complete tenant command attempts and their exact aggregate admission for a circuit.</summary>
/// <param name="admissionGate">The circuit-scoped aggregate admission gate.</param>
public sealed class TenantCommandAttemptTracker(TenantAggregateCommandAdmissionGate admissionGate)
{
    /// <summary>Raised when exact retained reconciliation evidence advances.</summary>
    public event EventHandler? StateChanged;

    private readonly object _sync = new();
    private readonly Dictionary<string, TenantRetainedCommandAttempt> _attempts = new(StringComparer.Ordinal);

    /// <summary>Returns whether the aggregate retains unresolved command ownership.</summary>
    /// <param name="tenantId">Literal tenant identifier.</param>
    /// <returns>Whether a retained attempt remains unresolved.</returns>
    public bool HasPendingOwnership(string tenantId)
    {
        lock (_sync)
        {
            return _attempts.TryGetValue(tenantId, out TenantRetainedCommandAttempt? attempt)
                && !IsTerminal(attempt.Snapshot);
        }
    }

    /// <summary>Reads complete retained evidence without acquiring rendering ownership.</summary>
    internal TSnapshot? Find<TSnapshot>(string tenantId) where TSnapshot : class
    {
        lock (_sync)
        {
            return _attempts.TryGetValue(tenantId, out TenantRetainedCommandAttempt? attempt)
                ? attempt.Snapshot as TSnapshot : null;
        }
    }

    /// <summary>Admits one new attempt with a stable lease owner before gateway I/O.</summary>
    internal bool TryBegin(string tenantId, object surfaceOwner, object snapshot, out TenantRetainedCommandAttempt? attempt)
    {
        lock (_sync)
        {
            if (_attempts.TryGetValue(tenantId, out TenantRetainedCommandAttempt? existing) && !IsTerminal(existing.Snapshot))
            {
                attempt = null;
                return false;
            }

            object leaseOwner = new();
            if (!admissionGate.TryAcquireLease(TenantCommandAggregateLock.ForTenant(tenantId), leaseOwner, out TenantAggregateCommandLease? lease))
            {
                attempt = null;
                return false;
            }

            attempt = new(tenantId, leaseOwner, lease!, surfaceOwner, snapshot);
            _attempts[tenantId] = attempt;
            return true;
        }
    }

    /// <summary>Attaches exactly one replacement renderer to the original typed attempt.</summary>
    internal bool TryAdopt<TSnapshot>(string tenantId, object surfaceOwner, out TenantRetainedCommandAttempt? attempt, out TSnapshot? snapshot)
        where TSnapshot : class
    {
        lock (_sync)
        {
            if (_attempts.TryGetValue(tenantId, out TenantRetainedCommandAttempt? retained)
                && retained.Snapshot is TSnapshot typed
                && (retained.SurfaceOwner is null || ReferenceEquals(retained.SurfaceOwner, surfaceOwner)))
            {
                retained.SurfaceOwner = surfaceOwner;
                attempt = retained;
                snapshot = typed;
                return true;
            }

            attempt = null;
            snapshot = null;
            return false;
        }
    }

    /// <summary>Atomically reads evidence only while this exact renderer still owns the exact attempt.</summary>
    internal bool TryRead<TSnapshot>(TenantRetainedCommandAttempt attempt, object surfaceOwner, out TSnapshot? snapshot)
        where TSnapshot : class
    {
        lock (_sync)
        {
            if (IsCurrent(attempt) && ReferenceEquals(attempt.SurfaceOwner, surfaceOwner)
                && attempt.Snapshot is TSnapshot typed)
            {
                snapshot = typed;
                return true;
            }
            snapshot = null;
            return false;
        }
    }

    /// <summary>Publishes evidence only against the exact retained snapshot observed before an await.</summary>
    internal bool TryUpdate(TenantRetainedCommandAttempt attempt, object expected, object snapshot)
    {
        lock (_sync)
        {
            if (!TryUpdateCore(attempt, expected, snapshot))
            {
                return false;
            }
        }
        PublishAdvance(attempt, expected, snapshot);
        return true;
    }

    private bool TryUpdateCore(TenantRetainedCommandAttempt attempt, object expected, object snapshot)
    {
        if (!IsCurrent(attempt) || !HasSameEvidence(attempt.Snapshot, expected)
            || IsTerminal(expected) && StateOf(expected) != StateOf(snapshot)
                && !(expected is TenantRemoveMemberCommandSnapshot { State: TenantCommandLifecycleState.Rejected }
                    && StateOf(snapshot) is TenantCommandLifecycleState.AlreadyApplied))
        {
            return false;
        }
        attempt.Snapshot = snapshot;
        return true;
    }

    private void PublishAdvance(TenantRetainedCommandAttempt attempt, object expected, object snapshot)
    {
        if (IsTerminal(snapshot))
        {
            _ = attempt.Lease.TryReleaseTerminal(attempt.LeaseOwner, StateOf(snapshot));
        }
        if (!ReferenceEquals(expected, snapshot) && StateChanged is { } observers)
        {
            foreach (EventHandler observer in observers.GetInvocationList().Cast<EventHandler>())
            {
                try
                {
                    observer(this, EventArgs.Empty);
                }
                catch (Exception)
                {
                    // An observer cannot undo durable command evidence or its exact lease release.
                }
            }
        }
    }

    /// <summary>Arms one original or same-identity recovery delivery while its renderer owns the attempt.</summary>
    internal bool TryStartDelivery(TenantRetainedCommandAttempt attempt, object surfaceOwner)
    {
        lock (_sync)
        {
            if (!IsCurrent(attempt) || !ReferenceEquals(attempt.SurfaceOwner, surfaceOwner)
                || attempt.DeliveryInFlight || IsTerminal(attempt.Snapshot))
            {
                return false;
            }

            if (!attempt.Lease.IsDispatchMarked && !attempt.Lease.TryMarkDispatched(attempt.LeaseOwner))
            {
                return false;
            }

            attempt.DeliveryInFlight = true;
            return true;
        }
    }

    /// <summary>Completes the exact delivery even when its original renderer has been disposed.</summary>
    internal bool TryCompleteDelivery(TenantRetainedCommandAttempt attempt, object expected, object snapshot)
    {
        object current;
        lock (_sync)
        {
            if (!IsCurrent(attempt) || !attempt.DeliveryInFlight)
            {
                return false;
            }
            attempt.DeliveryInFlight = false;
            current = attempt.Snapshot;
            if (expected.GetType() != current.GetType() || IsTerminal(current)
                || !TryUpdateCore(attempt, current, snapshot))
            {
                return false;
            }
        }
        PublishAdvance(attempt, current, snapshot);
        return true;
    }

    /// <summary>Detaches only the matching renderer, retaining dispatched evidence without expiry.</summary>
    internal void Detach(TenantRetainedCommandAttempt? attempt, object surfaceOwner)
    {
        lock (_sync)
        {
            if (attempt is null || !IsCurrent(attempt) || !ReferenceEquals(attempt.SurfaceOwner, surfaceOwner))
            {
                return;
            }

            attempt.SurfaceOwner = null;
            if (!attempt.Lease.IsDispatchMarked && attempt.Lease.TryAbandonBeforeDispatch(attempt.LeaseOwner))
            {
                _ = _attempts.Remove(attempt.TenantId);
            }
        }
    }

    /// <summary>Returns whether this exact renderer remains attached to the retained attempt.</summary>
    internal bool IsAttached(TenantRetainedCommandAttempt attempt, object surfaceOwner)
    {
        lock (_sync)
        {
            return IsCurrent(attempt) && ReferenceEquals(attempt.SurfaceOwner, surfaceOwner);
        }
    }

    /// <summary>Compares exact reconciliation evidence while allowing a SignalR focus-only nudge.</summary>
    internal static bool HasSameEvidence(object expected, object current)
        => ReferenceEquals(expected, current) || (expected, current) switch
        {
            (TenantCreateCommandSnapshot a, TenantCreateCommandSnapshot b) => a with { FocusTarget = TenantCommandFocusTarget.Refresh } == b with { FocusTarget = TenantCommandFocusTarget.Refresh },
            (TenantAddMemberCommandSnapshot a, TenantAddMemberCommandSnapshot b) => a with { FocusTarget = TenantCommandFocusTarget.Refresh } == b with { FocusTarget = TenantCommandFocusTarget.Refresh },
            (TenantChangeRoleCommandSnapshot a, TenantChangeRoleCommandSnapshot b) => a with { FocusTarget = TenantCommandFocusTarget.Refresh } == b with { FocusTarget = TenantCommandFocusTarget.Refresh },
            (TenantRemoveMemberCommandSnapshot a, TenantRemoveMemberCommandSnapshot b) => a with { FocusTarget = TenantCommandFocusTarget.Refresh } == b with { FocusTarget = TenantCommandFocusTarget.Refresh },
            (TenantUpdateMetadataCommandSnapshot a, TenantUpdateMetadataCommandSnapshot b) => a with { FocusTarget = TenantCommandFocusTarget.Refresh } == b with { FocusTarget = TenantCommandFocusTarget.Refresh },
            _ => false,
        };

    private bool IsCurrent(TenantRetainedCommandAttempt attempt)
        => _attempts.TryGetValue(attempt.TenantId, out TenantRetainedCommandAttempt? current) && ReferenceEquals(current, attempt);

    /// <summary>Returns whether the typed domain attempt has supported terminal evidence.</summary>
    internal static bool IsTerminal(object snapshot)
        => StateOf(snapshot) is TenantCommandLifecycleState.Confirmed or TenantCommandLifecycleState.Rejected
            or TenantCommandLifecycleState.AlreadyApplied or TenantCommandLifecycleState.Failed;

    private static TenantCommandLifecycleState StateOf(object snapshot)
        => snapshot switch
        {
            TenantCreateCommandSnapshot create => create.State,
            TenantAddMemberCommandSnapshot add => add.State,
            TenantChangeRoleCommandSnapshot change => change.State,
            TenantRemoveMemberCommandSnapshot remove => remove.State,
            TenantUpdateMetadataCommandSnapshot metadata => metadata.State,
            _ => throw new ArgumentException("Unsupported tenant command attempt.", nameof(snapshot)),
        };
}
