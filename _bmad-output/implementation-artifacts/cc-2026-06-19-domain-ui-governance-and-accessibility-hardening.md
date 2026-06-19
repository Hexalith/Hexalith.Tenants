---
title: 'Domain UI governance and accessibility hardening'
type: 'correct-course-hardening'
created: '2026-06-19'
status: 'ready-for-dev'
sprint_key: 'cc-2026-06-19-domain-ui-governance-and-accessibility-hardening'
source_proposal: '_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19-deferred-work.md'
approval: 'Administrator approved sprint-change-proposal-2026-06-19-deferred-work.md on 2026-06-19'
context:
  - '{project-root}/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/Hexalith.AI.Tools/hexalith-ux-instructions.md'
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-structural-and-style-conformance-sweep.md'
  - '{project-root}/_bmad-output/implementation-artifacts/deferred-work.md'
---

<frozen-after-approval reason="human-approved correct-course scope - guard behavior may change only inside this story">

## Intent

Re-approve and harden the Tenants UI governance guards that were intentionally deferred from the structural/style conformance sweep, then add small missing component tests for current FluentStack migrations and cosmetic route-heading fallbacks.

## Boundaries & Constraints

**Always:**
- Follow FrontComposer and Blazor Fluent UI V5 first. Use raw HTML/CSS only when there is no FrontComposer or Fluent equivalent.
- Preserve existing routes, stable selectors, localized copy, command lifecycle behavior, audit behavior, stale/degraded states, focus behavior, and support-safe copy.
- Treat this story as a guard and focused-test hardening story, not a visual redesign.
- Keep rule changes additive or explicitly justified. If a frozen Section 5.3 behavior is changed, document the before/after decision in this artifact.

**Never:**
- Do not weaken existing page-layout, page-header, control, table, form, accordion, or semantic CSS conformance guards.
- Do not add shared FrontComposer APIs or edit submodule files from this repository.
- Do not move shell/page landmark ownership into Tenants. FrontComposer shell/page-header accessibility remains an owner handoff.

</frozen-after-approval>

## Code Map

- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`

## Tasks & Acceptance

**Execution:**
- [ ] Add red-phase guard tests for compact non-zero spacing declarations such as `margin:0.5rem` and `padding:0.5rem`.
- [ ] Expand the raw-element inline-style guard to cover spacing, sizing, and alignment declarations beyond the original narrow set.
- [ ] Exclude comments before counting `<div>` and `<span>` budget usage.
- [ ] Decide and document whether `fc-css-exception` remains rule-scoped or becomes declaration-scoped.
- [ ] Decide and document whether `:focus-visible` remains a blanket exemption or is narrowed.
- [ ] Harden or replace `RemoveForcedColorsMediaBlocks` if braces in comments or strings can desynchronize it.
- [ ] Add bUnit coverage that `aria-controls` points to a rendered active-region `id` in `MemberAccessReview`.
- [ ] Add a localized blank/whitespace `TenantId` fallback for `TenantAuditPage` if accepted by the final implementation.
- [ ] Update `deferred-work.md` after the guard decisions are implemented.

**Acceptance Criteria:**
1. Given component CSS contains compact non-zero spacing such as `margin:0.5rem` or `padding:0.5rem`, when `Domain_ui_component_css_does_not_own_layout_spacing_or_typography` scans it, then it is flagged unless covered by an approved `fc-css-exception`.
2. Given inline raw-element `style=` contains layout/spacing/measure declarations beyond the original narrow set, including `margin`, `padding`, `width`, `inline-size`, `justify-content`, or `align-items`, when governance scans `.razor` source, then it is flagged unless the story records an explicit exception.
3. Given `<div>`/`<span>` budget counting, when comments contain tag-like text, then comments are excluded before counting.
4. Given `fc-css-exception` markers, when a marker exempts a rule, then the story either preserves rule-level scoping with a documented rationale or introduces declaration-level scoping with updated tests.
5. Given `:focus-visible` is an approved exemption today, when this story reviews it, then the final behavior is explicitly approved: retain blanket exemption or narrow it with rationale.
6. Given `RemoveForcedColorsMediaBlocks` strips forced-colors blocks, when CSS contains braces in comments or strings, then the helper remains stable or is replaced with a safer parser.
7. Given `MemberAccessReview` opens change-role or remove-member regions, when bUnit renders the active region, then the `aria-controls` source button points to a rendered target `id` after the FluentStack migration.
8. Given `TenantAuditPage` receives a blank or whitespace `TenantId`, when the page header renders, then it uses a localized fallback rather than a dangling `Audit - ` heading. This is cosmetic, not a crash fix.

## Verification

- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore --filter DomainUiFluentConformanceTests`
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore --filter TenantDetailSurfaceTests`
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore --filter TenantAuditPageTests`
- `git diff --check`

## Dev Agent Record

Pending implementation.
