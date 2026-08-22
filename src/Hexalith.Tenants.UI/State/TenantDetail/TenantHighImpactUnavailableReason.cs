namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Defines the canonical unavailable-action reason taxonomy.
/// </summary>
public enum TenantHighImpactUnavailableReason
{
    /// <summary>No unavailable reason applies.</summary>
    None,

    /// <summary>Authoritative permission or namespace-scope evidence does not qualify.</summary>
    MissingPermission,

    /// <summary>Authoritative read evidence is stale, unknown, or unusable.</summary>
    StaleData,

    /// <summary>The command lifecycle or action support is not available.</summary>
    MissingLifecycleSupport,

    /// <summary>The complete consequence preview is not available.</summary>
    MissingConsequencePreview,

    /// <summary>An action-declared mandatory proof path is not available.</summary>
    MissingAuditProof,

    /// <summary>The viewport or aggregate-admission safety gate does not qualify.</summary>
    HighImpactFlowNotReady,
}
