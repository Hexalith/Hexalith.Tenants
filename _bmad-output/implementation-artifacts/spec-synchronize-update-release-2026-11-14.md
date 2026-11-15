---
title: 'Synchronize dependencies and publish the .NET 11 release'
type: 'chore'
created: '2026-11-14'
status: 'in-review'
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
- `_bmad-output/implementation-artifacts/evidence/fix-builds-sdk-cohort-fixtures-2026-11-15/`
- `_bmad-output/implementation-artifacts/evidence/fix-firefox-route-focus-ci-2026-11-15/`
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

#### Remaining bounded patch tasks

- [x] UI audit support — add TenantAccessRefreshed to the existing administrative event, receipt and EN/FR mappings, with a projected-row-to-gateway/receipt regression. Keep unknown events/category mismatches fail-closed.
- [x] Projection/testing support — reject malformed new recovery snapshots for completeness certification with existing SDK limits; include the successful recovery event in tenant-isolation helpers and cover both behaviors.
- [ ] Authenticated recovery proof — exercise the existing composed live gateway and persisted backend for a current owner, denied permission and disabled tenant, with real stored event assertions and owned cleanup.
- [x] Verification/docs corrections — preserve phase logs on source drift with attempt-specific paths, make the evidence script portable, enforce the readiness request's real deadline, clarify the strict current versus frozen historical consumer inventories and correct the upgrade guide default.

These are direct corrections within the approved current-source release intent. Preserve every prior successful change, source-specific receipt, baseline, immutable historical inventory, authority boundary and refusal control. Read the ordinary shared state instructions before projection/persistence work and UX instructions before UI work. Parent owns integration and complete verification after the implementation subagent returns; it continues to own all Git, remote CI and publication.



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
