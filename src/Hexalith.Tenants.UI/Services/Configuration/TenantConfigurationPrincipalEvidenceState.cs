namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Identifies whether tenant configuration administrator evidence is proven.
/// </summary>
internal enum TenantConfigurationPrincipalEvidenceState
{
    /// <summary>Principal evidence is missing, malformed, or ambiguous.</summary>
    Indeterminate,

    /// <summary>One authenticated subject is proven without administrator evidence.</summary>
    NonAdministrator,

    /// <summary>One authenticated subject is proven to be a system-scoped global administrator.</summary>
    GlobalAdministrator,
}
