# Idempotent Event Processing

## Why Idempotency Matters

DAPR pub/sub guarantees **at-least-once delivery**, not exactly-once. Network retries, sidecar restarts, and redelivery can cause the same event to arrive multiple times at your consuming service.

Without idempotency protection, duplicate events cause incorrect state: a user added twice, a counter incremented twice, or a notification sent twice.

Subscriber endpoints must return success only after the event has been handled safely. `MapEventStoreDomainEvents()` returns `200 OK` for processed, duplicate, unknown, or intentionally unhandled events, but returns a server error for invalid payloads so DAPR can redeliver according to the pub/sub component policy. If a handler throws, `EventStoreDomainEventProcessor` removes the in-progress `MessageId` claim and lets the exception escape; the failed delivery is not marked complete, so a corrected redelivery with the same `MessageId` can run.

For the larger timing window around command status, publication, subscriber delivery, and local projection lag, see [Cross-Aggregate Timing](cross-aggregate-timing.md).

## How Hexalith.Tenants.Client Handles It

`EventStoreDomainEventProcessor` tracks processed `MessageId` values in a `ConcurrentDictionary`. The `MessageId` is the event identifier set by EventStore at persistence time. When a duplicate event arrives, the processor returns `EventStoreDomainEventProcessingResult.Duplicate` and does not dispatch handlers again.

### Deduplication Flow

```csharp
// Inside EventStoreDomainEventProcessor.ProcessAsync():

if (!_processedMessageIds.TryAdd(envelope.MessageId, ProcessingState.InProgress))
{
    return EventStoreDomainEventProcessingResult.Duplicate;
}

try
{
    // Resolve event type, deserialize, validate TenantId, and dispatch handlers.
    _processedMessageIds[envelope.MessageId] = ProcessingState.Completed;
    return EventStoreDomainEventProcessingResult.Processed;
}
catch
{
    _ = _processedMessageIds.TryRemove(envelope.MessageId, out _);
    throw;
}
```

Key behaviors:

- The `TryAdd` call is atomic and thread-safe.
- If handler execution fails, the message ID is removed so the same delivery can be retried.
- Invalid payloads also remove the message ID so a corrected redelivery with the same ID is possible.
- Completed message IDs are kept by the default in-memory processor for the service lifetime.

### Copyable MessageId Deduplication

Production consumers that perform side effects outside the local projection should store a bounded deduplication record before executing those side effects. The store can be in memory for single-instance samples, but scaled-out services should use a shared implementation with expiration.

```csharp
using System.Collections.Concurrent;

using Hexalith.EventStore.Client.Subscriptions;

public sealed class MessageIdDeduplicationStore
{
    private readonly ConcurrentDictionary<string, byte> _processed = new(StringComparer.Ordinal);

    public bool TryClaim(EventStoreDomainEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _processed.TryAdd(context.MessageId, 0);
    }

    public void Abandon(EventStoreDomainEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _ = _processed.TryRemove(context.MessageId, out _);
    }
}
```

Use `TryClaim` before work that must not run twice, and call `Abandon` if the handler fails before the side effect is safe. The built-in `EventStoreDomainEventProcessor` already performs this message-level claim for registered tenant handlers; the explicit store pattern is for additional side-effect boundaries such as emails, webhooks, exports, or durable outbox writes.

## Making Handlers Idempotent

Even with message-level deduplication, handlers should be designed for idempotent application as defense-in-depth:

**Inherently idempotent operations:**

- Setting a dictionary value: `state.Members[userId] = role` produces the same state when repeated.
- Removing a key: `state.Members.Remove(userId)` is a no-op when the key is already absent.
- Assigning lifecycle state: `state.Status = TenantStatus.Disabled` produces the same state when repeated.
- Setting configuration: `state.Configuration[key] = value` replaces the same value when repeated.

**Operations that need care:**

- Incrementing counters: `count++` is not idempotent. Store the target value or guard the increment with a deduplication record.
- Sending notifications: emails, webhooks, and chat messages should be protected by external deduplication or an outbox.
- Appending to lists: `list.Add(item)` can produce duplicates. Use a set, a key, or check-before-add semantics.

Example idempotent access-revocation handler pattern:

```csharp
using Hexalith.EventStore.Client.Subscriptions;
using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Events;

public sealed class TenantAccessProjectionHandler : IEventStoreDomainEventHandler<UserRemovedFromTenant>
{
    private readonly ITenantProjectionStore _store;

    public TenantAccessProjectionHandler(ITenantProjectionStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public async Task HandleAsync(
        UserRemovedFromTenant @event,
        EventStoreDomainEventContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);

        // The managed tenant ID is the envelope AggregateId (context.TenantId is the publisher scope).
        TenantLocalState state = await _store.GetAsync(context.AggregateId, cancellationToken).ConfigureAwait(false)
            ?? new TenantLocalState { TenantId = context.AggregateId };

        _ = state.Members.Remove(@event.UserId);
        await _store.SaveAsync(state, cancellationToken).ConfigureAwait(false);
    }
}
```

The same pattern applies to grants and role changes: assign `state.Members[@event.UserId] = @event.Role` or `state.Members[@event.UserId] = @event.NewRole` so a duplicate payload produces the same final state. The built-in `TenantProjectionEventHandler` uses only dictionary set/remove operations and property assignment, making it naturally safe against duplicate delivery.

## Local Projection Semantics

`Hexalith.Tenants.Client` includes a built-in `TenantProjectionEventHandler` that maintains `TenantLocalState` through the consumer-facing `ITenantProjectionStore`. The default `InMemoryTenantProjectionStore` is useful for local development, samples, and single-instance consumers. Scaled-out services should provide their own durable `ITenantProjectionStore` implementation so all instances observe a consistent local projection without adding a Client package dependency on Redis, SQL, or another database.

The local projection is runtime state for the consuming service. It should be used for fast tenant-aware access, lifecycle, and configuration reactions inside that service, while EventStore remains the durable source of truth. Do not call back to the Tenants host synchronously for every access or configuration decision; subscribe to the shared `tenants.events` topic and filter by event type through typed handlers.

Each consuming service processes events independently. DAPR pub/sub is at-least-once, and subscribers can lag or recover at different times, so consumers must not assume cross-service ordering or immediate read-after-write visibility. `TenantDisabled` and `TenantEnabled` should be treated as eventually consistent availability signals, and tenant configuration reads should be filtered by the consumer-owned dot-delimited prefix, such as `sample.` or `billing.`, so unrelated namespaces remain hidden unless explicitly handled.

`TenantProjectionEventHandler` records bounded `TenantLocalState.LastEvent` metadata from `EventStoreDomainEventContext`: the last message ID, aggregate-local sequence number, timestamp, and correlation ID. This is diagnostic metadata for the last successfully applied event in that local projection. It is not a durable audit log and is not enough by itself for scaled-out deduplication.

`SequenceNumber` is aggregate-local ordering metadata. It can help reason about order within one tenant aggregate stream, but it must not be treated as a global order across services, tenants, aggregates, topics, subscriber instances, or redelivery attempts. Use `MessageId` for duplicate detection and use the aggregate-local sequence number only inside the documented scope of one aggregate stream.

## Production Considerations

The in-memory `ConcurrentDictionary` used by `EventStoreDomainEventProcessor` grows unboundedly and resets on service restart. This is acceptable for MVP and development but needs attention for production.

### Bounded Cache

Replace the dictionary with a bounded or time-windowed cache. Events older than the cache window that are redelivered may process again, so handlers still need idempotent operations.

### Shared Deduplication Store

For scaled-out services, use a shared deduplication store keyed by `MessageId`:

- Redis: `SET message:{id} 1 EX 3600 NX` for atomic set-if-not-exists with TTL.
- Database: insert into a `processed_messages` table with a unique constraint on `MessageId`.

Keep the store bounded by time or size so retained message IDs do not grow forever. The Client package intentionally does not take a Redis, SQL, Kafka, RabbitMQ, or broker-specific dependency; register a durable `ITenantProjectionStore` and shared deduplication mechanism in the consuming service when production topology requires it.

### Defense-in-Depth

Combine multiple layers for maximum reliability:

1. Message-level deduplication in `EventStoreDomainEventProcessor` catches duplicate deliveries before handlers run.
2. Handler-level idempotency keeps local projection updates safe when duplicate payloads are replayed.
3. Shared deduplication protects scaled-out instances and external side effects.

See the [Event Contract Reference](event-contract-reference.md) for envelope metadata and the [Quickstart](quickstart.md#consume-tenant-events-in-your-service) for the sample-consuming-service registration path.
