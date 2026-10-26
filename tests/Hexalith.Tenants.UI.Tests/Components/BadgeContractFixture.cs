using System.Globalization;
using System.Resources;

using AngleSharp.Dom;

using Hexalith.Tenants.UI.Resources;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Independent state-to-resource expectations exported alongside actual rendered markup.</summary>
internal static class BadgeContractFixture
{
    private static readonly ResourceManager Resources = new("Hexalith.Tenants.UI.Resources.TenantsResources", typeof(TenantsResources).Assembly);

    /// <summary>Checks independently localized label/name expectations and tags the rendered browser fixture.</summary>
    internal static string Expect(IElement badge, string cultureName, string site, string state, string labelKey, string? nameKey = null)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
        string expectedLabel = Resources.GetString(labelKey, culture).ShouldNotBeNull();
        string expectedName = nameKey is null ? expectedLabel
            : string.Format(culture, Resources.GetString(nameKey, culture).ShouldNotBeNull(), expectedLabel);
        IElement outer = badge.Closest("[data-status-badge]").ShouldNotBeNull();
        outer.GetAttribute("aria-label").ShouldBe(expectedName);
        badge.GetAttribute("aria-label").ShouldBe(expectedName);
        badge.TextContent.Trim().ShouldBe(expectedLabel);
        badge.QuerySelector("svg").ShouldNotBeNull().GetAttribute("aria-label").ShouldBe(expectedLabel);
        outer.SetAttribute("data-badge-site", site);
        outer.SetAttribute("data-badge-state", state);
        outer.SetAttribute("data-expected-name", expectedName);
        outer.SetAttribute("data-expected-label", expectedLabel);
        return outer.OuterHtml;
    }
}
