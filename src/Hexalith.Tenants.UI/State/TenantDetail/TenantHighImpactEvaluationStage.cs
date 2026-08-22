namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Identifies whether availability is being evaluated for preview entry or command confirmation.
/// </summary>
public enum TenantHighImpactEvaluationStage
{
    /// <summary>The action launcher is being evaluated.</summary>
    PreviewEntry,

    /// <summary>The completed preview is being evaluated immediately before confirmation.</summary>
    Confirmation,
}
