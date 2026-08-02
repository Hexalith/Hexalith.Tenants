using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.Tenants.UI.Services.Gateways;

using Microsoft.AspNetCore.Components.Authorization;

namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Resolves tenant configuration evidence from the authoritative interactive-circuit identity.
/// </summary>
internal sealed class TenantConfigurationPrincipalResolver(
    CircuitServicesAccessor circuitServicesAccessor,
    IUserContextAccessor userContextAccessor,
    AuthenticationStateProvider? authenticationStateProvider = null,
    IHttpContextAccessor? httpContextAccessor = null) : ITenantConfigurationPrincipalResolver
{
    /// <inheritdoc />
    public async ValueTask<TenantConfigurationPrincipalEvidence> ResolveAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // CircuitServicesAccessor.Services is an AsyncLocal published only for the duration of an inbound
        // circuit activity and nulled in the handler's finally. Depending on it alone meant every path that is
        // not an inbound activity -- projection-notification refreshes and authentication transitions above all
        // -- resolved to Indeterminate and permanently revoked an authorized operator. This resolver is
        // registered Scoped, so inside a circuit its own injected provider IS the circuit's provider; it is
        // used whenever the activity-scoped accessor is unavailable. The accessor keeps precedence so callers
        // that do run inside an inbound activity behave exactly as before.
        AuthenticationStateProvider? provider =
            circuitServicesAccessor.Services?.GetService(typeof(AuthenticationStateProvider))
                as AuthenticationStateProvider;

        // ...but the injected fallback is admitted only when no HTTP request is in scope. Routes render with
        // prerendering enabled, and on that static pass there is no circuit at all: the resolver's own scope is
        // the *request* scope, so its injected AuthenticationStateProvider is seeded from HttpContext.User --
        // precisely the evidence source the 2026-08-01 owner decision removed, re-entering through the loop-2
        // availability fix. An active HttpContext is the discriminator: it is present for the prerender and
        // static-SSR passes and null for circuit activity, notification threads and authentication transitions.
        // Prerender therefore stays Indeterminate and renders the restricted surface; the interactive instance
        // resolves for real once the circuit is connected.
        provider ??= httpContextAccessor?.HttpContext is null
            ? authenticationStateProvider
            : null;
        if (provider is null)
        {
            return TenantConfigurationPrincipalEvidence.Indeterminate();
        }

        System.Security.Claims.ClaimsPrincipal principal;
        try
        {
            AuthenticationState state = await provider
                .GetAuthenticationStateAsync()
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
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

        return TenantsGlobalAdministratorClaims.ResolvePrincipalEvidence(
            principal,
            userContextAccessor.UserId,
            requireCorroboration: true);
    }
}
