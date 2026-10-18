# DW-608 validation evidence — 2026-10-18

Entry revision: `1dd9613e27abe935002d9d3af3a1f476bfdd73a6`. Scope: independently qualify the existing NuGet hash refusal in partial-release recovery. The production publisher is unchanged. External publication commands are intercepted by local functions; the fixture uses real `sha256sum` on local package bytes. No package/container publication or GitHub Release edit occurs.

## Acceptance behavior

The prior `ExistingGuardsRemainFailClosed("hash-mismatch")` case already covered one changed package. Its source-response fixture also rejected `hash-mismatch` after preparation, so bypassing the production guard could encounter an unrelated fixture failure. Mismatch cases now retain valid, stable source evidence. Each reviewed package has distinct contents and its own expected SHA256; hash records are deliberately reversed relative to the existing-package inventory to qualify lookup by ID.

Fifteen new cases cover every existing-package position: one in package-only recovery, all five in container-only and release-only recovery, and four existing packages plus one missing package in mixed recovery. Every case requires the exact hash-check prefix through the mismatched ID, exit 1, the package-specific refusal, no later source/release read, no package push, no container login/publication, no release creation, and unchanged state filenames/text. Six matching controls cover the existing five inventories plus the new mixed inventory and require exact successful publication calls. The earlier source, classification and inventory tests remain.

The initial Debug source build passes with **zero warnings/errors**. Before review corrections, the focused publisher class passes **71/71**, and the maintained full Contracts lane passes **435/435**, both with **zero failures/skips**. Scoped formatting, shell syntax, whitespace, story-gitlink validation and direct entry-revision pointer comparison pass. There is no original-production supplied-hash defect reproduction: current production already satisfies this guard. Final review corrections and the separately reproduced incomplete-evidence gap appear below.

```bash
mkdir -p /tmp/dw-608
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-608/build.log 2>&1
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests' -noColor -result-xml /tmp/dw-608/focused.xml > /tmp/dw-608/focused.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 435 --no-ansi --no-progress --results-directory /tmp/dw-608/contracts --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-608/contracts.log 2>&1
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs --verify-no-changes > /tmp/dw-608/format.log 2>&1
bash -n scripts/publish-partial-release.sh
git diff --check
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-608-existing-package-hash-refusal.md > /tmp/dw-608/story-gitlinks.log 2>&1
git diff --exit-code 1dd9613e27abe935002d9d3af3a1f476bfdd73a6 -- references/
```

## Reproducible copied-script controls

The final tracked runner first builds the current Debug source test project and fails on build errors. It copies the compiled output, marker solution, manifest and publisher into an isolated temporary repository, then runs nineteen mismatch cases and seven matching controls against pristine production and four intentionally harmful copies. Mutation anchors must each occur exactly once. Production files and test sources are never rewritten.

```bash
python3 _bmad-output/implementation-artifacts/dw-608-check-variants.py --output-directory /tmp/dw-608/variants-review > /tmp/dw-608/variants-review.log 2>&1
```

| Copied publisher | Passed | Failed | Skipped | Test process exit |
| --- | ---: | ---: | ---: | ---: |
| Unchanged production | 26 | 0 | 0 | 0 |
| Bypass comparison | 7 | 19 | 0 | 1 |
| Check only first existing package | 8 | 18 | 0 | 1 |
| Look up the first package's hash for every ID | 12 | 14 | 0 | 1 |
| Check hashes after package pushes | 1 | 25 | 0 | 1 |

Before review additions, the initial runner incorrectly expected 21 failures for the late-hash copy and exited 1 after observing 20. Its all-missing success control has no existing hashes and correctly still passes. Correcting that runner expectation changed no production or C# test behavior. The final runner exits 0 and retains XML/logs in a fresh `run-*` directory; `results.json` records that path, the build command and assembly/test-source/publisher SHA256 values. A prior summary is invalidated before work starts. It checks exact totals, case identities, failed-case sets, skips, errors, exit codes and the failure mechanism.

The bypass logs demonstrate the intended failure mechanism: valid source reads complete and intercepted package/container/release publication is reached, violating the exact refusal-prefix assertions. No unsupported-source-response error masks the missing guard. Wrong-ID and first-only mutations also reject matching controls, showing the per-ID content and complete-inventory checks matter.

## Pre-edit environment evidence

`aspire ps --format Json` returned no running AppHosts. The isolated Tenants start, resource/telemetry capture and exact-AppHost stop all exited 0. Memories resources were running and healthy; security was running but unhealthy, with thirteen resources waiting. This snapshot is environment evidence, not healthy-topology or release qualification.

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json > /tmp/dw-608/aspire-start.json 2>&1
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-608/aspire-describe.json 2>&1
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-608/aspire-otel-logs.json 2>&1
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive > /tmp/dw-608/aspire-stop.log 2>&1
```

## Scope and limits

The actual publisher runs against local package bytes with intercepted build, pack, validation and publication tools. These tests qualify its supplied-hash refusal behavior and ordering, not recovery-state completeness, real NuGet package reproducibility, credentials, remote registry contents or GitHub asset hashes. DW-620, unrelated deferred records and release holds remain separate. Historical DW-590 evidence remains unchanged.

## Independent review corrections

Review found four local acceptance issues, all patched: stale XML could qualify failed launches, replay could use an old assembly, aggregate totals could hide the earlier unsupported-source fixture error, and package-only recovery lacked multiple existing packages. Fresh reports and summary invalidation, an explicit source build with recorded content hashes, exact case/diagnostic checks, and four package-only mismatch positions plus one matching control address them. Every bypass failure must arise from the call-sequence assertion and show the expected intercepted publication.

Final corrected replay passes its production **26/26** control and rejects all four mutants with the tabled outcomes. Its source build has **zero warnings/errors**. Focused publisher execution passes **76/76**, and the maintained Contracts lane passes **440/440**, both with **zero failures/skips**.

The tracked negative checker seeds the successful reports, then intercepts mutant test launches so they exit 1 before producing XML. Replay refuses the missing fresh XML and invalidates the previous summary. A second in-memory runner copy restores the unrelated `hash-mismatch` fixture response only when bytes differ: all nineteen expected aggregate failures still occur, but the mechanism check rejects them. Both negative controls pass. The earlier temporary probe printed a Python invalid-escape SyntaxWarning for its wrapper string; the tracked checker uses correctly escaped bytes and runs without that warning.

```bash
python3 _bmad-output/implementation-artifacts/dw-608-check-runner-refusals.py --seed-directory /tmp/dw-608/variants-review --output-directory /tmp/dw-608/runner-refusals > /tmp/dw-608/tracked-runner-refusals.log 2>&1
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests' -noColor -result-xml /tmp/dw-608/review-focused.xml > /tmp/dw-608/review-focused.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 440 --no-ansi --no-progress --results-directory /tmp/dw-608/contracts-review --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-608/contracts-review.log 2>&1
```

The fifth finding is a pre-existing incomplete-evidence defect: production skips an existing package when its expected-hash record is absent. The tracked probe invokes unchanged production against differing local bytes, stable source evidence and intercepted destinations. `missing` exits **0**, reaching four package pushes, container login/publication and release creation. `present` supplies the reviewed hash and exits **1** at the existing package-specific refusal, before publication. This gap is deferred as **DW-654**, separate from DW-608's supplied-hash mismatch acceptance.

```bash
bash _bmad-output/implementation-artifacts/dw-608-probe-missing-hash.sh missing > /tmp/dw-608/missing-hash.log 2>&1
bash _bmad-output/implementation-artifacts/dw-608-probe-missing-hash.sh present > /tmp/dw-608/present-hash.log 2>&1
```

Final independent recheck ran the normal replay, both runner-refusal controls and both hash-evidence probes, confirmed all four corrections, and found no remaining issue in this change. It checked the focused XML/full Contracts TRX, whitespace, syntax and story-gitlink results. DW-654 remains an explicit deferral; release remains not ready.

## Closure, preservation and local commit

Only DW-608 closes. All **653 original IDs** remain plus DW-654: **654 total, 372 done / 282 open**, including **195 actionable**, across **254 index rows** with exact open-ID coverage. The closure checker compares every original record body (ignoring only blank record separators), every unrelated maintenance row and every selected original field with the full entry revision. Replaced DW-608 fields are retained under `pre-dw-608-*`. Archive, sprint tracking, production publisher and dependency pointers remain unchanged. Exact-pointer checks are independently reproducible with the commands above.

```bash
python3 /tmp/dw-608/validate-closure.py > /tmp/dw-608/closure.json
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-608-existing-package-hash-refusal.md > /tmp/dw-608/story-gitlinks-final.log 2>&1
git diff --exit-code 1dd9613e27abe935002d9d3af3a1f476bfdd73a6 -- references/
git diff --check
```

The temporary closure check is additional session evidence; the durable ledger/index diff retains the original fields and complete accounting for inspection. Final rechecks used installed SDK **10.0.402**. SDK-pin and quickstart edits appeared concurrently and are outside DW-608; they are preserved and excluded from its local commit.

Pinned commitlint **21.2.3**, matching `package-lock.json`, validates the exact local candidate with **zero problems/warnings** before use:

```bash
node node_modules/@commitlint/cli/cli.js --edit /tmp/dw-608/commit-message.txt --verbose > /tmp/dw-608/commitlint.log 2>&1
```
