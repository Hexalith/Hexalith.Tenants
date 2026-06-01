---
baseline_commit: 1d34fbc0421f0a5623838f70d328853a282ec666
---

# Story 9.4: Specify the RemoveUserFromTenant Command-Capable Journey

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a tenant owner or global administrator,
I want the first command-capable UI journey to remove user access safely,
so that access changes are previewed, submitted, reconciled, and proven without false success.

## Acceptance Criteria

1. **Given** the first command-capable UI slice is planned, **when** command workflow scope is selected, **then** `RemoveUserFromTenant` is the first command-capable journey, **and** it is launched from a specific tenant membership row with tenant, user, role, freshness, and authority context visible.
2. **Given** consequence preview is specified, **when** remove-user action is prepared, **then** preview content includes tenant, target user, current role, owner count, affected access path, freshness, recovery path, audit expectation, known consequences, and known unknowns, **and** incomplete consequence inputs block submit unless product and UX approve a named fallback.
3. **Given** high-risk access cases are specified, **when** last-owner removal, global administrator removal, or tenant-wide impact is detected, **then** elevated friction, affected scope, evidence freshness, audit consequence, and intentional confirmation are required, **and** destructive actions do not appear as casual primary actions.
4. **Given** command submission is specified, **when** the user confirms removal, **then** the UI records local pending or confirming hints without replacing confirmed projection truth, **and** required fields are validated before command preview or submit.
5. **Given** command reconciliation is specified, **when** the backend rejects, accepts, reports already applied, delays projection, or cannot be verified, **then** the UI preserves context, maps domain rejections to safe localized text, and offers retry, status review, inspect audit, continue read-only, or escalation paths, **and** raw command payloads, stack traces, tokens, and internal exception text are not exposed.

## Tasks / Subtasks

- [x] Preserve the Epic 9 planning-only boundary before editing any artifact. (AC: 1-5)
  - [x] Confirm this story produces planning/specification documentation only — a `RemoveUserFromTenant` command-capable journey specification for Phase 2 UI implementation stories. It does NOT make the journey implementation-ready.
  - [x] Do NOT implement Tenants Admin UI screens, FrontComposer/Fluent UI components (Consequence Preview, Command Lifecycle Panel, Truth State Badge, Freshness Gate, Unavailable Action Reason, Audit Evidence Receipt, etc.), Blazor pages/routes, backend endpoints, commands, queries, package references, generated UI files, domain-contract annotations, or submodule pointer changes. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`]
  - [x] Keep backend MVP work independent from UI dependency readiness; missing UI dependencies block or defer future Phase 2 UI rows, never backend package/release work. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- [x] Reconcile with existing Epic 9 planning artifacts instead of duplicating them. (AC: 1-5)
  - [x] Read `docs/tenants-ui-truth-state-and-action-availability-spec.md` (Story 9.3, the canonical owner of the truth-state badge vocabulary, freshness gating, unavailable-action reasons, layered feedback states, and the `RemoveUserFromTenant` command state model worked example in §5.3). This journey spec **composes** those patterns; it must NOT redefine badge states, freshness primitives, reason categories, feedback states, or the command state model.
  - [x] Read `docs/tenants-ui-operations-shell-spec.md` (Story 9.2) for the surface that launches this journey: the membership row in tenant detail → members. Preserve tenant context across overview/members/configuration/command state/audit; do not promote command lifecycle into a separate primary navigation model. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.2`]
  - [x] Read `docs/tenants-ui-frontcomposer-dependency-map.md` (Dependency ID Catalog) and reuse its dependency IDs verbatim — do NOT redefine `FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC`. The remove-user journey maps primarily to `FC-CMD` (command lifecycle), `FC-CNC` (concurrent command policy), `FC-CNS` (consequence preview, currently `missing`), `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-LYT`, `FC-DOC` (and `FC-AUD` where audit-evidence states are referenced — full audit/recovery patterns are Story 9.5).
  - [x] Read `docs/tenants-ui-phase-2-story-backlog.md` and bind the journey spec to the single consuming row `ui-14-user-management-remove-user`. Copy its `blockedBy` array verbatim: `[FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]`; row status is `blocked`; `fallbackDecision` is `deferred` (`inline-consequence-preview-not-approved`). Do NOT invent new dependency IDs, column names, or `ui-NN` keys.
  - [x] Decide output location: create a new `docs/tenants-ui-remove-user-from-tenant-journey-spec.md` (no command-journey spec exists today; `docs/` has only the dependency map, operations-shell spec, Phase 2 backlog, and truth-state spec for UI). If you instead extend an existing doc, justify why a new artifact is not warranted and avoid contradicting existing source-of-truth language.
- [x] Specify the journey launch context and scope. (AC: 1)
  - [x] State that `RemoveUserFromTenant` is the **first** command-capable journey and the recommended first command (not `DisableTenant`, which has broader blast radius). [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Defining Experience`; `#Platform Strategy`]
  - [x] Require the journey to launch from a **specific tenant membership row** in tenant detail → members, with tenant, target user, current role, freshness, and authority (who-can-act) context visible before the action. Treat it as an access-evidence journey, not a button. [Source: `#Experience Principles`; `#Journey 3: Remove User From Tenant`; AC1]
  - [x] Source launch context only from existing read endpoints: `GET /api/tenants/{tenantId}/users` (membership rows + roles), `GET /api/tenants/{tenantId}` (owner count, tenant status, freshness marker). No new read endpoint. [Source: `_bmad-output/project-context.md#API Surface`]
- [x] Specify the Consequence Preview content and the incomplete-input block. (AC: 2)
  - [x] Enumerate preview content exactly: tenant, target user, current role, owner count, affected access path, freshness, recovery path, audit expectation, known consequences, and known unknowns. [Source: `#Consequence Preview`; `#Experience Mechanics`; AC2]
  - [x] State the fail-closed rule: incomplete consequence inputs block submit unless product and UX approve a **named** fallback (do not silently submit on partial preview). Tie to `FC-CNS` (`missing`). [Source: `#Consequence Preview`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §3.3 fail-closed rule]
  - [x] Compose the preview from existing read-model evidence only; do NOT add a backend "consequence" or "command status" endpoint. Owner count and affected access path derive from the tenant/users read models. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/project-context.md#API Surface`]
  - [x] Do not claim session revocation, downstream enforcement, or token invalidation as consequences unless backend evidence exists; classify those as known unknowns. [Source: `#Flow Optimization Principles`]
- [x] Specify the high-risk access cases and elevated friction. (AC: 3)
  - [x] **Critical backend truth — do not invent a backend invariant.** `TenantAggregate` does NOT enforce a "must retain ≥1 owner" rule; last-owner removal is **allowed by design**, and the UI surfaces `ownerCount == 0` as a **warning** with elevated friction — never as a backend block. Specifying a hard backend prohibition would contradict the domain and block legitimate ownership-transfer flows. [Source: `_bmad-output/project-context.md#Aggregates`]
  - [x] Require elevated friction (affected scope, evidence freshness, audit consequence, intentional confirmation) for: last-owner removal (`ownerCount → 0`), removal of a user who also holds global-administrator authority, and tenant-wide impact cases. Destructive actions must not appear as casual primary actions. [Source: `#Flow Optimization Principles`; `#Button Hierarchy`; AC3]
  - [x] Keep domains distinct: this journey removes **tenant membership** (`RemoveUserFromTenant`, `tenants` domain). Removing platform global-administrator authority is a **separate** command (`RemoveGlobalAdministrator`, `global-administrators` singleton domain, backlog `ui-15`) — flag global-admin authority as a platform-level risk to elevate friction, but do not conflate it with this command. [Source: `_bmad-output/project-context.md#Identity Scheme`; `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories` (`ui-15`)]
- [x] Specify command submission: local hints without replacing confirmed truth. (AC: 4)
  - [x] Require the UI to record local **pending/confirming hints** separately from confirmed projection truth — never overwrite last-confirmed membership data with optimistic intent. [Source: `#Journey Invariants`; `#Command Lifecycle Panel`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §5.2 non-collapse invariant; AC4]
  - [x] Require required-field validation **before** preview or submit (target user, tenant, role context resolved; freshness/authorization gates passed). [Source: `#Implementation Story Rules`; AC4]
  - [x] Reference (do not redefine) the `RemoveUserFromTenant` command state model: `eligible → previewed → submitted → accepted → projection_pending → confirmed | failed | unknown | audit_pending | audit_available`. The canonical definition lives in the UX spec and the 9.3 truth-state spec. [Source: `#RemoveUserFromTenant Command State Model`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §5.3]
  - [x] State the command surface for lifecycle context as `POST /api/v1/commands` (FrontComposer `EventStoreOptions.CommandEndpointPath` default), with `command:RemoveUserFromTenant`. `project-context.md` records the unversioned `/api/commands` `CommandsController` as an alias to confirm against the deployed gateway. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Command Endpoint Route Evidence`; `_bmad-output/project-context.md#API Surface`]
- [x] Specify command reconciliation and recovery paths. (AC: 5)
  - [x] Cover every backend outcome distinctly: rejected, accepted, already applied, projection delayed (`projection_pending`), and unable-to-verify (`unknown`). Preserve tenant/user context across all outcomes. [Source: `#Journey 3: Remove User From Tenant`; `#Feedback Patterns`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §5; AC5]
  - [x] Map domain rejections to **safe localized text** composed at the HTTP boundary by `RejectionToHttpStatusMapper` (RFC 7807: 404 not-found, 409 conflict, 422 other). Never expose raw command payloads, stack traces, bearer tokens, internal correlation IDs, or internal exception text. [Source: `_bmad-output/project-context.md#API Surface`; `#Support-Safe References`; AC5]
  - [x] Offer concrete recovery choices: refresh, retry, status review, inspect audit, continue read-only, request permission, escalate with a support-safe reference (and, for an erroneous removal, start a compensating command — the full compensating-recovery UI pattern is **Story 9.5**, referenced not specified here). [Source: `#Concurrency and Recovery Cases`; `#Journey 4: Audit Evidence and Compensating Recovery`; AC5]
  - [x] Cover the event-sourced concurrency/recovery cases explicitly: target already removed before submit, tenant status changed while preview open, operator lost permission mid-flow, duplicate submit/refresh during pending, projection lag after acceptance, audit evidence delayed, SignalR nudge-only/disconnected, status lookup failed → `unknown`. SignalR is a freshness nudge only, never proof of command completion. [Source: `#Concurrency and Recovery Cases`; `#Journey Invariants`]
- [x] Record per-pattern consumption mapping and future-story readiness. (AC: 1-5)
  - [x] Bind the journey to consuming backlog row `ui-14-user-management-remove-user`; copy its `blockedBy` array verbatim and its `backendEvidence` (`[3-1-user-role-management, 3-2-role-behavior-enforcement, 5-3-query-endpoints-and-authorization, 9-3-query-policy-for-disabled-tenants-and-orphan-memberships, command:RemoveUserFromTenant, endpoint:POST /api/v1/commands]`). Keep the row `blocked`; it does NOT become implementation-ready in this story. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories` (`ui-14`)]
  - [x] State the Implementation Story Rules a future UI story must satisfy to implement this journey: source projection/query, freshness state shown, authorization + unavailable-action reason, command lifecycle states, consequence preview inputs, audit evidence path or approved read-only fallback, support-safe references, accessibility behavior, and localization responsibility. [Source: `#Implementation Story Rules`]
  - [x] Defer the audit-evidence-receipt and compensating-recovery UI patterns (and the flat audit DataGrid fallback) to Story 9.5; this journey references audit expectation and recovery path but does not specify their full patterns. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.5`]
- [x] Record support-safe references and automation/accessibility contracts shared with other Epic 9 stories. (AC: 1-5)
  - [x] Support-safe references for command/audit troubleshooting must never expose raw payloads, bearer tokens, stack traces, internal correlation IDs, or PII. [Source: `#Support-Safe References`; `_bmad-output/project-context.md#Logging & Telemetry`]
  - [x] Accessibility: disabled/unavailable explanations, command lifecycle changes, stale/degraded states, and audit availability must be perceivable without color and announced via live status; keyboard users must be able to complete or exit every preview/confirmation/command flow. Tie to `FC-A11Y`. [Source: `#Accessibility`]
  - [x] Require stable selectors / component contracts (not arbitrary row text) for automation of journey-state assertions, tied to `FC-A11Y`/`FC-DOC`. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §9.3]
- [x] Record implementation evidence and run documentation validation. (AC: 1-5)
  - [x] Run `git diff --check` after documentation edits.
  - [x] Confirm no source code, package versions, lockfiles, generated files, submodule pointers (`git diff --submodule=short` empty for all root submodules), backend story statuses, UI screens, endpoints, commands, queries, or Phase 1 release gates changed.
  - [x] Update this story's Dev Agent Record with files changed, validation performed, and unresolved decisions.

## Dev Notes

### Scope Guard (read first)

- Epic 9 is readiness/planning-only. This story produces a **`RemoveUserFromTenant` command-capable journey specification** — the worked first command journey that composes the truth-state/feedback contract (9.3), the operations shell (9.2), and the dependency map (9.1) into a single end-to-end remove-user flow spec — not shippable Admin UI. It must not route a Developer agent into product UI delivery until separate Phase 2 implementation stories are created. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`]
- Output is documentation only: the journey launch context, consequence-preview contract, high-risk friction rules, command-submission/local-hint rules, and reconciliation/recovery rules — plus per-pattern consumption mapping pointing back to existing source-of-truth artifacts. It does NOT create UI components, Blazor pages/routes, backend endpoints, commands, queries, package references, generated files, or Phase 1 release gates. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.4`]
- Backend MVP stories remain unblocked by UI dependency readiness. [Source: `_bmad-output/project-context.md#BMAD Workflow`]

### This story composes existing patterns — it does NOT redefine them

- Story 9.3's `docs/tenants-ui-truth-state-and-action-availability-spec.md` is the canonical owner of the Truth State Badge vocabulary, freshness gating (ETag `If-None-Match` → `304` via `CachingProjectionActor`; unmeasurable = `unknown`, fail-closed), the Unavailable Action Reason taxonomy, the layered feedback state set, and the `RemoveUserFromTenant` command state model worked example (§5.3). Story 9.4 **applies** these to the remove-user journey; it must stay consistent with them and must not re-enumerate or contradict them. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md`]
- Story 9.2's `docs/tenants-ui-operations-shell-spec.md` owns the navigation/IA and the tenant-detail → members surface this journey launches from. Preserve tenant context across overview/members/configuration/command state/audit; do not promote command lifecycle into separate primary navigation. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.2`]
- Story 9.1's `docs/tenants-ui-frontcomposer-dependency-map.md` owns the 10 fixed dependency IDs. Reuse them verbatim; add none.

### Previous Story Intelligence (Stories 9.1, 9.2 & 9.3 — done)

- **Reconcile, do not duplicate.** Story 9.1's review penalized an unnecessary parallel artifact and reverted a wrong endpoint normalization; Stories 9.2 and 9.3 reused the dependency map, shell spec, and backlog as copy-forward source of truth and copied `blockedBy` arrays verbatim. This journey spec adds a *worked command-journey* layer on top of those; it references dependency IDs, backlog rows, and the 9.3 truth-state patterns rather than redefining them. [Source: `_bmad-output/implementation-artifacts/9-1-map-fluent-ui-and-frontcomposer-dependencies-for-tenant-admin-screens.md#Senior Developer Review (AI)`; `_bmad-output/implementation-artifacts/9-3-define-truth-state-freshness-and-unavailable-action-patterns.md`]
- **Command endpoint route is `POST /api/v1/commands`** (FrontComposer `EventStoreOptions.CommandEndpointPath` default), not `/api/commands`. `project-context.md` records the unversioned `/api/commands` `CommandsController` as an *alias* to confirm against the deployed gateway. Use `/api/v1/commands` when citing the command surface. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Command Endpoint Route Evidence`; `_bmad-output/project-context.md#API Surface`]
- **Dependency ID catalog is fixed** at 10 IDs (`FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC`), each defined once. Do not add new IDs. `FC-CNS` (consequence preview) and `FC-CNC` (concurrent command policy) are recorded `missing`; `FC-CMD` is `needs-confirmation` — these keep `ui-14` `blocked`. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`]

### CRITICAL backend truth — last-owner removal is allowed (do NOT invent an invariant)

- `TenantAggregate` does **NOT** enforce a "must retain ≥1 owner" invariant. Removing the last owner is **allowed by design**; the UX surfaces `ownerCount == 0` as a **warning** (elevated friction), not a hard block. A future implementer must NOT add a backend prohibition or model the UI as if the backend rejects last-owner removal — that would block legitimate ownership-transfer flows. AC3's "elevated friction" is a UI-friction/warning requirement, not a backend invariant. [Source: `_bmad-output/project-context.md#Aggregates`]
- Empty-tenant bootstrap: `AddUserToTenant` skips owner-only RBAC when `state.HasMembershipHistory == false`. This is the inverse boundary (first user in), informative for the compensating-recovery narrative (Story 9.5), not for this removal journey's gating. [Source: `_bmad-output/project-context.md#Aggregates`]

### Domains are distinct (prevent conflation)

- This journey is **tenant membership** removal (`RemoveUserFromTenant`, domain `tenants`, `AggregateId` = managed tenant ID). Removing **platform global-administrator authority** is a separate command (`RemoveGlobalAdministrator`) on the singleton `global-administrators` domain/aggregate — backlog row `ui-15`, not this journey. AC3's "global administrator removal" friction means: when the *target user also holds global-admin authority*, raise platform-level risk friction; it does not mean this command edits the global-administrators aggregate. [Source: `_bmad-output/project-context.md#Identity Scheme`; `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories` (`ui-15`)]

### Numbering hazard — two different "9-3"/"10-x"/"11-x" namespaces (read carefully)

- This story's key is the **sprint-status Epic 9 UI-planning** key `9-4-specify-the-removeuserfromtenant-command-capable-journey`. The `backendEvidence` arrays inside `docs/tenants-ui-phase-2-story-backlog.md` (e.g. `ui-14` cites `9-3-query-policy-for-disabled-tenants-and-orphan-memberships`) reference a **separate Phase 2 backend backlog** that also uses `9-x`/`10-x`/`11-x`/`12-x` keys. These are NOT this epic's stories. Do not conflate the two namespaces or mark backend rows complete based on this UI story. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`; `_bmad-output/implementation-artifacts/sprint-status.yaml`]

### Journey shape (AC1–AC5) — canonical source

- The remove-user flow (UX `#Journey 3`): select member row → open remove-access preview (tenant, target user, role, owners, freshness, recovery path) → gate (fresh, authorized, consequence-ready, audit-ready?) → block/elevated friction or confirm → request sent → backend outcome (rejected / already applied / accepted → change pending → reconciled → access updated) → audit proof available or audit pending/unavailable. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Journey 3: Remove User From Tenant`]
- Command state model (reference, do not redefine): `eligible → previewed → submitted → accepted → projection_pending → confirmed | failed | unknown | audit_pending | audit_available`. Each state requires visible UI copy, enabled/disabled actions, retry behavior, and a support-safe reference where available. [Source: `#RemoveUserFromTenant Command State Model`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §5.3]
- Custom components this journey *uses* (do NOT implement them): Consequence Preview (`FC-CNS` missing), Command Lifecycle Panel (`FC-CMD` needs-confirmation, `FC-CNC` missing), Truth State Badge / Freshness Gate / Unavailable Action Reason (`FC-TOK`/`FC-A11Y`), Audit Evidence Receipt + flat audit fallback (`FC-AUD`, deferred to Story 9.5). [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Custom Components`; `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`]

### Pattern-to-backlog consumption mapping (copy `blockedBy`/`backendEvidence` verbatim; do not re-derive)

- **Consuming row:** `ui-14-user-management-remove-user` — status `blocked`; `fallbackDecision` `deferred` (`inline-consequence-preview-not-approved`); owner `Tenants Product/UX + Hexalith.FrontComposer`.
- **`blockedBy` (verbatim):** `[FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]`.
- **`backendEvidence` (verbatim):** `[3-1-user-role-management, 3-2-role-behavior-enforcement, 5-3-query-endpoints-and-authorization, 9-3-query-policy-for-disabled-tenants-and-orphan-memberships, command:RemoveUserFromTenant, endpoint:POST /api/v1/commands]`.
- Row stays `blocked` (FC-CNS `missing` + deferred high-impact fallback). It does NOT become implementation-ready here. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`]

### Backend and Data Boundaries

- Launch/preview/freshness evidence comes only from existing read endpoints: `GET /api/tenants/{tenantId}` (status, owner count, freshness), `GET /api/tenants/{tenantId}/users` (members + roles), and `GET /api/tenants/{tenantId}/audit` for audit-evidence states. Cursor-based pagination only — signed, opaque, scope-bound cursors; never offset/limit. ETag `If-None-Match` → `304` is the freshness primitive served by `CachingProjectionActor`. [Source: `_bmad-output/project-context.md#API Surface`; `#Projections`]
- The command is dispatched to `POST /api/v1/commands` (`command:RemoveUserFromTenant`). Do NOT add a backend "consequence" or "command status" endpoint; consequence preview composes from read models, and command lifecycle is tracked client-side via the FrontComposer command/feedback services (`FC-CMD`). SignalR notifications are refresh nudges only, never proof. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/project-context.md#API Surface`]
- Rejection events carry structured data only; user-facing text is composed at the HTTP boundary by `RejectionToHttpStatusMapper` (RFC 7807: 404 not-found, 409 conflict, 422 other domain rejections). Domain rejections must map to safe localized UI text — never expose raw payloads, stack traces, tokens, internal correlation IDs, or internal exception text. [Source: `_bmad-output/project-context.md#API Surface`; `#Support-Safe References`]
- Identity: use JWT `sub` (`envelope.UserId`) for user identity, never `name`/`email`. IDs are ULIDs, not GUIDs. Authorization is enforced server-side (L1 API gate + L2 domain RBAC); the UI reflects authorization state and unavailable-action reasons but is not the authorization boundary. [Source: `_bmad-output/project-context.md#Identity Scheme`; `#Authorization (RBAC)`]

### Project Structure Notes

- Documentation outputs belong under `docs/` and this story's Dev Agent Record. No command-journey spec exists yet (`docs/` has `tenants-ui-frontcomposer-dependency-map.md`, `tenants-ui-operations-shell-spec.md`, `tenants-ui-phase-2-story-backlog.md`, and `tenants-ui-truth-state-and-action-availability-spec.md` for UI), so a new `docs/tenants-ui-remove-user-from-tenant-journey-spec.md` is the natural home. Future Phase 2 UI assets belong in a dedicated FrontComposer adapter/UI project only after readiness conversion, not in backend packages. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- Preserve root-level submodule policy. Reading `Hexalith.FrontComposer` is allowed; never initialize nested submodules or run recursive submodule commands. [Source: `CLAUDE.md#Submodule Policy`]
- `_bmad-output/` is untracked — do not commit it in code PRs. Use central package management; add no package versions in this documentation story. [Source: `_bmad-output/project-context.md#BMAD Workflow`]

### Testing / Validation Requirements

- No source-code test suite is required: this story updates planning/specification documentation only. [Source: `_bmad-output/project-context.md#BMAD Workflow`]
- Required documentation validation:
  - `git diff --check`.
  - Manual AC checklist against this story (AC1 first-command-journey + membership-row launch context; AC2 consequence-preview content set + incomplete-input block; AC3 high-risk friction incl. last-owner-as-warning-not-block + casual-action prohibition; AC4 local-hints-without-replacing-truth + pre-submit validation; AC5 reconciliation outcomes + safe-localized-rejection + recovery paths + no-internal-leak).
  - Consistency check against `docs/tenants-ui-truth-state-and-action-availability-spec.md` (badge vocabulary, freshness primitive ETag/304, feedback states, command state model §5.3 — referenced, not redefined) and `docs/tenants-ui-frontcomposer-dependency-map.md` (10 fixed dependency IDs; no new IDs).
  - Per-pattern consumption check: the journey names consuming row `ui-14` and copies its literal `blockedBy`/`backendEvidence` arrays from the backlog; no new dependency IDs, column names, or `ui-NN` keys introduced; `ui-14` stays `blocked`.
  - Domain-distinction check: no conflation of `RemoveUserFromTenant` (tenants) with `RemoveGlobalAdministrator` (global-administrators); no invented "≥1 owner" backend invariant.
  - Change-boundary check confirming no source code, package files, generated artifacts, submodule pointers, backend endpoints, commands, queries, UI screens, or Phase 1 gates changed.

### References

- `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`
- `_bmad-output/planning-artifacts/epics.md#Story 9.4: Specify the RemoveUserFromTenant Command-Capable Journey`
- `_bmad-output/planning-artifacts/epics.md#Story 9.5: Specify Audit Evidence and Compensating Recovery UI Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Journey 3: Remove User From Tenant`
- `_bmad-output/planning-artifacts/ux-design-specification.md#RemoveUserFromTenant Command State Model`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Truth State Model`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Journey Invariants`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Flow Optimization Principles`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Concurrency and Recovery Cases`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Story Rules`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Consequence Preview`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Command Lifecycle Panel`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Experience Mechanics`
- `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`
- `docs/tenants-ui-truth-state-and-action-availability-spec.md`
- `docs/tenants-ui-operations-shell-spec.md`
- `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`
- `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`
- `_bmad-output/implementation-artifacts/9-3-define-truth-state-freshness-and-unavailable-action-patterns.md`
- `_bmad-output/project-context.md#Aggregates`
- `_bmad-output/project-context.md#Identity Scheme`
- `_bmad-output/project-context.md#API Surface`
- `_bmad-output/project-context.md#Support-Safe References`

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — create-story context engine

### Debug Log References

- 2026-06-02 - Resolved BMAD create-story customization (`resolve_customization.py` shape: empty prepend/append, `persistent_facts: **/project-context.md`, no `on_complete`); loaded config, sprint status, project context, planning artifacts (epics Epic 9 + Story 9.4/9.5, ux-design-specification Journey 3 / RemoveUserFromTenant Command State Model / Consequence Preview / Concurrency & Recovery / Implementation Story Rules, architecture Frontend), existing UI docs (dependency map, operations-shell spec, Phase 2 backlog, truth-state spec), and previous Stories 9.1/9.2/9.3 output.
- 2026-06-02 - Verified Story 9.4 target key from `sprint-status.yaml`: `9-4-specify-the-removeuserfromtenant-command-capable-journey` (status `backlog`); epic-9 already `in-progress` (9.1/9.2/9.3 done), no epic status change required (this is not the first story in the epic).
- 2026-06-02 - Confirmed no existing `docs/` command-journey spec; Story 9.4 produces a new `docs/tenants-ui-remove-user-from-tenant-journey-spec.md` that composes the 9.3 truth-state/feedback contract, 9.2 shell, and 9.1 dependency map into the worked RemoveUserFromTenant journey.
- 2026-06-02 - Captured AC1–AC5 source mapping and bound the journey to consuming backlog row `ui-14-user-management-remove-user` with verbatim `blockedBy` `[FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` and `backendEvidence`. Flagged two disaster-prevention insights: (1) `TenantAggregate` does NOT enforce a ≥1-owner invariant — last-owner removal is allowed; UI shows `ownerCount==0` as a warning, not a backend block; (2) `RemoveUserFromTenant` (tenants domain) must not be conflated with `RemoveGlobalAdministrator` (global-administrators singleton, backlog `ui-15`). Also flagged the dual `9-x`/`10-x`/`11-x` namespace hazard between sprint-status Epic 9 and the backlog `backendEvidence` keys, and the `/api/v1/commands` vs `/api/commands` alias.

#### Dev-story execution (2026-06-02)

- Authored `docs/tenants-ui-remove-user-from-tenant-journey-spec.md` — the worked RemoveUserFromTenant command-capable journey, composing the 9.3 truth-state/feedback contract, the 9.2 operations shell, and the 9.1 dependency map. Covers AC1 (launch context from a membership row, sourced from `GET /api/tenants/{tenantId}/users` + `GET /api/tenants/{tenantId}`), AC2 (10-item Consequence Preview content + fail-closed incomplete-input block tied to `FC-CNS` missing), AC3 (last-owner warning-not-block, global-admin platform-flag-not-domain-switch, casual-action prohibition), AC4 (local pending/confirming hints kept separate from confirmed projection truth, pre-submit validation, command state model referenced not redefined, `POST /api/v1/commands`), and AC5 (rejected/accepted/already-applied/projection-delayed/unable-to-verify outcomes, RFC 7807 safe-localized rejections via `RejectionToHttpStatusMapper`, recovery paths, SignalR nudge-only, no internal leakage).
- Reconciliation guardrails honored: no new dependency IDs, column names, or `ui-NN` keys; `ui-14` stays `blocked`; `blockedBy`/`backendEvidence` copied verbatim; no invented "≥1 owner" backend invariant; no `RemoveUserFromTenant`/`RemoveGlobalAdministrator` conflation; audit-evidence-receipt + compensating-recovery patterns deferred to Story 9.5 (referenced only).
- Validation: `git diff --check` clean; `git diff --submodule=short` empty for all root submodules; `git status --porcelain` shows only documentation/planning artifacts changed (new `docs/` journey spec + untracked `_bmad-output/` planning files + sprint-status). No source code, package versions, lockfiles, generated files, backend endpoints/commands/queries, UI screens, or Phase 1 release gates changed.

### Completion Notes List

- Create-story context engine analysis completed for Story 9.4.
- Existing dependency map, operations-shell spec, Phase 2 UI backlog, and 9.3 truth-state spec treated as copy-forward source of truth to prevent duplicate planning artifacts (carrying forward Story 9.1/9.2/9.3 review lessons).
- Story is explicitly scoped to documentation only (planning-only). It produces the first command-capable *journey* specification by composing — not redefining — the 9.3 truth-state/feedback patterns, the 9.2 shell, and the 9.1 dependency map.
- Two critical disaster-prevention guardrails embedded: no invented backend "≥1 owner" invariant (last-owner removal is allowed by design; UI warns), and strict separation of `RemoveUserFromTenant` from `RemoveGlobalAdministrator` domains.
- Audit-evidence-receipt and compensating-recovery UI patterns are deferred to Story 9.5; this journey references audit expectation and recovery path only.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- **Dev-story complete (2026-06-02):** delivered the planning-only journey specification `docs/tenants-ui-remove-user-from-tenant-journey-spec.md` satisfying AC1–AC5. All AC1–AC5 verified against the story checklist and cross-checked for consistency with the 9.3 truth-state spec (badge vocabulary, ETag/304 freshness primitive, feedback states, command state model §5.3 — referenced, not redefined) and the 10 fixed dependency IDs (no new IDs). Domain-distinction and no-invented-invariant checks pass. No source-code test suite is required for this documentation-only story. Story → `review`.

### File List

- `_bmad-output/implementation-artifacts/9-4-specify-the-removeuserfromtenant-command-capable-journey.md` (modified) — this story file; tasks checked, Status → review, Dev Agent Record updated. (untracked planning artifact)
- `docs/tenants-ui-remove-user-from-tenant-journey-spec.md` (new) — the RemoveUserFromTenant command-capable journey specification (AC1–AC5 deliverable).
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified) — `9-4-…-command-capable-journey` status → review; `last_updated` 2026-06-02. (untracked planning artifact)

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (AI adversarial review) — 2026-06-02
**Outcome:** ✅ Approve — Status → done
**Story key:** `9-4-specify-the-removeuserfromtenant-command-capable-journey`

### Scope of review

Adversarial validation of the planning-only deliverable `docs/tenants-ui-remove-user-from-tenant-journey-spec.md` against the story's AC1–AC5, the three source-of-truth UI docs (9.1 dependency map, 9.2 operations-shell spec, 9.3 truth-state spec), the Phase 2 backlog, the UX design specification, and `project-context.md`. Every cross-reference and "verbatim" claim was checked against its cited source. `_bmad/` and `_bmad-output/` were excluded from source-code review per the workflow.

### Claims verified (no defects found)

- **Verbatim backlog arrays (`ui-14-user-management-remove-user`).** `blockedBy = [FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` and `backendEvidence = [3-1-user-role-management, 3-2-role-behavior-enforcement, 5-3-query-endpoints-and-authorization, 9-3-query-policy-for-disabled-tenants-and-orphan-memberships, command:RemoveUserFromTenant, endpoint:POST /api/v1/commands]` match `docs/tenants-ui-phase-2-story-backlog.md` line 81 exactly. Row status `blocked`, `fallbackDecision` `deferred (inline-consequence-preview-not-approved)`, owner string — all match. No new dependency IDs, column names, or `ui-NN` keys introduced.
- **Dependency readiness states.** Spec claims FC-TBL `available`, FC-CMD `needs-confirmation`, FC-CNC `missing`, FC-CNS `missing`, FC-TOK `missing` — all match the canonical Dependency ID Catalog (`docs/tenants-ui-frontcomposer-dependency-map.md` lines 36–45). 10 fixed IDs reused; none added.
- **9.3 truth-state spec section anchors.** Every cited section (§2, §2.2, §3, §3.2, §3.3, §4.1, §4.2, §4.4, §5, §5.1, §5.2, §5.3, §5.4, §8, §9.1–§9.4, §10) exists in `docs/tenants-ui-truth-state-and-action-availability-spec.md` and is referenced (not redefined). Command state model and non-collapse invariant cited from §5.3/§5.2, not re-enumerated.
- **UX design-spec anchors.** All cited headings (Defining Experience, Platform Strategy, Experience Principles, Journey 3, Journey 4, Consequence Preview, Experience Mechanics, Flow Optimization Principles, Button Hierarchy, Concurrency and Recovery Cases, Journey Invariants, Implementation Story Rules, RemoveUserFromTenant Command State Model, Command Lifecycle Panel, Feedback Patterns, Accessibility) exist in `ux-design-specification.md`.
- **Backend guardrails vs `project-context.md`.** (1) Last-owner removal "allowed by design; UI warns, never a backend block" matches project-context lines 148/572 — no invented "≥1 owner" invariant. (2) `RemoveUserFromTenant` (tenants domain) kept strictly distinct from `RemoveGlobalAdministrator` (`global-administrators` singleton, backlog `ui-15`) — matches project-context lines 129–130. (3) RFC 7807 mapping via `RejectionToHttpStatusMapper` (404/409/422) matches line 198. (4) `/api/v1/commands` canonical with `/api/commands` recorded as alias-to-confirm — consistent with the accepted Story 9.1 "Command Endpoint Route Evidence" decision and the backlog's literal `endpoint:POST /api/v1/commands`.
- **Read-endpoint sourcing.** `GET /api/tenants/{tenantId}/users` and `GET /api/tenants/{tenantId}` exist (project-context line 195); the parenthetical handler `GetTenantUsersQuery` is a real type (`src/Hexalith.Tenants.Contracts/Queries/GetTenantUsersQuery.cs`). No new read/consequence/command-status endpoint proposed.
- **AC coverage.** AC1 §1, AC2 §2 (10-item preview + fail-closed incomplete-input block), AC3 §3 (last-owner warning, global-admin platform-flag, casual-action prohibition), AC4 §4 (local hints vs confirmed truth, pre-submit validation, state model referenced), AC5 §5 (five outcomes + concurrency table + safe-localized rejection + recovery paths + SignalR nudge-only + no-leak). Traceability table §9 is consistent.
- **Change-boundary / validation.** `git diff --check` clean; `git diff --submodule=short` empty for all root submodules; only tracked change is the new `docs/` markdown. No source, package, lockfile, generated, endpoint/command/query, UI-screen, or Phase 1 gate changes.

### Findings

- No CRITICAL, HIGH, or MEDIUM findings.
- **LOW (non-blocking, not fixed):** §10 "Backend and Data Boundaries" appears after §9 "Acceptance Criteria Traceability"; ordering is slightly unconventional but harmless and self-consistent. No action required.

All 11 task groups marked `[x]` are substantiated by corresponding spec content. Decision: 0 critical issues → **done**.

## Change Log

| Date | Change |
| --- | --- |
| 2026-06-02 | Created Story 9.4 (planning-only) via create-story context engine. Status → ready-for-dev. Sprint-status `9-4-specify-the-removeuserfromtenant-command-capable-journey` → ready-for-dev. |
| 2026-06-02 | Dev-story execution: authored `docs/tenants-ui-remove-user-from-tenant-journey-spec.md` (AC1–AC5, planning-only). Documentation validation passed (`git diff --check` clean, no submodule/code/package changes). All tasks checked. Status → review. Sprint-status `9-4-…-command-capable-journey` → review. |
| 2026-06-02 | Senior Developer Review (AI): adversarial validation of all AC/source cross-references, verbatim backlog arrays, dependency readiness, 9.3/UX section anchors, and backend guardrails — 0 critical/high/medium findings (1 non-blocking LOW noted). Status → done. Sprint-status `9-4-…-command-capable-journey` → done. |
