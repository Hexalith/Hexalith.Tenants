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
    private readonly Dictionary<string, TenantLifecycleCommandSnapshot> _snapshotByTenantId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _terminalMessageByTenantId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset?> _terminalAttemptStartedAtByTenantId = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the stable circuit-local admission owner used while a retained lifecycle attempt is pending.
    /// </summary>
    internal object LeaseOwner { get; } = new();

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
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!snapshot.RetainsAttempt)
        {
            throw new ArgumentException("Only accepted or projection-pending lifecycle attempts can be retained.", nameof(snapshot));
        }

        TenantLifecycleCommandRequest intent = snapshot.Intent
            ?? throw new ArgumentException("A retained lifecycle attempt must carry its intent.", nameof(snapshot));
        ArgumentException.ThrowIfNullOrWhiteSpace(intent.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshot.MessageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshot.CorrelationId);

        lock (_sync)
        {
            if (_terminalMessageByTenantId.TryGetValue(intent.TenantId, out string? terminalMessageId))
            {
                if (string.Equals(terminalMessageId, snapshot.MessageId, StringComparison.Ordinal))
                {
                    return false;
                }

                DateTimeOffset? terminalStartedAt = _terminalAttemptStartedAtByTenantId[intent.TenantId];
                if (snapshot.AttemptStartedAtUtc is null
                    || terminalStartedAt is null
                    || snapshot.AttemptStartedAtUtc <= terminalStartedAt)
                {
                    return false;
                }
            }

            if (_snapshotByTenantId.TryGetValue(intent.TenantId, out TenantLifecycleCommandSnapshot? retained))
            {
                if (string.Equals(retained.MessageId, snapshot.MessageId, StringComparison.Ordinal))
                {
                    if (!Equals(retained.Intent, snapshot.Intent)
                        || !string.Equals(retained.CorrelationId, snapshot.CorrelationId, StringComparison.Ordinal))
                    {
                        return false;
                    }

                    _snapshotByTenantId[intent.TenantId] = MergeSameAttempt(retained, snapshot);
                    return true;
                }

                if (snapshot.AttemptStartedAtUtc is null
                    || retained.AttemptStartedAtUtc is null
                    || snapshot.AttemptStartedAtUtc <= retained.AttemptStartedAtUtc)
                {
                    return false;
                }
            }

            _snapshotByTenantId[intent.TenantId] = snapshot;
            return true;
        }
    }

    /// <summary>
    /// Removes the retained attempt after terminal ownership is reached.
    /// </summary>
    /// <param name="tenantId">Literal caller-supplied tenant id.</param>
    /// <param name="messageId">
    /// Optional stable message id that must still identify the retained attempt. This prevents a late
    /// terminal completion from removing a newer attempt for the same tenant.
    /// </param>
    public void Forget(string tenantId, string? messageId = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return;
        }

        lock (_sync)
        {
            if (!_snapshotByTenantId.TryGetValue(tenantId, out TenantLifecycleCommandSnapshot? retained)
                || !string.IsNullOrWhiteSpace(messageId)
                    && !string.Equals(retained.MessageId, messageId, StringComparison.Ordinal))
            {
                return;
            }

            _terminalMessageByTenantId[tenantId] = retained.MessageId!;
            _terminalAttemptStartedAtByTenantId[tenantId] = retained.AttemptStartedAtUtc;
            _ = _snapshotByTenantId.Remove(tenantId);
        }
    }

    private static TenantLifecycleCommandSnapshot MergeSameAttempt(
        TenantLifecycleCommandSnapshot retained,
        TenantLifecycleCommandSnapshot incoming)
    {
        int retainedRank = ProgressRank(retained.State);
        int incomingRank = ProgressRank(incoming.State);
        TenantLifecycleCommandSnapshot preferred = incomingRank > retainedRank
            || incomingRank == retainedRank
                && incoming.PendingStatusPollCount > retained.PendingStatusPollCount
            ? incoming
            : retained;

        return preferred with
        {
            State = incomingRank > retainedRank ? incoming.State : retained.State,
            HasCommandEventEvidence = retained.HasCommandEventEvidence || incoming.HasCommandEventEvidence,
            PendingStatusPollCount = Math.Max(retained.PendingStatusPollCount, incoming.PendingStatusPollCount),
            AttemptStartedAtUtc = retained.AttemptStartedAtUtc ?? incoming.AttemptStartedAtUtc,
        };
    }

    private static int ProgressRank(TenantCommandLifecycleState state)
        => state switch
        {
            TenantCommandLifecycleState.Accepted => 0,
            TenantCommandLifecycleState.ProjectionPending => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };
}
