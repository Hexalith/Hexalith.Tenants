namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>Identifies a supported tenant lifecycle operation.</summary>
public enum TenantLifecycleOperation
{
    /// <summary>Enable a disabled tenant.</summary>
    EnableTenant,

    /// <summary>Disable an active tenant.</summary>
    DisableTenant,
}
