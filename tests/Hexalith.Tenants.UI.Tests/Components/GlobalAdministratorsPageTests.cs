using System.Globalization;
using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.Services.Configuration;
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
using Microsoft.Extensions.Logging;
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
            new StubTenantsBffComposition(
                TenantLifecycleAuthorizationReflectionState.Authorized,
                principalSource: authentication));
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
            new StubTenantsBffComposition(
                TenantLifecycleAuthorizationReflectionState.Indeterminate,
                principalSource: authentication));
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
    public async Task Authentication_transition_from_a_later_page_announces_the_page_one_restart()
    {
        var authentication = new MutableAuthenticationStateProvider(GlobalAdministratorPrincipal());
        var gateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin.page-one", ReadModelFreshnessState.Current)],
                nextCursor: "protected-page-two",
                hasMore: true,
                eTag: "\"page-one\"",
                freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current },
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin.page-two", ReadModelFreshnessState.Current)],
                nextCursor: null,
                hasMore: false,
                eTag: "\"page-two\"",
                freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current },
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin.reauthorized", ReadModelFreshnessState.Current)],
                nextCursor: null,
                hasMore: false,
                eTag: "\"reauthorized\"",
                freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(
            TenantLifecycleAuthorizationReflectionState.Authorized,
            principalSource: authentication));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        await cut.Find("[data-testid='tenants-global-admins-next']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-global-admins-user-id']")
            .TextContent.ShouldBe("admin.page-two"));

        authentication.Set(GlobalAdministratorPrincipal());

        cut.WaitForAssertion(() =>
        {
            gateway.GlobalAdministratorCalls.ShouldBe(3);
            gateway.Requests[2].Cursor.ShouldBeNull();
            IElement notice = cut.Find("[data-testid='tenants-global-admins-authorization-page-reset']");
            notice.GetAttribute("role").ShouldBe("status");
            notice.GetAttribute("aria-live").ShouldBe("polite");
            notice.TextContent.ShouldBe("Authorization changed. The review restarted at the first page.");
        });
    }

    [Fact]
    public void Real_principal_resolver_authorizes_the_page_outside_an_inbound_circuit_activity()
    {
        var authentication = new MutableAuthenticationStateProvider(GlobalAdministratorPrincipal());
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        userContext.UserId.Returns("operator.alpha");
        var principalResolver = new TenantConfigurationPrincipalResolver(
            new CircuitServicesAccessor(),
            userContext,
            authentication);
        var composition = new TenantsBffComposition(
            new StubTenantCommandGateway(),
            principalResolver: principalResolver,
            readSurface: new TenantsReadSurfaceAvailability(IsConnected: true));
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.real-resolver", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"real-resolver\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        gateway.GlobalAdministratorCalls.ShouldBe(1);
        cut.Find("[data-testid='tenants-global-admins-user-id']")
            .TextContent.ShouldBe("admin.real-resolver");

        authentication.Set(NonAdministratorPrincipal());
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admins-unavailable']");
            cut.Markup.ShouldNotContain("admin.real-resolver");
        });
    }

    /// <summary>
    /// The authentication-transition path writes Authorized from inside a dispatcher callback. Without a version
    /// re-check INSIDE that callback, a sign-out landing while the resolve is in flight is overwritten by the
    /// pre-sign-out answer: the privileged surface returns for an operator who has already signed out, and the
    /// post-hoc check cannot undo a write that has already rendered. Deleting the in-callback guard fails here.
    /// </summary>
    [Fact]
    public async Task A_sign_out_landing_during_a_transition_resolve_is_not_overwritten_by_the_stale_answer()
    {
        var authentication = new MutableAuthenticationStateProvider(GlobalAdministratorPrincipal());
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.before-signout", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"before-signout\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.Find("[data-testid='tenants-global-admins-list']");
        gateway.GlobalAdministratorCalls.ShouldBe(1);

        // A transition starts and its resolve suspends while still holding the pre-sign-out Authorized answer.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        composition.ResolutionGate = gate;
        authentication.Set(GlobalAdministratorPrincipal());
        await composition.ResolutionEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // The operator signs out. This bumps the transition version and collapses the surface.
        composition.Reflection = TenantLifecycleAuthorizationReflectionState.MissingPermission;
        authentication.Set(NonAdministratorPrincipal());
        cut.WaitForAssertion(
            () => cut.Find("[data-testid='tenants-global-admins-restricted-header']"),
            TimeSpan.FromSeconds(5));

        // Only now does the superseded resolve complete, answering Authorized.
        gate.SetResult();

        // Bounded settle: the assertion is that nothing further happens, which no retry-until-true wait can
        // express -- WaitForAssertion would pass on the current state before the stale continuation ran.
        await Task.Delay(250);

        cut.Find("[data-testid='tenants-global-admins-restricted-header']");
        cut.FindAll("[data-testid='tenants-global-admins-page-header']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-global-admins-list']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("admin.before-signout");
        gateway.GlobalAdministratorCalls.ShouldBe(1);
    }

    /// <summary>
    /// The initial resolve must not be able to miss a transition. Subscribing to AuthenticationStateChanged only
    /// AFTER the resolve completed meant a sign-out landing during initialization fired against no handler at
    /// all and was lost, and the pre-sign-out answer was then written unconditionally -- leaving the full
    /// privileged surface rendered for a signed-out principal with no later transition to correct it. Moving the
    /// subscription back after the resolve, or dropping the version capture, fails here.
    /// </summary>
    /// <remarks>
    /// The read surface is deliberately disconnected. With it connected the initial apply falls through to
    /// <c>LoadAsync</c>, whose default <c>reauthorize: true</c> re-resolves and collapses the surface anyway --
    /// so the missed event is masked and the test passes even with the subscription moved back after the
    /// resolve. Disconnected, the apply takes the early return and nothing re-authorizes, which is what makes
    /// the subscription ordering itself observable.
    /// </remarks>
    [Fact]
    public async Task A_sign_out_during_initial_authorization_resolution_is_observed_and_wins()
    {
        var authentication = new MutableAuthenticationStateProvider(GlobalAdministratorPrincipal());
        var composition = new StubTenantsBffComposition(
            TenantLifecycleAuthorizationReflectionState.Authorized,
            isReadSurfaceConnected: false);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        composition.ResolutionGate = gate;
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.initial", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"initial\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        await composition.ResolutionEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // The sign-out lands while initialization is still resolving. The page must already be subscribed.
        composition.Reflection = TenantLifecycleAuthorizationReflectionState.MissingPermission;
        authentication.Set(NonAdministratorPrincipal());

        // Wait for the transition's own resolve before releasing the stale one, so the ordering under test is
        // deterministic rather than a race with the handler being dispatched. If the page is not subscribed --
        // the mutation this test exists to catch -- no second resolution ever happens and this times out.
        // Polled rather than WaitForAssertion: that helper only re-evaluates on a render, and this counter is
        // not render-driven, so it checked once and timed out under load.
        await WaitUntilAsync(() => composition.AsyncResolutionCount >= 2, TimeSpan.FromSeconds(5));

        gate.SetResult();
        await Task.Delay(250);

        // The restricted header, not the authorized area: the sign-out was seen and beat the stale answer.
        // `tenants-global-admins-unavailable` is NOT a discriminator here -- it renders in the authorized
        // branch too, for an Unavailable snapshot.
        // The restricted header, not the authorized page header: the sign-out was seen and beat the stale
        // answer. Neither `tenants-global-admins-area` nor `tenants-global-admins-unavailable` discriminates --
        // the restricted branch deliberately keeps the page area and publishes its own unavailable region.
        cut.Find("[data-testid='tenants-global-admins-restricted-header']");
        cut.FindAll("[data-testid='tenants-global-admins-page-header']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("admin.initial");
        gateway.GlobalAdministratorCalls.ShouldBe(0);
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
            new StubTenantsBffComposition(
                TenantLifecycleAuthorizationReflectionState.Authorized,
                principalSource: authentication));
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
            .GetAttribute("role").ShouldBe("status");
        cut.Find("[data-testid='tenants-global-admins-projection-lifecycle-status-badge']")
            .GetAttribute("class").ShouldNotBeNull().ShouldContain("projection-lifecycle-badge--current");
        cut.Find("[data-testid='tenants-global-admins-projection-lifecycle-status-badge']")
            .TextContent.Trim().ShouldBe("Current");
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
    public async Task Indeterminate_authorization_offers_a_safe_retry_before_querying()
    {
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Indeterminate);
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.after-retry", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"after-retry\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        gateway.GlobalAdministratorCalls.ShouldBe(0);
        cut.Find("[data-testid='tenants-global-admins-unavailable']");
        cut.Markup.ShouldNotContain("admin.after-retry");

        composition.Reflection = TenantLifecycleAuthorizationReflectionState.Authorized;
        await cut.Find("[data-testid='tenants-global-admins-authorization-retry']")
            .ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            gateway.GlobalAdministratorCalls.ShouldBe(1);
            cut.Find("[data-testid='tenants-global-admins-user-id']")
                .TextContent.ShouldBe("admin.after-retry");
        });
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

    [Fact]
    public async Task Rows_free_retry_reopens_the_notification_setup_budget()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        int setupAttempts = 0;
        subscription
            .SubscribeAsync(
                GetGlobalAdministratorsQuery.ProjectionType,
                "system",
                Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref setupAttempts) <= 3
                ? Task.FromException(new HttpRequestException("transient setup failure"))
                : Task.CompletedTask);
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Error())
        {
            RepeatLastResponse = true,
        };
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        for (int render = 0; render < 10; render++)
        {
            cut.Render();
        }

        Volatile.Read(ref setupAttempts).ShouldBe(3);
        cut.FindAll("[data-testid='tenants-global-admins-list']").ShouldBeEmpty();

        await cut.Find("[data-testid='tenants-global-admins-retry']").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() => Volatile.Read(ref setupAttempts).ShouldBe(4));
        await subscription.Received(4).SubscribeAsync(
            GetGlobalAdministratorsQuery.ProjectionType,
            "system",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Notification_setup_failure_logs_only_a_support_safe_reason_code()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        var logger = new CapturingUntypedLogger();
        ILoggerFactory loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Empty(
            isAuthorizationScoped: true,
            ReadModelFreshnessState.Current,
            eTag: null))
        {
            RepeatLastResponse = true,
        };
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton(subscription);
        Services.AddSingleton<IProjectionChangeNotifierWithTenant>(new ThrowingProjectionNotifier());
        Services.AddSingleton(loggerFactory);
        Services.AddScoped<TenantReadRefreshSubscription>();

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        const string expected = "Optional projection refresh setup failed. ReasonCode=notification-setup-failed";
        cut.WaitForAssertion(() => logger.Messages.ShouldContain(expected));
        int entry = logger.Messages.IndexOf(expected);
        logger.Levels[entry].ShouldBe(LogLevel.Warning);
        logger.Exceptions[entry].ShouldBeNull();
        string logged = logger.Messages[entry];
        logged.ShouldNotContain("system", Case.Insensitive);
        logged.ShouldNotContain("global-administrators", Case.Insensitive);
        logged.ShouldNotContain(nameof(InvalidOperationException));
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
    public void Page_load_mutual_exclusion_uses_an_atomic_test_and_set_gate()
    {
        string source = ReadGlobalAdministratorsPageSource();

        source.ShouldContain("private int _pageLoadInFlight;");
        Regex.IsMatch(
            source,
            @"Interlocked\.CompareExchange\s*\(\s*ref\s+_pageLoadInFlight\s*,\s*1\s*,\s*0\s*\)",
            RegexOptions.CultureInvariant).ShouldBeTrue();
        Regex.IsMatch(
            source,
            @"_pageLoadInFlight\s*=\s*true\s*;",
            RegexOptions.CultureInvariant).ShouldBeFalse();
    }

    [Fact]
    public void Initialization_marshals_authorization_and_terminal_snapshot_state_to_the_renderer()
    {
        string source = ReadGlobalAdministratorsPageSource();
        string initialization = ExtractMethodBody(source, "protected override async Task OnInitializedAsync()");

        initialization.ShouldContain("await InvokeAsync(() =>");
        Regex.IsMatch(
            initialization,
            @"_authorizationReflection\s*=\s*await",
            RegexOptions.CultureInvariant).ShouldBeFalse();
    }

    [Fact]
    public void Retry_authorization_releases_the_page_load_gate_outside_the_renderer_dispatch()
    {
        string source = ReadGlobalAdministratorsPageSource();
        string retryAuthorization = ExtractMethodBody(source, "internal async Task RetryAuthorizationAsync()");

        Regex.IsMatch(
            retryAuthorization,
            @"finally\s*\{[^}]*EndPageLoad\s*\(\s*\)",
            RegexOptions.Singleline | RegexOptions.CultureInvariant).ShouldBeTrue();
        Regex.IsMatch(
            retryAuthorization,
            @"finally\s*\{[^}]*await\s+InvokeAsync\s*\([^)]*EndPageLoad",
            RegexOptions.Singleline | RegexOptions.CultureInvariant).ShouldBeFalse();
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
    public void Lifecycle_and_recovery_controls_are_outside_the_assertive_truth_region()
    {
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Error());
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        IElement assertiveState = cut.Find("[data-testid='tenants-global-admins-error']");
        assertiveState.QuerySelector("[data-testid='tenants-global-admins-projection-lifecycle-status']")
            .ShouldBeNull();
        assertiveState.QuerySelector("[data-testid='tenants-global-admins-retry']").ShouldBeNull();
        assertiveState.QuerySelector("[data-testid='tenants-global-admins-reset']").ShouldBeNull();
        cut.Find("[data-testid='tenants-global-admins-projection-lifecycle-status']");
        cut.Find("[data-testid='tenants-global-admins-retry']");
        cut.Find("[data-testid='tenants-global-admins-reset']");
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

    [Fact]
    public void Unknown_review_surface_with_rows_keeps_list_visible_and_actions_unavailable()
    {
        GlobalAdministratorsSnapshot snapshot = GlobalAdministratorsSnapshot.Unknown(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Unknown)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"etag\"");
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(snapshot));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-list']");
        cut.Find("[data-testid='tenants-global-admins-row']").TextContent.ShouldContain("admin-1");
        cut.Find("[data-testid='tenants-global-admins-grant-reason']").TextContent.ShouldContain("freshness", Case.Insensitive);
        cut.Find("[data-testid='tenants-global-admins-remove-reason']").TextContent.ShouldContain("freshness", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-global-admin-remove']").ShouldBeEmpty();
    }

    [Fact]
    public void Ready_has_more_without_cursor_offers_incomplete_paging_recovery()
    {
        GlobalAdministratorsSnapshot snapshot = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: true,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            IsCompleteEvidence = false,
        };
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(snapshot));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-next']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='tenants-global-admins-incomplete-paging']").TextContent
            .ShouldContain("no continuation page", Case.Insensitive);
        cut.Find("[data-testid='tenants-global-admins-reset']");
        cut.Find("[data-testid='tenants-global-admins-retry']");
    }

    [Fact]
    public void Current_freshness_without_projection_lifecycle_uses_lifecycle_unavailable_copy()
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
            Lifecycle = ProjectionLifecycleState.Unknown,
            IsCompleteEvidence = true,
        };
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(snapshot));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-grant-reason']").TextContent.ShouldContain("lifecycle", Case.Insensitive);
        cut.Find("[data-testid='tenants-global-admins-remove-reason']").TextContent.ShouldContain("lifecycle", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-global-admin-remove']").ShouldBeEmpty();
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

    [Fact]
    public void Page_scoped_remove_preview_labels_its_count_as_visible_rows_not_a_platform_total()
    {
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [
                new GlobalAdministratorRow("target-admin", ReadModelFreshnessState.Current),
                new GlobalAdministratorRow("other-visible-admin", ReadModelFreshnessState.Current),
            ],
            nextCursor: "opaque-next-page",
            hasMore: true,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            IsCompleteEvidence = false,
        }));
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admin-remove']").Click();

        cut.Find("[data-testid='tenants-global-admin-remove-count-label']")
            .TextContent.ShouldBe("Administrators visible on this page");
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
    public void Unsupported_grant_user_ids_stay_local_and_explain_the_supported_boundary()
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
        foreach (string unsupportedUserId in new[] { "target\u0001user", new string('u', 257) })
        {
            cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change(unsupportedUserId);
            cut.Find("[data-testid='tenants-global-admin-grant-form']").Submit();

            cut.Find("[data-testid='tenants-global-admin-grant-validation']").TextContent.ShouldContain("256");
        }

        commandGateway.SetGlobalAdministratorCalls.ShouldBe(0);
    }

    [Fact]
    public void Grant_user_id_input_does_not_expose_maxlength_truncation()
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

        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").GetAttribute("maxlength").ShouldBeNull();
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
            .ShouldContain("Current projection lifecycle evidence is required before granting platform authority.");

        cut.FindAll("[data-testid='tenants-global-admin-remove']").ShouldBeEmpty();
        cut.FindAll("[data-testid='tenants-global-admins-remove-reason']")
            .Select(static element => element.TextContent)
            .ShouldAllBe(static text => text.Contains("Current projection lifecycle evidence is required before removing platform authority.", StringComparison.Ordinal));
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
        beforeBreakpoint.Contains(".global-admins__mobile-mutation-reason", StringComparison.Ordinal)
            .ShouldBeTrue("per-row mobile reasons must be hidden by default");
        mobileBreakpoint.Contains(".global-admins__mobile-mutation-reason", StringComparison.Ordinal)
            .ShouldBeTrue("per-row mobile reasons must be revealed at the mobile breakpoint");
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

        // Endpoint authorization is attached conditionally by Program.cs when OIDC is configured, not as
        // static component metadata. The component must therefore remain routable on the Keycloak-disabled
        // topology, where its rendered state is the fail-closed authority.
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

    private static string ReadGlobalAdministratorsPageSource()
        => File.ReadAllText(Path.Combine(
            ProjectRoot(),
            "src",
            "Hexalith.Tenants.UI",
            "Components",
            "Pages",
            "GlobalAdministratorsPage.razor"));

    private static string ExtractMethodBody(string source, string methodSignature)
    {
        int start = source.IndexOf(methodSignature, StringComparison.Ordinal);
        start.ShouldBeGreaterThan(-1, $"Method '{methodSignature}' was not found in GlobalAdministratorsPage.razor.");
        int braceStart = source.IndexOf('{', start);
        braceStart.ShouldBeGreaterThan(start);
        int depth = 0;
        for (int index = braceStart; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[start..(index + 1)];
                }
            }
        }

        throw new InvalidOperationException($"Could not extract the body for '{methodSignature}'.");
    }

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

    /// <summary>
    /// A page reporting more results with no cursor to reach them must render Next disabled rather than
    /// clickable-and-silent. Every other Next assertion in this file clicks a snapshot that carries a cursor,
    /// so dropping the blank-cursor clause from the binding stayed green.
    /// </summary>
    [Fact]
    public void Next_is_disabled_when_the_service_reports_more_results_without_a_cursor()
    {
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.only-page", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: true,
            eTag: "\"no-cursor\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();

        cut.Find("[data-testid='tenants-global-admins-next']").HasAttribute("disabled").ShouldBeTrue();
        gateway.GlobalAdministratorCalls.ShouldBe(1);
    }

    /// <summary>
    /// Grant requery must not confirm from a snapshot object that is no longer the one on screen. The pagers
    /// already guard with <c>ReferenceEquals</c>; without it on the requery path a notification refresh landing
    /// mid-requery would report "confirmed" from rows the operator never saw.
    /// </summary>
    [Fact]
    public async Task Grant_requery_does_not_confirm_from_a_superseded_snapshot()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        GlobalAdministratorsSnapshot notificationSnapshot = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("notification-admin", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"notification\"",
            ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current };
        GlobalAdministratorsSnapshot confirmingSnapshot = GlobalAdministratorsSnapshot.Ready(
            [
                new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current),
                new GlobalAdministratorRow("target-user", ReadModelFreshnessState.Current),
            ],
            null,
            false,
            "\"confirmed\"",
            ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current };
        var queryGateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
                null,
                false,
                "\"etag-1\"",
                ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        var commandGateway = new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Accepted("message-grant", "correlation-grant"),
            new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1));
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        await subscription.Received(1).SubscribeAsync(
            GetGlobalAdministratorsQuery.ProjectionType,
            "system",
            Arg.Any<CancellationToken>());

        queryGateway.QueueResponse(Task.Run(async () =>
        {
            await Task.Delay(100);
            return confirmingSnapshot;
        }));
        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("target-user");
        Task grantSubmit = cut.Find("[data-testid='tenants-global-admin-grant-form']").SubmitAsync();
        queryGateway.QueueResponse(Task.FromResult(notificationSnapshot));
        await cut.InvokeAsync(() =>
        {
            notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(
                GetGlobalAdministratorsQuery.ProjectionType,
                "system");
            return Task.CompletedTask;
        });
        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("notification-admin"));

        await grantSubmit;

        cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent
            .ShouldNotContain("Projection confirmed the target user", Case.Insensitive);
        cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("notification-admin");
    }

    /// <summary>
    /// The remove requery path carries the same supersession guard as grant. Without
    /// <c>ReferenceEquals(_snapshot, snapshot)</c> a mid-requery notification could confirm removal while the
    /// operator still sees the target on screen.
    /// </summary>
    [Fact]
    public async Task Remove_requery_does_not_confirm_from_a_superseded_snapshot()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        GlobalAdministratorsSnapshot notificationSnapshot = GlobalAdministratorsSnapshot.Ready(
            [
                new GlobalAdministratorRow("target-admin", ReadModelFreshnessState.Current),
                new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current),
            ],
            null,
            false,
            "\"notification\"",
            ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            IsCompleteEvidence = true,
        };
        GlobalAdministratorsSnapshot confirmingSnapshot = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"confirmed\"",
            ReadModelFreshnessState.Current) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            IsCompleteEvidence = true,
        };
        var queryGateway = new StubTenantQueryGateway(
            GlobalAdministratorsSnapshot.Ready(
                [
                    new GlobalAdministratorRow("target-admin", ReadModelFreshnessState.Current),
                    new GlobalAdministratorRow("other-admin", ReadModelFreshnessState.Current),
                ],
                null,
                false,
                "\"etag-1\"",
                ReadModelFreshnessState.Current) with
            {
                Lifecycle = ProjectionLifecycleState.Current,
                IsCompleteEvidence = true,
            });
        var commandGateway = new StubTenantCommandGateway(statuses: [new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1)])
        {
            RemoveSubmission = TenantCommandSubmissionResult.Accepted("message-remove", "correlation-remove"),
        };
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(queryGateway);
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        await subscription.Received(1).SubscribeAsync(
            GetGlobalAdministratorsQuery.ProjectionType,
            "system",
            Arg.Any<CancellationToken>());

        queryGateway.QueueResponse(Task.Run(async () =>
        {
            await Task.Delay(100);
            return confirmingSnapshot;
        }));
        cut.Find("[data-testid='tenants-global-admin-remove']").Click();
        Task removeSubmit = cut.Find("[data-testid='tenants-global-admin-remove-submit']").ClickAsync(new MouseEventArgs());
        queryGateway.QueueResponse(Task.FromResult(notificationSnapshot));
        await cut.InvokeAsync(() =>
        {
            notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(
                GetGlobalAdministratorsQuery.ProjectionType,
                "system");
            return Task.CompletedTask;
        });
        cut.WaitForAssertion(() =>
            cut.FindAll("[data-testid='tenants-global-admins-user-id']")
                .Select(static element => element.TextContent)
                .ShouldContain("target-admin"));

        await removeSubmit;

        cut.Find("[data-testid='tenants-global-admin-remove-state']").TextContent
            .ShouldNotContain("Projection confirmed removal", Case.Insensitive);
        cut.FindAll("[data-testid='tenants-global-admins-user-id']")
            .Select(static element => element.TextContent)
            .ShouldContain("target-admin");
    }

    /// <summary>
    /// Behavioral requery races can be invalidated by load-generation before <c>ReferenceEquals</c> is
    /// reached, so deleting that clause alone can stay green. Pin both requery sites structurally.
    /// </summary>
    [Fact]
    public void Grant_and_remove_requery_paths_guard_superseded_snapshots_with_reference_equality()
    {
        string source = ReadGlobalAdministratorsPageSource();
        string grantRequery = ExtractMethodBody(source, "private async Task RequeryGrantProjectionAsync(long generation)");
        string removeRequery = ExtractMethodBody(source, "private async Task RequeryRemoveProjectionAsync(long generation)");

        Regex.IsMatch(
            grantRequery,
            @"ReferenceEquals\s*\(\s*_snapshot\s*,\s*snapshot\s*\)",
            RegexOptions.CultureInvariant).ShouldBeTrue();
        Regex.IsMatch(
            removeRequery,
            @"ReferenceEquals\s*\(\s*_snapshot\s*,\s*snapshot\s*\)",
            RegexOptions.CultureInvariant).ShouldBeTrue();
    }

    /// <summary>
    /// <c>ReauthorizeAsync</c> captures a transition version before resolving. A sign-out landing while grant
    /// submission is re-authorizing must not be overwritten by the pre-sign-out answer and must not dispatch
    /// the platform command.
    /// </summary>
    [Fact]
    public async Task A_sign_out_during_grant_reauthorization_fails_closed_without_dispatching_the_command()
    {
        var authentication = new MutableAuthenticationStateProvider(GlobalAdministratorPrincipal());
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var commandGateway = new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Accepted("message-grant", "correlation-grant"));
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
            ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current })
        {
            RepeatLastResponse = true,
        });
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.Find("[data-testid='tenants-global-admins-list']");

        composition.ResolutionGate = gate;
        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("target-user");
        Task grantSubmit = cut.Find("[data-testid='tenants-global-admin-grant-form']").SubmitAsync();
        await composition.ResolutionEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        composition.Reflection = TenantLifecycleAuthorizationReflectionState.MissingPermission;
        authentication.Set(NonAdministratorPrincipal());
        cut.WaitForAssertion(
            () => cut.Find("[data-testid='tenants-global-admins-restricted-header']"),
            TimeSpan.FromSeconds(5));
        await WaitUntilAsync(() => composition.AsyncResolutionCount >= 2, TimeSpan.FromSeconds(5));

        gate.SetResult();
        await grantSubmit;

        cut.Find("[data-testid='tenants-global-admins-restricted-header']");
        commandGateway.SetGlobalAdministratorCalls.ShouldBe(0);
    }

    /// <summary>
    /// The fail-closed half of Indeterminate Retry: clicking retry while authorization stays indeterminate must
    /// not query privileged data or restore the authorized surface.
    /// </summary>
    [Fact]
    public async Task Indeterminate_authorization_retry_stays_fail_closed_when_reauthorization_remains_indeterminate()
    {
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Indeterminate);
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.after-retry", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"after-retry\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current });
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.Find("[data-testid='tenants-global-admins-restricted-header']");
        gateway.GlobalAdministratorCalls.ShouldBe(0);

        await cut.Find("[data-testid='tenants-global-admins-authorization-retry']")
            .ClickAsync(new MouseEventArgs());

        await Task.Delay(250);
        gateway.GlobalAdministratorCalls.ShouldBe(0);
        cut.Find("[data-testid='tenants-global-admins-restricted-header']");
        cut.FindAll("[data-testid='tenants-global-admins-page-header']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("admin.after-retry");
    }

    /// <summary>
    /// Hoisting <c>ReauthorizeAsync</c> consumed the automatic render at submit suspension; without the
    /// marshalled follow-up render the RequestSent lifecycle string never reached the DOM while the command was
    /// still in flight.
    /// </summary>
    [Fact]
    public async Task Grant_submission_announces_request_sent_while_the_command_is_in_flight()
    {
        var submissionGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var commandGateway = new StubTenantCommandGateway(
            TenantCommandSubmissionResult.Accepted("message-grant", "correlation-grant"),
            new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1))
        {
            SubmissionGate = submissionGate,
        };
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
            ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current }));
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.Find("[data-testid='tenants-global-admin-grant-user-id']").Change("target-user");
        Task grantSubmit = cut.Find("[data-testid='tenants-global-admin-grant-form']").SubmitAsync();
        await commandGateway.SubmissionEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Find("[data-testid='tenants-global-admin-grant-state']").TextContent
            .ShouldContain("Grant command request was sent.");
        submissionGate.SetResult();
        await grantSubmit;
    }

    /// <summary>
    /// Remove submit uses the same marshalled RequestSent render as grant.
    /// </summary>
    [Fact]
    public async Task Remove_submission_announces_request_sent_while_the_command_is_in_flight()
    {
        var submissionGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var commandGateway = new StubTenantCommandGateway(statuses: [new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1)])
        {
            RemoveSubmission = TenantCommandSubmissionResult.Accepted("message-remove", "correlation-remove"),
            RemoveSubmissionGate = submissionGate,
        };
        Services.AddSingleton<ITenantsBffComposition>(new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
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
        Services.AddSingleton<ITenantCommandGateway>(commandGateway);
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.Find("[data-testid='tenants-global-admin-remove']").Click();
        Task removeSubmit = cut.Find("[data-testid='tenants-global-admin-remove-submit']").ClickAsync(new MouseEventArgs());
        await commandGateway.RemoveSubmissionEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Find("[data-testid='tenants-global-admin-remove-state']").TextContent
            .ShouldContain("Remove command request was sent.");
        submissionGate.SetResult();
        await removeSubmit;
    }

    /// <summary>
    /// Reset paging must reopen the bounded notification-subscription budget after it was exhausted on the
    /// current circuit, not only Retry and Refresh.
    /// </summary>
    [Fact]
    public async Task Reset_paging_reopens_the_notification_setup_budget()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        int setupAttempts = 0;
        subscription
            .SubscribeAsync(
                GetGlobalAdministratorsQuery.ProjectionType,
                "system",
                Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref setupAttempts) <= 3
                ? Task.FromException(new HttpRequestException("transient setup failure"))
                : Task.CompletedTask);
        GlobalAdministratorsSnapshot recoverable = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.alpha", ReadModelFreshnessState.Stale)],
            nextCursor: "page-2",
            hasMore: true,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Stale) with { Lifecycle = ProjectionLifecycleState.Current };
        var gateway = new StubTenantQueryGateway(recoverable) { RepeatLastResponse = true };
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        for (int render = 0; render < 10; render++)
        {
            cut.Render();
        }

        Volatile.Read(ref setupAttempts).ShouldBe(3);

        await cut.Find("[data-testid='tenants-global-admins-reset']").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() => Volatile.Read(ref setupAttempts).ShouldBe(4));
        await subscription.Received(4).SubscribeAsync(
            GetGlobalAdministratorsQuery.ProjectionType,
            "system",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Authorization retry must reopen the bounded notification-subscription budget before a recovered authorized
    /// boundary can subscribe again.
    /// </summary>
    [Fact]
    public async Task Authorization_retry_reopens_the_notification_setup_budget()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        int setupAttempts = 0;
        subscription
            .SubscribeAsync(
                GetGlobalAdministratorsQuery.ProjectionType,
                "system",
                Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref setupAttempts) <= 3
                ? Task.FromException(new HttpRequestException("transient setup failure"))
                : Task.CompletedTask);
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        GlobalAdministratorsSnapshot ready = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.current", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"current\"",
            ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current };
        var gateway = new StubTenantQueryGateway(ready) { RepeatLastResponse = true };
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        for (int render = 0; render < 10; render++)
        {
            cut.Render();
        }

        Volatile.Read(ref setupAttempts).ShouldBe(3);

        FieldInfo authorizationReflectionField = typeof(GlobalAdministratorsPage)
            .GetField("_authorizationReflection", BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo readRefreshAttemptsField = typeof(GlobalAdministratorsPage)
            .GetField("_readRefreshAttempts", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await cut.InvokeAsync(() =>
        {
            authorizationReflectionField.SetValue(
                cut.Instance,
                TenantLifecycleAuthorizationReflectionState.Indeterminate);
            readRefreshAttemptsField.SetValue(cut.Instance, 3);
        });

        await cut.InvokeAsync(() => cut.Instance.RetryAuthorizationAsync());

        cut.WaitForAssertion(() => Volatile.Read(ref setupAttempts).ShouldBe(4));
        await subscription.Received(4).SubscribeAsync(
            GetGlobalAdministratorsQuery.ProjectionType,
            "system",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Refresh must take the same atomic page-load gate as Next. Without it, Refresh during an in-flight Next
    /// cancels the pager read and produces no navigation or feedback.
    /// </summary>
    [Fact]
    public async Task Refresh_does_not_start_a_read_while_next_page_load_is_in_flight()
    {
        GlobalAdministratorsSnapshot recoverable = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin.alpha", ReadModelFreshnessState.Current)],
            nextCursor: "page-2",
            hasMore: true,
            eTag: "\"etag\"",
            freshness: ReadModelFreshnessState.Current) with { Lifecycle = ProjectionLifecycleState.Current };
        var gateway = new StubTenantQueryGateway(recoverable) { RepeatLastResponse = true };
        Services.AddSingleton<ITenantsBffComposition>(
            new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized));
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());

        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(1));

        var pendingPage = new TaskCompletionSource<GlobalAdministratorsSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.QueueResponse(pendingPage.Task);
        Task nextClick = cut.Find("[data-testid='tenants-global-admins-next']")
            .ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(2));

        await cut.InvokeAsync(() => cut.Instance.RefreshAsync());

        gateway.GlobalAdministratorCalls.ShouldBe(
            2,
            "Refresh must not start another read while Next is in flight.");

        pendingPage.SetResult(recoverable);
        await nextClick;
    }

    /// <summary>
    /// A dispose racing a suspended subscribe continuation must not leave the lease attached after assignment.
    /// </summary>
    [Fact]
    public async Task A_dispose_racing_subscribe_assignment_does_not_retain_the_refresh_lease()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var subscribeGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        subscription
            .SubscribeAsync(
                GetGlobalAdministratorsQuery.ProjectionType,
                "system",
                Arg.Any<CancellationToken>())
            .Returns(_ => subscribeGate.Task);
        var gateway = new StubTenantQueryGateway(GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            null,
            false,
            "\"etag\"",
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
        cut.WaitForAssertion(() => subscription.ReceivedCalls()
            .Count(call => call.GetMethodInfo().Name == nameof(IProjectionSubscription.SubscribeAsync))
            .ShouldBeGreaterThanOrEqualTo(1));

        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
        subscribeGate.SetResult();
        await Task.Delay(250);

        await subscription.Received(1).UnsubscribeAsync(
            GetGlobalAdministratorsQuery.ProjectionType,
            "system",
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Polls a non-render-driven condition. <c>WaitForAssertion</c> re-evaluates only when the component
    /// renders, so it cannot observe counters on a test double.
    /// </summary>
    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + timeout;
        while (!condition())
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new TimeoutException("The awaited condition was not met within the timeout.");
            }

            await Task.Delay(10);
        }
    }

    private sealed class StubTenantsBffComposition(
        TenantLifecycleAuthorizationReflectionState reflection,
        bool isReadSurfaceConnected = true,
        bool isCommandSurfaceConnected = true,
        AuthenticationStateProvider? principalSource = null) : ITenantsBffComposition
    {
        public bool IsReadSurfaceConnected => isReadSurfaceConnected;

        public bool IsCommandSurfaceConnected => isCommandSurfaceConnected;

        public TenantLifecycleAuthorizationReflectionState Reflection { get; set; } = reflection;

        /// <summary>
        /// When set, the stub resolves from the live principal instead of a fixed value, modelling the real
        /// BFF seam. The page no longer evaluates the authentication event's principal itself -- it delegates
        /// every path, transitions included -- so a transition test must drive authorization through this seam.
        /// A fixed-value stub cannot express "the principal changed" and would pass whatever the page did.
        /// </summary>
        private readonly AuthenticationStateProvider? _principalSource = principalSource;

        /// <summary>Set to make the async resolver throw, exercising the page's fail-closed catch.</summary>
        public bool ThrowFromAsyncResolution { get; set; }

        /// <summary>
        /// Arms a one-shot suspension of the next fixed-value resolution, so a test can land an authentication
        /// transition while an earlier resolve is still in flight. Without this every stub resolution completes
        /// synchronously, the in-flight window is zero-width, and the page's transition-version guards are
        /// unobservable -- which is why reverting them kept the whole suite green.
        /// </summary>
        public TaskCompletionSource? ResolutionGate { get; set; }

        /// <summary>Completes when a gated resolution has entered and suspended.</summary>
        public TaskCompletionSource ResolutionEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

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

        public async ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveGlobalAdministratorsAuthorizationAsync(
            CancellationToken cancellationToken = default)
        {
            AsyncResolutionCount++;
            if (ThrowFromAsyncResolution)
            {
                throw new InvalidOperationException("principal resolution failed");
            }

            if (_principalSource is null)
            {
                // Captured at entry, before any suspension: this models a resolve whose answer was computed
                // from the evidence that was current when it started. A gated resolve therefore completes with
                // the PRE-transition answer, which is exactly the race the page's version guard must reject.
                TenantLifecycleAuthorizationReflectionState answer = Reflection;
                TaskCompletionSource? gate = ResolutionGate;
                if (gate is not null)
                {
                    // One-shot: only the resolve that is armed suspends, so a later transition can overtake it.
                    ResolutionGate = null;
                    _ = ResolutionEntered.TrySetResult();
                    await gate.Task.ConfigureAwait(false);
                }

                return answer;
            }

            AuthenticationState state = await _principalSource
                .GetAuthenticationStateAsync()
                .ConfigureAwait(false);
            return TenantsGlobalAdministratorClaims.Evaluate(state.User);
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

        /// <summary>Arms a one-shot suspension of the next grant submission so RequestSent can be observed.</summary>
        public TaskCompletionSource? SubmissionGate { get; set; }

        /// <summary>Arms a one-shot suspension of the next remove submission so RequestSent can be observed.</summary>
        public TaskCompletionSource? RemoveSubmissionGate { get; set; }

        /// <summary>Completes when a gated grant submission has entered and suspended.</summary>
        public TaskCompletionSource SubmissionEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Completes when a gated remove submission has entered and suspended.</summary>
        public TaskCompletionSource RemoveSubmissionEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int SetGlobalAdministratorCalls { get; private set; }

        public int RemoveGlobalAdministratorCalls { get; private set; }

        public List<SetGlobalAdministrator> Requests { get; } = [];

        public List<RemoveGlobalAdministrator> RemoveRequests { get; } = [];

        public Task<TenantCommandSubmissionResult> CreateTenantAsync(
            CreateTenant request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> AddUserToTenantAsync(
            AddUserToTenant request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> ChangeUserRoleAsync(
            ChangeUserRole request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> RemoveUserFromTenantAsync(
            RemoveUserFromTenant request,
            string? messageId = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> UpdateTenantAsync(
            UpdateTenant request,
            string? messageId = null,
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

        public async Task<TenantCommandSubmissionResult> SetGlobalAdministratorAsync(
            SetGlobalAdministrator request,
            CancellationToken cancellationToken = default)
        {
            SetGlobalAdministratorCalls++;
            Requests.Add(request);
            TaskCompletionSource? gate = SubmissionGate;
            if (gate is not null)
            {
                SubmissionGate = null;
                _ = SubmissionEntered.TrySetResult();
                await gate.Task.ConfigureAwait(false);
            }

            return submission ?? TenantCommandSubmissionResult.Failed("No command response configured.");
        }

        public async Task<TenantCommandSubmissionResult> RemoveGlobalAdministratorAsync(
            RemoveGlobalAdministrator request,
            CancellationToken cancellationToken = default)
        {
            RemoveGlobalAdministratorCalls++;
            RemoveRequests.Add(request);
            TaskCompletionSource? gate = RemoveSubmissionGate;
            if (gate is not null)
            {
                RemoveSubmissionGate = null;
                _ = RemoveSubmissionEntered.TrySetResult();
                await gate.Task.ConfigureAwait(false);
            }

            return RemoveSubmission ?? TenantCommandSubmissionResult.Failed("No remove command response configured.");
        }

        public Task<TenantCommandSubmissionResult> EnableTenantTrackedAsync(
            TenantLifecycleCommandRequest request,
            string messageId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandSubmissionResult> DisableTenantTrackedAsync(
            TenantLifecycleCommandRequest request,
            string messageId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<TenantCommandStatusResult> GetStatusAsync(
            TenantCommandTrackingHandle handle,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_statuses.Count == 0
                ? TenantCommandStatusResult.Unknown("No command status configured.")
                : _statuses.Dequeue());
    }

    private sealed class CapturingUntypedLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public List<LogLevel> Levels { get; } = [];

        public List<Exception?> Exceptions { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Levels.Add(logLevel);
            Messages.Add(formatter(state, exception));
            Exceptions.Add(exception);
        }
    }

    private sealed class ThrowingProjectionNotifier : IProjectionChangeNotifierWithTenant
    {
        public event Action<string>? ProjectionChanged
        {
            add { }
            remove { }
        }

        public event Action<string, string>? ProjectionChangedForTenant
        {
            add => throw new InvalidOperationException("unsafe setup detail");
            remove { }
        }

        public void NotifyChanged(string projectionType) { }

        public void NotifyChanged(string projectionType, string tenantId) { }
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
            ["Tenants.ProjectionLifecycle.Label"] = "Projection lifecycle",
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
            ["Tenants.GlobalAdministrators.Grant.Unavailable.ProjectionLifecycle"] = "Current projection lifecycle evidence is required before granting platform authority.",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.InFlight"] = "Another platform authority command is in flight.",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.ReadSurface"] = "The global administrator read projection must be available before grant can be submitted.",
            ["Tenants.GlobalAdministrators.Grant.Unavailable.RemoveDeferred"] = "Remove global administrator is handled by a separate guarded flow.",
            ["Tenants.GlobalAdministrators.Grant.UserId.Help"] = "Enter the literal caller-supplied user id (256 characters or fewer, no control characters). It is not parsed as a tenant member, GUID, or ULID.",
            ["Tenants.GlobalAdministrators.Grant.Confirm.EvidenceRequired"] = "Current projection evidence is required before confirming the global administrator grant.",
            ["Tenants.GlobalAdministrators.Grant.Confirm.DidNotConfirm"] = "Projection re-query did not confirm the target global administrator.",
            ["Tenants.GlobalAdministrators.Grant.Confirm.PageScoped"] = "The projection re-query covers only the first page of global administrators, which does not include this user, so the grant cannot be confirmed from this page. Confirm the outcome from the tenant audit trail.",
            ["Tenants.GlobalAdministrators.Grant.UserId.Label"] = "User id",
            ["Tenants.GlobalAdministrators.Grant.Validation.UserIdInvalid"] = "Enter a supported user id of 256 characters or fewer without control characters.",
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
            ["Tenants.GlobalAdministrators.Remove.Preview.Count.VisiblePage"] = "Administrators visible on this page",
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
            ["Tenants.GlobalAdministrators.Remove.Unavailable.ProjectionLifecycle"] = "Current projection lifecycle evidence is required before removing platform authority.",
            ["Tenants.GlobalAdministrators.Remove.Unavailable.InFlight"] = "Another platform authority command is in flight.",
            ["Tenants.GlobalAdministrators.Remove.Unavailable.Incomplete"] = "Current complete projection evidence is required before removing platform authority.",
            ["Tenants.GlobalAdministrators.Remove.Confirm.PageScoped"] = "The projection re-query covers only the first page of global administrators, which cannot prove this administrator was removed platform-wide. Confirm the outcome from the tenant audit trail.",
            ["Tenants.GlobalAdministrators.Remove.Confirm.EvidenceRequired"] = "Current complete projection evidence is required before confirming global administrator removal.",
            ["Tenants.GlobalAdministrators.Remove.Confirm.StillPresent"] = "Projection re-query still shows the target global administrator. Do not treat removal as complete.",
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
            ["Tenants.GlobalAdministrators.Recovery.IncompletePaging"] = "More administrators are indicated, but no continuation page is available. Reset to the first page and retry the review.",
            ["Tenants.GlobalAdministrators.Recovery.PageRecovered"] = "The protected page could not be restored. The review restarted at the first page.",
            ["Tenants.GlobalAdministrators.Recovery.HistoryTruncated"] = "Paging history reached its limit, so this step went back to the first page instead of the previous one.",
            ["Tenants.GlobalAdministrators.Recovery.AuthorizationChanged"] = "Authorization changed. The review restarted at the first page.",
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
