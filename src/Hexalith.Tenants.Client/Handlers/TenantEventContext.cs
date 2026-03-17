namespace Hexalith.Tenants.Client.Handlers;

/// <summary>
/// Context for tenant event processing, providing envelope metadata to handlers.
/// </summary>
/// <param name="TenantId">The managed tenant ID (from AggregateId, not envelope TenantId).</param>
/// <param name="MessageId">The unique event message ID (ULID) for idempotency.</param>
/// <param name="SequenceNumber">The event sequence number within the aggregate.</param>
/// <param name="Timestamp">When the event was persisted.</param>
/// <param name="CorrelationId">The request correlation ID for tracing.</param>
public record TenantEventContext(
    string TenantId,
    string MessageId,
    long SequenceNumber,
    DateTimeOffset Timestamp,
    string CorrelationId);
