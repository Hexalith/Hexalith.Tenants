---
title: "Sprint Change Proposal — Implementation Readiness Remediation"
date: 2026-07-14
status: approved-for-implementation
trigger: "_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-14.md"
workflow: bmad-correct-course
mode: incremental
scope: moderate-planning-correction-and-platform-handoff
approved_by: Administrator
approval_confirmed: 2026-07-15
---

# Sprint Change Proposal — Implementation Readiness Remediation

## Proposal Record

- **Trigger assessment:** _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-14.md
- **Trigger verdict:** NOT READY
- **Coverage baseline:** 25/25 functional requirements mapped
- **Actionable findings:** 16
- **Primary blockers:** FR-12 sequencing, superseded navigation architecture, and a freshness wire that cannot produce aging
- **Selected path:** Direct adjustment
- **Approval:** Administrator approved checklist Sections 1–4 and Proposals 1–6 individually, granted standing approval for Proposals 7–16 on 2026-07-14, and explicitly confirmed final approval on 2026-07-15.
- **Collision handling:** The unrelated approved file sprint-change-proposal-2026-07-14.md is preserved unchanged. This proposal uses a descriptive suffix.

## 1. Issue Summary

The implementation-readiness assessment found complete functional-requirement coverage but an incoherent implementation handoff. A team following the active artifacts can:

1. Treat Story 2.4 as complete for FR-12 audit proof even though the story explicitly waits for Epic 5 evidence.
2. Implement a superseded three-area navigation model from Architecture instead of the approved one-entry Tenants workspace.
3. Promise an aging freshness experience that the current QueryResponseMetadata wire cannot produce.

Thirteen additional findings affect dependency ownership, NFR traceability, canonical vocabulary, settled product decisions, runtime and route authority, story structure, test infrastructure, and artifact completeness.

This is not a strategic pivot, requirements-coverage failure, rollback request, or MVP reduction. The correction establishes one authority chain and makes every residual dependency executable:

PRD/addendum → UX and Architecture → Epics/stories → implementation evidence → readiness reassessment.

Historical retrospectives and approved proposals remain evidence snapshots. Active canonical artifacts receive the current decisions.

## 2. Impact Analysis

### Epic Impact

| Epic | Impact | Decision |
|---|---|---|
| Epic 1 — Tenant Workspace Triage and Read-Only Insight | Story 1.2 is oversized and depends on an unowned Memories handoff; Story 1.8 mixes product behavior with governance; UI test infrastructure ownership is implicit. | Keep the epic historically complete. Split base cursor-list triage from whole-set search, establish producer/consumer gates, separate readiness certification, and assign test-infrastructure ownership. |
| Epic 2 — Tenant Membership and Tenant Record Management | Story 2.4 claims full FR-12 while proof is delivered later. Story 2.1 is an oversized historical foundation slice. | Keep the epic historically complete. Make Story 2.4 independently complete for removal, projection confirmation, and honest evidence handoff; map proof closure to Epic 5. Preserve Story 2.1 as an evidence-backed historical exception. |
| Epic 3 — Tenant Lifecycle and Configuration Control | Story 3.5 exists only as a summary and external implementation record in the canonical epic. | Add its canonical story, BDD, and test contract without reopening the completed defect fix. |
| Epic 4 — Global Administrator Governance | Story 4.1 already reflects contextual/module-internal navigation. | No functional story change. Correct Architecture to match the epic and current UX. |
| Epic 5 — Audit Evidence and Forward Recovery | Existing Stories 5.2–5.4 provide the missing FR-12 proof capabilities; Story 5.8 is technical cleanup presented as an ordinary user story. | Add explicit FR-12 proof ownership to Stories 5.2–5.4 and label Story 5.8 as technical debt/enabler work. |

No epic becomes obsolete. No new product epic is required. Existing implementation status is not rolled back.

### Artifact Impact

- **PRD/addendum:** become the canonical product authority for FR-12 ownership, NFR identifiers, recovery language, audit placeholder behavior, localization ownership, route authority, freshness evidence, FC-TOK fallback, and resolved operational boundaries.
- **Architecture:** correct navigation, freshness D6, current build gates, runtime wording, dependency baselines, test-infrastructure ownership, and remaining platform handoffs.
- **UX:** synchronize runtime, navigation, freshness prerequisites, vocabulary, localization, audit behavior, RTL scope, and consumed dependency guidance.
- **Epics:** repair slicing and traceability, normalize NFR references, classify work honestly, assign dependencies, and restore Story 3.5 completeness.
- **Supporting records:** preserve historical truth and add forward pointers where an old statement could otherwise be mistaken for current authority.

### Technical Impact

One real shared-platform capability is required: EventStore query metadata must expose authoritative projection-time evidence such as ProjectedAt. Tenants then maps it through the server-side read path and classifies the full freshness ladder. ServedAt is explicitly prohibited as a substitute.

The Memories handoff, FC-TOK fallback, UI-test infrastructure, and consumed Fluent/FrontComposer version also require named ownership and evidence. This proposal does not authorize changes inside root-declared submodules.

## 3. Recommended Approach

Use **Option 1 — Direct Adjustment**.

Implementation order:

1. Correct PRD/addendum product authority.
2. Synchronize Architecture and UX.
3. Update Epics and story contracts.
4. Complete or verify the named EventStore, Memories, FrontComposer, and CI handoffs.
5. Re-run implementation readiness.

### Alternatives Evaluated

- **Rollback:** rejected. The epics are already delivered, and rollback would neither reconcile planning authority nor create projection-age evidence.
- **Temporary four-state freshness:** viable only as a fallback requiring Product/UX approval and synchronized removal of aging claims. Rejected while the authoritative timestamp handoff remains feasible.
- **New epic or MVP reduction:** rejected. The current five epics and product scope remain valid.

### Classification

- **Scope:** Moderate
- **Product risk:** Low
- **Delivery risk:** Medium, concentrated in the shared freshness contract and cross-repository evidence
- **Timeline effect:** one planning-correction pass plus verification of the named platform prerequisites before a clean handoff

## 4. Detailed Change Proposals

### Proposal 1 — Repair FR-12 Sequencing and Ownership

**Status:** Approved.

**Rationale:** Story 2.4 must be independently complete, while Epic 5 explicitly owns audit-proof delivery.

**PRD FR-12 note — OLD**

> Story 2.4 delivers command lifecycle, projection confirmation, and honest audit handoff; Audit Evidence Receipt/proof UX remains Epic 5 unless the evidence source is already implemented.

**NEW**

> **Delivery ownership:** Story 2.4 independently delivers fail-closed preview, elevated friction, command lifecycle, projection-confirmed removal, and an honest evidence handoff. Epic 5 Stories 5.2–5.4 deliver the scoped audit entry point, support-safe receipt, and explicit proof-availability states. FR-12 is complete only when evidence exists for both slices.

Update the coverage map:

- Epic 2 Story 2.4 — removal command, projection confirmation, and honest evidence handoff.
- Epic 5 Stories 5.2–5.4 — scoped audit access, support-safe receipt, and proof-availability closure.

Replace Story 2.4 references to evidence “not yet implemented by Epic 5” with a capability-neutral rule: Story 2.4 is complete when removal is projection-confirmed and unavailable proof is represented honestly. It does not require or assert receipt capability.

Add FR-12 (audit-proof completion slice) to Stories 5.2–5.4 and test removal-command audit entry, receipt derivation, and unavailable-proof behavior.

### Proposal 2 — Correct Architecture Navigation

**Status:** Approved.

**Rationale:** Architecture must match the approved PRD/UX Option A information architecture.

**Shell composition — OLD**

> Tenants default / Global Administrators / Audit primary; Users contextual.

**NEW**

> Register exactly one primary left-navigation entry for the Tenants module. It opens the Tenants workspace with page-local Tenants and lookup-backed Users tabs. Global Administrators and Audit are reached through module-internal or contextual entry points and are not separate Tenants left-navigation entries.

Routing contract:

- /tenants opens the workspace on its Tenants tab.
- /tenants/users and /tenants/my remain deep-link aliases that restore the appropriate workspace context.
- Global Administrator and Audit routes may remain deep-linkable but do not register primary Tenants navigation entries.
- Active tab, lookup input, filters, selection, and return focus are preserved where applicable.

Relabel directory annotations so Users, GlobalAdministrators, and TenantAuditPage are internal/contextual surfaces rather than navigation areas. Update TenantsShellManifest guidance and require tests for exactly one module entry, two page-local tabs, lookup-backed Users behavior, contextual governance/audit, and deep-link context preservation.

### Proposal 3 — Make Aging Producible

**Status:** Approved.

**Rationale:** Aging requires authoritative projection age; ServedAt cannot provide it.

**Architecture D6 — OLD**

> The wire carries current/stale/unknown; aging collapses into current until a future QueryResponseMetadata.ProjectedAt handoff.

**NEW**

> QueryResponseMetadata must expose nullable ProjectedAt projection evidence. Tenants classifies current, aging, stale, or unknown from that value using configured thresholds; refreshing remains a client transient. Missing or invalid projection evidence produces unknown. ServedAt must never be used as projection age.

EventStore producer responsibilities:

- Expose ProjectedAt through the supported query metadata contract.
- Preserve backward-compatible stale metadata during migration.
- Publish a supported package/version.
- Provide contract tests for present, absent, and threshold-boundary timestamps.
- Pass package-only consumer validation.

Tenants consumer responsibilities:

- Pin the verified EventStore package/version.
- Map ProjectedAt through REST/BFF metadata without exposing internals to the browser.
- Test threshold boundaries and all five UI states.
- Prove only stale and unknown block by default; aging remains usable with friction.

Any acceptance claiming operational aging remains blocked until the platform version and Tenants integration evidence are recorded.

### Proposal 4 — Split Story 1.2 and Own Memories Readiness

**Status:** Approved.

**Rationale:** The cursor list is independently useful; whole-set search depends on a separately owned Memories capability.

Rename Story 1.2 to **Tenant Cursor-List Triage**. Retain BFF listing, cursor paging, filtering/sorting, authorization-safe results, freshness/list states, responsive behavior, accessibility, localization, and support safety.

Extract **Story 1.2a: Whole-Set Tenant Search with Authoritative Hydration**. Move all Memories-specific criteria and tests to it:

- tenants-index syntactic/BM25 search across the authorized visible set.
- Tenant-ID match sets only.
- Authoritative hydration through Tenants reads.
- Exact status filtering.
- Index-lag honesty.
- Memories-unavailable fallback to Story 1.2.
- Safe dropping of missing or unauthorized matches.

FR-1 completes through the combined evidence of Stories 1.2 and 1.2a.

Producer ownership belongs to Hexalith.Memories maintainers. Required handoff evidence covers upsert ingestion, attribute indexing and REST filtering, tenants-index registration, a supported client/package version, and contract tests.

Tenants owns search-to-match-set tests, authoritative hydration/authorization, empty-search bypass, degraded fallback, and Aspire end-to-end evidence. Story 1.2a remains verification-required until those artifacts are cited.

### Proposal 5 — Establish One Canonical NFR Registry

**Status:** Approved.

**Rationale:** PRD NFR-1–NFR-5, epic-local NFR6–NFR10, and assessment-only NFR-X identifiers compete.

Retain PRD NFR-1–NFR-5 and add:

- **NFR-6:** Accessibility conformance, keyboard/focus, screen-reader semantics, no-color-only operation, and reduced motion.
- **NFR-7:** Localization using culture-aware whole strings and named placeholders.
- **NFR-8:** Support safety and privacy.
- **NFR-9:** Responsive safety and fail-closed narrow-width behavior.
- **NFR-10:** Acceptance evidence and story readiness gates.
- **NFR-11:** Fluent semantic visual-system integrity and layout stability.

Audit capacity remains part of NFR-1.

Normalize every epic/story reference to the hyphenated PRD identifiers. Review applicability rather than replacing ranges blindly. Retire assessment-local NFR-X identifiers after mapping them to the canonical registry. Add a story-to-NFR traceability matrix with no undefined identifiers or conflicting definitions.

### Proposal 6 — Unify Recovery Vocabulary

**Status:** Approved.

**Rationale:** UX uses two recovery actions that the PRD/addendum does not recognize as canonical.

Retain the existing recovery list and add:

- reassign tenant owner
- retry access removal

Safety semantics:

- Reassign tenant owner starts a new forward command with its own authorization, freshness, preview, lifecycle, projection confirmation, and audit evidence.
- Retry access removal is offered only when authoritative projection evidence still shows access, no conflicting command is pending, and the complete removal flow can be revalidated.
- Neither action automatically replays a stored payload or implies undo, rollback, or history mutation.

Synchronize PRD/addendum, UX, RecoveryVerbs, localized whole-string resources, Story 2.4, Stories 5.4–5.6, and vocabulary/resource guard tests.

### Proposal 7 — Close the Audit-Placeholder Decision

**Status:** Approved under standing approval.

**Rationale:** UX and Architecture select the placeholder while the PRD still treats it as open.

**OLD**

> Audit nav area in MVP: present in the shell; confirm whether to hide or stub.

**NEW**

> During Phase 2a, contextual or module-internal audit entry points open an honest localized not-yet-available placeholder. Audit is not registered as a separate Tenants left-navigation entry. Live audit content remains a Phase 2c deliverable.

Replace Open Question 9 with a resolved decision. The placeholder identifies missing implementation support, preserves scope and return context, remains keyboard/forced-colors safe, exposes no receipt or proof, and uses stable selectors plus Tenants-owned localized copy. Change DESIGN wording from “may render” to “renders.”

### Proposal 8 — Close Localization Ownership

**Status:** Approved under standing approval.

**Rationale:** Architecture D4 is settled, but PRD and UX still call ownership open.

**OLD**

> Resource ownership (shared shell resources vs. Tenants-owned keys) is open.

**NEW**

> Tenants owns all domain copy as whole-string TenantsResources.resx and TenantsResources.fr.resx entries with named placeholders and EN/FR parity. FrontComposer owns inherited shell chrome through FcShellResources. Runtime sentence-fragment assembly is prohibited.

Update PRD NFR-7, §9, Open Question 4, UX voice/tone and readiness notes, Architecture references, Epics, and story requirements. Add resource-parity and no-fragment-assembly guards. Adopter-specific terminology remains Tenants-owned unless a separately versioned shared shell key exists.

### Proposal 9 — Remove Stale Runtime and Readiness Language

**Status:** Approved under standing approval.

**Rationale:** UX still leads with Blazor Auto and Architecture still declares closed FrontComposer gates as active build blockers.

Replace the EXPERIENCE foundation statement with:

> The application runs as Blazor InteractiveServer with a server-side BFF. Circuit reconnect re-derives truth from authoritative projection reads; it never resurrects optimistic or cached in-flight success.

Replace remaining Auto-specific reconnect wording in UX and Architecture while preserving the at-least-once, projection-lag, pending-state, and reconnect invariants.

Replace the Architecture readiness verdict:

> Story 1.0 closed FC-LYT, FC-CMD, FC-CNC, FC-A11Y, FC-L10N, and FC-DOC; Story 1.2 closed the Tenants FC-TBL boundary. Current residual gates are the EventStore ProjectedAt handoff, verification of the Memories search handoff, FC-TOK fallback/version evidence, and per-story acceptance evidence.

Do not turn this into blanket readiness. Each residual gate retains an owner and evidence condition.

### Proposal 10 — Fix Command-Route Authority

**Status:** Approved under standing approval.

**Rationale:** Architecture fixes a versioned route while PRD and Epics still ask implementers to choose.

**OLD**

> Confirm POST /api/v1/commands versus /api/commands before command stories.

**NEW**

> The authoritative command route is POST /api/v1/commands. Status lookup is GET /api/v1/commands/status/{correlationId}. The unversioned /api/commands alias is not an implementation authority and must not be selected by future story work.

Close PRD Open Question 1, update the addendum and Epics additional requirements, and retain the Architecture route. Add route-conformance tests for gateway construction, DAPR/service invocation, and status lookup.

### Proposal 11 — Normalize Story Size and Work Classification

**Status:** Approved under standing approval.

**Rationale:** Technical work and governance are presented as ordinary user stories, while one completed foundation story is too broad to serve as a future precedent.

Apply:

1. Keep Story 1.0 explicitly labeled **timeboxed enabler spike**, with no claim of user-facing value. Add a reproducible verification-command/evidence field; documentary evidence alone is insufficient where an API probe or build can verify the gate.
2. Label Story 5.8 **technical-debt/enabler**. FR-24/FR-25 are supported, not delivered primarily, by the efficiency cleanup.
3. Rename Story 1.8 to **Support-Safe Identifier Copy**. Retain FR-7 behavior and remove Epic 1 readiness certification from its identity and acceptance criteria.
4. Create a separate **Epic 1 Readiness Certification Gate** mapping FR-1–FR-9, NFRs, UX requirements, dependencies, and evidence.
5. Preserve Story 2.1 as an accepted historical foundation exception because its implementation record proves the gateway, lifecycle, idempotency, locking, rejection, projection-confirmation, localization, accessibility, and test foundation. Future command stories may remain combined only when they cite that reusable evidence; otherwise split command submission/status from projection/evidence outcomes.
6. Apply Proposal 4’s Story 1.2 split.

### Proposal 12 — Assign CI and Test-Infrastructure Ownership

**Status:** Approved under standing approval.

**Rationale:** Downstream story contracts assume UI test projects and CI lanes that no story or task owns.

Add an **Epic 1 Test Infrastructure Gate** owned jointly by the Tenants Developer and Test Architect.

Acceptance evidence:

- tests/Hexalith.Tenants.UI.Tests exists, is in Hexalith.Tenants.slnx, and runs bUnit/xUnit v3/Shouldly per-project.
- A Playwright E2E project or approved equivalent exists with Aspire/auth fixtures and stable-selector conventions.
- CI registers the UI unit/component lane and the E2E lane, publishes results/artifacts, and documents blocking versus non-blocking policy.
- Local guidance records the xUnit v3 executable fallback where Microsoft.Testing.Platform/VSTest behavior requires it.
- Downstream stories cite the relevant lane and cannot claim acceptance solely from an unregistered test contract.

If the infrastructure already exists, this is verify-and-close work: cite the project, pipeline definition, command, and successful run instead of creating duplicates.

### Proposal 13 — Restore Canonical Story 3.5 Completeness

**Status:** Approved under standing approval.

**Rationale:** The canonical epic contains only a summary and link, not a self-contained story contract.

Add a canonical Story 3.5 block:

> **Story 3.5: Tenant Query Gateway REST Routing — Retire Projection-Actor Path**
>
> As a Tenants UI user, I want tenant list, detail, membership, and audit surfaces to load through supported REST reads, so that I can use real authorized projection data without a retired projection actor.

Canonical BDD covers:

- BFF calls the five supported GET /api/tenants* and GET /api/users/{id}/tenants routes.
- No TenantsProjectionActor, TenantProjectionRouting, ProjectionActorType, or generic EventStore query-gateway path is used.
- Strong ETags and If-None-Match produce 304 with last-confirmed snapshot preservation.
- Missing configuration fails closed through UnavailableTenantQueryGateway.
- Authorization, cursor validation, six surface states, and support safety remain intact.
- Source guards cover Contracts, UI, and the Tenants host.

Canonical tests cover controller 200/304 behavior, handler ETag propagation, gateway mapping, all read surfaces, transport failure mapping, unavailable configuration, support-safety, and actor-routing source guards.

Add a supersession note: the original Story 3.5 claim that ETag/ServedAt alone could derive aging is replaced by Proposal 3. Story 3.5 establishes REST/ETag transport; the EventStore ProjectedAt handoff owns operational aging.

### Proposal 14 — Approve FC-TOK Fallback and Pin the Consumed UI Baseline

**Status:** Approved under standing approval.

**Rationale:** FC-TOK uses an interim mapping without the approval record applied to other fallbacks, and planning artifacts contain dependency-version drift.

Add FC-TOK to the fallback approval record:

- Tenants may use its canonical vocabulary plus a verified Fluent semantic-role/icon mapping until FrontComposer publishes a shared token contract.
- Tenants must not create generic shared token infrastructure locally.
- Product/UX owns meaning and copy; UX/A11y verifies semantic role, icon, text, forced-colors, and no-color-only behavior; FrontComposer owns the replacement path.
- A future shared contract replaces the fallback only through versioned compatibility evidence.

Make central Directory.Packages.props and the consumed FrontComposer dependency graph the package authority. At correction time the observed Fluent UI Blazor baseline is 5.0.0-rc.3-26138.1; remove the obsolete rc.2 discrepancy note. Planning documents reference the consumed baseline and require build-time verification instead of independently pinning stale values.

Require dependency-change tests for component parameters, icons, semantic roles, forced-colors behavior, ARIA behavior, and fallback compatibility.

### Proposal 15 — Resolve Operational Acceptance Boundaries

**Status:** Approved under standing approval.

**Rationale:** Cursor behavior, freshness tuning, performance ownership, RTL scope, and conditional WCAG support are described as open or assumed even where an implementation-safe boundary is available.

Record:

- **Cursor invalidation:** cursors are opaque, signed, scope-bound, and session-scoped. On invalidation the UI re-queries page 1, shows a localized list-refreshed notice, and does not expose or parse the cursor. Multi-replica durability remains a platform backlog item, not an open UI behavior.
- **Freshness thresholds:** configuration-owned with conservative defaults and boundary tests. Product/Operations owns production tuning. No magic numbers appear in components. Missing projection evidence is unknown.
- **Performance:** NFR-1 targets remain explicit assumptions until measured against a warm projection and representative audit data. Product/Operations owns final budgets; implementation records measurement evidence. If the audit target cannot be met, page-size or virtualization work is required before production readiness.
- **RTL:** v1 uses logical, RTL-ready layout practices but does not commit to RTL shipping or formal RTL acceptance testing. RTL delivery is future funded scope.
- **Accessibility:** WCAG 2.1 AA remains the baseline. WCAG 2.2 AA remains conditional on the consumed Fluent/FrontComposer version and recorded verification.

Close the corresponding PRD questions where the behavior is now decided; retain only the explicitly owned tuning or future-scope obligations.

### Proposal 16 — Restore Canonical Handoff Hygiene and Re-run Readiness

**Status:** Approved under standing approval.

**Rationale:** Active artifacts mix historical implementation evidence, completed statuses, defect records, and future-plan language.

Apply these governance rules:

- Active PRD/addendum, UX, Architecture, and Epics state current authority.
- Historical proposals, retrospectives, and implementation records remain unchanged evidence snapshots.
- Add forward pointers when historical wording could be copied as current guidance.
- Each external dependency record names producer, consumer, status, supported version/contract, acceptance evidence, and fallback.
- Each canonical story distinguishes requirement intent, prerequisite evidence, acceptance contract, and historical completion evidence.
- Do not mark implementation-ready merely because all FRs are mapped.

After all approved edits and handoff evidence are complete, re-run bmad-check-implementation-readiness. The planning package must not return READY until it has:

1. No critical forward dependency.
2. One navigation model.
3. An operational aging contract or an explicitly approved reduced-state alternative.
4. One NFR registry, recovery vocabulary, localization boundary, and command route.
5. Owned Memories, EventStore, FC-TOK, and CI/test prerequisites.
6. Canonically complete story contracts.
7. 25/25 FR coverage preserved.

## 5. Implementation Handoff

### Scope Classification

**Moderate.** The product scope and epic set remain stable, but four canonical planning families and several dependency/evidence records require coordinated correction.

### Workstreams and Owners

| Workstream | Primary recipient | Proposals |
|---|---|---|
| Product authority | Product Manager / Product Owner | 1, 5, 6, 7, 8, 10, 14, 15 |
| Architecture and shared contracts | System Architect | 2, 3, 9, 10, 12, 14, 15 |
| UX synchronization | UX Designer | 2, 3, 6, 7, 8, 9, 14, 15 |
| Epic/story normalization | Product Manager with Developer and Test Architect | 1, 4, 5, 11, 12, 13 |
| EventStore handoff | Hexalith.EventStore maintainers and Tenants consumer owner | 3 |
| Memories handoff | Hexalith.Memories maintainers and Tenants consumer owner | 4 |
| FrontComposer/token baseline | FrontComposer maintainers with Product/UX/A11y | 14 |
| Readiness validation | PM, Architect, UX, and Test Architect | 16 |

### Implementation Sequence

1. Apply PRD/addendum edits and close the settled questions.
2. Synchronize UX and Architecture from the corrected PRD.
3. Update Epics, story contracts, dependency records, and test-infrastructure gate.
4. Verify or deliver EventStore ProjectedAt, Memories search, FC-TOK fallback, and CI evidence.
5. Update active forward pointers without rewriting historical records.
6. Re-run implementation readiness.

### Constraints

- Do not modify Hexalith.EventStore, Hexalith.Memories, or Hexalith.FrontComposer submodules without explicit task-specific authorization.
- Do not create new backend endpoints for Tenants UI.
- Preserve POST /api/v1/commands and REST-backed Tenants reads.
- Preserve projection-confirmed success, support safety, forward-only correction, and the non-collapse state model.
- Preserve existing unrelated worktree changes.

### Success Criteria

- FR coverage remains 25/25.
- Story 2.4 has no forward dependency and FR-12 proof ownership is explicit.
- All active artifacts describe one Tenants workspace navigation model.
- Aging has an owned, testable projection-time contract.
- NFR, recovery, localization, route, and dependency authorities are singular.
- Memories and test-infrastructure dependencies have owners and evidence.
- Story classifications and Story 3.5 are canonical and complete.
- The next readiness assessment contains no critical dependency or contract-coherence failure.

## 6. Change-Navigation Checklist

- [x] **1.1 Trigger identified:** 2026-07-14 implementation-readiness assessment.
- [x] **1.2 Core problem defined:** contract coherence and executable sequencing, not FR coverage.
- [x] **1.3 Evidence collected:** PRD/addendum, UX, Architecture, Epics, readiness report, project contexts, and epic correction records.
- [x] **2.1 Current epic impact assessed:** all five epics remain historically complete.
- [x] **2.2 Epic-level changes assessed:** direct amendments to Epics 1, 2, 3, and 5; no new epic.
- [x] **2.3 Remaining/future impact assessed:** shared platform and evidence prerequisites are explicitly owned.
- [x] **2.4 Obsolescence assessed:** no epic is obsolete.
- [x] **2.5 Priority/order assessed:** correct authority first, then platform evidence, then readiness.
- [x] **3.1 PRD conflicts assessed:** FR-12, NFRs, vocabulary, placeholder, localization, route, freshness, and FC-TOK.
- [x] **3.2 Architecture conflicts assessed:** navigation, aging, runtime/readiness, versions, and infrastructure.
- [x] **3.3 UX conflicts assessed:** runtime, vocabulary, placeholder, localization, freshness dependency, RTL, and version.
- [x] **3.4 Other artifacts assessed:** Epics, CI/test infrastructure, dependency handoffs, Story 3.5, and historical records.
- [x] **4.1 Direct adjustment evaluated:** viable and selected.
- [x] **4.2 Rollback evaluated:** rejected.
- [x] **4.3 MVP reduction evaluated:** retained only as an unselected freshness fallback.
- [x] **4.4 Recommended path selected:** moderate direct adjustment.
- [x] **5.1 Issue summary created.**
- [x] **5.2 Epic and artifact impacts documented.**
- [x] **5.3 Recommended path documented.**
- [x] **5.4 Detailed edit proposals documented.**
- [x] **5.5 Handoff plan established.**
- [x] **6.1 Checklist reviewed for completeness.**
- [x] **6.2 Proposal reviewed for internal consistency.**
- [x] **6.3 User approval received:** Administrator, 2026-07-14.
- [ ] **6.4 Active planning artifacts and dependency evidence updated:** implementation handoff.
- [ ] **6.5 Readiness re-run completed:** required after implementation.

## 7. Approval

Administrator approved this course correction incrementally on 2026-07-14 and explicitly confirmed final approval on 2026-07-15.

Approval covers the sixteen proposals and the implementation handoff described above. It does not itself authorize edits inside root-declared submodules; those require explicit scoped implementation tasks.
