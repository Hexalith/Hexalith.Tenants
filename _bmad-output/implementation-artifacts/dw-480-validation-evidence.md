# DW-480 pre-dispatch removal resubmission — 2026-10-16

Entry: clean `main`, canonical HEAD `e91aef5d9a6ffee045f40957594010db61b36e5d`. The first refused activity lease created UnableToVerify without command identifiers or gateway delivery; a later submit then reported tracking loss. Only this undispatched refusal is eligible to return to Previewed. Potentially delivered retries and genuine tracking-loss guards retain their existing behavior.

## Runtime baseline

The repository Aspire skill supplied these commands, executed before source/test edits:

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire wait tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 45
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

All commands exit 0. Initial resource state: 13 Waiting, nine Running/Healthy, one Running/Unhealthy during startup. The UI readiness check succeeds after 22.1 seconds; final state is 23 Running/Healthy. The isolated Tenants host is stopped; pre-existing Platform and FrontComposer hosts are preserved. Raw output: `/tmp/dw-480-aspire-*`. Dashboard credentials are omitted. This establishes a runtime baseline, not browser acceptance.

## Regression verification

Commands run from the repository root:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/dw-480-red-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
```

The pre-fix build passes with zero errors and two occurrences of the same existing NU1903 SignalR.Common 10.0.12 advisory (GHSA-334f-vvp4-7g6p); audit policy is unchanged. Pre-fix focused execution exits 2: 83 passed, five failed, zero skipped. All five added cases fail as expected: callback decline/exception and safety cases observe UnableToVerify instead of the resubmittable Previewed state; the real competing-lease case directly proves the second submit dispatches zero commands instead of one. Existing tests pass. Raw output is retained in `/tmp/dw-480-red-build.log` and `/tmp/dw-480-red-focused.log`; the report is `/tmp/dw-480-red-focused/focused.trx`.

## Scope and integrity

Behavioral validation targets actual Fluent component events and the real shared admission gate/tracker. There are no structural markup, CSS, public API, backend, persistence, dependency or deployment changes. Review additionally corrected the existing live-region text selection for pre-dispatch refusal feedback. The narrow lane is the Debug source UI project, focused and full; unrelated Release/package/backend/browser gates are not rerun. Existing release qualification holds remain in prior evidence and are not resolved by this patch.

`/tmp/dw-480-entry-state.json` retains all 643 ledger records, tracked-file hashes, full baseline and root gitlinks. Only DW-480 and its maintenance index row may close after review. Archive, sprint and unrelated record bodies must remain unchanged. No staging, commit, push, branch or dependency mutation runs: user-provided repository instructions take precedence over the skill's automatic commit step.

## Post-fix verification

The same Debug source build passes with zero errors and the same one advisory emitted twice (`/tmp/dw-480-green-build.log`). All 88 focused removal cases pass, zero failures/skips (`/tmp/dw-480-green-focused.log`, `/tmp/dw-480-green-focused/focused.trx`). The five formerly failing cases now pass, as does the existing same-identity retry-refusal control.

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --minimum-expected-tests 88 --no-ansi --no-progress --results-directory /tmp/dw-480-green-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 5993 --no-ansi --no-progress --results-directory /tmp/dw-480-full --report-xunit-trx --report-xunit-trx-filename full.trx
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-480-predispatch-removal-resubmission.md
git diff --check
```

At this pre-review point the full UI project was still running; its completed outcome follows below. Story-gitlink validation passes with no pointer movement; whitespace validation passes with existing CRLF-to-index-LF normalization notices. The component/test checkout retains CRLF. The later completed independent blind review and corrections are recorded below.

## Independent review corrections

The blind reviewer computed `N = min(floor(sqrt(18.331) + 1), 10) = 5`. Four findings are patched: meaningful EN/FR live-region refusal feedback, current authority-provider denial/indeterminate coverage, held-reacquisition safety/cleanup coverage, and normal Accepted/status/projection reconciliation with exact ID and final lease release. The older stale-pre-dispatch recovery gap is verified and separately deferred as DW-644. Finding-by-finding verdicts are retained in the spec; no fix to that older branch is claimed.

The pre-review full UI run passes 5,993/5,993, zero failures/skips, exit 0, in 2m32s (`/tmp/dw-480-full.log`, `/tmp/dw-480-full/full.trx`). That result predates six review-driven cases and the live-region correction. The final reviewed build, focused and full results are recorded below.

The first reviewed build exits 1 with CS1739 in the expanded test fixture: `CommittedEventSequence` is an init property, not a constructor parameter. The fixture now uses its existing object-initializer convention; no runtime API changed. The failed log is preserved as `/tmp/dw-480-reviewed-build.log`. The corrected build writes `/tmp/dw-480-reviewed-build-recheck.log`.

The first reviewed focused run exits 2: 92 passed/two failed/zero skipped. Both new EN/FR fixtures resolved the bUnit service provider before registering the culture-bound localizer, causing its registration guard to throw. Registration now uses a deferred factory, followed by resolution after all registrations. Other reviewed regressions, including Accepted reconciliation and held safety/provider changes, pass. Failed output/report remain `/tmp/dw-480-reviewed-focused.log` and `/tmp/dw-480-reviewed-focused/focused.trx`. Corrected build and focused logs use the `reviewed-final` prefix below.

## Final reviewed commands

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --minimum-expected-tests 94 --no-ansi --no-progress --results-directory /tmp/dw-480-reviewed-final-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 5999 --no-ansi --no-progress --results-directory /tmp/dw-480-reviewed-final-full --report-xunit-trx --report-xunit-trx-filename full.trx
python3 /tmp/dw-480-verify-preservation.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-480-predispatch-removal-resubmission.md
git diff --check
```

The corrected final Debug source build exits 0, zero errors and the same advisory emitted twice (`/tmp/dw-480-reviewed-final-build.log`). Test logs/reports use the corresponding `reviewed-final-focused` and `reviewed-final-full` prefixes. The reviewer rechecked all five findings and confirmed no remaining implementation corrections; its recheck was source inspection, with final execution recorded separately. Final focused execution passes94/94 and full UI passes5,999/5,999, zero failures/skips, exit0; the full run takes2m28s. The integrity command runs after final ledger reconciliation and passes.

## Final closure and limits

Only DW-480 closes. Its original finding and all six earlier status/disposition/verification/action fields remain in the record. DW-644 separately records the existing non-lease stale-to-current pre-dispatch recovery defect, with source-only verification and a concrete future regression. There are644 unique IDs:342 done/302 open, covered exactly once by270 maintenance rows. The original439-record reconciliation now has138 done/214 actionable; the newly actionable DW-644 makes215 actionable overall. The integrity command exits0 and confirms all642 unrelated original record bodies unchanged, every unrelated tracked-file hash unchanged, archive/sprint preserved, canonical HEAD and root gitlinks unchanged, and C# / Razor CRLF retained (`/tmp/dw-480-preservation-result.json`). Story-gitlink and whitespace checks pass. Only the component, its existing test class, ledger and maintenance index change among existing tracked files; this spec and evidence document are new.

The final runtime diff is the undispatched refusal state/comment and localized live-region text selection. The new tests add11 cases, all passing, including normal Accepted/status/projection completion and exact ownership release. No new dispatched-retry policy, API, DOM structure, styles, persistence or dependencies are introduced. Rendered EN/FR live-region semantics are tested; actual assistive-technology speech timing and broader browser/Release/package/backend qualification are not claimed. The one pre-existing SignalR.Common package advisory and historical release qualification holds remain. No staging, commit, push, branch or dependency mutation ran.
