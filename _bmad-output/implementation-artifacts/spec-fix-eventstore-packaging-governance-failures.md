---
title: 'Repair EventStore packaging and governance test failures'
type: 'bugfix'
created: '2026-10-04'
status: 'ready-for-dev'
route: 'dispatch'
baseline_commit: '3709171944ae0226c7c9b1e6c6401125d94b308a'
eventstore_baseline_commit: '9b525ba8f0adf30a466f8727f1b37564181798eb'
review_loop_iteration: 0
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** EventStore Contracts reported 54 failures: nested-checkout assumptions, missing evidence exemptions, and historical fixtures mixed with current bytes.

**Approach:** Repair these tests in a separate EventStore maintenance task using permitted workspace dependencies and recorded historical inputs.

## Boundaries & Constraints

**Always:** Preserve package/source boundaries, central version authority, immutable evidence and fail-closed validators. Support standalone and umbrella layouts. Verify repository ownership before Git-object reads. Exempt only named standalone evidence probes.

**Never:** Initialize nested submodules; move dependency pins; rewrite historical evidence or approvals; skip failures or weaken assertions; change tenant behavior; stage, commit, push, publish or deploy.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected behavior | Failure handling |
| --- | --- | --- | --- |
| Checkout layout | Standalone dependencies, declared umbrella dependencies, or explicit source override | Read the intended dependency repository and exact pinned objects | Reject absent, invalid or mismatched repositories |
| Historical fixture | Recorded v4 source and checkout line endings | Reproduce sealed bytes in temporary test directories | Reject unreproducible identity; preserve original evidence |
| Version authority | Named standalone evidence probes versus executable projects | Exempt only the named probes; enforce central authority elsewhere | Reject newly introduced executable-project version overrides |

</frozen-after-approval>

## Code Map

Paths are relative to `references/Hexalith.EventStore/`; `{packaging}` means `tests/Hexalith.EventStore.Contracts.Tests/Packaging`.

- `Directory.Build.props` and `Directory.Packages.props`: supported dependency layouts; reuse conventions without changing mode.
- `{packaging}/`: eight failing classes, detailed in tasks. Counts: 30 layout failures, 23 historical-fixture failures, one exemption failure.
- `tools/validate-oq8-platform-evidence.py`: preserve current-source drift rejection.
- Workspace Builds already contains both required commits and matching tool bytes.

## Tasks & Acceptance

**Execution:**

- [ ] `{packaging}/PackagingRepositoryPaths.cs` and `{packaging}/PackagingRepositoryPathsTests.cs` — implement declaration-aware lookup; verify valid layouts, overrides, missing source and accidental parent-Git discovery.
- [ ] `{packaging}/{PackageOwnershipGovernanceTests,CommitMessagePolicyTests,ReleasePackageManifestTests}.cs` — resolve nested Tenants, AI.Tools and Builds reads through the helper.
- [ ] `{packaging}/{ContainerPublishingGovernanceTests,CorrectiveOciProvenanceReleaseTests,DeployedRuntimeParityClosureTests}.cs` — resolve Builds consistently; preserve commit, ancestry and tool-hash checks and `HEXALITH_BUILDS_SOURCE` support.
- [ ] `{packaging}/ContractsPackageDependencyTests.cs` — add exactly five verified exemptions beneath `_bmad-output/implementation-artifacts/evidence/`: `6-1-p1r-3109/consumer/Consumer.csproj`, `6-1-p1r-3109/rollback-probe/v3109/Probe.csproj`, `6-1-p1r-3109/rollback-probe/v370/Probe.csproj`, `6-1-p1r-3110/consumer/Consumer.csproj`, `6-1-p1r-3110/verification/Directory.Packages.props`. Test rejection of a non-exempt executable-project override.
- [ ] `{packaging}/Oq8PlatformClosureTests.cs` — materialize all historical v4 bound inputs, including `.gitattributes`, `Oq8PostgresqlFixture.cs` and `global.json`; reproduce recorded CRLF JSON bytes and sealed hashes before mutation tests.
- [ ] `docs/ci.md` — document the supported local governance invocation and required declared workspace sources.
- [ ] This spec — record verification and independent review separately from Story 5.6.

**Acceptance Criteria:**

- Given the original cases, when Contracts runs, then all 54 pass without added skips.
- Given invalid repository identity, pinned objects, tool bytes, version metadata or evidence, when governance executes, then its guard rejects the input.
- Given completion, when the diff is inspected, then historical evidence, dependency pins and tenant behavior remain unchanged.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

Run from EventStore; restore before building.

- `dotnet restore tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj -p:UseNuGetDeps=false -m:1 -v:q`
- `dotnet build tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore -v:q` — zero errors and warnings.
- `dotnet test --project tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — focused packaging classes first, then the full suite; preserve the existing two skips.
- Baselines: `/tmp/tenants-56-current-es-contracts-tests.log` and `/tmp/eventstore-packaging-maintenance-baseline.log`. Isolate the additional process-harness timeout before changing its scope.
- `git diff --check` in both repositories; inspect dependency-pointer and immutable-evidence diffs.

**Investigation evidence:** The full command above using existing build output exited 2: 2,178 total, 2,121 passed, 55 failed, two skipped. All original 54 reproduced. The extra five-second process timeout passed alone with `--filter-class '*ProofPacketDaprConflictProcessContractTests'` (exit 0, one passed; `/tmp/eventstore-packaging-maintenance-process-rerun.log`). Both repositories pass `git diff --check`; only this draft spec changed.
