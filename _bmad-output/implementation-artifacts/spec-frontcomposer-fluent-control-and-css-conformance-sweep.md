---
title: 'FrontComposer Fluent control and CSS conformance sweep'
type: 'refactor'
created: '2026-06-18'
status: 'done'
baseline_commit: '146173aaa4345df90450c89f2e6eed12095932ed'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-18.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent -- do not modify unless human renegotiates">

## Intent

**Problem:** Tenants UI still contains native HTML form wrappers and CSS-owned semantic control/status styling after the June 17 page component sweep. Those patterns violate the project rule that user-facing controls and visual state should come from FrontComposer or Blazor Fluent UI v5.

**Approach:** Replace remaining raw form wrappers with Blazor `EditForm` around existing Fluent controls, move status/control semantics out of bespoke CSS where the component model can own them, and extend governance tests so the regression cannot return.

## Boundaries & Constraints

**Always:** Use the repository's pinned Fluent UI Blazor v5 package and existing FrontComposer/Fluent patterns. Keep the change in the Tenants UI domain slice unless a truly generic missing primitive is discovered. Preserve current selectors, localized text, submit handlers, focus behavior, and command lifecycle behavior.

**Ask First:** Stop before adding new shared FrontComposer APIs, changing Fluent package versions, changing command semantics, or broadening this into a full visual redesign.

**Never:** Do not add generic UI infrastructure to Tenants. Do not replace all semantic HTML containers. Do not reintroduce raw visual or interactive controls. Do not use hard-coded colors for semantic status/control state when a Fluent role/component parameter exists.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Submit command flow | User submits an existing tenant command form | Existing submit handler runs from `EditForm`; Fluent fields/buttons keep current test selectors | Existing validation and unavailable messages stay visible |
| Audit/status state | Status or audit state is rendered | Fluent semantic primitives or layout-only CSS render the state without hard-coded bespoke colors | Unknown/degraded states map to neutral/warning Fluent roles |
| Regression scan | A raw form or semantic hex status style is added later | Governance test fails with offender path | Failure message points to the conformance rule |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Components/_Imports.razor` -- shared Razor imports for Blazor Forms.
- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` -- global admin grant command form.
- `src/Hexalith.Tenants.UI/Components/Pages/UserMembershipLookupPage.razor` -- user membership lookup form.
- `src/Hexalith.Tenants.UI/Components/Tenants/**/*Flow.razor` -- tenant command forms with existing Fluent fields and buttons.
- `src/Hexalith.Tenants.UI/Components/**/*.razor.css` -- component-local CSS requiring semantic color cleanup.
- `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` -- source governance guards.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- approved cross-cutting story status.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- add the approved cross-cutting key and track implementation state -- keeps sprint artifacts aligned with Correct Course approval.
- [x] `src/Hexalith.Tenants.UI/Components/_Imports.razor` and raw-form `.razor` files -- replace source-level raw `<form>` wrappers with `EditForm` using current Fluent controls and handlers -- removes HTML control markup without changing UX behavior.
- [x] `src/Hexalith.Tenants.UI/Components/**/*.razor.css` and related Razor components -- remove CSS-owned semantic control/status colors where Fluent roles or tokens should own state -- aligns visuals with Fluent authority.
- [x] `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` -- add raw form and semantic CSS guards -- prevents regression.

**Acceptance Criteria:**
- Given Tenants UI source, when governance tests scan `.razor` files, then no raw visual/interactive control tags or raw form wrappers are allowed.
- Given Tenants UI component CSS, when governance tests scan semantic status/control surfaces, then hard-coded semantic colors are rejected.
- Given existing command and lookup flows, when they are submitted, then the current submit handlers, selectors, localized labels, and disabled states remain intact.
- Given the UI test project, when it runs, then the conformance test suite passes against the pinned Fluent UI Blazor v5 package.

## Spec Change Log

## Verification

**Commands:**
- `rg -n "<form\\b|</form>" src/Hexalith.Tenants.UI/Components -g '*.razor'` -- expected: no matches.
- `rg -n "#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})\\b" src/Hexalith.Tenants.UI/Components -g '*.razor.css'` -- expected: no semantic status/control color offenders.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore` -- expected: pass.
- `git diff --check` -- expected: pass.

## Suggested Review Order

**Control Replacement**

- Audit navigation now uses Fluent link/button primitives.
  [`AuditEvidenceEntryPoint.razor:7`](../../src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceEntryPoint.razor#L7)

- Command flows replace raw forms with `EditForm`.
  [`CreateTenantFlow.razor:21`](../../src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor#L21)

**Visual Semantics**

- Tenant detail status is now a Fluent semantic badge.
  [`TenantDetailPage.razor:94`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L94)

**Regression Guards**

- Raw form and CSS semantic control guards live here.
  [`DomainUiFluentConformanceTests.cs:32`](../../tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs#L32)

- CSS color/native selector regression guard starts here.
  [`DomainUiFluentConformanceTests.cs:169`](../../tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs#L169)
