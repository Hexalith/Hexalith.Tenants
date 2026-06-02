# Input Reconciliation — Phase 2 Story Backlog vs. PRD + Addendum

**Source spec:** `docs/tenants-ui-phase-2-story-backlog.md` (the scope/phasing source for `ui-01..15`)
**Reconciled against:** `prd.md` + `addendum.md` (`prd-tenants-2026-06-02/`)
**Date:** 2026-06-02

This report lists only what the SOURCE SPEC contains that the PRD + addendum **missed, dropped, or misrepresented**. Items the PRD already covers adequately are not relisted. Nothing is invented beyond the spec.

---

## ui-01..15 → FR Coverage Table

Every backlog row maps to at least one PRD feature/FR. No capability is wholly missing, but several mappings are imprecise (see gaps). Phase column = PRD phase assigned to the mapped FR(s).

| Backlog row | Title (spec) | PRD FR(s) | PRD phase | Backlog readiness | Coverage verdict |
|---|---|---|---|---|---|
| ui-01 | Tenant List read-only | FR-1, FR-2 | 2a (MVP) | planning-only | Covered |
| ui-02 | My Tenants + User Search | FR-3, FR-4 | 2a (MVP) | planning-only | Covered |
| ui-03 | Tenant Detail overview | FR-5 | 2a (MVP) | planning-only | Covered |
| ui-04 | User Management member table | FR-8, FR-9 | 2a (MVP) | planning-only | Covered |
| ui-05 | Tenant Configuration read-only | FR-6, FR-7 | 2a (MVP) | planning-only | Covered |
| ui-06 | Global Admin read-only | FR-18 | 2a (MVP) | planning-only | Covered |
| ui-07 | Create Tenant command | FR-13 | 2b | planning-only | **Phase conflict (G1)** |
| ui-08 | Edit Tenant metadata command | FR-14 | 2b | planning-only | **Phase conflict (G1)** |
| ui-09 | Add user **AND** change role | FR-10 + FR-11 | 2b | planning-only | **1 row→2 FRs; phase conflict (G1, G2)** |
| ui-10 | Tenant Configuration edit | FR-16, FR-17 | 2c | blocked | Covered (phase OK) |
| ui-11 | Audit Trail flat timeline | FR-20, FR-21, FR-23 | 2c | blocked | Covered (phase OK) |
| ui-12 | Tenant Detail audit tab | FR-21 (partial) | 2c | blocked | **Weakly mapped (G6)** |
| ui-13 | Disable/enable tenant | FR-15 | 2c | blocked | Covered (phase OK) |
| ui-14 | Remove user from tenant | FR-12 | 2c | blocked | Covered (phase OK) |
| ui-15 | Global Admin command mgmt | FR-19 | 2c | blocked | Covered (phase OK) |

FR-22 (Audit Evidence Receipt) and FR-24/FR-25 (Compensating Recovery) have **no dedicated backlog row** — see G7.

---

## G1. Phasing: PRD Phase 2b assumes ui-07/08/09 are "unblocked", but the backlog blocks all three on FC-CMD + FC-CNC

- **Spec location:** Backlog rows ui-07/08/09 (lines 74–76); Deferred Decisions table rows for `FC-CMD` and `FC-CNC` (lines 128–129); Dependency ID Reference `FC-CNC` = `missing` (line 42).
- **What the PRD says:** §14.2 labels FR-10/11/13/14 as "Phase 2b — unblocked command flows … the command flows that do **not** depend on the missing FrontComposer components." §14.3 says a phase item "promotes only when its FrontComposer dependencies resolve or an approved fallback is recorded."
- **Conflict:** In the backlog, ui-07, ui-08, ui-09 each carry `blockedBy: [FC-LYT, FC-CMD, FC-CNC, …]`, with `FC-CMD: needs-confirmation` and **`FC-CNC: missing`**. The Deferred Decisions table explicitly lists ui-07/08/09 as gated on both the command-lifecycle contract (FC-CMD) and the concurrent-command/toast batching policy (FC-CNC). So these "Phase 2b" rows are **not** unblocked — they depend on a missing component (FC-CNC) and an unconfirmed one (FC-CMD), exactly the kind of dependency the PRD claims 2b avoids.
- **Severity:** HIGH (phasing premise is internally contradicted by the scope source).
- **Suggested PRD fix:** In §14.2/§14.3 redefine Phase 2b as "command flows blocked **only** on `FC-CMD`/`FC-CNC`/`FC-LYT` (not on `FC-AUD`/`FC-CNS`)" rather than "do not depend on missing FrontComposer components," and add `FC-CMD` resolution + `FC-CNC` policy (or approved reduced-feedback fallback) as explicit 2b gate conditions.

## G2. ui-09 bundles two PRD FRs (add user + change role) into one row; PRD treats them as separate FRs without noting the shared backlog row/command-story

- **Spec location:** Backlog ui-09 (line 76): `commands:AddUserToTenant|ChangeUserRole`, single `sequencingPriority: P09`, single `planning-only` row.
- **What the PRD says:** §7.4 splits these into FR-10 (add) and FR-11 (change role); addendum §A maps "7.4 Member & Role Mgmt (FR-10..12) → ui-09, ui-14."
- **Gap:** The PRD/addendum never record that FR-10 and FR-11 are a **single backlog candidate story (ui-09)** sharing one command-feedback story and one readiness verdict. Downstream epic/story owners reading the addendum could split ui-09 into two stories and lose the shared `FC-CMD`/`FC-CNC` gate and shared sequencing.
- **Severity:** MEDIUM.
- **Suggested PRD fix:** In addendum §A, annotate the FR-10/FR-11 → ui-09 row as "FR-10 + FR-11 share one candidate row (ui-09); same command-feedback dependency and readiness."

## G3. Addendum misrepresents FC-CNC readiness as "needs-confirmation"; backlog records it as "missing"

- **Spec location:** Backlog Dependency ID Reference `FC-CNC` (line 42) = `missing`; every command row (ui-07..10, ui-13..15) lists `FC-CNC: missing`; `<…>` alias and Deferred Decisions treat it as not-yet-available.
- **What the addendum says:** Table B (line 26): `FC-CNC | Concurrent-command / toast batching policy | needs-confirmation`.
- **Misrepresentation:** The addendum **upgrades** `missing` → `needs-confirmation`. `missing` means the capability/policy does not yet exist; `needs-confirmation` (per Story 12.1 vocabulary) means it likely exists and needs owner sign-off. This understates the gap and contradicts the readiness the backlog applies to seven command rows.
- **Severity:** HIGH (factual readiness error that softens a real blocker).
- **Suggested PRD fix:** Change addendum Table B `FC-CNC` readiness to **`missing`** (matching the backlog), and note the fallback path is "story-specific limits proving overlapping commands cannot occur."

## G4. Backlog's FC-TOK conditional readiness (needs-confirmation OR missing, by screen) is flattened to a single value

- **Spec location:** Backlog `FC-TOK` (line 45): readiness "depends on screen usage" — `needs-confirmation` for reused Fluent/FC role/status badges; `missing` when consequence/risk/destructive/timeline-connector token contracts are undefined. Rows reflect this: ui-02/03/04/06/07/09 use `FC-TOK: needs-confirmation`; ui-10/11/12/13/14/15 use `FC-TOK: missing`.
- **What the addendum says:** Table B (line 27): `FC-TOK | … | missing` (single value). PRD §12 R-1: "some tokens (FC-TOK) are missing."
- **Gap:** The per-screen split (badge tokens confirmable now vs. consequence/risk/timeline tokens missing) is lost. This matters because it is exactly what lets the read/2b rows proceed on existing Fluent badge semantics while only the high-impact/audit rows are token-blocked.
- **Severity:** MEDIUM.
- **Suggested PRD fix:** In addendum Table B, render `FC-TOK` as "**needs-confirmation** for reused role/status badges; **missing** for consequence/risk/timeline-connector tokens," matching the backlog's conditional rule.

## G5. Backlog distinguishes "planning-only" vs. "blocked" as a formal readiness state; PRD phasing collapses both into "out of scope for MVP" and never carries the blocked-vs-planning distinction

- **Spec location:** Backlog Readiness Order (lines 84–104): `planning-only` = ui-01..09; `blocked` = ui-10..15; with an explicit rule (line 104) that `blocked` requires `FC-AUD`/`FC-CNS: missing` **or** a `deferred` fallback on a high-impact/destructive workflow.
- **What the PRD says:** §14 buckets everything beyond MVP into "Phase 2b" and "Phase 2c" only; the words `planning-only`/`blocked` and the blocking rule never appear.
- **Gap:** The PRD loses the backlog's distinction that ui-10..15 are **blocked** (cannot even become "ready" without a missing component or an approved fallback) whereas ui-07..09 are merely **planning-only** (gated on confirmation, not on a missing component). A reader cannot tell from §14 which Phase 2c items are hard-blocked on `FC-AUD`/`FC-CNS` vs. which are deferred by risk policy.
- **Severity:** MEDIUM.
- **Suggested PRD fix:** Add a one-line readiness annotation to §14.2 per FR (e.g. "FR-16/17, FR-20–23: **blocked** on FC-CNS/FC-AUD; FR-15, FR-19: **blocked** by deferred high-impact fallback") cross-referencing the backlog's planning-only/blocked groups.

## G6. ui-12 (Tenant Detail audit tab) is only weakly mapped; PRD has no FR for an in-detail audit tab

- **Spec location:** Backlog ui-12 (line 79): "User reviews recent audit context **from tenant detail** without promoting grouped timeline mode," distinct backend evidence and `blocked` readiness.
- **What the PRD says:** Addendum §A maps ui-11+ui-12 jointly to "7.8 Audit Trail & Evidence (FR-20..23)." FR-21 ("Reach audit from context") mentions "tenant detail" as one entry point, but there is **no FR for an embedded audit tab/summary inside tenant detail** as a surface.
- **Gap:** ui-12 is a separate candidate surface (a tenant-detail audit tab with a "flat-audit-summary" fallback), not merely an entry point. It is folded into FR-21 and effectively under-specified as a distinct deliverable.
- **Severity:** LOW–MEDIUM.
- **Suggested PRD fix:** Either add a sub-consequence under FR-5 (Tenant overview) / FR-21 explicitly naming the tenant-detail audit tab (ui-12) as a Phase 2c surface, or note in addendum §A that ui-12 = "audit tab embedded in Tenant Detail," distinct from ui-11's top-level Audit area.

## G7. FR-22 (Audit Evidence Receipt) and FR-24/FR-25 (Compensating Recovery) have no backlog row; addendum admits recovery is "the recovery half of 9.5" with no real `ui-NN` candidate

- **Spec location:** The backlog contains **no row** for an evidence receipt or for compensating recovery; its Scope Boundary keeps "advanced analytics" out but never lists recovery/receipt as candidates. Addendum §A (line 17) maps FR-24/25 to "**(recovery half of 9.5)**" — a parenthetical, not a real backlog id (there is no ui-9.5).
- **What the PRD says:** §7.9 (FR-24, FR-25) and FR-22 are full Phase 2c features with testable consequences.
- **Gap / risk of silent promotion:** The PRD promotes Compensating Recovery (FR-24/25) and the Audit Evidence Receipt (FR-22) to first-class Phase 2c FRs, but the **scope source has no candidate row, no backend-evidence list, and no readiness/blockedBy for them.** This is the inverse of a dropped item — capabilities are asserted in the PRD ahead of the backlog. Per backlog Scope Boundary (lines 13–17), nothing should be promoted without an explicit product decision and backend evidence.
- **Severity:** HIGH (PRD scope exceeds the phasing source without a backlog mandate/evidence).
- **Suggested PRD fix:** Either (a) flag FR-22/FR-24/FR-25 as `[ASSUMPTION]`/"pending a backlog candidate row + backend-evidence confirmation," or (b) add candidate rows (e.g. ui-16 receipt, ui-17 compensating recovery) to the backlog before treating them as committed Phase 2c deliverables.

## G8. Backlog deferred-decision "split config edit into low-impact vs high-impact rows" is reflected only as an open question, not as a phasing/scope branch

- **Spec location:** Backlog Deferred Decisions, last row (line 134): unblock condition for ui-10 is that the "Configuration edit story **is split into low-impact and high-impact rows**, or Product/UX records a single consequence-preview policy."
- **What the PRD says:** §16 Q8 raises "Consequence Preview scope for config edits — always required, or only for a high-risk subset?" — but §14.2 places **all** of FR-16/17 in Phase 2c with no acknowledgment that a low-impact config-edit slice could phase earlier if the row is split.
- **Gap:** The backlog frames the split as an **unblock condition** (a scope/phasing lever), whereas the PRD frames it only as an interaction-design open question. The phasing implication (a low-impact config edit could move out of the blocked tier) is dropped.
- **Severity:** LOW–MEDIUM.
- **Suggested PRD fix:** In §14.2 (FR-16/17) note that splitting config edits into low-impact vs high-impact slices is a phasing option that could move the low-impact slice out of the FC-CNS-blocked tier (tie to §16 Q8).

## G9. PRD §12 R-1 implies FC-LYT blocks "high-value flows"; backlog records FC-LYT as a universal blocker on ui-01..15 (including all MVP reads)

- **Spec location:** Backlog Deferred Decisions, first row (line 127): the layout-contract decision affects "**ui-01 through ui-15**"; every row lists `FC-LYT` in `blockedBy`, including the MVP read rows ui-01..06.
- **What the PRD says:** §12 R-1 lists `FC-LYT` (layout contract) as "unconfirmed" among risks that block "high-value flows," and §16 Q3 treats it as an open layout question. §14.1 declares the MVP read foundation (FR-1..9, FR-18) "in scope" without flagging that the backlog marks **every** MVP read row as `blockedBy: [FC-LYT, …]`.
- **Gap:** Per the backlog, `FC-LYT` (needs-confirmation) is an unresolved blocker on the **MVP reads themselves**, not just high-value/command flows — so no ui-01..06 row has `blockedBy: []` and none is implementation-ready until layout is confirmed. The PRD's §14.1 "in scope (MVP)" reads as more ready than the backlog supports.
- **Severity:** MEDIUM.
- **Suggested PRD fix:** Add to §14.1 a gate note: "All MVP read rows (ui-01..06) remain blocked on `FC-LYT` confirmation per the backlog; resolving FC-LYT (or an approved current-shell fallback) is a precondition for any 2a story to become implementation-ready."

## G10. PRD claims FrontComposer "provides … a destructive-confirmation dialog" and "command lifecycle feedback (three-phase, projection-confirmed)" as given; backlog/addendum record these as not-yet-confirmed/missing

- **Spec location:** Backlog `FC-CMD` = `needs-confirmation` (line 41); `FC-CNS` (consequence preview / destructive flow) = `missing` (line 44); `<ConsequencePreview>` alias = `missing` (line 58).
- **What the PRD says:** §11 Dependencies lists, under "FrontComposer (provides — treat as given)," both "command lifecycle feedback (three-phase, projection-confirmed)" and "a destructive-confirmation dialog." Addendum Table B contradicts this: `FC-CMD: needs-confirmation`, `FC-CNS: missing` (with inline-text fallback needing approval).
- **Misrepresentation:** §11's "treat as given" list asserts capabilities the backlog/addendum classify as needs-confirmation/missing. A reader of §11 alone would assume command-feedback and destructive-confirmation are available when the scope source says they gate Phase 2b/2c.
- **Severity:** MEDIUM.
- **Suggested PRD fix:** In §11, move "command lifecycle feedback" and "destructive-confirmation dialog" out of the "provides — treat as given" list into a "depends on (FC-CMD needs-confirmation / FC-CNS missing — see addendum)" note, so §11 matches addendum Table B and the backlog.

---

## Items checked and found adequately covered (not gaps)

- ui-01..06 → FR-1..9/FR-18 mapping and 2a placement: consistent.
- ui-10, ui-11, ui-13, ui-14, ui-15 phase (all 2c) matches backlog `blocked` group.
- "No `ready` rows exist" (backlog line 62) is consistent with PRD treating everything as gated.
- Out-of-scope deferrals — grouped audit mode, server-side anomaly scoring, bulk provisioning, advanced analytics (backlog line 17) — are all carried into PRD §13 Non-Goals. None silently promoted.
- No-invitation / direct-add-by-user-id, no-data-store-edits, UI-reflects-not-enforces-authorization: consistent across both.
- FC-AUD missing + flat-list approved fallback for FR-20: consistent (backlog ui-11 + addendum Table B).
- `ui-NN` vs backend-epic numbering hazard: carried into PRD R-5 and addendum §E.

> Note (context, not a PRD gap): the `backendEvidence`/`evidenceSource` story keys (e.g. `5-3-…`, `9-1-…`, `post-epic-5-r5a3-…`) reference files under `_bmad-output/implementation-artifacts/`, which is currently **empty**. The backlog is explicitly "planning output only" (line 7), so these are forward references; flag only if downstream readiness checks assume the evidence files already exist.
