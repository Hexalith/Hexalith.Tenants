using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>
/// Retains one lifecycle command attempt per tenant for the lifetime of an interactive circuit.
/// </summary>
/// <remarks>
/// The retained snapshot carries the literal intent, stable message id, accepted tracking handle,
/// pre-submit projection baseline, and command-event evidence. A lifecycle surface that is remounted
/// therefore adopts the same logical attempt instead of dispatching another command.
/// </remarks>
public sealed class TenantLifecycleAttemptTracker
{
    private readonly object _sync = new();
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Dictionary<string, TenantLifecycleCommandSnapshot> _snapshotByTenantId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (TenantLifecycleCommandRequest Intent, string BaselineProjectionVersion, DateTimeOffset AttemptStartedAtUtc, string MessageId)> _dispatchIdentityByTenantId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (string MessageId, DateTimeOffset? AttemptStartedAtUtc, DateTimeOffset TerminalObservedAtUtc)> _terminalByTenantId = new(StringComparer.Ordinal);

    /// <summary>Initializes a circuit-local tracker that observes the system UTC clock.</summary>
    public TenantLifecycleAttemptTracker()
        : this(static () => DateTimeOffset.UtcNow)
    {
    }

    internal TenantLifecycleAttemptTracker(Func<DateTimeOffset> utcNow)
    {
        ArgumentNullException.ThrowIfNull(utcNow);
        _utcNow = utcNow;
    }

    /// <summary>
    /// Gets the stable circuit-local admission owner used while a retained lifecycle attempt is pending.
    /// </summary>
    internal object LeaseOwner { get; } = new();

    /// <summary>
    /// Starts or resumes the dispatch window for one logical lifecycle attempt.
    /// </summary>
    /// <param name="intent">Literal tenant and lifecycle operation.</param>
    /// <param name="baselineProjectionVersion">Projection version captured immediately before dispatch.</param>
    /// <param name="attemptStartedAtUtc">UTC instant at which the logical attempt started.</param>
    /// <returns>The deterministic message id and stable attempt start for dispatch or redispatch.</returns>
    internal (string MessageId, DateTimeOffset AttemptStartedAtUtc, string BaselineProjectionVersion) BeginDispatch(
        TenantLifecycleCommandRequest intent,
        string baselineProjectionVersion,
        DateTimeOffset attemptStartedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentException.ThrowIfNullOrWhiteSpace(intent.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(baselineProjectionVersion);
        if (!Enum.IsDefined(intent.Operation))
        {
            throw new ArgumentOutOfRangeException(nameof(intent), intent.Operation, null);
        }

        lock (_sync)
        {
            PruneExpiredLocked(_utcNow().ToUniversalTime());
            if (_dispatchIdentityByTenantId.TryGetValue(intent.TenantId, out var existing)
                && Equals(existing.Intent, intent))
            {
                return (existing.MessageId, existing.AttemptStartedAtUtc, existing.BaselineProjectionVersion);
            }

            if (_dispatchIdentityByTenantId.ContainsKey(intent.TenantId))
            {
                throw new InvalidOperationException(
                    "A different lifecycle intent already owns the tenant dispatch window.");
            }

            DateTimeOffset normalizedAttemptStart = attemptStartedAtUtc.ToUniversalTime();
            string messageId = CreateDeterministicMessageId(intent, baselineProjectionVersion, normalizedAttemptStart);
            _dispatchIdentityByTenantId[intent.TenantId] = (
                intent,
                baselineProjectionVersion,
                normalizedAttemptStart,
                messageId);
            return (messageId, normalizedAttemptStart, baselineProjectionVersion);
        }
    }

    /// <summary>Returns the unresolved dispatch-window intent for one literal tenant id.</summary>
    /// <param name="tenantId">Literal tenant id.</param>
    /// <returns>The dispatch intent, or <see langword="null"/> when no dispatch window is open.</returns>
    internal TenantLifecycleCommandRequest? FindDispatchIntent(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        lock (_sync)
        {
            PruneExpiredLocked(_utcNow().ToUniversalTime());
            return _dispatchIdentityByTenantId.TryGetValue(tenantId, out var existing)
                ? existing.Intent
                : null;
        }
    }

    /// <summary>
    /// Returns the retained non-terminal lifecycle attempt for <paramref name="tenantId"/>.
    /// </summary>
    /// <param name="tenantId">Literal caller-supplied tenant id.</param>
    /// <returns>The retained snapshot, or <see langword="null"/> when no attempt is retained.</returns>
    public TenantLifecycleCommandSnapshot? Find(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        lock (_sync)
        {
            PruneExpiredLocked(_utcNow().ToUniversalTime());
            return _snapshotByTenantId.TryGetValue(tenantId, out TenantLifecycleCommandSnapshot? snapshot)
                ? snapshot
                : null;
        }
    }

    /// <summary>
    /// Retains the latest state of a non-terminal lifecycle attempt.
    /// </summary>
    /// <param name="snapshot">Lifecycle attempt snapshot to retain.</param>
    /// <returns><see langword="true"/> when the snapshot is retained; otherwise <see langword="false"/>.</returns>
    public bool Remember(TenantLifecycleCommandSnapshot snapshot)
    {
        if (snapshot is null
            || !snapshot.RetainsAttempt
            || snapshot.Intent is not { } intent
            || string.IsNullOrWhiteSpace(intent.TenantId)
            || !Enum.IsDefined(intent.Operation)
            || string.IsNullOrWhiteSpace(snapshot.MessageId)
            || snapshot.AttemptStartedAtUtc is null
            || (snapshot.State is not TenantCommandLifecycleState.RequestSent
                && string.IsNullOrWhiteSpace(snapshot.CorrelationId)))
        {
            return false;
        }

        lock (_sync)
        {
            DateTimeOffset observedAtUtc = _utcNow().ToUniversalTime();
            PruneExpiredLocked(observedAtUtc);
            if (snapshot.IsRetentionExpired(observedAtUtc))
            {
                RememberTerminalLocked(
                    intent.TenantId,
                    snapshot.MessageId!,
                    snapshot.AttemptStartedAtUtc,
                    observedAtUtc);
                return false;
            }

            if (_terminalByTenantId.TryGetValue(intent.TenantId, out var terminal))
            {
                if (string.Equals(terminal.MessageId, snapshot.MessageId, StringComparison.Ordinal))
                {
                    return false;
                }

                if (CompareAttemptIdentity(
                    snapshot.AttemptStartedAtUtc.Value,
                    snapshot.MessageId!,
                    terminal.AttemptStartedAtUtc,
                    terminal.MessageId) <= 0)
                {
                    return false;
                }
            }

            if (_snapshotByTenantId.TryGetValue(intent.TenantId, out TenantLifecycleCommandSnapshot? retained))
            {
                if (string.Equals(retained.MessageId, snapshot.MessageId, StringComparison.Ordinal))
                {
                    if (!Equals(retained.Intent, snapshot.Intent)
                        || retained.CorrelationId is not null
                            && !string.Equals(
                                retained.CorrelationId,
                                snapshot.CorrelationId,
                                StringComparison.Ordinal))
                    {
                        return false;
                    }

                    _snapshotByTenantId[intent.TenantId] = MergeSameAttempt(retained, snapshot);
                    return true;
                }

                if (CompareAttemptIdentity(
                    snapshot.AttemptStartedAtUtc.Value,
                    snapshot.MessageId!,
                    retained.AttemptStartedAtUtc,
                    retained.MessageId!) <= 0)
                {
                    return false;
                }
            }

            _snapshotByTenantId[intent.TenantId] = snapshot;
            if (snapshot.State is not TenantCommandLifecycleState.RequestSent)
            {
                _ = _dispatchIdentityByTenantId.Remove(intent.TenantId);
            }

            return true;
        }
    }

    /// <summary>
    /// Removes the retained attempt after terminal ownership is reached.
    /// </summary>
    /// <param name="tenantId">Literal caller-supplied tenant id.</param>
    /// <param name="messageId">
    /// Stable message id that must still identify the retained attempt. This prevents a late terminal
    /// completion from removing a newer attempt for the same tenant.
    /// </param>
    public void Forget(string tenantId, string messageId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        lock (_sync)
        {
            DateTimeOffset observedAtUtc = _utcNow().ToUniversalTime();
            PruneExpiredLocked(observedAtUtc);
            if (_dispatchIdentityByTenantId.TryGetValue(tenantId, out var dispatchIdentity)
                && string.Equals(dispatchIdentity.MessageId, messageId, StringComparison.Ordinal))
            {
                RememberTerminalLocked(
                    tenantId,
                    dispatchIdentity.MessageId,
                    dispatchIdentity.AttemptStartedAtUtc,
                    observedAtUtc);
                _ = _dispatchIdentityByTenantId.Remove(tenantId);
            }

            if (!_snapshotByTenantId.TryGetValue(tenantId, out TenantLifecycleCommandSnapshot? retained)
                || !string.Equals(retained.MessageId, messageId, StringComparison.Ordinal))
            {
                return;
            }

            RememberTerminalLocked(
                tenantId,
                retained.MessageId!,
                retained.AttemptStartedAtUtc,
                observedAtUtc);
            _ = _snapshotByTenantId.Remove(tenantId);
        }
    }

    /// <summary>Gets whether retained or unresolved dispatch ownership is still live.</summary>
    /// <param name="tenantId">Literal tenant id.</param>
    /// <returns><see langword="true"/> until the bounded window expires or the attempt terminalizes.</returns>
    internal bool HasPendingOwnership(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        lock (_sync)
        {
            PruneExpiredLocked(_utcNow().ToUniversalTime());
            return _snapshotByTenantId.ContainsKey(tenantId)
                || _dispatchIdentityByTenantId.ContainsKey(tenantId);
        }
    }

    private static TenantLifecycleCommandSnapshot MergeSameAttempt(
        TenantLifecycleCommandSnapshot retained,
        TenantLifecycleCommandSnapshot incoming)
    {
        int retainedRank = ProgressRank(retained.State);
        int incomingRank = ProgressRank(incoming.State);
        int projectionComparison = TenantLifecycleProjectionVersion.CompareSequences(
            incoming.LastObservedProjectionVersion,
            retained.LastObservedProjectionVersion);
        TenantLifecycleCommandSnapshot preferred = incomingRank != retainedRank
            ? incomingRank > retainedRank ? incoming : retained
            : incoming.EvidenceRevision != retained.EvidenceRevision
                ? incoming.EvidenceRevision > retained.EvidenceRevision ? incoming : retained
                : incoming.PendingStatusPollCount != retained.PendingStatusPollCount
                    ? incoming.PendingStatusPollCount > retained.PendingStatusPollCount ? incoming : retained
                    : projectionComparison > 0 ? incoming : retained;
        TenantLifecycleCommandSnapshot projectionEvidence = projectionComparison switch
        {
            > 0 => incoming,
            < 0 => retained,
            _ => preferred,
        };

        return preferred with
        {
            State = incomingRank > retainedRank ? incoming.State : retained.State,
            HasCommandEventEvidence = retained.HasCommandEventEvidence || incoming.HasCommandEventEvidence,
            PendingStatusPollCount = Math.Max(retained.PendingStatusPollCount, incoming.PendingStatusPollCount),
            AttemptStartedAtUtc = retained.AttemptStartedAtUtc ?? incoming.AttemptStartedAtUtc,
            LastConfirmedStatus = projectionEvidence.LastConfirmedStatus,
            LastConfirmedProjection = projectionEvidence.LastConfirmedProjection,
            LastObservedProjectionVersion = projectionEvidence.LastObservedProjectionVersion,
            EvidenceRevision = Math.Max(retained.EvidenceRevision, incoming.EvidenceRevision),
        };
    }

    private static int CompareAttemptIdentity(
        DateTimeOffset incomingStartedAtUtc,
        string incomingMessageId,
        DateTimeOffset? retainedStartedAtUtc,
        string retainedMessageId)
    {
        if (retainedStartedAtUtc is null)
        {
            return 1;
        }

        int startedAtComparison = incomingStartedAtUtc.ToUniversalTime()
            .CompareTo(retainedStartedAtUtc.Value.ToUniversalTime());
        return startedAtComparison != 0
            ? startedAtComparison
            : string.CompareOrdinal(incomingMessageId, retainedMessageId);
    }

    private void PruneExpiredLocked(DateTimeOffset observedAtUtc)
    {
        foreach (string tenantId in _terminalByTenantId
            .Where(item => IsExpired(item.Value.TerminalObservedAtUtc, observedAtUtc))
            .Select(static item => item.Key)
            .ToArray())
        {
            _ = _terminalByTenantId.Remove(tenantId);
        }

        foreach ((string tenantId, TenantLifecycleCommandSnapshot snapshot) in _snapshotByTenantId.ToArray())
        {
            if (!snapshot.IsRetentionExpired(observedAtUtc))
            {
                continue;
            }

            RememberTerminalLocked(
                tenantId,
                snapshot.MessageId!,
                snapshot.AttemptStartedAtUtc,
                observedAtUtc);
            _ = _snapshotByTenantId.Remove(tenantId);
            _ = _dispatchIdentityByTenantId.Remove(tenantId);
        }

        foreach ((string tenantId, var dispatch) in _dispatchIdentityByTenantId.ToArray())
        {
            if (!IsExpired(dispatch.AttemptStartedAtUtc, observedAtUtc))
            {
                continue;
            }

            RememberTerminalLocked(
                tenantId,
                dispatch.MessageId,
                dispatch.AttemptStartedAtUtc,
                observedAtUtc);
            _ = _dispatchIdentityByTenantId.Remove(tenantId);
        }
    }

    private void RememberTerminalLocked(
        string tenantId,
        string messageId,
        DateTimeOffset? attemptStartedAtUtc,
        DateTimeOffset observedAtUtc)
        => _terminalByTenantId[tenantId] = (messageId, attemptStartedAtUtc, observedAtUtc);

    private static string CreateDeterministicMessageId(
        TenantLifecycleCommandRequest intent,
        string baselineProjectionVersion,
        DateTimeOffset attemptStartedAtUtc)
    {
        string material = string.Create(
            CultureInfo.InvariantCulture,
            $"{intent.TenantId.Length}:{intent.TenantId}|{(int)intent.Operation}|{baselineProjectionVersion.Length}:{baselineProjectionVersion}|{attemptStartedAtUtc.UtcDateTime.Ticks}");
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        Span<byte> deterministicUlid = stackalloc byte[16];
        ulong unixTimeMilliseconds = (ulong)Math.Max(0, attemptStartedAtUtc.ToUnixTimeMilliseconds());
        for (int index = 5; index >= 0; index--)
        {
            deterministicUlid[index] = (byte)unixTimeMilliseconds;
            unixTimeMilliseconds >>= 8;
        }

        digest.AsSpan(0, 10).CopyTo(deterministicUlid[6..]);
        return new NUlid.Ulid(deterministicUlid).ToString();
    }

    private static int ProgressRank(TenantCommandLifecycleState state)
        => state switch
        {
            TenantCommandLifecycleState.RequestSent => 0,
            TenantCommandLifecycleState.Accepted => 1,
            TenantCommandLifecycleState.ProjectionPending => 2,
            _ => int.MinValue,
        };

    private static bool IsExpired(DateTimeOffset startedAtUtc, DateTimeOffset observedAtUtc)
    {
        DateTimeOffset normalizedStart = startedAtUtc.ToUniversalTime();
        DateTimeOffset normalizedObserved = observedAtUtc.ToUniversalTime();
        return normalizedObserved < normalizedStart
            || normalizedObserved - normalizedStart
                >= TenantLifecycleCommandSnapshot.MaximumRetainedAttemptDuration;
    }
}
