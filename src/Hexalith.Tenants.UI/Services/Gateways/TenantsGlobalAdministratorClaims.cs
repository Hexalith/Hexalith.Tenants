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

        // Distinct values, not claim count: an OIDC pipeline that maps `sub` from both the id_token and the
        // userinfo response emits the claim twice with the same value, which is not ambiguous evidence.
        // Conflicting values still fail closed to Indeterminate.
        string[] subjects = identity.Claims
            .Where(static claim => string.Equals(claim.Type, "sub", StringComparison.Ordinal))
            .Select(static claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (subjects.Length != 1
            || string.IsNullOrWhiteSpace(subjects[0])
            || !string.Equals(subjects[0], subjects[0].Trim(), StringComparison.Ordinal)
            || subjects[0].Any(char.IsControl)
            || (requireCorroboration
                && (string.IsNullOrWhiteSpace(corroboratedSubject)
                    || !string.Equals(corroboratedSubject, subjects[0], StringComparison.Ordinal))))
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
            return TenantConfigurationPrincipalEvidence.NonAdministrator(subjects[0]);
        }

        bool? hasSystemScope = ResolveSystemScopeEvidence(identity);
        if (hasSystemScope is null)
        {
            return TenantConfigurationPrincipalEvidence.Indeterminate();
        }

        return hasSystemScope.Value
            ? TenantConfigurationPrincipalEvidence.GlobalAdministrator(subjects[0])
            : TenantConfigurationPrincipalEvidence.NonAdministrator(subjects[0]);
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

        // Scalar `role` / ClaimTypes.Role and collection `roles` must agree. JSON-array shapes that used to
        // fail closed via IsMalformedScalarRole are valid on `roles` through ResolveRoleCollection; routing
        // every role claim type through the same parser keeps IdP mappings that put a JSON array on `role`
        // working.
        return ResolveRoleCollection(claim.Value);
    }

    private static bool? ResolveRoleCollection(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Shape detection runs on the trimmed value. A JSON payload carrying leading whitespace previously fell
        // through to the delimiter split and yielded a *definite* NonAdministrator rather than Indeterminate,
        // so the same claim authorized or denied depending only on a leading space.
        string trimmed = value.Trim();
        string[] roles;
        if (trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                roles = JsonSerializer.Deserialize<string[]>(trimmed) ?? [];
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
            if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                return null;
            }

            // Split the normalized value, and treat every whitespace character as a separator rather than only
            // the space. "global-admin\ttenant-reader" previously tokenized as one unmatchable value.
            roles = trimmed.Split(
                [' ', ',', '\t', '\n', '\r'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (roles.Length == 0)
            {
                return null;
            }
        }

        return roles.Any(IsGlobalAdministratorValue);
    }

    private static bool IsRelevantClaimType(string type)
        => string.Equals(type, "sub", StringComparison.Ordinal)
        || string.Equals(type, "eventstore:tenant", StringComparison.Ordinal)
        || IsAdministratorClaimType(type);

    private static bool IsAdministratorClaimType(string type)
        => type is "global_admin" or "is_global_admin" or "role" or "roles"
        || string.Equals(type, ClaimTypes.Role, StringComparison.Ordinal);

    // Compared on the TRIMMED value, in every branch. Shape detection was normalized but tokenization and
    // comparison were not, so a `roles` claim of "\tglobal-admin" split into a single token that matched
    // nothing and produced a *definite* NonAdministrator -- which renders the terminal MissingPermission
    // surface, the one restricted state that offers no Retry -- while " global-admin" authorized, purely
    // because a space happens to be a split delimiter and a tab does not. The same claim must not authorize or
    // deny on which whitespace character precedes it, and scalar `role` must agree with collection `roles`:
    // ResolveClaim evaluates claim.Value directly, so normalizing here is what makes the two paths consistent.
    private static bool IsGlobalAdministratorValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        ReadOnlySpan<char> candidate = value.AsSpan().Trim();
        return candidate.Equals("GlobalAdministrator", StringComparison.OrdinalIgnoreCase)
            || candidate.Equals("global-administrator", StringComparison.OrdinalIgnoreCase)
            || candidate.Equals("global-admin", StringComparison.OrdinalIgnoreCase);
    }
}
