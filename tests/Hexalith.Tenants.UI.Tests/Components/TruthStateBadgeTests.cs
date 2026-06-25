using System.Globalization;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.Components.Shared;
using Hexalith.Tenants.UI.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class TruthStateBadgeTests : FluentBunitContext
{
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
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly IReadOnlyDictionary<string, string> Values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Tenants.List.Freshness.Refreshing"] = "Refreshing",
            ["Tenants.List.Freshness.Stale"] = "Stale",
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
