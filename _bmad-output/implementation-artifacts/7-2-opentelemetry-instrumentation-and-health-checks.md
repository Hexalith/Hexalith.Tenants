# Story 7.2: OpenTelemetry Instrumentation & Health Checks

Status: done

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

- [x] Task 1: Create `TenantActivitySource` in CommandApi (AC: #1, #3)
- [x] 1.1: Create `src/Hexalith.Tenants.CommandApi/Telemetry/TenantActivitySource.cs` — static class following `EventStoreActivitySource` pattern with `SourceName = "Hexalith.Tenants"`, span names for command processing (`Tenants.Command.Process`), and tag constants for `tenants.command_type`, `tenants.tenant_id`, `tenants.success`
- [x] 1.2: Create `src/Hexalith.Tenants.CommandApi/Telemetry/TenantMetrics.cs` — static class using `System.Diagnostics.Metrics.Meter` with `MeterName = "Hexalith.Tenants"`. Two histogram instruments only (no separate counters — histograms natively track count, sum, and bucket distribution; any metrics backend can derive `rate(count[5m])` from the histogram): command latency histogram (`tenants.command.duration`, unit "ms") and projection query duration histogram (`tenants.projection.query.duration`, unit "ms"). Expose static `RecordCommandDuration` and `RecordQueryDuration` methods for clean call sites. Include a `private static readonly HashSet<string> s_knownCommandTypes` populated with the 12 tenant command type strings for metric dimension sanitization — unknown types fall back to `"unknown"`. Metric dimensions: `command_type` + `success` only (bounded cardinality). **NEVER** use `tenant_id` as a metric dimension — it is unbounded and belongs on trace spans only
- [x] 1.3: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 2: Instrument `DomainServiceRequestHandler` with command spans (AC: #1, #3)
- [x] 2.1: In `DomainServiceRequestHandler.ProcessAsync()`, wrap the **entire** `foreach` processor loop (including the final `InvalidOperationException` throw) in an `Activity` span from `TenantActivitySource` with `ActivityKind.Internal`. Start the span and `Stopwatch` **before** the loop. Add tags: `tenants.command_type` (from `request.Command.CommandType`) immediately, `tenants.tenant_id` (from request) immediately, `tenants.success` (true/false) after processing completes. On success (first processor returns), set `tenants.success=true`. If all processors throw mismatch and the final `InvalidOperationException` is thrown, set `Activity.Status = ActivityStatusCode.Error` and record the exception via `activity?.SetStatus(ActivityStatusCode.Error, ex.Message)`
- [x] 2.2: Record command latency via `TenantMetrics.RecordCommandDuration()` histogram (no separate counter — the histogram natively tracks count). Use `Stopwatch.StartNew()` **independently** from the Activity to measure duration — do NOT rely on Activity duration for the histogram (Activity includes listener overhead). Call `stopwatch.Stop()` after processing, then `TenantMetrics.RecordCommandDuration(stopwatch.Elapsed.TotalMilliseconds, commandType, success)`. The `RecordCommandDuration` method internally sanitizes `commandType` against the known-command-types set
- [x] 2.3: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 3: Instrument projection query processing (AC: #2, #3)
- [x] 3.1: In `TenantsProjectionActor.ExecuteQueryAsync()`, add an `Activity` span from `TenantActivitySource` with query type tag (`tenants.query_type`). Use `Stopwatch.StartNew()` to measure duration independently from the Activity. Record query duration via `TenantMetrics.RecordQueryDuration()` histogram (no separate counter). Note: event-to-projection ingestion metrics are already covered by EventStore's `EventsPublish` and `DomainServiceInvoke` spans — this task instruments the **query dispatch** path
- [x] 3.2: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 4: Register telemetry sources in ServiceDefaults (AC: #1, #2, #3)
- [x] 4.1: In `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs`, add `.AddSource("Hexalith.Tenants")` to tracing configuration (already present — verify) and add `.AddMeter("Hexalith.Tenants")` to metrics configuration for the custom metrics
- [x] 4.2: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 5: Add DAPR health checks for event store reachability (AC: #4, #5)
- [x] 5.1: Create `src/Hexalith.Tenants.CommandApi/Health/DaprStateStoreHealthCheck.cs` — single health check that calls `DaprClient.GetStateAsync<string>("statestore", "health-probe")` to verify state store reachability. Returns `HealthCheckResult.Healthy()` on success, `HealthCheckResult.Unhealthy(...)` on any `Exception` (not just `DaprException` — also catches `TaskCanceledException`, `HttpRequestException`, etc. for robustness). A separate sidecar liveness check is unnecessary — the state store probe goes through the sidecar, so if the sidecar is down the probe fails too. One check, one signal, no redundancy
- [x] 5.2: In `src/Hexalith.Tenants.CommandApi/Program.cs`, register the health check **AFTER** `builder.Services.AddDaprClient()` (the check depends on `DaprClient` via DI — registering before `AddDaprClient()` causes DI resolution failure). Use `failureStatus: HealthStatus.Degraded` to avoid startup flapping (during Aspire boot, the sidecar/state store may not be ready yet — Degraded returns 200 OK per the existing `MapDefaultEndpoints` status code mapping, preventing Aspire topology test flakes): `builder.Services.AddHealthChecks().AddCheck<DaprStateStoreHealthCheck>("dapr-statestore", failureStatus: HealthStatus.Degraded, tags: ["ready"])`. Tagged `["ready"]` for Kubernetes readiness probe — DAPR outages should stop traffic routing, NOT trigger pod restarts
- [x] 5.3: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [x] Task 6: Create unit tests for telemetry instrumentation (AC: #1, #2)
- [x] 6.1: Add `<ProjectReference>` from `tests/Hexalith.Tenants.Server.Tests/` to `src/Hexalith.Tenants.CommandApi/` if not already present — needed because `TenantActivitySource`, `TenantMetrics`, and `DomainServiceRequestHandler` are all in CommandApi (Server.Tests already has `InternalsVisibleTo` from CommandApi)
- [x] 6.2: Create `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantActivitySourceTests.cs` — verify activity source creates spans with expected names and tags when called directly. Use `ActivityListener` to capture activities in-process
- [x] 6.3: Create `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantMetricsTests.cs` — verify metrics are recorded with expected dimensions using `System.Diagnostics.Metrics.MeterListener`
- [x] 6.4: Create `tests/Hexalith.Tenants.Server.Tests/Telemetry/DomainServiceRequestHandlerTelemetryTests.cs` — **wiring test**: create a real `DomainServiceRequestHandler` with a mock `IDomainProcessor` (via NSubstitute), call `ProcessAsync()`, and assert via `ActivityListener` that the tenant command span was started with the correct tags. This verifies the handler actually invokes the telemetry, not just that the telemetry classes work in isolation
- [x] 6.5: Create `tests/Hexalith.Tenants.Server.Tests/Health/DaprStateStoreHealthCheckTests.cs` — test `DaprStateStoreHealthCheck` with a mocked `DaprClient` (NSubstitute): mock `GetStateAsync` returning normally → Healthy; mock throwing `DaprException` → Unhealthy; mock throwing `TaskCanceledException` → Unhealthy (verifies catch-all `Exception` handler)
- [x] 6.6: Verify all tests pass: `dotnet test Hexalith.Tenants.slnx --configuration Release --filter "Category!=Integration"` — expect 391+N tests (N = new telemetry + health check tests)

- [x] Task 7: Full solution validation
- [x] 7.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
- [x] 7.2: `dotnet test Hexalith.Tenants.slnx --configuration Release --filter "Category!=Integration"` — all Tier 1+2 tests pass

## Dev Notes

### Architecture Context

The architecture specifies: "OpenTelemetry via EventStore's telemetry infrastructure. Provided by EventStore: EventStoreActivitySource, ServiceDefaults OpenTelemetry configuration." The EventStore already instruments its internal pipeline (actor command processing, event persistence, event publication, state rehydration). Story 7.2 adds **tenant-specific** telemetry at the Tenants service layer on top of the EventStore's infrastructure-level tracing.

### What Already Exists (DO NOT Recreate)

| Component                            | File                                                                 | Status                                                                                                       |
| ------------------------------------ | -------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| ServiceDefaults with OpenTelemetry   | `src/Hexalith.Tenants.ServiceDefaults/Extensions.cs`                 | Complete — configures OTLP, ASP.NET Core tracing, health endpoints                                           |
| Health check endpoints               | ServiceDefaults `MapDefaultEndpoints()`                              | Complete — `/health`, `/alive`, `/ready` with JSON response in dev                                           |
| EventStore ActivitySource            | `Hexalith.EventStore.Server.Telemetry.EventStoreActivitySource`      | Complete — `"Hexalith.EventStore"` source with pipeline spans                                                |
| EventStore CommandApi ActivitySource | `Hexalith.EventStore.CommandApi.Telemetry.EventStoreActivitySources` | Complete — `"Hexalith.EventStore.CommandApi"` source                                                         |
| Trace source registration            | ServiceDefaults `ConfigureOpenTelemetry()`                           | Already registers `"Hexalith.Tenants.CommandApi"`, `"Hexalith.Tenants"`, and `"Hexalith.EventStore"` sources |
| Health check with JSON response      | ServiceDefaults `WriteHealthCheckJsonResponse()`                     | Complete — detailed JSON in dev, minimal in prod                                                             |

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
// NOTE: This is a structural guide, not copy-paste code. Adapt to the actual class structure.
internal static class TenantMetrics
{
    private static readonly Meter s_meter = new("Hexalith.Tenants");

    // FR54: Command latency histogram (no separate counter — histogram tracks count natively)
    private static readonly Histogram<double> s_commandDuration =
        s_meter.CreateHistogram<double>("tenants.command.duration", "ms", "Tenant command processing duration");

    // NFR2: Projection query processing latency (supplements ASP.NET Core request metrics)
    // Note: FR55 "event processing metrics" are covered by EventStore's pipeline spans
    // (EventsPublish, DomainServiceInvoke). These query metrics serve NFR2 monitoring.
    private static readonly Histogram<double> s_projectionQueryDuration =
        s_meter.CreateHistogram<double>("tenants.projection.query.duration", "ms", "Projection query processing duration");

    // Known command types for metric dimension sanitization (prevents cardinality attacks)
    private static readonly HashSet<string> s_knownCommandTypes = new(StringComparer.Ordinal)
    {
        "CreateTenant", "UpdateTenantInformation", "DisableTenant", "EnableTenant",
        "AddUserToTenant", "RemoveUserFromTenant", "ChangeUserRole",
        "SetTenantConfiguration", "RemoveTenantConfiguration",
        "AddGlobalAdministrator", "RemoveGlobalAdministrator",
        "RegisterGlobalAdministrator",
    };

    private static string SanitizeCommandType(string commandType)
        => s_knownCommandTypes.Contains(commandType) ? commandType : "unknown";

    public static void RecordCommandDuration(double milliseconds, string commandType, bool success)
        => s_commandDuration.Record(milliseconds,
            new KeyValuePair<string, object?>("command_type", SanitizeCommandType(commandType)),
            new KeyValuePair<string, object?>("success", success));

    public static void RecordQueryDuration(double milliseconds, string queryType)
        => s_projectionQueryDuration.Record(milliseconds,
            new KeyValuePair<string, object?>("query_type", queryType));
}
```

**CRITICAL:** Register the meter in ServiceDefaults: `.AddMeter("Hexalith.Tenants")` in the `.WithMetrics()` chain.

### DomainServiceRequestHandler Instrumentation

The `DomainServiceRequestHandler` (`src/Hexalith.Tenants.CommandApi/DomainProcessing/DomainServiceRequestHandler.cs`) is the command entry point. It iterates over `IDomainProcessor` instances and delegates processing. Wrap the **entire** `foreach` loop (including the final exception) in an `Activity` span with independent `Stopwatch` timing:

```csharp
// NOTE: Structural guide — adapt to the actual handler structure.
using Activity? activity = TenantActivitySource.Instance.StartActivity(
    TenantActivitySource.CommandProcess, ActivityKind.Internal);
var stopwatch = Stopwatch.StartNew();
bool success = false;
string commandType = request.Command.CommandType;

activity?.SetTag(TenantActivitySource.TagCommandType, commandType);
activity?.SetTag(TenantActivitySource.TagTenantId, /* tenant ID from request */);

try
{
    // ... existing foreach processor loop ...
    success = true;
    activity?.SetTag(TenantActivitySource.TagSuccess, true);
    return wireResult;
}
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.SetTag(TenantActivitySource.TagSuccess, false);
    throw;
}
finally
{
    stopwatch.Stop();
    TenantMetrics.RecordCommandDuration(stopwatch.Elapsed.TotalMilliseconds, commandType, success);
}
```

The EventStore's pipeline already creates child spans for `ProcessCommand`, `DomainServiceInvoke`, `EventsPersist`, `EventsPublish` — the tenant-level span becomes the **parent** for the EventStore pipeline spans, giving a complete trace tree.

**CRITICAL: Stopwatch vs Activity duration** — Use `Stopwatch` independently from the Activity for histogram recording. Activity duration includes listener overhead and is not suitable for precise latency histograms.

**CRITICAL: `tenants.success` semantics** — `tenants.success=true` means "the handler completed without throwing" — it does NOT mean "the command succeeded at domain level." A command that is domain-rejected (e.g., `CreateTenant` for a duplicate ID) returns a `DomainResult.Rejection` which is a successful handler return (`success=true`). Domain-level success/rejection is visible in the EventStore's existing spans (`eventstore.command_type` + result events). This is the correct abstraction boundary — the handler-level span measures infrastructure latency, not business outcome.

### DAPR Health Check Pattern

AC #5 requires degraded/unhealthy status when the event store is unreachable. A single state store probe check is sufficient — a separate sidecar liveness check is unnecessary because the state store probe goes through the sidecar (if the sidecar is down, the probe fails too).

**ADR: Single health check, not dual.** Two checks that can never independently fail provide no additional signal. One check, one signal, no redundancy.

```csharp
// NOTE: Structural guide — adapt to actual implementation.
internal sealed class DaprStateStoreHealthCheck(DaprClient daprClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Lightweight read of a non-existent key — verifies both sidecar AND state store
            await daprClient.GetStateAsync<string>("statestore", "health-probe", cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)  // Catch Exception, not just DaprException — also handles
        {                      // TaskCanceledException, HttpRequestException, etc.
            return HealthCheckResult.Unhealthy("DAPR state store is unreachable", ex);
        }
    }
}
```

Register with `["ready"]` tag so it appears on `/ready` endpoint (Kubernetes readiness probe). The `/alive` endpoint uses `["live"]` tag and only checks the self-check — a DAPR outage should not cause Kubernetes to kill the pod (it should just stop traffic routing). Use `failureStatus: HealthStatus.Degraded` as a safety net for any unhandled exceptions (the check itself should never throw — it catches `Exception` — but `failureStatus` provides defense-in-depth).

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

| NFR   | Target                        | Metric                                                     |
| ----- | ----------------------------- | ---------------------------------------------------------- |
| NFR1  | Tenant commands < 50ms p95    | `tenants.command.duration` histogram                       |
| NFR2  | Read model queries < 50ms p95 | ASP.NET Core instrumentation (already via ServiceDefaults) |
| NFR3  | Event publication < 50ms p95  | EventStore's `EventsPublish` span (already instrumented)   |
| NFR22 | API 99.9% availability        | `/health` endpoint uptime monitoring                       |

NFR1 is measured by the new `tenants.command.duration` histogram. NFR2 is covered by the existing ASP.NET Core request duration metrics, supplemented by the new `tenants.projection.query.duration` histogram for projection-specific latency. NFR3 is already measurable via EventStore's existing `EventsPublish` activity span — this story does NOT add new instrumentation for NFR3; it is included in AC #3 to confirm the telemetry data is complete for platform engineer monitoring. NFR22 is covered by the existing health check endpoints (already functional), now enhanced with DAPR reachability probes.

### File Structure

```text
src/Hexalith.Tenants.CommandApi/
  ├── Telemetry/
  │   ├── TenantActivitySource.cs           # NEW — activity source for tenant spans
  │   └── TenantMetrics.cs                  # NEW — custom metrics (command + projection)
  ├── Health/
  │   └── DaprStateStoreHealthCheck.cs      # NEW — single DAPR state store reachability probe
  ├── DomainProcessing/
  │   └── DomainServiceRequestHandler.cs    # MODIFY — add telemetry spans + metrics
  ├── Actors/
  │   └── TenantsProjectionActor.cs         # MODIFY — add projection metrics
  └── Program.cs                            # MODIFY — register health check

src/Hexalith.Tenants.ServiceDefaults/
  └── Extensions.cs                         # MODIFY — add .AddMeter("Hexalith.Tenants")

tests/Hexalith.Tenants.Server.Tests/
  ├── Hexalith.Tenants.Server.Tests.csproj  # MODIFY — add CommandApi project reference
  ├── Telemetry/
  │   ├── TenantActivitySourceTests.cs                  # NEW — activity span tests
  │   ├── TenantMetricsTests.cs                         # NEW — metrics recording tests
  │   └── DomainServiceRequestHandlerTelemetryTests.cs  # NEW — wiring test (handler → telemetry)
  └── Health/
      └── DaprStateStoreHealthCheckTests.cs              # NEW — health check unit tests (mocked DaprClient)
```

### Critical Anti-Patterns (DO NOT)

- **DO NOT** modify EventStore's `EventStoreActivitySource` or `EventStoreActivitySources` — those belong to the EventStore submodule
- **DO NOT** change the existing trace sources registered in ServiceDefaults (lines 60-62) — they are correct
- **DO NOT** add OpenTelemetry NuGet packages to CommandApi.csproj — the `System.Diagnostics.ActivitySource` and `System.Diagnostics.Metrics` APIs are built into .NET. The OpenTelemetry packages are only in ServiceDefaults for OTLP export configuration
- **DO NOT** create a custom health check that directly calls Redis/EventStore bypassing DAPR — use `DaprClient.GetStateAsync()` which goes through the sidecar (the correct abstraction layer per DAPR architecture). Do NOT use `DaprClient.CheckHealthAsync()` alone — it only checks sidecar liveness, not state store reachability
- **DO NOT** register the DAPR health check with `["live"]` tag — DAPR outages should not trigger pod restarts, only stop traffic routing (`["ready"]` tag)
- **DO NOT** modify existing test files — add NEW test files only
- **DO NOT** add telemetry to the Sample project — it's a lightweight consuming service (per Story 7.1 dev notes)
- **DO NOT** instrument every method — only the command entry point (`DomainServiceRequestHandler.ProcessAsync`) and the projection query dispatch (`TenantsProjectionActor.ExecuteQueryAsync`). The EventStore pipeline handles the rest internally
- **DO NOT** use `tenant_id` as a metric dimension (histogram/counter tag) — it is a **trace tag only**. `tenant_id` is unbounded cardinality (grows with each new tenant) and will blow up any metrics backend (Prometheus, OTLP). Metric dimensions must be bounded: `command_type` (12 types) × `success` (2 values) = 24 series maximum. `tenant_id` belongs on Activity spans for per-request trace correlation, never on Meter instruments
- **DO NOT** pass `request.Command.CommandType` directly as a metric dimension without sanitization. `CommandType` is a string from the HTTP request body (user-controlled). An attacker could send thousands of unique command types, creating unbounded metric cardinality. Before recording metrics, validate `commandType` against a known set of command types (the 12 tenant commands). If unrecognized, use `"unknown"` as the dimension value. The trace span tag CAN use the raw value (traces are sampled, not aggregated like metrics)
- **DO NOT** rely on `Activity` duration for `Histogram<double>.Record()` — use independent `Stopwatch.StartNew()` timing. Activity duration includes OpenTelemetry listener overhead and is not suitable for precise latency measurement

### Code Style Requirements

- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman brace style (new line before opening brace)
- `ArgumentNullException.ThrowIfNull()` on public method parameters
- `TreatWarningsAsErrors = true` — zero warnings allowed
- 4-space indentation, CRLF line endings, UTF-8
- XML doc comments (`/// <summary>`) on public API types
- Use `internal` visibility for telemetry classes (not part of public API)

### Testing Strategy

**Three test layers, all Tier 1 (no infrastructure required):**

1. **Telemetry class tests** (`TenantActivitySourceTests`, `TenantMetricsTests`) — verify that telemetry classes emit correctly when called directly. Use .NET's built-in diagnostic listeners:
    - `ActivityListener` — capture `Activity` objects to verify span names, tags, and status
    - `MeterListener` — capture metric recordings to verify histogram/counter values and dimensions

2. **Wiring test** (`DomainServiceRequestHandlerTelemetryTests`) — create a real `DomainServiceRequestHandler` with a mock `IDomainProcessor` (NSubstitute), call `ProcessAsync()`, and assert via `ActivityListener` that the tenant command span was started with the correct tags AND via `MeterListener` that the histogram was recorded. This is critical — without this test, the telemetry classes could be perfect but never invoked.

3. **Health check tests** (`DaprStateStoreHealthCheckTests`) — mock `DaprClient` with NSubstitute, verify the single health check returns correct `HealthCheckResult` for three scenarios: success → Healthy, `DaprException` → Unhealthy, `TaskCanceledException` → Unhealthy. Tier 1 (no DAPR sidecar needed).

**Test placement:** `Server.Tests` project needs a `<ProjectReference>` to `CommandApi` since `TenantActivitySource`, `TenantMetrics`, `DomainServiceRequestHandler`, and the health checks are all in CommandApi. CommandApi already declares `InternalsVisibleTo` for `Hexalith.Tenants.Server.Tests`.

**Regression baseline:** Story 7.1 ended at 391 Tier 1 tests. Task 7 should verify 391+N pass (N = new telemetry + health check tests).

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

| Technology                        | Version     | Source                   |
| --------------------------------- | ----------- | ------------------------ |
| .NET SDK                          | 10.0.103    | global.json              |
| OpenTelemetry.\*                  | 1.15.0      | Directory.Packages.props |
| DAPR SDK                          | 1.17.3      | Directory.Packages.props |
| Aspire.Hosting                    | 13.1.2      | Directory.Packages.props |
| System.Diagnostics.ActivitySource | .NET 10 BCL | Built-in                 |
| System.Diagnostics.Metrics        | .NET 10 BCL | Built-in                 |

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

Claude Opus 4.6 (1M context)

### Debug Log References

- Test parallelism issue: `ActivitySource` listeners are global. Telemetry tests from different classes running in parallel captured each other's activities. Fixed by adding `[Collection("Telemetry")]` to serialize telemetry test classes.
- `using` declaration vs manual disposal: `using Activity?` declaration caused the activity to be disposed before the `finally` block's `SetTag` could take effect (observed on .NET 10). Fixed by switching to manual `activity?.Dispose()` in the `finally` block.

### Completion Notes List

- Task 1: Created `TenantActivitySource.cs` following `EventStoreActivitySource` pattern with `SourceName = "Hexalith.Tenants"`, span names for command processing and query execution, and tag constants. Created `TenantMetrics.cs` with `Meter` named `"Hexalith.Tenants"`, command duration and projection query duration histograms, and command type sanitization against 12 known types.
- Task 2: Instrumented `DomainServiceRequestHandler.ProcessAsync()` with an `Activity` span wrapping the entire foreach loop, independent `Stopwatch` timing, and command/tenant tags. Success tag set in `finally` block. Error status set on exception.
- Task 3: Instrumented `TenantsProjectionActor.ExecuteQueryAsync()` with an `Activity` span and query duration histogram recording via `TenantMetrics.RecordQueryDuration()`.
- Task 4: Added `.AddMeter("Hexalith.Tenants")` to ServiceDefaults `WithMetrics()` chain. Verified existing trace source registrations are correct.
- Task 5: Created `DaprStateStoreHealthCheck.cs` — single health check probing DAPR state store via `DaprClient.GetStateAsync`. Catches `Exception` (not just `DaprException`) for robustness. Registered with `failureStatus: HealthStatus.Degraded` and `["ready"]` tag.
- Task 6: Created 4 new test files with 31 new tests (422 total Tier 1+2, up from 391). All telemetry classes, handler wiring, metrics dimensions, and health check scenarios covered. Used `[Collection("Telemetry")]` to prevent ActivitySource listener cross-talk.
- Task 7: Full solution build 0 warnings 0 errors. All 422 Tier 1+2 tests pass. 2 pre-existing Tier 3 failures unchanged.
- Follow-up review fixes: registered `Hexalith.EventStore` as an OpenTelemetry trace source in `ServiceDefaults`, added query metric dimension sanitization for bounded `query_type` cardinality, and marked projection query spans as `Error` when handlers throw.
- Follow-up test coverage: added `TenantsProjectionActorTelemetryTests.cs` to validate projection query spans, query metric sanitization, and failure-path telemetry. Adjusted telemetry test assertions to select the intended activity/metric recordings under global listener contention. Verified `Hexalith.Tenants.Server.Tests` passes with 237 tests.
- Status note: review completed. AC #5 explicitly allows dependency failures to report as degraded or unhealthy, and the implemented state-store probe behavior satisfies that acceptance criterion.

### File List

New files:

- src/Hexalith.Tenants.CommandApi/Telemetry/TenantActivitySource.cs
- src/Hexalith.Tenants.CommandApi/Telemetry/TenantMetrics.cs
- src/Hexalith.Tenants.CommandApi/Health/DaprStateStoreHealthCheck.cs
- tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantActivitySourceTests.cs
- tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantMetricsTests.cs
- tests/Hexalith.Tenants.Server.Tests/Telemetry/DomainServiceRequestHandlerTelemetryTests.cs
- tests/Hexalith.Tenants.Server.Tests/Health/DaprStateStoreHealthCheckTests.cs
- tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantsProjectionActorTelemetryTests.cs

Modified files:

- src/Hexalith.Tenants.CommandApi/DomainProcessing/DomainServiceRequestHandler.cs
- src/Hexalith.Tenants.CommandApi/Actors/TenantsProjectionActor.cs
- src/Hexalith.Tenants.CommandApi/Program.cs
- src/Hexalith.Tenants.ServiceDefaults/Extensions.cs
- tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantMetricsTests.cs

### Change Log

- 2026-03-18: Implemented Story 7.2 — OpenTelemetry instrumentation (tenant command spans, projection query spans, command duration histogram, query duration histogram) and DAPR state store health check with 31 new tests.
- 2026-03-18: Applied post-review fixes — registered `Hexalith.EventStore` tracing, sanitized `query_type` metric dimensions, added projection query error spans, added projection telemetry regression tests, and closed review after confirming AC #5 accepts degraded or unhealthy dependency failure reporting.
    <!-- End of story 7.2 -->
    <!-- EOF -->
