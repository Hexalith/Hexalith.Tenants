# Investigation: Long-running snapshot performance test

## Hand-off Brief

1. **What happened.** Confirmed: the 16–21 minute duration comes from building 500,000 events through full command lifecycles; the asserted final operation is under 30 seconds.
2. **Where the case stands.** Concluded: the benchmark is optimizable, but it currently measures one cold full command rather than the NFR's stated system ready-state behavior.
3. **What's needed next.** Correct the benchmark contract, then add a deterministic EventStore test seeder and measure against an isolated disposable Redis dataset.

## Case Info

| Field            | Value |
| ---------------- | ----- |
| Ticket           | N/A |
| Date opened      | 2026-07-14 |
| Status           | Concluded |
| System           | Linux 6.6.87.2 WSL2 x86_64; .NET SDK 10.0.301; test runtime .NET 10.0.9 |
| Evidence sources | User observation; running-process inspection; TRX result; benchmark and DAPR test-gate source |

## Problem Statement

User report: "why this test is so long? can it be optimized?"

The initial hypothesis was that the performance test is inherently slow. The evidence refutes that framing: the asserted cold operation passes, while avoidable dataset construction dominates total duration.

## Evidence Inventory

| Source | Status | Notes |
| ------ | ------ | ----- |
| `TestResults/post-review-performance/post-review-performance.trx` | Available | One passed test; duration 00:16:12.0999439 |
| `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs` | Available | Benchmark setup submits 1,000 x 500 actor commands; only final rehydration is timed |
| `references/Hexalith.EventStore/src/Hexalith.EventStore.Testing.Integration/DaprFactAttribute.cs` | Available | Confirms process-local serialization gates for shared DAPR infrastructure |
| Version-control history | Available | Benchmark introduced in `5810018`, reframed as Story 7.5 NFR evidence in `11a32f8`, and serialized for DAPR safety in `d6c2584` |
| `TestResults/Hexalith.Tenants.IntegrationTests-Performance/performance.trx` | Available | Earlier successful run took 21m06.887s |
| Actor/snapshot source | Available | Aggregate actor, event persister/reader, snapshot manager, and fixture source are locally available |
| Issue tracker | Missing | No ticket or issue identifier was supplied or referenced by the benchmark |
| Profiler/trace capture | Missing | No `.nettrace`, speedscope, CPU profile, or equivalent artifact exists in the workspace |
| Fine-grained phase timings | Missing | No timings for seeding, deactivation, or rehydration are emitted |

## Investigation Backlog

| # | Path to Explore | Priority | Status | Notes |
| - | --------------- | -------- | ------ | ----- |
| 1 | Quantify the command-seeding cost and concurrency behavior | High | Done | Global-position actor serializes all workers; exact percentages need optional profiling |
| 2 | Trace snapshot storage and actor command processing | High | Done | Repeated rehydration, commits, domain calls, and snapshot cadence mapped |
| 3 | Separate fixture/topology startup from benchmark duration | Medium | Done | Test-case duration accounts for 16m12s of a 16m17s run |
| 4 | Evaluate valid setup alternatives | High | Done | Ranked plan preserves 500,000-event scale without full command seeding |
| 5 | Map the available evidence perimeter | High | Done | Test results, history, source, diagnostics, and missing evidence classified |
| 6 | Form and challenge causal hypotheses | High | Done | Command-pipeline amplification confirmed; topology-startup theory refuted |
| 7 | Trace source and process boundaries | High | Done | Real DAPR/Redis/HTTP work separated from in-memory test doubles |

## Timeline of Events

| Time | Event | Source | Confidence |
| ---- | ----- | ------ | ---------- |
| 2026-07-14 13:27:36 +02:00 | Benchmark test started | TRX | Confirmed |
| 2026-07-14 13:43:48 +02:00 | Benchmark passed after 16m12s | TRX | Confirmed |
| 2026-07-14 12:48:33 +02:00 | Earlier benchmark test started | TRX | Confirmed |
| 2026-07-14 13:09:39 +02:00 | Earlier benchmark passed after 21m06s | TRX | Confirmed |

## Confirmed Findings

### Finding 1: The test passed in 16m12s

**Evidence:** `TestResults/post-review-performance/post-review-performance.trx:8`

**Detail:** The runner reports one executed and passed test with duration `00:16:12.0999439`.

### Finding 2: The timed assertion excludes the 500,000-command setup

**Evidence:** `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs:64`, `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs:83`, `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs:106`

**Detail:** Phase 1 creates 1,000 tenants and submits 500 commands per tenant before the stopwatch starts around one final actor invocation.

### Finding 3: Successful runs vary by almost five minutes

**Evidence:** `TestResults/Hexalith.Tenants.IntegrationTests-Performance/performance.trx:8`, `TestResults/post-review-performance/post-review-performance.trx:8`

**Detail:** Two same-day successful executions took 21m06.887s and 16m12.100s. The faster run is 294.788 seconds, or 23.3%, shorter, but both validate the same final under-30-second assertion.

### Finding 4: No phase-level or profiler evidence was captured

**Evidence:** Workspace artifact inventory on 2026-07-14 found TRX and coverage files but no `.nettrace`, speedscope, CPU profile, or benchmark phase log.

**Detail:** The current evidence can bound setup cost but cannot distinguish actor RPC latency, Redis persistence, snapshot work, fixture startup, or resource contention quantitatively.

### Finding 5: The configured snapshot interval is 50 events

**Evidence:** `src/Hexalith.Tenants/appsettings.json:39`, `src/Hexalith.Tenants/appsettings.json:41`, `tests/Hexalith.Tenants.IntegrationTests/bin/Release/net10.0/appsettings.json:39`, `tests/Hexalith.Tenants.IntegrationTests/bin/Release/net10.0/appsettings.json:41`

**Detail:** Both source and built runtime configuration set the `tenants` domain interval to 50, matching the benchmark comment.

### Finding 6: Every seed command reconstructs state from snapshot plus ordered tail reads

**Evidence:** `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:388`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:401`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:409`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventStreamReader.cs:91`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventStreamReader.cs:98`

**Detail:** The actor does not reuse an in-memory aggregate state between seed commands. The reader loads tail events one at a time because actor state-manager access is explicitly ordered. Applying the actual snapshot cadence to 500 commands gives 12,295 tail reads per tenant, or approximately 12.295 million across 1,000 tenants.

### Finding 7: Each seed command executes the production persistence/publication pipeline

**Evidence:** `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:455`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:497`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:523`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:554`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:610`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventPersister.cs:51`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventPersister.cs:129`

**Detail:** Beyond rehydration, every command invokes the domain service, reads metadata again for persistence, stages an event and metadata, evaluates snapshot policy, commits actor state, and runs event publication/lifecycle work.

### Finding 8: Test-run overhead outside the test case is about four seconds

**Evidence:** `TestResults/post-review-performance/post-review-performance.trx:3`, `TestResults/post-review-performance/post-review-performance.trx:8`, `TestResults/post-review-performance/post-review-performance.trx:30`

**Detail:** The test run began at 13:27:32.291, the test case began at 13:27:36.366, and the test case consumed 16m12.100s of the 16m17.051s run. Runner/discovery/fixture work outside the recorded test case cannot explain the long duration.

### Finding 9: Each successful seed command has four actor commits and four status writes

**Evidence:** `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:379`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:382`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:556`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:596`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:636`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:647`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:748`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:749`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:2281`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:2290`

**Detail:** Across 500,000 successful commands, the setup drives approximately 2 million actor-state commits, 2 million advisory-status writes, 500,000 domain invocations, and 500,000 publication calls.

### Finding 10: Snapshot work is frequent but much smaller than command lifecycle volume

**Evidence:** `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:524`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:525`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:530`, `src/Hexalith.Tenants/appsettings.json:41`

**Detail:** The first command has no prior state. The remaining commands evaluate policy 499 times per tenant and produce snapshots at inferred sequences 49, 98, 147, 196, 245, 294, 343, 392, 441, and 490: 499,000 checks and 10,000 snapshot writes overall.

### Finding 11: The final stopwatch measures an entire command turn

**Evidence:** `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs:106`, `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs:107`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:165`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:388`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:455`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:497`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs:610`

**Detail:** The 30-second assertion includes activation/proxy overhead, validation, idempotency, pending-count work, rehydration, domain invocation, persistence, publication, status writes, and terminal cleanup. It does not isolate rehydration time. With the last inferred snapshot at 490, the final command should replay only ten tail events under healthy snapshot behavior.

### Finding 12: Global-position allocation serializes all seed commands through one actor

**Evidence:** `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/DaprGlobalPositionAllocator.cs:9`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/DaprGlobalPositionAllocator.cs:15`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/DaprGlobalPositionAllocator.cs:19`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/GlobalPositionActor.cs:23`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/GlobalPositionActor.cs:31`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/GlobalPositionActor.cs:32`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Configuration/ServiceCollectionExtensions.cs:45`

**Detail:** Every event-producing command allocates through actor ID `global`. The singleton actor reads its current position and saves the incremented value for every allocation, so all 50 tenant workers converge on one DAPR actor and Redis-backed counter.

### Finding 13: Domain execution includes 500,000 real DAPR self-HTTP calls

**Evidence:** `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs:32`, `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs:66`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/DomainServices/DaprDomainServiceInvoker.cs:50`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/DomainServices/DaprDomainServiceInvoker.cs:56`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/DomainServices/DaprDomainServiceInvoker.cs:61`

**Detail:** The domain service resolves to the same `commandapi` host, but the invoker still constructs a DAPR invocation request, sends an HTTP request through the sidecar path, and deserializes the response for every command.

### Finding 14: Status and publication paths are in-memory but retain millions of records

**Evidence:** `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs:89`, `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs:96`, `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TestEventPublisher.cs:13`, `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TestEventPublisher.cs:16`, `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TestEventPublisher.cs:84`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Testing/Fakes/InMemoryCommandStatusStore.cs:13`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Testing/Fakes/InMemoryCommandStatusStore.cs:32`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Testing/Fakes/InMemoryCommandStatusStore.cs:33`

**Detail:** These paths do not perform DAPR/Redis I/O in this fixture. They retain roughly 500,000 current status records, 2 million status-history entries, 500,000 publish-call records, and 500,000 published events, creating avoidable allocation and GC pressure for a benchmark that asserts none of them.

### Finding 15: The implementation does not measure the NFR's stated ready-state behavior

**Evidence:** `11a32f8:_bmad-output/implementation-artifacts/7-5-prove-stateless-operation-health-and-startup-reconstruction.md:23`, `11a32f8:_bmad-output/planning-artifacts/prd.md:603`, `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs:85`, `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs:93`, `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs:106`

**Detail:** The acceptance source requires reconstruction/time-to-ready evidence at 1,000 tenants and 500,000 total events. The test neither restarts the service nor probes ready state; it deactivates one random actor and times one complete command. This may be a deliberate proxy, but that equivalence is not specified or asserted.

### Finding 16: Repeated runs accumulate persistent benchmark state

**Evidence:** `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs:49`, `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs:65`, `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs:66`, `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs:20`, `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs:21`

**Detail:** Every run generates new tenant IDs with GUID suffixes and disposes only the test gate; it does not erase actor event/snapshot state from the shared local Redis infrastructure. Each execution therefore leaves another 500,000-event dataset behind, reducing repeatability and potentially changing later results.

## Deduced Conclusions

### Deduction 1: Setup dominates wall-clock duration

**Based on:** Findings 1 and 2.

**Reasoning:** The final measured operation is bounded by a 30-second assertion, while the whole test lasts 972 seconds. Therefore at least 942 seconds, or more than 96.9% of wall time, occurs outside the measured cold-rehydration operation.

**Conclusion:** The long duration is primarily benchmark-data construction, not the rehydration operation the NFR claims to measure.

### Deduction 2: The benchmark is primarily a bulk command-throughput workload

**Based on:** Findings 2, 5, 6, and 7.

**Reasoning:** The benchmark executes 500,000 end-to-end commands and millions of state reads to prepare one cold-rehydration measurement. The setup exercises much more than the asserted behavior.

**Conclusion:** Wall-clock optimization should target or bypass data construction first; relaxing the 30-second rehydration assertion would not materially shorten the run.

### Deduction 3: Dataset construction and measurement should be separated

**Based on:** Findings 3, 4, and 8.

**Reasoning:** Whole-test timing varies by 23.3%, setup dominates, and no phase timings exist. A single test result cannot distinguish benchmark regression from setup/load variation.

**Conclusion:** A valid optimized benchmark needs a deterministic seeded-state contract plus separate setup verification and measured rehydration evidence.

### Deduction 4: Snapshot-frequency tuning cannot remove the dominant fixed workload

**Based on:** Findings 6, 9, and 10.

**Reasoning:** Snapshot creation occurs 10,000 times, while the full setup performs 500,000 command turns, about 2 million commits, about 2 million status writes, and about 12.295 million tail reads.

**Conclusion:** Changing the interval may shift tail-read versus snapshot-write cost, but it cannot approach the gain available from avoiding 500,000 full command lifecycles during benchmark setup.

### Deduction 5: The stated metric is broader than its name and comments imply

**Based on:** Finding 11.

**Reasoning:** The stopwatch surrounds `ProcessCommandAsync`, not the snapshot-load and event-replay stage.

**Conclusion:** Optimization work must decide whether NFR13 intends cold command readiness or isolated reconstruction latency; the current test measures the former while describing the latter.

### Deduction 6: Increasing tenant concurrency cannot scale linearly

**Based on:** Findings 12 and 13.

**Reasoning:** Independent tenant actors still serialize through one global-position actor, and each command incurs a DAPR self-HTTP round trip.

**Conclusion:** A concurrency sweep may find a modest local optimum, but worker count cannot remove the global coordination point or per-command network boundary.

### Deduction 7: The benchmark requires semantic repair before performance tuning

**Based on:** Findings 11 and 15.

**Reasoning:** The test name/comments, stopwatch boundary, and historical NFR describe three different metrics: isolated rehydration, cold full-command latency, and system ready-state reconstruction.

**Conclusion:** Optimizing the existing code without first choosing the intended metric risks making a fast test that proves the wrong requirement.

### Deduction 8: The benchmark needs disposable storage isolation

**Based on:** Findings 3 and 16.

**Reasoning:** Same-day runs vary by 23.3%, and each run adds a new persistent dataset to shared Redis.

**Conclusion:** Performance evidence should run against a dedicated ephemeral Redis instance or a fingerprinted dataset restored into a clean namespace and destroyed afterward.

## Hypothesized Paths

### Hypothesis 1: Per-command actor/state-store round trips dominate seeding

**Status:** Confirmed

**Theory:** Five hundred thousand actor proxy calls, event writes, status updates, and periodic snapshot writes dominate setup time.

**Supporting indicators:** Each tenant loop awaits every command sequentially; only tenants are parallelized, with a maximum of 50 concurrent tenant loops.

**Would confirm:** Source evidence of repeated per-command rehydration/persistence plus whole-test evidence bounding all non-test overhead.

**Would refute:** Evidence that fixture startup or gate contention consumes most of the 16m12s.

**Resolution:** Confirmed by Findings 2, 6, 7, and 8. Fine-grained telemetry is still required to apportion cost among the confirmed operations.

### Hypothesis 2: DAPR topology startup is the primary cause

**Status:** Refuted

**Theory:** Sidecar/fixture initialization accounts for most of the 16–21 minute runtime.

**Supporting indicators:** The test requires a DAPR sidecar and Redis infrastructure.

**Would confirm:** A large gap between test-run start and test-case start, or phase logs showing startup dominates.

**Would refute:** Test-case duration closely matching total run duration.

**Resolution:** Refuted by Finding 8; the recorded test case itself consumes 16m12s of a 16m17s run.

### Hypothesis 3: Snapshot creation alone dominates setup

**Status:** Open

**Theory:** Snapshot protection and writes every 50 events dominate the command workload.

**Supporting indicators:** Snapshot creation includes protection and state staging.

**Would confirm:** Traces showing snapshot operations consume most Phase 1 time.

**Would refute:** Phase traces showing ordinary command rehydration, DAPR invocation, or commits dominate between snapshot boundaries.

**Resolution:** Source shows 10,000 snapshot writes versus approximately 2 million actor commits and 12.295 million tail reads. The "snapshot alone" explanation is structurally unlikely but remains open until phase timing refutes an unusually high per-snapshot cost.

### Hypothesis 4: Raising `MaxConcurrency` is the best optimization

**Status:** Refuted

**Theory:** More than 50 parallel tenant loops would reduce setup time proportionally.

**Supporting indicators:** Tenants are independent actor identities.

**Would confirm:** Isolated concurrency sweep showing higher throughput without latency, error, or Redis saturation growth.

**Would refute:** Flat or declining throughput and increased state-store latency above 50 workers.

**Resolution:** Refuted as the best optimization by Finding 12. All workers serialize through the singleton global-position actor; tuning may provide only a secondary improvement.

### Hypothesis 5: The test isolates reconstruction latency

**Status:** Refuted

**Theory:** The measured 30-second value represents only snapshot loading and tail replay.

**Supporting indicators:** The test and comments call the metric cold-start rehydration.

**Would confirm:** A stopwatch placed directly around snapshot load and event replay, or a dedicated metric exported for that activity.

**Would refute:** A stopwatch around the actor's complete command API.

**Resolution:** Refuted by Finding 11. The measured value is cold full-command latency.

### Hypothesis 6: Global-position allocation is a workload-wide serialization point

**Status:** Confirmed

**Theory:** The fixed `global` actor serializes allocation across otherwise independent tenants.

**Supporting indicators:** Every event-producing command requests a position from the same actor identity.

**Would confirm:** Allocator and actor source showing one fixed actor ID plus state read/save per allocation.

**Would refute:** Sharded/range-cached allocation or distinct actor IDs per tenant.

**Resolution:** Confirmed by Finding 12. Its precise percentage of wall time still requires tracing.

### Hypothesis 7: Test-double retention materially contributes to variability

**Status:** Open

**Theory:** Millions of retained status/event observations increase GC and memory pressure during seeding.

**Supporting indicators:** Both doubles retain all history, and same-day runtime varied by 23.3%.

**Would confirm:** Allocation/GC counters or an isolated run with bounded/no-op doubles showing lower time and memory.

**Would refute:** Equivalent timing and GC behavior after disabling retention.

**Resolution:** Structural allocation pressure is confirmed; timing impact is not measured.

## Missing Evidence

| Gap | Impact | How to Obtain |
| --- | ------ | ------------- |
| Phase-level timings | Cannot attribute exact percentages beyond the 30-second upper bound | Add temporary diagnostics or measure from traces/log timestamps |
| Seeder transaction limits and actor-key contract | Determines safe batch size and implementation mechanism | Prototype in `Hexalith.EventStore.Testing.Integration` and validate through production readers |
| Redis/DAPR latency and saturation | Cannot tune concurrency responsibly | Inspect Aspire/DAPR traces and Redis metrics during an isolated run |
| Exact NFR metric interpretation | Determines whether to measure ready state, isolated rehydration, or cold command latency | Product/architecture decision using the historical NFR and current lazy-actor design |

## Source Code Trace

| Element       | Detail |
| ------------- | ------ |
| Error origin  | No failure; long wall time reported by `SnapshotPerformanceTests.ColdStartRehydration_CompletesWithin30Seconds_With500KEvents` |
| Trigger       | Performance lane with DAPR prerequisites and performance opt-in enabled |
| Condition     | Benchmark builds 500,000 events through full command lifecycles, then times one cold full-command turn |
| Related files | `SnapshotPerformanceTests.cs`; `AggregateActor.cs`; `EventStreamReader.cs`; `EventPersister.cs`; `SnapshotManager.cs`; `DaprDomainServiceInvoker.cs`; `DaprGlobalPositionAllocator.cs`; `GlobalPositionActor.cs`; test fixture/doubles |

### Process-boundary trace

| Operation | Boundary | Scale |
| --------- | -------- | ----- |
| Aggregate actor command/state | DAPR actor + Redis | 500,000 commands; millions of reads/commits |
| Domain processing | DAPR self-HTTP then in-process aggregate | 500,000 requests |
| Global position | Singleton DAPR actor + Redis | 500,000 serialized allocations |
| Command status | In-memory concurrent collections | About 2 million retained history entries |
| Event publication | In-memory concurrent collections | 500,000 calls/events retained |

## Conclusion

**Confidence:** High

The root cause is confirmed: more than 96.9% of wall time is pre-measurement setup that drives 500,000 full actor command turns, approximately 12.295 million ordered tail reads, approximately 2 million actor commits, 500,000 DAPR self-HTTP calls, and 500,000 allocations through one singleton global-position actor. Snapshot tuning and higher concurrency cannot remove that fixed workload. The current test also has a contract defect: it labels the metric rehydration/startup readiness but times one complete cold command and never restarts or probes service readiness.

## Recommended Next Steps

### Fix direction

Ranked recommendation:

1. **Correct the benchmark contract first.** Decide whether NFR13 means system time-to-ready, isolated actor reconstruction, or cold full-command latency. Recommended: split these into separately named measurements; restart and probe readiness for the NFR, and retain a representative cold-command benchmark as a distinct metric.
2. **Add a deterministic platform-level seeder.** Implement it in `Hexalith.EventStore.Testing.Integration`, not Tenants. Seed valid event envelopes, aggregate metadata, and snapshots through the supported actor-state abstraction in bounded batches. Preallocate global positions in large ranges using `IGlobalPositionAllocator.AllocateAsync(count)` rather than calling the global actor once per event.
3. **Separate setup validation from timed measurement.** Assert 1,000 aggregates, 500 events each, valid snapshot sequence/state, monotonic unique global positions, protection metadata, and successful production-reader replay before beginning the measured restart/rehydration phase.
4. **Use isolated disposable storage.** Run against an ephemeral Redis/DAPR state store or restore a schema-fingerprinted 500,000-event artifact into a clean instance, then destroy it. Do not reuse the current shared persistent Redis state.
5. **Bound observation retention during setup.** Use performance-specific status/publisher doubles that count outcomes without retaining 2.5+ million history/publication objects. Re-enable the production-equivalent observation path only where the measured behavior requires it.
6. **Instrument before secondary tuning.** Record fixture startup, seeding, restart/deactivation, state-rehydration activity, full cold command, Redis latency, global-position allocation, GC, and peak memory separately.
7. **Tune concurrency last.** Sweep worker counts only after batched seeding removes the singleton-per-event allocation path. Do not reduce the required 1,000 × 500 dataset merely to make the test faster.

Expected result: direct/batched deterministic seeding removes hundreds of thousands of HTTP/actor calls and millions of redundant reads/commits, so an order-of-magnitude setup reduction is plausible. The exact gain must be benchmarked; no defensible minute target exists without implementing the seeder prototype.

### Diagnostic

For the first implementation comparison, preserve the current test as a baseline and add phase timers plus runtime counters. Compare current command seeding, batched deterministic seeding, and restored-artifact setup on clean Redis instances using at least three isolated runs each; report setup duration separately from the chosen NFR metric and include p50/max, peak memory, GC pauses, DAPR call counts, and Redis operation latency.

## Reproduction Plan

1. Start a clean dedicated DAPR/Redis topology with no concurrent integration or performance process.
2. Run the current opted-in benchmark once and retain TRX, phase timings, DAPR traces, Redis metrics, and .NET counters.
3. Verify persisted end state: 1,000 aggregate metadata records at sequence 500, 500,000 event envelopes, expected snapshots (current inferred last sequence 490), and valid unique global positions.
4. Restart the service if testing ready-state NFR13; otherwise deactivate the selected actor for the explicitly named cold-command/reconstruction metric.
5. Run the measured operation and record rehydration activity separately from full command latency.
6. Repeat with the deterministic batched seeder and, optionally, a restored dataset artifact on fresh storage.
7. Require state-equivalence checks and the same under-30-second chosen metric before accepting any speed improvement.

## Side Findings

- Confirmed: DAPR tests use process-local serialization gates, but separate test processes are not mutually serialized by these in-memory semaphores (`references/Hexalith.EventStore/src/Hexalith.EventStore.Testing.Integration/DaprFactAttribute.cs:43`, `references/Hexalith.EventStore/src/Hexalith.EventStore.Testing.Integration/DaprFactAttribute.cs:56`).
- Confirmed: the 23.3% same-day runtime variation means infrastructure/load sensitivity is material even though both tests pass.
- Confirmed: the repository has no existing supported bulk event seeder; backup restore is explicitly deferred, so this capability belongs in the EventStore testing platform rather than an ad hoc Tenants state-store writer.
