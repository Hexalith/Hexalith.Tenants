# Story 12.4: Phase 2 UI Story Backlog with Explicit `blockedBy`

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a product owner planning Phase 2 Admin UI delivery,
I want every Tenants UI story to declare its FrontComposer and backend dependencies explicitly,
so that implementation sequencing is transparent and stories do not hide unavailable prerequisites.

## Acceptance Criteria

1. Given Phase 2 Tenants UI stories are created, when a story depends on a FrontComposer deliverable, then the story includes an explicit `blockedBy` entry naming the owning FrontComposer story, dependency ID, or dependency artifact.
2. Given a UI story depends only on completed backend/query functionality, when the story is reviewed, then it references the completed backend story, endpoint, query, command, or documentation evidence instead of creating a duplicate backend requirement.
3. Given a UI story requires a backend capability that is not complete, when the backlog is reviewed, then the backend dependency is called out separately and the UI story is not marked ready for development.
4. Given Phase 2 stories are prioritized, when the backlog is ordered, then dependency-free or dependency-ready stories appear before blocked stories unless product explicitly accepts the sequencing risk.
5. Given story readiness is checked, when a UI story has unresolved `blockedBy` entries, then the story remains blocked or planning-only and cannot be assigned as implementation-ready.
6. Given Stories 12.1, 12.2, and 12.3 define dependency IDs and readiness rules, when this backlog is created, then each UI story reuses those stable IDs and readiness values instead of inventing parallel prose-only blockers.
7. Given current planning language uses `FrontShell`, `@hexalith/ui`, `useCommand`, `<AuditTimeline>`, and `<ConsequencePreview>`, when dependencies are recorded, then the backlog maps those aliases to current `Hexalith.FrontComposer` evidence or marks the dependency `needs-confirmation`, `missing`, `planned`, or `approved-fallback` with an owner.
8. Given future UI stories consume backend surfaces from Epics 9 through 11, when the backlog references those surfaces, then it cites completed backend story evidence and preserves tenant/user isolation, authorization, projection, and deployment-auth boundaries.
9. Given accessibility, localization, adopter experience, documentation, and evidence hygiene are cross-cutting FrontComposer dependencies, when every story row is drafted, then it includes applicable `FC-A11Y`, `FC-L10N`, and `FC-DOC` dependencies or explicitly explains why they do not apply.
10. Given Epic 12 is Phase 2 planning/readiness scope, when this story is implemented, then no UI screen, FrontComposer component, backend endpoint, command/query contract, package version, source code, submodule pointer, or Phase 1 release gate is changed by this story.

## Tasks / Subtasks

- [ ] Locate the Phase 2 dependency/readiness inputs. (AC: 1, 6, 7)
  - [ ] Read `docs/tenants-ui-frontcomposer-dependency-map.md` if it exists; otherwise use Story 12.1 as the dependency-map source of truth and record that the implementation output must create a backlog artifact without duplicating the dependency map.
  - [ ] Read Story 12.2 for audit timeline and consequence-preview readiness decisions.
  - [ ] Read Story 12.3 for command-feedback sequencing, degraded confirmation, and batching readiness decisions.
  - [ ] Reuse the readiness values from Story 12.1: `available`, `needs-confirmation`, `missing`, `planned`, and `approved-fallback`.
  - [ ] Reuse stable dependency IDs exactly as defined by earlier Epic 12 stories, including `FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`.
- [ ] Create the Phase 2 UI backlog artifact. (AC: 1-6, 9, 10)
  - [ ] Create `docs/tenants-ui-phase-2-story-backlog.md` unless an existing Phase 2 UI backlog document is discovered and is more appropriate.
  - [ ] Include one row per candidate UI story with mandatory fields for `storyKey`, `title`, `workflow`, `backendEvidence`, `frontComposerDependencies`, `blockedBy`, `readiness`, `sequencingPriority`, `fallbackDecision`, `decisionOwner`, and `evidenceSource`.
  - [ ] Keep `blockedBy` as an explicit array in every row; use `[]` only when backend evidence is complete, FrontComposer dependencies are available or approved-fallback, and no unresolved UX/auth/navigation/contract dependency remains.
  - [ ] Make every row copy-forward ready: blockers, evidence, fallback, owner, and sequencing must be understandable from the row without requiring future story authors to parse narrative sections.
  - [ ] Keep this artifact as planning/readiness output only; do not create new sprint-status entries, implementation story files, UI source files, or FrontComposer changes.
  - [ ] Use current terminology: `Hexalith.FrontComposer` for the checked-out submodule and `FrontShell` only as a quoted or legacy alias from UX/planning documents.
  - [ ] Use repo-relative evidence paths or explicit `evidence: missing`; do not include local absolute paths, generated build artifacts, secrets, raw tenant/user production data, bearer tokens, or transient logs.
  - [ ] Keep literal array-style fields in a copyable form, for example `blockedBy: [FC-AUD, FC-CNS]` or `blockedBy: []`; do not bury arrays inside prose notes.
- [ ] Draft the concrete UI story backlog. (AC: 1-5, 8, 9)
  - [ ] Cover the core UX screens and flows from the Tenants UX spec: Tenant List, Tenant Detail, Create Tenant, Edit Tenant, User Management, Tenant Configuration, Audit Trail, My Tenants/User Search, and Global Admin Management.
  - [ ] For each story, identify completed backend/query evidence where available: tenant list/detail/users, user-tenants lookup, tenant audit query, tenant lifecycle commands, member-role commands, tenant configuration commands, global administrator commands, auth readiness, and projection/write-safety hardening.
  - [ ] If a candidate story depends on incomplete backend functionality, mark it `blocked` or `planning-only` and name the backend owner or required story rather than adding backend work into the UI story.
  - [ ] If a candidate story depends on missing or unapproved FrontComposer deliverables, include exact `blockedBy` IDs such as `blockedBy: [FC-AUD, FC-CNS, FC-CMD, FC-CNC]` instead of broad screen-level blockers.
  - [ ] For stories that can proceed with completed backend evidence and approved FrontComposer fallbacks, state the fallback owner and approval evidence explicitly.
  - [ ] If current evidence conflicts with earlier planning aliases or readiness assumptions, mark the row `planning-only` and capture the alias/dependency owner instead of resolving the conflict silently.
- [ ] Prioritize by readiness and sequencing risk. (AC: 4, 5)
  - [ ] Sort by readiness group first, then sequencing priority inside each group; dependency-free or dependency-ready UI stories appear before stories blocked by missing FrontComposer components, backend gaps, product/UX fallback decisions, or documentation/reference evidence.
  - [ ] Separate `ready`, `ready-with-approved-fallback`, `planning-only`, and `blocked` candidate stories; do not use `ready-for-dev` for any candidate story with unresolved `blockedBy`.
  - [ ] Mark a row `ready` only when `blockedBy: []` is justified by cited evidence; mark `ready-with-approved-fallback` only when the fallback decision is named, approved, and owned.
  - [ ] Force any unresolved backend, FrontComposer, UX, auth, navigation, accessibility, localization, documentation, or contract dependency to `planning-only` or `blocked`.
  - [ ] Record any product decision to prioritize a blocked story early as an explicit sequencing-risk acceptance, with owner and rationale.
  - [ ] Keep grouped audit mode, server-side anomaly scoring, bulk provisioning, and advanced analytics out of first-slice UI backlog unless product explicitly promotes them.
  - [ ] Treat contradictory row data as validation failure: `readiness: ready` cannot coexist with a non-empty `blockedBy`, and `ready-with-approved-fallback` cannot coexist with `fallbackDecision: proposed` or `fallbackDecision: deferred`.
- [ ] Define story-template rules for future Phase 2 UI work. (AC: 1, 2, 6-9)
  - [ ] Add a short template or checklist requiring future UI stories to include `blockedBy`, backend evidence, FrontComposer dependency IDs, fallback policy, accessibility/localization/documentation coverage, and Phase 1 blocker status.
  - [ ] Require exact dependency IDs in `blockedBy`; do not allow prose-only statements such as "depends on FrontComposer readiness."
  - [ ] Require backend references to cite completed story keys, endpoints, commands, queries, or docs; do not allow duplicate backend requirements unless a product/architecture change explicitly reopens scope.
  - [ ] Require each story to preserve tenant/user context, role-aware UX, source-of-truth projection re-query behavior, and no-color-only accessibility behavior where applicable.
- [ ] Validate and record implementation evidence. (AC: 1-10)
  - [ ] Confirm every candidate UI story row has backend evidence, FrontComposer dependencies, `blockedBy`, readiness, owner, fallback/blocking policy, priority, and evidence.
  - [ ] Confirm all `blockedBy` values are arrays using stable dependency IDs such as `FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`, or explicit backend dependency/story keys when backend capability is incomplete.
  - [ ] Confirm unresolved dependencies keep candidate stories blocked or planning-only.
  - [ ] Confirm future stories can copy exact `blockedBy` arrays without rereading all Epic 12 story files.
  - [ ] Confirm the output reuses Story 12.1-12.3 IDs and readiness vocabulary.
  - [ ] Confirm alias mappings for `FrontShell`, `@hexalith/ui`, `useCommand`, `<AuditTimeline>`, and `<ConsequencePreview>` are recorded as current evidence, `evidence: missing`, deprecated/legacy alias, or deferred owner decision; do not normalize aliases into new contracts.
  - [ ] Confirm no source code, package versions, submodule pointers, backend contracts, new sprint-status story entries, or Phase 1 backend scope changed.
  - [ ] Record created/updated files and unresolved decisions in this story's Dev Agent Record.

## Dev Notes

### Phase 2 Scope Boundary

- Epic 12 is a Phase 2 planning/readiness and dependency-governance epic. It is not Phase 1 backend implementation scope and should not be counted as shipped Admin UI product behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 12: Phase 2 Admin UI Dependency Sequencing`]
- Story 12.4 creates a backlog and sequencing artifact for future UI implementation stories. It must not implement UI screens, FrontComposer components, backend endpoints, command/query contracts, package updates, or submodule changes. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.4: Phase 2 UI Story Backlog with Explicit blockedBy`]
- Candidate UI stories with unresolved dependencies should remain `blocked` or `planning-only`. Do not convert them into `ready-for-dev` sprint-status entries inside this story. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.4: Phase 2 UI Story Backlog with Explicit blockedBy`]

### Inputs From Earlier Epic 12 Stories

- Story 12.1 defines the dependency-map contract: stable dependency IDs, owner, expected deliverable, readiness, fallback/blocking policy, evidence source, and Phase 1 blocker status. Reuse it directly. [Source: `_bmad-output/implementation-artifacts/12-1-frontshell-dependency-map-for-tenants-ui.md`]
- Story 12.2 refines audit and destructive/high-impact workflow dependencies. Candidate stories for Audit Trail, tenant detail audit tab, remove-user, disable-tenant, remove-global-admin, and high-impact configuration flows should inherit `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` as applicable. [Source: `_bmad-output/implementation-artifacts/12-2-audit-timeline-and-consequence-preview-readiness.md`]
- Story 12.3 refines command feedback and degraded confirmation dependencies. Candidate stories with row-level or form-level commands should inherit `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` as applicable. [Source: `_bmad-output/implementation-artifacts/12-3-three-phase-command-feedback-sequencing.md`]
- Expected dependency IDs for future UI stories include:
  - `FC-TBL`: table/projection list rendering, filtering, sorting, empty/loading placeholders.
  - `FC-LYT`: full-width/constrained page layout and navigation behavior.
  - `FC-CMD`: command lifecycle feedback, pending identity, terminal outcomes, row/form pending state.
  - `FC-CNC`: concurrent commands, bounded pending state, duplicate observations, overflow summaries, feedback batching.
  - `FC-AUD`: audit timeline component or approved fallback.
  - `FC-CNS`: consequence preview component or approved fallback.
  - `FC-TOK`: role/status/timeline/consequence visual tokens.
  - `FC-A11Y`: keyboard, focus, live-region, reduced-motion, forced-colors, and no-color-only guarantees.
  - `FC-L10N`: localizable copy, culture-aware formatting, adopter terminology, and translation readiness.
  - `FC-DOC`: Storybook or equivalent component/reference documentation evidence.

### Candidate UI Backlog Coverage

- Cover the UX screen inventory: Tenant List, Tenant Detail, Create Tenant, Edit Tenant, User Management, Tenant Configuration, Audit Trail, My Tenants/User Search, and Global Admin Management. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Screen Inventory`]
- Tenant List and My Tenants stories consume read-heavy query surfaces and table/projection dependencies. They should reference completed query hardening and pagination work rather than creating duplicate query requirements. [Source: `_bmad-output/planning-artifacts/epics.md#Follow-Up FR Coverage Map`; `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- Tenant Detail and User Management stories must preserve role-aware information hierarchy: TenantReader, TenantContributor, TenantOwner, and GlobalAdmin represent different mental models, not only different button visibility. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Key Design Challenges`]
- Audit Trail and tenant detail audit-tab stories should reference `GetTenantAuditQuery`/FR29 and Story 12.2 audit readiness, including flat timeline first slice and grouped mode as fast-follow. [Source: `_bmad-output/planning-artifacts/prd.md#Tenant Discovery & Query`; `_bmad-output/implementation-artifacts/12-2-audit-timeline-and-consequence-preview-readiness.md`]
- Destructive or high-impact flows should declare consequence-preview dependencies and fallback approvals. Do not add a dedicated backend consequence endpoint; consequence previews use already-loaded projection/read-model data unless product/architecture changes scope. [Source: `_bmad-output/planning-artifacts/architecture.md#UX-Driven Amendments (2026-03-25)`; `_bmad-output/implementation-artifacts/12-2-audit-timeline-and-consequence-preview-readiness.md`]
- Commanding stories should distinguish optimistic command acceptance from projection confirmation. SignalR is a nudge to re-query with tenant/user/cache context, not durable projection data. [Source: `_bmad-output/implementation-artifacts/12-3-three-phase-command-feedback-sequencing.md`; `Hexalith.FrontComposer/_bmad-output/project-context.md`]

### Backend and Dependency Boundaries

- Backend stories from Epics 9 through 11 have hardened query policy, pagination, projection write safety, cancellation, JWT production auth, and tenant-claim contracts. Future UI stories should cite that evidence and avoid reopening those decisions in the backlog. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- If backend evidence is incomplete for a candidate UI story, record a separate backend dependency and leave the UI story blocked or planning-only. Do not silently introduce backend work under a UI story. [Source: `_bmad-output/planning-artifacts/epics.md#Story 12.4: Phase 2 UI Story Backlog with Explicit blockedBy`]
- The current checked-out UI dependency is `Hexalith.FrontComposer`; older UX language uses `FrontShell` and `@hexalith/ui`. Treat those as aliases unless current source evidence proves a concrete FrontComposer contract. [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md`; `_bmad-output/implementation-artifacts/12-1-frontshell-dependency-map-for-tenants-ui.md`]
- FrontComposer currently uses Blazor, Fluent UI Blazor v5, Fluxor, EventStore REST/query integration, and SignalR projection nudges. React-style terms such as `useCommand` should be mapped to current service/component/state contracts or marked unresolved with an owner. [Source: `Hexalith.FrontComposer/_bmad-output/project-context.md`; `_bmad-output/implementation-artifacts/12-3-three-phase-command-feedback-sequencing.md`]

### Output Structure Guidance

- Recommended output: `docs/tenants-ui-phase-2-story-backlog.md`.
- Recommended sections:
  - `Scope Boundary`: Phase 2 planning only; no implementation changes.
  - `Dependency ID Reference`: table of reused IDs and source story.
  - `Candidate UI Stories`: one table row per candidate story with exact `blockedBy`.
  - `Readiness Order`: ready, ready-with-approved-fallback, planning-only, blocked.
  - `Future Story Checklist`: required `blockedBy`, backend evidence, FrontComposer evidence, accessibility, localization, documentation, and evidence hygiene fields.
  - `Deferred Decisions`: product/UX, FrontComposer, backend, or Tenants-module owners for unresolved dependencies.
- Candidate story rows should be compact and copyable. A future story author should not need to parse narrative prose to find `blockedBy`, backend evidence, FrontComposer dependency status, fallback decision, owner, or sequencing rationale.
- Required candidate-row fields are `storyKey`, `title`, `workflow`, `backendEvidence`, `frontComposerDependencies`, `blockedBy`, `readiness`, `sequencingPriority`, `fallbackDecision`, `decisionOwner`, and `evidenceSource`.
- Allowed candidate-story readiness values are `ready`, `ready-with-approved-fallback`, `planning-only`, and `blocked`. The earlier dependency-map readiness values `available`, `needs-confirmation`, `missing`, `planned`, and `approved-fallback` remain valid inside dependency evidence, but do not replace the candidate-story readiness field.
- `blockedBy` must always be an array. Use `[]` only when every dependency is resolved by completed evidence or approved fallback. Rows with unresolved dependencies must remain `planning-only` or `blocked`.
- Fallback decisions must say whether they are `approved`, `proposed`, or `deferred`, and must name a decision owner role or accountable group.
- A row's readiness is invalid if its `blockedBy`, `fallbackDecision`, `decisionOwner`, or `evidenceSource` fields contradict each other; resolve the row toward `planning-only` or `blocked` rather than promoting uncertain work.
- The `Deferred Decisions` section should capture the owner, unblock condition, and affected candidate rows for every unresolved dependency so future story authors can tell what changed before promoting a row.
- Avoid local machine paths and checked-out absolute paths. Use repo-relative paths such as `_bmad-output/implementation-artifacts/12-3-three-phase-command-feedback-sequencing.md` or `Hexalith.FrontComposer/src/...`.

### Files Likely To Update

- `docs/tenants-ui-phase-2-story-backlog.md`: likely new planning artifact for candidate UI stories and `blockedBy` sequencing.
- `_bmad-output/implementation-artifacts/12-4-phase-2-ui-story-backlog-with-explicit-blockedby.md`: update Dev Agent Record during implementation.
- `_bmad-output/implementation-artifacts/sprint-status.yaml`: already updated by create-story workflow to mark this story `ready-for-dev`; later implementation should not change unrelated statuses.

### Testing / Validation Requirements

- No source-code test suite is required if implementation creates or updates documentation only.
- Docs-only verification should include reviewing the diff plus Markdown/table consistency. A build or source test run is not required unless implementation unexpectedly touches code, project files, packages, submodules, generated artifacts, sprint-status entries, backend contracts, or release gates.
- Validate by manual checklist:
  - Every candidate UI story has backend evidence, FrontComposer dependency IDs, exact `blockedBy`, readiness, owner, fallback/blocking policy, priority, and evidence.
  - Every candidate UI story row uses the required row fields and can be copied forward without consulting narrative prose for blockers or owner/fallback decisions.
  - Every `blockedBy` value is an array; `[]` appears only when evidence or approved fallback justifies it.
  - Unresolved FrontComposer or backend dependencies keep candidate stories `blocked` or `planning-only`.
  - Any unresolved UX, auth, navigation, accessibility, localization, documentation, or contract dependency prevents `ready`.
  - `ready-with-approved-fallback` rows name the approved fallback decision and decision owner.
  - Dependency-ready stories appear before blocked stories unless product accepts sequencing risk explicitly.
  - Candidate stories reuse Story 12.1-12.3 dependency IDs and readiness vocabulary.
  - Accessibility, localization, adopter copy, documentation/reference evidence, tenant/user isolation, and projection re-query behavior are represented where applicable.
  - The output uses repo-relative evidence or `evidence: missing`; it contains no local absolute paths, secrets, raw tenant/user production data, generated build artifacts, or transient logs.
  - No source code, package versions, submodule pointers, backend contracts, new sprint-status entries for candidate UI stories, or Phase 1 backend scope changed.
  - Row-level contradictions are resolved conservatively: non-empty `blockedBy`, proposed/deferred fallback, missing owner, or missing evidence prevents `ready`.
  - Deferred decisions identify affected rows, accountable owner, and the evidence required to unblock or split the future UI story.

### Previous Story Intelligence

- Story 12.1 shows how to avoid speculative dependency readiness: cite current checkout evidence, mark missing items honestly, and keep FrontComposer-owned API decisions out of Tenants story scope. [Source: `_bmad-output/implementation-artifacts/12-1-frontshell-dependency-map-for-tenants-ui.md`]
- Story 12.2 shows how to keep high-risk UI dependencies scoped to readiness and fallback decisions without implementing components or backend endpoints. [Source: `_bmad-output/implementation-artifacts/12-2-audit-timeline-and-consequence-preview-readiness.md`]
- Story 12.3 shows how to separate command acceptance, projection confirmation, degradation, and feedback batching while treating SignalR as a re-query nudge. [Source: `_bmad-output/implementation-artifacts/12-3-three-phase-command-feedback-sequencing.md`]
- Recent commits include active backend/test work on production authentication and code-structure refactoring. This story should not touch those source/test changes or the active `11-2` implementation state. [Source: `git log -5 --oneline`; `_bmad-output/process-notes/predev-preflight-latest.json`]

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

## Party-Mode Review

- Date/time: 2026-05-20T12:05:15.9068300+02:00
- Selected story key: `12-4-phase-2-ui-story-backlog-with-explicit-blockedby`
- Command/skill invocation used: `/bmad-party-mode 12-4-phase-2-ui-story-backlog-with-explicit-blockedby; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), Paige (Technical Writer)
- Findings summary:
  - Story scope is valid as a docs-only planning/readiness artifact, but the row schema needed to be mandatory enough to prevent drift.
  - `blockedBy` needed explicit array semantics so unresolved dependencies cannot be hidden in prose or accidentally treated as implementation-ready.
  - Candidate-story readiness needed a separate, auditable vocabulary from dependency-map readiness values.
  - Fallback, owner, alias, accessibility, localization, documentation, and evidence-hygiene expectations needed to be copy-forward ready inside each row.
- Changes applied:
  - Added mandatory candidate-row fields for `storyKey`, `title`, `workflow`, `backendEvidence`, `frontComposerDependencies`, `blockedBy`, `readiness`, `sequencingPriority`, `fallbackDecision`, `decisionOwner`, and `evidenceSource`.
  - Clarified `blockedBy` is always an array and `[]` is allowed only when backend evidence, FrontComposer readiness or approved fallback, and other UX/auth/navigation/contract dependencies are resolved.
  - Added readiness grouping rules for `ready`, `ready-with-approved-fallback`, `planning-only`, and `blocked`.
  - Added validation rules for unresolved backend, FrontComposer, UX, auth, navigation, accessibility, localization, documentation, and contract dependencies.
  - Clarified fallback decision wording, owner wording, alias mapping evidence, copy-forward row expectations, and docs-only verification.
- Findings deferred:
  - Whether legacy aliases become future canonical names remains a product/architecture decision.
  - Whether blocked Phase 2 UI stories should split further after backend or FrontComposer evidence changes remains deferred.
  - Final fallback approval ownership and binding force remain product/UX and FrontComposer governance decisions.
  - Whether future evidence citations require exact anchors or broader artifacts remains deferred to the implementation backlog artifact conventions.
- Final recommendation: `ready-for-dev`

## Advanced Elicitation

- Date/time: 2026-05-20T13:02:30.6264911+02:00
- Selected story key: `12-4-phase-2-ui-story-backlog-with-explicit-blockedby`
- Command/skill invocation used: `/bmad-advanced-elicitation 12-4-phase-2-ui-story-backlog-with-explicit-blockedby`
- Batch 1 method names: Expert Panel Review; Self-Consistency Validation; Red Team vs Blue Team; Pre-mortem Analysis; Critique and Refine
- Batch 2 method names: First Principles Analysis; Failure Mode Analysis; Comparative Analysis Matrix; User Persona Focus Group; Thread of Thought
- Findings summary:
  - The story was already scoped correctly as docs-only readiness work, but future implementers still needed clearer failure rules for contradictory backlog rows.
  - `blockedBy`, fallback, owner, and evidence fields needed to stay literal and copyable so future UI stories cannot accidentally inherit ambiguous prose.
  - Rows with alias conflicts, proposed fallbacks, deferred decisions, or missing evidence should degrade to `planning-only` or `blocked`.
  - Deferred decisions need owner and unblock-condition detail to avoid becoming permanent narrative notes.
- Changes applied:
  - Added literal array-field guidance for `blockedBy` and related copy-forward fields.
  - Added alias/evidence conflict handling that keeps uncertain rows `planning-only`.
  - Added contradiction rules for `readiness`, `blockedBy`, and fallback decisions.
  - Added deferred-decision guidance requiring affected rows, accountable owner, and unblock evidence.
  - Extended docs-only validation checks for row contradictions and deferred decision quality.
- Findings deferred:
  - Exact Markdown table formatting remains an implementation choice as long as the required row fields remain copyable and reviewable.
  - Final owner names for fallback approvals remain product/UX and FrontComposer governance decisions.
  - Promotion of any candidate row from `planning-only` or `blocked` remains deferred until backend and FrontComposer evidence changes.
- Final recommendation: `ready-for-dev`

### File List
