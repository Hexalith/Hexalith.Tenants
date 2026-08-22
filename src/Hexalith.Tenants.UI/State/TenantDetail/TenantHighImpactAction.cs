namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Identifies one high-impact tenant action evaluated by the shared availability kernel.
/// </summary>
public enum TenantHighImpactAction
{
    /// <summary>Enable a disabled tenant.</summary>
    EnableTenant,

    /// <summary>Disable an active tenant.</summary>
    DisableTenant,

    /// <summary>Set one namespaced configuration value.</summary>
    SetConfiguration,

    /// <summary>Remove one namespaced configuration key.</summary>
    RemoveConfiguration,
}
