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
