namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Represents safe consequence-preview readiness.
/// </summary>
public enum TenantHighImpactPreviewEvidence
{
    /// <summary>The consuming action explicitly declares that a preview is not required.</summary>
    NotRequired,

    /// <summary>One or more required safe preview facts are missing.</summary>
    Missing,

    /// <summary>Every required safe preview fact is ready.</summary>
    Ready,
}
