namespace Hexalith.Tenants.Contracts.Identity;

/// <summary>Explicit public platform scopes for Tenants operations, separate from retained storage identities.</summary>
public static class TenantPlatformScopes
{
    /// <summary>Scope for cataloged tenant administration and cross-tenant reads.</summary>
    public const string Tenants = "platform:tenants";

    /// <summary>Scope for cataloged global administrator operations.</summary>
    public const string GlobalAdministrators = "platform:global-administrators";
}
