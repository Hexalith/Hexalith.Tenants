---
stepsCompleted:
  - step-01-validate-prerequisites
  - step-02-design-epics
  - step-03-create-stories
  - step-04-final-validation
inputDocuments:
  - _bmad-output/planning-artifacts/prd.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-design-specification.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-02.md
---

# Tenants - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Tenants, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: A global administrator can create a new tenant with a unique identifier and name. In MVP, tenant creation is restricted to global administrators.

FR2: A developer can update a tenant's metadata, including name and description.

FR3: A global administrator can disable a tenant, preventing all commands against that tenant from succeeding.

FR4: A global administrator can re-enable a previously disabled tenant, restoring normal command processing.

FR5: The system produces a domain event for every tenant lifecycle change: created, updated, disabled, and enabled.

FR6: A tenant owner can add a user to a tenant with a specified role: TenantOwner, TenantContributor, or TenantReader.

FR7: A tenant owner can remove a user from a tenant.

FR8: A tenant owner can change a user's role within a tenant.

FR9: The system rejects adding a user who is already a member of the tenant.

FR10: The system rejects role changes that violate escalation boundaries, including a tenant owner assigning GlobalAdministrator.

FR11: The system produces a domain event for every user-role change: added, removed, and role changed.

FR12: The system enforces optimistic concurrency, rejecting conflicting concurrent modifications to the same aggregate.

FR13: An existing global administrator can designate a user as a global administrator.

FR14: An existing global administrator can remove a user's global administrator status and cannot remove self if they are the last global administrator.

FR15: A global administrator can perform any tenant operation across all tenants without per-tenant role assignment.

FR16: All global administrator actions produce auditable domain events.

FR17: The system provides a bootstrap mechanism, through seed command or startup configuration, to create the initial global administrator on first deployment when no global administrators exist.

FR18: The bootstrap mechanism only executes when zero global administrators exist in the event store; subsequent executions are rejected with a specific error indicating bootstrap has already completed.

FR19: A tenant owner can set a key-value configuration entry for a tenant.

FR20: A tenant owner can remove a configuration entry from a tenant.

FR21: Configuration keys support dot-delimited namespace conventions, such as `billing.plan` and `parties.maxContacts`, to prevent collisions between consuming services.

FR22: The system produces a domain event for every configuration change: set and removed.

FR23: The system enforces configuration limits: maximum 100 keys per tenant, maximum 1KB per value, and maximum 256 characters per key.

FR24: The system rejects configuration operations that exceed limits with a specific error identifying which limit was exceeded and current usage.

FR25: A developer can query a paginated list of all tenants with their IDs, names, and statuses.

FR26: A developer can query a specific tenant's details including its current users and their roles.

FR27: A developer can query the list of users in a specific tenant with their assigned roles.

FR28: A developer can query the list of tenants a specific user belongs to, with their role in each tenant.

FR29: A global administrator can query tenant access changes by tenant ID and date range for audit reporting, with pagination support using default page size 100 and maximum page size 1,000.

FR30: All list and query endpoints support cursor-based pagination with consistent ordering.

FR31: A TenantReader can query tenant details, user lists, and configuration for tenants they belong to, but cannot execute any state-changing commands.

FR32: A TenantContributor has TenantReader capabilities plus the ability to execute domain commands within the tenant, with specific commands defined by each consuming service.

FR33: A TenantOwner has TenantContributor capabilities plus user-role management and tenant configuration management.

FR34: A user with roles in multiple tenants can only access data and execute commands within each tenant according to their role in that tenant; roles do not transfer or aggregate across tenants.

FR35: The system publishes all tenant domain events via DAPR pub/sub as CloudEvents 1.0.

FR36: The system uses a documented topic naming convention for tenant events, such as `tenants.events`, consistent with Hexalith ecosystem patterns.

FR37: A consuming service can subscribe to tenant events and build a local projection of tenant state.

FR38: A consuming service can react to user addition/removal events to enforce or revoke access.

FR39: A consuming service can react to tenant disable/enable events to block or allow operations.

FR40: A consuming service can react to configuration change events to update tenant-specific behavior.

FR41: Event contracts include sufficient information, including event ID and aggregate version, for consuming services to implement idempotent event handling.

FR42: Documentation provides guidance on idempotent event processing patterns for consumers, including at-least-once delivery explanation, deduplication by event ID example, and idempotent handler pattern with code sample.

FR43: A developer can install Tenants via NuGet packages: Contracts, Client, Server, Testing, and Aspire.

FR44: A developer can register tenant client services in DI with a single extension method call.

FR45: A developer can register tenant event handlers in a consuming service in under 20 lines of DI configuration.

FR46: A developer can write tenant integration tests using in-memory fakes without external infrastructure, in under 10 lines per test.

FR47: The in-memory testing fakes execute the same domain logic as the production service, guaranteeing isolation at the aggregate domain model level and verified by a conformance test suite that runs identical command sequences against both fakes and production aggregate.

FR48: A developer can deploy the tenant service using .NET Aspire hosting extensions.

FR49: The system provides error messages for all command rejections that include the specific rejection reason, the entity involved, and a corrective action hint.

FR50: The system rejects commands targeting a non-existent tenant with a specific error identifying the missing tenant.

FR51: The system rejects commands targeting a disabled tenant with a specific error indicating the tenant's disabled status.

FR52: The system rejects duplicate operations, such as adding an already-present user, with a specific error including current state.

FR53: Commands and event storage succeed independently of DAPR pub/sub availability because the event store is the source of truth.

FR54: The system exposes tenant command latency metrics via OpenTelemetry.

FR55: The system exposes event processing metrics via OpenTelemetry.

FR56: A platform operator can deploy the tenant service alongside EventStore using standard DAPR configuration.

FR57: The tenant service is stateless between requests; all state is reconstructed from the event store on startup.

FR58: The CI/CD pipeline enforces quality gates: build, test Tier 1 and Tier 2, coverage threshold above 80% line coverage, 100% branch coverage on isolation/auth, and package validation before NuGet publish.

FR59: The project provides a quickstart guide that enables a developer to send their first tenant command within 30 minutes.

FR60: The quickstart guide includes prerequisite validation for DAPR sidecar and EventStore deployment.

FR61: The project provides an event contract reference documenting all commands, events, and schemas.

FR62: The project provides a sample consuming service demonstrating event subscription and access enforcement.

FR63: The project provides an "aha moment" demo, screencast, or video showing reactive cross-service access revocation.

FR64: The project provides documentation on cross-aggregate timing behavior, including event propagation window, sequence diagram, eventual consistency guidance, and planned auth plugin reference as synchronous enforcement option.

FR65: The project provides documentation on compensating command patterns, including restoring a wrongly removed user with explicit role specification and explaining why role must be explicitly specified rather than auto-restored.

### NonFunctional Requirements

NFR1: All tenant commands complete within 50ms p95 as measured by OpenTelemetry span duration.

NFR2: All read model queries complete within 50ms p95 for result sets within a single page, as measured by OpenTelemetry span duration.

NFR3: Event publication to DAPR pub/sub completes within 50ms p95 after command processing, as measured by OpenTelemetry span duration.

NFR4: In-memory testing fakes execute commands and produce events within 10ms, as measured by xUnit test execution time.

NFR5: Zero cross-tenant data leaks; no query, projection, or event subscription returns data belonging to a different tenant, verified by dedicated Tier 3 integration tests.

NFR6: Role escalation boundaries are enforced at the domain level; no actor can self-escalate, verified by unit tests covering every escalation path.

NFR7: All state-changing operations produce immutable, auditable domain events with actor ID, timestamp, and full operation context, verified by integration tests.

NFR8: Disabled tenants reject all commands immediately within the same aggregate, verified by unit tests after DisableTenant is applied to aggregate state.

NFR9: Encryption at rest and in transit is a deployment concern; the system relies on DAPR infrastructure configuration and does not implement its own encryption layer.

NFR10: Tenant isolation and role authorization logic requires 100% branch coverage, including aggregate authorization checks, projection tenant filtering, and role validation logic.

NFR11: The system supports up to 1,000 tenants with up to 500 users per tenant without performance degradation beyond stated latency targets, verified by seeded load tests.

NFR12: The tenant service is stateless; horizontal scaling is achieved by adding service instances.

NFR13: State reconstruction from the event store on startup completes within 30 seconds for up to 1,000 tenants with an assumed average of 500 events per tenant, using baseline EventStore snapshot configuration.

NFR14: All domain events conform to CloudEvents 1.0 specification.

NFR15: Event publication uses DAPR pub/sub abstraction with no direct dependency on a specific message broker.

NFR16: State persistence uses DAPR state store abstraction with no direct dependency on a specific database.

NFR17: The system degrades gracefully when DAPR pub/sub is unavailable; commands and event storage succeed because EventStore is the source of truth, and drain/catch-up evidence proves recovery.

NFR18: Event contracts are backward-compatible after v1.0 with no breaking schema changes to published events.

NFR19: All domain events include event ID and aggregate version to enable idempotent processing by consumers.

NFR20: The event store is the single source of truth; system state can be fully reconstructed by replaying events.

NFR21: Command processing and event storage are atomic; a command either fully succeeds or fully fails.

NFR22: API availability target is 99.9% in production deployments, as measured by health check endpoint uptime monitoring.

NFR23: No data loss under any failure scenario; events once stored are immutable and durable.

NFR24: MVP error messages and documentation are English-only. Phase 2 Admin UI accessibility baseline is WCAG 2.1 AA, with WCAG 2.2 AA as the target where supported by Fluent UI Blazor and FrontComposer. Phase 2 UI must address i18n considerations during scoping.

### Additional Requirements

- Starter template: Tenants must use the Hexalith.EventStore structure mirror as the canonical foundation. Do not run `aspire new` or another generic starter over this repository.

- Manual scaffolding or reconstruction must preserve EventStore-native package boundaries, DAPR/Aspire orchestration, and production/test parity.

- Runtime and language requirements are .NET 10 SDK `10.0.300`, C# latest, nullable references, implicit usings, and warnings as errors.

- Build tooling must use `Hexalith.Tenants.slnx`, central package management through `Directory.Packages.props`, shared `Directory.Build.props` and `Directory.Build.targets`, and no inline `Version=` attributes on `PackageReference`.

- Published package topology is Contracts, Client, Server, Aspire, and Testing. Host projects remain non-packable.

- Code organization must preserve `src/Hexalith.Tenants.Contracts`, `.Client`, `.Server`, host `Hexalith.Tenants`, `.Aspire`, `.AppHost`, `.ServiceDefaults`, `.Testing`, matching tests, and sample consuming service boundaries.

- Root-level submodules only may be initialized or updated. Recursive submodule initialization remains disallowed.

- Hexalith.EventStore remains a root-level submodule dependency, not a NuGet dependency, and EventStore primitives remain the foundation for command, query, aggregate, projection, and domain result behavior.

- Aggregates, states, validators, projections, and read models that EventStore must discover must live in `Hexalith.Tenants.Server`.

- The aggregate model includes `TenantAggregate` for tenant lifecycle, membership, and configuration, and `GlobalAdministratorsAggregate` for platform-level global administrator set and bootstrap protection.

- Tenant events must include managed `TenantId` as a top-level payload field because the EventStore envelope tenant is the platform tenant `system`.

- Identity conventions are platform tenant `system`, tenant domain `tenants`, global administrator domain `global-administrators`, and actor ID format `{tenant}:{domain}:{aggregateId}`.

- Authorization is layered through EventStore API gate, Tenants domain RBAC in aggregate Handle methods, trusted `actor:globalAdmin` command-envelope extension, and query-side row filtering.

- Production identity must derive user identity from JWT `sub` and must not use `name` or `email` for authorization decisions.

- Commands enter through EventStore command submission, with `POST /api/v1/commands` as the command gateway. Do not create per-command controllers.

- The Tenants host must expose the EventStore domain processor route, default `/process`, for aggregate actor domain service invocation.

- Query endpoints are explicit REST endpoints backed by EventStore query contracts: `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, and `GET /api/tenants/{tenantId}/audit`.

- Query controllers must be thin adapters that validate route/query input, derive authenticated user from JWT `sub`, validate signed opaque cursors, and dispatch `SubmitQuery`.

- Error responses must use RFC 7807 Problem Details. Domain rejections map to HTTP statuses through EventStore rejection handling and are not logged as infrastructure errors.

- List responses use cursor-based pagination with signed, opaque, scope-bound cursors. Offset/limit pagination is not allowed.

- DAPR resource names remain convention-derived: app ID `tenants`, state-store component `statestore`, topic `tenants.events`, and dead letter topic `deadletter.tenants.events`.

- DAPR access control is deny-by-default; new service invocation paths must update receiving service config and verify caller app IDs.

- Tenant domain snapshot interval is 50 events. Global administrator snapshot interval uses the EventStore default unless evidence requires otherwise.

- Projection state uses EventStore projection conventions and DAPR state abstraction. Shared cross-tenant indexes require ETag/optimistic concurrency or verified `CachingProjectionActor` fan-in behavior.

- Security hardening requires `TenantRole` and `TenantStatus` to serialize by name, reserve ordinal `0` for fail-closed `Unknown`, and reject missing/unrecognized unsafe values by default.

- User IDs and managed tenant IDs compare case-sensitively with `StringComparer.Ordinal`; canonical casing is an operator/IdP boundary contract.

- `InMemoryTenantService` and `TenantTestHelpers` intentionally return EventStore `DomainResult` as the canonical test outcome type.

- `InMemoryTenantProjection` may preserve silent default handling for real-service parity, but conformance tests must fail when a new success event is added without projection wiring.

- Tests follow the tiered model: Tier 1 pure unit/contract tests, Tier 2 DAPR/server integration tests, and Tier 3 Aspire E2E tests.

- Testing uses xUnit v3, Shouldly, NSubstitute, Testcontainers, Aspire testing, and coverlet. Shouldly assertions are required; raw `Assert.*` should not be used.

- Public events and contracts require serialization round-trip, naming convention, conformance, package-only consumer, and compatibility coverage appropriate to their blast radius.

- Phase 2 UI implementation is intentionally not ready until FrontComposer command lifecycle, audit timeline, consequence preview, accessibility, localization, and documentation evidence are resolved.

- Future UI implementation must consume FrontComposer primitives where available and keep tenant-specific mappings, workflow decisions, command availability, and domain wording in a Tenants UI adapter layer.

- Add a follow-up workstream named Shared Domain-Service Infrastructure Extraction. Do not reopen completed Tenants epics or rewrite completed story history.

- Shared extraction must preserve completed Tenants behavior while moving reusable mechanics into appropriate shared modules and then migrating Tenants to consume those APIs.

- Commons extraction candidates include generic pagination/result/options helpers with no Tenants or EventStore dependency, especially `PaginatedResult<T>` and small reusable validation helpers.

- EventStore hosting/runtime extraction candidates include ServiceDefaults patterns, DAPR state-store health checks, domain-service route mapping, `/process` and projection endpoint wiring, telemetry conventions, and common startup helpers where they can be made domain-neutral.

- EventStore query/projection extraction candidates include cursor codec pattern, pagination policy, cursor scope validation primitives, projection write policy, DAPR projection state-store adapter, ETag retry/recovery behavior, and projection write diagnostics.

- EventStore client extraction candidates include generic event subscription endpoint, event envelope processor, idempotent handler dispatch, and local projection application mechanics.

- EventStore testing extraction candidates include reusable in-memory aggregate/domain-service harness patterns and conformance helper utilities, while tenant command fixtures and tenant-specific assertions remain in Tenants.

- FrontComposer follow-up should convert Tenants UI planning into reusable operational primitives where appropriate while keeping tenant-specific UI mappings and command rules in a Tenants adapter layer.

- Tenants must continue to own commands, events, rejections, enums, identities, tenant query contracts, `TenantAggregate`, `GlobalAdministratorsAggregate`, tenant states, tenant read models, tenant-specific projection mutation logic, tenant-specific authorization, query filtering, audit semantics, support-safe wording, tenant package adapters, and tenant adoption documentation.

- After shared APIs exist, update PRD wording, architecture ownership language, README/package descriptions, package validation scripts, solution/package governance tests, consumer package smoke tests, deployment docs, and adoption docs to describe shared-module ownership accurately.

- Success criteria for the extraction workstream include functionally equivalent Tenants startup/runtime behavior, stable Tenants package public behavior except explicitly approved dependency/API changes, materially less boilerplate for a new EventStore-backed domain project, and Tenants code visibly centered on domain behavior.

### UX Design Requirements

UX-DR1: Use a Fluent UI Blazor v5 and Hexalith.FrontComposer foundation for the Phase 2 Admin UI, verifying exact component APIs against the project-pinned prerelease package during implementation.

UX-DR2: Use an Operations Shell as the primary layout with navigation for Tenants, Users, Global Administrators, and Audit.

UX-DR3: Make the tenant list the default operational triage surface with filters, sorting, pagination, tenant status, member count, owner count, warning indicators, pending command state, and projection freshness.

UX-DR4: Provide tenant detail context with overview, member access, configuration, command state, and audit evidence entry points while preserving selected tenant and filter context.

UX-DR5: Provide a lightweight exact user lookup path for access questions that start with a person rather than a tenant.

UX-DR6: Provide global administrator list and management planning surfaces with stronger friction for platform recovery risk.

UX-DR7: Provide audit views that support filtering by tenant, user, event type, or date and expose actor, target, tenant scope, outcome, timestamp, and support-safe reference.

UX-DR8: Implement a Truth State Badge pattern for freshness, authorization, command lifecycle, projection confirmation, and audit evidence states.

UX-DR9: Implement a Freshness Gate pattern that shows freshness label, timestamp/version marker, refresh action, and blocking reason before access-impacting commands.

UX-DR10: Implement an Unavailable Action Reason pattern so disabled or unavailable actions explain missing permission, stale data, missing lifecycle support, missing consequence preview, missing audit proof, backend unavailable, or high-impact flow not ready.

UX-DR11: Implement a Consequence Preview pattern for access-impacting commands showing tenant, target user, current role, owner count, known consequences, known unknowns, freshness, recovery path, and audit expectation.

UX-DR12: Implement a Command Lifecycle Panel that distinguishes eligible, previewed, submitted, accepted, projection pending, confirmed, failed, unknown, audit pending, and audit available states.

UX-DR13: Implement an Audit Evidence Receipt pattern after meaningful access changes, showing actor, target, tenant scope, outcome, timestamp, projection marker, and support-safe audit reference.

UX-DR14: Provide a Flat Audit List fallback using DataGrid with stable ordering, filters, loading/empty/error states, and accessible expansion when a reusable audit timeline is not ready.

UX-DR15: Treat `RemoveUserFromTenant` as the first command-capable slice and model it as an access-evidence journey: member row, consequence preview, command lifecycle, projection confirmation, and audit proof.

UX-DR16: Use the `RemoveUserFromTenant` state model `eligible -> previewed -> submitted -> accepted -> projection_pending -> confirmed | failed | unknown | audit_pending | audit_available`.

UX-DR17: Preserve last confirmed projection data and show pending or confirming hints separately; never replace projection truth with optimistic UI.

UX-DR18: Treat SignalR projection notifications as freshness nudges that trigger re-query or reconciliation, not as durable source-of-truth data.

UX-DR19: Explain unavailable actions where safety or authorization clarity matters instead of hiding them.

UX-DR20: Block or add explicit freshness friction before access-impacting commands when projection data is stale or freshness cannot be measured.

UX-DR21: Unknown freshness, incomplete consequence preview, indeterminate authorization, or missing lifecycle support blocks destructive action by default unless an approved override path exists.

UX-DR22: Last-owner, global-administrator, and tenant-wide actions require elevated friction with risk explanation, affected scope, evidence freshness, audit consequence, and intentional confirmation.

UX-DR23: Command feedback must distinguish request sent, accepted request, projection pending, confirmed access update, rejected request, already-applied outcome, failed transport, degraded status, unable-to-verify state, audit pending, and audit available.

UX-DR24: Do not use generic command success language such as "Saved" or "Done" for access-impacting workflows.

UX-DR25: Every meaningful access change must provide a path to audit evidence or explain audit proof is delayed, unavailable, or not implemented.

UX-DR26: Recovery flows must use explicit compensating commands and must not label correction as undo.

UX-DR27: The UI must not claim downstream session revocation, token invalidation, or consuming-service enforcement unless backend evidence exists.

UX-DR28: Use dense, full-width operational surfaces with tables, split views, tabs, side panels, dialogs, and inline status regions instead of decorative card-heavy dashboards.

UX-DR29: Use one primary action per region; destructive actions require consequence preview, eligibility checks, command feedback, and audit linkage rather than visual alarm alone.

UX-DR30: Use semantic Fluent tokens for tenant status, roles, projection freshness, command lifecycle, destructive actions, audit availability, degraded states, and unable-to-verify states; do not create a separate branded palette.

UX-DR31: Use color only as a secondary signal. All statuses require text, accessible labels, and appropriate iconography or structure.

UX-DR32: Use modest Fluent typography and compact headings appropriate to operational tables, panels, dialogs, and audit surfaces.

UX-DR33: Maintain stable dimensions for toolbars, status badges, row actions, command lifecycle panels, and action cells to avoid layout shift.

UX-DR34: Use DataGrid-backed patterns for tenant list, member table, user lookup, and flat audit fallback.

UX-DR35: Table states must distinguish loading, empty, filtered-empty, unauthorized, stale, failed-to-load, degraded, and not-yet-projected cases.

UX-DR36: Preserve context across tenant list, detail, access review, command preview, confirmation, audit evidence, and return navigation.

UX-DR37: Use desktop-first responsive design. Desktop starts at 1024px, wide desktop at 1440px, tablet at 768-1023px, and mobile at 320-767px.

UX-DR38: On small screens, prioritize tenant/user identity, status, freshness, read-only summary, audit/support-safe reference lookup, and degraded-state messaging.

UX-DR39: High-impact access changes should be discouraged or unavailable on very small screens unless freshness, authorization, consequence preview, lifecycle feedback, focus behavior, and audit path can all remain visible.

UX-DR40: Meet WCAG 2.1 AA baseline and target WCAG 2.2 AA where supported by Fluent UI Blazor and FrontComposer.

UX-DR41: All interactive elements must be keyboard reachable with focus order matching visual/task order and visible focus indicators in normal, high-contrast, and forced-colors modes.

UX-DR42: Disabled actions must expose readable reasons and cannot rely only on tooltips.

UX-DR43: Command lifecycle changes must use accessible live regions with appropriate politeness; assertive announcements are reserved for rejection, failure, destructive blockers, or unable-to-verify states.

UX-DR44: Dialogs and command previews must trap focus when modal, support safe escape behavior, and return focus to the launching row or action.

UX-DR45: Timestamps require exact accessible text, not only relative labels.

UX-DR46: All state labels, role names, timestamps, warnings, disabled reasons, and recovery actions must be localizable without concatenated sentence fragments.

UX-DR47: Support-safe references must avoid raw command payloads, bearer tokens, stack traces, aggregate IDs, internal correlation IDs, raw EventStore metadata, local paths, and sensitive tenant/user data.

UX-DR48: Responsive and accessibility acceptance evidence must cover stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, permission-missing cases, keyboard-only navigation, screen reader review, forced-colors, reduced motion, contrast, live-region announcements, and focus return.

### FR Coverage Map

FR1: Epic 1 - Tenant lifecycle creation governance.

FR2: Epic 1 - Tenant metadata update governance.

FR3: Epic 1 - Tenant disable governance.

FR4: Epic 1 - Tenant re-enable governance.

FR5: Epic 1 - Tenant lifecycle domain event production.

FR6: Epic 2 - Tenant member addition.

FR7: Epic 2 - Tenant member removal.

FR8: Epic 2 - Tenant role change.

FR9: Epic 2 - Duplicate tenant membership rejection.

FR10: Epic 2 - Role escalation rejection.

FR11: Epic 2 - User-role domain event production.

FR12: Epic 2 - Optimistic concurrency for tenant aggregate modifications.

FR13: Epic 1 - Global administrator designation.

FR14: Epic 1 - Global administrator removal safety.

FR15: Epic 1 - Cross-tenant global administrator authority.

FR16: Epic 1 - Global administrator audit events.

FR17: Epic 1 - Initial global administrator bootstrap.

FR18: Epic 1 - Bootstrap single-use protection.

FR19: Epic 2 - Tenant configuration set.

FR20: Epic 2 - Tenant configuration removal.

FR21: Epic 2 - Configuration key namespace convention.

FR22: Epic 2 - Configuration domain event production.

FR23: Epic 2 - Configuration count/key/value limits.

FR24: Epic 2 - Configuration limit rejection detail.

FR25: Epic 3 - Paginated tenant list query.

FR26: Epic 3 - Tenant detail query.

FR27: Epic 3 - Tenant users query.

FR28: Epic 3 - User tenants query.

FR29: Epic 3 - Tenant audit query by tenant and date range.

FR30: Epic 3 - Cursor-based pagination and consistent ordering.

FR31: Epic 2 - TenantReader read-only behavior.

FR32: Epic 2 - TenantContributor role behavior.

FR33: Epic 2 - TenantOwner role behavior.

FR34: Epic 2 - Tenant-scoped role isolation.

FR35: Epic 4 - DAPR pub/sub tenant event publication.

FR36: Epic 4 - Tenant event topic naming.

FR37: Epic 4 - Consumer local tenant projection.

FR38: Epic 4 - Consumer reaction to user addition/removal.

FR39: Epic 4 - Consumer reaction to tenant disable/enable.

FR40: Epic 4 - Consumer reaction to configuration change.

FR41: Epic 4 - Event metadata for idempotent consumer handling.

FR42: Epic 4 - Idempotent event processing documentation.

FR43: Epic 5 - NuGet package installation.

FR44: Epic 5 - Tenant client DI registration.

FR45: Epic 4 - Consumer event handler registration.

FR46: Epic 5 - In-memory fake tenant integration tests.

FR47: Epic 5 - Production/fake domain logic conformance.

FR48: Epic 5 - Aspire hosting extension deployment.

FR49: Epic 2 - Actionable domain rejection messages.

FR50: Epic 2 - Non-existent tenant command rejection.

FR51: Epic 2 - Disabled tenant command rejection.

FR52: Epic 2 - Duplicate operation rejection.

FR53: Epic 2 - Commands and event storage independent of pub/sub availability.

FR54: Epic 6 - Tenant command latency metrics.

FR55: Epic 6 - Event processing metrics.

FR56: Epic 6 - DAPR deployment alongside EventStore.

FR57: Epic 6 - Stateless tenant service operation.

FR58: Epic 6 - CI/CD quality gates and package validation.

FR59: Epic 7 - Quickstart first-command documentation.

FR60: Epic 7 - Quickstart prerequisite validation.

FR61: Epic 7 - Event contract reference.

FR62: Epic 4 - Sample consuming service for event subscription and access enforcement.

FR63: Epic 7 - Reactive access revocation demo.

FR64: Epic 7 - Cross-aggregate timing documentation.

FR65: Epic 7 - Compensating command pattern documentation.

## Epic List

### Epic 1: Tenant Governance Foundation

Global administrators can bootstrap governance, create and manage tenant lifecycle state, and receive auditable events for tenant and global administrator changes.

**FRs covered:** FR1, FR2, FR3, FR4, FR5, FR13, FR14, FR15, FR16, FR17, FR18

### Epic 2: Tenant Access, Roles, and Configuration

Tenant owners can manage tenant members, roles, configuration, command safety, and tenant-scoped RBAC with clear domain rejections and event-sourced invariants.

**FRs covered:** FR6, FR7, FR8, FR9, FR10, FR11, FR12, FR19, FR20, FR21, FR22, FR23, FR24, FR31, FR32, FR33, FR34, FR49, FR50, FR51, FR52, FR53

### Epic 3: Tenant Discovery and Audit Queries

Developers and administrators can discover tenants, inspect tenant/user membership, and retrieve paginated audit history through tenant-scoped query endpoints.

**FRs covered:** FR25, FR26, FR27, FR28, FR29, FR30

### Epic 4: Reactive Consumer Integration

Consuming services can subscribe to tenant events, build local projections, react to tenant changes, and handle event delivery idempotently.

**FRs covered:** FR35, FR36, FR37, FR38, FR39, FR40, FR41, FR42, FR45, FR62

### Epic 5: Developer Adoption and Testing

Developers can install Tenants packages, register client services, test with production-parity in-memory fakes, and deploy with Aspire support.

**FRs covered:** FR43, FR44, FR46, FR47, FR48

### Epic 6: Production Operations and Release Readiness

Platform operators can deploy, observe, scale, and release Tenants through DAPR, OpenTelemetry, stateless runtime behavior, and CI/CD quality gates.

**FRs covered:** FR54, FR55, FR56, FR57, FR58

### Epic 7: Documentation and Adoption Proof

Developers can reach first command quickly, understand tenant event contracts, design for timing windows, use compensating commands, and see the reactive access model demonstrated.

**FRs covered:** FR59, FR60, FR61, FR63, FR64, FR65

### Epic 8: Phase 2 Access Administration UI Readiness

Administrators get a safe Operations Shell plan for tenant access review, projection freshness, command lifecycle, audit proof, accessibility, localization, and `RemoveUserFromTenant` as the first command-capable slice.

**FRs covered:** None directly; covers NFR24 and UX-DR1 through UX-DR48

### Epic 9: Shared Domain-Service Infrastructure Extraction

Future EventStore-backed domain-service developers can reuse shared hosting, projection, cursor, subscription, testing, and UI primitives while Tenants returns to focused domain ownership.

**FRs covered:** None directly; covers the 2026-06-02 sprint change proposal and shared-infrastructure additional requirements

## Epic 1: Tenant Governance Foundation

Global administrators can bootstrap governance, create and manage tenant lifecycle state, and receive auditable events for tenant and global administrator changes.

### Story 1.1: Bootstrap Initial Global Administrator

As a platform operator,
I want the tenant service to bootstrap the first global administrator from startup configuration,
So that a fresh deployment can become governable before any tenant operations are allowed.

**Acceptance Criteria:**

**Given** the global administrator aggregate has no prior global administrator events
**When** the service starts with `Tenants:BootstrapGlobalAdminUserId` configured
**Then** the system submits a `BootstrapGlobalAdmin` command through the normal command pipeline
**And** the command targets platform tenant `system`, domain `global-administrators`, and aggregate `global-administrators`

**Given** the bootstrap command is handled with no existing global administrators
**When** the aggregate processes the command
**Then** it emits an auditable global administrator event for the configured user
**And** the event payload includes the required tenant/platform identity, actor context, and timestamp fields

**Given** at least one global administrator already exists
**When** bootstrap runs again on startup
**Then** the aggregate returns a specific bootstrap-already-completed rejection
**And** the startup flow treats the rejection as an expected idempotent outcome rather than an infrastructure failure

**Given** multiple service instances start concurrently with the same bootstrap user configured
**When** more than one instance submits bootstrap
**Then** only the first successful command establishes the global administrator
**And** later submissions are rejected or no-op according to the domain outcome without retry storms or warning/error logs

**Given** the bootstrap command, event, and rejection contracts are added or changed
**When** contract tests run
**Then** naming, serialization, and rejection-convention tests validate the new contracts
**And** aggregate unit tests cover success, already-bootstrapped rejection, and null-command guard behavior

### Story 1.2: Manage Global Administrator Membership

As a global administrator,
I want to add and remove global administrators,
So that platform governance can be delegated and recovered safely.

**Acceptance Criteria:**

**Given** a global administrator aggregate with at least one existing global administrator
**When** an existing global administrator designates another user as a global administrator
**Then** the aggregate emits an auditable global administrator membership event
**And** the resulting state includes the designated user as a global administrator

**Given** a user is already a global administrator
**When** an existing global administrator designates that same user again
**Then** the aggregate returns a specific duplicate global administrator rejection or no-op outcome
**And** no duplicate global administrator state entry is created

**Given** a global administrator aggregate with multiple global administrators
**When** an existing global administrator removes another global administrator
**Then** the aggregate emits an auditable global administrator removal event
**And** the removed user no longer has global administrator authority in the aggregate state

**Given** a global administrator is the last remaining global administrator
**When** that user attempts to remove their own global administrator status
**Then** the aggregate returns a specific last-global-administrator rejection
**And** the state remains unchanged

**Given** a command envelope does not carry trusted global administrator authority
**When** the actor attempts to add or remove a global administrator
**Then** the command is rejected by the authorization path
**And** user-supplied authority extensions are not trusted as global administrator proof

### Story 1.3: Create Tenant as Global Administrator

As a global administrator,
I want to create a tenant with a unique identifier and name,
So that applications can start managing access and configuration for a new tenant.

**Acceptance Criteria:**

**Given** no tenant aggregate state exists for the requested managed tenant ID
**When** a trusted global administrator submits `CreateTenant`
**Then** the aggregate emits a `TenantCreated` event
**And** the event payload includes the managed `TenantId`, name, optional description, actor context, and creation timestamp

**Given** `CreateTenant` is submitted
**When** the command envelope identifies the aggregate ID
**Then** the aggregate uses `envelope.AggregateId` as the managed tenant ID
**And** it does not trust a conflicting tenant ID from the command body

**Given** an actor is not a global administrator
**When** the actor attempts to create a tenant
**Then** the command is rejected before tenant state is created
**And** the rejection response identifies the authorization reason without exposing sensitive internal details

**Given** tenant aggregate state already exists
**When** a global administrator submits `CreateTenant` for the same tenant aggregate
**Then** the aggregate returns a specific tenant-already-exists rejection
**And** no second `TenantCreated` event is emitted

**Given** the tenant creation command succeeds
**When** event contract tests run
**Then** `TenantCreated` serialization round-trips with camelCase JSON and `DateTimeOffset` timestamp semantics
**And** naming convention tests validate command, event, and rejection names

### Story 1.4: Update Tenant Metadata

As an authorized tenant operator,
I want to update a tenant's name and description,
So that tenant records remain accurate for discovery, audit, and operations.

**Acceptance Criteria:**

**Given** an active tenant exists
**When** an authorized actor submits an update with a changed name or description
**Then** the aggregate emits a tenant metadata updated event
**And** the updated state reflects the new name and description

**Given** no tenant exists for the target aggregate
**When** an authorized actor submits a tenant metadata update
**Then** the aggregate returns a specific tenant-not-found rejection
**And** no update event is emitted

**Given** a tenant metadata update is submitted
**When** the actor lacks required authority for the tenant
**Then** the command is rejected through the governance authorization path
**And** the rejection includes a corrective action hint suitable for ProblemDetails mapping

**Given** a tenant metadata update succeeds
**When** the resulting event is persisted and applied
**Then** the event contains the managed `TenantId` as a top-level payload field
**And** the state `Apply` method mutates only the metadata fields represented by the event

**Given** metadata command or event contracts change
**When** unit and contract tests run
**Then** tests cover success, missing tenant rejection, authorization rejection, null-command guard behavior, and serialization round trip

### Story 1.5: Disable and Re-enable Tenant

As a global administrator,
I want to disable and re-enable tenants,
So that platform governance can stop or restore tenant command processing without deleting audit history.

**Acceptance Criteria:**

**Given** an active tenant exists
**When** a trusted global administrator submits `DisableTenant`
**Then** the aggregate emits a `TenantDisabled` event
**And** the tenant state becomes disabled without deleting tenant history, users, or configuration

**Given** a disabled tenant exists
**When** a trusted global administrator submits `EnableTenant`
**Then** the aggregate emits a `TenantEnabled` event
**And** the tenant state becomes active again for normal processing

**Given** no tenant exists for the target aggregate
**When** a global administrator submits disable or enable
**Then** the aggregate returns a specific tenant-not-found rejection
**And** no lifecycle event is emitted

**Given** an actor is not a global administrator
**When** the actor submits disable or enable
**Then** the command is rejected by the authorization path
**And** no lifecycle event is emitted

**Given** a tenant is disabled
**When** aggregate state is replayed from lifecycle events
**Then** the reconstructed state exposes the tenant as disabled
**And** the disabled state remains available for command validation without requiring future stories

### Story 1.6: Validate Tenant Governance Contract and Audit Events

As a developer adopting Tenants,
I want governance commands and events to be contractually validated,
So that tenant lifecycle and global administrator behavior remain stable across packages and consumers.

**Acceptance Criteria:**

**Given** tenant lifecycle and global administrator commands, events, and rejections exist
**When** naming convention tests run
**Then** commands follow `{Verb}{Target}`, events follow `{Target}{PastVerb}`, and rejections follow `{Target}{Reason}Rejection`
**And** all rejection events implement the required rejection event contract

**Given** governance events are serialized
**When** serialization round-trip tests run
**Then** events use `System.Text.Json`, camelCase JSON, `DateTimeOffset` timestamps, and top-level managed `TenantId` where tenant events are involved
**And** no Newtonsoft.Json dependency is introduced

**Given** governance aggregate Handle methods are discovered by EventStore conventions
**When** reflection or aggregate discovery tests run
**Then** Handle methods remain public static pure functions returning `DomainResult`
**And** aggregates and state classes remain in the EventStore-scanned `Hexalith.Tenants.Server` assembly

**Given** successful governance commands emit events
**When** aggregate unit tests inspect the emitted events
**Then** every lifecycle and global administrator state change produces an auditable event with actor, timestamp, and operation context
**And** business rule violations return domain rejections rather than thrown exceptions

**Given** package validation runs for governance contracts
**When** package-only consumer validation restores and builds against the Contracts and Server packages
**Then** governance contracts remain consumable without inline package versions
**And** public API changes are caught by the established package validation lane

## Epic 2: Tenant Access, Roles, and Configuration

Tenant owners can manage tenant members, roles, configuration, command safety, and tenant-scoped RBAC with clear domain rejections and event-sourced invariants.

### Story 2.1: Add Tenant Member With Role

As a tenant owner,
I want to add a user to my tenant with a defined tenant role,
So that access can be granted through the tenant domain model.

**Acceptance Criteria:**

**Given** an active tenant exists and the actor is authorized as a tenant owner or global administrator
**When** the actor submits `AddUserToTenant` with a user ID and assignable tenant role
**Then** the aggregate emits a `UserAddedToTenant` event
**And** the tenant state records the user with the requested role

**Given** the tenant has no membership history
**When** the first user is added to the tenant
**Then** the command is allowed through the bootstrap membership path
**And** the first-user path remains explicit in tests so later RBAC changes do not remove it accidentally

**Given** the target user is already a tenant member
**When** an authorized actor submits `AddUserToTenant` for the same user
**Then** the aggregate returns a specific duplicate membership rejection
**And** the rejection includes the existing role or current state needed for corrective action

**Given** the requested role is not assignable in the tenant domain
**When** an actor submits `AddUserToTenant`
**Then** the aggregate or validator rejects the command
**And** no membership event is emitted

**Given** add-user command, event, and rejection contracts exist
**When** contract and aggregate tests run
**Then** tests cover success, first-user bootstrap, duplicate rejection, invalid-role rejection, authorization failure, and null-command guard behavior

### Story 2.2: Remove Tenant Member

As a tenant owner,
I want to remove a user from my tenant,
So that tenant access can be revoked through an auditable domain event.

**Acceptance Criteria:**

**Given** an active tenant exists with the target user as a member
**When** an authorized tenant owner or global administrator submits `RemoveUserFromTenant`
**Then** the aggregate emits a `UserRemovedFromTenant` event
**And** the user is removed from tenant membership state

**Given** the target user is not a member of the tenant
**When** an authorized actor submits `RemoveUserFromTenant`
**Then** the aggregate returns a specific user-not-in-tenant rejection
**And** no removal event is emitted

**Given** removing the target user would leave the tenant with zero owners
**When** an authorized actor submits `RemoveUserFromTenant`
**Then** the aggregate allows the removal according to current product policy
**And** tests document that ownerless tenants are allowed and surfaced as a later UX warning rather than a domain invariant

**Given** an actor lacks owner or global administrator authority
**When** the actor submits `RemoveUserFromTenant`
**Then** the command is rejected
**And** the tenant membership state remains unchanged

**Given** removal succeeds
**When** event and state tests run
**Then** the event payload includes managed `TenantId`, target user, removed role context where available, actor context, and timestamp
**And** state replay reconstructs the removal from events

### Story 2.3: Change Tenant Member Role With Escalation Protection

As a tenant owner,
I want to change a tenant member's role within allowed tenant roles,
So that tenant access can evolve without allowing privilege escalation outside the tenant model.

**Acceptance Criteria:**

**Given** an active tenant exists with the target user as a member
**When** an authorized tenant owner or global administrator submits `ChangeUserRole` with an assignable tenant role
**Then** the aggregate emits a `UserRoleChanged` event
**And** the tenant state reflects the new role for the target user

**Given** the target user is not a tenant member
**When** an authorized actor submits `ChangeUserRole`
**Then** the aggregate returns a specific user-not-in-tenant rejection
**And** no role-changed event is emitted

**Given** the requested role is GlobalAdministrator or another non-tenant role
**When** a tenant owner submits `ChangeUserRole`
**Then** the aggregate rejects the command as a role escalation boundary violation
**And** no tenant role state changes

**Given** a user attempts to self-elevate beyond their current tenant authority
**When** the command is processed
**Then** the domain authorization logic rejects the command
**And** unit tests cover every self-escalation path defined by the role hierarchy

**Given** role enum serialization or validation changes
**When** tests run
**Then** `TenantRole.Unknown` remains non-assignable and default-denied
**And** unrecognized or unsafe role values fail closed

### Story 2.4: Enforce Tenant Role Behavior and Isolation

As a consuming-service developer,
I want tenant role behavior to be enforced consistently,
So that users only act within the authority granted for each tenant.

**Acceptance Criteria:**

**Given** a user has TenantReader role in a tenant
**When** the user performs tenant read operations
**Then** query access for tenant details, user lists, and configuration can be allowed by query authorization
**And** state-changing tenant commands are rejected

**Given** a user has TenantContributor role in a tenant
**When** consuming services evaluate tenant role authority
**Then** the user has TenantReader capabilities plus contributor-level command authority for consuming-service commands
**And** Tenants itself does not grant member or configuration management authority from TenantContributor alone

**Given** a user has TenantOwner role in a tenant
**When** the user submits tenant membership or configuration commands
**Then** the aggregate authorizes owner-level management actions
**And** the same role does not grant global administrator authority

**Given** a user has roles in multiple tenants
**When** the user acts against one tenant
**Then** only the role assigned in that tenant is evaluated
**And** roles from other tenants do not aggregate or transfer

**Given** role authorization logic is tested
**When** coverage reports are generated
**Then** tenant isolation and role authorization branch coverage meets the required 100% branch target for these paths

### Story 2.5: Set and Remove Tenant Configuration

As a tenant owner,
I want to set and remove tenant configuration entries,
So that consuming services can react to tenant-specific settings.

**Acceptance Criteria:**

**Given** an active tenant exists and the actor is a tenant owner or global administrator
**When** the actor submits `SetTenantConfiguration` with a key and value
**Then** the aggregate emits a `TenantConfigurationSet` event
**And** tenant state stores the key-value pair

**Given** an active tenant has an existing configuration key
**When** an authorized actor submits `RemoveTenantConfiguration`
**Then** the aggregate emits a `TenantConfigurationRemoved` event
**And** tenant state no longer contains that key

**Given** the requested configuration key does not exist
**When** an authorized actor submits `RemoveTenantConfiguration`
**Then** the aggregate returns a specific configuration-key-not-found rejection or no-op outcome defined by the domain contract
**And** no unrelated configuration entries are changed

**Given** an actor lacks owner or global administrator authority
**When** the actor submits a configuration command
**Then** the aggregate rejects the command
**And** no configuration event is emitted

**Given** configuration events are published and replayed
**When** state replay runs
**Then** configuration state reconstructs set and removed entries deterministically
**And** event payloads include managed `TenantId`, key, value or removed-key context, actor context, and timestamp

### Story 2.6: Enforce Configuration Namespaces and Limits

As a tenant owner,
I want configuration keys and values to follow documented limits,
So that tenant configuration remains predictable for all consuming services.

**Acceptance Criteria:**

**Given** a configuration key uses dot-delimited namespace form such as `billing.plan`
**When** an authorized actor sets the value
**Then** the command is accepted if all count, key length, and value length limits are satisfied
**And** the namespace form is preserved exactly in emitted events and state

**Given** a tenant already has the maximum number of configuration keys
**When** an authorized actor attempts to add a new key
**Then** the aggregate or validator rejects the command with the maximum-key-count reason
**And** the rejection includes the current usage needed for corrective action

**Given** a configuration key exceeds the maximum key length
**When** an authorized actor submits the command
**Then** the command is rejected with a key-length reason
**And** no configuration event is emitted

**Given** a configuration value exceeds the maximum value length
**When** an authorized actor submits the command
**Then** the command is rejected with a value-length reason
**And** the limit is evaluated using the documented character-count semantics

**Given** configuration limit constants exist on `TenantAggregate`
**When** validators and tests reference the limits
**Then** validators use the aggregate constants rather than duplicating literals
**And** tests cover boundary values at, below, and above each limit

### Story 2.7: Return Actionable Tenant Command Rejections

As a developer integrating Tenants,
I want tenant command failures to return specific actionable rejections,
So that problems can be diagnosed without reading internal logs.

**Acceptance Criteria:**

**Given** a command targets a non-existent tenant
**When** the command is processed
**Then** the system returns a specific tenant-not-found rejection
**And** the mapped ProblemDetails response identifies the missing tenant and corrective action

**Given** a command targets a disabled tenant
**When** the command is processed
**Then** the system returns a specific disabled-tenant rejection
**And** the rejection indicates that the tenant is disabled without exposing internal state

**Given** a duplicate operation is submitted
**When** the aggregate detects the duplicate
**Then** the system returns a specific duplicate-operation rejection
**And** the rejection includes current-state context safe for API clients

**Given** any business rule violation occurs in aggregate Handle methods
**When** the command is processed
**Then** the aggregate returns `DomainResult.Rejection`
**And** it does not throw exceptions for expected business rules

**Given** rejection events are persisted or mapped to API responses
**When** rejection tests and ProblemDetails catalog tests run
**Then** rejection type, reason code, HTTP status mapping, and corrective action wording are stable and documented

### Story 2.8: Validate Tenant Access Concurrency and Pub/Sub Independence

As a platform engineer,
I want tenant access and configuration commands to preserve integrity under concurrency and pub/sub failure,
So that the event store remains the durable source of truth.

**Acceptance Criteria:**

**Given** two conflicting commands modify the same tenant aggregate concurrently
**When** both commands are processed
**Then** EventStore optimistic concurrency allows only a valid serialized outcome
**And** the losing or retried command returns the configured conflict/rejection outcome without duplicate events

**Given** two commands add the same user concurrently
**When** aggregate state is rehydrated between attempts
**Then** duplicate membership is rejected
**And** final state contains a single membership entry for that user

**Given** DAPR pub/sub is unavailable after command processing succeeds
**When** tenant events are persisted
**Then** command processing and event storage remain successful because the event store is the source of truth
**And** publication recovery can republish persisted events when pub/sub recovers

**Given** access/configuration command tests run in Tier 1
**When** aggregate Handle and Apply behavior is exercised
**Then** tests use Shouldly assertions and cover success, rejection, disabled tenant, authorization, concurrency-relevant duplicate handling, and null guard paths

**Given** integration tests run where DAPR infrastructure is available
**When** command persistence and publication behavior are inspected
**Then** tests assert persisted CloudEvent/state-store end state rather than only API return codes
**And** pub/sub failure handling does not treat domain rejections as infrastructure dead letters

## Epic 3: Tenant Discovery and Audit Queries

Developers and administrators can discover tenants, inspect tenant/user membership, and retrieve paginated audit history through tenant-scoped query endpoints.

### Story 3.1: Query Paginated Tenant List

As a developer,
I want to query a paginated list of tenants,
So that I can discover existing tenants and inspect their status.

**Acceptance Criteria:**

**Given** tenant projection data exists for multiple tenants
**When** an authorized caller requests `GET /api/tenants`
**Then** the response returns tenant IDs, names, and statuses in a stable order
**And** the response shape includes `items`, `cursor`, and `hasMore`

**Given** more tenants exist than fit in one page
**When** the caller uses the returned cursor for the next request
**Then** the next page continues from the previous page without duplicates or skipped rows
**And** the cursor is opaque, signed, and bound to the query scope

**Given** a cursor is tampered with or replayed against a different query shape
**When** the endpoint validates the cursor
**Then** the request is rejected with a safe ProblemDetails response
**And** no internal signing material or cursor payload is exposed

**Given** tenant status enum values are serialized in query responses
**When** response serialization tests run
**Then** status values fail closed for unknown state
**And** missing or unsafe status values do not materialize as active tenant state

**Given** list query tests run
**When** tenant index projection state is queried
**Then** tests cover empty, single-page, multi-page, invalid-cursor, and isolation-safe cases

### Story 3.2: Query Tenant Details

As a developer,
I want to query a specific tenant's details,
So that I can inspect its current status, metadata, users, roles, and configuration.

**Acceptance Criteria:**

**Given** a tenant projection exists for the requested tenant ID
**When** an authorized caller requests `GET /api/tenants/{tenantId}`
**Then** the response returns tenant status, name, description, users, roles, and configuration visible to that caller
**And** the response includes freshness or ETag evidence where available

**Given** no tenant projection exists for the requested tenant ID
**When** the caller requests tenant detail
**Then** the endpoint returns a tenant-not-found ProblemDetails response
**And** the response does not reveal data for any other tenant

**Given** the caller is a TenantReader for the tenant
**When** the caller requests tenant detail
**Then** the query authorization path allows read-only detail access
**And** state-changing command affordance remains outside the query response contract

**Given** the caller lacks tenant scope and is not a global administrator
**When** the caller requests tenant detail
**Then** the query is rejected or filtered according to query authorization rules
**And** no tenant detail data is returned

**Given** detail query tests run
**When** projection and controller behavior are exercised
**Then** tests cover found, not found, reader-authorized, unauthorized, ETag/freshness, and cross-tenant isolation cases

### Story 3.3: Query Tenant Users

As a developer,
I want to query the users assigned to a tenant,
So that I can understand who currently has tenant access.

**Acceptance Criteria:**

**Given** a tenant has user-role membership in projection state
**When** an authorized caller requests `GET /api/tenants/{tenantId}/users`
**Then** the response returns users with assigned tenant roles
**And** users are ordered consistently for pagination or deterministic comparison

**Given** the tenant has no users
**When** an authorized caller requests the users endpoint
**Then** the response returns an empty `items` collection
**And** the endpoint distinguishes empty membership from tenant-not-found

**Given** the caller has TenantReader, TenantContributor, TenantOwner, or global administrator scope
**When** query authorization evaluates the request
**Then** the caller can read user-role rows for the tenant according to role policy
**And** roles from other tenants do not grant access

**Given** the caller lacks access to the tenant
**When** the users endpoint is requested
**Then** no user rows are returned
**And** the response follows the configured unauthorized or forbidden ProblemDetails behavior

**Given** tenant users query tests run
**When** multiple tenant memberships exist in projection state
**Then** tests prove no user rows leak across tenant IDs
**And** role serialization uses the fail-closed role contract

### Story 3.4: Query User Tenants

As a developer,
I want to query the tenants a user belongs to,
So that user-centered onboarding, support, and access review flows can discover tenant assignments.

**Acceptance Criteria:**

**Given** a user belongs to one or more tenants
**When** an authorized caller requests `GET /api/users/{userId}/tenants`
**Then** the response returns each visible tenant with the user's role in that tenant
**And** ordering is stable and pagination-ready

**Given** the requester is querying their own tenant assignments
**When** the query handler evaluates scope
**Then** the handler returns tenant rows visible to the requester
**And** no rows for unrelated tenants are included

**Given** a tenant owner queries a user assigned to one of their tenants
**When** query-side filtering evaluates the request
**Then** the owner sees only rows for tenants where they have owner authority
**And** rows from other tenants are filtered out

**Given** a global administrator queries a user
**When** the query handler evaluates scope
**Then** the global administrator can see the user's tenant assignments across tenants
**And** the response still omits sensitive internals not part of the query contract

**Given** user-tenants query tests run
**When** users and owners span multiple tenants
**Then** tests cover self, tenant-owner, global-admin, unauthorized, empty, and cross-tenant filtering cases

### Story 3.5: Query Tenant Access Audit History

As a global administrator,
I want to query tenant access changes by tenant and date range,
So that I can produce audit evidence for governance and incident response.

**Acceptance Criteria:**

**Given** audit projection state contains access-change events for a tenant
**When** a global administrator requests `GET /api/tenants/{tenantId}/audit` with a date range
**Then** the response returns matching audit records with actor, target, event type, timestamp, and tenant scope
**And** the result uses default page size 100 when no page size is provided

**Given** the caller requests a page size above 1,000
**When** the endpoint validates query parameters
**Then** the request is rejected or clamped according to the documented API contract
**And** the behavior is covered by tests

**Given** audit records span the requested date range boundaries
**When** the query executes
**Then** inclusive/exclusive boundary behavior is deterministic and documented in tests
**And** records are ordered consistently for pagination

**Given** the caller is not a global administrator or otherwise authorized audit viewer
**When** the audit endpoint is requested
**Then** the request is rejected
**And** no audit records are returned

**Given** audit query tests run
**When** multiple tenant histories exist
**Then** tests prove audit records are tenant-scoped and do not leak across tenants
**And** cursor pagination remains scope-bound to tenant and date range

### Story 3.6: Validate Projection Write Safety and Query Contracts

As a platform engineer,
I want tenant query projections and contracts to be safe under fan-in and concurrency,
So that discovery and audit views do not lose or leak data.

**Acceptance Criteria:**

**Given** tenant events from multiple aggregates feed a shared tenant index projection
**When** concurrent projection updates occur
**Then** the selected projection write policy prevents silent write loss
**And** the implementation uses verified `CachingProjectionActor` fan-in behavior or ETag-based retry/recovery fallback

**Given** projection write conflicts occur
**When** retry/recovery logic executes
**Then** conflicts are retried according to the configured policy
**And** exhausted retries produce diagnostics without corrupting projection state

**Given** query contracts are added or changed
**When** contract tests run
**Then** query contracts implement EventStore query contract requirements
**And** list response DTOs use the approved pagination shape

**Given** query endpoints are implemented
**When** controller tests run
**Then** controllers remain thin adapters for route/query validation, authenticated user extraction from `sub`, signed cursor validation, and `SubmitQuery` dispatch
**And** query authorization and row filtering remain in query handling rather than controller branching

**Given** Tier 2 or Tier 3 query tests run
**When** projection state and API responses are inspected
**Then** tests assert persisted/read-model end state, ETag or freshness behavior where applicable, and zero cross-tenant leakage

## Epic 4: Reactive Consumer Integration

Consuming services can subscribe to tenant events, build local projections, react to tenant changes, and handle event delivery idempotently.

### Story 4.1: Publish Tenant Domain Events as CloudEvents

As a consuming-service developer,
I want all tenant domain events published through DAPR pub/sub as CloudEvents,
So that my service can react to tenant changes through a standard event channel.

**Acceptance Criteria:**

**Given** a tenant lifecycle, membership, role, or configuration command succeeds
**When** the event is persisted
**Then** the event is published through DAPR pub/sub as a CloudEvents 1.0 message
**And** the topic name follows the documented `tenants.events` convention

**Given** a domain rejection occurs
**When** event publication logic evaluates the outcome
**Then** rejection events are treated as domain outcomes rather than infrastructure failures
**And** they are not dead-lettered as processing errors

**Given** an event payload is published
**When** a consumer inspects the CloudEvent
**Then** the payload contains the managed `TenantId`, event type, event ID, aggregate version, and enough context for idempotent handling
**And** envelope/platform tenant metadata does not replace the managed tenant ID in the payload

**Given** DAPR pub/sub is temporarily unavailable
**When** event persistence succeeds
**Then** persisted events remain the source of truth
**And** publication recovery behavior can republish from durable event state

**Given** Tier 2 event publication tests run
**When** tenant commands produce events
**Then** tests inspect persisted CloudEvent bodies and state-store evidence where available
**And** tests do not rely only on API response codes

### Story 4.2: Register Tenant Event Handlers in a Consuming Service

As a consuming-service developer,
I want to register tenant event handlers with minimal DI configuration,
So that my service can become tenant-aware without custom subscription plumbing.

**Acceptance Criteria:**

**Given** a consuming service references the Tenants Contracts and Client packages
**When** the developer registers tenant event handling in DI
**Then** the registration requires under 20 lines of configuration for the documented sample scenario
**And** the registration names the tenant event topic and handler types clearly

**Given** a tenant event arrives at the consumer endpoint
**When** the client subscription infrastructure receives it
**Then** the event envelope is deserialized with `System.Text.Json`
**And** the event is dispatched to the matching handler without accepting unknown event types silently

**Given** multiple tenant event handlers are registered
**When** events of different tenant event types arrive
**Then** each event is dispatched to the correct handler
**And** handler failures are surfaced through bounded diagnostics suitable for consumer troubleshooting

**Given** a handler registration is missing for a known event type
**When** the event arrives
**Then** the consumer behavior follows the documented default policy
**And** the event is not treated as successfully processed if required projection behavior is absent

**Given** client package tests run
**When** handler registration and dispatch are exercised
**Then** tests cover registration, known event dispatch, unknown event rejection/suggestion behavior, and failure diagnostics

### Story 4.3: Build Consumer Local Tenant Projection

As a consuming-service developer,
I want to build a local tenant projection from tenant events,
So that my service can enforce tenant-aware behavior without querying Tenants for every operation.

**Acceptance Criteria:**

**Given** a consumer receives `TenantCreated`, `TenantDisabled`, `TenantEnabled`, membership, role, and configuration events
**When** the consumer applies those events to a local projection
**Then** the projection stores tenant status, user roles, and relevant configuration for the consumer
**And** the projection uses the managed `TenantId` from event payloads

**Given** tenant events arrive more than once
**When** the consumer applies events using event ID or aggregate version metadata
**Then** duplicate delivery does not corrupt local projection state
**And** idempotency behavior is covered by tests

**Given** events arrive independently from other services' projections
**When** the local projection updates
**Then** the consumer does not assume cross-service processing order
**And** eventual consistency is reflected in documentation and sample comments

**Given** an event contains an unknown or unsupported contract version
**When** the consumer projection attempts to apply it
**Then** the consumer fails safely according to the documented policy
**And** diagnostics avoid logging raw payloads or sensitive data

**Given** consumer projection tests run
**When** event sequences are applied
**Then** tests cover ordered delivery, duplicate delivery, missing handler behavior, tenant ID isolation, and replay from event history

### Story 4.4: React to Tenant Membership and Role Changes

As a consuming-service developer,
I want my service to react to user addition, removal, and role-change events,
So that access is enforced or revoked automatically from tenant events.

**Acceptance Criteria:**

**Given** a `UserAddedToTenant` event is received
**When** the consuming service applies it
**Then** the user's tenant role is added to the local projection
**And** subsequent consumer authorization checks can allow behavior according to that role

**Given** a `UserRemovedFromTenant` event is received
**When** the consuming service applies it
**Then** the user's tenant access is removed from the local projection
**And** subsequent consumer authorization checks can reject tenant-scoped operations

**Given** a `UserRoleChanged` event is received
**When** the consuming service applies it
**Then** the user's role changes in the local projection
**And** role escalation boundaries are interpreted according to the tenant role contract

**Given** the same user belongs to multiple tenants
**When** a membership event is applied for one tenant
**Then** only that tenant's local projection row is changed
**And** no access is granted or revoked in unrelated tenants

**Given** the sample consumer runs
**When** a user is removed from a tenant
**Then** the sample demonstrates reactive access revocation without polling or per-service sync jobs
**And** this behavior is suitable for the adoption demo flow

### Story 4.5: React to Tenant Status and Configuration Changes

As a consuming-service developer,
I want my service to react to tenant disable/enable and configuration events,
So that tenant-specific behavior updates automatically.

**Acceptance Criteria:**

**Given** a `TenantDisabled` event is received
**When** the consuming service applies it
**Then** the local projection marks the tenant disabled
**And** consumer operations can block tenant-scoped work according to local policy

**Given** a `TenantEnabled` event is received
**When** the consuming service applies it
**Then** the local projection marks the tenant active
**And** consumer operations can resume according to local policy

**Given** a `TenantConfigurationSet` event is received
**When** the consuming service applies it
**Then** the local projection updates the namespaced configuration key
**And** consumer behavior can read the updated setting

**Given** a `TenantConfigurationRemoved` event is received
**When** the consuming service applies it
**Then** the local projection removes the configuration key
**And** no unrelated configuration keys are changed

**Given** status or configuration events are replayed
**When** projection tests run
**Then** tests cover disable, enable, set, remove, duplicate delivery, tenant isolation, and missing-key behavior

### Story 4.6: Document Idempotent Tenant Event Processing

As a consuming-service developer,
I want clear guidance for idempotent event processing,
So that my service handles DAPR at-least-once delivery safely.

**Acceptance Criteria:**

**Given** the idempotent event processing documentation is opened
**When** a developer reads the delivery model section
**Then** it explains DAPR at-least-once delivery and why duplicate event handling is required
**And** it states consumers must not assume cross-service ordering

**Given** a developer reads the implementation example
**When** they inspect the code sample
**Then** the sample deduplicates by event ID or aggregate version
**And** the sample shows an idempotent handler pattern suitable for local projections

**Given** a developer reads troubleshooting guidance
**When** duplicate, delayed, or failed event handling is described
**Then** guidance explains safe retry, local projection rebuild, and support-safe diagnostics
**And** raw payloads, tokens, and sensitive tenant/user data are not logged

**Given** documentation validation runs
**When** markdown/link checks execute
**Then** the idempotency guide links to event contract reference, cross-aggregate timing guidance, and sample consumer code
**And** examples compile or are validated through package-only consumer smoke tests where practical

### Story 4.7: Provide Sample Consuming Service

As a developer evaluating Tenants,
I want a sample consuming service that subscribes to tenant events,
So that I can copy the integration pattern into my own service.

**Acceptance Criteria:**

**Given** the sample consuming service is built
**When** a developer opens its registration code
**Then** it demonstrates tenant event handler registration in under 20 lines for the core scenario
**And** the code uses Contracts and Client packages rather than duplicating event schemas

**Given** the sample receives tenant membership, status, and configuration events
**When** sample handlers process the events
**Then** local projection state updates according to documented consumer behavior
**And** event handling remains idempotent under duplicate delivery

**Given** a user is removed from a tenant in the sample flow
**When** the removal event reaches the sample service
**Then** the sample blocks or revokes that user's tenant-scoped access in the sample domain
**And** this behavior can be used in the "aha moment" demo

**Given** the sample is built through CI/package validation
**When** package-only consumer validation runs
**Then** the sample restores and builds using package references
**And** it does not depend on source-project internals

**Given** sample documentation is generated or updated
**When** a developer follows it
**Then** prerequisites, registration, event handlers, local projection behavior, and idempotency expectations are clear enough to reproduce the integration

## Epic 5: Developer Adoption and Testing

Developers can install Tenants packages, register client services, test with production-parity in-memory fakes, and deploy with Aspire support.

### Story 5.1: Package Tenants for NuGet Adoption

As a developer,
I want Tenants distributed through clear NuGet packages,
So that I can reference only the package surface my service needs.

**Acceptance Criteria:**

**Given** the repository is packed for release
**When** package validation runs
**Then** exactly the approved Tenants packages are produced: Contracts, Client, Server, Testing, and Aspire
**And** host projects remain non-packable

**Given** a consumer project references `Hexalith.Tenants.Contracts`
**When** restore and build run
**Then** the consumer can use command, event, rejection, enum, identity, and query contracts
**And** the contracts package does not require host/runtime dependencies

**Given** a consumer project references Client, Server, Testing, or Aspire packages
**When** restore and build run
**Then** each package brings only its intended dependency surface
**And** no project-level `PackageReference` uses inline `Version=`

**Given** package metadata is validated
**When** CI runs package checks
**Then** package ID, version, authorship, repository metadata, symbols/source behavior, and dependency boundaries pass validation
**And** package-only consumer validation catches missing package dependencies

### Story 5.2: Register Tenant Client Services

As a developer,
I want to register tenant client services with one extension method,
So that application startup remains simple and consistent.

**Acceptance Criteria:**

**Given** a consuming application references the Client package
**When** the developer calls the tenant client registration extension
**Then** required client abstractions and configuration are registered in DI
**And** the registration does not require manual wiring of internal implementation types

**Given** the command endpoint path is not overridden
**When** client options are configured
**Then** the default command endpoint path aligns with EventStore `/api/v1/commands`
**And** documentation does not normalize it to an unversioned path without a proven gateway alias

**Given** invalid or missing client configuration is supplied
**When** application startup validates options
**Then** the failure is bounded and actionable
**And** no sensitive configuration values are logged

**Given** client registration tests run
**When** DI is built
**Then** required services resolve successfully
**And** registration tests cover default options, overridden options, and invalid options

### Story 5.3: Provide In-Memory Tenant Testing Fakes

As a developer,
I want to test tenant behavior with in-memory fakes,
So that my service tests can run without DAPR, Docker, or external infrastructure.

**Acceptance Criteria:**

**Given** a test project references `Hexalith.Tenants.Testing`
**When** the developer creates an in-memory tenant service using documented helpers
**Then** tenant commands can be executed in memory
**And** the setup fits the under-10-lines target for representative tests

**Given** in-memory fake commands execute
**When** events are produced
**Then** the fakes return the canonical EventStore `DomainResult`
**And** emitted event sequences can be asserted by consumer tests

**Given** a developer uses in-memory projection helpers
**When** tenant events are applied
**Then** local projection behavior is available for tenant-aware test scenarios
**And** projection-level isolation remains clearly documented as the consuming service's responsibility

**Given** fake execution is measured through tests
**When** representative command tests run
**Then** in-memory fake command/event execution stays within the 10ms target where practical
**And** tests avoid external infrastructure dependencies

### Story 5.4: Prove Production and Fake Domain Logic Conformance

As a developer relying on Tenants.Testing,
I want in-memory fakes to match production aggregate behavior,
So that test confidence is based on shared domain logic rather than mock drift.

**Acceptance Criteria:**

**Given** a command exists in `Hexalith.Tenants.Contracts.Commands`
**When** conformance tests enumerate tenant commands
**Then** each command is executed against the production aggregate and the in-memory fake
**And** both paths produce identical event sequences in identical order

**Given** a command produces a domain rejection
**When** conformance tests compare fake and production behavior
**Then** rejection event sequences match exactly
**And** rejection outcomes are included rather than skipped

**Given** a new tenant command is added
**When** conformance tests run
**Then** the new command is automatically included or the test fails until the fake supports it
**And** no skip is added for in-progress commands

**Given** projection fake support intentionally handles known events
**When** a new success event is added
**Then** projection conformance tests fail until the event is wired
**And** silent default handling does not hide projection drift

### Story 5.5: Provide Aspire Hosting Extensions

As a developer,
I want to deploy or orchestrate Tenants with Aspire hosting extensions,
So that local and distributed topology can be composed consistently with EventStore and DAPR.

**Acceptance Criteria:**

**Given** an Aspire AppHost references the Tenants Aspire package
**When** the developer adds Tenants to the app model
**Then** Tenants, DAPR sidecar configuration, and relevant dependencies can be composed through the hosting extension
**And** the extension follows current Aspire package/version constraints

**Given** the AppHost topology is configured
**When** local execution starts from `src/Hexalith.Tenants.AppHost`
**Then** required Tenants resources, DAPR components, and service invocation paths are available
**And** AppHost topology changes require restart as documented

**Given** a consuming AppHost uses Tenants hosting extensions
**When** package-only consumer validation runs
**Then** the consuming AppHost restores and builds using packages
**And** no source-project internals are required

**Given** Aspire integration behavior is tested
**When** relevant integration or AppHost tests run
**Then** tests validate resource names, DAPR component wiring, domain processor endpoint availability, and health/readiness behavior where practical

### Story 5.6: Validate Developer Adoption Surface

As a developer evaluating Tenants,
I want package, client, testing, and hosting examples to work together,
So that I can adopt Tenants without discovering missing integration steps.

**Acceptance Criteria:**

**Given** adoption examples are present in docs or samples
**When** package-only consumer smoke tests run
**Then** examples for package installation, client registration, in-memory tests, and Aspire hosting restore and build
**And** examples do not depend on repository-local paths or untracked generated artifacts

**Given** a developer follows the install and first-use path
**When** the documented package references are added
**Then** central package management guidance remains clear for this repository
**And** consumer-facing package references are valid for normal NuGet consumers

**Given** source APIs change in Client, Testing, or Aspire packages
**When** compatibility tests and sample builds run
**Then** breaking changes are detected before release
**And** documentation is updated alongside intentional API changes

## Epic 6: Production Operations and Release Readiness

Platform operators can deploy, observe, scale, and release Tenants through DAPR, OpenTelemetry, stateless runtime behavior, and CI/CD quality gates.

### Story 6.1: Deploy Tenants Alongside EventStore With DAPR

As a platform operator,
I want to deploy Tenants alongside EventStore using standard DAPR configuration,
So that tenant management fits the existing Hexalith runtime model.

**Acceptance Criteria:**

**Given** Tenants is deployed with EventStore
**When** the runtime starts
**Then** DAPR sidecars provide actors, state store, pub/sub, and service invocation required by Tenants
**And** Tenants domain code does not directly depend on Redis, databases, or brokers

**Given** the Tenants host receives EventStore aggregate actor callbacks
**When** the domain service invocation path is exercised
**Then** the `/process` domain processor route is available
**And** DAPR access control allows only intended caller app IDs

**Given** DAPR components are configured
**When** deployment validation runs
**Then** component names align with `statestore`, `tenants.events`, and `deadletter.tenants.events`
**And** resource names are convention-derived rather than hand-coded inconsistently

**Given** DAPR infrastructure is incomplete or unavailable
**When** health/readiness checks run
**Then** the operator receives bounded diagnostics that identify missing DAPR dependencies
**And** the service does not report ready before required runtime dependencies are usable

### Story 6.2: Expose Tenant Command and Event Metrics

As a platform operator,
I want tenant command and event processing metrics exposed through OpenTelemetry,
So that I can monitor latency and processing health.

**Acceptance Criteria:**

**Given** tenant commands are processed
**When** OpenTelemetry spans and metrics are exported
**Then** tenant command latency is measurable
**And** p95 latency can be evaluated against the 50ms target

**Given** tenant events are published or processed
**When** OpenTelemetry metrics are exported
**Then** event processing latency and publication behavior are observable
**And** p95 event publication can be evaluated against the 50ms target

**Given** command/event telemetry is emitted
**When** logs and traces are inspected
**Then** telemetry includes correlation, tenant, domain, aggregate, causation, command/event type, and stage metadata where appropriate
**And** telemetry does not log raw command payloads, event payloads, tokens, secrets, or sensitive tenant/user data

**Given** telemetry tests or smoke checks run
**When** representative command and event flows execute
**Then** required telemetry attributes are present
**And** failure/rejection paths remain distinguishable from infrastructure errors

### Story 6.3: Operate Tenants as a Stateless Service

As a platform operator,
I want Tenants to be stateless between requests,
So that I can restart, scale, and redeploy without data loss.

**Acceptance Criteria:**

**Given** tenant events are stored in EventStore
**When** a Tenants service instance restarts
**Then** aggregate and projection state can be reconstructed from durable event history and snapshots
**And** no tenant state is held only in process memory

**Given** multiple Tenants instances run concurrently
**When** tenant commands are submitted
**Then** EventStore/DAPR actor and concurrency behavior preserve single-aggregate consistency
**And** duplicate or conflicting command outcomes remain deterministic

**Given** snapshot configuration is loaded
**When** the tenants domain is configured
**Then** the tenants snapshot interval is 50 events
**And** global administrator state uses the EventStore default unless evidence requires otherwise

**Given** startup reconstruction performance evidence is collected
**When** the scheduled benchmark seeds up to 500,000 events
**Then** readiness is measured against the 30-second target
**And** advanced snapshot tuning remains deferred unless the target is missed

### Story 6.4: Verify Production Authentication Readiness

As a platform operator,
I want production authentication assumptions verified,
So that Tenants is not deployed with fail-open tenant or role behavior.

**Acceptance Criteria:**

**Given** production JWT/OIDC configuration is supplied
**When** Tenants validates authenticated requests
**Then** platform tenant claim `eventstore:tenant=system` is required for tenant-management operations
**And** user identity is derived from `sub`, not `name` or `email`

**Given** global administrator authority is evaluated
**When** commands are processed
**Then** authority comes from trusted server-side command-envelope metadata
**And** client-submitted authority extensions are ignored or sanitized

**Given** authentication or claim configuration is invalid
**When** startup or smoke tests run
**Then** failures are explicit and bounded
**And** the system does not silently accept unsafe default authority

**Given** production auth readiness tests run
**When** valid and invalid tokens are exercised
**Then** tests cover platform tenant claim, subject casing, missing claims, unauthorized tenant access, and global-admin override behavior

### Story 6.5: Enforce CI/CD Quality Gates

As a maintainer,
I want CI/CD to enforce build, test, coverage, and package gates,
So that release artifacts are reliable before NuGet publication.

**Acceptance Criteria:**

**Given** a push or pull request targets main
**When** CI runs
**Then** restore, Release build, Tier 1 tests, Tier 2 tests where configured, package metadata validation, and package-only consumer validation execute
**And** failures block release progress

**Given** coverage is collected
**When** CI evaluates coverage gates
**Then** overall line coverage stays above the required threshold
**And** tenant isolation/auth logic branch coverage reaches the required 100% target

**Given** release is triggered by merge to main
**When** semantic-release runs
**Then** versioning follows Conventional Commits
**And** the release packs and validates the five approved NuGet packages before publishing

**Given** a package or public API boundary changes
**When** package validation and consumer smoke tests run
**Then** package dependency, metadata, and consumer compatibility issues are detected before publication
**And** CHANGELOG/GitHub release behavior remains tied to semantic-release output

### Story 6.6: Validate Production Availability and Failure Behavior

As a platform operator,
I want health, readiness, and failure behavior to be observable,
So that Tenants can meet production availability expectations.

**Acceptance Criteria:**

**Given** Tenants is deployed in production-like topology
**When** health check monitoring polls the service
**Then** API availability can be measured against the 99.9% target
**And** readiness does not go green before required EventStore/DAPR dependencies are ready

**Given** DAPR pub/sub is unavailable
**When** tenant commands persist events successfully
**Then** command processing remains successful where EventStore persistence succeeds
**And** publication recovery or catch-up behavior is observable after pub/sub recovery

**Given** infrastructure failures occur
**When** structured logs and ProblemDetails responses are produced
**Then** infrastructure failures are distinguishable from domain rejections
**And** responses avoid raw payloads, tokens, local paths, and sensitive tenant/user data

**Given** deployment smoke tests run
**When** command, query, health, readiness, and pub/sub recovery paths are exercised
**Then** tests produce evidence suitable for release readiness decisions
**And** any unavailable infrastructure paths are explicitly documented rather than silently skipped

## Epic 7: Documentation and Adoption Proof

Developers can reach first command quickly, understand tenant event contracts, design for timing windows, use compensating commands, and see the reactive access model demonstrated.

### Story 7.1: Write Quickstart to First Tenant Command

As a developer evaluating Tenants,
I want a quickstart that gets me to my first tenant command within 30 minutes,
So that I can validate the platform before committing to integration.

**Acceptance Criteria:**

**Given** a developer opens the quickstart
**When** they follow the setup steps
**Then** they can validate DAPR sidecar readiness, EventStore deployment, Tenants host availability, and required configuration
**And** missing prerequisites produce clear remediation steps

**Given** prerequisites are satisfied
**When** the developer follows the command submission example
**Then** they can submit a first `CreateTenant` command through the documented command endpoint
**And** the example uses the correct `/api/v1/commands` path unless a deployed gateway alias is explicitly documented

**Given** the command succeeds
**When** the developer inspects the result
**Then** the quickstart shows how to verify the command outcome through event or query evidence
**And** the guide explains expected rejection behavior for common setup mistakes

**Given** quickstart validation runs
**When** docs or sample checks execute
**Then** code snippets, package references, configuration keys, and links are valid
**And** the guide remains achievable within the 30-minute target for a prepared Hexalith environment

### Story 7.2: Publish Event Contract Reference

As a consuming-service developer,
I want a complete tenant event contract reference,
So that I know which events to subscribe to and how to deserialize them safely.

**Acceptance Criteria:**

**Given** a developer opens the event contract reference
**When** they inspect tenant lifecycle, membership, role, configuration, global administrator, and rejection contracts
**Then** each command, event, and rejection schema is documented
**And** required fields, managed `TenantId`, timestamps, event ID, aggregate version, and enum behavior are described

**Given** event contracts are published as CloudEvents
**When** the reference describes the event channel
**Then** the topic name, CloudEvents 1.0 behavior, DAPR at-least-once delivery, and consumer filtering expectations are documented
**And** consumers are warned not to treat envelope tenant as the managed tenant ID

**Given** contracts evolve before or after v1.0
**When** the reference describes compatibility
**Then** pre-v1.0 breaking changes and post-v1.0 backward-compatibility expectations are clear
**And** unsafe enum default behavior is documented as fail-closed

**Given** documentation validation runs
**When** contract reference examples are checked
**Then** examples align with current contract source
**And** links to quickstart, idempotency, timing, and sample consumer docs remain valid

### Story 7.3: Produce Reactive Access Revocation Demo

As a developer evaluating the Tenants model,
I want a short demo showing reactive cross-service revocation,
So that I can understand the value faster than reading architecture docs.

**Acceptance Criteria:**

**Given** the demo scenario starts
**When** a tenant is created and a user is added with TenantContributor role
**Then** at least one subscribing service receives and reflects the `UserAddedToTenant` event
**And** the demo makes the event-driven path visible without exposing sensitive internals

**Given** the user is removed from the tenant
**When** `UserRemovedFromTenant` is published and consumed
**Then** subscribing services revoke or block access in the demo flow
**And** the visual or scripted evidence shows no polling or sync job is required

**Given** the demo shows audit or event history
**When** the viewer inspects the result
**Then** the demo proves who acted, what changed, and when
**And** the evidence uses support-safe references rather than raw payloads or tokens

**Given** the demo artifact is published as screencast, video, or scripted sample
**When** documentation links to it
**Then** setup assumptions, expected duration, and runnable commands are documented
**And** the demo remains aligned with current package and endpoint behavior

### Story 7.4: Document Cross-Aggregate Timing Behavior

As a consuming-service developer,
I want cross-aggregate timing behavior documented,
So that I can design safely for eventual consistency between Tenants and subscriber services.

**Acceptance Criteria:**

**Given** a developer reads the timing guide
**When** the guide explains tenant commands and subscriber processing
**Then** it describes the event propagation window between Tenants aggregate decisions and consumer projection updates
**And** it clarifies that consuming-service commands may briefly use stale local projections

**Given** the guide includes a sequence diagram
**When** a user removal or tenant disable scenario is shown
**Then** the diagram distinguishes command submission, event persistence, pub/sub delivery, subscriber projection update, and later consumer enforcement
**And** it does not imply synchronous downstream revocation unless such evidence exists

**Given** developers need mitigation guidance
**When** the guide explains design options
**Then** it recommends idempotent consumers, local projection checks, retry/status review, and eventual-consistency-safe UX
**And** it references the planned EventStore authorization plugin as the synchronous enforcement option where appropriate

**Given** docs validation runs
**When** the timing guide is checked
**Then** links to idempotent event processing, event contract reference, demo, and production auth guidance remain valid
**And** the guide avoids raw internal metadata or sensitive examples

### Story 7.5: Document Compensating Command Patterns

As a global administrator or tenant owner,
I want correction patterns documented as compensating commands,
So that mistakes can be repaired without hiding audit history.

**Acceptance Criteria:**

**Given** a user reads the compensating commands guide
**When** it defines correction behavior
**Then** it explains that corrections are new explicit commands, not hidden undo
**And** original and corrective events both remain in audit history

**Given** a user was wrongly removed from a tenant
**When** the guide shows a worked example
**Then** it demonstrates restoring intended access with `AddUserToTenant`
**And** it explains why the role must be explicitly specified rather than auto-restored

**Given** a role changed between mistake and correction
**When** the guide explains the decision point
**Then** it tells operators to inspect event history before selecting the corrective role
**And** it avoids implying that Tenants infers business intent automatically

**Given** command examples are included
**When** docs validation runs
**Then** examples match current command contracts and endpoint shape
**And** links to audit query, event contract reference, and quickstart remain valid

### Story 7.6: Maintain Adoption Documentation Quality

As a maintainer,
I want adoption documentation to stay validated with implementation,
So that developers do not follow stale package, endpoint, or runtime guidance.

**Acceptance Criteria:**

**Given** README, quickstart, event reference, timing guide, compensating guide, demo docs, and sample docs exist
**When** documentation validation runs
**Then** markdown formatting, links, referenced files, package IDs, endpoint paths, and sample snippets are checked
**And** failures block release or are explicitly tracked

**Given** package, endpoint, or command contracts change
**When** docs are updated
**Then** affected quickstart, event reference, samples, and demo instructions are updated in the same change
**And** the docs continue to describe Tenants as consuming shared EventStore infrastructure rather than owning reusable runtime infrastructure once extraction APIs exist

**Given** a developer reads adoption docs
**When** they compare docs to actual package behavior
**Then** terminology, role names, topic names, DAPR component names, and command endpoints are consistent
**And** prerequisite failures and common rejection responses are described in actionable language

## Epic 8: Phase 2 Access Administration UI Readiness

Administrators get a safe Operations Shell plan for tenant access review, projection freshness, command lifecycle, audit proof, accessibility, localization, and `RemoveUserFromTenant` as the first command-capable slice.

### Story 8.1: Define Operations Shell Navigation and Layout

As a global administrator,
I want a calm Operations Shell for tenant administration,
So that I can move between tenants, users, global administrators, and audit evidence without losing context.

**Acceptance Criteria:**

**Given** the Phase 2 UI shell is designed or implemented
**When** the user opens the administration surface
**Then** the shell provides navigation for Tenants, Users, Global Administrators, and Audit
**And** the tenant list is the default operational triage surface

**Given** the shell uses Fluent UI Blazor and FrontComposer
**When** implementation starts
**Then** exact component APIs are verified against the project-pinned Fluent UI Blazor v5 package
**And** generated FrontComposer patterns are used only where source-of-truth boundaries are clear

**Given** the user moves from tenant list to detail, access review, command preview, or audit
**When** they return
**Then** selected tenant, filters, pagination, and relevant context are preserved
**And** command lifecycle is shown inside the affected workflow rather than as separate primary navigation

**Given** layout design is reviewed
**When** the shell is assessed
**Then** it uses dense, full-width operational surfaces
**And** it avoids decorative card-heavy dashboards, marketing-style layout, and non-operational visual ornament

### Story 8.2: Build Tenant Triage List With Freshness Signals

As a global administrator,
I want the tenant list to surface access-risk signals and projection freshness,
So that I can decide where to investigate first.

**Acceptance Criteria:**

**Given** tenant projection data is available
**When** the tenant list renders
**Then** it displays tenant status, member count, owner count, warning indicators, pending command state, and projection freshness
**And** filtering, sorting, and pagination are available where backed by trustworthy query data

**Given** projection freshness is known
**When** a tenant row is displayed
**Then** a Truth State Badge or equivalent state indicator communicates current, refreshing, delayed, stale, or unable-to-verify state
**And** color is secondary to text, accessible labels, and structure

**Given** projection freshness is stale or unknown
**When** the user considers an access-impacting action from the tenant context
**Then** a Freshness Gate blocks or adds explicit freshness friction
**And** the user can refresh, wait, inspect audit, or continue read-only according to the state

**Given** tenant list table states occur
**When** data is loading, empty, filtered empty, unauthorized, stale, failed to load, degraded, or not yet projected
**Then** each state is visually and semantically distinct
**And** row action dimensions remain stable across state changes

### Story 8.3: Design Tenant Access Review and User Lookup Surfaces

As an administrator or tenant owner,
I want to inspect tenant membership and user assignments,
So that I can answer who has access and why before changing access.

**Acceptance Criteria:**

**Given** a user opens tenant detail
**When** detail context renders
**Then** overview, member access, configuration, command state, and audit evidence entry points are available
**And** tenant identity, status, role context, and freshness remain visible

**Given** a user opens member access review
**When** the member table renders
**Then** it shows user ID, assigned role, owner risk context, row actions, and freshness state
**And** row actions remain close to the affected user and tenant context

**Given** an access question starts from a user
**When** the user lookup path is used
**Then** exact user lookup can find visible tenant assignments
**And** user lookup does not imply broader directory search unless an external directory requirement exists

**Given** global administrator management surfaces are planned
**When** platform governance actions are displayed
**Then** global administrator list and management planning surfaces use stronger friction for platform recovery risk
**And** unavailable actions explain missing permission, stale data, backend unavailability, or high-impact flow readiness

### Story 8.4: Implement Truth State and Command Lifecycle Primitives

As an administrator,
I want command and projection states to be explicit,
So that I know whether I am seeing truth, waiting for truth, or unable to verify truth.

**Acceptance Criteria:**

**Given** UI state primitives are implemented
**When** freshness, authorization, command lifecycle, projection confirmation, or audit evidence states are shown
**Then** Truth State Badge semantics are reused consistently across list, detail, member table, command feedback, and audit
**And** states include current, refreshing, aging/stale, unknown, eligible, blocked, pending, accepted, confirmed, failed, audit pending, and audit available

**Given** command lifecycle is displayed
**When** a command progresses
**Then** the UI distinguishes request sent, accepted request, projection pending, confirmed access update, rejected, already applied, failed transport, degraded, unable to verify, audit pending, and audit available
**And** it never collapses accepted, projected, and proven into one generic success state

**Given** SignalR or projection notifications arrive
**When** the UI handles them
**Then** notifications act as freshness nudges that trigger re-query or reconciliation
**And** notification payloads are not treated as durable projection truth

**Given** confirmed projection data exists before a command is submitted
**When** the command enters pending or confirming state
**Then** the UI preserves last confirmed projection data
**And** pending hints are visually distinct from source-of-truth values

### Story 8.5: Design RemoveUserFromTenant Command Slice

As a global administrator or tenant owner,
I want to remove tenant access through a consequence-aware command flow,
So that access revocation is careful, verifiable, and recoverable.

**Acceptance Criteria:**

**Given** a user launches remove access from a member row
**When** the consequence preview opens
**Then** it shows tenant, target user, current role, owner count, known consequences, known unknowns, freshness, recovery path, and audit expectation
**And** the preview does not claim downstream session revocation, token invalidation, or consumer enforcement without backend evidence

**Given** the remove-user flow is modeled
**When** state transitions are defined
**Then** the model follows `eligible -> previewed -> submitted -> accepted -> projection_pending -> confirmed | failed | unknown | audit_pending | audit_available`
**And** each state has visible copy, enabled/disabled actions, retry/status behavior, and support-safe reference behavior where available

**Given** unknown freshness, incomplete consequence inputs, indeterminate authorization, or missing lifecycle support exists
**When** the user attempts removal
**Then** destructive action is blocked by default unless an approved override path exists
**And** the unavailable reason is visible and safely worded

**Given** removing the user may leave the tenant ownerless
**When** the flow detects last-owner risk
**Then** elevated friction shows risk explanation, affected scope, evidence freshness, audit consequence, and intentional confirmation
**And** the flow warns rather than inventing a backend invariant not present in the domain

**Given** the user submitted removal incorrectly
**When** recovery is needed
**Then** the UI guides toward an explicit compensating command
**And** correction is not labeled as undo

### Story 8.6: Provide Audit Evidence and Flat Audit Fallback

As an auditor or administrator,
I want access-changing workflows to end in audit evidence,
So that I can prove what happened later.

**Acceptance Criteria:**

**Given** a meaningful access change is accepted or confirmed
**When** audit evidence is available
**Then** the UI provides an Audit Evidence Receipt with actor, target, tenant scope, outcome, timestamp, projection marker, and support-safe audit reference
**And** the user can navigate from command result to audit proof

**Given** audit evidence is delayed, unavailable, or not implemented
**When** the workflow reaches proof state
**Then** the UI explains audit pending, delayed, unavailable, or approved fallback state
**And** it avoids presenting completed proof before evidence exists

**Given** a reusable audit timeline is not ready
**When** the first audit UI slice is implemented
**Then** a Flat Audit List fallback uses DataGrid with stable ordering, filters, loading/empty/error states, and accessible expansion
**And** the fallback can filter by tenant, user, event type, or date where query support exists

**Given** support-safe references are displayed or copied
**When** audit or command evidence is shown
**Then** raw command payloads, bearer tokens, stack traces, aggregate IDs, internal correlation IDs, raw EventStore metadata, local paths, and sensitive tenant/user data are not exposed

### Story 8.7: Apply Fluent Visual System and Responsive Rules

As an administrator,
I want the UI to remain readable, stable, and operational across supported screen sizes,
So that access review stays clear during long-running admin work.

**Acceptance Criteria:**

**Given** visual tokens are defined
**When** tenant status, roles, projection freshness, command lifecycle, destructive actions, audit availability, degraded state, or unable-to-verify state is shown
**Then** semantic Fluent tokens are used
**And** no separate branded palette or color-only status language is introduced

**Given** typography and spacing are implemented
**When** tables, panels, dialogs, command previews, and audit surfaces render
**Then** headings remain modest and container-appropriate
**And** stable dimensions prevent layout shift in toolbars, badges, row actions, lifecycle panels, and action cells

**Given** the UI is viewed at desktop widths of 1024px and above
**When** the layout renders
**Then** persistent shell navigation, DataGrid layouts, side panels, and compact summaries are usable
**And** wide desktop behavior remains scan-friendly

**Given** the UI is viewed at tablet or mobile widths
**When** content adapts
**Then** tenant/user identity, status, freshness, read-only summary, audit/support-safe references, and degraded messaging remain visible
**And** high-impact access changes are discouraged or unavailable on very small screens unless all safety context remains visible

### Story 8.8: Validate Accessibility and Localization Readiness

As an administrator using assistive technology or localized UI,
I want access workflows to remain perceivable and operable,
So that safety does not depend on color, mouse hover, or English-only sentence fragments.

**Acceptance Criteria:**

**Given** interactive UI elements are implemented
**When** keyboard-only navigation is tested
**Then** all interactive elements are reachable in visual/task order
**And** focus indicators are visible in normal, high-contrast, and forced-colors modes

**Given** disabled or unavailable actions are present
**When** a keyboard or screen-reader user encounters them
**Then** readable reasons are exposed without requiring mouse hover
**And** missing permission, stale data, unsupported lifecycle, and missing audit proof remain distinguishable

**Given** command lifecycle changes occur
**When** live-region behavior is tested
**Then** submitted, accepted, projection pending, rejected, unable-to-verify, audit pending, and audit available states are announced with appropriate politeness
**And** assertive announcements are reserved for failures and high-risk blockers

**Given** dialogs or command previews are modal
**When** accessibility tests run
**Then** focus is trapped while modal, safe escape behavior works, and focus returns to the launching row or action
**And** reduced-motion users do not depend on animation for lifecycle meaning

**Given** state labels, role names, timestamps, warnings, disabled reasons, and recovery actions are localized
**When** localization review runs
**Then** text is localizable without concatenated sentence fragments
**And** timestamps include exact accessible text rather than only relative labels

**Given** responsive and accessibility evidence is collected
**When** acceptance checks run
**Then** tests cover stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, permission missing, screen reader review, forced colors, reduced motion, contrast, live regions, and focus return

## Epic 9: Shared Domain-Service Infrastructure Extraction

Future EventStore-backed domain-service developers can reuse shared hosting, projection, cursor, subscription, testing, and UI primitives while Tenants returns to focused domain ownership.

### Story 9.1: Define Shared Infrastructure Ownership and Compatibility Plan

As an architect,
I want clear ownership and compatibility rules for shared domain-service infrastructure,
So that extraction improves reuse without breaking Tenants behavior or package consumers.

**Acceptance Criteria:**

**Given** reusable Tenants mechanics are identified
**When** the extraction plan is written
**Then** each candidate is assigned to EventStore, Commons, FrontComposer, or Tenants ownership
**And** Tenants-owned domain contracts, aggregates, projections, query semantics, support wording, adapters, and adoption docs remain explicitly in Tenants

**Given** a candidate shared API affects package dependencies
**When** compatibility policy is defined
**Then** public API, NuGet dependency, submodule pointer, and semantic-release impact are documented
**And** breaking changes require explicit approval before implementation

**Given** completed Tenants story history exists
**When** the extraction workstream is planned
**Then** completed story records are not rewritten
**And** the new workstream supersedes long-term ownership assumptions only for future work

**Given** root-level submodules are involved
**When** extraction planning references cross-repo changes
**Then** only root-level submodules are initialized or updated
**And** recursive/nested submodule operations remain disallowed

### Story 9.2: Extract Generic Commons Primitives

As a shared-platform developer,
I want generic pagination and validation primitives moved to Commons where appropriate,
So that domain services do not duplicate infrastructure-neutral helper types.

**Acceptance Criteria:**

**Given** a helper has no Tenants or EventStore dependency
**When** extraction candidates are reviewed
**Then** generic `PaginatedResult<T>` and small reusable validation/options helpers are eligible for Commons
**And** EventStore-specific cursor signing or query-envelope behavior is not moved to Commons accidentally

**Given** a Commons primitive is implemented
**When** Tenants consumes it
**Then** Tenants behavior and public response shape remain equivalent unless an approved API change says otherwise
**And** package dependency changes are reflected in central package management and package validation

**Given** Commons tests run
**When** generic primitives are validated
**Then** tests cover serialization shape, nullability, boundary cases, and consumer usage
**And** Tenants tests continue to pass against the shared primitive

**Given** documentation is updated
**When** package and architecture docs describe pagination/result ownership
**Then** Commons ownership is clear
**And** Tenants docs no longer imply ownership of generic helpers after migration

### Story 9.3: Extract EventStore Hosting and Runtime Primitives

As an EventStore-backed domain-service developer,
I want reusable hosting and runtime glue in EventStore,
So that new domain services can expose standard EventStore integration surfaces with less boilerplate.

**Acceptance Criteria:**

**Given** Tenants contains reusable hosting/runtime mechanics
**When** extraction candidates are reviewed
**Then** ServiceDefaults patterns, DAPR state-store health checks, domain-service route mapping, `/process` route wiring, projection endpoint wiring, telemetry conventions, and startup helpers are evaluated for EventStore ownership
**And** tenant-specific configuration or domain wording remains in Tenants

**Given** EventStore hosting APIs are implemented
**When** Tenants migrates to them
**Then** Tenants startup, health, readiness, domain processor route, and telemetry behavior remain functionally equivalent
**And** AppHost/local orchestration behavior remains validated

**Given** another domain service consumes the shared hosting API
**When** sample or fitness tests are run
**Then** the new domain service can wire EventStore domain-service hosting with materially less boilerplate than Tenants originally required
**And** DAPR access control remains deny-by-default and explicitly configured

**Given** hosting extraction changes package dependencies
**When** package-only consumer validation runs
**Then** Tenants and EventStore consumers restore and build
**And** dependency changes are documented for adopters

### Story 9.4: Extract EventStore Query and Projection Infrastructure

As an EventStore-backed domain-service developer,
I want shared cursor, pagination, projection write, and recovery infrastructure,
So that query and projection correctness does not have to be reimplemented per domain service.

**Acceptance Criteria:**

**Given** Tenants query/projection helpers are reviewed
**When** reusable mechanics are selected
**Then** cursor codec pattern, pagination policy, cursor scope validation, projection write policy, DAPR projection state-store adapter, ETag retry/recovery behavior, and projection write diagnostics are assigned to EventStore where domain-service specific
**And** tenant-specific query authorization/filtering remains in Tenants

**Given** shared projection write APIs are implemented
**When** concurrent fan-in events update a shared projection
**Then** write safety prevents silent data loss
**And** retries, exhausted conflicts, and recovery diagnostics are covered by tests

**Given** shared cursor APIs are implemented
**When** Tenants query endpoints use them
**Then** cursors remain signed, opaque, scope-bound, and replay-resistant across query shapes
**And** existing tenant query behavior remains equivalent

**Given** Tenants migrates projection/query infrastructure
**When** query and projection tests run
**Then** tenant list, detail, users, user-tenants, audit, pagination, ETag/freshness, and isolation tests pass
**And** package/architecture docs describe EventStore ownership for shared mechanics

### Story 9.5: Extract EventStore Client Subscription Infrastructure

As a consuming-service developer,
I want generic event subscription and dispatch mechanics in shared EventStore client infrastructure,
So that domain event consumers can reuse reliable subscription behavior.

**Acceptance Criteria:**

**Given** Tenants client subscription mechanics are reviewed
**When** reusable parts are selected
**Then** generic event subscription endpoint, envelope processor, idempotent dispatch, handler lookup, and local projection application mechanics are assigned to EventStore.Client or another shared client module
**And** tenant event schemas and tenant-specific handler examples remain in Tenants

**Given** shared client subscription APIs are implemented
**When** Tenants Client migrates to them
**Then** tenant handler registration, dispatch, idempotency, and diagnostics remain functionally equivalent
**And** public behavior stays stable unless an approved API change is documented

**Given** a different domain event package consumes the shared client APIs
**When** subscription fitness tests run
**Then** known event dispatch, unknown event behavior, duplicate delivery, handler failure, and projection application behavior work without Tenants-specific code
**And** diagnostics remain bounded and support-safe

**Given** package-only consumer validation runs
**When** client dependency changes are introduced
**Then** Tenants sample consuming service restores and builds
**And** docs explain the new shared client ownership

### Story 9.6: Extract EventStore Testing Harness and Conformance Helpers

As a developer building EventStore-backed domain services,
I want reusable in-memory domain-service testing and conformance helpers,
So that fake/aggregate parity can be proven without custom harnesses in every service.

**Acceptance Criteria:**

**Given** Tenants testing helpers are reviewed
**When** reusable mechanics are selected
**Then** in-memory aggregate/domain-service harness patterns and conformance helper utilities are assigned to EventStore testing infrastructure
**And** tenant command fixtures, tenant-specific assertions, and tenant projection expectations remain in Tenants

**Given** shared testing helpers are implemented
**When** Tenants Testing migrates to them
**Then** existing fake behavior and `DomainResult` outcome semantics remain equivalent
**And** conformance tests still compare production aggregate and fake event sequences exactly

**Given** a new EventStore-backed domain service uses the shared testing helpers
**When** sample conformance tests are written
**Then** the service can prove fake/aggregate parity with materially less custom test harness code
**And** skipped/in-progress commands are not silently excluded

**Given** tests run after migration
**When** Tenants Testing and consumer package smoke tests execute
**Then** in-memory tests remain infrastructure-free and fast
**And** projection drift guards still fail when new success events are not wired

### Story 9.7: Promote Reusable FrontComposer Operational Primitives

As a UI platform developer,
I want reusable operational UI primitives promoted into FrontComposer where appropriate,
So that future domain-service admin UIs can share freshness, action, data, and shell patterns.

**Acceptance Criteria:**

**Given** Tenants UI planning identifies reusable patterns
**When** FrontComposer extraction candidates are reviewed
**Then** shell, DataGrid, freshness, action availability, command lifecycle, audit fallback, and support-safe reference primitives are considered for FrontComposer ownership
**And** tenant-specific domain wording, command availability rules, mappings, and access semantics remain in a Tenants adapter layer

**Given** a reusable FrontComposer primitive is implemented
**When** Tenants UI planning or implementation consumes it
**Then** Operations Shell and access review behavior remain aligned with the UX spec
**And** the primitive does not require reshaping immutable Tenants domain contracts

**Given** accessibility and localization requirements apply
**When** reusable UI primitives are validated
**Then** keyboard, focus, live-region, forced-colors, reduced-motion, and localization behavior are documented and tested
**And** Tenants-specific high-impact workflows can still use custom overrides

**Given** UI documentation is updated
**When** FrontComposer and Tenants docs describe ownership
**Then** shared UI primitives are attributed to FrontComposer
**And** Tenants remains responsible for tenant-specific workflow decisions

### Story 9.8: Migrate Tenants to Shared APIs and Update Artifacts

As a maintainer,
I want Tenants migrated to shared infrastructure APIs after they exist,
So that Tenants code visibly centers on domain behavior while runtime behavior remains stable.

**Acceptance Criteria:**

**Given** shared Commons, EventStore, and FrontComposer APIs are available
**When** Tenants migration is performed
**Then** copied or local reusable infrastructure is replaced with shared-module calls
**And** Tenants retains tenant contracts, aggregates, states, projections, query semantics, authorization, support wording, adapters, and tenant docs

**Given** migration changes source or package dependencies
**When** validation runs
**Then** Tenants host startup, command processing, query behavior, projection behavior, client subscription, testing fakes, and Aspire hosting remain functionally equivalent
**And** package public behavior remains stable except for explicitly approved dependency/API changes

**Given** shared-module migration is complete
**When** artifacts are updated
**Then** PRD wording, architecture ownership language, README/package descriptions, package validation scripts, solution/package governance tests, consumer smoke tests, deployment docs, and adoption docs describe shared-module ownership accurately
**And** completed implementation story files remain historical records

**Given** a new EventStore-backed domain aggregate project is started after migration
**When** it consumes the shared APIs
**Then** it requires materially less boilerplate than Tenants originally contained
**And** evidence is captured to prove the extraction achieved its reuse goal
