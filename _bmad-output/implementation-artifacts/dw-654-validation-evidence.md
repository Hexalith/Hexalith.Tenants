# DW-654 validation evidence — 2026-10-18

Entry revision: `ff7014107a8fe0c53315e43d8ccd6838f8a3d010`. Scope: refuse recovery publication when an existing package lacks its expected hash. Publication tools are intercepted locally; tests use real SHA256 over distinct local package contents. No remote publication or release edit occurs.

## Defect and repair

The historical DW-608 probe reproduces the original defect: `missing` exits **0**, reaching four intercepted package pushes, container login/publication and release creation. `present` exits **1** at the supplied-hash mismatch refusal. The runtime fix replaces only the empty expected-hash continuation with a package-specific refusal and exit 1, retaining the comparison and final source gate.

Nineteen new omission cases remove each existing package's hash record across package-only, container-only, release-only and mixed recovery. Twenty further cases cover empty, whitespace-only, wrong-ID-only, blank-record and missing-file evidence across the four multi-package inventories. Package bytes otherwise match the reviewed fixture, so the omission cases prove evidence is mandatory even without changed bytes. Reversed hash-record order and all seven matching inventory controls remain, including empty evidence with no existing packages.

Each refusal requires the exact pre-publication call sequence: only prior existing packages may be hashed, followed by refusal before the current package hash, final source/release reads or any publication. Empty lookups require the package-specific diagnostic; absent files retain the strict-shell nonzero failure and file diagnostic. Recovery-state filenames and contents must remain unchanged. Stable source responses ensure bypassed guards reach intercepted publication instead of failing for unrelated fixture reasons.

The initial test build failed with **three CA1062 errors** after the helper began dereferencing its response argument. Adding `ArgumentNullException.ThrowIfNull(response)` at the private helper boundary resolves them. The next Debug source build succeeds with **zero warnings/errors**.

Against the original publisher, focused execution has **115 total, 80 passed, 35 failed, zero skipped/errors**. All nineteen omitted-record cases and sixteen empty/unusable-file cases fail on actual call-sequence assertions; the four absent-file cases already fail safely. After the one-line runtime repair, focused execution passes **115/115**, and the maintained Contracts lane passes **479/479**, both with **zero failures/skips**. Formatting, shell syntax, whitespace, story-gitlink and entry-revision dependency checks pass.

```bash
mkdir -p /tmp/dw-654
bash _bmad-output/implementation-artifacts/dw-608-probe-missing-hash.sh missing > /tmp/dw-654/original-missing.log 2>&1
bash _bmad-output/implementation-artifacts/dw-608-probe-missing-hash.sh present > /tmp/dw-654/original-present.log 2>&1
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -m:1 -nr:false -p:UseHexalithProjectReferences=true > /tmp/dw-654/build-red-retry.log 2>&1
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests' -noColor -result-xml /tmp/dw-654/original-focused.xml > /tmp/dw-654/original-focused.log 2>&1
```

The commands above ran before the publisher repair. The commands below ran afterward; both probes now exit **1**, respectively reporting missing reviewed hash and differing reviewed content, before publication.

```bash
bash _bmad-output/implementation-artifacts/dw-608-probe-missing-hash.sh missing > /tmp/dw-654/fixed-missing.log 2>&1
bash _bmad-output/implementation-artifacts/dw-608-probe-missing-hash.sh present > /tmp/dw-654/fixed-present.log 2>&1
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests' -noColor -result-xml /tmp/dw-654/focused.xml > /tmp/dw-654/focused.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 479 --no-ansi --no-progress --results-directory /tmp/dw-654/contracts --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-654/contracts.log 2>&1
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs --verify-no-changes > /tmp/dw-654/format.log 2>&1
bash -n scripts/publish-partial-release.sh
git diff --check
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-654-missing-recovery-package-hashes.md > /tmp/dw-654/story-gitlinks.log 2>&1
git diff --exit-code ff7014107a8fe0c53315e43d8ccd6838f8a3d010 -- references/
```

## Pre-edit environment evidence

`aspire ps --format Json` returns no running AppHosts. Isolated startup, state/telemetry capture and exact-AppHost stop all exit **0**. The snapshot contains nine Running, thirteen Waiting and one Starting resources; security is still Starting. This establishes environment state, not healthy-topology or release qualification. No AppHost or dependency change is made.

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json > /tmp/dw-654/aspire-start.json 2>&1
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-654/aspire-describe.json 2>&1
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/dw-654/aspire-otel-logs.json 2>&1
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive > /tmp/dw-654/aspire-stop.log 2>&1
```

## Scope and limits

These checks qualify the actual publisher against local recovery evidence and intercepted tools. They do not qualify remote registry contents, package reproducibility, credentials, existing GitHub Release asset contents or release readiness. DW-620 and all unrelated release holds remain separate. The historical DW-608 probe and evidence remain unchanged.

## Independent review corrections and reproducible replay

Independent review found three acceptance improvements, now implemented. The no-existing-package success control also omits the hash file; an unconditional file-presence mutation proves this control prevents overbroad refusal. Wrong-ID and blank-record cases now retain every other valid record and corrupt the last existing package, requiring all preceding comparisons to succeed. A tracked replay rebuilds the current tests, obtains the original publisher directly from the full entry revision and runs isolated copies with fresh report paths. It validates exact test identities, failed-case sets, diagnostics, publication continuation, skips/errors and process exits, and records publisher, assembly and test-source SHA256 values. The prior summary is invalidated before work starts; build or report failures prevent successful replay evidence.

```bash
python3 _bmad-output/implementation-artifacts/dw-654-check-variants.py --output-directory /tmp/dw-654/variants --probe-truncated-inventory > /tmp/dw-654/variants.log 2>&1
```

| Isolated publisher | Passed | Failed | Skipped/errors | Process exit |
| --- | ---: | ---: | ---: | ---: |
| Current production | 66 | 0 | 0 | 0 |
| Entry-revision production | 31 | 35 | 0 | 1 |
| Unconditional hash-file requirement | 65 | 1 | 0 | 1 |

The replay uses new tests plus the earlier mismatch and matching controls. All 35 entry-revision failures demonstrate intercepted publication rather than unrelated fixture failures. Its explicit Debug source build has **zero warnings/errors**. It records `results.json` under the output directory with per-run logs/XML in a fresh `run-*` directory. The original publisher's SHA256 is `c35a23b2264d1ab98c7f60c6a22769ffc1e3d75616874e7c4b43c9cd5581f7d9`; current production is `1db1d26e5decf9f95474858933bd1ec206d6b768140bee6820e899da6e634f6b`. The command above reproduces the historical defect without changing the working publisher or test sources. The earlier original-path commands document initial execution chronology; rerunning those paths after the fix exercises current production.

Review also reproduced a separate pre-existing inventory defect: `while read` ignores an unterminated final package ID. The optional probe derives a local copy of the unchanged historical DW-608 probe and removes only the inventory's final newline. Current production exits **0**, reaching four intercepted package pushes, container login/publication and release creation with empty hash evidence. Normal preflight emits terminated lines. This malformed/truncated-state scenario is retained as **DW-655**; this change does not claim inventory completeness. The probe and publisher hashes plus exact intercepted calls are recorded in replay output.

Final focused and maintained-project checks use the corrected test source:

```bash
dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class 'Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests' -noColor -result-xml /tmp/dw-654/review-focused.xml > /tmp/dw-654/review-focused.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 480 --no-ansi --no-progress --results-directory /tmp/dw-654/contracts-review --report-xunit-trx --report-xunit-trx-filename contracts.trx > /tmp/dw-654/contracts-review.log 2>&1
dotnet format whitespace tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs --verify-no-changes > /tmp/dw-654/format-review.log 2>&1
python3 -m py_compile _bmad-output/implementation-artifacts/dw-654-check-variants.py
```

Final corrected focused execution passes **116/116**, and the maintained Contracts project passes **480/480**, both with **zero failures/skips**. Scoped formatting and Python compilation pass.

## Ledger preservation and commit validation

Only DW-654 closes. All **654 original IDs** and every unrelated original record body remain, plus DW-655: **655 total, 373 done / 282 open**, with **195 actionable** records and **254 maintenance rows** covering open IDs exactly once. Replaced DW-654 fields remain under `pre-dw-654-*`; its original finding is preserved. The exact closure comparison confirms the index replaces only DW-654's action row with DW-655, while archive, sprint tracking, historical DW-608 artifacts and all dependency pointers remain unchanged.

```bash
python3 /tmp/dw-654/validate-closure.py > /tmp/dw-654/closure.json
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-654-missing-recovery-package-hashes.md > /tmp/dw-654/story-gitlinks-final.log 2>&1
git diff --exit-code ff7014107a8fe0c53315e43d8ccd6838f8a3d010 -- references/
git diff --check
```

The temporary closure checker is session evidence; the durable ledger/index diff preserves the original fields and accounting for inspection. Pinned commitlint **21.2.3**, matching `package-lock.json`, validates the exact local candidate with **zero problems/warnings** before use:

```bash
node node_modules/@commitlint/cli/cli.js --edit /tmp/dw-654/commit-message.txt --verbose > /tmp/dw-654/commitlint.log 2>&1
```

Independent correction recheck reruns the tracked replay into `/tmp/dw-654/independent-recheck`, including the optional inventory probe. Its Debug source build has **zero warnings/errors**, current production passes **66/66**, the baseline has exactly **35 intended failures**, and the unconditional-file mutant has exactly **one intended failure**. It confirms the final **116/116** focused XML and **480/480** maintained Contracts TRX, all three acceptance corrections, ledger preservation and the separate DW-655 deferral, with no remaining issue in this change.

```bash
python3 _bmad-output/implementation-artifacts/dw-654-check-variants.py --output-directory /tmp/dw-654/independent-recheck --probe-truncated-inventory > /tmp/dw-654/independent-recheck.log 2>&1
```

Only DW-654 closes. No push or live publication is requested. Release remains not ready.
