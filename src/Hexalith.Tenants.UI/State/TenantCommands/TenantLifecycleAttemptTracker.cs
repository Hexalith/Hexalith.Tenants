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
    public void Remember(TenantLifecycleCommandSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        TenantLifecycleCommandRequest intent = snapshot.Intent
            ?? throw new ArgumentException("A retained lifecycle attempt must carry its intent.", nameof(snapshot));
        ArgumentException.ThrowIfNullOrWhiteSpace(intent.TenantId);

        lock (_sync)
        {
            if (_snapshotByTenantId.TryGetValue(intent.TenantId, out TenantLifecycleCommandSnapshot? retained)
                && !string.IsNullOrWhiteSpace(retained.MessageId)
                && !string.Equals(retained.MessageId, snapshot.MessageId, StringComparison.Ordinal))
            {
                return;
            }

            _snapshotByTenantId[intent.TenantId] = snapshot;
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

            _ = _snapshotByTenantId.Remove(tenantId);
        }
    }
}
