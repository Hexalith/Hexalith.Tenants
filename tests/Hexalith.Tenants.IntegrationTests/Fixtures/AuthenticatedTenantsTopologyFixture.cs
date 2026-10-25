using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Uses the live topology with FrontComposer's production cookie handler enabled.</summary>
/// <remarks>Cookies use the host's real persisted key ring and contain a signed access token relayed to the live REST API.</remarks>
public sealed class AuthenticatedTenantsTopologyFixture : AspireTopologyFixture, IAsyncLifetime
{
    private const string Scheme = "Hexalith.FrontComposer.Cookie";
    private Process? _identityProvider;
    private Task<string>? _providerErrors;
    private Uri? _issuer;

    /// <summary>Gets the checkout containing the maintained identity-provider and browser fixtures.</summary>
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
    protected override IReadOnlyList<AspireResource> Resources
        => base.Resources.Select(static resource => resource.Name == "tenants-ui"
            ? resource with { EndpointName = "https" }
            : resource).ToArray();

    /// <inheritdoc/>
    public new async ValueTask InitializeAsync()
    {
        var start = new ProcessStartInfo("node")
        {
            WorkingDirectory = RepositoryRoot,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add("tests/Hexalith.Tenants.UI.Tests/Browser/live-route-oidc-provider.mjs");
        _identityProvider = Process.Start(start) ?? throw new InvalidOperationException("The loopback identity provider could not start.");
        _providerErrors = _identityProvider.StandardError.ReadToEndAsync();
        string? authority = await _identityProvider.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15));
        if (!Uri.TryCreate(authority, UriKind.Absolute, out Uri? issuer) || !issuer.IsLoopback || issuer.Scheme != "http")
        {
            await StopIdentityProviderAsync();
            throw new InvalidOperationException("The loopback identity provider did not publish its local authority.");
        }
        _issuer = issuer;

        // The host keeps its production OIDC validation and token-relay callbacks. The provider
        // signs ID tokens and relays a separately signed token validated by the real backend.
        Dictionary<string, string> settings = new(StringComparer.Ordinal)
        {
            ["Authentication__OpenIdConnect__Authority"] = issuer.AbsoluteUri,
            ["Authentication__OpenIdConnect__ClientId"] = "tenants-browser-fixture",
            ["Authentication__OpenIdConnect__ClientSecret"] = "loopback-test-provider-only",
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
        catch
        {
            await StopIdentityProviderAsync();
            throw;
        }
        finally
        {
            foreach (var setting in previous)
            {
                Environment.SetEnvironmentVariable(setting.Key, setting.Value);
            }
        }
    }

    /// <summary>Signs in through the production code exchange and retains only the encrypted cookie header.</summary>
    internal async Task<string> SignInAsync(CancellationToken cancellationToken)
    {
        Process provider = _identityProvider ?? throw new InvalidOperationException("The identity provider is not running.");
        await provider.StandardInput.WriteLineAsync(JsonSerializer.Serialize(AspireTopologyTests.CreateDemoJwt()).AsMemory(), cancellationToken);
        string? ready = await provider.StandardOutput.ReadLineAsync(cancellationToken);
        if (ready != "ready")
        {
            throw new InvalidOperationException("The loopback identity provider did not accept its backend token.");
        }

        var cookies = new CookieContainer();
        using var handler = new HttpClientHandler { CookieContainer = cookies, AllowAutoRedirect = false };
        using var client = new HttpClient(handler) { BaseAddress = TenantsUiClient.BaseAddress, Timeout = TimeSpan.FromSeconds(30) };
        Uri destination = new(client.BaseAddress!, "/authentication/challenge?returnUrl=%2Ftenants");
        bool signedIn = false;
        for (int hop = 0; hop < 8; hop++)
        {
            using HttpResponseMessage response = await client.GetAsync(destination, cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK && destination.Authority == client.BaseAddress!.Authority
                && hop >= 2)
            {
                signedIn = true;
                break;
            }

            if ((int)response.StatusCode is < 300 or >= 400 || response.Headers.Location is not { } location)
            {
                string endpoint = destination.AbsolutePath == "/signin-oidc" ? "callback"
                    : destination.AbsolutePath == "/authentication/challenge" ? "challenge"
                    : destination.Authority == _issuer!.Authority ? "provider"
                    : "return";
                throw new InvalidOperationException($"The production OIDC sign-in failed at {endpoint}, hop {hop}, HTTP {(int)response.StatusCode}.");
            }

            Uri next = location.IsAbsoluteUri ? location : new Uri(destination, location);
            // HttpClient deliberately does not auto-follow HTTPS-to-HTTP redirects. The only
            // HTTP destination allowed here is our already-declared loopback test provider.
            bool ui = next.Scheme == "https" && next.Authority == client.BaseAddress!.Authority;
            bool identity = next.Scheme == "http" && next.IsLoopback && next.Authority == _issuer!.Authority;
            if (!ui && !identity)
            {
                throw new InvalidOperationException("The OIDC fixture attempted to leave its declared local endpoints.");
            }

            destination = next;
        }

        if (!signedIn)
        {
            throw new InvalidOperationException("The production OIDC sign-in exceeded its redirect bound.");
        }

        string header = string.Join("; ", cookies.GetCookies(client.BaseAddress!)
            .Cast<Cookie>()
            .Where(static cookie => cookie.Name.StartsWith(".AspNetCore." + Scheme, StringComparison.Ordinal))
            .Select(static cookie => cookie.Name + "=" + cookie.Value));
        if (string.IsNullOrWhiteSpace(header))
        {
            throw new InvalidOperationException("The production OIDC handler did not issue its authentication cookie.");
        }

        return header;
    }

    /// <inheritdoc/>
    public new async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            await StopIdentityProviderAsync();
        }
    }

    private async Task StopIdentityProviderAsync()
    {
        if (_identityProvider is not { } provider)
        {
            return;
        }

        if (!provider.HasExited)
        {
            provider.StandardInput.Close();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await provider.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                provider.Kill(entireProcessTree: true);
                await provider.WaitForExitAsync();
            }
        }

        if (_providerErrors is not null)
        {
            await _providerErrors;
        }

        provider.Dispose();
        _identityProvider = null;
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
