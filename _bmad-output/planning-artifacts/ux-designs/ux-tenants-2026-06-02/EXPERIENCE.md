---
name: Tenants Management UI
status: final
sources:
  - "{planning_artifacts}/prds/prd-tenants-2026-06-02/prd.md"
updated: 2026-06-02
---

# Tenants Management UI — Experience Spine

> Trust-first operations console for the Hexalith Tenants domain. Distilled from `.decision-log.md` + the PRD UX digest (`.working/prd-ux-digest.md`) + sources. The spines win on conflict with any mock or import; **EXPERIENCE.md wins on behavior**, `DESIGN.md` wins on visuals. This is a **PLAN, not build-ready**: nothing here is buildable until the `FC-LYT` / `FC-CMD` / `FC-CNC` readiness gates clear (see *FrontComposer Readiness & Fallbacks*).

## Foundation

Desktop-first responsive web — the primary surface is a dense admin workstation (keyboard/mouse, side-by-side context); responsiveness exists to *prevent breakage at narrower widths*, not to re-target phones (see *Responsive & Platform*).

**UI system (inherited, not invented).** Microsoft Fluent UI Blazor v5 is the component source and visual authority, composed through the **FrontComposer shell** (the Operations Shell IA). There is no bespoke brand palette; meaning maps to Fluent semantic theme roles. `DESIGN.md` is the visual reference and owns every visual spec (color roles, badge appearance, density, the component visuals). This spine specifies **behavior, state, flow, and accessibility** only — it references DESIGN.md components by name and never restates their visuals.

**The honesty contract (the product's whole thesis).** This console is *honest about state* in an eventually-consistent, event-sourced system:

- **Projection is the source of truth.** The UI confirms every command and every audit fact by re-querying the projection. It never advances state from optimistic UI.
- **SignalR notifications are freshness NUDGES ONLY.** A live notification may prompt a refresh; it **never** advances a command to `confirmed` or audit to `audit available`. (CP-4.)
- **Never show unconfirmed success** — not in styling, copy, or announcement — that has not been confirmed against the projection. (CP-3.)
- **Never collapse distinct states** (CP-3): `accepted` ≠ `confirmed` ≠ `audit available`; `degraded` and `unable to verify` are distinct and success-prohibited.
- **Correct forward, never undo** (CP-7): corrections are new auditable compensating commands; history is never edited or deleted.

**Blazor Auto lifecycle constraint.** The app runs under Blazor Auto (prerender → Server circuit → WASM, with reconnect). State must be **correct under at-least-once delivery and projection lag** (NFR-3): no prerender or reconnect may resurrect a stale optimistic "success," and a circuit reconnect re-derives truth from the projection, never from a cached in-flight assumption. Pending/in-flight indicators survive reconnect as *pending*, never silently promoted to confirmed. *(Reconciliation note, 2026-06-03: architecture **D1 supersedes the "Auto" assumption with Blazor InteractiveServer + a server-side BFF** — a recorded divergence, not a contradiction; the at-least-once / projection-lag invariants above (NFR-3) hold identically under InteractiveServer.)*

## Information Architecture

Anchored to the **Operations Shell**. Primary navigation homes, in order: **Tenants** (default landing/triage) · **Global Administrators** · **Audit**. **Users is CONTEXTUAL** — reached from a member row and from global search, *not* a co-equal nav tab (decision log 2026-06-02; resolves operations-shell GAP-10). Command lifecycle is **never** a nav area: it is shown inline, anchored to the affected row/panel.

| Surface | Reached from | Purpose |
|---|---|---|
| **Tenant list / triage** | App open (default landing) · Tenants nav | Scan/search/filter/sort/page tenants via cursor pagination; default triage surface (FR-1). |
| **Tenant detail** | Tenant list row · deep link | Read-only single-tenant overview: status, metadata, member/configuration summaries, counts, freshness; deep-linkable. Returning to the list restores filter/sort/selection (FR-2, FR-5). |
| **Tenant configuration view (read-only)** | Tenant detail | Key/values grouped by namespace, filtered to the caller's owned/authorized prefix (FR-6). |
| **"My Tenants" self-audit** | Signed-in user (own identity) | The signed-in user's own memberships + role per tenant (FR-3). |
| **User lookup / user memberships** | A member row · global search (**contextual**) | Operator searches a user, views that user's tenant memberships (FR-4). |
| **Member table / access-review** | Tenant detail | Per-tenant members with role, owner count, status, freshness, orphan/disabled context; read-only ("must not imply mutation"); per-row reflected action availability + Unavailable Action Reason (FR-8, FR-9). |
| **Global Administrators review** | Global Administrators nav (authorized operators only) | Who holds global-admin access; data from the single fixed-identity `global-administrators` aggregate (FR-18). Never visible to owners. |
| **Audit trail list** | Audit nav · tenant row · tenant detail · user lookup · command result | Flat, stably-ordered, cursor-paginated list; date + `AuditEventCategory` filters (FR-20, FR-21). In MVP renders an honest not-yet-available placeholder (see *State Patterns*). |
| **Audit Evidence Receipt** | Audit row · command result | Support-safe receipt for a recorded action, assembled client-side from `NarrativePayload` (FR-22). |
| **Consequence Preview** | A high-impact/destructive command trigger (anchored, after gating passes) | Pre-submission summary carrying the full 10-item content set before destructive/high-impact actions (CP-5, FR-12/15/16/17). |
| **Command Lifecycle Panel** | Inline, anchored to the affected row/panel after dispatch | Tracks a dispatched command's lifecycle without overwriting confirmed projection data (FR-12). **Never a nav area.** |
| **Command flow surfaces** (custom, not generated CRUD) | The affected row/panel/detail | Add user (FR-10), Change role (FR-11), Remove user (FR-12), Create tenant (FR-13), Edit metadata (FR-14), Disable/Enable tenant (FR-15), Set/Remove configuration (FR-16/17), Grant/Remove global administrator (FR-19), Start/Preview compensating command (FR-24/25). |

**Context preservation.** Selection, filters, and scroll are preserved across navigation so "users don't lose their place." Returning from a tenant detail to the list restores the prior filter/sort/selection.

→ Composition reference: [`mockups/mock-tenant-list.html`](mockups/mock-tenant-list.html), [`mockups/mock-consequence-preview.html`](mockups/mock-consequence-preview.html), [`mockups/mock-command-lifecycle.html`](mockups/mock-command-lifecycle.html) (illustrative — they approximate Fluent UI Blazor v5). **Spine wins on conflict.**

## Voice and Tone

Microcopy is calm, precise, and honest. Brand voice and aesthetic posture live in `DESIGN.md`.

| Do | Don't |
|---|---|
| "Change submitted. Waiting for the projection to confirm." | "Done!" / "Success ✓" (announcing success before it is proven) |
| "Confirmed against the source of truth." | "Saved successfully" (when only `accepted`, not `confirmed`) |
| "Audit record available." | "Audited ✓" (when state is `audit pending`) |
| "Couldn't verify the result. Escalate with this reference." (`unable to verify`) | "Probably worked." / any success styling on `unable to verify` |
| "Already applied — no change was needed." | "Error" / "Failed" (for an idempotent `already applied`) |
| "Data is stale — refresh first." (inline, hover-free) | "Something went wrong." |
| "You don't have permission for this." | "Forbidden 403" / a stack trace |
| "Restore intended access." / "Start a correction." | "**Undo**" · "**Rollback**" · "**hidden edit**" (PROHIBITED words) |
| Whole-string resources with named placeholders: `"{userName} no longer has access to {tenantName}."` | Runtime sentence-fragment assembly (concatenating localized fragments) |
| Speak to operators and owners the same way — plain language, no audience-specific tone. | Different register per persona. |

**Prohibited vocabulary (hard rule):** `undo`, `rollback`, `hidden edit` never appear in any copy, label, tooltip, or announcement. Corrections are `start correction` / `restore intended access` / `start a compensating command`. **Localization:** every state label, role name, timestamp, warning, disabled reason, recovery action, confirmation copy, and empty/loading/error/degraded/stale/unavailable string is localizable and culture-aware, expressed as a **whole resource string with named placeholders** — never assembled from fragments at runtime. [NOTE FOR UX] Resource ownership (shared FrontComposer shell resources vs. Tenants-owned keys) is open (Open Q#4) and routed to architecture; the no-fragment-assembly constraint holds regardless of ownership.

## Component Patterns

Behavioral rules only. Visual specs for every named component live in `DESIGN.md.Components`. Component names below are the cross-ref contract.

| Component | Behavioral rules |
|---|---|
| **truth-state-badge** | Composes the freshness + authorization + command-lifecycle + projection-confirmation + audit dimensions. The **13-token Truth State Badge set** (*State Patterns* §1) is the badge's own composite display vocabulary; the **exhaustive per-state → role + glyph binding across all six status vocabularies** is DESIGN.md's verified status-icon table — build the badge from that table, not from the 13-token set alone. Never collapses two dimensions into one badge. Carries an accessible name (role + state) and an absolute timestamp where freshness applies. Status is **never color-only** — DESIGN.md pairs each with icon + text; the behavioral guarantee here is that the state token/text is always present for assistive tech. |
| **tenant-data-grid** | Cursor pagination only (never offset/limit). Required columns: tenant identity, status, member count, owner count, pending state, truth-state-badge with freshness. Renders the **six non-collapsible list-surface states**. **Sorting/paging must never hide a pending or stale marker** (GAP-4). **Row actions stay stable in width and placement** under data/sort/page change (GAP-9). On cursor invalidation, re-query page 1 with an honest "list refreshed" notice (not an error). |
| **member-table** | Read-only; **must not imply mutation**. Exposes table semantics (headers, sort state, row relationships) to assistive tech. Shows per-member role, owner count, status, freshness (truth-state-badge), orphan/disabled context. Each row reflects which actions *would* be available; where one is not, shows the **unavailable-action-reason** inline (not hover-only). It only *reflects* server-enforced authorization (CP-9) — it never enforces. |
| **audit-data-grid** | Flat, stably-ordered, cursor-paginated (the approved `FC-AUD` fallback). Date + `AuditEventCategory` (`Access`/`Administrative`) filters. States: loading / empty / filtered-empty / error (distinct). `filtered-empty` offers a clear filter reset. In MVP this surface renders the not-yet-available placeholder instead of live content. |
| **consequence-preview** | Opens **only after** validation + freshness + authorization gates are `eligible` (fail-closed). Carries the **full 10-item content set** (*State Patterns* / *UJ-3*). **If any of the 10 items is unavailable, submission is blocked** (fail-closed) and the missing item is named. Required for every config edit (FR-16/17) and every destructive/high-impact action. Previews **against current state** (the prior effect may already differ). Modal: focus-trapped with a safe escape that does **not** commit. |
| **command-lifecycle-panel** | Tracks the dispatched command inline, anchored to the affected row/panel, **without overwriting confirmed projection data**. Steps through the lifecycle tokens distinctly; **never collapses** `accepted`/`confirmed`/`audit available`. SignalR may nudge a re-query but never advances the panel's state. Duplicate submit / browser refresh during pending de-duplicates (no double-apply). |
| **audit-evidence-receipt** | Support-safe only: actor, target, tenant scope, outcome, timestamp, projection marker, audit/command reference — assembled client-side from `NarrativePayload` (target resolution `userId` → `key` → `TenantId`). **Never** exposes raw payloads, tokens, correlation ids, raw event metadata, or PII. On partial completion shows the *actual* lifecycle state (e.g. `audit pending`) — **never** pre-rendered proof. |
| **unavailable-action-reason** | Renders one of the **6 canonical categories** as plain, localized, **inline-visible** text (a tooltip may supplement but is never the only explanation). **Programmatically associated** with the row/action it explains (e.g. `aria-describedby`) and reachable by keyboard/screen-reader — not visually-inline only. Stable automation selector; never keyed on row text or color. |
| **primary-command-button** | The eligible primary affordance for *low-risk* command flows. Disabled when its gate is not `eligible`, with the reason inline. One-at-a-time: disabled while any command is in flight. |
| **destructive-control** | **Not a primary/casual button.** Distinguished, deliberately higher-friction (DESIGN.md owns the visual treatment). Behavior: requires the consequence-preview to have opened and passed; triggers **elevated-friction confirmation** for the zero-owner OR target-also-global-admin cases. A safe escape from its modal does **not** commit. |

## State Patterns

**Canonical state sets are used VERBATIM, no per-screen reinterpretation (CP-10).** Casing is significant and intentional. The Truth State Badge uses space-form `audit pending` / `audit available`; the RemoveUserFromTenant state machine uses snake_case `projection_pending` / `audit_pending` / `audit_available`. **These are the same concepts, different tokens — do not unify.** Reason categories and feedback states are lowercase, space-separated. (Reproduced from digest §6 / addendum §G.)

**1. Truth State Badge — 13 states:** `current`, `refreshing`, `aging`, `stale`, `unknown`, `eligible`, `blocked`, `pending`, `accepted`, `confirmed`, `failed`, `audit pending`, `audit available`. (This is the badge's composite display set; the full state→role/glyph binding spanning all six vocabularies below is DESIGN.md's status-icon table.)

**2. Freshness — 5 states:** `current` (usable), `refreshing` (usable nudge), `aging` (**usable with friction**), `stale` (**blocks** high-impact action), `unknown` (**blocks**, fails closed). Numeric `current`/`aging`/`stale` thresholds are deferred — **no magic numbers in this spine** (Open Q#10): they must be **configurable and honestly surfaced** (state badge + absolute timestamp; `unknown` when unmeasurable).

**3. Command lifecycle — 10 tokens:** `eligible`, `previewed`, `submitted`, `accepted`, `rejected`, `already applied`, `failed`, `duplicate`, `timeout`, `unknown`. (`duplicate` and `timeout` have **no dedicated gloss — do not invent one**.) The RemoveUserFromTenant worked state machine additionally uses snake_case `projection_pending`, `confirmed`, `audit_pending`, `audit_available`.

**4. Layered feedback — 10 states (must not be merged):** `request sent (submitted)`, `accepted`, `projection pending`, `confirmed`, `rejected`, `already applied`, `degraded` (**success-prohibited**), `audit pending`, `audit available`, `unable to verify` (**success-prohibited**). Invariant: `accepted` ≠ `confirmed` (projected) ≠ `audit available` (proven).

**5. List-surface states — six distinct, non-collapsible (FR-1):** **loading**, **empty**, **filtered-empty**, **error**, **stale**, **degraded** — "none may be collapsed into another." `filtered-empty` offers a clear filter reset; `stale` shows a freshness marker + refresh path; `degraded` explains what is unavailable and what still works. Empty states are authorization-safe (no leak of out-of-scope tenants/members; FR-4 `empty` ≠ `error`).

**6. Audit availability — 4 states (do not collapse):** `audit pending` (proof not yet available, expected), `audit delayed` (taking longer, capability exists), `audit unavailable` (path currently unavailable, e.g. read error — distinct from not-built), `missing implementation support` (capability not built — the `FC-AUD` dependency). **None is ever shown as success.** (Audit *list* states, FR-20: loading / empty / filtered-empty / error.)

**7. Unavailable Action Reason — 6 categories (inline-visible, not hover-only):** `missing permission` (authorization gate), `stale data` (freshness gate), `missing lifecycle support` (→ `FC-CMD`), `missing consequence preview` (→ `FC-CNS`), `missing audit proof` (→ `FC-AUD`), `high-impact flow not ready` (unresolved backlog `blockedBy`).

**8. Recovery verbs — canonical allowed terms:** `start correction`, `restore intended access`, `retry status lookup`, `inspect audit`, `escalate` (with a support-safe reference); plus general paths `refresh`, `wait`, `continue read-only`, `request permission`, `start a compensating command`; plus (per reconcile) `reassign tenant owner`, `retry access removal`. **Prohibited:** `undo`, `rollback`, `hidden edit`.

**Risk (derived, not stored) — `low` / `high`.** Risk is *computed*, not a persisted state: `high` when an action would drop a tenant's owner count to zero OR the target also holds global-administrator authority; `low` otherwise. It surfaces in the **member-table action context** and the **consequence-preview** (the target's platform standing), pinned **where shown** — it is **not** a standalone column on the tenant grid in v1. DESIGN.md binds `risk high`→Danger, `risk low`→Subtle.

**Non-collapse invariant (CP-3).** `accepted` ≠ `confirmed` (projected) ≠ `audit available` (proven); `degraded` and `unable to verify` are distinct and **success-prohibited**. The UI never shows success — styling, copy, or announcement — that it has not confirmed against the source-of-truth projection. `pending` is an in-flight, not-yet-confirmed indicator on the affected row/panel.

**Eventual-consistency signals (CP-4).** SignalR projection notifications are **freshness nudges only** — they never advance a command to `confirmed` or audit to `audit available`. The projection is the source of truth. **TenantDisabled/TenantEnabled:** disabled is an eventually-consistent availability signal; commands targeting a disabled tenant are **rejected** (`TenantDisabled`) and surfaced as safe localized text with no-color-only encoding.

**Rejection / NoOp surfacing.** Business-rule refusals return as domain rejection events → rendered as **safe, localized text only** (never a stack trace; RFC 7807 lives at the HTTP boundary). Named rejections include `UserAlreadyInTenant`, `RoleEscalation`/`Unknown` target, `ConfigurationLimitExceeded`, `ConfigurationKeyNotFound`, `TenantLifecycleStateAlreadySet`, `TenantDisabled`, `TenantAlreadyExists`, `LastGlobalAdministrator`. **NoOp** cases — `ChangeUserRole` to current role; `SetTenantConfiguration` identical key+value — surface as `already applied`. **Re-adding an existing member is NOT a NoOp — it is rejected (`UserAlreadyInTenant`).** Editing metadata always emits `TenantUpdated` (no same-state suppression) → success only after projection confirm.

**Recovery mapping (CP-8) — every failure → a distinct recovery verb, NEVER a dead end:**

| Failure mode | Recovery |
|---|---|
| `stale` data | `refresh` |
| `pending` (in flight) | `wait` |
| status-lookup failure / `unable to verify` | `retry status lookup`; `escalate` (with support-safe reference) |
| `missing permission` (lost mid-flow or never held) | `request permission` / `escalate`; `continue read-only` |
| wrong change made | `start correction` / `restore intended access` / `start a compensating command` |
| `already applied` | `inspect audit`; `continue read-only` |
| last-owner removed, restore needed | `reassign tenant owner` / `restore intended access` (relies on empty-tenant bootstrap, `HasMembershipHistory == false`) |
| removal didn't land | `retry access removal` |
| capability not built (`missing implementation support`) | honest not-yet-available state + the reason; `continue read-only` |

## Interaction Primitives

**Keyboard-first.** The audience is expert operators under incident pressure; every workflow is fully keyboard-operable.

- **Focus return:** focus returns to the launching row/control after a modal/preview/command workflow **closes, cancels, submits, or fails**.
- **Modal focus trap with a safe escape:** every modal (consequence-preview, destructive confirmation) traps focus; `Esc`/cancel provides a safe escape that **does NOT commit** a destructive action.
- **One-at-a-time commands (FC-CNC fallback):** no concurrent command submission, no toast-batching, no multi-row bulk actions in v1. While a command is in flight, other command triggers are unavailable with a stated reason.
- **FAIL-CLOSED gating (ordering is load-bearing):** validation **+** freshness **+** authorization must all be `eligible` **BEFORE** the consequence-preview opens — not only at submit. If freshness is `stale`/`unknown`, authorization indeterminate, the consequence preview incomplete, or lifecycle support missing, the action is **blocked** (fail-closed) with an inline reason.
- **Copy-full-id (FR-7):** copies the **literal caller-supplied string** TenantId/UserId and any support-safe reference. **Never** `Guid.TryParse`/`Ulid.TryParse` — these ids are meaningful strings, not ULIDs/GUIDs. Cursors are opaque/signed/session-scoped (never offset/limit); never copied as user-facing ids.
- **Automation (NFR-4):** every interactive element and status carries a **stable automation selector / component contract**, never keyed on row text or color.

**Banned in v1:** optimistic success display; concurrent/bulk command submission; toast-batching; hover-only disabled-action explanations; success announcement before projection confirmation; the words `undo`/`rollback`/`hidden edit`.

## Accessibility Floor

Behavioral floor. Visual contrast / forced-colors rendering lives in `DESIGN.md`.

- **Standard:** baseline **WCAG 2.1 AA**; target **WCAG 2.2 AA where the pinned Fluent UI Blazor / FrontComposer stack supports it** — conditional, no unconditional 2.2 promise (Open Q#5).
- **Complete-OR-exit guarantee:** keyboard users can **complete OR exit** every modal / preview / table / command workflow. This is a standalone obligation (reconcile-a11y Gap 4), not a side effect of focus order.
- **Focus:** all interactive elements reachable; logical focus order matching reading order; visible focus in normal / high-contrast / forced-colors; modal focus trap with a safe non-committing escape; focus returns to the launching row/control after close/cancel/submit/failure.
- **Live-region politeness:** politeness binds to a **dedicated announcement-intent field — never derived from `BadgeColor` / `MessageBarIntent`.** Deriving it from color would *over-announce* routine `risk high` badges and the resting destructive-control, and *miss* the real assertive triggers (which are Important/Severe-colored, not Danger). `AriaLive.Assertive` is **reserved** for rejection / failure / `unable to verify` / `degraded` / destructive-block. `AriaLive.Polite` for everything else. **Never announce success before projection truth.**
- **Timestamps:** **absolute** (not relative-only) for all freshness/audit times, with culture-aware formatting; accessible names for all statuses, badges, freshness indicators, and actions.
- **Table semantics:** headers, sort state, and row relationships exposed to assistive tech on every table (tenant-data-grid, member-table, audit-data-grid).
- **Disabled-action reasons:** **inline-visible**, not hover-only (a tooltip may supplement but cannot be the only explanation).
- **No-color-only:** color is never the sole signal (DESIGN.md adds icon + text/shape); behavioral guarantee here: the state token/text is always present for assistive tech in light/dark/high-contrast/forced-colors.
- **Reduced motion:** users who request reduced motion are never dependent on animation to perceive a state change.
- **Ready-gate evidence set (reconcile-a11y Gap 1).** A UI story cannot be `ready` until it cites applicable **accessibility, localization, responsive, and documentation/reference (`FC-DOC`)** evidence — or records a Product/UX-approved row-specific fallback documenting five things: keyboard/focus/live-region behavior, localizable-copy responsibility, documentation/reference (`FC-DOC`) evidence, replacement path, and owner approval. **Required acceptance scenarios:** stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, permission-missing; **plus keyboard-only complete-OR-exit of the consequence-preview and destructive-confirmation (focus trap, safe non-committing escape, focus return), forced-colors rendering of the status-icon set, and live-region politeness (assertive fires on rejection/failure/`unable to verify`/`degraded`/destructive-block; NO assertive on a resting destructive-control or a `risk high` badge).** Screen-reader review uses **NVDA + at least one browser/SR pairing**. Responsive evidence must include **horizontal table overflow, navigation collapse, and command-preview/dialog behavior at narrow widths**. Keyboard/focus and forced-colors evidence depend on `FC-A11Y` (needs-confirmation).

## Truth & Honesty Invariants

First-class rules. Every surface, flow, and component above inherits these; they override any convenience pattern.

- **CP-3 — Non-collapse.** `accepted` ≠ `confirmed` (projected) ≠ `audit available` (proven). `degraded` and `unable to verify` are distinct, **success-prohibited** states. The UI never renders success — in styling, copy, or announcement — that it has not confirmed against the source-of-truth projection. Partial completion shows the actual lifecycle state, never pre-rendered proof.
- **CP-4 — SignalR nudge-only.** Live projection notifications are **freshness nudges only**; they prompt a re-query but **never** advance command lifecycle or audit availability. The projection is the source of truth. Correct under at-least-once delivery + projection lag (Blazor Auto reconnect re-derives, never resurrects optimistic success).
- **CP-7 — Correct forward, never undo.** Recovery is always a forward **compensating command** with its own consequence-preview and proof; the original event is untouched; original and corrective records reference each other. The UI never labels this `undo`, `rollback`, or `hidden edit`. The correction previews **against current state**. (Restore-after-last-owner relies on the empty-tenant bootstrap, `HasMembershipHistory == false`.)
- **CP-8 — Recovery completeness.** Every failure mode maps to a **distinct named recovery verb** (see the recovery table) — **never a dead end**.
- **CP-10 — Canonical vocabulary, verbatim.** The canonical state sets are used as-written, casing-significant, with no per-screen reinterpretation and no unification of badge spaced-forms vs. state-machine underscore-forms.

[NOTE FOR UX] CP-9 (the UI only *reflects* server-enforced authorization, never enforces it) is encoded behaviorally in member-table and unavailable-action-reason; it is an authorization invariant rather than an honesty-of-state one, so it is folded into those components rather than listed here as a peer.

## Key Flows

Named-protagonist journeys (Elena, Sofia, Nadia, Marc). Phase labels (2a/2b/2c) mark when each becomes real. Each has an explicit **CLIMAX** beat mirroring the digest/PRD wording. **Coverage:** every IA surface is landed by at least one journey (see the coverage check at the end).

### UJ-1 — Elena triages tenants under pressure *(Phase 2a / MVP)*

*Elena, platform operations engineer, gets a report that "a tenant is acting up."*

1. Elena is authenticated as a global administrator and lands on the **Tenant list / triage** surface (default landing).
2. She filters/searches the list and scans status, owner/member counts, and freshness (truth-state-badge per row).
3. She opens the suspect tenant's **Tenant detail**.
4. She returns to the list — her filters and selection are preserved.
- **CLIMAX:** she has the right tenant open with its current state in front of her, and **knows how fresh that state is**.
- Resolution: proceeds to access review (UJ-2). **Edge:** an unmeasurable freshness value shows `unknown` rather than implying `current`.

### UJ-2 — Elena reviews who can do what *(Phase 2a / MVP — read; action availability reflected)*

*Continuing from UJ-1.*

1. From **Tenant detail**, Elena opens the **Member table / access-review** surface.
2. She reads each member's role, owner count, status, and freshness.
3. Per row, she sees which actions *would* be available; where one is not, a plain-language **unavailable-action-reason** (e.g. "you don't have permission", "data is stale — refresh first") shows inline.
- **CLIMAX:** she understands the access picture and **exactly what she could safely change**.
- Resolution: in MVP she stops here (read-only); later she proceeds to UJ-3.

### UJ-3 — Elena safely removes a user's access *(Phase 2c)* — **FLAGSHIP**

*Elena must revoke a user who should no longer have access.*

1. **Entry:** a specific **member row**, with the target user, current role, and freshness visible.
2. **Gating (fail-closed, BEFORE preview):** the system validates inputs and gates — **freshness + authorization must be `eligible`**, or the action is unavailable with a stated **unavailable-action-reason**. The remove control is a **destructive-control**, never a casual primary action.
3. **Consequence Preview** opens, carrying the **full 10-item content set**: (1) tenant; (2) target user; (3) current role; (4) owner-count impact (incl. the last-owner / zero-owner case); (5) the specific access being revoked/changed; (6) current freshness of the inputs; (7) the recovery path available afterward; (8) the audit expectation (what evidence will exist); (9) the target's platform standing (e.g. also a global administrator); (10) explicit **known consequences vs. known unknowns** (session/token invalidation is a known-unknown unless proven). **If any of the 10 is unavailable, submission is blocked (fail-closed)** and the missing item is named.
4. **Elevated-friction confirmation** for high-risk cases — triggered by **(a) dropping owner count to zero, OR (b) the target also holds global-administrator authority**. She clears the extra friction (last-owner is a *warning with extra friction, never a hard block*; the target-also-global-admin flag is reflected only and does not change which command dispatches).
5. She confirms → the command dispatches → the **command-lifecycle-panel** tracks it `submitted → accepted → projection_pending → confirmed`, then `audit_pending → audit_available`, **without overwriting the confirmed projection data** and without collapsing states. SignalR may nudge a re-query but never advances the panel.
- **CLIMAX / key decision beat:** **access is changed and *proven*, with no false "done" shown before the source-of-truth projection confirms it.**
- Resolution: the change is on the **Audit Evidence Receipt**. **Edges / reconciliation outcomes:** incomplete preview inputs block submission (fail-closed); an already-applied removal — including duplicate submit / refresh during pending — reads `already applied` (deduplicated, no double-apply), **never failure**, and offers `inspect audit` / `continue read-only`; unconfirmable reconciliation reads `unable to verify` (**never shown as success**), offering `retry status lookup` / `escalate`; lost permission mid-flow → she is told and offered `request permission` / `escalate`.

### UJ-4 — Sofia investigates an incident and recovers *(Phase 2c)*

*Sofia, incident & support lead, must understand and correct the effect of a mistaken access change.*

1. Sofia opens the **Audit trail list** (from nav, a tenant row, or a command result) and filters by date / `AuditEventCategory`.
2. She reads an **Audit Evidence Receipt** (who acted, on whom, in which tenant, outcome, when, support-safe reference) and identifies the wrong change.
3. She starts a **compensating command** — "restore intended access" — and **previews the correction against current state** (the original effect may already differ) via its own consequence-preview.
4. She submits a *new* command; original and corrective records are linked.
- **CLIMAX:** the effect is **corrected forward, with the mistake and the fix both permanently on the record**.
- Resolution: Sofia cites the support-safe reference to the stakeholder. **Edge:** delayed/unavailable evidence shows honestly (`audit pending` / `audit delayed` / `audit unavailable`) and offers retry/wait/escalate — **it never fabricates proof**. (Named recovery paths from the receipt: `reassign tenant owner`, `restore intended access`, `retry access removal`, `inspect audit`, `escalate`. A global-admin correction is a separate `global-administrators`-domain command, not a tenant edit.)

### UJ-5 — Nadia self-serves her own tenant *(Phase 2a / MVP — read; role change Phase 2b)*

*Nadia owns a customer tenant and wants to manage her team without a ticket.*

1. Nadia is authenticated as a tenant owner; she sees **only her own tenant** (authorization-scoped). She rides the **same surfaces** as operators — no dedicated owner home in v1 (honest-minimal).
2. She opens her tenant's **Member table** and reviews members and roles.
3. *(Phase 2b)* She changes a teammate's role and watches it **confirm** through the command-lifecycle-panel.
- **CLIMAX:** she **manages her own access picture independently**.
- Resolution: operators are no longer a bottleneck for her routine changes. **Edge:** she **never sees other tenants or the Global Administrators surface**. Dangerous accidents (e.g. removing the last owner) raise a warning with extra friction — she is *not* hard-blocked when she genuinely means it.

### UJ-6 — Elena onboards a new tenant *(Phase 2b / 2c)*

*A new customer needs a tenant stood up.*

1. Elena (operator) creates the tenant (FR-13).
2. She adds the first owner **directly by user id** (FR-10) — there is **no email-invitation step** in v1.
3. She sets initial configuration (FR-16) — each `SetTenantConfiguration` goes through a full consequence-preview (config edits always require it).
4. She **confirms each step landed** (projection-confirmed) before moving on.
- **CLIMAX:** **a usable, owned, configured tenant exists.**
- Resolution: the owner (Nadia) can now self-serve (UJ-5). **Edge:** adding a user is a *direct* add by user id; re-adding an existing member is rejected (`UserAlreadyInTenant`), not a NoOp.

> **Surface coverage check.** Tenant list → UJ-1/UJ-6; Tenant detail → UJ-1; Tenant configuration view → UJ-6 (set) + read within detail; **"My Tenants" self-audit → Marc** (FR-3: "See which tenants I belong to and in what role" — the lowest-scope self-verification journey; lands the My-Tenants surface); User lookup → reached contextually from a member row in UJ-2/UJ-3; Member table → UJ-2/UJ-3/UJ-5; Global Administrators review → UJ-3/UJ-4 (target-also-global-admin friction + global-admin correction) and authorized-operator review; Audit list + Audit Evidence Receipt → UJ-3/UJ-4; Consequence Preview + Command Lifecycle Panel → UJ-3/UJ-4/UJ-5/UJ-6; Command flow surfaces → UJ-3/UJ-5/UJ-6. [NOTE FOR UX] Marc has no full UJ in the sources (only the FR-3 goal line); the My-Tenants self-audit journey is the minimal landing for him and is marked accordingly — not invented requirements, just the surface-to-need binding.

## Responsive & Platform

Desktop-first. Breakpoints (the layout rule — distinct from the §9 *test* widths):

| Breakpoint | Behavior |
|---|---|
| **Mobile 320–767px** | **Read-only triage, lookup, and audit reference only — NO high-impact command flows.** Safety-critical columns pinned (never dropped). |
| **Tablet 768–1023px** | Navigation collapses; regions stack; tables preserved via horizontal scroll / column priority — **not** gesture-redesigned (touch targets adequate, no gesture-first workflows). |
| **Desktop 1024px+** | Primary admin workstation: dense tables, keyboard/mouse, side-by-side context. Full-width operational surfaces with constrained readable inner regions. |
| **Wide desktop 1440px+** | As desktop, with more horizontal room; safety-critical columns remain pinned. |

- **Safety-critical columns never drop:** identity, status, freshness, role, and (where present) risk are preserved at **every** width via horizontal scroll / column priority — never hidden. (Risk is a derived member/consequence-context signal — see *State Patterns* — not a standalone tenant-grid column in v1.) "Column priority" must never be read as permitting a safety-critical column off-screen.
- **FAIL-CLOSED responsive rule:** if a width cannot preserve full safety context for a high-impact action, that action becomes **unavailable** (with a visible reason) rather than rendering unsafely. (This is why mobile carries no high-impact command flows — there is no width-safe way to show the full 10-item consequence-preview + elevated friction.)
- **Stable layout:** reserves space to avoid shift; destructive/warning styling used sparingly (DESIGN.md owns the visuals).
- **RTL:** **RTL-ready, not RTL-tested in v1** — direction-agnostic layout (logical start/end, no hard-coded left/right). RTL shipping/verification deferred (Open Q#6).

## FrontComposer Readiness & Fallbacks

FrontComposer is the platform UI framework this UI **composes**. Per repo policy, missing shared UI capability belongs in **FrontComposer, not Tenants** — Tenants must never absorb the scaffolding. **Do not treat unconfirmed capabilities as given.**

| FC code | Capability | Readiness | Gates / behavior delta |
|---|---|---|---|
| **FC-TBL** | Projection list/table (DataGrid, filter/search/empty/loading) | **available** | Backbone of all read surfaces — the only fully-available capability. |
| **FC-LYT** | Shell layout contract (full-width vs constrained) | **needs-confirmation** | Gates `ui-01..15` — **including the read-only MVP**. UX intent: full-width operational surfaces with constrained inner regions for forms/previews. |
| **FC-CMD** | Command-lifecycle feedback (three-phase, projection-confirmed) | **needs-confirmation** | Required for **ALL command FRs**. Maps to the `missing lifecycle support` unavailable-action-reason. |
| **FC-A11Y** | Accessibility primitives | **needs-confirmation** | First-class, **non-removable even under fallback**. |
| **FC-L10N** | Localization (shell resources) | **needs-confirmation** | Resource ownership undecided (Open Q#4). |
| **FC-DOC** | Component documentation / Storybook | **needs-confirmation** | **Required for "ready"** (the ready-gate evidence set). |
| **FC-CNC** | Concurrent-command / toast-batching policy | **missing** | Gates remove-user + bulk; applies to all command FRs. → **fallback: one-at-a-time commands.** |
| **FC-TOK** | Status/severity/timeline tokens | **missing** | Polished audit/consequence visuals; use existing Fluent/FC badges as a *proposed* named fallback — do not assert a token name as available. |
| **FC-AUD** | `<AuditTimeline>` | **missing** | Audit timeline (FR-20). → **fallback: flat audit DataGrid.** Maps to `missing audit proof` / `missing implementation support`. |
| **FC-CNS** | `<ConsequencePreview>` | **missing** | Consequence Preview (CP-5, FR-12/15/16/17). → **fallback: inline consequence text** carrying the full 10-item set; fail-closed if any item is unavailable. |

**Three approved interim fallbacks (Product/UX approval recorded 2026-06-03 — see the [Fallback Approval Record](../../fallback-approval-record-2026-06-03.md); each flow is build-ready only once its other gates also clear):**
1. **FC-AUD → flat audit DataGrid** — cursor-paginated, date + `AuditEventCategory` filters, states loading/empty/filtered-empty/error, in lieu of `<AuditTimeline>`.
2. **FC-CNS → inline consequence text** — structured inline text carrying the **full 10-item content set**; content completeness is non-negotiable; **fail-closed** if any of the 10 is unavailable.
3. **FC-CNC → one-at-a-time command policy** — serialized single-command interaction; **no concurrent submission, toast-batching, or multi-row bulk actions in v1**.

**Readiness severity split.** Platform-wide destructive actions — **FR-15 (disable/enable)** and **FR-19 (global-admin grant/remove)** — are categorically **`blocked`** (no fallback). Tenant-scoped destructive — **FR-12 (remove-user)** and **FR-16/17 (config)** — are **`planning-only` (fallback-eligible)**.

> **This is a plan, not build-ready. Nothing here is buildable until `FC-LYT` / `FC-CMD` / `FC-CNC` clear.** Per PRD §14, no backlog row is unblocked yet — not even the read-only MVP. Design proceeds; building does not.

[NOTE FOR UX] Sources silent / deferred, flagged rather than invented: (1) numeric freshness thresholds — deferred to implementation + ops, must be configurable + surfaced (Open Q#10); (2) `duplicate` / `timeout` command-lifecycle tokens carry **no gloss** by spec instruction — none invented; (3) localization resource ownership unresolved (Open Q#4); (4) FR-22/24/25 (receipt assembly + compensating recovery) are committed product intent but **not yet backed by a `ui-NN` backlog row or backend evidence** — they need a future story before build-ready; (5) the digest's pinned-version discrepancy (Fluent `rc.2-26098.1` vs `rc.3-26138.1`) is a DESIGN.md/build concern, recorded here only as context. **Owners for deferred obligations:** freshness thresholds → product/ops; contrast (WCAG 1.4.3 / 1.4.11) verification of the inherited Fluent roles → the implementing team's a11y reviewer at build; `FC-A11Y` / `FC-DOC` / `FC-L10N` confirmation → the FrontComposer / shell team; l10n resource ownership (#4) → architecture; FR-22/24/25 backlog row → product.
