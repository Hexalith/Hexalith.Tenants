---
title: 'Sprint Change Proposal - June 5 Implementation Readiness Corrections'
date: '2026-06-05'
project_name: 'Hexalith.Tenants'
author: 'Correct Course workflow (DEV role)'
trigger: 'Implementation Readiness Assessment 2026-06-05 - NEEDS WORK / PLANNING-READY, BUILD-START-GATED'
mode: 'Batch'
scope_classification: 'Moderate (planning artifact reconciliation + backlog/status synchronization)'
status: 'APPROVED AND APPLIED 2026-06-05'
affectedArtifacts:
  - '_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05.md'
  - '_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md'
  - '_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md'
  - '_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md'
  - '_bmad-output/planning-artifacts/epics.md'
  - '_bmad-output/planning-artifacts/architecture.md'
  - '_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md'
  - '_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md'
  - '_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md'
  - 'docs/tenants-ui-frontcomposer-dependency-map.md'
  - 'docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md'
  - 'docs/tenants-ui-phase-2-story-backlog.md'
  - 'docs/tenants-ui-responsive-layout-and-visual-system-spec.md'
  - '_bmad-output/implementation-artifacts/sprint-status.yaml'
---

# Sprint Change Proposal - June 5 Implementation Readiness Corrections

**Date:** 2026-06-05  
**Mode:** Batch  
**Scope:** Moderate  
**Status:** Approved and applied 2026-06-05

## Section 1 - Issue Summary

The June 5 implementation readiness assessment reports the planning set as **NEEDS WORK / PLANNING-READY, BUILD-START-GATED**. It confirms full PRD coverage: all 25/25 functional requirements are represented by epics and stories. The problem is not missing product scope; it is that downstream implementers can still read the planning artifacts in ways that overstate readiness or blur story boundaries.

The readiness report identified five build-start gates:

1. FrontComposer contract gates.
2. Story 1.0 spike boundary.
3. Story 2.4 audit-proof boundary.
4. Large command-story sizing.
5. Build-time UX/accessibility verification.

Additional evidence found during Correct Course:

- `story-1-0-spike-note-2026-06-05.md` is **COMPLETE** and changes the gate state. It confirms `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`; confirms `FC-TBL` as available with caveats; and leaves a scoped `FC-TBL` grid decision before the tenant-list story.
- `sprint-status.yaml` records Story 1.0 as done and Story 1.1 as gate-cleared, but its epic/story IDs do not match the canonical `epics.md` assessed by the readiness report. Example: in `epics.md`, Story 2.4 is "Remove Tenant Member"; in `sprint-status.yaml`, Story 2.4 is "Self-audit My Tenants and look up a user's memberships." This is a real handoff risk.
- `architecture.md`, UX readiness tables, and project docs still contain stale "needs-confirmation" or "not build-ready" wording that was true before the Story 1.0 spike.

## Section 2 - Impact Analysis

### Epic Impact

| Epic | Impact |
| --- | --- |
| Epic 1 - Tenant workspace triage and read-only insight | Story 1.0 is complete and should be marked as a timeboxed enabler, not product value. Story 1.1 may proceed from the FrontComposer gate perspective. Tenant list implementation still needs the `FC-TBL` decision before build-start because FrontComposer's generated grid lacks cursor pagination, column pinning, and six non-collapsing list states. |
| Epic 2 - Tenant membership and tenant record management | Story 2.4 must be narrowed so it delivers command lifecycle, projection confirmation, and honest audit handoff only. It must not claim audit receipt/proof UX before Epic 5 evidence exists. |
| Epic 3 - Tenant lifecycle and configuration control | Command stories remain valid, but they need explicit split triggers if shared command lifecycle, consequence preview, one-at-a-time policy, and truth-state components are not already implemented. |
| Epic 4 - Global administrator governance | Global-admin command stories need the same split trigger and must preserve the platform-wide destructive-action distinction. |
| Epic 5 - Audit evidence and forward recovery | Remains the proper home for receipt/proof UX and compensating recovery. No scope reduction needed. |

### Story Impact

- **Story 1.0:** status/evidence should be recorded as complete; it should not count as user-facing MVP value.
- **Story 1.1:** should cite the completed Story 1.0 spike note as the gate-clearing evidence.
- **Story 1.2 Tenant List Triage:** should add a pre-build `FC-TBL` decision gate.
- **Story 2.4 Remove Tenant Member:** should explicitly separate "projection-confirmed removal" from "audit proof/receipt UX."
- **Command stories 3.2, 3.3, 3.4, 4.4, and 5.6:** should inherit a sizing rule: split if the shared command/preview/truth-state foundations are not already done.

### Artifact Conflicts

- `architecture.md` still says `FC-LYT`, `FC-CMD`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` need confirmation and still says the shell integration spec needs a spike. This conflicts with the completed spike note.
- `EXPERIENCE.md` still says the FC gates are not cleared, and it contains a duplicate `FC-CNS` fallback bullet.
- `docs/tenants-ui-frontcomposer-dependency-map.md` and `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md` still carry pre-spike statuses for FrontComposer dependencies.
- `sprint-status.yaml` conflicts with `epics.md` story IDs and should not be used for story creation until synchronized.

### Technical Impact

No production code exists for the Tenants UI yet. No rollback is needed. The change is planning-only but blocks safe implementation handoff if left unresolved.

## Section 3 - Recommended Approach

**Chosen path: Direct Adjustment.**

Do not change PRD scope and do not roll back. Apply targeted document updates so the planning layer reflects the current gate state and preserves clean handoff boundaries.

Recommended sequence:

1. Reconcile FrontComposer gate language across architecture, UX, docs, and epics using the completed Story 1.0 spike note.
2. Add an explicit `FC-TBL` grid decision gate before tenant-list implementation.
3. Narrow Story 2.4 so it cannot over-claim audit proof before Epic 5.
4. Add a cross-story command sizing/split rule to `epics.md`.
5. Regenerate or repair `sprint-status.yaml` so story IDs match the approved canonical epic file before any next story is created.

**Effort:** Low to medium documentation work, plus PO/Developer coordination for status synchronization.  
**Risk:** Medium if not fixed, because a developer could start the wrong story or implement against stale FrontComposer gates. Low after the proposed edits.  
**Timeline impact:** No MVP scope change. Story 1.1 can proceed after status synchronization. Tenant list implementation waits on the `FC-TBL` decision.

## Section 4 - Detailed Change Proposals

### 4.1 Architecture - update FrontComposer readiness state

Artifact: `_bmad-output/planning-artifacts/architecture.md`

Section: Technical Constraints & Dependencies

OLD:

```text
Readiness: `FC-TBL` available; `FC-LYT`/`FC-CMD`/`FC-A11Y`/`FC-L10N`/`FC-DOC`
needs-confirmation; `FC-CNC`/`FC-TOK`/`FC-AUD`/`FC-CNS` missing. **FC-LYT gates even
the read-only MVP; FC-CMD+FC-CNC gate all commands.**
```

NEW:

```text
Readiness updated by Story 1.0 spike note (2026-06-05): `FC-LYT`, `FC-CMD`,
`FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` are confirmed; `FC-TBL` is available
with caveats; `FC-AUD` and `FC-CNS` remain covered by Product/UX-approved fallbacks;
`FC-TOK` remains a missing shared capability covered by Tenants' canonical vocabulary
and verified Fluent semantic/icon mapping until a shared token contract exists.

`FC-TBL` does not provide cursor pagination, safety-column pinning, or the six
non-collapsing list states required by Tenants. Tenant-list implementation must
record a boundary decision before build-start: either compose a Tenants-specific
`TenantDataGrid` from Fluent/FrontComposer primitives while keeping generic grid
capability in FrontComposer, or move reusable cursor/pinning/list-state support into
FrontComposer before consuming it here.
```

Rationale: prevents implementers from treating already-cleared contracts as blockers, while preserving the new `FC-TBL` caveat.

### 4.2 Architecture - replace stale build-start gap

Artifact: `_bmad-output/planning-artifacts/architecture.md`

Section: Gap Analysis Results

OLD:

```text
- **FrontComposer Shell integration spec** - the exact Shell APIs (`AddHexalithFrontComposer*`,
  manifest registration, projection routing, the FC-TBL contract) need a short verification spike
  against the Shell source before coding.
- **Epics & stories layer absent** - this architecture is one of the two layers the readiness report
  named missing; the other still needs `create-epics-and-stories` (incl. FR-22/24/25 + a bootstrap
  story).
```

NEW:

```text
- **FrontComposer Shell integration spec - CLOSED 2026-06-05.** Story 1.0 is complete;
  see `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`.
  The remaining pre-list implementation item is the `FC-TBL` grid decision.
- **Epics & stories layer - CLOSED.** `epics.md` exists and covers FR1-FR25. The active
  handoff risk is now synchronization: `sprint-status.yaml` must match the canonical
  story IDs before the next story is created.
```

Rationale: removes stale architecture blockers and adds the real handoff risk.

### 4.3 Epics - mark Story 1.0 as a completed enabler spike

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `### Story 1.0: FrontComposer Shell Integration Spike`

ADD after the story heading:

```text
**Story type/status:** Timeboxed enabler spike; completed 2026-06-05. Evidence:
`_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`.
This story closes or records the build-start contract gates; it does not deliver
user-facing MVP value.
```

CHANGE acceptance criterion:

OLD:

```text
**And** it flags `FC-LYT` as blocking read MVP build-start unless confirmed or explicitly approved by Product/UX fallback.
```

NEW:

```text
**And** it records the current gate verdicts. As of the completed 2026-06-05 spike,
`FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` are confirmed;
`FC-TBL` is available with caveats that must be resolved before tenant-list
implementation.
```

Rationale: keeps the spike in the backlog for traceability without pretending it delivers product UI.

### 4.4 Epics - cite Story 1.0 evidence in Story 1.1

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: Story 1.1 acceptance criteria

OLD:

```text
**Given** Story 1.0 has confirmed the minimum FrontComposer shell composition path or recorded an approved fallback
```

NEW:

```text
**Given** Story 1.0 has confirmed the minimum FrontComposer shell composition path in
`story-1-0-spike-note-2026-06-05.md`
```

Rationale: Story 1.1 can now point at concrete evidence instead of an unresolved precondition.

### 4.5 Epics - add an FC-TBL decision gate before tenant-list implementation

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: Story 1.2 Tenant List Triage

ADD before Acceptance Criteria:

```text
**Pre-build gate:** resolve the `FC-TBL` caveat from Story 1.0 before implementation.
FrontComposer's generated projection grid does not satisfy Tenants' cursor pagination,
safety-column pinning, or six-state list-surface requirements. The default decision is
to compose a Tenants-specific `TenantDataGrid` from Fluent/FrontComposer primitives
for tenant-specific columns and safety states, while filing any reusable cursor/pinning/
list-state capability as a FrontComposer enhancement.
```

Rationale: keeps Tenants inside its domain boundary while preventing unsafe use of a generated grid that lacks required behavior.

### 4.6 Epics - narrow Story 2.4 audit-proof boundary

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: Story 2.4

OLD user story:

```text
So that access removal is deliberate, auditable, and never shown as successful before proof exists.
```

NEW user story:

```text
So that access removal is deliberate, projection-confirmed, and never shown as audit-proven before evidence exists.
```

CHANGE audit handoff acceptance criterion:

OLD:

```text
**Given** audit evidence is pending, delayed, unavailable, or missing implementation support
**When** the command reaches a terminal or unverifiable state
**Then** the UI provides the honest audit/evidence handoff state and appropriate recovery action such as wait, retry, inspect audit, continue read-only, or escalate
**And** the original event is not edited, deleted, or rewritten.
```

NEW:

```text
**Given** audit evidence is pending, delayed, unavailable, missing implementation support, or not yet implemented by Epic 5
**When** the command reaches a terminal or unverifiable state
**Then** the UI provides only an honest audit/evidence handoff state and appropriate recovery action such as wait, retry, inspect audit, continue read-only, or escalate
**And** it shows `audit available` or renders an Audit Evidence Receipt only when the Epic 5 evidence source is implemented and available
**And** the original event is not edited, deleted, or rewritten.
```

CHANGE test contract:

OLD:

```text
unit/component tests cover ... projection confirmation, audit unavailable states, and no optimistic removal
```

NEW:

```text
unit/component tests cover ... projection confirmation, audit unavailable states, no optimistic removal,
and the rule that audit proof/receipt UI is not asserted before the Epic 5 evidence source exists
```

Rationale: preserves FR12's audit intent without creating a forward dependency on Epic 5 proof UI.

### 4.7 Epics - add command story split trigger

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: Story Creation Guardrails

ADD:

```text
### Command Story Sizing Guardrail

Command stories may remain single stories only when the shared command lifecycle,
TruthState/vocabulary, one-at-a-time admission, consequence-preview fallback, BFF command
gateway, projection re-query confirmation, localization, and ready-gate evidence patterns
already exist. If those foundations are absent, split the command story into:

1. availability and fail-closed preview,
2. submit/status/projection confirmation,
3. audit/evidence handoff or proof.

Do not split platform-wide destructive actions in a way that bypasses their explicit
blocked/governance status.
```

Rationale: addresses the readiness report's sizing concern without rewriting every command story immediately.

### 4.8 UX - update FrontComposer readiness table and remove duplicate fallback bullet

Artifact: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`

Section: FrontComposer Readiness & Fallbacks

CHANGE the table statuses for `FC-LYT`, `FC-CMD`, `FC-A11Y`, `FC-L10N`, `FC-DOC`, and `FC-CNC` from pre-spike values to "confirmed by Story 1.0 spike note, 2026-06-05."

CHANGE `FC-TBL` behavior:

```text
Backbone candidate for read surfaces; available with caveats. Generated projection
DataGrid does not satisfy Tenants cursor pagination, safety-column pinning, or six
non-collapsing list states. Tenant-list implementation must use the approved FC-TBL
decision from Story 1.2.
```

REMOVE the duplicate `FC-CNS -> inline consequence text` fallback bullet so the approved fallback list contains exactly three unique entries.

Rationale: prevents UX from contradicting the completed spike and removes a duplicate introduced in prior reconciliation.

### 4.9 Project docs - update dependency statuses or mark as superseded by spike note

Artifacts:

- `docs/tenants-ui-frontcomposer-dependency-map.md`
- `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md`

CHANGE every affected pre-spike status row so it either:

- cites the Story 1.0 spike note and marks the dependency confirmed, or
- states that older `needs-confirmation` rows are superseded by `story-1-0-spike-note-2026-06-05.md`.

Keep build-time verification requirements in place. Confirmation of `FC-A11Y`, `FC-L10N`, and `FC-DOC` does not waive per-story keyboard, focus, screen-reader, forced-colors, localization, and documentation evidence.

Rationale: removes stale planning blockers while preserving the ready-gate evidence burden.

### 4.10 Sprint status - synchronize before next story creation

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

CURRENT CONFLICT:

```text
epics.md Story 2.4 = Remove Tenant Member with Consequence Preview
sprint-status.yaml Story 2.4 = Self-audit My Tenants and look up a user's memberships
```

PROPOSED ACTION:

```text
Before creating or implementing the next story, regenerate or repair sprint-status.yaml
from the approved canonical epics.md. Preserve Story 1.0 as done and preserve its
gate-clearing note, but do not keep story IDs that conflict with epics.md.
```

Rationale: prevents a developer from implementing the wrong story under the right-looking ID.

## Section 5 - Implementation Handoff

**Scope classification:** Moderate.

| Recipient | Responsibility |
| --- | --- |
| Product Owner / PM | Approve this proposal; confirm `epics.md` remains the canonical story source; approve sprint-status regeneration or repair. |
| Developer agent | Apply the approved document edits; then synchronize `sprint-status.yaml`; then proceed to Story 1.1 only after status IDs align. |
| FrontComposer/Tenants UI implementer | Use the completed Story 1.0 spike as gate evidence; make the `FC-TBL` grid decision before tenant-list implementation. |
| UX/a11y owner | Preserve per-story ready-gate evidence requirements even though `FC-A11Y`, `FC-L10N`, and `FC-DOC` are now confirmed as shell capabilities. |

Success criteria:

1. No canonical planning artifact still says `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, or `FC-DOC` are unresolved without referencing the completed Story 1.0 spike.
2. Story 1.0 is recorded as a completed enabler spike, not MVP product value.
3. Story 2.4 cannot be read as delivering audit receipt/proof UX before Epic 5.
4. Large command stories have an explicit split trigger.
5. `sprint-status.yaml` story IDs match the approved canonical `epics.md`.
6. UX/a11y build-time verification remains required per UI story.

## Checklist Status

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story | [x] Done | Trigger is readiness report, with Story 1.0 and Story 2.4 as affected stories. |
| 1.2 Core problem | [x] Done | Planning handoff and gate-state mismatch, not missing FR scope. |
| 1.3 Evidence | [x] Done | Readiness report, spike note, architecture/UX/docs searches, sprint-status mismatch. |
| 2.1 Current epic | [x] Done | Epic 1 can proceed to Story 1.1 only after status synchronization; tenant list needs FC-TBL decision. |
| 2.2 Epic changes | [x] Done | No new epics; targeted story/boundary changes. |
| 2.3 Remaining epics | [x] Done | Epics 2-5 affected by audit and command sizing clarifications. |
| 2.4 New/obsolete epics | [N/A] Skip | No epic added or removed. |
| 2.5 Priority/order | [x] Done | Story 1.1 remains next after status sync; FC-TBL decision before tenant list. |
| 3.1 PRD conflicts | [x] Done | PRD/addendum updated with post-Story-1.0 readiness state; MVP scope unchanged. |
| 3.2 Architecture conflicts | [!] Action-needed | Architecture has stale pre-spike gate and absent-epics text. |
| 3.3 UX conflicts | [!] Action-needed | UX readiness table stale; duplicate FC-CNS bullet. |
| 3.4 Other artifacts | [!] Action-needed | Docs and sprint-status need synchronization. |
| 4.1 Direct adjustment | [x] Viable | Recommended. |
| 4.2 Rollback | [N/A] Not viable | No production implementation to roll back. |
| 4.3 MVP review | [N/A] Not viable | MVP scope remains coherent. |
| 4.4 Recommended path | [x] Done | Direct Adjustment. |
| 5.1 Issue summary | [x] Done | Included. |
| 5.2 Impact and adjustments | [x] Done | Included. |
| 5.3 Path rationale | [x] Done | Included. |
| 5.4 MVP/action plan | [x] Done | MVP unchanged; action plan included. |
| 5.5 Handoff | [x] Done | Included. |
| 6.1 Checklist review | [x] Done | Completed with open action-needed items. |
| 6.2 Proposal accuracy | [x] Done | Drafted from current artifacts. |
| 6.3 User approval | [x] Done | User approved with `yes` on 2026-06-05. |
| 6.4 Sprint status update | [x] Done | `sprint-status.yaml` synchronized to canonical `epics.md`; Story 1.0 preserved as done. |
| 6.5 Next steps | [x] Done | Route to Developer/PO: Story 1.1 can proceed after using the synchronized status; Story 1.2 requires the FC-TBL decision. |

## Approval Request

Approved by user on 2026-06-05 and applied.

## Application Log

Applied on 2026-06-05:

- Updated `architecture.md` to mark Story 1.0 FrontComposer contract confirmation as closed and to carry the remaining `FC-TBL` grid decision.
- Updated PRD `prd.md` and `addendum.md` with the post-Story-1.0 readiness state while preserving MVP scope.
- Updated `epics.md` to mark Story 1.0 as a completed enabler spike, cite the spike evidence in Story 1.1, add the Story 1.2 `FC-TBL` pre-build gate, narrow Story 2.4's audit-proof boundary, and add the command-story sizing guardrail.
- Updated `EXPERIENCE.md` and `DESIGN.md` to reflect post-spike FrontComposer readiness and remove stale build-blocker language while keeping per-story evidence gates.
- Added a supersession note to `fallback-approval-record-2026-06-03.md`.
- Added supersession notes to `docs/tenants-ui-frontcomposer-dependency-map.md`, `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md`, `docs/tenants-ui-phase-2-story-backlog.md`, and `docs/tenants-ui-responsive-layout-and-visual-system-spec.md`.
- Synchronized `sprint-status.yaml` to the canonical story IDs from `epics.md`, preserving Story 1.0 as `done`.
