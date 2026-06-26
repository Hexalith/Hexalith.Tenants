---
baseline_commit: f76e0b0
created: 2026-06-06T13:32:59+02:00
---

# Story 4.1: Global Administrators Navigation and Read Contract Readiness

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 4.1. -->

## Story

As an authorized platform operator,
I want the Global Administrators area to appear only when its authorization and read contract are safe,
so that platform authority is never confused with tenant membership or exposed through an unsupported route.

## Acceptance Criteria

1. Given the Operations Shell navigation is rendered, when the caller is an authorized platform operator, then the primary navigation includes Global Administrators after Tenants and before Audit, and Users remains contextual and is not promoted to primary navigation.
2. Given the caller is a tenant owner without platform authority, when the Operations Shell navigation is rendered, then the Global Administrators area is hidden or unavailable according to server-side authorization reflection, and hidden authority data is not revealed through labels, counts, routes, or empty states.
3. Given global administrator data belongs to the fixed `global-administrators` aggregate, when the UI read contract is evaluated, then the story records the confirmed query/API route or marks the read surface as blocked by missing implementation support, and it does not route global administrator data through tenant-domain list, member, or user membership endpoints.
4. Given no confirmed global-administrator read route is available, when an authorized operator opens the area, then the UI shows an honest missing-implementation-support state with localized copy and support-safe recovery guidance, and the UI does not add a Tenants-local backend endpoint or fabricate administrator rows.
5. Given the area is unavailable because of authorization, stale freshness, missing read support, or FrontComposer gate status, when the area renders, then the unavailable state uses visible text, icon, accessible label, live-region behavior, forced-colors-safe styling, and stable selectors, and no state is shown as Success.
6. Given this story is complete, when verification is run, then component tests cover navigation ordering, platform-operator visibility, tenant-owner hiding, fixed-aggregate routing guard, missing-read-support state, localization, and stable selectors.
7. Given accessibility or E2E verification is run, then keyboard navigation, screen-reader labeling, forced-colors rendering, and support-safe unavailable copy are verified.

## Tasks / Subtasks

- [x] Add Global Administrators shell navigation without promoting Users (AC: 1, 2, 5, 6, 7)
  - [x] Extend the existing FrontComposer registration in `src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs` so shell navigation exposes primary areas in this order: Tenants, Global Administrators, Audit.
  - [x] Preserve `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` contextual links for My Tenants and User lookup; do not add a primary Users nav group or top-level Users route.
  - [x] Gate the Global Administrators nav entry from server-side reflected platform authority. Use `ITenantsBffComposition`/server principal evidence as the current UI-side reflection point; indeterminate authority must fail closed.
  - [x] Ensure tenant owners without platform authority do not see hidden authority labels, counts, fake empty rows, or a discoverable success state.

- [x] Add a focused Global Administrators page/surface for read-contract readiness (AC: 2, 3, 4, 5, 6, 7)
  - [x] Add a page under `src/Hexalith.Tenants.UI/Components/Pages/` or a focused `Components/GlobalAdministrators/` component, following existing page/component structure and CSS naming.
  - [x] Route the area under a clear top-level path such as `/global-administrators`; keep `TenantDetailPage` return-url safety so detail pages continue rejecting `/global-administrators` as an unsafe tenant-detail return target.
  - [x] Render a missing-implementation-support state when the read route is absent. The state must be visible, localized, support-safe, keyboard reachable, and announced as status/alert according to severity.
  - [x] Include stable selectors such as `tenants-global-admins-nav`, `tenants-global-admins-area`, `tenants-global-admins-unavailable`, `tenants-global-admins-read-contract`, `tenants-global-admins-live-region`, and `tenants-global-admins-recovery`.
  - [x] Do not render fabricated administrator rows, counts, or success badges. Do not infer administrators from claims beyond nav authorization reflection.

- [x] Record and enforce the global-admin read contract boundary (AC: 3, 4, 5, 6)
  - [x] Treat existing backend evidence as write/projection evidence only: `GlobalAdministratorProjectionHandler` writes `projection:global-administrators:singleton`, and `TenantIdentity.ForGlobalAdministrators()` fixes tenant `"system"`, domain `"global-administrators"`, aggregate id `"global-administrators"`.
  - [x] Record in code comments only where needed and in tests that no current `src/Hexalith.Tenants.Contracts/Queries` query contract or `TenantsQueryController` REST endpoint exposes a global-administrator read route.
  - [x] Do not add backend query contracts, REST endpoints, projection fields, controller actions, EventStore plumbing, or shared UI scaffolding as part of this story. If a real read contract is later required, it belongs in a backend/API story, not this UI readiness story.
  - [x] Do not route through `ListTenantsQuery`, `GetTenantQuery`, `GetTenantUsersQuery`, `GetUserTenantsQuery`, tenant detail rows, member rows, or user membership lookup as a substitute for platform authority data.

- [x] Extend localization, support-safety, and accessibility evidence (AC: 4, 5, 6, 7)
  - [x] Add EN/FR `Tenants.GlobalAdministrators.*` resource keys with parity and whole-string messages. Avoid runtime sentence-fragment assembly.
  - [x] Include unavailable reasons for missing permission, stale data if freshness is unknown, and missing implementation support. Keep these reasons distinct.
  - [x] Use visible text plus icon/shape for every unavailable/readiness state. Do not rely on color only.
  - [x] Add or update CSS with visible focus and forced-colors hooks, following `TenantsWorkspace.razor.css` and shared state component patterns.
  - [x] Keep rendered text and logs support-safe: no bearer tokens, decoded JWT payloads, raw claims dumps, correlation ids, message ids, cursors, ETags, stack traces, raw EventStore metadata, or production tenant/user data.

- [x] Add focused component/composition tests (AC: 1-7)
  - [x] Extend `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs` for nav group ordering, manifest boundaries, and platform-authority reflection.
  - [x] Add component tests for authorized platform operator visibility, tenant-owner hidden/unavailable behavior, missing-read-support state, no fabricated rows/counts, support-safe copy, stable selectors, and EN/FR resource parity.
  - [x] Add route/component tests proving Users remains contextual through `/tenants/my` and `/tenants/users` only.
  - [x] Add CSS/static tests or bUnit assertions for visible focus, forced-colors selectors, accessible labels, and live-region role/politeness.
  - [x] Run `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`; if `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest issue, use the xUnit v3 executable fallback pattern from Story 3.4.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 4 Story 4.1. Epic 4 covers global administrator governance and must keep platform authority separate from tenant membership. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.1: Global Administrators Navigation and Read Contract Readiness`]
- FR18 requires reviewing global administrators separately from tenant membership, and FR19 covers grant/remove global administrator flows with last-global-admin protection. Story 4.1 is the navigation/readiness slice only; it does not implement grant or remove commands. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 4: Global Administrator Governance`; `_bmad-output/planning-artifacts/epics.md#Functional Requirements by Epic`]
- Global administrators must be treated as a platform governance area, not a tenant detail subsection, tenant membership row, or user lookup variant. [Source: `docs/tenants-ui-operations-shell-spec.md#4.2 Global administrator read-only surface`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#8. Backend and Data Boundaries`]
- The Phase 2 backlog row that maps this read surface is `ui-06-global-admin-read-only`: backend evidence exists for the aggregate/auth posture, but the row remains planning-only unless story-specific accessibility/localization/documentation and current read-contract evidence are cited. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`; `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#8. Per-Row Consumption Mapping`]

### Confirmed Backend Facts

- `TenantIdentity.ForGlobalAdministrators()` returns `AggregateIdentity("system", "global-administrators", "global-administrators")`. Reuse these constants if identity evidence is displayed or tested; do not parse IDs as GUIDs/ULIDs. [Source: `src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs`; `_bmad-output/project-context.md#Identity Rules`]
- The projection write path exists. `GlobalAdministratorProjectionHandler` validates tenant `"system"` and aggregate `"global-administrators"`, rebuilds a `GlobalAdministratorReadModel`, and saves it to `projection:global-administrators:singleton`. [Source: `src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs`; `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs`]
- Tenant query authorization can check global administrator authority internally via `TenantQueryHandlerBase.IsGlobalAdminAsync`, which reads `projection:global-administrators:singleton`; that is authorization support for tenant queries, not a user-facing global-admin list endpoint. [Source: `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`]
- Current query contracts are tenant/user/audit only: `GetTenantQuery`, `GetTenantUsersQuery`, `GetUserTenantsQuery`, `ListTenantsQuery`, and `GetTenantAuditQuery`. No global-administrator read query contract is present under `src/Hexalith.Tenants.Contracts/Queries`. [Source: `src/Hexalith.Tenants.Contracts/Queries/`]
- `TenantsQueryController` exposes `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, and `GET /api/tenants/{tenantId}/audit`; it has no global-administrators read endpoint today. [Source: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`]

### Existing UI Implementation To Extend

- `MainLayout.razor` composes the body through `<FrontComposerShell>@Body</FrontComposerShell>`. Keep using the shared shell; do not add Tenants-local generic shell infrastructure. [Source: `src/Hexalith.Tenants.UI/Components/Layout/MainLayout.razor`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- `TenantsFrontComposerRegistration.RegisterDomain` currently adds only the Tenants nav group and registers an empty domain manifest. This is the likely composition point for the new primary navigation entry. [Source: `src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`; `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`]
- `TenantsWorkspace.razor` currently uses contextual links for My Tenants and User lookup. Preserve that shape; do not promote Users into primary navigation. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`; `docs/tenants-ui-operations-shell-spec.md#2026-06-06 Implementation Supersession`]
- `ITenantsBffComposition` and `TenantsBffComposition` already expose server-side lifecycle authorization reflection and fail closed unless the principal has both `eventstore:tenant=system` and a global administrator role/claim shape. Extend this carefully if a global-admin-nav-specific reflection member is needed. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs`; `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`]
- `ListSurfaceStates`, `TruthStateBadge`, and existing page CSS provide local patterns for status roles, `aria-live`, visible focus, forced-colors hooks, localized copy, and stable selectors. Reuse those conventions rather than inventing a parallel UI state system. [Source: `src/Hexalith.Tenants.UI/Components/Shared/ListSurfaceStates.razor`; `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor.css`]

### Architecture And UX Guardrails

- Operations Shell primary navigation is exactly Tenants, Global Administrators, Audit. Users is contextual through Tenants workspace entry points and access-review contexts. [Source: `docs/tenants-ui-operations-shell-spec.md#1.1 Primary navigation`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- Missing read support must surface as missing implementation support, not success, not empty administrator data, and not a generated tenant/user substitute. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.1: Global Administrators Navigation and Read Contract Readiness`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#4. Unavailable Action Reason Pattern`]
- Authorization reflection is UI disclosure only; backend remains the enforcing gate. Indeterminate authorization fails closed, and hidden authority data is never disclosed through labels, counts, routes, empty states, or test fixtures. [Source: `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`; `_bmad-output/project-context.md#Host Composition & Framework Rules`]
- The UI owns no datastore and must not read state stores, events, or projections directly. It consumes BFF/query gateway contracts only. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`; `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]
- Story 4.1 is not a backend/API story. Do not create a global-admin query contract, controller route, read model field, projection endpoint, EventStore registration, generic authorization service, FrontComposer component, or package reference here. [Source: `AGENTS.md#Domain Implementation Boundary`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- Truth-state presentation must keep missing permission, stale data, missing lifecycle support, missing consequence preview, missing audit proof, and high-impact flow not ready distinct. For this story, the key visible reasons are missing permission and missing implementation support; stale/unknown freshness should fail closed if freshness evidence is introduced. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md#4.1 Reason categories (enumerated verbatim)`]
- Global message bars are reserved for page-level degradation/system state. The global-admin surface should keep readiness/unavailable feedback close to the area itself. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md#6. Feedback Placement and Degradation Scope`]

### Accessibility, Localization, And Support Safety

- Baseline is WCAG 2.1 AA; WCAG 2.2 AA is the target where supported by the selected Fluent UI Blazor and FrontComposer stack. Verify story-specific evidence rather than assuming reusable shell evidence waives it. [Source: `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#1. WCAG Baseline, Target, and Scope`]
- Every unavailable/readiness state needs visible text, accessible name, non-color-only visual treatment, localized copy, stable selectors, and forced-colors-safe styling. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md#2.3 Presentation requirements (every state)`; `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#2. Keyboard and Focus Requirements`]
- Live-region politeness must be driven by state semantics, not color or visual intent. Assertive announcements are reserved for rejection, failure, destructive blockers, or unable-to-verify states. [Source: `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#3. Screen Reader, Status, and Live-Region Requirements`; `_bmad-output/implementation-artifacts/3-4-remove-tenant-configuration-key-with-consequence-preview.md#Previous Story Intelligence`]
- Add EN/FR resource parity for all new `Tenants.GlobalAdministrators.*` keys. Use whole strings with placeholders; do not assemble translated sentence fragments at runtime. [Source: `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#4. Localization and Message Composition Requirements`; `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`]
- Keep all user-facing output support-safe: no raw payloads, bearer tokens, decoded JWT payloads, raw claims dumps, stack traces, internal correlation ids, raw EventStore metadata, cursors, ETags, or real tenant/user production data. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `docs/tenants-ui-operations-shell-spec.md#5.3 Support-safe references`]

### Previous Story Intelligence

- Story 3.4 preserved focus-loop/focus-return, live-region politeness, forced-colors hooks, support-safe copy, EN/FR parity, and projection-truth honesty. Carry those quality bars forward even though Story 4.1 is read/readiness-oriented rather than command-oriented. [Source: `_bmad-output/implementation-artifacts/3-4-remove-tenant-configuration-key-with-consequence-preview.md#Dev Agent Record`]
- Story 3.4 validation used `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`; `dotnet test` hit the known .NET 10 Microsoft.Testing.Platform/VSTest issue, and the xUnit v3 executable fallback passed. Use the same fallback only if needed. [Source: `_bmad-output/implementation-artifacts/3-4-remove-tenant-configuration-key-with-consequence-preview.md#Debug Log References`]
- Story 1.3 already recorded the current navigation rule: Tenants, Global Administrators, Audit as primary; Users contextual. Do not regress that by following older planning text that listed Users as a primary area. [Source: `_bmad-output/implementation-artifacts/1-3-tenant-detail-navigation-and-overview.md`; `docs/tenants-ui-operations-shell-spec.md#2026-06-06 Implementation Supersession`]
- Story 1.0 confirmed FC-LYT/FC-CMD/FC-CNC/FC-A11Y/FC-L10N/FC-DOC shell gates but left Tenants story authors responsible for screen-specific accessibility, localization, and documentation evidence. [Source: `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`; `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#2026-06-05 Status Supersession`]

### Git Intelligence

- Recent commits are story-scoped Conventional Commits: `feat(story-3.4): Remove Tenant Configuration Key with Consequence Preview`, `feat(story-3.3): Set Tenant Configuration Key Value with Consequence Preview`, and `feat(story-3.2): Disable or Enable Tenant with High-Impact Confirmation`. A compatible implementation commit would be `feat(story-4.1): Add Global Administrators navigation readiness`. [Source: `git log --oneline -5`]
- The latest story commit touched story status/test summaries plus UI component, resource, gateway, and UI test files. Story 4.1 should follow that focused UI/test shape and avoid backend/submodule churn. [Source: `git show --stat --name-only f76e0b0`]
- Current dirty files before story creation included `_bmad-output/story-automator/orchestration-1-20260605-153745.md` and several `docs/tenants-ui-*` planning specs. They are unrelated to this create-story output; do not revert them. [Source: `git status --short`]

### Latest Technical Information

- Use the repo-pinned local stack: .NET 10, Blazor InteractiveServer, FrontComposer shell, Fluent UI Blazor v5 RC as pinned through repo/FrontComposer posture, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not add package versions or new dependencies. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- No external package/API research is required for Story 4.1. The material risks are scope confusion, false success, fabricated rows/counts, leaking platform authority to tenant owners, adding backend read scope in a UI story, and regressing contextual Users navigation.

### Project Structure Notes

- Expected source changes should stay under `src/Hexalith.Tenants.UI/Composition/`, `src/Hexalith.Tenants.UI/Components/Pages/`, optional `src/Hexalith.Tenants.UI/Components/GlobalAdministrators/`, `src/Hexalith.Tenants.UI/Services/Gateways/`, and `src/Hexalith.Tenants.UI/Resources/`.
- Tests should stay under `tests/Hexalith.Tenants.UI.Tests/`, primarily composition and component tests. Use xUnit v3, Shouldly, bUnit, and NSubstitute; test files remain plural `{Class}Tests.cs`.
- Do not modify `src/Hexalith.Tenants.Contracts`, `src/Hexalith.Tenants`, `src/Hexalith.Tenants.Server`, AppHost/Aspire plumbing, package metadata, shared FrontComposer code, or submodules for this story unless a compile-time integration break proves the existing UI can no longer consume shared contracts.
- Detected planning risk: historical planning docs call `ui-06-global-admin-read-only` planning-only, while current sprint asks Story 4.1 to become ready-for-dev. The implementation-ready slice is therefore the honest navigation/readiness surface with missing read support, not the global-admin list itself.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 4.1: Global Administrators Navigation and Read Contract Readiness`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 4: Global Administrator Governance`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/planning-artifacts/architecture.md#Data Architecture`; `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`; `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`
- UI specs: `docs/tenants-ui-operations-shell-spec.md#1. Operations Shell Information Architecture and Navigation`; `docs/tenants-ui-operations-shell-spec.md#4.2 Global administrator read-only surface`; `docs/tenants-ui-truth-state-and-action-availability-spec.md`; `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md`; `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`; `docs/tenants-ui-frontcomposer-dependency-map.md`
- Current implementation: `src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`; `src/Hexalith.Tenants.UI/Components/Layout/MainLayout.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`; `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs`; `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`; `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- Backend/projection evidence: `src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs`; `src/Hexalith.Tenants.Contracts/Queries/`; `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`; `src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs`; `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs`; `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`
- Tests and prior story evidence: `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`; `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`; `_bmad-output/implementation-artifacts/3-4-remove-tenant-configuration-key-with-consequence-preview.md`; `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, `architecture.md`, targeted PRD/UX/readiness docs, Story 3.4 previous-story intelligence, current UI composition/gateway/page/resource/test files, global administrator projection/identity/query-controller facts, and recent git history.
- Checklist validation was run against `.agents/skills/bmad-create-story/checklist.md`; corrections from validation are reflected in the explicit missing-read-support state, fixed-aggregate routing guard, no-backend-scope boundary, no-fabricated-rows rule, support-safety constraints, and story-specific a11y/l10n/test evidence tasks.
- Dev-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`, `references/Hexalith.FrontComposer/_bmad-output/project-context.md`, and `references/Hexalith.EventStore/_bmad-output/project-context.md`.
- Red-phase focused UI build failed as expected before implementation because `GlobalAdministratorsPage` and `ITenantsBffComposition.GlobalAdministratorsAuthorizationReflection` were missing.
- Validation: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Validation: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility; xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed 450/450.
- Regression validation: `dotnet build Hexalith.Tenants.slnx -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Regression validation: xUnit v3 executable fallback passed `Contracts.Tests` 103/103, `Client.Tests` 47/47, `Testing.Tests` 181/181, `Sample.Tests` 31/31, and `UI.Tests` 450/450.
- Broader Tier 2 signal: `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none` was attempted and failed 6/690 on pre-existing documentation/AppHost evidence issues already documented by Story 3.4: missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and stale deployment-readiness evidence.
- Final focused validation after adding the `tenants-global-admins-nav` selector: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors, and xUnit v3 executable fallback passed `UI.Tests` 450/450.
- Story-automator review loaded `.agents/skills/bmad-story-automator-review/SKILL.md`, `workflow.yaml`, `instructions.xml`, and `checklist.md`; reviewed the story file against actual git changes and local FrontComposer shell/registry source. External package/API research was not required because Story 4.1 uses repo-pinned APIs and local shared-source contracts.
- Review validation: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Review validation: xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed 453/453.
- Review regression validation: `dotnet build Hexalith.Tenants.slnx -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Review regression validation: xUnit v3 executable fallback passed `Contracts.Tests` 103/103, `Client.Tests` 47/47, `Testing.Tests` 181/181, `Sample.Tests` 31/31, and `UI.Tests` 453/453.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 4.1 to UI navigation and read-contract readiness for the Global Administrators area.
- Story context marks Story 4.1 ready-for-dev because the implementation slice can honestly expose/gate navigation and render missing read support without adding backend read scope or fabricating data.
- Story context identifies the main implementation risks: platform authority leaking to tenant owners, Users being promoted to primary nav, using tenant/user endpoints as substitutes, showing false Success/empty administrator data, adding backend scope in a UI story, and support-unsafe claim/authority output.
- Added static FrontComposer nav ordering for Tenants, Global Administrators, and Audit while preserving Users as contextual Tenants workspace links only.
- Added fail-closed global-administrator authorization reflection on `ITenantsBffComposition` and a `/global-administrators` readiness page that renders missing-permission or missing-read-support states without administrator rows, counts, success badges, raw claims, or tenant/user endpoint substitution.
- Added EN/FR whole-string localization, stable selectors, live-region behavior, visible icon/shape states, keyboard focus target, and forced-colors-safe CSS for the Global Administrators readiness surface.
- Added composition/static and bUnit coverage for nav order, manifest boundaries, authorization reflection, fixed-aggregate read-contract boundary, missing-read-support state, tenant-owner fail-closed behavior, contextual Users routes, resource parity, stable selectors, focus, and forced-colors hooks.
- Review fix: replaced the ineffective projection-manifest-only nav assumption with a Tenants-owned `FrontComposerShell.Navigation` slot component that renders Tenants, gated Global Administrators, and unavailable Audit in the required order.
- Review fix: moved `tenants-global-admins-nav` to the actual shell navigation entry and removed it from the page heading.
- Review fix: added complete EN/FR key parity coverage for the new `Tenants.GlobalAdministrators.*` and `Tenants.Navigation.*` resources.

### File List

- `_bmad-output/implementation-artifacts/4-1-global-administrators-navigation-and-read-contract-readiness.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants.UI/Components/Layout/MainLayout.razor`
- `src/Hexalith.Tenants.UI/Components/Layout/OperationsShellNavigation.razor`
- `src/Hexalith.Tenants.UI/Components/Layout/OperationsShellNavigation.razor.css`
- `src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/OperationsShellNavigationTests.cs`

### Senior Developer Review (AI)

#### Review Outcome

Approved after automatic fixes. No critical issues remain.

#### Findings Fixed

- [HIGH] The claimed primary navigation was not actually rendered by the shared shell. `TenantsFrontComposerRegistration` added nav groups, but local FrontComposer navigation renders only manifests with projections, and the Tenants manifest intentionally has none. Fixed by adding `OperationsShellNavigation` through the existing `FrontComposerShell.Navigation` slot, with Global Administrators gated by `ITenantsBffComposition.GlobalAdministratorsAuthorizationReflection`.
- [MEDIUM] The stable selector `tenants-global-admins-nav` was placed on the page heading, not the shell navigation entry. Fixed by moving that selector to the actual Global Administrators nav link and asserting that the page no longer renders it.
- [MEDIUM] EN/FR localization tests sampled only a few resource values and did not prove parity for every new key. Fixed with a `.resx` key-parity test for `Tenants.GlobalAdministrators.*` and `Tenants.Navigation.*`.
- [MEDIUM] The story File List omitted changed integration tests and the review-added navigation files. Fixed by updating the File List.

#### Review Validation Checklist

- [x] Story file loaded from `_bmad-output/implementation-artifacts/4-1-global-administrators-navigation-and-read-contract-readiness.md`
- [x] Story Status verified as reviewable (`review`)
- [x] Epic and Story IDs resolved (`4.1`)
- [x] Story Context located in story file; no separate context file found or required
- [x] Epic Tech Spec and architecture/standards docs loaded from story references and `_bmad-output/project-context.md`
- [x] Tech stack detected: .NET 10, Blazor InteractiveServer, FrontComposer shell, Fluent UI Blazor, xUnit v3, Shouldly, bUnit, NSubstitute
- [x] External MCP/web docs not required; local shared-source FrontComposer registry/shell code was reviewed as the authoritative API evidence
- [x] Acceptance Criteria cross-checked against implementation
- [x] File List reviewed and corrected for completeness
- [x] Tests identified and mapped to ACs; nav and resource parity gaps fixed
- [x] Code quality review performed on changed source files
- [x] Security review performed for authorization reflection, route disclosure, and support-safe output
- [x] Outcome decided: Approve after fixes
- [x] Review notes appended under `Senior Developer Review (AI)`
- [x] Change Log updated with review entry
- [x] Status updated to `done`
- [x] Sprint status synced to `done`
- [x] Story saved successfully

### Change Log

- 2026-06-06T13:32:59+02:00 - Created Story 4.1 context and marked it ready for development.
- 2026-06-06T13:43:42+02:00 - Implemented Global Administrators navigation/read-contract readiness surface, authorization reflection, localization, accessibility styling, and focused UI/composition tests; marked story ready for review.
- 2026-06-06T14:01:05+02:00 - Story-automator review fixed shell navigation gating/rendering, moved the global-admin nav selector to the actual nav entry, added resource parity coverage, corrected the File List, validated focused UI tests, and marked story done.
