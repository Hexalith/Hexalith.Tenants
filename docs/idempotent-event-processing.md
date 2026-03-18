# Idempotent Event Processing

## Why Idempotency Matters

DAPR pub/sub guarantees **at-least-once delivery**, NOT exactly-once. Network retries, sidecar restarts, and redelivery can cause the same event to arrive multiple times at your consuming service.

Without idempotency protection, duplicate events cause incorrect state: a user added twice, a counter incremented twice, or a notification sent twice.

## How Hexalith.Tenants.Client Handles It

`TenantEventProcessor` tracks processed `MessageId` values (ULID) in a `ConcurrentDictionary`. When a duplicate event arrives, it is silently skipped. The `MessageId` is a unique event identifier set by EventStore at persistence time.

### Deduplication Flow

```csharp
// Inside TenantEventProcessor.ProcessAsync():

// 1. Attempt to claim the message ID
if (!_processedMessageIds.TryAdd(envelope.MessageId, ProcessingState.InProgress))
{
    // Duplicate — already processed or in progress, skip silently
    return TenantEventProcessingResult.Duplicate;
}

// 2. Resolve event type, deserialize, dispatch to handlers...

// 3. Mark as completed on success
_processedMessageIds[envelope.MessageId] = ProcessingState.Completed;
```

Key behaviors:

- The `TryAdd` call is atomic and thread-safe
- If processing fails, the message ID is removed so it can be retried
- The `ProcessingState` enum distinguishes in-progress from completed events

## Making Handlers Idempotent

Even with message-level deduplication, handlers should be designed for idempotent application as defense-in-depth:

**Inherently idempotent operations:**

- Setting a dictionary value: `state.Members[userId] = role` — applying the same value twice produces the same state
- Removing a key: `state.Members.Remove(userId)` — removing an already-absent key is a no-op
- Setting a status: `state.Status = TenantStatus.Disabled` — same result regardless of how many times applied

**Operations that need care:**

- Incrementing counters: `count++` is NOT idempotent — use "set to value" instead of "add to value"
- Sending notifications: emails, webhooks, and Slack messages should be guarded by external deduplication
- Appending to lists: `list.Add(item)` can produce duplicates — use a set or check-before-add pattern

Example idempotent handler pattern:

```csharp
public async Task HandleAsync(UserAddedToTenant @event, TenantEventContext context, CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(@event);
    ArgumentNullException.ThrowIfNull(context);

    TenantLocalState state = await _store.GetAsync(context.TenantId, cancellationToken).ConfigureAwait(false)
        ?? new TenantLocalState { TenantId = context.TenantId };

    state.Members[@event.UserId] = @event.Role;
    await _store.SaveAsync(state, cancellationToken).ConfigureAwait(false);
}
```

The built-in `TenantProjectionEventHandler` uses only idempotent operations (dictionary set/remove, property assignment), making it naturally safe against duplicate delivery.

## Production Considerations

The in-memory `ConcurrentDictionary` used by `TenantEventProcessor` grows unboundedly and resets on service restart. This is acceptable for MVP and development but needs attention for production:

### Bounded LRU Cache

Replace the dictionary with a time-bounded cache. Events older than the cache window that are redelivered will be processed again, but idempotent handlers ensure correctness:

```csharp
// Example: Use MemoryCache with sliding expiration
var cacheOptions = new MemoryCacheEntryOptions
{
    SlidingExpiration = TimeSpan.FromHours(1),
};
```

### External Deduplication Store

For scaled-out services (multiple instances), use a shared deduplication store keyed by `MessageId`:

- **Redis**: `SET message:{id} 1 EX 3600 NX` — atomic set-if-not-exists with TTL
- **Database**: Insert into a `processed_messages` table with a unique constraint on `MessageId`

### Defense-in-Depth

Combine multiple layers for maximum reliability:

1. **Message-level deduplication** (TenantEventProcessor) — catches most duplicates
2. **Handler-level idempotency** (set-based operations) — safe even if deduplication misses
3. **External deduplication** (Redis/database) — survives restarts and works across instances
