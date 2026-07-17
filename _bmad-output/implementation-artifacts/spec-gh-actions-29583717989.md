---
title: 'Fix CI package boundary after EventStore upgrade'
type: 'bugfix'
created: '2026-07-17'
status: 'done'
baseline_commit: 'dd0dfab0a044b9d8ff274115371cf741c79ca444'
context:
  - _bmad-output/project-context.md
  - references/Hexalith.AI.Tools/hexalith-llm-instructions.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** GitHub Actions run `29583717989`, job `87895372865`, fails in the shared **Validate package consumer references** step. The Release build and all five package builds succeed, but `scripts/validate-nuget-packages.py` rejects `Microsoft.AspNetCore.DataProtection.Abstractions` as unexpected in `Hexalith.Tenants.Server`; the same stale boundary also affects `Hexalith.Tenants.Testing` and would fail next.

**Approach:** Align Tenants' exact package-dependency boundary and its mirrored synthetic test fixtures with the legitimate graph introduced when `Hexalith.EventStore` moved from `1.72.3` to `3.69.0`. Preserve strict allowlist validation and prove the full pack, metadata-validation, and isolated package-consumer sequence succeeds.

## Boundaries & Constraints

**Always:** Add `Microsoft.AspNetCore.DataProtection.Abstractions` only to the Server and Testing dependency sets where the generated nuspecs contain it. Keep the validator exact and keep CI/Release in NuGet package mode. Update the mirrored test fixture in lockstep. Preserve the committed `references/Hexalith.EventStore` gitlink at `dd3040c` and exclude it from this fix.

**Ask First:** Halt before changing production `.csproj` files, CI workflows, package versions, or anything inside `references/`. Halt if a fresh pack exposes any dependency difference beyond this one dependency in Server and Testing. Ask before committing, pushing, or merging.

**Never:** Do not downgrade or pin around EventStore `3.69.0`, switch CI to source `ProjectReference`s, weaken exact dependency-boundary enforcement, suppress the failure, or modify the EventStore submodule.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Server package | Release pack against EventStore `3.69.0` | Server boundary accepts DataProtection Abstractions and otherwise remains exact | Any other missing/unexpected dependency fails validation |
| Testing package | Validation continues past Server | Testing boundary also accepts the promoted dependency instead of becoming the next failure | Any other boundary drift fails validation |
| Unaffected packages | Contracts, Client, and Aspire nupkgs | Existing exact dependency sets remain unchanged and pass | Do not broaden their allowlists |

</frozen-after-approval>

## Code Map

- `scripts/validate-nuget-packages.py:22-122` -- canonical exact dependency-ID boundaries for the five published packages; Server and Testing are stale after the EventStore upgrade.
- `tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs:274-365` -- synthetic package dependencies that deliberately mirror the Python validator so metadata behavior is tested independently.
- `references/Hexalith.Builds/Props/Directory.Packages.props:7,170` -- read-only evidence: EventStore is now `3.69.0` and DataProtection Abstractions is centrally versioned at `10.0.10`.
- `/home/administrator/.nuget/packages/hexalith.eventstore.server/3.69.0/hexalith.eventstore.server.nuspec` -- read-only package evidence that the dependency is legitimate.

## Tasks & Acceptance

**Execution:**
- [x] `scripts/validate-nuget-packages.py` -- add `Microsoft.AspNetCore.DataProtection.Abstractions` to the Server and Testing expected dependency sets, preserving every other boundary entry.
- [x] `tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs` -- add the same dependency to the two mirrored fixture sets so synthetic valid packages match the production contract.
- [x] `scripts/pack-release-packages.py`, `scripts/validate-nuget-packages.py`, and `scripts/validate-consumer-package-references.py` -- run the complete Release pack → metadata validation → isolated consumer sequence and compare every generated package against its expected set.

**Acceptance Criteria:**
- Given all five packages are built in Release mode against current central versions, when `validate-nuget-packages.py` runs, then it validates all five with no missing or unexpected dependencies.
- Given the validated local packages, when `validate-consumer-package-references.py` runs, then its package-only consumers restore, build, and test without project-reference leakage.
- Given the mirrored synthetic fixtures, when `Hexalith.Tenants.Contracts.Tests` runs, then all package-validator and governance tests pass.
- Given the committed EventStore gitlink at `dd3040c`, when the fix is complete, then its checked-out commit and clean working-tree state are unchanged.

## Spec Change Log

## Design Notes

The shared workflow label is broader than the actual failure. Its shell runs pack, metadata validation, then consumer validation under `set -e`; metadata validation exits first, so the consumer harness never starts. This repair updates the intended package contract rather than bypassing the guard. Local reproduction confirms Contracts, Client, and Aspire have zero boundary differences, while Server and Testing each have exactly the same new dependency.

## Verification

**Commands:**
- `dotnet build Hexalith.Tenants.slnx --configuration Release` -- expected: zero warnings and errors.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-build` -- expected: all contract and CI-governance tests pass.
- `ci_fix_dir="$(mktemp -d)" && python3 scripts/pack-release-packages.py "$ci_fix_dir" 0.0.0-ci-test && python3 scripts/validate-nuget-packages.py "$ci_fix_dir" && python3 scripts/validate-consumer-package-references.py "$ci_fix_dir"` -- expected: five packages validate and both isolated consumer projects succeed.
- `git submodule status -- references/Hexalith.EventStore` before and after -- expected: unchanged clean gitlink at `dd3040c`.

## Suggested Review Order

**Package boundary correction**

- Server accepts the newly legitimate dependency while every other identifier remains exact.
  [`validate-nuget-packages.py:49`](../../scripts/validate-nuget-packages.py#L49)

- Testing mirrors Server's promoted dependency so validation continues past the first package.
  [`validate-nuget-packages.py:78`](../../scripts/validate-nuget-packages.py#L78)

**Fixture parity**

- Server's synthetic package fixture tracks the production boundary contract exactly.
  [`CiQualityGateScriptTests.cs:301`](../../tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs#L301)

- Testing's fixture prevents the second package from hiding behind Server's earlier failure.
  [`CiQualityGateScriptTests.cs:331`](../../tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs#L331)

**Concurrent dependency baseline**

- The user-bundled Builds update advances EventStore independently of the CI boundary fix.
  [`Directory.Packages.props:7`](../../references/Hexalith.Builds/Props/Directory.Packages.props#L7)

- The matching EventStore release record explains the concurrent `3.70.0` submodule advance.
  [`CHANGELOG.md:1`](../../references/Hexalith.EventStore/CHANGELOG.md#L1)
