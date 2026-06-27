---
baseline_commit: ba14356a8b2b648eda24a4dd7fbd25d60e0d674d
title: 'Tenants module tabbed workspace and single navigation entry'
type: 'correct-course-ui-ia'
created: '2026-06-27'
status: 'ready-for-dev'
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

Status: ready-for-dev

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

- [ ] Task 1 - Collapse Tenants shell navigation to one entry (AC: 1, 2, 9, 13)
  - [ ] Update `TenantsFrontComposerRegistration.RegisterDomain` to register one `FrontComposerNavEntry` for `/tenants` only.
  - [ ] Preserve `DomainManifest` localization/icon behavior.
  - [ ] Update `TenantsUiCompositionTests.FrontComposer_registration_exposes_tenants_nav_entries_and_minimal_manifest` to assert exactly one Tenants entry.
  - [ ] Update stale tests that currently expect `/tenants/my`, `/tenants/users`, or `/global-administrators` in the Tenants nav registration.

- [ ] Task 2 - Add page-local tabs to `TenantsWorkspace.razor` (AC: 3, 4, 11, 12)
  - [ ] Use `FcPageToolbar` with `FcPageToolbarTab` or direct `FluentTabs` only if `FcPageToolbar` cannot fit the aggregate list page slot.
  - [ ] Add query-bound active tab state such as `tab=tenants|users` and normalize unknown tabs to `tenants`.
  - [ ] Keep the existing `FcAggregateListPage<TenantListRow>` composition and list body for the Tenants tab.
  - [ ] Ensure tab controls have stable selectors, accessible labels, keyboard operation, and localized EN/FR labels.

- [ ] Task 3 - Recompose My Tenants as a Tenants-tab mode or alias (AC: 5, 7, 8)
  - [ ] Reuse `GetMyTenantsAsync`, `UserTenantMembershipSnapshot`, `MyTenantsDataGrid`, and `MyTenantsState`.
  - [ ] Decide the smallest maintainable implementation: inline self-audit mode under the Tenants tab, a view switch inside the tab, or an alias route that selects the tab/mode.
  - [ ] Preserve cursor paging, ETag reuse, stale/degraded/invalid/unauthorized/unavailable states, support-safe copy, and audit entry points.
  - [ ] Keep `/tenants/my` working through redirect/canonicalization or alias rendering, with test coverage.

- [ ] Task 4 - Recompose User Membership Lookup as the Users tab (AC: 6, 7, 8, 10)
  - [ ] Move or host the existing `UserMembershipLookupPage` behavior inside the `/tenants` Users tab without changing the BFF gateway contract.
  - [ ] Preserve literal caller-supplied user id handling; never parse `UserId` as GUID/ULID.
  - [ ] Preserve `userId`, `sort`, and `cursor` query behavior or provide deterministic canonical equivalents under `/tenants?tab=users`.
  - [ ] Keep `/tenants/users?userId=...` working through redirect/canonicalization or alias rendering.
  - [ ] Ensure visible copy says lookup/search and does not claim complete user inventory.

- [ ] Task 5 - Preserve contextual routes and return links (AC: 8, 9)
  - [ ] Update `TenantListNavigationContext` so detail/audit return URLs include active tab and any new scope/mode fields.
  - [ ] Update membership-grid audit return URLs so lookup/self-audit results return to the right tab and mode.
  - [ ] Verify `TenantDetailPage` safe `returnUrl` handling still allows `/tenants...` query routes and rejects unsafe external targets.
  - [ ] Do not remove `GlobalAdministratorsPage`, `TenantAuditPage`, or their route tests unless a separate approved story changes their IA.

- [ ] Task 6 - Resources, docs, and conformance (AC: 11, 12, 14)
  - [ ] Add/update `Tenants.Workspace.Tabs.*` or equivalent keys in `TenantsResources.resx` and `.fr.resx`.
  - [ ] Update `tests/test-summary.md` lines that describe the old primary navigation model.
  - [ ] Keep `DomainUiFluentConformanceTests`, `PageLayoutDeclarationTests`, resource parity tests, and support-safety tests green without raising budgets or allowlists unless the code-review note explains why.

- [ ] Task 7 - Focused tests and verification (AC: all)
  - [ ] Add bUnit coverage for `/tenants` default tab, tab switching callback/query normalization, my-tenants mode, Users lookup tab, route aliases, and old deep-link behavior.
  - [ ] Update `TenantsUiRouteSmokeTests` so hosted `/tenants/my` and `/tenants/users` expectations match the alias or canonical redirect strategy.
  - [ ] Run `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`.
  - [ ] Run the UI test executable fallback if `dotnet test` hits the known .NET 10 VSTest/MTP incompatibility. Do not run solution-level `dotnet test`.

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
| `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` | Provides `ListTenantsAsync`, `GetMyTenantsAsync`, and `GetUserTenantsAsync`; user membership calls existing `/api/users/{userId}/tenants`. | No backend/API change expected. | REST query path, server-side BFF, Memories search-as-index-only for tenant list, freshness resolution, support-safe error mapping. |
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

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List

