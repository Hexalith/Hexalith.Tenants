---
baseline_commit: a09dd85
created: 2026-06-06T14:17:15+02:00
---

# Story 4.2: Review Global Administrators from Fixed Aggregate

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 4.2. -->

## Story

As an authorized platform operator,
I want to review current global administrators from the fixed platform authority scope,
so that I can understand who holds platform-level governance power without conflating it with tenant membership.

## Acceptance Criteria

1. Given a confirmed global-administrator read contract is available, when an authorized operator opens Global Administrators, then the UI reads from the fixed `global-administrators` aggregate scope through the server-side BFF, and it does not use tenant membership, tenant detail, or user membership endpoints as a substitute.
2. Given global administrator projection data is available, when the review surface renders, then each row shows administrator identity and freshness, and the surface clearly communicates platform authority scope without calling it tenant ownership.
3. Given the read result is empty, stale, degraded, unauthorized, or unavailable, when the review surface renders, then the UI shows the exact state with localized copy and accessible semantics, and it does not reveal hidden administrators to unauthorized callers or show false Success.
4. Given freshness is unknown or the projection is stale, when action availability is computed from the review surface, then grant and remove actions fail closed with visible unavailable reasons, and last-confirmed administrator data remains visible without being overwritten by in-flight intent.
5. Given administrator identifiers are visible or truncated, when the operator inspects or copies them, then only support-safe literal identifiers or approved references are exposed, and no tokens, decoded JWT contents, raw metadata, stack traces, internal correlation ids, or PII are shown or copied.
6. Given the review surface is used at supported desktop, tablet, or mobile widths, when layout changes, then identity, platform authority context, freshness, and action/reason slots remain visible or fail closed, and stable selectors such as `data-testid="tenants-global-admins-list"` are present.
7. Given this story is complete, when verification is run, then unit/component tests cover fixed-aggregate read mapping, authorization-scoped visibility, empty/stale/degraded/unavailable states, no tenant-membership conflation, support-safe identifiers, and stable selectors.
8. Given accessibility or E2E verification is run, then keyboard navigation, row semantics, forced-colors status rendering, responsive safety, and localized state copy are verified.

## Tasks / Subtasks

- [x] Add the global-administrator read contract and DTOs (AC: 1, 2, 3, 5, 7)
  - [x] Add `GetGlobalAdministratorsQuery` under `src/Hexalith.Tenants.Contracts/Queries/` as a `sealed class : IQueryContract` with `QueryType = "get-global-administrators"`, `Domain = "global-administrators"`, and `ProjectionType = "global-administrators"`.
  - [x] Add a query response DTO such as `GlobalAdministratorSummary` or `GlobalAdministratorDetail` as a `sealed record` with `/// <summary>` docs, matching existing query DTO conventions.
  - [x] Return `PaginatedResult<GlobalAdministratorSummary>` from the read query so the surface can page by protected cursor without inventing a separate list shape.
  - [x] Update query contract tests that currently expect exactly 5 query contracts; Story 4.2 intentionally makes this 6.

- [x] Implement the fixed-aggregate query handler and route (AC: 1, 2, 3, 4, 7)
  - [x] Add `GetGlobalAdministratorsQueryHandler` under `src/Hexalith.Tenants/Queries/Handlers/`.
  - [x] Read only `projection:global-administrators:singleton` through `IReadModelStore`; do not read tenant projections, tenant index, events, DAPR actors, or the state store directly from the UI.
  - [x] Enforce authorization server-side from the projected global administrator list. If the authenticated `UserId` is not present in `GlobalAdministratorReadModel.Administrators`, return forbidden without leaking whether other administrators exist.
  - [x] Use fixed identity values: tenant `"system"`, domain `"global-administrators"`, aggregate id `"global-administrators"`. Do not parse administrator IDs as GUIDs or ULIDs.
  - [x] Preserve cursor protection. Add a `TenantQueryCursorScopes.GetGlobalAdministrators(requesterUserId)` scope, or an equivalent scope-bound helper, and cover wrong-query/wrong-scope rejection.
  - [x] Important: `TenantQueryHandlerBase.Domain` is currently fixed to `GetTenantQuery.Domain` (`"tenants"`). Make that member safely overrideable or implement the new handler directly as `IDomainQueryHandler`; otherwise `DomainQueryDispatcher` will not select the handler for the `global-administrators` domain.
  - [x] Register the handler in `src/Hexalith.Tenants/Program.cs` alongside the existing query handlers.
  - [x] Add a controller route such as `GET /api/global-administrators` or `GET /api/global-administrators/users` in the existing query controller layer, dispatching a `QueryEnvelope` with the fixed identity. Pick one route and update tests/docs consistently.
  - [x] Update the Story 4.1 integration test that currently asserts global administrator routes are not exposed; that guard is obsolete once this story intentionally exposes the read contract. Keep the no-tenant-substitute guard.

- [x] Extend the UI BFF/query gateway for the global administrator list (AC: 1, 2, 3, 4, 5, 7)
  - [x] Add `GetGlobalAdministratorsAsync` to `ITenantQueryGateway` and `TenantQueryGateway`.
  - [x] Submit the new query through `IEventStoreGatewayClient.SubmitQueryAsync<PaginatedResult<GlobalAdministratorSummary>>` using tenant `"system"`, domain `"global-administrators"`, aggregate id `"global-administrators"`, `ProjectionType = "global-administrators"`, and `ProjectionActorType = TenantProjectionRouting.ActorTypeName` unless current EventStore routing evidence requires a different actor type.
  - [x] Map `304 Not Modified`, stale metadata, degraded metadata, 401/403, 404/501/503, invalid cursor, and missing payload into explicit UI snapshot states. Preserve last-confirmed rows for stale/degraded/not-modified states.
  - [x] Do not fall back to `ListTenantsQuery`, `GetTenantQuery`, `GetTenantUsersQuery`, `GetUserTenantsQuery`, tenant detail rows, member rows, user lookup, claims, or nav authorization reflection as administrator data.
  - [x] Keep support-safe error mapping: no raw problem details, correlation ids, tokens, decoded JWT payloads, stack traces, cursors, ETags, EventStore metadata, or production tenant/user data in rendered text.

- [x] Replace the missing-read-support page with the actual review surface (AC: 1, 2, 3, 4, 5, 6, 8)
  - [x] Extend `GlobalAdministratorsPage.razor` to load the new BFF query after authorization reflection passes; tenant owners or indeterminate callers still get fail-closed hidden/unavailable behavior.
  - [x] Render a real list/table with `data-testid="tenants-global-admins-list"` plus stable row/copy/freshness/action-reason selectors. Keep `data-testid="tenants-global-admins-area"` and `tenants-global-admins-nav`.
  - [x] Show administrator identity as literal caller-supplied user IDs. Truncate visually only; expose full accessible names and safe copy behavior. Do not normalize, hash, classify, or parse IDs.
  - [x] Show freshness from query metadata/ETag as a surface or row freshness badge. The current projection model stores only `HashSet<string> Administrators`; do not invent per-administrator timestamps unless the backend read model is intentionally extended and tested.
  - [x] Distinguish loading, empty, stale, degraded, unauthorized, unavailable, and invalid-cursor states. None of these may be shown as Success.
  - [x] Compute grant/remove availability as read-only placeholders only. Story 4.2 does not implement grant/remove flows; stale or unknown freshness must show visible unavailable reasons and fail closed.
  - [x] Preserve last-confirmed administrator rows during stale/degraded/refreshing states; do not overwrite them with in-flight intent or missing-read placeholder content.
  - [x] Ensure responsive layout keeps identity, platform authority context, freshness, and action/reason slots visible via stable dimensions, wrapping, or horizontal overflow; if the safety context cannot fit, command actions remain unavailable.

- [x] Update localization, accessibility, and styling evidence (AC: 2, 3, 4, 5, 6, 8)
  - [x] Add/replace EN and FR `Tenants.GlobalAdministrators.*` resource keys for real list states, row labels, freshness, copy feedback, unavailable reasons, and platform-governance scope copy. Keep whole strings; do not assemble translated fragments at runtime.
  - [x] Keep visible text plus icon/shape for every state; never encode by color only.
  - [x] Bind live-region politeness to state semantics, not color. Use assertive only for failure, degraded, unable-to-verify, or destructive-block style states; routine loading/current/empty states are polite or non-live.
  - [x] Preserve visible focus and forced-colors hooks in `GlobalAdministratorsPage.razor.css` and navigation CSS.
  - [x] Add or update component/static tests for row semantics, keyboard focus order, stable selectors, resource parity, forced-colors CSS, and no support-unsafe rendered strings.

- [x] Add focused backend, UI, and regression tests (AC: 1-8)
  - [x] Contracts: query contract naming/count, DTO serialization, string shape, and no enum/numeric surprises.
  - [x] Server query: authorized projected global administrator receives paginated rows; non-admin receives forbidden; missing authenticated user is rejected before state access; missing projection is forbidden or unavailable without leaking hidden admins; cursors are protected and scoped.
  - [x] Controller/integration: the selected global-administrator route dispatches tenant `"system"`, domain `"global-administrators"`, aggregate id `"global-administrators"`, query type `"get-global-administrators"`, and bounded page size/cursor payload.
  - [x] UI gateway: submits the fixed query, maps metadata to freshness states, preserves previous rows on 304/stale/degraded, and never submits tenant/user substitute queries.
  - [x] Page/component: authorized list, empty state, stale/degraded/unavailable/unauthorized states, support-safe copy, fail-closed action reasons, stable selectors, responsive CSS hooks, and EN/FR parity.
  - [x] Run focused validation first: `dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Release -m:1 --no-restore`, `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release -m:1 --no-restore`, `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release -m:1 --no-restore`, and `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`.
  - [x] For tests, use per-project `dotnet test` where it works. If the known .NET 10 Microsoft.Testing.Platform/VSTest issue appears, use the xUnit v3 executable fallback pattern recorded by Story 4.1.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 4 Story 4.2. Epic 4 covers global administrator governance and requires platform authority to stay separate from tenant membership. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.2: Review Global Administrators from Fixed Aggregate`]
- FR18 requires authorized operators to review global administrators separately from tenant membership. The surface is hidden from tenant owners, reads the single fixed-identity `global-administrators` aggregate, and shows identity plus freshness. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-18: Review global administrators`; `_bmad-output/planning-artifacts/epics.md#Story 4.2: Review Global Administrators from Fixed Aggregate`]
- FR19 grant/remove is not part of this story. Story 4.2 may render disabled placeholders or unavailable reasons for grant/remove, but it must not submit `SetGlobalAdministrator` or `RemoveGlobalAdministrator`. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-19: Grant or remove a global administrator`; `docs/tenants-ui-operations-shell-spec.md#4.2 Global administrator read-only surface`]
- Current sprint status explicitly asks for Story 4.2 after Story 4.1 completion. Older planning docs mark `ui-06-global-admin-read-only` planning-only; treat those as historical readiness inputs, not a blocker to this current sprint story once the story creates the missing read contract and story-specific a11y/l10n evidence. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; `docs/tenants-ui-phase-2-story-backlog.md#2026-06-05 Status Supersession`]

### Confirmed Backend Facts

- Fixed global administrator identity is `AggregateIdentity("system", "global-administrators", "global-administrators")` through `TenantIdentity.ForGlobalAdministrators()`. Reuse these literal values; do not parse tenant IDs or user IDs as GUIDs/ULIDs. [Source: `src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs`; `_bmad-output/project-context.md#Identity Rules`]
- The projection write path exists. `GlobalAdministratorProjectionHandler` rebuilds `GlobalAdministratorReadModel` and saves it to `projection:global-administrators:singleton` only for tenant `"system"` and aggregate `"global-administrators"`. [Source: `src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs`; `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs`]
- The current read model contains only `HashSet<string> Administrators`. It has no per-row timestamp, display name, email, tenant memberships, or role metadata. Do not fabricate those fields in the UI. [Source: `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs`]
- Existing tenant query authorization uses `TenantQueryHandlerBase.IsGlobalAdminAsync`, which reads the same projection key. That supports tenant query RBAC today, but it is not a user-facing global administrator list endpoint. [Source: `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`]
- Existing public query contracts are tenant/user/audit only. There is no `GetGlobalAdministratorsQuery` or global-administrator REST route before this story. [Source: `src/Hexalith.Tenants.Contracts/Queries/`; `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`; `_bmad-output/implementation-artifacts/4-1-global-administrators-navigation-and-read-contract-readiness.md#Confirmed Backend Facts`]
- Query handlers are explicitly registered in `src/Hexalith.Tenants/Program.cs`; adding a handler class alone is not enough. [Source: `src/Hexalith.Tenants/Program.cs`]

### Existing UI Implementation To Extend

- Story 4.1 added `OperationsShellNavigation` through `FrontComposerShell.Navigation`. It gates the Global Administrators nav entry with `ITenantsBffComposition.GlobalAdministratorsAuthorizationReflection`. Preserve this shell behavior. [Source: `src/Hexalith.Tenants.UI/Components/Layout/OperationsShellNavigation.razor`; `_bmad-output/implementation-artifacts/4-1-global-administrators-navigation-and-read-contract-readiness.md#Senior Developer Review (AI)`]
- `GlobalAdministratorsPage.razor` currently renders an honest missing-read-support state and fixed aggregate facts. Story 4.2 should replace that state for authorized callers once the real read contract exists, while retaining unauthorized fail-closed behavior. [Source: `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`; `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`]
- `TenantQueryGateway` is the existing server-side BFF read gateway used by UI pages. Follow its metadata, ETag, support-safe exception mapping, and previous-snapshot preservation patterns. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs`; `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`]
- UI pages currently call the BFF gateway from server-side Blazor components (`TenantsWorkspace.razor`, `TenantDetailPage.razor`, `MyTenantsPage.razor`, `UserMembershipLookupPage.razor`). It is acceptable for the 4.2 page to follow that pattern; do not add a generic Fluxor/store framework just for this story. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`]
- Existing support-safe copy helpers and state components are available. Reuse local conventions for stable selectors, badges, focus, forced-colors CSS, and resource parity instead of adding a separate UI framework. [Source: `src/Hexalith.Tenants.UI/Components/Shared/ListSurfaceStates.razor`; `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/SupportSafeCopyButton.razor`]

### Architecture And UX Guardrails

- Global administrators are platform-level governance principals in a separate singleton `global-administrators` scope. They must not be modeled as tenant membership or routed as tenant-domain data. [Source: `docs/tenants-ui-operations-shell-spec.md#4.2 Global administrator read-only surface`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#4. Glossary`]
- Authorization is server-enforced. UI authorization reflection controls disclosure only and must fail closed when indeterminate. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#NFR-2 Security & authorization`; `_bmad-output/project-context.md#Host Composition & Framework Rules`]
- Projection data is the source of truth. SignalR or live notifications, if later added, are refresh nudges only and must never turn an in-flight intent into confirmed administrator data. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Foundation`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#6. Core Interaction Principles`]
- Truth-state and list states must remain non-collapsed: loading, empty, error, stale, degraded, unauthorized/unavailable each need distinct copy and semantics. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md#7.1 Read-only freshness`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#State Patterns`]
- Freshness is `current`, `refreshing`, `aging`, `stale`, or `unknown`. Stale and unknown freshness block high-impact actions. Since this story is read-only, grant/remove affordances should render unavailable with visible reasons rather than interactive flows. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#State Patterns`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#4. Glossary`]
- FrontComposer table primitives may be used for read-only rows, but command flows are custom and out of scope. Do not add missing shared scaffolding to Tenants; keep Tenants-owned code to screen composition, DTOs, query handlers, gateway mapping, resources, and tests. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Read-Only Surface Consumption Map`; `AGENTS.md#Domain Implementation Boundary`]

### Accessibility, Localization, And Support Safety

- Baseline is WCAG 2.1 AA, with WCAG 2.2 AA targeted where the pinned Fluent UI Blazor / FrontComposer stack supports it. Story 4.2 still needs story-specific keyboard, focus, forced-colors, responsive, and localized-copy evidence. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#9. Accessibility & Localization`; `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#8. Per-Row Consumption Mapping`]
- Live-region politeness is semantic, never color-derived. Assertive is reserved for failure, degraded, unable-to-verify, or destructive blockers; routine read states should not over-announce. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Accessibility Floor`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/review-accessibility.md#A11Y-03`]
- Long user IDs may be visually truncated only. Full literal values must remain available to assistive tech and safe-copy affordances. [Source: `docs/tenants-ui-operations-shell-spec.md#5.1 Identifier truncation and accessibility`; `_bmad-output/project-context.md#Identity Rules`]
- All new state labels, row labels, freshness labels, unavailable reasons, and copy feedback must be localizable as whole EN/FR strings with resource parity. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#9. Accessibility & Localization`; `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`]
- Do not show or copy bearer tokens, decoded JWT contents, raw claims, raw payloads, stack traces, internal correlation IDs, message IDs, cursors, ETags, raw EventStore metadata, or production tenant/user data. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `docs/tenants-ui-operations-shell-spec.md#5.3 Support-safe references`]

### Previous Story Intelligence

- Story 4.1 established the shell navigation and current missing-read-support state. Story 4.2 should be a direct extension: keep the nav gating, replace the authorized placeholder with a real read surface, and update tests that were intentionally guarding the pre-4.2 missing route. [Source: `_bmad-output/implementation-artifacts/4-1-global-administrators-navigation-and-read-contract-readiness.md#Completion Notes List`]
- Story 4.1 review found that projection-manifest-only navigation did not render in the shared shell. Continue using the explicit `FrontComposerShell.Navigation` slot and `OperationsShellNavigation`; do not regress to manifest-only assumptions. [Source: `_bmad-output/implementation-artifacts/4-1-global-administrators-navigation-and-read-contract-readiness.md#Senior Developer Review (AI)`]
- Story 4.1 validation passed UI-focused builds/tests, while broader Tier 2 server tests had pre-existing documentation/AppHost evidence failures. If similar failures recur outside changed 4.2 scope, document them rather than hiding them. [Source: `_bmad-output/implementation-artifacts/4-1-global-administrators-navigation-and-read-contract-readiness.md#Debug Log References`]
- Stories 3.2-3.4 and 4.1 reinforced EN/FR resource parity, support-safe rendered text, focus-visible/forced-colors CSS, xUnit v3 executable fallback, and no optimistic success. Carry those bars forward. [Source: `_bmad-output/implementation-artifacts/3-4-remove-tenant-configuration-key-with-consequence-preview.md`; `_bmad-output/implementation-artifacts/4-1-global-administrators-navigation-and-read-contract-readiness.md`]

### Git Intelligence

- Recent commits are story-scoped Conventional Commits: `feat(story-4.1): Global Administrators Navigation and Read Contract Readiness`, followed by `chore(story-automator): record story 4.1 completion`. A compatible implementation commit would be `feat(story-4.2): Review Global Administrators from Fixed Aggregate`. [Source: `git log --oneline -5`]
- Current dirty work before creating this story included only `_bmad-output/story-automator/orchestration-1-20260605-153745.md`. It is unrelated to the story file and sprint-status update; do not revert it. [Source: `git status --short`]

### Latest Technical Information

- Use the repo-pinned stack: .NET 10, Blazor InteractiveServer, EventStore gateway/query contracts, FrontComposer shell, Fluent UI Blazor, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not add package versions or new dependencies. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `Directory.Packages.props`]
- External package/API research is not required for Story 4.2 because the implementation consumes existing repo-pinned APIs and local shared-source contracts; the risk is incorrect scope/routing, not changing third-party APIs.

### Project Structure Notes

- Expected contract changes: `src/Hexalith.Tenants.Contracts/Queries/` and related `Contracts.Tests`.
- Expected server changes: `src/Hexalith.Tenants/Queries/Handlers/`, `src/Hexalith.Tenants/Queries/TenantQueryCursorScopes.cs`, `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`, `src/Hexalith.Tenants/Program.cs`, and matching server/integration tests.
- Expected UI changes: `src/Hexalith.Tenants.UI/Services/Gateways/`, `src/Hexalith.Tenants.UI/State/GlobalAdministrators/` or equivalent local page snapshot model, `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor(.css)`, resources, and `tests/Hexalith.Tenants.UI.Tests/`.
- Do not modify submodules, shared FrontComposer code, AppHost/Aspire plumbing, package metadata, command contracts, aggregate command behavior, or EventStore server registrations for this story unless a compile-time break proves a direct integration requirement.
- Detected conflict to resolve deliberately: Story 4.1 tests asserted no global-admin read route/contract. Story 4.2 intentionally changes that behavior. Replace those assertions with tests proving the new route uses the fixed `global-administrators` scope and never tenant/user substitute endpoints.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 4.2: Review Global Administrators from Fixed Aggregate`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 4: Global Administrator Governance`
- PRD/UX: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-18: Review global administrators`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Information Architecture`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Accessibility Floor`
- Architecture/specs: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`; `docs/tenants-ui-operations-shell-spec.md#4.2 Global administrator read-only surface`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#8. Backend and Data Boundaries`; `docs/tenants-ui-frontcomposer-dependency-map.md#Read-Only Surface Consumption Map`; `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#8. Per-Row Consumption Mapping`
- Backend code: `src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs`; `src/Hexalith.Tenants.Contracts/Queries/`; `src/Hexalith.Tenants.Server/Projections/GlobalAdministratorReadModel.cs`; `src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs`; `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`; `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`; `src/Hexalith.Tenants/Program.cs`
- UI code/tests: `src/Hexalith.Tenants.UI/Components/Layout/OperationsShellNavigation.razor`; `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/OperationsShellNavigationTests.cs`; `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, architecture, targeted PRD/UX/readiness docs, Story 4.1 previous-story intelligence, current backend query/projection/controller code, current UI global administrator page/navigation/gateway code, focused tests, and recent git history.
- Checklist validation was run against `.agents/skills/bmad-create-story/checklist.md`; corrections from validation are reflected in explicit fixed-domain routing, query handler domain-selection guard, obsolete Story 4.1 test updates, no tenant/user endpoint substitute rules, projection-freshness limits, support-safety constraints, and story-specific accessibility/localization/test evidence tasks.
- Dev-story validation: `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Release -m:1 --no-restore` hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility; xUnit v3 executable fallback was used for runnable tests.
- Focused builds passed: Contracts.Tests, Server.Tests, IntegrationTests, and UI.Tests with `dotnet build <project> -c Release -m:1 --no-restore`.
- Release solution build passed: `dotnet build Hexalith.Tenants.slnx -c Release -m:1 --no-restore`.
- Executable tests passed: Contracts.Tests 105/105, UI.Tests 465/465, Client.Tests 47/47, Testing.Tests 181/181, Sample.Tests 31/31, focused Server global-admin query tests 5/5, focused Integration global-admin route test 1/1, and Server event-contract documentation tests 7/7.
- Full Server.Tests executable was attempted and still has 6 known unrelated failures from missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` references and stale deployment-readiness evidence; Story 4.2-specific server/documentation failures were fixed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 4.2 to adding the fixed global-administrator read contract and rendering the authorized read-only review surface.
- Story context identifies the main implementation risks: handler domain mismatch, confusing platform authority with tenant membership, fabricating row metadata/freshness, leaking hidden administrators to unauthorized users, leaving obsolete no-route tests in place, and showing false Success for stale/degraded/unavailable states.
- Added the fixed `get-global-administrators` query contract and `GlobalAdministratorSummary` DTO, with contract count and serialization tests updated for six query contracts.
- Added `GetGlobalAdministratorsQueryHandler`, overrideable handler domain dispatch, global-administrator cursor scoping, route registration, and `GET /api/global-administrators` controller dispatch using tenant `system`, domain `global-administrators`, and aggregate id `global-administrators`.
- Added UI BFF snapshot models and `GetGlobalAdministratorsAsync` gateway mapping for fixed query submission, 304/stale/degraded preservation, invalid/unavailable/unauthorized states, and support-safe error behavior without tenant/user query fallback.
- Replaced the placeholder global administrator page with a localized review table, stable selectors, platform-scope copy, freshness badges, support-safe copy affordance, fail-closed action reasons, and responsive/forced-colors/focus CSS.
- Updated event contract reference documentation to include `GetGlobalAdministratorsQuery`, `GlobalAdministratorSummary`, and `GET /api/global-administrators`.

### File List

- `_bmad-output/implementation-artifacts/4-2-review-global-administrators-from-fixed-aggregate.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/event-contract-reference.md`
- `src/Hexalith.Tenants.Contracts/Queries/GetGlobalAdministratorsQuery.cs`
- `src/Hexalith.Tenants.Contracts/Queries/GlobalAdministratorSummary.cs`
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants/Program.cs`
- `src/Hexalith.Tenants/Queries/Handlers/GetGlobalAdministratorsQueryHandler.cs`
- `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryCursorScopes.cs`
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/_Imports.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRow.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsReason.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsRequest.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsSurfaceKind.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryContractNamingTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryDtoSerializationTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Documentation/EventContractReferenceDocumentationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Queries/GetGlobalAdministratorsQueryHandlerTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Support/TenantQueryTestHarness.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`

### Change Log

- 2026-06-06T14:17:15+02:00 - Created Story 4.2 context and marked it ready for development.
- 2026-06-06T14:39:16+02:00 - Implemented fixed global-administrator read contract, backend route/handler, UI BFF, review surface, localization/accessibility evidence, documentation, and focused regression tests; marked story ready for review.
- 2026-06-06 - Senior Developer Review (AI) completed by Jérôme Piquot. All 8 ACs verified implemented; focused suites re-run green (Contracts 105/105, Server global-admin + documentation 12/12, UI 470/470, Integration global-admin 3/3). No CRITICAL/HIGH/MEDIUM functional issues. Auto-fixed review findings: removed orphaned Story 4.1 resource keys (`ReadContract.*`, `Recovery.*`, `States.Missing*`, `States.UnknownFreshness`, `Unavailable.Missing*`) from EN/FR resx and replaced the contradictory `TenantsUiCompositionTests` assertion (which still claimed "read support is not implemented yet") with assertions on the real in-use `State.Stale.Title`/`State.Unauthorized.Title` keys; corrected stale "five handlers" comment in `TenantQueryTestHarness`. Status set to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot
**Date:** 2026-06-06
**Outcome:** Approve (auto-fixes applied)

### Scope

Adversarial review of Story 4.2 implementation against all 8 acceptance criteria and the claimed File List, with git cross-reference and re-execution of the focused test suites. Reviewed the new read contract/DTO, the fixed-aggregate query handler, cursor scope, controller route, UI BFF gateway mapping, snapshot/state model, the review-surface page (`.razor`/`.css`), EN/FR resources, and all changed/added tests.

### Verification

- **Git vs File List:** Consistent. Every changed/added source and test file is documented in the story File List; the only undocumented working-tree change is an unrelated story-automator orchestration log (expected, left untouched).
- **AC1–AC8:** All implemented and evidenced.
  - AC1: handler reads only `projection:global-administrators:singleton`; tests assert no tenant/index reads; gateway submits the fixed `system`/`global-administrators`/`global-administrators` identity with `ProjectionActorType = TenantProjectionRouting.ActorTypeName`; no tenant/user substitute queries.
  - AC2: rows show literal identity + freshness; scope copy is "Platform authority, not tenant owner" (never "tenant ownership").
  - AC3: distinct loading/empty/stale/degraded/unauthorized/unavailable/invalid states; non-admin and missing-projection callers fail closed as Forbidden with no hidden-admin leakage; missing authenticated user rejected before state access.
  - AC4: grant/remove render as read-only placeholders with visible unavailable reasons; stale/unknown freshness shows the freshness reason; last-confirmed rows preserved on 304/stale/degraded via gateway `previous` handling.
  - AC5: only literal user IDs and support-safe copy exposed; tests assert no tokens/`access_token`/`/api/tenants`/`/api/users` leakage.
  - AC6: responsive CSS (overflow/wrapping, max-widths, `42rem` breakpoint) and stable selectors present (`tenants-global-admins-list`, `-row`, `-area`, `-nav`, etc.).
  - AC7/AC8: contracts (count→6, serialization), server (authorized/forbidden/missing-user/missing-projection/cursor-scope), controller/integration (fixed-scope dispatch + signed cursor), gateway (state mapping + previous-row preservation), and component (states, support-safe copy, fail-closed reasons, EN/FR parity, forced-colors/focus CSS) tests all pass.
- **Test re-execution (xUnit v3 executable fallback, .NET 10):** Contracts 105/105; Server global-admin handler 5/5 + event-contract documentation 7/7; UI 470/470; Integration global-admin routes 3/3. Release builds of all four focused test projects: 0 warnings / 0 errors.

### Findings and resolution

| Sev | Finding | Resolution |
| --- | --- | --- |
| Medium | `TenantsUiCompositionTests.Localization_resources_resolve_english_and_french_workspace_copy` still asserted `Unavailable.MissingReadSupport.Title` == "Global administrator read support is not implemented yet" — copy that directly contradicts this story's central deliverable. | Replaced the two obsolete assertions with `State.Stale.Title` and `State.Unauthorized.Title` (real, in-use keys); preserves fail-closed-copy coverage. |
| Low | 13 orphaned Story 4.1 resource keys (`ReadContract.*`, `Recovery.*`, `States.Missing*`, `States.UnknownFreshness`, `Unavailable.Missing*`) had zero source references after the page replacement. | Removed from both EN and FR resx; EN/FR parity preserved (48/48 global-admin keys). |
| Low | `TenantQueryTestHarness` xmldoc said it instantiates "the five tenant query handlers" but now creates six. | Corrected to "six". |

No CRITICAL or HIGH issues. The loading-flash-on-refresh (briefly showing the Loading state before the gateway returns) is the established repo convention (`TenantDetailPage` behaves identically) and was deliberately not changed.
