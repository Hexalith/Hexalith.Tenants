namespace Hexalith.Tenants.Client.Subscription;

/// <summary>
/// Wire-format DTO for tenant events received via DAPR pub/sub.
/// Matches the flat EventEnvelope published by EventStore's EventPublisher.
/// Only the fields needed by consuming services are declared; JSON deserialization ignores extras.
/// </summary>
/// <param name="MessageId">The unique event message ID (ULID) for idempotency.</param>
/// <param name="AggregateId">The aggregate ID — for tenant events this is the managed tenant ID.</param>
/// <param name="TenantId">The tenant scope (always "system" for tenant management events).</param>
/// <param name="EventTypeName">The fully qualified .NET type name of the event payload.</param>
/// <param name="SequenceNumber">The event sequence number within the aggregate.</param>
/// <param name="Timestamp">When the event was persisted.</param>
/// <param name="CorrelationId">The request correlation ID for tracing.</param>
/// <param name="SerializationFormat">The serialization format (always "json").</param>
/// <param name="Payload">The JSON-serialized event payload bytes.</param>
public record TenantEventEnvelope(
    string MessageId,
    string AggregateId,
    string TenantId,
    string EventTypeName,
    long SequenceNumber,
    DateTimeOffset Timestamp,
    string CorrelationId,
    string SerializationFormat,
    byte[] Payload);
