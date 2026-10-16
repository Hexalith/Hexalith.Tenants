# DW-285 held submission acceptance — 2026-10-16

The original reentry defect is already repaired in the current create, add-member and change-role flows. This pass adds the missing acceptance regression in `TenantRetainedCommandFlowTests`, without changing runtime code. Independent review/recheck and final maintained Debug focused195/full UI5967 pass, zero failures/skips. Only DW-285 closes; DW-470 remains separate.

Twelve cases cover three flows × freshness/command-support loss × accepted/failed completion. Each dispatch stays held while parameters change through `SetParametersAsync` on the renderer. A direct second submission must leave the entire original snapshot and retained evidence unchanged, with one gateway send, unresolved ownership and a locked aggregate that refuses another owner. The original continuation is awaited after releasing delivery. Message ID, intent, baseline and returned correlation remain bound to the original attempt; acceptance retains ownership, while terminal failure releases it. Cleanup releases and awaits held delivery even when assertions fail.

## Commands and results

All commands run from `/home/administrator/projects/hexalith/tenants`.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantRetainedCommandFlowTests' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/dw-285-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 5954 --no-ansi --no-progress --results-directory /tmp/dw-285-full --report-xunit-trx --report-xunit-trx-filename full.trx
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-285-held-submission-acceptance.md
git diff --check
```

- Initial build: exit 1, CS0029 because `ShouldNotBeNullOrWhiteSpace` returns void. Corrected the test to obtain the non-null string separately and retain the whitespace assertion; no production change. Log: `/tmp/dw-285-debug-build.log`.
- Final Debug source build: exit 0, zero errors, two existing NU1903 warnings for pinned SignalR.Common 10.0.12. Log: `/tmp/dw-285-debug-build-final.log`.
- Focused retained-flow suite: exit 0, 195 passed, zero failed/skipped, including all 12 new cases. Log: `/tmp/dw-285-focused.log`; TRX: `/tmp/dw-285-focused/focused.trx`.
- Full UI: exit 0, 5,967 passed, zero failed/skipped, 1m45s. The unchanged minimum-test floor is exceeded. Log: `/tmp/dw-285-full.log`; TRX: `/tmp/dw-285-full/full.trx`.
- Story-gitlink validation: exit 0, no references pointer changes relative to the recorded entry baseline. Whitespace validation: exit 0. Git reports its existing CRLF-to-LF normalization advisory for the test file; checkout C# CRLF is preserved.

## Pre-edit Aspire qualification

Using the permitted repository Aspire skill, `aspire ps --format Json` found another repository's running AppHost. It was left running. These commands started and stopped only the exact Tenants AppHost in isolation:

```bash
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire wait tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 45
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
```

All commands exited 0. Initial resource inspection showed startup still in progress. The UI became healthy in 15.8 seconds; final inspection showed all 23 resources Running/Healthy. Initial telemetry contained 110 entries, including 78 warnings and three errors, so this is a qualified pre-edit baseline, not full runtime acceptance. No browser, data mutation, orchestration edit or dependency update ran. Raw output remains in `/tmp/dw-285-aspire-*`; dashboard login credentials are omitted here.

## Scope, provenance and limits

Entry HEAD is `8a678be7d05985ab366eae0156c4094a02d90b02` on clean `main`. This is a test-only acceptance reconciliation; the runtime flows, shared platform, resources, browser assertions, build inputs and gates retain their implementations. Debug source verification is the relevant lane. Release, browser, broad solution and strict package checks are not rerun for this test/documentation change. Prior broad/package and release holds remain in [DW-337 evidence](dw-337-validation-evidence.md); earlier evidence does not establish current full release readiness.

Preserve all historical findings, decisions and B1–B8/DW evidence. Archive and sprint tracking are unchanged. No staging, commit, push, branch change, submodule initialization/update, deployment or persisted-data repair is authorized or performed.

## Independent review

The context-free blind reviewer computed its floor as `min(floor(sqrt(12.156) + 1), 10) = 4` and returned four test-verification gaps. Parent triage accepted all four as medium and patched them: actual gateway ID/intent, remaining baseline evidence, exact attempt/lease/owner continuity, and completion messages. No runtime defect or additional deferred work was identified. The reviewer independently rechecked all four patches and reported them addressed with no regressions found. Full finding-by-finding triage remains in [the owning spec](spec-dw-285-held-submission-acceptance.md).

The reviewed build passes with zero errors and the same two advisory warnings. Final reviewed focused/full UI tests pass 195/5,967 respectively, with no failures/skips. Earlier passing results above are retained as pre-review evidence. Final commands retain the same build/test flags and minimum-test floors:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantRetainedCommandFlowTests' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/dw-285-reviewed-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 5954 --no-ansi --no-progress --results-directory /tmp/dw-285-reviewed-full --report-xunit-trx --report-xunit-trx-filename full.trx
```

All three exit 0. Raw logs: `/tmp/dw-285-reviewed-build.log`, `/tmp/dw-285-reviewed-focused.log`, `/tmp/dw-285-reviewed-full.log`. Final TRX reports are in the corresponding reviewed results directories. Reviewed full duration is 1m31s. The final test checkout SHA-256 is `584b2518f5a1ccda38085edb9659d188eaa0afccf86c2d7dc51ffc9d57bdced8`; C# CRLF is preserved. Captured runtime/archive/sprint checkout hashes remain identical.

Only DW-285's disposition and maintenance row close: all 643 IDs remain, 338 done/305 open, indexed by 273 rows. The reconciled subset is 134 done/217 actionable/15 blocked/26 awaiting-decision/nine accepted limitations/38 uncertain. Original findings and pre-closure verification/actions remain in the record; every other record and remaining index row is preserved. Release remains not ready.

Final independent XML audit confirms all 12 new rows passed in both reviewed reports; focused/full TRX SHA-256 values are `70964e957ec18e133fb06c58f20c67dae959f7e5315117644bdaa018440915ca` and `c84017afa44583d7432be7dd3bc7a454f06ace39b25ff5166f04569d6e9cc513`. A read-only Python comparison against entry HEAD proves DW-285 is the only changed ledger record and every other index action row is identical. Index references cover the 305 open IDs exactly once. Final story-gitlink and whitespace checks both exit 0. Root HEAD remains the recorded baseline; changes remain uncommitted.
