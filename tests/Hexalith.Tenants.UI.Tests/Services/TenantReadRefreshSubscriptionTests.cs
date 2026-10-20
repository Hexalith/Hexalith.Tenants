using System.Diagnostics;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.Tenants.UI.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services;

public sealed class TenantReadRefreshSubscriptionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Timed_out_setup_keeps_only_its_scope_quarantined_until_late_setup_and_cleanup_finish(bool lateFailure)
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var clock = new ManualTimeProvider();
        TaskCompletionSource setup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource cleanup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource cleanupEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken oldToken = default;
        int setups = 0;
        int cleanups = 0;
        subscription.SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (Interlocked.Increment(ref setups) == 1)
            {
                oldToken = call.Arg<CancellationToken>();
                return setup.Task;
            }

            return Task.CompletedTask;
        });
        subscription.UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (Interlocked.Increment(ref cleanups) == 1)
            {
                cleanupEntered.SetResult();
                return cleanup.Task;
            }

            return Task.CompletedTask;
        });
        await using ServiceProvider services = BoundedServices(subscription, notifier, clock);
        await using TenantReadRefreshSubscription sut = new(services);
        int obsoleteCallbacks = 0;
        int betaCallbacks = 0;
        TaskCompletionSource betaCalled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using TenantReadRefreshLease beta = await sut.SubscribeAsync("tenants", "tenant.beta", () =>
        {
            betaCallbacks++;
            betaCalled.TrySetResult();
            return Task.CompletedTask;
        });
        Task<TenantReadRefreshLease> pending = sut.SubscribeAsync("tenants", "tenant.alpha", () =>
        {
            obsoleteCallbacks++;
            return Task.CompletedTask;
        });
        setups.ShouldBe(1);

        clock.Advance(TimeSpan.FromSeconds(1));
        (await pending.WaitAsync(TimeSpan.FromSeconds(2))).IsSubscribed.ShouldBeFalse();
        oldToken.IsCancellationRequested.ShouldBeTrue();
        using CancellationTokenRegistration lateRegistration = oldToken.Register(() => { });
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.beta");
        await betaCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        betaCallbacks.ShouldBe(1);
        obsoleteCallbacks.ShouldBe(0);

        TaskCompletionSource replacementCalled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<TenantReadRefreshLease> replacement = sut.SubscribeAsync("tenants", "tenant.alpha", () =>
        {
            replacementCalled.TrySetResult();
            return Task.CompletedTask;
        });
        setups.ShouldBe(1);
        replacement.IsCompleted.ShouldBeFalse();
        if (lateFailure)
        {
            setup.SetException(new InvalidOperationException("unsafe obsolete setup"));
        }
        else
        {
            setup.SetResult();
        }

        await cleanupEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        setups.ShouldBe(1);
        replacement.IsCompleted.ShouldBeFalse();
        cleanup.SetResult();
        TenantReadRefreshLease current = await replacement.WaitAsync(TimeSpan.FromSeconds(2));
        current.IsSubscribed.ShouldBeTrue();
        setups.ShouldBe(2);
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        await replacementCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        obsoleteCallbacks.ShouldBe(0);
        cleanups.ShouldBe(1);
        await current.DisposeAsync();
        cleanups.ShouldBe(2);
        await subscription.DidNotReceive().UnsubscribeAsync("tenants", "tenant.beta", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Same_scope_wait_uses_the_original_deadline_and_cannot_duplicate_a_stalled_setup()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var clock = new ManualTimeProvider();
        TaskCompletionSource setup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        subscription.SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(setup.Task);
        await using ServiceProvider services = BoundedServices(subscription, notifier, clock);
        await using TenantReadRefreshSubscription sut = new(services);
        Task<TenantReadRefreshLease> first = sut.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask);
        clock.Advance(TimeSpan.FromMilliseconds(500));
        Task<TenantReadRefreshLease> second = sut.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask);

        clock.Advance(TimeSpan.FromMilliseconds(500));
        (await first.WaitAsync(TimeSpan.FromSeconds(2))).IsSubscribed.ShouldBeFalse();
        second.IsCompleted.ShouldBeFalse();
        clock.Advance(TimeSpan.FromMilliseconds(500));
        (await second.WaitAsync(TimeSpan.FromSeconds(2))).IsSubscribed.ShouldBeFalse();
        await subscription.Received(1).SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
        setup.SetResult();
    }

    [Fact]
    public async Task Concurrent_same_scope_consumers_join_one_completed_setup_and_keep_independent_leases()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var clock = new ManualTimeProvider();
        TaskCompletionSource setup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        subscription.SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(setup.Task);
        await using ServiceProvider services = BoundedServices(subscription, notifier, clock);
        await using TenantReadRefreshSubscription sut = new(services);
        TaskCompletionSource secondCalled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int firstCalls = 0;
        Task<TenantReadRefreshLease> first = sut.SubscribeAsync("tenants", "tenant.alpha", () =>
        {
            firstCalls++;
            return Task.CompletedTask;
        });
        Task<TenantReadRefreshLease> second = sut.SubscribeAsync("tenants", "tenant.alpha", () =>
        {
            secondCalled.TrySetResult();
            return Task.CompletedTask;
        });
        setup.SetResult();
        TenantReadRefreshLease[] leases = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(2));
        leases.ShouldAllBe(lease => lease.IsSubscribed);
        await subscription.Received(1).SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
        await leases[0].DisposeAsync();
        await subscription.DidNotReceive().UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        await secondCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        firstCalls.ShouldBe(0);
        await leases[1].DisposeAsync();
        await subscription.Received(1).UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stalled_last_reference_cleanup_is_bounded_and_cannot_detach_a_later_replacement(bool lateFailure)
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var clock = new ManualTimeProvider();
        TaskCompletionSource cleanup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int cleanups = 0;
        subscription.UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(_ =>
            Interlocked.Increment(ref cleanups) == 1 ? cleanup.Task : Task.CompletedTask);
        await using ServiceProvider services = BoundedServices(subscription, notifier, clock);
        await using TenantReadRefreshSubscription sut = new(services);
        TenantReadRefreshLease first = await sut.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask);
        Task release = first.DisposeAsync().AsTask();
        Task<TenantReadRefreshLease> blocked = sut.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask);
        await using TenantReadRefreshLease other = await sut.SubscribeAsync("tenants", "tenant.beta", () => Task.CompletedTask);
        other.IsSubscribed.ShouldBeTrue();

        clock.Advance(TimeSpan.FromSeconds(1));
        await release.WaitAsync(TimeSpan.FromSeconds(2));
        (await blocked.WaitAsync(TimeSpan.FromSeconds(2))).IsSubscribed.ShouldBeFalse();
        await subscription.Received(1).SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
        Task<TenantReadRefreshLease> replacement = sut.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask);
        if (lateFailure)
        {
            cleanup.SetException(new InvalidOperationException("late obsolete cleanup"));
        }
        else
        {
            cleanup.SetResult();
        }
        TenantReadRefreshLease current = await replacement.WaitAsync(TimeSpan.FromSeconds(2));
        current.IsSubscribed.ShouldBeTrue();
        cleanups.ShouldBe(1);
        await current.DisposeAsync();
        cleanups.ShouldBe(2);
    }

    [Fact]
    public async Task Circuit_disposal_cancels_held_setup_detaches_all_callbacks_and_cleans_late_success_once()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var clock = new ManualTimeProvider();
        TaskCompletionSource setup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource lateCleanup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        subscription.SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(setup.Task);
        subscription.UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            lateCleanup.TrySetResult();
            return Task.CompletedTask;
        });
        await using ServiceProvider services = BoundedServices(subscription, notifier, clock);
        TenantReadRefreshSubscription sut = new(services);
        int callbacks = 0;
        Task Callback()
        {
            callbacks++;
            return Task.CompletedTask;
        }

        TenantReadRefreshLease beta = await sut.SubscribeAsync("tenants", "tenant.beta", Callback);
        TenantReadRefreshLease betaSecond = await sut.SubscribeAsync("tenants", "tenant.beta", Callback);
        Task<TenantReadRefreshLease> pending = sut.SubscribeAsync("tenants", "tenant.alpha", Callback);
        await sut.DisposeAsync();
        _ = await Should.ThrowAsync<OperationCanceledException>(pending.WaitAsync(TimeSpan.FromSeconds(2)));
        _ = await Should.ThrowAsync<ObjectDisposedException>(() => sut.SubscribeAsync("tenants", "tenant.gamma", Callback));
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.beta");
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        callbacks.ShouldBe(0);
        await beta.DisposeAsync();
        await betaSecond.DisposeAsync();
        await subscription.Received(1).UnsubscribeAsync("tenants", "tenant.beta", Arg.Any<CancellationToken>());

        setup.SetResult();
        await lateCleanup.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await subscription.Received(1).UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
        await subscription.DidNotReceive().SubscribeAsync("tenants", "tenant.gamma", Arg.Any<CancellationToken>());
        callbacks.ShouldBe(0);
    }

    [Fact]
    public async Task Cooperative_setup_cancellation_releases_ownership_before_retry()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var clock = new ManualTimeProvider();
        TaskCompletionSource setup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int attempts = 0;
        subscription.SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (++attempts == 1)
            {
                CancellationToken token = call.Arg<CancellationToken>();
                _ = token.Register(() => setup.TrySetCanceled(token));
                return setup.Task;
            }

            return Task.CompletedTask;
        });
        await using ServiceProvider services = BoundedServices(subscription, notifier, clock);
        await using TenantReadRefreshSubscription sut = new(services);
        using var caller = new CancellationTokenSource();
        Task<TenantReadRefreshLease> pending = sut.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask, caller.Token);

        caller.Cancel();
        _ = await Should.ThrowAsync<OperationCanceledException>(pending.WaitAsync(TimeSpan.FromSeconds(2)));
        await using TenantReadRefreshLease recovered = await sut.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask);
        recovered.IsSubscribed.ShouldBeTrue();
        attempts.ShouldBe(2);
        await subscription.Received(1).UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failed_optional_notifier_registration_does_not_leave_a_scope_owner()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var clock = new ManualTimeProvider();
        int registrations = 0;
        notifier.When(value => value.ProjectionChangedForTenant += Arg.Any<Action<string, string>>())
            .Do(_ =>
            {
                if (++registrations == 1)
                {
                    throw new InvalidOperationException("optional notifier unavailable");
                }
            });
        await using ServiceProvider services = BoundedServices(subscription, notifier, clock);
        await using TenantReadRefreshSubscription sut = new(services);

        (await sut.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask)).IsSubscribed.ShouldBeFalse();
        await using TenantReadRefreshLease recovered = await sut.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask);
        recovered.IsSubscribed.ShouldBeTrue();
        registrations.ShouldBe(2);
        await subscription.Received(1).SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Retired_cleanup_contains_a_disposed_logger_and_releases_its_scope_after_actual_completion()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var clock = new ManualTimeProvider();
        TaskCompletionSource cleanup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int cleanups = 0;
        subscription.UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(_ =>
            ++cleanups == 1 ? cleanup.Task : Task.CompletedTask);
        await using ServiceProvider services = BoundedServices(subscription, notifier, clock);
        await using TenantReadRefreshSubscription sut = new(services, new ThrowingLogger());
        TenantReadRefreshLease first = await sut.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask);
        Task release = first.DisposeAsync().AsTask();

        clock.Advance(TimeSpan.FromSeconds(1));
        await release.WaitAsync(TimeSpan.FromSeconds(2));
        Task<TenantReadRefreshLease> pending = sut.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask);
        pending.IsCompleted.ShouldBeFalse();
        cleanup.SetResult();
        await using TenantReadRefreshLease recovered = await pending.WaitAsync(TimeSpan.FromSeconds(2));
        recovered.IsSubscribed.ShouldBeTrue();
        cleanups.ShouldBe(1);
    }

    [Fact]
    public async Task Blocked_backend_cancellation_callbacks_cannot_hold_setup_or_detach_a_replacement()
    {
        IProjectionSubscription backend = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var clock = new ManualTimeProvider();
        using var releaseCallback = new ManualResetEventSlim();
        TaskCompletionSource setup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource callbackEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource cleanupEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource replacementCalled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int setups = 0;
        int obsoleteCallbacks = 0;
        backend.SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (++setups == 1)
            {
                _ = call.Arg<CancellationToken>().Register(() =>
                {
                    callbackEntered.TrySetResult();
                    releaseCallback.Wait();
                });
                return setup.Task;
            }

            return Task.CompletedTask;
        });
        backend.UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cleanupEntered.TrySetResult();
            return Task.CompletedTask;
        });
        await using ServiceProvider services = BoundedServices(backend, notifier, clock);
        await using TenantReadRefreshSubscription coordinator = new(services);
        Task<TenantReadRefreshLease> first = coordinator.SubscribeAsync("tenants", "tenant.alpha", () =>
        {
            obsoleteCallbacks++;
            return Task.CompletedTask;
        });
        Task advance = Task.Run(() => clock.Advance(TimeSpan.FromSeconds(1)));
        try
        {
            await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
            (await first.WaitAsync(TimeSpan.FromSeconds(2))).IsSubscribed.ShouldBeFalse();
            await using TenantReadRefreshLease beta = await coordinator.SubscribeAsync("tenants", "tenant.beta", () => Task.CompletedTask);
            beta.IsSubscribed.ShouldBeTrue();
            setup.SetResult();
            Task<TenantReadRefreshLease> pending = coordinator.SubscribeAsync("tenants", "tenant.alpha", () =>
            {
                replacementCalled.TrySetResult();
                return Task.CompletedTask;
            });
            setups.ShouldBe(1);
            pending.IsCompleted.ShouldBeFalse();
            releaseCallback.Set();
            await cleanupEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await using TenantReadRefreshLease current = await pending.WaitAsync(TimeSpan.FromSeconds(2));
            current.IsSubscribed.ShouldBeTrue();
            setups.ShouldBe(2);
            notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
            await replacementCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));
            obsoleteCallbacks.ShouldBe(0);
        }
        finally
        {
            releaseCallback.Set();
            setup.TrySetResult();
            await advance.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    [Fact]
    public async Task A_canceled_same_scope_waiter_cannot_replace_or_cancel_the_held_owner()
    {
        IProjectionSubscription backend = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var clock = new ManualTimeProvider();
        TaskCompletionSource setup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken ownerToken = default;
        backend.SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(call =>
        {
            ownerToken = call.Arg<CancellationToken>();
            return setup.Task;
        });
        await using ServiceProvider services = BoundedServices(backend, notifier, clock);
        await using TenantReadRefreshSubscription coordinator = new(services);
        Task<TenantReadRefreshLease> owner = coordinator.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask);
        using var caller = new CancellationTokenSource();
        Task<TenantReadRefreshLease> waiter = coordinator.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask, caller.Token);
        caller.Cancel();

        _ = await Should.ThrowAsync<OperationCanceledException>(waiter.WaitAsync(TimeSpan.FromSeconds(2)));
        owner.IsCompleted.ShouldBeFalse();
        ownerToken.IsCancellationRequested.ShouldBeFalse();
        await backend.Received(1).SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
        setup.SetResult();
        await using TenantReadRefreshLease current = await owner.WaitAsync(TimeSpan.FromSeconds(2));
        current.IsSubscribed.ShouldBeTrue();
        await backend.DidNotReceive().UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Coordinator_disposal_detaches_callbacks_before_its_cleanup_deadline_even_when_cleanup_ignores_cancellation()
    {
        IProjectionSubscription backend = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var clock = new ManualTimeProvider();
        TaskCompletionSource cleanup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        backend.UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(cleanup.Task);
        await using ServiceProvider services = BoundedServices(backend, notifier, clock);
        TenantReadRefreshSubscription coordinator = new(services);
        int callbacks = 0;
        TenantReadRefreshLease lease = await coordinator.SubscribeAsync("tenants", "tenant.alpha", () =>
        {
            callbacks++;
            return Task.CompletedTask;
        });
        Task disposing = coordinator.DisposeAsync().AsTask();
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        callbacks.ShouldBe(0);
        disposing.IsCompleted.ShouldBeFalse();
        clock.Advance(TimeSpan.FromSeconds(1));
        await disposing.WaitAsync(TimeSpan.FromSeconds(2));
        _ = await Should.ThrowAsync<ObjectDisposedException>(() => coordinator.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask));
        await lease.DisposeAsync();
        cleanup.SetResult();
        await backend.Received(1).UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
        callbacks.ShouldBe(0);
    }

    private static ServiceProvider BoundedServices(
        IProjectionSubscription subscription,
        IProjectionChangeNotifierWithTenant notifier,
        TimeProvider clock)
        => new ServiceCollection()
            .AddSingleton(subscription)
            .AddSingleton(notifier)
            .AddSingleton<TimeProvider>(clock)
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { [TenantUiOperationPolicy.ConfigurationKey] = "00:00:01" }).Build())
            .BuildServiceProvider();

    [Fact]
    public async Task Matching_nudges_are_coalesced_and_cleanup_unsubscribes()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(subscription)
            .AddSingleton(notifier)
            .BuildServiceProvider();
        TenantReadRefreshSubscription sut = new(services);
        TaskCompletionSource firstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int refreshCount = 0;

        TenantReadRefreshLease lease = await sut.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            async () =>
            {
                int count = Interlocked.Increment(ref refreshCount);
                if (count == 1)
                {
                    firstEntered.SetResult();
                    await releaseFirst.Task;
                }
                else
                {
                    secondCompleted.SetResult();
                }
            });

        lease.IsSubscribed.ShouldBeTrue();

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        releaseFirst.SetResult();
        await secondCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        refreshCount.ShouldBe(2);
        await subscription.Received(1).SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
        await lease.DisposeAsync();
        await subscription.Received(1).UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        await Task.Yield();
        refreshCount.ShouldBe(2);
    }

    [Fact]
    public async Task Mismatched_and_missing_notification_services_do_not_refresh_or_block_reads()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(subscription)
            .AddSingleton(notifier)
            .BuildServiceProvider();
        TenantReadRefreshSubscription sut = new(services);
        int refreshCount = 0;
        await using IAsyncDisposable lease = await sut.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () =>
            {
                refreshCount++;
                return Task.CompletedTask;
            });

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "system");
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.beta");
        await Task.Yield();
        refreshCount.ShouldBe(0);

        TenantReadRefreshSubscription unavailable = new(new ServiceCollection().BuildServiceProvider());
        await using TenantReadRefreshLease noOp = await unavailable.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () => throw new InvalidOperationException("must not run"));
        noOp.IsSubscribed.ShouldBeFalse();
    }

    [Fact]
    public async Task Identical_subscribers_share_one_backend_subscription_and_release_only_their_own_callback()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(subscription)
            .AddSingleton(notifier)
            .BuildServiceProvider();
        TenantReadRefreshSubscription sut = new(services);
        int firstCount = 0;
        int secondCount = 0;
        TaskCompletionSource secondCalled = new(TaskCreationOptions.RunContinuationsAsynchronously);

        IAsyncDisposable first = await sut.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () =>
            {
                firstCount++;
                return Task.CompletedTask;
            });
        IAsyncDisposable second = await sut.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () =>
            {
                secondCount++;
                secondCalled.TrySetResult();
                return Task.CompletedTask;
            });

        await subscription.Received(1).SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
        await first.DisposeAsync();
        await subscription.DidNotReceive().UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        await secondCalled.Task.WaitAsync(TimeSpan.FromSeconds(2));
        firstCount.ShouldBe(0);
        secondCount.ShouldBe(1);

        await second.DisposeAsync();
        await subscription.Received(1).UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancellation_after_late_backend_setup_cleans_up_without_retaining_a_callback()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        TaskCompletionSource setupCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        subscription.SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>())
            .Returns(setupCompleted.Task);
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(subscription)
            .AddSingleton(notifier)
            .BuildServiceProvider();
        TenantReadRefreshSubscription sut = new(services);
        using var cancellation = new CancellationTokenSource();
        int refreshCount = 0;

        Task<TenantReadRefreshLease> pending = sut.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () =>
            {
                refreshCount++;
                return Task.CompletedTask;
            },
            cancellation.Token);
        cancellation.Cancel();
        setupCompleted.SetResult();

        _ = await Should.ThrowAsync<OperationCanceledException>(pending);
        await subscription.Received(1).UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        await Task.Yield();
        refreshCount.ShouldBe(0);
    }

    [Fact]
    public async Task Setup_failures_log_only_a_bounded_reason_code()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        subscription.SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("unsafe tenant.alpha literal")));
        var logger = new CapturingLogger();
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(subscription)
            .AddSingleton(notifier)
            .BuildServiceProvider();
        TenantReadRefreshSubscription sut = new(services, logger);

        await using TenantReadRefreshLease lease = await sut.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () => Task.CompletedTask);

        lease.IsSubscribed.ShouldBeFalse();
        string logged = string.Join(" ", logger.Messages);
        logged.ShouldContain(TenantReadRefreshSubscription.SetupFailureReasonCode);
        logged.ShouldNotContain("tenant.alpha");
        logged.ShouldNotContain("unsafe");
        logger.Exceptions.ShouldHaveSingleItem().ShouldBeNull();
    }

    [Fact]
    public async Task Registration_is_live_before_backend_setup_can_publish_a_notification()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        TaskCompletionSource refreshed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        subscription.SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(
                    "tenants",
                    "tenant.alpha");
                return Task.CompletedTask;
            });
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(subscription)
            .AddSingleton(notifier)
            .BuildServiceProvider();
        TenantReadRefreshSubscription sut = new(services);

        await using TenantReadRefreshLease lease = await sut.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () =>
            {
                refreshed.TrySetResult();
                return Task.CompletedTask;
            });

        lease.IsSubscribed.ShouldBeTrue();
        await refreshed.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Failed_setup_returns_an_empty_lease_and_a_later_retry_can_subscribe()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        subscription.SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException(new InvalidOperationException("first setup fails")),
                Task.CompletedTask);
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(subscription)
            .AddSingleton(notifier)
            .BuildServiceProvider();
        TenantReadRefreshSubscription sut = new(services);

        await using TenantReadRefreshLease failed = await sut.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () => Task.CompletedTask);
        await using TenantReadRefreshLease retried = await sut.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () => Task.CompletedTask);

        failed.IsSubscribed.ShouldBeFalse();
        retried.IsSubscribed.ShouldBeTrue();
        await subscription.Received(2).SubscribeAsync(
            "tenants",
            "tenant.alpha",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throwing_callback_does_not_block_siblings_or_later_nudges_and_logs_only_a_reason_code()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        var logger = new CapturingLogger();
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(subscription)
            .AddSingleton(notifier)
            .BuildServiceProvider();
        TenantReadRefreshSubscription sut = new(services, logger);
        int successfulCallbacks = 0;
        TaskCompletionSource secondNudgeCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await using TenantReadRefreshLease throwing = await sut.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () => throw new InvalidOperationException("unsafe tenant.alpha callback detail"));
        await using TenantReadRefreshLease succeeding = await sut.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () =>
            {
                if (Interlocked.Increment(ref successfulCallbacks) == 2)
                {
                    secondNudgeCompleted.TrySetResult();
                }

                return Task.CompletedTask;
            });

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        await WaitForAsync(
            () => Volatile.Read(ref successfulCallbacks) == 1,
            "the first successful refresh callback");
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        await secondNudgeCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        successfulCallbacks.ShouldBe(2);
        logger.Messages.Count(message => message.Contains(
            TenantReadRefreshSubscription.CallbackFailureReasonCode,
            StringComparison.Ordinal)).ShouldBe(2);
        string logged = string.Join(" ", logger.Messages);
        logged.ShouldNotContain("tenant.alpha");
        logged.ShouldNotContain("unsafe");
        logger.Exceptions.ShouldAllBe(static exception => exception == null);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task WaitHelperAcceptsImmediateAndPolledSuccess(int successfulCheck)
    {
        int checks = 0;

        await WaitForAsync(() => ++checks == successfulCheck, "the expected refresh callback");

        checks.ShouldBe(successfulCheck);
    }

    [Theory]
    [InlineData("the first successful refresh callback")]
    [InlineData("the expected refresh callback")]
    public async Task WaitHelperReportsTheUnmetConditionWhenItsDeadlineExpires(string conditionDescription)
    {
        long started = Stopwatch.GetTimestamp();
        ShouldAssertException? failure = null;
        try
        {
            await WaitForAsync(() => false, conditionDescription);
        }
        catch (ShouldAssertException exception)
        {
            failure = exception;
        }

        failure.ShouldNotBeNull().Message.ShouldBe(
            $"Timed out after two seconds waiting for {conditionDescription}.");
        Stopwatch.GetElapsedTime(started).ShouldBeGreaterThan(TimeSpan.FromSeconds(1.9));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task WaitHelperPreservesPredicateExceptions(bool cancellation, bool afterDeadline)
    {
        Exception expected = cancellation
            ? new OperationCanceledException("predicate cancellation")
            : new InvalidOperationException("predicate failure");
        int checks = 0;
        Task waiting = WaitForAsync(
            () =>
            {
                if (++checks == 1)
                {
                    return false;
                }

                if (afterDeadline)
                {
                    // Deliberately hold this synchronous predicate past the helper's two-second deadline.
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                }

                throw expected;
            },
            "the expected refresh callback");

        Exception? actual = null;
        try
        {
            await waiting;
        }
        catch (Exception exception)
        {
            actual = exception;
        }

        actual.ShouldBeSameAs(expected);
        checks.ShouldBe(2);
    }

    private static async Task WaitForAsync(Func<bool> condition, string conditionDescription)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            try
            {
                await Task.Delay(10, timeout.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                throw new ShouldAssertException($"Timed out after two seconds waiting for {conditionDescription}.");
            }
        }
    }

    private sealed class CapturingLogger : ILogger<TenantReadRefreshSubscription>
    {
        public List<string> Messages { get; } = [];

        public List<Exception?> Exceptions { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            Exceptions.Add(exception);
        }
    }

    [Fact]
    public async Task A_throwing_refresh_loop_still_releases_its_running_key_so_later_notifications_restart_it()
    {
        // OnProjectionChanged starts the loop only when _running.Add(key) succeeds, and removal used to
        // happen on exactly one path -- the normal return inside the lock. Any throw from the loop body
        // outside the inner try left the key in _running forever, so every later notification saw
        // start == false and auto-refresh never restarted. The task is fire-and-forget, so nothing observed
        // the fault. A throwing logger reproduces that shape: LogReason runs outside the inner try.
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();

        // A hand-written stub, not a substitute: the LoggerMessage-generated code checks IsEnabled first,
        // and a substitute returns false there, so the Log call -- and the throw -- never happen.
        ThrowingLogger logger = new();
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(subscription)
            .AddSingleton(notifier)
            .BuildServiceProvider();

        // The logger is an explicit constructor parameter, not resolved from the provider.
        TenantReadRefreshSubscription sut = new(services, logger);

        int refreshCount = 0;
        TaskCompletionSource firstFailed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondRan = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await using TenantReadRefreshLease lease = await sut.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () =>
            {
                int count = Interlocked.Increment(ref refreshCount);
                if (count == 1)
                {
                    firstFailed.SetResult();

                    // Faults the callback, so the loop reaches LogReason -- which throws.
                    throw new InvalidOperationException("callback failed");
                }

                secondRan.SetResult();
                return Task.CompletedTask;
            });
        lease.IsSubscribed.ShouldBeTrue();

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        await firstFailed.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // The key must have been released despite the throw, so a later notification starts a new loop.
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");
        await secondRan.Task.WaitAsync(TimeSpan.FromSeconds(2));

        refreshCount.ShouldBe(2);
    }

    /// <summary>
    /// A nudge that lands while the loop is unwinding from a throw must not be stranded.
    /// </summary>
    /// <remarks>
    /// The running key was released in the <c>finally</c>, in a second lock acquisition. A notification
    /// arriving between the loop's return and that re-acquisition added itself to the pending set, saw the
    /// key still marked running so started no loop, and was then dropped when the <c>finally</c> cleared it.
    /// The normal return path now releases in-lock, which closes that window outright; the throw path cannot,
    /// so it re-arms instead. This drives the throw path -- a throwing logger faults the loop body outside the
    /// inner try -- and queues the follow-up nudge from inside the failing callback, so the notification is
    /// already pending when the unwind begins.
    /// </remarks>
    [Fact]
    public async Task A_nudge_queued_while_the_loop_unwinds_from_a_throw_is_still_served()
    {
        IProjectionSubscription subscription = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        ThrowingLogger logger = new();
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(subscription)
            .AddSingleton(notifier)
            .BuildServiceProvider();
        TenantReadRefreshSubscription sut = new(services, logger);

        int refreshCount = 0;
        TaskCompletionSource secondRan = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await using TenantReadRefreshLease lease = await sut.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () =>
            {
                int count = Interlocked.Increment(ref refreshCount);
                if (count == 1)
                {
                    // Queued from inside the callback, so it is pending before the loop faults. The loop is
                    // already running, so this raises no second loop of its own -- only the unwinding
                    // finally can serve it.
                    notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>(
                        "tenants",
                        "tenant.alpha");

                    // Faults the callback, so the loop reaches LogReason -- which throws out of the body.
                    throw new InvalidOperationException("callback failed");
                }

                secondRan.SetResult();
                return Task.CompletedTask;
            });
        lease.IsSubscribed.ShouldBeTrue();

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "tenant.alpha");

        // No further external notification: the queued one must be served by the re-armed loop alone.
        await secondRan.Task.WaitAsync(TimeSpan.FromSeconds(2));
        refreshCount.ShouldBe(2);
    }

    /// <summary>A logger whose Log throws, standing in for one from a torn-down circuit scope.</summary>
    private sealed class ThrowingLogger : ILogger<TenantReadRefreshSubscription>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => throw new ObjectDisposedException(nameof(ThrowingLogger));
    }
}
