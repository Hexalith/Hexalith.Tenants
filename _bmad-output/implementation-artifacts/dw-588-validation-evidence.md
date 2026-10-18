# DW-588 validation evidence — 2026-10-18

Entry revision: `784b468e455150933585fec7b84fee2a18f3713e`. Scope: reject an existing GitHub Release with a missing corresponding tag during partial-release preflight, before destination reads or recovery-state preparation. No live publication or recovery is performed.

## Behavior and regression evidence

The existing tag branch already proves lightweight/annotated tag source binding. The new guard in the release-200 branch requires that proven tag before reading release metadata or preparing package/container recovery. Existing draft/tag-name checks and absent-release paths remain. Separate recovery concerns, including DW-581 and DW-607, are unchanged.

Sixteen cases execute the real validator with local Bash `gh`/`curl` fixtures and real jq. Two missing-tag cases cover fresh and pre-existing recovery state; five compatibility cases cover lightweight/annotated tags, absent releases and the existing both-absent inventory. Nine refusal controls cover stale source, absent successful CI, unknown tag/release statuses, wrong lightweight/annotated source, unsupported tag type, draft release and inconsistent release tag. Refusals assert no destination reads and no new/changed recovery state. Successful cases assert exact destination URLs and persisted source, version, missing-package inventory and package/container state.

The first test build exited **1** with **CS1503** at the new Shouldly assertion: its second positional argument selected the wrong overload. Naming `customMessage` corrected the fixture. The next Debug source build exited **0**, **zero warnings/errors**.

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-588/build.log 2>&1
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-588/baseline-build.log 2>&1
```

The first direct runner command incorrectly combined xUnit `-class` with MTP options; it exited **3**, `unknown option: --no-ansi`, without running tests:

```bash
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleaseRecoveryTests' --no-ansi --no-progress --results-directory /tmp/dw-588/baseline-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/dw-588/baseline-focused.log 2>&1
```

The corrected direct command against original production exited **1**: **14 passed / 2 failed / 0 skipped**. Both new defect cases proceeded to all six package/container destination reads and returned success, proving the defect. This log overwrote the earlier invocation-error log; the initial error is retained above.

```bash
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleaseRecoveryTests' -noColor -xml /tmp/dw-588/baseline-focused.xml > /tmp/dw-588/baseline-focused.log 2>&1
```

After the one-line guard, the focused direct run exited **0**, **16 passed / 0 failed / 0 skipped**. The maintained project-level MTP run exited **0**, **349 passed / 0 failed / 0 skipped**; this includes all sixteen new cases. Direct execution supplies focused reproduction; the project run supplies owning-suite acceptance.

```bash
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleaseRecoveryTests' -noColor -result-xml /tmp/dw-588/focused.xml > /tmp/dw-588/focused.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 349 --no-ansi --no-progress --results-directory /tmp/dw-588/contracts --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-588/contracts.log 2>&1
```

Shell syntax, whitespace diff and scoped formatter checks exit **0**:

```bash
bash -n scripts/validate-partial-release-recovery.sh
git diff --check
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PartialReleaseRecoveryTests.cs --verify-no-changes > /tmp/dw-588/format.log 2>&1
```

## Concurrent workspace changes and attribution limit

Another workspace operation created `6d36b3446e79607579bf4bee326adf4036fa960c`, committing the initial spec/test draft and six unrelated dependency-pointer changes. It then created `a402d96b4210a2943c8570f82ee73c310d3113c2`, capturing the production guard and assertion correction. These external commits are preserved. Their pointer moves are not declared as DW-588 work, and the original baseline remains unchanged.

The required historical story-gitlink check exits **1**, reporting the six external `references/` pointer updates as undeclared. No suggested reset/checkout correction was executed. A separate exact diff after the external pointer commit is empty; this distinguishes the bounded task check from the failing historical-range gate.

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-588-recovery-release-without-tag.md > /tmp/dw-588/story-gitlinks.log 2>&1
git diff --exit-code 6d36b3446e79607579bf4bee326adf4036fa960c -- references/
```

The second command exits **0**. The broad gate remains failed; no full historical-range pass is claimed.

## Required runtime snapshot

The Aspire skill supplied the required isolated pre-code snapshot. Discovery found no existing host. Start, describe, telemetry capture and stop each exited **0**. Memories was healthy; security was unhealthy and dependent domain services were waiting. Only this task's instance was stopped. This is environment evidence, not healthy-topology or release qualification.

```bash
aspire ps --format Json > /tmp/dw-588/aspire-ps.json 2>&1
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json > /tmp/dw-588/aspire-start.json 2>&1
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-588/aspire-describe.json 2>&1
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-588/aspire-otel-logs.json 2>&1
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj > /tmp/dw-588/aspire-stop.log 2>&1
```

## Review and maintenance reconciliation

Blind Hunter computed `min(floor(sqrt(23.570) + 1), 10) = 5` and returned five fixture findings. All were patched and independently rechecked with no residual code finding:

- Persist unexpected-call evidence independently of stderr, which production suppresses during status reads.
- Apply the real production CI jq filter to mixed exact/nonmatching source, branch, event, status and conclusion evidence.
- Initialize logs before API calls and add an invalid-source zero-call regression.
- Add absent-release compatibility with one existing package and a present container, checking downloaded content, hash and persisted classification.
- Seed and compare all eight pre-existing recovery-state files after the missing-tag refusal.

The production change remains one guard. The expanded focused suite exits **0**, **18 passed / 0 failed / 0 skipped**; the Debug source build exits **0**, **zero warnings/errors**. The maintained Contracts project and scoped formatter exit **0**, **351 passed / 0 failed / 0 skipped**:

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-588/review-build.log 2>&1
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleaseRecoveryTests' -noColor -result-xml /tmp/dw-588/review-focused.xml > /tmp/dw-588/review-focused.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 351 --no-ansi --no-progress --results-directory /tmp/dw-588/review-contracts --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-588/review-contracts.log 2>&1
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PartialReleaseRecoveryTests.cs --verify-no-changes > /tmp/dw-588/review-format.log 2>&1
```

Isolated mutation checks copy the built test output, authoritative manifest and validator into a temporary fixture root. Each mutation edits only that temporary validator and executes the actual eighteen compiled tests. No workspace source is rewritten:

```bash
python3 /tmp/dw-588/check-mutations.py
```

The driver exits **0**: pristine **18/18**; removing the admission guard produces **2 failures**; an unsupported tag endpoint produces **15 failures**, including the formerly masked unknown-tag scenario; dropping each of the five CI jq predicates independently produces **1 failure**. Every run executes all eighteen cases with zero skips. These seven harmful mutations are rejected by the suite. Logs and XML results are under `/tmp/dw-588`; the original temporary validator is restored at the end.

Final maintenance accounting exits **0**:

```bash
python3 /tmp/dw-588/check-ledger.py
```

The first accounting invocation failed because it assumed all historical done records had a disposition field. Correcting the read-only checker to classify those records from their existing done status resolved that checker error; no unrelated ledger data was changed. The passing check compares all 653 sections against the canonical entry revision, proves only DW-588 changed and proves every original replaced field survives under `pre-dw-588-*`. It verifies that only DW-588's action row was removed, that all other rows remain identical/in order and that each open ID occurs exactly once. Totals: **369 done / 284 open**, **256 rows**; open dispositions: **197 actionable**, **15 blocked**, **26 awaiting-decision**, **9 accepted limitations**, **37 uncertain**. Archive, sprint, release-readiness holds, package inventory, recovery workflow/publisher/verifier and post-external-commit dependency pointers are preserved. No finding is deferred. Release remains not ready.

Pinned and installed commitlint both equal **21.2.3**. The exact local commit candidate `fix(ci): reject recovery for releases without tags` validates with exit **0**, zero problems/warnings:

```bash
node_modules/.bin/commitlint --edit /tmp/dw-588/commit-message.txt --verbose > /tmp/dw-588/commitlint.log 2>&1
```

The invoked build workflow requires a local commit; no push, hosted workflow, release, publication or tag mutation is performed by this run. The two concurrent workspace commits described above were created by another operation.
