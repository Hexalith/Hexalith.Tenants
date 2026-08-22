namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Identifies server-reflected configuration mutation authority independently from namespace scope.
/// </summary>
public enum TenantConfigurationAuthorityState
{
    /// <summary>Principal or membership evidence could not be determined safely.</summary>
    Indeterminate,

    /// <summary>The principal is proven but is neither a tenant owner nor a global administrator.</summary>
    MissingPermission,

    /// <summary>The principal is authoritatively reflected as a tenant owner.</summary>
    TenantOwner,

    /// <summary>The principal is authoritatively reflected as a global administrator.</summary>
    GlobalAdministrator,
}
