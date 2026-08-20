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

        lock (_sync)
        {
            if (_ownerByKey.ContainsKey(aggregateLockKey))
            {
                return false;
            }

            _ownerByKey.Add(aggregateLockKey, owner);
            return true;
        }
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

        lock (_sync)
        {
            if (_ownerByKey.TryGetValue(aggregateLockKey, out object? currentOwner)
                && ReferenceEquals(currentOwner, owner))
            {
                _ = _ownerByKey.Remove(aggregateLockKey);
            }
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
            return _ownerByKey.TryGetValue(aggregateLockKey, out object? currentOwner)
                && !ReferenceEquals(currentOwner, owner);
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
                return _ownerByKey.Count > 0;
            }
        }
    }
}
