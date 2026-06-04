---
title: 'FrontComposer Readiness Request — Tenants Management UI (Blocker 1 hand-off)'
date: '2026-06-03'
from: 'Hexalith.Tenants (UI consumer)'
to: 'Hexalith.FrontComposer maintainers + Tenants Product/UX + Hexalith.EventStore maintainers'
project_name: 'Hexalith.Tenants'
purpose: 'Close the single remaining build-start gate for the Tenants Management UI (Blocker 1 of the 2026-06-03 implementation-readiness report).'
related:
  - 'implementation-readiness-report-2026-06-03.md (the 2 Critical blockers)'
  - 'sprint-change-proposal-2026-06-03.md (Blocker 2 resolved)'
  - 'fallback-approval-record-2026-06-03.md (3 fallbacks approved — see "Not requested")'
  - 'docs/tenants-ui-frontcomposer-dependency-map.md (source-of-truth dependency analysis)'
status: 'OPEN — awaiting FrontComposer-team confirmations'
---

# FrontComposer Readiness Request

**This is NOT a "correct course" / sprint change.** Correct Course operates on *this* repo's plan
(already done — Blocker 2 reconciled). The FrontComposer team works in the `Hexalith.FrontComposer`
submodule, so what they need is a **dependency hand-off**: a list of contracts to **confirm** and gaps to
**resolve**, each with a source path and a definition-of-done. That is this document.

## Context (1 paragraph)

The Tenants Management UI is a new Blazor InteractiveServer host that **composes** the FrontComposer Shell;
per repo domain-boundary policy, missing shared UI capability belongs in **FrontComposer, not Tenants**.
The plan is design-complete (PRD + UX + Architecture + Epics, 100% FR coverage). The **only** thing
standing between us and build-start is FrontComposer **contract readiness** — this request.

## Already settled — do NOT redo (so we don't re-litigate)

- ✅ **`FC-TBL` (DataGrid / projection rendering) is available** and consumable today — backbone of all read surfaces. No action.
- ✅ **The three interim fallbacks are Product/UX-approved (2026-06-03)** — see `fallback-approval-record-2026-06-03.md`:
  `FC-AUD` → flat audit DataGrid, `FC-CNS` → inline consequence text, `FC-CNC` → one-at-a-time commands.
  **Therefore the rich `<AuditTimeline>` / `<ConsequencePreview>` components and a toast-batching policy are NOT requested as blockers** — they are post-fallback enhancements for the FrontComposer backlog (fast-follow). See "Not requested now" at the end.

---

## The asks (ordered by what they unblock)

### GROUP 1 — CONFIRM these contracts → unblocks the **read-only MVP** (Epics 1–2)

> These gate **even the read-only MVP**. Confirming them is the highest-leverage action — it moves Epic 1 from `planning-only` to `ready`.

| ID | What to confirm | Confirm against (FrontComposer source) | Specific open question to answer | Done when |
|----|-----------------|----------------------------------------|----------------------------------|-----------|
| **FC-LYT** | The `<PageLayout>` **full-width vs constrained** contract for dense tables, detail views, forms, and standalone/audit views. | `src/Hexalith.FrontComposer.Shell/Components/Layout` + `.../Layout/FrontComposerShell.razor` | Is there a Tenants-compatible full-width/constrained variant contract, or do we use the current shell layout as-is? (Product/UX co-owns the screen-level decision.) | A confirmed layout contract (or "current shell layout is the contract") is documented and citable. |
| **FC-A11Y** | Accessibility primitives for consumed shell deliverables: keyboard, focus visibility, live-region, reduced-motion, forced-colors, contrast. | `docs/how-to/test-generated-components.md` + shell components | Confirm the shell provides these primitives (and which the Tenants story author must supply per screen). | The accessibility-primitive boundary (shell-provided vs Tenants-supplied) is documented. |
| **FC-L10N** | The **shell-owned vs Tenants-owned** string + culture-aware formatting boundary. | `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx` (+ `.fr.resx`) | Which strings/formatting come from `FcShellResources` vs Tenants `.resx`? | The ownership split is documented; Tenants knows which keys to author. |
| **FC-DOC** | Component documentation / reference for each consumed deliverable. | `docs/how-to/test-generated-components.md`, `docs/skills/frontcomposer/domain/projections.md` (Storybook **unverified**) | Confirm reference docs cover the consumed behavior (don't assert Storybook unless a real path exists). | Each consumed deliverable has a citable doc/reference path. |

### GROUP 2 — CONFIRM the command-lifecycle contract → unblocks **commands** (Epics 3–5)

| ID | What to confirm | Confirm against (FrontComposer source) | Specific open questions | Done when |
|----|-----------------|----------------------------------------|-------------------------|-----------|
| **FC-CMD** | The reusable, Tenants-compatible **command-lifecycle** contract: pending-command identity, accepted/terminal handling, idempotent confirmation (`alreadyApplied`), rejected + needs-review outcomes, scope-flush, projection/status reconciliation. | `State/PendingCommands` (incl. `PendingCommandStateService.cs`), `Components/Lifecycle`, `Services/Feedback`, `Components/EventStore/FcPendingCommandSummary.razor`, `Components/Rendering/FcAuthorizedCommandRegion.razor`, `Infrastructure/EventStore` | **(a)** Pending-command **identity / correlation-key shape** — the checkout normalizes to 26 chars, but that shape is **not approved as reusable**. Approve the key shape. **(b)** **Uniqueness scope** — per-tenant, per-user, or per-circuit? (all currently unconfirmed). **(c)** **Lifecycle ownership** + persistence/comparison rules (is pending state circuit-local? what survives a browser refresh?). | A reusable command-lifecycle contract (identity shape, uniqueness scope, ownership, reconciliation) is documented and citable. |

### GROUP 3 — CONFIRM the concurrency contract → unblocks rapid/destructive command sequences

| ID | What to confirm | Status | Done when |
|----|-----------------|--------|-----------|
| **FC-CNC** | That **one-at-a-time commands** is the v1 contract (Product/UX-approved fallback): distinct identities for any overlapping commands, bounded pending caps, duplicate/overflow handling. | Currently `missing`; the **one-at-a-time fallback is already approved for v1**, so this is "confirm you own the policy," not "build batching now." | FrontComposer confirms one-at-a-time as the v1 policy and owns it; toast-batching is logged as a fast-follow enhancement (not a blocker). |

### GROUP 4 — DECIDE numeric budgets (joint: Product/UX + FrontComposer + EventStore)

No numbers are approved yet. Needed before command phases ship (not before the read-only MVP):

| Budget | Owner(s) | Done when |
|--------|----------|-----------|
| `confirming → degraded` threshold (UI patience) | Tenants Product/UX + FrontComposer component default | A concrete threshold (ms) is approved and citable. |
| Polling / status-lookup budget (max attempts, max duration, backoff) | Hexalith.EventStore (status query) + FrontComposer (polling coordinator) | A concrete polling budget is approved. |
| Optimistic-failure revert + retry budget | Tenants Product/UX + FrontComposer lifecycle default | A concrete retry budget is approved. |

### GROUP 5 — EventStore command-status contract (likely already satisfied — confirm)

| Ask | Owner | Note |
|-----|-------|------|
| Confirm the **command-status query contract** the FrontComposer polling coordinator binds to. | Hexalith.EventStore maintainers | The Tenants host already exposes `GET /api/v1/commands/status/{correlationId}` (imported from EventStore). This is a **confirm-the-contract-is-stable** ask, not a build-new-endpoint ask. SignalR is a **nudge only** — never the confirmation source (architecture D2). |

### GROUP 6 — Shell-integration spike (Tenants runs it; FrontComposer answers API questions)

This is **Tenants' new Story 1.0** (added 2026-06-03). It is a Tenants-side verification spike against the
FrontComposer Shell source; the FrontComposer team's role is to **answer API/contract questions** that arise:

- Verify the actual **`AddHexalithFrontComposer*`** registration API(s).
- Verify **manifest registration** and **projection-routing** APIs.
- Re-verify the **`FC-TBL`** contract (DataGrid/projection) against current Shell source.
- (FrontComposer Shell uses **Fluxor**; reference UIs use InteractiveServer — consistent with Tenants D1.)

---

## Not requested now (post-fallback enhancements — FrontComposer backlog, fast-follow)

Because the fallbacks are approved, do **not** treat these as Tenants build blockers:

- **`<AuditTimeline>`** (rich, grouped-by-session) — Tenants ships the **flat audit DataGrid** fallback meanwhile.
- **`<ConsequencePreview>`** component — Tenants ships **inline consequence text** (full 10-item set, fail-closed) meanwhile.
- **`FC-TOK`** timeline-connector / consequence-severity tokens — Tenants uses existing Fluent/FC badge semantics meanwhile.
- **Toast/message batching** (`FC-CNC` rich form) — Tenants uses one-at-a-time meanwhile.

Track these as FrontComposer fast-follow so the Tenants UI can later swap the fallback for the rich component without a re-architecture (the component names already match: `ConsequencePreview`, `AuditDataGrid`).

---

## How to respond (so we can clear the gate)

For each ID above, reply with **a citable evidence reference** — a confirmed contract doc, a source path, or a
FrontComposer story/epic that delivers it — plus an **owner + date**. Tenants will then cite that evidence to
flip the corresponding per-story `Gate:` line from `planning-only`/`blocked` to `ready`.

**Minimum to start coding the read-only MVP:** GROUP 1 (especially **FC-LYT**) confirmed + GROUP 6 spike done.
**Minimum to start command phases:** add GROUP 2 + GROUP 3 + GROUP 4 + GROUP 5.

---

## One-line summary of "what to give the FrontComposer team"

> *"Confirm these reusable contracts against your Shell source — **FC-LYT, FC-CMD, FC-A11Y, FC-L10N, FC-DOC**
> (+ own the **FC-CNC** one-at-a-time policy), decide the **retry/timeout/polling budgets** with Product/UX and
> EventStore, and answer our **Shell-integration spike** questions. We are **not** asking you to build
> `<AuditTimeline>` / `<ConsequencePreview>` now — Product/UX approved the fallbacks; track the rich components
> as fast-follow."*
