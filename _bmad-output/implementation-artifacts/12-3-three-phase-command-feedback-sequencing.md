# Story 12.3: Three-Phase Command Feedback Sequencing

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As an admin UI user,
I want command actions to show clear optimistic, confirming, and confirmed states,
so that I can trust whether tenant changes have been accepted, projected, and reflected in the interface.

## Acceptance Criteria

1. Given a Tenants UI story includes row-level or form-level commands, when the story is planned, then it declares dependencies on pending command identity, concurrent command support, projection confirmation, and toast or feedback batching where those behaviors are required.
2. Given a user submits a tenant command from the UI, when the command is accepted but projection confirmation has not arrived, then the sequencing output defines optimistic and confirming visual, accessibility, and interaction states without blocking unrelated user activity.
3. Given projection confirmation arrives through SignalR or status reconciliation, when the matching pending command is confirmed, then the sequencing output defines how row state, cache/projection data, pending state, and toast feedback settle into the confirmed state.
4. Given SignalR is disconnected, delayed, or projection confirmation cannot be matched to a pending command within the expected threshold, when degraded feedback is needed, then the output defines warning, retry/requery, needs-review, and user-context-preserving behavior.
5. Given multiple command confirmations or rejections arrive within a short burst, when feedback is shown, then the output defines consolidated/batched behavior so the user is not overwhelmed and no command outcome becomes invisible.
6. Given current UX/planning language mentions `useCommand`, `pendingIds`, and FrontShell, when this story records readiness, then it maps those aliases to current `Hexalith.FrontComposer` Blazor/Fluxor/EventStore concepts or marks the dependency `needs-confirmation` or `missing` with an owner.
7. Given Story 12.1 defines dependency IDs and Story 12.2 refines high-risk workflow dependencies, when this story records command-feedback readiness, then it updates or references the same dependency contract, especially `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`.
8. Given current FrontComposer evidence is source-backed but not necessarily an approved reusable Tenants UI contract, when readiness is recorded, then each dependency separates verified checkout evidence from reusable API readiness, owner decision, fallback policy, and Phase 1 blocker status.
9. Given future Phase 2 UI stories consume this sequencing work, when the output is complete, then those stories can copy exact `blockedBy` values, three-phase state requirements, degradation thresholds, and fallback decisions without re-reading the UX specification.
10. Given Epic 12 is Phase 2 planning/readiness scope, when this story is implemented, then no backend endpoint, command/query contract, package version, source code, submodule pointer, or Phase 1 release gate is changed by this story.

## Tasks / Subtasks

- [ ] Locate and extend the Phase 2 dependency/readiness artifact. (AC: 1, 7, 8, 9)
  - [ ] Read `docs/tenants-ui-frontcomposer-dependency-map.md` if it exists; otherwise create `docs/tenants-ui-command-feedback-sequencing.md` as the focused readiness output and clearly cross-reference Story 12.1.
  - [ ] Reuse Story 12.1 readiness values: `available`, `needs-confirmation`, `missing`, `planned`, or `approved-fallback`; do not introduce a parallel taxonomy.
  - [ ] Reuse stable dependency IDs instead of prose-only blockers: `FC-CMD` for command lifecycle and pending identity, `FC-CNC` for concurrent command/toast batching, `FC-A11Y` for accessibility behavior, `FC-L10N` for localized/adopter-facing copy, and `FC-DOC` for documentation/reference evidence.
  - [ ] Include dependency ID, UX alias, current FrontComposer source path if verified, owner, expected deliverable, readiness, fallback/blocking policy, evidence, and Phase 1 blocker status for every readiness row.
  - [ ] Mark this story as Phase 2 planning/readiness only; do not implement Tenants UI screens, FrontComposer components, or backend endpoints.
- [ ] Define the three command-feedback phases. (AC: 2, 3, 4, 6)
  - [ ] Define Phase 1 `optimistic`: local pending entry registered, affected row/form shows pending affordance, action remains reversible only where the UX pattern allows it, and unrelated rows/forms stay interactive.
  - [ ] Define Phase 2 `confirming`: command accepted/acknowledged but projection has not settled; UI shows "confirming" or equivalent pending copy, preserves user context, and avoids replacing source-of-truth projection data with speculative durable state.
  - [ ] Define Phase 3 `confirmed`: projection re-query or status reconciliation confirms the matching message/correlation, pending state clears, row/form state reflects projection data, and feedback becomes success/already-applied/rejected/needs-review as appropriate.
  - [ ] Define idempotent confirmation behavior separately from normal success so "already applied" outcomes do not imply a new mutation occurred.
  - [ ] Define rejected and needs-review outcomes with bounded, user-safe copy that explains what failed, why, and what happened to the data without exposing raw command payloads, tokens, tenant/user identifiers, stack traces, or internal IDs.
- [ ] Map current FrontComposer evidence to readiness decisions. (AC: 1, 3, 4, 6, 8)
  - [ ] Verify current source evidence before claiming readiness. Relevant paths include `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands`, `Services/Feedback`, `Infrastructure/EventStore`, `Components/EventStore`, `Components/Lifecycle`, and `Components/Rendering`.
  - [ ] Treat React-style UX terms such as `useCommand`, `pendingIds`, and `pendingOperations` as aliases until mapped to current Blazor/FrontComposer pending-command, lifecycle, feedback, and projection-notification services.
  - [ ] Record `FC-CMD` readiness for pending command identity, lifecycle states, accepted/terminal outcomes, idempotent confirmation, rejection, needs-review, and scope flush behavior.
  - [ ] Record `FC-CNC` readiness for multiple concurrent commands, bounded pending-entry caps, burst confirmations, duplicate observations, overflow/summary behavior, and toast batching or equivalent feedback consolidation.
  - [ ] Record projection confirmation readiness against EventStore REST commands/queries plus SignalR projection nudges; SignalR is a nudge to re-query, not source-of-truth projection data.
  - [ ] If a current path is adjacent evidence only, mark it as `needs-confirmation` instead of `available` and name FrontComposer as the owner of reusable API/contract readiness.
- [ ] Define degraded and disconnected behavior. (AC: 4, 8, 9)
  - [ ] Define the expected confirmation threshold or owner for choosing it. If no threshold is approved, record `needs-confirmation` with Product/UX and FrontComposer ownership rather than inventing one.
  - [ ] Define behavior for SignalR disconnected, delayed, duplicate, unknown-message, invalid-message, polling-status unavailable, and lifecycle-dispatch-failed cases.
  - [ ] Preserve action context during degradation: keep the affected row/form visible where possible, expose pending/needs-review state, allow safe re-query, and avoid hiding unresolved mutations behind generic toast copy.
  - [ ] Specify that fallback polling/status lookup is bounded and evidence-safe; do not log raw payloads or expose local absolute paths, bearer tokens, tenant/user production data, or unbounded transport details.
  - [ ] State what future UI stories should do when `FC-CMD` or `FC-CNC` is unresolved: block, remain planning-only, or use an explicitly approved fallback with named owner.
- [ ] Define batching, localization, and accessibility requirements. (AC: 2, 3, 5, 7)
  - [ ] Define burst behavior for multiple command outcomes: consolidate feedback by operation type, row/entity scope, or summary count without losing individual rejected/needs-review details.
  - [ ] Define keyboard and focus expectations for pending rows, disabled/re-enabled buttons, undo actions, feedback summaries, and reconnect summaries.
  - [ ] Define live-region behavior: optimistic/confirming/confirmed announcements should be polite; connection/degraded warnings may be assertive when they affect trust or unresolved actions.
  - [ ] Define reduced-motion and forced-colors behavior for pending/confirming indicators; no command state may rely on color alone.
  - [ ] Define localization/adopter copy requirements for command names, success, idempotent, rejected, needs-review, overflow, and degraded-connection messages.
  - [ ] Define component documentation/reference evidence expectations without claiming Storybook or equivalent docs exist unless a real path is found.
- [ ] Capture future-story `blockedBy` examples. (AC: 1, 7, 9)
  - [ ] Provide exact `blockedBy` examples for Create Tenant, Edit Tenant, Disable Tenant, Add User, Remove User, Change Role, Set/Remove Configuration, Set/Remove Global Administrator, and Audit/filter commands if applicable.
  - [ ] For each example, list required IDs separately, such as `blockedBy: [FC-CMD, FC-CNC, FC-A11Y, FC-L10N]`, rather than using broad screen-level blockers.
  - [ ] State which future stories can proceed with existing FrontComposer evidence, which remain blocked, and which require approved fallback decisions.
  - [ ] Make clear that backend command acceptance, EventStore message IDs, and projection/query behavior are consumed evidence from completed backend work unless a later product/architecture decision explicitly changes scope.
- [ ] Validate and record implementation evidence. (AC: 1-10)
  - [ ] Confirm every readiness row has owner, readiness, expected deliverable, evidence, fallback/blocking policy, and Phase 1 blocker status.
  - [ ] Confirm the three phases define UI state, projection/data behavior, accessibility, localization/copy, degraded behavior, and terminal outcomes.
  - [ ] Confirm current FrontComposer paths are cited as repo-relative evidence and not confused with approved Tenants UI API contracts.
  - [ ] Confirm the output can be consumed by Story 12.4 to create Phase 2 UI stories with explicit `blockedBy` values.
  - [ ] Confirm no source code, package versions, submodule pointers, backend contracts, or Phase 1 backend scope changed.
  - [ ] Record created/updated files and unresolved decisions in this story's Dev Agent Record.

## Dev Notes

### Phase 2 Scope Boundary

- Epic 12 is Phase 2 planning/readiness and dependency governance. It is not Phase 1 backend implementation scope and should not be counted as shipped Admin UI product behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12: Phase 2 Admin UI Dependency Sequencing`]
- This story produces command-feedback sequencing guidance and dependency readiness for future UI stories. It must not implement UI screens, FrontComposer components, backend endpoints, command/query contracts, package updates, or submodule changes. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.3: Three-Phase Command Feedback Sequencing`]
- Backend MVP stories remain unblocked by missing UI-only command-feedback dependencies. Missing `FC-CMD` or `FC-CNC` readiness should block or defer dependent Phase 2 UI stories, not backend package/release work. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Scope note (2026-05-13)`]

### Story 12.1 and 12.2 Dependency Contract

- Story 12.1 defines the dependency-map contract: stable IDs, owner, expected deliverable, readiness, fallback/blocking policy, evidence source, and Phase 1 blocker status. Reuse it rather than creating new readiness vocabulary. [Source: `_bmad-output/implementation-artifacts/12-1-frontshell-dependency-map-for-tenants-ui.md`]
- Story 12.2 already refined high-risk workflow dependencies and reused `FC-CMD`, `FC-CNC`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`. This story should extend that command-feedback line rather than duplicate audit/consequence readiness. [Source: `_bmad-output/implementation-artifacts/12-2-audit-timeline-and-consequence-preview-readiness.md`]
- Expected IDs for this story include:
  - `FC-CMD`: command lifecycle feedback, pending command identity, terminal outcome resolution, and row/form pending state.
  - `FC-CNC`: concurrent command support, bounded pending state, duplicate observation handling, overflow summaries, and toast/feedback batching.
  - `FC-A11Y`: keyboard, focus, live-region, reduced-motion, forced-colors, and no-color-only command state guarantees.
  - `FC-L10N`: localizable command names, outcome copy, degraded copy, and adopter-facing terminology.
  - `FC-DOC`: Storybook or equivalent component/reference documentation evidence.
- Use current repository terminology. The UX spec says `FrontShell`, `@hexalith/ui`, and `useCommand`; the checked-out implementation evidence is `Hexalith.FrontComposer`, Blazor/Fluent UI, Fluxor, and EventStore integration. Treat UX names as aliases until verified. [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md`]

### Three-Phase Command Feedback Model

- The UX specification requires a canonical CQRS eventual-consistency interaction pattern: optimistic -> confirming -> confirmed. It explicitly calls out command response, projection lag, short polling, SignalR live updates, and reusable interaction behavior rather than a vague "CQRS challenge." [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Key Design Challenges`]
- The planned command flow must distinguish command acceptance from projection confirmation. A command being accepted is not the same as the row/detail/cache state being refreshed from source-of-truth projection data. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.3: Three-Phase Command Feedback Sequencing`]
- SignalR projection confirmation should be treated as a nudge to re-query with tenant/user/cache context, not as durable projection data. [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md#Framework-Specific Rules`]
- The UX state model mentions `pendingOperations` and clearing matching entries when Phase 3 arrives. In current FrontComposer, map this to pending command registrations and terminal observations keyed by message/correlation metadata rather than introducing a React hook contract. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#State Management Patterns`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands`]
- Future UI stories should define at least these command outcomes: pending/confirming, confirmed, idempotent confirmed/already applied, rejected, needs review, unknown/duplicate observation ignored, and disconnected/degraded confirmation.

### Current FrontComposer Evidence

- Current pending-command models include `PendingCommandRegistration`, `PendingCommandStatus`, terminal outcomes, registration statuses, resolution statuses, and terminal observations. They intentionally exclude raw command payloads, form values, tenant IDs, user IDs, and validation messages. [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandModels.cs`]
- `PendingCommandStateService` provides bounded, circuit-local pending command state; normalizes 26-character message IDs; preserves tenant/user scope boundaries; handles duplicate terminal observations; resolves lifecycle transitions; and marks evicted/unresolved commands as `NeedsReview`. Treat it as strong evidence for adjacent `FC-CMD` behavior, while still verifying whether it is an approved reusable Tenants UI contract. [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandStateService.cs`]
- `PendingCommandPollingCoordinator` supports bounded fallback polling through `IPendingCommandStatusQuery`; the null provider returns no outcome until an adopter/EventStore status endpoint is registered. This is evidence for degraded reconciliation seams, not proof that every Tenants command has a completed status endpoint. [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands/PendingCommandPollingCoordinator.cs`]
- `FcPendingCommandSummary` renders terminal pending command summaries, most-recent terminal entries first, overflow count, confirmed/idempotent/rejected/needs-review formatting, and localizable strings. Treat this as evidence for reconnect/degraded summaries, not full toast batching by itself. [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/EventStore/FcPendingCommandSummary.razor.cs`]
- `CommandFeedbackPublisher` exists as a scoped warning publisher. It isolates subscriber faults from the command pipeline, but it does not by itself prove product-ready toast batching. [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Services/Feedback/CommandFeedbackPublisher.cs`]
- `ProjectionChangeNotifier` exposes projection changed events with optional tenant context. This is source evidence for projection nudges; future UI stories must still re-query REST/query state for durable data. [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionChangeNotifier.cs`]
- Other relevant adjacent paths include `Components/EventStore/FcProjectionConnectionStatus.*`, `Components/Lifecycle/FcLifecycleWrapper.*`, `Components/Rendering/FcAuthorizedCommandRegion.*`, and `Services/Authorization/*`. Verify paths before marking readiness as `available`.

### UX, Accessibility, and Copy Requirements

- Command feedback must keep read-heavy screens efficient. The UX spec says most screens are read-heavy with occasional writes, so command feedback should not lock unrelated user activity or force whole-screen reloads for row-level actions. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Key Design Challenges`]
- Error copy follows `[What happened] + [Why] + [What to do]`; never show stack traces, generic "Something went wrong", HTTP status codes, or internal IDs. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Error Recovery Patterns`]
- Screen-reader announcements include Phase 1 optimistic "[action] pending - confirming..." and Phase 3 "[action] confirmed" via toast, both polite. Connection issue warnings may be assertive when changes may be delayed. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Screen Reader Announcements`]
- Visible toast/feedback must be keyboard reachable, including undo actions when present. No command state may rely on color alone; reduced-motion and forced-colors behavior are part of the FrontComposer contract. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Keyboard Navigation Map`; `Hexalith.FrontComposer/_bmad-output/project-context.md#Framework-Specific Rules`]
- Localization/adopter-experience is a first-class dependency. Outcome copy must be localizable, culturally safe, bounded, and free of raw identifiers unless the product explicitly requires a user-facing email or tenant display name.

### Backend and Data Boundaries

- Do not add backend consequence, command status, query, or projection endpoints in this story. If a status endpoint is needed for complete `FC-CMD` readiness, record it as a future dependency or owner decision instead of implementing it here. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.3: Three-Phase Command Feedback Sequencing`]
- Backend command/query hardening from Epics 9 through 11 should be cited as consumed evidence only. This story must not reopen authorization, projection write safety, cancellation, JWT, or audit-query decisions. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- EventStore and FrontComposer evidence output must not include raw payloads, bearer tokens, local absolute paths, tenant/user production data, or unbounded logs. [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md#Critical Don't-Miss Rules`]

### Files Likely To Update

- `docs/tenants-ui-frontcomposer-dependency-map.md`: update if Story 12.1 created this artifact.
- `docs/tenants-ui-command-feedback-sequencing.md`: create only if the dependency-map artifact does not exist or a focused addendum is cleaner.
- `_bmad-output/implementation-artifacts/12-3-three-phase-command-feedback-sequencing.md`: update Dev Agent Record during implementation.
- `_bmad-output/implementation-artifacts/sprint-status.yaml`: already updated by create-story workflow to mark this story `ready-for-dev`; later implementation should not change unrelated statuses.

### Testing / Validation Requirements

- No source-code test suite is required if implementation creates or updates documentation only.
- Validate by manual checklist:
  - `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` have readiness, owner, evidence, expected deliverable, fallback/blocking policy, and Phase 1 blocker status.
  - The three phases define local pending state, command acceptance, projection re-query/confirmation, terminal outcomes, idempotency, rejection, and needs-review handling.
  - Degraded behavior covers SignalR disconnected/delayed, unknown/duplicate message IDs, polling unavailable, lifecycle dispatch failure, and unresolved pending commands.
  - Burst outcomes define consolidated feedback without hiding individual rejected or needs-review outcomes.
  - Accessibility covers keyboard, focus, live regions, reduced motion, forced colors, and no color-only state.
  - Localization/adopter copy covers command names, success, idempotent, rejected, needs-review, overflow, and degraded connection messages.
  - Future UI stories can copy exact `blockedBy` examples and do not rely on narrative prose.
  - Current FrontComposer source paths are repo-relative evidence and are not overclaimed as approved Tenants UI contracts.
  - No source code, package versions, submodule pointers, backend contracts, or Phase 1 backend scope changed.

### Previous Story Intelligence

- Story 12.1 established stable dependency IDs, evidence hygiene, FrontComposer alias handling, non-speculative readiness states, and localization/adopter-experience coverage. Reuse these rules directly. [Source: `_bmad-output/implementation-artifacts/12-1-frontshell-dependency-map-for-tenants-ui.md`]
- Story 12.2 shows how to focus a readiness story on high-risk UI dependencies without implementing FrontComposer or backend scope. Apply the same pattern to command feedback and degraded confirmation. [Source: `_bmad-output/implementation-artifacts/12-2-audit-timeline-and-consequence-preview-readiness.md`]
- Recent work on Story 11.1 and Story 10.4 moved backend auth and projection conformance into review/done states. Treat those as backend evidence consumed by future UI stories, not as scope to reopen in this planning story. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; `git log -5 --oneline`]

### Latest Technical Information

- `Hexalith.FrontComposer` project context currently pins .NET SDK `10.0.300`, Fluent UI Blazor `5.0.0-rc.2-26098.1`, Fluxor `6.9.0`, Aspire `13.2.1`, Playwright `^1.49.0`, TypeScript `^5.6.0`, and Node `>=24.0.0`. This story should not update any of those dependencies. [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md`]
- Current FrontComposer implementation is Blazor/Fluent UI based, not React. UX references to hooks should be translated into current service/component/state contracts or marked unresolved with owners. [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md#Technology Stack & Versions`]

### Project Context Reference

- Follow `AGENTS.md`: do not initialize or update nested submodules recursively; use Conventional Commits for commits.
- Use plain Markdown documentation with repo-relative source evidence.
- No root application `project-context.md` exists. Submodule project contexts are reference context only and should not override this application's `AGENTS.md`.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

### File List
