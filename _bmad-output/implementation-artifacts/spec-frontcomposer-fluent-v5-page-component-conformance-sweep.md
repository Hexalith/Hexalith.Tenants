---
title: 'FrontComposer and Fluent UI v5 page-component conformance sweep'
type: 'refactor'
created: '2026-06-17'
status: 'done'
baseline_commit: '5666377fc41031fefe2fc1ecb04c096495b93e5e'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-17.md'
  - '{project-root}/Hexalith.AI.Tools/hexalith-ux-instructions.md'
---

<frozen-after-approval reason="human-owned intent - do not modify unless human renegotiates">

## Intent

**Problem:** Tenants UI already targets FrontComposer and Blazor Fluent UI v5, but several pages still use raw tables, section-heavy page markup, and layout/status patterns that should be represented with Fluent or FrontComposer components. This weakens the project-wide UI component policy and leaves gaps in the conformance tests.

**Approach:** Convert the remaining high-confidence raw data tables to `FluentDataGrid`, introduce `FluentAccordion` where there are sibling titled page sections, and tighten governance tests so raw table regressions are caught. Preserve domain behavior, selectors, focus restoration, accessibility names, and support-safe text.

## Boundaries & Constraints

**Always:** Use the pinned `Microsoft.FluentUI.AspNetCore.Components` API already referenced by the repo; keep grid-first pages directly visible; preserve `data-testid` selectors used by tests; keep FrontComposer/shared UI concerns out of Tenants unless the primitive is domain-specific; keep raw anchors where navigation semantics require them.

**Ask First:** Package upgrades, shared FrontComposer submodule edits, modal-flow redesigns, or any rewrite that changes command submission, audit proof, projection refresh, or lifecycle semantics.

**Never:** Add generic UI infrastructure to Tenants, remove support-safe copy/redaction behavior, hide stale/degraded rows behind a false success state, or initialize/modify nested submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Raw table conformance | Razor components render list/detail rows | Global administrators, tenant members, and configuration entries render through FluentDataGrid while preserving existing row test IDs and visible text | Governance test fails if a raw `<table>` tag returns |
| Multi-section detail | Tenant detail has identity, metadata, lifecycle, members, and configuration regions | Sibling titled regions are grouped with FluentAccordion; the first item is expanded and detail identity remains discoverable | Loading/error states remain distinct and support-safe |
| Command row behavior | Member/config/global-admin actions are clicked | Existing command flows, focus restoration, live regions, and unavailable reasons still work | Existing bUnit command tests remain green |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` -- global admin scope, grant/remove flows, and remaining raw table.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- multi-section tenant detail composition.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor` -- member review raw table and inline member command flows.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor` -- configuration raw table, grouping, filter, and remove flow.
- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` -- Fluent governance guard.
- `tests/Hexalith.Tenants.UI.Tests/Components/*.cs` -- bUnit assertions for changed rendering semantics.

## Tasks & Acceptance

**Execution:**
- [x] `GlobalAdministratorsPage.razor` -- replace the raw admin table with `FluentDataGrid` and group high-level sections with `FluentAccordion` where behavior remains stable.
- [x] `TenantDetailPage.razor` -- group applicable tenant detail sibling sections with `FluentAccordion` while keeping state and identity accessible.
- [x] `MemberAccessReview.razor` -- replace the raw member table with `FluentDataGrid` and preserve row actions, reason lists, and focus refs.
- [x] `TenantConfigurationView.razor` -- replace the raw configuration table with `FluentDataGrid` or accordion-plus-grid grouping and preserve filtering/redaction/copy actions.
- [x] `DomainUiFluentConformanceTests.cs` -- add a raw table regression guard and keep the existing raw interactive guard.
- [x] Component tests -- update only assertions coupled to raw table tags while preserving behavioral expectations.

**Acceptance Criteria:**
- Given Tenants UI Razor components, when governance tests scan them, then there are no raw `button`, `input`, `select`, `textarea`, or `table` tags.
- Given tenant detail renders with current data, when the page has multiple titled regions, then `FluentAccordion` renders the grouped regions and existing identity/member/config selectors remain available.
- Given global administrator, member, or configuration rows render, when tests query existing `data-testid` selectors, then row text, actions, copy buttons, and unavailable reasons still match previous behavior.
- Given stale/degraded/unauthorized states, when pages render, then safe state text and fail-closed behavior remain distinct and no false success text is introduced.

## Spec Change Log

## Design Notes

`FluentDataGrid` renders table semantics internally, so tests should keep user-facing and selector assertions but avoid requiring handwritten `<th scope="row">` markup. The raw-table governance guard should scan source Razor files, not rendered Fluent internals.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj` -- passed: 671 tests.

## Suggested Review Order

**Page Section Composition**

- Global administrator page regions now sit inside one Fluent accordion.
  [`GlobalAdministratorsPage.razor:16`](../../src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor#L16)

- Tenant detail keeps identity visible and groups operational regions.
  [`TenantDetailPage.razor:115`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L115)

**Grid Conversions**

- Global administrators list now uses FluentDataGrid cell templates.
  [`GlobalAdministratorsPage.razor:308`](../../src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor#L308)

- Member rows preserve action slots and focus refs inside FluentDataGrid.
  [`MemberAccessReview.razor:64`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor#L64)

- Configuration namespaces use accordion groups with nested FluentDataGrid.
  [`TenantConfigurationView.razor:106`](../../src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor#L106)

**Governance And Tests**

- Source guard now blocks raw table-family tags.
  [`DomainUiFluentConformanceTests.cs:32`](../../tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs#L32)

- Component assertions now target preserved selectors, not handwritten table tags.
  [`TenantDetailSurfaceTests.cs:391`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs#L391)
