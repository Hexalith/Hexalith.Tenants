---
title: 'FrontComposer Fluent layout page-layout conformance sweep'
type: 'refactor'
created: '2026-06-18'
status: 'ready-for-dev'
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
- [ ] Verify exact local APIs for `FcPageLayout`, `FcPageLayoutMode`, `FluentStack`, `FluentGrid`, and `FluentGridItem` against the pinned package/source before editing.
- [ ] Add page-layout declarations to Tenants pages, using `FullWidth` for DataGrid-dense pages and `Constrained` for form/prose/detail measure where appropriate.
- [ ] Replace page-owned layout grids with Fluent layout primitives where those grids only arrange sibling page regions.
- [ ] Reduce page `.razor.css` root layout rules to documented exceptions only.
- [ ] Extend `DomainUiFluentConformanceTests` with page-layout governance and allowlist rationale.
- [ ] Add component tests proving at least one full-width page and one constrained/readable page declare the expected layout.
- [ ] Update sprint status from `ready-for-dev` to the appropriate execution state when implementation starts and to `done` only after verification passes.

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
