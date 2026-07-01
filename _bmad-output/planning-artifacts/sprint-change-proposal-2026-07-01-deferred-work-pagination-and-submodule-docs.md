# Sprint Change Proposal — Deferred Work: GA Pagination Fail-Closed + Submodule Doc Handoffs

- **Date:** 2026-07-01
- **Author:** Amelia (Developer), via Correct Course
- **Trigger input:** `_bmad-output/implementation-artifacts/deferred-work.md` (routing index)
- **Mode:** Incremental (each edit approved individually)
- **Scope classification:** **Minor** (Developer-executable; no epic/PRD/MVP change)
- **Approval:** Administrator — approved the run scope ("Pagination fix + submodule docs"), each proposal
  individually, and (implicitly, by choosing the submodule-docs option) the FrontComposer + EventStore
  submodule doc-only edits.
- **Status:** Implemented, verified on the change surface, **UNCOMMITTED**.

---

## Section 1 — Issue Summary

The deferred-work routing index has had every *Tenants-owned, actionable parity* item resolved across
prior Correct Course runs (the latest, 2026-06-30, committed as `ac3b8d5`). A re-verification of the
remaining **open** items found four classes:

| # | Open item | Owner | Nature |
|---|-----------|-------|--------|
| 1 | **Global-administrator pagination >20 admins** — the correction snapshot reads presence/absence only from page 1 of the cursor-paged fixed projection (`PageSize=20`). A revoke of a page-2 admin reads `!present` and reaches a **false `Confirmed`** (a fail-**open** on a platform-authority correction); a revoke also mis-previews as "already removed" and a restore mis-arms / sticks pending. | **Tenants** | Real correctness bug (the only remaining open item with fail-open severity) |
| 2 | IA-blocked trio: GA/Audit discoverability after nav de-listing; `GlobalAdministratorPolicy` registered-but-unconsumed; page-local empty tabpanels | Product / UX | Needs a decision, not code |
| 3 | Cross-submodule doc handoffs: FrontComposer `FcContentLabel` dispose-clobber + server first-paint; `FcPageHeader.FocusHeadingAsync` no-op→throw; EventStore `StorageTreemap` SVG `<g tabindex>` cross-browser | FrontComposer / EventStore | Doc-only, outside the Tenants boundary |
| 4 | `EventCallback→Func` parent re-render (benign, intentional); ETag special-character (latent, non-exploitable) | Tenants | No action warranted |

**Discovery evidence.** Item 1 was logged 2026-06-29 (Story 5.7 review) and re-confirmed 2026-06-30 with a
*raised* severity note: the confirm-time revoke path (`ConfirmProjection` computing `projectionProvesCorrection = !present`)
turns "absent from page 1" into proof of removal, so a revoke of a 21st+ administrator can present as
`Confirmed` when it is not — the opposite of this codebase's fail-closed command-confirmation invariant.
The snapshot type already carries a `HasMore` completeness signal (`GlobalAdministratorsSnapshot.HasMore`),
so a minimal fail-closed guard is achievable without the full paging redesign.

This run took **item 1** (the fail-open) and **item 3** (the doc handoffs). Items 2 and 4 remain routed.

---

## Section 2 — Impact Analysis

- **Epic impact:** None. Epic 5 (Audit Evidence and Forward Recovery) is `done`; this is post-epic
  hardening of already-shipped correction code. No epic is re-opened, re-scoped, or re-sequenced.
- **Story impact:** None re-opened. The fix hardens Story 5.7 (global-administrator correction) code that
  Story 5.8's File List bundled in; it is registered as a deferred-work action item, not a new story.
- **PRD / Architecture / UX impact:** None. No requirement, contract, data model, or UX flow changes.
  The fix is *more* conservative (fails closed at unusual scale); the visible states it can produce
  (`UnableToVerify` + the existing "Current projection evidence is unavailable." copy) are already in the
  model and localized (EN/FR), so there is **no** resource, wire, or contract change.
- **Technical impact (code):**
  - `src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs` — absence is
    now conclusive only when the whole projection is loaded (`!HasMore`).
  - `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorCorrectionSnapshotTests.cs` — +3 tests, +1 helper.
- **Technical impact (submodules, doc-only):**
  - FrontComposer: `FcContentLabel.razor.cs`, `FcContentLabelCoordinator.cs`, `FcPageHeader.razor.cs` — XML `<remarks>` additions.
  - EventStore: `StorageTreemap.razor` — a Razor comment.
- **Artifact conflicts:** None. `deferred-work.md` is annotated (routing index) and `sprint-status.yaml`
  gains one action item.

---

## Section 3 — Recommended Approach

**Selected path: Direct Adjustment (Hybrid — implement the safety fix, keep the redesign routed).**

- **Item 1 — implement the minimal fail-closed guard now.** The false-`Confirmed` is a fail-open on
  platform authority and must not persist. The `HasMore` signal makes a surgical guard possible without
  the full multi-page load/aggregation. The *full* paging redesign (so a page-2 correction can actually
  run instead of being conservatively blocked) stays routed to a dedicated projection-paging story, as
  the index specifies — that is design-level work with its own query-shape decisions.
- **Item 3 — land the doc handoffs now.** They are doc-only, low-risk, and close long-standing
  cross-submodule follow-ups; the Administrator authorized the submodule edits.
- **Items 2 and 4 — remain routed.** Item 2 is genuinely Product/UX-blocked; item 4 is benign/latent.

**Rationale:** highest correctness value (closes the only remaining fail-open) at minimal risk and blast
radius (UI state class + doc comments), no epic/PRD/MVP disturbance, and it preserves the deliberate
scope boundary the index drew around the paging redesign.

**Effort:** Low. **Risk:** Low (more conservative behavior; no contract/resource/wire change).

**Alternatives considered:**
- *Full paging redesign now* — rejected: larger, design-level, needs query-shape decisions; correctly a
  separate story. The safety guard removes the fail-open in the interim.
- *Verify-and-route only (no code)* — rejected: would leave a known platform-authority fail-open in place.
- *Blanket-reject any `HasMore` projection in the readable gate* — rejected: it would also block valid
  page-1 corrections at >20-admin scale. The chosen "absence-must-be-conclusive" form preserves those.

---

## Section 4 — Detailed Change Proposals

### 4.1 Code — `GlobalAdministratorCorrectionSnapshot.cs` (Tenants)

Absence is conclusive only when the whole fixed projection is loaded (`!HasMore`). Presence-found is
always conclusive (the target IS in the loaded rows).

- **`EvaluateCurrentProjection` (restore branch):** target absent **and** `HasMore` ⇒
  `IncompleteProjectionFailClosed` (`UnableToVerify`), rather than arming a submittable grant.
- **`EvaluateCurrentProjection` (revoke branch):** target absent **and** `HasMore` ⇒
  `IncompleteProjectionFailClosed`, rather than the false `AlreadyApplied` ("already removed").
- **`ConfirmProjection`:** `projectionProvesCorrection = IsRestoreAccessAction ? present : (!present && !projection.HasMore)`
  — a revoke is proven only by a conclusive absence, killing the false `Confirmed`.
- **New helper `IncompleteProjectionFailClosed`:** mirrors the `ProjectionIsReadable` fail-closed shape and
  reuses the existing `Tenants.Correction.Unavailable.CurrentProjectionUnavailable` copy (no resx churn),
  with an XML note that the full paging redesign is out of scope and routed.

The `present == true` revoke path is untouched: a found target is conclusive, and `DistinctAdministratorCount`
undercounting at scale can only *over*-trigger the last-administrator hard stop, never under-trigger it
(conservative-safe).

### 4.2 Tests — `GlobalAdministratorCorrectionSnapshotTests.cs` (Tenants)

- `Revoke_with_absent_target_on_incomplete_page_fails_closed_instead_of_already_removed`
- `Restore_with_absent_target_on_incomplete_page_fails_closed`
- `Revoke_is_not_confirmed_from_an_incomplete_projection_page` (the false-`Confirmed` regression guard)
- `+ PagedProjectionReady(...)` helper (Ready + Current + `HasMore=true`).

### 4.3 Docs — FrontComposer submodule (doc-only)

- `FcContentLabel.razor.cs` `<remarks>`: single-writer last-writer-wins dispose-clobber; `OnAfterRender`-only
  server first-paint limitation, naming the shell-parameter path as first-paint-correct.
- `FcContentLabelCoordinator.cs` `<remarks>`: matching sentence so both files name the hazard identically.
- `FcPageHeader.razor.cs` `FocusHeadingAsync` `<remarks>`: adopter-facing no-op→throw behavior-change note
  (FrontComposer has no CHANGELOG), including the `FcAggregateListPage` `?? ValueTask.CompletedTask` caveat.

### 4.4 Docs — EventStore submodule (doc-only)

- `StorageTreemap.razor`: Razor comment above the focusable `<g role="button" tabindex="0">` recording the
  Chromium/Edge/Firefox-vs-Safari/WebKit tab-order caveat and the `<a>`/`<foreignObject>` remedy.

---

## Section 5 — Implementation Handoff

- **Scope:** Minor — implemented directly by the Developer agent during this run.
- **Verification performed:**
  - `Hexalith.Tenants.UI.Tests` build **0 warnings / 0 errors**; **874/874** tests pass (871 baseline + 3 new).
  - All Tenants library + Tier-1/Tier-2 test projects build 0/0 (slnx Debug).
  - FrontComposer `Shell` build 0/0 (validates the doc `cref`s under warnings-as-errors).
  - EventStore `Admin.UI` build 0/0 (validates the Razor comment).
- **Known blockers (pre-existing / environmental — NOT this change):**
  - `Hexalith.Tenants.AppHost` + `Hexalith.Tenants.IntegrationTests` Debug build fails: the `Hexalith.Commons`
    submodule was fast-forwarded to `3666203` by an external `git pull --tags origin main` (per its reflog;
    not run this session), and `Hexalith.Commons.Aspire` no longer resolves for the AppHost. Confirmed the
    AppHost fails to build in isolation too. Unrelated to the UI state class + doc comments; CI restores
    pinned submodule commits and is unaffected.
  - Release `-warnaserror` remains blocked locally by `NU1102` on the pinned `Hexalith.Commons.UniqueIds 3.19.0`
    (unpublished in this environment; CI has it) — same blocker recorded by the 2026-06-30 run.
- **Commit status:** UNCOMMITTED. Tenants repo (2 files) + FrontComposer submodule (3 files) + EventStore
  submodule (1 file). Any FrontComposer/EventStore commit is a new follow-up commit in the respective
  submodule, per their contribution rules.
- **Follow-ups kept deferred (routed):**
  - Full global-administrator projection paging/aggregation → **new dedicated story** (so a page-2
    correction can run rather than be conservatively blocked).
  - IA/Product-blocked trio (GA/Audit discoverability, unconsumed `GlobalAdministratorPolicy`, page-local
    empty tabpanels) → awaits the contextual-entry-point IA decision.
  - `EventCallback→Func` parent re-render (benign) and ETag special-character (latent) → watch-items.

---

## Success Criteria

- The false-`Confirmed` on a revoke of a >20-admin (page-2) target can no longer occur; incomplete-page
  presence/absence fails closed to `UnableToVerify`. ✅ (test `Revoke_is_not_confirmed_from_an_incomplete_projection_page`)
- No regression to page-1 corrections at scale or to the existing complete-page behavior. ✅ (existing 871 tests green)
- The three cross-submodule follow-ups are documented at their exact sites. ✅ (builds 0/0)
