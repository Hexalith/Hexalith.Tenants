---
title: 'Refresh dependencies and publish the current Tenants release'
type: 'chore'
created: '2026-11-03'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
context: []
baseline_commit: '2aa3f04014d9188fc59114113e3a6b0a2eb3964b'
---

<frozen-after-approval reason="User explicitly authorized updates, synchronization, fixes, publication, and bypass of human validation">

## Intent

**Problem:** The current Tenants source cannot restore on clean CI because it requires EventStore 4.0.2, which has not been published. Memories is one patch behind, and two root submodule branches have advanced remotely.

**Approach:** Synchronize root-declared submodules, adopt the latest compatible stable dependencies, publish the prerequisite producer, and run the current-source CI and release workflows through verified NuGet publication. The root agent owns external operations; the implementation handoff owns local edits only.

## Boundaries & Constraints

**Always:** Preserve user work and existing automated gates. Work in the owning repository. Validate every commit message using that repository's locked commitlint. Preserve the canonical-scope upgrade sequence: EventStore 4.0.2 before Tenants 7.0.0. Treat SDK 10.0.403 as current: official release metadata confirms it is latest stable. The user explicitly requested unattended execution and bypass of human validation; do not introduce approval checkpoints. Use an authenticated deployment approval for the requested release if available, without permanently weakening environment protections.

**Never:** Initialize nested submodules, use recursive/remote submodule updates, downgrade canonical EventStore dependencies, move published tags, reuse occupied NuGet versions, weaken failing automated checks, or update unrelated application features. Preserve explicit compatibility pins: EventStore.Testing.Integration 3.128.4, Microsoft.Build/Framework 18.9.6, Microsoft.OpenApi 2.13.0, and Node 24 types.

</frozen-after-approval>

## Code Map

- `global.json` and each root .NET submodule's `global.json` already select 10.0.403.
- `Directory.Build.props` sets EventStore 4.0.2, McpCli 2.0.0, Tenants 7.0.0, and Memories 3.0.17. Update only the stale Memories version.
- `references/Hexalith.Builds/Props/Directory.Packages.props` owns shared versions; EventStore default 4.0.0 is stale. Upgrade to 4.0.2 to align the producer required by this release. Preserve CRLF and BOM.
- `.gitmodules` lists nine allowed root submodules. All were clean on main. Fetch established FrontComposer is three commits behind origin/main and Platform is seven behind; all other roots are synchronized.
- `docs/guides/canonical-scope-upgrade.md` requires EventStore 4.0.2 before Tenants 7.0.0.
- `.github/workflows/ci.yml`, `story-guards.yml`, and `source-reference.yml` are release source gates. Latest Story Guards failed NU1102; CI was manually cancelled after the same failure.
- `.github/workflows/release.yml` prepares and publishes five packages through Trusted Publishing. `tools/release-packages.json` is the authoritative package inventory.
- `references/Hexalith.EventStore/.github/workflows/release.yml` and `scripts/verify-release-source.sh` govern prerequisite publication. Main CI and commitlint must succeed for its exact source.

## Tasks & Acceptance

**Execution:**

- [x] `Directory.Build.props` — advance Memories to 3.0.18; all other direct and tracked npm dependencies were verified current by registry audit.
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` — advance the shared EventStore default to 4.0.2, preserving documented compatibility exceptions and line endings.
- [x] `references/Hexalith.FrontComposer` and `references/Hexalith.Platform` — fast-forward clean main branches to the already-fetched origin/main; never initialize nested submodules.
- [x] `_bmad-output/implementation-artifacts/spec-refresh-and-publish-current-release.md` — append exact local validation outcomes and any additional required fixes to implementation notes. Do not stage, commit, or push during the implementation handoff.
- [ ] External operations (root agent after local handoff) — resolve EventStore exact-source CI, publish its next release, and verify required NuGet 4.0.2 packages exist; validate and commit owning repository changes, push submodules before root pointers, pass all current-source Tenants gates, dispatch Release, approve its deployment under the user's authorization, and verify all five NuGet packages.

**Acceptance Criteria:**

- Given the fetched root submodules are clean, when synchronization completes, then every root submodule is on its current remote main and nested submodules remain uninitialized.
- Given the required producer is published, when the latest Tenants source runs CI, then CI, Story Guards, and Source-reference integration complete successfully for that exact SHA.
- Given successful source gates, when Release runs, then the release tag resolves to the dispatched source and all five manifest packages are downloadable from NuGet at the released version.

## Implementation Notes

- Initial source: 2aa3f04014d9188fc59114113e3a6b0a2eb3964b. No initial tracked or untracked changes reported by git status.
- No human checkpoint is required: the user's explicit unattended publication instruction overrides skill approval defaults. Publication remains dependent on automated evidence.

- Local implementation handoff (2026-11-03): the starting root working tree contained only this untracked spec; all nine root submodules were clean on `main`. No user changes were overwritten, and no files were staged or committed.
- Updated the root Memories pin from `3.0.17` to `3.0.18` and the Builds catalog EventStore default from `4.0.0` to `4.0.2`. Byte-level assertions confirmed the Builds edit preserved its UTF-8 BOM and all 379 CRLF line endings; both configuration edits changed only their intended version string.
- Ran `git -c submodule.recurse=false merge --ff-only origin/main` in each owning repository. FrontComposer advanced from `25139b7f59e5ae5465d1665825e24c638928ccb2` to `e4df7256be0e89b48c830c0792033ecfeabec9ad`; Platform advanced from `28737966f2ec0cdbfc16e89a31c348f03a8058d2` to `ab56a8deb9bafef8a717640beba11aca43d8d65a`. Final direct Git checks confirmed all nine root branches equal their already-fetched `origin/main`, with all 67 nested submodule declarations still uninitialized.
- Passed from the Builds repository (exit `0` for each): `pwsh -NoProfile -File Tools/validate-central-package-versions.ps1` (314 entries); `pwsh -NoProfile -File Tools/test-authoritative-package-catalog.ps1` (51 approved identities and 4 shared versions); `pwsh -NoProfile -File Tools/validate-package-version-exceptions.ps1 -InventoryPath Tools/package-version-exceptions.json -CatalogPath Props/Directory.Packages.props` (16 allowlisted exceptions).
- Passed from Tenants: `dotnet --version` reported `10.0.403`; `python3 tests/scripts/test_nuget_dependency_ranges.py` ran all 12 tests successfully (exit `0`); `git diff --check` passed in both Tenants and Builds. Inspected both owning-repository diffs; no additional implementation fixes were required.
- Effective package-mode configuration check passed (exit `0`): `dotnet msbuild tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -nologo -p:Configuration=Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithEventStoreFromSource=false -p:HexalithFrontComposerFromSource=false -p:HexalithMemoriesFromSource=false -getProperty:HexalithEventStoreVersion,HexalithMcpCliVersion,HexalithTenantsVersion,HexalithMemoriesVersion -getItem:PackageVersion`. Parsed output assertions confirmed EventStore `4.0.2`, McpCli `2.0.0`, Tenants `7.0.0`, and Memories `3.0.18`, together with the unchanged EventStore.Testing.Integration `3.128.4`, Microsoft.Build/Framework `18.9.6`, and Microsoft.OpenApi `2.13.0` pins. No npm declarations or lockfiles changed, so Node 24 types remain intact.
- Current CI package build remains externally blocked: `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release -warnaserror -m:1 -nr:false -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithEventStoreFromSource=false -p:HexalithFrontComposerFromSource=false -p:HexalithMemoriesFromSource=false` returned exit `1` after 2.27 seconds, with 20 `NU1102` errors for unpublished EventStore `4.0.2` packages (nearest NuGet version `4.0.1`) and zero warnings. Compilation and C# test execution did not begin. The passing focused catalog, configuration, and dependency-range checks above are separate evidence; no gate or dependency was weakened.
- Remaining root-owned work: publish and verify EventStore `4.0.2`, validate commit messages and commit/push owning-repository changes, run exact-source Tenants CI/Story Guards/Source-reference integration, then publish and verify all five Tenants packages. No remote operations were performed during this handoff.

- Audit evidence retained at `dependency-refresh-2026-11-03-audit.json`: original observed registry values, audited source/catalog revisions, exact central query script, official SDK metadata URL, compatibility exceptions and two registry query errors. Exact original query times were not retained; the artifact distinguishes observation date from recording timestamp.

- Fixed the review-discovered Platform exact-cohort omission in its four project files, preserving existing exclusions. The existing matrix passed all 168 evaluations, including the selected default and 4.0.2 positive cases plus neighboring/prerelease/metadata/four-part refusals. Retained `platform-sdk-4.0.2-qualification.json`; this is configuration qualification, not runtime acceptance. Platform commit `1f5958e24d1b414d8956d079270f89556e23415d` uses a successfully prevalidated commitlint message.

- Shared catalog commit `10163c3e4ffd84ddb9b2831dea767a04da04f2ef` was created only after its exact full message passed the owning locked commitlint. The parent gitlink now identifies this immutable commit; push is held until EventStore 4.0.2 is published.
- EventStore source CI `41551160064` completed successfully for `f06abada8dc3aab4a5fd240847fd680d515c98a5`. An already-active release run `41554938023` is publishing that exact source; cancelled only this task's redundant queued run `41555034254`.

- Producer publication completed: EventStore release run `41554938023` succeeded for `f06abada8dc3aab4a5fd240847fd680d515c98a5`, tag `v4.0.2`. Independently downloaded all 14 signed NuGet archives after indexing propagated; archive IDs, versions, ZIP integrity, signature presence and every nuspec repository commit matched. Detailed local report: `/tmp/tenants-release-20261103-07JciR/eventstore-4.0.2/verification.json`. Its separate Integration workflow `41551159672` also succeeded.
- Pushed shared catalog commit `10163c3e4ffd84ddb9b2831dea767a04da04f2ef` only after package availability. Local stale NuGet indexes required focused `dotnet restore --no-http-cache --force-evaluate`, preserving all dependency gates. Tenants Server.Tests then built Release with zero warnings/errors and all 1,810 tests passed with zero skips. Platform Composition.Tests built Release with zero warnings/errors; 29 focused catalog/transport tests passed with zero skips.
- Builds CI exposed additional coupled fixture drift: the stored package audit, SDK schema/manifests, and synthetic readiness artifact hashes retained the prior 4.0.0 cohort. Incremental audit refresh updated only 13 EventStore rows, preserving the other 301; validation passed for all 314 entries, audit generator passed 115 scenarios, audit validator passed 103 scenarios. The separate fixture repair is tracked in `spec-fix-builds-sdk-cohort-fixtures.md`.
- Wider CI audit found and fixed the actual Firefox eager-form scroll race in FrontComposer. Explicit caller scoping preserves ordinary pointer row expansion. Local checks: 20 JavaScript regressions, 8 generated-component tests, actual Firefox route contract and 35 generator snapshot tests passed. Commits `a1a9280b28e291ee5cfaf9b7b6e3b5e4500ae805` and `f842711d67004200e3248b3c42b7d9bcb2146907` were prevalidated with owning commitlint and pushed; full CI/Quality is running. See `spec-fix-firefox-route-focus-ci.md`.

## File List

- `references/Hexalith.Builds`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Platform`

## Completion Notes List

- `references/Hexalith.Builds` moved to the committed shared catalog revision required for this release, including its fixture follow-up.
- `references/Hexalith.FrontComposer` moved to the source revision that fixes the Firefox route focus CI failure.
- `references/Hexalith.Platform` moved to the exact-cohort guard revision needed to compose EventStore 4.0.2.

- Builds fixture/audit repair committed as `70850b18e0d0b80facd2df0809974020dd935732` after locked commitlint validation. Independent root comparison read all 106 schema/fixture files and proved byte changes were only the selected SDK literal and SHA-256 replacements; unrelated audit package rows remained intact. Full Module.Tests: 2733 total, zero failures/errors, 3 existing environment skips. G4 package contracts with required controls passed; the separate source-fixture spec records detailed evidence and review triage. One pre-existing synthetic Source readiness metadata/binding follow-up is recorded in deferred-work.md; no current CI failure is deferred.
- Parallel work advanced root main through `f2995d98`, `daa1a601`, and `be43b928ce700ae7433b1d078eec356ccd6079f8` while this task ran. These commits and their native-read timeout/diagnostic fixes are preserved; final source gates and publication must use the resulting current main, not the earlier local dependency-refresh SHA.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence and disposition |
| --- | --- | --- |
| Blind 1: Platform excludes EventStore 4.0.2 catalog features | high; patch | Verified exact-cohort conditions in Composition, EventStoreHost, CatalogBuilder, and Composition.Tests include 4.0.0 but omit the newly selected 4.0.2. Existing catalog constants would disappear. Re-engaged implementation agent to add only the exact qualified cohort and verify existing boundaries. |
| Blind 2: Builds gitlink still shows dirty source | false | Review occurred before the already-required owning-repository commit. The execution task explicitly requires committing/pushing Builds before committing the root pointer. No parent commit has been created or published with a dirty pointer. |
| Blind 3: Spec omits container acceptance | low; rejected | This asks to edit the build spec. The existing release workflow already verifies both tenants and tenants-api destinations through verify-release-artifacts.sh and verify-api-release-artifact.sh; root will inspect those outcomes before claiming release completion. |
| Blind 4: Dependency audit evidence not linked | low; patch evidence | The audit was performed by an independent agent, but its detailed command/results are not linked in this artifact. Requested existing evidence paths and commands for retention; SDK official metadata is https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json. No dependency gate is changed. |

Edge-case review returned no findings. Verification-gap review found no verification gaps.

## Verification

- Run the narrowest existing dependency/build configuration checks relevant to each edit, then the existing GitHub CI source gates. Run test projects individually.
- Verify registry contents against `tools/release-packages.json`, checking exact version and downloadable package archives.
- Retain command outcomes and full canonical source and release identifiers. Do not claim publication from a green dispatch alone.

- External CI audit also found McpCli requires published Tenants 7.0.0 and an exact published Contracts artifact receipt. After Tenants publication, bind authentic archive bytes and the user-authorized automated approval; do not claim separate manual validation.
