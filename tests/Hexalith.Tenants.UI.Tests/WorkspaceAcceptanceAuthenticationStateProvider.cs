using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace Hexalith.Tenants.UI.Tests;

/// <summary>Retains the actual current provider task across authentication events and explicit reads.</summary>
internal sealed class WorkspaceAcceptanceAuthenticationStateProvider(Task<AuthenticationState> initial) : AuthenticationStateProvider
{
    private Task<AuthenticationState> _current = initial;

    /// <summary>Gets the number of explicit current-state reads.</summary>
    internal int ReadCount { get; private set; }

    /// <summary>Gets or sets an optional synchronous current-state acquisition failure.</summary>
    internal Exception? ReadFailure { get; set; }

    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        ReadCount++;
        if (ReadFailure is { } failure)
        {
            throw failure;
        }
        return _current;
    }

    /// <summary>Changes the current evidence without manufacturing a provider notification.</summary>
    /// <param name="current">The evidence returned by the next explicit read.</param>
    internal void SetCurrent(Task<AuthenticationState> current)
        => _current = current;

    /// <summary>Publishes the same task that subsequent explicit reads will return.</summary>
    /// <param name="current">The current authentication evidence.</param>
    internal void Notify(Task<AuthenticationState> current)
    {
        _current = current;
        NotifyAuthenticationStateChanged(current);
    }

    /// <summary>Publishes settled evidence for the current principal.</summary>
    /// <param name="principal">The current principal.</param>
    internal void Notify(ClaimsPrincipal principal)
        => Notify(Task.FromResult(new AuthenticationState(principal)));
}
