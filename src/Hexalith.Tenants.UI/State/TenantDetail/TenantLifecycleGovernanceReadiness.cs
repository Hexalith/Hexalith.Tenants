namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>Represents compatibility admission readiness for lifecycle commands.</summary>
public enum TenantLifecycleGovernanceReadiness
{
    /// <summary>Admission readiness has not been resolved.</summary>
    Unresolved,

    /// <summary>Admission is currently available.</summary>
    Ready,

    /// <summary>Admission is currently blocked.</summary>
    Blocked,
}
