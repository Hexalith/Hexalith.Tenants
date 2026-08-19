using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.Tenants.UI.Services.Gateways;

using Microsoft.AspNetCore.Components.Authorization;

namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Resolves tenant configuration evidence from the authoritative SSR request or interactive-circuit identity.
/// </summary>
internal sealed class TenantConfigurationPrincipalResolver(
    CircuitServicesAccessor circuitServicesAccessor,
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

        System.Security.Claims.ClaimsPrincipal principal;
        if (circuitServices is null
            && provider is null
            && httpContextAccessor?.HttpContext is { } httpContext)
        {
            // Static SSR has no circuit provider. HttpContext.User is the authoritative request identity, and
            // all subject, scope, and administrator evidence is derived from that one principal below.
            principal = httpContext.User;
        }
        else
        {
            if (circuitServices is not null && provider is null)
            {
                // An inbound circuit activity was published, but it did not carry its authoritative
                // AuthenticationStateProvider. Do not borrow either request or constructor-injected state.
                return TenantConfigurationPrincipalEvidence.Indeterminate();
            }

            // The scoped fallback covers circuit-owned work that runs outside an inbound activity, such as a
            // projection notification or authentication-state transition. It is never used while an SSR
            // request is in scope.
            provider ??= authenticationStateProvider;
            if (provider is null)
            {
                return TenantConfigurationPrincipalEvidence.Indeterminate();
            }

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
        }

        return TenantsGlobalAdministratorClaims.ResolvePrincipalEvidence(
            principal,
            corroboratedSubject: null,
            requireCorroboration: false);
    }
}
