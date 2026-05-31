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
---

# Tenants - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Tenants, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: A global administrator can create a new tenant with a unique identifier and name (in MVP, tenant creation is restricted to global administrators).

FR2: A developer can update a tenant's metadata (name, description).

FR3: A global administrator can disable a tenant, preventing all commands against that tenant from succeeding.

FR4: A global administrator can re-enable a previously disabled tenant, restoring normal command processing.

FR5: The system produces a domain event for every tenant lifecycle change (created, updated, disabled, enabled).

FR6: A tenant owner can add a user to a tenant with a specified role (TenantOwner, TenantContributor, or TenantReader).

FR7: A tenant owner can remove a user from a tenant.

FR8: A tenant owner can change a user's role within a tenant.

FR9: The system rejects adding a user who is already a member of the tenant.

FR10: The system rejects role changes that violate escalation boundaries (a tenant owner cannot assign GlobalAdministrator).

FR11: The system produces a domain event for every user-role change (added, removed, role changed).

FR12: The system enforces optimistic concurrency, rejecting conflicting concurrent modifications to the same aggregate.

FR13: An existing global administrator can designate a user as a global administrator.

FR14: An existing global administrator can remove a user's global administrator status and cannot remove self if they are the last global administrator.

FR15: A global administrator can perform any tenant operation across all tenants without per-tenant role assignment.

FR16: All global administrator actions produce auditable domain events.

FR17: The system provides a bootstrap mechanism (seed command or startup configuration) to create the initial global administrator on first deployment when no global administrators exist.

FR18: The bootstrap mechanism only executes when zero global administrators exist in the event store; subsequent executions are rejected with a specific error indicating that bootstrap has already been completed.

FR19: A tenant owner can set a key-value configuration entry for a tenant.

FR20: A tenant owner can remove a configuration entry from a tenant.

FR21: Configuration keys support dot-delimited namespace conventions, such as `billing.plan` and `parties.maxContacts`, to prevent collisions between consuming services.

FR22: The system produces a domain event for every configuration change (set, removed).

FR23: The system enforces configuration limits: maximum 100 keys per tenant, maximum 1KB per value, maximum 256 characters per key.

FR24: The system rejects configuration operations that exceed limits with a specific error identifying which limit was exceeded and the current usage.

FR25: A developer can query a paginated list of all tenants with their IDs, names, and statuses.

FR26: A developer can query a specific tenant's details including its current users and their roles.

FR27: A developer can query the list of users in a specific tenant with their assigned roles.

FR28: A developer can query the list of tenants a specific user belongs to, with their role in each tenant.

FR29: A global administrator can query tenant access changes by tenant ID and date range for audit reporting, with pagination support (default page size: 100 results, maximum: 1,000).

FR30: All list and query endpoints support cursor-based pagination with consistent ordering.

FR31: A TenantReader can query tenant details, user lists, and configuration for tenants they belong to, but cannot execute any state-changing commands.

FR32: A TenantContributor has TenantReader capabilities plus the ability to execute domain commands within the tenant, with specific commands defined by each consuming service.

FR33: A TenantOwner has TenantContributor capabilities plus user-role management (add, remove, change role) and tenant configuration management.

FR34: A user with roles in multiple tenants can only access data and execute commands within each tenant according to their role in that specific tenant; roles do not transfer or aggregate across tenants.

FR35: The system publishes all tenant domain events via DAPR pub/sub as CloudEvents 1.0.

FR36: The system uses a documented topic naming convention for tenant events, such as `tenants.events`, consistent with Hexalith ecosystem patterns.

FR37: A consuming service can subscribe to tenant events and build a local projection of tenant state.

FR38: A consuming service can react to user addition/removal events to enforce or revoke access.

FR39: A consuming service can react to tenant disable/enable events to block or allow operations.

FR40: A consuming service can react to configuration change events to update tenant-specific behavior.

FR41: Event contracts include sufficient information (event ID, aggregate version) for consuming services to implement idempotent event handling.

FR42: Documentation provides guidance on idempotent event processing patterns for consumers, since DAPR pub/sub may deliver events more than once. Minimum content: at-least-once delivery explanation, deduplication by event ID example, and idempotent handler pattern with code sample.

FR43: A developer can install Tenants via NuGet packages (Contracts, Client, Server, Testing, Aspire).

FR44: A developer can register tenant client services in DI with a single extension method call.

FR45: A developer can register tenant event handlers in a consuming service in under 20 lines of DI configuration.

FR46: A developer can write tenant integration tests using in-memory fakes without external infrastructure, in under 10 lines per test.

FR47: The in-memory testing fakes execute the same domain logic as the production service, guaranteeing isolation at the aggregate domain model level (command validation, event production, state transitions), verified by a conformance test suite that runs identical command sequences against both fakes and production aggregate. Projection-level and query-level isolation remains the responsibility of the consuming service's own test suite.

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

FR58: The CI/CD pipeline enforces quality gates: build, test (Tier 1+2), coverage threshold (> 80% line, 100% branch on isolation/auth), and package validation before NuGet publish.

FR59: The project provides a quickstart guide that enables a developer to send their first tenant command within 30 minutes.

FR60: The quickstart guide includes prerequisite validation (DAPR sidecar, EventStore deployment).

FR61: The project provides an event contract reference documenting all commands, events, and their schemas.

FR62: The project provides a sample consuming service demonstrating event subscription and access enforcement.

FR63: The project provides an "aha moment" demo (screencast or video) showing reactive cross-service access revocation.

FR64: The project provides documentation on cross-aggregate timing behavior, including the event propagation window between tenant commands and subscriber processing. Minimum content: timing window explanation, sequence diagram, guidance on designing for eventual consistency, and reference to planned auth plugin as synchronous enforcement option.

FR65: The project provides documentation on compensating command patterns, such as restoring a wrongly removed user with explicit role specification. Minimum content: compensating command definition, worked example with AddUserToTenant after incorrect RemoveUserFromTenant, and explanation of why role must be explicitly specified rather than auto-restored.

### NonFunctional Requirements

NFR1: All tenant commands complete within 50ms p95 as measured by OpenTelemetry span duration.

NFR2: All read model queries complete within 50ms p95 for result sets within a single page, as measured by OpenTelemetry span duration.

NFR3: Event publication to DAPR pub/sub completes within 50ms p95 after command processing, as measured by OpenTelemetry span duration.

NFR4: In-memory testing fakes execute commands and produce events within 10ms, as measured by xUnit test execution time.

NFR5: Zero cross-tenant data leaks; no query, projection, or event subscription returns data belonging to a different tenant, verified by dedicated Tier 3 integration tests that assert isolation across all read model endpoints and event subscriptions.

NFR6: Role escalation boundaries are enforced at the domain level; no actor can self-escalate, verified by unit tests that assert rejection of every escalation path, including TenantOwner assigning GlobalAdministrator and self-role elevation.

NFR7: All state-changing operations produce immutable, auditable domain events with actor ID, timestamp, and full operation context, verified by integration tests that assert event production for every command type and validate required event fields are populated.

NFR8: Disabled tenants reject all commands immediately within the same aggregate, verified by unit tests that assert command rejection after DisableTenant is applied to aggregate state.

NFR9: Encryption at rest and in transit is a deployment concern; the system relies on DAPR infrastructure configuration for encryption and does not implement its own encryption layer.

NFR10: 100% branch coverage on tenant isolation and role authorization logic, defined as aggregate Handle methods for authorization checks, tenant ID filtering in projections, and role validation logic, verified in CI via coverlet.

NFR11: The system supports up to 1,000 tenants with up to 500 users per tenant without performance degradation beyond stated latency targets, verified by load tests seeding the target volume and asserting NFR1-NFR3 latency targets hold.

NFR12: The tenant service is stateless; horizontal scaling is achieved by adding service instances.

NFR13: State reconstruction from the event store on startup completes within 30 seconds for up to 1,000 tenants with an assumed average of 500 events per tenant (500,000 total events), verified by a startup benchmark test that seeds the target event volume and measures time to ready state. Baseline EventStore snapshot configuration is part of Phase 1 reliability/performance work; advanced snapshot tuning beyond the baseline configuration is a Phase 3 optimization if this target is exceeded at scale.

NFR14: All domain events conform to CloudEvents 1.0 specification.

NFR15: Event publication uses DAPR pub/sub abstraction with no direct dependency on a specific message broker.

NFR16: State persistence uses DAPR state store abstraction with no direct dependency on a specific database.

NFR17: The system degrades gracefully when DAPR pub/sub is unavailable; commands succeed, subscribers catch up when pub/sub recovers, verified by a Tier 3 integration test that disables pub/sub, executes commands, re-enables pub/sub, and asserts subscribers receive all pending events.

NFR18: Event contracts are backward-compatible after v1.0, with no breaking schema changes to published events.

NFR19: All domain events include event ID and aggregate version to enable idempotent processing by consumers.

NFR20: The event store is the single source of truth; system state can be fully reconstructed by replaying events.

NFR21: Command processing and event storage are atomic; a command either fully succeeds or fully fails.

NFR22: API availability target is 99.9% in production deployments, as measured by health check endpoint uptime monitoring.

NFR23: No data loss under any failure scenario; events once stored are immutable and durable.

NFR24: MVP error messages and documentation are English-only. Phase 2 Admin UI accessibility baseline is WCAG 2.1 AA, with WCAG 2.2 AA as the design and implementation target where supported by the selected Fluent UI Blazor and FrontComposer stack. Phase 2 UI must address i18n considerations as part of requirements scoping.

### Additional Requirements

- Starter template: use the Hexalith.EventStore structure mirror as the canonical foundation. Do not run `aspire new` or any generic starter CLI over this repository.

- Manual scaffolding or reconstruction must preserve EventStore-native package boundaries, DAPR/Aspire orchestration, and production/test parity.

- Runtime and language requirements are .NET 10 SDK `10.0.300`, C# latest, nullable references, implicit usings, and warnings as errors.

- Build tooling must use `Hexalith.Tenants.slnx`, central package management through `Directory.Packages.props`, shared `Directory.Build.props` and `Directory.Build.targets`, and no inline `Version=` attributes on `PackageReference`.

- Published package topology is Contracts, Client, Server, Aspire, and Testing, with host projects remaining non-packable.

- Code organization must preserve `src/Hexalith.Tenants.Contracts`, `.Client`, `.Server`, host `Hexalith.Tenants`, `.Aspire`, `.AppHost`, `.ServiceDefaults`, and `.Testing`, plus matching tests and sample consuming service.

- Root-level submodules only may be initialized or updated; recursive submodule initialization remains disallowed.

- Aspire AppHost owns local/distributed topology; AppHost changes require restart, and local execution starts from `src/Hexalith.Tenants.AppHost`.

- DAPR sidecars provide actors, state, pub/sub, and service invocation. Domain code must not directly couple to Redis, databases, brokers, or direct infrastructure APIs.

- Containers are produced through .NET SDK container publishing unless a future deployment target proves otherwise.

- Semantic-release, Conventional Commits, CI build/test gates, and package validation remain release requirements.

- Use Hexalith.EventStore as the domain-service foundation, not a generic ASP.NET/Aspire template.

- EventStore is the source of truth through aggregates, DAPR actor state, persisted events, snapshots, and projections.

- Domain behavior is modeled by two aggregate families: `TenantAggregate` and `GlobalAdministratorsAggregate`.

- Aggregates, states, projections, validators, and read models must live in `Hexalith.Tenants.Server`, the assembly scanned by EventStore.

- Platform tenant context is `system`, domain is `tenants`, and aggregate ID is either the managed tenant ID or `global-administrators`.

- Actor identity follows `system:tenants:{aggregateId}`.

- Every tenant event payload must include top-level `TenantId` because the EventStore envelope tenant is the platform tenant.

- Commands enter through EventStore command submission; do not create per-command REST controllers.

- Queries are exposed through explicit REST endpoints backed by EventStore query contracts: `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, and `GET /api/tenants/{tenantId}/audit`.

- Error responses must use RFC 7807 Problem Details. Domain rejection mapping happens at the HTTP boundary.

- Rejection events carry structured data only and never persisted English prose.

- Business rule failures return `DomainResult.Rejection`; infrastructure/programmer failures may throw and are handled by the host pipeline.

- Domain Handle methods must not log business rejections.

- Tenant authentication uses JWT Bearer with EventStore claims transformation and validation.

- Authorization is layered: EventStore API gate, aggregate-level domain RBAC, trusted global-admin envelope extension, and query-side row filtering.

- Production identity providers must emit or normalize `eventstore:tenant=system` for tenant-management operations.

- User identity comes from JWT `sub`; never use `name`, `email`, or other user-controllable claims as identity.

- Tenant validation must occur before aggregate state rehydration.

- User-supplied command envelope extensions must not be trusted.

- Logs must not include command payloads, event payloads, tokens, secrets, PII, or sensitive tenant/user data.

- Operational telemetry must use OpenTelemetry and structured logging with correlation, tenant, domain, aggregate, causation, command/event type, and stage metadata.

- DAPR/EventStore resource names follow conventions: AppId `tenants`, state store `tenants-eventstore`, topic `tenants.events`, dead letter topic `deadletter.tenants.events`, and actor identity `system:tenants:{aggregateId}`.

- Events are immutable past-tense facts implementing EventStore payload contracts; they are not commands or status messages.

- DAPR pub/sub is at-least-once; consumers must be idempotent and must not assume cross-service ordering.

- Consumers filter by event type and build their own local projections.

- Tenant read models are projections, not authoritative write state.

- Projection model includes `TenantProjection`, `GlobalAdministratorsProjection`, `TenantIndexProjection`, and `TenantAuditProjection`.

- Projection state uses EventStore projection conventions and DAPR state abstraction. Shared cross-tenant index and audit writes must use ETag/optimistic concurrency or verified `CachingProjectionActor` fan-in behavior to avoid silent write loss.

- Snapshot interval for the `tenants` domain remains 50 events; global administrator singleton state uses EventStore default unless evidence requires otherwise.

- Aggregate `Handle` methods must be public static, pure, synchronous functions returning `DomainResult`.

- State `Apply` methods mutate state and perform no validation.

- Projection Apply methods trust events and update read models deterministically.

- Commands follow `{Verb}{Target}`, events follow `{Target}{PastVerb}`, and rejections follow `{Target}{Reason}Rejection` while implementing `IRejectionEvent`.

- Events and commands use `System.Text.Json`; API JSON uses camelCase.

- Timestamps use `DateTimeOffset` and `{Action}At` field names.

- IDs representing message, correlation, aggregate, or causation identifiers use ULID validation where applicable.

- Tests use xUnit v3, Shouldly, NSubstitute, Testcontainers, Aspire testing, and coverlet.

- Mandatory test categories include conformance, naming convention, serialization round trip, isolation, projection safety, and auth readiness.

- Conformance tests must prove testing fakes produce identical event sequences as real aggregates for every command type.

- Serialization round-trip tests must serialize and deserialize every event type and assert deep equality; post-v1.0 requires golden JSON fixtures.

- Cross-tenant isolation tests must cover Handle-level rejection, JWT authorization pipeline behavior, API-level requests, projections, event subscriptions, cursor tokens, and safe error bodies.

- Snapshot performance tests must seed the 500,000-event target and assert the 30-second reconstruction target.

- Production auth remains deployment-sensitive and requires startup validation, documentation, smoke tests, and environment-specific OIDC configuration.

- Phase 1 has no frontend implementation requirement.

- Phase 2 Admin UI must be a FrontComposer/Fluent UI Blazor adapter layer and must not annotate or reshape immutable Tenants domain contracts for UI generation.

- Phase 2 Admin UI must add UI-facing command/projection models and mappings where needed.

- SignalR projection notifications must be treated as refresh nudges only, never as source-of-truth projection data.

- Phase 2 UI work is intentionally not ready until command lifecycle, projection freshness, consequence preview, audit evidence, accessibility, localization, and documentation evidence are resolved.

- Bulk provisioning, real-time feature-flag service boundaries, advanced audit timeline/grouped timeline UX, server-side anomaly scoring, and broader consuming-service synchronous authorization are deferred.

- The current host reference collision noted in architecture remains an implementation cleanup item, not an architectural blocker.

### UX Design Requirements

UX-DR1: Use Microsoft Fluent UI Blazor v5 as the control and interaction foundation for standard application surfaces, implemented through Hexalith.FrontComposer where generated composition is appropriate.

UX-DR2: Verify exact Fluent UI Blazor component APIs, parameters, and token names against the project-pinned package during implementation.

UX-DR3: Use FrontComposer-generated patterns only for low-risk read-only and projection-driven surfaces where source-of-truth boundaries are clear.

UX-DR4: Use custom components or overrides for command lifecycle, consequence preview, audit evidence, authorization-sensitive flows, destructive actions, global administrator management, and degraded-state recovery.

UX-DR5: Implement an Operations Shell navigation model with primary navigation for Tenants, Users, Global Administrators, and Audit.

UX-DR6: Make the tenant list the default operational triage surface, supporting filter, search, sort, pagination, tenant status, member count, owner count, freshness, and pending state.

UX-DR7: Implement tenant detail views that preserve tenant context across overview, members, configuration, command state, and audit evidence.

UX-DR8: Implement a member table that shows user, role, owner count or relevant role context, tenant status, projection freshness, and stable available actions.

UX-DR9: Provide configuration read-only views through standard Fluent/FrontComposer surfaces and reserve high-impact configuration changes for custom consequence-aware workflows.

UX-DR10: Provide a user lookup path reachable from shell navigation and access-review contexts without replacing tenant-risk investigation as the primary workflow.

UX-DR11: Provide global administrator list and management planning surfaces that distinguish ordinary tenant membership from platform-level global administrator risk.

UX-DR12: Provide audit entry points from global navigation, tenant rows, tenant detail, user lookup, and command results.

UX-DR13: Provide a flat audit DataGrid fallback when a reusable audit timeline is unavailable, including stable ordering, filters, loading, empty, error, and accessible expansion states.

UX-DR14: Implement a Truth State Badge component with shared vocabulary for freshness, authorization, command lifecycle, projection confirmation, and audit evidence states.

UX-DR15: Truth State Badge states must include current, refreshing, aging, stale, unknown, eligible, blocked, pending, accepted, confirmed, failed, audit pending, and audit available; text labels are required and color/icons are secondary.

UX-DR16: Implement a Freshness Gate showing freshness label, timestamp or version marker, refresh action, and blocking reason; unknown freshness fails closed for destructive actions.

UX-DR17: Implement an Unavailable Action Reason component that explains missing permission, stale data, missing lifecycle support, missing consequence preview, missing audit proof, or high-impact flow readiness gaps.

UX-DR18: High-impact unavailable actions must expose visible inline reasons; tooltips may supplement but must not be the only explanation.

UX-DR19: Implement Consequence Preview for access-impacting commands, showing tenant, target user, current role, owner count, affected access path, freshness, recovery path, audit expectation, known consequences, and known unknowns.

UX-DR20: Consequence Preview must block submit if required consequence inputs are incomplete unless product/UX explicitly approves a named fallback.

UX-DR21: Implement a Command Lifecycle Panel that separates eligible, previewed, submitted, accepted, projection pending, confirmed, failed, unknown, audit pending, and audit available states.

UX-DR22: Command Lifecycle Panel content must include a support-safe command reference, accepted timestamp, projection confirmation status, retry or status-review action, and audit link or fallback state.

UX-DR23: Implement an Audit Evidence Receipt after meaningful access changes, containing actor, target, tenant scope, outcome, timestamp, projection marker, and audit reference.

UX-DR24: Audit Evidence Receipt must support copyable support-safe references without exposing raw payloads, bearer tokens, stack traces, or sensitive internals.

UX-DR25: Treat `RemoveUserFromTenant` as the first command-capable journey, launched from a specific tenant membership row and proven through preview, command lifecycle, projection reconciliation, and audit evidence.

UX-DR26: Access review must decide whether the user cannot act, should not act yet, or can proceed, based on projection freshness, authorization, command dependencies, high-risk status, and proof readiness.

UX-DR27: High-risk cases such as last-owner removal, global administrator removal, and tenant-wide impact require elevated friction, affected scope, current evidence freshness, audit consequence, and intentional confirmation.

UX-DR28: Preserve confirmed projection truth during command submission; local pending or confirming hints must not replace source-of-truth projection data.

UX-DR29: Distinguish request submission, accepted request, projection confirmation, rejection, already-applied outcome, degraded state, audit pending, audit available, and unable-to-verify states.

UX-DR30: SignalR and real-time projection notifications must be treated as freshness nudges that trigger re-query or status reconciliation, not durable truth.

UX-DR31: If projection confirmation is delayed, the UI must preserve context and offer safe re-query, retry status lookup, inspect audit, continue read-only, or escalation paths.

UX-DR32: If a command is rejected, the UI must explain the outcome, preserve user input where appropriate, and keep displayed projection state accurate.

UX-DR33: Recovery must use explicit compensating commands and must not be labeled as undo.

UX-DR34: Recovery paths should include reassign tenant owner, restore intended access through a new add-user command, retry access removal, open audit evidence, or escalate when proof is incomplete.

UX-DR35: Map tenant-specific meaning through semantic roles rather than hard-coded colors, including tenant status, projection freshness, command lifecycle, authorization state, audit evidence, and risk state.

UX-DR36: Color must never be the only indicator of tenant status, freshness, command lifecycle, risk, authorization, or audit availability; readable text, accessible labels, and icons or shapes must support meaning.

UX-DR37: Use professional, calm, precise, operational typography based on system UI fonts, compact density, modest hierarchy, and plain-language status labels.

UX-DR38: Use dense, efficient, stable layout patterns with tables, split views, tabs, side panels, dialogs, and inline status regions instead of decorative card grids.

UX-DR39: Keep command controls close to the affected tenant, user, role, or audit context and preserve context across list, detail, preview, confirmation, and audit evidence.

UX-DR40: Keep stable dimensions for status chips, action cells, toolbars, and command lifecycle regions to avoid layout shift.

UX-DR41: Use one primary action per region; destructive actions must not appear as casual primary actions and must use consequence preview plus danger treatment.

UX-DR42: Row-level commands must stay close to the affected row; retry, refresh, inspect audit, and continue read-only are secondary actions.

UX-DR43: Do not hide unavailable high-impact actions when the reason helps users understand authority, freshness, or readiness.

UX-DR44: Feedback must follow the truth-state model and appear close to the affected tenant, row, command panel, or audit context; global message bars are reserved for page-level degradation or system-wide state.

UX-DR45: Forms must be compact, validated, scoped to one user decision, and keep tenant, user, role, and freshness context visible.

UX-DR46: Access-impacting forms must not submit when freshness, authorization, or consequence inputs are unknown.

UX-DR47: Domain rejections must map to safe, localized user-facing text without exposing raw command payloads, stack traces, tokens, or internal exception text.

UX-DR48: Navigation must preserve selected tenant and filters when returning from detail.

UX-DR49: Command lifecycle must remain inside the affected workflow and must not become a separate primary navigation model.

UX-DR50: DataGrid-backed tenant list, member table, user lookup, and flat audit fallback must show loading, empty, filtered-empty, error, stale, and degraded states distinctly.

UX-DR51: Sorting and pagination must not hide pending or stale-state indicators.

UX-DR52: Long tenant IDs, user IDs, and support-safe references should truncate visually while remaining accessible.

UX-DR53: Automated tests should use stable selectors or component contracts rather than arbitrary row text.

UX-DR54: Dialogs and side panels may be used only when focus trap, keyboard behavior, escape behavior, and return focus are specified.

UX-DR55: Consequence preview must not be a generic confirmation dialog; it must show knowns, unknowns, audit expectation, and recovery path.

UX-DR56: Confirmation copy must be localizable and must not rely on sentence fragments assembled at runtime.

UX-DR57: Loading states must state what is being loaded and keep layout stable.

UX-DR58: Empty states must distinguish no data, no matching filtered data, no permission, and unavailable backend state.

UX-DR59: Stale states must show freshness marker and refresh path; degraded states must explain what is unavailable and what still works.

UX-DR60: Unable-to-verify states must avoid success language and offer retry, inspect audit, continue read-only, or escalation.

UX-DR61: Audit-unavailable states must distinguish delayed evidence from missing implementation support.

UX-DR62: All state labels, role names, timestamps, warnings, disabled reasons, and recovery actions must be localizable.

UX-DR63: Disabled explanations, command lifecycle changes, stale/degraded states, and audit availability must be perceivable without color.

UX-DR64: Keyboard users must be able to complete or exit every modal, preview, and command flow.

UX-DR65: Use a desktop-first operational responsive strategy optimized for admin workstation workflows with dense tables, persistent shell navigation, detail panels, member tables, command context, and audit evidence.

UX-DR66: Tablet layouts may collapse navigation, stack detail regions, and preserve table usability through horizontal scroll, column prioritization, or row detail expansion.

UX-DR67: Mobile is limited support for read-only triage, lookup, and audit reference review; high-impact access changes should fail closed or become unavailable when full safety context cannot be preserved.

UX-DR68: Responsive behavior must prioritize truth and context over visual compactness.

UX-DR69: Use breakpoints of mobile 320-767px, tablet 768-1023px, desktop 1024px and above, and wide desktop 1440px and above.

UX-DR70: Phase 2 Admin UI accessibility baseline is WCAG 2.1 AA, with WCAG 2.2 AA as target where supported by the selected Fluent UI Blazor and FrontComposer stack.

UX-DR71: All interactive elements must be keyboard reachable, with focus order following visual and task order and visible focus indicators in normal, high-contrast, and forced-colors modes.

UX-DR72: Disabled or unavailable actions must expose readable reasons, not only tooltips.

UX-DR73: Status labels must have accessible names, and timestamps need exact accessible labels rather than relative time only.

UX-DR74: Command lifecycle changes must use live regions with appropriate politeness; assertive announcements are reserved for rejection, failure, destructive blockers, or unable-to-verify states.

UX-DR75: Dialogs and command previews must trap focus when modal, support safe escape behavior, and return focus to the launching row or action.

UX-DR76: Tables must expose headers, row relationships, sort state, and row actions clearly.

UX-DR77: Reduced-motion users must not depend on animation to understand lifecycle progression.

UX-DR78: Responsive testing must cover desktop 1024px, 1366px, 1440px, and wide layouts; tablet 768px and 1024px; mobile 375px and 430px; horizontal table overflow; navigation collapse; and command preview/dialog behavior at narrow widths.

UX-DR79: Accessibility testing must cover keyboard-only navigation, screen reader review with NVDA and at least one browser/screen-reader pairing, automated accessibility checks, forced-colors/high-contrast mode, reduced motion, color contrast, live-region announcements, focus return, and disabled action explanations without mouse hover.

UX-DR80: Acceptance checks for UI stories must include stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, and permission-missing cases.

### FR Coverage Map

FR1: Epic 2 - global administrator creates tenants.

FR2: Epic 2 - tenant metadata can be updated.

FR3: Epic 2 - global administrator disables tenants.

FR4: Epic 2 - global administrator re-enables tenants.

FR5: Epic 2 - lifecycle changes produce domain events.

FR6: Epic 3 - tenant owner adds users with tenant roles.

FR7: Epic 3 - tenant owner removes users from tenants.

FR8: Epic 3 - tenant owner changes a user's tenant role.

FR9: Epic 3 - duplicate tenant membership is rejected.

FR10: Epic 3 - role escalation violations are rejected.

FR11: Epic 3 - user-role changes produce domain events.

FR12: Epic 3 - aggregate command conflicts are rejected through optimistic concurrency.

FR13: Epic 2 - global administrators designate new global administrators.

FR14: Epic 2 - global administrators remove global administrator status with last-admin protection.

FR15: Epic 2 - global administrators perform cross-tenant operations.

FR16: Epic 2 - global administrator actions produce auditable domain events.

FR17: Epic 2 - initial global administrator bootstrap exists.

FR18: Epic 2 - bootstrap is rejected after global administration already exists.

FR19: Epic 3 - tenant owners set configuration entries.

FR20: Epic 3 - tenant owners remove configuration entries.

FR21: Epic 3 - configuration keys support namespace conventions.

FR22: Epic 3 - configuration changes produce domain events.

FR23: Epic 3 - configuration key count, key length, and value length limits are enforced.

FR24: Epic 3 - configuration limit violations return specific rejections.

FR25: Epic 5 - users query a paginated tenant list.

FR26: Epic 5 - users query tenant details including users and roles.

FR27: Epic 5 - users query tenant user lists.

FR28: Epic 5 - users query a user's tenant memberships.

FR29: Epic 5 - global administrators query tenant access audit history.

FR30: Epic 5 - query endpoints support cursor-based pagination with stable ordering.

FR31: Epic 3 - TenantReader role has query-only tenant capabilities.

FR32: Epic 3 - TenantContributor role extends reader capabilities for tenant-scoped domain commands.

FR33: Epic 3 - TenantOwner role extends contributor capabilities for membership and configuration management.

FR34: Epic 3 - tenant roles remain isolated per tenant.

FR35: Epic 4 - tenant domain events publish via DAPR pub/sub as CloudEvents 1.0.

FR36: Epic 4 - tenant event topic naming is documented and consistent.

FR37: Epic 4 - consuming services subscribe to tenant events and build local projections.

FR38: Epic 4 - consuming services react to user addition/removal events.

FR39: Epic 4 - consuming services react to tenant disable/enable events.

FR40: Epic 4 - consuming services react to configuration change events.

FR41: Epic 4 - event contracts support idempotent consumer handling.

FR42: Epic 4 - idempotent event processing documentation is provided.

FR43: Epic 1 - developers install and reference the five NuGet packages.

FR44: Epic 4 - developers register tenant client services through one DI extension method.

FR45: Epic 4 - developers register tenant event handlers in under 20 lines.

FR46: Epic 6 - developers write in-memory tenant integration tests without infrastructure.

FR47: Epic 6 - testing fakes execute production-equivalent domain logic and pass conformance tests.

FR48: Epic 7 - developers deploy the tenant service with Aspire hosting extensions.

FR49: Epic 2 - command rejections expose specific, actionable error information at the API boundary.

FR50: Epic 2 - commands targeting non-existent tenants are rejected.

FR51: Epic 2 - commands targeting disabled tenants are rejected.

FR52: Epic 2 - duplicate operations are rejected with current-state context.

FR53: Epic 2 - command processing and event storage remain source-of-truth behavior independent of pub/sub availability.

FR54: Epic 7 - tenant command latency metrics are exposed through OpenTelemetry.

FR55: Epic 7 - event processing metrics are exposed through OpenTelemetry.

FR56: Epic 7 - operators deploy Tenants alongside EventStore with standard DAPR configuration.

FR57: Epic 7 - tenant service remains stateless and reconstructs state from EventStore.

FR58: Epic 1 - CI/CD enforces build, test, coverage, and package quality gates.

FR59: Epic 8 - quickstart enables first tenant command within 30 minutes.

FR60: Epic 8 - quickstart includes prerequisite validation.

FR61: Epic 8 - event contract reference documents commands, events, and schemas.

FR62: Epic 4 - sample consuming service demonstrates event subscription and access enforcement.

FR63: Epic 8 - "aha moment" demo shows reactive cross-service revocation.

FR64: Epic 8 - cross-aggregate timing documentation explains propagation windows and eventual consistency.

FR65: Epic 8 - compensating command documentation explains explicit correction workflows.

### NFR Coverage Map

| NFR | Primary Story Coverage | Required Evidence |
| --- | --- | --- |
| NFR1 | 7.4, command stories in Epics 2-3 | OpenTelemetry command latency measurement and p95 evidence. |
| NFR2 | Epic 5 query endpoint stories | Query latency measurement for single-page result sets. |
| NFR3 | 4.2, 7.4 | Event publication latency measurement. |
| NFR4 | 6.1-6.3 | xUnit timing evidence for in-memory fakes. |
| NFR5 | 5.7, endpoint stories in Epic 5, 4.2 | Tier 3 isolation tests across read models and event subscriptions. |
| NFR6 | 2.2, 2.3, 3.1-3.4 | Unit tests for every role escalation path. |
| NFR7 | Command stories in Epics 2-3, 5.4 | Integration tests proving event audit fields. |
| NFR8 | 2.5, command stories in Epic 3 | Unit tests proving disabled tenants reject commands. |
| NFR9 | 7.1, 7.3, 7.6 | Deployment documentation identifying encryption as infrastructure concern. |
| NFR10 | 2.3, 3.1-3.4, 5.7 | Coverage gate evidence for tenant isolation and role authorization branches. |
| NFR11 | 7.5 | Load test evidence at target tenant/user volume. |
| NFR12 | 7.5 | Multi-instance stateless operation evidence. |
| NFR13 | 7.5 | Startup benchmark for 500,000 events and snapshot baseline. |
| NFR14 | 4.1-4.3 | CloudEvents conformance tests. |
| NFR15 | 4.2, 7.1 | DAPR pub/sub abstraction verification. |
| NFR16 | 5.6, 7.1 | DAPR state store abstraction verification. |
| NFR17 | 4.2, 7.6 | Pub/sub outage and catch-up integration test. |
| NFR18 | 8.2 | Event contract compatibility documentation and package validation. |
| NFR19 | 4.1, 8.2 | Event ID and aggregate version contract tests/docs. |
| NFR20 | 7.5 | Replay/reconstruction evidence. |
| NFR21 | 2.4, 3.8 | Command atomicity and conflict tests. |
| NFR22 | 7.5, 7.6 | Health check availability and readiness evidence. |
| NFR23 | 4.2, 7.6 | Durable event storage and recovery evidence. |
| NFR24 | Epic 9 | Phase 2 accessibility and i18n readiness evidence. |

## Epic List

### Epic 1: Developers Can Build and Consume the Tenant Platform

Developers can clone, build, test, package, and reference the tenant platform with the EventStore-native project structure, package boundaries, CI gates, and release foundation in place.

**FRs covered:** FR43, FR58

### Epic 2: Global Administrators Can Bootstrap and Govern Tenants

Global administrators can bootstrap the first admin, manage global administrators, create tenants, update metadata, disable or enable tenants, and receive structured rejection outcomes.

**FRs covered:** FR1-FR5, FR13-FR18, FR49-FR53

### Epic 3: Tenant Owners Can Manage Access and Configuration Safely

Tenant owners can manage members, roles, role boundaries, tenant configuration, and tenant-scoped role behavior without cross-tenant leakage or escalation.

**FRs covered:** FR6-FR12, FR19-FR24, FR31-FR34

### Epic 4: Consuming Services Can React to Tenant Events

Consuming services can subscribe to tenant events, build local projections, handle idempotency, and react to access, lifecycle, and configuration changes.

**FRs covered:** FR35-FR42, FR44-FR45, FR62

### Epic 5: Operators and Developers Can Query Tenant State and Audit Access

Users can query tenants, tenant details, users, user memberships, and audit history through safe cursor-based APIs backed by durable projections.

**FRs covered:** FR25-FR30

### Epic 6: Developers Can Test Tenant Behavior Without Infrastructure

Developers can write fast tenant integration tests using in-memory fakes that execute production-equivalent domain behavior.

**FRs covered:** FR46-FR47

### Epic 7: Operators Can Deploy, Secure, and Observe Production Tenants

Operators can deploy Tenants alongside EventStore with Aspire/DAPR, production auth validation, telemetry, health, stateless operation, and recovery behavior.

**FRs covered:** FR48, FR54-FR57

### Epic 8: Developers Can Adopt Through Documentation and Demo Evidence

Developers can follow a validated quickstart, understand event contracts, see the reactive access demo, and design for timing, idempotency, and compensating commands.

**FRs covered:** FR59-FR65

### Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely

**Readiness status:** readiness/planning-only. This epic produces Phase 2 UI dependency maps, specifications, and acceptance-evidence requirements. It is not a shippable Admin UI implementation epic and must not be routed to Developer agents as product delivery work until separate implementation stories are created.

**Routing rule:** Product implementation must not create Developer-agent story files directly from Epic 9 stories. Before Phase 2 UI implementation starts, Product/UX/Architecture must convert these planning outputs into implementation stories with explicit source projections, FrontComposer/Fluent UI dependencies, adapter architecture, command lifecycle behavior, accessibility/localization evidence, and test artifacts.

Phase 2 Admin UI readiness is sequenced around operational access review, truth-state feedback, consequence preview, command lifecycle, audit evidence, accessibility, localization, and FrontComposer dependencies.

**FRs covered:** Supports FR25-FR29, FR31, FR34 through UI surfaces; primary coverage is UX-DR1-UX-DR80 and NFR24.

## Epic Readiness Status

| Epic | Status | Notes |
| --- | --- | --- |
| Epic 1 | Implementation-ready | Foundation, package, and CI work can proceed after current artifact cleanup is applied. |
| Epic 2 | Implementation-ready | Duplicate global administrator and lifecycle semantics are resolved as structured rejections. |
| Epic 3 | Implementation-ready | Missing configuration key and concurrency semantics are resolved as structured rejection/conflict outcomes. |
| Epic 4 | Implementation-ready | No readiness blocker identified by the 2026-05-31 assessment. |
| Epic 5 | Implementation-ready after correction | Projection write safety, query authorization, and cursor security are sequenced before endpoint completion. |
| Epic 6 | Implementation-ready | No readiness blocker identified by the 2026-05-31 assessment. |
| Epic 7 | Implementation-ready after correction | Invalid tenant-claim behavior is resolved as fail-closed rejection; deployment readiness smoke tests are split before Developer-agent handoff. |
| Epic 8 | Implementation-ready | No readiness blocker identified by the 2026-05-31 assessment. |
| Epic 9 | Readiness/planning-only | Produces Phase 2 UI planning outputs; not implementation-ready UI delivery. |

## Epic 1: Developers Can Build and Consume the Tenant Platform

Developers can clone, build, test, package, and reference the tenant platform with the EventStore-native project structure, package boundaries, CI gates, and release foundation in place.

### Story 1.1: Establish EventStore-Native Solution Structure

As a developer,
I want the Tenants repository to use the EventStore-native solution and project structure,
So that I can build and extend the tenant platform using the expected Hexalith package boundaries.

**Acceptance Criteria:**

**Given** the repository is checked out with root-level submodules initialized
**When** a developer opens the solution
**Then** `Hexalith.Tenants.slnx` contains the expected source projects for Contracts, Client, Server, host, Aspire, AppHost, ServiceDefaults, and Testing
**And** matching test projects exist under `tests/`.

**Given** the solution structure is reviewed
**When** project references are inspected
**Then** Contracts remains the public immutable API surface
**And** Server owns aggregate, state, validator, projection, and read-model implementation
**And** Client, Testing, Aspire, AppHost, and host projects preserve their documented boundaries.

**Given** the repo uses Hexalith.EventStore as a submodule dependency
**When** dependency setup is documented or validated
**Then** only root-level submodule initialization is required
**And** no recursive submodule initialization command is introduced.

**Given** a developer runs a focused restore/build for the solution
**When** project structure is valid
**Then** the build uses `Hexalith.Tenants.slnx`
**And** does not require a legacy `.sln` file.

### Story 1.2: Configure Central Build and Package Governance

As a package maintainer,
I want build and package settings governed centrally,
So that every Tenants package follows consistent versioning, warnings, metadata, and dependency rules.

**Acceptance Criteria:**

**Given** package references are reviewed across source and test projects
**When** a project references a NuGet package
**Then** the project uses central package management through `Directory.Packages.props`
**And** no project-level `PackageReference` contains an inline `Version=` attribute.

**Given** shared build settings are reviewed
**When** a developer builds the solution
**Then** nullable references, implicit usings, latest C# language version, and warnings-as-errors are applied consistently from shared build configuration.

**Given** published package projects are inspected
**When** package metadata and pack settings are evaluated
**Then** Contracts, Client, Server, Testing, and Aspire are configured as publishable packages
**And** host projects, AppHost, and ServiceDefaults are not packable.

**Given** container or publish settings are reviewed
**When** host projects are prepared for deployment
**Then** container defaults come from shared build targets or documented host configuration
**And** no Dockerfile or ad hoc publish convention is introduced for Phase 1 foundation work.

**Given** package governance tests or validation scripts run
**When** a project violates central versioning or packability expectations
**Then** validation fails with enough detail for a developer to identify the offending project.

### Story 1.3: Add CI Quality Gates for Build, Test, Coverage, and Package Validation

As a maintainer,
I want CI to enforce the tenant platform quality gates automatically,
So that every change proves the solution can build, test, and package before release.

**Acceptance Criteria:**

**Given** a pull request or push targets the protected development branch
**When** CI runs
**Then** the workflow restores dependencies and builds `Hexalith.Tenants.slnx` in Release configuration
**And** warnings are treated as build failures.

**Given** CI reaches the test stage
**When** Tier 1 and Tier 2 test projects are available
**Then** CI runs the blocking test set defined for Phase 1
**And** test failures stop the workflow before packaging or release steps.

**Given** coverage collection is enabled
**When** tests complete
**Then** CI verifies the configured coverage gates, including overall line coverage and branch coverage for isolation and authorization logic
**And** failures identify the missing or below-threshold coverage area.

**Given** package validation runs
**When** package projects are packed
**Then** exactly the expected publishable packages are validated: Contracts, Client, Server, Testing, and Aspire
**And** host, AppHost, and ServiceDefaults projects are not included as NuGet packages.

**Given** the repository contains root-level submodules
**When** CI initializes dependencies
**Then** only root-level submodules are initialized
**And** recursive nested submodule initialization is not used.

**Given** CI produces build, test, coverage, or package artifacts
**When** workflow outputs are reviewed
**Then** artifacts are bounded to required evidence
**And** generated `bin`, `obj`, `TestResults-Coverage`, `nupkgs`, or local cache files are not committed.

### Story 1.4: Verify Consumer Package Reference Experience

As a consuming developer,
I want the tenant packages to restore and expose the expected integration surface,
So that I can adopt Tenants without understanding the repository internals.

**Acceptance Criteria:**

**Given** the five package projects are packed locally or through CI
**When** the package artifacts are inspected
**Then** Contracts, Client, Server, Testing, and Aspire packages are produced with expected package IDs
**And** package metadata is consistent with the repository release conventions.

**Given** a sample or verification consumer project references the Contracts and Client packages
**When** the consumer project restores and builds
**Then** command, event, query, and client registration types are available through the public package surface
**And** no source-project reference is required for consumer code.

**Given** a test consumer references the Testing package
**When** the test project restores and builds
**Then** in-memory tenant testing helpers are available to the consumer
**And** the package does not require live DAPR, EventStore, or Aspire infrastructure for unit-test usage.

**Given** a deployment-oriented consumer references the Aspire package
**When** the AppHost integration is compiled
**Then** the tenant hosting extension is available through a single documented registration path
**And** the consumer does not need to duplicate Tenants AppHost wiring manually.

**Given** package dependency metadata is reviewed
**When** transitive dependencies are inspected
**Then** package dependencies follow the documented Contracts, Client, Server, Testing, and Aspire boundaries
**And** no package introduces an unexpected dependency on host-only projects.

## Epic 2: Global Administrators Can Bootstrap and Govern Tenants

Global administrators can bootstrap the first admin, manage global administrators, create tenants, update metadata, disable or enable tenants, and receive structured rejection outcomes.

### Story 2.1: Bootstrap the Initial Global Administrator

As a platform operator,
I want the first global administrator to be bootstrapped safely at startup,
So that a new deployment has an authorized actor without exposing a public bootstrap endpoint.

**Acceptance Criteria:**

**Given** no global administrator has been recorded in the event store
**When** the service starts with `Tenants:BootstrapGlobalAdminUserId` configured
**Then** the host submits the bootstrap command through the normal MediatR/EventStore pipeline
**And** the global administrator aggregate records the first global administrator event.

**Given** at least one global administrator already exists
**When** bootstrap runs again
**Then** the aggregate returns a specific already-bootstrapped rejection
**And** no additional global administrator is created by bootstrap.

**Given** multiple service instances start at the same time
**When** more than one instance attempts bootstrap
**Then** one instance can create the initial global administrator
**And** the remaining instances receive the expected already-bootstrapped rejection.

**Given** bootstrap is skipped or rejected because setup is already complete
**When** the host logs the outcome
**Then** the message is logged at Information level
**And** the log does not expose secrets, tokens, or command payloads.

**Given** API routes are inspected
**When** bootstrap support is reviewed
**Then** no public REST endpoint exists for bootstrap
**And** bootstrap remains a startup configuration or approved operator path only.

### Story 2.2: Manage Global Administrator Assignments

As a global administrator,
I want to add and remove global administrator assignments,
So that platform governance can be delegated and recovered without per-tenant role setup.

**Acceptance Criteria:**

**Given** an authenticated existing global administrator submits a command to add another user as global administrator
**When** the command is handled
**Then** the global administrators aggregate records a global administrator added event
**And** the event contains structured identifiers and timestamp data required for audit.

**Given** an authenticated existing global administrator submits a command to remove another user's global administrator status
**When** the command is valid
**Then** the aggregate records a global administrator removed event
**And** the removed user no longer has global administrator authority in subsequent command evaluation.

**Given** a global administrator attempts to remove themselves as the last remaining global administrator
**When** the command is handled
**Then** the aggregate returns a specific last-global-administrator rejection
**And** the existing global administrator set remains unchanged.

**Given** a duplicate global administrator add operation is submitted
**When** the target user is already a global administrator
**Then** the aggregate returns a structured duplicate-global-administrator rejection
**And** no additional global administrator added event is produced.

**Given** a duplicate global administrator remove operation is submitted
**When** the target user is not a global administrator
**Then** the aggregate returns a structured global-administrator-not-found rejection
**And** no global administrator removed event is produced.

**Given** duplicate global administrator assignment tests run
**When** duplicate add and missing remove cases are exercised
**Then** tests verify the exact rejection types, unchanged aggregate state, and absence of duplicate events.

**Given** global administrator assignment events are serialized
**When** contract round-trip tests run
**Then** every global administrator event and rejection can be serialized and deserialized with `System.Text.Json`
**And** deep equality is preserved.

### Story 2.3: Authorize Global Administrators for Cross-Tenant Governance

As a global administrator,
I want my platform authority to apply across tenant operations,
So that I can govern tenants without being assigned a role in every tenant.

**Acceptance Criteria:**

**Given** EventStore claims transformation marks a command envelope with the trusted global-admin extension
**When** a tenant governance command is handled
**Then** the aggregate treats the actor as globally authorized
**And** the aggregate does not depend on user-supplied command body fields for global administrator authority.

**Given** a command envelope does not contain trusted global-admin authority
**When** the actor attempts a global-administrator-only tenant operation
**Then** the command is rejected with a structured authorization rejection
**And** no tenant lifecycle event is produced.

**Given** a global administrator acts on any managed tenant aggregate
**When** the command envelope references the platform tenant `system`, domain `tenants`, and the managed aggregate ID
**Then** the command uses the aggregate ID from the envelope
**And** the command body cannot override the target aggregate identity.

**Given** authorization tests run
**When** global and non-global actors execute create, update, disable, and enable tenant commands
**Then** tests prove global administrators can perform cross-tenant operations
**And** non-global actors cannot bypass the aggregate authorization checks.

**Given** audit or telemetry metadata is emitted for global administrator commands
**When** logs and traces are inspected
**Then** they include support-safe correlation and command-stage metadata
**And** they do not include command payloads, tokens, or sensitive user data.

### Story 2.4: Create and Update Tenants

As a global administrator,
I want to create tenants and update tenant metadata,
So that tenant records can be introduced and maintained as event-sourced domain state.

**Acceptance Criteria:**

**Given** a global administrator submits a create tenant command with a unique tenant identifier and name
**When** the tenant aggregate handles the command
**Then** it records a tenant created event
**And** the event includes top-level `TenantId`, name, optional description, and `CreatedAt` data.

**Given** a create tenant command targets an existing tenant aggregate
**When** the command is handled
**Then** the aggregate returns a structured duplicate tenant rejection
**And** no second tenant created event is produced.

**Given** an authorized actor submits an update tenant metadata command
**When** the target tenant exists and is enabled
**Then** the aggregate records a tenant updated event
**And** the event includes top-level `TenantId`, updated metadata, and `UpdatedAt` data.

**Given** an update tenant metadata command targets a missing tenant
**When** the command is handled
**Then** the aggregate returns a structured tenant-not-found rejection
**And** no update event is produced.

**Given** tenant lifecycle contracts are tested
**When** naming convention and serialization tests run
**Then** create and update commands, events, and rejections follow the project naming conventions
**And** all events round-trip through `System.Text.Json`.

### Story 2.5: Disable and Re-Enable Tenants

As a global administrator,
I want to disable and re-enable tenants,
So that tenant operations can be stopped during risk or restored when the tenant is ready.

**Acceptance Criteria:**

**Given** a global administrator submits a disable tenant command for an enabled tenant
**When** the tenant aggregate handles the command
**Then** it records a tenant disabled event
**And** subsequent tenant-scoped state-changing commands are rejected while the tenant is disabled.

**Given** a global administrator submits an enable tenant command for a disabled tenant
**When** the tenant aggregate handles the command
**Then** it records a tenant enabled event
**And** normal tenant command processing is restored.

**Given** a disable or enable command targets a missing tenant
**When** the command is handled
**Then** the aggregate returns a structured tenant-not-found rejection
**And** no lifecycle event is produced.

**Given** a command targets a disabled tenant
**When** the command is not the authorized enable-tenant recovery operation
**Then** the aggregate rejects it immediately with a structured disabled-tenant rejection
**And** no state-changing event is produced.

**Given** a duplicate disable command is submitted
**When** the tenant is already disabled
**Then** the aggregate returns a structured duplicate-tenant-lifecycle-state rejection
**And** no tenant disabled event is produced.

**Given** a duplicate enable command is submitted
**When** the tenant is already enabled
**Then** the aggregate returns a structured duplicate-tenant-lifecycle-state rejection
**And** no tenant enabled event is produced.

**Given** tenant lifecycle duplicate tests run
**When** duplicate disable and duplicate enable cases are exercised
**Then** tests verify the exact rejection type, current lifecycle state, and absence of duplicate lifecycle events.

**Given** the tenant status enum reserves ordinal 0 as a non-active `Unknown` sentinel (TEN-2 correction)
**When** a tenant snapshot, read model, or query payload is deserialized with a missing or unrecognized status field
**Then** the status resolves to the fail-closed `Unknown` sentinel rather than defaulting to `Active`
**And** the status enum serializes by name, and consuming services never treat an absent status as an active tenant.

### Story 2.6: Return Structured Tenant Governance Rejections

As a tenant API consumer,
I want tenant governance failures to return structured, actionable error responses,
So that I can correct invalid commands without inspecting logs or persisted event payload prose.

**Acceptance Criteria:**

**Given** a tenant governance command fails a business rule
**When** the aggregate returns a rejection
**Then** the rejection event payload contains structured data only
**And** it does not contain localized or user-facing English prose.

**Given** a command targets a missing tenant
**When** the API maps the rejection to an HTTP response
**Then** the response uses RFC 7807 Problem Details
**And** the status code and `type` identify the tenant-not-found rejection.

**Given** a command targets a disabled tenant or duplicate state
**When** the API maps the rejection to an HTTP response
**Then** the response uses the configured rejection-to-status mapping
**And** the response includes a safe corrective action hint composed at the HTTP boundary.

**Given** a command fails authorization or escalation checks
**When** Problem Details are returned
**Then** the response does not leak command payloads, event payloads, tokens, stack traces, or sensitive tenant/user data.

**Given** rejection mapping tests run
**When** all tenant governance rejections are exercised
**Then** each rejection has a deterministic HTTP mapping
**And** unmapped rejections fail tests or use an explicitly documented default.

### Story 2.7: Preserve Command Source of Truth When Pub/Sub Is Unavailable

As a platform operator,
I want tenant governance commands to persist independently of pub/sub availability,
So that tenant state remains durable even when subscribers or messaging infrastructure are temporarily unavailable.

**Acceptance Criteria:**

**Given** a tenant governance command succeeds at the aggregate and event-store stages
**When** DAPR pub/sub is unavailable after the event is stored
**Then** command processing does not roll back the persisted event
**And** the event store remains the source of truth for later recovery.

**Given** pub/sub publication fails after event storage
**When** the failure is handled
**Then** the failure is observable through structured logs or metrics
**And** the log does not classify normal domain rejections as infrastructure errors.

**Given** subscribers are unavailable during tenant lifecycle changes
**When** subscribers or pub/sub recover
**Then** stored tenant events remain available for projection or catch-up processing according to EventStore/DAPR recovery behavior.

**Given** command status or publication status storage fails advisably
**When** the command pipeline completes event storage successfully
**Then** advisory status failure does not invalidate the committed tenant event
**And** the failure is observable for operators.

**Given** integration tests simulate pub/sub unavailability
**When** tenant create, update, disable, and enable commands are submitted
**Then** tests verify command/event storage behavior remains source-of-truth
**And** no duplicate events are produced during recovery.

## Epic 3: Tenant Owners Can Manage Access and Configuration Safely

Tenant owners can manage members, roles, role boundaries, tenant configuration, and tenant-scoped role behavior without cross-tenant leakage or escalation.

### Story 3.1: Add Users to a Tenant with Explicit Roles

As a tenant owner,
I want to add a user to my tenant with a specific tenant role,
So that I can grant access intentionally and produce an auditable membership event.

**Acceptance Criteria:**

**Given** a tenant exists and is enabled
**When** a TenantOwner submits an add-user command with a target user and role of TenantReader, TenantContributor, or TenantOwner
**Then** the tenant aggregate records a user-added-to-tenant event
**And** the event includes top-level `TenantId`, target user ID, assigned role, actor context, and timestamp data.

**Given** a tenant has no membership history
**When** the first membership is added through the approved bootstrap path
**Then** the aggregate allows the first-user membership flow according to the documented empty-tenant bootstrap exception
**And** subsequent membership additions require normal owner or global-admin authority.

**Given** the target user is already a tenant member
**When** a tenant owner submits the add-user command again
**Then** the aggregate returns a structured duplicate membership rejection
**And** no additional user-added event is produced.

**Given** the command attempts to assign a role outside the supported tenant role set
**When** the aggregate or validator handles the command
**Then** the command is rejected before an event is produced
**And** the rejection does not represent GlobalAdministrator as a tenant role.

**Given** membership add tests run
**When** authorized, duplicate, invalid-role, disabled-tenant, and missing-tenant cases are exercised
**Then** tests verify events, rejections, and state transitions without relying on live infrastructure.

**Given** the tenant role enum reserves ordinal 0 as a non-privileged `Unknown` sentinel (TEN-1 correction)
**When** an add-user or change-role command carries `Unknown`, an undefined role, or a payload whose role field is missing or unrecognized
**Then** the command is rejected with a structured role-escalation rejection and no membership event is produced
**And** the role enum serializes by name so a missing or unrecognized role deserializes to the fail-closed `Unknown` sentinel rather than `TenantOwner`.

### Story 3.2: Remove Users from a Tenant

As a tenant owner,
I want to remove a user's tenant membership,
So that I can revoke access while preserving immutable audit history.

**Acceptance Criteria:**

**Given** a tenant exists, is enabled, and contains the target user
**When** a TenantOwner submits a remove-user command
**Then** the tenant aggregate records a user-removed-from-tenant event
**And** the removed user is no longer present in tenant state after the event is applied.

**Given** the target user is not a tenant member
**When** a remove-user command is handled
**Then** the aggregate returns a structured membership-not-found rejection
**And** no removal event is produced.

**Given** the target user is the last TenantOwner in the tenant
**When** a TenantOwner or global administrator submits a valid remove-user command
**Then** the aggregate allows the removal according to the ownership-transfer design
**And** tests document that the backend does not enforce a must-retain-one-owner invariant.

**Given** a removed user attempts a subsequent tenant-owner-only command
**When** the aggregate evaluates tenant membership
**Then** the command is rejected for missing authority
**And** the previous membership does not grant residual access.

**Given** removal tests run
**When** authorized, non-member, disabled-tenant, missing-tenant, and last-owner cases are exercised
**Then** tests verify event production, rejections, and final aggregate state.

### Story 3.3: Change Tenant User Roles with Escalation Protection

As a tenant owner,
I want to change a member's tenant role,
So that access can be adjusted without allowing unauthorized privilege escalation.

**Acceptance Criteria:**

**Given** a tenant exists, is enabled, and contains the target user
**When** a TenantOwner changes the user's role to TenantReader, TenantContributor, or TenantOwner
**Then** the tenant aggregate records a user-role-changed event
**And** the applied tenant state reflects the new role.

**Given** the target user is not a tenant member
**When** a role-change command is handled
**Then** the aggregate returns a structured membership-not-found rejection
**And** no role-changed event is produced.

**Given** an actor attempts to assign GlobalAdministrator through a tenant role command
**When** the command is validated or handled
**Then** the operation is rejected as a role escalation violation
**And** global administrator state is not modified.

**Given** a non-owner tenant member attempts to change another user's role
**When** the aggregate evaluates authorization
**Then** the command is rejected for missing owner authority
**And** no role-changed event is produced.

**Given** role-change tests run
**When** every allowed tenant role transition and escalation path is exercised
**Then** tests verify state changes only for allowed transitions
**And** role escalation branch coverage is included in CI expectations.

### Story 3.4: Enforce Tenant-Scoped Role Behavior

As a tenant user,
I want my tenant role to grant only the capabilities intended for that tenant,
So that access does not escalate or leak across tenants.

**Acceptance Criteria:**

**Given** a user is TenantReader in a tenant
**When** the user attempts a tenant state-changing command
**Then** the command is rejected for insufficient tenant authority
**And** no state-changing event is produced.

**Given** a user is TenantContributor in a tenant
**When** the user's tenant role is evaluated
**Then** the user has reader-level visibility and contributor-level domain-command capability for consuming-service semantics
**And** the user cannot manage tenant membership, tenant roles, or tenant configuration.

**Given** a user is TenantOwner in a tenant
**When** the user's tenant role is evaluated for membership or configuration commands
**Then** the user can perform owner-authorized membership and configuration operations
**And** owner authority remains scoped to that tenant only.

**Given** a user has different roles in multiple tenants
**When** the user acts against tenant A
**Then** only the user's role in tenant A is considered
**And** roles from tenant B do not transfer or aggregate across tenants.

**Given** a trusted global-admin command envelope extension is present
**When** tenant role authorization is evaluated
**Then** global-admin authority can bypass per-tenant role checks
**And** the bypass is based on trusted envelope metadata, not user-supplied claims or command payload fields.

**Given** role behavior tests run
**When** reader, contributor, owner, global-admin, missing-member, and cross-tenant cases are exercised
**Then** tests prove tenant isolation and role authorization branch coverage.

### Story 3.5: Set Tenant Configuration Entries

As a tenant owner,
I want to set tenant configuration entries with namespaced keys,
So that consuming services can react to tenant-specific settings through domain events.

**Acceptance Criteria:**

**Given** a tenant exists, is enabled, and the actor has TenantOwner or global-admin authority
**When** the actor sets a configuration key and value within allowed limits
**Then** the tenant aggregate records a tenant-configuration-set event
**And** the applied tenant state contains the new or updated key-value entry.

**Given** the configuration key uses dot-delimited namespaces such as `billing.plan` or `parties.maxContacts`
**When** the command is handled
**Then** the key is accepted if it satisfies validation rules
**And** the event preserves the exact key for consuming services.

**Given** a non-owner tenant member attempts to set configuration
**When** the aggregate evaluates authorization
**Then** the command is rejected for insufficient tenant authority
**And** no configuration event is produced.

**Given** the target tenant is missing or disabled
**When** a configuration set command is handled
**Then** the aggregate returns the appropriate structured tenant-not-found or disabled-tenant rejection
**And** no configuration state is changed.

**Given** configuration set tests run
**When** new keys, existing keys, namespaced keys, unauthorized actors, missing tenants, and disabled tenants are exercised
**Then** tests verify event production, state mutation, and rejection outcomes.

### Story 3.6: Remove Tenant Configuration Entries

As a tenant owner,
I want to remove tenant configuration entries,
So that obsolete or incorrect tenant-specific settings stop influencing consuming services.

**Acceptance Criteria:**

**Given** a tenant exists, is enabled, and contains the configuration key
**When** a TenantOwner removes the configuration entry
**Then** the tenant aggregate records a tenant-configuration-removed event
**And** the applied tenant state no longer contains the key.

**Given** the requested configuration key does not exist
**When** a remove-configuration command is handled
**Then** the aggregate returns a structured configuration-key-not-found rejection
**And** no tenant-configuration-removed event is produced.

**Given** a non-owner tenant member attempts to remove configuration
**When** authorization is evaluated
**Then** the command is rejected for insufficient tenant authority
**And** no configuration state is changed.

**Given** the target tenant is missing or disabled
**When** a configuration remove command is handled
**Then** the aggregate returns the appropriate structured tenant rejection
**And** no configuration removal event is produced.

**Given** configuration removal tests run
**When** existing-key, missing-key, unauthorized, disabled-tenant, and missing-tenant cases are exercised
**Then** tests verify state mutation, rejection outcomes, and serialization round trips for configuration events.

### Story 3.7: Enforce Tenant Configuration Limits

As a tenant owner,
I want configuration limit violations to be rejected clearly,
So that tenant settings remain bounded and safe for event storage and consumers.

**Acceptance Criteria:**

**Given** a tenant has fewer than the maximum allowed configuration keys
**When** a TenantOwner adds a valid new key
**Then** the configuration set command succeeds
**And** the aggregate state remains within the maximum 100-key limit.

**Given** a tenant already has the maximum allowed number of configuration keys
**When** a TenantOwner attempts to add another distinct key
**Then** the aggregate returns a structured configuration-key-limit rejection
**And** the rejection identifies the limit and current usage with structured fields.

**Given** a configuration key exceeds 256 characters or violates required key validation
**When** the command is validated or handled
**Then** the command is rejected with a structured key-length or key-format rejection
**And** no configuration event is produced.

**Given** a configuration value exceeds the maximum 1KB value length
**When** the command is validated or handled
**Then** the command is rejected with a structured value-length rejection
**And** the rejection identifies the configured limit without storing the oversized value.

**Given** configuration limit tests run
**When** boundary values for key count, key length, value length, empty keys, namespaced keys, and update-existing-key cases are exercised
**Then** tests prove limits are enforced at the correct boundary
**And** persisted rejection payloads contain structured data only.

### Story 3.8: Reject Conflicting Concurrent Tenant Modifications

As a tenant owner,
I want conflicting tenant access and configuration changes to be rejected predictably,
So that concurrent administration does not silently overwrite tenant state.

**Acceptance Criteria:**

**Given** two actors submit conflicting membership commands against the same tenant aggregate version
**When** EventStore optimistic concurrency is evaluated
**Then** one command succeeds according to ordering rules
**And** the conflicting command returns a structured concurrency conflict outcome to the caller after the command pipeline's bounded retry policy is exhausted.

**Given** the command pipeline performs any automatic retry
**When** retry behavior is documented
**Then** the retry limit, retryable conflict conditions, final rejection mapping, and idempotency interaction are specified in the story implementation notes
**And** tests verify both successful retry and exhausted-retry conflict outcomes where the EventStore API supports them.

**Given** two actors submit conflicting role-change commands for the same user
**When** the aggregate state version differs from the expected command context
**Then** the conflict is surfaced as a structured concurrency outcome
**And** no silent overwrite of role state occurs.

**Given** concurrent configuration commands modify the same key
**When** the commands are processed against the aggregate
**Then** the final state corresponds to a valid ordered event sequence
**And** conflicting command results are observable to callers.

**Given** duplicate command replay occurs after a transient failure
**When** EventStore idempotency and aggregate behavior evaluate the command
**Then** duplicate events are not produced
**And** the caller receives a deterministic success, rejection, or no-op outcome.

**Given** concurrency tests run
**When** add, remove, change-role, set-configuration, and remove-configuration conflicts are simulated
**Then** tests verify deterministic event sequences, rejection behavior, and final aggregate state.

## Epic 4: Consuming Services Can React to Tenant Events

Consuming services can subscribe to tenant events, build local projections, handle idempotency, and react to access, lifecycle, and configuration changes.

### Story 4.1: Publish Tenant Domain Events as CloudEvents

As a consuming service developer,
I want tenant domain events to publish through a documented DAPR topic as CloudEvents,
So that my service can subscribe to tenant changes without direct infrastructure coupling.

**Acceptance Criteria:**

**Given** a tenant lifecycle, membership, role, configuration, or global-administrator domain event is persisted
**When** event publication runs
**Then** the event is published through DAPR pub/sub as a CloudEvents 1.0 message
**And** publication uses the tenant event topic `tenants.events`.

**Given** a tenant event payload is published
**When** a consumer receives the event
**Then** the payload contains top-level managed `TenantId`
**And** the envelope/platform tenant remains `system` according to EventStore conventions.

**Given** DAPR resource naming is reviewed
**When** tenant event publication is configured
**Then** the AppId, topic, state store, and dead-letter topic follow the documented conventions
**And** no direct broker-specific dependency is introduced in domain code.

**Given** consumers subscribe to `tenants.events`
**When** multiple event types are delivered on the shared topic
**Then** consumers can filter by event type
**And** documentation or sample code does not assume a separate topic per event type.

**Given** publication tests run
**When** representative tenant events are emitted
**Then** tests verify CloudEvents shape, topic naming, tenant identity fields, and event type metadata.

### Story 4.2: Expose Consumer DI Registration for Tenant Client Services

As a consuming service developer,
I want to register tenant client services with one DI extension method,
So that tenant integration setup remains small and repeatable across services.

**Acceptance Criteria:**

**Given** a consuming service references the Client package
**When** the developer calls the tenant client registration extension
**Then** the required tenant client services are registered in `IServiceCollection`
**And** the extension returns `IServiceCollection` for chaining.

**Given** the registration extension is called with default options
**When** the consuming service starts
**Then** the client services use documented defaults compatible with EventStore and DAPR conventions
**And** no server-only implementation types are required by the consumer.

**Given** the registration extension is called with configuration options
**When** the developer supplies valid settings
**Then** the extension binds and validates those settings consistently with the project options pattern
**And** invalid settings fail clearly during startup or validation.

**Given** a developer reviews the public API
**When** they inspect Client package registration methods
**Then** there is a single documented primary registration path
**And** public extension methods include the existing project style of XML documentation.

**Given** client registration tests run
**When** services are registered with default and configured options
**Then** tests verify expected service descriptors are present
**And** no host, AppHost, or Server-only dependency is introduced into consumer registration.

### Story 4.3: Register Tenant Event Handlers in Under Twenty Lines

As a consuming service developer,
I want a concise event-handler registration pattern,
So that a service can become tenant-aware without bespoke integration code.

**Acceptance Criteria:**

**Given** a consuming service references Contracts and Client packages
**When** the developer registers tenant event handlers using the documented pattern
**Then** the registration requires under 20 lines of DI configuration for the standard integration path
**And** the code compiles without referencing tenant host or server projects.

**Given** the consuming service needs only selected tenant event types
**When** event handlers are registered
**Then** the registration supports filtering or dispatch by event type
**And** handlers are not required to process unrelated event types.

**Given** DAPR invokes a tenant event subscription handler
**When** the event is received
**Then** the handler resolves consumer services through DI
**And** it can process events without direct broker-specific APIs.

**Given** handler registration is invalid or incomplete
**When** the consuming service starts or receives an event
**Then** the failure mode is clear and actionable for the developer
**And** sensitive event payloads are not logged as troubleshooting output.

**Given** registration tests and sample compilation run
**When** the sample consuming service builds
**Then** the handler registration remains under the target line count
**And** the sample proves the documented registration path.

### Story 4.4: Build a Local Consumer Projection from Tenant Events

As a consuming service developer,
I want to build a local projection from tenant events,
So that my service can enforce tenant-aware behavior using its own runtime state.

**Acceptance Criteria:**

**Given** a consuming service subscribes to tenant events
**When** `TenantCreated`, `TenantUpdated`, `TenantDisabled`, and `TenantEnabled` events are received
**Then** the local projection can maintain tenant lifecycle state
**And** it does not query Tenants synchronously for every consuming-service decision.

**Given** membership events are received
**When** users are added, removed, or assigned new roles
**Then** the local projection can maintain user-to-tenant role state
**And** removed users no longer appear as authorized members after projection processing.

**Given** events may be delivered at least once
**When** the same event is received more than once
**Then** the projection handles the duplicate idempotently
**And** no duplicate memberships, role transitions, or lifecycle records are created.

**Given** events from different services or subscriptions may arrive at different times
**When** a consuming service projection is updated
**Then** the implementation and documentation do not assume cross-service ordering
**And** consumers are guided to design for eventual consistency.

**Given** projection tests run in the sample or client test suite
**When** representative lifecycle and membership event sequences are applied
**Then** tests verify deterministic local projection state and idempotent duplicate handling.

### Story 4.5: React to Tenant Access, Lifecycle, and Configuration Changes

As a consuming service developer,
I want tenant events to trigger access, availability, and configuration reactions,
So that downstream services update behavior automatically when tenant state changes.

**Acceptance Criteria:**

**Given** a `UserAddedToTenant` event is processed
**When** the consuming service updates its local state
**Then** the user can be granted the role-specific local capability represented by that service
**And** the projection records enough event metadata for idempotent processing.

**Given** a `UserRemovedFromTenant` event is processed
**When** the consuming service updates its local state
**Then** the user's local tenant access is revoked
**And** repeated delivery of the removal event does not produce an error or duplicate side effect.

**Given** a `TenantDisabled` event is processed
**When** the consuming service evaluates tenant operations
**Then** tenant operations are blocked or degraded according to the consuming service policy
**And** the behavior is documented as eventually consistent with the tenant event stream.

**Given** a `TenantEnabled` event is processed
**When** the consuming service evaluates tenant operations
**Then** normal tenant operations can resume after the local projection reflects the event.

**Given** a tenant configuration set or removed event is processed
**When** the consuming service reads tenant-specific configuration from its local projection
**Then** namespaced configuration keys are applied or removed deterministically
**And** unrelated service namespaces are ignored unless explicitly handled.

**Given** reaction tests run
**When** access, lifecycle, and configuration event sequences are processed
**Then** tests prove the consuming service reacts without custom polling, sync jobs, or per-service Tenants API calls.

### Story 4.6: Provide Idempotent Consumer Guidance and Sample Service

As a developer evaluating Tenants,
I want a sample consuming service and idempotency guidance,
So that I can copy a safe event-driven integration pattern into my own service.

**Acceptance Criteria:**

**Given** the sample consuming service is opened
**When** a developer reviews tenant integration code
**Then** the sample demonstrates tenant event subscription, DI registration, local projection update, access revocation, lifecycle handling, and configuration reaction
**And** the standard setup remains under the documented integration-code target.

**Given** the idempotent event processing documentation is reviewed
**When** a developer follows the guidance
**Then** it explains DAPR at-least-once delivery
**And** it includes a deduplication-by-event-ID example and idempotent handler pattern with code.

**Given** tenant events include event ID and aggregate version metadata
**When** the sample handles events
**Then** it stores or checks enough metadata to avoid duplicate side effects
**And** it uses aggregate version or event ordering only within documented limits.

**Given** the sample demonstrates access revocation
**When** a user is removed from a tenant
**Then** the sample shows the consuming service revoking local access based on the tenant event stream
**And** no custom polling or manual synchronization job is required.

**Given** sample validation runs
**When** the sample and documentation snippets are built or tested
**Then** code samples compile against the published package surface
**And** docs do not rely on internal project references or unavailable infrastructure for basic understanding.

## Epic 5: Operators and Developers Can Query Tenant State and Audit Access

Users can query tenants, tenant details, users, user memberships, and audit history through safe cursor-based APIs backed by durable projections.

**Implementation sequencing:** Complete projection write safety, query-side authorization, and cursor security before endpoint delivery. Endpoint stories must prove that returned rows, cursors, errors, and pagination metadata do not leak hidden tenant data, and that the projection state required by the endpoint does not silently overwrite successfully processed events.

### Story 5.1: Persist Per-Tenant Detail Projections Without Silent Write Loss

As a platform operator,
I want per-tenant detail projections to handle concurrent writes safely,
So that tenant detail and user query results do not silently lose tenant events.

**Acceptance Criteria:**

**Given** multiple tenant events update the per-tenant detail projection close together
**When** projection state is persisted
**Then** the write path uses optimistic concurrency, ETag-aware writes, or verified `CachingProjectionActor` fan-in behavior
**And** no successful event update is silently overwritten.

**Given** per-tenant detail projection write conformance tests run
**When** tenant detail projection writes race
**Then** tests prove no silent data loss, deterministic recovery behavior, and enough diagnostics for replay or repair.

### Story 5.2: Persist the Shared Tenant Index Projection Without Silent Write Loss

As a platform operator,
I want the shared tenant index projection to handle concurrent writes safely,
So that tenant discovery does not silently lose tenant lifecycle events.

**Acceptance Criteria:**

**Given** multiple tenant events update the shared tenant index projection
**When** the shared projection state is modified
**Then** conflicting writes are retried or safely failed according to a documented retry policy
**And** final index state includes all successfully processed events.

**Given** shared tenant index projection write conformance tests run
**When** tenant index projection writes race
**Then** tests prove no silent data loss, deterministic recovery behavior, and enough diagnostics for replay or repair.

### Story 5.3: Persist the Tenant Audit Projection Without Silent Write Loss

As a platform operator,
I want the tenant audit projection to handle concurrent writes safely,
So that audit reports remain complete and ordered under access-change concurrency.

**Acceptance Criteria:**

**Given** multiple access-change events update the audit projection close together
**When** audit state is persisted
**Then** every successfully processed audit event remains queryable by date range and pagination cursor
**And** ordering remains deterministic.

**Given** audit projection write conformance tests run
**When** tenant audit projection writes race
**Then** tests prove no silent data loss, deterministic recovery behavior, and enough diagnostics for replay or repair.

### Story 5.4: Expose Projection Write Conflict Diagnostics and Recovery Evidence

As a platform operator,
I want projection write conflicts to be observable and recoverable,
So that projection failures are not mistaken for successful read-model updates.

**Acceptance Criteria:**

**Given** a projection write conflict exceeds the retry limit
**When** the projection cannot safely persist state
**Then** the failure is observable through structured logs or metrics
**And** the projection does not falsely report a successful update.

**Given** replay or repair evidence is needed
**When** projection conflict diagnostics are inspected
**Then** logs or metrics include support-safe tenant, domain, aggregate, projection type, event type, correlation, causation, and retry metadata
**And** they do not expose raw payloads, tokens, secrets, or PII.

### Story 5.5: Enforce Query-Side Authorization and Isolation

As a security-conscious platform owner,
I want query endpoints to filter results by requester scope,
So that tenant data is never exposed across tenant or role boundaries.

**Acceptance Criteria:**

**Given** a caller has no membership or global-admin authority for a tenant
**When** the caller requests tenant details, users, user memberships, or audit data
**Then** unauthorized rows are not returned
**And** the response does not reveal hidden tenant data through errors or pagination metadata.

**Given** a TenantReader queries their own tenant
**When** query-side authorization is evaluated
**Then** read-only tenant detail and user-list access is allowed
**And** no state-changing command authority is granted by the query path.

**Given** a TenantOwner queries scoped user access
**When** the target user has memberships across multiple tenants
**Then** only rows for tenants controlled by the owner are returned
**And** rows from other tenants are absent without disclosure.

**Given** a global administrator queries tenant state or audit data
**When** query-side authorization is evaluated
**Then** cross-tenant query visibility is allowed according to global-admin policy
**And** the response still uses safe DTOs, cursor tokens, and Problem Details.

**Given** cross-tenant isolation tests run
**When** query endpoints, projections, cursors, and error bodies are exercised across multiple tenants and users
**Then** tests verify zero cross-tenant data leaks
**And** coverage includes unauthorized, partially authorized, and global-admin cases.

### Story 5.6: Provide Safe Cursor-Based Pagination for Query Endpoints

As a tenant API consumer,
I want all tenant query endpoints to use safe cursor-based pagination,
So that I can page through results consistently without leaking tenant data.

**Acceptance Criteria:**

**Given** a tenant list, tenant users, user-tenants, or audit query returns more than one page
**When** the first page is returned
**Then** the response includes an opaque next cursor
**And** the cursor can be used to request the next page with stable ordering.

**Given** a cursor is malformed, expired, or mismatched to the endpoint or requester scope
**When** the cursor is submitted
**Then** the endpoint returns a safe validation error
**And** the response does not reveal embedded tenant IDs, user IDs, filters, or internal state.

**Given** list data changes between page requests
**When** a caller continues paging
**Then** the endpoint preserves the documented ordering and consistency behavior
**And** duplicate or skipped records are handled according to the selected cursor strategy.

**Given** a caller attempts to use a cursor generated for another tenant, user, or authorization scope
**When** the endpoint validates the cursor
**Then** the request is rejected or returns no unauthorized rows
**And** cross-tenant leakage is prevented.

**Given** pagination tests run across all list/query endpoints
**When** default size, maximum size, invalid cursor, scope-mismatched cursor, and concurrent data-change cases are exercised
**Then** tests verify consistency, security, and endpoint-specific behavior.

### Story 5.7: Query a Paginated Tenant List

As a developer,
I want to query a paginated list of tenants with status information,
So that I can discover existing tenants and decide which tenant to inspect.

**Acceptance Criteria:**

**Given** tenant lifecycle events have been projected
**When** a caller requests `GET /api/tenants`
**Then** the response returns tenant IDs, names, statuses, and pagination metadata
**And** the result ordering is deterministic across pages.

**Given** no tenants match the request
**When** the tenant list endpoint is called
**Then** the response returns an empty page using the standard query response shape
**And** it does not return an error for an empty result set.

**Given** a tenant has been disabled or re-enabled
**When** the list query is served from projections
**Then** the tenant status reflects the latest successfully projected lifecycle event
**And** stale projection behavior is documented as eventual consistency.

**Given** the caller supplies page size parameters
**When** the requested page size is omitted, valid, or above the maximum
**Then** the endpoint applies the default page size, accepts valid sizes, and enforces the configured maximum.

**Given** tenant list query tests run
**When** active, disabled, empty, paginated, and invalid-parameter cases are exercised
**Then** tests verify response shape, ordering, and safe error behavior.

### Story 5.8: Query Tenant Details and Tenant Users

As a tenant user,
I want to query tenant details and the tenant's users,
So that I can inspect the current tenant state allowed by my role.

**Acceptance Criteria:**

**Given** a tenant exists and its projection has been updated
**When** an authorized caller requests `GET /api/tenants/{tenantId}`
**Then** the response includes tenant metadata, status, users, roles, and configuration visible to that caller
**And** the response uses typed query DTOs rather than anonymous response shapes.

**Given** a tenant exists and contains users
**When** an authorized caller requests `GET /api/tenants/{tenantId}/users`
**Then** the response returns the tenant's users with assigned roles
**And** the endpoint supports pagination if the user list exceeds one page.

**Given** the requested tenant does not exist
**When** tenant detail or users are queried
**Then** the API returns a safe not-found response
**And** it does not reveal data from another tenant or internal projection keys.

**Given** a caller has TenantReader or higher authority for the tenant
**When** the caller queries details or users
**Then** read access is allowed according to tenant role behavior
**And** no state-changing authority is implied by query access.

**Given** tenant detail and users query tests run
**When** enabled, disabled, missing, empty-users, multi-page, and unauthorized cases are exercised
**Then** tests verify filtering, response shape, status codes, and isolation.

### Story 5.9: Query the Tenants a User Belongs To

As a developer or administrator,
I want to query the list of tenants a user belongs to,
So that user access can be reviewed without scanning every tenant manually.

**Acceptance Criteria:**

**Given** a user belongs to one or more tenants
**When** an authorized caller requests `GET /api/users/{userId}/tenants`
**Then** the response returns each visible tenant with the user's role in that tenant
**And** results are ordered consistently for pagination.

**Given** the requester asks for their own tenant memberships
**When** query-side authorization is evaluated
**Then** the requester can see their own allowed membership rows
**And** rows outside their authorized scope are not returned.

**Given** a TenantOwner queries another user's tenant memberships
**When** the target user has memberships in tenants the owner does and does not control
**Then** only memberships visible through the owner's tenant scope are returned
**And** memberships in other tenants are excluded without leaking their existence.

**Given** a global administrator queries a user's tenant memberships
**When** query-side authorization is evaluated
**Then** the global administrator can see memberships across tenants
**And** the result still uses pagination and stable ordering.

**Given** user-tenants query tests run
**When** self, tenant-owner, global-admin, missing-user, no-membership, and cross-tenant cases are exercised
**Then** tests prove row-level filtering and zero cross-tenant leakage.

### Story 5.10: Query Tenant Access Audit History

As a global administrator,
I want to query tenant access changes by tenant and date range,
So that I can reconstruct who changed access and when.

**Acceptance Criteria:**

**Given** tenant lifecycle, membership, role, configuration, and global-admin events have been projected into audit state
**When** a global administrator requests `GET /api/tenants/{tenantId}/audit` with a date range
**Then** the response returns matching audit entries for that tenant
**And** each entry includes support-safe actor, target, scope, outcome, timestamp, and event reference data.

**Given** no audit entries match the date range
**When** the audit endpoint is called
**Then** the response returns an empty page
**And** the empty result does not imply that the tenant is missing unless the tenant itself cannot be found.

**Given** the caller is not a global administrator or otherwise authorized for audit review
**When** the audit endpoint is called
**Then** the request is rejected or filtered according to the documented authorization policy
**And** audit data is not leaked through status codes, cursor tokens, or error bodies.

**Given** the caller requests a page size
**When** the page size is omitted, valid, or above the maximum
**Then** the endpoint uses the default page size of 100, accepts valid sizes, and enforces the maximum page size of 1,000.

**Given** audit query tests run
**When** date boundaries, empty results, multi-page results, unauthorized access, and missing-tenant cases are exercised
**Then** tests verify audit completeness, ordering, pagination, and safe failures.

## Epic 6: Developers Can Test Tenant Behavior Without Infrastructure

Developers can write fast tenant integration tests using in-memory fakes that execute production-equivalent domain behavior.

### Story 6.1: Provide In-Memory Tenant Test Fakes

As a developer,
I want in-memory tenant test fakes that require no external infrastructure,
So that I can write tenant integration tests quickly in ordinary unit-test projects.

**Acceptance Criteria:**

**Given** a test project references `Hexalith.Tenants.Testing`
**When** a developer creates the in-memory tenant fake using the documented helper
**Then** the fake can execute tenant commands without DAPR, Aspire, Docker, or a live EventStore process
**And** setup remains small enough for the documented under-10-lines target.

**Given** the fake is initialized
**When** a test submits tenant lifecycle, membership, role, and configuration commands
**Then** the fake returns success, rejection, or no-op outcomes using the same domain result semantics as production
**And** events are retained in memory for test assertions.

**Given** a test needs deterministic setup
**When** the fake is created for a new test
**Then** it starts from an isolated empty state unless seeded explicitly
**And** previous tests cannot leak tenant state into the new test.

**Given** invalid commands are submitted to the fake
**When** business rules reject the command
**Then** the fake exposes the same structured rejection event type expected from production domain logic
**And** no infrastructure exception is required to assert the business failure.

**Given** fake setup tests run
**When** basic create, add-user, remove-user, role-change, set-configuration, and rejection flows are exercised
**Then** tests verify the fake can be used without external infrastructure
**And** command execution remains fast enough to support ordinary unit-test workflows.

**Given** the testing fakes expose EventStore's `DomainResult` as their public result type (TEN-5 decision)
**When** the public `Hexalith.Tenants.Testing` surface is reviewed
**Then** returning `DomainResult` is documented as intentional in an architecture decision record, because the type is in-tier and reused by consuming-service tests without added coupling
**And** no wrapper type and no consuming-service architecture-fitness restriction are introduced.

### Story 6.2: Reuse Production Aggregate Logic in Testing Fakes

As a developer,
I want in-memory fakes to execute the same aggregate logic as production,
So that my tests do not pass against behavior that can drift from the deployed service.

**Acceptance Criteria:**

**Given** the testing fake handles a tenant command
**When** the fake evaluates the command
**Then** it invokes the same pure aggregate `Handle` logic used by production
**And** it applies resulting events through the same state `Apply` methods.

**Given** production aggregate logic changes
**When** the testing package is built and tested
**Then** fake behavior changes with the production aggregate behavior
**And** no duplicate hand-written fake rules must be maintained separately.

**Given** a command depends on command-envelope identity, aggregate ID, or trusted global-admin metadata
**When** the fake executes the command
**Then** the fake supplies equivalent envelope context through documented test helpers
**And** command bodies cannot override aggregate identity in fake execution.

**Given** domain business rules reject a command
**When** the same command is executed through production aggregate logic and through the fake
**Then** both paths produce equivalent structured rejection outcomes
**And** tests verify equality without relying on localized message text.

**Given** fake implementation code is reviewed
**When** maintainers inspect dependencies
**Then** the Testing package can depend on Server for production domain logic where required
**And** it does not introduce reverse dependencies from Contracts or Server into Testing.

### Story 6.3: Add Production/Fake Conformance Tests

As a maintainer,
I want conformance tests that compare in-memory fakes with production aggregate behavior,
So that fake behavior remains trustworthy across every command type.

**Acceptance Criteria:**

**Given** tenant command contracts are available
**When** conformance tests discover or enumerate supported command types
**Then** each tenant lifecycle, membership, role, configuration, and global-administration command is included in the conformance suite
**And** skipped command types must be explicitly justified.

**Given** a conformance command sequence is executed against production aggregate logic
**When** the same sequence is executed through the testing fake
**Then** both paths produce equivalent event and rejection sequences
**And** final aggregate state is equivalent for the tested scope.

**Given** authorization context matters for a command
**When** conformance tests execute the command
**Then** authorized, unauthorized, global-admin, missing-member, disabled-tenant, and duplicate-operation variants are covered where applicable.

**Given** a new command type is added in Contracts or Server
**When** conformance tests run
**Then** the missing command is detected by the conformance coverage mechanism
**And** the test suite fails until the command is added to conformance coverage.

**Given** conformance tests fail
**When** the failure output is reviewed
**Then** the output identifies the command sequence and differing event or rejection type
**And** it does not dump sensitive command payloads or secrets.

**Given** new tenant success events may be added to `Contracts.Events` over time (TEN-4 correction)
**When** the projection-conformance test enumerates every non-rejection event payload type
**Then** it asserts each type is explicitly handled by `InMemoryTenantProjection.Apply` and none reaches the silent `default:` arm
**And** adding an unwired success event fails the conformance test inside the Tenants test suite.

### Story 6.4: Support Consumer Tenant Isolation Tests

As a consuming service developer,
I want testing helpers for tenant isolation scenarios,
So that my service can prove its own projections and access checks do not leak tenant data.

**Acceptance Criteria:**

**Given** a consumer test uses the Testing package
**When** the developer creates multiple tenants and users in memory
**Then** helpers make it straightforward to seed tenant memberships, roles, and lifecycle state
**And** the test can assert behavior for separate tenant contexts without live infrastructure.

**Given** a consumer projection subscribes to fake tenant events
**When** membership and lifecycle events are emitted by the fake
**Then** the consumer can verify its local projection reacts to tenant A without adding tenant B data
**And** duplicate event delivery can be simulated for idempotency checks.

**Given** a user has roles in multiple tenants
**When** a consumer test evaluates access for each tenant
**Then** helpers support asserting tenant-specific authorization
**And** roles from one tenant do not implicitly authorize another tenant.

**Given** a consumer wants to test removal and revocation
**When** the fake emits user-added and user-removed event sequences
**Then** the consumer can assert access grant and revocation behavior in under ordinary unit-test timing
**And** no polling, Docker, DAPR sidecar, or network call is required.

**Given** helper documentation is reviewed
**When** developers follow examples
**Then** documentation clearly states that aggregate-level fake parity is provided
**And** consuming services remain responsible for testing their own projection-level and query-level isolation.

## Epic 7: Operators Can Deploy, Secure, and Observe Production Tenants

Operators can deploy Tenants alongside EventStore with Aspire/DAPR, production auth validation, telemetry, health, stateless operation, and recovery behavior.

### Story 7.1: Provide Aspire Hosting Extensions for Tenants

As a developer deploying Tenants,
I want Aspire hosting extensions for the tenant service,
So that I can add Tenants to an AppHost through the documented Hexalith integration path.

**Acceptance Criteria:**

**Given** an AppHost references `Hexalith.Tenants.Aspire`
**When** the developer calls the tenant hosting extension
**Then** the Tenants service is added to the distributed application model
**And** the extension returns the expected Aspire builder type for fluent composition.

**Given** the hosting extension is configured with default options
**When** the AppHost is built
**Then** the Tenants host uses the documented AppId, domain, state store, pub/sub topic, and service invocation conventions
**And** no inline duplicated AppHost wiring is required in consumer applications.

**Given** the hosting extension is configured with custom deployment options
**When** valid options are supplied
**Then** options are validated consistently with the project configuration pattern
**And** invalid options fail early with actionable errors.

**Given** the package boundary is reviewed
**When** the Aspire package is inspected
**Then** it exposes hosting composition only
**And** it does not own tenant domain rules or command handling behavior.

**Given** Aspire extension tests run
**When** default and configured AppHost setups are exercised
**Then** tests verify resource names, service references, DAPR sidecar wiring, and package boundaries.

### Story 7.2: Configure DAPR Components for Local and Production Deployment

As a platform operator,
I want standard DAPR component configuration for Tenants,
So that the tenant service can run beside EventStore using portable actors, state, pub/sub, and service invocation.

**Acceptance Criteria:**

**Given** local or production DAPR components are reviewed
**When** Tenants is deployed
**Then** actor, state store, pub/sub, and service invocation configuration match the documented Tenants/EventStore conventions
**And** domain code does not directly depend on Redis, brokers, or databases.

**Given** DAPR access control is enabled
**When** service invocation paths are configured
**Then** callers and receivers are explicit according to deny-by-default policy
**And** the Tenants domain processor route is reachable only through approved service invocation paths.

**Given** the domain processor route is configured
**When** EventStore aggregate actors invoke Tenants domain processing
**Then** the Tenants host exposes the required processing endpoint
**And** the route participates in the normal authentication, authorization, telemetry, and error-handling pipeline where applicable.

**Given** DAPR slim or local mode is used
**When** operators follow the setup guidance
**Then** placement and scheduler prerequisites are documented or validated
**And** actor startup failures point to the missing prerequisite rather than ambiguous command failures.

**Given** deployment configuration tests or AppHost diagnostics run
**When** DAPR components are missing or misnamed
**Then** startup or diagnostics fail clearly
**And** the failure identifies the component or AppId mismatch.

### Story 7.3: Validate Production Authentication and EventStore Tenant Claims

As a platform operator,
I want production authentication configuration validated before deployment,
So that tenant operations are authorized consistently and unsafe defaults do not reach production.

**Acceptance Criteria:**

**Given** production configuration omits required JWT authority, audience, signing, or metadata settings
**When** the Tenants service starts in production mode
**Then** startup validation fails with a clear configuration error
**And** logs identify missing keys without exposing secrets or token material.

**Given** valid production JWT settings are supplied through environment variables, AppHost, or deployment configuration
**When** the service starts
**Then** authentication options validate successfully
**And** no committed appsettings file needs to contain production secrets.

**Given** a production identity provider issues a token for tenant-management operations
**When** the token reaches EventStore tenant validation
**Then** it contains or is normalized to `eventstore:tenant=system`
**And** tenant validation does not fall into a shared anonymous partition silently.

**Given** a token is missing the `eventstore:tenant` claim
**When** a protected tenant endpoint is called in production mode
**Then** the request is rejected with a safe authentication or authorization failure
**And** no command, query, projection, or rate-limit partition uses an anonymous or fallback tenant.

**Given** a token has an invalid `eventstore:tenant` claim
**When** a protected tenant endpoint is called in production mode
**Then** the request is rejected fail-closed
**And** logs identify the claim contract failure without exposing token material.

**Given** auth tests run
**When** production-valid, production-invalid, development-valid, missing-claim, and wrong-claim tokens are exercised
**Then** tests verify startup validation, authorization behavior, and safe failure responses.

**Given** the identifier-casing contract is documented in `docs/production-auth-claim-contract.md` (TEN-3 correction)
**When** `sub`/userId and managed `tenantId` values are compared for membership, projection, or claim matching
**Then** comparison is case-sensitive (`StringComparer.Ordinal`) and a casing mismatch fails closed by design
**And** canonical casing is the identity provider's and operator's responsibility, so consuming services rely on the published contract instead of case-folding claims.

### Story 7.4: Expose Tenant Command and Event Metrics with OpenTelemetry

As a platform operator,
I want tenant command and event metrics through OpenTelemetry,
So that I can observe latency, failures, and processing health in production.

**Acceptance Criteria:**

**Given** tenant commands are submitted
**When** command processing completes, rejects, or fails
**Then** OpenTelemetry spans or metrics record command latency
**And** smoke-level telemetry presence checks run in the normal implementation lane
**And** p95 command duration evidence is classified as release evidence or scheduled performance evidence unless explicitly approved as a blocking CI gate.

**Given** tenant events are published or projected
**When** event processing completes, retries, or fails
**Then** OpenTelemetry spans or metrics record event processing latency and outcome
**And** smoke-level telemetry presence checks run in the normal implementation lane
**And** p95 event publication duration evidence is classified as release evidence or scheduled performance evidence unless explicitly approved as a blocking CI gate.

**Given** query endpoints are called
**When** read model queries complete
**Then** query latency is observable
**And** smoke-level telemetry presence checks run in the normal implementation lane
**And** p95 query duration evidence for single-page result sets is classified as release evidence or scheduled performance evidence unless explicitly approved as a blocking CI gate.

**Given** telemetry is emitted
**When** logs and spans are inspected
**Then** they include support-safe correlation, tenant, domain, aggregate, causation, command/event type, and stage metadata
**And** they do not include command payloads, event payloads, tokens, secrets, or PII.

**Given** telemetry tests or manual verification run
**When** successful, rejected, failed, and delayed operations are exercised
**Then** metrics and structured logs distinguish normal domain rejections from infrastructure failures.

### Story 7.5: Prove Stateless Operation, Health, and Startup Reconstruction

As a platform operator,
I want Tenants to expose reliable health and reconstruct state from EventStore,
So that horizontal scaling and recovery are predictable.

**Acceptance Criteria:**

**Given** a Tenants service instance starts
**When** it becomes ready
**Then** health checks reflect required dependencies and service readiness
**And** readiness does not claim success before required DAPR/EventStore dependencies are usable.

**Given** a Tenants service instance is restarted
**When** it rebuilds aggregate or projection state
**Then** state is reconstructed from EventStore events and snapshots
**And** no in-process state is required for correctness between requests.

**Given** multiple Tenants service instances run
**When** commands and queries are routed across instances
**Then** correctness depends on EventStore/DAPR state and actor semantics
**And** no instance-local tenant state causes inconsistent behavior.

**Given** snapshot configuration is reviewed
**When** the `tenants` domain is configured
**Then** the tenant snapshot interval is set to the documented 50-event interval
**And** global administrator singleton state uses the EventStore default unless evidence requires otherwise.

**Given** startup reconstruction performance tests run with the target scale data set
**When** 1,000 tenants with an assumed average of 500 events each are seeded
**Then** ready-state reconstruction completes within the 30-second target or reports a documented failure
**And** the 500,000-event benchmark is classified as scheduled performance evidence, while ordinary readiness and health checks remain in the implementation lane.

### Story 7.6A: Validate Production Auth Smoke Tests

As a platform operator,
I want production authentication smoke tests,
So that valid and invalid identity provider configuration is proven before users depend on the service.

**Acceptance Criteria:**

**Given** production-like smoke tests run
**When** valid and invalid tokens are used against protected tenant command and query endpoints
**Then** valid tokens succeed only within their allowed scope
**And** invalid or misconfigured tokens fail safely.

**Given** production auth smoke-test evidence is captured
**When** results are reviewed
**Then** issuer, audience, `eventstore:tenant`, HTTPS metadata, signing/authority source, and development-token separation are documented
**And** evidence does not expose token material, secrets, or PII.

### Story 7.6B: Validate DAPR Component and Service Invocation Smoke Tests

As a platform operator,
I want DAPR component and service invocation smoke tests,
So that tenant command processing can reach required EventStore and Tenants service paths safely.

**Acceptance Criteria:**

**Given** DAPR component smoke tests run
**When** actor, state store, pub/sub, placement, scheduler, and service invocation inputs are missing or misnamed
**Then** the failure identifies the missing deployment input or dependency
**And** it does not produce ambiguous runtime errors or leak secrets.

**Given** the domain processor route is smoke-tested
**When** EventStore aggregate actors invoke Tenants domain processing
**Then** the required service invocation path succeeds only through approved DAPR configuration
**And** deny-by-default service invocation assumptions are preserved.

### Story 7.6C: Validate Health and Dependency Readiness Smoke Tests

As a platform operator,
I want health and dependency readiness smoke tests,
So that the service does not report ready before required infrastructure is usable.

**Acceptance Criteria:**

**Given** health or dependency checks fail
**When** smoke tests run
**Then** the failure identifies the missing deployment input or dependency
**And** it does not produce ambiguous runtime errors or leak secrets.

**Given** health and readiness smoke-test evidence is captured
**When** operators review deployment readiness
**Then** readiness covers required DAPR/EventStore dependencies and tenant command/query paths
**And** readiness does not claim success before required dependencies are usable.

### Story 7.6D: Validate Pub/Sub Recovery and Catch-Up Evidence

As a platform operator,
I want pub/sub recovery and catch-up evidence,
So that temporary publication failures do not imply tenant event loss.

**Acceptance Criteria:**

**Given** DAPR pub/sub is temporarily unavailable
**When** tenant commands are submitted and then pub/sub recovers
**Then** persisted events remain durable
**And** subscribers or projections can catch up according to documented recovery behavior.

**Given** recovery evidence is captured
**When** operators inspect logs, metrics, or documented replay output
**Then** event durability, recovery path, and catch-up result are visible with support-safe identifiers
**And** raw payloads, tokens, secrets, or PII are not exposed.

### Story 7.6E: Publish the Deployment Readiness Checklist and Evidence Template

As a platform operator,
I want a deployment readiness checklist and evidence template,
So that production readiness proof is repeatable across environments.

**Acceptance Criteria:**

**Given** a deployment readiness checklist is followed
**When** an operator verifies Tenants in an environment
**Then** the checklist covers issuer, audience, `eventstore:tenant`, HTTPS metadata, signing/authority source, DAPR components, service invocation, and health endpoints
**And** development token guidance is clearly separated from production IdP setup.

**Given** deployment readiness documentation and smoke tests are reviewed
**When** operators prepare a production deployment
**Then** required environment variables, IdP claim mappings, DAPR prerequisites, AppHost overrides, and verification commands are documented
**And** smoke-test evidence can be used as release or deployment readiness proof.

## Epic 8: Developers Can Adopt Through Documentation and Demo Evidence

Developers can follow a validated quickstart, understand event contracts, see the reactive access demo, and design for timing, idempotency, and compensating commands.

### Story 8.1: Create a Prerequisite-Validated Quickstart

As a developer evaluating Tenants,
I want a quickstart that validates prerequisites before the first command,
So that I can reach my first tenant command within 30 minutes without guessing at environment setup.

**Acceptance Criteria:**

**Given** a developer opens the quickstart
**When** they begin setup
**Then** the guide lists required .NET SDK, root-level submodule initialization, DAPR, EventStore, and local runtime prerequisites
**And** it explicitly avoids recursive submodule initialization.

**Given** the developer follows prerequisite validation
**When** DAPR, EventStore, AppHost, or authentication prerequisites are missing
**Then** the guide explains how to detect and fix the missing prerequisite
**And** failures are identified before the first tenant command is submitted.

**Given** prerequisites are satisfied
**When** the developer follows the quickstart path
**Then** they can restore, build, start the required local topology, and submit a first tenant command within the target 30-minute journey
**And** the command path uses the documented EventStore command submission route.

**Given** the first command succeeds or rejects
**When** the developer inspects the outcome
**Then** the guide explains how to identify success, structured rejection, and next corrective action
**And** it does not require reading raw logs as the primary success signal.

**Given** quickstart validation is tested
**When** a reviewer follows the guide on a prepared environment
**Then** commands, paths, package names, and expected outputs are current
**And** any local-only assumptions are clearly labeled.

### Story 8.2: Publish the Event Contract Reference

As a consuming service developer,
I want a complete event contract reference,
So that I can subscribe to the right tenant events and handle their schemas safely.

**Acceptance Criteria:**

**Given** the contract reference is opened
**When** a developer reviews tenant commands, events, and rejections
**Then** the reference lists every public command, event, query, and rejection contract
**And** it identifies the owning package and intended consumer for each contract.

**Given** an event contract is documented
**When** a developer reads its schema
**Then** required fields, optional fields, timestamp fields, tenant identity fields, event ID, aggregate version, and serialization shape are documented
**And** every tenant event identifies the top-level managed `TenantId` requirement.

**Given** a rejection contract is documented
**When** a developer reads the reference
**Then** rejection payload fields are described as structured data
**And** the reference does not encourage consumers to depend on persisted English prose.

**Given** CloudEvents publication is documented
**When** a consumer subscribes through DAPR
**Then** the reference identifies the topic `tenants.events`, event type filtering guidance, and at-least-once delivery assumptions
**And** it does not imply cross-service ordering guarantees.

**Given** contract documentation validation runs
**When** public contract types are added, removed, or renamed
**Then** validation detects stale reference content or missing entries
**And** the docs are updated before the story can be considered complete.

### Story 8.3: Document the Sample Consuming Service Walkthrough

As a developer adopting Tenants,
I want a guided walkthrough of the sample consuming service,
So that I can copy the event subscription and access-enforcement pattern into my own service.

**Acceptance Criteria:**

**Given** the sample service from the integration epic is available
**When** a developer reads the walkthrough
**Then** it explains package references, DI registration, tenant event subscription, local projection updates, and access-enforcement behavior
**And** it points to the exact sample files or snippets that implement each step.

**Given** the sample registers tenant event handlers
**When** the walkthrough describes setup
**Then** it demonstrates the under-20-lines event-handler registration target
**And** it distinguishes reusable package setup from sample-only code.

**Given** the sample processes access events
**When** the walkthrough explains behavior
**Then** it shows how user add, remove, role-change, tenant disable, tenant enable, and configuration events update consumer state
**And** it explains eventual consistency rather than presenting the local projection as synchronous truth.

**Given** the developer wants to adapt the sample
**When** they follow the walkthrough
**Then** the guide identifies which code is safe to copy, which pieces are application-specific, and which identifiers or secrets must be supplied by the deployment
**And** it avoids exposing raw tokens or sensitive tenant/user data.

**Given** sample walkthrough validation runs
**When** snippets are compiled or checked against the sample
**Then** the documented code remains synchronized with the sample implementation
**And** broken snippets fail documentation validation.

### Story 8.4: Produce the Reactive Access "Aha Moment" Demo

As a developer evaluating Tenants,
I want a concise demo that shows access revocation propagating through subscribing services,
So that I can understand the event-driven value without reading the full architecture.

**Acceptance Criteria:**

**Given** the demo starts from a clean or documented local setup
**When** a tenant is created and a user is added with a tenant role
**Then** the demo shows subscribing services receiving the add-user event
**And** each service updates its local access state from the tenant event stream.

**Given** the user is removed from the tenant
**When** the remove event is published
**Then** the demo shows all subscribing services revoking or denying local access based on their projections
**And** no custom polling or manual synchronization job is used.

**Given** the demo presents the event history
**When** the viewer inspects the result
**Then** it shows an audit trail of who acted, what changed, and when
**And** it avoids exposing raw payloads, tokens, secrets, or sensitive user data.

**Given** demo narration or written steps explain the behavior
**When** the viewer follows along
**Then** the demo makes clear that subscribers are eventually consistent
**And** it references the planned synchronous authorization plugin only as a future option where appropriate.

**Given** demo assets are reviewed
**When** they are included in docs or README
**Then** the asset length, commands, package names, and visual output support the 90-second proof goal
**And** stale or misleading demo steps are flagged for update.

### Story 8.5: Document Cross-Aggregate Timing and Eventual Consistency

As a developer integrating tenant events,
I want documentation for timing windows and eventual consistency,
So that I can design services that behave correctly while projections catch up.

**Acceptance Criteria:**

**Given** a developer reads the timing documentation
**When** tenant commands, event persistence, pub/sub publication, subscriber processing, and local projections are described
**Then** the document explains the event propagation window clearly
**And** it identifies which state is authoritative at each stage.

**Given** the documentation includes a sequence diagram
**When** the developer follows the command-to-subscriber flow
**Then** the diagram shows command submission, aggregate handling, event storage, publication, subscriber processing, and projection update
**And** it does not imply synchronous subscriber enforcement.

**Given** a consumer service needs security-critical enforcement
**When** the developer reviews guidance
**Then** the document explains how to design for eventual consistency
**And** it references planned synchronous authorization plugin behavior as an optional future enforcement path.

**Given** projection lag or subscriber delay occurs
**When** the documentation describes user-visible behavior
**Then** it provides practical guidance for stale data, retries, local projection rebuilds, and support-safe diagnostics
**And** it avoids advising `Thread.Sleep` or fixed-delay waits as correctness mechanisms.

**Given** timing documentation is validated
**When** architecture or event-flow implementation changes
**Then** diagrams and text are checked for drift
**And** stale timing claims are corrected before release.

### Story 8.6: Document Compensating Command Patterns

As a developer or operator,
I want clear compensating command guidance,
So that incorrect tenant access changes are corrected explicitly and auditably.

**Acceptance Criteria:**

**Given** a user is removed from a tenant by mistake
**When** a developer reads the compensating-command documentation
**Then** the guide explains that recovery is a new explicit command, such as `AddUserToTenant` with a specified role
**And** it does not describe recovery as hidden undo.

**Given** a compensating action is documented
**When** the guide walks through an example
**Then** it explains why the intended role must be provided explicitly
**And** it does not imply the system automatically restores historical roles without a new command.

**Given** compensating command guidance discusses auditability
**When** a correction is made
**Then** the original event remains in history
**And** the correction produces its own command outcome and audit event.

**Given** common correction scenarios are documented
**When** developers review the guide
**Then** it covers mistaken user removal, wrong role assignment, configuration mistake, and tenant lifecycle correction where applicable
**And** each example identifies the safe command path and expected rejection cases.

**Given** compensating-command docs are validated
**When** command names, role names, or rejection behavior changes
**Then** examples are checked against current contracts
**And** stale command snippets or misleading recovery language are corrected.

## Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely

**Readiness status:** readiness/planning-only. This epic produces Phase 2 UI dependency maps, specifications, and acceptance-evidence requirements. It is not a shippable Admin UI implementation epic and must not be routed to Developer agents as product delivery work until separate implementation stories are created.

**Routing rule:** Product implementation must not create Developer-agent story files directly from Epic 9 stories. Before Phase 2 UI implementation starts, Product/UX/Architecture must convert these planning outputs into implementation stories with explicit source projections, FrontComposer/Fluent UI dependencies, adapter architecture, command lifecycle behavior, accessibility/localization evidence, and test artifacts.

Phase 2 Admin UI readiness is sequenced around operational access review, truth-state feedback, consequence preview, command lifecycle, audit evidence, accessibility, localization, and FrontComposer dependencies.

### Story 9.1: Map Fluent UI and FrontComposer Dependencies for Tenant Admin Screens

As a product owner planning Phase 2 Admin UI work,
I want each tenant admin screen mapped to required Fluent UI and FrontComposer capabilities,
So that UI implementation starts only when component dependencies and fallbacks are explicit.

**Acceptance Criteria:**

**Given** the UX design requirements are reviewed
**When** dependency mapping is created
**Then** each planned surface maps to Fluent UI Blazor v5 and FrontComposer capabilities
**And** exact Fluent UI API verification against the pinned package is recorded as an implementation prerequisite.

**Given** standard read-only surfaces are planned
**When** tenant list, tenant detail, member table, configuration read-only view, user lookup, global administrator list, and audit fallback are mapped
**Then** each screen identifies whether FrontComposer-generated composition is appropriate
**And** generated composition is limited to low-risk source-of-truth projection surfaces.

**Given** high-risk workflows are planned
**When** remove user, change role, disable or enable tenant, remove global administrator, high-impact configuration changes, command lifecycle feedback, consequence preview, audit evidence, and degraded-state recovery are mapped
**Then** each workflow identifies required custom components or overrides
**And** immutable Tenants domain contracts are not reshaped or annotated for UI generation.

**Given** a dependency is missing or unproven
**When** the dependency map is reviewed
**Then** the screen or workflow is marked blocked or assigned an explicitly approved fallback
**And** the owning project, dependency artifact, readiness status, and `blockedBy` reference are recorded.

**Given** the dependency map is complete
**When** Phase 2 UI stories are later drafted
**Then** each story can reference the mapped component, hook, token, layout, accessibility, localization, and documentation prerequisites
**And** backend MVP stories remain unblocked by UI dependency readiness.

### Story 9.2: Specify the Operations Shell and Read-Only Access Review Surfaces

As a tenant administrator,
I want the Phase 2 UI information architecture to support tenant discovery and access review,
So that I can find tenants, inspect access, and reach audit evidence before command workflows are enabled.

**Acceptance Criteria:**

**Given** the Operations Shell is specified
**When** primary navigation is defined
**Then** Tenants, Users, Global Administrators, and Audit are included as primary navigation areas
**And** command lifecycle is not promoted into a separate primary navigation model.

**Given** the tenant list is specified
**When** table behavior is documented
**Then** it includes filter, search, sort, pagination, tenant status, member count, owner count, freshness, pending state, loading, empty, filtered-empty, error, stale, and degraded states
**And** sorting and pagination do not hide pending or stale-state indicators.

**Given** tenant detail and member access review are specified
**When** a user navigates from tenant list to detail
**Then** tenant context is preserved across overview, members, configuration, command state, and audit evidence
**And** selected tenant and filters are preserved when returning to the list.

**Given** user lookup and global administrator surfaces are specified
**When** access questions begin with a user or platform role
**Then** user lookup remains reachable from shell navigation and access-review contexts
**And** global administrator surfaces distinguish platform-level risk from ordinary tenant membership.

**Given** read-only UI implementation stories are drafted
**When** the read-only specification is used
**Then** long tenant IDs, user IDs, and support-safe references remain visually truncated but accessible
**And** stable selectors or component contracts are required for automation instead of arbitrary row text.

### Story 9.3: Define Truth State, Freshness, and Unavailable Action Patterns

As a tenant admin UI user,
I want the interface to distinguish current, stale, pending, and blocked states,
So that I know what is true, what is delayed, and why an action is unavailable.

**Acceptance Criteria:**

**Given** the truth-state vocabulary is defined
**When** UI implementation stories use status indicators
**Then** Truth State Badge states include current, refreshing, aging, stale, unknown, eligible, blocked, pending, accepted, confirmed, failed, audit pending, and audit available
**And** every state has a text label, accessible name, and non-color-only visual treatment.

**Given** freshness gating is specified
**When** an access-impacting action is considered
**Then** the UI must show freshness label, timestamp or version marker, refresh action, and blocking reason
**And** unknown freshness fails closed for destructive actions.

**Given** unavailable actions are specified
**When** a high-impact action is disabled or blocked
**Then** the UI exposes a visible inline reason for missing permission, stale data, missing lifecycle support, missing consequence preview, missing audit proof, or high-impact flow readiness gaps
**And** tooltips may supplement but cannot be the only explanation.

**Given** feedback states are specified
**When** command, projection, or audit state changes
**Then** the UI distinguishes request sent, accepted, projection pending, confirmed, rejected, already applied, degraded, audit pending, audit available, and unable to verify
**And** accepted, projected, and proven states are not collapsed into one success state.

**Given** page-level degradation occurs
**When** feedback is displayed
**Then** feedback appears close to the affected tenant, row, command panel, or audit context where possible
**And** global message bars are reserved for page-level degradation or system-wide service state.

### Story 9.4: Specify the RemoveUserFromTenant Command-Capable Journey

As a tenant owner or global administrator,
I want the first command-capable UI journey to remove user access safely,
So that access changes are previewed, submitted, reconciled, and proven without false success.

**Acceptance Criteria:**

**Given** the first command-capable UI slice is planned
**When** command workflow scope is selected
**Then** `RemoveUserFromTenant` is the first command-capable journey
**And** it is launched from a specific tenant membership row with tenant, user, role, freshness, and authority context visible.

**Given** consequence preview is specified
**When** remove-user action is prepared
**Then** preview content includes tenant, target user, current role, owner count, affected access path, freshness, recovery path, audit expectation, known consequences, and known unknowns
**And** incomplete consequence inputs block submit unless product and UX approve a named fallback.

**Given** high-risk access cases are specified
**When** last-owner removal, global administrator removal, or tenant-wide impact is detected
**Then** elevated friction, affected scope, evidence freshness, audit consequence, and intentional confirmation are required
**And** destructive actions do not appear as casual primary actions.

**Given** command submission is specified
**When** the user confirms removal
**Then** the UI records local pending or confirming hints without replacing confirmed projection truth
**And** required fields are validated before command preview or submit.

**Given** command reconciliation is specified
**When** the backend rejects, accepts, reports already applied, delays projection, or cannot be verified
**Then** the UI preserves context, maps domain rejections to safe localized text, and offers retry, status review, inspect audit, continue read-only, or escalation paths
**And** raw command payloads, stack traces, tokens, and internal exception text are not exposed.

### Story 9.5: Specify Audit Evidence and Compensating Recovery UI Patterns

As a tenant auditor or operator,
I want audit evidence and recovery patterns to be explicit in the UI plan,
So that access changes can be proven later and corrections are handled as auditable commands.

**Acceptance Criteria:**

**Given** audit entry points are specified
**When** users navigate from global navigation, tenant rows, tenant detail, user lookup, or command results
**Then** each path can lead to audit context
**And** missing audit capability is represented as a documented fallback or blocked dependency.

**Given** an audit timeline dependency is unavailable
**When** the first UI slice still needs audit visibility
**Then** the approved fallback is a flat audit DataGrid with stable ordering, filters, loading, empty, error, and accessible expansion states
**And** reusable timeline dependency status remains visible in the dependency map.

**Given** Audit Evidence Receipt is specified
**When** meaningful access changes complete or partially complete
**Then** the receipt includes actor, target, tenant scope, outcome, timestamp, projection marker, audit reference, and support-safe command reference where available
**And** copyable references do not expose raw payloads, bearer tokens, stack traces, or sensitive internals.

**Given** audit proof is delayed or unavailable
**When** the UI communicates outcome
**Then** it distinguishes audit pending, audit delayed, audit unavailable, and missing implementation support
**And** it avoids success language when proof cannot be verified.

**Given** recovery guidance is specified
**When** a wrong access change is discovered
**Then** UI language uses explicit compensating-command terms such as start correction or restore intended access
**And** recovery is never labeled as hidden undo.

### Story 9.6: Specify Responsive Operational Layout and Visual System Usage

As a UI implementer,
I want responsive and visual-system rules for operational screens,
So that dense access-review workflows remain usable without sacrificing truth or context.

**Acceptance Criteria:**

**Given** visual system usage is specified
**When** tenant-specific meaning is mapped to UI treatment
**Then** tenant status, projection freshness, command lifecycle, authorization state, audit evidence, and risk use semantic roles instead of hard-coded colors
**And** every state remains understandable without color alone.

**Given** typography and layout rules are specified
**When** operational screens are implemented
**Then** they use professional, calm, precise system typography, compact density, modest hierarchy, and plain-language status labels
**And** decorative card grids or hero-scale type are avoided for dense operational workflows.

**Given** command and status layout is specified
**When** toolbars, status chips, action cells, and lifecycle panels render
**Then** stable dimensions prevent layout shift
**And** command controls remain close to the affected tenant, user, role, or audit context.

**Given** responsive behavior is specified
**When** screens render at desktop, tablet, and mobile widths
**Then** desktop remains the primary workstation layout, tablet may collapse navigation and stack regions, and mobile is limited to read-only triage, lookup, and audit reference review
**And** high-impact access changes fail closed or become unavailable when full safety context cannot be preserved.

**Given** breakpoint guidance is documented
**When** implementation stories define layout tests
**Then** mobile 320-767px, tablet 768-1023px, desktop 1024px and above, and wide desktop 1440px and above are covered
**And** DataGrid horizontal scroll or column priority preserves critical state instead of hiding it.

### Story 9.7: Define Accessibility, Localization, and UI Acceptance Evidence

As a product owner planning UI implementation,
I want accessibility, localization, and responsive evidence requirements defined before UI stories start,
So that Phase 2 work cannot ship without proving the operational trust surface is usable.

**Implementation split directive:** If these outputs become implementation backlog later, split this story into focused proof targets: 9.7A keyboard, focus, and modal accessibility evidence; 9.7B screen reader, live region, and status accessibility evidence; 9.7C localization and message composition evidence; 9.7D reduced motion, forced colors, contrast, and visual accessibility evidence; and 9.7E responsive layout and scenario evidence matrix.

**Acceptance Criteria:**

**Given** Phase 2 UI accessibility baseline is defined
**When** UI stories are drafted
**Then** WCAG 2.1 AA is the baseline and WCAG 2.2 AA is the target where supported
**And** Operations Shell, tenant list, member table, command preview, command lifecycle feedback, and audit evidence surfaces are in scope.

**Given** keyboard and focus behavior is specified
**When** modal, preview, table, and command workflows are implemented
**Then** all interactive elements are keyboard reachable, focus order follows task order, focus indicators work in forced-colors mode, dialogs trap focus when modal, escape behavior is safe, and focus returns to the launching row or action.

**Given** screen reader and status behavior is specified
**When** state labels, timestamps, status badges, row actions, and lifecycle changes are implemented
**Then** accessible names, exact timestamp labels, table headers, row relationships, sort state, and live-region announcements are required
**And** assertive announcements are reserved for rejection, failure, destructive blockers, or unable-to-verify states.

**Given** localization responsibility is specified
**When** UI text is implemented
**Then** state labels, role names, timestamps, warnings, disabled reasons, recovery actions, and confirmation copy are localizable
**And** confirmation messages do not rely on concatenated sentence fragments.

**Given** reduced motion and visual accessibility are specified
**When** lifecycle and state transitions are implemented
**Then** reduced-motion users do not depend on animation to understand progression
**And** color contrast, forced-colors, and high-contrast behavior are verified.

**Given** UI acceptance evidence is planned
**When** implementation stories are marked ready
**Then** required evidence includes desktop 1024px, 1366px, 1440px, wide layouts, tablet 768px and 1024px, mobile 375px and 430px, keyboard-only navigation, NVDA or approved screen-reader review, automated accessibility checks, live-region checks, focus return, and disabled explanations without mouse hover
**And** acceptance scenarios include stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, and permission-missing cases.
