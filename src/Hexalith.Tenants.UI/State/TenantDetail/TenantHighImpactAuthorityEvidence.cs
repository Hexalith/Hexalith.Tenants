namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Represents server-reflected authority evidence for one high-impact action.
/// </summary>
public enum TenantHighImpactAuthorityEvidence
{
    /// <summary>Authority could not be determined safely.</summary>
    Indeterminate,

    /// <summary>The current principal is proven but lacks the required role.</summary>
    MissingPermission,

    /// <summary>The required role is authoritatively reflected.</summary>
    Authorized,
}
