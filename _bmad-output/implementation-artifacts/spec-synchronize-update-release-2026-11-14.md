---
title: 'Synchronize dependencies and publish the .NET 11 release'
type: 'chore'
created: '2026-11-14'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '401d926eb7d51bdf332696ed3f0fe39a812b3501'
context:
  - '/home/administrator/projects/hexalith/tenants/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="User explicitly authorizes pushing all changes, synchronization, dependency updates, CI repair, release and bypass of human validation">

## Intent

Publish the current Tenants work as a tested .NET 11 release after synchronizing the repository and all nine root-declared submodules. Include existing local changes, update to latest stable SDK/packages, repair actual CI failures, and verify all five release packages on NuGet. The user authorizes concrete Git and publication operations without additional human checkpoints.

## Boundaries & Constraints

Preserve existing work and historical evidence. Work in each owning repository; do not load skills under references or initialize/update nested submodules. Keep automated acceptance, security, source/provenance and package boundary checks active. Do not fabricate approvals, test results or publication evidence. Latest compatible stable packages are preferred; retain and explain tested compatibility pins and intentional previews. Shared package authorities belong to Builds. Governed execution identities must remain separately qualified from source catalog revisions. SDK 11.0.100 is already latest; verify it rather than introducing artificial changes. Publication through existing workflows must retain source and artifact checks. Production approval may be satisfied through GitHub's existing authenticated maintainer permissions under the explicit user instruction; do not weaken persistent protections. Submodule content must be pushed and remotely verified before recording root gitlinks. Exact commit messages require pinned commitlint validation. Preserve LF/CRLF and catalog encoding rules. Use task-specific temporary paths; disk is nearly full, so use /dev/shm for large temporary caches/artifacts.

</frozen-after-approval>

## Code Map

- Root global.json pins11.0.100; Directory.Build.props targets net11 and overrides EventStore4.2.0, McpCli2.0.1, Memories3.0.27, Tenants7.0.6 before the Builds catalog import. Evaluate actual latest packages and adjust coordinated cohorts and relevant fixtures only with corresponding checks.
- references/Hexalith.Builds/Props/Directory.Packages.props is the central package authority. Remote Builds main is39 commits newer and already green. The root imports this catalog; never add a consumer-owned full copy.
- references/Hexalith.FrontComposer has the existing uncommitted net11 migration and generated awaited ValidateAsync repair. Root package CI still restores a generator using obsolete EditContext.Validate(), causing CS0618. Its migration must be published before root package-only validation can succeed. Integrate remote changes without overwriting local fixes.
- references/Hexalith.Memories contains existing net11 migration; newer remote main already released4.0.0 and may overlap those changes. Preserve semantic changes and record integration.
- references/Hexalith.McpCli contains existing package compatibility fixes, analyzer host migration and reviewed evidence. Newer remote main adds story inventories; preserve both. Its specialized census approvals remain exact and must never be invented from a broad release request.
- Other root gitlinks: Commons, EventStore, PolymorphicSerializations, AI.Tools, Platform. Fetch without recursion, inspect divergence, integrate current remote main. Platform currently has separate CI failures to investigate.
- Root tests/Hexalith.Tenants.UI.Tests/Browser and tests/integration contain existing net11 execution paths/native-read timeout work. Preserve those changes.
- .github/workflows/ci.yml, source-reference.yml, story-guards.yml are required exact-source release gates. Current Story Guards also fails performance source-provenance tests; inspect actual job logs and repair current path/assertion mismatches without weakening guard semantics.
- .github/workflows/release.yml dispatches prepared artifacts then protected production publication; .releaserc.json owns semantic version calculation. tools/release-packages.json lists Contracts, Client, Server, Testing, Aspire.
- Initial diffs, full revisions and untracked hashes are captured in /dev/shm/tenants-release-2026-11-14/initial-inventory.json and sibling initial.patch files. Previous migration specs/evidence remain historical.

## Tasks & Acceptance

- [x] Root-declared repository Git histories -- integrate upstream and preserve local work; remotely verify content before root pointers.
- [x] SDK and package authorities -- verify latest feeds, update required compatible cohorts and lockfiles, document retained constraints.
- [x] Existing owner migrations and publication -- validate and deliver the FrontComposer generator required by package consumers.
- [x] Root package/source builds and failing guards -- repair observed compatibility/CI failures and run relevant individual suites.
- [x] Final diff -- independently review correctness, edge cases and verification gaps, apply bounded corrections.
- [ ] Root main -- validate commit message, push synchronized work, obtain successful latest exact-source required CI.
- [ ] Release workflow and NuGet -- publish intended source, verify GitHub Release plus all five NuGet package versions and retained provenance.

Acceptance:
- Given authoritative current metadata, when build/restore runs, then SDK11.0.100 is selected and updated compatible dependencies restore with warnings as errors.
- Given existing local edits and upstream changes, when synchronized commits are pushed, then no user work is lost and each recorded root submodule commit is remotely reachable.
- Given the intended final source, when automated CI and release run, then every required job succeeds and all five public NuGet archives report the release version and intended repository source.

## Implementation Notes

This is one coordinated release goal. The user's explicit request supersedes workflow human checkpoints and prior no-publication scopes for the requested delivery. It does not assert that outstanding technical checks or narrowly scoped census authorities passed. Parent owns Git integration, remote CI and publication. The implementation agent owns root repository source/configuration repairs and local verification only. Parent handles owning submodule source integration and repairs; no nested repository initialization or remote mutation by that agent. Preserve existing test coverage and avoid unrelated product changes.

## Spec Change Log

- Concurrent upstream integration on 2026-11-14: root `6fcff842a8e48fc843d7b2df942d369f99f92b6b` adds authenticated tenant-access recovery. A fresh public SDK5.0.1 build correctly fails because its new technical snapshot-budget APIs are absent. EventStore owner history selects planned5.1.0, which remains unqualified/unpublished until its exact-source gates and release succeed. The authorized synchronization therefore prepares an exact5.1.0 default/admission while preserving historical archive cohorts and the frozen3.128.4 pin. The current route inventory becomes24 routes/24 operations/13 adapters; earlier23/23/12 receipts remain historical. Concurrent Builds `0230aefd0502775b034a38c3b037c32c79f20fd7` is preserved below the local adoption commits; its exact25-project Immutable view and current default are independently tested. Concurrent FrontComposer source advances to `4c9b5778f72822656eb80d56babf3f2d1d11d3c0`; prior467 candidate receipts retain their original scope.

## File List

- `references/Hexalith.AI.Tools`
- `references/Hexalith.Builds`
- `references/Hexalith.Commons`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.McpCli`
- `references/Hexalith.Memories`
- `references/Hexalith.Platform`
- `references/Hexalith.PolymorphicSerializations`
- `Directory.Build.props`
- `Directory.Build.targets`
- `.github/workflows/ci.yml`
- `scripts/verify-catalog-package-consumer.py`
- `tools/catalog-package-consumer/Program.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAccessRecoveryTests.cs`
- `tests/scripts/test_catalog_artifact_guards.py`
- `tests/scripts/test_catalog_package_consumer.py`
- `tests/scripts/test_tenant_audit_source_provenance.py`
- `tests/scripts/test_bootstrap_compatibility.py`
- `tests/scripts/test_dapr_read_policy_evidence.py`
- `tests/integration/qualify-dapr-read-retries.py`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `package.json`
- `package-lock.json`
- `tests/performance/tenant-audit/package.json`
- `tests/performance/tenant-audit/package-lock.json`
- `README.md`
- `docs/development-route-catalog.md`
- `docs/guides/canonical-scope-upgrade.md`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/root-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/current-source-manifest.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/commit-message-validation.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/additional-source-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/mcpcli-hosted-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/candidate-package-ui-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/upstream-integration-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/recovery-source-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/public-sdk51-backend-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/eventstore-publication-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/frontcomposer-publication-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/public-frontend-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/builds-public-consumer-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/builds-initialize-action-equivalence.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/parent-sdk51-bounded-review.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/parent-public-frontend-proof-check.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/prepush-synchronization.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/rebase-source-equivalence.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/packed-consumer-repair-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/parent-root-catalog-repair-review.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/eventstore-postrelease-source-synchronization.json`

## Completion Notes List

- All nine root-declared dependency paths are included in this coordinated synchronization. Advance a pointer only after its owning content is integrated, pushed and remotely verified; unchanged pointers need no artificial update. Historical implementation specs retain their own source and pointer attribution.
- The initial reviewed package default is exactly EventStore 5.0.1 with McpCli 2.0.1; the later concurrent upstream recovery feature requires the prepared5.1.0 default recorded in the Spec Change Log. Retained 4.0.4, 4.1.0 and 4.2.0 package profiles and the nonpublishable 3.128.4 bootstrap harness remain explicit compatibility choices. Memories and Tenants family defaults now come from the shared Builds catalog. The finite catalog admission guards include only the newly qualified 5.0.1 cohort; prereleases, neighboring unqualified versions, malformed aliases and mixed producer cohorts remain refused. No governed runtime identity is inferred from a source catalog revision.
- The performance provenance fixture now admits the exact read-only AppHost target-framework query that its runner performs and still rejects build or npm execution before the controlled certificate failure. It records .NET 11 tool identities.
- Registry checks found two direct npm updates: semantic-release GitHub 12.0.11 and Playwright 1.65.1. The remaining direct dependencies are current; Node type definitions stay on the latest compatible 24.x line selected by CI's Node 24 runtime.
- Review corrections require inclusive qualified SDK dependency minimums and package-mode restoration at the exact admitted EventStore cohort, including public dependencies. Authenticated native/persisted/browser lanes now bind SDK 5.0.1 in both builds and producer metadata. Current dependency-source identity is captured separately from historical command evidence; no newer dependency snapshot is attributed retroactively to those commands. Dapr qualification retains the safe generated policy text needed to reproduce its loaded-policy hash. The earlier framework/browser deferral has an append-only source resolution while its independent package/governance obligations remain open.

## Review Triage Log

All three independent layers completed before triage. Each filed verification gap is accepted from its traced evidence. Findings B1–B10 are the blind layer; V1 is the verification layer; the edge layer returned no findings.

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| B1: exclusive SDK dependency minimum | medium | Patch: the SDK path strips delimiters and accepts `(5.0.1,5.0.2]`; reuse the existing full range normalizer and require an inclusive coordinated minimum. |
| B2: public restored SDK cohort unchecked | high | Patch: SDK assets are inspected only through nonempty local-candidate prefixes; the ordinary public path can select another EventStore package version. Require every restored SDK member to match the admitted cohort and retain package-only/archive checks. |
| B3: live lane remains4.2 | high | Patch: authenticated route builds and producer assertions explicitly select4.2 while default5.0.1 is newly admitted. Exercise5.0.1 in that blocking persisted-state/native lane, retaining explicit compatibility coverage. |
| B4: FrontComposer publication prerequisite pending | medium | Operational follow-through: the interim report explicitly marks publication/package UI as pending. Parent must publish the migration, select its actual public version and pass final package lanes before completion; no interim result supplies that qualification. |
| B5: historical raw source-migration reports are temporary | medium | Defer as pre-existing evidence debt: the retained historical summaries state the original scratch paths; this delivery preserves them without claiming a fresh raw capture. Final exact-source hosted evidence remains mandatory for publication. |
| B6: local source build input closure incomplete | medium | Patch: intermediate local receipts need a clearly bounded provenance statement and current dependency source manifest; do not retroactively attribute prior commands to a newer checkout. Final committed-source CI is separate authority. |
| B7: workspace focus hash does not identify endpoint process | medium | Defer as pre-existing validator limitation: the changed lines only migrate the output path; endpoint ownership/provenance was not introduced by this change. Final release artifact/source checks and hosted topology checks remain independent requirements. |
| B8: status-label JS uses mutable restore assets | low | Reject: the intended CI caller restores/builds and immediately runs this isolated fixture in one job, with the same centrally pinned Fluent package. No package divergence is caused by the framework path edit; adding an artifact identity subsystem is disproportionate to this pre-existing standalone invocation concern. |
| B9: source-framework deferral remains open | low | Patch: append the source-only resolution with its committed evidence while retaining the separately unfinished package/governance obligations; preserve the original ledger history. |
| B10: loaded retry-policy digest loses generated bytes | low | Patch: retain the safe run-generated policy or its complete reconstruction inputs with the report, preserving existing policy/hash and privacy controls. |
| V1: current default lacks live package qualification | high | Patch with B3: existing AuthenticatedTenantsRouteTests are excluded from general integration and only execute under the4.2-pinned blocking lane. Require5.0.1 native reads and persisted-state assertions before release. |

## Verification

### Additional owner CI repairs and current source checks

- FrontComposer commit `2f8308fa08e20c04c170c87a3c01b91c77b0a393` advances the live package baseline to published 5.0.0, records only the 32 bounded .NET framework differences for the intended 6.0.0 major release, and adds an unsuppressed comparison of the actual published net10.0 and candidate net11.0 runtime assemblies. All seven real library packs pass; controls reject removed APIs, parameter renames when enabled, and missing runtime assets after a prior successful extraction. The baseline-free diagnostic switch remains supported while publication forces baseline validation.
- McpCli commit `bf098ca759c4bca67f9ccfc9837ce05230cb19ba` carries the five retained net10-compatible package selections into initial and later source-host builds only after framework selection. Real net10/net11 evaluations include quoted and special-character workspace paths. Both immutable archived Tenants builds pass with the same output assembly hash. The census and ordinary release automation hosted jobs pass; native hosted qualification remains pending.
- EventStore commit `325c3ddcf122d7b6dfc9f2a5190fa88ac4fa1394` repairs two test fixtures that discarded MVC route data before the current scope filter. All 223 sample tests pass locally, including a new missing-tenant refusal control; production validation is unchanged. Hosted exact-source CI remains pending.
- Current root source integration and UI builds pass with zero warnings/errors. The source owner gateway suite passes 27 tests and the real-handler metadata suite passes four tests. The UI suite passes 8,081 tests; its one source-scan failure was caused by the temporary entry-point location. Running that complete safety class from its actual project ancestry passes all ten tests, covering the previously blocked test without altering its source scan.
- FrontComposer hosted Debug validation exposed a merge regression that swallowed unrelated focus exceptions. Removing the two blanket catches preserves disposal, cancellation and JS handling; the complete affected class passes 56 Debug tests. Its separate dependency catalog gate also reports stale projection seals, which require the documented source reseal and subsequent activation push before publication.
- Follow-up FrontComposer repairs preserve migration publication's existing classified-guide contract and the generated form's captured cancellation token, model revision, validation, target resolution and dispatch ordering. All three formerly failing migration CLI tests, three browserless identity tests, TypeScript checks, six legacy runner tests and the updated Chromium accessibility smoke pass. The three-engine Playwright hold remains: official 1.65.0 and 1.65.1 tarballs contain the same Firefox revision 1572, while the separate Chromium-only workspace advances to 1.65.1.
- Three independent final review layers return no findings. They independently reproduce the exact CI closure (17 action sources), Release closure (ten), the unchanged historical publisher content, all 69 graph edges and eight selectors, and finite old/new-version refusal. Every previous authorization row is preserved. FrontComposer seal `e3b96baa332f100270d86745a16e898e80a25f2b` and its separate documentation-only activation `fe9b0e3b76b4d1ea9cd120394f4a6cb165bfff9c` are pushed. Local committed-source activation replay succeeds with no dependency changes or affected modules; exact hosted CI/Quality remain pending.
- McpCli's next hosted source build exposed two additional net11-only Commons packages in the frozen Parties net10 graph. Commit `98a04b5c6cc4e8b5c090bce41f9619eb5bcec8ff` adds exact private Commons/Commons.Http 2.30.10 selections only for that graph and leaves the seven root override rows unchanged. Public archives, both immutable hosts and the Parties adapter are checked; initial and forced later builds pass with identical assembly bytes and zero warnings/errors. Hosted native acceptance remains pending.
- EventStore's complete hosted CI now succeeds for `325c3ddcf122d7b6dfc9f2a5190fa88ac4fa1394` in run `43557250270`. FrontComposer's remaining test-only source mirrors, candidate-version assertion and one newly added public test declaration inventory are aligned in `80dcd61e1d3d04428c755cc7eb483469be9e7554`; seven source-contract and 12 governance cases pass locally. The unchanged Roslyn collector proves precisely one added declaration and no removals, without changing analyzer scopes. A fresh root source UI build against the synchronized dependency heads has zero warnings/errors, and all 8,082 UI tests pass with zero failures or skips. Its receipt captures the actual dependency selection and assembly hash; a temporary runner placement issue was corrected under excluded output paths without changing product builds or scanners.
- McpCli's complete hosted CI now succeeds for `98a04b5c6cc4e8b5c090bce41f9619eb5bcec8ff` in run `43558572242`. Retained native evidence reports full live acceptance, all 21 vectors passed, zero failed vectors, zero vector exit codes and a passed independent authorization boundary. This later hosted result is separate from the earlier narrowly scoped census maintenance decision. FrontComposer's final default-lane fixture corrections in `46716fb07ed71d3f574a6d15f2763e907a6c0d5c` select the installed .NET 11 virtualization callback and generated-tree framework symbols; all 40 generated-component cases, 40 integration cases and two framework compilation controls pass locally. The only two snapshot changes are verified framework spacer metadata.
- FrontComposer's exact-source CI `43563141444` and full Quality `43563141109` succeed for `46716fb07ed71d3f574a6d15f2763e907a6c0d5c`. Its protected release selects 6.0.0. The first candidate passes build, tests and package gates but correctly refuses missing/expired attestation-fallback settings. Independent validation derives the immutable digest from its actual sealed manifest, with unchanged expiry, context and drift refusal; a two-hour delegated owner record and a fresh dispatch preserve those gates. Root Release package-mode UI builds against that retained candidate with zero warnings/errors. All 8,081 UI cases are covered after environment-only correction of the two focus-validator checks; public package verification remains separate.

Use individual project builds/tests; .slnx only for restore/build. Validate relevant Python/Node guards. Prefer Debug source checks locally, package-only Release checks for actual CI triage. Preserve exact commands/results and current source identities. Check git diff --check and repository release package manifests. Independently inspect latest exact-source workflow outcomes and public NuGet indexes/manifests.

Local implementation verification is recorded in `evidence/synchronize-update-release-2026-11-14/root-verification.json`. Package-mode Contracts and Server test graphs and Debug source Contracts/Integration graphs build with zero warnings/errors. Package governance/catalog checks pass 540 cases, source catalog checks pass three and the published SDK owner/readiness/runtime boundary passes 19. The isolated locally packed catalog consumer restores exact SDK 5.0.1, verifies archive/DLL hashes and discovers 23 routes, 23 operations and 12 adapters. Scratch candidate metadata uses the starting source revision and a local-only version; it is API compatibility evidence, not final committed-source provenance or publication evidence. Story Guards' nine regression commands, bootstrap/profile/restore/refusal guards, release-version proposal tests, npm clean installs/audits, TypeScript and performance-mode checks pass. Final package-only UI/Integration validation depends on the new FrontComposer publication; latest exact-source remote acceptance and release verification remain parent-owned and incomplete.

## Review Corrections Verified

The implementation agent corrected the two admission predicates, switched the existing authenticated native/persisted/browser lane to exact5.0.1, bounded intermediate local source provenance separately from a current root/all9 source manifest, recorded the source-only ledger resolution, and retained generated resiliency text with its digest. Parent independently reran the14 catalog-consumer and14 bootstrap controls and the1 policy evidence control; all pass. Earlier source/raw-report and standalone endpoint-binding limitations are retained as explicit deferred evidence debt. Publication and final committed-source hosted checks remain pending.

## Dependency CI Repair Review

All three independent dependency-repair layers completed before triage. The verification layer found no gaps; blind DB1–DB10 and edge DE1–DE2 are individually recorded below. These are owner-local CI repairs supporting the same release goal.

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| DB1: direct API task ignores configured rules | medium | Patch: ValidateAssembliesTask currently forwards neither the parameter-name rule nor attribute/internal/strict baseline settings; a real renamed-parameter fixture passes despite the configured rule. Forward the existing SDK settings and cover the distinguishing case. |
| DB2: initial source overlay downgrades net11 | medium | Patch: the initial overlay is path-scoped before TargetFramework and the later net10-only update cannot undo its items. Apply the finite compatibility rows after framework selection for the required source preparation/rebuild path, and test both generated files with net10/net11 owners. |
| DB3: baseline-free flag ignored | medium | Patch: the new runtime comparison opens a baseline even when the existing skip-baseline flag explicitly disables it. Honor that flag while normal release commands continue forcing it false. |
| DB4: quote-bearing workspace path breaks conditions | low | Patch: source_condition embeds raw absolute paths inside MSBuild string literals; a valid apostrophe path causes MSB4092. Escape condition literals and verify actual MSBuild evaluation. |
| DB5: compatibility documentation stale | low | Patch: docs still say4.7.2/emptyv5 ledger; directly describe5.0.0, the32 exact v6 records, direct runtime comparison and v6.1 expiry. |
| DB6: v6 migration guidance absent | low | Patch: the intentional runtime target change requires concrete SDK11/net11/package-family upgrade instructions and a migration index entry. |
| DB7: C# diagnostic-ledger check accepts broader PKV rows | low | Patch: production Python already rejects malformed scopes, but the C# test helper now accepts PKV006 without its exact framework/package/lifecycle constraints. Align those private checks and mutation controls. |
| DB8: missing multi-target runtime removal fixture | false | Existing SDK compatible-framework comparison checks retained netstandard APIs against net11, and baseline comparison catches absent standard APIs. The verification reviewer independently traced and rejected this claimed gap; the new direct comparison additionally ran in real Contracts/Schema packs. |
| DB9: other enum interface loss hidden | false | The retained face remainsnetstandard2.0; C# enum interfaces are inherited from that pinned framework's System.Enum. Source code cannot independently drop another inherited interface while preserving enum/type/framework shape; type/base-kind/API changes have unsuppressed checks. The .NET11 reflection assertion verifies the intended retained interface. |
| DB10: stale extraction/missing DLL guard untested | low | Patch: add warm-success then missing baseline/candidate runtime asset controls so an old extracted DLL cannot satisfy a subsequent comparison. |
| DE1: skipped baseline still opened | medium | Patch with DB3; the reviewer reproducedMSB3932 under the documented baseline-free setting. |
| DE2: both overlays retain a net11 downgrade | medium | Patch with DB2; hook-only coverage cannot detect an earlier unconditional path-scoped item. |

## Current public SDK qualification

EventStore5.1.0 is public at source `49084f66f8d6545cae442875355a7066d5f5e73d`; all14 official NuGet archives pass listed metadata, source, SHA512 and signature checks. Root Release package-only Server/Contracts/Client graphs build with0 warnings/errors, and642 relevant backend/catalog/governance cases pass without skips after correcting three5.1.0 test-helper expectations. The actual restored5-ID core SDK graph independently matches official catalog source/hashes. Private wrapper source copies/caches/outputs are removed after verification. FrontComposer current-source6.0.0 publication, final source-qualified root CI and root publication remain pending.

## Public frontend and Builds source integration

FrontComposer6.0.0 is actually public from `d7e8f57808b610f7cddf95ecd06db178dfd7592f`; its8 official archives pass source, SHA512, signatures and sealed payload checks. The later `b13587aa` current source is a docs-only descendant and is never substituted as package provenance. Root public Release UI/Integration graphs build without warnings/errors. All8081 UI cases are covered after correcting the private source-copy layout for2 restore probes;67 generated HTTP and27 owner-source gateway cases pass. Builds integrated upstream `fd9c4de2` including its independent5.1.0 adoption/audit; the duplicate local SDK patch was dropped. FC source commit `37a3cdd6` precedes the revision-bound FC-only audit, all156 other family decisions/rows are preserved exactly, and deterministic validation passes317 packages/157 families. Current-vs-frozen FC pin assertions are explicit; all98 pin/manifest controls and2 public owned UI consumer builds pass. Builds `1ebc5b84ff2c8afd21a87d80ef3d9c9048eaf5e5` is pushed and remotely verified. Final root exact-source CI and publication remain pending; final publisher approval also awaits the separately owned current FC mutation gate.

## Independent5.1 review

Parent independently reviewed the nine-file5.1 delta and reported no findings. It reran all14 catalog-consumer and14 bootstrap controls, verified all8 backend XML hashes/642 passed cases, checked11 real native SDK admission cases and13 source-only recovery cases. The retained parent-sdk51-bounded-review.json records the exact scope and evidence. Earlier three-layer review remains scoped to the initial release changes; supporting Platform/Mcp current-head renewal and final root CI/publication remain pending.

## Final owning source selections before root push

All9 direct dependencies are clean/equal remote main, with exact heads in prepush-synchronization.json. Platform `de3ce679` changes current EventStore coordinate and3 reviewed immutable metadata descriptors; governed producer/frozen tuples stay unchanged. Mcp `4ffb33e8` carries append-onlyPA02 source-digest maintenance retaining identity, signal, fixtures, nine-object closure and scanner. FC `b76cfb91` adds2 BMAD doc/status changes aboveb135; actual6.0.0 package source remainsd7. EventStore `f712c08e` adds1 spec and6 direct gitlink metadata changes above490, with product/test/helper/workflow bytes unchanged; actual5.1.0 archive source remains490. No nested checkout or Platform cascade occurred. Parent reviews and owner local controls pass; current supporting hosted gates remain pending and historicalb223 native evidence is not relabeled. Root source commitb4dabc9e remains local until this final metadata/pointer follow-up. Final publisher approval stays held for actual FC mutation and supporting gates, with no new human checkpoint.


## Final Upstream Integration

The first ordinary root push safely refused a concurrent remote advance. The two unpublished commits were rebased onto upstream `48218900f2836a60f2bd7760954a89601cf82bbb`, preserving its boundary dependency adoption and the newer owner descendants. Before this evidence append, every tracked tree entry at rebased `23f07e05` exactly matches the former unpublished `da3345a6`; the saved SHA256 manifest independently matches all non-gitlink files. Earlier local command receipts keep their original source attribution, and this exact source equivalence explains their applicability. Required hosted checks will run against the actual final pushed head; no earlier CI is relabeled.


## Hosted Packed Consumer Correction

At pushed `6f4df89e`, Story Guards and both source-reference lanes succeed. The published catalog lane compiles and passes its focused HTTP and owner controls, then its actual packed consumer refuses stale12-command/12-adapter expectations after upstream recovery added a command. A private publicSDK5.1 build/pack reproduces that exact refusal, passes the corrected24-route/24-operation/13-adapter inventory, and still refuses an inventory omitting the recovery route. All14 metadata refusal controls pass. The verifier now surfaces failed-child stdout/stderr while retaining the failure, and all existing archive/source/cohort/digest checks remain unchanged. The wrapper cleans its private workspace/feed/cache/outputs; final CI must qualify the subsequent committed repair.

Parent bounded review reports no findings: the exact24 unique route IDs,13+9+2 categories and13 adapter correspondence remain complete; all provenance/digest/archive/DLL/deadline checks are preserved. Its retained review records the distinguishing actual pass/refusal execution proof.
