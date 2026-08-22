namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Represents action-specific command lifecycle support.
/// </summary>
public enum TenantHighImpactSupportEvidence
{
    /// <summary>Support availability has not been determined.</summary>
    Unknown,

    /// <summary>The action support is known to be unavailable.</summary>
    Missing,

    /// <summary>The action support is connected and ready.</summary>
    Ready,
}
