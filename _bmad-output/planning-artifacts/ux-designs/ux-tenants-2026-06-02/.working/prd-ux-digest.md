# UX Digest — Tenants Management UI (PRD `prd-tenants-2026-06-02`)

> Provenance: subagent extraction (2026-06-02) of the final PRD + addendum + 6 reconcile notes + downstream-readiness review. Preserved here as Discovery input; the spines are distilled from it at Finalize. Spines win on conflict.

---

## 1. Product & scope

The **Tenants Management UI** is the end-user web application for managing tenants, members, roles, namespaced configuration, lifecycle, and audit on the Hexalith platform, built on the **Hexalith.FrontComposer** framework. A single role-scoped application serves two audiences from the same surfaces: **platform operators / global administrators** (triage and act across every tenant) and **tenant owners** (manage only their own tenant). It turns capability today reachable only by hand-crafting command-API calls and reading projections directly into a "trustworthy operations and self-service experience" — letting users *see the truth* about access and change, *act safely* through previews and guardrails instead of raw commands, and *recover from mistakes* through forward compensating actions, "all without ever editing history or the data store." Its distinctive thesis is **honesty about state** in an eventually-consistent, event-sourced system: it surfaces data freshness, blocks high-impact actions when it cannot prove data is current, never reports unconfirmed success, and treats corrections as new auditable commands rather than silent "undo."

**In UI scope:** screen composition, column sets, route binding, custom command flows (not generated CRUD), client-side assembly of receipts/previews/command-status. **Out of UI scope:** adding/altering backend endpoints; reshaping immutable domain contracts; the UI *enforcing* authorization (it only *reflects* server-enforced authorization — NFR-2, CP-9); editing/deleting events, projections, or state to "fix" data (NFR-5); building the missing FrontComposer components inside Tenants (they belong in FrontComposer); email/link invitations; high-impact command flows on mobile; sensitive configuration-value display; grouped/session audit mode, anomaly scoring, advanced analytics, bulk provisioning.

---

## 2. Personas / actors (verbatim names)

**Platform operator / Global administrator** — triages and acts across every tenant; expert/power user operating under incident pressure. Named instances:
- **Elena** — "platform operations engineer" (protagonist of UJ-1, UJ-2, UJ-3, UJ-6). Goals: find/triage the right tenant fast; know whether data is current before acting; see exactly who can do what; act safely on access/lifecycle with consequence preview + confirmation; recover by correcting forward; prove what happened with support-safe audit evidence.
- **Sofia** — "incident & support lead" (protagonist of UJ-4). Goal: understand and undo the *effect* of a mistaken access change via audit + compensating command; cite a support-safe reference to a stakeholder.

**Tenant owner** — manages only their own tenant; authorization-scoped to it; non-expert relative to operators. Named instance:
- **Nadia** — "owner of a customer tenant" (protagonist of UJ-5). Goals: manage own tenant's members/roles without a ticket; see config + status and understand effects of changes; be stopped from dangerous accidents (e.g. removing the last owner) "without being hard-blocked when I genuinely mean it." She "never sees other tenants or the Global Administrators surface."

**Member / self-auditing user** — verifies own access; lowest expertise, narrowest scope. Named instance:
- **Marc** — Goal: "See which tenants I belong to and in what role."

**Non-users (v1):** anonymous/unauthenticated visitors (every surface requires authentication); programmatic integrators (continue to use APIs directly — "this UI is for humans"); end-consumers of a tenant's product.

> Note: v1 owner self-service has **no dedicated owner-only screens, onboarding, or journeys** — owners ride the *same* surfaces as operators, authorization-limited (an explicit downstream-UX gap; §3, §13, Open Question #13). Source specs are operator-centric.

---

## 3. Surfaces / screens implied

Anchored to the **Operations Shell**: four primary navigation areas, in order — **Tenants** (default landing/triage), **Users** (secondary but reachable; not co-equal — see reconcile-operations-shell GAP-10), **Global Administrators**, **Audit**. Command lifecycle is **never** a primary nav area — shown inline, anchored to the affected row/panel.

- **Tenant list / triage surface** (FR-1) — default landing; scan/search/filter/sort/page tenants via cursor pagination.
- **Tenant detail** (FR-2, FR-5) — read-only single-tenant overview: status, metadata, member/configuration summaries, counts, freshness; supports deep-linking; returning to list restores filter/sort/selection.
- **Tenant configuration view (read-only)** (FR-6) — key/values grouped by namespace, filtered to the caller's owned/authorized prefix.
- **"My Tenants" self-audit** (FR-3) — signed-in user's own memberships + role per tenant.
- **User lookup / user memberships** (FR-4) — operator searches a user, views that user's tenant memberships; reachable from a member row.
- **Member table / access-review surface** (FR-8, FR-9) — per-tenant members with role, owner count, status, freshness, orphan context; read-only, "must not imply mutation"; per-row reflected action availability + Unavailable Action Reason.
- **Global Administrators review surface** (FR-18) — who holds global-admin access; visible only to authorized operators; data from the single fixed-identity `global-administrators` aggregate.
- **Audit trail list** (FR-20, FR-21) — flat, stably-ordered, cursor-paginated; date + `AuditEventCategory` filters; reachable top-level and contextually.
- **Audit Evidence Receipt** (FR-22) — support-safe receipt panel for a recorded action.
- **Consequence Preview** (CP-5, FR-12, FR-15, FR-16) — pre-submission summary panel before destructive/high-impact actions.
- **Command Lifecycle Panel** (FR-12, Glossary) — tracks a dispatched command's lifecycle inline without overwriting confirmed projection data.
- **Command flow surfaces** (mutations): Add user (FR-10), Change role (FR-11), Remove user (FR-12), Create tenant (FR-13), Edit metadata (FR-14), Disable/Enable tenant (FR-15), Set/Remove configuration (FR-16/FR-17), Grant/Remove global administrator (FR-19), Start/Preview compensating command (FR-24/FR-25). These are **custom command flows, not generated CRUD**.

> In MVP (Phase 2a), the Audit nav area is "present in the shell but its list/evidence content is a Phase 2c deliverable… MVP shows it as not-yet-available rather than a broken surface." Open Question #9: hide vs. stub.

---

## 4. Key flows / journeys (UJ-1 … UJ-6)

All journeys but UJ-5 are operator-driven. Each carries Persona+context, Entry state, Path, Climax, Resolution, Edge case.

**UJ-1. Elena triages tenants under pressure** *(Phase 2a / MVP)*
Entry: authenticated global administrator; lands on tenant list (default triage surface). Path: filters/searches → scans status, owner/member counts, freshness → opens suspect tenant detail → returns to list with filters/selection preserved.
**Climax:** "she has the right tenant open with its current state in front of her, and knows how fresh that state is." Resolution: proceeds to UJ-2. Edge: an unmeasurable freshness value shows `unknown` rather than implying current.

**UJ-2. Elena reviews who can do what** *(Phase 2a / MVP — read; action availability reflected)*
Entry: tenant detail open. Path: opens member table → reads each member's role, owner count, status, freshness → sees per-row which actions *would* be available, and where one is not, a plain-language reason (e.g. "you don't have permission", "data is stale — refresh first").
**Climax:** "she understands the access picture and exactly what she could safely change." Resolution: stops here in MVP (read-only); later proceeds to UJ-3.

**UJ-3. Elena safely removes a user's access** *(Phase 2c)* — the flagship worked journey.
Entry: a specific member row with target user, current role, freshness visible. Path: system validates inputs and gates (freshness + authorization must be `eligible`, else action is unavailable with a stated reason) → she opens a **Consequence Preview** (owner-count impact, access being revoked, recovery path, audit expectation, explicit known-unknowns) → for high-risk cases (drops owner count to zero, OR target also holds global-administrator authority) she clears **elevated-friction confirmation** → confirms → command dispatches and the **Command Lifecycle Panel** tracks `submitted → accepted → projection_pending → confirmed`, then `audit_pending → audit_available`.
**Climax / key decision beat:** "access is changed and **proven**, with no false 'done' shown before the source-of-truth projection confirms it." Resolution: change is on the audit record. Edges: incomplete preview inputs block submission (fail-closed); removing the last owner is a warning with extra friction, never a hard block; an already-applied removal reads `already applied`, not failure; unconfirmable reconciliation reads `unable to verify` (never success); lost permission mid-flow → told + offered recovery.
> Reconcile-remove-user flags the Consequence Preview must carry the **full 10-item content set** (§2.1), not just "consequences/unknowns"; the **target-also-holds-global-admin friction case** is a distinct trigger; **already-applied** and **unable-to-verify** must be named reconciliation outcomes; validation must pass **before the preview opens**, not only at submit; the remove control "must **not** appear as a casual primary action."

**UJ-4. Sofia investigates an incident and recovers** *(Phase 2c)*
Entry: opens the audit trail (from nav, a tenant row, or a command result). Path: filters audit list → reads an **Audit Evidence Receipt** (who acted, on whom, in which tenant, outcome, when, support-safe reference) → identifies the wrong change → starts a **compensating command** ("restore intended access") → previews the correction against current state → submits a *new* command; original and corrective records are linked.
**Climax / key decision beat:** "the effect is corrected forward, with the mistake and the fix both permanently on the record." Resolution: Sofia cites the support-safe reference. Edge: delayed/unavailable evidence shows honestly (`audit pending` / `audit delayed` / `audit unavailable`) and offers retry/wait/escalate — "it never fabricates proof."
> Reconcile-audit-recovery adds recovery paths the PRD omitted: **"reassign tenant owner"** and **"retry access removal"**; a global-admin correction is a separate `global-administrators`-domain command, not a tenant edit; restore-after-last-owner relies on the **empty-tenant bootstrap** (`HasMembershipHistory == false`).

**UJ-5. Nadia self-serves her own tenant** *(Phase 2a / MVP — read; role change Phase 2b)*
Entry: authenticated tenant owner; sees only her own tenant (authorization-scoped). Path: opens her tenant's member table → reviews members and roles → (later phase) changes a teammate's role and watches it confirm.
**Climax:** "she manages her own access picture independently." Resolution: operators are no longer a bottleneck. Edge: "she never sees other tenants or the Global Administrators surface."

**UJ-6. Elena onboards a new tenant** *(Phase 2b/2c)*
Entry: authenticated operator. Path: creates the tenant → adds the first owner **directly by user id** → sets initial configuration → confirms each step landed.
**Climax:** "a usable, owned, configured tenant exists." Resolution: the owner (Nadia) can self-serve (UJ-5). Edge: "adding a user is a *direct* add by user id — there is no email-invitation step in v1."

---

## 5. Component & interaction requirements

- **Tenant list / DataGrid** — cursor pagination (never offset/limit). Required row columns per reconcile-operations-shell GAP-2 (spec-mandated, PRD body under-specified): **tenant identity, status, member count, owner count, pending state, Truth State Badge with freshness.** Must render six distinct non-collapsible states (see §6). Sorting/paging must **never hide a pending or stale marker** (GAP-4). **Row actions stay stable in width and placement** under data/sort/page change (GAP-9).
- **Member table** — read-only; "must not imply mutation"; exposes accessible semantics (headers, sort state, row relationships); freshness per Truth State Badge; orphan/disabled context flagged.
- **Forms / command flows** — custom command flows, not generated CRUD (reconcile-operations-shell GAP-6: generated FrontComposer composition is appropriate *only* for low-risk, read-only, projection-backed surfaces; cross-tenant revoke/remove must not be generated from query rows). Validation errors surface as safe localized field messages.
- **Command-submission pattern** — dispatch via the command endpoint (`POST /api/v1/commands`, alias open — §16.1); duplicate submits de-duplicated; NoOp → `already applied`; rejections → safe localized text (never stack trace).
- **Consequence Preview** (CP-5) — required before destructive/high-impact actions; **10-item content set** (addendum §H / remove-user §2.1): tenant, target user, current role, owner-count impact (incl. last-owner/zero-owner), specific access revoked/changed, current freshness of inputs, recovery path afterward, audit expectation, target's platform standing (e.g. also a global administrator), explicit **known consequences vs. known unknowns** (no over-claiming — session/token invalidation are known-unknowns unless proven). Incomplete inputs **block submission** (fail-closed). Currently rendered as **proposed inline consequence text** pending `FC-CNS` + Product/UX approval.
- **Confirmation / friction pattern** — asymmetric (CP-6): **last-owner = warning + elevated friction, never blocked**; **last-global-administrator = hard-rejected by backend, surfaced as *unavailable* with a clear reason, not completable friction**; target who also holds global-admin authority = additional platform-level friction flag (reflected only — does not change which command dispatches). The destructive control "is **not a primary/casual button**." Modal focus trap with a safe escape that does **not** commit a destructive action.
- **Command Lifecycle Panel** — tracks dispatched command state without overwriting confirmed projection data; never collapses lifecycle states.
- **Truth State Badge** — composes freshness + authorization + command lifecycle + projection confirmation + audit dimensions (13 canonical states; §6).
- **Operations Shell** — the application's information architecture: the FrontComposer shell framing the four nav areas (Tenants / Users / Global Administrators / Audit), preserving context (selection, filters) across surface navigation so "users don't lose their place." Audit reachable both top-level and contextually (tenant rows, tenant detail, user lookup, command results). Command status/feedback always shown **inline, anchored to the affected row/panel** — never a nav area.
- **Copy support-safe identifiers** (FR-7) — copy the full id (a caller-supplied string, **not assumed a ULID**) and any support-safe reference; no payloads/tokens/correlation ids/PII.
- **Automation** (NFR-4) — every interactive element + status carries a **stable automation selector / component contract**, never keyed on row text or color.

---

## 6. State patterns

**Canonical state sets are used VERBATIM, no per-screen reinterpretation (CP-10).** Casing is significant and intentional — the Truth State Badge uses space-form `audit pending` / `audit available`; the RemoveUserFromTenant state machine uses snake_case `projection_pending` / `audit_pending` / `audit_available`. "These are the same concepts, different tokens — do not unify." (The downstream-readiness review Finding 1 flags High-severity drift between PRD-body hyphen/spaced forms and the addendum's underscore forms — designers should treat the addendum §G underscore forms as the verbatim machine tokens.)

- **Truth State Badge — 13 states:** `current`, `refreshing`, `aging`, `stale`, `unknown`, `eligible`, `blocked`, `pending`, `accepted`, `confirmed`, `failed`, `audit pending`, `audit available`.
- **Freshness — 5 states:** `current` (usable), `refreshing` (usable nudge), `aging` (**usable with friction**), `stale` (**blocks** high-impact action), `unknown` (**blocks**, fails closed).
- **Command lifecycle — 10 tokens:** `eligible`, `previewed`, `submitted`, `accepted`, `rejected`, `already applied`, `failed`, `duplicate`, `timeout`, `unknown` (`duplicate`/`timeout` have no dedicated gloss — "do not invent one"). RemoveUserFromTenant additionally uses `projection_pending`, `confirmed`, `audit_pending`, `audit_available`.
- **Layered feedback — 10 states (must not be merged):** `request sent (submitted)`, `accepted`, `projection pending`, `confirmed`, `rejected`, `already applied`, `degraded` (**success-prohibited**), `audit pending`, `audit available`, `unable to verify` (**success-prohibited**).
- **List-surface states — six distinct, non-collapsible** (FR-1; reconcile-operations-shell GAP-3 — PRD body under-named): **loading, empty, filtered-empty, error, stale, degraded** ("none may be collapsed into another"). `filtered-empty` offers a clear filter reset; `stale` shows freshness marker + refresh path; `degraded` explains what is unavailable and what still works. Audit list states (FR-20): loading / empty / filtered-empty / error.
- **Loading / empty / error** — empty states are authorization-safe (no leak of out-of-scope tenants/members; FR-4 empty ≠ error).
- **Optimistic / pending command states** — the **Non-collapse invariant (CP-3):** `accepted` ≠ `confirmed` (projected) ≠ `audit available` (proven); `degraded` and `unable to verify` are distinct, success-prohibited. **"The UI never shows success — styling, copy, or announcement — that it has not confirmed against the source-of-truth projection."** Pending = an in-flight, not-yet-confirmed indicator on the affected row/panel.
- **Eventual-consistency / availability signals (CP-4):** real-time SignalR projection notifications are **freshness nudges only** — they "never advance a command to `confirmed` or audit to `audit available`." Projection is the **source of truth**. **TenantDisabled/TenantEnabled:** disabled is "an eventually-consistent availability signal"; commands targeting a disabled tenant are **rejected** (`TenantDisabled`); the lifecycle status displays with no-color-only encoding.
- **Rejection events surfacing:** business-rule refusals come back as domain rejection events → rendered as **safe, localized text only** (never a stack trace; RFC 7807 at the HTTP boundary, but the UI shows safe text). Specific rejections: `UserAlreadyInTenant`, `RoleEscalation`/`Unknown` target, `ConfigurationLimitExceeded`, `ConfigurationKeyNotFound`, `TenantLifecycleStateAlreadySet`, `TenantDisabled`, `TenantAlreadyExists`, `LastGlobalAdministrator`. **NoOp** cases (`ChangeUserRole` to current role; `SetTenantConfiguration` identical key+value) surface as `already applied`. Re-adding an existing member is **NOT** a NoOp — it is **rejected** (`UserAlreadyInTenant`).
- **Audit availability — 4 states (do not collapse):** `audit pending` (proof not yet available, expected), `audit delayed` (taking longer, capability exists), `audit unavailable` (path currently unavailable, e.g. read error — distinct from not-built), `missing implementation support` (capability not built — the `FC-AUD` dependency). **None is ever shown as success.**
- **Recovery — every failure mode maps to a distinct recovery (CP-8), never dead-ends:** stale → refresh; pending → wait; status-lookup failure → retry status lookup; missing permission → request permission/escalate; wrong change → start correction / restore intended access; unverifiable → escalate with a support-safe reference. Canonical recovery verbs: `start correction`, `restore intended access`, `retry status lookup`, `inspect audit`, `escalate`; plus `refresh`, `wait`, `continue read-only`, `request permission`, `start a compensating command`, and (per reconcile) `reassign tenant owner`, `retry access removal`. **Prohibited words:** `undo`, `rollback`, `hidden edit`.
- **Unavailable Action Reason — 6 categories:** `missing permission`, `stale data`, `missing lifecycle support` (→ `FC-CMD`), `missing consequence preview` (→ `FC-CNS`), `missing audit proof` (→ `FC-AUD`), `high-impact flow not ready` (unresolved backlog `blockedBy`). Inline-visible, not hover-only.

---

## 7. Accessibility & localization

**Standard:** baseline **WCAG 2.1 AA**; target **WCAG 2.2 AA where the selected Fluent UI Blazor / FrontComposer stack supports it** — *conditional, no unconditional 2.2 promise* (Open Question #5).

**Keyboard & focus:** all interactive elements reachable; logical focus order; visible focus in normal/high-contrast/forced-colors; modal focus trap with a safe escape that does **not** commit a destructive action; focus returns to the launching row/control after close/cancel/submit/failure; **"keyboard users can complete or exit every modal/preview/table/command workflow."** (Reconcile-a11y Gap 4 stresses the complete-OR-exit-every-workflow guarantee as a standalone obligation.)

**Screen reader & status:** accessible names for all statuses, badges, freshness indicators, actions; **absolute timestamps (not relative-only)**; table semantics (headers, sort state, row relationships); live regions with appropriate politeness — **`assertive` reserved for rejection/failure/destructive-blockers/unable-to-verify**; **"never announce success before projection truth."**

**No-color-only & motion:** color is **never the sole signal** — every status conveyed by text + icon/shape as well as color, legible in light/dark/high-contrast/**forced-colors**; reduced-motion users "never dependent on animation."

**Disabled-action explanations:** inline-visible (not hover-only); **"tooltips may supplement but cannot be the only explanation"** (reconcile-a11y Gap 5).

**Localization / i18n:** all state labels, role names, timestamps, warnings, disabled reasons, recovery actions, confirmation copy, and empty/loading/error/degraded/stale/unavailable copy are localizable, with culture-aware formatting. **"No runtime sentence-fragment assembly" — whole resource strings with named placeholders.** Resource ownership (shared shell resources vs. Tenants-owned keys) is an **open question** (#4). **RTL support is undecided** (#6 — "none of the specs commit").

**Languages:** **(not specified)** — no explicit target language list is given; only "localizable / culture-aware formatting."

**Acceptance evidence (definition of done):** keyboard-only navigation; screen-reader review (**NVDA + at least one browser/SR pairing**); automated accessibility checks; forced-colors/high-contrast; reduced-motion; contrast; live-region announcements; focus return; hover-free disabled explanations; **plus documentation/reference (`FC-DOC`) evidence** (reconcile-a11y Gap 2). **Required acceptance scenarios:** stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, permission-missing. Responsive evidence must include **horizontal table overflow, navigation collapse, and command-preview/dialog behavior at narrow widths** (reconcile-a11y Gap 3).

**Ready-gate** (reconcile-a11y Gap 1, High): a UI story cannot be `ready` until it cites applicable accessibility, localization, responsive, and **documentation/reference (`FC-DOC`)** evidence — or records a Product/UX-approved row-specific fallback documenting five things: keyboard/focus/live-region behavior, localizable-copy responsibility, documentation/reference evidence, replacement path, and owner approval.

---

## 8. Responsive & visual

**Form factors / breakpoints** (the layout rule, verbatim — reconcile-responsive-visual GAP-1 stresses the PRD body dropped these; the §9 widths are *test* widths, distinct):
- **mobile 320–767px**, **tablet 768–1023px**, **desktop 1024px+**, **wide desktop 1440px+** (the 4th "wide desktop" tier).
- **Desktop-first** (the primary admin workstation: dense tables, keyboard/mouse, side-by-side context); responsiveness exists to *prevent breakage*, not to re-target smaller screens (GAP-6).
- **Tablet:** navigation collapses, regions stack, tables preserved via scroll / column-priority — **not gesture-redesigned** (touch targets adequate, but no gesture-first workflows).
- **Mobile:** **read-only triage, lookup, and audit reference only — no high-impact command flows.**
- **Safety-critical columns never drop:** identity, status, freshness, role, risk are preserved at every width via horizontal scroll / column priority — **never hidden** (reconcile-responsive-visual GAP-2 warns "column-priority" must not be read as permitting a safety-critical column off-screen).
- **Fail-closed responsive rule:** if a width cannot preserve full safety context for a high-impact action, that action becomes **unavailable** (with a visible reason) rather than rendering unsafely.

**Content density / layout intent:** "professional, calm, precise operations console — not marketing"; system UI typography; modest hierarchy; plain-language labels; **compact density**; "whitespace groups meaning rather than adding drama"; **tables, split views, tabs, side panels, dialogs, and inline status regions are preferred over decorative card grids** (reconcile-responsive-visual GAP-3/GAP-4 — the PRD body lost this positive preference list). Fluent-compatible **4px spacing rhythm** with 8/12/16/24/32px steps; full-width operational surfaces with constrained readable inner regions (tied to the open `FC-LYT` contract — GAP-5). Layout is **stable** (reserves space to avoid shift); destructive/warning styling used **sparingly**. **Out of the first visual slice** (unless Product/UX promotes): decorative card dashboards, branded palettes, hero-scale typography, advanced/grouped visual modes, bespoke per-state color literals.

**Visual-identity status — EXPLICITLY LEFT OPEN / constrained, not freely chosen:** **Microsoft Fluent UI is the visual authority; there is NO separate branded palette** (`[ASSUMPTION]`). Meaning maps to **semantic theme roles, never hard-coded colors / hex** — six meaning→semantic-role mappings exist (tenant status, freshness, lifecycle, authorization, audit, risk). **No-color-only encoding** is mandatory. So: *there is no bespoke brand identity to design* — the visual direction is "Fluent semantic roles + calm operations-console tone." The genuinely open visual choices left to UX/elicitation are the `FC-LYT` full-width-vs-constrained decision (#3) and any promotion of out-of-first-slice visual elements; the human does **not** own a free choice of palette/brand (the PRD forbids inventing one). Designers must not assert a Fluent token name as available without verifying against the pinned package (`5.0.0-rc.3-26138.1`).

---

## 9. Audit & recovery

**Audit trail surfacing:** a **flat, stably-ordered, cursor-paginated list** (FR-20) with **date** and **`AuditEventCategory` (`Access` / `Administrative`)** filters; targets ~500 events without unacceptable degradation; loading/empty/filtered-empty/error states distinct and accessible. The flat list is a **proposed fallback for the absent `<AuditTimeline>`**, usable only once Product/UX approves it (reconcile-frontcomposer-depmap Gap 3 and reconcile-operations-shell GAP-7 both flag the PRD's "approved fallback" wording as overstated — it is *pending* approval, contradicting Open Question #2 and R-1). Reachable from nav, tenant row, tenant detail, user lookup, and command result (FR-21), each entry point scoped accordingly.

**Audit Evidence Receipt** (FR-22): a **support-safe** receipt — actor, target, tenant scope, outcome, timestamp, projection marker, audit/command reference — assembled **client-side from a structured `NarrativePayload`** (never the raw event payload; no new backend receipt endpoint). Target resolution: `NarrativePayload.userId` → `key` → `TenantId`. The projection marker is the read-model freshness marker (ETag → 304). Never exposes raw payloads, tokens, correlation ids, raw event metadata, or PII. **Partial completion shows the actual lifecycle state (e.g. `audit pending`), never pre-rendered proof** (CP-3). Positive copy-safe allow-list includes "audit event reference **or fallback state**" for unavailable/missing-support cases (reconcile-audit-recovery Gap 8).

**Compensating-command corrections (the immutable-history model):** **"Correct forward, never undo" (CP-7).** Recovery is always a **compensating command** — a new forward command (e.g. `AddUserToTenant` / a role change) with its **own Consequence Preview and proof**; the original event is untouched; both records reference each other (FR-24/FR-25). **The UI never labels this "undo," "rollback," or "hidden edit."** The correction **previews against current state** (the original effect may already differ). Restoring access to a tenant with no remaining membership history relies on the **empty-tenant bootstrap path** (`HasMembershipHistory == false`, where `AddUserToTenant` skips owner-only RBAC) — what makes restore-after-last-owner possible; no "≥1 owner" backend invariant exists. A global-administrator correction is a **separate `global-administrators`-domain command** (`SetGlobalAdministrator`/`RemoveGlobalAdministrator`) that does **not** edit a tenant aggregate. Named recovery paths from audit-evidence detail: reassign tenant owner, restore intended access, retry access removal, inspect audit, escalate.

> **Build caveat:** FR-22/FR-24/FR-25 (receipt assembly + compensating recovery) are "committed product intent but… **not yet backed by a dedicated `ui-NN` backlog row or backend evidence**" — they need a future story before build-ready (the recovery half of Story 9.5).

---

## 10. FrontComposer dependency / readiness gates *(critical)*

FrontComposer is the platform UI framework this UI composes. The PRD is explicit: **"do not treat unconfirmed capabilities as given."** Per repo policy, missing shared UI capability belongs in **FrontComposer, not Tenants** — Tenants must never absorb the scaffolding.

| FC code | Capability | Readiness | What it gates |
|---|---|---|---|
| **FC-TBL** | Projection list/table (DataGrid, filter/search/empty/loading) | **available** | Backbone of all read surfaces; "the only fully-available capability." |
| **FC-LYT** | Shell layout contract (full-width vs constrained) | **needs-confirmation** | Gates `ui-01..15` — **including the read-only MVP.** Full-width-vs-constrained shell/surface layout is unconfirmed (Open Question #3). |
| **FC-CMD** | Command-lifecycle feedback (three-phase, projection-confirmed) | **needs-confirmation** | Required for **ALL command FRs**. Maps to the `missing lifecycle support` Unavailable-Action-Reason. |
| **FC-CNC** | Concurrent-command / toast-batching policy | **missing** | Gates remove-user and bulk-revocation; **applies to all command FRs**. (reconcile-frontcomposer-depmap Gap 1, HIGH: addendum §B understates this as `needs-confirmation` — the spec says `missing`; multi-row/rapid command flows are blocked/planning-only until Product/UX approve a one-at-a-time fallback.) |
| **FC-TOK** | Status/severity/timeline tokens | **missing** | Polished audit/consequence visuals; use existing Fluent/FC badges as a *proposed* named fallback — "do not assert a token name as available." |
| **FC-AUD** | `<AuditTimeline>` | **missing** | Audit timeline (FR-20). Proposed fallback **pending Product/UX approval**: flat audit DataGrid. Maps to `missing audit proof` / `missing implementation support`. |
| **FC-CNS** | `<ConsequencePreview>` | **missing** | Consequence Preview (CP-5, FR-12/15/16). Proposed fallback **pending approval**: inline consequence text. "Implementation convenience is not approval." Maps to `missing consequence preview`. |
| **FC-A11Y** | Accessibility primitives | **needs-confirmation** | First-class, **non-removable even under fallback**. |
| **FC-L10N** | Localization (shell resources) | **needs-confirmation** | Resource ownership undecided (Open Question #4). |
| **FC-DOC** | Component documentation/Storybook | **needs-confirmation** | **Required for "ready"** (ready-gate, §7). |

> Note (reconcile-frontcomposer-depmap Gap 2, HIGH): PRD §11 prose lists FC-LYT and FC-CMD as "provides — treat as given," contradicting its own addendum (both `needs-confirmation`). Treat both as **needs-confirmation**.

**BLOCKED from being built right now (everything):**
- **The read-only MVP itself is blocked** — gated on confirming **FC-LYT** (needs-confirmation). Per §14: "**No backlog row is unblocked yet — not even the read-only MVP.**" Nothing is promotable until Open Questions #16.2 (`FC-AUD`/`FC-CNS`/`FC-CNC` fallbacks or scheduling) and #16.3 (`FC-LYT`) are decided.
- **All command flows** additionally need **FC-CMD** (needs-confirmation) + **FC-CNC** (missing) to resolve first. "There are no command flows that are unblocked today."
- **Audit / high-impact / recovery flows** need **FC-AUD** / **FC-CNS** (and **FC-TOK**) or **approved fallbacks — none approved yet.** Remove-user (FR-12) is blocked on `FC-CNS` + `FC-CMD` + `FC-CNC`.
- **Readiness severity split** (reconcile-frontcomposer-depmap Gap 5): platform-wide destructive actions — **FR-15 (disable/enable)** and **FR-19 (global-admin grant/remove)** — are categorically **`blocked`**; tenant-scoped destructive — **FR-12 (remove-user)** and **FR-16/17 (config)** — are **`planning-only` (fallback-eligible)**.

**Intended path once gates clear (not "now"):** Phase 2a (read-only foundation — FR-1..9, FR-18, shell read) unblocks **only after `FC-LYT` confirms**. Then Phase 2b (first command flows: FR-10/11/13/14) after `FC-CMD` + `FC-CNC`. Then Phase 2c (high-impact + audit + recovery) after the missing components or approved fallbacks. A story promotes to `ready` only when its `blockedBy` set empties OR a Product/UX-approved fallback is recorded.

---

## 11. Open UX questions / explicit gaps deferred to UX

1. **Command endpoint route** — `POST /api/v1/commands` vs. `/api/commands` alias. *(critical path)*
2. **FrontComposer component gaps** — secure Product/UX approval for the **flat-audit-list** and **inline-consequence-preview** fallbacks, or schedule `<AuditTimeline>`/`<ConsequencePreview>` (and `FC-CNC`). *(critical path)*
3. **Layout contract (`FC-LYT`)** — full-width vs constrained shell/surface layout; **gates even the read-only MVP**. *(critical path)*
4. **Localization resource ownership** — shared shell resources vs. Tenants-owned keys + adopter terminology.
5. **WCAG 2.2 AA** — what the pinned Fluent version actually supports (conditional target).
6. **RTL support** — in or out for v1? (no spec commits.)
7. **Cursor durability across replicas/restarts** — UI's expected behavior on cursor invalidation (treat cursors as opaque, session-scoped for now).
8. **Consequence Preview scope for config edits (FR-16)** — always required, or only a high-risk key subset? (also a phasing lever.)
9. **Audit area in MVP** — hide it, or show a "not yet available" placeholder?
10. **Freshness thresholds** — numeric `current`/`aging`/`stale` cutoffs need product input.
11. **Sensitive configuration values** — if/when to display, and under what authorization (out of read MVP).
12. **Source-spec ID-scheme correction** — several UI specs wrongly say tenant/user ids are ULIDs; authoritative rule is **caller-supplied strings**. Affects the "copy full id" affordance (FR-7) — never `Guid.TryParse`/`Ulid.TryParse` a `TenantId`/`UserId`.
13. **Owner self-service depth** — v1 is "honest-minimal" (shared surfaces, no owner-only screens); confirm when/whether dedicated owner journeys/UX become funded scope.

Additional reconciliation gaps: dedicated tenant-owner screens/onboarding are a downstream-UX gap; FR-22/24/25 lack a backlog row + backend evidence; canonical state-name drift must resolve before propagating the truth-state vocabulary; undefined glossary terms (`pending`/`pending state` as a column; the four audit-availability states); an unattached latency budget on the ~500-event target.

---

## 12. Design-constraining tech

- **Microsoft Fluent UI Blazor v5** is the visual authority and component source — pinned at **`5.0.0-rc.3-26138.1`** (PRD addendum §D). **Hard constraints:** no separate branded palette; meaning maps to **semantic theme roles, never hard-coded hex**; "exact component/token/ARIA behavior must be verified against the pinned package at implementation time; **do not assert a token name as available.**" WCAG 2.2 AA is conditional on what this version supports.
  - ⚠ Version note: the FrontComposer `project-context.md` (2026-05-10) records Fluent at `5.0.0-rc.2-26098.1`; the newer PRD addendum records `5.0.0-rc.3-26138.1`. Verify the actual pinned version at implementation time.
- **Eventual-consistency / Blazor-Auto contract** — the UI **re-queries to confirm against the projection**, treats SignalR as **nudges only**, and is "correct under at-least-once delivery and projection lag" (NFR-3, CP-3, CP-4). **Hard constraint:** never advance command/audit state from a live signal or optimistic UI; success only after projection confirmation. (The literal phrases "Fluxor state" / "Blazor Auto prerender/circuit" are not used in the read PRD files — the constraint is expressed as projection-source-of-truth + SignalR-nudge.)
- **FrontComposer framework** — the UI *composes* FrontComposer; **generated composition is limited to low-risk read-only projection-backed surfaces; all command/mutation flows are custom flows, never generated CRUD** (reconcile-operations-shell GAP-6). Missing shared capability must be built in FrontComposer, not Tenants.
- **Backend contract (consume-only):** read queries (`ListTenantsQuery`, `GetTenantQuery`, `GetTenantUsersQuery`, `GetUserTenantsQuery`, `GetTenantAuditQuery`) and commands via the command endpoint. **No new backend endpoints** — receipts/previews/command-status assembled **client-side from already-loaded projection/read-model fields**. Cursor pagination only (signed, opaque, scope-bound; never offset/limit).
- **Freshness primitive:** conditional requests (`If-None-Match` → `304`) via the caching projection actor; the Truth State Badge derives `current/refreshing/aging/stale/unknown` from timestamp / projection version / ETag evidence (`unknown` when none can be measured). Numeric thresholds deferred (#10).
- **Identity:** actor identity from JWT `sub` / envelope `UserId`; **tenant ids and user ids are meaningful caller-supplied strings, NOT ULIDs** — a hard constraint on the copy-id affordance and any parsing logic.

---

**Key cross-cutting caveat:** This is a *trust-first operations console* whose entire visual + experiential thesis is **honesty about state** (never fake success, never collapse distinct states, always surface freshness, correct-forward-never-undo). Visual identity is **constrained to Fluent semantic roles + a calm operations-console tone, with no bespoke brand to invent** — the open visual decision is principally the `FC-LYT` layout contract. Nothing is buildable until the §16.2/§16.3 FrontComposer-readiness decisions are made — the design spines should be authored as a plan, not as a green light.
