# Navigation accessibility qualification — 2026-10-23

This batch resolves DW-119, DW-211, DW-212, DW-213 and DW-444 over Tenants entry `48b0cd98be5ff75402e1206e083e561552146454`. FrontComposer entry `9b8cbf28c621cbf45429eab8db1a50cfc4bf1c38` advances through fetched public main `2d92bb1a1061e1fecb7d87c9fb450f39698326f6` to owning header commit `adc0dbe2228d6291df8b1fe5ba1dc8fb0080f631`. Linear rebases integrate 25 already-published upstream commits; their full IDs are retained in the test-results JSON. This batch's shared commit changes only its three header source/test/documentation files. The earlier local endpoints `25b620001c1fe9bbb5171e823338d6ee1733d13f` and `376076a3b7f0e98f4e144d62120d5cdfd184e1b7` are historical provenance. It is the only dependency pointer changed. The original deferred findings, duplicate decisions and historical verification fields remain in the ledger.

## Behavior and evidence

Paging moves focus from the activated pager to the persistent list heading before replacing the controls. After a current terminal render it restores an enabled originating button only while focus remains on that heading. Missing/disabled origins retain heading focus; a surviving control keeps focus. Cursor/history mutation follows the focus handoff so Previous cannot disable itself prematurely. An immutable operation version rejects older render continuations after opposite-direction paging. Held reads, duplicate callbacks and superseding refreshes have component regressions.

The missing-row detail-return component test checks the exact `restoreDetailFocus` target and retained notice. Chromium independently checks actual focus at `tenants-list-heading`, rather than inferring focus from tabindex.

FrontComposer rejects both blank title inputs with a diagnostic naming `PageTitle` and `Heading`. Explicit titles with suppressed headings and heading-derived titles remain supported. `FocusHeadingAsync` refuses a default element reference before browser interop; suppressing a heading clears its reference so restoration uses the newly rendered element. Header and layout regressions qualify these paths.

Audit paging reset captures focus ownership before removing an unsubmitted correction, then schedules origin/fallback recovery after clearing. The browser capture continues observing ownership until actual removal, including initially negative observations and Chromium blur-before-disconnect. Load/open/focus generations reject obsolete probes; submitted/retained attempts remain outside this removal path. Browser tests exercise the production projection-notification callback, invalid-cursor recovery and retained panel removal. Focus returns to the exact grid-origin explanation when role selection has been cleared, to the audit heading when that origin is absent, and stays on Refresh when that surviving control owns it.

## Browser scope and reproduction

The validator launches a separate process from the rebuilt production UI assembly. It completes the real OIDC challenge and code exchange using the existing local realm and registered origin. Controlled loopback REST and SignalR upstreams provide repeatable projection reads and notifications. The fixture does not persist domain data and rejects command requests. The browser fixture preserves app configuration and domain data; local identity-session state follows the recorded AppHost recovery.

This qualifies production UI focus and authentication integration with disclosed test upstreams. It does not qualify backend authorization, real domain projections, deployed delivery guarantees or assistive-technology announcements. The JSON's `authorizedReads` counter means fixture requests carried a bearer header; the test upstream does not perform backend signature/permission validation.

Start/inspect the existing AppHost through Aspire. Choose the documented credentials before starting a non-persistent realm, keep their values private, and discover the current UI HTTPS endpoint and `executable.pid` with:

```bash
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
```

Pass `TENANTS_BROWSER_BASE_URL`, `TENANTS_BROWSER_SOURCE_UI_PID`, `TENANTS_BROWSER_USERNAME` and `TENANTS_BROWSER_PASSWORD` through the process environment, then run:

```bash
node tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-navigation-focus.mjs /tmp/navigation-results.json
```

The harness requires the registered `localhost` origin, Linux `/proc` and uses the pinned Playwright install from `tests/performance/tenant-audit` (`npm ci --prefix tests/performance/tenant-audit`), or the existing module selected by `TENANTS_BROWSER_PLAYWRIGHT_MODULE`. `CHROMIUM_BIN` can select the installed browser. It first performs a normal audit-enabled Debug source build, then records source/validator/assembly hashes, browser versions and actual deepest focus, without retaining credentials, bearer tokens, cookies, session state or network bodies. Temporary UI logs are private.

The accepted `dw-119-211-212-213-444-browser-results.json` contains 24 passing cases with 190 bearer-bearing fixture reads and eight actual Memories search reads, zero command attempts and zero circuit errors. Both paging directions in ordinary, protected-search and fallback modes, enabled/unavailable origins, surviving focus, missing-row return and grid/receipt start/preview reset outcomes are mandatory. Protected-search and fallback modes assert their actual authority text and search-read counters. Earlier nine-case runs are superseded; the final 24-case packet builds current source and matches the UI assembly bytes used by the full test suite.

## Build and component checks

Normal audit-enabled source builds pass with zero warnings/errors:

```bash
# Tenants root
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
# FrontComposer root
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Debug -m:1 -p:UseHexalithProjectReferences=true -v:q
```

Initial implementation builds also passed with `-p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0`; acceptance does not depend on those pins. Focused commands invoke the built xUnit assembly directly:

```bash
# Tenants root
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.TenantsWorkspaceTests
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.Components.TenantAuditPageTests
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -result-xml /tmp/tenants-navigation-final-ui.xml
# FrontComposer root
dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class 'Hexalith.FrontComposer.Shell.Tests.Components.Layout.*'
dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method Hexalith.FrontComposer.Shell.Tests.Governance.AnalyzerPolicyGovernanceTests.AnalyzerPolicy_IdentifierInventory_MatchesSeal
```

Workspace: 150 passed. Audit: 266 passed. Header: 23 passed, included in the complete integrated layout group of 341 passes. The exact identifier-inventory guard passes. Every listed focused group has zero failures or skips. Initial full UI: 7,503 passes before the final missing-row case; that additional case passed in the final workspace group. After review corrections and rebase, final normal-build full UI passes **7,514/7,514** with zero failures, errors, skips or not-run cases (125.398 seconds). Full-source results and raw-log hashes are retained in `dw-119-211-212-213-444-test-results.json`.

The maintained Story Guards Chromium lane also passes:

```bash
TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
```

Its controlled DOM fixtures directly exercise the new exports with Fluent shadow descendants, enabled/disabled/missing pager origins, surviving controls, both panel selectors and ownership gained after an initially negative capture. This routine CI coverage complements the authenticated production-component checks; bUnit mock invocation counts alone do not qualify DOM focus.

## Preserved failures and broad-gate limits

`dw-119-211-212-213-444-initial-browser-failures.json` retains four rejected fixture runs. Their causes were the separate process's uninitialized token relay store, a production subscription tenant mismatch, an expectation for a Start button that is intentionally replaced by its focusable explanation, and an inconsistent fixture cursor/HasMore combination. The corrected fixture completes its own OIDC exchange, follows the authenticated tenant in memory, requires the exact origin explanation, and supplies consistent paging shapes. None of the failed attempts is accepted as proof.

The broad FrontComposer command was:

```bash
dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll
```

Before the upstream integration, it completed 3,768 tests with 15 failures and one skip. One failure was introduced by four new underscore-named tests changing a sealed CA1707 inventory; those names were corrected to PascalCase without changing the seal, and the exact inventory guard subsequently passes. Fourteen other cases were not qualified by that broad run; their full names are retained in the test-results JSON. Diagnostics include missing nested Builds `Props/Directory.Packages.props` and AI.Tools commit trees, unavailable historical release action/lineage data, incompatible changelog-writer tooling, and `NETSDK1004` for unrelated sample/test projects lacking restore assets. The later owning `npm ci --ignore-scripts` restored pinned commitlint/tooling without modifying the lockfile; no fresh broad-green claim is made.

The skipped live two-tenant adapter test reports unconfigured live endpoints, identities and bearer tokens. No nested submodule was initialized and no gate, fixture seal, release metadata or skip policy was weakened. Seven Pact files generated by the broad suite were restored to their entry bytes; only the intended three FrontComposer source/test/documentation files were committed. Focused layout, inventory and production-browser evidence are recorded separately from this broad failure. This work does not establish release readiness.

The first final full-UI run executed 7,504 cases with one failure in the unchanged `Cancellation_after_late_backend_setup_cleans_up_without_retaining_a_callback` test: it observed no `UnsubscribeAsync` call immediately after the expected cancellation exception. Its unchanged service starts `CleanupFailedSetupAsync` without awaiting it before rethrowing, while the test does not await cleanup completion. The complete subscription class then passed 37/37 in isolation. The failed full-run XML/log hashes and exact failing test remain in the test-results JSON; no assertion or runtime cleanup policy was weakened. The pre-review full recheck passes all 7,504 cases, and the final post-review run passes 7,514. Both are recorded separately rather than replacing that failed evidence. The deterministic cancellation-test synchronization follow-up remains unnumbered in the ledger; it does not change the five numbered closures.

## Reconciliation and repository checks

```bash
python3 _bmad-output/implementation-artifacts/dw-119-211-212-213-444-validate-ledger.py
python3 -O _bmad-output/implementation-artifacts/dw-119-211-212-213-444-validate-ledger.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-119-211-212-213-444-navigation-accessibility.md
node --check tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-navigation-focus.mjs
node --check src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js
git diff --check
```

The ledger validator defaults to comparing saved evidence without writing it; `--write-result` explicitly regenerates after intentional changes. It verifies exactly five closures, all 657 IDs, 570 done/87 open, 80 exact-covering index rows, original field multiplicity, unchanged unrelated records/rows and the unchanged archive. Current disposition totals are recomputed, correcting stale summary counts without rewriting historical closure notes.

FrontComposer's exact commit candidate passed its own locked/installed commitlint 21.2.2 with zero problems/warnings; validation output is retained in the test-results JSON. All three independent review layers reported before triage. Fourteen findings were retained individually and patched, covering focus races, direction/mode/panel coverage, routine CI coverage, source-bound builds and harness resource shutdown. Final artifact checks and reviewer rechecks precede the authorized pushes. Tenants' exact candidate also passes pinned commitlint 21.2.3. The committed-event guard is run against the completed local commit before pushing; its output is retained privately rather than creating a self-referential evidence commit.

## Harness failure controls

The temporary Node preload intercepts only the harness's separate UI launch: one mode substitutes a nonexistent executable and another terminates that child by signal immediately after spawn. A third run supplies a directory as the evidence path. Each must report failure promptly, and recorded UI child PIDs must no longer exist. The exact temporary replay command is `python3 /tmp/dw-navigation-review-harness-faults.py`; its source uses `node --import /tmp/dw-navigation-review-fault-preload.mjs` with `NAVIGATION_FAULT_MODE` and `NAVIGATION_PID_FILE`, plus the same private browser credentials. These injected runs test cleanup and are excluded from accepted focus evidence. The final replay output and source hashes are retained in the test-results JSON.

The final source-bound validator uses no audit/version fallback pins. Newly added C# tests use PascalCase, and newly added shared declarations use Allman braces; unrelated legacy text is retained. No independent review finding changes the frozen approved intent.

## Final correction recheck

Blind Hunter's recheck found one remaining coexistence problem: the capture originally waited for every matching panel to disappear, although tenant and global correction panels can coexist. The final patch tracks the particular focus-owning panel and preserves that owner's removal while another panel survives. Six real Chromium controls cover both panel directions, initial and late ownership, and focus surviving in the other panel. Blind Hunter independently reran all six against the current module and confirmed resolution. The verification and cleanup reviewers independently confirm their original findings are resolved, with no remaining concrete finding.

Only the pre-existing cancellation-test synchronization issue is deferred. The normal Chromium lane and authenticated validator are rerun after the final two-file DOM patch; the final result packet replaces the preceding browser packet while earlier rejected fixture observations remain preserved.

## Local identity-resource recovery

The final authenticated rerun initially failed during sign-in with zero fixture reads. Aspire reported `security` Exited; its ready endpoint refused connections. `aspire resource security-wmyymzgk restart --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` reported success, but two `aspire wait security --timeout 55` attempts timed out. The AppHost log explicitly records that container `d14cea0878dd` was missing and its delete request returned NotFound; this does not establish an OOM cause.

Restarting the owning AppHost with `aspire start --no-build --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --non-interactive`, supplied the same chosen credentials privately, restored healthy `security` and `tenants-ui` resources. No tracked configuration or stored domain state was repaired. The rejected zero-read packet is retained in `dw-119-211-212-213-444-idp-unavailable.json`; only the post-recovery authenticated run is accepted for the final source.

The final publication rebase integrates three further public commits over the earlier 22. Its only additional runtime-source change is a generated-command disposal guard; the owned header source/tests/docs remain byte-identical to the reviewed formatting endpoint `b3a02724278e4027eb88a00da331df6b5dbc3deb`. Fresh normal builds, 341 layout cases and the source-building integrated UI/browser checks qualify this base before Tenants publishes the fixed pointer.

## Final acceptance

The current-public-base source has zero-warning/error normal builds, 341 layout passes, 416 focused UI passes and **7,514 full UI passes** (125.398 seconds; zero failures/errors/skips/not-run). The complete normal Chromium lane passes with all six new coexistence controls. After the second local identity-resource recovery, the final authenticated packet passes **24/24**, records **190** bearer-bearing fixture reads and **eight** actual Memories reads, and has **zero** command attempts or circuit errors. Its normal Debug build precedes fingerprint collection. Validator/UI/Shell hashes match current files, and the tested UI dependency bytes equal the browser-tested UI assembly bytes.

The shared fix is published on FrontComposer main at `adc0dbe2228d6291df8b1fe5ba1dc8fb0080f631`. Both rejected zero-read sign-in attempts remain separate artifacts. All independent review findings are resolved; only the documented pre-existing cancellation-test timing follow-up is newly deferred. Broad Shell and release-readiness limitations remain as recorded. The five numbered closures preserve all original fields and exactly cover the remaining 87 open IDs in 80 index rows.
