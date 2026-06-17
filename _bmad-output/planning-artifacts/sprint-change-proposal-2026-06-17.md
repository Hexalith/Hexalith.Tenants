# Sprint Change Proposal - FrontComposer and Fluent UI v5 Page Component Conformance

Date: 2026-06-17
Workflow: bmad-correct-course
Mode: Batch, assumed from explicit user correction request
Status: Approved and implemented
Owner: Administrator

## 1. Issue Summary

The Tenants UI planning artifacts already require FrontComposer and Blazor Fluent UI v5 as the UI foundation, but the implemented pages need a focused conformance sweep. The correction request is to check each page for HTML tags or local component patterns that can be replaced by FrontComposer and Fluent UI v5 components, with page sections using `FluentAccordion` where appropriate.

Initial evidence from the pre-implementation code inventory:

- The UI project references `Hexalith.FrontComposer.Contracts`, `Hexalith.FrontComposer.Shell`, and `Microsoft.FluentUI.AspNetCore.Components`.
- The package pin is `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`.
- The Fluent UI Blazor documentation MCP currently targets `5.0.0.26139`; implementation must verify APIs against the pinned rc.3 package before changing code.
- No Tenants UI `.razor` file currently uses `FluentAccordion`.
- Raw data tables remain in:
  - `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`
  - `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`
  - `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`
- Raw forms remain in command and lookup flows even when Fluent inputs and buttons are already used.
- The current UI conformance test only blocks raw `button`, `input`, `select`, and `textarea`, so raw tables, page sections, and broader Fluent/FrontComposer composition gaps are not covered.

## 2. Change Analysis Checklist

| Area | Status | Notes |
| --- | --- | --- |
| Trigger understood | Complete | User explicitly requested FrontComposer and Fluent UI v5 replacement review across pages. |
| Epic impact | Complete | This touches completed UI stories across Epics 1, 2, 3, 4, and 5, but does not change business capabilities. |
| Artifact conflicts | Complete | PRD, architecture, and UX already support this direction. The gap is implementation conformance, not product intent. |
| Path forward | Complete | Recommend a direct adjustment with one cross-cutting remediation story. |
| Approval | Complete | Approved by Administrator on 2026-06-17; implemented through `spec-frontcomposer-fluent-v5-page-component-conformance-sweep.md`. |

## 3. Impact Analysis

### Epic Impact

No feature epics need to be reopened for product scope. The change should be tracked as a cross-cutting UI conformance hardening story because the affected pages span several completed stories.

Affected areas:

- Global administrator management
- Tenant detail, lifecycle, metadata, members, and configuration surfaces
- Tenant audit and evidence surfaces
- My tenants and workspace list surfaces
- User membership lookup
- Command flow components and inline lifecycle panels

### Artifact Impact

PRD: No requirement change is needed. The PRD already states that Tenants must use FrontComposer Shell and Fluent UI v5, avoid bespoke UI, and remain an operational console rather than a decorative dashboard.

Architecture: Add or reinforce an implementation note that hand-authored Tenants pages must follow the FrontComposer page-section layout guideline: two or more sibling titled sections should be grouped in a `FluentAccordion`, while page titles, breadcrumbs, toolbars, navigation chrome, and single-region grid-first pages remain outside the accordion.

UX design: No visual redesign is required. Add acceptance guidance for replacing raw data tables and section/state markup with Fluent or FrontComposer equivalents while preserving the existing information architecture.

Tests: Extend Tenants UI conformance coverage beyond raw interactive controls. At minimum, add coverage for raw table usage and a review-backed inventory for page-section accordion conformance.

### Technical Impact

The change is moderate in implementation size because it touches multiple Razor pages and command components, but it is low to medium risk because it should preserve existing BFF contracts, domain flows, test IDs, live regions, focus behavior, and command semantics.

No backend, event-store, domain model, or package capability changes are required. If a reusable UI primitive is missing, it should be added to `Hexalith.FrontComposer` in a separate shared-module change rather than duplicated in Tenants.

## 4. Recommended Approach

Use direct adjustment.

Add one cross-cutting remediation story and implement it against the current completed UI surfaces. Do not rewrite domain workflows. Replace the remaining HTML-first presentation patterns with Fluent/FrontComposer equivalents where there is a clear component match, and document any native HTML exception that must remain for browser semantics.

This avoids changing the product roadmap while creating a clear acceptance target for the requested conformance sweep.

## 5. Detailed Change Proposals

### Story Change

OLD:

No explicit sprint story exists for a post-completion FrontComposer and Fluent UI v5 page-component conformance sweep. Existing UI stories include the broad FrontComposer/Fluent requirement, but implementation validation currently focuses mainly on raw interactive controls.

NEW:

Add a cross-cutting remediation story:

Title: FrontComposer and Fluent UI v5 page-component conformance sweep

Acceptance criteria:

1. Replace remaining raw data tables with `FluentDataGrid` or an established FrontComposer grid primitive.
2. Group two or more sibling titled page sections with `FluentAccordion` and `FluentAccordionItem`, with the first or primary item expanded by default.
3. Keep page title, breadcrumbs, toolbar chrome, navigation chrome, and grid-first primary content outside accordions.
4. Replace status-only page sections with `FluentMessageBar`, existing Tenants state components, or appropriate FrontComposer state primitives.
5. Review raw forms and field wrappers. Use Blazor `EditForm`, Fluent field/input composition, or a documented native-form exception when browser submit semantics are required.
6. Use `FluentStack` or established FrontComposer layout primitives for command rows, filters, and action groups where that removes layout-only `div` structure.
7. Preserve all domain behavior, BFF calls, `data-testid` selectors, accessibility names, live-region behavior, focus restoration, and command lifecycle evidence.
8. Extend UI conformance tests to prevent regression for raw tables and existing raw interactive controls. Add a page inventory or test-backed review item for accordion conformance.

Rationale:

This records the correction as a contained implementation hardening item and avoids retroactively changing the business scope of completed stories.

### Sprint Status Change

OLD:

The sprint status tracks domain UI stories as complete or near-complete without a dedicated conformance remediation item.

NEW:

Add the remediation story as a backlog or ready-for-dev cross-cutting story. Mark it done only after code changes and conformance tests are complete.

### Architecture Note Change

OLD:

The architecture requires FrontComposer Shell and Fluent UI v5 and references the existing shell/composition model.

NEW:

Add an implementation note:

Hand-authored Tenants pages and panels must use FrontComposer or Fluent UI v5 components for page sections, grids, status surfaces, command rows, and field composition where an equivalent exists. Pages with two or more sibling titled sections should use one `FluentAccordion` with one `FluentAccordionItem` per section, except for page title, breadcrumb, toolbar, navigation chrome, single-region pages, and grid-first primary content that must remain directly visible.

### UX Acceptance Note Change

OLD:

The UX artifacts define an operational console style and require Fluent/FrontComposer, but do not provide a page-by-page replacement checklist for the implemented Razor files.

NEW:

Add or reference the page inventory below as implementation acceptance evidence for the conformance story.

## 6. Page and Component Inventory

| Surface | Current finding | Proposed replacement or rule |
| --- | --- | --- |
| `Pages/GlobalAdministratorsPage.razor` | Raw table plus multiple titled grant, remove, lifecycle, preview, and list sections. | Replace table with `FluentDataGrid`. Group grant/remove/list related sections in `FluentAccordion`. Use `FluentMessageBar` or existing command lifecycle components for lifecycle/status panels. |
| `Pages/TenantDetailPage.razor` | Multiple titled tenant detail sections and nested domain panels. | Use `FluentAccordion` for overview, metadata, members, configuration, lifecycle, and audit sections where they are sibling page sections. Keep page heading and primary summary outside or in the first expanded item according to layout fit. |
| `Pages/TenantAuditPage.razor` | Grid-first audit surface with controls and state sections. | Keep the audit grid directly visible. Use `FluentStack` for controls and `FluentMessageBar` or existing state components for status. Do not collapse the primary grid in an accordion. |
| `Pages/TenantsWorkspace.razor` | Grid-first workspace list. | Keep `TenantDataGrid` visible. Use `FluentStack` or FrontComposer layout for filters/actions where applicable. No accordion required for a single primary grid region. |
| `Pages/MyTenantsPage.razor` | Grid-first membership list. | Keep `MyTenantsDataGrid` visible. Use Fluent state/layout components for toolbar and messages. No accordion required for a single primary grid region. |
| `Pages/UserMembershipLookupPage.razor` | Lookup form and result/status sections. | Review raw form. Prefer `EditForm` plus Fluent fields, or document a native submit exception. Use `FluentMessageBar` for lookup status. |
| `Tenants/Members/MemberAccessReview.razor` | Raw member table with inline add/change/remove flows. | Replace table with `FluentDataGrid`. Preserve action slots, reason catalog, focus restoration, and row-level command flows. Use `FluentAccordion` for sibling command/review sections where appropriate. |
| `Tenants/TenantConfigurationView.razor` | Raw configuration table with namespace grouping and set/remove flows. | Replace table with `FluentDataGrid`, or use namespace `FluentAccordion` items containing grids if grouping must remain visually explicit. Preserve copy actions, filtering, row action focus, and remove flow behavior. |
| Command flow components | Raw forms and sibling preview/lifecycle panels appear across create, metadata, member, configuration, and lifecycle flows. | Prefer `EditForm`, Fluent field/input composition, `FluentAccordion` for editor/preview/lifecycle sections, and `FluentMessageBar` or existing `CommandLifecyclePanel` for status. |
| Status and receipt components | Some state panels are section-like markup. | Use `FluentMessageBar`, existing Tenants state components, or FrontComposer state primitives. Use `FluentCard` only for true repeated objects or framed receipts, not as a page-section substitute. |

## 7. Implementation Handoff

Recommended implementer: developer agent or direct implementation in the current branch.

Implementation steps:

1. Verify Fluent API names against `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1` before edits.
2. Convert raw tables first because they are concrete, testable conformance gaps.
3. Convert page-section groupings to `FluentAccordion` where the FrontComposer page-section guideline applies.
4. Review raw forms and state panels after section/grid conversion, preserving browser submit behavior and accessibility.
5. Extend `DomainUiFluentConformanceTests` or add adjacent tests for raw table detection and component usage evidence.
6. Run the UI test project and targeted build after implementation.

Definition of done:

- No raw data tables remain in Tenants UI pages or domain UI components unless a documented exception is added.
- All applicable multi-section pages use `FluentAccordion`.
- Existing grid-first pages keep their primary grid directly visible.
- Existing domain tests and UI conformance tests pass.
- No generic shared UI primitive is added to Tenants when it belongs in FrontComposer.

## 8. Approval Request

Approved by Administrator on 2026-06-17. Implementation is tracked in `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-v5-page-component-conformance-sweep.md`.
