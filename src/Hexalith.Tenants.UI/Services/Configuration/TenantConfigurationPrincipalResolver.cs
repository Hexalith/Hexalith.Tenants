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

        // Circuit activity takes precedence over request state so an interactive operation cannot borrow a
        // principal from the HTTP request that originally established the circuit.
        IServiceProvider? circuitServices;
        AuthenticationStateProvider? provider;
        try
        {
            circuitServices = circuitServicesAccessor.Services;
            provider = circuitServices?.GetService(typeof(AuthenticationStateProvider))
                as AuthenticationStateProvider;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // A disposed circuit scope or a service-provider lookup fault is missing identity evidence,
            // never permission to fall through to another principal source.
            return TenantConfigurationPrincipalEvidence.Indeterminate();
        }

        if (circuitServices is not null && provider is null)
        {
            // An inbound circuit activity was published, but it did not carry its authoritative
            // AuthenticationStateProvider. Do not borrow either request or constructor-injected state.
            return TenantConfigurationPrincipalEvidence.Indeterminate();
        }

        // An active HttpContext is the discriminator that keeps HttpContext.User out of the evidence chain.
        // This resolver is registered Scoped, so on the prerender and static-SSR passes its own scope IS the
        // request scope and the injected AuthenticationStateProvider is seeded from HttpContext.User --
        // exactly the source the 2026-08-01 owner decision removed
        // (spec-1-11-authorized-global-administrator-review.md:75: "keep the current circuit-over-HTTP
        // precedence with no HttpContext.User fallback"), because a stale request principal must not retain
        // privilege after a live circuit authentication change. IHttpContextAccessor.HttpContext is
        // AsyncLocal, so the establishing request's context can also surface on circuit-owned work; refusing
        // whenever it is in scope is what closes that path. Prerender therefore stays Indeterminate and
        // renders the restricted surface; the interactive instance resolves for real once the circuit is up.
        // The availability cost of this rule is addressed by the cancellation and non-blocking read patches,
        // not by switching identity sources.
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

        // Corroborated: the principal's sub claim must agree with the independently resolved user context.
        // Claims alone are not sufficient evidence for global-administrator standing.
        return TenantsGlobalAdministratorClaims.ResolvePrincipalEvidence(
            principal,
            userContextAccessor.UserId,
            requireCorroboration: true);
    }
}
