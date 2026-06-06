---
created: 2026-06-06T02:40:41+02:00
baseline_commit: 32366bc
---

# Story 1.7: Tenant Member Table and Action Availability

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 1.7. -->

## Story

As an authorized tenant user,
I want to review tenant members and see which actions are available or unavailable,
so that I can understand membership state and safety constraints before any mutation workflow exists.

## Acceptance Criteria

1. Given an authorized user opens a tenant's member section, when member data is available from the existing `GET /api/tenants/{tenantId}/users` query path, then the member table shows each member's literal user id, role, owner count context, tenant status, freshness, and orphan or disabled context where applicable, and the table remains read-only for this story.
2. Given the member table renders, when assistive technology reads it, then headers, sort state, row relationships, role labels, status badges, action reason associations, and freshness are programmatically exposed, and the table does not rely on color alone.
3. Given a member action is unavailable because of permissions, freshness, lifecycle, platform support, conflict, or safety policy, when the row renders, then an inline `UnavailableActionReason` shows one of the six canonical localized categories: `missing permission`, `stale data`, `missing lifecycle support`, `missing consequence preview`, `missing audit proof`, or `high-impact flow not ready`.
4. Given future member actions are represented as unavailable or not-yet-supported, when the current story is complete, then no Add User, Change Role, Remove User, command lifecycle, consequence preview, audit proof, or command submission path is rendered as executable, and the UI never implies mutation success or in-flight command state.
5. Given the tenant is disabled, stale, unknown, degraded, unavailable, unauthorized, or authorization-indeterminate, when member action availability is evaluated, then action slots fail closed with visible localized reasons, and indeterminate authorization is not treated as allowed.
6. Given the member table is rendered across supported viewport widths, when width changes, then identity, role, owner count, tenant status, freshness, and reason slots remain visible or the action area fails closed, and stable selectors such as `data-testid="tenants-member-table"` and `data-testid="tenants-member-unavailable-reason"` are present.
7. Given this story is complete, when verification is run, then component and gateway-adapter tests cover member row mapping, role/status labels, read-only behavior, all unavailable reason categories, disabled/stale/unknown authorization handling, selector stability, table semantics, keyboard reachability, screen-reader association, forced-colors rendering, no-color-only status, localization parity, and no browser-side backend/token access.

## Tasks / Subtasks

- [x] Add the tenant member access review section under the existing tenant detail route (AC: 1, 2, 5, 6)
  - [x] Prefer `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor` and `.razor.css`; compose it from `TenantDetailPage.razor` so `/tenants/{TenantId}` remains the source route and list return behavior stays intact.
  - [x] Use the existing BFF/gateway pattern. If a dedicated member page is needed, extend `ITenantQueryGateway` with a tenant-member query method backed by `GetTenantUsersQuery`; do not call REST from the browser and do not add backend endpoints.
  - [x] Preserve `TenantDetail.Members` as the already-loaded owner-count/context source until a dedicated member snapshot is loaded; do not duplicate query DTOs or fabricate member metadata not in the contracts.
  - [x] Render an authorization-safe empty state when no members are visible. Empty must not reveal hidden memberships and must not collapse unavailable, unauthorized, stale, degraded, or unknown states into success.
  - [x] Keep the current detail overview, member summary, configuration summary, configuration view, stale/degraded banners, freshness badge, and safe return URL behavior intact.

- [x] Implement read-only member rows and action-availability reflection (AC: 1, 3, 4, 5, 6)
  - [x] Render literal `TenantMember.UserId` values without parsing, normalizing, truncating into test-only text, or treating them as GUIDs/ULIDs.
  - [x] Render role labels for `TenantOwner`, `TenantContributor`, `TenantReader`, and `Unknown`; `Unknown` must fail closed and never grant owner/contributor affordance.
  - [x] Compute owner count from the visible current member set and show last-owner/zero-owner context as warning or unavailable context only. Story 1.7 must not launch remove/change-role flows.
  - [x] Show tenant status from the current tenant detail projection; disabled tenants make all future mutation action slots unavailable with visible reasons.
  - [x] Represent future Add, Change Role, and Remove action slots only as read-only unavailable context. Do not render enabled buttons, hidden submit forms, command payloads, command lifecycle panels, consequence previews, or audit evidence receipts.
  - [x] Implement the six canonical `UnavailableActionReason` categories as localized whole strings under `Tenants.Members.*`, with stable selectors such as `tenants-member-unavailable-reason`, `tenants-member-action-slot`, and per-category test hooks that are not keyed on row text.

- [x] Preserve truth-state, fail-closed gating, and support-safety behavior (AC: 2, 3, 4, 5)
  - [x] Reuse `TruthStateBadge` for freshness, with a member-specific `TestId` and resource prefix so the table does not leak list/configuration selectors.
  - [x] Evaluate action availability in fail-closed order: unavailable/unauthorized detail state, disabled tenant lifecycle, unknown/stale freshness, indeterminate authorization, missing command/consequence/audit support, then future high-impact readiness.
  - [x] Treat SignalR and ETag/304 as freshness evidence only; never show mutation success, accepted, confirmed, or audit available in this read-only story.
  - [x] Do not log, render, serialize into markup, copy, or announce raw backend payloads, bearer tokens, decoded JWT contents, command payloads, EventStore metadata, correlation ids, stack traces, or real PII.
  - [x] Keep user lookup contextual: member user ids may link to or prefill the existing `/tenants/users?userId=...` lookup if implemented safely, but this story must not promote Users into a primary member-management nav model.

- [x] Add localized copy, accessibility semantics, and responsive styling (AC: 2, 3, 6)
  - [x] Add EN/FR parity for `Tenants.Members.*` resource keys covering title, description, table caption, columns, role labels, tenant status labels or reused labels, owner count context, empty/unavailable/stale/degraded states, all six unavailable reason categories, action slot labels, announcements, and support-safe accessible labels.
  - [x] Use table or DataGrid semantics with real column headers, row headers for user ids, sort-state exposure when sorting is present, `aria-describedby` linking each unavailable reason to the relevant action slot, and keyboard-reachable reason content that is not hover-only.
  - [x] Use text plus icon/shape/state labels for role, status, freshness, and reasons. Color must never be the only signal; forced-colors mode must preserve meaning.
  - [x] Add CSS that reserves action/reason column space, allows literal user ids to wrap with `overflow-wrap: anywhere`, defines stable responsive constraints, and prevents overlap on mobile and desktop.

- [x] Add focused verification (AC: 1-7)
  - [x] Extend `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` or add `MemberAccessReviewTests.cs` for member mapping, owner-count context, role labels, tenant disabled context, empty state, stable selectors, no mutation affordances, and accessible literal user ids.
  - [x] Add gateway tests only if `ITenantQueryGateway` gains a dedicated `GetTenantMembersAsync` path; cover `GetTenantUsersQuery`, `ProjectionActorType = TenantProjectionRouting.ActorTypeName`, cursor/page-size payload, ETag/304 behavior, stale/degraded metadata, and safe 401/403/404/503 state mapping.
  - [x] Add tests for all six unavailable reason categories and the fail-closed combinations: disabled tenant, stale freshness, unknown freshness, degraded detail, unauthorized/unavailable detail, missing consequence preview, missing audit proof, and high-impact flow not ready.
  - [x] Add resource parity tests for every `Tenants.Members.*` key in invariant and French `.resx` files.
  - [x] Add CSS or component coverage for table semantics, keyboard focus, `aria-describedby`, forced-colors hooks, responsive/no-overlap behavior, and selector stability.
  - [x] Run `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` after restore, plus `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -nr:false` or the established xUnit v3 in-process runner fallback if the known .NET 10 Microsoft.Testing.Platform/VSTest issue appears.

## Dev Notes

Story 1.7 delivers FR8 and FR9: an authorization-safe, read-only member table plus visible action availability reasons. It is a read-model/UI composition slice. It must not implement Add User, Change Role, Remove User, command lifecycle tracking, consequence preview, audit proof, global-administrator management, or safe-copy behavior from Story 1.8. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.7: Tenant Member Table and Action Availability`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#7.3 Member & Access Review`]

### Existing Implementation Context

- Stories 1.1 through 1.6 already created the Blazor InteractiveServer UI host, FrontComposer shell composition, Tenants domain registration, AppHost wiring, localization resources, BFF gateway seams, tenant list, detail route, My Tenants, user lookup, `TruthStateBadge`, and read-only configuration view. Do not recreate host, shell, AppHost, ServiceDefaults, FrontComposer scaffolding, resources infrastructure, or package setup. [Source: `_bmad-output/implementation-artifacts/1-1-tenants-ui-host-bootstrap.md`; `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md#Existing Implementation Context`]
- `TenantDetailPage.razor` currently renders the detail states, identity summary, owner/member summary, configuration summary, and `TenantConfigurationView`. Story 1.7 should replace/deepen the member summary with a member access review section while preserving the existing overview/configuration behavior. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`]
- `TenantConfigurationView` established the current section pattern: focused component under `Components/Tenants`, component CSS, `TruthStateBadge` with surface-specific `TestId`/resource prefix, section-specific `tenants-config-*` selectors, EN/FR resource parity, bUnit coverage, forced-colors hooks, and no mutation affordances. Mirror that discipline with `tenants-member-*` selectors and `Tenants.Members.*` resources. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`; `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`]

### Contract And Backend Requirements

- The member DTO is `TenantMember(string UserId, TenantRole Role)`. It has no display name, email, profile, orphan flag, last-active timestamp, per-member freshness, authorization decision, action eligibility DTO, or global-admin standing. Do not fabricate missing fields. [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantMember.cs`]
- `TenantDetail` already includes `IReadOnlyList<TenantMember> Members`, tenant `Status`, configuration, and `CreatedAt`; the current UI detail route loads it through `ITenantQueryGateway.GetTenantAsync`. This can provide owner count and tenant status context without a new browser endpoint. [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantDetail.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs#GetTenantAsync`]
- The backend also exposes `GET /api/tenants/{tenantId}/users`, translated to `GetTenantUsersQuery`, cursor-paginated with signed opaque cursors and standard page-size clamping. If this story adds a dedicated member gateway, it must use the existing query contract and `IEventStoreGatewayClient` BFF pattern, including `ProjectionActorType = TenantProjectionRouting.ActorTypeName`; the browser must not call REST directly. [Source: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs#GetTenantUsersAsync`; `src/Hexalith.Tenants.Contracts/Queries/GetTenantUsersQuery.cs`; `src/Hexalith.Tenants/Queries/Handlers/GetTenantUsersQueryHandler.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`]
- `GetTenantUsersQueryHandler` filters to concrete roles through `GetConcreteMembers`, authorizes by tenant membership or global administrator, returns forbidden/not-found safely, and paginates by user id. The UI must treat server authorization as authoritative and avoid exposing hidden memberships in empty or forbidden states. [Source: `src/Hexalith.Tenants/Queries/Handlers/GetTenantUsersQueryHandler.cs`; `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`]
- User ids and tenant ids are meaningful caller-supplied strings, case-sensitive in projections, and not ULIDs/GUIDs. Preserve literal values and do not normalize casing. [Source: `_bmad-output/project-context.md#Identity Rules`; `tests/Hexalith.Tenants.Client.Tests/Projections/TenantLocalStateCasingTests.cs`]

### UX, Accessibility, And Safety Requirements

- FR8 requires a read-only member table with role, owner count, status, freshness, and orphan/disabled context. FR9 requires visible plain-language action availability reasons; actions are reflective in MVP and arrive in later phases. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#7.3 Member & Access Review`]
- The canonical `member-table` component is read-only, exposes table semantics, shows role/owner count/status/freshness/orphan-disabled context, and reflects which actions would be available without enforcing authorization in the UI. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Component Patterns`]
- `UnavailableActionReason` has exactly six canonical categories: `missing permission`, `stale data`, `missing lifecycle support`, `missing consequence preview`, `missing audit proof`, and `high-impact flow not ready`. Reasons must be inline-visible, localized, keyboard/screen-reader reachable, associated with the row/action they explain, and never tooltip-only. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#State Patterns`]
- Disabled tenants are eventual-consistency availability signals. Commands targeting disabled tenants are rejected as `TenantDisabled`; this story must reflect disabled/lifecycle constraints as unavailable context and must not submit commands. [Source: `_bmad-output/project-context.md#DAPR, Eventing & Consumer Rules`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#State Patterns`]
- Last-owner removal is not a backend hard stop, but it is high risk and belongs to later remove-user consequence-preview work. Story 1.7 may show owner-count context and high-impact-readiness reasons; it must not present last-owner handling as an enabled remove flow. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#6. Cross-Cutting Product Rules`; `docs/tenants-ui-remove-user-from-tenant-journey-spec.md#3. High-Risk Access Cases and Elevated Friction`]
- Support-safety is hard: no surface, copy action, log, toast, accessible label, or announcement may expose bearer tokens, decoded JWT contents, command payloads, serialized event bodies, raw EventStore metadata, internal correlation ids, stack traces, or real PII. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`]

### Scope Boundaries

- Do not add backend endpoints, EventStore server plumbing, generic UI framework scaffolding, package versions in `.csproj`, Dockerfiles, `.sln` files, copied DTOs, shared test harness helpers, or submodule changes. [Source: `AGENTS.md#Domain Implementation Boundary`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- Do not implement Epic 2 membership mutation flows, Epic 5 audit evidence, command lifecycle panels, consequence previews, global administrator review, safe-copy controls, or support evidence receipts. Keep this story read-only and reflective. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant Workspace Triage and Read-Only Insight`; `_bmad-output/planning-artifacts/epics.md#Epic 2: Tenant Membership and Tenant Record Management`]
- Missing reusable FrontComposer table/action/reason capability should be handled inside the Tenants read-only surface only when narrow and local; otherwise record a follow-up rather than patching `Hexalith.FrontComposer`. [Source: `AGENTS.md#Submodule Policy`; `docs/tenants-ui-phase-2-story-backlog.md#ui-04-user-management-member-table`]

### Existing Files To Update And Preserve

- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`: compose the member access review and preserve `/tenants/{TenantId}`, loading/unauthorized/not-found/unavailable/stale/degraded behavior, identity summary, configuration summary/view, freshness badge, and safe return URL.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor.css`: extend only if layout integration is needed; preserve responsive grid, `overflow-wrap: anywhere`, forced-colors hooks, and focus-visible styling.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/`: add `MemberAccessReview.razor` and `.razor.css` if using the focused component path.
- `src/Hexalith.Tenants.UI/Components/Shared/`: add a narrowly scoped `UnavailableActionReason` component only if useful; do not invent generic command infrastructure.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs` and `TenantQueryGateway.cs`: update only if adding a dedicated paged member gateway around `GetTenantUsersQuery`; preserve existing list/detail/user-membership behavior and all `ConfigureAwait(false)` awaits.
- `src/Hexalith.Tenants.UI/State/`: add `TenantMembers` request/snapshot/row/reason types only if the member table needs an independent load state. Follow existing record/state naming from `TenantDetail`, `TenantList`, and `UserTenants`.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`: add `Tenants.Members.*` whole-string keys with parity.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`: extend or split focused member component tests while preserving configuration/detail coverage.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`: update only if gateway behavior changes.

### Previous Story Intelligence

- Story 1.6 showed selector/resource separation matters: the review fixed a leaked default `tenants-list-truth-state` selector inside the configuration table. For Story 1.7, every badge/reason/control inside the member table needs explicit `tenants-member-*` selectors. [Source: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md#Senior Developer Review (AI)`]
- Story 1.6 also fixed missing row headers. Use `<th scope="row">` for the literal user id column and test the row relationship; do not leave data rows as all `<td>`. [Source: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md#Senior Developer Review (AI)`]
- Story 1.5 and Story 1.6 both kept user lookup/configuration changes scoped to their surfaces and localized through resource prefixes. Use `Tenants.Members.*` and avoid reusing list/configuration copy for member-specific failure states. [Source: `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md`; `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md`]
- Story 1.6 verification used the known xUnit v3 in-process runner fallback when `dotnet test` hit the .NET 10 Microsoft.Testing.Platform/VSTest target issue. Keep the same verification pattern and report the fallback only if it is needed. [Source: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md#Debug Log References`]

### Git Intelligence

- Recent story commits use story-scoped Conventional Commit style: `feat(story-1.1)` through `feat(story-1.6)`. Follow `feat(story-1.7): Tenant Member Table and Action Availability` if committing later. [Source: `git log --oneline -8`]
- Story 1.6 touched only detail/configuration UI, resources, UI tests, sprint status, and story artifacts. Story 1.7 should similarly stay in detail/member UI, resources, focused gateway state if needed, and UI tests. [Source: `git show --stat --oneline -1 32366bc`]

### Latest Technical Information

Network research was not performed because network access is restricted and this story relies on repo-pinned local versions plus existing backend contracts.

- .NET SDK `10.0.300`, target `net10.0`, nullable/implicit usings, `TreatWarningsAsErrors=true`. [Source: `global.json`; `Directory.Build.props`; `_bmad-output/project-context.md#Technology Stack & Versions`]
- Fluent UI Blazor remains pinned through FrontComposer at `5.0.0-rc.3-26138.1`; do not upgrade Fluent or FrontComposer as part of this story. Verify exact table/icon APIs locally before using new component features. [Source: `Hexalith.FrontComposer/Directory.Packages.props`; `_bmad-output/planning-artifacts/architecture.md#Starter Template Evaluation`]
- Tests use xUnit v3, Shouldly, NSubstitute, and bUnit. Test classes/files use plural `{Class}Tests.cs`; avoid raw `Assert.*`. [Source: `_bmad-output/project-context.md#Testing Rules`; `tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj`]

### Project Structure Notes

- Source should stay in `src/Hexalith.Tenants.UI/`, primarily `Components/Pages/`, `Components/Tenants/Members/`, `Components/Shared/`, `State/TenantMembers/` if needed, `Services/Gateways/` if needed, `Resources/`, and component CSS.
- Tests should stay in `tests/Hexalith.Tenants.UI.Tests/`; this story should not need a new route smoke project or backend integration tests unless the UI gateway contract changes.
- The architecture documents describe future command and audit components. Implement only the read-only member table/action-availability slice of `ui-04-user-management-member-table`.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 1.7: Tenant Member Table and Action Availability`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant Workspace Triage and Read-Only Insight`
- PRD/FR source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#7.3 Member & Access Review`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`, `#Project Structure & Boundaries`, `#Requirements to Structure Mapping`
- UX/specs: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Component Patterns`, `#State Patterns`; `docs/tenants-ui-phase-2-story-backlog.md#ui-04-user-management-member-table`; `docs/tenants-ui-remove-user-from-tenant-journey-spec.md#1.3 Launch context sources (read endpoints only)`
- Contracts/backend: `src/Hexalith.Tenants.Contracts/Queries/TenantMember.cs`; `src/Hexalith.Tenants.Contracts/Queries/TenantDetail.cs`; `src/Hexalith.Tenants.Contracts/Queries/GetTenantUsersQuery.cs`; `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs#GetTenantUsersAsync`; `src/Hexalith.Tenants/Queries/Handlers/GetTenantUsersQueryHandler.cs`
- Existing UI implementation: `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- Previous stories: `_bmad-output/implementation-artifacts/1-1-tenants-ui-host-bootstrap.md`, `1-2-tenant-list-triage.md`, `1-3-tenant-detail-navigation-and-overview.md`, `1-4-my-tenants-self-audit-view.md`, `1-5-user-membership-lookup.md`, `1-6-read-only-tenant-configuration-view.md`
- Project rules: `_bmad-output/project-context.md`, `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent fact `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, `architecture.md`, relevant PRD/UX/spec sections, Story 1.6, current detail/configuration/gateway source, query contracts, backend controller/handler behavior, UI tests/resources, and recent git history.
- Network research was not performed; local pinned versions and source are the authority for this story.
- Validation was run against `.agents/skills/bmad-create-story/checklist.md`; critical disaster-prevention checks are reflected in this story context.
- 2026-06-06: Dev-story activation resolved customization with no prepend/append steps and persistent fact `_bmad-output/project-context.md`; loaded project context, sprint status, and complete Story 1.7.
- 2026-06-06: Red phase added member access review assertions; `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -nr:false` hit the known .NET 10 Microsoft.Testing.Platform/VSTest target error, and Release test build failed as expected before implementation because `Hexalith.Tenants.UI.Components.Tenants.Members` did not exist.
- 2026-06-06: Implemented `MemberAccessReview` using existing `TenantDetail.Members`, `TenantDetail.Status`, `TenantDetailSurfaceKind`, and `TenantFreshnessState`; no browser REST calls, gateway methods, backend endpoints, command payloads, or mutation forms were added.
- 2026-06-06: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -warnaserror -m:1 -nr:false` passed with 0 warnings/0 errors.
- 2026-06-06: `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed 118/118. (Corrected during review: an earlier entry reported 112/112, which predated the full member-test set.)
- 2026-06-06: `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` passed with 0 warnings/0 errors.
- 2026-06-06: Tier 1 plus UI in-process test executables passed: Contracts 103/103, Client 47/47, Testing 181/181, Sample 31/31, UI 118/118.
- 2026-06-06: `Hexalith.Tenants.Server.Tests` in-process executable was attempted and still has the known pre-existing 6 documentation/configuration failures from missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and the Story 7.6A deployment-readiness summary expectation; no failure touches Story 1.7 UI files.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 1.7 to the read-only member table and action availability reflection, separating it from Epic 2 mutation flows and Epic 5 audit proof.
- Story context identifies the key implementation risk: `TenantMember` has only `UserId` and `Role`; action availability, orphan/global-admin/per-member freshness details must not be fabricated.
- Story context preserves the existing tenant detail route, BFF gateway boundary, projection truth/freshness handling, localization pattern, configuration view, and UI test conventions.
- Added a read-only member access review section to the existing tenant detail route, preserving the existing overview, member summary, configuration summary/view, freshness badge, and safe return URL behavior.
- Rendered literal member user ids with table row headers, role/status/freshness labels, owner-count context, and fail-closed unavailable action slots for future add/change/remove actions without executable mutation affordances.
- Added localized EN/FR `Tenants.Members.*` copy, six canonical unavailable reason categories, member-specific `TruthStateBadge` selectors, keyboard-reachable reason content, forced-colors-safe styling, and responsive table constraints.
- Extended UI tests for member row mapping, role/status labels, read-only behavior, all unavailable reason categories, disabled/stale/unknown/degraded fail-closed states, empty-state safety, selector stability, table semantics, keyboard/ARIA association, CSS hooks, and localization parity.

### File List

- `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor.css`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/test-summary.md`

### Change Log

- 2026-06-06: Implemented Story 1.7 tenant member access review as a read-only detail-route component with localized fail-closed action availability reasons and focused UI verification. Status set to review.
- 2026-06-06: Senior Developer Review (AI) completed with auto-fixes for member-table ARIA list semantics, test-stub localization fidelity, and Dev Agent Record accuracy; full solution Release build clean (0/0) and UI suite 118/118. Status set to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot
**Date:** 2026-06-06
**Outcome:** Approved (auto-fix mode) — 0 critical issues remaining; status → done.

Adversarial review validated all 7 acceptance criteria against the implementation. ACs 1–7 are implemented: literal user ids with `<th scope="row">` headers, role/status/owner/freshness context, the six canonical localized `UnavailableActionReason` categories, fail-closed gating for disabled/stale/unknown/degraded/unauthorized/unavailable states, read-only (no buttons/forms/command lifecycle/token leakage), stable `tenants-member-*` selectors, EN/FR parity (45 keys, full parity), and accessibility hooks. No tasks were falsely marked complete; the File List matched git reality except for one omission (fixed below).

### Findings and fixes applied

- **[MEDIUM] File List incomplete** — `tests/test-summary.md` was modified but not listed in the Dev Agent Record File List. Fixed: added to File List above.
- **[MEDIUM] Inaccurate test counts in Debug Log** — Debug Log reported "112/112" for the UI suite; the actual count after the new member tests is **118/118** (verified by direct executable run). Fixed: corrected the Debug Log entries below.
- **[MEDIUM] Invalid/mislabeled ARIA list structure in the action cell** (AC2) — `MemberAccessReview.razor` wrapped each row's action slots in a `role="list"` whose accessible name was the *reason* label ("Unavailable action reasons for {user}") and which contained a non-`listitem` `<ul>` of reasons as a direct child. Fixed: removed the mislabeled `role="list"`/`aria-label` from the `tenant-members__actions` wrapper and the orphaned `role="listitem"` from each slot; reason–action association is preserved via per-slot `aria-label` + `aria-describedby`, and the reasons remain a proper `<ul>`.
- **[LOW] Reason catalog `aria-label` on a roleless `<div>`** (AC2) — the catalog's `aria-label` was not reliably exposed because the container had no list role. Fixed: added `role="list"` to the catalog and `role="listitem"` to each entry.
- **[LOW] Test-stub localization drift** (AC7) — `StubTenantsLocalizer` text for `State.Stale`, `State.Degraded`, `ScopeNotice`, and `ReasonCatalogLabel` did not match the shipped `.resx` strings, so assertions exercised text the app never renders. Fixed: aligned the stub values to the real `.resx` and updated the affected exact-text assertion.

### Verification after fixes

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -warnaserror -m:1 -nr:false` → 0 warnings / 0 errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` → 118 total, 0 failed, 0 skipped.
- `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` → 0 warnings / 0 errors.
