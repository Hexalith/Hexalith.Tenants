# DW-655 validation evidence — 2026-10-18

Entry revision: `c9feedb6656d70c7271e851c844ee29bdfb28c3e`, clean `main`. Scope: process the final unterminated existing-package ID before recovery publication. All publisher executions use isolated fixture state and intercepted publication tools; no real package, container or release is published.

## Regression and implementation

Twenty new cases use the production publisher: six final omitted-hash records, two single-ID absent-hash-file refusals, six final content mismatches and six matching-hash controls. Each destination inventory is exercised with one existing ID or several IDs whose preceding hashes remain valid. The fixture retains reversed per-ID hash records, distinct package bytes, exact hash/publication call sequences, refusal diagnostics, process exits and unchanged state-file names/contents.

Before the runtime fix, the Debug source build succeeds with **zero warnings/errors** and focused execution reports **116 passed / 20 failed / 136 total**, with zero skips/errors. All 20 failures are the added unterminated cases. The one-line repair extends only the existing-package verification loop to process a nonempty final `read` result at EOF. The preflight producer, missing-package loop, manifest, workflow and dependencies are unchanged.

```bash
mkdir -p /tmp/dw-655
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-655/build-red.log 2>&1
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests -noColor -result-xml /tmp/dw-655/original-focused.xml > /tmp/dw-655/original-focused.log 2>&1
```

These commands document the pre-fix run. Rerunning them against the repaired working publisher exercises the fix. The replay below obtains the original publisher directly from Git without changing working files.

## Reproducible isolated replay

```bash
python3 _bmad-output/implementation-artifacts/dw-655-check-variants.py --output-directory /tmp/dw-655/variants > /tmp/dw-655/variants.log 2>&1
```

The tracked replay performs a fresh Debug source build, copies the test assembly/dependencies into a temporary fixture root, and runs current, exact entry-revision and deliberately overbroad publisher variants. It invalidates previous summary output before starting, uses fresh per-run logs/XML, checks exact case identities, failure sets/mechanisms, process exits and zero skips/errors, and records publisher/test-source/assembly SHA256 values in `results.json`. Original-publisher failures explicitly contain intercepted publication continuation; matching cases also require the final package's actual hash call. The mutation rejects every unterminated inventory and must fail all six matching-evidence controls while canonical and empty-inventory controls still pass.

| Isolated publisher | Passed | Failed | Skipped/errors | Process exit |
| --- | ---: | ---: | ---: | ---: |
| Current production | 28 | 0 | 0 | 0 |
| Entry-revision production | 8 | 20 | 0 | 1 |
| Reject all unterminated inventories | 8 | 6 | 0 | 1 |

The replay build has **zero warnings/errors**. Original publisher SHA256: `1db1d26e5decf9f95474858933bd1ec206d6b768140bee6820e899da6e634f6b`. Repaired publisher SHA256: `24b340454f2377e437fbca7caa20020d6d516cc8dec0132d42b055b060b4506e`.

## Initial maintained verification

Initial post-fix focused execution passes **136/136** and the full maintained Contracts project passes **500/500**, both with **zero failures/skips**. Scoped C# whitespace formatting, shell syntax, Python compilation, story-gitlink and entry-revision dependency checks pass.

```bash
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests -noColor -result-xml /tmp/dw-655/focused.xml > /tmp/dw-655/focused.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 500 --no-ansi --no-progress --results-directory /tmp/dw-655/contracts --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-655/contracts.log 2>&1
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs --verify-no-changes > /tmp/dw-655/format.log 2>&1
bash -n scripts/publish-partial-release.sh
python3 -m py_compile _bmad-output/implementation-artifacts/dw-655-check-variants.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-655-unterminated-recovery-inventory.md
git diff --exit-code c9feedb6656d70c7271e851c844ee29bdfb28c3e -- references/
git diff --check
```

## Pre-edit runtime snapshot

The required Aspire baseline has no initially running AppHosts. Isolated start, describe, telemetry capture and exact-AppHost stop all exit **0**. The describe snapshot reports **23 Running** resources. A final `aspire ps --format Json` returns `[]`. This is a local environment snapshot; no AppHost change or release-readiness claim follows from it.

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json > /tmp/dw-655/aspire-start.json 2>&1
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-655/aspire-describe.json 2>&1
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-655/aspire-otel-logs.json 2>&1
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive > /tmp/dw-655/aspire-stop.log 2>&1
aspire ps --format Json
```

## Scope and historical evidence

DW-608/DW-654 probes and evidence remain unchanged. The DW-654 optional truncated-inventory probe deliberately asserts the old bug's publication continuation; it is historical evidence and is expected to fail after DW-655. Use the DW-655 replay for current qualification. Inventory partition/completeness, other malformed records, missing-package parsing, remote artifacts, reproducibility and existing-release asset reconciliation remain outside this repair. DW-620 and all unrelated release holds remain open. Release remains not ready.

## Independent review corrections and final verification

All five independent review findings are patched: required replay gates use explicit exceptions that survive Python optimization; publication evidence parses the actual call array separately from expected/difference arrays; provenance captures and verifies the executed fixture assembly/publisher/manifest and checks source stability across the build/replay; final test indices derive from the manifest-backed inventory; and six additional cases change live main while hashing the final unterminated ID. No review finding is deferred.

The corrected replay build again has **zero warnings/errors**. Final focused execution passes **142/142**, and the maintained Contracts project passes **506/506**, both with **zero failures/skips**. Scoped formatting and Python compilation pass.

```bash
python3 _bmad-output/implementation-artifacts/dw-655-check-variants.py --output-directory /tmp/dw-655/review-variants > /tmp/dw-655/review-variants.log 2>&1
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests -noColor -result-xml /tmp/dw-655/review-focused.xml > /tmp/dw-655/review-focused.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 506 --no-ansi --no-progress --results-directory /tmp/dw-655/contracts-review --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-655/contracts-review.log 2>&1
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs --verify-no-changes > /tmp/dw-655/format-review.log 2>&1
```

| Corrected isolated publisher | Passed | Failed | Skipped/errors | Process exit |
| --- | ---: | ---: | ---: | ---: |
| Current production | 34 | 0 | 0 | 0 |
| Entry-revision production | 8 | 26 | 0 | 1 |
| Reject all unterminated inventories | 8 | 6 | 0 | 1 |

All 26 baseline failures include publication in their parsed **actual** calls. An additional control replaces the actual arrays with empty arrays in all six matching-hash failure messages while retaining publication markers in the expected/difference sections; the parser returns no actual publication calls for those controls.

A temporary copy of the tracked replay changes only its project-root binding and replaces the production control's publisher with the entry-revision publisher. Running it with `PYTHONOPTIMIZE=1` exits **1** at the explicit production totals check (8 passed / 26 failed), and no successful `results.json` exists. This proves optimization cannot bypass a failing qualification. Recreate that negative control with:

```bash
python3 - <<'PY'
from pathlib import Path
source = Path('_bmad-output/implementation-artifacts/dw-655-check-variants.py').read_text()
source = source.replace('repo = Path(__file__).resolve().parents[2]', 'repo = Path(' + repr(str(Path.cwd())) + ')')
anchor = '("production", publisher, all_cases, set())'
if source.count(anchor) != 1:
    raise RuntimeError('Missing production control anchor')
source = source.replace(anchor, '("production", baseline, all_cases, set())')
Path('/tmp/dw-655/replay-optimization-negative.py').write_text(source)
PY
PYTHONOPTIMIZE=1 python3 /tmp/dw-655/replay-optimization-negative.py --output-directory /tmp/dw-655/optimization-negative > /tmp/dw-655/optimization-negative.log 2>&1
```

Independent correction recheck verifies all five fixes and finds **no remaining code or replay finding**. It confirms **142/142** focused, **506/506** Contracts, **34/34** current replay and the optimization negative control's explicit failure without successful summary output. The initial 136/500/28 totals above remain historical execution evidence; the final corrected totals are 142/506/34.

## Ledger preservation and local commit validation

Only DW-655 closes. All **655 original IDs** remain: **374 done / 281 open**, including **194 actionable** records, across **253** maintenance rows covering open IDs exactly once. The original DW-655 finding and superseded fields are retained under `pre-dw-655-*`; every unrelated record body and action row, historical summary, archive/sprint file, DW-608/DW-654 artifact and dependency pointer remains unchanged. The temporary checker compares the entry revision and working tree, validates exact index coverage, and verifies protected artifacts byte-for-byte. Its source and JSON are session evidence under `/tmp/dw-655`; the durable ledger/index diff preserves the same facts for inspection.

```bash
python3 /tmp/dw-655/validate-closure.py > /tmp/dw-655/closure.json
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-655-unterminated-recovery-inventory.md > /tmp/dw-655/story-gitlinks-final.log 2>&1
git diff --exit-code c9feedb6656d70c7271e851c844ee29bdfb28c3e -- references/
git diff --check
node node_modules/@commitlint/cli/cli.js --edit /tmp/dw-655/commit-message.txt --verbose > /tmp/dw-655/commitlint.log 2>&1
```

Pinned commitlint **21.2.3** validates the exact full local candidate with **zero problems/warnings** before use. Independent review is complete, all required scoped checks pass, and no finding is deferred. No push or live publication is requested. Release remains not ready.
