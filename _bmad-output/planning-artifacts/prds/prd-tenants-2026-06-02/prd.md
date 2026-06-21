---
title: Tenants Management UI
status: final
created: 2026-06-02
updated: 2026-06-05
---

# PRD: Tenants Management UI
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

> **Post-readiness update (2026-06-05):** Story 1.0 completed the FrontComposer shell-integration spike and confirmed `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`; see `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`. Remaining planning gates are the `FC-TBL` tenant-list grid decision, per-story accessibility/localization/responsive/documentation evidence, deferred numerics, and Epic 5 audit/proof evidence readiness.

## 1. Vision

The Hexalith platform's tenant model is rich and safety-critical: tenants, their members and roles, namespaced configuration, lifecycle state, and an immutable event-sourced history with compensating-command corrections. Today all of this is reachable only by hand-crafting command-API calls and reading projections directly — which is slow, error-prone, and impossible to delegate to the people who should own routine tenant changes.

The Tenants Management UI turns that capability into a **trustworthy operations and self-service experience**. A single role-scoped application serves two audiences from the same surfaces: **platform operators / global administrators**, who triage and act across every tenant, and **tenant owners**, who manage only their own tenant. It lets these users *see the truth* about who has access and what changed, *act safely* through previews and guardrails instead of raw commands, and *recover from mistakes* through forward compensating actions — all without ever editing history or the data store.

What makes this product distinctive is its honesty about state. In an eventually-consistent, event-sourced system the UI refuses to fake certainty: it tells the user how fresh the data is, blocks high-impact actions when it cannot prove the data is current, never reports success it has not confirmed against the source of truth, and treats a correction as a new auditable command rather than a silent "undo." The result is a console operators can trust under incident pressure and owners can use without fear.

## 2. Why Now

- The **Phase 1 backend is built and tested** — tenant/member/role/configuration/lifecycle commands, queries, cursor pagination, authorization, projection safety, and production JWT configuration all exist. A few hardening items remain deferred (notably cursor durability across replicas — §16.7); treat the backend as ready-but-not-frozen, not flawless.
- Operators currently perform tenant changes by **calling the command API directly**, which does not scale, cannot be safely delegated to tenant owners, and produces no guided audit/recovery path.
- **FrontComposer** provides the shell/layout/command groundwork to begin composing this UI. Story 1.0 confirmed the main shell contracts on 2026-06-05; the remaining near-term readiness decision is how Tenants uses or extends `FC-TBL` for cursor pagination, safety-column pinning, and six non-collapsing list states — see the build-readiness status in §14.

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
  - **Entry state:** opens the audit trail (from nav, a tenant row, or a command result).
  - **Path:** filters the audit list → reads an **Audit Evidence Receipt** (who acted, on whom, in which tenant, outcome, when, with a support-safe reference) → identifies the wrong change → starts a **compensating command** ("restore intended access") → previews the correction against current state → submits a *new* command; both the original and corrective records are linked.
  - **Climax:** the effect is corrected forward, with the mistake and the fix both permanently on the record.
  - **Resolution:** Sofia cites the support-safe reference to the stakeholder. **Edge case:** if evidence is delayed or unavailable, the UI shows that honestly (`audit pending` / `audit delayed` / `audit unavailable`) and offers retry/wait/escalate — it never fabricates proof.

- **UJ-5. Nadia self-serves her own tenant.** *(Phase 2a / MVP — read; role change Phase 2b)*
  - **Persona + context:** Nadia owns a customer tenant and wants to manage her team without a ticket.
  - **Entry state:** authenticated as a tenant owner; she sees only her own tenant (authorization-scoped).
  - **Path:** opens her tenant's member table → reviews members and roles → (later phase) changes a teammate's role and watches it confirm.
  - **Climax:** she manages her own access picture independently.
  - **Resolution:** platform operators are no longer a bottleneck for her routine changes. **Edge case:** she never sees other tenants or the Global Administrators surface.

- **UJ-6. Elena onboards a new tenant.** *(Phase 2b/2c)*
  - **Persona + context:** a new customer needs a tenant stood up.
  - **Entry state:** authenticated operator.
  - **Path:** creates the tenant → adds the first owner directly by user id → sets initial configuration → confirms each step landed.
  - **Climax:** a usable, owned, configured tenant exists.
  - **Resolution:** the owner (Nadia) can now self-serve (UJ-5). **Edge case:** adding a user is a *direct* add by user id — there is no email-invitation step in v1 (§13). `[ASSUMPTION]`

## 4. Glossary

Downstream workflows and readers must use these terms exactly; FRs, UJs, and SMs use them verbatim. The full canonical state-set enumerations are in addendum §G.

- **Tenant** — an isolated organizational scope on the platform, identified by a **meaningful caller-supplied string id (not a ULID)**. Has members, namespaced configuration, and a lifecycle status.
- **Member** — a user who holds a **Role** in a **Tenant**. Identified by a **caller-supplied user id (not a ULID)**.
- **Role** — a member's permission level within a tenant (`TenantRole`); roles include an owner-level role. Has a `Unknown` sentinel that is never a valid target.
- **Owner / Owner count** — members holding the owner-level role; **owner count** is how many a tenant has. Reducing it to zero is the **last-owner** case (allowed by the backend — see CP-6).
- **Global administrator** — a platform-level governance principal in the separate `global-administrators` scope (a single fixed-identity aggregate, not tenant-routed); distinct from tenant membership. The **last global administrator** is protected by a backend invariant (its removal is rejected — see CP-6).
- **Tenant lifecycle status** — a tenant's state, including **disabled** (an eventually-consistent availability signal; commands targeting a disabled tenant are rejected) and enabled.
- **Configuration** — namespaced tenant key/values, partitioned by a consumer-owned dot-prefix; readers filter by their prefix and ignore others; the UI groups displayed config by namespace.
- **Operations Shell** — the application's information architecture: four primary navigation areas — **Tenants, Users, Global Administrators, Audit** — within a FrontComposer shell. Tenants is the default landing/triage surface; Users is a secondary area (realized by FR-3 and FR-4).
- **Pending state** — a tenant/row indicator that a command affecting that tenant or member is in flight (not yet confirmed).
- **Projection** — the read model the UI reads from; the **source of truth** for confirmation is the projection, not optimistic UI state or live notifications.
- **Truth State Badge** — the visible indicator of how trustworthy displayed data is, composing freshness, authorization, command lifecycle, projection confirmation, and audit dimensions. Its full canonical 13-state set is defined once (addendum §G) and used verbatim everywhere — no per-screen reinterpretation.
- **Freshness** — how current the displayed data is: one of `current`, `refreshing`, `aging`, `stale`, `unknown`. `aging` is **usable with friction**; `stale` and `unknown` block high-impact action. Numeric thresholds are set per implementation, not in this PRD.
- **Freshness Gate** — the rule that blocks access-impacting/destructive actions when freshness is `stale`/`unknown` (fail-closed) unless an approved override exists.
- **Unavailable Action Reason** — the plain-language, hover-free reason an action is disabled. The canonical six categories are `missing permission`, `stale data`, `missing lifecycle support`, `missing consequence preview`, `missing audit proof`, `high-impact flow not ready` (addendum §G).
- **Consequence Preview** — the pre-submission summary of what a command will and will not do, including **known consequences** and **known unknowns**, shown before destructive/high-impact actions. Its content set is defined in addendum §H.
- **Command Lifecycle Panel** — the surface that tracks a dispatched command's state without overwriting confirmed projection data.
- **Non-collapse invariant** — the rule that `accepted` ≠ `confirmed` (projected) ≠ `audit available` (proven); these — and `degraded` and `unable to verify` — are never merged or shown optimistically as success.
- **Fail-closed** — when freshness is unknown, authorization indeterminate, the consequence preview incomplete, or lifecycle support missing, the action is blocked rather than allowed.
- **Compensating command** — a forward command that corrects the *effect* of a prior action (e.g. re-adding a user). Corrections append new events; history is never edited or deleted. The UI never labels this "undo".
- **Audit Evidence Receipt** — the support-safe record of a recorded action: actor, target, tenant scope, outcome, timestamp, projection marker, and an audit/command reference. Assembled client-side from a structured **NarrativePayload** (never the raw event payload).
- **AuditEventCategory** — the category an audit entry belongs to: `Access` or `Administrative`.
- **Audit availability** — one of `audit pending`, `audit delayed`, `audit unavailable`, `missing implementation support` (addendum §G); none is ever shown as success.
- **Support-safe reference** — an identifier safe to share that never exposes payloads, bearer tokens, stack traces, internal correlation ids, raw event metadata, or PII.
- **Orphan membership** — a membership whose context (e.g. a disabled tenant) makes it require operator attention.
- **NoOp** — a same-state request that produces no event — specifically `ChangeUserRole` to the current role and `SetTenantConfiguration` with an identical key+value; the UI reflects `already applied`. (Note: re-adding an existing member is *not* a NoOp — it is rejected; see addendum §D.)
- **Rejection** — a business-rule refusal returned as a domain rejection event (surfaced to the user as safe, localized text), never a stack trace.
- **Proposed fallback** — a simpler implementation proposed for when a richer FrontComposer component is unavailable (e.g. a flat audit list instead of a timeline). A fallback is **only usable once Product/UX has approved it**. **The three interim fallbacks — `FC-AUD` → flat audit grid, `FC-CNS` → inline consequence text, `FC-CNC` → one-at-a-time commands — are approved** (recorded 2026-06-03; see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md)); each remains build-ready only once its other FrontComposer gates clear.

## 5. Information Architecture & Visual Language

### 5.1 Operations Shell
Three primary navigation areas, in order: **Tenants** (default landing / triage surface), **Global Administrators**, **Audit**. **Users is contextual** — reached from a member row and global search (realized by FR-3 "My Tenants" and FR-4 user lookup), not a co-equal tab (reconciled with architecture + epics, 2026-06-03; resolves the prior PRD↔UX "Users-nav" divergence to *contextual*). Command lifecycle is **never** a primary navigation area — command status and feedback are shown inline, anchored to the affected row/panel. Audit is reachable both as a top-level area and contextually from tenant rows, tenant detail, user lookup, and command results. Navigating between surfaces preserves context (selection, filters) so users don't lose their place.

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
- **CP-7 Correct forward, never "undo".** Recovery is always a **compensating command** with its own preview and proof; the original record stays in the immutable trail. The words "undo", "rollback", and "hidden edit" are never used.
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
- **Search consequences (testable; cc-2026-06-21):** search matches a tenant by its **Name or TenantId** across the **entire** tenant set (not just the loaded page), computed by `Hexalith.Memories` **syntactic/BM25** search against a dedicated `tenants-index`; a non-empty term triggers a **server round-trip** (no in-memory page re-filter), an empty/whitespace term returns the unchanged cursor list; matched rows are **hydrated fresh** via the existing ETag/freshness read path (the search index never supplies row data); the **status filter is exact** (structured attribute once the Memories REST filter ships; the hydrated authoritative status in the interim), never fuzzy text; search is **eventually consistent** (a new/renamed tenant becomes findable once indexed; rows always render correct data regardless of index lag); and search **never blocks the list** — if Memories is unavailable the list falls back to the cursor view with a non-blocking notice (no tokens/correlation-ids/ETags in any user-facing copy).

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
A user can view a tenant's configuration key/values, grouped by namespace and filtered to the namespaces they own/are authorized for.
- **Consequences:** values outside the caller's prefix are not shown; sensitive-value display is out of scope for read MVP. `[ASSUMPTION]`

#### FR-7: Copy support-safe identifiers
A user can copy a full identifier (which may be visually truncated on screen) and any support-safe reference.
- **Consequences:** copied content is the full id (a caller-supplied string — not assumed to be a ULID); no payloads, tokens, correlation ids, or PII are ever exposed.

### 7.3 Member & Access Review *(ui-04 — Phase 2a / MVP)*
**Description:** Review who has access to a tenant and what is actionable. Realizes UJ-2, UJ-5.

#### FR-8: Review the member table
A user can review a tenant's members with role, owner count, status, freshness, and orphan context, read-only.
- **Consequences:** the table is **read-only and must not imply mutation**; it exposes accessible semantics (headers, sort state, row relationships); freshness is per the Truth State Badge; orphan/disabled context is flagged.

#### FR-9: See action availability and reasons
A user can see, per member, which actions would be available and — when one is not — a plain-language **Unavailable Action Reason**. *(Reflective in MVP; actions arrive in later phases.)* Realizes UJ-2.
- **Consequences:** reasons use the six canonical categories (addendum §G); reasons are inline-visible (not hover-only; tooltips may supplement but are never the only explanation).

### 7.4 Member & Role Management *(ui-09, ui-14 — Phase 2b/2c)*
**Description:** The command flows that change tenant membership. All inherit §6. Custom command flows — not generated CRUD. Realizes UJ-3, UJ-5, UJ-6.

#### FR-10: Add a user to a tenant *(Phase 2b)*
An authorized user can add a user to a tenant with an explicit role, by user id. Realizes UJ-6, UJ-5.
- **Consequences:** add is a **direct** add by caller-supplied user id (no invitation/pending step — §13); **adding a user who is already a member is *rejected* (`UserAlreadyInTenant`), surfaced as safe localized text — it is not a NoOp**; a corrective add states the explicit intended role. *(ui-09 bundles add + change-role behind a shared availability gate.)*

#### FR-11: Change a member's role *(Phase 2b)*
An authorized user can change a member's role. Realizes UJ-5.
- **Consequences:** changing to the current role is a **NoOp** shown as `already applied`; role escalation and `Unknown` targets are rejected with safe localized text; the change shows success only after projection confirmation (CP-3).

#### FR-12: Remove a user from a tenant *(Phase 2c)*
An authorized user can remove a user's tenant access, with Consequence Preview, fail-closed gating, elevated-friction handling, and proof via audit. Realizes UJ-3.
- **Consequences:** required inputs (target, tenant, current role, freshness, authorization) are validated before the preview, and incomplete preview inputs block submission (CP-5); the Consequence Preview states owner-count impact, the access revoked, the recovery path, the audit expectation, and known-unknowns (addendum §H); reducing owner count to zero triggers elevated friction but is **not blocked**, and a target who also holds global-administrator authority raises platform-level friction (CP-6); the control is **not a primary/casual button**; an already-applied removal reads as `already applied` and duplicate submits are de-duplicated; the Command Lifecycle Panel tracks `submitted → accepted → projection_pending → confirmed → audit_pending → audit_available` without collapsing states, and an unconfirmable outcome shows `unable to verify` (never success) (CP-3); every failure mode maps to a stated recovery (CP-8).
- **Notes:** uses the Product/UX-approved `FC-CNS` inline consequence fallback and the Story 1.0-confirmed `FC-CMD`/`FC-CNC` command contracts. Story 2.4 delivers command lifecycle, projection confirmation, and honest audit handoff; Audit Evidence Receipt/proof UX remains Epic 5 unless the evidence source is already implemented. `[NOTE FOR PM]`

### 7.5 Tenant Lifecycle Management *(ui-07, ui-08, ui-13 — Phase 2b/2c)*
**Description:** Create, edit, and enable/disable tenants. Realizes UJ-6.

#### FR-13: Create a tenant *(Phase 2b)*
An authorized operator can create a new tenant. Realizes UJ-6.
- **Consequences:** creating an existing tenant id is rejected with safe text (`TenantAlreadyExists`); success shown only after projection confirmation.

#### FR-14: Edit tenant metadata *(Phase 2b)*
An authorized user (tenant **contributor or global administrator**) can edit a tenant's metadata.
- **Consequences:** **every successful edit emits an update — there is no same-state suppression** (the backend always records `TenantUpdated`); validation errors surface as safe localized field messages.

#### FR-15: Disable or enable a tenant *(Phase 2c)*
An authorized operator (**global administrator only**) can disable or enable a tenant (high-impact, platform-wide), with Consequence Preview.
- **Consequences:** setting a tenant to a state it is already in is **rejected** (`TenantLifecycleStateAlreadySet`); the Consequence Preview notes that disabled status is an **eventually-consistent availability signal** and that **commands targeting a disabled tenant are rejected** (`TenantDisabled`); success shown only after projection confirmation; the lifecycle status updates with no-color-only encoding.
- **Scope clarification (2026-06-06):** for this phase, disable/enable is a reversible lifecycle soft-delete / availability-control operation, not hard destructive tenant deletion. Hard destructive tenant deletion is out of scope and belongs to future independent administrators-only CLI tooling.

### 7.6 Tenant Configuration Management *(ui-10 — Phase 2c, high-impact)*
**Description:** Set and remove namespaced configuration. Inherits §6.

#### FR-16: Set a configuration value *(Phase 2c)*
An authorized user can set a namespaced configuration key/value, with Consequence Preview for high-impact keys.
- **Consequences:** identical key+value is a **NoOp** (`already applied`); values exceeding domain limits are rejected with safe text (`ConfigurationLimitExceeded`); `[ASSUMPTION]` whether every config edit needs a preview or only a high-risk subset is an open question that is also a phasing lever (§16).

#### FR-17: Remove a configuration key *(Phase 2c)*
An authorized user can remove a configuration key.
- **Consequences:** removing a missing key surfaces a safe `ConfigurationKeyNotFound` rejection; removal shows success only after projection confirmation.

### 7.7 Global Administrator Governance *(ui-06 read — Phase 2a / MVP; ui-15 commands — Phase 2c)*
**Description:** Review and manage platform-level global administrators, kept distinct from tenant membership.

#### FR-18: Review global administrators *(Phase 2a / MVP, read)*
An authorized operator can review who holds global-administrator access, separate from any tenant membership. Realizes UJ-2.
- **Consequences:** the surface is visible only to authorized operators; tenant owners (e.g. Nadia) never see it; data comes from the single fixed-identity `global-administrators` aggregate (not tenant-routed); rows show identity and a freshness badge.

#### FR-19: Grant or remove a global administrator *(Phase 2c, high-impact, platform-wide)*
An authorized operator can grant a global administrator, or remove one **except the last**.
- **Consequences:** **removing the last global administrator is rejected by the domain (`LastGlobalAdministrator`)** — the UI reflects this as an *unavailable* action with a safe reason, not as completable friction (CP-6, asymmetric with last-owner); operations are in the `global-administrators` scope, never conflated with tenant membership.

### 7.8 Audit Trail & Evidence *(ui-11, ui-12 — Phase 2c; flat-list fallback approved and implemented in Epic 5)*
**Description:** The evidentiary surface — see what changed, with support-safe receipts. Realizes UJ-4.

#### FR-20: Browse a tenant's audit trail
A user can browse a tenant's audit entries as a flat, stably ordered list with date and `AuditEventCategory` (`Access` / `Administrative`) filters. Realizes UJ-4.
- **Consequences:** list paginates via cursor; targets ~500 events without unacceptable degradation; loading/empty/filtered-empty/error states are distinct and accessible; the flat list uses the Product/UX-approved `FC-AUD` DataGrid fallback implemented by Epic 5. The reusable FrontComposer timeline remains a deferred replacement path, not a prerequisite for the delivered Tenants slice.

#### FR-21: Reach audit from context
A user can reach audit evidence from navigation, a tenant row, tenant detail, a user lookup, and a command result.
- **Consequences:** each entry point lands scoped to the relevant tenant/user/command.

#### FR-22: View an Audit Evidence Receipt
A user can view a support-safe receipt for a recorded action (actor, target, tenant scope, outcome, timestamp, projection marker, audit/command reference).
- **Consequences:** the receipt is assembled client-side from a structured **NarrativePayload** (no new backend receipt endpoint) and never exposes raw payloads, tokens, correlation ids, raw event metadata, or PII; partial completion shows the actual lifecycle state (e.g. `audit pending`), never pre-rendered proof (CP-3).

#### FR-23: Distinguish audit availability states
A user can tell apart `audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support`, each with a stated recovery.
- **Consequences:** none of these states is shown as success; each offers retry/wait/escalate (CP-8); `missing implementation support` reflects the `FC-AUD` dependency, not a data error.

### 7.9 Compensating Recovery *(Phase 2c)*
**Description:** Correct the effect of a prior action forward, from audit evidence. Inherits §6. Realizes UJ-4.

*Post-Epic 5 / Epic 4 note: compensating recovery (FR-24, FR-25) and evidence-receipt assembly (FR-22) now have completed Epic 5 implementation evidence (`epics.md` Stories 5.3, 5.5, and 5.6 plus the matching story records). Tenant-domain correction preview, submission, projection confirmation, and proof linking are implemented through existing BFF gateways. Epic 4 Stories 4.3 and 4.4 now provide global-administrator grant/remove command support; any enabled global-administrator correction path still needs story-specific verification that it selects the fixed `global-administrators` command scope and preserves last-admin safety.* `[NOTE FOR PM]`

#### FR-24: Start a compensating command
From audit evidence, an authorized user can start a correction ("restore intended access" / "start correction").
- **Consequences:** the correction is a new forward command (e.g. `AddUserToTenant` / a role change) with its own Consequence Preview and proof; the UI never calls it "undo"; the original event is untouched; because re-adding an existing member is rejected (addendum §D), the correction previews against current state (FR-25); restoring access to a tenant with no remaining membership history relies on the empty-tenant bootstrap path (addendum §D).

#### FR-25: Preview and link the correction
A user can preview the correction against current state and have the original and corrective records linked.
- **Consequences:** the preview reflects current state (the original effect may already differ); both audit records reference each other; success shown only after projection confirmation.

**Feature-specific NFR (7.8/7.9):** audit rendering must meet the ~500-event target; if a flat render cannot, virtualization or a stricter page size is required before this feature is "ready".

## 8. Cross-Cutting Non-Functional Requirements

- **NFR-1 Performance & freshness.** Reads use cursor pagination and conditional requests so unchanged data is cheap; freshness is surfaced, not hidden. Targets: a tenant list/detail/member surface renders interactive in **≤ ~1s on a warm projection** for a typical tenant; the audit view targets ~500 events without unacceptable latency. (Exact budgets confirmed at implementation.) `[ASSUMPTION]`
- **NFR-2 Security & authorization.** Authorization is **server-enforced** at the API layer and in the domain; the UI **reflects** authorization state and never enforces it. The UI must remain safe if it misjudges authorization — the server is the gate. Role-scoping (tenant owner sees only their tenant; global administrator sees all) is enforced in the projection/query layer.
- **NFR-3 Reliability & consistency.** The system is eventually consistent; the UI treats the projection as source of truth, re-queries to confirm, and is correct under at-least-once delivery and projection lag (CP-3, CP-4).
- **NFR-4 Observability & testability.** Every interactive element and status carries a **stable automation selector / component contract** (never keyed on row text or color), so acceptance and E2E tests are robust.
- **NFR-5 No data-store edits.** The UI never edits, deletes, or rewrites events, projections, or state to "fix" data; corrections are compensating commands only (CP-7).

## 9. Accessibility & Localization

- **A11y standard:** baseline **WCAG 2.1 AA**; target **WCAG 2.2 AA where the selected Fluent UI Blazor / FrontComposer stack supports it** (conditional — no unconditional 2.2 promise). `[ASSUMPTION]`
- **Keyboard & focus:** all interactive elements reachable; logical focus order; visible focus in normal/high-contrast/forced-colors; modal focus trap with a safe escape that does **not** commit a destructive action; focus returns to the launching row/control after close/cancel/submit/failure; **keyboard users can complete or exit every modal/preview/table/command workflow.**
- **Screen reader & status:** accessible names for all statuses, badges, freshness indicators, and actions; absolute timestamps (not relative-only); table semantics (headers, sort state, row relationships); live regions with appropriate politeness — `assertive` reserved for rejection/failure/destructive-blockers/unable-to-verify; **never announce success before projection truth**.
- **No-color-only & motion:** color never the sole signal; reduced-motion users are never dependent on animation.
- **Localization:** all state labels, role names, timestamps, warnings, disabled reasons, recovery actions, confirmation copy, and empty/loading/error/degraded/stale/unavailable copy are localizable, with culture-aware formatting. **No runtime sentence-fragment assembly** — whole resource strings with named placeholders. Resource ownership (shared shell resources vs. Tenants-owned keys) is an open question (§16). **RTL support is undecided** (§16). `[ASSUMPTION]`
- **Acceptance evidence (definition of done for UI work):** keyboard-only navigation; screen-reader review (NVDA + at least one browser/SR pairing); automated accessibility checks; forced-colors/high-contrast; reduced-motion; contrast; live-region announcements; focus return; hover-free disabled explanations. Required acceptance scenarios: **stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, permission-missing.** Responsive evidence at desktop (1024/1366/1440 + wide), tablet (768/1024), mobile (375/430), plus distinct narrow-width behavior (horizontal table overflow, nav collapse, dialog behavior).
- **Ready-gate:** a UI story cannot be marked *ready* until it cites the applicable accessibility, localization, responsive, and **documentation/reference (`FC-DOC`)** evidence — or records a Product/UX-approved row-specific fallback that documents keyboard/focus/live-region behavior, copy responsibility, doc evidence, replacement path, and owner approval.

## 10. Guardrails — Privacy & Support-Safety

- **Support-safety (hard rule):** no surface, label, log, toast, receipt, or copied value may expose bearer tokens, decoded JWT contents, command payloads, serialized event bodies, raw EventStore metadata, internal correlation ids, stack traces, or real PII. Domain rejections are shown as safe, localized text.
- **Support-safe references** are the only sharable identifiers; receipts use a structured support-safe narrative (NarrativePayload), never the raw event payload.
- **Privacy:** the UI shows only data the caller is authorized to see; empty/error states must not reveal the existence of out-of-scope tenants, members, or memberships.

## 11. Dependencies & Integration

- **FrontComposer (the platform UI framework this UI composes) — readiness matters; do not treat unconfirmed capabilities as given:**
  - **Confirmed by Story 1.0 (2026-06-05):** the application shell/layout (`FC-LYT`), command-lifecycle feedback (`FC-CMD`), one-at-a-time command policy (`FC-CNC`), localization resources (`FC-L10N`), accessibility primitives (`FC-A11Y`), and documentation/reference evidence (`FC-DOC`).
  - **Available with caveats:** `FC-TBL` table/projection building block; generated projection DataGrid lacks Tenants-required cursor pagination, safety-column pinning, and six non-collapsing list states. Resolve the Story 1.2 grid decision before tenant-list implementation.
  - **Missing but covered by approved fallbacks:** `<AuditTimeline>` (`FC-AUD`) via flat audit DataGrid; `<ConsequencePreview>` (`FC-CNS`) via inline consequence text.
  - **Missing shared capability:** status/severity/timeline tokens (`FC-TOK`); Tenants uses its canonical vocabulary plus verified Fluent semantic/icon mapping until a shared token contract exists.
  - Status/role badges and a destructive-confirmation dialog exist but need verification against the pinned Fluent version. See §12 and addendum §B for the full readiness table.
- **Backend (consumes — already built):** tenant/user/audit read queries and tenant lifecycle / member-role / configuration / global-administrator commands. The UI **composes** these with **custom command flows (not generated CRUD)**; it does not add backend endpoints, reshape immutable domain contracts, or annotate command/query contracts for UI generation.
- **Boundary policy:** shared UI capability that is missing belongs in **FrontComposer**, not in Tenants (per repo policy). Tenants owns screen composition, column sets, and route binding; FrontComposer owns reusable component/API contracts; Product/UX owns interaction/copy and fallback approval.
- **Detailed mapping** (feature → backend query/command, FrontComposer dependency ids, readiness, fallback decisions, canonical state sets, rejection/NoOp matrix, endpoint specifics) lives in `addendum.md`.

## 12. Risks & Mitigations

- **R-1 Missing FrontComposer components block high-value flows.** `<AuditTimeline>` (`FC-AUD`) and `<ConsequencePreview>` (`FC-CNS`) do not exist; tokens (`FC-TOK`) remain missing as a shared capability. `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` are confirmed by Story 1.0 (2026-06-05). **Mitigation:** MVP avoids `FC-AUD`/`FC-CNS` entirely (read-only); later phases use **Product/UX-approved fallbacks** (flat audit list, inline consequence text; approval recorded 2026-06-03 — see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md)), and the rich components are tracked as a FrontComposer dependency — not built inside Tenants. Tenant-list build-start still requires the `FC-TBL` decision recorded in Story 1.2.
- **R-2 False-success risk.** The temptation to show optimistic success would break trust. **Mitigation:** the non-collapse invariant (CP-3) and "live signals are nudges" (CP-4) are mandatory and on the acceptance-scenario list; SM-6 measures it.
- **R-3 Query cursor durability across replicas** is deferred (a separate backend epic); the UI must not assume cursors survive restarts/replica changes yet. **Mitigation:** treat cursors as opaque and session-scoped; flag in §16.
- **R-4 Fallback-approval dependency — RESOLVED (2026-06-03); contract confirmation updated 2026-06-05.** The three interim fallbacks (`FC-AUD`/`FC-CNS`/`FC-CNC`) are Product/UX-approved (see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md)); Story 1.0 confirms `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`. Remaining gates are story-specific evidence, `FC-TBL` decisioning, `FC-TOK` fallback discipline, and audit/proof evidence readiness — not fallback approval.
- **R-5 Numbering-namespace collision** between UI `ui-NN` keys and backend epic keys. **Mitigation:** keep `ui-` prefix everywhere; never conflate (also noted in addendum).
- **R-6 Source-spec ID-scheme error.** Several UI specs state tenant/user ids are ULIDs; the authoritative domain rule says they are caller-supplied strings (only envelope `MessageId` is a ULID). **Mitigation:** this PRD follows the domain rule; the specs need correcting (§16) so implementation never parses a `TenantId`/`UserId` as a ULID.

## 13. Non-Goals (Explicit)

- **Email / link invitations or pending-member flows** — the backend has no invitation concept; v1 adds users directly by user id. `[ASSUMPTION]` Revisit only with backend support.
- **Dedicated tenant-owner screens, onboarding, or journeys** — v1 owner self-service rides the shared, authorization-scoped surfaces (§3); richer owner UX is a downstream-UX gap, not v1 scope.
- **Editing or deleting events, projections, or state to "fix" data** — never; corrections are compensating commands only.
- **The UI enforcing authorization** — it reflects server-enforced authorization only.
- **High-impact command flows on mobile** — mobile is read-only triage/lookup/audit reference.
- **Building the missing FrontComposer components inside Tenants** — they belong in FrontComposer.
- **Grouped/session audit mode, server-side anomaly scoring, advanced analytics, bulk provisioning** — deferred fast-follows, out of this product's first cut.
- **Sensitive configuration-value display** — out of the read MVP. `[ASSUMPTION]`

## 14. MVP Scope & Phasing

The PRD describes the whole Phase 2 vision; the **MVP is the read-only foundation only**.

> **Build-readiness status (updated 2026-06-05):** Story 1.0 completed the FrontComposer shell-integration spike and confirms `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`. Story 1.1 can proceed from the FrontComposer gate perspective once sprint status is synchronized. Tenant-list implementation still requires the `FC-TBL` decision; every UI story still needs accessibility/localization/responsive/documentation evidence; audit/proof UX remains gated by Epic 5 evidence readiness. **This PRD is a complete plan, not a blanket green light for all stories.**

### 14.1 In scope (MVP — "Phase 2a")
- The **Operations Shell** with Tenants, Users, and Global Administrators areas functioning (read).
- **FR-1..FR-9, FR-18** — tenant discovery/triage, my-tenants & user lookup, tenant detail & configuration view, member & access review (read), global-administrator review (read).
- **Truth State Badge + Freshness** on all read surfaces; **Unavailable Action Reason** reflected (no mutations yet).
- Full **accessibility, localization, and responsive read evidence** per §9.
- Support-safety per §10 across all read surfaces.

### 14.2 Out of scope for MVP (later phases)
- **Phase 2b — first command flows:** FR-10 (add user), FR-11 (change role), FR-13 (create tenant), FR-14 (edit metadata). *These are the most tractable command flows. Story 1.0 confirms the command-feedback contract (`FC-CMD`) and one-at-a-time command policy (`FC-CNC`); command stories still need the shared command lifecycle/truth/preview foundations and per-story evidence before they are ready.*
- **Phase 2c — high-impact, audit & recovery (gated on FrontComposer components / fallback approvals):** FR-12 (remove user), FR-15 (disable/enable), FR-16–17 (configuration commands), FR-19 (global-admin commands), FR-20–23 (audit trail & evidence), FR-24–25 (compensating recovery). FR15 is a reversible lifecycle soft-delete / availability-control flow; FR19 global-admin commands carry platform-governance blast radius and are implemented by Epic 4 Stories 4.3 and 4.4 with fixed-scope routing, projection confirmation, and last-admin safety.
- **Audit nav area in MVP:** present in the shell but its list/evidence content is a Phase 2c deliverable (blocked on `FC-AUD`); MVP shows it as not-yet-available rather than a broken surface. `[ASSUMPTION]` — confirm whether to hide or stub the Audit area in 2a (§16).

### 14.3 Phasing summary
`Phase 2a (MVP)` read-only foundation → `Phase 2b` first command flows → `Phase 2c` high-impact + audit + recovery. A backlog item carries `planning-only` (read, tenant-scoped, or approved reversible lifecycle-control fallback) or `blocked` (hard destructive platform-governance action, or dependent on a missing component with no approved fallback); it promotes to *ready* only when its FrontComposer dependencies resolve **or** a Product/UX-approved fallback is recorded. **The fallback-approval gate is satisfied** for the three interim fallbacks (recorded 2026-06-03 — see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md)); **the shell/layout/command contracts are satisfied** by Story 1.0 (2026-06-05). Remaining promotion gates are story-specific evidence, `FC-TBL` decisioning for the tenant list, and audit/proof evidence readiness. The 2026-06-06 correction reclassifies FR15 as eligible under the approved command and preview fallbacks; hard destructive tenant deletion remains out of scope for future administrators-only CLI tooling.

## 15. Success Metrics

Targets are `[ASSUMPTION]` pending your numbers; methods noted. SMs cross-reference the FRs/CPs they validate.

**Primary**
- **SM-1 (Safe operations adoption):** share of routine tenant/access/lifecycle operations performed through the UI rather than raw command-API calls. *Target: majority within one quarter of the relevant command phase shipping.* Validates FR-10..FR-17, FR-19. *MVP leading indicator:* operators use the UI as the default place to answer access questions (SM-2).
- **SM-2 (Time-to-answer "who has access / what changed"):** median time for an operator to answer an access/audit question via the UI. *Target: under ~1 minute for access; under ~3 minutes for an audited change.* Validates FR-1..FR-9, FR-18, FR-20..FR-23.
- **SM-3 (Self-service uptake):** share of tenant-owner-initiated member/role changes done by owners themselves (vs. operator tickets). *Target: rising quarter over quarter once Phase 2b ships.* Validates FR-3, FR-11.
- **SM-6 (Trust — no false success):** the direct measure of the product thesis — **zero confirmed cases** where the UI presented success (styling, copy, or announcement) for an action not yet `confirmed` against the source-of-truth projection, and high-impact actions on `stale`/`unknown` data are gated rather than completed. *Target: 0 false-success incidents.* Validates CP-3, CP-4, R-2.

**Secondary**
- **SM-4 (Onboarding time):** time to stand up a new, owned, configured tenant via the UI. Validates FR-13, FR-10, FR-16.
- **SM-5 (Recovery without data-store edits):** number of corrections performed via compensating commands through the UI; target trend up while raw/manual fixes trend to zero. Validates FR-24, FR-25, NFR-5.

**Counter-metrics (do not optimize)**
- **SM-C1 (Don't streamline away safety):** destructive/high-impact actions completed **without** viewing a Consequence Preview should stay at ~0. We do **not** want to "reduce friction" by removing the preview. Counterbalances SM-1/SM-4.
- **SM-C2 (Don't hide errors to look successful):** rejected/failed commands must remain clearly surfaced; a *drop* in surfaced errors is a red flag (likely suppression), not a win. Counterbalances SM-1.
- **SM-C3 (Don't trade trust for speed):** any rise in users acting on `stale`/`unknown` data (override usage) is a regression even if it speeds tasks. Counterbalances SM-2/SM-6.

## 16. Open Questions

1. **Command endpoint route** — confirm `POST /api/v1/commands` vs. the unversioned `/api/commands` alias against the deployed gateway before any command phase. (addendum)
2. **FrontComposer component gaps — fallback approval RESOLVED (2026-06-03); shell/command contract confirmation RESOLVED (2026-06-05).** Product/UX approved the flat-audit-list, inline-consequence-preview, and one-at-a-time fallbacks (see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md)); Story 1.0 confirmed `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` (see `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`). Building `<AuditTimeline>`/`<ConsequencePreview>`/shared `FC-TOK` capabilities in FrontComposer remains a post-fallback enhancement, not a blocker.
3. **Tenant-list grid decision (`FC-TBL`)** — current generated grid support is available but does not satisfy cursor pagination, safety-column pinning, or six non-collapsing list states. Resolve in Story 1.2 before tenant-list implementation.
4. **Localization resource ownership** — shared shell resources vs. Tenants-owned keys + adopter terminology.
5. **WCAG 2.2 AA** — confirm what the pinned Fluent UI Blazor version actually supports; the 2.2 target is conditional.
6. **RTL support** — in or out for v1? (none of the specs commit.)
7. **Cursor durability across replicas/restarts** — deferred backend epic; until then, what is the UI's expected behavior on cursor invalidation?
8. **Consequence Preview scope for config edits (FR-16)** — always required, or only for a high-risk key subset? (Also a phasing lever.)
9. **Audit area in MVP (Phase 2a)** — hide it, or show a "not yet available" placeholder?
10. **Freshness thresholds** — the numeric `current`/`aging`/`stale` cutoffs (deferred to implementation) need product input.
11. **Sensitive configuration values** — if/when the UI should ever display them, and under what authorization.
12. **Source-spec ID-scheme correction** — the operations-shell (and other) UI specs state tenant/user ids are ULIDs, contradicting the authoritative domain rule (caller-supplied strings; only `MessageId` is a ULID). The specs should be corrected so downstream work never parses a `TenantId`/`UserId` as a ULID. (R-6)
13. **Owner self-service depth** — v1 is honest-minimal (shared surfaces, no owner-only screens). Confirm when/whether dedicated owner journeys/UX become a funded scope. (§3, §13)

## 17. Assumptions Index

- §3 — Owner self-service in v1 = shared, authorization-scoped surfaces only; no dedicated owner screens/journeys (honest-minimal).
- §3.2 — Programmatic integrators keep using APIs directly; this UI is human-only.
- §3.3 / §7.4 / §13 — Adding a user is a **direct add by user id**; no email-invitation flow in v1 (backend has no invitation concept).
- §5.2 — Fluent UI semantic theme roles (no separate branded palette) are the visual authority.
- §5.3 — Mobile is read-only; no high-impact commands on mobile.
- §7.2 (FR-6) / §13 — Sensitive configuration-value display is out of read MVP.
- §7.6 (FR-16) — Whether every config edit needs a Consequence Preview, or only a high-risk subset, is unresolved.
- §8 (NFR-1) — Read-surface performance budgets (~1s warm) are placeholders pending confirmation.
- §9 — WCAG 2.2 AA is a conditional target dependent on Fluent stack support; RTL undecided; localization resource ownership undecided.
- §14.2 — Audit nav area present-but-not-yet-available in MVP (hide vs. stub to be confirmed).
- §15 — All metric targets are placeholders pending your numbers.
- General — MVP = read-only foundation (`ui-01..06` equivalent) per your scope decision; full Phase 2 vision documented with command/audit/recovery in later phases.
