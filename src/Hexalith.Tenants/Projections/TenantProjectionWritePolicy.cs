using System.Diagnostics;

using Dapr.Client;

using Hexalith.EventStore.Contracts.Projections;

using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.Projections;

internal static partial class TenantProjectionWritePolicy {
    public const int MaxAttempts = 3;

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

        for (int attempt = 1; attempt <= MaxAttempts; attempt++) {
            ProjectionStateRead<TValue> read = await stateStore
                .GetStateAndETagAsync<TValue>(storeName, key, cancellationToken)
                .ConfigureAwait(false);

            TValue state = read.Value ?? defaultFactory();
            foreach (ProjectionEventDto? evt in events) {
                if (evt is null) {
                    continue;
                }

                applyEvent(state, evt);
            }

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
                    messageIds);

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
                messageIds);
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

    [LoggerMessage(
        EventId = 100101,
        Level = LogLevel.Warning,
        Message = "Projection state optimistic concurrency conflict for state store {StateStoreName}, key category {StateKeyCategory}, attempt {AttemptCount} of {MaxAttempts}, operation {OperationContext}, reason {Reason}, correlation ID {CorrelationId}, message IDs {MessageIds}.")]
    private static partial void OptimisticConcurrencyConflict(
        ILogger logger,
        string stateStoreName,
        string stateKeyCategory,
        int attemptCount,
        int maxAttempts,
        string operationContext,
        string reason,
        string correlationId,
        string messageIds);

    [LoggerMessage(
        EventId = 100102,
        Level = LogLevel.Error,
        Message = "Projection state optimistic concurrency retry exhausted for state store {StateStoreName}, key category {StateKeyCategory}, attempts {AttemptCount} of {MaxAttempts}, operation {OperationContext}, reason {Reason}, correlation ID {CorrelationId}, message IDs {MessageIds}.")]
    private static partial void RetryExhausted(
        ILogger logger,
        string stateStoreName,
        string stateKeyCategory,
        int attemptCount,
        int maxAttempts,
        string operationContext,
        string reason,
        string correlationId,
        string messageIds);
}
