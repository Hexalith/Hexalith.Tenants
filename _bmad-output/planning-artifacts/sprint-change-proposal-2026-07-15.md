---
title: "Sprint Change Proposal — Readiness Contract and Backlog Correction"
date: 2026-07-15
status: approved-for-implementation
trigger: "_bmad-output/planning-artifacts/implementation-readiness-report-2026-07-15.md"
workflow: bmad-correct-course
mode: incremental-with-standing-approval
scope: moderate-coordinated-correction
incremental_approval: standing-approved
final_approval: approved
approved_by: Administrator
approved_on: 2026-07-15
---

# Sprint Change Proposal — Readiness Contract and Backlog Correction

## Proposal Record

- **Trigger:** the 2026-07-15 Implementation Readiness Assessment.
- **Verdict:** NOT READY.
- **Coverage baseline:** 25/25 functional requirements, 100%.
- **Critical backlog violations:** 2.
- **Consolidated issue themes:** 17.
- **Architecture authority:** AD-1 through AD-14 in architecture.md.
- **Selected path:** direct adjustment with explicit platform prerequisites.
- **Approval state:** Proposals 1 and 2 were individually approved. Administrator then granted standing approval for Proposal 3 and all remaining incremental proposals and approved the compiled proposal on 2026-07-15.
- **Historical handling:** the approved 2026-07-14 proposal and all implementation records remain historical evidence. This proposal supersedes them only where it explicitly says so.
- **Worktree handling:** unrelated root and submodule changes remain untouched.

## 1. Issue Summary

The planning set has complete functional coverage and a coherent architecture spine, but it is not safe to use as an implementation contract. Active PRD, UX, epic, and implementation language still disagrees with AD-1 through AD-14 about read provenance, receipt and preview ownership, command-lock scope, search-cursor security, runtime and orchestration ownership. The backlog also contains one forward epic dependency and one circular correction dependency.

The core problem is contract coherence and executable sequencing, not missing product scope. A developer following the current artifacts could:

1. Treat unknown freshness as current or claim projection confirmation without authoritative provenance.
2. expose a plaintext Memories offset as a cursor;
3. assemble support-sensitive receipt or preview data at the rendered component boundary;
4. implement either a global command lock or an aggregate-scoped lock;
5. complete Story 2.4 only after later Epic 5 work;
6. enable global-administrator correction in Story 5.5 while Story 5.7 says it remains gated;
7. follow stale five-read, Blazor Auto, Fluent RC3, navigation, AppHost, Fluxor, or FR-19 language;
8. treat historical completion commentary as a waiver for unresolved readiness.

This proposal preserves the PRD goals, all 25 FRs, the five user-valued epics, completed implementation evidence, and the canonical architecture. It corrects active contracts and introduces explicit prerequisite work before further feature implementation is called ready.

### Consolidated Theme Map

| # | Issue theme | Resolution |
|---|---|---|
| 1 | Freshness provenance is normalized to Unknown | Proposal 3 |
| 2 | Plaintext, unscoped Memories cursor | Proposal 4 |
| 3 | Client-side receipt and preview assembly | Proposal 5 |
| 4 | Conflicting command-lock scope | Proposal 6 |
| 5 | Story 2.4 depends on Epic 5 | Proposal 1 |
| 6 | Stories 5.5 and 5.7 form a correction cycle | Proposal 2 |
| 7 | Five-read inventory omits Global Administrators | Proposals 3 and 7 |
| 8 | Runtime still names Blazor Auto | Proposal 7 |
| 9 | Fluent package baseline is stale | Proposal 7 |
| 10 | PRD navigation wording conflicts with the one-entry IA | Proposal 7 |
| 11 | Untraced global-search entry point | Proposal 7 |
| 12 | AppHost and production-operations ownership is stale | Proposals 7 and 9 |
| 13 | Epic-local NFR numbering conflicts with the PRD | Proposal 7 |
| 14 | Technical/readiness work appears as user stories | Proposal 8 |
| 15 | Oversized, malformed, or mixed-purpose stories | Proposal 8 |
| 16 | Audit performance and test-tier acceptance are not measurable | Proposal 9 |
| 17 | Normative plans mix current intent with execution history | Proposal 10 |

## 2. Impact Analysis

### Epic Impact

| Epic | Impact | Decision |
|---|---|---|
| Epic 1 — Tenant Workspace Triage and Read-Only Insight | Truthful freshness and secure search paging are not available on the current transport. Story 1.2 is oversized; Stories 1.0 and 1.8 mix enabler/readiness work with user value. | Preserve the user outcome. Split list and search slices, move enabler/certification work outside the user-story list, and block provenance-dependent readiness on explicit work packages. |
| Epic 2 — Tenant Membership and Tenant Record Management | Story 2.4 claims complete FR-12 but defers proof to Epic 5. Command-lock wording also conflicts with AD-12. | Bring minimum support-safe removal proof into Epic 2 as a technical prerequisite and make Story 2.4 independently complete. Apply aggregate-scoped locking. |
| Epic 3 — Tenant Lifecycle and Configuration Control | Story 3.5 is a malformed historical record whose claimed REST conformance is contradicted by the July 15 reality check. Story 3.1 is a readiness shell unless it delivers a separately useful blocked-state surface. | Move Story 3.5 to non-normative history and reopen read conformance through UI-READ-1. Fold Story 3.1 into Story 3.2 unless Product retains it as a separately demonstrable availability outcome. |
| Epic 4 — Global Administrator Governance | Story 4.1 is a readiness gate; Story 4.2 carries the review value. The six-read inventory and freshness prerequisites affect this epic. | Merge valid route, authorization, and fixed-scope criteria into Story 4.2. Keep readiness evidence outside the user-story list. |
| Epic 5 — Audit Evidence and Forward Recovery | Stories 5.5 and 5.7 are cyclic; Stories 5.2, 5.6, and 5.7 are oversized; Story 5.8 is technical cleanup. Audit performance is unmeasurable. | Restrict 5.5–5.6 to tenant recovery, make 5.7 the later global-administrator slice, split oversized outcomes, move cleanup to technical tasks, and add an objective performance decision gate. |

No epic is obsolete and no new product epic is required. The execution order changes because platform and safety prerequisites must precede dependent feature verification.

### Artifact Conflicts

- **PRD/addendum:** stale client-side safety ownership, five-read inventory, Fluent pin, shell-area wording, direct-read availability claims, command-lock wording, open operational questions, and AppHost ownership.
- **Architecture:** AD-1 through AD-14 remain canonical and require no weakening. Its implementation-handoff section gains work-package IDs and acceptance evidence.
- **UX DESIGN/EXPERIENCE:** stale receipt assembly, global command serialization, Blazor Auto, RC3 verification, global-search entry, and historical implementation commentary.
- **Epics/stories:** both dependency violations, stale requirements inventory, counterfeit PRD NFR identifiers, missing dependency metadata, mixed plan/history, ambiguous test tiers, story sizing, and malformed Story 3.5.
- **Secondary artifacts:** sprint-status.yaml after final approval; platform/composing-host dependency records; UI test and CI evidence; project-context only through separately authorized work where it conflicts with AD-13.

### Technical and Operational Impact

The correction introduces work in four ownership domains:

1. EventStore/Tenants read metadata and query provenance.
2. Platform/composing-host service references, orchestration, health, telemetry, and production constraints.
3. Tenants UI BFF query/command separation, safe view models, cursor protection, state, and tests.
4. Product, UX, and backlog normalization.

No submodule implementation is authorized by this proposal. Each shared-platform change requires a separately scoped task and owner.

## 3. Recommended Approach

Use **Direct Adjustment** with explicit prerequisite work packages.

### Option Evaluation

| Option | Viability | Effort | Risk | Decision |
|---|---|---:|---:|---|
| Direct adjustment | Viable | Medium-High | Medium | Selected |
| Roll back completed implementation | Not justified | High | High | Rejected; it does not repair contracts or create provenance. |
| Reduce or redefine the MVP | Not required | Medium | Medium-High | Rejected; it does not resolve security, ownership, or dependency defects. |

The product goals and MVP remain achievable. Timeline impact is a prerequisite correction and verification pass before new dependent stories proceed. Calendar estimates require owner capacity and are not invented here.

### Ordered Path

1. Correct PRD/addendum authority and UX behavior language.
2. Normalize epics, dependencies, work classification, and test ownership.
3. Deliver or verify freshness, service-reference, BFF-read, cursor, and production-host prerequisites.
4. Reverify affected implementation against the corrected story contracts.
5. Re-run Implementation Readiness.

## 4. Detailed Change Proposals

### Proposal 1 — Remove Story 2.4's Epic 5 Dependency

**Status:** Approved individually.

**Affected artifacts:** PRD FR-12 note; Epic 2; Story 2.4; Epic 5 reuse notes.

**OLD**

> Story 2.4 delivers command lifecycle, projection confirmation, and honest audit handoff; Audit Evidence Receipt/proof UX remains Epic 5 unless the evidence source is already implemented.

**NEW**

> Story 2.4 delivers the complete FR-12 vertical slice: fail-closed gating, consequence preview, elevated friction, projection-confirmed removal, audit-availability handling, and minimum support-safe removal proof. It has no dependency on Epic 5.

Add an Epic 2 technical prerequisite work package, outside the user-story list:

**WP-2A — Minimum Removal Audit Proof**

- Consume the existing audit read path; add no receipt endpoint.
- Produce a BFF-assembled, redacted removal-proof view model.
- Support pending, delayed, unavailable, and available audit states without false success.
- Keep raw NarrativePayload, payloads, tokens, internal identifiers, and metadata outside rendered components.
- Provide gateway, component, and integration evidence for confirmed removal through support-safe proof.

Story 2.4 depends only on earlier Epic 1/Epic 2 foundations and WP-2A. Epic 5 generalizes audit browsing and recovery from this foundation; it does not complete Story 2.4 retroactively.

Historical Story 2.4 evidence remains unchanged but must be reverified against the new complete contract.

### Proposal 2 — Break the Story 5.5/5.7 Correction Cycle

**Status:** Approved individually.

**Affected artifact:** epics.md Stories 5.5–5.7.

**OLD**

> Given the correction relates to global administrator authority, Story 5.5 prepares SetGlobalAdministrator or RemoveGlobalAdministrator.

Story 5.7 simultaneously says the global-administrator path remains gated until fixed-scope verification.

**NEW**

- Story 5.5 selects tenant-domain corrections only.
- Story 5.6 previews, submits, confirms, and links tenant-domain corrections only.
- Story 5.7 owns the complete global-administrator correction slice.
- Before Story 5.7, global-administrator correction is unavailable with high-impact flow not ready.
- Story 5.7 depends on Stories 4.2–4.4 and 5.3–5.6; all dependencies point backward.
- Tests prove tenant correction never selects global-administrator commands and global-administrator correction never routes through tenant membership.

### Proposal 3 — Restore Truthful Freshness Provenance

**Status:** Approved under standing approval.

**Affected artifacts:** PRD/addendum, epics.md, architecture implementation handoff, dependency records.

**OLD**

> Conditional Tenants REST reads and in-process query-handler metadata provide the freshness used by the Truth State Badge.

The current UI gateway instead uses the generic EventStore query route, which normalizes provenance to Unknown.

**NEW**

Introduce three ordered work packages:

**PLAT-FRESH-1 — REST Freshness Provenance**

- Propagate ETag, projection version, and read-model freshness through the supported Tenants REST response contract.
- Preserve metadata on 200, 304, empty, and authorization-safe responses.
- Never substitute ServedAt for projection age.

**HOST-REF-1 — Split Service References**

- A platform/composing host exposes separate Tenants-query and EventStore-command references.
- The repository AppHost is not expanded with shared orchestration capability.

**UI-READ-1 — Split BFF Clients**

- Route all six reads directly to Tenants.
- Keep commands and status lookup on the EventStore command client.
- Remove the generic EventStore query route from Tenants UI reads.

The six reads are:

- GET /api/tenants
- GET /api/tenants/{tenantId}
- GET /api/tenants/{tenantId}/users
- GET /api/users/{userId}/tenants
- GET /api/tenants/{tenantId}/audit
- GET /api/global-administrators

Acceptance evidence covers current, stale, and unknown; Refreshing remains client-transient. Aging is not claimed on the wire until authoritative projection-time provenance supports it. Freshness-dependent stories receive blockedBy metadata until the three packages are verified.

### Proposal 4 — Replace the Plaintext Memories Search Cursor

**Status:** Approved under standing approval.

**Affected artifacts:** PRD/addendum cursor mechanics, UX tenant-data-grid behavior, AD-10 handoff, Story 1.2 search slice.

**OLD**

> Search paging returns the next Memories offset.

The current implementation exposes that offset as plaintext and does not bind it to the authenticated user or query scope.

**NEW**

Create **SEARCH-CURSOR-1**:

- Protect the raw Memories offset with the approved server-side cursor codec/DataProtection path.
- Bind the cursor to authenticated user plus normalized query, status, sort, direction, and page-size scope.
- Keep the offset and protected cursor out of visible copy, DOM attributes, logs, telemetry tags, and copy actions.
- On scope mismatch, decoding failure, or invalidation, restart from page 1 and show an honest localized list-refreshed notice.
- Advance the internal offset by raw hits consumed, including dropped malformed, duplicate, unauthorized, or unhydrated hits, as AD-10 requires.
- Preserve non-blocking fallback to the ordinary cursor list when Memories is unavailable.

The whole-set search story remains blocked until security, mismatch, page-1 recovery, and cross-user isolation tests pass.

### Proposal 5 — Move Receipt, Preview, and Rejection Safety to the BFF

**Status:** Approved under standing approval.

**Affected artifacts:** PRD glossary and FR-22, addendum sections C/D, DESIGN.md, EXPERIENCE.md, epics inventory, Stories 2.4, 3.2–3.4, 4.4, 5.3, 5.6, and 5.7.

**OLD**

> The Audit Evidence Receipt is assembled client-side from NarrativePayload.

> The UI assembles consequence previews and receipt/status information from already-loaded fields.

**NEW**

> The server-side BFF assembles and redacts receipt, consequence-preview, and rejection view models. Rendered components receive only support-safe localized fields and never receive raw NarrativePayload, event bodies, command payloads, tokens, internal correlations, ETags, or raw metadata.

Story 5.3 field derivation remains behaviorally the same, but the derivation occurs in the BFF. Component tests assert only safe DTOs cross the render boundary. Gateway and negative tests prove forbidden fields cannot be rendered, copied, announced, logged, or serialized into component state.

No new backend receipt or preview endpoint is added.

### Proposal 6 — Adopt AD-12 Aggregate-Scoped Command Locking

**Status:** Approved under standing approval.

**Affected artifacts:** PRD/addendum FC-CNC language, UX interaction primitives and primary-command-button behavior, epics UX-DR27 and all command-story locking criteria.

**OLD**

> While any command is in flight, every other command trigger in the UI is unavailable.

**NEW**

> Lock scope is (interactive circuit, AggregateIdentity). One command for the same aggregate remains active from submit through accepted/projection-pending until terminal evidence. Unrelated aggregates may proceed. Bulk submission, toast batching, and multiple simultaneous commands for one aggregate remain prohibited.

Tests must prove:

- a second command for the same aggregate is unavailable;
- a command for an unrelated aggregate remains available;
- lock retention continues through accepted and projection-pending;
- release occurs only on terminal evidence;
- reconnect and failure cannot leak or prematurely release a lock.

The historical global one-at-a-time fallback remains evidence of the earlier decision but is superseded by AD-12 in active artifacts.

### Proposal 7 — Synchronize the Canonical Artifact Set

**Status:** Approved under standing approval.

Apply one authority sweep:

1. **Read inventory:** replace five-read language with the six direct reads listed in Proposal 3.
2. **Runtime:** replace Blazor Auto normative wording with InteractiveServer plus server-side BFF. Preserve reconnect and no-optimistic-success invariants.
3. **Fluent baseline:** replace RC3 with centrally consumed 5.0.0-rc.4-26180.1 and require build-time component/icon/ARIA verification.
4. **Information architecture:** one Tenants shell entry at /tenants; Tenants and Users are page-local workspace tabs; Global Administrators and Audit are contextual routes.
5. **Global search:** remove the untraced global-search entry point. Reintroduction requires an explicit requirement and architecture decision.
6. **Orchestration:** the Tenants UI host remains domain-owned; orchestration and shared hosting remain platform/composing-host owned. The repository AppHost is transitional.
7. **State implementation:** typed immutable state is required; Fluxor is not a mandatory architecture constraint.
8. **FR-19:** remove categorically blocked language; retain fixed-scope, freshness, last-administrator, and evidence gates.
9. **Derived quality labels:** keep PRD NFR-1 through NFR-5. Rename epic-local NFR6–NFR10 as:
   - DQR-A11Y
   - DQR-L10N
   - DQR-SAFE
   - DQR-RESP
   - DQR-EVIDENCE
10. **Open questions:** close decisions already settled by AD-1 through AD-14. Retain only explicitly owned product/operations decisions, including freshness tuning, performance budget approval, sensitive configuration, and future RTL/WCAG scope.

### Proposal 8 — Normalize Backlog Structure and Story Size

**Status:** Approved under standing approval.

**Story 1.0**

- Move the completed spike from the user-story sequence to Completed Enabler Evidence.
- Preserve its ID and historical evidence.

**Story 1.2**

- Story 1.2 becomes Tenant Cursor-List Triage.
- Story 1.2a becomes Whole-Set Tenant Search with Authoritative Hydration.
- Story 1.2a depends on Story 1.2, UI-READ-1, and SEARCH-CURSOR-1.
- FR-1 completes through evidence from both slices.

**Story 1.8**

- Keep Support-Safe Identifier Copy as the user story.
- Move Epic 1 readiness certification into an exit gate outside the user-story list.

**Story 2.1**

- Preserve it as a completed historical foundation exception.
- Do not use its breadth as permission for future oversized command stories.

**Story 3.1**

- Merge its availability/guardrail criteria into Story 3.2 unless Product explicitly keeps the blocked-state surface as an independently demonstrable user outcome.

**Story 3.5**

- Remove the malformed completed-story paragraph from the normative epic sequence.
- Preserve its implementation record in non-normative history.
- Mark its prior REST-conformance conclusion reopened by the July 15 evidence and route remediation through UI-READ-1.
- This supersedes the July 14 proposal to restore Story 3.5 as a completed canonical story.

**Story 4.1**

- Merge route, visibility, authorization, and fixed-scope readiness criteria into Story 4.2.
- Keep prerequisite evidence outside the user-story list.

**Story 5.2**

- Split tenant list/detail audit entry, user/member audit entry, and command-result audit entry into independently reviewable slices with explicit return-context tests.
- Remove primary Audit navigation wording.

**Story 5.6**

- Split preview/submission/projection confirmation from bidirectional proof linking.

**Story 5.7**

- Split grant/restore correction from removal correction with last-administrator protection if the complete story cannot satisfy the story-size guardrail.
- Both slices remain after tenant correction foundations and retain fixed-scope tests.

**Story 5.8**

- Move the projection-refresh call-count cleanup to technical tasks under the affected correction story.
- Remove primary FR-24/FR-25 delivery claims.

Add explicit dependsOn and blockedBy fields to every active story. A validation script or test must reject forward dependencies and unknown work-package IDs.

### Proposal 9 — Establish Objective Quality and Production Gates

**Status:** Approved under standing approval.

**Audit performance**

Replace:

> about 500 events without unacceptable degradation

with a blocked decision record requiring Product/Operations to approve, before Story 5.1 is Ready:

- representative 500-event dataset shape;
- page size and filter mix;
- reference environment and network assumptions;
- initial-render and interaction percentile budgets;
- authoritative test tier and repeatability method;
- fallback trigger for stricter paging or virtualization.

No numeric budget is invented by this workflow.

**Test ownership**

- Replace Playwright or component alternatives with one named authoritative tier per acceptance outcome.
- Component tests own deterministic rendering, state, accessibility semantics, and fail-closed behavior.
- Gateway/integration tests own transport, headers, cursor scope, authorization, and persisted/read-model evidence.
- Playwright owns navigational, focus, responsive, and full hosted-flow evidence.
- CI records which lanes are blocking and publishes results.

**Production boundary**

Create **PLATFORM-OPS-1**:

- migrate topology ownership to a platform/composing host;
- consume shared ServiceDefaults, health endpoints, OpenTelemetry, configuration, secrets, and non-root SDK-container defaults;
- keep InteractiveServer at one replica until shared DataProtection, circuit/session routing, and cursor durability are verified;
- record exact evidence before any multi-replica or production-ready claim.

### Proposal 10 — Separate Current Authority from Historical Evidence

**Status:** Approved under standing approval.

- Active PRD/addendum, UX, Architecture, and Epics state current intent and prerequisite status.
- Completed-story evidence, defect history, retrospectives, and prior proposals remain unchanged historical records.
- Active artifacts may link to history but cannot use historical completion as a readiness waiver.
- Every dependency record names producer, consumer, owner, supported contract/version, status, fallback, and acceptance evidence.
- The 2026-07-14 proposal remains approved historical evidence. This proposal supersedes its FR-12 treatment and Story 3.5 restoration where those conflict with the July 15 assessment.
- Canonical edits and sprint-status synchronization occur only after final approval of this compiled proposal.
- Re-run Implementation Readiness after artifact edits and prerequisite scheduling.

## 5. Implementation Handoff

### Scope Classification

**Moderate coordinated correction.** Product scope is stable, but backlog structure, four planning families, and several platform ownership boundaries require Product Owner, Developer, Architect, UX, and Test coordination.

### Recipients and Responsibilities

| Recipient | Responsibility |
|---|---|
| Product Manager / Product Owner | Approve product wording, Story 2.4 proof ownership, story splits, derived quality labels, performance decision owner, and canonical-history separation. |
| Solution Architect | Guard AD-1 through AD-14, define PLAT-FRESH-1, HOST-REF-1, SEARCH-CURSOR-1, and PLATFORM-OPS-1 acceptance evidence. |
| UX Designer | Replace client assembly and global-lock behavior; synchronize runtime, IA, search entry points, Fluent pin, and recovery states. |
| Tenants Developer | Implement or reverify UI-READ-1, safe BFF view models, aggregate locking, cursor integration, and affected story behavior. |
| EventStore/Tenants platform owners | Provide supported REST freshness/ETag/projection metadata without weakening domain boundaries. |
| Platform/composing-host owner | Provide split service references, orchestration migration, health, telemetry, DataProtection/session constraints, and production evidence. |
| Test Architect | Assign authoritative test tiers, define dependency validation, cursor isolation, provenance, support-safety, performance, and hosted-flow evidence. |

### Implementation Sequence

1. Apply active PRD/addendum and UX corrections.
2. Normalize epics, stories, dependency metadata, and historical placement.
3. Schedule PLAT-FRESH-1, HOST-REF-1, UI-READ-1, SEARCH-CURSOR-1, WP-2A, and PLATFORM-OPS-1.
4. Reverify affected completed implementation; do not assume historical status proves the corrected contract.
5. Update sprint-status.yaml to represent approved active work and preserve historical completion records.
6. Run the configured quality evidence.
7. Re-run Implementation Readiness.

### Constraints

- Do not modify root-declared submodules without separately scoped authorization.
- Do not add new Tenants receipt, preview, list-filter, or correction endpoints.
- Preserve POST /api/v1/commands and direct Tenants REST reads.
- Preserve projection-confirmed success, fail-closed behavior, forward-only correction, support safety, and the non-collapse state model.
- Do not expand the repository AppHost with shared platform plumbing.
- Preserve unrelated worktree and submodule changes.

### Success Criteria

- FR coverage remains 25/25.
- No story depends on a later epic or itself through another story.
- Story 2.4 owns complete FR-12 proof without Epic 5.
- Story 5.5 does not claim global-administrator correction.
- All six reads use direct Tenants REST provenance.
- No plaintext or cross-user search cursor exists.
- Receipt, preview, and rejection view models are BFF-assembled and support-safe.
- Command locking follows (circuit, AggregateIdentity) and permits unrelated aggregates.
- Active artifacts agree on InteractiveServer, one Tenants entry, RC4, platform-owned orchestration, and derived quality labels.
- Audit performance has an approved measurable contract.
- Production readiness does not exceed evidence.
- The next readiness assessment finds zero critical dependency violations and one authoritative contract per disputed behavior.

## 6. Change Navigation Checklist

### Section 1 — Trigger and Context

- [N/A] **1.1 Triggering story:** the trigger is the 2026-07-15 readiness assessment rather than one story.
- [x] **1.2 Core problem:** contract incoherence, technical provenance/security limitations, and failed backlog sequencing.
- [x] **1.3 Evidence:** canonical PRD/addendum, epics, AD-1 through AD-14 architecture, UX spines, project contexts, and the July 15 report.

### Section 2 — Epic Impact

- [x] **2.1 Current epic impact:** all five epics assessed.
- [x] **2.2 Epic changes:** scopes retained; prerequisites, dependencies, splits, and work classification corrected.
- [x] **2.3 Remaining epics:** all impacted by shared transport, safety, history, or test evidence.
- [x] **2.4 Obsolescence/new epics:** no product epic is obsolete; technical work packages are required.
- [x] **2.5 Order/priority:** artifact authority first, then prerequisites, revalidation, and readiness.

### Section 3 — Artifact Conflict Analysis

- [x] **3.1 PRD:** product scope remains; mechanics and authority wording require correction.
- [x] **3.2 Architecture:** AD-1 through AD-14 remain canonical; implementation work is scheduled to conform.
- [x] **3.3 UX:** assembly ownership, locking, runtime, pin, search entry, and history require correction.
- [x] **3.4 Other artifacts:** epics, sprint status, dependency records, CI/test ownership, and production evidence are affected.

### Section 4 — Path Forward

- [x] **4.1 Direct adjustment:** viable; Medium-High effort, Medium risk.
- [x] **4.2 Rollback:** not justified; High effort, High risk.
- [x] **4.3 MVP review:** scope reduction not required.
- [x] **4.4 Selected path:** Direct Adjustment with explicit platform prerequisites.

### Section 5 — Proposal Components

- [x] **5.1 Issue summary created.**
- [x] **5.2 Epic and artifact impacts documented.**
- [x] **5.3 Recommended path and alternatives documented.**
- [x] **5.4 MVP impact, dependencies, and action plan documented.**
- [x] **5.5 Agent and owner handoff defined.**

### Section 6 — Final Review and Handoff

- [x] **6.1 Checklist reviewed.**
- [x] **6.2 Proposal consistency reviewed.**
- [x] **6.3 Final user approval:** approved by Administrator on 2026-07-15.
- [!] **6.4 sprint-status.yaml:** pending implementation of the approved canonical story and work-package structure.
- [x] **6.5 Handoff completion:** recipients, responsibilities, sequence, constraints, and success criteria are documented; execution and the readiness rerun remain downstream work.

## 7. Final Approval

Incremental Proposals 1 and 2 were approved individually. Administrator granted standing approval for Proposals 3 through 10.

Administrator approved this compiled Sprint Change Proposal for implementation on 2026-07-15.

Approval authorizes the planning-artifact and sprint-handoff corrections described here. It does not authorize implementation inside root-declared submodules or external platform repositories without a separate scoped task.

### Handoff Record

- **Scope:** Moderate.
- **Primary route:** Product Owner and Developer.
- **Supporting recipients:** Solution Architect, UX Designer, Test Architect, EventStore/Tenants platform owners, and the platform/composing-host owner.
- **Immediate handoff:** apply the approved PRD/addendum, UX, epic/story, dependency-metadata, and sprint-status corrections; schedule the six named prerequisite work packages; then re-run Implementation Readiness.
- **Canonical artifacts changed by this workflow:** none. Their approved edits are implementation-handoff work.
- **Workflow artifacts changed:** this approved proposal and the root workflow execution log.
