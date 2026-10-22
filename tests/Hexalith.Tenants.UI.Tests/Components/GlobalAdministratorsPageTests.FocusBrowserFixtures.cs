using System.Globalization;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed partial class GlobalAdministratorsPageTests
{
    /// <summary>Exports the real page's localized Cancel host for browser focus qualification.</summary>
    /// <param name="culture">The shipped resource culture rendered into the fixture.</param>
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public void RemoveCancelExportsActualFluentControlForBrowserFocus(string culture)
    {
        var factory = new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions()), NullLoggerFactory.Instance);
        var localizer = new CultureBoundTenantsLocalizer(
            factory.Create(typeof(TenantsResources)), CultureInfo.GetCultureInfo(culture));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(localizer);
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            ComponentReady("projection-v1", "target-admin", "other-admin")));
        var commands = new StubTenantCommandGateway();
        Services.AddSingleton<ITenantCommandGateway>(commands);
        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        OpenRemovePreview(cut);

        IElement dialog = cut.Find("[data-testid='tenants-global-admin-remove-preview']");
        dialog.Closest("[inert], [hidden], [aria-hidden='true']").ShouldBeNull();
        IElement cancel = dialog.QuerySelector("[data-testid='tenants-global-admin-remove-cancel']").ShouldNotBeNull();
        cancel.LocalName.ShouldBe("fluent-button");
        cancel.GetAttribute("id").ShouldBe("tenants-global-admin-remove-cancel-button");
        cancel.HasAttribute("disabled").ShouldBeFalse();
        cancel.TextContent.Trim().ShouldBe(culture == "fr" ? "Annuler" : "Cancel");
        cut.FindComponents<FluentButton>().ShouldContain(component =>
            component.Instance.Id == cancel.GetAttribute("id"));
        cut.Find("[data-testid='tenants-global-admins-area']").HasAttribute("inert").ShouldBeTrue();
        commands.RemoveGlobalAdministratorCalls.ShouldBe(0);

        string? directory = Environment.GetEnvironmentVariable("TENANTS_REMOVE_FOCUS_FIXTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, $"remove-cancel-{culture}.html"), dialog.OuterHtml);
        }
    }

    /// <summary>Exports the grant preview's actual Fluent Cancel and approved modal chrome.</summary>
    /// <param name="culture">The shipped culture rendered into the fixture.</param>
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public void GrantCancelExportsActualFluentControlForBrowserFocus(string culture)
    {
        var factory = new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions()), NullLoggerFactory.Instance);
        var localizer = new CultureBoundTenantsLocalizer(
            factory.Create(typeof(TenantsResources)), CultureInfo.GetCultureInfo(culture));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(localizer);
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            ComponentReady("projection-v1", "admin-a")) { RepeatLastResponse = true });
        var commands = new StubTenantCommandGateway();
        Services.AddSingleton<ITenantCommandGateway>(commands);
        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("target-admin");
        OpenGrantPreview(cut);
        IElement dialog = cut.Find("[data-testid='tenants-global-admin-grant-preview']");
        dialog.Closest("[inert], [hidden], [aria-hidden='true']").ShouldBeNull();
        IElement cancel = dialog.QuerySelector("[data-testid='tenants-global-admin-grant-preview-cancel']").ShouldNotBeNull();
        cancel.LocalName.ShouldBe("fluent-button");
        cancel.GetAttribute("id").ShouldBe("tenants-global-admin-grant-cancel-button");
        cancel.HasAttribute("disabled").ShouldBeFalse();
        cancel.TextContent.Trim().ShouldBe(culture == "fr" ? "Annuler" : "Cancel");
        cut.FindComponents<FluentButton>().ShouldContain(component => component.Instance.Id == cancel.GetAttribute("id"));
        commands.SetGlobalAdministratorCalls.ShouldBe(0);
        string? directory = Environment.GetEnvironmentVariable("TENANTS_GRANT_FOCUS_FIXTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, $"grant-cancel-{culture}.html"), dialog.OuterHtml);
        }
    }
}
