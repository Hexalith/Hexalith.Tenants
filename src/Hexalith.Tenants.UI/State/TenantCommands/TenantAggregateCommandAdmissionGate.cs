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
    private readonly Dictionary<string, int> _lockDepthByKey = new(StringComparer.Ordinal);

    /// <summary>
    /// Acquires or re-enters the lock for <paramref name="aggregateLockKey"/>.
    /// Unrelated keys remain independently acquirable on the same circuit.
    /// </summary>
    /// <param name="aggregateLockKey">AggregateIdentity-shaped lock key for the command attempt.</param>
    /// <returns>Always <see langword="true"/>; acquisition is per-key and non-exclusive across aggregates.</returns>
    public bool TryAcquire(string aggregateLockKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateLockKey);

        lock (_sync)
        {
            _lockDepthByKey[aggregateLockKey] = _lockDepthByKey.TryGetValue(aggregateLockKey, out int depth)
                ? depth + 1
                : 1;
            return true;
        }
    }

    /// <summary>
    /// Releases one hold for <paramref name="aggregateLockKey"/>. Unrelated keys are ignored.
    /// </summary>
    /// <param name="aggregateLockKey">AggregateIdentity-shaped lock key that previously acquired the lock.</param>
    public void Release(string aggregateLockKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateLockKey);

        lock (_sync)
        {
            if (!_lockDepthByKey.TryGetValue(aggregateLockKey, out int depth) || depth <= 0)
            {
                return;
            }

            if (depth == 1)
            {
                _ = _lockDepthByKey.Remove(aggregateLockKey);
                return;
            }

            _lockDepthByKey[aggregateLockKey] = depth - 1;
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
            return _lockDepthByKey.TryGetValue(aggregateLockKey, out int depth) && depth > 0;
        }
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
                return _lockDepthByKey.Count > 0;
            }
        }
    }
}
