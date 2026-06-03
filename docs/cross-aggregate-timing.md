[Back to README](../README.md)

# Cross-Aggregate Timing

Tenant commands, EventStore status records, DAPR pub/sub, Tenants query projections, and consuming-service local projections do not all move at the same instant. This guide documents the timing boundaries so consuming services can make correct decisions while projections catch up.

Source anchors for this guide:

- `Hexalith.EventStore/docs/concepts/command-lifecycle.md`
- `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs`
- `src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs`
- `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`
- `samples/Hexalith.Tenants.Sample/Endpoints/AccessCheckEndpoints.cs`
- `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`
- `src/Hexalith.Tenants.AppHost/DaprComponents/resiliency.yaml`
- `deploy/dapr/pubsub.yaml`
- `deploy/dapr/resiliency.yaml`

## State Authority

| Stage | What happened | Authoritative state at this point |
|-------|---------------|------------------------------------|
| Command submission | The client sends `POST /api/v1/commands` to the EventStore command gateway. The HTTP response is `202 Accepted` with a status location. | The command is accepted for processing, not proven complete. Poll `GET /api/v1/commands/status/{correlationId}` for outcome. |
| Aggregate/domain result | `MediatR/SubmitCommandHandler` routes through the `AggregateActor`; the actor rehydrates state and calls the Tenants domain processor. | The aggregate decides success, rejection, or no-op from the current event-sourced state. Domain rejections are valid outcomes, not subscriber decisions. |
| Persisted event stream | EventStore writes resulting events to the EventStore state store and checkpoints `EventsStored`. | EventStore events are the source-of-truth write history. Persisted events are not rolled back because a subscriber is slow or unavailable. |
| EventStore command status | Status can move through `Received`, `Processing`, `EventsStored`, `EventsPublished`, and terminal states such as `Completed`, `Rejected`, `PublishFailed`, or `TimedOut`. | The command status proves command pipeline progress. `Completed` means EventStore persisted and published to pub/sub; it does not mean every subscriber projection has updated. |
| Tenants query projections | Tenants read models process stored events into query projections. | Tenants query projections are projection state. They can briefly lag behind the authoritative event stream. |
| DAPR pub/sub delivery | EventStore publishes CloudEvents to the shared `tenants.events` topic through DAPR pub/sub. | DAPR is the delivery mechanism. DAPR delivery is at-least-once, and subscriber processing is independent from command submission and command completion. |
| Consuming-service local projection | A service maps `MapEventStoreDomainEvents()`, receives `/tenants/events`, runs `EventStoreDomainEventProcessor`, dispatches `TenantProjectionEventHandler`, and saves through `ITenantProjectionStore`. | Consumer local projections are projection state owned by the consuming service. They can lag independently from Tenants query projections and from other services. |
| Aspire/log/trace diagnostics | Operators inspect resource health, command status, structured logs, traces, and projection metadata. | Diagnostics are evidence for support and recovery. They are not a replacement for the source-of-truth event history or command status states. |

## Propagation Flow

The authoritative persistence boundary is the event write to the EventStore state store. The eventual-consistency window begins after persistence and continues until each independent subscriber has delivered, processed, and saved its own projection update.

```mermaid
sequenceDiagram
    participant Client
    participant Gateway as EventStore command gateway
    participant Handler as MediatR/SubmitCommandHandler
    participant Actor as AggregateActor
    participant Domain as Tenants domain processor
    participant Store as EventStore state store
    participant Status as command status polling
    participant PubSub as DAPR pub/sub
    participant Endpoint as Sample/consumer endpoint
    participant Processor as EventStoreDomainEventProcessor
    participant ProjectionHandler as TenantProjectionEventHandler
    participant ProjectionStore as ITenantProjectionStore
    participant LocalRead as local access/configuration endpoint

    Client->>Gateway: POST /api/v1/commands
    Gateway->>Handler: submit command
    Handler->>Actor: route to aggregate actor
    Actor->>Domain: process command against current state
    Domain-->>Actor: DomainResult
    Actor->>Store: persist events
    Store-->>Actor: EventsStored
    Note over Store,Actor: authoritative persistence boundary
    Actor->>PubSub: publish CloudEvents to tenants.events
    Actor-->>Handler: EventsPublished / Completed / PublishFailed
    Handler-->>Gateway: accepted result payload
    Gateway-->>Client: 202 Accepted + Location

    Client->>Status: GET /api/v1/commands/status/{correlationId}
    Status-->>Client: EventsStored, EventsPublished, Completed, Rejected, PublishFailed

    Note over PubSub,ProjectionStore: eventual-consistency window
    PubSub--)Endpoint: deliver tenant event
    Endpoint->>Processor: /tenants/events via MapEventStoreDomainEvents()
    Processor->>ProjectionHandler: dispatch typed event
    ProjectionHandler->>ProjectionStore: SaveAsync(local projection)
    LocalRead->>ProjectionStore: GET /access/{tenantId}/{userId}

    alt PublishFailed after storage
        Actor-->>Status: PublishFailed
        Actor->>PubSub: republish persisted event during drain recovery
    end

    alt subscriber failure
        PubSub--)Endpoint: subscriber redelivery
        PubSub--)Endpoint: deadletter.tenants.events after retry/dead-letter policy
    end
```

Command status polling is separate from subscriber processing. `Completed` means the EventStore pipeline reached its terminal success state. It does not imply hidden rollback of persisted events, synchronous subscriber access enforcement, cross-service ordering, or simultaneous projection catch-up.

If multiple services subscribe to `tenants.events`, treat them as independent consumers of the same topic. One service can catch up before another, and each service must decide how to handle stale local state while it waits.

## Eventual Consistency Rules

EventStore events are the source-of-truth write history. Tenants query projections and consumer local projections are derived views and can lag independently.

DAPR delivery is at-least-once. Use idempotent handlers because a message can be redelivered after a timeout, process crash, publish retry, or subscriber retry. `EventStoreDomainEventProcessor` deduplicates by `MessageId` for the current process, removes failed in-progress claims so corrected redelivery can run, and skips unknown event types safely.

`SequenceNumber` is aggregate-local only. Use it for diagnostics within one aggregate stream. Consumers must not assume cross-service ordering, cross-tenant ordering, ordering across aggregate types, or matching observation time between services.

The built-in `TenantProjectionEventHandler` updates `TenantLocalState` through `ITenantProjectionStore` and records bounded `LastEvent` metadata: message ID, aggregate-local sequence number, timestamp, and correlation ID. This helps support diagnose lag, but it is not a durable audit log and it is not enough for scaled-out deduplication by itself.

## Security-Critical Decisions

Current MVP consumers must design for eventual consistency and fail closed. The sample `/access/{tenantId}/{userId}` endpoint reads only local projection state. If the tenant is missing, disabled, unknown, has no membership, or has an invalid role, the endpoint denies access instead of synchronously calling Tenants or EventStore.

For security-critical paths:

- deny or degrade safely when the local projection is missing or stale;
- use command status polling to prove the command pipeline outcome;
- use bounded retry/backoff when waiting for projection visibility;
- expose projection metadata to support so operators can see the last applied message and correlation ID;
- provide a local projection rebuild or catch-up procedure from the durable event history;
- avoid treating a fresh query projection as write authorization.

The planned EventStore authorization plugin is a future/optional synchronous pipeline enforcement path. It may eventually use a local authorization projection before domain service invocation, but it is not current behavior and should not be documented as required for MVP consumers.

## Failure And Recovery

`PublishFailed` means events were persisted but EventStore publication failed after the configured attempts. The persisted event stream remains authoritative. Drain recovery can republish the stored sequence range, and subscribers must handle duplicates.

Subscriber failure does not roll back the stored event. `MapEventStoreDomainEvents()` returns success for processed, duplicate, unknown, or intentionally unhandled events. Invalid payloads or thrown handlers return an error so DAPR can redeliver according to the pub/sub component and resiliency policy. The local and production `resiliency.yaml` files target the `pubsub` component with inbound retry before the dead-letter path, and the pub/sub component files configure the `deadletter.tenants.events` topic for deliveries that still cannot be processed. Keep those retry and dead-letter settings reviewed together.

When a local projection is stale:

- check command status first: `EventsStored`, `EventsPublished`, `Completed`, `Rejected`, and `PublishFailed` tell different stories;
- inspect subscriber health and DAPR pub/sub health;
- inspect structured logs and traces for the correlation ID, message ID, and event type;
- compare local projection metadata with the expected aggregate-local `SequenceNumber`;
- rebuild or catch up the local projection from the durable event stream when the service has fallen behind;
- keep support-safe diagnostics: do not record raw bearer tokens, decoded JWT payloads, secrets, complete serialized payloads, stack traces, or real tenant/user data.

Do not use `Thread.Sleep`, magic fixed-delay waits, or a hard-coded delay as a correctness mechanism. A delay can make a demo look stable while hiding a broken subscriber. Prefer status polling, bounded retry/backoff, projection metadata, health, log, and trace evidence, and explicit rebuild/catch-up procedures.

## Drift Checks

When architecture or event-flow implementation changes, update this guide and its sequence diagram in the same change. Re-check the EventStore command lifecycle docs, `CommandStatusController`, the Tenants Client subscription endpoint, `EventStoreDomainEventProcessor`, `TenantProjectionEventHandler`, the sample `/access/{tenantId}/{userId}` endpoint, and DAPR component/resiliency YAML before release.

Related guides:

- [Event Contract Reference](event-contract-reference.md)
- [Compensating Commands](compensating-commands.md)
- [Idempotent Event Processing](idempotent-event-processing.md)
- [Sample Consuming Service Walkthrough](sample-consuming-service-walkthrough.md)
- ["Aha Moment" Demo](demo.md)
