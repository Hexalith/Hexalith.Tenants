# Sprint Change Proposal — Shared Domain-Service Infrastructure Extraction

- **Project:** Tenants
- **Date:** 2026-06-02
- **Author:** Jerome (via Correct Course workflow)
- **Trigger origin:** Post-implementation architecture review after Tenants completed the current backend/package/UI-planning sprint. The review identified reusable technical infrastructure inside Tenants that should live in shared Hexalith platform modules.
- **Mode:** Incremental; sections 1-5 approved individually.
- **Scope classification:** Direct Adjustment + Artifact Clarification. Route to Architect/Product for backlog shaping, then Developer for staged implementation.

---

## Section 1 — Issue Summary

Tenants is intended to be a minimal EventStore-native domain aggregate implementation: tenant commands, events, aggregate rules, read models, tenant-specific projections, domain authorization/query semantics, package adapters, and adoption documentation.

The current implementation also contains reusable technical mechanics that are common to any EventStore-backed domain service. Keeping these mechanics inside Tenants creates architecture drift:

- The next domain aggregate project is likely to copy Tenants boilerplate instead of consuming shared platform APIs.
- Tenants becomes harder to read as a domain implementation because hosting, projection-write, cursor, subscription, and test-harness mechanics are mixed with tenant behavior.
- Product and architecture artifacts say EventStore provides foundational infrastructure, but some implementation and documentation still describe Tenants as owning reusable runtime infrastructure.

This is not a functional defect in completed Tenants behavior. It is an ownership and reuse correction.

### Evidence

- PRD states that EventStore provides foundational infrastructure and Tenants effort should focus on tenant domain model, packages, and documentation.
- Architecture requires Tenants to be an EventStore-native domain service, not a generic infrastructure template.
- Code review identified reusable mechanics in ServiceDefaults, DAPR health checks, host route registration, projection write/state helpers, cursor/pagination helpers, client event subscription processing, and in-memory testing harnesses.

---

## Section 2 — Epic Impact

This correction is cross-epic and post-implementation. No single story caused it.

### Current Sprint State

The current sprint status shows Epics 1-9 complete, including Epic 9 planning-only UI readiness. Completed story records should not be rewritten as history.

### Epic-Level Assessment

| Area | Impact | Disposition |
|------|--------|-------------|
| Epics 1-3 domain model | Low | Tenant contracts, aggregates, events, rejections, and domain rules remain valid and Tenants-owned. |
| Epic 4 client/event integration | Medium | Tenant event contracts and handler examples remain valid; reusable subscription endpoint, envelope processing, idempotent dispatch, and local projection mechanics are candidates for EventStore.Client or a shared client module. |
| Epic 5 queries/projections | High | Tenant-specific query semantics remain valid; cursor codec pattern, pagination policy, projection write policy, ETag retry/recovery helpers, and DAPR projection state store are candidates for EventStore or Commons. |
| Epic 6 testing | Medium | Tenant fake behavior remains valid; generic in-memory aggregate/domain-service harness and conformance helpers should move to shared EventStore testing infrastructure. |
| Epic 7 deployment/observability | High | Tenants deployment behavior remains valid; ServiceDefaults, DAPR state-store health check, domain-service route mapping, telemetry conventions, and hosting glue should move to EventStore/shared infrastructure where possible. |
| Epic 8 documentation/adoption | Medium | Documentation remains valid but should describe Tenants as consuming shared platform infrastructure after extraction. |
| Epic 9 Phase 2 UI planning | Medium | Planning remains valid; future UI implementation should consume FrontComposer primitives instead of creating Tenants-owned UI framework code. |

### Epic Change Recommendation

Add a follow-up workstream named **Shared Domain-Service Infrastructure Extraction**.

Do not reopen completed Tenants epics. Do not roll back delivered behavior. Treat the existing Tenants code as source material for shared APIs, then migrate Tenants to consume those APIs.

---

## Section 3 — Artifact Impact

### PRD

No core PRD conflict exists. The MVP remains valid.

Required future PRD edits:

- Clarify that Tenants exposes domain packages over shared EventStore/Commons/FrontComposer primitives.
- Keep the five Tenants package names unless a later package strategy explicitly changes them.
- Avoid describing reusable runtime infrastructure as Tenants-owned when it is provided by shared Hexalith modules.

### Architecture

Material architecture updates are required after concrete shared APIs are defined.

Revise ownership language for:

- ServiceDefaults and health/readiness endpoint patterns.
- Domain-service host registration and EventStore route mapping.
- Projection write policy, projection state-store adapters, ETag retries, and recovery diagnostics.
- Cursor signing, cursor scope binding, pagination policy, and generic paginated result shape.
- Client event subscription endpoint, event envelope processor, and idempotent dispatch.
- In-memory domain-service test harness and conformance helper patterns.

Architecture should explicitly state:

- Tenants owns domain contracts, aggregate behavior, tenant-specific projections, tenant-specific query authorization/filtering, and tenant adoption docs.
- EventStore owns EventStore-domain-service mechanics.
- Commons owns small generic primitives that are not EventStore-specific.
- FrontComposer owns reusable UI composition primitives and operational UI shell patterns.

### UI/UX

The UX spec already supports FrontComposer for low-risk projection-driven surfaces and custom overrides for high-risk command workflows.

Future UI stories should:

- Consume reusable FrontComposer shell/data/action/freshness primitives where available.
- Keep tenant-specific mappings and workflow decisions in the Tenants UI adapter layer.
- Avoid reshaping immutable Tenants domain contracts for UI generation.

### Secondary Artifacts

Update after extraction APIs exist:

- `README.md` project structure and package descriptions.
- Package validation scripts if expected dependencies change.
- Solution/package governance tests that assert project boundaries.
- Consumer package smoke tests if new shared module dependencies appear.
- Deployment docs that currently imply Tenants owns shared ServiceDefaults or health-check mechanics.

Completed implementation story files should remain historical records. The new proposal supersedes their long-term ownership assumptions.

---

## Section 4 — Path Forward

### Option 1 — Direct Adjustment

Viable. Add staged extraction work, migrate Tenants to shared APIs, and update artifacts/tests afterward.

- **Effort:** Medium to high.
- **Risk:** Medium.
- **Primary risks:** public API design, package dependency changes, cross-repo versioning, submodule pointer coordination, and tests that intentionally assert current Tenants-owned structure.

### Option 2 — Rollback

Not viable. Completed Tenants behavior is valid. Rolling back Epics 4-7 would discard useful behavior and evidence without solving the ownership problem.

- **Effort:** High.
- **Risk:** High.
- **Benefit:** Low.

### Option 3 — PRD MVP Review

Viable only as confirmation. The MVP does not need reduction or redefinition.

- **Effort:** Low.
- **Risk:** Low.
- **Outcome:** Clarify ownership language; keep MVP scope.

### Recommended Approach

Use a hybrid path:

1. Keep completed Tenants behavior.
2. Add the shared extraction workstream.
3. Implement shared APIs in small stages.
4. Migrate Tenants after each shared API is available.
5. Update artifacts and tests when the code boundary is real.

This preserves delivery evidence while improving long-term platform reuse.

---

## Section 5 — Proposed Workstream

### Workstream Name

**Shared Domain-Service Infrastructure Extraction**

### Objective

Reduce Tenants to a focused domain aggregate/service implementation by moving reusable EventStore-domain-service, Commons, and FrontComposer mechanics into their appropriate shared modules.

### Proposed Sequencing

1. **Commons primitives**
   - Move generic pagination/result/options helpers that have no Tenants or EventStore dependency.
   - Candidate: generic `PaginatedResult<T>` and small reusable validation helpers.

2. **EventStore hosting/runtime**
   - Move ServiceDefaults pattern, DAPR state-store health check, domain-service route mapping, `/process` and projection endpoint wiring, telemetry conventions, and common app startup helpers where they can be made domain-neutral.

3. **EventStore query/projection infrastructure**
   - Move cursor codec pattern, pagination policy, cursor scope validation primitives, projection write policy, DAPR projection state-store adapter, ETag retry/recovery behavior, and projection write diagnostics.

4. **EventStore client infrastructure**
   - Move generic event subscription endpoint, event envelope processor, idempotent handler dispatch, and local projection application mechanics.

5. **EventStore testing infrastructure**
   - Move reusable in-memory aggregate/domain-service harness patterns and conformance helper utilities.
   - Keep tenant command fixtures and tenant-specific assertions in Tenants.

6. **FrontComposer follow-up**
   - Convert Tenants UI planning into reusable FrontComposer operational primitives where appropriate.
   - Keep tenant-specific UI mappings, command availability rules, and domain wording in a Tenants adapter layer.

7. **Tenants migration and cleanup**
   - Replace copied infrastructure with shared-module calls.
   - Keep only tenant domain code, tenant-specific adapters, and tenant documentation in this repository.

### Candidate Ownership Map

| Candidate code/pattern | Target module | Rationale |
|------------------------|---------------|-----------|
| ServiceDefaults, OTEL/readiness patterns, DAPR state-store health check | EventStore | Common to EventStore-backed domain services. |
| Domain-service route/handler registration and EventStore host wiring | EventStore | Every domain service should expose the same EventStore integration surface with minimal host code. |
| Projection write policy, DAPR projection state store, ETag retry/recovery diagnostics | EventStore | Cross-domain projection correctness infrastructure. |
| Cursor codec pattern, pagination policy, cursor scope validation primitives | EventStore or Commons | EventStore if tied to query envelopes/security; Commons only for generic DTOs/policies. |
| `PaginatedResult<T>` | Commons | Generic transport DTO shape, not tenant-specific. |
| Client event subscription endpoint/envelope processor/idempotent dispatch | EventStore.Client/shared client module | Reusable consumer integration mechanics for domain events. |
| In-memory domain-service harness/conformance helper pattern | EventStore testing infrastructure | Reusable way to prove fake/aggregate parity. |
| FrontComposer shell/data-grid/freshness/action patterns | FrontComposer | Reusable UI composition and operational feedback primitives. |

### What Must Stay in Tenants

- Commands, events, rejections, enums, identities, and tenant query contracts.
- `TenantAggregate`, `GlobalAdministratorsAggregate`, tenant states, and domain invariants.
- Tenant read models and tenant-specific projection mutation logic.
- Tenant-specific authorization, query filtering, audit semantics, and support-safe wording.
- Tenant package adapters and documentation that describe tenant adoption.

---

## Section 6 — Handoff Plan

| Role | Responsibility |
|------|----------------|
| Architect / PM | Create the extraction epic/story set, decide API ownership, define compatibility and package-versioning policy, and identify any breaking-change constraints. |
| Developer | Implement shared APIs, migrate Tenants incrementally, update submodule pointers, and keep Tenants behavior unchanged. |
| Test Architect / Developer | Update package-boundary tests, package validation scripts, consumer smoke tests, projection/write/conformance coverage, and migration regression tests. |
| Tech Writer | Update PRD/architecture/README/deployment/adoption docs after concrete shared APIs exist. |

### Sprint Status

No `sprint-status.yaml` update is made by this proposal.

Reason: the proposal recommends a new workstream, but concrete epic/story IDs and acceptance criteria have not yet been produced. A follow-up `create-epics-and-stories` or sprint-planning run should add the actual backlog entries.

### Success Criteria

- Tenants host startup and runtime behavior remain functionally equivalent after migration.
- Tenants package public behavior remains stable except for explicitly approved dependency/API changes.
- A new EventStore-backed domain aggregate project can reuse the shared infrastructure with materially less boilerplate than Tenants currently contains.
- Tenants code visibly centers on domain contracts, aggregates, projections, query semantics, and adapters.
- Package validation, solution structure tests, and adoption docs describe the new shared-module ownership accurately.

---

## Appendix — Change Navigation Checklist Results

| Section | Item | Status |
|---------|------|--------|
| 1.1 | Triggering story identified | N/A — post-implementation architecture review after completed sprint. |
| 1.2 | Core problem defined | Done — architecture drift / reusable infrastructure leakage. |
| 1.3 | Supporting evidence gathered | Done — planning docs and code review evidence collected. |
| 2.1 | Current epic assessed | N/A — all current sprint epics are complete. |
| 2.2 | Epic-level changes determined | Done — add follow-up extraction workstream. |
| 2.3 | Remaining/future epics reviewed | Done — future UI implementation and future domain projects are impacted. |
| 2.4 | Invalidated/new epics checked | Done — no epic invalidated; new follow-up workstream recommended. |
| 2.5 | Priority/order considered | Done — extraction should happen before using Tenants as another domain-project template. |
| 3.1 | PRD conflicts checked | Done — no MVP conflict; ownership wording updates needed. |
| 3.2 | Architecture conflicts checked | Done — material ownership updates required. |
| 3.3 | UI/UX conflicts checked | Done — minor/supportive; FrontComposer ownership reinforced. |
| 3.4 | Secondary artifacts checked | Done — README, tests, scripts, package validation, deployment docs impacted. |
| 4.1 | Direct Adjustment evaluated | Viable — selected as part of hybrid path. |
| 4.2 | Rollback evaluated | Not viable. |
| 4.3 | PRD MVP review evaluated | MVP unchanged; clarify only. |
| 4.4 | Recommended path selected | Done — hybrid direct adjustment plus artifact clarification. |
| 5.1 | Issue summary created | Done. |
| 5.2 | Epic/artifact impact documented | Done. |
| 5.3 | Recommended path documented | Done. |
| 5.4 | MVP impact/action plan defined | Done. |
| 5.5 | Agent handoff plan established | Done. |
| 6.1 | Checklist completion reviewed | Done — sections 1-5 approved incrementally; section 6 reviewed after file creation. |
| 6.2 | Proposal accuracy verified | Done — proposal is consistent with approved findings and no sprint-status update is required yet. |
| 6.3 | Explicit user approval | Done — approved by Jerome on 2026-06-02. |
| 6.4 | Sprint-status update | N/A until concrete backlog entries are created. |
| 6.5 | Next steps and handoff confirmed | Done — handoff is Architect/Product backlog shaping, then staged Developer implementation. |
