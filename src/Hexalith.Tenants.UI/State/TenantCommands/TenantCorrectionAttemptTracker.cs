using Hexalith.Tenants.UI.State.TenantAudit;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Retains one correction attempt per tenant for the lifetime of an interactive circuit.</summary>
public sealed class TenantCorrectionAttemptTracker
{
    private readonly object _sync = new();
    private readonly Dictionary<string, TenantCorrectionAttempt> _attempts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _deliveriesInFlight = new(StringComparer.Ordinal);
    private readonly object _leaseOwner = new();

    /// <summary>Returns the retained attempt, including uncertain delivery, after a panel is remounted.</summary>
    internal TenantCorrectionAttempt? Find(string tenantId)
    {
        lock (_sync)
        {
            return _attempts.GetValueOrDefault(tenantId);
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
            _attempts.TryGetValue(preview.TenantId, out TenantCorrectionAttempt? previous);
            if (previous is not null)
            {
                if (!IsTerminal(previous.Snapshot.LifecycleState))
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
            DateTimeOffset startedAtUtc = DateTimeOffset.UtcNow;
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

            return true;
        }
    }

    /// <summary>Allows one same-ID retry only after the earlier delivery operation has finished.</summary>
    internal bool TryStartRetry(string tenantId, string messageId)
    {
        lock (_sync)
        {
            if (_deliveriesInFlight.Contains(tenantId)
                || !_attempts.TryGetValue(tenantId, out TenantCorrectionAttempt? attempt)
                || !string.Equals(attempt.MessageId, messageId, StringComparison.Ordinal)
                || !string.IsNullOrWhiteSpace(attempt.Snapshot.CorrelationId)
                || IsTerminal(attempt.Snapshot.LifecycleState))
            {
                return false;
            }

            return _deliveriesInFlight.Add(tenantId);
        }
    }

    /// <summary>Ends only delivery of the retained message id.</summary>
    internal void EndDelivery(string tenantId, string messageId)
    {
        lock (_sync)
        {
            if (_attempts.TryGetValue(tenantId, out TenantCorrectionAttempt? attempt)
                && string.Equals(attempt.MessageId, messageId, StringComparison.Ordinal))
            {
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

            _attempts[tenantId] = current with { Snapshot = snapshot with { MessageId = messageId } };
            if (snapshot.LifecycleState is TenantCommandLifecycleState.Confirmed
                or TenantCommandLifecycleState.Rejected
                or TenantCommandLifecycleState.AlreadyApplied
                or TenantCommandLifecycleState.Failed)
            {
                current.Lease.TryReleaseTerminal(_leaseOwner, snapshot.LifecycleState);
            }

            return true;
        }
    }

    private static bool IsTerminal(TenantCommandLifecycleState state)
        => state is TenantCommandLifecycleState.Confirmed
            or TenantCommandLifecycleState.Rejected
            or TenantCommandLifecycleState.AlreadyApplied
            or TenantCommandLifecycleState.Failed;
}
