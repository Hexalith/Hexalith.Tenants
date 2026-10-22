using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Components.Home;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Components.Pages;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Testing;
using Hexalith.Tenants.UI.Components;
using Hexalith.Tenants.UI.Composition;
using Hexalith.Tenants.UI.Extensions;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Exercises navigation on the mounted production router and framework shell.</summary>
public sealed class TenantsRouterTests : BunitContext
{
    /// <summary>Composes the production shell and domain module with isolated storage and a valid test scope.</summary>
    public TenantsRouterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        MarkupSanitizedOptions.ThrowOnUnsafe = false;
        Services.AddLogging();
        Services.AddFluentUIComponents();
        Services.AddHexalithFrontComposerQuickstart(options =>
            options.ScanAssemblies(typeof(TenantsFrontComposerDomain).Assembly));
        Services.AddHexalithDomain<TenantsFrontComposerDomain>();
        Services.AddHexalithTenantsUiModule(new ConfigurationBuilder().Build(), enableGatewayAuthorization: false);
        Services.Replace(ServiceDescriptor.Scoped<IStorageService, InMemoryStorageService>());
        Services.Replace(ServiceDescriptor.Scoped<IThemeService>(_ => Substitute.For<IThemeService>()));
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns("tenant.router");
        accessor.UserId.Returns("operator.router");
        Services.Replace(ServiceDescriptor.Scoped(_ => accessor));
        AddAuthorization().SetAuthorized("operator.router");
    }

    /// <summary>Requires the same live router to discover and leave the framework-owned home page.</summary>
    [Fact]
    public async Task MountedRouterNavigatesFromTenantsToFrameworkHomeAndBack()
    {
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/tenants/my");
        IRenderedComponent<Routes> cut = Render<Routes>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-page-header']").ShouldNotBeNull());
        Router router = cut.FindComponent<Router>().Instance;

        await cut.InvokeAsync(() => navigation.NavigateTo("/home"));

        cut.WaitForAssertion(() => cut.FindComponent<FcHomeRouteView>().ShouldNotBeNull());
        cut.FindComponent<FcHomeDirectory>().ShouldNotBeNull();
        cut.FindComponent<FrontComposerShell>().ShouldNotBeNull();
        cut.Find("#fc-main-content [data-testid='fc-home-directory']").ShouldNotBeNull();
        cut.FindAll("[data-testid='tenants-my-page-header']").ShouldBeEmpty();
        cut.FindComponent<Router>().Instance.ShouldBeSameAs(router);

        await cut.InvokeAsync(() => navigation.NavigateTo("/tenants/my"));

        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-my-page-header']").ShouldNotBeNull());
        cut.FindComponents<FcHomeRouteView>().ShouldBeEmpty();
        cut.FindComponent<Router>().Instance.ShouldBeSameAs(router);
    }

    /// <summary>Requires a localized recovery surface and a fixed home destination after an unmatched navigation.</summary>
    /// <param name="culture">The active UI culture.</param>
    /// <param name="heading">The expected recovery heading.</param>
    /// <param name="message">The expected explanatory text.</param>
    /// <param name="recovery">The expected recovery link text.</param>
    [Theory]
    [InlineData("en", "Page not found", "The requested page is unavailable. Return to the workspace home to continue.", "Return to home")]
    [InlineData("fr", "Page introuvable", "La page demandée est indisponible. Revenez à l’accueil de l’espace de travail pour continuer.", "Revenir à l’accueil")]
    public async Task UnmatchedInteractiveRouteOffersLocalizedSafeRecovery(
        string culture,
        string heading,
        string message,
        string recovery)
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            SetRendererInfo(new RendererInfo("Server", isInteractive: true));
            NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo("/home");
            IRenderedComponent<Routes> cut = Render<Routes>();
            cut.WaitForAssertion(() => cut.FindComponent<FcHomeRouteView>().ShouldNotBeNull());
            Router router = cut.FindComponent<Router>().Instance;

            await cut.InvokeAsync(() => navigation.NavigateTo(
                "/missing/private/path?returnUrl=https%3A%2F%2Fevil.invalid%2F&access_token=private-token"));

            cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-route-not-found']").ShouldNotBeNull());
            cut.Find("#tenants-route-not-found-heading").TextContent.ShouldBe(heading);
            cut.Find("#tenants-route-not-found-heading").GetAttribute("tabindex").ShouldBe("-1");
            FocusOnNavigate focus = cut.FindComponent<FocusOnNavigate>().Instance;
            focus.RouteData.ShouldNotBeNull();
            focus.RouteData!.PageType.ShouldBe(typeof(UI.Components.Pages.RouteNotFoundPage));
            focus.Selector.ShouldBe("h1");
            cut.Find("[data-testid='tenants-route-not-found']").TextContent.ShouldContain(message);
            cut.Find("[data-testid='tenants-route-recovery']").TextContent.Trim().ShouldBe(recovery);
            string? href = cut.Find("[data-testid='tenants-route-recovery']").GetAttribute("href");
            href.ShouldBe("/home");
            cut.Find("#fc-main-content").TextContent.ShouldNotContain("private-token");
            cut.Find("#fc-main-content").TextContent.ShouldNotContain("/missing/private/path");
            cut.Find("#fc-main-content").TextContent.ShouldNotContain("evil.invalid");
            cut.FindComponents<FcHomeRouteView>().ShouldBeEmpty();
            cut.FindComponent<Router>().Instance.ShouldBeSameAs(router);

            await cut.InvokeAsync(() => navigation.NavigateTo(href!));

            cut.WaitForAssertion(() => cut.FindComponent<FcHomeRouteView>().ShouldNotBeNull());
            cut.FindAll("[data-testid='tenants-route-not-found']").ShouldBeEmpty();
            cut.FindComponent<Router>().Instance.ShouldBeSameAs(router);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }
}
