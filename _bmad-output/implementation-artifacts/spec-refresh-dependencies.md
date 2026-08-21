---
title: 'Refresh Tenants dependencies and root submodules'
type: 'chore'
created: '2026-08-21'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: '8f6f8cb813255cc94ada95cda1a5e224c3b6bed0'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-20.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Tenants' effective NuGet graph and root release tooling contain older package pins, including a source/package mismatch where EventStore source is newer than the published-package pin. Root submodules must also be confirmed against their live default-branch tips.

**Approach:** Move every Tenants-consumed package to the latest authoritative version admitted by the repository's .NET 10, release-channel, family-alignment, and audit policies; refresh npm tooling and its lockfile; and advance only root-declared submodules whose live tips changed. Treat an already-current dependency as a verified no-op.

## Boundaries & Constraints

**Always:** Work in the repository that owns each edit. Refresh the Builds package audit before selecting versions. Keep NuGet versions centralized, align package families and `Aspire.AppHost.Sdk`, preserve Debug-source/Release-package selection, use stable releases for stable pins, and declare every changed gitlink from the true baseline.

**Ask First:** Any target-framework migration, stable-to-prerelease move, package change outside the Tenants effective direct graph, non-mechanical product-code migration, or edit inside a submodule other than the package-owning Builds repository.

**Never:** Use recursive or `--remote` submodule updates, initialize nested submodules, add inline `PackageReference` versions, bypass package-audit locks, take .NET 11 packages, edit unpublished state, commit/push/stage, or weaken validation to force a nominally newer version.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible package | Authoritative registry reports a newer policy-admissible version | Owning catalog/manifest and generated evidence move together | Stop that family if restore, audit, engine, or compatibility checks reject it |
| Ineligible package | Candidate is prerelease-only, wrong TFM/channel, family-split, or explicitly locked | Existing compatible pin remains with evidence explaining the hold | Do not substitute an unaudited version |
| Root submodule | Gitlink differs from a verified live default-branch tip | Explicit root gitlink advances to that exact commit | Leave dirty/divergent modules untouched and report them |
| Already current | Pin or gitlink equals authoritative latest eligible target | No file mutation | Record the verified no-op |

</frozen-after-approval>

## Code Map

- `Directory.Packages.props:3-13` -- read-only Tenants shim importing the Builds-owned catalog; local overrides are disabled.
- `references/Hexalith.Builds/Props/Directory.Packages.props:3-318` -- authoritative NuGet catalog; limit edits to packages directly consumed by Tenants.
- `references/Hexalith.Builds/Tools/package-version-audit.json:1-5` -- stale generated audit evidence to refresh with the supported tooling.
- `references/Hexalith.Builds/Tools/package-version-exceptions.json:5-104` -- Aspire SDK exceptions that must match the central Aspire family.
- `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj:1` -- root-owned `Aspire.AppHost.Sdk` pin.
- `package.json:6-15` and `package-lock.json:1-18` -- root npm release/commit tooling and reproducible lock.
- `.gitmodules:1-21` -- exhaustive allowed root-submodule set; all seven matched live `main` at planning time.
- `_bmad-output/project-context.md:20-32` -- current dependency baseline documentation to reconcile after accepted changes.
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs:107-133` -- focused centralization and package-authority guard.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/Tools/package-version-audit.json` -- regenerate live authoritative package evidence using the tracked audit workflow -- prevents stale or semantically mis-ranked choices.
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` and `Tools/package-version-exceptions.json` -- update only Tenants-consumed eligible packages and aligned families -- resolves effective NuGet drift without broadening to unrelated consumers.
- [x] `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` -- align the Aspire SDK with the admitted Aspire Hosting version -- keeps AppHost tooling coherent.
- [x] `package.json` and `package-lock.json` -- update all direct npm dev dependencies to latest stable compatible releases -- keeps release and commit tooling reproducible.
- [x] `_bmad-output/project-context.md` -- reconcile current-version facts with the resulting graph -- prevents agents from reintroducing stale pins.
- [x] `references/*` -- re-resolve each declared remote tip and advance only changed root gitlinks -- fulfills submodule currency without touching nested dependencies.

**Acceptance Criteria:**
- Given the refreshed authoritative audit, when the effective Tenants graph is inspected, then every direct package is either at the latest admissible version or has a recorded policy hold.
- Given Debug and Release evaluation, when dependencies resolve, then Debug source conveniences and Release package-only behavior remain intact with no source/package version conflict.
- Given all root-declared submodules, when compared with live default-branch tips, then every gitlink matches exactly and every nested submodule remains uninitialized.
- Given the updated dependency graph, when repository validation runs, then restore, warning-as-error builds, package governance, and relevant per-project tests pass.

## Spec Change Log

- 2026-08-21: Regenerated the 284-package live audit; admitted EventStore 3.96.2 and Fluent UI rc.5; refreshed all direct npm tooling; recorded policy and compatibility holds for Aspire, Dapr, Shouldly, .NET 11 candidates, and xUnit 4; verified all seven root gitlinks already matched their live `main` tips.

## Design Notes

"Latest" means latest eligible, not highest SemVer string. The Builds audit and exception validators are the authority for TFM compatibility, stable/prerelease channels, family rollback groups, Dapr locks, and the Microsoft.OpenApi 2.x hold. A rejected candidate is a successful compatibility decision when its evidence is preserved.

## Verification

**Commands:**
- `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1` (from `references/Hexalith.Builds`) -- expected: refreshed audit and catalog dispositions pass.
- `pwsh -NoProfile -File ./Tools/validate-package-version-exceptions.ps1 -InventoryPath ./Tools/package-version-exceptions.json -CatalogPath ./Props/Directory.Packages.props` -- expected: Aspire SDK/catalog alignment passes.
- `dotnet restore Hexalith.Tenants.slnx -p:Configuration=Release && dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror` -- expected: zero warnings and errors.
- `dotnet restore Hexalith.Tenants.Standalone.slnx -p:Configuration=Release -p:UseNuGetDeps=true && dotnet build Hexalith.Tenants.Standalone.slnx --no-restore --configuration Release -warnaserror -p:UseNuGetDeps=true` -- expected: package-only consumer path passes.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release` -- expected: package and solution governance pass.
- `npm ci --ignore-scripts && npm audit signatures` -- expected: lockfile installs and registry signatures verify.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-refresh-dependencies.md && git diff --check && git submodule status` -- expected: declared gitlink scope, whitespace, and submodule state pass.

**Results:**
- Live NuGet audit passed for 284 packages in 139 families; central-catalog, exception, Dapr-lock, and 29 audit-validator fixture checks passed.
- Package-only `Hexalith.Tenants.Standalone.slnx` Release restore/build passed with 0 warnings and 0 errors. Contracts, Client, Testing, Sample, Server, and UI suites passed 3,298 tests in total.
- `npm outdated --json` returned `{}`; clean install and registry signature verification passed with 506 signatures and 121 attestations. Seven bundled-npm advisories remain because npm's proposed remediation downgrades `semantic-release`.
- Gitlink validation and its 19 regression tests passed. All seven root gitlinks match the live default-branch tips captured during this run; nested submodules remain uninitialized.
- Source-inclusive `Hexalith.Tenants.slnx` Release build is blocked by four unchanged Memories errors: three obsolete `RedisConnectionException` constructor calls and analyzer `SER301`. Editing `references/Hexalith.Memories` requires human approval under the frozen scope.
