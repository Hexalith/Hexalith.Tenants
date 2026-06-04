---
title: 'Sprint Change Proposal — FrontComposer Readiness + Fallback-Approval Reconciliation'
date: '2026-06-03'
project_name: 'Hexalith.Tenants'
author: 'Correct Course workflow (DEV/PM role)'
trigger: 'Implementation Readiness Assessment 2026-06-03 — 2 Critical build-start blockers'
mode: 'Batch'
scope_classification: 'Moderate (cross-document reconciliation + optional backlog addition)'
status: 'APPLIED 2026-06-03 — all edits applied; owner attribution confirmed'
decision:
  fallback_approval: 'APPROVED (reconciled toward UX) — recorded in fallback-approval-record-2026-06-03.md; owner = Jérôme Piquot (project owner), confirmed 2026-06-03'
affectedArtifacts:
  - 'NEW: fallback-approval-record-2026-06-03.md'
  - 'prds/prd-tenants-2026-06-02/prd.md'
  - 'prds/prd-tenants-2026-06-02/addendum.md'
  - 'architecture.md'
  - 'epics.md'
  - 'ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md'
  - 'ux-designs/ux-tenants-2026-06-02/DESIGN.md'
---

# Sprint Change Proposal — Hexalith.Tenants UI

**Date:** 2026-06-03 · **Mode:** Batch · **Scope:** Moderate · **Status:** DRAFT (awaiting approval)

---

## Section 1 — Issue Summary

The **Implementation Readiness Assessment (2026-06-03)** returned a *split verdict*: the planning layer is design-complete and high-quality (100% FR coverage — 25/25; 0 critical/0 structural-major epic defects; BDD-rigorous acceptance criteria), but **build-start is externally gated** by **two Critical blockers**. There is no artifact *rework* forced by a defect — both blockers are decision/external in nature.

### Blocker 1 — FrontComposer readiness (gates everything, including the read-only MVP)
- `FC-LYT` (shell layout contract) is **needs-confirmation** and gates even Epic 1 / the read-only MVP.
- `FC-CMD` (command-lifecycle feedback) + `FC-CNC` (concurrent-command policy) gate **all** command flows.
- The architecture additionally calls for a short **FrontComposer Shell-integration spike** (verify `AddHexalithFrontComposer*` / manifest registration / projection routing / the `FC-TBL` contract against Shell source) before any code.
- **Nature:** external dependency on the FrontComposer team + one dev spike. Cannot be closed by editing Tenants artifacts.

### Blocker 2 — Fallback-approval contradiction (load-bearing, cross-document)
- **UX** (`DESIGN.md` + `EXPERIENCE.md`) asserts the three interim fallbacks are *"approved (design-time Product/UX approvals)"* — but as a **bare claim with no evidence pointer**.
- **PRD** (§4 glossary, §12 R-1/R-4, §14 build-readiness, §14.3 phasing, §16.2) + **addendum** (§B, §F, §H) say **"no fallback is approved yet."**
- **Architecture D3** + **epics D3/UX-DR18** *build Phase-2c on the approved premise* while simultaneously flagging the contradiction as "must be reconciled with an owner/evidence/date record."
- **Nature:** a genuine cross-document contradiction requiring an authoritative Product/UX decision.

**The three fallbacks in question:** (a) `FC-AUD` → **flat audit DataGrid** (in lieu of `<AuditTimeline>`); (b) `FC-CNS` → **inline consequence text** carrying the full 10-item set, fail-closed; (c) `FC-CNC` → **one-at-a-time command policy** (no concurrent submission/toast-batching/bulk in v1).

### Decision taken in this Correct Course session
The user confirmed (interactive, 2026-06-03): **the fallbacks are genuinely approved** → reconcile **toward the UX**. This proposal therefore (i) creates a single authoritative **Fallback Approval Record** as the evidence source, and (ii) updates the PRD, addendum, architecture, and epics to cite it — turning a contradiction into one sourced fact.

---

## Section 2 — Impact Analysis

### Epic Impact
| Epic | Phase | Effect of this change |
|------|-------|-----------------------|
| **E1** Operations Shell & Triage (FR-1/2/5/7) | 2a MVP | **Unaffected by the approval** — read-only, uses `FC-TBL` (available). Its *only* gate is `FC-LYT` + the Shell spike (Blocker 1). Becomes build-ready the moment FC-LYT is confirmed. |
| **E2** Access/Config/Governance Review (FR-3/4/6/8/9/18) | 2a MVP | Read-only; gated on `FC-LYT` (Blocker 1). Unaffected by the approval. |
| **E3** Provisioning (FR-10/11/13/14) | 2b | Gated on `FC-CMD`+`FC-CNC` **contracts** (Blocker 1). The `FC-CNC` one-at-a-time **fallback** is now approved, so the *policy* gap is covered; the contract confirmation remains. |
| **E4** High-Impact & Destructive (FR-12/15/16/17/19) | 2c | **Premise confirmed.** The `FC-CNS` inline-consequence fallback is now approved → tenant-scoped destructive (FR-12, FR-16/17) gated only on the contract confirmations; platform-wide (FR-15, FR-19) stay categorically `blocked` by design (no fallback — unchanged). |
| **E5** Audit/Evidence/Recovery (FR-20–25) | 2c | **Premise confirmed.** The `FC-AUD` flat-grid fallback is now approved → Story 5.1's "`blocked` until the fallback is approved" clause is **satisfied**; remaining gate = `FC-LYT` contract + ready-gate evidence. |

**Net:** no epic is added, removed, or redefined. The approval **removes the fallback-approval gate** from every Phase-2b/2c row; it does **not** by itself make them build-ready (the FrontComposer **contract** confirmations remain). One **optional** structural addition is proposed below (a spike story) to make Blocker 1's spike trackable.

### Story Impact
- **Story 5.1** (Browse audit trail): gate clause "`blocked` until the fallback is approved" → satisfied; re-stated as fallback-approved, remaining gate = FC-LYT + evidence.
- **Stories 4.1 / 4.3 / 5.3 / 5.4** (consequence-preview & audit-dependent): premise now sourced; gate lines already name the fallbacks — no change beyond the central record.
- **Story 1.1** (bootstrap): unchanged; its Gate line already names the Shell-integration spike (see optional Story 1.0 below).
- **No story acceptance criteria change.**

### Artifact Conflicts (what must change, and why)
| Artifact | Spots | Current | Target |
|----------|-------|---------|--------|
| **PRD** `prd.md` | §4 (L148), §12 R-1 (L358), §12 R-4 (L361), §14 (L380), §14.3 (L395), §16.2 (L419) | "no fallback approved yet" / "require sign-off before use" | "approved 2026-06-03 — see Fallback Approval Record"; remaining gate = FC-LYT/FC-CMD contracts |
| **Addendum** `addendum.md` | §B rows FC-AUD/FC-CNS/FC-CNC (L28/30/31), §B criterion (L36), §F (L82), §H (L115) | "Proposed fallback (pending Product/UX approval)" | "Approved fallback (Product/UX, 2026-06-03)" |
| **Architecture** `architecture.md` | L129–130, L404, L406–408, L738–741, L794–797, L810 | "No fallback recorded as approved (a PRD↔UX contradiction)" / "currently unapproved" | "fallback approvals secured 2026-06-03"; remaining gate = contracts |
| **Epics** `epics.md` | banner (L34), D3 (L102), UX-DR18 (L158), Story 5.1 gate (L812) | "and on Product/UX fallback approvals" / "(Subject to reconciliation)" / "`blocked` until approved" | approvals secured; contradiction closed; remaining gate = contracts |
| **UX** `EXPERIENCE.md` / `DESIGN.md` | EXP L274; DSGN L221, L270 | "approved" (bare, unsourced) | "approved — recorded 2026-06-03 (see Fallback Approval Record)" |

### Technical Impact
- **No code exists yet** → no implementation rollback, no migration. (`implementation-artifacts/` is empty.)
- **No backend/contract change** — the UI composes fixed, already-built endpoints; nothing here touches `src/`.
- **`sprint-status.yaml` does not exist** (sprint planning has not run) → checklist item 6.4 is **N/A**; epic-status tracking is deferred to `bmad-sprint-planning` downstream.

---

## Section 3 — Recommended Approach

**Chosen path: Direct Adjustment (Option 1)** — reconcile the documents and structure the remaining external work; do **not** roll back (nothing built) and do **not** cut MVP scope (the approval expands buildability, it does not shrink scope).

**Rationale**
- The plan is excellent; the only work is (a) recording one decision and (b) closing one external dependency. Direct Adjustment matches that exactly.
- It preserves all team momentum and the 100%-traceable epic structure.
- Risk is low: every edit is a *sourcing/wording* change that removes a contradiction; none alters an FR, AC, NFR, or architectural decision.

**What this proposal resolves vs. what remains**
- ✅ **Blocker 2 — fully resolved here.** The Fallback Approval Record + the six-document reconciliation close the contradiction permanently.
- 🔻 **Blocker 1 — collapses to a single external action.** After Blocker 2, the remaining gate for *every* row is the FrontComposer **contract confirmation** — `FC-LYT` for the MVP (Epics 1–2), plus `FC-CMD`/`FC-CNC` for commands (Epics 3–5), plus `FC-A11Y`/`FC-L10N`/`FC-DOC` for each story's ready-gate — and the **Shell-integration spike**. These are owned by the FrontComposer team + one dev spike; they cannot be closed by editing Tenants docs.

**Effort:** Low (doc edits, ~10 min to apply) + External (FC-team confirmation, out of our hands). **Timeline impact:** none added by us; the FC-team confirmation is the pacing item. **Risk:** Low.

---

## Section 4 — Detailed Change Proposals

> All edits are **old → new**. Approval metadata placeholders are written as `«OWNER»`, `«DATE»`, `«EVIDENCE»` — **I will not invent these**; you supply them and I substitute on apply (see *Approval Required* at the end).

### 4.0 NEW FILE — `fallback-approval-record-2026-06-03.md` (the single source of truth)

A standalone, citable record that every other document points to. Contents:
- The three approved fallbacks (FC-AUD flat grid, FC-CNS inline text, FC-CNC one-at-a-time), each with its scope and fail-closed/safety conditions copied from the UX spines.
- **Approving owner(s):** `«OWNER»` · **Approval date:** `«DATE»` · **Evidence:** `«EVIDENCE»` (meeting note / decision-log entry / thread reference).
- Explicit scope statement: *approval covers exactly these three interim fallbacks; it does NOT waive the FC-LYT/FC-CMD/FC-CNC contract confirmations, the per-story ready-gate evidence (FC-A11Y/FC-L10N/FC-DOC), or the categorical `blocked` status of platform-wide destructive actions (FR-15/FR-19).*

### 4.1 PRD — `prds/prd-tenants-2026-06-02/prd.md`

**P1 · §4 Glossary (L148)**
- OLD: `A fallback is **only usable once Product/UX has approved it**; until then the dependent capability stays blocked. **No fallback is approved yet** (see §16).`
- NEW: `A fallback is **only usable once Product/UX has approved it**. **The three interim fallbacks — FC-AUD → flat audit grid, FC-CNS → inline consequence text, FC-CNC → one-at-a-time commands — are approved** (recorded «DATE»; see the Fallback Approval Record). Each remains build-ready only once its other FrontComposer gates clear.`

**P2 · §12 R-1 (L358)** — replace "proposed fallbacks … require Product/UX sign-off before use" → "**Product/UX-approved** fallbacks (approval recorded «DATE» — see the Fallback Approval Record)".

**P3 · §12 R-4 (L361)**
- OLD: `**R-4 Fallback-approval dependency.** Blocked stories cannot become "ready" without Product/UX fallback approval (none granted yet). **Mitigation:** explicit approval step in the per-phase gate (§9 ready-gate).`
- NEW: `**R-4 Fallback-approval dependency — RESOLVED («DATE»).** The three interim fallbacks (FC-AUD/FC-CNS/FC-CNC) are Product/UX-approved (see the Fallback Approval Record); the per-phase ready-gate's approval step is satisfied for these three. The remaining gates are the FrontComposer **contract confirmations** (FC-LYT/FC-CMD) plus FC-A11Y/FC-L10N/FC-DOC — not fallback approval.`

**P4 · §14 Build-readiness status (L380)** — header → `**Build-readiness status (updated 2026-06-03):**`; replace "audit/high-impact flows need FC-AUD/FC-CNS or approved fallbacks (none approved yet)" → "audit/high-impact flows are covered by the **Product/UX-approved** FC-AUD/FC-CNS/FC-CNC fallbacks (recorded «DATE» — see the Fallback Approval Record), leaving the FrontComposer **contract** confirmations (FC-LYT/FC-CMD) as their remaining gate".

**P5 · §14.3 Phasing summary (L395)** — replace "**Today both gates are open for every row** — nothing is promotable until §16.2/§16.3 are decided." → "**The fallback-approval gate is now satisfied** for the three interim fallbacks (recorded «DATE»); the **FC-LYT layout-contract gate (§16.3) remains open for every row, including the MVP**, and FC-CMD remains open for commands."

**P6 · §16.2 Open Question (L419)** — re-label as **RESOLVED («DATE»)**: Product/UX approved the flat-audit-list, inline-consequence-preview, and one-at-a-time fallbacks (see the Fallback Approval Record); building the rich `<AuditTimeline>`/`<ConsequencePreview>`/`FC-CNC` in FrontComposer becomes a *post-fallback enhancement*, not a blocker. **§16.3 (FC-LYT) remains the critical path.**

### 4.2 Addendum — `prds/prd-tenants-2026-06-02/addendum.md`

**A1 · §B table FC-AUD (L30)** — `**Proposed fallback (pending Product/UX approval): flat audit DataGrid** (FR-20).` → `**Approved fallback (Product/UX, «DATE»): flat audit DataGrid** (FR-20). See Fallback Approval Record.`

**A2 · §B table FC-CNS (L31)** — `**Proposed fallback (pending Product/UX approval): inline consequence text** (CP-5).` → `**Approved fallback (Product/UX, «DATE»): inline consequence text** (CP-5). See Fallback Approval Record.`

**A3 · §B table FC-CNC (L28)** — append `**Approved fallback (Product/UX, «DATE»): one-at-a-time commands.**` to the Notes cell.

**A4 · §B criterion (L36)** — append after "(none approved yet)" replacement: "The three interim fallbacks (FC-AUD/FC-CNS/FC-CNC) are approved («DATE» — see Fallback Approval Record); FC-LYT/FC-CMD contract confirmations remain."

**A5 · §F (L82)** — "flat list **proposed** … **pending Product/UX approval**" → "flat list is the **Product/UX-approved** fallback … (approved «DATE» — see Fallback Approval Record)".

**A6 · §H (L115)** — "The preview is **proposed** for inline rendering pending FC-CNS/Product-UX approval (§B)." → "The preview's inline rendering (the FC-CNS fallback) is **Product/UX-approved** («DATE» — see Fallback Approval Record; §B)."

### 4.3 Architecture — `architecture.md`

**AR1 · L129–130** — "No fallback is recorded as approved (a PRD↔UX contradiction to reconcile)." → "The FC-AUD/FC-CNS/FC-CNC fallbacks are Product/UX-approved («DATE» — see the Fallback Approval Record)."

**AR2 · L404** — "FC-AUD/FC-CNS fallback **approvals** gate phase-2c." → "FC-AUD/FC-CNS/FC-CNC fallback approvals are secured («DATE»); the FC-LYT/FC-CMD/FC-CNC **contract** confirmations gate phase-2c."

**AR3 · L406–408 (action items)** — mark the approval item ✅ "secured «DATE» (see the Fallback Approval Record)"; keep "Confirm FC-LYT / FC-CMD contracts" as the remaining build-start gate.

**AR4 · L738–741 (Gap Analysis Critical)** — "the FC-AUD/FC-CNS fallback **approvals secured** (currently unapproved)" → "the FC-AUD/FC-CNS/FC-CNC fallback **approvals are secured** («DATE»); the FC-LYT/FC-CMD/FC-CNC **contracts** still must be confirmed with the FrontComposer team."

**AR5 · L794–797 (Readiness Assessment)** — "gated by FrontComposer readiness (… + FC-AUD/FC-CNS fallback approvals) and the not-yet-created epics/stories layer" → "gated by FrontComposer **contract** readiness (FC-LYT/FC-CMD/FC-CNC). The fallback approvals are secured («DATE»); the epics/stories layer now exists (`epics.md`)." *(also corrects a second staleness — epics now exist.)*

**AR6 · L810** — "Close the FrontComposer contracts/approvals; run the Shell integration spike." → "Close the FrontComposer **contracts** (fallback approvals secured «DATE»); run the Shell integration spike."

### 4.4 Epics — `epics.md`

**E1 · Build-readiness banner (L34)** — re-state: rows are gated on FrontComposer **contract** readiness (FC-LYT/FC-CMD/FC-CNC); the FC-AUD/FC-CNS/FC-CNC **fallback approvals are secured («DATE» — see the Fallback Approval Record)**, so audit/high-impact rows are gated only on those contracts, not on fallback sign-off.

**E2 · D3 (L102)** — drop the "(Fallback-approval contradiction — UX says 'approved', PRD/backlog say 'none approved' — must be reconciled …)" parenthetical → "(Fallback-approval **reconciled «DATE»** — approval recorded with owner/date/evidence in the Fallback Approval Record; the prior PRD↔UX contradiction is closed.)"; add that FC-CNC's one-at-a-time fallback is likewise approved.

**E3 · UX-DR18 (L158)** — "(Subject to the fallback-approval reconciliation noted in Additional Requirements.)" → "(Fallback-approval **reconciled «DATE»** — see the Fallback Approval Record.)"

**E4 · Story 5.1 Gate (L812)** — "FC-AUD (fallback = flat DataGrid), … — `blocked` until the fallback is approved." → "FC-AUD (Product/UX-approved fallback = flat DataGrid, «DATE»), … — fallback approved; remaining gate = FC-LYT contract confirmation + ready-gate evidence."

**E5 · OPTIONAL — add Story 1.0 (Spike): FrontComposer Shell-integration + FC-LYT/FC-CMD contract confirmation.** A time-boxed, non-user-value enabler (sibling to the 1.1/1.2 enablers) that makes Blocker 1's spike a tracked, assignable item instead of a Gate-line footnote on Story 1.1. *Accept or reject this one in review — it is the only net-new backlog item in this proposal.*

### 4.5 UX — `EXPERIENCE.md` / `DESIGN.md` (add the evidence pointer to the existing, correct claims)

**U1 · EXPERIENCE.md L274** — "**Three approved interim fallbacks (design-time Product/UX approvals; …):**" → "**Three approved interim fallbacks (Product/UX approval recorded «DATE» — see the Fallback Approval Record; …):**"

**U2 · DESIGN.md L221** — append "(approval recorded «DATE» — see the Fallback Approval Record)" to "the approved v1 form is an inline structured-text fallback".

**U3 · DESIGN.md L270** — append the same pointer to "the approved interim form in place of `<AuditTimeline>`".

### 4.6 OPTIONAL — Secondary reconciliations (non-blocking; from readiness report rec #3)
*Include or skip — none of these gates build. Recommended because they are cheap and prevent an implementer being misled by a stale source doc.*
- **Divergence #1 (render mode):** `EXPERIENCE.md` §Foundation still says "Blazor **Auto**"; architecture **D1 = InteractiveServer** (a recorded reconciliation, NFR-3 holds either way). Add a one-line reconciliation note pointing to D1. *(Needs UX sign-off if you want the body text changed rather than annotated.)*
- **Divergence #3 (Users nav):** PRD §5.1 (L153) still lists **Users as a secondary primary-nav area**; architecture + epics resolved it to **contextual** (3 primary areas). Back-update §5.1 to "three primary areas; Users contextual."
- **R-6 (ULID error):** several `docs/tenants-ui-*.md` specs call `TenantId`/`UserId` ULIDs, contradicting the domain rule (caller-supplied strings, NOT ULIDs). **Flagged as a follow-up sweep** (multiple non-planning spec files) rather than applied inline here.

---

## Section 5 — Implementation Handoff

**Scope classification: Moderate** (cross-document reconciliation + one optional backlog addition; no replan, no scope change).

| Recipient | Responsibility |
|-----------|----------------|
| **Product / UX (approval owner)** | Ratify the Fallback Approval Record — supply `«OWNER»`, `«DATE»`, `«EVIDENCE»`. This is the one input only you hold. |
| **FrontComposer team** | Confirm `FC-LYT` + `FC-CMD` contracts, `FC-CNC` policy, and `FC-A11Y`/`FC-L10N`/`FC-DOC`; pair on the Shell-integration spike. **This is the remaining build-start gate (Blocker 1).** |
| **Architect (Winston)** | Architecture is sound; absorbs the AR1–AR6 doc edits and (if accepted) the Story 1.0 spike. |
| **Developer (Amelia)** | Run the Shell-integration spike (Story 1.0) once FC-LYT is confirmed → then Story 1.1 → 1.2 → Epic 1 read surfaces. |
| **PM / PO (John)** | After the spike: run `bmad-sprint-planning` + lightweight story sizing (no `sprint-status.yaml` exists yet); resolve phasing-lever open questions per phase (§16.1 command route, §16.8 config-edit preview scope, deferred numerics). |

**Success criteria for this change**
1. The Fallback Approval Record exists with real owner/date/evidence (no placeholders).
2. No planning document still says "no fallback approved," "pending Product/UX approval," "currently unapproved," or flags the contradiction.
3. Every former contradiction site cites the Fallback Approval Record.
4. The remaining build-start gate is stated identically across PRD/architecture/epics: **FC-LYT/FC-CMD contract confirmation + Shell spike** (no longer "fallback approval").

---

## Approval Required (before any artifact is edited)

To finalize, I need from you:
1. **Approving owner(s) + role** for the Fallback Approval Record (`«OWNER»`).
2. **Approval date** (`«DATE»`) — if it is today, I'll use 2026-06-03.
3. **Evidence pointer** (`«EVIDENCE»`) — a meeting note, decision-log line, ticket, or thread reference.
4. **Accept or reject the optional Story 1.0 spike** (§4.4 E5).
5. **Include or skip the optional secondary reconciliations** (§4.6).
6. **Continue [c]** to apply all edits, or **Edit [e]** to adjust this proposal first.

---

## Application Log — APPLIED 2026-06-03

User approved with **[c]** (apply all edits, both optional items included). Applied:
- **NEW** `fallback-approval-record-2026-06-03.md` (owner = Jérôme Piquot, project owner — **confirmed 2026-06-03**; evidence = ratified via this proposal).
- **PRD** `prd.md` — 6 reconciliation edits + Users-nav back-update (§5.1 → contextual).
- **Addendum** `addendum.md` — 6 edits (FC-AUD/FC-CNS/FC-CNC rows + §B criterion + §F + §H).
- **Architecture** `architecture.md` — 6 edits (key decisions, D-summary, action items, Gap Analysis, Readiness Assessment, Future Enhancements); also corrected the stale "epics not yet created" note.
- **Epics** `epics.md` — banner + D3 + UX-DR18 + Story 5.1 gate; **new Story 1.0 (Spike)** inserted in Epic 1.
- **UX** `EXPERIENCE.md` (record pointer + render-mode reconciliation note) + `DESIGN.md` (2 record pointers).

**Verification:** `rg` sweep confirms **0** residual "none approved / pending / unapproved / contradiction" strings across all six documents; the record is cited in every document.

**Deferred (as proposed):** the `docs/tenants-ui-*` ULID (R-6) correction sweep — a careful per-file follow-up (the fallback contradiction does **not** appear in `docs/`, so nothing there blocks build).
