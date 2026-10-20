using System.Globalization;
using System.Resources;

using Bunit;

using Hexalith.Tenants.UI.Components.Shared;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Verifies tenant-list state copy against the shipped English and French resources.</summary>
public sealed class ListSurfaceStatesTests : BunitContext
{
    /// <summary>Includes every reason and surface, search-page continuation, and invalid reason values.</summary>
    public static TheoryData<string, TenantListSurfaceKind, TenantListReason, bool> LocalizedScenarios
    {
        get
        {
            TheoryData<string, TenantListSurfaceKind, TenantListReason, bool> scenarios = [];
            foreach (string culture in new[] { "en", "fr" })
            {
                foreach (TenantListSurfaceKind kind in Enum.GetValues<TenantListSurfaceKind>())
                {
                    foreach (TenantListReason reason in Enum.GetValues<TenantListReason>()
                        .Concat([(TenantListReason)(-1), (TenantListReason)999]))
                    {
                        scenarios.Add(culture, kind, reason, false);
                        if (kind is TenantListSurfaceKind.SearchPageEmpty)
                        {
                            scenarios.Add(culture, kind, reason, true);
                        }
                    }
                }
            }

            return scenarios;
        }
    }

    /// <summary>Preserves supported reason copy and uses surface copy whenever no reason resource exists.</summary>
    [Theory]
    [MemberData(nameof(LocalizedScenarios))]
    public void StateMessageIsLocalizedForEveryReason(
        string cultureName,
        TenantListSurfaceKind kind,
        TenantListReason reason,
        bool hasMore)
    {
        CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
        Services.AddFluentUIComponents();
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(provider =>
            new CultureBoundTenantsLocalizer(
                provider.GetRequiredService<IStringLocalizerFactory>().Create(typeof(TenantsResources)), culture));
        string stateName = kind is TenantListSurfaceKind.Ready ? "Empty" : kind.ToString();
        ResourceManager resources = new(typeof(TenantsResources));
        string? reasonMessage = reason is TenantListReason.None
            ? null
            : resources.GetString($"Tenants.List.Reason.{reason}", culture);
        if (reason is TenantListReason.GatewayUnavailable or TenantListReason.NotModifiedWithoutSnapshot
            or TenantListReason.ProjectionDegraded or TenantListReason.RowEnrichmentUnavailable
            or TenantListReason.SearchPartiallyAvailable)
        {
            reasonMessage.ShouldNotBeNullOrWhiteSpace();
        }

        string fallbackKey = kind is TenantListSurfaceKind.SearchPageEmpty && hasMore
            ? "Tenants.List.State.SearchPageEmpty.MoreMessage"
            : $"Tenants.List.State.{stateName}.Message";
        string expectedMessage = reasonMessage ?? resources.GetString(fallbackKey, culture).ShouldNotBeNull();
        int resetCalls = 0;
        int refreshCalls = 0;

        IRenderedComponent<ListSurfaceStates> cut = Render<ListSurfaceStates>(parameters => parameters
            .Add(component => component.Kind, kind)
            .Add(component => component.Reason, reason)
            .Add(component => component.HasMore, hasMore)
            .Add(component => component.OnReset, () => resetCalls++)
            .Add(component => component.OnRefresh, () => refreshCalls++));

        cut.Find("p").TextContent.ShouldBe(expectedMessage);
        cut.Find("p").TextContent.ShouldNotContain("Tenants.List.");
        string titleKey = kind is TenantListSurfaceKind.SearchPageEmpty && hasMore
            ? "Tenants.List.State.SearchPageEmpty.MoreTitle"
            : $"Tenants.List.State.{stateName}.Title";
        cut.Find("h2").TextContent.ShouldBe(resources.GetString(titleKey, culture));
        bool assertive = kind is TenantListSurfaceKind.Error or TenantListSurfaceKind.Degraded;
        cut.Find("section").GetAttribute("role").ShouldBe(assertive ? "alert" : "status");
        cut.Find("section").GetAttribute("aria-live").ShouldBe(assertive ? "assertive" : "polite");

        if (kind is TenantListSurfaceKind.FilteredEmpty or TenantListSurfaceKind.SearchPageEmpty)
        {
            cut.Find("[data-testid='tenants-list-state-reset']").Click();
            resetCalls.ShouldBe(1);
        }

        if (kind is TenantListSurfaceKind.Stale)
        {
            cut.Find("[data-testid='tenants-list-state-refresh']").Click();
            refreshCalls.ShouldBe(1);
        }

        resetCalls.ShouldBe(kind is TenantListSurfaceKind.FilteredEmpty or TenantListSurfaceKind.SearchPageEmpty ? 1 : 0);
        refreshCalls.ShouldBe(kind is TenantListSurfaceKind.Stale ? 1 : 0);
    }
}
