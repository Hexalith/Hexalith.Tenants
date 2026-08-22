namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Represents ordinal namespace-scope evidence for one high-impact action.
/// </summary>
public enum TenantHighImpactNamespaceScopeEvidence
{
    /// <summary>The action does not consume configuration namespace scope.</summary>
    NotRequired,

    /// <summary>Namespace policy could not be determined safely.</summary>
    Indeterminate,

    /// <summary>Namespace policy is known and grants no matching scope.</summary>
    Missing,

    /// <summary>The literal key or action context is within proven ordinal scope.</summary>
    Authorized,
}
