# DW-644 pre-dispatch evidence recovery — 2026-10-17

Entry: clean `main`, canonical HEAD `0e1fe111a5b4cce684455948b4fd12960589ef55`. Scope is DW-644, the latest concrete deferred follow-up from DW-480. Member removal blocked before any delivery must remain explicitly resubmittable after evidence recovers; uncertain delivered attempts retain their original identity and fail-closed tracking guards.

## Runtime baseline

The repository Aspire skill supplied these commands, run before source/test edits:

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire wait tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 45
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

All commands exit 0. The first snapshot had 13 Waiting, nine Running/Healthy and one Running/Unhealthy during startup. UI readiness succeeds. The final snapshot has 16 Running/Healthy resources and seven Dapr CLI resources Finished with exit code 1 (eventstore-admin, eventstore-admin-ui, eventstore, memories, sample, tenants-api and tenants sidecars). This does not establish a healthy distributed topology; no root-cause diagnosis or runtime repair is claimed. Raw command outputs remain in `/tmp/dw-644-aspire-*`; dashboard credentials are omitted. The isolated Tenants host was stopped and pre-existing hosts were preserved.

## Regression verification

Commands from the owning repository root:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --minimum-expected-tests 99 --no-ansi --no-progress --results-directory /tmp/dw-644-red-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
```

The initial fixture build exits 1, CS0117: ReadModelFreshnessState has no Refreshing value. The fixture uses the existing Aging value instead; the repeated build passes with zero errors (`/tmp/dw-644-red-build.log`, `/tmp/dw-644-red-build-recheck.log`). The pre-fix focused run exits 2: 99 total, 94 passed, five failed, zero skipped, in 5.526 seconds. All five new current-to-unavailable-to-current or held-admission recovery cases fail because the second submission dispatches zero commands. Existing cases pass. Raw log/report: `/tmp/dw-644-red-focused.log`, `/tmp/dw-644-red-focused/focused.trx`.

Expanded post-fix fixtures initially fail compilation with CS0117 (ViewportTier.Mobile; corrected to Phone), then CA1062 (theory string parameter; corrected with ArgumentNullException.ThrowIfNull). These are fixture-only corrections; no runtime API or analyzer gate changed. Logs: `/tmp/dw-644-green-build.log`, `/tmp/dw-644-green-build-recheck.log`. The corrected source build passes with zero errors (`/tmp/dw-644-green-build-recheck2.log`). Builds preserve the existing NU1903 SignalR.Common 10.0.12 advisory, emitted twice; no dependency or audit-policy changes ran.

## Post-fix verification

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --minimum-expected-tests 109 --no-ansi --no-progress --results-directory /tmp/dw-644-green-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6014 --no-ansi --no-progress --results-directory /tmp/dw-644-full --report-xunit-trx --report-xunit-trx-filename full.trx
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-644-predispatch-freshness-recovery.md
git diff --check
```

Focused execution passes 109/109, zero failures/skips, exit 0 in 2.600 seconds (`/tmp/dw-644-green-focused.log`, `/tmp/dw-644-green-focused/focused.trx`). This includes 15 added cases plus five extended initial unavailable-context cases; all formerly failing recovery cases now pass. Full UI execution and independent blind review are in progress at this checkpoint; completion is recorded below. Story-gitlink validation passes with no pointer movement; whitespace passes with the existing checkout CRLF-to-index-LF notices.

The completed full UI run passes 6,014/6,014, zero failures/skips, exit 0 in 1m22.163s (`/tmp/dw-644-full.log`, `/tmp/dw-644-full/full.trx`). This result covers the complete current runtime/test patch before independent review.

## Independent review and correction verification

The context-free blind reviewer computed `N = min(floor(sqrt(29.90) + 1), 10) = 6` and returned six findings. All are triaged individually in [the spec](spec-dw-644-predispatch-freshness-recovery.md#review-triage-log). Five are corrected: current tenant/target intent and renewed typed confirmation, suppression of obsolete safety-refusal copy after recovery, final gateway disappearance/restoration coverage, initially unavailable EN/FR announcements, and partial-tracking controls. The independent lease-busy refusal still retains its existing announcement; uncertain delivered attempts never become fresh previews.

The sixth finding is an older source-verified cleanup-exception defect, newly recorded as DW-645: a false activity release can throw after undispatched ownership is detached but before a recoverable snapshot is installed. The original source already awaited release before Blocked and already rethrew false-release errors. It is independently shippable cleanup work, not part of DW-644 closure. No throwing-release end-to-end execution or repair is claimed.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --minimum-expected-tests 118 --no-ansi --no-progress --results-directory /tmp/dw-644-reviewed-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6023 --no-ansi --no-progress --results-directory /tmp/dw-644-reviewed-full --report-xunit-trx --report-xunit-trx-filename full.trx
```

The reviewed build passes, zero errors and the same existing advisory emitted twice (`/tmp/dw-644-reviewed-build.log`). Reviewed focused tests pass 118/118, zero failures/skips, exit 0 in 2.331 seconds (`/tmp/dw-644-reviewed-focused.log`, `/tmp/dw-644-reviewed-focused/focused.trx`). This is 24 added cases and five extended initial unavailable cases over entry. The final reviewed full run and independent recheck are in progress here; their completion is recorded below.

That intermediate full run passes 6,023/6,023, zero failures/skips, exit 0 in 1m23.683s (`/tmp/dw-644-reviewed-full.log`, `/tmp/dw-644-reviewed-full/full.trx`). Independent recheck accepts findings 3–6 and confirms finding 2 is old cleanup behavior, but finds two further identity variants: held admission resets old intent before comparison, and initial Idle/no-preview contexts have no intent. A private parameter identity tuple now clears confirmation before retained-attempt restoration; additional held and initial-Idle tests cover both variants. `/tmp/dw-644-final-build.log` is an intermediate passing build for the held-case correction; the final validation below includes independent parameter identity and all 27 added cases. No previous pass is represented as verification of later source.

The final independent read-only recheck confirms finding 1 and its held/initial-Idle variants are addressed, with no remaining specific correction. The source-confirmed DW-645 deferral remains separate. The exact baseline read confirming its provenance is:

```bash
git show 0e1fe111a5b4cce684455948b4fd12960589ef55:src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor
```

## Final verification

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --minimum-expected-tests 121 --no-ansi --no-progress --results-directory /tmp/dw-644-final-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6026 --no-ansi --no-progress --results-directory /tmp/dw-644-final-full --report-xunit-trx --report-xunit-trx-filename full.trx
```

The final build passes with zero errors and the unchanged advisory emitted twice (`/tmp/dw-644-final-build-recheck.log`). Final focused execution passes 121/121, zero failures/skips, exit 0 in 2.308 seconds (`/tmp/dw-644-final-focused.log`, `/tmp/dw-644-final-focused/focused.trx`). The full final result follows when complete.

This full run exits 2: 6,025 passed/one failed/zero skipped (6,026 total), in 1m27.857s (`/tmp/dw-644-final-full.log`, `/tmp/dw-644-final-full/full.trx`). The existing shared-flow theory `FaultedOldActivityAcquisitionCannotDetachTheNewRouteAttempt("remove")` timed out waiting for new-tenant delivery because it reused old confirmation instead of typing renewed consent. The corrected runtime intentionally clears that consent. Only this theory's removal branch in `tests/Hexalith.Tenants.UI.Tests/Components/TenantRetainedCommandFlowTests.cs` is adjusted: assert blank confirmation, attempt submission and require zero delivery, then deliberately confirm before running all original old-fault/new-attempt ownership assertions. This adds one necessary shared fixture to the tracked footprint; runtime source and admission safeguards are unchanged. The final rechecks below supersede this failed run.

The independent reviewer confirms this fixture update retains the original old-callback/new-route race assertions and requires the newly correct consent. The source build passes with zero errors and the same advisory (`/tmp/dw-644-shared-build.log`). The entire shared retained-flow class passes 207/207, zero failures/skips, exit 0 in 23.732 seconds (`/tmp/dw-644-shared-focused.log`, `/tmp/dw-644-shared-focused/focused.trx`).

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantRetainedCommandFlowTests' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/dw-644-shared-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6026 --no-ansi --no-progress --results-directory /tmp/dw-644-accepted-full --report-xunit-trx --report-xunit-trx-filename full.trx
```

The accepted full UI run passes 6,026/6,026, zero failures/skips, exit 0 in 1m32.277s (`/tmp/dw-644-accepted-full.log`, `/tmp/dw-644-accepted-full/full.trx`). It includes the final runtime correction, all 27 added removal cases and the reviewed shared fixture adjustment. Final removal121/shared207/full6026 outcomes are all passing. All six independent findings and the identity recheck variants have a recorded disposition; only the source-confirmed older DW-645 cleanup defect remains deferred.

## Final reconciliation and integrity

Only DW-644 closes. Its original finding and six earlier status/disposition/verification/action fields remain as `pre-closure-*` evidence. New DW-645 is open with source-only verification, an exact throwing-release reproduction and a concrete next action. The ledger contains 645 unique IDs:343 done/302 open, covered exactly once by270 maintenance rows. The original439-record reconciliation remains138 done/214 actionable; newly actionable DW-645 is separate from that original population. Every643 unrelated original ledger body remains byte-equivalent under the ledger's LF text convention; archive and sprint hashes are unchanged.

```bash
python3 /tmp/dw-644-integrity.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-644-predispatch-freshness-recovery.md
git diff --check
git status --short --branch
```

All commands exit 0. Integrity output `/tmp/dw-644-preservation-result.json` confirms exact ID/index coverage, preserved unrelated records/files, unchanged canonical HEAD and index/root pointers, archive/sprint preservation and C#/Razor CRLF. Five existing tracked files change: the component, its removal tests, the one shared-flow fixture, ledger and maintenance index. This spec and evidence are the two new artifacts. Story-gitlink and whitespace checks pass; checkout normalization notices are retained. No staging, commit, push, branch, dependency mutation, nested initialization, deployment or data replay ran. Release holds and the limited Aspire baseline above remain unresolved; this scoped UI closure does not establish release readiness.

## Scope and preservation

The runtime change is private component lifecycle handling. Tests exercise actual Fluent form events, real aggregate admission/tracker state, held callbacks, current authority, confirmation, audit support, measured viewport, duplicate delivery suppression, existing accepted/status/projection reconciliation and shipped EN/FR refusal feedback. Initial unavailable contexts retain Idle until a complete preview exists; existing previews retain their evidence. Genuine uncertain null-ID lifecycle states remain fail closed. Previously delivered Failed attempts preserve same-ID recovery through unavailable freshness.

No layout/CSS, backend, persistence, public API, dependency or deployment changes are included. The narrow gate is the Debug source UI test project, focused and full. Backend/package/browser lanes are not rerun for this component state correction. Existing release qualification holds and the limited runtime baseline above remain unresolved.

`/tmp/dw-644-entry-state.json` retains 644 ledger record bodies, tracked hashes, full HEAD and root gitlinks. Only DW-644 and its index row may close after independent review. Preserve historical fields, unrelated records, archive/sprint and dependency pointers. User-provided repository instructions prohibit unrequested staging/commits/pushes/branches/dependency changes and take precedence over the workflow's automatic commit step.
