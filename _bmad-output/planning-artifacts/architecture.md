---
stepsCompleted:
  - 1
  - 2
  - 3
  - 4
  - 5
  - 6
  - 7
  - 8
status: 'amended'
completedAt: '2026-03-07'
amendedAt: '2026-03-25'
lastStep: 8
inputDocuments:
  - prd.md
  - product-brief-Hexalith.Tenants-2026-03-06.md
  - prd-validation-report.md
  - Hexalith.EventStore/docs/concepts/architecture-overview.md
  - Hexalith.EventStore/docs/concepts/command-lifecycle.md
  - Hexalith.EventStore/docs/concepts/event-envelope.md
  - Hexalith.EventStore/docs/concepts/identity-scheme.md
  - Hexalith.EventStore/docs/concepts/choose-the-right-tool.md
  - Hexalith.EventStore/docs/reference/nuget-packages.md
  - Hexalith.EventStore/docs/reference/command-api.md
  - Hexalith.EventStore/docs/guides/security-model.md
  - Hexalith.EventStore/docs/guides/dapr-component-reference.md
  - Hexalith.EventStore/docs/guides/configuration-reference.md
  - Hexalith.EventStore/docs/guides/deployment-progression.md
  - Hexalith.EventStore/docs/getting-started/first-domain-service.md
  - ux-design-specification.md
workflowType: 'architecture'
project_name: 'Hexalith.Tenants'
user_name: 'Jerome'
date: '2026-03-07'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

### Requirements Overview

**Functional Requirements:**

65 FRs across 9 categories:

| Category | FRs | Architectural Significance |
|----------|-----|---------------------------|
| Tenant Lifecycle Management | FR1-FR5 | Core aggregate design -- CreateTenant, UpdateTenant, DisableTenant, EnableTenant commands with domain events |
| User-Role Management | FR6-FR12 | Most complex aggregate behavior -- 3 fixed roles, escalation boundaries, duplicate detection, optimistic concurrency |
| Global Administration | FR13-FR18 | Cross-tenant access model, bootstrap problem for first deployment |
| Tenant Configuration | FR19-FR24 | Schema-free key-value store within tenant aggregate, size/count limits |
| Tenant Discovery & Query | FR25-FR30 | Centralized read model projection with cursor-based pagination -- separate from consuming service projections |
| Role Behavior | FR31-FR34 | Authorization semantics -- Reader/Contributor/Owner hierarchy, cross-tenant isolation |
| Event-Driven Integration | FR35-FR42 | DAPR pub/sub, CloudEvents 1.0, topic naming, idempotent processing guidance |
| Developer Experience & Packaging | FR43-FR49 | 5 NuGet packages, DI registration, testing fakes with production-parity domain logic |
| Documentation & Adoption | FR59-FR65 | Quickstart, event contract reference, sample consuming service, "aha moment" demo |

**Non-Functional Requirements:**

24 NFRs driving architectural decisions:

- **Performance (NFR1-4):** 50ms p95 for commands/queries/events, 10ms for test fakes
- **Security (NFR5-10):** Zero cross-tenant leaks, role escalation enforcement, 100% branch coverage on isolation/auth
- **Scalability (NFR11-13):** 1K tenants x 500 users, stateless horizontal scaling, 30s startup reconstruction for 500K events
- **Integration (NFR14-19):** CloudEvents 1.0, DAPR pub/sub/state store abstraction, graceful degradation, backward-compatible contracts post-v1.0
- **Reliability (NFR20-23):** Event store as source of truth, atomic command/event storage, 99.9% availability
- **Accessibility (NFR24):** English-only MVP, WCAG 2.1 AA for Phase 2 Admin UI

**Scale & Complexity:**

- Primary domain: Backend API / Event-sourced domain service
- Complexity level: Medium-high
- Estimated architectural components: ~12 (5 NuGet packages + Hexalith.Tenants + AppHost + ServiceDefaults + test projects + sample)

### Technical Constraints & Dependencies

- **Must follow EventStore patterns exactly:** `Handle/Apply` pure functions, `EventStoreAggregate<TState>` base class, assembly scanning auto-discovery, DAPR actor model, event envelope metadata
- **Identity scheme:** Commands use `{tenant}:{domain}:{aggregateId}` -- the "domain" will be `tenants` and the aggregateId must encode the specific tenant instance
- **DAPR building blocks:** State store, pub/sub, actors, service invocation -- all infrastructure access through sidecars only
- **Package dependency chain:** Hexalith.Tenants.Contracts depends on Hexalith.EventStore.Contracts; Server depends on EventStore.Server; etc.
- **.NET 10 SDK** (pinned via `global.json`), modern XML solution format (`.slnx`)
- **EventStore submodule** already present in repository at `Hexalith.EventStore/`

### Cross-Cutting Concerns Identified

1. **Multi-tenant isolation** -- Every component (aggregate, projections, API, events) must enforce tenant boundaries. The tenant service manages tenants *for other services*, but it also operates within EventStore's own multi-tenant model
2. **Authorization model** -- Two layers: (a) EventStore's JWT-based authorization for API access, (b) Tenant domain's own RBAC (TenantOwner/Contributor/Reader/GlobalAdmin) for business logic authorization within the aggregate
3. **Event contract stability** -- Event schemas become ecosystem contracts post-v1.0; breaking changes affect all consuming services
4. **Read model architecture** -- The tenant service needs its own query endpoints (FR25-30) -- this goes beyond the pure command-side that EventStore domain services typically handle
5. **Testing parity** -- In-memory fakes must execute identical domain logic (FR47), requiring architectural separation that enables both production and test execution paths
6. **Global administrator scope** -- GlobalAdmin is cross-tenant, which introduces a concept that doesn't naturally fit the per-tenant-scoped aggregate model -- needs careful aggregate boundary design

### Advanced Elicitation Findings

#### Aggregate Boundaries (First Principles + ADR Analysis)

Two aggregates identified through first principles analysis and architectural debate:

- **TenantAggregate** -- Manages lifecycle (Create/Update/Disable/Enable), user-role management (Add/Remove/ChangeRole), and tenant configuration (Set/Remove). These share invariants: duplicate user detection (FR9), role escalation boundaries (FR10), configuration limits (FR23), and disabled tenant rejection (FR3/NFR8). Flat state design following EventStore conventions (~25KB at max capacity of 500 users + 100 config keys).
- **GlobalAdministratorAggregate** -- Manages cross-tenant admin roles (Set/Remove GlobalAdministrator) and bootstrap mechanism (FR17-18). Singleton aggregate with a small state (set of user IDs). Separate from TenantAggregate because GlobalAdmin is a platform-level concept that doesn't belong to any specific managed tenant.

#### Identity Mapping (ADR)

EventStore identity scheme mapping for tenant management:

- **Platform tenant context:** `system` (configurable) -- the tenant service is a platform-level service, not owned by any managed tenant
- **Domain:** `tenants`
- **AggregateId:** Managed tenant ID (e.g., `acme-corp`) for TenantAggregate, or `global-administrators` for GlobalAdministratorAggregate
- **Actor IDs:** `system:tenants:acme-corp`, `system:tenants:global-administrators`
- **Pub/Sub topic:** `system.tenants.events` -- single topic for all tenant events; consuming services filter by event type
- **JWT claims:** Tenant service operations require `eventstore:tenant` claim for `system` -- deployment configuration concern

#### Critical Risks Identified (Pre-mortem)

| Risk | Prevention |
|------|-----------|
| Consuming services can't identify which managed tenant an event belongs to (envelope `tenantId` = `system`) | Design all event payloads to include `TenantId` as a top-level field identifying the managed tenant |
| Read model serves stale data after command completion | Document eventual consistency; consider read-after-write pattern for tenant service's own API |
| Snapshot interval too large for NFR13 (30s startup for 500K events) | Configure snapshot interval ~50 events for tenant domain; test NFR13 with seeded data |
| Event schema breaks consuming services pre-v1.0 | Document contract instability; use `eventTypeName` + `domainServiceVersion` for version-aware deserialization |
| Bootstrap creates duplicate global administrators on concurrent startup | Actor model guarantees single-threaded execution; aggregate Handle rejects if any GlobalAdministratorSet event exists |

#### Security Model (Red Team Analysis)

| Attack Vector | Defense | Hardening |
|--------------|---------|-----------|
| Cross-tenant data access via read model queries | Query endpoints validate requesting user has role in target tenant (or is GlobalAdmin) | Every read model query includes tenant authorization check |
| Role escalation via concurrent commands | Actor is single-threaded -- concurrent commands serialize; FR10 checked in Handle method | No additional hardening needed -- actor model prevents races |
| Forging bootstrap command after initial setup | FR18 -- aggregate rejects if GlobalAdmin already exists in state | Bootstrap must not be a public API endpoint; startup config or CLI only |
| Event subscription exposes all managed tenant data | DAPR pub/sub topic `system.tenants.events` contains all tenant events | Deployment concern -- DAPR subscription scoping and network policies; document trust model |
| Disabled tenant commands accepted by consuming services | Cross-aggregate timing window (Journey 4, FR64); tenant aggregate rejects immediately (NFR8) | MVP documents the window; Phase 2 auth plugin provides synchronous enforcement |

#### State Design (Tree of Thoughts)

Selected: **Flat state class** -- simplest approach, follows EventStore conventions (CounterState, InventoryState patterns).

```
TenantState {
  string TenantId
  string Name
  string? Description
  TenantStatus Status  // Active, Disabled
  Dictionary<string, TenantRole> Members  // UserId -> Role
  Dictionary<string, string> Configuration  // Key -> Value
  DateTimeOffset CreatedAt
}
```

- ~25KB at max capacity (500 users + 100 config keys)
- All invariants checkable in-state: duplicate users, role escalation, config limits, disabled status
- Dictionary.Count is O(1) -- no need for pre-computed counters
- Snapshot-friendly for fast actor rehydration

#### Party Mode Review Findings

Cross-functional panel review (Architect, Dev, PM, QA, Test Architect) surfaced 5 additional architectural constraints:

**1. `system` tenant is a deployment prerequisite**

The platform tenant `system` must be pre-configured in EventStore's domain service registration (`appsettings.json`) and in the identity provider's JWT claims (`eventstore:tenant` = `system`). This is not dynamically provisioned -- it is a static deployment configuration. The quickstart guide (FR59-60) must include this as a prerequisite validation step.

**2. Low-frequency operation assumption**

The single-actor-per-tenant design serializes all operations (user management, config changes, lifecycle) through one actor. This is explicitly designed for administrative-frequency operations (a few per day per tenant), not high-throughput bulk scenarios. Bulk user import and bulk tenant provisioning are Phase 2 features that may require a different execution path (e.g., batch command processing). This assumption must be documented in the architecture to set correct expectations for consuming developers.

**3. Tenant configuration boundary**

Tenant configuration (FR19-24) is designed for low-frequency administrative settings: billing plans, feature flags that change occasionally, branding overrides. It is NOT designed for real-time feature flags or high-frequency configuration updates. Services needing real-time config should use a dedicated configuration service (e.g., DAPR configuration store directly). This boundary must be documented to prevent misuse.

**4. Aggregate package location -- hard architectural constraint**

Aggregates (TenantAggregate, GlobalAdministratorAggregate) and their state classes MUST NOT live in the Server package. They must live in a package referenceable by both Server (for DAPR actor execution) and Testing (for in-memory fake execution). This is driven by:

- FR47: Testing fakes execute the same domain logic as production
- NFR10: 100% branch coverage on isolation/auth logic requires Tier 1 (unit) testability
- EventStore pattern: `EventStoreAggregate<T>` base class is in Client; domain aggregates reference Client

**Recommended location:** Aggregates and state in `Hexalith.Tenants.Server` project but with the Handle/Apply methods designed as pure static functions testable without DAPR infrastructure. Alternatively, a shared domain project referenced by both Server and Testing. This decision to be finalized in the technology stack step.

**5. Tier 1 testability as architectural constraint**

All Handle and Apply methods must be testable as pure functions: `Handle(Command, State?) -> DomainResult` with no DAPR, no actors, no infrastructure. This is not a testing preference -- it is a hard architectural requirement driven by NFR10 (100% branch coverage on isolation/auth) and FR47 (testing fakes use same domain logic). The aggregate design must enforce this by keeping Handle/Apply as static pure functions following EventStore's convention.

## Starter Template Evaluation

### Primary Technology Domain

.NET 10 backend domain service library — event-sourced microservice with NuGet package distribution. Technology stack is fully determined by the Hexalith.EventStore ecosystem.

### Starter Options Considered

**Option 1: Third-party .NET starter template (dotnet new, Aspire templates)**
Rejected — no existing template matches the Hexalith.EventStore project structure (multi-package NuGet library + DAPR actors + Aspire AppHost + testing fakes). Generic templates would require more removal and restructuring than building from the EventStore reference.

**Option 2: Fork/copy EventStore project structure (Selected)**
The Hexalith.EventStore repository provides a proven, production-ready structure that Hexalith.Tenants must mirror by design. This is not a generic template — it is the ecosystem's established architecture.

### Selected Starter: EventStore Structure Mirror

**Rationale for Selection:**
The PRD specifies that Hexalith.Tenants follows "the same project structure, conventions, and documentation approach as Hexalith.EventStore." The EventStore submodule is already present in the repository. Using it as the structural reference ensures ecosystem consistency and leverages proven patterns.

**Initialization Approach:**
Scaffold the solution by mirroring EventStore's structure with `Hexalith.Tenants` naming. No CLI command — manual scaffolding from the reference project.

**Architectural Decisions Provided by Starter:**

**Language & Runtime:**
- C# on .NET 10 (SDK 10.0.103 via `global.json`, `rollForward: latestPatch`)
- Nullable references enabled globally
- Implicit usings enabled globally
- Warnings as errors enabled

**Build & Versioning:**
- Modern XML solution format (`Hexalith.Tenants.slnx`)
- `Directory.Build.props` for shared project properties
- `Directory.Packages.props` for centralized NuGet package management
- semantic-release for automated SemVer from Conventional Commits (on merge to main, tag prefix `v`)

**Testing Framework:**
- xUnit 2.9.3, Shouldly 4.3.0 (fluent assertions), NSubstitute 5.3.0
- coverlet.collector 6.0.4 for coverage
- Three-tier test architecture (Unit → DAPR → Aspire E2E)

**Code Organization:**
- `src/` — 8 projects (Contracts, Client, Server, Hexalith.Tenants, Aspire, AppHost, ServiceDefaults, Testing)
- `tests/` — 5 test projects (Contracts.Tests, Client.Tests, Server.Tests, Testing.Tests, IntegrationTests)
- `samples/` — Sample consuming service + tests

**Development Experience:**
- `.editorconfig` inherited from EventStore
- GitHub Actions CI/CD
- .NET Aspire AppHost for local development topology

**Note:** Project initialization (solution scaffolding) should be the first implementation story.

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions (Block Implementation):**
1. Read model architecture → EventStoreProjection pattern with DAPR state store
2. Query endpoints → Served from Hexalith.Tenants (single deployable with route groups)
3. Bootstrap mechanism → Startup configuration via appsettings.json, through full MediatR pipeline
4. .NET SDK version → 10.0.103

**Important Decisions (Shape Architecture):**
5. Snapshot interval → 50 events for tenant domain, default 100 for GlobalAdmin
6. Event serialization → System.Text.Json (EventStore standard)
7. Cross-tenant index projections → Separate architectural concern from per-aggregate projections
8. Testing fakes include in-memory projection → Consuming services can test query scenarios

**Deferred Decisions (Post-MVP):**
- Bulk provisioning execution path
- Real-time feature flag service boundary
- QueryApi separation (if scaling demands independent query scaling)

**Resolved (2026-03-15 — EventStore Upgrade Alignment):**
- ~~EventStore authorization plugin integration~~ → Resolved: EventStore now provides `IRbacValidator`/`ITenantValidator` interfaces. Tenants uses claims-based validators; consuming services implement these interfaces using Tenants projections

### Data Architecture

**Event Store (Command Side):**
- Decision: DAPR state store via EventStore's actor model
- Rationale: Follows EventStore's established pattern — DAPR abstracts the underlying storage
- Affects: All aggregate state persistence

**Read Model (Query Side):**
- Decision: `EventStoreProjection<TReadModel>` pattern with DAPR state store
- Rationale: EventStore provides `EventStoreProjection<TReadModel>` base class with reflection-based Apply method discovery. Assembly scanning auto-discovers projections. DAPR pub/sub routes events to projections. Read model stored in convention-named DAPR state store
- Projections needed:
  - `TenantProjection : EventStoreProjection<TenantReadModel>` — per-tenant detail view (users, roles, config, status)
  - `GlobalAdministratorProjection : EventStoreProjection<GlobalAdministratorReadModel>` — global admin list
  - Cross-tenant index projections for ListTenants, GetUserTenants, and audit queries
- Affects: Server, Hexalith.Tenants, Testing

**Cross-Tenant Index Projections (Party Mode Finding):**
- Per-aggregate projections replay events for one aggregate instance. Cross-tenant indexes are materially different — they aggregate data across ALL tenant aggregate instances into a single queryable index
- Implementation: A pub/sub subscription handler receives events from all tenants and maintains shared read model state. `EventStoreProjection<T>` handles the Apply mechanics; the hosting (subscription endpoint + state management) requires explicit design in the CommandApi
- DAPR state store key design constraint: DAPR state store is key-value only (no native query support). For ListTenants with cursor-based pagination (FR30): use a well-known key holding the full tenant list (workable at 1K tenants scale). If query support is needed later, a DAPR state store backend that supports queries (e.g., CosmosDB) can be swapped without code changes
- **Concurrency on index key (Party Mode Validation Finding):** The cross-tenant index key is a shared write target — every `TenantCreated`, `TenantDisabled`, etc. triggers a read-modify-write on the same key. Decision: use ETag-based optimistic concurrency (`StateOptions.Concurrency = ConcurrencyMode.FirstWrite`) with retry logic (max 3 attempts) in the projection handler. Without this, concurrent index updates cause silent data loss. Pattern:
  1. `GET` state with ETag
  2. Modify in-memory list
  3. `PUT` state with ETag
  4. On `409 Conflict` → retry from step 1
  This is critical for test environments with bulk tenant seeding and for production resilience

**Query Pipeline (EventStore Infrastructure — Available for Epic 5):**
- EventStore now provides a built-in query pipeline: `IQueryContract` (typed query contracts with `QueryType`, `Domain`, `ProjectionType`), `IQueryResponse<T>` (typed response wrapper), `SubmitQuery`/`QueryRouter` (MediatR-based dispatch), and `QueriesController` (REST endpoint scaffolding)
- Additionally provides `CachingProjectionActor` for projection state with ETag caching, `ETagActor` for ETag management, and `SelfRoutingETag` for query response caching
- Epic 5 stories should leverage this infrastructure rather than building custom query routing. Query contracts for Tenants (e.g., `GetTenantQuery`, `ListTenantsQuery`) should implement `IQueryContract`

**D4 Revision (2026-03-15 — EventStore Upgrade Alignment):**
- Decision: All three projections (TenantProjection, GlobalAdministratorProjection, TenantIndexProjection) use EventStore's `CachingProjectionActor` with built-in ETag management via `ETagActor`
- Cross-tenant index projection uses `CachingProjectionActor`'s state management with ETag-based retry pattern (replacing manual DAPR state store read-modify-write)
- `IProjectionChangeNotifier` triggers automatic ETag invalidation on projection state changes
- SignalR real-time notifications available via `IProjectionChangedBroadcaster` (optional)
- **Caveat (Party Mode Finding):** Cross-tenant index `CachingProjectionActor` adoption is conditional on verifying the actor supports fan-in event processing (events from ALL tenant aggregates funneling into one projection actor). This is architecturally distinct from per-aggregate projections. Fallback: manual DAPR state store with ETag retry pattern (original D4 design). Verification happens during Epic 5 Story 5.2 implementation
- Rationale: Unified infrastructure, less custom code, automatic ETag caching, consistent with EventStore ecosystem patterns
- Testing: `FakeProjectionActor` and `FakeETagActor` from EventStore.Testing for Tier 1 tests

**Snapshot Strategy:**
- Decision: 50-event interval for tenant domain, default 100 for GlobalAdministratorAggregate
- Rationale: Tenant aggregates grow with user/config additions (~1000 events at max capacity). 50-event snapshots ensure startup replays max 50 events from last snapshot, meeting NFR13. GlobalAdministratorAggregate is a singleton with very low event volume — default 100 is more appropriate
- Configuration: `appsettings.json` → `EventStore:Snapshots:DomainIntervals:tenants = 50`
- Provided by EventStore: SnapshotManager with per-domain configurable intervals (min 10, default 100)

**Event Serialization:**
- Decision: System.Text.Json (EventStore standard)
- Rationale: EventStore uses System.Text.Json throughout (aggregate rehydration, projection replay, event envelope serialization). No alternative to evaluate
- Provided by EventStore: Yes

### Authentication & Security

**Authentication:**
- Decision: JWT Bearer tokens via EventStore's authentication pipeline
- Provided by EventStore: `Microsoft.AspNetCore.Authentication.JwtBearer` + `EventStoreClaimsTransformation`
- Affects: Hexalith.Tenants

**Authorization (Two Layers):**
- Decision: Layer 1 = EventStore JWT-based API access authorization. Layer 2 = Tenant domain RBAC (TenantOwner/Contributor/Reader/GlobalAdmin) enforced in aggregate Handle methods
- Rationale: API-level authorization gates access to the service. Domain-level authorization enforces business rules (who can add users, change roles, manage config)
- Provided by EventStore: Layer 1 (AuthorizationBehavior in MediatR pipeline). Domain-specific: Layer 2

**D8 Revision (2026-03-15 — EventStore Upgrade Alignment): Authorization Model Clarification**
- Decision: Hybrid three-layer authorization (clarified, not fundamentally changed)
  - Layer 1: EventStore's `AuthorizationBehavior` in MediatR pipeline with `ClaimsTenantValidator` and `ClaimsRbacValidator` — validates JWT claims (`eventstore:tenant` = `system`)
  - Layer 2: Domain RBAC in aggregate Handle methods — checks `state.Members[userId]` for TenantOwner/Contributor/Reader permissions. This IS domain logic, not infrastructure auth
  - Layer 3 (guidance for consuming services): Consuming services implement `IRbacValidator` using Tenants projections (via event subscription) for their own domain authorization
- EventStore's `ITenantValidator`/`IRbacValidator` interfaces acknowledged as extension points for consuming services, NOT for Tenants' own authorization (avoids circular dependency)
- Rationale: Tenants is the source of truth for membership/roles — it cannot query itself for authorization. Handle method RBAC is appropriate domain logic. Consuming services use the EventStore authorization interfaces with Tenants-projected state

**Bootstrap Mechanism (FR17-18):**
- Decision: Startup configuration via appsettings.json, executed through the full MediatR pipeline
- Rationale: Hexalith.Tenants reads `Tenants:BootstrapGlobalAdminUserId` on startup and sends `BootstrapGlobalAdmin` command through MediatR (validation, authorization). GlobalAdministratorAggregate rejects if any GlobalAdministratorSet event exists. Zero-touch after first boot. Must go through full pipeline — aggregate rejection is the safety net, not a shortcut
- Affects: Hexalith.Tenants startup, GlobalAdministratorAggregate

### API & Communication Patterns

**Command API:**
- Decision: REST via EventStore's CommandApi pattern (CommandsController)
- Provided by EventStore: SubmitCommandRequest/Response, validation pipeline, error handling
- Affects: Hexalith.Tenants

**Query Endpoints (FR25-30):**
- Decision: Served from Hexalith.Tenants as route groups (single deployable)
- Rationale: At admin-frequency scale (1K tenants), a separate QueryApi deployable adds operational complexity without scaling benefit. Route groups in Hexalith.Tenants (e.g., `/api/commands/*` and `/api/tenants/*`) achieve code-level separation without deployment overhead. Separation into a standalone QueryApi can happen later if scaling demands it
- Party Mode Finding: Winston (Architect) identified that the operational cost of two DAPR-sidecared deployables outweighs CQRS purity at this scale
- Shared startup logic extracted into ServiceDefaults to avoid drift if separation happens later
- Affects: Hexalith.Tenants project structure

**D7 Revision (2026-03-15 — EventStore Upgrade Alignment): Dual-Layer Query Architecture**
- Decision: Dual-layer query architecture
  - Internal: Projections implement `IQueryContract` (typed `QueryType`, `Domain`, `ProjectionType`). Queries dispatched via `SubmitQuery`/`QueryRouter` through MediatR pipeline. `CachingProjectionActor` serves cached results with ETag support
  - External: Thin REST controllers (`GET /api/tenants/*`) translate REST requests into `SubmitQuery` dispatches via MediatR, preserving clean REST API semantics
- Query contracts: `GetTenantQuery`, `ListTenantsQuery`, `GetTenantUsersQuery`, `GetUserTenantsQuery`, `GetTenantAuditQuery` — all implement `IQueryContract`
- Authorization reused: queries flow through same `AuthorizationBehavior` pipeline as commands
- **Testing (Party Mode Finding):** Query contracts should have a reflection-based naming convention test in Contracts.Tests, verifying all `IQueryContract` implementations follow naming conventions — consistent with existing command/event naming tests
- Rationale: Clean REST API externally for developer experience, EventStore query infrastructure internally for ETag caching, authorization reuse, ecosystem consistency

**Event Publishing:**
- Decision: DAPR pub/sub, CloudEvents 1.0, topic `system.tenants.events`
- Rationale: Decided in Step 2 context analysis. All tenant events on single topic; consumers filter by event type
- Provided by EventStore: EventPublisher, TopicNameValidator

**Query Consistency Model (Party Mode Validation Finding):**
- Decision: Eventual consistency with read-after-write mitigation (Option A)
- Rationale: Projections are eventually consistent — a tenant created via `POST /api/commands` may not appear immediately in `GET /api/tenants`. Admin experience requires confirmation, not immediate list visibility
- Pattern: Command response includes the aggregate ID. Client navigates to `GET /api/tenants/{id}` directly. If projection hasn't processed yet, UI shows "processing..." with short poll. No infrastructure changes required
- Affects: Hexalith.Tenants command response shape, consuming service documentation
- Note: FR25-30 in the PRD do not specify the consistency model for queries. This should be clarified in the PRD to prevent test assertions that assume immediate consistency

**Error Handling:**
- Decision: Rejection events via `DomainResult.Rejection()` for business rule violations (FR49-53). Handle methods return `DomainResult.Rejection([new XxxRejection(...)])` instead of throwing domain exceptions. This aligns with EventStore's three-outcome model (Success/Rejection/NoOp), enables idempotency caching of rejections, and provides an audit trail of failed command attempts. Domain exception classes are NOT used — rejection events replace them entirely.
- Rationale: EventStore's `EventStoreAggregate.DispatchCommandAsync()` does not catch exceptions from Handle methods. Thrown exceptions bypass the idempotency record, meaning duplicate commands re-execute domain logic and re-throw. Rejection events are persisted in the idempotency store, giving consistent behavior for duplicate commands.
- Provided by EventStore: `IRejectionEvent` marker interface, `DomainResult.Rejection()` factory method, GlobalExceptionHandler for infrastructure failures only

### Infrastructure & Deployment

**Hosting:**
- Decision: DAPR sidecar + .NET Aspire AppHost orchestration
- Provided by EventStore: AppHost pattern, Aspire extensions, ServiceDefaults

**CI/CD:**
- Decision: GitHub Actions — build, test (Tier 1+2), pack, validate packages, push to NuGet
- Provided by EventStore: CI/CD workflow pattern

**Monitoring:**
- Decision: OpenTelemetry via EventStore's telemetry infrastructure
- Provided by EventStore: EventStoreActivitySource, ServiceDefaults OpenTelemetry configuration

**Scaling:**
- Decision: Stateless horizontal scaling — all state in event store, rebuilt on startup
- Provided by EventStore: Actor model, state rehydration

### Decision Impact Analysis

**Updated Project Structure (Post-Party Mode):**
```
src/
  Hexalith.Tenants.Contracts         # Commands, events, results, identities
  Hexalith.Tenants.Client            # Client abstractions and DI registration
  Hexalith.Tenants.Server            # Aggregates, projections, DAPR integration
  Hexalith.Tenants        # REST command + query gateway, auth, validation, bootstrap
  Hexalith.Tenants.Aspire            # .NET Aspire hosting extensions
  Hexalith.Tenants.AppHost           # Aspire AppHost (DAPR topology orchestrator)
  Hexalith.Tenants.ServiceDefaults   # Shared service config, OpenTelemetry
  Hexalith.Tenants.Testing           # In-memory fakes + in-memory projection for query testing

tests/
  Hexalith.Tenants.Contracts.Tests   # Tier 1
  Hexalith.Tenants.Client.Tests      # Tier 1
  Hexalith.Tenants.Server.Tests      # Tier 2
  Hexalith.Tenants.Testing.Tests     # Tier 1
  Hexalith.Tenants.IntegrationTests  # Tier 3

samples/
  Hexalith.Tenants.Sample            # Sample consuming service
  Hexalith.Tenants.Sample.Tests      # Tier 1
```

**Cross-Component Dependencies:**
- Hexalith.Tenants depends on: Server (aggregates + projections), Contracts, ServiceDefaults
- Server contains: Aggregates + Projections (both auto-discovered via assembly scanning)
- Testing references: Server (same domain logic for fakes + in-memory projection for query testing)
- AppHost orchestrates: Hexalith.Tenants + EventStore + Keycloak + DAPR sidecar (single topology)

**Party Mode Review Summary:**
Panel review (Architect, Dev, Test Architect) surfaced 8 findings, all accepted:
1. QueryApi merged into Hexalith.Tenants (single deployable, route groups)
2. Cross-tenant index projections documented as distinct architectural concern
3. GlobalAdmin snapshot uses default 100 (low event volume)
4. Shared startup logic extracted into ServiceDefaults
5. DAPR state store key design constraint documented for cross-tenant indexes
6. Bootstrap goes through full MediatR pipeline
7. Testing fakes include in-memory projection for query scenarios
8. No separate QueryApi.Tests needed (covered by existing test projects)

**Party Mode Validation Review Summary:**
Post-completion validation review (Architect, Dev, PM, Test Architect) surfaced 8 additional findings, all applied:
1. ETag-based optimistic concurrency on cross-tenant index key (Critical → Data Architecture)
2. Read-after-write pattern decided as Option A — aggregate ID in command response (Important → API Patterns)
3. PRD FR25-30 consistency model clarification needed (Important → Gap Analysis)
4. DAPR component YAML examples promoted to project structure (Important → Directory Structure)
5. Cross-tenant isolation test pattern with tier mapping (Critical → Testing Strategy)
6. Snapshot performance test category with NFR13 threshold (Medium → Testing Strategy)
7. Conformance test mandate for production-test parity (Critical → Process Patterns)
8. Event serialization round-trip test with golden fixture plan (Important → Process Patterns)

## Implementation Patterns & Consistency Rules

### Pattern Categories Defined

**Critical Conflict Points Identified:** 5 categories where AI agents could make different choices without explicit guidance.

### Naming Patterns

**Command Naming Convention:**
- Pattern: `{Verb}{Target}` — PascalCase, verb-first
- Examples: `CreateTenant`, `AddUserToTenant`, `RemoveUserFromTenant`, `ChangeUserRole`, `SetTenantConfiguration`, `RemoveTenantConfiguration`, `DisableTenant`, `EnableTenant`, `UpdateTenant`, `BootstrapGlobalAdmin`, `SetGlobalAdministrator`, `RemoveGlobalAdministrator`
- Anti-pattern: `TenantCreate`, `UserAdd` (noun-first)

**Event Naming Convention:**
- Pattern: `{Target}{PastVerb}` — PascalCase, past tense
- Examples: `TenantCreated`, `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, `TenantConfigurationRemoved`, `TenantDisabled`, `TenantEnabled`, `TenantUpdated`, `GlobalAdministratorSet`, `GlobalAdministratorRemoved`
- Anti-pattern: `TenantUserAdded`, `CreateTenantEvent` (verb-first or -Event suffix)

**Rejection Event Naming Convention:**
- Pattern: `{Target}{Reason}Rejection`
- Examples: `TenantNotFoundRejection`, `UserAlreadyInTenantRejection`, `RoleEscalationRejection`, `TenantDisabledRejection`, `ConfigurationLimitExceededRejection`, `GlobalAdminAlreadyBootstrappedRejection`
- All implement `IRejectionEvent` (extends `IEventPayload`) from EventStore.Contracts
- Anti-pattern: Domain exception classes for business rule violations (use rejection events instead); `InvalidOperationException` with generic message

**API Endpoint Naming:**
- Command endpoint: `POST /api/commands` (EventStore standard — single command endpoint)
- Query endpoints: `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, `GET /api/tenants/{tenantId}/audit`
- Pattern: Plural nouns, kebab-case for multi-word segments, path parameters for IDs

**DAPR Resource Naming:**
- Convention-derived by EventStore's NamingConventionEngine
- AppId: `tenants` (matches domain name)
- State store: `tenants-eventstore` (domain name + suffix)
- Topic: `tenants.events` (domain name + `.events`)
- Dead letter: `deadletter.tenants.events`

### Structure Patterns

**Type Location Rules (Critical — Most Common Agent Conflict):**

| Type Category | Project Location | Rationale |
|--------------|-----------------|-----------|
| Commands (`CreateTenant`, etc.) | Contracts | Referenced by consuming services |
| Events (`TenantCreated`, etc.) | Contracts | Referenced by consuming services |
| Result types | Contracts | Referenced by consuming services |
| Identity types | Contracts | Referenced by consuming services |
| Enums (`TenantRole`, `TenantStatus`) | Contracts | Referenced by consuming services |
| Aggregates (`TenantAggregate`) | Server | Domain logic, auto-discovered |
| State classes (`TenantState`) | Server | Aggregate state, not exposed |
| Projections (`TenantProjection`) | Server | Read model logic, auto-discovered |
| Read model classes (`TenantReadModel`) | Server | Projection output, not exposed |
| Audit read model (`TenantAuditReadModel`) | Server | D12: Audit projection output |
| Audit event category enum (`AuditEventCategory`) | Contracts | D12: Referenced by query contracts |
| Rejection events (`*Rejection`) | Contracts | Business rule violation events (IRejectionEvent) |
| FluentValidation validators | Server | Command validation |
| Controllers | Hexalith.Tenants | REST endpoints |
| API models (DTOs) | Hexalith.Tenants | Request/response shapes if different from commands |
| Client abstractions | Client | DI extension methods for consuming services |
| In-memory fakes | Testing | Test infrastructure |
| In-memory projection | Testing | Query testing infrastructure |

**File Organization Within Projects:**
- One type per file (enforced by .editorconfig)
- Folder structure mirrors namespace: `Aggregates/`, `Events/`, `Events/Rejections/`, `Commands/`, `Projections/`, `Validators/`
- No nested folders deeper than 2 levels within a project

### Format Patterns

**API Response Formats:**
- Command responses: EventStore's `SubmitCommandResponse` pattern (command ID, status, correlation ID)
- Query responses: Direct JSON objects, no wrapper — `{ "tenantId": "acme-corp", "name": "Acme Corp", ... }`
- List responses: `{ "items": [...], "cursor": "next-page-token", "hasMore": true }`
- JSON field naming: camelCase (System.Text.Json default `JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase`)

**HTTP Status Codes:**
- `200 OK` — Successful query or command accepted
- `202 Accepted` — Command accepted for async processing (if applicable)
- `400 Bad Request` — FluentValidation failure
- `403 Forbidden` — Authorization failure (insufficient role)
- `404 Not Found` — Tenant/user not found (mapped from `TenantNotFoundRejection`)
- `409 Conflict` — Concurrency conflict or duplicate operation (mapped from `UserAlreadyInTenantRejection`, `TenantAlreadyExistsRejection`)
- `422 Unprocessable Entity` — Domain rejection (mapped from other `IRejectionEvent` types)
- `500 Internal Server Error` — Unexpected infrastructure errors only

**Error Response Structure:**
```json
{
  "type": "TenantNotFoundRejection",
  "title": "Tenant 'acme-test' does not exist.",
  "detail": "Ensure CreateTenant has been processed before adding users.",
  "status": 422,
  "correlationId": "abc-123"
}
```
Follows RFC 7807 Problem Details format. Rejection events are mapped to HTTP status codes by a `RejectionToHttpStatusMapper` middleware in Hexalith.Tenants. The `type` field uses the rejection event type name for programmatic error handling by consumers.

### Communication Patterns

**Event Payload Structure:**
- All events include `TenantId` as a top-level field identifying the managed tenant (Step 2 risk mitigation — envelope `tenantId` = `system`, so payload must carry the managed tenant ID)
- Events are records (immutable): `public record TenantCreated(string TenantId, string Name, string? Description, DateTimeOffset CreatedAt)`
- No nullable fields on events except optional descriptors — events represent facts that happened
- Timestamp fields: `DateTimeOffset` (preserves timezone), named `{Action}At` (e.g., `CreatedAt`, `DisabledAt`)

**Event Versioning:**
- Pre-v1.0: Event schemas may change; document instability
- Post-v1.0: Additive changes only (new fields with defaults). Breaking changes require new event type + backward-compatible deserialization via `eventTypeName` + `domainServiceVersion`

### Process Patterns

**Handle Method Implementation Pattern (Three-Outcome Model):**
```csharp
// Success: state change occurs
public static DomainResult Handle(CreateTenant command, TenantState? state)
    => state?.TenantId is not null
        ? DomainResult.Rejection([new TenantAlreadyExistsRejection(command.TenantId)])
        : DomainResult.Success([new TenantCreated(command.TenantId, command.Name, command.Description, DateTimeOffset.UtcNow)]);

// Rejection: business rule violation, no state change
public static DomainResult Handle(AddUserToTenant command, TenantState? state)
    => state switch
    {
        null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
        { Status: TenantStatus.Disabled } => DomainResult.Rejection([new TenantDisabledRejection(command.TenantId)]),
        _ when state.Members.ContainsKey(command.UserId)
            => DomainResult.Rejection([new UserAlreadyInTenantRejection(command.TenantId, command.UserId)]),
        _ => DomainResult.Success([new UserAddedToTenant(command.TenantId, command.UserId, command.Role)])
    };

// NoOp: command acknowledged but no state change needed (idempotent)
public static DomainResult Handle(DisableTenant command, TenantState? state)
    => state switch
    {
        null => DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)]),
        { Status: TenantStatus.Disabled } => DomainResult.NoOp(),
        _ => DomainResult.Success([new TenantDisabled(command.TenantId, DateTimeOffset.UtcNow)])
    };
```
- Always static, always pure (no side effects, no I/O, no thrown exceptions)
- Three outcomes: `DomainResult.Success(events)` for state changes, `DomainResult.Rejection([rejections])` for business rule violations, `DomainResult.NoOp()` for idempotent no-change
- Rejection events implement `IRejectionEvent` and are persisted in the idempotency store
- NEVER throw domain exceptions from Handle methods — use `DomainResult.Rejection()` instead

**Apply Method Implementation Pattern:**
```csharp
public void Apply(TenantCreated @event)
{
    TenantId = @event.TenantId;
    Name = @event.Name;
    Description = @event.Description;
    Status = TenantStatus.Active;
    CreatedAt = @event.CreatedAt;
}
```
- No validation in Apply — trust the event (it was validated in Handle)
- Direct state mutation (not immutable — state class is mutable by design)

**Logging Pattern:**
- Structured logging with semantic parameters: `logger.LogInformation("Tenant created: TenantId={TenantId}, Name={Name}", tenantId, name)`
- Never log state content or PII in Warning/Error (SnapshotManager rule #5 pattern)
- Always include `CorrelationId` via EventStore's middleware
- Log levels: Information (lifecycle), Warning (advisory failures like snapshots), Error (command rejections at infrastructure level)

**Validation Pattern:**
- FluentValidation for command structure validation (required fields, format, length limits)
- Domain validation in Handle methods (business rules, state preconditions)
- FluentValidation runs in MediatR pipeline before Handle — command never reaches aggregate if structurally invalid
- Commands without explicit FluentValidation validators (8 of 12 commands) are validated only at domain level in Handle methods. FluentValidation validators are required for commands with complex structural constraints (CreateTenant, AddUserToTenant, SetTenantConfiguration, ChangeUserRole). Simple commands (DisableTenant, EnableTenant, RemoveUserFromTenant, etc.) rely on domain validation only

**Conformance Test Pattern (Party Mode Validation Finding — Critical):**
- Mandatory test in `Testing.Tests`, Tier 1. Proves FR47 (testing fakes use same domain logic)
- For each command type: execute command against real TenantAggregate, capture resulting events. Execute same command against InMemoryTenantService, capture resulting events. Assert identical event sequences
- Reflection-driven: auto-discovers all command types in Contracts assembly, ensuring new commands are automatically covered
- If this test fails, production and test execution paths have diverged — this is a release blocker

**Event Contract Serialization Round-Trip Test (Party Mode Validation Finding):**
- Mandatory test in `Contracts.Tests`, Tier 1. Catches breaking schema changes on every PR
- Phase 1 (pre-v1.0): For each event type in Contracts, create instance with all fields populated, serialize to JSON via System.Text.Json, deserialize back, assert deep equality
- Phase 2 (post-v1.0): Additionally deserialize from stored golden JSON fixtures to catch backward-incompatible changes. Golden files stored in `tests/Hexalith.Tenants.Contracts.Tests/Fixtures/`

### Enforcement Guidelines

**All AI Agents MUST:**
1. Place types in the correct project per the Type Location Rules table
2. Follow command/event/rejection naming conventions exactly
3. Implement Handle methods as `public static` pure functions — no instance state, no I/O, no thrown exceptions
4. Include `TenantId` as a top-level field in all event payloads (success events AND rejection events)
5. Use `DomainResult.Rejection([new XxxRejection(...)])` for business rule violations — NEVER throw domain exceptions from Handle methods
6. Follow RFC 7807 Problem Details for API error responses (rejection events mapped to HTTP status codes)
7. Use structured logging with semantic parameters, never string interpolation in log templates

**Pattern Verification:**
- Tier 1 tests verify naming conventions via reflection (all commands in Contracts, all events in Contracts, all rejection events end with `Rejection`)
- Tier 1 conformance tests verify InMemoryTenantService produces identical events as TenantAggregate for every command
- Tier 1 serialization round-trip tests verify all event types survive JSON serialize/deserialize
- Tier 2 cross-tenant isolation tests verify both authorization layers reject cross-tenant access
- Code review checklist includes type location verification
- .editorconfig enforces code style automatically

## Project Structure & Boundaries

### Complete Project Directory Structure

```
Hexalith.Tenants/
├── .editorconfig                          # Inherited from EventStore conventions
├── .gitignore
├── .gitmodules                            # EventStore submodule reference
├── .github/
│   └── workflows/
│       ├── ci.yml                         # Build + Tier 1+2 tests on push/PR
│       └── release.yml                    # Merge-triggered: semantic-release + tests + pack + NuGet push
├── global.json                            # SDK 10.0.103, rollForward: latestPatch
├── Directory.Build.props                  # Shared project properties, NuGet metadata
├── Directory.Packages.props               # Centralized NuGet package versions
├── Hexalith.Tenants.slnx                  # Modern XML solution file
├── README.md                              # Quickstart, badges, demo GIF
├── LICENSE                                # MIT
├── CHANGELOG.md
├── CONTRIBUTING.md
├── Hexalith.EventStore/                   # Git submodule
│
├── src/
│   ├── Hexalith.Tenants.Contracts/        # NuGet: Commands, events, enums, identities
│   │   ├── Hexalith.Tenants.Contracts.csproj
│   │   ├── Commands/
│   │   │   ├── CreateTenant.cs            # FR1
│   │   │   ├── UpdateTenant.cs            # FR2
│   │   │   ├── DisableTenant.cs           # FR3
│   │   │   ├── EnableTenant.cs            # FR4
│   │   │   ├── AddUserToTenant.cs         # FR6
│   │   │   ├── RemoveUserFromTenant.cs    # FR7
│   │   │   ├── ChangeUserRole.cs          # FR8
│   │   │   ├── SetTenantConfiguration.cs  # FR19
│   │   │   ├── RemoveTenantConfiguration.cs # FR20
│   │   │   ├── BootstrapGlobalAdmin.cs    # FR17
│   │   │   ├── SetGlobalAdministrator.cs  # FR13
│   │   │   └── RemoveGlobalAdministrator.cs # FR14
│   │   ├── Events/
│   │   │   ├── TenantCreated.cs           # FR5
│   │   │   ├── TenantUpdated.cs           # FR5
│   │   │   ├── TenantDisabled.cs          # FR5
│   │   │   ├── TenantEnabled.cs           # FR5
│   │   │   ├── UserAddedToTenant.cs       # FR11
│   │   │   ├── UserRemovedFromTenant.cs   # FR11
│   │   │   ├── UserRoleChanged.cs         # FR11
│   │   │   ├── TenantConfigurationSet.cs  # FR22
│   │   │   ├── TenantConfigurationRemoved.cs # FR22
│   │   │   ├── GlobalAdministratorSet.cs  # FR16
│   │   │   ├── GlobalAdministratorRemoved.cs # FR16
│   │   │   └── Rejections/
│   │   │       ├── TenantAlreadyExistsRejection.cs  # FR50, FR52
│   │   │       ├── TenantNotFoundRejection.cs        # FR50
│   │   │       ├── TenantDisabledRejection.cs        # FR51
│   │   │       ├── UserAlreadyInTenantRejection.cs   # FR9, FR52
│   │   │       ├── UserNotInTenantRejection.cs
│   │   │       ├── RoleEscalationRejection.cs        # FR10
│   │   │       ├── ConfigurationLimitExceededRejection.cs # FR24
│   │   │       └── GlobalAdminAlreadyBootstrappedRejection.cs # FR18
│   │   ├── Identity/
│   │   │   └── TenantIdentity.cs          # Identity scheme helpers
│   │   └── Enums/
│   │       ├── TenantRole.cs              # TenantOwner, TenantContributor, TenantReader
│   │       └── TenantStatus.cs            # Active, Disabled
│   │
│   ├── Hexalith.Tenants.Client/           # NuGet: Client abstractions, DI
│   │   ├── Hexalith.Tenants.Client.csproj
│   │   └── Registration/
│   │       └── TenantServiceCollectionExtensions.cs # FR44, FR45
│   │
│   ├── Hexalith.Tenants.Server/           # NuGet: Aggregates, projections, DAPR
│   │   ├── Hexalith.Tenants.Server.csproj
│   │   ├── Aggregates/
│   │   │   ├── TenantAggregate.cs         # FR1-12, FR19-24, FR31-34, FR50-53
│   │   │   ├── TenantState.cs
│   │   │   ├── GlobalAdministratorAggregate.cs # FR13-18
│   │   │   └── GlobalAdministratorState.cs
│   │   ├── Projections/
│   │   │   ├── TenantProjection.cs        # Per-tenant read model
│   │   │   ├── TenantReadModel.cs         # FR26-27
│   │   │   ├── GlobalAdministratorProjection.cs
│   │   │   ├── GlobalAdministratorReadModel.cs
│   │   │   ├── TenantIndexProjection.cs   # Cross-tenant index (FR25, FR28-30)
│   │   │   ├── TenantIndexReadModel.cs
│   │   │   ├── TenantAuditProjection.cs   # D12: Audit trail (FR29, UX Journey 5)
│   │   │   └── TenantAuditReadModel.cs
│   │   └── Validators/
│   │       ├── CreateTenantValidator.cs
│   │       ├── AddUserToTenantValidator.cs
│   │       ├── SetTenantConfigurationValidator.cs
│   │       └── ChangeUserRoleValidator.cs
│   │
│   ├── Hexalith.Tenants/       # Deployable: REST commands + queries
│   │   ├── Hexalith.Tenants.csproj
│   │   ├── Program.cs
│   │   ├── Controllers/
│   │   │   └── TenantsQueryController.cs  # FR25-30 query endpoints
│   │   ├── Configuration/
│   │   │   └── TenantBootstrapOptions.cs  # FR17-18
│   │   └── Bootstrap/
│   │       └── TenantBootstrapHostedService.cs # Startup bootstrap logic
│   │
│   ├── Hexalith.Tenants.Aspire/           # NuGet: Aspire hosting extensions
│   │   ├── Hexalith.Tenants.Aspire.csproj
│   │   ├── HexalithTenantsExtensions.cs   # FR48
│   │   └── HexalithTenantsResources.cs
│   │
│   ├── Hexalith.Tenants.AppHost/          # Aspire AppHost
│   │   ├── Hexalith.Tenants.AppHost.csproj
│   │   └── Program.cs                     # DAPR topology orchestration
│   │
│   ├── Hexalith.Tenants.ServiceDefaults/  # Shared service config
│   │   ├── Hexalith.Tenants.ServiceDefaults.csproj
│   │   └── Extensions.cs                  # OpenTelemetry, shared DI
│   │
│   └── Hexalith.Tenants.Testing/          # NuGet: In-memory fakes
│       ├── Hexalith.Tenants.Testing.csproj
│       ├── Fakes/
│       │   └── InMemoryTenantService.cs   # FR46-47
│       ├── Projections/
│       │   └── InMemoryTenantProjection.cs # Query testing support
│       └── Helpers/
│           └── TenantTestHelpers.cs
│
├── tests/
│   ├── Directory.Build.props              # Test-specific properties (IsPackable=false)
│   ├── Hexalith.Tenants.Contracts.Tests/  # Tier 1: Command/event structure
│   │   └── Hexalith.Tenants.Contracts.Tests.csproj
│   ├── Hexalith.Tenants.Client.Tests/     # Tier 1: DI registration
│   │   └── Hexalith.Tenants.Client.Tests.csproj
│   ├── Hexalith.Tenants.Server.Tests/     # Tier 2: Aggregate logic + DAPR
│   │   └── Hexalith.Tenants.Server.Tests.csproj
│   ├── Hexalith.Tenants.Testing.Tests/    # Tier 1: Test the testing fakes
│   │   └── Hexalith.Tenants.Testing.Tests.csproj
│   └── Hexalith.Tenants.IntegrationTests/ # Tier 3: Aspire E2E
│       └── Hexalith.Tenants.IntegrationTests.csproj
│
├── samples/
│   ├── Hexalith.Tenants.Sample/           # Sample consuming service (FR62)
│   │   └── Hexalith.Tenants.Sample.csproj
│   └── Hexalith.Tenants.Sample.Tests/     # Tier 1: Sample tests
│       └── Hexalith.Tenants.Sample.Tests.csproj
│
├── dapr/                                  # DAPR component configuration
│   └── components/
│       ├── statestore.yaml                # tenants-eventstore state store
│       ├── pubsub.yaml                    # tenants pub/sub component
│       └── actors.yaml                    # TenantAggregate, GlobalAdministratorAggregate actor config
│
└── docs/                                  # Conceptual documentation
    ├── quickstart.md                      # FR59-60 (references dapr/components/)
    ├── event-contract-reference.md        # FR61
    ├── cross-aggregate-timing.md          # FR64
    └── compensating-commands.md           # FR65
```

### Architectural Boundaries

**API Boundaries:**
- External: REST API via Hexalith.Tenants (commands at `POST /api/commands`, queries at `GET /api/tenants/*`)
- Internal: MediatR pipeline for command dispatch, DAPR actor invocation for aggregate processing
- Auth boundary: JWT validation at Hexalith.Tenants entry point, domain RBAC in aggregate Handle methods

**Component Boundaries:**
- Contracts → Referenced by all other projects and consuming services (public API surface)
- Client → References Contracts only (thin DI layer)
- Server → References Contracts (domain logic, auto-discovered by EventStore)
- Hexalith.Tenants → References Server + Contracts + ServiceDefaults (deployable host)
- Testing → References Server + Contracts (same domain logic for fakes)
- Aspire → References Contracts + Client (hosting extensions)

**Data Boundaries:**
- Event store: DAPR state store via actor state manager (one per aggregate instance)
- Read model: DAPR state store via projection handler (convention-named)
- Cross-tenant index: DAPR state store with well-known keys (separate from per-aggregate state)
- No direct database access — all storage through DAPR abstraction

### FR-to-Structure Mapping

| FR Category | Primary Location | Key Files |
|-------------|-----------------|-----------|
| Tenant Lifecycle (FR1-5) | Contracts/Commands, Server/Aggregates | CreateTenant.cs → TenantAggregate.cs |
| User-Role Management (FR6-12) | Contracts/Commands, Server/Aggregates | AddUserToTenant.cs → TenantAggregate.cs |
| Global Administration (FR13-18) | Contracts/Commands, Server/Aggregates | BootstrapGlobalAdmin.cs → GlobalAdministratorAggregate.cs |
| Tenant Configuration (FR19-24) | Contracts/Commands, Server/Aggregates | SetTenantConfiguration.cs → TenantAggregate.cs |
| Tenant Discovery & Query (FR25-30) | Server/Projections, Hexalith.Tenants/Controllers | TenantIndexProjection.cs → TenantsQueryController.cs |
| Role Behavior (FR31-34) | Server/Aggregates | TenantAggregate.cs Handle methods |
| Event-Driven Integration (FR35-42) | Contracts/Events, docs/ | TenantCreated.cs + event-contract-reference.md |
| Developer Experience (FR43-49) | Client, Testing, Aspire | TenantServiceCollectionExtensions.cs, InMemoryTenantService.cs |
| Documentation (FR59-65) | docs/, samples/ | quickstart.md, Sample/ |

### Data Flow

**D9 Revision (2026-03-15 — Research-Validated Command Processing Pipeline):**

The command pipeline spans REST → MediatR → DAPR Actors → domain service invocation → event persistence. Critical architectural insight: **CommandApi is both the REST gateway AND the domain service host.** The AggregateActor (in EventStore.Server) delegates domain processing back to CommandApi via DAPR service-to-service invocation.

```
HTTP POST /api/v1/commands (JWT-authenticated)
  │
  └─ CommandsController.Submit()
     ├─ Extract JWT `sub` claim as userId
     ├─ Sanitize extension metadata (SEC-4)
     ├─ Create SubmitCommand (MediatR request)
     │
     └─ MediatR pipeline (FluentValidation → AuthorizationBehavior)
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

**AggregateActor 5-Step Checkpointed Pipeline:**

| Step | Operation | Recovery |
| ---- | --------- | -------- |
| 1 | **Idempotency check** — cached result by CausationId; resume for in-flight pipelines | Skip to cached result |
| 2 | **Tenant validation** — validates TenantId matches actor ID (SEC-2). BEFORE state access | Reject with TenantMismatchException |
| 3 | **State rehydration** — load snapshot + tail-only event replay → current state | Dead-letter on failure |
| 4 | **Domain service invocation** — DAPR service-to-service call to Hexalith.Tenants `/process` endpoint | Dead-letter on failure |
| 5 | **Event persistence + publication** — persist events atomically, snapshot if threshold met, publish via DAPR pub/sub | Drain reminder for failed publications |

```
Domain Service Invocation (Step 4 Detail):
  AggregateActor
    └─ DaprDomainServiceInvoker.InvokeAsync()
       ├─ Resolve service registration (AppId, MethodName) via IDomainServiceResolver
       └─ daprClient.InvokeMethodAsync<DomainServiceRequest, DomainServiceWireResult>()
          │
          ├─ DomainServiceRequest = { CommandEnvelope, CurrentState }
          └─ DomainServiceWireResult → DomainResult (events or rejections)

Hexalith.Tenants /process endpoint:
  DomainServiceRequest received
    └─ IDomainProcessor.ProcessAsync()
       └─ Reflection-based dispatch to Aggregate.Handle(Command, State?)
          └─ Returns DomainResult (Success/Rejection/NoOp)
```

Terminal states: Completed (success), Rejected (domain rejection), PublishFailed (events persisted, pub/sub failed — drain recovery active).

**Query Flow:**
```
Client → Hexalith.Tenants (REST) → MediatR → QueryRouter → CachingProjectionActor → ReadModel
(ETag pre-check at controller level: If-None-Match → 304 Not Modified)
```

**Projection Flow:**
```
DAPR pub/sub → Subscription Endpoint → Projection.Apply() → DAPR State Store
  → DaprProjectionChangeNotifier → ETagActor.RegenerateAsync() (invalidates cached queries)
```

**Consuming Service Flow:**
```
DAPR pub/sub → Service Subscription → Local Projection → Service-specific behavior
```

### Aggregate Testing Blueprint (D10 — Research-Validated 2026-03-15)

Event-sourced aggregates are testable as pure functions with zero infrastructure. The EventStore framework exposes `ProcessAsync` on the aggregate base class for test invocation:

```csharp
// CommandEnvelope construction helper for tests
private static CommandEnvelope CreateCommand<T>(T command) where T : notnull
    => new(
        "system",                          // TenantId (platform tenant)
        "tenants",                         // Domain
        command is BootstrapGlobalAdmin or SetGlobalAdministrator or RemoveGlobalAdministrator
            ? "global-administrators"      // Singleton aggregate
            : ((dynamic)command).TenantId, // Per-tenant aggregate
        typeof(T).Name,                    // CommandType
        JsonSerializer.SerializeToUtf8Bytes(command),
        Guid.NewGuid().ToString(),         // CorrelationId
        null,                              // CausationId
        "test-user",                       // UserId
        null);                             // Extensions

// Given/When/Then test pattern
[Fact]
public async Task CreateTenant_WhenTenantDoesNotExist_ProducesTenantCreated()
{
    // Given: no prior state
    var aggregate = new TenantAggregate();
    var command = CreateCommand(new CreateTenant("acme", "Acme Corp", "Test"));

    // When: command processed
    DomainResult result = await aggregate.ProcessAsync(command, currentState: null);

    // Then: success with TenantCreated event
    result.IsSuccess.ShouldBeTrue();
    result.Events[0].ShouldBeOfType<TenantCreated>();
}
```

**Test categories for aggregate stories (2.2, 2.3):**

| Category | What to test | Infrastructure |
| -------- | ------------ | -------------- |
| Handle success paths | Command + null/existing state → correct events | None |
| Handle rejection paths | Command + invalid state → correct rejection events | None |
| Handle NoOp paths | Command + already-in-desired-state → DomainResult.NoOp() | None |
| Apply methods | Event sequence → correct state properties | None |
| State replay | Full event history → correct final state | None |

All tests are Tier 1 (unit) — no DAPR, no actors, no mocking.

## UX-Driven Architecture Amendments (2026-03-25)

_Amendments based on the UX Design Specification (ux-design-specification.md, completed 2026-03-24). The UX spec introduces frontend screens, interaction patterns, and data requirements that surface gaps in the original architecture. Each amendment references the original decision it extends._

### D11: User Search Authorization Scoping

**Decision:** `GetUserTenantsQuery` uses row-level filtering in the query handler based on the requesting user's authorization scope.

**Authorization scoping rules:**

| Requesting User | Query Scope | Rationale |
|----------------|-------------|-----------|
| Any authenticated user searching **themselves** | All own memberships across all tenants | Self-audit capability (UX Journey 7) |
| TenantOwner searching **another user** | Only memberships in tenants the requester owns | TenantOwner manages their tenant but cannot see other tenants' memberships |
| GlobalAdmin searching **any user** | All memberships across all tenants | Platform-level oversight (UX Journey 3 — incident response) |

**Implementation location:** Query handler for `GetUserTenantsQuery` in Server. The `AuthorizationBehavior` in MediatR pipeline validates API access (Layer 1). The query handler receives the requesting user's identity from the command context and applies row-level filtering on the result set before returning.

**This is a third authorization pattern** — neither command-side domain RBAC (Layer 2) nor API-level JWT validation (Layer 1). It is **query-side result filtering** based on the requesting user's aggregate membership state. The pattern:
1. `AuthorizationBehavior` validates JWT claims (Layer 1 — existing)
2. Query handler loads full result set from projection
3. Query handler loads requesting user's memberships/roles from projection
4. Query handler filters results: keep rows where requester is the target user, OR requester is TenantOwner of that tenant, OR requester is GlobalAdmin
5. Return filtered results

**Affects:** Server (`GetUserTenantsQuery` handler), potentially a shared authorization helper for query-side filtering reusable by other scoped queries.

**Extends:** D8 (Authorization Model) — adds Layer 2b (query-side result filtering) alongside Layer 2 (domain RBAC in Handle methods).

---

### D12: Audit Projection and Query Design

**Decision:** A dedicated `TenantAuditProjection` materializes audit-relevant event data into a queryable read model, supporting date range filtering and event category classification.

**Rationale:** The UX spec makes the audit trail a must-ship screen with three entry points (standalone, tenant detail tab, user search drill-down), date range filtering, and event category filtering (Access vs Administrative). Event store replay is not suitable for fast, filtered reads at query time.

**Projection design:**

```
TenantAuditProjection : EventStoreProjection<TenantAuditReadModel>
```

**`TenantAuditReadModel` fields:**

| Field | Type | Purpose |
|-------|------|---------|
| `eventId` | string | Unique event identifier |
| `eventType` | string | Event type name (e.g., `UserAddedToTenant`) |
| `category` | enum: `Access`, `Administrative` | Classified at projection time for UX filtering |
| `actorId` | string | User ID of who performed the action |
| `timestamp` | DateTimeOffset | When the event occurred |
| `tenantId` | string | Which managed tenant |
| `narrativePayload` | Dictionary<string, string> | Key fields for narrative template rendering (e.g., `targetUserId`, `role`, `configKey`) |

**Event category classification:**

| Category | Events |
|----------|--------|
| **Access** | `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `GlobalAdministratorSet`, `GlobalAdministratorRemoved` |
| **Administrative** | `TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`, `TenantConfigurationSet`, `TenantConfigurationRemoved` |

**Query contract:**

```csharp
public record GetTenantAuditQuery(
    string TenantId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    AuditEventCategory? Category,  // Access, Administrative, or null for all
    string? Cursor,
    int PageSize = 50
) : IQueryContract;
```

**DAPR state store key design:** `audit:{tenantId}` — stores audit entries as a time-ordered list per tenant. Cursor-based pagination over the list. At 1K tenants × ~1000 events per tenant (max), this is workable with the existing DAPR state store approach. If audit volume grows significantly, a DAPR state store backend with native query support (e.g., CosmosDB) can be swapped without code changes.

**REST endpoint:** `GET /api/tenants/{tenantId}/audit?from=2026-03-01&to=2026-03-25&category=access&cursor=xxx`

**Type locations:**
- `GetTenantAuditQuery`, `AuditEventCategory` enum → Contracts
- `TenantAuditProjection`, `TenantAuditReadModel` → Server
- REST endpoint → Hexalith.Tenants (`TenantsQueryController`)

**Extends:** D4 (Data Architecture — Read Model) — adds a fourth projection alongside `TenantProjection`, `GlobalAdministratorProjection`, and `TenantIndexProjection`.

---

### D13: SignalR as Must-Ship Dependency

**Decision:** SignalR real-time projection updates are a must-ship dependency, not optional. EventStore's `IProjectionChangedBroadcaster` and `IProjectionChangeNotifier` are required infrastructure for the Tenants module.

**Rationale:** The UX spec's three-phase feedback pattern (optimistic → confirming → confirmed) depends on SignalR delivering Phase 3 confirmation. This is the core trust-building mechanism — without it, every command shows a perpetual "Verifying..." state with fallback polling. The UX spec defines explicit degradation thresholds:

| Threshold | UX Response |
|-----------|-------------|
| 0-2s | Phase 2 confirming indicator (normal) |
| 5s | "Verifying..." text appears |
| 15s | Amber banner: "Connection issue — changes may be delayed" |
| Reconnect | Batch resolve all pending operations from projection state |

**Multi-tenant SignalR subscription pattern:**

The User Search page displays memberships across multiple tenants and must receive real-time updates from ALL tenants in the result set. Proposed approach:

- SignalR hub uses **topic-based groups**: `tenant:{tenantId}`
- User Search page joins groups for all tenants in the result set on mount
- Leaves groups on unmount or when results change
- EventStore's `IProjectionChangedBroadcaster` publishes to the relevant tenant group when a projection updates
- Standard SignalR group management — no custom infrastructure needed

**Fallback:** Automatic polling at 5s intervals when SignalR connection fails. Client uses `stale-while-revalidate` pattern — shows cached data immediately, refreshes silently on reconnect.

**Affects:** Hexalith.Tenants (SignalR hub configuration), ServiceDefaults (SignalR service registration), AppHost (SignalR resource in Aspire topology).

**Extends:** D4 Revision (2026-03-15) — promotes `IProjectionChangedBroadcaster` from optional to required; adds multi-tenant subscription pattern.

---

### D14: Anomaly Detection — Client-Side Heuristics

**Decision:** MVP anomaly detection for User Search is implemented as client-side heuristics. No backend anomaly service or scoring projection.

**Rationale:** The projection already exposes the fields needed for simple heuristic checks (timestamps, actor IDs). Server-side anomaly scoring adds complexity without clear MVP value. Phase 2 can introduce server-side scoring if heuristics prove insufficient.

**Client-side heuristic rules:**

| Anomaly | Heuristic | Data Source |
|---------|-----------|-------------|
| Off-hours grant | `grantedAt` hour is outside 6 AM - 10 PM local time | `GetUserTenantsQuery` response |
| First-time actor | `grantedBy` is not a known admin in the tenant's projection | `GetUserTenantsQuery` response + `TenantReadModel` |
| Rapid bulk additions | 3+ grants by same actor within 5 minutes to same tenant | `GetUserTenantsQuery` response (multiple rows) |

**Backend requirement:** No new endpoints. The `GetUserTenantsQuery` response must include per-membership metadata fields (see D15).

**Extends:** No existing decision — new UX-driven concern. Client-side only for MVP.

---

### D15: Projection Field Enrichment

**Decision:** Existing projections are enriched with additional fields required by the UX design specification.

**`TenantReadModel` — additional fields:**

| Field | Type | Purpose | Derived From |
|-------|------|---------|-------------|
| `lastActivityAt` | DateTimeOffset | Dashboard "last activity" column, recency highlight | Timestamp of most recent event applied to projection |
| `ownerCount` | int | "No owners" warning indicator, last-owner consequence preview | Count of members with `TenantOwner` role |
| `configKeyCount` | int | Config limit proximity warning indicator | Count of configuration entries |

Note: `ownerCount` and `configKeyCount` are derivable from `Members.Count(r => r == TenantOwner)` and `Configuration.Count` respectively, but pre-computed in the read model for fast dashboard rendering at scale (1K tenants list without per-row computation).

**`GetUserTenantsQuery` response — per-membership metadata:**

| Field | Type | Purpose | Derived From |
|-------|------|---------|-------------|
| `grantedAt` | DateTimeOffset | Anomaly detection (time-of-day heuristic) | `UserAddedToTenant` event timestamp |
| `grantedBy` | string | Anomaly detection (actor attribution) | `UserAddedToTenant` event actor ID |
| `lastRoleChangeAt` | DateTimeOffset? | Anomaly detection for role changes | `UserRoleChanged` event timestamp (null if role never changed) |
| `lastRoleChangeBy` | string? | Anomaly detection (role change actor) | `UserRoleChanged` event actor ID (null if role never changed) |

**Implementation:** These fields are populated in the `TenantProjection.Apply()` methods for the relevant events. The `UserAddedToTenant` Apply stores the actor and timestamp alongside the membership entry. The `UserRoleChanged` Apply updates the role change metadata.

**State design impact:** The `TenantState` (command-side) is unchanged — these are read model enrichments only. The `TenantReadModel` adds the pre-computed counts. The membership detail in the read model extends from `{ userId, role }` to `{ userId, role, grantedAt, grantedBy, lastRoleChangeAt, lastRoleChangeBy }`.

**Extends:** D4 (Data Architecture) — enriches existing `TenantReadModel` and shapes the `GetUserTenantsQuery` response contract.

---

### D16: Consequence Preview Data Flow

**Decision:** Consequence previews use data already available in client-side projections. No dedicated consequence computation endpoint.

**Rationale:** The UX spec explicitly requires zero additional round-trips for consequence data during high-impact operations. All required data is derivable from projections already loaded on the screen.

**Consequence data mapping:**

| Command | Consequence Data | Source (Already on Screen) |
|---------|-----------------|---------------------------|
| `DisableTenant` | "N active members will lose command access" | `TenantReadModel.Members.Count` from tenant detail |
| `RemoveUserFromTenant` | "This user has access to N other tenants" | `GetUserTenantsQuery` results (User Search page) or not shown (tenant detail — only current tenant visible) |
| `RemoveUserFromTenant` | "This is the last TenantOwner" warning | `TenantReadModel.ownerCount` from tenant detail |
| `RemoveGlobalAdministrator` | "Last global administrator" warning | `GlobalAdministratorReadModel` admin count from Global Admin page |

**Domain invariant decision:** The domain does **not** enforce a "must have at least one TenantOwner" invariant. Removing the last owner is allowed — the consequence preview provides a prominent warning, but the Handle method does not reject. Rationale: hard invariants block legitimate scenarios (e.g., reassigning ownership requires removing the old owner first). The "no owners" state is surfaced as a dashboard warning indicator (D15, `ownerCount == 0`) for Elena's daily review.

**Extends:** No existing decision directly — documents the data flow pattern for UX consequence previews as a client-side concern with no backend impact beyond D15 projection enrichments.

---

### D17: FrontShell Cross-Project Dependencies

**Decision:** The Tenants UI depends on deliverables from Hexalith.FrontShell that must be sequenced in epic/story planning. These are documented as external dependencies.

**FrontShell Change Proposal deliverables:**

| Deliverable | Type | Tenants Screens Blocked |
|-------------|------|------------------------|
| `<AuditTimeline>` component (flat MVP, grouped fast-follow) | New `@hexalith/ui` component | Audit trail screens (standalone + tenant detail tab) |
| `<ConsequencePreview>` component | New `@hexalith/ui` component | Remove user, disable tenant, remove global admin |
| `useCommand` `pendingIds` enhancement | Hook API change | Three-phase feedback on all table rows |
| `useCommand` concurrent command support | Hook API change | Sequential rapid action (incident response) |
| Toast batch consolidation (100ms window) | Shell behavior | Multiple Phase 3 confirmations during rapid operations |
| `<PageLayout>` `full-width` / `constrained` variants | Confirm or add | All page layouts |
| 5 new design tokens (3 role semantic + 2 component) | Token additions | Role badges, timeline connector, consequence panel |
| Three-phase Storybook story | Developer documentation | Pattern reference for module developers |

**Sequencing constraint:** Tenants UI stories that use these deliverables must have explicit `blockedBy` relationships to the corresponding FrontShell stories. Backend stories (aggregates, projections, API endpoints) are not blocked — only frontend stories that consume these components.

**Extends:** No existing backend decision — documents cross-project dependency for implementation planning.

## Architecture Validation Results

### Coherence Validation ✅

**Decision Compatibility:**
All technology choices work together without conflicts. .NET 10 + DAPR SDK 1.17.3 + .NET Aspire 13.1.x are compatible. System.Text.Json used throughout (no serializer conflicts). MediatR + FluentValidation pipeline follows EventStore pattern. JWT auth + domain RBAC + query-side result filtering (D11) are three clear, non-overlapping authorization layers. Single deployable (CommandApi) with route groups has no contradictions with CQRS decisions. SignalR (D13) integrates via EventStore's existing `IProjectionChangedBroadcaster` infrastructure. UX amendments (D11-D17) are additive — no conflicts with original D1-D10 decisions.

**Pattern Consistency:**
All implementation patterns support the architectural decisions. Naming conventions (PascalCase commands/events) align with EventStore's reflection-based discovery. Handle/Apply pure function pattern is enforced by EventStore's aggregate base class. RFC 7807 error responses match EventStore's error handler infrastructure. Structured logging follows OpenTelemetry semantic conventions.

**Structure Alignment:**
Project structure supports all decisions. 8 src projects mirror EventStore (minus separate QueryApi, per Party Mode decision). Testing → Server dependency matches EventStore.Testing → EventStore.Server pattern. All types mapped to correct projects per Type Location Rules table.

### Requirements Coverage Validation ✅

**Functional Requirements Coverage:**

| FR Category | Status | Architectural Support |
|-------------|--------|----------------------|
| FR1-5 (Tenant Lifecycle) | ✅ | Commands + events + TenantAggregate |
| FR6-12 (User-Role Management) | ✅ | TenantAggregate with duplicate detection, escalation, concurrency |
| FR13-18 (Global Administration) | ✅ | GlobalAdministratorAggregate + bootstrap mechanism |
| FR19-24 (Tenant Configuration) | ✅ | Commands + limits enforced in aggregate + events |
| FR25-30 (Tenant Discovery & Query) | ✅ | TenantIndexProjection + TenantsQueryController |
| FR31-34 (Role Behavior) | ✅ | Handle methods enforce role semantics |
| FR35-42 (Event-Driven Integration) | ✅ | DAPR pub/sub, CloudEvents 1.0, idempotency docs |
| FR43-49 (Developer Experience) | ✅ | 5 NuGet packages, DI registration, testing fakes, error messages |
| FR50-53 (Command Validation) | ✅ | Domain-specific rejection events for each scenario |
| FR54-58 (Observability & Operations) | ✅ | OpenTelemetry, stateless service architecture |
| FR59-65 (Documentation & Adoption) | ✅ | docs/ folder with quickstart, event reference, timing guide, compensating commands |

**Non-Functional Requirements Coverage:**

| NFR Category | Status | Architectural Support |
|-------------|--------|----------------------|
| NFR1-4 (Performance) | ✅ | DAPR actor model + 50-event snapshots |
| NFR5-10 (Security) | ✅ | Aggregate isolation, role enforcement, audit events, 100% branch target |
| NFR11-13 (Scalability) | ✅ | Stateless horizontal scaling, 50-event snapshot for 30s startup |
| NFR14-19 (Integration) | ✅ | CloudEvents 1.0, DAPR abstraction, graceful degradation |
| NFR20-23 (Reliability) | ✅ | Event store as source of truth, advisory snapshots |
| NFR24 (Accessibility) | ✅ | English-only MVP |

### Testing Strategy Validation (Party Mode Validation Findings)

**Cross-Tenant Isolation Test Pattern (Critical — NFR5):**
- NFR5 requires "zero cross-tenant leaks." Isolation is achieved through aggregate boundaries (actor-per-tenant) and JWT authorization. Testing must verify both defense layers:
- **Tier 1 (Unit):** Verify Handle methods reject operations when the requesting user is not in `state.Members`. Pure function tests — no infrastructure needed
- **Tier 2 (DAPR):** Verify EventStore's `AuthorizationBehavior` in MediatR pipeline rejects requests with wrong tenant scope in JWT claims
- **Tier 2/3 (API):** Dedicated `CrossTenantIsolationTests`:
  1. Create TenantA with UserX as Owner
  2. Create TenantB with UserY as Owner
  3. Attempt `AddUserToTenant(TenantA, UserY)` with TenantB-scoped credentials → Assert `403 Forbidden`
  4. Attempt `GET /api/tenants/TenantA` with TenantB-scoped JWT → Assert `403 Forbidden`
- Each isolation property is mapped to a specific test tier. This is not optional — it is the single most critical security validation

**Snapshot Performance Test (NFR13):**
- NFR13 requires 30s startup reconstruction for 500K events. This is a performance test that does not fit Tier 1-3
- Decision: Dedicated performance test category, runs on CI schedule (nightly), not on every PR
- Test approach: Seed 500K events into state store, cold-start actor, measure rehydration time with 50-event snapshot interval
- Location: Separate test project or test category filter in `IntegrationTests`
- Failure threshold: Actor rehydration must complete in under 30 seconds on CI runner hardware

**Bootstrap Multi-Instance Behavior (Party Mode Validation Finding):**
- `TenantBootstrapHostedService` runs on every instance startup and sends `BootstrapGlobalAdmin` through MediatR. On multi-instance deployments, N-1 instances will receive `DomainResult.Rejection([new GlobalAdminAlreadyBootstrappedRejection("system")])`. This is expected behavior, not an error
- Decision: Log bootstrap rejection at `Information` level (not `Warning` or `Error`) with message "Global administrator already bootstrapped, skipping"
- Affects: TenantBootstrapHostedService implementation and logging configuration

### Implementation Readiness Validation ✅

**Decision Completeness:**
- All critical decisions documented with technology versions ✅
- Implementation patterns include concrete code examples ✅
- Consistency rules are clear and enforceable ✅
- Type location rules provide explicit guidance for every type category ✅

**Structure Completeness:**
- Complete project tree with all files and directories ✅
- All FR categories mapped to specific files ✅
- Component boundaries and dependencies documented ✅
- Data flow diagrams for command, query, projection, and consuming service flows ✅

**Pattern Completeness:**
- 5 conflict categories addressed (naming, structure, format, communication, process) ✅
- Enforcement guidelines with 7 mandatory rules ✅
- Pattern verification approach documented (reflection tests + code review) ✅

### Gap Analysis Results

**Critical Gaps: None remaining.** (2 critical gaps from Party Mode Validation resolved — see below)

**Resolved by Party Mode Validation Review:**
1. ~~Cross-tenant isolation test pattern~~ → Added to Testing Strategy Validation section (Critical)
2. ~~Conformance test mandate~~ → Added to Process Patterns as mandatory Conformance Test Pattern (Critical)
3. ~~ETag concurrency on index key~~ → Added to Cross-Tenant Index Projections section (Important)
4. ~~Read-after-write pattern~~ → Added to API & Communication Patterns as decided pattern (Important)
5. ~~DAPR component YAML examples~~ → Added to project structure as `dapr/components/` directory (Important)
6. ~~Event serialization round-trip test~~ → Added to Process Patterns (Important)
7. ~~Snapshot performance test~~ → Added to Testing Strategy Validation section (Medium)
8. ~~Bootstrap multi-instance behavior~~ → Added to Testing Strategy Validation section (Medium)

**Important Gaps (non-blocking, addressable during implementation):**
1. DAPR state store key design for cross-tenant indexes — ETag concurrency decided, specific key schema deferred to implementation (appropriate for architecture level)
2. PRD FR25-30 consistency model clarification — PRD does not specify eventual consistency for queries. Should be updated to prevent test assertions assuming immediate consistency
3. "Aha moment" demo artifact (FR63) — screencast/video not mapped to a file; can be added to docs/ or project root during implementation
4. ~~Audit query design~~ → Resolved by D12 (TenantAuditProjection with date range + category filtering)
5. ~~SignalR as optional~~ → Resolved by D13 (promoted to must-ship dependency)
6. ~~User Search authorization scoping~~ → Resolved by D11 (query-side result filtering)
7. ~~Projection fields for UX dashboard~~ → Resolved by D15 (TenantReadModel + GetUserTenantsQuery enrichment)

**Nice-to-Have (future enhancement):**
- Sample consuming service internal structure detail
- Migration guide for v1.0 event contract stability transition
- Server-side anomaly scoring for User Search (D14 documents MVP client-side heuristics; Phase 2 may introduce backend scoring)

### Architecture Completeness Checklist

**✅ Requirements Analysis**
- [x] Project context thoroughly analyzed (65 FRs, 24 NFRs)
- [x] Scale and complexity assessed (1K tenants × 500 users, medium-high)
- [x] Technical constraints identified (EventStore patterns, DAPR, identity scheme)
- [x] Cross-cutting concerns mapped (6 concerns + 5 Party Mode findings)

**✅ Architectural Decisions**
- [x] Critical decisions documented with versions (.NET 10.0.103, DAPR 1.17.3, Aspire 13.1.x)
- [x] Technology stack fully specified (all EventStore dependencies confirmed)
- [x] Integration patterns defined (DAPR pub/sub, actor model, projections)
- [x] Performance considerations addressed (snapshots, stateless scaling)

**✅ Implementation Patterns**
- [x] Naming conventions established (commands, events, rejection events, query contracts, API endpoints)
- [x] Structure patterns defined (type location rules table)
- [x] Communication patterns specified (event payload structure, versioning)
- [x] Process patterns documented (Handle/Apply, validation, error handling, logging)

**✅ Project Structure**
- [x] Complete directory structure defined (8 src, 5 test, 2 sample projects + DAPR components)
- [x] Component boundaries established (dependency graph)
- [x] Integration points mapped (data flow diagrams)
- [x] Requirements to structure mapping complete (FR-to-file table)

**✅ Testing Strategy (Party Mode Validation)**
- [x] Cross-tenant isolation test pattern defined with tier mapping (Critical)
- [x] Conformance test mandate for production-test parity (Critical)
- [x] Event serialization round-trip test with golden fixture plan (Important)
- [x] Snapshot performance test category and threshold defined (Medium)
- [x] Bootstrap multi-instance behavior documented as expected (Medium)

**✅ UX-Driven Amendments (2026-03-25)**
- [x] User Search authorization scoping — query-side result filtering (D11)
- [x] Audit projection and query design — TenantAuditProjection with date range + category (D12)
- [x] SignalR promoted to must-ship — degradation thresholds, multi-tenant subscription (D13)
- [x] Anomaly detection — client-side heuristics for MVP (D14)
- [x] Projection field enrichment — TenantReadModel + GetUserTenantsQuery metadata (D15)
- [x] Consequence preview data flow — client-side, no new endpoints (D16)
- [x] FrontShell cross-project dependencies documented (D17)

### Architecture Readiness Assessment

**Overall Status:** READY FOR IMPLEMENTATION

**Confidence Level:** HIGH — architecture is built on proven EventStore patterns with comprehensive domain-specific decisions

**Key Strengths:**
- Built on proven, production-tested EventStore framework
- Full FR and NFR coverage validated
- Party Mode review surfaced and resolved 8 architectural refinements
- Comprehensive implementation patterns with code examples prevent AI agent conflicts
- Type location rules provide deterministic guidance for every code artifact

**Areas for Future Enhancement:**
- QueryApi separation when scaling demands independent query scaling
- Bulk provisioning execution path (Phase 2)
- Real-time feature flag service boundary documentation
- Cross-tenant index `CachingProjectionActor` fan-in verification (Epic 5, Story 5.2)
- Server-side anomaly scoring for User Search (D14 — Phase 2)
- Audit timeline grouped-by-session mode (D12/D17 — fast-follow)

### Implementation Handoff

**AI Agent Guidelines:**
- Follow all architectural decisions exactly as documented
- Use implementation patterns consistently across all components
- Respect project structure and Type Location Rules table
- Place types in the correct project — Contracts for public API surface, Server for domain logic
- Implement Handle methods as `public static` pure functions
- Include `TenantId` as a top-level field in all event payloads
- Refer to this document for all architectural questions

**First Implementation Priority:**
Project initialization — scaffold the solution structure from EventStore's reference project:
1. Create `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`
2. Create `Hexalith.Tenants.slnx` with all 8 src + 5 test + 2 sample projects
3. Create `.csproj` files with correct dependencies
4. Verify `dotnet build` succeeds with empty projects
5. Verify `dotnet test` runs (no tests yet, but infrastructure works)
