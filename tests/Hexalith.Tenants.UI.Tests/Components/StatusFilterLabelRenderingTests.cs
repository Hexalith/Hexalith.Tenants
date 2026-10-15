using System.Globalization;

using AngleSharp.Dom;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

/// <summary>Exercises scope labels through the actual workspace and Fluent selector rendering.</summary>
public sealed class StatusFilterLabelRenderingTests : BunitContext
{
    private JSRuntimeInvocationHandler<bool> _labelSync = default!;

    /// <summary>Checks ordinary, fallback, partial and empty authoritative labels in both shipped cultures.</summary>
    [Theory]
    [InlineData("en", "ordinary", false)]
    [InlineData("fr", "ordinary", false)]
    [InlineData("en", "fallback", false)]
    [InlineData("fr", "fallback", false)]
    [InlineData("en", "authoritative", true)]
    [InlineData("fr", "authoritative", true)]
    [InlineData("en", "partial", true)]
    [InlineData("fr", "partial", true)]
    [InlineData("en", "empty", true)]
    [InlineData("fr", "empty", true)]
    public void LabelAndAccessibleNameDescribeTheResolvedScope(string culture, string mode, bool authoritative)
    {
        TenantListSnapshot snapshot = Snapshot(authoritative);
        snapshot = mode switch
        {
            "fallback" => snapshot with { Notice = TenantListReason.SearchUnavailable },
            "partial" => TenantListSnapshot.Degraded(snapshot.Rows, TenantListReason.SearchPartiallyAvailable)
                with { IsAuthoritativeSearch = true },
            "empty" => snapshot with { Kind = TenantListSurfaceKind.SearchPageEmpty, Rows = [] },
            _ => snapshot,
        };
        RegisterServices(culture, _ => Task.FromResult(snapshot));
        Services.GetRequiredService<NavigationManager>().NavigateTo(
            mode == "ordinary" ? "/tenants" : "/tenants?search=needle");

        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();

        Export(cut, culture, mode);
        cut.WaitForAssertion(() => AssertLabel(cut, culture, authoritative));
    }

    /// <summary>Rechecks both names after search, fallback, recovery and clearing on the same workspace.</summary>
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public async Task BothNamesFollowSearchFallbackRecoveryAndClear(string culture)
    {
        bool searchAvailable = true;
        List<TenantListRequest> requests = [];
        RegisterServices(culture, call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            requests.Add(request);
            bool authoritative = searchAvailable && request.Search is not null;
            return Task.FromResult(Snapshot(authoritative) with
            {
                Notice = request.Search is not null && !searchAvailable
                    ? TenantListReason.SearchUnavailable : TenantListReason.None,
            });
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?status=Active");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForAssertion(() => AssertTransition(cut, culture, authoritative: false));
        Export(cut, culture, "transition-0");

        FluentTextInput search = cut.FindComponents<FluentTextInput>()
            .Single(component => component.Instance.AdditionalAttributes?["data-testid"] as string == "tenants-list-search").Instance;
        await cut.InvokeAsync(() => search.ValueChanged.InvokeAsync("needle"));
        cut.WaitForAssertion(() => AssertTransition(cut, culture, authoritative: true));
        Export(cut, culture, "transition-1");

        searchAvailable = false;
        cut.Find("[data-testid='tenants-list-refresh']").Click();
        cut.WaitForAssertion(() => AssertTransition(cut, culture, authoritative: false));
        Export(cut, culture, "transition-2");
        requests[^1].Search.ShouldBe("needle");

        searchAvailable = true;
        cut.Find("[data-testid='tenants-list-refresh']").Click();
        cut.WaitForAssertion(() => AssertTransition(cut, culture, authoritative: true));
        Export(cut, culture, "transition-3");

        await cut.InvokeAsync(() => search.ValueChanged.InvokeAsync(string.Empty));
        cut.WaitForAssertion(() => AssertTransition(cut, culture, authoritative: false));
        Export(cut, culture, "transition-4");
        requests[^1].Search.ShouldBeNull();
        requests.ShouldAllBe(request => request.Status == TenantStatus.Active);
        _labelSync.Invocations.Count.ShouldBeGreaterThanOrEqualTo(5);
    }

    /// <summary>Includes the first loading render before an authoritative route finishes its gateway read.</summary>
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public void InitialLoadingThenAuthoritativeCompletionUpdatesBothNames(string culture)
    {
        var pending = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        RegisterServices(culture, _ => pending.Task);
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle&status=Active");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForElement("[data-testid='tenants-list-loading']");
        AssertTransition(cut, culture, authoritative: false);
        Export(cut, culture, "delayed-0");

        pending.SetResult(Snapshot(authoritative: true));

        cut.WaitForElement("[data-testid='tenants-list-grid']");
        cut.WaitForAssertion(() => AssertTransition(cut, culture, authoritative: true));
        Export(cut, culture, "delayed-1");
        _labelSync.Invocations.Count.ShouldBeGreaterThanOrEqualTo(2);
    }

    /// <summary>Changes status while the authoritative read is held, retaining the selector and its chosen status.</summary>
    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public async Task HeldAuthoritativeStatusChangeKeepsTheControlAndSynchronizesEachScope(string culture)
    {
        var pending = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        TenantListRequest? changedRequest = null;
        RegisterServices(culture, call =>
        {
            TenantListRequest request = call.ArgAt<TenantListRequest>(0);
            if (request.Status == TenantStatus.Disabled)
            {
                changedRequest = request;
                return pending.Task;
            }
            return Task.FromResult(Snapshot(authoritative: true));
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle&status=Active");
        IRenderedComponent<TenantsWorkspace> cut = Render<TenantsWorkspace>();
        cut.WaitForAssertion(() => AssertTransition(cut, culture, authoritative: true));
        string selectorId = cut.Find("[data-testid='tenants-list-status-filter']").Id.ShouldNotBeNull();
        Export(cut, culture, "status-0");
        FluentSelect<string, string> select = cut.FindComponents<FluentSelect<string, string>>()
            .Single(component => component.Instance.Id == selectorId).Instance;

        Task change = cut.InvokeAsync(() => select.ValueChanged.InvokeAsync(nameof(TenantStatus.Disabled)));
        cut.WaitForElement("[data-testid='tenants-list-loading']");
        AssertLabel(cut, culture, authoritative: false);
        cut.Find("[data-testid='tenants-list-status-filter']").Id.ShouldBe(selectorId);
        select.Value.ShouldBe(nameof(TenantStatus.Disabled));
        Export(cut, culture, "status-1");
        pending.SetResult(Snapshot(authoritative: true) with
        {
            Rows = [TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Disabled))],
        });
        await change;

        cut.WaitForAssertion(() => AssertLabel(cut, culture, authoritative: true));
        cut.Find("[data-testid='tenants-list-status-filter']").Id.ShouldBe(selectorId);
        select.Value.ShouldBe(nameof(TenantStatus.Disabled));
        changedRequest.ShouldNotBeNull().Search.ShouldBe("needle");
        changedRequest.Status.ShouldBe(TenantStatus.Disabled);
        _labelSync.Invocations.Count.ShouldBeGreaterThanOrEqualTo(3);
        Export(cut, culture, "status-2");
    }

    private void RegisterServices(string culture, Func<NSubstitute.Core.CallInfo, Task<TenantListSnapshot>> resultFactory)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _labelSync = JSInterop.SetupModule("./js/tenantsStatusFilter.js")
            .Setup<bool>("syncStatusFilterLabel").SetResult(true);
        Services.AddLocalization();
        Services.AddFluentUIComponents();
        Services.AddScoped<TenantSearchPagingState>();
#if HFC_ROUTE_OPTIONS
        Services.AddScoped<Hexalith.FrontComposer.Shell.Services.NavigationFailureNotifier>();
#endif
        var factory = new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions()), NullLoggerFactory.Instance);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new CultureBoundTenantsLocalizer(
            factory.Create(typeof(TenantsResources)), CultureInfo.GetCultureInfo(culture)));
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(resultFactory);
        Services.AddSingleton(gateway);
        IUserContextAccessor user = Substitute.For<IUserContextAccessor>();
        user.UserId.Returns("operator-user");
        Services.AddSingleton(user);
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private static TenantListSnapshot Snapshot(bool authoritative)
        => TenantListSnapshot.Ready(
            [TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active))],
            nextCursor: null, hasMore: false, eTag: "\"etag\"", ReadModelFreshnessState.Unknown, isDegraded: false)
            with { IsAuthoritativeSearch = authoritative };

    private static void AssertLabel(IRenderedComponent<TenantsWorkspace> cut, string culture, bool authoritative)
    {
        string expected = (culture, authoritative) switch
        {
            ("fr", true) => "Statut parmi les candidats indexés",
            ("fr", false) => "Statut sur la page actuelle",
            (_, true) => "Status across indexed candidates",
            _ => "Status on current page",
        };
        IElement selector = cut.Find("[data-testid='tenants-list-status-filter']");
        selector.NodeName.ShouldBe("FLUENT-DROPDOWN");
        IElement label = selector.Closest("fluent-field").ShouldNotBeNull()
            .QuerySelector("label[slot='label']").ShouldNotBeNull();
        label.GetAttribute("for").ShouldBe(selector.Id);
        label.TextContent.Trim().ShouldBe(expected);
        selector.GetAttribute("aria-label").ShouldBe(expected);
    }

    private static void AssertTransition(IRenderedComponent<TenantsWorkspace> cut, string culture, bool authoritative)
    {
        AssertLabel(cut, culture, authoritative);
        cut.FindComponents<FluentSelect<string, string>>()
            .Single(component => component.Instance.AdditionalAttributes?["data-testid"] as string == "tenants-list-status-filter")
            .Instance.Value.ShouldBe(nameof(TenantStatus.Active));
    }

    private void Export(IRenderedComponent<TenantsWorkspace> cut, string culture, string mode)
    {
        string? directory = Environment.GetEnvironmentVariable("TENANTS_STATUS_LABEL_FIXTURE_DIRECTORY");
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, $"status-{culture}-{mode}.html"),
                cut.Find("[data-testid='tenants-list-status-filter']").Closest("fluent-field")!.OuterHtml);
            File.WriteAllText(Path.Combine(directory, $"status-{culture}-{mode}.json"),
                System.Text.Json.JsonSerializer.Serialize(new { syncCount = _labelSync.Invocations.Count }));
        }
    }
}
