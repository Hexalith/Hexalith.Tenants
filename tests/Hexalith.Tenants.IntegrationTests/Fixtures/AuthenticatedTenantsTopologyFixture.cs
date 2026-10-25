using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Uses the live topology with FrontComposer's production cookie handler enabled.</summary>
/// <remarks>Cookies use the host's real persisted key ring and contain a signed access token relayed to the live REST API.</remarks>
public sealed class AuthenticatedTenantsTopologyFixture : AspireTopologyFixture, IAsyncLifetime
{
    private const string Scheme = "Hexalith.FrontComposer.Cookie";

    internal static string RepositoryRoot
    {
        get
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Hexalith.Tenants.slnx")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("The live route validator requires the repository checkout.");
        }
    }

    /// <inheritdoc/>
    public new async ValueTask InitializeAsync()
    {
        // Aspire children inherit these standard host configuration values. No production auth or
        // gateway service is replaced. A cookie is supplied directly, so discovery is never invoked.
        Dictionary<string, string> settings = new(StringComparer.Ordinal)
        {
            ["Authentication__OpenIdConnect__Authority"] = "https://live-cookie-fixture.invalid/realms/hexalith",
            ["Authentication__OpenIdConnect__ClientId"] = "tenants-live-cookie-fixture",
            ["Authentication__OpenIdConnect__ClientSecret"] = "fixture-only-no-provider-request",
        };
        Dictionary<string, string?> previous = settings.Keys.ToDictionary(static key => key, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var setting in settings)
            {
                Environment.SetEnvironmentVariable(setting.Key, setting.Value);
            }

            await base.InitializeAsync();
        }
        finally
        {
            foreach (var setting in previous)
            {
                Environment.SetEnvironmentVariable(setting.Key, setting.Value);
            }
        }
    }

    /// <summary>Protects a real cookie ticket for the current host, including its signed relayed token.</summary>
    internal static string CreateCookie(string token, bool ambiguousTenant = false)
    {
        string keyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspnet", "DataProtection-Keys");
        string uiRoot = Path.Combine(RepositoryRoot, "src", "Hexalith.Tenants.UI") + Path.DirectorySeparatorChar;
        IDataProtectionProvider provider = DataProtectionProvider.Create(new DirectoryInfo(keyPath), options => options.SetApplicationName(uiRoot));
        var format = new TicketDataFormat(provider.CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware", Scheme, "v2"));
        List<Claim> claims =
        [
            new("sub", AuthenticatedRouteState.CallerId),
            new("eventstore:current-tenant", "system"),
            new("eventstore:tenant", "system"),
            new("global_admin", "true"),
        ];
        if (ambiguousTenant)
        {
            claims.Add(new("eventstore:current-tenant", "ambiguous-other-tenant"));
        }

        var properties = new AuthenticationProperties
        {
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(15),
        };
        properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = token }]);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme, "sub", ClaimTypes.Role)), properties, Scheme);
        return $".AspNetCore.{Scheme}={format.Protect(ticket)}";
    }
}
