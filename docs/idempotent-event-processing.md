# Idempotent Event Processing

## Why Idempotency Matters

DAPR pub/sub guarantees **at-least-once delivery**, not exactly-once. Network retries, sidecar restarts, and redelivery can cause the same event to arrive multiple times at your consuming service.

Without idempotency protection, duplicate events cause incorrect state: a user added twice, a counter incremented twice, or a notification sent twice.

With the current EventStore package, `MapEventStoreDomainEvents()` acknowledges completed deliveries, duplicates and deliberately skipped events. It also acknowledges a terminal `FailedInvalidPayload` result. In-progress deliveries, retryable capability mismatches and thrown handlers return an error so DAPR can redeliver.

For the larger timing window around command status, publication, subscriber delivery, and local projection lag, see [Cross-Aggregate Timing](cross-aggregate-timing.md).

## How Hexalith.Tenants.Client Handles It

`EventStoreDomainEventProcessor` uses `IEventStoreDomainEventMarkerStore` keyed by the persisted event's `MessageId`. In the default in-memory store, completed markers suppress redelivery within a process. An in-progress claim keeps a concurrent duplicate retryable. A thrown handler leaves the delivery eligible for retry, so a handler that already succeeded on that delivery may run again.

The default marker store resets on restart. Consumers that need shared delivery markers can register `AddDaprEventStoreDomainEventMarkerStore()` and configure `EventStoreDomainEventsOptions.MarkerStateStoreName` to name a consumer-owned component. A marker for a dispatched delivery permits completion to resume after a completion-write failure. Ordering and recovery for tenant state rely on the separate projection checkpoint described below.

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

## Local Projection Semantics

The built-in `TenantProjectionEventHandler` applies membership, tenant lifecycle and configuration events. The default `InMemoryTenantProjectionStore` is for demos and process-local reads. Applying a duplicate dictionary removal is safe, but ordering matters. If an unrelated update at sequence 4 arrives ahead of a membership removal at sequence 3, the default handler can advance to 4 and skip the removal. A store that simply saves each new state has the same ordering risk.

For membership-based authorization use the checkpointed registration in the current package. Configure the EventStore gateway with the consumer's history-read credentials and endpoint. Provision a shared Dapr read-model state store with optimistic concurrency and grant the consumer access to it. For example, once the gateway is configured:

```csharp
builder.Services.AddHexalithTenants()
    .AddHexalithTenantsDurableProjection("consumer-state");
```

Inject `ITenantProjectionSnapshotStore` into the consuming policy. Its presence makes the checkpointed dependency explicit. For example, a service capability available to tenant owners can evaluate a server-selected tenant and the authenticated caller's subject:

```csharp
TenantProjectionSnapshot? snapshot = await snapshots.GetSnapshotAsync(tenantId, cancellationToken);
return snapshot is { IsHealthy: true, State.Status: TenantStatus.Active }
    && snapshot.State.Members.TryGetValue(subjectId, out TenantRole role)
    && role == TenantRole.TenantOwner;
```

During a detected gap, readers receive no usable state. The checkpointed handler reads the missing events through `IEventStoreGatewayClient` and persists the recovered state and sequence before acknowledging recovery. If history is unavailable, let delivery fail and retry. If the policy cannot read the store, deny or return a temporary service error before performing protected work. A healthy checkpoint covers the observed stream prefix. When a decision must include a known change, require the snapshot to reach that tenant stream sequence.

Each consuming service processes events independently. DAPR pub/sub is at-least-once, and subscribers can lag or recover at different times, so consumers must not assume cross-service ordering or immediate read-after-write visibility. `TenantDisabled` and `TenantEnabled` should be treated as eventually consistent availability signals, and tenant configuration reads should be filtered by the consumer-owned dot-delimited prefix, such as `sample.` or `billing.`, so unrelated namespaces remain hidden unless explicitly handled.

`TenantProjectionEventHandler` records bounded `TenantLocalState.LastEvent` metadata from `EventStoreDomainEventContext`: the last message ID, aggregate-local sequence number, timestamp, and correlation ID. This is diagnostic metadata for the last successfully applied event in that local projection. It is not a durable audit log and is not enough by itself for scaled-out deduplication.

`SequenceNumber` is aggregate-local ordering metadata. It can help reason about order within one tenant aggregate stream, but it must not be treated as a global order across services, tenants, aggregates, topics, subscriber instances, or redelivery attempts. Use `MessageId` for duplicate detection and use the aggregate-local sequence number only inside the documented scope of one aggregate stream.

## Production Considerations

The default marker store retains completed IDs for the life of the process. Size durable marker retention for the redelivery window and ensure repeated older deliveries remain safe. When a handler writes an outbox record, commit that record with a unique `MessageId` for the operation. For an external call, pass an idempotency key the destination honors. A crash between making the call and writing its completion marker can otherwise repeat the side effect.

See the [Event Contract Reference](event-contract-reference.md) for envelope metadata and the [Quickstart](quickstart.md#consume-tenant-events-in-your-service) for the sample-consuming-service registration path.
