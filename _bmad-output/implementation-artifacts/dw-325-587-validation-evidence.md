# DW-325/DW-587 validation evidence — 2026-10-17

Clean entry: `fa56dc1e7abd48e770a87925d3aa3b221e648cf7`, branch `main`. These duplicate records concern configuration-management activity conflating retained removal evidence with an accepted child activity lease. Scope remains one expiry/ownership correction.

## Runtime baseline

```bash
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire logs security --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --tail 10
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
aspire ps --format Json
```

All commands exit 0. Startup succeeds, but the inspected readiness snapshot reports security Unhealthy at `https://localhost:8543/health/ready` with a TLS unexpected-EOF failure; dependent Tenants resources are Waiting. Security logs were inspected. This run's AppHost was stopped before source builds; the final process list is empty. The initial snapshot does not establish eventual readiness or any post-patch browser behavior. An attempted summary pipe used unavailable `python` (exit 127), then succeeded with `python3`; no configuration was changed.

## Reproduction and fixture corrections

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*TenantDetailSurfaceTests.Management_releases_retained_only_activity_after_refused_adoption_expires' --minimum-expected-tests 2 --no-ansi --no-progress --results-directory /tmp/dw-325-red-corrected --report-xunit-trx --report-xunit-trx-filename focused.trx
```

The first build succeeds, but the initial two test cases fail during fixture setup: registering the tracker after `RegisterComponentServices` resolves bUnit's provider is prohibited. Registration now precedes the helper. Rebuild succeeds; the corrected original-source test command above fails both cases, zero passes/skips, exit 2: the unavailable landmark is absent after refused retained adoption and clock-controlled expiry, with and without a subsequent parameter render. This is the executed defect. The historical literal “unmounted child” scenario was not reproduced; current rendering retains an adopted child. The regression instead proves a mounted child that never acquired activity cannot lower the parent's retained-only latch.

The first patched build succeeds with zero warnings/errors in 14.66s. A three-case execution adds `--filter-method '*TenantDetailSurfaceTests.Matching_signalr_notification_reconciles_retained_remove_without_redispatch_or_nudge_success'`, minimum 3, results `/tmp/dw-325-green`. Terminal confirmation passes; both new tests fail because they expect the resource key where the stub localizer returns translated text. They now compare the exact localized value. The missing-landmark assertion is preserved.

Expanded five-class focused runs use the command below. Initial expanded execution has 462 passes/one failure/zero skips, exit 2 in 30.089s: its disposal test reads `cut.Instance` after disposal. Capturing the instance fixes that access. The next execution also has 462 passes/one failure/zero skips, exit 2 in 30.653s: synchronous wrapper disposal returns before renderer teardown removes subscriptions. The fixture now awaits `DisposeComponentsAsync`, checks actual unsubscription, invokes a previously captured callback, drains the renderer and verifies no render or renderer exception.

## Independent review and correction

The context-free blind reviewer computed `N = min(floor(sqrt(20.457) + 1), 10) = 5`. All five findings are accepted as medium/patch and individually recorded in the spec:

- Child-first expiry cancels the tracker's timer through `Forget`. The existing tracker now emits its existing expiry event after removing matching expired ownership, outside the lock. Ordinary terminal/abandon release and wrong identities remain silent; no public signature changes.
- Notification-driven modal removal schedules the existing surviving-heading focus fallback; regressions assert its exact focus target.
- A queued old notification rereads replacement retention and its target, preserving a different key.
- Held-dispatcher tests exercise same-tenant replacement and an actual tenant switch before queued notification execution.
- Disposal evidence now awaits teardown and drains captured callback work.

The independent source recheck accepts all five corrections and identifies no concrete new problem. It did not execute tests. No finding was deferred.

## Final verification

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantDetailSurfaceTests' --filter-class '*RemoveTenantConfigurationFlowTests' --filter-class '*SetTenantConfigurationFlowTests' --filter-class '*TenantRemoveConfigurationAttemptTrackerTests' --filter-class '*TenantSetConfigurationAttemptTrackerTests' --minimum-expected-tests 470 --no-ansi --no-progress --results-directory /tmp/dw-325-reviewed-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
```

Reviewed source build passes with zero warnings/errors, exit 0 in 15.17s (`/tmp/dw-325-reviewed-build.log`). Focused execution passes **470/470**, zero failures/skips, exit 0 in 31.481s (`/tmp/dw-325-reviewed-focused.log`). All thirteen added cases pass. The four source/test hashes saved in `/tmp/dw-325-reviewed-inputs.json` match after the full run.

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6137 --no-ansi --no-progress --results-directory /tmp/dw-325-reviewed-full --report-xunit-trx --report-xunit-trx-filename full.trx
```

Full individual UI execution passes **6137/6137**, zero failures/skips, exit 0 in 1m58.609s (`/tmp/dw-325-reviewed-full.log`, `/tmp/dw-325-reviewed-full/full.trx`). These are the final independently rechecked runtime inputs; no source changes follow this execution.

## Tracking and limits

Only DW-325/DW-587 and their shared maintenance row close. All 647 IDs remain: **348 done/299 open**, indexed by 268 rows. The preserved entry snapshot verifies all 645 unrelated ledger bodies as identical normalized text, every unrelated index row unchanged, six original fields per closed entry retained under `pre-dw-325-`/`pre-dw-587-`, exact-once open-ID index coverage, unchanged archive/sprint hashes, unchanged canonical HEAD before the workflow commit and unchanged indexed dependency pointers. [Machine-readable preservation, source hashes and TRX counts](dw-325-587-ledger-validation.json) retain these results. Historical findings, decisions and prior validation evidence remain in place. No Release, unrelated project, live browser, production operation or complete release qualification is claimed.

The new spec initially fails the story-gitlink validator for missing `baseline_commit` (exit 1). Adding the directly obtained canonical entry revision makes validation pass, with no reference pointer changes. Pinned commitlint 21.2.3 validates the exact full candidate file with zero problems/warnings, exit 0:

```bash
node node_modules/@commitlint/cli/cli.js --edit /tmp/dw-325-commit.txt --verbose
```

Validated candidate: `fix(ui): release expired configuration removal activity`. The installed CLI and lockfile both resolve commitlint 21.2.3. Its successful output is:

```text
fix(ui): release expired configuration removal activity
✔   found 0 problems, 0 warnings
```

The workflow's local commit step uses that exact validated message file after the final tree inspection. No push, branch or dependency update is performed.

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-325-587-remove-configuration-activity.md
git -c core.safecrlf=false diff --check
```

Final checks pass with exit 0. The story guard reports no reference pointer changes. Existing release holds remain; this bundle does not establish full release readiness.
