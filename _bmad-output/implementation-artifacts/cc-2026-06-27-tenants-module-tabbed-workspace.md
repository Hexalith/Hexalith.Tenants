---
baseline_commit: ba14356a8b2b648eda24a4dd7fbd25d60e0d674d
title: 'Tenants module tabbed workspace and single navigation entry'
type: 'correct-course-ui-ia'
created: '2026-06-27'
status: 'done'
sprint_key: 'cc-2026-06-27-tenants-module-tabbed-workspace'
source_proposal: '_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-27.md'
approval: 'Administrator approved 2026-06-27, Option A - lookup-backed Users tab'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-27.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md'
---

# Story cc-2026-06-27: Tenants Module Tabbed Workspace And Single Navigation Entry

Status: done

<!-- Correct Course story, not an epics.md numbered story. -->
<!-- Completion note: Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a Hexalith operator or tenant user,
I want the shell left navigation to show one Tenants module entry and use page-local tabs inside the Tenants workspace,
so that Tenants-domain read surfaces stay grouped under the module instead of competing as separate shell entries.

This implements the approved 2026-06-27 Correct Course, Option A. The Users tab is lookup-backed by the existing `GET /api/users/{userId}/tenants` behavior. It must not claim to be an exhaustive all-users membership inventory. A real all-users inventory remains out of scope until Product/API approves and builds an authorization-scoped backend read query.

## Acceptance Criteria

1. The Tenants FrontComposer registration contributes exactly one Tenants left-menu entry for this module: title fallback `Tenants`, href `/tenants`, bounded context `tenants`, localized with `Tenants.Navigation.Tenants`, and no separate Tenants nav entries for `/tenants/my`, `/tenants/users`, or `/global-administrators`.
2. Activating the Tenants module entry opens `/tenants`; the shell manifest/icon remains registered so the bounded-context tile still uses the localized Tenants category and `Regular.Size20.BuildingPeople` icon.
3. `/tenants` renders page-local tabs with at least `Tenants` and `Users`, using FrontComposer or Fluent UI Blazor v5 tab primitives. Prefer `FcPageToolbar` tabs because FrontComposer already owns a page toolbar tab contract.
4. The `Tenants` tab contains the existing tenant list behavior: server-side search through `ITenantQueryGateway.ListTenantsAsync`, status filter, in-grid sorting, cursor paging, refresh/reset, create-tenant accordion, list states, freshness, support-safe copy, detail/audit row links, and stable `tenants-list-*` / `tenants-workspace` selectors.
5. The `Tenants` tab includes a safe "my tenants" filter or view mode that reuses the existing `GetMyTenantsAsync` / `GetUserTenantsQuery` self-audit semantics. It must not client-filter the currently loaded tenant page and call that complete; it either renders the existing membership rows honestly or issues the existing authorized self-membership query.
6. The `Users` tab contains the existing user membership lookup behavior backed by `ITenantQueryGateway.GetUserTenantsAsync` and `GET /api/users/{userId}/tenants`. Labels, empty states, and descriptions must say lookup/search semantics and must not imply a complete all-users list.
7. Existing `/tenants/my` and `/tenants/users` deep links remain reachable. They either redirect/canonicalize to `/tenants?tab=tenants&scope=mine` and `/tenants?tab=users...` or remain alias pages that render the same tab content with the requested active tab. In both cases, route smoke and bUnit coverage must prove old links do not break.
8. Tenant detail, tenant audit, member-row audit entry points, command result links, and lookup-result audit entry points preserve active tab and list/user context through `returnUrl`, `returnFocus`, and query parameters. Returning from detail/audit must not reset users to the wrong tab or lose search/status/sort/cursor/userId context.
9. Global Administrators and Audit are no longer Tenants left-menu entries. Existing routes and implemented pages remain accessible through approved internal/contextual paths, but this story must not delete their page implementations or backend/query behavior.
10. The implementation adds no backend endpoint, no new query contract, no browser-side backend API call, no token storage, and no EventStore generic query routing. Backend egress remains through the server-side BFF gateways.
11. All changed controls use FrontComposer and Blazor Fluent UI v5 components. Do not add raw interactive HTML controls, raw tables, raw forms, local shell/page-layout scaffolding, custom theme tokens, or generic UI infrastructure inside Tenants.
12. Accessibility, localization, responsive, forced-colors, and support-safety evidence is updated: page-local tabs are keyboard reachable, active tab is announced/represented, visible focus remains, state copy is whole-string EN/FR `.resx`, no color-only status is introduced, and no rendered output exposes payloads, tokens, ETags/cursors as user-copy, correlation ids, stack traces, or PII.
13. Tests are updated or added for the new IA: nav entry count and href, active tab routing/query aliases, Tenants tab list behavior, my-tenants mode/filter, Users lookup tab, old route aliases, localization parity, stable selectors, keyboard/focus behavior, no authorization leakage, and no all-users inventory claim.
14. Documentation/evidence files that currently assert the old shell navigation shape are reconciled, at minimum `tests/test-summary.md` and any directly affected Tenants-owned docs/spec notes. The story does not need to rewrite historical completed story files unless a current test/doc asserts the old behavior as live truth.

## Tasks / Subtasks

- [x] Task 1 - Collapse Tenants shell navigation to one entry (AC: 1, 2, 9, 13)
  - [x] Update `TenantsFrontComposerRegistration.RegisterDomain` to register one `FrontComposerNavEntry` for `/tenants` only.
  - [x] Preserve `DomainManifest` localization/icon behavior.
  - [x] Update `TenantsUiCompositionTests.FrontComposer_registration_exposes_tenants_nav_entries_and_minimal_manifest` to assert exactly one Tenants entry.
  - [x] Update stale tests that currently expect `/tenants/my`, `/tenants/users`, or `/global-administrators` in the Tenants nav registration.

- [x] Task 2 - Add page-local tabs to `TenantsWorkspace.razor` (AC: 3, 4, 11, 12)
  - [x] Use `FcPageToolbar` with `FcPageToolbarTab` or direct `FluentTabs` only if `FcPageToolbar` cannot fit the aggregate list page slot.
  - [x] Add query-bound active tab state such as `tab=tenants|users` and normalize unknown tabs to `tenants`.
  - [x] Keep the existing `FcAggregateListPage<TenantListRow>` composition and list body for the Tenants tab.
  - [x] Ensure tab controls have stable selectors, accessible labels, keyboard operation, and localized EN/FR labels.

- [x] Task 3 - Recompose My Tenants as a Tenants-tab mode or alias (AC: 5, 7, 8)
  - [x] Reuse `GetMyTenantsAsync`, `UserTenantMembershipSnapshot`, `MyTenantsDataGrid`, and `MyTenantsState`.
  - [x] Decide the smallest maintainable implementation: inline self-audit mode under the Tenants tab, a view switch inside the tab, or an alias route that selects the tab/mode.
  - [x] Preserve cursor paging, ETag reuse, stale/degraded/invalid/unauthorized/unavailable states, support-safe copy, and audit entry points.
  - [x] Keep `/tenants/my` working through redirect/canonicalization or alias rendering, with test coverage.

- [x] Task 4 - Recompose User Membership Lookup as the Users tab (AC: 6, 7, 8, 10)
  - [x] Move or host the existing `UserMembershipLookupPage` behavior inside the `/tenants` Users tab without changing the BFF gateway contract.
  - [x] Preserve literal caller-supplied user id handling; never parse `UserId` as GUID/ULID.
  - [x] Preserve `userId`, `sort`, and `cursor` query behavior or provide deterministic canonical equivalents under `/tenants?tab=users`.
  - [x] Keep `/tenants/users?userId=...` working through redirect/canonicalization or alias rendering.
  - [x] Ensure visible copy says lookup/search and does not claim complete user inventory.

- [x] Task 5 - Preserve contextual routes and return links (AC: 8, 9)
  - [x] Update `TenantListNavigationContext` so detail/audit return URLs include active tab and any new scope/mode fields.
  - [x] Update membership-grid audit return URLs so lookup/self-audit results return to the right tab and mode.
  - [x] Verify `TenantDetailPage` safe `returnUrl` handling still allows `/tenants...` query routes and rejects unsafe external targets.
  - [x] Do not remove `GlobalAdministratorsPage`, `TenantAuditPage`, or their route tests unless a separate approved story changes their IA.

- [x] Task 6 - Resources, docs, and conformance (AC: 11, 12, 14)
  - [x] Add/update `Tenants.Workspace.Tabs.*` or equivalent keys in `TenantsResources.resx` and `.fr.resx`.
  - [x] Update `tests/test-summary.md` lines that describe the old primary navigation model.
  - [x] Keep `DomainUiFluentConformanceTests`, `PageLayoutDeclarationTests`, resource parity tests, and support-safety tests green without raising budgets or allowlists unless the code-review note explains why.

- [x] Task 7 - Focused tests and verification (AC: all)
  - [x] Add bUnit coverage for `/tenants` default tab, tab switching callback/query normalization, my-tenants mode, Users lookup tab, route aliases, and old deep-link behavior.
  - [x] Verified `TenantsUiRouteSmokeTests` needs no change: the alias pages keep the `tenants-my-*` / `tenants-user-lookup` testids, so the existing hosted `/tenants/my` and `/tenants/users` smoke expectations already match the alias strategy (file is unchanged in `ba14356..HEAD`). [corrected 2026-06-28 per review]
  - [x] Run `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`.
  - [x] Run the UI test executable fallback if `dotnet test` hits the known .NET 10 VSTest/MTP incompatibility. Do not run solution-level `dotnet test`.

## Dev Notes

### Current State To Modify

| File | Current state | This story changes | Must preserve |
| --- | --- | --- | --- |
| `src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs` | Registers `All tenants`, `My tenants`, `User lookup`, and `Global Administrators` under bounded context `tenants`; manifest has localized Tenants category and BuildingPeople icon. | Register exactly one Tenants nav entry to `/tenants`. | Manifest registration, localization resource marker, icon, bounded-context grouping, global-admin policy constant if still used by page logic/tests. |
| `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` | Owns `/` and `/tenants`; composes `FcAggregateListPage<TenantListRow>`; loads tenant list; owns search/status/sort/cursor/return context; hosts create flow and `TenantDataGrid`. | Add page-local tabs and host/reuse tenant list, my-tenants/self-audit mode, and user lookup. | Server-side search, cursor paging, ETag reuse, list states, create command flow, stable selectors, support-safe copy, audit/detail links, focus restoration. |
| `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor` | Route `/tenants/my`; renders self-audit memberships with `MyTenantsDataGrid`, `MyTenantsState`, cursor paging, refresh, and back link. | Convert to alias/redirect or extract reusable tab content. | `GetMyTenantsAsync` semantics, stale/degraded/invalid/unauthorized states, support-safe tenant id copy, audit entry point source kind `my-tenants`. |
| `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor` | Route `/tenants/users`; lookup form, query prefill, canonical URL update, sort, cursor paging, status focus, and `MyTenantsDataGrid` with `ResourcePrefix="Tenants.UserLookup"`. | Convert to Users tab content or route alias. | Literal target user id, `GetUserTenantsAsync`, honest empty/unauthorized/unavailable/degraded states, query/cursor behavior, no browser HTTP/token storage. |
| `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor` | Shared membership grid for self-audit and user lookup via resource/selector prefixes, audit entry points, pinned role/status/freshness columns. | Reuse directly. | ResourcePrefix/SelectorPrefix split, TargetUserId, SourceKind, ReturnUrl, `DataGridColumnPin.Start`, no raw table markup. |
| `src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs` | Builds `/tenants` return/detail/audit URLs with search/status/sort/desc/cursor/selected/anchor. | Extend to carry active tab and mode where needed. | Safe query encoding, tenant row anchor, detail and audit return context. |
| `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` | Provides `ListTenantsAsync`, `GetMyTenantsAsync`, and `GetUserTenantsAsync`; user membership calls existing `/api/users/{userId}/tenants`. | Freshness resolution rewritten (bundled from `cc-2026-06-25`, reconciled here 2026-06-28): `ResolveFreshness` keys off the new `X-Hexalith-Is-Stale` header (absent → `Unknown`), Degraded precedes Stale across all surfaces, and 304/not-modified freshness resolves via `ResolveNotModifiedFreshness`. No new query contract or browser-side API call. | REST query path, server-side BFF, Memories search-as-index-only for tenant list, freshness resolution, support-safe error mapping. |
| `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs` | Currently asserts four Tenants nav entries and old hrefs. | Assert one entry and preserved manifest. | Icon validation, resource parity, BFF/global-admin authorization tests. |
| `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs` | Locks `/tenants/my` standalone page and asserts workspace toolbar does not duplicate shell nav links. | Update to tab/mode/alias behavior. | Existing self-audit state, cursor, support-safety, no mutation-control tests. |
| `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs` | Locks `/tenants/users` standalone page and canonicalization. | Update to Users tab/alias behavior. | Lookup target, literal ids, distinct safe states, paging/sort/refresh, support-safety tests. |
| `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` | Contains a static route/nav assertion that `/tenants/my` and `/tenants/users` are registered shell nav entries and `/global-administrators` is registered there too. | Rewrite this assertion to the approved 2026-06-27 IA. | Global administrator page route and fixed aggregate behavior. |
| `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` | Hosted smoke tests hit `/tenants`, `/tenants/my`, `/tenants/users`, `/global-administrators`. | Update expected alias/redirect/render behavior. | Fail-closed no-auth posture, no sample data/token leakage. |

### Scope Boundaries

- This is a Tenants UI IA correction. Do not add a new Tenants backend query, all-users membership DTO, all-users endpoint, server projection change, EventStore generic query route, or Memories ingestion change.
- The Users tab is a lookup surface. It can be titled `Users` at the tab level only if the visible copy and empty state make lookup semantics explicit.
- Do not fake all-users membership by paging tenants client-side, aggregating member tables, or walking all tenant pages. That would break authorization, cursor semantics, freshness honesty, and performance.
- Do not move generic tab/page-shell capability into Tenants. `FcPageToolbar` already supports tabs through `FcPageToolbarTab`; if a needed shell behavior is missing, record a FrontComposer handoff instead of building generic shell infrastructure here.
- FrontComposer rail direct-click behavior is not in Tenants scope. Current shell code renders a context tile plus a flyout for registered entries. With one entry, a one-item flyout is acceptable unless a separate FrontComposer owner-approved change is explicitly requested.

### Previous Story Intelligence

- `cc-2026-06-21-frontcomposer-aggregate-list-detail-extraction` introduced and rebased Tenants onto `FcAggregateListPage<TItem>` / `FcAggregateDetailPage<TItem>`. Preserve those wrappers rather than recreating page chrome.
- The 2026-06-21 Memories-backed tenant search story made tenant list search server-side and index-only. Do not reintroduce client-side search over the loaded page.
- The 2026-06-25 ergonomics pass deliberately removed duplicate My Tenants/User Lookup toolbar links because they were shell nav entries. This story supersedes that premise: those surfaces move inside the page, so tests/comments from that pass must be updated deliberately.
- The 2026-06-25 freshness adoption story is in review and migrated UI freshness to `ReadModelFreshnessState`. Do not reintroduce `TenantFreshnessState`; keep `Refreshing` as a transient badge flag if touched.

### Git Intelligence Summary

- Current `HEAD` is `60d99ea feat(tenants): implement tabbed workspace navigation for Tenants module and update sprint status`, but `git show --name-only -1` proves that commit created this story file, touched sprint-story metadata, and updated a submodule pointer; it did not change the production Tenants UI files. Treat the checked-out source as pre-implementation: `TenantsFrontComposerRegistration` still registers four nav entries and `TenantsWorkspace.razor` still has no tabs.
- Recent UI work in the 2026-06-21 and 2026-06-25 stories established the patterns to preserve: `FcAggregateListPage<TItem>`/`FcAggregateDetailPage<TItem>` own reusable page chrome, tenant search remains server-side and Memories index-only, and UI freshness uses EventStore `ReadModelFreshnessState`.
- The 2026-06-25 ergonomics comments in tests now encode a stale assumption that My Tenants/User Lookup live in the shell rail. Update those comments/tests deliberately when moving the surfaces into page-local tabs; do not delete them without replacing the behavioral assertion.

### Architecture And UX Guardrails

- Compose UI through FrontComposer and Blazor Fluent UI V5. Use Fluent/FrontComposer controls before raw CSS/HTML. Raw semantic tags remain allowed only for documented landmark/list/link fallbacks covered by `DomainUiFluentConformanceTests`.
- Keep page-like surfaces with multiple titled sibling content regions grouped with `FluentAccordion`. Tabs choose the surface; accordions can still group sections inside a tab.
- Tenant list safety columns and membership role/status/freshness context must remain visible or fail closed with visible reasons. Do not hide stale/pending markers behind tabs, toolbar overflow, sorting, paging, or responsive layout.
- Tenants-owned copy remains in `TenantsResources.resx` and `TenantsResources.fr.resx` as whole strings. Do not move domain labels into FrontComposer resources.
- The browser never calls `/api/tenants` or `/api/users` directly and never stores tokens. Components continue to depend on `ITenantQueryGateway`.
- `TenantId` and `UserId` are meaningful caller-supplied strings. Preserve exact casing and encoding; do not validate them as GUID/ULID.

### Latest Technical Information

- The repo pins `Microsoft.FluentUI.AspNetCore.Components` to `5.0.0-rc.3-26138.1` in `Directory.Packages.props`; compile-tested local API is authoritative. Do not bump Fluent, Aspire, Dapr, xUnit, or package versions as part of this story.
- FrontComposer source currently provides `FcPageToolbar` with `Tabs`, `ActiveTabId`, and `ActiveTabIdChanged`, rendering `FluentTabs` with `TabsAppearance.Subtle`. This is the preferred tab primitive for page-local tabs in Tenants.
- Microsoft's Blazor routing documentation supports binding query values with `[SupplyParameterFromQuery]`; this matches the existing Tenants pattern for `search`, `status`, `cursor`, `userId`, and return context.
- Fluent UI Blazor tab documentation and the local FrontComposer wrapper tests show `ActiveTabId`/`ActiveTabIdChanged` as the tab-selection mechanism. Use bUnit tests to verify exact behavior against the pinned package.
- Version caveat: the Fluent UI Blazor MCP documentation reports `5.0.0.26139`, while this repo pins `5.0.0-rc.3-26138.1`; the public Tabs demo page currently reports a different site version. Use those docs for conceptual confirmation only. The local `FcPageToolbar` source and this repository's compile/test results are the exact-version authority.

External references checked during story creation:
- Fluent UI Blazor Tabs: https://fluentui-blazor.azurewebsites.net/Tabs
- ASP.NET Core Blazor routing and query strings: https://learn.microsoft.com/aspnet/core/blazor/fundamentals/routing

### Testing Standards

- Use xUnit v3, Shouldly, NSubstitute, and bUnit. Test files remain plural `{Class}Tests.cs`.
- Run test projects individually. Use `.slnx` for restore/build only; do not run solution-level `dotnet test`.
- Expected focused lane: build UI tests, then run the UI test executable fallback if `dotnet test` hits the known .NET 10 VSTest/MTP incompatibility.
- Keep conformance tests green. Do not loosen `DomainUiFluentConformanceTests`, CSS exception rules, raw interactive control guards, or resource parity guards to make this story pass.

### References

- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-27.md`]
- [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- [Source: `_bmad-output/project-context.md`]
- [Source: `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`]
- [Source: `references/Hexalith.AI.Tools/hexalith-ux-instructions.md`]
- [Source: `_bmad-output/planning-artifacts/architecture.md`]
- [Source: `_bmad-output/planning-artifacts/epics.md`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md`]
- [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`]
- [Source: `_bmad-output/implementation-artifacts/cc-2026-06-21-frontcomposer-aggregate-list-detail-extraction.md`]
- [Source: `_bmad-output/implementation-artifacts/cc-2026-06-25-tenant-read-model-freshness-adoption.md`]
- [Source: `src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`]
- [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`]
- [Source: `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor`]
- [Source: `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor`]
- [Source: `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor`]
- [Source: `src/Hexalith.Tenants.UI/Components/Users/MyTenantsState.razor`]
- [Source: `src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs`]
- [Source: `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`]
- [Source: `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor`]
- [Source: `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor.cs`]
- [Source: `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbarTab.cs`]
- [Source: `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor`]
- [Source: `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`]
- [Source: `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`]
- [Source: `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs`]
- [Source: `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs`]
- [Source: `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs`]
- [Source: `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Implementation Plan

- Collapsed Tenants FrontComposer navigation to one `/tenants` module entry while preserving the Tenants manifest icon/category behavior.
- Kept the existing `FcAggregateListPage<TenantListRow>` list chrome and used direct Fluent v5 tabs in the toolbar because `FcPageToolbar` always renders its own fixed-selector search input, which would break the stable `tenants-list-*` selector/search contract and add an irrelevant search box on the Users tab.
- Extracted Tenants-owned `MyTenantsPanel` and `UserMembershipLookupPanel` components so `/tenants` tabs and old `/tenants/my` / `/tenants/users` routes reuse the same gateway-backed behavior.
- Extended return URL generation for tenant list, self-audit, and user lookup contexts so audit/detail navigation preserves tab/scope/user/sort/cursor context where applicable.

### Debug Log References

- Red-phase focused test confirmed old four-entry nav registration failed the new one-entry assertion.
- Focused tab tests initially failed because `/tenants` did not render `tenants-workspace-tabs`, `scope=mine`, or `tab=users` surfaces.
- Validation:
  - `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~TenantsUiCompositionTests.FrontComposer_registration_exposes_tenants_nav_entries_and_minimal_manifest|FullyQualifiedName~TenantsWorkspaceTests|FullyQualifiedName~MyTenantsSurfaceTests|FullyQualifiedName~UserMembershipLookupSurfaceTests|FullyQualifiedName~GlobalAdministratorsPageTests.Routes_stay_reachable_while_tenants_nav_collapses_to_one_module_entry"` passed 32/32.
  - `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
  - `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore` passed 765/765.
  - `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Release --no-restore` passed 106/106.
  - `dotnet test tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj -c Release --no-restore` passed 48/48.
  - `dotnet test tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj -c Release --no-restore` passed 181/181.
  - `dotnet test samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj -c Release --no-restore` passed 39/39.
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release --no-build --no-restore` passed 736/736 after adding the pre-cancelled startup token guard.
  - `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release --no-restore` passed 224/225 with 1 expected performance skip.

### Completion Notes List

- Implemented the approved single Tenants module entry and removed `/tenants/my`, `/tenants/users`, and `/global-administrators` from the Tenants shell nav registration.
- Added `/tenants` page-local Fluent tabs with query-bound `tab=tenants|users`; unknown tabs normalize to `tenants`.
- Added a Tenants-tab `scope=mine` self-audit view backed by `GetMyTenantsAsync`; it does not client-filter a tenant-list page.
- Added a Users tab backed by `GetUserTenantsAsync` and the existing `/api/users/{userId}/tenants` BFF gateway path; visible copy remains lookup/search scoped and does not claim an all-users inventory.
- Kept `/tenants/my`, `/tenants/users`, `/global-administrators`, tenant detail, and tenant audit routes implemented and covered.
- Updated EN/FR resources, UI governance tests, route/static IA tests, and `tests/test-summary.md`.
- Fixed two deterministic regression blockers found during full validation: the solution-structure test now allows the already-root-declared `references/Hexalith.Memories/` projects, and `TenantBootstrapHostedService.StartAsync` skips registration when the startup token is already cancelled.

### File List

- `_bmad-output/implementation-artifacts/cc-2026-06-27-tenants-module-tabbed-workspace.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor.css` (deleted)
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor.css` (deleted)
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsPanel.razor`
- `src/Hexalith.Tenants.UI/Components/Users/MyTenantsPanel.razor.css`
- `src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor`
- `src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor.css`
- `src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`
- `src/Hexalith.Tenants.UI/Program.cs`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs`
- `src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/SolutionStructureTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceEntryPointTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`

#### Bundled `cc-2026-06-25` freshness files (reconciled into this File List 2026-06-28 per review D2)

These `X-Hexalith-Is-Stale` server-header + gateway freshness-resolution files landed in the `ba14356..HEAD` range (commit `ad7312b` + parts of `675dced`). They implement the `cc-2026-06-25-tenant-read-model-freshness-adoption` story and were originally omitted from this File List:

- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs`
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryResult.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Bootstrap/TenantBootstrapHostedServiceTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Queries/TenantQueryFreshnessTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsQueryApiClientTests.cs`

Submodule pointer (review D3): `references/Hexalith.PolymorphicSerializations` bumped `db291e8 → 3f7ca70` (upstream cosmetic "reorder using directives"). Reachable on `origin/main` (not clone-breaking); retained intentionally.

### Change Log

- 2026-06-27T16:42:50+02:00 - Implemented Tenants module tabbed workspace, single shell navigation entry, reusable self-audit/user lookup panels, resources/docs/tests, and deterministic validation fixes.

## Review Findings

_Adversarial code review 2026-06-27 (Blind Hunter + Edge Case Hunter + Acceptance Auditor over the uncommitted working tree). All 14 ACs verified functionally met; the items below are correctness/robustness/process findings. 1 decision-needed, 7 patch, 1 defer, 3 dismissed (incl. 1 false positive)._

### Decision needed

- [ ] [Review][Decision] Out-of-scope server projection change bundled into this UI-IA diff — `TenantProjectionHandler.LatestProjectedAt(...)` `ProjectedAt` monotonicity (`src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`) + new server test `ProjectAsync_RetryAndMergeDoNotMoveProjectedAtBackwardAsync` belong to the `cc-2026-06-25` freshness-adoption story, not the tabbed-workspace File List. The code itself is correct (verified). Decision: keep bundled in this commit, or split/re-attribute to the freshness story before merge.

### Patch

- [x] [Review][Patch] Off-Dispatcher `NavigateTo` + focus JS-interop after `ConfigureAwait(false)` (HIGH) — matches the documented repo circuit-crash pattern; auto-fires on `/tenants?tab=users&userId=X` deep-link and every submit/refresh/page action; bUnit can't catch it (substitute resumes inline on the dispatcher). Fix: `await InvokeAsync(() => Navigation.NavigateTo(...))` and marshal the `OnAfterRenderAsync` focus sequence onto the dispatcher, keeping `ConfigureAwait(false)` on the gateway await. [src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor:384-389, :286-292]
- [x] [Review][Patch] Tab switch drops list search/status/sort/cursor from the URL (MED) — `OnActiveTabChanged` navigates to hard-coded `/tenants?tab=users` / `/tenants`, so URL↔in-memory state diverge; F5 or a shared URL re-inits empty and shows a different list. `OnTenantScopeChanged` already preserves via `CurrentNavigationContext().ToReturnUrl()`. Fix: tenants branch → `CurrentNavigationContext().ToReturnUrl()`. [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:472, :476]
- [x] [Review][Patch] Deep-linked / audit-return cursor strands the user with "Previous" disabled (MED) — `_currentCursor = InitialCursor` but `_cursorHistory` is never seeded; Previous is `Disabled` when the in-memory stack is empty. Newly reachable for My Tenants (panel now takes `InitialCursor` and emits `&cursor=` in `BuildReturnUrl`). Fix: when `InitialCursor` is non-empty, seed history with `null` so Previous returns to the first page; apply to both panels. [src/Hexalith.Tenants.UI/Components/Users/MyTenantsPanel.razor:112,:73 ; UserMembershipLookupPanel.razor:272,:151]
- [x] [Review][Patch] Tenant `scope` state inconsistent across tab switches (LOW) — `RestoreContextFromQuery` sets `_tenantScope` from `QueryScope` regardless of tab, so a `tab=users&scope=mine` deep link + click on the Tenants tab lands on "My tenants" instead of the default list (and no list load); conversely the Users branch force-resets `_tenantScope=AllTenantsScope`, dropping an in-UI `scope=mine` on a Tenants→Users→Tenants round-trip. Fix: treat scope as tenants-tab-only (ignore `QueryScope` when restoring `tab=users`; don't clobber the remembered scope when leaving to Users). [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:455-456, :466-481]
- [x] [Review][Patch] No bUnit a11y coverage for the new tabs + bare-selector tabpanel nuance (LOW) — `<FluentTab Id Header />` carry no child content, so the tab→panel ARIA association points at empty tabpanels while real content lives in sibling `FcAggregateListPage` slots; AC12/AC13 keyboard/active-tab/focus guarantees ride entirely on the Fluent primitive with no Tenants-owned assertion. Fix: add a focused test for active-tab (`aria-selected`) + keyboard switch; the empty-tabpanel structure is an `FcAggregateListPage`-slot architectural nuance — note as a FrontComposer/UX follow-up, not a blocker. [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:22-30]
- [x] [Review][Patch] Test stub localizer weakened to swallow missing resource keys (LOW) — `StubTenantsLocalizer` changed from `Values[name]` (threw on undefined key) to a `TryGetValue ... : name` fallback, removing the implicit missing-key guard; render tests now pass even if markup references an undefined `.resx` key. (The specific key it would have masked, `Tenants.MyTenants.AuditAccessibleLabel`, is verified present, so no live leak today.) Fix: keep a strict stub or add a dedicated resx-existence test. [tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs StubTenantsLocalizer]
- [x] [Review][Patch] Task-7 record claims `TenantsUiRouteSmokeTests` was updated, but it is unchanged (LOW) — AC7 is actually satisfied (alias pages preserve `tenants-my-*` / `tenants-user-lookup` testids and the `/tenants/users` redirect), so existing smoke coverage still holds; only the Dev Agent Record checkbox is inaccurate. Fix: correct the Task-7 note (or add the claimed smoke assertions). [this story file, Task 7]

### Deferred

- [x] [Review][Defer] Global Administrators / Audit reachable only by direct URL after nav de-listing — adding a module-internal/contextual entry point is an explicitly-deferred future product/IA decision (AC9 approved the de-listing; routes/pages/policy preserved). [src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs] — deferred, approved-scope follow-up

### Dismissed (noise / false positive)

- `Tenants.MyTenants.AuditAccessibleLabel` "missing from .resx" — FALSE POSITIVE: present in both EN and FR (`TenantsResources.resx:3165`, sibling `UserLookup` at :3168). Blind Hunter was resx-blind and self-flagged it for verification.
- Shared `sort` query param across two surfaces — crafted-URL only; an unknown sort token falls through the list's sort switch (ignored) and self-heals on the next interaction; normal navigation drops `sort`.
- `TenantBootstrapHostedService` pre-cancelled-token guard + `SolutionStructureTests` Memories-root allowance — both declared in the File List, individually correct and justified deterministic-validation fixes; scope-creep process note only, not defects.

### Review Findings — Group 1 re-review (2026-06-27, chunked, post-commit)

_Chunked adversarial code review of the committed work `ba14356..HEAD` (Blind Hunter + Edge Case Hunter + Acceptance Auditor). This pass covers **Group 1 = UI workspace & panels only** (pages, panels, nav registration, Program.cs, TenantListNavigationContext, resources). Groups 2 (server freshness header + gateway), 3 (tests), and 4 (docs) are pending separate follow-up runs. Result: 1 decision-needed, 5 patch, 2 defer, 3 dismissed. All 14 ACs verified met in structure within Group 1; items below are correctness/robustness findings._

#### Decision needed

- [x] [Review][Decision→Deferred] Create-tenant freshness gate narrowed `Current or Unknown` → `Current` (MED) — `IsFresh="@(_snapshot.Freshness is ReadModelFreshnessState.Current)"` now treats `Unknown` freshness as not-fresh, so the create flow is gated off whenever the backend omits/cannot classify the staleness header (a plausible empty-list / first-tenant bootstrap or degraded state). This is a bundled behavior change overlapping the `cc-2026-06-25` freshness-adoption story, not the tabbed-workspace scope. Decision: (a) intentional fail-closed tightening — keep, and confirm the empty/bootstrap path resolves to `Current` not `Unknown` so first-tenant creation isn't blocked; or (b) accidental regression — restore `Current or Unknown`. [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:117] — **DEFERRED 2026-06-27** (reason: check in next review — revisit alongside the Group 2 server-freshness header review).

#### Patch

- [x] [Review][Patch] Off-Dispatcher `NavigateTo` + focus JS-interop after `ConfigureAwait(false)` → Blazor circuit crash (HIGH) — in `RunLookupAsync` the post-await continuation runs on a thread-pool thread and calls `Navigation.NavigateTo(...)` off the renderer Dispatcher (matches the documented repo circuit-crash class; `TenantsWorkspace.LoadAsync` already guards with `InvokeAsync`). Auto-fires on the `/tenants?tab=users&userId=X` deep link (first render via `OnInitializedAsync`) and every submit/refresh/page action; `OnAfterRenderAsync` focus chain has the same off-dispatcher hazard. bUnit can't catch it. Fix: marshal `NavigateTo` + the focus sequence onto the dispatcher (`await InvokeAsync(...)`), keeping `ConfigureAwait(false)` only on the gateway await. [src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor:389, :277, :286-292]
- [x] [Review][Patch] Deep-linked / audit-return cursor strands the user with "Previous" disabled (MED) — both panels set `_currentCursor = InitialCursor` but never seed `_cursorHistory`; "Previous" is `Disabled` when the stack is empty, so returning from audit (or any deep link) at a non-first cursor leaves page 1 unreachable. Newly reachable for My Tenants because `MyTenantsPanel.BuildReturnUrl()` now emits `&cursor=`. Fix: when `InitialCursor` is non-empty, seed history with `null`. Apply to both panels. [src/Hexalith.Tenants.UI/Components/Users/MyTenantsPanel.razor:112,:73 ; UserMembershipLookupPanel.razor:272,:151]
- [x] [Review][Patch] Tab switch to Tenants drops list search/status/sort/cursor from the URL (MED) — `OnActiveTabChanged` navigates to hard-coded `/tenants` (or `?tab=tenants&scope=mine`), so URL↔in-memory state diverge; F5 / shared URL re-inits a different list. `OnTenantScopeChanged` already preserves via `CurrentNavigationContext().ToReturnUrl()`. Fix: tenants-list branch → `CurrentNavigationContext().ToReturnUrl()`. [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:476]
- [x] [Review][Patch] Foreign Users-surface `cursor`/`sort` mis-assigned to the tenant list on refresh/deep-link (MED) — `RestoreContextFromQuery` unconditionally reads `sort`/`cursor` into the tenant-list fields even when `tab=users`; clicking the Tenants tab then triggers `LoadAsync` with a user-lookup cursor (different cursor space) → bogus/empty/degraded tenant load. Fix: adopt `sort`/`cursor` into tenant-list state only when the active surface is the tenant list (`tab=tenants` & `scope=all`); the My/Users panels already receive cursor/sort via their own `Initial*` params. [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:453-464, :477]
- [x] [Review][Patch] "My tenants" scope clobbered on a Tenants→Users→Tenants round-trip; scope applied inconsistently across tabs (LOW) — `OnActiveTabChanged` force-resets `_tenantScope = AllTenantsScope` when leaving to Users, so a remembered `scope=mine` is silently lost on return; conversely a `tab=users&scope=mine` deep link then clicking Tenants lands on My tenants. Fix: treat scope as tenants-tab-only — don't clobber the remembered scope when switching to Users, and ignore `QueryScope` when restoring `tab=users`. (Minor UX judgment on whether scope should persist; "persist" matches the deep-link path.) [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:471, :455-456]

#### Deferred

- [x] [Review][Defer] Global Administrators / Audit reachable only by direct URL after nav de-listing; `GlobalAdministratorPolicy` now registered but unconsumed (no `RequiredPolicy:`/`[Authorize]` references it; the GA page authorizes via `BffComposition` reflection) — AC9 approved the de-listing and routes/pages/policy are preserved; adding a module-internal/contextual entry point is an explicitly-deferred product/IA decision (policy retention is intentional pending it). [src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs ; src/Hexalith.Tenants.UI/Program.cs:33] — deferred, approved-scope follow-up
- [x] [Review][Defer] New `FluentTabs` render empty tabpanels (active content lives in sibling `FcAggregateListPage` slots), so the tab→tabpanel ARIA association points at empty regions (LOW) — `aria-selected` is correct and tabs are keyboard reachable, but the empty-tabpanel structure is an `FcAggregateListPage`-slot architectural nuance best owned upstream. [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:28-30] — deferred, FrontComposer/UX follow-up

#### Dismissed (noise / false positive)

- New nav `TitleKey "Tenants.Navigation.Tenants"` "missing from .resx" — FALSE POSITIVE: present in both EN and FR (`TenantsResources.resx:2409`, `.fr.resx:2409`). Blind Hunter was resx-blind.
- "Tab return only reloads when `_snapshot.Kind is Loading` → stale/stuck list" — intentional caching: the My Tenants / Users surfaces never disturb the tenant-list `_snapshot`, the `is Loading` gate exists to lazily load when the user starts on a non-list tab, and the toolbar Refresh button (shown for the list view) covers the Error case. Not a defect.
- Orphaned old nav resource strings (`Tenants.Navigation.AllTenants`/`.GlobalAdministrators`, `Tenants.MyTenants.Link`, `Tenants.UserLookup.Link`) — dead strings only after the nav collapse; EN/FR parity intact, no functional impact. Optional cleanup, non-blocking.

#### Resolution (2026-06-27)

All 5 Group 1 `patch` findings were applied to the working tree (the off-Dispatcher fix marshals `NavigateTo`/focus through `InvokeAsync`; both panels now seed cursor history; tab-switch preserves list state via `ToReturnUrl()`; `RestoreContextFromQuery` adopts `sort`/`cursor` into the tenant list only when it is the active surface; the Users-switch no longer clobbers the remembered scope). D1 deferred (reason: check in next review). Validation: `dotnet build src/Hexalith.Tenants.UI` Release `-m:1` = 0 warnings / 0 errors; UI test executable = **776/776** pass. Patches are **UNCOMMITTED**. **Review is chunked — only Group 1 (UI workspace & panels) is complete; Groups 2 (server freshness header + gateway), 3 (tests), 4 (docs) remain, so the story stays in `review`.**

### Review Findings — Full review 2026-06-28 (Groups 2–4 + Group 1 re-verify)

_Full adversarial code review of `ba14356..HEAD` (Blind Hunter + Edge Case Hunter + Acceptance Auditor), completing Groups 2 (server freshness header + gateway), 3 (tests), and 4 (docs) that the 2026-06-27 chunked pass left open, and re-verifying the committed Group 1 patches. All 14 ACs verified met (AC1–AC11 + AC14 positively; AC8/AC12/AC13 met with the LOW caveats below). Group 1 patches (off-Dispatcher `NavigateTo` via `InvokeAsync`; cursor-history seeding) confirmed correctly landed in `f30d874`. Result: 4 decision-needed, 4 patch, 1 defer, 3 dismissed (all 3 Blind-Hunter items — false positives verified against the code)._

#### Decision needed

- [x] [Review][Decision] First-tenant bootstrap blocked — create gate narrowed to `Current`-only rejects `Unknown` freshness (HIGH) — Resolves the 2026-06-27 deferred D1: it IS a real regression, not a false alarm. `IsFresh="@(_snapshot.Freshness is ReadModelFreshnessState.Current)"` (was `Current or Unknown`) disables the create submit (`CreateTenantFlow` → `UnavailableReason` → `IsSubmitDisabled` → `disabled` button) whenever list freshness is `Unknown`. A cold/empty tenant index has no persisted `ProjectedAt`, so the server emits NO `X-Hexalith-Is-Stale` header (integration test `ListTenants_omits_freshness_header_when_query_metadata_is_unknown`, `new TenantIndexReadModel()`); the gateway's rewritten `ResolveFreshness` maps absent `IsStale`→`Unknown`, and `TenantListSnapshot.Empty` carries it. On a fresh deployment with zero tenants the first-tenant create is unreachable from the UI, and Refresh re-queries the same cold projection → still `Unknown` → no recovery. Test-LOCKED by `Workspace_blocks_create_flow_when_list_freshness_is_unknown`. Triple-confirmed (Auditor + Edge Case Hunter + reviewer). The 2026-06-27 deferred note said "revisit alongside the Group 2 server-freshness review: confirm empty/bootstrap resolves to `Current` (keep) or restore `Current or Unknown`" — it resolves to `Unknown`, so the recommended option is (a). Decision: (a) restore `Current or Unknown` + update the locking test; (b) special-case the authorized-empty/bootstrap path to `Current`; or (c) keep fail-closed but guarantee the empty index is stamped `Current` before first use. [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:117]
- [x] [Review][Decision] Freshness story bundled into this UI-IA diff; File List omits ~11 changed files (MED) — The entire `X-Hexalith-Is-Stale` server header + the large `TenantQueryGateway` freshness rewrite (`Resolve*ForFreshness`/`ResolveNotModifiedFreshness`/`AggregateRowFreshness`, Degraded-before-Stale reordering across all surfaces) + the `TenantProjectionHandler.LatestProjectedAt` `ProjectedAt` monotonicity guard belong to `cc-2026-06-25-tenant-read-model-freshness-adoption`, not a "Tenants UI IA correction." The code is correct, but: (1) the File List omits `TenantQueryGateway.cs`, `TenantsQueryApiClient.cs`, `TenantsQueryController.cs`, `TenantQueryResult.cs`, `TenantProjectionHandler.cs`, and their 6 test files; and (2) the Dev Notes "Current State To Modify" table asserts the gateway needs "No backend/API change expected," contradicted by the rewrite. Decision: re-attribute/split to the freshness story, or reconcile this story's File List + Dev Notes to own them. [src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs ; src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs]
- [x] [Review][Decision] Unexplained `Hexalith.PolymorphicSerializations` submodule pointer bump (LOW) — Commit `60d99ea` bumps the submodule `db291e8 → 3f7ca70` (upstream `3f7ca70` is a cosmetic "reorder using directives"). Not in the File List/Completion Notes; out of scope for a UI-IA story under the repo's strict submodule policy. Verified NOT clone-breaking (`3f7ca70` is reachable on `origin/main`, `db291e8` is an ancestor). Decision: document the intent or revert the pointer from this story. [references/Hexalith.PolymorphicSerializations]
- [x] [Review][Decision] `TenantBootstrapHostedService` 409-only narrowing drops documented Dapr-remap tolerance (LOW) — The benign already-bootstrapped marker is now accepted only on a literal `409 Conflict` (`httpResponse.StatusCode == HttpStatusCode.Conflict && errorBody.Contains("GlobalAdminAlreadyBootstrappedRejection")`), where the prior code deliberately accepted the marker on ANY non-success status, with a comment explaining it tolerates Dapr sidecar status remapping. The new code is more precise but removes that documented resilience: if a sidecar relays/remaps the 409, restarts will log the benign already-done outcome as unexpected. Not in the Completion Notes. Decision: confirm the tightening is intended, or restore the any-status tolerance. [src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs:97-107]

#### Resolution of decision-needed (2026-06-28, Administrator)

- D1 → **PATCH**: restore the `IsFresh` gate to `Current or Unknown` (fail-open on `Unknown`) and update the `Workspace_blocks_create_flow_when_list_freshness_is_unknown` test to assert the bootstrap/empty path stays creatable.
- D2 → **PATCH**: reconcile this story's File List (add the ~11 bundled freshness files) and fix the Dev Notes "no backend/API change expected" line to reflect the gateway/server changes.
- D3 → **PATCH**: keep the `Hexalith.PolymorphicSerializations` submodule bump; add a one-line note documenting the intent.
- D4 → **DISMISSED**: the `409`-only already-bootstrapped narrowing is confirmed intended; no code change.

#### Patch

- [x] [Review][Patch] [D1] Restore create-gate to `Current or Unknown` + update locking test — fail-open on `Unknown` so first-tenant bootstrap is creatable; flip `IsFresh="@(_snapshot.Freshness is ReadModelFreshnessState.Current)"` back to `is Current or Unknown` and update `Workspace_blocks_create_flow_when_list_freshness_is_unknown` to assert Unknown stays creatable (keep a separate Stale-blocks-create assertion). [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:117 ; tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs]
- [x] [Review][Patch] [D2] Reconcile File List + Dev Notes — add the bundled freshness files to the File List and correct the "No backend/API change expected" Dev Notes line. [this story file]
- [x] [Review][Patch] [D3] Document the `Hexalith.PolymorphicSerializations` submodule bump intent. [this story file / File List]
- [x] [Review][Patch] User-lookup target silently dropped on a Users → other-tab → Users round-trip — switching to Users always navigates to hard-coded `/tenants?tab=users` (no `userId`); the `@if (IsUsersTab)` panel is disposed on tab-away and recreated with `InitialUserId="@QueryUserId"` = null → blank form, prior results gone. Asymmetric with the deliberately-preserved `scope=mine`. Fix: carry the remembered `userId` in a workspace field (or include it in the nav URL) so re-entering Users restores the last lookup. [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:474-479]
- [x] [Review][Patch] Extracted panels lack the workspace's request-version/cancellation guard (Clear/overlap races) — `UserMembershipLookupPanel.RunLookupAsync` / `MyTenantsPanel.LoadAsync` call the gateway with `CancellationToken.None` and no `_loadVersion`/Interlocked guard (the workspace's own `LoadAsync` HAS one). So (a) a Clear during an in-flight lookup is undone by the stale completion's `InvokeAsync` continuation (results resurrected + a malformed `userId=` return URL), and (b) overlapping lookups race — last-to-complete wins `_snapshot` while `_targetUserId`/URL say the other user. Fix: add a load-version (or CTS) guard to both panels and bail in the continuation if a newer load started. [src/Hexalith.Tenants.UI/Components/Users/UserMembershipLookupPanel.razor:363-406 ; src/Hexalith.Tenants.UI/Components/Users/MyTenantsPanel.razor:122-135]
- [x] [Review][Patch] MyTenantsPanel scoped `margin:0` reset no-ops + stale `fc-css-exception` comment — the summary was migrated to `<FluentText As="TextTag.Paragraph">`, so the scoped rule `.my-tenants-page p` (compiled to `p[b-xxx]`) can't match the `<p>` that the child `FluentText` renders → the reset silently does nothing, and the exception comment ("no Fluent typography primitive wraps these semantic elements") is now factually false. Fix: remove the dead rule + stale comment, or apply the margin via the `__summary` class / `FluentText` params. [src/Hexalith.Tenants.UI/Components/Users/MyTenantsPanel.razor.css:1-5]
- [x] [Review][Patch] Dev Agent Record Task 7 inaccuracy — Task 7 checks off "Update `TenantsUiRouteSmokeTests` …", but `git diff ba14356..HEAD -- …/TenantsUiRouteSmokeTests.cs` is empty (unchanged). AC7 is still functionally met (alias pages keep `tenants-my-*` / `tenants-user-lookup` testids), so this is a record inaccuracy only (also noted by the 2026-06-27 pass, still uncorrected). Fix: correct the Task-7 note. [this story file, Task 7]

#### Deferred

- [x] [Review][Defer] Page-local tabs a11y: empty tabpanels + no Tenants-owned `aria-selected`/keyboard bUnit assertion [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:22-30] — deferred, FrontComposer/UX follow-up. The new `FluentTabs` carry `Id`/`Header` only; active content renders in sibling `FcAggregateListPage` slots, so the tab→tabpanel ARIA association points at empty regions, and AC12/AC13 keyboard/active-tab guarantees ride entirely on the Fluent primitive with no Tenants-owned assertion. Already recorded in `deferred-work.md` (2026-06-27 Group 1 re-review); this run adds the missing-bUnit-assertion observation to it.

#### Dismissed (false positive — verified against the code)

- Blind Hunter "tenant filters/scope wiped after a Users-tab detour" — FALSE POSITIVE: `RestoreContextFromQuery` runs only in `OnInitializedAsync` (there is no `OnParametersSet` override), so a `NavigateTo` query-string change does not re-read the URL; in-memory `_tenantScope`/`_search`/`_sortColumn`/`_currentCursor` survive the round-trip and the return rebuilds the URL via `CurrentNavigationContext().ToReturnUrl()`. (Only a hard F5 parked on the Users tab drops tenants-tab state from the URL — minor/acceptable.)
- Blind Hunter "stale-on-304 recovery dead in production" — FALSE POSITIVE: `ApplyFreshnessHeaders(metadata)` is called at `TenantsQueryController.cs:370` BEFORE `return StatusCode(304)` at :372, so 304 responses DO carry `X-Hexalith-Is-Stale`; an aging-but-unchanged projection (same ETag) surfaces as Stale on a 304 as intended. (Edge Case Hunter verified the same; reviewer confirmed by reading lines 369–373.)
- Blind Hunter "double tenant-list fetch on a tab/scope switch" — FALSE POSITIVE: `NavigateTo` to the same component does not re-run `OnInitializedAsync`, so the guarded `if (IsTenantListView && _snapshot.Kind is Loading) LoadAsync` in the handler is the only fetch.
