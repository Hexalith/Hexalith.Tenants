# Sprint Change Proposal — Extract DAPR/Aspire Test Harness to EventStore Platform

Date: 2026-06-20
Project: tenants
Workflow: bmad-correct-course
Mode: Batch
Status: APPROVED (2026-06-20 by Jérôme Piquot / Administrator) — routed for implementation
Supersedes: `sprint-change-proposal-2026-06-20.md` (DAPR Baseline Availability) — the fixture/port-resolution
portion is absorbed here; the documentation portion is carried forward as Workstream C.

## 1. Issue Summary

**Trigger (user question):** "Can we move this technical code to the EventStore client project?"

The "technical code" is the DAPR/Aspire **integration-test harness** that currently lives in
`tests/Hexalith.Tenants.IntegrationTests/Fixtures/`:

- `DaprLocalEndpoints.cs` (new) — placement/scheduler host-port resolution.
- `DaprFactAttribute.cs` — `DaprFactAttribute`, `DaprPerformanceFactAttribute`,
  `DaprTestSerializationAttribute`, `DaprTestExecutionGate`, `DaprTestPrerequisites`,
  `DaprPerformanceTestPrerequisites`.
- `TenantsDaprTestFixture.cs` — local `daprd` sidecar launcher + EventStore domain-service test host.
- `AspireTopologyFixture.cs` — full Aspire topology fixture.

This is a **boundary violation**, confirmed by both repositories' own rules:

- **Tenants CLAUDE.md** — "Do not add boilerplate code that is common to domain modules here…
  move the boilerplate into the appropriate technical module before consuming it from Tenants…
  test harness helpers" are explicitly named.
- **EventStore CLAUDE.md** (Domain-Module Authoring) — a domain module "must not re-implement…
  DAPR wiring, telemetry sources, health checks, or event-subscription plumbing. If a capability
  is missing, add it to the platform (SDK / Client / Aspire / ServiceDefaults), not the domain."

A DAPR-backed-domain integration-test harness is exactly such a missing **platform capability**:
every domain module built on EventStore (the Counter sample, Tenants, and future domains) needs the
same `daprd`-sidecar bootstrap, the same placement/scheduler probing, the same Aspire-topology
liveness fixture, and the same support-safe diagnostic scrubbing. Today it is hand-written in Tenants
and would have to be copy-pasted into every new domain.

### Correction to the proposed target

The user proposed the **`Hexalith.EventStore.Client`** project. That is **not** the right home:

- `Client` is a **shipped, production NuGet package** ("Multi-tenant client SDK… domain processor
  registration"). Its only dependencies are `Contracts` + production packages (`Dapr.Client`,
  DataProtection, Http, Configuration.Binder).
- The harness depends on `Xunit.v3`, `Aspire.Hosting.Testing`, raw `TcpClient` probing, and
  `daprd` process launching. Placing it in `Client` would drag test-and-Aspire dependencies into
  every runtime consumer of the production SDK.

**Approved target (this proposal):** a **new** `Hexalith.EventStore.Testing.Integration` package in
the EventStore submodule, depending on `Hexalith.EventStore.Testing` + `Aspire` +
`Aspire.Hosting.Testing`. This keeps the heavy Aspire/`daprd` dependencies out of the lightweight,
widely-consumed `Hexalith.EventStore.Testing` package (8 published packages today; this adds a 9th).

### Evidence

- `tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj` already references
  `$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Testing\…` and pulls `Aspire.Hosting.Testing` —
  the cross-submodule reference plumbing already exists.
- `TenantsDaprTestFixture` already imports `Hexalith.EventStore.Testing.Fakes`
  (`TestEventPublisher`/`FakeDeadLetterPublisher`/`InMemoryCommandStatusStore`) — a shared EventStore
  testing home is already in use.
- The harness is ~80% domain-agnostic; only a thin slice is tenant-specific (see §4 table).

## 2. Impact Analysis

### Epic Impact

No product/PRD epic scope changes. The PRD, epics, architecture, and UX artifacts describe
tenant-management behavior and remain valid. This is a **test-infrastructure / platform-boundary**
correction, not a feature change.

- Epics 1–5: no acceptance-criteria changes to product behavior.
- UI-story integration/smoke tests: continue to pass; they consume the relocated fixtures via thin
  Tenants subclasses with unchanged public surface.

### Story Impact

- No story acceptance criteria change.
- New rule for future stories that add DAPR-backed tests:

  ```text
  DAPR/Aspire integration-test harness rule: domain modules must NOT hand-write daprd-sidecar
  bootstrap, placement/scheduler probing, Aspire-topology fixtures, or support-safe diagnostic
  scrubbing. Consume Hexalith.EventStore.Testing.Integration and supply only domain specifics
  (AppHost type, resource names, domain-service registrations, topic overrides, cursor codec).
  ```

### Artifact Conflicts

- **EventStore solution/release pipeline:** a new packable project must be added to
  `Hexalith.EventStore.slnx` and to the release/package list (8 → 9 published packages). This is an
  **EventStore-repo architecture decision** and must be approved by the EventStore repo owner.
- **Tenants integration-test project:** `.csproj` reference change + fixture replacement.
- **Documentation (carried from superseded baseline proposal):** README/CONTRIBUTING/quickstart/
  deployment-readiness/deploy still describe `dapr init` as a per-repo step (Workstream C).

### Technical Impact

- **EventStore submodule (separate repo):** new `src/Hexalith.EventStore.Testing.Integration` project
  + new `tests/Hexalith.EventStore.Testing.Integration.Tests`; `.slnx` + release-list edits.
- **Tenants repo:** delete the moved fixtures, add thin subclasses, repoint `.csproj`, bump the
  EventStore submodule pointer.
- No backend domain behavior, commands, events, projections, or UI feature contracts change.

> ⚠️ **Cross-repo / submodule constraint.** This cannot be a single in-place edit. The platform code
> lands and is committed in the `Hexalith.EventStore` submodule first; the submodule pointer is then
> bumped in Tenants, and Tenants is updated to consume the new package. Sequencing is in §5.

## 3. Recommended Approach

**Recommended path: Direct Adjustment (Hybrid — platform extraction + thin domain subclass).**

Rationale:

- The change improves maintainability without altering product scope; it is the explicitly
  documented "move boilerplate to the technical module" path from both CLAUDE.md files.
- Extract the domain-agnostic harness once; every current and future domain module reuses it.
- Folding in the superseded baseline proposal means the placement/scheduler port-resolution fix
  (`6050`/`6060` → `50005`/`50006` fallback) is written **once, in the platform**, instead of being
  written into Tenants and immediately relocated.
- No rollback or MVP review is justified.

Effort estimate: **Medium** (cross-repo, new published package, release-list change, careful generic
extraction with abstract hooks).

Risk level: **Medium.** Risks: (a) the generic extraction must keep the exact public surface the
Tenants tests rely on; (b) the new package + its tests must build clean under EventStore's
`TreatWarningsAsErrors` (note: the harness already uses `ConfigureAwait(false)`, satisfying CA2007);
(c) the submodule-pointer bump must precede the Tenants `.csproj` change or Tenants will not build.

## 4. Detailed Change Proposals

### 4.1 What moves vs. what stays

| Move to `Hexalith.EventStore.Testing.Integration` (domain-agnostic) | Keep in Tenants (domain-specific) |
|---|---|
| `DaprLocalEndpoints` (placement/scheduler port resolution) | `system\|tenants\|v1` + `system\|global-administrators\|v1` domain-service registrations |
| `DaprTestPrerequisites` (Redis/placement/scheduler probes, skip reason) | `tenants.events` topic override + `EventPublisher.TopicOverrides` seed |
| `DaprPerformanceTestPrerequisites` | `TenantAggregate`/`GlobalAdministratorsAggregate` registration + `/process` router wiring |
| `DaprFactAttribute`, `DaprPerformanceFactAttribute`, `DaprTestSerializationAttribute`, `DaprTestExecutionGate` | Query-cursor codec id `"Hexalith.Tenants.QueryCursor.v1"` |
| `daprd` sidecar launcher: path resolution, `GetAvailablePorts`, `StartDaprSidecar`, `WaitForDaprHealthAsync`, `KillOrphanedDaprdProcesses`, fixture lock, statestore/pubsub component-file generation | `Projects.Hexalith_Tenants_AppHost` AppHost type |
| `ToSupportSafeDiagnostic`, `IsDaprInfrastructureStartupFailure`, `BuildPrerequisiteFailureMessage`, `GetPrerequisiteFailuresAsync` | Aspire resource names: `eventstore`, `tenants`, `tenants-ui`, `sample` + which get `/alive` checks |
| Aspire topology base: prerequisite probing, Docker health, `DistributedApplicationTestingBuilder.CreateAsync<TAppHost>`, resource-wait/client-create, endpoint-published polling, health-wait, timeout diagnostics | The xUnit collection definitions (`AspireTopologyCollection`, `TenantsDaprTestCollection`) and all test classes |

### 4.2 New project — `Hexalith.EventStore.Testing.Integration` (EventStore submodule)

Path: `src/Hexalith.EventStore.Testing.Integration/`

Proposed `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <Description>Integration-test harness for DAPR-backed Hexalith domain modules — local daprd
      sidecar bootstrap, placement/scheduler endpoint resolution, Aspire topology fixtures, and
      support-safe diagnostics for .NET event sourcing.</Description>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Hexalith.EventStore.Testing\Hexalith.EventStore.Testing.csproj" />
    <ProjectReference Include="..\Hexalith.EventStore.Client\Hexalith.EventStore.Client.csproj" />
    <ProjectReference Include="..\Hexalith.EventStore.Server\Hexalith.EventStore.Server.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting" />
    <PackageReference Include="Aspire.Hosting.Testing" />
    <PackageReference Include="xunit.v3.extensibility.core" /> <!-- FactAttribute / BeforeAfterTestAttribute -->
  </ItemGroup>
</Project>
```

Public API surface to expose (so cross-assembly Tenants subclasses can consume):

- `static class DaprLocalEndpoints` — `PlacementPort` / `SchedulerPort` (verbatim; folds in the
  baseline port fix). Generalize env-var names `HEXALITH_TENANTS_TEST_*` → `HEXALITH_EVENTSTORE_TEST_*`
  (keep the old names honored for one release if desired).
- `static class DaprTestPrerequisites` + `DaprPerformanceTestPrerequisites`
  (`HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS` → `HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS`).
- `DaprFactAttribute`, `DaprPerformanceFactAttribute`, `DaprTestSerializationAttribute`.
- `abstract class DaprDomainServiceTestFixtureBase : IAsyncLifetime` — owns all generic sidecar
  lifecycle; exposes abstract/virtual hooks the domain overrides:
  - `abstract string AppId { get; }`
  - `abstract void ConfigureDomainConfiguration(IConfigurationBuilder config)` (domain-service
    registrations, topic overrides, drain timings)
  - `abstract void ConfigureDomainServices(IServiceCollection services, IConfiguration config)`
    (fakes, `AddEventStore(domainAssembly)`, cursor codec)
  - `abstract void MapDomainEndpoints(WebApplication app)` (the `/process` router map)
  - public `DaprHttpEndpoint`, `AppEndpoint`, `PrerequisitesAvailable`, `SkipReason`,
    `SkipIfUnavailable()`.
- `abstract class AspireTopologyFixtureBase<TAppHost> : IAsyncLifetime where TAppHost : class` —
  owns probing + build/start/client-create; domain overrides:
  - `abstract IReadOnlyList<string> ResourceNames { get; }`
  - `abstract IReadOnlyList<string> AlivenessResourceNames { get; }`
  - `virtual IReadOnlyList<string> ExtraAppArgs { get; }` (e.g. `--EnableKeycloak=false`)
  - public typed `HttpClient` accessors via `Client(string resourceName)`.

> Alternative considered (not recommended now): have the base build the host via the domain-service
> SDK (`AddEventStoreDomainService` / `UseEventStoreDomainService`) instead of `AddEventStoreServer` +
> manual `/process`. Cleaner long-term, but a larger change than the boundary fix requires. Note it as
> a platform follow-up.

### 4.3 New test project — `Hexalith.EventStore.Testing.Integration.Tests` (EventStore submodule)

Move `DaprTestPrerequisiteDiagnosticsTests` here (it tests only the now-relocated generic API:
`ToSupportSafeDiagnostic`, `IsDaprInfrastructureStartupFailure`, `BuildPrerequisiteFailureMessage`,
`DaprTestPrerequisites.SkipReason`). Update the port assertions to use `DaprLocalEndpoints` rather
than the OS-based guess (folds in the baseline change), and update tenant-flavored wording
(`tenantId`/`userId` redaction cases stay — they prove the generic scrubber).

### 4.4 Tenants — thin subclasses replace the moved fixtures

`tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs` becomes:

```csharp
public sealed class AspireTopologyFixture
    : AspireTopologyFixtureBase<Projects.Hexalith_Tenants_AppHost> {
    protected override IReadOnlyList<string> ResourceNames =>
        ["eventstore", "tenants", "tenants-ui", "sample"];
    protected override IReadOnlyList<string> AlivenessResourceNames =>
        ["eventstore", "tenants", "sample"];
    protected override IReadOnlyList<string> ExtraAppArgs => ["--EnableKeycloak=false"];

    public HttpClient CommandApiClient => Client("eventstore");
    public HttpClient TenantsClient => Client("tenants");
    public HttpClient TenantsUiClient => Client("tenants-ui");
    public HttpClient SampleClient => Client("sample");
}
```

`TenantsDaprTestFixture.cs` becomes a `DaprDomainServiceTestFixtureBase` subclass that supplies only
the tenant configuration block, fakes, `AddEventStore(typeof(TenantAggregate).Assembly)`, the cursor
codec, and the `/process` map. The collection definitions and every test class are unchanged.

### 4.5 Tenants `.csproj` reference

`tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj` — add:

```xml
<ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Testing.Integration\Hexalith.EventStore.Testing.Integration.csproj" />
```

(`Aspire.Hosting.Testing` may then move to a transitive dependency of the new package; keep the
explicit `PackageReference` only if a Tenants-local test still needs it directly.)

### 4.6 Workstream C — documentation (carried from the superseded baseline proposal)

The documentation edits from `sprint-change-proposal-2026-06-20.md` (README test-requirements,
CONTRIBUTING prerequisites, quickstart DAPR + troubleshooting, deployment-readiness, deploy/dapr
README, project-context wording) remain valid and are **orthogonal** to the relocation. Execute them
as a Tenants-repo doc workstream alongside or after the relocation. They are not repeated verbatim
here; see the superseded file for exact old→new blocks.

## 5. Implementation Handoff

**Scope classification: Moderate** — cross-repo, a new published platform package, a release-pipeline
list change, and backlog coordination across two repositories. (Not Minor: not a single in-place
edit. Not Major: no PRD/architecture/product replan.)

### Sequencing (must be in order)

1. **EventStore repo** — approve the new 9th package (repo owner); create
   `Hexalith.EventStore.Testing.Integration` + `…Integration.Tests`; extract the generic harness with
   the abstract hooks in §4.2; add both to `Hexalith.EventStore.slnx`; add the package to the release/
   package list; build Release + run the new tests clean under `TreatWarningsAsErrors`.
2. **EventStore repo** — commit (Conventional Commits, e.g.
   `feat(testing): add DAPR/Aspire integration-test harness package`); push.
3. **Tenants repo** — bump the `Hexalith.EventStore` submodule pointer to that commit.
4. **Tenants repo** — repoint `Hexalith.Tenants.IntegrationTests.csproj` (§4.5); replace the two
   fixtures with thin subclasses (§4.4); delete `DaprLocalEndpoints.cs` + the moved generic types;
   move `DaprTestPrerequisiteDiagnosticsTests` to the EventStore test project (step 1).
5. **Tenants repo** — run Tier-2 + Tier-3:
   - `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj -c Release`
6. **Tenants repo** — execute Workstream C doc edits; commit.

### Handoff recipients

- **EventStore repo owner / Architect** — approve and own the new platform package (steps 1–2). Use
  the EventStore-scoped BMAD skills (`Hexalith.EventStore:bmad-*`) for work under that submodule.
- **Developer agent (Tenants)** — steps 3–6 after the submodule pointer is available.

### Success criteria

- No domain-agnostic DAPR/Aspire harness code remains in `Hexalith.Tenants.IntegrationTests/Fixtures/`;
  only thin domain subclasses + collection definitions + test classes remain.
- `Hexalith.EventStore.Testing.Integration` builds and tests clean under `TreatWarningsAsErrors`.
- Tenants Tier-2 + Tier-3 tests pass with no behavior change and no false DAPR skips (port fix folded
  in via `DaprLocalEndpoints`).
- Support-safe diagnostic guarantees remain intact (relocated diagnostics tests pass).
- The Counter sample / future domains can consume the same package — no Tenants coupling leaks into it.

## 6. Checklist Results

- [x] 1.1 Trigger identified: user question — relocate DAPR/Aspire test harness out of Tenants.
- [x] 1.2 Core problem: misplaced platform boundary (domain module hosts reusable technical harness).
- [x] 1.3 Evidence: both CLAUDE.md boundary rules, existing EventStore.Testing reference, harness composition.
- [x] 2.1–2.5 Epic assessment: product epics valid; no epic add/remove/reorder.
- [x] 3.1 PRD: no conflict.
- [x] 3.2 Architecture: platform package addition (EventStore repo); no domain architecture change.
- [x] 3.3 UX: none.
- [x] 3.4 Other artifacts: EventStore `.slnx` + release list, Tenants `.csproj`, docs (Workstream C).
- [x] 4.1 Direct Adjustment (Hybrid): viable, Medium effort, Medium risk — selected.
- [x] 4.2 Rollback: not viable (no completed feature to revert).
- [x] 4.3 MVP review: not viable (MVP unchanged).
- [x] 4.4 Recommended path: Direct Adjustment (platform extraction + thin domain subclass).
- [x] 5.1–5.5 Issue/impact/recommendation/action plan/handoff: included.
- [x] 6.1 Checklist complete.
- [x] 6.2 Proposal reviewed against discovered files.
- [x] 6.3 User approval: obtained 2026-06-20.
- [N/A] 6.4 Sprint-status update: no epic/story additions, removals, or reordering.
- [x] 6.5 Next steps/handoff: confirmed — EventStore repo owner (steps 1–2), Tenants Developer agent (steps 3–6).

## 7. Approval Request

Approve this Sprint Change Proposal for implementation?

- `yes` — route to EventStore repo owner (platform package) then Tenants Developer agent.
- `revise` — adjust the target, the extraction surface, or the sequencing.
- `no` — stop this correction.
