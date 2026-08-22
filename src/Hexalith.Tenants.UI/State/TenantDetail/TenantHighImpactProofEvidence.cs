namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Represents action-declared audit or proof readiness.
/// </summary>
public enum TenantHighImpactProofEvidence
{
    /// <summary>The consuming action explicitly declares that proof is not required for confirmation readiness.</summary>
    NotRequired,

    /// <summary>The declared mandatory proof path is missing.</summary>
    Missing,

    /// <summary>The declared mandatory proof path is ready.</summary>
    Ready,
}
