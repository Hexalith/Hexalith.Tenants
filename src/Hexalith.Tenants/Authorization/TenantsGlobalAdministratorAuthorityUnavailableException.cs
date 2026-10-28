namespace Hexalith.Tenants.Authorization;

/// <summary>Indicates that global authority cannot be established from current authenticated source evidence.</summary>
public sealed class TenantsGlobalAdministratorAuthorityUnavailableException : InvalidOperationException
{
    /// <summary>Initializes a support-safe authority failure without source or principal material.</summary>
    public TenantsGlobalAdministratorAuthorityUnavailableException()
        : base("Global administrator source authority is unavailable.") { }
}
