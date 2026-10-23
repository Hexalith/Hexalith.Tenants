using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace Hexalith.Tenants.UI.Tests.State;

/// <summary>Controls circuit caller resolution and notifications for correction ownership tests.</summary>
internal sealed class TenantCorrectionAuthenticationStateProvider : AuthenticationStateProvider
{
    private Task<AuthenticationState> _state = Task.FromResult(State("operator.alpha"));

    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _state;

    /// <summary>Publishes the exact supplied identity task.</summary>
    internal void Change(Task<AuthenticationState> state)
    {
        _state = state;
        NotifyAuthenticationStateChanged(state);
    }

    /// <summary>Creates an authenticated literal subject, or a signed-out principal.</summary>
    internal static AuthenticationState State(string? subject)
        => new(subject is null ? new ClaimsPrincipal() : new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("sub", subject)], "test")));
}
