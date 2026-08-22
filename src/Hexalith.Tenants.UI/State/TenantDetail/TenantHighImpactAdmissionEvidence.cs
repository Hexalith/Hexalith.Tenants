namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Represents a non-mutating observation of aggregate command admission.
/// </summary>
public enum TenantHighImpactAdmissionEvidence
{
    /// <summary>Admission availability could not be observed.</summary>
    Unknown,

    /// <summary>Another action currently owns aggregate admission.</summary>
    Busy,

    /// <summary>The aggregate is currently free for an attempt to acquire admission later.</summary>
    Available,
}
