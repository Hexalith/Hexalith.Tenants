# DW-344 / DW-381 / DW-393 / DW-394 / DW-395 validation

Date: 2026-10-21. Entry revision: `2cd3cc270e88d3cdbb47a1a938fd6a0951b6f0d4`.

The five records form one provenance verification batch. DW-393 remains an explicit duplicate of DW-381. No dependency versions, submodule pointers, product authorization policy or release settings change.

## Behavior and qualification

- DW-381/DW-393: the actual four-row `GlobalAdministratorsRealHandlerMetadataSurvivesRouterRestClientAndUiGateway` method was built with exactly its first InlineData row temporarily marked `Skip = "DW-381 forced-skip control"`. The existing four-test minimum exits 0 with three successes and one skip. The new `--fail-skips on` setting exits 2 with three successes and one failed skip. The original controller source was restored byte-for-byte; the four real cases pass with zero skips. The initial bare `--fail-skips` trial exited 5 with zero tests: this pinned xUnit MTP adapter requires the `on` value. The final workflow uses that executed setting.
- DW-394: the actual XML governance rule qualifies all three EventStore child-host build edges. It requires the Debug/source/Exists condition and unconditional false values for `ReferenceOutputAssembly`, `Private` and `IsAspireProjectResource`. The Aspire helper remains a compile reference. Independent controls reject missing, true, conditional and overriding metadata, lost/altered conditions and missing/duplicate edges.
- DW-395: YamlDotNet parses the actual job, ordered run steps and scalar commands. Expected command tokens bind configuration, source mode, no-build, method, four-case minimum and skip failure to their executable steps. The strict repository-specific command contract intentionally rejects alternate command vectors. Controls reject ineffective comments, moved flags, wrong configuration, conditional/ignored execution and malformed/duplicate YAML; equivalent scalar styles and explicit line continuations pass.
- DW-344: evaluated dependency-mode values are compiled into the AppHost assembly. The model test reads `builder.AppHostAssembly`, checks package mode and retains every original secret-store relationship assertion. It moves into the existing blocking AppHost.Tests lane, allowing independent package qualification. The passing package run records `UseNuGetDeps=true`, `UseHexalithProjectReferences=false`, `HexalithMemoriesFromSource=false`; the actual source-built AppHost records the reverse and fails the package assertion. No resource is started by these model tests.

## Commands

The source integration build and passing four-case execution use the workflow's exact arguments:

```bash
dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-incremental
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-build -p:UseHexalithProjectReferences=true --minimum-expected-tests 4 --filter-method Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.GlobalAdministratorsRealHandlerMetadataSurvivesRouterRestClientAndUiGateway --fail-skips on
```

For the forced-skip control, temporarily add the Skip property to only the first row immediately above that method, build with the same source arguments (the executed incremental probe additionally used `-p:BuildProjectReferences=false --no-restore` after a successful full source build), then compare the test command with and without `--fail-skips on`. Restore the original source before building the final graph. The raw archive retains both outcomes and the initial invalid bare-switch trial.

```bash
dotnet build tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithMemoriesFromSource=false -warnaserror -m:1 -nr:false --no-incremental
dotnet test tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj --configuration Debug --no-build -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithMemoriesFromSource=false --minimum-expected-tests 7 --fail-skips on --output Detailed --report-xunit-trx --report-xunit-trx-filename apphost-package.trx --results-directory /tmp/tenants-provenance-apphost-results
```

The final source-mode negative control builds AppHost.Tests with `--configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false`, then runs it with `--no-build`, the same source property, `--minimum-expected-tests 1 --filter-class Hexalith.Tenants.AppHost.Tests.MemoriesSecretStoreResourceGraphTests --fail-skips on`. It exits 2 at the package-mode assertion: one failed, zero succeeded/skipped. The archive retains the earlier equivalent IntegrationTests control and the final moved-class control separately.

```bash
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug --no-build -p:UseHexalithProjectReferences=true --minimum-expected-tests 1 --fail-skips on --report-xunit-trx --report-xunit-trx-filename contracts.trx --results-directory /tmp/tenants-provenance-contracts-results
```

Focused Contracts runs use the same no-build source arguments with `--filter-class '*SourceReferenceWorkflowGovernanceTests'` or `--filter-class '*PackageGovernanceTests'`, without TRX switches. The first build caught an unsupported string assertion overload; after correction the Debug build has zero warnings/errors. An attempted focus run against the stale assembly found zero matching cases and is not acceptance evidence.

## Broad blocker and limits

The attempted Debug package IntegrationTests build used `-p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithMemoriesFromSource=false -warnaserror -m:1 -nr:false --no-incremental`. It fails with CS0246 at `src/Hexalith.Tenants.Contracts/Queries/TenantReadAuthorityResult.cs:12`: the pinned EventStore package does not contain `NativeSourceProof`. That unchanged contract/package boundary is not repaired or accepted here. The focused AppHost package graph builds cleanly and establishes only the model qualification above; no full package integration or release readiness is inferred.

Aspire was started/restarted through its skill before edits and after AppHost metadata changed. The baseline reported all 23 resources Running and the security readiness check Healthy. This is resource-state inspection, not socket-level functional proof.

`actionlint .github/workflows/source-reference.yml` and `git diff --check` pass. `dotnet format whitespace Hexalith.Tenants.slnx --no-restore --include tests/Hexalith.Tenants.Contracts.Tests/SourceReferenceWorkflowGovernanceTests.cs tests/Hexalith.Tenants.AppHost.Tests/MemoriesSecretStoreResourceGraphTests.cs --verify-no-changes` exits 0 with a workspace-load warning; it reports no formatting violations.

## Ledger audit

Only the selected five record bodies change, preserving every original mutable field with the `pre-provenance-governance-` prefix and every original immutable line. Four index rows close because DW-381/DW-393 share one row. All 657 IDs remain: 450 done, 207 open across 189 rows; every open ID occurs exactly once. Archive, sprint tracking and submodule pointers remain unchanged.

Run `python3 _bmad-output/implementation-artifacts/dw-344-381-393-394-395-verify.py` to reproduce that audit against the exact entry revision and compare the saved JSON. Default replay does not overwrite evidence; `--write-result` explicitly regenerates it.

## Initial acceptance results (superseded below)

| Check | Succeeded | Failed / skipped | Exit |
| --- | ---: | --- | ---: |
| Workflow governance class, including mutation controls | 52 | 0 / 0 | 0 |
| Package governance class, including metadata controls | 234 | 0 / 0 | 0 |
| Full Contracts project (includes both focused classes) | 630 | 0 / 0 | 0 |
| Full package-mode AppHost project | 9 | 0 / 0 | 0 |
| Restored real-handler source provenance | 4 | 0 / 0 | 0 |
| Old four-case floor with one forced skip | 3 | 0 / 1 | 0 |
| Hardened command with the same forced skip | 3 | 1 / 0 | 2 (expected) |
| Final moved model test against source-built AppHost | 0 | 1 / 0 | 2 (expected) |

The full acceptance projects and real-handler cases total 643 successes; the focused counts are subsets and are not added again. The negative controls intentionally fail. The read-only ledger replay passes after normalizing terminal `status: done` records, including seven historical records whose disposition says `resolved`; their original fields remain unchanged.

The final source workflow was rebuilt after the model-test move: zero warnings/errors (49.41s), then 4/4 pass (1.688s). The restarted Aspire AppHost reports all 23 resources Running. The [run manifest](dw-344-381-393-394-395-run-manifest.json) records raw entry hashes and TRX counters; the [raw archive](dw-344-381-393-394-395-raw-logs.zip) preserves successful runs, deliberate failures, the initial build failure and the unchanged package blocker. Runtime dashboard credentials are excluded. The temporary forced-skip source has the same SHA-256 as the restored controller (`2a6edcdd7b6d7f7ac92821fcbcd38569a8c01d1d582fa27e2d899d2b47e0a821`).


## Reviewed acceptance

All ten Blind Hunter findings are patched and recorded individually in the spec. XML controls now reject authored Update/Remove operations, blank overriding metadata and a self-consistent wrong source root. YAML controls additionally require automated main push/PR triggers and reject job prerequisites that could suppress provenance. The declaration and command contracts remain deliberately specific to this repository, rather than general MSBuild or shell interpreters.

Actual AppHost metadata comparisons now follow MSBuild's case-insensitive Boolean semantics. The final package build and full/named proof runs use `-p:UseHexalithProjectReferences=FALSE -p:UseNuGetDeps=TRUE -p:HexalithMemoriesFromSource=FALSE`. The full test command raises `--minimum-expected-tests` from the historical seven to nine and writes `apphost-package-review.trx` under `/tmp/tenants-provenance-apphost-review-results`. A separate no-build command requires `--minimum-expected-tests 1 --filter-method Hexalith.Tenants.AppHost.Tests.MemoriesSecretStoreResourceGraphTests.PackageModeAppHostUsesTheConsumerOwnedMemoriesSecretStore --fail-skips on` with the same uppercase properties. Both pass. The source-mode negative control rebuilt the corrected test and still fails with the three source values; its exit 2 is expected.

The reviewed Contracts build fixed one newly exposed CA1062 warning by validating the mutation parameter. The final Debug build has zero warnings/errors. The full no-build command uses `--minimum-expected-tests 654`, writes `contracts-review.trx` under `/tmp/tenants-provenance-contracts-review-results`, and passes 654/654. That TRX contains all 68 YAML governance cases and all 242 package governance cases. The final separately captured real-handler invocation passes 4/4. Formatting verification exits 0 with the same workspace-load warning.

| Final check | Passed | Failed / skipped |
| --- | ---: | --- |
| Full Contracts | 654 | 0 / 0 |
| Full uppercase package AppHost | 9 | 0 / 0 |
| Required named Memories proof (subset of AppHost) | 1 | 0 / 0 |
| Real-handler source provenance | 4 | 0 / 0 |
| Source AppHost refusal control | 0 | 1 / 0 (expected) |

The final distinct acceptance total is 667; the named proof is not added again. The earlier 643 total remains historical. Existing broad package integration and release limits remain unchanged.

The final archive has 39 manifest entries. Every executed log has command/cwd/exit attribution. New reviewed runs capture invocation, tested-source hashes (raw and normalized LF), log hash, and test-binary hashes before/after at execution time. Earlier invocations are explicitly transcribed from tool history and retained as historical results. TRX entries name their producer. The archived execution-capture script documents this collection. The default verifier checks the archive/entry hashes, TRX counters, named proof, final invocation results, stable no-build binaries and current normalized source hashes, without changing saved evidence.


Pinned commit validation: `node node_modules/@commitlint/cli/cli.js --edit /tmp/tenants-provenance-commit.txt` exits 0 with no output for the exact full message `fix(ci): enforce source and package provenance checks` (one trailing newline; no body). Candidate file SHA-256: `5dc0a5741e879008cd41bf9b42b6afaa86fa70432378352f24776228c0dbea0e`. No commit-validation bypass is used.


Final independent recheck clears all ten findings with no remaining concrete finding or deferral. Direct probes accept the actual documents and reject every demonstrated bypass. The verifier and archived 654/9/1/4 acceptance plus expected source refusal all pass. The original five closures and every unrelated record remain preserved.
