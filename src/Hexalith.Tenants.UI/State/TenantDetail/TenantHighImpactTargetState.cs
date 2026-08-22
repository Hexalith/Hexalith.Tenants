namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Represents safe action-specific target evidence without carrying configuration values.
/// </summary>
public enum TenantHighImpactTargetState
{
    /// <summary>No target-domain outcome applies.</summary>
    NotApplicable,

    /// <summary>The target state is not yet known.</summary>
    Unknown,

    /// <summary>The target is authoritatively present.</summary>
    Present,

    /// <summary>The target is authoritatively missing.</summary>
    Missing,

    /// <summary>The requested configuration value is already reflected.</summary>
    AlreadyApplied,
}
