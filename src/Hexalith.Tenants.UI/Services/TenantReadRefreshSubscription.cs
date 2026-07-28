using Hexalith.FrontComposer.Contracts.Communication;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.UI.Services;

/// <summary>
/// Converts optional projection notifications into coalesced authoritative read callbacks.
/// </summary>
public sealed partial class TenantReadRefreshSubscription(
    IServiceProvider services,
    ILogger<TenantReadRefreshSubscription>? logger = null)
{
    /// <summary>Reason code emitted when notification setup fails.</summary>
    internal const string SetupFailureReasonCode = "notification-setup-failed";

    /// <summary>Reason code emitted when an authoritative callback fails.</summary>
    internal const string CallbackFailureReasonCode = "notification-callback-failed";

    /// <summary>Reason code emitted when notification cleanup fails.</summary>
    internal const string CleanupFailureReasonCode = "notification-cleanup-failed";

    private readonly Dictionary<(string ProjectionType, string TenantId), Dictionary<Guid, Func<Task>>> _callbacks = [];
    private readonly HashSet<(string ProjectionType, string TenantId)> _pending = [];
    private readonly HashSet<(string ProjectionType, string TenantId)> _running = [];
    private readonly SemaphoreSlim _subscriptionGate = new(1, 1);
    private readonly object _sync = new();
    private IProjectionSubscription? _subscription;
    private IProjectionChangeNotifierWithTenant? _notifier;

    /// <summary>
    /// Subscribes to one canonical projection and tenant scope when notification services are available.
    /// </summary>
    /// <param name="projectionType">Canonical projection type.</param>
    /// <param name="tenantId">Canonical tenant subscription scope.</param>
    /// <param name="refresh">Authoritative direct-read callback.</param>
    /// <param name="cancellationToken">Subscription cancellation.</param>
    /// <returns>A reference-counted lease that detaches only its own callback.</returns>
    /// <remarks>
    /// Returns the concrete lease so callers can distinguish a live registration from
    /// <see cref="TenantReadRefreshLease.Empty"/> via <see cref="TenantReadRefreshLease.IsSubscribed"/>.
    /// Recording an Empty lease as an established subscription made a transient setup failure permanent.
    /// </remarks>
    public async Task<TenantReadRefreshLease> SubscribeAsync(
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
            return TenantReadRefreshLease.Empty;
        }

        (string ProjectionType, string TenantId) key = (projectionType, tenantId);
        await _subscriptionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            bool alreadySubscribed;
            lock (_sync)
            {
                alreadySubscribed = _callbacks.ContainsKey(key);
            }

            if (!alreadySubscribed)
            {
                try
                {
                    await subscription
                        .SubscribeAsync(projectionType, tenantId, cancellationToken)
                        .ConfigureAwait(false);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await subscription
                                .UnsubscribeAsync(projectionType, tenantId, CancellationToken.None)
                                .ConfigureAwait(false);
                        }
                        catch
                        {
                            LogReason(CleanupFailureReasonCode);
                        }

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    LogReason(SetupFailureReasonCode);
                    return TenantReadRefreshLease.Empty;
                }
            }

            Guid callbackId = Guid.NewGuid();
            lock (_sync)
            {
                if (_callbacks.Count == 0)
                {
                    notifier.ProjectionChangedForTenant += OnProjectionChanged;
                }

                if (!_callbacks.TryGetValue(key, out Dictionary<Guid, Func<Task>>? callbacks))
                {
                    callbacks = [];
                    _callbacks.Add(key, callbacks);
                }

                callbacks.Add(callbackId, refresh);
                _subscription = subscription;
                _notifier = notifier;
            }

            return new TenantReadRefreshLease(() => ReleaseAsync(key, callbackId));
        }
        finally
        {
            _subscriptionGate.Release();
        }
    }

    private void OnProjectionChanged(string projectionType, string tenantId)
    {
        (string ProjectionType, string TenantId) key = (projectionType, tenantId);
        bool start;
        lock (_sync)
        {
            if (!_callbacks.ContainsKey(key))
            {
                return;
            }

            _ = _pending.Add(key);
            start = _running.Add(key);
        }

        if (start)
        {
            _ = RunRefreshLoopAsync(key);
        }
    }

    private async ValueTask ReleaseAsync(
        (string ProjectionType, string TenantId) key,
        Guid callbackId)
    {
        await _subscriptionGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            bool unsubscribe = false;
            IProjectionSubscription? subscription;
            IProjectionChangeNotifierWithTenant? notifier;
            lock (_sync)
            {
                if (!_callbacks.TryGetValue(key, out Dictionary<Guid, Func<Task>>? callbacks)
                    || !callbacks.Remove(callbackId))
                {
                    return;
                }

                if (callbacks.Count == 0)
                {
                    _callbacks.Remove(key);
                    _pending.Remove(key);
                    unsubscribe = true;
                }

                subscription = _subscription;
                notifier = _notifier;
                if (_callbacks.Count == 0 && notifier is not null)
                {
                    notifier.ProjectionChangedForTenant -= OnProjectionChanged;
                    _subscription = null;
                    _notifier = null;
                }
            }

            if (unsubscribe && subscription is not null)
            {
                try
                {
                    await subscription
                        .UnsubscribeAsync(key.ProjectionType, key.TenantId, CancellationToken.None)
                        .ConfigureAwait(false);
                }
                catch
                {
                    LogReason(CleanupFailureReasonCode);
                }
            }
        }
        finally
        {
            _subscriptionGate.Release();
        }
    }

    private async Task RunRefreshLoopAsync((string ProjectionType, string TenantId) key)
    {
        while (true)
        {
            Func<Task>[] callbacks;
            lock (_sync)
            {
                if (!_pending.Remove(key)
                    || !_callbacks.TryGetValue(key, out Dictionary<Guid, Func<Task>>? registered))
                {
                    _running.Remove(key);
                    return;
                }

                callbacks = [.. registered.Values];
            }

            foreach (Func<Task> callback in callbacks)
            {
                try
                {
                    await callback().ConfigureAwait(false);
                }
                catch
                {
                    LogReason(CallbackFailureReasonCode);
                }
            }
        }
    }

    private void LogReason(string reasonCode)
    {
        if (logger is not null)
        {
            NotificationOperationFailed(logger, reasonCode);
        }
    }

    [LoggerMessage(
        EventId = 1910,
        Level = LogLevel.Warning,
        Message = "Tenant read refresh notification operation failed. ReasonCode: {ReasonCode}")]
    private static partial void NotificationOperationFailed(ILogger logger, string reasonCode);
}
