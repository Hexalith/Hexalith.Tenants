using System.Globalization;

using Bunit;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed partial class TenantDetailSurfaceTests
{
    [Theory]
    [InlineData("en", TenantStatus.Active, BadgeColor.Success, BadgeAppearance.Tint)]
    [InlineData("fr", TenantStatus.Active, BadgeColor.Success, BadgeAppearance.Tint)]
    [InlineData("en", TenantStatus.Disabled, BadgeColor.Danger, BadgeAppearance.Filled)]
    [InlineData("fr", TenantStatus.Disabled, BadgeColor.Danger, BadgeAppearance.Filled)]
    [InlineData("en", TenantStatus.Unknown, BadgeColor.Subtle, BadgeAppearance.Tint)]
    [InlineData("fr", TenantStatus.Unknown, BadgeColor.Subtle, BadgeAppearance.Tint)]
    public void DetailStatusUsesTheSharedBadgeAnatomy(string cultureName, TenantStatus status, BadgeColor color, BadgeAppearance appearance)
    {
        RegisterServices(_ => Task.FromResult(ReadyWithSafeConfiguration(
            Detail("tenant.alpha", new Dictionary<string, string>(), status, []),
            ProjectionLifecycleState.Current, "tenant-sequence:41")));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(provider => new CultureBoundTenantsLocalizer(
            provider.GetRequiredService<IStringLocalizerFactory>().Create(typeof(TenantsResources)),
            CultureInfo.GetCultureInfo(cultureName)));
        var cut = Render<TenantDetailPage>(parameters => parameters.Add(page => page.TenantId, "tenant.alpha"));
        cut.WaitForElement(".tenant-detail__status");
        FluentBadge badge = cut.FindComponents<FluentBadge>().Select(component => component.Instance)
            .Single(component => component.Class == "tenant-detail__status");

        badge.Shape.ShouldBe(BadgeShape.Rounded);
        badge.Color.ShouldBe(color);
        badge.Appearance.ShouldBe(appearance);
        badge.IconLabel.ShouldNotBeNullOrWhiteSpace();
        var element = cut.Find(".tenant-detail__status");
        element.GetAttribute("aria-hidden").ShouldBe("true");
        element.Closest("[data-status-badge]").ShouldNotBeNull().GetAttribute("role").ShouldBe("img");
        element.GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();
        element.TextContent.Trim().ShouldBe(badge.IconLabel);
        element.GetAttribute("aria-live").ShouldBeNull();
        string markup = BadgeContractFixture.Expect(element, cultureName, "detail-status", status.ToString(),
            $"Tenants.Detail.Status.{status}", "Tenants.Detail.StatusAccessibleLabel");
        string? directory = Environment.GetEnvironmentVariable("TENANTS_BADGE_FIXTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, $"status-badges-detail-{cultureName}-{status}.html"), markup);
        }
    }
}
