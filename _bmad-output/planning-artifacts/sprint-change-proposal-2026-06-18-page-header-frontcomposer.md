# Sprint Change Proposal - FrontComposer Page Header Component

Date: 2026-06-18
Workflow: bmad-correct-course
Mode: Batch, inferred from the explicit correction request
Status: Approved and routed for implementation
Owner: Administrator
Approval: Administrator approved on 2026-06-18

## 1. Issue Summary

The correction request is that each route page's browser `PageTitle` and visible page header should be bundled in a FrontComposer component, and the page header should be uniform through the application.

This is a shared UI composition correction. Tenants pages currently declare document title and visible page header separately, with each page owning its own `header`, `h1`, eyebrow, description, return context, and action placement. That makes the header pattern drift page-by-page and keeps generic page chrome in the Tenants module instead of the FrontComposer technical module.

Current evidence:

- `TenantsWorkspace.razor`, `TenantDetailPage.razor`, `MyTenantsPage.razor`, `UserMembershipLookupPage.razor`, `GlobalAdministratorsPage.razor`, and `TenantAuditPage.razor` each declare their own `<PageTitle>` and page-level `<h1>` or state-heading structure.
- `FrontComposerShell` already owns shell layout and `FcPageLayout` already owns the `FC-LYT` page measure contract, but there is no FrontComposer-owned page-header component that bundles the document title and visible page heading.
- The Hexalith UX rules require module UI to use FrontComposer and Fluent UI Blazor v5 first, and to avoid Tenants-owned generic UI scaffolding when a shared capability belongs in FrontComposer.
- The active cross-cutting implementation artifact, `spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`, is still in review and explicitly says to stop before editing the FrontComposer submodule or adding shared layout APIs. This proposal is the missing approval boundary for that scope expansion.

## 2. Change Analysis Checklist

| Area | Status | Notes |
| --- | --- | --- |
| Triggering issue | Done | Administrator requested bundling `PageTitle` and page header into a FrontComposer component with uniform application-wide header behavior. |
| Triggering story | Done | No product epic story revealed this. It emerged during the active cross-cutting FrontComposer/Fluent page-layout conformance sweep, currently in review. |
| Core problem | Done | Page title/header is generic page chrome but is implemented repeatedly in Tenants pages. |
| Evidence gathered | Done | Source scan confirms direct `<PageTitle>` plus per-page `<header>`/`<h1>` ownership across route pages. FrontComposer has `FcPageLayout`, but no page-header equivalent. |
| Epic impact | Done | No feature epic objective changes. This is a cross-cutting UI composition correction. |
| Story impact | Done | Amend or reopen the active page-layout conformance story before it is finalized. If review has already closed, create an immediate follow-up cross-cutting story. |
| PRD impact | Done | No product scope change. The PRD already mandates FrontComposer/Fluent composition and shared UI capability ownership. |
| Architecture impact | Done | Add a FrontComposer-owned reusable page-header component contract; Tenants passes localized domain strings and fragments into it. |
| UI/UX impact | Done | Aligns page title, heading, eyebrow, description, action, and status placement across all route pages. |
| Test impact | Done | Add FrontComposer component tests and Tenants governance tests preventing direct page-owned title/header patterns from returning. |
| Path forward | Done | Direct Adjustment is viable. Rollback and MVP review are not justified. |

## 3. Impact Analysis

Epic impact:

- Epics 1 through 5 remain valid and complete according to sprint status.
- The change affects route pages produced by those epics, but it does not change FRs, command behavior, read models, authorization, audit, recovery, or support-safety requirements.
- No new product epic is needed.

Story impact:

- Preferred: reopen or amend `cc-2026-06-18-frontcomposer-fluent-layout-page-layout-conformance-sweep` before it is marked done, because the page-header pattern is part of page-level composition.
- Alternative: if that story has already been accepted, add a new cross-cutting story key such as `cc-2026-06-18-frontcomposer-page-header-component-conformance-sweep`.
- Completed feature stories remain done; this is a cross-cutting refactor over their page surfaces.

Artifact conflicts:

- `spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md` currently excludes adding shared FrontComposer APIs without human approval. This proposal, if approved, explicitly expands that boundary.
- Existing planning docs that state page titles identify scope remain correct, but they need implementation guidance that the page-title/header bundle is FrontComposer-owned.

Technical impact:

- Add a reusable FrontComposer shell/layout component, tentatively `FcPageHeader`, under `Hexalith.FrontComposer.Shell.Components.Layout` or the current FrontComposer layout-component convention.
- The component renders Blazor `<PageTitle>` and a uniform visible page header with a single route-level heading.
- The component accepts localized strings and optional slots/fragments from Tenants; it must not own Tenants domain copy.
- Migrate Tenants route pages to use the component instead of declaring `<PageTitle>` and root `<header>/<h1>` directly.
- Preserve stable selectors, focus behavior, accessibility semantics, localized resources, and existing route/page behavior.
- Add governance tests so future Tenants pages cannot reintroduce direct page-owned `PageTitle` and inconsistent route-level headers.

## 4. Recommended Approach

Recommended path: Direct Adjustment.

Rationale:

- The request reinforces the existing architecture boundary: FrontComposer owns reusable shell/page chrome; Tenants owns domain strings, data, flows, and page composition.
- The active layout conformance story is still in review, so the correction can be folded in before final acceptance rather than reopening many completed feature stories.
- The change is cross-module because the shared component belongs in `Hexalith.FrontComposer`. That makes the scope moderate, not a small Tenants-only cleanup.

Rejected alternatives:

- Keep a Tenants-local `TenantsPageHeader` wrapper: rejected because the capability is generic page chrome and would duplicate shared FrontComposer responsibility.
- Only normalize CSS around existing headers: rejected because it leaves `PageTitle` and header semantics split across pages.
- Roll back the page-layout conformance sweep: rejected because the previous work moved in the same direction and should be extended, not reverted.
- Rework PRD scope: rejected because this is implementation architecture/governance, not product scope.

## 5. Detailed Change Proposals

### 5.1 Active Cross-Cutting Story

Artifact: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`

Section: Intent

OLD:

```markdown
Page-level layout is still owned by Tenants page wrappers and page-root CSS grids.
```

NEW:

```markdown
Page-level layout and page chrome are still partially owned by Tenants page wrappers: route pages declare `PageTitle` and visible page headers independently, which produces inconsistent page headers and duplicates generic page-title/header scaffolding that belongs in FrontComposer.
```

Rationale: The story should cover page header composition alongside page measure/layout composition.

### 5.2 Boundary Expansion

Artifact: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`

Section: Boundaries & Constraints

OLD:

```markdown
Ask First: Stop before changing the Fluent package version, editing the FrontComposer submodule, adding shared layout APIs, changing command/query behavior, or turning this into a visual redesign.
```

NEW:

```markdown
Approved by this Sprint Change Proposal: add a narrow FrontComposer-owned page-header component that bundles Blazor `PageTitle` with the visible route-level header. Still stop before changing the Fluent package version, changing command/query behavior, adding unrelated shared layout APIs, or turning this into a visual redesign.
```

Rationale: The correction explicitly needs a shared FrontComposer component, so the previous "ask first" guard must be satisfied before implementation.

### 5.3 FrontComposer Component Contract

Artifact: FrontComposer shared module implementation story or amended cross-cutting story

Proposed component intent:

```razor
<FcPageHeader PageTitle="@DocumentTitle"
              Heading="@Heading"
              Eyebrow="@Eyebrow"
              Description="@Description"
              HeadingId="tenants-list-heading"
              TestId="tenants-page-header">
    <Actions>...</Actions>
    <Metadata>...</Metadata>
</FcPageHeader>
```

Contract notes:

- Render `<PageTitle>@PageTitle</PageTitle>` internally.
- Render exactly one route-level visible heading for the page header.
- Use Fluent/FrontComposer layout primitives, not raw page-specific layout scaffolding.
- Accept optional `Eyebrow`, `Description`, `Actions`, and `Metadata` slots.
- Preserve accessible heading id/focus needs through parameters such as `HeadingId`, `HeadingTabIndex`, and a documented focus pattern.
- Keep localization ownership with the consuming module: Tenants passes localized strings from `TenantsResources`.
- Provide stable test hooks without forcing Tenants-specific selector names into FrontComposer.

Rationale: This bundles the browser title and visual heading while keeping domain text and page-specific actions outside the shared component.

### 5.4 Tenants Page Migration

Affected pages:

| Page | Current gap | Target |
| --- | --- | --- |
| `TenantsWorkspace.razor` | Direct `<PageTitle>`, local `<header>`, local eyebrow, `h1`, return context | `FcPageHeader` with title, eyebrow, optional return-context metadata, and action/toolbar placement preserved nearby |
| `TenantDetailPage.razor` | Direct `<PageTitle>` and multiple state-specific `h1` headings | `FcPageHeader` owns the route-level heading; state headings become subordinate where needed |
| `MyTenantsPage.razor` | Direct `<PageTitle>` and local header/description | `FcPageHeader` owns title/header/description |
| `UserMembershipLookupPage.razor` | Direct `<PageTitle>` and local header/description | `FcPageHeader` owns title/header/description while form/status sections remain in accordion |
| `GlobalAdministratorsPage.razor` | Direct `<PageTitle>` and local header | `FcPageHeader` owns platform area header and optional restricted/unavailable title handling |
| `TenantAuditPage.razor` | Direct `<PageTitle>` and local audit header | `FcPageHeader` owns audit title and context metadata |

Migration rule:

```markdown
No route page under `src/Hexalith.Tenants.UI/Components/Pages` should declare `<PageTitle>` directly after migration. Route-level headers should use the FrontComposer page-header component. Section-level headings remain local to the page or component.
```

Rationale: This makes the page header uniform without stripping local section semantics.

### 5.5 Architecture Guidance Clarification

Artifact: implementation guidance in the cross-cutting story; architecture can be updated later if needed.

OLD:

```markdown
`MainLayout.razor` composes `<FrontComposerShell>@Body</FrontComposerShell>`. Individual Tenants pages declare page measure with `FcPageLayout`.
```

NEW:

```markdown
`MainLayout.razor` composes `<FrontComposerShell>@Body</FrontComposerShell>`. Individual Tenants pages declare page measure with `FcPageLayout` and route-level document title/header with the FrontComposer page-header component. Tenants supplies localized domain strings and page-specific fragments; FrontComposer owns the shared title/header structure.
```

Rationale: Keeps shell, page measure, and page header responsibilities in the shared UI module.

### 5.6 Governance Tests

Add or amend tests in `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`:

- Fail if a route page under `Components/Pages` declares `<PageTitle>` directly.
- Fail if a route page declares a top-level page `<header>`/`h1` pattern instead of the FrontComposer page-header component.
- Allow local `h2`/`h3` section headings and state headings where they are subordinate to the route header.
- Assert all route pages use the FrontComposer page-header component.
- Keep existing page-layout, raw control, raw table, raw form, accordion, and semantic CSS guards.

Add FrontComposer tests:

- The page-header component renders child fragments and visible heading consistently.
- The component emits `PageTitle` through Blazor's head outlet path.
- The component preserves accessible heading id and optional focus target behavior.
- The component has no Tenants-specific resource dependency.

Rationale: Prevents future drift and proves the new shared component stays generic.

### 5.7 Sprint Status

Preferred status change after approval, if the current review story is reopened:

OLD:

```yaml
cc-2026-06-18-frontcomposer-fluent-layout-page-layout-conformance-sweep: review
```

NEW:

```yaml
cc-2026-06-18-frontcomposer-fluent-layout-page-layout-conformance-sweep: in-progress
```

Then move it back to `review` after the header component and Tenants migration are implemented and verified.

Alternative status entry if the current review story is left intact:

```yaml
cc-2026-06-18-frontcomposer-page-header-component-conformance-sweep: ready-for-dev
```

Rationale: The preferred route avoids accepting a page-layout conformance story while known page-header conformance is still split across Tenants pages.

## 6. Implementation Handoff

Scope classification: Moderate.

Reason: The Tenants migration is straightforward, but the reusable component belongs in the shared `Hexalith.FrontComposer` technical module, so this crosses repository module boundaries and needs explicit approval.

Suggested execution sequence:

1. Approve this proposal and reopen the active layout conformance story, or create the follow-up story if review must remain closed.
2. Add the FrontComposer page-header component with focused component tests in the FrontComposer shell test project.
3. Migrate Tenants route pages from direct `<PageTitle>` plus local page header to the FrontComposer page-header component.
4. Preserve localized Tenants resource keys and existing visible copy unless a specific header copy change is required.
5. Preserve route behavior, stable selectors, focus restoration, command lifecycle, audit state, stale/degraded state, and support-safe behavior.
6. Add Tenants conformance tests that block direct route-page `<PageTitle>` and page-owned route headers.
7. Run affected FrontComposer shell tests, Tenants UI tests, and `git diff --check`.

Recommended verification:

```bash
dotnet test references/Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --no-restore
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore
git diff --check
```

Success criteria:

- Browser page title and visible route-level page header are bundled in a FrontComposer-owned component.
- Tenants route pages consume the component and no longer declare direct route-level `<PageTitle>` or page-owned route header scaffolding.
- The page header pattern is visually and semantically uniform across Tenants, My Tenants, User Lookup, Tenant Detail, Global Administrators, and Tenant Audit pages.
- The component remains generic: no Tenants resource dependency, no Tenants-specific selectors, no domain behavior.
- Existing command/query/audit/freshness behavior is unchanged.

Handoff recipients:

- Developer agent for implementation.
- FrontComposer owner or maintainer for shared component review.
- PO/sprint owner for sprint-status update after approval.

## 7. Approval Record

Administrator approved this Sprint Change Proposal on 2026-06-18.

Implementation route:

- Reopen/amend `cc-2026-06-18-frontcomposer-fluent-layout-page-layout-conformance-sweep`.
- Add the FrontComposer-owned page-header component and migrate Tenants route pages.
- Update sprint status from `review` to `in-progress` while the approved extension is implemented.
