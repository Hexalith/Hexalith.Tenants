---
title: 'Synchronize dependencies and prepare the Tenants release'
type: 'chore'
created: '2026-11-02'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 608e91460c9cbf0770672b13634af334bc885972
context:
  - /home/administrator/projects/hexalith/tenants/AGENTS.md
  - /home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md
  - /home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-git-instructions.md
---

<frozen-after-approval reason="user-authorized release preparation; user explicitly requested no human validation checkpoints">

## Intent

Prepare and push a synchronized Tenants workspace using the latest stable .NET SDK and compatible package versions, repair actual CI failures, and release through the existing publication workflow when its enforced prerequisites pass. The user's instruction authorizes dependency changes, commits, pushes and release dispatch without additional planning or implementation approval.

## Boundaries & Constraints

Use the owning repositories and preserve unrelated work. Synchronize only the nine root-declared submodules; never initialize their nested submodules, merge unrelated feature branches or rewrite history. Commit and push any Builds changes before updating its parent gitlink. The parent agent owns all commits, pushes, remote CI monitoring and release dispatch after implementation review.

Keep all enforced CI, provenance, package-cohort and production environment protection gates. Do not approve on behalf of a required human reviewer, remove environment rules, weaken tests, use source references to satisfy package CI, or manufacture package versions. Do not downgrade the canonical EventStore 4.0.0 / McpCli 2.0.0 cohort to the currently published older API. Record exact publication blockers if the prerequisites remain unavailable.

Latest means stable and compatible: SDK 10.0.403 is already current, as are npm direct dependencies and the performance harness Node 24 type line. Keep Microsoft.OpenApi 2.13.0 and Microsoft.Build/Framework 18.9.6 because the newer major/target frameworks are incompatible. Preserve intentionally accepted preview dependencies.

</frozen-after-approval>

## Code Map

- Root `.gitmodules` declares EventStore, Commons, AI.Tools, FrontComposer, Builds, PolymorphicSerializations, Memories, McpCli and Platform. All started clean on main. Fetch completed without recursion; only Builds, FrontComposer, Memories and Platform had remote main advances. Synchronize with clean fast-forward merges after examining their owning guidance and histories.
- `references/Hexalith.Builds/Props/Directory.Packages.props` owns central versions. Live NuGet audit `/tmp/tenants-latest-nuget-audit.json` found compatible updates AngleSharp 1.9.0 to 1.9.1, StackExchange.Redis 3.4.29 to 3.4.31, Radzen.Blazor 12.1.9 to 12.1.10. Inspect owning `Tools/README.md`, package audit metadata and validator before editing; preserve CRLF/BOM. Do not load skills below references.
- `tests/scripts/test_catalog_artifact_guards.py` copies current Tenants sources then builds them against old EventStore 3.128.4. The canonical Contracts cohort guard now correctly rejects that setup, so CI run 41477454046 fails before exercising pack refusal. Repair the test fixtures to exercise real stale DLL pack refusal with the current canonical boundary intact. Retain legacy and stale-byte negative assertions, foreign-project positive control, and add/retain a successful qualified control where practical. Never relax `Directory.Build.targets` to make obsolete current sources compile.
- `Directory.Build.props` pins the new major cohort; `Directory.Build.targets` enforces it and verifies real packed DLLs via `tools/CatalogArtifactVerifier`. Preserve these production guards.
- `docs/guides/canonical-scope-upgrade.md` requires producers first. Live NuGet currently has EventStore 3.133.0 and McpCli 1.1.0, with requested next major versions returning 404.
- `.github/workflows/release.yml` dispatches from exact green main, prepares packages, then publishes five manifest packages in protected production. `tools/release-packages.json` is authoritative. Production currently requires reviewer jpiquot.

## Tasks & Acceptance

**Execution:**
- [x] Fast-forward the four advanced root submodule checkouts; inspect the remaining five and preserve them when current. Record full identities.
- [x] Update the three compatible central packages in Builds, with any required associated audit metadata and owning narrow validation. Inspect SDK pins across root submodules; update only if older than 10.0.403.
- [x] Repair the stale catalog packaging fixture without weakening production gates or dropping negative coverage. Run its test and relevant adjacent script tests.
- [x] Install the pinned npm dependencies as needed and validate the available builds/tests. Report the exact command and result for unpublished-package or environment blockers; distinguish local source checks from CI package evidence.
- [x] Repair actual canonical-scope fixture and documentation failures in Server tests while preserving authorization, transport, timeout and publication guards; rebuild and run the complete Server test assembly.
- [x] Repair additional actual UI gateway/composition scope fixture failures and run the complete aligned Debug/source UI test assembly.
- [x] Record changed files, completed checks and unresolved blockers in Implementation Notes. Leave commits and publication to the parent agent after review.

**Acceptance Criteria:**
- Given clean root submodules with remote main advances, when synchronization finishes, then their main checkouts include the inspected remote commits without nested initialization.
- Given the latest registry metadata, when updating dependencies, then only newer compatible versions are selected and compatibility holds remain explained.
- Given stale pack-target binaries or legacy package selection, when ordinary packing runs, then it refuses publication with the intended diagnostic and emits no archive.
- Given unavailable producer packages or required human production approval, when validating release readiness, then the blocker is recorded accurately and no gate is bypassed.

## Implementation Notes

Initial repository HEAD and nine root submodules were clean. The user explicitly requested execution without human validation, so the optional skill planning checkpoint is waived under the user's instruction; enforced GitHub protection remains binding. Microsoft release metadata and live npm/NuGet registries were queried.

### Implementation evidence (2026-11-02)

Loaded the complete spec and all three frontmatter context files before implementation. Fast-forwarded only Builds, FrontComposer, Memories and Platform with `git -c submodule.recurse=false merge --ff-only origin/main`; preserved the other five current checkouts. All nine root checkouts have zero initialized nested-submodule URL entries. All eight .NET root-submodule SDK pins and the Tenants SDK already select 10.0.403; AI.Tools has no SDK pin.

| Root submodule | Original parent gitlink | Checkout identity at implementation handoff |
| --- | --- | --- |
| Hexalith.EventStore | `ff5a38f5b80d66bafbac9ddc2388510534fb0ddd` | `ff5a38f5b80d66bafbac9ddc2388510534fb0ddd` |
| Hexalith.Commons | `11555ace9efcae2928a00122dcba91243e5a8a1a` | `11555ace9efcae2928a00122dcba91243e5a8a1a` |
| Hexalith.AI.Tools | `1745b6663ad32bb1288167135ed0f979366c53a0` | `1745b6663ad32bb1288167135ed0f979366c53a0` |
| Hexalith.FrontComposer | `d1236f9e5f21c0a000e1081846a2ea6308a35fd3` | `73ff2f87e430d983661c42674c0e433b4b76045f` |
| Hexalith.Builds | `728945314c3b7adf300df1964d5668047cfc338a` | `0112662625b9f87f56bec51da408adb2246f8f86` |
| Hexalith.PolymorphicSerializations | `bce4eb9f45c6b131b960ab4fa39e50ff01e51960` | `bce4eb9f45c6b131b960ab4fa39e50ff01e51960` |
| Hexalith.Memories | `9c0fac6ec0227b318662dcb9bb3217c5de995c66` | `66822bc5bc4858c824f17b7d093f5f0f676b03ea` |
| Hexalith.McpCli | `06cfdf25a352034b36041e9ed44da514b167ca5a` | `06cfdf25a352034b36041e9ed44da514b167ca5a` |
| Hexalith.Platform | `52e361d4a41b681b06f5e885f9f9857d8b08f5b2` | `38de4c15aa5990d169cd20be34e37ef6cc823491` |

Builds checkout identities may advance further through the parent's authorized catalog/audit commits before the parent records its final gitlink. Its inspected remote-main synchronization identity was `5df75d54ad1cab5e3c2e7357c59b6f438f0bc0e8`.

Changed `references/Hexalith.Builds/Props/Directory.Packages.props`: AngleSharp 1.9.0 → 1.9.1, StackExchange.Redis 3.4.29 → 3.4.31, Radzen.Blazor 12.1.9 → 12.1.10. Preserved the UTF-8 BOM and all 379 CRLF lines. `pwsh -NoProfile -File Tools/validate-central-package-versions.ps1` passed all 314 entries before the parent's catalog commit. The required revision-bound audit cannot be regenerated from dirty catalog declarations: preserved its original bytes at `/tmp/tenants-builds-package-version-audit-before-gbsmpqcf.json`, then handed source commits, audit generation/validation, and pushes to the parent. Microsoft.OpenApi 2.13.0, Microsoft.Build/Framework 18.9.6, intentional previews and current npm/Node type selections remain unchanged.

Changed `tests/scripts/test_catalog_artifact_guards.py` to compile isolated test-only assemblies under unchanged copied production props/targets. Two explicit canonical producer guards, all five legacy pack refusals, all five successful qualified fixture packs with exact packed DLL-byte comparison, six stale-DLL replacement refusals (own and dependency outputs), and the foreign-project positive control now pass. Synthetic external attributes are confined to the disposable test fixture; they do not manufacture producer packages or establish real SDK release qualification. Production `Directory.Build.props`, `Directory.Build.targets`, and `tools/CatalogArtifactVerifier` remain unchanged.

Additional actual Server test failures were stale fixtures from the canonical-scope transition. Updated `TenantBootstrapReadinessBudgetTests`, `TenantBootstrapHostedServiceTests`, and `TenantBootstrapCommandTransportTests` to use the declared global-administrator platform scope, required bootstrap permission, and retained delegated Bearer. The transport test now validates the signature, subject, administrator hint, bootstrap permission, absence of legacy tenant claims, scope header, bound proof, one-attempt behavior, receipts, cancellation and deadlines. Updated `TenantClaimContractTests` to preserve fail-closed coverage for missing grants and subjects, malformed and duplicate canonical grants, reserved public `system`, and administrator hints without tenant authority. Added the new global-administrator audit query to `EventPublicationConfigurationTests` inventories and `EventContractReferenceDocumentationTests`' public query count. Updated `docs/event-contract-reference.md` with the seventh public query, its route/filters, current public scopes and retained storage identities. `docs/production-auth-claim-contract.md` now leads with authoritative 7.x guidance and labels/nests the retained older material as 6.x compatibility reference. No production authorization or transport guard was relaxed.

Successful checks:

- `npm ci --ignore-scripts` from Tenants root: 318 packages installed, 319 audited, zero vulnerabilities.
- `python3 tests/scripts/test_catalog_artifact_guards.py`: 2 test methods passed, including the controls above.
- `python3 -m unittest discover -s tests/scripts -p 'test_catalog_package_consumer.py' -v`: 7 passed.
- `python3 -m unittest discover -s tests/scripts -p 'test_hosted_package_profile.py' -v`: 2 passed.
- `python3 -m unittest discover -s tests/scripts -p 'test_nuget_dependency_ranges.py' -v`: 12 passed.
- `python3 -m unittest discover -s tests/scripts -p 'test_release_artifact_verification.py' -v`: 21 passed.
- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false -p:HexalithMemoriesFromSource=false -warnaserror -m:1 -nr:false`: succeeded with zero warnings/errors. This consumed pre-existing local candidate packages and is local fixture evidence only.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests.dll`: 1,802 passed, zero failures/errors/skips; log `/tmp/tenants-release-server-suite-repaired.log` (13.067 seconds). Includes all affected classes and retained authorization/transport negatives.
- After the final auth-guide edit, `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests.dll -class 'Hexalith.Tenants.Server.Tests.Documentation.*'`: all 50 documentation tests passed with zero skips.
- `git diff --check`: passed.

Publication blocker, independently reproduced by the parent with a fresh cache: `dotnet restore /tmp/tenants-published-cohort-ydzzw8aw/PublishedCohort.csproj --source https://api.nuget.org/v3/index.json --packages /tmp/tenants-published-cohort-ydzzw8aw/packages --no-http-cache --verbosity minimal` exited 1 with NU1102 for Hexalith.EventStore.Contracts >= 4.0.0 (nearest 3.133.0) and Hexalith.McpCli.Abstractions >= 2.0.0 (nearest 1.1.0); exact log `/tmp/tenants-published-cohort-ydzzw8aw/restore.log`. Local cached-candidate success is not published-package evidence. Current main CI also reports the missing major cohort. Producers must publish first; do not downgrade or fabricate the dependencies. Production publication additionally retains required human reviewer jpiquot. The parent owns exact-head CI monitoring, all commits/pushes, final Builds audit checks, and any release dispatch; no protected environment approval was performed.

### Parent validation and publication observations

- Generated revision-bound central audit from committed catalog `0112662625b9f87f56bec51da408adb2246f8f86`. Actual audit validation passed 314 packages / 154 families; generator fixture suite passed 115 scenarios and validator fixture suite passed 103 scenarios. Consumer authority passed 23 projects, exception inventory passed 16 entries, and Builds.Tooling Debug build passed with zero warnings/errors.
- Release workflow governance tests passed 6 cases. Registry-only, isolated-cache restore reproduces missing producer majors independently of the pre-existing local candidate cache. Current-main CI run 41488878555 failed its build-and-test restore on the same NU1102 errors.
- Downloaded all five Tenants NuGet artifacts: latest stable is 6.0.0 for Contracts, Client, Server, Testing and Aspire; every nuspec reports source `9e5a991d0ba304bac7b1a5809cd71797b47d78ed`, matching release v6.0.0 and run 41219537483. No new package publication has occurred.
- Read-only semantic release proposal resolves 7.0.0. Required release order remains EventStore 4.0.0, McpCli bootstrap Abstractions 2.0.0, Tenants 7.0.0, then McpCli paired release. Production environment requires reviewer jpiquot; no approval bypass was attempted.
- Retried only FrontComposer Quality run 41482793808's failed job through the GitHub connector. Its initial failure was a Dapr placement-port collision before test startup; rerun status remains observable in GitHub.
- Additional UI source validation initially compiled mixed producer identities and failed runtime assembly resolution. Repeated build using the exact producer Version selected by source-reference.yml (3.133.0) rather than treating the default future package pin as the source identity; aligned build passed with zero warnings/errors. It exposed 271 old scope assertions in TenantCommandGatewayTests and TenantsUiCompositionTests. Corrected only submitted public scope expectations and the generated API Platform declaration, retaining internal system receipt/storage fixtures. Repaired UI source suite passed 8,079 tests with zero failures/errors/skips (112.973 seconds); logs `/tmp/tenants-ui-source-debug-repaired-build-20261102.log` and `/tmp/tenants-ui-source-debug-repaired-tests-20261102.log`.

- Final Builds audit commit: `8f3ce34436299d4682ff50dc3fe18360a09a88e1`; committed audit validation passed all 314 packages and 154 families again.

### Final integration and review result

- Builds changes and revision-bound audit were pushed to origin/main at `8f3ce34436299d4682ff50dc3fe18360a09a88e1` before the parent gitlink update. Final Platform fetch also advanced its clean checkout to `c635a2dc22f404d97c9fe7e27f7e7f4c9017943b`; inspected changes qualify its catalog consumers on published EventStore 3.133.0.
- During final verification, origin/main advanced to `93a5aa8385cc65c73b850738643d7f8204ee52da`, adding the reviewed native authority invocation fence. Fast-forwarded without disturbing these changes. A cached-candidate Release rebuild then correctly failed CS0246 for RouteCatalogInvocationScope because that old local candidate lacks the newly integrated API. The actual source build is the authoritative local fallback, not a replacement for package CI.
- Final command: `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug -warnaserror -m:1 -nr:false -p:UseHexalithProjectReferences=true -p:HexalithEventStoreVersion=3.133.0` passed with zero warnings/errors. `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll` passed all 1,810 cases with zero errors/failures/skips (12.830 seconds). The producer version is selected from the source project as required by source-reference.yml, not from the future NuGet pin.
- Reviewed and repaired the remaining canonical membership positive/case variants and current IdP/query examples. Initial independent edge-case and verification-gap reviews found no defects. Blind review findings are individually triaged below; small fixes were verified, and one pre-existing, unverified no-build cohort follow-up was appended to the deferred ledger. No human approval gate was bypassed.
- FrontComposer Quality run 41482793808 attempt 2 completed successfully after retrying only the failed infrastructure job. All 8,079 final UI cases and all 1,810 final Server cases pass locally. Full package CI and a new 7.0.0 publication remain blocked by unavailable upstream producer packages and required production reviewer approval. Preparation is complete; publication is not complete.

## Spec Change Log

- 2026-11-02: Added observed canonical-scope Server fixture/documentation failures to the existing CI-repair task after focused and complete Server runs exposed them; retained frozen boundaries and all production gates.

## Review Triage Log

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind1: formerly qualified EventStore no-build packs | maybe-false | Synthetic non-Contracts shells can pack with 3.133.0 because the existing canonical API guard belongs to Contracts. The real current package graph and its transitive 4.0 requirement cannot be established from shells while producers are unpublished. Defer as medium, unverified, pending real-package no-build probes; no production guard was changed. |
| Blind2: older McpCli no-build packs | maybe-false | Same pre-existing Contracts-only guard and synthetic-shell limitation for 1.1.0. Group with Blind1; verify real dependency nuspecs and normal publication preflight before claiming a consumer defect. |
| Blind3: lost real-project qualification | false | Existing published-catalog-http CI builds actual IntegrationTests and Contracts projects; build-release-candidate.sh packs all five actual manifest projects and validates consumer references. The synthetic test is explicitly only a guard fixture, and those real qualification lanes remain. |
| Blind4: more enrollment mutation cases | false | Unchanged RequireSource rejects wrong exact enrollment and duplicate source metadata, and the verifier checks public static Operations. The review demonstrates no bad behavior introduced by this fixture repair; independent verification review found no missing failure-mode requirement. Additional mutation cases are a coverage suggestion, not a verified defect. |
| Blind5: canonical positive membership coverage | medium | The rewritten authorization cases otherwise all deny; add successful canonical membership and casing examples so this class detects an always-deny regression. Patch. |
| Blind6: reserved tenant case variants | low | Add System and SYSTEM directly to the existing reserved-system theory; trivial extra canonicalization negatives. Patch. |
| Blind7: name-only fixture also lacks membership | low | The test explicitly proves transformation does not mint NameIdentifier and separately denies absent tenant membership. A matching-grant trusted-subject authorization proof belongs to the actual subject-validation boundary, rather than ClaimsTenantValidator, which validates membership. Pre-existing test limitation; reject a broader boundary-fixture expansion as disproportionate to this cosmetic coverage attribution. |
| Blind8: retained legacy system grants | medium | Canonical grant validation rejects reserved values in the complete collection. Current upgrade prose should explicitly tell operators to remove old system grants. Patch documentation. |
| Blind9: current request examples | low | Legacy examples are clearly labeled, but a small current claims/query example removes avoidable operator reconstruction. Patch documentation from existing routes/client. |
| Blind10: Redis runtime acceptance evidence | false | Generated family dispositions explicitly remain retained; the work records metadata checks and a compile result, not live Redis acceptance or production readiness. Missing upstream packages and production approval remain release blockers. No runtime compatibility acceptance is asserted. |

Edge-case reviewer returned no findings. Verification-gap reviewer returned no findings, including a separate review of the later two UI fixture corrections.

## File List

- `references/Hexalith.McpCli`

- `references/Hexalith.EventStore`

- `references/Hexalith.Builds`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Memories`
- `references/Hexalith.Platform`

## Completion Notes List

These root dependency pointer changes implement the user's explicit synchronization request. Builds includes the compatible package upgrades and generated audit; FrontComposer includes its reviewed accessibility evidence; Memories includes its current release workflow fixes; Platform includes current scoped catalog consumer qualification. EventStore advanced during the final push with canonical platform-scope validation fixes; the remaining four root pointers were already current. No nested submodule was initialized.

| Path | Baseline | Target |
| --- | --- | --- |
| `references/Hexalith.EventStore` | `ff5a38f5b80d66bafbac9ddc2388510534fb0ddd` | `853721d77b9f15fc2963ef0b9a019d4a2cf22f1e` |
| `references/Hexalith.Builds` | `728945314c3b7adf300df1964d5668047cfc338a` | `8f3ce34436299d4682ff50dc3fe18360a09a88e1` |
| `references/Hexalith.FrontComposer` | `d1236f9e5f21c0a000e1081846a2ea6308a35fd3` | `73ff2f87e430d983661c42674c0e433b4b76045f` |
| `references/Hexalith.Memories` | `9c0fac6ec0227b318662dcb9bb3217c5de995c66` | `66822bc5bc4858c824f17b7d093f5f0f676b03ea` |
| `references/Hexalith.Platform` | `52e361d4a41b681b06f5e885f9f9857d8b08f5b2` | `c635a2dc22f404d97c9fe7e27f7e7f4c9017943b` |
| `references/Hexalith.EventStore` | `853721d77b9f15fc2963ef0b9a019d4a2cf22f1e` | `653124dced81830e932edb7738a78ee61104d55a` |

The first exact push-range gitlink check exposed missing declarations in this new spec. These declarations and exact targets repair that evidence omission; the baseline and production guard remain unchanged.

Final synchronization also records EventStore `853721d77b9f15fc2963ef0b9a019d4a2cf22f1e`, already pushed by concurrent upstream work. It adds canonical platform scope acceptance to the four API request validators and their focused regressions. This inspected upstream commit changes no library source edge used by the final Tenants unit suites. EventStore run41488610131 passed on its preceding main; the new commit has its own pending CI. FrontComposer retry remains green. No unfinished upstream edits were staged by this task.

Story 2.14 subsequently advanced the pinned EventStore source to `653124dced81830e932edb7738a78ee61104d55a` for authorized platform-scope retained-history reads. The final EventStore table row records that later pointer without changing the earlier synchronization receipt.

**All-branch merge integration**

The user's subsequent instruction to merge all branches authorizes the outstanding branch integrations. McpCli package-vector receipt branch is merged at `dea5ff0a17bd5e2a7211e3b44fb4b8bae118ad74`; its Abstractions test build and all 20 tests passed. Tenants Dapr readback branch is merged at `861060aa9b5b8ec3dae425b669c178cb6526d1dd`, retaining the concurrent bootstrap-scope fix. No root submodule's nested submodules were initialized. EventStore 4.0.0 and McpCli.Abstractions 2.0.0 were downloaded successfully from nuget.org; no candidate packages were published by this task.

This final integration snapshot supersedes earlier binding target rows above. Historical synchronization receipts retain their original identities.

| Path | Baseline | Target |
| --- | --- | --- |
| `references/Hexalith.Builds` | `728945314c3b7adf300df1964d5668047cfc338a` | `3b192bafd3093e83d789e533dffc39d24e3524a7` |
| `references/Hexalith.EventStore` | `ff5a38f5b80d66bafbac9ddc2388510534fb0ddd` | `653124dced81830e932edb7738a78ee61104d55a` |
| `references/Hexalith.FrontComposer` | `d1236f9e5f21c0a000e1081846a2ea6308a35fd3` | `b547cd80df1dbdb73f6e3538812db38e156f304e` |
| `references/Hexalith.Memories` | `9c0fac6ec0227b318662dcb9bb3217c5de995c66` | `d9d61d8a95c7dc8827acf68ae723859711703729` |
| `references/Hexalith.McpCli` | `06cfdf25a352034b36041e9ed44da514b167ca5a` | `dea5ff0a17bd5e2a7211e3b44fb4b8bae118ad74` |
| `references/Hexalith.Platform` | `52e361d4a41b681b06f5e885f9f9857d8b08f5b2` | `c869f1c7b6b86bd693911ad10e328235ea697da8` |

## Verification

- `python3 tests/scripts/test_catalog_artifact_guards.py` must pass all cases after the fixture repair.
- Run related package/profile script tests and the owning Builds package audit validator for central updates.
- Attempt the CI-equivalent focused package build and record exact unresolved restore failures.
- Parent: validate each exact commit message with the owning pinned commitlint; push Builds first, then root; inspect exact-head CI and dispatch only when gates permit.
