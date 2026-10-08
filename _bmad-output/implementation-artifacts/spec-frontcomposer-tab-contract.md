---
title: 'FrontComposer explicit page-tab panel contract'
type: 'bugfix'
created: '2026-08-28'
status: in-progress
baseline_revision: 'b5d2734f1774923c5f4334b898653cfc49abf369'
baseline_commit: 'b011873a2cae73718f5054a83569ea6ac92e3bdb'
resumed_revision: '94c6a89c01bedce23cbeacfae5dae3b4c49cd2fa'
resumed_frontcomposer_revision: 'c561b3210f15206a90c39c82c58f2e5b1005cd60'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
warnings:
  - 'Current Builds revision and aligned xUnit 4.0.1 predate this resumed run but differ from the approved exact dependency acceptance; clarification is pending.'
  - 'The preserved baseline_commit includes nine later committed gitlink changes; the story gitlink validation fails before any resumed-run pointer changes.'
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
- [ ] Tenants dependency gitlink -- advance only root-declared `references/Hexalith.Builds` to `fd606d51826a8282cacecace965ed502461a2e33`; make no package edits or other dependency-pointer changes. The approved advance was committed previously; current dependencies are newer, so the exact current-state acceptance remains unresolved.
- [x] FrontComposer layout source -- implement the additive body-level tab/panel API with XML docs and Fluent child content; keep each C# type in its own file. Already present at the resumed FrontComposer revision; verified without production changes.
- [x] FrontComposer component/specimen/docs/e2e files -- prove deterministic association and real Chromium keyboard behavior; document the derived panel id and body-placement rule.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- recompose existing fragments under their owning panels without changing gateway, route, support-safety, or command-admission logic. Already present at the resumed Tenants revision; preserved and verified.
- [x] Tenants UI tests -- replace presence-only/raw-component coupling with shared-contract association and transition evidence; retain all create/freshness locks.

**Acceptance Criteria:**
- Given the FrontComposer Shell.Tests verification lane, when its project restores through the Tenants root dependency graph, then `references/Hexalith.Builds` is exactly `fd606d51826a8282cacecace965ed502461a2e33`, the three xUnit v3 packages resolve at `4.0.0`, and no local package override or restore-policy bypass is present.
- Given any enabled `FcPageTab`, when rendered, then its Fluent tab controls exactly one `${id}-panel` with `role=tabpanel` and caller-owned non-empty content.
- Given the browser specimen on Summary, when ArrowRight, End, Home, and reverse/wrap transitions run, then focus, `aria-selected`, active state, and visible panel stay synchronized.
- Given `/tenants` or `?tab=users`, when selection changes, then only the selected surface is active, canonical state and prior tenant query context are preserved, and no foreign cursor or eager gateway request crosses panels.
- Given stale, ambiguous, non-empty Unknown, or disconnected command evidence after migration, when create availability is evaluated, then it remains disabled; authoritative empty Unknown remains the sole bootstrap exception.

## Spec Change Log

- 2026-08-28: Human resolution approved only the exact Tenants `Hexalith.Builds` gitlink advance to `fd606d51826a8282cacecace965ed502461a2e33` so the required xUnit test graph aligns at `4.0.0`; all other dependency changes and restore bypasses remain forbidden.
- 2026-10-08: Resumed from the recorded revisions above. The production contract and workspace migration were already present; strengthened component/browser association, transition, lazy-loading, header-placement, query, and cursor evidence. Preserved the original dependency constraint and baseline while recording the current-state mismatch for human resolution.

## Review Triage Log

## Design Notes

The pinned Fluent package hard-codes panel ids and splats `AdditionalAttributes` onto both header and panel. The contract therefore carries content, not caller-defined external panel ids. Browser tests own focusgroup behavior because bUnit does not execute Fluent custom-element JavaScript.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Debug -m:1` then the built xUnit executable filtered to `FcPageTabsTests` -- expected: shared contract passes.
- `npm --prefix tests/e2e run typecheck && npm --prefix tests/e2e run test:fc-page-toolbar` from FrontComposer -- expected: Chromium association, keyboard, and axe checks pass.
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` then the built UI test executable -- expected: full UI suite passes against modified FrontComposer source.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-frontcomposer-tab-contract.md` -- expected: no undeclared moved gitlink.

**2026-10-08 resumed-run results:**

Commands from `references/Hexalith.FrontComposer`:
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Debug -m:1` -- passed, 0 warnings and 0 errors.
- `tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests -class Hexalith.FrontComposer.Shell.Tests.Components.Layout.FcPageTabsTests` -- 27 passed, 0 failed, 0 skipped.
- `npm --prefix tests/e2e run typecheck` -- passed.
- `PLAYWRIGHT_SKIP_WEBSERVER=1 npm --prefix tests/e2e run test:fc-page-toolbar` -- 7 Chromium tests passed, including reciprocal associations, every keyboard transition, lazy retention, and axe. Used a separately started Debug Counter.Web host with specimens enabled; stopped it after verification. [JUnit report](../../references/Hexalith.FrontComposer/tests/e2e/test-results/junit.xml) and [HTML report](../../references/Hexalith.FrontComposer/tests/e2e/playwright-report/index.html).

Commands from Tenants:
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` -- passed, 0 warnings and 0 errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests` -- 3,986 passed, 0 failed, 0 skipped. Includes initial/direct/invalid selection, canonical state and query preservation, cursor isolation, no eager requests, and all existing create/freshness/authorization regressions.
- `git diff --check` -- passed in both owning repositories.
- `git diff --ignore-submodules=dirty --raw 94c6a89c01bedce23cbeacfae5dae3b4c49cd2fa -- references/` -- empty: no gitlink moved during the resumed run.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-frontcomposer-tab-contract.md` -- failed on nine pre-existing, undeclared committed pointer differences from the preserved baseline. No pointer was reverted or newly advanced to hide that failure.

The current unmodified Builds revision is `870bd6b85ec5cd841da6fbcc7dbc71b8e4ad764b`; all three xUnit v3 packages resolve at `4.0.1`. This passes the verification lanes but does not satisfy the original exact `fd606d51826a8282cacecace965ed502461a2e33` / `4.0.0` acceptance. The prior approved dependency advance is recorded in commit `eb965727329c7d7335be4cd341db4e2f9bf57b56`, which predates the preserved `baseline_commit`. Asked whether to retain current dependencies and revise the acceptance or explicitly return to the older approved catalog; no answer has been assumed. Formal review/completion remains pending these acceptance and provenance constraints.

The required Aspire baseline attempt, `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --isolated` from FrontComposer, failed with 26 CS0234/RZ10012 errors caused by absent nested Tenants/Parties UI projects. No nested submodules were initialized; the narrow Debug build and browser lanes above passed independently.

## File List

Changes during the 2026-10-08 resumed run only; FrontComposer owns its three nested files. No production file or dependency gitlink changed.

- `_bmad-output/implementation-artifacts/spec-frontcomposer-tab-contract.md`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`
- `references/Hexalith.FrontComposer/docs/reference/components/page-tabs.md`
- `references/Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageTabsTests.cs`
- `references/Hexalith.FrontComposer/tests/e2e/specs/page-toolbar.spec.ts`
