---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments: []
workflowType: 'research'
lastStep: 1
research_type: 'technical'
research_topic: 'Hexalith.EventStore implementation for tenant microservice - commands, queries, and events'
research_goals: 'Understand how to create and manage tenant commands, queries, and events using Hexalith.EventStore architecture'
user_name: 'Jerome'
date: '2026-03-15'
web_research_enabled: true
source_verification: true
---

# Hexalith.EventStore Tenant Implementation: Comprehensive Technical Research

**Date:** 2026-03-15
**Author:** Jerome
**Research Type:** Technical — CQRS/Event Sourcing Implementation

---

## Research Overview

This technical research document provides a comprehensive analysis of how the Hexalith.Tenants microservice should implement the Hexalith.EventStore framework for creating and managing tenant commands, queries, and events. The research was conducted through deep codebase analysis of the EventStore submodule, exploration of the existing Tenants project structure, and verification against current industry best practices for CQRS/Event Sourcing in .NET.

The research covers the complete implementation landscape: from the framework's reflection-driven aggregate architecture and 5-step checkpointed actor pipeline, through the two-aggregate domain model (TenantAggregate + GlobalAdministratorsAggregate), to concrete implementation blueprints with code patterns, testing strategies, and deployment workflows. All findings are grounded in the actual EventStore source code and validated against current web sources.

Key outcomes include a clear implementation roadmap (state classes → aggregates → projections → DI wiring), a Given/When/Then testing strategy that exploits the pure-function nature of event-sourced aggregates, and identification of the critical "platform tenant" identity pattern where all tenant management runs under `TenantId = "system"`. See the full Executive Summary and Recommendations in the Research Synthesis section at the end of this document.

---

## Technical Research Scope Confirmation

**Research Topic:** Hexalith.EventStore implementation for tenant microservice - commands, queries, and events
**Research Goals:** Understand how to create and manage tenant commands, queries, and events using Hexalith.EventStore architecture

**Technical Research Scope:**

- Architecture Analysis - EventStore design patterns, CQRS/ES framework structure, aggregate roots
- Implementation Approaches - command/event/query creation patterns, handler conventions
- Technology Stack - Hexalith base classes, interfaces, DI registration
- Integration Patterns - command-to-event flow, event-to-projection flow, API integration
- Performance Considerations - snapshotting, replay, scalability

**Research Methodology:**

- Deep codebase analysis of the Hexalith.EventStore submodule
- Current web data with rigorous source verification
- Multi-source validation for critical technical claims
- Confidence level framework for uncertain information
- Comprehensive technical coverage with architecture-specific insights

**Scope Confirmed:** 2026-03-15

## Technology Stack Analysis

### Core Framework: Hexalith.EventStore

Hexalith is an application framework for building modular, multi-tenant applications on ASP.NET Core and DAPR. The EventStore component provides a reflection-driven CQRS/Event Sourcing framework with convention-over-configuration design.

_Framework source: Hexalith.EventStore submodule (local codebase analysis)_
_Organization: [Hexalith GitHub](https://github.com/Hexalith/)_
_Documentation: [hexalith.readthedocs.io](https://hexalith.readthedocs.io/en/latest/)_

**Key Package Hierarchy:**

| Package | Purpose | Dependencies |
|---------|---------|--------------|
| `Hexalith.EventStore.Contracts` | Core CQRS/ES types (commands, events, queries, identity) | Zero infrastructure deps |
| `Hexalith.EventStore.Client` | Domain implementation SDK (aggregates, projections, DI) | Contracts |
| `Hexalith.EventStore.Server` | DAPR actor orchestration (state machines, publishing) | Client, Dapr.Actors |
| `Hexalith.EventStore.CommandApi` | REST API layer (controllers, auth, rate limiting) | Server |
| `Hexalith.EventStore.Testing` | Test builders and assertions | Client |

### Programming Languages and Runtime

_Language: C# (.NET 10.0), 93.4% of Hexalith codebase_
_Target Framework: net10.0 with nullable reference types and implicit usings_
_Build: Warnings treated as errors, MinVer (7.0.0) for git-based semantic versioning_

**Language Features Used:**
- C# records for immutable commands, events, and DTOs
- Static abstract interface members (C# 11+) for `IQueryContract`
- Source-generated regex (C# 12+) in `KebabConverter`
- Primary constructors for records
- Pattern matching in command dispatch and state rehydration

### Development Frameworks and Libraries

_Major Frameworks:_
- **ASP.NET Core 10.0** — Web API hosting, DI, configuration
- **DAPR 1.17.3** — Distributed runtime (state stores, pub/sub, actors)
  - `Dapr.Client` — Service invocation and state management
  - `Dapr.AspNetCore` — ASP.NET Core integration
  - `Dapr.Actors` / `Dapr.Actors.AspNetCore` — Virtual actor framework
- **.NET Aspire 13.1.2** — Distributed application composition and orchestration
  - `Aspire.Hosting.Dapr` (13.0.0) — DAPR resource provisioning

_Application Libraries:_
- **MediatR 14.0.0** — In-process command/query mediation
- **FluentValidation 12.1.1** — Request validation
- **Microsoft.AspNetCore.Authentication.JwtBearer 10.0.0** — JWT auth
- **Microsoft.AspNetCore.OpenApi 10.0.3** — OpenAPI/Swagger

_Source: Directory.Packages.props (central package management)_

### Database and Storage Technologies

_State Store: Redis (via DAPR state store component)_
- Configuration: `statestore.yaml` with `actorStateStore: "true"`
- Scoped to `commandapi` service
- Environment-driven: `REDIS_HOST`, `REDIS_PASSWORD`

_Event Persistence: DAPR state store with composite keys_
- Event stream keys: `{tenantId}:{domain}:{aggregateId}:events:{sequenceNumber}`
- Snapshot keys: `{tenantId}:{domain}:{aggregateId}:snapshot`
- Metadata keys: `{tenantId}:{domain}:{aggregateId}:metadata`

_Multi-tenancy Isolation:_
- Layer 1: Colon-separated key components (structurally disjoint)
- Layer 2: Composite key prefixing per aggregate identity
- Layer 3: DAPR actor scoping (one actor instance per identity)
- Layer 4: JWT tenant claim enforcement at API layer

_Source: Hexalith.EventStore.Contracts/Identity/AggregateIdentity.cs, AppHost/DaprComponents/statestore.yaml_

### Development Tools and Platforms

_IDE: Visual Studio 2022 / VS Code with .NET tooling_
_Build: dotnet CLI with Directory.Build.props central configuration_
_Testing Framework:_
- xUnit 2.9.3 — Test runner
- Shouldly 4.3.0 — Fluent assertions
- NSubstitute 5.3.0 — Mocking
- Testcontainers — Integration testing with Docker

_Code Quality:_
- SonarCloud analysis
- Coverity Scan
- Codacy reviews
- EditorConfig for code style enforcement
- Markdownlint for documentation

_Source: Directory.Packages.props, .editorconfig_

### Cloud Infrastructure and Deployment

_Container Technologies:_
- DAPR sidecar pattern for distributed capabilities
- Docker + Testcontainers for integration testing
- .NET Aspire for local development orchestration

_Distributed Patterns:_
- Virtual actors (DAPR Actors) for aggregate state machines
- Pub/sub (DAPR) for event publishing
- SignalR for real-time projection change notifications
- Service discovery via Aspire

_Security:_
- DAPR access control policies (deny-by-default in production)
- mTLS between services
- JWT Bearer authentication at API boundary
- Payload redaction in logs (SEC-5 compliance)

_Source: AppHost/DaprComponents/, Hexalith.EventStore.CommandApi, Hexalith.EventStore.SignalR_

### Technology Adoption and Patterns

_Architecture Pattern: CQRS + Event Sourcing with DDD aggregates_
- Commands are pure data records (no base class required)
- Events implement `IEventPayload` marker interface
- Rejections implement `IRejectionEvent` (extends `IEventPayload`)
- Aggregates use reflection-based `Handle(Command, State?) -> DomainResult` dispatch
- Projections use reflection-based `Apply(Event)` on read models
- Three-outcome results: Success, Rejection, NoOp

_Convention-over-Configuration:_
- `NamingConventionEngine`: PascalCase -> kebab-case for domain names
- `AssemblyScanner`: Auto-discovery of aggregates and projections
- 5-layer cascade configuration (conventions -> global options -> self-config -> appsettings -> explicit overrides)
- `EventStoreDomainAttribute` for convention override

_Multi-Tenancy Built-In:_
- `AggregateIdentity(TenantId, Domain, AggregateId)` — colon-separated canonical format
- All state store keys prefixed with tenant identity
- DAPR actor isolation per tenant
- JWT claim-based tenant enforcement

_Source: Hexalith.EventStore.Client/Conventions/, Discovery/, Configuration/_

## Integration Patterns Analysis

### Command Submission Pipeline (REST API → DAPR Actors)

The command flow follows a multi-stage pipeline from HTTP to DAPR actor processing:

```
HTTP POST /api/v1/commands (JWT-authenticated)
  │
  └─ CommandsController.Submit()
     ├─ Extract JWT `sub` claim as userId (F-RT2: never use `name` claim)
     ├─ Sanitize extension metadata (SEC-4)
     ├─ Create SubmitCommand (MediatR request)
     │
     └─ MediatR pipeline
        └─ SubmitCommandHandler.Handle()
           ├─ [Advisory] Write "Received" status to state store
           ├─ [Advisory] Archive original command for replay
           │
           └─ CommandRouter.RouteCommandAsync()
              ├─ Derive AggregateIdentity(TenantId, Domain, AggregateId)
              ├─ Construct ActorId = "{tenant}:{domain}:{aggregateId}"
              └─ Create ActorProxy<AggregateActor>(actorId)
                 │
                 └─ Returns HTTP 202 Accepted + CorrelationId
```

_Key Design: Advisory status writes (Step 1-2) cannot fail the pipeline — only the actor routing (Step 3) is critical._
_Source: EventStore.CommandApi/Controllers/CommandsController.cs, EventStore.Server/Pipeline/SubmitCommandHandler.cs, EventStore.Server/Commands/CommandRouter.cs_
_Reference: [DAPR Actors Overview](https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/)_

### Aggregate Actor Processing (5-Step Checkpointed Pipeline)

The `AggregateActor` implements a crash-recoverable 5-step pipeline:

| Step | Operation | Recovery |
|------|-----------|----------|
| 1 | **Idempotency check** — cached result by CausationId; resume detection for in-flight pipelines | Skip to cached result |
| 2 | **Tenant validation** — validates TenantId matches actor ID (SEC-2, F-PM2). BEFORE any state access | Reject with TenantMismatchException |
| 3 | **State rehydration** — load snapshot + tail-only event replay to reconstruct aggregate state | Dead-letter on infrastructure failure |
| 4 | **Domain service invocation** — DAPR service-to-service call to domain processor (Handle method) | Dead-letter on infrastructure failure |
| 5 | **Event persistence + publication** — persist events atomically, create snapshot if threshold met, publish via DAPR pub/sub | Drain reminder for failed publications |

_Terminal states: Completed (success), Rejected (domain rejection), PublishFailed (events persisted, pub/sub failed — drain recovery active)_
_Source: EventStore.Server/Actors/AggregateActor.cs_

### Domain Service Invocation (DAPR Service-to-Service)

The aggregate actor delegates command processing to a registered domain service via DAPR service invocation:

```
AggregateActor
  └─ DaprDomainServiceInvoker.InvokeAsync()
     ├─ Extract version from command extensions (default: "v1")
     ├─ Resolve service registration (AppId, MethodName) via IDomainServiceResolver
     └─ daprClient.InvokeMethodAsync<DomainServiceRequest, DomainServiceWireResult>()
        │
        ├─ DomainServiceRequest = { CommandEnvelope, CurrentState }
        └─ DomainServiceWireResult → DomainResult (events or rejections)
```

The domain service (e.g., the Tenants CommandApi hosting `TenantAggregate`) exposes a `/process` endpoint that:
1. Receives `DomainServiceRequest(Command, CurrentState)`
2. Calls `IDomainProcessor.ProcessAsync()` (dispatches to `Handle(Command, State?)`)
3. Returns `DomainServiceWireResult` containing events

_Pattern: The aggregate actor never runs domain logic directly — it invokes a separate DAPR service. This enables independent versioning and scaling of domain services._
_Source: EventStore.Server/DomainServices/DaprDomainServiceInvoker.cs, EventStore.Sample/Program.cs_

### Event Publication via DAPR Pub/Sub

After events are persisted to the state store, they are published via DAPR pub/sub:

```
EventPublisher.PublishEventsAsync()
  └─ For each event:
     ├─ Unprotect payload (decrypt if needed)
     ├─ Construct EventEnvelope with metadata
     └─ daprClient.PublishEventAsync()
        ├─ PubSub: configured component name (e.g., "pubsub")
        ├─ Topic: derived from AggregateIdentity (e.g., "acme.tenant.events")
        └─ CloudEvents 1.0 metadata:
           ├─ cloudevent.type = EventTypeName
           ├─ cloudevent.source = "hexalith-eventstore/{TenantId}/{Domain}"
           └─ cloudevent.id = "{CorrelationId}:{SequenceNumber}"
```

_DAPR resiliency policies handle transient failures — no custom retry logic in the publisher (Rule #4)._
_DAPR pubsub configuration: exponential backoff (10s outbound, 30s inbound), circuit breaker at 5 consecutive failures._
_Source: EventStore.Server/Events/EventPublisher.cs, AppHost/DaprComponents/resiliency.yaml_
_Reference: [DAPR Pub/Sub Overview](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/)_

### Query Execution Pipeline (Two-Gate ETag Caching)

Queries use a highly optimized two-gate caching strategy:

```
HTTP POST /api/v1/queries
  │
  ├─ Gate 1: ETag Pre-check (Controller level)
  │  ├─ Decode self-routing ETag from If-None-Match header
  │  ├─ Get current ETag from ETagActor
  │  └─ If match → HTTP 304 Not Modified (no actor invocation)
  │
  └─ MediatR pipeline
     └─ QueryRouter.RouteQueryAsync()
        ├─ Derive projection actor ID (QueryType + Tenant + EntityId)
        └─ CachingProjectionActor.QueryAsync()
           │
           ├─ Gate 2: In-Memory ETag Cache (Actor level)
           │  ├─ Get current ETag from ETagActor
           │  └─ If cached ETag matches → return cached payload (no computation)
           │
           └─ Cache miss: ExecuteQueryAsync() → build read model
              ├─ Runtime projection type discovery (FR63)
              ├─ Clone and cache payload (prevent dangling references)
              └─ Return QueryResult with ETag header
```

_Self-routing ETags contain the projection type, enabling efficient actor lookup._
_Source: EventStore.CommandApi/Controllers/QueriesController.cs, EventStore.Server/Actors/CachingProjectionActor.cs, EventStore.Server/Actors/ETagActor.cs_

### Projection Change Notifications (SignalR Real-Time)

When projections change (events processed), clients are notified in real-time:

```
Event processed by projection subscriber
  │
  └─ DaprProjectionChangeNotifier.NotifyProjectionChangedAsync()
     │
     ├─ ETagActor.RegenerateAsync()
     │  ├─ Actor ID: "{ProjectionType}:{TenantId}"
     │  ├─ Generate new self-routing ETag
     │  └─ Persist to actor state (invalidates all cached queries)
     │
     └─ SignalRProjectionChangedBroadcaster.BroadcastChangedAsync()
        ├─ Group: "{ProjectionType}:{TenantId}"
        └─ Client method: ProjectionChanged(projectionType, tenantId)
           └─ Clients invalidate local cache / refresh UI
```

_Fail-open pattern (ADR-18.5a): SignalR broadcast failures do NOT block ETag regeneration._
_Hub path: /hubs/projection-changes_
_Source: EventStore.Server/Projections/DaprProjectionChangeNotifier.cs, EventStore.CommandApi/SignalR/ProjectionChangedHub.cs_

### Multi-Tenancy Integration Pattern

Multi-tenancy is enforced at every layer of the integration pipeline:

| Layer | Mechanism | File |
|-------|-----------|------|
| **API Boundary** | JWT `sub` claim → UserId; tenant from request body | CommandsController |
| **Identity Construction** | `AggregateIdentity(TenantId, Domain, AggregateId)` — validated, lowercase, no colons | AggregateIdentity.cs |
| **Actor Routing** | ActorId = `{tenant}:{domain}:{aggregateId}` — structurally disjoint per tenant | CommandRouter.cs |
| **Pre-State Validation** | Step 2: Tenant validation BEFORE any state access (SEC-2) | AggregateActor.cs |
| **State Store Keys** | All keys prefixed: `{tenant}:{domain}:{aggregateId}:events:N` | AggregateIdentity.cs |
| **Pub/Sub Topics** | Topic: `{tenant}.{domain}.events` — tenant-scoped | EventPublisher.cs |
| **ETag Actors** | Actor ID: `{projectionType}:{tenantId}` — per-tenant cache invalidation | ETagActor.cs |
| **DAPR Access Control** | `accesscontrol.yaml` — deny-by-default for commandapi | DaprComponents/ |

_Source: Codebase analysis of AggregateIdentity, AggregateActor, CommandRouter, ETagActor_

### Security Integration Patterns

| Pattern | Implementation | Standard |
|---------|---------------|----------|
| **JWT Bearer Auth** | ASP.NET Core middleware at API boundary | OAuth 2.0 |
| **Mutual TLS** | DAPR sidecar-to-sidecar communication | mTLS |
| **Payload Redaction** | `CommandEnvelope.ToString()` and `EventEnvelope.ToString()` redact payload bytes | SEC-5 |
| **Extension Sanitization** | Metadata keys/values validated and sanitized at controller | SEC-4 |
| **Optimistic Concurrency** | ETags for state store reads/writes via DAPR | OCC |
| **Dead-Letter Routing** | Infrastructure failures → dead-letter topic for manual review | DAPR DLQ |

_Source: EventStore.CommandApi (JWT, sanitization), EventStore.Contracts (redaction), DaprComponents (mTLS, DLQ)_
_Reference: [DAPR State Management](https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/)_

### Aspire Topology Composition

The Hexalith.Tenants project uses .NET Aspire for local development orchestration:

```csharp
// AppHost/Program.cs
IResourceBuilder<ProjectResource> commandApi = builder
    .AddProject<Projects.Hexalith_Tenants_CommandApi>("commandapi");

HexalithTenantsResources resources = builder.AddHexalithTenants(
    commandApi, accessControlConfigPath);

// HexalithTenantsExtensions.cs — reusable topology
IResourceBuilder<IDaprComponentResource> stateStore = builder
    .AddDaprComponent("statestore", "state.in-memory")
    .WithMetadata("actorStateStore", "true");
IResourceBuilder<IDaprComponentResource> pubSub = builder.AddDaprPubSub("pubsub");

commandApi.WithDaprSidecar(sidecar => sidecar
    .WithOptions(new DaprSidecarOptions { AppId = "commandapi", Config = daprConfigPath })
    .WithReference(stateStore)
    .WithReference(pubSub));
```

_Uses `AddDaprComponent()` instead of `AddDaprStateStore()` to ensure `actorStateStore: "true"` metadata propagates._
_Source: Hexalith.Tenants.AppHost/Program.cs, Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs_

### DAPR Resiliency Configuration

```yaml
# AppHost/DaprComponents/resiliency.yaml
retries:
  pubsubRetryOutbound: exponential, maxInterval=10s, maxRetries=3
  pubsubRetryInbound: exponential, maxInterval=30s, maxRetries=10
  defaultRetry: constant, duration=1s, maxRetries=3

circuitBreakers:
  defaultBreaker: trip after 3 consecutive failures, 30s timeout
  pubsubBreaker: trip after 5 consecutive failures, 30s timeout

targets:
  commandapi: defaultRetry + defaultBreaker
  pubsub outbound: pubsubRetryOutbound + pubsubBreaker
  pubsub inbound: pubsubRetryInbound
  statestore: defaultRetry + defaultBreaker
```

_Source: AppHost/DaprComponents/resiliency.yaml_
_Reference: [DAPR Resiliency](https://docs.dapr.io/operations/resiliency/)_

## Architectural Patterns and Design

### System Architecture: Two-Aggregate Domain Model

The Tenants domain uses a **two-aggregate root design** — a deliberate DDD boundary decision separating per-tenant state from platform-wide administration:

**Aggregate 1: TenantAggregate** (Multiple instances)
- Identity: `system:tenants:{managedTenantId}` via `TenantIdentity.ForTenant(id)`
- Scope: Single tenant's metadata, status, users, roles, and configuration
- Commands: CreateTenant, UpdateTenant, EnableTenant, DisableTenant, AddUserToTenant, RemoveUserFromTenant, ChangeUserRole, SetTenantConfiguration, RemoveTenantConfiguration
- State shape: TenantId, Name, Description, Status (Active/Disabled), Users (Dictionary<UserId, TenantRole>), Configuration (Dictionary<Key, Value>), Timestamps

**Aggregate 2: GlobalAdministratorsAggregate** (Singleton)
- Identity: `system:tenants:global-administrators` via `TenantIdentity.ForGlobalAdministrators()`
- Scope: Platform-wide administrator roster
- Commands: BootstrapGlobalAdmin, SetGlobalAdministrator, RemoveGlobalAdministrator
- State shape: Administrators (HashSet<UserId>), Bootstrapped flag
- Design note: BootstrapGlobalAdmin rejects if any admin exists (one-time operation); SetGlobalAdministrator is idempotent (always succeeds)

**Rationale for separation:** Tenant aggregates have per-tenant lifecycles and transactional boundaries. Global admins are a platform-level singleton with different consistency requirements. Both live under platform tenant "system" but maintain separate event streams and DAPR actor instances.

_Source: Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs, domain model analysis_
_Reference: [Effective Aggregate Design by Vaughn Vernon](https://github.com/heynickc/awesome-ddd)_

### Design Principle: Pure Function Domain Processing

The Hexalith.EventStore architecture enforces a **pure function** pattern for domain logic:

```
Handle(Command, State?) → DomainResult(Events[] | RejectionEvents[] | Empty)
```

**Key properties:**
1. **No side effects** — Handle methods must not call databases, APIs, or external services
2. **Deterministic** — Same command + state always produces same events
3. **Testable in isolation** — No DI, no mocking needed — just call Handle() with test data
4. **Three outcomes only:**
   - `DomainResult.Success(events)` — command accepted, state will change
   - `DomainResult.Rejection(rejections)` — business rule violated, no state change
   - `DomainResult.NoOp()` — command acknowledged but no change needed (idempotent operations)

**Apply methods are equally pure:** `Apply(Event) → void` — mutates state in place during replay, no external dependencies.

_This pattern aligns with the functional core / imperative shell architecture: domain logic is pure, infrastructure (actors, state stores, pub/sub) is handled by the framework._
_Source: EventStore.Client/Aggregates/EventStoreAggregate.cs_
_Reference: [CQRS and Event Sourcing in .NET](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/apply-simplified-microservice-cqrs-ddd-patterns)_

### Design Principle: Rejection Events Over Exceptions

The domain model uses **rejection events** (`IRejectionEvent`) instead of throwing domain exceptions:

| Pattern | Hexalith Approach | Traditional Approach |
|---------|-------------------|---------------------|
| Duplicate tenant | `DomainResult.Rejection([TenantAlreadyExistsRejection])` | `throw TenantAlreadyExistsException` |
| Missing tenant | `DomainResult.Rejection([TenantNotFoundRejection])` | `throw TenantNotFoundException` |
| Disabled tenant | `DomainResult.Rejection([TenantDisabledRejection])` | `throw InvalidOperationException` |
| Unauthorized role change | `DomainResult.Rejection([RoleEscalationRejection])` | `throw UnauthorizedAccessException` |

**Benefits:**
- Rejection events are persisted and auditable (same as success events)
- The actor pipeline handles rejections uniformly (no try/catch branching)
- Rejection events carry diagnostic data (e.g., `AttemptedRole`, `CurrentCount`/`MaxAllowed`)
- Client receives structured rejection data (not error messages)

**Constraint:** `DomainResult` validates that events cannot mix success and rejection types — a command either succeeds, is rejected, or is a no-op.

_Source: EventStore.Contracts/Results/DomainResult.cs, Hexalith.Tenants.Contracts/Events/Rejections/_

### Scalability: Snapshot-Based State Rehydration

Hexalith.EventStore uses a **snapshot-first, tail-only replay** strategy for state rehydration:

```
State Rehydration Flow:
1. Load snapshot (if exists) → base state + sequence number
2. Load events AFTER snapshot sequence → tail events only
3. Apply tail events to snapshot state → current state
4. If no snapshot: replay ALL events from beginning
```

**Snapshot creation** is configurable per domain (via 5-layer cascade configuration):
- Created after event persistence when threshold criteria met
- Stored alongside event stream in DAPR state store
- Key pattern: `{tenant}:{domain}:{aggregateId}:snapshot`

**Best practice alignment:** Snapshots are a performance optimization, not a correctness requirement. The system works correctly without snapshots (full replay). Introduce snapshot thresholds when aggregates accumulate hundreds of events.

_Source: EventStore.Server/Actors/AggregateActor.cs (Step 3 + Step 5b)_
_Reference: [Event Sourcing Snapshotting — Domain Centric](https://domaincentric.net/blog/event-sourcing-snapshotting), [Snapshots in Event Sourcing — Kurrent](https://www.kurrent.io/blog/snapshots-in-event-sourcing)_

### Multi-Tenancy Architecture: Platform Tenant Model

The Tenants domain uses a **platform tenant** model where managed tenants are aggregates WITHIN the platform:

```
Platform Tenant: "system"
├── Domain: "tenants"
│   ├── Aggregate: "acme-corp"    → TenantAggregate (manages tenant "acme-corp")
│   ├── Aggregate: "contoso"      → TenantAggregate (manages tenant "contoso")
│   ├── Aggregate: "fabrikam"     → TenantAggregate (manages tenant "fabrikam")
│   └── Aggregate: "global-administrators" → GlobalAdministratorsAggregate (singleton)
```

**Key design decisions:**
1. **DefaultTenantId = "system"** — All tenant management operations run under the platform tenant, NOT under the managed tenant
2. **TenantId in event payload** — Because the `CommandEnvelope.TenantId` = "system", the managed tenant ID is carried in the event payload (e.g., `TenantCreated.TenantId`)
3. **AggregateId = managed tenant ID** — The managed tenant's ID becomes the AggregateId within the "system" tenant

This differs from how other microservices use tenants: a Sales microservice would use `CommandEnvelope.TenantId = "acme-corp"` to scope operations. The Tenants microservice manages tenant metadata at the platform level.

_Source: Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs_
_Reference: [Multi-Tenant Event Sourcing — Marten](https://martendb.io/events/multitenancy.html), [Azure Multi-Tenant Messaging](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/approaches/messaging)_

### Data Architecture: Command → Event Mapping

The domain model defines a **12-command → 11-event-type** mapping with specific design choices:

| Command | Success Event(s) | Rejection Event(s) |
|---------|------------------|---------------------|
| CreateTenant | TenantCreated | TenantAlreadyExistsRejection |
| UpdateTenant | TenantUpdated | TenantNotFoundRejection |
| EnableTenant | TenantEnabled | TenantNotFoundRejection |
| DisableTenant | TenantDisabled | TenantNotFoundRejection |
| AddUserToTenant | UserAddedToTenant | TenantNotFoundRejection, TenantDisabledRejection, UserAlreadyInTenantRejection |
| RemoveUserFromTenant | UserRemovedFromTenant | TenantNotFoundRejection, UserNotInTenantRejection |
| ChangeUserRole | UserRoleChanged | TenantNotFoundRejection, UserNotInTenantRejection, RoleEscalationRejection |
| SetTenantConfiguration | TenantConfigurationSet | TenantNotFoundRejection, ConfigurationLimitExceededRejection |
| RemoveTenantConfiguration | TenantConfigurationRemoved | TenantNotFoundRejection |
| BootstrapGlobalAdmin | GlobalAdministratorSet | GlobalAdminAlreadyBootstrappedRejection |
| SetGlobalAdministrator | GlobalAdministratorSet | _(none — idempotent)_ |
| RemoveGlobalAdministrator | GlobalAdministratorRemoved | _(none — idempotent)_ |

**Design notes:**
- BootstrapGlobalAdmin and SetGlobalAdministrator emit the SAME event type (`GlobalAdministratorSet`) — the difference is in business rules (bootstrap rejects if any admin exists)
- Global admin commands are idempotent where possible (set/remove succeed even if already in desired state)
- Multiple rejection types possible per command (e.g., AddUserToTenant can reject for 3 different reasons)

_Source: Hexalith.Tenants.Contracts/Commands/, Events/, Events/Rejections/_

### Naming Conventions and Code Standards

The domain follows strict naming patterns enforced by convention tests:

| Category | Pattern | Examples |
|----------|---------|----------|
| Commands | `{Verb}{Target}` — verb-first | CreateTenant, AddUserToTenant, BootstrapGlobalAdmin |
| Success Events | `{Target}{PastVerb}` — past tense | TenantCreated, UserAddedToTenant, GlobalAdministratorSet |
| Rejection Events | `{Target}{Reason}Rejection` — Rejection suffix | TenantNotFoundRejection, RoleEscalationRejection |
| Enums | PascalCase values | TenantOwner, TenantContributor, TenantReader |

**Enforced by tests:**
- All commands must start with approved verb prefixes (Create, Update, Disable, Enable, Add, Remove, Change, Set, Bootstrap)
- All success events must contain past-tense verbs (Created, Updated, Disabled, Enabled, Added, Removed, Changed, Set)
- All rejection events must end with "Rejection" suffix
- All event types must have a `TenantId` property of type `string`

_Source: Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs, EventSerializationTests.cs_

### Deployment Architecture: Aspire + DAPR Composition

```
┌─────────────────────────────────────────────────────┐
│  .NET Aspire AppHost (Orchestrator)                  │
│                                                      │
│  ┌─────────────────────┐  ┌──────────────────────┐  │
│  │ CommandApi Service   │  │ DAPR Sidecar         │  │
│  │ (ASP.NET Core)       │──│ AppId: "commandapi"  │  │
│  │ - REST Controllers   │  │ - State Store (Redis)│  │
│  │ - MediatR Pipeline   │  │ - Pub/Sub (Redis)    │  │
│  │ - DAPR Actor Host    │  │ - Access Control     │  │
│  │ - SignalR Hub        │  │ - Resiliency Policies│  │
│  └─────────────────────┘  └──────────────────────┘  │
│                                                      │
│  ┌─────────────────────┐  ┌──────────────────────┐  │
│  │ Redis                │  │ OpenTelemetry        │  │
│  │ - Event streams      │  │ - Tracing            │  │
│  │ - Actor state        │  │ - Metrics            │  │
│  │ - Pub/Sub topics     │  │ - Health checks      │  │
│  └─────────────────────┘  └──────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

_Source: Hexalith.Tenants.AppHost/Program.cs, Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs_

### Implementation Blueprint: What Needs to Be Built

**Currently implemented (Contracts layer):**
- 12 command records
- 11 success event records (IEventPayload)
- 8 rejection event records (IRejectionEvent)
- 2 enums (TenantRole, TenantStatus)
- TenantIdentity helper
- Serialization + naming convention tests

**Not yet implemented (Server layer — currently empty):**

1. **TenantState** — Mutable state class with Apply methods for all tenant events
2. **TenantAggregate** — `EventStoreAggregate<TenantState>` with Handle methods for 9 tenant commands
3. **GlobalAdministratorsState** — Mutable state class with Apply methods for admin events
4. **GlobalAdministratorsAggregate** — `EventStoreAggregate<GlobalAdministratorsState>` with Handle methods for 3 admin commands
5. **Projections** — Read model classes for queries (TenantDetails, TenantList, etc.)
6. **Query contracts** — `IQueryContract` implementations for each query type
7. **DI registration** — `AddEventStore()` in CommandApi Program.cs
8. **Unit tests** — Aggregate behavior tests in Server.Tests

_Source: Codebase analysis — Hexalith.Tenants.Server project contains only .csproj file, no C# source_

## Implementation Approaches and Technology Adoption

### Implementation Strategy: Bottom-Up, Test-First

The recommended implementation order follows a **bottom-up approach** — state classes first (pure Apply methods), then aggregates (Handle methods), then projections, then DI wiring:

**Phase 1: State Classes (Pure Event Application)**

```csharp
// src/Hexalith.Tenants.Server/State/TenantState.cs
public sealed class TenantState
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TenantStatus Status { get; private set; }
    public Dictionary<string, TenantRole> Users { get; private set; } = new();
    public Dictionary<string, string> Configuration { get; private set; } = new();
    public DateTimeOffset CreatedAt { get; private set; }

    // Apply methods — one per success event type
    public void Apply(TenantCreated e) { TenantId = e.TenantId; Name = e.Name; Description = e.Description; Status = TenantStatus.Active; CreatedAt = e.CreatedAt; }
    public void Apply(TenantUpdated e) { Name = e.Name; Description = e.Description; }
    public void Apply(TenantEnabled e) { Status = TenantStatus.Active; }
    public void Apply(TenantDisabled e) { Status = TenantStatus.Disabled; }
    public void Apply(UserAddedToTenant e) { Users[e.UserId] = e.Role; }
    public void Apply(UserRemovedFromTenant e) { Users.Remove(e.UserId); }
    public void Apply(UserRoleChanged e) { Users[e.UserId] = e.NewRole; }
    public void Apply(TenantConfigurationSet e) { Configuration[e.Key] = e.Value; }
    public void Apply(TenantConfigurationRemoved e) { Configuration.Remove(e.Key); }
}
```

```csharp
// src/Hexalith.Tenants.Server/State/GlobalAdministratorsState.cs
public sealed class GlobalAdministratorsState
{
    public HashSet<string> Administrators { get; private set; } = new();
    public bool Bootstrapped { get; private set; }

    public void Apply(GlobalAdministratorSet e) { Administrators.Add(e.UserId); Bootstrapped = true; }
    public void Apply(GlobalAdministratorRemoved e) { Administrators.Remove(e.UserId); }
}
```

**Phase 2: Aggregates (Command Handling)**

```csharp
// src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs
public sealed class TenantAggregate : EventStoreAggregate<TenantState>
{
    public static DomainResult Handle(CreateTenant cmd, TenantState? state)
        => state is not null
            ? DomainResult.Rejection([new TenantAlreadyExistsRejection(cmd.TenantId)])
            : DomainResult.Success([new TenantCreated(cmd.TenantId, cmd.Name, cmd.Description, DateTimeOffset.UtcNow)]);

    public static DomainResult Handle(DisableTenant cmd, TenantState? state)
        => state is null
            ? DomainResult.Rejection([new TenantNotFoundRejection(cmd.TenantId)])
            : state.Status == TenantStatus.Disabled
                ? DomainResult.NoOp()
                : DomainResult.Success([new TenantDisabled(cmd.TenantId, DateTimeOffset.UtcNow)]);

    public static DomainResult Handle(AddUserToTenant cmd, TenantState? state)
    {
        if (state is null) return DomainResult.Rejection([new TenantNotFoundRejection(cmd.TenantId)]);
        if (state.Status == TenantStatus.Disabled) return DomainResult.Rejection([new TenantDisabledRejection(cmd.TenantId)]);
        if (state.Users.ContainsKey(cmd.UserId)) return DomainResult.Rejection([new UserAlreadyInTenantRejection(cmd.TenantId, cmd.UserId)]);
        return DomainResult.Success([new UserAddedToTenant(cmd.TenantId, cmd.UserId, cmd.Role)]);
    }
    // ... remaining Handle methods follow same pattern
}
```

**Phase 3: DI Registration**

```csharp
// src/Hexalith.Tenants.CommandApi/Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddEventStore();  // Auto-discovers TenantAggregate, GlobalAdministratorsAggregate
var app = builder.Build();
app.UseEventStore();  // Resolves cascade configuration
app.Run();
```

_Source: Counter sample (EventStore.Sample/Counter/) as reference implementation_
_Reference: [Testing Event Sourcing — Event-Driven.io](https://event-driven.io/en/testing_event_sourcing/)_

### Testing Strategy: Given-When-Then for Event-Sourced Aggregates

Event-sourced aggregates are **exceptionally testable** due to their pure-function nature. The recommended testing pattern follows Given/When/Then:

```csharp
// Given: a stream of past events (state reconstruction)
// When: a command is issued
// Then: specific events (or rejections) are produced

[Fact]
public async Task CreateTenant_WhenTenantDoesNotExist_ProducesTenantCreated()
{
    // Arrange
    var aggregate = new TenantAggregate();
    var command = CreateCommand(new CreateTenant("acme", "Acme Corp", "A test tenant"));

    // Act
    DomainResult result = await aggregate.ProcessAsync(command, currentState: null);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Events.Count.ShouldBe(1);
    var evt = result.Events[0].ShouldBeOfType<TenantCreated>();
    evt.TenantId.ShouldBe("acme");
    evt.Name.ShouldBe("Acme Corp");
}

[Fact]
public async Task CreateTenant_WhenTenantAlreadyExists_ProducesRejection()
{
    // Arrange — Given a tenant already exists
    var aggregate = new TenantAggregate();
    var existingState = new TenantState();
    existingState.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));

    var command = CreateCommand(new CreateTenant("acme", "Acme Corp", null));

    // Act
    DomainResult result = await aggregate.ProcessAsync(command, existingState);

    // Assert
    result.IsRejection.ShouldBeTrue();
    result.Events[0].ShouldBeOfType<TenantAlreadyExistsRejection>();
}

[Fact]
public async Task AddUserToTenant_WhenTenantDisabled_ProducesRejection()
{
    // Arrange — Given tenant exists but is disabled
    var aggregate = new TenantAggregate();
    var state = new TenantState();
    state.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));
    state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

    var command = CreateCommand(new AddUserToTenant("acme", "user1", TenantRole.TenantReader));

    // Act
    DomainResult result = await aggregate.ProcessAsync(command, state);

    // Assert
    result.IsRejection.ShouldBeTrue();
    result.Events[0].ShouldBeOfType<TenantDisabledRejection>();
}

// Helper
private static CommandEnvelope CreateCommand<T>(T command) where T : notnull
    => new("system", "tenants", command is BootstrapGlobalAdmin or SetGlobalAdministrator or RemoveGlobalAdministrator
            ? "global-administrators" : ((dynamic)command).TenantId,
        typeof(T).Name, JsonSerializer.SerializeToUtf8Bytes(command),
        Guid.NewGuid().ToString(), null, "test-user", null);
```

**Test categories:**
1. **Unit tests** — Aggregate Handle methods with prepared state (no infrastructure)
2. **State replay tests** — Verify Apply methods produce correct state from event sequences
3. **Serialization tests** — Already exist in Contracts.Tests (JSON round-trip for all events)
4. **Convention tests** — Already exist (naming, TenantId property enforcement)
5. **Integration tests** — DAPR actor pipeline with Testcontainers (Redis)

_No mocking needed for domain logic tests — aggregates are pure functions._
_Source: EventStore.Sample.Tests/QuickstartSmokeTest.cs (reference pattern)_
_Reference: [Testing Event-Sourced Aggregates — buildplease.com](https://buildplease.com/pages/fpc-13/), [Testing Domain with Event Sourcing — CodeOpinion](https://codeopinion.com/testing-your-domain-when-event-sourcing/)_

### Development Workflow: Local DAPR + Aspire

The development workflow leverages .NET Aspire for local orchestration:

```
Developer Workflow:
1. `dotnet run --project src/Hexalith.Tenants.AppHost`
   → Aspire starts CommandApi + DAPR sidecar + Redis
   → Dashboard at https://localhost:XXXX (auto-detected)

2. DAPR sidecar auto-provisions:
   → State store (in-memory for dev, Redis for integration tests)
   → Pub/Sub (Redis)
   → Access control policies
   → Resiliency policies

3. Commands submitted via:
   → HTTP POST to http://localhost:{port}/api/v1/commands
   → DAPR sidecar routes to aggregate actors
   → Events persisted + published automatically

4. Aspire dashboard provides:
   → OpenTelemetry traces (full request→actor→event pipeline)
   → Health checks (/health, /alive, /ready)
   → Service discovery
   → Resource status monitoring
```

**CI/CD considerations:**
- Unit tests (aggregates, state): No infrastructure needed — run in any CI environment
- Integration tests: Testcontainers spins up Redis + DAPR for full pipeline tests
- DAPR actors require `dapr init --slim` for CI environments (no Docker daemon needed for unit tests)

_Source: [DAPR .NET Aspire Integration](https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-integrations/dotnet-development-dapr-aspire/), [Aspire DAPR Integration](https://aspire.dev/integrations/frameworks/dapr/)_

### Risk Assessment and Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Event schema evolution** | Breaking changes to event records break replay | Use `MessageType` versioning (v1, v2); maintain backward-compatible deserialization |
| **Eventual consistency** | Read models may show stale data after commands | Two-gate ETag caching + SignalR notifications minimize staleness window |
| **Aggregate explosion** | Too many events per aggregate degrades replay performance | Snapshot strategy with configurable thresholds (5-layer cascade) |
| **Missing rejection cases** | Unhandled business rules bypass validation | Convention tests enforce all commands have corresponding rejection event types |
| **Cross-aggregate operations** | Need to coordinate Tenant + GlobalAdmin aggregates | Use process managers/sagas (DAPR pub/sub event handlers) for cross-aggregate workflows |
| **DAPR dependency** | Actor framework ties infrastructure to DAPR | Clean separation: domain logic in Contracts/Server has zero DAPR dependencies |

_Source: [Lessons from the Trenches — CQRS/ES](https://www.ashrafmageed.com/cqrs-eventsourcing-and-the-cost-of-tooling-constraints/), [Lessons Learned Building Distributed Systems — HackerNoon](https://hackernoon.com/lessons-ive-learned-building-distributed-systems-with-cqrs-and-event-sourcing-ece284ecc1a1)_

## Technical Research Recommendations

### Implementation Roadmap

**Sprint 1: Core Domain Logic (Server Layer)**

1. Create `TenantState` with Apply methods for all 9 tenant events
2. Create `GlobalAdministratorsState` with Apply methods for 2 admin events
3. Create `TenantAggregate : EventStoreAggregate<TenantState>` with Handle methods for 9 commands
4. Create `GlobalAdministratorsAggregate : EventStoreAggregate<GlobalAdministratorsState>` with Handle methods for 3 commands
5. Write unit tests for all Handle methods (success, rejection, no-op paths)

**Sprint 2: Query Layer**

6. Define `IQueryContract` implementations (GetTenantDetails, ListTenants, etc.)
7. Create projection read models (TenantDetailsReadModel, TenantListReadModel)
8. Create projection classes extending `EventStoreProjection<TReadModel>`
9. Write projection replay tests

**Sprint 3: API Integration**

10. Wire `AddEventStore()` and `UseEventStore()` in CommandApi Program.cs
11. Verify end-to-end command flow: HTTP → MediatR → Actor → Events → Projections
12. Write integration tests with Testcontainers (Redis + DAPR)
13. Verify SignalR projection change notifications

### Technology Stack Recommendations

| Layer | Technology | Status |
|-------|-----------|--------|
| Domain contracts | C# records, IEventPayload, IRejectionEvent | Done |
| State classes | Mutable classes with Apply methods | To implement |
| Aggregates | EventStoreAggregate<TState> with static Handle methods | To implement |
| Projections | EventStoreProjection<TReadModel> | To implement |
| Queries | IQueryContract with static abstract members | To implement |
| DI Registration | AddEventStore() / UseEventStore() | To wire |
| Unit Tests | xUnit + Shouldly (Given/When/Then pattern) | To implement |
| Integration Tests | Testcontainers + DAPR | To implement |

### Success Metrics

- All 12 commands have corresponding Handle methods with unit tests
- All success paths produce correct events verified by assertions
- All rejection paths verified for each applicable rejection event type
- State replay tests verify Apply methods produce correct state from event sequences
- Naming convention tests continue to pass as Server code is added
- Integration tests verify full command→event→projection pipeline
- Aspire AppHost starts cleanly with all DAPR components

## Research Synthesis

### Executive Summary

This research establishes the complete technical foundation for implementing the Hexalith.Tenants microservice on the Hexalith.EventStore CQRS/Event Sourcing framework. The Tenants domain is well-positioned for implementation: all 12 commands, 11 success events, 8 rejection events, and identity helpers are fully defined in the Contracts layer. The Server layer (aggregates, state, projections) is the primary implementation gap.

The Hexalith.EventStore framework provides a reflection-driven, convention-over-configuration approach where domain developers need only define state classes with `Apply(Event)` methods and aggregate classes with `Handle(Command, State?)` methods. The framework handles all infrastructure concerns — DAPR actor lifecycle, event persistence, pub/sub publishing, snapshot management, and crash recovery — through a 5-step checkpointed pipeline in the AggregateActor.

The Tenants domain uses a distinctive **platform tenant model** where all tenant management operations run under `TenantId = "system"`, with managed tenant IDs carried as aggregate IDs and event payload properties. This is architecturally significant because it differs from how other Hexalith microservices consume tenancy.

**Key Technical Findings:**

- Hexalith.EventStore uses reflection-based `Handle`/`Apply` method discovery — no manual registration or interface implementation beyond extending `EventStoreAggregate<TState>`
- The two-aggregate design (TenantAggregate per managed tenant + GlobalAdministratorsAggregate singleton) follows DDD aggregate boundary best practices
- Domain logic is a pure function `(Command, State?) → Events` with zero infrastructure dependencies — exceptionally testable
- The 5-layer cascade configuration system provides flexible resource naming from conventions through explicit overrides
- Multi-tenancy isolation is enforced at 8 layers from JWT claims through colon-separated state store keys to DAPR actor scoping

**Strategic Recommendations:**

1. Implement state classes first (TenantState, GlobalAdministratorsState) — they are the foundation for both aggregates and projections
2. Use static Handle methods on aggregates (as demonstrated in the Counter sample) for clean, testable domain logic
3. Follow the Given/When/Then testing pattern that exploits event sourcing's pure-function nature
4. Wire `AddEventStore()` / `UseEventStore()` in CommandApi Program.cs to leverage auto-discovery
5. Defer projection and query implementation to Sprint 2 — command-side is the critical path

### Table of Contents

1. Technical Research Scope Confirmation
2. Technology Stack Analysis
3. Integration Patterns Analysis
4. Architectural Patterns and Design
5. Implementation Approaches and Technology Adoption
6. Research Synthesis (this section)

### Source Documentation

**Primary Sources (Codebase Analysis):**

- Hexalith.EventStore submodule — complete source analysis of Contracts, Client, Server, CommandApi, SignalR packages
- Hexalith.Tenants project — all Contracts (Commands, Events, Identity, Enums), AppHost, Aspire, ServiceDefaults
- Counter sample — reference implementation (EventStore.Sample)
- Test projects — QuickstartSmokeTest, EventStoreAggregateTests, EventStoreProjectionTests

**Web Sources:**

- [Hexalith GitHub Organization](https://github.com/Hexalith/)
- [Hexalith Documentation](https://hexalith.readthedocs.io/en/latest/)
- [DAPR Actors Overview](https://docs.dapr.io/developing-applications/building-blocks/actors/actors-overview/)
- [DAPR Pub/Sub Overview](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/)
- [DAPR State Management](https://docs.dapr.io/developing-applications/building-blocks/state-management/state-management-overview/)
- [DAPR .NET Aspire Integration](https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-integrations/dotnet-development-dapr-aspire/)
- [Aspire DAPR Integration](https://aspire.dev/integrations/frameworks/dapr/)
- [CQRS Pattern — Azure Architecture](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Event Sourcing Pattern — Azure Architecture](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)
- [Applying CQRS/DDD in .NET Microservices — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/apply-simplified-microservice-cqrs-ddd-patterns)
- [Event Sourcing Snapshotting — Domain Centric](https://domaincentric.net/blog/event-sourcing-snapshotting)
- [Snapshots in Event Sourcing — Kurrent](https://www.kurrent.io/blog/snapshots-in-event-sourcing)
- [Testing Event Sourcing — Event-Driven.io](https://event-driven.io/en/testing_event_sourcing/)
- [Testing Domain with Event Sourcing — CodeOpinion](https://codeopinion.com/testing-your-domain-when-event-sourcing/)
- [Multi-Tenant Event Sourcing — Marten](https://martendb.io/events/multitenancy.html)
- [Multi-Tenant Architecture Guide 2026](https://www.future-processing.com/blog/multi-tenant-architecture/)
- [Lessons from the Trenches — CQRS/ES](https://www.ashrafmageed.com/cqrs-eventsourcing-and-the-cost-of-tooling-constraints/)
- [Event Driven Architecture in 2025 — Growin](https://www.growin.com/blog/event-driven-architecture-scale-systems-2025/)

### Research Quality Assessment

- **Confidence Level:** High — all framework patterns verified against actual source code
- **Coverage:** Complete for command/event/aggregate/projection architecture; partial for production deployment patterns (DAPR in Kubernetes)
- **Limitations:** Hexalith.EventStore has no public NuGet packages or external documentation — all findings derived from submodule source analysis
- **Verification:** Web sources confirm industry alignment of CQRS/ES, DAPR actor, and multi-tenant patterns used by the framework

---

**Technical Research Completion Date:** 2026-03-15
**Research Methodology:** Deep codebase analysis + web verification
**Source Verification:** All technical facts cited with current sources
**Confidence Level:** High — based on actual source code analysis and multiple authoritative sources

_This comprehensive technical research document serves as the authoritative reference for implementing the Hexalith.Tenants microservice on the Hexalith.EventStore framework, providing strategic insights and concrete implementation guidance for the development team._
