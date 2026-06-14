
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Hexalith.Tenants.UI.Services.Auth;
/// <summary>
/// Process-wide store of per-user EventStore access tokens captured at OIDC sign-in. Keyed by the
/// authenticated user's stable identifier (the <c>sub</c>/NameIdentifier claim). It lets the Blazor
/// Server circuit — which has no <see cref="HttpContext"/> — relay the signed-in user's bearer token
/// to the EventStore gateway. Tokens are overwritten on each sign-in and removed on sign-out.
/// </summary>
public sealed class TenantsUserTokenStore {
    private readonly ConcurrentDictionary<string, string> _tokens = new(StringComparer.Ordinal);

    public void Set(string userId, string accessToken) => _tokens[userId] = accessToken;

    public bool TryGet(string userId, out string accessToken) => _tokens.TryGetValue(userId, out accessToken!);

    public void Remove(string userId) => _tokens.TryRemove(userId, out _);
}

/// <summary>
/// Holds the current Blazor circuit's service provider in an <see cref="AsyncLocal{T}"/> so pooled
/// infrastructure (such as <see cref="DelegatingHandler"/> instances created by
/// <see cref="IHttpClientFactory"/>) can resolve circuit-scoped services while an inbound circuit
/// activity is executing. Registered as a singleton; the value is published per inbound activity by
/// <see cref="TenantsCircuitServicesHandler"/>.
/// </summary>
public sealed class CircuitServicesAccessor {
    private static readonly AsyncLocal<IServiceProvider?> Current = new();

    public IServiceProvider? Services {
        get => Current.Value;
        set => Current.Value = value;
    }
}

/// <summary>
/// Publishes the circuit's scoped <see cref="IServiceProvider"/> into <see cref="CircuitServicesAccessor"/>
/// for the duration of each inbound circuit activity, enabling outbound HTTP handlers to read
/// circuit-scoped services (for example <see cref="AuthenticationStateProvider"/>).
/// </summary>
public sealed class TenantsCircuitServicesHandler(
    IServiceProvider circuitServices,
    CircuitServicesAccessor accessor) : CircuitHandler {
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
        => async context => {
            accessor.Services = circuitServices;
            try {
                await next(context).ConfigureAwait(false);
            }
            finally {
                accessor.Services = null;
            }
        };
}

/// <summary>
/// Attaches the signed-in user's EventStore access token as a bearer header on gateway requests.
/// Resolves the current principal from <see cref="IHttpContextAccessor"/> during server-side render
/// and from the circuit's <see cref="AuthenticationStateProvider"/> (via
/// <see cref="CircuitServicesAccessor"/>) during interactive circuit activity. Anonymous requests are
/// left untouched, so the gateway returns 401 and the UI surfaces the sign-in state.
/// </summary>
public sealed class GatewayAuthorizationHandler(
    IHttpContextAccessor httpContextAccessor,
    CircuitServicesAccessor circuitServicesAccessor,
    TenantsUserTokenStore tokenStore) : DelegatingHandler {
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Headers.Authorization is null) {
            string? userId = await ResolveUserIdAsync().ConfigureAwait(false);
            if (userId is not null && tokenStore.TryGet(userId, out string token)) {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> ResolveUserIdAsync() {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) {
            var provider = circuitServicesAccessor.Services?.GetService(typeof(AuthenticationStateProvider)) as AuthenticationStateProvider;
            if (provider is not null) {
                AuthenticationState state = await provider.GetAuthenticationStateAsync().ConfigureAwait(false);
                user = state.User;
            }
        }

        return user?.Identity?.IsAuthenticated == true
            ? user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
    }
}

/// <summary>
/// Registration helpers for per-user EventStore token relay from the Tenants UI (Blazor Server).
/// </summary>
public static class TenantsTokenRelayExtensions {
    /// <summary>The FrontComposer OIDC challenge scheme name (see <c>FrontComposerOpenIdConnectOptions.ChallengeScheme</c>).</summary>
    public const string OidcScheme = "Hexalith.FrontComposer.Oidc";

    /// <summary>
    /// Registers the circuit-safe token relay services and captures the user's EventStore access
    /// token on each OIDC sign-in. Call after <c>AddHexalithFrontComposerAuthentication</c>.
    /// </summary>
    public static IServiceCollection AddTenantsTokenRelay(this IServiceCollection services) {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddSingleton<TenantsUserTokenStore>();
        _ = services.AddSingleton<CircuitServicesAccessor>();
        _ = services.AddScoped<CircuitHandler, TenantsCircuitServicesHandler>();
        _ = services.AddTransient<GatewayAuthorizationHandler>();

        // Local-dev OIDC against http Keycloak: allow metadata over http, keep tokens for relay, and
        // capture the access token into the per-user store when the authorization code is validated.
        _ = services.AddOptions<OpenIdConnectOptions>(OidcScheme)
            .Configure<TenantsUserTokenStore>((options, tokenStore) => {
                options.RequireHttpsMetadata = false;
                options.SaveTokens = true;

                Func<TokenValidatedContext, Task>? previous = options.Events.OnTokenValidated;
                options.Events.OnTokenValidated = async context => {
                    if (previous is not null) {
                        await previous(context).ConfigureAwait(false);
                    }

                    string? token = context.TokenEndpointResponse?.AccessToken;
                    string? userId = context.Principal?.FindFirstValue("sub")
                        ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (!string.IsNullOrEmpty(token) && userId is not null) {
                        tokenStore.Set(userId, token);
                    }
                };
            });

        return services;
    }

    /// <summary>Adds the bearer-token relay handler to an EventStore gateway HTTP client.</summary>
    public static IHttpClientBuilder AddGatewayAuthorization(this IHttpClientBuilder builder) {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddHttpMessageHandler<GatewayAuthorizationHandler>();
    }
}
