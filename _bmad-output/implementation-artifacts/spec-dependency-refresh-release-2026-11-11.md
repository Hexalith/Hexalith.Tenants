---
title: 'Refresh dependencies and publish a verified Tenants release'
type: 'chore'
created: '2026-11-11'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: cca5537c3a9d6dde162787053b34e17c8351b21d
context: []
---

<frozen-after-approval reason="User explicitly authorized updates, push, release, and bypass of human validation">

## Intent

Publish an updated, tested Hexalith.Tenants release using the latest stable .NET SDK and compatible package versions, after synchronizing the nine root-declared submodules and repository. The user explicitly authorizes committing, pushing, releasing, and handling the production approval without another human checkpoint.

## Boundaries & Constraints

Use SDK 11.0.100, released 2026-11-10, while preserving the existing net10.0 consumer target. Select current compatible stable packages from authoritative NuGet and npm feeds. Preserve documented compatibility pins, security pins, explicit previews without a stable equivalent, package/source separation, existing runtime contracts, and all CI/release checks. Keep the governed Builds execution SHA distinct from the source dependency gitlink. Never initialize nested submodules, weaken gates, or alter published tags. Shared package versions belong to Hexalith.Builds. Preserve BOM and CRLF in its central catalog. Commit messages require exact pinned commitlint validation before use. Submodule content commits must reach their remote before the root records their gitlinks. Do not modify application behavior except to repair verified compatibility or CI failures caused by this refresh.

</frozen-after-approval>

## Code Map

- `global.json`: root SDK 10.0.403 and Microsoft.Testing.Platform runner; pin SDK 11.0.100.
- `Directory.Build.props`: root package defaults override the imported Builds catalog. EventStore 4.1.0, McpCli 2.0.0, Memories 3.0.25 need current published stable versions (4.2.0, 2.0.1, 3.0.26). The frozen Testing.Integration 3.128.4 fixture override must remain.
- `references/Hexalith.Builds/Props/Directory.Packages.props`: authoritative shared versions. Update consumed dependencies and related version cohorts to latest compatible stable releases. Relevant candidates: Aspire stable 13.6.5, Dapr 1.19.1, .NET 10 servicing 10.0.15, bunit 2.15.2, Test SDK 18.11.1; verify against feeds before editing. Read ordinary Builds guidance/configuration first; do not load its skills.
- `references/Hexalith.Builds/Tools/`: package validators and platform catalogs; maintain any corresponding required catalog inventories when their authorities change.
- `tests/performance/tenant-audit/package.json` and lockfile: browser tests, TypeScript, Node typings. Match typings to the Node runtime used by CI, and verify typecheck/mode tests.
- `package.json` and lockfile: release/commitlint dependencies currently latest; verify rather than changing unnecessarily.
- `.github/workflows/ci.yml`, `source-reference.yml`, `story-guards.yml`: latest successful exact-source push runs required for release. Existing CI on baseline is green.
- `.github/workflows/release.yml`: manual release, production environment, NuGet Trusted Publishing, five-package preparation and content verification, container publication, exact source proof. Preserve release implementation.
- `tools/release-packages.json`: authoritative Contracts, Client, Server, Testing, Aspire inventory.
- `.releaserc.json`: semantic-release needs a releasable validated dependency-fix commit for patch publication.
- Root-declared submodules were clean and contained no unpublished commits. Root and all nine remotes fetched without recursion. Five advanced by fast-forward to origin/main; others already current. Existing baseline root commit is recorded above.

## Tasks & Acceptance

**Execution:**
- [x] `global.json` -- install/use and pin latest stable SDK while retaining target compatibility.
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` -- refresh relevant compatible package cohorts and validate catalog authority and encoding.
- [x] `Directory.Build.props` -- refresh overriding Hexalith package versions without weakening qualification.
- [x] `tests/performance/tenant-audit/package.json` -- update compatible outdated tooling and lockfile if any; verify browser harness typecheck/mode tests.
- [x] Root and shared catalog -- run focused package restore/build, relevant individual test projects and script checks; fix discovered failures.




**Acceptance Criteria:**
- Given current authoritative stable releases, when the repository builds, then SDK 11.0.100 is selected and net10.0 consumers restore compatible updated dependencies.
- Given all declared submodule remotes, when root changes are pushed, then every recorded dependency commit is remotely reachable and no user changes or unpublished history are lost.
- Given the updated root commit, when the release is dispatched, then required automated checks pass, a new GitHub Release points to the intended source, and all five NuGet packages are publicly available with matching version and source evidence.

## Implementation Notes

The authoritative 316-package feed audit is available at `/tmp/tenants-dependency-audit-2026-11-11.json`. Read it as investigation input; scope updates to Tenants-consumed packages and corresponding cohorts. NuGet Playwright 1.64.1 is already latest (npm Playwright 1.65.0 is separate). Preserve AngleSharp 1.10.1 required by latest bunit. Additional consumed candidates include OpenTelemetry 1.21.0, NuGet.Versioning 8.0.0, SourceLink 11.0.100, and standalone SwaggerUI 11.0.0, all compatible with net10.0. Do not update full Swashbuckle to a version requiring Microsoft.OpenApi 3.x.

The workflow's human checkpoints are bypassed under the user's explicit instruction. Implementation agent handles file changes and focused local checks only; parent handles final repository commits, pushes, remote CI and release publication after inspecting the diff. Do not change submodule SDK pins unrelated to the root build merely to create content changes.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence and resolution |
| --- | --- | --- |
| Blind: freshness audit stale | medium | New catalog hash and Utilities.Core identity require a complete audit after the catalog content commit. Generation before commit intentionally refuses dirty authority; regenerate and validate before pushing. |
| Blind: SDK inventory/source drift | medium | Workspace inspection finds declared root AppHosts still13.6.4 while the catalog/inventory says13.6.5. Align their owner declarations and validate actual files. |
| Blind: dependency range syntax | medium | Splitting the raw range admits an exclusive lower bound and inverted interval. Patch validation and refusal tests. |
| Blind: restored McpCli differs | medium | The newly admitted cohort is only metadata-bound; bind the actual restored assets entry to the admitted version before build. |
| Blind: developer SDK docs stale | low | Quickstart's explicit10.0.403 cannot satisfy global11.0.100/latestPatch. Correct current prerequisites and retain net10 runtime instructions. |
| Blind: current dependency docs stale | low | Current-default prose still says4.1.0 while actual defaults now4.2.0/2.0.1. Correct current prose while retaining compatibility history. |
| Blind: premature remote-success claim | false | The cited lines are expected command outcomes under Verification, not reported executed results; remote tasks are unchecked. Explicitly record their pending status below until CI/publication execute. |
| Edge: exclusive McpCli lower bound | medium | Same demonstrated parsing defect; a future higher version could satisfy the exclusive interval. Patch the actual bound and restored-version checks. |
| Verification: live package lane stays old | high | Authenticated provisioning/build explicitly pin4.1.0/2.0.0 and the ordinary Aspire runner excludes those persisted-state classes. Update the existing live lane and tests to4.2.0/2.0.1. |

## Verification

- `dotnet --version` -- 11.0.100 at the root.
- Individual package-profile builds/tests and existing Python script tests -- pass without weakened expectations.
- Builds central-package validator -- valid evaluated catalog, preserved encoding and prerequisites.
- Browser harness `npm run typecheck` and `npm run test:mode` -- pass.
- Exact-source GitHub CI and release jobs -- successful.
- NuGet flat-container indexes and package manifests -- all five expected release versions.

Local implementation verification: SDK 11.0.100; Contracts and AppHost Release builds pass without warnings; AppHost 66, UI composition 96, version-selection 27, catalog metadata 9, source-profile 3, browser mode 4 tests pass. Central 317-entry, platform, Dapr and exception inventories validate. SDK11 MSBuild task probe reaches its expected HXW006 refusal. Shared Tooling uses private compatible MSBuild package references and C#14. The full package-only standalone UI profile is unsupported by the documented UI source exception; intended source composition passes. Freshness audit requires a committed catalog before generation and will be regenerated before push.

Remote verification is pending: exact-source CI, release dispatch/production approval, GitHub Release and five-package public NuGet verification will run after dependency content and root changes are committed and pushed.

Implementation and review are complete. Publication remains an authorized operational follow-through, verified independently by the existing exact-source workflows after this implementation commit is pushed.

- Root and declared submodules -- validate exact messages, commit owning content, push dependencies first, verify remote reachability, and commit/push synchronized gitlinks.
- Release workflow -- wait for green latest exact-source CI gates, dispatch release on main, satisfy production deployment approval under explicit user authorization, and repair actual release failures.
- NuGet publication -- verify all five new package versions and exact release source through workflow evidence and public package endpoints.

The Builds remote advanced during implementation with its own SDK11 refresh (see canonical recorded revisions in the companion JSON). Its owner changes supply SDK11, compatible MSBuild compile-only references, C#15-compatible source, current package selections and a complete317-entry audit. Integration preserved those changes; the local C#14 pin was discarded because the owning repository now pins SDK11. Only the remaining SwaggerUI11.0.0 update was applied on top, followed by a validated revision-bound audit. Final AppHost Release build and66 tests and intended UI Release build pass without warnings against that integrated revision. Review fixes pass11 catalog-consumer and14 bootstrap/workflow tests. Existing workspace inventory completeness errors and Platform's separate release-provenance refusal are independent of Tenants admission; no gates were weakened.
