namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Identifies a safe expected domain outcome independently from evidence blockers.
/// </summary>
public enum TenantHighImpactDomainOutcome
{
    /// <summary>No expected domain outcome applies.</summary>
    None,

    /// <summary>The requested lifecycle state is already reflected.</summary>
    LifecycleStateAlreadySet,

    /// <summary>The tenant is authoritatively disabled for configuration mutation.</summary>
    TenantDisabled,

    /// <summary>The requested configuration value is already reflected.</summary>
    ConfigurationAlreadyApplied,

    /// <summary>The authorized configuration removal target is authoritatively missing.</summary>
    ConfigurationKeyNotFound,
}
