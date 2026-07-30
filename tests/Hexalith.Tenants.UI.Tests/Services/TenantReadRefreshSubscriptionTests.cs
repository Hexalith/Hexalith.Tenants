using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.Tenants.UI.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services;

public sealed class TenantReadRefreshSubscriptionTests
{
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
        await WaitForAsync(() => Volatile.Read(ref successfulCallbacks) == 1);
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

    private static async Task WaitForAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
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
