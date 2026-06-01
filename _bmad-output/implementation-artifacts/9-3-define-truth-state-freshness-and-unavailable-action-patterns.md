---
baseline_commit: 00abb6c93a8580176118a4e3c9a6d567f6da4dc8
---

# Story 9.3: Define Truth State, Freshness, and Unavailable Action Patterns

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a tenant admin UI user,
I want the interface to distinguish current, stale, pending, and blocked states,
so that I know what is true, what is delayed, and why an action is unavailable.

## Acceptance Criteria

1. **Given** the truth-state vocabulary is defined, **when** UI implementation stories use status indicators, **then** Truth State Badge states include current, refreshing, aging, stale, unknown, eligible, blocked, pending, accepted, confirmed, failed, audit pending, and audit available, **and** every state has a text label, accessible name, and non-color-only visual treatment.
2. **Given** freshness gating is specified, **when** an access-impacting action is considered, **then** the UI must show freshness label, timestamp or version marker, refresh action, and blocking reason, **and** unknown freshness fails closed for destructive actions.
3. **Given** unavailable actions are specified, **when** a high-impact action is disabled or blocked, **then** the UI exposes a visible inline reason for missing permission, stale data, missing lifecycle support, missing consequence preview, missing audit proof, or high-impact flow readiness gaps, **and** tooltips may supplement but cannot be the only explanation.
4. **Given** feedback states are specified, **when** command, projection, or audit state changes, **then** the UI distinguishes request sent, accepted, projection pending, confirmed, rejected, already applied, degraded, audit pending, audit available, and unable to verify, **and** accepted, projected, and proven states are not collapsed into one success state.
5. **Given** page-level degradation occurs, **when** feedback is displayed, **then** feedback appears close to the affected tenant, row, command panel, or audit context where possible, **and** global message bars are reserved for page-level degradation or system-wide service state.

## Tasks / Subtasks

- [x] Preserve the Epic 9 planning-only boundary before editing any artifact. (AC: 1-5)
  - [x] Confirm this story produces planning/specification documentation only — a Truth State, Freshness, and Action-Availability pattern specification for Phase 2 UI implementation stories.
  - [x] Do NOT implement Tenants Admin UI screens, FrontComposer/Fluent UI components (Truth State Badge, Freshness Gate, Unavailable Action Reason, Command Lifecycle Panel, etc.), Blazor pages/routes, backend endpoints, commands, queries, package references, generated UI files, domain-contract annotations, or submodule pointer changes. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`]
  - [x] Keep backend MVP work independent from UI dependency readiness; missing UI dependencies block or defer future Phase 2 UI rows, never backend package/release work. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- [x] Reconcile with existing Epic 9 planning artifacts instead of duplicating them. (AC: 1-5)
  - [x] Read `docs/tenants-ui-operations-shell-spec.md` §"Truth-State Vocabulary (referenced, not redefined)". Story 9.2 explicitly **defers ownership of the full truth-state vocabulary and freshness gating to Story 9.3** — this story is the canonical owner. After authoring, the 9.3 spec must be consistent with (not contradict) the shell spec's freshness primitive and per-surface freshness states.
  - [x] Read `docs/tenants-ui-frontcomposer-dependency-map.md` (Dependency ID Catalog, Read-Only Surface Consumption Map, custom-component rows) and reuse its dependency IDs verbatim — do NOT redefine `FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC`. Truth-state/freshness/feedback patterns map primarily to `FC-TOK` (status tokens), `FC-CMD`/`FC-CNC` (command lifecycle + concurrency), `FC-A11Y` (no-color-only / live status), `FC-L10N` (localized state labels), and `FC-DOC`.
  - [x] Read `docs/tenants-ui-phase-2-story-backlog.md` and align the pattern spec to the candidate rows that consume truth-state patterns: read-only freshness on `ui-01`…`ui-06`, command lifecycle/feedback on `ui-07`…`ui-10`, `ui-13`…`ui-15`, and audit-evidence states on `ui-11`/`ui-12`. Do NOT invent new dependency IDs, new column names, or new `ui-NN` keys.
  - [x] Decide output location: create a new `docs/tenants-ui-truth-state-and-action-availability-spec.md` (no truth-state/action-availability spec exists today; `docs/` has only the dependency map, operations-shell spec, and Phase 2 backlog for UI). If you instead extend the shell spec, justify why a new artifact is not warranted and avoid contradicting the existing source-of-truth language.
- [x] Specify the Truth State Badge vocabulary canonically. (AC: 1)
  - [x] Enumerate the AC1 badge states exactly: current, refreshing, aging, stale, unknown, eligible, blocked, pending, accepted, confirmed, failed, audit pending, audit available. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Truth State Badge`]
  - [x] Group the states by the five truth-state dimensions (freshness, authorization, command lifecycle, projection confirmation, audit evidence) so implementation stories cannot reinterpret "current"/"accepted"/"confirmed"/"audited" per screen. [Source: `#Truth State Model`]
  - [x] Require every state to carry a text label, an accessible name, and a non-color-only visual treatment (text + icon/shape), working in forced-colors mode. [Source: `#Truth State Badge`; `#Accessibility`]
  - [x] Require all state labels, role names, timestamps, warnings, and disabled reasons to be localizable (no runtime sentence-fragment assembly). Tie to `FC-L10N` and `FC-TOK`. [Source: `#Localization`; `#Component Strategy`]
- [x] Specify freshness gating for access-impacting actions. (AC: 2)
  - [x] Require the Freshness Gate to show: freshness label, timestamp or projection-version marker, refresh action, and blocking reason. [Source: `#Freshness Gate`]
  - [x] Bind freshness measurement to read-model evidence only: timestamp, projection version, or ETag (`If-None-Match` → `304 Not Modified` served by `CachingProjectionActor`). If freshness cannot be measured, state is `unknown`. [Source: `_bmad-output/project-context.md#API Surface`; `docs/tenants-ui-operations-shell-spec.md#Truth-State Vocabulary (referenced, not redefined)`]
  - [x] State the fail-closed rule: unknown freshness (and indeterminate authorization, incomplete consequence preview, or missing lifecycle support) blocks destructive actions by default unless an explicitly approved override path exists. [Source: `#Flow Optimization Principles`; `#Truth State Model`]
  - [x] Clarify that SignalR projection notifications are freshness nudges only — never proof of command completion or projection consistency. [Source: `#Journey Invariants`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- [x] Specify the Unavailable Action Reason pattern. (AC: 3)
  - [x] Enumerate the reason categories exactly: missing permission, stale data, missing lifecycle support, missing consequence preview, missing audit proof, high-impact flow not ready. [Source: `#Unavailable Action Reason`]
  - [x] Require a visible inline reason for high-impact disabled/blocked actions; tooltips may supplement but cannot be the only explanation. [Source: `#Unavailable Action Reason`; AC3]
  - [x] Require unavailable high-impact actions to remain visible when the reason aids safety or understanding (do not silently hide). Separate "missing permission" from "stale data" from "blocked risk" from "unavailable implementation dependency". [Source: `#Button Hierarchy`; `#Truth State Model` (Authorization dimension)]
  - [x] Map each reason category to its evidence source (authorization → projection/query authz; stale data → freshness marker; lifecycle support → `FC-CMD`; consequence preview → `FC-CNS`; audit proof → `FC-AUD`; high-impact readiness → backlog `blockedBy`). [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`]
- [x] Specify the layered feedback state set for command/projection/audit changes. (AC: 4)
  - [x] Enumerate the AC4 feedback states distinctly: request sent (submitted), accepted, projection pending, confirmed, rejected, already applied, degraded, audit pending, audit available, unable to verify. [Source: `#Feedback Patterns`]
  - [x] State the non-collapse invariant: accepted, projected, and proven states must NOT be merged into one success state; preserve last-confirmed projection data separately from submitted/pending intent. [Source: `#Feedback Patterns`; `#Journey Invariants`; AC4]
  - [x] Reference the `RemoveUserFromTenant` command state model (`eligible → previewed → submitted → accepted → projection_pending → confirmed | failed | unknown | audit_pending | audit_available`) as the worked example, but do not redefine the Story 9.4 journey here. [Source: `#RemoveUserFromTenant Command State Model`]
  - [x] Cover the concurrency/recovery cases that change feedback state (already removed before submit, status changed while preview open, permission lost mid-flow, duplicate submit/refresh during pending, projection lag, delayed audit, SignalR nudge-only, status-lookup failure → unknown). Recovery actions must be concrete: refresh, wait, retry status lookup, inspect audit, continue read-only, request permission, start a compensating command, escalate with support-safe reference. [Source: `#Concurrency and Recovery Cases`; `#Flow Optimization Principles`]
- [x] Specify feedback placement and degradation scope. (AC: 5)
  - [x] Require feedback to appear close to the affected tenant, row, command panel, or audit context where possible. [Source: `#Feedback Patterns`; AC5]
  - [x] Reserve global message bars (`FluentMessageBar`) for page-level degradation or system-wide service state only. [Source: `#Feedback Patterns`]
  - [x] Define the degraded/unable-to-verify presentation: distinguish delayed evidence from missing implementation support; avoid success language; offer retry, inspect audit, continue read-only, or escalate. [Source: `#Empty, Loading, Stale, and Degraded States`]
- [x] Record support-safe references and automation/accessibility contracts shared with other Epic 9 stories. (AC: 1-5)
  - [x] Support-safe references for command/audit troubleshooting must never expose raw payloads, bearer tokens, stack traces, internal correlation IDs, or PII. [Source: `#Support-Safe References`; `_bmad-output/project-context.md#Logging & Telemetry`; `_bmad-output/project-context.md#Security & Sanitization`]
  - [x] Accessibility: disabled explanations, command lifecycle changes, stale/degraded states, and audit availability must be perceivable without color and announced via live status; keyboard users must be able to complete or exit every modal/preview/command flow. Tie to `FC-A11Y`. [Source: `#Accessibility`]
  - [x] Require stable selectors / component contracts (not arbitrary row text) for automation of state assertions, tied to `FC-A11Y`/`FC-DOC`. [Source: `#Search, Filtering, and Table Patterns`]
- [x] Record per-pattern consumption mapping and future-story readiness. (AC: 1-5)
  - [x] For each pattern (Truth State Badge, Freshness Gate, Unavailable Action Reason, Feedback/Command Lifecycle, Degradation Placement), name the consuming backlog rows and copy the relevant `blockedBy` dependency IDs from the backlog rather than re-deriving them. Keep every pattern `planning-only`/`blocked`; none becomes implementation-ready in this story.
  - [x] State the Implementation Story Rules a future UI story must satisfy to use these patterns: source projection/query, freshness state shown, authorization + unavailable-action reason, command lifecycle states (if dispatching), consequence inputs (if access-impacting), audit path or approved fallback, support-safe references, accessibility behavior, and localization responsibility. [Source: `#Implementation Story Rules`]
- [x] Record implementation evidence and run documentation validation. (AC: 1-5)
  - [x] Run `git diff --check` after documentation edits.
  - [x] Confirm no source code, package versions, lockfiles, generated files, submodule pointers (`git diff --submodule=short` empty for all root submodules), backend story statuses, UI screens, endpoints, commands, queries, or Phase 1 release gates changed.
  - [x] Update this story's Dev Agent Record with files changed, validation performed, and unresolved decisions.

## Dev Notes

### Scope Guard (read first)

- Epic 9 is readiness/planning-only. This story produces a **Truth State, Freshness, and Action-Availability pattern specification** — the shared truth/feedback contract that all Phase 2 UI implementation stories must consume — not shippable Admin UI. It must not route a Developer agent into product UI delivery until separate Phase 2 implementation stories are created. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`]
- Output is documentation only: the truth-state badge vocabulary, freshness-gating rules, unavailable-action reason taxonomy, layered feedback/command-lifecycle state set, and feedback-placement/degradation rules — plus per-pattern consumption mapping pointing back to existing source-of-truth artifacts. It does NOT create UI components, Blazor pages/routes, backend endpoints, commands, queries, package references, generated files, or Phase 1 release gates. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.3`]
- Backend MVP stories remain unblocked by UI dependency readiness. [Source: `_bmad-output/project-context.md#BMAD Workflow`]

### Story is the canonical owner of the truth-state vocabulary

- Story 9.2's `docs/tenants-ui-operations-shell-spec.md` §"Truth-State Vocabulary (referenced, not redefined)" **explicitly defers** the full truth-state vocabulary and freshness gating to **this story (9.3)** and the UX spec Truth State Model. This story must therefore make the vocabulary canonical and self-consistent, and must NOT contradict the shell spec's two fixed claims: (1) the freshness primitive is ETag `If-None-Match` → `304 Not Modified` served by `CachingProjectionActor`; (2) if freshness cannot be measured, the state is `unknown`. [Source: `docs/tenants-ui-operations-shell-spec.md#Truth-State Vocabulary (referenced, not redefined)`; `_bmad-output/project-context.md#API Surface`]
- The 9.2 senior review canonicalized the truth-state vocabulary by grouping states by dimension (freshness / command lifecycle / projection confirmation / audit evidence). Use that same grouping so the two docs agree. [Source: `_bmad-output/implementation-artifacts/9-2-specify-the-operations-shell-and-read-only-access-review-surfaces.md#Senior Developer Review (AI)`]

### Previous Story Intelligence (Stories 9.1 & 9.2 — done)

- **Reconcile, do not duplicate.** Story 9.1's review penalized an unnecessary parallel artifact and reverted a wrong endpoint normalization; Story 9.2 reused the dependency map and backlog as copy-forward source of truth. This spec adds a *truth-state/feedback pattern* layer on top of the existing IA spec; it references dependency IDs and backlog rows rather than redefining them. [Source: `_bmad-output/implementation-artifacts/9-1-map-fluent-ui-and-frontcomposer-dependencies-for-tenant-admin-screens.md#Senior Developer Review (AI)`; `_bmad-output/implementation-artifacts/9-2-specify-the-operations-shell-and-read-only-access-review-surfaces.md`]
- **Command endpoint route is `POST /api/v1/commands`** (FrontComposer `EventStoreOptions.CommandEndpointPath` default), not `/api/commands`. `project-context.md` records the unversioned `/api/commands` `CommandsController` as an *alias* to confirm against the deployed gateway. This spec is pattern-level and read-model-oriented; if it cites a command surface for lifecycle context, use `/api/v1/commands`. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Command Endpoint Route Evidence`]
- **Dependency ID catalog is fixed** at 10 IDs (`FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC`), each defined once. Do not add new IDs. Verified FrontComposer source buckets exist for DataGrid, Rendering, Layout, EventStore, Lifecycle, State/PendingCommands, Services/Feedback, Badges, Resources; `AuditTimeline`/`ConsequencePreview`/Storybook are absent. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Fluent UI API Verification Prerequisite`]

### Numbering hazard — two different "9-3" / "10-x" / "11-x" namespaces (read carefully)

- This story's key is the **sprint-status Epic 9 UI-planning** key `9-3-define-truth-state-freshness-and-unavailable-action-patterns`. The `backendEvidence` arrays inside `docs/tenants-ui-phase-2-story-backlog.md` reference a **separate Phase 2 backend backlog** that also uses `9-x`, `10-x`, `11-x`, `12-x` keys (e.g. `9-3-query-policy-for-disabled-tenants-and-orphan-memberships`). These are NOT this epic's stories. Do not conflate the two namespaces or mark backend rows complete based on this UI story. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`; `_bmad-output/implementation-artifacts/sprint-status.yaml`]

### Truth State Model (AC1, AC2, AC4) — canonical source

- Five truth-state dimensions, each with a user-facing question and required UI behavior: Freshness, Authorization, Command lifecycle, Projection confirmation, Audit evidence. Every journey shares this contract so screens don't reinterpret "current"/"accepted"/"confirmed"/"audited". [Source: `#Truth State Model`]
- AC1 Truth State Badge states (enumerate verbatim): current, refreshing, aging, stale, unknown, eligible, blocked, pending, accepted, confirmed, failed, audit pending, audit available. Each requires text label + accessible name + non-color-only treatment (works in forced-colors mode). [Source: `#Truth State Badge`]
- Freshness states: current, refreshing, aging, stale, unknown. Stale-data thresholds are defined by *implementation* stories using timestamp / projection version / ETag evidence; unmeasurable → `unknown` → destructive action fails closed. [Source: `#Truth State Model`; `#Freshness Gate`]
- Command-lifecycle vocabulary in the model: eligible, previewed, submitted, accepted, rejected, already applied, failed, duplicate, timeout, unknown. Feedback Patterns add: projection pending, confirmed, audit pending, audit available. Do not collapse accepted/projected/proven into one success state. [Source: `#Truth State Model`; `#Feedback Patterns`]

### Custom components this spec governs (do NOT implement them)

- **Truth State Badge** — shared vocabulary; text label required, color/icon secondary; forced-colors safe. (AC1)
- **Freshness Gate** — freshness label, timestamp/version marker, refresh action, blocking reason; unknown fails closed. (AC2)
- **Unavailable Action Reason** — reason categories: missing permission, stale data, missing lifecycle support, missing consequence preview, missing audit proof, high-impact flow not ready; visible inline, tooltip supplements only. (AC3)
- **Command Lifecycle Panel** — eligible, previewed, submitted, accepted, projection pending, confirmed, failed, unknown, audit pending, audit available; never overwrites confirmed projection data. (AC4)
- These are FrontComposer/Fluent UI *custom* components, NOT generated CRUD. Their readiness flows through `FC-CMD`, `FC-CNC`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC` (and `FC-CNS`/`FC-AUD` where consequence preview / audit proof states are referenced). [Source: `#Custom Components`; `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`]

### Pattern-to-backlog consumption mapping (copy `blockedBy` verbatim; do not re-derive)

- **Read-only freshness** (Truth State Badge freshness dimension + Freshness Gate display only): consumed by `ui-01`…`ui-06`. Representative `blockedBy`: `ui-01` `[FC-LYT, FC-A11Y, FC-L10N, FC-DOC]`; `ui-02`/`ui-03`/`ui-04`/`ui-06` `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]`; `ui-05` `[FC-LYT, FC-A11Y, FC-L10N, FC-DOC]`.
- **Command lifecycle / feedback** (Command Lifecycle Panel + Feedback Patterns + Unavailable Action Reason for lifecycle gaps): consumed by `ui-07`, `ui-08`, `ui-09` (`planning-only`) and `ui-10`, `ui-13`, `ui-14`, `ui-15` (`blocked`). Representative `blockedBy`: `ui-07`/`ui-09` `[FC-LYT, FC-CMD, FC-CNC, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]`; `ui-08` `[FC-LYT, FC-CMD, FC-CNC, FC-A11Y, FC-L10N, FC-DOC]`; `ui-10`/`ui-13`/`ui-14`/`ui-15` `[FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]`.
- **Audit-evidence states** (audit pending / audit available / delayed / unavailable / approved fallback): consumed by `ui-11`/`ui-12` (`blocked`), `blockedBy` `[FC-LYT, FC-AUD, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]`. Do not claim an `<AuditTimeline>` component exists; flat DataGrid fallback only.
- Keep every pattern `planning-only`/`blocked`; none becomes implementation-ready here. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`]

### Backend and Data Boundaries

- Truth-state evidence comes only from existing read endpoints: `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, `GET /api/tenants/{tenantId}/audit`. Cursor-based pagination only — signed, opaque, scope-bound cursors; never offset/limit. ETag `If-None-Match` → `304` is the freshness primitive. [Source: `_bmad-output/project-context.md#API Surface`]
- Do not add a backend "consequence" or "command status" endpoint, annotate immutable Tenants domain contracts, or model SignalR nudges as proof. SignalR notifications are refresh nudges only. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- Rejection events carry structured data only; user-facing text is composed at the HTTP boundary by `RejectionToHttpStatusMapper` (RFC 7807). Domain rejections must map to safe localized UI text — never expose raw payloads, stack traces, tokens, or internal exception text. [Source: `_bmad-output/project-context.md#API Surface`; `#Form Patterns`; `#Support-Safe References`]
- Global administrators live in the separate `global-administrators` domain (singleton aggregate); platform-governance truth-state is distinct from tenant membership. [Source: `_bmad-output/project-context.md#Identity Scheme`]

### Project Structure Notes

- Documentation outputs belong under `docs/` and this story's Dev Agent Record. No truth-state/action-availability spec exists yet (`docs/` has only `tenants-ui-frontcomposer-dependency-map.md`, `tenants-ui-operations-shell-spec.md`, and `tenants-ui-phase-2-story-backlog.md` for UI), so a new `docs/tenants-ui-truth-state-and-action-availability-spec.md` is the natural home. Future Phase 2 UI assets belong in a dedicated FrontComposer adapter/UI project only after readiness conversion, not in backend packages. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- Preserve root-level submodule policy. Reading `Hexalith.FrontComposer` is allowed; never initialize nested submodules or run recursive submodule commands. [Source: `CLAUDE.md#Submodule Policy`; `_bmad-output/project-context.md`]
- `_bmad-output/` is untracked — do not commit it in code PRs. Use central package management; add no package versions in this documentation story. [Source: `_bmad-output/project-context.md#BMAD Workflow`]

### Testing / Validation Requirements

- No source-code test suite is required: this story updates planning/specification documentation only. [Source: `_bmad-output/project-context.md#BMAD Workflow`]
- Required documentation validation:
  - `git diff --check`.
  - Manual AC checklist against this story (AC1 badge state set + label/accessible-name/non-color-only; AC2 freshness label/timestamp/refresh/blocking-reason + unknown-fails-closed; AC3 inline reason categories + tooltip-not-sole; AC4 layered feedback state set + no success-collapse; AC5 proximity placement + global-bar reservation).
  - Consistency check against `docs/tenants-ui-operations-shell-spec.md` (freshness primitive = ETag/304; unmeasurable = `unknown`) and `docs/tenants-ui-frontcomposer-dependency-map.md` (10 fixed dependency IDs; no new IDs).
  - Per-pattern consumption check: every pattern names the consuming backlog rows and copies a literal `blockedBy` array from the backlog; no new dependency IDs, column names, or `ui-NN` keys introduced.
  - Change-boundary check confirming no source code, package files, generated artifacts, submodule pointers, backend endpoints, commands, queries, UI screens, or Phase 1 gates changed.

### References

- `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`
- `_bmad-output/planning-artifacts/epics.md#Story 9.3: Define Truth State, Freshness, and Unavailable Action Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Truth State Model`
- `_bmad-output/planning-artifacts/ux-design-specification.md#RemoveUserFromTenant Command State Model`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Flow Optimization Principles`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Concurrency and Recovery Cases`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Story Rules`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Truth State Badge`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Freshness Gate`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Unavailable Action Reason`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Command Lifecycle Panel`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Feedback Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Empty, Loading, Stale, and Degraded States`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Support-Safe References`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Accessibility`
- `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`
- `docs/tenants-ui-operations-shell-spec.md#Truth-State Vocabulary (referenced, not redefined)`
- `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`
- `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`
- `_bmad-output/implementation-artifacts/9-2-specify-the-operations-shell-and-read-only-access-review-surfaces.md`
- `_bmad-output/project-context.md#API Surface`
- `_bmad-output/project-context.md#Identity Scheme`
- `_bmad-output/project-context.md#Logging & Telemetry`

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — create-story context engine

### Debug Log References

- 2026-06-01 - Resolved BMAD create-story customization (`resolve_customization.py` → empty prepend/append, `persistent_facts: project-context.md`, no `on_complete`); loaded config, sprint status, project context, planning artifacts (epics Epic 9 + Story 9.3, ux-design-specification Truth State Model / custom components / feedback patterns, architecture), existing UI docs (dependency map, operations-shell spec, Phase 2 backlog), and previous Stories 9.1/9.2 output.
- 2026-06-01 - Verified Story 9.3 target key from `sprint-status.yaml`: `9-3-define-truth-state-freshness-and-unavailable-action-patterns` (status `backlog`); epic-9 already `in-progress` (9.1/9.2 done), no epic status change required.
- 2026-06-01 - Confirmed no existing `docs/` truth-state/action-availability spec; Story 9.3 produces a new specification artifact and is the canonical owner of the truth-state vocabulary that Story 9.2's shell spec deferred to it.
- 2026-06-01 - Captured AC1-5 source mapping: Truth State Badge state set, Freshness Gate display + fail-closed, Unavailable Action Reason categories, layered Feedback Patterns + no-success-collapse, and feedback proximity / global-bar reservation. Mapped truth-state patterns to backlog consuming rows (`ui-01`…`ui-15`) with verbatim `blockedBy` arrays; flagged the dual `9-x`/`10-x`/`11-x` namespace hazard between sprint-status Epic 9 and the backlog `backendEvidence` keys.
- 2026-06-01 (dev-story) - Authored canonical spec `docs/tenants-ui-truth-state-and-action-availability-spec.md`. Re-read source-of-truth artifacts before writing: ux-design-specification (Truth State Model, Truth State Badge, Freshness Gate, Unavailable Action Reason, Command Lifecycle Panel, Feedback Patterns, Concurrency/Recovery, Implementation Story Rules, Accessibility, Localization, Support-Safe References), operations-shell spec §Truth-State Vocabulary, dependency map Dependency ID Reference, and the Phase 2 backlog candidate-rows table. Copied all 15 `ui-NN` `blockedBy` arrays verbatim from the backlog table (rows 68–82); they match the story Dev Notes exactly.
- 2026-06-01 (dev-story) - Validation: `git diff --check` clean; `git diff --submodule=short` for all five root submodules empty (no pointer changes); `git status --porcelain` shows only the new `docs/` spec plus untracked `_bmad-output/` artifacts — no source code, package files, lockfiles, generated files, endpoints, commands, queries, UI screens, or Phase 1 gates changed. Manual AC1–AC5 checklist passed against the authored spec; consistency confirmed against shell-spec freshness primitive (ETag/304 via `CachingProjectionActor`) and `unknown`-when-unmeasurable rule, and against the 10 fixed FC dependency IDs (no new IDs/columns/`ui-NN` keys introduced).

### Completion Notes List

- Create-story context engine analysis completed for Story 9.3.
- Existing dependency map, operations-shell spec, and Phase 2 UI backlog treated as copy-forward source of truth to prevent duplicate planning artifacts (carrying forward Story 9.1/9.2 review lessons).
- Story is explicitly scoped to documentation only and is the canonical owner of the truth-state vocabulary deferred to it by Story 9.2; it must stay consistent with the shell spec's freshness primitive (ETag/304) and `unknown`-when-unmeasurable rule.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- ✅ Authored the canonical Truth State, Freshness, and Action-Availability pattern specification (`docs/tenants-ui-truth-state-and-action-availability-spec.md`), the shared truth/feedback contract for all Phase 2 UI implementation stories.
- AC1 — §2 enumerates all 13 Truth State Badge states verbatim, groups them by the 5 truth-state dimensions, and requires text label + accessible name + non-color-only (forced-colors-safe) treatment plus localizable copy (`FC-L10N`/`FC-TOK`).
- AC2 — §3 specifies the Freshness Gate (freshness label, timestamp/projection-version marker, refresh action, blocking reason), binds freshness to read-model evidence only (ETag `If-None-Match` → `304` via `CachingProjectionActor`; unmeasurable = `unknown`), states the fail-closed rule, and confirms SignalR is a freshness nudge only.
- AC3 — §4 enumerates the 6 Unavailable Action Reason categories, requires a visible inline reason (tooltip supplements only), keeps the four reason classes distinct, and maps each reason to its evidence source.
- AC4 — §5 enumerates the 10 layered feedback states distinctly, states the non-collapse invariant (accepted/projected/proven not merged), references the `RemoveUserFromTenant` worked example without redefining Story 9.4, and tabulates the concurrency/recovery cases with concrete recoveries.
- AC5 — §6 requires proximity placement, reserves global message bars for page-level/system-wide degradation, and defines the degraded/unable-to-verify presentation.
- §7 names the consuming backlog rows per pattern and copies every `blockedBy` array verbatim from the backlog; §10 records the Implementation Story Rules a future UI story must satisfy. All patterns remain `planning-only`/`blocked` — none became implementation-ready.
- Documentation-only story: no source-code test suite required. Validation = `git diff --check` (clean), submodule-pointer check (empty), change-boundary check (only the new `docs/` spec + untracked `_bmad-output/` edits), and manual AC1–AC5 + cross-doc consistency checklist. No unresolved decisions.

### File List

- `docs/tenants-ui-truth-state-and-action-availability-spec.md` (new; review-edited) — canonical Truth State, Freshness, and Action-Availability pattern specification. Senior review applied two LOW fixes (§5.1 citation breadth; §2.2 grouping clarification).
- `_bmad-output/implementation-artifacts/9-3-define-truth-state-freshness-and-unavailable-action-patterns.md` (modified) — task checkboxes, Status → done, Dev Agent Record, Senior Developer Review (AI), File List, Change Log. (untracked planning artifact)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified) — story status → done; `last_updated`. (untracked planning artifact)

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot — adversarial AI review (story-automator non-interactive)
**Date:** 2026-06-01
**Outcome:** ✅ Approve — Status → done (0 critical/high findings after fixes)

### Scope reviewed

Documentation/planning-only story. Reviewed the sole deliverable `docs/tenants-ui-truth-state-and-action-availability-spec.md` against AC1–AC5, the four source-of-truth artifacts it claims to reconcile with (UX design specification, operations-shell spec, FrontComposer dependency map, Phase 2 story backlog), and git reality.

### Verification performed (claims validated, not assumed)

- **Git vs. File List** — `git status --porcelain` / `git diff --check` confirm the only tracked-scope change is the new `docs/` spec; submodule pointers unchanged for all five root submodules; no source, package, lockfile, generated, endpoint, command, query, UI, or Phase 1 gate change. File List is accurate.
- **AC1** — All 13 Truth State Badge states verified **verbatim** against `ux-design-specification.md#Truth State Badge` (line 971). Grouped by all five dimensions (5/2/3/1/2 = 13). Text-label + accessible-name + non-color-only/forced-colors requirements confirmed against UX spec lines 702, 973.
- **AC2** — Freshness Gate four-part content, ETag `If-None-Match`→`304` via `CachingProjectionActor` primitive, `unknown`-fails-closed, and SignalR-nudge-only all confirmed consistent with `tenants-ui-operations-shell-spec.md` (lines 23–25) and `project-context.md#API Surface`.
- **AC3** — All 6 Unavailable Action Reason categories verbatim against `ux-design-specification.md#Unavailable Action Reason` (line 987); inline-visible/tooltip-supplement rule and reason→evidence mapping confirmed.
- **AC4** — 10 layered feedback states distinct; non-collapse invariant present; `RemoveUserFromTenant` state model copied verbatim from UX spec line 864. The three states beyond the literal `#Feedback Patterns` list (`already applied`, `degraded`, `unable to verify`) were confirmed grounded in the UX spec command-trust narrative (lines 89, 249, 321) — not invented.
- **AC5** — Proximity placement + `FluentMessageBar` global-bar reservation confirmed against UX spec line ~1104 and component inventory line 948.
- **Cross-doc integrity** — All 15 `ui-NN` `blockedBy` arrays copied **verbatim** from the backlog candidate-rows table; exactly the 10 fixed FC dependency IDs (`FC-TBL/LYT/CMD/CNC/AUD/CNS/TOK/A11Y/L10N/DOC`), none invented; every `#`-anchor and cross-document reference resolves to a real heading.

### Findings

No CRITICAL, HIGH, or MEDIUM findings. Two LOW findings identified and auto-fixed during review:

- **[LOW][fixed] §5.1 citation breadth** — three of the ten AC4 feedback states were attributed only to `#Feedback Patterns`, which does not list them literally. Broadened the citation to the UX spec command-trust/stale-degraded narrative and AC4 so the layered set is fully sourced.
- **[LOW][fixed] §2.2 grouping order** — the dimension-grouped badge table preserves verbatim AC1 order, which places command-lifecycle `failed` after projection-confirmation `confirmed`. Added a clarifying note that the **Dimension** column (not row adjacency) is authoritative, removing any appearance of mis-grouping.

### Validation after fixes

`git diff --check` clean; submodule pointers still empty; change boundary unchanged (only the new `docs/` spec edited). Manual AC1–AC5 re-checklist passes; cross-doc consistency intact.

## Change Log

| Date | Change |
| --- | --- |
| 2026-06-01 | Created Story 9.3 (planning-only) via create-story context engine. Status → ready-for-dev. Sprint-status `9-3-define-truth-state-freshness-and-unavailable-action-patterns` → ready-for-dev. |
| 2026-06-01 | dev-story: authored `docs/tenants-ui-truth-state-and-action-availability-spec.md` (canonical truth-state/freshness/unavailable-action/feedback pattern contract covering AC1–AC5). Reconciled with operations-shell spec, dependency map, and Phase 2 backlog (verbatim `blockedBy` arrays; 10 fixed FC IDs; ETag/304 freshness primitive). All tasks checked; validation passed (no source/package/submodule/endpoint changes). Status → review. Sprint-status `9-3-define-truth-state-freshness-and-unavailable-action-patterns` → review. |
| 2026-06-01 | Senior Developer Review (AI): adversarial review validated AC1–AC5 verbatim against source artifacts (13 badge states, 6 reason categories, 10 feedback states, 15 verbatim `blockedBy` arrays, 10 fixed FC IDs, ETag/304 freshness primitive). 0 critical/high/medium findings; 2 LOW findings auto-fixed in the spec (§5.1 citation breadth, §2.2 grouping clarification). Outcome Approve. Status → done. Sprint-status `9-3-define-truth-state-freshness-and-unavailable-action-patterns` → done. |
