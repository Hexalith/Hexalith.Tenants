---
title: 'Run unchanged McpCli vectors through the canonical native runtime'
type: 'bugfix'
created: '2026-11-03'
status: 'ready-for-dev'
route: 'dispatch'
baseline_commit: '31aa26ccf4af260930143b6929dbcd6e215c5db8'
owning_repository_baseline_commit: 'edaecb04b1a8edf78f6ad28e5bf253db594003f4'
context:
  - '/home/administrator/projects/hexalith/tenants/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.McpCli/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="User authorized all necessary CI repairs, current packages, unattended publication and human-validation bypass">

## Intent

Replace McpCli's incompatible frozen live runtime with the actual canonical EventStore4.0.4/Tenants7 runtime, preserving unchanged21 vectors plus smoke, native authorization, writer activation, persisted effects and owned cleanup. This is test infrastructure work. Artifact approval remains separately gated on actual public Tenants7 bytes; generic code and offline tests may be prepared before that publication.

## Boundaries & Constraints

No production src module-specific logic, vector rewrites, reduced modules/cases, skips, false acceptance, weaker validators/catalog/loader checks, source-mode or compile-symbol tricks, cross-request authority cache or retries. Keep Testing.Integration3.128.4 as the independent harness and Parties.Contracts1.5.1/Aspire2.0.0 approval. Preserve concurrent pins/policy/README edits. No staging, commits, pushes, local nested-submodule initialization/update, or local business runtime launches. CI may select exact qualified source commits in its own disposable root-declared inputs, with provenance checks. Root owns source publication, approval and remote live CI. Never expose keys, bearer tokens, raw private custody or operational user state.

</frozen-after-approval>

## Proven Findings and Code Map

- Owning repository is references/Hexalith.McpCli. Read its tracked build/config/architecture/test guidance and tools/live-topology/README.md. Existing runtime is tools/live-topology/ci.py→prepare-ci.sh/run-ci.sh→source builds→gateway_package_provenance→restored_cohort. It forces3.128.4. That archive's validators reject platform:tenants before dispatch; unchanged Tenants7 vectors contain45 platform calls and14 explicit tenant calls. Neither relaxing MVC validation nor using the old recorded source can produce a valid canonical runtime.
- tools/live-topology/ci.py, tests/Hexalith.McpCli.AppHost/{Program.cs,TopologyConfiguration.cs,appsettings.json,DaprComponents}, EventStoreGateway.TestHost/{Program.cs,Configuration} and Integration.Tests/{AspireTopologyFixture,AdministratorAuthorityBootstrap,ProjectionWriterBootstrap,GatewaySmokeProbe}.cs own the relevant composition/provenance/token/barrier paths. Preserve old isolated-runtime sentinel, fresh backend and pre-application Redis backup before any catalog/administrator/writer mutation. Inspect existing receipt invalidation and cleanup before changing them.
- Select all actual runtime projects against public4.0.4. Its14 archives are verified in /tmp/tenants-release-20261103-07JciR/eventstore-4.0.4-root-agent/verification.json, source326bfef884543cdbcaf96de7f4c4ad378dbad6d7. Gateway and Tenants source selection must bind exact authenticated public archive repository commits, not moving branches or cached local7 candidates. Current local global7 archive is an unpublished old candidate; final restores need a fresh task-owned NUGET_PACKAGES consistently inherited by subprocesses. Do not restore unpublished7 or write approval claims before root supplies public verification.
- Parties has a separate clean owning checkout at /home/administrator/projects/hexalith/parties. Its actual exact4.0.4 qualification is completed locally:41 controls,41 routes/41 operations/28 adapters, real native/Contracts/adapter builds with public4.0.4 plus retained Tenants5.11.2/Memories3.0.11. Root is committing/reviewing this owner repair; exact committed source will be supplied in /tmp/tenants-release-20261103-07JciR/parties404-committed-source.json. Implement generic selection/negative tests now if that receipt is not ready, then bind its real full SHA before final CI; ship no placeholder. Do not alter owner code or default pins from McpCli. Select actual qualified Platform preparation APIs too; root's qualified Platform revision is ee6f477c595defb79e17ca6ea3e5790de24c114e.
- AppHost source root checks already require declared, real, unlinked Git checkouts but intentionally do not equate their current checkout with a gitlink. Extend the explicit source/cohort receipt coherently for package-aligned disposable CI selection. Verify repository identity, exact source revision, clean owned source and actual artifact/closure hashes; do not silently ignore arbitrary source drift or edit local nested checkouts. Tests of selection must use synthetic repositories/process mocks and explicit rejected identities.
- Reuse shared AddEventStoreDevelopmentRouteCatalog/DevelopmentRouteCatalogCompositionOptions, actual SDK model observer/bootstrap coordinator and existing Platform catalog-builder preparation as used in root src/Hexalith.Tenants.AppHost/TenantsDevelopmentRoutingCatalog.cs and PartiesDevelopmentCatalogBootstrap.cs. Root's4-owner shape is not a universal requirement; compose the actual required hosts for this test topology and verify every real readback. Keep composition alive to application shutdown, then dispose it. Generate private digest custody only for the proven new isolated backend, bind real peer identities, perform prepare/ready/commit, and retain source-bound sanitized evidence.
- SDK PrepareEventStoreDevelopmentOwnerAsync requires both split artifacts inside a host's real deps closure and verifies byte-identical SDKs. Tenants Contracts/Server fits. Parties adapters deliberately live separately in EventStoreExtensions; preserve that graph and use its existing separate-artifact Platform preparation path. Never add fake host edges or bypass loader provenance checks.
- Register the native gateway catalog/technical receiver path in the actual package test host; package version changes alone do not activate authority. Supply Tenants' existing owner-bound bootstrap and proof settings. Use the real committed startup bootstrap and persisted administrator barrier; a human/global_admin claim cannot bootstrap or substitute for authority. Update bootstrap read to platform:global-administrators and smoke list read to platform:tenants. Mint signed fixture identities with eventstore:surface, exact required platform permissions and exact normal-tenant grants, omitting reserved system grants. Preserve401 for anonymous/forged credentials and all writer/persistence assertions. Stored system namespaces/event topics are not public scopes and must not be mechanically rewritten.
- Preserve other approved source dependency selections and the separate legacy harness. Refresh Gateway.TestHost public configuration snapshot/provenance from actual4.0.4 archive metadata rather than hand-authored substitutes. Update current topology docs truthfully; retain historical receipts. Coordinate README regions with the separate enrollment work.

## Tasks & Acceptance

- [ ] Given valid public package/qualified owner source inputs, selection/receipts bind exact revisions and actual SDK bytes; missing, dirty, wrong-repository, mismatched and local-candidate inputs refuse before mutation.
- [ ] Given the existing isolated fresh backend/backup proof, actual catalog preparation/activation and owner-bound bootstrap produce matching participant readbacks and persisted administrator authority, with no synthetic runtime evidence or weakened guard.
- [ ] Given unchanged vectors and native scopes, smoke/bootstrap/token/writer paths use canonical transport identities while retaining negative auth, every21 vector selection and persisted effect. Original operation/vector bytes remain unchanged.
- [ ] Run focused offline Python selection/provenance/plan tests, individual builds/preflight/discovery as public dependencies permit, and exact21-plus-smoke checks. Do not start local business resources. Report package availability blockers separately; root must prove full remote live acceptance.
- [ ] Save changed files, exact commands/results, hashes, selected source/SDK identities, remaining dependencies and limitations in /tmp/tenants-release-20261103-07JciR/mcpcli-native-runtime-handoff.json.

## File List

- references/Hexalith.McpCli
- references/Hexalith.EventStore
- references/Hexalith.Memories
- references/Hexalith.Platform

## Verification

Implementation pending; actual public Tenants7/source receipt and complete remote live acceptance remain external prerequisites.


## Coordinated Source Inputs

The concurrent root dependency synchronization131966a5546efa8f5495ba73fcb409dab3d2eb74 moved EventStore/Memories/Platform gitlinks; these incidental paths are declared above without asserting that moving source is public-package proof. The native runtime continues to require exact public archive provenance. Qualified Parties source13a4f913f2f0177a30291091cbc8893202e4622d is committed/pushed; receipt: /tmp/tenants-release-20261103-07JciR/parties404-committed-source.json. Actual public Tenants7 remains pending.
