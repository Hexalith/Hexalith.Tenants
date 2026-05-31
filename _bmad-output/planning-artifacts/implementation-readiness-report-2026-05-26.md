---
date: 2026-05-26
project: Tenants
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
readinessStatus: NEEDS WORK
issueCounts:
  critical: 3
  major: 6
  minor: 3
  uxAlignment: 3
includedDocuments:
  prd: _bmad-output/planning-artifacts/prd.md
  architecture: _bmad-output/planning-artifacts/architecture.md
  epics: _bmad-output/planning-artifacts/epics.md
  ux: _bmad-output/planning-artifacts/ux-design-specification.md
excludedDocuments:
  - path: _bmad-output/planning-artifacts/prd-validation-report.md
    reason: Prior PRD validation report matched the PRD filename pattern but is not a source PRD document.
---

# Implementation Readiness Assessment Report

**Date:** 2026-05-26
**Project:** Tenants

## Step 1: Document Discovery

### Confirmed Assessment Documents

- PRD: `_bmad-output/planning-artifacts/prd.md`
- Architecture: `_bmad-output/planning-artifacts/architecture.md`
- Epics and Stories: `_bmad-output/planning-artifacts/epics.md`
- UX Design: `_bmad-output/planning-artifacts/ux-design-specification.md`

### Inventory

#### PRD Files Found

Whole documents:
- `prd.md` (58,020 bytes, modified 2026-05-18 10:58:44)
- `prd-validation-report.md` (25,410 bytes, modified 2026-03-07 18:35:12)

Sharded documents:
- None

#### Architecture Files Found

Whole documents:
- `architecture.md` (42,903 bytes, modified 2026-05-26 21:09:42)

Sharded documents:
- None

#### Epics and Stories Files Found

Whole documents:
- `epics.md` (100,946 bytes, modified 2026-05-18 10:58:44)

Sharded documents:
- None

#### UX Design Files Found

Whole documents:
- `ux-design-specification.md` (82,514 bytes, modified 2026-05-26 17:04:40)

Sharded documents:
- None

### Discovery Issues

- No whole-vs-sharded duplicate document formats were found.
- `prd-validation-report.md` was excluded from source assessment because it appears to be a prior validation artifact rather than the PRD source.

## Step 2: PRD Analysis

### Functional Requirements

FR1: A global administrator can create a new tenant with a unique identifier and name (in MVP, tenant creation is restricted to global administrators)

FR2: A developer can update a tenant's metadata (name, description)

FR3: A global administrator can disable a tenant, preventing all commands against that tenant from succeeding

FR4: A global administrator can re-enable a previously disabled tenant, restoring normal command processing

FR5: The system produces a domain event for every tenant lifecycle change (created, updated, disabled, enabled)

FR6: A tenant owner can add a user to a tenant with a specified role (TenantOwner, TenantContributor, or TenantReader)

FR7: A tenant owner can remove a user from a tenant

FR8: A tenant owner can change a user's role within a tenant

FR9: The system rejects adding a user who is already a member of the tenant

FR10: The system rejects role changes that violate escalation boundaries (a tenant owner cannot assign GlobalAdministrator)

FR11: The system produces a domain event for every user-role change (added, removed, role changed)

FR12: The system enforces optimistic concurrency, rejecting conflicting concurrent modifications to the same aggregate

FR13: An existing global administrator can designate a user as a global administrator

FR14: An existing global administrator can remove a user's global administrator status (cannot remove self if they are the last global administrator)

FR15: A global administrator can perform any tenant operation across all tenants without per-tenant role assignment

FR16: All global administrator actions produce auditable domain events

FR17: The system provides a bootstrap mechanism (seed command or startup configuration) to create the initial global administrator on first deployment when no global administrators exist

FR18: The bootstrap mechanism only executes when zero global administrators exist in the event store - subsequent executions are rejected with a specific error indicating that bootstrap has already been completed

FR19: A tenant owner can set a key-value configuration entry for a tenant

FR20: A tenant owner can remove a configuration entry from a tenant

FR21: Configuration keys support dot-delimited namespace conventions (e.g., `billing.plan`, `parties.maxContacts`) to prevent collisions between consuming services

FR22: The system produces a domain event for every configuration change (set, removed)

FR23: The system enforces configuration limits: maximum 100 keys per tenant, maximum 1KB per value, maximum 256 characters per key

FR24: The system rejects configuration operations that exceed limits with a specific error identifying which limit was exceeded and the current usage

FR25: A developer can query a paginated list of all tenants with their IDs, names, and statuses

FR26: A developer can query a specific tenant's details including its current users and their roles

FR27: A developer can query the list of users in a specific tenant with their assigned roles

FR28: A developer can query the list of tenants a specific user belongs to, with their role in each tenant

FR29: A global administrator can query tenant access changes by tenant ID and date range for audit reporting, with pagination support (default page size: 100 results, maximum: 1,000)

FR30: All list and query endpoints support cursor-based pagination with consistent ordering

FR31: A TenantReader can query tenant details, user lists, and configuration for tenants they belong to, but cannot execute any state-changing commands

FR32: A TenantContributor has TenantReader capabilities plus the ability to execute domain commands within the tenant (the specific commands are defined by each consuming service)

FR33: A TenantOwner has TenantContributor capabilities plus user-role management (add, remove, change role) and tenant configuration management

FR34: A user with roles in multiple tenants can only access data and execute commands within each tenant according to their role in that specific tenant - roles do not transfer or aggregate across tenants

FR35: The system publishes all tenant domain events via DAPR pub/sub as CloudEvents 1.0

FR36: The system uses a documented topic naming convention for tenant events (e.g., `tenants.events`) consistent with Hexalith ecosystem patterns

FR37: A consuming service can subscribe to tenant events and build a local projection of tenant state

FR38: A consuming service can react to user addition/removal events to enforce or revoke access

FR39: A consuming service can react to tenant disable/enable events to block or allow operations

FR40: A consuming service can react to configuration change events to update tenant-specific behavior

FR41: Event contracts include sufficient information (event ID, aggregate version) for consuming services to implement idempotent event handling

FR42: Documentation provides guidance on idempotent event processing patterns for consumers, since DAPR pub/sub may deliver events more than once. Minimum content: at-least-once delivery explanation, deduplication by event ID example, idempotent handler pattern with code sample

FR43: A developer can install Tenants via NuGet packages (Contracts, Client, Server, Testing, Aspire)

FR44: A developer can register tenant client services in DI with a single extension method call

FR45: A developer can register tenant event handlers in a consuming service in under 20 lines of DI configuration

FR46: A developer can write tenant integration tests using in-memory fakes without external infrastructure, in under 10 lines per test

FR47: The in-memory testing fakes execute the same domain logic as the production service, guaranteeing isolation at the aggregate domain model level (command validation, event production, state transitions), verified by a conformance test suite that runs identical command sequences against both fakes and production aggregate. Projection-level and query-level isolation is the responsibility of the consuming service's own test suite

FR48: A developer can deploy the tenant service using .NET Aspire hosting extensions

FR49: The system provides error messages for all command rejections that include: the specific rejection reason, the entity involved, and a corrective action hint

FR50: The system rejects commands targeting a non-existent tenant with a specific error identifying the missing tenant

FR51: The system rejects commands targeting a disabled tenant with a specific error indicating the tenant's disabled status

FR52: The system rejects duplicate operations (e.g., adding an already-present user) with a specific error including current state

FR53: Commands and event storage succeed independently of DAPR pub/sub availability (event store is the source of truth)

FR54: The system exposes tenant command latency metrics via OpenTelemetry

FR55: The system exposes event processing metrics via OpenTelemetry

FR56: A platform operator can deploy the tenant service alongside EventStore using standard DAPR configuration

FR57: The tenant service is stateless between requests - all state is reconstructed from the event store on startup

FR58: The CI/CD pipeline enforces quality gates: build, test (Tier 1+2), coverage threshold (> 80% line, 100% branch on isolation/auth), and package validation before NuGet publish

FR59: The project provides a quickstart guide that enables a developer to send their first tenant command within 30 minutes

FR60: The quickstart guide includes prerequisite validation (DAPR sidecar, EventStore deployment)

FR61: The project provides an event contract reference documenting all commands, events, and their schemas

FR62: The project provides a sample consuming service demonstrating event subscription and access enforcement

FR63: The project provides an "aha moment" demo (screencast or video) showing reactive cross-service access revocation

FR64: The project provides documentation on cross-aggregate timing behavior, including the event propagation window between tenant commands and subscriber processing. Minimum content: timing window explanation, sequence diagram, guidance on designing for eventual consistency, reference to planned auth plugin as synchronous enforcement option

FR65: The project provides documentation on compensating command patterns (e.g., restoring a wrongly removed user with explicit role specification). Minimum content: compensating command definition, worked example with AddUserToTenant after incorrect RemoveUserFromTenant, explanation of why role must be explicitly specified (not auto-restored)

Total FRs: 65

### Non-Functional Requirements

NFR1: All tenant commands complete within 50ms (p95) as measured by OpenTelemetry span duration

NFR2: All read model queries complete within 50ms (p95) for result sets within a single page (see FR30 pagination), as measured by OpenTelemetry span duration

NFR3: Event publication to DAPR pub/sub completes within 50ms (p95) after command processing, as measured by OpenTelemetry span duration

NFR4: In-memory testing fakes execute commands and produce events within 10ms, as measured by xUnit test execution time

NFR5: Zero cross-tenant data leaks - no query, projection, or event subscription returns data belonging to a different tenant, verified by dedicated Tier 3 integration tests that assert isolation across all read model endpoints and event subscriptions

NFR6: Role escalation boundaries enforced at the domain level - no actor can self-escalate, verified by unit tests that assert rejection of every escalation path (TenantOwner assigning GlobalAdministrator, self-role elevation)

NFR7: All state-changing operations produce immutable, auditable domain events with actor ID, timestamp, and full operation context, verified by integration tests that assert event production for every command type and validate required event fields are populated

NFR8: Disabled tenants reject all commands immediately within the same aggregate, verified by unit tests that assert command rejection after DisableTenant is applied to aggregate state

NFR9: Encryption at rest and in transit is a deployment concern - the system relies on DAPR infrastructure configuration for encryption and does not implement its own encryption layer

NFR10: 100% branch coverage on tenant isolation and role authorization logic (defined as: aggregate Handle methods for authorization checks, tenant ID filtering in projections, and role validation logic), verified in CI via coverlet

NFR11: The system supports up to 1,000 tenants with up to 500 users per tenant without performance degradation beyond stated latency targets, verified by load tests seeding the target volume and asserting NFR1-NFR3 latency targets hold

NFR12: The tenant service is stateless - horizontal scaling achieved by adding service instances

NFR13: State reconstruction from the event store on startup completes within 30 seconds for up to 1,000 tenants with an assumed average of 500 events per tenant (500,000 total events), verified by a startup benchmark test that seeds the target event volume and measures time to ready state. Baseline EventStore snapshot configuration is part of Phase 1 reliability/performance work; advanced snapshot tuning beyond the baseline configuration is a Phase 3 optimization if this target is exceeded at scale.

NFR14: All domain events conform to CloudEvents 1.0 specification

NFR15: Event publication uses DAPR pub/sub abstraction - no direct dependency on a specific message broker

NFR16: State persistence uses DAPR state store abstraction - no direct dependency on a specific database

NFR17: The system degrades gracefully when DAPR pub/sub is unavailable - commands succeed, subscribers catch up when pub/sub recovers, verified by a Tier 3 integration test that disables pub/sub, executes commands, re-enables pub/sub, and asserts subscribers receive all pending events

NFR18: Event contracts are backward-compatible after v1.0 - no breaking schema changes to published events

NFR19: All domain events include event ID and aggregate version to enable idempotent processing by consumers

NFR20: The event store is the single source of truth - system state can be fully reconstructed by replaying events

NFR21: Command processing and event storage are atomic - a command either fully succeeds or fully fails

NFR22: API availability target: 99.9% in production deployments, as measured by health check endpoint uptime monitoring

NFR23: No data loss under any failure scenario - events once stored are immutable and durable

NFR24: MVP error messages and documentation are English-only. Phase 2 Admin UI must address WCAG 2.1 AA accessibility and i18n considerations as part of its requirements scoping

Total NFRs: 24

### Additional Requirements

- Phase 1 MVP is backend/package/documentation only. It includes tenant domain behavior, query endpoints, audit-query capability, packages, tests, deployment, observability, and adoption documentation.
- Admin UI / FrontShell reference module is Phase 2 unless explicitly promoted by a future scope decision.
- Event contracts may evolve with breaking changes during pre-1.0 development; event contract stability is a v1.0 milestone.
- Tenant deletion is out of scope for all phases. Tenants can be disabled but never deleted; a disabled tenant is terminal.
- gRPC API surface is out of scope for all phases. The command API uses REST only.
- Phase 2 growth priorities include the EventStore tenant authorization plugin, Keycloak JWT projection sync, Admin UI / FrontShell reference module, custom/extensible roles, bulk tenant provisioning, and F# consumption support.
- Phase 3 expansion candidates include hierarchical sub-tenants, multi-deployment tenant migration through event replay, per-tenant service registry, cross-deployment tenant federation, and advanced snapshot optimization.
- The product is distributed as five NuGet packages: `Hexalith.Tenants.Contracts`, `Hexalith.Tenants.Client`, `Hexalith.Tenants.Server`, `Hexalith.Tenants.Testing`, and `Hexalith.Tenants.Aspire`.
- Required solution structure includes `Hexalith.Tenants.slnx`, source projects for contracts, client, server, host, Aspire, AppHost, ServiceDefaults, and Testing, plus Tier 1, Tier 2, and Tier 3 test projects and a sample consuming service.
- The command API is REST, tenant events are CloudEvents 1.0 over DAPR pub/sub, read model queries include list/get tenant and user views, and consuming services register through minimal DI extension methods.
- Test architecture is tiered: Tier 1 unit tests without external dependencies, Tier 2 integration tests requiring DAPR slim init, and Tier 3 Aspire E2E contract tests requiring full DAPR init and Docker.
- Documentation strategy includes README quickstart demo material, docs folder, changelog, contributing guide, inline C# code samples, markdown/link validation, and quickstart targeting under 30 minutes.
- CI/CD expectations include GitHub Actions restore/build/test/package validation, semantic-release/Conventional Commits, and NuGet publishing.
- Implementation considerations require EventStore aggregate pattern, pure command handlers, reflection-based Handle/Apply discovery, tenant commands following EventStore domain/aggregate/tenant patterns, and DAPR sidecar infrastructure access.
- Success metrics include under 30 minutes to first command, under 20 lines for consuming-service integration, under 10 lines per tenant integration test, zero cross-tenant leaks, 100% isolation/auth branch coverage, p95 event latency under 50ms, greater than 80% overall line coverage, and Hexalith.Parties as first consuming project.

### PRD Completeness Assessment

The PRD is broad and mostly implementation-ready as a source of traceability. It has explicit numbered FRs/NFRs, measurable success criteria, clear MVP versus post-MVP boundaries, target packages, test tiers, deployment assumptions, documentation deliverables, and known timing-consistency constraints.

Initial concerns for later validation:

- Some requirements blend product behavior with documentation deliverables, CI/CD policy, and adoption artifacts. This is acceptable for planning but requires careful epic mapping so non-code deliverables are not lost.
- Several requirements are verification-heavy and need explicit story coverage for benchmark/load tests, Tier 3 isolation tests, idempotent event processing examples, and documentation content minimums.
- FR2 says "A developer can update a tenant's metadata" while the tenant lifecycle section otherwise restricts create/disable/enable to global administrators; later epic coverage should confirm the intended authorization role for update.
- The PRD contains a code-style statement inherited from EventStore that says Allman braces, while current project context says Tenants uses K&R brace style. This is a planning/context conflict to track during readiness validation.
- Release trigger language in the PRD says release is triggered by `v*` tags, while project context says release is triggered on merge to `main` through semantic-release. This is another planning/context conflict to resolve or document.

## Step 3: Epic Coverage Validation

### Epic FR Coverage Extracted

- FR1: Covered in Epic 2 - Create tenant with unique identifier and name
- FR2: Covered in Epic 2 - Update tenant metadata
- FR3: Covered in Epic 2 - Disable tenant
- FR4: Covered in Epic 2 - Re-enable disabled tenant
- FR5: Covered in Epic 2 - Domain events for tenant lifecycle changes
- FR6: Covered in Epic 3 - Add user to tenant with role
- FR7: Covered in Epic 3 - Remove user from tenant
- FR8: Covered in Epic 3 - Change user role within tenant
- FR9: Covered in Epic 3 - Reject duplicate user addition
- FR10: Covered in Epic 3 - Reject role escalation violations
- FR11: Covered in Epic 3 - Domain events for user-role changes
- FR12: Covered in Epic 3 - Optimistic concurrency enforcement
- FR13: Covered in Epic 2 - Designate global administrator
- FR14: Covered in Epic 2 - Remove global administrator status
- FR15: Covered in Epic 2 - Global admin cross-tenant operations
- FR16: Covered in Epic 2 - Auditable global admin events
- FR17: Covered in Epic 2 - Bootstrap mechanism for initial global admin
- FR18: Covered in Epic 2 - Bootstrap rejected when global admin exists
- FR19: Covered in Epic 3 - Set key-value configuration entry
- FR20: Covered in Epic 3 - Remove configuration entry
- FR21: Covered in Epic 3 - Dot-delimited namespace conventions
- FR22: Covered in Epic 3 - Domain events for configuration changes
- FR23: Covered in Epic 3 - Configuration limits enforcement
- FR24: Covered in Epic 3 - Reject operations exceeding limits
- FR25: Covered in Epic 5 - Paginated tenant list query
- FR26: Covered in Epic 5 - Specific tenant detail query
- FR27: Covered in Epic 5 - Tenant users list query
- FR28: Covered in Epic 5 - User tenants list query
- FR29: Covered in Epic 5 - Audit queries by tenant and date range
- FR30: Covered in Epic 5 - Cursor-based pagination
- FR31: Covered in Epic 3 - TenantReader query-only behavior
- FR32: Covered in Epic 3 - TenantContributor domain command capability
- FR33: Covered in Epic 3 - TenantOwner user-role and config management
- FR34: Covered in Epic 3 - Cross-tenant role isolation
- FR35: Covered in Epic 2 - DAPR pub/sub CloudEvents 1.0 publishing
- FR36: Covered in Epic 2 - Documented topic naming convention
- FR37: Covered in Epic 4 - Consuming service event subscription and local projection
- FR38: Covered in Epic 4 - React to user addition/removal events
- FR39: Covered in Epic 4 - React to tenant disable/enable events
- FR40: Covered in Epic 4 - React to configuration change events
- FR41: Covered in Epic 4 - Event contracts for idempotent handling
- FR42: Covered in Epic 4 - Idempotent event processing documentation
- FR43: Covered in Epic 1 - NuGet package distribution
- FR44: Covered in Epic 4 - Single extension method DI registration
- FR45: Covered in Epic 4 - Event handler registration under 20 lines
- FR46: Covered in Epic 6 - In-memory fakes without infrastructure
- FR47: Covered in Epic 6 - Testing fakes use same domain logic
- FR48: Covered in Epic 7 - .NET Aspire hosting extensions
- FR49: Covered in Epic 2 - Actionable error messages for command rejections
- FR50: Covered in Epic 2 - Reject commands for non-existent tenant
- FR51: Covered in Epic 2 - Reject commands for disabled tenant
- FR52: Covered in Epic 2 - Reject duplicate operations
- FR53: Covered in Epic 2 - Commands succeed independently of pub/sub
- FR54: Covered in Epic 7 - Command latency metrics via OpenTelemetry
- FR55: Covered in Epic 7 - Event processing metrics via OpenTelemetry
- FR56: Covered in Epic 7 - Deploy alongside EventStore with DAPR
- FR57: Covered in Epic 7 - Stateless service with event store reconstruction
- FR58: Covered in Epic 1 - CI/CD quality gates
- FR59: Covered in Epic 8 - Quickstart guide under 30 minutes
- FR60: Covered in Epic 8 - Prerequisite validation in quickstart
- FR61: Covered in Epic 8 - Event contract reference documentation
- FR62: Covered in Epic 4 - Sample consuming service
- FR63: Covered in Epic 8 - "Aha moment" demo
- FR64: Covered in Epic 8 - Cross-aggregate timing documentation
- FR65: Covered in Epic 8 - Compensating command patterns documentation

Total FRs in epics: 65

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --------- | --------------- | ------------- | ------ |
| FR1 | A global administrator can create a new tenant with a unique identifier and name (in MVP, tenant creation is restricted to global administrators) | Epic 2 | Covered |
| FR2 | A developer can update a tenant's metadata (name, description) | Epic 2 | Covered |
| FR3 | A global administrator can disable a tenant, preventing all commands against that tenant from succeeding | Epic 2 | Covered |
| FR4 | A global administrator can re-enable a previously disabled tenant, restoring normal command processing | Epic 2 | Covered |
| FR5 | The system produces a domain event for every tenant lifecycle change (created, updated, disabled, enabled) | Epic 2 | Covered |
| FR6 | A tenant owner can add a user to a tenant with a specified role (TenantOwner, TenantContributor, or TenantReader) | Epic 3 | Covered |
| FR7 | A tenant owner can remove a user from a tenant | Epic 3 | Covered |
| FR8 | A tenant owner can change a user's role within a tenant | Epic 3 | Covered |
| FR9 | The system rejects adding a user who is already a member of the tenant | Epic 3 | Covered |
| FR10 | The system rejects role changes that violate escalation boundaries (a tenant owner cannot assign GlobalAdministrator) | Epic 3 | Covered |
| FR11 | The system produces a domain event for every user-role change (added, removed, role changed) | Epic 3 | Covered |
| FR12 | The system enforces optimistic concurrency, rejecting conflicting concurrent modifications to the same aggregate | Epic 3 | Covered |
| FR13 | An existing global administrator can designate a user as a global administrator | Epic 2 | Covered |
| FR14 | An existing global administrator can remove a user's global administrator status (cannot remove self if they are the last global administrator) | Epic 2 | Covered |
| FR15 | A global administrator can perform any tenant operation across all tenants without per-tenant role assignment | Epic 2 | Covered |
| FR16 | All global administrator actions produce auditable domain events | Epic 2 | Covered |
| FR17 | The system provides a bootstrap mechanism (seed command or startup configuration) to create the initial global administrator on first deployment when no global administrators exist | Epic 2 | Covered |
| FR18 | The bootstrap mechanism only executes when zero global administrators exist in the event store - subsequent executions are rejected with a specific error indicating that bootstrap has already been completed | Epic 2 | Covered |
| FR19 | A tenant owner can set a key-value configuration entry for a tenant | Epic 3 | Covered |
| FR20 | A tenant owner can remove a configuration entry from a tenant | Epic 3 | Covered |
| FR21 | Configuration keys support dot-delimited namespace conventions (e.g., `billing.plan`, `parties.maxContacts`) to prevent collisions between consuming services | Epic 3 | Covered |
| FR22 | The system produces a domain event for every configuration change (set, removed) | Epic 3 | Covered |
| FR23 | The system enforces configuration limits: maximum 100 keys per tenant, maximum 1KB per value, maximum 256 characters per key | Epic 3 | Covered |
| FR24 | The system rejects configuration operations that exceed limits with a specific error identifying which limit was exceeded and the current usage | Epic 3 | Covered |
| FR25 | A developer can query a paginated list of all tenants with their IDs, names, and statuses | Epic 5 | Covered |
| FR26 | A developer can query a specific tenant's details including its current users and their roles | Epic 5 | Covered |
| FR27 | A developer can query the list of users in a specific tenant with their assigned roles | Epic 5 | Covered |
| FR28 | A developer can query the list of tenants a specific user belongs to, with their role in each tenant | Epic 5 | Covered |
| FR29 | A global administrator can query tenant access changes by tenant ID and date range for audit reporting, with pagination support (default page size: 100 results, maximum: 1,000) | Epic 5 | Covered |
| FR30 | All list and query endpoints support cursor-based pagination with consistent ordering | Epic 5 | Covered |
| FR31 | A TenantReader can query tenant details, user lists, and configuration for tenants they belong to, but cannot execute any state-changing commands | Epic 3 | Covered |
| FR32 | A TenantContributor has TenantReader capabilities plus the ability to execute domain commands within the tenant (the specific commands are defined by each consuming service) | Epic 3 | Covered |
| FR33 | A TenantOwner has TenantContributor capabilities plus user-role management (add, remove, change role) and tenant configuration management | Epic 3 | Covered |
| FR34 | A user with roles in multiple tenants can only access data and execute commands within each tenant according to their role in that specific tenant - roles do not transfer or aggregate across tenants | Epic 3 | Covered |
| FR35 | The system publishes all tenant domain events via DAPR pub/sub as CloudEvents 1.0 | Epic 2 | Covered |
| FR36 | The system uses a documented topic naming convention for tenant events (e.g., `tenants.events`) consistent with Hexalith ecosystem patterns | Epic 2 | Covered |
| FR37 | A consuming service can subscribe to tenant events and build a local projection of tenant state | Epic 4 | Covered |
| FR38 | A consuming service can react to user addition/removal events to enforce or revoke access | Epic 4 | Covered |
| FR39 | A consuming service can react to tenant disable/enable events to block or allow operations | Epic 4 | Covered |
| FR40 | A consuming service can react to configuration change events to update tenant-specific behavior | Epic 4 | Covered |
| FR41 | Event contracts include sufficient information (event ID, aggregate version) for consuming services to implement idempotent event handling | Epic 4 | Covered |
| FR42 | Documentation provides guidance on idempotent event processing patterns for consumers, since DAPR pub/sub may deliver events more than once. Minimum content: at-least-once delivery explanation, deduplication by event ID example, idempotent handler pattern with code sample | Epic 4 | Covered |
| FR43 | A developer can install Tenants via NuGet packages (Contracts, Client, Server, Testing, Aspire) | Epic 1 | Covered |
| FR44 | A developer can register tenant client services in DI with a single extension method call | Epic 4 | Covered |
| FR45 | A developer can register tenant event handlers in a consuming service in under 20 lines of DI configuration | Epic 4 | Covered |
| FR46 | A developer can write tenant integration tests using in-memory fakes without external infrastructure, in under 10 lines per test | Epic 6 | Covered |
| FR47 | The in-memory testing fakes execute the same domain logic as the production service, guaranteeing isolation at the aggregate domain model level (command validation, event production, state transitions), verified by a conformance test suite that runs identical command sequences against both fakes and production aggregate. Projection-level and query-level isolation is the responsibility of the consuming service's own test suite | Epic 6 | Covered |
| FR48 | A developer can deploy the tenant service using .NET Aspire hosting extensions | Epic 7 | Covered |
| FR49 | The system provides error messages for all command rejections that include: the specific rejection reason, the entity involved, and a corrective action hint | Epic 2 | Covered |
| FR50 | The system rejects commands targeting a non-existent tenant with a specific error identifying the missing tenant | Epic 2 | Covered |
| FR51 | The system rejects commands targeting a disabled tenant with a specific error indicating the tenant's disabled status | Epic 2 | Covered |
| FR52 | The system rejects duplicate operations (e.g., adding an already-present user) with a specific error including current state | Epic 2 | Covered |
| FR53 | Commands and event storage succeed independently of DAPR pub/sub availability (event store is the source of truth) | Epic 2 | Covered |
| FR54 | The system exposes tenant command latency metrics via OpenTelemetry | Epic 7 | Covered |
| FR55 | The system exposes event processing metrics via OpenTelemetry | Epic 7 | Covered |
| FR56 | A platform operator can deploy the tenant service alongside EventStore using standard DAPR configuration | Epic 7 | Covered |
| FR57 | The tenant service is stateless between requests - all state is reconstructed from the event store on startup | Epic 7 | Covered |
| FR58 | The CI/CD pipeline enforces quality gates: build, test (Tier 1+2), coverage threshold (> 80% line, 100% branch on isolation/auth), and package validation before NuGet publish | Epic 1 | Covered |
| FR59 | The project provides a quickstart guide that enables a developer to send their first tenant command within 30 minutes | Epic 8 | Covered |
| FR60 | The quickstart guide includes prerequisite validation (DAPR sidecar, EventStore deployment) | Epic 8 | Covered |
| FR61 | The project provides an event contract reference documenting all commands, events, and their schemas | Epic 8 | Covered |
| FR62 | The project provides a sample consuming service demonstrating event subscription and access enforcement | Epic 4 | Covered |
| FR63 | The project provides an "aha moment" demo (screencast or video) showing reactive cross-service access revocation | Epic 8 | Covered |
| FR64 | The project provides documentation on cross-aggregate timing behavior, including the event propagation window between tenant commands and subscriber processing. Minimum content: timing window explanation, sequence diagram, guidance on designing for eventual consistency, reference to planned auth plugin as synchronous enforcement option | Epic 8 | Covered |
| FR65 | The project provides documentation on compensating command patterns (e.g., restoring a wrongly removed user with explicit role specification). Minimum content: compensating command definition, worked example with AddUserToTenant after incorrect RemoveUserFromTenant, explanation of why role must be explicitly specified (not auto-restored) | Epic 8 | Covered |

### Missing Requirements

No missing FR coverage was found. Every PRD FR1-FR65 appears in the epics FR coverage map.

No FRs were found in the epics coverage map that do not exist in the PRD.

### Coverage Statistics

- Total PRD FRs: 65
- FRs covered in epics: 65
- Coverage percentage: 100%

### Coverage Notes

- Epics 1-8 provide the original complete MVP coverage map.
- Follow-up Epics 9-12 reinforce selected requirements around query operations, projection write safety, production authorization readiness, and Phase 2 Admin UI dependency sequencing.
- This step validates FR presence and traceability only. Story quality, NFR coverage, UX alignment, and architecture alignment are evaluated in later workflow steps.

## Step 4: UX Alignment Assessment

### UX Document Status

Found: `_bmad-output/planning-artifacts/ux-design-specification.md`

The UX document is complete and dated 2026-05-26. It is explicitly framed as the Phase 2 Tenants Admin UI / FrontComposer operational trust surface, not Phase 1 backend MVP scope.

### UX to PRD Alignment

Aligned areas:

- PRD scopes the Admin UI / FrontShell reference module to Phase 2, and the UX spec consistently treats command-capable UI as a later gated slice.
- PRD Journey 5 tenant discovery, Journey 6 operations, and Journey 7 security/audit are directly reflected in the UX Operations Shell, tenant list, access review, audit evidence, user lookup, and incident-response flows.
- PRD FR25-FR30 query requirements support the UX tenant list, tenant detail, tenant users, user tenants, audit, and pagination surfaces.
- PRD FR31-FR34 role behavior requirements support the UX role-aware action availability and unavailable-action explanations.
- PRD FR64 and FR65 are reflected strongly in the UX truth-state model, projection-lag handling, and compensating-command recovery language.
- PRD NFR24 requires Phase 2 Admin UI accessibility and i18n consideration; the UX spec expands this into concrete keyboard, screen-reader, live-region, forced-colors, reduced-motion, localization, and responsive expectations.

Potential PRD alignment issues:

- The UX spec targets WCAG 2.2 AA, while PRD NFR24 says WCAG 2.1 AA. This is a higher standard, but the requirement baseline should be reconciled so implementation stories know which standard is contractual.
- The UX spec describes a lightweight user lookup path and shell navigation item. The PRD/backend query surface supports `GET /api/users/{userId}/tenants`, but does not clearly define broad user search/discovery. If the UX expects searching users by text or browsing all users, that backend capability is not yet represented in the PRD FRs.
- The UX spec requires command lifecycle, projection freshness, SignalR/degraded-update handling, audit proof availability, and support-safe references as first-class interaction states. PRD describes the underlying concepts, but UI-specific acceptance criteria need to remain in Phase 2 stories so these expectations are not treated as already delivered by backend MVP stories.

### UX to Architecture Alignment

Aligned areas:

- Architecture explicitly states Phase 1 has no frontend implementation requirement and Phase 2 UI should use Hexalith.FrontComposer plus Fluent UI Blazor through an adapter-backed composition layer.
- Architecture supports the UX rule that SignalR/projection notifications are refresh nudges, not durable truth.
- Architecture requires separation between projection data and local pending/confirming command hints, matching the UX truth-state and command lifecycle model.
- Architecture names command lifecycle, projection freshness, consequence preview, audit evidence, accessibility, localization, and documentation as explicit readiness gates.
- Architecture includes Epic 12 as Phase 2 UI dependency sequencing, which matches the UX's known FrontComposer dependencies and implementation-story rules.
- Architecture protects domain contracts from UI generation pressure by requiring UI-facing command/projection models and mappings instead of annotating or reshaping immutable Tenants contracts.

Architecture support gaps and warnings:

- Architecture acknowledges that a detailed FrontComposer adapter/module architecture is a future enhancement. This is acceptable for Phase 1, but Phase 2 UI implementation should not start until the adapter boundary is designed.
- Consequence Preview, Command Lifecycle Panel, Audit Evidence Receipt, and reusable Audit Timeline are specified by UX, but architecture treats them as gated or missing dependencies rather than implemented capabilities. UI command stories should stay blocked or fallback-scoped until those dependencies are resolved.
- UX depends on projection freshness markers, command status reconciliation, and audit proof paths. Architecture supports the concepts, but implementation readiness depends on concrete read-model fields, query DTOs, status APIs, or audit references being available and tested.
- UX's broad responsive/accessibility test matrix exceeds the Phase 1 backend architecture. This is not a contradiction, but it must be owned by Phase 2 UI stories and not silently assumed by backend epics.

### Alignment Issues

1. **WCAG baseline mismatch:** PRD says WCAG 2.1 AA; UX says WCAG 2.2 AA. Decide whether Phase 2 UI contract is 2.1 AA, 2.2 AA, or 2.1 AA minimum with 2.2 AA target.

2. **User lookup scope ambiguity:** UX implies user lookup as a navigation/workflow path. Current PRD and architecture clearly support lookup by known user ID through user-tenants query, but not a general user search capability. Clarify whether user lookup is exact-ID only, external-directory backed, or a new backend query requirement.

3. **Phase 2 dependency readiness:** UX command-capable flows require FrontComposer command lifecycle support, consequence preview, audit evidence, localization, accessibility evidence, and potentially SignalR/status reconciliation. Architecture correctly gates these, but Phase 2 stories must retain explicit `blockedBy` dependencies.

### Warnings

- No missing UX documentation warning is needed; UX documentation exists and is substantial.
- Do not promote UX command-capable flows into Phase 1. PRD, epics, UX, and architecture all treat Admin UI as Phase 2 unless explicitly re-scoped.
- Read-only UI surfaces appear closer to readiness than command-capable surfaces. Command-capable stories should fail closed unless freshness, authorization, lifecycle, consequence, audit, accessibility, and localization behaviors are explicitly specified.
- Backend implementation stories should avoid reshaping contracts for UI generation; use the architecture's adapter/UI-facing model boundary.

## Step 5: Epic Quality Review

### Review Scope

Reviewed `_bmad-output/planning-artifacts/epics.md` against create-epics-and-stories quality standards:

- Epics must deliver user value, not only technical milestones.
- Epics must be independently useful in sequence.
- Stories must be independently completable without relying on future stories.
- Acceptance criteria must be specific, testable, and complete.
- Starter/foundation stories must match the architecture's starter decision.
- Upfront entity/model creation should be avoided unless justified by an early user-facing slice.

### Critical Violations

#### 1. Aggregate Name Inconsistency: `GlobalAdministratorAggregate` vs `GlobalAdministratorsAggregate`

Affected examples:

- Epic additional architecture requirements reference `GlobalAdministratorAggregate`.
- Story 1.2 says `actors.yaml` configures `TenantAggregate` and `GlobalAdministratorAggregate` actor types.
- Story 2.2 title/body/blueprint use `GlobalAdministratorAggregate`.

Expected:

- Current architecture/project context uses `GlobalAdministratorsAggregate` with plural "Administrators".

Impact:

- This is not a cosmetic issue. Aggregate names affect actor type names, EventStore reflection discovery, DAPR actor configuration, tests, and implementation handoff. A singular/plural mismatch can lead to silent no-registration or command routing failures.

Recommendation:

- Normalize all planning artifacts and story references to `GlobalAdministratorsAggregate` and `GlobalAdministratorsState`.
- Add a story readiness check that rejects singular `GlobalAdministratorAggregate` references before implementation.

#### 2. DAPR Topic Naming Conflict: `system.tenants.events` vs `tenants.events`

Affected examples:

- Epics additional requirements and Story 2.4 reference DAPR pub/sub topic `system.tenants.events`.

Expected:

- Current project context and architecture pattern section specify topic `tenants.events` and dead letter `deadletter.tenants.events`.

Impact:

- Topic name mismatch breaks producer/consumer subscription alignment and invalidates sample service/event documentation expectations.

Recommendation:

- Resolve the canonical topic name once. Based on current project context, update epics/story ACs to `tenants.events`.
- If `system.tenants.events` is intentionally retained, update project context, architecture, code, DAPR components, and docs together.

#### 3. Story 2.4 Is Epic-Sized

Story 2.4 includes command API, bootstrap hosted service, multi-instance behavior, DAPR pub/sub publication and recovery, ProblemDetails mapping, authentication, aggregate auto-discovery, `/process` domain dispatch, and Tier 2 end-to-end verification.

Impact:

- This violates story sizing standards. It is too broad to estimate, review, test, or complete as one independent story.

Mitigation already present:

- The epics document contains a post-readiness correction splitting Story 2.4 into five logical work packages: 2.4A through 2.4E.

Recommendation:

- Treat Story 2.4 as historical only.
- For future sprint execution or rework, use the split work packages as separate implementation stories.

### Major Issues

#### 1. Story 2.1 Creates Future-Epic Contracts Up Front

Story 2.1 requires all 12 command records and all 11 event records, including membership and configuration commands/events that are not behaviorally implemented until Epic 3.

Impact:

- This is the contract equivalent of "create all tables/models upfront." It front-loads schema surface before its vertical behavior exists.
- It increases the risk of stale contract types, incomplete rejection mapping, and semantic-release/public API churn before behavior is proven.

Recommendation:

- Prefer vertical contract creation by first behavioral story, or explicitly mark Story 2.1 as a public-contract scaffolding exception with tests preventing unused/incomplete contracts from shipping accidentally.

#### 2. Story 2.3 Creates Future Apply Methods Before Epic 3 Behavior

Story 2.3 blueprint says `TenantState` includes Apply methods for user-role and configuration events, while corresponding Handle methods are implemented later in Epic 3.

Impact:

- This creates future-facing state behavior before the story's user-value slice needs it.
- It weakens story independence and can hide incomplete event semantics.

Recommendation:

- Move user-role/configuration Apply methods into the first story that introduces the related events, or explicitly document why a complete state skeleton is required and test it without pretending the feature is complete.

#### 3. Story 6.1 References Projection Isolation Before Story 6.2 Introduces In-Memory Projection

Story 6.1 acceptance criteria say projections for tenant A never contain data from tenant B, but Story 6.2 introduces `InMemoryTenantProjection`.

Impact:

- This is a within-epic forward dependency or wording error.

Recommendation:

- Change Story 6.1 to assert aggregate/event-level isolation only.
- Move projection-level isolation acceptance criteria to Story 6.2.

#### 4. Story 10.3B Is Explicitly Blocked by External EventStore API Work

Story 10.3B depends on Story 10.3A and on cancellation-aware EventStore projection APIs being merged or otherwise available.

Impact:

- Story 10.3B is not independently implementation-ready in the Tenants repo until the EventStore prerequisite is complete and pinned by submodule commit/version.

Recommendation:

- Keep Story 10.3B blocked.
- Do not assign it to implementation until 10.3A names the exact EventStore APIs and available submodule commit.

#### 5. Epic 12 Is Planning/Dependency Governance, Not Product Implementation

Epic 12 correctly states it is Phase 2 planning/readiness and dependency governance, not Phase 1 backend scope.

Impact:

- If treated as an implementation epic, it violates the user-value rule because its stories produce dependency maps and backlog readiness rather than shippable Admin UI behavior.

Recommendation:

- Keep Epic 12 in planning artifacts only.
- Convert its outputs into concrete Phase 2 UI implementation stories with explicit `blockedBy` entries before assigning work.

#### 6. Story 1.1 Acceptance Criteria Preserve Outdated EventStore Style Assumptions

Story 1.1 says `.editorconfig` enforces EventStore conventions including Allman braces.

Expected:

- Current Tenants project context says Tenants uses K&R brace style and agents must not "fix" it to Allman.

Impact:

- A developer following Story 1.1 literally could introduce style churn and violate current repository conventions.

Recommendation:

- Update Story 1.1 and architecture text to say Tenants follows its current `.editorconfig`, with K&R brace style where applicable.

### Minor Concerns

#### 1. Story 7.1 Contains Duplicate Wording

Acceptance criterion says "the Aspire dashboard launches and the Aspire dashboard launches".

Recommendation:

- Clean up wording before story assignment.

#### 2. Story 1.3 Description Mentions Tagged Releases While AC Uses Semantic-Release on Main

The story text says tagged releases publish packages, while the acceptance criteria correctly describe semantic-release after merge to `main`.

Recommendation:

- Update the story description to match current release behavior.

#### 3. Story 9.5 Shared Pagination Utilities Appears After Cursor Stories

Stories 9.1 and 9.2 introduce cursor signing/stability before Story 9.5 centralizes pagination/cursor utilities.

Impact:

- This is not a hard forward dependency, but the sequencing may create avoidable rework.

Recommendation:

- Consider moving shared cursor utility work before or into Story 9.1.

### Epic Compliance Checklist

| Epic | User Value | Independent in Sequence | Story Sizing | No Forward Dependencies | AC Quality | Notes |
| ---- | ---------- | ----------------------- | ------------ | ----------------------- | ---------- | ----- |
| Epic 1 Foundation | Pass with greenfield exception | Pass | Mostly pass | Pass | Mixed | Technical but necessary; update style/release wording. |
| Epic 2 Core Tenant Management | Pass | Pass after Epic 1 | Mixed | Mixed | Mixed | Story 2.1 upfront contracts, Story 2.3 future Apply methods, Story 2.4 too broad, naming/topic conflicts. |
| Epic 3 Membership/Roles/Config | Pass | Pass after Epic 2 | Pass | Pass | Pass | Strong user-value framing. |
| Epic 4 Event Integration | Pass | Pass after Epics 1-3 | Pass | Pass | Pass | Consumer value clear. |
| Epic 5 Tenant Query | Pass | Pass after event/projection foundations | Pass | Pass | Pass | Query endpoints and projections are cohesive. |
| Epic 6 Testing Package | Pass | Pass after domain behavior exists | Mixed | Mixed | Mostly pass | Projection isolation criterion belongs in Story 6.2. |
| Epic 7 Deployment/Observability | Pass | Pass after host/service exists | Pass | Pass | Mostly pass | Minor wording cleanup in Story 7.1. |
| Epic 8 Docs/Adoption | Pass | Pass after feature evidence exists | Pass | Pass | Pass | Strong adoption value. |
| Epic 9 Query Hardening | Pass | Pass after query endpoints exist | Pass | Pass | Pass | Consider moving shared cursor utilities earlier. |
| Epic 10 Projection Write Safety | Pass | Pass for 10.1/10.2/10.4 | Mixed | Pass | Pass | 10.3B blocked on external EventStore API prerequisite. |
| Epic 11 Production Auth | Pass | Pass after deployment/auth base exists | Pass | Pass | Pass | Good platform-operator value. |
| Epic 12 UI Dependency Sequencing | Planning value only | Not product implementation | Planning-sized | Explicitly dependency-focused | Pass for planning | Do not treat as implementation-ready UI behavior. |

### Dependency Analysis

No circular epic dependencies were found.

No Epic N was found to require Epic N+1 for its primary value to function, with these exceptions/qualifications:

- Story 2.1 front-loads contracts for Epic 3 behaviors.
- Story 2.3 front-loads state Apply methods for Epic 3 behaviors.
- Story 6.1 includes a projection-level isolation criterion that belongs after the projection story.
- Story 10.3B is correctly blocked by 10.3A and external EventStore work, so it should remain unavailable for implementation until the prerequisite is complete.
- Epic 12 is intentionally planning/dependency governance and should not be assessed as product implementation readiness.

Database/entity creation timing:

- No direct database table creation exists because the architecture uses EventStore and DAPR abstractions.
- The same anti-pattern appears in contract/state timing: Story 2.1 and Story 2.3 create future surface before the corresponding user-value stories.

Starter template requirement:

- Architecture selects manual mirroring of the Hexalith.EventStore structure rather than running a starter CLI.
- Epic 1 Story 1.1 satisfies the greenfield foundation requirement in substance, but should explicitly mention the EventStore structure mirror and remove outdated Allman brace wording.

### Recommendations

1. Fix naming and DAPR topic conflicts before any further implementation handoff.
2. Split or preserve the existing split of Story 2.4 into separately assignable implementation stories.
3. Update Story 1.1 to current Tenants style/release rules.
4. Remove future-facing contract/state work from Story 2.1/2.3 or document them as intentional scaffolding exceptions with protective tests.
5. Move projection-level testing criteria from Story 6.1 to Story 6.2.
6. Keep Story 10.3B blocked until EventStore prerequisite APIs are available and pinned.
7. Keep Epic 12 as planning output until converted into concrete Phase 2 UI implementation stories.

## Summary and Recommendations

### Overall Readiness Status

NEEDS WORK

The planning set is strong and largely traceable, but it is not ready for unqualified implementation handoff. The PRD, architecture, epics, and UX documents are all present. PRD FR coverage is complete: 65 of 65 FRs are mapped into epics. The problem is not missing requirements; the problem is artifact consistency and story readiness.

Implementation can proceed only after the critical naming/topic conflicts are corrected and blocked or oversized stories are split, scoped, or explicitly marked as non-ready.

### Critical Issues Requiring Immediate Action

1. Aggregate naming conflict: story artifacts use `GlobalAdministratorAggregate`, while current architecture/project context requires `GlobalAdministratorsAggregate`.

2. DAPR topic conflict: epics use `system.tenants.events`, while current project context/architecture use `tenants.events`.

3. Story 2.4 is too large for implementation as a single story. The post-readiness split into 2.4A-2.4E must be used for future work.

### Major Issues Requiring Correction

1. Story 2.1 creates future Epic 3 contracts before the related behavior exists.

2. Story 2.3 creates future user/config Apply methods before Epic 3 behavior.

3. Story 6.1 includes projection-level isolation before Story 6.2 introduces the in-memory projection.

4. Story 10.3B is blocked by EventStore API prerequisite work and is not implementation-ready.

5. Epic 12 is planning/dependency governance, not a product implementation epic.

6. Story 1.1 preserves outdated EventStore/Allman style assumptions that conflict with current Tenants K&R style guidance.

### UX Alignment Issues Requiring Clarification

1. Decide whether Phase 2 UI targets WCAG 2.1 AA, WCAG 2.2 AA, or 2.1 AA minimum with 2.2 AA target.

2. Clarify whether UX user lookup means exact user ID lookup only or a broader user search/discovery capability requiring new backend requirements.

3. Keep command-capable UI stories blocked until FrontComposer command lifecycle, consequence preview, audit evidence, accessibility, localization, and status reconciliation dependencies are explicitly available or approved as fallbacks.

### Recommended Next Steps

1. Normalize all aggregate references to `GlobalAdministratorsAggregate` / `GlobalAdministratorsState`.

2. Normalize all DAPR topic references to the canonical topic name, currently `tenants.events` per project context.

3. Rewrite Story 2.4 into separately assignable stories 2.4A-2.4E before any future sprint use.

4. Update Story 1.1 to current repository conventions: Tenants `.editorconfig`, K&R brace style, `.slnx`, central package management, and semantic-release on merge to `main`.

5. Decide whether Story 2.1/2.3 remain intentional scaffolding exceptions or should be re-sliced vertically by behavior.

6. Move Story 6.1 projection-level isolation criteria to Story 6.2.

7. Keep Story 10.3B blocked until EventStore cancellation-aware APIs are merged and the submodule commit/version is named.

8. Keep Epic 12 out of implementation scheduling until it is converted into concrete Phase 2 UI stories with explicit `blockedBy` dependencies.

9. Re-run implementation readiness after artifact corrections, focusing on the changed epics/stories rather than re-validating every unchanged FR.

### Final Note

This assessment identified 15 issues or concerns across document inventory, PRD extraction, FR coverage, UX alignment, and epic/story quality:

- 3 critical issues
- 6 major issues
- 3 minor concerns
- 3 UX alignment clarifications

The artifacts are close to usable because requirement coverage is complete and the architecture is detailed. Address the critical and major issues before proceeding with implementation handoff. Proceeding as-is would create avoidable implementation ambiguity around aggregate discovery, event topics, style rules, story sizing, and Phase 2 UI readiness.

**Assessor:** Codex using `bmad-check-implementation-readiness`
**Assessment Date:** 2026-05-26
