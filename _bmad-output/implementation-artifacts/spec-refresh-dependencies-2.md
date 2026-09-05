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

- `.gitmodules` and `references/*` -- exhaustive allowed root-submodule set and parent-owned gitlinks; investigation found Builds targeting `b7493539e4c6ede44d895524f4420f1c0ff51d40` and FrontComposer targeting `20d62abd1cf7d2f4bd06b9d6cd743c5abb204ffb`, with all other live `main` tips unchanged.
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- authoritative central catalog; live Builds tip changes `HexalithEventStoreVersion` from `3.101.0` to `3.102.0`.
- `references/Hexalith.Builds/Tools/audit-central-package-versions.ps1` -- supported official-feed audit generator; it records evidence but does not edit the catalog.
- `references/Hexalith.Builds/Tools/package-version-audit.json` and `package-version-exceptions.json` -- selected-version evidence and policy holds that must remain consistent with the catalog.
- `Directory.Packages.props` and `Directory.Build.props` -- Tenants import/wiring layer; inspect only unless updated upstream contracts require a local adjustment.
- `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` -- independent Aspire SDK pin; do not alter for a Hexalith-only refresh.
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` -- focused centralization and package-authority guard.

## Tasks & Acceptance

**Execution:**
- [ ] `references/Hexalith.Builds` -- fast-forward from `e0e0694` to verified live `main` and refresh its Hexalith EventStore audit evidence if the new catalog pin requires it -- adopts the latest published EventStore family through its owning repository.
- [ ] `references/Hexalith.FrontComposer` -- fast-forward from `1a7edded` to verified live `main` -- updates the remaining stale root gitlink without touching nested dependencies.
- [ ] `references/Hexalith.{AI.Tools,Commons,EventStore,Memories,PolymorphicSerializations}` -- recheck live default-branch equality and retain verified no-ops -- proves full root-submodule currency.
- [ ] `_bmad-output/implementation-artifacts/spec-refresh-dependencies-2.md` -- record actual versions, changed gitlinks, validation results, and any policy holds -- keeps the gitlink change declared and reviewable.

**Acceptance Criteria:**
- Given the root `.gitmodules`, when each declared submodule is compared with its live remote default branch, then every parent gitlink equals the exact tip and no nested submodule is initialized.
- Given the effective Tenants Release graph, when official package evidence is refreshed, then every consumed Hexalith family is either at its latest policy-admissible published version or has an explicit evidence-backed hold.
- Given Debug and Release evaluation, when restore, build, and governance checks run, then source conveniences remain Debug-only and package-only Release consumption resolves without family skew.

## Implementation Notes

## Spec Change Log

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
