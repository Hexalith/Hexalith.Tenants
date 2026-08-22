namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Represents command lifecycle support for a high-impact action. Current composition derives this
/// from the shared command-surface connectivity signal for all four actions; it is modeled per-action
/// so a future source that can isolate support to one action does not require a shape change.
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
