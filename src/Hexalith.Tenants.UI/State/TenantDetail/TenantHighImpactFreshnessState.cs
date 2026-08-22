namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Represents authoritative projection freshness plus the UI-only refreshing state.
/// </summary>
public enum TenantHighImpactFreshnessState
{
    /// <summary>Freshness cannot be measured.</summary>
    Unknown,

    /// <summary>The authoritative projection is current.</summary>
    Current,

    /// <summary>A refresh is in progress while last-confirmed data remains visible.</summary>
    Refreshing,

    /// <summary>The authoritative projection is aging but remains usable with friction.</summary>
    Aging,

    /// <summary>The authoritative projection is stale.</summary>
    Stale,
}
