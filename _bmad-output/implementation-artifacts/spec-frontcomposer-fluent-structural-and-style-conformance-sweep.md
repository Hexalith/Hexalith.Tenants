---
title: 'FrontComposer Fluent structural-HTML and style-token conformance sweep'
type: 'refactor'
created: '2026-06-18'
status: 'ready-for-dev'
baseline_commit: '4ce8a84'
approval: 'Administrator approved sprint-change-proposal-2026-06-18-fluent-only-components-and-styles.md on 2026-06-18'
context:
  - '{project-root}/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-18-fluent-only-components-and-styles.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-control-and-css-conformance-sweep.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
---

<frozen-after-approval reason="human-owned intent -- do not modify unless human renegotiates">

## Intent

**Problem:** Tenants UI controls, tables, forms, page layout, and page headers now conform to Fluent v5 / FrontComposer (controls/forms/tables source scans = 0 offenders). But route pages and components still use layout-only `<div>`/`<span>` wrappers, text-styling `<span>`/`<p>`, and 28 component-local `.razor.css` files that own spacing/typography/layout. A 2026-06-18 source scan found `<div>` 138, `<span>` 107, `<p>` 116, `<section>` 56, `<dl>/<dt>/<dd>` 14/52/52. This violates the standing UX rule (`hexalith-ux-instructions.md`): use FrontComposer/Fluent v5 first; fall back to raw HTML/CSS only when no equivalent exists; use Fluent layout and styles.

**Approach:** Replace layout-only `<div>`/`<span>` wrappers with `FluentStack`/`FluentGrid`; replace text-styling `<span>`/`<p>` with `FluentLabel`/Fluent typography where they carry no semantic role; reduce each component `.razor.css` to Fluent design tokens (`var(--*)` spacing/typography) plus a documented exception allowlist. Keep accessibility landmarks, lists, and nav anchors as the documented "no Fluent equivalent" fallback. Add governance so the gap cannot reopen.

## Boundaries & Constraints

**Always:** Use the repository's pinned `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1` API and verified FrontComposer source. Preserve routes, `data-testid` selectors, localized copy, focus restoration, live regions, command lifecycle behavior, audit behavior, stale/degraded states, and support-safe copy/redaction behavior. Verify exact `FluentStack`/`FluentGrid`/`FluentGridItem`/`FluentLabel` parameters against the pinned package before editing.

**Keep (documented fallback — no Fluent v5 equivalent, record in the governance allowlist with rationale):** semantic landmarks `<header>`/`<section>`/`<nav>` (`<main>` is shell-owned and already guarded), description lists `<dl>`/`<dt>`/`<dd>`, bullet/ordered lists `<ul>`/`<ol>`/`<li>`, and raw `<a>` nav links.

**CSS exceptions allowed (record per file with a `/* fc-css-exception: <reason> */` marker):** `@media (forced-colors)` / `forced-color-adjust`, `:focus-visible` / focus outlines, `overflow`/scroll behavior, and state visualization Fluent cannot express. Everything else (layout flex/grid, `gap`, non-zero `margin`/`padding`, `font-size`/`font-weight`/`line-height`) moves to Fluent primitives + design tokens.

**Never:** Add Tenants-owned shell/page-layout/breakpoint/provider/max-width infrastructure. Convert a11y landmarks or nav anchors to non-semantic markup. Weaken any existing conformance guard (controls, forms, tables, accordion, page-layout, page-header, page-root layout/CSS, semantic colors). Add a new shared FrontComposer API — if a primitive is genuinely missing (e.g. a definition-list equivalent), **stop and record the gap for the FrontComposer owner**. Initialize or modify nested submodules; edit submodule files (`hexalith-ux-instructions.md`, FrontComposer `project-context.md`) — those are owner handoffs.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|----------------------------|----------------|
| Layout-only wrapper | A `<div>`/`<span>` exists only to arrange/space sibling regions | Rendered through `FluentStack`/`FluentGrid` with Fluent gap/spacing | Governance fails on inline layout `style=` and on component CSS owning layout |
| Text styling | A `<span>`/`<p>` exists only to size/weight text | Rendered through `FluentLabel` / Fluent typography | Semantic/inline-meaning cases are kept and allowlisted |
| a11y landmark / list / nav | `<header>`/`<section>`/`<nav>`/`<dl>`/`<ul>`/`<a>` carries semantics | Kept as-is, recorded in the allowlist with rationale | Removing or de-semanticizing one fails review |
| Component CSS | A `.razor.css` sets flex/grid/gap/margin/padding/font | Reduced to Fluent design tokens; remainder marked as documented exception | Guard fails on unmarked layout/typography ownership |
| Missing primitive | No Fluent/FrontComposer equivalent exists | Work stops; gap + proposed owner recorded | Do not add generic infrastructure to Tenants |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Pages/*.razor` + `*.razor.css` — six route pages.
- `src/Hexalith.Tenants.UI/Components/Tenants/**/*.razor` + `*.razor.css` — member/config/lifecycle/metadata/audit flows and views (largest CSS: `MemberAccessReview` 184, `TenantConfigurationView` 161, `ChangeTenantMemberRoleFlow` 130, `RemoveTenantConfigurationFlow` 126).
- `src/Hexalith.Tenants.UI/Components/Users/**` and `Shared/**` — `MyTenants*`, `TruthStateBadge`, `ListSurfaceStates`, `SupportSafeCopyButton`.
- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` — add the three new guards (§5.3 of the proposal); keep all nine existing guards.
- `tests/Hexalith.Tenants.UI.Tests/Components/*.cs` — update only assertions coupled to removed wrapper markup; preserve behavioral assertions.

## Tasks & Acceptance

**Execution:**
- [ ] Verify exact `FluentStack`/`FluentGrid`/`FluentGridItem`/`FluentLabel` APIs against the pinned package before editing.
- [ ] Add the three governance guards first (red phase): (a) component CSS must not own layout/spacing/typography outside documented exceptions; (b) no inline layout `style=` on raw elements; (c) structural-HTML allowlist + a ratcheting `<div>`+`<span>` budget ceiling.
- [ ] Migrate layout-only `<div>`/`<span>` wrappers to `FluentStack`/`FluentGrid` across `Components/**`.
- [ ] Migrate text-styling `<span>`/`<p>` to `FluentLabel` / Fluent typography where they carry no semantic role; keep semantic/inline cases.
- [ ] Reduce each component `.razor.css` to Fluent design tokens + `/* fc-css-exception: <reason> */`-marked exceptions; lower the structural-tag budget to the achieved floor.
- [ ] Record the kept a11y/nav HTML allowlist (landmarks, lists, anchors) with per-tag rationale.
- [ ] Preserve selectors/copy/focus/live-regions/command-lifecycle/audit/stale/support-safe behavior; update only assertions coupled to removed wrapper markup.
- [ ] Update sprint status to `done` only after verification passes.

**Acceptance Criteria:**
- Given Tenants UI source, when governance scans `.razor` files, then there are no layout-only `<div>`/`<span>` wrappers carrying inline layout styles, and the `<div>`+`<span>` budget is at or below the recorded ceiling.
- Given Tenants UI component CSS, when governance scans `*.razor.css`, then no file owns layout/spacing/typography outside `@media (forced-colors)`, focus, overflow, or a `/* fc-css-exception: … */`-marked rule.
- Given a11y landmarks, lists, and nav anchors, when governance scans pages, then each kept raw structural tag is present in the documented allowlist with a rationale.
- Given existing flows render, when bUnit and conformance suites run, then routes, `data-testid` selectors, localized labels, focus behavior, command lifecycle, audit/stale/degraded states, and support-safe behavior are unchanged.
- Given the UI test project, when it runs against the pinned Fluent v5 package, then all guards (nine existing + three new) and component tests pass.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore`
- `git diff --check`

## Dev Agent Record

(empty — to be filled by the developer agent)

## Status

ready-for-dev
