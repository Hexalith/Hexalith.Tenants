# Tenant read/paging validation — 2026-10-22

Closes DW-602, DW-603, DW-610, DW-611 and DW-619 against clean entry revision `165bd76c908003fa92008632360ff8335cfd997a`. The user requested bmad-build, five deferred items, and a push on main.

The REST client and gateway both reject summaries exceeding the normalized requested page size before detail enrichment, including custom typed clients. Valid boundaries and matching retained evidence remain supported. Refresh callbacks use the existing shared finite deadline; actual per-registration ownership prevents overlap, queues catch-up until late completion, and refuses restart after lease/circuit detachment. Both membership Next handlers require usable arrived evidence, HasMore, a nonblank cursor and no ongoing transition. EN/FR lookup controls, announcements, tenant-grid sort headers and helper text explicitly describe current-page sorting. The ledger explicitly permits this scoped alternative for DW-611 and DW-619. Two interleaved pages qualify the preserved local sorting behavior; this change does not implement global ordering.

## Checks

```bash
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json

dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore

dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --filter-class '*CLASS' --minimum-expected-tests 1 --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/tenants-read-paging/CLASS

python3 _bmad-output/implementation-artifacts/dw-602-603-610-611-619-verify.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-602-603-610-611-619-read-paging.md
```

Aspire startup succeeds. A later sanitized description reports all executable/project resources Running, security Healthy, and no waiting resources. This is local baseline inspection, not acceptance evidence for browser transport or identity-provider integration.

The Debug source build passes with zero warnings/errors. The six focused classes pass 1,065 cases, zero failures/skips: REST client 128, query gateway 724, subscription 34, My Tenants 28, lookup 35, tenant list 116. Logs and TRX files are under `/tmp/tenants-read-paging/`. An intermediate new-test build failed CS8604 on a nullable selector label; an explicit non-null assertion corrected it. The first tenant-list class run failed four new localization fixtures because service registration followed container initialization; registering the localizer before initialization corrected all four. Initial failures are retained in `validation-history.txt`, `TenantListSurfaceTests.initial-failure.log` and `TenantListSurfaceTests/initial-failure.trx`.

The first complete-suite run also detected two stale lookup localizer fixture values after the intentional resource change. The fixture and its original positive/negative announcement assertions now use the shipped current-page wording. The reviewed broad and final acceptance results are recorded below.

## Ledger preservation

The committed verifier reconstructs each selected original record from its preserved `pre-read-paging-` fields, requires all 652 unrelated record bodies and the prior preamble unchanged, and verifies the exact maintenance-index edits. It passes: 657 IDs, 515 done, 142 open, 132 index rows covering each remaining open ID exactly once. Archive, sprint tracking, release-readiness artifact and dependency pointers remain unchanged.

## Review and initial broad gate

Three independent reviewers examined the full diff. They identified obsolete paging transition ownership, omitted sort-reset disclosure, and opportunities to strengthen ordering/disposal/completion tests. The review patch addresses these and moves count rejection ahead of item traversal. A scope-wide catch-up can intentionally cause a redundant healthy authoritative reread; triage accepts that small optional-nudge cost rather than adding per-registration queues. Callback deadlines apply to returned asynchronous work; callbacks must return promptly without synchronous blocking. Owned callbacks use the renderer dispatcher. No synchronous-blocking containment or preemption is claimed.

The first complete maintained run finished **7,256 cases: 7,252 passed, four failed, zero skipped**, 4m10.806s. Besides the corrected localizer parity fixture, two unchanged browser subprocess tests exceeded their existing 15-second bounds and a notification-render check exceeded its existing assertion window. After the clean parent rebuild (zero warnings/errors), all failed cases pass unchanged directly: localizer parity 3/3, browser bounds 2/2, notification 1/1; lookup 36/36 and subscription 35/35 also pass. No timing threshold or gate was relaxed. The earlier focused class logs predate three final facts; the first complete run already included them. Exact full-run inventory reconciliation qualifies the final counts.

Independent edge-case recheck accepts both generation ownership repairs and the clarified callback contract, with no concrete prior finding remaining. Final review-build output records two transient MSB3026 retries copying EventStore source outputs, zero errors; the subsequent serialized test-project rebuild passes with zero warnings/errors. Neither dependency content nor any threshold changed.

## Reviewed complete run and inventory

The reviewed complete run passes all **7,264/7,264** cases, zero failures/errors/skips/not-executed, 3m04.148s. Its command exited 9 only because the agent supplied an incorrect `--minimum-expected-tests 7266`. Parsed TRX case inventories prove the first full run had 7,256 cases, the review adds exactly eight, and no case is removed. The comparison uses a multiset because some theory display names coincide. `test-inventory-reconciliation.json` retains every added case identity. Final acceptance reruns the same maintained command with the verified minimum 7,264; no test, timeout or repository gate was changed.

The next full run used the reconciled minimum and executed 7,264 cases: 7,263 passed and one existing finite-operation test observed two still-retiring callback timers immediately after the rendered state became visible (2m01.537s). The new wrapper owns asynchronous cleanup beyond that render boundary. The narrow test repair uses its existing `WaitForAssertion` mechanism for the same zero-timer assertion; caller cancellation, authority independence, read counts and subscription ownership assertions remain unchanged. No timeout threshold was increased. Its test-only rebuild passes with zero warnings/errors.

The repaired timer case passes 1/1, zero failures/skips. Independent verification recheck confirms this waits for asynchronous completion without masking a leaked timer: it neither advances the manual clock nor weakens cancellation, authority, ownership, counts or timeout thresholds.

## Final acceptance

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -m:1 -p:UseHexalithProjectReferences=true -p:BuildProjectReferences=false --no-restore

dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --minimum-expected-tests 7264 --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/tenants-read-paging-review/final-full

python3 _bmad-output/implementation-artifacts/dw-602-603-610-611-619-verify.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-602-603-610-611-619-read-paging.md
git diff --check
node_modules/.bin/commitlint --edit /tmp/tenants-read-paging/commit-message.txt --verbose
```

Final complete UI acceptance: **7,264/7,264 passed, zero failures/errors/skips/not-executed; exit 0**. All 47 new cases against the entry baseline are included. The review-focused filters pass **1,079/1,079**, including the 12-case unavailable-query companion matched by the gateway class filter and 3 localizer-parity cases. The isolated timer case also passes. The final test-project build has zero warnings/errors; its dependencies had already been built from source. Initial broad/focused failures, transient source-build copy retries and the erroneous inferred minimum remain recorded above. No new review work was deferred.

Pinned commitlint **21.2.3**, matching the installed lockfile, accepts the exact full candidate `fix(ui): resolve five deferred read paging records` with **zero problems, zero warnings**. Fetch succeeded and local main matched origin/main before committing.

[Raw logs, TRX files and tested-source manifests](dw-602-603-610-611-619-raw-logs.zip) preserve initial and final evidence. The accepted manifest binds the final source files and assembly to final acceptance, separately from the pre-timer-repair focused manifest. Release holds, dependency pointers and runtime qualification limits remain unchanged.
