# Story 4.2: Event Subscription & Local Projection Pattern

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer building a consuming service,
I want to subscribe to tenant events via DAPR pub/sub and build a local projection of tenant state,
So that my service can reactively enforce access and respond to tenant changes.

## Acceptance Criteria

1. **Given** a consuming service is subscribed to the `system.tenants.events` DAPR pub/sub topic
   **When** a UserAddedToTenant event is published
   **Then** the consuming service receives the event and can update its local projection of tenant membership

2. **Given** a consuming service is subscribed to tenant events
   **When** a UserRemovedFromTenant event is published
   **Then** the consuming service can revoke access for the removed user in its local projection

3. **Given** a consuming service is subscribed to tenant events
   **When** a TenantDisabled event is published
   **Then** the consuming service can block operations for the disabled tenant

4. **Given** a consuming service is subscribed to tenant events
   **When** a TenantConfigurationSet event is published
   **Then** the consuming service can update tenant-specific behavior based on the configuration change

5. **Given** event contracts include event ID and aggregate version (FR41)
   **When** a consuming service receives a duplicate event (DAPR at-least-once delivery)
   **Then** the service can detect the duplicate via event ID and skip reprocessing

6. **Given** a consuming service builds a local projection from tenant events
   **When** multiple events arrive for different tenants
   **Then** each tenant's projection is maintained independently with no cross-tenant data leakage

## Tasks / Subtasks

- [x] Task 1: Create `ITenantEventHandler<TEvent>` interface (AC: #1, #2, #3, #4)
    - [x]1.1: Create `src/Hexalith.Tenants.Client/Handlers/ITenantEventHandler.cs`
    - [x]1.2: Generic interface constrained to `IEventPayload`, with `HandleAsync(TEvent, TenantEventContext, CancellationToken)` method
    - [x]1.3: Create `src/Hexalith.Tenants.Client/Handlers/TenantEventContext.cs` — context record with TenantId, MessageId, SequenceNumber
    - [x]1.4: Verify solution builds: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 2: Create `TenantLocalState` read model and projection store (AC: #1, #6)
    - [x]2.1: Create `src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs` — per-tenant read model class
    - [x]2.2: Properties: TenantId, Name, Description, Status (TenantStatus), Members (Dictionary UserId→TenantRole), Configuration (Dictionary Key→Value)
    - [x]2.3: Create `src/Hexalith.Tenants.Client/Projections/ITenantProjectionStore.cs` — Get/Save abstraction
    - [x]2.4: Create `src/Hexalith.Tenants.Client/Projections/InMemoryTenantProjectionStore.cs` — ConcurrentDictionary implementation
    - [x]2.5: Verify solution builds

- [x] Task 3: Create `TenantProjectionEventHandler` — built-in handler applying events to TenantLocalState (AC: #1, #2, #3, #4)
    - [x]3.1: Create `src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs`
    - [x]3.2: Implement `ITenantEventHandler<T>` for: TenantCreated, TenantUpdated, TenantDisabled, TenantEnabled, UserAddedToTenant, UserRemovedFromTenant, UserRoleChanged, TenantConfigurationSet, TenantConfigurationRemoved
    - [x]3.3: Each handler reads state from ITenantProjectionStore, applies the event, saves updated state
    - [x]3.4: Verify solution builds

- [x] Task 4: Create `TenantEventProcessor` — event dispatch orchestrator (AC: #1, #5)
    - [x]4.1: Create `src/Hexalith.Tenants.Client/Subscription/TenantEventEnvelope.cs` — wire-format DTO matching EventStore's published EventEnvelope
    - [x]4.2: Create `src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs` — receives envelope, resolves type, deserializes payload, deduplicates by MessageId, dispatches to handlers
    - [x]4.3: Event type resolution via assembly scanning of Contracts at startup (IEventPayload implementations)
    - [x]4.4: Idempotency via ConcurrentDictionary tracking processed MessageIds
    - [x]4.5: Verify solution builds

- [x] Task 5: Create `MapTenantEventSubscription()` endpoint mapping (AC: #1)
    - [x]5.1: Create `src/Hexalith.Tenants.Client/Subscription/TenantEventSubscriptionEndpoints.cs`
    - [x]5.2: Extension method on `IEndpointRouteBuilder` mapping DAPR pub/sub subscription for tenant events
    - [x]5.3: Add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to Client.csproj
    - [x]5.4: Verify solution builds

- [x] Task 6: Update `AddHexalithTenants()` DI registration (AC: #1, #6)
    - [x]6.1: Register `ITenantProjectionStore` → `InMemoryTenantProjectionStore` (Singleton)
    - [x]6.2: Register `TenantProjectionEventHandler` for each supported event type
    - [x]6.3: Register `TenantEventProcessor` (Singleton)
    - [x]6.4: Build event type registry from Contracts assembly
    - [x]6.5: Maintain idempotency — skip registrations if already present
    - [x]6.6: Verify solution builds

- [x] Task 7: Create unit tests (AC: all)
    - [x]7.1: Create `tests/Hexalith.Tenants.Client.Tests/Handlers/TenantProjectionEventHandlerTests.cs` — test each event type application
    - [x]7.2: Create `tests/Hexalith.Tenants.Client.Tests/Projections/InMemoryTenantProjectionStoreTests.cs` — CRUD + isolation tests
    - [x]7.3: Create `tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs` — dispatch + idempotency tests
    - [x]7.4: Update DI registration tests to verify new service registrations
    - [x]7.5: Verify all tests pass: `dotnet test Hexalith.Tenants.slnx` — no regressions
    - [x]7.6: Final build: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors

## Dev Notes

### Scope: Event Consumption Infrastructure

This story adds event handler abstractions, local projection infrastructure, and DAPR subscription support to the Client package. Story 4.1 created the DI foundation (DaprClient + HexalithTenantsOptions). This story adds the behavior layer. Combined, they provide consuming services with a complete integration package.

**This story does NOT:**

- Create a sample consuming service (Story 4.3)
- Create server-side projections (Epic 5)
- Modify CommandApi or Server projects
- Add event publishing logic (already handled by EventStore)

### Current State: Client Has DI Foundation

Story 4.1 created:

- `Configuration/HexalithTenantsOptions.cs` — PubSubName, TopicName, CommandApiAppId
- `Registration/TenantServiceCollectionExtensions.cs` — `AddHexalithTenants()` with DaprClient + options binding
- Client.csproj references: `Dapr.AspNetCore`, `Microsoft.Extensions.Configuration.Binder`, `Microsoft.Extensions.Hosting.Abstractions`, `Hexalith.Tenants.Contracts`
- 12 unit tests in Client.Tests

Story 4.2 extends this foundation. The `AddHexalithTenants()` extension method gains new registrations. Existing tests must continue to pass.

### Wire Format: EventStore's Published EventEnvelope

EventStore publishes events via DAPR pub/sub. The published data is a **flat EventEnvelope** (Server format) containing 17 fields. The consuming service subscription endpoint receives this as DAPR CloudEvent data.

**Critical fields for consuming services:**

| Field                 | Type           | Purpose                                                                                    |
| --------------------- | -------------- | ------------------------------------------------------------------------------------------ |
| `MessageId`           | string (ULID)  | Unique event ID — **use for idempotency/deduplication** (AC5, FR41)                        |
| `AggregateId`         | string         | Aggregate ID — for tenant events this IS the TenantId                                      |
| `TenantId`            | string         | The tenant scope (= "system" for all tenant events — the managed tenant is in AggregateId) |
| `EventTypeName`       | string         | Fully qualified .NET type name — **use for event type resolution**                         |
| `SequenceNumber`      | long           | Event sequence within aggregate — **use for ordering verification**                        |
| `Payload`             | byte[]         | JSON-serialized event payload (the actual IEventPayload record)                            |
| `SerializationFormat` | string         | Always "json"                                                                              |
| `Timestamp`           | DateTimeOffset | When the event was persisted                                                               |
| `CorrelationId`       | string         | Request tracing ID                                                                         |

**Wire format DTO — match the published shape:**

```csharp
// src/Hexalith.Tenants.Client/Subscription/TenantEventEnvelope.cs
using System.Text.Json.Serialization;

namespace Hexalith.Tenants.Client.Subscription;

/// <summary>
/// Wire-format DTO for tenant events received via DAPR pub/sub.
/// Matches the flat EventEnvelope published by EventStore's EventPublisher.
/// Only the fields needed by consuming services are declared; JSON deserialization ignores extras.
/// </summary>
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
```

**IMPORTANT — TenantId vs AggregateId:** For tenant domain events, `TenantId` in the envelope is `"system"` (the platform tenant that owns the tenant management domain). The **managed tenant** (e.g., "acme-corp") is in `AggregateId`. When building per-tenant projections, key by `AggregateId`, NOT `TenantId`. This is also confirmed by the event payloads — all events have a `TenantId` property in their payload record (e.g., `TenantCreated.TenantId`) which matches `AggregateId`.

### Event Type Resolution

The `EventTypeName` field is the fully qualified .NET type name (e.g., `"Hexalith.Tenants.Contracts.Events.TenantCreated"`). To deserialize the `Payload` bytes to the correct type:

1. At startup, scan the `Hexalith.Tenants.Contracts` assembly for all types implementing `IEventPayload`
2. Build a `Dictionary<string, Type>` mapping `FullName → Type`
3. When processing an event: look up `EventTypeName` in the dictionary, deserialize `Payload` via `System.Text.Json`

```csharp
// Type registry construction (in AddHexalithTenants or TenantEventProcessor constructor)
var eventTypes = typeof(TenantCreated).Assembly
    .GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract && typeof(IEventPayload).IsAssignableFrom(t))
    .ToDictionary(t => t.FullName!, t => t);
```

**IMPORTANT:** Use `Type.FullName` for matching (not `Name`), because `EventTypeName` is the fully qualified name. Handle the case where the type is not found (unknown event type) — log a warning and skip, do not throw.

### ITenantEventHandler<TEvent> Interface

```csharp
// src/Hexalith.Tenants.Client/Handlers/ITenantEventHandler.cs
using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Tenants.Client.Handlers;

/// <summary>
/// Handles a specific tenant event type in a consuming service.
/// </summary>
/// <typeparam name="TEvent">The event payload type.</typeparam>
public interface ITenantEventHandler<in TEvent>
    where TEvent : IEventPayload
{
    Task HandleAsync(TEvent @event, TenantEventContext context, CancellationToken cancellationToken = default);
}
```

```csharp
// src/Hexalith.Tenants.Client/Handlers/TenantEventContext.cs
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
```

### TenantLocalState Read Model

```csharp
// src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.Client.Projections;

/// <summary>
/// Per-tenant read model built from tenant event stream.
/// Consuming services use this to enforce access and react to tenant changes.
/// </summary>
public class TenantLocalState
{
    public string TenantId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TenantStatus Status { get; set; } = TenantStatus.Active;

    public Dictionary<string, TenantRole> Members { get; init; } = [];

    public Dictionary<string, string> Configuration { get; init; } = [];
}
```

**Design notes:**

- `Members` maps UserId → TenantRole. Consuming services check `state.Members.ContainsKey(userId)` for membership and `state.Members[userId]` for role.
- `Configuration` maps Key → Value. Consuming services check `state.Configuration.TryGetValue("billing.plan", out var plan)` for behavior customization.
- `Status` is `TenantStatus.Active` or `TenantStatus.Disabled`. Consuming services check `state.Status == TenantStatus.Disabled` to block operations.
- `Members` and `Configuration` use `init` accessor with `[]` initializer — the collections are mutable (add/remove entries) but the dictionary reference is set once.

### ITenantProjectionStore and InMemoryTenantProjectionStore

```csharp
// src/Hexalith.Tenants.Client/Projections/ITenantProjectionStore.cs
namespace Hexalith.Tenants.Client.Projections;

/// <summary>
/// Abstraction for persisting and retrieving per-tenant local projections.
/// </summary>
public interface ITenantProjectionStore
{
    Task<TenantLocalState?> GetAsync(string tenantId, CancellationToken cancellationToken = default);

    Task SaveAsync(TenantLocalState state, CancellationToken cancellationToken = default);
}
```

```csharp
// src/Hexalith.Tenants.Client/Projections/InMemoryTenantProjectionStore.cs
using System.Collections.Concurrent;

namespace Hexalith.Tenants.Client.Projections;

/// <summary>
/// Thread-safe in-memory implementation of ITenantProjectionStore.
/// Suitable for single-instance services. For scaled-out services,
/// consumers should implement ITenantProjectionStore against a durable store (DAPR state store, database, etc.).
/// </summary>
public class InMemoryTenantProjectionStore : ITenantProjectionStore
{
    private readonly ConcurrentDictionary<string, TenantLocalState> _states = new(StringComparer.Ordinal);

    public Task<TenantLocalState?> GetAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        _ = _states.TryGetValue(tenantId, out TenantLocalState? state);
        return Task.FromResult(state);
    }

    public Task SaveAsync(TenantLocalState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(state.TenantId);
        _states[state.TenantId] = state;
        return Task.CompletedTask;
    }
}
```

**Per-tenant isolation (AC6):** Each tenant has its own `TenantLocalState` keyed by `TenantId` in the `ConcurrentDictionary`. No shared state between tenants. Thread safety is guaranteed by `ConcurrentDictionary`.

### TenantProjectionEventHandler

This is the built-in handler that applies events to `TenantLocalState`. It implements `ITenantEventHandler<T>` for each event type.

```csharp
// src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs
namespace Hexalith.Tenants.Client.Handlers;

public class TenantProjectionEventHandler :
    ITenantEventHandler<TenantCreated>,
    ITenantEventHandler<TenantUpdated>,
    ITenantEventHandler<TenantDisabled>,
    ITenantEventHandler<TenantEnabled>,
    ITenantEventHandler<UserAddedToTenant>,
    ITenantEventHandler<UserRemovedFromTenant>,
    ITenantEventHandler<UserRoleChanged>,
    ITenantEventHandler<TenantConfigurationSet>,
    ITenantEventHandler<TenantConfigurationRemoved>
{
    private readonly ITenantProjectionStore _store;

    public TenantProjectionEventHandler(ITenantProjectionStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    // Each HandleAsync method: get-or-create state, apply event, save.
    // Example for UserAddedToTenant:
    public async Task HandleAsync(UserAddedToTenant @event, TenantEventContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);
        TenantLocalState state = await GetOrCreateStateAsync(context.TenantId, cancellationToken).ConfigureAwait(false);
        state.Members[@event.UserId] = @event.Role;
        await _store.SaveAsync(state, cancellationToken).ConfigureAwait(false);
    }

    // Pattern for all handlers: get-or-create → mutate → save.
    // TenantCreated: set Name, Description, Status=Active.
    // TenantUpdated: set Name, Description.
    // TenantDisabled: set Status=Disabled.
    // TenantEnabled: set Status=Active.
    // UserRemovedFromTenant: state.Members.Remove(UserId).
    // UserRoleChanged: state.Members[UserId] = NewRole.
    // TenantConfigurationSet: state.Configuration[Key] = Value.
    // TenantConfigurationRemoved: state.Configuration.Remove(Key).

    private async Task<TenantLocalState> GetOrCreateStateAsync(string tenantId, CancellationToken cancellationToken)
    {
        TenantLocalState? state = await _store.GetAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (state is null)
        {
            state = new TenantLocalState { TenantId = tenantId };
        }

        return state;
    }
}
```

**IMPORTANT — Implement ALL 9 handlers.** Not just the 4 mentioned in the ACs. The local projection must handle all tenant events to maintain correct state. A consuming service that only receives TenantDisabled but misses TenantCreated would have an inconsistent projection.

### TenantEventProcessor — Event Dispatch Orchestrator

```csharp
// src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs
namespace Hexalith.Tenants.Client.Subscription;

public class TenantEventProcessor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyDictionary<string, Type> _eventTypeRegistry;
    private readonly ConcurrentDictionary<string, byte> _processedMessageIds;

    public TenantEventProcessor(
        IServiceProvider serviceProvider,
        IReadOnlyDictionary<string, Type> eventTypeRegistry)
    {
        _serviceProvider = serviceProvider;
        _eventTypeRegistry = eventTypeRegistry;
        _processedMessageIds = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
    }

    public async Task<bool> ProcessAsync(TenantEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        // 1. Idempotency check (AC5)
        if (!_processedMessageIds.TryAdd(envelope.MessageId, 0))
        {
            return false; // Already processed
        }

        // 2. Resolve event type
        if (!_eventTypeRegistry.TryGetValue(envelope.EventTypeName, out Type? eventType))
        {
            return false; // Unknown event type — skip
        }

        // 3. Deserialize payload
        IEventPayload @event = (IEventPayload)JsonSerializer.Deserialize(envelope.Payload, eventType)!;

        // 4. Build context (use AggregateId as managed tenant ID)
        var context = new TenantEventContext(
            envelope.AggregateId,
            envelope.MessageId,
            envelope.SequenceNumber,
            envelope.Timestamp,
            envelope.CorrelationId);

        // 5. Dispatch to all registered ITenantEventHandler<TEvent> implementations
        // Use reflection or IServiceProvider to resolve handlers for the concrete event type
        // Pattern: typeof(ITenantEventHandler<>).MakeGenericType(eventType)
        Type handlerType = typeof(ITenantEventHandler<>).MakeGenericType(eventType);
        IEnumerable<object?> handlers = _serviceProvider.GetServices(handlerType);
        foreach (object? handler in handlers)
        {
            if (handler is null) continue;
            // Invoke HandleAsync via reflection or a dispatch helper
        }

        return true;
    }
}
```

**IMPORTANT — Reflection dispatch:** The `ProcessAsync` method needs to call `HandleAsync` on each handler via reflection since the event type is only known at runtime. Use `MethodInfo.Invoke` or a compiled delegate cache for performance. The dev agent should choose the cleanest approach.

**IMPORTANT — Idempotency memory management (AC5):** The `ConcurrentDictionary<string, byte>` grows unboundedly. For MVP, this is acceptable (consuming services are typically restarted periodically). Add a comment noting that production deployments should consider a bounded LRU cache or external deduplication store. Do NOT implement a complex eviction strategy — that's premature optimization.

### MapTenantEventSubscription — Endpoint Mapping

```csharp
// src/Hexalith.Tenants.Client/Subscription/TenantEventSubscriptionEndpoints.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hexalith.Tenants.Client.Subscription;

/// <summary>
/// Maps DAPR pub/sub subscription endpoint for tenant events.
/// </summary>
public static class TenantEventSubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapTenantEventSubscription(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Map a POST endpoint that DAPR calls when tenant events arrive.
        // The [Topic] attribute or Dapr.MapSubscribeHandler() integration
        // routes events from the configured pub/sub topic to this endpoint.

        // Get topic configuration from HexalithTenantsOptions
        // Endpoint receives TenantEventEnvelope, delegates to TenantEventProcessor

        return endpoints;
    }
}
```

**DAPR subscription integration options (dev agent must choose one):**

- **Option A (Minimal API + Dapr.AspNetCore):** Use `endpoints.MapPost("/tenants/events", handler).WithTopic(pubsubName, topicName)` if DAPR SDK 1.17.3 supports `WithTopic()` on minimal API
- **Option B (Controller):** Create a small controller class with `[Topic("pubsub", "system.tenants.events")]` attribute
- **Option C (Programmatic subscription):** Use `DaprClient.SubscribeToTopicAsync()` if available

Research the DAPR .NET SDK 1.17.3 API to determine which approach works. The key requirement: consuming services call `app.MapTenantEventSubscription()` and the endpoint automatically subscribes to the correct topic.

**IMPORTANT:** The topic name should come from `HexalithTenantsOptions.TopicName` (resolved from DI) and the pub/sub name from `HexalithTenantsOptions.PubSubName`. Do NOT hardcode these — use IOptions<HexalithTenantsOptions> from the service provider.

### Updated AddHexalithTenants() Registration

The existing `AddHexalithTenants()` must be extended (not replaced). Keep all existing registrations (DaprClient, options binding). Add new registrations **after** the existing ones.

```csharp
// In EnsureCoreRegistrations() or a new helper method:

// Register projection store (Singleton — shared across requests)
if (!services.Any(s => s.ServiceType == typeof(ITenantProjectionStore)))
{
    services.AddSingleton<ITenantProjectionStore, InMemoryTenantProjectionStore>();
}

// Register built-in projection handler for all 9 event types
services.AddSingleton<TenantProjectionEventHandler>();
RegisterEventHandler<TenantCreated, TenantProjectionEventHandler>(services);
RegisterEventHandler<TenantUpdated, TenantProjectionEventHandler>(services);
// ... (all 9 event types)

// Register event type registry (scan Contracts assembly)
if (!services.Any(s => s.ServiceType == typeof(IReadOnlyDictionary<string, Type>)
    && s.ServiceKey is "TenantEventTypes"))
{
    var registry = BuildEventTypeRegistry();
    services.AddSingleton(registry);
}

// Register event processor
services.AddSingleton<TenantEventProcessor>();
```

**IMPORTANT — Idempotency of new registrations:** Check if services are already registered before adding. Follow the existing pattern in `EnsureCoreRegistrations()`. The extension method must remain idempotent (calling twice has no side effects).

**IMPORTANT — Handler registration pattern:** Register `TenantProjectionEventHandler` both as itself (for internal use) and as `ITenantEventHandler<TEvent>` for each event type. Use keyed services or `IEnumerable<ITenantEventHandler<TEvent>>` resolution pattern so consuming services can add their own handlers alongside the built-in one.

```csharp
// Helper for registering event handlers
private static void RegisterEventHandler<TEvent, THandler>(IServiceCollection services)
    where TEvent : IEventPayload
    where THandler : class, ITenantEventHandler<TEvent>
{
    services.AddSingleton<ITenantEventHandler<TEvent>>(sp => sp.GetRequiredService<THandler>());
}
```

### Architecture Compliance

**Type Location Rules (MUST follow):**

| Type                              | Project | Folder        | File                                          |
| --------------------------------- | ------- | ------------- | --------------------------------------------- |
| ITenantEventHandler<TEvent>       | Client  | Handlers/     | ITenantEventHandler.cs (CREATE)               |
| TenantEventContext                | Client  | Handlers/     | TenantEventContext.cs (CREATE)                |
| TenantProjectionEventHandler      | Client  | Handlers/     | TenantProjectionEventHandler.cs (CREATE)      |
| TenantLocalState                  | Client  | Projections/  | TenantLocalState.cs (CREATE)                  |
| ITenantProjectionStore            | Client  | Projections/  | ITenantProjectionStore.cs (CREATE)            |
| InMemoryTenantProjectionStore     | Client  | Projections/  | InMemoryTenantProjectionStore.cs (CREATE)     |
| TenantEventEnvelope               | Client  | Subscription/ | TenantEventEnvelope.cs (CREATE)               |
| TenantEventProcessor              | Client  | Subscription/ | TenantEventProcessor.cs (CREATE)              |
| TenantEventSubscriptionEndpoints  | Client  | Subscription/ | TenantEventSubscriptionEndpoints.cs (CREATE)  |
| TenantServiceCollectionExtensions | Client  | Registration/ | TenantServiceCollectionExtensions.cs (MODIFY) |
| Hexalith.Tenants.Client.csproj    | Client  | /             | Hexalith.Tenants.Client.csproj (MODIFY)       |

**DO NOT:**

- Create types outside the Client or Client.Tests projects
- Modify the Server, CommandApi, or Contracts projects
- Add new NuGet packages to Client (all dependencies are already available)
- Create sample consuming service code (Story 4.3 scope)
- Create server-side projections (Epic 5 scope)
- Create InMemoryTenantService (Story 6.1 scope — Testing package)
- Break existing tests or DI registrations

### Library & Framework Requirements

**Source (Client) — Changes to csproj:**

Add ASP.NET Core FrameworkReference for `IEndpointRouteBuilder` and minimal API types:

```xml
<FrameworkReference Include="Microsoft.AspNetCore.App" />
```

This is required for `MapTenantEventSubscription()`. The Client package is already ASP.NET Core-coupled via `Dapr.AspNetCore`. Adding the FrameworkReference makes this explicit. Consuming services are ASP.NET Core web apps (they need DAPR sidecar + web endpoints).

**All other dependencies are already available:**

- `Dapr.AspNetCore` — DAPR pub/sub subscription attributes, `DaprClient`
- `Microsoft.Extensions.Configuration.Binder` — options binding
- `Microsoft.Extensions.Hosting.Abstractions` — `IHostApplicationBuilder`
- `Hexalith.Tenants.Contracts` — event types (IEventPayload implementations)
- `Hexalith.EventStore.Contracts` (transitive via Contracts) — `IEventPayload`, `IRejectionEvent`
- `System.Text.Json` (framework) — event payload deserialization

**Tests (Client.Tests) — No new packages required.**

All test dependencies from Story 4.1 remain:

- xUnit 2.9.3 via `tests/Directory.Build.props`
- Shouldly 4.3.0 via `tests/Directory.Build.props`
- Project references: Client, Testing

### File Structure Requirements

```
src/Hexalith.Tenants.Client/
├── Hexalith.Tenants.Client.csproj          (MODIFY — add FrameworkReference)
├── Configuration/
│   └── HexalithTenantsOptions.cs           (EXISTS — no changes)
├── Handlers/
│   ├── ITenantEventHandler.cs              (CREATE)
│   ├── TenantEventContext.cs               (CREATE)
│   └── TenantProjectionEventHandler.cs     (CREATE)
├── Projections/
│   ├── TenantLocalState.cs                 (CREATE)
│   ├── ITenantProjectionStore.cs           (CREATE)
│   └── InMemoryTenantProjectionStore.cs    (CREATE)
├── Registration/
│   └── TenantServiceCollectionExtensions.cs (MODIFY — add event handler registrations)
└── Subscription/
    ├── TenantEventEnvelope.cs              (CREATE)
    ├── TenantEventProcessor.cs             (CREATE)
    └── TenantEventSubscriptionEndpoints.cs (CREATE)

tests/Hexalith.Tenants.Client.Tests/
├── Hexalith.Tenants.Client.Tests.csproj    (EXISTS — no changes)
├── Handlers/
│   └── TenantProjectionEventHandlerTests.cs (CREATE)
├── Projections/
│   └── InMemoryTenantProjectionStoreTests.cs (CREATE)
├── Registration/
│   └── TenantServiceCollectionExtensionsTests.cs (EXISTS — add new tests)
├── Subscription/
│   └── TenantEventProcessorTests.cs        (CREATE)
└── ScaffoldingSmokeTests.cs                (EXISTS — no changes)
```

### Testing Requirements

**Tier 1 (Unit) — No infrastructure needed.**

**Test Matrix — TenantProjectionEventHandler:**

| #   | Test                                      | Event                                                 | Expected State Change                            | AC  |
| --- | ----------------------------------------- | ----------------------------------------------------- | ------------------------------------------------ | --- |
| P1  | TenantCreated initializes state           | TenantCreated("acme", "Acme Corp", "Desc", now)       | TenantId="acme", Name="Acme Corp", Status=Active | #1  |
| P2  | UserAddedToTenant adds member             | UserAddedToTenant("acme", "user1", Owner)             | Members["user1"] = TenantOwner                   | #1  |
| P3  | UserRemovedFromTenant removes member      | UserRemovedFromTenant("acme", "user1")                | Members does not contain "user1"                 | #2  |
| P4  | TenantDisabled sets status                | TenantDisabled("acme", now)                           | Status = Disabled                                | #3  |
| P5  | TenantEnabled restores status             | TenantEnabled("acme", now)                            | Status = Active                                  | #3  |
| P6  | TenantConfigurationSet adds config        | TenantConfigurationSet("acme", "billing.plan", "pro") | Configuration["billing.plan"] = "pro"            | #4  |
| P7  | TenantConfigurationRemoved removes config | TenantConfigurationRemoved("acme", "billing.plan")    | Configuration does not contain "billing.plan"    | #4  |
| P8  | UserRoleChanged updates role              | UserRoleChanged("acme", "user1", Owner, Reader)       | Members["user1"] = TenantReader                  | #1  |
| P9  | TenantUpdated updates metadata            | TenantUpdated("acme", "New Name", "New Desc")         | Name="New Name", Description="New Desc"          | #1  |
| P10 | Multi-tenant isolation                    | Events for "acme" and "beta"                          | Each tenant has independent state                | #6  |

**Test Matrix — InMemoryTenantProjectionStore:**

| #   | Test                                     | Setup                      | Expected                   | AC  |
| --- | ---------------------------------------- | -------------------------- | -------------------------- | --- |
| S1  | GetAsync returns null for unknown tenant | Empty store                | null                       | #6  |
| S2  | SaveAsync then GetAsync returns state    | Save state for "acme"      | Returns same state         | #1  |
| S3  | SaveAsync overwrites existing state      | Save twice for "acme"      | Returns latest             | #1  |
| S4  | Tenant isolation                         | Save for "acme" and "beta" | Get("acme") != Get("beta") | #6  |
| S5  | Null state throws                        | SaveAsync(null)            | ArgumentNullException      | —   |
| S6  | Empty tenantId throws                    | GetAsync("")               | ArgumentException          | —   |

**Test Matrix — TenantEventProcessor:**

| #   | Test                            | Setup                                | Expected                     | AC  |
| --- | ------------------------------- | ------------------------------------ | ---------------------------- | --- |
| E1  | Processes known event type      | Envelope with TenantCreated          | Handler called, returns true | #1  |
| E2  | Skips unknown event type        | Envelope with unknown EventTypeName  | Returns false, no exception  | —   |
| E3  | Deduplicates by MessageId       | Same MessageId sent twice            | Second call returns false    | #5  |
| E4  | Dispatches to multiple handlers | Built-in + custom handler registered | Both handlers called         | #1  |
| E5  | Null envelope throws            | ProcessAsync(null)                   | ArgumentNullException        | —   |

**Test Matrix — Updated DI Registration:**

| #   | Test                                                      | Expected                                                                         | AC                |
| --- | --------------------------------------------------------- | -------------------------------------------------------------------------------- | ----------------- | --- |
| D13 | AddHexalithTenants registers ITenantProjectionStore       | Descriptor exists for ITenantProjectionStore                                     | #1                |
| D14 | AddHexalithTenants registers TenantEventProcessor         | Descriptor exists for TenantEventProcessor                                       | #1                |
| D15 | AddHexalithTenants registers TenantProjectionEventHandler | Descriptor exists for ITenantEventHandler<TenantCreated> (and other event types) | #1                |
| D16 | InMemoryTenantProjectionStore is default implementation   | Resolve ITenantProjectionStore → InMemoryTenantProjectionStore                   | #1                |
| D17 | Custom ITenantProjectionStore overrides default           | Register custom store before AddHexalithTenants                                  | Custom store used | #6  |

**Key test patterns:**

```csharp
// P2: UserAddedToTenant applies correctly
[Fact]
public async Task HandleAsync_UserAddedToTenant_AddsMemberWithRole()
{
    var store = new InMemoryTenantProjectionStore();
    var handler = new TenantProjectionEventHandler(store);
    var @event = new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner);
    var context = new TenantEventContext("acme", "msg-1", 1, DateTimeOffset.UtcNow, "corr-1");

    await handler.HandleAsync(@event, context);

    TenantLocalState? state = await store.GetAsync("acme");
    state.ShouldNotBeNull();
    state.Members.ShouldContainKey("user1");
    state.Members["user1"].ShouldBe(TenantRole.TenantOwner);
}

// E3: Idempotency — duplicate MessageId skipped
[Fact]
public async Task ProcessAsync_DuplicateMessageId_ReturnsFalse()
{
    // Setup processor with event type registry and a mock/real handler
    var envelope = CreateEnvelope("msg-1", "TenantCreated", ...);

    bool first = await processor.ProcessAsync(envelope);
    bool second = await processor.ProcessAsync(envelope);

    first.ShouldBeTrue();
    second.ShouldBeFalse();
}

// P10: Multi-tenant isolation
[Fact]
public async Task HandleAsync_MultipleTenants_MaintainsIndependentState()
{
    var store = new InMemoryTenantProjectionStore();
    var handler = new TenantProjectionEventHandler(store);

    await handler.HandleAsync(
        new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
        new TenantEventContext("acme", "msg-1", 1, DateTimeOffset.UtcNow, "corr-1"));

    await handler.HandleAsync(
        new UserAddedToTenant("beta", "user2", TenantRole.TenantReader),
        new TenantEventContext("beta", "msg-2", 1, DateTimeOffset.UtcNow, "corr-2"));

    TenantLocalState? acmeState = await store.GetAsync("acme");
    TenantLocalState? betaState = await store.GetAsync("beta");

    acmeState!.Members.ShouldContainKey("user1");
    acmeState.Members.ShouldNotContainKey("user2");
    betaState!.Members.ShouldContainKey("user2");
    betaState.Members.ShouldNotContainKey("user1");
}
```

### Code Style Requirements

- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman braces (new line before opening brace)
- 4-space indentation, CRLF line endings, UTF-8
- `TreatWarningsAsErrors = true` — all warnings are build failures
- `ArgumentNullException.ThrowIfNull()` on all reference type parameters
- `ArgumentException.ThrowIfNullOrWhiteSpace()` on string parameters that must not be empty
- XML doc comments: Add `/// <summary>` on all public types, interfaces, and public methods — this is a NuGet package's public API surface
- No XML docs on private/internal helpers
- No `_ = RuleFor(...)` discard pattern
- Use `ConfigureAwait(false)` on all awaited calls in library code (Client is a library, not an app)

### Previous Story Intelligence

**Story 4.1 (review) — Client DI Registration:**

- `EnsureCoreRegistrations()` pattern for shared logic between overloads — extend this or add a parallel method
- `HasTenantOptionsConfiguration()` pattern for idempotency checks — reuse this pattern for new registrations
- `TryGetConfiguration()` pattern for opportunistic config resolution — already available, no changes needed
- `Dapr.Client` was replaced by `Dapr.AspNetCore` in the csproj — this is the correct package for DAPR integration
- `Microsoft.Extensions.Configuration.Memory` was NOT needed (available transitively in .NET 10)
- Build verification: 172/172 Tier 1+2 tests pass, 2 pre-existing Tier 3 integration test failures (DAPR sidecar required)

**Story 3.3 (review) — Tenant Configuration Management:**

- Established `TenantConfigurationSet` and `TenantConfigurationRemoved` event records
- Configuration uses Key/Value pattern with no namespace property on the event itself (the PRD mentions namespace conventions like "billing.plan" but the key IS the namespaced key)

### Git Intelligence

Recent commits:

- `ed9474b feat: Update tenant configuration management and validation`
- `0e55463 fix: Code review fixes for Story 3.2 Role Behavior Enforcement`
- `fd1b5d9 feat: Finalize CommandApi Bootstrap & Event Publishing`
- `9753e09 feat: Implement tenant configuration management with validation and RBAC support`

All Epic 2 and 3 stories are done/review. Story 4.1 is in review (DI foundation complete). Epic 4 focus is now on consuming service integration.

### Cross-Story Dependencies

**This story depends on:**

- Story 4.1 (review): DI foundation — `AddHexalithTenants()`, `HexalithTenantsOptions`, DaprClient registration
- Epic 1 (done): Solution structure, Client.csproj, build configuration
- Story 2.1 (done): Contract types (events) in Contracts project
- Story 3.3 (review): Configuration events (TenantConfigurationSet/Removed)

**Stories that depend on this:**

- Story 4.3: Sample Consuming Service — uses the pattern established here
- Epic 5: Query endpoints — consuming services reference Client package
- Story 6.1: InMemoryTenantService and test helpers — may reuse projection patterns

### Critical Anti-Patterns (DO NOT)

- **DO NOT** reference Server or CommandApi projects from Client — Client is a consuming-service package
- **DO NOT** use the Contracts `EventEnvelope` (nested Metadata + Payload) for DAPR subscription — the wire format is the flat Server EventEnvelope. Create a Client-side DTO that matches the flat shape
- **DO NOT** hardcode the topic name or pub/sub name — always resolve from `IOptions<HexalithTenantsOptions>`
- **DO NOT** throw exceptions for unknown event types — log a warning and skip. New events may be added to the system before consuming services are updated
- **DO NOT** use `typeof(HexalithTenantsOptions)` for idempotency checks — use `typeof(IConfigureOptions<HexalithTenantsOptions>)` per Story 4.1 pattern
- **DO NOT** make `TenantLocalState` a record — it needs mutable collections (Members, Configuration) that are updated in place
- **DO NOT** add async methods to `TenantLocalState` — it's a plain POCO read model
- **DO NOT** implement complex eviction/TTL on the idempotency dictionary — add a comment for production, keep MVP simple
- **DO NOT** modify any files in Contracts, Server, or CommandApi projects
- **DO NOT** create test infrastructure that duplicates what's in Testing package
- **DO NOT** block the DAPR subscription endpoint — return success (200) quickly after dispatching. If handler logic is slow, consider background processing (but not in MVP)

### Project Structure Notes

- Alignment with EventStore: mirrors Client package structure (Handlers, Projections, Registration)
- `InternalsVisibleTo` already configured in Client.csproj for Client.Tests
- Client.Tests.csproj already references Client and Testing projects — no changes needed
- `Directory.Build.props` in `tests/` sets `IsPackable=false` for test projects
- Solution file (`Hexalith.Tenants.slnx`) already includes Client and Client.Tests

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.2] — Story definition, ACs, BDD scenarios
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 4] — Epic objectives: consuming service support
- [Source: _bmad-output/planning-artifacts/prd.md#FR37-FR42] — Event-driven integration requirements
- [Source: _bmad-output/planning-artifacts/prd.md#FR44-FR45] — DI registration requirements (under 20 lines)
- [Source: _bmad-output/planning-artifacts/prd.md#Journey 3] — Alex integrates tenant events across services
- [Source: _bmad-output/planning-artifacts/architecture.md#Consuming Service Flow] — DAPR pub/sub → Service Subscription → Local Projection
- [Source: _bmad-output/planning-artifacts/architecture.md#Event Publishing] — DAPR pub/sub, CloudEvents 1.0, topic system.tenants.events
- [Source: _bmad-output/planning-artifacts/architecture.md#Client] — "References Contracts only (thin DI layer)"
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventPublisher.cs] — How events are published (flat EventEnvelope, CloudEvent metadata)
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventEnvelope.cs] — Wire format (17-field flat record)
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/EventMetadata.cs] — 15-field metadata (MessageId, SequenceNumber, EventTypeName, etc.)
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Events/IEventPayload.cs] — Marker interface for all events
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Registration/EventStoreServiceCollectionExtensions.cs] — DI extension method pattern
- [Source: src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs] — Current DI registration (to extend)
- [Source: src/Hexalith.Tenants.Client/Configuration/HexalithTenantsOptions.cs] — Options with PubSubName, TopicName
- [Source: _bmad-output/implementation-artifacts/4-1-client-di-registration.md] — Previous story: DI foundation, patterns, learnings

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- Fixed `using ServiceProvider` causing ObjectDisposedException in TenantEventProcessorTests — provider was disposed before async processing completed.
- Removed redundant `Microsoft.Extensions.Configuration.Binder` and `Microsoft.Extensions.Hosting.Abstractions` packages from Client.csproj after adding `FrameworkReference Include="Microsoft.AspNetCore.App"` (NU1510 warnings-as-errors).
- Review follow-up fixed custom `ITenantProjectionStore` registration so the event processor, registry, and handlers still register when consumers provide their own store.
- Review follow-up hardened `TenantEventProcessor` so invalid payloads and handler failures do not permanently poison deduplication, while skipped/unsupported events are logged explicitly.
- Review follow-up isolated in-memory projection state with cloning and serialized per-tenant updates to avoid shared mutable state aliasing.

### Completion Notes List

- Task 1: Created `ITenantEventHandler<TEvent>` generic interface constrained to `IEventPayload` and `TenantEventContext` record with TenantId, MessageId, SequenceNumber, Timestamp, CorrelationId.
- Task 2: Created `TenantLocalState` read model (mutable class with Members/Configuration dictionaries), `ITenantProjectionStore` abstraction, and `InMemoryTenantProjectionStore` using `ConcurrentDictionary` for thread safety.
- Task 3: Created `TenantProjectionEventHandler` implementing all 9 event handler interfaces (TenantCreated, TenantUpdated, TenantDisabled, TenantEnabled, UserAddedToTenant, UserRemovedFromTenant, UserRoleChanged, TenantConfigurationSet, TenantConfigurationRemoved). Each follows get-or-create → mutate → save pattern.
- Task 4: Created `TenantEventEnvelope` wire-format DTO matching EventStore's published flat envelope. Created `TenantEventProcessor` with idempotency via `ConcurrentDictionary<string, byte>`, event type resolution via assembly scanning registry, payload deserialization via `System.Text.Json`, and reflection-based dispatch to all registered `ITenantEventHandler<TEvent>` implementations.
- Task 5: Created `TenantEventSubscriptionEndpoints.MapTenantEventSubscription()` extension method using Dapr.AspNetCore's `WithTopic()` minimal API support. Added `FrameworkReference Include="Microsoft.AspNetCore.App"` to Client.csproj.
- Task 6: Extended `AddHexalithTenants()` with `EnsureEventHandlerRegistrations()` registering: `ITenantProjectionStore` → `InMemoryTenantProjectionStore` (Singleton), `TenantProjectionEventHandler` (Singleton) forwarded to all 9 `ITenantEventHandler<T>` interfaces, event type registry from Contracts assembly scan, and `TenantEventProcessor` (Singleton). All registrations are idempotent.
- Review fixes: Added `TenantEventProcessingResult`, explicit no-handler/invalid-payload outcomes, safe dedup cleanup on failure, and endpoint behavior that distinguishes successful skips from invalid payload failures.
- Review fixes: Added cloning in `TenantLocalState`/`InMemoryTenantProjectionStore` and per-tenant locking in `TenantProjectionEventHandler` to prevent shared-state mutation races.
- Task 7: Expanded regression coverage for custom projection-store registration, cloning semantics, invalid payload handling, and retry-after-handler-failure behavior. Focused client suite now passes 48/48 tests.

### File List

**New files:**

- src/Hexalith.Tenants.Client/Handlers/ITenantEventHandler.cs
- src/Hexalith.Tenants.Client/Handlers/TenantEventContext.cs
- src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs
- src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs
- src/Hexalith.Tenants.Client/Projections/ITenantProjectionStore.cs
- src/Hexalith.Tenants.Client/Projections/InMemoryTenantProjectionStore.cs
- src/Hexalith.Tenants.Client/Subscription/TenantEventEnvelope.cs
- src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs
- src/Hexalith.Tenants.Client/Subscription/TenantEventProcessingResult.cs
- src/Hexalith.Tenants.Client/Subscription/TenantEventSubscriptionEndpoints.cs
- tests/Hexalith.Tenants.Client.Tests/Handlers/TenantProjectionEventHandlerTests.cs
- tests/Hexalith.Tenants.Client.Tests/Projections/InMemoryTenantProjectionStoreTests.cs
- tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs

**Modified files:**

- src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj (added FrameworkReference, removed redundant packages)
- src/Hexalith.Tenants.Client/Registration/TenantServiceCollectionExtensions.cs (added event handler registrations)
- src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs (added cloning support)
- src/Hexalith.Tenants.Client/Projections/InMemoryTenantProjectionStore.cs (clone on read/write)
- src/Hexalith.Tenants.Client/Handlers/TenantProjectionEventHandler.cs (per-tenant synchronization)
- src/Hexalith.Tenants.Client/Subscription/TenantEventProcessor.cs (safe processing outcomes and retry-friendly dedup behavior)
- src/Hexalith.Tenants.Client/Subscription/TenantEventSubscriptionEndpoints.cs (explicit result mapping)
- tests/Hexalith.Tenants.Client.Tests/Registration/TenantServiceCollectionExtensionsTests.cs (added 5 new DI tests)
- tests/Hexalith.Tenants.Client.Tests/Projections/InMemoryTenantProjectionStoreTests.cs (added clone isolation coverage)
- tests/Hexalith.Tenants.Client.Tests/Subscription/TenantEventProcessorTests.cs (added invalid payload, no-handler, and retry-after-failure coverage)

## Change Log

- 2026-03-17: Story 4.2 implementation complete — Event Subscription & Local Projection Pattern. Added event handler abstractions, local projection infrastructure, DAPR subscription support, and comprehensive unit tests (28 new tests, 44 total Client tests).
- 2026-03-17: Review follow-up complete — fixed custom-store DI registration, retry-safe event processing, explicit processing outcomes, and projection state isolation; validated with `Hexalith.Tenants.Client.Tests` (48/48 passing).
