---
title: Tenants Management UI
status: final
created: 2026-06-02
updated: 2026-11-21
---

# PRD: Tenants Management UI

**Approved McpCli course correction (2026-09-27):** `Hexalith.McpCli` is the target Hexalith-owned CLI/MCP surface for Tenants operations that pass contract enrollment and authorization. Any proprietary module CLI, MCP host, plug-in, or planned adapter described below is an obsolete migration source or historical design, not a new target. The module retains its domain, UI, and security semantics; replacement or approved withdrawal and parity evidence precede retirement. External development CLIs are unaffected.

*Working title — confirm.*

## 0. Document Purpose

This PRD is the product-altitude definition of the **Tenants Management UI** — the end-user web application for managing tenants, members, roles, configuration, lifecycle, and audit on the Hexalith platform, built on the **Hexalith.FrontComposer** framework. It is written for the PM, downstream UX/architecture/epic owners, and reviewers.

It sits *above* an existing body of Epic 9 "Phase 2" readiness specifications in `docs/` and does not duplicate them — it gives them a product frame and references them as the source of technical depth:

- `tenants-ui-operations-shell-spec.md` — information architecture (the "Operations Shell").
- `tenants-ui-truth-state-and-action-availability-spec.md` — the truth/feedback interaction contract (canonical state sets).
- `tenants-ui-remove-user-from-tenant-journey-spec.md` — the first worked command journey.
- `tenants-ui-audit-evidence-and-compensating-recovery-spec.md` — audit evidence and recovery.
- `tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md` — cross-cutting quality + acceptance gates.
- `tenants-ui-responsive-layout-and-visual-system-spec.md` — responsive layout and visual language.
- `tenants-ui-frontcomposer-dependency-map.md` — what FrontComposer provides and what is missing.
- `tenants-ui-phase-2-story-backlog.md` — the `ui-01..15` story backlog.

**How to read this PRD:** vocabulary is anchored in the **Glossary (§4)**; features are grouped with globally numbered Functional Requirements (FR-N) nested under them; a single cross-cutting **interaction contract (§6)** governs every command flow and is referenced by ID rather than repeated; assumptions are tagged inline as `[ASSUMPTION]` and indexed in §17. Implementation mechanics (endpoints, components, dependency IDs, **canonical state-set enumerations**, fallback decisions, the rejection/NoOp matrix) live in `addendum.md`, not here. **A "final" status means this plan is complete — not that every story is unblocked; see the build-readiness status in §14.**

> The June–July 2026 review established the shell, grid and evidence prerequisites. Addendum §I records which prerequisites have since been delivered. Section 14 identifies the remaining release and deployment checks. [Prerequisite status](addendum.md#i-prerequisite-work-packages-2026-07-15-readiness-correction)

## 1. Vision

The Hexalith platform's tenant model is rich and safety-critical: tenants, their members and roles, namespaced configuration, lifecycle state, and an immutable event-sourced history with compensating-command corrections. Before the management UI, operators submitted commands and read projections directly. The UI gives authorized users a way to perform routine changes and check their outcomes.

The Tenants Management UI turns that capability into a **trustworthy operations and self-service experience**. A single role-scoped application serves two audiences from the same surfaces: **platform operators / global administrators**, who triage and act across every tenant, and **tenant owners**, who manage only their own tenant. It lets these users *see the truth* about who has access and what changed, *act safely* through previews and guardrails instead of raw commands, and *recover from mistakes* through forward compensating actions — all without ever editing history or the data store.

What makes this product distinctive is its honesty about state. In an eventually-consistent, event-sourced system the UI refuses to fake certainty: it tells the user how fresh the data is, blocks high-impact actions when it cannot prove the data is current, never reports success it has not confirmed against the source of truth, and treats a correction as a new auditable command rather than a silent "undo." The result is a console operators can trust under incident pressure and owners can use without fear.

## 2. Why Now

- The **Phase 1 backend is built and tested** — tenant/member/role/configuration/lifecycle commands, queries, cursor pagination, authorization, projection safety, and production JWT configuration all exist. A few hardening items remain deferred (notably cursor durability across replicas — §16.7); treat the backend as ready-but-not-frozen, not flawless.
- Operators can now inspect tenants, change membership and configuration, and track commands in the UI. Tenant creation and global-administrator grants and removals require global-administrator authority. The original read-only MVP and later command phases are described in §14.
- **FrontComposer** provides the shell/layout/command groundwork to begin composing this UI. Story 1.0 confirmed the main shell contracts on 2026-06-05; Story 1.2 resolved the `FC-TBL` boundary by using a Tenants-specific `TenantDataGrid` for cursor pagination, safety-column preservation, and six non-collapsing list states while leaving reusable grid capability in FrontComposer — see the build-readiness status in §14.

## 3. Target Users

The same application serves both audiences; what each sees is scoped by server-enforced authorization (§8 NFR-2), not by separate apps.

**Scope of self-service (v1):** tenant owners use the *same* surfaces as operators, authorization-limited to their own tenant — viewing their tenant, members, and configuration, and managing their members and roles (FR-3; FR-8/FR-9 read; FR-10/FR-11). There are **no dedicated owner-only screens, onboarding, or journeys** in v1; richer owner-scoped UX is an explicit downstream-UX gap, since the source specs are operator-centric. `[ASSUMPTION]`

### 3.1 Jobs To Be Done

**Platform operator / Global administrator** (e.g. *Elena*, platform operations engineer; *Sofia*, incident & support lead)
- When a tenant is reported as misconfigured or compromised, **find and triage the right tenant fast**, so I can act before impact spreads.
- Before I change anyone's access, **know whether what I'm looking at is actually current**, so I don't act on stale data.
- **See exactly who can do what** in a tenant — members, roles, owners, global administrators — so access reviews are quick and defensible.
- **Act safely on access and lifecycle** (add/remove members, change roles, disable/enable tenants) with a preview of consequences and confirmation it landed.
- When something was done in error, **recover by correcting forward**, with both the mistake and the fix on the record — never by erasing history.
- **Prove what happened** with audit evidence I can cite to support or compliance without leaking secrets.

**Tenant owner** (e.g. *Nadia*, owner of a customer tenant)
- **Manage my own tenant's members and roles** without filing a ticket or waiting on a platform operator.
- **See my tenant's configuration and status** and understand the effect of changing it.
- Be **stopped from doing something dangerous by accident** (e.g. removing my tenant's last owner) without being hard-blocked when I genuinely mean it.

**Member / self-auditing user** (e.g. *Marc*)
- **See which tenants I belong to and in what role**, so I can verify my own access.

### 3.2 Non-Users (v1)

- **Anonymous / unauthenticated visitors** — every surface requires authentication.
- **Programmatic integrators** — automation should continue to use the command/query APIs directly; this UI is for humans. `[ASSUMPTION]`
- **End-consumers of a tenant's product** — this manages tenants, not the tenant's own application users beyond membership.

### 3.3 Key User Journeys

Journeys are numbered globally (UJ-1..UJ-6) and referenced by FRs. Phase labels indicate when each becomes real (see §14). All journeys but UJ-5 are operator-driven; owner self-service in v1 rides the same read/manage surfaces (see "Scope of self-service" above).

- **UJ-1. Elena triages tenants under pressure.** *(Phase 2a / MVP)*
  - **Persona + context:** Elena, platform operations engineer, gets a report that "a tenant is acting up."
  - **Entry state:** authenticated as a global administrator; lands on the tenant list (the default triage surface).
  - **Path:** filters/searches the list → scans status, owner/member counts, and freshness → opens the suspect tenant's detail → returns to the list with her filters and selection preserved.
  - **Climax:** she has the right tenant open with its current state in front of her, and knows how fresh that state is.
  - **Resolution:** she proceeds to access review (UJ-2). **Edge case:** if a value can't be measured for freshness, it shows `unknown` rather than implying it is current.

- **UJ-2. Elena reviews who can do what.** *(Phase 2a / MVP — read; action availability reflected)*
  - **Persona + context:** continuing from UJ-1.
  - **Entry state:** tenant detail open.
  - **Path:** opens the member table → reads each member's role, owner count, status, and freshness → sees, per row, which actions *would* be available and, where one is not, a plain-language reason (e.g. "you don't have permission", "data is stale — refresh first").
  - **Climax:** she understands the access picture and exactly what she could safely change.
  - **Resolution:** in the MVP she stops here (read-only); in later phases she proceeds to a command (UJ-3).

- **UJ-3. Elena safely removes a user's access.** *(Phase 2c)*
  - **Persona + context:** Elena must revoke a user who should no longer have access.
  - **Entry state:** a specific member row, with the target user, current role, and freshness visible.
  - **Path:** the system validates inputs and gates (freshness + authorization must be `eligible`, or the action is unavailable with a stated reason) → she opens a **Consequence Preview** of what removal will and won't do (owner-count impact, the access being revoked, the recovery path, the audit expectation, and explicit known-unknowns) → for high-risk cases (e.g. this would drop owner count to zero, or the target also holds global-administrator authority) she clears elevated-friction confirmation → she confirms → the command dispatches and the **Command Lifecycle Panel** tracks it `submitted → accepted → projection_pending → confirmed`, then `audit_pending → audit_available`.
  - **Climax:** access is changed and **proven**, with no false "done" shown before the source-of-truth projection confirms it.
  - **Resolution:** the change is on the audit record. **Edge cases:** incomplete preview inputs block submission (fail-closed); removing the last owner is a warning with extra friction, never a hard block; a removal that is already applied reads as `already applied`, not failure; if reconciliation can't confirm, the state is `unable to verify` (never shown as success); if she lost permission mid-flow she is told and offered a recovery path.

- **UJ-4. Sofia investigates an incident and recovers.** *(Phase 2c)*
  - **Persona + context:** Sofia, incident & support lead, must understand and undo the *effect* of a mistaken access change.
  - **Entry state:** opens the audit trail from a contextual entry point (a tenant row, tenant detail, a user lookup, or a command result — not a nav area; §5.1).
  - **Path:** Sofia reads the original Audit Evidence Receipt, identifies the mistaken tenant membership change, and previews the intended role against current membership. She submits the corrective command and watches for the projection to confirm the change.
  - **Climax:** the confirmed projection shows the intended access. Sofia can still inspect the original receipt. The tenant workflow shows `missing implementation support` for paired proof (FR-25).
  - **Resolution:** Sofia can report the confirmed access change and cite the original support-safe reference. If asked for a linked corrective receipt, she explains the evidence available and uses the support escalation path.

- **UJ-5. Nadia self-serves her own tenant.** *(Phase 2a / MVP — read; role change Phase 2b)*
  - **Persona + context:** Nadia owns a customer tenant and wants to manage her team without a ticket.
  - **Entry state:** authenticated as a tenant owner; she sees only her own tenant (authorization-scoped).
  - **Path:** opens her tenant's member table → reviews members and roles → (later phase) changes a teammate's role and watches it confirm.
  - **Climax:** she manages her own access picture independently.
  - **Resolution:** platform operators are no longer a bottleneck for her routine changes. **Edge case:** she never sees other tenants or the Global Administrators surface.

- **UJ-6. Elena onboards a new tenant.** *(Phase 2b/2c)*
  - **Persona + context:** a new customer needs a tenant stood up.
  - **Entry state:** authenticated as a global administrator.
  - **Path:** creates the tenant → adds the first owner directly by user id → sets the initial configuration → verifies that each step completed.
  - **Climax:** a usable, owned, configured tenant exists.
  - **Resolution:** the owner (Nadia) can now self-serve (UJ-5). **Edge case:** adding a user is a *direct* add by user id — there is no email-invitation step in v1 (§13). `[ASSUMPTION]`

## 4. Glossary

Downstream workflows and readers must use these terms exactly; FRs, UJs, and SMs use them verbatim. The full canonical state-set enumerations are in addendum §G.

- **Tenant** — an isolated organizational scope on the platform, identified by a **meaningful caller-supplied string id (not a ULID)**. Has members, namespaced configuration, and a lifecycle status.
- **Member** — a user who holds a **Role** in a **Tenant**. Identified by a **caller-supplied user id (not a ULID)**.
- **Role** — a member's permission level within a tenant (`TenantRole`); roles include an owner-level role. Has a `Unknown` sentinel that is never a valid target.
- **Owner / Owner count** — members holding the owner-level role; **owner count** is how many a tenant has. Reducing it to zero is the **last-owner** case (allowed by the backend — see CP-6).
- **Global administrator** — a platform-level governance principal in the separate `global-administrators` scope (a single fixed-identity aggregate, not tenant-routed); distinct from tenant membership. The **last global administrator** is protected by a backend invariant (its removal is rejected — see CP-6).
- **Tenant lifecycle status** — active or disabled. Disabling a tenant stops membership, configuration and metadata changes. Consumers observe the change asynchronously. FR-15 defines reactivation and access refresh.
- **Configuration** — tenant key/value settings grouped by namespace. The management UI displays exact keys approved as safe. Tenant-scoped users also need a matching namespace prefix (FR-6).
- **Operations Shell** — the application's information architecture: a FrontComposer shell whose left navigation exposes **one entry per module**. The single **Tenants** module entry opens the Tenants workspace, which groups Tenants-domain read surfaces as page-local tabs (first tab **Tenants** = list/triage; second tab **Users** = lookup-backed membership). Global Administrators and Audit are reached through module-internal/contextual paths, not separate left-menu entries (see §5.1; Correct Course 2026-06-27).
- **Pending state** — a tenant/row indicator that a command affecting that tenant or member is in flight (not yet confirmed).
- **Projection** — the read model the UI reads from; the **source of truth** for confirmation is the projection, not optimistic UI state or live notifications.
- **Truth State Badge** — the visible indicator of how trustworthy displayed data is, composing freshness, authorization, command lifecycle, projection confirmation, and audit dimensions. Its full canonical 13-state set is defined once (addendum §G) and used verbatim everywhere — no per-screen reinterpretation.
- **Freshness** — how current the displayed data is: one of `current`, `refreshing`, `aging`, `stale`, `unknown`. `aging` is **usable with friction**; `stale` and `unknown` block high-impact action. Numeric thresholds are set per implementation, not in this PRD.
- **Freshness Gate** — the rule that blocks access-impacting/destructive actions when freshness is `stale`/`unknown` (fail-closed) unless an approved override exists.
- **Unavailable Action Reason** — the plain-language, hover-free reason an action is disabled. The canonical six categories are `missing permission`, `stale data`, `missing lifecycle support`, `missing consequence preview`, `missing audit proof`, `high-impact flow not ready` (addendum §G).
- **Consequence Preview** — the pre-submission summary of what a command will and will not do, including **known consequences** and **known unknowns**, shown before destructive/high-impact actions. Assembled and redacted in the server-side BFF; its content set is defined in addendum §H.
- **Command Lifecycle Panel** — the surface that tracks a dispatched command's state without overwriting confirmed projection data.
- **Non-collapse invariant** — the rule that `accepted` ≠ `confirmed` (projected) ≠ `audit available` (proven); these — and `degraded` and `unable to verify` — are never merged or shown optimistically as success.
- **Fail-closed** — when freshness is unknown, authorization indeterminate, the consequence preview incomplete, or lifecycle support missing, the action is blocked rather than allowed.
- **Compensating command** — a forward command that corrects the *effect* of a prior action (e.g. re-adding a user). Corrections append new events; history is never edited or deleted. The UI never labels this "undo".
- **Audit Evidence Receipt** — the support-safe record of a recorded action: actor, target, tenant scope, outcome, timestamp, projection marker, and an audit/command reference. Assembled and redacted in the server-side BFF from a structured **NarrativePayload** (never the raw event payload); rendered components receive only support-safe localized fields.
- **AuditEventCategory** — the category an audit entry belongs to: `Access` or `Administrative`.
- **Audit availability** — one of `audit pending`, `audit delayed`, `audit unavailable`, `missing implementation support` (addendum §G); none is ever shown as success.
- **Support-safe reference** — an identifier safe to share that never exposes payloads, bearer tokens, stack traces, internal correlation ids, raw event metadata, or PII.
- **Orphan membership** — a membership whose context (e.g. a disabled tenant) makes it require operator attention.
- **NoOp** — a same-state request that produces no event — specifically `ChangeUserRole` to the current role and `SetTenantConfiguration` with an identical key+value; the UI reflects `already applied`. (Note: re-adding an existing member is *not* a NoOp — it is rejected; see addendum §D.)
- **Rejection** — a business-rule refusal returned as a domain rejection event, surfaced through a BFF-assembled, support-safe rejection view model as safe, localized text — never a stack trace.
- **Proposed fallback** — a simpler implementation proposed for when a richer FrontComposer component is unavailable (e.g. a flat audit list instead of a timeline). A fallback is **only usable once Product/UX has approved it**. **The three interim fallbacks — `FC-AUD` → flat audit grid, `FC-CNS` → inline consequence text, `FC-CNC` → one-at-a-time commands — are approved** (recorded 2026-06-03; see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md)); each remains build-ready only once its other FrontComposer gates clear. The `FC-CNC` one-at-a-time scope is since superseded (2026-07-15): command locking is **aggregate-scoped per AD-12** (`architecture.md`) — one active command per (interactive circuit, AggregateIdentity) through the operation’s tracking period, while unrelated aggregates proceed. AD-12 defines terminal evidence, the lifecycle and configuration release conditions, and continued membership recovery after a projection timeout. Bulk submission, toast batching, and multiple simultaneous tracked commands for one aggregate remain prohibited.

## 5. Information Architecture & Visual Language

### 5.1 Operations Shell
The shell left navigation exposes **one entry per module**. For the Tenants module, the single **Tenants** entry opens the Tenants module workspace. The workspace groups Tenants-domain read surfaces as **page-local tabs** (reconciled with the shipped tabbed-workspace implementation, Correct Course 2026-06-27, Option A; supersedes the prior 2026-06-03 "three primary nav areas / Users contextual" model).

The first tab is **Tenants**: the tenant list / triage surface with search, status filter, cursor paging, freshness, and pending state, plus an optional "my tenants" filter that narrows results to tenants the signed-in user belongs to when the caller is authorized.

The second tab is **Users**: the user-membership surface showing a user's tenant memberships. It is currently **lookup/search-backed** (realized by FR-3 "My Tenants" and FR-4 user lookup) and must not be labeled or behave as an exhaustive all-users inventory. Promoting it to a complete cursor-paged list of all users and their memberships requires the backend to add an authorization-scoped read query first.

Global administrator and audit capabilities remain available through module-internal tabs or contextual entry points (tenant rows, tenant detail, user lookup, command results), **not** as separate left-menu entries for the Tenants module unless a future module-level IA decision adds them explicitly.

Command lifecycle is **never** a navigation area — command status and feedback are shown inline, anchored to the affected row/panel. Navigating between surfaces and tabs preserves context (active tab, selection, filters) so users don't lose their place.

### 5.2 Visual language & tone
- **Microsoft Fluent UI** is the visual authority; there is no separate branded palette. Meaning maps to **semantic theme roles**, never hard-coded colors. `[ASSUMPTION]`
- Tone is a **professional, calm, precise operations console** — not marketing. System UI typography, modest hierarchy, plain-language labels, compact density; whitespace groups meaning rather than adding drama. Tables, split views, and side panels are preferred over decorative card grids.
- **No-color-only encoding:** every status is conveyed by text + icon/shape as well as color, and must remain legible in light, dark, high-contrast, and forced-colors modes.
- Layout is **stable** (reserves space to avoid shift); destructive/warning styling is used sparingly.
- **Out of the first visual slice** (unless Product/UX promotes): decorative card dashboards, branded palettes, hero-scale typography, advanced grouped visual modes, and bespoke per-state color literals.

### 5.3 Form factors
- **Breakpoints** (a layout rule, not just test widths): **mobile 320–767px, tablet 768–1023px, desktop 1024px+, wide desktop 1440px+.**
- **Desktop-first** (the primary admin workstation: dense tables, keyboard/mouse, side-by-side context).
- **Tablet:** navigation collapses, regions stack, tables preserved via scroll / column-priority (not gesture-redesigned).
- **Mobile:** **read-only triage, lookup, and audit reference only — no high-impact command flows.** `[ASSUMPTION]`
- **Safety-critical columns never drop:** identity, status, freshness, role, and risk are preserved at every width (via horizontal scroll / column priority), never hidden.
- A **fail-closed responsive rule:** if a width cannot preserve the full safety context for a high-impact action, that action becomes unavailable (with a visible reason) rather than rendering unsafely.

## 6. Core Interaction Principles — Truth, Safety & Recovery *(cross-cutting contract)*

This is the interaction contract that every command FR in §7 references. It is a product requirement, not an implementation detail.

- **CP-1 Five truth dimensions.** Every actionable surface reasons over Freshness, Authorization, Command lifecycle, Projection confirmation, and Audit evidence, surfaced via the **Truth State Badge** (13 canonical states — addendum §G).
- **CP-2 Fail-closed.** `stale`/`unknown` freshness, indeterminate authorization, an incomplete Consequence Preview, or missing lifecycle support each **block** an access-impacting/destructive action unless an explicitly approved override exists. (`aging` data remains usable with friction; only `stale`/`unknown` freshness blocks.) The user is told *which* of these blocked them via an **Unavailable Action Reason**.
- **CP-3 Non-collapse invariant.** `accepted`, `confirmed` (projected), and `audit available` (proven) are distinct and never merged; `degraded` and `unable to verify` are likewise distinct, **success-prohibited** states. The UI **never shows success — styling, copy, or announcement — that it has not confirmed against the source-of-truth projection.**
- **CP-4 Live signals are nudges, not proof.** Real-time projection notifications prompt a refresh; they never advance a command to `confirmed` or audit to `audit available`.
- **CP-5 Consequence Preview before destructive action.** High-impact/destructive commands require a preview of known consequences and known unknowns (content set in addendum §H); incomplete inputs block submission (fail-closed).
- **CP-6 High-risk actions — friction vs. hard stops (asymmetric).** The **last-owner** case is *allowed* by the backend — surfaced as a warning with elevated friction, never blocked (the system supports deliberately reducing a tenant to zero owners). The **last-global-administrator** case is the opposite: the backend **hard-rejects it** (a domain invariant), so the UI reflects it as *unavailable with a clear reason*, not as completable friction. Acting on a target who **also holds global-administrator authority** raises an additional platform-level friction flag; tenant-membership and `global-administrators` actions are never conflated.
- **CP-7 Correct forward, never "undo".** Recovery uses a new **compensating command** with its own preview. The original record stays in the immutable trail. The UI reports the confirmed outcome and the available audit evidence separately (FR-25). The words "undo", "rollback", and "hidden edit" are never used.
- **CP-8 Distinct recovery for every failure mode.** Stale data → refresh; pending → wait; status-lookup failure → retry status lookup; missing permission → request permission/escalate; wrong change → start correction / restore intended access; unverifiable → escalate with a support-safe reference. The UI never dead-ends.
- **CP-9 Authorization is reflected, never enforced, by the UI** (see §8 NFR-2 / §10).
- **CP-10 Canonical state sets, used verbatim.** The full enumerations — Truth State Badge (13), freshness (5), command lifecycle (10), layered feedback (10), Unavailable-Action-Reason (6), audit availability, recovery verbs — are defined in the truth-state spec and mirrored verbatim in addendum §G. Every surface uses them as written (including the deliberate casing distinction between the badge's `audit pending` and the RemoveUserFromTenant state machine's `audit_pending`); **no per-screen reinterpretation.**

## 7. Features

Each feature lists its candidate backlog id(s) and phase. FR consequences are testable. Command FRs inherit the §6 contract. The rejection/NoOp behavior referenced below is specified in addendum §D.

### 7.1 Tenant Discovery & Triage *(ui-01, ui-02 — Phase 2a / MVP)*
**Description:** The entry experience. Operators scan, filter, and open tenants from a paginated list; users self-audit their own memberships; operators look up a specific user's memberships. Realizes UJ-1, UJ-2, UJ-5.

#### FR-1: Browse and triage the tenant list
A platform operator can scan, search, filter, sort, and page through tenants. Realizes UJ-1.
- **Consequences (testable):** list paginates via cursor (never offset/limit); each row shows tenant identity, status, **member count, owner count, pending state,** and a Truth State Badge with freshness; the list renders its distinct states — **loading, empty, filtered-empty, error, stale, degraded** — without collapsing them; **sorting or paging never hides a pending or stale marker**; all states are authorization-safe (no leak of out-of-scope tenants).
- **Search consequences (testable; cc-2026-06-21):** search matches a tenant by its **Name or TenantId** across the **entire** tenant set (not just the loaded page), computed by `Hexalith.Memories` **syntactic/BM25** search against a dedicated `tenants-index`; a non-empty term triggers a **server round-trip** (no in-memory page re-filter), an empty/whitespace term returns the unchanged cursor list; matched rows are **hydrated fresh** via the direct Tenants REST read path with authoritative freshness provenance (the search index never supplies row data; UI-READ-1 / PLAT-FRESH-1, addendum §I); search paging uses a **protected, scope-bound cursor**, never a plaintext Memories offset — on scope mismatch, decode failure, or invalidation the list restarts from page 1 with an honest localized notice (SEARCH-CURSOR-1); the **status filter is exact** (structured attribute once the Memories REST filter ships; the hydrated authoritative status in the interim), never fuzzy text; search is **eventually consistent** (a new/renamed tenant becomes findable once indexed; rows always render correct data regardless of index lag); and search **never blocks the list** — if Memories is unavailable the list falls back to the cursor view with a non-blocking notice (no tokens/correlation-ids/ETags in any user-facing copy).

#### FR-2: Open a tenant and return with context preserved
A user can open a tenant's detail and return to the list with prior selection and filters intact. Realizes UJ-1.
- **Consequences:** returning from detail restores the prior filter/sort/selection; deep-linking to a tenant detail is supported.

#### FR-3: Self-audit "My Tenants"
A signed-in user can view the tenants they belong to and their role in each. Realizes UJ-5.
- **Consequences:** shows only memberships the caller is authorized to see; role and tenant status are shown per row.

#### FR-4: Look up a user's memberships
An operator can search for a user and view that user's tenant memberships, and reach a user from a member row. Realizes UJ-2.
- **Consequences:** results are authorization-scoped; a user with no visible memberships shows an explicit empty state, not an error.

### 7.2 Tenant Detail & Configuration View *(ui-03, ui-05 — Phase 2a / MVP)*
**Description:** A read-only inspection surface for one tenant. Realizes UJ-1, UJ-2.

#### FR-5: View tenant overview
A user can view a tenant's status, metadata, and member/configuration summaries on one surface.
- **Consequences:** overview shows lifecycle status with no-color-only encoding and a freshness indicator; counts (members, owners) are shown.

#### FR-6: View tenant configuration (read-only)
An authorized user can view approved configuration entries grouped by namespace. Each displayed key must appear on the exact display-safe key list. Tenant-scoped callers also need an authorized prefix that matches the key using ordinal comparison. A global administrator bypasses only the prefix check.
- **Acceptance.** A key outside the display-safe list stays hidden even under an allowed prefix or when a global administrator opens the view. A listed key outside a tenant-scoped caller’s granted prefixes also stays hidden. A missing or malformed policy withholds configuration. The authorized API retains its full response. The server removes raw configuration before composing UI state. [Configuration display decision](../../../implementation-artifacts/dw-117-118-127-599-618-validation-evidence.md)

#### FR-7: Copy support-safe identifiers
A user can copy a full identifier (which may be visually truncated on screen) and any support-safe reference.
- **Consequences:** copied content is the full id (a caller-supplied string — not assumed to be a ULID); no payloads, tokens, correlation ids, or PII are ever exposed.

### 7.3 Member & Access Review *(ui-04 — Phase 2a / MVP)*
**Description:** Review who has access to a tenant and what is actionable. Realizes UJ-2, UJ-5.

#### FR-8: Review the member table
A user can review a tenant's members with role, owner count, status, freshness, and orphan context, read-only.
- **Consequences:** the table is **read-only and must not imply mutation**; it exposes accessible semantics (headers, sort state, row relationships); freshness is per the Truth State Badge; orphan/disabled context is flagged.
- **Roster access.** Before successful creation, including after a rejected creation attempt, a global administrator sees “tenant not found.” Other authenticated callers receive “forbidden,” which conceals whether the tenant exists. Once the tenant exists, authorized callers can see its roster, including an empty one. Callers outside its scope still receive “forbidden.”

#### FR-9: See action availability and reasons
A user can see, per member, which actions would be available and — when one is not — a plain-language **Unavailable Action Reason**. *(Reflective in MVP; actions arrive in later phases.)* Realizes UJ-2.
- **Consequences:** reasons use the six canonical categories (addendum §G); reasons are inline-visible (not hover-only; tooltips may supplement but are never the only explanation).

### 7.4 Member & Role Management *(ui-09, ui-14 — Phase 2b/2c)*
**Description:** The command flows that change tenant membership. All inherit §6. Custom command flows — not generated CRUD. Realizes UJ-3, UJ-5, UJ-6.

#### FR-10: Add a user to a tenant *(Phase 2b)*
An authorized user can add a user to a tenant with an explicit role, by user id. Realizes UJ-6, UJ-5.
- **Consequences:** add is a **direct** add by caller-supplied user id (no invitation/pending step — §13); **adding a user who is already a member is *rejected* (`UserAlreadyInTenant`), surfaced as safe localized text — it is not a NoOp**; a corrective add states the explicit intended role. *(ui-09 bundles add + change-role behind a shared availability gate.)*

#### FR-11: Change a member's role *(Phase 2b)*
On an active tenant, a tenant owner or global administrator can change a member’s role. For example, an owner can promote a reader to contributor. Realizes UJ-5.
- **Acceptance.** The assignable roles are reader, contributor and owner. Unauthorized callers receive a permission rejection. An authorized change to the current role returns `already applied`. An `Unknown` or out-of-range target yields a safe, localized `RoleEscalation` rejection. A changed role shows success after projection confirmation (CP-3).

#### FR-12: Remove a user from a tenant *(Phase 2c)*
An authorized user can remove a user's tenant access, with Consequence Preview, fail-closed gating, elevated-friction handling, and proof via audit. Realizes UJ-3.
- **Consequences:** required inputs (target, tenant, current role, freshness, authorization) are validated before the preview, and incomplete preview inputs block submission (CP-5); the Consequence Preview states owner-count impact, the access revoked, the recovery path, the audit expectation, and known-unknowns (addendum §H); reducing owner count to zero triggers elevated friction but is **not blocked**, and a target who also holds global-administrator authority raises platform-level friction (CP-6); the control is **not a primary/casual button**; an already-applied removal reads as `already applied` and duplicate submits are de-duplicated; the Command Lifecycle Panel tracks `submitted → accepted → projection_pending → confirmed → audit_pending → audit_available` without collapsing states, and an unconfirmable outcome shows `unable to verify` (never success) (CP-3); every failure mode maps to a stated recovery (CP-8).
- **Downstream access.** Confirmation establishes the removal in Tenants. A service enforcing membership from tenant events must withhold access while it repairs a detected gap. If an access decision depends on a known removal, the consumer must advance its checkpoint through that event before it can grant access. A delayed removal must remain effective after restart and redelivery of an earlier grant.
- **Notes:** uses the Product/UX-approved `FC-CNS` inline consequence fallback, the `FC-CMD` command-lifecycle contract, and the `FC-CNC` concurrency policy (aggregate-scoped per AD-12). Story 2.4 delivers the complete FR-12 vertical slice — fail-closed gating, consequence preview, elevated friction, projection-confirmed removal, audit-availability handling, and minimum support-safe removal proof (work package WP-2A, addendum §I). It has no dependency on Epic 5; Epic 5 generalizes audit browsing and recovery from this foundation, it does not complete FR-12 retroactively.

### 7.5 Tenant Lifecycle Management *(ui-07, ui-08, ui-13 — Phase 2b/2c)*
**Description:** Create, edit, and enable/disable tenants. Realizes UJ-6.

#### FR-13: Create a tenant *(Phase 2b)*
Only a global administrator can create a new tenant; the domain enforces this authorization rule. Realizes UJ-6.
- **Consequences:** callers without global-administrator authority see the action as unavailable with `missing permission`. If an unauthorized creation request is nevertheless dispatched, the domain emits `InsufficientPermissionsRejection`, which the UI maps to safe localized `InsufficientPermissions` text. Creating a tenant with an existing tenant id is rejected with safe text (`TenantAlreadyExists`); success is shown only after projection confirmation.

#### FR-14: Edit tenant metadata *(Phase 2b)*
A tenant contributor, tenant owner or global administrator can edit an active tenant’s metadata.
- **Consequences:** **every successful edit emits an update — there is no same-state suppression** (the backend always records `TenantUpdated`); validation errors surface as safe localized field messages.

#### FR-15: Disable or enable a tenant *(Phase 2c)*
An authorized operator (**global administrator only**) can disable or enable a tenant (high-impact, platform-wide), with Consequence Preview.
- **Acceptance.** A global administrator can re-enable a disabled tenant. Repeating an authorized disable or enable returns `TenantLifecycleStateAlreadySet`. Other callers receive a permission rejection. Membership, configuration and metadata mutations on a disabled tenant return `TenantDisabled`. An authorized access snapshot refresh can still publish the disabled status. The preview explains the delay before consumers see a transition. The UI shows success after projection confirmation and conveys status through text as well as color.
- **Scope.** Enable and disable are reversible. Hard deletion is outside the UI scope. Any proposal to expose hard deletion through a CLI or MCP must specify the contract, required authority and enrollment in `Hexalith.McpCli` (§0).

### 7.6 Tenant Configuration Management *(ui-10 — Phase 2c, high-impact)*
**Description:** Set and remove namespaced configuration. Inherits §6.

#### FR-16: Set a configuration value *(Phase 2c)*
An authorized user can set a namespaced configuration key/value, with Consequence Preview required for every eligible configuration mutation in v1.
- **Consequences:** identical key+value is a **NoOp** (`already applied`); values exceeding domain limits are rejected with safe text (`ConfigurationLimitExceeded`); no low-risk-key bypass exists in v1, and any future narrowing to a high-risk subset requires a Product/UX/Architecture decision that defines key classification, user-facing reasons, tests, and phasing impact (§16).

#### FR-17: Remove a configuration key *(Phase 2c)*
An authorized user can remove a configuration key, with Consequence Preview required for every eligible configuration removal in v1.
- **Consequences:** removing a missing key surfaces a safe `ConfigurationKeyNotFound` rejection; removal shows success only after projection confirmation; no low-risk-key bypass exists in v1.

### 7.7 Global Administrator Governance *(ui-06 read — Phase 2a / MVP; ui-15 commands — Phase 2c)*
**Description:** Review and manage platform-level global administrators, kept distinct from tenant membership.

#### FR-18: Review global administrators *(Phase 2a / MVP, read)*
An authorized operator can review who holds global-administrator access, separate from any tenant membership. Realizes UJ-2.
- **Consequences:** the surface is visible only to authorized operators; tenant owners (e.g. Nadia) never see it; data comes from the single fixed-identity `global-administrators` aggregate (not tenant-routed); rows show identity and a freshness badge.

#### FR-19: Grant or remove a global administrator *(Phase 2c, high-impact, platform-wide)*
An authorized operator can grant a global administrator, or remove one **except the last**.
- **Consequences:** **removing the last global administrator is rejected by the domain (`LastGlobalAdministrator`)** — the UI reflects this as an *unavailable* action with a safe reason, not as completable friction (CP-6, asymmetric with last-owner); operations are in the `global-administrators` scope, never conflated with tenant membership.
- **Retrying a grant.** If delivery is uncertain and no status reference is available, the retry control and its handler use the same eligibility rule. A change of caller, an invalidated or superseded preview, or loss of permission blocks dispatch. An eligible retry reuses the original command identity. Once a status reference is available, refresh checks that command’s status.

### 7.8 Audit Trail & Evidence *(ui-11, ui-12 — Phase 2c; flat-list fallback approved 2026-06-03; Epic 5 delivery is historical evidence)*
**Description:** The evidentiary surface — see what changed, with support-safe receipts. Realizes UJ-4.

#### FR-20: Browse a tenant's audit trail
A user can browse a tenant's audit entries as a flat, stably ordered list with date and `AuditEventCategory` (`Access` / `Administrative`) filters. Realizes UJ-4.
- **Consequences:** list paginates via cursor; the audit list must meet the approved 50-row page budgets for initial render, filtering, paging and visible feedback (§16.14); loading/empty/filtered-empty/error states are distinct and accessible; the flat list uses the Product/UX-approved `FC-AUD` DataGrid fallback (Epic 5 delivery is historical evidence, subject to reverification against the corrected contracts). The reusable FrontComposer timeline remains a deferred replacement path, not a prerequisite for the Tenants slice.

#### FR-21: Reach audit from context
A user can reach audit evidence from a tenant row, tenant detail, a user lookup, and a command result — contextual entry points, not a separate nav area (§5.1).
- **Consequences:** each entry point lands scoped to the relevant tenant/user/command.

#### FR-22: View an Audit Evidence Receipt
A user can view a support-safe receipt for a recorded action (actor, target, tenant scope, outcome, timestamp, projection marker, audit/command reference).
- **Consequences:** the receipt view model is assembled and redacted in the server-side BFF from a structured **NarrativePayload** (no new backend receipt endpoint); rendered components receive only support-safe localized fields and never raw `NarrativePayload`, event bodies, command payloads, tokens, internal correlations, ETags, or raw metadata; partial completion shows the actual lifecycle state (e.g. `audit pending`), never pre-rendered proof (CP-3).

#### FR-23: Distinguish audit availability states
A user can tell apart `audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support`, each with a stated recovery.
- **Consequences:** The UI offers wait, retry or escalation according to the evidence state (CP-8). A failed read yields `audit unavailable`. When the workflow lacks a capability needed to establish proof, such as linking a tenant correction to its original receipt, it shows `missing implementation support`. Both outcomes withhold a success claim for that evidence.

### 7.9 Compensating Recovery *(Phase 2c)*
**Description:** Correct the effect of a prior action forward, from audit evidence. Inherits §6. Realizes UJ-4.

Stories 5.5–5.6 cover tenant corrections. Story 5.7 delivered global-administrator correction in the fixed governance scope. Each attempt requires current authority, a complete ordered projection, a complete preview, and the last-administrator check. A legacy governance projection needs an ordered replay from durable history before the action becomes available. FR-25 specifies what evidence each flow can show. [Governance correction acceptance](../../../implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md)

#### FR-24: Start a compensating command
From audit evidence, an authorized user can start a correction ("restore intended access" / "start correction").
- **Acceptance.** Preview against current membership and submit a new forward command with an explicit intended role. The original event stays in the audit trail. First-member bootstrap applies only while an active tenant has never had a member. Removing its last owner leaves its membership history intact. An unauthorized caller receives a permission rejection and must escalate. A global administrator can restore an owner by adding an absent member or changing an existing member’s role. [Domain regression for an emptied tenant](../../../../tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs#L1626)

#### FR-25: Preview a correction and verify its audit evidence
The operator previews against current state. The correction is confirmed when a fresh, ordered projection reaches the verified command’s committed sequence and shows its intended effect. The UI reports audit evidence separately.
- **Tenant corrections.** The current workflow retains the original receipt and displays `missing implementation support` for the link to a corrective receipt. The operator can inspect the original and escalate using its support-safe reference. Deterministic pairing for tenant corrections remains outstanding. [Tenant correction contract](../../../implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md)
- **Global-administrator corrections.** The UI can link receipts within the same circuit when it reauthorizes the caller and verifies both protected audit rows against the original source, the same command attempt and its ordered projection evidence. If it cannot establish causation, it withholds the link and reports the applicable audit state (FR-23). A shared target, timestamp, or projected role alone cannot establish a pair.

**Audit performance (FR-20–FR-25).** Measure the audit grid with its evidence and correction controls visible. Use the approved workload, budgets and fallback in §16.14. Run the dedicated workload for the release candidate and repeat it after changes to the audit query, BFF mapping, grid or paging.

## 8. Cross-Cutting Non-Functional Requirements

- **NFR-1 Performance & freshness.** The UI uses cursor pagination and conditional reads. On data and unchanged responses, freshness comes from authoritative projection metadata. Missing or unusable freshness evidence yields `unknown`. `ServedAt` records when the response was served. For a typical tenant, the proposed warm list, detail and member target is about one second to interactive. `[ASSUMPTION]` The audit grid has its own approved budgets (§16.14).
- **NFR-2 Security & authorization.** Authorization is **server-enforced** at the API layer and in the domain; the UI **reflects** authorization state and never enforces it. The UI must remain safe if it misjudges authorization — the server is the gate. Role-scoping (tenant owner sees only their tenant; global administrator sees all) is enforced in the projection/query layer. In Production, the public REST API and the domain host must refuse to start with an invalid identity configuration. Both require OpenID Connect discovery over HTTPS, an explicit issuer and audience, and an allowed asymmetric signing algorithm. Both hosts reject configured shared signing keys in Production.
- **NFR-3 Reliability & consistency.** The system is eventually consistent; the UI treats the projection as source of truth, re-queries to confirm, and is correct under at-least-once delivery and projection lag (CP-3, CP-4).
- **NFR-4 Observability & testability.** Every interactive element and status carries a **stable automation selector / component contract** (never keyed on row text or color), so acceptance and E2E tests are robust.
- **NFR-5 No data-store edits.** The UI never edits, deletes, or rewrites events, projections, or state to "fix" data; corrections are compensating commands only (CP-7).

## 9. Accessibility & Localization

- **A11y standard:** baseline **WCAG 2.1 AA**; target **WCAG 2.2 AA where the selected Fluent UI Blazor / FrontComposer stack supports it** (conditional — no unconditional 2.2 promise). `[ASSUMPTION]`
- **Keyboard & focus:** all interactive elements reachable; logical focus order; visible focus in normal/high-contrast/forced-colors; modal focus trap with a safe escape that does **not** commit a destructive action; focus returns to the launching row/control after close/cancel/submit/failure; **keyboard users can complete or exit every modal/preview/table/command workflow.**
- **Screen reader & status:** accessible names for all statuses, badges, freshness indicators, and actions; absolute timestamps (not relative-only); table semantics (headers, sort state, row relationships); live regions with appropriate politeness — `assertive` reserved for rejection/failure/destructive-blockers/unable-to-verify; **never announce success before projection truth**.
- **No-color-only & motion:** color never the sole signal; reduced-motion users are never dependent on animation.
- **Localization:** all state labels, role names, timestamps, warnings, disabled reasons, recovery actions, confirmation copy, and empty/loading/error/degraded/stale/unavailable copy are localizable, with culture-aware formatting. **No runtime sentence-fragment assembly** — whole resource strings with indexed .NET placeholders such as `{0}` and `{1}`. Translations may reorder the placeholders while preserving the meaning of each index. Tenants owns domain strings and FrontComposer owns shell strings (§16.4). **RTL support is undecided** (§16). `[ASSUMPTION]`
- **Acceptance evidence (definition of done for UI work):** keyboard-only navigation; screen-reader review (NVDA + at least one browser/SR pairing); automated accessibility checks; forced-colors/high-contrast; reduced-motion; contrast; live-region announcements; focus return; hover-free disabled explanations; command-lock retention through `accepted` and `projection_pending` states; and reason-honesty for degraded, unavailable, and unknown projection states. Required acceptance scenarios: **stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, permission-missing, command accepted but still projection-pending, focus escape/cancel no-commit, and data unavailable but not authorization-denied.** Responsive evidence at desktop (1024/1366/1440 + wide), tablet (768/1024), mobile (375/430), plus distinct narrow-width behavior (horizontal table overflow, nav collapse, dialog behavior).
- **Ready-gate:** a UI story cannot be marked *ready* until it cites the applicable accessibility, localization, responsive, and **documentation/reference (`FC-DOC`)** evidence — or records a Product/UX-approved row-specific fallback that documents keyboard/focus/live-region behavior, copy responsibility, doc evidence, replacement path, and owner approval.

## 10. Guardrails — Privacy & Support-Safety

- **Support-safety (hard rule):** no surface, label, log, toast, receipt, or copied value may expose bearer tokens, decoded JWT contents, command payloads, serialized event bodies, raw EventStore metadata, internal correlation ids, stack traces, or real PII. Domain rejections are shown as safe, localized text.
- **Support-safe references** are the only sharable identifiers; receipts use a structured support-safe narrative (NarrativePayload), never the raw event payload.
- **BFF assembly boundary:** the server-side BFF assembles and redacts every receipt, consequence-preview, and rejection view model. Rendered components receive only support-safe localized fields and never receive raw `NarrativePayload`, event bodies, command payloads, tokens, internal correlations, ETags, or raw metadata — and forbidden fields must be provably un-renderable, un-copyable, un-announceable, un-loggable, and un-serializable into component state.
- **Privacy:** the UI shows only data the caller is authorized to see; empty/error states must not reveal the existence of out-of-scope tenants, members, or memberships. For configuration, the server applies the exact display-safe key list and the caller’s prefix policy before passing rows to the UI (FR-6).

## 11. Dependencies & Integration

- **FrontComposer (the platform UI framework this UI composes) — readiness matters; do not treat unconfirmed capabilities as given:**
  - **Confirmed by Story 1.0 (2026-06-05):** the application shell/layout (`FC-LYT`), command-lifecycle feedback (`FC-CMD`), the concurrent-command policy (`FC-CNC` — since aggregate-scoped per AD-12: one active command per (circuit, AggregateIdentity), unrelated aggregates proceed; the historical global one-at-a-time scope is superseded), localization resources (`FC-L10N`), accessibility primitives (`FC-A11Y`), and documentation/reference evidence (`FC-DOC`).
  - **Available with resolved Tenants boundary:** `FC-TBL` table/projection building block; generated projection DataGrid lacks Tenants-required cursor pagination, safety-column pinning, and six non-collapsing list states by itself. Story 1.2 resolves the Tenants implementation path with a Tenants-specific `TenantDataGrid`; reusable cursor/pinning/list-state capability remains FrontComposer-owned enhancement work.
  - **Missing but covered by approved fallbacks:** `<AuditTimeline>` (`FC-AUD`) via flat audit DataGrid; `<ConsequencePreview>` (`FC-CNS`) via inline consequence text.
  - **Missing shared capability:** status/severity/timeline tokens (`FC-TOK`); Tenants uses its canonical vocabulary plus verified Fluent semantic/icon mapping until a shared token contract exists.
  - Status/role badges and a destructive-confirmation dialog exist but need verification against the pinned Fluent version. See §12 and addendum §B for the full readiness table.
- **Backend reads and commands.** The UI reads the tenant list, tenant detail, tenant users, user memberships, audit and global administrators through Tenants REST. Within audit, tenant history uses the tenant-scoped route and governance history uses the fixed `GET /api/global-administrators/audit` route. The REST client evaluates each response’s projection evidence (NFR-1). The EventStore command client handles submission at `POST /api/v1/commands` and status lookup. Tenant-specific UI flows compose existing contracts. Addendum §C lists the routes. [Read transport and host verification](../../../implementation-artifacts/story-1-10-direct-tenants-reads-and-authoritative-freshness-evidence-2026-07-28.md)
- **Boundary policy:** shared UI capability that is missing belongs in **FrontComposer**, not in Tenants (per repo policy). Tenants owns screen composition, column sets, and route binding; FrontComposer owns reusable component/API contracts; Product/UX owns interaction/copy and fallback approval.
- **Hosting & orchestration boundary:** the Tenants UI host remains domain-owned; orchestration, shared hosting, health, telemetry, and production-operations ownership belong to a platform/composing host (PLATFORM-OPS-1, addendum §I) — the repository AppHost is transitional and is not expanded with shared platform plumbing. Production-readiness claims must not exceed recorded evidence; InteractiveServer stays at one replica until shared DataProtection, circuit/session routing, and cursor durability are verified.
- **Detailed mapping** (feature → backend query/command, FrontComposer dependency ids, readiness, fallback decisions, canonical state sets, rejection/NoOp matrix, endpoint specifics) lives in `addendum.md`.

## 12. Risks & Mitigations

- **R-1 Missing FrontComposer components block high-value flows.** `<AuditTimeline>` (`FC-AUD`) and `<ConsequencePreview>` (`FC-CNS`) do not exist; tokens (`FC-TOK`) remain missing as a shared capability. `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` are confirmed by Story 1.0 (2026-06-05). Story 1.2 resolved the Tenants `FC-TBL` implementation path with a Tenants-specific `TenantDataGrid`. **Mitigation:** MVP avoids `FC-AUD`/`FC-CNS` entirely (read-only); later phases use **Product/UX-approved fallbacks** (flat audit list, inline consequence text; approval recorded 2026-06-03 — see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md)), and the rich/reusable components are tracked as FrontComposer dependencies — not built inside Tenants.
- **R-2 False-success risk.** The temptation to show optimistic success would break trust. **Mitigation:** the non-collapse invariant (CP-3) and "live signals are nudges" (CP-4) are mandatory and on the acceptance-scenario list; SM-6 measures it.
- **R-3 Query cursor durability across replicas** is deferred (a separate backend epic); the UI must not assume cursors survive restarts/replica changes yet. **Mitigation:** treat cursors as opaque and session-scoped; on invalidation the UI restarts from page 1 with an honest localized notice (§16.7).
- **R-4 Fallback-approval dependency — RESOLVED (2026-06-03); contract confirmation updated 2026-06-27.** The three interim fallbacks (`FC-AUD`/`FC-CNS`/`FC-CNC`) are Product/UX-approved (see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md)); Story 1.0 confirmed `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`, and Story 1.2 resolved the `FC-TBL` tenant-list boundary — historical evidence, not readiness waivers (2026-07-15). For current read transport see §11. FR-25 defines when audit proof is available, and §14 lists open release and deployment evidence.
- **R-5 Numbering-namespace collision** between UI `ui-NN` keys and backend epic keys. **Mitigation:** keep `ui-` prefix everywhere; never conflate (also noted in addendum).
- **R-6 Identifier handling.** Tenant IDs and user IDs are caller-supplied strings. Preserve the complete value through navigation and copy actions (FR-7). The operations-shell spec records the resolved display and copy rules (§16.12).

## 13. Non-Goals (Explicit)

- **Email / link invitations or pending-member flows** — the backend has no invitation concept; v1 adds users directly by user id. `[ASSUMPTION]` Revisit only with backend support.
- **Dedicated tenant-owner screens, onboarding, or journeys** — v1 owner self-service rides the shared, authorization-scoped surfaces (§3); richer owner UX is a downstream-UX gap, not v1 scope.
- **Editing or deleting events, projections, or state to "fix" data** — never; corrections are compensating commands only.
- **The UI enforcing authorization** — it reflects server-enforced authorization only.
- **High-impact command flows on mobile** — mobile is read-only triage/lookup/audit reference.
- **Building the missing FrontComposer components inside Tenants** — they belong in FrontComposer.
- **Grouped/session audit mode, server-side anomaly scoring, advanced analytics, bulk provisioning** — deferred fast-follows, out of this product's first cut.
- **Sensitive configuration values.** Display is limited to the safe-key policy in FR-6. Any expansion requires a separate product and authorization decision.

## 14. MVP Scope & Phasing

Phase 2a defined the original read-only MVP. The current UI also supports permission-gated commands (§2).

**Release and deployment evidence, 21 November 2026.** Eight audit corrections passed local verification. The four hosted live-route suites passed for Tenants `118293e1` and `9196587b`. Three deployment checks remain open for recovery in the first service that enforces membership, production identity through ingress, and tenant-index capacity under representative load. [Owners and acceptance evidence](addendum.md#j-release-and-deployment-evidence) · [Hosted job results](../../sprint-change-proposal-2026-11-21.md)

> On July 15, the readiness review required fresh evidence for the revised read, search and production contracts. Story 1.10 subsequently verified the direct-read transport. Each UI story still requires its applicable accessibility, localization, responsive and documentation evidence. [July readiness decision](../../sprint-change-proposal-2026-07-15.md) · [Current prerequisite status](addendum.md#i-prerequisite-work-packages-2026-07-15-readiness-correction)

### 14.1 In scope (MVP — "Phase 2a")
- The **Operations Shell** with the single Tenants entry functioning (read): the Tenants and Users workspace tabs, plus the Global Administrators read surface reached contextually (§5.1).
- **FR-1..FR-9, FR-18** — tenant discovery/triage, my-tenants & user lookup, tenant detail & configuration view, member & access review (read), global-administrator review (read).
- **Truth State Badge + Freshness** on all read surfaces; **Unavailable Action Reason** reflected (no mutations yet).
- Full **accessibility, localization, and responsive read evidence** per §9.
- Support-safety per §10 across all read surfaces.

### 14.2 Out of scope for MVP (later phases)
- **Phase 2b — first command flows:** FR-10 (add user), FR-11 (change role), FR-13 (create tenant), FR-14 (edit metadata). *These are the most tractable command flows. Story 1.0 confirmed the command-feedback contract (`FC-CMD`) and the concurrent-command policy (`FC-CNC` — since aggregate-scoped per AD-12); Story 2.1 provides the first Tenants command gateway/lifecycle/projection-confirmation foundation evidence (historical evidence, subject to reverification against the corrected contracts). Future command stories still need to cite equivalent shared command lifecycle/truth/preview foundations and per-story evidence before they are ready.*
- **Phase 2c — high-impact, audit & recovery (gated on approved fallbacks, per-story evidence, and the addendum §I prerequisites):** FR-12 (remove user), FR-15 (disable/enable), FR-16–17 (configuration commands), FR-19 (global-admin commands), FR-20–23 (audit trail & evidence), FR-24–25 (compensating recovery). FR15 is a reversible lifecycle soft-delete / availability-control flow; FR-19 global-admin commands carry platform-governance blast radius and are gated on fixed-scope routing, freshness, last-administrator protection, and evidence gates — not categorically blocked. The current tracker records grant in Story 4.2 and removal in Story 4.3. Story 5.7 covers corrections initiated from audit evidence. [Story number mapping](../../../implementation-artifacts/release-readiness.md#L127)
- **Audit surface in MVP:** audit is a contextual route (§5.1), not a nav area; its list/evidence content is a Phase 2c deliverable. MVP shows the contextual entry as not-yet-available rather than a broken surface. `[ASSUMPTION]` — confirm whether to hide or stub the contextual audit entry in 2a (§16).

### 14.3 Phasing summary
`Phase 2a (MVP)` read-only foundation → `Phase 2b` first command flows → `Phase 2c` high-impact + audit + recovery. A backlog item carries `planning-only` (read, tenant-scoped, or approved reversible lifecycle-control fallback) or `blocked` (hard destructive platform-governance action, or dependent on a missing component with no approved fallback); it promotes to *ready* only when its FrontComposer dependencies resolve **or** a Product/UX-approved fallback is recorded. **The fallback-approval gate is satisfied** for the three interim fallbacks (recorded 2026-06-03 — see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md)); **the shell/layout/command contracts were confirmed** by Story 1.0 (2026-06-05) and **the tenant-list `FC-TBL` boundary resolved** by Story 1.2 — historical evidence, not readiness waivers (2026-07-15). Use addendum §I for the original prerequisites, §11 for the implemented read path, and §14 for outstanding release and deployment evidence. FR-15 covers reversible lifecycle control. Hard deletion is outside this plan.

## 15. Success Metrics

Targets are `[ASSUMPTION]` pending your numbers; methods noted. SMs cross-reference the FRs/CPs they validate.

**Primary**
- **SM-1 (Safe operations adoption):** share of routine tenant/access/lifecycle operations performed through the UI rather than raw command-API calls. *Target: majority within one quarter of the relevant command phase shipping.* Validates FR-10..FR-17, FR-19. *MVP leading indicator:* operators use the UI as the default place to answer access questions (SM-2).
- **SM-2 (Time-to-answer "who has access / what changed"):** median time for an operator to answer an access/audit question via the UI. *Target: under ~1 minute for access; under ~3 minutes for an audited change.* Validates FR-1..FR-9, FR-18, FR-20..FR-23.
- **SM-3 (Self-service uptake):** share of tenant-owner-initiated member/role changes done by owners themselves (vs. operator tickets). *Target: rising quarter over quarter once Phase 2b ships.* Validates FR-3, FR-11.
- **SM-6 (Trust — no false success).** Target zero cases in which the UI shows or announces success before an authoritative projection confirms the same command’s outcome. An unconfirmed high-impact action on stale or unknown data must stay blocked. Validates CP-3, CP-4 and R-2.
  - **Proposed measurement** `[ASSUMPTION]`. For each release candidate, the release owner reports the number of submitted UI command journeys in its qualification runs and the fraction with enough evidence to assess the result. For each assessed journey, compare the first success shown or announced with the matching projection evidence available at that moment. Count premature successes. Record attempts with missing observations as unassessed and report their count. A run with no assessed journeys has no result. Publish aggregate counts using support-safe evidence. Product and Release should confirm the owner and reporting window before the first report.

**Secondary**
- **SM-4 (Onboarding time):** time to stand up a new, owned, configured tenant via the UI. Validates FR-13, FR-10, FR-16.
- **SM-5 (Recovery without data-store edits):** number of corrections performed via compensating commands through the UI; target trend up while raw/manual fixes trend to zero. Validates FR-24, FR-25, NFR-5.

**Counter-metrics (do not optimize)**
- **SM-C1 (Don't streamline away safety):** destructive/high-impact actions completed **without** viewing a Consequence Preview should stay at ~0. We do **not** want to "reduce friction" by removing the preview. Counterbalances SM-1/SM-4.
- **SM-C2 (Don't hide errors to look successful):** rejected/failed commands must remain clearly surfaced; a *drop* in surfaced errors is a red flag (likely suppression), not a win. Counterbalances SM-1.
- **SM-C3 (Don't trade trust for speed):** any rise in users acting on `stale`/`unknown` data (override usage) is a regression even if it speeds tasks. Counterbalances SM-2/SM-6.

## 16. Open Questions

1. **Command endpoint route — RESOLVED (2026-07-15).** The command endpoint is `POST /api/v1/commands`; the 2026-07-15 correction preserves it explicitly and adopts no unversioned alias.
2. **FrontComposer component gaps — fallback approval RESOLVED (2026-06-03); shell/command contract confirmation RESOLVED (2026-06-05).** Product/UX approved the flat-audit-list, inline-consequence-preview, and one-at-a-time fallbacks (see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md); the one-at-a-time scope is since superseded — aggregate-scoped per AD-12, see §4 / addendum §B); Story 1.0 confirmed `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` (see `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`). Building `<AuditTimeline>`/`<ConsequencePreview>`/shared `FC-TOK` capabilities in FrontComposer remains a post-fallback enhancement, not a blocker.
3. **Tenant-list grid decision (`FC-TBL`) — RESOLVED by Story 1.2.** Current generated grid support is available but does not by itself satisfy cursor pagination, safety-column pinning, or six non-collapsing list states. Tenants uses a Tenants-specific `TenantDataGrid`; reusable grid capability remains a FrontComposer enhancement.
4. **Localization resource ownership — RESOLVED by architecture D4.** Tenants owns whole-string domain `.resx` keys and inherits only shell-chrome strings from `FcShellResources`; adopter terminology remains Tenants-owned domain copy.
5. **WCAG 2.2 AA evidence.** The central Builds package configuration selects Fluent UI Blazor 5.1.0. Record the version resolved by the qualifying build and verify the controls, keyboard behavior, announcements and contrast used by Tenants (§9). WCAG 2.2 AA remains conditional on that evidence.
6. **RTL support** — in or out for v1? (none of the specs commit.)
7. **Cursor durability across replicas/restarts — UI behavior RESOLVED (2026-07-15); durability itself remains deferred.** UI behavior on cursor invalidation is defined: restart from page 1 with an honest localized notice (SEARCH-CURSOR-1 / AD-10). Durability verification itself belongs to PLATFORM-OPS-1 before any multi-replica claim; the deferred backend epic stands.
8. **Consequence Preview scope for config edits (FR-16/FR-17) - RESOLVED 2026-06-29.** All eligible set/remove configuration mutations require Consequence Preview in v1. No high-risk-subset bypass is defined. Any future narrowing requires a Product/UX/Architecture decision that defines the key classification rule, user-facing reason copy, test coverage for low-risk and high-risk keys, and the phasing impact.
9. **Audit contextual entry in MVP (Phase 2a)** — hide it, or show a "not yet available" placeholder? (The IA itself is settled: audit is a contextual route, not a nav area — §5.1.)
10. **Freshness thresholds** — the numeric `current`/`aging`/`stale` cutoffs (deferred to implementation) need product input.
11. **Future display of sensitive configuration.** FR-6 establishes the current policy. A future expansion would need an explicit decision on which keys and audiences it covers and which safeguards apply.
12. **Tenant and user identifier semantics — resolved.** Both are literal caller-supplied strings. The operations-shell spec requires visual truncation to preserve the full value for accessibility and copying (FR-7, R-6). [Operations-shell identifier rules](../../../../docs/tenants-ui-operations-shell-spec.md#L186)
13. **Owner self-service depth** — v1 is honest-minimal (shared surfaces, no owner-only screens). Confirm when/whether dedicated owner journeys/UX become a funded scope. (§3, §13)
14. **Audit performance contract — approved September 23, 2026.** The initial audit-ready render budget is p75 ≤ 2.5 seconds and p95 ≤ 4.0 seconds on the dedicated runner. The September 24 run passed all 126 percentile groups and ten functional gates. Use addendum §F for the workload, filter and feedback budgets, fallback and candidate rerun criteria. The November production index-capacity check remains open (§14). [Qualification and fallback](addendum.md#f-options-considered--deferred-for-downstream-ux--architecture) · [Approved contract](../../../implementation-artifacts/story-5-1-performance-decision.md)

## 17. Assumptions Index

- §3 — Owner self-service in v1 = shared, authorization-scoped surfaces only; no dedicated owner screens/journeys (honest-minimal).
- §3.2 — Programmatic integrators keep using APIs directly; this UI is human-only.
- §3.3 / §7.4 / §13 — Adding a user is a **direct add by user id**; no email-invitation flow in v1 (backend has no invitation concept).
- §5.2 — Fluent UI semantic theme roles (no separate branded palette) are the visual authority.
- §5.3 — Mobile is read-only; no high-impact commands on mobile.
- §8 (NFR-1) — Read-surface performance budgets (~1s warm) are placeholders pending confirmation.
- §9 — WCAG 2.2 AA is a conditional target dependent on Fluent stack support; RTL remains undecided; localization resource ownership is resolved by architecture D4 (Tenants-owned domain strings, FrontComposer-owned shell chrome).
- §14.2 — Audit contextual entry present-but-not-yet-available in MVP (hide vs. stub to be confirmed).
- §15 — Product metric targets are proposed. The proposed SM-6 qualification cohort, reporting window and owner await Product and Release confirmation.
- General — MVP = read-only foundation (`ui-01..06` equivalent) per your scope decision; full Phase 2 vision documented with command/audit/recovery in later phases.
