using Hexalith.FrontComposer.Contracts.Communication;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.UI.Services;

/// <summary>
/// Converts optional projection notifications into coalesced authoritative read callbacks.
/// </summary>
public sealed partial class TenantReadRefreshSubscription(
    IServiceProvider services,
    ILogger<TenantReadRefreshSubscription>? logger = null) : IAsyncDisposable
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
    private readonly Dictionary<(string ProjectionType, string TenantId), TaskCompletionSource> _unsettled = [];
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly TenantUiOperationPolicy _policy = TenantUiOperationPolicy.FromServices(services);
    private readonly object _sync = new();
    private IProjectionSubscription? _subscription;
    private IProjectionChangeNotifierWithTenant? _notifier;
    private bool _disposed;

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
        cancellationToken.ThrowIfCancellationRequested();
        using CancellationTokenSource lifetime = BeginOperationLifetime(cancellationToken);
        CancellationToken lifetimeToken = lifetime.Token;

        IProjectionSubscription? subscription = services.GetService<IProjectionSubscription>();
        IProjectionChangeNotifierWithTenant? notifier = services.GetService<IProjectionChangeNotifierWithTenant>();
        if (subscription is null || notifier is null)
        {
            return TenantReadRefreshLease.Empty;
        }

        // A single deadline includes waiting for an older owner of the same scope AND new setup.
        // Other scopes never wait on this key's backend operation. The core observes cancellation
        // itself so its local callback has been detached before the public bounded wait returns.
        Task<TenantReadRefreshLease>? pending = null;
        try
        {
            return await _policy.RunAsync(token => pending = SubscribeCoreAsync(
                (projectionType, tenantId), refresh, subscription, notifier, token), lifetime.Token).ConfigureAwait(false);
        }
        catch
        {
            if (pending is not null)
            {
                try
                {
                    TenantReadRefreshLease rejected = await pending.ConfigureAwait(false);
                    _ = rejected.DisposeAsync();
                }
                catch
                {
                    // The core has released callback ownership. Its backend work remains quarantined.
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (lifetimeToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(lifetimeToken);
            }

            LogReason(SetupFailureReasonCode);
            return TenantReadRefreshLease.Empty;
        }
    }

    private CancellationTokenSource BeginOperationLifetime(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeCancellation.Token);
        }
    }

    private async Task<TenantReadRefreshLease> SubscribeCoreAsync(
        (string ProjectionType, string TenantId) key,
        Func<Task> refresh,
        IProjectionSubscription subscription,
        IProjectionChangeNotifierWithTenant notifier,
        CancellationToken cancellationToken)
    {
        Guid callbackId = Guid.NewGuid();
        TaskCompletionSource owner;
        while (true)
        {
            Task? previous = null;
            lock (_sync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_unsettled.TryGetValue(key, out TaskCompletionSource? unsettled))
                {
                    previous = unsettled.Task;
                    owner = unsettled;
                }
                else if (_callbacks.TryGetValue(key, out Dictionary<Guid, Func<Task>>? established))
                {
                    established.Add(callbackId, refresh);
                    return new TenantReadRefreshLease(() => ReleaseAsync(key, callbackId, subscription, notifier));
                }
                else
                {
                    owner = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    if (_callbacks.Count == 0)
                    {
                        try
                        {
                            notifier.ProjectionChangedForTenant += OnProjectionChanged;
                        }
                        catch
                        {
                            try
                            {
                                notifier.ProjectionChangedForTenant -= OnProjectionChanged;
                            }
                            catch
                            {
                                // A failed optional event registration must leave no scoped gate owner.
                            }

                            throw;
                        }
                    }

                    _unsettled.Add(key, owner);
                    _callbacks.Add(key, new() { [callbackId] = refresh });
                    _subscription = subscription;
                    _notifier = notifier;
                }
            }

            if (previous is null)
            {
                break;
            }

            await previous.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        Task? setup = null;
        Task? setupLifetime = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _policy.RunAsync(token => setup = subscription.SubscribeAsync(
                key.ProjectionType, key.TenantId, token), cancellationToken, completion => setupLifetime = completion)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                CompleteUnsettled(key, owner);
            }

            return new TenantReadRefreshLease(() => ReleaseAsync(key, callbackId, subscription, notifier));
        }
        catch
        {
            lock (_sync)
            {
                RemoveCallback(key, callbackId, notifier);
            }

            // Unsubscribe is keyed by scope, not registration identity. Keep this key quarantined
            // until the actual old setup AND cleanup finish, including cancellation-ignoring tasks.
            _ = CleanupFailedSetupAsync(setupLifetime ?? setup, key, owner, subscription);
            throw;
        }
    }

    private void RemoveCallback(
        (string ProjectionType, string TenantId) key,
        Guid callbackId,
        IProjectionChangeNotifierWithTenant notifier)
    {
        if (_callbacks.TryGetValue(key, out Dictionary<Guid, Func<Task>>? callbacks))
        {
            _ = callbacks.Remove(callbackId);
            if (callbacks.Count == 0)
            {
                _callbacks.Remove(key);
                _pending.Remove(key);
            }
        }

        if (_callbacks.Count == 0)
        {
            DetachNotifierSafely(notifier);
            _subscription = null;
            _notifier = null;
        }
    }

    private async Task CleanupFailedSetupAsync(
        Task? setup,
        (string ProjectionType, string TenantId) key,
        TaskCompletionSource owner,
        IProjectionSubscription subscription)
    {
        if (setup is not null)
        {
            try
            {
                await setup.ConfigureAwait(false);
            }
            catch
            {
                // A late setup fault has no callback, authority or diagnostic payload to apply.
            }
        }

        await CleanupWithinDeadlineAsync(subscription, key, owner).ConfigureAwait(false);
    }

    private async Task CleanupWithinDeadlineAsync(
        IProjectionSubscription subscription,
        (string ProjectionType, string TenantId) key,
        TaskCompletionSource owner)
    {
        Task? cleanup = null;
        Task? cleanupLifetime = null;
        try
        {
            await _policy.RunAsync(token => cleanup = subscription
                .UnsubscribeAsync(key.ProjectionType, key.TenantId, token),
                observeCompletion: completion => cleanupLifetime = completion).ConfigureAwait(false);
        }
        catch
        {
            LogCleanupFailureSafely();
        }
        finally
        {
            Task? retired = cleanupLifetime ?? cleanup;
            if (retired is null || retired.IsCompleted)
            {
                CompleteUnsettled(key, owner);
            }
            else
            {
                _ = CompleteLateCleanupAsync(retired, key, owner);
            }
        }
    }

    private async Task CompleteLateCleanupAsync(
        Task cleanup,
        (string ProjectionType, string TenantId) key,
        TaskCompletionSource owner)
    {
        try
        {
            await cleanup.ConfigureAwait(false);
        }
        catch
        {
            // The bounded cleanup already recorded only the fixed failure reason.
        }
        finally
        {
            CompleteUnsettled(key, owner);
        }
    }

    private void CompleteUnsettled((string ProjectionType, string TenantId) key, TaskCompletionSource owner)
    {
        lock (_sync)
        {
            if (_unsettled.TryGetValue(key, out TaskCompletionSource? current) && ReferenceEquals(current, owner))
            {
                _unsettled.Remove(key);
                owner.TrySetResult();
            }
        }
    }

    private void OnProjectionChanged(string projectionType, string tenantId)
    {
        (string ProjectionType, string TenantId) key = (projectionType, tenantId);
        bool start;
        lock (_sync)
        {
            if (_disposed || !_callbacks.ContainsKey(key))
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
        Guid callbackId,
        IProjectionSubscription subscription,
        IProjectionChangeNotifierWithTenant notifier)
    {
        TaskCompletionSource? owner = null;
        lock (_sync)
        {
            if (!_callbacks.TryGetValue(key, out Dictionary<Guid, Func<Task>>? callbacks)
                || !callbacks.ContainsKey(callbackId))
            {
                return;
            }

            RemoveCallback(key, callbackId, notifier);
            if (!_callbacks.ContainsKey(key))
            {
                owner = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _unsettled.Add(key, owner);
            }
        }

        if (owner is not null)
        {
            await CleanupWithinDeadlineAsync(subscription, key, owner).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        List<((string ProjectionType, string TenantId) Key, TaskCompletionSource Owner)> cleanup = [];
        IProjectionSubscription? subscription;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            subscription = _subscription;
            if (_notifier is not null)
            {
                DetachNotifierSafely(_notifier);
            }

            foreach ((string ProjectionType, string TenantId) key in _callbacks.Keys)
            {
                if (!_unsettled.ContainsKey(key))
                {
                    var owner = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    _unsettled.Add(key, owner);
                    cleanup.Add((key, owner));
                }
            }

            _callbacks.Clear();
            _pending.Clear();
            _subscription = null;
            _notifier = null;
        }

        _lifetimeCancellation.Cancel();
        _lifetimeCancellation.Dispose();
        if (subscription is not null)
        {
            await Task.WhenAll(cleanup.Select(item => CleanupWithinDeadlineAsync(subscription, item.Key, item.Owner)))
                .ConfigureAwait(false);
        }
    }

    /// <summary>Drains pending notifications for one projection scope.</summary>
    /// <remarks>
    /// <para>
    /// The running key is released inside the <em>same</em> lock acquisition that observes the empty queue.
    /// Releasing it in the <c>finally</c> instead re-acquired the lock, and a notification arriving in that
    /// gap added itself to <c>_pending</c>, saw <c>_running.Add(key) == false</c> so started no loop, and was
    /// then stranded when the <c>finally</c> cleared the key -- trading the stuck-key fault below for a
    /// lost-wakeup one.
    /// </para>
    /// <para>
    /// The <c>finally</c> is retained for the throw path only. <c>OnProjectionChanged</c> starts this loop
    /// only when <c>_running.Add(key)</c> succeeds, so any throw from the loop body outside the inner try
    /// (most plausibly <c>LogReason</c> on a logger from a torn-down circuit scope) would otherwise leave the
    /// key in <c>_running</c> forever and auto-refresh would never restart. That path re-arms the loop when a
    /// notification did land while it was unwinding, so neither fault can strand a nudge.
    /// </para>
    /// </remarks>
    private async Task RunRefreshLoopAsync((string ProjectionType, string TenantId) key)
    {
        bool released = false;
        try
        {
            while (true)
            {
                KeyValuePair<Guid, Func<Task>>[] callbacks;
                lock (_sync)
                {
                    if (!_pending.Remove(key)
                        || !_callbacks.TryGetValue(key, out Dictionary<Guid, Func<Task>>? registered))
                    {
                        _ = _running.Remove(key);
                        released = true;
                        return;
                    }

                    callbacks = [.. registered];
                }

                foreach (KeyValuePair<Guid, Func<Task>> callback in callbacks)
                {
                    try
                    {
                        Task refresh;
                        lock (_sync)
                        {
                            if (_disposed
                                || !_callbacks.TryGetValue(key, out Dictionary<Guid, Func<Task>>? current)
                                || !current.ContainsKey(callback.Key))
                            {
                                continue;
                            }

                            // Invocation and removal share the lock; a captured callback cannot start
                            // after its lease or circuit has detached it. Awaiting never holds this lock.
                            refresh = callback.Value();
                        }

                        await refresh.ConfigureAwait(false);
                    }
                    catch
                    {
                        LogReason(CallbackFailureReasonCode);
                    }
                }
            }
        }
        finally
        {
            if (!released)
            {
                bool restart;
                lock (_sync)
                {
                    _ = _running.Remove(key);
                    restart = _pending.Contains(key)
                        && _callbacks.ContainsKey(key)
                        && _running.Add(key);
                }

                if (restart)
                {
                    _ = RunRefreshLoopAsync(key);
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

    private void DetachNotifierSafely(IProjectionChangeNotifierWithTenant notifier)
    {
        try
        {
            notifier.ProjectionChangedForTenant -= OnProjectionChanged;
        }
        catch
        {
            LogCleanupFailureSafely();
        }
    }

    private void LogCleanupFailureSafely()
    {
        try
        {
            LogReason(CleanupFailureReasonCode);
        }
        catch
        {
            // Retired cleanup can outlive its circuit logger. Diagnostics cannot retain ownership.
        }
    }

    [LoggerMessage(
        EventId = 1910,
        Level = LogLevel.Warning,
        Message = "Tenant read refresh notification operation failed. ReasonCode: {ReasonCode}")]
    private static partial void NotificationOperationFailed(ILogger logger, string reasonCode);
}
