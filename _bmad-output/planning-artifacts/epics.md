---
stepsCompleted:
    - step-01-validate-prerequisites
    - step-02-design-epics
    - step-03-create-stories
    - step-04-final-validation
status: complete
completedAt: "2026-03-07"
inputDocuments:
    - prd.md
    - architecture.md
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
- Two aggregates: TenantAggregate (lifecycle, user-role management, configuration) and GlobalAdministratorAggregate (cross-tenant admin roles, bootstrap). Separate because GlobalAdmin is platform-level, not tenant-scoped
- Identity Mapping: Platform tenant context = `system` (configurable), domain = `tenants`, aggregateId = managed tenant ID or `global-administrators`. Actor IDs: `system:tenants:acme-corp`, `system:tenants:global-administrators`
- Pub/Sub topic: `system.tenants.events` -- single topic for all tenant events; consumers filter by event type
- Read model: `EventStoreProjection<TReadModel>` pattern with DAPR state store. Three projections needed: TenantProjection, GlobalAdministratorProjection, TenantIndexProjection (cross-tenant)
- Cross-tenant index projections use ETag-based optimistic concurrency (`ConcurrencyMode.FirstWrite`) with retry logic (max 3 attempts) to prevent silent data loss on concurrent updates
- Snapshot strategy: 50-event interval for tenant domain, default 100 for GlobalAdministratorAggregate
- Bootstrap mechanism: Startup config via `appsettings.json` (`Tenants:BootstrapGlobalAdminUserId`), executed through full MediatR pipeline. GlobalAdministratorAggregate rejects if any GlobalAdministratorSet event exists
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
**Then** it specifies SDK version 10.0.103 with `rollForward: latestPatch`

**Given** the solution is built
**When** a developer inspects `Directory.Build.props`
**Then** it contains shared project properties including NuGet metadata, nullable references enabled, implicit usings enabled, and warnings as errors

**Given** the solution is built
**When** a developer inspects `Directory.Packages.props`
**Then** it contains centralized NuGet package versions for all dependencies (EventStore, DAPR SDK, Aspire, xUnit, Shouldly, NSubstitute, coverlet, FluentValidation, MediatR, MinVer)

**Given** the solution is built
**When** a developer inspects `.editorconfig`
**Then** it enforces EventStore conventions (file-scoped namespaces, Allman braces, `_camelCase` private fields, 4-space indentation)

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
**Then** `actors.yaml` configures TenantAggregate and GlobalAdministratorAggregate actor types

**Given** the ServiceDefaults project exists
**When** a developer inspects `Extensions.cs`
**Then** it contains OpenTelemetry configuration with tracing and metrics setup following EventStore's ServiceDefaults pattern

**Given** the ServiceDefaults project exists
**When** `dotnet build` is executed
**Then** the ServiceDefaults project compiles successfully and is referenced by Hexalith.Tenants

### Story 1.3: CI/CD Pipeline

As a developer,
I want GitHub Actions workflows for continuous integration and release publishing,
So that every PR is validated automatically and tagged releases publish NuGet packages.

**Acceptance Criteria:**

**Given** a developer pushes a commit or opens a PR to main
**When** the CI workflow (`ci.yml`) triggers
**Then** it executes: restore, build (Release configuration), and runs Tier 1+2 tests

**Given** the CI workflow runs
**When** all tests pass
**Then** the workflow reports success and code coverage is collected via coverlet

**Given** a developer pushes a tag matching `v*` (e.g., `v0.1.0`)
**When** the release workflow (`release.yml`) triggers
**Then** it executes the full test suite, packs all 5 NuGet packages (Contracts, Client, Server, Testing, Aspire), validates the expected package count (5), and pushes to NuGet.org

**Given** the release workflow runs
**When** the package count does not match the expected 5
**Then** the workflow fails before pushing to NuGet.org

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

**Given** the GlobalAdministratorAggregate Handle methods
**When** tested as static pure functions with no infrastructure
**Then** all Handle and Apply methods execute correctly as Tier 1 unit tests

**Given** the GlobalAdministratorState class
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
- Note: TenantState includes Users/Configuration Apply methods for completeness — those Handle methods are implemented in Epic 3 (Stories 3.1, 3.3) but the state class is created here with all Apply methods

Testing pattern — same as Story 2.2: `aggregate.ProcessAsync(commandEnvelope, state)` with `CommandEnvelope` helper (see Architecture §D10 Testing Blueprint). All tests Tier 1.

### Story 2.4: Tenant Service, Bootstrap & Event Publishing

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
**Then** events are published to DAPR pub/sub topic `system.tenants.events` as CloudEvents 1.0

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

DAPR version alignment: `Directory.Packages.props` must be updated from DAPR 1.16.1 to 1.17.3 to match EventStore submodule before this story begins.

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

**Given** a consuming service is subscribed to the `system.tenants.events` DAPR pub/sub topic
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
**Then** projections for tenant A never contain data from tenant B (aggregate-level isolation guarantee)

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
**Then** the Aspire dashboard launches and the Aspire dashboard launches with Hexalith.Tenants (AppId: tenants), EventStore server, and Keycloak, all started with DAPR sidecars configured for state store, pub/sub, and actors

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

**Given** the GlobalAdministratorAggregate uses the default snapshot interval of 100 events
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
**Then** it documents all 12 commands and 11 events with their full schemas (field names, types, descriptions), organized by aggregate (TenantAggregate, GlobalAdministratorAggregate)

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
