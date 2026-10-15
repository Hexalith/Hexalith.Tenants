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
    /// <returns>The current operation result.</returns>
    internal async Task<T> RunAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var deadline = new CancellationTokenSource(Timeout, TimeProvider);
        var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        CancellationToken token = source.Token;
        Task<T> pending;
        try
        {
            pending = operation(token);
        }
        catch
        {
            source.Dispose();
            deadline.Dispose();
            throw;
        }

        // The provider can register against its token even after our bounded wait has finished. Its
        // completion owns disposal; observing it also contains faults arriving after timeout/cancellation.
        _ = ObserveCompletionAsync(pending, source, deadline);
        try
        {
            T result = await pending.WaitAsync(token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            throw new TimeoutException("The optional tenant UI operation exceeded its configured deadline.");
        }
    }

    /// <summary>Runs a resultless operation within the same deadline.</summary>
    /// <param name="operation">The operation, receiving a cancellable token.</param>
    /// <param name="cancellationToken">Replaceable caller or circuit cancellation.</param>
    /// <returns>A task completing when the bounded operation finishes.</returns>
    internal Task RunAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
        => RunAsync(async token =>
        {
            await operation(token).ConfigureAwait(false);
            return true;
        }, cancellationToken);

    private static async Task ObserveCompletionAsync(Task pending, CancellationTokenSource source, CancellationTokenSource deadline)
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
            source.Dispose();
            deadline.Dispose();
        }
    }
}
