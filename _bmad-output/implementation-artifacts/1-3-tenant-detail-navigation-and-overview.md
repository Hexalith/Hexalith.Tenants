---
baseline_commit: 3967f4061c045f0cfe5ff8aa394439fd5267b7a8
---

# Story 1.3: Tenant Detail Navigation and Overview

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 1.3. -->

## Story

As a tenant operator,
I want to open tenant details from the list and return without losing context,
so that I can inspect a tenant while preserving my triage workflow.

## Acceptance Criteria

1. Given an authorized user is viewing the tenant list with filters, sorting, paging, selection, and scroll context, when the user opens a tenant detail surface and then returns to the list, then the prior filter, sort, cursor, selection, and context are preserved, and the return path does not reload into a misleading default list state.
2. Given a tenant detail deep link is opened directly, when the tenant exists and the caller is authorized to view it, then the detail surface loads through the server-side BFF query gateway using `GET /api/tenants/{tenantId}`, and the browser does not call the backend directly.
3. Given tenant detail projection data is available, when the overview renders, then it shows tenant status, lifecycle, metadata, member summaries, configuration summaries, member count, owner count, and freshness, and status/lifecycle are shown with text, icon/shape, semantic badge, and accessible label rather than color alone.
4. Given the detail projection is stale, degraded, unknown, unavailable, not found, or unauthorized, when the detail surface renders, then the UI shows the actual state with localized copy and recovery action where applicable, and it does not render stale or unauthorized data as current.
5. Given the tenant id or support-safe reference is visually truncated in detail, when the user inspects the value, then the full literal caller-supplied identifier remains available through accessible text and future copy affordances, and the UI does not parse or reformat the identifier as a GUID or ULID.
6. Given the detail surface is rendered at desktop, tablet, or mobile widths, when layout changes, then safety-critical state, freshness, lifecycle, and identity context remain visible or the view fails closed with a visible reason, and focus order and keyboard navigation remain predictable.
7. Given this story is complete, when verification is run, then component tests cover deep-link loading, list-context preservation, overview fields, unauthorized/not-found/stale states, localization keys, keyboard navigation, focus return, stable selectors, and responsive safety behavior.

## Tasks / Subtasks

- [x] Add a deep-linkable tenant detail route and overview surface (AC: 2, 3, 4, 5, 6)
  - [x] Add a page or route-backed component under `src/Hexalith.Tenants.UI/Components/Pages/` or `Components/Tenants/` with route `/tenants/{tenantId}`; keep the list routes `/` and `/tenants` intact.
  - [x] Render an operational overview, not a marketing/card dashboard: tenant identity, name/description metadata, status/lifecycle, member count, owner count, configuration summary, freshness, and recovery actions/states.
  - [x] Use `TruthStateBadge` or an intentionally extended shared truth-state component for freshness/status semantics; if it is extended, preserve text plus icon/shape plus accessible name and forced-colors behavior.
  - [x] Use stable selectors such as `data-testid="tenants-detail"`, `tenants-detail-back`, `tenants-detail-truth-state`, `tenants-detail-identity`, `tenants-detail-member-summary`, `tenants-detail-configuration-summary`, `tenants-detail-loading`, `tenants-detail-error`, `tenants-detail-stale`, `tenants-detail-degraded`, and `tenants-detail-unauthorized`.

- [x] Extend the server-side BFF query gateway for detail reads (AC: 2, 3, 4)
  - [x] Extend `ITenantQueryGateway` with a detail method, for example `GetTenantAsync(TenantDetailRequest request, TenantDetailSnapshot? previous, CancellationToken cancellationToken = default)`.
  - [x] Reuse the existing `IEventStoreGatewayClient` path in `TenantQueryGateway`; submit `GetTenantQuery` with `Domain = "tenants"`, `ProjectionType = "tenants"`, aggregate/entity id equal to the literal tenant id, and conditional `ifNoneMatch` support.
  - [x] Map `304 Not Modified` to the previous detail snapshot when present; if there is no previous snapshot, return an honest degraded/unknown state rather than an empty or current detail.
  - [x] Map `401`/`403`, `404`, stale metadata, degraded metadata, invalid gateway configuration, and generic gateway failures to distinct safe states; never surface raw ProblemDetails, stack traces, payload JSON, bearer tokens, internal correlation ids, or raw EventStore metadata.
  - [x] Keep browser components free of `HttpClient`, direct `/api/tenants` calls, token storage, local/session storage, or backend payload parsing.

- [x] Preserve tenant-list context across list/detail navigation (AC: 1, 6, 7)
  - [x] Add an explicit Tenants-owned list context model for search, status filter, sort column, sort direction, cursor, selected tenant id, and a return/scroll anchor.
  - [x] Prefer URL/query-string or Blazor navigation state that survives direct links and browser back/forward; do not use localStorage/sessionStorage for tenant context.
  - [x] Make tenant-list rows keyboard-reachable links/buttons to detail while preserving the current context in the generated detail URL or return state.
  - [x] On return to `/tenants`, restore filters/sort/cursor before rendering the list as ready; do not flash or settle on the default list as if context were lost.
  - [x] Return focus to the launching row/action when possible; if the row is no longer visible after refresh, focus the list heading or grid with a localized explanation.

- [x] Reconcile detail overview data with current contracts without fabricating values (AC: 3, 4, 5)
  - [x] Use `TenantDetail` as the source for `TenantId`, `Name`, `Description`, `Status`, `Members`, `Configuration`, and `CreatedAt`.
  - [x] Derive member count and owner count from `TenantDetail.Members`; show unknown/degraded when detail is unavailable rather than fake zeros.
  - [x] Treat lifecycle as the currently available `TenantStatus`/projection status unless a stronger lifecycle field exists locally; do not add a domain-contract field only for UI convenience.
  - [x] Summarize configuration by count and safe grouped metadata only in this story. Full read-only configuration display belongs to Story 1.6, and member table detail belongs to Story 1.7.
  - [x] Keep tenant ids/user ids as literal caller-supplied strings; never call `Guid.TryParse`, `Ulid.TryParse`, or reformat them.

- [x] Add Tenants-owned localization and responsive/accessibility styling (AC: 3, 4, 5, 6)
  - [x] Add whole-string `.resx` keys under `Tenants.Detail.*` in both invariant and French resource files; do not assemble sentence fragments at runtime.
  - [x] Remove or stop asserting obsolete Story 1.1 placeholder resource keys/tests if touched by this work; Story 1.2 review already identified leftover `Tenants.Workspace.*` placeholder keys and unused CSS selectors as cleanup debt.
  - [x] Preserve forced-colors hooks, visible focus, stable dimensions, and no-color-only status treatment.
  - [x] Preserve critical identity, status/lifecycle, freshness, and recovery context at mobile/tablet widths using horizontal overflow, stacking, or a visible fail-closed state.

- [x] Update focused tests and route smoke coverage (AC: 1, 2, 4, 7)
  - [x] Add gateway tests in `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs` for successful detail mapping, `304` reuse, unauthorized/not-found, degraded/stale metadata, and sanitized gateway errors.
  - [x] Add bUnit/component tests for direct `/tenants/{tenantId}` rendering, detail loading/error/stale/degraded/unauthorized states, localization keys, stable selectors, keyboard-reachable back navigation, and context-preserving list return.
  - [x] Extend no-browser-backend tests so all Razor components still avoid direct backend calls and token/local storage.
  - [x] Update `TenantsUiRouteSmokeTests` only if the hosted UI route smoke can assert the new route without requiring live tenant data; keep DAPR/Aspire prerequisite handling consistent with current integration tests.

## Dev Notes

Story 1.3 delivers the read-only tenant detail navigation and overview slice. It may add the detail route/surface, detail query gateway method, detail state models, list-context preservation, localization, styling, and tests required for tenant detail overview. It must not implement full configuration table behavior, member-table management, lifecycle/metadata/configuration commands, audit grids, copy-to-clipboard feedback, consequence previews, create/edit/disable tenant flows, or any mock tenant data. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.3: Tenant Detail Navigation and Overview`]

### Existing Implementation Context

- Story 1.1 created the Blazor InteractiveServer UI host, FrontComposer shell composition, Tenants manifest, AppHost registration, localization resources, and UI test project. Do not recreate the host, shell, or AppHost foundations. [Source: `_bmad-output/implementation-artifacts/1-1-tenants-ui-host-bootstrap.md#Completion Notes List`]
- Story 1.2 replaced the bootstrap placeholder with the read-only tenant list surface, query gateway, row state model, list components, localization, and focused UI/gateway tests. Build on those files; do not introduce a parallel UI access pattern. [Source: `_bmad-output/implementation-artifacts/1-2-tenant-list-triage.md#Completion Notes List`]
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` currently owns `/` and `/tenants`, local search/filter/sort state, cursor history, refresh/reset, and the `TenantDataGrid`. Detail navigation must preserve those semantics. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`]
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor` currently renders identity, status, member count, owner count, pending, and freshness. Extend it to expose a detail launch affordance only if the launch preserves safety-critical columns and selectors. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`]
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` already calls `GetTenantQuery` privately to enrich list counts. Story 1.3 should promote/reuse that detail read path instead of adding another transport style. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`]
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs` currently returns a safe list error when EventStore gateway configuration is missing. Add a safe detail unavailable state there as well. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs`]
- Current UI tests use bUnit, xUnit v3, NSubstitute, Shouldly, and `AddFluentUIComponents`. Keep Shouldly assertions and plural test class files. [Source: `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`; `_bmad-output/project-context.md#Testing Rules`]

### Contract And Gateway Requirements

- `GetTenantQuery` is the detail contract: `QueryType = "get-tenant"`, `Domain = "tenants"`, `ProjectionType = "tenants"`. [Source: `src/Hexalith.Tenants.Contracts/Queries/GetTenantQuery.cs`]
- `TenantDetail` contains `TenantId`, `Name`, `Description`, `Status`, `Members`, `Configuration`, and `CreatedAt`. It does not currently expose ETag, projection timestamp, projection version, separate lifecycle, pending command state, or support-safe reference. Do not extend this contract casually for UI convenience and do not fabricate missing values. [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantDetail.cs`; `_bmad-output/project-context.md#C# Language & Contract Rules`]
- Tenant query endpoints are protected REST adapters over EventStore `SubmitQuery`; controllers validate route/query input, derive authenticated user from JWT `sub`, validate opaque cursors, and dispatch to the projection actor. UI authorization is reflective only; server query/projection handling is authoritative. [Source: `docs/event-contract-reference.md#Query API Reference`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#8. Cross-Cutting Non-Functional Requirements`]
- Reads use conditional request semantics where supported. `If-None-Match` to `304 Not Modified` is the freshness primitive; unmeasurable freshness is `unknown`, not current. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md#8. Backend and Data Boundaries`; `_bmad-output/planning-artifacts/epics.md#Story 1.3: Tenant Detail Navigation and Overview`]
- Runtime is Blazor InteractiveServer with a server-side BFF. Browser code must not hold backend access tokens or call Tenants/EventStore APIs directly. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]

### UX, Accessibility, And Safety Requirements

- The Tenants workspace is an operational console. Keep dense, scannable layouts; avoid marketing heroes, decorative card dashboards, and oversized type inside the detail surface. [Source: `docs/tenants-ui-responsive-layout-and-visual-system-spec.md#2. Typography and Layout for Dense Operational Screens`]
- Status, lifecycle, freshness, degraded, stale, unauthorized, and unavailable states must be perceivable without color alone. Pair text with icon/shape/semantic label and preserve forced-colors behavior. [Source: `_bmad-output/planning-artifacts/epics.md#UX Design Requirements`; `docs/tenants-ui-responsive-layout-and-visual-system-spec.md#1.2 No-color-only invariant`]
- Preserve selection, filters, and scroll across navigation, especially tenant list to detail and back. [Source: `_bmad-output/planning-artifacts/epics.md#UX Design Requirements`; `docs/tenants-ui-operations-shell-spec.md#3.2 Context preservation (both directions)`]
- Full identifiers must remain available through accessible text when visually truncated. Story 1.8 owns full copy-control implementation; this story must not add unsafe copy behavior or expose sensitive backend references. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.3: Tenant Detail Navigation and Overview`; `_bmad-output/planning-artifacts/epics.md#Story 1.8: Support-Safe Identifier Copy and Epic 1 Readiness Evidence`]
- The reconciled navigation model is Tenants, Global Administrators, Audit as primary navigation, with Users contextual rather than a co-equal tab. Do not follow older operations-shell text that listed Users as primary navigation. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#6. User Journeys & Navigation`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/planning-artifacts/epics.md#UX Design Requirements`]

### Scope Boundaries

- Do not add backend query endpoints, domain-contract annotations, generated FrontComposer artifacts, shared UI infrastructure in Tenants, or generic test harness helpers. If a generic capability is missing, use the established Tenants-local pattern for this story and record a FrontComposer/shared-module follow-up. [Source: `AGENTS.md#Domain Implementation Boundary`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#8. Backend and Data Boundaries`]
- Do not add command behavior. Story 1.3 is read-only and must never display optimistic command success, command lifecycle, consequence previews, or audit proof as if implemented. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant Workspace Triage and Read-Only Insight`]
- Do not call the full detail page "configuration view" or "member table" complete. Configuration read-only is Story 1.6 and member table/action availability is Story 1.7. [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.6: Read-Only Tenant Configuration View`; `_bmad-output/planning-artifacts/epics.md#Story 1.7: Tenant Member Table and Action Availability`]

### Technology And Framework Requirements

- .NET SDK `10.0.302`, target `net10.0`, nullable/implicit usings, `TreatWarningsAsErrors=true`. [Source: `global.json`; `Directory.Build.props`; `_bmad-output/project-context.md#Technology Stack & Versions`]
- Fluent UI Blazor remains pinned through FrontComposer at `5.0.0-rc.3-26138.1`; do not upgrade Fluent as part of this story. The public Fluent UI Blazor documentation currently emphasizes v4 docs/releases, so verify exact v5 RC APIs against the local pinned source/package before using new DataGrid, badge, button, focus, or token APIs. [Source: `references/Hexalith.FrontComposer/Directory.Packages.props`; `_bmad-output/implementation-artifacts/1-2-tenant-list-triage.md#Technology And Framework Requirements`; `https://www.fluentui-blazor.net/`]
- Use Tenants-owned `.resx` resources with dotted PascalCase keys under `Tenants.` for all tenant-detail copy. Shell chrome strings remain FrontComposer-owned. [Source: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`; `_bmad-output/implementation-artifacts/1-2-tenant-list-triage.md#UX, Accessibility, And Safety Requirements`]
- Run test projects individually; do not run solution-level `dotnet test`. Use `.slnx` for restore/build only. [Source: `_bmad-output/project-context.md#Testing Rules`; `_bmad-output/project-context.md#Code Quality & Style Rules`]

### Existing Files To Update And Preserve

- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs`: extend with detail read behavior while preserving existing list signature for Story 1.2 tests.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`: reuse the existing `SubmitQueryAsync` and `GetTenantQuery` path; keep `ConfigureAwait(false)` on awaits.
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs`: add safe detail unavailable behavior without mock tenant data.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`: preserve list route behavior, filters, sort, cursor history, refresh/reset controls, and safety markers while adding detail launch/return context.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`: preserve pinned/safety-critical columns and stable selectors if adding row navigation.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`: add `Tenants.Detail.*` keys and update obsolete placeholder assertions only where the story touches them.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`, `TenantsWorkspaceTests.cs`, and `Services/Gateways/TenantQueryGatewayTests.cs`: extend rather than replacing existing Story 1.2 coverage.

### Project Structure Notes

- Source should stay in `src/Hexalith.Tenants.UI/`, primarily `Components/Pages/`, `Components/Tenants/`, `Components/Shared/`, `Services/Gateways/`, `State/TenantDetail/`, `State/TenantList/`, `State/TruthState/`, `Resources/`, and page/component CSS as needed.
- Tests should stay in `tests/Hexalith.Tenants.UI.Tests/` unless route smoke coverage requires the existing `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs`.
- The architecture documents are broader than this story. Treat their planned detail/members/configuration/audit surface map as the north star, not permission to build all later surfaces in Story 1.3.
- Known planning conflict: `docs/tenants-ui-operations-shell-spec.md` still says Users is primary navigation, while the PRD/architecture/epics reconcile Users as contextual. Follow the reconciled PRD/architecture/epics for implementation.

### Previous Story Intelligence

- Story 1.2 is done and created the list surface and gateway patterns this story should extend. It also documents that full `Server.Tests` and `IntegrationTests` have unrelated DAPR/Aspire prerequisite failures in this environment; report those if encountered rather than hiding them. [Source: `_bmad-output/implementation-artifacts/1-2-tenant-list-triage.md#Debug Log References`]
- Story 1.2 review left one high-priority follow-up: add a routing/authorization safety test ensuring unauthenticated/unauthorized users cannot bootstrap a permissive BFF client path. If this story touches gateway registration or route auth behavior, add the guard here. [Source: `_bmad-output/implementation-artifacts/1-2-tenant-list-triage.md#Tasks / Subtasks`]
- Story 1.2 review left a medium follow-up: add French localizer assertions for new list keys and remove obsolete placeholder assertions. Story 1.3 should not expand this debt; add French detail keys/tests for newly introduced copy. [Source: `_bmad-output/implementation-artifacts/1-2-tenant-list-triage.md#Tasks / Subtasks`]
- Story 1.1 found direct UI host startup may be blocked in this sandbox by socket binding. Use build/component tests and Aspire smoke where available; do not treat inability to bind Kestrel in the sandbox as product failure. [Source: `_bmad-output/implementation-artifacts/1-1-tenants-ui-host-bootstrap.md#Debug Log References`]

### Git Intelligence

- Recent commits show Story 1.1 and Story 1.2 were implemented as `feat(story-1.1): Tenants UI Host Bootstrap` and `feat(story-1.2): Tenant List Triage`; follow that story-scoped commit style if committing later. [Source: `git log --oneline -5`]
- Story 1.2 changed the exact UI, gateway, state, resource, and test files listed above. Preserve those list behaviors while adding detail rather than replacing the list surface. [Source: `git show --stat --name-only 3967f40`]

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 1.3: Tenant Detail Navigation and Overview`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 1: Tenant Workspace Triage and Read-Only Insight`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`, `#API & Communication Patterns`, `#Enforcement Guidelines`
- PRD: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#7.1 Tenant Discovery & Triage`, `#8. Cross-Cutting Non-Functional Requirements`
- Query contracts: `src/Hexalith.Tenants.Contracts/Queries/GetTenantQuery.cs`, `TenantDetail.cs`, `TenantMember.cs`
- Existing UI implementation: `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`, `Components/Tenants/TenantDataGrid.razor`, `Services/Gateways/TenantQueryGateway.cs`
- UX/support specs: `docs/tenants-ui-operations-shell-spec.md#3. Tenant Detail and Member Access-Review Context Preservation`, `docs/tenants-ui-responsive-layout-and-visual-system-spec.md`, `docs/tenants-ui-truth-state-and-action-availability-spec.md`
- Project rules: `_bmad-output/project-context.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story artifact analysis completed against `epics.md`, `architecture.md`, PRD, operations-shell/truth-state/responsive specs, Story 1.1 and 1.2 artifacts, persistent project context, current UI source/tests, query contracts, and recent git history.
- External documentation check: Fluent UI Blazor public docs currently emphasize v4.14.1 while this repo is pinned to a v5 RC through FrontComposer; implementation must verify local pinned APIs and must not upgrade Fluent for this story.
- 2026-06-06: Added red-phase gateway tests for detail query submission, 304 reuse, safe unauthorized/not-found/unavailable states, stale/degraded metadata, and sanitized errors.
- 2026-06-06: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -m:1 /nr:false -p:NuGetAudit=false -v:minimal` passed.
- 2026-06-06: `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -parallel none -noLogo` passed, 40/40.
- 2026-06-06: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build -m:1 /nr:false -p:NuGetAudit=false` remains blocked before test execution by the known .NET 10 Microsoft.Testing.Platform VSTest target error; in-process xUnit v3 runner used for execution evidence.
- 2026-06-06: `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -m:1 /nr:false -p:NuGetAudit=false -v:minimal` passed.
- 2026-06-06: Tier 1 plus UI in-process test executables passed: Contracts 103/103, Client 47/47, Testing 181/181, Sample 31/31, UI 40/40.
- 2026-06-06: `Hexalith.Tenants.Server.Tests` in-process runner failed 6 pre-existing documentation/configuration tests because `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` is missing and a deployment-readiness summary expectation references Story 7.6A; no failures touch Story 1.3 UI files.
- 2026-06-06: `Hexalith.Tenants.IntegrationTests` in-process runner failed 54 and skipped 29 under unavailable DAPR prerequisites; failures are server/integration DaprException/internal-server-error paths and not caused by Story 1.3 UI changes.

### Completion Notes List

- Story context created with explicit route, gateway, list-context preservation, safety, localization, responsive, and test guardrails.
- Checklist validation applied in YOLO mode; no user input was requested.
- Added `/tenants/{TenantId}` detail page with safe loading, stale, degraded, unavailable, not-found, and unauthorized states plus operational overview fields sourced from `TenantDetail`.
- Promoted the existing BFF EventStore detail query path to `ITenantQueryGateway.GetTenantAsync`, including conditional ETag handling and safe exception/state mapping.
- Added URL/query-string list return context via a Tenants-owned navigation model and keyboard-reachable tenant detail links.
- Added invariant and French `Tenants.Detail.*` resources and responsive/forced-colors detail styling.
- Added focused gateway and bUnit coverage for detail loading, state rendering, localization keys, stable selectors, context-preserving return URLs, no browser backend calls, and safe gateway errors.

### File List

- `_bmad-output/implementation-artifacts/1-3-tenant-detail-navigation-and-overview.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor.css`
- `src/Hexalith.Tenants.UI/Components/_Imports.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantDetailRequest.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantDetailSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantDetailSurfaceKind.cs`
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs`

### Change Log

- 2026-06-06: Implemented Story 1.3 tenant detail route, gateway detail reads, list return context, localization/styling, and focused tests; moved status to review.
- 2026-06-06: Senior Developer Review (AI) auto-fix pass: corrected tenant query projection-actor routing, implemented focus return + real list-row anchor target, hardened 304 truth-state handling, documented the previously-omitted route smoke test, and added regression coverage; UI suite 46/46; status moved to done.

## Senior Developer Review (AI)

**Reviewer:** Administrator (adversarial autonomous review) · **Date:** 2026-06-06 · **Outcome:** Changes Requested → auto-fixed and verified

### Findings and resolutions

- **[HIGH][AC2 – FIXED]** `TenantQueryGateway` submitted both the detail and list `SubmitQueryRequest` without `ProjectionActorType`, so `QueryRouter` defaulted them to EventStore's generic `"ProjectionActor"` instead of the tenants-owned `TenantProjectionRouting.ActorTypeName` (`"TenantsProjectionActor"`). The tenant projection is hosted by a distinct actor type ("to avoid placement collisions"), and the reference `DaprTenantQueryService` plus the integration fixtures already target that constant — so against the live backend the deep-link detail read and the list read would never reach the authoritative tenant projection. Fixed `CreateDetailQuery` and `CreateListQuery` to set `ProjectionActorType: TenantProjectionRouting.ActorTypeName`; added gateway assertions. [`src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`]
- **[HIGH][AC6/AC7 – FIXED]** The "return focus to the launching row / focus the list heading" subtask was marked complete, but `anchor=tenant-row-{id}` was dead data: no DOM element carried that id and there was no focus management at all. Added `id="tenant-row-{TenantId}"` to the grid identity cell (real anchor target) and focus the list heading on return from detail via `OnAfterRenderAsync` + `ElementReference.FocusAsync()`, paired with the existing localized return-context explanation (the spec's documented fallback). Added a regression assertion that the anchor target and focusable heading render. [`Components/Tenants/TenantDataGrid.razor`, `Components/Pages/TenantsWorkspace.razor`, `tests/.../TenantDetailSurfaceTests.cs`]
- **[MEDIUM][Transparency – FIXED]** `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` was modified (new hosted detail-route smoke test) but omitted from the File List. Added it to the File List above.
- **[LOW][AC4 – FIXED]** The `GetTenantAsync` 304/Not-Modified branch forced any non-Stale previous snapshot to `Ready`/`Current`, which could render a previously **degraded** detail as current. Now preserves the prior truth-state kind and only refreshes freshness to `Current` for a previously `Ready` snapshot. [`src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`]

### Validation

- `dotnet build Hexalith.Tenants.slnx -c Release --no-restore -p:NuGetAudit=false` → succeeded, 0 warnings / 0 errors (`TreatWarningsAsErrors`).
- `Hexalith.Tenants.UI.Tests` in-process runner → 46/46 passed, 0 failed.
- Pre-existing `Server.Tests` (missing `pubsub.yaml`) and `IntegrationTests` (DAPR/Aspire prerequisites unavailable in this sandbox) failures are unchanged and unrelated to these UI-layer fixes.

### Notes / observations (no change required)

- ASCII status/freshness glyphs (`OK` / `!` / `?`) remain the icon/shape signal, consistent with the Story 1.2 `TruthStateBadge` pattern and paired with text + semantic badge + accessible label (not color-only). Acceptable for this story.
- Configuration is summarized by count and distinct safe prefix-group count only; no configuration values or member detail are exposed, preserving the Story 1.6/1.7 scope boundary.
