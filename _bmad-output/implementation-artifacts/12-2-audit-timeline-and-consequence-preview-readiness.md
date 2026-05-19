# Story 12.2: Audit Timeline and Consequence Preview Readiness

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a UX/product owner planning the Tenants Admin UI,
I want audit and consequence-preview component dependencies resolved before dependent screens are scheduled,
so that high-risk access-management workflows have the right interaction patterns available when implementation begins.

## Acceptance Criteria

1. Given the Audit Trail screen or tenant detail audit tab is planned, when readiness is assessed, then the story declares a dependency on the FrontComposer `<AuditTimeline>` deliverable, a current `Hexalith.FrontComposer` source-backed equivalent, or an explicitly approved fallback.
2. Given a remove-user, disable-tenant, remove-global-admin, or high-impact configuration workflow is planned, when readiness is assessed, then the story declares a dependency on `<ConsequencePreview>`, a current source-backed equivalent, or an explicitly approved fallback.
3. Given `<AuditTimeline>` is required for first-slice flat timeline mode, when readiness is documented, then flat timeline behavior, event ordering, keyboard interaction, screen-reader behavior, loading/empty/error states, and the 500-event performance target are defined.
4. Given grouped-by-session audit mode is not required for the first UI slice, when backlog sequencing is reviewed, then grouped mode is marked as fast-follow and does not block the flat timeline story.
5. Given consequence previews use already-loaded projection data, when remove/disable workflows are designed, then stories confirm no dedicated backend consequence endpoint is required unless product explicitly changes scope.
6. Given the current checked-out UI dependency is `Hexalith.FrontComposer`, when this story records FrontShell dependencies, then it uses current repository names and paths where available and treats UX names such as `FrontShell`, `@hexalith/ui`, `<AuditTimeline>`, and `<ConsequencePreview>` as aliases until verified.
7. Given Story 12.1 defines dependency IDs, when this story records readiness decisions, then it updates or references the same stable IDs, including `FC-AUD`, `FC-CNS`, `FC-CMD`, `FC-CNC`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`, instead of creating prose-only blockers.
8. Given current FrontComposer evidence is incomplete, stale, or unavailable, when readiness is recorded, then the dependency is marked `needs-confirmation`, `missing`, `planned`, or `approved-fallback` with an owner and evidence source rather than inferred from desired UX language.
9. Given Phase 2 UI stories will consume this readiness work, when the output is complete, then dependent UI stories can copy exact `blockedBy` values and fallback decisions without re-reading the full UX specification.
10. Given Epic 12 is Phase 2 planning/readiness scope, when this story is implemented, then no backend endpoint, command, query, package, source code, submodule pointer, or Phase 1 release gate is changed by this story.

## Tasks / Subtasks

- [ ] Locate and extend the dependency-map output from Story 12.1. (AC: 1, 2, 5, 7, 9)
  - [ ] Read `docs/tenants-ui-frontcomposer-dependency-map.md` if it exists; otherwise create `docs/tenants-ui-audit-consequence-readiness.md` as the focused readiness output and clearly cross-reference the Story 12.1 map.
  - [ ] Reuse Story 12.1 dependency IDs and readiness vocabulary; do not introduce duplicate IDs for the same FrontComposer deliverables.
  - [ ] For every readiness row, include dependency ID, UX alias, current FrontComposer source path if verified, owner, expected deliverable, readiness, fallback/blocking policy, evidence, and Phase 1 blocker status.
  - [ ] Mark this story as Phase 2 planning/readiness only and explicitly state that it does not implement Tenants UI screens.
- [ ] Resolve `<AuditTimeline>` readiness. (AC: 1, 3, 4, 6, 8)
  - [ ] Define first-slice flat timeline behavior: date-range-filtered tenant audit events, stable chronological ordering, event category/actor/target/role/status display, expandable details, empty state, and error state.
  - [ ] Define loading behavior from the UX spec: content-aware audit skeleton, three visible groups with two skeleton events each, connector line visible, 300ms minimum display, and crossfade per group.
  - [ ] Define accessibility expectations: semantic ordered list or table alternative, keyboard navigation with up/down and Enter expand/collapse, focus visibility, screen-reader announcements, reduced-motion behavior, forced-colors behavior, and no color-only encoding.
  - [ ] Define performance evidence needed for a 500-event timeline: bounded rendering target, expected fixture size, whether virtualization is required, and where benchmark or component-test evidence should live when the FrontComposer component is implemented.
  - [ ] Mark grouped-by-session mode as fast-follow unless product/UX explicitly promotes it; include a separate readiness row for grouped mode so it does not block flat timeline planning.
  - [ ] If no current `Hexalith.FrontComposer` component path exists for audit timeline behavior, record `evidence: missing` or `needs-confirmation` with FrontComposer as owner instead of inventing a path.
- [ ] Resolve `<ConsequencePreview>` readiness. (AC: 2, 5, 6, 8)
  - [ ] Define covered workflows at minimum: remove user from tenant, disable tenant, remove global administrator, and any high-impact configuration change that product/UX classifies as irreversible or high impact.
  - [ ] Define preview inputs from already-loaded projection/read-model data: tenant status, member counts, affected user/role, global-admin count, configuration key context, and last-known audit context where available.
  - [ ] State that no dedicated backend consequence endpoint is required for this story; any new endpoint requires a future product/architecture scope decision.
  - [ ] Define fallback rules for missing component support: block dependent UI story, planning-only story, or explicitly approved inline copy/dialog fallback with product/UX owner.
  - [ ] Define accessibility expectations: ordered consequences, `role="alert"` or equivalent assertive/polite behavior as appropriate, keyboard reachability, focus return, screen-reader order, and no destructive action hidden behind color alone.
  - [ ] If no current `Hexalith.FrontComposer` component path exists for consequence preview behavior, record `evidence: missing` or `needs-confirmation` with FrontComposer/product/UX ownership rather than asserting availability.
- [ ] Align command-feedback and data-source boundaries. (AC: 2, 5, 7, 10)
  - [ ] Map readiness dependencies to Story 12.1 IDs: `FC-CMD` for command lifecycle feedback, `FC-CNC` for concurrent command/toast batching, `FC-TOK` for risk/status tokens, `FC-A11Y` for accessibility, `FC-L10N` for copy/localization readiness, and `FC-DOC` for component documentation evidence.
  - [ ] Treat UX terms such as `useCommand pendingIds` as aliases; map them to current Blazor/FrontComposer pending-command and feedback concepts if verified.
  - [ ] Do not add backend consequence queries, command endpoints, or new projection fields. Use completed backend/query evidence for `GetTenantAuditQuery`, tenant details, tenant users, global administrators, and configuration where already present.
  - [ ] Where data is not already loaded by a planned UI screen, record a dependency or deferred decision rather than expanding backend scope.
- [ ] Capture sequencing output for future Phase 2 UI stories. (AC: 4, 7, 8, 9)
  - [ ] Add exact `blockedBy` examples for Audit Trail, Tenant Detail audit tab, User Management remove-user flow, Global Admin remove flow, Disable Tenant flow, and high-impact configuration changes.
  - [ ] For each future story, state whether it is ready, blocked, planning-only, or allowed with approved fallback.
  - [ ] Name decision owners for unresolved fallback choices: Product/UX for interaction fallback, FrontComposer for reusable component/API evidence, Tenants module for screen composition, and backend only when a completed backend surface is missing.
  - [ ] Keep grouped audit mode and advanced analytics/anomaly scoring out of the first slice unless explicitly promoted.
- [ ] Validate and record implementation evidence. (AC: 1-10)
  - [ ] Confirm the readiness output cites repo-relative paths only and avoids local absolute paths, secrets, token values, raw tenant/user production data, generated `bin/`/`obj/` artifacts, and transient logs.
  - [ ] Confirm every dependency row has readiness, owner, evidence, fallback/blocking policy, and Phase 1 blocker status.
  - [ ] Confirm the output can be consumed by Story 12.4 to generate concrete UI stories with explicit `blockedBy` values.
  - [ ] Confirm no source code, package files, submodule pointers, or backend story statuses changed.
  - [ ] Record created/updated files and unresolved decisions in this story's Dev Agent Record.

## Dev Notes

### Phase 2 Scope Boundary

- Epic 12 is Phase 2 planning/readiness and dependency governance. It is not Phase 1 backend implementation scope and should not be counted as shipped Admin UI product behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12: Phase 2 Admin UI Dependency Sequencing`]
- This story resolves UI dependency readiness for high-risk audit and destructive-action workflows. It must not implement UI screens, FrontComposer components, backend endpoints, or command/query contracts. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.2: Audit Timeline and Consequence Preview Readiness`]
- Backend MVP stories remain unblocked by missing UI-only dependencies. Missing `<AuditTimeline>` or `<ConsequencePreview>` support should block or defer dependent Phase 2 UI stories, not backend package/release work. [Source: `_bmad-output/planning-artifacts/prd.md#MVP Feature Set (Phase 1)`]

### Story 12.1 Dependency Contract

- Story 12.1 is the immediate prerequisite and defines the dependency-map contract. Reuse its stable dependency IDs and evidence rules instead of creating a parallel readiness taxonomy. [Source: `_bmad-output/implementation-artifacts/12-1-frontshell-dependency-map-for-tenants-ui.md`]
- Expected IDs for this story include:
  - `FC-AUD`: audit timeline component or approved fallback.
  - `FC-CNS`: consequence preview component or approved fallback.
  - `FC-CMD`: command lifecycle feedback and pending command identity.
  - `FC-CNC`: concurrent command support and toast batching.
  - `FC-TOK`: role/status/timeline/consequence visual tokens.
  - `FC-A11Y`: keyboard, live-region, reduced-motion, forced-colors, and accessibility guarantees.
  - `FC-L10N`: localization, culture-aware formatting, adopter terminology, and translation readiness.
  - `FC-DOC`: Storybook or equivalent component documentation/reference evidence.
- Use current repository terminology. The UX spec says `FrontShell` and `@hexalith/ui`; the checked-out submodule is `Hexalith.FrontComposer`, whose components use `Fc...` naming and Fluent UI Blazor patterns. [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md`; `_bmad-output/implementation-artifacts/12-1-frontshell-dependency-map-for-tenants-ui.md`]

### Audit Timeline Readiness

- The Audit Trail screen uses `GetTenantAuditQuery` for temporal event view with date-range filtering. The Tenant Detail page also has an audit tab. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Screen Inventory`]
- FR29 requires global administrators to query tenant access changes by tenant ID and date range for audit reporting, with cursor pagination default page size 100 and maximum 1,000. [Source: `_bmad-output/planning-artifacts/prd.md#Tenant Discovery & Query`]
- UX specifies `<AuditTimeline>` as a must-ship flat-view FrontShell change proposal and grouped mode as fast-follow. Do not make grouped-by-session mode a first-slice blocker. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Roadmap`; `_bmad-output/planning-artifacts/epics.md#Story 12.2: Audit Timeline and Consequence Preview Readiness`]
- The first timeline slice should define flat chronology, date filtering, event category/actor/target rendering, expandable details, empty state, and error state. Use `AuditNarrative` as Tenants-module composition guidance only; do not require a FrontComposer implementation if evidence is missing. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Roadmap`]
- Loading behavior is explicit: audit timeline skeleton has three skeleton session groups with two skeleton events each, visible connector line, 300ms minimum display, and crossfade per group. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Loading State Patterns`]
- Accessibility expectations include keyboard navigation with up/down between audit events and Enter to expand/collapse detail, semantic structure, visible focus, screen-reader announcements, and no color-only encoding. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Keyboard Navigation Map`; `_bmad-output/planning-artifacts/ux-design-specification.md#WCAG 2.1 AA Compliance`]
- Component quality gates require Playwright component tests, axe-core accessibility checks, Storybook or equivalent reference documentation, visual coverage for reusable UI components, and a performance benchmark for `<AuditTimeline>` at 500 events. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Component Quality Gates`]

### Consequence Preview Readiness

- UX identifies `<ConsequencePreview>` as a should-ship FrontShell change proposal used by revoke-user and disable-tenant flows, with risk escalation for irreversible or high-impact actions. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Roadmap`; `_bmad-output/planning-artifacts/ux-design-specification.md#Confirmation Pattern Hierarchy`]
- Remove-user, disable-tenant, remove-global-admin, and high-impact configuration flows should declare dependency on `FC-CNS` or an approved fallback. Fallback approval is a product/UX decision, not an implementation convenience.
- Consequence previews must use already-loaded projection/read-model data. The architecture records consequence preview data flow as client-side with no new backend endpoint. [Source: `_bmad-output/planning-artifacts/architecture.md#UX-Driven Amendments (2026-03-25)`; `_bmad-output/planning-artifacts/epics.md#Story 12.2: Audit Timeline and Consequence Preview Readiness`]
- Consequence preview accessibility should include ordered consequences, readable escalation copy, keyboard reachability, focus management, and screen-reader order. The UX spec calls out `role="alert"` for consequence preview. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#WCAG 2.1 AA Compliance`; `_bmad-output/planning-artifacts/ux-design-specification.md#Screen Reader Announcements`]
- Destructive and irreversible workflows should follow the confirmation hierarchy. Reversible single-item actions may use optimistic action plus undo toast, while irreversible high-impact actions require consequence preview inline plus confirm button and assertive register. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Confirmation Pattern Hierarchy`]

### Current FrontComposer Evidence

- Current local FrontComposer evidence includes data-grid, projection placeholder/loading, authorized command region, pending command summary, projection connection status, destructive confirmation dialog, layout, lifecycle, and feedback surfaces under `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell`. [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell`]
- Verified examples include `Components/EventStore/FcPendingCommandSummary.razor`, `Components/EventStore/FcProjectionConnectionStatus.razor`, `Components/Forms/FcDestructiveConfirmationDialog.razor`, `Components/Rendering/FcProjectionLoadingSkeleton.razor`, and `Components/Rendering/FcProjectionEmptyPlaceholder.razor`. Treat these as evidence of adjacent primitives, not proof that `<AuditTimeline>` or `<ConsequencePreview>` already exists.
- If the implementation does not find concrete audit timeline or consequence preview source paths, use `evidence: missing` or `needs-confirmation`. Do not infer readiness from package names, UX aliases, or desired component names.
- FrontComposer project context currently pins .NET SDK `10.0.300`, Fluent UI Blazor `5.0.0-rc.2-26098.1`, Fluxor `6.9.0`, Aspire `13.2.1`, Playwright `^1.49.0`, and Node `>=24.0.0`. This story should not update those dependencies. [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md`]

### Backend and Data Boundaries

- Do not add a dedicated consequence endpoint. Use already-loaded query/projection data from tenant detail, tenant users, global-admin state, configuration, and audit surfaces where available. [Source: `_bmad-output/planning-artifacts/architecture.md#UX-Driven Amendments (2026-03-25)`]
- Do not reopen completed backend authorization, query hardening, audit projection, or projection write-safety stories. This story maps readiness for UI dependencies that consume those surfaces. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- Audit query design is recorded as resolved by D12 with `TenantAuditProjection` date range and category filtering. Treat implementation status as backend evidence to cite, not a reason to add new backend scope. [Source: `_bmad-output/planning-artifacts/architecture.md#Gap Analysis Results`]
- Cross-tenant isolation remains a security requirement for any future UI story that consumes audit or role-management data. Future UI stories should cite completed backend authorization stories and maintain tenant/user context in evidence. [Source: `_bmad-output/planning-artifacts/prd.md#Security`; `_bmad-output/planning-artifacts/architecture.md#Cross-Cutting Concerns Identified`]

### Files Likely To Update

- `docs/tenants-ui-frontcomposer-dependency-map.md`: update if Story 12.1 created this artifact.
- `docs/tenants-ui-audit-consequence-readiness.md`: create only if the dependency-map artifact does not exist or a focused addendum is cleaner.
- `_bmad-output/implementation-artifacts/12-2-audit-timeline-and-consequence-preview-readiness.md`: update Dev Agent Record during implementation.
- `_bmad-output/implementation-artifacts/sprint-status.yaml`: already updated by create-story workflow to mark this story `ready-for-dev`; later implementation should not change unrelated statuses.

### Testing / Validation Requirements

- No source-code test suite is required if implementation creates or updates documentation only.
- Validate by manual checklist:
  - `FC-AUD` and `FC-CNS` have readiness, owner, evidence, expected deliverable, fallback/blocking policy, and Phase 1 blocker status.
  - Audit flat timeline behavior includes ordering, filtering, expansion, loading, empty/error states, accessibility, localization/copy, documentation evidence, and 500-event performance evidence requirements.
  - Grouped audit mode is marked fast-follow and does not block flat timeline readiness.
  - Consequence preview uses already-loaded projection/read-model data and does not introduce a backend consequence endpoint.
  - Destructive/high-impact workflows have clear fallback/approval rules and future `blockedBy` examples.
  - Every missing or unverified dependency is marked `needs-confirmation`, `missing`, `planned`, or `approved-fallback` with an owner.
  - The output uses repo-relative paths or explicit `evidence: missing`; it contains no local absolute paths, secrets, raw tenant/user production data, generated build artifacts, or transient logs.
  - No source code, package versions, submodule pointers, or Phase 1 backend scope changed.

### Previous Story Intelligence

- Story 12.1 already hardened dependency-map rules through party-mode and advanced elicitation. Reuse its stable dependency IDs, FrontComposer alias handling, non-speculative readiness states, evidence requirements, and localization/adopter-experience coverage. [Source: `_bmad-output/implementation-artifacts/12-1-frontshell-dependency-map-for-tenants-ui.md`]
- Story 11.3 shows the preferred pattern for readiness stories: separate local smoke/evidence from production promises, avoid leaking secrets or environment-specific data, and record residual risks rather than expanding scope. [Source: `_bmad-output/implementation-artifacts/11-3-deployment-auth-readiness-documentation-and-smoke-tests.md`]
- Stories 9 through 11 hardened query policy, projection safety, cancellation, and production auth readiness. This story should cite those capabilities only as consumed backend evidence for future UI stories, not reopen them.

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
