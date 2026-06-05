---
title: 'Fallback Approval Record — Tenants Management UI (FrontComposer interim fallbacks)'
date: '2026-06-03'
project_name: 'Hexalith.Tenants'
record_type: 'product-ux-decision'
status: 'APPROVED'
supersedes: 'The prior PRD↔UX contradiction (UX: "approved" vs PRD/addendum: "none approved yet")'
created_by: 'Correct Course session — Sprint Change Proposal 2026-06-03'
approver: 'Jérôme Piquot — project owner, Hexalith.Tenants (acting Product/UX decision authority for this planning effort)'
approval_date: '2026-06-03'
evidence: 'Ratified via the Correct Course session and Sprint Change Proposal sprint-change-proposal-2026-06-03.md (2026-06-03). Owner attribution confirmed by the project owner (Jérôme Piquot) on 2026-06-03.'
cited_by:
  - 'prds/prd-tenants-2026-06-02/prd.md (§4, §12 R-1/R-4, §14, §14.3, §16.2)'
  - 'prds/prd-tenants-2026-06-02/addendum.md (§B, §F, §H)'
  - 'architecture.md (key decisions, D3, action items, Gap Analysis, Readiness Assessment)'
  - 'epics.md (build-readiness banner, D3, UX-DR18, Story 5.1 gate)'
  - 'ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md (FrontComposer Readiness & Fallbacks)'
  - 'ux-designs/ux-tenants-2026-06-02/DESIGN.md (ConsequencePreview, AuditDataGrid)'
---

# Fallback Approval Record — FrontComposer Interim Fallbacks

**Status:** ✅ APPROVED · **Date:** 2026-06-03 · **Project:** Hexalith.Tenants (Tenants Management UI)

> **Purpose.** This record is the single authoritative source for the Product/UX approval of the three
> interim FrontComposer fallbacks used by the Tenants Management UI. It **closes the prior
> cross-document contradiction** in which the UX spines asserted the fallbacks were "approved" while the
> PRD and addendum stated "no fallback is approved yet." All planning documents now cite this record.

---

## Approval

| Field | Value |
|-------|-------|
| **Decision** | The three interim FrontComposer fallbacks below are **approved for use** by Product/UX. |
| **Approving owner** | **Jérôme Piquot — project owner, Hexalith.Tenants** (acting Product/UX decision authority for this planning effort). |
| **Approval date** | **2026-06-03** |
| **Evidence** | Ratified via the **Correct Course session** and **Sprint Change Proposal** `sprint-change-proposal-2026-06-03.md` (2026-06-03). The decision was confirmed interactively during that session, reconciling the prior PRD↔UX contradiction in favour of approval. |

> ✅ **Owner confirmed (2026-06-03).** Jérôme Piquot (project owner) confirmed this attribution as the
> approving authority for this planning effort. No replacement needed.

---

## The three approved fallbacks

Each fallback is the **interim form** used until the corresponding rich FrontComposer capability ships.
Behaviour and safety conditions are copied verbatim-in-spirit from the UX spines (`EXPERIENCE.md`
"FrontComposer Readiness & Fallbacks"; `DESIGN.md` ConsequencePreview / AuditDataGrid).

### 1. `FC-AUD` → flat audit DataGrid *(in lieu of `<AuditTimeline>`)*
- Flat, **stably-ordered**, **cursor-paginated** Fluent `FluentDataGrid`.
- Date + `AuditEventCategory` (`Access` / `Administrative`) filters.
- Four distinct, accessible list states: loading / empty / filtered-empty / error; `filtered-empty` offers a clear filter reset.
- Targets ~500 events without unacceptable degradation (virtualization or stricter page size if a flat render cannot meet it).
- Maps to the `missing audit proof` / `missing implementation support` audit-availability reasons.
- Applies to **FR-20** (Epic 5, Story 5.1).

### 2. `FC-CNS` → inline consequence text *(in lieu of `<ConsequencePreview>`)*
- A **constrained inner region** of structured inline text carrying the **full 10-item Consequence Preview content set** (addendum §H).
- **Content completeness is non-negotiable; the panel fails closed** — it does not render a partial preview that could mislead. If any of the 10 items is unavailable, submission is blocked and the missing item is named.
- Applies to **CP-5** and **FR-12 / FR-15 / FR-16 / FR-17** (Epic 4).

### 3. `FC-CNC` → one-at-a-time command policy *(in lieu of a concurrent-command / toast-batching policy)*
- Serialized single-command interaction: **no concurrent command submission, no toast-batching, no multi-row bulk actions in v1**.
- While a command is in flight, other command triggers are unavailable with a stated reason.
- Applies to **all command FRs** (Epics 3–5).

---

## Scope of this approval — what it does and does NOT do

> **2026-06-05 supersession note:** Items 1 and 2 below were true at the time of fallback approval. Story 1.0 later confirmed `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`; see `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`. This record still remains the source of truth for the three Product/UX fallback approvals.

**This approval covers exactly the three interim fallbacks above.** It explicitly does **not**:

1. **Waive the FrontComposer contract confirmations.** `FC-LYT` (shell layout — gates even the read-only MVP) and `FC-CMD` (command-lifecycle feedback — gates all commands), plus the `FC-CNC` policy contract, **still must be confirmed with the FrontComposer team**. These remain the build-start gate (Blocker 1 of the 2026-06-03 readiness report).
2. **Waive the per-story ready-gate evidence.** `FC-A11Y` / `FC-L10N` / `FC-DOC` (needs-confirmation) plus the accessibility/localization/responsive acceptance evidence are still required before any story is `ready`.
3. **Change the categorical `blocked` status of platform-wide destructive actions.** **FR-15 (disable/enable)** and **FR-19 (global-admin grant/remove)** remain categorically `blocked` — they have **no fallback** by design. Only tenant-scoped destructive actions (FR-12, FR-16/17) are fallback-eligible.
4. **Authorize building the rich components inside Tenants.** Per the repo domain-boundary policy, `<AuditTimeline>` / `<ConsequencePreview>` and the concurrent-command policy belong in **FrontComposer**, not Tenants. Building them there is now a *post-fallback enhancement*, not a blocker.

---

## Effect on build-readiness

| Before (contradiction open) | After (this record) |
|-----------------------------|---------------------|
| Two open gates per row: (1) FrontComposer dependency resolution **and** (2) fallback approval. Nothing promotable. | The **fallback-approval gate is closed** for the three interim fallbacks. The **only remaining gate** for every row is the FrontComposer **contract confirmation** — `FC-LYT` (MVP), `+FC-CMD`/`FC-CNC` (commands), `+FC-A11Y`/`FC-L10N`/`FC-DOC` (ready-gate) — **plus the Shell-integration spike**. |

The read-only MVP (Epic 1) never depended on these fallbacks; it becomes build-ready the moment `FC-LYT`
is confirmed and the Shell-integration spike is done.

---

*Record created 2026-06-03 by the Correct Course workflow. Cite this file by path; do not duplicate its contents.*
