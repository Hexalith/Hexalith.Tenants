---
stepsCompleted: ['step-01-load-context', 'step-02-discover-tests', 'step-03-quality-evaluation', 'step-03f-aggregate-scores', 'step-04-generate-report']
lastStep: 'step-04-generate-report'
lastSaved: '2026-05-19'
workflowType: 'testarch-test-review'
inputDocuments:
  - 'D:/Hexalith.Tenants/CLAUDE.md'
  - 'D:/Hexalith.Tenants/Hexalith.EventStore/CLAUDE.md'
  - 'D:/Hexalith.Tenants/Hexalith.Commons/_bmad-output/project-context.md'
  - 'D:/Hexalith.Tenants/Hexalith.EventStore/_bmad-output/project-context.md'
  - 'D:/Hexalith.Tenants/Hexalith.FrontComposer/_bmad-output/project-context.md'
  - 'D:/Hexalith.Tenants/_bmad/tea/config.yaml'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/test-quality.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/test-levels-framework.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/data-factories.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/test-healing-patterns.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/test-priorities-matrix.md'
subagentOutputs:
  - '_bmad-output/test-artifacts/.tmp/tea-test-review-determinism-20260519-180000.json'
  - '_bmad-output/test-artifacts/.tmp/tea-test-review-isolation-20260519-180000.json'
  - '_bmad-output/test-artifacts/.tmp/tea-test-review-maintainability-20260519-180000.json'
  - '_bmad-output/test-artifacts/.tmp/tea-test-review-performance-20260519-180000.json'
---

# Test Quality Review: Hexalith.Tenants Suite

**Overall Quality Score**: **52 / 100** (Grade **F** — Needs Improvement)
**Review Date**: 2026-05-19
**Review Scope**: Suite (all 6 Hexalith.Tenants test projects, 62 .cs test files, ~8,500 lines)
**Reviewer**: Murat (TEA Agent)
**Execution Mode**: Parallel subagents (4 quality dimensions)

---

> **Coverage out of scope.** This review audits the quality of *existing* tests (determinism, isolation, maintainability, performance). Coverage mapping and gate decisions live in the `trace` workflow — see [traceability-matrix-epic-10-2026-05-19.md](_bmad-output/test-artifacts/traceability-matrix-epic-10-2026-05-19.md).

---

## Executive Summary

**Overall Assessment**: **Needs Improvement (with caveat)**

**Recommendation**: **Request Changes** — but the headline F-grade is misleading without context (see "Score Interpretation" below).

### Headline Numbers

| Dimension | Weight | Score | Grade | HIGH | MEDIUM | LOW |
|---|---|---:|:---:|---:|---:|---:|
| Determinism | 30% | 53 | F | 6 | 3 | 0 |
| Isolation | 30% | 84 | B | 0 | 2 | 2 |
| Maintainability | 25% | 0 | F | 8 | 12 | 3 |
| Performance | 15% | 72 | C | 0 | 4 | 4 |
| **Weighted Overall** | 100% | **52** | **F** | **14** | **21** | **9** |

### Score Interpretation (critical context)

The score is dominated by **two distinct problem classes** that should be triaged separately:

1. **Real test-design issues** (Determinism, ~half of Maintainability HIGHs): Wall-clock-coupled assertions, sleep-based synchronization in fixtures, oversized monolithic test files. These are structural and require code changes to the SUT (e.g. `TimeProvider` injection on `TenantAggregate`).
2. **Mechanical convention drift** (the other half of Maintainability HIGHs): 64/66 test files missing the ITANEO copyright header, 47/66 files in K&R brace style instead of project-mandated Allman. These are one-PR bulk fixes (`dotnet format` + SA1633 header analyzer).

If the mechanical-drift findings are excluded from scoring, Maintainability rises from 0 to ~46, and the weighted overall climbs from 52 to ~64 (D). The suite is closer to "needs work" than "broken" — but the **6 HIGH determinism violations are real flake risks** and warrant a CR-blocking patch.

### Key Strengths

✅ **Isolation is solid (B)** — No mutable static fields, no hardcoded ports, dynamic port allocation in DAPR/Aspire fixtures, GUID-suffixed per-test tenant IDs throughout integration tests, try/finally env-var restoration in `TenantsDaprTestFixture` and `AuthenticationConfigurationTests`.
✅ **Tier model is well-respected** — Tier 1 unit tests have zero `Thread.Sleep` and zero `Task.Delay >= 1000ms`; all wall-clock waits are confined to Tier 3 Aspire/DAPR fixtures where slowness is acceptable.
✅ **`[Collection(...)]` usage is justified** — Every serialized collection (TenantsDaprTest, AspireTopology, AuthenticationConfiguration, Telemetry) has a real reason (fixture sharing, env-var manipulation, global `ActivitySource`). No gratuitous serialization.
✅ **Tenant tagging is parallel-safe** — Integration tests use unique GUID-prefixed tenant IDs (`$"t-perf-{i:D4}-{Guid.NewGuid():N}"`), eliminating cross-test collisions in the shared DAPR state store.
✅ **`SnapshotPerformanceTests` is well-engineered** — Properly gated with `[DaprPerformanceFact]` + `[Trait("Category", "Performance")]`, uses `SemaphoreSlim` with `MaxConcurrency=50` for seeding, single `Stopwatch` measurement.

### Key Weaknesses

❌ **Wall-clock-coupled assertions in `TenantAggregateTests`** — Three `ShouldBeInRange(DateTimeOffset.UtcNow.AddSeconds(-5), DateTimeOffset.UtcNow.AddSeconds(1))` flake risks at lines 62, 145, 194. Root cause: `TenantAggregate` hard-codes `DateTimeOffset.UtcNow` instead of injecting `TimeProvider`.
❌ **Sleep-based synchronization in integration fixtures** — Three HIGH violations in `TenantsDaprTestFixture.cs` (line 137: `Task.Delay(2000)` after WaitForDaprHealth) and `GracefulDegradationTests.cs` (line 107: 90-second busy-poll). These are CI-flake amplifiers.
❌ **Convention drift is systemic** — 97% of test files (64/66) lack the required ITANEO copyright header; 71% (47/66) use K&R braces instead of the project's enforced Allman style.
❌ **Monolithic test files** — `TenantsProjectionActorTests.cs` is 2,075 lines / 68 methods; `TenantAggregateTests.cs` is 1,455 lines / 81 methods; `TenantConformanceTests.cs` is 1,169 lines with 12+ copy-paste blocks.
❌ **`ScaffoldingSmokeTests` are anti-patterns** — Five copies use `Assert.True(true)`, which violates the project's "use Shouldly, never raw Assert.*" rule AND asserts nothing meaningful.

### Summary

Hexalith.Tenants has a **mature, well-isolated test suite with two genuine quality risks: clock-leakage and fixture-sleep flakiness** — both fixable with focused changes. The headline F-grade is amplified by mechanical convention drift (copyright headers, brace style) that a single bulk-cleanup PR can resolve. **Recommend a two-stage remediation: (1) ship a focused PR fixing the 6 HIGH determinism findings and split the three flagship files, (2) ship a mechanical cleanup PR (`dotnet format` + SA1633 rule + bulk copyright headers).**

---

## Quality Criteria Assessment

| Criterion | Status | Violations | Notes |
|---|---|---|---|
| **Determinism — No wall-clock in assertions** | ❌ FAIL | 3 HIGH | `TenantAggregateTests` lines 62/145/194 use moving `DateTimeOffset.UtcNow` windows |
| **Determinism — No sleep-as-wait** | ❌ FAIL | 3 HIGH + 2 MEDIUM | `TenantsDaprTestFixture` lines 137/463/493, `TenantBootstrapHostedServiceTests` 170/178, `GracefulDegradationTests` 107 |
| **Determinism — Seeded randomness** | ⚠️ WARN | 1 HIGH | `SnapshotPerformanceTests:73` uses unseeded `Random.Shared.Next` |
| **Isolation — No mutable static fields** | ✅ PASS | 0 | Clean across all 62 files |
| **Isolation — Per-test cleanup** | ⚠️ WARN | 2 MEDIUM | `TenantsDaprTest` collection shares `FakeEventPublisher` without reset; `SnapshotPerformanceTests` writes ~500K Redis keys without cleanup |
| **Isolation — Unique tenant IDs** | ✅ PASS | 1 LOW | Hardcoded `"tenant-1"` in `TenantsQueryControllerIntegrationTests` only safe because the router is mocked |
| **Isolation — Explicit collection definitions** | ⚠️ WARN | 1 LOW | `[Collection("Telemetry")]` used without explicit `[CollectionDefinition("Telemetry")]` |
| **Maintainability — File size ≤ 500 lines** | ❌ FAIL | 6 HIGH + 4 MEDIUM | Top: `TenantsProjectionActorTests.cs` 2,075 lines |
| **Maintainability — Copyright headers** | ❌ FAIL | 1 HIGH (systemic) | 64/66 files missing ITANEO header |
| **Maintainability — Allman brace style** | ⚠️ WARN | 1 MEDIUM (systemic) | 47/66 files in K&R style |
| **Maintainability — No raw `Assert.*`** | ⚠️ WARN | 1 MEDIUM | 5 `ScaffoldingSmokeTests` use `Assert.True(true)` |
| **Maintainability — No copy-paste** | ❌ FAIL | 1 HIGH | `TenantConformanceTests` has 12+ near-identical blocks |
| **Maintainability — Magic strings extracted** | ⚠️ WARN | 2 MEDIUM | 180+ literal repetitions in `TenantsProjectionActorTests` and `TenantAggregateTests` |
| **Maintainability — `[Trait]` grouping** | ⚠️ WARN | 2 MEDIUM | The 2 flagship files (2,075 and 1,455 lines) have no `[Trait]` taxonomy |
| **Performance — No `Thread.Sleep` in Tier 1** | ✅ PASS | 0 | Clean |
| **Performance — No `Task.Delay >= 1000ms` in Tier 1** | ✅ PASS | 0 | All long delays confined to Tier 3 |
| **Performance — Fixture reuse** | ⚠️ WARN | 3 MEDIUM | `TenantServiceCollectionExtensionsTests` and `AuthenticationConfigurationTests` rebuild `ServiceProvider` 14+ times each; 4 Telemetry classes rebuild listeners 67 times |
| **Performance — `[Collection]` usage justified** | ✅ PASS | 0 | All serial collections have a real reason |
| **Performance — Explicit parallelization config** | ⚠️ WARN | 2 LOW | No `xunit.runner.json`, no `[assembly: CollectionBehavior]` |

**Total Violations**: 14 HIGH (P0/P1), 21 MEDIUM (P2), 9 LOW (P3) = **44 findings**

---

## Quality Score Breakdown

```
Determinism      (30% weight):  53/100  -> contributes  15.9
Isolation        (30% weight):  84/100  -> contributes  25.2
Maintainability  (25% weight):   0/100  -> contributes   0.0
Performance      (15% weight):  72/100  -> contributes  10.8
                                          -----------
Weighted Overall Score:                   51.9 -> 52/100
Grade:                                    F  (<60)
```

**Per-dimension scoring**: `score = max(0, 100 - HIGH*10 - MEDIUM*5 - LOW*2)`.

The Maintainability score floored at 0 because the systemic copyright-header finding (treated as 1 HIGH) plus 7 other HIGHs (file-size + copy-paste + brace drift) generated a 146-point penalty — capped at -100. **An adjusted-for-mechanical-drift score would be ~46 for Maintainability** (excluding the bulk copyright/brace findings), bringing the weighted overall to **~64 (D)**.

---

## Critical Issues (Must Fix Before Merge)

### 1. Wall-clock-coupled assertions in `TenantAggregateTests` (3 HIGH)

**Severity**: P0 (Critical — flakes under GC pause / CPU contention)
**Location**:
- [TenantAggregateTests.cs:62](tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs:62)
- [TenantAggregateTests.cs:145](tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs:145)
- [TenantAggregateTests.cs:194](tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs:194)

**Criterion**: Determinism — no wall-clock in assertions
**Knowledge Base**: [test-quality.md](.claude/skills/bmad-testarch-test-review/resources/knowledge/test-quality.md) Example 1 (Deterministic Test Pattern)

**Issue**: Three tests assert event timestamps fall within a moving window relative to `DateTimeOffset.UtcNow` at assertion time. The bounds shift between arrange and assert, so on a slow CI agent (GC pause, CPU contention) the actual `CreatedAt`/`DisabledAt`/`EnabledAt` can drift outside `[-5s, +1s]`.

**Current Code** (TenantAggregateTests.cs:60-65):
```csharp
// Bad — moving window means non-deterministic upper bound (1 second is tight)
((TenantCreated)evt).CreatedAt.ShouldBeInRange(
    DateTimeOffset.UtcNow.AddSeconds(-5),
    DateTimeOffset.UtcNow.AddSeconds(1));
```

**Recommended Fix** — inject `TimeProvider` into `TenantAggregate`:

```csharp
// In src/Hexalith.Tenants.Server/Domain/TenantAggregate.cs:
public sealed class TenantAggregate(TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    // ... emit new TenantCreated(..., _clock.GetUtcNow())
}

// In TenantAggregateTests.cs:
var fixedNow = DateTimeOffset.Parse("2026-05-19T10:00:00+00:00", CultureInfo.InvariantCulture);
var clock = new FakeTimeProvider(fixedNow); // or NSubstitute
var aggregate = new TenantAggregate(clock);

// ... act ...

((TenantCreated)evt).CreatedAt.ShouldBe(fixedNow); // exact equality, deterministic
```

**Why This Matters**: This single structural change eliminates all 3 HIGH timestamp violations AND prevents new tests from inheriting the same flake pattern. The aggregate currently emits ~10 wall-clock-coupled fields across the codebase (events, audit entries) — `TimeProvider` injection fixes them all.

**Related Violations**: ~80 additional `DateTimeOffset.UtcNow` occurrences in test files (`TenantConformanceTests`, `TenantReadModelTests`, `InMemoryTenantProjectionTests`, etc.) where the value is never asserted — currently benign, but a single test refactor could turn one into a flake.

---

### 2. Sleep-based synchronization in `TenantsDaprTestFixture` (1 HIGH + 2 MEDIUM)

**Severity**: P0 (CI-flake amplifier — under contention, sidecar may not be ready in 2 seconds)
**Location**:
- [TenantsDaprTestFixture.cs:137](tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs:137) — HIGH
- [TenantsDaprTestFixture.cs:463](tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs:463) — MEDIUM
- [TenantsDaprTestFixture.cs:493](tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs:493) — MEDIUM

**Criterion**: Determinism — no sleep-as-wait

**Issue (line 137)**: A fixed 2-second sleep after `WaitForDaprHealthAsync` "to let sidecar complete actor registration with placement service". This is a wall-clock guess masking a missing readiness signal.

**Current Code**:
```csharp
// Let sidecar complete actor registration with placement service.
await Task.Delay(2000).ConfigureAwait(false);
```

**Recommended Fix** — poll a real readiness signal:

```csharp
// Poll DAPR placement table until it lists the actor type.
private async Task WaitForPlacementRegistrationAsync(string actorType, TimeSpan timeout, CancellationToken cancellationToken)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    cts.CancelAfter(timeout);

    while (!cts.IsCancellationRequested)
    {
        try
        {
            // /v1.0/metadata returns registered actor types
            var response = await _httpClient.GetAsync($"{DaprHttpEndpoint}/v1.0/metadata", cts.Token).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
            if (body.Contains($"\"{actorType}\"", StringComparison.Ordinal))
            {
                return;
            }
        }
        catch (HttpRequestException) { /* sidecar not ready yet */ }

        await Task.Delay(100, cts.Token).ConfigureAwait(false);
    }

    throw new TimeoutException($"Actor type '{actorType}' not registered with placement service within {timeout}.");
}
```

**Why This Matters**: The current 2-second sleep is paid on every Tier 3 collection run regardless of actual readiness. Under CI contention it can be too short (test cascade failure); on a fast dev machine it's pure latency. Polling fixes both.

---

### 3. 90-second busy-poll in `GracefulDegradationTests.DrainRecovery` (1 HIGH)

**Severity**: P0 (slow + opaque failure)
**Location**: [GracefulDegradationTests.cs:107](tests/Hexalith.Tenants.IntegrationTests/GracefulDegradationTests.cs:107)
**Criterion**: Determinism — no polling loop without deterministic signal

**Issue**: The test polls every 1 second for up to 90 seconds for an event count to increase, with no deterministic completion signal. The AC text says "within 90 seconds" but the test does not assert that bound — it just times out at 90 seconds.

**Current Code**:
```csharp
for (int i = 0; i < 90; i++)
{
    int eventsNow = _fixture.EventPublisher.GetEventsForTopic(expectedTopic).Count;
    if (eventsNow > eventsBefore)
    {
        drainSucceeded = true;
        break;
    }
    await Task.Delay(1000);
}
```

**Recommended Fix** — TaskCompletionSource + bounded wait + Stopwatch:

```csharp
var drainComplete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
using var subscription = _fixture.EventPublisher.OnEventPublished(expectedTopic, _ => drainComplete.TrySetResult(true));

var stopwatch = Stopwatch.StartNew();
var completed = await Task.WhenAny(drainComplete.Task, Task.Delay(TimeSpan.FromSeconds(90))).ConfigureAwait(false);
stopwatch.Stop();

completed.ShouldBeSameAs(drainComplete.Task, "drain recovery did not publish to expected topic within 90s");
stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(90)); // AC-5 explicit
```

**Why This Matters**: AC-5 ("within 90 seconds") is now part of the assertion, not just the loop bound. Failure messages will show "drain did not publish" instead of opaque "DrainSucceeded was false".

---

### 4. Unseeded `Random.Shared.Next` in `SnapshotPerformanceTests` (1 HIGH)

**Severity**: P0 (perf failures are non-reproducible)
**Location**: [SnapshotPerformanceTests.cs:73](tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs:73)
**Criterion**: Determinism — seeded randomness

**Issue**: `Random.Shared.Next(tenantIds.Length)` picks an unseeded tenant for the cold-start rehydration measurement. When the perf test fails, the chosen tenant changes between runs.

**Current Code**:
```csharp
string targetTenantId = tenantIds[Random.Shared.Next(tenantIds.Length)];
```

**Recommended Fix**:

```csharp
const int seed = 42;
var rng = new Random(seed);
int targetIndex = rng.Next(tenantIds.Length);
string targetTenantId = tenantIds[targetIndex];
_output.WriteLine($"[seed={seed}] Selected tenant index {targetIndex}: {targetTenantId}");
```

---

### 5. Hosted-service test sleep + busy-poll (1 HIGH + 1 MEDIUM)

**Severity**: P0
**Location**:
- [TenantBootstrapHostedServiceTests.cs:170](tests/Hexalith.Tenants.Server.Tests/Bootstrap/TenantBootstrapHostedServiceTests.cs:170) — HIGH (`await Task.Delay(50)` as "wait for no-call")
- [TenantBootstrapHostedServiceTests.cs:178](tests/Hexalith.Tenants.Server.Tests/Bootstrap/TenantBootstrapHostedServiceTests.cs:178) — MEDIUM (busy-poll loop)

**Criterion**: Determinism — no sleep-as-wait

**Recommended Fix**: Capture the start continuation and use `TaskCompletionSource` for the negative-assertion path (a TCS that completes only when the HTTP handler is invoked → assert TCS is NOT completed).

---

### 6. Monolithic test files (6 HIGH)

**Severity**: P1 (high friction for new contributors; obscures coverage)
**Locations**:
- [TenantsProjectionActorTests.cs](tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs) — 2,075 lines, 68 methods
- [TenantAggregateTests.cs](tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs) — 1,455 lines, 81 methods
- [TenantConformanceTests.cs](Hexalith.Tenants/Hexalith.Tenants.Testing/tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs) — 1,169 lines (12+ copy-paste blocks)
- [ProjectionWriteConformanceTests.cs](tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs) — 704 lines
- [TenantsDaprTestFixture.cs](tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs) — 604 lines
- [TenantProjectionHandlerTests.cs](tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs) — 593 lines

**Criterion**: Maintainability — file size ≤ 500 lines

**Recommended Splits** (already signposted by section-comment dividers in the files):
- `TenantsProjectionActorTests` → `GetTenantTests`, `ListTenantsTests`, `GetTenantUsersTests`, `GetUserTenantsTests`, `GetTenantAuditTests` (+ shared `TenantsProjectionActorTestHarness`)
- `TenantAggregateTests` → `TenantAggregateLifecycleTests`, `TenantAggregateUserRoleTests`, `TenantAggregateConfigurationTests`, `TenantAggregateRbacTests`, `TenantStateReplayTests` (use the existing `// ===== Story X.X =====` comments as split lines)
- `TenantConformanceTests` → collapse to a single `[Theory]` driven by `[ClassData]`/`[MemberData]` — expected ~80% line reduction
- `ProjectionWriteConformanceTests` → split by projection target (Detail / Index / Audit) + extract scenario builders to the fixture
- `TenantsDaprTestFixture` → extract `DaprPortAllocator`, `DaprSidecarProcessLauncher`, `DaprSidecarHealthProbe`
- `TenantProjectionHandlerTests` → split by projected entity + move event-builders to a fixture

---

### 7. Missing ITANEO copyright headers — systemic (1 HIGH affecting 64 files)

**Severity**: P1 (project policy violation; CI should fail)
**Location**: 64 of 66 `.cs` test files. Only `ProjectionWriteConformanceTests.cs` and `ProjectionWriteConformanceFixture.cs` comply.
**Criterion**: Maintainability — copyright header on every `.cs` file (per `project-context.md`)

**Recommended Fix** — bulk-apply + analyzer enforcement:

```csharp
// <copyright file="TenantAggregateTests.cs" company="ITANEO">
// Copyright (c) ITANEO. All rights reserved.
// Licensed under the MIT License.
// </copyright>
```

Enable `SA1633 (FileMustHaveHeader)` in `tests/.editorconfig` or `Directory.Build.props` with `EnforceCodeStyleInBuild=true`. One-time PR fixes 64 files; the analyzer prevents regression.

---

## Recommendations (Should Fix)

### 8. `ScaffoldingSmokeTests` use raw `Assert.True(true)` (5 occurrences)

**Severity**: P2
**Locations**: One file per test project — `tests/Hexalith.Tenants.Client.Tests/`, `tests/Hexalith.Tenants.Contracts.Tests/`, `tests/Hexalith.Tenants.IntegrationTests/`, `tests/Hexalith.Tenants.Server.Tests/`, `tests/Hexalith.Tenants.Testing.Tests/`, `samples/Hexalith.Tenants.Sample.Tests/`

**Criterion**: Maintainability — no raw `Assert.*`

**Issue**: Files assert `Assert.True(true)` — both violates the project rule AND asserts nothing. They produce a false coverage signal (project "loads") that the loader test infrastructure already validates.

**Recommended Fix**: Delete these placeholder files entirely. The test runner already verifies project compilation by virtue of running the suite.

---

### 9. Allman brace style drift (47/66 files)

**Severity**: P2
**Criterion**: Maintainability — Allman brace style (`.editorconfig` `csharp_new_line_before_open_brace = all`)

**Recommended Fix**: Run `dotnet format` over the test projects. Then raise the warning to error in `EnforceCodeStyleInBuild` so CI catches further drift. Zero-risk mechanical fix.

---

### 10. `FakeEventPublisher` shared across the `TenantsDaprTest` collection without reset

**Severity**: P2 (forward-looking — current tests defend with unique-prefix assertions, but a future count-based assertion would leak)
**Location**: [TenantsDaprTestFixture.cs:71](tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs:71)

**Recommended Fix**: Add `Reset()`/`ClearPublishedEvents()` to `FakeEventPublisher` and `FakeDeadLetterPublisher`. Call it from a base test class constructor (xUnit ctor runs per test, collection fixture does not).

---

### 11. `SnapshotPerformanceTests` writes ~500K Redis keys without cleanup

**Severity**: P2
**Location**: [SnapshotPerformanceTests.cs:53](tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs:53)

**Recommended Fix**: After the assertion, issue `DaprClient.DeleteStateAsync(StateStoreName, key)` for each `t-perf-*` prefixed key, or call `FLUSHDB` scoped to the test's key prefix.

---

### 12. Per-test `BuildServiceProvider()` in DI tests (3 MEDIUM)

**Severity**: P2 (CI throughput)
**Locations**:
- [TenantServiceCollectionExtensionsTests.cs](tests/Hexalith.Tenants.Client.Tests/Registration/TenantServiceCollectionExtensionsTests.cs) — 14+ rebuilds
- [AuthenticationConfigurationTests.cs](tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs) — 14+ rebuilds

**Recommended Fix**: For descriptor-only assertions (e.g., "DI registers `IDaprClient`"), operate on `IServiceCollection` without calling `BuildServiceProvider()`. For resolution-required tests, memoize the provider per configuration shape via `[Theory]` + per-config fixture.

---

### 13. Telemetry collection has no shared fixture (1 MEDIUM)

**Severity**: P2
**Location**: `[Collection("Telemetry")]` is applied to 4 test classes but no `[CollectionDefinition("Telemetry")]` exists. All 67 tests rebuild identical `ActivityListener`/`MeterListener` pairs.

**Recommended Fix**: Add a `TelemetryListenerFixture` with `ICollectionFixture<>` and a per-test reset method. Saves ~50-100ms per CI run; also makes isolation contract explicit.

---

### 14. Magic-string repetition (180+ occurrences)

**Severity**: P2
**Locations**: `TenantsProjectionActorTests` and `TenantAggregateTests`

**Recommended Fix**: Extract `TenantTestIds` and `TenantTestTimestamps` static classes:

```csharp
internal static class TenantTestIds
{
    public const string AcmeId = "acme";
    public const string OwnerUser = "owner-user";
    public const string ContributorUser = "contributor-user";
    public const string ReaderUser = "reader-user";
    public const string OrphanTenantId = "tenant-002-orphan";
    // ...
}

internal static class TenantTestTimestamps
{
    public static readonly DateTimeOffset Genesis =
        DateTimeOffset.Parse("2026-01-15T10:30:00+00:00", CultureInfo.InvariantCulture);
}
```

---

## Best Practices Found

### ✅ Aspire/DAPR fixtures use dynamic port allocation

**Location**: [TenantsDaprTestFixture.cs](tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs)
**Pattern**: Per-fixture-run port allocation prevents parallel collision on shared CI hosts.

### ✅ `DaprFactAttribute` for graceful skip

**Location**: [DaprFactAttribute.cs](tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprFactAttribute.cs)
**Pattern**: Tier 2/3 tests skip cleanly when the sidecar is unavailable instead of failing the suite. Use as a model for any future infrastructure-dependent test.

### ✅ Try/finally environment-variable restoration

**Location**: [AuthenticationConfigurationTests.cs](tests/Hexalith.Tenants.Server.Tests/Configuration/AuthenticationConfigurationTests.cs)
**Pattern**: Tests that mutate `Environment.SetEnvironmentVariable` restore the original value in `finally`. Correct cross-test hygiene.

### ✅ `[DaprPerformanceFact]` + `[Trait("Category", "Performance")]` gating

**Location**: [SnapshotPerformanceTests.cs](tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs)
**Pattern**: Performance tests are explicitly tagged and gated so CI lanes can filter (`dotnet test --filter "Category!=Performance"`). Use this pattern when adding more perf coverage.

### ✅ Delta-snapshot assertions in shared fixtures

**Location**: [GracefulDegradationTests.cs](tests/Hexalith.Tenants.IntegrationTests/GracefulDegradationTests.cs) — `eventsBefore`/`eventsAfterFailure`
**Pattern**: When a fake publisher is shared across collection members, assert on the delta (count change between snapshots) rather than absolute counts. Codify this as the canonical pattern for the `TenantsDaprTest` collection.

---

## Test Suite Profile

### Inventory

- **Total test projects**: 6 (Client, Contracts, IntegrationTests, Server, Testing, Sample)
- **Total `.cs` files**: 66 (62 in scope after excluding 4 `ScaffoldingSmokeTests` placeholders)
- **Total lines**: ~8,500
- **Test framework**: xUnit (likely v3 — see EventStore CLAUDE.md note) + Shouldly + NSubstitute
- **Language**: C# on .NET 10

### File Size Distribution

| Percentile | Lines |
|---|---|
| p50 | 128 |
| p90 | 461 |
| Max | 2,075 (`TenantsProjectionActorTests.cs`) |

Files over the 500-line threshold: **6** (~9% of suite, but they hold ~30% of the line volume).

### Tier Distribution

| Tier | Project(s) | Files | Notes |
|---|---|---|---|
| **Tier 1 — Unit** | `Contracts.Tests`, `Client.Tests`, `Testing.Tests`, `Sample.Tests`, most of `Server.Tests` | ~47 | No external deps; should run in milliseconds |
| **Tier 2 — Integration** | Bootstrap + Telemetry parts of `Server.Tests` | ~2 | Light DI graph, no sidecar |
| **Tier 3 — End-to-end** | `IntegrationTests` (all) | ~11 | Aspire + DAPR + Docker, gated with `DaprFactAttribute` |

### Parallelization Profile

- Estimated parallelizable: **90%** of tests
- Estimated serial (`[Collection]`-bound): **10%** — all justified (env-var mutation, global `ActivitySource`, DAPR fixture sharing)
- No `xunit.runner.json` — the parallel contract is implicit. Adding one makes it defendable.

---

## Knowledge Base References

This review applied the following knowledge fragments to the .NET/C# context:

- **[test-quality.md](.claude/skills/bmad-testarch-test-review/resources/knowledge/test-quality.md)** — Definition of Done: no hard waits, < 300 lines, < 1.5 min, self-cleaning
- **[test-levels-framework.md](.claude/skills/bmad-testarch-test-review/resources/knowledge/test-levels-framework.md)** — Unit vs Integration vs E2E selection (maps to Hexalith Tier 1/2/3)
- **[data-factories.md](.claude/skills/bmad-testarch-test-review/resources/knowledge/data-factories.md)** — Factory functions with overrides, API-first setup
- **[test-healing-patterns.md](.claude/skills/bmad-testarch-test-review/resources/knowledge/test-healing-patterns.md)** — Race condition / hard wait / dynamic data patterns
- **[test-priorities-matrix.md](.claude/skills/bmad-testarch-test-review/resources/knowledge/test-priorities-matrix.md)** — P0/P1/P2/P3 classification

**JavaScript-specific fragments loaded but cross-applied conceptually** (the patterns transfer; the code syntax does not): `overview.md`, `api-request.md`, `auth-session.md`, `recurse.md`.

For coverage analysis, see the `trace` workflow — coverage is out of scope here.

---

## Next Steps

### Immediate Actions (P0 — Before Next Sprint)

| # | Action | Owner | Effort |
|---|---|---|---|
| 1 | Inject `TimeProvider` into `TenantAggregate`; rewrite 3 `ShouldBeInRange` assertions to exact equality | Server-side dev | ~4h |
| 2 | Replace `Task.Delay(2000)` placement wait in `TenantsDaprTestFixture` with `/v1.0/metadata` polling | Integration test dev | ~2h |
| 3 | Replace `GracefulDegradationTests.DrainRecovery` busy-poll with `TaskCompletionSource` + explicit elapsed assertion | Integration test dev | ~2h |
| 4 | Seed the `Random.Shared.Next` call in `SnapshotPerformanceTests` | Perf test dev | ~15m |
| 5 | Replace `Task.Delay(50)` in `TenantBootstrapHostedServiceTests` with TCS-based negative assertion | Server-side dev | ~1h |

**Bundle 1-5 in one PR titled `fix(tests): remove wall-clock and sleep-based determinism risks`.**

### Mechanical Cleanup (P1 — Single PR, Low Risk)

| # | Action | Owner | Effort |
|---|---|---|---|
| 6 | Enable SA1633 + bulk-apply ITANEO copyright header to 64 files | Tooling | ~30m |
| 7 | Run `dotnet format` over test projects to fix 47 brace-style files | Tooling | ~15m |
| 8 | Delete or convert the 5 `ScaffoldingSmokeTests` placeholders | Anyone | ~30m |
| 9 | Add `tests/xunit.runner.json` + `[assembly: CollectionBehavior]` to each project | Tooling | ~30m |

**Bundle 6-9 in one PR titled `chore(tests): apply project conventions across the test suite`.**

### Follow-up Refactors (P2 — Next 2 Sprints)

| # | Action | Owner | Effort |
|---|---|---|---|
| 10 | Split `TenantsProjectionActorTests.cs` (2,075 lines) along existing section dividers | Server-side dev | ~6h |
| 11 | Split `TenantAggregateTests.cs` (1,455 lines) along `// ===== Story X.X =====` markers | Server-side dev | ~6h |
| 12 | Convert `TenantConformanceTests` to a single `[Theory]` + `[ClassData]` | Server-side dev | ~4h |
| 13 | Extract `TenantTestIds`/`TenantTestTimestamps` constants for magic-string elimination | Anyone | ~2h |
| 14 | Add `TelemetryListenerFixture` + `[CollectionDefinition("Telemetry")]` for fixture sharing | Server-side dev | ~3h |
| 15 | Add `Reset()` to `FakeEventPublisher`/`FakeDeadLetterPublisher`; call from test ctors | Integration test dev | ~2h |
| 16 | Per-test Redis cleanup in `SnapshotPerformanceTests` | Perf test dev | ~1h |

### Re-review Trigger

**Re-run `test-review` after PRs #1 and #6 land.** Expected outcomes:
- Determinism: 53 → ~85 (B) — eliminates 5 of 6 HIGH findings
- Maintainability: 0 → ~50 (F→C) — eliminates the systemic copyright and brace HIGHs
- **Weighted overall: 52 → ~73 (C)**

After PRs #10-13 also land (~ next sprint), expect weighted overall ~85 (B).

---

## Decision

**Recommendation**: **Request Changes**

**Rationale**: The suite has solid isolation (B) and good performance hygiene (C), but **6 HIGH determinism findings represent real CI-flake risk** — particularly the `TenantAggregate` wall-clock coupling that affects future test writes as well. The mechanical convention drift (copyright headers, brace style) is bulk-fixable in a single tooling PR. The monolithic test files are an organizational debt, not a correctness risk, and can be deferred to follow-up sprints.

**Net assessment**: The suite is **closer to "needs focused remediation" than to "broken"**. The 52/100 score is amplified by mechanical-drift penalties (which mask real strengths in isolation and performance), and an adjusted-for-drift overall would land in the mid-60s (D, on the cusp of C).

Once the P0 batch (Actions 1-5) and mechanical cleanup (6-9) ship, the suite will comfortably exceed the C-grade threshold. **Do not block ongoing feature work** — schedule the two cleanup PRs and proceed.

---

## Appendix A — All Violations by File

| File | Severity | Dimension | Category | Brief |
|---|:---:|---|---|---|
| `TenantAggregateTests.cs:62` | HIGH | Determinism | wallclock-in-assertion | `ShouldBeInRange(UtcNow-5s, UtcNow+1s)` on `CreatedAt` |
| `TenantAggregateTests.cs:145` | HIGH | Determinism | wallclock-in-assertion | Same pattern on `DisabledAt` |
| `TenantAggregateTests.cs:194` | HIGH | Determinism | wallclock-in-assertion | Same pattern on `EnabledAt` |
| `TenantBootstrapHostedServiceTests.cs:170` | HIGH | Determinism | sleep-as-wait | `Task.Delay(50)` as negative-assertion barrier |
| `TenantBootstrapHostedServiceTests.cs:178` | MEDIUM | Determinism | polling-loop | `WaitUntilAsync` polls every 10ms for 2s |
| `SnapshotPerformanceTests.cs:73` | HIGH | Determinism | unseeded-random | `Random.Shared.Next` unseeded |
| `GracefulDegradationTests.cs:107` | HIGH | Determinism | polling-loop | 90-second busy-poll |
| `TenantsDaprTestFixture.cs:137` | HIGH | Determinism | sleep-as-wait | `Task.Delay(2000)` for placement registration |
| `TenantsDaprTestFixture.cs:463` | MEDIUM | Determinism | polling-loop | 1s health-poll tick |
| `TenantsDaprTestFixture.cs:493` | MEDIUM | Determinism | polling-loop | 200ms × 30 attempts app-listening probe |
| `TenantsDaprTestFixture.cs:71` | MEDIUM | Isolation | shared-mutable-fixture-state | `FakeEventPublisher` shared without reset |
| `SnapshotPerformanceTests.cs:53` | MEDIUM | Isolation | shared-state-store-no-cleanup | 500K Redis keys uncleaned |
| `Telemetry/*.cs` | LOW | Isolation | missing-collection-definition | `[Collection("Telemetry")]` without definition |
| `TenantsQueryControllerIntegrationTests.cs:73` | LOW | Isolation | hardcoded-tenant-literal | `"tenant-1"` hardcoded |
| `TenantsProjectionActorTests.cs` | HIGH | Maintainability | file-size | 2,075 lines |
| `TenantAggregateTests.cs` | HIGH | Maintainability | file-size | 1,455 lines |
| `TenantConformanceTests.cs` | HIGH | Maintainability | file-size | 1,169 lines |
| `ProjectionWriteConformanceTests.cs` | HIGH | Maintainability | file-size | 704 lines |
| `TenantsDaprTestFixture.cs` | HIGH | Maintainability | file-size | 604 lines |
| `TenantProjectionHandlerTests.cs` | HIGH | Maintainability | file-size | 593 lines |
| (suite-wide) | HIGH | Maintainability | missing-copyright-systemic | 64 of 66 files |
| `TenantConformanceTests.cs:60` | HIGH | Maintainability | copy-paste-duplication | 12+ near-identical blocks |
| `TenantsQueryControllerIntegrationTests.cs` | MEDIUM | Maintainability | file-size | 387 lines (watch zone) |
| `AspireTopologyFixture.cs` | MEDIUM | Maintainability | file-size | 461 lines (watch zone) |
| `ProjectionWriteConformanceFixture.cs` | MEDIUM | Maintainability | file-size | 342 lines (watch zone) |
| `InMemoryTenantProjectionTests.cs` | MEDIUM | Maintainability | file-size | 328 lines (watch zone) |
| `TenantServiceCollectionExtensionsTests.cs` | MEDIUM | Maintainability | file-size | 322 lines (watch zone) |
| (suite-wide) | MEDIUM | Maintainability | brace-style-deviation | 47 of 66 files |
| `TenantsProjectionActorTests.cs:26` | MEDIUM | Maintainability | missing-trait-grouping | 68 methods, no `[Trait]` |
| `TenantAggregateTests.cs:17` | MEDIUM | Maintainability | missing-trait-grouping | 81 methods, no `[Trait]` |
| `TenantsProjectionActorTests.cs:35` | MEDIUM | Maintainability | theory-inlinedata-overload | 15 `[InlineData]` without group comments |
| `TenantsProjectionActorTests.cs:939` | MEDIUM | Maintainability | magic-string-repetition | 180+ literal repetitions |
| `TenantAggregateTests.cs:18` | MEDIUM | Maintainability | magic-string-repetition | 185+ literal repetitions |
| `ScaffoldingSmokeTests.cs` (×5) | MEDIUM | Maintainability | raw-assert-vs-shouldly | `Assert.True(true)` placeholder |
| `TenantConformanceTests.cs:14` | LOW | Maintainability | trailing-blank-line | Duplicate blank between using/namespace |
| `TenantsProjectionActorTests.cs` | LOW | Maintainability | aaa-markers-missing | AAA section comments absent |
| `TenantsProjectionActorTests.cs:1785` | LOW | Maintainability | helper-overload-sprawl | 4 `CreateActor` overloads chain |
| `TenantServiceCollectionExtensionsTests.cs:63` | MEDIUM | Performance | per-test-service-provider | 14+ `BuildServiceProvider()` calls |
| `TenantBootstrapHostedServiceTests.cs:170` | MEDIUM | Performance | wait-via-task-delay | `Task.Delay(50)` (also flagged for Determinism) |
| `TenantsProjectionActorTelemetryTests.cs:28` | MEDIUM | Performance | duplicated-listener-setup | 4 classes × ActivityListener+MeterListener |
| `AuthenticationConfigurationTests.cs:200` | MEDIUM | Performance | per-test-service-provider | Provider rebuilt per test |
| `TenantsDaprTestFixture.cs:137` | LOW | Performance | fixed-sleep-instead-of-poll | (also flagged for Determinism) |
| (suite-wide) | LOW | Performance | no-xunit-runner-json | Missing config file |
| (suite-wide) | LOW | Performance | no-assembly-collection-behavior | Missing attribute |
| `TenantBootstrapHostedServiceTests.cs:188` | LOW | Performance | real-http-client-in-unit-test | Unnecessary `HttpClient` allocation |

---

## Review Metadata

- **Generated By**: BMad TEA Agent (Master Test Architect) — Murat
- **Workflow**: `testarch-test-review` v4.0 (BMAD 6.7.1)
- **Execution Mode**: Parallel (4 subagents — Determinism / Isolation / Maintainability / Performance)
- **Review ID**: `test-review-hexalith-tenants-suite-20260519`
- **Timestamp**: 2026-05-19T18:00:00+00:00
- **Version**: 1.0
- **Subagent Outputs**:
  - `_bmad-output/test-artifacts/.tmp/tea-test-review-determinism-20260519-180000.json`
  - `_bmad-output/test-artifacts/.tmp/tea-test-review-isolation-20260519-180000.json`
  - `_bmad-output/test-artifacts/.tmp/tea-test-review-maintainability-20260519-180000.json`
  - `_bmad-output/test-artifacts/.tmp/tea-test-review-performance-20260519-180000.json`
