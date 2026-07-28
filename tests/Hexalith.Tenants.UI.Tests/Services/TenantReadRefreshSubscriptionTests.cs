using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.Tenants.UI.Services;

using Microsoft.Extensions.DependencyInjection;

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
            "tenant-index",
            "system",
            () =>
            {
                refreshCount++;
                return Task.CompletedTask;
            });

        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenants", "system");
        notifier.ProjectionChangedForTenant += Raise.Event<Action<string, string>>("tenant-index", "other");
        await Task.Yield();
        refreshCount.ShouldBe(0);

        TenantReadRefreshSubscription unavailable = new(new ServiceCollection().BuildServiceProvider());
        await using IAsyncDisposable noOp = await unavailable.SubscribeAsync(
            "tenant-index",
            "system",
            () => throw new InvalidOperationException("must not run"));
    }
}
