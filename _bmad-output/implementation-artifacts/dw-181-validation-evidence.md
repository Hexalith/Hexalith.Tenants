# DW-181 validation evidence — 2026-10-21

Entry revision: `8ca1db8d5662d94272811951803974b8ed1c3e0d`, clean main. The user requested the next deferred work and explicitly authorized commit/push on main. DW-181 is the next dependency-free actionable Tenants record after DW-178; DW-180 remains uncertain and open, and lower records retain their ownership/dependencies/dispositions.

## Scope and runtime proof

The existing detail page cancels superseded operations immediately and lets each `PendingOperation` dispose its source through `CompleteCancellation`. This change adds coverage without changing production behavior. The initial three rendered cases hold the second real detail query, then trigger a public metadata refresh, change the route, or unmount through bUnit's `DisposeComponentsAsync`. Independent review expands this to six cases with metadata-refresh and shared parameter-load owners, held successors and actual request-identity assertions, as recorded below.

Each case observes token cancellation while the gateway remains held, verifies a signalled usable wait handle and a newly registered cancellation callback, then releases and awaits the owning refresh callback. Wait-handle access must throw `ObjectDisposedException` after completion. The refresh/route cases retain the replacement configuration and correct tenant identity after the obsolete result arrives. The unmount case checks no subsequent renders or unhandled renderer exception. Gates release in finally; review adds explicit bounds to harness teardown/dispatcher waits as well as transition/completion waits. No private-field reflection, arbitrary delay, source-text assertion or production API is added.

This is component/runtime regression evidence, not browser, distributed-functional, deployment or release qualification. Only DW-181 is in scope; existing release holds remain.

## Source build and focused checks

Commands run from the owning repository root; logs are under `/tmp/dw181-*.log`, and TRX files under `/tmp/dw181/`.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

Baseline (`dw181-build-baseline.log`) and initial new-test build (`dw181-build-tests.log`) each exit 0 with zero warnings/errors. During the run another process advances six root dependency checkouts. These are source-checkout builds; no qualification of the original declared dependency pins is claimed. This session runs no dependency update and will not stage external gitlink changes.

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/dw181/focused --filter-method '*DetailReadCancellationLifetimeExtendsThroughCompletion*' --minimum-expected-tests 3
```

Initial focused result: exit 0, 3 passed, zero failed/skipped.

## Aspire baseline

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json --non-interactive
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --severity Error --limit 5 --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
```

Initial ps is empty. Start succeeds; first describe has waiting resources and unhealthy security. Final describe reports all 23 resources Running/Healthy. The bounded error sample is inspected locally without retaining credentials or tokens in tracked evidence. The app started by this session stops successfully. This records resource readiness, not live functional proof.

## Full maintained UI suite

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/dw181/full --minimum-expected-tests 6621
```

Exit 0; TRX counters: 6621 passed, zero failed/skipped. This includes the existing detail cancellation, authentication, supersession and generation regressions. The Aspire error sample includes Memories tenant-registry initialization failures with HTTP 500 during startup; healthy resource status does not establish successful initialization and no such claim is made.

## Isolated harmful controls

Controls use a disposable local shared clone at `/tmp/dw181/control`, detached at the full entry revision above, with a byte-identical copy of the new test file. Link only the root-declared dependency directories to the live workspace; do not initialize submodules. The first symlink-path build fails with 329 dependency diagnostics (beginning with FrontComposer CS1591; also CA1707 and downstream CS0006). This is a control-harness path failure, not a test result or maintained build failure. The corrected invocation supplies canonical physical dependency roots:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -p:HexalithFrontComposerRoot=/home/administrator/projects/hexalith/tenants/references/Hexalith.FrontComposer -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/tenants/references/Hexalith.EventStore -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/tenants/references/Hexalith.Commons -p:HexalithMcpCliRoot=/home/administrator/projects/hexalith/tenants/references/Hexalith.McpCli -p:HexalithMemoriesRoot=/home/administrator/projects/hexalith/tenants/references/Hexalith.Memories -m:1 -v:q
```

Run this from the isolated checkout after each exact mutation below. Then use the focused test command above from that checkout, changing only `--results-directory` to the matching `/tmp/dw181/control-early` or `/tmp/dw181/control-late` directory. Start each mutation from original entry-revision `TenantDetailPage.razor`; do not stack mutations or touch the live production file.

- Premature disposal: append `_loadCancellation?.Dispose();` immediately after each of the two `RetireCancellationCore(_loadCancellation);` statements, in `ReplaceActiveCancellation` and `CancelActiveCancellation`. Corrected build exits 0 with zero warnings/errors. Focused run exits 2: all three cases fail at their held-token `WaitHandle` access with ObjectDisposedException, zero skipped. This proves the lifetime assertions detect disposal before the held operation completes.
- Missing completion cleanup: remove only `source.Dispose();` inside `CompleteCancellation(CancellationTokenSource source)`, preserving cancellation and every other disposal path. Build exits 0 with zero warnings/errors; focused exits 2 with all three initial cases failing the expected post-completion ObjectDisposedException assertion, zero skipped.

The initial failed symlink-path build is retained as `/tmp/dw181-control-early-build.log`; the corrected successful build is `dw181-control-early-build-canonical.log`, and the failing test is `dw181-control-early-test.log`.

## Git ownership and inherited sync

While qualification ran, concurrent external commit `ec10b2575c836d650a68ffcbe14a16ccc9257528` captured the initial new test/spec and six dependency updates. Preserve that commit. The spec keeps its true entry baseline and records every full inherited dependency target in its completion table. This session ran no dependency update and stages no dependency paths. The earlier default and explicit-HEAD own-spec checks both exit 1 with six undeclared pointers before that attribution is added (`dw181-gitlinks-working.log`, `dw181-gitlinks-recorded.log`). The unchanged guard then exits 0 with all six correctly declared targets (`dw181-gitlinks-inherited.log`). The failure is resolved by honest inherited-change attribution, without changing the baseline or validator.

Scoped formatting exits 0:

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --verify-no-changes --include tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.CancellationLifetime.cs --verbosity quiet
```

Reconciliation verification retains all 656 IDs and modifies only the DW-181 entry body; 402 done/254 open, exactly covered once by 229 maintenance rows. Archive and sprint files are byte-identical to entry. Prior findings and original DW-181 status/disposition/evidence fields are retained with `pre-dw-181-` names. Existing release qualification holds remain in force.

## Independent review corrections

The configured Blind Hunter reviewer returns five concrete gaps. All are accepted as test/evidence corrections: bound teardown/dispatcher waits, hold a separate successor to establish prompt cancellation, bind route expectations to actual query requests, cover parameter-owned shared detail/member reads, and preserve a successor cancellation source after its predecessor completes. The reviewed theory has six cases: metadata refresh or parameter load, each superseded by refresh/route/unmount. Parameter cases invoke the actual public SetParametersAsync lifecycle and keep members held after detail returns. Four non-disposal cases finish the old owner while the successor remains held, then cancel it through a further load and preserve the final replacement projection.

The first corrected-source build (`dw181-build-reviewed.log`) exits 1 with zero warnings/one CS1061: DisposeComponentsAsync returns Task, so AsTask is invalid. Remove that conversion and rebuild; no production source changes are needed. Earlier three-case and 6621-case evidence remains historical and does not describe the final six-case file.

## Final control-harness and environment limits

The initial three-case missing-cleanup control fails all three post-completion disposal assertions (exit 2, zero skips), with a zero-warning/error build. The expanded six-case premature-disposal control uses a disk-saving runner linked to existing dependency binaries and substitutes the isolated mutated UI assembly. Its first runner entry is a symlink: direct xUnit invocation exits 0, six passed, because .NET resolves the live entry assembly rather than loading the intended mutated UI. Treat that as invalid control evidence. Copy the test DLL, deps.json and runtimeconfig.json into the isolated runner as real files while keeping the isolated UI substitution; the corrected direct command then exits 1 with six expected ObjectDisposedException failures at held-token WaitHandle access, zero skips:

```bash
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -method '*DetailReadCancellationLifetimeExtendsThroughCompletion*' -result-xml /tmp/dw181/control-early-six-copied.xml
```

That expanded control is before the final additional pre-fourth-load stale-marker assertion; it is not claimed to use the final byte-identical test file. No expanded six-case missing-cleanup or successor-detachment control is executed or claimed. The maintained final tests themselves assert successor cancellation and cleanup.

During final qualification the disk fills completely. The first revised project-runner command and direct-assembly fallback each abort with exit 134 and empty redirected output (`dw181-focused-reviewed.log`, `dw181-focused-reviewed-direct.log`). `df -h /tmp /home/administrator` reports zero available space. Reclaim only this task's temporary isolated binaries and copied documentation; the unchanged focused retry then passes six of six, zero failed/skipped (`dw181-focused-reviewed-retry.log`). The next full run passes 6624/6624, zero failed/skipped (`dw181-full-reviewed.log`). Its TRX counters were checked before superseded TRX directories and the task-owned disposable clone were removed for space; logs and recorded counters remain. No user files or shared caches are cleaned.

A proposed remaining-control runner cannot be written when the disk fills again; it runs no further control. Preserve the explicit qualification limits above. A first UI-only six-case build is inadvertently invoked in the live owning repository rather than the isolated clone and supplies no mutation evidence; the corrected isolated build has zero warnings/errors. Neither command edits live production source.

Independent correction recheck accepts the original five findings, then asks for the original stale-marker check before the fourth load. Add it and obtain reviewer confirmation of all corrections with no further issue. The first build of that extra assertion fails with CS0136 (foreach local `table` conflicts with the later local); rename it `renderedTable`. This changes test naming only. The final corrected-source checks are recorded below.

## Final accepted qualification

The final source build is `dw181-build-final-corrected.log`: the ordinary Debug source-build command above exits 0 with zero warnings/errors. Run the focused project-runner command with directory `/tmp/dw181/focused-final`, the same method filter and `--minimum-expected-tests 6`: exit 0, six passed, zero failed/skipped. Run the full project-runner command with directory `/tmp/dw181/full-final` and `--minimum-expected-tests 6624`: exit 0, 6624 passed, zero failed/skipped. Both final TRX counter sets are checked explicitly. These final executions include the last pre-fourth-load stale-marker assertion and its corrected local name.

The final scoped formatting command exits 0 (`dw181-format-final.log`). The default own-spec gitlink command exits 0 (`dw181-gitlinks-final.log`), binding all inherited targets while keeping the true entry baseline. `git diff --check` passes. Reconciliation and frozen-intent comparisons pass, preserving all unrelated entry bodies, archive and sprint. Review/recheck accepts all six corrections and identifies no remaining issue; nothing new is deferred.

The owning repository's lock-installed commitlint CLI is 21.2.3. Exact full candidate validation succeeds before use:

```bash
node_modules/.bin/commitlint --edit /tmp/dw181-commit.txt --verbose
```

Input is `test(ui): qualify detail cancellation lifetime` with a trailing newline. Result (`dw181-commitlint.log`): exit 0, zero problems and zero warnings. Fetch inspection shows main and origin/main aligned at the inherited external sync before this completion commit. Stage only the five task-owned test/spec/evidence/maintenance paths; no dependency path or unrelated change belongs to this completion. Existing release qualification holds remain unchanged.
