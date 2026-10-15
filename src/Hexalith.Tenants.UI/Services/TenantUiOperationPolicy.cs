using System.Globalization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Tenants.UI.Services;

/// <summary>
/// Applies the shared finite deadline to optional authority and notification operations.
/// </summary>
internal sealed class TenantUiOperationPolicy
{
    /// <summary>The deployment-owned configuration key for the shared deadline.</summary>
    internal const string ConfigurationKey = "Tenants:Ui:AuthorizationAndSubscriptionTimeout";

    /// <summary>The finite deadline used when deployment configuration omits the setting.</summary>
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    /// <summary>Creates the policy from deployment configuration and an optional clock.</summary>
    /// <param name="configuration">The host configuration.</param>
    /// <param name="timeProvider">The clock used for cancellation timers.</param>
    internal TenantUiOperationPolicy(IConfiguration? configuration = null, TimeProvider? timeProvider = null)
    {
        string? configured = configuration?[ConfigurationKey];
        if (configured is not null
            && (!TimeSpan.TryParse(configured, CultureInfo.InvariantCulture, out TimeSpan parsed)
                || parsed <= TimeSpan.Zero
                || parsed.TotalMilliseconds > uint.MaxValue - 1))
        {
            throw new InvalidOperationException($"{ConfigurationKey} must be a positive finite TimeSpan within timer limits.");
        }

        Timeout = configured is null
            ? DefaultTimeout
            : TimeSpan.Parse(configured, CultureInfo.InvariantCulture);
        TimeProvider = timeProvider ?? global::System.TimeProvider.System;
    }

    /// <summary>Gets the shared finite operation deadline.</summary>
    internal TimeSpan Timeout { get; }

    /// <summary>Gets the clock that owns deadline timers.</summary>
    internal TimeProvider TimeProvider { get; }

    /// <summary>Resolves the host policy, including hosts that register only individual components.</summary>
    /// <param name="services">The component or circuit services.</param>
    /// <returns>The configured shared policy.</returns>
    internal static TenantUiOperationPolicy FromServices(IServiceProvider services)
        => services.GetService<TenantUiOperationPolicy>()
            ?? new(services.GetService<IConfiguration>(), services.GetService<TimeProvider>());

    /// <summary>Runs an operation within the shared deadline and caller cancellation.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operation">The operation, receiving a cancellable token.</param>
    /// <param name="cancellationToken">Replaceable caller or circuit cancellation.</param>
    /// <param name="observeCompletion">Receives the complete provider lifetime, including cancellation callbacks.</param>
    /// <returns>The current operation result.</returns>
    internal async Task<T> RunAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default,
        Action<Task>? observeCompletion = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var source = new CancellationTokenSource();
        var sync = new object();
        Task? cancellation = null;
        Task RequestCancellationAsync()
        {
            lock (sync)
            {
                return cancellation ??= source.CancelAsync();
            }
        }

        // A provider's cancellation callback can itself stall. It must never run inline on the caller's
        // cancellation path or own the deadline waiter. CancelAsync sets the token immediately and runs
        // those callbacks independently; the waiter uses its own timer and the original caller token.
        CancellationTokenRegistration registration = cancellationToken.Register(() => _ = RequestCancellationAsync());
        Task<T>? pending = null;
        try
        {
            pending = operation(source.Token);
            T result = await pending.WaitAsync(Timeout, TimeProvider, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }
        catch (Exception exception) when (exception is TimeoutException || cancellationToken.IsCancellationRequested)
        {
            _ = RequestCancellationAsync();
            throw;
        }
        finally
        {
            // Late token registration remains valid until BOTH the raw operation and its asynchronous
            // cancellation callbacks settle. Neither a late exception nor callback failure is detached.
            Task completion = ObserveCompletionAsync(pending ?? Task.CompletedTask, source, registration, () =>
            {
                lock (sync)
                {
                    return cancellation;
                }
            });
            observeCompletion?.Invoke(completion);
        }
    }

    /// <summary>Runs a resultless operation within the same deadline.</summary>
    /// <param name="operation">The operation, receiving a cancellable token.</param>
    /// <param name="cancellationToken">Replaceable caller or circuit cancellation.</param>
    /// <param name="observeCompletion">Receives the complete provider lifetime, including cancellation callbacks.</param>
    /// <returns>A task completing when the bounded operation finishes.</returns>
    internal Task RunAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default,
        Action<Task>? observeCompletion = null)
        => RunAsync(async token =>
        {
            await operation(token).ConfigureAwait(false);
            return true;
        }, cancellationToken, observeCompletion);

    private static async Task ObserveCompletionAsync(
        Task pending,
        CancellationTokenSource source,
        CancellationTokenRegistration registration,
        Func<Task?> cancellation)
    {
        try
        {
            await pending.ConfigureAwait(false);
        }
        catch
        {
            // The active waiter handles current failures; late failures carry no authority or diagnostics.
        }
        finally
        {
            await registration.DisposeAsync().ConfigureAwait(false);
            if (cancellation() is Task callbacks)
            {
                try
                {
                    await callbacks.ConfigureAwait(false);
                }
                catch
                {
                    // Provider cancellation callbacks have no authority or diagnostic payload to apply.
                }
            }

            source.Dispose();
        }
    }
}
