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
- `.gitmodules` -- exhaustive allowed root-submodule set; now nine root declarations, with each live default-branch tip recorded below.
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

- 2026-10-08: Refreshed the complete 304-package audit against published Builds `520abb5898ad44b30c0744e707b53cd94741e6b1`; adopted its aligned Aspire 13.6.1 family and SDK exceptions; updated root AppHost SDK and three npm pins; declared all nine baseline gitlinks; preserved concurrent external work and recorded unresolved broad build failures.

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

**Results (2026-10-08):**
- Live audit generation and deterministic audit/central-catalog/exception validators passed for 304 packages, 146 families, one configured source and 15 SDK/tool exceptions. All 41 Tenants direct package IDs are at the latest listed stable version. Aspire 13.6.1 comes from a published Builds commit; Dapr 1.19.0-rc.2, Shouldly 5.0.0-preview.2 and .NET 11 candidates remain outside the admitted stable/framework channel. Microsoft.OpenApi retains its documented compatible 2.x pin.
- Both Release restores passed. The current source-inclusive build log fails with 5 errors, including `CS0246` for `IDomainServiceAdministratorVerifier` and `DomainServiceAdministratorClaim` in `src/Hexalith.Tenants/Authorization/TenantsGlobalAdministratorVerifier.cs:19/22`. The standalone package-only build log fails with 7 errors, including the same missing published EventStore 3.115.0 types and external `TenantAuditPage.razor:1921` error `CS0136` for a shadowed `projection` variable. Cascade `MSB4181` failures are retained in the evidence. These broad gates are not green.
- Focused package-only AppHost Release restore/build passed with Aspire SDK 13.6.1 and 0 warnings/errors. Focused package-only Contracts Release build passed with 0 warnings/errors and its 144 tests passed. Explicit Debug-source and Release-package evaluations retain `net10.0` and the central EventStore 3.115.0 selection.
- Six per-project suites passed 5,182 tests without skips: Contracts 144, Client 50, Testing 181, Sample 39, Server 808 and UI 3,960. These `--no-build` runs cover previously built assemblies; subsequent external source edits are not claimed as covered by those results.
- `npm ci --ignore-scripts --engine-strict`, `npm audit signatures` and `npm outdated --json` passed: 503 verified signatures, 134 attestations and `{}`. Compatible transitive remediation reduced 18 advisory findings to 15 (13 high, 2 moderate); npm still proposes major downgrades for several release plugins and reports bundled-npm findings. No forced remediation or dependency override was used.
- The live submodule snapshot records all nine verified default-branch tips; nested submodules remain uninitialized. Root-only fast-forwards preserved external work. Gitlink declaration validation and whitespace checks passed. Audit generator fixtures passed 115 scenarios and gitlink-validator fixtures passed 23 tests. Audit-validator fixtures passed 103 scenarios.

## File List

- `package.json`
- `package-lock.json`
- `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`
- `_bmad-output/project-context.md`
- `_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
- `_bmad-output/implementation-artifacts/spec-refresh-dependencies-evidence-2026-10-08.json`
- `_bmad-output/implementation-artifacts/dependency-refresh-evidence-2026-10-08.json` -- separately attributed delegated implementation checks; preserves the concurrent evidence above.
- `references/Hexalith.Builds/Tools/package-version-audit.json`
- `references/Hexalith.AI.Tools`
- `references/Hexalith.Builds`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.McpCli`
- `references/Hexalith.Memories`
- `references/Hexalith.Platform`
- `references/Hexalith.PolymorphicSerializations`

## Completion Notes List

The spec remains in progress because the source/package administrator-verification gap and broad build failures prevent full acceptance. This run did not make product migrations, stage, commit or push. A concurrently published Builds commit made Aspire 13.6.1 admissible without editing uncommitted catalog declarations; the supported audit was regenerated against that committed revision.

The recorded baseline remains `8f6f8cb813255cc94ada95cda1a5e224c3b6bed0`. Current root HEAD at evidence capture is `c3c2b546e0d80e5793645e6926e5169d821a4738`. External commits and rebases occurred throughout this run; historical baseline-to-current pointer changes below are declared for provenance, without attributing those external changes to this implementation. This run fast-forwarded Builds and the clean EventStore checkout to exact verified published tips. Other roots were already current or advanced externally. No nested dependency was initialized.

| Root gitlink | True baseline to current checkout | Verified live tip |
| --- | --- | --- |
| `references/Hexalith.AI.Tools` | `de38f78ef7672df2a0997ddc60bf35ba0d02fa25 -> 3f194e17174994d308ec84af9ee2b5aa68674d0d` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| `references/Hexalith.Builds` | `307a043efac96208494ce8e9651920fe236d6d47 -> 520abb5898ad44b30c0744e707b53cd94741e6b1` | `520abb5898ad44b30c0744e707b53cd94741e6b1` |
| `references/Hexalith.Commons` | `5ff390a46685c72145de2337893f71ec8bc6a62c -> 116d26815eb81e35b3c161e1799e5ee12805fc0a` | `116d26815eb81e35b3c161e1799e5ee12805fc0a` |
| `references/Hexalith.EventStore` | `94591f3539ce30372db58e5fdd3ba017ea8c07b8 -> 3600d196799b7bbc5398a6d126e918171f07a2d3` | `3600d196799b7bbc5398a6d126e918171f07a2d3` |
| `references/Hexalith.FrontComposer` | `7a337a21d4ba261bf27aeb3feedde47789f0160a -> c561b3210f15206a90c39c82c58f2e5b1005cd60` | `c561b3210f15206a90c39c82c58f2e5b1005cd60` |
| `references/Hexalith.McpCli` | `0000000000000000000000000000000000000000 -> e159f82b7528797fc245045625ff387d65294ba9` | `e159f82b7528797fc245045625ff387d65294ba9` |
| `references/Hexalith.Memories` | `003fd21488d60307cd932a3139f69319a25cea66 -> aac6d9054cb138881e6e49c8e48233553123ffce` | `aac6d9054cb138881e6e49c8e48233553123ffce` |
| `references/Hexalith.Platform` | `0000000000000000000000000000000000000000 -> f043a2f242762233091abdaa5bbe1ab777bd0f12` | `f043a2f242762233091abdaa5bbe1ab777bd0f12` |
| `references/Hexalith.PolymorphicSerializations` | `93bcc44a65cd42fcc4558de8f8a8e4d523486157 -> 98de6e013840ece9f0fa7c68ab7dcdf2bba3b375` | `98de6e013840ece9f0fa7c68ab7dcdf2bba3b375` |

The matrix evidence JSON records every direct NuGet selection, npm pin, root tip, nested-submodule state, build blocker and relevant log digest. Logs are under `/tmp/tenants-refresh-*`; audit generation binds Builds `520abb5898ad44b30c0744e707b53cd94741e6b1`. The existing Aspire instance was inspected read-only; its restart is pending with the owner of the concurrent run.

## Delegated Implementation Verification Supplement (2026-10-08)

The preceding spec update and `spec-refresh-dependencies-evidence-2026-10-08.json` were written by a concurrent session and are preserved. The delegated implementation records its separate observations in `dependency-refresh-evidence-2026-10-08.json`; its results must not be conflated with the preceding `--no-build` observations.

- All 41 declared direct NuGet packages match their latest listed stable versions in the 304-package/146-family audit generated from Builds `520abb5898ad44b30c0744e707b53cd94741e6b1` at `2026-10-08T08:04:22.3574452+00:00`. There are no freshness holds in that direct graph; the prerelease/.NET 11/OpenAPI policy exclusions remain.
- The delegated implementation ran current per-project build/test commands, with no `--no-build`: Contracts Release **144**, Client Release **50**, Testing Release **181**, Sample Release **39**, and Server Debug source **825**. Total: **1,239 passed, zero failed/skipped**. Commands are `dotnet test --project <project> --configuration Release`, with `dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug -p:UseNuGetDeps=false` for the Server lane. Raw logs are `/tmp/tenants-refresh-test-{contracts,client,testing,sample,server-source}.log`.
- `dotnet build src/Hexalith.Tenants/Hexalith.Tenants.csproj --configuration Debug -warnaserror -p:UseNuGetDeps=false -m:1` passes with zero warnings/errors after retrying a transient concurrent copy lock. The passing source checks used EventStore `d87c969b6518751fafec9fbd01733d885b2efbfe`; subsequent external advancement to `3600d196799b7bbc5398a6d126e918171f07a2d3` is preserved and is not claimed as tested by those earlier checks. Debug/source evaluation selects EventStore project edges only; Release/package evaluation selects the complementary package edges only.
- Focused AppHost Release build with SDK `13.6.1` passes with zero warnings/errors. Both specified solution restores pass; broad Release builds remain blocked by the missing published EventStore administrator-verification APIs, and the standalone build also encounters the concurrent `TenantAuditPage.razor:1921` shadowed-local error. The delegated implementation did not rerun UI tests against this failing concurrent compilation.
- After the delegated compatible lockfile refresh, final `npm ci --ignore-scripts`, `npm outdated --json` (`{}`), and `npm audit signatures` pass with **503 signatures and 134 attestations**. Safe `npm audit fix --ignore-scripts` leaves **15 advisories** (13 high, two moderate) because bundled dependencies cannot be automatically replaced and other remedies require incompatible release-tool downgrades. These final lockfile results supersede this implementation's earlier 509/130 observation; the other session's evidence remains intact.
- All nine current root HEADs equal their observed live tips and all nested submodules remain uninitialized. This delegated implementation fast-forwarded **Platform only**, `794e8c63fe945bf689064edb8a406801810cb4cf -> f043a2f242762233091abdaa5bbe1ab777bd0f12`. Builds/EventStore/AppHost SDK/context changes were inherited or concurrent. It did not stage, commit, push, initialize nested dependencies, or change product code.
- The delegated central-catalog, audit, SDK-exception and Dapr validators passed; the gitlink-validator regression suite passed **23 tests**. The existing Aspire topology was inspected through `aspire ps`/`aspire describe` (19 resources Running, one Finished) and was not restarted amid concurrent product edits. Full acceptance remains blocked, so status stays `in-progress`.
