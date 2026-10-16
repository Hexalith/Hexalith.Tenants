# DW-470 keyed member failure acceptance — 2026-10-16

Entry HEAD: `9abc6f70607ec73bfb1da3fd49b74b3560609e8c`, clean `main`. Both runtime member flows already preserve the safe message key in initial and retry nonaccepted delivery completions. This reconciliation adds missing acceptance coverage; runtime, resources and dependencies retain their implementations.

Twelve actual Fluent cases cover add/change × EN/FR × initial failure/failed resubmission/ambiguous delivery retry. The gateway returns `FailedWithKey("Tenants.Commands.Unavailable.InvalidTrackingReference")` without a literal explanation or returned identity. Tests assert the exact shipped localized explanation, key preservation, null literal message, localized assertive failed announcement, original gateway identity and intent, baseline, dispatch count, no status lookup, independent audit state and terminal aggregate admission release. The two retry variants first supply a failed sent identity; the ambiguous variant proves RequestSent before entering RetryDeliveryAsync. A fresh pre-dispatch failure remains NotStarted in the audit dimension; retries remain AuditUnavailable.

## Commands and results

All commands run from `/home/administrator/projects/hexalith/tenants`.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantRetainedCommandFlowTests' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/dw-470-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 5967 --no-ansi --no-progress --results-directory /tmp/dw-470-full --report-xunit-trx --report-xunit-trx-filename full.trx
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-470-keyed-member-failures.md
git diff --check
```

- Initial build: exit 1, CS8382 at the new tuple-equality ShouldAllBe predicate. Corrected to foreach tuple assertions. Log: `/tmp/dw-470-debug-build.log`.
- Corrected Debug source build: exit 0, zero errors; two existing NU1903 warnings for pinned SignalR.Common 10.0.12. Log: `/tmp/dw-470-debug-build-final.log`.
- Pre-review focused suite: exit 0, 207 passed, zero failed/skipped, including all 12 new cases. Log: `/tmp/dw-470-focused.log`; TRX: `/tmp/dw-470-focused/focused.trx`.
- Pre-review full suite: exit 0, 5,979 passed, zero failed/skipped, 2m01s. Log: `/tmp/dw-470-full.log`; TRX: `/tmp/dw-470-full/full.trx`. This pre-review run preceded the corrections and final passing rechecks recorded below.
- Story-gitlink guard: exit 0, no references pointer changes from the full recorded entry baseline. Whitespace check: exit 0; existing CRLF-to-LF normalization advisory remains. Checkout C# CRLF is preserved.

## Pre-edit runtime check

Using the permitted repository Aspire skill, discovered an unrelated FrontComposer AppHost and left it untouched. Started Tenants in isolation:

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire wait tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 45
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

All commands exited 0. UI readiness took 31.6 seconds; final inspection showed all 23 resources Running/Healthy. Initial telemetry contained 126 entries (94 Warning, 30 Information, two Error), so this is a qualified baseline rather than comprehensive runtime acceptance. Raw output is under `/tmp/dw-470-aspire-*`; dashboard credentials are omitted.

## Scope and limits

This is a test/documentation acceptance reconciliation. No runtime or resource patch is needed. Debug source is the relevant verification lane; Release, standalone browser, broad solution, package and unrelated backend lanes are not rerun. Existing release holds remain documented in [earlier qualification evidence](dw-337-validation-evidence.md). No production data repair or deployment ran.

Checkout SHA-256 snapshots for both runtime flows, archive, sprint, build configuration, package catalog and UI test project were recorded before edits in `/tmp/dw-470-entry-state.json`. Their contents remain unchanged at the pre-review check. No staging, commit, push, branch or dependency mutation is performed. The user-provided repository instructions take precedence over the workflow's automatic commit step.

## Independent review and correction

The context-free blind reviewer computed `min(floor(sqrt(8.13) + 1), 10) = 3` and returned three acceptance gaps. All three were accepted as medium and patched: explicit baseline expectations, literal EN/FR failed announcements independent of neutral resource fallback, and rendered audit availability for reused/fresh identities. The reviewer independently rechecked the patch, confirmed all findings resolved, and reported no additional regression. Finding-by-finding triage is in [the owning spec](spec-dw-470-keyed-member-failures.md). Nothing was deferred.

Reviewed build passes with zero errors and the same two existing advisory warnings. Reviewed focused suite passes 207/207, including all 12 new cases, with zero failures/skips. Reviewed full suite passes 5,979/5,979, zero failures/skips, 2m06s. Final reviewed commands preserve the build flags and existing full-suite floor:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantRetainedCommandFlowTests' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/dw-470-reviewed-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 5967 --no-ansi --no-progress --results-directory /tmp/dw-470-reviewed-full --report-xunit-trx --report-xunit-trx-filename full.trx
```

Raw reviewed logs: `/tmp/dw-470-reviewed-build.log`, `/tmp/dw-470-reviewed-focused.log`, `/tmp/dw-470-reviewed-full.log`. TRX reports remain in the corresponding reviewed results directories. Earlier passing results are preserved as pre-review evidence.

## Final reconciliation

Reviewed build and both reviewed test commands exit 0. All 12 added cases passed in both reviewed TRX reports. Only DW-470 and its maintenance row close: all 643 IDs remain, 339 done/304 open across 272 rows. The reconciled subset is 135 done/216 actionable/15 blocked/26 awaiting-decision/nine accepted limitations/38 uncertain. A read-only baseline comparison verifies every other ledger record and maintenance action row is identical and the index covers every open ID exactly once. DW-470 retains all original finding and pre-closure verification/action fields. Archive, sprint, both runtime flows and captured build inputs retain their entry checkout hashes.

The reviewed test checkout SHA-256 is `68523c6ae1f4ebe4682046d3bb211a3467803f98df494b3ae52f0eaab0f65ec3`; C# CRLF is preserved. Reviewed TRX SHA-256 values:

- `/tmp/dw-470-reviewed-focused/focused.trx`: `a9149e1dc1ab768c81e1eaf52ed64aacdf321898cb80980d9aa4553810782d92`.
- `/tmp/dw-470-reviewed-full/full.trx`: `761143cc24acf8bcb551d6abd1d8be27ff76e22cf8f10e4b92d34713321d98ba`.

Release holds remain; closing DW-470 does not establish release readiness. No further bundle is selected. Changes remain uncommitted.

Final story-gitlink validation and `git diff --check` both exit 0. Root HEAD remains `9abc6f70607ec73bfb1da3fd49b74b3560609e8c`. No references pointer movement is reported by the guard; archive and sprint checkout hashes remain identical.
