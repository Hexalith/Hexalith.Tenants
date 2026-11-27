using Microsoft.AspNetCore.Components.Authorization;

namespace Hexalith.Tenants.UI.Tests.Services.Configuration;

/// <summary>
/// Test <see cref="AuthenticationStateProvider"/> backed by a caller-supplied delegate.
/// </summary>
internal sealed class DelegateAuthenticationStateProvider(
    Func<Task<AuthenticationState>> getAuthenticationStateAsync) : AuthenticationStateProvider
{
    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => getAuthenticationStateAsync();
}
