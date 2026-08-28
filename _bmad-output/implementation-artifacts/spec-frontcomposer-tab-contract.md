---
title: 'FrontComposer explicit page-tab panel contract'
type: 'bugfix'
created: '2026-08-28'
status: 'blocked'
baseline_revision: 'b5d2734f1774923c5f4334b898653cfc49abf369'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Tenants renders header-only Fluent tabs while the selected surface lives in sibling `FcAggregateListPage` slots, so the generated tabpanels are empty and Tenants has no owned proof of selection or keyboard transitions.

**Approach:** Add a body-level FrontComposer page-tabs contract whose tab children own their real panel content, migrate the complete Tenants and Users surfaces into it, and lock the Fluent v5 association and keyboard behavior with component and browser tests.

## Boundaries & Constraints

**Always:** Keep Fluent UI v5 responsible for tab semantics, roving focus, and selection; let its pinned `${tabId}-panel` convention create the association from actual `FluentTab.ChildContent`. Preserve `tenants|users` URL/state behavior, stable selectors, lazy surface loading, localized labels, and all existing authorization/freshness/create-command gates. Work in the owning FrontComposer and Tenants repositories and declare both repositories' changed files. For dependency verification only, advance the Tenants root-declared `references/Hexalith.Builds` gitlink from `9aca670aa9d4605bb147f641ef23d30d37813e92` to exactly `fd606d51826a8282cacecace965ed502461a2e33`; that approved commit's only dependency-version changes align `xunit.v3`, `xunit.v3.assert`, and `xunit.v3.extensibility.core` at `4.0.0`.

**Block If:** The implementation cannot keep full page-body content outside `FcPageHeader.Actions`, requires changing a fail-closed Tenants rule or the approved tab route semantics, or still cannot restore the FrontComposer test graph after the exact approved `Hexalith.Builds` gitlink advance without another dependency change or restore-policy bypass.

**Never:** Edit `_bmad-output/implementation-artifacts/deferred-work.md`; override `aria-controls`/panel ids through `FluentTab.AdditionalAttributes`; put grids, states, or command flows inside the page header; hand-roll raw tab controls; eagerly issue hidden-panel gateway requests; change any dependency version except through the exact approved `Hexalith.Builds` gitlink advance above; add local FrontComposer or Tenants package-version overrides; disable central package management or transitive pinning; use a restore-policy overlay to bypass the graph; change backend contracts or the deferred ledger.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Initial workspace | `/tenants` | Tenants is active; its generated panel contains all tenant controls/states/body/pager; Users is inactive and lazy | No empty or missing controlled panel |
| Keyboard switch | Focused horizontal tab; ArrowRight/ArrowLeft/Home/End | Fluent moves focus/selection, toggles the matching panel, and the callback updates canonical workspace state | Disabled tabs are skipped; wrapping follows pinned Fluent behavior |
| Direct/invalid route | `tab=users` or unknown `tab` | Users loads without a tenant-list request; unknown values normalize to Tenants | Existing support-safe/fail-closed states remain authoritative |
| Unsafe create evidence | stale, ambiguous Unknown, non-empty Unknown, or disconnected command surface | Tenant create remains disabled | Only authoritative first-tenant empty Unknown retains the documented exception |
| FrontComposer test restore | Tenants root gitlink `references/Hexalith.Builds` at `fd606d51826a8282cacecace965ed502461a2e33` | `xunit.v3`, `xunit.v3.assert`, and `xunit.v3.extensibility.core` resolve together at `4.0.0`, and the exact Shell.Tests build proceeds | Block on any remaining conflict; do not add local overrides, select another dependency revision, or weaken restore policy |

</intent-contract>

## Code Map

- `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor` and `FcPageToolbarTab.cs` -- current header-only compatibility API; do not place workspace body content here.
- `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcAggregateListPage.razor` -- page header plus body slots; body-level tabs belong after the header, not in `Toolbar`.
- `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageTabs.*` and `FcPageTab.*` -- new shared wrapper/child contract; expose active id/callback, accessible/test labels, disabled/icon/deferred-loading options, and real panel content.
- `references/Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageTabsTests.cs` -- deterministic component, `${id}-panel`, content, lazy-loading, and callback coverage.
- `references/Hexalith.FrontComposer/samples/Counter/Counter.Specimens/FrontComposerPageToolbarSpecimen.razor` plus `tests/e2e/{page-objects/page-toolbar-specimen.page.ts,specs/page-toolbar.spec.ts}` -- browser-owned Arrow/Home/End focus, selected state, visibility, and reciprocal association proof.
- `references/Hexalith.FrontComposer/docs/reference/components/{index.md,page-tabs.md}` -- adopter contract and warning against external sibling panels.
- `references/Hexalith.Builds` -- Tenants-owned root gitlink may advance only from `9aca670aa9d4605bb147f641ef23d30d37813e92` to `fd606d51826a8282cacecace965ed502461a2e33` for the approved xUnit-family alignment; do not edit the Builds repository in this story.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:12` -- migrate the complete Tenants and Users surfaces into lazy `FcPageTab` panels; keep state methods at lines 1189-1348 unchanged except composition-required mapping.
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs:88` and `Components/TenantListSurfaceTests.cs:359` -- initial/changed selection, non-empty associations, direct Users/invalid route, preserved query, and no cursor leakage.
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs:565` -- read-only regression locks for the first-tenant exception and fail-closed stale/ambiguous/disconnected cases.

## Tasks & Acceptance

**Execution:**
- Tenants dependency gitlink -- advance only root-declared `references/Hexalith.Builds` to `fd606d51826a8282cacecace965ed502461a2e33`; make no package edits or other dependency-pointer changes.
- FrontComposer layout source -- implement the additive body-level tab/panel API with XML docs and Fluent child content; keep each C# type in its own file.
- FrontComposer component/specimen/docs/e2e files -- prove deterministic association and real Chromium keyboard behavior; document the derived panel id and body-placement rule.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- recompose existing fragments under their owning panels without changing gateway, route, support-safety, or command-admission logic.
- Tenants UI tests -- replace presence-only/raw-component coupling with shared-contract association and transition evidence; retain all create/freshness locks.

**Acceptance Criteria:**
- Given the FrontComposer Shell.Tests verification lane, when its project restores through the Tenants root dependency graph, then `references/Hexalith.Builds` is exactly `fd606d51826a8282cacecace965ed502461a2e33`, the three xUnit v3 packages resolve at `4.0.0`, and no local package override or restore-policy bypass is present.
- Given any enabled `FcPageTab`, when rendered, then its Fluent tab controls exactly one `${id}-panel` with `role=tabpanel` and caller-owned non-empty content.
- Given the browser specimen on Summary, when ArrowRight, End, Home, and reverse/wrap transitions run, then focus, `aria-selected`, active state, and visible panel stay synchronized.
- Given `/tenants` or `?tab=users`, when selection changes, then only the selected surface is active, canonical state and prior tenant query context are preserved, and no foreign cursor or eager gateway request crosses panels.
- Given stale, ambiguous, non-empty Unknown, or disconnected command evidence after migration, when create availability is evaluated, then it remains disabled; authoritative empty Unknown remains the sole bootstrap exception.

## Spec Change Log

- 2026-08-28: Human resolution approved only the exact Tenants `Hexalith.Builds` gitlink advance to `fd606d51826a8282cacecace965ed502461a2e33` so the required xUnit test graph aligns at `4.0.0`; all other dependency changes and restore bypasses remain forbidden.

## Review Triage Log

## Design Notes

The pinned Fluent package hard-codes panel ids and splats `AdditionalAttributes` onto both header and panel. The contract therefore carries content, not caller-defined external panel ids. Browser tests own focusgroup behavior because bUnit does not execute Fluent custom-element JavaScript.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Debug -m:1` then the built xUnit executable filtered to `FcPageTabsTests` -- expected: shared contract passes.
- `npm --prefix tests/e2e run typecheck && npm --prefix tests/e2e run test:fc-page-toolbar` from FrontComposer -- expected: Chromium association, keyboard, and axe checks pass.
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` then the built UI test executable -- expected: full UI suite passes against modified FrontComposer source.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-frontcomposer-tab-contract.md` -- expected: no undeclared moved gitlink.

## Auto Run Result

Status: blocked
Blocking condition: implementation verification failed

The exact command `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Debug -m:1` fails during restore with `NU1107`: the centrally managed test graph combines `xunit.v3.common` 4.0.0 through `xunit.v3` 4.0.0 with `xunit.v3.common` 3.2.2 through `xunit.v3.extensibility.core` 3.2.2. The intent contract forbids dependency-version changes, so the workflow cannot fix or bypass the conflict. No FrontComposer bUnit assembly was produced and the matrix test audit cannot be completed.
