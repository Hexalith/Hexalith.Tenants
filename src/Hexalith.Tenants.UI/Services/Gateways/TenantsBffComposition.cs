using System.Security.Claims;
using System.Text.Json;

using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantsBffComposition(
    ITenantCommandGateway commandGateway,
    IHttpContextAccessor? httpContextAccessor = null) : ITenantsBffComposition
{
    public bool IsReadSurfaceConnected => true;

    public bool IsCommandSurfaceConnected => commandGateway is not UnavailableTenantCommandGateway;

    public TenantLifecycleAuthorizationReflectionState LifecycleAuthorizationReflection
        => IsCommandSurfaceConnected && IsServerReflectedGlobalAdministrator(httpContextAccessor?.HttpContext?.User)
            ? TenantLifecycleAuthorizationReflectionState.Authorized
            : TenantLifecycleAuthorizationReflectionState.Indeterminate;

    private static bool IsServerReflectedGlobalAdministrator(ClaimsPrincipal? principal)
        => principal?.Identity?.IsAuthenticated == true
        && principal.HasClaim(static claim =>
            string.Equals(claim.Type, "eventstore:tenant", StringComparison.Ordinal)
            && string.Equals(claim.Value, "system", StringComparison.Ordinal))
        && principal.Claims.Any(IsGlobalAdministratorClaim);

    private static bool IsGlobalAdministratorClaim(Claim claim)
    {
        if (claim.Type is "global_admin" or "is_global_admin")
        {
            return bool.TryParse(claim.Value, out bool isGlobalAdmin) && isGlobalAdmin;
        }

        if (claim.Type is ClaimTypes.Role or "role")
        {
            return IsGlobalAdministratorValue(claim.Value);
        }

        return string.Equals(claim.Type, "roles", StringComparison.Ordinal)
            && ClaimValueContainsGlobalAdministrator(claim.Value);
    }

    private static bool ClaimValueContainsGlobalAdministrator(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.StartsWith('['))
        {
            try
            {
                string[]? roles = JsonSerializer.Deserialize<string[]>(value);
                if (roles is not null)
                {
                    return roles.Any(IsGlobalAdministratorValue);
                }
            }
            catch (JsonException)
            {
                // Fall through to delimiter-based parsing below.
            }
        }

        return value
            .Split([' ', ','], StringSplitOptions.RemoveEmptyEntries)
            .Any(IsGlobalAdministratorValue);
    }

    private static bool IsGlobalAdministratorValue(string? value)
        => !string.IsNullOrWhiteSpace(value)
        && (string.Equals(value, "GlobalAdministrator", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "global-administrator", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "global-admin", StringComparison.OrdinalIgnoreCase));
}
