using System.Globalization;
using System.Security.Claims;

using AngleSharp.Dom;

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
using Hexalith.Tenants.UI.State;
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
    public async Task Successful_subscription_setups_do_not_exhaust_the_budget_across_authorization_transitions()
    {
        var authentication = new MutableAuthenticationStateProvider(GlobalAdministratorPrincipal());
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        GlobalAdministratorsSnapshot ready = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.current", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"current\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current };
        // Five responses for five expected reads left no slack: one extra read -- a notification nudge, an
        // authorization re-read -- made Dequeue() throw InvalidOperationException from inside the gateway
        // call, which LoadAsync does not catch (only OperationCanceledException). The stub repeats its last
        // response, so an unexpected read fails an assertion instead of an unrelated queue underflow.
        var gateway = new StubTenantQueryGateway(ready) { RepeatLastResponse = true };
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
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

        for (int transition = 1; transition <= 4; transition++)
        {
            authentication.Set(NonAdministratorPrincipal());
            cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-global-admins-unavailable']"));

            // Polled, not asserted at an instant. CollapseAuthorizationAsync calls StateHasChanged BEFORE
            // disposing the lease, so the unavailable panel rendering does not imply the unsubscribe was
            // issued -- NSubstitute's Received() does not wait, so this raced and could fail on transition 1.
            cut.WaitForAssertion(() => subscription.Received(transition).UnsubscribeAsync(
                GetGlobalAdministratorsQuery.ProjectionType,
                "system",
                Arg.Any<CancellationToken>()));

            authentication.Set(GlobalAdministratorPrincipal());
            cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(transition + 1));

            // Same reason: the gateway call count reaching n does not imply OnAfterRenderAsync's subscribe
            // has run.
            cut.WaitForAssertion(() => subscription.Received(transition + 1).SubscribeAsync(
                GetGlobalAdministratorsQuery.ProjectionType,
                "system",
                Arg.Any<CancellationToken>()));
        }

        await Task.CompletedTask;
    }

    [Fact]
    public void Authorized_operator_sees_global_administrators_from_fixed_scope()
    {
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("platform-admin.alpha", ReadModelFreshnessState.Current) with
            {
                Lifecycle = ProjectionLifecycleState.Current,
            }],
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
        cut.Find("[data-testid='tenants-global-admins-projection-lifecycle-status']")
            .GetAttribute("class").ShouldNotBeNull().ShouldContain("projection-lifecycle-badge--current");
        cut.Find("[data-testid='tenants-global-admins-projection-lifecycle']")
            .GetAttribute("class").ShouldNotBeNull().ShouldContain("projection-lifecycle-badge--current");

        // The localized label, not only the class: a class token is incidental markup, and asserting it
        // alone cannot detect a missing or wrong badge string.
        cut.Find("[data-testid='tenants-global-admins-projection-lifecycle']").TextContent.Trim().ShouldBe("Current");
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

        // Anchor on the live region itself, not only on its parent. The denied-message element CONTAINS the
        // live region, so its TextContent includes the child either way -- deleting the live-region wrapper
        // from the restricted branch left the denial with no identified live region, test green.
        cut.Find("[data-testid='tenants-global-admins-live-region']").TextContent.ShouldContain("fails closed");
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
    public async Task Manual_refresh_retries_notification_setup_after_a_transient_empty_lease()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        int setupAttempts = 0;
        subscription
            .SubscribeAsync(
                GetGlobalAdministratorsQuery.ProjectionType,
                "system",
                Arg.Any<CancellationToken>())
            // Keeps failing until the per-render budget is spent, so the budget itself is exercised and the
            // only attempt that can succeed is the one an explicit refresh unlocks.
            .Returns(_ => Interlocked.Increment(ref setupAttempts) <= 3
                ? Task.FromException(new HttpRequestException("transient setup failure"))
                : Task.CompletedTask);
        var gateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin.confirmed", ReadModelFreshnessState.Current)],
                null,
                false,
                "\"etag-1\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current },
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin.confirmed", ReadModelFreshnessState.Current)],
                null,
                false,
                "\"etag-2\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.WaitForAssertion(() => setupAttempts.ShouldBeGreaterThanOrEqualTo(1));

        // Exhaust the per-render retry budget. Without a bound, a failing subscribe was retried on EVERY
        // render, so "the Refresh click retried setup" was indistinguishable from "the click caused a
        // render": rewiring the button to a no-op that calls StateHasChanged kept the test green.
        for (int render = 0; render < 10; render++)
        {
            cut.Render();
        }

        int attemptsBeforeRefresh = Volatile.Read(ref setupAttempts);
        attemptsBeforeRefresh.ShouldBe(3, "The per-render retry must be bounded, and spend its whole budget.");

        // Only an explicit refresh resets the budget, so only it can produce a further attempt.
        cut.Find("[data-testid='tenants-global-admins-refresh']").Click();

        cut.WaitForAssertion(() => Volatile.Read(ref setupAttempts).ShouldBe(attemptsBeforeRefresh + 1));
        await subscription.Received(attemptsBeforeRefresh + 1).SubscribeAsync(
            GetGlobalAdministratorsQuery.ProjectionType,
            "system",
            Arg.Any<CancellationToken>());

        // Exactly one further attempt, because the empty lease was transient and this retry SUCCEEDS -- not
        // because a refresh grants one attempt. Refresh restores the whole budget of 3; what stops it being
        // spent is the success. Re-rendering ten more times must therefore add nothing, which is what
        // separates "refresh reset the budget and the retry worked" from "every render retries forever" --
        // the regression this test exists for, and the half that was previously unasserted.
        for (int render = 0; render < 10; render++)
        {
            cut.Render();
        }

        Volatile.Read(ref setupAttempts).ShouldBe(attemptsBeforeRefresh + 1);
    }

    /// <summary>
    /// Retry and Reset must refuse a click dispatched while a page read is already in flight.
    /// </summary>
    /// <remarks>
    /// Both render alongside the pager whenever a rows-bearing snapshot is also recoverable, and neither used
    /// to set or read the in-flight gate: a Next click followed by Reset cleared the cursor state
    /// synchronously, Next's load then returned null as superseded and its <c>finally</c> re-enabled the
    /// pager against the pre-reset snapshot, so the next Next pushed a null prior cursor and recorded page
    /// one for a view showing page four. Neutering the early return in both handlers survived the suite --
    /// the only retry test clicked once against a settled surface, and the rendered <c>disabled</c> attribute
    /// cannot observe a click dispatched before the re-render lands.
    /// </remarks>
    [Fact]
    public async Task Recovery_affordances_reject_a_click_dispatched_while_a_page_read_is_in_flight()
    {
        GlobalAdministratorsSnapshot recoverable = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.alpha", ReadModelFreshnessState.Stale)],
            nextCursor: "page-2",
            hasMore: true,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Stale);
        var gateway = new StubTenantQueryGateway(recoverable) { RepeatLastResponse = true };
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(1));

        // Hold the next page read open, so the surface really is mid-load.
        var pendingPage = new TaskCompletionSource<GlobalAdministratorsSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.QueueResponse(pendingPage.Task);
        Task nextClick = cut.Find("[data-testid='tenants-global-admins-next']")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(2));

        await cut.InvokeAsync(() => cut.Instance.RetryAsync());
        await cut.InvokeAsync(() => cut.Instance.ResetPagingAsync());

        gateway.GlobalAdministratorCalls.ShouldBe(
            2,
            "Neither recovery affordance may start a read while a page read is in flight.");

        pendingPage.SetResult(recoverable);
        await nextClick;

        // Once the load settles, the affordances work again. BOTH of them: re-exercising only RetryAsync
        // left a permanently dead ResetPagingAsync passing this test, because the assertion it was subject
        // to above is that it does NOT read -- an implementation that never reads satisfies that for the
        // wrong reason. Each is given its own positive control.
        await cut.InvokeAsync(() => cut.Instance.RetryAsync());
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(3));

        await cut.InvokeAsync(() => cut.Instance.ResetPagingAsync());
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(4));
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
    public void Previous_page_returns_to_page_one_after_a_confirmed_next_page()
    {
        var gateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
                "protected-next-cursor",
                true,
                "\"etag-1\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current },
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-2", ReadModelFreshnessState.Current)],
                null,
                false,
                "\"etag-2\"",
                ReadModelFreshnessState.Current) with
            {
                Lifecycle = ProjectionLifecycleState.Current,
                RequestCursor = "protected-next-cursor",
            },
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
                "protected-next-cursor",
                true,
                "\"etag-1\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.Find("[data-testid='tenants-global-admins-next']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin-2"));

        cut.Find("[data-testid='tenants-global-admins-previous']").Click();
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(3));
        gateway.Requests.Select(static request => request.Cursor).ShouldBe([null, "protected-next-cursor", null]);
    }

    [Fact]
    public void Failed_previous_keeps_history_so_the_operator_can_retry_page_one()
    {
        GlobalAdministratorsSnapshot pageTwo = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-2", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"etag-2\"",
            ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            RequestCursor = "protected-next-cursor",
        };
        var gateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
                "protected-next-cursor",
                true,
                "\"etag-1\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current },
            pageTwo,
            GlobalAdministratorsSnapshot.Degraded(
                pageTwo.Rows,
                GlobalAdministratorsReason.GatewayUnavailable) with
            {
                RequestCursor = "protected-next-cursor",
            },
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
                "protected-next-cursor",
                true,
                "\"etag-1\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.Find("[data-testid='tenants-global-admins-next']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin-2"));

        cut.Find("[data-testid='tenants-global-admins-previous']").Click();
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(3));
        cut.Find("[data-testid='tenants-global-admins-previous']").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("[data-testid='tenants-global-admins-previous']").Click();

        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(4));
        gateway.Requests.Select(static request => request.Cursor)
            .ShouldBe([null, "protected-next-cursor", null, null]);
    }

    /// <summary>
    /// A Previous click that lands on page one through a trimmed history must be announced.
    /// </summary>
    /// <remarks>
    /// <c>CursorHistory.Trim</c> re-appends the first-page sentinel beneath the newest entries, so one later
    /// Previous click walks the operator from mid-sequence straight to page one. Deleting the whole notice
    /// block left the suite green: <c>tenants-global-admins-history-truncated</c> had no reference anywhere
    /// under tests/, which also made the <c>_pagingHistoryTruncated</c> plumbing on this page unobservable.
    /// The audit twin has carried this coverage since the notice was introduced.
    /// </remarks>
    [Fact]
    public void A_previous_click_that_jumps_to_page_one_through_a_trimmed_history_is_announced()
    {
        // Bound taken from the production constant. Duplicating it as a literal meant a change to
        // CursorHistory.DefaultMaximum made this walk the wrong number of steps and fail with
        // "previous is not disabled" -- a diagnosis that names nothing.
        const int bound = CursorHistory.DefaultMaximum;
        ITenantQueryGateway gateway = Substitute.For<ITenantQueryGateway>();
        gateway.GetGlobalAdministratorsAsync(
            Arg.Any<GlobalAdministratorsRequest>(),
            Arg.Any<GlobalAdministratorsSnapshot?>(),
            Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                GlobalAdministratorsRequest request = call.Arg<GlobalAdministratorsRequest>()!;
                int page = request.Cursor is null
                    ? 0
                    : int.Parse(request.Cursor["page-".Length..], CultureInfo.InvariantCulture);
                return Task.FromResult(GlobalAdministratorsSnapshot.Ready(
                    [new GlobalAdministratorRow($"admin-page-{page}", ReadModelFreshnessState.Current)],
                    $"page-{page + 1}",
                    true,
                    $"\"etag-{page}\"",
                    ReadModelFreshnessState.Current) with
                {
                    Lifecycle = ProjectionLifecycleState.Current,
                    RequestCursor = request.Cursor,
                });
            });
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin-page-0"));

        // One page past the bound, so the trim runs and drops the oldest non-sentinel entries.
        for (int page = 1; page <= bound + 1; page++)
        {
            cut.Find("[data-testid='tenants-global-admins-next']").Click();
            cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-global-admins-user-id']")
                .TextContent.ShouldBe($"admin-page-{page}"));
        }

        cut.FindAll("[data-testid='tenants-global-admins-history-truncated']").ShouldBeEmpty(
            "Paging forward is not a jump; the notice belongs to the Previous click that lands on page one.");

        // Walk back. The retained history is 49 entries plus the re-appended sentinel, so the last of these
        // pops the sentinel and lands on page one from the middle of the sequence.
        for (int step = 1; step < bound; step++)
        {
            cut.Find("[data-testid='tenants-global-admins-previous']").Click();
            cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-global-admins-previous']")
                .HasAttribute("disabled").ShouldBeFalse());
            cut.FindAll("[data-testid='tenants-global-admins-history-truncated']").ShouldBeEmpty();
        }

        cut.Find("[data-testid='tenants-global-admins-previous']").Click();
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin-page-0");
            cut.Find("[data-testid='tenants-global-admins-previous']").HasAttribute("disabled").ShouldBeTrue();
        });

        IElement notice = cut.Find("[data-testid='tenants-global-admins-history-truncated']");
        notice.GetAttribute("role").ShouldBe("status");
        notice.GetAttribute("aria-live").ShouldBe("polite");
        notice.TextContent.ShouldContain("first page");

        // An ordinary reload retires the one-shot navigation notice even though the operator remains on page
        // one. This pins the shared LoadAsync path used by manual, notification, and command-driven refreshes.
        cut.Find("[data-testid='tenants-global-admins-refresh']").Click();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='tenants-global-admins-history-truncated']").ShouldBeEmpty());

        // Paging forward also keeps it retired: the operator is no longer on the jumped-to page.
        cut.Find("[data-testid='tenants-global-admins-next']").Click();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='tenants-global-admins-history-truncated']").ShouldBeEmpty());
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

        // Complete evidence: the count really is the platform total, so it carries the total label.
        cut.Find("[data-testid='tenants-global-admin-remove-count-label']")
            .TextContent.ShouldBe("Current administrator count");
        cut.Find("[data-testid='tenants-global-admin-remove-count']").TextContent.ShouldBe("2");
    }

    /// <summary>
    /// The preview count and its label must come from the same evidence.
    /// </summary>
    /// <remarks>
    /// The count rendered <c>_removeSnapshot.PreviewRows</c>, captured at preview time, while its label was
    /// selected from the live <c>_snapshot.IsCompleteEvidence</c>. A notification refresh landing between
    /// preview and render therefore paired "Current administrator count" -- a claim about the whole platform
    /// -- with a page-one count, or the converse, on the destructive flow. Both are now read from the preview
    /// snapshot. The label and the id had zero test references before this.
    /// </remarks>
    [Fact]
    public void Remove_preview_count_and_label_survive_a_refresh_that_changes_evidence_completeness()
    {
        GlobalAdministratorsSnapshot completePage = GlobalAdministratorsSnapshot.Ready(
            [
                new GlobalAdministratorRow("target-admin", ReadModelFreshnessState.Current),
                new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current),
            ],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            IsCompleteEvidence = true,
        };

        // The same rows, but the platform grew a second page while the dialog was open.
        GlobalAdministratorsSnapshot pagedRefresh = completePage with
        {
            NextCursor = "page-2",
            HasMore = true,
            IsCompleteEvidence = false,
        };

        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        StubTenantQueryGateway gateway = new(completePage, pagedRefresh);
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.Find("[data-testid='tenants-global-admin-remove']").Click();

        string previewCount = cut.Find("[data-testid='tenants-global-admin-remove-count']").TextContent;
        cut.Find("[data-testid='tenants-global-admin-remove-count-label']")
            .TextContent.ShouldBe("Current administrator count");

        // Refresh under the open dialog: the live snapshot is now page-scoped.
        cut.Find("[data-testid='tenants-global-admins-refresh']").Click();
        cut.WaitForAssertion(() => gateway.Requests.Count.ShouldBe(2));

        // The captured count is unchanged, so its label must be too -- the pair still describes one snapshot.
        cut.Find("[data-testid='tenants-global-admin-remove-count']").TextContent.ShouldBe(previewCount);
        cut.Find("[data-testid='tenants-global-admin-remove-count-label']")
            .TextContent.ShouldBe("Current administrator count");
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

    [Fact]
    public async Task Cancelling_grant_does_not_cancel_in_flight_remove_status_tracking()
    {
        ITenantCommandGateway commandGateway = Substitute.For<ITenantCommandGateway>();
        var pendingStatus = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken removeStatusToken = default;
        commandGateway
            .RemoveGlobalAdministratorAsync(Arg.Any<RemoveGlobalAdministrator>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantCommandSubmissionResult.Accepted("message-remove", "correlation-remove")));
        commandGateway
            .GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                removeStatusToken = call.ArgAt<CancellationToken>(1);
                return pendingStatus.Task;
            });
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [
                    new GlobalAdministratorRow("target-admin", ReadModelFreshnessState.Current),
                    new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current),
                ],
                null,
                false,
                "\"etag\"",
                ReadModelFreshnessState.Current) with
            {
                Lifecycle = ProjectionLifecycleState.Current,
                IsCompleteEvidence = true,
            }));
        Services.AddSingleton(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("grant-candidate");
        cut.Find("[data-testid='tenants-global-admin-remove']").Click();
        Task removeSubmit = cut.Find("[data-testid='tenants-global-admin-remove-submit']")
            .ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => removeStatusToken.CanBeCanceled.ShouldBeTrue());

        cut.Find("[data-testid='tenants-global-admin-grant-cancel']").Click();

        removeStatusToken.IsCancellationRequested.ShouldBeFalse();
        pendingStatus.SetResult(TenantCommandStatusResult.Unknown("Status remains pending."));
        await removeSubmit;
        cut.Find("[data-testid='tenants-global-admin-remove-state']").TextContent
            .ShouldNotContain("No global administrator remove command");
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
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true },
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
                null,
                false,
                "\"etag-2\"",
                // IsCompleteEvidence matches what the gateway computes for this shape (null cursor,
                // HasMore false, Current freshness and lifecycle). Without it the re-query reads as
                // page-scoped evidence, which is a different terminal message from a genuine
                // "did not confirm" -- and this test is asserting the latter.
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true });
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

        // Both halves of the breakpoint, separately. A bare `ShouldContain(".global-admins__mobile-readonly-host")`
        // is satisfied by either the base `display: none` rule or the media-query `display: block` one, so
        // deleting the base rule -- which makes the read-only notice permanently visible on desktop, the
        // exact regression this replaced -- left the assertion green. Likewise, hoisting the
        // mutation-initiation rule out of the media query hides Grant and every Remove launcher at all
        // widths, also undetected.
        // Guarded: an unguarded IndexOf slice throws ArgumentOutOfRangeException when the media query is
        // renamed, which reports as an error rather than as the assertion failure it actually is.
        int breakpointIndex = styles.IndexOf("@media (max-width: 42rem)", StringComparison.Ordinal);
        breakpointIndex.ShouldBeGreaterThan(-1);
        string mobileBreakpoint = styles[breakpointIndex..];
        string beforeBreakpoint = styles[..breakpointIndex];

        beforeBreakpoint.Contains(".global-admins__mobile-readonly-host", StringComparison.Ordinal)
            .ShouldBeTrue("the notice must be hidden by default, or it is permanently visible on desktop");
        mobileBreakpoint.Contains(".global-admins__mobile-readonly-host", StringComparison.Ordinal)
            .ShouldBeTrue("the notice must be revealed at the mobile breakpoint");
        beforeBreakpoint.Contains("::deep .global-admins__mutation-initiation", StringComparison.Ordinal)
            .ShouldBeFalse("hoisting the launcher rule out of the media query hides mutation affordances at every width");
        mobileBreakpoint.Contains("::deep .global-admins__mutation-initiation", StringComparison.Ordinal)
            .ShouldBeTrue();
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

        /// <summary>Set to make the async resolver throw, exercising the page's fail-closed catch.</summary>
        public bool ThrowFromAsyncResolution { get; set; }

        /// <summary>Counts async resolutions, proving the page does not fall back to the sync property.</summary>
        public int AsyncResolutionCount { get; private set; }

        /// <summary>
        /// Deliberately NOT the value the page should read. Overriding the async resolver below was the
        /// missing piece: without it every test here hit the default interface implementation, which
        /// returns this property, so swapping the page's call back to claims-only authorization kept the
        /// whole suite green and the fail-closed catch was unreachable because the default cannot throw.
        /// </summary>
        public TenantLifecycleAuthorizationReflectionState GlobalAdministratorsAuthorizationReflection
            => TenantLifecycleAuthorizationReflectionState.Indeterminate;

        public ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveGlobalAdministratorsAuthorizationAsync(
            CancellationToken cancellationToken = default)
        {
            AsyncResolutionCount++;
            return ThrowFromAsyncResolution
                ? throw new InvalidOperationException("principal resolution failed")
                : ValueTask.FromResult(Reflection);
        }
    }

    private sealed class StubTenantQueryGateway(params GlobalAdministratorsSnapshot[] snapshots) : ITenantQueryGateway
    {
        /// <summary>
        /// Explicit because <c>ITenantQueryGateway.GetTenantUsersAsync</c> is no longer a default interface
        /// method. This stub returns <c>Unavailable</c> deliberately: these tests do not exercise the member
        /// read, and an unavailable member surface is the correct fail-closed shape for them. Note this is
        /// the same value the removed default interface method returned, so a member-read regression is NOT
        /// caught here -- it is caught by the member-specific suites in
        /// <c>TenantDetailSurfaceTests</c>. (An earlier version of this remark claimed the opposite.)
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
        private GlobalAdministratorsSnapshot? _lastSnapshot;

        /// <summary>
        /// When set, the final queued snapshot is returned again for any further read instead of the queue
        /// underflowing. A fixed-length queue sized to the exact number of expected reads turns one extra
        /// read into an <see cref="InvalidOperationException"/> thrown from inside the gateway call, which
        /// the page does not catch -- so the test fails with a queue error rather than naming the behaviour.
        /// </summary>
        public bool RepeatLastResponse { get; init; }

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
            if (_queuedResponses.Count > 0)
            {
                return _queuedResponses.Dequeue();
            }

            if (_snapshots.Count > 0)
            {
                _lastSnapshot = _snapshots.Dequeue();
            }
            else if (!RepeatLastResponse)
            {
                _ = _snapshots.Dequeue();
            }

            return Task.FromResult(_lastSnapshot!);
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
            // Without these, ProjectionLifecycleBadge rendered the literal resource key back through this
            // echoing stub, so the badge's label was never really asserted and only its CSS class was --
            // which project rules forbid relying on alone.
            ["Tenants.ProjectionLifecycle.Current"] = "Current",
            ["Tenants.ProjectionLifecycle.Stale"] = "Stale",
            ["Tenants.ProjectionLifecycle.Unknown"] = "Unknown",
            ["Tenants.ProjectionLifecycle.Rebuilding"] = "Rebuilding",
            ["Tenants.ProjectionLifecycle.Degraded"] = "Degraded",
            ["Tenants.ProjectionLifecycle.Unavailable"] = "Unavailable",
            ["Tenants.ProjectionLifecycle.LocalOnly"] = "Local only",
            ["Tenants.Copy.Action"] = "Copy",
            ["Tenants.Copy.Feedback.Copied"] = "Copied.",
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
            ["Tenants.GlobalAdministrators.Remove.Unavailable.Incomplete"] = "Current complete projection evidence is required before removing platform authority.",
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
            ["Tenants.GlobalAdministrators.Recovery.HistoryTruncated"] = "Paging history reached its limit, so this step went back to the first page instead of the previous one.",
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

    [Fact]
    public async Task Cancelling_a_previewed_remove_does_not_cancel_in_flight_grant_status_tracking()
    {
        // The converse of Cancelling_grant_does_not_cancel_in_flight_remove_status_tracking. Only the
        // grant->remove direction was covered, so restoring the shared-invalidation defect in the
        // remove->grant direction kept the whole suite green -- while in production it discarded an accepted
        // grant's tracking handle and left its snapshot stuck at RequestSent.
        ITenantCommandGateway commandGateway = Substitute.For<ITenantCommandGateway>();
        var pendingStatus = new TaskCompletionSource<TenantCommandStatusResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken grantStatusToken = default;
        commandGateway
            .SetGlobalAdministratorAsync(Arg.Any<SetGlobalAdministrator>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(TenantCommandSubmissionResult.Accepted("message-grant", "correlation-grant")));
        commandGateway
            .GetStatusAsync(Arg.Any<TenantCommandTrackingHandle>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                grantStatusToken = call.ArgAt<CancellationToken>(1);
                return pendingStatus.Task;
            });
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [
                    new GlobalAdministratorRow("target-admin", ReadModelFreshnessState.Current),
                    new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current),
                ],
                null,
                false,
                "\"etag\"",
                ReadModelFreshnessState.Current) with
            {
                Lifecycle = ProjectionLifecycleState.Current,
                IsCompleteEvidence = true,
            }));
        Services.AddSingleton(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        // Preview a removal without submitting it, then submit a grant.
        cut.Find("[data-testid='tenants-global-admin-remove']").Click();
        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("grant-candidate");
        Task grantSubmit = cut.Find("[data-testid='tenants-global-admin-grant-form']").SubmitAsync();
        cut.WaitForAssertion(() => grantStatusToken.CanBeCanceled.ShouldBeTrue());

        cut.Find("[data-testid='tenants-global-admin-remove-cancel']").Click();

        grantStatusToken.IsCancellationRequested.ShouldBeFalse();
        pendingStatus.SetResult(TenantCommandStatusResult.Unknown("Status remains pending."));
        await grantSubmit;
        cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent
            .ShouldNotContain("No global administrator grant command");
    }

    [Fact]
    public void Client_local_paging_history_is_not_population_evidence_for_the_last_administrator_stop()
    {
        // The removed fail-open. On page 2+ the completeness gate is false, so the LastAdmin branch is
        // skipped; treating a non-empty cursor history as evidence that other administrators exist then
        // re-enabled Remove against a platform that may have exactly one administrator left. Only
        // server-stated population counts may satisfy the gate.
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(
            // Page one: HasMore, so paging forward is offered.
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-page-1", ReadModelFreshnessState.Current)],
                "cursor-page-2",
                true,
                "\"etag-1\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current },
            // Page two: the last page (HasMore false) and a cursor was used, so IsCompleteEvidence is false.
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-page-2", ReadModelFreshnessState.Current)],
                null,
                false,
                "\"etag-2\"",
                ReadModelFreshnessState.Current) with
            {
                Lifecycle = ProjectionLifecycleState.Current,
                RequestCursor = "cursor-page-2",
            }));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.WaitForElement("[data-testid='tenants-global-admins-next']").Click();

        // On page two the history is non-empty, HasMore is false and evidence is incomplete: Remove must
        // stay unavailable rather than trusting client-local paging bookkeeping.
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldContain("admin-page-2");

            // The launcher is replaced by its unavailability reason rather than rendered disabled.
            cut.FindAll("[data-testid='tenants-global-admin-remove']").ShouldBeEmpty();
            cut.Find("[data-testid='tenants-global-admins-remove-reason']").TextContent
                .ShouldContain("Current complete projection evidence is required before removing platform authority.");
        });
    }

    [Fact]
    public async Task Remove_submission_is_blocked_when_a_refresh_removes_the_previewed_target_from_the_page()
    {
        // The TargetMissing re-check. Preview is not a durable authorization: a refresh replaces _snapshot
        // underneath an open preview without resetting the intent, so the submit path must re-derive the
        // gates against the CURRENT snapshot. Only the LastAdmin branch was covered, so replacing this
        // condition with `false` kept the suite green.
        var commandGateway = new StubTenantCommandGateway();
        var gateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [
                    new GlobalAdministratorRow("target-admin", ReadModelFreshnessState.Current),
                    new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current),
                    new GlobalAdministratorRow("third-admin", ReadModelFreshnessState.Current),
                ],
                nextCursor: null,
                hasMore: false,
                eTag: "\"etag\"",
                freshness: ReadModelFreshnessState.Current) with
            { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true },
            // The previewed target is gone, but two administrators remain -- so the LastAdmin branch cannot
            // be what blocks the submit. Only the TargetMissing re-check can.
            GlobalAdministratorsSnapshot.Ready(
                [
                    new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current),
                    new GlobalAdministratorRow("third-admin", ReadModelFreshnessState.Current),
                ],
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
        cut.Find("[data-testid='tenants-global-admin-remove-target']").TextContent.ShouldContain("target-admin");
        cut.Find("[data-testid='tenants-global-admin-remove-submit']").HasAttribute("disabled").ShouldBeFalse();

        await cut.Find("[data-testid='tenants-global-admins-refresh']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(2));

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='tenants-global-admin-remove-submit']").HasAttribute("disabled").ShouldBeTrue());
        commandGateway.RemoveGlobalAdministratorCalls.ShouldBe(0);
    }

    [Fact]
    public void The_page_authorizes_through_the_async_resolver_not_the_synchronous_claims_property()
    {
        // The stub's synchronous property returns Indeterminate while its async resolver returns Authorized.
        // A page that reverted to claims-only authorization would render the denied surface.
        StubTenantsBffComposition composition = new(TenantLifecycleAuthorizationReflectionState.Authorized);
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with
        { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true }));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldContain("admin-1"));
        composition.AsyncResolutionCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void A_throwing_principal_resolution_fails_closed_to_the_denied_surface()
    {
        // The page's `catch { return Indeterminate; }` was unreachable in test, because the default
        // interface implementation it was hitting cannot throw.
        StubTenantsBffComposition composition = new(TenantLifecycleAuthorizationReflectionState.Authorized)
        {
            ThrowFromAsyncResolution = true,
        };
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with
        { Lifecycle = ProjectionLifecycleState.Current, IsCompleteEvidence = true }));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.WaitForAssertion(() =>
            cut.FindAll("[data-testid='tenants-global-admins-user-id']").ShouldBeEmpty());
        composition.AsyncResolutionCount.ShouldBeGreaterThan(0);
    }
}
