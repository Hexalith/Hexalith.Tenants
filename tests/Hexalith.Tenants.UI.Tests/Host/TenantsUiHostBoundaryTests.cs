using System.Net;
using System.Security.Claims;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.Tenants.UI.Services.Gateways;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Host;

/// <summary>Exercises the production host's rendered language and authentication scope boundary.</summary>
public sealed class TenantsUiHostBoundaryTests
{
    /// <summary>Requires a real method error to retain its status without a misleading missing-page body.</summary>
    [Fact]
    public async Task UnsupportedHttpMethodRetainsItsErrorWithoutNotFoundRecovery()
    {
        await using WebApplicationFactory<global::Program> baseFactory = new();
        using WebApplicationFactory<global::Program> factory = ConfigureHost(baseFactory, authenticated: false);
        using HttpClient client = CreateClient(factory);

        using HttpResponseMessage response = await client.DeleteAsync("/tenants/my", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldNotContain("tenants-route-not-found");
        body.ShouldNotContain("fc-scope-blocked");
    }

    /// <summary>Checks localized HTML from the actual request pipeline, including unsupported-culture fallback.</summary>
    /// <param name="requestedCulture">The requested UI culture.</param>
    /// <param name="requestedUiCulture">The UI culture, independently of formatting culture.</param>
    /// <param name="expectedLanguage">The supported document language.</param>
    /// <param name="expectedHeading">The localized scope-denial heading.</param>
    [Theory]
    [InlineData("en", "en", "en", "Workspace unavailable")]
    [InlineData("fr", "fr", "fr", "Espace de travail indisponible")]
    [InlineData("de", "de", "en", "Workspace unavailable")]
    [InlineData("en", "fr", "fr", "Espace de travail indisponible")]
    public async Task DocumentLanguageFollowsSupportedRequestCulture(
        string requestedCulture,
        string requestedUiCulture,
        string expectedLanguage,
        string expectedHeading)
    {
        await using WebApplicationFactory<global::Program> baseFactory = new();
        using WebApplicationFactory<global::Program> factory = ConfigureHost(baseFactory, authenticated: false);
        using HttpClient client = CreateClient(factory);

        using HttpResponseMessage response = await client.GetAsync(
            $"/tenants?culture={requestedCulture}&ui-culture={requestedUiCulture}",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        IDocument document = new HtmlParser().ParseDocument(await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken));
        document.DocumentElement!.GetAttribute("lang").ShouldBe(expectedLanguage);
        document.QuerySelector("[data-testid='fc-scope-blocked'] h1")!.TextContent.ShouldBe(expectedHeading);
    }

    /// <summary>Requires unknown HTTP destinations, including intercepted-link fallbacks, to recover safely.</summary>
    /// <param name="culture">The supported browser language.</param>
    /// <param name="signedIn">Whether the request has a validated current tenant.</param>
    /// <param name="heading">The localized page heading when scope is valid.</param>
    [Theory]
    [InlineData("en", true, "Page not found")]
    [InlineData("fr", true, "Page introuvable")]
    [InlineData("en", false, "Workspace unavailable")]
    [InlineData("fr", false, "Espace de travail indisponible")]
    public async Task UnknownHttpRoutesRenderLocalizedRecoveryOrScopeDenial(
        string culture,
        bool signedIn,
        string heading)
    {
        await using WebApplicationFactory<global::Program> baseFactory = new();
        using WebApplicationFactory<global::Program> factory = ConfigureHost(baseFactory, authenticated: true);
        using HttpClient client = CreateClient(factory);
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);
        if (signedIn)
        {
            await AddHostCookieAsync(factory, client,
            [
                new("sub", "operator.host-01"),
                new("eventstore:tenant", "tenant.alpha"),
                new("eventstore:tenant", "tenant.beta"),
                new("eventstore:current-tenant", "tenant.alpha"),
            ]);
        }

        using HttpResponseMessage response = await client.GetAsync(
            "/missing/private/path?returnUrl=https%3A%2F%2Fevil.invalid%2F&access_token=private-token",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        IDocument document = new HtmlParser().ParseDocument(await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken));
        document.DocumentElement!.GetAttribute("lang").ShouldBe(culture);
        document.QuerySelector("#fc-main-content h1")!.TextContent.Trim().ShouldBe(heading);
        document.Body!.TextContent.ShouldNotContain("private-token");
        document.Body.TextContent.ShouldNotContain("evil.invalid");
        if (signedIn)
        {
            document.QuerySelector("[data-testid='tenants-route-recovery']")!.GetAttribute("href").ShouldBe("/home");
            document.QuerySelector("[data-testid='fc-scope-blocked']").ShouldBeNull();
        }
        else
        {
            document.QuerySelector("[data-testid='fc-scope-blocked']").ShouldNotBeNull();
            document.QuerySelector("[data-testid='tenants-route-not-found']").ShouldBeNull();
        }
    }

    /// <summary>Requires each anonymous canonical route to deny scope before resolving domain readers.</summary>
    /// <param name="path">The canonical route and optional caller-supplied context.</param>
    /// <param name="authenticatedHost">Whether the production host composes its OIDC recipe.</param>
    [Theory]
    [InlineData("/tenants", false)]
    [InlineData("/tenants/tenant.host-private", false)]
    [InlineData("/tenants/tenant.host-private/audit?targetUserId=operator.host-private", false)]
    [InlineData("/tenants/my", false)]
    [InlineData("/tenants/users?userId=operator.host-private", false)]
    [InlineData("/global-administrators", false)]
    [InlineData("/tenants", true)]
    [InlineData("/tenants/tenant.host-private", true)]
    [InlineData("/tenants/tenant.host-private/audit?targetUserId=operator.host-private", true)]
    [InlineData("/tenants/my", true)]
    [InlineData("/tenants/users?userId=operator.host-private", true)]
    [InlineData("/global-administrators", true)]
    public async Task AnonymousRoutesRenderScopeDenialBeforeDomainContentOrReads(string path, bool authenticatedHost)
    {
        int gatewayResolutions = 0;
        await using WebApplicationFactory<global::Program> baseFactory = new();
        using WebApplicationFactory<global::Program> factory = ConfigureHost(
            baseFactory,
            authenticated: authenticatedHost,
            services =>
            {
                // Supply discovery metadata only; the production cookie/OIDC handlers remain installed.
                services.PostConfigureAll<OpenIdConnectOptions>(options =>
                {
                    options.Configuration = new OpenIdConnectConfiguration
                    {
                        Issuer = "https://keycloak.invalid/realms/hexalith",
                        AuthorizationEndpoint = "https://keycloak.invalid/realms/hexalith/protocol/openid-connect/auth",
                    };
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(options.Configuration);
                });
                services.AddScoped<ITenantQueryGateway>(_ =>
                {
                    Interlocked.Increment(ref gatewayResolutions);
                    return new UnavailableTenantQueryGateway();
                });
            });
        using HttpClient client = CreateClient(factory);

        using HttpResponseMessage response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Volatile.Read(ref gatewayResolutions).ShouldBe(0);
        if (authenticatedHost && path == "/global-administrators")
        {
            response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
            response.Headers.Location!.Host.ShouldBe("keycloak.invalid");
            response.Headers.Location.AbsolutePath.ShouldBe("/realms/hexalith/protocol/openid-connect/auth");
            return;
        }
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        IDocument document = new HtmlParser().ParseDocument(await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken));
        document.QuerySelector("[data-testid='fc-scope-blocked']")!.TextContent
            .ShouldContain("Your workspace identity could not be verified");
        foreach (string marker in new[]
        {
            "tenants-workspace", "tenants-detail", "tenants-audit-surface", "tenants-my-page",
            "tenants-user-lookup", "tenants-global-admins-area", "tenants-audit-row", "tenants-my-row",
        })
        {
            document.QuerySelector($"[data-testid='{marker}']").ShouldBeNull();
        }
        document.Body!.TextContent.ShouldNotContain("tenant.host-private");
        document.Body.TextContent.ShouldNotContain("operator.host-private");
        Volatile.Read(ref gatewayResolutions).ShouldBe(0);
    }

    /// <summary>Exercises real cookie authentication and the host's current-tenant claim mapping.</summary>
    /// <param name="currentTenant">The current-tenant claim, or missing evidence.</param>
    /// <param name="conflictingAlias">Whether a second, conflicting current-tenant claim is present.</param>
    /// <param name="subject">The subject claim, or missing evidence.</param>
    /// <param name="conflictingSubject">Whether a second, conflicting subject claim is present.</param>
    /// <param name="expectedScope">Whether the request may render scoped domain content.</param>
    [Theory]
    [InlineData("tenant.alpha", false, "operator.host-01", false, true)]
    [InlineData("tenant.beta", false, "operator.host-01", false, true)]
    [InlineData(null, false, "operator.host-01", false, false)]
    [InlineData("tenant.alpha", true, "operator.host-01", false, false)]
    [InlineData("tenant.alpha", false, null, false, false)]
    [InlineData("tenant.alpha", false, "operator.host-01", true, false)]
    public async Task AuthenticatedHostUsesOnlyUnambiguousCurrentTenantAlias(
        string? currentTenant,
        bool conflictingAlias,
        string? subject,
        bool conflictingSubject,
        bool expectedScope)
    {
        string? observedTenant = null;
        string? observedUser = null;
        Type? accessorType = null;
        int gatewayResolutions = 0;
        await using WebApplicationFactory<global::Program> baseFactory = new();
        using WebApplicationFactory<global::Program> factory = ConfigureHost(
            baseFactory,
            authenticated: true,
            services => services.AddScoped<ITenantQueryGateway>(provider =>
            {
                IUserContextAccessor accessor = provider.GetRequiredService<IUserContextAccessor>();
                accessorType = accessor.GetType();
                observedTenant = accessor.TenantId;
                observedUser = accessor.UserId;
                Interlocked.Increment(ref gatewayResolutions);
                return new UnavailableTenantQueryGateway();
            }));
        using HttpClient client = CreateClient(factory);
        List<Claim> claims =
        [
            new("eventstore:tenant", "tenant.alpha"),
            new("eventstore:tenant", "tenant.beta"),
        ];
        if (subject is not null)
        {
            claims.Add(new("sub", subject));
        }
        if (conflictingSubject)
        {
            claims.Add(new("sub", "operator.host-02"));
        }
        if (currentTenant is not null)
        {
            claims.Add(new("eventstore:current-tenant", currentTenant));
        }
        if (conflictingAlias)
        {
            claims.Add(new("eventstore:current-tenant", "tenant.beta"));
        }
        await AddHostCookieAsync(factory, client, claims);

        using HttpResponseMessage response = await client.GetAsync("/tenants/my", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        IDocument document = new HtmlParser().ParseDocument(await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken));
        if (expectedScope)
        {
            document.QuerySelector("[data-testid='fc-scope-blocked']").ShouldBeNull();
            document.QuerySelector("[data-testid='tenants-my-page-header']").ShouldNotBeNull();
            Volatile.Read(ref gatewayResolutions).ShouldBeGreaterThan(0);
            accessorType.ShouldBe(typeof(ServerCircuitUserContextAccessor));
            observedTenant.ShouldBe(currentTenant);
            observedUser.ShouldBe("operator.host-01");
        }
        else
        {
            document.QuerySelector("[data-testid='fc-scope-blocked']").ShouldNotBeNull();
            document.QuerySelector("[data-testid='tenants-my-page-header']").ShouldBeNull();
            Volatile.Read(ref gatewayResolutions).ShouldBe(0);
        }
    }

    private static WebApplicationFactory<global::Program> ConfigureHost(
        WebApplicationFactory<global::Program> factory,
        bool authenticated,
        Action<IServiceCollection>? configure = null)
        => factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Authentication:OpenIdConnect:Authority",
                authenticated ? "https://keycloak.invalid/realms/hexalith" : string.Empty);
            builder.UseSetting("Authentication:OpenIdConnect:ClientId", authenticated ? "host-tests" : string.Empty);
            builder.UseSetting("Authentication:OpenIdConnect:ClientSecret", authenticated ? "test-only-secret" : string.Empty);
            builder.ConfigureTestServices(services =>
            {
                if (configure is not null)
                {
                    services.RemoveAll<ITenantQueryGateway>();
                    configure(services);
                }
            });
        });

    private static HttpClient CreateClient(WebApplicationFactory<global::Program> factory)
        => factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

    private static async Task AddHostCookieAsync(
        WebApplicationFactory<global::Program> factory,
        HttpClient client,
        IEnumerable<Claim> claims)
    {
        AuthenticationScheme scheme = (await factory.Services.GetRequiredService<IAuthenticationSchemeProvider>()
            .GetDefaultAuthenticateSchemeAsync())!;
        CookieAuthenticationOptions cookie = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(scheme.Name);
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, scheme.Name, "sub", ClaimTypes.Role));
        AuthenticationTicket ticket = new(principal, new AuthenticationProperties
        {
            IssuedUtc = DateTimeOffset.UtcNow,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(5),
        }, scheme.Name);
        client.DefaultRequestHeaders.Add("Cookie", $"{cookie.Cookie.Name}={cookie.TicketDataFormat.Protect(ticket)}");
    }
}
