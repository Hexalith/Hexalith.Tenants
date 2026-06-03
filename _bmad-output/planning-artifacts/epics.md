---
stepsCompleted: [1, 2]
inputDocuments:
  # --- Core spines (reconciled, authoritative) ---
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md
  # --- Phase-2 candidate backlog (ui-01..15) ---
  - docs/tenants-ui-phase-2-story-backlog.md
  # --- docs/ UI technical specs (depth) ---
  - docs/tenants-ui-operations-shell-spec.md
  - docs/tenants-ui-truth-state-and-action-availability-spec.md
  - docs/tenants-ui-remove-user-from-tenant-journey-spec.md
  - docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md
  - docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md
  - docs/tenants-ui-responsive-layout-and-visual-system-spec.md
  - docs/tenants-ui-frontcomposer-dependency-map.md
  # --- Readiness report ---
  - _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-02.md
  # --- UX mockups (illustrative; spines win on conflict) ---
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/mockups/mock-tenant-list.html
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/mockups/mock-consequence-preview.html
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/mockups/mock-command-lifecycle.html
---

# Tenants Management UI - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for the **Tenants Management UI** — a trust-first operations console (new Blazor `InteractiveServer` host `src/Hexalith.Tenants.UI`, composed on the Hexalith.FrontComposer shell) that turns the event-sourced Tenants backend into a safe, role-scoped self-service and operations experience. It decomposes the requirements from the PRD, the UX Design (DESIGN.md visual spine + EXPERIENCE.md behavioral spine), and the Architecture decision document into implementable stories.

> **Build-readiness reality (carried from PRD §14, architecture Gap Analysis, readiness report):** this is a complete *plan*, **not** a green light to start coding. Every candidate row is externally gated on FrontComposer readiness (`FC-LYT` gates even the read-only MVP; `FC-CMD`+`FC-CNC` gate all commands; `FC-AUD`/`FC-CNS` gate audit/high-impact) and on Product/UX fallback approvals. Epics/stories are authored so they are build-ready the moment their named gates clear — the gates are tracked explicitly per story, never silently assumed cleared.

## Requirements Inventory

### Functional Requirements

Phase legend: **2a** = MVP read-only foundation · **2b** = first command flows · **2c** = high-impact + audit + recovery. Backlog row = candidate `ui-NN` from `docs/tenants-ui-phase-2-story-backlog.md`.

**7.1 Tenant Discovery & Triage — Phase 2a (ui-01, ui-02)**
- **FR-1: Browse and triage the tenant list.** Operator scans/searches/filters/sorts/pages tenants. Cursor pagination (never offset/limit); each row shows tenant identity, status, member count, owner count, pending state, and a Truth State Badge with freshness; renders six distinct, non-collapsing list states (loading, empty, filtered-empty, error, stale, degraded); sorting/paging never hides a pending or stale marker; all states authorization-safe (no out-of-scope leak). Realizes UJ-1.
- **FR-2: Open a tenant and return with context preserved.** Open a tenant's detail and return to the list with prior filter/sort/selection intact; deep-linking to detail supported. Realizes UJ-1.
- **FR-3: Self-audit "My Tenants".** Signed-in user views the tenants they belong to and their role in each; shows only authorized memberships; role + tenant status per row. Realizes UJ-5 (lands Marc's self-audit surface).
- **FR-4: Look up a user's memberships.** Operator searches a user and views that user's tenant memberships, and reaches a user from a member row; authorization-scoped; no-memberships shows an explicit empty state (≠ error). Realizes UJ-2.

**7.2 Tenant Detail & Configuration View — Phase 2a (ui-03, ui-05)**
- **FR-5: View tenant overview.** Status, metadata, member/configuration summaries on one surface; lifecycle status with no-color-only encoding + freshness indicator; member/owner counts.
- **FR-6: View tenant configuration (read-only).** Key/values grouped by namespace, filtered to the caller's owned/authorized dot-prefix; values outside the prefix not shown; sensitive-value display out of read-MVP scope `[ASSUMPTION]`.
- **FR-7: Copy support-safe identifiers.** Copy a full identifier (may be visually truncated) and any support-safe reference; the copied value is the literal caller-supplied string (NOT a ULID — never `Guid`/`Ulid.TryParse`); never exposes payloads, tokens, correlation ids, or PII. *(Weakly covered in backlog — needs dedicated AC treatment.)*

**7.3 Member & Access Review — Phase 2a (ui-04)**
- **FR-8: Review the member table.** Members with role, owner count, status, freshness, and orphan/disabled context, read-only — must not imply mutation; exposes accessible table semantics (headers, sort state, row relationships); orphan/disabled flagged. Realizes UJ-2.
- **FR-9: See action availability and reasons.** Per member, which actions *would* be available and — where not — a plain-language Unavailable Action Reason (6 canonical categories), inline-visible (not hover-only). Reflective in MVP; actions arrive in later phases. Realizes UJ-2.

**7.4 Member & Role Management — Phase 2b/2c (ui-09 add+role, ui-14 remove)**
- **FR-10: Add a user to a tenant *(2b)*.** Direct add by caller-supplied user id with explicit role (no invitation/pending step); adding an existing member is **rejected** (`UserAlreadyInTenant`), surfaced as safe localized text — NOT a NoOp; a corrective add states the explicit intended role. Realizes UJ-6, UJ-5.
- **FR-11: Change a member's role *(2b)*.** Change to the current role is a **NoOp** shown as `already applied`; role escalation and `Unknown` targets rejected (safe text); success only after projection confirmation (CP-3). Realizes UJ-5.
- **FR-12: Remove a user from a tenant *(2c)*.** Inputs validated before preview; Consequence Preview (10-item set); reducing owner count to zero = elevated friction, **not blocked**; target who also holds global-admin authority = platform-level friction (CP-6); not a casual button; already-applied removal reads `already applied` and duplicate submits de-duplicate; Command Lifecycle Panel tracks `submitted → accepted → projection_pending → confirmed → audit_pending → audit_available` without collapse; unconfirmable = `unable to verify` (never success); every failure → a stated recovery (CP-8). **Blocked on FC-CNS, FC-CMD, FC-CNC.** Realizes UJ-3 (flagship).

**7.5 Tenant Lifecycle Management — Phase 2b/2c (ui-07, ui-08, ui-13)**
- **FR-13: Create a tenant *(2b)*.** Creating an existing tenant id is rejected (`TenantAlreadyExists`); success only after projection confirmation. Realizes UJ-6.
- **FR-14: Edit tenant metadata *(2b)*.** RBAC = tenant contributor or global admin; **every successful edit emits `TenantUpdated` — no same-state suppression**; validation errors as safe localized field messages.
- **FR-15: Disable or enable a tenant *(2c)*.** RBAC = global administrator only (platform-wide, high-impact); setting a state already set is rejected (`TenantLifecycleStateAlreadySet`); Consequence Preview notes disabled = eventually-consistent availability signal and that commands to a disabled tenant are rejected (`TenantDisabled`); success only after projection confirmation; lifecycle status with no-color-only encoding. *(Categorically `blocked` — no fallback.)*

**7.6 Tenant Configuration Management — Phase 2c (ui-10)**
- **FR-16: Set a configuration value *(2c)*.** Identical key+value is a **NoOp** (`already applied`); values over domain limits rejected (`ConfigurationLimitExceeded`); whether every config edit needs a preview or only a high-risk subset is an open question + phasing lever (§16.8).
- **FR-17: Remove a configuration key *(2c)*.** Removing a missing key surfaces a safe `ConfigurationKeyNotFound` rejection; success only after projection confirmation.

**7.7 Global Administrator Governance — Phase 2a read (ui-06) / 2c cmd (ui-15)**
- **FR-18: Review global administrators *(2a, read)*.** Visible only to authorized operators (tenant owners never see it); data from the single fixed-identity `global-administrators` aggregate (not tenant-routed); rows show identity + freshness badge. Realizes UJ-2.
- **FR-19: Grant or remove a global administrator *(2c, platform-wide)*.** Grant, or remove except the last; **removing the last global administrator is rejected (`LastGlobalAdministrator`)** → UI reflects as *unavailable* with a safe reason, not as completable friction (CP-6, asymmetric with last-owner); operations in the `global-administrators` scope, never conflated with tenant membership. *(Categorically `blocked` — no fallback.)*

**7.8 Audit Trail & Evidence — Phase 2c, blocked on FC-AUD (ui-11, ui-12)**
- **FR-20: Browse a tenant's audit trail.** Flat, stably-ordered, cursor-paginated list with date + `AuditEventCategory` (`Access`/`Administrative`) filters; targets ~500 events without unacceptable degradation; distinct accessible states (loading/empty/filtered-empty/error); flat list is the FC-AUD fallback (usable only on Product/UX approval). Realizes UJ-4.
- **FR-21: Reach audit from context.** From nav, a tenant row, tenant detail, a user lookup, and a command result; each entry point lands scoped to the relevant tenant/user/command.
- **FR-22: View an Audit Evidence Receipt.** Support-safe receipt (actor, target, tenant scope, outcome, absolute timestamp, projection marker, audit/command reference) assembled client-side from a structured NarrativePayload (no new backend endpoint); never exposes raw payloads, tokens, correlation ids, raw metadata, or PII; partial completion shows the actual lifecycle state, never pre-rendered proof. **⚠ No backlog row — needs a new story.**
- **FR-23: Distinguish audit availability states.** `audit pending` / `audit delayed` / `audit unavailable` / `missing implementation support`, each with a stated recovery; none shown as success; `missing implementation support` reflects the FC-AUD dependency, not a data error. *(Weakly covered — only implied in ui-11/ui-12.)*

**7.9 Compensating Recovery — Phase 2c (NO backlog row — needs stories)**
- **FR-24: Start a compensating command.** From audit evidence, start a forward correction ("restore intended access" / "start correction") — a new command with its own Consequence Preview and proof; never "undo"; original event untouched; previews against current state; restore-to-empty-tenant relies on the empty-tenant bootstrap (`HasMembershipHistory == false`). **⚠ No backlog row — needs a new story.**
- **FR-25: Preview and link the correction.** Preview the correction against current state; original and corrective records reference each other; success only after projection confirmation. **⚠ No backlog row — needs a new story.**

### NonFunctional Requirements

- **NFR-1: Performance & freshness.** Cursor pagination + conditional requests (ETag `If-None-Match` → `304`) so unchanged data is cheap; freshness surfaced, not hidden; tenant list/detail/member surfaces interactive in **≤ ~1s on a warm projection**; audit view targets **~500 events** without unacceptable latency (virtualization or stricter page size if a flat render cannot). Exact budgets `[ASSUMPTION]`, confirmed at implementation.
- **NFR-2: Security & authorization.** Authorization is **server-enforced** at the API layer (L1) and in the domain (L2); the UI **reflects, never enforces**, and must remain safe even if it misjudges authorization; role-scoping (owner sees only their tenant; global admin sees all) enforced in the projection/query layer; under InteractiveServer the access token stays **server-side** (browser never receives it).
- **NFR-3: Reliability & consistency.** Eventually consistent, event-sourced; the projection is the **source of truth**; the UI re-queries to confirm and is correct under at-least-once delivery + projection lag (CP-3/CP-4); Blazor reconnect re-derives truth from the projection, never resurrects optimistic success.
- **NFR-4: Observability & testability.** Every interactive element and status carries a **stable automation selector / component contract** (`data-testid="tenants-{surface}-{element}"`), **never** keyed on row text or color, so acceptance/E2E tests are robust.
- **NFR-5: No data-store edits.** The UI never edits, deletes, or rewrites events, projections, or state to "fix" data; corrections are forward compensating commands only (CP-7); no new backend endpoints — receipts/previews/status assembled client-side from already-loaded read-model fields.

### Additional Requirements

*(Technical & cross-cutting requirements from the Architecture decision document + PRD §6/§10/§11 + addendum that materially shape epics, stories, sequencing, and acceptance criteria.)*

**🟢 STARTER TEMPLATE — Epic 1 / Story 1 bootstrap (greenfield; first implementation priority).** No UI host exists today. The architecture mandates a **new `src/Hexalith.Tenants.UI`** project — `Microsoft.NET.Sdk.Web`, `net10.0`, Blazor **InteractiveServer** (D1) — created manually (no scaffolder; `frontcomposer` CLI is inspect/migrate only) from the EventStore reference-UI pattern, added to `Hexalith.Tenants.slnx`, orchestrated by the existing `Hexalith.Tenants.AppHost`. Bootstrap composes the FrontComposer Shell + `FluentProviders`, wires JWT bearer + the BFF query gateway, and stands up the Fluxor `TruthState` feature + the `Vocabulary/` canonical-state library. **This is the prerequisite for every other story.**

**Architectural decisions (D1–D10) that constrain stories:**
- **D1 Runtime:** Blazor **InteractiveServer** + a server-side **BFF** in the UI host (supersedes the UX "Auto" assumption — a recorded reconciliation, NFR-3 holds either way).
- **D2 Command confirmation (the ONE pattern):** on dispatch, run status-poll (`GET /api/v1/commands/status/{correlationId}`) **and** SignalR concurrently; first terminal/projection-change signal triggers the authoritative **projection re-query**; lifecycle flips to `confirmed` only from the re-query. SignalR never advances lifecycle/audit. No surface implements an optimistic path.
- **D3 FrontComposer posture:** **hybrid** — FC-LYT/FC-CMD/FC-CNC are contracts to confirm with the FrontComposer team; FC-AUD/FC-CNS delivered via the **approved fallbacks** (flat audit DataGrid, inline consequence text). *(Fallback-approval contradiction — UX says "approved", PRD/backlog say "none approved" — must be reconciled with an owner/evidence/date record; architecture D3 commits to the fallbacks.)*
- **D4 Localization ownership:** **Tenants-owned** whole-string `.resx` keys (dotted PascalCase under `Tenants.`); inherit only shell-chrome strings from `FcShellResources`.
- **D5 Truth-state model:** one shared Fluxor **TruthState feature** + a typed, casing-faithful **canonical-vocabulary library** (`Vocabulary/`); non-collapse enforced in the reducer.
- **D6 Freshness:** server-side conditional reads (`If-None-Match` → `304`); thresholds configurable + surfaced; `unknown` when unmeasurable (fail-closed).
- **D7 Authorization reflection:** server-side claims→action-availability service producing the 6-category Unavailable Action Reason; indeterminate → fail-closed.
- **D8 Support-safety:** server-side receipt/preview/redaction assembly (NarrativePayload→receipt, consequence-preview, rejection→text); only safe, localized, redacted projections reach the browser.
- **D9 Cursors:** opaque, signed, scope-bound, **server-held** pass-through; page-1 re-query on invalidation with an honest "list refreshed" notice; multi-replica durability not-yet-guaranteed (backend Epic 11).
- **D10 UI host placement:** new `src/Hexalith.Tenants.UI` in the Tenants repo (presentation host distinct from the domain-module AppHost policy; consumes platform ServiceDefaults).

**Implementation sequence (architecture):** (1) bootstrap → (2) read surfaces (FR-1..9, FR-18) once **FC-LYT** confirmed → (3) first commands (FR-10/11/13/14) once **FC-CMD + FC-CNC** resolve → (4) high-impact + audit + recovery (FR-12/15/16/17/19/20–25) on approved **FC-AUD/FC-CNS** fallbacks. Shared foundations (Fluxor truth-state model, Vocabulary library, BFF query/command gateway, authorization-reflection service, support-safety/redaction layer) are built first; every surface depends on them.

**Information Architecture (Operations Shell):** primary nav, in order — **Tenants** (default landing/triage) · **Global Administrators** · **Audit**. **Users is CONTEXTUAL** (reached from a member row + global search), not a co-equal tab (resolves the PRD↔UX "Users-nav" divergence to *contextual*). Command lifecycle is **never** a nav area — shown inline, anchored to the affected row/panel. Selection/filters/scroll preserved across navigation.

**Backend surfaces consumed (already built — consume verbatim, add/alter nothing):** 5 read endpoints — `GET /api/tenants`, `/api/tenants/{tenantId}`, `/api/tenants/{tenantId}/users`, `/api/users/{userId}/tenants`, `/api/tenants/{tenantId}/audit` — plus `POST /api/v1/commands` (with client `messageId` ULID idempotency key; `202` + correlationId) and `GET /api/v1/commands/status/{correlationId}`. **No new backend endpoints.** Command route `/api/v1/commands` vs `/api/commands` alias to confirm against the gateway (§16.1). Bind to `Hexalith.Tenants.Client`/`.Contracts` DTOs (`PaginatedResult<T>`, `TenantSummary`, `TenantDetail`, `TenantMember`, `UserTenantMembership`, `TenantAuditEntry`); never re-declare a DTO or re-case a wire field.

**Cross-cutting interaction contract (CP-1..CP-10) — governs every command flow:** CP-1 five truth dimensions; CP-2 fail-closed; CP-3 non-collapse invariant (`accepted` ≠ `confirmed` ≠ `audit available`; never show unconfirmed success in styling/copy/announcement); CP-4 live signals are nudges, not proof; CP-5 Consequence Preview before destructive action; CP-6 asymmetric high-risk (last-owner = friction/allowed; last-global-admin = hard-reject/unavailable); CP-7 correct forward, never "undo"; CP-8 distinct recovery per failure mode; CP-9 authorization reflected, never enforced; CP-10 canonical state sets used verbatim (casing-significant).

**Canonical state sets (addendum §G — used VERBATIM, casing significant, single `Vocabulary/` source):** Truth State Badge **13**; Freshness **5** (`current`/`refreshing`/`aging`/`stale`/`unknown`; `aging` usable-with-friction, `stale`/`unknown` block); Command lifecycle **10** (+ snake_case machine tokens `projection_pending`/`confirmed`/`audit_pending`/`audit_available`); Layered feedback **10** (`degraded` + `unable to verify` success-prohibited); Unavailable Action Reason **6**; Audit availability **4**; Recovery verbs (allowed set; **prohibited:** `undo`/`rollback`/`hidden edit`). Badge `audit pending` vs machine `audit_pending` stay distinct — never unified.

**Rejection / NoOp matrix (addendum §D — drives FR consequence text):** `UserAlreadyInTenant` (re-add = rejection, NOT NoOp); `ChangeUserRole` to current role = NoOp `already applied`; `RoleEscalation`/`Unknown` = rejection; `UpdateTenant` always emits `TenantUpdated` (no suppression); `SetTenantConfiguration` identical = NoOp, over-limit = `ConfigurationLimitExceeded`; `RemoveTenantConfiguration` missing key = `ConfigurationKeyNotFound`; disable/enable to already-set = `TenantLifecycleStateAlreadySet`; any command to a disabled tenant = `TenantDisabled`; remove last owner = **allowed** (friction); remove last global admin = `LastGlobalAdministrator` rejection (unavailable); `TenantAlreadyExists` on create. Domain rejections → RFC 7807 at the boundary → safe localized text via a Tenants rejection catalog keyed by safe reason code.

**Consequence Preview content set (addendum §H — 10 items, fail-closed if any unavailable):** tenant; target user; current role; owner-count impact (incl. last-owner/zero-owner); specific access revoked/changed; current freshness of inputs; recovery path afterward; audit expectation; target's platform standing (e.g. also a global admin); explicit known-consequences vs known-unknowns (no over-claiming — session/token invalidation is a known-unknown unless proven).

**Support-safety & privacy guardrails (PRD §10 — hard rule):** no surface/label/log/toast/receipt/copied value may expose bearer tokens, decoded JWT contents, command payloads, serialized event bodies, raw EventStore metadata, internal correlation ids, stack traces, or PII; support-safe references are the only sharable ids; empty/error states must not reveal out-of-scope tenants/members/memberships.

**Identity rules:** `TenantId`/`UserId` are meaningful **caller-supplied strings, case-sensitive (Ordinal), NOT ULIDs** — never `Guid`/`Ulid.TryParse`; copy-full-id copies the literal. *(Corrects the source-spec ULID error, PRD R-6/§16.12.)* Actor identity from JWT `sub`/envelope `UserId`; tenant scope via `eventstore:tenant=system`; global-admin via `global_admin`/`role` claim shapes; Keycloak (prod) or symmetric-key JWT (dev).

**FrontComposer dependency & build-readiness gates (the per-story gating set):** `FC-TBL` **available** (DataGrid backbone of all read surfaces) · `FC-LYT` needs-confirmation (shell layout — gates even the MVP) · `FC-CMD` needs-confirmation (command-lifecycle feedback — all commands) · `FC-A11Y`/`FC-L10N`/`FC-DOC` needs-confirmation (every row's ready-gate) · `FC-CNC` **missing** (concurrent-command policy — all commands; fallback = one-at-a-time) · `FC-TOK` **missing** (status/severity/timeline tokens; Fluent/FC badge fallback) · `FC-AUD` **missing** (`<AuditTimeline>`; fallback = flat audit DataGrid) · `FC-CNS` **missing** (`<ConsequencePreview>`; fallback = inline consequence text). A story promotes to *ready* only when its `blockedBy` set empties **or** a Product/UX-approved fallback is recorded. Readiness split: tenant-scoped destructive (FR-12, FR-16/17) = `planning-only`/fallback-eligible; platform-wide destructive (FR-15, FR-19) = categorically `blocked`.

**Testing & quality (architecture + project-context):** bUnit (component, Tier 1) + Playwright (E2E, Tier 3, likely non-blocking) + xUnit v3 + Shouldly; tests in separate `*.UI.Tests` / `*.UI.E2E` projects (`{Class}Tests.cs` plural, never co-located). Pattern-enforcement tests: bUnit asserts non-collapse + the six list states + no-color-only; a guard test fails any surface referencing a raw state literal instead of the `Vocabulary/` library; Playwright asserts the **six required acceptance scenarios** (stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, permission-missing) keyed on `data-testid`. Repo conventions inherited verbatim (`.slnx` only; central package versions; `ConfigureAwait(false)`; no copyright headers; Conventional Commits; SDK containers → `registry.hexalith.com/tenants-ui`, ships as image not NuGet).

**Coverage gaps to author as stories (readiness report §3/§5):** FR-22 (audit-evidence-receipt assembly), FR-24 + FR-25 (compensating recovery) have **no candidate backlog row**; FR-7 (copy support-safe id) and FR-23 (4-state audit availability) are weakly covered and need dedicated AC treatment. The Epic 1 bootstrap story is also net-new.

**Deferred numerics (set later, do not block plan):** NFR performance budgets; freshness `current`/`aging`/`stale` thresholds (config, no magic numbers); success-metric targets (SM-1..SM-6); RTL shipping (Open Q#6); WCAG 2.2 confirmation against the pinned Fluent build; sensitive-config display (Open Q#11); cursor durability across replicas (backend Epic 11).

### UX Design Requirements

*(First-class, actionable UX work items from DESIGN.md (visual spine) + EXPERIENCE.md (behavioral spine). Each is specific enough to generate a story with testable acceptance criteria.)*

- **UX-DR1: The 10 domain components.** Build all ten, each composing Fluent v5 primitives, with exact names: **(1) `TruthStateBadge`** (fuses color+icon+text; renders the 6 status vocabularies; never collapses two dimensions), **(2) `ConsequencePreview`** (constrained inner region; full 10-item set; fails closed on any missing item), **(3) `CommandLifecyclePanel`** (FluentMessageBar Notification; inline, anchored; never overwrites confirmed data; steps shown distinctly), **(4) `UnavailableActionReason`** (inline-visible, never hover-only; 6 categories; `aria-describedby`-associated), **(5) `AuditEvidenceReceipt`** (FluentMessageBar; client-assembled from NarrativePayload; support-safe; honest partial state), **(6) `TenantDataGrid`** (cursor pagination; pinned identity/status/freshness; six list states), **(7) `MemberTable`** (read-only, must-not-imply-mutation; pinned identity/role/status/freshness; action-or-reason cell), **(8) `AuditDataGrid`** (flat, stably-ordered, cursor-paginated FC-AUD fallback; pinned timestamp/actor/outcome; date+category filters), **(9) `PrimaryCommandButton`** (Fluent Accent/Brand; chrome accent, never a status carrier; disabled while any command in flight), **(10) `DestructiveControl`** (NOT a primary/casual button; low-emphasis until gated; gated behind ConsequencePreview + asymmetric confirmation; safe non-committing escape).
- **UX-DR2: Semantic color system — bind to Fluent `BadgeColor` roles by name, never hex.** Eight roles: Success / Informative / Warning / Severe / Danger / Important / Subtle / Brand. **Locked invariant — Success-is-proven firewall:** `Success` only for projection-proven states (`current`, `confirmed`, `audit available`, tenant `active`); every in-flight/pending state (`accepted`, `submitted`, `previewed`, `projection pending`, `audit pending`, `refreshing`) is `Informative` — `accepted` is explicitly NOT Success.
- **UX-DR3: The 3-tier caution ramp (never collapsed into one "bad" color).** Tier 1 `Warning` = usable-with-friction (`aging`, `timeout`, `audit delayed`); Tier 2 `Severe` = blocks-but-not-error (`stale`, tenant `disabled`, `audit unavailable`, `degraded`); Tier 3 `Danger` = refusal/failure/destruction, used sparingly (`rejected`, `failed`, risk `high`, destructive moment). `Important` = must-act/uncertain (`unknown` freshness/lifecycle, authorization `blocked`, `unable to verify`); `Subtle` = benign (`eligible`, `already applied`, `duplicate`, risk `low`, `missing implementation support`).
- **UX-DR4: No-color-only is absolute.** Every status = `BadgeColor` + Fluent `IconStart` glyph + `IconLabel`/visible text, legible in light/dark/high-contrast/**forced-colors**. Implement the **verified cross-role-distinct status-icon set** (per DESIGN.md table; pin status-badge glyphs to Size20 — several lack Size16 variants); `BadgeAppearance.Tint` default, `Filled` only for Danger+Severe (emphasis, NOT counted toward no-color-only).
- **UX-DR5: Typography — inherit the Fluent/system ramp; one delta role.** Modest hierarchy (no hero/display scale). `mono` role for `TenantId`/`UserId`/support-safe references **and** absolute timestamps (keeps glyph-similar chars distinguishable; makes copy-full-id faithful). All labels are localizable whole strings; layout reserves room for the longest localized label.
- **UX-DR6: Layout & spacing.** Full-width operational surfaces with **constrained, readable inner regions** for forms/previews/panels/dialogs (gated on FC-LYT). 4px spacing rhythm (4/8/12/16/24/32); compact density; tables/split-views/side-panels over decorative card grids. **Forbidden:** marketing card dashboards, hero type, decorative card grids.
- **UX-DR7: Stable layout — reserve space, never shift.** Status cells, action cells, freshness markers, and reason slots reserve their footprint whether or not content is present; row actions hold stable width/placement under data/sort/page change; loading states reserve space.
- **UX-DR8: Pinned safety-critical columns.** Identity, status, freshness, role, risk pinned `DataGridColumnPin.Start` so they never drop under horizontal scroll; consistent pinned treatment + accessible "pinned" indication (not color-only) across all three grids. "Column priority" never means a safety-critical column may go off-screen.
- **UX-DR9: Six non-collapsible list-surface states (shared component).** `loading`, `empty`, `filtered-empty`, `error`, `stale`, `degraded` — each visually distinct: `filtered-empty` offers filter reset; `stale` shows freshness marker + refresh path; `degraded` explains what still works; `empty` is authorization-safe. Sorting/paging never hides a pending or stale marker.
- **UX-DR10: Fail-closed gating ORDER (load-bearing).** Validation **+** freshness **+** authorization all `eligible` **BEFORE** the Consequence Preview opens (not only at submit); missing any → blocked with the inline UnavailableActionReason; `stale`/`unknown` freshness, indeterminate authorization, incomplete preview, or missing lifecycle support each block.
- **UX-DR11: The ONE command-confirmation flow (no alternatives/optimistic path).** Dispatch → effect runs status-poll **+** SignalR concurrently → first terminal/projection-change → authoritative re-query action → reducer flips `confirmed`. SignalR nudge dispatches only a re-query, never a state-advancing action. Idempotency: one client `messageId` per attempt; dedup by `correlationId`. Duplicate submit/refresh during pending de-duplicates (no double-apply).
- **UX-DR12: Focus & modal behavior.** Every modal/preview traps focus; `Esc`/cancel is a safe **non-committing** escape; focus returns to the launching row/control on close/cancel/submit/failure; **keyboard users can complete OR exit** every modal/preview/table/command workflow (standalone obligation).
- **UX-DR13: Live-region politeness from a dedicated announcement-intent field** (never derived from `BadgeColor`/`MessageBarIntent`). `AriaLive.Assertive` reserved for rejection/failure/`unable to verify`/`degraded`/destructive-block; `Polite` otherwise; **never announce success before projection confirmation**; no assertive on a resting destructive-control or a `risk high` badge.
- **UX-DR14: Recovery-verb mapping — every failure → a distinct named verb, never a dead end.** stale→`refresh`; pending→`wait`; status-lookup fail/`unable to verify`→`retry status lookup`/`escalate`; missing permission→`request permission`/`escalate`/`continue read-only`; wrong change→`start correction`/`restore intended access`; `already applied`→`inspect audit`/`continue read-only`; last-owner removed→`reassign tenant owner`/`restore intended access`; removal didn't land→`retry access removal`; capability not built→honest not-yet-available + `continue read-only`. Prohibited words `undo`/`rollback`/`hidden edit` never appear in any copy/label/tooltip/announcement.
- **UX-DR15: Localization discipline.** Whole-string resources with named placeholders (`{userName}`, `{tenantName}`) — **never** runtime sentence-fragment assembly; culture-aware formatting; every state label/role name/timestamp/warning/disabled-reason/recovery-verb/confirmation/empty/loading/error/degraded/stale/unavailable string localizable.
- **UX-DR16: Responsive behavior (desktop-first).** Breakpoints mobile 320–767 / tablet 768–1023 / desktop 1024+ / wide 1440+. **Mobile = read-only triage/lookup/audit reference only — no high-impact command flows.** Tablet: nav collapses, regions stack, tables preserved via horizontal scroll/column-priority (not gesture-redesigned). **Fail-closed responsive rule:** if a width can't preserve full safety context for a high-impact action, that action becomes unavailable with a visible reason. RTL-ready (logical start/end, no hard-coded left/right), not RTL-tested in v1.
- **UX-DR17: Accessibility floor & ready-gate evidence.** Baseline WCAG 2.1 AA (conditional 2.2 AA where the pinned Fluent stack supports it). Required acceptance scenarios: **stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, permission-missing** — plus keyboard-only complete-OR-exit of consequence-preview & destructive-confirmation (focus trap, safe escape, focus return), forced-colors rendering of the status-icon set, and live-region politeness checks. SR review: **NVDA + ≥1 browser/SR pairing**. Responsive evidence widths: desktop 1024/1366/1440 + wide, tablet 768/1024, mobile 375/430, plus horizontal-overflow/nav-collapse/dialog-at-narrow-width. Absolute (not relative-only) timestamps; table semantics (headers/sort-state/row-relationships) on every grid; reduced-motion never blocks perceiving a state change. **A UI story cannot be `ready` until it cites a11y/l10n/responsive/`FC-DOC` evidence — or records a Product/UX-approved row-specific fallback** documenting keyboard/focus/live-region behavior, copy responsibility, doc evidence, replacement path, and owner approval.
- **UX-DR18: Three approved interim fallbacks (design-time; each build-ready only once its other gates clear).** (a) FC-AUD → flat audit DataGrid (cursor-paginated, date+category filters, 4 list states); (b) FC-CNS → inline consequence text carrying the full 10-item set, fail-closed if any item unavailable; (c) FC-CNC → one-at-a-time command policy (no concurrent submission, toast-batching, or multi-row bulk in v1). *(Subject to the fallback-approval reconciliation noted in Additional Requirements.)*
- **UX-DR19: Voice & tone (microcopy).** Calm, precise, honest; same register for operators and owners. Confirmed examples: "Change submitted. Waiting for the projection to confirm." / "Confirmed against the source of truth." / "Already applied — no change was needed." / "Data is stale — refresh first." / "Couldn't verify the result. Escalate with this reference." Never "Done!"/"Success ✓"/"Saved successfully" before proof.
- **UX-DR20: Risk is derived, not stored (`low`/`high`).** `high` when an action would drop owner count to zero OR the target also holds global-admin authority; `low` otherwise. Surfaces in the member-table action context and the consequence-preview (target's platform standing), pinned where shown — **not** a standalone tenant-grid column in v1. `risk high`→Danger, `risk low`→Subtle.

### FR Coverage Map

*(Every FR maps to exactly one epic; dependencies are strictly backward — no epic requires a later epic to function.)*

- **FR-1:** Epic 1 — Browse and triage the tenant list
- **FR-2:** Epic 1 — Open a tenant and return with context preserved
- **FR-3:** Epic 2 — Self-audit "My Tenants"
- **FR-4:** Epic 2 — Look up a user's memberships
- **FR-5:** Epic 1 — View tenant overview (detail)
- **FR-6:** Epic 2 — View tenant configuration (read-only)
- **FR-7:** Epic 1 — Copy support-safe identifiers
- **FR-8:** Epic 2 — Review the member table
- **FR-9:** Epic 2 — See action availability and reasons
- **FR-10:** Epic 3 — Add a user to a tenant
- **FR-11:** Epic 3 — Change a member's role
- **FR-12:** Epic 4 — Remove a user from a tenant (flagship UJ-3)
- **FR-13:** Epic 3 — Create a tenant
- **FR-14:** Epic 3 — Edit tenant metadata
- **FR-15:** Epic 4 — Disable or enable a tenant (platform-wide, categorically `blocked`)
- **FR-16:** Epic 4 — Set a configuration value
- **FR-17:** Epic 4 — Remove a configuration key
- **FR-18:** Epic 2 — Review global administrators
- **FR-19:** Epic 4 — Grant or remove a global administrator (platform-wide, categorically `blocked`)
- **FR-20:** Epic 5 — Browse a tenant's audit trail
- **FR-21:** Epic 5 — Reach audit from context
- **FR-22:** Epic 5 — View an Audit Evidence Receipt (net-new story)
- **FR-23:** Epic 5 — Distinguish audit availability states
- **FR-24:** Epic 5 — Start a compensating command (net-new story)
- **FR-25:** Epic 5 — Preview and link the correction (net-new story)

**Coverage check:** 25/25 FRs mapped · Epic 1 (4), Epic 2 (6), Epic 3 (4), Epic 4 (5), Epic 5 (6) · no FR unmapped, no FR in two epics.

## Epic List

Five epics, organized by user-value theme. The theme boundaries coincide with the FrontComposer gate seams (`FC-LYT` → `FC-CMD`+`FC-CNC` → `FC-AUD`/`FC-CNS`) and the PRD phases (2a → 2b → 2c). Dependencies are strictly backward; each epic delivers complete, independently valuable functionality and never requires a later epic to function.

### Epic 1: Operations Shell Foundation & Tenant Triage
An operator lands on a trustworthy operations console and triages tenants under pressure — scanning, searching, filtering, sorting, and paging the tenant list (cursor pagination, six non-collapsing list states, freshness on every row), opening a tenant's detail and returning with filters/selection intact, and copying support-safe identifiers — always knowing how fresh the data is. This epic also stands up the walking-skeleton foundation every later epic reuses: the new `src/Hexalith.Tenants.UI` Blazor InteractiveServer host, the composed FrontComposer Shell, JWT auth + the server-side BFF query gateway, and the shared Fluxor TruthState feature + casing-faithful `Vocabulary/` library. *Phase 2a · gate: FC-LYT · Realizes UJ-1.*
**FRs covered:** FR-1, FR-2, FR-5, FR-7

### Epic 2: Access, Configuration & Governance Review
Operators and tenant owners see the *full* truth, read-only — completing the MVP "see the truth" foundation. They review a tenant's member table (role, owner count, status, freshness, orphan/disabled context) with per-row reflected action availability and plain-language Unavailable Action Reasons; inspect namespaced configuration filtered to their authorized prefix; self-audit their own memberships; look up any user's memberships; and review the platform's global-administrator roster — each surface authorization-safe and never implying mutation. *Phase 2a (completes read-only MVP) · gate: FC-LYT · Realizes UJ-2 (read), UJ-5 (read), Marc's self-audit.*
**FRs covered:** FR-3, FR-4, FR-6, FR-8, FR-9, FR-18

### Epic 3: Tenant & Membership Provisioning
Operators and owners safely stand up and adjust tenants and members through the single projection-confirmed command pattern — creating a tenant, editing its metadata, adding a user by id with an explicit role, and changing a member's role — watching each command move through its real lifecycle and confirm against the source-of-truth projection, never an optimistic "done." This epic introduces the D2 command-confirmation flow (parallel status-poll + SignalR → authoritative re-query), the CommandGateway, the CommandLifecyclePanel, and the PrimaryCommandButton. *Phase 2b · gates: FC-CMD, FC-CNC · Realizes UJ-6 (create+add), UJ-5 (role change).*
**FRs covered:** FR-10, FR-11, FR-13, FR-14

### Epic 4: High-Impact & Destructive Operations
Operators perform the highest-blast-radius actions safely — removing a user's access (the flagship remove-user journey), disabling or enabling a tenant, setting or removing configuration, and granting or removing a global administrator — each gated behind a full 10-item Consequence Preview, fail-closed validation+freshness+authorization, and asymmetric high-risk handling (last-owner = warning with elevated friction, never blocked; last-global-administrator = reflected as unavailable). Success is shown only after projection proof. This epic introduces the ConsequencePreview and DestructiveControl components and the fail-closed gating order. *Phase 2c · gate: FC-CNS (FR-15/FR-19 platform-wide, categorically `blocked`) · Realizes UJ-3 (flagship).*
**FRs covered:** FR-12, FR-15, FR-16, FR-17, FR-19

### Epic 5: Audit Trail, Evidence & Compensating Recovery
Operators investigate incidents and recover by correcting forward — browsing a tenant's flat, stably-ordered, cursor-paginated audit trail with date/category filters, reaching audit evidence from every context, reading support-safe Audit Evidence Receipts assembled client-side from NarrativePayload, distinguishing the four audit-availability states (each with a stated recovery, none shown as success), and starting/previewing/linking a compensating command — never "undo", with both the mistake and the fix permanently on the record. This epic includes the three net-new stories (FR-22 receipt, FR-24/FR-25 recovery) that no candidate backlog row covered. *Phase 2c · gates: FC-AUD, FC-CNS · Realizes UJ-4.*
**FRs covered:** FR-20, FR-21, FR-22, FR-23, FR-24, FR-25

---

## Epic 1: Operations Shell Foundation & Tenant Triage

An operator lands on a trustworthy operations console and triages tenants — scanning, filtering, opening detail, and returning with context intact, always knowing how fresh the data is — on a walking-skeleton foundation (host, shell, JWT/BFF, Fluxor TruthState, canonical `Vocabulary/` library) every later epic reuses. *Phase 2a · gate: FC-LYT · Realizes UJ-1 · FRs: FR-1, FR-2, FR-5, FR-7.*

Relevant UX-DRs: UX-DR1 (TenantDataGrid, TruthStateBadge), UX-DR2/3/4 (color firewall, caution ramp, no-color-only), UX-DR5 (mono ids/timestamps), UX-DR6/7/8 (layout, stable, pinned columns), UX-DR9 (six list states), UX-DR13/15/16/17 (live-region, localization, responsive, a11y ready-gate).

### Story 1.1: Bootstrap the Tenants UI host and Operations Shell

As a platform operator,
I want a signed-in, shell-composed Tenants console to exist and load,
So that there is a trustworthy, authenticated home before any tenant data or actions are built on it.

**Acceptance Criteria:**

**Given** no UI host exists in the repo today
**When** the bootstrap is implemented
**Then** a new `src/Hexalith.Tenants.UI` project exists (`Microsoft.NET.Sdk.Web`, `net10.0`, Blazor **InteractiveServer**, `EnableContainer`, `ContainerRepository=tenants-ui`), added to `Hexalith.Tenants.slnx` and orchestrated by `Hexalith.Tenants.AppHost`
**And** no package versions are in the `.csproj` (central `Directory.Packages.props`), no copyright headers are added, and the build passes `-warnaserror`.

**Given** an unauthenticated user
**When** they navigate to any UI route
**Then** they are challenged via JWT bearer (Keycloak/OIDC in prod; symmetric-key JWT when `EnableKeycloak=false`), and no surface renders before authentication (NFR-2).

**Given** an authenticated operator opens the app
**When** the root renders
**Then** `App.razor`/`Routes.razor` compose `FluentProviders` + the FrontComposer Shell with primary nav **Tenants (default landing) · Global Administrators · Audit**, and **Users reachable contextually** (not a co-equal tab)
**And** the access token stays server-side (browser never receives it; BFF egress only) (D1, NFR-2)
**And** the Audit area renders an honest "not yet available" placeholder, not a broken surface.

**Given** the trust edge
**When** the BFF query gateway is wired
**Then** `Services/Gateways/TenantQueryGateway` is the only backend egress (DAPR service invocation / Aspire service discovery to `GET /api/tenants*`), no browser-side `HttpClient` to the backend exists, and the Fluxor store + a `TruthState` feature scaffold are registered.

**Given** repo conventions
**When** the project is created
**Then** `ConfigureAwait(false)` and the file-scoped/Allman/`_camelCase`/`I`-prefix/`Async`-suffix conventions hold, and a `tests/Hexalith.Tenants.UI.Tests` (bUnit + xUnit v3, `{Class}Tests.cs`) project exists and runs in CI.

**Gate:** FC-LYT must be confirmed (or an approved layout fallback recorded) and the FrontComposer Shell-integration spike (`AddHexalithFrontComposer*` / manifest / routing APIs) done before build-ready.

### Story 1.2: Canonical state Vocabulary library and Truth State Badge

As an operator relying on the console under incident pressure,
I want every status shown through one honest, accessible badge drawn from a single canonical vocabulary,
So that what looks proven is proven, and no surface quietly redefines a state.

**Acceptance Criteria:**

**Given** the casing-significant canonical state sets (addendum §G, CP-10)
**When** the `Vocabulary/` library is built
**Then** it exposes typed, verbatim tokens for all six vocabularies — Truth State Badge (13), Freshness (5), Command lifecycle (10 + snake_case machine tokens), Layered feedback (10), Unavailable Action Reason (6), Audit availability (4) — plus recovery verbs
**And** badge `audit pending`/`audit available` and machine `audit_pending`/`audit_available` stay distinct (never unified)
**And** a guard test fails any surface referencing a raw state literal instead of the library.

**Given** the `TruthStateBadge` component (UX-DR1/2/4)
**When** it renders any status
**Then** it composes Fluent `BadgeColor` (semantic role by name, never hex) + `IconStart` glyph + visible text + `IconLabel` aria name, legible in light/dark/high-contrast/**forced-colors**, with color never the sole signal (`Filled` for Danger/Severe only, not counted toward no-color-only).

**Given** the Success-is-proven firewall
**When** an in-flight state renders (`accepted`, `submitted`, `previewed`, `projection pending`, `audit pending`, `refreshing`)
**Then** it is `Informative`, never `Success` (`accepted` explicitly not Success); `Success` appears only for projection-proven `current`/`confirmed`/`audit available`/tenant `active`.

**Given** the 3-tier caution ramp (UX-DR3)
**When** caution/uncertain/benign states render
**Then** Warning / Severe / Danger stay distinct, with Important (`unknown`/`blocked`/`unable to verify`) and Subtle (`eligible`/`already applied`/`duplicate`/risk `low`/`missing implementation support`) per the mapping.

**Given** timestamps (UX-DR5)
**When** a status shows a time
**Then** it is absolute, culture-formatted, monospace — never relative-only.

**Given** the TruthState Fluxor feature (D5)
**When** status is modeled
**Then** the non-collapse invariant is enforced in the reducer (`accepted` ≠ `confirmed` ≠ `audit available`; `degraded`/`unable to verify` success-prohibited) and last-confirmed projection is held separately from in-flight intent.

**Gate:** FC-TOK fallback = existing Fluent/FC badges; FC-A11Y needs-confirmation; verify every Fluent token/icon/ARIA name against the pinned `5.0.0-rc.3-26138.1` package at build.

### Story 1.3: Browse and triage the tenant list

As a platform operator triaging under pressure (UJ-1),
I want to scan, search, filter, sort, and page the tenant list with honest per-row state,
So that I can find the right tenant fast and know how fresh what I'm seeing is.

**Acceptance Criteria:**

**Given** the tenant list is the default landing surface
**When** an operator opens it
**Then** the `TenantDataGrid` (FC-TBL) loads via **cursor pagination (never offset/limit)** and each row shows tenant identity, status, member count, owner count, pending state, and a `TruthStateBadge` with freshness (FR-1).

**Given** the six list-surface states (UX-DR9, FR-1)
**When** the list is in each state
**Then** **loading, empty, filtered-empty, error, stale, degraded** render distinctly via the shared `ListSurfaceStates` component (filtered-empty offers filter reset; stale shows freshness marker + refresh; degraded explains what still works), and none collapse.

**Given** sorting/paging
**When** the operator sorts or pages
**Then** a pending or stale marker is never hidden, and row markers/actions hold stable width/placement (UX-DR7).

**Given** authorization scoping (NFR-2, §10)
**When** results render in any state
**Then** no out-of-scope tenant leaks (including in empty/error), and empty is authorization-safe.

**Given** conditional reads (D6, NFR-1)
**When** the gateway reads the projection
**Then** it uses `If-None-Match` → `304`; the badge derives `current/refreshing/aging/stale/unknown`; unmeasurable freshness shows `unknown` (fail-closed), never implying `current`.

**Given** safety-critical columns (UX-DR8)
**When** the grid scrolls horizontally
**Then** identity, status, and freshness are pinned `DataGridColumnPin.Start` and never drop.

**Given** automation + a11y (NFR-4, UX-DR15/17)
**When** the grid renders
**Then** every element/status carries `data-testid="tenants-tenant-list-*"` (never keyed on row text/color), table semantics (headers, sort state, row relationships) are exposed, and labels are localized whole strings.

**Gate:** FC-LYT, FC-A11Y, FC-L10N, FC-DOC (ready-gate) — `planning-only` until cleared or an approved fallback is recorded.

### Story 1.4: Open tenant detail and return with context preserved

As an operator (UJ-1),
I want to open a tenant's detail and come back to the list exactly where I was,
So that I can inspect a suspect tenant and keep triaging without losing my place.

**Acceptance Criteria:**

**Given** a tenant row
**When** the operator opens it (or deep-links its route)
**Then** the `TenantDetailPage` shows status (no-color-only encoding), metadata, member and configuration summaries, member/owner counts, and a freshness indicator (FR-5), and deep-linking is supported (FR-2).

**Given** the operator returns to the list
**When** they navigate back
**Then** prior filter, sort, and selection are restored and the previously selected tenant is highlighted (FR-2).

**Given** honesty (NFR-3, CP-3)
**When** detail data is shown
**Then** it reflects the projection with a freshness badge; an unmeasurable value shows `unknown`; nothing is shown more certain than it is.

**Given** support-safety (§10)
**When** ids/metadata render
**Then** no tokens/payloads/correlation-ids/PII appear and ids render monospace as literal strings.

**Given** a11y/responsive (UX-DR16/17)
**When** detail renders at narrow widths
**Then** safety-critical context is preserved (mobile = read-only), focus order is logical, and absolute timestamps are used.

**Gate:** FC-LYT, FC-A11Y, FC-L10N, FC-DOC.

### Story 1.5: Copy support-safe identifiers

As an operator citing a tenant/user to support (FR-7),
I want to copy the full, exact identifier and any support-safe reference,
So that I can reference it accurately without leaking anything sensitive.

**Acceptance Criteria:**

**Given** an id is visually truncated on screen
**When** the operator triggers copy-full-id
**Then** the copied value is the **literal caller-supplied string** (TenantId/UserId), never re-formatted and never parsed as a ULID/Guid (`Guid`/`Ulid.TryParse` not used) (FR-7, identity rule).

**Given** support-safety (§10, NFR-5)
**When** any copy runs
**Then** copied content is only the full id or a support-safe reference — never bearer tokens, decoded JWT, command payloads, serialized events, raw metadata, internal correlation ids, stack traces, or PII.

**Given** monospace ids (UX-DR5)
**When** shown
**Then** glyph-similar characters stay distinguishable and the visible-vs-copied values are consistent (copy faithful to the literal).

**Given** automation/a11y
**When** the copy control renders
**Then** it carries a stable `data-testid`, an accessible name, is keyboard-operable, and success feedback never implies more than "copied".

**Gate:** FC-A11Y, FC-L10N, FC-DOC.

<!-- NEXT: Epics 2-5 detail appended in subsequent passes. -->

