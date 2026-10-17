# DW-646 validation evidence — 2026-10-17

Entry: `f052c0094d952cb4856a4aa1e1e69cd15ca6ac72`, clean `main`. Scope: retained failed removal identity across abandonment of a same-identity, undispatched retry and subsequent renderer replacement/cancel/reopen. No dependency, source-reference or public API change.

## Runtime baseline

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire wait tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 45
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

All exited 0. No pre-existing AppHost was running. Initial describe had resources waiting while security initialized; UI wait passed in 18.9s, and the final describe showed all 23 resources Running/Healthy. This run's AppHost was stopped before source builds. Local raw descriptions/logs: `/tmp/dw-646-aspire-before.json`, `/tmp/dw-646-aspire-after.json`, `/tmp/dw-646-aspire-logs.json`. Runtime startup is a baseline check, not live end-to-end qualification of the patch.

## Initial reproduction

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*RemoveTenantMemberFlowTests.AbandonedRemovalRetryRetainsIdentityAcrossRendererReplacement' --minimum-expected-tests 8 --no-ansi --no-progress --results-directory /tmp/dw-646-red --report-xunit-trx --report-xunit-trx-filename focused.trx
```

Built original runtime source with eight new cases: zero warnings/errors, exit 0, 13.87s (`/tmp/dw-646-red-build.log`). All eight tests fail because the retained original record is null, zero skips, exit 2 (`/tmp/dw-646-red-tests.log`, `/tmp/dw-646-red/focused.trx`). Cases cover freshness/confirmation refusal and successful/faulted cleanup, refused/faulted acquisition, and disposal during held cleanup.

## Focused verification

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --filter-class '*TenantRetainedCommandAttemptTests' --filter-class '*TenantRetainedCommandFlowTests' --minimum-expected-tests 150 --no-ansi --no-progress --results-directory /tmp/dw-646-focused-corrected --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*TenantDetailSurfaceTests.MemberReviewCancelReopenPreservesRefusedRemovalRetryIdentity' --minimum-expected-tests 2 --no-ansi --no-progress --results-directory /tmp/dw-646-parent --report-xunit-trx --report-xunit-trx-filename focused.trx
```

Build: exit 0, zero warnings/errors, 12.97s (`/tmp/dw-646-build.log`). Focused: 364/364, zero failures/skips, exit 0, 24.667s (`/tmp/dw-646-focused-corrected.log`). Parent: 2/2, zero failures/skips, exit 0, 1.733s (`/tmp/dw-646-parent.log`). New controls require fresh confirmation/admission and exact retry IDs, preserve original start/baseline and request, leave no pending ownership/lock after refusal, reject unrelated rollback and stale-owner cleanup, and retain new dispatched evidence through ownerless completion.

Initial verification used the same three class filters together with the parent method filter and `--minimum-expected-tests 220 --results-directory /tmp/dw-646-focused`; it ran zero tests and exited 8. Class and method filter groups intersect, so separate invocations above replace that invalid evidence. Exact initial output is `/tmp/dw-646-focused.log`; no gate was relaxed.

## Full UI verification

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6058 --no-ansi --no-progress --results-directory /tmp/dw-646-full --report-xunit-trx --report-xunit-trx-filename full.trx
```

Initial pre-review run: **6,058/6,058 passed**, zero failures/skips, exit 0, 1m36.911s (`/tmp/dw-646-full.log`). Final reviewed execution is recorded below; this initial run does not validate later corrections.

## Boundaries

The owning UI project is the relevant test gate. No Release, other-project, post-patch live browser or full release qualification is claimed. Existing release holds and DW-647 remain independent. No Git mutation, event/state-store repair, production operation, dependency update or nested-submodule initialization ran.

## Independent review and corrections

The context-free blind reviewer computed `N = min(floor(sqrt(30.123) + 1), 10) = 6` and returned six findings. All six are retained with verdict/evidence in the spec's Review Triage Log and addressed. One runtime predicate correction accepts null/empty/whitespace failed-source correlation, matching SubmitAsync. Additional tests exercise disposal during held retry acquisition, new same-tenant delivery before old acquisition/release returns (success/fault), current replacement authority/freshness/viewport/member-role/lifecycle/audit refusals, and all eight original recoveries through accepted delivery, verified status and projection confirmation with terminal admission release. New conditional bodies use conventional Allman blocks.

The first expanded build (`/tmp/dw-646-reviewed-build.log`) exited1 with three CS0117 errors in the added fixture (invented Denied/Unknown/Mobile enum values). Correcting them to the actual MissingPermission/Indeterminate/Phone values produced zero warnings/errors and exit 0 in 27.11s (`/tmp/dw-646-reviewed-build-corrected.log`). Both runs used the same source build command shown above.

The first expanded focused run used the three class filters above, minimum 380 and result directory `/tmp/dw-646-reviewed-focused`; 375/380 passed, five failed, zero skipped, exit 2 in 24.726s. Four new timing fixtures checked state before bUnit handle disposal had executed during an outstanding callback. They now use the component's synchronous IDisposable cleanup, matching the existing disposal regression. The fifth incorrectly assumed a last owner must be blocked; the current product uses elevated confirmation friction. That fixture now checks Unknown role, which actually refuses eligibility. Safety assertions remain; production policy did not change for these fixture corrections. Exact output: `/tmp/dw-646-reviewed-focused.log` and its TRX.

The independent correction rechecks accept all six changes and both fixture corrections, with no remaining concrete issue. Reviewers inspected source and did not independently execute tests. No new work was deferred; DW-647 remains the separate existing feedback issue.

## Final reviewed verification

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --filter-class '*TenantRetainedCommandAttemptTests' --filter-class '*TenantRetainedCommandFlowTests' --minimum-expected-tests 380 --no-ansi --no-progress --results-directory /tmp/dw-646-final-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6074 --no-ansi --no-progress --results-directory /tmp/dw-646-final-full --report-xunit-trx --report-xunit-trx-filename full.trx
```

Build passes with zero warnings/errors, exit 0,15.99s (`/tmp/dw-646-final-build.log`). Focused: **380/380 passed**, zero failures/skips, exit 0,25.145s (`/tmp/dw-646-final-focused.log`). Full UI: **6,074/6,074 passed**, zero failures/skips, exit 0,1m37.011s (`/tmp/dw-646-final-full.log`). All 32 added cases are included, along with the two parent cancel/reopen cases. DW-646 is complete after these passing executions and independent source correction rechecks.

## Backlog preservation and repository checks

The pre-edit ledger snapshot `/tmp/dw-646-entry-state.json` qualifies the closure. The preservation runner verifies all 646 unrelated ledger bodies exactly as normalized text, all six original DW-646 status/evidence/action fields under pre-closure keys, unchanged archive/sprint SHA-256 values, canonical HEAD and indexed root gitlinks, and one-to-one index coverage of every remaining open record. Result: **647 records, 345 done, 302 open, 270 maintenance rows**. Machine-readable output is [dw-646-ledger-validation.json](dw-646-ledger-validation.json). Only DW-646 closes; no record was moved or added and all earlier history remains.

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-646-removal-retry-retention.md
git -c core.safecrlf=false diff --check
```

Both pass with exit 0; gitlink validation reports no references pointer changes. Git status shows only the authored source/tests and DW-646 tracking artifacts. No commit, staging, branch, push or dependency mutation ran. Existing release holds remain; this maintenance closure does not establish release readiness.
