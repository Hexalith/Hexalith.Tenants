---
baseline_commit: f4281a100468c90acc6b3a4fd9d7a3f14bc9579e
---

# Story cc-2026-06-21: FrontComposer Aggregate List/Detail Extraction

Status: in-progress

<!-- Source of truth: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-21-reusable-aggregate-pages-and-tenant-search.md (Phase 2). -->
<!-- Correct Course story, not an epics.md numbered story. Phase 1 search story is already done. -->
<!-- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a **Hexalith domain UI implementer**,
I want **reusable aggregate list and aggregate detail page wrappers in FrontComposer, with Tenants re-based on those wrappers**,
so that **domain modules share operational browse/detail chrome and toolbars while Tenants keeps its domain-specific query, freshness, safety, search, and command behavior intact**.

This completes **G2** of the approved 2026-06-21 Correct Course: extract the common list/detail page chrome from the already-working Tenants list and detail surfaces into FrontComposer as reusable building blocks. Phase 1 (`cc-2026-06-21-memories-backed-tenant-search`) already completed the search-first prerequisite: Tenants now has real Memories-backed cross-set tenant search, fresh row hydration, AppHost wiring, and tests.

This story is **not** a new tenant list, a new tenant detail page, or a backend feature. It is an extraction and rebase story. The implementation must preserve the current Tenants behavior end-to-end, then move only the reusable page-level shell/chrome into FrontComposer.

### Cross-repo split

- **FrontComposer submodule:** define and implement the shared contracts/components for `FC-LST` and `FC-DTL`, expected names `FcAggregateListPage<TItem>` and `FcAggregateDetailPage<TItem>`, plus docs/tests/public-surface evidence. These changes belong in `Hexalith.FrontComposer/**`. Because that is a root-level submodule, do not edit it unless the dev-story run has explicit owner approval for this cross-submodule story. If that approval is not present, stop after producing a FrontComposer handoff spec; do not claim completion.
- **Tenants repo:** consume the new wrappers from `src/Hexalith.Tenants.UI` and re-base `TenantsWorkspace.razor` and `TenantDetailPage.razor` without changing the backend contract or browser security posture.
- **Memories submodule:** no work expected. Phase 1 search must keep working. Do not reopen the `SearchIndexEntryChanged` ingestion path or status-filter handoff in this story.

## Acceptance Criteria

1. **FC-LST and FC-DTL contracts are explicit.** FrontComposer records the shared contract for aggregate list/detail pages in its contract/docs area and exposes public components named `FcAggregateListPage<TItem>` and `FcAggregateDetailPage<TItem>` or an explicitly documented equivalent. The contract must be domain-agnostic: no `Tenant*`, EventStore query DTO, Memories, or Tenants resource keys in FrontComposer.

2. **Aggregate list wrapper composes existing FrontComposer/Fluent primitives.** The list wrapper uses `FcPageLayout`, `FcPageHeader`, the header `Actions` slot as the toolbar, Fluent layout primitives, and FC-TBL-compatible DataGrid/search/filter/state primitives where they fit. It accepts domain-supplied item rendering, columns, toolbar content, state content, search/filter values, paging callbacks, and row navigation callbacks. It must not own Tenants query semantics or require a FrontComposer `IQueryService`.

3. **Aggregate detail wrapper composes existing FrontComposer/Fluent primitives.** The detail wrapper uses `FcPageLayout`, `FcPageHeader`, `Actions` for toolbar commands, `Metadata` for return context and identifiers, and domain-supplied detail body/sections. It supports loading, unauthorized, not-found, unavailable/unknown, stale, degraded, and ready states without collapsing them. Multi-section detail content remains grouped with `FluentAccordion` or wrapper-supported section slots that render equivalent accordion semantics.

4. **Tenants list is re-based without behavior loss.** `TenantsWorkspace.razor` keeps routes `/` and `/tenants`, all query-bound context (`search`, `status`, `sort`, `desc`, `cursor`, `selected`, `anchor`), server-side search through `ITenantQueryGateway.ListTenantsAsync`, cursor paging, refresh/reset, return-context focus, create-tenant flow, My Tenants/User lookup links, `TenantDataGrid` row behavior or an equivalent domain row template, audit entry points, support-safe copy, and existing `data-testid` selectors.

5. **Tenants detail is re-based without behavior loss.** `TenantDetailPage.razor` keeps route `/tenants/{TenantId}`, safe `returnUrl` handling, BFF gateway detail load, blank-name fallback heading, support-safe tenant id copy, audit entrypoint, truth-state/freshness display, metadata/lifecycle/member/configuration sections, command-flow hosting, projection evidence providers, and the command-in-flight guard.

6. **No backend contract drift.** The story adds no Tenants backend endpoints, no EventStore generic query routing, no browser-side backend calls, and no client-side token/storage usage. The only backend egress remains server-side gateways/BFF services. `ListTenantsQuery`, `GetTenantQuery`, and existing REST read endpoints remain the source contracts.

7. **Phase 1 search and freshness stay intact.** Non-empty list search still round-trips to the server, uses Memories as match-set only, hydrates rows through the existing ETag-fresh detail read path, and degrades to the cursor list when Memories is unavailable. Clearing search still returns to the cursor list. Do not reintroduce client-side search over only the loaded page.

8. **Safety states remain non-collapsible.** The list still renders distinct loading, empty, filtered-empty, error/unauthorized, stale, and degraded states with localized Tenants copy and correct roles/live-region behavior. The detail still renders distinct loading, unauthorized, not-found, unavailable/unknown, stale, degraded, and ready states. No state may be dressed as Success unless the projection proves it.

9. **Fluent/FrontComposer governance holds.** Tenants UI remains Fluent v5/FrontComposer only: no raw interactive controls, raw forms, raw tables, root `<main>` wrappers, direct `<PageTitle>`, raw route-level `<h1>`, hard-coded semantic colors, native control CSS selectors, inline layout styles, or unmarked CSS ownership. Layout stays in `FluentStack`/`FluentGrid`/`FcPageLayout`; data surfaces stay in `FluentDataGrid` or FrontComposer grid primitives.

10. **Accessibility and responsive safety are preserved.** Identity/status/freshness safety context remains visible or fails closed with a visible reason. Status is never color-only. Keyboard navigation, focus return, accessible names, forced-colors hooks, overflow wrapping, and stable dimensions continue to pass existing tests. The wrapper must not hide a pending/stale marker behind toolbar, paging, sorting, or responsive layout.

11. **Localization ownership is unchanged.** FrontComposer wrappers expose parameters/slots and shell-level resources only. Tenants-owned copy stays in `TenantsResources.resx` and `TenantsResources.fr.resx` with whole-string keys. Do not move domain text into FrontComposer or assemble localized sentences from fragments.

12. **Tests prove wrapper contracts and Tenants behavior.** FrontComposer tests cover the new wrapper components, toolbar/action slots, states, section rendering, and conformance/public surface as appropriate. Tenants tests update to prove the rebased pages still pass `TenantListSurfaceTests`, `TenantDetailSurfaceTests`, `PageLayoutDeclarationTests`, `DomainUiFluentConformanceTests`, and gateway search tests. Do not weaken or delete regression tests to make the extraction pass.

13. **Public/package boundary is deliberate.** If `FcAggregateListPage<TItem>` or `FcAggregateDetailPage<TItem>` becomes public package surface, update the relevant FrontComposer public API/package baseline and docs intentionally. If the components remain internal to Shell, document why domain modules can still consume them safely.

14. **Support-safety remains hard.** Rendered markup, copy, snapshots, tests, docs, and logs must not expose bearer tokens, decoded JWTs, raw command payloads, raw EventStore metadata, internal correlation ids, ETags as user copy, stack traces, or real PII.

15. **Verification is current-version aware.** Use the pinned package versions from `Directory.Packages.props`; do not add package versions to `.csproj` files or bump Fluent/Aspire/Dapr as part of this story. Because the Fluent MCP docs are for `5.0.0.26139` while the repo pins `5.0.0-rc.3-26138.1`, compile and component tests are the authority for exact parameter names.

## Tasks / Subtasks

> Build order: define the FrontComposer contract first, implement wrappers with focused tests, then re-base Tenants list/detail one page at a time while keeping existing tests green after each page.

- [ ] **Task 1 - Contract and boundary setup** (AC: 1, 13, 15)
  - [ ] Record the `FC-LST` / `FC-DTL` contract in the appropriate FrontComposer `_bmad-output/contracts/` or project-docs location, including component names, package/public-surface posture, state model, toolbar slots, data-source callbacks, and non-goals.
  - [ ] Confirm explicit owner approval for `Hexalith.FrontComposer/**` edits in this dev-story run. If absent, create only the handoff spec and stop without marking the story done.
  - [ ] Read current FrontComposer `FcPageHeader`, `FcPageLayout`, DataGrid filter/search/status components, and their tests before writing wrappers.
  - [ ] Confirm no changes are needed in `Hexalith.Memories/**` for this story.

- [ ] **Task 2 - Implement `FcAggregateListPage<TItem>` in FrontComposer** (AC: 1, 2, 8, 9, 10, 13)
  - [ ] Compose `FcPageLayout`, `FcPageHeader`, header `Actions` toolbar, optional `Metadata`, and a domain-provided grid/body slot.
  - [ ] Expose parameters for title/heading/eyebrow/description/test id, layout mode, search value/callback, filter/status slot or value/callback, refresh/reset/paging actions, state kind/content, item list/data-source result, and row/detail navigation callback.
  - [ ] Reuse existing FC-TBL primitives where compatible, but do not force the generated projection Fluxor search model onto domain pages that need server-side BFF search.
  - [ ] Keep the wrapper generic over `TItem`; no Tenants DTOs, resources, or query gateway dependencies.

- [ ] **Task 3 - Implement `FcAggregateDetailPage<TItem>` in FrontComposer** (AC: 1, 3, 8, 9, 10, 13)
  - [ ] Compose `FcPageLayout`, `FcPageHeader`, safe toolbar/metadata slots, state rendering slots, and ready-body/sections slots.
  - [ ] Support route-level heading focus, nonblank heading requirements, and caller-supplied fallback heading for blank item names.
  - [ ] Support multi-section detail surfaces without forcing Tenants domain sections into FrontComposer.
  - [ ] Ensure sections can render `FluentAccordion ExpandMode="AccordionExpandMode.Multi"` with first/all relevant items expanded by default.

- [ ] **Task 4 - FrontComposer tests, docs, and public surface** (AC: 1, 9, 10, 11, 12, 13, 15)
  - [ ] Add Shell component tests for both wrappers: toolbar action slot, metadata slot, loading/error/degraded/stale/ready states, section rendering, test ids, layout mode, and no domain copy.
  - [ ] Add/update governance tests if wrappers introduce new layout/CSS surface.
  - [ ] Update component inventory/docs and public API baselines if the components are package-facing.
  - [ ] Run the focused FrontComposer Shell test lane with `DiffEngine_Disabled=true` and the repo-required test command shape.

- [ ] **Task 5 - Re-base `TenantsWorkspace.razor`** (AC: 4, 6, 7, 8, 9, 10, 11, 14)
  - [ ] Replace page-level local chrome with `FcAggregateListPage<TenantListRow>` while keeping the Tenants-specific query state and callbacks.
  - [ ] Preserve server-side search (`OnSearchChanged` -> `LoadAsync`), status behavior, cursor history, reset/refresh, return-context focus, and query-string restoration.
  - [ ] Preserve `TenantDataGrid` or move its column template into the wrapper body without losing `ItemKey`, support-safe copy, audit entrypoint, pinned/safety classes, detail link context, and existing test ids.
  - [ ] Host `CreateTenantFlow` in the wrapper toolbar/body according to the contract without changing command lifecycle behavior.

- [ ] **Task 6 - Re-base `TenantDetailPage.razor`** (AC: 5, 6, 8, 9, 10, 11, 14)
  - [ ] Replace page-level local chrome with `FcAggregateDetailPage<TenantDetail>` while keeping the Tenants-specific sections and command flows.
  - [ ] Preserve safe back link, `returnUrl` validation, blank-name fallback, tenant id literal display/copy, audit entrypoint, truth-state badge, facts, and accordion sections.
  - [ ] Preserve all projection evidence providers and command activity callbacks. Command flows must still block when stale/unknown/unavailable and must not show optimistic success.
  - [ ] Keep `ITenantsBffComposition` and `ITenantQueryGateway` semantics local to Tenants.

- [ ] **Task 7 - Tenants tests and conformance** (AC: 4-12, 14)
  - [ ] Update `TenantListSurfaceTests` to prove the rebased list still renders grid controls, server-search round-trip, filtered-empty/degraded states, cursor paging, support-safe copy, and stable selectors.
  - [ ] Update `TenantDetailSurfaceTests` to prove gateway load, safe return URL, blank-name fallback, all state surfaces, command sections, resource parity, support-safety, and CSS responsive hooks.
  - [ ] Keep `DomainUiFluentConformanceTests`, `PageLayoutDeclarationTests`, and browser-backend guard tests green without weakening their assertions.
  - [ ] Add any new wrapper-consumption test needed to catch a regression that old tests would miss.

- [ ] **Task 8 - Verification** (AC: all)
  - [ ] Run focused FrontComposer tests for the new wrappers.
  - [ ] Run `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj`.
  - [ ] Run any affected sample/package tests if FrontComposer or Tenants public surface changes.
  - [ ] Build with warnings as errors through the repo's `.slnx` build path when practical; do not run solution-level `dotnet test`.

- [ ] **Task 9 - Story artifacts and handoff** (AC: 1, 12, 13)
  - [ ] Update this story's Dev Agent Record with exact files, tests, and any FrontComposer submodule commit/status.
  - [ ] If FrontComposer work cannot be completed in this repo run, create a handoff under `_bmad-output/planning-artifacts/` and leave the story status truthful.
  - [ ] Do not mark done until Tenants pages consume the wrappers and the focused tests prove behavior is preserved.

## Dev Notes

### Current State: What Exists Today

| File | Current state | What this story changes | Must preserve |
|---|---|---|---|
| `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` | Route page for `/` and `/tenants`; owns `FcPageLayout`, `FcPageHeader`, filters, toolbar buttons, create flow, list states, `TenantDataGrid`, cursor history, query string restoration, and server-side `LoadAsync`. | Replace only reusable page chrome/control scaffolding with `FcAggregateListPage<TenantListRow>`. Keep Tenants state/load logic unless the wrapper exposes a cleaner callback with identical behavior. | Server-side search, cursor paging, ETag reuse, status filter behavior, return context, stable selectors, list states, support-safe copy, audit links, no browser backend calls. |
| `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` | Route page for `/tenants/{TenantId}`; owns `FcPageLayout`, safe back link, state branches, `FcPageHeader`, facts, truth-state, and four accordion sections with command flows. | Replace reusable detail chrome/state framing with `FcAggregateDetailPage<TenantDetail>`. Keep Tenants-specific facts, sections, command flows, and evidence callbacks. | Safe `returnUrl`, blank-name fallback, exact state branches, command-in-flight guard, localization, support-safe copy, audit entrypoint, accordion sections. |
| `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor` | Tenants-specific `FluentDataGrid` with identity/status/member/owner/pending/freshness/audit columns, support-safe copy, detail/audit href delegates, and sortable Tenant/Status columns. | Either keep as the domain grid child of the generic list wrapper or split only if the reusable wrapper needs a `RenderFragment` column/body contract. | `ItemKey`, data-testids, safe id copy, audit entrypoint, safety classes, localized labels, `TruthStateBadge`, no raw table markup. |
| `src/Hexalith.Tenants.UI/Components/Shared/ListSurfaceStates.razor` | Tenants-local mapping of list surface states to localized state sections and reset/refresh actions. | May remain Tenants-local and be passed into wrapper state slots. Do not move Tenants copy into FrontComposer. | Distinct state selectors and roles/live-region semantics. |
| `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` | Owns tenant list/detail reads. Phase 1 added Memories search branch and ETag-fresh hydration. | No change expected unless wrapper consumption reveals an adapter type is needed. | Memories match-set only, detail hydration, support-safe degraded fallback, `ResolveFreshness`, REST read path, no EventStore generic query gateway. |
| `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.*` | Existing page header with `Actions` and `Metadata` slots, `PageTitle`, heading focus, and Fluent-based layout. | Reuse inside wrappers; do not fork the header. | `FocusHeadingAsync`, nonblank heading validation, Fluent-only styling, route-level header contract. |
| `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageLayout.*` | Layout mode declaration component for `FullWidth`/`Constrained` via shell coordinator. | Reuse inside wrappers; wrapper should forward `FcPageLayoutMode`. | Full-width list surfaces, constrained detail surfaces, no page-root layout wrappers. |
| `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcProjectionGlobalSearch.*`, `FcStatusFilterChips.*`, `FcFilterEmptyState.*`, `FcFilterResetButton.*`, `FcColumnFilterCell.*` | Existing FC-TBL pieces, mostly Fluxor/DataGrid navigation-state oriented. | Reuse where compatible, or keep wrapper generic enough that Tenants can supply its own server-side search controls. | Do not force a client-side/generated projection model onto Tenants. |
| `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs` | Locks server search, list states, cursor paging, support-safe/browser-backend guard, grid markers. | Update expected markup/component location only. | Do not weaken behavioral assertions. |
| `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` | Locks detail load, safe return URL, blank-name fallback, state branches, sections, resources, support-safety, CSS hooks. | Update selectors only if the wrapper contract intentionally changes the DOM wrapper. | Do not remove coverage for command sections or safe states. |
| `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` | Governance guard for Fluent-only controls/forms/tables, page layout/header, CSS ownership, raw layout budget. | Keep green; update only for intentionally new FrontComposer wrapper usage. | Do not raise budgets or allowlists without a code-review note. |

### Architecture Guardrails

- Tenants is a domain implementation. Generic UI scaffolding belongs in `Hexalith.FrontComposer`; Tenants consumes it. Do not duplicate a generic list/detail shell in Tenants after this story.
- The browser never calls Tenants/EventStore/Memories APIs directly and never stores tokens. InteractiveServer keeps backend access server-side.
- The Tenants list/detail read path is `ITenantQueryGateway`, not FrontComposer `IQueryService` and not EventStore generic query routing. This is load-bearing for D6 ETag/freshness.
- `TenantId` and `UserId` are meaningful caller-supplied strings. Never parse them as GUID/ULID or normalize casing.
- Projection truth is authoritative. SignalR is a freshness nudge only. Command success is confirmed only after re-query.
- All awaited calls in Tenants host/client code need `ConfigureAwait(false)` unless a Blazor dispatcher continuation intentionally uses `InvokeAsync`.
- Package versions are centralized. Never add `Version=` to a `.csproj`.

### Previous Story Intelligence

Phase 1 (`cc-2026-06-21-memories-backed-tenant-search`) is done and directly affects this story:

- `TenantsWorkspace.OnSearchChanged` now reloads from the server; do not reintroduce `ApplyVisibleRows()` search filtering.
- `TenantQueryGateway.SearchTenantsAsync` uses Memories as an index-only match-set and hydrates through the existing detail path. Preserve this separation.
- Memories API status attribute filtering remains deferred; Tenants currently filters hydrated authoritative status as the interim exact path.
- The previous implementation intentionally placed the index publisher in `samples/Hexalith.Tenants.Sample`, not in the broker-free `Hexalith.Tenants.Client`.
- The previous story showed submodule edits can happen only with explicit owner approval. Carry that discipline to FrontComposer.
- Existing test evidence from Phase 1: UI tests covered search round-trip, filtered empty, degraded fallback; gateway tests covered Memories failure and support-safety; publisher tests covered curated events.

### Git Intelligence

Recent commits show the implementation pattern to preserve:

- `7ef796f feat: Enhance tenant search functionality with pagination and improved state management` changed `TenantsWorkspace.razor`, `TenantQueryGateway.cs`, Tenant projection handling, and UI/gateway tests. This is the immediate behavior baseline for Phase 2.
- `4273bbe feat: Implement Memories Search-Index Ingestion for Tenant Search` added Memories contracts/submodule status, AppHost wiring, `MemoriesSearchIndexEventPublisher`, PRD/architecture updates, and the handoff spec. Do not undo those integration choices.
- `f4281a1 fix: mark Hexalith.Memories subproject as dirty` only adjusted submodule gitlinks/status. Verify submodule working trees before and after Phase 2.

### Latest Technical Information

- **Fluent UI Blazor:** Tenants and FrontComposer pin `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1`. The available Fluent UI Blazor MCP server documents `5.0.0.26139` and reports the project pin as incompatible for documentation accuracy. Treat MCP docs as near-current guidance, not compile truth; verify every component parameter against the pinned package build.
- **FluentDataGrid:** current docs describe `FluentDataGrid<T>` as the primary component for tabular data, with `Items`/`ItemsProvider`, `ItemKey`, `GenerateHeader`, sortable columns, resizing, `DisplayMode`, loading/error/empty content, and keyboard support. The Tenants grid already uses `Items`, `ItemKey`, `TemplateColumn`, sticky header, resizable columns, and start-pinned critical columns. Preserve compile-tested usage.
- **FluentStack/Accordion:** docs confirm `FluentStack` owns orientation/alignment/gap/wrap layout and `FluentAccordion` supports multi-expanded sections. Use these instead of raw layout wrappers or local CSS ownership.
- **NuGet package behavior:** Fluent UI Blazor packages wrap Fluent UI Web Components and include the matching script in the library, so this story must not add CDN scripts or raw web-component bootstrapping.
- **Aspire:** no AppHost changes are expected for the extraction. If a visual/E2E check needs AppHost changes, follow Aspire's declarative resource model and existing Tenants AppHost patterns; resource references and `WaitFor` express startup dependencies.

External references checked during story creation:
- Fluent UI Blazor NuGet package overview: https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components/
- Fluent Web Components Data Grid reference: https://learn.microsoft.com/en-us/fluent-ui/web-components/components/data-grid
- Aspire AppHost overview: https://aspire.dev/get-started/app-host/
- Aspire custom resources: https://aspire.dev/extensibility/custom-resources/
- Aspire 13.4 announcement: https://devblogs.microsoft.com/aspire/whats-new-aspire-13-4/

### Testing Standards

- Tenants tests: xUnit v3 + Shouldly + NSubstitute + bUnit. Test files are plural `{Class}Tests.cs`. Run test projects individually.
- FrontComposer tests: follow FrontComposer's own lane rules. Use `DiffEngine_Disabled=true` when running Verify-backed tests. Do not apply Tenants' per-project test rule to FrontComposer if its docs require solution-level filtered tests.
- Minimum focused Tenants verification: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj`.
- If package/public surface changes in FrontComposer, run the focused package/public API tests required by FrontComposer docs.

### Project Structure Notes

- Expected Tenants changes: `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`, `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`, maybe `TenantDataGrid.razor` and UI tests. Keep Tenants domain copy/resources in Tenants.
- Expected FrontComposer changes: new wrapper components under `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/` or a better existing Shell component folder, plus tests under `Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/`, docs/contracts, and public API baseline if required.
- Avoid AppHost, EventStore, Memories, server aggregate, contract DTO, or persistence changes unless a compile break proves they are necessary. Generic UI belongs in FrontComposer; Tenants should only adapt to the new shared components.

### Open Questions / Assumptions

- Assumption: the approved 2026-06-21 Correct Course authorizes the FrontComposer handoff, but actual submodule source edits still require explicit dev-story owner approval. The dev agent must verify that approval before editing `Hexalith.FrontComposer/**`.
- Assumption: the first implementation can keep `TenantDataGrid` as the domain-specific grid body passed into `FcAggregateListPage<TenantListRow>`. A later story can generalize column definitions further if other domains need it.

### References

- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-21-reusable-aggregate-pages-and-tenant-search.md#Phase 2 - FrontComposer reusable aggregate list/detail + toolbars`]
- [Source: `_bmad-output/implementation-artifacts/cc-2026-06-21-memories-backed-tenant-search.md`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Tenant search (Memories-backed, index-only; cc-2026-06-21)`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.2: Tenant List Triage`]
- [Source: `_bmad-output/planning-artifacts/epics.md#Story 1.3: Tenant Detail Navigation and Overview`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#tenant-data-grid`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#FrontComposer Readiness & Fallbacks`]
- [Source: `_bmad-output/project-context.md`]
- [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`]
- [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`]
- [Source: `src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`]
- [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor`]
- [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageLayout.razor`]
- [Source: `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/DataGrid/`]
- [Source: `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`]
- [Source: `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`]
- [Source: `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`]

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List

### Change Log

| Date | Change |
|---|---|
| 2026-06-21 | Created ready-for-dev story context for Phase 2 FrontComposer aggregate list/detail extraction and Tenants rebase. |
