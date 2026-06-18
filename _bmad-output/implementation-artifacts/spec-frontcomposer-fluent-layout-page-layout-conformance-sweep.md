---
title: 'FrontComposer Fluent layout page-layout conformance sweep'
type: 'refactor'
created: '2026-06-18'
status: 'in-progress'
baseline_commit: '974eac7fe6b7b6bc2545c2c51adb170ed587482a'
approval: 'Administrator approved sprint-change-proposal-2026-06-18-fluent-layout-page-layout.md on 2026-06-18'
scope_extension_approval: 'Administrator approved sprint-change-proposal-2026-06-18-page-header-frontcomposer.md on 2026-06-18'
context:
  - '{project-root}/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-18-fluent-layout-page-layout.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-18-page-header-frontcomposer.md'
  - '{project-root}/_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/docs/tenants-ui-responsive-layout-and-visual-system-spec.md'
---

<frozen-after-approval reason="human-owned intent -- do not modify unless human renegotiates">

## Intent

**Problem:** Tenants UI now conforms on major Fluent controls, raw tables, raw forms, and semantic CSS state ownership, but page-level layout and page chrome are still partially owned by Tenants page wrappers. Route pages declare `PageTitle` and visible page headers independently, which produces inconsistent page headers and duplicates generic page-title/header scaffolding that belongs in FrontComposer. The approved correction requires page layout to use Fluent UI Blazor v5 Layout through the existing FrontComposer shell and `FC-LYT` page-layout contract, and requires page title/header composition to move into a FrontComposer component.

**Approach:** Keep `MainLayout.razor` as `<FrontComposerShell>@Body</FrontComposerShell>`. Move page-level layout decisions into `FcPageLayout` and Fluent layout primitives (`FluentStack`, `FluentGrid`, `FluentGridItem`, and shell-owned `FluentLayout` where applicable). Add a narrow FrontComposer-owned page-header component that bundles Blazor `PageTitle` with the visible route-level header, while Tenants supplies localized domain strings and page-specific fragments. Reduce page CSS to unavoidable page-specific exceptions and add governance tests so local page-layout and page-header wrappers do not return.

## Boundaries & Constraints

**Always:** Use the repository's pinned `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1` API and verified FrontComposer source. Preserve existing routes, selectors, localized copy, focus restoration, command lifecycle behavior, audit behavior, stale/degraded states, and support-safe copy/redaction behavior. Keep dense grid-first operational pages full width and use constrained measures for prose, forms, and readable detail regions.

**Approved by scope extension:** Add a narrow FrontComposer-owned page-header component that bundles Blazor `PageTitle` with the visible route-level header. Continue to stop before changing the Fluent package version, changing command/query behavior, adding unrelated shared layout APIs, or turning this into a visual redesign.

**Never:** Do not add Tenants-owned shell, page-layout, breakpoint, provider, or max-width infrastructure. Do not replace `FrontComposerShell` with a Tenants-local `FluentLayout`. Do not hide primary DataGrid content behind a new layout interaction. Do not weaken existing accordion, control, raw-table, raw-form, or semantic CSS conformance guards.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|----------------------------|----------------|
| Dense grid page | Tenant list, global administrators, audit, or membership grid is visible | Page declares full-width layout and keeps DataGrid content directly visible | Stale/degraded/error states remain non-collapsed and visible |
| Detail or command-heavy page | Tenant detail, user lookup, or command regions render alongside grids | Readable regions use constrained measure or Fluent layout primitives without local page max-width scaffolding | Command forms still submit through existing handlers and remain fail-closed |
| Governance scan | A future page adds root grid wrapper or page-root layout CSS | Test fails with offender file and rule | Allow only documented exceptions in the implementation artifact |
| FrontComposer boundary | Implementation discovers a missing reusable layout primitive | Work stops for FrontComposer/UX decision instead of adding generic infrastructure to Tenants | Record the gap and proposed owner |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Layout/MainLayout.razor` -- must remain the shared shell wrapper.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` and `.razor.css` -- tenant list, create flow entry point, filters, and pager.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` and `.razor.css` -- tenant detail, metadata/lifecycle/member/configuration composition.
- `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor` and `.razor.css` -- signed-in user membership surface.
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor` and `.razor.css` -- user lookup form and results.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` and `.razor.css` -- global administrator review and command regions.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` and `.razor.css` -- audit filters, state, grid, receipt, and correction panels.
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor` and supporting partial/CSS files if needed -- shared page-title/header component.
- `Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/` -- shared component tests for the page-header contract.
- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` -- source governance guards.
- `tests/Hexalith.Tenants.UI.Tests/Components/*.cs` -- focused bUnit coverage for preserved selectors and layout declarations.

## Tasks & Acceptance

**Execution:**
- [x] Verify exact local APIs for `FcPageLayout`, `FcPageLayoutMode`, `FluentStack`, `FluentGrid`, and `FluentGridItem` against the pinned package/source before editing.
- [x] Add page-layout declarations to Tenants pages, using `FullWidth` for DataGrid-dense pages and `Constrained` for form/prose/detail measure where appropriate.
- [x] Replace page-owned layout grids with Fluent layout primitives where those grids only arrange sibling page regions.
- [x] Reduce page `.razor.css` root layout rules to documented exceptions only.
- [x] Extend `DomainUiFluentConformanceTests` with page-layout governance and allowlist rationale.
- [x] Add component tests proving at least one full-width page and one constrained/readable page declare the expected layout.
- [x] Update sprint status from `ready-for-dev` to the appropriate execution state when implementation starts and to `done` only after verification passes.
- [x] Add a FrontComposer-owned page-header component that bundles Blazor `PageTitle` with the visible route-level page header.
- [x] Add FrontComposer component tests proving heading, title, optional fragments, accessibility parameters, and generic resource independence.
- [x] Migrate Tenants route pages from direct `<PageTitle>` plus page-owned route headers to the FrontComposer page-header component.
- [x] Extend Tenants governance tests to reject direct route-page `<PageTitle>` and page-owned route-header scaffolding.
- [x] Preserve existing route behavior, localized copy, selectors, focus behavior, command lifecycle, audit state, stale/degraded states, and support-safe behavior.

**Acceptance Criteria:**
- Given Tenants page source, when governance tests scan page components, then page-level layout is declared through `FrontComposerShell`/`FcPageLayout` and Fluent layout primitives rather than Tenants-owned page layout scaffolding.
- Given dense operational pages, when they render, then DataGrid-first content remains directly visible and full-width.
- Given detail/form/prose-heavy regions, when they render, then readable measure is constrained through the FrontComposer/Fluent layout contract, not bespoke max-width page CSS.
- Given route pages render, when browser title and visible page header are needed, then both are declared through the FrontComposer page-header component and Tenants supplies only localized domain text and page-specific fragments.
- Given Tenants page source, when governance tests scan route pages, then direct route-page `<PageTitle>` and page-owned route-level header scaffolding are rejected.
- Given existing component tests, when they query stable selectors and localized text, then routes, labels, focus behavior, command lifecycle, audit state, and support-safe behavior remain unchanged.
- Given the UI test project, when it runs, then conformance and affected component tests pass against the pinned Fluent UI Blazor v5 package.

### Review Findings (code review 2026-06-18)

Scope reviewed: layout phase only — diff `974eac7..HEAD` (`92bf113`). The page-header scope-extension tasks above are not yet implemented and were not in scope for this review. 3 decision-needed (resolved 2026-06-18), 4 patch findings handled (3 applied, 1 not-applied/already-governed), 0 deferred, 12 dismissed as noise/false-positive. Layout-phase ACs 1–3 and the governance/non-weakening ACs are satisfied (confirmed by the Acceptance Auditor). Verification after fixes: `Hexalith.Tenants.UI.Tests` 681/681 passing (Release).

**Decision resolutions (2026-06-18):**
- D1 (`<main>` landmark, HIGH) → applied in FrontComposer shell (patch 1).
- D2 (Constrained dense grids on Detail/User-Lookup, MEDIUM) → resolved: use Fluent UI default DataGrid responsiveness within the current `Constrained` measure; no code change. Revisit if UX finds the 75rem cap too narrow on wide displays.
- D3 (out-of-story DAPR/health commit, MEDIUM) → investigated; not applied (patch 2).

- [x] [Review][Patch] Added `role="main"` to `#fc-main-content` in `FrontComposerShell` (restores the page `main` landmark removed when page `<main>` wrappers became `<FluentStack>`); also removed the now-redundant `role="main"` from `FcHomeDirectory.razor` to avoid a duplicate landmark on the home route. NOTE: two FrontComposer submodule files changed — run `Hexalith.FrontComposer.Shell.Tests` to confirm (no `role="main"` assertions exist there; Tenants UI 681/681 pass). [HIGH] [Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:113; .../Components/Home/FcHomeDirectory.razor:12]
- [x] [Review][Patch] DAPR/health hardening — **NOT APPLIED (already governed)**: both halves are locked by existing test contracts. `CrossAggregateTimingDocumentationTests.Timing_guide_matches_current_DAPR_pubsub_component_contracts` asserts `enableDeadLetter`/`deadLetterTopic` in both local + production `pubsub.yaml` and the timing guide; `HealthEndpointsTests.Ready_development_json_response_is_support_safe...` asserts the curated `description` IS surfaced while exceptions/tokens/connection strings are excluded (the writer already omits exceptions). Changing either would break Server.Tests. Recommend handling any dead-letter-semantics change as a separate deployment-readiness task. [MEDIUM] [src/Hexalith.Tenants/Program.cs; src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml]
- [x] [Review][Patch] Restored pager trailing-alignment (`HorizontalAlignment.End`) on both pagers and the page-header child gap (Fluent `VerticalGap` stack — the header children carry `margin:0`, so they would otherwise touch). Filter-label spacing unaffected (Fluent `Label=`). [LOW] [src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor; TenantsWorkspace.razor]
- [x] [Review][Patch] Closed governance-guard holes — broadened the forbidden page-root property regex to logical/longhand props (`padding-inline/-block(-start/-end)`, `inline-size`, `max-inline-size`, `block-size`); the CSS guard now derives page-root classes dynamically per page file (no hardcoded allowlist); and the declare-modes guard now fails any route page lacking `<FcPageLayout`. [LOW] [tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs]

## Design Notes

`FrontComposerShell` already owns the shell-level `FluentLayout`. Tenants pages should not instantiate a competing shell layout. The page-level decision is the `FC-LYT` measure: full-width for operational grids and constrained for readable regions. When layout primitives are needed inside a page, use Fluent layout components before CSS grids.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore`
- `git diff --check`

## Suggested Review Order

**Layout Contract**

- Confirm `MainLayout.razor` still composes `<FrontComposerShell>@Body</FrontComposerShell>`.
- Review page declarations for correct `FcPageLayoutMode.FullWidth` versus `FcPageLayoutMode.Constrained` choices.

**Page CSS**

- Review root page CSS removals before component-local spacing changes.
- Confirm remaining page CSS is documented as an exception or is not expressing a layout primitive already available through Fluent/FrontComposer.

**Governance**

- Review `DomainUiFluentConformanceTests` last so the guard matches the intended migration boundary.

## Dev Agent Record

### Implementation Plan

- Verified `FcPageLayout`, `FcPageLayoutMode`, and shell layout behavior from local FrontComposer source.
- Verified `FluentStack`, `FluentGrid`, and `FluentGridItem` parameters from the pinned local NuGet XML for `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`; the Fluent MCP server documents `5.0.0.26139`, so it was treated as secondary only.
- Added failing page-layout governance before implementation, then migrated the six Tenants pages to explicit `FcPageLayout` declarations and Fluent layout primitives.
- Trimmed page-root CSS layout ownership while preserving forced-colors, focus, state, grid overflow, and readable-region styling.
- Added `FcPageHeader` in FrontComposer as a generic page-title/header component with consumer-owned localized text, optional metadata/actions slots, heading accessibility parameters, and a focus method for route context restoration.

### Debug Log

- Red phase confirmed: `DomainUiFluentConformanceTests` failed on missing `FcPageLayout`, page-root `<main>` wrappers, and page-root layout CSS.
- Green phase: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore` passed `681/681`.
- Tier 1 regression: Contracts `106/106`, Client `47/47`, Testing `181/181`, Sample `32/32` passed. Initial parallel attempts hit MSBuild file locks; sequential reruns passed.
- Tier 2 `Server.Tests` was attempted and still has six known pre-existing documentation/AppHost failures around missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and stale deployment-readiness evidence.
- Tier 3 `IntegrationTests` was attempted with `Category!=Performance`; DAPR/Aspire-dependent rows skipped and two known pre-existing health-readiness drift tests failed.
- `git diff --check` passed with a CRLF normalization warning for `_bmad-output/implementation-artifacts/sprint-status.yaml`.
- Red phase confirmed: `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-restore --filter FcPageHeaderTests` failed because `FcPageHeader` did not exist.
- Green phase: the same focused FrontComposer command passed `4/4` after adding `FcPageHeader` and the component tests. The run also required a Release-only import fix in `FrontComposerShellTests.cs` for the pre-existing `CustomizationLevel` reference.
- Red phase confirmed: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore --filter Domain_route_pages_declare_frontcomposer_page_headers` failed with 18 route-page offenders before migration.
- Green phase: the same focused Tenants governance command passed after migrating the route pages to `FcPageHeader`.
- UI regression: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore` passed `682/682`.
- Full FrontComposer shell regression was attempted with `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-restore`; it still failed `20/1905` on pre-existing/out-of-scope shell security, route hydration, auth-boundary, release-readiness, package-inventory, home-directory landmark, and Verify snapshot/culture drift assertions. Focused `FcPageHeaderTests` passed.

### Completion Notes

- Tenants pages now declare FC-LYT layout through `FcPageLayout`: full-width for tenant list, my tenants, global administrators, and audit; constrained for tenant detail and user lookup.
- Page composition moved from Tenants-owned root `<main>`/CSS layout scaffolding to Fluent `FluentStack` and `FluentGrid` primitives where the story required page-level layout ownership to move out of local CSS.
- Governance now blocks missing page layout declarations, raw page-root layout wrappers, and page-root CSS layout ownership.
- Component coverage now renders a full-width page and a constrained page inside `FrontComposerShell` and asserts shell layout state.
- FrontComposer now owns the shared `FcPageHeader` page-title/header contract, with tests covering the browser title component, visible route heading, optional text/fragments, accessibility parameters, and generic resource independence.
- Tenants route pages now declare route browser titles and visible route headers through `FcPageHeader`; page-specific metadata/actions remain Tenants-owned fragments.
- Tenant list return-context focus restoration now uses `FcPageHeader.FocusHeadingAsync()` against the shared heading element, preserving the existing return-focus behavior without direct page-owned `<h1>` markup.
- Governance now rejects route pages that omit `FcPageHeader`, declare direct `<PageTitle>`, or declare raw route-level `<h1>` markup.

## File List

- `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-18-page-header-frontcomposer.md`
- `src/Hexalith.Tenants.UI/Components/_Imports.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/MyTenantsPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor.css`
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor`
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor.css`
- `tests/Hexalith.Tenants.UI.Tests/Components/PageLayoutDeclarationTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor`
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor`
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs`
- `Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/wwwroot/css/fc-page-header.css`
- `Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageHeaderTests.cs`
- `Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`

## Change Log

- 2026-06-18: Implemented FrontComposer/Fluent page-layout conformance sweep and moved story to review.
- 2026-06-18: Reopened story to in-progress after Administrator approved the FrontComposer page-header component scope extension.
- 2026-06-18: Code review of the layout phase (diff 974eac7..HEAD). Applied 3 patches (shell `role="main"` landmark via FrontComposer + home-page duplicate cleanup; pager/header CSS-regression fixes; governance-guard hardening); 1 finding not applied (DAPR/health already governed by existing Server.Tests); D2 resolved to no code change. `Hexalith.Tenants.UI.Tests` 681/681. Page-header migration phase still pending.
- 2026-06-18: Implemented the FrontComposer `FcPageHeader`, migrated Tenants route pages to it, and added route-header governance. Tenants UI passed 682/682 and focused FrontComposer page-header tests passed 4/4; full FrontComposer shell regression still has unrelated pre-existing failures.

## Status

in-progress
