---
stepsCompleted:
  - step-01-init
  - step-02-discovery
  - step-02b-vision
  - step-02c-executive-summary
  - step-03-success
  - step-01b-continue
  - step-04-journeys
  - step-05-domain
  - step-06-innovation
  - step-07-project-type
  - step-08-scoping
  - step-09-functional
  - step-10-nonfunctional
  - step-11-polish
  - step-12-complete
inputDocuments:
  - product-brief-Hexalith.Tenants-2026-03-06.md
documentCounts:
  briefs: 1
  research: 0
  brainstorming: 0
  projectDocs: 0
classification:
  projectType: developer_tool
  domain: general
  complexity: medium-high
  projectContext: greenfield
workflowType: 'prd'
---

# Product Requirements Document - Hexalith.Tenants

**Author:** Jerome
**Date:** 2026-03-06

## Executive Summary

Hexalith.Tenants is a standalone, event-sourced microservice that provides multi-tenant management for applications built on Hexalith.EventStore. Its core value: when a user is added to or removed from a tenant, every subscribing service enforces the change automatically through DAPR pub/sub event subscriptions — no polling, no sync jobs, no per-service integration code. Developers add the NuGet packages with minimal DI registration and get tenant lifecycle management, tenant-scoped RBAC, and tenant configuration that propagates reactively across all subscribing services.

The vision extends beyond event-driven integration: a planned EventStore authorization plugin will reject commands from unauthorized users — those without TenantOwner, TenantContributor, or GlobalAdministrator roles — at the pipeline level, before they reach any domain service. Combined with in-memory test fakes via `Hexalith.Tenants.Testing`, the result is tenant management adoptable with minimal DI registration, transparent once running, and testable without live infrastructure.

### What Makes This Special

Existing approaches to multi-tenancy in .NET — custom EF Core tenant filters, CRUD-based tenant tables, or identity provider extensions — treat tenant data as static state. They cannot produce domain events, cannot answer temporal queries ("who had access last Tuesday?"), and cannot trigger reactive behavior across services. Hexalith.Tenants makes tenant-user-role relationships native to the event-sourced lifecycle. Every change is an immutable domain event that flows through the same CQRS/ES pipeline as all other Hexalith state — provably auditable through event history and operationally invisible to consuming services.

## Project Classification

- **Project Type:** Developer tool — NuGet packages and deployable microservice consumed by .NET developers
- **Domain:** Software infrastructure (domain-agnostic multi-tenant management)
- **Complexity:** Medium-high — architecturally demanding (event sourcing + CQRS + multi-tenancy + DAPR integration) but no external regulatory constraints
- **Project Context:** Greenfield — new standalone service building on established Hexalith.EventStore patterns

## Success Criteria

### User Success

- A developer can deploy Hexalith.Tenants and send a CreateTenant command within 30 minutes following the quickstart guide
- A consuming service can become fully tenant-aware — DI registration, event subscription, and access enforcement — in under 20 lines of code
- A developer can write a tenant integration test in under 10 lines using `Hexalith.Tenants.Testing` in-memory fakes, zero external infrastructure required
- The "aha moment": a user removal in one place automatically revokes access across all subscribing services without any custom integration code

### Business Success

- **Primary metric:** Number of Hexalith-based projects adopting Hexalith.Tenants, starting with Hexalith.Parties as the first consumer
- Elimination of duplicated effort — teams no longer spend 2-4 weeks per project building custom tenant management
- Community traction — GitHub stars, contributions, and discussions grow alongside EventStore adoption

### Technical Success

- Zero cross-tenant data leaks, verified by integration tests
- 100% branch coverage on tenant isolation and role authorization logic
- Event processing latency < 50ms for tenant commands (p95), measured via OpenTelemetry
- > 80% line coverage across all packages (coverlet.collector in CI)
- All packages build, pass tests, and publish to NuGet via CI/CD
- API availability target: 99.9% in production deployments

### Measurable Outcomes

| Outcome | Target | Measurement |
|---------|--------|-------------|
| Time-to-first-command | < 30 minutes | Quickstart guide validation |
| Integration code surface | < 20 lines total | Code review of sample consuming service |
| Test authoring surface | < 10 lines per test | Code review of testing package samples |
| Cross-tenant isolation | Zero leaks | Automated integration tests |
| Isolation/auth branch coverage | 100% | CI pipeline (coverlet) |
| Event latency (p95) | < 50ms | OpenTelemetry metrics |
| Test coverage (overall) | > 80% line coverage | CI pipeline (coverlet) |
| Project adoption | Hexalith.Parties as first consumer | Tracking adopting projects |

## Product Scope

### MVP Strategy & Philosophy

**MVP Approach:** Platform MVP — deliver the complete tenant management infrastructure that proves the event-sourced paradigm works end-to-end, from command to event to cross-service reaction.

The MVP must satisfy two validation goals simultaneously:
1. **Technical validation** via Hexalith.Parties as first consumer — proving the reactive tenant model works
2. **Adoption validation** via the developer experience — proving that external developers can adopt it within 30 minutes

**Resource Requirements:** Solo developer (Jerome) with CI/CD automation. The EventStore submodule provides the foundational infrastructure, so the effort focuses on the tenant domain model, packages, and documentation.

### MVP Feature Set (Phase 1)

Event contracts may evolve with breaking changes during pre-1.0 development. Event contract stability (zero breaking changes) is a v1.0 release milestone.

**Core User Journeys Supported:**
- Journey 1 (Alex — Evaluate & Adopt): Full quickstart path from NuGet install to first tenant command
- Journey 2 (Alex — Testing): In-memory fakes with production-parity domain logic
- Journey 3 (Alex — Multi-service): Event subscription across multiple consuming services
- Journey 4 (Alex — First Error): Actionable error messages, optimistic concurrency, clear failure behavior
- Journey 5 (Alex — Tenant Discovery): Read model queries for listing and discovering tenants
- Journey 6 (Priya — Deploy & Operate): DAPR deployment, OpenTelemetry, basic monitoring
- Journey 7 (Sofia — Security): Audit queries via standard read model, compensating command patterns

**Must-Have Capabilities:**

| Capability | Justification |
|---|---|
| Tenant aggregate (Create, Update, Disable, Enable) | Core domain — without this, nothing works |
| User-role management (Add, Remove, ChangeRole) with three roles (TenantOwner, TenantContributor, TenantReader) | Core value prop — reactive access management |
| Global administrator (Set, Remove) with cross-tenant access | Cross-tenant operations required for platform governance |
| Tenant configuration (Set, Remove) as schema-free key-value pairs with namespace conventions | Enables consuming services to react to per-tenant settings |
| Tenant read model (ListTenants, GetTenant, GetTenantUsers, GetUserTenants, audit queries by tenant/date range) | Required for tenant discovery, audit, and operational monitoring |
| Global administrator bootstrapping (seed command or configuration) | Required for first deployment — no authorized actors exist without it |
| 5 NuGet packages (Contracts, Client, Server, Testing, Aspire) | Distribution mechanism — Aspire included to match EventStore parity |
| Event-driven integration via DAPR pub/sub (CloudEvents 1.0) | Core innovation — reactive cross-service tenant management |
| Optimistic concurrency on aggregate commands | Data integrity — prevents duplicate users, race conditions |
| Actionable domain error messages | Developer experience — clear, specific rejection reasons |
| Quickstart guide with prerequisite validation | Adoption gate — < 30 minutes to first command |
| Event contract reference documentation | Developer reference — which events to subscribe to and their schemas |
| Sample consuming service | Proof of integration pattern — DI registration, event handlers |
| "Aha moment" demo (screencast/video) | Primary adoption tool — 90-second proof of the paradigm |
| CI/CD pipeline (build, test, pack, publish) | Quality gate — automated validation on every change |

### Post-MVP Features (Phase 2 — Growth)

Priority-ordered:

1. **EventStore tenant authorization plugin** — Pipeline-level command authorization using local projection of tenant-user-role mappings. Closes the cross-aggregate timing window identified in Journey 4. Deployment-level opt-in via `services.AddTenantAuthorization()`
2. **Keycloak JWT projection sync** — Synchronize tenant roles to JWT claims for token-based authorization
3. **Admin UI / dashboard** — Visual management interface for tenants, users, and configuration
4. **Custom/extensible roles** — Tenant-defined roles beyond Owner/Contributor/Reader
5. **Bulk tenant provisioning** — Batch import/migration tooling for onboarding at scale (Priya's Journey 6 gap)
6. **F# consumption support** — Validated F#-friendly API surface for contracts and client packages

### Explicitly Out of Scope for All Phases

- **Tenant deletion** — Tenants can be disabled but never deleted. Event history is immutable and must be preserved for audit integrity. A disabled tenant is the terminal state.
- **gRPC API surface** — The command API uses REST endpoints only. gRPC is not planned for any phase. (The Product Brief referenced "REST/gRPC" but gRPC was eliminated during PRD scoping as unnecessary complexity for the target audience.)

### Vision (Phase 3 — Expansion)

- Hierarchical sub-tenants (parent-child relationships)
- Multi-deployment tenant migration via event replay
- Per-tenant service registry (controlling which domain services are available per tenant)
- Cross-deployment tenant federation
- Event store snapshots for faster state reconstruction at scale

### Risk Mitigation Strategy

**Technical Risks:**

| Risk | Mitigation |
|---|---|
| EventStore aggregate pattern doesn't map cleanly to tenant domain | Tenant domain is structurally simple (CRUD-like commands with event outputs) — the aggregate pattern is well-suited. Counter sample in EventStore validates the pattern |
| Read model projection complexity for audit queries | Start with flat projections (list tenants, list users per tenant). Complex temporal queries can be built incrementally on the same event stream |
| DAPR pub/sub event ordering across services | Document as eventual consistency by design. Each service builds its own local projection. Auth plugin (Phase 2) closes the synchronous enforcement gap |
| Event sourcing complexity deters adoption | Testing package as low-risk evaluation entry point; quickstart guide abstracts ES complexity behind simple commands |
| Event contract evolution breaks consumers | Event contract stability as explicit v1.0 milestone; contract versioning documentation; CI contract tests |

**Market Risks:**

| Risk | Mitigation |
|---|---|
| EventStore ecosystem too small for meaningful adoption | Hexalith.Parties as first consumer validates the model. Package design allows standalone value even for single-service deployments |
| Tenants-as-domain-state paradigm isn't understood | Lead with the "aha moment" demo showing reactive revocation across services — visual proof beats written explanation |
| Competing approaches emerge | Network effect creates switching cost after adoption; event contract gravity makes competing schemas costly |
| Innovation is technically sound but market isn't ready | Target EventStore community events and .NET conferences; let the demo do the convincing |

**Resource Risks:**

| Risk | Mitigation |
|---|---|
| Solo developer bandwidth | EventStore submodule provides infrastructure foundation — tenant domain logic is the primary effort. Tiered test architecture allows incremental quality |
| Scope creep during development | MVP scope is locked to the must-have table above. Post-MVP features are explicitly deferred. Phase 2 starts only after MVP is validated via Hexalith.Parties adoption |

## User Journeys

### Journey 1: Alex Evaluates and Adopts Hexalith.Tenants

**Alex** is a .NET developer three weeks into a new SaaS project built on Hexalith.EventStore. He's just finished modeling his core domain — a party management system — and now faces the same wall he's hit twice before: tenant management. Last time, he spent three weeks building custom tenant tables, EF Core filters, and a hand-rolled access control layer that still had a cross-tenant data leak in production.

He finds Hexalith.Tenants in the EventStore documentation. Before committing, he scans the event contract reference — clean CloudEvents 1.0 schema, three roles, clear aggregate boundaries. Then he sees something that makes him pause: the `Hexalith.Tenants.Testing` package with in-memory fakes that use the same domain logic as the real service. He can evaluate the entire tenant model locally without deploying anything. That's what flips him from browsing to trying.

He follows the quickstart guide, which starts with a prerequisite check: DAPR sidecar running, EventStore deployed. His DAPR config is stale — the guide catches this with a validation step and points him to the DAPR setup docs. Fifteen minutes later, prerequisites are green.

Twenty minutes after that, he sends his first `CreateTenant` command and watches the `TenantCreated` event appear in his event stream. He adds a user, assigns the TenantContributor role, and sees `UserAddedToTenant` flow through the same pipeline as his domain events.

The real moment comes when he wires up his Parties service to subscribe to tenant events. He removes a user from the tenant — and his Parties service automatically revokes that user's access. No webhook. No polling. No custom integration code. Just a DAPR pub/sub subscription and a handful of lines in DI registration. He stares at the screen for a moment, then deletes 400 lines of custom tenant code from his previous project's clipboard.

The next morning, he pulls his senior dev colleague into a screen share. "Watch this," he says, and removes a user from the tenant. The colleague watches the Parties service log the access revocation in real time. "Wait — that just... works? No integration code?" Alex grins. By lunch, the team has agreed to adopt Hexalith.Tenants as standard infrastructure for all new projects.

**What this reveals:** Event contract documentation quality, testing package as evaluation hook, prerequisite validation in quickstart, DAPR setup guidance, NuGet package discoverability, DI registration simplicity, event subscription developer experience, time-to-first-command metric, peer validation as adoption accelerator.

### Journey 2: Alex Tests Tenant Logic Without Infrastructure

It's 4 PM on Friday. **Alex** has a PR that needs tests before Monday's code review. Last project, setting up the test infrastructure — Docker Compose with a database, message broker, and test tenant service — took longer than writing the tests themselves. That 15-minute setup broke every time someone updated a container image, and his team lead has zero patience for "works on my machine" excuses.

He adds `Hexalith.Tenants.Testing` to his test project. In seven lines of code, he has an in-memory fake tenant service that responds to commands and publishes events. Crucially, the fakes use the same domain logic as the real service — so test behavior matches production behavior by design, not by coincidence. This isn't a mock that might drift from reality; it's the real aggregate running in memory.

He writes a test: create a tenant, add a user, verify the user can execute a party command, remove the user, verify the command is rejected. He adds a tenant isolation verification test: two tenants, two users, confirm that projections for tenant A never contain data from tenant B. The test runs in 200ms with zero external dependencies.

He pushes to CI. The tests pass on the first try — no Docker, no flaky infrastructure. He submits the PR at 4:45 PM and closes his laptop. He realizes he can now TDD his tenant integration logic the same way he TDDs his domain logic — including projection-level isolation guarantees.

**What this reveals:** Testing package API surface, in-memory fake fidelity and architectural guarantee (same domain logic), projection-level tenant isolation testing, CI/CD compatibility, test authoring simplicity (< 10 lines target), developer confidence through reliable test infrastructure.

### Journey 3: Alex Integrates Tenant Events Across Services

**Alex's** Parties service is working, but now his team is building a second service — Billing — that also needs tenant awareness. He dreads the integration work, but then remembers: the tenant events are already flowing through DAPR pub/sub.

He adds `Hexalith.Tenants.Contracts` and `Hexalith.Tenants.Client` to the Billing service. He consults the event contract reference to identify exactly which events Billing needs: `UserAddedToTenant`, `UserRemovedFromTenant`, `TenantDisabled`, and `TenantConfigurationSet`. He registers the tenant event handlers in DI — under 20 lines total.

When a tenant's billing plan changes via `SetTenantConfiguration` (namespaced as `billing.plan`), the Billing service picks it up automatically. During testing, he notices Billing processes a `UserRemovedFromTenant` event a few hundred milliseconds before Parties does. He checks the documentation and confirms: this is eventual consistency by design. Each service processes events independently and builds its own local projection from the tenant event stream. DAPR pub/sub does not guarantee cross-topic ordering, so each service must handle its projection gracefully without assuming events arrive in the same order as another service's projection. The docs explain that for security-critical scenarios, the planned EventStore authorization plugin will provide synchronous enforcement at the command pipeline level.

A month later, his team launches a third service — Reporting — and it takes under an hour to make it tenant-aware using the same Contracts and Client packages. Alex realizes something his architect colleague later confirms: each new service subscribing to the tenant event stream deepens the organization's investment in the shared tenant model. The tenant service has become the nervous system connecting all their domain services. Switching to a different approach would mean rewiring every service simultaneously — which no one wants to do because the current model just works.

**What this reveals:** Event contract reference as developer tool, contracts package reusability, multi-service event consumption patterns, per-service local projection model, event ordering reality (no cross-topic guarantees), eventual consistency documentation, configuration event utility, integration code surface (< 20 lines target), network effect and organizational lock-in through shared event stream.

### Journey 4: Alex Hits His First Error

**Alex** is writing a new integration and sends an `AddUserToTenant` command with a tenant ID that doesn't exist yet. He braces himself — in his last project, a wrong ID produced a wall of stack traces, a generic 500 error, and twenty minutes of digging through logs to find the actual problem.

Instead, he gets a clear domain rejection: `TenantNotFoundRejection: Tenant 'acme-test' does not exist. Ensure CreateTenant has been processed before adding users.` One sentence. Actionable. He exhales.

He fixes the command ordering. Next, he tries to add a user who's already in the tenant. Another clear response: `UserAlreadyInTenantRejection` with the existing role information. He tries two concurrent `AddUserToTenant` commands for the same user from different test threads — the aggregate's optimistic concurrency catches the conflict and rejects the second with a clear concurrency error rather than creating a duplicate. He realizes the aggregate enforces all invariants — duplicate additions, invalid role transitions, operations on disabled tenants, concurrent modifications — and every rejection is a specific, actionable message.

He tests a cross-aggregate timing scenario: what happens if `DisableTenant` is processed on the Tenant aggregate while his Parties service is processing a command that depends on the tenant being active? The Tenant aggregate rejects any further tenant commands immediately. For commands on other aggregates (like Parties), the behavior depends on whether the service has processed the `TenantDisabled` event yet. During the brief window before the event is processed, commands may still succeed against the Parties aggregate. The planned EventStore authorization plugin will close this gap with synchronous pipeline-level checks using a local projection. For MVP, the documentation makes this timing window explicit so developers can design accordingly.

He tests what happens when he sends a command while DAPR pub/sub is temporarily down. The command succeeds — the event is stored in the event store — but his subscribing Parties service doesn't receive the event until pub/sub recovers. He confirms: commands and event storage are synchronous and reliable; event distribution to subscribers is eventually consistent. The event store is the source of truth, not the pub/sub channel.

**What this reveals:** Error message quality and actionability, domain validation model, aggregate invariant enforcement, optimistic concurrency for concurrent operations, cross-aggregate timing behavior and documentation requirements, command vs. event delivery guarantees, behavior during infrastructure partial failures.

### Journey 5: Alex Discovers Existing Tenants

**Alex's** colleague **Dana** joins the team two weeks after the initial setup. She needs to build a feature for the Acme Corp tenant but doesn't know the tenant ID. She can't just create a new one — she needs to find the existing tenant.

She queries the tenant read model — a standard projection that the tenant service maintains — and gets back a list of tenants with their IDs, names, and statuses. She finds "acme-corp," checks its current users and roles, and confirms she has TenantContributor access. She's productive within minutes of joining the project.

This same query capability is what powers Sofia's audit dashboards and Priya's operational monitoring. The tenant read model isn't an afterthought — it's the foundation for every non-command interaction with the tenant system.

**What this reveals:** Tenant discovery and query capabilities (ListTenants, GetTenant, GetTenantUsers), read model as shared foundation for developers/operators/admins, onboarding experience for team members joining mid-project.

### Journey 6: Priya Deploys and Operates the Tenant Platform

**Priya** is the platform engineer responsible for deploying Alex's application stack. She's used to each app having its own tenant storage — a mess of scattered databases and inconsistent schemas. When Alex tells her there's a shared tenant microservice, she's cautiously optimistic.

She deploys Hexalith.Tenants alongside the EventStore using the team's standard DAPR configuration. The service slots into the existing infrastructure — same message broker, same state store, same observability pipeline. Because DAPR abstracts the infrastructure, she knows she can swap providers (Redis to Kafka, CosmosDB to PostgreSQL) without changing tenant service code.

Her first "aha" moment comes during a routine update. She redeploys the tenant service with a new version — and when it starts, all tenant state is already there. No migration script, no data seeding. The service is stateless between requests; all state lives in the event store and is rebuilt on startup. She can scale horizontally, restart without data loss, and blue-green deploy without downtime. This is fundamentally different from the stateful services she's used to operating.

She configures OpenTelemetry and sees tenant command latency metrics flowing into Grafana immediately. She sets up three alert categories: command latency (p95 > 50ms), event processing lag (subscriber behind by > 100 events), and — critically — tenant isolation anomalies (any query or projection returning data from a non-matching tenant ID).

A week later, a new enterprise customer signs up and needs 50 tenants provisioned. Priya scripts the `CreateTenant` commands in a batch. It works, but she notes that at organizational scale, a dedicated bulk provisioning capability would save operational time. This is a known post-MVP enhancement — for now, scripted individual commands handle it.

A latency alert fires. She traces the issue to a specific tenant whose bulk user import is saturating the message broker partition. She rebalances the partitions and latency drops back.

Three months in, a second team asks Priya to set up Hexalith.Tenants for their project. She does it in 20 minutes — DAPR config, EventStore connection, OpenTelemetry pipeline, done. The first time took her half a day with discovery and learning. Now it's muscle memory. She's become the go-to person for tenant infrastructure across the organization, and the consistency across projects means she troubleshoots one system, not five different ones.

**What this reveals:** Deployment simplicity, DAPR infrastructure portability, stateless service architecture (horizontal scaling, blue-green deploy, restart resilience), OpenTelemetry integration, tenant isolation monitoring (not just testing), latency SLA enforcement, per-tenant observability, bulk provisioning as explicit post-MVP enhancement, platform engineer mastery and repeat deployment confidence.

### Journey 7: Sofia Manages Tenant Security — Reactive and Proactive

**Sofia** is the global administrator. Her work has two modes: proactive governance and incident response.

**Proactive mode:** Every Monday morning, Sofia opens the tenant access summary — a standard read model projection maintained by the tenant service. She used to dread this review when it meant logging into five different apps and cross-referencing spreadsheets. Now it takes ten minutes. She scans for anomalies: users with TenantOwner in more than five tenants, tenants where all owners are inactive, configuration changes made outside business hours.

This Monday, something catches her eye. A user she doesn't recognize — "ext-vendor-9" — was granted TenantOwner on the "healthcare-prod" tenant at 2 AM Saturday. The event stream shows exactly who granted it: an intern's service account that shouldn't have had permission to assign TenantOwner. Sofia immediately downgrades the vendor to TenantReader, revokes the intern's account's ability to assign roles, and files an incident report. Without the event-sourced audit trail, this unauthorized privilege escalation could have gone unnoticed for weeks. No other multi-tenant solution she's used could have surfaced this — CRUD-based systems only show current state, not who changed what and when.

**Incident response:** She receives a report that a contractor's credentials may have been compromised. She issues a `RemoveUserFromTenant` command for the contractor on the affected Acme Corp tenant. Within seconds, every subscribing service — Parties, Billing, Reporting — receives the `UserRemovedFromTenant` event and revokes the contractor's access. But what about commands the contractor sent in the last few seconds before the tenant was secured? Any command submitted after the `RemoveUserFromTenant` event was processed by the Tenant aggregate would be rejected by the aggregate's invariant check. For the brief window between event storage and subscriber processing on other aggregates, the planned EventStore authorization plugin will close this gap with synchronous pipeline-level enforcement. For MVP, this window is documented and understood.

Her manager asks for a compliance report: all access changes in the last quarter for Acme Corp. Sofia queries the tenant read model projection by tenant ID and date range, producing a complete, immutable audit trail — every `UserAddedToTenant`, `UserRoleChanged`, and `UserRemovedFromTenant` event with timestamps and actor IDs. This is a standard capability of the tenant service's read model — no custom development needed for basic audit queries.

Then her stomach drops. She realizes she removed the wrong contractor — "jdoe-contractor" instead of "jdoe-consulting." The compromised account still has access. She acts fast: issues `RemoveUserFromTenant` for the correct account, then `AddUserToTenant` to restore the wrongly removed contractor. She restores the contractor with TenantContributor — the role they had at the time of removal, which she confirms from the event history. She notes: if another role change had occurred between the removal and the restoration, the compensating command would set the role she specifies, not automatically restore the previous role. Event sourcing gives her the information to make the right call, but the decision is hers.

The event history preserves full auditability: the mistake, the correction, the timestamps of both. When the auditor asks about the blip, the evidence explains itself.

**What this reveals:** Proactive audit and governance via standard read model projections, anomaly detection that catches real security issues, event-sourced audit trail as competitive differentiator (what CRUD systems cannot do), cross-service access revocation via events, in-flight command race condition awareness and documentation, global administrator capabilities, temporal audit queries, compensating command behavior (explicit role specification, not automatic state restoration), human error recovery patterns.

### Journey Requirements Summary

| Journey | Key Capabilities Revealed | Requirement Type |
|---------|--------------------------|-----------------|
| Alex — Evaluate & Adopt | Event contract docs, testing package as evaluation hook, prerequisite validation, quickstart quality, DI simplicity, peer validation | Product + Documentation |
| Alex — Testing | Testing package fidelity, same-domain-logic guarantee, projection-level isolation testing, CI compatibility | Product |
| Alex — Multi-service | Event contract reference, contracts reuse, per-service projections, event ordering docs, network effect | Product + Documentation |
| Alex — First Error | Error messages, aggregate invariants, optimistic concurrency, cross-aggregate timing docs | Product + Documentation |
| Alex — Tenant Discovery | ListTenants, GetTenant, GetTenantUsers read model queries, team onboarding | Product |
| Priya — Deploy & Operate | DAPR portability, stateless architecture, OpenTelemetry, isolation monitoring, bulk provisioning (post-MVP) | Product + Operations |
| Sofia — Security | Audit projections (standard read model), anomaly detection, event-driven revocation, temporal queries, compensating commands | Product + Compliance |

**Documentation requirements surfaced across journeys:** Quickstart with prerequisite validation, event contract reference, eventual consistency and event ordering guide, cross-aggregate timing documentation, compensating command patterns guide.

## Innovation & Novel Patterns

### The Core Insight

A developer removes a contractor from a tenant. In a CRUD-based system, nothing happens — the contractor's access persists in three other services until someone manually updates each one, if they remember. In Hexalith.Tenants, every subscribing service revokes access within seconds, and the event stream records exactly who removed the contractor, when, and from which role — queryable forever.

This difference isn't a feature gap. It's an architectural category difference.

Every multi-tenant .NET library treats tenant data as static rows in a database. Hexalith.Tenants treats it as what it actually is: domain state that changes over time, produces events, and must flow reactively across services. Multi-tenancy is a domain concern, not an infrastructure concern.

From this recognition, everything else follows:
- If tenants are domain state, they should be event-sourced
- If tenant changes are domain events, they should flow through the event pipeline
- If tenant events flow between services, each service subscribes rather than implements
- If tenant state is temporal, audit and compliance are inherent, not bolted on

### Innovation Areas

**1. Event-Sourced Tenant Management**

No existing .NET library treats tenant-user-role relationships as event-sourced domain state. This isn't merely a matter of timing — it's an architectural capability gap. CRUD-based tenant systems fundamentally cannot produce domain events, support temporal queries, or enable reactive cross-service integration. These capabilities are impossible without event sourcing the tenant model.

This innovation directly shapes product scope: the EventStore authorization plugin is post-MVP because the core innovation — event-sourced tenant state flowing reactively — delivers value without synchronous enforcement. The plugin optimizes the enforcement timing window, not a prerequisite for the reactive model.

**2. Temporal Audit as Inherent Capability**

The innovation is recognizing that tenant management is one of the domains where temporal auditability matters most. Compliance officers and security auditors need to answer "who had access at 3:47 PM last Tuesday, and who granted it?" CRUD-based systems require custom audit log bolting, log correlation, and reconstruction. Event-sourced tenant management answers these queries natively because the event stream is the audit trail.

**3. Isolation Invariant Guarantee Through Testing Fakes**

`Hexalith.Tenants.Testing` provides in-memory fakes that run the same domain logic as the production service. Tenant isolation invariants tested in-memory are the same invariants enforced in production at the aggregate domain logic level.

Scope of the guarantee: the fakes guarantee isolation at the **aggregate domain model level** — command validation, event production, and state transitions. They do not guarantee isolation at the projection or query layer, which depends on how the consuming service builds its read models. Consuming services are responsible for testing their own projection-level isolation, using the tenant events the fakes produce.

### Competitive Landscape

No known open-source project in the .NET ecosystem event-sources tenant management. Existing approaches occupy fundamentally different architectural categories:

| Capability | Hexalith.Tenants | Finbuckle / EF Core Filters | Identity Provider (Keycloak / Entra ID) |
|---|---|---|---|
| Domain events from tenant changes | Native — all changes are domain events | Impossible — CRUD architecture | Platform notifications — not domain events, not part of application event pipeline |
| Temporal audit queries | Inherent — event stream is the audit trail | Impossible — current state only | Basic audit log, not temporal domain queries |
| Cross-service reactive integration | Native — DAPR pub/sub domain events | Impossible — single-service scoped | Platform webhooks — manual integration, no domain semantics |
| Adoption simplicity | Moderate — requires EventStore + DAPR | High — EF Core plugin, familiar | High — already in place |

**Why these gaps are permanent:**

| Capability Gap | Effort to Close |
|---|---|
| Domain events from tenant changes | Full architectural rewrite from CRUD to event sourcing — not a feature addition, a redesign |
| Temporal audit queries | Requires adding an event store layer for tenant state — fundamental change to data model |
| Cross-service reactive integration | Requires adopting event-driven architecture for tenant state — architectural transformation |

This is an architectural category difference. Features can be copied. Architectural foundations cannot.

**Market context:** Hexalith.Tenants serves the Hexalith.EventStore ecosystem — .NET developers building event-sourced, DAPR-native applications that need multi-tenancy. This is a focused market, not a mass market. The product's value grows with EventStore adoption.

### Defensibility

**Primary moat — Network effect:** Each service subscribing to the tenant event stream deepens the organization's investment in the shared tenant model. After two or three services subscribe, switching means rewiring every service simultaneously.

**Secondary moat — Event contract gravity:** Once the event schema becomes the de facto contract in the ecosystem, a competitor must either adopt the same schema (validating Hexalith.Tenants' design) or create an incompatible one (fragmenting the ecosystem). Post-v1.0 contract stability transforms the event schema from an implementation detail into an ecosystem standard.

### Validation Approach

- **Internal validation (Hexalith.Parties):** First consuming service demonstrates the reactive tenant model end-to-end. Proves the paradigm works technically.
- **External validation (community adoption):** NuGet downloads, GitHub stars, and independent projects adopting the packages prove the paradigm solves a real problem. Target: multiple independent projects within the first year.

### The "Aha Moment" Demo

A first-class adoption artifact that proves every innovation claim in under two minutes:

1. Create a tenant
2. Add a user with TenantContributor role
3. Show three subscribing services receiving the `UserAddedToTenant` event
4. Remove the user
5. Watch all three services revoke access automatically in real time
6. Query the event history — full audit trail of who did what, when

This 90-second sequence communicates the value of event-sourced tenant management more effectively than any written explanation. It should be produced as a screencast or live demo and treated as a primary adoption tool alongside the quickstart guide.

## Developer Tool Specific Requirements

### Project-Type Overview

Hexalith.Tenants is a .NET developer tool distributed as NuGet packages and a deployable microservice. It follows the same project structure, conventions, and documentation approach as Hexalith.EventStore — ensuring consistency across the Hexalith ecosystem.

### Language & Framework Support

- **Primary language:** C# (.NET 10+, matching EventStore's SDK pinning via `global.json`)
- **F# support:** Future consideration — event contracts and client packages designed with F# consumption in mind
- **Nullable references:** Enabled globally
- **Implicit usings:** Enabled globally

### NuGet Package Architecture

| Package | Purpose | Dependencies |
|---------|---------|-------------|
| `Hexalith.Tenants.Contracts` | Commands, events, result types, identities | Minimal — Hexalith.EventStore.Contracts |
| `Hexalith.Tenants.Client` | Client abstractions and DI registration | Contracts |
| `Hexalith.Tenants.Server` | Domain service with aggregate, processors, DAPR integration | Contracts, Client, EventStore.Server |
| `Hexalith.Tenants.Testing` | In-memory fakes for integration testing | Contracts, Server (same domain logic) |
| `Hexalith.Tenants.Aspire` | .NET Aspire hosting extensions | Contracts, Client |

**Package quality standards:** Source Link, deterministic builds, XML documentation, MinVer (git tag-based SemVer, prefix `v`), centralized package management via `Directory.Packages.props`, CI validates expected package count before NuGet push.

### Solution & Project Structure

```
src/
  Hexalith.Tenants.Contracts          # Domain types: commands, events, results, identities
  Hexalith.Tenants.Client             # Client abstractions and DI registration
  Hexalith.Tenants.Server             # Server-side domain aggregate, processors, DAPR
  Hexalith.Tenants         # REST API gateway, auth, validation
  Hexalith.Tenants.Aspire             # .NET Aspire hosting extensions
  Hexalith.Tenants.AppHost            # Aspire AppHost (DAPR topology orchestrator)
  Hexalith.Tenants.ServiceDefaults    # Shared service config, OpenTelemetry
  Hexalith.Tenants.Testing            # In-memory fakes using same domain logic

tests/
  Hexalith.Tenants.Contracts.Tests    # Tier 1 — unit tests
  Hexalith.Tenants.Client.Tests       # Tier 1 — unit tests
  Hexalith.Tenants.Server.Tests       # Tier 2 — requires DAPR slim init
  Hexalith.Tenants.Testing.Tests      # Tier 1 — test the testing fakes
  Hexalith.Tenants.IntegrationTests   # Tier 3 — Aspire E2E contract tests

samples/
  Hexalith.Tenants.Sample             # Sample consuming service demonstrating event subscription
```

**Solution file:** `Hexalith.Tenants.slnx` (modern XML solution format)

### API Surface

- **Command API:** REST endpoints for tenant commands via Hexalith.Tenants project
- **Event contracts:** CloudEvents 1.0 published via DAPR pub/sub
- **Read model queries:** ListTenants, GetTenant, GetTenantUsers via standard read model projections
- **Client DI registration:** Consuming services register via minimal DI extension method

### Test Architecture

- **Tier 1 — Unit tests:** No external dependencies. Run in CI on every PR
- **Tier 2 — Integration tests:** Requires DAPR slim init. Server tests with DAPR state/pub-sub
- **Tier 3 — E2E contract tests:** Requires full DAPR init + Docker. Aspire-orchestrated cross-service validation
- **Framework:** xUnit, Shouldly, NSubstitute, coverlet.collector
- **Coverage target:** > 80% line coverage, 100% branch coverage on isolation and authorization logic

### Code Style & Conventions

Inherited from EventStore's `.editorconfig`: file-scoped namespaces, Allman braces, `_camelCase` private fields, `I` prefix for interfaces, `Async` suffix for async methods, 4-space indentation, CRLF, UTF-8, warnings as errors.

### Documentation Strategy

Following EventStore's approach: README.md with quickstart demo GIF and badges, docs/ folder for conceptual documentation, CHANGELOG.md, CONTRIBUTING.md, inline C# code samples, GitHub Actions docs validation (markdown lint, lychee link checking), quickstart guide targeting < 30 minutes.

### CI/CD Pipeline

- **CI:** GitHub Actions on push/PR to main — restore, build (Release), Tier 1+2 tests, optional Tier 3
- **Release:** Triggered by `v*` tags — full test suite, pack, validate 5 packages, push to NuGet.org
- **Branch naming:** `feat/<description>`, `fix/<description>`, `docs/<description>`

### Key Dependencies

Hexalith.EventStore (Contracts, Client, Server), DAPR SDK, .NET Aspire, MediatR, FluentValidation, OpenTelemetry.

### Implementation Considerations

- **Aggregate pattern:** `Handle(Command, State?) -> DomainResult` with `Apply(Event)` on state — pure functions, no side effects
- **Fluent convention:** Reflection-based discovery of Handle/Apply methods (no manual registration)
- **Multi-tenancy at contract level:** Tenant commands carry TenantId following EventStore's `Domain + AggregateId + TenantId` pattern
- **DAPR abstraction:** All infrastructure accessed via DAPR sidecars for infrastructure portability

## Functional Requirements

### Tenant Lifecycle Management

- FR1: A global administrator can create a new tenant with a unique identifier and name (in MVP, tenant creation is restricted to global administrators)
- FR2: A developer can update a tenant's metadata (name, description)
- FR3: A global administrator can disable a tenant, preventing all commands against that tenant from succeeding
- FR4: A global administrator can re-enable a previously disabled tenant, restoring normal command processing
- FR5: The system produces a domain event for every tenant lifecycle change (created, updated, disabled, enabled)

### User-Role Management

- FR6: A tenant owner can add a user to a tenant with a specified role (TenantOwner, TenantContributor, or TenantReader)
- FR7: A tenant owner can remove a user from a tenant
- FR8: A tenant owner can change a user's role within a tenant
- FR9: The system rejects adding a user who is already a member of the tenant
- FR10: The system rejects role changes that violate escalation boundaries (a tenant owner cannot assign GlobalAdministrator)
- FR11: The system produces a domain event for every user-role change (added, removed, role changed)
- FR12: The system enforces optimistic concurrency, rejecting conflicting concurrent modifications to the same aggregate

### Global Administration

- FR13: An existing global administrator can designate a user as a global administrator
- FR14: An existing global administrator can remove a user's global administrator status (cannot remove self if they are the last global administrator)
- FR15: A global administrator can perform any tenant operation across all tenants without per-tenant role assignment
- FR16: All global administrator actions produce auditable domain events
- FR17: The system provides a bootstrap mechanism (seed command or startup configuration) to create the initial global administrator on first deployment when no global administrators exist
- FR18: The bootstrap mechanism only executes when zero global administrators exist in the event store — subsequent executions are rejected with a specific error indicating that bootstrap has already been completed

### Tenant Configuration

- FR19: A tenant owner can set a key-value configuration entry for a tenant
- FR20: A tenant owner can remove a configuration entry from a tenant
- FR21: Configuration keys support dot-delimited namespace conventions (e.g., `billing.plan`, `parties.maxContacts`) to prevent collisions between consuming services
- FR22: The system produces a domain event for every configuration change (set, removed)
- FR23: The system enforces configuration limits: maximum 100 keys per tenant, maximum 1KB per value, maximum 256 characters per key
- FR24: The system rejects configuration operations that exceed limits with a specific error identifying which limit was exceeded and the current usage

### Tenant Discovery & Query

The tenant service owns a centralized read model projection for tenant discovery, audit, and operational queries (FR25-30). Consuming services build their own local projections from the tenant event stream for per-service access enforcement and tenant-aware behavior. These are complementary — the centralized model serves queries, the local projections serve runtime enforcement.

- FR25: A developer can query a paginated list of all tenants with their IDs, names, and statuses
- FR26: A developer can query a specific tenant's details including its current users and their roles
- FR27: A developer can query the list of users in a specific tenant with their assigned roles
- FR28: A developer can query the list of tenants a specific user belongs to, with their role in each tenant
- FR29: A global administrator can query tenant access changes by tenant ID and date range for audit reporting, with pagination support (default page size: 100 results, maximum: 1,000)
- FR30: All list and query endpoints support cursor-based pagination with consistent ordering

### Role Behavior

- FR31: A TenantReader can query tenant details, user lists, and configuration for tenants they belong to, but cannot execute any state-changing commands
- FR32: A TenantContributor has TenantReader capabilities plus the ability to execute domain commands within the tenant (the specific commands are defined by each consuming service)
- FR33: A TenantOwner has TenantContributor capabilities plus user-role management (add, remove, change role) and tenant configuration management
- FR34: A user with roles in multiple tenants can only access data and execute commands within each tenant according to their role in that specific tenant — roles do not transfer or aggregate across tenants

### Event-Driven Integration

- FR35: The system publishes all tenant domain events via DAPR pub/sub as CloudEvents 1.0
- FR36: The system uses a documented topic naming convention for tenant events (e.g., `tenants.events`) consistent with Hexalith ecosystem patterns
- FR37: A consuming service can subscribe to tenant events and build a local projection of tenant state
- FR38: A consuming service can react to user addition/removal events to enforce or revoke access
- FR39: A consuming service can react to tenant disable/enable events to block or allow operations
- FR40: A consuming service can react to configuration change events to update tenant-specific behavior
- FR41: Event contracts include sufficient information (event ID, aggregate version) for consuming services to implement idempotent event handling
- FR42: Documentation provides guidance on idempotent event processing patterns for consumers, since DAPR pub/sub may deliver events more than once. Minimum content: at-least-once delivery explanation, deduplication by event ID example, idempotent handler pattern with code sample

### Developer Experience & Packaging

- FR43: A developer can install Hexalith.Tenants via NuGet packages (Contracts, Client, Server, Testing, Aspire)
- FR44: A developer can register tenant client services in DI with a single extension method call
- FR45: A developer can register tenant event handlers in a consuming service in under 20 lines of DI configuration
- FR46: A developer can write tenant integration tests using in-memory fakes without external infrastructure, in under 10 lines per test
- FR47: The in-memory testing fakes execute the same domain logic as the production service, guaranteeing isolation at the aggregate domain model level (command validation, event production, state transitions), verified by a conformance test suite that runs identical command sequences against both fakes and production aggregate. Projection-level and query-level isolation is the responsibility of the consuming service's own test suite
- FR48: A developer can deploy the tenant service using .NET Aspire hosting extensions
- FR49: The system provides error messages for all command rejections that include: the specific rejection reason, the entity involved, and a corrective action hint

### Command Validation & Error Handling

- FR50: The system rejects commands targeting a non-existent tenant with a specific error identifying the missing tenant
- FR51: The system rejects commands targeting a disabled tenant with a specific error indicating the tenant's disabled status
- FR52: The system rejects duplicate operations (e.g., adding an already-present user) with a specific error including current state
- FR53: Commands and event storage succeed independently of DAPR pub/sub availability (event store is the source of truth)

### Observability & Operations

- FR54: The system exposes tenant command latency metrics via OpenTelemetry
- FR55: The system exposes event processing metrics via OpenTelemetry
- FR56: A platform operator can deploy the tenant service alongside EventStore using standard DAPR configuration
- FR57: The tenant service is stateless between requests — all state is reconstructed from the event store on startup
- FR58: The CI/CD pipeline enforces quality gates: build, test (Tier 1+2), coverage threshold (> 80% line, 100% branch on isolation/auth), and package validation before NuGet publish

### Documentation & Adoption

- FR59: The project provides a quickstart guide that enables a developer to send their first tenant command within 30 minutes
- FR60: The quickstart guide includes prerequisite validation (DAPR sidecar, EventStore deployment)
- FR61: The project provides an event contract reference documenting all commands, events, and their schemas
- FR62: The project provides a sample consuming service demonstrating event subscription and access enforcement
- FR63: The project provides an "aha moment" demo (screencast or video) showing reactive cross-service access revocation
- FR64: The project provides documentation on cross-aggregate timing behavior, including the event propagation window between tenant commands and subscriber processing. Minimum content: timing window explanation, sequence diagram, guidance on designing for eventual consistency, reference to planned auth plugin as synchronous enforcement option
- FR65: The project provides documentation on compensating command patterns (e.g., restoring a wrongly removed user with explicit role specification). Minimum content: compensating command definition, worked example with AddUserToTenant after incorrect RemoveUserFromTenant, explanation of why role must be explicitly specified (not auto-restored)

## Non-Functional Requirements

### Performance

- NFR1: All tenant commands complete within 50ms (p95) as measured by OpenTelemetry span duration
- NFR2: All read model queries complete within 50ms (p95) for result sets within a single page (see FR30 pagination), as measured by OpenTelemetry span duration
- NFR3: Event publication to DAPR pub/sub completes within 50ms (p95) after command processing, as measured by OpenTelemetry span duration
- NFR4: In-memory testing fakes execute commands and produce events within 10ms, as measured by xUnit test execution time

### Security

- NFR5: Zero cross-tenant data leaks — no query, projection, or event subscription returns data belonging to a different tenant, verified by dedicated Tier 3 integration tests that assert isolation across all read model endpoints and event subscriptions
- NFR6: Role escalation boundaries enforced at the domain level — no actor can self-escalate, verified by unit tests that assert rejection of every escalation path (TenantOwner assigning GlobalAdministrator, self-role elevation)
- NFR7: All state-changing operations produce immutable, auditable domain events with actor ID, timestamp, and full operation context, verified by integration tests that assert event production for every command type and validate required event fields are populated
- NFR8: Disabled tenants reject all commands immediately within the same aggregate, verified by unit tests that assert command rejection after DisableTenant is applied to aggregate state
- NFR9: Encryption at rest and in transit is a deployment concern — the system relies on DAPR infrastructure configuration for encryption and does not implement its own encryption layer
- NFR10: 100% branch coverage on tenant isolation and role authorization logic (defined as: aggregate Handle methods for authorization checks, tenant ID filtering in projections, and role validation logic), verified in CI via coverlet

### Scalability

- NFR11: The system supports up to 1,000 tenants with up to 500 users per tenant without performance degradation beyond stated latency targets, verified by load tests seeding the target volume and asserting NFR1-NFR3 latency targets hold
- NFR12: The tenant service is stateless — horizontal scaling achieved by adding service instances
- NFR13: State reconstruction from the event store on startup completes within 30 seconds for up to 1,000 tenants with an assumed average of 500 events per tenant (500,000 total events), verified by a startup benchmark test that seeds the target event volume and measures time to ready state. Event store snapshots are a Phase 3 optimization if this target is exceeded at scale

### Integration

- NFR14: All domain events conform to CloudEvents 1.0 specification
- NFR15: Event publication uses DAPR pub/sub abstraction — no direct dependency on a specific message broker
- NFR16: State persistence uses DAPR state store abstraction — no direct dependency on a specific database
- NFR17: The system degrades gracefully when DAPR pub/sub is unavailable — commands succeed, subscribers catch up when pub/sub recovers, verified by a Tier 3 integration test that disables pub/sub, executes commands, re-enables pub/sub, and asserts subscribers receive all pending events
- NFR18: Event contracts are backward-compatible after v1.0 — no breaking schema changes to published events
- NFR19: All domain events include event ID and aggregate version to enable idempotent processing by consumers

### Reliability

- NFR20: The event store is the single source of truth — system state can be fully reconstructed by replaying events
- NFR21: Command processing and event storage are atomic — a command either fully succeeds or fully fails
- NFR22: API availability target: 99.9% in production deployments, as measured by health check endpoint uptime monitoring
- NFR23: No data loss under any failure scenario — events once stored are immutable and durable

### Accessibility & Internationalization

- NFR24: MVP error messages and documentation are English-only. Phase 2 Admin UI must address WCAG 2.1 AA accessibility and i18n considerations as part of its requirements scoping
