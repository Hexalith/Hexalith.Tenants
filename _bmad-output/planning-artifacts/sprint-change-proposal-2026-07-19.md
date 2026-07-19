# Sprint Change Proposal — 2026-07-19

**Trigger:** Implementation Readiness Assessment 2026-07-19 (5 Major epic-quality findings)
**Mode:** Batch review
**Scope classification:** Minor (documentation-only edits to `epics.md`; no epic/story additions, removals, or renumbering)
**Status:** APPROVED (Administrator, 2026-07-19) — **APPLIED** to `epics.md` same day; success criteria verified (member-row AC owned by Story 1.7 only; 4.1/4.2/4.3 preview symmetry; consistent 3.1 actor; 1.8 evidence scoped to Stories 1.2–1.8; 5.5 framing matched to its non-submitting slice; Given/Then integrity 443/443)

---

## 1. Issue Summary

The 2026-07-19 implementation-readiness assessment (`implementation-readiness-report-2026-07-19.md`) returned **READY** with **0 Critical** findings across 32 stories, but identified **5 Major** story-local defects in `epics.md` that should be corrected before the affected stories are created:

1. **Story 4.2** (grant global administrator) carries no Consequence Preview despite FR-19's "high-impact, platform-wide" classification (CP-5, UX-DR19) — asymmetric with Story 4.3, which has the full ten-item preview, and inconsistent with Story 4.1, which lists "preview readiness where required" as a grant/remove availability input.
2. **Story 1.5** (user lookup) forward-depends on Story 1.7 (member table): its member-row entry AC (lines 578–581) and must-pass "member-row entry" test scenario (line 615) require the member table delivered two stories later.
3. **Story 3.1** (create tenant) mixes actors: persona "authorized platform operator" (line 1288), AC actor "authorized global administrator" (line 1294), later "the operator" (line 1310). **Domain verification during this run:** `TenantAggregate.Handle(CreateTenant …)` (`src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs:28-36`) rejects non-global-administrators with `GlobalAdminRequired` — creation is **global-administrator-only**, so the AC actor is correct and the persona/FR wording is loose.
4. **Story 1.8** bundles FR7 identifier copy with an epic-wide evidence rollup whose "Epic 1 read surfaces" ACs (lines 767/772/777) reach into Stories 1.9–1.11, which come later.
5. **Story 5.5**'s "So that I can restore intended access…" (line 2437) is unreachable within its own slice — the story "dispatches no command… and creates no corrective proof" (line 2489); Story 5.6 owns submission/confirmation/linking (line 2513).

Evidence: every finding is line-verified in the readiness report (Epic Quality Review section).

## 2. Impact Analysis

- **Epic impact:** Epics 1, 3, 4, 5 receive wording/AC-level edits only. All epics remain completable as planned; no epic scope, sequencing, or priority changes. Epic 2 is untouched (its findings were Minor and are out of this proposal's scope).
- **Story impact:** 6 stories edited (1.5, 1.7, 1.8, 3.1, 4.2, 5.5) — no stories added, removed, or renumbered. The 1.5→1.7 fix is resolved by **moving AC ownership**, not reordering stories, so `sprint-status.yaml` needs no ID changes.
- **PRD:** no conflict with goals or MVP. One **companion errata handoff** (not edited here): PRD §7.5 FR-13 says "An authorized operator can create a tenant" while the domain enforces global-administrator-only — route to the next `bmad-prd` update run (same pattern as the 2026-07-19 UX-run audit-nav errata).
- **Architecture:** no conflict — CP-5/AD-12 and UX-DR19 are the authorities *motivating* Edit 1; no architecture edits needed.
- **UX:** no conflict; no UX-spine edits needed.
- **Other artifacts:** `sprint-status.yaml` — N/A (no ID/count changes). Story files for the affected stories have not been created under the corrected contract yet, so no story-file rework.

## 3. Recommended Approach

**Option 1 — Direct Adjustment** (chosen). Effort: **Low** (five localized `epics.md` edit sets). Risk: **Low** (wording/AC-level; all edits converge the epics with already-ratified authorities: CP-5, UX-DR19, AD-12, FR13's enforced RBAC, and create-epics-and-stories independence rules). Rollback (Option 2) is N/A — nothing to revert; MVP review (Option 3) is unnecessary — scope is unaffected.

## 4. Detailed Change Proposals (epics.md)

### CP-1 — Story 4.2: add the grant Consequence Preview and confirmation friction

**(a) Fail-closed availability gate — add preview readiness (line 1919)**

OLD:
> **Given** authorization, direct-read freshness, fixed-scope command support, authoritative re-query support, aggregate admission, or viewport safety is stale, missing, unknown, or indeterminate

NEW:
> **Given** authorization, direct-read freshness, fixed-scope command support, authoritative re-query support, preview readiness, aggregate admission, or viewport safety is stale, missing, unknown, or indeterminate

**(b) Insert two new ACs after that gate block (after line 1922), mirroring Story 4.3's ten-item platform-governance preview adapted to grant:**

> **Given** grant eligibility gates pass for a literal target UserId
> **When** the BFF assembles the grant consequence preview
> **Then** it supplies all ten platform-governance items: fixed platform scope, target UserId, current complete administrator count, resulting count impact, the specific platform authority being granted, authoritative freshness, the recovery path (deliberate removal as the forward correction), the audit expectation, caller/target platform context, and known consequences versus known unknowns
> **And** the redacted support-safe preview introduces no new backend endpoint and blocks confirmation while any required item is missing.
>
> **Given** the complete high-impact grant preview is open
> **When** the user reviews, cancels, presses Escape, or explicitly confirms
> **Then** focus is trapped while open, cancel and Escape dispatch nothing, deliberate confirmation friction is required before dispatch, and focus returns to the launching control
> **And** grant is never a primary/casual or bulk action and remains unavailable on layouts that cannot preserve the full safety context.

**(c) Gate dispatch on the confirmed preview (line 1924)**

OLD:
> **Given** valid eligible input and an explicit deliberate submit

NEW:
> **Given** the complete preview remains current and deliberate confirmation succeeds

**(d) Closing test AC — cover the preview (line 1990)**

OLD:
> **When** focused fixed-payload, literal-identity, validation, authorization, existing-target rejection, idempotency, aggregate-lock, lifecycle, projection-provenance, audit handoff, reconnect, localization, accessibility, responsive, support-safety, and E2E tests run

NEW:
> **When** focused fixed-payload, literal-identity, validation, authorization, complete-preview, confirmation-friction, existing-target rejection, idempotency, aggregate-lock, lifecycle, projection-provenance, audit handoff, reconnect, localization, accessibility, responsive, support-safety, and E2E tests run

**Rationale:** FR-19 is "high-impact, platform-wide" (PRD §7.7); CP-5 and UX-DR19 require a complete preview for high-impact flows; 4.1 already treats preview readiness as a grant/remove availability input. This restores 4.1↔4.2↔4.3 symmetry.

### CP-2 — Move the member-row entry point from Story 1.5 to Story 1.7

**(a) DELETE from Story 1.5 (lines 578–581):**

> **Given** the operator is reviewing a tenant member row
> **When** the contextual user-membership link is activated
> **Then** it opens the canonical Users tab with the safely encoded `userId` workspace state
> **And** return navigation preserves the originating tenant, tab, filter, selection, cursor context, and scroll position.

**(b) Story 1.5 test AC (line 615):**

OLD:
> **Then** direct entry, member-row entry, hidden membership, empty, error, Unicode identifier, and return-context scenarios pass

NEW:
> **Then** direct entry, hidden membership, empty, error, Unicode identifier, and return-context scenarios pass

**(c) ADD to Story 1.7, as a new AC after the risk/context AC (after line 707):** the exact four lines deleted from 1.5 (unchanged text).

**(d) Story 1.7 test AC (line 731):**

OLD:
> **Then** every canonical unavailable reason and required edge context is covered

NEW:
> **Then** every canonical unavailable reason, the member-row user-membership entry scenario, and required edge context are covered

**Rationale:** Story 1.7 owns the member table that hosts the link; FR4's "reach the same view from a member row" is then realized by 1.5 (lookup surface) + 1.7 (entry point), with no forward dependency. No story reordering, so no sprint-status ripple.

### CP-3 — Story 3.1: align the actor with the enforced RBAC

**(a) Persona (line 1288):** OLD `As an authorized platform operator,` → NEW `As an authorized global administrator,`

**(b) Submit AC (line 1310):** OLD `**When** the operator submits a deliberate attempt` → NEW `**When** the global administrator submits a deliberate attempt`

**(c) Fail-closed AC closing line (line 1307):**

OLD:
> **And** server-side API/domain authorization remains the enforcement boundary.

NEW:
> **And** server-side API/domain authorization remains the enforcement boundary: tenant creation is domain-enforced as global-administrator-only (`GlobalAdminRequired`), reflected for non-global-administrator callers as `missing permission` and surfaced, if dispatched anyway, as safe localized rejection text.

**(d) Requirements inventory FR13 (line 45):** insert the clarification `(domain-enforced as global-administrator-only)` after "An authorized operator".

**(e) FR Coverage Map FR13 (line 218):** OLD `FR13: Epic 3 - Create a tenant with safe duplicate handling and projection confirmation.` → NEW `FR13: Epic 3 - Create a tenant (global-administrator-only) with safe duplicate handling and projection confirmation.`

**(f) Companion errata handoff (NOT edited here):** PRD §7.5 FR-13 "An authorized operator can create a tenant" → route to the next `bmad-prd` update run to add the same enforcement clarification.

**Rationale:** verified against `TenantAggregate.cs:28-36` — the AC actor was right; the persona and inventory were loose.

### CP-4 — Story 1.8: restrict the evidence rollup to delivered surfaces

**(a) line 767:** OLD `**Given** Epic 1 read surfaces in English and French` → NEW `**Given** the read surfaces delivered by Stories 1.2 through 1.8 in English and French`

**(b) line 772:** OLD `**Given** Epic 1 read surfaces at desktop, tablet, mobile, high contrast, forced colors, and reduced motion` → NEW `**Given** the read surfaces delivered by Stories 1.2 through 1.8 at desktop, tablet, mobile, high contrast, forced colors, and reduced motion`

**(c) line 777:** OLD `**Given** Epic 1 automation and documentation contracts` → NEW `**Given** the automation and documentation contracts of the surfaces delivered by Stories 1.2 through 1.8`

**(d) line 790:** OLD `**And** evidence is recorded without treating historical completion as a readiness waiver for Stories 1.9 through 1.11.` → NEW `**And** evidence is recorded for the in-scope surfaces only; Stories 1.9 through 1.11 carry their own equivalent evidence gates, and historical completion never becomes a readiness waiver for them.`

**Rationale:** removes the forward reach while preserving full epic coverage — 1.9/1.10/1.11 each already carry their own NFR10 evidence ACs (readiness report, Epic 1 review).

### CP-5 — Story 5.5: honest standalone-value framing

**(lines 2435–2437)**

OLD:
> As an authorized tenant operator,
> I want to start a forward membership correction from proven audit evidence,
> So that I can restore intended access without editing or relabeling the original event.

NEW:
> As an authorized tenant operator,
> I want to start a forward membership correction from proven audit evidence with a verified current-state intent,
> So that a mistaken access change can be corrected forward — submission, confirmation, and linked proof completing in Story 5.6 — without editing or relabeling the original event.

**Rationale:** the story's own ACs (lines 2489, 2511–2514) correctly scope it as a non-submitting start slice; the value statement now matches, and the 5.5+5.6 pairing for FR24 is explicit.

## 5. Implementation Handoff

- **Scope: Minor** → direct implementation (documentation edits to `epics.md` applied on approval in this session).
- **`sprint-status.yaml`:** no update required (no epic/story additions, removals, or renumbering).
- **Companion handoffs:** (1) PRD FR-13 wording errata → next `bmad-prd` update run; (2) the 10 Minor readiness findings (incl. the recurring NFR10 documentation-evidence line) remain available in the readiness report for a later polish pass — deliberately excluded from this Minor-scope proposal.
- **Success criteria:** all five CP edit sets applied verbatim; a re-scan confirms no remaining forward dependency (1.5→1.7), preview symmetry across 4.1/4.2/4.3, consistent 3.1 actor, scoped 1.8 evidence, and matching 5.5 framing.

## 6. Checklist Record

- §1 Trigger/context: **Done** (trigger = readiness assessment, not a story; evidence line-verified)
- §2 Epic impact: **Done** (2.1/2.2/2.3 Done — AC-level edits only; 2.4/2.5 N/A — no new/removed/resequenced epics)
- §3 Artifact conflicts: **Done** (3.1 PRD: no conflict + one errata handoff; 3.2 Architecture: N/A; 3.3 UX: N/A; 3.4 Other: sprint-status N/A)
- §4 Path forward: **Done** (Option 1 Direct Adjustment — Low effort, Low risk; Options 2/3 not viable/not needed)
- §5 Proposal components: **Done** (this document)
- §6 Final review: **Done** (6.1/6.2 Done; 6.3 approved by Administrator 2026-07-19; 6.4 sprint-status.yaml N/A — no ID/count changes; 6.5 handoffs confirmed: PRD FR-13 errata → next `bmad-prd` run; Minor findings → later polish pass)
