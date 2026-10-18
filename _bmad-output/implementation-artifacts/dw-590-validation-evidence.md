# DW-590 validation evidence — 2026-10-18

Entry revision: `2d733caf0c989d3531d82bd9e27d40fef12fc1f4`. Scope: recheck live main after recovery artifact preparation and existing-package hash verification, immediately before publication. Every publication tool in executable fixtures is intercepted locally; no live recovery, package push, container push or GitHub Release creation occurs.

## Behavior and regression evidence

The publisher retains its initial stored-source, version, inventory and live-source guards. A second exact main-SHA comparison now follows the existing-package hash loop and precedes the missing-package loop, container login/publication and GitHub Release handling. API failure, empty/malformed output and a changed SHA fail closed. The check proves main at that boundary; it cannot make later remote operations atomic with future branch movement. Separate preparation/publication separation, credentials, artifact provenance and recovery eligibility records remain open.

The first 33 executable cases failed against original production: all lacked the required final source read, and all 29 unsafe-source cases continued to publication. Four otherwise supported stable cases failed the missing-read sequence assertion. After the three-line addition, all 33 passed. The first maintained owning-suite run passed 384/384, zero failures/skips.

Independent review accepted the production placement and identified five fixture gaps. All were patched and independently rechecked without a residual finding: complete five-package existing inventories; genuinely empty existing inventories; an executable mismatched-hash refusal; initial source/version/inventory/stale-main refusals; and exact package identities, source target, release assets and container destination/options. The expanded suite has **46 cases**, including drift during the final of five hash comparisons and all-five-missing publication. Every run compares retained recovery-state filenames and contents. The preservation hash control does not close separately scoped DW-608.

The initial build exited 1 with **CS1929** at the collection assertion because the expected list did not select the intended Shouldly overload. Materializing the expected array corrected the fixture. Corrected initial and final Debug source builds exit 0 with **zero warnings/errors**.

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-590/baseline-build.log 2>&1
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-590/baseline-build-corrected.log 2>&1
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests' -noColor -xml /tmp/dw-590/baseline-focused.xml > /tmp/dw-590/baseline-focused.log 2>&1
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests' -noColor -xml /tmp/dw-590/focused.xml > /tmp/dw-590/focused.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 384 --no-ansi --no-progress --results-directory /tmp/dw-590/contracts --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-590/contracts.log 2>&1
```

The first direct commands used deprecated but accepted `-xml`; subsequent execution uses supported `-result-xml`.

Final expanded focused execution exits 0: **46 passed / 0 failed / 0 skipped**. Final maintained Contracts execution exits 0: **397 passed / 0 failed / 0 skipped**. Scoped formatter validation exits 0.

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-590/review-build.log 2>&1
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests' -noColor -result-xml /tmp/dw-590/review-focused.xml > /tmp/dw-590/review-focused.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 397 --no-ansi --no-progress --results-directory /tmp/dw-590/contracts-final --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-590/contracts-final.log 2>&1
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs --verify-no-changes > /tmp/dw-590/format-final.log 2>&1
```

## Adversarial controls

The compiled focused suite also runs against copied scripts in an isolated temporary repository fixture, without mutating production. `python3 /tmp/dw-590/check-mutations.py` exits 0, with these results:

| Copied publisher | Failed cases / 46 |
| --- | ---: |
| Pristine control | 0 |
| Omit final gate | 41 |
| Gate before hash checks | 34 |
| Gate inside hash loop | 26 |
| Accept failed final API lookup | 4 |
| Omit existing-package hash refusal | 1 |
| Wrong release source target | 3 |
| Repeated package instead of each missing ID | 3 |

All seven harmful mutations exit 1; the pristine control exits 0. Exact summaries are in `/tmp/dw-590/mutations.json`. No mutation is applied to the working tree.

## Required runtime snapshot

Aspire discovery found an unrelated running FrontComposer AppHost and left it running. The isolated Tenants start, describe and telemetry capture exit 0. Memories resources are healthy; security is unhealthy and dependent services are waiting. This snapshot is environment evidence, not healthy-topology or release qualification.

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json > /tmp/dw-590/aspire-start.json 2>&1
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-590/aspire-describe.json 2>&1
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-590/aspire-otel-logs.json 2>&1
```

The initial stop command exits 1 because `aspire stop` does not accept `--format Json`. The corrected command exits 0 and stops only the task-owned Tenants instance. Final discovery confirms the unrelated FrontComposer instance remains running.

```bash
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-590/aspire-stop.json 2>&1
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive > /tmp/dw-590/aspire-stop-corrected.log 2>&1
aspire ps --format Json > /tmp/dw-590/aspire-final-ps.json 2>&1
```

## Scoped closure and remaining limits

Only DW-590 closes. All 653 original IDs and original fields remain, with replaced fields retained under `pre-dw-590-*`. Final accounting is **370 done / 283 open**, including **196 actionable**, across **255 exactly covering maintenance rows**. Unrelated record bodies/index rows, archive, sprint tracking and dependency pointers remain unchanged. Separate release/provenance/security/operator holds remain; this change does not establish release readiness.

Shell syntax, whitespace, story-gitlink validation and the direct baseline pointer comparison pass:

```bash
bash -n scripts/publish-partial-release.sh
git diff --check
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-590-final-recovery-source-check.md > /tmp/dw-590/story-gitlinks.log 2>&1
git diff --exit-code 2d733caf0c989d3531d82bd9e27d40fef12fc1f4 -- references/
```

`python3 /tmp/dw-590/validate-closure.py > /tmp/dw-590/closure.json` exits 0. The checker compares each original ledger record and index row against the canonical entry revision, requires exact open-ID index coverage, retains every original DW-590 field and verifies unchanged archive, sprint and dependency pointers.

The build workflow includes a local Conventional Commit. Pinned commitlint 21.2.3 validates the exact full candidate with **zero problems/warnings** before use; validation evidence is retained in `/tmp/dw-590/commitlint.log`.

```bash
node node_modules/@commitlint/cli/cli.js --edit /tmp/dw-590/commit-message.txt --verbose > /tmp/dw-590/commitlint.log 2>&1
```

No push or publication is authorized or performed by this run.
