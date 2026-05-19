# Story 12.1: FrontShell Dependency Map for Tenants UI

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a product owner planning Phase 2 Admin UI work,
I want each Tenants UI screen mapped to its required FrontShell components, hooks, and tokens,
so that UI implementation starts only when required cross-project dependencies are known and sequenced.

## Acceptance Criteria

1. Given the Tenants UX specification lists Phase 2 screens, when the dependency map is created, then each screen maps to its required FrontShell or current `Hexalith.FrontComposer` components, hooks, layout capabilities, tokens, and documentation or Storybook references.
2. Given a screen depends on `<AuditTimeline>`, `<ConsequencePreview>`, command pending state, concurrent command support, toast batching, layout variants, or design tokens, when the dependency map is reviewed, then the dependency is captured with owning project, expected deliverable, readiness status, and source evidence.
3. Given a Tenants UI story is drafted, when it consumes a FrontShell deliverable, then the story references the matching dependency-map entry instead of silently assuming the deliverable exists.
4. Given a dependency is not yet available, when a Tenants UI story would require it, then the story is blocked, marked planning-only, or scoped to an explicitly approved fallback by product and UX.
5. Given the dependency map is complete, when Phase 2 planning starts, then backend MVP stories remain unblocked and UI dependencies are not promoted into Phase 1 scope accidentally.
6. Given the current FrontShell implementation is represented by the `Hexalith.FrontComposer` submodule, when the map names FrontShell dependencies, then it uses current repository names and paths where available and calls out UX-spec legacy names only as aliases.
7. Given dependency evidence includes source paths, when the map is committed, then it does not require initializing or updating nested submodules and does not copy generated build artifacts, secrets, local absolute paths, or environment-specific evidence into the document.

## Tasks / Subtasks

- [ ] Inventory Tenants UI screens and backend surfaces. (AC: 1, 5)
  - [ ] Read `_bmad-output/planning-artifacts/ux-design-specification.md` and extract the Phase 2 screens: Tenant List, Tenant Detail, Create Tenant, User Management, User Search, Tenant Configuration, Audit Trail, and Global Admin Management.
  - [ ] Record each screen's backend surface from the UX spec and PRD: `ListTenantsQuery`, `GetTenantQuery`, `GetTenantUsersQuery`, `GetUserTenantsQuery`, `GetTenantAuditQuery`, tenant lifecycle commands, member-role commands, tenant configuration commands, and global administrator commands.
  - [ ] Mark the output as Phase 2 planning/readiness only; do not add UI implementation tasks to backend MVP stories.
- [ ] Build the dependency-map artifact. (AC: 1, 2, 3, 4, 6)
  - [ ] Create `docs/tenants-ui-frontcomposer-dependency-map.md` unless an existing Phase 2 dependency-map document is discovered and is more appropriate.
  - [ ] Include one row per Tenants UI screen with columns for screen, user workflow, backend surface, required FrontComposer deliverables, readiness status, fallback decision, blocked-by reference, and evidence source.
  - [ ] Use current project terminology: `Hexalith.FrontComposer` for the checked-out submodule, and mention `FrontShell` only where quoting or aliasing older UX/planning language.
  - [ ] For every dependency, classify readiness as `available`, `needs-confirmation`, `missing`, `planned`, or `approved-fallback`; do not use vague values like `TBD` without a named decision owner.
  - [ ] Include an explicit "not a Phase 1 blocker" statement for missing UI-only dependencies.
- [ ] Map required FrontComposer components and hooks. (AC: 1, 2, 6)
  - [ ] Map existing or planned table/list dependencies to current FrontComposer primitives such as projection rendering, DataGrid support, empty/loading placeholders, authorized command regions, command feedback, and projection connection status where those paths exist.
  - [ ] Capture UX-specified new or unconfirmed dependencies: `<AuditTimeline>`, `<ConsequencePreview>`, command pending identifiers or equivalent pending-command state, concurrent command support, toast batching, `PageLayout` full-width/constrained behavior, role/status design tokens, timeline connector token, and consequence panel token.
  - [ ] If the current FrontComposer implementation uses different names, record the current type/path and the UX alias together instead of inventing a new component name.
  - [ ] Do not assert Storybook coverage exists unless a real Storybook or documentation path is found; mark it `missing` or `needs-confirmation` otherwise.
- [ ] Capture screen-by-screen fallback and blocking policy. (AC: 3, 4, 5)
  - [ ] For each missing dependency, state whether the corresponding Tenants UI story should be blocked, planning-only, or allowed to proceed with a named fallback.
  - [ ] Require product and UX approval for fallbacks to `<AuditTimeline>`, `<ConsequencePreview>`, three-phase command feedback, role/status token gaps, or layout variants.
  - [ ] State that backend query, projection, authorization, and deployment stories remain independent of FrontComposer UI readiness unless a later scope decision explicitly promotes Admin UI work.
- [ ] Add source evidence and review checklist. (AC: 2, 3, 7)
  - [ ] Link each dependency-map section to source documents such as `_bmad-output/planning-artifacts/ux-design-specification.md`, `_bmad-output/planning-artifacts/epics.md`, `Hexalith.FrontComposer/_bmad-output/project-context.md`, and relevant FrontComposer planning/source paths.
  - [ ] Include a short checklist for future Tenants UI story authors: every UI story must cite dependency-map IDs, carry `blockedBy` for missing FrontComposer deliverables, and avoid new backend requirements unless backed by completed backend story evidence.
  - [ ] Keep evidence sanitized: no local machine paths in the document body, no generated `bin/` or `obj/` artifacts, no token or tenant/user production data, and no copied private configuration.
- [ ] Validate the dependency map. (AC: 1-7)
  - [ ] Review the final document against this story's acceptance criteria.
  - [ ] Confirm the document does not change source code, package versions, submodule pointers, or Phase 1 backend scope.
  - [ ] Record the created/updated files and any unresolved dependency decisions in this story's Dev Agent Record.

## Dev Notes

### Phase 2 Scope Boundary

- Epic 12 is explicitly a Phase 2 planning/readiness and dependency-governance epic. It should not be counted as shipped Admin UI product behavior, and it should not block Phase 1 backend/package/documentation MVP work. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12: Phase 2 Admin UI Dependency Sequencing`]
- Story 12.1 creates a dependency map, not a UI implementation, component library implementation, backend endpoint, or FrontComposer feature. Later Phase 2 UI implementation stories should consume this map and declare `blockedBy` entries. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.1: FrontShell Dependency Map for Tenants UI`]
- The UX spec still uses `FrontShell` language; the checked-out dependency in this repository is `Hexalith.FrontComposer`. Treat `FrontShell` as the legacy/product name and `Hexalith.FrontComposer` as the current source repository name when citing implementation evidence. [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md`]

### Required Screen Inventory

- The dependency map should cover the Tenants UX screens listed in the UX specification: Tenant List, Tenant Detail, Create Tenant, User Management, Tenant Configuration, Audit Trail, My Tenants/User Search, and Global Admin Management. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Screen Inventory`; `_bmad-output/planning-artifacts/ux-design-specification.md#Revised Screen Inventory`]
- Backend surfaces already exist in the product/planning scope: list tenants, tenant detail, tenant users, user-tenants lookup, tenant audit query, tenant lifecycle commands, user-role commands, tenant configuration commands, and global administrator commands. Do not create duplicate backend requirements in this story. [Source: `_bmad-output/planning-artifacts/prd.md#Tenant Discovery & Query`; `_bmad-output/planning-artifacts/epics.md#Follow-Up FR Coverage Map`]
- User Search authorization has product/architecture nuance: self plus owned-tenant scope was the UX recommendation, while backend stories have hardened query authorization separately. The dependency map may reference this as a UI planning concern but must not alter backend authorization policy. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Journey Patterns`; `_bmad-output/planning-artifacts/architecture.md#D11: User Search Authorization Scoping`]

### FrontComposer Dependency Categories

- Required existing or current FrontComposer concepts include command lifecycle feedback, projection rendering, projection change notifications, pending command state, authorized command regions, empty/loading placeholders, command palette/navigation, projection connection status, and layout shell behavior. Relevant source paths include `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Rendering`, `Components/EventStore`, `Components/Layout`, `Services/Feedback`, `State/PendingCommands`, and `Infrastructure/EventStore`. [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell`]
- UX-specified gaps and proposed deliverables include `<AuditTimeline>`, `<ConsequencePreview>`, `useCommand` pending identifiers, concurrent command support, toast batching, `<PageLayout>` full-width/constrained variants, role semantic tokens, `--timeline-connector-color`, and `--consequence-bg`. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#FrontShell Change Proposal - Updated Scope`; `_bmad-output/planning-artifacts/ux-design-specification.md#New Design Tokens Required`]
- Current FrontComposer planning says custom components use the `Fc` prefix and Fluent UI Blazor v5 primitives. If the dependency map proposes or references implementation names, prefer `Fc...` current naming or mark the UX component name as an alias. [Source: `Hexalith.FrontComposer/_bmad-output/planning-artifacts/ux-design-specification/component-strategy.md`]
- FrontComposer has a zero-override visual boundary: microservices should consume shell-resolved theming and component contracts, not inject CSS variables, stylesheets, or arbitrary design tokens. Tenants UI dependency planning must preserve that boundary. [Source: `Hexalith.FrontComposer/_bmad-output/planning-artifacts/ux-design-specification/visual-design-foundation.md`; `Hexalith.FrontComposer/_bmad-output/planning-artifacts/ux-design-specification/design-system-foundation.md`]

### Dependency Map Structure

- Recommended dependency IDs:
  - `FC-TBL`: table/projection list rendering and filtering/sorting surface.
  - `FC-LYT`: full-width/constrained page layout behavior.
  - `FC-CMD`: command lifecycle feedback and pending command identity support.
  - `FC-CNC`: concurrent command support and toast batching.
  - `FC-AUD`: audit timeline component or approved fallback.
  - `FC-CNS`: consequence preview component or approved fallback.
  - `FC-TOK`: role/status/timeline/consequence visual tokens.
  - `FC-A11Y`: accessibility, keyboard, live-region, reduced-motion, and forced-colors guarantees.
  - `FC-DOC`: Storybook or equivalent component documentation/reference evidence.
- The document should make missing dependencies actionable: identify owner (`Hexalith.FrontComposer`, Tenants module, product/UX decision, or backend story), expected deliverable, readiness, fallback, and the Tenants screens blocked by it.
- Keep `blockedBy` values stable and story-friendly. Future Story 12.4 depends on this map to create Phase 2 UI stories with explicit dependency references. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.4: Phase 2 UI Story Backlog with Explicit blockedBy`]

### Current Code and Repository State

- This story should not modify source under `src/`, tests, package files, or FrontComposer submodule content. The expected implementation artifact is documentation plus this story's Dev Agent Record updates.
- If implementation discovers an existing dependency-map file, update that file instead of creating a duplicate. Preserve the same source-evidence and readiness-classification requirements.
- Do not initialize or update nested submodules recursively. Reading the current `Hexalith.FrontComposer` root-level submodule is sufficient for this planning story. [Source: `AGENTS.md`]

### Files Likely To Update

- `docs/tenants-ui-frontcomposer-dependency-map.md`: likely new dependency map for Phase 2 Tenants UI planning.
- `_bmad-output/implementation-artifacts/12-1-frontshell-dependency-map-for-tenants-ui.md`: update Dev Agent Record after implementation.
- `_bmad-output/implementation-artifacts/sprint-status.yaml`: already updated by create-story workflow to mark this story `ready-for-dev`; later implementation should not change unrelated statuses.

### Testing / Validation Requirements

- No code test suite is required if only documentation is created.
- Validate by manual checklist:
  - Every UX screen has a dependency row.
  - Every FrontComposer dependency has owner, readiness, fallback/blocking policy, and evidence.
  - Missing dependencies are not promoted into Phase 1 backend scope.
  - No source code, package versions, submodule pointers, generated build artifacts, or secrets changed.
  - Future UI stories can cite stable dependency IDs and `blockedBy` references.

### Previous Story Intelligence

- Story 11.3 is the nearest previous story in document order and shows the preferred pattern for planning stories that verify inherited contracts: state prerequisite boundaries, distinguish local evidence from production promises, and record residual risks instead of silently expanding scope. [Source: `_bmad-output/implementation-artifacts/11-3-deployment-auth-readiness-documentation-and-smoke-tests.md`]
- Stories 9 through 11 hardened backend queries, projection write safety, and auth readiness. This story must not reopen those backend decisions; it maps UI dependencies on completed or planned backend surfaces. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- Story 10.4 shows how to keep future implementation stories gated on prerequisite contracts and avoid speculative tests or implementation when dependencies are not stable. Apply the same pattern to missing FrontComposer deliverables. [Source: `_bmad-output/implementation-artifacts/10-4-projection-write-conformance-and-recovery-tests.md`]

### Latest Technical Information

- `Hexalith.FrontComposer` project context currently pins .NET SDK `10.0.300`, Fluent UI Blazor `5.0.0-rc.2-26098.1`, Fluxor `6.9.0`, Aspire `13.2.1`, Playwright `^1.49.0`, and Node `>=24.0.0`. This story should not update any of those dependencies. [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md`]
- FrontComposer current implementation is Blazor/Fluent UI based, not React. UX text that mentions `useCommand` should be mapped to the current FrontComposer command lifecycle and pending-command services unless a later FrontComposer story explicitly provides a hook with that name. [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/State/PendingCommands`; `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Services/Feedback`]
- Storybook coverage is a UX requirement for reusable component references, but current local evidence should be verified before claiming it exists. If no Storybook path exists, mark documentation/reference evidence as `missing` or `needs-confirmation` in the map. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Component Quality Gates`]

### Project Context Reference

- Follow `AGENTS.md`: do not initialize or update nested submodules recursively; use Conventional Commits for commits.
- Follow repository conventions for documentation: plain Markdown, source-path evidence, no secrets or local-machine evidence, and no unrelated generated-artifact churn.
- No root application `project-context.md` exists. Submodule project contexts are reference context only and should not override this application's `AGENTS.md`.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

### File List

