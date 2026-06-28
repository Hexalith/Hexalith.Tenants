# Tenants UI Operations Shell and Read-Only Access Review Specification

Owner: Hexalith.Tenants product and UX planning
Status: Phase 2 planning/readiness artifact (planning-only)
Last reviewed: 2026-06-01
Story: 9.2 — Specify the Operations Shell and Read-Only Access Review Surfaces

This document specifies the Phase 2 Admin UI **information architecture (Operations Shell)** and the **read-only access-review surfaces** that let a tenant administrator find tenants, inspect access, and reach audit evidence *before* command-capable workflows are enabled. It is the navigation and read-only surface contract that future Phase 2 implementation stories consume.

## 2026-06-06 / 2026-06-27 Implementation Supersession

Epic 1 implementation supersedes older planning assumptions in this document:

- **(2026-06-27, Correct Course Option A)** The shell left navigation exposes **one entry per module**. The single **Tenants** module entry opens the Tenants workspace, which groups Tenants-domain read surfaces as **page-local tabs**: first tab **Tenants** (list/triage), second tab **Users** (lookup/search-backed membership; `/tenants/my` and `/tenants/users` remain deep-link aliases that set the active tab). Global Administrators and Audit are reached through module-internal/contextual entry points, not separate Tenants left-menu entries. This supersedes the earlier "primary navigation: Tenants, Global Administrators, Audit; Users contextual" model recorded below.
- Tenant IDs and user IDs are literal caller-supplied strings. They are not assumed to be ULIDs or GUIDs and must not be parsed, normalized, or reformatted.

## Scope and Boundary (read first)

- **Planning/specification only.** Epic 9 is readiness/planning-only. This story produces an Operations Shell + read-only access-review surface specification, not shippable Admin UI. It must not route a Developer agent into product UI delivery until separate Phase 2 implementation stories are created from this spec. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`]
- **This document does NOT** create UI components, Blazor pages/routes, backend endpoints, commands, queries, package references, generated FrontComposer files, domain-contract annotations, generated artifacts, Phase 1 release gates, or submodule pointer changes.
- **Backend MVP work stays independent of UI dependency readiness.** Missing UI dependencies block or defer future Phase 2 UI rows; they never block backend package/release work. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- **Reconcile, do not duplicate.** Dependency IDs and per-surface `blockedBy` arrays are owned by `docs/tenants-ui-frontcomposer-dependency-map.md` and `docs/tenants-ui-phase-2-story-backlog.md`. This spec references and copies them verbatim; it does not redefine `FC-TBL`, `FC-LYT`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC`, `FC-CMD`, `FC-CNC`, `FC-AUD`, or `FC-CNS`, and it does not invent new dependency IDs or column names. [Source: Story 9.1 senior review; `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`]

### Why a new artifact

No shell/IA specification existed in `docs/` before this story (no `*shell*` or `*operation*` file). The dependency map sequences *screens against FrontComposer dependencies*; the backlog records *candidate story rows*. Neither defines the **navigation contract** or the **per-surface read-only state contract**. This spec fills that gap and points back to the two existing source-of-truth artifacts for all dependency readiness and `blockedBy` data, so it adds an information-architecture layer rather than a parallel dependency map.

## Truth-State Vocabulary (referenced, not redefined)

The full truth-state vocabulary — freshness (`current`, `refreshing`, `aging`, `stale`, `unknown`), command lifecycle (`eligible`, `previewed`, `submitted`, `accepted`, `rejected`, `already applied`, `failed`, `duplicate`, `timeout`, `unknown`), projection confirmation (pending confirmation vs. last-confirmed projection data), and audit evidence (`audit pending`, `audit available`, `delayed`, `unavailable`, `approved fallback`) — and freshness gating are owned by Story 9.3 and the UX spec Truth State Model. This spec references that shared model and does not redefine its states. Freshness markers on the tenant list and detail must use timestamp / projection version / ETag evidence available from the read model; if freshness cannot be measured, the state is `unknown`. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Truth State Model`]

The freshness primitive for every read surface in this spec is the `If-None-Match` → `304 Not Modified` ETag pre-check served by `CachingProjectionActor`. [Source: `_bmad-output/project-context.md#API Surface`]

## 1. Operations Shell Information Architecture and Navigation (AC1, AC3, AC4)

The Operations Shell is the stable navigation model. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Navigation Patterns`]

### 1.1 Primary navigation

Primary navigation areas are exactly these three, in this order:

1. **Tenants**
2. **Global Administrators**
3. **Audit**

The **tenant list is the default triage surface** — the landing surface of the shell. Command lifecycle is **not** promoted into a separate primary navigation area; it is shown inside the affected workflow. (AC1)

### 1.2 Navigation rules (capture verbatim)

- Preserve the selected tenant and active list filters when returning from tenant detail to the tenant list. (AC3)
- Keep tenant, user, and role context visible during command preview. (Forward-looking; this spec defines read-only context, command preview is a later story.)
- **User lookup is reachable** as the **Users** workspace tab and from access-review contexts (tenant detail member rows, audit results). It stays lookup/search-backed — not an exhaustive all-users inventory — and Tenants still contributes a single module left-menu entry (see the 2026-06-27 supersession note above). (AC4)
- **Audit is reachable from multiple entry points**: global navigation (the Audit area), tenant rows, tenant detail, user lookup, and command result. Audit is never the only way to reach proof, but it is always reachable from any access-review context.
- **Command lifecycle is never a separate primary navigation model.** Show it inside the affected workflow, close to the affected row or tenant context. (AC1)

[Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Navigation Patterns`]

### 1.3 Navigation map

| From | To | Context preserved |
| --- | --- | --- |
| Shell nav → Tenants | Tenant list (default triage) | n/a (entry surface) |
| Tenant list row → | Tenant detail | Selected tenant; list filters retained for return |
| Tenant detail → back | Tenant list | Selected tenant highlighted + filters restored (AC3) |
| Tenants context → Users | User lookup / My Tenants | Target user where supplied; no primary Users nav |
| Tenant detail member row → | User lookup (that user) | Tenant + user context |
| Shell nav → Global Administrators | Global admin read-only review | n/a |
| Shell nav → Audit | Audit surface (planning-only/blocked) | n/a |
| Tenant row / detail / user lookup / command result → | Audit (scoped context) | Tenant and/or user scope |

## 2. Tenant List Read-Only Surface (AC2)

Surface key: `ui-01-tenant-list-read-only`. The tenant list is the default triage surface and the entry point of Journey 1 (Tenant Discovery and Triage). [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Journey 1: Tenant Discovery and Triage`]

### 2.1 Source projection and data binding

- **Source query/endpoint:** `GET /api/tenants` (`ListTenantsQuery` projection). Read-only projection data only.
- **Pagination:** cursor-based only — signed, opaque, scope-bound cursors. List responses use `{ "items": [...], "cursor": "next-page-token", "hasMore": true }`. **Do not add offset/limit pagination.** [Source: `_bmad-output/project-context.md#API Surface`]
- **Freshness:** ETag `If-None-Match` → `304` is the freshness primitive; surface a freshness marker (timestamp / projection version / ETag). If freshness cannot be measured, state is `unknown`.

### 2.2 Capabilities

- **Filter, search, sort, pagination.** Provide search or filter only when it operates on a trustworthy query/projection. [Source: `#Search, Filtering, and Table Patterns`]

### 2.3 Displayed columns

- Tenant status
- Member count
- Owner count
- Freshness (freshness marker per the truth-state model)
- Pending state

### 2.4 Distinct surface states

Each state has its own user-facing meaning and copy; none may be collapsed into another. [Source: `#Empty, Loading, Stale, and Degraded States`]

| State | User-facing meaning |
| --- | --- |
| `loading` | Show what is being loaded; keep layout stable. |
| `empty` | No tenants exist; explain the absence without implying failure. |
| `filtered-empty` | No tenants match the current filter/search; offer a clear filter reset. |
| `error` | The list could not be loaded; offer retry without leaking backend detail. |
| `stale` | Projection is not current enough; show freshness marker and refresh path. |
| `degraded` | Some capability is unavailable; explain what is unavailable and what still works. |

### 2.5 Invariants

- **Sort and pagination must never hide pending or stale-state indicators.** Reordering or paging the list must keep the pending and stale markers visible for affected rows. (AC2)
- **Row actions stay stable in width and placement** as data, sort order, and page change.
- Status, freshness, and pending state must be perceivable **without color alone** (text label + icon). [Source: `#Accessibility`]

### 2.6 Readiness

- Readiness: `planning-only`.
- `blockedBy: [FC-LYT, FC-A11Y, FC-L10N, FC-DOC]` (copied verbatim from `ui-01-tenant-list-read-only`).
- Available basis: `FC-TBL` projection/DataGrid primitives (`available`). Layout variant, accessibility, localization, and component-reference evidence remain unresolved.

## 3. Tenant Detail and Member Access-Review Context Preservation (AC3)

Surface keys: `ui-03-tenant-detail-overview-read-only` (overview), `ui-04-user-management-member-table` (member table), `ui-05-tenant-configuration-read-only` (configuration). Tenant detail is reached from the tenant list and continues Journey 1 into Journey 2 (Access Review and Action Availability). [Source: `#Journey 2: Access Review and Action Availability`]

### 3.1 Tenant detail sections (context preserved across all)

Tenant context (selected tenant identity, status, freshness) is preserved across every section:

1. **Overview** — tenant status, metadata, member summary, configuration summary, navigation to deeper panels. Source: `GET /api/tenants/{tenantId}` (`GetTenantQuery`).
2. **Members** — member access-review table. Source: `GET /api/tenants/{tenantId}/users` (`GetTenantUsersQuery`).
3. **Configuration (read-only)** — namespace-grouped key/value display. Source: `GET /api/tenants/{tenantId}` / configuration read model.
4. **Command state** — shown inside the workflow, never as a separate primary nav area (forward-looking; this spec is read-only). Command lifecycle and feedback are owned by later command-capable stories.
5. **Audit evidence** — audit entry points scoped to the tenant (see §6).

### 3.2 Context preservation (both directions)

- **List → detail:** the selected tenant and its scope carry into detail.
- **Detail → list:** when the user returns to the list, the **selected tenant and the list filters are preserved** (Journey 1 → detail → list round trip). (AC3)
- Within detail, switching sections (overview ↔ members ↔ configuration ↔ command state ↔ audit) never loses tenant context.

### 3.3 Member access review (read-only)

The member table surfaces, per member, without implying command completion or membership mutation:

- User (identity, truncated/accessible per §5)
- Role
- Owner count (tenant-level context)
- Tenant status
- Freshness

Add/remove/change-role actions are **custom command flows, not generated CRUD**, and are out of scope for this read-only spec. The read-only member table must not imply that a removal or role change has been applied. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Read-Only Surface Consumption Map`]

### 3.4 Readiness

| Section | Surface key | Readiness | `blockedBy` (verbatim) |
| --- | --- | --- | --- |
| Overview | `ui-03-tenant-detail-overview-read-only` | `planning-only` | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| Member table | `ui-04-user-management-member-table` | `planning-only` | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| Configuration (read-only) | `ui-05-tenant-configuration-read-only` | `planning-only` | `[FC-LYT, FC-A11Y, FC-L10N, FC-DOC]` |

## 4. User Lookup and Global Administrator Read-Only Surfaces (AC4)

Access questions can begin with a user or a platform role, not only a tenant. Both surfaces stay reachable from shell navigation and access-review contexts.

### 4.1 User lookup / My Tenants

Surface key: `ui-02-my-tenants-and-user-search-read-only`.

- **Source query/endpoint:** `GET /api/users/{userId}/tenants` (`GetUserTenantsQuery`). Cursor-based pagination, signed opaque scoped cursors only.
- **Reachability:** reachable as the **Users** tab of the Tenants workspace and from access-review contexts (a tenant detail member row links to that user's lookup). The Users surface stays lookup/search-backed — not an exhaustive all-users inventory (see the 2026-06-27 supersession note). (AC4)
- **Authorization-safe states:** expose authorization-safe empty and error states. A user who has no accessible tenants, or whose scope the caller is not authorized to view, must see an authorization-safe empty/error state that does not leak whether memberships exist beyond the caller's authorized scope. Authorization and filtering live in projection/query handling, not in the UI. [Source: `_bmad-output/project-context.md#API Surface`]
- Cross-tenant revoke/remove actions are custom high-risk command flows and must **not** be generated from query rows.
- Readiness: `planning-only`; `blockedBy: [FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` (verbatim).

### 4.2 Global administrator read-only surface

Surface key: `ui-06-global-admin-read-only`.

- **Purpose:** review platform administrator access before deciding whether command flows are safe to expose.
- **Platform-vs-membership distinction (AC4):** global administrators live in the **separate `global-administrators` domain** (singleton aggregate, ID `"global-administrators"`). The surface explicitly distinguishes **platform-level governance risk** from ordinary tenant membership. It **must not be modeled as tenant membership** and must not route global-administrator data as normal tenant-domain data. [Source: `_bmad-output/project-context.md#Identity Scheme`; `docs/tenants-ui-frontcomposer-dependency-map.md#Read-Only Surface Consumption Map`]
- **Source evidence:** global administrator projection/read evidence from completed backend scope (`2-2-global-administrator-aggregate`, `11-1-production-jwt-configuration-validation`, `11-2-eventstore-tenant-claim-contract`; `docs:docs/production-auth-readiness.md`).
- Grant/remove global-administrator workflows are custom command flows, out of scope here.
- Readiness: `planning-only`; `blockedBy: [FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` (verbatim).

## 5. Identifiers, Support-Safe References, and Automation Selectors (AC5)

### 5.1 Identifier truncation and accessibility

- **IDs are literal caller-supplied strings, not ULIDs or GUIDs.** Long tenant IDs, user IDs, and references truncate **visually** but remain fully accessible:
  - Full value is copyable.
  - Full accessible name is exposed (no information loss for screen readers).
  - Truncation is visual only; no semantic truncation. [Source: `#Search, Filtering, and Table Patterns`; `_bmad-output/project-context.md#Identity Scheme`]

### 5.2 Automation selectors

- Future implementation must rely on **stable selectors or component contracts** for automation. **Relying on arbitrary row text is forbidden.** This ties to `FC-A11Y` (accessible names, stable roles) and `FC-DOC` (component-reference evidence). [Source: `#Search, Filtering, and Table Patterns`]

### 5.3 Support-safe references

Support-safe references for command/audit troubleshooting must **never expose** raw payloads, bearer tokens, stack traces, internal correlation IDs, or PII. Reference content is limited to non-sensitive, support-safe tokens (e.g., a support-safe command reference, tenant/user reference, projection version/freshness marker, accepted timestamp, audit event reference or fallback state). [Source: `#Support-Safe References`; `_bmad-output/project-context.md#Logging & Telemetry`; `_bmad-output/project-context.md#Security & Sanitization`]

## 6. Audit Entry-Point Context (read-only, planning context only)

This spec defines audit **entry points** within the shell and access-review contexts (§1.2), not the audit surface implementation. The audit surface itself remains `blocked`/`planning-only` and is owned by later stories.

- Audit is reachable from global nav, tenant rows, tenant detail, user lookup, and command result (§1.2).
- **Do not claim an `<AuditTimeline>` component exists.** No verified FrontComposer source path exists for it; the first audit slice is a flat DataGrid-backed list only if product/UX approves the fallback.
- Audit fallback surface keys and readiness (entry-point context only here):
  - `ui-11-audit-trail-flat-timeline` — `blocked`; `blockedBy: [FC-LYT, FC-AUD, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` (verbatim).
  - `ui-12-tenant-detail-audit-tab` — `blocked`; `blockedBy: [FC-LYT, FC-AUD, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` (verbatim).

[Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Read-Only Surface Consumption Map`; `docs/tenants-ui-phase-2-story-backlog.md`]

## 7. Per-Surface Readiness and Consumption Table

Every surface below names its source projection/query, freshness state, authorization/unavailable-action reason, accessibility behavior, localization responsibility, and a literal `blockedBy` array **copied verbatim** from the Phase 2 backlog. No surface becomes implementation-ready in this story; every surface remains `planning-only` or `blocked`. [Source: `#Implementation Story Rules`; `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`]

| Surface | Backlog key | Source projection / query | Freshness state | Authorization / unavailable-action reason | Accessibility behavior | Localization responsibility | Readiness | `blockedBy` (verbatim) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Tenant list | `ui-01-tenant-list-read-only` | `GET /api/tenants` (`ListTenantsQuery`) | ETag/304 + freshness marker; `unknown` if unmeasurable | Authorization filtering in projection/query; degraded/error states explain unavailable backend without leaking detail | Status/freshness/pending perceivable without color; stable selectors; keyboard scan | `FC-L10N` — status/action labels, timestamps, empty/error copy | `planning-only` | `[FC-LYT, FC-A11Y, FC-L10N, FC-DOC]` |
| User lookup / My Tenants | `ui-02-my-tenants-and-user-search-read-only` | `GET /api/users/{userId}/tenants` (`GetUserTenantsQuery`) | ETag/304 + freshness marker | Authorization-safe empty/error; no leak of out-of-scope memberships | No-color-only role/status; accessible names; stable selectors | `FC-L10N` — role/status labels, empty/error copy | `planning-only` | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| Tenant detail overview | `ui-03-tenant-detail-overview-read-only` | `GET /api/tenants/{tenantId}` (`GetTenantQuery`); `GET /api/tenants/{tenantId}/users` for summary | ETag/304 + freshness marker | Read-only; unavailable sections explain reason | Section navigation keyboard-reachable; context preserved | `FC-L10N` — section labels, status/role labels | `planning-only` | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| Member table | `ui-04-user-management-member-table` | `GET /api/tenants/{tenantId}/users` (`GetTenantUsersQuery`) | ETag/304 + freshness marker | Read-only; no implied membership mutation; role-aware display only | No-color-only role/status; keyboard/focus; stable selectors | `FC-L10N` — role/status labels | `planning-only` | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| Configuration (read-only) | `ui-05-tenant-configuration-read-only` | `GET /api/tenants/{tenantId}` / configuration read model | ETag/304 + freshness marker | Read-only; sensitive-value handling deferred to a later story | Table accessibility; localized empty/error | `FC-L10N` — key/value and empty/error copy | `planning-only` | `[FC-LYT, FC-A11Y, FC-L10N, FC-DOC]` |
| Global admin read-only | `ui-06-global-admin-read-only` | Global administrator projection/read (`global-administrators` domain) | ETag/304 + freshness marker | Platform governance distinct from membership; read-only | No-color-only platform-access labels; keyboard/focus | `FC-L10N` — platform-access labels | `planning-only` | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| Audit fallback (entry-point context) | `ui-11-audit-trail-flat-timeline` / `ui-12-tenant-detail-audit-tab` | `GET /api/tenants/{tenantId}/audit` (`GetTenantAuditQuery`) | ETag/304 + freshness; delayed vs unavailable distinguished | Read-only; distinguish delayed evidence from missing implementation support | Flat list accessible; no `<AuditTimeline>` claim | `FC-L10N` — audit labels, timestamps, empty/error | `blocked` | `[FC-LYT, FC-AUD, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |

## 8. Backend and Data Boundaries

- Future surfaces consume **existing read endpoints only**: `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, `GET /api/tenants/{tenantId}/audit`. Cursor-based pagination only — signed, opaque, scope-bound cursors; never offset/limit. ETag `If-None-Match` → `304` is the freshness primitive. [Source: `_bmad-output/project-context.md#API Surface`]
- **Command endpoint route note (read-only spec):** this spec defines read-only surfaces and introduces no command rows. If a later workflow cites command surfaces for navigation context, the route is `POST /api/v1/commands` (FrontComposer `EventStoreOptions.CommandEndpointPath` default). `project-context.md` records the unversioned `POST /api/commands` `CommandsController` as an alias to confirm against the deployed gateway, not to assume. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Command Endpoint Route Evidence`]
- **Do not annotate or reshape immutable Tenants domain contracts** for UI generation.
- **SignalR projection notifications are refresh nudges only** — never proof of command completion or projection consistency. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- Generated FrontComposer composition is appropriate **only** for low-risk, read-only, projection-backed surfaces where Tenants stays source of truth and the UI does not imply durable command success. Member/global-admin command actions are custom command flows, not generated CRUD. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Read-Only Surface Consumption Map`]

## 9. Acceptance Criteria Traceability

| AC | Requirement | Spec section |
| --- | --- | --- |
| AC1 | Primary nav = Tenants, Users, Global Administrators, Audit; command lifecycle not a separate primary nav model | §1.1, §1.2 |
| AC2 | Tenant list: filter/search/sort/pagination + columns + loading/empty/filtered-empty/error/stale/degraded; sort/pagination never hide pending/stale | §2 |
| AC3 | Tenant context preserved across overview/members/configuration/command state/audit; selected tenant + filters preserved on return to list | §1.2, §3 |
| AC4 | User lookup reachable from shell nav + access-review contexts; global admin distinguishes platform risk from tenant membership | §1.2, §4 |
| AC5 | Long IDs/refs truncate visually but stay accessible; stable selectors/component contracts required instead of arbitrary row text | §5 |

## 10. References

- `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Navigation Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Search, Filtering, and Table Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Empty, Loading, Stale, and Degraded States`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Journey 1: Tenant Discovery and Triage`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Journey 2: Access Review and Action Availability`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Support-Safe References`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Truth State Model`
- `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`
- `docs/tenants-ui-frontcomposer-dependency-map.md#Read-Only Surface Consumption Map`
- `docs/tenants-ui-frontcomposer-dependency-map.md#Command Endpoint Route Evidence`
- `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`
- `_bmad-output/project-context.md#API Surface`
- `_bmad-output/project-context.md#Identity Scheme`
- `_bmad-output/project-context.md#Logging & Telemetry`
