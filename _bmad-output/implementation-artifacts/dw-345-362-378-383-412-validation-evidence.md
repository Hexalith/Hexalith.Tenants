# DW-345 / DW-362 / DW-378 / DW-383 / DW-412 validation

Date: 2026-10-22. Entry revision: `ab6e99f656eaae14a01c021cf0c6eadf9f38cefe`.

## Finding and outcome

These five records describe the same unverified risk: Cancel might focus an inner native control while `focusElementById` reports failure because it compares `document.activeElement` with the Blazor FluentButton host. Current production output and restored Fluent **5.0.1** browser code do not exhibit that failure. In Chromium, direct and sentinel-contract calls return true, the exact `FLUENT-BUTTON#tenants-global-admin-remove-cancel-button` owns document focus, and its shadow root has no active element. No production helper change is warranted.

A new two-case component theory opens the real `GlobalAdministratorsPage` removal preview using the existing authorized-composition and read fixtures, the shipped EN/FR resource localizer, and the actual FluentButton component. It exports the production dialog rather than handwriting a matching custom element. It verifies the Cancel ID, enabled state, localized label, real component identity, inert background and zero removal commands. The browser loads that markup, its compiled page CSS and the restored Fluent JavaScript.

The new browser harness verifies six scenarios per culture/viewport: direct Cancel focus, a sentinel-contract shim that must avoid fallback after success, and disabled/hidden/inert/missing targets that must report false and focus the actual acknowledgement input. Traces preserve document and nested shadow active elements. The always-true-without-focus and always-false-after-focusing helper controls are rejected. Independent sentinel-only wrong-result and omitted-fallback controls also fail their own specific assertions. The latter specifically detects a successful focus paired with the wrong boolean result. The existing maintained lane invokes these checks automatically.

## Scope and limitations

This settles the shared host-versus-inner-control helper question for the tested dependency/browser combination. Component authorization and read results are substituted; this is not a live authenticated topology. The sentinel browser listener models the helper/fallback contract; existing component tests cover the actual Blazor callback. This run does not qualify end-to-end callback dispatch, keyboard trapping, screen readers, all browsers, responsive breakpoint boundaries, or release readiness. DW-336/DW-357/DW-359 and DW-391 remain open for broader modal/browser evidence.

The new cases assert measured **500px** and **1024px** viewports. The first experimental run asked full Chrome for a 390px window but measured 500px; that observation led to explicit measured-width assertions. Existing older lane summaries may still label their requested 390px window; those labels are not new exact-width proof.

## Baseline, build and test commands

The Aspire skill inspected the already-running AppHost before changes:

```bash
aspire ps --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --severity Error --limit 10 --format Json
```

All three exit 0; 23 resources report Running/Healthy, and the bounded Error query returns `[]`. Raw environment-bearing diagnostics stay in `/tmp/dw345-aspire-{ps,describe,errors}.json`. The existing app was left running.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
TENANTS_REMOVE_FOCUS_FIXTURE_DIRECTORY=/tmp/dw345-fixtures tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests -method '*GlobalAdministratorsPageTests.RemoveCancelExportsActualFluentControlForBrowserFocus'
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx --minimum-expected-tests 7119 --results-directory /tmp/dw345-full
TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
```

Accepted results:

| Check | Result |
| --- | --- |
| Debug source build | 0 warnings, 0 errors; 34.41s |
| New localized fixture tests | 2 passed; 0 failed/skipped |
| Full UI suite | 7,119 passed; 0 failed/skipped; 2m02.160s |
| Maintained browser lane | Exit 0; Chrome 154.0.8037.57; all existing and new scenarios complete |
| New actual Cancel browser matrix | EN/FR × measured 500/1024px × six focus states pass |
| Harmful helper controls | Four rejected: two direct-result controls, one sentinel-result control and one omitted-fallback control |

Logs: `/tmp/dw345-build-fixed.log`, `/tmp/dw345-fixture-tests-fixed.log`, `/tmp/dw345-full.log`, `/tmp/dw345-browser-final.log`. The full TRX is `/tmp/dw345-full/results.trx`. The tracked [run manifest](dw-345-362-378-383-412-run-manifest.json) preserves browser observations, source/assembly identities, restored Fluent module hash, TRX counters/hash and log hashes. Exported fixture IDs vary per component render; recorded traces bind each browser run to its own actual input control.

## Initial failures and capture-driver correction

1. Initial build fails with CS0103 because the new fixture lacked its `TenantDetail` namespace import. Correcting the import gives the clean build above (`/tmp/dw345-build.log` and `build-fixed.log`). A premature invocation against the old executable discovers zero cases and is excluded from acceptance; the runner also requires both nonempty exported fixture files.
2. The first maintained browser run fails the pre-existing start-sentinel assertion before reaching the new checks (`/tmp/dw345-browser.log`). Isolated production-rendered capture reports direct focus success, but `document.hasFocus()` is false and the sentinel listener never runs. This is absent browser focus-event dispatch, not the hypothesized helper false failure.
3. The shared CDP capture driver now calls `Emulation.setFocusEmulationEnabled` with `enabled: true` before navigation. The new harness requires document foreground focus. The existing start-sentinel case and the production-rendered sentinel-contract case then both pass, while unavailable targets still fall back. The capture changes only the test driver's foreground-focus state.
4. The first foreground-emulated run passes the browser checks but exits 2 at EOF because the executing shell file was edited while the run was active to improve viewport accounting (`/tmp/dw345-browser-foreground.log`). It is excluded as a green lane. A fresh run against unchanged final browser sources exits 0 (`/tmp/dw345-browser-final.log`).

## Ledger conservation and local checks

```bash
python3 _bmad-output/implementation-artifacts/dw-345-362-378-383-412-verify.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-345-362-378-383-412-rendered-cancel-focus.md
bash -n tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
node --check tests/Hexalith.Tenants.UI.Tests/Browser/dump-validation-dom.mjs
git -c core.safecrlf=false diff --check
```

The standalone conservation checker verifies all 657 numeric IDs, rejects three duplicate-heading controls, preserves every original selected field under `pre-cancel-focus-` when updated, preserves all 652 unrelated record bodies, removes exactly the shared selected maintenance row, and proves exact open-record coverage. Final counts: **475 done / 182 open across 172 maintenance rows**. [Stored checker output](dw-345-362-378-383-412-ledger-validation.json) retains the disposition counts. Production focus code, dependency pointers, archive, sprint status and release holds are preserved.

## Commit validation

The exact complete candidate (one LF-terminated line) is:

```text
test(ui): qualify five deferred Cancel focus records
```

```bash
node_modules/.bin/commitlint --verbose < /tmp/dw345-commit-message.txt
git fetch origin main
git rev-list --left-right --count HEAD...origin/main
```

Pinned/installed commitlint **21.2.3** reports:

```text
⧗   --- input ---
test(ui): qualify five deferred Cancel focus records
✔   found 0 problems, 0 warnings
```

The fetched comparison is `0 0` before commit. Review and final synchronization results are appended before publication.

Post-driver focused qualification uses the full UI command above plus `--filter-class '*GlobalAdministratorsPageTests' --minimum-expected-tests 1 --results-directory /tmp/dw345-focused-final`: **276/276 pass**, zero failures/skips, 23.333s. This includes the real Blazor sentinel routing and browser subprocess failure/bound checks against the finalized driver/runner. The run manifest preserves its TRX identity/counters and log hash.

## Review corrections and durable evidence

All nine independent findings are triaged in the owning spec and addressed. The browser now requires literal true on success, verifies the acknowledgement's native shadow INPUT, checks disabled as well as hidden/inert/missing targets, and rejects independent sentinel-result and omitted-fallback controls. The component export refuses a hidden/inert ancestor. The conservation checker rejects wrong/duplicate current dispositions and enforces the exact allowed change paths. The runner emits hashes of its actual EN/FR HTML fixtures, loaded page CSS and runtime UI assembly; these identities are in the run manifest.

Reviewed Debug build passes with zero warnings/errors in 17.79s; localized export passes 2/2. The strengthened maintained Chromium lane exits 0 with all original scenarios, the 24 new focus observations and all four harmful controls passing their expected assertions. Reviewed logs are `dw345-build-review.log`, `dw345-fixture-tests-review.log` and `dw345-browser-review.log` under `/tmp`.

The first reviewed full-suite run exits 2: 7,118 pass / one failure / zero skips. The unchanged `EditTenantMetadataFlowTests.LifecycleRoundTripClosesMetadataAfterProvisionalAdmissionUnwinds(holdSecondAuthority: true)` expects Idle but observes UnableToVerify at line 171. Both cases of that method pass immediately in isolation against the same reviewed assembly. No metadata source or test change is made, and no cause is asserted from this one intermittent observation. The exact failed run and focused recheck are preserved. A fresh full-suite recheck runs separately from the browser lane before acceptance.

The tracked [validation transcript](dw-345-362-378-383-412-validation-transcript.txt) retains the raw command output for the initial failures and accepted/reviewed checks, with repository/NuGet filesystem prefixes replaced by `<repo>` / `<nuget>`. It includes full browser reports and test-run summaries. The complete per-test TRX files remain local; their counters and hashes are retained in the run manifest without claiming that a hash preserves the complete raw TRX.

Independent recheck clears the nine original findings and identifies a shared-dictionary provenance error: updating the reviewed hash map had also changed the nested historical map. The initial source/assembly hashes are restored verbatim from the reviewer's complete retained first-review snapshot, and kept separate from current identities. No old passing TRX is attributed to the reviewed assembly.

During final verification, an unrelated `.mcp.json` edit appears after the clean entry snapshot. The exact-path checker first rejects it. It now pins that observed external file's SHA-256 and refuses to stage it; no other unexpected path is accepted. The task commit excludes `.mcp.json`, whose content is neither reset nor copied into evidence. This qualifies preservation claims in the presence of concurrent external work.

Final reviewed full-suite recheck: **7,119/7,119 pass**, zero failures/skips, **1m57.175s**, exit 0. It uses the maintained full command with `--results-directory /tmp/dw345-full-recheck`; log `/tmp/dw345-full-recheck.log`, TRX `/tmp/dw345-full-recheck/results.trx`. No browser process or build runs concurrently with this accepted recheck. Its raw summary is appended to the tracked transcript, and its TRX counters/hash are the run manifest's current `ui_suite`. The earlier accepted source/assembly map and outcomes remain separately under `initial_accepted_runs`; reviewed current hashes bind only the reviewed runs.

Final independent recheck reports **no remaining findings**: all nine original corrections and the historical-identity fix are accepted; historical/current maps, final TRX counters/hash, durable transcript and unstaged external configuration match disk. Conservation, story gitlinks, syntax and whitespace checks pass. The unchanged full candidate again passes commitlint with zero problems/warnings; final fetch reports 0/0 divergence before commit. No new item is deferred.

Transcript sanitization also trims trailing whitespace to satisfy the repository whitespace gate; original local log hashes remain in the manifest.
