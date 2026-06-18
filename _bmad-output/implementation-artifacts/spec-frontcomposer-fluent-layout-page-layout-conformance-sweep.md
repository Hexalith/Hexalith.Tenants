---
title: 'FrontComposer Fluent layout page-layout conformance sweep'
type: 'refactor'
created: '2026-06-18'
status: 'review'
baseline_commit: '974eac7fe6b7b6bc2545c2c51adb170ed587482a'
approval: 'Administrator approved sprint-change-proposal-2026-06-18-fluent-layout-page-layout.md on 2026-06-18'
context:
  - '{project-root}/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-18-fluent-layout-page-layout.md'
  - '{project-root}/_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/docs/tenants-ui-responsive-layout-and-visual-system-spec.md'
---

<frozen-after-approval reason="human-owned intent -- do not modify unless human renegotiates">

## Intent

**Problem:** Tenants UI now conforms on major Fluent controls, raw tables, raw forms, and semantic CSS state ownership, but page-level layout is still owned by Tenants page wrappers and page-root CSS grids. The approved correction requires page layout to use Fluent UI Blazor v5 Layout through the existing FrontComposer shell and `FC-LYT` page-layout contract.

**Approach:** Keep `MainLayout.razor` as `<FrontComposerShell>@Body</FrontComposerShell>`. Move page-level layout decisions into `FcPageLayout` and Fluent layout primitives (`FluentStack`, `FluentGrid`, `FluentGridItem`, and shell-owned `FluentLayout` where applicable). Reduce page CSS to unavoidable page-specific exceptions and add governance tests so local page-layout wrappers do not return.

## Boundaries & Constraints

**Always:** Use the repository's pinned `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1` API and verified FrontComposer source. Preserve existing routes, selectors, localized copy, focus restoration, command lifecycle behavior, audit behavior, stale/degraded states, and support-safe copy/redaction behavior. Keep dense grid-first operational pages full width and use constrained measures for prose, forms, and readable detail regions.

**Ask First:** Stop before changing the Fluent package version, editing the FrontComposer submodule, adding shared layout APIs, changing command/query behavior, or turning this into a visual redesign.

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

**Acceptance Criteria:**
- Given Tenants page source, when governance tests scan page components, then page-level layout is declared through `FrontComposerShell`/`FcPageLayout` and Fluent layout primitives rather than Tenants-owned page layout scaffolding.
- Given dense operational pages, when they render, then DataGrid-first content remains directly visible and full-width.
- Given detail/form/prose-heavy regions, when they render, then readable measure is constrained through the FrontComposer/Fluent layout contract, not bespoke max-width page CSS.
- Given existing component tests, when they query stable selectors and localized text, then routes, labels, focus behavior, command lifecycle, audit state, and support-safe behavior remain unchanged.
- Given the UI test project, when it runs, then conformance and affected component tests pass against the pinned Fluent UI Blazor v5 package.

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

### Debug Log

- Red phase confirmed: `DomainUiFluentConformanceTests` failed on missing `FcPageLayout`, page-root `<main>` wrappers, and page-root layout CSS.
- Green phase: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore` passed `681/681`.
- Tier 1 regression: Contracts `106/106`, Client `47/47`, Testing `181/181`, Sample `32/32` passed. Initial parallel attempts hit MSBuild file locks; sequential reruns passed.
- Tier 2 `Server.Tests` was attempted and still has six known pre-existing documentation/AppHost failures around missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and stale deployment-readiness evidence.
- Tier 3 `IntegrationTests` was attempted with `Category!=Performance`; DAPR/Aspire-dependent rows skipped and two known pre-existing health-readiness drift tests failed.
- `git diff --check` passed with a CRLF normalization warning for `_bmad-output/implementation-artifacts/sprint-status.yaml`.

### Completion Notes

- Tenants pages now declare FC-LYT layout through `FcPageLayout`: full-width for tenant list, my tenants, global administrators, and audit; constrained for tenant detail and user lookup.
- Page composition moved from Tenants-owned root `<main>`/CSS layout scaffolding to Fluent `FluentStack` and `FluentGrid` primitives where the story required page-level layout ownership to move out of local CSS.
- Governance now blocks missing page layout declarations, raw page-root layout wrappers, and page-root CSS layout ownership.
- Component coverage now renders a full-width page and a constrained page inside `FrontComposerShell` and asserts shell layout state.

## File List

- `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
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

## Change Log

- 2026-06-18: Implemented FrontComposer/Fluent page-layout conformance sweep and moved story to review.

## Status

review
