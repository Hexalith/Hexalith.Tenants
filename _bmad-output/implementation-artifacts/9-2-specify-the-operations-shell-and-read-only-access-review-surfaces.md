---
baseline_commit: 91f16d188f25746134fb96d824d4d92ef890feb4
---

# Story 9.2: Specify the Operations Shell and Read-Only Access Review Surfaces

Status: done

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. Adversarial senior review (2026-06-01) verified all ACs and source-of-truth claims; 3 documentation-fidelity issues found and auto-fixed (0 critical/high).

## Story

As a tenant administrator,
I want the Phase 2 UI information architecture to support tenant discovery and access review,
so that I can find tenants, inspect access, and reach audit evidence before command workflows are enabled.

## Acceptance Criteria

1. **Given** the Operations Shell is specified, **when** primary navigation is defined, **then** Tenants, Users, Global Administrators, and Audit are included as primary navigation areas, **and** command lifecycle is not promoted into a separate primary navigation model.
2. **Given** the tenant list is specified, **when** table behavior is documented, **then** it includes filter, search, sort, pagination, tenant status, member count, owner count, freshness, pending state, loading, empty, filtered-empty, error, stale, and degraded states, **and** sorting and pagination do not hide pending or stale-state indicators.
3. **Given** tenant detail and member access review are specified, **when** a user navigates from tenant list to detail, **then** tenant context is preserved across overview, members, configuration, command state, and audit evidence, **and** selected tenant and filters are preserved when returning to the list.
4. **Given** user lookup and global administrator surfaces are specified, **when** access questions begin with a user or platform role, **then** user lookup remains reachable from shell navigation and access-review contexts, **and** global administrator surfaces distinguish platform-level risk from ordinary tenant membership.
5. **Given** read-only UI implementation stories are drafted, **when** the read-only specification is used, **then** long tenant IDs, user IDs, and support-safe references remain visually truncated but accessible, **and** stable selectors or component contracts are required for automation instead of arbitrary row text.

## Tasks / Subtasks

- [x] Preserve the Epic 9 planning-only boundary before editing any artifact. (AC: 1-5)
  - [x] Confirm this story produces planning/specification documentation only — an Operations Shell + read-only access-review surface specification.
  - [x] Do NOT implement Tenants Admin UI screens, FrontComposer components, Blazor pages/routes, backend endpoints, commands, queries, package references, generated UI files, domain-contract annotations, or submodule pointer changes.
  - [x] Keep backend MVP work independent from UI dependency readiness; missing UI dependencies block or defer future Phase 2 UI rows, never backend package/release work. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`]
- [x] Reconcile with existing Epic 9 / Epic 12 planning artifacts instead of duplicating them. (AC: 1-5)
  - [x] Read `docs/tenants-ui-frontcomposer-dependency-map.md` (Read-Only Surface Consumption Map, Screen Dependency Matrix, Dependency ID Catalog) and reuse its dependency IDs and readiness verbatim — do not redefine `FC-TBL`, `FC-LYT`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`.
  - [x] Read `docs/tenants-ui-phase-2-story-backlog.md` and align surfaces to its candidate rows (`ui-01`…`ui-06`, `ui-11`/`ui-12`) and literal `blockedBy` arrays. Do not invent new dependency IDs or new column names.
  - [x] Decide output location: create a new `docs/tenants-ui-operations-shell-spec.md` (no shell/IA spec exists today). If you instead extend the dependency map, justify why a new artifact is not warranted and avoid contradicting the existing source-of-truth language.
- [x] Specify the Operations Shell information architecture and navigation. (AC: 1, 3, 4)
  - [x] Define primary navigation exactly as Tenants, Users, Global Administrators, Audit. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Navigation Patterns`]
  - [x] State that the tenant list is the default triage surface and that command lifecycle is shown inside the affected workflow, never as a separate primary nav area.
  - [x] Capture navigation rules: preserve selected tenant + filters on return from detail; keep tenant/user/role context visible during command preview; user lookup is secondary but reachable from shell navigation and access-review contexts; Audit is reachable from global nav, tenant rows, tenant detail, user lookup, and command result.
- [x] Specify the tenant list read-only surface and its full state set. (AC: 2)
  - [x] Document filter, search, sort, pagination, and the displayed columns: tenant status, member count, owner count, freshness, pending state.
  - [x] Enumerate distinct surface states: loading, empty, filtered-empty, error, stale, degraded — each with its own user-facing meaning per `#Empty, Loading, Stale, and Degraded States`.
  - [x] Require that sort and pagination never hide pending or stale-state indicators, and that row actions stay stable in width and placement.
  - [x] Bind the surface to `GET /api/tenants` projection data only (cursor-based pagination, signed opaque scoped cursors). Do not add offset/limit. [Source: `_bmad-output/project-context.md#API Surface`]
- [x] Specify tenant detail and member access-review context preservation. (AC: 3)
  - [x] Define tenant detail sections: overview, members, configuration (read-only), command state, audit evidence — with tenant context preserved across all of them.
  - [x] Require that selected tenant and list filters are preserved when returning to the list (Journey 1 → detail → list round trip).
  - [x] Member access review surfaces user, role, owner count, tenant status, and freshness without implying command completion or membership mutation. [Source: `#Journey 2: Access Review and Action Availability`]
- [x] Specify user lookup and global administrator read-only surfaces. (AC: 4)
  - [x] User lookup ("My Tenants" / user search) consumes `GET /api/users/{userId}/tenants`, stays reachable from shell nav and access-review contexts, and exposes authorization-safe empty/error states.
  - [x] Global administrator surface reviews platform administrator access and explicitly distinguishes platform-level governance risk from ordinary tenant membership (separate `global-administrators` domain). It must not be modeled as tenant membership. [Source: `_bmad-output/project-context.md#Identity Scheme`; `docs/tenants-ui-frontcomposer-dependency-map.md#Read-Only Surface Consumption Map`]
- [x] Specify identifier truncation, support-safe references, and automation selectors. (AC: 5)
  - [x] Long tenant IDs (ULIDs), user IDs, and references truncate visually but remain fully accessible (copyable, accessible name, no information loss for screen readers).
  - [x] Require stable selectors or component contracts for automation; forbid relying on arbitrary row text. Tie this to `FC-A11Y`/`FC-DOC` evidence.
  - [x] Support-safe references must never expose raw payloads, bearer tokens, stack traces, internal correlation IDs, or PII. [Source: `#Support-Safe References`; `_bmad-output/project-context.md#Logging & Telemetry`]
- [x] Record per-surface readiness, `blockedBy`, and future-story consumption. (AC: 1-5)
  - [x] For each specified surface, copy the corresponding `blockedBy` array from the backlog row (e.g. `ui-01`, `ui-02`, `ui-03`, `ui-04`, `ui-05`, `ui-06`) rather than re-deriving it.
  - [x] Keep every surface `planning-only` or `blocked`; none becomes implementation-ready in this story. Audit fallback surface stays `blocked`/`planning-only` per `ui-11`/`ui-12`.
  - [x] Confirm each surface names its source projection/query, freshness state, authorization/unavailable-action reason, accessibility behavior, and localization responsibility per `#Implementation Story Rules`.
- [x] Record implementation evidence and run documentation validation. (AC: 1-5)
  - [x] Run `git diff --check` after documentation edits.
  - [x] Confirm no source code, package versions, lockfiles, generated files, submodule pointers (`git diff --submodule=short` empty for all root submodules), backend story statuses, UI screens, endpoints, commands, queries, or Phase 1 release gates changed.
  - [x] Update this story's Dev Agent Record with files changed, validation performed, and unresolved decisions.

## Dev Notes

### Scope Guard (read first)

- Epic 9 is readiness/planning-only. This story produces an **Operations Shell + read-only access-review surface specification**, not shippable Admin UI. It must not route a Developer agent into product UI delivery until separate Phase 2 implementation stories are created from this spec. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`]
- Output is documentation only: information architecture, navigation contract, per-surface state specifications, and copy-forward readiness rows. It does NOT create UI components, Blazor pages/routes, backend endpoints, commands, queries, package references, generated files, or Phase 1 release gates. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.2`]
- Backend MVP stories remain unblocked by UI dependency readiness. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]

### Previous Story Intelligence (Story 9.1 — done)

- **Reconcile, do not duplicate.** Story 9.1's senior review penalized an unnecessary parallel artifact risk and reverted a wrong endpoint normalization. Reuse the dependency map and backlog as the copy-forward source of truth; this spec references their IDs and rows rather than redefining them. [Source: `_bmad-output/implementation-artifacts/9-1-map-fluent-ui-and-frontcomposer-dependencies-for-tenant-admin-screens.md#Senior Developer Review (AI)`]
- **Command endpoint route is `POST /api/v1/commands`**, not `/api/commands`. Story 9.1 review reverted a wrong normalization: FrontComposer `EventStoreOptions.CommandEndpointPath` defaults to `/api/v1/commands`; `project-context.md` records the unversioned `/api/commands` `CommandsController` as an *alias* to confirm against the deployed gateway, not to assume. This spec is read-only and should not introduce command rows, but if it cites command surfaces for navigation context, use `/api/v1/commands`. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Command Endpoint Route Evidence`]
- **Dependency ID catalog is fixed** at 10 IDs (`FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC`), each defined exactly once. Do not add new IDs. Verified FrontComposer source buckets exist for DataGrid, Rendering, Contracts/Rendering, Layout, EventStore, Lifecycle, State/PendingCommands, Services/Feedback, Badges, Resources; `AuditTimeline`/`ConsequencePreview`/Storybook are absent. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Fluent UI API Verification Prerequisite`]

### Navigation Contract (AC1, AC3, AC4)

- Operations Shell is the stable navigation model. Primary nav = **Tenants, Users, Global Administrators, Audit** — exactly these four. Tenant list is the default triage surface. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Navigation Patterns`]
- Tenant detail preserves tenant context across overview, members, configuration, command state, and audit evidence.
- Navigation rules to capture verbatim: preserve selected tenant + filters on return from detail; keep tenant/user/role context visible during command preview; user lookup secondary but reachable from shell nav; audit reachable from global nav, tenant rows, tenant detail, user lookup, command result; **command lifecycle is never a separate primary nav model** — show it inside the affected workflow.

### Read-Only Surface Specifications (AC2, AC4, AC5)

- DataGrid-backed patterns for tenant list, member table, user lookup, and flat audit fallback. Provide search/filter only when it operates on a trustworthy query/projection. [Source: `#Search, Filtering, and Table Patterns`]
- Tenant list columns/states (AC2): filter, search, sort, pagination, tenant status, member count, owner count, freshness, pending state; plus distinct loading, empty, filtered-empty, error, stale, degraded states. Sort/pagination must not hide pending or stale indicators; row actions stable in width/placement. [Source: `#Empty, Loading, Stale, and Degraded States`]
- Surface-to-backlog mapping (copy `blockedBy` verbatim from `docs/tenants-ui-phase-2-story-backlog.md`):
  - Tenant list → `ui-01-tenant-list-read-only`, `blockedBy: [FC-LYT, FC-A11Y, FC-L10N, FC-DOC]`.
  - User lookup / My Tenants → `ui-02-my-tenants-and-user-search-read-only`, `blockedBy: [FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]`.
  - Tenant detail overview → `ui-03-tenant-detail-overview-read-only`, `blockedBy: [FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]`.
  - Member table → `ui-04-user-management-member-table`, `blockedBy: [FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]`.
  - Configuration read-only → `ui-05-tenant-configuration-read-only`, `blockedBy: [FC-LYT, FC-A11Y, FC-L10N, FC-DOC]`.
  - Global admin read-only → `ui-06-global-admin-read-only`, `blockedBy: [FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]`.
  - Audit fallback (entry-point context only here) → `ui-11`/`ui-12`, `blockedBy: [FC-LYT, FC-AUD, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` — stays `blocked`/`planning-only`; do not claim an `<AuditTimeline>` component exists. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Read-Only Surface Consumption Map`]
- Generated FrontComposer composition is appropriate ONLY for low-risk, read-only, projection-backed surfaces where Tenants stays source of truth and the UI does not imply durable command success. Member/global-admin command actions are custom command flows, NOT generated CRUD. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Read-Only Surface Consumption Map`; `#Anti-Patterns to Avoid`]

### Identifiers, Support-Safe References, and Automation Selectors (AC5)

- Long tenant IDs (ULIDs, not GUIDs), user IDs, and references truncate visually but remain accessible: full value copyable, full accessible name, no screen-reader information loss. [Source: `#Search, Filtering, and Table Patterns`; `_bmad-output/project-context.md#Identity Scheme`]
- Require stable selectors / component contracts for automation; forbid arbitrary row-text targeting (ties to `FC-A11Y` and `FC-DOC`).
- Support-safe references never expose raw payloads, bearer tokens, stack traces, internal correlation IDs, or PII. [Source: `#Support-Safe References`; `_bmad-output/project-context.md#Logging & Telemetry`]

### Backend and Data Boundaries

- Future surfaces consume existing read endpoints only: `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, `GET /api/tenants/{tenantId}/audit`. Cursor-based pagination only — signed, opaque, scope-bound cursors; never offset/limit. ETag `If-None-Match` → 304 is the freshness primitive. [Source: `_bmad-output/project-context.md#API Surface`]
- Do not annotate or reshape immutable Tenants domain contracts for UI generation. SignalR projection notifications are refresh nudges only — never proof of command completion or projection consistency. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- Global administrators live in the separate `global-administrators` domain (singleton aggregate); do not model platform governance as tenant membership. [Source: `_bmad-output/project-context.md#Identity Scheme`]

### Truth-State Vocabulary (shared with Story 9.3)

- This spec defines navigation + read-only surfaces; the full truth-state badge vocabulary (current, refreshing, aging, stale, unknown, pending, etc.) and freshness gating are owned by Story 9.3. Reference the shared truth-state model but do not redefine its states here. Freshness markers shown on the tenant list/detail must use timestamp / projection version / ETag evidence; if freshness cannot be measured, state is `unknown`. [Source: `#Truth State Model`]

### Project Structure Notes

- Documentation outputs belong under `docs/` and this story's Dev Agent Record. No shell/IA spec exists yet (`docs/` has no `*shell*`/`*operation*` file), so a new `docs/tenants-ui-operations-shell-spec.md` is the natural home. Future Phase 2 UI assets belong in a dedicated FrontComposer adapter/UI project only after readiness conversion, not in backend packages. [Source: `_bmad-output/planning-artifacts/architecture.md#Asset Organization`]
- Preserve root-level submodule policy. Reading `Hexalith.FrontComposer` is allowed; never initialize nested submodules or run recursive submodule commands. [Source: `AGENTS.md#Submodule Policy`; `_bmad-output/project-context.md`]
- `_bmad-output/` is untracked — do not commit it in code PRs. Use central package management; add no package versions in this documentation story. [Source: `_bmad-output/project-context.md#BMAD Workflow`]

### Testing / Validation Requirements

- No source-code test suite is required: this story updates planning/specification documentation only.
- Required documentation validation:
  - `git diff --check`.
  - Manual AC checklist against this story (AC1 navigation set; AC2 tenant-list state set; AC3 context preservation both directions; AC4 user-lookup reachability + platform/membership distinction; AC5 truncation accessibility + stable selectors).
  - Per-surface consumption check: every surface names source projection/query, freshness state, authorization/unavailable-action reason, accessibility behavior, localization responsibility, and a literal `blockedBy` array copied from the backlog.
  - Change-boundary check confirming no source code, package files, generated artifacts, submodule pointers, backend endpoints, commands, queries, UI screens, or Phase 1 gates changed.

### References

- `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`
- `_bmad-output/planning-artifacts/epics.md#Story 9.2: Specify the Operations Shell and Read-Only Access Review Surfaces`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Navigation Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Search, Filtering, and Table Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Empty, Loading, Stale, and Degraded States`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Journey 1: Tenant Discovery and Triage`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Journey 2: Access Review and Action Availability`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Support-Safe References`
- `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`
- `docs/tenants-ui-frontcomposer-dependency-map.md#Read-Only Surface Consumption Map`
- `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`
- `_bmad-output/implementation-artifacts/9-1-map-fluent-ui-and-frontcomposer-dependencies-for-tenant-admin-screens.md`
- `_bmad-output/project-context.md#API Surface`

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — create-story context engine

### Debug Log References

- 2026-06-01 - Resolved BMAD create-story customization; loaded config, sprint status, project context, planning artifacts (epics, ux-design-specification, architecture), existing UI dependency docs, and previous Story 9.1 output.
- 2026-06-01 - Verified Story 9.2 target key from `sprint-status.yaml`: `9-2-specify-the-operations-shell-and-read-only-access-review-surfaces` (status `backlog`); epic-9 already `in-progress` (9.1 done), no epic status change required.
- 2026-06-01 - Confirmed no existing `docs/` shell/operations/IA spec; Story 9.2 produces a new specification artifact.
- 2026-06-01 - Captured navigation contract, tenant-list state set, detail context preservation, user-lookup/global-admin distinction, and ID-truncation/selector rules from UX spec; mapped each surface to backlog `blockedBy` arrays (`ui-01`…`ui-06`, `ui-11`/`ui-12`).
- 2026-06-01 (dev) - Implemented Story 9.2: authored `docs/tenants-ui-operations-shell-spec.md`. Re-read dependency map (Dependency ID Catalog, Read-Only Surface Consumption Map, Command Endpoint Route Evidence), Phase 2 backlog (candidate rows + literal `blockedBy` arrays), UX spec (Navigation Patterns; Search/Filtering/Table Patterns; Empty/Loading/Stale/Degraded States; Journeys 1-2; Truth State Model; Support-Safe References), and project-context (API Surface, Identity Scheme, Logging & Telemetry).
- 2026-06-01 (dev) - Documentation validation: `git diff --check` clean (exit 0); `git diff --submodule=short` empty for all five root submodules (exit 0). Working tree shows only the new `docs/` spec as a tracked-repo change; other changes are untracked `_bmad-output/` BMAD artifacts (story file + sprint-status self-tracking + story-automator log).
- 2026-06-01 (dev) - Verified all seven surface `blockedBy` arrays copied verbatim against backlog rows (`ui-01`, `ui-02`, `ui-03`, `ui-04`, `ui-05`, `ui-06`, `ui-11`/`ui-12`); no new dependency IDs or column names introduced.

### Completion Notes List

- Create-story context engine analysis completed for Story 9.2.
- Existing dependency map and Phase 2 UI backlog treated as copy-forward source of truth to prevent duplicate planning artifacts (carrying forward Story 9.1 review lessons).
- Story guidance explicitly blocks UI implementation, backend endpoint creation, package changes, and submodule pointer changes; output is an Operations Shell + read-only surface specification.
- ✅ Authored `docs/tenants-ui-operations-shell-spec.md` — a planning-only Operations Shell IA + read-only access-review surface specification. It defines the four-area primary navigation (Tenants, Users, Global Administrators, Audit), navigation/context-preservation rules, the tenant-list read-only surface with its full distinct-state set, tenant-detail context preservation, user-lookup + global-admin read-only surfaces, identifier truncation/support-safe references/automation-selector rules, a per-surface readiness + consumption table, backend/data boundaries, and an AC1-5 traceability table.
- ✅ Reconciled rather than duplicated: the spec references the dependency map and backlog for all dependency-ID readiness and copies each surface's `blockedBy` array verbatim; it adds an IA/navigation + read-only state layer that did not exist in `docs/`.
- ✅ AC checklist (manual): AC1 navigation set + command-lifecycle-not-primary; AC2 tenant-list capability/column/state set + sort/pagination-do-not-hide-pending/stale; AC3 context preservation both directions; AC4 user-lookup reachability + platform/membership distinction (separate `global-administrators` domain); AC5 ULID truncation accessibility + stable selectors + support-safe reference sanitization — all satisfied (see spec §9).
- All surfaces remain `planning-only` or `blocked`; none became implementation-ready. No command rows introduced (read-only spec); command endpoint route noted as `POST /api/v1/commands` per Story 9.1 evidence only for navigation context.
- Unresolved decisions: none introduced by this story. All open fallback/readiness decisions remain owned by the existing dependency map and backlog Deferred Decisions table.

### File List

- _bmad-output/implementation-artifacts/9-2-specify-the-operations-shell-and-read-only-access-review-surfaces.md (story tracking: tasks, Dev Agent Record, Senior Developer Review, status → done)
- docs/tenants-ui-operations-shell-spec.md (new — Operations Shell + read-only access-review surface specification; review auto-fixes: truth-state vocabulary §"Truth-State Vocabulary", §5.3 anchor, §4.2 backend keys)
- _bmad-output/implementation-artifacts/sprint-status.yaml (self-tracking: 9-2 status → done)

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — 2026-06-01 (story-automator adversarial review, auto-fix mode)
**Outcome:** Approve (status → done). 0 critical, 0 high, 1 medium, 2 low — all auto-fixed.

### Scope and method

Documentation-only story; the deliverable is `docs/tenants-ui-operations-shell-spec.md`. Review validated every claim in the spec against its cited source-of-truth artifacts and the actual codebase rather than trusting the story's self-report.

### Verified correct (no findings)

- **`blockedBy` arrays (8):** all match `docs/tenants-ui-phase-2-story-backlog.md` rows `ui-01`–`ui-06`, `ui-11`, `ui-12` verbatim.
- **Dependency IDs:** the 10 catalog IDs match `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`; no new IDs invented.
- **Query class names:** `ListTenantsQuery`, `GetTenantQuery`, `GetTenantUsersQuery`, `GetUserTenantsQuery`, `GetTenantAuditQuery` all exist under `src/Hexalith.Tenants.Contracts/Queries/`.
- **Endpoints, pagination shape, freshness primitive:** `GET` endpoints, `{ items, cursor, hasMore }` response shape, cursor-only/no-offset rule, and `If-None-Match → 304` served by `CachingProjectionActor` all match `_bmad-output/project-context.md#API Surface` verbatim.
- **Identity claims:** `global-administrators` separate domain + singleton aggregate ID, and ULID (not GUID) identifiers, match `project-context.md#Identity Scheme`.
- **Navigation set (AC1):** four-area primary nav and navigation rules match `ux-design-specification.md#Navigation Patterns` verbatim.
- **AC coverage:** AC1–AC5 all implemented with verified backing (see spec §9 traceability).
- **Git vs File List:** File List accurate; the untracked `_bmad-output/story-automator` orchestration log is a session artifact correctly excluded from the deliverable.

### Findings (auto-fixed)

1. **[MEDIUM] Truth-state vocabulary misnamed.** The Truth-State Vocabulary paragraph enumerated `blocked` and a standalone `pending` — neither is a literal state name in `ux-design-specification.md#Truth State Model` — and omitted canonical lifecycle/audit states, while claiming not to redefine the model. **Fix:** replaced the inline list with the model's canonical states grouped by dimension (freshness / command lifecycle / projection confirmation / audit evidence).
2. **[LOW] Misattributed cross-reference (§5.3).** `#Security & Sanitization` was cited as a bare anchor (this spec's convention is bare `#…` = UX spec), but that section exists only in `project-context.md`. **Fix:** qualified the anchor to `_bmad-output/project-context.md#Security & Sanitization`.
3. **[LOW] Imprecise backend story keys (§4.2).** Cited `11-1`, `11-2` instead of the full keys used in the `ui-06` backlog row. **Fix:** expanded to `11-1-production-jwt-configuration-validation` and `11-2-eventstore-tenant-claim-contract`.

### Post-fix validation

- `git diff --check` clean (exit 0).
- No source code, package, generated, submodule-pointer, backend-endpoint, command, query, or Phase 1 release-gate changes; the only tracked-repo change remains the new `docs/` spec.

## Change Log

| Date | Change |
| --- | --- |
| 2026-06-01 | Implemented Story 9.2 (planning-only). Authored `docs/tenants-ui-operations-shell-spec.md` defining the Operations Shell information architecture, navigation/context-preservation contract, tenant-list read-only surface + full state set, tenant-detail + member access-review context preservation, user-lookup + global-administrator read-only surfaces, identifier truncation / support-safe reference / automation-selector rules, and a per-surface readiness/`blockedBy` consumption table copied verbatim from the Phase 2 backlog. Ran documentation validation (`git diff --check` clean; no submodule pointer changes; no source/package/generated/backend changes). Story status → review. |
| 2026-06-01 | Senior Developer Review (AI), auto-fix mode. Verified all ACs and source-of-truth claims (blockedBy arrays, dependency IDs, query names in `src/`, endpoints, pagination/ETag, identity scheme, nav set) — all accurate. Found and auto-fixed 3 documentation-fidelity issues (1 medium: truth-state vocabulary canonicalized; 2 low: §5.3 anchor qualified to project-context, §4.2 backend keys expanded). 0 critical/high. `git diff --check` clean post-fix. Story status → done. |
