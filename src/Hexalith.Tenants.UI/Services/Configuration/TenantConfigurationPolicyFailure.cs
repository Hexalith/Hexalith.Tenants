namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Names why configuration read policy could not be resolved.
/// </summary>
/// <remarks>
/// This enum exists so an operator can tell a deployment mistake from an ordinary unauthenticated
/// read. It deliberately carries no tenant identifier, subject, prefix, key, or value, so emitting it
/// cannot disclose policy content.
/// </remarks>
internal enum TenantConfigurationPolicyFailure
{
    /// <summary>Policy resolved successfully.</summary>
    None = 0,

    /// <summary>Principal evidence was missing, malformed, cross-identity, or otherwise ambiguous.</summary>
    IndeterminatePrincipal = 1,

    /// <summary>The policy section was absent from configuration.</summary>
    MissingSection = 2,

    /// <summary>A collection-shaped member carried a scalar value.</summary>
    ScalarCollection = 3,

    /// <summary>The section could not be bound to the typed options shape.</summary>
    UnbindableSection = 4,

    /// <summary>A grant or display-safe declaration failed semantic validation.</summary>
    InvalidDeclaration = 5,
}
