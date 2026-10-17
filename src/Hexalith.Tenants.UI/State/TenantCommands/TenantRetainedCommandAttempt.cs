namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Retains one complete domain command attempt independently of its rendering surface.</summary>
internal sealed class TenantRetainedCommandAttempt(
    string tenantId,
    object leaseOwner,
    TenantAggregateCommandLease lease,
    object surfaceOwner,
    object snapshot)
{
    /// <summary>Gets the literal aggregate identifier.</summary>
    internal string TenantId { get; } = tenantId;

    /// <summary>Gets the stable owner of this attempt's aggregate admission.</summary>
    internal object LeaseOwner { get; } = leaseOwner;

    /// <summary>Gets the exact aggregate admission.</summary>
    internal TenantAggregateCommandLease Lease { get; } = lease;

    /// <summary>Gets or sets the single attached rendering surface.</summary>
    internal object? SurfaceOwner { get; set; } = surfaceOwner;

    /// <summary>Gets or sets the complete typed reconciliation snapshot.</summary>
    internal object Snapshot { get; set; } = snapshot;

    /// <summary>Gets or sets the monotonic start of this membership attempt's one projection wait.</summary>
    internal long? MembershipProjectionStartedTimestamp { get; set; }

    /// <summary>Gets or sets the timer owned by this retained attempt, independently of its renderer.</summary>
    internal ITimer? MembershipProjectionTimer { get; set; }

    /// <summary>Gets or sets the exact evidence changed only by deadline expiry, for late reconciliation.</summary>
    internal object? MembershipProjectionExpirySource { get; set; }

    /// <summary>Gets or sets whether a delivery is awaiting its result.</summary>
    internal bool DeliveryInFlight { get; set; }

    /// <summary>Gets or sets failed removal evidence to restore if this exact retry is abandoned before delivery.</summary>
    internal TenantRetainedCommandAttempt? UndispatchedRemovalRetryBasis { get; set; }
}
