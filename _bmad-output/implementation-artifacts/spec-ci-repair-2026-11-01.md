---
status: in-review
date: '2026-11-01'
baseline_commit: 60cc86e2b80ab1dd912598a3753d2721c2520cf1
---

# Repair merged-main CI compatibility

## Intent

Repair demonstrated main failures while retaining the frozen bootstrap fixture,
authenticated bound-command checks, source-reference governance, and immutable
shared CI selection. Original HEAD is
`60cc86e2b80ab1dd912598a3753d2721c2520cf1`.

## Failures

- CI run `41249508661`: the frozen EventStore 3.128.4 topology build rejects the
  unguarded `ServiceDefaults.Routing` import with CS0234. Its Contracts lane has
  33 source-workflow trigger failures and two stale mutable-workflow assertions.
- Source-reference run `41249508174`: nullable catalog asynchronous disposal in
  AppHost `Program.cs` fails CA2007 at line 338.
- Story Guards run `41249508260`: the SDK qualification spec does not declare
  the already integrated `references/Hexalith.EventStore` pointer movement.

## File List

- `src/Hexalith.Tenants/Bootstrap/TenantBootstrapBoundCommand.cs`
- `src/Hexalith.Tenants.AppHost/Program.cs`
- `.github/workflows/ci.yml`
- `.github/workflows/source-reference.yml`
- `.github/workflows/story-guards.yml`
- `.github/workflows/commitlint.yml`
- `.github/workflows/codeql.yml`
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `_bmad-output/implementation-artifacts/spec-qualify-sdk-3-130-catalog-selection.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-ci-repair-2026-11-01.md`

## Changes

The existing catalog-capability symbol guards the bootstrap catalog import and
readiness wait, so the legacy fixture remains compilable and qualified catalog
SDKs retain the wait. Explicit configured catalog disposal follows application
disposal and precedes the existing Dapr cleanup. Main push triggers shed the
merged qualification branch, while the package-governance assertions now
require the existing immutable shared workflow commit. The earlier spec records
its integrated source pointer without changing any gitlink or baseline.

## Validation

- `python3 tests/scripts/test_bootstrap_compatibility.py`: 8 passed.
- `python3 tests/scripts/test_validate_story_gitlinks.py`: 77 passed.
- `python3 tests/scripts/test_validate_changed_story_gitlinks.py`: 101 passed.
- `python3 scripts/validate-story-gitlinks.py` for this record and the SDK
  qualification spec: both passed; the earlier move is declared and this repair
  introduces no pointer movement.
- `actionlint` on all five changed workflows: passed with no diagnostics.
- `git diff --check`: passed. `git diff --raw -- references/`: empty.
- Parent verification: the frozen EventStore 3.128.4 Release build and source
  IntegrationTests Debug build both passed with zero warnings and errors.
- First targeted Contracts run: 393 of 394 passed. The remaining failure in
  `Ci_workflow_enforces_build_test_coverage_and_artifact_governance` exposed a
  third stale assertion admitting only mutable shared-workflow references. The
  predicate now requires the exact approved `domain-ci.yml` path and immutable
  `1cc94bbb932be0d08a31b4e1393fcfc450a0b1c9` reference; approved action restrictions
  remain intact. The final focused rerun remains pending.

The parent session coordinates the final focused Contracts rerun to avoid
concurrent writes to shared project outputs. No .NET build or
test has been started by the repair implementation agent.

## Review

Independent review triaged one pre-existing verification gap as deferred:
`TenantBootstrapBoundCommandTests` does not install a catalog authority and
expected root, so wrong-root to matching-root readiness and cancellation during
waiting lack distinguishing tests. The owning deferred-work ledger records the
evidence and required controls under this repair's dated grouping. This finding
does not establish a production failure or change authenticated command policy.

The remaining review actions belong to parent-session verification and Git
ordering: rerun the corrected Contracts method and commit reviewed Tenants
content before recording its parent gitlink.

## Parent verification outcome

- Legacy package profile: `dotnet build src/Hexalith.Tenants/Hexalith.Tenants.csproj --configuration Release -warnaserror -m:1 -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithEventStoreVersion=3.128.4 -p:HexalithFrontComposerFromSource=false -p:HexalithMemoriesFromSource=false` passed, zero warnings/errors.
- Current source profile: `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -p:HexalithEventStoreVersion=3.130.1 -warnaserror -m:1 -nr:false` passed, zero warnings/errors, including AppHost compilation.
- Contracts compiled with the same current source/version properties, zero warnings/errors. Direct assembly classes `*SourceReferenceWorkflowGovernanceTests` and `*PackageGovernanceTests` executed 394 cases: 393 passed and exposed the third obsolete mutable-reference predicate. After correcting that predicate to the exact approved workflow path/SHA, the Contracts rebuild passed and `-method '*PackageGovernanceTests.Ci_workflow_enforces_build_test_coverage_and_artifact_governance'` passed (1/1, no skips).
- Independent edge-case review returned no findings. Both catalog-readiness coverage findings were identified as pre-existing and are recorded once in the owning deferred ledger; the existing readiness predicate is preserved.
- Parent validated the exact commit message with pinned commitlint before committing. Fresh hosted workflows will verify the committed source; their outcome is not inferred from local results.
