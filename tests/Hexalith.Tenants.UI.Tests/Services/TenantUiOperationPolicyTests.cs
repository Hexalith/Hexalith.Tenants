using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.Tenants.UI.Extensions;
using Hexalith.Tenants.UI.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services;

public sealed class TenantUiOperationPolicyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A_stalled_provider_cancellation_callback_cannot_hold_the_deadline_or_replacement_wait(bool callerCancellation)
    {
        var clock = new ManualTimeProvider();
        TenantUiOperationPolicy policy = Policy(clock);
        using var caller = new CancellationTokenSource();
        using var releaseCallback = new ManualResetEventSlim();
        TaskCompletionSource callbackEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<int> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken providerToken = default;
        Task<int> pending = policy.RunAsync(token =>
        {
            providerToken = token;
            _ = token.Register(() =>
            {
                callbackEntered.TrySetResult();
                releaseCallback.Wait();
            });
            return held.Task;
        }, caller.Token);
        // Dispatch the cancellation trigger so the regression also fails boundedly against the former
        // implementation, whose timer ran the blocking provider callback synchronously.
        Task trigger = Task.Run(() =>
        {
            if (callerCancellation)
            {
                caller.Cancel();
            }
            else
            {
                clock.Advance(TimeSpan.FromSeconds(1));
            }
        });
        try
        {
            await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
            if (callerCancellation)
            {
                _ = await Should.ThrowAsync<OperationCanceledException>(pending.WaitAsync(TimeSpan.FromSeconds(2)));
            }
            else
            {
                _ = await Should.ThrowAsync<TimeoutException>(pending.WaitAsync(TimeSpan.FromSeconds(2)));
            }

            providerToken.IsCancellationRequested.ShouldBeTrue();
            held.SetResult(100);
            using CancellationTokenRegistration late = providerToken.Register(() => { });
            (await policy.RunAsync(_ => Task.FromResult(42))).ShouldBe(42);
        }
        finally
        {
            releaseCallback.Set();
            held.TrySetResult(100);
            await trigger.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    [Fact]
    public async Task Production_registration_applies_one_configured_clock_and_deadline_to_authority_setup_and_cleanup()
    {
        var clock = new ManualTimeProvider();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { [TenantUiOperationPolicy.ConfigurationKey] = "00:00:02" }).Build();
        IProjectionSubscription backend = Substitute.For<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant notifier = Substitute.For<IProjectionChangeNotifierWithTenant>();
        TaskCompletionSource<int> authority = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource setup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource cleanup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource alphaCleaned = new(TaskCreationOptions.RunContinuationsAsynchronously);
        backend.SubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(setup.Task);
        backend.UnsubscribeAsync("tenants", "tenant.beta", Arg.Any<CancellationToken>()).Returns(cleanup.Task);
        backend.UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            alphaCleaned.TrySetResult();
            return Task.CompletedTask;
        });
        ServiceCollection registrations = new();
        registrations.AddLogging();
        registrations.AddSingleton(configuration);
        registrations.AddSingleton<TimeProvider>(clock);
        registrations.AddSingleton(backend);
        registrations.AddSingleton(notifier);
        registrations.AddHexalithTenantsUiModule(configuration, enableGatewayAuthorization: false);
        await using ServiceProvider services = registrations.BuildServiceProvider();
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        TenantUiOperationPolicy policy = scope.ServiceProvider.GetRequiredService<TenantUiOperationPolicy>();
        TenantUiOperationPolicy.FromServices(scope.ServiceProvider).ShouldBeSameAs(policy);
        policy.TimeProvider.ShouldBeSameAs(clock);
        policy.Timeout.ShouldBe(TimeSpan.FromSeconds(2));
        TenantReadRefreshSubscription coordinator = scope.ServiceProvider.GetRequiredService<TenantReadRefreshSubscription>();
        Task<int> resolving = policy.RunAsync(_ => authority.Task);
        Task<TenantReadRefreshLease> subscribing = coordinator.SubscribeAsync("tenants", "tenant.alpha", () => Task.CompletedTask);
        TenantReadRefreshLease beta = await coordinator.SubscribeAsync("tenants", "tenant.beta", () => Task.CompletedTask);
        Task releasing = beta.DisposeAsync().AsTask();

        clock.Advance(TimeSpan.FromMilliseconds(1999));
        resolving.IsCompleted.ShouldBeFalse();
        subscribing.IsCompleted.ShouldBeFalse();
        releasing.IsCompleted.ShouldBeFalse();
        clock.Advance(TimeSpan.FromMilliseconds(1));
        _ = await Should.ThrowAsync<TimeoutException>(resolving.WaitAsync(TimeSpan.FromSeconds(2)));
        (await subscribing.WaitAsync(TimeSpan.FromSeconds(2))).IsSubscribed.ShouldBeFalse();
        await releasing.WaitAsync(TimeSpan.FromSeconds(2));
        authority.SetResult(100);
        setup.SetResult();
        cleanup.SetResult();
        await alphaCleaned.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await backend.Received(1).UnsubscribeAsync("tenants", "tenant.alpha", Arg.Any<CancellationToken>());
        await backend.Received(1).UnsubscribeAsync("tenants", "tenant.beta", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("50.00:00:00")]
    public void Production_registration_rejects_an_invalid_finite_deadline(string configured)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { [TenantUiOperationPolicy.ConfigurationKey] = configured }).Build();
        ServiceCollection registrations = new();
        registrations.AddHexalithTenantsUiModule(configuration, enableGatewayAuthorization: false);
        using ServiceProvider services = registrations.BuildServiceProvider();

        _ = Should.Throw<InvalidOperationException>(() => services.GetRequiredService<TenantUiOperationPolicy>());
    }

    [Fact]
    public async Task Configured_deadline_cancels_an_ignoring_provider_without_disposing_its_late_token()
    {
        var clock = new ManualTimeProvider();
        TenantUiOperationPolicy policy = Policy(clock);
        TaskCompletionSource<int> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken providerToken = default;
        Task<int> pending = policy.RunAsync(token =>
        {
            providerToken = token;
            return held.Task;
        });

        clock.Advance(TimeSpan.FromMilliseconds(999));
        pending.IsCompleted.ShouldBeFalse();
        clock.Advance(TimeSpan.FromMilliseconds(1));
        _ = await Should.ThrowAsync<TimeoutException>(pending.WaitAsync(TimeSpan.FromSeconds(2)));
        providerToken.IsCancellationRequested.ShouldBeTrue();
        int lateCancellation = 0;
        using CancellationTokenRegistration late = providerToken.Register(() => lateCancellation++);
        lateCancellation.ShouldBe(1);
        held.SetException(new InvalidOperationException("late provider failure"));

        (await policy.RunAsync(_ => Task.FromResult(42))).ShouldBe(42);
        clock.PendingTimerCount.ShouldBe(0);
    }

    [Fact]
    public async Task Replaceable_caller_cancellation_ends_the_wait_before_deadline_and_late_success_has_no_result()
    {
        var clock = new ManualTimeProvider();
        TenantUiOperationPolicy policy = Policy(clock);
        TaskCompletionSource<int> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var caller = new CancellationTokenSource();
        CancellationToken providerToken = default;
        Task<int> pending = policy.RunAsync(token =>
        {
            providerToken = token;
            return held.Task;
        }, caller.Token);

        caller.Cancel();
        _ = await Should.ThrowAsync<OperationCanceledException>(pending.WaitAsync(TimeSpan.FromSeconds(2)));
        providerToken.IsCancellationRequested.ShouldBeTrue();
        using CancellationTokenRegistration late = providerToken.Register(() => { });
        held.SetResult(100);
        (await policy.RunAsync(_ => Task.FromResult(42))).ShouldBe(42);
    }

    [Theory]
    [InlineData("")]
    [InlineData("unbounded")]
    [InlineData("00:00:00")]
    [InlineData("-00:00:01")]
    [InlineData("50.00:00:00")]
    public void Invalid_deadline_cannot_disable_the_finite_bound(string configured)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { [TenantUiOperationPolicy.ConfigurationKey] = configured }).Build();

        _ = Should.Throw<InvalidOperationException>(() => new TenantUiOperationPolicy(configuration));
    }

    [Fact]
    public void Hosts_share_the_registered_policy_and_the_default_stays_finite()
    {
        var clock = new ManualTimeProvider();
        TenantUiOperationPolicy policy = Policy(clock);
        using ServiceProvider services = new ServiceCollection().AddSingleton(policy).BuildServiceProvider();

        TenantUiOperationPolicy.FromServices(services).ShouldBeSameAs(policy);
        policy.Timeout.ShouldBe(TimeSpan.FromSeconds(1));
        policy.TimeProvider.ShouldBeSameAs(clock);
        new TenantUiOperationPolicy().Timeout.ShouldBe(TimeSpan.FromSeconds(10));
    }

    private static TenantUiOperationPolicy Policy(TimeProvider clock)
        => new(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { [TenantUiOperationPolicy.ConfigurationKey] = "00:00:01" }).Build(), clock);
}
