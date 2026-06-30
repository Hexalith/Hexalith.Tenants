# Sprint Change Proposal — Epic 1 Retrospective Follow-Through (Verify & Close)

- **Date:** 2026-06-30
- **Trigger artifact:** `_bmad-output/implementation-artifacts/epic-1-retro-2026-06-06.md`
- **Workflow:** Correct Course (`bmad-correct-course`)
- **Mode:** Verify & close (residual-gap audit), batch review
- **Facilitator:** Amelia (Developer)
- **Project lead:** Administrator
- **Change scope classification:** **Minor** (one surgical doc edit + traceability registration)

---

## Section 1 — Issue Summary

The Epic 1 retrospective (`epic-1-retro-2026-06-06.md`) closed five action items (AI1–AI5) and listed six documentation-drift audit candidates. The retrospective is **24 days old**: today is 2026-06-30 and **Epics 1–5 are all `done`** (retros exist for all five; stories run through 5-8), with ~50 sprint change proposals landed since. The retrospective's action items were therefore largely **overtaken by subsequent execution** rather than left open.

A second, smaller issue surfaced during verification: the five Epic 1 retro action items were **never registered** in `sprint-status.yaml`'s `action_items` list (only Epic 3 and Epic 5 retro items are tracked there), even though `epic-1-retrospective: done`. This breaks the retrospective→action-item→closure traceability chain that the sprint-status schema is designed to surface.

**Discovery context:** verification grep/read sweep over the six audit-candidate docs, `architecture.md`, `sprint-status.yaml`, and the Tenants UI source tree.

---

## Section 2 — Impact Analysis

### Epic Impact

- **Epic 1:** `done`; completed as planned. No epic-level modification.
- **Epics 2–5:** all `done`. No resequencing, no new epic, no obsoleted epic, no priority change.
- **Net epic impact:** none.

### Per-Action-Item Verification (the core of this proposal)

| # | Action item | Verified status today | Evidence |
|---|---|---|---|
| **AI1** | Update verified documentation drift (Auto→InteractiveServer, contextual Users, literal IDs, FC-LYT/FC-TBL) | **DONE + 1 residual** | `architecture.md:813` records the reconciliations; `:282` resolves Auto→InteractiveServer; `:144` "NOT ULIDs"; audit-candidate docs carry literal-ID language. **Residual:** `docs/tenants-ui-responsive-layout-and-visual-system-spec.md:30` asserts a nav model that was itself superseded again by the **2026-06-27 tabbed-workspace Option A** — see Edit 1. (Provenance: this residual was introduced by the 2026-06-27 sync missing one file, not by Epic 1 work.) |
| **AI2** | Epic 2 command-foundation readiness check before Story 2.1 | **CLOSED (moot)** | Epic 2 `done` (`sprint-status.yaml:65–66`). Foundations realized in code: `TenantCommandGateway.cs`/`ITenantCommandGateway.cs`, `TenantLifecycleCommandFlow.razor`, command snapshots under `State/`. |
| **AI3** | Promote review findings into a UI pre-review checklist | **SATISFIED in substance** | No standalone checklist artifact, but the findings are enforced by `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` + `project-context.md` testing rule (row headers, surface-specific selectors, fail-closed, support-safe, EN/FR parity, focus/live-region). Enforced beats documented. |
| **AI4** | Track test-environment blockers separately | **DONE (documented)** | `docs/quickstart.md:391` documents the .NET 10 Microsoft.Testing.Platform/VSTest incompatibility + xUnit v3 executable fallback; `docs/deployment-readiness.md` gives per-project run commands; CI runs per-project (project-context). |
| **AI5** | Keep support-safe copy/redaction patterns for Epic 2 | **DONE** | `SupportSafeCopyClassifier.cs` + `SupportSafeCopyButton.razor` reused across `TenantDetailPage`, `GlobalAdministratorsPage`, `TenantAuditPage`, `AuditEvidenceReceipt` (Epics 2–5). Classifier-first pattern preserved. |

### Artifact Conflicts

- **PRD / Architecture:** no conflict — AI1 reconciliations already landed; the tabbed nav model was synced on 2026-06-27.
- **UI/UX specs:** **one** residual conflict — `responsive-layout-and-visual-system-spec.md:30` (Edit 1). Note: `operations-shell-spec.md` keeps its original §1.1 / AC-table body but **self-flags** it stale via the top supersession note (line 14), so it is internally consistent and **not** edited here.
- **Tracking artifact:** `sprint-status.yaml` `action_items` is missing the five Epic 1 entries (Edit 2).

### Technical Impact

None. No code, schema, API, or CI change. Documentation + tracking only.

---

## Section 3 — Recommended Approach

**Selected path: Option 1 — Direct Adjustment (verify & close).**

- **Rollback (Option 2):** not viable / not needed — nothing to revert; the work is complete.
- **MVP review (Option 3):** not needed — MVP unaffected; Epics 1–5 delivered.

**Rationale:** AI2/AI4/AI5 are demonstrably closed by shipped work, AI3 is enforced by tests + project rules, and AI1 is done except for one two-generations-stale sentence. The correct, honest action is to (a) fix the single residual drift and (b) record the verified closure for traceability — not to re-open settled or moot items.

- **Effort:** Low.
- **Risk:** Low (doc + YAML only).
- **Timeline impact:** none.

---

## Section 4 — Detailed Change Proposals

### Edit 1 — Documentation fix (AI1 residual)

- **File:** `docs/tenants-ui-responsive-layout-and-visual-system-spec.md`
- **Section:** "This spec composes existing patterns" → Story 9.2 bullet (line 30)
- **Rationale:** The bullet asserts the pre-2026-06-27 nav model ("primary Tenants, Global Administrators, and Audit, with Users contextual") as current truth, with no reference to the 2026-06-27 tabbed-workspace Option A supersession recorded in `operations-shell-spec.md:14`. The grep confirms this file has zero references to the tabbed model. Mirror the canonical Option A wording.

**OLD:**
> - **Story 9.2** (`docs/tenants-ui-operations-shell-spec.md`) owns the operations navigation model, now superseded by Epic 1 implementation to primary Tenants, Global Administrators, and Audit, with Users contextual. The tenant list remains the default triage surface, command lifecycle is never a separate primary navigation model, and `loading` must stay layout-stable (§1, §2.4). Responsive collapse rules here preserve that context-preservation contract; they do not introduce a new navigation model.

**NEW:**
> - **Story 9.2** (`docs/tenants-ui-operations-shell-spec.md`) owns the operations navigation model. Per the **2026-06-27 Correct Course Option A** supersession recorded in that spec, the shell left navigation exposes **one entry per module**: the single **Tenants** module entry opens the Tenants workspace, which groups Tenants-domain read surfaces as **page-local tabs** (first tab Tenants list/triage, second tab Users lookup; `/tenants/my` and `/tenants/users` are deep-link aliases that set the active tab), with Global Administrators and Audit reached through module-internal/contextual entry points. The tenant list remains the default triage surface, command lifecycle is never a separate primary navigation model, and `loading` must stay layout-stable (§1, §2.4). Responsive collapse rules here preserve that context-preservation contract; they do not introduce a new navigation model.

### Edit 2 — Traceability registration (governance)

- **File:** `_bmad-output/implementation-artifacts/sprint-status.yaml`
- **Section:** `action_items` (append after the last entry, line 175)
- **Rationale:** Register the five Epic 1 retro action items with their verified closure status so the retrospective→action-item→closure chain is complete and `sprint-status` surfaces nothing falsely open. Schema mirrors the existing Epic 3 / Epic 5 entries.

**APPEND:**
```yaml
  - id: epic-1-retro-2026-06-06-doc-drift
    epic: 1
    action: "Update verified documentation drift from Epic 1 implementation so architecture, README, and UI planning specs no longer contradict InteractiveServer, contextual Users, literal IDs, or resolved FC-LYT/FC-TBL decisions."
    owner: "Paige (Technical Writer)"
    status: done
    source: "_bmad-output/implementation-artifacts/epic-1-retro-2026-06-06.md"
    resolution_source: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-30-epic-1-retro-follow-through.md"
  - id: epic-1-retro-2026-06-06-command-foundation-readiness
    epic: 1
    action: "Add an Epic 2 command-foundation readiness check before Story 2.1 (command gateway, lifecycle states, one-at-a-time admission, projection re-query confirmation, safe rejection mapping, audit handoff)."
    owner: "Amelia (Developer) and Winston (System Architect)"
    status: done
    source: "_bmad-output/implementation-artifacts/epic-1-retro-2026-06-06.md"
  - id: epic-1-retro-2026-06-06-ui-pre-review-checklist
    epic: 1
    action: "Promote repeated review findings into a pre-review checklist for UI stories (row headers, aria-describedby, resource parity, surface-specific selectors, false-success fallthroughs, support-safe source checks, File List vs git reality)."
    owner: "Murat (Test Architect)"
    status: done
    source: "_bmad-output/implementation-artifacts/epic-1-retro-2026-06-06.md"
  - id: epic-1-retro-2026-06-06-test-env-blockers
    epic: 1
    action: "Track pre-existing test-environment blockers separately from Epic 1 UI quality (.NET 10 MTP/VSTest behavior, NuGet audit network access, AppHost DAPR component evidence, Server/Integration prerequisite failures)."
    owner: "Winston (System Architect)"
    status: done
    source: "_bmad-output/implementation-artifacts/epic-1-retro-2026-06-06.md"
  - id: epic-1-retro-2026-06-06-support-safe-copy-reuse
    epic: 1
    action: "Keep support-safe copy and redaction patterns (classifier-first) available for Epic 2 command/audit references."
    owner: "Amelia (Developer)"
    status: done
    source: "_bmad-output/implementation-artifacts/epic-1-retro-2026-06-06.md"
```

---

## Section 5 — Implementation Handoff

- **Scope:** Minor — direct implementation.
- **Recipients:**
  - **Paige (Technical Writer)** — apply Edit 1 (doc fix).
  - **Amelia (Developer)** — apply Edit 2 (sprint-status registration).
- **Success criteria:**
  1. `responsive-layout-and-visual-system-spec.md` describes the 2026-06-27 tabbed-workspace nav model; no doc asserts the superseded three-area model as current truth.
  2. `sprint-status.yaml` `action_items` contains five `epic-1-retro-2026-06-06-*` entries, all `status: done`, with sources.
  3. No code/test/CI change; Tenants build and tests remain green (unchanged — no source touched).

---

## Checklist Status (Change Navigation)

- **§1 Trigger & Context:** 1.1 Done · 1.2 Done (category: drift / overtaken-by-events) · 1.3 Done
- **§2 Epic Impact:** 2.1 N/A · 2.2 N/A · 2.3 N/A · 2.4 N/A · 2.5 N/A (Epics 1–5 done)
- **§3 Artifact Conflict:** 3.1 Done (no conflict) · 3.2 Done (already reconciled) · 3.3 Action-needed → Edit 1 · 3.4 Action-needed → Edit 2
- **§4 Path Forward:** 4.1 Viable (selected) · 4.2 Not viable · 4.3 Not viable · 4.4 Option 1
- **§5 Proposal Components:** 5.1–5.5 Done
- **§6 Final Review:** pending user approval (6.3), then apply edits (6.4)
