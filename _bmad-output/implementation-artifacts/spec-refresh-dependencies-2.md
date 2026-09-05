---
title: 'Refresh root submodules and Hexalith package dependencies'
type: 'chore'
created: '2026-09-05'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'de5784ca751f51a4cfe282f67e11b13dd1ed4b45'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-20.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Root-declared submodules and the Hexalith packages consumed by Tenants may have advanced since the previous dependency sweep, leaving local source references or Release package references behind their authoritative upstream versions.

**Approach:** Re-resolve every root submodule against its live remote default-branch tip, fast-forward changed gitlinks, and align Tenants-consumed `Hexalith.*` package families to the latest published versions admitted by the repository's .NET 10, release-channel, audit, and family-alignment policies.

**Decision (2026-09-05):** “Latest” for packages means the latest stable published version. Do not adopt a proof or prerelease package identity; hold any submodule tip that would force one until a stable upstream replacement exists.

## Boundaries & Constraints

**Always:** Preserve the existing unrelated spec and Razor edits; work in the repository that owns each changed file; update only submodules declared by the root `.gitmodules`; use exact verified remote tips; keep Hexalith package families aligned through the Builds-owned central catalog and refreshed audit evidence; preserve Debug source-reference and Release package-reference behavior.

**Never:** Initialize nested submodules; use recursive or `--remote` submodule updates; rewrite divergent or dirty submodule worktrees; migrate to .NET 11; move a stable family to prerelease merely because its SemVer is higher; update unrelated non-Hexalith or npm dependencies; stage, commit, push, or discard pre-existing work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Changed clean submodule | Root gitlink is behind a verified default-branch tip by a fast-forward | Worktree and parent gitlink advance to the exact live tip | Stop for that submodule if dirty, divergent, or no longer root-declared |
| Current submodule | Gitlink already equals the live default-branch tip | Leave it unchanged and record a verified no-op | Report an unavailable remote without guessing a target |
| New eligible Hexalith package | Official feed and Builds audit admit a newer family version | Catalog, audit evidence, and package-only restore agree on the version | Retain the prior pin if channel, TFM, family, or validation policy rejects the candidate |

</frozen-after-approval>

## Code Map

- `.gitmodules` and `references/*` -- exhaustive allowed root-submodule set and parent-owned gitlinks; the final fetched `origin/main` tips are AI.Tools `5f93d2ec`, Builds `e81e6277` before the scoped package commits, Commons `6da79aed`, EventStore `6d436b3c`, FrontComposer `780dd5e9`, Memories `f7fef9fb`, and PolymorphicSerializations `8aeed1d2`.
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- authoritative central catalog; the accepted refresh keeps Commons `2.30.0`, EventStore `3.102.0`, and PolymorphicSerializations `1.19.2`, and advances FrontComposer `4.2.0` to `4.3.0` and Memories `2.22.1` to `2.24.1`.
- `references/Hexalith.Builds/Tools/audit-central-package-versions.ps1` -- supported official-feed audit generator; it records evidence but does not edit the catalog.
- `references/Hexalith.Builds/Tools/package-version-audit.json` and `package-version-exceptions.json` -- selected-version evidence and policy holds that must remain consistent with the catalog.
- `Directory.Packages.props` and `Directory.Build.props` -- Tenants import/wiring layer; inspect only unless updated upstream contracts require a local adjustment.
- `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` and `Program.cs` -- keep the independent Aspire SDK pin unchanged; use the consumer-owned Memories secret-store resource required by both the `2.24.1` package and current source.
- `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` -- explicitly bind `CommandStatus` to EventStore after the refreshed Redis package introduced a colliding type name.
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` -- focused centralization and package-authority guard.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.{AI.Tools,Builds,Commons,EventStore,FrontComposer,Memories,PolymorphicSerializations}` -- fetch each root-declared repository independently and verify its clean local `main` equals the exact `origin/main` tip; do not initialize nested submodules.
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` and `Tools/package-version-audit.json` -- advance the consumed FrontComposer family to `4.3.0` and the aligned Memories family to `2.24.1`, with revision-bound official-feed evidence in local commits `56f5bad` and `0fbbc73`.
- [x] `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` and `Program.cs` -- compile against the consumer-owned secret-store resource in both Debug source mode and Release package mode.
- [x] `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` -- alias EventStore `CommandStatus` explicitly -- resolves the refreshed Redis namespace collision without changing test behavior.
- [x] `_bmad-output/implementation-artifacts/spec-refresh-dependencies-2.md` -- record actual versions, changed gitlinks, validation results, and policy holds -- keeps the remaining gitlink race and compatibility work reviewable.

**Acceptance Criteria:**
- Given the root `.gitmodules`, when each declared submodule is compared with its live remote default branch, then every parent gitlink equals the exact tip and no nested submodule is initialized.
- Given the effective Tenants Release graph, when official package evidence is refreshed, then every consumed Hexalith family is either at its latest policy-admissible published version or has an explicit evidence-backed hold.
- Given Debug and Release evaluation, when restore, build, and governance checks run, then source conveniences remain Debug-only and package-only Release consumption resolves without family skew.

## Implementation Notes

- Concurrent `/pushall` work advanced the baseline commit and root pointers beyond the planning snapshot before implementation began. Those newer clean pointers were preserved; no gitlink was downgraded.
- A final independent fetch verified every root-declared submodule at its exact `origin/main` tip: AI.Tools `5f93d2ec8239494852c97032c819cb1689939e36`, Builds `e81e62770bcc72bc3d6722aefe6844775b82b6cf`, Commons `6da79aed2daa4e199689331ee3196f7872c0988a`, EventStore `6d436b3c271af1c3d0ef15b102c786465ed4a258`, FrontComposer `780dd5e901daa2472365e85b123de8b83e32cf22`, Memories `f7fef9fb4716721baffc7dfd435de4a2968428cc`, and PolymorphicSerializations `8aeed1d27c9a050bc4bec6d89051aa00de306a69`.
- The human selected latest-stable package policy on 2026-09-05. Official-feed evidence keeps Commons `2.30.0`, EventStore `3.102.0`, and PolymorphicSerializations `1.19.2`, and advances FrontComposer to `4.3.0` and Memories to the latest aligned stable `2.24.1`. `Hexalith.Memories.Contracts` alone lists `2.25.0`, but its Aspire and Client.Rest family siblings list `2.24.1`, so the aligned family cannot advance further.
- Builds commit `56f5bada93fb09464317fdf7821c3c099df269c6` changes only the two central version properties; `0fbbc735335352f5e563563ab3e50fa86c42a07b` changes only their revision-bound audit evidence. Both commit messages passed the repository's pinned commitlint CLI. Neither commit was pushed.
- Published `Hexalith.Memories.Aspire` `2.24.1` and the current Memories source tip both accept `IResourceBuilder<IDaprComponentResource>`, so the AppHost now creates and passes the same consumer-owned resource in both dependency modes.
- `StackExchange.Redis` `3.1.31` exposes a colliding `CommandStatus`; the integration test now aliases the intended EventStore enum.

## Spec Change Log

- 2026-09-05: Recorded the concurrent live-tip race, implemented package/source compatibility for the Memories AppHost helper, disambiguated EventStore `CommandStatus`, and captured validation evidence and remaining source-graph blockers.
- 2026-09-05: Reconciled all root submodules to freshly fetched tips, accepted stable FrontComposer `4.3.0` and aligned Memories `2.24.1`, removed the obsolete package/source signature split, and qualified both dependency modes.

## Review Triage Log

## Design Notes

“Latest” is evaluated independently for source and package channels: a submodule advances to its live default-branch tip, while a NuGet family advances only to a published, policy-admissible version. A source tip may legitimately contain commits beyond its latest package release.

## Completion Notes List

- references/Hexalith.Builds

The Builds pointer advances from the fetched upstream tip to the two local, scoped commits that accept the latest aligned stable FrontComposer and Memories packages and bind their audit evidence. No push was performed.

## File List

- references/Hexalith.Builds
- src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
- src/Hexalith.Tenants.AppHost/Program.cs
- _bmad-output/implementation-artifacts/spec-refresh-dependencies-2.md

## Verification

**Commands:**
- `pwsh -NoProfile -Command "& './Tools/audit-central-package-versions.ps1' -PriorAuditPath './Tools/package-version-audit.json' -Family @('hexalith-frontcomposer','hexalith-memories')"` from `references/Hexalith.Builds` -- expected: official-feed evidence selects the admitted aligned families.
- `pwsh -NoProfile -File ./Tools/validate-central-package-versions.ps1 && pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1 && pwsh -NoProfile -File ./Tools/validate-package-version-exceptions.ps1 -InventoryPath ./Tools/package-version-exceptions.json -CatalogPath ./Props/Directory.Packages.props` from `references/Hexalith.Builds` -- expected: catalog, audit, and exception policies pass.
- `dotnet build Hexalith.Tenants.Standalone.slnx -c Release -warnaserror -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false -p:HexalithMemoriesFromSource=false` -- expected: package-only Release build succeeds with zero warnings and errors.
- `dotnet build src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj -c Debug -warnaserror -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -m:1` -- expected: explicit source-mode build succeeds with zero warnings and errors.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Release` -- expected: package governance tests pass.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-refresh-dependencies-2.md && git diff --check && git submodule status` -- expected: changed gitlinks are declared, whitespace is clean, and all root worktrees match their gitlinks.

**Results:**
- The final incremental FrontComposer/Memories audit wrote 286 packages in incremental mode with 2 refreshed and 139 preserved families. Every FrontComposer row selects `4.3.0`; every Memories row selects `2.24.1`; generated evidence is bound to catalog commit `56f5bada93fb09464317fdf7821c3c099df269c6`.
- Central catalog validation passed 286 entries; audit validation passed 286 packages across 141 families and one source; exception validation passed 15 allowlisted exceptions; consumer package authority passed for all 17 Tenants projects.
- The package-mode AppHost and full `Hexalith.Tenants.Standalone.slnx` Release build passed with 0 warnings and 0 errors. The explicit Debug source-mode AppHost build passed with 0 warnings and 0 errors using `-m:1`; an initial parallel attempt hit two transient EventStore DLL copy locks before the single-node retry succeeded.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Release --no-restore` passed 132/132 tests.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false -p:HexalithMemoriesFromSource=false` passed 2774/2775 tests. The sole failure is the pre-existing concurrent `LocalizerDoubleParityTests` mismatch: 13 administrator-removal stub strings differ from the shipped resources; those unrelated files were not changed.
- `pwsh -NoProfile -File ./Tools/test-package-version-audit-validator.ps1` was stopped after 40 seconds with no output at the orchestrator's request; no result is claimed for that optional fixture lane.
- The parent gitlink/spec validator passes for the declared Builds move, whitespace validation passes, every root worktree matches its recorded gitlink except the intentionally pending Builds parent pointer, and all declared nested submodules remain uninitialized.
