# Story 1.2: DAPR Component Configuration & ServiceDefaults

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->
<!-- ADR: Component location follows EventStore implementation pattern (AppHost/DaprComponents/), not architecture prose (dapr/components/). See Dev Notes for rationale. -->
<!-- Scope expanded per elicitation: includes access control, resiliency, Aspire extension, AppHost wiring, and full ServiceDefaults implementation. -->

## Story

As a developer,
I want DAPR component YAML files for local development, a fully implemented ServiceDefaults project with OpenTelemetry and health checks, and the AppHost wired to orchestrate DAPR sidecars via Aspire,
so that local development with DAPR sidecars, observability, and health probes is ready for domain service implementation in Epic 2.

## Acceptance Criteria

1. **Given** the solution from Story 1.1 exists **When** a developer inspects `src/Hexalith.Tenants.AppHost/DaprComponents/` **Then** `statestore.yaml` configures a Redis state store with `actorStateStore: "true"` metadata, environment-variable-based connection, and `scopes: [commandapi]`

2. **Given** the solution from Story 1.1 exists **When** a developer inspects `src/Hexalith.Tenants.AppHost/DaprComponents/` **Then** `pubsub.yaml` configures a Redis pub/sub component with `scopes: [commandapi]`, environment-variable-based connection, and dead-letter enabled

3. **Given** the solution from Story 1.1 exists **When** a developer inspects `src/Hexalith.Tenants.AppHost/DaprComponents/` **Then** `accesscontrol.yaml` configures `defaultAction: allow` for local development with a prominent warning comment that this must NOT be used in production

4. **Given** the solution from Story 1.1 exists **When** a developer inspects `src/Hexalith.Tenants.AppHost/DaprComponents/` **Then** `resiliency.yaml` configures retry, timeout, and circuit-breaker policies for statestore, pubsub, and app targets following EventStore's local development pattern

5. **Given** the ServiceDefaults project exists **When** a developer inspects `Extensions.cs` **Then** it contains:
   - `AddServiceDefaults()` method with OpenTelemetry tracing (with Tenants-specific sources), metrics (AspNetCore, Http, Runtime), and JSON console logging
   - `ConfigureOpenTelemetry()` method with structured logging and OTLP exporter support
   - `AddDefaultHealthChecks()` method with a self-check liveness probe
   - `MapDefaultEndpoints()` method mapping `/health`, `/alive`, and `/ready` endpoints with health status codes

6. **Given** the Aspire project exists **When** a developer inspects the Aspire extension **Then** it contains an `AddHexalithTenants()` extension method that creates DAPR state store, pub/sub, and sidecar configuration for the CommandApi project, returning a resources record for downstream use

7. **Given** the AppHost project exists **When** a developer inspects `Program.cs` **Then** it uses the Aspire extension to wire CommandApi with a DAPR sidecar, referencing the access control configuration and DAPR components

8. **Given** all changes are complete **When** `dotnet build Hexalith.Tenants.slnx --configuration Release` is executed **Then** all projects compile with zero errors

9. **Given** all changes are complete **When** `dotnet test Hexalith.Tenants.slnx` is executed **Then** all 6 test projects are discovered with zero failures (no regressions from Story 1.1)

## Tasks / Subtasks

- [x] Task 0: Verify prerequisites (AC: all)
  - [x] 0.1: Verify Story 1.1 solution builds — run `dotnet build Hexalith.Tenants.slnx` and confirm zero errors
  - [x] 0.2: Verify EventStore reference files exist — confirm `Hexalith.EventStore/src/Hexalith.EventStore.AppHost/DaprComponents/` contains statestore.yaml, pubsub.yaml, accesscontrol.yaml, resiliency.yaml
  - [x] 0.3: Verify EventStore ServiceDefaults reference — confirm `Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Extensions.cs` exists and read its full content
  - [x] 0.4: Verify EventStore Aspire extension reference — confirm `Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreExtensions.cs` exists and read its full content

- [x] Task 1: Implement ServiceDefaults Extensions.cs (AC: #5)
  - [x] 1.1: Replace the placeholder `Extensions.cs` in `src/Hexalith.Tenants.ServiceDefaults/` with full implementation mirroring EventStore's pattern
  - [x] 1.2: Implement `AddServiceDefaults<TBuilder>()` — calls ConfigureOpenTelemetry, AddDefaultHealthChecks, AddServiceDiscovery, ConfigureHttpClientDefaults with standard resilience handler
  - [x] 1.3: Implement `ConfigureOpenTelemetry<TBuilder>()` — structured logging with `AddOpenTelemetry` (IncludeFormattedMessage, IncludeScopes), JSON console logging with UTC timestamps, metrics (AspNetCore, Http, Runtime instrumentation), tracing with sources `Hexalith.Tenants.CommandApi` and `Hexalith.Tenants` plus app name, health endpoint filtering, OTLP exporter
  - [x] 1.4: Implement `AddDefaultHealthChecks<TBuilder>()` — self-check liveness probe tagged `"live"`
  - [x] 1.5: Implement `MapDefaultEndpoints()` — map `/health` (all checks), `/alive` (live-tagged), `/ready` (ready-tagged) with status code mapping (Healthy/Degraded → 200, Unhealthy → 503), JSON response writer in Development environment
  - [x] 1.6: Implement private `AddOpenTelemetryExporters<TBuilder>()` — OTLP exporter configuration conditional on `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable
  - [x] 1.7: Verify `dotnet build` passes for ServiceDefaults project

- [x] Task 2: Create DAPR component YAML files (AC: #1, #2, #3, #4)
  - [x] 2.1: Create directory `src/Hexalith.Tenants.AppHost/DaprComponents/`
  - [x] 2.2: Create `statestore.yaml` — `state.redis` type, `actorStateStore: "true"`, `redisHost: "{env:REDIS_HOST|localhost:6379}"`, `scopes: [commandapi]`. Mirror EventStore's DaprComponents/statestore.yaml
  - [x] 2.3: Create `pubsub.yaml` — `pubsub.redis` type, `enableDeadLetter: "true"`, `deadLetterTopic: "deadletter"`, `redisHost: "{env:REDIS_HOST|localhost:6379}"`, `scopes: [commandapi]`. Mirror EventStore's DaprComponents/pubsub.yaml
  - [x] 2.4: Create `accesscontrol.yaml` — `defaultAction: allow` for local dev, policies for `commandapi` allowing POST on `/**, add `# WARNING: Local development only. Production MUST use deny-by-default with mTLS. See deploy/dapr/ for production templates.` comment. Mirror EventStore's DaprComponents/accesscontrol.yaml
  - [x] 2.5: Create `resiliency.yaml` — retry (constant 1s, 3 retries default; exponential for pubsub outbound/inbound), timeout (5s general, 10s pubsub, 30s subscriber), circuit breaker (3 consecutive failures). Target apps: commandapi. Target components: pubsub, statestore. Mirror EventStore's DaprComponents/resiliency.yaml

- [x] Task 3: Create Aspire extension (AC: #6)
  - [x] 3.1: Create `src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs` with `AddHexalithTenants()` extension method on `IDistributedApplicationBuilder`
  - [x] 3.2: Extension takes `IResourceBuilder<ProjectResource> commandApi` and optional `string? daprConfigPath` parameters
  - [x] 3.3: Create DAPR state store via `AddDaprComponent("statestore", "state.in-memory")` with `actorStateStore: "true"` metadata
  - [x] 3.4: Create DAPR pub/sub via `AddDaprPubSub("pubsub")`
  - [x] 3.5: Wire CommandApi with `WithDaprSidecar()` — AppId `"commandapi"`, Config from `daprConfigPath`, references to stateStore and pubSub
  - [x] 3.6: Create `HexalithTenantsResources` record returning `StateStore`, `PubSub`, `CommandApi` resource builders
  - [x] 3.7: Verify `dotnet build` passes for Aspire project

- [x] Task 4: Update AppHost Program.cs (AC: #7)
  - [x] 4.1: Add `using` for `CommunityToolkit.Aspire.Hosting.Dapr` and `Hexalith.Tenants.Aspire`
  - [x] 4.2: Add access control config path resolution with fallback (runtime vs source directory) — mirror EventStore's AppHost pattern
  - [x] 4.3: Add CommandApi project resource via `builder.AddProject<Projects.Hexalith_Tenants_CommandApi>("commandapi")`
  - [x] 4.4: Call `builder.AddHexalithTenants(commandApi, accessControlConfigPath)` to wire DAPR topology
  - [x] 4.5: Verify `dotnet build` passes for AppHost project

- [x] Task 5: Full verification (AC: #8, #9)
  - [x] 5.1: Run `dotnet build Hexalith.Tenants.slnx --configuration Release` — verify zero errors across all projects
  - [x] 5.2: Run `dotnet test Hexalith.Tenants.slnx` — verify 6 test projects discovered, zero failures
  - [x] 5.3: Verify all 4 DAPR component YAML files exist in `src/Hexalith.Tenants.AppHost/DaprComponents/` with correct content

## Dev Notes

### Architecture Requirements

- **Mirror EventStore implementation patterns exactly** — the reference implementation is in the EventStore submodule at `Hexalith.EventStore/src/`, NOT the architecture prose. Where the architecture document and EventStore implementation diverge, follow EventStore's code.
- **DAPR component location**: `src/Hexalith.Tenants.AppHost/DaprComponents/` (EventStore pattern), NOT `dapr/components/` (architecture prose). The architecture document's directory structure lists `dapr/components/` but EventStore places local dev components inside the AppHost project for Aspire integration. Follow the implementation.
- **No `actors.yaml` file needed**: The architecture document lists `actors.yaml` for actor type configuration, but EventStore does NOT use a separate actors component file. Actor state is handled by setting `actorStateStore: "true"` on the state store component (AC 1). Actor type registration happens programmatically via `app.MapActorsHandlers()` in later stories (Epic 2). This is an architecture document error — document the deviation in the Change Log.
- **ServiceDefaults is NOT referenced by AppHost** — only by web application projects (CommandApi, Sample). AppHost uses `Aspire.AppHost.Sdk` which conflicts with `FrameworkReference Microsoft.AspNetCore.App`. The dependency graph from Story 1.1 already enforces this — do not change it.
- **Aspire extension is self-contained for Story 1.2** — `AddHexalithTenants()` does NOT reference EventStore's Aspire extension. Cross-project Aspire wiring comes in later stories when EventStore runtime integration is needed.

### DAPR Component Patterns (from EventStore Reference)

**State Store (`statestore.yaml`):**
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: statestore
spec:
  type: state.redis
  version: v1
  metadata:
    - name: redisHost
      value: "{env:REDIS_HOST|localhost:6379}"
    - name: redisPassword
      value: "{env:REDIS_PASSWORD}"
    - name: actorStateStore
      value: "true"
scopes:
  - commandapi
```

**Pub/Sub (`pubsub.yaml`):**
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: pubsub
spec:
  type: pubsub.redis
  version: v1
  metadata:
    - name: redisHost
      value: "{env:REDIS_HOST|localhost:6379}"
    - name: redisPassword
      value: "{env:REDIS_PASSWORD}"
    - name: enableDeadLetter
      value: "true"
    - name: deadLetterTopic
      value: "deadletter"
scopes:
  - commandapi
```

**Access Control (`accesscontrol.yaml`):**
```yaml
# WARNING: Local development only.
# Production MUST use deny-by-default with mTLS.
# See deploy/dapr/ for production templates.
apiVersion: dapr.io/v1alpha1
kind: Configuration
metadata:
  name: accesscontrol
spec:
  accessControl:
    defaultAction: allow
    trustDomain: "public"
    policies:
      - appId: commandapi
        defaultAction: deny
        trustDomain: "public"
        operations:
          - name: /**
            httpVerb: ['POST']
            action: allow
```

**Resiliency (`resiliency.yaml`):** Mirror EventStore's `DaprComponents/resiliency.yaml` with:
- Retry policies: constant 1s/3 retries (default), exponential for pubsub outbound (10s max, 3 retries) and inbound (30s max, 10 retries)
- Timeouts: 5s general, 10s pubsub, 30s subscriber
- Circuit breakers: 3 consecutive failures (default), 5 for pubsub
- Targets: `commandapi` app, `pubsub` and `statestore` components

### ServiceDefaults Pattern (from EventStore Reference)

The `Extensions.cs` file must contain these public methods:
1. `AddServiceDefaults<TBuilder>(this TBuilder builder)` where `TBuilder : IHostApplicationBuilder` — main entry point calling all others
2. `ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)` — logging + tracing + metrics
3. `AddDefaultHealthChecks<TBuilder>(this TBuilder builder)` — liveness probe
4. `MapDefaultEndpoints(this WebApplication app)` — health/alive/ready endpoint mapping

**Key implementation details:**
- Namespace: `Hexalith.Tenants.ServiceDefaults`
- Trace sources: `builder.Environment.ApplicationName`, `"Hexalith.Tenants.CommandApi"`, `"Hexalith.Tenants"`
- Health endpoint paths: `/health`, `/alive`, `/ready` (constants)
- Filter health endpoints from traces (no noise in telemetry)
- JSON console logging with UTC timestamps
- OTLP exporter conditional on `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable
- Status codes: Healthy/Degraded → 200, Unhealthy → 503
- Development environment: include JSON response writer for health check details

### Aspire Extension Pattern (from EventStore Reference)

**`HexalithTenantsExtensions.cs`:**
- Namespace: `Hexalith.Tenants.Aspire`
- `AddHexalithTenants(this IDistributedApplicationBuilder builder, IResourceBuilder<ProjectResource> commandApi, string? daprConfigPath = null)`
- Uses `AddDaprComponent("statestore", "state.in-memory")` with `.WithMetadata("actorStateStore", "true")` — NOT `AddDaprStateStore` (metadata propagation requires `AddDaprComponent`)
- Uses `AddDaprPubSub("pubsub")` for pub/sub
- Wires CommandApi with `WithDaprSidecar()` — AppId `"commandapi"`, Config from `daprConfigPath`, references stateStore and pubSub
- AppPort intentionally OMITTED for Aspire Testing compatibility (Aspire randomizes ports)
- Returns `HexalithTenantsResources` record with `StateStore`, `PubSub`, `CommandApi` resource builders

**`HexalithTenantsResources.cs`:**
```csharp
public record HexalithTenantsResources(
    IResourceBuilder<IDaprComponentResource> StateStore,
    IResourceBuilder<IDaprComponentResource> PubSub,
    IResourceBuilder<ProjectResource> CommandApi);
```

### AppHost Program.cs Pattern (from EventStore Reference)

- Resolve access control config path dynamically with fallback:
  1. Try `Path.Combine(Directory.GetCurrentDirectory(), "DaprComponents", "accesscontrol.yaml")`
  2. Fallback: `Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DaprComponents", "accesscontrol.yaml"))`
- Add CommandApi project: `builder.AddProject<Projects.Hexalith_Tenants_CommandApi>("commandapi")`
- Wire DAPR topology: `builder.AddHexalithTenants(commandApi, accessControlConfigPath)`

### Library & Framework Requirements

All package versions are already pinned in `Directory.Packages.props` from Story 1.1. No new packages to add. Relevant packages for this story:

| Package | Version | Used By |
|---------|---------|---------|
| CommunityToolkit.Aspire.Hosting.Dapr | 13.0.0 | Aspire extension (`AddDaprComponent`, `AddDaprPubSub`, `WithDaprSidecar`) |
| Aspire.Hosting | 13.1.2 | AppHost, Aspire extension |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.15.0 | ServiceDefaults (OTLP export) |
| OpenTelemetry.Extensions.Hosting | 1.15.0 | ServiceDefaults (hosting integration) |
| OpenTelemetry.Instrumentation.AspNetCore | 1.15.0 | ServiceDefaults (ASP.NET tracing) |
| OpenTelemetry.Instrumentation.Http | 1.15.0 | ServiceDefaults (HTTP client tracing) |
| OpenTelemetry.Instrumentation.Runtime | 1.15.0 | ServiceDefaults (runtime metrics) |
| Microsoft.Extensions.Http.Resilience | 10.3.0 | ServiceDefaults (standard resilience handler) |
| Microsoft.Extensions.ServiceDiscovery | 10.3.0 | ServiceDefaults (Aspire service discovery) |

**DO NOT** add new NuGet packages. All dependencies are already in `Directory.Packages.props`. If a package is missing, check EventStore's `Directory.Packages.props` first — it should already be there.

### File Structure Requirements

**New files to create:**
```
src/Hexalith.Tenants.AppHost/
├── DaprComponents/
│   ├── statestore.yaml          # NEW — Redis state store for local dev
│   ├── pubsub.yaml              # NEW — Redis pub/sub for local dev
│   ├── accesscontrol.yaml       # NEW — Allow-by-default for local dev
│   └── resiliency.yaml          # NEW — Conservative retry/timeout/CB

src/Hexalith.Tenants.Aspire/
├── HexalithTenantsExtensions.cs # NEW — AddHexalithTenants() extension
└── HexalithTenantsResources.cs  # NEW — Resources record
```

**Existing files to modify:**
```
src/Hexalith.Tenants.ServiceDefaults/
└── Extensions.cs                # REPLACE — Full implementation (was placeholder)

src/Hexalith.Tenants.AppHost/
└── Program.cs                   # REPLACE — Full Aspire topology (was minimal stub)
```

**Files NOT to create or modify:**
- Do NOT create `dapr/components/` at project root (architecture prose location — wrong)
- Do NOT create `actors.yaml` (not needed — see Architecture Requirements)
- Do NOT create `deploy/dapr/` production templates (future story)
- Do NOT modify any `.csproj` files (dependencies already correct from Story 1.1)
- Do NOT add `appsettings.json` or `launchSettings.json` (future stories)
- Do NOT add domain logic, commands, events, or aggregates

### Testing Requirements

**Verification approach for Story 1.2:**

This story creates infrastructure configuration files and shared startup code. There are no unit-testable behaviors yet — the DAPR components and ServiceDefaults patterns will be functionally tested when domain services are implemented in Epic 2.

**Required verifications:**
1. `dotnet build Hexalith.Tenants.slnx --configuration Release` — zero errors (AC #8)
2. `dotnet test Hexalith.Tenants.slnx` — 6 test projects discovered, zero failures (AC #9)
3. All 4 YAML files exist in `src/Hexalith.Tenants.AppHost/DaprComponents/` with correct structure

**YAML validation approach:**
- Each YAML file must have correct `apiVersion`, `kind`, `metadata.name`, and `spec` structure
- State store must include `actorStateStore: "true"` in metadata
- All component files must include `scopes: [commandapi]`
- Access control must include the production warning comment

**What NOT to test in this story:**
- Do NOT attempt to start Dapr sidecars or validate DAPR runtime behavior
- Do NOT write unit tests for ServiceDefaults (Extension methods are integration-tested via Aspire in later stories)
- Do NOT write integration tests for DAPR components (that's Story 7.1/IntegrationTests)

### Previous Story Intelligence (from Story 1.1)

**Key learnings from Story 1.1 implementation:**
- ServiceDefaults `Extensions.cs` initially failed to compile — `IHostApplicationBuilder` required explicit `using Microsoft.Extensions.Hosting;` since plain SDK projects don't include it in implicit usings (Web SDK does). **Ensure all required usings are explicit in ServiceDefaults.**
- Library SDK projects (Contracts, Client, Server, Testing, Aspire) compile empty without placeholders. Only Web SDK and Exe projects need entry points.
- EventStore submodule is confirmed working with 8 src project directories present.
- .NET SDK 10.0.103 confirmed available. `dotnet build --configuration Release` passes with zero errors and warnings.
- All 6 test projects discovered by `dotnet test` with zero failures.
- The `.slnx` format uses modern XML — when adding new files, they do NOT need to be added to the solution file (solution file references projects, not individual files).

**Patterns established in Story 1.1 that must be maintained:**
- Project references use relative paths through the EventStore submodule (e.g., `..\..\Hexalith.EventStore\src\...`)
- Namespace convention: `Hexalith.Tenants.{ProjectName}`
- File-scoped namespaces enforced by `.editorconfig`
- Allman braces, `_camelCase` private fields, 4-space indentation

**Files created in Story 1.1 relevant to Story 1.2:**
- `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs` — **placeholder to REPLACE** (currently empty static class with placeholder method)
- `src/Hexalith.Tenants.AppHost/Program.cs` — **stub to REPLACE** (currently minimal `DistributedApplication.CreateBuilder(args); builder.Build().Run();`)
- `src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj` — **already has correct dependencies** (Aspire.Hosting, CommunityToolkit.Aspire.Hosting.Dapr)

### Git Intelligence

**Recent commits (5):**
```
19feed1 Merge pull request #2 from Hexalith/add-bmad-planning-artifacts
f2c46ee Add BMAD planning artifacts and sprint status
363167e Merge pull request #1 from Hexalith/add-project-setup
f93ed19 Add project setup with EventStore submodule and tooling config
c04fc8b Initial commit
```

**Observations:**
- Story 1.1 code changes are untracked (in `review` status, not yet committed to main). The working tree has all Story 1.1 files as untracked additions.
- No existing DAPR component files or Aspire extensions in the repository yet.
- The branch structure uses PR-based merges to main.

### Critical Implementation Guards

- **DO NOT** create `dapr/components/` at the project root — use `src/Hexalith.Tenants.AppHost/DaprComponents/` (EventStore pattern)
- **DO NOT** create `actors.yaml` — actor state is configured via `actorStateStore: "true"` on the state store component; actor types are registered programmatically in later stories
- **DO NOT** deploy local dev access control config to production — it uses `defaultAction: allow`
- **DO NOT** hard-code secrets in YAML — use `{env:VARIABLE_NAME}` pattern for all credentials
- **DO NOT** reference ServiceDefaults from AppHost — only web app projects (CommandApi, Sample) use ServiceDefaults
- **DO NOT** reference EventStore's Aspire extension from Tenants' Aspire extension in this story — cross-project wiring comes later
- **DO NOT** omit `scopes` from DAPR component files — every component MUST be scoped to `commandapi` from day one
- **DO NOT** add `appsettings.json`, `launchSettings.json`, domain logic, commands, events, or aggregates — those are future stories
- **DO** use `AddDaprComponent()` (not `AddDaprStateStore()`) for state store in the Aspire extension — metadata propagation requires the generic method
- **DO** omit `AppPort` in `DaprSidecarOptions` — Aspire Testing randomizes ports and auto-detection is required

### Project Structure Notes

- Alignment with unified project structure: All new files placed within existing project directories from Story 1.1. No new projects created.
- The `DaprComponents/` directory is new under AppHost — this is a convention from EventStore, not a separate project.
- `HexalithTenantsExtensions.cs` and `HexalithTenantsResources.cs` are added to the existing `Hexalith.Tenants.Aspire` project.
- No changes to `.slnx` or any `.csproj` files required.

### References

- [Source: Hexalith.EventStore/src/Hexalith.EventStore.AppHost/DaprComponents/] — DAPR component YAML reference files (statestore, pubsub, accesscontrol, resiliency)
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.ServiceDefaults/Extensions.cs] — ServiceDefaults implementation pattern
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreExtensions.cs] — Aspire extension pattern
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Aspire/HexalithEventStoreResources.cs] — Resources record pattern
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.AppHost/Program.cs] — AppHost DAPR topology wiring pattern
- [Source: Hexalith.EventStore/docs/guides/dapr-component-reference.md] — Comprehensive DAPR component reference
- [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture] — State store, pub/sub, projection decisions
- [Source: _bmad-output/planning-artifacts/architecture.md#Infrastructure & Deployment] — DAPR sidecar + Aspire hosting decisions
- [Source: _bmad-output/planning-artifacts/architecture.md#Monitoring] — OpenTelemetry via EventStore's telemetry infrastructure
- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2] — Original acceptance criteria and story definition
- [Source: _bmad-output/implementation-artifacts/1-1-solution-structure-and-build-configuration.md] — Previous story learnings and established patterns

## Change Log

- 2026-03-08: Story context created — comprehensive developer guide with DAPR component patterns, ServiceDefaults implementation, Aspire extension, and AppHost wiring. Scope expanded from original epics to include access control, resiliency, and full Aspire integration based on EventStore reference analysis. Architecture deviation documented: `actors.yaml` removed (not used in EventStore), component location corrected to `AppHost/DaprComponents/`.
- 2026-03-08: Story implementation complete — all 6 tasks (0-5) done. ServiceDefaults with full OpenTelemetry/health checks, 4 DAPR component YAML files, Aspire extension with AddHexalithTenants(), and AppHost Program.cs with DAPR topology wiring. Release build zero errors, 6 test projects zero failures.
- 2026-03-13: Code review complete — 0 HIGH, 0 MEDIUM, 2 LOW issues found and fixed. (1) Added missing `namespace: "default"` to accesscontrol.yaml commandapi policy for production compatibility. (2) Added inline comments to resiliency.yaml mirroring EventStore's documentation pattern. All 9 ACs verified. Build re-confirmed zero errors.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.6 (claude-opus-4-6)

### Debug Log References

None — clean implementation, no debugging required.

### Completion Notes List

- Task 0: All prerequisites verified — Story 1.1 builds clean, EventStore reference files (DaprComponents, ServiceDefaults, Aspire extension) all present and loaded.
- Task 1: ServiceDefaults Extensions.cs replaced placeholder with full implementation mirroring EventStore pattern. All 4 public methods + 1 private exporter method + internal JSON health writer. Allman braces per .editorconfig. Build verified zero errors.
- Task 2: Created 4 DAPR component YAML files in `src/Hexalith.Tenants.AppHost/DaprComponents/` — statestore (Redis, actorStateStore, scoped), pubsub (Redis, dead-letter, scoped), accesscontrol (allow-by-default with production warning), resiliency (retry/timeout/CB for apps and components).
- Task 3: Created Aspire extension — `HexalithTenantsExtensions.cs` with `AddHexalithTenants()` using `AddDaprComponent` (not `AddDaprStateStore` for metadata propagation), `AddDaprPubSub`, and `WithDaprSidecar` (no AppPort for Aspire Testing compatibility). `HexalithTenantsResources.cs` record for downstream use. Build verified zero errors.
- Task 4: Updated AppHost Program.cs with access control config path resolution (CWD + BaseDirectory fallback), CommandApi project resource, and `AddHexalithTenants()` call. Build verified zero errors.
- Task 5: Full verification — `dotnet build --configuration Release` zero errors across all projects. `dotnet test` discovers 6 test projects with zero failures. All 4 YAML files confirmed present.

### File List

**New files:**
- src/Hexalith.Tenants.AppHost/DaprComponents/statestore.yaml
- src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml
- src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.yaml
- src/Hexalith.Tenants.AppHost/DaprComponents/resiliency.yaml
- src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs
- src/Hexalith.Tenants.Aspire/HexalithTenantsResources.cs

**Modified files:**
- src/Hexalith.Tenants.ServiceDefaults/Extensions.cs (replaced placeholder with full implementation)
- src/Hexalith.Tenants.AppHost/Program.cs (replaced stub with full Aspire topology)
