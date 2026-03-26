# Story 7.1: Aspire Hosting & AppHost

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer,
I want .NET Aspire hosting extensions and an AppHost that orchestrates the tenant service with DAPR sidecars,
So that I can start the full local development topology with a single `dotnet run` command.

## Acceptance Criteria

1. **Given** the Hexalith.Tenants.Aspire project exists
   **When** a developer inspects the package
   **Then** it contains `HexalithTenantsExtensions` with extension methods for adding the tenant service to an Aspire distributed application and `HexalithTenantsResources` defining the tenant service resource

2. **Given** the Hexalith.Tenants.AppHost project exists
   **When** `dotnet run` is executed on the AppHost
   **Then** the Aspire dashboard launches and the tenant Hexalith.Tenants is started with a DAPR sidecar configured for state store, pub/sub, and actors

3. **Given** the AppHost is running
   **When** a developer sends a command to the tenant service via the Aspire dashboard or direct HTTP
   **Then** the command is processed end-to-end through the DAPR actor pipeline

4. **Given** a consuming service project references the Hexalith.Tenants.Aspire package
   **When** the developer adds `.AddHexalithTenants()` to their AppHost
   **Then** the tenant service and its DAPR sidecar are included in the consuming service's Aspire topology

## Tasks / Subtasks

- [x] Task 1: Add Sample consuming service to AppHost topology (AC: #2, #3)
    - [x] 1.1: In `src/Hexalith.Tenants.AppHost/Program.cs`, add the Sample project to the Aspire topology with `builder.AddProject<Projects.Hexalith_Tenants_Sample>("sample")` and wire it with a DAPR sidecar that references the PubSub component (the Sample is an event subscriber)
    - [x] 1.2: Add a health endpoint to the Sample project — in `samples/Hexalith.Tenants.Sample/Program.cs`, add `app.MapGet("/health", () => Results.Ok("healthy"));` before `app.Run()`. The Sample does not use ServiceDefaults (it's a lightweight consuming service), so it needs an explicit health endpoint for the Aspire topology test to verify it started successfully.
    - [x] 1.3: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`
    - [x] 1.4: Verify the AppHost correctly references both Hexalith.Tenants and Sample in its topology

- [x] Task 2: Add AppHost reference to IntegrationTests project (unblocks Task 3)
    - [x] 2.1: Add `<ProjectReference Include="..\..\src\Hexalith.Tenants.AppHost\Hexalith.Tenants.AppHost.csproj" />` to `tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj` — required for `DistributedApplicationTestingBuilder.CreateAsync<Projects.Hexalith_Tenants_AppHost>()` to resolve the AppHost project
    - [x] 2.2: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 3: Create Aspire topology smoke test (AC: #2, #3, #4)
    - [x] 3.1: Create `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs` — shared xUnit fixture that boots the AppHost via `DistributedApplicationTestingBuilder.CreateAsync<Projects.Hexalith_Tenants_AppHost>()`, creates HTTP clients for both Hexalith.Tenants and Sample, and waits for both `/health` endpoints to return 200 OK. Use a **3-minute** startup timeout (DAPR actor placement service registration takes time). On timeout failure, capture basic resource diagnostics via the Aspire `DistributedApplication` API to aid CI debugging.
    - [x] 3.2: Create `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyCollection.cs` — xUnit `[CollectionDefinition]` to share the fixture across Aspire topology tests
    - [x] 3.3: Create `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` with tests for `Hexalith.Tenants_resource_starts_and_is_healthy`, `Sample_resource_starts_and_is_healthy`, and `Hexalith.Tenants_process_endpoint_dispatches_command`; the last test provides AppHost topology smoke coverage for AC #3 without over-claiming actor-state verification.
    - [x] 3.4: Mark all Aspire topology tests with `[Trait("Category", "Integration")]` for CI filtering (Tier 3 — require Docker for DAPR)
    - [x] 3.5: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 4: Verify Aspire package NuGet-packability (AC: #1, #4)
    - [x] 4.1: Verify `dotnet pack src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj --configuration Release` produces a valid NuGet package (the Aspire project does NOT set `IsPackable=false`, so it should pack — confirm this)
    - [x] 4.2: Verify the packed NuGet contains `HexalithTenantsExtensions` and `HexalithTenantsResources` types

- [x] Task 5: Full solution validation
    - [x] 5.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
    - [x] 5.2: `dotnet test Hexalith.Tenants.slnx --configuration Release --filter "Category!=Integration"` — all Tier 1 tests pass

## Dev Notes

### Current Implementation State

Most of the Aspire/AppHost infrastructure was scaffolded in Epic 1 (Stories 1.1 and 1.2) and progressively refined across Epics 2-6. The following components are **already fully implemented and functional**:

| Component                 | File                                                             | Status              |
| ------------------------- | ---------------------------------------------------------------- | ------------------- |
| Aspire hosting extensions | `src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs`       | Complete            |
| Aspire resource record    | `src/Hexalith.Tenants.Aspire/HexalithTenantsResources.cs`        | Complete            |
| AppHost Program.cs        | `src/Hexalith.Tenants.AppHost/Program.cs`                        | Needs Sample wiring |
| ServiceDefaults           | `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs`             | Complete            |
| DAPR statestore YAML      | `src/Hexalith.Tenants.AppHost/DaprComponents/statestore.yaml`    | Complete            |
| DAPR pubsub YAML          | `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`        | Complete            |
| DAPR access control YAML  | `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.yaml` | Complete            |
| DAPR resiliency YAML      | `src/Hexalith.Tenants.AppHost/DaprComponents/resiliency.yaml`    | Complete            |

### Gap: Sample Not in AppHost Topology

The **only code change** needed is adding the Sample consuming service to the AppHost topology. Currently, the AppHost's `Program.cs` only adds Hexalith.Tenants:

```csharp
IResourceBuilder<ProjectResource> commandApi = builder.AddProject<Projects.Hexalith_Tenants_Hexalith.Tenants>("commandapi");
HexalithTenantsResources tenantsResources = builder.AddHexalithTenants(commandApi, accessControlConfigPath);
builder.Build().Run();
```

The Sample project (`samples/Hexalith.Tenants.Sample`) is referenced in the AppHost `.csproj` but is NOT added to the Aspire topology. It needs a DAPR sidecar to receive pub/sub events. The EventStore's AppHost (reference pattern at `Hexalith.EventStore/src/Hexalith.EventStore.AppHost/Program.cs`) shows the correct approach:

```csharp
IResourceBuilder<ProjectResource> sample = builder.AddProject<Projects.Hexalith_EventStore_Sample>("sample")
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(new DaprSidecarOptions {
            AppId = "sample",
            Config = accessControlConfigPath,
        }));
```

**Key difference from EventStore pattern:** The Tenants Sample is a **pub/sub subscriber** (it calls `app.MapSubscribeHandler()` and `app.MapTenantEventSubscription()` in its `Program.cs`). Its DAPR sidecar needs the PubSub component reference to receive events:

```csharp
IResourceBuilder<ProjectResource> sample = builder.AddProject<Projects.Hexalith_Tenants_Sample>("sample")
    .WithDaprSidecar(sidecar => sidecar
        .WithOptions(new DaprSidecarOptions
        {
            AppId = "sample",
            Config = accessControlConfigPath,
        })
        .WithReference(tenantsResources.PubSub));
```

The Sample should **NOT** reference `tenantsResources.StateStore` — it has zero direct infrastructure access (per D4). Only Hexalith.Tenants needs the state store for actor state management.

### How the Aspire Topology Works

```
AppHost (dotnet run)
├── Hexalith.Tenants ("commandapi")
│   └── DAPR Sidecar
│       ├── StateStore (state.in-memory, actorStateStore=true)
│       ├── PubSub (pubsub.in-memory)
│       └── Config (accesscontrol.yaml)
│
└── Sample ("sample")          ← ADD THIS
    └── DAPR Sidecar
        ├── PubSub (shared)    ← subscriber needs pub/sub access
        └── Config (accesscontrol.yaml)
```

`HexalithTenantsExtensions.AddHexalithTenants()` provisions:

1. In-memory state store with `actorStateStore=true` metadata (uses `AddDaprComponent` not `AddDaprStateStore` — critical for metadata propagation)
2. In-memory pub/sub via `AddDaprPubSub`
3. Hexalith.Tenants DAPR sidecar with `AppId = "commandapi"`, both component references, and optional access control config
4. Returns `HexalithTenantsResources` record for further customization by the AppHost consumer

### Aspire Package Dependencies (Aspire.csproj)

Current dependencies:

- `Aspire.Hosting` (13.1.2)
- `CommunityToolkit.Aspire.Hosting.Dapr` (13.0.0)

This follows the EventStore Aspire package pattern exactly. The PRD lists "Contracts, Client" as dependencies but these are not needed — the Aspire package only contains hosting extensions (builder methods), not domain types or client abstractions. The EventStore Aspire package confirms this pattern.

### AppHost Dependencies (AppHost.csproj)

```xml
<Project Sdk="Aspire.AppHost.Sdk/13.1.2">
  <ItemGroup>
    <ProjectReference Include="..\Hexalith.Tenants\..." />
    <ProjectReference Include="..\..\samples\Hexalith.Tenants.Sample\..." />
    <ProjectReference Include="..\Hexalith.Tenants.Aspire\..." IsAspireProjectResource="false" />
  </ItemGroup>
</Project>
```

`IsAspireProjectResource="false"` on the Aspire reference prevents Aspire from treating it as a deployable service — it's a library consumed by the AppHost, not a resource.

### ServiceDefaults Already Complete

`src/Hexalith.Tenants.ServiceDefaults/Extensions.cs` provides:

- `AddServiceDefaults()` — OpenTelemetry, health checks, service discovery, resilience
- `ConfigureOpenTelemetry()` — traces (ASP.NET Core + HTTP client + runtime), metrics, OTLP exporter
- `AddDefaultHealthChecks()` — self-check with "live" tag
- `MapDefaultEndpoints()` — `/health`, `/alive`, `/ready` endpoints with JSON response in Development

OpenTelemetry trace sources: `Hexalith.Tenants`, `Hexalith.Tenants` (custom application traces). Health check endpoint returns 503 for Unhealthy status, 200 for Healthy/Degraded.

### DAPR Component Configuration

All 4 DAPR component YAMLs in `src/Hexalith.Tenants.AppHost/DaprComponents/` are complete:

| File                 | Type          | Notes                                                                              |
| -------------------- | ------------- | ---------------------------------------------------------------------------------- |
| `statestore.yaml`    | state.redis   | `actorStateStore=true`, scoped to `commandapi`                                     |
| `pubsub.yaml`        | pubsub.redis  | Dead letter enabled, scoped to `commandapi`                                        |
| `accesscontrol.yaml` | Configuration | Deny-by-default for commandapi, allow POST/\*\*                                    |
| `resiliency.yaml`    | Resiliency    | Retry (constant 3x), timeout (5s), circuit breaker for apps and pub/sub components |

**Note:** The Aspire `AddHexalithTenants()` extension creates **in-memory** state store and pub/sub components at runtime (via `AddDaprComponent("statestore", "state.in-memory")` and `AddDaprPubSub("pubsub")`). The YAML files in DaprComponents/ are for **standalone DAPR** mode (running `daprd` manually outside Aspire). Both configurations are needed: YAMLs for integration tests, Aspire extensions for AppHost.

### Aspire Topology Smoke Test (NEW — Party Review Finding)

The existing integration tests (`TenantsDaprTestFixture`) use a **manual DAPR sidecar** — completely separate from the Aspire orchestration layer. ACs #2 and #3 require proving the Aspire topology works end-to-end, but without an Aspire-based test, this is only manually verifiable. Every previous story shipped tests alongside implementation.

**Reference pattern:** `Hexalith.EventStore/tests/Hexalith.EventStore.IntegrationTests/Security/AspireTopologyFixture.cs` demonstrates the exact approach:

```csharp
// Fixture boots the full AppHost topology
_builder = await DistributedApplicationTestingBuilder
    .CreateAsync<Projects.Hexalith_Tenants_AppHost>()
    .ConfigureAwait(false);
_app = await _builder.BuildAsync().ConfigureAwait(false);
await _app.StartAsync(cts.Token).ConfigureAwait(false);

// Create typed HTTP clients for each resource
HttpClient commandApiClient = _app.CreateHttpClient("commandapi");
HttpClient sampleClient = _app.CreateHttpClient("sample");
```

**Key differences from EventStore pattern:**

- No Keycloak (Tenants AppHost doesn't use it) — simpler fixture
- Must verify **both** Hexalith.Tenants and Sample resources start and are healthy
- The `/process` endpoint test provides smoke coverage for AC #3 by verifying command dispatch through the hosted Aspire topology

**IntegrationTests.csproj change required:** Add AppHost project reference:

```xml
<ProjectReference Include="..\..\src\Hexalith.Tenants.AppHost\Hexalith.Tenants.AppHost.csproj" />
```

The project already has `Aspire.Hosting.Testing` (13.1.1) — no new NuGet packages needed.

**Sample health endpoint:** The Sample project (`samples/Hexalith.Tenants.Sample/Program.cs`) does not currently map a `/health` endpoint. The Aspire topology test needs to verify the Sample is running. Either:

- Add `app.MapGet("/health", () => Results.Ok("healthy"))` to the Sample's `Program.cs` (minimal, recommended), OR
- Use a different endpoint like the subscription handler's existence check

**Fixture design — simple but debuggable:**

- Single `AspireTopologyFixture : IAsyncLifetime` with **3-minute** startup timeout (DAPR actor placement registration takes time — 2 minutes is too aggressive)
- `AspireTopologyCollection` collection definition for xUnit sharing
- 3 test methods in `AspireTopologyTests.cs` (health checks + process endpoint)
- All marked `[Trait("Category", "Integration")]` (Tier 3)
- On timeout failure, include basic diagnostics: resource names, how long startup ran, last HTTP error. Do NOT replicate the EventStore fixture's Docker CLI log capture — Aspire Testing manages DAPR sidecars as processes, not containers

**Known limitation — Sample telemetry:** The Sample project does not reference ServiceDefaults, so it will not report OpenTelemetry traces/metrics to the Aspire dashboard. This is intentional — the Sample is a lightweight consuming service demonstrating event subscription, not a full Aspire-instrumented service. Story 8.3 (aha-moment demo) may revisit this if the demo requires visible telemetry from the subscriber.

### Existing Integration Tests (DO NOT Modify)

The existing test suite at `tests/Hexalith.Tenants.IntegrationTests/` validates the DAPR pipeline via manual sidecar:

- `Hexalith.TenantsRuntimeIntegrationTests.cs` — process endpoint dispatches commands, rejection returns problem details
- `DaprEndToEndTests.cs` — full actor pipeline (CreateTenant → events → state)
- `TenantsDaprTestFixture.cs` — standalone DAPR sidecar test fixture with Redis

These tests are **Tier 3** (require `dapr init` + Docker/Redis). Do NOT modify them in this story.

### Existing Code to Reuse (DO NOT Recreate)

- `HexalithTenantsExtensions.cs` (`src/Hexalith.Tenants.Aspire/`) — `AddHexalithTenants()` extension, ALREADY COMPLETE
- `HexalithTenantsResources.cs` (`src/Hexalith.Tenants.Aspire/`) — resource record, ALREADY COMPLETE
- `Extensions.cs` (`src/Hexalith.Tenants.ServiceDefaults/`) — OpenTelemetry + health checks, ALREADY COMPLETE
- All 4 DaprComponents YAML files — ALREADY COMPLETE
- `Program.cs` (`src/Hexalith.Tenants/`) — full command pipeline, ALREADY COMPLETE
- `Program.cs` (`samples/Hexalith.Tenants.Sample/`) — event subscriber with `AddHexalithTenants()` DI, ALREADY COMPLETE

### Critical Anti-Patterns (DO NOT)

- **DO NOT** recreate or modify `HexalithTenantsExtensions.cs` or `HexalithTenantsResources.cs` — they are complete and match the EventStore pattern
- **DO NOT** use `AddDaprStateStore` — use `AddDaprComponent("statestore", "state.in-memory")` with `.WithMetadata("actorStateStore", "true")` (the convenience method ignores metadata)
- **DO NOT** hardcode `AppPort` in DAPR sidecar options — Aspire auto-detects from the resource model; hardcoding breaks Aspire Testing which randomizes ports
- **DO NOT** add StateStore reference to the Sample's sidecar — only Hexalith.Tenants accesses state store (actors)
- **DO NOT** modify ServiceDefaults — OpenTelemetry instrumentation is Story 7.2's scope
- **DO NOT** modify any DAPR component YAML files — they are already correct
- **DO NOT** modify existing integration test files (`Hexalith.TenantsRuntimeIntegrationTests.cs`, `DaprEndToEndTests.cs`, `TenantsDaprTestFixture.cs`, `TenantsQueryControllerIntegrationTests.cs`) — add NEW files only
- **DO NOT** add DAPR or infrastructure dependencies to non-AppHost/Hexalith.Tenants projects
- **DO NOT** over-engineer the Aspire topology fixture — no Keycloak, no Docker CLI log capture. Keep it simple: boot + health check + dispose. Include basic timeout diagnostics (resource names, startup duration, last error) for CI debuggability, but do not replicate the EventStore fixture's full container log capture pipeline

### Code Style Requirements

- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman brace style (new line before opening brace)
- `ArgumentNullException.ThrowIfNull()` on public method parameters
- `TreatWarningsAsErrors = true` — zero warnings allowed
- 4-space indentation, CRLF line endings, UTF-8
- XML doc comments (`/// <summary>`) on public API types

### File Structure

```
src/Hexalith.Tenants.AppHost/
  ├── Hexalith.Tenants.AppHost.csproj    # EXISTS — references Hexalith.Tenants, Sample, Aspire
  ├── Program.cs                          # EXISTS — MODIFY (add Sample to topology)
  └── DaprComponents/
      ├── accesscontrol.yaml              # EXISTS — no change
      ├── pubsub.yaml                     # EXISTS — no change
      ├── resiliency.yaml                 # EXISTS — no change
      └── statestore.yaml                 # EXISTS — no change

src/Hexalith.Tenants.Aspire/
  ├── Hexalith.Tenants.Aspire.csproj      # EXISTS — no change
  ├── HexalithTenantsExtensions.cs        # EXISTS — no change
  └── HexalithTenantsResources.cs         # EXISTS — no change

src/Hexalith.Tenants.ServiceDefaults/
  ├── Hexalith.Tenants.ServiceDefaults.csproj  # EXISTS — no change
  └── Extensions.cs                             # EXISTS — no change

samples/Hexalith.Tenants.Sample/
  └── Program.cs                          # EXISTS — MODIFY (add /health endpoint if missing)

tests/Hexalith.Tenants.IntegrationTests/
  ├── Hexalith.Tenants.IntegrationTests.csproj  # EXISTS — MODIFY (add AppHost project reference)
  ├── Fixtures/
  │   ├── AspireTopologyFixture.cs              # NEW — Aspire Testing fixture
  │   ├── AspireTopologyCollection.cs           # NEW — xUnit collection definition
  │   ├── TenantsDaprTestFixture.cs             # EXISTS — no change
  │   └── TenantsDaprTestCollection.cs          # EXISTS — no change
  ├── AspireTopologyTests.cs                     # NEW — Aspire smoke tests (3 tests)
  ├── Hexalith.TenantsRuntimeIntegrationTests.cs       # EXISTS — no change
  ├── DaprEndToEndTests.cs                       # EXISTS — no change
  ├── TenantsQueryControllerIntegrationTests.cs  # EXISTS — no change
  └── ScaffoldingSmokeTests.cs                   # EXISTS — no change
```

### Previous Story Intelligence (6.2)

Story 6.2 established:

- Full conformance test suite proving production-test parity (InMemoryTenantService vs TenantAggregate)
- InMemoryTenantProjection for query testing
- All 325 Tier 1 tests pass
- 2 pre-existing IntegrationTests failures (DAPR infrastructure not available locally — expected for Tier 3)

### Git Intelligence

Recent commits show:

- `a1a9d53` — Refactor code structure for improved readability (latest)
- `3e4ef10` — InMemoryTenantService and TenantTestHelpers for integration testing
- All epics 1-6 are complete with tested, working code
- The project has 8 src projects, 5 test projects, 2 sample projects in the solution

### Technology Versions

| Technology                           | Version  | Source                   |
| ------------------------------------ | -------- | ------------------------ |
| .NET SDK                             | 10.0.103 | global.json              |
| Aspire.Hosting                       | 13.1.2   | Directory.Packages.props |
| Aspire.AppHost.Sdk                   | 13.1.2   | AppHost.csproj           |
| CommunityToolkit.Aspire.Hosting.Dapr | 13.0.0   | Directory.Packages.props |
| Aspire.Hosting.Testing               | 13.1.1   | Directory.Packages.props |
| DAPR SDK                             | 1.17.3   | Directory.Packages.props |
| OpenTelemetry                        | 1.15.0   | Directory.Packages.props |

### Project Structure Notes

- Solution file: `Hexalith.Tenants.slnx` (modern XML format)
- All projects already in solution — no changes needed to .slnx
- Files modified: `src/Hexalith.Tenants.AppHost/Program.cs`, `tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj`, `samples/Hexalith.Tenants.Sample/Program.cs` (health endpoint)
- Files created: `AspireTopologyFixture.cs`, `AspireTopologyCollection.cs`, `AspireTopologyTests.cs`
- The Aspire project is packable (no `IsPackable=false` set) — it will be included in the 5 NuGet packages per CI pipeline (FR43)

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 7.1] — Story definition, ACs
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 7] — Epic objectives: deployment & observability
- [Source: _bmad-output/planning-artifacts/prd.md#FR48] — Deploy using .NET Aspire hosting extensions
- [Source: _bmad-output/planning-artifacts/prd.md#FR43] — NuGet packages include Aspire
- [Source: _bmad-output/planning-artifacts/prd.md#FR56] — Deploy alongside EventStore using DAPR configuration
- [Source: _bmad-output/planning-artifacts/architecture.md#Infrastructure & Deployment] — DAPR sidecar + .NET Aspire AppHost orchestration
- [Source: _bmad-output/planning-artifacts/architecture.md#Decision Impact Analysis] — AppHost orchestrates Hexalith.Tenants + DAPR sidecar (single topology)
- [Source: _bmad-output/planning-artifacts/architecture.md#Complete Project Directory Structure] — Aspire + AppHost + ServiceDefaults projects
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreExtensions.cs] — Reference pattern for Aspire hosting extensions
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.AppHost/Program.cs] — Reference pattern for AppHost with Sample + Keycloak topology
- [Source: src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs] — Existing implementation (complete)
- [Source: src/Hexalith.Tenants.Aspire/HexalithTenantsResources.cs] — Existing resource record (complete)
- [Source: src/Hexalith.Tenants.AppHost/Program.cs] — Existing AppHost (needs Sample wiring)
- [Source: src/Hexalith.Tenants.ServiceDefaults/Extensions.cs] — Existing OpenTelemetry + health checks (complete)
- [Source: samples/Hexalith.Tenants.Sample/Program.cs] — Consuming service with event subscription
- [Source: Hexalith.EventStore/tests/Hexalith.EventStore.IntegrationTests/Security/AspireTopologyFixture.cs] — Reference pattern for Aspire Testing fixture (DistributedApplicationTestingBuilder)
- [Source: Hexalith.EventStore/tests/Hexalith.EventStore.IntegrationTests/Security/AspireTopologyCollection.cs] — Reference pattern for xUnit collection definition
- [Source: tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj] — Already has Aspire.Hosting.Testing package, needs AppHost reference

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (1M context)

### Debug Log References

- Build verified 0 warnings, 0 errors at each task checkpoint
- 391 Tier 1 tests pass (34 Contracts + 89 Testing + 48 Client + 17 Sample + 203 Server)
- 2 pre-existing DaprEndToEndTests failures (Tier 3, require DAPR infrastructure) — unrelated to this story
- NuGet pack of Aspire project produces valid package with both public types

### Completion Notes List

- **Task 1**: Added Sample consuming service to AppHost topology. Sample gets a DAPR sidecar with PubSub reference (subscriber-only, no StateStore). Added `/health` endpoint to Sample for topology verification. Follows EventStore AppHost pattern exactly.
- **Task 2**: Added AppHost project reference to IntegrationTests.csproj, enabling `DistributedApplicationTestingBuilder.CreateAsync<Projects.Hexalith_Tenants_AppHost>()`.
- **Task 3**: Created 3 Aspire topology smoke tests (health checks for Hexalith.Tenants and Sample, plus `/process` command-dispatch smoke coverage through the hosted topology). Shared fixture with 3-minute startup timeout, per-resource health polling, prerequisite checks, and timeout diagnostics. All marked `[Trait("Category", "Integration")]`.
- **Task 4**: Verified Aspire package is NuGet-packable. `dotnet pack` succeeds. Package contains `HexalithTenantsExtensions` and `HexalithTenantsResources` types.
- **Task 5**: Full solution builds with 0 warnings, 0 errors. All 391 Tier 1 tests pass.

### File List

**Modified:**

- `src/Hexalith.Tenants.AppHost/Program.cs` — Added Sample to Aspire topology with DAPR sidecar + PubSub reference
- `samples/Hexalith.Tenants.Sample/Program.cs` — Added `/health` endpoint
- `tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj` — Added AppHost project reference

**Created:**

- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs` — Aspire Testing fixture
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyCollection.cs` — xUnit collection definition
- `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` — 3 Aspire topology smoke tests

### Change Log

- 2026-03-18: Story 7.1 implementation — Added Sample to AppHost Aspire topology, created Aspire topology smoke tests (3 tests), verified NuGet packability

