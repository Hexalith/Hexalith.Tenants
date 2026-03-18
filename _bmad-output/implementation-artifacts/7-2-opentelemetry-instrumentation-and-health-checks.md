# Story 7.2: OpenTelemetry Instrumentation & Health Checks

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform engineer,
I want tenant command latency and event processing metrics exposed via OpenTelemetry and a health check endpoint,
So that I can monitor service performance and availability in production.

## Acceptance Criteria

1. **Given** the tenant service is deployed with OpenTelemetry configured via ServiceDefaults
   **When** a tenant command is processed
   **Then** an OpenTelemetry span is emitted measuring command latency with attributes for command type, tenant ID, and success/failure status

2. **Given** the tenant service is serving projection queries
   **When** a query is dispatched through the projection actor
   **Then** OpenTelemetry metrics are emitted for query processing duration and query count, and a trace span is emitted with the query type attribute

3. **Given** the OpenTelemetry metrics are collected
   **When** a platform engineer inspects the telemetry data
   **Then** command latency (NFR1) and event publication latency (NFR3) are measurable at p95 against the 50ms target

4. **Given** the tenant service is deployed
   **When** a GET request is sent to the health check endpoint
   **Then** a 200 OK response is returned indicating the service is healthy and available for uptime monitoring (NFR22: 99.9% target)

5. **Given** the health check endpoint
   **When** the event store is unreachable
   **Then** the health check reports degraded or unhealthy status

## Tasks / Subtasks

- [ ] Task 1: Create `TenantActivitySource` in CommandApi (AC: #1, #3)
  - [ ] 1.1: Create `src/Hexalith.Tenants.CommandApi/Telemetry/TenantActivitySource.cs` — static class following `EventStoreActivitySource` pattern with `SourceName = "Hexalith.Tenants"`, span names for command processing (`Tenants.Command.Process`), and tag constants for `tenants.command_type`, `tenants.tenant_id`, `tenants.success`
  - [ ] 1.2: Create `src/Hexalith.Tenants.CommandApi/Telemetry/TenantMetrics.cs` — static class using `System.Diagnostics.Metrics.Meter` for command latency histogram (`tenants.command.duration`) and command count counter (`tenants.command.count`), plus event processing metrics (`tenants.projection.event.duration`, `tenants.projection.event.count`)
  - [ ] 1.3: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 2: Instrument `DomainServiceRequestHandler` with command spans (AC: #1, #3)
  - [ ] 2.1: In `DomainServiceRequestHandler.ProcessAsync()`, wrap the **entire** `foreach` processor loop (including the final `InvalidOperationException` throw) in an `Activity` span from `TenantActivitySource` with `ActivityKind.Internal`. Start the span and `Stopwatch` **before** the loop. Add tags: `tenants.command_type` (from `request.Command.CommandType`) immediately, `tenants.tenant_id` (from request) immediately, `tenants.success` (true/false) after processing completes. On success (first processor returns), set `tenants.success=true`. If all processors throw mismatch and the final `InvalidOperationException` is thrown, set `Activity.Status = ActivityStatusCode.Error` and record the exception via `activity?.SetStatus(ActivityStatusCode.Error, ex.Message)`
  - [ ] 2.2: Record command count and latency via `TenantMetrics` histogram/counter with dimensions for command type and success. Use `Stopwatch.StartNew()` **independently** from the Activity to measure duration — do NOT rely on Activity duration for the histogram (Activity includes listener overhead). Call `stopwatch.Stop()` after processing, then `TenantMetrics.RecordCommandDuration(stopwatch.Elapsed.TotalMilliseconds, commandType, success)`
  - [ ] 2.3: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 3: Instrument projection query processing (AC: #2, #3)
  - [ ] 3.1: In `TenantsProjectionActor.ExecuteQueryAsync()`, add an `Activity` span from `TenantActivitySource` with query type tag (`tenants.query_type`). Use `Stopwatch.StartNew()` to measure duration independently from the Activity. Record query count and duration via `TenantMetrics` (`tenants.projection.query.duration` histogram, `tenants.projection.query.count` counter). Note: event-to-projection ingestion metrics are already covered by EventStore's `EventsPublish` and `DomainServiceInvoke` spans — this task instruments the **query dispatch** path
  - [ ] 3.2: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 4: Register telemetry sources in ServiceDefaults (AC: #1, #2, #3)
  - [ ] 4.1: In `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs`, add `.AddSource("Hexalith.Tenants")` to tracing configuration (already present — verify) and add `.AddMeter("Hexalith.Tenants")` to metrics configuration for the custom metrics
  - [ ] 4.2: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 5: Add DAPR health checks for event store reachability (AC: #4, #5)
  - [ ] 5.1: Create `src/Hexalith.Tenants.CommandApi/Health/DaprHealthCheck.cs` with two checks: (a) `DaprSidecarHealthCheck` — calls `DaprClient.CheckHealthAsync()` to verify sidecar liveness, and (b) `DaprStateStoreHealthCheck` — calls `DaprClient.GetStateAsync<string>("statestore", "health-probe")` to verify state store reachability (returns Healthy if no exception, Unhealthy if `DaprException` is thrown). Both implement `IHealthCheck`. Note: `CheckHealthAsync()` only verifies sidecar liveness, NOT state store reachability — the state store probe covers AC #5's "event store is unreachable" requirement at the correct DAPR abstraction layer
  - [ ] 5.2: In `src/Hexalith.Tenants.CommandApi/Program.cs`, register both health checks after `builder.AddServiceDefaults()`: `builder.Services.AddHealthChecks().AddCheck<DaprSidecarHealthCheck>("dapr-sidecar", tags: ["ready"]).AddCheck<DaprStateStoreHealthCheck>("dapr-statestore", tags: ["ready"])`. Both tagged `["ready"]` for Kubernetes readiness probe — DAPR outages should stop traffic routing, NOT trigger pod restarts
  - [ ] 5.3: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 6: Create unit tests for telemetry instrumentation (AC: #1, #2)
  - [ ] 6.1: Create `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantActivitySourceTests.cs` — verify activity source creates spans with expected names and tags when processing commands. Use `ActivityListener` to capture activities in-process
  - [ ] 6.2: Create `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantMetricsTests.cs` — verify metrics are recorded with expected dimensions using `System.Diagnostics.Metrics.MeterListener`
  - [ ] 6.3: Verify all tests pass: `dotnet test Hexalith.Tenants.slnx --configuration Release --filter "Category!=Integration"`

- [ ] Task 7: Full solution validation
  - [ ] 7.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
  - [ ] 7.2: `dotnet test Hexalith.Tenants.slnx --configuration Release --filter "Category!=Integration"` — all Tier 1+2 tests pass

## Dev Notes

### Architecture Context

The architecture specifies: "OpenTelemetry via EventStore's telemetry infrastructure. Provided by EventStore: EventStoreActivitySource, ServiceDefaults OpenTelemetry configuration." The EventStore already instruments its internal pipeline (actor command processing, event persistence, event publication, state rehydration). Story 7.2 adds **tenant-specific** telemetry at the Tenants service layer on top of the EventStore's infrastructure-level tracing.

### What Already Exists (DO NOT Recreate)

| Component | File | Status |
|-----------|------|--------|
| ServiceDefaults with OpenTelemetry | `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs` | Complete — configures OTLP, ASP.NET Core tracing, health endpoints |
| Health check endpoints | ServiceDefaults `MapDefaultEndpoints()` | Complete — `/health`, `/alive`, `/ready` with JSON response in dev |
| EventStore ActivitySource | `Hexalith.EventStore.Server.Telemetry.EventStoreActivitySource` | Complete — `"Hexalith.EventStore"` source with pipeline spans |
| EventStore CommandApi ActivitySource | `Hexalith.EventStore.CommandApi.Telemetry.EventStoreActivitySources` | Complete — `"Hexalith.EventStore.CommandApi"` source |
| Trace source registration | ServiceDefaults `ConfigureOpenTelemetry()` | Already registers `"Hexalith.Tenants.CommandApi"`, `"Hexalith.Tenants"`, and `"Hexalith.EventStore"` sources |
| Health check with JSON response | ServiceDefaults `WriteHealthCheckJsonResponse()` | Complete — detailed JSON in dev, minimal in prod |

### EventStore ActivitySource Pattern (MUST Follow)

The EventStore's `EventStoreActivitySource` (`Hexalith.EventStore/src/Hexalith.EventStore.Server/Telemetry/EventStoreActivitySource.cs`) is the reference pattern:

```csharp
// Static class with const span names and tag keys
public static class EventStoreActivitySource
{
    public const string SourceName = "Hexalith.EventStore";
    public const string ProcessCommand = "EventStore.Actor.ProcessCommand";
    public const string TagCommandType = "eventstore.command_type";
    public const string TagTenantId = "eventstore.tenant_id";
    public static ActivitySource Instance { get; } = new(SourceName);
}
```

The `EventStoreActivitySources` in CommandApi (`Hexalith.EventStore/src/Hexalith.EventStore.CommandApi/Telemetry/EventStoreActivitySources.cs`) shows the CommandApi layer pattern:

```csharp
internal static class EventStoreActivitySources
{
    public const string Submit = "EventStore.CommandApi.Submit";
    public static readonly ActivitySource CommandApi = new("Hexalith.EventStore.CommandApi");
}
```

**Create `TenantActivitySource` following this exact pattern** — `SourceName = "Hexalith.Tenants"` (matches the `.AddSource("Hexalith.Tenants")` already in ServiceDefaults).

### .NET Metrics Pattern (System.Diagnostics.Metrics)

Use `System.Diagnostics.Metrics.Meter` for custom metrics (FR54, FR55). This is the .NET-native approach that integrates with OpenTelemetry via the `.AddMeter()` registration:

```csharp
internal static class TenantMetrics
{
    private static readonly Meter s_meter = new("Hexalith.Tenants");

    // FR54: Command latency histogram
    private static readonly Histogram<double> s_commandDuration =
        s_meter.CreateHistogram<double>("tenants.command.duration", "ms", "Tenant command processing duration");

    // FR54: Command count
    private static readonly Counter<long> s_commandCount =
        s_meter.CreateCounter<long>("tenants.command.count", "{commands}", "Total tenant commands processed");

    // FR55: Projection event processing
    private static readonly Histogram<double> s_projectionDuration =
        s_meter.CreateHistogram<double>("tenants.projection.event.duration", "ms", "Projection event processing duration");

    private static readonly Counter<long> s_projectionEventCount =
        s_meter.CreateCounter<long>("tenants.projection.event.count", "{events}", "Total projection events processed");
}
```

**CRITICAL:** Register the meter in ServiceDefaults: `.AddMeter("Hexalith.Tenants")` in the `.WithMetrics()` chain.

### DomainServiceRequestHandler Instrumentation

The `DomainServiceRequestHandler` (`src/Hexalith.Tenants.CommandApi/DomainProcessing/DomainServiceRequestHandler.cs`) is the command entry point. It iterates over `IDomainProcessor` instances and delegates processing. Wrap the `ProcessAsync` method body in an `Activity` span:

```csharp
using Activity? activity = TenantActivitySource.Instance.StartActivity(
    TenantActivitySource.CommandProcess, ActivityKind.Internal);

activity?.SetTag(TenantActivitySource.TagCommandType, request.Command.CommandType);
// ... process ...
activity?.SetTag(TenantActivitySource.TagSuccess, true);
```

The EventStore's pipeline already creates child spans for `ProcessCommand`, `DomainServiceInvoke`, `EventsPersist`, `EventsPublish` — the tenant-level span becomes the **parent** for the EventStore pipeline spans, giving a complete trace tree.

### DAPR Health Check Pattern

AC #5 requires degraded/unhealthy status when the event store is unreachable. The DAPR sidecar health check is the correct abstraction — if the sidecar is down, the state store (event store) is unreachable:

```csharp
internal sealed class DaprHealthCheck(DaprClient daprClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        bool healthy = await daprClient.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
        return healthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("DAPR sidecar is unreachable");
    }
}
```

Register with `["ready"]` tag so it appears on `/ready` endpoint (Kubernetes readiness probe). The `/alive` endpoint uses `["live"]` tag and only checks the self-check — a DAPR outage should not cause Kubernetes to kill the pod (it should just stop routing traffic).

### ServiceDefaults Modifications

The `ConfigureOpenTelemetry()` method in `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs` already registers trace sources `"Hexalith.Tenants.CommandApi"` and `"Hexalith.Tenants"` (lines 60-62). **ONLY** add `.AddMeter("Hexalith.Tenants")` to the `.WithMetrics()` chain:

```csharp
.WithMetrics(metrics => metrics
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddRuntimeInstrumentation()
    .AddMeter("Hexalith.Tenants"))  // ADD THIS
```

### NFR Targets for Telemetry Validation

| NFR | Target | Metric |
|-----|--------|--------|
| NFR1 | Tenant commands < 50ms p95 | `tenants.command.duration` histogram |
| NFR2 | Read model queries < 50ms p95 | ASP.NET Core instrumentation (already via ServiceDefaults) |
| NFR3 | Event publication < 50ms p95 | EventStore's `EventsPublish` span (already instrumented) |
| NFR22 | API 99.9% availability | `/health` endpoint uptime monitoring |

NFR1 is measured by the new `tenants.command.duration` histogram. NFR2 is covered by the existing ASP.NET Core request duration metrics. NFR3 is covered by EventStore's existing `EventsPublish` activity span. NFR22 is covered by the existing health check endpoints (already functional).

### File Structure

```
src/Hexalith.Tenants.CommandApi/
  ├── Telemetry/
  │   ├── TenantActivitySource.cs           # NEW — activity source for tenant spans
  │   └── TenantMetrics.cs                  # NEW — custom metrics (command + projection)
  ├── Health/
  │   └── DaprHealthCheck.cs                # NEW — DAPR sidecar health check
  ├── DomainProcessing/
  │   └── DomainServiceRequestHandler.cs    # MODIFY — add telemetry spans + metrics
  ├── Actors/
  │   └── TenantsProjectionActor.cs         # MODIFY — add projection metrics
  └── Program.cs                            # MODIFY — register health check

src/Hexalith.Tenants.ServiceDefaults/
  └── Extensions.cs                         # MODIFY — add .AddMeter("Hexalith.Tenants")

tests/Hexalith.Tenants.Server.Tests/
  └── Telemetry/
      ├── TenantActivitySourceTests.cs      # NEW — activity span tests
      └── TenantMetricsTests.cs             # NEW — metrics recording tests
```

### Critical Anti-Patterns (DO NOT)

- **DO NOT** modify EventStore's `EventStoreActivitySource` or `EventStoreActivitySources` — those belong to the EventStore submodule
- **DO NOT** change the existing trace sources registered in ServiceDefaults (lines 60-62) — they are correct
- **DO NOT** add OpenTelemetry NuGet packages to CommandApi.csproj — the `System.Diagnostics.ActivitySource` and `System.Diagnostics.Metrics` APIs are built into .NET. The OpenTelemetry packages are only in ServiceDefaults for OTLP export configuration
- **DO NOT** create a custom health check that directly calls Redis/EventStore — use `DaprClient.CheckHealthAsync()` which checks the sidecar (the correct abstraction layer per DAPR architecture)
- **DO NOT** register the DAPR health check with `["live"]` tag — DAPR outages should not trigger pod restarts, only stop traffic routing (`["ready"]` tag)
- **DO NOT** modify existing test files — add NEW test files only
- **DO NOT** add telemetry to the Sample project — it's a lightweight consuming service (per Story 7.1 dev notes)
- **DO NOT** instrument every method — only the command entry point (`DomainServiceRequestHandler.ProcessAsync`) and the projection query dispatch (`TenantsProjectionActor.ExecuteQueryAsync`). The EventStore pipeline handles the rest internally

### Code Style Requirements

- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman brace style (new line before opening brace)
- `ArgumentNullException.ThrowIfNull()` on public method parameters
- `TreatWarningsAsErrors = true` — zero warnings allowed
- 4-space indentation, CRLF line endings, UTF-8
- XML doc comments (`/// <summary>`) on public API types
- Use `internal` visibility for telemetry classes (not part of public API)

### Testing Strategy

Tests should use .NET's built-in diagnostic listeners:

- **ActivityListener** — capture `Activity` objects to verify span names, tags, and status
- **MeterListener** — capture metric recordings to verify histogram/counter values and dimensions
- Both run entirely in-process, no OTLP collector needed (Tier 1 tests)

Test placement in `Server.Tests` (not CommandApi — CommandApi is not directly testable via unit tests, but the telemetry classes can be tested via the public API of the handler/actor). **Alternative:** If `TenantActivitySource` and `TenantMetrics` are `internal` to CommandApi, use the existing `InternalsVisibleTo` for `Hexalith.Tenants.Server.Tests` (already declared in CommandApi.csproj).

### Project Structure Notes

- No new projects created — all changes in existing projects
- No new NuGet packages required — `System.Diagnostics.ActivitySource` and `System.Diagnostics.Metrics` are in .NET BCL
- ServiceDefaults already has all required OpenTelemetry packages
- CommandApi already has `InternalsVisibleTo` for Server.Tests and IntegrationTests

### Previous Story Intelligence (7.1)

Story 7.1 established:
- AppHost topology with CommandApi and Sample, both with DAPR sidecars
- Aspire topology smoke tests (3 tests) in IntegrationTests
- 391 Tier 1 tests pass; 2 pre-existing Tier 3 failures (expected — DAPR infrastructure)
- ServiceDefaults is fully functional with OpenTelemetry tracing, health endpoints, JSON console logging
- The Sample does NOT use ServiceDefaults — no OpenTelemetry from Sample

### Git Intelligence

Recent commits:
- `a1a9d53` — Refactor code structure for improved readability and maintainability (latest)
- `3e4ef10` — InMemoryTenantService and TenantTestHelpers for integration testing
- All epics 1-6 complete, epic 7 in progress with 7.1 in review

### Technology Versions

| Technology | Version | Source |
|-----------|---------|--------|
| .NET SDK | 10.0.103 | global.json |
| OpenTelemetry.* | 1.15.0 | Directory.Packages.props |
| DAPR SDK | 1.17.3 | Directory.Packages.props |
| Aspire.Hosting | 13.1.2 | Directory.Packages.props |
| System.Diagnostics.ActivitySource | .NET 10 BCL | Built-in |
| System.Diagnostics.Metrics | .NET 10 BCL | Built-in |

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 7.2] — Story definition, ACs
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 7] — Epic objectives: deployment & observability
- [Source: _bmad-output/planning-artifacts/prd.md#FR54] — Command latency metrics via OpenTelemetry
- [Source: _bmad-output/planning-artifacts/prd.md#FR55] — Event processing metrics via OpenTelemetry
- [Source: _bmad-output/planning-artifacts/prd.md#NFR1] — Commands < 50ms p95
- [Source: _bmad-output/planning-artifacts/prd.md#NFR3] — Event publication < 50ms p95
- [Source: _bmad-output/planning-artifacts/prd.md#NFR22] — 99.9% API availability via health check
- [Source: _bmad-output/planning-artifacts/architecture.md#Infrastructure & Deployment] — OpenTelemetry via EventStore's telemetry infrastructure
- [Source: _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines] — Structured logging with semantic parameters
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Telemetry/EventStoreActivitySource.cs] — Reference pattern for ActivitySource
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.CommandApi/Telemetry/EventStoreActivitySources.cs] — Reference pattern for CommandApi telemetry
- [Source: src/Hexalith.Tenants.ServiceDefaults/Extensions.cs] — Existing OpenTelemetry + health checks
- [Source: src/Hexalith.Tenants.CommandApi/DomainProcessing/DomainServiceRequestHandler.cs] — Command processing entry point (instrumentation target)
- [Source: src/Hexalith.Tenants.CommandApi/Actors/TenantsProjectionActor.cs] — Projection query dispatch (instrumentation target)
- [Source: _bmad-output/implementation-artifacts/7-1-aspire-hosting-and-apphost.md] — Previous story learnings

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
