using System.Globalization;

using AngleSharp.Dom;

using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Components.Tenants;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests;

public sealed partial class TenantsWorkspaceTests
{
    [Theory]
    [InlineData("en-US", false, TenantListSurfaceKind.Ready)]
    [InlineData("fr-FR", false, TenantListSurfaceKind.Ready)]
    [InlineData("en-US", true, TenantListSurfaceKind.Ready)]
    [InlineData("fr-FR", true, TenantListSurfaceKind.Ready)]
    [InlineData("en-US", false, TenantListSurfaceKind.Stale)]
    [InlineData("fr-FR", true, TenantListSurfaceKind.Stale)]
    [InlineData("en-US", false, TenantListSurfaceKind.Degraded)]
    [InlineData("fr-FR", true, TenantListSurfaceKind.Degraded)]
    public async Task SoftRefreshRetainsMountedRowsAndFailsClosedUntilCompletion(
        string culture,
        bool search,
        TenantListSurfaceKind kind)
    {
        CultureInfo priorCulture = CultureInfo.CurrentCulture;
        CultureInfo priorUiCulture = CultureInfo.CurrentUICulture;
        var held = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            TenantListSnapshot original = SoftRefreshSnapshot(search) with
            {
                Kind = kind,
                Freshness = kind is TenantListSurfaceKind.Ready ? ReadModelFreshnessState.Current
                    : kind is TenantListSurfaceKind.Stale ? ReadModelFreshnessState.Stale : ReadModelFreshnessState.Unknown,
                IsDegraded = kind is TenantListSurfaceKind.Degraded,
            };
            var requests = new List<(TenantListRequest Request, TenantListSnapshot? Previous)>();
            RegisterSoftRefreshServices(call =>
            {
                requests.Add((call.ArgAt<TenantListRequest>(0), call.ArgAt<TenantListSnapshot?>(1)));
                return requests.Count == 1 ? Task.FromResult(original) : held.Task;
            });
            Services.GetRequiredService<NavigationManager>().NavigateTo(search ? "/tenants?search=needle" : "/tenants");
            IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
            cut.WaitForElement("[data-testid='tenants-list-grid']");
            TenantDataGrid grid = cut.FindComponent<TenantDataGrid>().Instance;
            FluentDataGrid<TenantListRow> nativeGrid = cut.FindComponent<FluentDataGrid<TenantListRow>>().Instance;
            IElement[] detailLinks = cut.FindAll("[data-testid='tenants-list-detail-link']").ToArray();
            string[] rowIds = grid.Rows.Select(row => row.TenantId).ToArray();
            rowIds.ShouldBe(search ? ["tenant.zeta", "tenant.alpha"] : ["tenant.alpha", "tenant.zeta"]);

            Task refresh = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
            cut.WaitForAssertion(() => requests.Count.ShouldBe(2));

            cut.FindComponent<TenantDataGrid>().Instance.ShouldBeSameAs(grid);
            cut.FindComponent<FluentDataGrid<TenantListRow>>().Instance.ShouldBeSameAs(nativeGrid);
            grid.Rows.Select(row => row.TenantId).ShouldBe(rowIds);
            grid.PreserveRowOrder.ShouldBe(search);
            cut.FindAll("[data-testid='tenants-list-detail-link']").Select(link => link.TextContent.Trim()).ShouldBe(rowIds);
            cut.FindAll("[data-testid='tenants-list-detail-link']").Select(link => link.GetAttribute("href"))
                .ShouldBe(detailLinks.Select(link => link.GetAttribute("href")));
            cut.Find("[data-testid='tenants-create-submit']").HasAttribute("disabled").ShouldBeTrue();
            foreach (IElement entry in cut.FindAll("[data-testid='tenants-list-audit-entrypoint']"))
            {
                entry.ParentElement.ShouldNotBeNull().GetAttribute("href").ShouldBeNull();
                entry.ParentElement.ShouldNotBeNull().HasAttribute("disabled").ShouldBeTrue();
            }
            if (kind is TenantListSurfaceKind.Stale or TenantListSurfaceKind.Degraded)
            {
                cut.Find($"[data-testid='tenants-list-{kind.ToString().ToLowerInvariant()}']");
            }
            cut.Find("[data-testid='tenants-list-loading']").TextContent
                .ShouldContain(Services.GetRequiredService<IStringLocalizer<TenantsResources>>()["Tenants.List.State.Loading.Message"].Value);
            cut.Find("[data-testid='tenants-list-loading']").GetAttribute("aria-live").ShouldBe("polite");
            cut.Find("[data-testid='tenants-list-next']").HasAttribute("disabled").ShouldBeTrue();
            cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
            cut.FindComponent<CreateTenantFlow>().Instance.IsFresh.ShouldBeFalse();
            grid.AuditReadAvailable.ShouldBeFalse();
            requests[1].Previous.ShouldBeSameAs(original);
            requests[1].Request.ETag.ShouldBe(original.ETag);
            requests[1].Request.Search.ShouldBe(search ? "needle" : null);
            requests[1].Request.Cursor.ShouldBeNull();
            requests[1].Request.SearchCursor.ShouldBeNull();

            TenantListSnapshot updated = SoftRefreshSnapshot(search, "tenant.updated");
            held.SetResult(updated);
            await refresh.WaitAsync(TimeSpan.FromSeconds(5));
            cut.WaitForAssertion(() => grid.Rows.Select(row => row.TenantId).ShouldBe(["tenant.updated"]));
            cut.FindComponent<TenantDataGrid>().Instance.ShouldBeSameAs(grid);
            cut.FindAll("[data-testid='tenants-list-loading']").ShouldBeEmpty();
            cut.FindComponent<CreateTenantFlow>().Instance.IsFresh.ShouldBeTrue();
            grid.AuditReadAvailable.ShouldBeTrue();
            cut.Find("[data-testid='tenants-list-next']").HasAttribute("disabled").ShouldBeFalse();
        }
        finally
        {
            held.TrySetResult(TenantListSnapshot.Error());
            CultureInfo.CurrentCulture = priorCulture;
            CultureInfo.CurrentUICulture = priorUiCulture;
        }
    }

    [Theory]
    [InlineData("en-US", TenantListSurfaceKind.Ready)]
    [InlineData("fr-FR", TenantListSurfaceKind.Ready)]
    [InlineData("en-US", TenantListSurfaceKind.Stale)]
    [InlineData("fr-FR", TenantListSurfaceKind.Stale)]
    [InlineData("en-US", TenantListSurfaceKind.Degraded)]
    [InlineData("fr-FR", TenantListSurfaceKind.Degraded)]
    public async Task SoftRefreshPreservesWarningsAndPagerWhenPageLocalFilterMatchesNoRows(
        string culture,
        TenantListSurfaceKind kind)
    {
        CultureInfo priorCulture = CultureInfo.CurrentCulture;
        CultureInfo priorUiCulture = CultureInfo.CurrentUICulture;
        var held = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? refresh = null;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            TenantListSnapshot original = SoftRefreshSnapshot(false) with
            {
                Kind = kind,
                Freshness = kind is TenantListSurfaceKind.Ready ? ReadModelFreshnessState.Current
                    : kind is TenantListSurfaceKind.Stale ? ReadModelFreshnessState.Stale : ReadModelFreshnessState.Unknown,
                IsDegraded = kind is TenantListSurfaceKind.Degraded,
                Reason = TenantListReason.SearchPartiallyAvailable,
            };
            int calls = 0;
            RegisterSoftRefreshServices(_ => ++calls == 1 ? Task.FromResult(original) : held.Task);
            Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?status=Disabled");
            IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
            cut.WaitForElement("[data-testid='tenants-list-next']");
            TenantDataGrid? grid = cut.FindComponents<TenantDataGrid>().SingleOrDefault()?.Instance;
            if (grid is not null)
            {
                grid.Rows.ShouldBeEmpty();
            }
            string selector = $"[data-testid='tenants-list-{(kind is TenantListSurfaceKind.Ready ? "filtered-empty" : kind.ToString().ToLowerInvariant())}']";
            string warning = cut.Find(selector).TextContent;
            refresh = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
            cut.WaitForAssertion(() => calls.ShouldBe(2));
            if (grid is not null)
            {
                cut.FindComponent<TenantDataGrid>().Instance.ShouldBeSameAs(grid);
                cut.Find(selector).TextContent.ShouldBe(warning);
            }
            else
            {
                cut.FindAll("[data-testid='tenants-list-grid']").ShouldBeEmpty();
            }
            cut.Find("[data-testid='tenants-list-loading']");
            cut.Find("[data-testid='tenants-list-next']").HasAttribute("disabled").ShouldBeTrue();
            cut.FindAll("[data-testid='tenants-list-filtered-empty']").ShouldBeEmpty();
            held.SetResult(original);
            await refresh.WaitAsync(TimeSpan.FromSeconds(5));
            cut.Find(selector).TextContent.ShouldBe(warning);
            cut.FindAll("[data-testid='tenants-list-loading']").ShouldBeEmpty();
        }
        finally
        {
            held.TrySetResult(TenantListSnapshot.Error());
            if (refresh is not null)
            {
                await refresh.WaitAsync(TimeSpan.FromSeconds(5));
            }
            CultureInfo.CurrentCulture = priorCulture;
            CultureInfo.CurrentUICulture = priorUiCulture;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SoftRefreshClearsRetainedEvidenceWhenAuthenticationChangesDuringRead(bool unresolvedCaller)
    {
        var held = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        RegisterSoftRefreshServices(_ => ++calls == 1 ? Task.FromResult(SoftRefreshSnapshot(false)) : held.Task);
        var authentication = (MutableAuthenticationStateProvider)Services.GetRequiredService<AuthenticationStateProvider>();
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        Task refresh = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        TaskCompletionSource<AuthenticationState>? pendingCaller = null;
        try
        {
            cut.WaitForAssertion(() => calls.ShouldBe(2));
            cut.Find("[data-testid='tenants-list-grid']");
            await cut.InvokeAsync(() =>
            {
                Services.GetRequiredService<IUserContextAccessor>().UserId.Returns(unresolvedCaller ? null : "operator.other");
                if (unresolvedCaller)
                {
                    pendingCaller = authentication.NotifyPending();
                }
                else
                {
                    authentication.Set(NonAdministratorPrincipal());
                }
            });
            cut.WaitForAssertion(() => cut.FindAll("[data-testid='tenants-list-grid']").ShouldBeEmpty());
            cut.FindAll("[data-testid='tenants-list-next']").ShouldBeEmpty();
            cut.Find("[data-testid='tenants-list-loading']");
            cut.Markup.ShouldNotContain("tenant.alpha");
            calls.ShouldBe(2);
        }
        finally
        {
            held.TrySetResult(TenantListSnapshot.Unauthorized());
            pendingCaller?.TrySetResult(new AuthenticationState(NonAdministratorPrincipal()));
        }

        await refresh.WaitAsync(TimeSpan.FromSeconds(5));
        cut.Find("[data-testid='tenants-list-unauthorized']");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SoftRefreshKeepsBothPagerDirectionsMountedAndRejectsQueuedClicks(bool search)
    {
        var held = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = new List<TenantListRequest>();
        TenantListSnapshot first = SoftRefreshSnapshot(search, "tenant.first");
        TenantListSnapshot second = SoftRefreshSnapshot(search, "tenant.second");
        RegisterSoftRefreshServices(call =>
        {
            requests.Add(call.ArgAt<TenantListRequest>(0));
            return requests.Count switch
            {
                1 => Task.FromResult(first),
                2 => Task.FromResult(second),
                3 => held.Task,
                _ => Task.FromResult(first),
            };
        });
        Services.GetRequiredService<NavigationManager>().NavigateTo(search ? "/tenants?search=needle" : "/tenants");
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        await cut.Find("[data-testid='tenants-list-next']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => requests.Count.ShouldBe(2));
        EventCallback<MouseEventArgs> previous = SoftRefreshButton(cut, "tenants-list-previous").OnClick;
        EventCallback<MouseEventArgs> next = SoftRefreshButton(cut, "tenants-list-next").OnClick;
        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeFalse();
        string pageTwoUrl = Services.GetRequiredService<NavigationManager>().Uri;
        Task refresh = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        try
        {
            cut.WaitForAssertion(() => requests.Count.ShouldBe(3));
            cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
            cut.Find("[data-testid='tenants-list-next']").HasAttribute("disabled").ShouldBeTrue();
            await cut.InvokeAsync(() => previous.InvokeAsync(new MouseEventArgs())).WaitAsync(TimeSpan.FromSeconds(5));
            await cut.InvokeAsync(() => next.InvokeAsync(new MouseEventArgs())).WaitAsync(TimeSpan.FromSeconds(5));
            requests.Count.ShouldBe(3);
            Services.GetRequiredService<NavigationManager>().Uri.ShouldBe(pageTwoUrl);
            (search ? requests[2].SearchCursor : requests[2].Cursor).ShouldBe("next-page");
            requests[2].ETag.ShouldBe(second.ETag);
        }
        finally
        {
            held.TrySetResult(second);
        }

        await refresh.WaitAsync(TimeSpan.FromSeconds(5));
        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("[data-testid='tenants-list-next']").HasAttribute("disabled").ShouldBeFalse();
        await cut.Find("[data-testid='tenants-list-previous']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => requests.Count.ShouldBe(4));
        (search ? requests[3].SearchCursor : requests[3].Cursor).ShouldBeNull();
        cut.Find("[data-testid='tenants-list-previous']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Theory]
    [InlineData(TenantListSurfaceKind.Error)]
    [InlineData(TenantListSurfaceKind.Unauthorized)]
    [InlineData(TenantListSurfaceKind.Empty)]
    public async Task SoftRefreshAppliesTerminalOutcomeAndNeverResurrectsOlderRows(TenantListSurfaceKind outcome)
    {
        var held = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var terminalRead = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        TenantListSnapshot terminal = outcome switch
        {
            TenantListSurfaceKind.Unauthorized => TenantListSnapshot.Unauthorized(),
            TenantListSurfaceKind.Empty => TenantListSnapshot.Empty(true, ReadModelFreshnessState.Current),
            _ => TenantListSnapshot.Error(),
        };
        RegisterSoftRefreshServices(_ => ++calls switch
        {
            1 => Task.FromResult(SoftRefreshSnapshot(false)),
            2 => terminalRead.Task,
            _ => held.Task,
        });
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        Task terminalRefresh = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        try
        {
            cut.WaitForAssertion(() => calls.ShouldBe(2));
            cut.Find("[data-testid='tenants-list-grid']");
            cut.Find("[data-testid='tenants-list-loading']");
        }
        finally
        {
            terminalRead.TrySetResult(terminal);
        }
        await terminalRefresh.WaitAsync(TimeSpan.FromSeconds(5));
        cut.FindAll("[data-testid='tenants-list-grid']").ShouldBeEmpty();
        string expectedId = $"tenants-list-{outcome.ToString().ToLowerInvariant()}";
        cut.Find($"[data-testid='{expectedId}']");
        Task refresh = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        try
        {
            cut.WaitForAssertion(() => calls.ShouldBe(3));
            cut.Find("[data-testid='tenants-list-loading']");
            cut.FindAll("[data-testid='tenants-list-grid']").ShouldBeEmpty();
            cut.Markup.ShouldNotContain("tenant.alpha");
        }
        finally
        {
            held.TrySetResult(terminal);
        }

        await refresh.WaitAsync(TimeSpan.FromSeconds(5));
        cut.Find($"[data-testid='{expectedId}']");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("operator.other")]
    public async Task SoftRefreshCannotRetainRowsForMissingOrDifferentCaller(string? caller)
    {
        var held = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        RegisterSoftRefreshServices(_ => ++calls == 1 ? Task.FromResult(SoftRefreshSnapshot(false)) : held.Task);
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        Services.GetRequiredService<IUserContextAccessor>().UserId.Returns(caller);
        Task refresh = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        try
        {
            cut.WaitForAssertion(() => calls.ShouldBe(2));
            cut.Find("[data-testid='tenants-list-loading']");
            cut.FindAll("[data-testid='tenants-list-grid']").ShouldBeEmpty();
            cut.Markup.ShouldNotContain("tenant.alpha");
        }
        finally
        {
            held.TrySetResult(TenantListSnapshot.Unauthorized());
        }

        await refresh.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("foreign-scope")]
    public async Task SoftRefreshRequiresMatchingSnapshotCallerEvidence(string? scope)
    {
        var held = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        RegisterSoftRefreshServices(_ => ++calls == 1
            ? Task.FromResult(SoftRefreshSnapshot(false) with { CallerScope = scope }) : held.Task);
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        Task refresh = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        try
        {
            cut.WaitForAssertion(() => calls.ShouldBe(2));
            cut.FindAll("[data-testid='tenants-list-grid']").ShouldBeEmpty();
            cut.Find("[data-testid='tenants-list-loading']");
        }
        finally
        {
            held.TrySetResult(TenantListSnapshot.Error());
        }

        await refresh.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SoftRefreshDuringInitialLoadCannotInventConfirmedRows()
    {
        var held = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        RegisterSoftRefreshServices(_ =>
        {
            calls++;
            return held.Task;
        });
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        cut.WaitForAssertion(() => calls.ShouldBe(1));
        cut.Find("[data-testid='tenants-list-loading']");
        cut.FindAll("[data-testid='tenants-list-grid']").ShouldBeEmpty();
        Task refresh = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        try
        {
            cut.WaitForAssertion(() => calls.ShouldBe(2));
            cut.FindAll("[data-testid='tenants-list-grid']").ShouldBeEmpty();
        }
        finally
        {
            held.TrySetResult(SoftRefreshSnapshot(false));
        }

        await refresh.WaitAsync(TimeSpan.FromSeconds(5));
        cut.WaitForElement("[data-testid='tenants-list-grid']");
    }

    [Fact]
    public async Task SoftRefreshCannotRestorePreviousScopeDuringPendingHardLoad()
    {
        var held = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        RegisterSoftRefreshServices(_ => ++calls == 1 ? Task.FromResult(SoftRefreshSnapshot(true)) : held.Task);
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants?search=needle");
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        Task reset = cut.Find("[data-testid='tenants-list-reset']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => calls.ShouldBe(2));
        Task refresh = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        try
        {
            cut.WaitForAssertion(() => calls.ShouldBe(3));
            cut.FindAll("[data-testid='tenants-list-grid']").ShouldBeEmpty();
            cut.Find("[data-testid='tenants-list-loading']");
        }
        finally
        {
            held.TrySetResult(SoftRefreshSnapshot(false, "tenant.new-scope"));
        }

        await Task.WhenAll(reset, refresh).WaitAsync(TimeSpan.FromSeconds(5));
        cut.FindComponent<TenantDataGrid>().Instance.Rows.Select(row => row.TenantId).ShouldBe(["tenant.new-scope"]);
    }

    [Fact]
    public async Task SoftRefreshSupersessionRetainsRowsAndRejectsLateCompletion()
    {
        var older = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var newer = new TaskCompletionSource<TenantListSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = new List<(TenantListRequest Request, TenantListSnapshot? Previous)>();
        TenantListSnapshot original = SoftRefreshSnapshot(false);
        RegisterSoftRefreshServices(call =>
        {
            requests.Add((call.ArgAt<TenantListRequest>(0), call.ArgAt<TenantListSnapshot?>(1)));
            return requests.Count switch
            {
                1 => Task.FromResult(original),
                2 => older.Task,
                _ => newer.Task,
            };
        });
        IRenderedComponent<TenantsWorkspace> cut = RenderWorkspace();
        cut.WaitForElement("[data-testid='tenants-list-grid']");
        TenantDataGrid grid = cut.FindComponent<TenantDataGrid>().Instance;
        Task first = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => requests.Count.ShouldBe(2));
        Task second = cut.Find("[data-testid='tenants-list-refresh']").ClickAsync(new MouseEventArgs());
        try
        {
            cut.WaitForAssertion(() => requests.Count.ShouldBe(3));
            cut.FindComponent<TenantDataGrid>().Instance.ShouldBeSameAs(grid);
            foreach (var request in requests.Skip(1))
            {
                request.Previous.ShouldBeSameAs(original);
                request.Request.ETag.ShouldBe(original.ETag);
            }
            newer.SetResult(SoftRefreshSnapshot(false, "tenant.latest"));
            await second.WaitAsync(TimeSpan.FromSeconds(5));
            grid.Rows.Select(row => row.TenantId).ShouldBe(["tenant.latest"]);
        }
        finally
        {
            newer.TrySetResult(TenantListSnapshot.Error());
            older.TrySetResult(SoftRefreshSnapshot(false, "tenant.obsolete"));
        }

        await first.WaitAsync(TimeSpan.FromSeconds(5));
        grid.Rows.Select(row => row.TenantId).ShouldBe(["tenant.latest"]);
        cut.Markup.ShouldNotContain("tenant.obsolete");
    }

    private void RegisterSoftRefreshServices(Func<NSubstitute.Core.CallInfo, Task<TenantListSnapshot>> response)
    {
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.ListTenantsAsync(Arg.Any<TenantListRequest>(), Arg.Any<TenantListSnapshot?>(), Arg.Any<CancellationToken>())
            .Returns(response);
        Services.AddSingleton(gateway);
        Services.AddSingleton<AuthenticationStateProvider>(new MutableAuthenticationStateProvider(AdministratorPrincipal()));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddFluentUIComponents();
    }

    private static TenantListSnapshot SoftRefreshSnapshot(bool search, string? tenantId = null)
    {
        string[] ids = tenantId is null ? ["tenant.zeta", "tenant.alpha"] : [tenantId];
        TenantListRow[] rows = ids.Select(id => TenantListRow.FromSummary(new TenantSummary(id, id, TenantStatus.Active)) with
        {
            Freshness = ReadModelFreshnessState.Current,
            Lifecycle = ProjectionLifecycleState.Current,
        }).ToArray();
        return TenantListSnapshot.Ready(rows, "next-page", true, "\"retained-etag\"", ReadModelFreshnessState.Current, false) with
        {
            IsAuthoritativeSearch = search,
            Lifecycle = ProjectionLifecycleState.Current,
            ProjectionVersion = "list-version-7",
            CallerScope = TenantQueryGateway.AuditCallerScope("operator.alpha"),
        };
    }

    private static FluentButton SoftRefreshButton(IRenderedComponent<TenantsWorkspace> cut, string testId)
        => cut.FindComponents<FluentButton>().Select(component => component.Instance)
            .Single(button => button.AdditionalAttributes is { } attributes
                && attributes.TryGetValue("data-testid", out object? actual)
                && string.Equals(actual as string, testId, StringComparison.Ordinal));
}
