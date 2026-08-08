using System.Globalization;

using Bunit;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Shared;
using Hexalith.Tenants.UI.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class ProjectionLifecycleStatusTests : FluentBunitContext
{
    [Theory]
    [InlineData(ProjectionLifecycleState.Current, "status", "Current")]
    [InlineData(ProjectionLifecycleState.Stale, "status", "Stale")]
    [InlineData(ProjectionLifecycleState.Rebuilding, "status", "Rebuilding")]
    [InlineData(ProjectionLifecycleState.Unavailable, "alert", "Unavailable")]
    public void Status_wrapper_uses_alert_only_for_unavailable_and_shows_localized_label(
        ProjectionLifecycleState lifecycle,
        string expectedRole,
        string expectedLabel)
    {
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<ProjectionLifecycleStatus> cut = Render<ProjectionLifecycleStatus>(parameters => parameters
            .Add(status => status.Lifecycle, lifecycle)
            .Add(status => status.TestId, "tenants-projection-lifecycle-status")
            .Add(status => status.BadgeTestId, "tenants-projection-lifecycle-status-badge"));

        var region = cut.Find("[data-testid='tenants-projection-lifecycle-status']");
        region.GetAttribute("role").ShouldBe(expectedRole);
        region.GetAttribute("aria-live").ShouldBeNull();
        region.TextContent.ShouldContain("Projection lifecycle");
        cut.Find("[data-testid='tenants-projection-lifecycle-status-badge']").TextContent.Trim().ShouldBe(expectedLabel);
        (cut.Find("[data-testid='tenants-projection-lifecycle-status-badge']").GetAttribute("class") ?? string.Empty)
            .ShouldContain($"projection-lifecycle-badge--{lifecycle.ToString().ToLowerInvariant()}");
    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly IReadOnlyDictionary<string, string> Values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Tenants.ProjectionLifecycle.Label"] = "Projection lifecycle",
            ["Tenants.ProjectionLifecycle.Current"] = "Current",
            ["Tenants.ProjectionLifecycle.Stale"] = "Stale",
            ["Tenants.ProjectionLifecycle.Rebuilding"] = "Rebuilding",
            ["Tenants.ProjectionLifecycle.Unavailable"] = "Unavailable",
            ["Tenants.ProjectionLifecycle.Degraded"] = "Degraded",
            ["Tenants.ProjectionLifecycle.LocalOnly"] = "Local only",
            ["Tenants.ProjectionLifecycle.Unknown"] = "Unknown",
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
