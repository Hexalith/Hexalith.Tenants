using System.Diagnostics;

using Dapr.Client;

using Hexalith.EventStore.Contracts.Projections;

using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.Projections;

internal static partial class TenantProjectionWritePolicy {
    public const int MaxAttempts = 3;

    private const int MaxLoggedEventTypes = 8;
    private const int MaxLoggedMessageIds = 20;
    private const string ConflictReason = "guarded-save-conflict";
    private const string RetryExhaustedReason = "retry-exhausted";

    public static async Task<TValue> SaveWithOptimisticConcurrencyAsync<TValue>(
        ITenantProjectionStateStore stateStore,
        ILogger logger,
        string storeName,
        string key,
        string stateKeyCategory,
        string operationContext,
        IReadOnlyCollection<ProjectionEventDto?> events,
        Func<TValue> defaultFactory,
        Action<TValue, ProjectionEventDto> applyEvent,
        CancellationToken cancellationToken = default)
        where TValue : class {
        ArgumentNullException.ThrowIfNull(stateStore);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(storeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateKeyCategory);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationContext);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(defaultFactory);
        ArgumentNullException.ThrowIfNull(applyEvent);

        string correlationId = events.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e?.CorrelationId))?.CorrelationId ?? string.Empty;
        string messageIds = BuildBoundedMessageIds(events);
        string eventTypes = BuildBoundedEventTypes(events);

        for (int attempt = 1; attempt <= MaxAttempts; attempt++) {
            cancellationToken.ThrowIfCancellationRequested();
            ProjectionStateRead<TValue> read = await stateStore
                .GetStateAndETagAsync<TValue>(storeName, key, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            // Reload-and-merge: when prior state exists we reuse it and re-apply the incoming
            // events. This works under both full-history replay (where reapplying is a no-op
            // for idempotent Apply) and delta replay (where the prior state is required to
            // avoid losing previously applied events). The contract this places on callers
            // is that `applyEvent` MUST be idempotent on the loaded state under full-replay,
            // i.e. reapplying the same event must not duplicate list entries, double-count
            // counters, or otherwise diverge from a from-scratch rebuild. The singleton
            // index path additionally depends on this branch to preserve entries from other
            // aggregates that share the key.
            TValue state = read.Value ?? defaultFactory();
            foreach (ProjectionEventDto? evt in events) {
                if (evt is null) {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                applyEvent(state, evt);
            }

            cancellationToken.ThrowIfCancellationRequested();
            bool saved = await stateStore
                .TrySaveStateAsync(
                    storeName,
                    key,
                    state,
                    read.ETag ?? string.Empty,
                    CreateGuardedWriteOptions(),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (saved) {
                return state;
            }

            if (attempt == MaxAttempts) {
                RetryExhausted(
                    logger,
                    storeName,
                    stateKeyCategory,
                    attempt,
                    MaxAttempts,
                    operationContext,
                    RetryExhaustedReason,
                    correlationId,
                    messageIds,
                    eventTypes);

                throw new InvalidOperationException(
                    $"{stateKeyCategory} projection write exceeded optimistic concurrency retry limit after {MaxAttempts} attempts.");
            }

            OptimisticConcurrencyConflict(
                logger,
                storeName,
                stateKeyCategory,
                attempt,
                MaxAttempts,
                operationContext,
                ConflictReason,
                correlationId,
                messageIds,
                eventTypes);
        }

        throw new UnreachableException();
    }

    public static async Task<TValue> SaveMergedWithOptimisticConcurrencyAsync<TValue>(
        ITenantProjectionStateStore stateStore,
        ILogger logger,
        string storeName,
        string key,
        string stateKeyCategory,
        string operationContext,
        IReadOnlyCollection<ProjectionEventDto?> events,
        TValue incomingState,
        Func<TValue> defaultFactory,
        Func<TValue, TValue, TValue> mergeState,
        CancellationToken cancellationToken = default)
        where TValue : class {
        ArgumentNullException.ThrowIfNull(stateStore);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentException.ThrowIfNullOrWhiteSpace(storeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateKeyCategory);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationContext);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(incomingState);
        ArgumentNullException.ThrowIfNull(defaultFactory);
        ArgumentNullException.ThrowIfNull(mergeState);

        string correlationId = events.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e?.CorrelationId))?.CorrelationId ?? string.Empty;
        string messageIds = BuildBoundedMessageIds(events);
        string eventTypes = BuildBoundedEventTypes(events);

        for (int attempt = 1; attempt <= MaxAttempts; attempt++) {
            cancellationToken.ThrowIfCancellationRequested();
            ProjectionStateRead<TValue> read = await stateStore
                .GetStateAndETagAsync<TValue>(storeName, key, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            TValue state = mergeState(read.Value ?? defaultFactory(), incomingState);
            cancellationToken.ThrowIfCancellationRequested();

            bool saved = await stateStore
                .TrySaveStateAsync(
                    storeName,
                    key,
                    state,
                    read.ETag ?? string.Empty,
                    CreateGuardedWriteOptions(),
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (saved) {
                return state;
            }

            if (attempt == MaxAttempts) {
                RetryExhausted(
                    logger,
                    storeName,
                    stateKeyCategory,
                    attempt,
                    MaxAttempts,
                    operationContext,
                    RetryExhaustedReason,
                    correlationId,
                    messageIds,
                    eventTypes);

                throw new InvalidOperationException(
                    $"{stateKeyCategory} projection write exceeded optimistic concurrency retry limit after {MaxAttempts} attempts.");
            }

            OptimisticConcurrencyConflict(
                logger,
                storeName,
                stateKeyCategory,
                attempt,
                MaxAttempts,
                operationContext,
                ConflictReason,
                correlationId,
                messageIds,
                eventTypes);
        }

        throw new UnreachableException();
    }

    private static StateOptions CreateGuardedWriteOptions() =>
        new() {
            Concurrency = ConcurrencyMode.FirstWrite,
        };

    private static string BuildBoundedMessageIds(IReadOnlyCollection<ProjectionEventDto?> events) {
        // Bound the joined log field so a full-replay batch with thousands of events
        // cannot emit hundreds-of-KB log lines per conflict or exhaustion.
        int total = 0;
        List<string> sample = new(MaxLoggedMessageIds);
        foreach (ProjectionEventDto? evt in events) {
            if (string.IsNullOrWhiteSpace(evt?.MessageId)) {
                continue;
            }

            total++;
            if (sample.Count < MaxLoggedMessageIds) {
                sample.Add(evt!.MessageId);
            }
        }

        string joined = string.Join(",", sample);
        int omitted = total - sample.Count;
        return omitted > 0 ? $"{joined}+{omitted} more" : joined;
    }

    private static string BuildBoundedEventTypes(IReadOnlyCollection<ProjectionEventDto?> events) {
        // Emit distinct event types per AC9 ("audit event ID/type when available")
        // so operators can recognise which event categories triggered a conflict
        // or exhaustion without payload bodies. Bounded for the same reason as
        // BuildBoundedMessageIds.
        HashSet<string> distinct = new(StringComparer.Ordinal);
        List<string> sample = new(MaxLoggedEventTypes);
        int omitted = 0;
        foreach (ProjectionEventDto? evt in events) {
            string? name = evt?.EventTypeName;
            if (string.IsNullOrWhiteSpace(name) || !distinct.Add(name)) {
                continue;
            }

            if (sample.Count < MaxLoggedEventTypes) {
                sample.Add(name);
            }
            else {
                omitted++;
            }
        }

        string joined = string.Join(",", sample);
        return omitted > 0 ? $"{joined}+{omitted} more" : joined;
    }

    [LoggerMessage(
        EventId = 100101,
        Level = LogLevel.Warning,
        Message = "Projection state optimistic concurrency conflict for state store {StateStoreName}, key category {StateKeyCategory}, attempt {AttemptCount} of {MaxAttempts}, operation {OperationContext}, reason {Reason}, correlation ID {CorrelationId}, message IDs {MessageIds}, event types {EventTypes}.")]
    private static partial void OptimisticConcurrencyConflict(
        ILogger logger,
        string stateStoreName,
        string stateKeyCategory,
        int attemptCount,
        int maxAttempts,
        string operationContext,
        string reason,
        string correlationId,
        string messageIds,
        string eventTypes);

    [LoggerMessage(
        EventId = 100102,
        Level = LogLevel.Error,
        Message = "Projection state optimistic concurrency retry exhausted for state store {StateStoreName}, key category {StateKeyCategory}, attempts {AttemptCount} of {MaxAttempts}, operation {OperationContext}, reason {Reason}, correlation ID {CorrelationId}, message IDs {MessageIds}, event types {EventTypes}.")]
    private static partial void RetryExhausted(
        ILogger logger,
        string stateStoreName,
        string stateKeyCategory,
        int attemptCount,
        int maxAttempts,
        string operationContext,
        string reason,
        string correlationId,
        string messageIds,
        string eventTypes);
}
