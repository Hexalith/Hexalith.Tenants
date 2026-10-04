---
title: 'Repair EventStore packaging and governance test failures'
type: 'bugfix'
created: '2026-10-04'
status: 'done'
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

- [x] `{packaging}/PackagingRepositoryPaths.cs` and `{packaging}/PackagingRepositoryPathsTests.cs` — implement declaration-aware lookup; verify valid layouts, overrides, missing source and accidental parent-Git discovery.
- [x] `{packaging}/{PackageOwnershipGovernanceTests,CommitMessagePolicyTests,ReleasePackageManifestTests}.cs` — resolve nested Tenants, AI.Tools and Builds reads through the helper.
- [x] `{packaging}/{ContainerPublishingGovernanceTests,CorrectiveOciProvenanceReleaseTests,DeployedRuntimeParityClosureTests}.cs` — resolve Builds consistently; preserve commit, ancestry and tool-hash checks and `HEXALITH_BUILDS_SOURCE` support.
- [x] `{packaging}/ContractsPackageDependencyTests.cs` — retain exactly ten verified additions to the original three exemptions beneath `_bmad-output/implementation-artifacts/evidence/`: `6-1-p1r-3109/consumer/Consumer.csproj`, `6-1-p1r-3109/rollback-probe/v3109/Probe.csproj`, `6-1-p1r-3109/rollback-probe/v370/Probe.csproj`, `6-1-p1r-3110/consumer/Consumer.csproj`, and `6-1-p1r-3110/verification/{Directory.Packages.props,Directory.Build.props,Directory.Build.targets,domain/Domain.csproj,host/Host.csproj,probe/Probe.csproj}` (six exact files, not a glob). Test rejection of a non-exempt executable-project override. The five additional verification-harness exclusions were approved by the user on 2026-10-04.
- [x] `{packaging}/Oq8PlatformClosureTests.cs` — materialize all historical v4 bound inputs, including `.gitattributes`, `Oq8PostgresqlFixture.cs` and `global.json`; reproduce recorded CRLF JSON bytes and sealed hashes before mutation tests.
- [x] `docs/ci.md` — document the supported local governance invocation and required declared workspace sources.
- [x] This spec — record verification and independent review separately from Story 5.6.

**Acceptance Criteria:**

- Given the original cases, when Contracts runs, then all 54 pass without added skips.
- Given invalid repository identity, pinned objects, tool bytes, version metadata or evidence, when governance executes, then its guard rejects the input.
- Given completion, when the diff is inspected, then historical evidence, dependency pins and tenant behavior remain unchanged.

## Implementation Notes

- Added declaration-aware standalone/umbrella dependency resolution. Selected worktrees and origin identities are checked before historical reads; full pinned commits and existing ancestry/tool-byte assertions remain enforced. No nested submodule was initialized.
- Historical v4 fixture sources are derived from recorded `gateInputs` and read from the recorded v4 commit. Sealed hashes must match Git bytes or their recorded CRLF checkout form before mutation tests. Evidence and validators are unchanged.
- Before the scope amendment, the strict five-path exemption list was implemented, with new `Version` and `VersionOverride` rejection cases for a non-exempt executable project. The shared validator rejected three frozen verification projects that inherit the historical catalog; this was the pre-amendment acceptance conflict, not a skipped failure. The approved exact exclusions resolve that conflict, and ordinary/evidence-sibling executable overrides remain rejected.
- Independent review hardened source selection before catalog parsing or validator execution, cleared inherited Git repository selectors, disabled replacement objects for pinned reads, and enforced commit/blob object types. Regression coverage includes SSH origins, originless declaring workspaces, mismatched owner declarations, tracked discovery, and unsafe manifest paths. Lifecycle fixture updates now write explicit LF bytes.

- Execution began on Tenants `04e655cf070b17eced9daefb9eaa87a09ec60e81` and EventStore `b7355178786cea3daac9bc06114c67f755a718f5`, both clean on `main`. Preserve the recorded planning baselines above; inspect the intervening CI-input repair before adding changes.

## Spec Change Log

- 2026-10-04: The intervening EventStore CI repair had added ten evidence exclusions. This implementation retained only the five specified additions and exposed a contradiction between that exact scope and the all-54-pass criterion. Requested the owner's decision whether to retain the five additional fixed historical verification-harness exclusions or preserve the strict list and record the blocked criterion; no scope expansion was authorized at that checkpoint.

### Approved scope amendment (2026-10-04)

The user answered "yes" to retaining the five additional named historical-harness exemptions and finishing verification and independent review. To satisfy the shared catalog validator without modifying historical evidence, retain these five additional exact paths beneath `_bmad-output/implementation-artifacts/evidence/6-1-p1r-3110/verification/`:

- `Directory.Build.props`
- `Directory.Build.targets`
- `domain/Domain.csproj`
- `host/Host.csproj`
- `probe/Probe.csproj`

These are the standalone historical verification harness already exempted by the intervening EventStore CI repair. The approved change retains ten additions to the original three-path list, with no directory/glob exemption; ordinary executable projects remain subject to central-authority validation and the new negative tests. Apply only these five additional exact exclusions, then rebuild, verify the focused dependency-authority class and full Contracts suite, and complete independent review. Earlier failed runs below remain recorded as pre-amendment evidence.

## Review Triage Log

### Independent review triage (2026-10-04)

All three layers reported before triage. The EventStore task diff was reviewed from clean execution revision `b7355178786cea3daac9bc06114c67f755a718f5` (pre-patch snapshot preserved at `/tmp/eventstore-packaging-independent-zth3sefx.pre-patch.diff`); the wider recorded-baseline snapshot is retained separately because it includes intervening owner commits. Each finding is classified before grouping. All findings below have a small test-only correction, add no runtime public surface, and route to `patch`.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| BH-1: imported catalog/validator source bypasses repository verification | medium | `ResolveSharedPackageVersionsPath` returns the first existing import, and the extracted validator runner derives and executes its Builds script without verifying that worktree or declaration. This bypasses the source-selection checks used by the other governance callers. Patch: verify the effective imported catalog's owning declared Builds source while preserving the actual MSBuild import order. |
| BH-2: inherited Git repository selectors spoof ownership | medium | The new helper's child process inherits `GIT_DIR`, `GIT_WORK_TREE`, and `GIT_COMMON_DIR`; setting the first two can make an unowned path pass `--show-toplevel` and read another repository's origin/objects. Patch: remove these selectors for verified Git operations and their affected callers. Group with EC-2. |
| BH-3: remaining pinned archive/ancestry reads honor replacement refs | medium | The corrected Builds lookup makes `CorrectiveOciProvenanceReleaseTests`' archive and `ContainerPublishingGovernanceTests`' ancestry readers reachable, but their existing process helpers omit `--no-replace-objects`; a replacement ref changes pinned content/history. Patch: consistently disable replacement objects for these reads. |
| BH-4: declaring umbrella without origin is rejected | medium | The host worktree is already verified, but `HasRepositoryIdentity` throws when its optional origin is absent before inspecting its correctly declared dependency. Patch: a missing host origin is a nonmatch; selected dependencies still require their intended origin identity. Group with EC-1. |
| BH-5: pinned reader accepts tag/tree object types | medium | `cat-file -e SHA^{commit}` accepts annotated tag objects, and `git show COMMIT:tests` returns a tree listing successfully (confirmed against the recorded v4 commit). This violates the new reader's exact-commit/blob contract. Patch: reject non-commit revisions and non-blob paths before reading bytes. |
| BH-6: negative control cannot detect a broad evidence-directory exemption | medium | The only newly introduced negative project is under `src`; a regression excluding the whole evidence directory would leave it detected. Patch: also reject a non-exempt sibling beside the exempt historical files for both metadata forms. |
| BH-7: new negative fixture does not exercise tracked discovery | medium | The temporary negative fixture has no Git worktree; the shared validator explicitly falls back to filesystem enumeration when `git ls-files` fails. The live positive lane uses tracked discovery. Patch: add a tracked temporary-fixture case without staging or changing either owning repository's index or history. |
| VG-1: wrong EventStore ancestor declaration lacks a regression check | medium | Pre-verified coverage gap: removing the exact-owner comparison leaves current layout tests passing but permits another EventStore clone's ancestor to supply Builds. Patch: add a negative fixture with a mismatched declared EventStore path. |
| VG-2: accepted SSH remote forms lack regression coverage | medium | Pre-verified coverage gap: all synthetic remotes are HTTPS although the helper explicitly supports SCP-style and SSH URI remotes. Removing either normalization is undetected. Patch: exercise both accepted SSH forms for owner and dependency remotes. |
| EC-1: no-origin umbrella | medium | Same verified missing optional host-origin defect as BH-4; retain this separate verdict and group its patch with BH-4. |
| EC-2: inherited repository selectors | medium | Same verified unowned-worktree acceptance defect as BH-2; retain this separate verdict and group its patch with BH-2. |
| EC-3: manifest path may escape temporary evidence | medium | `MaterializeHistoricalV4Inputs` combines and writes the manifest path before any containment check. An absolute or parent-traversing entry can reach an existing file outside the evidence directory; CRLF reproduction can change its bytes before the validator runs. Patch: validate repository-relative contained file paths before reading/writing and cover rejection. |
| EC-4: lifecycle setup rewrites sealed LF spec on Windows | medium | After restoring every sealed gate input, `SetFinalLifecycle` calls `SetStory415Lifecycle`, which uses platform-dependent `File.WriteAllLines` on the hash-bound spec. Windows changes LF to CRLF even though the recorded spec hash binds LF. Patch: preserve canonical LF when updating lifecycle text, keeping the existing all-bound-input hash assertion. |

All 13 findings were patched in 11 root-cause groups. The targeted rerun passed 1,151 cases with zero failures/skips: 932 cases in the six edited classes and 219 in `CorrectedDeployedRuntimeParityClosureTests`, inadvertently included by the suffix glob for `DeployedRuntimeParityClosureTests`. Parent XML audit confirmed every new regression group, including the inherited-selector child witness and all six unsafe manifest paths. The prescribed post-review full-suite verification follows below.

- Pre-amendment acceptance audit: no layout or historical-fixture failures remained. The shared catalog validator was the sole outstanding original failure; review awaited the exemption-scope decision.
- Approved-scope acceptance audit before review patches: the prescribed full Contracts rerun passed (2,187 passed, zero failed, two existing skips), including all 54 originally failing cases. Independent review subsequently produced the patches recorded above.

## Verification

Run from EventStore; restore before building.

- `dotnet restore tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj -p:UseNuGetDeps=false -m:1 -v:q`
- `dotnet build tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 --no-restore -v:q` — zero errors and warnings.
- `dotnet test --project tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj -c Debug -p:UseNuGetDeps=false --no-build --no-restore` — focused packaging classes first, then the full suite; preserve the existing two skips.
- Baselines: `/tmp/tenants-56-current-es-contracts-tests.log` and `/tmp/eventstore-packaging-maintenance-baseline.log`. Isolate the additional process-harness timeout before changing its scope.
- `git diff --check` in both repositories; inspect dependency-pointer and immutable-evidence diffs.

**Investigation evidence:** The full command above using existing build output exited 2: 2,178 total, 2,121 passed, 55 failed, two skipped. All original 54 reproduced. The extra five-second process timeout passed alone with `--filter-class '*ProofPacketDaprConflictProcessContractTests'` (exit 0, one passed; `/tmp/eventstore-packaging-maintenance-process-rerun.log`). Both repositories pass `git diff --check`; only this draft spec changed.

### Execution evidence (2026-10-04)

- Prescribed restore: exit 0; `/tmp/eventstore-packaging-maintenance-restore.log`.
- Prescribed Debug source-mode build: exit 0, zero warnings/errors; `/tmp/eventstore-packaging-maintenance-build.log`.
- Packaging run with `--filter-class '*Packaging.*'`: exit 2, 1,391 total, 1,387 passed, two failures, two original skips; `/tmp/eventstore-packaging-maintenance-focused.log`. The catalog conflict and the known five-second process-harness timeout were the only failures.
- New checkout matrix tests: eight passed with XML evidence at `/tmp/eventstore-packaging-paths.xml`. All 11 added cases passed in the packaging run.
- Prescribed full Contracts command: exit 2, 2,189 total, 2,186 passed, one failed, two original skips; `/tmp/eventstore-packaging-maintenance-full.log`. The process-harness timeout did not reproduce. The sole failure is `SharedConsumerAuthorityValidatorPassesForEveryTrackedMsBuildSurfaceAsync`; the frozen `6-1-p1r-3110/verification/{domain,host,probe}` projects inherit a historical two-package catalog and fail current central-catalog identity and package completeness checks.
- The first full run preceded the final EventStore-root verification hardening. Final source-mode restore and rebuild passed with zero warnings/errors (`/tmp/eventstore-packaging-maintenance-final-restore.log` and `/tmp/eventstore-packaging-maintenance-final-rebuild.log`); the final affected-class rerun completed as recorded below. An intervening rebuild had failed with `CS1704`/`MSB3243` after package-mode test restores changed shared outputs; refreshing the prescribed source-mode restore resolved it without dependency or configuration changes.
- Unified diffs were captured without staging: recorded-baseline artifact `/tmp/eventstore-packaging-review-yvki4zji.diff` and execution-only artifact `/tmp/eventstore-packaging-execution-ry6n4e0d.diff`. The recorded-baseline artifact includes intervening owner commits; task changes are audited against the clean execution revisions recorded above.

- Final-code execution diff `/tmp/eventstore-packaging-final-hcmpzabr.diff` was read in full. Both repository `git diff --check` commands passed. No index changes, dependency configuration or pointers, immutable evidence, or runtime-source changes were introduced by this task.

### Source verification before the scope amendment

The final serialized command ran from `references/Hexalith.EventStore/`:

```bash
dotnet tests/Hexalith.EventStore.Contracts.Tests/bin/Debug/net10.0/Hexalith.EventStore.Contracts.Tests.dll -class '*PackageOwnershipGovernanceTests' -class '*CommitMessagePolicyTests' -class '*ReleasePackageManifestTests' -class '*ContainerPublishingGovernanceTests' -class '*CorrectiveOciProvenanceReleaseTests' -class '*DeployedRuntimeParityClosureTests' -class '*ContractsPackageDependencyTests' -class '*Oq8PlatformClosureTests' -class '*PackagingRepositoryPathsTests' -parallelMode none -result-xml /tmp/eventstore-packaging-maintenance-final-focused.xml > /tmp/eventstore-packaging-maintenance-final-focused.log 2>&1
```

- Exit 1: 1,272 total, 1,271 passed, one failed, zero skips. The sole failure is the shared central-authority validator's historical-catalog conflict (906 diagnostics for the three frozen verification executables).
- Parent audit of the XML confirmed passing coverage for every matrix row: all eight checkout/layout and invalid-repository cases, historical sealed-byte reproduction with corrupted-byte rejection, and both non-exempt executable `Version`/`VersionOverride` rejections. The intended positive exemption behavior remains blocked by the unresolved scope conflict.
- Comparing every original failing method against all of its final XML cases confirms 53 original failure rows now pass, one still fails, and none are missing or skipped; `/tmp/eventstore-packaging-original-failure-audit.txt`.
- Both repositories pass `git diff --check`; no staged files, history mutations, submodule initialization, changed dependency pins, changed runtime source, or immutable-evidence edits. The final EventStore HEAD remains `b7355178786cea3daac9bc06114c67f755a718f5`.
- Acceptance and independent review remain incomplete pending the owner's exemption-scope decision. The spec stays `in-progress`; Story 5.6 tracking was not modified.

### Resumed execution after scope approval

- The user approved the five additional named historical verification-harness exemptions with "yes". Original baselines and frozen boundaries are preserved. The earlier failing results are retained; resumed verification and review will be recorded separately.

- Approved-scope source-mode restore and build: exit 0, zero warnings/errors; `/tmp/eventstore-packaging-maintenance-approved-restore.log` and `/tmp/eventstore-packaging-maintenance-approved-build.log`.
- Approved-scope focused command: `dotnet tests/Hexalith.EventStore.Contracts.Tests/bin/Debug/net10.0/Hexalith.EventStore.Contracts.Tests.dll -class '*ContractsPackageDependencyTests' -parallelMode none -result-xml /tmp/eventstore-packaging-maintenance-approved-dependency.xml` (run from EventStore; stdout/stderr `/tmp/eventstore-packaging-maintenance-approved-dependency.log`): exit 0, 19 passed, zero skips. Parent XML audit confirmed the shared authority validator and both non-exempt executable-version rejection cases pass. Full Contracts rerun is in progress.

- Approved-scope prescribed full Contracts command: exit 0, 2,189 total, 2,187 passed, zero failed, two original package-inventory skips; `/tmp/eventstore-packaging-maintenance-approved-full.log`. No new skips. All 54 originally failing cases are now covered by passing runs.
- Matrix audit after approval: the earlier final-source XML covers all eight repository/layout cases and historical-byte reproduction/corruption; the approved dependency XML additionally confirms positive shared-authority validation and both executable-version negative controls pass. The only code changed since the earlier final-source matrix run was the exact approved evidence-exclusion list.

### Verification of independent-review patches

- Review-patch source-mode restore/build: exit 0, zero warnings/errors; `/tmp/eventstore-packaging-review-restore.log` and `/tmp/eventstore-packaging-review-build.log`.
- Targeted serialized run: exit 0, 1,151 passed, zero failed/skipped; `/tmp/eventstore-packaging-review-targeted.log` and `/tmp/eventstore-packaging-review-targeted.xml`. The six edited classes account for 932 cases: `PackagingRepositoryPathsTests`, `ContractsPackageDependencyTests`, `ContainerPublishingGovernanceTests`, `CorrectiveOciProvenanceReleaseTests`, `DeployedRuntimeParityClosureTests`, and `Oq8PlatformClosureTests`. The suffix glob also selected 219 cases in `CorrectedDeployedRuntimeParityClosureTests`; parent XML audit confirmed this count. No additional targeted run was performed.
- Parent XML audit confirmed all new groups pass: two SSH forms, mismatched ancestor declaration, no-origin host, selected-import precedence, tag/tree rejection, inherited Git-selector isolation, three catalog-ownership failures, eight version-metadata controls across ordinary/evidence-sibling and filesystem/tracked discovery, six unsafe manifest paths, and every sealed historical source hash with corrupted-byte rejection.
- Refreshed EventStore diff was read in full at `/tmp/eventstore-packaging-independent-zth3sefx.diff` (70,873 bytes; SHA-256 `28f7040c54eab17364c558432352f4817834d95ff06fbc75f1b3acfc4cfccff9`). No review layers were skipped; all 13 findings routed to patches, with zero deferrals or loopbacks.
- Verification ran on Linux. The LF/CRLF regression asserts exact sealed hashes and explicit LF lifecycle bytes; native Windows execution was not performed.

### Final acceptance (2026-10-04)

- Parent reran the prescribed source-mode restore and Debug build after review patches: both exited 0; build reported zero warnings/errors. Logs: `/tmp/eventstore-packaging-maintenance-post-review-restore.log` and `/tmp/eventstore-packaging-maintenance-post-review-build.log`.
- Parent reran the exact prescribed full Contracts command: exit 0, 2,211 total, 2,209 passed, zero failed, two existing skips, duration 3m 09s. Log: `/tmp/eventstore-packaging-maintenance-post-review-full.log`. All 54 originally failing cases pass; the 22 additional review regression cases also pass.
- The unchanged skips are `PackagedReminderApiTests.PackagedReminderApiRunsWithoutWorksTypes` and `TrustedEffectPackageContractTests.NamedPackagesExposeTrustedEffectContracts`, because `EVENTSTORE_PACKAGE_CONTRACT_DIR` is unset. No new skips were introduced.
- All three independent review layers completed; 13 findings were verified/triaged separately and patched in 11 groups. No findings were deferred, no layers were skipped, and no loopback was required.
- Both repositories pass `git diff --check`. Final inspection confirms only the test harness, CI documentation, and this maintenance spec changed. Immutable evidence, dependency configuration/pins, runtime source and Story 5.6 tracking remain unchanged; no nested submodules were initialized. Both owning indexes and HEADs remain unchanged. No commit was created, as the approved task explicitly prohibits staging or committing.
- All acceptance criteria are satisfied. This separate maintenance spec is `done`.
