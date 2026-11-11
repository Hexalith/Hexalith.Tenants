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

## File List

- `.github/workflows/ci.yml`
- `CONTRIBUTING.md`
- `Directory.Build.props`
- `Directory.Build.targets`
- `README.md`
- `_bmad-output/implementation-artifacts/dependency-refresh-2026-11-11-verification.json`
- `_bmad-output/implementation-artifacts/spec-dependency-refresh-release-2026-11-11.md`
- `docs/development-route-catalog.md`
- `docs/guides/canonical-scope-upgrade.md`
- `docs/quickstart.md`
- `docs/tenants-ui-deferred-at-handoff.md`
- `global.json`
- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.McpCli`
- `references/Hexalith.Memories`
- `references/Hexalith.Platform`
- `scripts/verify-catalog-package-consumer.py`
- `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj`
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `tests/scripts/test_bootstrap_compatibility.py`
- `tests/scripts/test_catalog_artifact_guards.py`
- `tests/scripts/test_catalog_package_consumer.py`

## Completion Notes List

All declared root dependency pointers are intentional parts of the requested synchronization. Builds supplies the refreshed SDK/package catalog, SDK11-compatible tooling and validated package audit. The other pointers synchronize remotely available domain, UI, host and publication updates. Dependency AppHost SDKs retain the13.6.4 selected by their own recorded catalogs; Tenants uses13.6.5 from its updated root catalog. Updating these independent owners by guessing sibling-catalog compatibility caused NU1109 in standalone CI and was corrected without updating nested submodules or adding disallowed consumer catalog overrides.

| Path | Baseline | Target |
| --- | --- | --- |
| `references/Hexalith.Builds` | `891886ddc60a9924d053bb74bf6bcc73e101b2ce` | `e35dd39872baf071aec6a7be1b252b16f460e08c` |
| `references/Hexalith.EventStore` | `b882c1005050aaebfa70618b5a72c1c8be452281` | `0c7b14856f2b730749caa8a552742a77d4882dc7` |
| `references/Hexalith.FrontComposer` | `7ace06c5c50406b9021fc95d8d87cf962b5180dd` | `57aa756c02edbae4294412885823533308b7db0d` |
| `references/Hexalith.McpCli` | `051fadedf68335ba5c525cf26c42f86aa69697f9` | `94f5ee691c70dcb30e893c50629d95afb63d216f` |
| `references/Hexalith.Memories` | `43d2d72f7170a29a58b6046f69e05c9ad9734701` | `fe27727d0ee43892c55818acb1cfb435370b4005` |
| `references/Hexalith.Platform` | `5adb00b2f30d2985ee0b1fb1abd713b10660dc48` | `b4b4eda9120e89a52afe6576f46bb7706860f3d1` |

Final shared CI repair: the canonical Builds owner revision supplies SDK10.0.404 alongside SDK11 for isolated net10.0.15 targeting/apphost packs and binds the packaged probe to DiagnosticSource11 required by OpenTelemetry1.21. All13 formerly failing fixture cases,518 workflow assertions, real packaged loader/subscription probes and31-project consumer authority checks passed locally. Equivalent repairs landed upstream during validation; their canonical workflow and direct DiagnosticSource implementation were retained, and its real packaged probe was rebuilt and passed. The final revision-bound catalog audit validates318 packages/158 families. The full Tenants Release solution build is repeated against this final dependency. Independent nested catalog adoption remains pending the explicit exception requested from the user; no nested gitlinks or working trees have been changed.

Hosted Ubuntu DNS recovery: the final runtime revision's ordinary checks passed, but all browser prerequisite jobs failed because the hosted resolver could not resolve Canonical mirror names. An isolated control run43041608789 reproduced this on Ubuntu24 and26; a priority-zero kernel.org mirror, Ubuntu-keyring verification and actual font installation passed on both. The immutable Builds CI hotfix f5a46e8a70a1893bc638fa5f488d57767fafdede backports only one prerequisite step to the previously qualified1cc workflow and adds the shared action/script plus8 passing tests. Tenants uses that exact action before its four matrix jobs and that exact workflow for its generic smoke lane. Source Builds dependencies remain at e35dd39872baf071aec6a7be1b252b16f460e08c. No signature, browser, application or coverage checks were disabled.

The first mirror-recovery CI run passed all four live browser qualifications. Two Contracts governance assertions still pinned the previous workflow SHA. Their approved immutable workflow SHA now matches the recovery backport, and the approved action inventory includes the mirror initializer. Both focused workflow governance tests pass after a warning-free Release build.

Additional owner-local CI repairs: FrontComposer records its existing private-fixture analyzer pragma in the factual ledger (governance and identifier seal tests plus31 consumer projection cases pass; the current configuration already matches its sealed digest). McpCli independently validates its approved catalog gitlink and workflow executor (4 tests and11 negative drift cases pass). EventStore fixes its stale4.47.0 tool assertion and four current scanner findings with276 scanner regressions plus focused contract and redaction cases passing. The initial nine immutable archived synthetic-fixture findings were subsequently corrected with exact path, line and full-snapshot hashes. All286 security tests, including the tracked scan, pass; archived bytes and detection of modified, moved or adjacent credentials remain intact. McpCli's older nested catalog vulnerability/census and Platform's frozen qualification lineage remain separate owner blockers. No nested gitlinks were changed.

Final owner follow-through: EventStore archive fixture recognition passed286 complete security tests and independent hash/byte review. McpCli's bounded SDK-import and shell-context parser fixes pass145 census fixtures. Its full local scan is blocked by unavailable nested amendment fixture Git objects; exact-source CI remains authoritative. True new source scopes still require specific owner approvals under the census contract; no approval or provenance records were fabricated.

FrontComposer's exact-source Quality run reproduced canonical mirror DNS failures before Chromium installation. The same validated immutable APT initializer now precedes each of its three existing HTTPS preparation steps. All original steps are retained; actionlint, two existing browser governance facts, immutable pin/order checks and the unchanged consumer seal validate.

FrontComposer platform correction: its accessibility job runs on Windows, so all three Ubuntu initializer calls now have an explicit Linux condition. Actionlint and parsed YAML checks prove the condition and preserve initializer/HTTPS/browser ordering; Windows retains its original preparation flow.

The complete Tenants CI, Story Guards, source-reference, CodeQL and Commitlint gates passed on aa1653a45eaf2163c57278923543dc68f652f7b3 (CI run43053489644), including all four live suites and the671+ Aspire tier. Final owner hygiene changes follow that qualification: EventStore renames only a runtime-generated probe variable in its now-tracked helper; its tracked-content scan and9 archive cases pass10/10 after a clean Release build. FrontComposer corrects one unsupported action status to the explicitly required open state; all222 validator tests pass with2 existing skips, and the prior exact-source browser/accessibility/Epic9 jobs are green. The synchronized final source must pass its own exact-source gates before release.

The synchronized c9f93e817c645dee8628c7ca96ab92f81511b792 source passed all five exact-source workflows, including complete CI43058015302. Its unpublished release attempt43063000754 was cancelled before protected approval when EventStore's own integration step exposed a toolchain prerequisite mismatch: frozen executor b470 selected Dapr CLI1.18.2/runtime1.18.7, while the module requires1.19.1/1.19.1. EventStore now pins existing immutable Builds fb027a87afcdf7661c8f80ade89231b7730b60d1, with Aspire13.6.4 and the required Dapr tuple plus archive hashes. Actionlint, a warning-free Contracts Release build and3 focused governance facts pass. The supported workflow contract has no Dapr override fields, and no provenance or qualification records were changed. The updated source must pass its own exact-source gates before publication.
