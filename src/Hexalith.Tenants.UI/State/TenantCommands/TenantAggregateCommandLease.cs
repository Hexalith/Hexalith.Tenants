namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Represents ownership of one circuit-scoped aggregate command admission.</summary>
public sealed class TenantAggregateCommandLease
{
    private readonly TenantAggregateCommandAdmissionGate _gate;
    private int _dispatchMarked;
    private int _released;

    internal TenantAggregateCommandLease(
        TenantAggregateCommandAdmissionGate gate,
        string aggregateLockKey,
        object owner)
    {
        _gate = gate;
        AggregateLockKey = aggregateLockKey;
        Owner = owner;
    }

    /// <summary>Gets the admitted aggregate key.</summary>
    public string AggregateLockKey { get; }

    /// <summary>Gets a value indicating whether dispatch has been marked.</summary>
    public bool IsDispatchMarked => Volatile.Read(ref _dispatchMarked) != 0;

    internal object Owner { get; }

    /// <summary>Marks the single dispatch associated with this lease.</summary>
    /// <returns><see langword="true"/> only for the first mark while the lease is active.</returns>
    public bool TryMarkDispatched()
        => Volatile.Read(ref _released) == 0
            && Interlocked.CompareExchange(ref _dispatchMarked, 1, 0) == 0;

    /// <summary>Abandons an admitted attempt before any command dispatch.</summary>
    /// <returns><see langword="true"/> when the active pre-dispatch lease was released.</returns>
    public bool TryAbandonBeforeDispatch()
    {
        if (IsDispatchMarked || Interlocked.CompareExchange(ref _released, 1, 0) != 0)
        {
            return false;
        }

        if (_gate.TryReleaseLease(this, requireDispatched: false))
        {
            return true;
        }

        Volatile.Write(ref _released, 0);
        return false;
    }

    /// <summary>Releases a dispatched attempt only after explicit terminal lifecycle evidence.</summary>
    /// <param name="terminalState">Explicit command lifecycle state.</param>
    /// <returns><see langword="true"/> when the active lease was released.</returns>
    public bool TryReleaseTerminal(TenantCommandLifecycleState terminalState)
    {
        if (!IsDispatchMarked
            || !IsTerminal(terminalState)
            || Interlocked.CompareExchange(ref _released, 1, 0) != 0)
        {
            return false;
        }

        if (_gate.TryReleaseLease(this, requireDispatched: true))
        {
            return true;
        }

        Volatile.Write(ref _released, 0);
        return false;
    }

    private static bool IsTerminal(TenantCommandLifecycleState state)
        => state is TenantCommandLifecycleState.Confirmed
            or TenantCommandLifecycleState.Failed
            or TenantCommandLifecycleState.Rejected
            or TenantCommandLifecycleState.AlreadyApplied;
}
