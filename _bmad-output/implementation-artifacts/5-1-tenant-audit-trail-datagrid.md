---
baseline_commit: 75d1e90
created: 2026-06-06T15:28:04+02:00
---

# Story 5.1: Tenant Audit Trail DataGrid

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 5.1. -->

## Story

As an authorized Tenants user,
I want to browse a tenant audit trail as a flat, filtered, cursor-paginated list,
so that I can inspect recorded tenant activity without relying on hidden event payloads.

## Acceptance Criteria

1. Given an authorized user opens a tenant audit surface, when the audit list loads, then the UI queries `GET /api/tenants/{tenantId}/audit` through the server-side BFF and renders a flat AuditDataGrid, and the browser never calls the backend directly or stores backend access tokens.
2. Given audit entries are available, when the audit grid renders, then entries are stably ordered, cursor-paginated, and filterable by absolute date range and `AuditEventCategory`, and the surface targets about 500 events without unacceptable degradation.
3. Given audit list data is loading, empty, filtered-empty, stale, degraded, unauthorized, invalid-cursor, or errored, when that state occurs, then the UI renders a distinct localized, accessible state, and no state is collapsed into false Success or fabricated proof.
4. Given an audit cursor becomes invalid or scope-bound data changes, when the user refreshes or pages, then the BFF re-queries page 1 and the UI shows an honest list-refreshed notice, and invalidation is not treated as a generic error.
5. Given audit rows contain actor, target, tenant scope, outcome, timestamp, projection marker, category, and reference data, when the row renders, then only support-safe fields derived from structured audit data are shown, and raw event payloads, serialized command payloads, raw EventStore metadata, stack traces, tokens, internal correlation ids, and PII are never displayed or copied.
6. Given the audit grid is viewed at desktop, tablet, or mobile widths, when layout changes, then safety-critical columns, category, timestamp, outcome, freshness, and reference context remain visible or the surface fails closed with visible reason, and stable selectors such as `data-testid="tenants-audit-grid"` and `data-testid="tenants-audit-filter-category"` are present.
7. Given this story is complete, when verification is run, then unit/component tests cover BFF audit query mapping, date/category filters, cursor paging, invalid cursor refresh, empty/filtered-empty/stale/degraded/error states, support-safe row mapping, and stable selectors.
8. Given accessibility or E2E verification is run, then keyboard filtering, paging, forced-colors-safe statuses, live-region announcements, responsive safety, and no raw payload exposure are verified.

## Tasks / Subtasks

- [x] Add audit UI state models and BFF gateway support (AC: 1, 2, 3, 4, 5, 7)
  - [x] Add focused audit request/snapshot/row/reason/surface-kind models under `src/Hexalith.Tenants.UI/State/TenantAudit/` or an equivalent tenant-audit namespace.
  - [x] Add `GetTenantAuditAsync` to `ITenantQueryGateway`, `TenantQueryGateway`, and `UnavailableTenantQueryGateway`.
  - [x] Submit `GetTenantAuditQuery` through `IEventStoreGatewayClient.SubmitQueryAsync<PaginatedResult<TenantAuditEntry>>` using tenant `"system"`, domain `"tenants"`, aggregate id and entity id equal to the requested tenant id, query type `"get-tenant-audit"`, projection type `"tenants"`, and `ProjectionActorType = TenantProjectionRouting.ActorTypeName`.
  - [x] Include payload fields `{ from, to, category, cursor, pageSize }`; preserve the protected cursor as opaque text and never convert to offset/limit.
  - [x] Use the server-side audit page-size policy indirectly through the backend. The UI may choose a default page size for rendering, but must not assume cursor internals or over-fetch raw events.
  - [x] Map result metadata and HTTP failures into explicit audit states: loading, ready, empty, filtered-empty, stale, degraded, unauthorized, invalid-cursor, unavailable/error, and list-refreshed after invalidation.
  - [x] Preserve last-confirmed rows for stale/degraded/not-modified states where scope still matches the same tenant/filter set. Do not reuse rows across tenant id, date window, category, or cursor scope changes.
  - [x] Treat `400` with safe problem `reasonCode = "invalid-cursor"` as invalid-cursor/list-refreshed, not a generic error; clear cursor history and re-query page 1.
  - [x] Keep support-safe exception mapping: no raw ProblemDetails, cursors, ETags, message ids, correlation ids, tokens, raw payloads, stack traces, or EventStore metadata in rendered text.

- [x] Build the tenant-scoped audit surface and grid (AC: 1, 2, 3, 5, 6, 8)
  - [x] Add a tenant-scoped page such as `Components/Pages/TenantAuditPage.razor` with route `/tenants/{TenantId}/audit`.
  - [x] Keep Story 5.1 scoped to the tenant audit grid. Do not implement all Story 5.2 contextual entry points from tenant rows, user lookup, member rows, command results, or primary Audit navigation unless only a minimal non-disruptive link is needed to exercise this route.
  - [x] If the existing top-level Audit nav is touched, it must remain honest when no tenant scope is present; it must not query audit data without an explicit tenant id and must not fabricate a global audit view.
  - [x] Add an `AuditDataGrid` component under `Components/Tenants/Audit/` or an equivalent local folder, using Fluent/FrontComposer primitives already used by `TenantDataGrid`.
  - [x] Render columns for absolute timestamp, actor, target, tenant scope, `AuditEventCategory`, outcome, projection/freshness marker, and support-safe reference context.
  - [x] Pin safety-critical columns according to UX: timestamp, actor, and outcome at the start; keep category, freshness, and reference context visible via stable width, wrapping, or horizontal overflow.
  - [x] Add category and absolute date range filters with stable selectors including `data-testid="tenants-audit-filter-category"`.
  - [x] Add cursor pager controls; previous-page behavior may use local cursor history but must clear history when tenant/date/category filters change.
  - [x] Use stable selectors including `tenants-audit-grid`, `tenants-audit-row`, `tenants-audit-filter-from`, `tenants-audit-filter-to`, `tenants-audit-next`, `tenants-audit-previous`, `tenants-audit-refresh`, and state selectors for loading/empty/filtered-empty/stale/degraded/unauthorized/invalid-cursor/error.

- [x] Enforce support-safe audit row mapping (AC: 5, 7, 8)
  - [x] Map from `TenantAuditEntry` fields only: `EventId`, `EventType`, `Category`, `ActorId`, `Timestamp`, `TenantId`, `Target`, `Scope`, `Outcome`, and approved structured `NarrativePayload` keys.
  - [x] Treat `NarrativePayload` as structured narrative metadata, not raw event body. Show only approved keys needed for this grid, such as `userId`, `key`, role names, and safe event timestamps.
  - [x] Never display or copy raw serialized payloads, command payloads, raw EventStore metadata, stack traces, bearer tokens, decoded JWT contents, internal correlation ids, protected cursors, ETags, or unapproved PII.
  - [x] Use support-safe copy only for approved references. `EventId` is an audit/event reference and may be copied only if the support-safety classifier or local allowlist explicitly treats it as safe.
  - [x] Do not expose `MessageId`/`EventId` as a user identity and do not parse tenant ids, user ids, or event references as GUIDs/ULIDs.

- [x] Add localization, accessibility, responsive, and styling evidence (AC: 3, 5, 6, 8)
  - [x] Add EN/FR `Tenants.Audit.*` resource keys for titles, filters, columns, state messages, reset/refresh/paging, invalid-cursor list-refreshed notice, support-safe reference labels, and unavailable/error copy.
  - [x] Use whole localized strings with named placeholders. Do not assemble sentences from fragments at runtime.
  - [x] Bind live-region politeness by semantics: routine loading/refresh/list-refreshed states are polite; authorization/error/degraded/unavailable states are assertive only where they block or fail.
  - [x] Use visible text plus icon/shape or truth-state text for every state; never rely on color alone.
  - [x] Preserve keyboard operation for filters, reset, refresh, next/previous, row reference copy, and back navigation.
  - [x] Add forced-colors and focus-visible CSS hooks, responsive horizontal overflow or stable wrapping, and no overlapping text at mobile, tablet, desktop, and wide widths.
  - [x] Mobile is read-only audit reference only. Do not add high-impact correction or command actions in Story 5.1.

- [x] Add focused tests and validation (AC: 1-8)
  - [x] UI gateway tests prove the audit query uses the exact tenant/domain/aggregate/query/projection actor shape and payload fields, preserves opaque cursor, and never submits offset/limit.
  - [x] Gateway tests cover 304/not-modified with matching scope, stale/degraded metadata, 401/403 unauthorized, 400 invalid cursor, 503/unavailable, missing payload, support-safe exception redaction, and no tenant/user substitute query.
  - [x] Component/page tests cover initial load, date/category filtering, cursor paging, invalid cursor re-query page 1 and list-refreshed notice, empty and filtered-empty reset, stale/degraded/error/unauthorized states, row mapping, and no raw payload exposure.
  - [x] Static/component tests assert no browser-side `HttpClient`, backend route string calls from `.razor` components, `localStorage`, `sessionStorage`, `access_token`, raw payload text, cursor leakage, or EventStore metadata leakage.
  - [x] Resource tests cover EN/FR parity for all new `Tenants.Audit.*` keys.
  - [x] CSS/static tests cover forced-colors hooks, focus-visible hooks, horizontal overflow or stable wrapping, and safety-critical column preservation.
  - [x] Run focused validation first: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`.
  - [x] Run per-project tests or the xUnit v3 executable fallback if the known .NET 10 Microsoft.Testing.Platform/VSTest issue appears.
  - [x] If backend route behavior is touched, also run focused `tests/Hexalith.Tenants.IntegrationTests` audit controller tests and any affected `Server.Tests` query/projection tests.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 5 Story 5.1. Epic 5 covers audit evidence and forward recovery; Story 5.1 is only the flat tenant audit trail list. Receipts, scoped entry points, audit availability recovery, correction start, and correction proof linking are Stories 5.2 through 5.6. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.1: Tenant Audit Trail DataGrid`]
- FR20 requires a flat, stably ordered tenant audit list with date and `AuditEventCategory` filters, cursor pagination, distinct accessible states, and about 500 events without unacceptable degradation. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-20: Browse a tenant's audit trail`]
- The flat audit DataGrid is the Product/UX-approved interim `FC-AUD` fallback in place of `<AuditTimeline>`. Do not build `<AuditTimeline>` or generic audit timeline infrastructure inside Tenants. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#1. FC-AUD -> flat audit DataGrid`; `_bmad-output/planning-artifacts/architecture.md#Key Findings`]
- Story 5.1 is read-only audit reference. It must not start correction flows, submit commands, link original/corrective proof, or implement Audit Evidence Receipt behavior. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.2: Scoped Audit Evidence Entry Points`; `_bmad-output/planning-artifacts/epics.md#Story 5.3: Support-Safe Audit Evidence Receipt`; `_bmad-output/planning-artifacts/epics.md#Story 5.5: Start Forward Correction from Audit Evidence`]
- Current sprint status has `5-1-tenant-audit-trail-datagrid` in backlog and `epic-5` in backlog before this create-story run. This story is the first Epic 5 story, so `epic-5` is promoted to `in-progress`. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]

### Confirmed Backend Facts

- The backend endpoint already exists: `GET /api/tenants/{tenantId}/audit` on `TenantsQueryController.GetTenantAuditAsync`. It accepts `from`, `to`, `category`, `cursor`, and `pageSize`; validates tenant id, date window, category, authenticated `sub`, and protected cursor before dispatch. [Source: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`]
- The audit query contract already exists as `GetTenantAuditQuery` with `QueryType = "get-tenant-audit"`, `Domain = "tenants"`, and `ProjectionType = "tenants"`. Do not create a duplicate query contract. [Source: `src/Hexalith.Tenants.Contracts/Queries/GetTenantAuditQuery.cs`]
- The response item already exists as `TenantAuditEntry`. It exposes `EventId`, `EventType`, `Category`, `ActorId`, `Timestamp`, `TenantId`, `NarrativePayload`, and computed `Target`, `Scope`, and `Outcome`. [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`]
- `GetTenantAuditQueryHandler` is restricted to global administrators today. Non-admin callers get forbidden before audit projection access. Story 5.1 must reflect this server-enforced authorization and must not bypass or loosen it in UI code. [Source: `src/Hexalith.Tenants/Queries/Handlers/GetTenantAuditQueryHandler.cs`]
- Cursor scope is bound to tenant id, `from`, `to`, and category through `TenantQueryCursorScopes.GetTenantAudit`. Changing any of those values invalidates old cursors by design. [Source: `src/Hexalith.Tenants/Queries/TenantQueryCursorScopes.cs`]
- Audit page-size bounds are server-side: default 100 and maximum 1000. The handler filters by tenant/date/category and returns `PaginatedResult<TenantAuditEntry>` with protected cursors. [Source: `src/Hexalith.Tenants/Queries/TenantQueryPaginationPolicy.cs`; `src/Hexalith.Tenants/Queries/Handlers/GetTenantAuditQueryHandler.cs`]
- The audit projection already exists. `TenantAuditReadModel` builds support-safe structured narrative entries from supported tenant/global-admin events and sorts by timestamp then event id. `TenantAuditProjection` is a static helper, not a discoverable aggregate/projection actor. [Source: `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`; `src/Hexalith.Tenants.Server/Projections/TenantAuditProjection.cs`]

### Existing UI Implementation To Extend

- `ITenantQueryGateway` currently supports tenant detail/list, my-tenants/user-tenants, and global administrators. It does not yet expose tenant audit. Add a focused audit method rather than creating a second gateway. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`]
- `TenantQueryGateway` already has the BFF patterns this story needs: submit typed EventStore queries, pass opaque cursors in JSON payload, map metadata to freshness states, preserve previous snapshots on 304, and redact unsafe gateway failure details. Follow those patterns. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`; `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`]
- Existing pages load through the server-side gateway in Blazor InteractiveServer components. The browser components must not call backend URLs directly and must not use browser token storage. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`; `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs#Tenant_list_component_has_no_browser_backend_http_or_token_storage`]
- `TenantDataGrid` is the local pattern for a Tenants-specific Fluent grid composed from existing primitives with pinned critical columns and stable selectors. Reuse this local pattern for `AuditDataGrid`; do not add generic grid infrastructure to Tenants. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor.css`]
- `ListSurfaceStates` handles list states for tenant list only. Either create audit-specific state rendering or generalize only if it stays small and does not introduce cross-domain shared UI scaffolding into Tenants. Generic reusable list-state capability belongs in FrontComposer. [Source: `src/Hexalith.Tenants.UI/Components/Shared/ListSurfaceStates.razor`; `AGENTS.md#Domain Implementation Boundary`]
- `OperationsShellNavigation` currently renders Audit as an unavailable nav item with `data-testid="tenants-nav-audit"`. Story 5.2 owns broad Audit entry points. If Story 5.1 changes navigation, it must preserve honest scope requirements and stable selectors. [Source: `src/Hexalith.Tenants.UI/Components/Layout/OperationsShellNavigation.razor`; `_bmad-output/planning-artifacts/epics.md#Story 5.2: Scoped Audit Evidence Entry Points`]

### Architecture And UX Guardrails

- The consume-only backend is fixed at five read endpoints plus command status/submit endpoints. Do not add a new backend endpoint for Story 5.1 and do not assemble receipts or previews through a new server route. [Source: `_bmad-output/planning-artifacts/architecture.md#Key Findings`]
- Runtime is Blazor InteractiveServer with server-side BFF. Access tokens remain server-side; browser components get safe UI state only. [Source: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`; `_bmad-output/project-context.md#Technology Stack & Versions`]
- Cursors are opaque, signed, scope-bound, and session-scoped. Multi-replica/restart cursor durability is not guaranteed; UI must treat invalidation as a page-1 refresh with honest copy. [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`; `_bmad-output/project-context.md#Host Composition & Framework Rules`]
- Audit-data-grid UX requires a flat, stably ordered, cursor-paginated Fluent `FluentDataGrid`; filters by date and `AuditEventCategory`; columns timestamp, actor, target, category, outcome; and pinned timestamp, actor, outcome columns. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#audit-data-grid`]
- Audit-data-grid behavior requires distinct loading, empty, filtered-empty, and error states. Story 5.1 ACs add stale, degraded, unauthorized, and invalid-cursor states from the broader read-surface/truth-state model; keep them distinct. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Component Patterns`; `_bmad-output/planning-artifacts/epics.md#Story 5.1: Tenant Audit Trail DataGrid`]
- Safety-critical columns never drop at any supported width. Use horizontal scroll, pinning, stable widths, and wrapping; fail closed with visible reason if full safety context cannot be preserved. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Responsive & Platform`]
- The non-collapse invariant applies: audit pending, audit delayed, audit unavailable, missing implementation support, degraded, and unable-to-verify must never be styled, copied, or announced as success. Story 5.1 should not fabricate proof from the presence of audit rows. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#Core Interaction Principles`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#State Patterns`]

### Support Safety And Data Boundaries

- Audit rows must show support-safe fields derived from structured audit data. `NarrativePayload` is narrative metadata and must not be treated as raw event payload. [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#audit-evidence-receipt`]
- Do not display or copy bearer tokens, decoded JWT contents, command payloads, serialized event bodies, raw EventStore metadata, internal correlation ids, protected cursors, ETags, stack traces, or real PII. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#10. Guardrails - Privacy & Support-Safety`]
- Tenant ids and user ids are caller-supplied meaningful strings, not GUIDs or ULIDs. Preserve literal values, compare ordinally, and do not normalize. [Source: `_bmad-output/project-context.md#Identity Rules`]
- Sequence numbers are aggregate-local only. Audit ordering for this query comes from the projected audit entries and cursor returned by the handler; do not invent global ordering across tenants, topics, or services. [Source: `_bmad-output/project-context.md#DAPR, Eventing & Consumer Rules`; `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`]

### Previous Work And Git Intelligence

- Story 4.1 established explicit shell navigation through `FrontComposerShell.Navigation`; do not regress to manifest-only navigation assumptions. [Source: `_bmad-output/implementation-artifacts/4-1-global-administrators-navigation-and-read-contract-readiness.md#Senior Developer Review (AI)`]
- Story 4.2 added the current query gateway pattern for global administrators, including fixed query identity, cursor payloads, stale/degraded/not-modified preservation, and support-safe mapping. The audit gateway should follow this style. [Source: `_bmad-output/implementation-artifacts/4-2-review-global-administrators-from-fixed-aggregate.md#Completion Notes List`]
- Stories 4.3 and 4.4 were created as blocked/backlog records after governance readiness checks. Do not import global-administrator command behavior into Story 5.1. [Source: `_bmad-output/implementation-artifacts/4-3-grant-global-administrator-with-projection-confirmation.md`; `_bmad-output/implementation-artifacts/4-4-remove-global-administrator-with-last-admin-hard-stop.md`]
- Recent commits are story-scoped Conventional Commits: `feat(story-4.2): Review Global Administrators from Fixed Aggregate`, `docs(story-4.3): record blocked global administrator grant story`, and `docs(story-4.4): record blocked global administrator removal story`. A compatible implementation commit would be `feat(story-5.1): Tenant Audit Trail DataGrid`. [Source: `git log --oneline -5`]
- Current dirty work before creating this story included only `_bmad-output/story-automator/orchestration-1-20260605-153745.md`. It is unrelated to this story file and sprint-status update; do not revert it. [Source: `git status --short`]

### Latest Technical Information

- Use the repo-pinned stack: .NET 10, Blazor InteractiveServer, EventStore gateway/query contracts, FrontComposer shell, Fluent UI Blazor `5.0.0-rc.3-26138.1`, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not add package versions or new dependencies. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `Directory.Packages.props`]
- External package/API research is not required for Story 5.1 because the implementation consumes existing repo-pinned APIs and local contracts. The primary risk is data-boundary/support-safety mistakes, not third-party API drift.

### Project Structure Notes

- Expected UI changes: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs`, `TenantQueryGateway.cs`, `UnavailableTenantQueryGateway.cs`, a new `State/TenantAudit/` model set, a tenant audit page, an audit grid component and CSS, EN/FR resources, and `tests/Hexalith.Tenants.UI.Tests/`.
- Expected tests: UI gateway tests, component/page tests, resource parity tests, CSS/static safety tests, and possibly route smoke tests. Backend tests should be touched only if UI work exposes an existing route mapping gap.
- Avoid changes to audit query contracts, audit projection storage, `GetTenantAuditQueryHandler`, EventStore server registrations, AppHost/Aspire plumbing, package metadata, shared FrontComposer code, or submodules unless a compile-time break proves a direct integration need.
- Do not add generic hosting, query infrastructure, shared table primitives, audit timeline components, receipt components, or cross-domain support-safety scaffolding to Tenants. Move or request those in the relevant technical module if they are truly missing.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 5.1: Tenant Audit Trail DataGrid`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 5: Audit Evidence and Forward Recovery`
- PRD/UX: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-20: Browse a tenant's audit trail`; `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#1. FC-AUD -> flat audit DataGrid`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#audit-data-grid`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Component Patterns`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Key Findings`; `_bmad-output/planning-artifacts/architecture.md#Data Architecture`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`
- Backend code/tests: `src/Hexalith.Tenants.Contracts/Queries/GetTenantAuditQuery.cs`; `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`; `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`; `src/Hexalith.Tenants/Queries/Handlers/GetTenantAuditQueryHandler.cs`; `src/Hexalith.Tenants/Queries/TenantQueryCursorScopes.cs`; `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`; `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`; `tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditReadModelTests.cs`
- UI code/tests: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`; `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`; `src/Hexalith.Tenants.UI/Components/Shared/ListSurfaceStates.razor`; `src/Hexalith.Tenants.UI/Components/Layout/OperationsShellNavigation.razor`; `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, architecture, targeted PRD/UX/fallback documents, existing audit backend contracts/controllers/handlers/projections/tests, current UI gateway/grid/page/navigation patterns, recent story files, and recent git history.
- Checklist validation was run against `.agents/skills/bmad-create-story/checklist.md`; corrections from validation are reflected in explicit no-new-backend guidance, no raw payload exposure, invalid-cursor page-1 refresh handling, audit scope boundaries, no Story 5.2/5.3/5.5 scope creep, support-safe copy rules, and focused UI test requirements.
- Dev-story activation resolved customization with no prepend/append steps; loaded `_bmad-output/project-context.md`, `_bmad/bmm/config.yaml`, sprint status, and this story.
- Story status moved from `ready-for-dev` to `in-progress` at 2026-06-06T15:33:21+02:00 while preserving existing `baseline_commit: 75d1e90`.
- Focused validation first hit the known .NET 10 Microsoft.Testing.Platform/VSTest issue with `dotnet test`; used the xUnit v3 executable fallback as required by the story.
- Validation commands run: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`; `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests`; `dotnet build Hexalith.Tenants.slnx -c Release -m:1 --no-restore`; no-infrastructure test executables for Client, Contracts, Testing, Sample, and UI.
- Broader `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests` run failed 6 existing documentation/configuration tests unrelated to Story 5.1: missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and deployment-readiness summary expecting `Story 7.6A`.
- Broader `tests/Hexalith.Tenants.IntegrationTests/bin/Release/net10.0/Hexalith.Tenants.IntegrationTests` run failed in the local environment: 223 total, 54 failed, 33 skipped, with DAPR prerequisite skips and DaprException/InternalServerError failures in existing query-controller paths.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 5.1 to the tenant-scoped audit DataGrid and BFF/UI mapping.
- Story context identifies the main implementation risks: duplicating existing audit backend contracts, leaking raw event metadata/payloads, treating invalid cursor as generic failure, querying without explicit tenant scope, widening server authorization from UI code, hiding safety-critical audit columns responsively, and implementing later Epic 5 receipt/entry/correction work too early.
- Added tenant-audit-specific UI state models, support-safe audit row mapping, and `GetTenantAuditAsync` gateway support using the existing EventStore query gateway/BFF pattern.
- Added `/tenants/{TenantId}/audit` with server-side gateway loading, absolute date/category filters, cursor paging, explicit audit state rendering, list-refreshed handling after invalid cursor recovery, and a Fluent `AuditDataGrid` with pinned safety-critical columns.
- Added EN/FR `Tenants.Audit.*` localization keys, forced-colors/focus-visible/responsive CSS hooks, live-region state semantics, support-safe copy for approved event references, and stable selectors required by the story.
- Added focused gateway/component/static/resource/CSS tests for audit query shape, opaque cursors, date/category filters, cursor paging, invalid-cursor page-1 refresh, stale/degraded/unauthorized/unavailable/error states, missing payload, support-safe redaction, row mapping, no browser backend/token storage, EN/FR resource parity, and responsive/accessibility hooks.

### File List

- `_bmad-output/implementation-artifacts/5-1-tenant-audit-trail-datagrid.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants.UI/Components/_Imports.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor.css`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReason.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRequest.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRow.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSnapshot.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSurfaceKind.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`

### Change Log

- 2026-06-06T15:28:04+02:00 - Created Story 5.1 context and marked it ready for development.
- 2026-06-06T15:46:18+02:00 - Implemented tenant audit trail DataGrid, BFF gateway mapping, support-safe row rendering, localization, accessibility/responsive hooks, and focused validation tests; marked story ready for review.
- 2026-06-06 - Adversarial code review (story-automator-review). Validated all 8 ACs, every [x] task, and the UI→backend payload contract against `GetTenantAuditQueryHandler`/`DeserializeAuditPayload`. Fixed 1 MEDIUM (server-local timezone dependence in timestamp display + date-filter parsing) and added a UTC regression test. Status → done.

## Senior Developer Review (AI)

**Reviewer:** Administrator · **Date:** 2026-06-06 · **Outcome:** Approve (1 MEDIUM auto-fixed; 0 CRITICAL/HIGH)

### Scope verified

- **Git vs File List:** Consistent. All changed source files are documented in the File List; the only undocumented working-tree changes are excluded `_bmad-output/` artifacts.
- **Backend integration contract (highest risk):** The BFF query payload field names (`from`, `to`, `category`, `cursor`, `pageSize`), the ISO‑8601 `DateTimeOffset` serialization, and `ProjectionActorType = TenantProjectionRouting.ActorTypeName` with aggregate/entity id = tenant id were cross-checked against `GetTenantAuditQueryHandler` and `DeserializeAuditPayload`. They match exactly — no silent query-routing/field-name mismatch (the failure class flagged in prior stories).
- **ACs 1–8:** All implemented. BFF-only access with no browser `HttpClient`/token storage (static test enforced); cursor pagination with opaque cursor preserved and no offset/limit; 11 distinct accessible states with stable selectors and EN/FR resource parity (full key parity confirmed); invalid-cursor → page‑1 re-query + list-refreshed notice; support-safe row mapping that scrubs unsafe fields and only emits approved narrative keys; pinned safety-critical columns + responsive/forced-colors/focus-visible hooks.
- **Tests:** Real assertions (not placeholders). Full UI suite green: 503 passing (added 1 regression test).

### Findings

- 🟡 **MEDIUM (fixed):** `AuditDataGrid.TimestampLabel` used `ToLocalTime()` and `TenantAuditPage.ParseDate` used `DateTimeStyles.AssumeLocal`. Under Blazor InteractiveServer, "local" is the *server* timezone — non-deterministic across deployments and inconsistent with the app's UTC convention (`TenantDetailPage` `"u"`), shifting both the AC2 absolute date-range filter and the AC5/AC6 timestamp column. Fixed: render UTC (`ToUniversalTime()` + `… 'UTC'`) and parse filter input as UTC (`AssumeUniversal | AdjustToUniversal`); added `Tenant_audit_page_renders_timestamps_in_utc_independent_of_host_timezone`.
- 🟢 **LOW (noted):** `ListRefreshed`/`Stale`/`Degraded` with zero rows shows the state banner but no grid/empty cue (rare edge after invalid-cursor recovery to an empty page 1) — honest, just minimal.
- 🟢 **LOW (noted):** The required `tenants-audit-row` selector sits on an off-screen marker span (carrying `data-audit-reference`) rather than the visible FluentDataGrid row — adequate for reference/existence assertions, less ideal for visible-row E2E interaction.
