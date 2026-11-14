# .NET 11 source graph implementation: first phase

All commands ran locally; no application topology was started, no source pointers moved, and no package publication, staging, or commits were performed.

## Passing checks

- From `references/Hexalith.FrontComposer`: `dotnet build src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true -p:TreatWarningsAsErrors=true` — zero warnings/errors; preserves netstandard2.0 generator and Contracts face (`frontcomposer-generator-source-build.log`).
- From `references/Hexalith.Memories`: `dotnet build tests/Hexalith.Memories.Cli.Tests/Hexalith.Memories.Cli.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true -p:TreatWarningsAsErrors=true` — zero warnings/errors (`memories-cli-source-build-2.log`).
- From Memories: `dotnet tests/Hexalith.Memories.Cli.Tests/bin/Debug/net11.0/Hexalith.Memories.Cli.Tests.dll -class Hexalith.Memories.Cli.Tests.Cli.QuickstartPrerequisiteTests` — 19 passed (`memories-prerequisite-tests.log`).
- From FrontComposer: `python3 -m unittest discover -s tests/eng -p test_adopter_proof.py` — 25 passed (`frontcomposer-adopter-tests.log`).
- From FrontComposer: `python3 tests/e2e/scripts/verify-isolated-readiness.test.py ReadinessSafetyTests.test_native_channel_uses_owned_processes_and_rejects_missing_mismatched_or_ambiguous_secrets` — 1 passed (`frontcomposer-readiness-focused-test.log`).
- From Memories: `python3 -m unittest discover -s tests/tooling/ci_alignment -p '*_test.py'` — 15 passed (`memories-ci-alignment-tests.log`).
- From Memories: `python3 -m unittest discover -s tests/tooling/coverage_gate -p '*_test.py'` — 40 passed (`memories-coverage-tool-tests.log`).
- Seven root `node --check` calls and nine changed Python AST checks passed (`syntax-checks.json`).
- `git diff --check` passed in all three owning repositories.
- Root `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-complete-dotnet-11-source-graph.md` passed (`story-gitlink-validation.log`).

## Pending checks and exact blockers

- Root `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true -p:TreatWarningsAsErrors=true` now restores the .NET 11 FrontComposer/Memories source graph. Initial NU1510 errors were repaired by removing runtime-provided package references from Memories Client.Rest/Cli; initial CA2263 was repaired with the generic JsonSerializerOptions.GetTypeInfo API. Latest build stops at 17 distinct .NET 11 Blazor analyzer diagnostics in FrontComposer Shell (reported twice under graph identities; 34 total). See `tenants-ui-source-build-3.log` and `remaining-shell-analyzer-errors.txt`. No warning gate was weakened.
- FrontComposer full `python3 tests/e2e/scripts/verify-isolated-readiness.test.py` ran 78 tests: 77 passed, one errors because the intentionally uninitialized nested `references/Hexalith.AI.Tools/hexalith-git-instructions.md` does not exist. No nested checkout was initialized. The directly changed executable-path fixture passes separately.
- Generated-form behavior and snapshot tests are written but await the Shell compatibility phase before execution. Their six scenarios cover pending valid/invalid validation, caller cancellation, disposal, a validator-thrown OperationCanceledException, and a replaced EditContext. Existing admission and authorization ordering assertions remain.
- Tenants integration source build, FrontComposer owner/package-boundary test execution, and rendered status-label browser control remain for the next sequential phase after the Shell build succeeds.
- No package-mode validation was run in this phase; source-mode evidence must not be represented as package qualification.

## Deliberate compatibility retention

Contracts/Schema retain netstandard2.0, SourceTools retains netstandard2.0, and useful NET10_0_OR_GREATER branches remain. Synthetic archive/package compatibility tests and the frozen functional-retention OCI image/digest are retained. Explicit external source-resource SDK 10.0.302 and 10.0.404 installation cases remain unchanged. No root SDK/catalog selection changed.
