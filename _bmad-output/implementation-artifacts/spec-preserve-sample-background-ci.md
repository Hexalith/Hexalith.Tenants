---
title: 'Preserve sample background delivery in the canonical CI topology'
type: 'bugfix'
created: '2026-11-03'
status: 'in-progress'
route: 'dispatch'
baseline_commit: 'bcd923781e97cf7ea6391f73eb0b3d5469c254c4'
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

- [ ] Implement explicit Sample inclusion while preserving ordinary and focused defaults. Given canonical profile alone, Sample stays absent; with explicit inclusion, the real Sample module and authenticated sidecar exist without entering required catalog owner receivers.
- [ ] Keep full7-case CI coverage and enable actual background workers/responses only for that prepared test step. Preserve all other gate/minimum/skip settings.
- [ ] Adapt only the Aha test's bootstrap and command-token setup to its actual canonical runtime. Require the fixture's proven committed cold bootstrap; keep every existing grant/revoke/status/audit assertion and real pubsub flow.
- [ ] Run narrow offline resource-model and fixture setup tests plus compilation against independently verified public EventStore4.0.3. Report exact commands/counts/logs and any incomplete live checks. Do not claim runtime acceptance from offline model assertions; root must verify all seven cases on the actual fresh CI backend.

## Implementation Notes

- This repair follows a read-only failure audit, not a weakened gate. The previous generic lane's24 topology initialization failures prevented these bodies from running. Their coverage must be retained in a coherent prepared topology.
- Existing metadata/native helper changes in AspireTopologyFixture and AuthenticatedRouteFixtureIsolationTests belong to another completed handoff; inspect and preserve them before editing. Root handles final reviews, source gates and release publication.

## File List

- `references/Hexalith.Builds`
- `references/Hexalith.EventStore`
- `references/Hexalith.Platform`
