using Hexalith.Tenants.UI.State.TenantAudit;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Retains one correction attempt per tenant for the lifetime of an interactive circuit.</summary>
public sealed class TenantCorrectionAttemptTracker : IDisposable
{
    private readonly object _sync = new();
    private readonly Dictionary<string, TenantCorrectionAttempt> _attempts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _deliveriesInFlight = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Timer> _expiryTimers = new(StringComparer.Ordinal);
    private readonly object _leaseOwner = new();
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly TimeSpan _expiryCheckInterval;

    /// <summary>Raised when a retained attempt reaches its delivery deadline.</summary>
    internal event EventHandler? StateChanged;

    /// <summary>Creates a circuit-local tracker using UTC time.</summary>
    public TenantCorrectionAttemptTracker() : this(static () => DateTimeOffset.UtcNow) { }

    internal TenantCorrectionAttemptTracker(Func<DateTimeOffset> utcNow, TimeSpan? expiryCheckInterval = null)
    {
        ArgumentNullException.ThrowIfNull(utcNow);
        _utcNow = utcNow;
        _expiryCheckInterval = expiryCheckInterval ?? TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration;
        if (_expiryCheckInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(expiryCheckInterval));
    }

    /// <summary>Returns the retained attempt, including uncertain delivery, after a panel is remounted.</summary>
    internal TenantCorrectionAttempt? Find(string tenantId)
    {
        lock (_sync)
        {
            PruneExpiredLocked();
            return _attempts.GetValueOrDefault(tenantId);
        }
    }

    /// <summary>Reports whether delivery has expired; a correlated attempt can still check status.</summary>
    internal bool IsExpired(string tenantId)
    {
        lock (_sync)
        {
            PruneExpiredLocked();
            return _attempts.GetValueOrDefault(tenantId)?.IsExpired is true;
        }
    }

    /// <summary>Admits one exact attempt before gateway I/O and retains its ULID and baseline.</summary>
    internal bool TryBegin(
        TenantCorrectionPreviewSnapshot preview,
        TenantAggregateCommandAdmissionGate gate,
        out TenantCorrectionAttempt? attempt)
    {
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(gate);
        lock (_sync)
        {
            PruneExpiredLocked();
            _attempts.TryGetValue(preview.TenantId, out TenantCorrectionAttempt? previous);
            if (previous is not null)
            {
                if (previous.BlocksAdmission)
                {
                    attempt = previous;
                    return false;
                }
            }

            if (!gate.TryAcquireLease(TenantCommandAggregateLock.ForTenant(preview.TenantId), _leaseOwner, out TenantAggregateCommandLease? lease)
                || lease is null)
            {
                attempt = previous;
                return false;
            }

            string messageId = NUlid.Ulid.NewUlid().ToString();
            DateTimeOffset startedAtUtc = _utcNow().ToUniversalTime();
            TenantCorrectionPreviewSnapshot requestSent = preview.RequestSent() with {
                MessageId = messageId, AttemptStartedAtUtc = startedAtUtc };
            attempt = new(preview.TenantId, messageId, requestSent, lease, startedAtUtc);
            _attempts[preview.TenantId] = attempt;
            _deliveriesInFlight.Add(preview.TenantId);
            if (!lease.TryMarkDispatched(_leaseOwner))
            {
                if (previous is null) _attempts.Remove(preview.TenantId);
                else _attempts[preview.TenantId] = previous;
                _deliveriesInFlight.Remove(preview.TenantId);
                lease.TryAbandonBeforeDispatch(_leaseOwner);
                attempt = null;
                return false;
            }

            if (_expiryTimers.Remove(preview.TenantId, out Timer? previousTimer)) previousTimer.Dispose();
            _expiryTimers[preview.TenantId] = new Timer(
                _ => OnExpiryTimer(preview.TenantId), null,
                _expiryCheckInterval,
                Timeout.InfiniteTimeSpan);

            return true;
        }
    }

    /// <summary>Allows one same-ID retry only after the earlier delivery operation has finished.</summary>
    internal bool TryStartRetry(string tenantId, string messageId)
    {
        lock (_sync)
        {
            PruneExpiredLocked();
            if (_deliveriesInFlight.Contains(tenantId)
                || !_attempts.TryGetValue(tenantId, out TenantCorrectionAttempt? attempt)
                || attempt.IsExpired
                || !string.Equals(attempt.MessageId, messageId, StringComparison.Ordinal)
                || !string.IsNullOrWhiteSpace(attempt.Snapshot.CorrelationId)
                || IsTerminal(attempt.Snapshot.LifecycleState))
            {
                return false;
            }

            return _deliveriesInFlight.Add(tenantId);
        }
    }

    /// <summary>When supplied, retains the delivery outcome before allowing another same-id delivery.</summary>
    internal void EndDelivery(string tenantId, string messageId, TenantCorrectionPreviewSnapshot? outcome = null)
    {
        lock (_sync)
        {
            if (_attempts.TryGetValue(tenantId, out TenantCorrectionAttempt? attempt)
                && string.Equals(attempt.MessageId, messageId, StringComparison.Ordinal))
            {
                if (outcome is not null) TryUpdate(tenantId, messageId, outcome);
                _deliveriesInFlight.Remove(tenantId);
            }
        }
    }

    /// <summary>Advances only the exact retained attempt; late completions cannot replace another attempt.</summary>
    internal bool TryUpdate(string tenantId, string messageId, TenantCorrectionPreviewSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_sync)
        {
            if (!_attempts.TryGetValue(tenantId, out TenantCorrectionAttempt? current)
                || !string.Equals(current.MessageId, messageId, StringComparison.Ordinal))
            {
                return false;
            }

            if (IsTerminal(current.Snapshot.LifecycleState)
                && current.Snapshot.LifecycleState != snapshot.LifecycleState
                || current.Snapshot.CorrelationId is not null && snapshot.CorrelationId is null
                || current.Snapshot.LifecycleState is TenantCommandLifecycleState.ProjectionPending
                    && snapshot.LifecycleState is TenantCommandLifecycleState.RequestSent
                        or TenantCommandLifecycleState.Accepted)
            {
                return false;
            }

            if (current.IsExpired && !IsTerminal(snapshot.LifecycleState))
            {
                snapshot = snapshot with {
                    LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = null,
                    SafeMessageKey = snapshot.CorrelationId is null
                        ? "Tenants.Correction.Unavailable.AttemptExpiredWithoutCorrelation"
                        : "Tenants.Correction.Unavailable.AttemptExpired",
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                };
            }

            _attempts[tenantId] = current with { Snapshot = snapshot with {
                MessageId = messageId,
                CommittedEventSequence = snapshot.CommittedEventSequence ?? current.Snapshot.CommittedEventSequence,
            } };
            if (snapshot.LifecycleState is TenantCommandLifecycleState.Confirmed
                or TenantCommandLifecycleState.Rejected
                or TenantCommandLifecycleState.AlreadyApplied
                or TenantCommandLifecycleState.Failed)
            {
                current.Lease.TryReleaseTerminal(_leaseOwner, snapshot.LifecycleState);
                if (_expiryTimers.Remove(tenantId, out Timer? timer)) timer.Dispose();
            }

            return true;
        }
    }

    private static bool IsTerminal(TenantCommandLifecycleState state)
        => state is TenantCommandLifecycleState.Confirmed
            or TenantCommandLifecycleState.Rejected
            or TenantCommandLifecycleState.AlreadyApplied
            or TenantCommandLifecycleState.Failed;

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync)
        {
            foreach (Timer timer in _expiryTimers.Values) timer.Dispose();
            _expiryTimers.Clear();
        }
    }

    private void OnExpiryTimer(string tenantId)
    {
        bool changed;
        lock (_sync)
        {
            changed = PruneExpiredLocked();
            if (_attempts.TryGetValue(tenantId, out TenantCorrectionAttempt? attempt)
                && attempt.BlocksAdmission
                && _expiryTimers.TryGetValue(tenantId, out Timer? timer))
            {
                DateTimeOffset now = _utcNow().ToUniversalTime();
                TimeSpan remaining = attempt.StartedAtUtc + TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration - now;
                timer.Change(remaining > _expiryCheckInterval ? _expiryCheckInterval : remaining > TimeSpan.Zero
                    ? remaining : TimeSpan.FromMilliseconds(1),
                    Timeout.InfiniteTimeSpan);
            }
        }
        if (changed) StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool PruneExpiredLocked()
    {
        DateTimeOffset now = _utcNow().ToUniversalTime();
        bool changed = false;
        foreach ((string tenantId, TenantCorrectionAttempt attempt) in _attempts.ToArray())
        {
            if (!attempt.BlocksAdmission
                || now < attempt.StartedAtUtc
                || now - attempt.StartedAtUtc < TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration)
            {
                continue;
            }

            // Release bounded circuit admission, but retain the original identity and snapshot.
            // A correlated attempt can inspect status until a fresh current-state preview replaces it.
            // Without correlation, the retained identity remains for escalation but cannot be looked up.
            attempt.Lease.TryReleaseTerminal(_leaseOwner, TenantCommandLifecycleState.Failed);
            _attempts[tenantId] = attempt with { IsExpired = true, Snapshot = attempt.Snapshot with {
                LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = null,
                SafeMessageKey = attempt.Snapshot.CorrelationId is null
                    ? "Tenants.Correction.Unavailable.AttemptExpiredWithoutCorrelation"
                    : "Tenants.Correction.Unavailable.AttemptExpired",
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            } };
            _deliveriesInFlight.Remove(tenantId);
            if (_expiryTimers.Remove(tenantId, out Timer? timer)) timer.Dispose();
            changed = true;
        }
        return changed;
    }
}
