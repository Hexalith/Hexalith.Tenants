using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Retains one support-safe set-configuration attempt per tenant and interactive circuit.</summary>
public sealed class TenantSetConfigurationAttemptTracker
{
    private readonly object _sync = new();
    private readonly Func<string> _newMessageId;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Dictionary<string, TenantSetConfigurationCommandSnapshot> _snapshots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (TenantSetConfigurationIntent Intent, string BaselineProjectionVersion, DateTimeOffset AttemptStartedAtUtc, string MessageId)> _dispatches = new(StringComparer.Ordinal);

    /// <summary>Initializes a tracker using the system UTC clock.</summary>
    public TenantSetConfigurationAttemptTracker()
        : this(static () => DateTimeOffset.UtcNow, static () => NUlid.Ulid.NewUlid().ToString())
    {
    }

    internal TenantSetConfigurationAttemptTracker(Func<DateTimeOffset> utcNow)
        : this(utcNow, static () => NUlid.Ulid.NewUlid().ToString())
    {
    }

    internal TenantSetConfigurationAttemptTracker(Func<DateTimeOffset> utcNow, Func<string> newMessageId)
    {
        ArgumentNullException.ThrowIfNull(utcNow);
        ArgumentNullException.ThrowIfNull(newMessageId);
        _utcNow = utcNow;
        _newMessageId = newMessageId;
    }

    /// <summary>Gets the stable admission-gate owner for retained set attempts.</summary>
    internal object LeaseOwner { get; } = new();

    /// <summary>Starts or resumes the dispatch identity for one exact safe intent.</summary>
    internal (string MessageId, DateTimeOffset AttemptStartedAtUtc) BeginDispatch(
        TenantSetConfigurationIntent intent,
        string baselineProjectionVersion,
        DateTimeOffset attemptStartedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentException.ThrowIfNullOrWhiteSpace(intent.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(intent.FullKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(intent.ValueFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(baselineProjectionVersion);

        lock (_sync)
        {
            PruneExpiredLocked(_utcNow().ToUniversalTime());
            if (_dispatches.TryGetValue(intent.TenantId, out var retained))
            {
                if (!Equals(retained.Intent, intent)
                    || !string.Equals(retained.BaselineProjectionVersion, baselineProjectionVersion, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "A different set-configuration intent already owns the tenant dispatch window.");
                }

                return (retained.MessageId, retained.AttemptStartedAtUtc);
            }

            DateTimeOffset normalized = attemptStartedAtUtc.ToUniversalTime();
            string messageId = _newMessageId();
            if (!NUlid.Ulid.TryParse(messageId, out _))
            {
                throw new InvalidOperationException("The set-configuration message id factory returned an invalid ULID.");
            }

            _dispatches[intent.TenantId] = (intent, baselineProjectionVersion, normalized, messageId);
            return (messageId, normalized);
        }
    }

    /// <summary>Returns the retained unresolved attempt for a literal tenant id.</summary>
    public TenantSetConfigurationCommandSnapshot? Find(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        lock (_sync)
        {
            PruneExpiredLocked(_utcNow().ToUniversalTime());
            return _snapshots.TryGetValue(tenantId, out TenantSetConfigurationCommandSnapshot? snapshot)
                ? snapshot
                : null;
        }
    }

    /// <summary>Monotonically retains the latest evidence for one logical attempt.</summary>
    public bool Remember(TenantSetConfigurationCommandSnapshot snapshot)
    {
        if (snapshot is null
            || !snapshot.RetainsAttempt
            || snapshot.Intent is not { } intent
            || string.IsNullOrWhiteSpace(snapshot.MessageId)
            || snapshot.AttemptStartedAtUtc is null)
        {
            return false;
        }

        lock (_sync)
        {
            DateTimeOffset now = _utcNow().ToUniversalTime();
            PruneExpiredLocked(now);
            if (snapshot.IsRetentionExpired(now))
            {
                _ = _snapshots.Remove(intent.TenantId);
                _ = _dispatches.Remove(intent.TenantId);
                return false;
            }

            if (_snapshots.TryGetValue(intent.TenantId, out TenantSetConfigurationCommandSnapshot? retained))
            {
                if (!string.Equals(retained.MessageId, snapshot.MessageId, StringComparison.Ordinal)
                    || !Equals(retained.Intent, snapshot.Intent)
                    || !string.Equals(
                        retained.BaselineProjectionVersion,
                        snapshot.BaselineProjectionVersion,
                        StringComparison.Ordinal))
                {
                    return false;
                }

                _snapshots[intent.TenantId] = Merge(retained, snapshot);
                return true;
            }

            _snapshots[intent.TenantId] = snapshot;
            return true;
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
            if (_snapshots.TryGetValue(tenantId, out TenantSetConfigurationCommandSnapshot? retained)
                && string.Equals(retained.MessageId, messageId, StringComparison.Ordinal))
            {
                _ = _snapshots.Remove(tenantId);
            }

            if (_dispatches.TryGetValue(tenantId, out var dispatch)
                && string.Equals(dispatch.MessageId, messageId, StringComparison.Ordinal))
            {
                _ = _dispatches.Remove(tenantId);
            }
        }
    }

    /// <summary>Gets whether this tenant still has retained or pre-response dispatch ownership.</summary>
    internal bool HasPendingOwnership(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        lock (_sync)
        {
            PruneExpiredLocked(_utcNow().ToUniversalTime());
            return _snapshots.ContainsKey(tenantId) || _dispatches.ContainsKey(tenantId);
        }
    }

    private static TenantSetConfigurationCommandSnapshot Merge(
        TenantSetConfigurationCommandSnapshot retained,
        TenantSetConfigurationCommandSnapshot incoming)
    {
        int retainedRank = ProgressRank(retained.State);
        int incomingRank = ProgressRank(incoming.State);
        TenantLifecycleSequenceRelation proofRelation = TenantLifecycleProjectionVersion.CompareSequences(
            incoming.LastConfigurationProof?.ProjectionVersion,
            retained.LastConfigurationProof?.ProjectionVersion);
        TenantSetConfigurationCommandSnapshot preferred = incomingRank > retainedRank
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

        bool hasCommandEventEvidence = retained.HasCommandEventEvidence || incoming.HasCommandEventEvidence;
        return preferred with
        {
            Intent = retained.Intent,
            Preview = retained.Preview,
            MessageId = retained.MessageId,
            CorrelationId = retained.CorrelationId ?? incoming.CorrelationId,
            BaselineProjectionVersion = retained.BaselineProjectionVersion,
            AttemptStartedAtUtc = retained.AttemptStartedAtUtc,
            CompletedWithoutEvents = !hasCommandEventEvidence
                && (retained.CompletedWithoutEvents || incoming.CompletedWithoutEvents),
            HasCommandEventEvidence = hasCommandEventEvidence,
            PendingStatusPollCount = Math.Max(retained.PendingStatusPollCount, incoming.PendingStatusPollCount),
            StatusObservationCount = Math.Max(retained.StatusObservationCount, incoming.StatusObservationCount),
            LastConfigurationProof = proof,
        };
    }

    private void PruneExpiredLocked(DateTimeOffset observedAtUtc)
    {
        foreach ((string tenantId, TenantSetConfigurationCommandSnapshot snapshot) in _snapshots.ToArray())
        {
            if (snapshot.IsRetentionExpired(observedAtUtc))
            {
                _ = _snapshots.Remove(tenantId);
                _ = _dispatches.Remove(tenantId);
            }
        }

        foreach ((string tenantId, var dispatch) in _dispatches.ToArray())
        {
            if (IsExpired(dispatch.AttemptStartedAtUtc, observedAtUtc))
            {
                _ = _dispatches.Remove(tenantId);
            }
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
                >= TenantSetConfigurationCommandSnapshot.MaximumRetainedAttemptDuration;
}
