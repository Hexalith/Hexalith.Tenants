using System.Globalization;
using System.Security.Claims;

using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantUsers;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed class GlobalAdministratorsPageTests : FluentBunitContext
{
    [Fact]
    public void Authentication_revocation_collapses_privileged_state_without_another_query()
    {
        var authentication = new MutableAuthenticationStateProvider(GlobalAdministratorPrincipal());
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.before", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"before\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin.before");

        authentication.Set(NonAdministratorPrincipal());
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admins-unavailable']");
            cut.Markup.ShouldNotContain("admin.before");
            cut.FindAll("[data-testid='tenants-global-admins-list']").ShouldBeEmpty();
        });
        gateway.GlobalAdministratorCalls.ShouldBe(1);
    }

    [Fact]
    public void Authentication_restore_requeries_after_claim_correct_authorization()
    {
        var authentication = new MutableAuthenticationStateProvider(NonAdministratorPrincipal());
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.after", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"after\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Indeterminate));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.Find("[data-testid='tenants-global-admins-unavailable']");
        gateway.GlobalAdministratorCalls.ShouldBe(0);

        authentication.Set(GlobalAdministratorPrincipal());
        cut.WaitForAssertion(
            () => gateway.GlobalAdministratorCalls.ShouldBe(1),
            TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin.after");
            cut.Find("[data-testid='tenants-global-admins-list']");
        }, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Authorized_operator_sees_global_administrators_from_fixed_scope()
    {
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("platform-admin.alpha", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        BunitJSModuleInterop module = JSInterop.SetupModule("./js/tenantsClipboard.js");
        JSRuntimeInvocationHandler writeHandler = module.SetupVoid("writeText", "platform-admin.alpha").SetVoidResult();

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        gateway.GlobalAdministratorCalls.ShouldBe(1);
        cut.Find("[data-testid='tenants-global-admins-area']");
        cut.Find("[data-testid='tenants-global-admins-scope']").TextContent.ShouldContain("global-administrators");
        cut.Find("[data-testid='tenants-global-admins-scope']").TextContent.ShouldContain("system");
        cut.Find("[data-testid='tenants-global-admins-list']");
        cut.Find("[data-testid='tenants-global-admins-row']");
        cut.Find("[data-testid='tenants-global-admins-mobile-readonly']");
        cut.FindAll(".global-admins__mutation-section").Count.ShouldBe(2);
        cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("platform-admin.alpha");
        cut.Find("[data-testid='tenants-global-admins-authority-scope']").TextContent.ShouldContain("Platform authority");
        cut.Find("[data-testid='tenants-global-admins-action-reasons']").TextContent.ShouldContain("Grant is available");
        cut.Find("[data-testid='tenants-global-admins-live-region']").GetAttribute("aria-live").ShouldBeNull();
        cut.Markup.ShouldNotContain("/api/tenants", Case.Insensitive);
        cut.Markup.ShouldNotContain("/api/users", Case.Insensitive);
        cut.Find("[data-testid='tenants-global-admins-authority-scope']").TextContent.ShouldContain("not tenant ownership");
        cut.Markup.ShouldContain("data-testid=\"tenants-global-admins-list\"");

        cut.Find("[data-surface-testid='tenants-global-admins-copy-user-id']").Click();
        cut.WaitForAssertion(() => writeHandler.Invocations.Count.ShouldBe(1));
        writeHandler.Invocations.Single().Arguments[0].ShouldBe("platform-admin.alpha");
    }

    [Fact]
    public async Task Tenant_owner_without_platform_authority_gets_fail_closed_without_querying_or_subscribing()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("hidden-admin", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
            ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Indeterminate));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        gateway.GlobalAdministratorCalls.ShouldBe(0);
        cut.Find("[data-testid='tenants-global-admins-unavailable']").GetAttribute("role").ShouldBe("alert");
        cut.Find("[data-testid='tenants-global-admins-denied-message']").TextContent.ShouldContain("fails closed");
        cut.Markup.ShouldNotContain("hidden-admin");
        cut.Markup.ShouldNotContain("tenants-global-admins-list");
        cut.Markup.ShouldNotContain("tenants-global-admins-scope");
        cut.Markup.ShouldNotContain("tenants-global-admin-grant");
        cut.Markup.ShouldNotContain("tenants-global-admin-remove");
        cut.Markup.ShouldNotContain("global-administrators");
        cut.Markup.ShouldNotContain("system");
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
        await subscription.DidNotReceive()
            .SubscribeAsync(
                GetGlobalAdministratorsQuery.ProjectionType,
                "system",
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Server_denial_after_optimistic_reflection_discards_all_privileged_markup()
    {
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Unauthorized());
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        gateway.GlobalAdministratorCalls.ShouldBe(1);
        cut.Find("[data-testid='tenants-global-admins-unavailable']");
        cut.Markup.ShouldNotContain("tenants-global-admins-list");
        cut.Markup.ShouldNotContain("tenants-global-admins-scope");
        cut.Markup.ShouldNotContain("tenants-global-admin-grant");
        cut.Markup.ShouldNotContain("tenants-global-admin-remove");
    }

    [Fact]
    public async Task Notification_reauthorization_denial_disposes_the_lease_and_clears_privileged_state()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("must-be-cleared", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
            ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        await subscription.Received(1)
            .SubscribeAsync(
                GetGlobalAdministratorsQuery.ProjectionType,
                "system",
                Arg.Any<CancellationToken>());

        composition.Reflection = TenantLifecycleAuthorizationReflectionState.MissingPermission;
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(
            GetGlobalAdministratorsQuery.ProjectionType,
            "system");

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admins-unavailable']");
            cut.Markup.ShouldNotContain("must-be-cleared");
            cut.Markup.ShouldNotContain("tenants-global-admins-scope");
            cut.Markup.ShouldNotContain("tenants-global-admin-grant");
            cut.Markup.ShouldNotContain("tenants-global-admin-remove");
        });
        gateway.GlobalAdministratorCalls.ShouldBe(1);
        await subscription.Received(1)
            .UnsubscribeAsync(
                GetGlobalAdministratorsQuery.ProjectionType,
                "system",
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Notification_refreshing_affordance_retains_rows_until_the_authoritative_read_completes()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var pending = new TaskCompletionSource<GlobalAdministratorsSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.confirmed", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"confirmed\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        await subscription.Received(1).SubscribeAsync(
            GetGlobalAdministratorsQuery.ProjectionType,
            "system",
            Arg.Any<CancellationToken>());
        gateway.QueueResponse(pending.Task);

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(
            GetGlobalAdministratorsQuery.ProjectionType,
            "system");

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admins-notification-refreshing']")
                .GetAttribute("role").ShouldBe("status");
            cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin.confirmed");
        });

        pending.SetResult(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.refreshed", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"refreshed\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("[data-testid='tenants-global-admins-notification-refreshing']").ShouldBeEmpty();
            cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin.refreshed");
        });
    }

    [Fact]
    public async Task Recovery_affordances_announce_page_recovery_and_execute_reset_then_retry()
    {
        GlobalAdministratorsSnapshot recoverable = GlobalAdministratorsSnapshot.Error() with
        {
            PagingRecovered = true,
            Reason = GlobalAdministratorsReason.PageRecovered,
        };
        var gateway = new StubTenantQueryGateway(
            recoverable,
            GlobalAdministratorsSnapshot.Error(),
            GlobalAdministratorsSnapshot.Empty(
                isAuthorizationScoped: true,
                ReadModelFreshnessState.Current,
                eTag: "\"recovered\""));
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-page-recovered']")
            .GetAttribute("aria-live").ShouldBe("polite");
        await cut.Find("[data-testid='tenants-global-admins-reset']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(2));
        gateway.Requests[1].Cursor.ShouldBeNull();
        gateway.Requests[1].ETag.ShouldBeNull();

        await cut.Find("[data-testid='tenants-global-admins-retry']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() =>
        {
            gateway.GlobalAdministratorCalls.ShouldBe(3);
            cut.Find("[data-testid='tenants-global-admins-empty']");

            // Recovery affordances must SURVIVE an empty page. ShouldRenderRows requires Rows.Count > 0 and
            // owns the pager, Refresh and grid, so an Empty page renders no controls of its own; if Retry and
            // Reset also disappeared, an operator who paged into a page emptied by a concurrent removal would
            // be stranded on that cursor with only a browser reload to recover.
            cut.FindAll("[data-testid='tenants-global-admins-retry']").ShouldNotBeEmpty();
            cut.FindAll("[data-testid='tenants-global-admins-reset']").ShouldNotBeEmpty();
        });
        gateway.Requests[2].ETag.ShouldBeNull();
    }

    [Fact]
    public async Task Newer_notification_refresh_rejects_a_late_cursor_result_and_preserves_cursor_history()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var latePage = new TaskCompletionSource<GlobalAdministratorsSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("first-admin", ReadModelFreshnessState.Current)],
                "protected-next",
                true,
                "\"first-etag\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current },
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("newest-admin", ReadModelFreshnessState.Current)],
                null,
                false,
                "\"newest-etag\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        await subscription.Received(1)
            .SubscribeAsync(
                GetGlobalAdministratorsQuery.ProjectionType,
                "system",
                Arg.Any<CancellationToken>());
        gateway.QueueResponse(latePage.Task);

        Task nextNavigation = cut.Find("[data-testid='tenants-global-admins-next']")
            .ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(2));

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(
            GetGlobalAdministratorsQuery.ProjectionType,
            "system");
        cut.WaitForAssertion(() =>
        {
            gateway.GlobalAdministratorCalls.ShouldBe(3);
            cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("newest-admin");
        });

        latePage.SetResult(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("late-admin", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"late-etag\"",
            ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        await nextNavigation;

        cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("newest-admin");
        cut.Markup.ShouldNotContain("late-admin");
        gateway.Requests[1].Cursor.ShouldBe("protected-next");
        gateway.Requests[2].Cursor.ShouldBeNull();
        cut.Find("[data-testid='tenants-global-admins-previous']").HasAttribute("disabled").ShouldBeTrue();

        await cut.Instance.DisposeAsync();
        await subscription.Received(1)
            .UnsubscribeAsync(
                GetGlobalAdministratorsQuery.ProjectionType,
                "system",
                Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("/tenants?search=alpha&sort=name", "/tenants?search=alpha&sort=name")]
    [InlineData("https://evil.example/tenants", "/tenants")]
    [InlineData("//evil.example/tenants", "/tenants")]
    [InlineData("/tenants?cursor=protected-secret", "/tenants")]
    public void Return_navigation_accepts_only_cursor_free_canonical_workspace_urls(string supplied, string expected)
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Empty(
            isAuthorizationScoped: true,
            ReadModelFreshnessState.Current,
            eTag: "\"empty\"")));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(navigation.GetUriWithQueryParameter("returnUrl", supplied));

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-return']").GetAttribute("href").ShouldBe(expected);
        cut.Markup.ShouldNotContain("protected-secret");
        cut.Markup.ShouldNotContain("evil.example");
    }

    [Theory]
    [InlineData("all-tenants")]
    [InlineData("my-tenants")]
    [InlineData("users")]
    public void Every_cursor_free_canonical_workspace_shape_is_accepted_as_return_context(string shape)
    {
        TenantWorkspaceState state = shape switch
        {
            "all-tenants" => TenantWorkspaceState.FromQuery(
                TenantWorkspaceState.TenantsTab,
                TenantWorkspaceState.AllScope,
                userId: null,
                search: "alpha beta",
                status: TenantStatus.Active.ToString(),
                sort: TenantListSortColumns.Name,
                sortDescending: bool.TrueString,
                cursor: null,
                selectedTenantId: "tenant.alpha",
                anchor: "tenant-row-tenant.alpha"),
            "my-tenants" => TenantWorkspaceState.FromQuery(
                TenantWorkspaceState.TenantsTab,
                TenantWorkspaceState.MyScope,
                userId: null,
                search: null,
                status: null,
                sort: null,
                sortDescending: null,
                cursor: null,
                selectedTenantId: "tenant.mine",
                anchor: "tenant-row-tenant.mine"),
            "users" => TenantWorkspaceState.FromQuery(
                TenantWorkspaceState.UsersTab,
                TenantWorkspaceState.AllScope,
                userId: "target/user",
                search: null,
                status: null,
                sort: UserTenantMembershipSortColumns.Role,
                sortDescending: null,
                cursor: null,
                selectedTenantId: null,
                anchor: null),
            _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
        };
        string canonical = state.ToCanonicalUrl();
        canonical.ShouldNotBe("/tenants");
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Empty(
            isAuthorizationScoped: true,
            ReadModelFreshnessState.Current,
            eTag: "\"empty\"")));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(navigation.GetUriWithQueryParameter("returnUrl", canonical));

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-return']").GetAttribute("href").ShouldBe(canonical);
    }

    /// <summary>
    /// Client-local paging history is not evidence that other administrators exist.
    /// </summary>
    /// <remarks>
    /// On a later page the completeness gate is false, so the LastAdmin branch is skipped. The population
    /// check then accepted a non-empty cursor history — which only records that this circuit navigated
    /// forward at some point — and enabled Remove against what may be the platform's last global
    /// administrator. Only server-stated evidence (complete evidence with more than one row, or HasMore)
    /// may unlock removal.
    /// </remarks>
    [Fact]
    public void Single_row_page_without_complete_evidence_keeps_removal_unavailable()
    {
        GlobalAdministratorsSnapshot lastPage = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            IsCompleteEvidence = false,
        };
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(lastPage));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.FindAll("[data-testid='tenants-global-admins-remove-reason']").ShouldNotBeEmpty(
            "removal must fail closed when no server evidence proves another administrator exists");
        cut.FindAll("[data-testid='tenants-global-admin-remove']").ShouldBeEmpty(
            "the remove launcher must not render while removal is unavailable");
    }

    [Fact]
    public void Incomplete_current_page_with_more_results_allows_safe_initiation()
    {
        GlobalAdministratorsSnapshot incomplete = GlobalAdministratorsSnapshot.Ready(
            [
                new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current),
                new GlobalAdministratorRow("admin-2", ReadModelFreshnessState.Current),
            ],
            nextCursor: "opaque-next",
            hasMore: true,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            IsCompleteEvidence = false,
        };
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(incomplete));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.FindAll("[data-testid='tenants-global-admin-grant-unavailable-reason']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-global-admin-remove']").Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(GlobalAdministratorsSurfaceKind.Stale, ReadModelFreshnessState.Stale, "stale")]
    [InlineData(GlobalAdministratorsSurfaceKind.Degraded, ReadModelFreshnessState.Unknown, "degraded")]
    public void Stale_or_degraded_review_surface_keeps_rows_visible_and_actions_unavailable(
        GlobalAdministratorsSurfaceKind kind,
        ReadModelFreshnessState freshness,
        string expectedReason)
    {
        GlobalAdministratorsSnapshot snapshot = kind is GlobalAdministratorsSurfaceKind.Stale
            ? GlobalAdministratorsSnapshot.Stale([new GlobalAdministratorRow("admin-1", freshness)], null, false, "\"etag\"")
            : GlobalAdministratorsSnapshot.Degraded([new GlobalAdministratorRow("admin-1", freshness)], GlobalAdministratorsReason.ProjectionDegraded, "\"etag\"");
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(snapshot));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-list']");
        cut.Find("[data-testid='tenants-global-admins-row']").TextContent.ShouldContain("admin-1");
        cut.Find("[data-testid='tenants-global-admins-live-region']").TextContent.ShouldContain(expectedReason, Case.Insensitive);
        cut.Find("[data-testid='tenants-global-admins-grant-reason']").TextContent.ShouldContain("freshness", Case.Insensitive);
        cut.Find("[data-testid='tenants-global-admins-remove-reason']").TextContent.ShouldContain("freshness", Case.Insensitive);
    }

    [Theory]
    [InlineData(GlobalAdministratorsSurfaceKind.Empty, "tenants-global-admins-empty", "No global administrators")]
    [InlineData(GlobalAdministratorsSurfaceKind.Unknown, "tenants-global-admins-unknown", "truth unknown")]
    [InlineData(GlobalAdministratorsSurfaceKind.Error, "tenants-global-admins-error", "review failed")]
    [InlineData(GlobalAdministratorsSurfaceKind.Invalid, "tenants-global-admins-invalid", "Invalid global administrator page")]
    [InlineData(GlobalAdministratorsSurfaceKind.Unavailable, "tenants-global-admins-unavailable", "Global administrator data unavailable")]
    public void Empty_invalid_and_unavailable_states_do_not_render_false_success_or_hidden_rows(
        GlobalAdministratorsSurfaceKind kind,
        string expectedTestId,
        string expectedCopy)
    {
        GlobalAdministratorsSnapshot snapshot = kind switch
        {
            GlobalAdministratorsSurfaceKind.Empty => GlobalAdministratorsSnapshot.Empty(
                isAuthorizationScoped: true,
                ReadModelFreshnessState.Current,
                "\"empty\""),
            GlobalAdministratorsSurfaceKind.Unknown => GlobalAdministratorsSnapshot.Unknown([], null, false, null),
            GlobalAdministratorsSurfaceKind.Error => GlobalAdministratorsSnapshot.Error(),
            GlobalAdministratorsSurfaceKind.Invalid => GlobalAdministratorsSnapshot.Invalid(),
            GlobalAdministratorsSurfaceKind.Unavailable => GlobalAdministratorsSnapshot.Unavailable(),
            _ => throw new InvalidOperationException($"Unsupported state {kind}."),
        };
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(snapshot));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find($"[data-testid='{expectedTestId}']").TextContent.ShouldContain(expectedCopy);
        cut.Markup.ShouldNotContain("tenants-global-admins-row");
        cut.Markup.ShouldNotContain("hidden-admin", Case.Insensitive);
        cut.Markup.ShouldNotContain("success", Case.Insensitive);
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
    }

    [Fact]
    public void Refresh_reuses_etag_and_preserves_previous_snapshot_for_server_side_bff_query()
    {
        var gateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
                nextCursor: null,
                hasMore: false,
                eTag: "\"etag-1\"",
                freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current },
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
                nextCursor: null,
                hasMore: false,
                eTag: "\"etag-1\"",
                freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-refresh']").Click();

        gateway.GlobalAdministratorCalls.ShouldBe(2);
        gateway.Requests[0].ETag.ShouldBeNull();
        gateway.Requests[1].ETag.ShouldBe("\"etag-1\"");
        gateway.PreviousSnapshots[1].ShouldNotBeNull().Rows.ShouldHaveSingleItem().UserId.ShouldBe("admin-1");
        cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin-1");
    }

    [Fact]
    public void Next_page_uses_protected_cursor_without_offset_or_tenant_substitute_markers()
    {
        var gateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
                nextCursor: "protected-next-cursor",
                hasMore: true,
                eTag: "\"etag-1\"",
                freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current },
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-2", ReadModelFreshnessState.Current)],
                nextCursor: null,
                hasMore: false,
                eTag: "\"etag-2\"",
                freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-next']").Click();

        gateway.GlobalAdministratorCalls.ShouldBe(2);
        gateway.Requests[0].Cursor.ShouldBeNull();
        gateway.Requests[0].PageSize.ShouldBe(20);
        gateway.Requests[1].Cursor.ShouldBe("protected-next-cursor");
        gateway.Requests[1].ETag.ShouldBeNull();
        cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin-2");
        cut.Markup.ShouldNotContain("offset", Case.Insensitive);
        cut.Markup.ShouldNotContain("/api/tenants", Case.Insensitive);
        cut.Markup.ShouldNotContain("/api/users", Case.Insensitive);
    }

    [Fact]
    public void Grant_flow_renders_fixed_scope_form_without_tenant_membership_inputs()
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true }));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant']");
        cut.Find("[data-testid='tenants-global-admin-grant-user-id']");
        cut.Find("[data-testid='tenants-global-admin-grant-scope']").TextContent.ShouldContain("system");
        cut.Find("[data-testid='tenants-global-admin-grant-scope']").TextContent.ShouldContain("global-administrators");
        cut.Markup.ShouldNotContain("TenantRole", Case.Insensitive);
        cut.Markup.ShouldNotContain("tenant-member", Case.Insensitive);
        cut.Markup.ShouldNotContain("member table", Case.Insensitive);
    }

    [Fact]
    public void Last_global_administrator_remove_is_unavailable_without_confirmation_affordance()
    {
        var commandGateway = new StubTenantCommandGateway();
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("only-admin", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true }));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-remove-reason']").TextContent.ShouldContain("last global administrator", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-global-admin-remove']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("override", Case.Insensitive);
        cut.Markup.ShouldNotContain("elevated friction", Case.Insensitive);
        commandGateway.RemoveGlobalAdministratorCalls.ShouldBe(0);
    }

    [Fact]
    public void Remove_preview_renders_fixed_scope_consequences_before_submission()
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [
                new GlobalAdministratorRow("target-admin", ReadModelFreshnessState.Current),
                new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current),
            ],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true }));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-remove']").Click();

        cut.Find("[data-testid='tenants-global-admin-remove-preview']").TextContent.ShouldContain("target-admin");
        cut.Find("[data-testid='tenants-global-admin-remove-preview']").TextContent.ShouldContain("system");
        cut.Find("[data-testid='tenants-global-admin-remove-preview']").TextContent.ShouldContain("global-administrators");
        cut.Find("[data-testid='tenants-global-admin-remove-preview']").TextContent.ShouldContain("2");
        cut.Find("[data-testid='tenants-global-admin-remove-known-consequences']").TextContent.ShouldContain("platform authority", Case.Insensitive);
        cut.Find("[data-testid='tenants-global-admin-remove-known-unknowns']").TextContent.ShouldContain("token invalidation", Case.Insensitive);
        cut.Find("[data-testid='tenants-global-admin-remove-audit-expectation']").TextContent.ShouldContain("audit", Case.Insensitive);
        cut.Find("[data-testid='tenants-global-admin-remove-recovery']").TextContent.ShouldContain("grant", Case.Insensitive);
        cut.Find("[data-testid='tenants-global-admin-remove-submit']").HasAttribute("disabled").ShouldBeFalse();
        cut.Markup.ShouldNotContain("tenant-member", Case.Insensitive);
    }

    /// <summary>
    /// The last-administrator hard stop must bind at SUBMIT, not only at preview time.
    /// </summary>
    /// <remarks>
    /// A projection notification replaces the snapshot underneath a Previewed intent without resetting the
    /// remove flow, so a preview taken while two administrators were visible could be submitted after the
    /// page had refreshed to a complete single-administrator page. IsRemoveSubmitDisabled consulted only the
    /// lifecycle state, and SubmitRemoveAsync re-checked only authorization -- the grant path already
    /// re-evaluated its full unavailable reason, so the asymmetry was the defect.
    /// </remarks>
    [Fact]
    public async Task Remove_submission_is_blocked_when_a_refresh_makes_the_target_the_last_administrator()
    {
        var commandGateway = new StubTenantCommandGateway();
        var gateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [
                    new GlobalAdministratorRow("target-admin", ReadModelFreshnessState.Current),
                    new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current),
                ],
                nextCursor: null,
                hasMore: false,
                eTag: "\"etag\"",
                freshness: ReadModelFreshnessState.Current) with
            { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true },
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("target-admin", ReadModelFreshnessState.Current)],
                nextCursor: null,
                hasMore: false,
                eTag: "\"etag-2\"",
                freshness: ReadModelFreshnessState.Current) with
            { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true });
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-remove']").Click();
        cut.Find("[data-testid='tenants-global-admin-remove-submit']").HasAttribute("disabled").ShouldBeFalse();

        // Refresh under the open preview: the page now holds a complete single-administrator page.
        await cut.Find("[data-testid='tenants-global-admins-refresh']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(2));

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='tenants-global-admin-remove-submit']").HasAttribute("disabled").ShouldBeTrue());
        commandGateway.RemoveGlobalAdministratorCalls.ShouldBe(0);
    }

    [Fact]
    public void Remove_preview_escape_cancels_without_submission_and_exposes_focus_sentinels()
    {
        var commandGateway = new StubTenantCommandGateway();
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [
                new GlobalAdministratorRow("target-admin", ReadModelFreshnessState.Current),
                new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current),
            ],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true }));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-remove']").Click();
        cut.Find("[data-testid='tenants-global-admin-remove-preview']").GetAttribute("role").ShouldBe("dialog");
        cut.Find("[data-testid='tenants-global-admin-remove-preview']").GetAttribute("aria-modal").ShouldBe("true");
        cut.Find("[data-testid='tenants-global-admin-remove-focus-start']").GetAttribute("tabindex").ShouldBe("0");
        cut.Find("[data-testid='tenants-global-admin-remove-focus-end']").GetAttribute("tabindex").ShouldBe("0");

        cut.Find("[data-testid='tenants-global-admin-remove-preview']").KeyDown("Escape");

        commandGateway.RemoveGlobalAdministratorCalls.ShouldBe(0);
        cut.FindAll("[data-testid='tenants-global-admin-remove-preview']").ShouldBeEmpty();
        cut.Find("[data-testid='tenants-global-admin-remove-state']").TextContent.ShouldContain("No global administrator remove command");
    }

    [Fact]
    public void Remove_submission_confirms_only_after_projection_requery_excludes_target_user()
    {
        var queryGateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [
                    new GlobalAdministratorRow("target-admin", ReadModelFreshnessState.Current),
                    new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current),
                ],
                null,
                false,
                "\"etag-1\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true },
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current)],
                null,
                false,
                "\"etag-2\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true });
        var commandGateway = new StubTenantCommandGateway(statuses: [new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1)])
        {
            RemoveSubmission = TenantCommandSubmissionResult.Accepted("message-remove", "correlation-remove"),
        };
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-remove']").Click();
        cut.Find("[data-testid='tenants-global-admin-remove-submit']").Click();

        cut.WaitForAssertion(() =>
        {
            commandGateway.RemoveGlobalAdministratorCalls.ShouldBe(1);
            commandGateway.RemoveRequests.ShouldHaveSingleItem().UserId.ShouldBe("target-admin");
            queryGateway.GlobalAdministratorCalls.ShouldBe(2);
            cut.Find("[data-testid='tenants-global-admin-remove-state']").TextContent.ShouldContain("Projection confirmed removal");
            cut.Find("[data-testid='tenants-global-admin-remove-live-region']").GetAttribute("aria-live").ShouldBe("polite");
            cut.FindAll("[data-testid='tenants-global-admins-user-id']").Select(static element => element.TextContent)
                .ShouldNotContain("target-admin");
        });
    }

    [Theory]
    [InlineData("LastGlobalAdministrator", "last global administrator")]
    [InlineData("GlobalAdministratorNotFound", "not a global administrator")]
    public void Remove_rejection_keeps_last_confirmed_rows_without_success_or_member_copy(
        string rejectionCode,
        string expectedText)
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [
                new GlobalAdministratorRow("target-admin", ReadModelFreshnessState.Current),
                new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current),
            ],
            null,
            false,
            "\"etag\"",
            ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true }));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway
        {
            RemoveSubmission = TenantCommandSubmissionResult.Rejected(expectedText, rejectionCode),
        });
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-remove']").Click();
        cut.Find("[data-testid='tenants-global-admin-remove-submit']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admin-remove-state']").TextContent.ShouldContain("rejected", Case.Insensitive);
            cut.Find("[data-testid='tenants-global-admin-remove-safe-message']").TextContent.ShouldContain(expectedText, Case.Insensitive);
            cut.Find("[data-testid='tenants-global-admin-remove-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
            cut.FindAll("[data-testid='tenants-global-admins-user-id']").Select(static element => element.TextContent)
                .ShouldContain("target-admin");
            // Visible text only — avoids the Fluent success-color token false positive (see VisibleText).
            cut.VisibleText().ShouldNotContain("success", Case.Insensitive);
            cut.Markup.ShouldNotContain("remove member", Case.Insensitive);
        });
    }

    [Fact]
    public void Blank_keyboard_form_submission_keeps_command_local_and_focuses_user_id_recovery()
    {
        var commandGateway = new StubTenantCommandGateway();
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current }));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        commandGateway.SetGlobalAdministratorCalls.ShouldBe(0);
        cut.Find("[data-testid='tenants-global-admin-grant-submit']").GetAttribute("type").ShouldBe("submit");
        cut.Find("[data-testid='tenants-global-admin-grant-validation']").TextContent.ShouldContain("User id is required");
        cut.Find("[data-testid='tenants-global-admin-grant-validation']").GetAttribute("role").ShouldBe("alert");
        cut.Find("[data-testid='tenants-global-admin-grant-user-id']")
            .GetAttribute("aria-describedby")
            .ShouldNotBeNull()
            .ShouldContain("tenants-global-admin-grant-validation");
        cut.Find("[data-testid='tenants-global-admin-grant-live-region']").GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void Grant_submission_confirms_only_after_projection_requery_contains_target_user()
    {
        var queryGateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
                null,
                false,
                "\"etag-1\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current },
            GlobalAdministratorsSnapshot.Ready(
                [
                    new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current),
                    new GlobalAdministratorRow("target-user", ReadModelFreshnessState.Current),
                ],
                null,
                false,
                "\"etag-2\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        var commandGateway = new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Accepted("message-grant", "correlation-grant"),
            new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("target-user");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        cut.WaitForAssertion(() =>
        {
            commandGateway.SetGlobalAdministratorCalls.ShouldBe(1);
            commandGateway.Requests.ShouldHaveSingleItem().UserId.ShouldBe("target-user");
            queryGateway.GlobalAdministratorCalls.ShouldBe(2);
            cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain("Projection confirmed the target user", Case.Insensitive);
            cut.Find("[data-testid='tenants-global-admin-grant-live-region']").GetAttribute("aria-live").ShouldBe("polite");
            cut.FindAll("[data-testid='tenants-global-admins-user-id']").Select(static element => element.TextContent)
                .ShouldContain("target-user");
        });
    }

    [Theory]
    [InlineData(false, true, "read projection")]
    [InlineData(true, false, "command surface")]
    public void Command_or_read_surface_unavailable_blocks_grant_without_command_submission(
        bool isReadSurfaceConnected,
        bool isCommandSurfaceConnected,
        string expectedReason)
    {
        var commandGateway = new StubTenantCommandGateway();
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(
            TenantLifecycleAuthorizationReflectionState.Authorized,
            isReadSurfaceConnected,
            isCommandSurfaceConnected));
        var queryGateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
            ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true });
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("target-user");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        commandGateway.SetGlobalAdministratorCalls.ShouldBe(0);
        if (!isReadSurfaceConnected)
        {
            queryGateway.GlobalAdministratorCalls.ShouldBe(0);
            cut.Markup.ShouldNotContain("admin-1");
        }

        cut.Find("[data-testid='tenants-global-admin-grant-submit']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-global-admin-grant-unavailable-reason']").TextContent.ShouldContain(expectedReason);
        cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain("could not be verified");
        cut.Find("[data-testid='tenants-global-admin-grant-audit-state']").TextContent.ShouldContain("Audit support");
        cut.Find("[data-testid='tenants-global-admin-grant-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
        cut.Markup.ShouldNotContain("access_token", Case.Insensitive);
        cut.Markup.ShouldNotContain("correlation-", Case.Insensitive);
    }

    // A projection-backed response carrying only the legacy X-Hexalith-Is-Stale: false compatibility signal
    // resolves to Current freshness with no projection lifecycle evidence. That is honest for display, but it
    // is not projection-confirmed evidence and must not unlock platform-authority mutations. HasMore is true
    // so the removal population gate is satisfied on its own: without the lifecycle requirement, Remove would
    // render as an enabled launcher.
    [Fact]
    public void Current_freshness_without_projection_lifecycle_evidence_blocks_grant_and_remove()
    {
        var commandGateway = new StubTenantCommandGateway();
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [
                new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current),
                new GlobalAdministratorRow("admin-2", ReadModelFreshnessState.Current),
            ],
            nextCursor: "opaque-next",
            hasMore: true,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Unknown }));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("target-user");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        commandGateway.SetGlobalAdministratorCalls.ShouldBe(0);
        cut.Find("[data-testid='tenants-global-admin-grant-submit']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-global-admin-grant-unavailable-reason']").TextContent
            .ShouldContain("Refresh projection freshness before granting platform authority.");

        cut.FindAll("[data-testid='tenants-global-admin-remove']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-global-admins-remove-reason']")
            .Select(static element => element.TextContent)
            .ShouldAllBe(static text => text.Contains("Refresh projection freshness before removing platform authority.", StringComparison.Ordinal));
    }

    [Fact]
    public void Cancel_grant_clears_literal_user_id_and_does_not_submit_command()
    {
        var commandGateway = new StubTenantCommandGateway();
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
            ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true }));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("target-user");
        cut.Find("[data-testid='tenants-global-admin-grant-cancel']").Click();

        commandGateway.SetGlobalAdministratorCalls.ShouldBe(0);
        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").GetAttribute("value").ShouldBeNullOrEmpty();
        cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain("No global administrator grant command");
        cut.Find("[data-testid='tenants-global-admin-grant-live-region']").GetAttribute("aria-live").ShouldBe("polite");
        cut.FindAll("[data-testid='tenants-global-admins-user-id']").Select(static element => element.TextContent)
            .ShouldNotContain("target-user");
    }

    [Fact]
    public void Completed_grant_without_projection_evidence_is_unable_to_verify_and_not_optimistic()
    {
        var queryGateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
                null,
                false,
                "\"etag-1\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current },
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
                null,
                false,
                "\"etag-2\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Accepted("message-grant", "correlation-grant"),
            new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1)));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("target-user");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain("could not be verified");
            cut.Find("[data-testid='tenants-global-admin-grant-safe-message']").TextContent.ShouldContain("did not confirm");
            cut.Find("[data-testid='tenants-global-admin-grant-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
            cut.FindAll("[data-testid='tenants-global-admins-user-id']").Select(static element => element.TextContent)
                .ShouldNotContain("target-user");
        });
    }

    [Theory]
    [InlineData(CommandStatus.PublishFailed, "degraded", "Audit evidence is delayed;")]
    [InlineData(CommandStatus.TimedOut, "could not be verified", "Audit evidence is delayed;")]
    public void Terminal_status_without_projection_confirmation_stays_distinct_and_assertive(
        CommandStatus status,
        string expectedStateText,
        string expectedAuditText)
    {
        var queryGateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
            ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Accepted("message-grant", "correlation-grant"),
            new TenantCommandStatusResult(status, "Status remained support-safe.")));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("target-user");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        cut.WaitForAssertion(() =>
        {
            queryGateway.GlobalAdministratorCalls.ShouldBe(1);
            cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain(expectedStateText, Case.Insensitive);
            cut.Find("[data-testid='tenants-global-admin-grant-audit-state']").TextContent.ShouldContain(expectedAuditText);
            cut.Find("[data-testid='tenants-global-admin-grant-safe-message']").TextContent.ShouldContain("support-safe");
            cut.Find("[data-testid='tenants-global-admin-grant-lifecycle']").GetAttribute("role").ShouldBe("alert");
            cut.Find("[data-testid='tenants-global-admin-grant-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
            cut.FindAll("[data-testid='tenants-global-admins-user-id']").Select(static element => element.TextContent)
                .ShouldNotContain("target-user");
        });
    }

    [Fact]
    public void Already_global_administrator_rejection_stays_rejected_without_success_copy()
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("existing-admin", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
            ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current }));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Rejected(
                "This user is already a global administrator. Refresh the platform authority projection before trying another action.",
                "GlobalAdministratorAlreadyExists")));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("existing-admin");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain("rejected", Case.Insensitive);
            cut.Find("[data-testid='tenants-global-admin-grant-safe-message']").TextContent.ShouldContain("already a global administrator");
            cut.Markup.ShouldNotContain("AlreadyApplied");
            // Visible text only — avoids the Fluent success-color token false positive (see VisibleText).
            cut.VisibleText().ShouldNotContain("success", Case.Insensitive);
        });
    }

    [Fact]
    public void Insufficient_permissions_rejection_uses_safe_platform_governance_copy()
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
            ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current }));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Rejected(
                "The caller is not authorized for platform governance changes.",
                "InsufficientPermissions")));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("target-user");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain("rejected", Case.Insensitive);
            cut.Find("[data-testid='tenants-global-admin-grant-safe-message']").TextContent.ShouldContain("platform governance");
            cut.Find("[data-testid='tenants-global-admin-grant-live-region']").GetAttribute("aria-live").ShouldBe("assertive");
            cut.Markup.ShouldNotContain("tenant command succeeded", Case.Insensitive);
            cut.Markup.ShouldNotContain("target-user is now", Case.Insensitive);
            cut.Markup.ShouldNotContain("correlation-", Case.Insensitive);
        });
    }

    [Fact]
    public void Processing_grant_keeps_one_at_a_time_lock_and_remove_placeholder_unavailable()
    {
        var queryGateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
                null,
                false,
                "\"etag-1\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current },
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
                null,
                false,
                "\"etag-2\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Accepted("message-grant", "correlation-grant"),
            new TenantCommandStatusResult(CommandStatus.Processing)));
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("target-user");
        cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent.ShouldContain("accepted", Case.Insensitive);
            cut.Find("[data-testid='tenants-global-admin-grant-submit']").HasAttribute("disabled").ShouldBeTrue();
            cut.Find("[data-testid='tenants-global-admins-remove-reason']").TextContent.ShouldContain("in flight");
        });
    }

    [Fact]
    public void Grant_resources_and_styles_cover_accessible_forced_colors_support_safe_states()
    {
        string projectRoot = ProjectRoot();
        string resourceRoot = Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Resources");
        string styles = File.ReadAllText(
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Pages", "GlobalAdministratorsPage.razor.css"));
        HashSet<string> englishKeys = ResourceKeys(Path.Combine(resourceRoot, "TenantsResources.resx"), "Tenants.GlobalAdministrators.Grant.");
        HashSet<string> frenchKeys = ResourceKeys(Path.Combine(resourceRoot, "TenantsResources.fr.resx"), "Tenants.GlobalAdministrators.Grant.");
        HashSet<string> englishRemoveKeys = ResourceKeys(Path.Combine(resourceRoot, "TenantsResources.resx"), "Tenants.GlobalAdministrators.Remove.");
        HashSet<string> frenchRemoveKeys = ResourceKeys(Path.Combine(resourceRoot, "TenantsResources.fr.resx"), "Tenants.GlobalAdministrators.Remove.");

        englishKeys.ShouldBe(frenchKeys);
        englishKeys.ShouldContain("Tenants.GlobalAdministrators.Grant.State.Confirmed");
        englishKeys.ShouldContain("Tenants.GlobalAdministrators.Grant.State.Rejected");
        englishKeys.ShouldContain("Tenants.GlobalAdministrators.Grant.State.UnableToVerify");
        englishKeys.ShouldContain("Tenants.GlobalAdministrators.Grant.Audit.AuditDelayed");
        englishKeys.ShouldContain("Tenants.GlobalAdministrators.Grant.Unavailable.CommandSurface");
        englishRemoveKeys.ShouldBe(frenchRemoveKeys);
        englishRemoveKeys.ShouldContain("Tenants.GlobalAdministrators.Remove.State.Confirmed");
        englishRemoveKeys.ShouldContain("Tenants.GlobalAdministrators.Remove.State.Rejected");
        englishRemoveKeys.ShouldContain("Tenants.GlobalAdministrators.Remove.Unavailable.LastAdmin");
        englishRemoveKeys.ShouldContain("Tenants.GlobalAdministrators.Remove.Preview.KnownUnknowns.Value");
        styles.ShouldContain("@media (forced-colors: active)");
        styles.ShouldContain(".global-admins__grant-lifecycle:focus-visible");
        styles.ShouldContain(".global-admins__grant-state-symbol");
        styles.ShouldContain(".global-admins__remove-lifecycle:focus-visible");
        styles.ShouldContain(".global-admins__remove-state-symbol");
        styles.ShouldContain("@media (max-width: 42rem)");
        styles.ShouldContain("overflow-wrap: anywhere");

        // The per-row Remove launcher is a FluentButton, so it carries no scope attribute of its own, but it
        // renders inside a plain scoped ancestor -- ::deep is both necessary and sufficient there.
        styles.ShouldContain("::deep .global-admins__mutation-initiation");

        // The read-only notice is different: ::deep alone was NOT enough, because ::deep still compiles to
        // "[b-xxx] <selector>" and the FluentMessageBar had no ancestor rendered by this component to carry
        // the scope attribute. It is now hidden via its own plain-HTML host element, which does. Asserting
        // the host selector (and the absence of the ::deep form) keeps the inert construct from returning.
        styles.ShouldContain(".global-admins__mobile-readonly-host");
        styles.ShouldNotContain("::deep .global-admins__mobile-readonly ");
        styles.ShouldNotContain("::deep .global-admins__mobile-readonly{");
        styles.ShouldNotContain("::deep .global-admins__mobile-readonly {");
    }

    /// <summary>
    /// Pins the classes the mobile read-only breakpoint depends on to the rendered DOM.
    /// </summary>
    /// <remarks>
    /// The stylesheet assertions above cannot show that any element actually carries these classes. The
    /// previous regex-only check passed while the notice was permanently visible on desktop and while the
    /// per-row Remove launcher sat outside every selector the breakpoint could match, so dropping a class
    /// from the markup changed nothing it observed.
    /// </remarks>
    [Fact]
    public void Mobile_read_only_notice_and_mutation_launchers_are_present_in_the_rendered_dom()
    {
        GlobalAdministratorsSnapshot snapshot = GlobalAdministratorsSnapshot.Ready(
            [
                new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current),
                new GlobalAdministratorRow("admin-2", ReadModelFreshnessState.Current),
            ],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            IsCompleteEvidence = true,
        };
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(snapshot));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.FindAll("[data-testid='tenants-global-admins-mobile-readonly']")
            .ShouldNotBeEmpty("the mobile read-only notice must exist for the breakpoint rule to reveal it");

        // The host wrapper is the element the breakpoint actually hides and reveals. It must be plain HTML
        // rendered by this component, because that is the only thing CSS isolation will scope; asserting the
        // class on the FluentMessageBar alone is what let the permanently-visible-on-desktop defect survive.
        cut.FindAll("div.global-admins__mobile-readonly-host")
            .ShouldNotBeEmpty("the notice must sit inside a plain scoped host for the breakpoint rule to match");

        // Every per-row Remove launcher must be hideable, not just the grant/remove panel controls.
        cut.FindAll("[data-testid='tenants-global-admin-remove']").Count.ShouldBe(2);
        cut.FindAll(".global-admins__mutation-initiation").Count.ShouldBeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void Routes_stay_reachable_while_tenants_nav_collapses_to_one_module_entry()
    {
        string projectRoot = ProjectRoot();
        string page = File.ReadAllText(
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Pages", "GlobalAdministratorsPage.razor"));
        string myTenantsPage = File.ReadAllText(
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Pages", "MyTenantsPage.razor"));
        string userLookupPage = File.ReadAllText(
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Pages", "UserMembershipLookupPage.razor"));
        string workspace = File.ReadAllText(
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Pages", "TenantsWorkspace.razor"));
        string registration = File.ReadAllText(
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Composition", "TenantsFrontComposerRegistration.cs"));
        string detail = File.ReadAllText(
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Pages", "TenantDetailPage.razor"));
        string routes = File.ReadAllText(
            Path.Combine(projectRoot, "src", "Hexalith.Tenants.UI", "Components", "Routes.razor"));

        page.ShouldContain("@page \"/global-administrators\"");

        // Platform authority is a rendered fail-closed state, never endpoint authorization. The host
        // registers an authentication scheme only when OIDC is configured, so authorize metadata on the
        // route would fault the request instead of rendering the restricted surface.
        page.ShouldNotContain("@attribute [Authorize");
        routes.ShouldNotContain("<AuthorizeRouteView");
        routes.ShouldContain("<RouteView");
        myTenantsPage.ShouldContain("@page \"/tenants/my\"");
        userLookupPage.ShouldContain("@page \"/tenants/users\"");

        // Correct Course 2026-06-27: the shell rail exposes one Tenants module entry. My Tenants,
        // User lookup, and Global Administrators remain implemented routes, but they are no longer
        // registered as Tenants left-menu entries.
        registration.ShouldContain("\"/tenants\"");
        registration.ShouldNotContain("\"/tenants/my\"");
        registration.ShouldNotContain("\"/tenants/users\"");
        registration.ShouldNotContain("\"/global-administrators\"");
        workspace.ShouldNotContain("href=\"/users\"");
        workspace.ShouldNotContain("href=\"/tenants/my\"");
        workspace.ShouldNotContain("href=\"/tenants/users\"");
        detail.ShouldContain("returnUrl.StartsWith(\"/tenants\", StringComparison.Ordinal)");
    }

    private static string ProjectRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static ClaimsPrincipal GlobalAdministratorPrincipal()
        => new(new ClaimsIdentity(
        [
            new Claim("sub", "operator.alpha"),
            new Claim("role", "global-administrator"),
            new Claim("eventstore:tenant", "system"),
        ],
        "test"));

    private static ClaimsPrincipal NonAdministratorPrincipal()
        => new(new ClaimsIdentity([new Claim("sub", "operator.alpha")], "test"));

    private static HashSet<string> ResourceKeys(string path, string prefix)
        => System.Xml.Linq.XDocument
            .Load(path)
            .Descendants("data")
            .Select(static element => element.Attribute("name")?.Value)
            .Where(name => name is not null && name.StartsWith(prefix, StringComparison.Ordinal))
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);

    private sealed class StubTenantsBffComposition(
        TenantLifecycleAuthorizationReflectionState reflection,
        bool isReadSurfaceConnected = true,
        bool isCommandSurfaceConnected = true) : ITenantsBffComposition
    {
        public bool IsReadSurfaceConnected => isReadSurfaceConnected;

        public bool IsCommandSurfaceConnected => isCommandSurfaceConnected;

        public TenantLifecycleAuthorizationReflectionState Reflection { get; set; } = reflection;

        public TenantLifecycleAuthorizationReflectionState GlobalAdministratorsAuthorizationReflection => Reflection;
    }

    private sealed class StubTenantQueryGateway(params GlobalAdministratorsSnapshot[] snapshots) : ITenantQueryGateway
    {
        /// <summary>
        /// Explicit because <c>ITenantQueryGateway.GetTenantUsersAsync</c> is no longer a default interface
        /// method. These stubs previously inherited a silent <c>Unavailable</c> fallback, so a member-read
        /// regression would have rendered as an outage here rather than failing the build.
        /// </summary>
        public Task<TenantUsersSnapshot> GetTenantUsersAsync(
            TenantUsersRequest request,
            TenantUsersSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Task.FromResult(TenantUsersSnapshot.Unavailable(request.TenantId));
        }

        private readonly Queue<GlobalAdministratorsSnapshot> _snapshots = new(snapshots);
        private readonly Queue<Task<GlobalAdministratorsSnapshot>> _queuedResponses = [];

        public int GlobalAdministratorCalls { get; private set; }

        public List<GlobalAdministratorsRequest> Requests { get; } = [];

        public List<GlobalAdministratorsSnapshot?> PreviousSnapshots { get; } = [];

        public void QueueResponse(Task<GlobalAdministratorsSnapshot> response)
            => _queuedResponses.Enqueue(response);

        public Task<TenantDetailSnapshot> GetTenantAsync(
            TenantDetailRequest request,
            TenantDetailSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantListSnapshot> ListTenantsAsync(
            TenantListRequest request,
            TenantListSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserTenantMembershipSnapshot> GetMyTenantsAsync(
            UserTenantMembershipRequest request,
            UserTenantMembershipSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<UserTenantMembershipSnapshot> GetUserTenantsAsync(
            UserTenantMembershipRequest request,
            UserTenantMembershipSnapshot? previous,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<GlobalAdministratorsSnapshot> GetGlobalAdministratorsAsync(
            GlobalAdministratorsRequest request,
            GlobalAdministratorsSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            GlobalAdministratorCalls++;
            Requests.Add(request);
            PreviousSnapshots.Add(previous);
            return _queuedResponses.Count > 0
                ? _queuedResponses.Dequeue()
                : Task.FromResult(_snapshots.Dequeue());
        }

        public Task<TenantAuditSnapshot> GetTenantAuditAsync(
            TenantAuditRequest request,
            TenantAuditSnapshot? previous,
            CancellationToken cancellationToken = default)
            => Task.FromResult(TenantAuditSnapshot.Unavailable(request));
    }

    private sealed class StubTenantCommandGateway(
        TenantCommandSubmissionResult? submission = null,
        params TenantCommandStatusResult[] statuses) : ITenantCommandGateway
    {
        private readonly Queue<TenantCommandStatusResult> _statuses = new(statuses);

        public TenantCommandSubmissionResult? RemoveSubmission { get; init; }

        public int SetGlobalAdministratorCalls { get; private set; }

        public int RemoveGlobalAdministratorCalls { get; private set; }

        public List<SetGlobalAdministrator> Requests { get; } = [];

        public List<RemoveGlobalAdministrator> RemoveRequests { get; } = [];

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(
            CreateTenant request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(
            AddUserToTenant request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(
            ChangeUserRole request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(
            RemoveUserFromTenant request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(
            UpdateTenant request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> SetTenantConfigurationAsync(
            SetTenantConfiguration request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> RemoveTenantConfigurationAsync(
            RemoveTenantConfiguration request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> SetGlobalAdministratorAsync(
            SetGlobalAdministrator request,
            CancellationToken cancellationToken = default)
        {
            SetGlobalAdministratorCalls++;
            Requests.Add(request);
            return Task.FromResult(submission ?? TenantCommandSubmissionResult.Failed("No command response configured."));
        }

        public Task<TenantCommandSubmissionResult> RemoveGlobalAdministratorAsync(
            RemoveGlobalAdministrator request,
            CancellationToken cancellationToken = default)
        {
            RemoveGlobalAdministratorCalls++;
            RemoveRequests.Add(request);
            return Task.FromResult(RemoveSubmission ?? TenantCommandSubmissionResult.Failed("No remove command response configured."));
        }

        public Task<TenantCommandSubmissionResult> EnableTenantAsync(
            TenantLifecycleCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> DisableTenantAsync(
            TenantLifecycleCommandRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandStatusResult> GetStatusAsync(
            TenantCommandTrackingHandle handle,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_statuses.Count == 0
                ? TenantCommandStatusResult.Unknown("No command status configured.")
                : _statuses.Dequeue());
    }

    private sealed class MutableAuthenticationStateProvider(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        private AuthenticationState _state = new(principal);

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(_state);

        public void Set(ClaimsPrincipal updated)
        {
            _state = new AuthenticationState(updated);
            NotifyAuthenticationStateChanged(Task.FromResult(_state));
        }

    }

    private sealed class StubTenantsLocalizer : IStringLocalizer<TenantsResources>
    {
        private static readonly Dictionary<string, string> Values = new(StringComparer.Ordinal)
        {
            ["Tenants.Copy.Action"] = "Copy",
            ["Tenants.Copy.Feedback.Copied"] = "Copied.",
            ["Tenants.GlobalAdministrators.Action.Unavailable.Freshness"] = "Unavailable until projection freshness is current.",
            ["Tenants.GlobalAdministrators.Action.Unavailable.ReadOnly"] = "Unavailable in this read-only review.",
            ["Tenants.GlobalAdministrators.Aggregate.Domain.Label"] = "Domain",
            ["Tenants.GlobalAdministrators.Aggregate.Domain.Value"] = "global-administrators",
            ["Tenants.GlobalAdministrators.Aggregate.Id.Label"] = "Aggregate id",
            ["Tenants.GlobalAdministrators.Aggregate.Id.Value"] = "global-administrators",
            ["Tenants.GlobalAdministrators.Aggregate.Tenant.Label"] = "Tenant scope",
            ["Tenants.GlobalAdministrators.Aggregate.Tenant.Value"] = "system",
            ["Tenants.GlobalAdministrators.Column.Actions"] = "Grant/remove availability",
            ["Tenants.GlobalAdministrators.Column.Freshness"] = "Freshness",
            ["Tenants.GlobalAdministrators.Column.Identity"] = "Administrator identity",
            ["Tenants.GlobalAdministrators.Column.Scope"] = "Authority scope",
            ["Tenants.GlobalAdministrators.Copy.UserId"] = "Copy global administrator identifier {0}",
            ["Tenants.GlobalAdministrators.Description"] = "Review platform-level administrators from the fixed global-administrators authority scope.",
            ["Tenants.GlobalAdministrators.Eyebrow"] = "Platform governance",
            ["Tenants.GlobalAdministrators.Freshness.Current"] = "Current",
            ["Tenants.GlobalAdministrators.Freshness.Stale"] = "Stale",
            ["Tenants.GlobalAdministrators.Freshness.Unknown"] = "Unknown",
            ["Tenants.GlobalAdministrators.Grant.Audit.AuditDelayed"] = "Audit evidence is delayed; refresh status or inspect audit before citing proof.",
            ["Tenants.GlobalAdministrators.Grant.Audit.AuditPending"] = "Audit evidence is pending; do not cite audit proof until it is visible.",
            ["Tenants.GlobalAdministrators.Grant.Audit.AuditUnavailable"] = "Audit evidence is unavailable from this flow.",
            ["Tenants.GlobalAdministrators.Grant.Audit.MissingSupport"] = "Audit support for this flow is not available.",
            ["Tenants.GlobalAdministrators.Grant.Audit.NotStarted"] = "No audit evidence is available before command submission.",
            ["Tenants.GlobalAdministrators.Grant.Available"] = "Grant is available from the confirmed platform authority projection.",
            ["Tenants.GlobalAdministrators.Grant.Cancel"] = "Cancel",
            ["Tenants.GlobalAdministrators.Grant.Description"] = "Grant platform authority in tenant system, domain global-administrators, aggregate global-administrators. Completion requires projection confirmation.",
            ["Tenants.GlobalAdministrators.Grant.Lifecycle.Title"] = "Grant lifecycle",
            ["Tenants.GlobalAdministrators.Grant.Refresh"] = "Refresh status",
            ["Tenants.GlobalAdministrators.Grant.State.Accepted"] = "Command accepted; projection confirmation is still required.",
            ["Tenants.GlobalAdministrators.Grant.State.AlreadyApplied"] = "Already-applied is not used for global administrator grants.",
            ["Tenants.GlobalAdministrators.Grant.State.Confirmed"] = "Projection confirmed the target user in the fixed global-administrators scope.",
            ["Tenants.GlobalAdministrators.Grant.State.Degraded"] = "Grant verification is degraded; publication or audit evidence could not be verified.",
            ["Tenants.GlobalAdministrators.Grant.State.DuplicatePrevented"] = "A concurrent grant command was prevented.",
            ["Tenants.GlobalAdministrators.Grant.State.Failed"] = "Grant command failed before it could be verified.",
            ["Tenants.GlobalAdministrators.Grant.State.Idle"] = "No global administrator grant command has been submitted.",
            ["Tenants.GlobalAdministrators.Grant.State.Previewed"] = "Grant intent is previewed but not submitted.",
            ["Tenants.GlobalAdministrators.Grant.State.ProjectionPending"] = "Projection pending; the target user is not confirmed as a global administrator yet.",
            ["Tenants.GlobalAdministrators.Grant.State.Rejected"] = "Grant command was rejected.",
            ["Tenants.GlobalAdministrators.Grant.State.RequestSent"] = "Grant command request was sent.",
            ["Tenants.GlobalAdministrators.Grant.State.UnableToVerify"] = "Grant command could not be verified from command status and projection evidence.",
            ["Tenants.GlobalAdministrators.Grant.Submit"] = "Grant global administrator",
            ["Tenants.GlobalAdministrators.Grant.Title"] = "Grant global administrator",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.Authorization"] = "Platform authority is not confirmed, so grant fails closed without revealing administrator data.",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.CommandSurface"] = "The command surface is unavailable for platform governance changes.",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.Freshness"] = "Refresh projection freshness before granting platform authority.",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.InFlight"] = "Another platform authority command is in flight.",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.ReadSurface"] = "The global administrator read projection must be available before grant can be submitted.",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.RemoveDeferred"] = "Remove global administrator is handled by a separate guarded flow.",
            ["Tenants.GlobalAdministrators.Grant.UserId.Help"] = "Enter the literal caller-supplied user id. It is not parsed as a tenant member, GUID, or ULID.",
            ["Tenants.GlobalAdministrators.Grant.UserId.Label"] = "User id",
            ["Tenants.GlobalAdministrators.Grant.Validation.UserIdRequired"] = "User id is required before granting global administrator authority.",
            ["Tenants.GlobalAdministrators.Remove.Audit.AuditDelayed"] = "Audit evidence is delayed; refresh status or inspect audit before citing proof.",
            ["Tenants.GlobalAdministrators.Remove.Audit.AuditPending"] = "Audit evidence is pending; do not cite audit proof until it is visible.",
            ["Tenants.GlobalAdministrators.Remove.Audit.AuditUnavailable"] = "Audit evidence is unavailable from this flow.",
            ["Tenants.GlobalAdministrators.Remove.Audit.MissingSupport"] = "Audit support for this flow is not available.",
            ["Tenants.GlobalAdministrators.Remove.Audit.NotStarted"] = "No audit evidence is available before command submission.",
            ["Tenants.GlobalAdministrators.Remove.Cancel"] = "Cancel",
            ["Tenants.GlobalAdministrators.Remove.Description"] = "Remove platform authority only when the fixed global-administrators projection proves it will not remove the last global administrator.",
            ["Tenants.GlobalAdministrators.Remove.Launch"] = "Remove global administrator",
            ["Tenants.GlobalAdministrators.Remove.Lifecycle.Title"] = "Remove lifecycle",
            ["Tenants.GlobalAdministrators.Remove.Preview.AccessRevoked"] = "Access being revoked",
            ["Tenants.GlobalAdministrators.Remove.Preview.AccessRevoked.Value"] = "Platform global-administrator authority is revoked from the target user only after projection confirmation.",
            ["Tenants.GlobalAdministrators.Remove.Preview.Audit"] = "Audit expectation",
            ["Tenants.GlobalAdministrators.Remove.Preview.Audit.Value"] = "Audit evidence is expected after command acceptance and projection-confirmed removal; this panel does not fabricate proof.",
            ["Tenants.GlobalAdministrators.Remove.Preview.Count"] = "Current administrator count",
            ["Tenants.GlobalAdministrators.Remove.Preview.Freshness"] = "Projection freshness",
            ["Tenants.GlobalAdministrators.Remove.Preview.KnownConsequences"] = "Known consequences",
            ["Tenants.GlobalAdministrators.Remove.Preview.KnownConsequences.Value"] = "The target loses platform authority in the system global-administrators scope; tenant membership is not changed.",
            ["Tenants.GlobalAdministrators.Remove.Preview.KnownUnknowns"] = "Known unknowns",
            ["Tenants.GlobalAdministrators.Remove.Preview.KnownUnknowns.Value"] = "Session revocation, token invalidation, downstream enforcement timing, and audit proof timing are not proven by command status alone.",
            ["Tenants.GlobalAdministrators.Remove.Preview.LastAdminImpact"] = "Last administrator impact",
            ["Tenants.GlobalAdministrators.Remove.Preview.LastAdminImpact.Value"] = "The target is not the last visible global administrator in the current projection.",
            ["Tenants.GlobalAdministrators.Remove.Preview.Recovery"] = "Recovery path",
            ["Tenants.GlobalAdministrators.Remove.Preview.Recovery.Value"] = "Refresh projection truth, inspect audit evidence, or grant global administrator authority again through the fixed platform-governance flow.",
            ["Tenants.GlobalAdministrators.Remove.Preview.Scope"] = "Platform authority scope",
            ["Tenants.GlobalAdministrators.Remove.Preview.Scope.Value"] = "tenant system, domain global-administrators, aggregate global-administrators",
            ["Tenants.GlobalAdministrators.Remove.Preview.Target"] = "Target user id",
            ["Tenants.GlobalAdministrators.Remove.Preview.Title"] = "Remove consequence preview",
            ["Tenants.GlobalAdministrators.Remove.Refresh"] = "Refresh status",
            ["Tenants.GlobalAdministrators.Remove.State.Accepted"] = "Command accepted; projection confirmation is still required.",
            ["Tenants.GlobalAdministrators.Remove.State.AlreadyApplied"] = "Already-applied is not used for global administrator removal.",
            ["Tenants.GlobalAdministrators.Remove.State.Confirmed"] = "Projection confirmed removal from the fixed global-administrators scope.",
            ["Tenants.GlobalAdministrators.Remove.State.Degraded"] = "Remove verification is degraded; publication or audit evidence could not be verified.",
            ["Tenants.GlobalAdministrators.Remove.State.DuplicatePrevented"] = "A concurrent remove command was prevented.",
            ["Tenants.GlobalAdministrators.Remove.State.Failed"] = "Remove command failed before it could be verified.",
            ["Tenants.GlobalAdministrators.Remove.State.Idle"] = "No global administrator remove command has been submitted.",
            ["Tenants.GlobalAdministrators.Remove.State.Previewed"] = "Remove preview is ready for deliberate confirmation.",
            ["Tenants.GlobalAdministrators.Remove.State.ProjectionPending"] = "Projection pending; the target user is still visible until a re-query proves absence.",
            ["Tenants.GlobalAdministrators.Remove.State.Rejected"] = "Remove command was rejected.",
            ["Tenants.GlobalAdministrators.Remove.State.RequestSent"] = "Remove command request was sent.",
            ["Tenants.GlobalAdministrators.Remove.State.UnableToVerify"] = "Remove command could not be verified from command status and projection evidence.",
            ["Tenants.GlobalAdministrators.Remove.Submit"] = "Confirm removal",
            ["Tenants.GlobalAdministrators.Remove.Title"] = "Remove global administrator",
            ["Tenants.GlobalAdministrators.Remove.Unavailable.Authorization"] = "Platform authority is not confirmed, so remove fails closed without revealing administrator data.",
            ["Tenants.GlobalAdministrators.Remove.Unavailable.CommandSurface"] = "The command surface is unavailable for platform governance changes.",
            ["Tenants.GlobalAdministrators.Remove.Unavailable.Freshness"] = "Refresh projection freshness before removing platform authority.",
            ["Tenants.GlobalAdministrators.Remove.Unavailable.InFlight"] = "Another platform authority command is in flight.",
            ["Tenants.GlobalAdministrators.Remove.Unavailable.LastAdmin"] = "The last global administrator cannot be removed.",
            ["Tenants.GlobalAdministrators.Remove.Unavailable.ReadSurface"] = "The global administrator read projection must be available before removal can be submitted.",
            ["Tenants.GlobalAdministrators.Remove.Unavailable.TargetMissing"] = "The target administrator is not visible in the current projection.",
            ["Tenants.GlobalAdministrators.Identity.Accessible"] = "Global administrator identifier {0}",
            ["Tenants.GlobalAdministrators.List.Title"] = "Current global administrators",
            ["Tenants.GlobalAdministrators.Next"] = "Next",
            ["Tenants.GlobalAdministrators.PaginationLabel"] = "Global administrator pages",
            ["Tenants.GlobalAdministrators.Previous"] = "Previous",
            ["Tenants.GlobalAdministrators.Refresh"] = "Refresh",
            ["Tenants.GlobalAdministrators.Return"] = "Return to Tenants",
            ["Tenants.GlobalAdministrators.Recovery.Retry"] = "Retry review",
            ["Tenants.GlobalAdministrators.Recovery.Reset"] = "Reset to first page",
            ["Tenants.GlobalAdministrators.Recovery.PageRecovered"] = "The protected page could not be restored. The review restarted at the first page.",
            ["Tenants.GlobalAdministrators.Mobile.ReadOnly.Title"] = "Read-only on narrow screens",
            ["Tenants.GlobalAdministrators.Mobile.ReadOnly.Message"] = "Review, paging, copy, and recovery remain available. Grant and remove controls require a wider viewport.",
            ["Tenants.GlobalAdministrators.RestrictedTitle"] = "Platform area unavailable",
            ["Tenants.GlobalAdministrators.Row.Scope"] = "Platform authority, not tenant ownership",
            ["Tenants.GlobalAdministrators.Scope.Message"] = "This surface uses the singleton platform authority aggregate and never substitutes tenant membership data.",
            ["Tenants.GlobalAdministrators.Scope.Title"] = "Fixed aggregate scope",
            ["Tenants.GlobalAdministrators.State.Degraded.Message"] = "Projection data is degraded. Last confirmed administrators remain visible and actions are unavailable.",
            ["Tenants.GlobalAdministrators.State.Degraded.Title"] = "Global administrator data degraded",
            ["Tenants.GlobalAdministrators.State.Empty.Message"] = "No global administrators were returned for the authorized fixed scope.",
            ["Tenants.GlobalAdministrators.State.Empty.Title"] = "No global administrators returned",
            ["Tenants.GlobalAdministrators.State.Invalid.Message"] = "The requested page cursor is invalid. No administrator data is revealed.",
            ["Tenants.GlobalAdministrators.State.Invalid.Title"] = "Invalid global administrator page",
            ["Tenants.GlobalAdministrators.State.Loading.Message"] = "Loading global administrators from the fixed platform authority scope.",
            ["Tenants.GlobalAdministrators.State.Loading.Title"] = "Loading global administrators",
            ["Tenants.GlobalAdministrators.State.Ready.Message"] = "Global administrators are loaded from the fixed platform authority projection.",
            ["Tenants.GlobalAdministrators.State.Ready.Title"] = "Global administrators loaded",
            ["Tenants.GlobalAdministrators.State.Unknown.Message"] = "Projection freshness is unknown. Previously confirmed rows may be reviewed, but no current or complete claim is made.",
            ["Tenants.GlobalAdministrators.State.Unknown.Title"] = "Global administrator truth unknown",
            ["Tenants.GlobalAdministrators.State.Error.Message"] = "The review could not be refreshed. Retry or reset to the first page; no current projection claim is made.",
            ["Tenants.GlobalAdministrators.State.Error.Title"] = "Global administrator review failed",
            ["Tenants.GlobalAdministrators.State.Stale.Message"] = "Projection freshness is stale. Last confirmed administrators remain visible and actions are unavailable.",
            ["Tenants.GlobalAdministrators.State.Stale.Title"] = "Global administrator data stale",
            ["Tenants.GlobalAdministrators.State.Unauthorized.Message"] = "Platform authority was not confirmed. The area fails closed and does not reveal administrator data.",
            ["Tenants.GlobalAdministrators.State.Unauthorized.Title"] = "Platform area unavailable",
            ["Tenants.GlobalAdministrators.State.Unavailable.Message"] = "The global administrator read surface is unavailable. No hidden administrator data is shown.",
            ["Tenants.GlobalAdministrators.State.Unavailable.Title"] = "Global administrator data unavailable",
            ["Tenants.GlobalAdministrators.Title"] = "Global Administrators",
        };

        public LocalizedString this[string name]
            => new(name, Values.TryGetValue(name, out string? value) ? value : name);

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                string value = Values.TryGetValue(name, out string? template) ? template : name;
                return new(name, string.Format(CultureInfo.CurrentCulture, value, arguments));
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Values.Select(static v => new LocalizedString(v.Key, v.Value));
    }
}
