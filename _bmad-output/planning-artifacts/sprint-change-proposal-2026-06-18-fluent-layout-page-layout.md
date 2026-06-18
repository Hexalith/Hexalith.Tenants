# Sprint Change Proposal - Fluent UI Layout for Page Layout

Date: 2026-06-18
Workflow: bmad-correct-course
Mode: Batch, inferred from the explicit correction request
Status: Approved and routed for implementation
Owner: Administrator

## 1. Issue Summary

The Tenants UI has completed two recent FrontComposer/Fluent UI v5 conformance sweeps: page component/table conformance on 2026-06-17 and control/CSS conformance on 2026-06-18. The new correction request identifies the next remaining conformance gap: page-level layout should use the Fluent UI Blazor v5 Layout pattern referenced at `https://fluentui-blazor-v5.azurewebsites.net/Layout`.

Current evidence:

- The repository is pinned to `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`; the local package XML documents `FluentLayout`, `FluentLayoutItem`, `FluentLayoutHamburger`, `FluentStack`, and `FluentGrid`.
- `FrontComposerShell.razor` already composes the application shell using Fluent `FluentLayout`, `FluentLayoutItem`, and `FluentStack`.
- FrontComposer already exposes the `FC-LYT` page-measure contract through `FcPageLayout` and `FcPageLayoutMode.FullWidth` / `Constrained`.
- Tenants pages still use page-owned wrappers such as `<main class="tenants-workspace">`, `<main class="tenant-detail">`, `<main class="tenant-audit">`, `<main class="global-admins">`, `<main class="my-tenants-page">`, and `<main class="user-lookup-page">`, with page-specific CSS grids controlling page layout.

This is a conformance and layout-governance correction. It does not change the product scope, routes, command behavior, projection-confirmation rules, authorization rules, or support-safety posture.

## 2. Change Analysis Checklist

| Area | Status | Notes |
| --- | --- | --- |
| Triggering issue | Done | Administrator requested use of the Fluent UI Blazor v5 Layout page for page layout. |
| Core problem | Done | Page-level layout is still expressed through Tenants-local wrappers and CSS grid rules instead of the confirmed FrontComposer/Fluent layout contract. |
| Evidence gathered | Done | Source scan confirms six top-level page wrappers and page-specific layout CSS; local Fluent package and FrontComposer shell sources confirm available layout primitives. |
| Epic impact | Done | No epic objective changes. Add one cross-cutting follow-up story/spec after the existing 2026-06-17 and 2026-06-18 conformance stories. |
| Story impact | Done | Completed UI stories stay done; the follow-up applies across pages produced by Epics 1, 2, 4, and 5. |
| PRD impact | Done | No PRD behavior changes. The PRD already mandates FrontComposer and Fluent UI v5 composition. |
| Architecture impact | Done | Existing architecture already identifies `FC-LYT` and `FrontComposerShell`; update implementation guidance only. |
| UI/UX impact | Done | Aligns with the responsive visual spec: full-width operational surfaces with constrained readable inner regions. |
| Test impact | Done | Add governance tests for page-layout wrappers and direct page-layout CSS ownership. |
| Path forward | Done | Direct Adjustment is viable; rollback and MVP review are not justified. |

## 3. Impact Analysis

Epic impact:

- Epics 1, 2, 4, and 5 remain complete.
- Epic 3 remains in progress by sprint status, but its implemented UI pages/flows are affected where they are composed into Tenants pages.
- Add one cross-cutting conformance story rather than reopening every completed story.

Story impact:

- Story 1.1 remains the shell bootstrap source of truth: `MainLayout.razor` must continue to compose `<FrontComposerShell>@Body</FrontComposerShell>`.
- Stories 1.2 through 1.7, 2.1 through 2.5, 4.1 through 4.4, and 5.1 through 5.6 have affected page surfaces or nested flows.
- The new story should focus on page-level layout only. Existing command lifecycle, audit, data grid, form, and semantic-state behavior should be preserved.

Artifact conflicts:

- `sprint-status.yaml` should receive a new cross-cutting key only after approval.
- A new implementation artifact should be created for the page-layout conformance sweep after approval.
- Existing docs that still mention `FC-LYT` as historical `needs-confirmation` are superseded by Story 1.0; no broad PRD or architecture rewrite is required.

Technical impact:

- Wrap page content in `FcPageLayout` where a page needs to declare `FullWidth` or `Constrained`.
- Keep dense grid-first operational pages full width.
- Use constrained layout for prose/form/detail pages or readable inner regions, through `FcPageLayoutMode.Constrained`, not Tenants-owned max-width CSS.
- Prefer Fluent layout primitives (`FluentStack`, `FluentGrid`, `FluentGridItem`, `FluentLayoutItem` when working at shell level) over page-owned grid wrappers for page composition.
- Reduce page `.razor.css` files to layout exceptions that cannot be expressed by FrontComposer/Fluent primitives.
- Add source governance that flags page-owned root layout wrappers and page-root `display: grid` unless the implementation artifact documents an exception.

## 4. Recommended Approach

Recommended path: Direct Adjustment.

Rationale:

- The requested change reinforces existing architecture and UX direction instead of changing product behavior.
- FrontComposer already owns the application shell with Fluent `FluentLayout`; Tenants should consume the shell and `FcPageLayout` rather than creating local shell/page layout infrastructure.
- The change can be implemented as a focused cross-cutting cleanup with tests.

Rejected alternatives:

- Roll back the prior conformance sweeps: not appropriate; those changes moved controls and tables toward the same Fluent/FrontComposer policy.
- Rebuild the shell in Tenants using raw `FluentLayout`: not appropriate; `FrontComposerShell` already owns shell layout and uses Fluent Layout internally.
- Rewrite all nested component spacing: too broad; this correction should target page-level layout, not every component-local arrangement.

## 5. Detailed Change Proposals

### 5.1 Sprint Status

Old:

```yaml
cross-cutting:
  cc-2026-06-17-frontcomposer-fluent-v5-page-component-conformance-sweep: done
  cc-2026-06-18-frontcomposer-fluent-control-and-css-conformance-sweep: done
```

New, after approval:

```yaml
cross-cutting:
  cc-2026-06-17-frontcomposer-fluent-v5-page-component-conformance-sweep: done
  cc-2026-06-18-frontcomposer-fluent-control-and-css-conformance-sweep: done
  cc-2026-06-18-frontcomposer-fluent-layout-page-layout-conformance-sweep: ready-for-dev
```

Rationale: Track the layout correction separately from the already completed table/control conformance work.

### 5.2 New Cross-Cutting Story/Spec

Create after approval:

`_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`

Proposed acceptance criteria:

1. Page-level layout uses `FrontComposerShell` and `FcPageLayout` for full-width/constrained layout measure decisions.
2. Dense operational pages and DataGrid-first pages remain full width unless a readable inner region is explicitly constrained.
3. Detail, form, command, and prose-heavy page regions use `FcPageLayoutMode.Constrained` or a documented FrontComposer/Fluent layout primitive instead of Tenants-owned max-width/page-grid CSS.
4. Page composition uses Fluent layout primitives such as `FluentStack`, `FluentGrid`, and `FluentGridItem` where the current page-owned grid wrappers are only arranging sibling page regions.
5. Existing `data-testid` selectors, localization keys, focus restoration, command lifecycle, stale/degraded states, and audit behavior are preserved.
6. No generic layout component or shell scaffolding is added to Tenants. Missing reusable capability is requested or implemented in `Hexalith.FrontComposer`.
7. Page `.razor.css` files retain only page-specific exceptions that cannot be represented through FrontComposer/Fluent layout components.
8. Governance tests reject new page-owned root layout wrappers and page-root grid CSS unless explicitly allowlisted with rationale.
9. Component tests cover at least one full-width grid page and one constrained/readable page layout declaration.
10. Verification runs against the repository's pinned Fluent UI Blazor package.

### 5.3 Story Text Update Pattern

Story: cross-cutting conformance story
Section: Boundaries & Constraints

OLD:

```markdown
Always: Use the repository's pinned Fluent UI Blazor v5 package and existing FrontComposer/Fluent patterns.
```

NEW:

```markdown
Always: Use the repository's pinned Fluent UI Blazor v5 package and existing FrontComposer/Fluent patterns. Page-level layout must be declared through `FrontComposerShell`/`FcPageLayout` and Fluent layout primitives. Do not add Tenants-owned shell, page-layout, breakpoint, or max-width infrastructure.
```

Rationale: Makes page layout a first-class conformance requirement without changing feature behavior.

### 5.4 Architecture Guidance Clarification

Old:

```markdown
MainLayout.razor composes <FrontComposerShell> (FC-LYT)
```

New implementation note:

```markdown
`MainLayout.razor` composes `<FrontComposerShell>@Body</FrontComposerShell>`. Individual Tenants pages declare page measure with `FcPageLayout` when they need a non-default layout. Use `FullWidth` for DataGrid-dense operational pages and `Constrained` for forms, prose, and detail regions. Do not create Tenants-local shell or page-layout abstractions.
```

Rationale: The shell-level layout is already Fluent; the gap is page-level adoption of the `FC-LYT` contract.

### 5.5 Governance Tests

Old:

- Guard raw button/input/select/textarea markup.
- Guard raw table markup.
- Guard raw form wrappers.
- Guard selected CSS semantic-control color ownership.
- Guard expected accordion usage on known multi-region pages.

New:

- Keep all existing guards.
- Add a page-layout guard for page components under `src/Hexalith.Tenants.UI/Components/Pages`.
- Fail on new page-root wrappers that own layout through raw `<main class="...">` unless the page is migrated or explicitly documented.
- Fail on page-root CSS selectors that set `display: grid` / major page grid behavior when the layout can be represented by `FcPageLayout`, `FluentStack`, or `FluentGrid`.
- Assert the layout declarations that should exist after migration, including at least one `FcPageLayoutMode.FullWidth` page and one `FcPageLayoutMode.Constrained` page.

## 6. Target Inventory

| Current surface | Current gap | Target |
| --- | --- | --- |
| `TenantsWorkspace.razor` | Root `<main class="tenants-workspace">` and page-owned controls/actions grid | `FcPageLayout` full-width page with Fluent layout primitives around header, filters, actions, create flow, grid, and pager |
| `TenantDetailPage.razor` | Root `<main class="tenant-detail">`, summary/facts grids, detail CSS owning page measure | `FcPageLayoutMode.Constrained` for readable detail regions, with dense member/config grids remaining directly visible and full-width where needed |
| `MyTenantsPage.razor` | Root page wrapper and toolbar layout CSS | `FcPageLayout` full-width or constrained based on final grid density, with Fluent layout primitives for toolbar and pager |
| `UserMembershipLookupPage.razor` | Root page wrapper, form/results sections, page-owned grid layout | `FcPageLayoutMode.Constrained` for lookup form/header, full-width results region if the grid needs it |
| `GlobalAdministratorsPage.razor` | Root wrapper and page-owned governance grid layout | `FcPageLayout` full-width for the administrator grid, constrained inner command/preview regions where appropriate |
| `TenantAuditPage.razor` | Root wrapper and page-owned audit controls/state/grid shell layout | `FcPageLayout` full-width for the audit grid; Fluent layout primitives for filter controls and state panels |
| Page `.razor.css` files | Root `display: grid`, `grid-template-columns`, page measure, and breakpoint ownership | Keep only component-specific exceptions; move page composition to FrontComposer/Fluent layout primitives |

## 7. Implementation Handoff

Scope: Moderate

Suggested execution sequence:

1. Create the implementation artifact/story with the acceptance criteria above.
2. Verify exact Fluent layout APIs against the pinned package `5.0.0-rc.3-26138.1` and FrontComposer source before editing Razor components.
3. Add `FcPageLayout` declarations to page components, choosing `FullWidth` or `Constrained` per surface.
4. Replace page-owned layout grids with `FluentStack`, `FluentGrid`, and `FluentGridItem` where they only arrange page regions.
5. Reduce page CSS to unavoidable exceptions and document any retained page-layout CSS.
6. Extend `DomainUiFluentConformanceTests` with page-layout governance.
7. Run `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore`.
8. Run `git diff --check`.

Required assignees:

- Developer agent for implementation.
- PO/sprint owner for sprint status update after approval.
- Architect/UX only if implementation discovers a missing reusable layout capability that belongs in `Hexalith.FrontComposer`.

Success criteria:

- Tenants pages use the confirmed `FC-LYT` contract and Fluent layout primitives for page layout.
- Existing routes, selectors, states, command behavior, audit behavior, and support-safe copy remain unchanged.
- Governance tests prevent page-level layout regressions.

## 8. Approval Request

Approved by Administrator on 2026-06-18.

Implementation handoff created:

`_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`

Sprint status entry:

`cc-2026-06-18-frontcomposer-fluent-layout-page-layout-conformance-sweep: ready-for-dev`
