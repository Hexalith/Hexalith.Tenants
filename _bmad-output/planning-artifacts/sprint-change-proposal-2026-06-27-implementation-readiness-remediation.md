# Sprint Change Proposal - 2026-06-27 - Implementation Readiness Remediation

Approval: approved by Administrator on 2026-06-27.

Implementation status: applied on 2026-06-27.

Mode: batch.

Trigger: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-27.md` returned **NEEDS WORK** with three major action items:

1. Complete Story 1.2 FR1 Memories-backed search acceptance criteria.
2. Resolve/synchronize the Story 1.2 `FC-TBL` handoff state.
3. Right-size Story 2.1 or cite command-foundation evidence.

This proposal intentionally uses a suffixed filename because `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-27.md` already exists for the approved Tenants module tabbed workspace change.

## 1. Issue Summary

The readiness report found that the current planning package is mostly coherent but not synchronized enough for clean implementation handoff. The problem is not that the implementation records lack evidence. The repository already contains completed implementation artifacts for the affected areas:

- Story 1.2 is marked `done` and records the Tenants-specific `TenantDataGrid` decision in `_bmad-output/implementation-artifacts/1-2-tenant-list-triage.md`.
- Correct Course story `cc-2026-06-21-memories-backed-tenant-search` is marked `done` and records Memories-backed cross-set tenant search, index-only matching, ETag/fresh hydration, fallback behavior, and handoff state in `_bmad-output/implementation-artifacts/cc-2026-06-21-memories-backed-tenant-search.md`.
- Story 2.1 is marked `done` and records the Tenants command gateway, lifecycle model, one-at-a-time policy, projection re-query confirmation, localization, accessibility, and test evidence in `_bmad-output/implementation-artifacts/2-1-create-tenant-with-projection-confirmed-command-lifecycle.md`.
- `sprint-status.yaml` marks Epic 1, Epic 2, Story 1.2, Story 2.1, and the 2026-06-21 Memories-backed search Correct Course as `done`.

The remaining issue is documentation drift: `epics.md`, PRD readiness notes, UX readiness notes, and the FrontComposer dependency map still contain stale pre-build gate wording or generic Story 1.2/2.1 wording that does not cite the completed implementation evidence.

## 2. Change Analysis Checklist Results

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [x] | The trigger is the 2026-06-27 implementation readiness report, specifically Story 1.2 and Story 2.1 readiness findings. |
| 1.2 Core problem | [x] | Artifact synchronization issue. Implemented/evidenced decisions are not fully reflected in planning handoff documents. |
| 1.3 Evidence | [x] | Evidence exists in Story 1.2, Story 2.1, `cc-2026-06-21-memories-backed-tenant-search`, architecture, PRD FR1, and sprint status. |
| 2.1 Current epic impact | [x] | Epic 1 and Epic 2 remain valid. No epic scope reduction is needed. |
| 2.2 Epic-level changes | [x] | Update Epic 1 Story 1.2 and Epic 2 Story 2.1 readiness wording. No new epic required. |
| 2.3 Remaining epics | [x] | Epics 3-5 are not structurally affected. They benefit from the Story 2.1 command foundation citation. |
| 2.4 Future epic invalidation | [N/A] | No future epic is invalidated. |
| 2.5 Epic order/priority | [N/A] | No resequencing needed. |
| 3.1 PRD conflicts | [x] | PRD FR1 already has the Memories search consequences, but PRD readiness notes still say `FC-TBL` remains unresolved. |
| 3.2 Architecture conflicts | [x] | Architecture mostly records the correct state but still has a few stale summary/action lines saying `FC-TBL` remains a decision item. |
| 3.3 UI/UX conflicts | [x] | UX and dependency-map readiness text still says the Story 1.2 `FC-TBL` decision is pending. UX also does not explicitly name the Memories-unavailable search notice. |
| 3.4 Secondary artifacts | [x] | `sprint-status.yaml` build-readiness note still lists Story 1.2 and command-story split as remaining planning gates. |
| 4.1 Direct adjustment | [x] | Viable. Patch planning/readiness text and cite implementation evidence. Effort low. Risk low. |
| 4.2 Potential rollback | [N/A] | Rollback is not useful. The implementation evidence is the asset to preserve. |
| 4.3 PRD MVP review | [N/A] | MVP scope remains unchanged. |
| 4.4 Recommended path | [x] | Direct documentation synchronization. |
| 5.1 Issue summary | [x] | Documented above. |
| 5.2 Epic/artifact needs | [x] | Detailed below. |
| 5.3 Recommendation | [x] | Update planning artifacts; do not split implemented Story 2.1 retroactively. |
| 5.4 MVP impact/action plan | [x] | No MVP scope change. The action plan is artifact patching plus approval. |
| 5.5 Handoff plan | [x] | Minor-scope Developer handoff. |
| 6.1 Checklist completion | [x] | Applicable checklist sections completed. |
| 6.2 Proposal accuracy | [x] | Based on the readiness report, current planning artifacts, implementation artifacts, and sprint status. |
| 6.3 User approval | [x] | Approved by Administrator on 2026-06-27. |
| 6.4 Sprint status update | [x] | Applied to `_bmad-output/implementation-artifacts/sprint-status.yaml`. |
| 6.5 Next steps/handoff | [x] | Defined below. |

## 3. Impact Analysis

### Epic Impact

Epic 1 remains valid. Story 1.2 needs its acceptance criteria and test contract updated to make the PRD FR1 Memories-backed search behavior explicit in the epic source, not only in PRD FR1 and the Correct Course implementation artifact.

Epic 2 remains valid. Story 2.1 does not need to be split retroactively because it has already implemented and tested the command foundation. The planning artifact should cite that evidence so future readiness checks do not treat Story 2.1 as an oversized unproven story.

### Story Impact

Story 1.2:

- Add explicit Memories-backed whole-set search acceptance criteria.
- State that non-empty search terms round-trip to the server and empty/whitespace terms use the cursor list path.
- State that Memories supplies only the match-set, and the BFF hydrates rows through authoritative ETag/freshness detail reads.
- State that status filtering is exact, search index lag is accepted, and Memories-unavailable fallback is non-blocking.
- Update the test contract accordingly.
- Change `FC-TBL` from pre-build gate to resolved boundary decision.

Story 2.1:

- Add a foundation evidence note instead of splitting.
- Cite Story 1.0 for `FC-CMD`/`FC-CNC`, Story 2.1 for command gateway/lifecycle/projection confirmation, and UI test evidence.
- Preserve the rule that future command stories must split if they lack equivalent foundation evidence.

### Artifact Conflicts

Planning artifact drift exists in:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`
- `docs/tenants-ui-frontcomposer-dependency-map.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- A few architecture summary lines in `_bmad-output/planning-artifacts/architecture.md`

### Technical Impact

No product code change is proposed by this Sprint Change Proposal. The technical impact is keeping implementation handoff documents honest so Developer agents do not re-open completed gates or split completed stories unnecessarily.

## 4. Recommended Approach

Recommended path: **Direct Adjustment**.

Rationale:

- The readiness problems are drift between planning artifacts and completed evidence, not missing implementation.
- Splitting Story 2.1 after completion would damage traceability and duplicate already-tested work.
- Keeping the correction documentation-only avoids adding generic grid or command infrastructure to Tenants.
- The existing domain boundaries remain intact: reusable grid improvements stay in FrontComposer, Memories owns search indexing/search, EventStore owns command submission/status, and Tenants owns tenant-specific UI composition and domain flows.

Effort: Low.

Risk: Low.

Timeline impact: One documentation patch plus review.

## 5. Detailed Change Proposals

### Proposal A: Patch Story 1.2 Search Acceptance Criteria

Artifact: `_bmad-output/planning-artifacts/epics.md`

Story: `Story 1.2: Tenant List Triage`

Section: Acceptance Criteria

OLD:

```md
**Given** the operator searches, filters, sorts, or cursor-pages the tenant list
**When** new results are loaded
**Then** the list preserves distinct pending and stale markers on every affected row
**And** sorting or paging never hides safety-critical columns or truth-state indicators.
```

NEW:

```md
**Given** the operator enters a non-empty tenant search term
**When** results are loaded
**Then** the BFF performs a server-side search through `Hexalith.Memories` syntactic/BM25 search against the dedicated `tenants-index`
**And** the search matches tenants by Name or TenantId across the whole visible tenant set, not only the currently loaded page
**And** the browser does not perform direct backend calls or page-local-only filtering for the search result set.

**Given** Memories returns a search match-set
**When** tenant rows are rendered
**Then** Memories determines only which tenant ids are candidates
**And** each row is hydrated through the existing authoritative ETag/freshness tenant read path before display
**And** stale index data never supplies row status, name, counts, pending state, or freshness.

**Given** the operator combines search with status filtering
**When** the filter is applied
**Then** status filtering is exact, not BM25 text matching
**And** it uses a structured Memories attribute filter when available or the hydrated authoritative status as the interim bridge.

**Given** the search term is empty or whitespace
**When** results are loaded
**Then** the list uses the unchanged cursor-list path and does not call Memories.

**Given** Memories is unavailable, degraded, or cannot serve the syntactic axis
**When** the list reloads
**Then** the UI falls back to the cursor list with a non-blocking search-unavailable notice
**And** no token, ETag, correlation id, raw payload, stack trace, or backend diagnostic detail is rendered.

**Given** search indexing lags behind tenant creation or rename
**When** the operator searches
**Then** eventual consistency is surfaced honestly
**And** hydrated rows always show authoritative current data for any returned match.

**Given** the operator searches, filters, sorts, or cursor-pages the tenant list
**When** new results are loaded
**Then** the list preserves distinct pending and stale markers on every affected row
**And** sorting or paging never hides safety-critical columns or truth-state indicators.
```

Rationale: Makes Story 1.2 detail-level coverage match PRD FR1 and the completed 2026-06-21 Memories-backed search story.

### Proposal B: Patch Story 1.2 Test Contract

Artifact: `_bmad-output/planning-artifacts/epics.md`

Story: `Story 1.2: Tenant List Triage`

Section: Test Contract

OLD:

```md
**Given** this story is complete
**When** verification is run
**Then** unit or component tests cover the BFF query gateway mapping, cursor/freshness states, authorization-safe empty states, and no-direct-browser-backend behavior
**And** bUnit or Playwright coverage verifies loading, empty, filtered-empty, stale, degraded, error, keyboard navigation, forced-colors-safe status rendering, and stable selectors.
```

NEW:

```md
**Given** this story is complete
**When** verification is run
**Then** unit or component tests cover BFF query gateway mapping, cursor/freshness states, authorization-safe empty states, and no-direct-browser-backend behavior
**And** gateway tests cover non-empty search term to Memories match-set, TenantId recovery from the search hit, authoritative row hydration, exact status filtering, empty-search bypass, Memories-unavailable fallback, dropped not-found/forbidden match ids, and support-safe degraded copy
**And** bUnit or Playwright coverage verifies search round-trip, loading, empty, filtered-empty, stale, degraded, error, keyboard navigation, forced-colors-safe status rendering, stable selectors, and no page-local-only search result filtering.
```

Rationale: Prevents future readiness checks from treating Memories search as uncited acceptance evidence.

### Proposal C: Synchronize `FC-TBL` Handoff State

Artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`
- `docs/tenants-ui-frontcomposer-dependency-map.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD examples:

```md
tenant-list implementation still requires the Story 1.2 `FC-TBL` decision.
```

```md
Tenant-list implementation still waits on the Story 1.2 `FC-TBL` decision.
```

```md
Story 1.2 must record the grid boundary decision before tenant-list implementation.
```

NEW:

```md
The Story 1.2 `FC-TBL` boundary decision is resolved. Tenants uses a Tenants-specific `TenantDataGrid` composed from Fluent UI Blazor and available FrontComposer primitives for tenant-specific columns, cursor handling, safety-column preservation, and six non-collapsing list states. Reusable cursor pagination, safety-column pinning, and list-state capability remain FrontComposer-owned enhancement work, not Tenants-local generic infrastructure.
```

Rationale: Architecture already records this decision as closed. PRD, UX, dependency-map, epics, and sprint status should match it.

### Proposal D: Add Story 2.1 Foundation Evidence Instead of Splitting

Artifact: `_bmad-output/planning-artifacts/epics.md`

Story: `Story 2.1: Create Tenant with Projection-Confirmed Command Lifecycle`

Section: Add immediately after `Requirements`.

OLD:

```md
**Requirements:** FR13; NFR2-NFR8, NFR10; Additional command confirmation requirements; UX-DR3, UX-DR11-UX-DR13, UX-DR22-UX-DR28, UX-DR33.
```

NEW:

```md
**Requirements:** FR13; NFR2-NFR8, NFR10; Additional command confirmation requirements; UX-DR3, UX-DR11-UX-DR13, UX-DR22-UX-DR28, UX-DR33.

**Command-foundation evidence:** Story 2.1 is not an unproven oversized first-command story. Story 1.0 confirmed `FC-CMD` and `FC-CNC`; Story 2.1 implementation added the Tenants command gateway, create-tenant lifecycle state, one-at-a-time admission, projection re-query confirmation, safe rejection mapping, audit handoff states, EN/FR resources, accessibility/focus/live-region evidence, and UI/gateway/state tests. Evidence is recorded in `_bmad-output/implementation-artifacts/2-1-create-tenant-with-projection-confirmed-command-lifecycle.md` and `tests/test-summary.md`. Future command stories may remain single stories only when they cite equivalent foundation evidence; otherwise they must split.
```

Rationale: The readiness report's risk is valid for an unimplemented Story 2.1, but the current repository has completed implementation evidence. The correct correction is citation, not retroactive story splitting.

### Proposal E: Update Sprint Status Build-Readiness Note

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Section: `BUILD-READINESS NOTE`

OLD:

```yaml
# Remaining planning gates:
#   - Story 1.2 tenant list: resolve FC-TBL caveat before implementation.
#   - Command stories: split if shared command/truth/preview foundations do not exist.
```

NEW:

```yaml
# Resolved planning gates:
#   - Story 1.2 tenant list: FC-TBL boundary decision is resolved by Tenants-specific
#     TenantDataGrid composition; reusable grid capability remains a FrontComposer
#     enhancement/handoff, not Tenants-local generic infrastructure.
#   - Story 1.2 FR1 search: Memories-backed cross-set search is covered by
#     cc-2026-06-21-memories-backed-tenant-search and must stay synchronized in epics/PRD/UX.
#   - Story 2.1 first command flow: command foundation evidence is recorded in the
#     Story 2.1 artifact; future command stories still split if equivalent foundation
#     evidence is absent.
```

Rationale: Sprint status should not keep stale blockers for stories already marked `done`.

### Proposal F: PRD Readiness Note Synchronization

Artifact: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md`

Sections: post-readiness update, Why Now, readiness risk lines, build-readiness status, open questions.

OLD examples:

```md
Remaining planning gates are the `FC-TBL` tenant-list grid decision...
```

```md
Tenant-list implementation still requires the `FC-TBL` decision...
```

NEW:

```md
The `FC-TBL` tenant-list grid decision is resolved by Story 1.2: Tenants uses a Tenants-specific `TenantDataGrid`, and reusable grid capability remains a FrontComposer enhancement. Remaining gates are per-story accessibility/localization/responsive/documentation evidence, deferred numerics/freshness thresholds, `FC-TOK` fallback discipline, and audit/proof evidence readiness where applicable.
```

Rationale: PRD FR1 already contains the Memories search contract. The stale PRD issue is only readiness status wording.

### Proposal G: UX Search-Degraded State Note

Artifact: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`

Section: tenant-data-grid / FrontComposer readiness.

OLD:

```md
| **tenant-data-grid** | Cursor pagination only (never offset/limit). Required columns: tenant identity, status, member count, owner count, pending state, truth-state-badge with freshness. Renders the **six non-collapsible list-surface states**. **Sorting/paging must never hide a pending or stale marker** (GAP-4). **Row actions stay stable in width and placement** under data/sort/page change (GAP-9). On cursor invalidation, re-query page 1 with an honest "list refreshed" notice (not an error). |
```

NEW:

```md
| **tenant-data-grid** | Cursor pagination only (never offset/limit). Required columns: tenant identity, status, member count, owner count, pending state, truth-state-badge with freshness. Search is server-side for non-empty terms: Memories supplies the match-set and the BFF hydrates authoritative rows. Renders the **six non-collapsible list-surface states** plus a non-blocking search-unavailable notice when Memories is degraded and the cursor list fallback is shown. **Sorting/paging must never hide a pending or stale marker** (GAP-4). **Row actions stay stable in width and placement** under data/sort/page change (GAP-9). On cursor invalidation, re-query page 1 with an honest "list refreshed" notice (not an error). |
```

Rationale: UX behavior should explicitly cover the Memories-unavailable notice and fallback.

## 6. Implementation Handoff

Scope classification: **Minor**.

Route to: Developer agent.

Deliverables:

- Patch the planning/readiness artifacts named in Section 5.
- Do not change product code for this correction.
- Do not modify submodule files.
- Do not add generic grid, command, search, or persistence infrastructure in Tenants.
- Preserve the already approved `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-27.md` tabbed workspace proposal.

Success criteria:

1. `implementation-readiness-report-2026-06-27.md` action items are addressed by explicit artifact updates or evidence citations.
2. `epics.md` Story 1.2 includes Memories-backed FR1 search acceptance criteria and test contract details.
3. `FC-TBL` is consistently described as resolved for Tenants by the Story 1.2 `TenantDataGrid` decision.
4. Story 2.1 is documented as completed with foundation evidence, not as a story requiring retroactive split.
5. `sprint-status.yaml` no longer lists resolved Story 1.2 and Story 2.1 gates as remaining planning gates.

## 7. Approval and Implementation Outcome

Approved by Administrator on 2026-06-27 and implemented as a documentation synchronization change.

Updated artifacts:

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md`
- `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `docs/tenants-ui-frontcomposer-dependency-map.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

No product code or submodule files were changed by this correction.
