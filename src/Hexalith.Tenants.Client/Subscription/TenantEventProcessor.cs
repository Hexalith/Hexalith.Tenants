using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Events;
using Hexalith.Tenants.Client.Handlers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.Client.Subscription;

/// <summary>
/// Receives tenant event envelopes, resolves the event type, deserializes the payload,
/// deduplicates by MessageId, and dispatches to registered <see cref="ITenantEventHandler{TEvent}"/> implementations.
/// </summary>
public class TenantEventProcessor {
    private enum ProcessingState {
        InProgress,
        Completed,
    }

    private static readonly MethodInfo _dispatchMethod = typeof(TenantEventProcessor)
        .GetMethod(nameof(DispatchAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly IReadOnlyDictionary<string, Type> _eventTypeRegistry;
    private readonly ILogger<TenantEventProcessor> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    // NOTE: This dictionary grows unboundedly. For MVP, this is acceptable (consuming services are
    // typically restarted periodically). Production deployments should consider a bounded LRU cache
    // or external deduplication store.
    private readonly ConcurrentDictionary<string, ProcessingState> _processedMessageIds = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantEventProcessor"/> class.
    /// </summary>
    /// <param name="serviceScopeFactory">The service scope factory for resolving event handlers.</param>
    /// <param name="eventTypeRegistry">The mapping of event type names to their CLR types.</param>
    /// <param name="logger">The logger.</param>
    public TenantEventProcessor(
        IServiceScopeFactory serviceScopeFactory,
        IReadOnlyDictionary<string, Type> eventTypeRegistry,
        ILogger<TenantEventProcessor> logger) {
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);
        ArgumentNullException.ThrowIfNull(eventTypeRegistry);
        ArgumentNullException.ThrowIfNull(logger);
        _serviceScopeFactory = serviceScopeFactory;
        _eventTypeRegistry = eventTypeRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Processes a tenant event envelope: deduplicates, resolves type, deserializes, and dispatches to handlers.
    /// </summary>
    /// <param name="envelope">The tenant event envelope.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The event processing outcome.</returns>
    public async Task<TenantEventProcessingResult> ProcessAsync(TenantEventEnvelope envelope, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!_processedMessageIds.TryAdd(envelope.MessageId, ProcessingState.InProgress)) {
            _logger.LogDebug("Skipping duplicate event {MessageId}", envelope.MessageId);
            return TenantEventProcessingResult.Duplicate;
        }

        try {
            if (!_eventTypeRegistry.TryGetValue(envelope.EventTypeName, out Type? eventType)) {
                _logger.LogWarning("Unknown event type '{EventTypeName}' — skipping", envelope.EventTypeName);
                _processedMessageIds[envelope.MessageId] = ProcessingState.Completed;
                return TenantEventProcessingResult.SkippedUnknownEventType;
            }

            object? deserialized;
            try {
                deserialized = JsonSerializer.Deserialize(envelope.Payload, eventType);
            }
            catch (JsonException exception) {
                _logger.LogWarning(exception, "Failed to deserialize event {MessageId} as {EventTypeName}", envelope.MessageId, envelope.EventTypeName);
                _ = _processedMessageIds.TryRemove(envelope.MessageId, out _);
                return TenantEventProcessingResult.FailedInvalidPayload;
            }
            catch (NotSupportedException exception) {
                _logger.LogWarning(exception, "Failed to deserialize event {MessageId} as {EventTypeName}", envelope.MessageId, envelope.EventTypeName);
                _ = _processedMessageIds.TryRemove(envelope.MessageId, out _);
                return TenantEventProcessingResult.FailedInvalidPayload;
            }

            if (deserialized is not IEventPayload @event) {
                _logger.LogWarning("Failed to deserialize event {MessageId} as {EventTypeName}", envelope.MessageId, envelope.EventTypeName);
                _ = _processedMessageIds.TryRemove(envelope.MessageId, out _);
                return TenantEventProcessingResult.FailedInvalidPayload;
            }

            var context = new TenantEventContext(
                envelope.AggregateId,
                envelope.MessageId,
                envelope.SequenceNumber,
                envelope.Timestamp,
                envelope.CorrelationId);

            MethodInfo genericDispatch = _dispatchMethod.MakeGenericMethod(eventType);
            using IServiceScope scope = _serviceScopeFactory.CreateScope();
            int handlerCount = await ((Task<int>)genericDispatch.Invoke(null, [scope.ServiceProvider, @event, context, cancellationToken])!).ConfigureAwait(false);
            if (handlerCount == 0) {
                _logger.LogWarning("No handlers registered for event type '{EventTypeName}' — skipping", envelope.EventTypeName);
                _processedMessageIds[envelope.MessageId] = ProcessingState.Completed;
                return TenantEventProcessingResult.SkippedNoHandlers;
            }

            _processedMessageIds[envelope.MessageId] = ProcessingState.Completed;
            return TenantEventProcessingResult.Processed;
        }
        catch {
            _ = _processedMessageIds.TryRemove(envelope.MessageId, out _);
            throw;
        }
    }

    private static async Task<int> DispatchAsync<TEvent>(
        IServiceProvider serviceProvider,
        TEvent @event,
        TenantEventContext context,
        CancellationToken cancellationToken)
        where TEvent : IEventPayload {
        ITenantEventHandler<TEvent>[] handlers = serviceProvider.GetServices<ITenantEventHandler<TEvent>>().ToArray();
        foreach (ITenantEventHandler<TEvent> handler in handlers) {
            await handler.HandleAsync(@event, context, cancellationToken).ConfigureAwait(false);
        }

        return handlers.Length;
    }
}
