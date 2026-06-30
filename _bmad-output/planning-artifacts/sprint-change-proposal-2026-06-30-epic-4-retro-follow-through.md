# Sprint Change Proposal — Epic 4 Retrospective Follow-Through (Verify & Close)

- **Date:** 2026-06-30
- **Trigger artifact:** `_bmad-output/implementation-artifacts/epic-4-retro-2026-06-07.md`
- **Workflow:** Correct Course (`bmad-correct-course`)
- **Mode:** Verify & close (residual-gap audit), batch review — matching the same-day Epic 1 and Epic 2 retro follow-through precedent
- **Facilitator:** Amelia (Developer)
- **Project lead:** Administrator
- **Change scope classification:** **Minor** (traceability registration only — no source, no doc-body edit)

---

## Section 1 — Issue Summary

The Epic 4 retrospective (`epic-4-retro-2026-06-07.md`, *Global Administrator Governance*) closed Stories 4.1–4.4, recorded **four action items (AI1–AI4)**, and audited **twelve documentation candidates** (eight "verified discrepancy", four "checked and left unchanged"). The central finding was a **verified FR19 documentation discrepancy**: planning and backlog docs still described FR19 global-administrator command management as `blocked`/`deferred`, even though Stories 4.3 (`SetGlobalAdministrator`) and 4.4 (`RemoveGlobalAdministrator`) had shipped implemented grant/remove flows.

The retrospective is **23 days old**: today is 2026-06-30 and **Epics 1–5 are all `done`** (`sprint-status.yaml`; retros exist for Epics 1–5; stories run through 5-8). Critically, the **Epic 5 retrospective doc-drift action item (2026-06-29)** and the **2026-06-27 doc sync** already swept in the FR19 reconciliation across every flagged document. Every Epic 4 action item was therefore **overtaken by subsequent execution** rather than left open.

A second, smaller issue surfaced during verification (identical to the Epic 1 and Epic 2 follow-throughs earlier today): the four Epic 4 retro action items were **never registered** in `sprint-status.yaml`'s `action_items` list, even though `epic-4-retrospective: done` (`sprint-status.yaml:88`). This breaks the retrospective→action-item→closure traceability chain the sprint-status schema is designed to surface.

**Discovery context:** verification grep/read sweep over the eight audit-candidate docs (`architecture.md`, `addendum.md`, `prd.md`, and the five `docs/tenants-ui-*` specs), the global-administrator command source (`TenantCommandGateway.cs`) and its tests (7 test files), Story 5.7's status, and `sprint-status.yaml`'s `action_items` list.

---

## Section 2 — Impact Analysis

### Epic Impact

- **Epic 4:** `done`; completed as planned (FR18/FR19 in implemented scope). No epic-level modification.
- **Epic 5:** `done`. Story 5.7 absorbed the AI2 verification gate; the Epic 5 retro doc-drift audit absorbed the AI1 doc sync. No resequencing, new epic, obsoleted epic, or priority change.
- **Net epic impact:** none.

### Per-Action-Item Verification (the core of this proposal)

| # | Action item (owner) | Verified status today | Evidence |
|---|---|---|---|
| **AI1** | Synchronize FR19 planning status with implementation evidence (Paige) | **DONE** | `architecture.md:85-86` ("Stories 4.3 and 4.4 now provide FR-19 global-administrator grant/remove command support in the fixed `global-administrators` scope"); `addendum.md:18` ("FR19 was previously blocked, but Epic 4 Stories 4.3 and 4.4 now provide implemented grant/remove … command flows"); `prd.md:321` (Post-Epic 5/Epic 4/Story 5.7 note) and `:404` ("FR-19 (global-admin commands) … are implemented by Epic 4 Stories 4.3 and 4.4"); `docs/tenants-ui-phase-2-story-backlog.md:27,93,102` (`ui-06`/`ui-15` readiness now `implemented`). No flagged doc states FR19 blocked/deferred as **current** truth. |
| **AI2** | Keep global-admin correction blocked until a story verifies the new command dependency; the enabled path must cite Stories 4.3/4.4 and test fixed-scope selection (Winston + Amelia) | **DONE (realized as Story 5.7)** | `sprint-status.yaml:98` `5-7-global-administrator-correction-verification: done`; `prd.md:321,337` and `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md:19,145,160` document Story 5.7 verifying the correction path uses the fixed `global-administrators` command scope and preserves last-admin safety, citing Stories 4.3/4.4. The Epic 5 retro action item `epic-5-retro-2026-06-29-global-admin-correction-verification` is already `done`. |
| **AI3** | Preserve platform-governance-specific rejection copy; tests prove the three rejections do not use tenant-membership wording (Amelia) | **DONE** | Seven test files assert the platform-specific copy: `TenantCommandGatewayTests.cs` (parameterized command-neutral shared-status path → "last global administrator" / "not a global administrator" / "already a global administrator"), `GlobalAdministratorsPageTests.cs`, `GlobalAdministratorCorrectionSnapshotTests.cs`, `GlobalAdministratorGrantCommandSnapshotTests.cs`, `GlobalAdministratorRemoveCommandSnapshotTests.cs`, `GlobalAdministratorsAggregateTests.cs`, and `CommandApiRuntimeIntegrationTests.cs` (rejection ProblemDetails titles `last-global-administrator-rejection`, `global-administrator-not-found-rejection`, `global-administrator-already-exists-rejection`). None reuse tenant-membership wording. |
| **AI4** | Track validation environment blockers separately from story acceptance (Murat) | **SATISFIED (standing convention)** | Encoded in `_bmad-output/project-context.md` (xUnit v3 executable-fallback rule; "document any blocked validation with the exact command and blocker"; CI tier separation) and consistently applied in story files and `tests/test-summary.md`, which keep focused acceptance evidence distinct from pre-existing Server/AppHost/DAPR environment failures. Same item-class as the closed Epic 1 AI4; no discrete artifact — encoded as convention. |

### Documentation Audit (twelve retro candidates)

| Doc | Retro finding | Verified today |
|---|---|---|
| `_bmad-output/planning-artifacts/architecture.md` | FR19 still separately gated by Epic 4 4.3/4.4 | **Resolved** — `:85-86` states 4.3/4.4 now provide FR-19 command support; `:77`, `:404`-equivalent, component map `:655,714` consistent. |
| `…/prds/prd-tenants-2026-06-02/addendum.md` | FR19 remains blocked as current state | **Resolved** — `:18` records the supersession; `:15` row no longer marks the FR-18..19 row blocked. |
| `…/prds/prd-tenants-2026-06-02/prd.md` | global-admin correction fail-closed until 4.3/4.4; FR19 separately gated | **Resolved** — `:321` (Post-Epic 5/Epic 4/Story 5.7 note), `:404` (FR-19 implemented by 4.3/4.4). |
| `docs/tenants-ui-phase-2-story-backlog.md` | `ui-06` planning-only, `ui-15` blocked/deferred without supersession note | **Resolved** — `:27` supersession note; `:93` `ui-06` = `implemented`; `:102` `ui-15` = `implemented`; `:128` Blocked section explicitly excludes `ui-15`. |
| `docs/tenants-ui-truth-state-and-action-availability-spec.md` | command lifecycle table marked `ui-15` blocked | **Resolved** — `:34` Epic 4 supersession note; `:261` `ui-15` = `implemented` / `[]`. |
| `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md` | evidence table marked `ui-15` blocked | **Resolved** — `:18` Epic 4 note; `:186` `ui-15` = `implemented` / `[]`. |
| `docs/tenants-ui-responsive-layout-and-visual-system-spec.md` | responsive table marked `ui-15` blocked | **Resolved** — `:173` `ui-15` = `implemented` / `[]`. |
| `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md` | recovery section said `ui-15` remained blocked | **Resolved** — `:19` Epic 4 supplies grant/remove support; `:160` supersedes the blocked status for `ui-15`. |
| `docs/tenants-ui-operations-shell-spec.md` | left unchanged (read-only-surface section accurate) | **Still correct** — no change. |
| `docs/event-contract-reference.md` | left unchanged (already documents the contracts) | **Still correct** — no change. |
| `README.md`, `docs/quickstart.md` | left unchanged (no Epic 4 setup drift) | **Still correct** — no change. |
| `…/sprint-change-proposal-2026-06-06.md` | left unchanged (historical FR15 proposal) | **Still correct** — historical snapshot. |

> **Why no doc-body edit is proposed (same precedent as Epic 1/2):** all eight "verified discrepancy" candidates were reconciled by the 2026-06-27 doc sync and the 2026-06-29 Epic 5 retro doc-drift audit. The historical *Per-Pattern Consumption Mapping* tables in the UI spec docs still copy `ui-06`/`ui-15` `blockedBy` arrays verbatim as planning-only/blocked (e.g. `accessibility-localization…spec.md:177`, `responsive…spec.md:164`, `truth-state…spec.md:247`), but each such table is explicitly self-flagged as a historical snapshot whose readiness "may be superseded by later implementation evidence", and each file's top-of-section note names Epic 4 as superseding the row. Internally consistent and self-flagged stale → per the established precedent it is **not** edited.

### Next-Epic Preparation (retro §"Next Epic Preparation")

The retro's forward dependency — *global-administrator correction enablement reusing fixed-scope command support without tenant-membership conflation* — is **closed by Story 5.7** (`done`), which verifies command selection picks `SetGlobalAdministrator`/`RemoveGlobalAdministrator` in the `global-administrators` domain, preserves projection-confirmed success and audit/proof honesty, and keeps last-admin removal unavailable before submit / rejected after race conditions. AI3's test set covers the focused command-selection / fail-closed / support-safe assertions the retro requested.

### Artifact Conflicts

- **PRD / Architecture / UX specs:** no conflict — all FR19 drift reconciled by prior audits; every flagged doc self-flags its historical sections.
- **Tracking artifact:** `sprint-status.yaml` `action_items` was missing the four Epic 4 entries (the single change, below).

### Technical Impact

None. No code, schema, API, test, or CI change. Tracking/traceability only.

---

## Section 3 — Recommended Approach

**Selected path: Option 1 — Direct Adjustment (verify & close).**

- **Rollback (Option 2):** not viable / not needed — nothing to revert; the work is complete.
- **MVP review (Option 3):** not needed — MVP unaffected; Epics 1–5 delivered.

**Rationale:** AI1 is fully resolved across architecture, PRD, addendum, the backlog, and all four UI spec docs; AI2 shipped as Story 5.7 with the fixed-scope verification gate; AI3 is enforced by platform-specific rejection-copy tests across seven files (including the command-neutral shared-status path); and AI4 is satisfied as a standing convention codified in project-context and applied per story. The honest action is to record the verified closure for traceability — not to re-open settled or moot items, and not to fabricate doc edits where the docs are already self-consistent.

- **Effort:** Low.
- **Risk:** Low (one YAML append, no source touched).
- **Timeline impact:** none.

---

## Section 4 — Detailed Change Proposals

### Edit 1 — Traceability registration (governance) — APPLIED

- **File:** `_bmad-output/implementation-artifacts/sprint-status.yaml`
- **Section:** `action_items` (appended after the last Epic 2 entry)
- **Rationale:** Register the four Epic 4 retro action items with their verified closure status so the retrospective→action-item→closure chain is complete and `sprint-status` surfaces nothing falsely open. Schema mirrors the existing Epic 1 / Epic 2 / Epic 3 / Epic 5 entries, including `resolution_source` pointing at the discrete closing artifact for each.

**APPENDED:**
```yaml
  - id: epic-4-retro-2026-06-07-fr19-planning-sync
    epic: 4
    action: "Synchronize FR19 planning status with implementation evidence so architecture, PRD/addendum, and backlog docs no longer say FR19 is blocked/deferred as current state after Stories 4.3 and 4.4."
    owner: "Paige (Technical Writer)"
    status: done
    source: "_bmad-output/implementation-artifacts/epic-4-retro-2026-06-07.md"
    resolution_source: "_bmad-output/implementation-artifacts/epic-5-retro-2026-06-29.md"
  - id: epic-4-retro-2026-06-07-global-admin-correction-gate
    epic: 4
    action: "Keep global-admin correction blocked until a story verifies the new command dependency; any enabled global-admin correction path must cite Story 4.3 and 4.4 command support and test fixed-scope command selection."
    owner: "Winston (System Architect) and Amelia (Developer)"
    status: done
    source: "_bmad-output/implementation-artifacts/epic-4-retro-2026-06-07.md"
    resolution_source: "_bmad-output/implementation-artifacts/5-7-global-administrator-correction-verification.md"
  - id: epic-4-retro-2026-06-07-platform-rejection-copy
    epic: 4
    action: "Preserve platform-governance-specific rejection copy; command/status tests prove GlobalAdministratorAlreadyExists, LastGlobalAdministrator, and GlobalAdministratorNotFound do not use tenant-membership wording."
    owner: "Amelia (Developer)"
    status: done
    source: "_bmad-output/implementation-artifacts/epic-4-retro-2026-06-07.md"
    resolution_source: "tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs"
  - id: epic-4-retro-2026-06-07-validation-env-blockers
    epic: 4
    action: "Track validation environment blockers separately from story acceptance; future stories keep focused acceptance evidence distinct from pre-existing Server/AppHost/DAPR failures."
    owner: "Murat (Test Architect)"
    status: done
    source: "_bmad-output/implementation-artifacts/epic-4-retro-2026-06-07.md"
    resolution_source: "_bmad-output/project-context.md"
```

> No other edit is proposed. All twelve documentation-audit candidates and the retro's Next-Epic-Preparation dependency are already satisfied (Section 2); proposing doc-body edits there would be redundant or would re-assert already-recorded supersessions.

---

## Section 5 — Implementation Handoff

- **Scope:** Minor — direct implementation (already applied).
- **Recipients:**
  - **Amelia (Developer)** — applied Edit 1 (sprint-status registration).
- **Success criteria:**
  1. `sprint-status.yaml` `action_items` contains four `epic-4-retro-2026-06-07-*` entries, all `status: done`, each with `source` and `resolution_source`. ✅ applied (YAML re-parsed clean; `action_items` count = 26, epic-4 = 4).
  2. No code/test/CI change; Tenants build and tests remain green (unchanged — no source touched). ✅ by construction.
  3. No doc-body edit, because every Epic 4 audit candidate is already self-consistent (Section 2). ✅ verified.

---

## Checklist Status (Change Navigation)

- **§1 Trigger & Context:** 1.1 Done · 1.2 Done (category: drift / overtaken-by-events) · 1.3 Done
- **§2 Epic Impact:** 2.1 N/A · 2.2 N/A · 2.3 N/A · 2.4 N/A · 2.5 N/A (Epics 1–5 done)
- **§3 Artifact Conflict:** 3.1 Done (no conflict) · 3.2 Done (already reconciled) · 3.3 Done (no residual doc edit) · 3.4 Action-needed → Edit 1 (applied)
- **§4 Path Forward:** 4.1 Viable (selected) · 4.2 Not viable · 4.3 Not viable · 4.4 Option 1
- **§5 Proposal Components:** 5.1–5.5 Done
- **§6 Final Review:** pending user confirmation
