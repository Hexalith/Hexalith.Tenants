using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.Tenants.UI.Services.Gateways;

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
    /// <inheritdoc />
    public async ValueTask<TenantConfigurationPrincipalEvidence> ResolveAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        System.Security.Claims.ClaimsPrincipal? principal;
        if (circuitServicesAccessor.Services?.GetService(typeof(AuthenticationStateProvider))
            is AuthenticationStateProvider provider)
        {
            try
            {
                AuthenticationState state = await provider.GetAuthenticationStateAsync().ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                principal = state.User;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return TenantConfigurationPrincipalEvidence.Indeterminate();
            }
        }
        else
        {
            principal = httpContextAccessor.HttpContext?.User;
        }

        return TenantsGlobalAdministratorClaims.ResolvePrincipalEvidence(
            principal,
            userContextAccessor.UserId,
            requireCorroboration: true);
    }
}
