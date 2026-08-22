namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Represents browser viewport evidence observed from FrontComposer.
/// </summary>
public enum TenantHighImpactViewportState
{
    /// <summary>No browser measurement has been observed.</summary>
    Unknown,

    /// <summary>The measured viewport preserves the complete safety context.</summary>
    Safe,

    /// <summary>The measured viewport cannot preserve the complete safety context.</summary>
    Unsafe,
}
