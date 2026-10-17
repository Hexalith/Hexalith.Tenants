# DW-647 validation evidence — 2026-10-17

Clean entry: `3f4901afa260309f467e1971c082c5db4b7c4108`, branch `main`. Scope: accurate localized feedback when current safety changes invalidate held removal admission. Existing authorization, dispatch refusal, cleanup and failed-delivery identities remain enforced.

## Pre-edit runtime baseline

```bash
aspire ps --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated
aspire wait tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 120
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire logs tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --tail 12
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

All commands exit 0. No AppHost was initially running. Isolated start succeeds, UI readiness takes 21.5s, and the saved describe contains 23 Running/Healthy resources (`/tmp/dw-647-aspire-baseline.json`). UI logs were inspected; this run's AppHost was stopped before source edits/builds. This is baseline startup evidence, not post-patch browser qualification.

## Reproduction and fixture correction

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*RemoveTenantMemberFlowTests.HeldRemovalAdmission*' --filter-method '*RemoveTenantMemberFlowTests.RestoredSafetyStillRefusesInvalidatedRemovalAdmission' --minimum-expected-tests 24 --no-ansi --no-progress --results-directory /tmp/dw-647-red --report-xunit-trx --report-xunit-trx-filename focused.trx
```

Initial build: exit 0, zero warnings/errors, 19.23s (`/tmp/dw-647-red-build.log`). Initial tests: 2 pass/22 fail/zero skipped, exit 2, 2.477s (`/tmp/dw-647-red-tests.log`). Those 22 cases incorrectly resolved bUnit's service provider before registering gate/tracker services; the exception prevents reaching the runtime assertion. Registration now precedes resolution; no assertion was weakened.

Rebuild with the same build command: exit 0, zero warnings/errors, 15.51s (`/tmp/dw-647-red-corrected-build.log`). Repeating the test command with results directory `/tmp/dw-647-red-corrected` gives **16 exact wrong-copy failures, 8 passes, zero skips**, exit 2 in 2.105s (`/tmp/dw-647-red-corrected-tests.log`). Shipped EN/FR freshness and ordinary/elevated confirmation explanations are replaced by authority-indeterminate text in every failing first-attempt/retry case. The six genuine authority-failure and two restored-generation controls pass against the original source.

## Initial corrected verification

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --filter-class '*TenantRetainedCommandAttemptTests' --filter-class '*TenantRetainedCommandFlowTests' --minimum-expected-tests 404 --no-ansi --no-progress --results-directory /tmp/dw-647-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
```

Build: exit 0, zero warnings/errors, 14.92s (`/tmp/dw-647-build.log`). Focused: **404/404 pass**, zero failures/skips, exit 0, 26.938s (`/tmp/dw-647-focused.log`, `/tmp/dw-647-focused/focused.trx`). All 24 new cases pass. They verify exact shipped EN/FR copy, ordinary/elevated confirmation, stale/unknown freshness, initial/failed same-ID retry refusals, first-attempt assertive live-region feedback, no dispatch/status lookup, cleared or preserved identity as appropriate, released ownership, and explicit recovery. Denied/indeterminate/faulted fresh authority retains distinct authority feedback without disclosing exception detail. Temporarily invalidated then restored evidence cannot authorize dispatch; the next explicit submit performs two fresh authority reads.

## Independent review and correction

Initial full UI command:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6098 --no-ansi --no-progress --results-directory /tmp/dw-647-full --report-xunit-trx --report-xunit-trx-filename full.trx
```

**6098/6098 pass**, zero failures/skips, exit 0 in 1m59.389s (`/tmp/dw-647-full.log`). This execution precedes the review corrections below.

The independent context-free blind reviewer computed `N = min(floor(sqrt(19.162) + 1), 10) = 5` and reported five findings. Each is verified and classified `medium / patch`; the spec retains the individual triage/evidence. None is deferred. Retry refusal announcements omitted the actual reason; cleanup could publish an obsolete reason; restored-safe invalidated admission had inaccurate and hidden guidance; held-provider cancellation and authority-failed retries lacked coverage.

Corrections preserve Failed lifecycle truth while announcing Failed/Submit refusal explanations, re-evaluate eligibility after awaited cleanup and ownership guards, and add EN/FR `Tenants.RemoveMember.Admission.Changed` reconfirmation guidance that stays visible until explicit resubmission. Expanded behavioral coverage totals 50 new cases, including canceled held authority with late denied results, changed safety during held cleanup, and authority refusal/recovery for retained same-ID retries. No public contract, dispatch, cancellation or retention algorithm changes.

Independent source recheck accepts all five corrections, with no remaining concrete issue; the reviewer did not independently run tests. Reviewed build passes with zero warnings/errors, exit 0,16.53s (`/tmp/dw-647-reviewed-build.log`). Expanded focused execution, with the same three class filters and minimum 430, results directory `/tmp/dw-647-reviewed-focused`, gives 424/430 passes/six failures/zero skips, exit 2 in 33.141s (`/tmp/dw-647-reviewed-focused.log`). All six failures are the new held-release fixture waiting for an internal counter via bUnit's render-driven polling while callback entry produces no render. An explicit release-entered TaskCompletionSource now synchronizes that wait; source behavior and assertions are unchanged. This is a fixture correction, not a timeout increase or relaxed gate.

## Final reviewed verification

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --filter-class '*TenantRetainedCommandAttemptTests' --filter-class '*TenantRetainedCommandFlowTests' --minimum-expected-tests 430 --no-ansi --no-progress --results-directory /tmp/dw-647-final-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6124 --no-ansi --no-progress --results-directory /tmp/dw-647-final-full --report-xunit-trx --report-xunit-trx-filename full.trx
```

Final build: zero warnings/errors, exit 0,15.56s (`/tmp/dw-647-final-build.log`). Final focused run: **430/430 pass**, zero failures/skips, exit 0 in 28.900s (`/tmp/dw-647-final-focused.log`). The independent final recheck accepts the explicit callback-entry barrier and confirms all five findings remain satisfied, with no remaining concrete concern. All 50 added cases are included in focused coverage. Four final runtime/test/resource hashes were saved before the full run in `/tmp/dw-647-final-inputs.json`.

Final full UI run: **6124/6124 pass**, zero failures/skips, exit 0 in 2m07.875s (`/tmp/dw-647-final-full.log`, `/tmp/dw-647-final-full/full.trx`). The four final input hashes match after execution. This is the final reviewed source, including the fixture synchronization correction.

## Backlog closure and preservation

Only DW-647 and its action row close. The pre-edit snapshot `/tmp/dw-647-entry-state.json` verifies all 646 unrelated ledger bodies as identical normalized text, all six original DW-647 status/evidence/action fields under `pre-dw-647-` keys, every unrelated maintenance row unchanged, archive/sprint SHA-256 hashes unchanged, and canonical HEAD/indexed gitlinks unchanged. Final count: **647 IDs, 346 done, 301 open, 269 maintenance rows**. Every open ID appears in exactly one index row. No record was moved or added. Machine-readable results and final TRX counts are in [dw-647-ledger-validation.json](dw-647-ledger-validation.json).

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-647-removal-admission-feedback.md
git -c core.safecrlf=false diff --check
```

Both pass with exit 0. Story-gitlink validation reports no references pointer changes. Git status contains only the authored component, two resource files, behavioral tests and DW-647 tracking artifacts. No staging, commit, branch, push or dependency mutation ran.

## Verification boundaries

The relevant gate is the individual UI test project in Debug with source references. No Release build, unrelated test-project run, post-patch live browser test, production operation or full release qualification is claimed. Existing release holds remain. No dependency update, nested-submodule initialization, staging, commit, branch or push is part of this work.
