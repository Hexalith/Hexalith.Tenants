# DW-177 validation evidence — 2026-10-20

Entry revision: `3ae9db014dfe52bdee4f6731465cc2a7b65701b7`, clean main. The user requested the next deferred work and authorized commit/push on main. DW-177 is the next dependency-free actionable Tenants entry after DW-160; intervening records retain their blocked dispositions.

## Change and acceptance boundary

`TenantsWorkspace.LoadAsync` retains the currently displayed nonempty snapshot only for a soft refresh with a nonblank authenticated user whose protected caller scope matches the snapshot. It preserves the rows, search ordering and metadata while marking the transient page Loading with Unknown freshness. The existing localized loading surface appears alongside the mounted grid and pager. Both pager directions and their callbacks refuse loading-time navigation; existing create and audit availability gates fail closed. The gateway still receives the unchanged last-confirmed snapshot and conditional validator, and completion uses the existing generation/disposal checks.

Initial reads, hard loads, missing/foreign caller evidence and snapshots after error/denial never resurrect earlier confirmed rows. An active search reset clears the previous scope even if a soft refresh supersedes that pending hard read. Terminal gateway results replace the retained surface through the existing behavior. No new snapshot contract, dependency, resource string, shared component or public API is added. This qualifies rendered component behavior, without claiming browser focus, shadow DOM, assistive-technology or deployed end-to-end evidence.

## Exact validation commands and results

Commands run from the owning repository root. Debug builds use source project references and keep the ordinary NuGet audit enabled:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

Initial `build-tests-initial.log`, first fix `build-first-fix.log` and expanded-test `build-focused.log` in `/tmp/dw177/` each exit 0 with zero warnings/errors. The final formatted build/result is recorded below once qualification completes.

Every test command starts with:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx
```

Append the filter/minimum below and `--results-directory /tmp/dw177/<directory>`. Local logs are `/tmp/dw177/tests-<directory>.log` except the explicit names shown. TRX counters are read directly rather than inferred from process success.

| Directory / log | Extra arguments | Result |
| --- | --- | --- |
| `original-production` / `tests-original-production.log` | `--filter-method '*SoftRefresh*' --minimum-expected-tests 18` | Exit 2; 18 executed, seven passed, 11 failed, zero skipped. Initial tests with unchanged production reproduce missing grid/pager. |
| `focused-first` / `tests-focused-first.log` | `--filter-method '*SoftRefresh*' --minimum-expected-tests 22` | Exit 0; 22 passed, zero failed/skipped. |
| `workspace-first` / `tests-workspace-first.log` | `--filter-class '*TenantsWorkspaceTests' --minimum-expected-tests 100` | Exit 0; 128 passed, zero failed/skipped. |

The dedicated cases cover ordinary/search ordering and component identity, EN/FR loading copy, Ready/Stale/Degraded retained pages, gateway previous/ETag/request continuity, create/audit fail-closed behavior and recovery, both queued pager callbacks on page two and previous-page recovery, error/unauthorized/empty completion and non-resurrection, missing/changed users, missing/foreign snapshot scope, initial loading, active-search hard reset and repeated soft-refresh late completion.

Scoped formatting uses:

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --verify-no-changes --include tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.SoftRefresh.cs src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor -v:q
```

The first check exits 2 with 390 ENDOFLINE findings, all confined to the new test file. The same scoped command without `--verify-no-changes` exits 0 and applies the configured CRLF endings; there are no unrelated formatting changes. Logs: `format-initial.log`, `format-apply.log`; final verification is recorded after completion.

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-177-retained-list-refresh.md
```

Initial check exits 0: no references pointer changes from the full baseline revision.

## Aspire baseline and limits

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json --non-interactive
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --severity Error --limit 5 --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
```

Start succeeds. Initial describe includes resources waiting for security; final describe reports all 23 Running, with the reported security health check Healthy. The bounded error sample has three existing Memories/EventStore workflow retry errors, including HTTP 500 and required authentication. Therefore this is resource-baseline evidence, not clean distributed functional qualification. The started Tenants AppHost stops successfully; the pre-existing Folders AppHost is preserved. Local output is under `/tmp/dw177/aspire-*`; no secrets or login tokens are copied into tracked evidence.

## Review corrections and isolated final qualification

The independent blind review produced six findings, all accepted: preserve warning/zero-filter mounting, clear retained rows on authentication transitions, hold terminal responses, assert both repeated-refresh previous objects/ETags, and inspect native grid identity plus rendered row/action wiring. The private original-kind field preserves warnings without changing the snapshot contract. The review recheck found one remaining Ready/zero-filter pager case; separating logical page history from button enablement and adding EN/FR Ready coverage fixes it. A second recheck accepts all six findings with no concrete remaining issue. No review finding is deferred.

The first full UI run (`full-final`, `tests-full-final.log`) exits 2: 6604 executed, 6603 passed, one failed, zero skipped. The failure is the unrelated existing `GlobalAdministratorsPageTests.OrdinaryRefreshAndProjectionNotificationShareCallerCancellationWithoutCancelingSiblingAuthority`, asserting PendingTimerCount 0 but observing 1. The exact isolated recheck below passes; the failed result remains preserved.

While work was running, external commit `75627d267296d26a9f4fee05fa0495deb110fc11` advanced main and added six unrelated authority files. The next ordinary source build (`build-reviewed.log`, same root build command above) exits 1 with zero warnings and one error:

```text
src/Hexalith.Tenants.Contracts/Queries/TenantReadAuthorityResult.cs(12,5): error CS0246: The type or namespace name 'NativeSourceProof' could not be found
```

The declared EventStore source checkout `f6b545cd02632f662136d0b4b8e6952baa7afec3` does not contain this type. Preserve the external commit and all dependency pointers. No current-main clean build claim is made. The earlier formatted source build (`build-final.log`) completed before that external addition and passed with zero warnings/errors.

To qualify the complete DW-177 change separately, create an isolated detached worktree at the full entry revision, then copy exactly the final production Razor file and new partial test file. Only declared root dependency directories are linked for read access; no submodule is initialized or updated. Build with explicit real source-root paths so compiler/configuration discovery uses the original dependency locations:

```bash
git worktree add --detach /tmp/dw177/validation 3ae9db014dfe52bdee4f6731465cc2a7b65701b7
cd /tmp/dw177/validation
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/tenants/references/Hexalith.EventStore -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/tenants/references/Hexalith.Commons -p:HexalithFrontComposerRoot=/home/administrator/projects/hexalith/tenants/references/Hexalith.FrontComposer -p:HexalithMemoriesRoot=/home/administrator/projects/hexalith/tenants/references/Hexalith.Memories -p:HexalithMcpCliRoot=/home/administrator/projects/hexalith/tenants/references/Hexalith.McpCli -m:1 -v:q
```

The first isolated attempt without those real root overrides (`build-isolated-reviewed.log`) fails with 329 analyzer/metadata errors: compiler files resolve through the symlink while configuration discovery follows aliased project paths. The corrected absolute-root command (`build-isolated-absolute-roots.log`) exits 0 with zero warnings/errors. No analyzer suppression, warning-gate change or dependency update is used.

The isolated production/test overlays are byte-identical to the final owning-worktree files. SHA-256:

```text
f197a71ba657c1b968ada99585342657113ed1f2a39505dbf777a5ceb66043f3  src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor
2797cdd22e9d507b01c20fa1eb5da6a8db42c3cb9b1ee2f39ac5940f40d8f346  tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.SoftRefresh.cs
```

Run the same test command prefix documented above from the isolated worktree:

| Directory / log | Additional arguments | Result |
| --- | --- | --- |
| `focused-reviewed` / `tests-focused-reviewed.log` | `--filter-method '*SoftRefresh*' --minimum-expected-tests 30` | Exit 0; 30 passed, zero failed/skipped. |
| `timer-recheck` / `tests-timer-recheck.log` | `--filter-method '*OrdinaryRefreshAndProjectionNotificationShareCallerCancellationWithoutCancelingSiblingAuthority' --minimum-expected-tests 1` | Exit 0; one passed, zero failed/skipped. |

Eight final cases extend the original matrix to cover EN/FR Ready/Stale/Degraded zero-match status filters and pending/resolved authentication transitions during a held read. Terminal Error/Unauthorized/Empty reads are now held before completion, and both superseding requests explicitly preserve the original confirmed object and validator. The second scoped formatting verification (`format-final.log`) exits 0 before review edits; final reviewed formatting (`format-reviewed.log`) is checked separately below.

## Closure

Final isolated full UI qualification (`full-reviewed`, `tests-full-reviewed.log`) exits 0: 6612 executed/passed, zero failures/skips, including the exact timer case that failed in the earlier full run. The 30-case dedicated lane and corrected source build use byte-identical final overlays. Reviewed scoped formatting (`format-reviewed.log`) exits 0 in the owning worktree. The own-spec gitlink check (`gitlinks-final.log`) exits 0 with no dependency pointer changes.

Ledger validation reads exact TRX counters before closure, preserves all 656 IDs and every unrelated record body, and closes only DW-177. Counts are 400 done/256 open with 231 maintenance rows covering every open ID exactly once. Archive, sprint, unrelated history and dependency pointers are unchanged. Original DW-177 status/disposition/verification/action fields are preserved under pre-dw-177 names. Local exact counters and assertions are retained in `/tmp/dw177/ledger-validation.json`.

Pinned commitlint 21.2.3 validates the exact full message using:

```bash
node node_modules/@commitlint/cli/cli.js --edit /tmp/dw177/commit-message.txt --verbose
```

Exit 0; zero problems/warnings (`commitlint.log`). The current-main NativeSourceProof build blocker is external to DW-177 and remains an explicit limitation, alongside existing release qualification holds. The isolated entry-revision UI pass does not represent a current-main all-project build.


Pre-commit fetch succeeds; HEAD and origin/main both resolve to `75627d267296d26a9f4fee05fa0495deb110fc11`, with zero divergence. `git diff --check` and the final own-spec gitlink check pass. Commit scope is the six DW-177 production/test/maintenance files; the external authority commit remains intact.
