# Sprint Change Proposal — 2026-07-19 v2 (Readiness Polish)

**Trigger:** Post-SCP Implementation Readiness Assessment v2 (`implementation-readiness-report-2026-07-19-v2.md`) — READY with 0 Critical, 0 Major, 10 Minor epic-quality findings, and 3 minor cross-document observations  
**Mode:** Batch review (working assumption; Administrator may switch to Incremental before approval)  
**Scope classification:** Minor — documentation-only direct adjustment  
**Status:** APPROVED AND APPLIED — explicitly approved by Administrator and implemented on 2026-07-19  
**Preservation note:** This file uses the `-v2` suffix because `sprint-change-proposal-2026-07-19.md` is the approved and applied Major-remediation proposal. The 16:45 readiness baseline, the post-SCP v2 readiness report, and the approved proposal remain untouched.

## 1. Issue Summary

The post-SCP readiness rerun verified that all five Major findings from the 16:45 baseline are fixed and that the planning stack is implementation-ready. It also carried forward ten non-blocking epic-quality polish findings and recorded three cosmetic cross-document observations:

1. NFR10 documentation/reference evidence is not explicitly incorporated into ready/closing gates across Epics 2–5.
2. Story 1.0 frames the work around a maintainer rather than the operator outcome being protected.
3. Story 1.10 appears after read-surface consumers without explicitly saying that it corrects their transport/provenance.
4. Story 1.11 uses historical Story 4.1/4.2 numbers that collide visually with the current Epic 4.
5. Story 2.2 can make `already applied` look like a valid response to a pre-existing member even though `UserAlreadyInTenant` is a rejection.
6. Story 2.4 names `restore intended access` without stating that the recovery appears only when the later correction capability is available.
7. Story 2.4 is a deliberately large vertical slice, but its conditional implementation split is undocumented.
8. Story 3.1 omits the first-tenant bootstrap exception in which an empty list with `unknown` freshness remains creatable and `TenantAlreadyExists` is the collision backstop.
9. Story 5.2 does not explicitly say it consumes only shared canonical audit labels rather than Story 5.4's later recovery-detail UI.
10. Epic 5 retains visually dangling historical Story 5.8 references and does not record the deliberate sizing posture for Story 5.7 versus Stories 5.5–5.6.
11. PRD §16.4 still calls localization resource ownership open although architecture D4 resolved it.
12. Architecture's historical Project Context Analysis still says `~500-event audit target` and `Blazor Auto reconnect` despite the §16.14 decision gate and InteractiveServer decision.
13. `DESIGN.md` labels the icon table “Verified” although its names were verified against rc.3 and the current pin is rc.4; the existing build-time reverification obligation is correct but the heading overstates current evidence.

During source checking, the focused FR-13 consistency review exposed one already-documented downstream residual not counted among the readiness report's 13 items: Story 3.1 still uses `GlobalAdminRequired` shorthand. The focused review explicitly routed that wording to the next workflow authorized to modify `epics.md`. This proposal folds the correction into item 8: unauthorized creation emits `InsufficientPermissionsRejection`, maps to safe `InsufficientPermissions`, and remains reflected as `missing permission`.

Evidence:

- The v2 readiness report verifies 25/25 FR coverage, 443/443 Given/When/Then integrity, no forward dependencies, and no Critical or Major finding.
- PRD §9 and epics NFR10 already define the required documentation/reference evidence; this proposal makes that gate explicitly authoritative rather than duplicating it across twenty closing ACs.
- Architecture D4 already assigns Tenants-owned whole-string domain resources and FrontComposer-owned shell chrome.
- `review-fr13-consistency.md` records the Story 3.1 downstream vocabulary residual and the observable domain/UI contract.
- The first-tenant bootstrap behavior is already implemented and covered by `TenantsWorkspaceTests.Workspace_allows_create_flow_when_list_freshness_is_unknown`; the story text is the missing layer.

## 2. Impact Analysis

### Epic and story impact

- **All five epics remain valid and independently completable.** No epic scope, order, priority, or completion boundary changes.
- **Stories touched:** 1.0, 1.10, 1.11, 2.2, 2.4, 3.1, 5.2, 5.6, and 5.7, plus shared evidence/sizing notes in `epics.md`.
- **No story is added, removed, renumbered, reopened, or moved.** Conditional 2.4a/2.4b and 5.7a/5.7b labels are delivery-task slices only if iteration planning requires them; they are not new canonical story IDs.
- **Story 5.1 remains not Ready** until the Product/Operations §16.14 audit-performance decision record is approved. This proposal neither resolves nor weakens that gate.

### Artifact impact

- **`epics.md`:** ten polish dispositions plus the FR-13 observable-contract rider.
- **PRD `prd.md`:** resolve open question §16.4 and synchronize the assumptions index.
- **`architecture.md`:** correct two superseded phrases in the non-authoritative historical analysis; AD-1..AD-14 are unchanged.
- **UX `DESIGN.md`:** make the icon-set verification status honest for the rc.4 pin; icon bindings are unchanged and still require Story 1.0 build evidence.
- **Architecture/UX behavioral contracts:** no change.
- **`sprint-status.yaml`:** N/A — no canonical epic/story ID or count changes.
- **Readiness reports and prior proposal:** preserved unchanged as historical evidence.

### Technical and schedule impact

- No code, API, domain contract, data, infrastructure, deployment, dependency, or test implementation change is authorized by this proposal.
- Effort is **Low**: one bounded documentation pass followed by structural/text validation.
- Risk is **Low**: edits clarify already-ratified rules, current implementation behavior, and historical numbering. The only contract-vocabulary edit moves Story 3.1 toward verified domain truth.
- Timeline impact is **none expected**. The external §I work packages and §16.14 decision remain the actual schedule constraints.

## 3. Recommended Approach

Select **Option 1 — Direct Adjustment**.

- It closes every finding through localized text or an explicit disposition inside the current plan.
- A shared authoritative NFR10 gate avoids repetitive edits across twenty story closers while preserving per-story evidence responsibility.
- Conditional delivery-task splits document sizing risk without manufacturing new story IDs or partial completion semantics.
- Rollback is not applicable: the Major-remediation proposal is correct and remains applied.
- MVP review is not applicable: requirements, coverage, phasing, and implementation readiness remain unchanged.

## 4. Detailed Change Proposals

### A. `epics.md`

#### CC-1 — Make the shared NFR10 evidence gate authoritative

**Finding:** Epic-quality Minor 1.

**INSERT immediately after NFR10:**

> **Shared NFR10 evidence gate (authoritative for Stories 1.0–5.7):** Every story's ready and completion decision incorporates NFR10 together with PRD §9 and UX-DR33. A story's closing focused-check AC lists its additional story-specific lanes; it does not replace or narrow this shared gate. Evidence must cite the applicable accessibility, localization, responsive, documentation/reference (`FC-DOC` or the exact approved fallback), and focused-test sources/versions/results, or record the Product/UX-approved row-specific fallback with behavior, copy owner, documentation evidence, replacement path, and owner approval.

**Rationale:** This uses the readiness report's allowed “declare a shared checklist authoritative” disposition and avoids twenty mechanically duplicated AC lines.

#### CC-2 — Story 1.0: state the protected operator outcome

**Finding:** Epic-quality Minor 2.

**OLD:**

> As a Tenants UI maintainer,  
> I want the shared FrontComposer and Fluent contracts reverified against the corrected architecture and pinned dependencies,  
> So that subsequent stories build on demonstrated capabilities and honest fallback boundaries.

**NEW:**

> As a Tenants UI maintainer protecting operator outcomes,  
> I want the shared FrontComposer and Fluent contracts reverified against the corrected architecture and pinned dependencies,  
> So that operators receive a stable, accessible, projection-honest workspace and subsequent stories build only on demonstrated capabilities and honest fallback boundaries.

**Rationale:** The technical reverification actor remains truthful, while the protected user value becomes explicit.

#### CC-3 — Story 1.10: explain the corrective ordering

**Finding:** Epic-quality Minor 3.

**INSERT as the first acceptance criterion:**

> **Given** Stories 1.2 through 1.9 consume tenant reads earlier in the documented story order  
> **When** Story 1.10 is planned or reverified  
> **Then** it is treated as the explicit transport-and-provenance correction for those already-consumed read surfaces, not as a prerequisite silently assumed complete  
> **And** until `PLAT-FRESH-1`, `HOST-REF-1`, and `UI-READ-1` are verified, those surfaces remain honestly `unknown` where provenance is unavailable and fail closed where the safety contract requires it.

**Rationale:** The existing order remains defensible and no resequencing is needed; the text now explains why consumers can exist safely before the correction.

#### CC-4 — Story 1.11: label legacy numbering

**Finding:** Epic-quality Minor 4.

**OLD:**

> **Given** historical Stories 4.1 and 4.2 implementation evidence

**NEW:**

> **Given** pre-restructure implementation evidence historically numbered Stories 4.1 and 4.2 (not current Epic 4 Stories 4.1 and 4.2)

**Rationale:** Removes the visual collision without changing evidence ownership.

#### CC-5 — Story 2.2: scope `already applied` to same-attempt idempotency

**Finding:** Epic-quality Minor 5.

**OLD:**

> **Given** the expected membership already exists before qualifying post-submit provenance, a retry is deduplicated, or confirmation cannot be established  
> **When** reconciliation completes  
> **Then** the flow renders the supportable `already applied`, duplicate, or `unable to verify` state without false success  
> **And** offers inspect audit, retry status lookup, refresh, continue read-only, or escalation as appropriate.

**NEW:**

> **Given** the same logical add attempt is replayed or deduplicated after its accepted result may already have materialized, or qualifying confirmation cannot be established  
> **When** reconciliation completes  
> **Then** same-attempt idempotent evidence may support `already applied` or duplicate, and missing qualifying evidence produces `unable to verify`, without false success  
> **And** membership that authoritatively pre-existed this logical attempt remains the `UserAlreadyInTenant` rejection defined above—never `already applied`—with inspect audit, retry status lookup, refresh, continue read-only, or escalation offered as appropriate.

**Rationale:** Separates same-attempt replay semantics from the domain's existing-member rejection.

#### CC-6 — Story 2.4: make recovery capability availability explicit

**Finding:** Epic-quality Minor 6.

**OLD:**

> **Then** it offers the applicable refresh, wait, retry status lookup, request permission, restore intended access, inspect audit, continue read-only, or escalation path  
> **And** it does not promise undo, rollback, hidden editing, automatic re-addition, or any recovery the platform does not support.

**NEW:**

> **Then** it offers the applicable refresh, wait, retry status lookup, request permission, inspect audit, continue read-only, or escalation path, and offers `restore intended access` only when the separately delivered correction capability is available  
> **And** otherwise it states the correction capability is not ready without promising undo, rollback, hidden editing, automatic re-addition, or any recovery the platform does not support; Story 2.4 still completes FR12 and WP-2A without an Epic 5 dependency.

**Rationale:** Preserves the recovery vocabulary without creating a forward dependency.

#### CC-7 — Story 2.4: record the sizing decision

**Finding:** Epic-quality Minor 7.

**INSERT after the story value statement and before Acceptance Criteria:**

> **Delivery sizing decision:** Story 2.4 remains one acceptance and completion contract because fail-closed eligibility, preview, dispatch, projection confirmation, and WP-2A proof form one user-visible vertical outcome. If iteration planning cannot contain the work, use child delivery tasks **2.4a** (eligibility, preview, confirmation, dispatch) and **2.4b** (reconciliation, WP-2A proof, evidence/recovery), but keep Story 2.4 incomplete until both pass. These are task labels, not canonical story IDs; they do not change `sprint-status.yaml` without a separately approved reorganization.

**Rationale:** Makes the split decision deliberate while preventing partial “Done” or ID drift.

#### CC-8 — Story 3.1: document the first-tenant exception and exact rejection contract

**Finding:** Epic-quality Minor 8 plus the focused FR-13 downstream vocabulary residual.

**(a) Replace the authorization-enforcement line.**

**OLD:**

> **And** server-side API/domain authorization remains the enforcement boundary: tenant creation is domain-enforced as global-administrator-only (`GlobalAdminRequired`), reflected for non-global-administrator callers as `missing permission` and surfaced, if dispatched anyway, as safe localized rejection text.

**NEW:**

> **And** server-side API/domain authorization remains the enforcement boundary: tenant creation is global-administrator-only; non-global-administrator callers see `missing permission`, and a dispatched unauthorized request emits `InsufficientPermissionsRejection`, mapped by the UI to safe localized `InsufficientPermissions` text.

**(b) Insert immediately before the general valid-input submission AC.**

> **Given** the authorization-scoped tenant list is authoritatively empty and its freshness is `unknown` only because the first-tenant projection has no persisted write timestamp  
> **When** an authorized global administrator otherwise satisfies validation, command connectivity, lifecycle support, and aggregate admission  
> **Then** the create action remains available under the documented first-tenant bootstrap exception rather than deadlocking initial creation  
> **And** `TenantAlreadyExists` remains the authoritative collision backstop; `unknown` freshness for a non-empty, ambiguous, unauthorized, or otherwise unproven list does not receive this exception.

**Rationale:** Aligns the story with implemented bootstrap behavior, PRD/addendum vocabulary, domain code, and the focused FR-13 review.

#### CC-9 — Story 5.2: remove soft coupling to Story 5.4

**Finding:** Epic-quality Minor 9.

**INSERT before the “Story 5.2 is limited to audit reachability” AC:**

> **Given** a Story 5.2 entry point displays audit availability beside its contextual link  
> **When** pending, delayed, unavailable, missing-support, or available state is presented  
> **Then** Story 5.2 consumes only the shared typed audit-availability model and canonical labels already defined by the planning stack  
> **And** reachability is complete without depending on Story 5.4's later recovery-detail presentation; Story 5.4 enriches recovery behavior without becoming a prerequisite for navigation.

**Rationale:** Makes the existing intended boundary explicit and preserves within-epic backward dependency.

#### CC-10 — Epic 5: normalize legacy numbering and record Story 5.7 sizing

**Finding:** Epic-quality Minor 10.

**(a) Insert below the Epic 5 outcome paragraph.**

> **Legacy numbering note:** Historical Story 5.8 evidence refers to the pre-restructure single-authoritative-refresh/proof-association work now folded into current Stories 5.6 and 5.7. There is no current Story 5.8; references below label it as **former pre-restructure Story 5.8** and do not create a dependency or story ID.

**(b) Story 5.6 legacy-evidence AC.**

**OLD:**

> **Given** historical Stories 5.6 and 5.8 implementation evidence

**NEW:**

> **Given** historical Story 5.6 evidence plus former pre-restructure Story 5.8 evidence now folded into current Story 5.6

**(c) Story 5.7 composition AC.**

**OLD:**

> **Then** it reuses Story 4.1 availability, Stories 4.2–4.3 grant/remove command and projection behavior, Stories 5.1–5.4 evidence/recovery, and Story 5.6's proof plus folded historical Story 5.8 single-refresh foundations

**NEW:**

> **Then** it reuses Story 4.1 availability, Stories 4.2–4.3 grant/remove command and projection behavior, Stories 5.1–5.4 evidence/recovery, and Story 5.6's proof plus the folded single-authoritative-refresh foundations

**(d) Story 5.7 legacy-evidence AC.**

**OLD:**

> **Given** historical Stories 5.7 and 5.8 implementation evidence

**NEW:**

> **Given** historical Story 5.7 evidence plus former pre-restructure Story 5.8 evidence now folded into current Story 5.7

**(e) Insert after Story 5.7's value statement and before Acceptance Criteria.**

> **Delivery sizing decision:** Story 5.7 remains one acceptance and completion contract because fixed-scope evidence eligibility, complete-count/last-administrator safety, preview, dispatch, projection confirmation, and linked proof form one platform-governance outcome. If iteration planning cannot contain it, use child delivery tasks **5.7a** (evidence eligibility, complete current projection, preview, confirmation) and **5.7b** (dispatch, reconciliation, corrective proof linking), but keep Story 5.7 incomplete until both pass. These are task labels, not canonical story IDs.

**Rationale:** Removes dangling-current-number optics and documents why the larger fixed-scope slice remains comparable to the paired 5.5+5.6 tenant slice.

### B. PRD `prds/prd-tenants-2026-06-02/prd.md`

#### CC-11 — Resolve localization ownership in §16 and the assumptions index

**Observation:** Cross-document observation 1.

**(a) Open question §16.4.**

**OLD:**

> 4. **Localization resource ownership** — shared shell resources vs. Tenants-owned keys + adopter terminology.

**NEW:**

> 4. **Localization resource ownership — RESOLVED by architecture D4.** Tenants owns whole-string domain `.resx` keys and inherits only shell-chrome strings from `FcShellResources`; adopter terminology remains Tenants-owned domain copy.

**(b) Assumptions index §9 line.**

**OLD:**

> - §9 — WCAG 2.2 AA is a conditional target dependent on Fluent stack support; RTL undecided; localization resource ownership undecided.

**NEW:**

> - §9 — WCAG 2.2 AA is a conditional target dependent on Fluent stack support; RTL remains undecided; localization resource ownership is resolved by architecture D4 (Tenants-owned domain strings, FrontComposer-owned shell chrome).

**Rationale:** PRD now reflects the architecture decision already consumed by epics and UX; no behavior changes.

### C. `architecture.md`

#### CC-12 — Correct two superseded historical phrases

**Observation:** Cross-document observation 2.

**(a) NFR-1 historical summary.**

**OLD:**

> - **NFR-1 Performance/freshness:** cursor pagination + conditional requests (ETag/304);  
>   ~1s warm render; ~500-event audit target. Budgets are `[ASSUMPTION]`.

**NEW:**

> - **NFR-1 Performance/freshness:** cursor pagination + conditional requests (ETag/304);  
>   ~1s warm tenant-read target remains `[ASSUMPTION]`; audit performance has no numeric target until the Product/Operations §16.14 decision record approves the representative dataset, budgets, test method, and fallback trigger.

**(b) Cross-cutting concern 3.**

**OLD:**

> 3. **Freshness & eventual consistency (NFR-3)** — ETag/304 + projection-as-truth + Blazor  
>    Auto reconnect re-derivation.

**NEW:**

> 3. **Freshness & eventual consistency (NFR-3)** — ETag/304 + projection-as-truth + Blazor  
>    InteractiveServer circuit-reconnect re-derivation.

**Rationale:** Aligns non-authoritative historical description with the governing AD spine, PRD §16.14, and D1 without rewriting decision history.

### D. UX `ux-designs/ux-tenants-2026-06-02/DESIGN.md`

#### CC-13 — Make rc.4 icon verification status explicit

**Observation:** Cross-document observation 3.

**OLD opening:**

> **Verified status icon set.** Every name below was confirmed present in `5.0.0-rc.3-26138.1` ...; the pin is now **`5.0.0-rc.4-26180.1`, centrally consumed** — re-verify names, sizes, and variants against the rc.4 package at build.

**NEW opening:**

> **Historically verified status icon set; rc.4 reverification required.** Every name below was confirmed present in `5.0.0-rc.3-26138.1` ...; the current pin is **`5.0.0-rc.4-26180.1`, centrally consumed**, so current verification remains open until Story 1.0 records names, sizes, variants, and behavior against rc.4 at build.

The remainder of the icon table and its Size20/forced-colors rules remain unchanged.

**Rationale:** The document no longer overstates the release candidate against which evidence was collected, while preserving the already-correct build gate.

## 5. Implementation Handoff

### Classification and routing

- **Scope:** Minor.
- **Route after approval:** Developer agent for direct documentation implementation.
- **Product/Operations:** retain ownership of §16.14 audit-performance decision and freshness-threshold decisions; no decision is inferred here.
- **Platform owners:** retain the §I work packages (`PLAT-FRESH-1`, `HOST-REF-1`, `PLATFORM-OPS-1`); this proposal does not authorize work in their repositories or submodules.

### Implementation order

1. Apply CC-1 through CC-10 to `epics.md` as one coherent patch.
2. Apply CC-11 to `prd.md`, CC-12 to `architecture.md`, and CC-13 to `DESIGN.md`.
3. Verify headings/story IDs unchanged; confirm no current Story 5.8 is implied.
4. Re-run structural and targeted text checks, including Given/When/Then integrity and FR-13 rejection vocabulary.
5. Leave readiness reports and both same-day baseline artifacts unchanged.

### Success criteria

- All 10 readiness Minor findings have an explicit edit or authoritative disposition.
- All 3 cross-document observations are corrected without changing requirements or behavior.
- Story 3.1 matches the observable `InsufficientPermissionsRejection` → `InsufficientPermissions` → `missing permission` contract and documents the narrowly scoped first-tenant exception.
- NFR10 documentation/reference evidence is explicitly authoritative for every story without repetitive per-story drift.
- Story 2.4 and Story 5.7 sizing notes prevent partial completion and do not create canonical IDs.
- Epic 5 states that former Story 5.8 is folded and no current Story 5.8 exists.
- Story count remains 32; FR coverage remains 25/25; no epic/story numbering, ordering, scope, or sprint-status entry changes.
- Existing external gates remain visible and unchanged.

## 6. Checklist Record

- **§1 Understand trigger/context:** 1.1 N/A (assessment-triggered, not story-triggered); 1.2 Done (documentation consistency/polish); 1.3 Done (v2 report and current artifact lines).
- **§2 Epic impact:** 2.1 Done (all current epics remain completable); 2.2 Done (localized/shared-gate edits only); 2.3 Done (all five epics reviewed); 2.4 N/A (no obsolete/new epic); 2.5 N/A (no resequencing).
- **§3 Artifact conflicts:** 3.1 Done (PRD §16.4/assumption drift); 3.2 Done (two historical phrases); 3.3 Done (rc.4 evidence label); 3.4 Done (`sprint-status.yaml` N/A, no code/infrastructure change).
- **§4 Path forward:** 4.1 Viable — Direct Adjustment, Low effort/Low risk; 4.2 Not viable/not needed — rollback adds risk; 4.3 Not viable/not needed — MVP remains achievable; 4.4 Done — Option 1 selected.
- **§5 Proposal components:** 5.1–5.5 Done in this document.
- **§6 Final review/handoff:** 6.1 Done; 6.2 Done; 6.3 Done — explicit Administrator approval received on 2026-07-19; 6.4 N/A — no epic/story IDs change; 6.5 Done — Minor documentation implementation routed to the Developer role, applied, and verified.

## 7. Implementation Record

- **Approval:** Administrator explicitly approved this proposal on 2026-07-19.
- **Applied scope:** CC-1 through CC-10 in `epics.md`, CC-11 in the canonical PRD, CC-12 in `architecture.md`, and CC-13 in UX `DESIGN.md`.
- **Structural validation:** 32 story headings, 32 unique story IDs, zero duplicate story IDs, and 25 unique FR IDs remain.
- **Acceptance-criteria validation:** Given/When/Then counts are balanced at 446/446/446 after the three approved criterion insertions.
- **Targeted validation:** no current Story 5.8 heading or `GlobalAdminRequired` shorthand remains in `epics.md`; the exact FR-13 rejection/UI/availability vocabulary and first-tenant bootstrap exception are present; the superseded architecture, PRD, and rc.3-only verification phrases are removed.
- **Unchanged boundaries:** no sprint-status, source-code, API, domain-contract, dependency, infrastructure, or readiness-report change was made. The §I external work packages and §16.14 decision gate remain unchanged.
