using System.Globalization;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Shared;
using Hexalith.Tenants.UI.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TruthStateBadgeTests : FluentBunitContext
{
    [Theory]
    [InlineData(ReadModelFreshnessState.Current, BadgeColor.Success, "Checkmark")]
    [InlineData(ReadModelFreshnessState.Aging, BadgeColor.Warning, "Clock")]
    [InlineData(ReadModelFreshnessState.Stale, BadgeColor.Severe, "ClockAlarm")]
    [InlineData(ReadModelFreshnessState.Unknown, BadgeColor.Important, "QuestionCircle")]
    public void Freshness_uses_locked_semantics_and_size20_icons(
        ReadModelFreshnessState freshness,
        BadgeColor expectedColor,
        string expectedIconType)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<TruthStateBadge> cut = Render<TruthStateBadge>(parameters => parameters
            .Add(badge => badge.Freshness, freshness));

        FluentBadge badge = cut.FindComponent<FluentBadge>().Instance;
        badge.Color.ShouldBe(expectedColor);
        badge.IconStart.ShouldNotBeNull().GetType().Name.ShouldBe(expectedIconType);
        badge.IconStart.Size.ShouldBe(IconSize.Size20);
        badge.IconLabel.ShouldBe(cut.Find("[data-testid='tenants-list-truth-state']").TextContent.Trim());
        cut.Find("[data-testid='tenants-list-truth-state']").GetAttribute("role").ShouldBeNull();
    }

    [Fact]
    public void Refreshing_is_a_transient_badge_flag_not_a_freshness_state()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<TruthStateBadge> cut = Render<TruthStateBadge>(parameters => parameters
            .Add(badge => badge.Freshness, ReadModelFreshnessState.Stale)
            .Add(badge => badge.IsRefreshing, true)
            .Add(badge => badge.TestId, "tenants-list-truth-state"));

        var badge = cut.Find("[data-testid='tenants-list-truth-state']");
        badge.TextContent.ShouldContain("Refreshing");
        badge.GetAttribute("aria-label").ShouldBe("Refreshing");
        (badge.GetAttribute("class") ?? string.Empty).ShouldContain("truth-state-badge--refreshing");
        FluentBadge fluentBadge = cut.FindComponent<FluentBadge>().Instance;
        fluentBadge.Color.ShouldBe(BadgeColor.Informative);
        fluentBadge.IconStart.ShouldNotBeNull().GetType().Name.ShouldBe("ArrowClockwise");
        fluentBadge.IconStart.Size.ShouldBe(IconSize.Size20);
        fluentBadge.IconLabel.ShouldBe("Refreshing");
        badge.GetAttribute("role").ShouldBe("status");
    }

    [Theory]
    [InlineData(ProjectionLifecycleState.Current, BadgeColor.Success, "Checkmark", "Current")]
    [InlineData(ProjectionLifecycleState.Stale, BadgeColor.Severe, "ClockAlarm", "Stale")]
    [InlineData(ProjectionLifecycleState.Rebuilding, BadgeColor.Informative, "ArrowClockwise", "Rebuilding")]
    [InlineData(ProjectionLifecycleState.Degraded, BadgeColor.Warning, "ClockAlarm", "Degraded")]
    [InlineData(ProjectionLifecycleState.Unavailable, BadgeColor.Severe, "QuestionCircle", "Unavailable")]
    [InlineData(ProjectionLifecycleState.LocalOnly, BadgeColor.Important, "Clock", "Local only")]
    [InlineData(ProjectionLifecycleState.Unknown, BadgeColor.Important, "QuestionCircle", "Unknown")]
    public void Projection_lifecycle_has_an_independent_badge(
        ProjectionLifecycleState lifecycle,
        BadgeColor expectedColor,
        string expectedIconType,
        string expectedLabel)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<ProjectionLifecycleBadge> cut = Render<ProjectionLifecycleBadge>(parameters => parameters
            .Add(badge => badge.Lifecycle, lifecycle));

        var badge = cut.Find("[data-testid='tenants-projection-lifecycle']");
        badge.TextContent.Trim().ShouldBe(expectedLabel);
        badge.GetAttribute("aria-label").ShouldBe(expectedLabel);
        (badge.GetAttribute("class") ?? string.Empty).ShouldContain($"projection-lifecycle-badge--{lifecycle.ToString().ToLowerInvariant()}");
        FluentBadge fluentBadge = cut.FindComponent<FluentBadge>().Instance;
        fluentBadge.Color.ShouldBe(expectedColor);
        fluentBadge.IconStart.ShouldNotBeNull().GetType().Name.ShouldBe(expectedIconType);
        fluentBadge.IconStart.Size.ShouldBe(IconSize.Size20);
        fluentBadge.IconLabel.ShouldBe(expectedLabel);
    }

    [Fact]
    public void Freshness_and_lifecycle_render_without_masking_each_other()
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<TruthStateBadge> freshness = Render<TruthStateBadge>(parameters => parameters
            .Add(badge => badge.Freshness, ReadModelFreshnessState.Aging));
        IRenderedComponent<ProjectionLifecycleBadge> lifecycle = Render<ProjectionLifecycleBadge>(parameters => parameters
            .Add(badge => badge.Lifecycle, ProjectionLifecycleState.Stale));

        freshness.Find("[data-testid='tenants-list-truth-state']").TextContent.Trim().ShouldBe("Aging");
        freshness.FindComponent<FluentBadge>().Instance.Color.ShouldBe(BadgeColor.Warning);
        lifecycle.Find("[data-testid='tenants-projection-lifecycle']").TextContent.Trim().ShouldBe("Stale");
        lifecycle.FindComponent<FluentBadge>().Instance.Color.ShouldBe(BadgeColor.Severe);
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly IReadOnlyDictionary<string, string> Values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Tenants.List.Freshness.Refreshing"] = "Refreshing",
            ["Tenants.List.Freshness.Current"] = "Current",
            ["Tenants.List.Freshness.Aging"] = "Aging",
            ["Tenants.List.Freshness.Stale"] = "Stale",
            ["Tenants.List.Freshness.Unknown"] = "Unknown",
            ["Tenants.ProjectionLifecycle.Rebuilding"] = "Rebuilding",
            ["Tenants.ProjectionLifecycle.Current"] = "Current",
            ["Tenants.ProjectionLifecycle.Stale"] = "Stale",
            ["Tenants.ProjectionLifecycle.Unknown"] = "Unknown",
            ["Tenants.ProjectionLifecycle.Degraded"] = "Degraded",
            ["Tenants.ProjectionLifecycle.Unavailable"] = "Unavailable",
            ["Tenants.ProjectionLifecycle.LocalOnly"] = "Local only",
        };

        public LocalizedString this[string name] => CreateString(name);

        public LocalizedString this[string name, params object[] arguments]
            => new(
                name,
                string.Format(CultureInfo.InvariantCulture, CreateString(name).Value, arguments),
                resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static pair => new LocalizedString(pair.Key, pair.Value, resourceNotFound: false));

        private static LocalizedString CreateString(string name)
            => Values.TryGetValue(name, out string? value)
                ? new LocalizedString(name, value, resourceNotFound: false)
                : new LocalizedString(name, name, resourceNotFound: true);
    }
}
