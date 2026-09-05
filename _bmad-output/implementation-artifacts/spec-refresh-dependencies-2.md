---
title: 'Refresh root submodules and Hexalith package dependencies'
type: 'chore'
created: '2026-09-05'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '403c3f259cbca8bfa928163c42944e663c45998e'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md'
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-20.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Root-declared submodules and the Hexalith packages consumed by Tenants may have advanced since the previous dependency sweep, leaving local source references or Release package references behind their authoritative upstream versions.

**Approach:** Re-resolve every root submodule against its live remote default-branch tip, fast-forward changed gitlinks, and align Tenants-consumed `Hexalith.*` package families to the latest published versions admitted by the repository's .NET 10, release-channel, audit, and family-alignment policies.

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

- `.gitmodules` and `references/*` -- exhaustive allowed root-submodule set and parent-owned gitlinks; a 2026-09-05 recheck found Builds local `0a54e63a7903bd599e35b79159782b4c84d01c07` versus live `fe6002d7a2c01c5f9689425a9cd27520aacf4361`, FrontComposer local `58197d7c2845b4f371d9e80f2ef27d945d580731` versus live `d71790bb6a6df776558c6cb7051eaa93fe411397`, and Memories local `3a7a70259d0ff185947fcc2e4216f7a275651d68` versus live `396f8381c5627424b67b1b55441ceff7d782e1c1`; AI.Tools, Commons, EventStore, and PolymorphicSerializations remained exact.
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- authoritative central catalog; live Builds tip changes `HexalithEventStoreVersion` from `3.101.0` to `3.102.0`.
- `references/Hexalith.Builds/Tools/audit-central-package-versions.ps1` -- supported official-feed audit generator; it records evidence but does not edit the catalog.
- `references/Hexalith.Builds/Tools/package-version-audit.json` and `package-version-exceptions.json` -- selected-version evidence and policy holds that must remain consistent with the catalog.
- `Directory.Packages.props` and `Directory.Build.props` -- Tenants import/wiring layer; inspect only unless updated upstream contracts require a local adjustment.
- `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` and `Program.cs` -- keep the independent Aspire SDK pin unchanged; define the source-mode compile symbol and select the published-package or newer-source Memories secret-store signature without changing either mode's behavior.
- `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` -- explicitly bind `CommandStatus` to EventStore after the refreshed Redis package introduced a colliding type name.
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` -- focused centralization and package-authority guard.

## Tasks & Acceptance

**Execution:**
- [ ] `references/Hexalith.Builds` -- fast-forward from `e0e0694` to verified live `main` and refresh its Hexalith EventStore audit evidence if the new catalog pin requires it -- local `0a54e63a` remains behind live `fe6002d7`; the tip advanced during implementation and further remote operations were prohibited.
- [ ] `references/Hexalith.FrontComposer` -- fast-forward from `1a7edded` to verified live `main` -- local `58197d7c` remains behind live `d71790bb` after the same live-tip race.
- [ ] `references/Hexalith.{AI.Tools,Commons,EventStore,Memories,PolymorphicSerializations}` -- recheck live default-branch equality and retain verified no-ops -- AI.Tools `5f93d2ec`, Commons `6da79aed`, EventStore `5583e207`, and PolymorphicSerializations `8aeed1d2` are exact; Memories local `3a7a7025` remains behind live `396f8381`.
- [x] `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` and `Program.cs` -- compile against the consumer-owned secret-store resource in Debug source mode and the component-path signature in Release package mode -- preserves both dependency channels while the latest published Memories package trails its source tip.
- [x] `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` -- alias EventStore `CommandStatus` explicitly -- resolves the refreshed Redis namespace collision without changing test behavior.
- [x] `_bmad-output/implementation-artifacts/spec-refresh-dependencies-2.md` -- record actual versions, changed gitlinks, validation results, and policy holds -- keeps the remaining gitlink race and compatibility work reviewable.

**Acceptance Criteria:**
- Given the root `.gitmodules`, when each declared submodule is compared with its live remote default branch, then every parent gitlink equals the exact tip and no nested submodule is initialized.
- Given the effective Tenants Release graph, when official package evidence is refreshed, then every consumed Hexalith family is either at its latest policy-admissible published version or has an explicit evidence-backed hold.
- Given Debug and Release evaluation, when restore, build, and governance checks run, then source conveniences remain Debug-only and package-only Release consumption resolves without family skew.

## Implementation Notes

- Concurrent `/pushall` work advanced the baseline commit and root pointers beyond the planning snapshot before implementation began. Those newer clean pointers were preserved; no gitlink was downgraded.
- The current Builds catalog and live temporary audit agree that all 13 EventStore packages select the listed latest stable `3.102.0`. The later live Builds tip `fe6002d7` re-pins that family to `999.1.20-proof.fa2d1c9910f8`; it was not adopted because the tip arrived during the run and further remote operations were prohibited.
- Published `Hexalith.Memories.Aspire` `2.22.1` accepts `secretStoreComponentPath`, while the clean Memories source tip accepts `IResourceBuilder<IDaprComponentResource>`. `HEXALITH_MEMORIES_FROM_SOURCE` now selects the matching call at compile time; the resolved YAML path and resulting DAPR resource behavior are unchanged.
- `StackExchange.Redis` `3.1.31` exposes a colliding `CommandStatus`; the integration test now aliases the intended EventStore enum.

## Spec Change Log

- 2026-09-05: Recorded the concurrent live-tip race, implemented package/source compatibility for the Memories AppHost helper, disambiguated EventStore `CommandStatus`, and captured validation evidence and remaining source-graph blockers.

## Review Triage Log

## Design Notes

“Latest” is evaluated independently for source and package channels: a submodule advances to its live default-branch tip, while a NuGet family advances only to a published, policy-admissible version. A source tip may legitimately contain commits beyond its latest package release.

## Verification

**Commands:**
- `pwsh -NoProfile -File ./Tools/audit-central-package-versions.ps1 -PriorAuditPath ./Tools/package-version-audit.json -Family hexalith-eventstore` from `references/Hexalith.Builds` -- expected: current official-feed evidence selects the admitted EventStore family.
- `pwsh -NoProfile -File ./Tools/validate-central-package-versions.ps1 && pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1 && pwsh -NoProfile -File ./Tools/validate-package-version-exceptions.ps1 -InventoryPath ./Tools/package-version-exceptions.json -CatalogPath ./Props/Directory.Packages.props` from `references/Hexalith.Builds` -- expected: catalog, audit, and exception policies pass.
- `dotnet build Hexalith.Tenants.Standalone.slnx -c Release -warnaserror -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false` -- expected: package-only Release build succeeds with zero warnings and errors.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Release` -- expected: package governance tests pass.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-refresh-dependencies-2.md && git diff --check && git submodule status` -- expected: changed gitlinks are declared, whitespace is clean, and all root worktrees match their gitlinks.

**Results:**
- A live incremental EventStore-family audit generated from Builds `0a54e63a7903bd599e35b79159782b4c84d01c07` into an isolated temporary file: 286 packages, one refreshed family, 140 preserved families, and all 13 EventStore rows listed with selected/latest stable `3.102.0`. The tracked Builds worktree remained clean.
- Central catalog validation passed 286 entries; audit validation passed 286 packages across 141 families and one source; exception validation passed 15 allowlisted exceptions. The audit-generator regression lane passed all 111 scenarios.
- `dotnet build src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj -c Release -warnaserror -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false` passed with 0 warnings and 0 errors. The source-mode AppHost compile with `BuildProjectReferences=false` also passed with 0 warnings and 0 errors.
- `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release -warnaserror -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false` passed with 0 warnings and 0 errors.
- `dotnet build Hexalith.Tenants.Standalone.slnx -c Release -warnaserror -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false` passed with 0 warnings and 0 errors. `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj -c Release` passed 132/132 tests.
- Full Debug source-mode AppHost build remains blocked inside the concurrently advanced EventStore source: `AggregateActor.cs` cannot resolve `InspectPublicationRecoverySaveFailureAsync` at lines 1114 and 3212, followed by deconstruction inference errors at line 3212. The package/source compatibility call site itself compiled successfully when project-reference builds were isolated.
- `pwsh -NoProfile -File ./Tools/test-package-version-audit-validator.ps1` was stopped after 40 seconds with no output at the orchestrator's request; no result is claimed for that optional fixture lane.
- The final gitlink gate remains pending because Builds, FrontComposer, and Memories advanced on live `main` during the run and local pointers were deliberately left unchanged when further remote operations were prohibited. All declared nested submodules remain uninitialized.
