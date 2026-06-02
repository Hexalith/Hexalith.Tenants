---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
includedDocuments:
  prd: _bmad-output/planning-artifacts/prd.md
  architecture: _bmad-output/planning-artifacts/architecture.md
  epics: _bmad-output/planning-artifacts/epics.md
  ux: _bmad-output/planning-artifacts/ux-design-specification.md
supportingDocuments:
  - _bmad-output/planning-artifacts/prd-validation-report.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-06-02
**Project:** Tenants

## Document Discovery

### PRD Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/prd.md` (58,514 bytes, modified 2026-06-02 09:51) - selected as PRD source
- `_bmad-output/planning-artifacts/prd-validation-report.md` (24,841 bytes, modified 2026-05-31 20:13) - supporting context only

**Sharded Documents:**
- None found

### Architecture Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/architecture.md` (45,849 bytes, modified 2026-06-02 09:51) - selected as architecture source

**Sharded Documents:**
- None found

### Epics & Stories Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/epics.md` (126,514 bytes, modified 2026-06-02 11:51) - selected as epics source

**Sharded Documents:**
- None found

### UX Design Files Found

**Whole Documents:**
- `_bmad-output/planning-artifacts/ux-design-specification.md` (82,664 bytes, modified 2026-05-31 20:13) - selected as UX source

**Sharded Documents:**
- None found

### Discovery Issues

- No whole + sharded duplicate document formats found.
- `prd-validation-report.md` matched the PRD filename search but is a validation report, not the selected PRD source.

## PRD Analysis

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

**Total FRs:** 65

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

NFR17: The system degrades gracefully when DAPR pub/sub is unavailable - commands and event storage succeed because EventStore is the source of truth, drain recovery republishes persisted events when pub/sub recovers, and subscriber or projection catch-up is verified by live assertions when available or by documented idempotency/catch-up evidence when live subscriber proof is not present

NFR18: Event contracts are backward-compatible after v1.0 - no breaking schema changes to published events

NFR19: All domain events include event ID and aggregate version to enable idempotent processing by consumers

NFR20: The event store is the single source of truth - system state can be fully reconstructed by replaying events

NFR21: Command processing and event storage are atomic - a command either fully succeeds or fully fails

NFR22: API availability target: 99.9% in production deployments, as measured by health check endpoint uptime monitoring

NFR23: No data loss under any failure scenario - events once stored are immutable and durable

NFR24: MVP error messages and documentation are English-only. Phase 2 Admin UI accessibility baseline is WCAG 2.1 AA, with WCAG 2.2 AA as the design and implementation target where supported by the selected Fluent UI Blazor and FrontComposer stack. Phase 2 UI must address i18n considerations as part of its requirements scoping

**Total NFRs:** 24

### Additional Requirements

- Phase 1 is explicitly a backend/package/documentation MVP. Tenant Admin UI / FrontShell reference module is Phase 2 unless promoted by a future scope decision.
- Event contract stability is a v1.0 milestone; pre-v1.0 event contracts may evolve with breaking changes.
- Tenant deletion is out of scope for all phases; tenants may be disabled but not deleted because event history must remain immutable.
- gRPC API surface is out of scope for all phases; command API uses REST endpoints only.
- Phase 2 includes the EventStore tenant authorization plugin, Keycloak JWT projection sync, Admin UI / FrontShell reference module, custom/extensible roles, bulk tenant provisioning, and F# consumption support.
- Phase 3 includes hierarchical sub-tenants, multi-deployment tenant migration by event replay, per-tenant service registry, cross-deployment federation, and advanced snapshot optimization.
- The product targets C# on .NET 10 LTS with nullable references and implicit usings enabled; F# support is future-facing.
- The solution must publish five NuGet packages: `Hexalith.Tenants.Contracts`, `Hexalith.Tenants.Client`, `Hexalith.Tenants.Server`, `Hexalith.Tenants.Testing`, and `Hexalith.Tenants.Aspire`.
- Package quality standards include Source Link, deterministic builds, semantic-release, centralized package management, expected package count validation, and package-only consumer restore/build validation.
- Solution structure is fixed around `src/`, `tests/`, and `samples/` projects, with `Hexalith.Tenants.slnx` as the solution file.
- API surface includes REST command endpoints, CloudEvents 1.0 event contracts through DAPR pub/sub, read model queries, and minimal client DI registration.
- Testing architecture uses Tier 1 unit tests, Tier 2 DAPR integration tests, and Tier 3 Aspire E2E contract tests.
- Documentation requirements include README/quickstart, event contract reference, docs folder, CHANGELOG, CONTRIBUTING, inline C# code samples, documentation validation, eventual consistency and event ordering guide, cross-aggregate timing documentation, and compensating command pattern guidance.
- Adoption assets include a sample consuming service and a 90-second "aha moment" demo showing reactive cross-service access revocation and audit history.

### PRD Completeness Assessment

The PRD is broad and well structured for implementation readiness analysis: it contains 65 numbered functional requirements, 24 numbered non-functional requirements, explicit phase boundaries, explicit out-of-scope decisions, measurable success criteria, user journeys, technical constraints, packaging expectations, API surface expectations, testing architecture, and documentation/adoption requirements.

Initial risks for downstream validation:

- Some FRs are documentation/adoption deliverables rather than product behavior and must be traced to epics and stories, not only to code tasks.
- Several NFRs require specific verification evidence, especially NFR5, NFR10, NFR11, NFR13, NFR17, NFR22, and NFR23.
- Phase 2 Admin UI requirements are intentionally deferred but referenced by UX design, so epics must avoid accidentally pulling UI scope into the Phase 1 MVP unless explicitly promoted.
- The PRD contains both backend MVP requirements and future roadmap items; epic validation must distinguish committed MVP work from post-MVP scope.

## Epic Coverage Validation

### Epic FR Coverage Extracted

FR1: Covered in Epic 1 - Tenant lifecycle creation governance

FR2: Covered in Epic 1 - Tenant metadata update governance

FR3: Covered in Epic 1 - Tenant disable governance

FR4: Covered in Epic 1 - Tenant re-enable governance

FR5: Covered in Epic 1 - Tenant lifecycle domain event production

FR6: Covered in Epic 2 - Tenant member addition

FR7: Covered in Epic 2 - Tenant member removal

FR8: Covered in Epic 2 - Tenant role change

FR9: Covered in Epic 2 - Duplicate tenant membership rejection

FR10: Covered in Epic 2 - Role escalation rejection

FR11: Covered in Epic 2 - User-role domain event production

FR12: Covered in Epic 2 - Optimistic concurrency for tenant aggregate modifications

FR13: Covered in Epic 1 - Global administrator designation

FR14: Covered in Epic 1 - Global administrator removal safety

FR15: Covered in Epic 1 - Cross-tenant global administrator authority

FR16: Covered in Epic 1 - Global administrator audit events

FR17: Covered in Epic 1 - Initial global administrator bootstrap

FR18: Covered in Epic 1 - Bootstrap single-use protection

FR19: Covered in Epic 2 - Tenant configuration set

FR20: Covered in Epic 2 - Tenant configuration removal

FR21: Covered in Epic 2 - Configuration key namespace convention

FR22: Covered in Epic 2 - Configuration domain event production

FR23: Covered in Epic 2 - Configuration count/key/value limits

FR24: Covered in Epic 2 - Configuration limit rejection detail

FR25: Covered in Epic 3 - Paginated tenant list query

FR26: Covered in Epic 3 - Tenant detail query

FR27: Covered in Epic 3 - Tenant users query

FR28: Covered in Epic 3 - User tenants query

FR29: Covered in Epic 3 - Tenant audit query by tenant and date range

FR30: Covered in Epic 3 - Cursor-based pagination and consistent ordering

FR31: Covered in Epic 2 - TenantReader read-only behavior

FR32: Covered in Epic 2 - TenantContributor role behavior

FR33: Covered in Epic 2 - TenantOwner role behavior

FR34: Covered in Epic 2 - Tenant-scoped role isolation

FR35: Covered in Epic 4 - DAPR pub/sub tenant event publication

FR36: Covered in Epic 4 - Tenant event topic naming

FR37: Covered in Epic 4 - Consumer local tenant projection

FR38: Covered in Epic 4 - Consumer reaction to user addition/removal

FR39: Covered in Epic 4 - Consumer reaction to tenant disable/enable

FR40: Covered in Epic 4 - Consumer reaction to configuration change

FR41: Covered in Epic 4 - Event metadata for idempotent consumer handling

FR42: Covered in Epic 4 - Idempotent event processing documentation

FR43: Covered in Epic 5 - NuGet package installation

FR44: Covered in Epic 5 - Tenant client DI registration

FR45: Covered in Epic 4 - Consumer event handler registration

FR46: Covered in Epic 5 - In-memory fake tenant integration tests

FR47: Covered in Epic 5 - Production/fake domain logic conformance

FR48: Covered in Epic 5 - Aspire hosting extension deployment

FR49: Covered in Epic 2 - Actionable domain rejection messages

FR50: Covered in Epic 2 - Non-existent tenant command rejection

FR51: Covered in Epic 2 - Disabled tenant command rejection

FR52: Covered in Epic 2 - Duplicate operation rejection

FR53: Covered in Epic 2 - Commands and event storage independent of pub/sub availability

FR54: Covered in Epic 6 - Tenant command latency metrics

FR55: Covered in Epic 6 - Event processing metrics

FR56: Covered in Epic 6 - DAPR deployment alongside EventStore

FR57: Covered in Epic 6 - Stateless tenant service operation

FR58: Covered in Epic 6 - CI/CD quality gates and package validation

FR59: Covered in Epic 7 - Quickstart first-command documentation

FR60: Covered in Epic 7 - Quickstart prerequisite validation

FR61: Covered in Epic 7 - Event contract reference

FR62: Covered in Epic 4 - Sample consuming service for event subscription and access enforcement

FR63: Covered in Epic 7 - Reactive access revocation demo

FR64: Covered in Epic 7 - Cross-aggregate timing documentation

FR65: Covered in Epic 7 - Compensating command pattern documentation

**Total FRs in epics:** 65

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --------- | --------------- | ------------- | ------ |
| FR1 | A global administrator can create a new tenant with a unique identifier and name (in MVP, tenant creation is restricted to global administrators) | Epic 1 - Tenant lifecycle creation governance | Covered |
| FR2 | A developer can update a tenant's metadata (name, description) | Epic 1 - Tenant metadata update governance | Covered |
| FR3 | A global administrator can disable a tenant, preventing all commands against that tenant from succeeding | Epic 1 - Tenant disable governance | Covered |
| FR4 | A global administrator can re-enable a previously disabled tenant, restoring normal command processing | Epic 1 - Tenant re-enable governance | Covered |
| FR5 | The system produces a domain event for every tenant lifecycle change (created, updated, disabled, enabled) | Epic 1 - Tenant lifecycle domain event production | Covered |
| FR6 | A tenant owner can add a user to a tenant with a specified role (TenantOwner, TenantContributor, or TenantReader) | Epic 2 - Tenant member addition | Covered |
| FR7 | A tenant owner can remove a user from a tenant | Epic 2 - Tenant member removal | Covered |
| FR8 | A tenant owner can change a user's role within a tenant | Epic 2 - Tenant role change | Covered |
| FR9 | The system rejects adding a user who is already a member of the tenant | Epic 2 - Duplicate tenant membership rejection | Covered |
| FR10 | The system rejects role changes that violate escalation boundaries (a tenant owner cannot assign GlobalAdministrator) | Epic 2 - Role escalation rejection | Covered |
| FR11 | The system produces a domain event for every user-role change (added, removed, role changed) | Epic 2 - User-role domain event production | Covered |
| FR12 | The system enforces optimistic concurrency, rejecting conflicting concurrent modifications to the same aggregate | Epic 2 - Optimistic concurrency for tenant aggregate modifications | Covered |
| FR13 | An existing global administrator can designate a user as a global administrator | Epic 1 - Global administrator designation | Covered |
| FR14 | An existing global administrator can remove a user's global administrator status (cannot remove self if they are the last global administrator) | Epic 1 - Global administrator removal safety | Covered |
| FR15 | A global administrator can perform any tenant operation across all tenants without per-tenant role assignment | Epic 1 - Cross-tenant global administrator authority | Covered |
| FR16 | All global administrator actions produce auditable domain events | Epic 1 - Global administrator audit events | Covered |
| FR17 | The system provides a bootstrap mechanism (seed command or startup configuration) to create the initial global administrator on first deployment when no global administrators exist | Epic 1 - Initial global administrator bootstrap | Covered |
| FR18 | The bootstrap mechanism only executes when zero global administrators exist in the event store - subsequent executions are rejected with a specific error indicating that bootstrap has already been completed | Epic 1 - Bootstrap single-use protection | Covered |
| FR19 | A tenant owner can set a key-value configuration entry for a tenant | Epic 2 - Tenant configuration set | Covered |
| FR20 | A tenant owner can remove a configuration entry from a tenant | Epic 2 - Tenant configuration removal | Covered |
| FR21 | Configuration keys support dot-delimited namespace conventions (e.g., `billing.plan`, `parties.maxContacts`) to prevent collisions between consuming services | Epic 2 - Configuration key namespace convention | Covered |
| FR22 | The system produces a domain event for every configuration change (set, removed) | Epic 2 - Configuration domain event production | Covered |
| FR23 | The system enforces configuration limits: maximum 100 keys per tenant, maximum 1KB per value, maximum 256 characters per key | Epic 2 - Configuration count/key/value limits | Covered |
| FR24 | The system rejects configuration operations that exceed limits with a specific error identifying which limit was exceeded and the current usage | Epic 2 - Configuration limit rejection detail | Covered |
| FR25 | A developer can query a paginated list of all tenants with their IDs, names, and statuses | Epic 3 - Paginated tenant list query | Covered |
| FR26 | A developer can query a specific tenant's details including its current users and their roles | Epic 3 - Tenant detail query | Covered |
| FR27 | A developer can query the list of users in a specific tenant with their assigned roles | Epic 3 - Tenant users query | Covered |
| FR28 | A developer can query the list of tenants a specific user belongs to, with their role in each tenant | Epic 3 - User tenants query | Covered |
| FR29 | A global administrator can query tenant access changes by tenant ID and date range for audit reporting, with pagination support (default page size: 100 results, maximum: 1,000) | Epic 3 - Tenant audit query by tenant and date range | Covered |
| FR30 | All list and query endpoints support cursor-based pagination with consistent ordering | Epic 3 - Cursor-based pagination and consistent ordering | Covered |
| FR31 | A TenantReader can query tenant details, user lists, and configuration for tenants they belong to, but cannot execute any state-changing commands | Epic 2 - TenantReader read-only behavior | Covered |
| FR32 | A TenantContributor has TenantReader capabilities plus the ability to execute domain commands within the tenant (the specific commands are defined by each consuming service) | Epic 2 - TenantContributor role behavior | Covered |
| FR33 | A TenantOwner has TenantContributor capabilities plus user-role management (add, remove, change role) and tenant configuration management | Epic 2 - TenantOwner role behavior | Covered |
| FR34 | A user with roles in multiple tenants can only access data and execute commands within each tenant according to their role in that specific tenant - roles do not transfer or aggregate across tenants | Epic 2 - Tenant-scoped role isolation | Covered |
| FR35 | The system publishes all tenant domain events via DAPR pub/sub as CloudEvents 1.0 | Epic 4 - DAPR pub/sub tenant event publication | Covered |
| FR36 | The system uses a documented topic naming convention for tenant events (e.g., `tenants.events`) consistent with Hexalith ecosystem patterns | Epic 4 - Tenant event topic naming | Covered |
| FR37 | A consuming service can subscribe to tenant events and build a local projection of tenant state | Epic 4 - Consumer local tenant projection | Covered |
| FR38 | A consuming service can react to user addition/removal events to enforce or revoke access | Epic 4 - Consumer reaction to user addition/removal | Covered |
| FR39 | A consuming service can react to tenant disable/enable events to block or allow operations | Epic 4 - Consumer reaction to tenant disable/enable | Covered |
| FR40 | A consuming service can react to configuration change events to update tenant-specific behavior | Epic 4 - Consumer reaction to configuration change | Covered |
| FR41 | Event contracts include sufficient information (event ID, aggregate version) for consuming services to implement idempotent event handling | Epic 4 - Event metadata for idempotent consumer handling | Covered |
| FR42 | Documentation provides guidance on idempotent event processing patterns for consumers, since DAPR pub/sub may deliver events more than once. Minimum content: at-least-once delivery explanation, deduplication by event ID example, idempotent handler pattern with code sample | Epic 4 - Idempotent event processing documentation | Covered |
| FR43 | A developer can install Tenants via NuGet packages (Contracts, Client, Server, Testing, Aspire) | Epic 5 - NuGet package installation | Covered |
| FR44 | A developer can register tenant client services in DI with a single extension method call | Epic 5 - Tenant client DI registration | Covered |
| FR45 | A developer can register tenant event handlers in a consuming service in under 20 lines of DI configuration | Epic 4 - Consumer event handler registration | Covered |
| FR46 | A developer can write tenant integration tests using in-memory fakes without external infrastructure, in under 10 lines per test | Epic 5 - In-memory fake tenant integration tests | Covered |
| FR47 | The in-memory testing fakes execute the same domain logic as the production service, guaranteeing isolation at the aggregate domain model level (command validation, event production, state transitions), verified by a conformance test suite that runs identical command sequences against both fakes and production aggregate. Projection-level and query-level isolation is the responsibility of the consuming service's own test suite | Epic 5 - Production/fake domain logic conformance | Covered |
| FR48 | A developer can deploy the tenant service using .NET Aspire hosting extensions | Epic 5 - Aspire hosting extension deployment | Covered |
| FR49 | The system provides error messages for all command rejections that include: the specific rejection reason, the entity involved, and a corrective action hint | Epic 2 - Actionable domain rejection messages | Covered |
| FR50 | The system rejects commands targeting a non-existent tenant with a specific error identifying the missing tenant | Epic 2 - Non-existent tenant command rejection | Covered |
| FR51 | The system rejects commands targeting a disabled tenant with a specific error indicating the tenant's disabled status | Epic 2 - Disabled tenant command rejection | Covered |
| FR52 | The system rejects duplicate operations (e.g., adding an already-present user) with a specific error including current state | Epic 2 - Duplicate operation rejection | Covered |
| FR53 | Commands and event storage succeed independently of DAPR pub/sub availability (event store is the source of truth) | Epic 2 - Commands and event storage independent of pub/sub availability | Covered |
| FR54 | The system exposes tenant command latency metrics via OpenTelemetry | Epic 6 - Tenant command latency metrics | Covered |
| FR55 | The system exposes event processing metrics via OpenTelemetry | Epic 6 - Event processing metrics | Covered |
| FR56 | A platform operator can deploy the tenant service alongside EventStore using standard DAPR configuration | Epic 6 - DAPR deployment alongside EventStore | Covered |
| FR57 | The tenant service is stateless between requests - all state is reconstructed from the event store on startup | Epic 6 - Stateless tenant service operation | Covered |
| FR58 | The CI/CD pipeline enforces quality gates: build, test (Tier 1+2), coverage threshold (> 80% line, 100% branch on isolation/auth), and package validation before NuGet publish | Epic 6 - CI/CD quality gates and package validation | Covered |
| FR59 | The project provides a quickstart guide that enables a developer to send their first tenant command within 30 minutes | Epic 7 - Quickstart first-command documentation | Covered |
| FR60 | The quickstart guide includes prerequisite validation (DAPR sidecar, EventStore deployment) | Epic 7 - Quickstart prerequisite validation | Covered |
| FR61 | The project provides an event contract reference documenting all commands, events, and their schemas | Epic 7 - Event contract reference | Covered |
| FR62 | The project provides a sample consuming service demonstrating event subscription and access enforcement | Epic 4 - Sample consuming service for event subscription and access enforcement | Covered |
| FR63 | The project provides an "aha moment" demo (screencast or video) showing reactive cross-service access revocation | Epic 7 - Reactive access revocation demo | Covered |
| FR64 | The project provides documentation on cross-aggregate timing behavior, including the event propagation window between tenant commands and subscriber processing. Minimum content: timing window explanation, sequence diagram, guidance on designing for eventual consistency, reference to planned auth plugin as synchronous enforcement option | Epic 7 - Cross-aggregate timing documentation | Covered |
| FR65 | The project provides documentation on compensating command patterns (e.g., restoring a wrongly removed user with explicit role specification). Minimum content: compensating command definition, worked example with AddUserToTenant after incorrect RemoveUserFromTenant, explanation of why role must be explicitly specified (not auto-restored) | Epic 7 - Compensating command pattern documentation | Covered |

### Missing Requirements

No missing PRD functional requirement coverage found.

No FRs were found in the epics coverage map that are absent from the PRD FR list.

### Coverage Statistics

- Total PRD FRs: 65
- FRs covered in epics: 65
- Missing FRs: 0
- Extra FRs in epics not present in PRD: 0
- Coverage percentage: 100%

## UX Alignment Assessment

### UX Document Status

Found: `_bmad-output/planning-artifacts/ux-design-specification.md`

The UX documentation is complete and explicitly scoped as a Phase 2 operational Admin UI plan. It defines an Operations Shell, tenant triage list, tenant/member detail flows, projection freshness, command lifecycle feedback, consequence previews, audit evidence, accessibility, localization, responsive behavior, and `RemoveUserFromTenant` as the first command-capable slice.

### UX to PRD Alignment

Aligned:

- PRD states Phase 1 remains backend/package/documentation MVP and defers Tenants Admin UI / FrontShell reference module to Phase 2 unless explicitly promoted.
- PRD NFR24 requires Phase 2 Admin UI accessibility and i18n scoping; UX expands this into WCAG 2.1 AA baseline, WCAG 2.2 AA target where supported, localization constraints, keyboard/focus/live-region behavior, forced-colors, reduced-motion, and responsive evidence.
- PRD names the Admin UI / FrontShell reference module as a Phase 2 growth item guided by `ux-design-specification.md`; UX follows that boundary and does not promote UI to Phase 1 implementation.
- PRD user journeys for tenant discovery, security management, audit, compensating commands, and event-propagation timing are reflected in UX journeys for tenant triage, access review, remove-user flow, audit evidence, and compensating recovery.
- UX avoids claiming downstream session revocation or token invalidation without backend evidence, which aligns with the PRD's documented eventual-consistency and cross-aggregate timing model.

UX requirements not directly expressed as PRD FRs:

- UX-DR1 through UX-DR48 go beyond PRD NFR24 by specifying detailed UI state, command lifecycle, layout, accessibility, localization, and evidence patterns.
- These additional UX requirements are captured in the epics document under Epic 8, so they have an implementation-planning home even though they are not direct PRD FRs.

### UX to Architecture Alignment

Aligned:

- Architecture states Phase 1 has no frontend implementation requirement and Phase 2 Admin UI should use Hexalith.FrontComposer plus Fluent UI Blazor through an adapter-backed composition layer.
- Architecture explicitly supports UX boundaries: do not reshape immutable Tenants domain contracts for UI generation, use UI-facing command/projection mappings, treat SignalR notifications as refresh nudges, and gate UI command success on status/projection/audit evidence.
- Architecture recognizes FrontComposer readiness dependencies for command lifecycle feedback, audit timeline, consequence preview, semantic tokens, accessibility, localization, and documentation evidence.
- Architecture supports the UX truth model by requiring projection freshness, command lifecycle, consequence preview, audit evidence, accessibility, localization, and documentation as explicit Phase 2 gates.
- Architecture supports the UX security posture through ProblemDetails, support-safe diagnostics, no raw payload/token/internal metadata exposure, and source-of-truth event/projection separation.

### Alignment Issues

1. Architecture epic numbering is stale relative to the current epics document.
   - Current epics: Epic 8 is Phase 2 Access Administration UI Readiness; Epic 9 is Shared Domain-Service Infrastructure Extraction.
   - Architecture still says in multiple places that Epics 1-8 cover backend/package/documentation MVP and Epic 9 is Phase 2 UI readiness.
   - Impact: implementation handoff may route UI-readiness work or shared-infrastructure extraction to the wrong epic if readers rely on the architecture mapping.
   - Recommendation: update architecture sections that map epic numbers so Epic 8 = UI readiness and Epic 9 = shared infrastructure extraction.

### Warnings

- Phase 2 UI is intentionally not implementation-ready until FrontComposer command lifecycle, audit timeline or fallback, consequence preview, accessibility, localization, and documentation evidence are resolved or scoped as approved fallbacks.
- UX contains detailed Phase 2 requirements that are not PRD FRs; this is acceptable only if Epic 8 remains clearly marked as Phase 2 UI readiness and does not contaminate Phase 1 backend/package MVP scope.
- Command-capable UI slices must fail closed when freshness, authorization, consequence preview, lifecycle support, or audit path cannot be established.

## Epic Quality Review

### Quality Findings by Severity

#### Critical Violations

1. Epic 8 is a Phase 2 UI readiness/design track, not an implementation-ready user-value epic.
   - Evidence: Epic 8 has no direct FR coverage and is titled "Phase 2 Access Administration UI Readiness." Stories include "Define Operations Shell Navigation and Layout" and "Design Tenant Access Review and User Lookup Surfaces."
   - Why this violates best practices: implementation epics should deliver independently usable value. A readiness/design epic can be valid planning work, but it should not be presented as a normal implementation epic unless it produces shippable UI increments.
   - Impact: sprint planning could treat Phase 2 UI readiness as Phase 1 implementation work, despite PRD and architecture saying Phase 1 has no frontend implementation requirement.
   - Recommendation: keep Epic 8 explicitly outside Phase 1 implementation, or rewrite it into future implementation epics with shippable read-only UI slices and command-capable slices once FrontComposer dependencies are resolved.

2. Epic 9 is a technical shared-infrastructure extraction workstream, not a Tenants user-value epic.
   - Evidence: Epic 9 has no direct FR coverage and covers shared hosting, projection, cursor, subscription, testing, and UI primitives across Commons, EventStore, and FrontComposer.
   - Why this violates best practices: the epic is primarily architectural/refactoring infrastructure. It may deliver developer/platform value eventually, but it crosses repository/module ownership and is not independently implementable inside the Tenants domain without coordinated shared-module work.
   - Impact: it risks violating the Tenants domain boundary by pulling generic infrastructure work into the Tenants implementation plan.
   - Recommendation: move Epic 9 to a shared-platform backlog or split it by owning technical module: Commons, EventStore, FrontComposer, then a final Tenants migration story after shared APIs exist.

#### Major Issues

1. Architecture and epics disagree on Epic 8/Epic 9 meaning.
   - Evidence: current `epics.md` defines Epic 8 as UI readiness and Epic 9 as shared infrastructure extraction; `architecture.md` still describes Epic 9 as Phase 2 UI readiness in multiple places.
   - Impact: implementation handoff can route work to the wrong epic and misclassify shared extraction versus UI readiness.
   - Recommendation: update architecture epic mappings before Phase 4 implementation begins.

2. Epic 4 has a forward dependency on Epic 5 client/package work.
   - Evidence: Story 4.2 assumes a consuming service references Tenants Contracts and Client packages, and Story 4.7 requires package-only consumer validation; Epic 5 later defines NuGet packaging, client service registration, and package adoption.
   - Impact: Epic 4 consumer integration is not fully independent unless the Client package and registration surface already exist before Epic 4 begins.
   - Recommendation: move minimal client package/registration prerequisites before Epic 4, or move consumer handler registration/sample-service stories after Epic 5.

3. Story 1.6 has a forward dependency on packaging validation.
   - Evidence: Story 1.6 expects package-only consumer validation against Contracts and Server packages, but packaging is not established until Epic 5.
   - Impact: Epic 1 cannot be completed independently if package-only validation infrastructure is not already available.
   - Recommendation: either move package validation ACs to Epic 5/6 or establish the package validation baseline in an earlier setup story.

4. Greenfield/brownfield planning state is ambiguous.
   - Evidence: PRD classifies the project as greenfield, architecture says the repository is already initialized and no starter CLI should be run, and epics begin with domain bootstrap rather than initial project setup.
   - Impact: if implementation starts from scratch, setup/CI/package topology work is missing or too late; if implementation starts from the existing repo, the epics should say they assume an initialized baseline.
   - Recommendation: state the implementation baseline explicitly. For a true greenfield run, add early setup and CI/package topology stories. For the current repo, document "existing initialized repository" as a prerequisite.

5. Several acceptance criteria leave outcomes ambiguous.
   - Examples:
     - Story 1.1: "rejected or no-op according to the domain outcome"
     - Story 1.2: "duplicate global administrator rejection or no-op outcome"
     - Story 2.5: "configuration-key-not-found rejection or no-op outcome defined by the domain contract"
     - Story 3.5: "rejected or clamped according to the documented API contract"
     - Stories 4.6, 5.3, 5.5, and 6.5 use "where practical," "where available," or "where configured"
   - Impact: tests cannot be written deterministically until the expected behavior is chosen.
   - Recommendation: replace alternatives with one expected outcome per AC, or add a named decision table that stories must follow.

6. Some stories are too broad for independent completion.
   - Examples:
     - Story 3.6 combines projection write safety, query contracts, controller adapter behavior, ETag/freshness behavior, and Tier 2/3 isolation evidence.
     - Story 4.7 combines sample service implementation, event handlers, idempotent projection, demo support, package-only validation, and docs.
     - Story 6.5 combines CI restore/build/tests, coverage gates, semantic-release, package validation, public API compatibility, and release behavior.
     - Story 6.6 combines health/readiness, pub/sub recovery, structured logging, ProblemDetails safety, and deployment smoke evidence.
     - Story 8.8 combines keyboard, screen reader, forced colors, reduced motion, contrast, live regions, localization, focus return, and multiple domain states.
     - Story 9.8 combines migration to shared APIs, runtime equivalence, package behavior, documentation/artifact updates, and reuse evidence.
   - Impact: these are likely multi-story or mini-epic units and will be hard to complete, review, and validate in one implementation pass.
   - Recommendation: split by independently testable behavior and evidence target.

#### Minor Concerns

1. Epic 6 is operator/maintainer-value oriented and acceptable, but Story 6.5 is more of a release-engineering gate than user-facing behavior.
   - Recommendation: keep the maintainer persona explicit and ensure each CI/CD story has observable release-readiness value.

2. Several stories rely on "support-safe references" without defining the exact reference shape in the story.
   - Recommendation: add or reference a common support-safe reference contract before stories assert copy/display behavior.

3. Database/entity creation timing check is not applicable.
   - Reason: Tenants uses EventStore and DAPR abstractions, not direct relational database tables. No up-front "create all tables" anti-pattern was found.

### Epic Compliance Summary

| Epic | User Value | Independence | Story Quality | Main Finding |
| ---- | ---------- | ------------ | ------------- | ------------ |
| Epic 1 | Mostly pass | Partial | Partial | Governance value is clear, but Story 1.6 depends on later packaging validation and some ACs allow rejection/no-op ambiguity. |
| Epic 2 | Pass | Mostly pass | Partial | Access/configuration value is clear; ambiguous no-op/rejection outcomes should be resolved. |
| Epic 3 | Pass | Mostly pass | Partial | Query/audit value is clear; Story 3.6 is too broad and Story 3.5 has reject-or-clamp ambiguity. |
| Epic 4 | Pass | Partial | Partial | Consumer value is clear, but it depends on Client/package capabilities from later Epic 5. |
| Epic 5 | Pass | Mostly pass | Mostly pass | Developer adoption value is clear; "where practical" performance/API evidence should be made deterministic. |
| Epic 6 | Mostly pass | Mostly pass | Partial | Operator value is clear; CI/CD and availability stories are broad and need slicing. |
| Epic 7 | Pass | Pass | Mostly pass | Documentation/adoption proof is user-value oriented and traceable. |
| Epic 8 | Fail for Phase 1 implementation | Fail unless deferred | Partial | UI readiness/design track is valid planning work but not a Phase 1 implementation-ready epic. |
| Epic 9 | Fail as Tenants implementation epic | Fail without cross-module plan | Partial | Shared extraction is technical cross-repo work and should move to owning technical-module backlogs. |

### Dependency Analysis

- No circular dependency was found among Epics 1-7.
- Forward dependency found: Epic 4 consumer integration assumes Epic 5 client/package adoption surface.
- Forward dependency found: Story 1.6 assumes packaging validation that is not introduced until Epic 5/6.
- Deferred dependency found: Epic 8 depends on FrontComposer command lifecycle, audit timeline/fallback, consequence preview, accessibility, localization, and documentation evidence.
- Cross-module dependency found: Epic 9 depends on shared Commons, EventStore, and FrontComposer work that cannot be completed solely inside Tenants.

### Remediation Priorities

1. Update architecture epic numbering and scope language to match current `epics.md`.
2. Decide implementation baseline: existing initialized repo versus true greenfield setup.
3. Move or split packaging/client prerequisites so Epic 4 does not depend on future Epic 5 work.
4. Resolve ambiguous AC alternatives into deterministic expected outcomes.
5. Keep Epic 8 and Epic 9 out of Phase 1 implementation readiness unless they are explicitly re-scoped.
6. Split broad stories before sprint execution.

## Summary and Recommendations

### Overall Readiness Status

**NEEDS WORK**

The planning set is not ready to proceed as a full implementation plan without correction. The PRD is complete, functional requirement coverage is complete, and UX scope is mostly aligned. The blocker is epic quality: the current epic set mixes Phase 1 implementation, Phase 2 UI readiness/design, and shared cross-module infrastructure extraction in one implementation plan.

Conditional path: Epics 1-7 are broadly usable after dependency and AC cleanup. Epics 8 and 9 should not enter Phase 1 implementation as-is.

### Critical Issues Requiring Immediate Action

1. Epic 8 must be explicitly deferred or rewritten.
   - Current form is Phase 2 UI readiness/design, not a Phase 1 implementation-ready epic.
   - It should remain outside Phase 1 or be rewritten later as shippable UI slices after FrontComposer readiness is resolved.

2. Epic 9 must move out of the Tenants implementation plan.
   - Current form is cross-module shared-infrastructure extraction.
   - It belongs in the owning technical-module backlogs or a coordinated shared-platform plan, with a later Tenants migration story.

3. Architecture must be updated to match current epic numbering.
   - Architecture still describes Epic 9 as Phase 2 UI readiness.
   - Current epics define Epic 8 as UI readiness and Epic 9 as shared infrastructure extraction.

4. Forward dependencies must be removed before sprint execution.
   - Epic 4 assumes Client/package capabilities that are introduced in Epic 5.
   - Story 1.6 assumes package-only validation before packaging is established.

5. Ambiguous acceptance criteria must be made deterministic.
   - Replace "rejection or no-op," "rejected or clamped," "where practical," and "where available" with explicit expected outcomes or named fallback policies.

### Recommended Next Steps

1. Update `architecture.md` to correct Epic 8/Epic 9 mapping and Phase 1 versus Phase 2 scope language.

2. Mark Epic 8 and Epic 9 as deferred/non-Phase-1 work, or split them into separate planning tracks outside the Phase 1 implementation backlog.

3. Reorder or split client/package prerequisite work so Epic 4 consumer integration no longer depends on later Epic 5 capabilities.

4. Move package-only validation ACs out of Story 1.6 or add an early setup/package-validation baseline story if implementation truly starts from a greenfield baseline.

5. Replace ambiguous AC alternatives with chosen outcomes for bootstrap duplicate behavior, duplicate global admin behavior, missing configuration removal, audit page-size handling, and "where practical" evidence expectations.

6. Split broad stories before sprint execution, especially Stories 3.6, 4.7, 6.5, 6.6, 8.8, and 9.8.

7. Clarify whether Phase 4 implementation starts from the existing initialized repository or from a true greenfield setup. If true greenfield, add setup, CI, solution topology, and package baseline stories before domain stories.

### Final Note

This assessment identified **11 primary issues** across **2 categories**:

- UX/architecture alignment: 1 major issue
- Epic quality/readiness: 2 critical, 6 major, and 3 minor issues

No document-format duplicates, missing PRD requirements, or missing FR coverage gaps were found. Address the critical and major issues before proceeding with the full implementation plan.

**Assessment Date:** 2026-06-02
**Assessor:** Codex using `bmad-check-implementation-readiness`
