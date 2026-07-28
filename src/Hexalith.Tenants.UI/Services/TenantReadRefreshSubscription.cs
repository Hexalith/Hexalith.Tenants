using Hexalith.FrontComposer.Contracts.Communication;

using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Tenants.UI.Services;

/// <summary>
/// Converts optional projection notifications into coalesced authoritative read callbacks.
/// </summary>
public sealed class TenantReadRefreshSubscription(IServiceProvider services)
{
    /// <summary>
    /// Subscribes to one canonical projection and tenant scope when notification services are available.
    /// </summary>
    /// <param name="projectionType">Canonical projection type.</param>
    /// <param name="tenantId">Canonical tenant subscription scope.</param>
    /// <param name="refresh">Authoritative direct-read callback.</param>
    /// <param name="cancellationToken">Subscription cancellation.</param>
    /// <returns>A lease that detaches the notifier and subscription.</returns>
    public async Task<IAsyncDisposable> SubscribeAsync(
        string projectionType,
        string tenantId,
        Func<Task> refresh,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(refresh);

        IProjectionSubscription? subscription = services.GetService<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant? notifier = services.GetService<IProjectionChangeNotifierWithTenant>();
        if (subscription is null || notifier is null)
        {
            return NoOpLease.Instance;
        }

        Lease lease = new(subscription, notifier, projectionType, tenantId, refresh);
        notifier.ProjectionChangedForTenant += lease.OnProjectionChanged;
        try
        {
            await subscription.SubscribeAsync(projectionType, tenantId, cancellationToken).ConfigureAwait(false);
            return lease;
        }
        catch (OperationCanceledException)
        {
            notifier.ProjectionChangedForTenant -= lease.OnProjectionChanged;
            throw;
        }
        catch
        {
            notifier.ProjectionChangedForTenant -= lease.OnProjectionChanged;
            return NoOpLease.Instance;
        }
    }

    private sealed class Lease(
        IProjectionSubscription subscription,
        IProjectionChangeNotifierWithTenant notifier,
        string projectionType,
        string tenantId,
        Func<Task> refresh) : IAsyncDisposable
    {
        private int _disposed;
        private int _pending;
        private int _running;

        internal void OnProjectionChanged(string changedProjectionType, string changedTenantId)
        {
            if (Volatile.Read(ref _disposed) != 0
                || !string.Equals(changedProjectionType, projectionType, StringComparison.Ordinal)
                || !string.Equals(changedTenantId, tenantId, StringComparison.Ordinal))
            {
                return;
            }

            _ = Interlocked.Exchange(ref _pending, 1);
            if (Interlocked.CompareExchange(ref _running, 1, 0) == 0)
            {
                _ = RunRefreshLoopAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            notifier.ProjectionChangedForTenant -= OnProjectionChanged;
            try
            {
                await subscription.UnsubscribeAsync(projectionType, tenantId, CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Notification cleanup is best-effort and never changes read authority.
            }
        }

        private async Task RunRefreshLoopAsync()
        {
            try
            {
                while (Volatile.Read(ref _disposed) == 0
                    && Interlocked.Exchange(ref _pending, 0) != 0)
                {
                    try
                    {
                        await refresh().ConfigureAwait(false);
                    }
                    catch
                    {
                        // A nudge is never evidence; the owning surface retains its last-confirmed state.
                    }
                }
            }
            finally
            {
                _ = Interlocked.Exchange(ref _running, 0);
                if (Volatile.Read(ref _disposed) == 0
                    && Volatile.Read(ref _pending) != 0
                    && Interlocked.CompareExchange(ref _running, 1, 0) == 0)
                {
                    _ = RunRefreshLoopAsync();
                }
            }
        }
    }

    private sealed class NoOpLease : IAsyncDisposable
    {
        internal static readonly NoOpLease Instance = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
