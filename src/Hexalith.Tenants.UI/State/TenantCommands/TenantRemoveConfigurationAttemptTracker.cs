using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Retains one support-safe remove-configuration attempt per tenant and interactive circuit.</summary>
public sealed class TenantRemoveConfigurationAttemptTracker : IDisposable
{
    private readonly object _sync = new();
    private readonly Func<string> _newMessageId;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Dictionary<string, TenantRemoveConfigurationCommandSnapshot> _snapshots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (TenantRemoveConfigurationIntent Intent, string BaselineProjectionVersion, DateTimeOffset AttemptStartedAtUtc, string MessageId)> _dispatches = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CancellationTokenSource> _expiryByTenant = new(StringComparer.Ordinal);
    private bool _disposed;

    /// <summary>Raised after bounded ownership is autonomously removed.</summary>
    internal event Action<string>? OwnershipExpired;

    /// <summary>Initializes a tracker using the system UTC clock.</summary>
    public TenantRemoveConfigurationAttemptTracker()
        : this(static () => DateTimeOffset.UtcNow, static () => NUlid.Ulid.NewUlid().ToString())
    {
    }

    internal TenantRemoveConfigurationAttemptTracker(Func<DateTimeOffset> utcNow)
        : this(utcNow, static () => NUlid.Ulid.NewUlid().ToString())
    {
    }

    internal TenantRemoveConfigurationAttemptTracker(Func<DateTimeOffset> utcNow, Func<string> newMessageId)
    {
        ArgumentNullException.ThrowIfNull(utcNow);
        ArgumentNullException.ThrowIfNull(newMessageId);
        _utcNow = utcNow;
        _newMessageId = newMessageId;
    }

    /// <summary>Gets the stable admission-gate owner for retained remove attempts.</summary>
    internal object LeaseOwner { get; } = new();

    /// <summary>Starts or resumes the dispatch identity for one exact safe intent.</summary>
    internal (string MessageId, DateTimeOffset AttemptStartedAtUtc) BeginDispatch(
        TenantRemoveConfigurationIntent intent,
        string baselineProjectionVersion,
        DateTimeOffset attemptStartedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentException.ThrowIfNullOrWhiteSpace(intent.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(intent.FullKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(baselineProjectionVersion);

        List<string> expired = [];
        try
        {
            lock (_sync)
            {
                PruneExpiredLocked(_utcNow().ToUniversalTime(), expired);
                if (_dispatches.TryGetValue(intent.TenantId, out var retained))
                {
                    if (!Equals(retained.Intent, intent)
                        || !string.Equals(retained.BaselineProjectionVersion, baselineProjectionVersion, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "A different remove-configuration intent already owns the tenant dispatch window.");
                    }

                    return (retained.MessageId, retained.AttemptStartedAtUtc);
                }

                DateTimeOffset normalized = attemptStartedAtUtc.ToUniversalTime();
                string messageId = _newMessageId();
                if (!NUlid.Ulid.TryParse(messageId, out _))
                {
                    throw new InvalidOperationException("The remove-configuration message id factory returned an invalid ULID.");
                }

                _dispatches[intent.TenantId] = (intent, baselineProjectionVersion, normalized, messageId);
                ScheduleExpiryLocked(intent.TenantId, normalized);
                return (messageId, normalized);
            }
        }
        finally
        {
            RaiseOwnershipExpired(expired);
        }
    }

    /// <summary>Returns the retained unresolved attempt for a literal tenant id.</summary>
    public TenantRemoveConfigurationCommandSnapshot? Find(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        List<string> expired = [];
        try
        {
            lock (_sync)
            {
                PruneExpiredLocked(_utcNow().ToUniversalTime(), expired);
                return _snapshots.TryGetValue(tenantId, out TenantRemoveConfigurationCommandSnapshot? snapshot)
                    ? snapshot
                    : null;
            }
        }
        finally
        {
            RaiseOwnershipExpired(expired);
        }
    }

    /// <summary>Monotonically retains the latest evidence for one logical attempt.</summary>
    public bool Remember(TenantRemoveConfigurationCommandSnapshot snapshot)
    {
        if (snapshot is null
            || !snapshot.RetainsAttempt
            || snapshot.Intent is not { } intent
            || string.IsNullOrWhiteSpace(snapshot.MessageId)
            || snapshot.AttemptStartedAtUtc is null)
        {
            return false;
        }

        List<string> expired = [];
        try
        {
            lock (_sync)
            {
                DateTimeOffset now = _utcNow().ToUniversalTime();
                PruneExpiredLocked(now, expired);
                if (snapshot.IsRetentionExpired(now))
                {
                    _ = _snapshots.Remove(intent.TenantId);
                    _ = _dispatches.Remove(intent.TenantId);
                    return false;
                }

                if (_snapshots.TryGetValue(intent.TenantId, out TenantRemoveConfigurationCommandSnapshot? retained))
                {
                    if (!string.Equals(retained.MessageId, snapshot.MessageId, StringComparison.Ordinal)
                        || !Equals(retained.Intent, snapshot.Intent)
                        || !string.Equals(retained.BaselineProjectionVersion, snapshot.BaselineProjectionVersion, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    _snapshots[intent.TenantId] = Merge(retained, snapshot);
                    return true;
                }

                _snapshots[intent.TenantId] = snapshot;
                ScheduleExpiryLocked(intent.TenantId, snapshot.AttemptStartedAtUtc.Value);
                return true;
            }
        }
        finally
        {
            RaiseOwnershipExpired(expired);
        }
    }

    /// <summary>Releases the retained attempt only when the expected message still owns it.</summary>
    public void Forget(string tenantId, string messageId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        lock (_sync)
        {
            if (_snapshots.TryGetValue(tenantId, out TenantRemoveConfigurationCommandSnapshot? retained)
                && string.Equals(retained.MessageId, messageId, StringComparison.Ordinal))
            {
                _ = _snapshots.Remove(tenantId);
            }

            if (_dispatches.TryGetValue(tenantId, out var dispatch)
                && string.Equals(dispatch.MessageId, messageId, StringComparison.Ordinal))
            {
                _ = _dispatches.Remove(tenantId);
            }

            if (!_snapshots.ContainsKey(tenantId) && !_dispatches.ContainsKey(tenantId))
            {
                CancelExpiryLocked(tenantId);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            foreach (CancellationTokenSource cancellation in _expiryByTenant.Values)
            {
                cancellation.Cancel();
                cancellation.Dispose();
            }

            _expiryByTenant.Clear();
        }
    }

    /// <summary>Gets whether this tenant still has retained or pre-response dispatch ownership.</summary>
    internal bool HasPendingOwnership(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        List<string> expired = [];
        try
        {
            lock (_sync)
            {
                PruneExpiredLocked(_utcNow().ToUniversalTime(), expired);
                return _snapshots.ContainsKey(tenantId) || _dispatches.ContainsKey(tenantId);
            }
        }
        finally
        {
            RaiseOwnershipExpired(expired);
        }
    }

    private static TenantRemoveConfigurationCommandSnapshot Merge(
        TenantRemoveConfigurationCommandSnapshot retained,
        TenantRemoveConfigurationCommandSnapshot incoming)
    {
        int retainedRank = ProgressRank(retained.State);
        int incomingRank = ProgressRank(incoming.State);
        TenantLifecycleSequenceRelation proofRelation = TenantLifecycleProjectionVersion.CompareSequences(
            incoming.LastConfigurationProof?.ProjectionVersion,
            retained.LastConfigurationProof?.ProjectionVersion);
        bool newerSafetyEvidence = incoming.State is TenantCommandLifecycleState.UnableToVerify
            && incoming.StatusObservationCount > retained.StatusObservationCount;
        TenantRemoveConfigurationCommandSnapshot preferred = newerSafetyEvidence
            ? incoming
            : incomingRank > retainedRank
                ? incoming
            : incomingRank < retainedRank
                ? retained
                : incoming.StatusObservationCount > retained.StatusObservationCount
                    ? incoming
                    : incoming.StatusObservationCount < retained.StatusObservationCount
                        ? retained
                        : incoming.PendingStatusPollCount >= retained.PendingStatusPollCount
                            ? incoming
                            : retained;
        TenantConfigurationProjectionProof? proof = proofRelation switch
        {
            TenantLifecycleSequenceRelation.IncomingNewer => incoming.LastConfigurationProof,
            TenantLifecycleSequenceRelation.IncomingOlder => retained.LastConfigurationProof,
            _ => preferred.LastConfigurationProof,
        };

        return preferred with
        {
            Intent = retained.Intent,
            Preview = retained.Preview,
            MessageId = retained.MessageId,
            CorrelationId = retained.CorrelationId ?? incoming.CorrelationId,
            BaselineProjectionVersion = retained.BaselineProjectionVersion,
            AttemptStartedAtUtc = retained.AttemptStartedAtUtc,
            HasCommandEventEvidence = retained.HasCommandEventEvidence || incoming.HasCommandEventEvidence,
            PendingStatusPollCount = Math.Max(retained.PendingStatusPollCount, incoming.PendingStatusPollCount),
            StatusObservationCount = Math.Max(retained.StatusObservationCount, incoming.StatusObservationCount),
            LastConfigurationProof = proof,
        };
    }

    // Every caller holds _sync, so the notifications are collected here and raised by PruneExpired once the
    // lock is released. ObserveExpiryAsync already took that shape deliberately: the subscriber re-enters
    // this tracker and queues a render, which must not run while the tracker's own state is held.
    private void PruneExpiredLocked(DateTimeOffset observedAtUtc, List<string> expired)
    {
        foreach ((string tenantId, TenantRemoveConfigurationCommandSnapshot snapshot) in _snapshots.ToArray())
        {
            if (snapshot.IsRetentionExpired(observedAtUtc))
            {
                _ = _snapshots.Remove(tenantId);
                _ = _dispatches.Remove(tenantId);
                CancelExpiryLocked(tenantId);
                expired.Add(tenantId);
            }
        }

        foreach ((string tenantId, var dispatch) in _dispatches.ToArray())
        {
            if (IsExpired(dispatch.AttemptStartedAtUtc, observedAtUtc))
            {
                _ = _dispatches.Remove(tenantId);
                CancelExpiryLocked(tenantId);
                expired.Add(tenantId);
            }
        }
    }

    private void RaiseOwnershipExpired(List<string> expired)
    {
        if (expired.Count == 0)
        {
            return;
        }

        Action<string>? handler;
        lock (_sync)
        {
            handler = OwnershipExpired;
        }

        foreach (string tenantId in expired)
        {
            handler?.Invoke(tenantId);
        }
    }

    private void ScheduleExpiryLocked(string tenantId, DateTimeOffset startedAtUtc)
    {
        if (_disposed || _expiryByTenant.ContainsKey(tenantId))
        {
            return;
        }

        var cancellation = new CancellationTokenSource();
        _expiryByTenant[tenantId] = cancellation;
        _ = ObserveExpiryAsync(
            tenantId,
            startedAtUtc + TenantRemoveConfigurationCommandSnapshot.MaximumRetainedAttemptDuration,
            cancellation.Token);
    }

    private async Task ObserveExpiryAsync(
        string tenantId,
        DateTimeOffset deadline,
        CancellationToken cancellationToken)
    {
        try
        {
            TimeSpan remaining = deadline - _utcNow().ToUniversalTime();
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, cancellationToken).ConfigureAwait(false);
            }

            Action<string>? expired;
            lock (_sync)
            {
                if (_disposed || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                _ = _snapshots.Remove(tenantId);
                _ = _dispatches.Remove(tenantId);
                if (_expiryByTenant.Remove(tenantId, out CancellationTokenSource? cancellation))
                {
                    cancellation.Dispose();
                }

                expired = OwnershipExpired;
            }

            expired?.Invoke(tenantId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void CancelExpiryLocked(string tenantId)
    {
        if (_expiryByTenant.Remove(tenantId, out CancellationTokenSource? cancellation))
        {
            cancellation.Cancel();
            cancellation.Dispose();
        }
    }

    private static int ProgressRank(TenantCommandLifecycleState state)
        => state switch
        {
            TenantCommandLifecycleState.RequestSent => 0,
            TenantCommandLifecycleState.UnableToVerify => 1,
            TenantCommandLifecycleState.Accepted => 2,
            TenantCommandLifecycleState.ProjectionPending => 3,
            TenantCommandLifecycleState.Degraded => 4,
            _ => int.MinValue,
        };

    private static bool IsExpired(DateTimeOffset startedAtUtc, DateTimeOffset observedAtUtc)
        => observedAtUtc.ToUniversalTime() < startedAtUtc.ToUniversalTime()
            || observedAtUtc.ToUniversalTime() - startedAtUtc.ToUniversalTime()
                >= TenantRemoveConfigurationCommandSnapshot.MaximumRetainedAttemptDuration;
}
