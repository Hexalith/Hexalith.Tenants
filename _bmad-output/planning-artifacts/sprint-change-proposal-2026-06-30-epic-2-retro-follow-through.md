# Sprint Change Proposal — Epic 2 Retrospective Follow-Through (Verify & Close)

- **Date:** 2026-06-30
- **Trigger artifact:** `_bmad-output/implementation-artifacts/epic-2-retro-2026-06-06.md`
- **Workflow:** Correct Course (`bmad-correct-course`)
- **Mode:** Verify & close (residual-gap audit), batch review — matching the same-day Epic 1 retro follow-through precedent
- **Facilitator:** Amelia (Developer)
- **Project lead:** Administrator
- **Change scope classification:** **Minor** (traceability registration only — no source, no doc-body edit)

---

## Section 1 — Issue Summary

The Epic 2 retrospective (`epic-2-retro-2026-06-06.md`) closed Epic 2's stories, recorded five action items (AI1–AI5), audited six documentation drift candidates, and listed a four-item "Critical Path Before Epic 3". The retrospective is **24 days old**: today is 2026-06-30 and **Epics 2–5 are all `done`** (`sprint-status.yaml:65–100`; retros exist for Epics 1–5; stories run through 5-8), with the Epic 3/4/5 retros and ~50 sprint change proposals landed since. Every Epic 2 action item targeted Epic 3 readiness, and Epic 3 has since shipped — so the items were **overtaken by subsequent execution** rather than left open.

A second, smaller issue surfaced during verification (identical to the one fixed for Epic 1 earlier today): the five Epic 2 retro action items were **never registered** in `sprint-status.yaml`'s `action_items` list, even though `epic-2-retrospective: done` (`sprint-status.yaml:71`). This breaks the retrospective→action-item→closure traceability chain the sprint-status schema is designed to surface.

**Discovery context:** verification grep/read sweep over the six audit-candidate docs, the Epic 3 story files (`3-1`, `3-2`, `3-4`), the UI command-gateway source and tests, the Epic 3 retro closures, and `sprint-status.yaml`.

---

## Section 2 — Impact Analysis

### Epic Impact

- **Epic 2:** `done`; completed as planned. No epic-level modification.
- **Epics 3–5:** all `done`. Epic 3 absorbed every Epic 2 carry-forward risk. No resequencing, new epic, obsoleted epic, or priority change.
- **Net epic impact:** none.

### Per-Action-Item Verification (the core of this proposal)

| # | Action item | Verified status today | Evidence |
|---|---|---|---|
| **AI1** | Update verified documentation drift (Story 2.4 fallback no longer "deferred"; local test guidance documents the xUnit v3 executable fallback) | **DONE** | `README.md:113` and `docs/quickstart.md:391` document the .NET 10 MTP/VSTest incompatibility + xUnit v3 executable fallback. `docs/tenants-ui-remove-user-from-tenant-journey-spec.md:10` carries an "Implementation supersession (Epic 2)" note (`fallbackDecision` "historically `deferred`; superseded for Story 2.4"). `docs/tenants-ui-phase-2-story-backlog.md:21,126` records Story 2.4 superseding the `ui-14` blocked row. `docs/tenants-ui-truth-state-and-action-availability-spec.md:158,167–171,203–208` documents the Epic 2 `duplicate prevented` and `audit delayed` refinements as first-class. `architecture.md` was correctly left unchanged by the retro. |
| **AI2** | Add an Epic 3 command-readiness guardrail before Story 3.1 | **DONE (realized as Story 3.1)** | Story `3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md` AC1 requires availability from "server-side authorization reflection, tenant lifecycle state, freshness, command-surface readiness, and platform-wide governance gate status" and fails closed; tasks name command-surface connection (`ITenantsBffComposition.IsCommandSurfaceConnected`), authorization reflection state, consequence/blocked-state guardrail, and one-at-a-time command lifecycle. Epic 3 `done` (`sprint-status.yaml:74`). |
| **AI3** | Keep shared command status mapping command-neutral; new command stories add regression tests | **DONE** | `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`: `Status_lookup_keeps_shared_rejection_copy_command_neutral` (parameterized over rejection types), `Status_lookup_generic_rejection_stays_command_neutral_for_shared_status_path`, and `Status_lookup_maps_configuration_limit_rejection_to_command_neutral_safe_text` (Story 3.3, a *new* Epic 3 command story). Gateway code (`TenantCommandGateway.cs`) keeps the generic rejected fallback command-neutral so one command's copy never leaks into another's lifecycle panel. |
| **AI4** | Decide durable lifecycle feedback for row-removal happy paths | **SATISFIED in substance (encoded in Epic 3 stories)** | Epic 3's dominant lifecycle flow — disable/enable (`3-2`) — does **not** remove a row: the tenant stays with an updated `TenantStatus` badge, so confirmation feedback is durable by construction. The one row-removing Epic 3 flow — config-key removal (`3-4`) — uses a **flow-scoped** lifecycle panel + dedicated `tenants-config-remove-live-region` with an explicit `Confirmed` state and "keep last-confirmed `TenantDetail.Configuration` visible," i.e. a stable post-confirmation feedback area rather than Epic 2's in-row unmounting panel. No standalone PO/UX decision memo exists; the decision is encoded in the shipped story files (not overclaimed as a separate artifact). |
| **AI5** | Track command-locking symmetry as a shared UI follow-up (common availability signal) | **DONE** | Implemented as the shared `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCommandFlowGuard.cs` with `TenantCommandFlowGuardTests.cs` + `CommandFlowGuardConformanceTests.cs`. Epic 3 retro item `epic-3-retro-2026-06-29-command-lock-retention` is `done` and closed by `sprint-change-proposal-2026-06-30-command-lock-retention-guard-closure.md` (commit `939bebc7`). This is the cross-flow common availability signal AI5 requested. |

### Documentation Audit (six candidates from the retro)

| Doc | Retro finding | Verified today |
|---|---|---|
| `README.md` | recommended `dotnet test` without xUnit fallback + time prediction | **Resolved** — `:113` documents the xUnit v3 executable fallback; no build-time prediction remains. |
| `docs/quickstart.md` | time predictions + missing xUnit fallback | **Resolved** — `:391` documents the fallback (`:142` is a JWT-expiry timestamp, not a build prediction). |
| `docs/tenants-ui-truth-state-and-action-availability-spec.md` | missing duplicate-prevented / audit-delayed refinements | **Resolved** — `:158`, `:167–171`, `:203–208`. |
| `docs/tenants-ui-remove-user-from-tenant-journey-spec.md` | inline fallback still "deferred / not implementation-ready" | **Resolved** — `:10`, `:187`, `:197–203` carry the Story 2.4 supersession. |
| `docs/tenants-ui-phase-2-story-backlog.md` | did not mention Story 2.4 superseding the `ui-14` blocked row | **Resolved** — `:21`, `:126`, `:132`. |
| `_bmad-output/planning-artifacts/architecture.md` | checked, left unchanged | **Still correct** — no change. |

> **Why no doc-body edit is proposed (contrast with the Epic 1 follow-through):** the Epic 1 sweep found one residual sentence asserting a superseded model as *current truth* without a self-flag. Here, every flagged doc already carries an explicit supersession/historical note (the recent 2026-06-29 Epic 3 doc-drift audit and 2026-06-30 config-preview-scope closure swept these in — see the `Jun 29 17:01` / `Jun 30 11:19` mtimes). The `ui-14` row cells in `phase-2-story-backlog.md:101` still read `blocked`/`deferred`, but the file's top-of-section notes (`:13`, `:21`, `:126`, `:132`) explicitly mark the row table historical and name Story 2.4 as superseding it — internally consistent and self-flagged stale, so per the established precedent it is **not** edited.

### Critical Path Before Epic 3 (four retro items)

- Doc drift applied or explicitly deferred → **done** (AI1, above).
- Story 3.1 validates page-level action availability + fail-closed wiring → **done** (Story 3.1 AC1–AC7).
- Story 3.2 does not assume Story 2.4 preview is sufficient → **done** (3.2 ships its own high-impact confirmation + consequence content; `3-4:139` re-derives the destructive rules per flow).
- New command stories preserve the Epic 2 command gateway/lifecycle tests → **done** (`TenantCommandGatewayTests.cs` grew with Epic 3 config/lifecycle coverage; no forked command infrastructure).

### Artifact Conflicts

- **PRD / Architecture / UX specs:** no conflict — all drift reconciled by prior audits; every flagged doc self-flags its historical sections.
- **Tracking artifact:** `sprint-status.yaml` `action_items` was missing the five Epic 2 entries (the single change, below).

### Technical Impact

None. No code, schema, API, test, or CI change. Tracking/traceability only.

---

## Section 3 — Recommended Approach

**Selected path: Option 1 — Direct Adjustment (verify & close).**

- **Rollback (Option 2):** not viable / not needed — nothing to revert; the work is complete.
- **MVP review (Option 3):** not needed — MVP unaffected; Epics 1–5 delivered.

**Rationale:** AI1 is fully resolved across all six audit docs plus README/quickstart; AI2 shipped as Story 3.1; AI3 is enforced by command-neutral regression tests (extended through Epic 3's config command); AI5 shipped as the shared `TenantCommandFlowGuard`; and AI4 is encoded in the Epic 3 story files (disable/enable keeps the row + status; config-removal provides a flow-scoped stable feedback area). The honest action is to record the verified closure for traceability — not to re-open settled or moot items, and not to fabricate doc edits where the docs are already self-consistent.

- **Effort:** Low.
- **Risk:** Low (one YAML append, no source touched).
- **Timeline impact:** none.

---

## Section 4 — Detailed Change Proposals

### Edit 1 — Traceability registration (governance) — APPLIED

- **File:** `_bmad-output/implementation-artifacts/sprint-status.yaml`
- **Section:** `action_items` (appended after the last Epic 1 entry)
- **Rationale:** Register the five Epic 2 retro action items with their verified closure status so the retrospective→action-item→closure chain is complete and `sprint-status` surfaces nothing falsely open. Schema mirrors the existing Epic 1 / Epic 3 / Epic 5 entries, including `resolution_source` where a discrete closing artifact exists.

**APPENDED:**
```yaml
  - id: epic-2-retro-2026-06-06-doc-drift
    epic: 2
    action: "Update verified documentation drift from Epic 2 implementation so docs no longer claim Story 2.4's inline consequence fallback is deferred, and local test guidance documents the xUnit v3 executable fallback."
    owner: "Paige (Technical Writer)"
    status: done
    source: "_bmad-output/implementation-artifacts/epic-2-retro-2026-06-06.md"
    resolution_source: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-30-epic-2-retro-follow-through.md"
  - id: epic-2-retro-2026-06-06-epic-3-command-readiness-guardrail
    epic: 2
    action: "Add an Epic 3 command-readiness guardrail before Story 3.1 so lifecycle/configuration stories name how authorization reflection, command-surface availability, consequence-preview policy, and one-at-a-time locking are wired before implementation."
    owner: "Amelia (Developer) and Winston (System Architect)"
    status: done
    source: "_bmad-output/implementation-artifacts/epic-2-retro-2026-06-06.md"
    resolution_source: "_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md"
  - id: epic-2-retro-2026-06-06-command-neutral-status
    epic: 2
    action: "Keep shared command status mapping command-neutral; new command stories add regression tests proving shared status lookup does not surface another command's copy."
    owner: "Amelia (Developer)"
    status: done
    source: "_bmad-output/implementation-artifacts/epic-2-retro-2026-06-06.md"
    resolution_source: "tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs"
  - id: epic-2-retro-2026-06-06-durable-lifecycle-feedback
    epic: 2
    action: "Decide durable lifecycle feedback behavior for row-removal happy paths; Epic 3 story files either preserve row-disappears-on-confirm intentionally or specify a stable post-confirmation feedback area."
    owner: "Alice (Product Owner) and Sally (UX Designer)"
    status: done
    source: "_bmad-output/implementation-artifacts/epic-2-retro-2026-06-06.md"
    resolution_source: "_bmad-output/implementation-artifacts/3-4-remove-tenant-configuration-key-with-consequence-preview.md"
  - id: epic-2-retro-2026-06-06-command-locking-symmetry
    epic: 2
    action: "Track command-locking symmetry as a shared UI follow-up so member/metadata/lifecycle/configuration flows can drive a common availability signal, or each story records why one-direction locking is sufficient."
    owner: "Winston (System Architect)"
    status: done
    source: "_bmad-output/implementation-artifacts/epic-2-retro-2026-06-06.md"
    resolution_source: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-30-command-lock-retention-guard-closure.md"
```

> No other edit is proposed. All six documentation-audit candidates and the four Critical-Path items are already satisfied (Section 2); proposing doc-body edits there would be redundant or would re-assert already-recorded supersessions.

---

## Section 5 — Implementation Handoff

- **Scope:** Minor — direct implementation (already applied).
- **Recipients:**
  - **Amelia (Developer)** — applied Edit 1 (sprint-status registration).
- **Success criteria:**
  1. `sprint-status.yaml` `action_items` contains five `epic-2-retro-2026-06-06-*` entries, all `status: done`, each with `source` and (where a discrete closing artifact exists) `resolution_source`. ✅ applied.
  2. No code/test/CI change; Tenants build and tests remain green (unchanged — no source touched). ✅ by construction.
  3. No doc-body edit, because every Epic 2 audit candidate is already self-consistent (Section 2). ✅ verified.

---

## Checklist Status (Change Navigation)

- **§1 Trigger & Context:** 1.1 Done · 1.2 Done (category: drift / overtaken-by-events) · 1.3 Done
- **§2 Epic Impact:** 2.1 N/A · 2.2 N/A · 2.3 N/A · 2.4 N/A · 2.5 N/A (Epics 1–5 done)
- **§3 Artifact Conflict:** 3.1 Done (no conflict) · 3.2 Done (already reconciled) · 3.3 Done (no residual doc edit) · 3.4 Action-needed → Edit 1 (applied)
- **§4 Path Forward:** 4.1 Viable (selected) · 4.2 Not viable · 4.3 Not viable · 4.4 Option 1
- **§5 Proposal Components:** 5.1–5.5 Done
- **§6 Final Review:** pending user confirmation
