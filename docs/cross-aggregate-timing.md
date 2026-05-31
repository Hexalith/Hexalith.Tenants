[Back to README](../README.md)

# Cross-Aggregate Timing

How events propagate from command processing to consuming services, the timing window this creates, and how to design for eventual consistency.

## Timing Window

When a tenant command is processed (e.g., `RemoveUserFromTenant`), the event is stored **atomically** in the event store but delivered to subscribers **asynchronously** via DAPR pub/sub. The stored event stream is the source of truth; the pub/sub channel is a delivery mechanism that can lag or temporarily fail after storage succeeds. This creates a timing window where:

1. The Tenant aggregate has already applied the state change
2. Subscribing services have **not yet** received or processed the event
3. During this window, a subscribing service's local projection still reflects the old state

Under normal load, the propagation window is typically **50–200ms**. Under pub/sub backpressure, subscriber downtime, or pub/sub infrastructure outage, it can extend to low seconds or until recovery completes.

If pub/sub publication fails after the event has been stored, command processing does not roll back the event. Operators should expect a `PublishFailed` command status or structured warning/metric, followed by EventStore drain recovery republishing the persisted sequence range when pub/sub is available again. During that outage, downstream projections can be stale, but the aggregate event stream remains durable and can be used for projection catch-up.

Tenant query projections have the same eventual-consistency window. After `UserRemovedFromTenant` is accepted by the aggregate, a `get-user-tenants` self-lookup can briefly return the previous membership until the tenant index projection processes the removal event. That stale query result is read-only visibility: it does not grant command capability, does not override aggregate authorization, and does not allow writes against a disabled tenant or removed membership.

## Event Propagation Flow

The following diagram shows the consumer-facing event propagation flow. The synchronous boundary (atomic store + response) and asynchronous boundary (pub/sub delivery) define the timing window.

```mermaid
sequenceDiagram
    participant Client
    participant CommandApi
    participant EventStore as Event Store
    participant PubSub as DAPR Pub/Sub
    participant ServiceA as Service A
    participant ServiceB as Service B

    Client->>CommandApi: POST /api/v1/commands
    CommandApi->>EventStore: Store events atomically
    EventStore-->>CommandApi: Events persisted
    CommandApi-->>Client: 202 Accepted (correlationId)

    Note over Client,EventStore: Synchronous boundary — state committed

    EventStore--)PubSub: Publish events async
    Note over EventStore,PubSub: Timing window starts here

    PubSub--)ServiceA: Deliver event
    ServiceA->>ServiceA: Update local projection

    PubSub--)ServiceB: Deliver event
    ServiceB->>ServiceB: Update local projection

    Note over ServiceA,ServiceB: Timing window closes when all subscribers process
```

## Designing for Eventual Consistency

Subscribing services should treat tenant state as **eventually consistent**. Follow these guidelines:

**Event ordering guarantees:**

- **Within a single aggregate instance**, events are **stored** in strict order — the aggregate version (sequence number) is monotonically increasing. Note that DAPR pub/sub does not guarantee delivery order; events may be redelivered out of sequence. Consumers must resequence using `aggregateVersion`, not delivery order.
- **Across different aggregates** and **across different subscribing services**, there is **no ordering guarantee**. Do not assume events arrive in the same order across different services.

**Design handlers to be idempotent.** DAPR pub/sub guarantees at-least-once delivery, meaning events may arrive more than once. See [Idempotent Event Processing](idempotent-event-processing.md) for patterns.

**Treat stored events as authoritative.** Pub/sub recovery may redeliver an event after a temporary outage, and duplicate deliveries are possible. Consumers must deduplicate by stable event metadata such as message ID, aggregate identity, sequence number, and correlation ID. Do not infer that a missing pub/sub delivery means the command failed.

**Use the query endpoint for read-after-write confirmation.** When a consuming service needs to verify a command was processed before proceeding:

- Query `GET /api/tenants/{id}` to check the current tenant state
- Command responses include the aggregate ID for direct navigation
- Retry with a short backoff if the projection has not caught up

## Phase 2: Synchronous Enforcement

For security-critical scenarios where eventual consistency is insufficient (e.g., rejecting unauthorized commands before they reach any domain service), the **planned EventStore authorization plugin** provides a synchronous enforcement option.

The auth plugin will use a **local projection** of tenant-user-role state to reject unauthorized commands at the MediatR pipeline level, **before** they reach any domain service. This closes the timing window by providing synchronous permission checks.

> **Current status:** The auth plugin is planned for Phase 2. It does not exist yet.

**MVP approach:** Document the timing window, design consuming services for eventual consistency, and use the query endpoint for read-after-write verification when needed.
