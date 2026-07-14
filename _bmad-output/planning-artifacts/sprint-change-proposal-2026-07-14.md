# Sprint Change Proposal — Correct NFR13 Reconstruction Evidence

- **Date:** 2026-07-14
- **Trigger:** `_bmad-output/implementation-artifacts/investigations/long-running-performance-test-investigation.md`
- **Review mode:** Batch
- **Scope classification:** Major — requirement/evidence semantics and implementation ownership cross Tenants, EventStore, and Hexalith.Builds
- **MVP impact:** None to the completed Phase 2 UI MVP
- **Status:** APPROVED — 2026-07-14 by Administrator; routed to Product Management and Solution Architecture

## 1. Issue Summary

The scheduled `SnapshotPerformanceTests.ColdStartRehydration_CompletesWithin30Seconds_With500KEvents` benchmark passed, but the passing result does not prove the behavior named by historical NFR13 or Story 7.5 AC5.

The test spends 16–21 minutes constructing 500,000 events through 500,000 full actor command lifecycles, then starts its stopwatch around one final `ProcessCommandAsync` invocation for one deactivated aggregate actor. It does not restart the Tenants service, wait for dependency readiness, or measure system restart-to-ready. The timed call also includes validation, idempotency, actor activation, reconstruction, domain invocation, persistence, publication, advisory status, and cleanup, so it is not an isolated reconstruction measurement either.

The benchmark therefore establishes only this narrower fact:

> For one aggregate selected from a 1,000-tenant/500,000-event shared Redis dataset, one cold full command completed in less than 30 seconds.

It does **not** establish the historical requirement:

> State reconstruction from the event store on startup completes within 30 seconds for up to 1,000 tenants and 500,000 total events.

The long runtime is also dominated by avoidable dataset construction. Confirmed setup amplification includes approximately 12.295 million ordered tail reads, 2 million actor-state commits, 2 million advisory status writes, 500,000 DAPR self-HTTP domain calls, 500,000 singleton global-position allocations, 500,000 publication calls, and 10,000 snapshot writes. More than 96.9% of the wall time is setup. Random tenant identifiers and shared Redis storage leave each run's dataset behind, so repeated scheduled runs accumulate state.

Historical Story 7.5 was honest at completion: it recorded that the 500,000-event benchmark did not run and explicitly did not claim NFR13 compliance. The problem arose when a later execution of the semantically mismatched test was treated as NFR13 evidence.

## 2. Evidence and Trigger Classification

### 2.1 Confirmed evidence

- `TestResults/post-review-performance/post-review-performance.trx`: passed in `00:16:12.0999439`.
- `TestResults/Hexalith.Tenants.IntegrationTests-Performance/performance.trx`: earlier pass in `00:21:06.887`.
- Same-day successful runs differ by 294.788 seconds, or 23.3%.
- `SnapshotPerformanceTests.cs:64-83`: builds all 500,000 events before timing begins.
- `SnapshotPerformanceTests.cs:88-108`: deactivates one actor and times one `ProcessCommandAsync` call.
- `SnapshotPerformanceTests.cs:110-118`: labels that full command result as the NFR13 reconstruction threshold.
- No service restart, `/ready` probe, phase timing, profiler trace, peak-memory record, or isolated dataset lifecycle is present.
- The shared `domain-ci.yml` performance job currently omits `HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1`; the scheduled lane can therefore skip the EventStore performance gate even while selecting `Category=Performance`.

### 2.2 Trigger classification

This is a combined:

- **Requirement ambiguity:** “startup reconstruction” and “ready state” were never reduced to an observable timing boundary.
- **Evidence-contract defect:** actor deactivation plus a full command was substituted for process restart plus readiness.
- **Test-design inefficiency:** the dataset is generated through the behavior under load rather than through a deterministic benchmark-data facility.
- **Cross-repository ownership gap:** the reusable seeder belongs to EventStore testing infrastructure, and the missing scheduled opt-in belongs to Hexalith.Builds, not Tenants.

This is not a product outage and does not invalidate Story 7.5 AC1–AC4. It invalidates only any claim that the current benchmark proves the historical NFR13 30-second startup target.

## 3. Impact Analysis

### 3.1 Epic and story impact

- Current Epics 1–5 are Phase 2 UI epics and are complete. They remain unchanged.
- Historical Story 7.5 remains an accurate record of the work completed at the time because its completion notes explicitly said NFR13 was not claimed.
- Historical Story 7.5 AC5 requires an evidence annotation: later execution of the current benchmark does not close NFR13.
- A new corrective epic is required because the work has independently testable outcomes, spans three repositories, and should not be hidden inside a completed historical story.
- No existing story becomes obsolete. The current benchmark can be retained after being renamed and relabeled as cold full-command latency evidence.

### 3.2 PRD and requirements impact

The active PRD at `planning-artifacts/prds/prd-tenants-2026-06-02/prd.md` is the Phase 2 UI PRD. It contains UI NFR-1 through NFR-10 and does not contain historical NFR13. Adding the old service-startup requirement directly to that UI PRD would mix phases and products.

Recommended requirements action:

1. Do not edit the active UI PRD.
2. Create a canonical cross-phase `tenants-service-performance-evidence.md` planning artifact that supersedes only the historical NFR13 measurement and evidence wording.
3. Link the new corrective epic to that contract.
4. Record NFR13 as **not proven** until the corrected scheduled evidence succeeds.

### 3.3 Architecture impact

The active architecture document is UI-focused and should not absorb an EventStore benchmark-data design. Create a focused performance-evidence architecture section in the new contract, or a companion ADR, covering:

- exact timing boundaries for restart-to-ready, isolated reconstruction, and cold full-command latency;
- an EventStore-owned deterministic dataset builder;
- supported state-writing abstractions and production-reader equivalence validation;
- range allocation for global positions;
- disposable, isolated DAPR/Redis storage per run;
- bounded/no-op setup observers where production-equivalent persistence does not require retained observation history;
- phase, dependency, memory, and GC telemetry;
- scheduled-lane ownership and evidence retention.

No Tenants production architecture, command routing, DAPR component name, snapshot interval, public contract, or UI architecture changes are required.

### 3.4 UX impact

None. No user journey, interaction, content, accessibility, localization, or visual design changes are required.

### 3.5 Technical artifact impact

| Area | Proposed impact | Owner |
|---|---|---|
| Tenants performance tests | Relabel existing test; add corrected split measurements and dataset validation integration | Tenants |
| EventStore testing integration | Add deterministic, reusable benchmark dataset builder through a supported state abstraction | EventStore |
| DAPR/Redis test topology | Use an ephemeral isolated store or a validated schema-fingerprinted restored artifact; destroy after run | EventStore/Tenants test infrastructure |
| Hexalith.Builds scheduled workflow | Enable the EventStore performance opt-in and retain scheduled evidence artifacts | Hexalith.Builds |
| Planning/evidence docs | Add the corrected contract, historical evidence annotation, benchmark report format, and traceability | PM/Architect/Test |
| Sprint tracking | Add the corrective epic and stories only after proposal approval | PM/PO |

Submodule modifications are not authorized by this proposal alone. EventStore and Hexalith.Builds changes require explicit approval in their owning repositories before implementation.

## 4. Options Considered

### Option A — Direct adjustment in the current plan

Add a corrective epic, preserve the current test as a narrower baseline, implement the missing platform data builder, and add the correct readiness benchmark.

- **Viability:** High.
- **Effort:** High, approximately 8–15 engineering days across planning, EventStore, Tenants, and Builds.
- **Risk:** Medium; the main uncertainty is the supported high-volume state-writing abstraction and dataset-equivalence proof.
- **Benefit:** Correct evidence without discarding completed reliability work.

### Option B — Roll back Story 7.5 or remove the benchmark

Revert the completed health, statelessness, snapshot configuration, and restart evidence, or delete the current test.

- **Viability:** Low.
- **Effort:** Medium.
- **Risk:** High; it destroys valid AC1–AC4 evidence and loses a useful cold-command baseline.
- **Benefit:** None commensurate with the disruption.

### Option C — Change or reduce the 30-second target

Redefine NFR13 to match the current one-aggregate command test, remove the 1,000-tenant ready-state scope, or defer the target indefinitely.

- **Viability:** Only as an explicit product/operations decision after obtaining corrected baseline data.
- **Effort:** Medium.
- **Risk:** Low implementation risk but high evidence-integrity risk if used merely to ratify the existing test.
- **Benefit:** May produce a more operationally relevant target later, but no present evidence justifies changing the target.

### Option D — Recommended hybrid

Use Option A with a focused requirements clarification from Option C:

1. Preserve the original 30-second threshold as the recommended restart-to-ready target.
2. Split actor reconstruction and cold full-command latency into separate diagnostic measurements without inheriting the 30-second threshold automatically.
3. Build the reusable dataset facility at the EventStore platform boundary.
4. Keep dataset construction outside timed performance assertions but report it as an operational cost.
5. Mark NFR13 unproven until the corrected scheduled benchmark runs.

This option corrects course without rollback or UI MVP disruption.

## 5. Detailed Change Proposals

### 5.1 Historical NFR13 evidence correction

**Artifact:** new `planning-artifacts/tenants-service-performance-evidence.md`, linked from the corrective epic.

**Before — historical NFR13:**

> State reconstruction from the event store on startup completes within 30 seconds for up to 1,000 tenants with an assumed average of 500 events per tenant (500,000 total events), verified by a startup benchmark test that seeds the target event volume and measures time to ready state. Baseline EventStore snapshot configuration is part of Phase 1 reliability/performance work; advanced snapshot tuning beyond baseline is Phase 3 if target exceeded.

**After — proposed NFR13 contract:**

> For a validated and isolated dataset containing 1,000 tenant aggregates with 500 events each, restarting the Tenants service must reach dependency-ready state within 30 seconds. Measure from the start of the service restart until the Tenants `/ready` endpoint first returns success after required DAPR and EventStore dependencies are usable. Dataset creation or restoration is outside this timed interval but must be timed and reported separately. Evidence is valid only when the dataset passes production-reader and snapshot/metadata invariants, storage is disposable or restored from a schema-fingerprinted artifact, and at least three scheduled runs report restart-to-ready results. Isolated aggregate reconstruction and cold full-command latency are separate diagnostic metrics and do not prove this threshold. Until corrected scheduled evidence succeeds, NFR13 status is **not proven**.

**Decision requested:** approve restart-to-ready as the metric to which the historical 30-second threshold applies. If operations intended a different readiness boundary, revise the contract before implementation.

### 5.2 Historical Story 7.5 AC5 evidence annotation

**Before:**

> Given startup reconstruction performance tests run with the target scale data set, when 1,000 tenants with an assumed average of 500 events each are seeded, then ready-state reconstruction completes within the 30-second target or reports a documented failure, and the 500,000-event benchmark is classified as scheduled performance evidence while ordinary readiness and health checks remain in the implementation lane.

**After — proposed superseding annotation:**

> Story 7.5 AC1–AC4 remain complete. AC5's NFR13 threshold remains unproven. A later run of `SnapshotPerformanceTests.ColdStartRehydration_CompletesWithin30Seconds_With500KEvents` timed one cold full command after deactivating one actor; it did not restart the service or measure `/ready`. That result may be retained as cold full-command latency evidence but must not be cited as restart-to-ready evidence. Corrected evidence is owned by the new Performance Evidence Integrity epic.

Do not rewrite the historical completion notes, which accurately recorded the benchmark skip and did not claim compliance. Add the annotation to the new canonical evidence contract and any current traceability index.

### 5.3 New Epic 6 — Performance Evidence Integrity

**Epic outcome:** Operators and maintainers can distinguish service readiness, aggregate reconstruction, and full-command latency, and can reproduce NFR13 evidence against a validated isolated 500,000-event dataset in the scheduled lane.

#### Story 6.1 — Ratify the reconstruction performance contract

As an operator and test architect,
I want explicit performance metric boundaries,
So that a passing benchmark proves the behavior named by the requirement.

**Acceptance criteria:**

1. Restart-to-ready, isolated aggregate reconstruction, cold full-command latency, and dataset construction are defined as four separate measurements.
2. The 30-second NFR13 threshold applies only to restart-to-ready unless an approved requirements decision changes it.
3. The readiness dependency set and stopwatch start/stop events are explicit and automatable.
4. NFR13 is recorded as not proven until corrected evidence exists.
5. The active Phase 2 UI PRD and completed UI epics remain unchanged.

#### Story 6.2 — Provide a deterministic EventStore benchmark dataset builder

As a domain integration-test author,
I want an EventStore-owned benchmark dataset builder,
So that production-readable histories, metadata, and snapshots can be prepared without 500,000 full command turns.

**Acceptance criteria:**

1. The capability is implemented in `Hexalith.EventStore.Testing.Integration`, not in Tenants production or domain code.
2. It creates valid event envelopes, aggregate metadata, protection metadata, and snapshots through an approved EventStore/DAPR state abstraction.
3. Global positions are unique and monotonic and are reserved in ranges through `IGlobalPositionAllocator.AllocateAsync(count)` or an approved equivalent; one singleton actor allocation per event is not required.
4. Writes are bounded and batched without bypassing the persisted format read by production EventStore readers.
5. Validation proves 1,000 aggregates, 500 events per aggregate, expected snapshot sequence/state, and successful production-reader reconstruction.
6. The implementation contains no Tenants-specific state format beyond supplied domain event/state factories.

#### Story 6.3 — Isolate and validate scheduled benchmark storage

As a performance-test maintainer,
I want a disposable, validated benchmark data environment,
So that repeated runs are reproducible and cannot accumulate hidden state.

**Acceptance criteria:**

1. Each run uses an ephemeral Redis/DAPR topology or restores a schema- and version-fingerprinted dataset artifact into isolated storage.
2. Dataset validation completes before any performance stopwatch starts.
3. Storage is destroyed or deterministically cleaned after the run, including failure and cancellation paths.
4. Test identities are deterministic within the isolated store; random GUID accumulation in shared Redis is removed.
5. Setup observation doubles are bounded or no-op where observation history is not part of the persisted dataset contract.

#### Story 6.4 — Split and instrument the Tenants performance benchmarks

As a maintainer,
I want separate benchmarks for readiness, reconstruction, and full-command latency,
So that failures identify the affected behavior and cannot produce false NFR claims.

**Acceptance criteria:**

1. A restart-to-ready benchmark restarts the Tenants service and times until `/ready` succeeds against the validated 500,000-event dataset.
2. An isolated aggregate reconstruction benchmark times only activation plus snapshot/tail replay through an approved measurement seam.
3. The existing test is renamed and documented as cold full-command latency, retaining its command-pipeline semantics without citing NFR13.
4. All three measurements report setup/restore time, phase timings, result distribution across at least three runs, peak memory, GC activity, and relevant DAPR/Redis latency or operation counts.
5. Assertions include persisted-state equivalence and not only command acceptance/status.
6. Snapshot interval remains 50 unless corrected evidence fails and a separate architecture decision approves tuning.

#### Story 6.5 — Make scheduled performance evidence executable and reviewable

As a release maintainer,
I want the shared scheduled lane to enable every selected performance gate and retain a structured report,
So that a scheduled green run represents executed evidence rather than a skip.

**Acceptance criteria:**

1. The Hexalith.Builds performance job sets the required Tenants and EventStore performance opt-ins for this lane.
2. A scheduled-shaped validation proves the benchmark is executed rather than skipped.
3. TRX plus a structured benchmark report are retained, including environment fingerprint, dataset fingerprint, phase timings, run distribution, resource metrics, and invariant results.
4. A failed threshold or invalid dataset fails the evidence lane with a documented reason.
5. NFR13 traceability is updated only after a valid corrected run.

### 5.4 Proposed test refactoring

**Current test:**

`ColdStartRehydration_CompletesWithin30Seconds_With500KEvents`

**Proposed disposition:**

- Rename to `ColdFullCommand_AfterActorDeactivation_ReportsLatency_With500KEventDataset`.
- Remove “startup,” “ready-state,” and NFR13 compliance wording.
- Move dataset preparation behind the validated EventStore dataset-builder fixture.
- Report latency rather than applying the inherited 30-second startup assertion unless a separate cold-command SLO is approved.

**Add:**

- `RestartToReady_CompletesWithin30Seconds_WithValidated500KEventDataset`.
- `AggregateReconstruction_ReportsSnapshotAndTailReplayLatency_WithValidatedDataset`.
- Dataset equivalence tests that read the generated state through production EventStore readers and assert reconstructed end state.
- Phase-level benchmark output for topology start, dataset restore/build, validation, process restart, dependency readiness, actor activation/replay, command pipeline, cleanup, memory, and GC.

### 5.5 Proposed architecture decisions

1. **Measurement boundary:** NFR13 measures process restart-to-ready, not actor reactivation or a command response.
2. **Platform ownership:** reusable persisted EventStore dataset construction belongs to `Hexalith.EventStore.Testing.Integration`.
3. **Format fidelity:** the builder may optimize construction but must emit the same persisted format and invariants consumed by production readers.
4. **Position allocation:** reserve global positions by range rather than invoking the singleton global-position actor once per seed event.
5. **Isolation:** never run this benchmark against a long-lived shared Redis dataset.
6. **Evidence split:** setup cost is excluded from the NFR stopwatch but is always measured and reported.
7. **Observer policy:** setup uses bounded/no-op observation sinks unless retained observations are part of the behavior under measurement.
8. **Optimization order:** instrument first, correct dataset construction second, tune concurrency last.

### 5.6 Proposed sprint-status changes after approval

Append the following backlog entries without reopening completed UI epics:

```yaml
epic-6: backlog
6-1-ratify-reconstruction-performance-contract: backlog
6-2-provide-deterministic-eventstore-benchmark-dataset-builder: backlog
6-3-isolate-and-validate-scheduled-benchmark-storage: backlog
6-4-split-and-instrument-tenants-performance-benchmarks: backlog
6-5-make-scheduled-performance-evidence-executable-and-reviewable: backlog
```

Add cross-repository dependencies to the epic/story records:

- Story 6.2 cannot start without explicit EventStore submodule/repository approval.
- Story 6.5 cannot modify `references/Hexalith.Builds` without explicit Builds approval.
- Story 6.4 depends on Stories 6.1–6.3.
- NFR13 evidence closure depends on Story 6.5 and a valid scheduled result.

## 6. Checklist Execution Record

### Section 1 — Understand the trigger and context: COMPLETE

- [x] Triggering story/requirement identified: historical NFR13 and Story 7.5 AC5.
- [x] Problem classified as requirement ambiguity, evidence mismatch, inefficient test setup, and cross-repository ownership gap.
- [x] Supporting evidence collected from the investigation, test source, TRX results, history, and shared workflow.

### Section 2 — Epic impact assessment: COMPLETE WITH ACTIONS

- [!] Historical Epic 7 remains valid for AC1–AC4, but later evidence must not imply AC5/NFR13 closure.
- [x] Current UI Epics 1–5 remain viable and unchanged.
- [x] No current epic becomes obsolete.
- [!] New corrective Epic 6 is required.
- [x] Sequencing and cross-repository dependencies are defined.

### Section 3 — Artifact conflict and impact analysis: COMPLETE WITH ACTIONS

- [!] Historical NFR13 needs a canonical clarified contract; the active UI PRD must remain unchanged.
- [!] A focused performance-evidence architecture decision is required; the current UI architecture is unaffected.
- [N/A] UX impact.
- [!] Tenants tests, EventStore testing integration, disposable DAPR/Redis topology, shared Builds workflow, evidence documentation, and sprint status are affected.

### Section 4 — Path-forward evaluation: COMPLETE

- [x] Direct adjustment evaluated as viable.
- [x] Rollback evaluated and rejected.
- [x] Requirement/MVP review evaluated; target change deferred until corrected evidence exists.
- [x] Hybrid path selected: direct correction plus explicit metric clarification.

### Section 5 — Sprint Change Proposal components: COMPLETE

- [x] Issue summary documented.
- [x] Epic, story, artifact, technical, and ownership impact documented.
- [x] Recommended path and rationale documented.
- [x] Specific before/after requirement and story edits provided.
- [x] New epic, stories, acceptance criteria, dependencies, and sprint-status changes proposed.

### Section 6 — Final review and handoff: COMPLETE

- [x] Proposal internally checked for consistency and scope.
- [x] Proposed edits are actionable and ownership boundaries are explicit.
- [x] User reviewed the complete proposal in batch mode.
- [x] User explicitly approved the complete proposal on 2026-07-14.
- [x] Major-change handoff recorded for Product Management and Solution Architecture.
- [x] Epic and sprint-status edits remain an approved replan input; they are not applied before the Major-change handoff.

## 7. Delivery Impact and Risk

### Estimate

| Work | Estimate |
|---|---:|
| Contract ratification and evidence schema | 0.5–1 day |
| EventStore dataset-builder spike and implementation | 3–5 days |
| Isolated topology and dataset validation | 2–3 days |
| Split Tenants benchmarks and instrumentation | 2–4 days |
| Scheduled lane wiring and evidence publication | 1–2 days |
| **Total** | **8.5–15 days** |

These are engineering estimates, not commitments. The largest uncertainty is whether the existing EventStore/DAPR abstraction supports production-equivalent bulk preparation without adding a new test-only platform seam.

### Risks and mitigations

| Risk | Mitigation |
|---|---|
| Fast seeding produces non-production-readable state | Validate every generated dataset through production readers and end-state invariants before timing |
| Direct Redis writes couple tests to implementation details | Require an EventStore-owned supported abstraction and architecture review |
| Restored artifacts drift from schema/runtime versions | Fingerprint EventStore, domain contract, snapshot schema, and DAPR component configuration |
| Benchmark remains noisy | Isolate storage, record environment/resources, run at least three samples, report distribution |
| Scheduled lane silently skips | Set all gate variables and fail when the selected benchmark is not executed |
| Cross-repo work lands out of order | Implement EventStore capability first, Tenants adoption second, Builds lane last; push owning repos before submodule pointers |
| Metric semantics drift again | Put stopwatch boundaries and prohibited evidence substitutions in acceptance criteria and report schema |

### Delivery recommendation

Do not block or reopen the completed Phase 2 UI MVP. Treat this as a corrective reliability/performance evidence epic before any future release or architecture claim cites NFR13 as satisfied.

## 8. Success Criteria

The correction is complete when:

1. The canonical contract distinguishes restart-to-ready, isolated reconstruction, cold full-command latency, and dataset construction.
2. The 30-second threshold has one approved observable boundary.
3. The 500,000-event dataset is reproducible, isolated, production-readable, and cleaned after use.
4. Dataset setup no longer requires 500,000 full actor command lifecycles.
5. Scheduled evidence runs rather than skips and reports at least three samples plus resource and invariant data.
6. The existing cold-command measurement cannot be mistaken for restart-to-ready evidence.
7. NFR13 is marked proven only after the corrected scheduled benchmark passes.

## 9. Handoff Plan

Because this is a Major correction, route the approved proposal first to the Product Manager and Solution Architect for requirement-boundary and ownership decisions, then to the Product Owner/Test Architect for sequencing and evidence criteria.

Implementation order after approval:

1. PM/Test Architect approve the NFR13 metric boundary and evidence schema.
2. Solution Architect approves the EventStore dataset-builder and isolation design.
3. EventStore owner explicitly authorizes and implements Story 6.2.
4. Tenants team implements Stories 6.3–6.4 against the approved EventStore capability.
5. Hexalith.Builds owner explicitly authorizes and implements Story 6.5 workflow changes.
6. Run scheduled-shaped validation, then the scheduled benchmark.
7. Update NFR13 traceability and evidence status from the actual result.

## 10. Approval Record

- **Decision:** Approved
- **Approver:** Administrator
- **Approval date:** 2026-07-14
- **Approved scope:** Complete proposal, including the recommended hybrid course correction, clarified NFR13 contract, corrective Epic 6 plan, cross-repository ownership boundaries, estimates, risks, and handoff sequence
- **Required revisions:** None
- **Submodule authorization:** Not granted by this proposal; must be explicit per owning repository

## 11. Workflow Execution Log

| Field | Record |
|---|---|
| Workflow | `bmad-correct-course` |
| Trigger | Long-running performance-test investigation and invalid NFR13 evidence semantics |
| Mode | Batch |
| User review | Continued after complete-proposal review |
| Decision | Explicitly approved on 2026-07-14 |
| Change scope | Major |
| Artifacts modified by this workflow | This Sprint Change Proposal only |
| Artifacts deliberately not modified | Active UI PRD, UI architecture, `epics.md`, `sprint-status.yaml`, source/tests, and all submodules |
| Routed to | Product Manager and Solution Architect |
| Handoff deliverables | Approved proposal, before/after edits, corrective epic/story plan, ownership map, success criteria, and escalation notice |
| Next control point | PM/Architect ratification of the NFR13 boundary and cross-repository replan before backlog or implementation changes |
