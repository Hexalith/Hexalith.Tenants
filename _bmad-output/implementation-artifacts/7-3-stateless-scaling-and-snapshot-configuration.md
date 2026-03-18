# Story 7.3: Stateless Scaling & Snapshot Configuration

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform engineer,
I want the tenant service to be stateless with configurable snapshot intervals and graceful degradation,
So that I can scale horizontally, restart without data loss, and maintain operations during infrastructure partial failures.

## Acceptance Criteria

1. **Given** the tenant service is running
   **When** the service is restarted
   **Then** all tenant state is reconstructed from the event store -- no data loss, no migration scripts, no data seeding required (NFR12, NFR20)

2. **Given** the tenant service is configured with snapshot interval of 50 events for tenant domain
   **When** a tenant aggregate accumulates more than 50 events
   **Then** a snapshot is persisted and subsequent actor rehydration replays at most 50 events from the last snapshot

3. **Given** the GlobalAdministratorAggregate uses the default snapshot interval of 100 events
   **When** the aggregate is rehydrated
   **Then** snapshots are created at the 100-event interval appropriate for its low event volume

4. **Given** DAPR pub/sub is temporarily unavailable
   **When** a command is processed
   **Then** the command succeeds and events are stored in the event store; when pub/sub recovers, subscribers receive all pending events (NFR17)

5. **Given** a Tier 3 integration test
   **When** pub/sub is disabled, commands are executed, and pub/sub is re-enabled
   **Then** subscribers receive all events that were stored during the outage

6. **Given** a snapshot performance test seeded with 500,000 events (1,000 tenants x 500 events average) with 50-event snapshot interval
   **When** a cold-start actor rehydration is measured
   **Then** state reconstruction completes within 30 seconds (NFR13) -- this test runs on nightly CI schedule, not on every PR

## Tasks / Subtasks

- [ ] Task 1: Configure snapshot interval for tenant domain (AC: #2, #3)
  - [ ] 1.1: In `src/Hexalith.Tenants.CommandApi/appsettings.json`, add the `EventStore:Snapshots` section with `DomainIntervals` setting `tenants` to `50`. The `DefaultInterval` stays at `100` (EventStore's default in `SnapshotOptions`) — this covers GlobalAdministratorAggregate's low event volume. Do NOT set `TenantDomainIntervals` — the `system` tenant uses the same domain interval as any other tenant
  - [ ] 1.2: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 2: Create snapshot configuration unit test (AC: #2, #3)
  - [ ] 2.1: Create `tests/Hexalith.Tenants.Server.Tests/Configuration/SnapshotConfigurationTests.cs` — test that verifies the `appsettings.json` snapshot configuration loads correctly into `SnapshotOptions`. Use `ConfigurationBuilder` with `AddJsonFile("appsettings.json")` and bind to `SnapshotOptions`. Assert: `DomainIntervals["tenants"] == 50`, `DefaultInterval == 100`. This is a Tier 1 test (no infrastructure). Copy `appsettings.json` to test output via `<Content Include="...appsettings.json" CopyToOutputDirectory="PreserveNewest" Link="appsettings.json" />` in test csproj
  - [ ] 2.2: Verify build and test: `dotnet test Hexalith.Tenants.slnx --configuration Release --filter "Category!=Integration"`

- [ ] Task 3: Create stateless restart verification test (AC: #1)
  - [ ] 3.1: Create `tests/Hexalith.Tenants.IntegrationTests/StatelessRestartTests.cs` — Tier 2 integration test using the existing `TenantsDaprTestFixture`. Send a `CreateTenant` command via actor proxy, verify accepted. Then create a **new** actor proxy for the same actor ID (simulating restart/rehydration — DAPR actors deactivate and reactivate, replaying state from the state store). Send `DisableTenant` to the same tenant, verify accepted (proves state was reconstructed from event store). Mark with `[Trait("Category", "Integration")]`
  - [ ] 3.2: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 4: Create graceful degradation verification test (AC: #4, #5)
  - [ ] 4.1: Create `tests/Hexalith.Tenants.IntegrationTests/GracefulDegradationTests.cs` — Tier 2 integration test using the existing `TenantsDaprTestFixture`. The test verifies that commands succeed even when pub/sub publication fails. The EventStore's `AggregateActor` already handles this: when pub/sub fails, events are persisted in the state store and a drain reminder is scheduled (Story 4.4 drain recovery). The test: (1) Configure the `FakeEventPublisher` in the fixture to throw on publish (simulating pub/sub outage), (2) Send `CreateTenant` command via actor proxy, (3) Verify the command result indicates events were persisted (`EventCount == 1`) even though publication failed (`result.Status` may be `PublishFailed` — this is expected terminal state, NOT a failure for AC #4 — the key assertion is that events are **persisted**, not lost), (4) Reset the `FakeEventPublisher` to succeed, (5) Trigger drain recovery (invoke the actor's reminder mechanism or wait for auto-drain), (6) Verify events are eventually published. Mark with `[Trait("Category", "Integration")]`
  - [ ] 4.2: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 5: Create snapshot performance test scaffold (AC: #6)
  - [ ] 5.1: Create `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs` — Tier 3 performance test. Mark with `[Trait("Category", "Performance")]` for CI filtering (nightly schedule only, NOT on every PR). The test: (1) Seed 1,000 tenant aggregates via actor proxies with 500 events each (Create + 499 Update/AddUser/SetConfig commands per tenant) — use parallel Task execution with `SemaphoreSlim` to limit concurrency, (2) Deactivate all actors (force cold-start), (3) Measure cold-start rehydration time for a single actor: create a new actor proxy and send a command (triggers full rehydration from snapshot + tail events), (4) Assert rehydration completes in under 30 seconds. Note: This test requires a running DAPR sidecar with Redis state store. The 50-event snapshot interval means each tenant has ~10 snapshots, and rehydration replays at most 50 events from the last snapshot
  - [ ] 5.2: Verify build: `dotnet build Hexalith.Tenants.slnx --configuration Release`

- [ ] Task 6: Full solution validation
  - [ ] 6.1: `dotnet build Hexalith.Tenants.slnx --configuration Release` — 0 warnings, 0 errors
  - [ ] 6.2: `dotnet test Hexalith.Tenants.slnx --configuration Release --filter "Category!=Integration&Category!=Performance"` — all Tier 1+2 tests pass (422+N, N = new snapshot config test)
  - [ ] 6.3: Verify no existing tests broken by configuration changes

## Dev Notes

### Architecture Context

This story validates three architectural properties that are **already implemented by EventStore's infrastructure**:

1. **Stateless service architecture** (FR57, NFR12): The tenant service stores no local state between requests. All state is reconstructed from the event store via DAPR actor state rehydration (snapshot + tail event replay). EventStore's `AggregateActor` handles this automatically.

2. **Configurable snapshot intervals** (NFR13): EventStore's `SnapshotManager` creates snapshots at configurable intervals. The tenant domain needs a 50-event interval (tenants grow with user/config additions, up to ~1000 events at max capacity). GlobalAdministratorAggregate uses the default 100-event interval (singleton, very low event volume).

3. **Graceful degradation** (NFR17): EventStore's `AggregateActor` already implements pub/sub failure handling: events are persisted atomically in the state store, and a drain reminder (Story 4.4) retries publication when pub/sub recovers. Commands succeed even during pub/sub outages.

**This story is primarily a configuration + verification story, not a code-heavy implementation story.**

### What Already Exists (DO NOT Recreate)

| Component | Location | Status |
|-----------|----------|--------|
| `SnapshotManager` (configurable intervals) | `Hexalith.EventStore.Server.Events.SnapshotManager` | Complete — three-tier resolution: tenant-domain > domain > default |
| `SnapshotOptions` (configuration model) | `Hexalith.EventStore.Server.Configuration.SnapshotOptions` | Complete — `DefaultInterval`, `DomainIntervals`, `TenantDomainIntervals` |
| `ISnapshotManager` DI registration | `EventStoreServerServiceCollectionExtensions.AddEventStoreServer()` | Complete — binds `EventStore:Snapshots` config section |
| `AggregateActor` snapshot integration | `Hexalith.EventStore.Server.Actors.AggregateActor` | Complete — Step 5b creates snapshots after event persistence |
| `AggregateActor` drain recovery | `Hexalith.EventStore.Server.Actors.AggregateActor` (Story 4.4) | Complete — `IRemindable` for pub/sub failure recovery |
| `FakeSnapshotManager` for testing | `Hexalith.EventStore.Testing.Fakes.FakeSnapshotManager` | Complete — in-memory implementation with assertion helpers |
| `FakeEventPublisher` for testing | `Hexalith.EventStore.Testing.Fakes.FakeEventPublisher` | Complete — used in `TenantsDaprTestFixture` |
| DAPR resiliency YAML | `src/Hexalith.Tenants.AppHost/DaprComponents/resiliency.yaml` | Complete — pub/sub circuit breaker, state store retry |
| `TenantsDaprTestFixture` | `tests/Hexalith.Tenants.IntegrationTests/Fixtures/` | Complete — boots CommandApi with local daprd sidecar |
| Health check endpoints | `ServiceDefaults.MapDefaultEndpoints()` | Complete — `/health`, `/alive`, `/ready` |
| Telemetry instrumentation | Story 7.2 | Complete — command spans, projection metrics, health checks |

### Snapshot Configuration Detail

EventStore's `SnapshotOptions` is bound from `appsettings.json` at the path `EventStore:Snapshots`. The configuration to add:

```json
{
  "EventStore": {
    "Snapshots": {
      "DomainIntervals": {
        "tenants": 50
      }
    }
  }
}
```

**Resolution order** (from `SnapshotManager.GetInterval()`):
1. `TenantDomainIntervals["system:tenants"]` — NOT needed (no per-tenant override)
2. `DomainIntervals["tenants"]` = 50 — **THIS IS WHAT WE SET**
3. `DefaultInterval` = 100 — default from `SnapshotOptions`, covers GlobalAdministratorAggregate

**Minimum interval enforcement**: `SnapshotOptions.Validate()` rejects intervals < 10. The 50-event interval is safe.

**Validation at startup**: `AddEventStoreServer()` registers `SnapshotOptions` with `.ValidateOnStart()`, so invalid configuration fails fast during startup.

### Stateless Service Verification

The tenant service is stateless by design:
- `Program.cs` does NOT persist any state locally — no file writes, no in-memory caches surviving restarts
- All domain state lives in the DAPR state store, managed by `AggregateActor`
- Projections use `CachingProjectionActor` which rebuilds from state store on activation
- The bootstrap `TenantBootstrapHostedService` is idempotent — re-running after restart produces a rejection (logged at Information level, not Error)

The Tier 2 test for AC #1 simulates restart by creating a new actor proxy (DAPR deactivates idle actors, and a new proxy triggers reactivation with full state rehydration from the state store).

### Graceful Degradation (NFR17)

EventStore's `AggregateActor` 5-step pipeline handles pub/sub failure at Step 5:
1. Events are persisted atomically in the DAPR state store (Step 5a — never lost)
2. If pub/sub publication fails, the actor transitions to `PublishFailed` terminal state
3. A drain record is stored alongside the events (same atomic batch)
4. A drain reminder (`IRemindable`) is registered for retry
5. When pub/sub recovers, the drain reminder fires and retries publication

The DAPR resiliency YAML (`resiliency.yaml`) adds circuit breaker protection: `pubsubBreaker` opens after 5 consecutive failures, causing immediate fast-fail (the actor doesn't wait for timeout). This is already configured in the AppHost.

**Key test insight**: The `FakeEventPublisher` in `TenantsDaprTestFixture` is replaceable. For the graceful degradation test, configure it to throw on the first publish call, then switch to succeeding on drain retry.

### Test Strategy

**Task 2 — Snapshot Configuration Test (Tier 1):**
- Load `appsettings.json` via `ConfigurationBuilder`, bind to `SnapshotOptions`
- Assert `DomainIntervals["tenants"] == 50` and `DefaultInterval == 100`
- No infrastructure required — pure configuration binding test
- Place in `tests/Hexalith.Tenants.Server.Tests/Configuration/`

**Task 3 — Stateless Restart Test (Tier 2):**
- Uses existing `TenantsDaprTestFixture` (DAPR sidecar + Redis)
- Send CreateTenant, then create new actor proxy for same actor ID, send DisableTenant
- Proves state survives actor deactivation/reactivation
- `[Collection("TenantsDaprTest")]` for fixture sharing

**Task 4 — Graceful Degradation Test (Tier 2):**
- Uses existing `TenantsDaprTestFixture`
- Manipulate `FakeEventPublisher` to simulate pub/sub outage
- Verify events are persisted even when publication fails
- Verify drain recovery publishes pending events
- `[Collection("TenantsDaprTest")]` for fixture sharing

**Task 5 — Snapshot Performance Test (Tier 3, nightly only):**
- Seed 500K events, cold-start rehydration, measure < 30s
- `[Trait("Category", "Performance")]` — excluded from PR CI
- May need dedicated infrastructure setup

### Critical Anti-Patterns (DO NOT)

- **DO NOT** create a custom `SnapshotManager` — EventStore's `SnapshotManager` is already registered via `AddEventStoreServer()` and fully handles all snapshot operations
- **DO NOT** call `SaveStateAsync` from snapshot code — `SnapshotManager.CreateSnapshotAsync` stages the write via `SetStateAsync`, and the caller (`AggregateActor`) commits atomically
- **DO NOT** set `TenantDomainIntervals` in config — the `system` tenant domain interval should match the global `DomainIntervals["tenants"]` setting. Per-tenant-domain overrides are for multi-tenant EventStore deployments with different performance characteristics
- **DO NOT** modify `AggregateActor`, `SnapshotManager`, or any EventStore code — these are in the `Hexalith.EventStore` submodule and must not be changed
- **DO NOT** add snapshot-related NuGet packages — the snapshot infrastructure is built into EventStore.Server
- **DO NOT** create custom pub/sub retry logic — EventStore's drain recovery (Story 4.4) handles pub/sub outages. DAPR resiliency YAML handles transient failures
- **DO NOT** modify existing DAPR component YAML files (`statestore.yaml`, `pubsub.yaml`, `resiliency.yaml`) — they are already correctly configured
- **DO NOT** add the snapshot configuration to `appsettings.Development.json` — use `appsettings.json` as the snapshot interval should be the same in all environments
- **DO NOT** duplicate integration test fixtures — use the existing `TenantsDaprTestFixture` and `TenantsDaprTestCollection`
- **DO NOT** mark snapshot/degradation tests as Tier 1 — they require DAPR infrastructure (Tasks 3-5 are Tier 2/3)

### Code Style Requirements

- File-scoped namespaces (`namespace X.Y.Z;`)
- Allman brace style (new line before opening brace)
- `ArgumentNullException.ThrowIfNull()` on public method parameters
- `TreatWarningsAsErrors = true` — zero warnings allowed
- 4-space indentation, CRLF line endings, UTF-8
- Test assertions use Shouldly (`result.ShouldBe(...)`)
- Test mocking uses NSubstitute
- Test framework: xUnit with `[Fact]` and `[Theory]`

### File Structure

```
src/Hexalith.Tenants.CommandApi/
  └── appsettings.json                          # MODIFY — add EventStore:Snapshots section

tests/Hexalith.Tenants.Server.Tests/
  └── Configuration/
      └── SnapshotConfigurationTests.cs         # NEW — Tier 1 config binding test

tests/Hexalith.Tenants.IntegrationTests/
  ├── StatelessRestartTests.cs                  # NEW — Tier 2 stateless restart verification
  ├── GracefulDegradationTests.cs               # NEW — Tier 2 pub/sub failure recovery
  └── SnapshotPerformanceTests.cs               # NEW — Tier 3 performance test (nightly CI)
```

### Previous Story Intelligence (7.2)

Story 7.2 established:
- Telemetry instrumentation: `TenantActivitySource`, `TenantMetrics` in CommandApi
- Health check: `DaprStateStoreHealthCheck` with `["ready"]` tag, `failureStatus: HealthStatus.Degraded`
- ServiceDefaults updated with `.AddMeter("Hexalith.Tenants")`
- 422 Tier 1+2 tests pass (up from 391 in Story 7.1)
- 2 pre-existing Tier 3 failures unchanged
- `[Collection("Telemetry")]` pattern for serializing tests that use global `ActivityListener`
- `using Activity?` declaration vs manual disposal issue on .NET 10 — prefer manual `activity?.Dispose()` in `finally`

Story 7.1 established:
- AppHost topology with CommandApi and Sample, both with DAPR sidecars
- Aspire topology smoke tests (3 tests) in IntegrationTests
- The Sample does NOT use ServiceDefaults

### Git Intelligence

Recent commits:
- `e09dbea` — Story 7.1: Aspire hosting and smoke tests
- `a1a9d53` — Refactor code structure (readability)
- `3e4ef10` — InMemoryTenantService and TenantTestHelpers

### Technology Versions

| Technology | Version | Source |
|-----------|---------|--------|
| .NET SDK | 10.0.103 | global.json |
| DAPR SDK | 1.17.3 | Directory.Packages.props |
| Aspire.Hosting | 13.1.2 | Directory.Packages.props |
| xUnit | 2.9.3 | Directory.Packages.props |
| Shouldly | 4.3.0 | Directory.Packages.props |
| NSubstitute | 5.3.0 | Directory.Packages.props |

### Project Structure Notes

- Only `appsettings.json` is modified in production code — all other changes are new test files
- No new NuGet packages required
- No new projects created
- Server.Tests may need `<Content>` item for `appsettings.json` if not already configured
- IntegrationTests already references CommandApi and EventStore.Testing

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 7.3] — Story definition, ACs
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 7] — Epic objectives: deployment & observability
- [Source: _bmad-output/planning-artifacts/prd.md#FR57] — Stateless service with event store reconstruction
- [Source: _bmad-output/planning-artifacts/prd.md#NFR12] — Stateless horizontal scaling
- [Source: _bmad-output/planning-artifacts/prd.md#NFR13] — 30s startup reconstruction for 500K events
- [Source: _bmad-output/planning-artifacts/prd.md#NFR17] — Graceful degradation when pub/sub unavailable
- [Source: _bmad-output/planning-artifacts/prd.md#NFR20] — Event store as single source of truth
- [Source: _bmad-output/planning-artifacts/architecture.md#Snapshot Strategy] — 50-event interval for tenants, default 100 for GlobalAdmin
- [Source: _bmad-output/planning-artifacts/architecture.md#Scaling] — Stateless horizontal scaling, all state in event store
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Configuration/SnapshotOptions.cs] — Snapshot configuration model
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/SnapshotManager.cs] — Snapshot creation/loading with three-tier interval resolution
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/ISnapshotManager.cs] — Snapshot manager interface
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs] — 5-step pipeline with snapshot + drain recovery
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Testing/Fakes/FakeSnapshotManager.cs] — In-memory snapshot manager for tests
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.AppHost/DaprComponents/resiliency.yaml] — Pub/sub resiliency with circuit breaker
- [Source: _bmad-output/implementation-artifacts/7-2-opentelemetry-instrumentation-and-health-checks.md] — Previous story learnings
- [Source: _bmad-output/implementation-artifacts/7-1-aspire-hosting-and-apphost.md] — Previous story learnings

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
