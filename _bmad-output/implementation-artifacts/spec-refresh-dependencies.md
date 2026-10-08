---
title: 'Refresh Tenants dependencies and root submodules'
type: 'chore'
created: '2026-08-21'
status: 'done'
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
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props`, `Tools/package-version-audit.json`, and `_bmad-output/project-context.md` -- apply the user's 2026-10-08 continuation: upgrade the aligned EventStore family to exactly `3.117.0`, refresh supported authoritative evidence, and validate the Tenants package consumer against those published packages.
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

The user explicitly requested EventStore `3.117.0` on 2026-10-08 after the missing administrator-contract blocker. All 13 centrally aligned EventStore package IDs publish that stable version on NuGet.org. This continuation targets that exact family version. Preserve unrelated concurrent work and historical verification; capture new commands/results separately. Prepare and validate the upgrade without staging, committing, or bypassing the audit's committed-catalog provenance requirement. If supported audit generation or validation rejects an uncommitted declaration, retain the concrete upgrade and its build evidence as a pending governed change and report the exact blocker.

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
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- externally committed EventStore `3.117.0` family upgrade consumed by this continuation.
- `references/Hexalith.Builds/Tools/package-version-audit.json`
- `_bmad-output/implementation-artifacts/dependency-refresh-eventstore-3.117.0-evidence-2026-10-08.json` -- separately attributed continuation checks.
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
| `references/Hexalith.Builds` | `307a043efac96208494ce8e9651920fe236d6d47 -> 893db14b25843db140942d839e4d659584221315` | `893db14b25843db140942d839e4d659584221315` |
| `references/Hexalith.Commons` | `5ff390a46685c72145de2337893f71ec8bc6a62c -> 116d26815eb81e35b3c161e1799e5ee12805fc0a` | `116d26815eb81e35b3c161e1799e5ee12805fc0a` |
| `references/Hexalith.EventStore` | `94591f3539ce30372db58e5fdd3ba017ea8c07b8 -> 0dad344d37343f589d859d6d8d6701283122b338` | `0dad344d37343f589d859d6d8d6701283122b338` |
| `references/Hexalith.FrontComposer` | `7a337a21d4ba261bf27aeb3feedde47789f0160a -> 0e114214007c22f5cdbac21a6853cff4208340ee` | `0e114214007c22f5cdbac21a6853cff4208340ee` |
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

## EventStore 3.117.0 Continuation Verification (2026-10-08)

This supplement preserves the earlier run's observations and records the explicitly requested `3.117.0` continuation separately in `dependency-refresh-eventstore-3.117.0-evidence-2026-10-08.json`. The earlier missing published administrator-verification API blocker is resolved by this package version; the historical source and broad-build results above are not reclassified.

- The supported command `pwsh -NoProfile -File ./Tools/audit-central-package-versions.ps1 -PriorAuditPath ./Tools/package-version-audit.json -ChangedFamily hexalith-eventstore`, run in Builds before the catalog edit, refreshed one family and preserved 145 families across 304 packages. It binds committed Builds `ad52c5bdd4361c59eedf12a16620150006403584` at `2026-10-08T09:24:57.5078060+00:00`. All 13 EventStore IDs report latest listed stable `3.117.0`; every other package row and family decision is preserved unchanged.
- The single `HexalithEventStoreVersion` declaration now prepares all 13 aligned catalog packages at exactly `3.117.0`; catalog UTF-8 BOM and CRLF are preserved. The project context records the resulting version and pending audit governance. No other family, npm package, product code or gitlink was changed by this continuation.
- Current package-only Release host restore and warning-as-error build pass with zero warnings/errors. `project.assets.json` selects published EventStore `3.117.0`, including `DomainService` and `Gateway`; the prior missing administrator-verification APIs no longer prevent compilation.
- Current package-only Release tests pass without `--no-build`: **Server 825**, **Contracts 144**, totaling **969 passed, zero failed/skipped**. Explicit Debug/source and Release/package MSBuild evaluations retain `net10.0` and `3.117.0` while selecting complementary EventStore project/package edges.
- Central catalog validation passes for 304 entries and SDK/tool exception validation passes for 15 entries. Full solution acceptance checks remain with the parent implementation workflow.
- **Pending governed change:** supported post-edit audit regeneration exits **1** with `Central package freshness audit failed: catalog 'Props/Directory.Packages.props' is dirty relative to generated-from revision 'ad52c5bdd4361c59eedf12a16620150006403584'.` The generated pre-edit artifact remains truthful: its accepted selection is the committed `3.115.0`, with `3.117.0` recorded as the current listed candidate. `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1` exits **1** with **14 errors**: the catalog hash mismatch and one unmatched accepted selection for each of the 13 prepared EventStore pins. The provenance requirement was not bypassed, and no staging or commit was performed. An accepted `3.117.0` audit requires the owning Builds catalog change to be committed through its governed workflow, then supported regeneration and validation.

The continuation remains unchecked and the spec remains `in-progress` until governed audit acceptance and the remaining broad gates succeed. Raw focused logs are under `/tmp/tenants-eventstore-3117-*`; the evidence artifact records their SHA-256 digests and exact commands.

## EventStore Committed-Catalog Revalidation (2026-10-08)

The preceding continuation observations are historical. Before this resumed implementation, external commits advanced Builds to `893db14b25843db140942d839e4d659584221315` and committed the aligned EventStore `3.117.0` catalog. Focused checks began at root HEAD `e3bfdcc8cd3b3d9769e24f461b812d0a4f96cfcf`; external documentation-only commit `1846c7128cf0f6b16f9e62f38032bcab78e26e38` arrived before the final pointer checks, without changing product source or dependencies. The new `committedCatalogRevalidation` section in `dependency-refresh-eventstore-3.117.0-evidence-2026-10-08.json` records this run separately and supersedes the earlier pending catalog-provenance blocker.

- Supported incremental audit regeneration passed against committed Builds `893db14b25843db140942d839e4d659584221315` at `2026-10-08T09:37:07.9735246+00:00`: 304 packages, one refreshed EventStore family, and 145 preserved families. All inherited package rows and non-EventStore family decisions remain unchanged; the EventStore decision only receives the new observation time. Deterministic audit validation now passes for 304 packages, 146 families and one source. Central catalog validation passes for 304 entries, and SDK/tool exception validation passes for 15 entries.
- All 13 aligned EventStore selections are exactly `3.117.0`. NuGet now lists stable `3.117.1` for every family member. The explicit continuation version remains `3.117.0`; this run records the available patch and does not claim the selected version is registry-latest.
- The current package-only Release host restore and warning-as-error build pass with zero warnings/errors. Current package-only Release tests pass without `--no-build`: Server **825**, Contracts **144**, totaling **969 passed, zero failed/skipped**. Resolved host, Server and Contracts assets select only published EventStore `3.117.0`. Explicit Debug/source and Release/package evaluations retain `net10.0`, `3.117.0`, and complementary EventStore project/package edges.
- All nine root HEADs match freshly observed live default-branch tips, and every nested submodule remains uninitialized. The pointer matrix above is reconciled to the current tree from the unchanged true baseline `8f6f8cb813255cc94ada95cda1a5e224c3b6bed0`; story gitlink validation and both root/Builds whitespace checks pass. Its earlier observed Builds `520abb5898ad44b30c0744e707b53cd94741e6b1`, EventStore `3600d196799b7bbc5398a6d126e918171f07a2d3`, and FrontComposer `c561b3210f15206a90c39c82c58f2e5b1005cd60` tips remain historical observations in the preceding evidence; the current advances were external and are not attributed to this run.
- This resumed implementation refreshed only generated audit evidence and reconciled dependency documentation. It did not modify the committed catalog, product code, npm pins or gitlinks, and did not stage, commit, push or initialize nested dependencies. The execution task is checked; broad solution acceptance remains with the parent workflow, so the spec remains `in-progress`.

Raw focused logs are under `/tmp/tenants-eventstore-3117-revalidation-*`; the evidence supplement records exact commands and SHA-256 digests.

## Final EventStore 3.117.0 Verification (2026-10-08)

This final supplement supersedes the preparation-stage governance blocker above. An external session published the catalog change in Builds `893db14b25843db140942d839e4d659584221315`; this run then regenerated and validated supported incremental evidence against that committed catalog. No staging, commit or push was performed by this run.

- All 13 EventStore catalog selections are accepted at exactly `3.117.0`. The deterministic package audit, central-catalog and 15 SDK/tool exception validators pass. The incremental snapshot refreshes EventStore and preserves the other 145 families. NuGet published `3.117.1` during final validation; the explicit user request holds this continuation at `3.117.0`.
- Both specified solution Release restores and warning-as-error builds pass with zero warnings/errors. The standalone check additionally uses `-p:HexalithFrontComposerFromSource=false`, confirming the build consumes packaged FrontComposer as well as packaged EventStore. Exact commands and log digests are in `dependency-refresh-eventstore-3.117.0-final-evidence-2026-10-08.json`.
- Current per-project tests pass without `--no-build`: Contracts 144, Server 825, Client 50, Testing 181, Sample 39, and default UI 3,986: **5,225 passed, zero failed/skipped**. EventStore remains in package mode for these tests; the UI retains its documented available-FrontComposer-source exception.
- A supplemental UI run forced to published FrontComposer fails 151 of 3,985 tests (150 report the missing `NavigationFailureNotifier` service on `FcPageTabs`). The FrontComposer version and policy are unchanged by this EventStore upgrade; this evidence is retained as a separate existing source/package compatibility limitation, not represented as a passing lane.
- All nine root checkouts equal the captured live default-branch tips and nested submodules remain uninitialized. Root pointer advances and catalog publication observed during validation were performed externally and preserved. The gitlink table above now records the current shipped values; the original baseline is preserved.

Additional file: `_bmad-output/implementation-artifacts/dependency-refresh-eventstore-3.117.0-final-evidence-2026-10-08.json`.

## Review Triage Log

All three reviewers completed against the complete diff from the unchanged baseline, including historical and concurrent work. The explicit continuation upgrades the EventStore family to `3.117.0`; no reviewed product or tooling implementation was changed by that upgrade. Each finding was assessed separately before grouping. No surviving finding requires a patch or a loopback for this dependency change.

| ID / layer | Finding | Verdict | Route | Verification evidence |
| --- | --- | --- | --- | --- |
| BH1 / blind | Older projection replay can regress the singleton tenant index. | medium | defer | `TenantProjectionHandler.cs:156` applies every event to the index, while its detail transform guards sequence numbers; `TenantIndexReadModel.Apply(TenantUpdated)` overwrites the current name. The older-replay test initializes an empty index and checks only detail. This behavior predates the upgrade (last handler edit `b2b80941`, 2026-08-22); defer the per-aggregate index watermark and persisted-index replay coverage. |
| BH2 / blind | The custom `/project` route bypasses legacy projection evolution admission. | medium | defer | `Program.cs:173` invokes the Tenants dispatcher directly; that dispatcher does not reject evolution hints or versioned event metadata before persistence. The same `RequireLegacy` guard already exists in the source revision `283b07a52c9c70e1c940164a7011ee8c3ad98b2d` embedded in the published `3.115.0` DomainService nuspec. The custom route predates this upgrade; defer restoring admission parity without changing this dependency refresh. |
| BH3 / blind | A staged-only gitlink change can escape the working-tree check. | false | reject | An isolated repository using existing objects reproduced a checkout equal to HEAD with a different staged Builds pointer. The guard's exact `git diff --ignore-submodules=dirty --no-abbrev --raw HEAD` still returns the path as a modification with a null target SHA, so the recorded-commit branch emits `[WORKTREE MOVE]` and fails. Resolving the checkout SHA does not discard the returned modification. |
| BH4 / blind | A newly added submodule fails the recorded pointer chain. | medium | defer | `current_pointer(path, baseline)` returns `None` for an absent gitlink, but the addition's raw old SHA is forty zeroes; the unconditional comparison at `validate-story-gitlinks.py:428` therefore emits `[CHAIN GAP]` for a valid addition. This recorded-commit branch was introduced before the dependency continuation (`31f401c1`, 2026-09-23); defer null-SHA normalization and addition/removal coverage. |
| BH5 / blind | NuGet artifact validation discards dependency versions and framework groups. | medium | defer | `load_restore_dependencies` and `get_metadata` both reduce dependencies to ID sets. An isolated nuspec with EventStore `[3.115.0]` in `net9.0` passes the boundary validator when the expected ID matches; neither the version nor framework survives the API. The validator predates this upgrade (`7b46a7b8`, 2026-09-02); defer comparison against versioned framework restore evidence. |
| BH6 / blind | Deduplicating administrator pages can falsely prove completeness. | false | reject | The real query handler pages a unique `HashSet<string>` using ordinal ordering and an exclusive lower-bound cursor, so a stable projection cannot emit overlapping pages. The loader rejects projection-version changes and repeated cursors. Its explicit deduplication test returns the complete ordinal identity union; duplicate rows alone do not demonstrate an omitted administrator. The proposed new rejection would change that established behavior without a reachable loss case. |
| BH7 / blind | The token response cap is enforced after HTTP buffering. | medium | defer | `TenantBootstrapCredentialProvider.cs:86` uses buffered `PostAsync` before `LoadIntoBufferAsync(64 KiB)`, and its caller creates a default client without a smaller response buffer cap. An oversized authority response can consume memory before the intended bound is checked. Both files were introduced before this upgrade (`81144734`, 2026-10-07); defer headers-first bounded reading and an oversized-response regression. |
| BH8 / blind | Performance provenance omits dirty files and checked-out submodule revisions. | medium | defer | The runner records only root HEAD in `environment.json`, then restores/builds the working tree without a cleanliness check or source snapshot. Different local candidates can therefore carry the same revision. The runner predates this upgrade (`697cdb58`, 2026-09-24); defer exact source capture or a clean-candidate guard. This does not establish that the separately recorded clean historical performance run was invalid. |
| BH9 / blind | The timing browser script lacks a required protected-cursor gate. | false | reject | Approved performance contract revision 1 permits separate functional gates. `TenantQueryCursorCodecTests` rejects malformed, tampered and cross-caller cursors, and `TenantsProjectionActorTests.GetTenantAudit_rejects_cursor_scope_mismatch_before_audit_state_readAsync` covers tenant, date-range, category and caller isolation before audit reads. These checks belong to the passing 825-test Server lane; their absence from the timing script does not leave the behavior unverified. |
| BH10 / blind | Fourteen public command types share one C# file. | low | defer | `TenantCreateCommandModels.cs` contains fourteen public records/enums, contrary to the required baseline's explicit one-type-per-file authoring rule; create, membership, metadata and lifecycle changes share this review surface. The file's latest edit predates the upgrade (`b63bdbba`, 2026-10-04). Defer the mechanical split to the owning command-flow work. |
| EC1 / edge | Focus watchdog can act after a route change. | low | reject | `tenantsFocus.js:252` omits the route-equivalence check present in the observer/interval callback; a navigation immediately before the 20-second watchdog can therefore act on a still-present matching heading. This uncommon timeout race has negligible impact, and fixing it adds a guard branch. Reject under the review rule for rare low-impact findings requiring extra guards. The code predates this upgrade. |
| EC2 / edge | Blanket Release package-only prose conflicts with the UI's source preference. | low | reject | Release UI evaluation does prefer initialized FrontComposer source. `Directory.Build.props` has explicitly documented this non-packable UI/test exception since `635c3374` (2026-08-29), and the continuation preserves it; the standalone validation explicitly forces `HexalithFrontComposerFromSource=false` and passes. The remaining blanket acceptance wording is a claim-only spec issue, whose fix would edit this build's spec; reject as directed. The supplemental published-FrontComposer UI failures remain recorded above. |
| VG1 / verification | Recorded gitlink continuity has no mutation-sensitive regression. | medium | defer | Accepted as pre-verified: the reviewer read the only executable guard suite and disabled only `[CHAIN GAP]` in memory; all 23 tests still passed. Existing cases cover single commits, not an omitted intermediate pointer-changing commit. The guard and tests predate the dependency continuation; defer a valid two-commit case and an omitted-intermediate CLI failure case. |

Review result: thirteen findings triaged, three refuted, two low-impact findings rejected, and eight pre-existing issues appended to `deferred-work.md`. No product code, validation policy or dependency selection changed during triage. Required verification remains green; the existing supplemental FrontComposer package-only UI limitation remains explicit.


### Focused continuation evidence review

The preceding thirteen findings and deferred-work entries were recorded by a concurrent review and are preserved. This parent workflow's three independent reviewers also completed the focused EventStore continuation review, with the full original-baseline diff retained for provenance. Its edge-case and verification-gap layers reported no findings. Each of its ten blind-hunter findings is recorded separately below, before grouping; none duplicates the preceding location/claim pairs.

| ID / layer | Finding | Verdict | Route | Verification evidence |
| --- | --- | --- | --- | --- |
| CE1 / blind | Distinguish accepted selection wording from the audit's retained family disposition. | low | reject | The generated family disposition remains `retained`, and its compatibility evidence does not claim upstream owner acceptance. All 13 catalog/audited selections are `3.117.0`; the validator accepts that retained baseline while `3.117.1` remains an unadopted candidate. The proposed wording fix includes this build's spec, so reject under the review rule against fixes to the spec. Completion below states the exact validation scope. |
| CE2 / blind | Historical in-progress paragraphs disagree with the in-review frontmatter. | low | reject | Earlier supplements preserve intermediate observations, and the final supplement supersedes their blockers. Their statuses describe earlier stages. Reject the proposed historical rewrite because its fix edits this build's spec. |
| CE3 / blind | Final check commands omit their owning working directory. | low | patch | Relative `./Tools` commands require the Builds directory while solution/test commands require the root. Each check now records its correct `workingDirectory`; all 16 entries were verified. |
| CE4 / blind | Preparation evidence lacks a forward link to completed verification. | low | patch | Root and committed-catalog historical records retained pending statuses without an explicit link. Both now contain a resolving relative `supersededBy` link; historical observations remain intact. |
| CE5 / blind | Temporary raw logs will disappear after cleanup. | low | patch | The cited files exist and their hashes match now, but `/tmp` is ephemeral. All 16 final raw logs are now preserved in a compact archive beside the evidence, with archive SHA-256 and per-check entries matching the existing log digests. |
| CE6 / blind | Concurrent root advancement makes tested input attribution uncertain. | false | reject | The only committed root difference from `e3bfdcc` to `1846c71` is `spec-frontcomposer-tab-contract.md`; no product source, catalog or gitlink changed. Final evidence binds Builds `893db14`, the exact audit hash, commands and raw-log hashes. The claimed untracked dependency/input transition did not occur. |
| CE7 / blind | The earlier submodule snapshot cannot support the final root revision. | false | reject | The snapshot explicitly records its own capture time and root `e3bfdcc`, rather than claiming capture at `1846c71`. That later documentation-only commit leaves all nine gitlinks unchanged; fresh final pointer checks verified the same tips. |
| CE8 / blind | Resolved assets evidence leaves other EventStore consumers uncertain. | false | reject | Parent inspection covered all nine owned production asset files: host, API, AppHost, Aspire, Client, Contracts, Server, Testing and UI. Every resolved EventStore library is `3.117.0`; the single central family property and both successful solution builds agree. The abbreviated focused summary does not indicate a mixed graph. |
| CE9 / blind | Missing local pack and isolated-consumer checks imply unsupported package verification claims. | false | reject | Neither evidence nor acceptance claims a local pack or isolated package probe. This continuation changes the consumed family pin and generated audit, without modifying pack targets, release manifests or package production. Both consumer solution builds pass; existing CI retains `run-consumer-validation: true`. No broken package metadata was demonstrated. The unperformed local pack probe is not represented as passed. |
| CE10 / blind | Published-package administrator HTTP behavior has no focused recorded validation. | low | patch | Build and Server unit results did not record the real HTTP/projected-administrator boundary. The parent ran `DomainServiceEndpointsTests` in Release with `UseNuGetDeps=true` and a minimum of two tests. Both passed with zero failures/skips; the command, separate counts and raw-log digest are now in final evidence. |

These four surviving findings have separate causes and were patched individually by re-engaging the original implementation agent. This focused review adds no deferred-work entry or loopback. The concurrent review's eight pre-existing deferrals remain preserved, along with the existing FrontComposer package-only UI limitation.

## Completed Parent Verification (2026-10-08)

This supplement supersedes all earlier pending acceptance statements. Both specified Release solution restore/build gates pass with zero warnings and errors; the independently recorded standalone build also passes with FrontComposer forced to packages. The six required per-project lanes pass **5,225 tests**, and the supplemental package-mode administrator HTTP lane passes **two additional tests**, with no failures or skips in those passing lanes. Its host tests exercise signed projection/query requests and administrator verification against the projected read model, including rejection of a forged internal call.

Current audit validation, central catalog validation, SDK/tool exception validation, declared-gitlink validation and root/Builds whitespace checks pass. All 13 EventStore selections remain the externally committed `3.117.0` baseline; the generated audit's exact disposition is `retained`, with registry candidate `3.117.1` recorded separately. Tenants consumption is verified; this completion does not assert a new Builds-wide candidate compatibility or owner approval decision.

Evidence review verified every raw-log digest and the accepted catalog audit hash, then checked the metadata-only review corrections without repeating application builds. The durable raw-log archive is linked from `dependency-refresh-eventstore-3.117.0-final-evidence-2026-10-08.json`. The final evidence records the existing failed forced-FrontComposer-package UI lane separately; the repository's documented default UI source exception passes.

The spec is complete. No staging, commit, push, product migration or nested-submodule initialization was performed. The user's frozen intent explicitly prohibits staging and commits, so it takes precedence over the workflow's default local-commit step.
