# Tenants UI Responsive Operational Layout and Visual System Specification

Owner: Hexalith.Tenants product and UX planning
Status: Phase 2 planning/readiness artifact (planning-only)
Last reviewed: 2026-06-05
Story: 9.6 — Specify Responsive Operational Layout and Visual System Usage

This document is the **cross-cutting responsive-layout and visual-system usage contract** for the Phase 2 Tenants Admin UI. It defines how the already-specified semantic states (Story 9.3) and information architecture (Story 9.2) are rendered: which meaning maps to which semantic role (never a hard-coded color), how typography and density behave on dense operational screens, how command/status layout stays stable and proximate, how layout responds across desktop, tablet, and mobile, the breakpoint set, and how a DataGrid preserves safety-critical state at narrow widths. Every Phase 2 UI implementation story must follow these rules so that dense access-review workflows remain usable **without sacrificing truth or context**.

## 2026-06-05 Status Supersession

Story 1.0 completed FrontComposer shell-integration verification on 2026-06-05; see `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`. That spike confirms `FC-LYT`, so older text below that describes the layout contract as `needs-confirmation` is superseded. Story 1.2 resolved the Epic 1 `FC-TBL` path with Tenants-specific grid/table composition, while keeping reusable generic grid capability as a FrontComposer concern. The responsive and visual rules remain binding, and implementing stories still must verify exact shell behavior and responsive evidence at build.

This spec **maps** existing semantic states to visual roles and layout. It does **not** redefine, rename, or re-enumerate the badge states, freshness/lifecycle/reason/feedback sets, or navigation model owned by Stories 9.1–9.3. Layout behavior maps primarily to `FC-LYT`; the visual semantic system maps primarily to `FC-TOK`. Accessibility, localization, and the responsive acceptance/test evidence matrix are deferred to Story 9.7.

## Scope and Boundary (read first)

- **Planning/specification only.** Epic 9 is readiness/planning-only. This story produces a responsive-layout and visual-system usage specification, not shippable Admin UI. It must not route a Developer agent into product UI delivery until separate Phase 2 implementation stories are created from this spec. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`]
- **This document does NOT** create UI components, Blazor pages/routes/layouts, CSS/theme files, design tokens, backend endpoints, commands, queries, package references, generated FrontComposer files, domain-contract annotations, generated artifacts, Phase 1 release gates, or submodule pointer changes. The custom components named here (Truth State Badge, Freshness Gate, Unavailable Action Reason, Command Lifecycle Panel, Consequence Preview, Audit Evidence Receipt, Flat Audit List Fallback, and the Operations Shell layout) are **governed, not implemented**. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.6`]
- **Backend MVP work stays independent of UI dependency readiness.** Missing UI dependencies block or defer future Phase 2 UI rows; they never block backend package/release work. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- **Reconcile, do not duplicate.** Dependency IDs and per-row `blockedBy` arrays are owned by `docs/tenants-ui-frontcomposer-dependency-map.md` and `docs/tenants-ui-phase-2-story-backlog.md`. This spec references and copies them verbatim; it does not redefine `FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC`, and it invents no new dependency IDs, column names, or `ui-NN` keys. [Source: Story 9.1 senior review; `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`]

### Why a new artifact

No responsive/visual-system pattern specification existed in `docs/` before this story; the directory held the dependency map (9.1), the operations-shell spec (9.2), the truth-state/action-availability spec (9.3), the remove-user journey spec (9.4), the audit-evidence/recovery spec (9.5), and the Phase 2 backlog. None of those defines the **cross-cutting layout (`FC-LYT`) and visual-semantic (`FC-TOK`) rules** that every screen must follow. This spec adds that rule layer on top of the existing source-of-truth artifacts and points back to them for all semantic-state vocabulary, navigation, dependency readiness, and `blockedBy` data. It adds a presentation-rule layer rather than a parallel dependency map.

## This spec composes existing patterns — it does NOT redefine them

- **Story 9.3** (`docs/tenants-ui-truth-state-and-action-availability-spec.md`) is the canonical owner of the truth-state badge vocabulary (§2.1/§2.2), the non-color-only presentation requirement (§2.3), the freshness/lifecycle/unavailable-reason/feedback state sets, the fail-closed rule (§3.3), the "keep unavailable high-impact actions visible" rule (§4.3), and the feedback-placement/proximity contract (§6.1/§6.2). This spec maps those semantic states to visual roles and layout; it must stay consistent and must not rename, re-enumerate, or contradict them.
- **Story 9.2** (`docs/tenants-ui-operations-shell-spec.md`) owns the operations navigation model, now superseded by Epic 1 implementation to primary Tenants, Global Administrators, and Audit, with Users contextual. The tenant list remains the default triage surface, command lifecycle is never a separate primary navigation model, and `loading` must stay layout-stable (§1, §2.4). Responsive collapse rules here preserve that context-preservation contract; they do not introduce a new navigation model.
- **Story 9.1** (`docs/tenants-ui-frontcomposer-dependency-map.md`) owns the 10 fixed dependency IDs. `FC-LYT` (layout/responsive) is confirmed by Story 1.0. `FC-TOK` (semantic tokens) is still missing for timeline/consequence tokens; existing Fluent/FrontComposer status/role **badges** (`.../Components/Badges`) are adjacent evidence usable only as a named fallback. Reuse these IDs verbatim; add none.
- **Story 9.7** (`9-7-define-accessibility-localization-and-ui-acceptance-evidence`, still `backlog`) owns the formal WCAG baseline (2.1 AA / 2.2 AA target), accessibility/localization proof, the responsive **testing** matrix, and UI acceptance scenarios. This spec sets the layout/visual **rules** (including the breakpoint set and the no-color-only invariant) but **defers the formal acceptance/evidence matrix and accessibility proof requirements to 9.7**. Keep them consistent (UX-DR65–UX-DR80). [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.7`]

### Numbering hazard (read carefully)

This story's key is the sprint-status Epic 9 UI-planning key `9-6-specify-responsive-operational-layout-and-visual-system-usage`. The dependency map and backlog cite a **separate Phase 2 backend/FrontComposer backlog** that also uses `9-x`/`10-x`/`11-x`/`12-x` keys (e.g. `FC-LYT`/`FC-TOK` sourced from `12-1-frontshell-dependency-map-for-tenants-ui` and `12-2-audit-timeline-and-consequence-preview-readiness`; backend evidence such as `10-1-optimistic-concurrency-for-tenant-read-model-writes`). Those are NOT this epic's stories. Do not conflate the two namespaces or mark any backend/FrontComposer row complete based on this UI planning story. [Source: `docs/tenants-ui-phase-2-story-backlog.md`; `docs/tenants-ui-frontcomposer-dependency-map.md`; `_bmad-output/implementation-artifacts/sprint-status.yaml`]

## 1. Visual System: Semantic Roles, Not Hard-Coded Colors (AC1)

The visual system maps **meaning to semantic roles** — Fluent UI theme tokens / semantic color roles supplied by the active theme — never to literal hex/RGB values. Tenants follows Microsoft Fluent UI as the visual authority and introduces **no separate branded palette** for Phase 2. The six meaning dimensions and their state names are owned by the UX Color System and Story 9.3 §2; they are referenced here, not re-enumerated or renamed. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Color System`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §2.1/§2.2]

### 1.1 Meaning dimensions mapped to semantic roles (referenced, not redefined)

Each tenant-specific meaning dimension maps to a semantic role, never a hard-coded color literal. The state names below are owned by the UX Color System and Story 9.3 §2 — this table records the mapping intent only.

| Meaning dimension | Owned state vocabulary (referenced) | Visual rule |
| --- | --- | --- |
| Tenant status | active, disabled, degraded | Map to neutral/accent/warning semantic roles; never a custom palette. |
| Projection freshness | current, updating, delayed, unable to verify (Story 9.3 freshness set: `current`/`refreshing`/`aging`/`stale`/`unknown`) | Freshness role + text + icon; `unknown`/`unable to verify` must read as not-confirmed, never as success. |
| Command lifecycle | request sent, change pending, access updated, rejected, already applied, needs follow-up (Story 9.3 lifecycle set) | Lifecycle role + text; never collapse accepted/projected/proven into one success color (Story 9.3 §5.2 non-collapse invariant). |
| Authorization state | available, unavailable, missing permission, blocked by stale data | Authorization role + visible inline reason (Story 9.3 §4); never color-only. |
| Audit evidence | available, delayed, unavailable | Audit role + text; distinguish delayed from unavailable (Story 9.3 §6.3). |
| Risk state | last-owner warning, global-administrator risk, destructive action | Warning/destructive role used **sparingly**; risk must be perceivable without color. |

Primary actions use the Fluent brand/accent treatment supplied by the active theme; secondary, subtle, and transparent actions preserve Fluent hierarchy rather than adding custom button colors. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Color System`]

### 1.2 No-color-only invariant (core AC1 rule)

Every state pairs the semantic color with **text plus an icon or shape** so it is perceivable in light, dark, high-contrast, and **forced-colors** contexts and by users who cannot perceive color. **Color supports meaning; it never carries it alone.** This is the most common implementation failure mode (color-only status), and it is forbidden. Warning and destructive treatments are used sparingly so high-impact access changes stay visible without making the whole interface feel alarming. Non-color-only treatment ties to `FC-TOK` (semantic tokens) and `FC-A11Y` (forced-colors/contrast); formal accessibility evidence is deferred to Story 9.7. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Color System`; `#Accessibility Considerations`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §2.3]

### 1.3 `FC-TOK` readiness truth (do not assert a token name as ready)

- Role/status semantics may reuse **existing Fluent/FrontComposer status/role badges** (`Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Badges`) **only when a consuming story names that as the fallback**.
- Timeline-connector and consequence-severity tokens are `missing` and remain blockers for polished audit/consequence visuals until tokens are confirmed or product/UX approves a named fallback.
- Exact Fluent UI Blazor v5 component APIs and token names must be verified against the pinned package (`Microsoft.FluentUI.AspNetCore.Components 5.0.0-rc.3-26138.1` in `Hexalith.FrontComposer/Directory.Packages.props`) and current official docs during implementation. Stable v4, React Fluent, or legacy `@hexalith/ui` aliases are not sufficient evidence. **Do NOT assert a specific Fluent token name as available here.** [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#FC-TOK`; `#Fluent UI API Verification Prerequisite`]

## 2. Typography and Layout for Dense Operational Screens (AC2)

### 2.1 System typography

Operational screens use professional, calm, precise **system UI typography** following the Fluent typographic approach: the platform/system stack led by Segoe UI where available, falling back to standard sans-serif fonts. Typography prioritizes scanning, comparison, and accurate status interpretation over expressive branding — this is an administration surface for access decisions and audit evidence, not a marketing surface. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Typography System`]

### 2.2 Modest type hierarchy

The type hierarchy is modest:

- Page titles identify the current tenant, user, or operational scope.
- Section headings separate access, configuration, command status, and audit evidence.
- Table text stays compact and readable for long sessions.
- Status labels and helper text use **plain language**, not event-sourcing vocabulary.
- Confirmation and risk text is slightly more prominent than ordinary helper text but **not oversized**.

Avoid hero-scale type except where no operational content competes for attention. Dense tables, dialogs, command previews, and audit surfaces use compact headings sized to their containers. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Typography System`]

### 2.3 Compact density and stable layout

- Use a Fluent-compatible spacing rhythm: a **4px base** with common **8px, 12px, 16px, 24px, 32px** steps.
- Use **full-width operational surfaces with constrained readable inner regions** where needed.
- Prefer **tables, split views, tabs, side panels, dialogs, and inline status regions** over **decorative card grids**.
- Whitespace groups meaning, not drama.

**Explicitly forbidden for dense access-review workflows:** marketing-style card dashboards and hero-scale type. Tenants is an operational console, not a marketing site or card dashboard. Layout-variant decisions (full-width vs constrained) tie to the Story 1.0-confirmed `FC-LYT` contract. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Spacing & Layout Foundation`; `#Implementation Approach`; `docs/tenants-ui-frontcomposer-dependency-map.md#FC-LYT`]

## 3. Command/Status Layout Stability and Proximity (AC3)

### 3.1 Stable dimensions prevent layout shift

Status chips, action cells, toolbars, and command lifecycle regions keep **stable dimensions** so lifecycle transitions (e.g. request sent → change pending → access updated/rejected) do **not** cause layout shift, reflow, or row jumping. Loading/skeleton states must **reserve space and keep layout stable**, consistent with the read-only `loading` rule (Story 9.2 §2.4: show what is being loaded; keep layout stable). [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Spacing & Layout Foundation`; `docs/tenants-ui-operations-shell-spec.md` §2.4]

### 3.2 Proximity to context

Command controls, status chips, and lifecycle/feedback panels stay **close to the affected tenant, user, role, or audit context** (row-anchored or panel-anchored), reusing the Story 9.3 feedback-placement contract: proximity placement (§6.1); global message bars (`FluentMessageBar`) reserved for page-level degradation or system-wide service state only (§6.2). Command lifecycle is shown inside the affected workflow, **never promoted to a separate primary navigation area** (Story 9.2 §1.2). [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §6.1/§6.2; `docs/tenants-ui-operations-shell-spec.md` §1.2; `_bmad-output/planning-artifacts/ux-design-specification.md#Spacing & Layout Foundation`]

## 4. Responsive Behavior Across Desktop, Tablet, and Mobile (AC4)

### 4.1 Desktop-first operational strategy

Desktop/laptop is the **primary admin-workstation layout**: dense tables, persistent shell navigation, detail panels, member tables, command context, and audit evidence, optimized for fast scanning, keyboard use, side-by-side context, and stable row actions. Responsive behavior prevents breakage on smaller screens, but the first design target is keyboard-and-mouse workstation usage. [Source: `_bmad-output/planning-artifacts/epics.md` UX-DR65; `_bmad-output/planning-artifacts/ux-design-specification.md#Spacing & Layout Foundation`; `#Responsive Strategy`]

### 4.2 Tablet behavior

Tablet layouts may **collapse navigation, stack detail regions, and preserve table usability through horizontal scroll, column prioritization, or row-detail expansion** — without hiding critical state. Touch targets remain large enough, but the product is not redesigned around gesture-heavy workflows. [Source: `_bmad-output/planning-artifacts/epics.md` UX-DR66; `_bmad-output/planning-artifacts/ux-design-specification.md#Responsive Strategy`]

### 4.3 Mobile behavior

Mobile is **limited support for read-only triage, lookup, and audit reference review only**. The interface should not break on small screens, but mobile is not the primary target for the first slice and carries no high-impact command flows. [Source: `_bmad-output/planning-artifacts/epics.md` UX-DR67; `_bmad-output/planning-artifacts/ux-design-specification.md#Responsive Strategy`]

### 4.4 Fail-closed rule (CRITICAL — truth over compactness)

High-impact / destructive access changes must **fail closed or become unavailable** when the full safety context — freshness, consequence preview, authority, and audit expectation — cannot be preserved at the current width. Responsive behavior **prioritizes truth and context over visual compactness**; it never strips safety context to fit a viewport. This reuses the Story 9.3 fail-closed rule (§3.3) and the keep-unavailable-high-impact-actions-visible rule (§4.3): **surface the unavailable-action reason; do not silently hide the action**. Mobile is read-only triage/lookup/audit reference only — no high-impact command flows. [Source: `_bmad-output/planning-artifacts/epics.md` UX-DR67/UX-DR68; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §3.3/§4.3]

## 5. Breakpoint Set and DataGrid Critical-State Preservation (AC5)

### 5.1 Breakpoints (verbatim)

The four breakpoints are, verbatim:

- **Mobile: 320–767px**
- **Tablet: 768–1023px**
- **Desktop: 1024px and above**
- **Wide desktop: 1440px and above**

These are the layout rule. [Source: `_bmad-output/planning-artifacts/epics.md` UX-DR69]

### 5.2 DataGrid critical-state preservation

At narrow widths, **horizontal scroll or column priority preserves critical state** — tenant/user identity, status, freshness, role, and risk — instead of hiding it. Column dropping must **never** remove a safety-critical column at narrow widths; prefer horizontal scroll or a row-detail expansion. This composes the Story 9.2 invariant that sort and pagination must never hide pending or stale-state indicators. [Source: `_bmad-output/planning-artifacts/epics.md` UX-DR66/UX-DR69; `_bmad-output/planning-artifacts/ux-design-specification.md#Search, Filtering, and Table Patterns`; `docs/tenants-ui-operations-shell-spec.md` §2.5]

### 5.3 Responsive testing matrix is a Story 9.7 deliverable (referenced, not duplicated)

The responsive **testing** widths — desktop 1024/1366/1440/wide, tablet 768/1024, mobile 375/430, plus horizontal table overflow, navigation collapse, and command preview/dialog behavior at narrow widths — are the **Story 9.7 evidence matrix** (UX-DR78). This spec states the rules; Story 9.7 owns the acceptance/test evidence. Do not duplicate the testing widths as acceptance evidence here. [Source: `_bmad-output/planning-artifacts/epics.md` UX-DR78; `#Story 9.7`]

## 6. Per-Pattern Consumption Mapping (copy `blockedBy` verbatim; do not re-derive)

Layout (`FC-LYT`) and the visual semantic system (`FC-TOK`) are cross-cutting across the candidate rows. Each `blockedBy` array below is copied **verbatim** from `docs/tenants-ui-phase-2-story-backlog.md`. No row becomes implementation-ready in this story; every consuming row stays `planning-only` or `blocked`. No new dependency IDs, column names, or `ui-NN` keys are introduced.

### 6.1 Layout/responsive (`FC-LYT`) — cross-cutting across all rows `ui-01`…`ui-15`

Closed by Story 1.0: the full-width/constrained screen layout contract is confirmed for implementation use. Older `blockedBy` arrays that include `FC-LYT` are retained as historical planning data but no longer mean the layout contract is unknown.

### 6.2 Visual semantic system (`FC-TOK`) — cross-cutting across the token-consuming rows

Deferred Decision: "Confirm semantic role/status/timeline/consequence token usage" (affected rows `ui-02`, `ui-03`, `ui-04`, `ui-06`, `ui-07`, `ui-09`, `ui-10`, `ui-11`, `ui-12`, `ui-13`, `ui-14`, `ui-15`). `ui-01` and `ui-05` do not consume `FC-TOK`.

### 6.3 Consuming rows with verbatim `blockedBy`

| Backlog row | Readiness | `FC-LYT` | `FC-TOK` | `blockedBy` (verbatim) |
| --- | --- | --- | --- | --- |
| `ui-01-tenant-list-read-only` | `planning-only` | yes | no | `[FC-LYT, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-02-my-tenants-and-user-search-read-only` | `planning-only` | yes | yes | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-03-tenant-detail-overview-read-only` | `planning-only` | yes | yes | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-04-user-management-member-table` | `planning-only` | yes | yes | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-05-tenant-configuration-read-only` | `planning-only` | yes | no | `[FC-LYT, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-06-global-admin-read-only` | `planning-only` | yes | yes | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-07-create-tenant-command` | `planning-only` | yes | yes | `[FC-LYT, FC-CMD, FC-CNC, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-08-edit-tenant-metadata-command` | `planning-only` | yes | no | `[FC-LYT, FC-CMD, FC-CNC, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-09-user-management-add-or-change-role` | `planning-only` | yes | yes | `[FC-LYT, FC-CMD, FC-CNC, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-10-tenant-configuration-edit` | `blocked` | yes | yes | `[FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-11-audit-trail-flat-timeline` | `blocked` | yes | yes | `[FC-LYT, FC-AUD, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-12-tenant-detail-audit-tab` | `blocked` | yes | yes | `[FC-LYT, FC-AUD, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-13-disable-or-enable-tenant` | `blocked` | yes | yes | `[FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-14-user-management-remove-user` | `blocked` | yes | yes | `[FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-15-global-admin-command-management` | `blocked` | yes | yes | `[FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |

`ui-08` carries `FC-LYT` but not `FC-TOK` (its `blockedBy` has no `FC-TOK`), so it is governed by the layout rules of this spec but is not a `FC-TOK` token-consuming row. The `FC-TOK` row set in §6.2 (`ui-02`/`ui-03`/`ui-04`/`ui-06`/`ui-07`/`ui-09`/`ui-10`/`ui-11`/`ui-12`/`ui-13`/`ui-14`/`ui-15`) is the authoritative token-consuming set. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`; `#Deferred Decisions`; `#Readiness Order`]

### 6.4 Custom components this spec governs but does NOT implement

The visual semantic-role/status treatment and responsive layout behavior of the following components are governed here, not implemented or redefined: Truth State Badge, Freshness Gate, Unavailable Action Reason, Command Lifecycle Panel, Consequence Preview, Audit Evidence Receipt, and Flat Audit List Fallback (referenced from Stories 9.2/9.3/9.5), plus the Operations Shell and all read-only/command surfaces. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Custom Components`; `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`]

## 7. Implementation Story Rules

A future Phase 2 UI story may apply these patterns only when it can satisfy **all** of the following (it fails closed otherwise). These are acceptance criteria for the UX promise, not implementation preferences. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Story Rules`]

1. **Semantic-role color treatment** for every state — no hard-coded color literal as a contract.
2. **No-color-only encoding** verified in forced-colors mode (text + icon/shape for every state).
3. **Compact density / system typography / no decorative card grids** for dense operational workflows.
4. **Stable dimensions** (no layout shift) for status chips, action cells, toolbars, and lifecycle panels.
5. **Proximity** of command controls, status chips, and lifecycle/feedback panels to the affected context.
6. **Desktop-primary responsive layout** with tablet collapse and mobile read-only triage/lookup/audit reference.
7. **Fail-closed high-impact actions** when full safety context cannot be preserved at the current width.
8. **The four breakpoints** (mobile 320–767px, tablet 768–1023px, desktop 1024px+, wide desktop 1440px+).
9. **DataGrid critical-state preservation** via horizontal scroll or column priority — never drop a safety-critical column.

Accessibility, localization, and acceptance **evidence** for these rules are owned by Story 9.7 (UX-DR70–UX-DR80).

### 7.1 First-slice scope boundary

Decorative dashboards, branded palettes, hero-scale type, grouped/advanced visual modes, and bespoke per-state color literals stay **out of the first slice** unless product/UX explicitly promotes them. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Scope Boundary`; `_bmad-output/planning-artifacts/ux-design-specification.md#Color System`]

## 8. Support-Safe, Accessibility, and Localization Contracts (shared with other Epic 9 stories)

- **Accessibility (ties to `FC-A11Y`; formal evidence deferred to Story 9.7):** no-color-only and forced-colors support, stable focus order, and keyboard reachability are accessibility expectations referenced from Story 9.3 §9.2 and UX-DR70. This spec does not redefine the WCAG baseline; it cites UX-DR70 and defers formal acceptance/evidence to Story 9.7. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §9.2; `_bmad-output/planning-artifacts/epics.md` UX-DR70]
- **Automation selectors (ties to `FC-A11Y`/`FC-DOC`):** automation of layout/state assertions must rely on **stable selectors or component contracts**, never arbitrary row text or color. [Source: `docs/tenants-ui-operations-shell-spec.md` §5.2; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §9.3]
- **Localization (ties to `FC-L10N`):** localize all status labels, role names, timestamps, risk warnings, disabled reasons, and breakpoint-dependent affordances; no runtime sentence-fragment assembly. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §9.4]

## 9. Backend and Data Boundaries

- This spec specifies **presentation rules only**. It adds NO backend endpoint, command, query, projection field, package reference, CSS/theme/token file, or generated artifact. Layout and visual treatment compose over the already-specified read endpoints (`GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, `GET /api/tenants/{tenantId}/audit`) and the command endpoint (`POST /api/v1/commands` per FrontComposer `EventStoreOptions.CommandEndpointPath`; `POST /api/commands` recorded as an alias to confirm against the deployed gateway). [Source: `_bmad-output/project-context.md#API Surface`; `docs/tenants-ui-frontcomposer-dependency-map.md#Command Endpoint Route Evidence`]
- Freshness/lifecycle/audit visuals reflect read-model evidence and client-tracked command lifecycle only; SignalR is a freshness nudge, never proof. Visual "success" treatment must obey the Story 9.3 non-collapse invariant — never render confirmed-success styling before projection truth confirms. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §3.4/§5.2; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- Identity/rejection sanitation applies to any visible label: never render raw payloads, bearer tokens, stack traces, internal correlation IDs, internal exception text, or PII. User-facing rejection text is composed at the HTTP boundary by EventStore's domain-rejection ProblemDetails handling/catalog (RFC 7807). Tenant IDs and user IDs are literal caller-supplied strings, not ULIDs or GUIDs; use JWT `sub` for actor identity. [Source: `_bmad-output/project-context.md#API Surface`; `#Rejection Event Payloads`; `#Identity Scheme`; `#Logging & Telemetry`]

## 10. Acceptance Criteria Traceability

| AC | Requirement | Spec section |
| --- | --- | --- |
| AC1 | Tenant status, freshness, command lifecycle, authorization, audit evidence, and risk map to semantic roles, not hard-coded colors; every state understandable without color alone (forced-colors safe); `FC-TOK` readiness truth recorded | §1 |
| AC2 | Professional/calm/precise system typography, modest hierarchy, plain-language labels, compact 4px-rhythm density; tables/split views/tabs/side panels/dialogs/inline status over decorative card grids; no hero-scale type for dense workflows | §2 |
| AC3 | Stable dimensions for chips/cells/toolbars/lifecycle panels prevent layout shift (loading reserves space); command controls stay close to affected tenant/user/role/audit context; lifecycle inside the workflow | §3 |
| AC4 | Desktop-primary workstation layout; tablet collapses navigation and stacks regions; mobile read-only triage/lookup/audit reference only; high-impact actions fail closed or become unavailable when full safety context cannot be preserved | §4 |
| AC5 | Breakpoints (mobile 320–767px, tablet 768–1023px, desktop 1024px+, wide desktop 1440px+); DataGrid horizontal scroll or column priority preserves critical state instead of hiding it; Story 9.7 owns the testing matrix | §5 |

## 11. References

- `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`
- `_bmad-output/planning-artifacts/epics.md#Story 9.6: Specify Responsive Operational Layout and Visual System Usage`
- `_bmad-output/planning-artifacts/epics.md#Story 9.7: Define Accessibility, Localization, and UI Acceptance Evidence`
- `_bmad-output/planning-artifacts/epics.md` UX-DR65, UX-DR66, UX-DR67, UX-DR68, UX-DR69, UX-DR70, UX-DR78, UX-DR79, UX-DR80
- `_bmad-output/planning-artifacts/ux-design-specification.md#Color System`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Typography System`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Spacing & Layout Foundation`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Accessibility Considerations`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Approach`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Search, Filtering, and Table Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Responsive Strategy`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Story Rules`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Custom Components`
- `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`
- `docs/tenants-ui-truth-state-and-action-availability-spec.md` (§2.1, §2.2, §2.3, §3.3, §3.4, §4.3, §5.2, §6.1, §6.2, §9.2, §9.3, §9.4)
- `docs/tenants-ui-operations-shell-spec.md` (§1, §1.2, §2.4, §2.5, §5.2)
- `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog` (`FC-LYT`, `FC-TOK`); `#Fluent UI API Verification Prerequisite`; `#Command Endpoint Route Evidence`
- `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories` (`ui-01`…`ui-15`); `#Deferred Decisions`; `#Readiness Order`; `#Scope Boundary`
- `_bmad-output/project-context.md#API Surface`
- `_bmad-output/project-context.md#Identity Scheme`
- `_bmad-output/project-context.md#Rejection Event Payloads`
- `_bmad-output/project-context.md#Logging & Telemetry`
