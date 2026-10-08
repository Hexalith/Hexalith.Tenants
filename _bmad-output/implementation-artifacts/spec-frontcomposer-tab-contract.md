---
title: 'FrontComposer explicit page-tab panel contract'
type: 'bugfix'
created: '2026-08-28'
status: done
baseline_revision: 'b5d2734f1774923c5f4334b898653cfc49abf369'
baseline_commit: 'e3bfdcc8cd3b3d9769e24f461b812d0a4f96cfcf'
acceptance_refresh_baseline_commit: 'b5821e2d1c3fe11c33631f562d07fa10b7bd773a'
historical_baseline_commit: 'b011873a2cae73718f5054a83569ea6ac92e3bdb'
implementation_baseline_commit: '5a3bc6dd4c1aa77cec50d0a960e986aa89c08cbe'
resumed_revision: '94c6a89c01bedce23cbeacfae5dae3b4c49cd2fa'
resumed_frontcomposer_revision: 'c561b3210f15206a90c39c82c58f2e5b1005cd60'
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

**Always:** Keep Fluent UI v5 responsible for tab semantics, roving focus, and selection; let its pinned `${tabId}-panel` convention create the association from actual `FluentTab.ChildContent`. Preserve `tenants|users` URL/state behavior, stable selectors, lazy surface loading, localized labels, and all existing authorization/freshness/create-command gates. Work in the owning FrontComposer and Tenants repositories and declare both repositories' changed files. Retain the current Tenants root-declared `references/Hexalith.Builds` gitlink at `893db14b25843db140942d839e4d659584221315`; its central catalog aligns `xunit.v3`, `xunit.v3.assert`, and `xunit.v3.extensibility.core` at `4.0.1`.

**Block If:** The implementation cannot keep full page-body content outside `FcPageHeader.Actions`, requires changing a fail-closed Tenants rule or the approved tab route semantics, or cannot restore the FrontComposer test graph with the retained central catalog without a dependency change or restore-policy bypass.

**Never:** Edit `_bmad-output/implementation-artifacts/deferred-work.md`; override `aria-controls`/panel ids through `FluentTab.AdditionalAttributes`; put grids, states, or command flows inside the page header; hand-roll raw tab controls; eagerly issue hidden-panel gateway requests; change dependency versions or gitlink revisions; add local FrontComposer or Tenants package-version overrides; disable central package management or transitive pinning; use a restore-policy overlay to bypass the graph; change backend contracts or the deferred ledger.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Initial workspace | `/tenants` | Tenants is active; its generated panel contains all tenant controls/states/body/pager; Users is inactive and lazy | No empty or missing controlled panel |
| Keyboard switch | Focused horizontal tab; ArrowRight/ArrowLeft/Home/End | Fluent moves focus/selection, toggles the matching panel, and the callback updates canonical workspace state | Disabled tabs are skipped; wrapping follows pinned Fluent behavior |
| Direct/invalid route | `tab=users` or unknown `tab` | Users loads without a tenant-list request; unknown values normalize to Tenants | Existing support-safe/fail-closed states remain authoritative |
| Unsafe create evidence | stale, ambiguous Unknown, non-empty Unknown, or disconnected command surface | Tenant create remains disabled | Only authoritative first-tenant empty Unknown retains the documented exception |
| FrontComposer test restore | Retained Tenants root gitlink `references/Hexalith.Builds` at `893db14b25843db140942d839e4d659584221315` | `xunit.v3`, `xunit.v3.assert`, and `xunit.v3.extensibility.core` resolve together at `4.0.1`, and the exact Shell.Tests build proceeds | Block on any remaining conflict; do not add local overrides, select another dependency revision, or weaken restore policy |

</intent-contract>

## Code Map

- `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageToolbar.razor` and `FcPageToolbarTab.cs` -- current header-only compatibility API; do not place workspace body content here.
- `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcAggregateListPage.razor` -- page header plus body slots; body-level tabs belong after the header, not in `Toolbar`.
- `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageTabs.*` and `FcPageTab.*` -- new shared wrapper/child contract; expose active id/callback, accessible/test labels, disabled/icon/deferred-loading options, and real panel content.
- `references/Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageTabsTests.cs` -- deterministic component, `${id}-panel`, content, lazy-loading, and callback coverage.
- `references/Hexalith.FrontComposer/samples/Counter/Counter.Specimens/FrontComposerPageToolbarSpecimen.razor` plus `tests/e2e/{page-objects/page-toolbar-specimen.page.ts,specs/page-toolbar.spec.ts}` -- browser-owned Arrow/Home/End focus, selected state, visibility, and reciprocal association proof.
- `references/Hexalith.FrontComposer/docs/reference/components/{index.md,page-tabs.md}` -- adopter contract and warning against external sibling panels.
- `references/Hexalith.Builds` -- retain the Tenants-owned root gitlink at `893db14b25843db140942d839e4d659584221315` and its xUnit-family alignment at `4.0.1`; do not edit the Builds repository in this story.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:12` -- migrate the complete Tenants and Users surfaces into lazy `FcPageTab` panels; keep state methods at lines 1189-1348 unchanged except composition-required mapping.
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs:88` and `Components/TenantListSurfaceTests.cs:359` -- initial/changed selection, non-empty associations, direct Users/invalid route, preserved query, and no cursor leakage.
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs:565` -- read-only regression locks for the first-tenant exception and fail-closed stale/ambiguous/disconnected cases.

## Tasks & Acceptance

**Execution:**
- [x] Tenants dependency gitlink -- retain `references/Hexalith.Builds` at `893db14b25843db140942d839e4d659584221315`, verify aligned xUnit `4.0.1`, and make no package edits or dependency-pointer changes. Human approved retaining current versions and refreshing the stale acceptance/provenance records.
- [x] FrontComposer layout source -- implement the additive body-level tab/panel API with XML docs and Fluent child content; keep each C# type in its own file. Already present at the resumed FrontComposer revision; verified without production changes.
- [x] FrontComposer component/specimen/docs/e2e files -- prove deterministic association and real Chromium keyboard behavior; document the derived panel id and body-placement rule.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- recompose existing fragments under their owning panels without changing gateway, route, support-safety, or command-admission logic. Already present at the resumed Tenants revision; preserved and verified.
- [x] Tenants UI tests -- replace presence-only/raw-component coupling with shared-contract association and transition evidence; retain all create/freshness locks.

**Acceptance Criteria:**
- Given the FrontComposer Shell.Tests verification lane, when its project restores through the Tenants root dependency graph, then `references/Hexalith.Builds` remains at `893db14b25843db140942d839e4d659584221315`, the three xUnit v3 packages resolve at `4.0.1`, and no local package override or restore-policy bypass is present.
- Given any enabled `FcPageTab`, when rendered, then its Fluent tab controls exactly one `${id}-panel` with `role=tabpanel` and caller-owned non-empty content.
- Given the browser specimen on Summary, when ArrowRight, End, Home, and reverse/wrap transitions run, then focus, `aria-selected`, active state, and visible panel stay synchronized.
- Given `/tenants` or `?tab=users`, when selection changes, then only the selected surface is active, canonical state and prior tenant query context are preserved, and no foreign cursor or eager gateway request crosses panels.
- Given stale, ambiguous, non-empty Unknown, or disconnected command evidence after migration, when create availability is evaluated, then it remains disabled; authoritative empty Unknown remains the sole bootstrap exception.

## Spec Change Log

- 2026-08-28: Human resolution approved only the exact Tenants `Hexalith.Builds` gitlink advance to `fd606d51826a8282cacecace965ed502461a2e33` so the required xUnit test graph aligns at `4.0.0`; all other dependency changes and restore bypasses remain forbidden.
- 2026-10-08: Resumed from the recorded revisions above. The production contract and workspace migration were already present; strengthened component/browser association, transition, lazy-loading, header-placement, query, and cursor evidence. Preserved the original dependency constraint and baseline while recording the current-state mismatch for human resolution.
- 2026-10-08: Human approved retaining current Builds/xUnit `4.0.1` and refreshing the outdated acceptance and gitlink records. Updated current-state constraints to the retained Builds revision and set the review baseline to the current committed Tenants tree, preserving the former value as `historical_baseline_commit`. The intervening HEAD amendment changed only the committed Builds pointer; the current Builds catalog is byte-identical to the previously tested catalog. No dependency or production changes were made by this story.
- 2026-10-08: Concurrent workspace work committed the EventStore `3.117.0` upgrade and the final FrontComposer documentation correction. Retained the resulting Builds revision `893db14b25843db140942d839e4d659584221315` with unchanged xUnit `4.0.1`; refreshed the forward gitlink guard to the new committed Tenants baseline and reverified the affected graph. Earlier baseline identifiers and verification remain recorded below.
- 2026-10-08: Provenance record for the original implementation window, `b5d2734f1774923c5f4334b898653cfc49abf369` through `eb965727329c7d7335be4cd341db4e2f9bf57b56`. That window predates every `baseline_commit` recorded above, so no guard run in this file covers it, and none of its three root gitlink moves was declared at the time. `references/Hexalith.FrontComposer` moved from `b6ec1ccb72b4e9a467e8b02f0567a4b7d59e1e09` to `61c0525604204509a146856ac400a3095058a63c`; this move is story-owned because it carries FrontComposer `1ca5a5dbd9273035b001b6b37c9e88c9dfd104f3` (the page-tabs contract). `references/Hexalith.Builds` moved from `c8837217e6c07f7e12ccf3e3b5e86c5bc83ceade` to `fd606d51826a8282cacecace965ed502461a2e33`. Only the hop from `9aca670aa9d4605bb147f641ef23d30d37813e92` to `fd606d51826a8282cacecace965ed502461a2e33` in `eb965727329c7d7335be4cd341db4e2f9bf57b56` was approved. The earlier hop from `c8837217e6c07f7e12ccf3e3b5e86c5bc83ceade` to `9aca670aa9d4605bb147f641ef23d30d37813e92` came from `498eee33de9ae6b01f8e5ff46a004db3b3fa0c31`. `references/Hexalith.EventStore` moved from `33e630bf5bd459678c555b167cf86b08908de12b` to `f18fbf113e1ccfb41d330a3e4aecb913c16bc6de`, also in `498eee33de9ae6b01f8e5ff46a004db3b3fa0c31`, and this spec never sanctioned that move. These pointers are recorded here as non-binding history rather than as File List items: a path-level File List declaration would mark any later movement of these pointers as declared against the current baseline. An independent pre-strengthening verification on the clean tree at `94c6a89c01bedce23cbeacfae5dae3b4c49cd2fa` (Builds `870bd6b85ec5cd841da6fbcc7dbc71b8e4ad764b`) also passed: both Debug builds reported 0 warnings and 0 errors, `FcPageTabsTests` passed 22/22, Chromium `page-toolbar` passed 7/7, and the full UI executable passed 3,986/3,986.

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
|---|---|---|
| Blind 1: panel-label ownership in the adopter docs | low | `FcPageTabs.OnAfterRenderAsync` invokes FrontComposer's `fc-focus.js:labelTabPanels`, which supplies `aria-labelledby`; the new paragraph incorrectly attributes this to Fluent. Patched the attribution to identify FrontComposer interop while retaining Fluent ownership of focus, selection, and visibility; the file-scoped diff check passed. |
| Blind 2: non-empty-content acceptance versus deferred panels | low | The broad acceptance wording omits the first-activation exception already required by the lazy-loading constraint and matrix. The test correctly keeps an enabled, never-activated panel's content absent. Rejected as a spec-only wording finding; no behavior change is needed. |
| Blind 3: missing current gitlink-check result | low | The refreshed guard passed with its mid-story-baseline warning; the artifact retained historical failure evidence but had not yet recorded the final result. Rejected as a spec-only finding; record the current result as planned completion evidence. |
| Blind 4: static retention fixture does not establish component/state retention | false | `Workspace_tab_round_trip_retains_the_once_loaded_users_panel_instance` checks both real grid/Users component identities, retains user id/sort/cursor values, and exercises the complete Fluent-driven round trip. It passed in the full UI suite; the static shared fixture supplements this existing stateful integration evidence. |
| Blind 5: missing sequential Tab/Shift+Tab coverage | false | The cited change adds native ArrowLeft/ArrowRight/Home/End/wrap/disabled transitions and does not alter sequential focus behavior or remove its verification. Every transition required by the matrix ran with focus, selected state, and visible-panel assertions. The reviewer proposed additional native-library scenarios without demonstrating a defect in the changed code. |
| Blind 6: Empty-snapshot test does not establish every conditional surface's placement | false | `TenantsWorkspace` owns the entire controls/state/grid/pager branch inside the same `FcPageTab.ChildContent`; the header contains no such branch. Existing state and pager tests passed in the full UI suite, and the new assertions additionally check body/header separation. No conditional surface escaped the owning panel. |
| Blind 7: shared-callback tests bypass the wrapper's query-preservation handler | false | The existing Fluent-driven instance-retention round trip asserts query-bearing tenant and user destinations. `FcPageTabs_AdopterNavigation_PreservesPanelQuery` separately drives the Fluent callback and asserts that the wrapper preserves the adopter's query URL. Both passed; the changed tests add consumer-boundary assertions without removing wrapper integration coverage. |
| Edge 1: enabled deferred Activity has no content before activation | low | Confirmed the deliberate first-activation exception: the panel wrapper and control association exist while lazy content is absent. The I/O matrix requires inactive Users to remain lazy. Rejected as the same spec-only wording issue reported by Blind 2; no production defect. |

The verification-gap review reported no gaps. All three review layers completed; no intent gap, bad-spec loopback, or deferred-ledger change was required.

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

The verification above initially used Builds `870bd6b85ec5cd841da6fbcc7dbc71b8e4ad764b`. At the first acceptance refresh, HEAD retained Builds `ad52c5bdd4361c59eedf12a16620150006403584`; the two revisions differ only in `Tools/package-version-audit.json`, and the complete central package catalog and build configuration are unchanged. All three xUnit v3 packages resolve at `4.0.1`. Human approved retaining these versions and refreshing acceptance/provenance, superseding the older exact dependency requirement. The prior approved dependency advance remains recorded in commit `eb965727329c7d7335be4cd341db4e2f9bf57b56`; the former baseline remains in `historical_baseline_commit` for historical context.

The first refreshed guard baseline was `b5821e2d1c3fe11c33631f562d07fa10b7bd773a` (retained as `acceptance_refresh_baseline_commit`). During this records correction, concurrent workspace activity committed the previously verified Tenants test/spec changes and advanced EventStore/FrontComposer pointers; those operations were not performed by this build. EventStore changed only unrelated test/evidence files, and FrontComposer committed exactly the verified three-file diff, so the tested production graph is unchanged. Review includes the current spec diff, the already-committed Tenants test changes from `implementation_baseline_commit`, and all three FrontComposer changes from `resumed_frontcomposer_revision`. The historical gitlink-check failure above remains recorded as pre-resolution evidence; the refreshed guard checks for any further pointer movement without attributing concurrent committed updates to this build.

The required Aspire baseline attempt, `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --isolated` from FrontComposer, failed with 26 CS0234/RZ10012 errors caused by absent nested Tenants/Parties UI projects. No nested submodules were initialized; the narrow Debug build and browser lanes above passed independently.

**Final acceptance refresh and verification:**

The current committed Tenants baseline is `e3bfdcc8cd3b3d9769e24f461b812d0a4f96cfcf`, with Builds `893db14b25843db140942d839e4d659584221315`, EventStore `0dad344d37343f589d859d6d8d6701283122b338`, and FrontComposer `0e114214007c22f5cdbac21a6853cff4208340ee`. The concurrent commits preserve every tab-contract test/documentation change and are not operations performed by this build. xUnit remains aligned at `4.0.1`; the central EventStore family is now `3.117.0`.

Repeated the affected verification after that catalog change: both exact Debug build commands above passed with 0 warnings/errors; the filtered page-tabs executable passed 27 tests and the full UI executable passed 3,986 tests, each with 0 failures/skips/not-run cases. Logs are in `/tmp/frontcomposer-tab-final-checks-3mav1mcr/{shell-build,page-tabs-tests,ui-build,ui-tests}.log`. TypeScript checking passed and all 7 Chromium tests passed against the newly built Debug Counter specimen, including axe and reciprocal association/keyboard/lazy-retention checks. Their logs are `typecheck.log` and `chromium.log` in the same directory; the owned server was stopped after verification.

The first refreshed gitlink guard passed with a warning that its baseline already contained story files. That warning is expected: this guard detects further pointer movement, while the separately preserved implementation and FrontComposer revisions supply the complete code-review diff. The final guard uses the latest committed baseline and preserves the same division of evidence. Historical failures remain above for audit context.

Completion: all tasks and acceptance checks are satisfied under the human-approved current-dependency constraint. All three review layers completed, the single documentation correction is applied, and no findings were deferred. The original Aspire baseline startup limitation remains recorded above; the required narrow build, component, browser, and complete UI verification lanes passed. The final gitlink and diff checks pass without changing dependency pointers or the deferred ledger.

## File List

Changes during the 2026-10-08 resumed run only; FrontComposer owns its three nested files. No production file or dependency gitlink changed.

- `_bmad-output/implementation-artifacts/spec-frontcomposer-tab-contract.md`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`
- `references/Hexalith.FrontComposer/docs/reference/components/page-tabs.md`
- `references/Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcPageTabsTests.cs`
- `references/Hexalith.FrontComposer/tests/e2e/specs/page-toolbar.spec.ts`
