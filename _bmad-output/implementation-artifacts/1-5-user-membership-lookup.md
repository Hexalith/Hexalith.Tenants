---
created: 2026-06-06T01:28:30+02:00
baseline_commit: 3077c7e
---

# Story 1.5: User Membership Lookup

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 1.5. -->

## Story

As a platform operator,
I want to look up a user's tenant memberships and navigate there from member rows,
so that I can understand a user's tenant access without exposing unauthorized memberships.

## Acceptance Criteria

1. Given an authorized operator enters a caller-supplied user id in the contextual user lookup flow, when the lookup runs, then the UI queries memberships through the server-side BFF using the existing `GET /api/users/{userId}/tenants`/`GetUserTenantsQuery` contract, sends the authenticated caller as requester and the entered user id as target, and the browser never calls the backend directly or treats the user id as a GUID or ULID.
2. Given the lookup returns authorized membership results, when the result table renders, then each row shows tenant identity, user role, tenant status, lifecycle context, and freshness, and results are scoped to what the caller is authorized to see.
3. Given no memberships are visible to the caller, when the lookup completes successfully, then the UI shows an explicit authorization-safe empty state and does not reveal whether hidden memberships, missing users, orphan memberships, or out-of-scope memberships exist.
4. Given a user membership view is reached from a tenant member row, when the target user id is passed to the lookup route or surface, then the same authorization-scoped lookup behavior is used, the entered or linked target user is visible as support-safe context, and Users remains contextual rather than becoming a primary navigation tab.
5. Given lookup data is loading, stale, degraded, unauthorized, invalid, or unavailable, when the view renders, then each state has distinct localized copy and accessible semantics, and no state collapses into false Success.
6. Given the lookup form and results are used with keyboard, screen reader, or narrow viewport, when the user submits, clears, sorts, pages, refreshes, or navigates results, then focus, status announcement, safety-critical columns, and stable selectors such as `data-testid="tenants-user-lookup"` are preserved, and copy remains support-safe and free of raw backend payloads, cursor contents, stack traces, tokens, correlation ids, or EventStore metadata.
7. Given this story is complete, when verification is run, then gateway or API-adapter tests cover target user id handling, requester/target cursor scope behavior, authorization-scoped result mapping, empty/hidden-membership behavior, stale/degraded/error states, invalid input, and contextual navigation; bUnit or Playwright tests verify keyboard submission, clear/reset focus behavior, live-region announcement, responsive safety, and selector stability.

## Tasks / Subtasks

- [x] Add a contextual user membership lookup surface and route (AC: 1, 3, 4, 5, 6)
  - [x] Add a route-backed page under `src/Hexalith.Tenants.UI/Components/Pages/`, for example `/tenants/users` plus query parameter `userId`, or `/tenants/users/{UserId}` if the route can safely carry every backend-valid id.
  - [x] Add a lookup form with a required caller-supplied user id field, submit, clear, refresh, and back/context controls; use stable selectors including `tenants-user-lookup`, `tenants-user-lookup-input`, `tenants-user-lookup-submit`, `tenants-user-lookup-clear`, `tenants-user-lookup-status`, and `tenants-user-lookup-results`.
  - [x] Preserve primary navigation order: Tenants, Global Administrators, Audit. Do not add Users as a co-equal primary navigation tab.
  - [x] Keep any Tenants workspace entry point contextual, such as a secondary link or search affordance, and preserve existing `/`, `/tenants`, `/tenants/my`, and `/tenants/{TenantId}` behavior.
  - [x] Ensure the route can be reached with a prefilled target user id from a future tenant member row without using a different lookup path.

- [x] Generalize the server-side BFF user-tenants gateway for target-user lookup (AC: 1, 2, 3, 5, 7)
  - [x] Extend `UserTenantMembershipRequest` or add a sibling request type with `TargetUserId`, `Cursor`, `PageSize`, and `ETag`; do not remove the self-audit behavior used by Story 1.4.
  - [x] Add an `ITenantQueryGateway` method such as `GetUserTenantsAsync(UserTenantMembershipRequest request, UserTenantMembershipSnapshot? previous, CancellationToken cancellationToken = default)`, or make the existing method accept an explicit target while keeping `GetMyTenantsAsync` as a self-targeting wrapper.
  - [x] In `TenantQueryGateway`, build `GetUserTenantsQuery` with `Domain = "tenants"`, `ProjectionType = "tenant-index"`, aggregate id `"index"`, `EntityId = request.TargetUserId`, payload `{ cursor, pageSize }`, and `ProjectionActorType = TenantProjectionRouting.ActorTypeName`.
  - [x] Use `ClaimsUserContextAccessor`/`IUserContextAccessor.UserId` as the authenticated requester. Do not use the target user id as the requester and do not fall back to static `"system"` for this lookup.
  - [x] Preserve opaque cursor values exactly; do not decode cursor internals in UI, convert to offset/limit, log cursor contents, or expose them in visible copy.
  - [x] Map `304 Not Modified` to the previous snapshot for the same target user only; if no matching previous snapshot exists, return degraded/unknown rather than empty or current.
  - [x] Map `400`, `401`, `403`, invalid gateway configuration, unavailable backend, stale metadata, degraded metadata, and generic gateway failures to distinct safe states without raw `ProblemDetails`, stack traces, tokens, internal correlation ids, payload JSON, or EventStore metadata.

- [x] Reuse and safely extend the existing membership grid/state model (AC: 2, 3, 5, 6)
  - [x] Reuse `UserTenantMembershipRow`, `UserTenantMembershipSnapshot`, and `MyTenantsDataGrid` patterns where practical, but do not make My Tenants copy or selectors appear on the operator lookup surface.
  - [x] If sharing the grid, parameterize resource prefix and selector prefix so My Tenants keeps `tenants-my-*` selectors while user lookup uses `tenants-user-*` selectors.
  - [x] Render `TenantId`, `Name`, `Status`, and `Role` from `UserTenantMembership`; treat lifecycle context as the available `Status`/projection status for this story.
  - [x] Keep `TenantStatus.Unknown` and `TenantRole.Unknown` fail-safe and never style them as Success.
  - [x] Do not add mutation affordances, command triggers, audit proof, consequence previews, member management, configuration reads, global administrator facts, or cross-tenant revoke/remove controls.
  - [x] Results must remain authorization-scoped: empty means "no visible memberships for this lookup", not "user does not exist" or "user has no memberships anywhere".

- [x] Add localized lookup copy and accessible state rendering (AC: 3, 4, 5, 6)
  - [x] Add whole-string `.resx` keys under `Tenants.UserLookup.*` in both invariant and French resource files for title, description, form labels, help text, target-user context, controls, columns, role labels, status labels, freshness labels, empty/loading/invalid/unauthorized/unavailable/stale/degraded copy, paging, refresh, clear, and announcements.
  - [x] Preserve role/status/freshness as text plus icon/shape/semantic label; color must never be the only signal and forced-colors users must retain meaning.
  - [x] Provide a polite live-region announcement for successful lookup, empty results, paging, refresh, and clear/reset; use assertive only for rejection, unavailable, unauthorized, invalid, unable-to-verify, or degraded states.
  - [x] Keep user ids and tenant ids literal caller-supplied strings. Validate only for required/safely supported input shape; never `Guid.TryParse`, `Ulid.TryParse`, normalize casing, or reformat ids.
  - [x] Do not display raw backend failure details, cursor strings, payload JSON, tokens, decoded JWT payloads, stack traces, internal correlation ids, raw EventStore metadata, or real PII.

- [x] Preserve navigation, focus, responsive, and support-safety requirements (AC: 4, 6)
  - [x] On direct route with `userId`, prefill the lookup field and load the target user through the same gateway path used by manual submit.
  - [x] After submit, move focus to the lookup status/results heading; after clear, return focus to the input and clear result state without fabricating success.
  - [x] Keep tenant identity, role, status/lifecycle, and freshness visible at mobile, tablet, desktop, and wide desktop widths through horizontal scroll, pinned/priority columns, or a visible fail-closed state.
  - [x] Preserve keyboard operation for input, submit, clear, refresh, paging, and result links; do not require hover-only explanations.
  - [x] If adding a contextual link from tenant member rows is blocked because Story 1.7 has not built the member table yet, add the route/link contract and tests for a prefilled route, then document the member-row link as a Story 1.7 integration point.

- [x] Add focused tests and verification evidence (AC: 1-7)
  - [x] Extend `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs` for explicit target-user query construction: authenticated requester, `EntityId = target user`, `ProjectionActorType`, cursor payload, no offset conversion, ETag pass-through, target-specific 304 reuse, and no backend payload leakage in failure states.
  - [x] Add component tests for the lookup route/form, direct route prefill, manual submit, clear/reset, loading/empty/invalid/unauthorized/unavailable/stale/degraded/ready states, selector stability, role/status localization, live-region behavior, focus movement, no mutation controls, and no browser-side backend/token access.
  - [x] Add tests proving My Tenants still targets the signed-in user after gateway generalization.
  - [x] Add route smoke coverage only if it can assert the route without live membership data; keep DAPR/Aspire prerequisite handling consistent with existing integration tests.
  - [x] Run `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` after restore, plus `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -nr:false` or the established xUnit v3 in-process runner fallback if the local .NET 10 Microsoft.Testing.Platform/VSTest issue appears.

## Dev Notes

Story 1.5 delivers FR4: an operator-facing, read-only user membership lookup. It should build on Story 1.4's `GetUserTenantsQuery` UI path, but Story 1.4 is self-audit only and currently hardwires target user to the authenticated user. This story must add explicit target-user support without breaking `/tenants/my`. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.5: User Membership Lookup`; `_bmad-output/implementation-artifacts/1-4-my-tenants-self-audit-view.md#Scope Boundaries`]

### Existing Implementation Context

- Story 1.1 created the Blazor InteractiveServer UI host, FrontComposer shell composition, Tenants domain registration, AppHost wiring, localization resources, BFF seams, and UI test project. Do not recreate host or shell foundations. [Source: `_bmad-output/implementation-artifacts/1-1-tenants-ui-host-bootstrap.md#Completion Notes List`; `src/Hexalith.Tenants.UI/Program.cs`]
- Story 1.2 created `ITenantQueryGateway`/`TenantQueryGateway`, the tenant list, `TenantDataGrid`, `TruthStateBadge`, `ListSurfaceStates`, stable selector conventions, resource patterns, and gateway/component tests. Reuse these conventions. [Source: `_bmad-output/implementation-artifacts/1-2-tenant-list-triage.md#Completion Notes List`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`]
- Story 1.3 added tenant detail routing and return-context preservation. Preserve `/`, `/tenants`, and `/tenants/{TenantId}` behavior when adding user lookup routes. [Source: `_bmad-output/implementation-artifacts/1-3-tenant-detail-navigation-and-overview.md#Completion Notes List`; `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`]
- Story 1.4 added `/tenants/my`, `GetMyTenantsAsync`, `UserTenantMembership*` state records, `MyTenantsDataGrid`, `MyTenantsState`, `Tenants.MyTenants.*` resources, and gateway/bUnit coverage. Extend these patterns rather than creating a second transport or redeclaring membership DTOs. [Source: `_bmad-output/implementation-artifacts/1-4-my-tenants-self-audit-view.md#Completion Notes List`; `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor`; `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor`]
- `TenantsFrontComposerRegistration` currently keeps the Tenants domain manifest minimal. Do not introduce a primary Users nav group; user lookup is contextual. [Source: `src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#6. User Journeys & Navigation`]

### Contract And Backend Requirements

- The source contract is `GetUserTenantsQuery`: `QueryType = "get-user-tenants"`, `Domain = "tenants"`, `ProjectionType = "tenant-index"`. The response shape is `PaginatedResult<UserTenantMembership>`. [Source: `src/Hexalith.Tenants.Contracts/Queries/GetUserTenantsQuery.cs`; `src/Hexalith.Tenants.Contracts/Queries/PaginatedResult.cs`]
- `UserTenantMembership` contains only `TenantId`, `Name`, `Status`, and `Role`. It has no member counts, owner counts, action availability, audit evidence, freshness timestamp, or separate lifecycle field. Do not fabricate missing fields. [Source: `src/Hexalith.Tenants.Contracts/Queries/UserTenantMembership.cs`]
- The backend REST route is `GET /api/users/{userId}/tenants`. It validates the route id, requires authenticated `sub`, scopes cursor validation with requester plus target user, dispatches `UserId = authenticatedUserId`, and sets `EntityId = userId`. [Source: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs#GetUserTenantsAsync`; `src/Hexalith.Tenants/Queries/TenantQueryCursorScopes.cs#GetUserTenants`]
- `GetUserTenantsQueryHandler` treats `EntityId` as the target user id. It permits self lookup, global-admin lookup of any user, and tenant-owner lookup of a target user's memberships only for tenants the requester owns. Missing target users, missing tenant index, no memberships, no overlap, and non-owner cross-user lookups return empty pages rather than public diagnostics. [Source: `src/Hexalith.Tenants/Queries/Handlers/GetUserTenantsQueryHandler.cs`; `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs#GetVisibleUserTenants`]
- Backend tests establish ordering by tenant id, disabled tenant status inclusion, orphan membership filtering before pagination, cursor scope binding to requester and target user, exclusion of `TenantRole.Unknown` and invalid enum roles, and empty results for non-owner/no-overlap cases. Preserve these assumptions in UI copy and tests. [Source: `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs#GetUserTenants_orders_by_tenant_id_and_returns_membership_fieldsAsync`; `#GetUserTenants_tenant_owner_querying_user_with_overlap_returns_owned_tenants_only`; `#GetUserTenants_non_owner_querying_other_user_returns_empty_page`; `#GetUserTenants_rejects_cursor_issued_for_different_requesterAsync`; `#GetUserTenants_excludes_unknown_roles_and_does_not_use_them_as_owner_authorityAsync`]
- Runtime remains Blazor InteractiveServer with a server-side BFF. Browser components must not hold backend access tokens, call Tenants/EventStore APIs directly, store token data, or parse backend payload JSON. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]

### UX, Accessibility, And Safety Requirements

- User lookup is contextual from a member row and global search, not a co-equal primary nav tab. Since Story 1.7 has not yet delivered the member table, this story should at least create the direct/prefilled route contract for later row links. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Information Architecture`; `_bmad-output/planning-artifacts/epics.md#UX Design Requirements`]
- The `ui-02-my-tenants-and-user-search-read-only` planning row uses `GET /api/users/{userId}/tenants`, cursor-based pagination, signed opaque scoped cursors, authorization-safe empty/error states, no-color-only role/status labels, accessible names, stable selectors, and localized role/status/empty/error copy. [Source: `docs/tenants-ui-operations-shell-spec.md#4.1 User lookup / My Tenants`; `docs/tenants-ui-operations-shell-spec.md#Read-Only Surface Consumption Map`]
- Cross-tenant revoke/remove actions are custom high-risk command flows and must not be generated from query rows. Story 1.5 is read-only. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Read-Only Surface Consumption Map`; `_bmad-output/planning-artifacts/epics.md#Story 1.5: User Membership Lookup`]
- Use absolute, culture-aware timestamps only if backend freshness evidence exists. If freshness cannot be measured, render `unknown` and fail closed for any action availability. [Source: `_bmad-output/planning-artifacts/epics.md#UX Design Requirements`; `_bmad-output/planning-artifacts/architecture.md#Data Architecture`]
- Known documentation conflict: `docs/tenants-ui-operations-shell-spec.md#5.1` refers to ids as ULIDs, while project context and epics state tenant/user ids are caller-supplied strings. Follow project context and epics: never parse or reformat tenant/user ids as generated identifiers. [Source: `_bmad-output/project-context.md#Identity Rules`; `_bmad-output/planning-artifacts/epics.md#Additional Requirements`]

### Scope Boundaries

- Do not add backend endpoints, new query contracts, command APIs, EventStore server plumbing, generic UI framework scaffolding, package versions in `.csproj`, Dockerfiles, `.sln` files, copied DTOs, or shared test harness helpers. [Source: `AGENTS.md#Domain Implementation Boundary`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- Do not modify root-declared submodules under `references/` for this story. If a reusable FrontComposer user-search, table, token, accessibility, or localization gap appears, record a follow-up instead of patching `Hexalith.FrontComposer` from this Tenants story. [Source: `AGENTS.md#Submodule Policy`; `_bmad-output/project-context.md#Code Quality & Style Rules`]
- Do not implement Story 1.7 tenant member table, Story 1.8 copy-to-clipboard readiness evidence, global administrator review, audit evidence, command lifecycle, consequence previews, or any tenant/user mutation. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant Workspace Triage and Read-Only Insight`]

### Previous Story Intelligence

- Story 1.4 already extended the BFF for `GetUserTenantsQuery` but made it self-audit-specific: `CreateUserTenantsQuery` currently sets both requester and `EntityId` to the authenticated user. Story 1.5 must introduce explicit target-user support and add tests that prove requester and target can differ. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`; `_bmad-output/implementation-artifacts/1-4-my-tenants-self-audit-view.md#Senior Developer Review (AI)`]
- Story 1.4 fixed the Story 1.2 identity-forwarding risk for self-audit by using `IUserContextAccessor.UserId`. Preserve that requester behavior for operator lookup. [Source: `_bmad-output/implementation-artifacts/1-4-my-tenants-self-audit-view.md#Prior-story regressions checked`]
- Story 1.3 fixed projection actor routing by requiring `ProjectionActorType = TenantProjectionRouting.ActorTypeName` on Tenants queries. Keep this on the generalized user lookup query. [Source: `_bmad-output/implementation-artifacts/1-3-tenant-detail-navigation-and-overview.md#Senior Developer Review (AI)`]
- Story 1.4 verification shows Release build and UI in-process xUnit v3 tests pass; `dotnet test` may still hit the known .NET 10 Microsoft.Testing.Platform/VSTest target issue, so use the in-process runner fallback only when needed and report it. [Source: `_bmad-output/implementation-artifacts/1-4-my-tenants-self-audit-view.md#Debug Log References`; `_bmad-output/implementation-artifacts/1-4-my-tenants-self-audit-view.md#Verification evidence`]
- Full `Server.Tests` and `IntegrationTests` may have DAPR/Aspire prerequisites unrelated to UI work. Do not hide those failures if encountered, but focus this story's required verification on build plus UI/gateway/component tests unless backend behavior is changed. [Source: `_bmad-output/implementation-artifacts/1-4-my-tenants-self-audit-view.md#Debug Log References`]

### Git Intelligence

- Recent story commits are `feat(story-1.1): Tenants UI Host Bootstrap`, `feat(story-1.2): Tenant List Triage`, `feat(story-1.3): Tenant Detail Navigation and Overview`, and `feat(story-1.4): My Tenants Self-Audit View`; follow that story-scoped Conventional Commit style if committing later. [Source: `git log --oneline -8`]
- Story 1.4 modified `TenantQueryGateway`, `ITenantQueryGateway`, `UnavailableTenantQueryGateway`, `MyTenantsPage`, user membership components/state, resources, UI gateway tests, component tests, workspace tests, route smoke tests, and `tests/test-summary.md`. Extend these carefully rather than replacing them. [Source: `_bmad-output/implementation-artifacts/1-4-my-tenants-self-audit-view.md#File List`]

### Latest Technical Information

Network research was not performed because this story relies on repo-pinned local versions, existing source, and already implemented backend contracts.

- .NET SDK `10.0.302`, target `net10.0`, nullable/implicit usings, `TreatWarningsAsErrors=true`. [Source: `global.json`; `Directory.Build.props`; `_bmad-output/project-context.md#Technology Stack & Versions`]
- Fluent UI Blazor remains pinned through FrontComposer at `5.0.0-rc.3-26138.1`; do not upgrade Fluent as part of this story. Verify exact DataGrid/table/form APIs locally before using new component features. [Source: `references/Hexalith.FrontComposer/Directory.Packages.props`; `_bmad-output/planning-artifacts/architecture.md#Starter Template Evaluation`]
- Tests use xUnit v3, Shouldly, NSubstitute, and bUnit. Test classes/files use plural `{Class}Tests.cs`; avoid raw `Assert.*`. [Source: `_bmad-output/project-context.md#Testing Rules`; `tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj`]

### Existing Files To Update And Preserve

- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs`: add explicit target-user lookup while preserving list, detail, and My Tenants signatures or wrappers.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`: generalize `CreateUserTenantsQuery` to requester plus target user; preserve `ProjectionActorType`, cursor payload, conditional ETag handling, and safe failure mapping.
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs`: return safe unavailable user lookup behavior without mock membership data.
- `src/Hexalith.Tenants.UI/State/UserTenants/*`: extend request/snapshot/reason/surface state with target-user and invalid/lookup-specific states as needed while keeping My Tenants behavior.
- `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor`: preserve self-audit route and behavior.
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor` and `MyTenantsState.razor`: reuse or parameterize only if it does not leak My Tenants labels/selectors into User Lookup.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`: add at most contextual reachability and preserve existing list filters, paging, return context, detail links, and My Tenants link.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`: add `Tenants.UserLookup.*` keys with EN/FR parity; do not assemble runtime sentence fragments.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`, `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs`, `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`: extend coverage without weakening existing list/detail/My Tenants tests.
- Add focused files such as `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor`, `.razor.css`, and `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs` if that matches the current UI organization.

### Project Structure Notes

- Source should stay in `src/Hexalith.Tenants.UI/`, primarily `Components/Pages/`, `Components/Users/`, `Components/Shared/`, `Services/Gateways/`, `State/UserTenants/`, `State/TruthState/`, `Resources/`, and component CSS.
- Tests should stay in `tests/Hexalith.Tenants.UI.Tests/` unless route smoke coverage requires the existing `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs`.
- The architecture documents are broader than this story. Implement only the read-only user membership lookup slice of `ui-02-my-tenants-and-user-search-read-only`; do not promote broader user management.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 1.5: User Membership Lookup`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant Workspace Triage and Read-Only Insight`
- PRD: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-4: Look up a user's memberships`
- UX: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Information Architecture`, `#Responsive & Platform`
- UI specs: `docs/tenants-ui-operations-shell-spec.md#4.1 User lookup / My Tenants`, `docs/tenants-ui-frontcomposer-dependency-map.md#Read-Only Surface Consumption Map`, `docs/tenants-ui-truth-state-and-action-availability-spec.md`, `docs/tenants-ui-responsive-layout-and-visual-system-spec.md`, `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md`
- Contracts: `src/Hexalith.Tenants.Contracts/Queries/GetUserTenantsQuery.cs`, `UserTenantMembership.cs`, `PaginatedResult.cs`, `TenantProjectionRouting.cs`
- Backend implementation evidence: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs#GetUserTenantsAsync`, `src/Hexalith.Tenants/Queries/Handlers/GetUserTenantsQueryHandler.cs`, `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs#GetVisibleUserTenants`, `src/Hexalith.Tenants/Queries/TenantQueryCursorScopes.cs`
- Existing UI implementation: `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`, `ClaimsUserContextAccessor.cs`, `Components/Pages/MyTenantsPage.razor`, `Components/Users/MyTenantsDataGrid.razor`, `Components/Users/MyTenantsState.razor`, `Components/Pages/TenantsWorkspace.razor`
- Previous stories: `_bmad-output/implementation-artifacts/1-1-tenants-ui-host-bootstrap.md`, `1-2-tenant-list-triage.md`, `1-3-tenant-detail-navigation-and-overview.md`, `1-4-my-tenants-self-audit-view.md`
- Project rules: `_bmad-output/project-context.md`, `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent fact `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, `architecture.md`, targeted PRD/UX/spec sections, Story 1.4, current UI source/tests, query contracts, backend controller/handler/cursor-scope behavior, server test evidence, and recent git history.
- Network research was not performed; local pinned versions and source are the authority for this story.
- Validation was run against `.agents/skills/bmad-create-story/checklist.md`; critical disaster-prevention checks are reflected in the story context.
- 2026-06-06: Implemented Story 1.5 with contextual `/tenants/users?userId=` lookup route, target-aware BFF gateway, shared user membership grid/state prefixes, localized EN/FR `Tenants.UserLookup.*` resources, and focused gateway/bUnit tests.
- 2026-06-06: Initial `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 -nr:false` failed during restore because the sandbox cannot reach NuGet vulnerability data at `api.nuget.org` and NU1900 is treated as an error.
- 2026-06-06: Cleared generated stale NU1900 entries in local `obj` restore files so `--no-restore` build/test validation could use the existing package cache; these generated files are not source changes and will be regenerated by normal restore.
- 2026-06-06: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -m:1 -nr:false` reached the known .NET 10 Microsoft.Testing.Platform/VSTest target error; used the established xUnit v3 in-process runner fallback.
- 2026-06-06: `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- 2026-06-06: `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed 84/84 tests.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context explicitly scopes Story 1.5 to read-only operator user membership lookup and separates it from My Tenants, tenant member table, command flows, audit evidence, copy support, and global administrator review.
- Story context identifies the key implementation risk: current `GetMyTenantsAsync` uses authenticated user as both requester and target, so this story must add explicit target-user support while keeping self-audit intact.
- Story context cites backend authorization behavior so UI implementation does not leak missing, hidden, orphaned, or out-of-scope memberships.
- Story context preserves the current UI host, FrontComposer shell posture, list/detail/My Tenants routes, gateway seams, localization pattern, and test conventions.
- Added a contextual `/tenants/users` lookup page with query-string prefill, literal target user id handling, submit/clear/refresh/sort/paging controls, live status announcements, focus movement, and stable `tenants-user-*` selectors.
- Generalized the BFF membership gateway with `GetUserTenantsAsync` while preserving `GetMyTenantsAsync` as a signed-in-user self-audit wrapper; requester stays `IUserContextAccessor.UserId`, target flows through `EntityId`.
- Extended user membership request/snapshot state with target user context and invalid input state; 304 reuse is target-specific and gateway failures map to safe states without backend payload leakage.
- Parameterized the existing My Tenants grid/state resource and selector prefixes so My Tenants keeps `tenants-my-*` and user lookup uses `tenants-user-*`.
- Added invariant and French whole-string lookup resources for controls, columns, role/status/freshness labels, state copy, announcements, paging, refresh, clear, and target context.
- Added gateway and bUnit coverage for explicit target query construction, requester/target separation, cursor/ETag behavior, target-specific 304 reuse, My Tenants preservation, direct route prefill, manual submit, clear/reset, state rendering, selector stability, responsive/forced-colors styles, and browser-side backend/token safety.

### Change Log

- 2026-06-06: Implemented read-only contextual user membership lookup and target-aware BFF gateway for Story 1.5.
- 2026-06-06: Added localized lookup UI and tests; verified solution Release build and UI xUnit v3 in-process suite.
- 2026-06-06 (review auto-fix): Fixed a `/tenants/my` regression — `MyTenantsPage.razor` gained an explicit `Invalid` branch so a backend `400` (now mapped to `Invalid` by the shared exception mapper) no longer collapses into an empty data grid (false Success). Added a `My_tenants_invalid_state_does_not_collapse_into_an_empty_grid` bUnit regression test plus its stub resources, and documented the previously-untracked route smoke test and `tests/test-summary.md` in the File List.

### File List

- `_bmad-output/implementation-artifacts/1-5-user-membership-lookup.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor`
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor.css`
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsState.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/State/UserTenants/UserTenantMembershipReason.cs`
- `src/Hexalith.Tenants.UI/State/UserTenants/UserTenantMembershipRequest.cs`
- `src/Hexalith.Tenants.UI/State/UserTenants/UserTenantMembershipSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/UserTenants/UserTenantMembershipSurfaceKind.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- `tests/test-summary.md`

## Senior Developer Review (AI)

**Reviewer:** Administrator · **Date:** 2026-06-06 · **Outcome:** Approved (auto-fix applied)

Adversarial review validated the story File List against git reality and verified every Acceptance Criterion and `[x]` task against the implementation. All seven ACs are implemented; no task was marked complete without supporting code; EN/FR `Tenants.UserLookup.*` resources have full 66-key parity; the lookup surface keeps `tenants-user-*` selectors separate from `tenants-my-*`, holds no browser-side backend/token access, and never parses caller-supplied ids as GUID/ULID.

Findings and resolution:

- **HIGH — False-Success collapse on the self-audit surface (FIXED).** Story 1.5 generalized `MapUserTenantException` to map HTTP `400 → UserTenantMembershipSurfaceKind.Invalid` (previously `Unavailable`) and updated the shared gateway test accordingly, but `MyTenantsPage.razor` had no `Invalid` rendering branch. Because `GetMyTenantsAsync` shares `GetUserTenantsCoreAsync`/`MapUserTenantException`, a backend `400` during self-audit produced an `Invalid` snapshot that fell through to the data-grid `else` branch, rendering an empty grid + pager with no error semantics — a false-negative ("you have no tenants") that violates AC5 ("no state collapses into false Success") and the "do not break `/tenants/my`" constraint. The dev had already added `Tenants.MyTenants.State.Invalid.*` resources and `Invalid` support in `MyTenantsState`, so the rendering branch was the missing wire-up. Added `else if (Invalid)` to `MyTenantsPage.razor` and a regression test asserting the safe `alert` state renders and the grid/pager do not.
- **MEDIUM — Incomplete File List documentation (FIXED).** `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` and `tests/test-summary.md` were changed in git but absent from the Dev Agent Record → File List. Added both, plus `MyTenantsPage.razor` for the fix above.
- **LOW — Client-side sort scope (accepted).** The lookup result sort reorders only the current page (backend orders by tenant id and paginates by opaque cursor). This is consistent with the read-only, cursor-paged contract and the no-offset-conversion rule; left as-is.

Verification after fixes: `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -warnaserror -m:1 -nr:false` → 0 warnings / 0 errors. UI suite via the in-process xUnit v3 runner → 93/93 passing (the known .NET 10 Microsoft.Testing.Platform/VSTest `dotnet test` target issue still applies, so the in-process runner fallback was used).
