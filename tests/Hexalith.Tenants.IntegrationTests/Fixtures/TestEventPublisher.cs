using System.Collections.Concurrent;

using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Server.Events;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>
/// Test publisher for shared DAPR fixtures. Event observations and failures are correlation-scoped
/// so concurrently running tests cannot clear or fail each other's publish attempts.
/// </summary>
public sealed class TestEventPublisher : IEventPublisher {
    private readonly ConcurrentDictionary<string, ConcurrentBag<EventEnvelope>> _eventsByTopic = new();
    private readonly ConcurrentDictionary<string, string> _failuresByCorrelationId = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _topicOverrides = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentBag<PublishCall> _publishCalls = [];
    private int _publishedEventCount;

    /// <summary>Gets domain-specific topic overrides used by tests that mirror configured publisher behavior.</summary>
    public IDictionary<string, string> TopicOverrides => _topicOverrides;

    /// <summary>Gets the list of all publish calls for test assertions.</summary>
    public IReadOnlyList<PublishCall> PublishCalls => [.. _publishCalls];

    /// <summary>Gets the total number of events published across all calls.</summary>
    public int TotalEventsPublished => _publishedEventCount;

    /// <summary>Gets all unique topic names that events have been published to.</summary>
    public IReadOnlyList<string> GetPublishedTopics()
        => [.. _eventsByTopic.Keys.Order()];

    /// <summary>Gets all events published to a specific topic.</summary>
    public IReadOnlyList<EventEnvelope> GetEventsForTopic(string topic) {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        return _eventsByTopic.TryGetValue(topic, out ConcurrentBag<EventEnvelope>? events)
            ? [.. events]
            : [];
    }

    /// <summary>Configures publication for one correlation ID to fail.</summary>
    public void SetupFailureForCorrelation(string correlationId, string failureMessage) {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(failureMessage);
        _failuresByCorrelationId[correlationId] = failureMessage;
    }

    /// <summary>Configures all currently known publish attempts to fail until cleared.</summary>
    public void SetupFailure(string failureMessage = "Pub/sub unavailable") {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureMessage);
        _failuresByCorrelationId["*"] = failureMessage;
    }

    /// <summary>Clears all configured failures.</summary>
    public void ClearFailure() => _failuresByCorrelationId.Clear();

    /// <summary>Clears the failure configured for a single correlation ID.</summary>
    public void ClearFailureForCorrelation(string correlationId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        _ = _failuresByCorrelationId.TryRemove(correlationId, out _);
    }

    /// <summary>
    /// Preserves observations for parallel tests. Tests use unique correlation IDs and tenant IDs
    /// for assertions, so clearing shared state would create races.
    /// </summary>
    public void Reset() {
        _ = _eventsByTopic.Count;
    }

    /// <inheritdoc/>
    public Task<EventPublishResult> PublishEventsAsync(
        AggregateIdentity identity,
        IReadOnlyList<EventEnvelope> events,
        string correlationId,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        string topic = _topicOverrides.TryGetValue(identity.Domain, out string? configuredTopic) && !string.IsNullOrWhiteSpace(configuredTopic)
            ? configuredTopic
            : identity.PubSubTopic;
        _publishCalls.Add(new PublishCall(identity, events, correlationId, topic));

        if (_failuresByCorrelationId.TryGetValue(correlationId, out string? failureReason)
            || _failuresByCorrelationId.TryGetValue("*", out failureReason)) {
            return Task.FromResult(new EventPublishResult(Success: false, PublishedCount: 0, FailureReason: failureReason));
        }

        if (events.Count > 0) {
            ConcurrentBag<EventEnvelope> topicEvents = _eventsByTopic.GetOrAdd(topic, _ => []);
            foreach (EventEnvelope @event in events) {
                topicEvents.Add(@event);
            }
        }

        _ = Interlocked.Add(ref _publishedEventCount, events.Count);
        return Task.FromResult(new EventPublishResult(Success: true, PublishedCount: events.Count, FailureReason: null));
    }

    /// <summary>Record of a single publish call.</summary>
    public record PublishCall(
        AggregateIdentity Identity,
        IReadOnlyList<EventEnvelope> Events,
        string CorrelationId,
        string Topic);
}
