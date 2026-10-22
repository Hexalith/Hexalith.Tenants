using Bunit;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Components.Pages;
using Hexalith.Tenants.UI.Resources;
using Hexalith.Tenants.UI.Services;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantDetail;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Components;

public sealed partial class GlobalAdministratorsPageTests
{
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    public async Task FiniteAuthorizationBoundReleasesInitializationAndRetryWithoutLateAuthority(
        bool duringInitialization,
        bool ignoreCancellation,
        bool lateFault)
    {
        var clock = RegisterFiniteOperationClock();
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var composition = new StubTenantsBffComposition(duringInitialization
            ? TenantLifecycleAuthorizationReflectionState.Authorized
            : TenantLifecycleAuthorizationReflectionState.Indeterminate)
        {
            ResolutionGate = duringInitialization ? held : null,
            IgnoreResolutionCancellation = ignoreCancellation,
        };
        var gateway = new StubTenantQueryGateway(ComponentReady("current", "admin.recovered"))
        {
            RepeatLastResponse = true,
        };
        RegisterFinitePageServices(composition, gateway);
        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        Task retry = Task.CompletedTask;
        if (!duringInitialization)
        {
            composition.Reflection = TenantLifecycleAuthorizationReflectionState.Authorized;
            composition.ResolutionGate = held;
            retry = cut.Find("[data-testid='tenants-global-admins-authorization-retry']")
                .ClickAsync(new MouseEventArgs());
        }

        await composition.ResolutionEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitUntilAsync(() => clock.PendingTimerCount > 0, TimeSpan.FromSeconds(5));
        composition.LastResolutionCancellation.CanBeCanceled.ShouldBeTrue();
        gateway.GlobalAdministratorCalls.ShouldBe(0);

        clock.Advance(TimeSpan.FromSeconds(1));
        await retry.WaitAsync(TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admins-unavailable']");
            var button = cut.Find("[data-testid='tenants-global-admins-authorization-retry']");
            button.NodeName.ShouldBe("FLUENT-BUTTON");
            button.HasAttribute("disabled").ShouldBeFalse();
            PrivateField<int>(cut.Instance, "_pageLoadInFlight").ShouldBe(0);
            PrivateField<TenantLifecycleAuthorizationReflectionState>(cut.Instance, "_authorizationReflection")
                .ShouldBe(TenantLifecycleAuthorizationReflectionState.Indeterminate);
        });
        composition.LastResolutionCancellation.IsCancellationRequested.ShouldBeTrue();
        gateway.GlobalAdministratorCalls.ShouldBe(0);
        cut.Markup.ShouldNotContain("admin.recovered");
        if (!ignoreCancellation)
        {
            await composition.ResolutionCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            composition.ResolutionCancellationObserved.ShouldBe(1);
        }

        await cut.Find("[data-testid='tenants-global-admins-authorization-retry']")
            .ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() =>
        {
            gateway.GlobalAdministratorCalls.ShouldBe(1);
            cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin.recovered");
        });
        gateway.Requests.ShouldHaveSingleItem().Cursor.ShouldBeNull();

        if (ignoreCancellation && lateFault)
        {
            held.SetException(new InvalidOperationException("obsolete provider detail must remain private"));
        }
        else
        {
            held.SetResult();
        }

        await composition.ResolutionCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cut.InvokeAsync(static () => Task.CompletedTask);
        gateway.GlobalAdministratorCalls.ShouldBe(1);
        cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin.recovered");
        cut.Markup.ShouldNotContain("obsolete provider detail");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ReplacingOrDisposingAuthorizationCancelsHeldRetryAndIgnoresItsCompletion(
        bool dispose,
        bool lateFault)
    {
        RegisterFiniteOperationClock();
        var authentication = new MutableAuthenticationStateProvider(GlobalAdministratorPrincipal());
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Indeterminate)
        {
            IgnoreResolutionCancellation = true,
        };
        var gateway = new StubTenantQueryGateway(ComponentReady("current", "admin.current")) { RepeatLastResponse = true };
        RegisterFinitePageServices(composition, gateway);
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        composition.Reflection = TenantLifecycleAuthorizationReflectionState.Authorized;
        composition.ResolutionGate = held;
        Task retry = cut.Find("[data-testid='tenants-global-admins-authorization-retry']")
            .ClickAsync(new MouseEventArgs());
        await composition.ResolutionEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        if (dispose)
        {
            await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
        }
        else
        {
            composition.Reflection = TenantLifecycleAuthorizationReflectionState.MissingPermission;
            await cut.InvokeAsync(() => authentication.Set(NonAdministratorPrincipal()));
            cut.WaitForAssertion(() =>
            {
                PrivateField<TenantLifecycleAuthorizationReflectionState>(cut.Instance, "_authorizationReflection")
                    .ShouldBe(TenantLifecycleAuthorizationReflectionState.MissingPermission);
                cut.FindAll("[data-testid='tenants-global-admins-authorization-retry']").ShouldBeEmpty();
            });
        }

        await retry.WaitAsync(TimeSpan.FromSeconds(5));
        composition.LastResolutionCancellation.IsCancellationRequested.ShouldBeTrue();
        PrivateField<int>(cut.Instance, "_pageLoadInFlight").ShouldBe(0);
        gateway.GlobalAdministratorCalls.ShouldBe(0);
        if (lateFault)
        {
            held.SetException(new InvalidOperationException("obsolete authorization failure"));
        }
        else
        {
            held.SetResult();
        }

        await composition.ResolutionCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cut.InvokeAsync(static () => Task.CompletedTask);
        gateway.GlobalAdministratorCalls.ShouldBe(0);
        if (dispose)
        {
            int resolvedBeforeNotification = composition.AsyncResolutionCount;
            authentication.Set(GlobalAdministratorPrincipal());
            composition.AsyncResolutionCount.ShouldBe(resolvedBeforeNotification);
        }
        else
        {
            cut.Find("[data-testid='tenants-global-admins-unavailable']");
            cut.Markup.ShouldNotContain("admin.current");
            PrivateField<TenantLifecycleAuthorizationReflectionState>(cut.Instance, "_authorizationReflection")
                .ShouldBe(TenantLifecycleAuthorizationReflectionState.MissingPermission);
            composition.Reflection = TenantLifecycleAuthorizationReflectionState.Authorized;
            authentication.Set(GlobalAdministratorPrincipal());
            cut.WaitForAssertion(() =>
            {
                gateway.GlobalAdministratorCalls.ShouldBe(1);
                cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin.current");
            });
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HeldSubscriptionTimeoutExhaustsThreeAttemptsAndExplicitRefreshRecoversSharedOwnership(bool lateFault)
    {
        var clock = RegisterFiniteOperationClock();
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var setup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var setupEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken setupCancellation = default;
        int setupCalls = 0;
        subscription.SubscribeAsync(GetGlobalAdministratorsQuery.ProjectionType, "system", Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (Interlocked.Increment(ref setupCalls) != 1)
                {
                    return Task.CompletedTask;
                }

                setupCancellation = call.ArgAt<CancellationToken>(2);
                setupEntered.SetResult();
                return setup.Task;
            });
        subscription.UnsubscribeAsync(GetGlobalAdministratorsQuery.ProjectionType, "system", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cleanupEntered.TrySetResult();
                return Task.CompletedTask;
            });
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        var gateway = new StubTenantQueryGateway(ComponentReady("current", "admin.current")) { RepeatLastResponse = true };
        RegisterFinitePageServices(composition, gateway);
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();
        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        await setupEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitUntilAsync(() => clock.PendingTimerCount > 0, TimeSpan.FromSeconds(5));

        clock.Advance(TimeSpan.FromSeconds(1));
        await WaitUntilAsync(() => PrivateField<int>(cut.Instance, "_readRefreshAttempts") >= 1
            && PrivateField<int>(cut.Instance, "_readRefreshSubscriptionInFlight") == 0, TimeSpan.FromSeconds(5));
        setupCancellation.IsCancellationRequested.ShouldBeTrue();
        for (int attempt = 2; attempt <= 3; attempt++)
        {
            cut.Render();
            await WaitUntilAsync(() => clock.PendingTimerCount > 0
                && PrivateField<int>(cut.Instance, "_readRefreshSubscriptionInFlight") == 1, TimeSpan.FromSeconds(5));
            clock.Advance(TimeSpan.FromSeconds(1));
            int expectedAttempts = attempt;
            await WaitUntilAsync(() => PrivateField<int>(cut.Instance, "_readRefreshAttempts") == expectedAttempts
                && PrivateField<int>(cut.Instance, "_readRefreshSubscriptionInFlight") == 0, TimeSpan.FromSeconds(5));
        }
        for (int render = 0; render < 10; render++)
        {
            cut.Render();
        }

        PrivateField<int>(cut.Instance, "_readRefreshAttempts").ShouldBe(3);
        PrivateField<IAsyncDisposable?>(cut.Instance, "_readRefreshLease").ShouldBeNull();
        Volatile.Read(ref setupCalls).ShouldBe(1, "A quarantined scope cannot acquire a replacement removed by late cleanup.");
        cut.Find("[data-testid='tenants-global-admins-projection-lifecycle']").TextContent.Trim().ShouldBe("Current");
        cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin.current");
        PrivateField<GlobalAdministratorsSnapshot>(cut.Instance, "_snapshot").Freshness.ShouldBe(ReadModelFreshnessState.Current);
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(GetGlobalAdministratorsQuery.ProjectionType, "system");
        await cut.InvokeAsync(static () => Task.CompletedTask);
        gateway.GlobalAdministratorCalls.ShouldBe(1, "Expired setup must detach its callback without inventing refreshed evidence.");

        if (lateFault)
        {
            setup.SetException(new InvalidOperationException("late setup detail"));
        }
        else
        {
            setup.SetResult();
        }

        await cleanupEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitUntilAsync(() => clock.PendingTimerCount == 0, TimeSpan.FromSeconds(5));
        TenantReadRefreshSubscription coordinator = Services.GetRequiredService<TenantReadRefreshSubscription>();
        TenantReadRefreshLease sharedLease = await coordinator.SubscribeAsync(
            GetGlobalAdministratorsQuery.ProjectionType, "system", static () => Task.CompletedTask);
        sharedLease.IsSubscribed.ShouldBeTrue();

        await cut.Find("[data-testid='tenants-global-admins-refresh']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() =>
        {
            PrivateField<IAsyncDisposable?>(cut.Instance, "_readRefreshLease").ShouldNotBeNull();
            PrivateField<int>(cut.Instance, "_readRefreshAttempts").ShouldBe(0);
            gateway.GlobalAdministratorCalls.ShouldBe(2);
        });
        Volatile.Read(ref setupCalls).ShouldBe(2);
        await sharedLease.DisposeAsync();
        await subscription.Received(1).UnsubscribeAsync(GetGlobalAdministratorsQuery.ProjectionType, "system", Arg.Any<CancellationToken>());

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(GetGlobalAdministratorsQuery.ProjectionType, "other-tenant");
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("other-projection", "system");
        await cut.InvokeAsync(static () => Task.CompletedTask);
        gateway.GlobalAdministratorCalls.ShouldBe(2);
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(GetGlobalAdministratorsQuery.ProjectionType, "system");
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(3));
        gateway.Requests.ShouldAllBe(request => request.Cursor == null);
        await subscription.Received(2).SubscribeAsync(GetGlobalAdministratorsQuery.ProjectionType, "system", Arg.Any<CancellationToken>());
        cut.Markup.ShouldNotContain("late setup detail");

        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
        await subscription.Received(2).UnsubscribeAsync(GetGlobalAdministratorsQuery.ProjectionType, "system", Arg.Any<CancellationToken>());
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(GetGlobalAdministratorsQuery.ProjectionType, "system");
        gateway.GlobalAdministratorCalls.ShouldBe(3);
    }

    [Fact]
    public async Task OrdinaryRefreshAndProjectionNotificationShareCallerCancellationWithoutCancelingSiblingAuthority()
    {
        var clock = RegisterFiniteOperationClock();
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        var gateway = new StubTenantQueryGateway(
            ComponentReady("initial", "admin.initial"),
            ComponentReady("manual", "admin.manual"),
            ComponentReady("notification", "admin.notification"));
        RegisterFinitePageServices(composition, gateway);
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();
        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        cut.WaitForAssertion(() =>
        {
            PrivateField<IAsyncDisposable?>(cut.Instance, "_readRefreshLease").ShouldNotBeNull();
            gateway.GlobalAdministratorCalls.ShouldBe(1);
        });
        long callerVersion = PrivateField<long>(cut.Instance, "_authorizationTransitionVersion");
        int initialResolutions = composition.AsyncResolutionCount;
        var ordinaryAuthority = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var notificationAuthority = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        composition.ResolutionGate = ordinaryAuthority;
        Task refresh = cut.Find("[data-testid='tenants-global-admins-refresh']").ClickAsync(new MouseEventArgs());
        await composition.ResolutionEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        CancellationToken ordinaryToken = composition.LastResolutionCancellation;
        PrivateField<int>(cut.Instance, "_pageLoadInFlight").ShouldBe(1);
        composition.ResolutionGate = notificationAuthority;

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(GetGlobalAdministratorsQuery.ProjectionType, "system");
        await WaitUntilAsync(() => composition.AsyncResolutionCount == initialResolutions + 2
            && composition.LastResolutionCancellation != ordinaryToken, TimeSpan.FromSeconds(5));
        CancellationToken notificationToken = composition.LastResolutionCancellation;
        ordinaryToken.CanBeCanceled.ShouldBeTrue();
        notificationToken.CanBeCanceled.ShouldBeTrue();
        ordinaryToken.IsCancellationRequested.ShouldBeFalse("A notification cannot revoke a sibling authority check for the same caller.");
        notificationToken.IsCancellationRequested.ShouldBeFalse();
        PrivateField<bool>(cut.Instance, "_isNotificationRefreshing").ShouldBeTrue();
        gateway.GlobalAdministratorCalls.ShouldBe(1, "Both reads must await their own authority proof.");
        PrivateField<TenantLifecycleAuthorizationReflectionState>(cut.Instance, "_authorizationReflection")
            .ShouldBe(TenantLifecycleAuthorizationReflectionState.Authorized);

        ordinaryAuthority.SetResult();
        await refresh.WaitAsync(TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() =>
        {
            gateway.GlobalAdministratorCalls.ShouldBe(2);
            cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin.manual");
        });
        PrivateField<int>(cut.Instance, "_pageLoadInFlight").ShouldBe(0);
        notificationToken.IsCancellationRequested.ShouldBeFalse("Completing an ordinary read cannot cancel pending notification authority.");
        PrivateField<bool>(cut.Instance, "_isNotificationRefreshing").ShouldBeTrue();

        notificationAuthority.SetResult();
        cut.WaitForAssertion(() =>
        {
            gateway.GlobalAdministratorCalls.ShouldBe(3);
            cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin.notification");
            PrivateField<bool>(cut.Instance, "_isNotificationRefreshing").ShouldBeFalse();
            PrivateField<int>(cut.Instance, "_pageLoadInFlight").ShouldBe(0);
            PrivateField<TenantLifecycleAuthorizationReflectionState>(cut.Instance, "_authorizationReflection")
                .ShouldBe(TenantLifecycleAuthorizationReflectionState.Authorized);
        });
        composition.ResolutionCancellationObserved.ShouldBe(0);
        PrivateField<long>(cut.Instance, "_authorizationTransitionVersion").ShouldBe(callerVersion);
        PrivateField<int>(cut.Instance, "_readRefreshAttempts").ShouldBe(0);
        gateway.Requests.ShouldAllBe(request => request.Cursor == null);
        // Visible renderer state precedes retirement of the bounded notification callback.
        cut.WaitForAssertion(() => clock.PendingTimerCount.ShouldBe(0));
        await subscription.Received(1).SubscribeAsync(GetGlobalAdministratorsQuery.ProjectionType, "system", Arg.Any<CancellationToken>());
        await subscription.DidNotReceive().UnsubscribeAsync(GetGlobalAdministratorsQuery.ProjectionType, "system", Arg.Any<CancellationToken>());

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(GetGlobalAdministratorsQuery.ProjectionType, "system");
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(4));
        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
        await subscription.Received(1).UnsubscribeAsync(GetGlobalAdministratorsQuery.ProjectionType, "system", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HeldAuthenticationEventExpiresClosedRecoversExplicitlyAndCancelsOnDisposal(bool lateFault)
    {
        var clock = RegisterFiniteOperationClock();
        var authentication = new MutableAuthenticationStateProvider(GlobalAdministratorPrincipal());
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        var gateway = new StubTenantQueryGateway(ComponentReady("current", "admin.current"));
        RegisterFinitePageServices(composition, gateway);
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        gateway.GlobalAdministratorCalls.ShouldBe(1);

        TaskCompletionSource<AuthenticationState> expired = authentication.NotifyPending();
        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='tenants-global-admins-unavailable']");
            cut.Markup.ShouldNotContain("admin.current");
        });
        await WaitUntilAsync(() => clock.PendingTimerCount > 0, TimeSpan.FromSeconds(5));
        CancellationToken expiredToken = PrivateField<CancellationTokenSource>(cut.Instance, "_authenticationStateCancellation").Token;
        clock.Advance(TimeSpan.FromSeconds(1));
        await WaitUntilAsync(() => expiredToken.IsCancellationRequested
            || typeof(GlobalAdministratorsPage).GetField("_authenticationStateCancellation",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(cut.Instance) is null,
            TimeSpan.FromSeconds(5));
        cut.Find("[data-testid='tenants-global-admins-authorization-retry']").HasAttribute("disabled").ShouldBeFalse();
        PrivateField<TenantLifecycleAuthorizationReflectionState>(cut.Instance, "_authorizationReflection")
            .ShouldBe(TenantLifecycleAuthorizationReflectionState.Indeterminate);
        gateway.GlobalAdministratorCalls.ShouldBe(1);

        await cut.Find("[data-testid='tenants-global-admins-authorization-retry']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() =>
        {
            gateway.GlobalAdministratorCalls.ShouldBe(2);
            cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin.current");
        });
        int currentResolutions = composition.AsyncResolutionCount;
        if (lateFault)
        {
            expired.SetException(new InvalidOperationException("expired event detail"));
        }
        else
        {
            expired.SetResult(new AuthenticationState(NonAdministratorPrincipal()));
        }
        await cut.InvokeAsync(static () => Task.CompletedTask);
        composition.AsyncResolutionCount.ShouldBe(currentResolutions);
        gateway.GlobalAdministratorCalls.ShouldBe(2);
        cut.Find("[data-testid='tenants-global-admins-user-id']").TextContent.ShouldBe("admin.current");

        TaskCompletionSource<AuthenticationState> disposed = authentication.NotifyPending();
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-global-admins-unavailable']"));
        await WaitUntilAsync(() => clock.PendingTimerCount > 0, TimeSpan.FromSeconds(5));
        CancellationToken disposedToken = PrivateField<CancellationTokenSource>(cut.Instance, "_authenticationStateCancellation").Token;
        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
        disposedToken.IsCancellationRequested.ShouldBeTrue("Disposal must cancel an active event before its configured deadline.");
        long disposedVersion = PrivateField<long>(cut.Instance, "_authorizationTransitionVersion");
        currentResolutions = composition.AsyncResolutionCount;
        if (lateFault)
        {
            disposed.SetException(new InvalidOperationException("disposed event detail"));
        }
        else
        {
            disposed.SetResult(new AuthenticationState(GlobalAdministratorPrincipal()));
        }
        await WaitUntilAsync(() => clock.PendingTimerCount == 0, TimeSpan.FromSeconds(5));
        await cut.InvokeAsync(static () => Task.CompletedTask);
        PrivateField<long>(cut.Instance, "_authorizationTransitionVersion").ShouldBe(disposedVersion);
        composition.AsyncResolutionCount.ShouldBe(currentResolutions);
        gateway.GlobalAdministratorCalls.ShouldBe(2);
        PrivateField<int>(cut.Instance, "_pageLoadInFlight").ShouldBe(0);
        clock.GetUtcNow().ShouldBe(DateTimeOffset.UnixEpoch.AddSeconds(1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthorityTransitionDetachesHeldSetupAndHandsOffAfterLateCompletionWithoutChargingReplacement(bool lateFault)
    {
        var clock = RegisterFiniteOperationClock();
        var authentication = new MutableAuthenticationStateProvider(GlobalAdministratorPrincipal());
        var composition = new StubTenantsBffComposition(TenantLifecycleAuthorizationReflectionState.Authorized);
        var gateway = new StubTenantQueryGateway(ComponentReady("current", "admin.current"));
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken setupToken = default;
        int setupCalls = 0;
        int otherRefreshes = 0;
        subscription.SubscribeAsync(GetGlobalAdministratorsQuery.ProjectionType, "system", Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                if (Interlocked.Increment(ref setupCalls) != 1)
                {
                    return Task.CompletedTask;
                }
                setupToken = call.ArgAt<CancellationToken>(2);
                entered.SetResult();
                return held.Task;
            });
        RegisterFinitePageServices(composition, gateway);
        Services.AddSingleton<AuthenticationStateProvider>(authentication);
        Services.AddSingleton(subscription);
        Services.AddSingleton(notifier);
        Services.AddScoped<TenantReadRefreshSubscription>();
        TenantReadRefreshSubscription coordinator = Services.GetRequiredService<TenantReadRefreshSubscription>();
        TenantReadRefreshLease other = await coordinator.SubscribeAsync("other-projection", "tenant.other", () =>
        {
            Interlocked.Increment(ref otherRefreshes);
            return Task.CompletedTask;
        });
        other.IsSubscribed.ShouldBeTrue();
        IRenderedComponent<GlobalAdministratorsPage> cut = Render<GlobalAdministratorsPage>();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        composition.Reflection = TenantLifecycleAuthorizationReflectionState.MissingPermission;
        authentication.Set(NonAdministratorPrincipal());
        cut.WaitForAssertion(() => cut.Find("[data-testid='tenants-global-admins-unavailable']"));
        // Setup gate completion does not itself render. Observe it within the same default wait
        // instead of requiring an unrelated render to re-run a DOM wait assertion.
        await WaitUntilAsync(() => PrivateField<int>(cut.Instance, "_readRefreshSubscriptionInFlight") == 0,
            BunitContext.DefaultWaitTimeout);
        PrivateField<int>(cut.Instance, "_readRefreshAttempts").ShouldBe(0);
        setupToken.IsCancellationRequested.ShouldBeTrue();
        PrivateField<IAsyncDisposable?>(cut.Instance, "_readRefreshLease").ShouldBeNull();
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(GetGlobalAdministratorsQuery.ProjectionType, "system");
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("other-projection", "tenant.other");
        await WaitUntilAsync(() => Volatile.Read(ref otherRefreshes) == 1, TimeSpan.FromSeconds(5));
        gateway.GlobalAdministratorCalls.ShouldBe(1);

        composition.Reflection = TenantLifecycleAuthorizationReflectionState.Authorized;
        authentication.Set(GlobalAdministratorPrincipal());
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(2));
        await WaitUntilAsync(() => PrivateField<int>(cut.Instance, "_readRefreshSubscriptionInFlight") == 1,
            BunitContext.DefaultWaitTimeout);
        PrivateField<int>(cut.Instance, "_readRefreshAttempts").ShouldBe(0);
        Volatile.Read(ref setupCalls).ShouldBe(1, "Replacement ownership must await obsolete scope cleanup.");
        if (lateFault)
        {
            held.SetException(new InvalidOperationException("obsolete subscription setup detail"));
        }
        else
        {
            held.SetResult();
        }
        await WaitUntilAsync(() => Volatile.Read(ref setupCalls) == 2
            && PrivateField<int>(cut.Instance, "_readRefreshSubscriptionInFlight") == 0, TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() =>
        {
            PrivateField<IAsyncDisposable?>(cut.Instance, "_readRefreshLease").ShouldNotBeNull();
            PrivateField<int>(cut.Instance, "_readRefreshAttempts").ShouldBe(0);
        });
        await subscription.Received(1).UnsubscribeAsync(GetGlobalAdministratorsQuery.ProjectionType, "system", Arg.Any<CancellationToken>());
        await subscription.DidNotReceive().UnsubscribeAsync("other-projection", "tenant.other", Arg.Any<CancellationToken>());
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(GetGlobalAdministratorsQuery.ProjectionType, "system");
        cut.WaitForAssertion(() => gateway.GlobalAdministratorCalls.ShouldBe(3));
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("other-projection", "tenant.other");
        await WaitUntilAsync(() => Volatile.Read(ref otherRefreshes) == 2, TimeSpan.FromSeconds(5));
        gateway.GlobalAdministratorCalls.ShouldBe(3);
        clock.GetUtcNow().ShouldBe(DateTimeOffset.UnixEpoch, "Caller invalidation must hand off without waiting for the deadline.");
        await cut.InvokeAsync(async () => await cut.Instance.DisposeAsync());
        await subscription.Received(2).UnsubscribeAsync(GetGlobalAdministratorsQuery.ProjectionType, "system", Arg.Any<CancellationToken>());
        await subscription.DidNotReceive().UnsubscribeAsync("other-projection", "tenant.other", Arg.Any<CancellationToken>());
        await other.DisposeAsync();
        await subscription.Received(1).UnsubscribeAsync("other-projection", "tenant.other", Arg.Any<CancellationToken>());
    }

    private ManualTimeProvider RegisterFiniteOperationClock()
    {
        var clock = new ManualTimeProvider();
        Services.AddSingleton<TimeProvider>(clock);
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Tenants:Ui:AuthorizationAndSubscriptionTimeout"] = "00:00:01",
            }).Build());
        return clock;
    }

    private void RegisterFinitePageServices(StubTenantsBffComposition composition, StubTenantQueryGateway gateway)
    {
        Services.AddSingleton<ITenantsBffComposition>(composition);
        Services.AddSingleton<ITenantQueryGateway>(gateway);
        Services.AddSingleton<ITenantCommandGateway>(new StubTenantCommandGateway());
        Services.AddSingleton<IStringLocalizer<TenantsResources>>(new StubTenantsLocalizer());
    }
}
