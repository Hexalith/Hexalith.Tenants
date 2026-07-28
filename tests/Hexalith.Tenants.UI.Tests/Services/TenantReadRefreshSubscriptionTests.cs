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

        IAsyncDisposable lease = await sut.SubscribeAsync(
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
        await using IAsyncDisposable noOp = await unavailable.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () => throw new InvalidOperationException("must not run"));
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

        Task<IAsyncDisposable> pending = sut.SubscribeAsync(
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

        await using IAsyncDisposable lease = await sut.SubscribeAsync(
            "tenants",
            "tenant.alpha",
            () => Task.CompletedTask);

        string logged = string.Join(" ", logger.Messages);
        logged.ShouldContain(TenantReadRefreshSubscription.SetupFailureReasonCode);
        logged.ShouldNotContain("tenant.alpha");
        logged.ShouldNotContain("unsafe");
    }

    private sealed class CapturingLogger : ILogger<TenantReadRefreshSubscription>
    {
        public List<string> Messages { get; } = [];

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
            => Messages.Add(formatter(state, exception));
    }
}
