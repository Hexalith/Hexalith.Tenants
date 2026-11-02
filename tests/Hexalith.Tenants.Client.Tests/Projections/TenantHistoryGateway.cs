using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;

namespace Hexalith.Tenants.Client.Tests.Projections;

/// <summary>The authoritative tenant stream served through the EventStore gateway range-read contract.</summary>
internal sealed class TenantHistoryGateway : IEventStoreGatewayClient {
    private readonly Lock _gate = new();
    private readonly Dictionary<string, SortedDictionary<long, StreamReadEvent>> _streams = new(StringComparer.Ordinal);

    public bool Unavailable { get; set; }

    public int Reads { get; private set; }

    public void Append(EventStoreDomainEventEnvelope envelope) {
        ArgumentNullException.ThrowIfNull(envelope);
        lock (_gate) {
            if (!_streams.TryGetValue(envelope.AggregateId, out SortedDictionary<long, StreamReadEvent>? stream)) {
                stream = [];
                _streams[envelope.AggregateId] = stream;
            }

            stream[envelope.SequenceNumber] = new StreamReadEvent(
                envelope.SequenceNumber,
                envelope.EventTypeName,
                envelope.Payload,
                envelope.SerializationFormat,
                1,
                envelope.MessageId,
                envelope.CorrelationId,
                envelope.CausationId,
                envelope.Timestamp,
                envelope.UserId);
        }
    }

    public void ReplaceHistoryMetadata(string aggregateId, long sequence, string serializationFormat, int? payloadVersion) {
        lock (_gate) {
            StreamReadEvent original = _streams[aggregateId][sequence];
            _streams[aggregateId][sequence] = original with {
                SerializationFormat = serializationFormat,
                PayloadVersion = payloadVersion,
            };
        }
    }

    public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        // Like the real gateway, the retained system/tenants history is requested through the explicit platform
        // scope and answered with the storage identity; any other stream identity is a defect.
        if (request.Tenant != "platform:tenants" || request.Domain != "tenants" || string.IsNullOrWhiteSpace(request.AggregateId)) {
            throw new InvalidOperationException($"Unexpected history stream {request.Tenant}/{request.Domain}/{request.AggregateId}.");
        }

        lock (_gate) {
            Reads++;
            if (Unavailable) {
                throw new EventStoreGatewayException(503, "Synthetic history outage");
            }

            StreamReadEvent[] events = _streams.TryGetValue(request.AggregateId ?? string.Empty, out SortedDictionary<long, StreamReadEvent>? stream)
                ? [.. stream.Values.Where(e => e.SequenceNumber > request.FromSequence && e.SequenceNumber <= (request.ToSequence ?? long.MaxValue))]
                : [];
            return Task.FromResult(new StreamReadPage(
                "system",
                request.Domain,
                request.AggregateId,
                events,
                new StreamReadMetadata(request.FromSequence, request.ToSequence, events.Length == 0 ? null : events[^1].SequenceNumber,
                    stream?.Keys.DefaultIfEmpty(0).Max() ?? 0, events.Length, false, null)));
        }
    }

    public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<EventStoreQueryResult> SubmitQueryAsync(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(SubmitQueryRequest request, string? ifNoneMatch = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
