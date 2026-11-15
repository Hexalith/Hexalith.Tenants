---
title: 'Preserve sample background delivery in the canonical CI topology'
type: 'bugfix'
created: '2026-11-03'
status: 'done'
resolution: 'superseded'
closed: '2026-11-15'
superseded_by: 'spec-synchronize-update-release-2026-11-14.md'
route: 'dispatch'
baseline_commit: 'bcd923781e97cf7ea6391f73eb0b3d5469c254c4'
gitlink_audit_ref: 'ae40c9657175ae5567cdd182384d3840af64d8a2'
context:
  - '/home/administrator/projects/hexalith/tenants/AGENTS.md'
  - '/home/administrator/projects/hexalith/tenants/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/tenants/.agents/skills/aspire/SKILL.md'
---

<frozen-after-approval reason="User authorized fixing all CI failures, unattended execution and release">

## Intent

Retain all seven existing AspireTopologyTests in the prepared canonical catalog CI lane, including actual sample liveness and background event delivery that grants then revokes sample access. Generic CI cannot start an unconfigured catalog topology, so it relocates this complete class to a fresh prepared backend. The prepared profile currently omits Sample and disables workers, making two of the seven relocated tests incompatible. Compose their actual required topology and reuse the real owner bootstrap without weakening any existing security, projection, command-identity, audit or refusal assertion.

## Boundaries & Constraints

Keep the default focused catalog profile's Sample omission, existing four required owner identities, generated app-channel authentication, catalog guards, custody proof and cold-bootstrap checks. Sample is only an optional pub/sub consumer, never a pretend owner/receiver. Never fabricate authority or BootstrapGlobalAdmin success, submit an internal command through a human route in the canonical profile, skip existing tests, remove tests from CI, warm/retry a failed test into passing, or change production authorization/deadlines. Preserve concurrent root and submodule work. No commits, staging, pushes, nested initialization or reference skills. Do not launch overlapping private runtimes; root owns isolated live/current-source CI qualification. Ordinary builds and offline AppHost model tests are allowed.

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.AppHost/Program.cs` around Sample creation currently includes the actual generated-token Sample domain module only when TENANTS_SOURCE_CATALOG_PROFILE is false. Add explicit opt-in TENANTS_SOURCE_CATALOG_INCLUDE_SAMPLE=true: include when profile is disabled OR flag true. Preserve existing AddEventStoreDomainModule, pub/sub/reference/token/security wiring exactly.
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs` Resources currently omits Sample whenever IsSourceCatalogProfile. Match the same include predicate. Keep the native transport/key modifications from the preceding repair and its existing APIs intact; only add the Sample inclusion seam. AppHost inherits the flag through existing environment/configuration; never place keys in arguments.
- `src/Hexalith.Tenants.AppHost/TenantsDevelopmentRoutingCatalog.cs` explicitly declares four owner receivers. Do not add Sample there. SDK development composition inventories all actual Dapr project AppIds while RequiredHostIds and catalog authority component scopes come from receivers; topology validation allows required hosts to be a subset of selected apps. Sample's Program uses AddHexalithTenants, sidecar channel authentication and MapEventStoreDomainEvents; it has no DomainRouteCatalogFilter or owner authority. Its direct access-check HTTP endpoint reads the local subscribed projection.
- `.github/workflows/ci.yml` persisted row now selects the full AspireTopologyTests class, minimum7 and fail-skips. Add step-scoped env for only that full-class invocation: TENANTS_SOURCE_CATALOG_INCLUDE_SAMPLE=true, EventStore__Projections__WorkersEnabled=true, TENANTS_SOURCE_WORKER_BACKED_RESPONSES_ENABLED=true. Step env must override the authenticated preparation script's GITHUB_ENV false for worker-backed responses. Other REST/UI rows remain request-path-only. Do not modify generic runner exclusions; their relocated classes now retain complete coverage.
- `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` Aha test currently submits BootstrapGlobalAdmin directly even in the catalog profile, and uses tenant-scoped token for command ingress. Mirror the existing Generated_tenants_api... test's profile branch: use commandToken=sourceProfile?CreateDemoJwt():token for command submissions and the actual TenantCommandGateway statusClient; retain tenant token for tenant-scoped audit reads. In source profile require the existing fixture-proven owned bootstrap (BootstrapCommittedSequence positive and first sequence when cold required) rather than submitting a second internal command. Preserve the original non-source bootstrap behavior and all downstream committed event, proof, projection, access-grant/removal and audit checks.
- Existing offline tests: `tests/Hexalith.Tenants.AppHost.Tests/AppHostSecurityResourceGraphTests.cs` checks focused profile omission and app-channel wiring. Add the matching explicit-inclusion case and verify the Sample retains its generated token/shared pubsub wiring. Extend relevant existing fixture isolation/source-shape checks only where they protect the actual new configuration boundary.
- Aspire guidance was read through `aspire docs search "conditional resources configuration"` and `aspire docs get advanced-testing-scenarios`: configuration-controlled resources are documented. `aspire ps --format Json` returned[] before this work. No unfamiliar resource API or dependency is needed; reuse the existing builder chain.

## Tasks & Acceptance

- [x] Implement explicit Sample inclusion while preserving ordinary and focused defaults. Given canonical profile alone, Sample stays absent; with explicit inclusion, the real Sample module and authenticated sidecar exist without entering required catalog owner receivers.
- [x] Keep full7-case CI coverage and enable actual background workers/responses only for that prepared test step. Preserve all other gate/minimum/skip settings.
- [x] Adapt only the Aha test's bootstrap and command-token setup to its actual canonical runtime. Require the fixture's proven committed cold bootstrap; keep every existing grant/revoke/status/audit assertion and real pubsub flow.
- [x] Run narrow offline resource-model and fixture setup tests plus compilation against independently verified public EventStore4.0.3. Report exact commands/counts/logs and any incomplete live checks. Do not claim runtime acceptance from offline model assertions; root must verify all seven cases on the actual fresh CI backend.

## Implementation Notes

- This repair follows a read-only failure audit, not a weakened gate. The previous generic lane's24 topology initialization failures prevented these bodies from running. Their coverage must be retained in a coherent prepared topology.
- Existing metadata/native helper changes in AspireTopologyFixture and AuthenticatedRouteFixtureIsolationTests belong to another completed handoff; inspect and preserve them before editing. Root handles final reviews, source gates and release publication.

## File List

- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.Platform`

- `references/Hexalith.FrontComposer`
- `references/Hexalith.McpCli`
- `references/Hexalith.Memories`

## Verification

- Concurrent implementation retained in cea24f9a2cc513beef36d42805634e21ffe217a4: actual Sample opt-in, matching fixture resources, step-scoped workers/background response settings, and owner-bound bootstrap/command-token setup. No required catalog owner was added.
- Package-mode Release AppHost.Tests build: zero warnings/errors (3.44s; `/tmp/tenants-sample-403-apphost-build3.log`). IntegrationTests build: zero warnings/errors (4.63s; `/tmp/tenants-sample-403-integration-build.log`). Both use independently verified public EventStore4.0.3.
- Root independently ran `dotnet test tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj --configuration Release --no-build --no-restore --filter-method '*SourceCatalogProfile*' --minimum-expected-tests 2 --fail-skips on`: both actual offline profile-model tests passed, zero skips. These tests establish focused omission and explicit Sample inclusion/channel wiring, not runtime acceptance.
- Full non-topology integration runner passed609 cases with zero failures/skips in185s (`/tmp/tenants-package-403-aspire-tier.log`). The complete24 existing live topology cases remain scheduled in their prepared matrix; all seven AspireTopologyTests must still pass on the actual fresh backend. Latest exact-source main CI41570880671 at cea24f9a2cc513beef36d42805634e21ffe217a4 is running.

## Historical Closure — 2026-11-15

Closed as superseded under the user’s instruction, "close obsolete specs". The active successor is [Synchronize dependencies and publish the .NET 11 release](spec-synchronize-update-release-2026-11-14.md). The original intent, checklist, review findings, baselines and verification notes above remain historical. The `done` status records administrative closure of this superseded plan; unchecked historical tasks are not asserted to have passed.

The Sample opt-in, worker settings and owner-bootstrap changes were already implemented in the retained historical source. Their remaining hosted acceptance belongs to the successor’s complete persisted/topology CI lanes rather than a new EventStore 4.0.3 repair.

Remaining qualification: Keep the complete Sample/background topology coverage, real grant/revoke and pub/sub persisted behavior, authentication and skip refusals in the current release gates.

This closure grants no new technical acceptance, policy exception, source classification, Git operation or publication authority. Existing deferred records remain unchanged. [Closure validation](evidence/obsolete-spec-closure-2026-11-15/validation.json) retains the pre-existing historical gitlink-audit failures separately from the documentation checks; no baseline or gitlink audit metadata is rewritten.

## Completion Notes List

Historical audit reconciliation (2026-11-15): `ae40c9657175ae5567cdd182384d3840af64d8a2` is the latest committed story-associated pointer event before the current documentation-only repair. The unchanged baseline is `bcd923781e97cf7ea6391f73eb0b3d5469c254c4`. The complete baseline-to-endpoint inventory below includes incidental workspace dependency movement; it records historical range contents and grants no runtime, publication, enrollment or maintainer approval. Original implementation notes, checklists and source-specific receipts retain their original attribution.

| Dependency | Historical baseline | Historical audit endpoint |
| --- | --- | --- |
| `references/Hexalith.Builds` | `70850b18e0d0b80facd2df0809974020dd935732` | `725754075e1d6369430a3adf94d46fb71e6eaf9e` |
| `references/Hexalith.EventStore` | `f06abada8dc3aab4a5fd240847fd680d515c98a5` | `16dff1f018e8a68a9d244975e8228d96429eb61c` |
| `references/Hexalith.FrontComposer` | `f842711d67004200e3248b3c42b7d9bcb2146907` | `b105c846c31844441f1cff84b5c187da0eff3546` |
| `references/Hexalith.McpCli` | `edaecb04b1a8edf78f6ad28e5bf253db594003f4` | `58afd4766849e33e75471c9047ee945025ca3b6e` |
| `references/Hexalith.Memories` | `d9d61d8a95c7dc8827acf68ae723859711703729` | `d959bdae5af9c22f53573eda71c091cb5c0aae4c` |
| `references/Hexalith.Platform` | `1f5958e24d1b414d8956d079270f89556e23415d` | `8432397c66ebc0fd22e5671ce620a7d2139f7453` |
