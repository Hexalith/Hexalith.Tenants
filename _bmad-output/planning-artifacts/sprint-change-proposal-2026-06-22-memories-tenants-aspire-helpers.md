# Sprint Change Proposal — Per-Module Aspire Hosting Helpers for Memories & Tenants

Date: 2026-06-22
Project: tenants
Workflow: bmad-correct-course
Mode: Batch
Status: APPROVED (2026-06-22 by Administrator / Jérôme Piquot) — implemented locally, review-complete with patches applied, focused build/pack verified; cross-repo commit/push pending
Related precedent: `sprint-change-proposal-2026-06-22-eventstore-aspire-platform-projects-helper.md` (committed `2012fb9`) and `sprint-change-proposal-2026-06-20-eventstore-test-harness-extraction.md` — same platform-boundary / cross-submodule pattern.

## 1. Issue Summary

**Trigger (user request):** "aspire eventstore setup has been moved to a helper class in eventstore as this initialization is common to all hexalith modules using eventstore. Do the same for memories and tenants."

The Tenants AppHost (`src/Hexalith.Tenants.AppHost/Program.cs`) hand-rolled two per-module hosting blocks that the EventStore extraction had not yet covered:

- **Memories** (old lines ~96–146): a FalkorDB container + `memories-secretstore` + `memories-llm` DAPR components + the `memories-server` project (`HexalithMemoriesServer` `IProjectMetadata`, `SuppressBuild`) + a DAPR sidecar (3502/50002) + redis/falkordb/topic/routing env. The block carried `// TODO: extract to a reusable Memories Aspire extension if a second consumer appears`.
- **Tenants** (old lines ~78–84): the Tenants domain server project (`HexalithTenants` `IProjectMetadata`) wired with `AddEventStoreDomainModule` + a bootstrap env var.

Every Hexalith domain module that embeds the Memories search index or hosts the Tenants service needs the same wiring, so it belongs in each module's own `*.Aspire` library — mirroring `Hexalith.EventStore.Aspire`.

## 2. Decisions (confirmed with Administrator, 2026-06-22)

1. **Tenants scope → Reinstate `Hexalith.Tenants.Aspire`.** The package existed (`25d53a3 feat(story-7.1)`) and was removed by Epic A4 in favour of the platform `AddEventStoreDomainModule`. It is recreated exposing `AddHexalithTenantsServer`. This **intentionally overrides** the EventStore CLAUDE.md rule *"a domain module must not ship its own `*.Aspire` project"* — the owner owns both repos and wants symmetric per-module `*.Aspire` helpers. It restores the 5th package `project-context.md:39` already names.
2. **Memories helper → new published package** `Hexalith.Memories.Aspire` in the `Hexalith.Memories` submodule (added to `tools/release-packages.json` + `.slnx`). Cross-repo sequencing (commit+push Memories → bump pointer), identical to the EventStore helper.
3. **CC mode → Batch.**

## 3. Impact Analysis

- **Epics/Stories:** no product/PRD/UX scope change. Reinforces `epics.md` Story 1.1 AC ("no event-store boilerplate duplicated inside Tenants") and extends the same principle to Memories.
- **Aspire resource graph:** unchanged — resource names (`memories-server`, `memories-falkordb`, `memories-secretstore`, `memories-llm`, `tenants`, `sample`), sidecar ports (3502/50002 Memories; tenants sidecar via `AddEventStoreDomainModule`), env keys, `WaitFor`/reference edges are byte-for-byte equivalent. Pure code-location refactor (independently verified — §6).
- **Design refinement vs. the original plan:** following the EventStore precedent exactly, the **gateway-side domain-service registrations** (`system|tenants|v1`, `system|global-administrators|v1`) and the `global-administrators → tenants.events` **topic override** were kept as **AppHost composition** (they configure the EventStore command gateway, as they did for EventStore) rather than being absorbed into `AddHexalithTenantsServer`. The helper adds only the Tenants **service runtime** (server project + sidecar), symmetric with `AddHexalithEventStorePlatformProjects` (which adds only projects). This keeps the helper faithful to the cited precedent and avoids relocating configuration the AppHost owns.
- **Public API (two packages):**
  - `Hexalith.Memories.Aspire` — new published package (Memories repo: `feat` minor bump; new entry in `tools/release-packages.json`).
  - `Hexalith.Tenants.Aspire` — reinstated published package (Tenants repo: `feat` minor bump; added to the 3 release scripts + the test mirrors).
- **Cross-repo / submodule:** Memories edits land+commit+push first, then the Tenants submodule pointer is bumped (must be reachable on the Memories remote — see `submodule-pointer-push-consistency`). The Tenants AppHost consumes both `.Aspire` libs by `ProjectReference` (`IsAspireProjectResource="false"`), so local builds work before NuGet publish.
- **Bundled pre-existing-drift fix (Memories):** `validate-release-packages.ps1` was already red on Memories `main` because `src/Hexalith.Memories.Web` (`IsPackable=false`) was never classified in `tools/release-packages.json`. Since the new package participates in that same inventory and the fix is one obviously-correct line in the file being edited, `Hexalith.Memories.Web` was added to `nonPackableProjects`. Flagged here for transparency; unrelated to the Aspire helper.

## 4. Detailed Changes

### A. `Hexalith.Memories.Aspire` (NEW, Memories submodule)
`src/Hexalith.Memories.Aspire/`: `Hexalith.Memories.Aspire.csproj` (packable, ITANEO metadata, README, pins `MessagePack`, references `CommunityToolkit.Aspire.Hosting.Dapr`); internal `RepositoryProjectPaths` (own 5-up resolver — no EventStore dependency); internal `MemoriesServerProjectMetadata` (`SuppressBuild`); public `HexalithMemoriesServerExtensions.AddHexalithMemoriesSearchIndexServer(stateStore, pubSub, secretStorePath, llmPath, …) → HexalithMemoriesSearchIndexServerResources`. Registered in `Hexalith.Memories.slnx` + `tools/release-packages.json`.

### B. `Hexalith.Tenants.Aspire` (REINSTATED, this repo)
`src/Hexalith.Tenants.Aspire/`: `Hexalith.Tenants.Aspire.csproj` (inherits packable defaults; references `Aspire.Hosting` + `ProjectReference` EventStore.Aspire); internal `TenantsServerProjectMetadata` (`SuppressBuild`, dual-layout path); public `HexalithTenantsServerExtensions.AddHexalithTenantsServer(eventStore, daprConfigPath, …) → IResourceBuilder<ProjectResource>` (= `AddProject<TenantsServerProjectMetadata>(appId).AddEventStoreDomainModule(…)`). Registered in `Hexalith.Tenants.slnx`.

### C. Tenants AppHost rewire — `src/Hexalith.Tenants.AppHost/`
`Program.cs` (260 → 220 lines): the Tenants server block → `builder.AddHexalithTenantsServer(eventStoreResources, accessControlConfigPath, …).WithEnvironment("Tenants__BootstrapGlobalAdminUserId", …)`; the Memories block → `builder.AddHexalithMemoriesSearchIndexServer(eventStoreResources.StateStore, eventStoreResources.PubSub, …)` + the Tenants source→index routing env. Gateway-side registrations + topic override + UI/Sample/Keycloak composition unchanged. Deleted `HexalithTenants.cs` + `HexalithMemoriesServer.cs`; kept `HexalithTenantsUI.cs` + `HexalithTenantsSample.cs`. `Hexalith.Tenants.AppHost.csproj` adds `ProjectReference`s to both `.Aspire` libs (`IsAspireProjectResource="false"`).

### D. Release-list + governance tests
- Tenants: `scripts/pack-release-packages.py`, `scripts/validate-nuget-packages.py` (+ the exact 12-dependency boundary for `Hexalith.Tenants.Aspire`), `release.config.cjs` (`expectedPackageIds`). `validate-consumer-package-references.py` intentionally **not** changed (the Aspire host-helper is not a domain consumer surface).
- Governance tests updated to the reinstated state: `SolutionStructureTests` (Aspire moved forbidden→required), `PackageGovernanceTests` (publishable projects + expected ids; consumer-script comment), `CiQualityGateScriptTests` (expected ids + deps + "Validated 5 …"), `EventPublicationConfigurationTests.AppHost_DaprTopology_…` (rewritten to assert the helper-based composition and that the Tenants runtime + domain-module wiring live in `Hexalith.Tenants.Aspire`).

## 5. Implementation Sequencing (cross-repo — ordered)

1. **Memories submodule:** `Hexalith.Memories.Aspire` + slnx + release-packages.json (+ the Web drift fix). Build clean; `validate-release-packages.ps1` green; commit `feat(aspire): add Memories search-index server AppHost helper`; **push**.
2. **Tenants:** bump `Hexalith.Memories` submodule pointer; verify reachable on remote.
3. **Tenants:** commit `Hexalith.Tenants.Aspire` + AppHost rewire + release-scripts + governance tests as `feat(aspire): reinstate Hexalith.Tenants.Aspire + consume Memories/Tenants AppHost helpers`; push branch (no merge/PR unless asked).

## 6. Implementation Log (2026-06-22 — implemented, build/test-verified, uncommitted)

**Memories submodule (uncommitted):** new `src/Hexalith.Memories.Aspire/` (5 files); `Hexalith.Memories.slnx` + `tools/release-packages.json` updated (Aspire added; Web classified). `Hexalith.Memories.Aspire` builds Release `-warnaserror` **0/0**; `tools/validate-release-packages.ps1` **passed**; `tests/tooling/release_packages/release_packages_test.py` **18/18**.

**Tenants repo (uncommitted, branch pending):** new `src/Hexalith.Tenants.Aspire/` (3 files); AppHost rewired; release scripts + `release.config.cjs` + 4 governance-test files updated.
- Full `Hexalith.Tenants.slnx` Release `-warnaserror`: **0 warn / 0 err**.
- `pack-release-packages.py` → 5 packages; `validate-nuget-packages.py` → **passed** (Aspire deps exact).
- Tests: Contracts **106/106**, Server **700/700**, Client **48/48**, Testing **181/181**, UI **757/757**, Sample **39/39** — **1831 total, 0 failures**.
- `rg` sweep: no lingering references to the deleted `HexalithTenants` / `HexalithMemoriesServer` metadata classes.

## 7. Checklist Results
- [x] Trigger identified; core problem (per-module AppHost hosting boilerplate not yet extracted / A4-removed Tenants helper wanted back).
- [x] Evidence: `Program.cs` TODO; `project-context.md:39`; git `25d53a3`/`37678cf`; EventStore precedent `2012fb9`.
- [x] Epic/Story impact: none to product scope; reinforces Story 1.1 AC.
- [x] Artifacts: 2 published packages, AppHost rewire, 3 release scripts, 2 `.slnx`, `release-packages.json`, 4 governance-test files.
- [x] Approach: Direct Adjustment (per-module extraction + reinstated package) — Low–Medium effort, Low risk.
- [x] Decisions confirmed (Tenants=reinstate, Memories=published, Batch).
- [x] Handoff: Moderate scope (cross-repo + public API + submodule bump). Sequencing in §5.

## 8. Approval

Approved 2026-06-22 by Administrator / Jérôme Piquot (AskUserQuestion + ExitPlanMode). Routed for the cross-repo commit/push sequence in §5.

## 9. Review Findings

- [x] [Review][Decision] EventStore submodule bump is outside this proposal's stated sequencing — resolved: accepted as intentionally bundled in this review scope. The reviewed merge changes `Hexalith.EventStore` from `95c4b118e2d0bbf5fefc57650912b1b8c5e32c14` to `4914d301a1925b83473b526ac3c80c0e692dfc05`, while §5 only calls for bumping `Hexalith.Memories` before the Tenants commit.
- [x] [Review][Patch] Add a scoped NU5104 suppression to `Hexalith.Tenants.Aspire` [src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj:3]
- [x] [Review][Patch] Add a scoped NU5104 suppression to `Hexalith.Memories.Aspire` [references/Hexalith.Memories/src/Hexalith.Memories.Aspire/Hexalith.Memories.Aspire.csproj:3]
- [x] [Review][Patch] Correct the AppHost comment that says `AddHexalithTenantsServer` registers gateway routing [src/Hexalith.Tenants.AppHost/Program.cs:81]
- [x] [Review][Patch] Add the required Memories copyright header to the new Memories Aspire C# files [references/Hexalith.Memories/src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs:1]
- [x] [Review][Patch] Split `HexalithMemoriesSearchIndexServerResources` into its own file [references/Hexalith.Memories/src/Hexalith.Memories.Aspire/HexalithMemoriesServerExtensions.cs:17]
