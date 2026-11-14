# FrontComposer owner verification after Shell compatibility

This sequential phase preserves the parent-reported Tenants results and does not rerun their unchanged broad gates. It made no repository commits, staging, pointer updates, application launches, nested initialization, or publication. All owner builds below use Debug/source mode; archive/package qualification is separate and is not claimed.

## Final fixes and passing proof

- The pending-validation behavior tests found a real generated-form rendering failure: ConfigureAwait(false) resumed a valid asynchronous validation outside Blazor's dispatcher. ValidateAsync now uses ConfigureAwait(true), preserving the event's renderer context. Both generated snapshots reflect the final source. All six valid/invalid/canceled/disposed/thrown-cancellation/replaced-context scenarios pass within the final Shell selection.
- Seven individual owner test projects build with zero warnings/errors using `dotnet build <project> --configuration Debug -m:1 -p:UseHexalithProjectReferences=true -p:MinVerVersionOverride=1.0.0 -warnaserror`: SourceTools, Shell, Contracts, Contracts.UI, Testing, Mcp, and Shell.Tests.Bench. Exact commands and logs are in `frontcomposer-owner-build-results.json`.
- Final SourceTools.Tests was rebuilt after the ConfigureAwait(true) change. Its copied generator DLL has the same SHA-256 as the owning generator's freshly built Debug/netstandard2.0 DLL. Those hashes and the final emitter source hash are retained in `frontcomposer-owner-test-results.json`.
- Test-only .NET 11 compilation issues were repaired with JsonElement.Parse for six resource-lifetime cases, direct known-state notification in the fake auth provider, and 24 member-scoped BL0016 exceptions on controlled JS fakes or NSubstitute setup/verification. Their exceptions intentionally preserve observed failures and cancellations; they do not swallow exceptions or alter production policy. The active exception ledger's exact attribute census is 83 and the fail-closed policy control passes.
- The account observer's genuine shared fatal catch increases the taxonomy census from 79 to 80. The historical Story 11.22 assembly matrix remains net10.0, matching its untouched dated evidence; only live build/framework specimens stay on net11.0.
- Four active DocFX runtime assembly inputs now use net11.0; the SourceTools input remains netstandard2.0. Every configured DocFX input was checked against its owning project target (`docfx-framework-input-checks.json`). The active MCP benchmark friend-seam loader now resolves Debug/net11.0, and its source governance tests pass.
- The four requested touched Memories files were normalized to their declared CRLF without changing their semantic diffs. Root and both owning `git diff --check` commands pass, as does root story-gitlink validation.

The final passing owner selections contain **399 tests**:

| Selection | Passed |
| --- | ---: |
| CommandFormEmitter (including both snapshots) | 68 |
| Generated async-validation plus affected account/navigation/focus/storage/download/authorization/abandonment/lifecycle behavior | 297 |
| Current SDK/CI controls, analyzer policy/identifier seals, shared fatal taxonomy, and Shell boundary API | 9 |
| Contracts.UI source API/ownership/baseline controls | 3 |
| Testing source API and central-version boundary controls | 3 |
| MCP invocation and type-organization/friend-seam controls | 19 |

Exact filters/commands, report paths, counts, and SHA-256 values are in `frontcomposer-owner-test-results.json`. Raw reports are under `/tmp/tenants-net11-repair-ng54_u4z/`, not in the repository.

## Broad governance limitations retained without weakening gates

The initial broader Debug/source governance selection ran 113 tests: 107 passed and six failed. Two failures were factual expectations repaired and revalidated by the final focused control selection above. Four remaining checks have prerequisites independent of this migration:

1. `CiGovernanceTests.HexalithDependencyMode_DefaultsToProjectReferencesForDebugAndPackagesForRelease` reads `references/Hexalith.FrontComposer/references/Hexalith.Builds/Props/Directory.Packages.props`, which does not exist in this permitted root-only submodule layout. No nested submodule was initialized.
2. `CiGovernanceTests.ReleaseEvidenceScript_SealAndVerifyManifest_RoundTripsCleanly`, `ReleaseEvidenceScript_DetectsPostSealArtifactMutationFromRealFiles`, and `ReleaseEvidenceScript_DetectsReleaseDefinitionDriftFromRealFiles` fail in the unchanged synthetic `tests/ci-governance/stage_release_state.py` fixture. Reproduction shows `dependency_graph.GraphError: github.com/hexalith/hexalith.builds@<synthetic commit>: missing Props/Directory.Packages.props` because that fixture creates only a workflow in its synthetic Builds repository. The precise captured traceback is in `frontcomposer-release-fixture-blocker.log`.

The initial full selection is recorded in `frontcomposer-governance-tests.log` and `/tmp/tenants-net11-repair-ng54_u4z/frontcomposer-governance.xml`. It is not represented as a passing broad governance gate.

Existing package archive controls require Release/package producers (some launch their own Release builds; others pack with Release --no-build). At parent direction, this phase stayed Debug/source and did not consume a concurrent unowned Alignment suite's Release output as fresh proof. These archive/clean-package-consumer and API-compatibility lanes remain unqualified; the source boundary controls above do not replace them.

## Parent handoff

The final emitter's renderer-context fix changes generated source after the parent's previous Tenants proof. The parent should regenerate/rebuild and revalidate the affected Tenants output before final acceptance. No further source edits or owner build processes remain active from this phase.
