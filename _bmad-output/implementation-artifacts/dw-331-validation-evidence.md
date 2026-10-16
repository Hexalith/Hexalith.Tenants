# DW-331 complete-population scope validation — 2026-10-16

Entry: clean `main`, HEAD `e44ac7f537e9f192605e24efc3e61a97d00d44f3`. The removal evaluator did not inspect `CompleteIsAuthorizationScopedEmpty`, although it is part of its public immutable evidence. The bounded production loader already rejects nonempty authorization-scoped-empty reads. This patch adds the missing population guard without changing the loader or installed consumers.

## Regression evidence

The two new cases were built and executed before the runtime fix. The focused class reported 24 passed, two failed, zero skipped, exit 2: otherwise valid contradictory evidence incorrectly allowed removal; with missing removal preview the same contradiction incorrectly returned `MissingConsequencePreview`. Both must return `IncompletePopulation`. The standalone case also asserts the removal action, unavailable flag, exact existing reason/recovery keys and independent available grant. Existing valid-removal and last-administrator controls remain in the class.

Commands run from the repository root:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GlobalAdministratorActionAvailabilityTests' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/dw-331-red-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
```

The pre-fix build succeeded with zero errors and two warning occurrences for the same existing NU1903 advisory, GHSA-334f-vvp4-7g6p, for pinned SignalR.Common 10.0.12; auditing and the warning policy were retained. Logs: `/tmp/dw-331-red-build.log`, `/tmp/dw-331-red-focused.log`; report: `/tmp/dw-331-red-focused/focused.trx`.

## Pre-edit runtime inspection

The repository Aspire skill supplied these commands:

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire wait tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 45
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

All commands exited 0. The first inspection showed 13 Waiting, nine Running/Healthy and one Running/Unhealthy resources during startup. Final inspection showed all 23 Running/Healthy and the UI readiness wait succeeded. Initial telemetry contained 126 entries; this establishes a qualified runtime baseline, not comprehensive runtime acceptance. Other running Platform and FrontComposer hosts were left untouched; the isolated Tenants validation host was stopped. Raw output is retained under `/tmp/dw-331-aspire-*`; dashboard credentials are omitted from this document.

## Scope and limits

The change is one pure evaluator guard and nine behavioral regression cases after review. There is no component, resource, persistence, deployment or dependency change. Debug source verification is the relevant lane; Release, browser, broad solution, package and backend lanes are outside this patch's footprint and are not rerun. Existing release holds remain in [earlier qualification evidence](dw-337-validation-evidence.md).

The entry hashes and canonical baseline are in `/tmp/dw-331-entry-state.json`; original ledger records are in `/tmp/dw-331-entry-records.json`. Only DW-331 is eligible for closure after passing validation and review. Archive, sprint tracking and unrelated records remain preserved. User-provided repository instructions prohibit automatic staging, committing, pushing, branching or dependency mutation for this task and take precedence over the skill's automatic commit step.

## Post-fix verification

The same Debug source build passes with zero errors and two occurrences of the same existing NU1903 advisory (`/tmp/dw-331-green-build.log`). The focused class passes 26/26, zero failures/skips, including both previously failing cases (`/tmp/dw-331-green-focused.log`, `/tmp/dw-331-green-focused/focused.trx`). The pre-review full UI project passes 5,981/5,981, zero failures/skips, in 1m57s (`/tmp/dw-331-full.log`, `/tmp/dw-331-full/full.trx`). These results precede the seven review-driven precedence cases below.

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GlobalAdministratorActionAvailabilityTests' --minimum-expected-tests 26 --no-ansi --no-progress --results-directory /tmp/dw-331-green-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 5981 --no-ansi --no-progress --results-directory /tmp/dw-331-full --report-xunit-trx --report-xunit-trx-filename full.trx
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-331-complete-population-scope.md
git diff --check
```

Story-gitlink validation passes with no reference pointer movement from the recorded full baseline. Whitespace validation passes; Git reports only its existing checkout CRLF-to-index-LF normalization advisories. Changed C# files retain CRLF on disk.

## Independent review and corrections

The context-free blind reviewer computed `min(floor(sqrt(10.307) + 1), 10) = 4` and returned four findings. All four are accepted and patched: five common-gate precedence cases, two target/count precedence cases, the missing exact full-suite command and accurate duplicate-warning wording. Finding-by-finding verdicts are in [the owning spec](spec-dw-331-complete-population-scope.md). The runtime fix remains a single condition, with no API or policy change. Nothing is deferred.

The following reviewed verification commands were executed; outcomes, including the initial failed full run, follow:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GlobalAdministratorActionAvailabilityTests' --minimum-expected-tests 33 --no-ansi --no-progress --results-directory /tmp/dw-331-reviewed-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 5988 --no-ansi --no-progress --results-directory /tmp/dw-331-reviewed-full --report-xunit-trx --report-xunit-trx-filename full.trx
```

Raw reviewed logs: `/tmp/dw-331-reviewed-build.log`, `/tmp/dw-331-reviewed-focused.log`, `/tmp/dw-331-reviewed-full.log`; TRX reports use the corresponding result directories. Earlier results remain pre-review evidence.

The reviewed build passes with zero errors and the same one advisory emitted twice; focused cases pass 33/33, zero failures/skips. The first reviewed full run exits 2: 5,987 passed, one failed, zero skipped, 1m56s. The failure is `CorrectionStartPanelTests.Expired_retained_attempt_shows_only_available_status_recovery(hasCorrelation: True)` at line 602: expected two status handles, observed one. The component and test are unchanged, neither references the patched global-administrator evaluator, and the pre-review full run passed the same case. This is an observed intermittent failure, not proof of its root cause. The failed run and report are preserved above. Its class is being checked separately before another full run; no unrelated runtime/test patch or gate weakening has been applied.

The unchanged correction class passes separately: 118/118, zero failures/skips, exit 0. The full suite recheck uses the same reviewed assembly and test floor. Exact commands:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*CorrectionStartPanelTests' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/dw-331-correction-recheck --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 5988 --no-ansi --no-progress --results-directory /tmp/dw-331-reviewed-full-recheck --report-xunit-trx --report-xunit-trx-filename full.trx
```

Recheck logs: `/tmp/dw-331-correction-recheck.log`, `/tmp/dw-331-reviewed-full-recheck.log`; TRX reports are in the corresponding results directories. No rebuild or source changes occurred between these runs.

## Final result and closure

The reviewed full recheck passes 5,988/5,988, zero failures/skips, exit 0, in 2m00s. Final focused availability cases pass 33/33; the correction-class recheck passes 118/118. The independent reviewer confirmed the test-coverage and command-evidence corrections and found no new code regression; its remaining warning-wording correction is explicitly recorded as an erratum in the spec's append-only implementation notes. All four review findings were patched; none was deferred.

Only DW-331 closes. Its original finding and pre-closure status/disposition/verification/action are retained in the ledger. All 643 unique IDs remain: 340 done/303 open; reconciled counts are 136 done, 215 actionable, 15 blocked, 26 awaiting-decision, nine accepted limitations and 38 uncertain. The maintenance index has 271 rows covering every open ID exactly once. The archive, sprint, build/package configuration and dependency revisions remain unchanged. The observed intermittent correction-test failure remains a validation limit even though the unchanged isolated and full rechecks pass. Release remains not ready; broader historical qualification holds are preserved.

Final integrity command: `python3 /tmp/dw-331-verify-ledger.py`, exit 0. It compares all 643 record bodies to the entry snapshot (only DW-331 changes), verifies all six prior fields are retained, exact open-ID index coverage/disposition counts, unchanged archive/sprint/build/configuration/HEAD hashes and C# CRLF. Two initial audit attempts exited 1 because the ad-hoc parser assumed archived done stubs retained disposition fields, then assumed every done status lacked trailing annotations. Corrected the audit to count status and preserve the four historical `(B7 closure)` suffixes; no ledger history was rewritten to satisfy the check. Story-gitlink and whitespace checks also pass.
