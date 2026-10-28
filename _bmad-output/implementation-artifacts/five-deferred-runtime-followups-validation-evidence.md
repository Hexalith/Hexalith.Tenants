# Five deferred runtime follow-ups — 2026-10-28

Source: [approved spec](spec-five-deferred-runtime-followups.md). Baseline: `0b07d279aacce430b04b42ee9abd8aa5a6d6dafc`.

All five approved investigations ran. Two unnumbered records close with verified fixes: credential redirect replay and removal submission after rejected focus preparation. Grant Escape, the external grid defect and DW-466 remain open. No numbered status changes: 657 IDs, 639 done and 18 open. [Conservation evidence](evidence/five-runtime-followups/ledger-conservation.json) records all 656 unselected numbered bodies unchanged and every original DW-466 line preserved.

## Outcomes

| Item | Result and limits |
| --- | --- |
| Credential redirects | **Fixed.** Real handlers reproduced synthetic credential-form replay for same/cross-origin 307/308, and empty GET redirects for 301/302/303. A dedicated named credential client now refuses every redirect. Ten no-replay cases and direct-token success use production registration; a hosted-service test checks separate credential/command client selection. Command transport is unchanged. Operators must configure the final trusted token endpoint. The decision was recorded before production edits. |
| Grant Escape | **Bounded negative reproduction; open.** The exact live unscoped global-administrator subscription is held. A negative control rejects completion while held; after release, a scoped notification must produce an authoritative read and distinct DOM row before final launcher-focus checks. This observes callback/render completion, without claiming a private component-lifecycle hook. The historical body-focus cause remains unexplained. |
| Removal focus rejection | **Fixed.** Review-strengthened testing held subsequent focus recovery and reproduced dispatch followed by native Escape closing the modal with body focus. Private focus preparation now reports failure and refuses admission before touching the attempt, lease or dispatch boundary. Component regressions cover first submit and retained ambiguous delivery, preserving snapshot, confirmation and ownership; retry preserves message identity. The fresh browser proves two zero-dispatch refusals, safe Escape/overlay dismissal, then a successful retry with the exact member payload, tenant/domain/aggregate and repeated same-message status lookup. This does not promise recovery from every arbitrary interop failure. |
| Grid navigation | **Reproduced external defect; open.** Controlled initialization asserts connected entry, detail-navigation detachment and release while detached. Pinned `Microsoft.FluentUI.AspNetCore.Components` 5.0.1 throws `ObjectDisposedException` from `FluentDataGrid.UpdateColumnInteractionsAsync` / `OnAfterRenderAsync`, followed by `CircuitHost[111]`. It also occurred without injection in the final full browser lane. No local workaround, fault suppression or speculative upgrade was applied. An owning-component fix and dependency qualification remain required. |
| DW-466 | **Bounded negative reproduction and missing historical evidence; open.** Five controlled executions each require exactly one named passing, unskipped xUnit XML case and two live single-CPU workers with increased CPU time/counters. All five pass. The September report/archive inventory still does not identify the second metadata-confirmation method. No timeout inflation or speculative synchronization edit was made. |

## Final verification

All builds are Debug/source builds. No dependency version or gitlink changes occurred. Logs are exact archive members in [validation-logs.zip](evidence/five-runtime-followups/validation-logs.zip); [log manifest](evidence/five-runtime-followups/validation-log-manifest.json) records bytes and SHA-256.

| Command | Final result / log member |
| --- | --- |
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -m:1 -p:UseNuGetDeps=false` | Passed, zero warnings/errors; `parent-ui-build.log`. |
| `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore` | **7,841/7,841**, zero failed/skipped; `parent-ui-full.log`. Includes the two new refusal/retry regressions. |
| `dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore` | **1,582/1,582**, zero failed/skipped; `parent-server-full.log`. |
| Built Server test assembly, `-noLogo -parallelMode none -class '*TenantBootstrapRedirectTests' -class '*TenantBootstrapHostedServiceTests'` | **141/141**; `server-focused.log`. Real-handler matrix has 21 cases. Initial Server Debug/source build passes with zero warnings/errors in `server-build.log`. |
| Built UI test assembly, `-noLogo -parallelMode none -class '*RemoveTenantMemberFlowTests'` | **260/260**; `five-runtime-focus-fix-class.log`. Direct new regression **2/2** in `five-runtime-focus-fix-regression.log`. |
| Browser command below, focused mode | **4/4**, fresh build, zero circuit errors, source stable through cleanup; `parent-focused-browser.log`. |
| Same browser command without `TENANTS_BROWSER_UI_SAFETY_ONLY=1` | **Failed**, 12 completed cases; one circuit error from the pinned grid disposal fault at create return; `parent-browser.log`. This broad gate was not weakened or reclassified as passing. |
| `python3 _bmad-output/implementation-artifacts/evidence/five-runtime-followups/authorization-load.py` | **5/5** exact named cases with observed CPU contention. [Machine-readable results](evidence/five-runtime-followups/authorization-load.json), `authorization-load.log`. |
| `python3 _bmad-output/implementation-artifacts/evidence/five-runtime-followups/authorization-load-controls.py` | Valid controls accepted; zero/skipped/aborted/wrong/failed/incomplete tests and dead/inactive workers rejected; `parent-load-controls.log`. |

Fresh focused browser command:

```bash
TENANTS_BROWSER_STANDALONE_OIDC=1 TENANTS_BROWSER_RUNTIME_FOLLOWUPS=1 \
TENANTS_BROWSER_UI_SAFETY_ONLY=1 CHROMIUM_BIN=/usr/bin/google-chrome \
node tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-administrator-dialogs.mjs \
  /tmp/five-runtime-parent-focused-browser.json
```

The focused result is a narrower qualification; the runner keeps its full-suite `acceptedForClosure` false in safety-only mode. [Browser packet archive](evidence/five-runtime-followups/browser-packets.zip) members `final/focused-browser.json` and `final/full-browser-blocked.json` retain both outcomes. [Packet manifest](evidence/five-runtime-followups/browser-packet-manifest.json) hashes every member. Synthetic signed loopback OIDC tokens exercise production authentication/components. Only two local command simulations occur, with zero unexpected/forwarded commands. Backend authorization, persisted state, real delivery and assistive-technology announcements are outside this fixture.

## Separate grid diagnostic

```bash
TENANTS_BROWSER_STANDALONE_OIDC=1 TENANTS_BROWSER_HOLD_GRID=1 \
TENANTS_BROWSER_UI_SAFETY_ONLY=1 TENANTS_BROWSER_SKIP_BUILD=1 \
CHROMIUM_BIN=/usr/bin/google-chrome \
node tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-administrator-dialogs.mjs \
  /tmp/five-runtime-parent-grid.json
```

Run sequentially after the fresh focused build. It exits **1**, with two circuit-error observations, fails the unchanged zero-error assertion and is ineligible for closure acceptance. Packet `final/grid-diagnostic.json` records module/validator/assembly hashes, asserted connected/detached ordering, source equality through cleanup and sanitized owning stack. Log: `parent-grid.log`. Production dependency assets remain unchanged; only this browser's module response holds initialization.

## Review, earlier failures and preservation

Three independent review lenses filed thirteen findings. All were triaged individually in the spec, grouped by cause and patched. Strengthening the removal test exposed the actual pre-dispatch bug and superseded its original negative-reproduction claim. A final independent read-only recheck found no remaining concrete issues in the product guard or corrected harness.

[Pre-review evidence](evidence/five-runtime-followups/pre-review-evidence.zip), packet members `pre-review/` and `pre-fix/`, and both diagnostic-attempt archives preserve earlier source/results. Earlier full UI **7,839/7,839** and browser **13/13** passes predate the final removal fix and do not qualify current source. Two later targeted runs reached command-support-unavailable before fixture HTTP; their cause remains unproven. A temporary exception-type-only diagnostic run passed and logged no exception, then the temporary line was removed before final builds. Its packet is retained only under `diagnostic-instrumented/`, never as final acceptance. The uninstrumented fresh focused run subsequently passed. No concurrency or root cause is inferred from passing reruns.

AppHost startup was invoked before code edits. The immediate describe raced pending startup; later path/PID ownership and [resource states](evidence/five-runtime-followups/aspire-resource-states.json) confirm this run's Tenants topology started. It was stopped by its exact AppHost path. These are topology observations, not domain-write acceptance. Historical report/archive [hashes](evidence/five-runtime-followups/historical-evidence.json) remain unchanged. Packets/logs omit bearer tokens, credential form contents, decoded claims and raw cursor/ETag data.

Final hygiene includes the story gitlink guard, diff whitespace checks, archive CRC/member hashes and local Markdown link checks. The exact commit candidate passes pinned `@commitlint/cli` **21.2.3**, zero problems/warnings (`commitlint.log`). Remote divergence is inspected before the authorized push to `main`; the final chat response records the pushed commit.
