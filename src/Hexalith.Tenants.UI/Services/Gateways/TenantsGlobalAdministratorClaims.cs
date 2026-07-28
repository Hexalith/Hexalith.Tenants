using System.Security.Claims;
using System.Text.Json;

using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal static class TenantsGlobalAdministratorClaims
{
    public static bool IsGlobalAdministrator(ClaimsPrincipal? principal)
        => Evaluate(principal) == TenantLifecycleAuthorizationReflectionState.Authorized;

    public static TenantLifecycleAuthorizationReflectionState Evaluate(ClaimsPrincipal? principal)
        => ToReflection(ResolvePrincipalEvidence(principal, corroboratedSubject: null, requireCorroboration: false));

    public static TenantConfigurationPrincipalEvidence ResolvePrincipalEvidence(
        ClaimsPrincipal? principal,
        string? corroboratedSubject,
        bool requireCorroboration)
    {
        if (principal is null)
        {
            return TenantConfigurationPrincipalEvidence.Indeterminate();
        }

        ClaimsIdentity[] authenticated = principal.Identities
            .Where(static identity => identity.IsAuthenticated)
            .ToArray();
        if (authenticated.Length != 1 || HasRelevantClaimsOnOtherIdentity(principal, authenticated.Single()))
        {
            return TenantConfigurationPrincipalEvidence.Indeterminate();
        }

        ClaimsIdentity identity = authenticated.Single();
        Claim[] subjects = identity.Claims
            .Where(static claim => string.Equals(claim.Type, "sub", StringComparison.Ordinal))
            .ToArray();
        if (subjects.Length != 1
            || string.IsNullOrWhiteSpace(subjects[0].Value)
            || !string.Equals(subjects[0].Value, subjects[0].Value.Trim(), StringComparison.Ordinal)
            || subjects[0].Value.Any(char.IsControl)
            || (requireCorroboration
                && (string.IsNullOrWhiteSpace(corroboratedSubject)
                    || !string.Equals(corroboratedSubject, subjects[0].Value, StringComparison.Ordinal))))
        {
            return TenantConfigurationPrincipalEvidence.Indeterminate();
        }

        bool? administrator = ResolveAdministratorEvidence(identity);
        if (administrator is null)
        {
            return TenantConfigurationPrincipalEvidence.Indeterminate();
        }

        if (!administrator.Value)
        {
            return TenantConfigurationPrincipalEvidence.NonAdministrator(subjects[0].Value);
        }

        bool? hasSystemScope = ResolveSystemScopeEvidence(identity);
        if (hasSystemScope is null)
        {
            return TenantConfigurationPrincipalEvidence.Indeterminate();
        }

        return hasSystemScope.Value
            ? TenantConfigurationPrincipalEvidence.GlobalAdministrator(subjects[0].Value)
            : TenantConfigurationPrincipalEvidence.NonAdministrator(subjects[0].Value);
    }

    private static TenantLifecycleAuthorizationReflectionState ToReflection(
        TenantConfigurationPrincipalEvidence evidence)
        => evidence.State switch
        {
            TenantConfigurationPrincipalEvidenceState.GlobalAdministrator
                => TenantLifecycleAuthorizationReflectionState.Authorized,
            TenantConfigurationPrincipalEvidenceState.NonAdministrator
                => TenantLifecycleAuthorizationReflectionState.MissingPermission,
            _ => TenantLifecycleAuthorizationReflectionState.Indeterminate,
        };

    private static bool HasRelevantClaimsOnOtherIdentity(ClaimsPrincipal principal, ClaimsIdentity authenticated)
        => principal.Identities
            .Where(identity => !ReferenceEquals(identity, authenticated))
            .SelectMany(static identity => identity.Claims)
            .Any(static claim => IsRelevantClaimType(claim.Type));

    private static bool? ResolveAdministratorEvidence(ClaimsIdentity identity)
    {
        bool administrator = false;
        bool explicitDenial = false;
        foreach (Claim claim in identity.Claims.Where(static claim => IsAdministratorClaimType(claim.Type)))
        {
            bool? evidence = ResolveClaim(claim);
            if (evidence is null)
            {
                return null;
            }

            if (claim.Type is "global_admin" or "is_global_admin" && !evidence.Value)
            {
                explicitDenial = true;
            }
            else
            {
                administrator |= evidence.Value;
            }
        }

        return administrator && explicitDenial
            ? null
            : administrator;
    }

    private static bool? ResolveSystemScopeEvidence(ClaimsIdentity identity)
    {
        string[] scopes = identity.Claims
            .Where(static claim => string.Equals(claim.Type, "eventstore:tenant", StringComparison.Ordinal))
            .Select(static claim => claim.Value)
            .ToArray();
        if (scopes.Any(static scope => string.IsNullOrWhiteSpace(scope) || scope.Any(char.IsWhiteSpace))
            || scopes.Distinct(StringComparer.Ordinal).Skip(1).Any())
        {
            return null;
        }

        return scopes.Any(static scope => string.Equals(scope, "system", StringComparison.Ordinal));
    }

    private static bool? ResolveClaim(Claim claim)
    {
        if (claim.Type is "global_admin" or "is_global_admin")
        {
            return bool.TryParse(claim.Value, out bool value)
                ? value
                : null;
        }

        if (claim.Type is ClaimTypes.Role or "role")
        {
            return IsMalformedScalarRole(claim.Value)
                ? null
                : IsGlobalAdministratorValue(claim.Value);
        }

        return ResolveRoleCollection(claim.Value);
    }

    private static bool? ResolveRoleCollection(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string[] roles;
        if (value.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                roles = JsonSerializer.Deserialize<string[]>(value) ?? [];
            }
            catch (JsonException)
            {
                return null;
            }

            if (roles.Length == 0 || roles.Any(string.IsNullOrWhiteSpace))
            {
                return null;
            }
        }
        else
        {
            if (value.StartsWith("{", StringComparison.Ordinal))
            {
                return null;
            }

            roles = value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
            if (roles.Length == 0)
            {
                return null;
            }
        }

        return roles.Any(IsGlobalAdministratorValue);
    }

    private static bool IsMalformedScalarRole(string value)
        => string.IsNullOrWhiteSpace(value)
        || value.StartsWith("[", StringComparison.Ordinal)
        || value.StartsWith("{", StringComparison.Ordinal);

    private static bool IsRelevantClaimType(string type)
        => string.Equals(type, "sub", StringComparison.Ordinal)
        || string.Equals(type, "eventstore:tenant", StringComparison.Ordinal)
        || IsAdministratorClaimType(type);

    private static bool IsAdministratorClaimType(string type)
        => type is "global_admin" or "is_global_admin" or "role" or "roles"
        || string.Equals(type, ClaimTypes.Role, StringComparison.Ordinal);

    private static bool IsGlobalAdministratorValue(string? value)
        => !string.IsNullOrWhiteSpace(value)
        && (string.Equals(value, "GlobalAdministrator", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "global-administrator", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "global-admin", StringComparison.OrdinalIgnoreCase));
}
