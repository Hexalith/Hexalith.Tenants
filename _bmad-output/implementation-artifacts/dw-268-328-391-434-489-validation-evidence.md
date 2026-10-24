# Five deferred UI runtime records — 2026-10-24

The batch closes DW-268, DW-328, DW-391, DW-434 and DW-489 over canonical Tenants baseline `3808cd73f6560d035c417109d27d0a2b3365992b`. The reviewed Debug/source UI binary is identical in all three accepted browser packets and the full test output. No dependency pointer or public API changes.

## Result and bounded evidence

- **DW-268:** Four native CSS-isolation owners activate the original five surfaces' Fluent-host rules; receipt reuses its semantic section. The grid rule targets only its outer table, preserving header widths. The audit-entrypoint owner uses `display: contents`, preserving Fluent layout. Computed styles cover available/unavailable roots, actual recovery-region width, five widths, forced colors and reduced motion. Removing/restoring owner scope attributes proves the rules depend on the intended owner.
- **DW-328:** A fresh [independent configuration-removal review](dw-328-independent-review.md) traces expiry, late results, ownership and event-backed ordered absence proof. Subsequent admission fixes preserve those contracts and pass the new regressions and full suite.
- **DW-391:** The real authenticated administrator page qualifies grant/removal native Tab/Shift+Tab, inert background, Escape restoration, complete preview facts, supported 256-character identities, short/mobile layouts and forced-color focus. [Final normal evidence](dw-391-authenticated-browser-evidence.json) passes 9/9.
- **DW-434:** Matching retained set ownership can mount before re-adopting its exact lease. A shared injected TimeProvider makes expiry, heading focus, replacement deadlines, scope changes and disposal deterministic. Exact-own admission bookkeeping preserves only already confirmed lifecycle/removal inputs; authority, tenant, version, support, viewport, confirmation and generation changes still refuse. [Final normal evidence](dw-434-browser-acceptance.json) passes 7/7: all three collapsed flows in Accepted and RequestSent plus terminal heading/notice fallback. Six logical synthetic attempts produce seven raw requests: only lifecycle RequestSent follows its existing identical-message/payload redelivery contract. No domain write or forwarding occurs.
- **DW-489:** The composed audit grid, receipt, start and preview qualify exact fixture values and readable bounds at 390/767/768/1024/1366px and 400px height. Native scrolling reaches the grid's final column, Cancel remains reachable, and unsafe narrow correction stays read-only. [Layout and scoped-style evidence](dw-268-489-authenticated-browser-evidence.json) passes 17/17 with 129 bearer-bearing fixture reads, zero commands and zero circuit errors.

All **33 browser cases** pass normal source builds with stable before/after source and validator fingerprints. Debug/reused-build packets explicitly cannot qualify closure. Source-change failure controls reject before browser startup; child-start and evidence-write controls qualify cleanup. Exact commands, counters and hashes are in [test results](dw-268-328-391-434-489-test-results.json), the [DW-434 command record](dw-434-validation-commands.json), and the administrator fault/source-control packets.

## Current validation

| Check | Result |
| --- | --- |
| Final combined Debug/source UI.Tests build | zero warnings/errors; 14.26 seconds |
| New ownership/expiry/return regressions | 43/43; zero failures/skips |
| Earlier broad focused UI classes | 1379/1379; zero failures/skips |
| Final full UI project |**7592/7592**; zero failures/skips |
| Normal authenticated browsers |17+9+7; zero circuit errors/domain writes |
| Documentation-only test rebuild | zero warnings/errors; 17.77 seconds; assertions/runtime unchanged |
| New C# whitespace verification |exit0; workspace-loading warnings are retained separately |
| Pinned commitlint 21.2.3 |exact full candidate: zero problems/warnings |

Final full command:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore -p:UseNuGetDeps=false -- --report-xunit-trx --results-directory /tmp/tenants-dw-final-full-ui
```

The three validators run with `TENANTS_BROWSER_BASE_URL`, `TENANTS_BROWSER_SOURCE_UI_PID`, the chosen local realm username/password, an installed Playwright module and optional `CHROMIUM_BIN`. Actual invocations use the safe runner retained in the raw-validation archive; its private input JSON and all credentials/auth protocol logs are excluded. Each validator rebuilds current Debug/source UI and rejects source or validator changes during the build/run. Real OIDC authenticates the production UI; controlled upstream responses qualify UI behavior and do **not** qualify backend authorization, deployed domain state, package/Release mode or release readiness.

## Preserved failures and remaining qualification

The initial full run had 7547 passed/two failed: the intentionally changed entrypoint parent required an updated exact shared-shell assertion, and an unchanged cancellation-coordination test timed out. Both subsequently pass the focused 2/2 recheck; final full acceptance is recorded separately. Development evidence also retains the zero-selection filter exit 8, two nullable test compilation errors, an initially incorrect failed-submission stub, and browser expectation/interaction-readiness mistakes. No test was disabled, gate weakened or timeout raised to obtain acceptance.

The local security container disappeared during final sign-in attempts; Docker inspect could no longer find it. Restarting the existing AppHost without a build restored healthy identity/UI resources and privately refreshed local credentials. Zero-read rejected authentication packets remain separate; no OOM cause, tracked configuration repair or deployment is claimed.

One frozen-source administrator Escape check detached the modal but observed body focus for twenty seconds. The later instrumented three-case trace and final standalone normal nine-case run pass. The cause remains unproven: [safe diagnostic](dw-391-focus-diagnostic.json) and [failed packet](dw-391-final-grant-focus-unavailable.json) remain preserved. An **unnumbered unverified-medium follow-up** requests controlled foreground/subscription ordering before any production focus patch. No browser-concurrency cause is claimed.

## Review and reconciliation

Blind Hunter reported ten findings; all were checked and patched: expiry focus, replacement wake-up, RequestSent coverage, deterministic clock, unavailable branch, media selectors, actual containing width, exact field values, before-build source binding and exact current ledger fields. Its independent recheck establishes no additional reachable authority, scope, version, confirmation, generation or admission-ownership bypass. The extra unexplained focus observation is deferred with its disambiguating evidence; the original ten findings are resolved.

Exactly five numbered records close. All 657 identifiers and original field multiplicity remain; 590 are done and 67 remain open in 62 exact-covering maintenance rows. Unrelated numbered bodies/rows, archive, sprint state, dependency pointers and broader product/runtime/release holds are preserved. Unnumbered follow-ups are explicitly excluded from those numbered totals.

```bash
python3 _bmad-output/implementation-artifacts/dw-268-328-391-434-489-validate-ledger.py
python3 -O _bmad-output/implementation-artifacts/dw-268-328-391-434-489-validate-ledger.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-268-328-391-434-489-ui-runtime-qualification.md
git diff --check
```

## Remote main integration

The first `git push origin main` failed with a remote index-pack/unpack error while remote main advanced to `9c343c2d36d6d291ffea60928b6377013d8fcc84`. A linear rebase preserves that user build-default fix. Both the complete UI and test source trees are byte-identical as Git trees before/after integration. Explicit Debug/source evaluation still selects FrontComposer and Memories projects; the new default condition does not change that tested mode. A fresh no-restore build has zero warnings/errors and the new 43 cases pass 43/43. [Exact integration commands, tree IDs and logs](dw-268-328-391-434-489-main-integration.json) qualify this later step. The rebuilt binary embeds a new revision in informational metadata; original full 7592/browser 33 packets remain immutable and bound to their original tested revision/binary. No new browser or Release run is claimed for the integration.
