using Hexalith.Tenants.UI.State.GlobalAdministrators;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Represents ownership of one circuit-scoped aggregate command admission.</summary>
public sealed class TenantAggregateCommandLease
{
    private readonly TenantAggregateCommandAdmissionGate _gate;

    internal TenantAggregateCommandLease(
        TenantAggregateCommandAdmissionGate gate,
        string aggregateLockKey,
        object owner)
    {
        _gate = gate;
        AggregateLockKey = aggregateLockKey;
        CurrentOwner = owner;
    }

    /// <summary>Gets the admitted aggregate key.</summary>
    public string AggregateLockKey { get; }

    /// <summary>Gets a value indicating whether dispatch has been marked.</summary>
    public bool IsDispatchMarked => _gate.IsDispatchMarked(this);

    internal object? CurrentOwner { get; set; }

    internal bool DispatchMarked { get; set; }

    internal bool IsReleased { get; set; }

    internal bool IsRetainedForAdoption { get; set; }

    internal GlobalAdministratorReconciliationState? Reconciliation { get; set; }

    internal long ActiveReconciliationDispatchToken { get; set; }

    /// <summary>Gets whether an exact same-command recovery dispatch is awaiting its delivery result.</summary>
    internal bool IsReconciliationDispatchInFlight
        => _gate.IsReconciliationDispatchInFlight(this);

    /// <summary>Marks the single dispatch associated with this lease.</summary>
    /// <returns><see langword="true"/> only for the first mark while the lease is active.</returns>
    public bool TryMarkDispatched(object owner)
        => _gate.TryMarkDispatched(this, owner);

    /// <summary>Advances the resumable reconciliation evidence while this surface owns the lease.</summary>
    internal bool TryAdvanceReconciliation(
        object owner,
        GlobalAdministratorReconciliationState reconciliation)
        => _gate.TryAdvanceReconciliation(this, owner, reconciliation);

    /// <summary>Retains reconciliation for exclusive adoption by a replacement surface.</summary>
    internal bool TryRetainReconciliation(
        object owner,
        GlobalAdministratorReconciliationState reconciliation)
        => _gate.TryRetainReconciliation(this, owner, reconciliation);

    /// <summary>Starts one lease-backed same-command delivery operation.</summary>
    /// <param name="owner">Current command-surface owner.</param>
    /// <param name="expected">Exact reconciliation being delivered.</param>
    /// <param name="completionToken">Opaque token that authorizes the matching completion.</param>
    /// <returns><see langword="true"/> when no delivery operation is already active.</returns>
    internal bool TryBeginReconciliationDispatch(
        object owner,
        GlobalAdministratorReconciliationState expected,
        out long completionToken)
        => _gate.TryBeginReconciliationDispatch(this, owner, expected, out completionToken);

    /// <summary>Publishes the exact delivery result for adoption by whichever surface now owns the lease.</summary>
    /// <param name="completionToken">Token returned when the delivery began.</param>
    /// <param name="completion">Monotonic result for the same command identity.</param>
    /// <returns><see langword="true"/> when the result became durable on the lease.</returns>
    internal bool TryCompleteReconciliationDispatch(
        long completionToken,
        GlobalAdministratorReconciliationState completion)
        => _gate.TryCompleteReconciliationDispatch(this, completionToken, completion);

    /// <summary>Reads the latest durable reconciliation when the supplied surface still owns this lease.</summary>
    /// <param name="owner">Current command-surface owner.</param>
    /// <param name="reconciliation">Latest durable evidence.</param>
    /// <returns><see langword="true"/> when the lease is active and owned by <paramref name="owner"/>.</returns>
    internal bool TryReadReconciliation(
        object owner,
        out GlobalAdministratorReconciliationState? reconciliation)
        => _gate.TryReadReconciliation(this, owner, out reconciliation);

    /// <summary>Abandons an admitted attempt before any command dispatch.</summary>
    /// <returns><see langword="true"/> when the active pre-dispatch lease was released.</returns>
    public bool TryAbandonBeforeDispatch(object owner)
        => _gate.TryAbandonBeforeDispatch(this, owner);

    /// <summary>Releases a dispatched attempt only after explicit terminal lifecycle evidence.</summary>
    /// <param name="terminalState">Explicit command lifecycle state.</param>
    /// <returns><see langword="true"/> when the active lease was released.</returns>
    public bool TryReleaseTerminal(object owner, TenantCommandLifecycleState terminalState)
        => _gate.TryReleaseTerminal(this, owner, terminalState);
}
