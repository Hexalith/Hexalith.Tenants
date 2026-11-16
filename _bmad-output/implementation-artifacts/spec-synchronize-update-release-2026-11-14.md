---
title: 'Synchronize dependencies and publish the .NET 11 release'
type: 'chore'
created: '2026-11-14'
status: 'done'
route: 'dispatch'
review_loop_iteration: 1
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
- [x] Review corrections -- enforce a current Server dependency floor separate from historical archive admission, including no-build packing; qualify actual public EventStore5.1.1 before adopting the new shared catalog.
- [x] Recovery verification -- rejection-only state must remain not-found; prove exact snapshot budgets, registered durable delivery/checkpoint/gap recovery, and repeated versus fresh operation-ID behavior using existing production harnesses.
- [x] Testing/documentation parity -- add the ordinary fake recovery overload and clarify that contract decoration does not qualify current native McpCli enrollment.
- [x] Verification receipts -- refuse cross-revision evidence reuse and dirty source attribution in the Builds fixture verifier; make the Firefox host probe obey its stated elapsed-time deadline.
- [x] Root main -- validate commit message, push synchronized work, obtain successful latest exact-source required CI.
- [x] Release workflow and NuGet -- publish intended source, verify GitHub Release plus all five NuGet package versions and retained provenance.

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
- `.github/workflows/story-guards.yml`
- `scripts/verify-catalog-package-consumer.py`
- `tools/catalog-package-consumer/Program.cs`
- `src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs`
- `src/Hexalith.Tenants.Testing/Projections/InMemoryTenantProjection.cs`
- `tests/Hexalith.Tenants.IntegrationTests/AuthenticatedTenantsRouteTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/TenantsApiStructuralTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Routing/TenantIntentDeclarationTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Projections/InMemoryTenantProjectionConformanceTests.cs`
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
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/recovery-conformance-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/parent-root-rest-session-review.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/parent-root-recovery-review.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/independent-root-recovery-review.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/aspire-inventory-repair-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/parent-root-aspire-inventory-review.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/final-source-synchronization.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/root-platform6-provenance-assertion-audit.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/builds6-publication-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/builds601-publication-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/builds602-publication-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/platform-d77-current-acceptance.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/parent-platform-d77-current-artifact-review.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/builds-retained-sdk42-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/builds-retained-sdk42-audit-preservation.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/parent-builds602-compatibility-review.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/parent-builds602-integrated-review.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/builds12475-github-action-closure.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/root-postrepair-sdk-package-version-audit.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/mcpcli-current-hosted-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/builds-c91-integration-verification.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/root-final-submodule-url-sync.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/root-final-sdk-package-version-audit.json`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/eventstore-postrelease-source-synchronization.json`

- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `src/Hexalith.Tenants.Testing/Helpers/TenantIsolationTestHelpers.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs`
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs`
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `tests/Hexalith.Tenants.Client.Tests/Projections/DurableTenantProjectionStoreTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/ProjectionReplayIntegrityTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Helpers/TenantIsolationTestHelpersTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantAuditReceiptTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs`
- `tests/scripts/test_release_resume_verification.py`
- `tests/scripts/test_public_release_verification.py`
- `tests/scripts/test_release_evidence_replays.py`
- `_bmad-output/implementation-artifacts/evidence/fix-builds-sdk-cohort-fixtures-2026-11-15/`
- `_bmad-output/implementation-artifacts/evidence/fix-firefox-route-focus-ci-2026-11-15/`
- `_bmad-output/implementation-artifacts/evidence/latest-sdk-catalog-qualification-2026-11-15/`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-root-corrections/`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/final-root-corrections/`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-parent-integration/`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-parent-root-verification/`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-parent-root-verification-attempt2/`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-parent-integration-verification/`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-parent-integration-verification-attempt2/`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/parent-root-coverage-summary.json`

- `docs/event-contract-reference.md`
- `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`
- `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`
- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-parent-packed-verification/`

- `_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/eventstore-payload-ci/`

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

### Final Independent Review Triage — 2026-11-15

All three final layers completed before triage. The edge layer returned no findings. FB1–FB10 are the blind layer; FV1 is the verification layer's traced Other finding. Parent verified the non-gap findings against the actual caller/guard code.

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| FB1: recovery audit row rejected by UI | high | Patch with FV1: TenantAuditReadModel emits the administrative recovery row, while TenantQueryGateway's explicit supported-event switch rejects its whole page. Receipt outcome handling and EN/FR resources also omit the event. Add the existing row to those mappings and prove projected-row gateway/receipt behavior. |
| FB2: tenant-isolation helper drops recovery | medium | Patch: GetTenantId's success-event switch omits TenantAccessRefreshed, so both public selector helpers filter it out. Add the existing event and cover tenant separation. |
| FB3: malformed snapshots gain complete audit certificate | medium | Patch: IsCompleteAuditPayload validates old role events and scalar presence but accepts null snapshot collections and unvalidated nested roles/budgets. Extend only the new snapshot contract's checks using the existing SDK limits; malformed controls must not gain a tenant-sequence certificate, while valid snapshots remain complete. |
| FB4: current inventory verifier advertises old inventory support | low | Patch: the current compiled consumer deliberately requires the exact24-route/13-adapter inventory, but the net10 help text and archive wording imply full qualification of older23-route archives. Clarify that current discovery is strict and historical end-to-end qualification uses its recorded source verifier. Preserve old metadata admission and historical evidence; never let a current archive omit recovery by accepting either count. |
| FB5: composed authenticated recovery not exercised | medium | Patch: static gateway/controller and direct persisted actor cases do not prove the new command crosses the authenticated gateway boundary. Use existing live infrastructure for owner success, permission refusal and disabled-tenant recovery, asserting actual persisted events/state and owned cleanup. Do not add or bypass transport/authentication infrastructure. |
| FB6: upgrade guide default remains5.1.0 | low | Patch: its current-default statement conflicts with the actual5.1.1 property and fresh public qualification. Correct the default while retaining5.1.0 as an admitted recovery minimum where applicable. |
| FB7: verification owner path hardcoded | low | Patch: the new evidence verifier embeds this workstation's absolute checkout path. Derive the owner from its existing repository-relative artifact location; no additional public interface is needed. |
| FB8: failed attempt overwrites prior phase logs | medium | Patch: run opens a shared phase log before checking source stability. A changed-source refusal preserves the previous JSON but truncates its associated log. Write attempt-specific logs and bind each new check to them, preserving previous receipt and log bytes on refusal. |
| FB9: readiness can accept late200 | medium | Patch: urllib's socket timeout is per operation, so slow headers/redirects may finish after the elapsed deadline; the current200 branch does not recheck time. Use a bounded overall request, recheck elapsed time before acceptance, and exercise slow headers/redirect behavior rather than only mocked timeouts. |
| FB10: final CI/publication unchecked | false | No premature closure is claimed: status remains in-review and both parent-owned tasks are explicitly pending. The unchanged Release admission requires the latest exact-source workflows before publication. Their actual execution remains required operational follow-through, not a missing code guard. |
| FV1: new audit page becomes Error | high | Patch with FB1: verified GetTenantAuditAsync returns an Error snapshot with zero rows at its invalid-payload branch. Existing event/localization matrices cover only the preceding eleven event names. |

#### EventStore Payload Fixture Review — 2026-11-15

All three additional owner-review layers completed before triage. The edge layer returns no findings; the verification layer traces all six real build paths into the existing hosted wildcard/vector/installation checks and finds no gaps. The blind layer files six individual observations.

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| PB1: source defaults remove CI | medium | Defer as pre-existing: all six original source-mode fixtures already remove CI, and the frozen Platform guard checks that marker. An omitted package-mode workflow setting could therefore evade its Release/package guard. The actual payload workflow explicitly supplies Release/false; the changed package path retains CI and passes the original guard. Record the unchanged source-default caveat without claiming it was repaired. |
| PB2: runtime consistency test only parses XML | false | The focused policy test is structural, but the existing hosted discovery actually restores/builds all six selected Platform component/role paths and executes their outputs. Role closure verifies actual runtime11 metadata; the retained missing-import/global-only attempts fail real reference discovery with NU1202/NETSDK1005. The complete graph behavior is therefore covered beyond the XML assertion. |
| PB3: inherited custom hook replaced | low | Reject under the low-impact complexity rule: an explicitly customized package probe could lose its inherited CustomAfterMicrosoftCommonProps hook. The maintained workflow supplies none, default source mode adds no hook, and these offline package fixtures choose their own runtime import and grant no mapped/native qualification. Chaining arbitrary imports or adding a new conflict interface would complicate an uncommon unsupported fixture customization. |
| PB4: duplicate policy can diverge | false | The six callers currently use the same validated policy and one shared runtime props file; the new matrix tests every caller's selected configuration, reference mode, hook and CI handling. No present caller divergence is demonstrated. A general helper refactor does not resolve a verified defect. |
| PB5: comments call every input frozen | low | Patch: the same source-only setup can select an unverified sibling checkout. Replace frozen with selected in the two new comments. Exact frozen identities remain claims of the recorded private run, not the generic fixture branch. |
| PB6: mocked TargetPath test stops before runner | false | Both existing real snapshot/producer setup paths evaluate TargetPath, require the resulting actual file and execute that assembly to emit vectors consumed by their tests. The successful private run and maintained hosted wildcard cover this positive handoff; a separate mocked positive case adds no missing behavior coverage. |

#### Remaining bounded patch tasks

- [x] UI audit support — add TenantAccessRefreshed to the existing administrative event, receipt and EN/FR mappings, with a projected-row-to-gateway/receipt regression. Keep unknown events/category mismatches fail-closed.
- [x] Projection/testing support — reject malformed new recovery snapshots for completeness certification with existing SDK limits; include the successful recovery event in tenant-isolation helpers and cover both behaviors.
- [x] Authenticated recovery proof — exercise the existing composed live gateway and persisted backend for a current owner, denied permission and disabled tenant, with real stored event assertions and owned cleanup.
- [x] Verification/docs corrections — preserve phase logs on source drift with attempt-specific paths, make the evidence script portable, enforce the readiness request's real deadline, clarify the strict current versus frozen historical consumer inventories and correct the upgrade guide default.

These are direct corrections within the approved current-source release intent. Preserve every prior successful change, source-specific receipt, baseline, immutable historical inventory, authority boundary and refusal control. Read the ordinary shared state instructions before projection/persistence work and UX instructions before UI work. Parent owns integration and complete verification after the implementation subagent returns; it continues to own all Git, remote CI and publication.




#### FrontComposer Regression Review — 2026-11-16

All three additional owner-review layers finish before triage. The edge layer returns no findings; the verification layer finds no gaps. The five blind observations are independently traced against the actual public helpers and existing tests. They concern the new test oracles; no production defect or completed mutation score is inferred.

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| FCB1: Skill identity arguments are not asserted by the new skill payload test. | medium | Patch: Only this new SourceTools test invokes CreateSkillCorpusResourcePayload; canonical contractId, schemaVersion and protocol rows plus independent identity/fingerprint variations are absent. Add direct assertions for the existing negotiated identity contract. |
| FCB2: Renderer test omits independent fingerprint sensitivity. | medium | Patch: Existing RendererPayload_BoundsContributeToFingerprint changes both limits together, so loss of either individual bound can pass; the new case checks text only and strategy has no paired fingerprint assertion. Extend the new case with independent strategy and bound variations. |
| FCB3: New diagnostic equality case admits reference-only equality. | medium | Patch: Its same-instance and null assertions do not establish independently created diagnostic value equality or sensitivity to every identity field used by generic caches. Existing round-trip cache cases do not replace these new direct public comparison boundaries; assert equal-copy hashes and changed-field inequality without numeric hash pins. |
| FCB4: First-enum fixture does not distinguish declaration and name order. | medium | Patch: The first property is A and second Z, so alphabetical sorting preserves the same answer despite the parser documented declaration-order fallback. Reverse property names and add the corresponding unknown-state refusal using the selected enum. |
| FCB5: Malformed annotation cases leave fallback metadata unchecked. | medium | Patch: The test only requires Value to survive, admitting fabricated CTA/group/priority/format metadata. RelativeTime(null) currently uses int and can stop at the earlier incompatible-type branch. Assert existing fallback metadata, and use DateTime to reach invalid-window handling with its warning/default-format contract. |


All five bounded assertion corrections are applied to the four new owner test files on b34be9245443d21b72a6076cf205bbbc8a8c521f. Parent reads the changes, authenticates210 sealed evidence files and exact working-input hashes, and verifies38/38 passing cases in the two edited classes. The unchanged provider case retains its earlier exact-body proof; no clean committed-source or full mutation qualification is inferred. The real malformed RelativeTime(null) compiler context has zero constructor arguments and the existing RelativeTime/seven-day fallback without HFC1032; the separate compatible1/0 constant case verifies the actual warning/Default/null-window path. Earlier36/2 failed expectations remain retained. Original53 TRX captures and the complete Error JSON/HTML are preserved byte-for-byte outside copied/global output ignore rules. Full original Happy reports, truthful finite owner dispositions and fresh final-source gates remain pending.

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


## Recovery Conformance and Bound Live Credentials

At `a2142101`, all packed catalog, HTTP, persisted, anonymous/authenticated UI, source-reference and Story guard lanes pass. Full Tier1 identifies three remaining recovery omissions in the Testing dispatch/projection and exact permission fixtures; five REST cases reject a legacy unscoped token-store write. The fake now delegates the new command/state event to existing aggregate methods, explicitly leaves business projection state unchanged for the access snapshot, and tests owner/global/disabled success plus reader/missing/target-substitution refusals. The fixture uses one exact session/subject/system-tenant binding through the existing FC relay. Runtime domain handlers and authentication policy are unchanged.

Actual publicSDK5.1/FC6 builds pass without warnings; all189 Testing cases and1888 Server cases pass, followed by all31 intent cases after review adds the missing recovery alias/target/retention/version row. The real registered token-relay handler passes its exact binding and three foreign-binding/unscoped-write refusals. Wrapper cleanup is verified. Evidence retains initial collection-reference failures and private cache/source/probe-layout corrections. Parent and two independent bounded reviews report no remaining findings; the sole coverage finding is resolved by that new intent row. Final hosted Tier1/live cases must pass on the resulting source.


## Complete Hosted Recovery Inventory

At `f3b0e85f`, all eight main functional CI jobs succeed, including full Tier1 and all eight live REST cases; Story Guards and both source-reference lanes also succeed. The additional shared Aspire hook runs683 Integration cases and exposes two remaining exact inventory omissions (681pass/2fail/0skip). The generated controller assertion now includes the exact POST refresh-access action, and the explicit budget-rejection row exercises the existing422 ProblemDetails, reason, identity, redaction and corrective-action assertions. Actual package-only build and all17 focused cases pass, with wrapper cleanup verified. Parent bounded review reports no findings. The complete final-source CI, including this shared smoke hook, remains required.


## Final Current Dependency Source Selection

At `91405352`, all root required workflows succeed, including all684 shared Aspire cases with0 failures/skips. A fresh nine-owner read verifies clean checkouts equal actual remote main heads. Nonrecursive `git submodule sync` aligns every declared/local/origin URL without changing owner HEADs or nested checkouts.

Builds source advances to `c91beec6`, preserving two external finite PlatformSDK4.2/M327 compatibility-view commits and their revision-bound audit. Global defaults remainSDK5.1,Memories4,FC6. Shape317/audit157,202 positive-negative views, complete authority controls and all43 tests in both changed Module classes pass with0 warnings/errors; the entire Github action/helper tree is identical to1ebc. The official fresh SDK feed confirms11.0.100; actual normal Release evaluations of18 root projects and42 direct package IDs match latest listed stable versions except the explicit frozen3.128.4 bootstrap harness. Git-blobLF and BOM/CRLF checkout catalog hashes both match thec91 audit.

FC selects `89b1c553` after reviewed test/CI-only followups and supported MTP collector wiring. Original configs, Category filters, thresholds and validators are unchanged; real per-test coverage, known mutation kill and full cohort case-ID equality qualify the collector fix locally. Full authoritative Nightly still must pass. Public6.0.0 archives retain sourced7. Platform selects merged `2119b7ed`; signed producer6.0.0/source1ebc observes defaultSDK5.1/Mem4 separately from the explicit admitted SDK4.2/T707/M327/P311/Mcp201 native build/replay family. Frozen4/5 roles remain unchanged; no defaultSDK5.1 native credit is inferred. Current hosted Platform gates remain pending.

The latest sourcecatalogc91, actual signed producer6/1ebc, Root CI executor6ad5854 and Release executorff3 are distinct. Root live/source assertions dynamically record actual sources and need no producer assertion migration. EventStore currentf712 passes completeCI/integration while public5.1 archives retain490 provenance. Mcp current4ff passes completeCI,445-surface census and authenticated21-vector/45-boundary live proof limited to retained frozen profiles; it does not enroll Root5.1/new recovery operations. Final root source must pass all three workflows including shared Aspire; final publisher approval stays held for outstanding automatic dependency gates.


## Published producer compatibility repair

Root `0cfe90e8` passes all three required workflows, including all nine functional CI jobs and shared Aspire. Its unprotected 8.0.0 preparation succeeds with five exact source packages and the real consumer discovers 24 routes, 24 operations and 13 adapters. That candidate remains unpublished and is deliberately cancelled before any publisher step because later dependency repairs require a new final source. Its artifact and source markers remain historical.

The actual Platform reader reveals two producer gaps: signed Builds 6.0.0 does not retain its SDK 4.2 view, and 6.0.1 still selects Memories 4 for private Parties despite SDK 4.2. The unchanged catalog validator correctly refuses an inconsistent caller override. Upstream `590a56fa` and `60ce7967` supply the finite Parties, Platform and three packaged-host SDK 4.2/M327 correction and revision-bound audit. Those commits are preserved; `12475add` adds stronger tests only. All 247 actual catalog views and 51 materializer/declaration cases pass with zero warnings, failures or skips; independent reviews cover the complete scope. The existing 590a audit remains valid and preserves 156 unrelated family decisions and 312 package rows exactly. At this stage, source 12475 passes all seven functional owning CI jobs.

Builds 6.0.2 is actually public from immutable source `60ce7967607dc8b703fff11f4fc13cabb09c090a`, distinct from source 12475. Both official listed archives pass source, SHA512 and repository-signature checks. The real CLI reports 6.0.2+60ce; all 38 Evidence and 316 Module unsigned payload entries match the authenticated original artifact. The existing publication was observed and verified; a redundant queued dispatch is cancelled before any jobs. Existing environment protection is unchanged. This publication supplies no native replay credit. Platform must bind the actual source/archive and pass its fresh complete qualification.

A fresh 15:30 audit binds the then-current catalog blob/raw hash and confirms SDK 11.0.100 plus all 18 actual normal Release project selections. The 42 direct package identities and versions remain latest listed stable except the explicit frozen 3.128.4 bootstrap harness. Finite SDK 4.2 compatibility views do not change Root's generic SDK 5.1/Memories 4 defaults. All disposable verification workspaces are removed after completion.

The FC WebKit failure is an existing product entry-focus readiness defect, also present in published 6.0.0/D7. A demonstrated fix needs a verified public 6.0.1 patch before Root advances its package selection. SourceTools mutation qualification retains its actual source/input scope and must complete without weakening cohorts or scores. Final Root owner pointers, version/audit receipt, exact source CI and publication remain pending those concrete inputs.


Platform `d77f741d` subsequently passes both exact source workflows, including the complete tuple verdict and all six catalog consumers. Its authenticated current artifact proves 478 current SDK 4.2 cases, 97 base cases and 9 extensions, all passing without failures or skips; historical 349/412 replays also pass. Current .NET 11 / Builds 6.0.2 collectors remain distinct from the retained .NET 10 / 4.47 reader. The receipt supplies its stated package-mode source/consumer regression scope and does not infer a new image, OCI or release identity.

## Superseded Spec Register — 2026-11-15

The user requested "close obsolete specs". The following twelve historical plans are administratively closed as superseded by this current release goal. Their original frozen intent, checklists, reviews, evidence and baselines remain intact. This register consolidates tracking; it does not expand the approved release scope or claim that an unchecked historical gate passed.

| Superseded spec | Qualification retained in current tracking |
| --- | --- |
| [spec-eventstore-secret-scanner-ci-repair.md](spec-eventstore-secret-scanner-ci-repair.md) | Current owning-source security/unit CI and any separately required Azure conformance remain distinct; historical unit or scanner results do not grant live-Azure acceptance. |
| [spec-fix-native-removal-production-transport.md](spec-fix-native-removal-production-transport.md) | Current-source authenticated native removal, persisted owner authority, revocation/refusal behavior and owned cleanup remain required in the release qualification. |
| [spec-native-mcpcli-runtime-cohort.md](spec-native-mcpcli-runtime-cohort.md) | Retain exact package/source binding, every required vector plus smoke, persisted effects, independent authorization refusal and owned cleanup. Historical native results do not enroll new recovery operations or a different SDK cohort. |
| [spec-native-query-selector-compatibility-cohort.md](spec-native-query-selector-compatibility-cohort.md) | Preserve frozen wire/vector contracts, digest-bound selectors and exact receiver/scope/permission authority; qualify each actual native consumer cohort rather than transferring old acceptance. |
| [spec-native-request-compatibility-follow-up.md](spec-native-request-compatibility-follow-up.md) | Retain pre-build exact-byte audit coverage, signed nonmember/unavailable-owner refusal, unchanged vectors, persisted behavior and owned cleanup under the current qualification scope. |
| [spec-preserve-sample-background-ci.md](spec-preserve-sample-background-ci.md) | Keep the complete Sample/background topology coverage, real grant/revoke and pub/sub persisted behavior, authentication and skip refusals in the current release gates. |
| [spec-publish-compiled-catalog-inputs.md](spec-publish-compiled-catalog-inputs.md) | Require current compiled catalog/actual-PE package-consumer verification, all five package identities, exact source/archive provenance and final intended-source publication. |
| [spec-qualify-published-eventstore-4.0.3-cohort.md](spec-qualify-published-eventstore-4.0.3-cohort.md) | Keep current public-archive/source verification, catalog/refusal checks and exact-source CI separate from every historical compatibility receipt. |
| [spec-qualify-published-mcpcli-7.0.0-enrollment.md](spec-qualify-published-mcpcli-7.0.0-enrollment.md) | Preserve authentic published-artifact approvals and frozen vectors; current enrollment, replay, native behavior and publication require their own exact-source evidence. |
| [spec-refresh-and-publish-current-release.md](spec-refresh-and-publish-current-release.md) | The successor’s unchecked exact-source CI and publication tasks remain open; closing this old release attempt does not claim a new release or reuse old producer acceptance. |
| [spec-repair-current-mcpcli-published-cohort-ci.md](spec-repair-current-mcpcli-published-cohort-ci.md) | The existing remaining-census follow-up in deferred-work.md stays open and unchanged. No additional source renewal, classification, exclusion or approval follows from this closure; current committed-source census/hosted/native and publication acceptance remain separately scoped. |
| [spec-scope-live-corpus-read-tokens.md](spec-scope-live-corpus-read-tokens.md) | Preserve exact-corpus least-privilege tokens, malformed/incomplete corpus refusals, cancellation, all maintained browser cases and admission windows; require current-source hosted acceptance. |

This spec stays `in-review`; its exact-source CI and final five-package publication tasks remain unchecked. Historical native results remain limited to their recorded source/package/profile. In particular, the [remaining McpCli census follow-up](deferred-work.md) still requires its specifically scoped authority and qualification; this closure neither approves the proposed source renewals nor infers acceptance for a new SDK or recovery operation. The separate real-Azure conformance boundary also remains unchanged.

[Closure validation](evidence/obsolete-spec-closure-2026-11-15/validation.json) records preserved historical bodies/checklists, successor links, status conservation and whitespace checks. Historical gitlink-audit failures were present before this documentation edit and are retained as separate blockers; no source, dependency pointer or audit baseline was changed.


## Resumed Review Triage — 2026-11-15

All three reviewers completed before this triage. B1–B10 below are the resumed blind layer; E1–E2 are edge findings; V1 is the pre-verified durable-delivery gap.

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| B1: old SDK current-source packing | high | Bad spec: current aggregate references5.1 snapshot types while the canonical package target admits4.x/5.0.1, including GenerateNuspec without a build. Separate current package production from historical archive verification and qualify the observed5.1.1 patch explicitly. |
| B2: rejection-only aggregate history | medium | Patch: TenantState.Apply(TenantNotFoundRejection) leaves an empty TenantId; refresh checks that empty identity before the null/not-found branch. Preserve payload target substitution refusal and return not-found for the uncreated state. |
| B3: exact snapshot budget boundaries absent | medium | Patch: oversized cases cover both budgets but no exact threshold. Add below/equal/above tests through the aggregate using the actual SDK limits and serialization. |
| B4: durable refresh not exercised | high | Patch with V1: handler and registration changed, while registered durable tests deliver only prior event types. Prove persisted revocation recovery, checkpoint continuity and a subsequent business event. |
| B5: recovery operation-ID behavior not exercised | medium | Patch: the decorated command promises same-ID retry/fresh-ID recovery; existing static intent rows alone cannot establish persisted event counts and snapshot contents. Cover it through the existing runtime command harness rather than introducing idempotency infrastructure. |
| B6: ordinary fake API lacks recovery overload | medium | Bad spec: ProcessTenantCommand supports recovery but the ordinary typed ProcessCommand API lacks the parallel operation. Add the same thin Handle/Apply delegation and parity checks as the other tenant commands. |
| B7: native enrollment wording overstates decoration | medium | Patch: reference says exposes ten commands while the current release retains a frozen native cohort without recovery enrollment. Clarify decorated metadata, supported REST/SDK invocation and separate enrollment prerequisites. |
| B8: evidence-only revision substitution | medium | Patch: verify-current.py copies prior successful module rows into a receipt naming ambient Builds HEAD without comparing revisions. Refuse the mismatch and preserve original attribution. |
| B9: dirty copied files labeled by HEAD alone | medium | Patch: verify-current.py copies working-tree bytes but records only HEAD. Require a clean tracked source selection and bind copied inputs or verify the source remains unchanged before writing success. |
| B10: Firefox readiness limit overruns | low | Patch:120 requests each allow1 second plus quarter-second pauses, despite the claimed30-second deadline. Use monotonic time and bound each wait by remaining time. |
| E1: no-build packing admits old SDK metadata | high | Bad spec with B1: GenerateNuspec checks the permissive canonical cohort even when existing binaries require5.1. Historical archive admission remains separate. |
| E2: evidence-only reuse after head moves | medium | Patch with B8: the previous revision is read but never compared before importing checks. |
| V1: registered durable recovery gap | high | Patch with B4: the verification reviewer traced the production registered processor and existing durable harness; no refresh case proves a withheld revocation is replayed or checkpoint4 is persisted. |

## Resumed Implementation Instructions

The new review corrections above are the local implementation agent's remaining root source scope. Parent owns all Git integration, dependency source selection, remote CI and publication. Do not stage, commit, push, change any submodule, or re-run this Build skill. Preserve all pre-existing working-tree edits and evidence. Do not reimplement platform idempotency or snapshot infrastructure. Use `run-with-temp` for disposable builds/tests; create copies, feeds, caches and outputs beneath its TMPDIR, and retain useful logs outside it.

Investigate actual current files, not historical Code Map coordinates. Root remains at d73267fdbe84d09ccddf0a6492390e32d8938405 before these corrections. The parent observed public EventStore5.1.1 and upstream Builds afeee370137aaf961451e4f6881a61251273e674, which selects that patch. Parent has not yet advanced the shared catalog checkout. Add precise5.1.1 admission/refusal controls and update current lane expectations coherently; retain explicit historical archive/profile cases and finite refusal of unknown versions. Do not silently label5.1.0 or another earlier command as5.1.1 evidence. The parent will select and remotely verify the new catalog before final exact-source CI.

Resumed review modifies the non-frozen tasks to resolve package-production and fake-API omissions. KEEP every existing user change, historical baseline/review/receipt, frozen archive cohort, exact authority and refusal check, and all current-source recovery behavior except the demonstrated rejection-only-history defect. Higher-priority preservation instructions prohibit reverting accumulated user/upstream work; re-derivation is therefore limited to these bounded corrections. Frozen intent and baseline_commit remain unchanged.


## Resumed Root Corrections Verified — 2026-11-15

The bounded resumed corrections B1–B10, E1–E2 and V1 are implemented and locally verified. Current package production now requires the snapshot-capable exact EventStore5.1.0 or5.1.1 cohort before restore, build and GenerateNuspec; historical archive admission and the frozen3.128.4 harness remain separate. The ordinary default and live lane now select5.1.1, with finite neighboring-version, prerelease, revision and malformed-metadata refusal controls.

All14 listed official5.1.1 NuGet archives match immutable release source `f792bba6c0564d3c36dda6ad1c2fc55b0b0a6331`, official catalog SHA512 and successful repository-signature verification. Byte-bound private package-mode builds pass without warnings/errors. All1896 Server,103 Client,197 Testing and574 package-governance/catalog cases pass with no failures/skips. They include rejection-only not-found behavior, below/equal/above entry and actual serialized-byte budgets, ordinary fake API parity, and registered durable refresh delivery with contiguous/withheld-revocation histories, persisted checkpoint4, restart and subsequent business-event checkpoint5. The actual Dapr actor suite passes all20 cases, including original-operation retry without another event and fresh-operation snapshot contents after a persisted revocation. Existing platform idempotency and snapshot machinery are reused unchanged.

All five actual local packages pack and validate, and the public5.1.1 package-only consumer discovers24 routes,24 operations and13 adapters. These scratch packages use a local-only version and starting-HEAD metadata; their input manifest identifies the modified working-tree bytes. They supply API/behavior compatibility evidence, not final committed-source or publication provenance. The clean Builds input is `264e17d3185c87b72b0757dd66a13040793756bb`; other copied owner inputs retain their own exact identities and hashes.

All34 Python guard cases pass, including actual old-SDK restore/build/no-build pack refusals across all five packages, revision/manifest and dirty-input receipt controls, source-change-during-child refusal, and the elapsed-time Firefox readiness deadline. The real evidence-only invocation refuses the changed Builds revision and leaves the historical receipt byte-identical. Contract documentation now distinguishes decoration and authenticated REST/SDK recovery support from separately qualified native McpCli enrollment. No frozen vector or enrollment authority changes.

[Completion receipt](evidence/synchronize-update-release-2026-11-14/resumed-root-corrections/completion.json), [current source checks](evidence/synchronize-update-release-2026-11-14/resumed-root-corrections/verification.json), [live actor checks](evidence/synchronize-update-release-2026-11-14/resumed-root-corrections/integration-verification.json), [actual packing](evidence/synchronize-update-release-2026-11-14/resumed-root-corrections/packed-verification.json) and [public SDK archive verification](evidence/synchronize-update-release-2026-11-14/resumed-root-corrections/public-sdk-5.1.1.json) retain commands, input hashes, raw results and initial verification-copy corrections. Wrapper-owned sources, feeds, caches and outputs are removed after every completed child; no owned Dapr sidecar remains. Whitespace checks pass. Parent-owned Git integration, final dependency selection, complete exact-source hosted CI and final five-package publication remain required and unchecked.


## Resumed Parent Integration — 2026-11-15

The local review corrections qualify the actual public EventStore5.1.1 archives at immutable source `f792bba6c0564d3c36dda6ad1c2fc55b0b0a6331`. All 2790 selected C# cases and 34 Python controls pass, including actual persisted Dapr retry/fresh-operation recovery, exact snapshot thresholds, registered durable checkpoint recovery and all five real packs/catalog consumer. Private build snapshots and original receipts retain their own source attribution; final committed-source CI and publication remain required.

Builds' two stale current-catalog SDK expectations were independently repaired upstream at `264e17d3185c87b72b0757dd66a13040793756bb`. Its actual catalog is unchanged from `afeee370137aaf961451e4f6881a61251273e674`; the duplicate local test-only commit was dropped through rebase. FrontComposer's four-field source reseal and three package expectation renewals landed independently as `50def1fe238787e9365c93991e3dc83af8c2e299` and activated at `d654bb412bfc54635b356527a89988a99f91992e`. The parent integrated them without changing any evaluator authorization. Its existing seven local form/fixture changes were preserved and committed as `923988d855cd6131b4287e3da763f12baf60ab22` after 117 Shell/governance, 182 graph and 71 generator cases passed. Initial private graph fixture omissions and successive stale package expectations remain failed attempts, not green qualification.

FrontComposer's first actual current-source mutation run43743363373 fails before mutations because the SDK container's Git refuses its mounted workspace ownership. A one-line exact-workspace safe.directory setup passes a real Git different-owner control: both repositories initially refuse; only the intended checkout succeeds afterward, while the unrelated repository remains refused. The `8ce46cfc1293925459c8b6af15ef7719cf0fc071` source repair needs its own fresh hosted qualification; no mutation score is inferred from either attempt.

Platform source `217889d7830553c9a8c5340f02f9ec5fe87c8001` renews only the three immutable companion selectors after synchronized documentation changed the compared tree. The existing preparation function preserves every frozen metadata hash/gitlink and all production, controls and helpers. All 47 source controls, 5 reader controls and three real committed-source equivalence checks pass; the52 tests and comparison receipts grant no runtime/candidate acceptance. Three immutable companion refs and main were pushed and remotely verified. GitHub reported its actual PR-only rule bypass for the authorized maintainer main push. Fresh tuple/catalog gates remain required; later concurrent owner descendants must retain their own selection and evidence.

A simulated committed event first exposed 14 historical audit failures in the pre-existing documentation edits. The supported historical-audit protocol now binds each to the latest actual story-associated pointer event, preserves every original baseline, frozen intent and checklist, and records the complete historical range inventory without claiming runtime ownership. Two already-landed creation corrections retain their original IDs in prose while advancing to their later committed, already-corrected baseline endpoint. The second real committed-event replay passes all 16 selected stories with zero failures and no pointer movement. Historical documentation must therefore land in a separate push from current gitlinks, as the unchanged guard requires. Earlier closure validation remains historical and is supplemented by the new reconciliation receipt.

The prepared Root release43735886034 was cancelled before publication because it selects the older `d73267fdbe84d09ccddf0a6492390e32d8938405` source. Its successful preparation remains historical; a fresh final-source CI/preparation/publication is required. No publisher approval or package release is claimed by these integration notes.


## Renewed Parent Source Comparisons — 2026-11-15

Concurrent Platform d386e36005214f925b391a34684748e6c56728e6 updates documentation and release tooling; its strict same-code companions therefore require a fresh source binding. Commit5b44f2d058f9221d8b54782c377e09fec5786b65 updates only the two selector descriptors to three newly pushed immutable companions. All47 source,5 reader and82 release controls pass, followed by all three actual committed-source equivalence checks. Main and companion refs are pushed and remotely verified. Their exact frozen metadata and349/412 counts are preserved, with no runtime/candidate acceptance inferred. GitHub again reports the authorized maintainer's actual PR-only main-push bypass; persistent protection is unchanged. Fresh hosted gates remain required.

The original parent command receipts and failed attempts are preserved under evidence/synchronize-update-release-2026-11-14/resumed-parent-integration/; their embedded original source identities/log paths remain unchanged. Historical documentation is integrated separately from pointer movement so the unchanged committed-event guard can authenticate its explicit endpoints.


## Final Bounded Root Corrections — 2026-11-15

The UI now admits the existing TenantAccessRefreshed administrative row through the gateway, receipt and rendered outcome mappings, with EN/FR translations. Unknown events and mismatched categories still fail closed. A real persisted ProjectAsync recovery row survives gateway mapping and becomes a Ready receipt. Both public tenant-isolation selectors retain recovery only for the requested tenant. Completeness certification now validates the new snapshot's nonnull collections, actual lifecycle, member roles/identifiers, unique configuration keys/member names and actual/default serialized and entry budgets using ProjectionSnapshotLimits.Default; malformed actual ProjectAsync inputs never gain tenant-sequence authority. Business projection behavior is unchanged.

The evidence verifier derives its Builds owner from its repository-relative location, retains unique per-attempt phase logs and check records, and preserves the prior JSON and associated log bytes when source attribution fails. Readiness runs each HTTP attempt in an owned process bounded by the remaining overall deadline, rechecks monotonic time before accepting200, and kills/waits the request child on timeout. Real scaled-deadline slow-header, redirect and late-response controls prove refusal and cleanup. The actual probe lives in the retained Firefox evidence verify.sh; root ci.yml contains no separate urllib readiness probe. Current consumer help/docs retain strict24-route/24-operation/13-adapter discovery for both runtime targets and direct historical archives to their recorded verifier revision. The upgrade guide selects5.1.1 while retaining5.1.0 as the recovery minimum/history.

Only affected checks ran:26 projection certification,6 isolation,41 audit gateway/receipt/translation/rendering and1 persisted projected-row-to-gateway/receipt case pass, with no failures/skips. All7 receipt/deadline and14 catalog metadata controls pass. Builds pass without warnings/errors. The initial renderer failure and private-copy/compiler corrections remain separate attempts. The [completion receipt](evidence/synchronize-update-release-2026-11-14/final-root-corrections/completion.json), final-python-verification.json, named XML results and phase logs retain exact commands, source hashes and source-specific outcomes; prior receipts remain unchanged.

The new composed authenticated test uses the existing real gateway/backend fixture, a distinct current owner, exact403 permission refusal with no appended event, and active/disabled snapshot event/state assertions. It compiles, but local runtime acceptance remains pending: after the four actual host prebuilds pass, the retained local backend refuses its missing committed catalog identity/profile before any test case executes (exit58/0cases). The unchanged provisioning helper requires the declared fresh GitHub job, actual committed gitlinks and a newly owned empty Redis backend; none were fabricated, replaced or bypassed. The raw refusal is retained in authenticated-owner-recovery.log and live-verification.json. All owned fixture processes/containers and wrapper workspaces have finished cleanup. The authenticated-proof checkbox remains unchecked for the parent's genuine prepared CI lane. Status stays in-review, review_loop_iteration1, and final full verification/Git/publication remain parent-owned.


## Parent Root Verification and Source Handoff — 2026-11-15

Historical documentation4faa79395b092fd73cd552ae9a37fdcbf77124ae is pushed separately; all16 actual committed-event story audits pass locally and the hosted story-gitlink job passes. Current source corrections preserve those baselines/endpoints and do not edit the historical stories again.

Parent independently verifies1909 Server,103 Client,198 Testing and574 package-governance/catalog cases. The full8085 UI invocation passes8084 and exposes only one omitted tracked evidence Markdown in the private copy; after restoring that input, all96 cases of its class pass with unchanged common input hashes. The composite receipt preserves the first failure, every resolved case identity and exact successful scope. All four Python guard suites pass. The ordinary Integration tier and the genuine prepared authenticated lane remain separate checks.

FrontComposer984b6be79a09e5e5796f75c9c0eb35ab4ccc2707 fixes the captured-context .NET10 fallback and its framework/compiler/Shell controls;73 generator and161 Shell cases pass. Its hosted source-contract failure identifies three missed assertions, corrected without changing their ordering/cancellation checks at a6ed4b94b74aacf833c76ba1391a51e383bd6478. All49 affected C# and3 browserless Playwright contract cases pass. The queued984 mutation attempt is cancelled before execution and replaced with43750383653 for a6ed; the earlier8ce error-handling segment continues with its original evidence. No mutation score is inferred or threshold/cohort weakened. Public FrontComposer remains6.0.0/source d7e8f57808b610f7cddf95ecd06db178dfd7592f until a fresh verified patch publication.

New upstream Builds04bdcaf929198e69321c226dda5ab40f7b3839ce adds a bounded60-minute full-suite CI option; catalog and runtime source bytes are unchanged. Memories59df159117c2f92c262804e8574a7e979525d202 repairs owned semantic-inventory test fixtures only. Both are integrated by fast-forward and remotely verified. Their later source identities do not relabel earlier local snapshots. Current owner content is clean/pushed before its root gitlink is recorded; final dependency selection, exact-source CI and publication remain pending.


The parent then runs the unchanged ordinary Integration CI script: all 690 cases pass with no failures or skips, including the slim persisted actor tier. Its first direct-runner attempt refuses an unsupported command-line option before discovery; the successful attempt uses scripts/run-aspire-integration-tier.sh exactly. All five current local packages also pack and validate successfully, and the actual public SDK 5.1.1 consumer discovers 24 routes, 24 operations and 13 adapters. These are modified-source local compatibility checks with local-only versions and starting-HEAD package metadata. Final committed-source hosted admission, authenticated recovery on its prepared backend and real publication remain unchecked. The wrapper workspaces and owned fixtures have completed cleanup.


A final precommit read observes Memories61a2caef3f3b7d556a856100b04b05feddddfb34. Its sole change makes the existing owned-child termination fixture publish its PID atomically and observe SIGKILL completion under a fixed bound. The parent integrates it by fast-forward and runs that exact offline test successfully. No domain, package or authority source changes. The read-only FrontComposer release planner selects6.0.1; this is a proposal, not publication.


McpCli9b9c2c46e29d9b34b51badbdb4334b4ca0047930 is a four-file upstream census metadata continuation. It changes no scanner, production, profile or runtime source. Its seven exact exclusions retain their recorded source revisions/digests and disclaim human source review, runtime, migration and publication acceptance. The parent preserves this upstream artifact and its authority limits without creating another approval. Full committed-source owning census remains required; no current Root SDK5.1.1 or recovery native enrollment is inferred.


## Prepared Current-Source Acceptance — 2026-11-15

Root source8aa6f169b3e27f0a831ad6425c4bc1278ca2c445 passes the ordinary build/test and authenticated REST, persisted, anonymous UI and authenticated UI jobs. Both source-reference jobs and all Story Guards also pass. The genuine prepared REST artifact14176224661 has verified GitHub SHA2560789b064d3bb137d4eb6dd01f1dbbb285ff2895b7281c1a1932fb55c99b5269b. Its original TRX records AuthenticatedOwnerRecoveryPersistsExactSnapshotsAndPermissionRefusalDoesNotAppendEvents Passed,17.780seconds, within9/9 passing REST cases. This resolves the composed recovery proof without relabeling the local exit58 refusal. The remaining shared Aspire job, final dependency adoption and publication remain separate gates.

McpCli upstream4cf120c410a668d08c6ba70d59962c3a2c031952 integrates by fast-forward. Its existing exact-source census continuation preserves scanner/runtime behavior and disclaims individual human review, runtime, migration and publication acceptance. All222 local census refusal controls pass, and the complete17-root hosted census132929961282 passes. The remaining owning full CI must still complete. No new Root SDK/recovery native enrollment is inferred.

FrontComposer2809eb366bf0548fe97f52c529892009c1b7ce35 changes only the mutation container CPU quota from2 to4; two concurrent test servers,10GiB/no swap, both complete cohorts, thresholds80/60 and strict provenance/report validation remain unchanged. Actual prior telemetry showed CPU throttling with no OOM, while the public Ubuntu runner supplies4CPUs. Documentation-only0c2322d2b52d4a275b58214d03fb1b11c476e8cb aligns the resource description. The already-failing8ce mutation attempt43744824336 is cancelled to free its serialized slot for a complete current-source run43754386355. Its failed unmutated Happy baseline and incomplete Error progress retain original scope; no score is claimed. Queued earlier-source runs are cancelled before execution. Current-source CI, Quality, both actual mutation reports and the verified public6.0.1 patch remain required.

EventStore's Phase4 live matrix job intentionally reports FAIL: its owning Story9.2 explicitly requires truthful failure for unbound future gates, absent seals and absent registry-human attestations. Its separate fixture job passes. This evidence-only diagnostic grants no release/readiness authority and is preserved without inventing attestations or weakening checks. The unrelated actual Payload Protection test-harness framework mismatch is under repair in its owning repository.


Across the five required Root workflows on8aa6f169b3e27f0a831ad6425c4bc1278ca2c445,17 jobs succeed and the configured non-enabled performance job is skipped, with exact source/run/job identities in root-8aa-complete-hosted-ci.json. Later concurrent FrontComposer e0eef14ddbbcd03823863a4f417a4b07f6f5bafc changes only the Quality concurrency group, isolating manual diagnostics from automatic push cancellation. Parsed YAML comparison confirms all jobs, triggers, permissions and cancellation policy unchanged. It is preserved by fast-forward. Current full mutation qualification moves to43755375617 for that exact source;0c source reports cannot qualifye0. Push Quality43755163930 and manual diagnostics43755164106 retain their distinct event scope. Final public patch and catalog adoption remain pending.


## EventStore Offline Payload Fixture Repair — 2026-11-15

The actual owner Payload Protection job fails seven Python setups because six fixture implementations force Debug/source builds of frozen net10 Platform against current net11 EventStore. They now honor the existing Release/package workflow settings and retain ordinary local defaults, exact hash-pinned supplied-assembly checks, original vector assertions and fail-closed policy values. An eight-line test-only CustomAfterMicrosoftCommonProps import rebuilds the complete ephemeral component/role graph for the actual verifier runtime net11; ordinary TargetFramework globals are stripped by reference discovery and failed attempts remain retained. No dependency version, frozen source/pointer, validator, authority or attestation changes. The owning catalog retains its EventStore4.2.0/Memories3.0.27 component package view. Actual compiler outputs/runtime metadata establish the fixture target; this supplies no historical, installed or native-provider qualification.

The exact frozen Platform00ed06074f5b67522eff81dc8dae6ebd28161c36 and Builds e2bbbd0893f586e4d447ef8358ff747ea3d34435 run passes138 Python cases with one existing supplied-assembly skip. A separate actual hash-pinned producer invocation passes6 cases including that case. Seven direct C# paths pass34 cases; standalone PayloadProtection passes1245 cases and its invariant-globalization control passes1. All builds pass with zero warnings/errors. Five focused fixture-policy controls also pass independently on the parent. Original framework refusals and all unsuccessful framework-property attempts retain their own logs/inputs. All wrapper workspaces and owned work have completed cleanup. Owning-source review, commit/push and fresh hosted acceptance remain pending.


All three EventStore owner reviews finish; six blind observations are individually triaged under Review Triage Log. The only patch changes two comments from frozen to selected. The unchanged source-mode CI-marker caveat is recorded as pre-existing debt; the maintained package workflow retains CI. Parent verifies every original artifact/log hash, all8 current source hashes (only2 proven comment-only changes), cleanup and5 focused controls. Commit981fabaa3cf44fc2a71eed73549f4fb0d847e2a6 is pinned-commitlint validated, pushed, remotely verified and clean. GitHub reports actual PR-only and8-expected-check main-push bypasses for the authorized maintainer operation. Fresh required owner CI and Payload Protection43757391741 remain mandatory before release; persistent protections are unchanged.


EventStore Payload Protection43757391741 succeeds on committed981fabaa3cf44fc2a71eed73549f4fb0d847e2a6. Official artifact14178324672 verifies SHA25606636321019927004223dce6eb529a420e38ded896c5ee07b0a379340db94df0; both original hosted TRX files retain1245/1245 and1/1 passes without skips. McpCli4cf completeCI43752669619 also succeeds, including the17-root census and real retained live cohorts; it grants no new Root recovery enrollment. FrontComposer e0 pushCI43755164227, pushQuality43755163930 and separate manualQuality43755164106 all succeed. Release43757692983 authenticates source and dry-run selects6.0.1, then waits for protected preparation. Production approval remains held until actual current-source mutation/property43755375617 passes. No mutation score or patch publication is inferred.

The public-verification recipe is first replayed against actual historical FrontComposer6.0.0/source d7e8f57808b610f7cddf95ecd06db178dfd7592f. It authenticates successful candidate artifact14109303333 by GitHub SHA25652738f729894f27e70e3299a5edb1c6c6a6fede1375586e80945e99afec81f58 and exact release/tag source. All8 listed official archives pass catalogSHA512, repository source, native signatures and unchanged prepared payload comparisons. This qualifies the verification recipe against real retained inputs; it does not publish or qualify6.0.1.


EventStore981 mainCI43757392176 succeeds; separate integration live-sidecar passes while published compatibility continues. Concurrent FrontComposer origin advances through902fe13f to695d23149c56a6104e81d23a54116b4fb914df95, changing9 documentation/planning/status files and no production, test or workflow bytes. Its pushCI43759663625 and Quality43759663406 succeed. Obsolete e0 release43757692983 is cancelled before protected preparation/publication. The e0 mutation run continues to produce complete diagnostic reports; these source-specific results cannot qualify the newer main. A bounded private investigation of41 observed Happy survivors prepares meaningful parser regression controls while leaving the owner worktree unchanged. No final score, approval, equivalence or defer decision follows from partial data.


The complete unchanged e0 Error-only local diagnostic finishes in3812seconds with tool exit0,66.88percent and exact private-tool provenance exit0. Original JSON reports1647 Killed,630 Survived,186 NoCoverage,1 Timeout,717 exact nonexecuted compiler candidates and562 Ignored. All2278 planned executions and303 resolved case-ID bindings are retained. Timeout14021 removes the Pop from the namespace-append loop; final JSON explicitly supersedes partial console success counters. Strict BOTH validation exits1 for missing Happy evidence and untriaged630/186/1; no missing report or review is fabricated. Original JSON/HTML, raw/resource console and normalized inventories retain separate hashes. The wrapper is removed with no owned processes. Substantive per-ID/cause investigation proceeds before any proposed correction/triage and fresh current-source qualification; no threshold, cohort, tool or provenance gate changes.


FrontComposer hosted mutation43755375617 continues to report Happy in progress, but its fetched original log snapshot remains at22:42:23UTC for over20minutes. Observed completion reaches1012 unique IDs; no final score or runner-loss cause is claimed. A separate unchanged e0 complete Happy-only local diagnostic starts with the exact original config/tool/cohort and no private regression patch, solely to obtain complete diagnostic reports. It supplies no newer-source or hosted acceptance. The new public-verification helper retains FrontComposer's existing immutable-release policy and records Tenants' actual release metadata under its existing tag/source/archive checks; it does not invent a Tenants repository immutability setting or relax any production workflow.


### Complete FrontComposer Regression and Job-Lifetime Review

All three layers completed before any triage. The reviewed owner diff is93,082bytes/SHA2566a367debcdb107ca6e2f742155b31a924fc11fe0542e040485884463d8b8a24c on b34be9245443d21b72a6076cf205bbbc8a8c521f. Every finding is judged separately before grouping. The three pre-existing product defects retain their bounded source evidence and are deferred; no release or full mutation qualification is inferred.

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| FCL-B1: segment artifacts precede sanitization | high | Patch: new immutable per-segment uploads precede existing Normalize-ArtifactText/Test-UnsafeText. The later aggregate operates on copies and cannot repair already uploaded data; reuse the exact policy and require a successful publication guard before segment/aggregate uploads. |
| FCL-B2: nested projection generated references | medium | Defer, pre-existing: unchanged b34 AttributeParser stores typeSymbol.Name without containing-type qualification; emitters consume that model, matching the reviewed CS0246 probe. The new test promises parser acceptance only. Track full nested generation/compilation separately; no production nested-type support change is included. |
| FCL-B3: delimiter-ambiguous skill sets | medium | Defer, pre-existing: unchanged CreateSkillCorpusResourcePayload joins sorted element strings with a pipe before whole-value canonical escaping, so the two reviewed partitions collide. The public helper has no tracked production call site beyond these tests; fixing its versioned canonical representation needs a separate compatibility decision. |
| FCL-B4: default diagnostic cross-file location | medium | Defer, pre-existing: unchanged ValidateDefaultValueType takes line/column from ApplicationSyntaxReference but passes the root command filePath. A partial member in another syntax tree therefore gets the wrong path; this new single-file fixture does not introduce it. |
| FCL-B5: renderer identity/header assertions missing | medium | Patch: the new complete-contract assertion omits contractId/protocol/schemaVersion and never independently changes renderer ID. Assert those actual canonical rows and the ID-sensitive fingerprint. |
| FCL-B6: badge member-to-slot pairing unchecked | medium | Patch: independent presence of enum member names and slot constants misses a swap. Bind each actual emitted member case to its slot and verify initial unannotated cases use plain-text fallback. |
| FCL-B7: missing-date fallback assertion vacuous | medium | Patch: absence of Timestamp-based ordering is vacuous for a fixture without Timestamp. Assert its actual declaration-order iteration and compile the valid initial generated output. |
| FCL-B8: packed template version not asserted | medium | Patch: descriptor inclusion alone permits build-compatible packed version1000007 to be rewritten1000000. Inspect each included descriptor and preserve its actual original packed value. |
| FCL-B9: healthy shared descriptor only checked by name | medium | Patch: typeof(HealthyT) presence does not protect its projection/role/version/count. Compare the complete unaffected descriptor across the real edit on the same driver. |
| FCL-B10: reverse generator invalidation missing | medium | Patch: each new recovery helper performs only damaged-to-repaired generation; the same demonstrated source edit can reverse. Reuse that same driver to restore the original source and assert the original diagnostic/fallback/descriptor state returns. |
| FCL-E1: pre-normalization segment publication | high | Patch with FCL-B1 after independent verdict: the concrete new upload occurs before existing path normalization, leaving raw machine-local paths in immutable artifacts even if the later aggregate normalizes its copies. |
| FCL-V1: publication scan bypass | high | Patch with FCL-B1 after independent verdict: filed verified evidence traces the new uploads and MutationReportValidationTests privacy fixtures. Segment/aggregate publication must use the existing policy before uploading; unsafe content must refuse publication while ordinary failed-but-safe reports remain retainable. |

| FCL-P1: native Shell fixture misses extracted module | medium | Patch: parent executes the existing fixture copy pattern and actual PowerShell fails at validator line13 before manifest/report validation because only the main script is copied. Include its exact shared-policy dependency in MutationReportValidationTests and run all45 native scenarios; preserve every existing assertion. |

| FCL-P2: publication rewriter invalidates report audit | medium | Patch: the aggregate validator computes each OutputSha256, then the new publication guard unconditionally Set-Content writes the already normalized text and appends a newline. Guard publication must preserve normalized bytes on repeat and keep the audited report hash valid; exact shared normalization/scan semantics remain unchanged. |

| FCL-P3: exact triage guard must persist in owning CI | medium | Patch: existing Get-TriageCount is count-only, so new aggregate maxima could accept an unknown future mutation that fits the allowance. A matcher retained only in parent evidence would not protect later owner runs. Persist and require the exact identity/source/span/status matcher in the owner aggregate alongside the unchanged strict BOTH validator and raw segment exits. |


### Final FrontComposer Identity and Publication Review

All three final layers completed before triage on1,241,086bytes/SHA2562999a6f48c20a396b12df27917a901577c706c3c8f18324ab2d8e620e1b9c956. The serialized-JSON cases concern the new pre-publication entry point; existing plain-text function behavior alone is insufficient there. Each finding is judged separately before grouping; no threshold, cohort, tool, native credit or human authority change is inferred.

| Finding | Verdict | Route and evidence |
| --- | --- | --- |
| FCF-B1: encoded JSON credential escapes | high | Patch: the new serialized-JSON publication entry point only scans raw text. The demonstrated password\u003d value decodes to a prohibited marker; inspect decoded JSON strings using the shared policy before publication, without weakening ordinary token-reference exemptions. |
| FCF-B2: escaped Windows user paths | medium | Patch: the new raw-JSON publication path leaves doubled JSON backslashes unmatched by the shared plain-text Windows matcher. Normalize decoded string values and serialize valid JSON; preserve original native digest lineage. |
| FCF-B3: redaction corrupts JSON delimiters | medium | Patch: the raw regex consumes the closing quote/brace in the demonstrated /home/private-person scalar and the new publication guard reports success. Process JSON structurally and verify valid serialized output, retaining byte idempotence for unchanged artifacts. |
| FCF-B4: reviewed live identity becomes Ignored | medium | Patch: inspect_report treats Ignored as valid/nonproblem and accepts a known test-added identity despite no approved transition. Refuse exclusion of reviewed live identities and require a genuinely admitted resolved/problem state. |
| FCF-B5: reviewed identities can disappear | medium | Patch: the new matcher only visits supplied mutants. Strict validator file coverage/minimum1 does not prove the expected reviewed IDs occurred; enforce exact applicable identity presence and add realistic truncated-report refusals. |
| FCF-B6: new Python suites not wired into CI | medium | Patch: whole-owner runner search and the filed verified gap show quality explicitly enumerates older suites. Add both existing production-guard regression suites to required normal CI and retain actual exit enforcement. |
| FCF-B7: local replay omits exact admission | medium | Patch: the supported replay still derives its result from native segments and count-only strict validation. Include the newly mandatory exact checker and preserve every failure code; count maxima alone are insufficient. |
| FCF-B8: original native input digest lost | medium | Patch: worker normalization precedes aggregate InputSha256, which now identifies the published normalized copy. Retain a safe explicit original-to-published digest record before normalization and carry/verify it through the aggregate artifact. |
| FCF-B9: regression evidence not resolvable | medium | Patch: evidenceId plus a Boolean does not locate the actual failure. Bind each claimed control to its test method, exact command/result record and test-source fingerprint through a finite resolvable catalog; preserve both1MiB limits and avoid pretending final native outcomes. |
| FCF-B10: hash field inventory derives from equality | medium | Patch: the new test skips a field when the implementation reports every sampled value equal, hiding loss of a required equality/cache-key field. Declare the intended participating scalar fields independently and name intentional exclusions; keep collisions allowed and avoid numeric hash pins. |
| FCF-E1: Ignored is an implicit unapproved transition | medium | Patch with FCF-B4 after independent verdict: the filed exact in-memory reproduction shows the same reviewed identity accepted both Killed and Ignored, while the ledger only approves the existing live-state transitions. |
| FCF-V1: no CI runner for the23 Python cases | medium | Patch with FCF-B6 after independent verdict; accept the filed verified gap. Direct run-with-temp executes23passing tests, but neither workflow enumerates them, so normal CI misses their actual refusal assertions. |

## Complete Root Source and Evidence Review

All three independent layers completed on the full401d926eb7d51bdf332696ed3f0fe39a812b3501-to-current diff including untracked evidence:274,570,964bytes/SHA256f1c44f149ae3fad418462a150c5832fd852d4ce8d754c2038c1ae4c438fa8fd9. Verification reports no gaps. Every finding below is judged separately before grouping. Future native mutation, catalog adoption and publication remain required.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| RCB1: Malformed authoritative producer state | maybe-false | defer: Valid command/replay paths initialize Active or Disabled and validate assignable roles; the cited Unknown-role history is itself already uncertifiable before refresh. No real historical actor snapshot showing a newly lost valid certification was supplied. A production historical-state fixture plus the intended repair/rejection policy would settle this potential medium issue; do not invent a new public rejection. |
| RCB2: Projection snapshot check follows parsing | low | reject: The additional certification check really runs after the existing audit decoder. Normal production refresh already enforces the serialized bound before emission, and the retained oversized legacy fixture only demonstrates bounded parsing, not a resource failure. Moving the check before the audit decoder would add a new legacy-history rejection/skip policy; an uncommon low-impact concern does not justify that branch. |
| RCB3: Optimized Python removes release assertions | medium | patch: The new public-release verification recipe uses assert for its input, source/tag, workflow, artifact hash, package and signature-result gates. Inherited PYTHONOPTIMIZE or python -O removes them. Replace them with explicit failures and verify an optimized interpreter still refuses invalid input. |
| RCB4: Generic artifact has no producing-attempt proof | medium | patch: Tenants uploads generic release-candidate; the helper currently records the run latest run_attempt without identifying its producing job. Bind generic artifacts to an actual successful prepare-release job in an exact attempt, or refuse ambiguity; do not label a stale artifact as a later successful attempt. |
| RCB5: Evidence reuse does not check retained logs | medium | patch: reuse verifies revision/manifest/exit codes but accepts missing logs and arbitrary log_sha256. Resolve safe contained evidence paths and require real retained byte hashes before reusing either successful check. |
| RCB6: Evidence reuse omits recipe and commands | medium | patch: The source manifest excludes the root verification recipe; reuse can retain checks after configuration/filter changes. Require expected exact commands/configuration and a matching recipe identity, preserving original evidence and refusing older incomplete records. |
| RCB7: Dapr policy evidence regression is absent from CI | medium | patch: All three relevant workflows were searched: only test_*release*.py is discovered; no runner selects test_dapr_read_policy_evidence.py. Add the existing passing single regression to the required Story Guards lane. |
| RCB8: Historical SDK replay description overclaims reproduction | low | patch: The historical helper requires SDK5.1.0 still to be latest while copying current source. Its README advertises reproduction without that condition. Correct the description to state that it re-executes current inputs under an intentionally stale 5.1.0 freshness condition and will refuse after5.1.1, directing current qualification to the fresh receipt. |
| RCB9: Operational state retains conflicting current fields | low | patch: The state current top-level FC fields still describe pending/uncommitted work and old job/release IDs while newer nested current records identify committed11c and green CI/Quality. Reconcile current fields and mark or move historical observations explicitly without changing original standalone receipts. |
| RCB10: Generated HTTP gate remains at66 for67 cases | medium | patch: The current selected suite has67 cases including recovery, while its required CI floor remains66. Raise this ordinary exact-profile floor to67 or explicitly require the added recovery case. |
| RCE1: Parent replay drivers overwrite prior output | medium | patch: Both resumed-parent-root-verification/verify.py and resumed-parent-packed-verification/verify.py use fixed original OUT paths and w writes. Give each replay a unique retained output directory while preserving existing original results. |

## Current FrontComposer Source and Completed Root Review Patches

FrontComposer27 reviewed files are committed and remotely verified at11c8377a858a756c4e4069bf08e62db1f58e1733 after pinnedcommitlint21.2.4 passes with0problems/0warnings. CI43807620376, Quality43807619578(all8jobs), CodeQL43807620044 and Commitlint43807620102 succeed on that exact source. The actual lifecycle artifact14199108268 matches GitHub SHA2562021139a4fc7193fa5b2800c0ae5352c20b7ee902dc08dca5a37628120dfc10f; original TRX passes1/1 and replay evidence records10,000sequences. Full native mutation43807658283 still has both complete segments in progress. Release43810326251 verifies exact source and plans6.0.1, then waits at protected preparation; no approval or publication is performed pending actual full native qualification.

All nine Root review patch findings are corrected through eight bounded groups. Parent independently reruns18focused Python cases and bothactionlint checks successfully against the12retained final input digests. Explicit public-release failures remain effective under optimized Python; generic candidates require an actual successful producing job and exact attempt/time window. Builds receipt reuse requires source/revision, Debug configuration, self-bound recipe, exact commands, contained distinct real logs and matching hashes. Replay outputs are exclusive; original receipts/logs remain unchanged. Existing Dapr policy evidence is now in Story Guards and the current generated HTTP minimum is67. Historical5.1.0 qualification wording states its obsolete freshness precondition. No production C# changes or new native authority are inferred.

The hardened public verifier replays actual6.0.0 candidate14109303333 and verifies all8official packages, including actual scoped producing job132411943902/attempt1. A separate generic Tenants control proves actual candidate14170533309 originated from successful prepare job132878386126/attempt1; its overall run remains cancelled and correctly receives no release qualification. New18case/unit, source/audit and real positive replay receipts remain separate from earlier helper versions. An exact compressed archive retains1,327ignored original task command logs (37,564,732bytes), with per-member hashes and verified round-trip bytes, without changing ignore policy or overwriting originals. Final catalog adoption, source-exact Root hosted gates and actual five-package publication remain pending.

Current11c Error mutation job133102010838 completes successfully on2026-11-16. Artifact14205209896 matches official SHA256a32d201263365aeaab137c766b12a0fdb91140866fb3dd2714faa90fd84b4f6d. Its original JSON records1909Killed/465Survived/89NoCoverage/1Timeout/717CompileError/562Ignored and recomputes77.51623376623377percent, above60. Current owner exact-identity inspection admits all555remaining reviewed problems with0failures and preserves717conditional native invalid candidates. Hosted private-provenance and publication-guard steps pass. This is Error-only evidence: Happy80percent and the unchanged independently built strict BOTH/digest/identity aggregate remain pending; protected6.0.1preparation is still held.

## Concurrent Upstream Continuation and Current Gate Repairs

The complete11c Happy job succeeds with2006Killed/400Survived/57NoCoverage/1Timeout,81.45292207792208percent above80, and0finiteidentity failures. Error remains77.51623376623377percent above60. The independently rebuilt aggregate verifies real private provenance, worker digests and exact identities, but fails only strict target inventory for3implicit enum sources. Correction to earlier wording: both original JSONs contain those3file/source entries with mutants:[]; the inventory counts no active mutation for them. The immutable original reports are unchanged. Finite non-mutatable enum accounting is prepared without changing mutate predicates, thresholds,717invalid or562Ignored identities.

Concurrent FrontComposer main advances toebd3f76dc0b320d455917c593ff86526b58d0c77 through planning records and9nested gitlink selections; all27earlier production/test/tool/workflow repair bytes are preserved. Current CI43834275373 and Quality43834274896 correctly refuse stale Platform projection seals. Parent cancels stale11c Release43810326251 before approval/publication. Four exact source-seal fields now match selected Platformead9/Builds65fd objects. The newly exposed legitimate current catalog has exact package-only4.2guard/default5.1.1shape; the bounded reader addition preserves historical5.1.0/two-selector paths,40/33/3role sets, allclosed proofs, tuples and evaluator authorities. Real selected65fd validation changes from retained refusal to success;36methods/374subcases pass. No current-source native or migration approval is inferred. Parent independent review, seal/activation commits and fresh exact-source qualification remain pending.

Top-level upstream histories are preserved by fast-forward for AI.Tools7ef, Buildsfdda, EventStore97c, Memories65b and Platformead9, without nested initialization/update. Existing Root source/profile Python controls pass against them. McpCli incomingmainfbe includes an enrollment requiring Projects.Contracts2.0.0-story66.3; a fresh officialNuGet index still contains only1.0.0/1.2.x, and incomingCI failsNU1102. This release therefore retains already-qualified4cf as an explicit compatibility source hold, with allnewcommits preserved on local/remotemainfbe; no package relabeling or new census approval occurs. EventStore97c has one actual blocking secret-content failure at3newfixturelines. Three fixture-only repairs use a runtime privacy canary and explicit inert placeholders; the secret scanner remains unchanged and focused verification is running.

## Current FrontComposer Gate Repair Review

All three independent layers completed on141,422bytes/SHA2566e3ac0e85526c3ff788fe945d7501f2585fd7c27e86a111570b66e7a0036850a. The verification layer reports no gaps and independently runs15focused tests. Each finding is judged separately before grouping; original approvals and current pending source gates are unchanged.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| FCG-B1: Native enum exception lacks a source binding | medium | patch: The native validator admits the three exact enum paths solely by owner/rationale. The reviewer changes a declaration to an explicit ordinal and retains empty reports; the validator still succeeds. Bind these three finite non-mutatable exceptions to canonical source hashes and enforce them at native admission; leave existing other exceptions and mutation predicates unchanged. |
| FCG-B2: Active reported enum results lack a distinguishing control | low | patch: The new tests exercise empty enum entries, implicit declarations and unrelated executable drift. Add a focused control that active results in one of the same exception paths remain in native report audits/live counts and untriaged problem refusal. No existing runtime filtering defect or separate synthetic raw score is claimed. |
| FCG-B3: Historical16.2 validation is not a current combined-change run | false | reject: The retained command belongs to the separate already-completed upstream16.2 tracking closure. Its refusal when unrelated current gate-repair files are present is correct scope enforcement, not invalid historical evidence. The current repair is owned by this build, and a separate current-owner activation record is planned; never edit the older16.2 File List or relabel its past command as current verification. |
| FCG-B4: Reopening fails to reset EVT-XFER-1 tracking | medium | defer: The upstream epic context reopens16.2 and its row but leaves approved-change EVT-XFER-1 explicitly not-required. An operator following those future steps gets inconsistent active-transfer tracking. This is a pre-existing upstream documentation/process issue, not caused by enum/catalog repairs; original human records remain preserved. |
| FCG-B5: Missing receipt allegedly admits nonoperative receipts | false | reject: The same context defines missing/stale/role-inferred/tuple-mismatched receipts as invalid and separately requires valid transfer/original-maintainer evidence in the reopening paragraph. The old C records are explicitly nonoperative and cannot satisfy the exact tuple being evaluated. Literal file existence is not the operative condition claimed by this finding. |
| FCG-B6: Fixed-M historical procedure needs another supersession notice | false | reject: The procedure is already conditional on exact live-main/candidate/deadline checks and explicitly stops on drift. The later context and current build record preserve observed drift; the old exact-M procedure grants no execution authority on ebd. Its intended fail-closed behavior disproves the unsafe-current-execution claim. |
| FCG-B7: Historical backlog comment allegedly conflicts with current done state | false | reject: Older dated preparation comments retain not-invoked/backlog history. The later authoritative not-invoked record explicitly supersedes only that tracking disposition, preserves earlier text, and the current YAML row is done while16.3/G-3 remain open. Context links the owning accepted record. No current semantic state or approval conflict is demonstrated. |
| FCG-B8: PO decision lacks an extra retrieval-coordinate schema | false | reject: The repository receipt form is a dated owning record and the actual quoted decision; no examined rule requires an additional conversation/message-ID schema. The upstream record preserves the bounded observation and exact decision without inferring another role or operative migration approval. Do not invent missing coordinates or rewrite the preserved human record. |
| FCG-B9: Complete selected-catalog verification allegedly absent | false | reject: The separate actual real selected65fd whole-catalog --policy validation is retained before/after reader changes, with source hashes and native MSBuild corroboration. Normal graph/semantic gates read the committed selected complete catalog. The XML fixture itself asserts no hash-based authority and is only a unit input. Existing full-input verification disproves absence. |
| FCG-B10: Dependency ranges are not readable in the review diff | low | patch: The nested working trees are intentionally uninitialized; normal submodule diff cannot inline their source. Supply immutable old/new ranges and source summaries from existing top-level/sibling object stores, keeping selected catalogs/canonical source hashes and existing hosted scopes explicit. No nested initialization or producer qualification is implied. |
| FCG-E1: EVT-XFER-1 remains not-required after reopening | medium | defer: Independently verified same upstream reopen procedure asFCG-B4: its steps update16.2 but omit approved-change reset. Same pre-existing root cause; retain this separate verdict then group one deferred item. |


## Pragmatic completion steering (2026-11-16)

The user asked whether the release process is overengineered, has real ROI, and can be simplified. Completion now freezes the selected owner revisions instead of chasing further upstream advances. Finish demonstrated fixes with focused verification and retain existing required CI, Quality, security, provenance and public-package checks. Do not launch another full mutation sweep or redundant review round for the finite validation changes. Original native run43807658283 remains source11c with successful segments and failed aggregate; no evidence transfer or reclassification. Broader CI migration and scheduling mutation testing separately are recommendations, not implemented pipeline changes.

FrontComposer source `a03ee98a8964d56506160bcd9c73de992df56f58` commits the seven reviewed seal/reader/enum fixes. EventStore source `25cde2065c6f8f919cebc17e067bea5f5953980f` commits three safe test fixtures;286 existing secret controls,25 diagnostics and2 focused Python checks pass. Both exact commit messages passed pinned commitlint21.2.4. Neither new source is pushed. The newly synchronized shared baseline requires a local checker that refuses the absent nested Builds checkout and assumes an unadopted CI configuration. The user prohibits nested initialization. A scoped exception request is pending; preserve the current existing automated gates and do not push until resolved. Exact refusals and review/test packets are retained under `resumed-parent-integration/`. Root release remains incomplete.


## Direct Root release decision (2026-11-16)

The pragmatic completion audit removed an unnecessary parent-imposed FC6.0.1 prerequisite. Frozen intent requires the published.NET11 generator and five verified Tenants packages; the generator prerequisite was already met by publicFC6.0.0, whose eight official archives/source/signatures/prepared payload passed independent verification, and by prior Root package UI/Integration/generated-HTTP checks. Root production/configuration bytes are unchanged from green8aa; subsequent FCf263 changes since green9ea are documentation only. Prior receipts retain exact original source attribution, and final Root source/package CI must independently pass. Keep Buildsfdda and its qualified public600 default/audit; no new catalog family change is needed. Preserve all pushed owner fixes, existing automatic owner checks, historical native failure and specialized authority boundaries. FC601 remains its own future owner release. A pause in other FC writers is no longer needed to start final Root CI/publication. See resumed-parent-integration/pragmatic-root-release-decision.json.


## Approved pragmatic stop conditions (2026-11-16)

Apply the approved sprint-change-proposal-2026-11-16-pragmatic-release-policy.md4A/4D. Done when each selected pushed owner source has green required push CI (and Quality where applicable), the final frozen Root gitlink/source commit passes exact pinned commitlint and required Root CI, the existing release workflow publishes the intended source, all five official NuGet archives verify version/source/provenance, and one concise release-report-<version>.md exists. Later upstream work, new full mutation sweeps, additional reviews, CiCd adoption and new evidence-qualification CI steps are excluded. Source holds preserve all other user work on existing main branches.

Freeze FrontComposer at9ea805dc7bbdf048b282e66e94115add295099a3, whose actual CI43856960601,Quality43856959801(all8),CodeQL43856960406 and Commitlint43856960438 succeeded. Its main branch and later documentation remain preserved. The extra proposed601 publication is unnecessary because verified public600 already satisfies Tenants' package dependency. EventStore8b6d5bee5217b3f4bbd8fd689ad887aca863961b fixes the demonstrated source scanner blocker; wait its existing required CI. Keep McpCli4cf qualified source and the other already selected revisions. No specialized migration/census approval is inferred.

The new Dapr read-policy step is evidence-byte qualification only, as its existing test docstring states. Remove that newly introduced step and retain the existing test and all original8aa release gates; pipeline qualification expansion is deferred under approved4A. The generatedHTTP floor67 remains.

An intermediate push4b99a89c78c57294b99b52eaf5889e8c74b87999 included bulk diagnostics before the concurrent approved policy was read. A forward corrective commit removes newly introduced diagnostic captures from the tracked release tree while preserving their exact local bytes and public history. Only the required public verifier helper and its regression test sources remain as new verification code. The existing historical evidence remains. Final publication uses the corrected source, never the intermediate snapshot.

## Release completion (2026-11-16)

Published [Tenants 8.0.1](https://github.com/Hexalith/Hexalith.Tenants/releases/tag/v8.0.1) from `ab67fd6bc308b5a641b0ca398d776e347193c168` through successful release run43880703312. All five required source workflows passed on that exact commit:17 successful jobs and one existing configured performance skip. Selected EventStore required checks passed, with its failed live job retried successfully on the same source; the underlying Development port-allocation issue remains deferred. Independent official NuGet verification passed for all five packages, including listed version, repository commit, official SHA512, native signatures and candidate payload equality. Both existing container destination checks passed. See [the concise release report](release-report-8.0.1.md) for frozen revisions, run links, exceptions and remaining deferrals. The completion documentation commit does not change the published source or tag. Bulk diagnostics remain local, and historical failures and separate migration authorities retain their original scope. The scoped checker exception expires when this completion bookkeeping finishes; other FrontComposer pushes may resume.
