namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Identifies support-safe configuration projection proof outcomes.
/// </summary>
public enum TenantConfigurationProjectionProofKind
{
    /// <summary>Evidence was missing, unsafe, stale, degraded, or mismatched.</summary>
    Unavailable,

    /// <summary>Current projection evidence proves the requested set value.</summary>
    SetConfirmed,

    /// <summary>Current projection evidence does not yet prove the requested set value.</summary>
    SetNotConfirmed,

    /// <summary>Current projection evidence proves the requested key is absent.</summary>
    RemoveConfirmed,

    /// <summary>Current projection evidence still contains the requested key.</summary>
    RemoveNotConfirmed,
}
