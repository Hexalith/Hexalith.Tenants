# Release artifact verification evidence

This batch resolves only DW-527, DW-544, DW-582, DW-583 and DW-589 against specification `spec-dw-527-544-582-583-589-release-artifact-verification.md` and entry revision `25e3bf827a7963ec79b57b4918290b3fed728706`. No release was published during this work, and no dependency or gitlink changed.

NuGet validation retains the direct and centrally transitive dependency ranges separately for each target framework. Plain versions, `>=` minimums and inclusive unbounded minimum ranges normalize consistently; exact pins stay distinct. Missing, duplicate, mismatched, malformed and swapped framework/range evidence refuses admission. Both allowed technical ServiceDefaults IDs pass the same version/framework checks as other permitted dependencies; the exceptions only exempt their IDs from the forbidden-project name check. Dependency-free packages remain supported.

Recovery uses the tracked root Builds OCI validator to negotiate an OCI index and validate its immutable index, child manifests, sizes, exact two-platform set, config platforms and five exact source/repository/version provenance labels. Its accepted receipt is part of the prepared-state binding. A fresh observation must match that digest immediately before the first publication write, and a formerly absent tag must still be absent. Missing containers receive the shared imported provenance properties and a fresh accepted receipt after publication. This proof establishes immutable registry content with exact source provenance; it does not assert equality with an independently rebuilt image.

Normal publication derives the actual prepared version from all five reviewed packages, then requires the complete manifest filename inventory, matching NuGet payload content after repository signing, matching authenticated GitHub asset bytes, an exact source tag and the publisher OCI receipt digest. It invokes the approved copied Builds validator immediately after Semantic Release in the existing publication job. Recovery calls the same read-only destination checks. The separate exact-source publication job remains intact. Success receipts, raw OCI evidence, downloaded destination package bytes and hashes are retained below the workspace; existing symlink descendants and workspace escapes refuse admission. Both workflows upload retained evidence even when a step fails. NuGet and GitHub response bodies are retained separately, and failed recovery observations remain separate from accepted state.

The normal release execution SHA remains `22a578b576a515d2af214fe81859447fffc97981`; the recovery workflow still declares `2eac6955a812465a4308d194ae38fee35b766b05` while using the root Builds helper. That root helper's provenance CLI is newer than recovery's declared execution pin. DW-593 remains open, as do the separate credential separation records DW-560 and DW-563.

The mandatory Story Guards lane initializes only the root-declared Builds submodule, then executes the recovery and normal-publication fixtures against the real shared OCI validator and the range/framework validator fixtures. Contracts tests additionally exercise the actual scripts and parse/mutate the workflow binding so verification cannot be skipped or rebound.

## Verification

The first broad Python run passed 256/256 cases in 144.303 seconds. The initial affected Contracts pair passed 69/69 with zero errors, failures or skips in 95.935 seconds. The adapted older recovery inventory/publication/classification classes passed 173/173 with zero errors, failures or skips in 62.497 seconds. Final combined results are recorded below.

```text
dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Debug --no-restore -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -m:1 -nr:false
  Passed: zero warnings, zero errors. Initial ordinary restore/build also passed.
bash -n scripts/partial-release-recovery-common.sh scripts/validate-partial-release-recovery.sh scripts/prepare-partial-release.sh scripts/publish-partial-release.sh scripts/verify-partial-release.sh scripts/verify-release-artifacts.sh
  Passed.
actionlint .github/workflows/release.yml .github/workflows/recover-partial-release.yml .github/workflows/story-guards.yml
  Passed.
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-527-544-582-583-589-release-artifact-verification.md
  Passed: HEAD plus working tree, no references pointer changes.
git diff --check -- scripts .github/workflows tests/Hexalith.Tenants.Contracts.Tests tests/scripts
  Passed; existing C# CRLF normalization warnings only.
```

The initial six-class Contracts run had 38 fixture setup failures because older fixtures did not set `RECOVERY_FIXTURE` for the newly shared isolated registry transport. Adding that fixture environment binding corrected the failures; no production gate was weakened. Initial recovery fixture setup likewise lacked a path to the shared validator and was corrected before its passing focused run. These are implementation-time test failures, not passing evidence. A whitespace-only blank fixture line was removed after scoped diff checking identified it.

`python3 scripts/validate-changed-story-gitlinks.py` without a CI event failed with `GITHUB_EVENT_PATH or --event-path is required.` The parent completion step supplies the actual committed before/after event range. The pre-existing archive diff also reports a blank line at EOF; its bytes remain untouched and it is excluded from the task commit.

## Aspire baseline

The repository does have `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`. An initial mistaken absence statement was corrected before continuing implementation. The required baseline was obtained through the repository Aspire skill:

```text
UseHexalithProjectReferences=true UseNuGetDeps=false aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json --non-interactive
  Passed.
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
  Passed: Memories, its Dapr sidecar, graphs and vectors Running/Healthy;
  security Running/Unhealthy; Tenants, EventStore, API and UI Waiting on security.
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
  Passed, scoped to this AppHost.
```

The attempted `aspire stop` with `--format Json` was rejected because that option is unsupported by stop; the corrected scoped command above succeeded. These startup observations do not establish runtime acceptance or release readiness. No AppHost code changed.

## Ledger preservation

The entry dirty ledger and archive were preserved at `/tmp/tenants-dw-527-544-582-583-589-0r_75odq` before implementation. Their SHA-256 values are `5b7fb9dbfe0ece314cf208fdd9b9a9dfa042f077e6a83aa6c3bd37e0f3b6c5f9` and `7ff7509177a32e7f4ff51a830ac13ddfa016b6485811e520b1a0036e8326509c`. The executable batch verifier reverses only the five explicit status transitions and appended resolution fields, then requires the complete reconstructed ledger to match the entry byte hash. It also requires the archive byte hash to match, all 657 IDs to remain, the index to cover each remaining open record exactly once, and DW-560/DW-563/DW-593 to remain open. Original selected fields and historical findings remain visible. The task commit must exclude the pre-existing ledger cleanup and all archive edits.

Independent review, exact pinned commitlint validation, committed-range story checking and remote synchronization belong to the parent completion step and are not claimed by this implementation handoff.

## Final implementation results

```text
python3 -m unittest discover -s tests/scripts
  257/257 passed in 138.972 seconds; zero failures/skips.
tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests -class Hexalith.Tenants.Contracts.Tests.CiQualityGateScriptTests -class Hexalith.Tenants.Contracts.Tests.PartialReleaseRecoveryWorkflowTests -class Hexalith.Tenants.Contracts.Tests.PartialReleaseVerificationTests -class Hexalith.Tenants.Contracts.Tests.PartialReleaseRecoveryTests -class Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests -class Hexalith.Tenants.Contracts.Tests.PackageGovernanceTests
  484/484 passed in 96.774 seconds; zero errors/failures/skips/not-run.
python3 tests/scripts/test_release_artifact_verification.py
  Final fixture recheck: 10/10 passed in 10.622 seconds.
python3 tests/scripts/test_nuget_dependency_ranges.py
  Final fixture recheck: 8/8 passed.
python3 _bmad-output/implementation-artifacts/dw-527-544-582-583-589-verify.py
  Passed: exactly five transitions; 657 IDs, 545 done, 112 open, 102 exact-covering index rows.
  Reconstructed entry ledger and unchanged archive hashes match their dirty entry snapshots.
```

The raw passing logs and the initial affected-Contracts fixture setup failures are retained in [the raw log archive](dw-527-544-582-583-589-raw-logs.zip). The older `Remaining` prose in the maintenance index was stale at entry (122/112 while the actual rows represented 117/107); its current summary now matches the verified 112 open records across 102 rows. Every unrelated index row is preserved.

## Independent review corrections

All three review layers completed before triage. Fourteen initial findings reduced to ten concrete causes; each is recorded separately in the spec. The implementation now freezes reviewed package/manifest/receipt inputs, refuses evidence overlap, rechecks live main and preparation after OCI reads, revokes stale success markers before admission, separates destination response bodies, retains failed recovery observations, waits within a bounded NuGet readiness deadline, and normalizes supported NuGet version/framework aliases. No new deferred record or dependency change was introduced.

NuGet.org automatically adds repository signatures ([Microsoft signing reference](https://learn.microsoft.com/en-us/nuget/reference/signed-packages-reference)); package payload equality therefore permits only the signing entry to differ while raw destination hashes remain retained. Recovery preparation also checks its retained download against the accepted raw hash before comparing payloads. Publication readiness waiting accounts for the documented validation/indexing delay ([Microsoft publishing reference](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package)). Bound normalization follows [NuGet version semantics](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning).

After these corrections, the parent reran all 271 Python tooling cases (215.053 seconds) and all 484 affected Contracts cases (159.861 seconds), with zero failures or skips. A focused independent recheck confirmed all ten original causes resolved. It additionally identified that merely accepting a signing entry does not establish signature validity. Native `dotnet nuget verify --all` passed a real cached repository-signed Shouldly 4.3.0 package and rejected a copy with only its signature corrupted (exit 1); the native signature admission fix and its final gates are recorded below.

The final signing comparer requires `dotnet nuget verify --all` for every signed destination, including identical archives. Missing tooling, verification failure, a bounded timeout and archive mutation during verification all reject. Native diagnostics remain beside the retained archive. The complete comparer accepted a real Shouldly 4.3.0 repository-signed package and rejected the same payload with corrupted signature bytes. A second independent recheck confirms the signing concern resolved.

## Final parent verification

The final complete Python discovery run passed **273/273** in **196.182 seconds**. The final six-class Contracts run passed **484/484** in **151.052 seconds**, with zero errors, failures, skips or not-run cases. The source build has zero warnings/errors. Actionlint, shell syntax, scoped whitespace, the worktree story guard and exact-five ledger preservation all pass. The original archive bytes remain unchanged, and an independent index comparison proves all 652 unrelated committed ledger record bodies remain unchanged.

The exact full commit message passes the repository's lockfile-pinned commitlint 21.2.3 with zero problems/warnings. All review findings and follow-up signature controls are resolved; no new work is deferred. Only task-owned changes are staged. The user's original ledger cleanup and archive changes remain outside this commit. Final publication is the explicitly requested Git push to main, without running a product release.
