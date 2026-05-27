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
lastStep: 8
status: 'complete'
completedAt: '2026-05-26'
inputDocuments:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/prd-validation-report.md
  - _bmad-output/planning-artifacts/product-brief-Hexalith.Tenants-2026-03-06.md
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/planning-artifacts/research/technical-hexalith-frontcomposer-to-create-hexalith-tenants-ux-research-2026-05-26.md
  - docs/compensating-commands.md
  - docs/cross-aggregate-timing.md
  - docs/demo.md
  - docs/event-contract-reference.md
  - docs/idempotent-event-processing.md
  - docs/production-auth-claim-contract.md
  - docs/production-auth-readiness.md
  - docs/quickstart.md
  - docs/tenants-ui-frontcomposer-dependency-map.md
  - docs/tenants-ui-phase-2-story-backlog.md
  - _bmad-output/project-context.md
  - Hexalith.Commons/_bmad-output/project-context.md
  - Hexalith.EventStore/_bmad-output/project-context.md
  - Hexalith.FrontComposer/_bmad-output/project-context.md
workflowType: 'architecture'
project_name: 'Hexalith.Tenants'
user_name: 'Jerome'
date: '2026-05-26'
---

# Architecture Decision Document

_This document builds collaboratively through step-by-step discovery. Sections are appended as we work through each architectural decision together._

## Project Context Analysis

### Requirements Overview

**Functional Requirements:**

The PRD defines 65 functional requirements across 11 categories:

| Category | FRs | Architectural Significance |
|----------|-----|----------------------------|
| Tenant Lifecycle Management | FR1-FR5 | Core tenant aggregate commands and lifecycle events: create, update, disable, enable |
| User-Role Management | FR6-FR12 | Membership commands, role changes, duplicate detection, escalation boundaries, optimistic concurrency |
| Global Administration | FR13-FR18 | Platform-level global administrator aggregate, bootstrap flow, cross-tenant authority |
| Tenant Configuration | FR19-FR24 | Low-frequency key-value configuration with namespace conventions and size/count limits |
| Tenant Discovery & Query | FR25-FR30 | Centralized read models, paginated query endpoints, audit query capability, cursor consistency |
| Role Behavior | FR31-FR34 | Reader/Contributor/Owner semantics and strict cross-tenant role isolation |
| Event-Driven Integration | FR35-FR42 | DAPR pub/sub, CloudEvents 1.0, local consumer projections, idempotent event handling |
| Developer Experience & Packaging | FR43-FR49 | Five NuGet packages, DI registration, in-memory testing fakes, Aspire extensions, actionable rejections |
| Command Validation & Error Handling | FR50-FR53 | Domain rejection model, disabled/nonexistent tenant behavior, source-of-truth event storage |
| Observability & Operations | FR54-FR58 | OpenTelemetry metrics, stateless operation, DAPR deployment, CI quality gates |
| Documentation & Adoption | FR59-FR65 | Quickstart, event contract reference, sample service, demo, eventual-consistency docs, compensating command docs |

The epic breakdown maps these requirements into 12 epics and 41 stories. Epics 1-8 cover the backend/package/documentation MVP. Epics 9-11 harden query correctness, projection durability, and production authorization. Epic 12 is Phase 2 Admin UI dependency sequencing and should not be treated as Phase 1 backend scope.

**Non-Functional Requirements:**

The PRD defines 24 NFRs across performance, security, scalability, integration, reliability, and accessibility/i18n.

Key architecture-shaping NFRs:

- Commands, read queries, and event publication each target 50ms p95.
- In-memory testing fakes target 10ms command/event execution.
- Cross-tenant data leaks must be zero across query, projection, and event subscription paths.
- Tenant isolation and role authorization logic require 100% branch coverage.
- Scale target is 1,000 tenants with up to 500 users per tenant.
- Startup reconstruction target is 500,000 events within the defined readiness threshold, using baseline EventStore snapshot behavior.
- Events must use CloudEvents 1.0, DAPR pub/sub, DAPR state abstraction, durable immutable storage, and idempotency metadata.
- Post-v1.0 event contracts must remain backward-compatible.
- Phase 2 Admin UI must address WCAG accessibility and localization concerns.

**Scale & Complexity:**

- Primary domain: event-sourced backend/platform infrastructure, with Phase 2 operational admin UI.
- Complexity level: high.
- Estimated architectural components: 15+ including five published NuGet packages, API host, AppHost, ServiceDefaults, aggregates, projections, query endpoints, DAPR components, test tiers, sample consuming service, documentation set, and a future FrontComposer UI module.

The system is not computationally complex in the algorithmic sense. Its complexity comes from correctness boundaries: multi-tenant isolation, event-sourced state, asynchronous projection lag, cross-service event propagation, production authentication, package compatibility, and test parity.

### Technical Constraints & Dependencies

- Runtime and language are constrained to .NET 10 / C# latest with nullable references, warnings as errors, and central package management.
- Solution format must remain `.slnx`; inline package versions are not allowed.
- Hexalith.EventStore is a root-level submodule dependency, not a NuGet dependency. Nested recursive submodule initialization is explicitly disallowed.
- EventStore patterns are mandatory: `EventStoreAggregate<TState>`, pure static `Handle` methods, state `Apply` methods, `DomainResult`, `IEventPayload`, `IRejectionEvent`, `IQueryContract`, `SubmitCommand`, `SubmitQuery`, and projection actors.
- DAPR is the infrastructure abstraction for actors, state, pub/sub, and service invocation. Domain services should not directly couple to Redis, databases, or brokers.
- Aspire owns local/distributed orchestration. AppHost topology changes require an Aspire restart.
- Authentication and authorization depend on EventStore claims validation plus Tenants domain RBAC. Production tokens must authorize the platform tenant context `system`.
- The managed tenant ID must be carried in event payloads because the EventStore envelope tenant remains the platform tenant.
- Query APIs and projections must account for eventual consistency and projection freshness.
- Phase 2 UI work depends on Hexalith.FrontComposer readiness, especially command lifecycle feedback, audit timeline, consequence preview, semantic tokens, accessibility, localization, and documentation evidence.

### Cross-Cutting Concerns Identified

1. Multi-tenant isolation across aggregates, projections, query endpoints, event subscriptions, cursor tokens, and error bodies.
2. Authorization layering: API gate, EventStore claims validation, Tenants domain RBAC, global-admin override, and query-side row filtering.
3. Event contract stability and payload design, especially persisted rejection events and post-v1.0 compatibility.
4. Projection correctness under concurrency, including cross-tenant indexes, audit timelines, pagination, and recovery from write conflicts.
5. Eventual consistency and read-after-write behavior for command results, projection updates, subscriber processing, and UX confirmation.
6. Bootstrap and production auth readiness, especially the `system` tenant claim contract and first global administrator setup.
7. Testing parity between production aggregates and in-memory fakes, including conformance tests and serialization round trips.
8. Operational observability through structured logs, OpenTelemetry metrics/traces, health checks, and safe support references.
9. Phase 2 UI trust model: command lifecycle state, projection freshness, consequence previews, audit evidence, accessibility, localization, and FrontComposer dependency governance.
10. Package and release governance: five NuGet packages, semantic-release, Conventional Commits, and public API compatibility.

## Starter Template Evaluation

### Primary Technology Domain

Hexalith.Tenants is a .NET 10 event-sourced backend/platform service with DAPR and Aspire orchestration, distributed as multiple NuGet packages and one deployable service. Phase 2 adds an operational Blazor/FrontComposer admin UI, but the Phase 1 architectural foundation is backend/package infrastructure.

The repository is already initialized, so starter evaluation is about which foundation should remain canonical, not which new CLI template should overwrite the tree.

### Starter Options Considered

**Option 1: Official Aspire Starter App (`aspire new aspire-starter`)**

The current Aspire starter creates a sample solution with an AppHost, ServiceDefaults, an API service, and a Blazor frontend. It is useful for greenfield distributed app learning and validates that Aspire is the right orchestration family.

Rejected as the primary starter because it does not encode the Hexalith.EventStore architecture: event-sourced aggregates, DAPR actors, command/query contracts, reflection-based handler discovery, five NuGet packages, testing fakes, DAPR component conventions, or the required dependency split between Contracts, Client, Server, Testing, Aspire, AppHost, and host projects.

**Option 2: Official Aspire Empty AppHost (`aspire new aspire-empty`)**

This is useful when adding Aspire orchestration to an existing solution.

Rejected as the primary starter because Hexalith.Tenants needs more than an AppHost. The package topology, EventStore integration, DAPR actor/service invocation path, test tiers, and semantic-release packaging rules remain project-specific.

**Option 3: Generic ASP.NET Core / Blazor / React starters**

Generic web/API starters could provide routing, UI scaffolding, or frontend conventions.

Rejected because they would introduce irrelevant defaults and likely conflict with the EventStore and FrontComposer boundaries. The Phase 2 UI should use Hexalith.FrontComposer and Fluent UI Blazor patterns, not a separate generic web starter.

**Option 4: Mirror the Hexalith.EventStore structure and adapt it to Tenants**

Selected. Hexalith.Tenants is a Hexalith.EventStore domain service and must follow the established ecosystem structure rather than a generic template.

### Selected Starter: Hexalith.EventStore Structure Mirror

**Rationale for Selection:**

The PRD, epics, project context, and existing repository all point to the same foundation: Hexalith.Tenants should be structured as an EventStore-native domain service with explicit package boundaries, DAPR/Aspire orchestration, and production/test parity.

This starter is not a third-party template. It is the ecosystem reference architecture already present through the `Hexalith.EventStore` root-level submodule.

**Initialization Command:**

No starter CLI command should be run against this existing repository.

For a greenfield reconstruction, the correct approach is manual scaffolding from the Hexalith.EventStore reference structure, preserving project names and package boundaries. Do not run `aspire new` over the repository.

**Architectural Decisions Provided by Starter:**

**Language & Runtime:**

- .NET 10 SDK pinned by `global.json` to `10.0.300` with latest patch roll-forward.
- C# `LangVersion=latest`, nullable references enabled, implicit usings enabled.
- Warnings as errors through shared build configuration.

**Styling Solution:**

- No Phase 1 frontend styling starter.
- Phase 2 UI should use Hexalith.FrontComposer plus Fluent UI Blazor, with exact component APIs verified against the pinned package before implementation.

**Build Tooling:**

- Modern `.slnx` solution format.
- Central package management through `Directory.Packages.props`.
- `Directory.Build.props` and `Directory.Build.targets` for shared build, container, and packaging conventions.
- No inline `Version=` attributes in project package references.

**Testing Framework:**

- xUnit v3, Shouldly, NSubstitute, Testcontainers, Aspire testing, and coverlet.
- Tiered testing model: unit/contract tests, DAPR/server tests, Aspire integration tests.
- Mandatory conformance, naming convention, serialization round-trip, isolation, projection safety, and auth readiness tests.

**Code Organization:**

- `src/Hexalith.Tenants.Contracts`
- `src/Hexalith.Tenants.Client`
- `src/Hexalith.Tenants.Server`
- `src/Hexalith.Tenants`
- `src/Hexalith.Tenants.Aspire`
- `src/Hexalith.Tenants.AppHost`
- `src/Hexalith.Tenants.ServiceDefaults`
- `src/Hexalith.Tenants.Testing`
- matching `tests/` projects and `samples/` consuming service.

**Development Experience:**

- Aspire AppHost for local distributed topology.
- DAPR sidecars/components for actors, state, pub/sub, and service invocation.
- OpenTelemetry and health checks through ServiceDefaults.
- Semantic-release and Conventional Commits for package release.
- Root-level submodules only; no recursive submodule initialization.

## Core Architectural Decisions

### Decision Priority Analysis

**Critical Decisions (Block Implementation):**

1. Use Hexalith.EventStore as the domain-service foundation, not a generic ASP.NET/Aspire template.
2. Keep event sourcing as the source of truth through EventStore aggregates, DAPR actor state, persisted events, and projections.
3. Model domain behavior with two aggregate families: `TenantAggregate` and `GlobalAdministratorsAggregate`.
4. Keep aggregates, states, and projections in `Hexalith.Tenants.Server`, the assembly scanned by EventStore.
5. Use the platform tenant context `system`, domain `tenants`, and managed tenant ID as aggregate ID.
6. Include managed `TenantId` in every tenant event payload because the EventStore envelope tenant is `system`.
7. Enforce authorization in layers: EventStore API gate, Tenants domain RBAC, global-admin override, and query-side filtering.
8. Expose commands through EventStore command submission and expose tenant queries through explicit REST query endpoints.
9. Use EventStore/DAPR projections for query state; do not introduce a direct database dependency.
10. Preserve production/test parity through pure Handle/Apply methods and conformance tests.

**Important Decisions (Shape Architecture):**

1. Use .NET 10 SDK `10.0.300`, C# latest, nullable references, warnings as errors, and central package management.
2. Use DAPR SDK `1.17.9` and Dapr runtime v1.17 family for actors, state, pub/sub, and service invocation.
3. Use Aspire AppHost orchestration with existing Aspire package family and CommunityToolkit DAPR integration.
4. Use MediatR `14.1.0` through EventStore command/query contracts.
5. Use OpenTelemetry OTLP `1.15.x` family and structured logs for observability.
6. Use xUnit v3, Shouldly, NSubstitute, Testcontainers, and Aspire testing for tiered validation.
7. Keep Phase 2 UI as a FrontComposer/Fluent UI Blazor adapter layer, not a rewrite of domain contracts.

**Deferred Decisions (Post-MVP):**

1. Bulk provisioning execution path.
2. Real-time feature flag service boundary beyond low-frequency tenant configuration.
3. Phase 2 Admin UI implementation details blocked by FrontComposer readiness.
4. Advanced audit timeline/grouped timeline UX.
5. Server-side anomaly scoring.
6. Consuming-service synchronous authorization enhancements beyond the current Tenants domain RBAC and EventStore validator model.

### Data Architecture

EventStore remains the source of truth. Aggregate state is reconstructed from persisted events and snapshots through EventStore/DAPR actor infrastructure.

**Aggregate model:**

- `TenantAggregate`: tenant lifecycle, user-role membership, tenant configuration.
- `GlobalAdministratorsAggregate`: platform-level global administrator set and bootstrap protection.

**Read model/projection model:**

- `TenantProjection`: per-tenant detail read model.
- `GlobalAdministratorsProjection`: global administrator read model.
- `TenantIndexProjection`: cross-tenant tenant/user lookup index.
- `TenantAuditProjection`: audit query read model.

Projection state uses EventStore projection conventions and DAPR state abstraction. Shared cross-tenant indexes must use ETag/optimistic concurrency or verified `CachingProjectionActor` fan-in behavior to avoid silent write loss.

Snapshot interval for the `tenants` domain remains 50 events; singleton/global administrator state uses the EventStore default unless evidence requires otherwise.

### Authentication & Security

Authentication uses ASP.NET Core JWT Bearer with EventStore claims transformation and validation.

Authorization is layered:

- API gate: EventStore tenant/domain/permission claims.
- Domain RBAC: aggregate Handle methods enforce Owner/Contributor/Reader rules.
- Global admin override: trusted `actor:globalAdmin` command-envelope extension, not raw user claims.
- Query filtering: query handlers restrict visible rows by caller scope.

Production identity providers must emit or normalize to `eventstore:tenant=system` for tenant-management operations. User identity comes from `sub`, never `name` or `email`.

Security-sensitive invariants:

- Tenant validation before aggregate state rehydration.
- No direct trust in user-supplied command extensions.
- Rejection events carry structured data only.
- No command payloads, event payloads, tokens, secrets, or PII in logs.
- Cross-tenant isolation tests are release blockers.

### Security & Contract Hardening Decisions (Correct Course 2026-05-27)

Resolves fail-open defaults and consumer-contract gaps raised in the Parties review (TEN-1 … TEN-5). See `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-27.md`.

- **Enum fail-safe serialization (TEN-1/TEN-2).** `TenantRole` and `TenantStatus` serialize **by name** (`JsonStringEnumConverter<T>`) and reserve ordinal `0` as a non-privileged `Unknown` sentinel. A missing field deserializes to `Unknown` (default-denied by `MeetsMinimumRole`; never `Active`); an unrecognized name fails closed via `JsonException`. The aggregate's `IsAssignableRole` guard and the `AddUserToTenant`/`ChangeUserRole` validators reject `Unknown`. Pre-v1.0 wire change (int→name + ordinal shift), recorded in CHANGELOG.
- **Identifier casing contract (TEN-3).** `sub`/userId and managed `tenantId` are compared case-sensitively (`StringComparer.Ordinal`) everywhere. Canonical casing is a boundary contract owned by the IdP/operator (OIDC `sub` is case-sensitive; case-folding could merge distinct subjects). A casing mismatch fails closed by design. Documented in `docs/production-auth-claim-contract.md`; consuming services rely on the contract instead of compensating.
- **Tenants.Testing result type (TEN-5).** `InMemoryTenantService`/`TenantTestHelpers` return `Hexalith.EventStore.Contracts.Results.DomainResult` intentionally — the canonical, in-tier outcome type reused by consumer tests without new coupling. No wrapper; no consumer fitness restriction.
- **Projection drift guard (TEN-4).** `InMemoryTenantProjection` keeps its silent `default:` arm for real-service parity, but `InMemoryTenantProjectionConformanceTests` fails if a `Contracts.Events` success event is added without being wired.

### API & Communication Patterns

Commands use EventStore command submission, with `POST /api/v1/commands` or the repository's current EventStore command route as the command gateway. Command handling returns success, rejection, or no-op through EventStore domain result semantics.

Queries are explicit REST endpoints backed by EventStore query contracts:

- `GET /api/tenants`
- `GET /api/tenants/{tenantId}`
- `GET /api/tenants/{tenantId}/users`
- `GET /api/users/{userId}/tenants`
- `GET /api/tenants/{tenantId}/audit`

Errors use RFC 7807 Problem Details. Domain rejections map to HTTP statuses without logging rejections as infrastructure errors.

Events publish through DAPR pub/sub as CloudEvents 1.0 on the tenant event topic. Consumers must assume at-least-once delivery and eventual consistency.

### Frontend Architecture

Phase 1 has no frontend implementation requirement.

Phase 2 Admin UI should use Hexalith.FrontComposer and Fluent UI Blazor through an adapter-backed composition layer:

- Do not annotate or reshape immutable Tenants domain contracts for UI generation.
- Add UI-facing command/projection models and mappings.
- Use SignalR projection notifications only as refresh nudges, not source-of-truth state.
- Keep command lifecycle, projection freshness, consequence preview, audit evidence, accessibility, localization, and documentation as explicit readiness gates.

### Infrastructure & Deployment

Aspire AppHost is the local/distributed orchestration model. DAPR provides actors, state, pub/sub, and service invocation. Containers are produced through .NET SDK container publishing, not Dockerfiles, unless a future deployment target proves otherwise.

CI/CD remains GitHub Actions plus semantic-release:

- Restore/build/test on PR and push.
- Tier 1 and Tier 2 tests in the blocking lane.
- Tier 3/Aspire tests where infrastructure is available.
- Package validation before publishing five NuGet packages.

Operational telemetry uses OpenTelemetry and structured logging with correlation, tenant, domain, aggregate, causation, command/event type, and stage metadata.

### Decision Impact Analysis

**Implementation Sequence:**

1. Preserve solution/package topology and central build configuration.
2. Keep EventStore aggregate and projection placement in `Hexalith.Tenants.Server`.
3. Enforce identity and authorization invariants before expanding command/query surface.
4. Harden projections, cursor pagination, audit query behavior, and concurrency.
5. Validate production auth and deployment readiness.
6. Treat Phase 2 UI as a separate adapter/composition track.

**Cross-Component Dependencies:**

- Command contracts affect event contracts, rejection mapping, tests, docs, and semantic-release impact.
- Projection shape affects query endpoints, cursor stability, UI readiness, audit evidence, and isolation tests.
- Auth decisions affect command handling, query filtering, production smoke tests, and FrontComposer command affordances.
- DAPR/Aspire topology affects local development, Tier 2/3 tests, deployment docs, and operational troubleshooting.

## Implementation Patterns & Consistency Rules

### Pattern Categories Defined

**Critical Conflict Points Identified:**
22 areas where AI agents could make different choices: type placement, command/event naming, aggregate signatures, projection state keys, REST routes, error formats, JSON casing, date/time representation, authorization checks, logging, telemetry, retry policy, test location, test helpers, DAPR resource names, package references, UI model boundaries, and Phase 2 command-feedback states.

### Naming Patterns

**Database Naming Conventions:**

No direct database table/column naming is owned by Hexalith.Tenants. Durable state is accessed through EventStore and DAPR abstractions.

Agents must name DAPR/EventStore resources through existing conventions:

- AppId: `tenants`
- State store: `tenants-eventstore`
- Topic: `tenants.events`
- Dead letter topic: `deadletter.tenants.events`
- Actor identity: `system:tenants:{aggregateId}`

Projection state keys must follow existing projection policy types and tests. Do not invent ad hoc Redis/database key names inside domain code.

**API Naming Conventions:**

- REST endpoints use plural nouns: `/api/tenants`, `/api/tenants/{tenantId}/users`.
- Route parameters use ASP.NET Core `{tenantId}` style.
- Query parameters and JSON fields use camelCase.
- Command submission uses EventStore command envelope routes and payload shape; do not create per-command REST endpoints.
- Audit endpoint remains `/api/tenants/{tenantId}/audit`.

**Code Naming Conventions:**

- Commands: `{Verb}{Target}` such as `CreateTenant`, `AddUserToTenant`.
- Events: `{Target}{PastVerb}` such as `TenantCreated`, `UserAddedToTenant`.
- Rejections: `{Target}{Reason}Rejection` and implement `IRejectionEvent`.
- Aggregates: `TenantAggregate`, `GlobalAdministratorsAggregate`.
- State: `TenantState`, `GlobalAdministratorsState`.
- Projections/read models: `TenantProjection`, `TenantReadModel`, `TenantAuditProjection`.
- Private fields use `_camelCase`; public members use PascalCase.
- File name matches the single type in the file.

### Structure Patterns

**Project Organization:**

- Public immutable contracts live in `Hexalith.Tenants.Contracts`.
- Client registration and consumer-facing event processing live in `Hexalith.Tenants.Client`.
- Aggregates, state, validators, projections, and read models live in `Hexalith.Tenants.Server`.
- Host/API/controllers/domain-processing live in `Hexalith.Tenants`.
- Aspire hosting extensions live in `Hexalith.Tenants.Aspire`.
- In-memory test fakes live in `Hexalith.Tenants.Testing`.
- Tests live in matching `tests/Hexalith.Tenants.*.Tests` projects.

**File Structure Patterns:**

- Contracts use folders: `Commands`, `Events`, `Events/Rejections`, `Enums`, `Identity`, `Queries`.
- Server uses folders: `Aggregates`, `Projections`, `Validators`.
- Host uses folders by runtime concern: `Controllers`, `Bootstrap`, `Configuration`, `DomainProcessing`, `Projections`, `Queries`, `Telemetry`, `Validation`.
- Do not place generated, `bin`, `obj`, coverage, or local cache artifacts in architecture or source changes.
- Do not add inline `Version=` to `PackageReference`.

### Format Patterns

**API Response Formats:**

- Domain rejection responses use RFC 7807 Problem Details.
- Rejection `type` uses the rejection event type name.
- Rejection payloads contain structured data only, never English prose.
- Command outcomes follow EventStore success/rejection/no-op semantics.
- Query responses use typed query DTOs and pagination contracts, not anonymous shapes.

**Data Exchange Formats:**

- JSON uses camelCase at API boundaries.
- Events and commands use `System.Text.Json`.
- Timestamps use `DateTimeOffset` and `{Action}At` names.
- IDs that represent message/correlation/aggregate/causation identifiers use ULID validation where applicable.
- Every tenant event payload includes top-level `TenantId`.

### Communication Patterns

**Event System Patterns:**

- Events are immutable records implementing EventStore payload contracts.
- Events are past-tense facts, not commands or status messages.
- DAPR pub/sub is at-least-once; consumers must be idempotent.
- Consumers filter by event type and must not assume cross-service ordering.
- Domain rejections are normal events, not infrastructure errors.

**State Management Patterns:**

- Aggregate `Handle` methods are `public static`, pure, synchronous functions returning `DomainResult`.
- State `Apply` methods mutate state and perform no validation.
- Projection Apply methods trust events and update read models deterministically.
- Shared projection writes must use the selected optimistic concurrency/write policy.
- Phase 2 UI state must distinguish projection data from local pending/confirming command hints.

### Process Patterns

**Error Handling Patterns:**

- Business rule failures return `DomainResult.Rejection`.
- Infrastructure/programmer failures may throw and are handled by the host pipeline.
- Domain Handle methods do not log rejections.
- ProblemDetails mapping happens at the HTTP boundary.
- User-facing text is composed outside persisted rejection payloads.

**Loading State Patterns:**

- Backend code does not invent UI loading states.
- Phase 2 UI must represent loading, stale, refreshing, degraded, accepted, confirmed, rejected, audit pending, and unable-to-verify distinctly.
- SignalR projection notifications are refresh nudges only.
- Do not mark UI command success until status/projection/audit evidence supports it.

### Enforcement Guidelines

**All AI Agents MUST:**

- Keep aggregates and projections in `Hexalith.Tenants.Server`.
- Use existing EventStore command/query/projection contracts.
- Preserve `system` platform tenant and `tenants` domain identity rules.
- Include managed `TenantId` in tenant event payloads.
- Use Shouldly, not `Assert.*`, in tests.
- Use central package management only.
- Avoid recursive submodule initialization.
- Run focused build/test validation for touched areas.

**Pattern Enforcement:**

- Naming convention tests enforce command/event/rejection names.
- Serialization round-trip tests enforce event contract stability.
- Conformance tests enforce production/testing parity.
- Cross-tenant isolation tests enforce security boundaries.
- Code review must reject misplaced types, inline package versions, raw payload logging, direct infrastructure coupling, and ad hoc auth shortcuts.

### Pattern Examples

**Good Examples:**

- `public record TenantCreated(string TenantId, string Name, string? Description, DateTimeOffset CreatedAt) : IEventPayload;`
- `DomainResult.Rejection([new TenantNotFoundRejection(command.TenantId)])`
- `GET /api/tenants/{tenantId}/users`
- `logger.LogInformation("Tenant bootstrap skipped: TenantId={TenantId}", tenantId);`

**Anti-Patterns:**

- Adding `TenantAggregate` outside `Hexalith.Tenants.Server`.
- Throwing an exception for `TenantNotFound`.
- Logging serialized command or event payloads.
- Adding `Version=` to a project-level `PackageReference`.
- Treating SignalR notification payloads as durable projection state.
- Adding a new database or broker dependency directly to Tenants domain code.

## Project Structure & Boundaries

### Complete Project Directory Structure

```text
Hexalith.Tenants/
├── global.json
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
├── Hexalith.Tenants.slnx
├── .editorconfig
├── .gitattributes
├── .gitignore
├── .gitmodules
├── .releaserc.json
├── README.md
├── CONTRIBUTING.md
├── CHANGELOG.md
├── docs/
│   ├── quickstart.md
│   ├── event-contract-reference.md
│   ├── idempotent-event-processing.md
│   ├── cross-aggregate-timing.md
│   ├── compensating-commands.md
│   ├── demo.md
│   ├── production-auth-claim-contract.md
│   ├── production-auth-readiness.md
│   ├── tenants-ui-frontcomposer-dependency-map.md
│   └── tenants-ui-phase-2-story-backlog.md
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── release.yml
├── src/
│   ├── Hexalith.Tenants.Contracts/
│   │   ├── Commands/
│   │   ├── Enums/
│   │   ├── Events/
│   │   │   └── Rejections/
│   │   ├── Identity/
│   │   └── Queries/
│   ├── Hexalith.Tenants.Client/
│   │   ├── Configuration/
│   │   ├── Handlers/
│   │   ├── Projections/
│   │   ├── Registration/
│   │   └── Subscription/
│   ├── Hexalith.Tenants.Server/
│   │   ├── Aggregates/
│   │   ├── Projections/
│   │   └── Validators/
│   ├── Hexalith.Tenants/
│   │   ├── Actors/
│   │   ├── Bootstrap/
│   │   ├── Configuration/
│   │   ├── Controllers/
│   │   ├── DomainProcessing/
│   │   ├── Health/
│   │   ├── Projections/
│   │   ├── Queries/
│   │   ├── Telemetry/
│   │   ├── Validation/
│   │   └── Program.cs
│   ├── Hexalith.Tenants.Aspire/
│   ├── Hexalith.Tenants.AppHost/
│   │   ├── DaprComponents/
│   │   ├── KeycloakRealms/
│   │   └── Program.cs
│   ├── Hexalith.Tenants.ServiceDefaults/
│   └── Hexalith.Tenants.Testing/
│       ├── Fakes/
│       ├── Helpers/
│       └── Projections/
├── tests/
│   ├── Directory.Build.props
│   ├── Hexalith.Tenants.Contracts.Tests/
│   ├── Hexalith.Tenants.Client.Tests/
│   ├── Hexalith.Tenants.Server.Tests/
│   ├── Hexalith.Tenants.Testing.Tests/
│   └── Hexalith.Tenants.IntegrationTests/
├── samples/
│   ├── Hexalith.Tenants.Sample/
│   │   ├── Endpoints/
│   │   └── Handlers/
│   └── Hexalith.Tenants.Sample.Tests/
├── scripts/
├── Hexalith.EventStore/
├── Hexalith.Commons/
├── Hexalith.FrontComposer/
└── Hexalith.AI.Tools/
```

### Architectural Boundaries

**API Boundaries:**

- `src/Hexalith.Tenants` owns HTTP hosting, authentication setup, command gateway integration, query controllers, domain processing route, health checks, and telemetry.
- Commands enter through EventStore command submission. Do not create per-command controllers.
- Queries are exposed through `TenantsQueryController` and dispatch through EventStore query contracts.
- Error responses are ProblemDetails at the HTTP boundary.

**Component Boundaries:**

- `Contracts` is public immutable API surface.
- `Client` is consumer integration surface.
- `Server` is domain logic and EventStore-discovered aggregate/projection surface.
- `Testing` provides in-memory fakes and helpers without changing production domain behavior.
- `Aspire` and `AppHost` own orchestration and hosting composition, not domain rules.

**Service Boundaries:**

- Tenants depends on EventStore primitives through the root-level submodule.
- DAPR is the only infrastructure abstraction for actors, state, pub/sub, and service invocation.
- Consuming services subscribe to tenant events and build their own local projections.

**Data Boundaries:**

- EventStore events are the source of truth.
- Aggregate state is reconstructed from event history and snapshots.
- Query read models are projections, not authoritative write state.
- No direct Redis/database/broker coupling is allowed in domain code.

### Requirements to Structure Mapping

**Feature/Epic Mapping:**

- Epic 1 Foundation -> root build files, `.github/workflows`, `src/*`, `tests/*`.
- Epic 2 Tenant lifecycle/global admin -> `Contracts/Commands`, `Contracts/Events`, `Server/Aggregates`, `src/Hexalith.Tenants/Bootstrap`.
- Epic 3 Membership/roles/config -> `Server/Aggregates`, `Server/Validators`, rejection events, aggregate tests.
- Epic 4 Event integration -> `Client/Handlers`, `Client/Subscription`, `samples/Hexalith.Tenants.Sample`.
- Epic 5 Tenant queries -> `Contracts/Queries`, `Server/Projections`, `src/Hexalith.Tenants/Controllers`, `src/Hexalith.Tenants/Queries`.
- Epic 6 Testing package -> `src/Hexalith.Tenants.Testing`, `tests/Hexalith.Tenants.Testing.Tests`.
- Epic 7 Deployment/observability -> `AppHost`, `Aspire`, `ServiceDefaults`, `Telemetry`, `Health`.
- Epic 8 Documentation/adoption -> `docs/`, `README.md`, sample project.
- Epic 9 Query hardening -> `Queries`, query controller tests, cursor/pagination utilities.
- Epic 10 Projection durability -> host projection state store, projection write policy, projection recovery tests.
- Epic 11 Production auth -> `Configuration`, `Validation`, auth tests, production auth docs.
- Epic 12 Phase 2 UI sequencing -> `docs/tenants-ui-*`, future FrontComposer adapter module only after readiness.

**Cross-Cutting Concerns:**

- Authorization -> EventStore validators, aggregate RBAC, query filtering, server/auth tests.
- Observability -> `Telemetry`, structured logs, ServiceDefaults, docs.
- Serialization and contract stability -> `Contracts.Tests`.
- Production/test parity -> `Testing.Tests/Conformance`.

### Integration Points

**Internal Communication:**

- HTTP command request -> EventStore command pipeline -> DAPR actor -> domain processor route -> aggregate Handle -> event persistence -> projections.
- Query request -> Tenants query controller -> EventStore query dispatch -> projection actor/read model.

**External Integrations:**

- DAPR sidecars for actors, state, pub/sub, and service invocation.
- Aspire AppHost for local topology and resource wiring.
- JWT/OIDC identity providers through EventStore claim contract.
- NuGet consumers through Contracts, Client, Server, Aspire, and Testing packages.

**Data Flow:**

1. Client submits command envelope.
2. EventStore validates auth and routes command to aggregate actor.
3. Aggregate returns success, rejection, or no-op.
4. Events are persisted as source of truth.
5. Events are published through DAPR pub/sub.
6. Tenants and consuming-service projections update asynchronously.
7. Query endpoints serve projection state with explicit eventual-consistency semantics.

### File Organization Patterns

**Configuration Files:**

- Build/package config stays at repo root.
- Runtime appsettings stay in the owning host project.
- DAPR component examples stay under `AppHost/DaprComponents`.
- Keycloak local sample realm stays under `AppHost/KeycloakRealms`.

**Source Organization:**

- New public contracts go in `Contracts`.
- New domain behavior goes in `Server`.
- New host wiring goes in `src/Hexalith.Tenants`.
- New consumer helpers go in `Client`.
- New test helpers/fakes go in `Testing`.

**Test Organization:**

- Tests mirror the owning project.
- Unit/domain tests stay in `Server.Tests` or `Contracts.Tests`.
- Consumer integration behavior stays in `Client.Tests`.
- In-memory fake parity stays in `Testing.Tests`.
- AppHost/DAPR/OIDC runtime behavior stays in `IntegrationTests`.

**Asset Organization:**

- Phase 1 has no frontend asset pipeline.
- Documentation images/media belong under `docs/` only when intentionally added.
- Future Phase 2 UI assets belong in the FrontComposer adapter/UI project, not in backend packages.

### Development Workflow Integration

**Development Server Structure:**

- Local distributed execution starts from `src/Hexalith.Tenants.AppHost`.
- AppHost changes require restart.
- Root-level submodules only; do not initialize nested submodules recursively.

**Build Process Structure:**

- Build uses `Hexalith.Tenants.slnx`.
- Package versions come only from `Directory.Packages.props`.
- Published packages are Contracts, Client, Server, Aspire, and Testing.

**Deployment Structure:**

- Host projects are not NuGet packages.
- Container publishing uses .NET SDK container support.
- Release automation uses semantic-release and Conventional Commits.

## Architecture Validation Results

### Coherence Validation ✅

**Decision Compatibility:**
The decisions are compatible. .NET 10, EventStore, DAPR, Aspire, MediatR, OpenTelemetry, xUnit v3, and central package management align with the current repository and project context. The architecture does not mix generic starter assumptions with EventStore-specific runtime behavior.

**Pattern Consistency:**
Implementation patterns support the core decisions. Type placement, command/event/rejection naming, aggregate signatures, projection rules, REST routes, ProblemDetails errors, System.Text.Json, DateTimeOffset timestamps, and Shouldly-based tests are consistent with the repo.

**Structure Alignment:**
The project structure supports the decisions. The package split maps cleanly to Contracts, Client, Server, host, Aspire, AppHost, ServiceDefaults, Testing, tests, samples, and docs. Boundaries are explicit enough to prevent common agent conflicts.

### Requirements Coverage Validation ✅

**Epic/Feature Coverage:**
All 12 epics have architectural support. Epics 1-8 map to the backend/package/documentation MVP. Epics 9-11 map to query hardening, projection durability, and production auth readiness. Epic 12 is correctly scoped as Phase 2 UI sequencing.

**Functional Requirements Coverage:**
All FR groups are covered:

- FR1-FR5: tenant lifecycle aggregate and events.
- FR6-FR12: membership/role aggregate behavior and validation.
- FR13-FR18: global administrator aggregate and bootstrap.
- FR19-FR24: tenant configuration and limits.
- FR25-FR30: query endpoints, projections, pagination, audit.
- FR31-FR34: role behavior and cross-tenant isolation.
- FR35-FR42: DAPR pub/sub, CloudEvents, consumer projections, idempotency.
- FR43-FR49: packages, DI, testing, Aspire, rejection handling.
- FR50-FR53: domain rejection and event-store source-of-truth behavior.
- FR54-FR58: telemetry, deployment, statelessness, CI gates.
- FR59-FR65: docs, demo, timing, compensating commands.

**Non-Functional Requirements Coverage:**
Performance, security, scalability, integration, reliability, and accessibility/i18n are architecturally addressed. Security and projection correctness are the highest-risk areas and are supported by layered authorization, isolation tests, EventStore source-of-truth rules, projection write safety, and explicit Phase 2 UI gates.

### Implementation Readiness Validation ✅

**Decision Completeness:**
Critical decisions are documented with current versions and repository-specific constraints. Deferred decisions are named and scoped so agents should not implement them accidentally.

**Structure Completeness:**
The structure section is concrete and matches the current repository layout. It defines where each major artifact belongs and which directories own runtime, contracts, domain, tests, docs, and orchestration.

**Pattern Completeness:**
The main conflict points are addressed: naming, type placement, events, query routes, serialization, auth, logging, tests, package management, submodules, DAPR resources, and Phase 2 UI state boundaries.

### Gap Analysis Results

**Critical Gaps: None.**

**Important Gaps:**

1. Phase 2 UI implementation is intentionally not ready until FrontComposer command lifecycle, audit timeline, consequence preview, accessibility, localization, and documentation evidence are resolved.
2. Projection fan-in/write-safety implementation must continue to prove no silent write loss under concurrent updates.
3. Production auth remains deployment-sensitive and must be verified with smoke tests and environment-specific OIDC configuration.
4. The current host reference collision noted in the project file remains an implementation cleanup item, not an architectural blocker.

**Nice-to-Have Gaps:**

1. More detailed future architecture for a dedicated FrontComposer adapter module.
2. More detailed deployment target matrix for Docker, Kubernetes, and Azure Container Apps.
3. Expanded operational dashboard conventions once production telemetry evidence exists.

### Validation Issues Addressed

No critical validation issues required changes. Non-blocking gaps were classified as deferred or implementation evidence items.

### Architecture Completeness Checklist

**Requirements Analysis**

- [x] Project context thoroughly analyzed
- [x] Scale and complexity assessed
- [x] Technical constraints identified
- [x] Cross-cutting concerns mapped

**Architectural Decisions**

- [x] Critical decisions documented with versions
- [x] Technology stack fully specified
- [x] Integration patterns defined
- [x] Performance considerations addressed

**Implementation Patterns**

- [x] Naming conventions established
- [x] Structure patterns defined
- [x] Communication patterns specified
- [x] Process patterns documented

**Project Structure**

- [x] Complete directory structure defined
- [x] Component boundaries established
- [x] Integration points mapped
- [x] Requirements to structure mapping complete

### Architecture Readiness Assessment

**Overall Status:** READY FOR IMPLEMENTATION

**Confidence Level:** high

**Key Strengths:**

- Strong alignment with the existing EventStore ecosystem.
- Clear package and assembly boundaries.
- Explicit security and multi-tenant invariants.
- Strong test parity and conformance expectations.
- Phase 2 UI dependencies are scoped without contaminating Phase 1 backend architecture.

**Areas for Future Enhancement:**

- FrontComposer adapter/module architecture once UI dependencies are ready.
- Deployment target-specific architecture details.
- Production telemetry dashboard conventions.
- Advanced audit and anomaly analysis patterns.

### Implementation Handoff

**AI Agent Guidelines:**

- Follow all architectural decisions exactly as documented.
- Use implementation patterns consistently across all components.
- Respect project structure and boundaries.
- Refer to this document for architectural questions.
- Treat deferred decisions as out of scope unless a later story explicitly promotes them.

**First Implementation Priority:**
Continue story-driven implementation from `_bmad-output/implementation-artifacts/`, using the current sprint/story file as the source of truth. For new work, start with the narrowest relevant story and validate against this architecture, the project-context rules, and affected tests.
