---
stepsCompleted:
    - step-01-validate-prerequisites
    - step-02-design-epics
    - step-03-create-stories
    - step-04-final-validation
status: complete
completedAt: "2026-03-07"
lastUpdated: "2026-05-15"
followUpStepsCompleted:
    - step-01-validate-prerequisites
    - step-02-design-epics
    - step-03-create-stories
    - step-04-final-validation
inputDocuments:
    - prd.md
    - architecture.md
    - ux-design-specification.md
    - ../implementation-artifacts/deferred-work.md
---

# Hexalith.Tenants - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Hexalith.Tenants, decomposing the requirements from the PRD and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

**Tenant Lifecycle Management (FR1-FR5)**

- FR1: A global administrator can create a new tenant with a unique identifier and name (in MVP, tenant creation is restricted to global administrators)
- FR2: A developer can update a tenant's metadata (name, description)
- FR3: A global administrator can disable a tenant, preventing all commands against that tenant from succeeding
- FR4: A global administrator can re-enable a previously disabled tenant, restoring normal command processing
- FR5: The system produces a domain event for every tenant lifecycle change (created, updated, disabled, enabled)

**User-Role Management (FR6-FR12)**

- FR6: A tenant owner can add a user to a tenant with a specified role (TenantOwner, TenantContributor, or TenantReader)
- FR7: A tenant owner can remove a user from a tenant
- FR8: A tenant owner can change a user's role within a tenant
- FR9: The system rejects adding a user who is already a member of the tenant
- FR10: The system rejects role changes that violate escalation boundaries (a tenant owner cannot assign GlobalAdministrator)
- FR11: The system produces a domain event for every user-role change (added, removed, role changed)
- FR12: The system enforces optimistic concurrency, rejecting conflicting concurrent modifications to the same aggregate

**Global Administration (FR13-FR18)**

- FR13: An existing global administrator can designate a user as a global administrator
- FR14: An existing global administrator can remove a user's global administrator status (cannot remove self if they are the last global administrator)
- FR15: A global administrator can perform any tenant operation across all tenants without per-tenant role assignment
- FR16: All global administrator actions produce auditable domain events
- FR17: The system provides a bootstrap mechanism (seed command or startup configuration) to create the initial global administrator on first deployment when no global administrators exist
- FR18: The bootstrap mechanism only executes when zero global administrators exist in the event store -- subsequent executions are rejected with a specific error indicating that bootstrap has already been completed

**Tenant Configuration (FR19-FR24)**

- FR19: A tenant owner can set a key-value configuration entry for a tenant
- FR20: A tenant owner can remove a configuration entry from a tenant
- FR21: Configuration keys support dot-delimited namespace conventions (e.g., `billing.plan`, `parties.maxContacts`) to prevent collisions between consuming services
- FR22: The system produces a domain event for every configuration change (set, removed)
- FR23: The system enforces configuration limits: maximum 100 keys per tenant, maximum 1KB per value, maximum 256 characters per key
- FR24: The system rejects configuration operations that exceed limits with a specific error identifying which limit was exceeded and the current usage

**Tenant Discovery & Query (FR25-FR30)**

- FR25: A developer can query a paginated list of all tenants with their IDs, names, and statuses
- FR26: A developer can query a specific tenant's details including its current users and their roles
- FR27: A developer can query the list of users in a specific tenant with their assigned roles
- FR28: A developer can query the list of tenants a specific user belongs to, with their role in each tenant
- FR29: A global administrator can query tenant access changes by tenant ID and date range for audit reporting, with pagination support (default page size: 100 results, maximum: 1,000)
- FR30: All list and query endpoints support cursor-based pagination with consistent ordering

**Role Behavior (FR31-FR34)**

- FR31: A TenantReader can query tenant details, user lists, and configuration for tenants they belong to, but cannot execute any state-changing commands
- FR32: A TenantContributor has TenantReader capabilities plus the ability to execute domain commands within the tenant (the specific commands are defined by each consuming service)
- FR33: A TenantOwner has TenantContributor capabilities plus user-role management (add, remove, change role) and tenant configuration management
- FR34: A user with roles in multiple tenants can only access data and execute commands within each tenant according to their role in that specific tenant -- roles do not transfer or aggregate across tenants

**Event-Driven Integration (FR35-FR42)**

- FR35: The system publishes all tenant domain events via DAPR pub/sub as CloudEvents 1.0
- FR36: The system uses a documented topic naming convention for tenant events (e.g., `tenants.events`) consistent with Hexalith ecosystem patterns
- FR37: A consuming service can subscribe to tenant events and build a local projection of tenant state
- FR38: A consuming service can react to user addition/removal events to enforce or revoke access
- FR39: A consuming service can react to tenant disable/enable events to block or allow operations
- FR40: A consuming service can react to configuration change events to update tenant-specific behavior
- FR41: Event contracts include sufficient information (event ID, aggregate version) for consuming services to implement idempotent event handling
- FR42: Documentation provides guidance on idempotent event processing patterns for consumers, since DAPR pub/sub may deliver events more than once

**Developer Experience & Packaging (FR43-FR49)**

- FR43: A developer can install Hexalith.Tenants via NuGet packages (Contracts, Client, Server, Testing, Aspire)
- FR44: A developer can register tenant client services in DI with a single extension method call
- FR45: A developer can register tenant event handlers in a consuming service in under 20 lines of DI configuration
- FR46: A developer can write tenant integration tests using in-memory fakes without external infrastructure, in under 10 lines per test
- FR47: The in-memory testing fakes execute the same domain logic as the production service, guaranteeing isolation at the aggregate domain model level, verified by a conformance test suite
- FR48: A developer can deploy the tenant service using .NET Aspire hosting extensions
- FR49: The system provides error messages for all command rejections that include: the specific rejection reason, the entity involved, and a corrective action hint

**Command Validation & Error Handling (FR50-FR53)**

- FR50: The system rejects commands targeting a non-existent tenant with a specific error identifying the missing tenant
- FR51: The system rejects commands targeting a disabled tenant with a specific error indicating the tenant's disabled status
- FR52: The system rejects duplicate operations (e.g., adding an already-present user) with a specific error including current state
- FR53: Commands and event storage succeed independently of DAPR pub/sub availability (event store is the source of truth)

**Observability & Operations (FR54-FR58)**

- FR54: The system exposes tenant command latency metrics via OpenTelemetry
- FR55: The system exposes event processing metrics via OpenTelemetry
- FR56: A platform operator can deploy the tenant service alongside EventStore using standard DAPR configuration
- FR57: The tenant service is stateless between requests -- all state is reconstructed from the event store on startup
- FR58: The CI/CD pipeline enforces quality gates: build, test (Tier 1+2), coverage threshold (> 80% line, 100% branch on isolation/auth), and package validation before NuGet publish

**Documentation & Adoption (FR59-FR65)**

- FR59: The project provides a quickstart guide that enables a developer to send their first tenant command within 30 minutes
- FR60: The quickstart guide includes prerequisite validation (DAPR sidecar, EventStore deployment)
- FR61: The project provides an event contract reference documenting all commands, events, and their schemas
- FR62: The project provides a sample consuming service demonstrating event subscription and access enforcement
- FR63: The project provides an "aha moment" demo (screencast or video) showing reactive cross-service access revocation
- FR64: The project provides documentation on cross-aggregate timing behavior, including the event propagation window between tenant commands and subscriber processing
- FR65: The project provides documentation on compensating command patterns (e.g., restoring a wrongly removed user with explicit role specification)

### NonFunctional Requirements

**Performance (NFR1-NFR4)**

- NFR1: All tenant commands complete within 50ms (p95) as measured by OpenTelemetry span duration
- NFR2: All read model queries complete within 50ms (p95) for result sets within a single page, as measured by OpenTelemetry span duration
- NFR3: Event publication to DAPR pub/sub completes within 50ms (p95) after command processing, as measured by OpenTelemetry span duration
- NFR4: In-memory testing fakes execute commands and produce events within 10ms, as measured by xUnit test execution time

**Security (NFR5-NFR10)**

- NFR5: Zero cross-tenant data leaks -- no query, projection, or event subscription returns data belonging to a different tenant, verified by dedicated Tier 3 integration tests
- NFR6: Role escalation boundaries enforced at the domain level -- no actor can self-escalate, verified by unit tests
- NFR7: All state-changing operations produce immutable, auditable domain events with actor ID, timestamp, and full operation context
- NFR8: Disabled tenants reject all commands immediately within the same aggregate, verified by unit tests
- NFR9: Encryption at rest and in transit is a deployment concern -- relies on DAPR infrastructure configuration
- NFR10: 100% branch coverage on tenant isolation and role authorization logic, verified in CI via coverlet

**Scalability (NFR11-NFR13)**

- NFR11: The system supports up to 1,000 tenants with up to 500 users per tenant without performance degradation beyond stated latency targets
- NFR12: The tenant service is stateless -- horizontal scaling achieved by adding service instances
- NFR13: State reconstruction from the event store on startup completes within 30 seconds for up to 1,000 tenants with 500 events per tenant (500,000 total events)

**Integration (NFR14-NFR19)**

- NFR14: All domain events conform to CloudEvents 1.0 specification
- NFR15: Event publication uses DAPR pub/sub abstraction -- no direct dependency on a specific message broker
- NFR16: State persistence uses DAPR state store abstraction -- no direct dependency on a specific database
- NFR17: The system degrades gracefully when DAPR pub/sub is unavailable -- commands succeed, subscribers catch up when pub/sub recovers
- NFR18: Event contracts are backward-compatible after v1.0 -- no breaking schema changes to published events
- NFR19: All domain events include event ID and aggregate version to enable idempotent processing by consumers

**Reliability (NFR20-NFR23)**

- NFR20: The event store is the single source of truth -- system state can be fully reconstructed by replaying events
- NFR21: Command processing and event storage are atomic -- a command either fully succeeds or fully fails
- NFR22: API availability target: 99.9% in production deployments, as measured by health check endpoint uptime monitoring
- NFR23: No data loss under any failure scenario -- events once stored are immutable and durable

**Accessibility & Internationalization (NFR24)**

- NFR24: MVP error messages and documentation are English-only. Phase 2 Admin UI must address WCAG 2.1 AA accessibility and i18n considerations

### Additional Requirements

**From Architecture:**

- Starter Template: Scaffold solution by mirroring EventStore's structure with `Hexalith.Tenants` naming. Manual scaffolding from the reference project (no CLI template). Project initialization should be the first implementation story
- Two aggregates: TenantAggregate (lifecycle, user-role management, configuration) and GlobalAdministratorsAggregate (cross-tenant admin roles, bootstrap). Separate because GlobalAdmin is platform-level, not tenant-scoped
- Identity Mapping: Platform tenant context = `system` (configurable), domain = `tenants`, aggregateId = managed tenant ID or `global-administrators`. Actor IDs: `system:tenants:acme-corp`, `system:tenants:global-administrators`
- Pub/Sub topic: `tenants.events` -- single topic for all tenant events; consumers filter by event type
- Read model: `EventStoreProjection<TReadModel>` pattern with DAPR state store. Three projections needed: TenantProjection, GlobalAdministratorProjection, TenantIndexProjection (cross-tenant)
- Cross-tenant index projections use ETag-based optimistic concurrency (`ConcurrencyMode.FirstWrite`) with retry logic (max 3 attempts) to prevent silent data loss on concurrent updates
- Snapshot strategy: 50-event interval for tenant domain, default 100 for GlobalAdministratorsAggregate
- Bootstrap mechanism: Startup config via `appsettings.json` (`Tenants:BootstrapGlobalAdminUserId`), executed through full MediatR pipeline. GlobalAdministratorsAggregate rejects if any GlobalAdministratorSet event exists
- Query endpoints served from Hexalith.Tenants as route groups (single deployable) -- `POST /api/commands` and `GET /api/tenants/*`
- JWT Bearer authentication via EventStore pipeline + domain RBAC in aggregate Handle methods (two authorization layers)
- DAPR component YAML files in `dapr/components/` directory (statestore.yaml, pubsub.yaml, actors.yaml)
- `system` tenant is a deployment prerequisite -- must be pre-configured in EventStore's domain service registration and identity provider JWT claims
- Low-frequency operation assumption: single-actor-per-tenant serializes all operations; designed for administrative-frequency operations (a few per day per tenant), not high-throughput bulk scenarios
- Tenant configuration boundary: designed for low-frequency administrative settings, NOT real-time feature flags or high-frequency updates
- Aggregates must be testable as pure functions (Tier 1) -- Handle/Apply as static pure functions with no DAPR, no actors, no infrastructure
- Conformance test pattern: mandatory test in Testing.Tests proving testing fakes produce identical event sequences as real aggregates for every command type. Reflection-driven auto-discovery of commands. Release blocker if fails
- Event serialization round-trip test: mandatory in Contracts.Tests. Serialize/deserialize every event type, assert deep equality. Post-v1.0: golden JSON fixtures
- Cross-tenant isolation test pattern with tier mapping: Tier 1 (Handle method rejection), Tier 2 (JWT authorization pipeline), Tier 2/3 (API-level cross-tenant requests)
- Snapshot performance test: dedicated category, nightly CI schedule, seed 500K events, assert < 30s rehydration
- Bootstrap multi-instance behavior: N-1 instances receive rejection on startup -- log at Information level, not Warning/Error
- Query consistency model: eventual consistency with read-after-write mitigation (command response includes aggregate ID, client navigates directly)
- All events must include `TenantId` as a top-level field identifying the managed tenant (envelope `tenantId` = `system`, so payload must carry managed tenant ID)
- RFC 7807 Problem Details for API error responses

### FR Coverage Map

FR1: Epic 2 - Create tenant with unique identifier and name
FR2: Epic 2 - Update tenant metadata
FR3: Epic 2 - Disable tenant
FR4: Epic 2 - Re-enable disabled tenant
FR5: Epic 2 - Domain events for tenant lifecycle changes
FR6: Epic 3 - Add user to tenant with role
FR7: Epic 3 - Remove user from tenant
FR8: Epic 3 - Change user role within tenant
FR9: Epic 3 - Reject duplicate user addition
FR10: Epic 3 - Reject role escalation violations
FR11: Epic 3 - Domain events for user-role changes
FR12: Epic 3 - Optimistic concurrency enforcement
FR13: Epic 2 - Designate global administrator
FR14: Epic 2 - Remove global administrator status
FR15: Epic 2 - Global admin cross-tenant operations
FR16: Epic 2 - Auditable global admin events
FR17: Epic 2 - Bootstrap mechanism for initial global admin
FR18: Epic 2 - Bootstrap rejected when global admin exists
FR19: Epic 3 - Set key-value configuration entry
FR20: Epic 3 - Remove configuration entry
FR21: Epic 3 - Dot-delimited namespace conventions
FR22: Epic 3 - Domain events for configuration changes
FR23: Epic 3 - Configuration limits enforcement
FR24: Epic 3 - Reject operations exceeding limits
FR25: Epic 5 - Paginated tenant list query
FR26: Epic 5 - Specific tenant detail query
FR27: Epic 5 - Tenant users list query
FR28: Epic 5 - User tenants list query
FR29: Epic 5 - Audit queries by tenant and date range
FR30: Epic 5 - Cursor-based pagination
FR31: Epic 3 - TenantReader query-only behavior
FR32: Epic 3 - TenantContributor domain command capability
FR33: Epic 3 - TenantOwner user-role and config management
FR34: Epic 3 - Cross-tenant role isolation
FR35: Epic 2 - DAPR pub/sub CloudEvents 1.0 publishing
FR36: Epic 2 - Documented topic naming convention
FR37: Epic 4 - Consuming service event subscription and local projection
FR38: Epic 4 - React to user addition/removal events
FR39: Epic 4 - React to tenant disable/enable events
FR40: Epic 4 - React to configuration change events
FR41: Epic 4 - Event contracts for idempotent handling
FR42: Epic 4 - Idempotent event processing documentation
FR43: Epic 1 - NuGet package distribution
FR44: Epic 4 - Single extension method DI registration
FR45: Epic 4 - Event handler registration < 20 lines
FR46: Epic 6 - In-memory fakes without infrastructure
FR47: Epic 6 - Testing fakes use same domain logic
FR48: Epic 7 - .NET Aspire hosting extensions
FR49: Epic 2 - Actionable error messages for command rejections
FR50: Epic 2 - Reject commands for non-existent tenant
FR51: Epic 2 - Reject commands for disabled tenant
FR52: Epic 2 - Reject duplicate operations
FR53: Epic 2 - Commands succeed independently of pub/sub
FR54: Epic 7 - Command latency metrics via OpenTelemetry
FR55: Epic 7 - Event processing metrics via OpenTelemetry
FR56: Epic 7 - Deploy alongside EventStore with DAPR
FR57: Epic 7 - Stateless service with event store reconstruction
FR58: Epic 1 - CI/CD quality gates
FR59: Epic 8 - Quickstart guide < 30 minutes
FR60: Epic 8 - Prerequisite validation in quickstart
FR61: Epic 8 - Event contract reference documentation
FR62: Epic 4 - Sample consuming service
FR63: Epic 8 - "Aha moment" demo
FR64: Epic 8 - Cross-aggregate timing documentation
FR65: Epic 8 - Compensating command patterns documentation

## Epic List

### Epic 1: Project Foundation & Solution Scaffolding

A developer can clone the repo, build the solution, and run tests with the full project infrastructure in place -- including DAPR component configuration and ServiceDefaults skeleton.
**FRs covered:** FR43, FR58
**Additional:** Architecture starter template, CI/CD pipeline, DAPR component YAML, ServiceDefaults skeleton

### Epic 2: Core Tenant Management & Global Administration

A global administrator can bootstrap the system, create tenants, and manage their lifecycle (create, update, disable, enable). Tenant events are published via DAPR pub/sub.
**FRs covered:** FR1-FR5, FR13-FR18, FR35-FR36, FR49-FR53
**NFRs addressed:** NFR5, NFR7-NFR8, NFR14-NFR16, NFR19-NFR21

### Epic 3: Tenant Membership, Roles & Configuration

A tenant owner can manage who has access to their tenant (add, remove, change roles) and configure tenant-specific settings -- with full invariant enforcement and event production.
**FRs covered:** FR6-FR12, FR19-FR24, FR31-FR34
**NFRs addressed:** NFR6, NFR10

### Epic 4: Event-Driven Integration & Consuming Service Support

A consuming service can subscribe to tenant events, build local projections, and reactively enforce access -- proven by a sample consuming service and client DI registration.
**FRs covered:** FR37-FR42, FR44-FR45, FR62

### Epic 5: Tenant Discovery & Query

Developers and administrators can query tenants, list users, look up memberships, and run audit reports through read model projections and query endpoints.
**FRs covered:** FR25-FR30
**NFRs addressed:** NFR2

### Epic 6: Testing Package

A developer can write tenant integration tests using in-memory fakes with production-parity domain logic, with no external infrastructure needed.
**FRs covered:** FR46-FR47
**NFRs addressed:** NFR4

### Epic 7: Deployment & Observability

A platform engineer can deploy the tenant service with .NET Aspire, monitor it with OpenTelemetry metrics, and operate it at scale with stateless horizontal scaling.
**FRs covered:** FR48, FR54-FR57
**NFRs addressed:** NFR1, NFR3, NFR11-NFR13, NFR17, NFR22-NFR23

### Epic 8: Documentation & Adoption

A developer can follow the quickstart to their first tenant command in < 30 minutes, reference event contracts, understand timing behavior, and see the "aha moment" demo.
**FRs covered:** FR59-FR61, FR63-FR65
**NFRs addressed:** NFR24

## Epic 1: Project Foundation & Solution Scaffolding

A developer can clone the repo, build the solution, and run tests with the full project infrastructure in place -- including DAPR component configuration and ServiceDefaults skeleton.

### Story 1.1: Solution Structure & Build Configuration

As a developer,
I want to clone the Hexalith.Tenants repository and have a fully buildable solution with all project shells and correct dependency chains,
So that I can begin implementing domain logic on a proven, consistent project structure mirroring EventStore conventions.

**Acceptance Criteria:**

**Given** the repository is cloned with the EventStore submodule initialized
**When** the developer opens the solution
**Then** `Hexalith.Tenants.slnx` contains all 15 projects (8 src, 5 test, 2 sample)

**Given** the solution structure exists
**When** `dotnet build` is executed
**Then** all projects compile successfully with zero errors and warnings-as-errors enabled

**Given** the solution structure exists
**When** `dotnet test` is executed
**Then** the test runner discovers all 6 test projects (5 under `tests/` + `samples/Hexalith.Tenants.Sample.Tests`) and reports zero failures

**Given** the solution is built
**When** a developer inspects `global.json`
**Then** it specifies the latest supported .NET 10 SDK, currently 10.0.300, with `rollForward: latestPatch`

**Given** the solution is built
**When** a developer inspects `Directory.Build.props`
**Then** it contains shared project properties including NuGet metadata, nullable references enabled, implicit usings enabled, and warnings as errors

**Given** the solution is built
**When** a developer inspects `Directory.Packages.props`
**Then** it contains centralized NuGet package versions for all dependencies without inline `Version=` attributes

**Given** the solution is built
**When** a developer inspects `.editorconfig`
**Then** it enforces current Hexalith.Tenants conventions (file-scoped namespaces, K&R brace style where applicable, `_camelCase` private fields, 4-space indentation, warnings as errors)

**Given** the solution is built
**When** a developer inspects project dependencies
**Then** Contracts depends on EventStore.Contracts; Client depends on Contracts; Server depends on Contracts and EventStore.Server; Testing depends on Server and Contracts; CommandApi depends on Server, Contracts, and ServiceDefaults; Aspire depends on Contracts and Client; test projects reference their corresponding src projects plus xUnit, Shouldly, NSubstitute, and coverlet

### Story 1.2: DAPR Component Configuration & ServiceDefaults

As a developer,
I want DAPR component YAML files and a ServiceDefaults project with OpenTelemetry skeleton in place,
So that local development with DAPR sidecars and observability is ready for domain service implementation.

**Acceptance Criteria:**

**Given** the solution from Story 1.1 exists
**When** a developer inspects `dapr/components/`
**Then** `statestore.yaml` configures the `tenants-eventstore` state store component

**Given** the solution from Story 1.1 exists
**When** a developer inspects `dapr/components/`
**Then** `pubsub.yaml` configures the pub/sub component for tenant events

**Given** the solution from Story 1.1 exists
**When** a developer inspects `dapr/components/`
**Then** `actors.yaml` configures TenantAggregate and GlobalAdministratorsAggregate actor types

**Given** the ServiceDefaults project exists
**When** a developer inspects `Extensions.cs`
**Then** it contains OpenTelemetry configuration with tracing and metrics setup following EventStore's ServiceDefaults pattern

**Given** the ServiceDefaults project exists
**When** `dotnet build` is executed
**Then** the ServiceDefaults project compiles successfully and is referenced by Hexalith.Tenants

### Story 1.3: CI/CD Pipeline

As a developer,
I want GitHub Actions workflows for continuous integration and release publishing,
So that every PR is validated automatically and semantic-release publishes NuGet packages after qualifying merges to `main`.

**Acceptance Criteria:**

**Given** a developer pushes a commit or opens a PR to main
**When** the CI workflow (`ci.yml`) triggers
**Then** it executes: restore, build (Release configuration), and runs Tier 1+2 tests

**Given** the CI workflow runs
**When** all tests pass
**Then** the workflow reports success and code coverage is collected via coverlet

**Given** a developer merges a PR with Conventional Commit messages to `main`
**When** the release workflow (`release.yml`) triggers via semantic-release
**Then** it determines the next SemVer version from commit history, runs the full test suite, builds and packs all 5 NuGet packages (Contracts, Client, Server, Testing, Aspire), publishes to NuGet.org, creates a GitHub Release with assets, and commits an updated CHANGELOG.md

**Given** the release workflow runs
**When** no releasable commits are found (e.g., only `chore:` or `docs:` commits)
**Then** semantic-release skips the release and the workflow succeeds without publishing

## Epic 2: Core Tenant Management & Global Administration

A global administrator can bootstrap the system, create tenants, and manage their lifecycle (create, update, disable, enable). Tenant events are published via DAPR pub/sub.

### Story 2.1: Tenant Domain Contracts

As a developer,
I want all tenant commands, events, enums, and identity types defined in the Contracts package,
So that consuming services and all other packages have a stable, shared API surface to reference.

**Acceptance Criteria:**

**Given** the Contracts project exists
**When** a developer inspects the Commands folder
**Then** it contains all 12 command records: CreateTenant, UpdateTenant, DisableTenant, EnableTenant, AddUserToTenant, RemoveUserFromTenant, ChangeUserRole, SetTenantConfiguration, RemoveTenantConfiguration, BootstrapGlobalAdmin, SetGlobalAdministrator, RemoveGlobalAdministrator

**Given** the Contracts project exists
**When** a developer inspects the Events folder
**Then** it contains all 11 event records: TenantCreated, TenantUpdated, TenantDisabled, TenantEnabled, UserAddedToTenant, UserRemovedFromTenant, UserRoleChanged, TenantConfigurationSet, TenantConfigurationRemoved, GlobalAdministratorSet, GlobalAdministratorRemoved

**Scaffolding exception:** Story 2.1 includes the initial public contract set for known Epic 2 and Epic 3 behavior because this pre-1.0 project already relies on serialization, naming, and conformance tests to prevent contract drift. Future stories should prefer vertical contract creation by first behavioral use unless the Product Owner explicitly approves another scaffolding exception.

**Given** the Contracts project exists
**When** a developer inspects the Enums folder
**Then** it contains TenantRole (TenantOwner, TenantContributor, TenantReader) and TenantStatus (Active, Disabled)

**Given** the Contracts project exists
**When** a developer inspects the Identity folder
**Then** it contains TenantIdentity with identity scheme helpers mapping to `system:tenants:{aggregateId}`

**Given** all event types exist
**When** each event is serialized to JSON via System.Text.Json and deserialized back
**Then** deep equality holds for all fields (serialization round-trip test in Contracts.Tests)

**Given** all command and event types exist
**When** a reflection-based test scans the Contracts assembly
**Then** all commands follow `{Verb}{Target}` naming and all events follow `{Target}{PastVerb}` naming

**Given** all event types exist
**When** a developer inspects any event record
**Then** every event includes `TenantId` as a top-level field identifying the managed tenant

### Story 2.2: Global Administrator Aggregate

As a global administrator,
I want to bootstrap the first global admin on initial deployment and manage global administrator designations,
So that the system has authorized actors who can create and manage tenants.

**Acceptance Criteria:**

**Given** no global administrators exist in the event store
**When** a BootstrapGlobalAdmin command is processed with a valid user ID
**Then** a GlobalAdministratorSet event is produced with the specified user ID

**Given** a global administrator already exists
**When** a BootstrapGlobalAdmin command is processed
**Then** the command is rejected with GlobalAdminAlreadyBootstrappedRejection

**Given** an existing global administrator
**When** a SetGlobalAdministrator command is processed with a new user ID
**Then** a GlobalAdministratorSet event is produced

**Given** an existing global administrator
**When** a RemoveGlobalAdministrator command is processed for a designated admin
**Then** a GlobalAdministratorRemoved event is produced

**Given** only one global administrator exists
**When** a RemoveGlobalAdministrator command attempts to remove the last global administrator
**Then** the command is rejected with a specific error indicating the last admin cannot be removed

**Given** the GlobalAdministratorsAggregate Handle methods
**When** tested as static pure functions with no infrastructure
**Then** all Handle and Apply methods execute correctly as Tier 1 unit tests

**Given** the GlobalAdministratorsState class
**When** Apply methods are called with each event type
**Then** state is correctly mutated (administrators set added/removed)

**Implementation Blueprint (Research-Validated 2026-03-15):**

State class — `Server/Aggregates/GlobalAdministratorsState.cs`:

```csharp
public sealed class GlobalAdministratorsState
{
    public HashSet<string> Administrators { get; private set; } = new();
    public bool Bootstrapped { get; private set; }

    public void Apply(GlobalAdministratorSet e) { Administrators.Add(e.UserId); Bootstrapped = true; }
    public void Apply(GlobalAdministratorRemoved e) { Administrators.Remove(e.UserId); }
}
```

Aggregate class — `Server/Aggregates/GlobalAdministratorsAggregate.cs`:

- Extends `EventStoreAggregate<GlobalAdministratorsState>` (reflection-based Handle/Apply discovery)
- 3 Handle methods: `Handle(BootstrapGlobalAdmin, state?)`, `Handle(SetGlobalAdministrator, state?)`, `Handle(RemoveGlobalAdministrator, state?)`
- All `public static` pure functions returning `DomainResult`
- BootstrapGlobalAdmin rejects if `state?.Bootstrapped == true` → reuses `GlobalAdministratorSet` event (same as SetGlobalAdministrator)
- SetGlobalAdministrator is idempotent: if user already in set → `DomainResult.NoOp()`
- RemoveGlobalAdministrator: if user not in set → `DomainResult.NoOp()`; if last admin → rejection
- Last-admin protection: `state.Administrators.Count == 1 && state.Administrators.Contains(cmd.UserId)` → reject

Testing pattern — uses `aggregate.ProcessAsync(commandEnvelope, state)` with `CommandEnvelope` construction helper (see Architecture §D10 Testing Blueprint). All tests are Tier 1 unit — no DAPR, no actors, no mocking.

### Story 2.3: Tenant Aggregate Lifecycle

As a global administrator,
I want to create, update, disable, and enable tenants,
So that I can manage the tenant lifecycle for all consuming services.

**Acceptance Criteria:**

**Given** no tenant exists with the specified ID
**When** a CreateTenant command is processed with a valid tenant ID and name
**Then** a TenantCreated event is produced with TenantId, Name, Description, and CreatedAt

**Given** a tenant already exists with the specified ID
**When** a CreateTenant command is processed with the same ID
**Then** the command is rejected with TenantAlreadyExistsRejection

**Given** an active tenant exists
**When** an UpdateTenant command is processed with new name and description
**Then** a TenantUpdated event is produced with the updated metadata

**Given** an active tenant exists
**When** a DisableTenant command is processed
**Then** a TenantDisabled event is produced and the tenant status becomes Disabled

**Given** a disabled tenant exists
**When** any command targeting that tenant is processed (except EnableTenant)
**Then** the command is rejected with TenantDisabledRejection indicating the tenant's disabled status

**Given** a disabled tenant exists
**When** an EnableTenant command is processed
**Then** a TenantEnabled event is produced and the tenant status becomes Active

**Given** a CreateTenant command is submitted
**When** FluentValidation runs in the MediatR pipeline
**Then** the command is validated for required fields (TenantId non-empty, Name non-empty) and rejected with 400 Bad Request if invalid

**Given** commands targeting a non-existent tenant (Update, Disable, Enable)
**When** processed against null state
**Then** the command is rejected with TenantNotFoundRejection identifying the missing tenant

**Given** the TenantAggregate Handle methods
**When** tested as static pure functions with no infrastructure
**Then** all Handle and Apply methods for lifecycle commands execute correctly as Tier 1 unit tests with 100% branch coverage on validation logic

**Implementation Blueprint (Research-Validated 2026-03-15):**

State class — `Server/Aggregates/TenantState.cs`:

```csharp
public sealed class TenantState
{
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TenantStatus Status { get; private set; }
    public Dictionary<string, TenantRole> Users { get; private set; } = new();
    public Dictionary<string, string> Configuration { get; private set; } = new();
    public DateTimeOffset CreatedAt { get; private set; }

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

Aggregate class — `Server/Aggregates/TenantAggregate.cs`:

- Extends `EventStoreAggregate<TenantState>` (reflection-based Handle/Apply discovery)
- 4 lifecycle Handle methods in this story: `Handle(CreateTenant, state?)`, `Handle(UpdateTenant, state?)`, `Handle(DisableTenant, state?)`, `Handle(EnableTenant, state?)`
- All `public static` pure functions returning `DomainResult`
- CreateTenant: `state is not null` → `TenantAlreadyExistsRejection`; else → `TenantCreated`
- UpdateTenant: `state is null` → `TenantNotFoundRejection`; else → `TenantUpdated` (full-replacement semantics)
- DisableTenant: `state is null` → `TenantNotFoundRejection`; `state.Status == Disabled` → `NoOp()`; else → `TenantDisabled`
- EnableTenant: `state is null` → `TenantNotFoundRejection`; `state.Status == Active` → `NoOp()`; else → `TenantEnabled`
- Scaffolding exception: TenantState includes Users/Configuration Apply methods because related event contracts already exist and projection/testing conformance depends on stable replay behavior. This does not mean membership/configuration command behavior is complete; that behavior remains owned by Epic 3 Stories 3.1 and 3.3. Future stories should avoid adding future-facing Apply methods before their behavior slice unless explicitly approved.

Testing pattern — same as Story 2.2: `aggregate.ProcessAsync(commandEnvelope, state)` with `CommandEnvelope` helper (see Architecture §D10 Testing Blueprint). All tests Tier 1.

### Story 2.4: Tenant Service, Bootstrap & Event Publishing

> **Post-readiness correction (2026-05-16):** Story 2.4 is already complete and remains the historical implementation story, but it is too broad to use as a future sprint-slicing model. For evidence review, maintenance, and any future rework, treat it as five logical work packages:
>
> - **2.4A Command API and EventStore processing endpoint wiring:** REST command endpoint, MediatR pipeline, aggregate auto-discovery, and `/process` domain dispatch.
> - **2.4B Bootstrap hosted service and multi-instance idempotency:** startup configuration, first global administrator creation, and information-level already-bootstrapped handling.
> - **2.4C DAPR pub/sub publication and recovery behavior:** CloudEvents publication, pub/sub unavailable behavior, source-of-truth persistence, and catch-up/drain expectations.
> - **2.4D API error and authentication response mapping:** RFC 7807 rejection mapping, correlation ID propagation, and 401 handling for unauthenticated requests.
> - **2.4E Tier 2 command pipeline verification:** DAPR slim-init integration coverage for command processing, bootstrap, event publication, and `/process` dispatch.
>
> Future stories with this breadth must be split before sprint execution unless the Product Owner explicitly accepts a single-story integration spike.

As a platform operator,
I want a deployable REST API that accepts tenant commands, bootstraps the global admin on startup, and publishes domain events via DAPR pub/sub,
So that the tenant service is operational end-to-end from command to event distribution.

**Acceptance Criteria:**

**Given** Hexalith.Tenants is deployed with DAPR sidecar
**When** a valid command is sent to `POST /api/commands`
**Then** the command is processed through the MediatR pipeline (validation, authorization, aggregate Handle) and a success response is returned

**Given** Hexalith.Tenants starts with `Tenants:BootstrapGlobalAdminUserId` configured in appsettings.json
**When** no global administrators exist in the event store
**Then** TenantBootstrapHostedService sends a BootstrapGlobalAdmin command through MediatR and the initial global admin is created

**Given** Hexalith.Tenants starts on a multi-instance deployment where bootstrap has already completed
**When** TenantBootstrapHostedService sends the BootstrapGlobalAdmin command
**Then** the rejection is logged at Information level with "Global administrator already bootstrapped, skipping"

**Given** a command is successfully processed by an aggregate
**When** domain events are produced
**Then** events are published to DAPR pub/sub topic `tenants.events` as CloudEvents 1.0

**Given** DAPR pub/sub is temporarily unavailable
**When** a command is processed
**Then** the command succeeds and events are stored in the event store (source of truth); subscribers catch up when pub/sub recovers

**Given** a command is rejected by domain validation
**When** the error response is returned
**Then** it follows RFC 7807 Problem Details format with type, title, detail, status, and correlationId fields

**Given** Hexalith.Tenants is deployed with JWT authentication
**When** a request arrives without valid JWT credentials
**Then** the request is rejected with 401 Unauthorized

**Given** Hexalith.Tenants registers domain services via `AddEventStore()`
**When** the application starts
**Then** `TenantAggregate` and `GlobalAdministratorsAggregate` are auto-discovered via assembly scanning and registered as domain processors

**Given** the AggregateActor invokes domain processing via DAPR service-to-service call
**When** a command reaches Step 4 of the actor pipeline
**Then** Hexalith.Tenants' `/process` endpoint receives the `DomainServiceRequest`, dispatches to `IDomainProcessor.ProcessAsync()`, and returns `DomainServiceWireResult` with events or rejections

**Given** the full command pipeline is operational
**When** Tier 2 integration tests run with DAPR slim init
**Then** CreateTenant, DisableTenant, EnableTenant, and BootstrapGlobalAdmin commands succeed end-to-end with events published

**Implementation Blueprint (Research-Validated 2026-03-15):**

Hexalith.Tenants `Program.cs` — DI registration and middleware:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddEventStore();  // Auto-discovers TenantAggregate, GlobalAdministratorsAggregate
var app = builder.Build();
app.UseEventStore();               // Resolves 5-layer cascade configuration
app.Run();
```

Key wiring details:

- `AddEventStore()` triggers `AssemblyScanner` which discovers all `EventStoreAggregate<T>` subclasses and `EventStoreProjection<T>` subclasses in referenced assemblies
- `UseEventStore()` resolves the 5-layer cascade configuration: conventions → global options → self-config → appsettings → explicit overrides
- The `/process` endpoint is registered automatically by `UseEventStore()` — it maps to `IDomainProcessor.ProcessAsync()` which dispatches to the discovered aggregate's Handle method via reflection
- `IDomainServiceResolver` maps aggregate types to the Hexalith.Tenants' DAPR AppId for service-to-service invocation
- `TenantBootstrapHostedService`: reads `Tenants:BootstrapGlobalAdminUserId` from configuration, sends `BootstrapGlobalAdmin` through MediatR on startup. Logs rejection at Information level (idempotent on multi-instance)
- `RejectionToHttpStatusMapper` middleware maps `IRejectionEvent` types to HTTP status codes per architecture §Format Patterns

DAPR version alignment: `Directory.Packages.props` must keep DAPR SDK packages aligned on the approved version family, currently 1.17.9, and match the EventStore submodule before this story begins.

## Epic 3: Tenant Membership, Roles & Configuration

A tenant owner can manage who has access to their tenant (add, remove, change roles) and configure tenant-specific settings -- with full invariant enforcement and event production.

### Story 3.1: User-Role Management

As a tenant owner,
I want to add users to my tenant with a specified role, remove users, and change their roles,
So that I can control who has access to my tenant and what they can do.

**Acceptance Criteria:**

**Given** an active tenant exists and the requesting user is a TenantOwner or GlobalAdministrator
**When** an AddUserToTenant command is processed with a valid user ID and role (TenantOwner, TenantContributor, or TenantReader)
**Then** a UserAddedToTenant event is produced with TenantId, UserId, and Role

**Given** a user is already a member of the tenant
**When** an AddUserToTenant command is processed for the same user
**Then** the command is rejected with UserAlreadyInTenantRejection including the existing role information

**Given** a user is a member of the tenant
**When** a RemoveUserFromTenant command is processed
**Then** a UserRemovedFromTenant event is produced with TenantId and UserId

**Given** a user is not a member of the tenant
**When** a RemoveUserFromTenant command is processed for that user
**Then** the command is rejected with UserNotInTenantRejection

**Given** a user is a member of the tenant with one role
**When** a ChangeUserRole command is processed with a new valid role
**Then** a UserRoleChanged event is produced with TenantId, UserId, OldRole, and NewRole

**Given** a TenantOwner attempts to assign GlobalAdministrator role
**When** the ChangeUserRole or AddUserToTenant command is processed
**Then** the command is rejected with RoleEscalationRejection

**Given** two concurrent AddUserToTenant commands for the same user
**When** both are processed against the same aggregate version
**Then** the first succeeds and the second is rejected with a concurrency conflict error

**Given** an AddUserToTenant command is submitted
**When** FluentValidation runs in the MediatR pipeline
**Then** the command is validated for required fields (TenantId, UserId non-empty, Role is valid enum value)

**Given** the TenantAggregate Handle methods for user-role commands
**When** tested as static pure functions
**Then** all Handle and Apply methods execute correctly as Tier 1 unit tests with 100% branch coverage on escalation boundaries and duplicate detection

### Story 3.2: Role Behavior Enforcement

As a developer integrating with the tenant system,
I want role-based authorization enforced at the domain level so that TenantReader, TenantContributor, and TenantOwner permissions are consistently applied,
So that tenant security boundaries are guaranteed by the aggregate regardless of the calling context.

**Acceptance Criteria:**

**Given** a user with TenantReader role in a tenant
**When** a state-changing command (AddUserToTenant, ChangeUserRole, RemoveUserFromTenant, SetTenantConfiguration) is processed with that user as the actor
**Then** the command is rejected indicating insufficient permissions

**Given** a user with TenantContributor role in a tenant
**When** a user-role management command (AddUserToTenant, ChangeUserRole, RemoveUserFromTenant) or configuration command is processed with that user as the actor
**Then** the command is rejected indicating insufficient permissions (Contributor cannot manage users or config)

**Given** a user with TenantOwner role in a tenant
**When** a user-role management or configuration command is processed with that user as the actor
**Then** the command succeeds (Owner has full tenant management capabilities)

**Given** a user with roles in multiple tenants (e.g., Owner in Tenant A, Reader in Tenant B)
**When** a state-changing command targeting Tenant B is processed with that user as the actor
**Then** the command is rejected based on the user's role in Tenant B, not Tenant A -- roles do not transfer across tenants

**Given** a GlobalAdministrator
**When** any tenant command is processed with that user as the actor
**Then** the command succeeds regardless of per-tenant role assignment

**Given** all role behavior enforcement paths
**When** tested as Tier 1 unit tests
**Then** 100% branch coverage is achieved on role authorization logic in Handle methods

### Story 3.3: Tenant Configuration Management

As a tenant owner,
I want to set and remove key-value configuration entries for my tenant using namespaced keys,
So that consuming services can react to per-tenant settings like billing plans or feature flags.

**Acceptance Criteria:**

**Given** an active tenant exists and the requesting user is a TenantOwner or GlobalAdministrator
**When** a SetTenantConfiguration command is processed with a key and value
**Then** a TenantConfigurationSet event is produced with TenantId, Key, and Value

**Given** a configuration entry exists for a tenant
**When** a RemoveTenantConfiguration command is processed with the matching key
**Then** a TenantConfigurationRemoved event is produced with TenantId and Key

**Given** a configuration key uses dot-delimited namespace convention (e.g., `billing.plan`, `parties.maxContacts`)
**When** the SetTenantConfiguration command is processed
**Then** the key is accepted and stored preserving the namespace structure

**Given** a tenant already has 100 configuration keys
**When** a SetTenantConfiguration command attempts to add a 101st key
**Then** the command is rejected with ConfigurationLimitExceededRejection identifying the key count limit (100) and current usage

**Given** a SetTenantConfiguration command with a value exceeding 1KB
**When** the command is processed
**Then** the command is rejected with ConfigurationLimitExceededRejection identifying the value size limit (1KB)

**Given** a SetTenantConfiguration command with a key exceeding 256 characters
**When** the command is processed
**Then** the command is rejected with ConfigurationLimitExceededRejection identifying the key length limit (256)

**Given** a SetTenantConfiguration command is submitted
**When** FluentValidation runs in the MediatR pipeline
**Then** the command is validated for required fields (TenantId, Key non-empty) and structural constraints

**Given** the TenantAggregate Handle methods for configuration commands
**When** tested as static pure functions
**Then** all Handle and Apply methods execute correctly as Tier 1 unit tests with 100% branch coverage on limit enforcement logic

## Epic 4: Event-Driven Integration & Consuming Service Support

A consuming service can subscribe to tenant events, build local projections, and reactively enforce access -- proven by a sample consuming service and client DI registration.

### Story 4.1: Client DI Registration

As a developer building a consuming service,
I want to register tenant client services in my DI container with a single extension method call,
So that my service is wired up for tenant event handling with minimal configuration.

**Acceptance Criteria:**

**Given** a consuming service references the Hexalith.Tenants.Client NuGet package
**When** the developer calls `services.AddHexalithTenants()` in their DI configuration
**Then** all required tenant client services (event handlers, abstractions) are registered in the service collection

**Given** a consuming service references the Hexalith.Tenants.Contracts and Client packages
**When** the developer registers tenant event handlers
**Then** the total DI configuration is under 20 lines of code

**Given** the Client DI extension method is called
**When** the service collection is inspected
**Then** all expected service registrations are present with correct lifetimes

**Given** the Client package
**When** Tier 1 unit tests in Client.Tests are executed
**Then** DI registration tests verify all services are registered correctly and resolve without errors

### Story 4.2: Event Subscription & Local Projection Pattern

As a developer building a consuming service,
I want to subscribe to tenant events via DAPR pub/sub and build a local projection of tenant state,
So that my service can reactively enforce access and respond to tenant changes.

**Acceptance Criteria:**

**Given** a consuming service is subscribed to the `tenants.events` DAPR pub/sub topic
**When** a UserAddedToTenant event is published
**Then** the consuming service receives the event and can update its local projection of tenant membership

**Given** a consuming service is subscribed to tenant events
**When** a UserRemovedFromTenant event is published
**Then** the consuming service can revoke access for the removed user in its local projection

**Given** a consuming service is subscribed to tenant events
**When** a TenantDisabled event is published
**Then** the consuming service can block operations for the disabled tenant

**Given** a consuming service is subscribed to tenant events
**When** a TenantConfigurationSet event is published
**Then** the consuming service can update tenant-specific behavior based on the configuration change

**Given** event contracts include event ID and aggregate version (FR41)
**When** a consuming service receives a duplicate event (DAPR at-least-once delivery)
**Then** the service can detect the duplicate via event ID and skip reprocessing

**Given** a consuming service builds a local projection from tenant events
**When** multiple events arrive for different tenants
**Then** each tenant's projection is maintained independently with no cross-tenant data leakage

### Story 4.3: Sample Consuming Service & Idempotent Processing Guide

As a developer evaluating Hexalith.Tenants,
I want a complete sample consuming service and documentation on idempotent event processing,
So that I have a proven reference implementation to follow when integrating tenant events into my own services.

**Acceptance Criteria:**

**Given** the `samples/Hexalith.Tenants.Sample` project exists
**When** a developer inspects the sample
**Then** it demonstrates: DI registration via `AddHexalithTenants()`, DAPR pub/sub event subscription, a local projection of tenant-user-role state, and access enforcement based on the projection

**Given** the sample consuming service is running with DAPR sidecar
**When** a UserAddedToTenant event is published by the tenant service
**Then** the sample service logs the event and updates its local projection

**Given** the sample consuming service is running
**When** a UserRemovedFromTenant event is published
**Then** the sample service revokes access and logs the revocation

**Given** the sample project
**When** `samples/Hexalith.Tenants.Sample.Tests` are executed
**Then** Tier 1 tests verify the sample's event handling and projection logic

**Given** the project documentation
**When** a developer reads the idempotent event processing guidance (FR42)
**Then** it includes: at-least-once delivery explanation, deduplication by event ID example, idempotent handler pattern with code sample

## Epic 5: Tenant Discovery & Query

Developers and administrators can query tenants, list users, look up memberships, and run audit reports through read model projections and query endpoints.

### Story 5.1: Per-Tenant & Global Admin Projections

As a developer,
I want per-tenant read model projections and a global administrator projection maintained automatically from domain events,
So that query endpoints have up-to-date data for tenant details, user lists, and admin lookups.

**Acceptance Criteria:**

**Given** a TenantCreated event is published
**When** the TenantProjection processes the event
**Then** a TenantReadModel is created in the DAPR state store with the tenant's ID, name, description, status, empty members list, and empty configuration

**Given** UserAddedToTenant, UserRemovedFromTenant, and UserRoleChanged events are published
**When** the TenantProjection processes these events
**Then** the TenantReadModel's members dictionary is updated accordingly

**Given** TenantConfigurationSet and TenantConfigurationRemoved events are published
**When** the TenantProjection processes these events
**Then** the TenantReadModel's configuration dictionary is updated accordingly

**Given** TenantDisabled and TenantEnabled events are published
**When** the TenantProjection processes these events
**Then** the TenantReadModel's status is updated to Disabled or Active

**Given** GlobalAdministratorSet and GlobalAdministratorRemoved events are published
**When** the GlobalAdministratorProjection processes these events
**Then** the GlobalAdministratorReadModel is updated with the current set of global administrator user IDs

**Given** both projection classes exist in the Server project
**When** the application starts
**Then** projections are auto-discovered via EventStore's assembly scanning and registered for event processing

### Story 5.2: Cross-Tenant Index Projection

As a developer,
I want a cross-tenant index projection that aggregates data across all tenants,
So that ListTenants and GetUserTenants queries can be served efficiently at scale.

**Acceptance Criteria:**

**Given** a TenantCreated event is published
**When** the TenantIndexProjection processes the event
**Then** the tenant is added to the cross-tenant index stored under a well-known key in the DAPR state store

**Given** a TenantDisabled or TenantEnabled event is published
**When** the TenantIndexProjection processes the event
**Then** the tenant's status is updated in the cross-tenant index

**Given** UserAddedToTenant or UserRemovedFromTenant events are published
**When** the TenantIndexProjection processes these events
**Then** the user-to-tenant mapping index is updated

**Given** two concurrent events trigger simultaneous updates to the cross-tenant index key
**When** the projection performs a read-modify-write on the shared state key
**Then** ETag-based optimistic concurrency (`ConcurrencyMode.FirstWrite`) detects the conflict and retries (max 3 attempts) from step 1 (GET state with ETag)

**Given** the cross-tenant index is populated with 1,000 tenants
**When** the index is queried
**Then** it returns results within NFR2 latency targets (50ms p95 per page)

### Story 5.3: Query Endpoints & Authorization

As a developer or administrator,
I want REST query endpoints to list tenants, view tenant details, look up user memberships, and run audit queries,
So that I can discover tenants, manage access, and produce compliance reports.

**Acceptance Criteria:**

**Given** an authenticated user with a role in at least one tenant
**When** a GET request is sent to `/api/tenants`
**Then** a paginated list of tenants is returned with IDs, names, and statuses using cursor-based pagination (`{ "items": [...], "cursor": "...", "hasMore": true }`)

**Given** an authenticated user with a role in the target tenant (or GlobalAdmin)
**When** a GET request is sent to `/api/tenants/{tenantId}`
**Then** the tenant's full details are returned including current users and their roles

**Given** an authenticated user with a role in the target tenant (or GlobalAdmin)
**When** a GET request is sent to `/api/tenants/{tenantId}/users`
**Then** a paginated list of users in that tenant with their assigned roles is returned

**Given** an authenticated user
**When** a GET request is sent to `/api/users/{userId}/tenants`
**Then** a paginated list of tenants the specified user belongs to is returned with their role in each tenant

**Given** an authenticated GlobalAdministrator
**When** a GET request is sent to `/api/tenants/{tenantId}/audit` with date range parameters
**Then** tenant access change events are returned with pagination support (default 100, max 1,000 results per page)

**Given** an authenticated user without a role in the target tenant and not a GlobalAdmin
**When** a GET request is sent to `/api/tenants/{tenantId}` or `/api/tenants/{tenantId}/users`
**Then** the request is rejected with 403 Forbidden

**Given** all query endpoints
**When** cursor-based pagination parameters are provided
**Then** results are returned with consistent ordering and valid cursor tokens for next-page navigation

**Given** a command has just been processed (e.g., CreateTenant)
**When** the command response is returned
**Then** the response includes the aggregate ID so the client can navigate directly to `GET /api/tenants/{id}` for read-after-write confirmation

## Epic 6: Testing Package

A developer can write tenant integration tests using in-memory fakes with production-parity domain logic, with no external infrastructure needed.

### Story 6.1: In-Memory Tenant Service & Test Helpers

As a developer,
I want an in-memory fake tenant service and test helpers that execute the same domain logic as production,
So that I can write tenant integration tests in under 10 lines without external infrastructure.

**Acceptance Criteria:**

**Given** a test project references the Hexalith.Tenants.Testing NuGet package
**When** the developer creates an InMemoryTenantService instance
**Then** the service accepts commands (CreateTenant, AddUserToTenant, etc.) and produces the same domain events as the production TenantAggregate

**Given** the InMemoryTenantService is instantiated
**When** a CreateTenant command is processed followed by AddUserToTenant
**Then** the events are returned and state is maintained in memory with no DAPR, no actors, and no external dependencies

**Given** the InMemoryTenantService
**When** a command violates domain invariants (e.g., duplicate user, disabled tenant, role escalation)
**Then** the same rejection events are returned as in production via DomainResult.Rejection() (UserAlreadyInTenantRejection, TenantDisabledRejection, RoleEscalationRejection, etc.)

**Given** TenantTestHelpers exist in the Testing package
**When** a developer writes a tenant integration test
**Then** common setup patterns (create tenant, add user, bootstrap admin) are available as helper methods reducing test authoring to under 10 lines per test

**Given** the InMemoryTenantService processes a command
**When** execution time is measured
**Then** commands execute and produce events within 10ms (NFR4)

**Given** the InMemoryTenantService
**When** two tenants are created and users are added to each
**Then** aggregate state and produced events for tenant A never include tenant B membership or configuration data

### Story 6.2: In-Memory Projection & Conformance Tests

As a developer,
I want an in-memory projection for query testing and a conformance test suite proving production-test parity,
So that I can test query scenarios locally and trust that test behavior matches production behavior.

**Acceptance Criteria:**

**Given** the InMemoryTenantProjection exists in the Testing package
**When** events produced by InMemoryTenantService are applied to the projection
**Then** the projection maintains queryable tenant state (tenant details, user lists, configuration) in memory

**Given** the InMemoryTenantProjection
**When** a developer queries for tenants, users, or configuration in a test
**Then** results are returned from the in-memory projection without DAPR state store dependency

**Given** the InMemoryTenantProjection
**When** events for tenant A and tenant B are applied in the same test run
**Then** projected query results for tenant A never contain tenant B data, and projected query results for tenant B never contain tenant A data

**Given** the conformance test suite in Testing.Tests
**When** a reflection-based scan discovers all command types in the Contracts assembly
**Then** every command type is automatically included in the conformance test -- no manual registration required

**Given** the conformance test suite
**When** an identical command sequence is executed against the real TenantAggregate and the InMemoryTenantService
**Then** both produce identical event sequences (same event types, same field values) for every command type

**Given** the conformance test suite
**When** a new command type is added to the Contracts assembly
**Then** the reflection-based discovery automatically includes it in the next test run without any test code changes

**Given** the conformance test suite fails
**When** the CI pipeline runs
**Then** the build is marked as failed -- this is a release blocker indicating production and test execution paths have diverged

## Epic 7: Deployment & Observability

A platform engineer can deploy the tenant service with .NET Aspire, monitor it with OpenTelemetry metrics, and operate it at scale with stateless horizontal scaling.

### Story 7.1: Aspire Hosting & AppHost

As a developer,
I want .NET Aspire hosting extensions and an AppHost that orchestrates the tenant service with DAPR sidecars,
So that I can start the full local development topology with a single `dotnet run` command.

**Acceptance Criteria:**

**Given** the Hexalith.Tenants.Aspire project exists
**When** a developer inspects the package
**Then** it contains `HexalithTenantsExtensions` with extension methods for adding the tenant service to an Aspire distributed application and `HexalithTenantsResources` defining the tenant service resource

**Given** the Hexalith.Tenants.AppHost project exists
**When** `dotnet run` is executed on the AppHost
**Then** the Aspire dashboard launches with Hexalith.Tenants (AppId: tenants), EventStore server, and Keycloak, all started with DAPR sidecars configured for state store, pub/sub, and actors

**Given** the AppHost is running
**When** a developer sends a command to the tenant service via the Aspire dashboard or direct HTTP
**Then** the command is processed end-to-end through the DAPR actor pipeline

**Given** a consuming service project references the Hexalith.Tenants.Aspire package
**When** the developer adds `.AddHexalithTenants()` to their AppHost
**Then** the tenant service and its DAPR sidecar are included in the consuming service's Aspire topology

### Story 7.2: OpenTelemetry Instrumentation & Health Checks

As a platform engineer,
I want tenant command latency and event processing metrics exposed via OpenTelemetry and a health check endpoint,
So that I can monitor service performance and availability in production.

**Acceptance Criteria:**

**Given** the tenant service is deployed with OpenTelemetry configured via ServiceDefaults
**When** a tenant command is processed
**Then** an OpenTelemetry span is emitted measuring command latency with attributes for command type, tenant ID, and success/failure status

**Given** the tenant service is processing events for projections
**When** events flow through the projection pipeline
**Then** OpenTelemetry metrics are emitted for event processing duration and event count

**Given** the OpenTelemetry metrics are collected
**When** a platform engineer inspects the telemetry data
**Then** command latency (NFR1) and event publication latency (NFR3) are measurable at p95 against the 50ms target

**Given** the tenant service is deployed
**When** a GET request is sent to the health check endpoint
**Then** a 200 OK response is returned indicating the service is healthy and available for uptime monitoring (NFR22: 99.9% target)

**Given** the health check endpoint
**When** the event store is unreachable
**Then** the health check reports degraded or unhealthy status

### Story 7.3: Stateless Scaling & Snapshot Configuration

As a platform engineer,
I want the tenant service to be stateless with configurable snapshot intervals and graceful degradation,
So that I can scale horizontally, restart without data loss, and maintain operations during infrastructure partial failures.

**Acceptance Criteria:**

**Given** the tenant service is running
**When** the service is restarted
**Then** all tenant state is reconstructed from the event store -- no data loss, no migration scripts, no data seeding required (NFR12, NFR20)

**Given** the tenant service is configured with snapshot interval of 50 events for tenant domain
**When** a tenant aggregate accumulates more than 50 events
**Then** a snapshot is persisted and subsequent actor rehydration replays at most 50 events from the last snapshot

**Given** the GlobalAdministratorsAggregate uses the default snapshot interval of 100 events
**When** the aggregate is rehydrated
**Then** snapshots are created at the 100-event interval appropriate for its low event volume

**Given** DAPR pub/sub is temporarily unavailable
**When** a command is processed
**Then** the command succeeds and events are stored in the event store; when pub/sub recovers, subscribers receive all pending events (NFR17)

**Given** a Tier 3 integration test
**When** pub/sub is disabled, commands are executed, and pub/sub is re-enabled
**Then** subscribers receive all events that were stored during the outage

**Given** a snapshot performance test seeded with 500,000 events (1,000 tenants x 500 events average) with 50-event snapshot interval
**When** a cold-start actor rehydration is measured
**Then** state reconstruction completes within 30 seconds (NFR13) -- this test runs on nightly CI schedule, not on every PR

## Epic 8: Documentation & Adoption

A developer can follow the quickstart to their first tenant command in < 30 minutes, reference event contracts, understand timing behavior, and see the "aha moment" demo.

### Story 8.1: Quickstart Guide & README

As a developer evaluating Hexalith.Tenants,
I want a quickstart guide with prerequisite validation that gets me to my first tenant command within 30 minutes,
So that I can evaluate the system quickly and confidently with clear guidance at every step.

**Acceptance Criteria:**

**Given** a developer reads `docs/quickstart.md`
**When** they follow the guide from the beginning
**Then** the guide starts with a prerequisite validation section checking: DAPR sidecar is running, EventStore is deployed, `system` tenant is configured in EventStore's domain service registration, and JWT claims include `eventstore:tenant` = `system`

**Given** a prerequisite check fails
**When** the developer reads the validation output
**Then** the guide provides a specific remediation step with a link to the relevant DAPR or EventStore documentation

**Given** all prerequisites pass
**When** the developer follows the remaining steps
**Then** they can send a CreateTenant command and see the TenantCreated event within 30 minutes of starting the guide

**Given** the quickstart guide
**When** a developer inspects its content
**Then** it includes: NuGet package installation, DI configuration, DAPR component setup reference, first command execution, and verification of the produced event

**Given** the project README.md
**When** a developer visits the repository
**Then** the README includes: project description, badges (build status, NuGet version, coverage), a link to the quickstart guide, and a demo GIF or link to the "aha moment" demo

### Story 8.2: Event Contract Reference & Technical Documentation

As a developer integrating tenant events into a consuming service,
I want comprehensive documentation on event contracts, cross-aggregate timing, and compensating commands,
So that I can design my integration correctly and handle edge cases with confidence.

**Acceptance Criteria:**

**Given** `docs/event-contract-reference.md` exists
**When** a developer reads the document
**Then** it documents all 12 commands and 11 events with their full schemas (field names, types, descriptions), organized by aggregate (TenantAggregate, GlobalAdministratorsAggregate)

**Given** the event contract reference
**When** a developer looks up a specific event (e.g., UserAddedToTenant)
**Then** the documentation includes: event name, all fields with types, a JSON example, the command that produces it, and the topic it is published on

**Given** `docs/cross-aggregate-timing.md` exists
**When** a developer reads the document
**Then** it includes: timing window explanation between tenant commands and subscriber processing, a sequence diagram showing the event propagation flow, guidance on designing for eventual consistency, and a reference to the planned Phase 2 auth plugin as the synchronous enforcement option

**Given** `docs/compensating-commands.md` exists
**When** a developer reads the document
**Then** it includes: compensating command definition, a worked example showing AddUserToTenant after an incorrect RemoveUserFromTenant, and an explanation of why the role must be explicitly specified (not auto-restored from previous state)

### Story 8.3: "Aha Moment" Demo & Project Documentation

As a developer or decision-maker evaluating Hexalith.Tenants,
I want a compelling demo showing reactive cross-service access revocation and complete project documentation,
So that I can see the value of event-sourced tenant management in under 2 minutes and understand how to contribute.

**Acceptance Criteria:**

**Given** the "aha moment" demo artifact exists (screencast, scripted demo, or reproducible script)
**When** a viewer watches or runs the demo
**Then** it demonstrates in under 2 minutes: create a tenant, add a user with TenantContributor role, show multiple subscribing services receiving the UserAddedToTenant event, remove the user, watch all services revoke access automatically, and query the event history showing the full audit trail

**Given** the demo
**When** a developer wants to reproduce it locally
**Then** instructions or a script are provided to set up the multi-service scenario using the AppHost

**Given** CHANGELOG.md exists
**When** a developer inspects it
**Then** it follows Keep a Changelog format with an initial release entry documenting MVP capabilities

**Given** CONTRIBUTING.md exists
**When** a developer reads it
**Then** it includes: development setup instructions, branch naming conventions (`feat/`, `fix/`, `docs/`), PR process, test requirements (Tier 1+2 must pass), and code style reference (`.editorconfig`)

## Follow-Up Correction Epics (2026-05-15)

These epics preserve the completed MVP epic plan above and organize deferred hardening work from `deferred-work.md` into user-value-focused follow-up areas.

### Epic 9: Trustworthy Tenant Query Operations

Operators and developers can rely on tenant query endpoints for secure, stable, tenant-isolated pagination and audit lookups.

**FRs covered:** FR25, FR26, FR27, FR28, FR29, FR30, FR31, FR34

**NFRs reinforced:** NFR2, NFR5, NFR10

### Story 9.1: Opaque Signed Query Cursors

As a platform operator,
I want paginated query cursors to be opaque and tamper-resistant,
So that clients cannot forge cursor positions or infer internal projection keys across tenant query endpoints.

**Acceptance Criteria:**

**Given** a paginated tenant query returns a continuation cursor
**When** the response is serialized
**Then** the cursor is opaque and does not expose raw timestamps, event IDs, tenant keys, or projection keys.

**Given** a client submits a valid signed cursor
**When** the matching endpoint processes the next page request
**Then** pagination resumes from the same logical position as the previous plain cursor behavior.

**Given** a client submits a tampered cursor
**When** the endpoint validates the cursor
**Then** the request is rejected with a safe `400 Bad Request` ProblemDetails response and no query state is leaked.

**Given** cursor signing is enabled
**When** `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit` return paginated results
**Then** each endpoint uses the same cursor codec/signing policy.

**Given** cursor validation fails
**When** logs are emitted
**Then** logs include correlation metadata but do not include secrets, raw signing material, or full cursor payloads.

**Given** focused query tests run
**When** valid, malformed, and tampered cursors are exercised
**Then** the tests verify success for valid cursors and safe rejection for invalid cursors across all affected paginated endpoints.

### Story 9.2: Stable Cursor Pagination Under Role and Membership Changes

As a tenant operator,
I want paginated tenant query results to remain predictable when roles or memberships change between page requests,
So that users do not silently skip or gain visibility into tenants because projection state changed mid-pagination.

**Acceptance Criteria:**

**Given** a user is paging through `get-user-tenants` results
**When** their role on a tenant is revoked between page requests
**Then** the next page does not reveal tenants the requester is no longer allowed to see.

**Given** a user is paging through `get-user-tenants` results
**When** a newly visible tenant would sort before or at the submitted cursor position
**Then** the endpoint behavior is documented and tested so the result is predictable rather than accidental.

**Given** `list-tenants` and `get-user-tenants` both use cursor-based pagination
**When** role or tenant state changes occur between page fetches
**Then** both endpoints follow the same documented cursor stability policy where applicable.

**Given** a cursor references an item that is no longer visible to the requester
**When** the endpoint processes the request
**Then** it safely advances or rejects according to the chosen policy without leaking the hidden item.

**Given** focused tests simulate concurrent membership and role mutation
**When** paginated queries continue from a prior cursor
**Then** tests verify no cross-tenant data leak and document any accepted eventual-consistency behavior.

### Story 9.3: Query Policy for Disabled Tenants and Orphan Memberships

> **Policy decision (2026-05-16):** Disabled tenants are included in query responses when the caller is otherwise authorized to see the tenant or membership. The response must include tenant status so clients can distinguish disabled access from active access. Disabled status does not imply command capability; state-changing commands against disabled tenants remain rejected by the aggregate.
>
> Orphan memberships are filtered from normal user-facing query responses, never returned with blank or synthesized tenant names. The endpoint logs an observable projection-repair warning with correlation metadata and preserves immutable audit/event history for investigation. A global administrator audit flow may surface the inconsistency only through an explicit diagnostic or repair view, not through ordinary membership list results.
>
> Eventual consistency after membership removal is accepted and documented: a self-lookup can briefly show the prior membership until projection catch-up completes, but this query result grants no write capability and must be covered by tests or explicit documentation notes.

As a platform operator,
I want tenant query endpoints to apply explicit policies for disabled tenants and inconsistent projection entries,
So that query results are predictable, explainable, and do not accidentally expose stale or misleading tenant access.

**Acceptance Criteria:**

**Given** a tenant has `TenantStatus.Disabled`
**When** `get-user-tenants` is queried by self, tenant owner, or global administrator
**Then** the response includes the disabled tenant when the caller is otherwise authorized to see the tenant or membership.

**Given** a disabled tenant appears in `get-user-tenants` or related query output
**When** the response is serialized
**Then** the response clearly includes `TenantStatus.Disabled` so clients can distinguish disabled access from active access and avoid implying command capability.

**Given** a `UserTenants` projection references a tenant missing from the tenant index/read model
**When** the query response is built
**Then** normal user-facing query responses filter the orphan membership, log an observable projection-repair warning with correlation metadata, and never return an unexplained blank or synthesized tenant name.

**Given** projection eventual consistency creates a stale self-lookup result after user removal
**When** the removed user queries their memberships briefly after removal
**Then** the accepted temporary visibility window is documented and covered by tests or explicit notes, and the stale query result does not grant write capability.

### Story 9.4: Actor-Layer Query Guardrails

As a platform operator,
I want projection actors to reject malformed or unauthenticated query envelopes defensively,
So that authorization assumptions remain protected even if a controller or caller bypasses the normal API boundary.

**Acceptance Criteria:**

**Given** a query envelope reaches `TenantsProjectionActor` with an empty or missing `UserId`
**When** the actor handles any role-sensitive query
**Then** the actor rejects the query with a safe authorization failure instead of relying only on controller-layer checks.

**Given** a role-sensitive query is executed through the normal controller path
**When** the authenticated user ID is present
**Then** existing successful query behavior remains unchanged.

**Given** a query envelope contains a user ID that is not authorized for the requested tenant data
**When** the actor evaluates the query
**Then** no tenant data is returned outside the caller's allowed scope.

**Given** actor-layer guardrails reject a query
**When** the failure is logged
**Then** logs include correlation metadata but do not expose tenant membership details or sensitive payload data.

**Given** focused actor tests run
**When** empty-user, missing-user, unauthorized-user, and valid-user query paths are exercised
**Then** tests verify defense-in-depth behavior without weakening existing controller authorization tests.

### Story 9.5: Shared Pagination Bounds and Cursor Utilities

As a developer maintaining tenant query endpoints,
I want pagination bounds and cursor handling to be centralized,
So that tenant query behavior stays consistent as endpoints evolve.

**Acceptance Criteria:**

**Given** tenant query endpoints clamp page sizes
**When** `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit` apply defaults and maximums
**Then** the shared policy uses one source of truth for default and maximum page size values.

**Given** `get-tenant-audit` currently has duplicate page-size clamping in controller and actor code
**When** the duplication is refactored
**Then** both layers continue to enforce the same default `100` and maximum `1000` behavior unless a deliberate policy change is documented.

**Given** cursor encoding/decoding is required by multiple endpoints
**When** cursor utilities are introduced or refactored
**Then** endpoint-specific ordering details remain explicit and testable rather than hidden behind unclear generic code.

**Given** invalid page sizes or cursors are submitted
**When** the endpoint validates request parameters
**Then** responses remain safe and consistent with existing API error patterns.

**Given** focused tests run
**When** page size defaults, maximum clamping, invalid inputs, and endpoint-specific cursor behavior are exercised
**Then** tests verify consistent behavior across all affected query endpoints.

### Epic 10: Durable Projection Write Safety

The tenant read models preserve projection correctness under concurrent event delivery and long-running operations.

**FRs covered:** FR25, FR26, FR27, FR28, FR29, FR30, FR53

**NFRs reinforced:** NFR5, NFR17, NFR20, NFR23

### Story 10.1: Optimistic Concurrency for Tenant Read-Model Writes

As a platform operator,
I want tenant read-model writes to use optimistic concurrency,
So that concurrent projection updates do not silently overwrite tenant query state.

**Acceptance Criteria:**

**Given** multiple tenant events update `projection:tenants:{tenantId}` concurrently
**When** the projection writes read-model state
**Then** updates use an optimistic concurrency or ETag-aware write path instead of last-writer-wins state replacement.

**Given** multiple tenant events update `projection:tenant-index:singleton` concurrently
**When** the shared tenant index is modified
**Then** conflicting writes are retried or safely rejected according to a documented retry policy.

**Given** a concurrency conflict occurs during read-model persistence
**When** the retry policy is applied
**Then** the final read model includes all successfully processed events without silently dropping one update.

**Given** the retry limit is exceeded
**When** the projection cannot safely persist state
**Then** the failure is observable through logs/metrics and does not report a successful projection update.

**Given** focused tests simulate concurrent read-model writes
**When** tenant projection and tenant index updates race
**Then** tests verify no silent data loss and document the selected retry behavior.

### Story 10.2: Audit Projection Write Safety

As a global administrator,
I want tenant audit projection writes to preserve every access-change event,
So that audit reports remain complete even when many tenant membership changes are processed at the same time.

**Acceptance Criteria:**

**Given** multiple access-change events are applied to `audit:{tenantId}` concurrently
**When** `TenantAuditProjection` persists audit read-model state
**Then** the write path prevents silent last-writer-wins loss of audit entries.

**Given** audit events for the same tenant arrive close together
**When** the projection updates the audit timeline
**Then** each event remains queryable by date range and pagination cursor after processing completes.

**Given** a concurrency conflict occurs while saving audit state
**When** the projection retries or reloads state
**Then** the final audit read model preserves all events that were successfully processed.

**Given** audit write safety cannot be guaranteed after retry exhaustion
**When** the projection reports failure
**Then** the failure is observable and does not falsely mark the projection update as complete.

**Given** focused audit projection tests run
**When** concurrent add/remove/change-role events are projected
**Then** tests verify that audit entry count and ordering remain correct.

### Story 10.3A: EventStore Projection Cancellation API Prerequisite

As a platform framework maintainer,
I want EventStore projection query and projection dispatch contracts to expose cancellation-aware signatures,
So that tenant query and projection code can observe abandoned requests without inventing Tenants-only infrastructure.

**Dependency status (2026-05-16):** Hexalith.EventStore currently lacks cancellation-aware signatures on the key projection contracts used by Tenants: `IProjectionActor.QueryAsync(QueryEnvelope envelope)`, `CachingProjectionActor.ExecuteQueryAsync(QueryEnvelope envelope)`, and synchronous `EventStoreProjection<TReadModel>.Project(...)` / `ProjectFromJson(...)`. Tenants cancellation call-site work is blocked until this prerequisite is completed in the `Hexalith.EventStore` submodule or an approved compatible API already exists.

**Acceptance Criteria:**

**Given** `IProjectionActor.QueryAsync` is the public DAPR actor projection query contract
**When** cancellation support is added
**Then** the contract or an approved companion path accepts and propagates a `CancellationToken` without breaking existing callers unexpectedly.

**Given** `CachingProjectionActor` executes projection query logic
**When** derived actors implement query handlers
**Then** `ExecuteQueryAsync` or its approved replacement receives cancellation and can pass it to downstream reads.

**Given** projection read/write infrastructure uses DAPR state APIs
**When** projection state is read or written
**Then** DAPR calls receive the propagated cancellation token where supported.

**Given** the EventStore API change is merged or otherwise available to Tenants
**When** Story 10.3B starts
**Then** the Tenants story names the exact EventStore APIs and version/submodule commit it depends on.

### Story 10.3B: Cancellation Token Threading for Tenant Projection Queries

As a platform operator,
I want long-running tenant projection queries and projection writes to observe request cancellation,
So that abandoned requests do not keep consuming compute or block projection processing unnecessarily.

**Blocked by:** Story 10.3A, unless grooming confirms an existing EventStore cancellation-aware projection path and records the exact APIs before implementation.

**Acceptance Criteria:**

**Given** a request to a paginated tenant query is cancelled by the client
**When** the cancellation reaches the query endpoint
**Then** cancellation is propagated through the projection dispatch path instead of being dropped.

**Given** `HandleGetTenantAuditAsync` performs a long-running audit read
**When** the caller cancels the request
**Then** the read observes the cancellation token and stops without returning partial successful data.

**Given** `TenantProjectionHandler.ProjectAsync` performs projection work
**When** the hosting pipeline supplies cancellation
**Then** projection reads/writes observe the provided token through the EventStore APIs named by Story 10.3A.

**Given** focused tests run
**When** cancellation is triggered before and during projection query handling
**Then** tests verify cancellation is observed and does not corrupt read-model state.

### Story 10.4: Projection Write Conformance and Recovery Tests

As a developer maintaining tenant projections,
I want focused conformance and recovery tests for projection persistence behavior,
So that future projection changes cannot reintroduce silent write loss, ordering errors, or recovery gaps.

**Acceptance Criteria:**

**Given** tenant read-model projections use a selected concurrency/retry policy
**When** conformance tests run against tenant detail, tenant index, and audit projection writes
**Then** each projection proves it preserves all successfully processed events under concurrent updates.

**Given** projection writes encounter transient persistence conflicts
**When** retry behavior is exercised in tests
**Then** tests verify the projection eventually succeeds or reports a safe observable failure.

**Given** projection writes fail after retry exhaustion
**When** recovery behavior is tested
**Then** the failure path does not claim success and leaves enough diagnostic information to replay or repair safely.

**Given** projection event ordering matters for cursor and audit behavior
**When** tests project mixed lifecycle, membership, configuration, and audit events
**Then** resulting read models preserve deterministic ordering for query responses.

**Given** future projection implementations are added
**When** they opt into the tenant projection conformance test suite
**Then** the same concurrency and recovery expectations can be reused without duplicating test logic.

### Epic 11: Production Authorization Readiness

Platform operators can deploy Hexalith.Tenants with validated JWT configuration and predictable rate-limit partitioning.

**FRs covered:** FR15, FR31, FR32, FR33, FR34, FR48, FR56

**NFRs reinforced:** NFR5, NFR22

### Story 11.1: Production JWT Configuration Validation

As a platform operator,
I want production JWT configuration to be validated before deployment,
So that Hexalith.Tenants does not fail at startup or accept unsafe authentication settings unexpectedly.

**Acceptance Criteria:**

**Given** production `appsettings.json` contains empty JWT `Authority` and `SigningKey` placeholders
**When** the service starts without AppHost or environment overrides
**Then** startup fails with a clear configuration validation error.

**Given** production JWT settings are supplied through environment variables, AppHost, or deployment configuration
**When** the service starts
**Then** `EventStoreAuthenticationOptions` validation succeeds without requiring secrets in committed appsettings files.

**Given** development mode uses symmetric-key JWT validation
**When** `appsettings.Development.json` or local overrides are loaded
**Then** dev authentication remains usable without weakening production validation.

**Given** authentication configuration fails validation
**When** logs are emitted
**Then** logs identify the missing configuration key but do not expose signing keys or token material.

**Given** focused configuration tests run
**When** production-valid, production-invalid, and development-valid configurations are bound
**Then** tests verify startup validation behavior for each mode.

### Story 11.2: EventStore Tenant Claim Contract

As a platform operator,
I want the production identity provider to emit the tenant claim expected by EventStore infrastructure,
So that authenticated requests are partitioned and authorized consistently instead of falling into a shared anonymous bucket.

**Acceptance Criteria:**

**Given** Hexalith.Tenants is deployed with a production IdP
**When** an authenticated token is issued
**Then** the token includes the configured `eventstore:tenant` claim value required by EventStore tenant validation and rate-limit partitioning.

**Given** an authenticated token is missing the `eventstore:tenant` claim
**When** a request reaches the API
**Then** the behavior is explicit and tested: reject the token or route it through a documented fallback, rather than silently sharing an `"anonymous"` rate-limit bucket.

**Given** test JWTs and `TestAuthHandler` assert tenant claim behavior
**When** production claim-contract tests are reviewed
**Then** test assumptions match the documented IdP claim contract.

**Given** a deployment operator configures Keycloak or another IdP
**When** they follow the deployment documentation
**Then** the required claim mapping is documented with the exact claim name, value expectations, and verification steps.

**Given** focused auth tests run
**When** tokens include, omit, or vary the tenant claim
**Then** tests verify the selected production behavior and rate-limit partitioning assumptions.

### Story 11.3: Deployment Auth Readiness Documentation and Smoke Tests

As a platform operator,
I want deployment documentation and smoke tests that prove production authentication is wired correctly,
So that auth misconfiguration is caught before users hit runtime failures.

**Acceptance Criteria:**

**Given** deployment documentation is updated
**When** an operator prepares Hexalith.Tenants for production
**Then** the docs list required JWT settings, required IdP claims, environment variable names, and AppHost/deployment override expectations.

**Given** an operator follows the readiness checklist
**When** they verify a deployment
**Then** the checklist includes token issuer, audience, `eventstore:tenant`, HTTPS metadata, signing/authority source, and rate-limit partitioning checks.

**Given** production-like smoke tests run
**When** valid and invalid tokens are used against protected tenant endpoints
**Then** valid tokens succeed only within their allowed scope and invalid/misconfigured tokens fail safely.

**Given** the service is deployed with missing or invalid auth overrides
**When** the smoke test or startup validation runs
**Then** the failure points to the missing deployment input rather than producing ambiguous runtime errors.

**Given** local development docs remain available
**When** developers use dev-mode JWTs
**Then** the docs clearly separate development token generation from production IdP configuration.

### Epic 12: Phase 2 Admin UI Dependency Sequencing

> **Scope note (2026-05-16):** Epic 12 is a Phase 2 planning/readiness and dependency-governance epic. It is not Phase 1 backend implementation scope and should not be counted as shipped Admin UI product behavior. When Phase 2 UI work is ready to implement, convert the outputs of these stories into concrete UI implementation stories with explicit `blockedBy` dependencies.

Admin UI delivery can proceed without hidden FrontShell blockers by sequencing cross-project dependencies explicitly.

**FRs covered:** FR25, FR26, FR27, FR28, FR29, FR31, FR34

**NFRs reinforced:** NFR24

### Story 12.1: FrontShell Dependency Map for Tenants UI

As a product owner planning Phase 2 Admin UI work,
I want each Tenants UI screen mapped to its required FrontShell components, hooks, and tokens,
So that UI implementation starts only when required cross-project dependencies are known and sequenced.

**Acceptance Criteria:**

**Given** the Tenants UX specification lists Phase 2 screens
**When** the dependency map is created
**Then** each screen maps to the FrontShell components, hooks, tokens, and Storybook references it requires.

**Given** a screen depends on `<AuditTimeline>`, `<ConsequencePreview>`, `useCommand pendingIds`, concurrent command support, toast batching, layout variants, or design tokens
**When** the dependency map is reviewed
**Then** the dependency is captured with the owning project, expected deliverable, and readiness status.

**Given** a Tenants UI story is drafted
**When** it consumes a FrontShell deliverable
**Then** the story references the corresponding dependency rather than silently assuming it exists.

**Given** a dependency is not yet available
**When** a Tenants UI story would require it
**Then** the story is blocked or scoped to a fallback explicitly approved by product and UX.

**Given** the dependency map is complete
**When** Phase 2 planning starts
**Then** backend MVP stories remain unblocked and UI dependencies are not promoted into Phase 1 scope accidentally.

### Story 12.2: Audit Timeline and Consequence Preview Readiness

As a UX/product owner planning the Tenants Admin UI,
I want audit and consequence-preview component dependencies resolved before dependent screens are scheduled,
So that high-risk access-management workflows have the right interaction patterns available when implementation begins.

**Acceptance Criteria:**

**Given** the Audit Trail screen or tenant detail audit tab is planned
**When** the story is created
**Then** it declares a dependency on the `<AuditTimeline>` FrontShell component or an explicitly approved fallback.

**Given** a remove-user, disable-tenant, or remove-global-admin workflow is planned
**When** the story is created
**Then** it declares a dependency on `<ConsequencePreview>` or an explicitly approved fallback.

**Given** `<AuditTimeline>` is required for MVP flat timeline mode
**When** readiness is assessed
**Then** the flat timeline behavior, accessibility expectations, loading states, and 500-event performance target are defined.

**Given** grouped-by-session audit mode is not required for the first UI slice
**When** backlog sequencing is reviewed
**Then** grouped mode is marked as fast-follow and does not block the flat timeline story.

**Given** consequence previews use already-loaded projection data
**When** remove/disable workflows are designed
**Then** stories confirm no dedicated backend consequence endpoint is required unless product explicitly changes scope.

### Story 12.3: Three-Phase Command Feedback Sequencing

As an admin UI user,
I want command actions to show clear optimistic, confirming, and confirmed states,
So that I can trust whether tenant changes have been accepted, projected, and reflected in the interface.

**Acceptance Criteria:**

**Given** a Tenants UI story includes row-level or form-level commands
**When** the story is planned
**Then** it declares dependencies on `useCommand pendingIds`, concurrent command support, SignalR projection confirmation, and toast batching where those behaviors are required.

**Given** a user submits a tenant command from the UI
**When** the command is accepted but projection confirmation has not arrived
**Then** the UI story specifies the optimistic and confirming visual states without blocking unrelated user activity.

**Given** projection confirmation arrives through SignalR
**When** the matching pending command is confirmed
**Then** the UI story specifies how row state, cache/projection data, and toast feedback settle into the confirmed state.

**Given** SignalR is disconnected or delayed
**When** confirmation does not arrive within the expected threshold
**Then** the story specifies a degraded feedback pattern that warns the user without losing their action context.

**Given** multiple command confirmations arrive within a short burst
**When** toast feedback is shown
**Then** the story uses consolidated/batched toast behavior to avoid overwhelming the user.

### Story 12.4: Phase 2 UI Story Backlog with Explicit `blockedBy`

As a product owner planning Phase 2 Admin UI delivery,
I want every Tenants UI story to declare its FrontShell and backend dependencies explicitly,
So that implementation sequencing is transparent and stories do not hide unavailable prerequisites.

**Acceptance Criteria:**

**Given** Phase 2 Tenants UI stories are created
**When** a story depends on a FrontShell deliverable
**Then** the story includes an explicit `blockedBy` entry naming the owning FrontShell story or dependency artifact.

**Given** a UI story depends only on completed backend/query functionality
**When** the story is reviewed
**Then** it references the completed backend story or endpoint rather than creating a duplicate backend requirement.

**Given** a UI story requires a backend capability that is not complete
**When** the backlog is reviewed
**Then** the backend dependency is called out separately and the UI story is not marked ready for development.

**Given** Phase 2 stories are prioritized
**When** the backlog is ordered
**Then** dependency-free or dependency-ready stories appear before blocked stories, unless product explicitly accepts the sequencing risk.

**Given** story readiness is checked
**When** a UI story has unresolved `blockedBy` entries
**Then** the story remains blocked or planning-only and cannot be assigned as implementation-ready.

### Follow-Up FR Coverage Map

- FR15: Epic 11 - production authorization and global admin deployment readiness
- FR25: Epic 9, Epic 10, Epic 12 - list tenants query hardening, projection correctness, UI sequencing
- FR26: Epic 9, Epic 10, Epic 12 - tenant detail query hardening and UI dependency alignment
- FR27: Epic 9, Epic 10, Epic 12 - tenant users query hardening and UI dependency alignment
- FR28: Epic 9, Epic 10, Epic 12 - user tenants scoped lookup correctness and UI incident-response support
- FR29: Epic 9, Epic 10, Epic 12 - audit query cursor/security correctness and audit UI sequencing
- FR30: Epic 9, Epic 10 - cursor pagination security, stability, and projection correctness
- FR31-FR34: Epic 9, Epic 11, Epic 12 - role-aware query behavior, deployment auth, UI role behavior
- FR48, FR56: Epic 11 - deployable service auth configuration readiness
- FR53: Epic 10 - projection durability and source-of-truth behavior
