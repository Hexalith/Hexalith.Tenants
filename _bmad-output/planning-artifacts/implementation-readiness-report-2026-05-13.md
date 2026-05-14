---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
includedFiles:
  prd:
    - _bmad-output/planning-artifacts/prd.md
  architecture:
    - _bmad-output/planning-artifacts/architecture.md
  epics:
    - _bmad-output/planning-artifacts/epics.md
  ux:
    - _bmad-output/planning-artifacts/ux-design-specification.md
relatedContext:
  - _bmad-output/planning-artifacts/prd-validation-report.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-05-12-epic-5-runtime-readiness-caveat.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-05-13
**Project:** Hexalith.Tenants

## Step 1: Document Discovery

### PRD Files Found

**Whole Documents:**
- prd.md (57,342 bytes, modified 2026-04-02 07:04)
- prd-validation-report.md (25,410 bytes, modified 2026-03-07 18:35) - related report, not the PRD source

**Sharded Documents:**
- None found

### Architecture Files Found

**Whole Documents:**
- architecture.md (89,939 bytes, modified 2026-04-02 07:04)

**Sharded Documents:**
- None found

### Epics & Stories Files Found

**Whole Documents:**
- epics.md (70,620 bytes, modified 2026-04-02 07:04)
- sprint-change-proposal-2026-05-12-epic-5-runtime-readiness-caveat.md (7,317 bytes, modified 2026-05-12 20:26) - related change proposal

**Sharded Documents:**
- None found

### UX Design Files Found

**Whole Documents:**
- ux-design-specification.md (123,071 bytes, modified 2026-03-25 20:14)

**Sharded Documents:**
- None found

### Issues Found

- No whole-versus-sharded duplicate conflicts found.
- Recommended assessment set confirmed by user on 2026-05-13:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/ux-design-specification.md

## PRD Analysis

Source: _bmad-output/planning-artifacts/prd.md

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
FR43: A developer can install Hexalith.Tenants via NuGet packages (Contracts, Client, Server, Testing, Aspire)
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
NFR13: State reconstruction from the event store on startup completes within 30 seconds for up to 1,000 tenants with an assumed average of 500 events per tenant (500,000 total events), verified by a startup benchmark test that seeds the target event volume and measures time to ready state. Event store snapshots are a Phase 3 optimization if this target is exceeded at scale
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

- MVP must validate both the Hexalith.Parties first-consumer technical path and a developer adoption path that reaches the first command within 30 minutes.
- MVP must include tenant aggregate lifecycle commands, user-role management, global administrator support, tenant configuration, tenant read models, global administrator bootstrapping, five NuGet packages, DAPR pub/sub integration, optimistic concurrency, actionable domain errors, quickstart documentation, event contract reference, sample consuming service, "aha moment" demo, and CI/CD quality gates.
- Tenant deletion is explicitly out of scope; tenants can be disabled but never deleted, preserving immutable audit history.
- gRPC is explicitly out of scope; command APIs are REST only.
- Phase 2 features are deferred: EventStore tenant authorization plugin, Keycloak JWT projection sync, Admin UI/dashboard, custom/extensible roles, bulk tenant provisioning, and F# consumption support.
- Phase 3 features are deferred: hierarchical sub-tenants, multi-deployment tenant migration, per-tenant service registry, cross-deployment tenant federation, and event store snapshots for faster state reconstruction at scale.
- The product is a .NET 10+ developer tool distributed as five NuGet packages plus a deployable microservice, aligned with Hexalith.EventStore structure and conventions.
- Package quality standards include Source Link, deterministic builds, XML documentation, semantic-release, centralized package management, and CI validation of expected package count before publish.
- Solution structure expects Contracts, Client, Server, API gateway, Aspire, AppHost, ServiceDefaults, Testing, tiered tests, and sample projects.
- Test architecture is tiered: Tier 1 unit tests, Tier 2 DAPR slim integration tests, Tier 3 Aspire/Docker E2E contract tests.
- Code style inherits EventStore conventions: file-scoped namespaces, Allman braces, `_camelCase` fields, interface `I` prefix, async suffixes, CRLF, UTF-8, and warnings as errors.
- Infrastructure access is through DAPR sidecars; tenant commands carry TenantId following EventStore's Domain + AggregateId + TenantId pattern.

### PRD Completeness Assessment

The PRD is highly explicit for readiness purposes: it contains 65 numbered functional requirements, 24 numbered non-functional requirements, clear MVP scope, deferred Phase 2/Phase 3 scope, out-of-scope exclusions, package architecture, solution structure, test tiers, CI/CD expectations, and implementation constraints. The main traceability risk is breadth: many requirements combine product behavior, developer experience, operations, tests, and documentation, so the epic validation must prove that every non-code deliverable and quality gate is represented, not just the tenant domain model.

## Epic Coverage Validation

Source: _bmad-output/planning-artifacts/epics.md

### Epic FR Coverage Extracted

- Epic 1 covers FR43 and FR58.
- Epic 2 covers FR1-FR5, FR13-FR18, FR35-FR36, and FR49-FR53.
- Epic 3 covers FR6-FR12, FR19-FR24, and FR31-FR34.
- Epic 4 covers FR37-FR42, FR44-FR45, and FR62.
- Epic 5 covers FR25-FR30.
- Epic 6 covers FR46-FR47.
- Epic 7 covers FR48 and FR54-FR57.
- Epic 8 covers FR59-FR61 and FR63-FR65.

Total FRs in epics: 65

### Coverage Matrix

| FR Number | PRD Requirement | Epic Coverage | Status |
| --------- | --------------- | ------------- | ------ |
| FR1 | A global administrator can create a new tenant with a unique identifier and name | Epic 2 | Covered |
| FR2 | A developer can update a tenant's metadata | Epic 2 | Covered |
| FR3 | A global administrator can disable a tenant | Epic 2 | Covered |
| FR4 | A global administrator can re-enable a tenant | Epic 2 | Covered |
| FR5 | Domain events for tenant lifecycle changes | Epic 2 | Covered |
| FR6 | Tenant owner can add a user with a role | Epic 3 | Covered |
| FR7 | Tenant owner can remove a user | Epic 3 | Covered |
| FR8 | Tenant owner can change a user's role | Epic 3 | Covered |
| FR9 | Reject adding an existing tenant member | Epic 3 | Covered |
| FR10 | Reject role escalation violations | Epic 3 | Covered |
| FR11 | Domain events for user-role changes | Epic 3 | Covered |
| FR12 | Optimistic concurrency enforcement | Epic 3 | Covered |
| FR13 | Existing global administrator can designate another global administrator | Epic 2 | Covered |
| FR14 | Existing global administrator can remove global administrator status | Epic 2 | Covered |
| FR15 | Global administrator can perform all tenant operations | Epic 2 | Covered |
| FR16 | Global administrator actions produce auditable events | Epic 2 | Covered |
| FR17 | Bootstrap initial global administrator | Epic 2 | Covered |
| FR18 | Reject bootstrap after global administrators exist | Epic 2 | Covered |
| FR19 | Tenant owner can set configuration entry | Epic 3 | Covered |
| FR20 | Tenant owner can remove configuration entry | Epic 3 | Covered |
| FR21 | Configuration keys support dot-delimited namespaces | Epic 3 | Covered |
| FR22 | Domain events for configuration changes | Epic 3 | Covered |
| FR23 | Enforce configuration limits | Epic 3 | Covered |
| FR24 | Reject configuration operations exceeding limits | Epic 3 | Covered |
| FR25 | Paginated list of tenants | Epic 5 | Covered |
| FR26 | Query specific tenant details | Epic 5 | Covered |
| FR27 | Query tenant users with roles | Epic 5 | Covered |
| FR28 | Query tenants for a specific user | Epic 5 | Covered |
| FR29 | Audit query by tenant ID and date range | Epic 5 | Covered |
| FR30 | Cursor-based pagination with consistent ordering | Epic 5 | Covered |
| FR31 | TenantReader query-only behavior | Epic 3 | Covered |
| FR32 | TenantContributor domain command capability | Epic 3 | Covered |
| FR33 | TenantOwner user-role and configuration management | Epic 3 | Covered |
| FR34 | Roles isolated per tenant | Epic 3 | Covered |
| FR35 | Publish tenant domain events via DAPR pub/sub as CloudEvents 1.0 | Epic 2 | Covered |
| FR36 | Documented tenant event topic naming convention | Epic 2 | Covered |
| FR37 | Consuming service can subscribe and build local projection | Epic 4 | Covered |
| FR38 | Consuming service reacts to user addition/removal | Epic 4 | Covered |
| FR39 | Consuming service reacts to tenant disable/enable | Epic 4 | Covered |
| FR40 | Consuming service reacts to configuration changes | Epic 4 | Covered |
| FR41 | Event contracts include event ID and aggregate version | Epic 4 | Covered |
| FR42 | Idempotent event processing documentation | Epic 4 | Covered |
| FR43 | Install Hexalith.Tenants via NuGet packages | Epic 1 | Covered |
| FR44 | Register tenant client services with a single DI extension method | Epic 4 | Covered |
| FR45 | Register tenant event handlers under 20 lines | Epic 4 | Covered |
| FR46 | In-memory fakes without external infrastructure | Epic 6 | Covered |
| FR47 | Testing fakes execute same domain logic as production | Epic 6 | Covered |
| FR48 | Deploy tenant service using .NET Aspire hosting extensions | Epic 7 | Covered |
| FR49 | Actionable command rejection error messages | Epic 2 | Covered |
| FR50 | Reject commands targeting non-existent tenant | Epic 2 | Covered |
| FR51 | Reject commands targeting disabled tenant | Epic 2 | Covered |
| FR52 | Reject duplicate operations with current state | Epic 2 | Covered |
| FR53 | Commands and event storage succeed independently of DAPR pub/sub | Epic 2 | Covered |
| FR54 | Tenant command latency metrics via OpenTelemetry | Epic 7 | Covered |
| FR55 | Event processing metrics via OpenTelemetry | Epic 7 | Covered |
| FR56 | Deploy alongside EventStore using standard DAPR configuration | Epic 7 | Covered |
| FR57 | Tenant service is stateless between requests | Epic 7 | Covered |
| FR58 | CI/CD quality gates | Epic 1 | Covered |
| FR59 | Quickstart guide enables first command within 30 minutes | Epic 8 | Covered |
| FR60 | Quickstart includes prerequisite validation | Epic 8 | Covered |
| FR61 | Event contract reference for commands/events/schemas | Epic 8 | Covered |
| FR62 | Sample consuming service | Epic 4 | Covered |
| FR63 | "Aha moment" demo | Epic 8 | Covered |
| FR64 | Cross-aggregate timing behavior documentation | Epic 8 | Covered |
| FR65 | Compensating command patterns documentation | Epic 8 | Covered |

### Missing Requirements

No missing PRD functional requirements were found in the epics FR Coverage Map.

No extra FR numbers were found in the epics coverage map that are absent from the PRD.

### Coverage Statistics

- Total PRD FRs: 65
- FRs covered in epics: 65
- Missing PRD FRs: 0
- Extra epic FRs not in PRD: 0
- Coverage percentage: 100%

### Coverage Assessment

Functional coverage is complete at the epic-map level. The next readiness risk is not whether FRs are named in epics, but whether the UX, architecture, story acceptance criteria, and NFR/test commitments are aligned deeply enough to make those epics implementable without hidden interpretation work.

## UX Alignment Assessment

### UX Document Status

Found: _bmad-output/planning-artifacts/ux-design-specification.md

The UX document is complete and substantial. It defines a production admin interface for Hexalith.Tenants and a reference module for Hexalith.FrontShell, including screen inventory, workflows, responsive behavior, accessibility requirements, component strategy, interaction patterns, and cross-project FrontShell dependencies.

### UX to PRD Alignment

Aligned areas:

- UX screen inventory maps directly to PRD tenant list/detail, create/update tenant, user-role management, configuration, audit, user tenant lookup, and global administrator requirements.
- UX Journey 2 supports FR6-FR8 and FR31-FR33 through inline user role management and role explanation.
- UX Journey 3 and Journey 7 support FR28, FR37-FR39, FR41-FR42, and incident/self-audit flows.
- UX Journey 4 supports FR1, FR17-FR18, FR59-FR60, and the first-run/bootstrap adoption path.
- UX Journey 5 supports FR29, FR61, FR64, and the audit/compliance story.
- UX Journey 6 supports FR3-FR4, FR51, and auditability around tenant disable/enable.
- UX accessibility and responsive design expand on NFR24 and future Admin UI quality expectations.

Alignment issues:

- The PRD explicitly defers "Admin UI / dashboard" to Phase 2, while the UX specification treats the Hexalith.Tenants UI as a production admin interface and reference FrontShell module with must-ship MVP interactions. This is a material scope mismatch.
- The PRD MVP feature set is backend/package/documentation oriented, but the UX spec introduces UI routes, components, SignalR-driven three-phase feedback, FrontShell hook changes, design tokens, and component deliverables. Those are not represented as PRD MVP requirements.
- PRD NFR24 states MVP error messages and documentation are English-only, with Phase 2 Admin UI responsible for WCAG 2.1 AA and i18n scoping. The UX spec defines WCAG 2.1 AA, keyboard navigation, and screen reader behavior as implementation requirements for the UI.

### UX to Architecture Alignment

Aligned areas:

- Architecture explicitly incorporates the UX document through "UX-Driven Architecture Amendments (2026-03-25)."
- D11 supports user search authorization scoping for GlobalAdmin, TenantOwner, and self-audit flows.
- D12 supports the audit trail UX through `TenantAuditProjection`, `GetTenantAuditQuery`, event categories, date filtering, and pagination.
- D13 promotes SignalR to must-ship and defines degradation thresholds for the UX three-phase feedback pattern.
- D14 supports anomaly detection as client-side heuristics, matching the UX MVP design.
- D15 enriches projections with `lastActivityAt`, `ownerCount`, `configKeyCount`, and membership metadata needed by dashboard indicators and user search.
- D16 supports consequence previews without a dedicated backend endpoint.
- D17 documents FrontShell cross-project dependencies and sequencing constraints.

Architecture gaps or risks:

- Architecture supports the UX additions, but this creates a stronger implementation scope than the PRD MVP appears to authorize. The architecture is aligned with UX, but PRD/architecture scope agreement needs a decision.
- D17 states frontend stories using FrontShell deliverables need explicit `blockedBy` relationships. Readiness depends on whether the epics/stories actually model these FrontShell dependencies.
- SignalR is marked must-ship in architecture because the UX needs Phase 3 confirmation, but PRD FR/NFR language does not independently establish SignalR as an MVP requirement.

### Warnings

- Warning: UX is not merely implied; it is fully specified. If Phase 4 implementation starts from the current epics without reconciling whether Admin UI is MVP or Phase 2, teams may implement different scopes from different planning artifacts.
- Warning: Cross-project FrontShell dependencies can block UI delivery even if backend tenant epics are ready.
- Warning: Accessibility expectations are clear in UX but deferred/ambiguous in PRD; acceptance criteria must identify whether WCAG 2.1 AA is required now or only for the Phase 2 Admin UI scope.

## Epic Quality Review

Source: _bmad-output/planning-artifacts/epics.md

### Best Practices Compliance Summary

| Epic | User Value | Independence | Story Size | Acceptance Criteria | Traceability | Status |
| ---- | ---------- | ------------ | ---------- | ------------------- | ------------ | ------ |
| Epic 1: Project Foundation & Solution Scaffolding | Borderline but acceptable for greenfield developer-tool setup | Mostly independent | Mixed | Strong BDD | FR43, FR58 | Major concern |
| Epic 2: Core Tenant Management & Global Administration | Strong | Mostly independent | Story 2.4 oversized | Strong but broad | Strong | Major concern |
| Epic 3: Tenant Membership, Roles & Configuration | Strong | Depends only on Epic 2 | Good | Strong | Strong | Acceptable |
| Epic 4: Event-Driven Integration & Consuming Service Support | Strong | Depends on Epic 2 events | Good | Strong | Strong | Acceptable |
| Epic 5: Tenant Discovery & Query | Strong | Depends on prior events/projections | Mixed | Incomplete for security/audit amendments | FR mapped but stale | Critical concern |
| Epic 6: Testing Package | Strong developer value | Depends on domain behavior | Good | Strong | Strong | Acceptable |
| Epic 7: Deployment & Observability | Strong operator value | Depends on service | Good | Mostly strong | Strong | Major concern |
| Epic 8: Documentation & Adoption | Strong developer value | Depends on completed behavior | Good | Strong | Strong | Acceptable |

### Critical Violations

1. **Epics are stale relative to UX-driven architecture amendments.**
   - Evidence: `epics.md` was completed 2026-03-07. The architecture was amended on 2026-03-25 with D11-D17 based on `ux-design-specification.md`, but the epics contain no stories for SignalR, FrontShell dependencies, `AuditTimeline`, `ConsequencePreview`, `useCommand pendingIds`, `useCommand` concurrent command support, toast batching, design tokens, or the three-phase Storybook reference.
   - Impact: Architecture says these are must-ship or explicit dependencies, but implementation stories do not sequence or own them.
   - Recommendation: Add an explicit UI/FrontShell integration epic or amend relevant epics with stories and `blockedBy` relationships for D17 deliverables. If Admin UI remains Phase 2, mark the UX-driven items deferred and remove must-ship language from MVP architecture/story readiness.

2. **Story 5.3 omits the query-side authorization scoping required by architecture D11.**
   - Evidence: Story 5.3 says any authenticated user calling `/api/users/{userId}/tenants` receives the specified user's tenant list. Architecture D11 requires scoped filtering: self gets own memberships, TenantOwner gets only users in owned tenants, GlobalAdmin gets all memberships.
   - Impact: This is a cross-tenant data leak risk and conflicts with NFR5.
   - Recommendation: Add acceptance criteria for all D11 cases, including negative assertions proving TenantOwners cannot see memberships outside owned tenants and ordinary users cannot query arbitrary users.

3. **Audit projection architecture is not represented as an implementable story.**
   - Evidence: Architecture D12 adds `TenantAuditProjection`, `TenantAuditReadModel`, `GetTenantAuditQuery`, category filtering, and date filtering. Epic 5 Story 5.3 exposes an audit endpoint, but Stories 5.1 and 5.2 only define tenant/global admin projections and cross-tenant indexes.
   - Impact: The endpoint depends on a projection/read model that no story actually creates.
   - Recommendation: Add a dedicated story for `TenantAuditProjection` and audit read model/query contract before Story 5.3, or expand Story 5.1 explicitly to include it.

### Major Issues

1. **Story 2.4 is too large and bundles several independently risky implementation areas.**
   - Evidence: One story includes REST command API, MediatR pipeline, bootstrap hosted service, multi-instance bootstrap behavior, DAPR pub/sub CloudEvents publishing, RFC 7807 rejection mapping, JWT authentication, aggregate discovery, `/process` endpoint behavior, DAPR service invocation, and Tier 2 integration tests.
   - Impact: This is closer to an epic slice than a story and makes completion, testing, and review harder.
   - Recommendation: Split into API command gateway, bootstrap service, event publishing, auth/error mapping, and DAPR integration test stories.

2. **Story 2.3 prebuilds future Epic 3 state behavior.**
   - Evidence: Story 2.3 creates `TenantState` with Users and Configuration fields plus Apply methods for `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, and `TenantConfigurationRemoved`, while noting the Handle methods are implemented later in Epic 3.
   - Impact: This violates incremental story boundaries by implementing future-domain storage ahead of the story that owns the behavior.
   - Recommendation: Keep only lifecycle state in Story 2.3, then add users/configuration state and Apply methods in Stories 3.1 and 3.3 when those behaviors are first delivered.

3. **FR53 validation in Story 2.4 reaches into future subscriber behavior.**
   - Evidence: Story 2.4 says subscribers catch up when pub/sub recovers, but consuming service subscription and sample subscriber behavior are not delivered until Epic 4.
   - Impact: The story cannot fully prove its own acceptance criteria without future work.
   - Recommendation: In Story 2.4, validate only command success and durable event storage when pub/sub is unavailable. Move subscriber catch-up validation to Epic 4 or Epic 7 integration tests.

4. **Epic 1 remains a technical setup epic, only partially rescued by developer value framing.**
   - Evidence: Epic 1 is "Project Foundation & Solution Scaffolding" with stories for solution structure, DAPR component YAML, ServiceDefaults, and CI/CD.
   - Impact: Greenfield setup is necessary, but the epic is still a technical milestone and does not deliver tenant-domain capability by itself.
   - Recommendation: Keep it only if the team accepts a greenfield foundation exception. Otherwise merge setup work into the first user-visible/domain slice and keep CI/CD as part of Definition of Done.

5. **Snapshot assumptions conflict across planning artifacts and current project context.**
   - Evidence: Story 7.3 specifies a 50-event snapshot interval for tenant domain and 100 for global administrators. The PRD says snapshots are Phase 3 optimization if startup targets are exceeded, while current EventStore project context says snapshot configuration is mandatory with an existing default of 100.
   - Impact: Implementers may choose different snapshot behavior depending on which artifact they trust.
   - Recommendation: Reconcile snapshot policy in PRD, architecture, and Story 7.3 before implementation.

### Minor Concerns

- Story 7.1 contains a duplicated phrase: "the Aspire dashboard launches and the Aspire dashboard launches."
- Story 1.1 says all 15 projects include 8 src, 5 test, and 2 sample projects, while another AC says `dotnet test` discovers 6 test projects including `samples/Hexalith.Tenants.Sample.Tests`. This is understandable but should be made explicit in the project count.
- Story 1.3 release expectations should be checked against the current semantic-release workflow, because local project instructions say release runs on merge to main.

### Dependency Analysis

- No explicit forward references like "depends on Story 5.x" were found in story headings, but several hidden forward dependencies exist through acceptance criteria.
- Epic order is mostly coherent: setup, core aggregates/API, membership/config, event consumers, queries, testing package, deployment/ops, documentation.
- Hidden dependency risk is highest around Story 2.4, Story 5.3, and the missing UX/FrontShell stories.

### Recommendations

1. Decide whether the Admin UI/FrontShell module is MVP or Phase 2. This determines whether D11-D17 become implementation stories now or later.
2. Amend Epic 5 for D11/D12 security and audit projection coverage before implementation.
3. Split Story 2.4 into smaller stories with independently testable acceptance criteria.
4. Move future state/apply behavior out of Story 2.3 into the Epic 3 stories that own those capabilities.
5. Reconcile snapshot interval and snapshot obligation across PRD, architecture, epics, and current EventStore rules.

### Quality Assessment

The epics are strong on FR traceability and BDD acceptance criteria, but they are not fully implementation-ready as written. The document is older than the UX and architecture amendments, and that staleness creates critical security, audit, and frontend dependency gaps. Backend/domain stories can likely begin after targeted amendments, but full Phase 4 implementation should not start from these epics unchanged.

## Summary and Recommendations

### Overall Readiness Status

**NEEDS WORK before full Phase 4 implementation.**

The planning set is not broken. It has strong PRD structure, complete FR numbering, complete epic-level FR coverage, robust BDD-style acceptance criteria, and a thoughtful architecture. The blocker is artifact alignment: the epics are stale relative to the UX-driven architecture amendments, and several critical implementation responsibilities are either missing or under-specified.

Backend/domain implementation could start safely only after the critical security/audit/story-boundary amendments below are made, and only if the Admin UI/FrontShell scope is explicitly deferred or separately planned.

### Critical Issues Requiring Immediate Action

1. **Resolve Admin UI/FrontShell scope.**
   - PRD defers Admin UI/dashboard to Phase 2.
   - UX treats the Tenants UI as production MVP/reference-module work.
   - Architecture D11-D17 supports UX as must-ship in several places.
   - Decision needed: Admin UI is MVP with stories, or it is Phase 2 and the architecture/UX amendments are deferred.

2. **Patch Epic 5 for query-side authorization scoping.**
   - Story 5.3 must implement D11 filtering for `/api/users/{userId}/tenants`.
   - Missing criteria create cross-tenant data leak risk against NFR5.

3. **Add audit projection ownership.**
   - Architecture D12 defines `TenantAuditProjection`, `TenantAuditReadModel`, and `GetTenantAuditQuery`.
   - Epics expose audit query behavior but do not clearly create the required projection/read model.

4. **Update epics for D13-D17 or explicitly defer them.**
   - SignalR, FrontShell dependencies, projection enrichment, consequence previews, and FrontShell component/hook changes need stories or deferral.

5. **Split Story 2.4 and remove hidden forward dependencies.**
   - Story 2.4 is too broad.
   - FR53 subscriber catch-up validation reaches into future Epic 4 behavior.
   - Story 2.3 prebuilds Epic 3 state/apply behavior.

### Recommended Next Steps

1. Run a correction pass on the planning artifacts, focused only on alignment:
   - Decide MVP versus Phase 2 for Admin UI.
   - Amend PRD, UX, architecture, and epics so they agree.

2. Update `epics.md` with targeted changes:
   - Add or amend a story for D11 query-side authorization.
   - Add a story for D12 audit projection/query model.
   - Add/defer D13-D17 FrontShell/SignalR/UI dependencies.
   - Split Story 2.4 into independently completable slices.

3. Reconcile snapshot policy:
   - PRD says snapshots are Phase 3 optimization if scale target fails.
   - Architecture/story text specifies snapshot intervals.
   - Current EventStore rules say snapshot configuration is mandatory with default 100.

4. Re-run implementation readiness after the artifact updates.

5. Only then create implementation stories or begin Phase 4 execution.

### Issue Count

This assessment identified **14 issues or risks** across **four categories**:

- Artifact alignment and scope
- Security/query authorization
- Missing architecture-to-epic coverage
- Story quality and dependency structure

### Final Note

This project is close in the good way: the raw planning quality is high, but the later UX and architecture work outran the epic document. Address the critical alignment issues before proceeding to implementation so developers are not forced to arbitrate between contradictory artifacts during story execution.

**Assessment completed:** 2026-05-13
**Assessor:** Codex using `bmad-check-implementation-readiness`
