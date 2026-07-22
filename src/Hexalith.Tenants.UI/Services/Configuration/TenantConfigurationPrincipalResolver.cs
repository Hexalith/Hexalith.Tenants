using System.Security.Claims;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Auth;

using Microsoft.AspNetCore.Components.Authorization;

namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Resolves tenant configuration evidence from one request or interactive-circuit identity.
/// </summary>
internal sealed class TenantConfigurationPrincipalResolver(
    IHttpContextAccessor httpContextAccessor,
    CircuitServicesAccessor circuitServicesAccessor,
    IUserContextAccessor userContextAccessor) : ITenantConfigurationPrincipalResolver
{
    private static readonly string[] AdministratorRoleValues =
    [
        "GlobalAdministrator",
        "global-administrator",
        "global-admin",
    ];

    /// <inheritdoc />
    public async ValueTask<TenantConfigurationPrincipalEvidence> ResolveAsync(
        CancellationToken cancellationToken = default)
    {
        ClaimsPrincipal? principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true
            && circuitServicesAccessor.Services?.GetService(typeof(AuthenticationStateProvider))
                is AuthenticationStateProvider provider)
        {
            AuthenticationState state = await provider.GetAuthenticationStateAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            principal = state.User;
        }

        return Resolve(principal);
    }

    private TenantConfigurationPrincipalEvidence Resolve(ClaimsPrincipal? principal)
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
        if (subjects.Length != 1 || string.IsNullOrWhiteSpace(subjects[0].Value))
        {
            return TenantConfigurationPrincipalEvidence.Indeterminate();
        }

        string? authenticatedSubject = userContextAccessor.UserId;
        if (string.IsNullOrWhiteSpace(authenticatedSubject)
            || !string.Equals(authenticatedSubject, subjects[0].Value, StringComparison.Ordinal))
        {
            return TenantConfigurationPrincipalEvidence.Indeterminate();
        }

        bool? administrator = ResolveAdministratorEvidence(identity);
        if (administrator is null)
        {
            return TenantConfigurationPrincipalEvidence.Indeterminate();
        }

        if (administrator.Value)
        {
            bool hasSystemScope = identity.Claims.Any(static claim =>
                string.Equals(claim.Type, "eventstore:tenant", StringComparison.Ordinal)
                && string.Equals(claim.Value, "system", StringComparison.Ordinal));
            if (!hasSystemScope)
            {
                return TenantConfigurationPrincipalEvidence.Indeterminate();
            }

            return TenantConfigurationPrincipalEvidence.GlobalAdministrator(authenticatedSubject);
        }

        return TenantConfigurationPrincipalEvidence.NonAdministrator(authenticatedSubject);
    }

    private static bool HasRelevantClaimsOnOtherIdentity(ClaimsPrincipal principal, ClaimsIdentity authenticated)
        => principal.Identities
            .Where(identity => !ReferenceEquals(identity, authenticated))
            .SelectMany(static identity => identity.Claims)
            .Any(static claim => IsRelevantClaimType(claim.Type));

    private static bool? ResolveAdministratorEvidence(ClaimsIdentity identity)
    {
        bool administrator = false;
        foreach (Claim claim in identity.Claims.Where(static claim => IsAdministratorClaimType(claim.Type)))
        {
            bool? evidence = ResolveClaim(claim);
            if (evidence is null)
            {
                return null;
            }

            administrator |= evidence.Value;
        }

        return administrator;
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
                : IsAdministratorRole(claim.Value);
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

        return roles.Any(IsAdministratorRole);
    }

    private static bool IsMalformedScalarRole(string value)
        => string.IsNullOrWhiteSpace(value)
        || value.StartsWith("[", StringComparison.Ordinal)
        || value.StartsWith("{", StringComparison.Ordinal);

    private static bool IsAdministratorRole(string value)
        => AdministratorRoleValues.Contains(value, StringComparer.OrdinalIgnoreCase);

    private static bool IsRelevantClaimType(string type)
        => string.Equals(type, "sub", StringComparison.Ordinal)
        || string.Equals(type, "eventstore:tenant", StringComparison.Ordinal)
        || IsAdministratorClaimType(type);

    private static bool IsAdministratorClaimType(string type)
        => type is "global_admin" or "is_global_admin" or "role" or "roles"
        || string.Equals(type, ClaimTypes.Role, StringComparison.Ordinal);

}
