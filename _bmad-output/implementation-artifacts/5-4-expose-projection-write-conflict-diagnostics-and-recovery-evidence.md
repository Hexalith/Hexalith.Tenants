---
baseline_commit: 54376be
---

# Story 5.4: Expose Projection Write Conflict Diagnostics and Recovery Evidence

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform operator,
I want projection write conflicts to be observable and recoverable,
so that projection failures are not mistaken for successful read-model updates.

## Acceptance Criteria

1. **Given** a projection write conflict exceeds the retry limit
   **When** the projection cannot safely persist state
   **Then** the failure is observable through structured logs or metrics
   **And** the projection does not falsely report a successful update.

2. **Given** replay or repair evidence is needed
   **When** projection conflict diagnostics are inspected
   **Then** logs or metrics include support-safe tenant, domain, aggregate, projection type, event type, correlation, causation, and retry metadata
   **And** they do not expose raw payloads, tokens, secrets, or PII.

3. **Given** the current projection DTO does not expose causation IDs
   **When** diagnostics are emitted from `TenantProjectionWritePolicy`
   **Then** the implementation must not fabricate causation values or widen the EventStore projection DTO contract without an explicit architecture decision
   **And** the diagnostic shape must make the causation gap explicit and support-safe, using available event message IDs as replay evidence.

4. **Given** projection write diagnostics are emitted for tenant detail, tenant audit, and tenant index writes
   **When** metrics or spans are recorded
   **Then** metric/tag dimensions use bounded, low-cardinality values for state key category, projection type, reason, and success/failure
   **And** high-cardinality tenant IDs, aggregate IDs, message IDs, correlation IDs, and event ID lists stay in structured logs or trace tags only, not metrics.

5. **Given** a projection write conflict is recovered before the retry limit
   **When** the guarded save eventually succeeds
   **Then** conflict attempts are still observable
   **And** no retry-exhausted metric, error span, or exception is emitted for the recovered write.

6. **Given** this story is completed
   **When** existing Epic 5 projection write conformance tests run
   **Then** tenant detail, tenant index, and tenant audit write-safety behavior remains unchanged
   **And** no query endpoint, cursor, authorization, REST response, audit retention, or UI behavior changes are introduced.

## Tasks / Subtasks

- [x] Task 1: Confirm the existing projection write diagnostics surface before editing (AC: #1, #2, #6)
  - [x] Read `TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync` and `SaveMergedWithOptimisticConcurrencyAsync`; preserve the 3-attempt retry budget, reload-before-save behavior, `ConcurrencyMode.FirstWrite`, and exception-on-exhaustion behavior.
  - [x] Verify current structured log EventIds remain `100101` for guarded-save conflicts and `100102` for retry exhaustion.
  - [x] Verify existing state key categories stay `tenant read-model`, `tenant audit`, and `tenant index`.
  - [x] Verify existing state keys stay `projection:tenants:{tenantId}`, `audit:{tenantId}`, and `projection:tenant-index:singleton`.
  - [x] Do not replace `TenantProjectionWritePolicy`, `ITenantProjectionStateStore`, or `DaprTenantProjectionStateStore` with a new repository, Redis adapter, public DAPR helper, or `CachingProjectionActor` rewrite.

- [x] Task 2: Add explicit recovery-evidence fields without leaking payload data (AC: #2, #3)
  - [x] Extend the policy diagnostic shape to expose support-safe `TenantId`, `Domain`, `AggregateId`, `ProjectionType`, `EventTypes`, `CorrelationId`, available event `MessageIds`, attempt count, max attempts, reason, operation context, and state-store/key category.
  - [x] Derive tenant/domain/aggregate/projection values from the `ProjectionRequest` path or explicit policy parameters; do not parse raw event payloads for diagnostics.
  - [x] Keep `MessageIds` and `EventTypes` bounded; preserve the current limits unless there is a documented reason to change them.
  - [x] Treat causation as unavailable in the current baseline because `ProjectionEventDto` deliberately excludes `CausationId`; expose an explicit support-safe placeholder or field such as `CausationIdStatus = "unavailable-from-projection-dto"` rather than inventing a value.
  - [x] If true causation IDs are judged mandatory, stop and record a separate EventStore contract story; do not silently edit the submodule contract in this story.

- [x] Task 3: Add metrics and/or span evidence for conflict and exhaustion outcomes (AC: #1, #4, #5)
  - [x] Reuse `TenantMetrics` and `TenantActivitySource`; do not introduce a second telemetry framework, custom static meter, or new package dependency.
  - [x] Add a projection-write metric such as `tenants.projection.write.conflicts` or equivalent, recorded on each conflict attempt with low-cardinality tags only.
  - [x] Add a retry-exhausted metric or failure dimension so operators can alert on terminal projection write failures.
  - [x] If spans are added, use `TenantActivitySource` and set status to error only for retry exhaustion, not for a recovered conflict.
  - [x] Sanitize metric dimensions by whitelist or bounded mapping; unknown state categories/projection types should collapse to `unknown`, not create unbounded cardinality.
  - [x] Keep tenant ID, aggregate ID, correlation ID, message IDs, and event ID lists out of metrics.

- [x] Task 4: Preserve failure semantics and replay recovery contract (AC: #1, #5, #6)
  - [x] Confirm retry exhaustion still throws `InvalidOperationException`; callers must not receive a success response when persistence did not complete.
  - [x] Confirm `TenantProjectionHandler.ProjectAsync` does not return `ProjectionResponse` after retry exhaustion on tenant detail, audit, or index writes.
  - [x] Preserve cross-key non-atomicity: tenant detail may commit before audit failure, and audit may commit before index failure; replay/idempotency remains the recovery contract.
  - [x] Do not add rollback, two-phase commit, compensating writes, audit retention, backoff/jitter, or DAPR transport exception classification unless required to keep tests green.

- [x] Task 5: Extend focused tests around diagnostics and observability (AC: #1-#6)
  - [x] Add or update tests in `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs` to assert the new structured fields for all three write paths.
  - [x] Add or update telemetry tests in `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantMetricsTests.cs` and, if spans are introduced, `TenantActivitySourceTests.cs`.
  - [x] Cover both conflict-then-success and retry-exhaustion cases so recovered conflicts do not look terminal.
  - [x] Add negative assertions for tenant names, payload user IDs, configuration values, raw payload JSON, tokens, secrets, and unbounded event lists.
  - [x] Keep tests driving production code through `TenantProjectionHandler.ProjectAsync` and the conformance fixture; do not call private helpers by reflection.

- [x] Task 6: Run focused and regression validation (AC: #1-#6)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~ProjectionWriteConformanceTests|FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~TenantMetricsTests|FullyQualifiedName~TenantActivitySourceTests"`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/`.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore`.
  - [x] If VSTest socket setup is blocked in the sandbox, run the built xUnit v3 test executable directly and record the runner limitation separately from product failures.

## Dev Notes

### Current Implementation Posture

Story 5.4 follows the refreshed Epic 5 sequence after Stories 5.1-5.3 validated durable projection writes for tenant detail, tenant index, and tenant audit. The implementation should make conflict/recovery evidence easier to observe; it must not redesign the write path.

Canonical write and diagnostic surfaces:

- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs`
- `src/Hexalith.Tenants/Projections/ITenantProjectionStateStore.cs`
- `src/Hexalith.Tenants/Projections/DaprTenantProjectionStateStore.cs`
- `src/Hexalith.Tenants/Telemetry/TenantMetrics.cs`
- `src/Hexalith.Tenants/Telemetry/TenantActivitySource.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceFixture.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantMetricsTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantActivitySourceTests.cs`

`TenantProjectionWritePolicy` already emits structured logs:

- EventId `100101`, `Warning`: optimistic concurrency conflict before retry.
- EventId `100102`, `Error`: retry exhausted, followed by `InvalidOperationException`.
- Existing fields: `StateStoreName`, `StateKeyCategory`, `AttemptCount`, `MaxAttempts`, `OperationContext`, `Reason`, `CorrelationId`, bounded `MessageIds`, and bounded `EventTypes`.

The likely implementation gap is metric/span observability and richer support-safe routing context, not the absence of basic logs. Keep this story focused on making the existing diagnostics operationally useful.

### Current State Of Files To Touch

`TenantProjectionHandler.ProjectAsync` currently:

- Rejects null or whitespace `AggregateId` before state-store access.
- Returns a default `TenantReadModel` without state-store access for empty or all-null event batches.
- Builds incoming audit state before any state-store write so audit invariant failures abort early.
- Saves tenant detail first at `projection:tenants:{aggregateId}` with state category `tenant read-model`.
- Saves tenant audit second at `audit:{aggregateId}` with state category `tenant audit` and operation context `TenantProjectionHandler.ProjectAsync:{aggregateId}`.
- Saves singleton tenant index third at `projection:tenant-index:singleton` with state category `tenant index`.
- Returns the tenant-detail projection response only after all three write paths complete.

`TenantProjectionWritePolicy` currently:

- Uses `GetStateAndETagAsync` plus guarded `TrySaveStateAsync`.
- Uses `ConcurrencyMode.FirstWrite`.
- Retries up to `MaxAttempts = 3`.
- Logs conflict and retry exhaustion with support-safe, bounded message IDs and event types.
- Throws `InvalidOperationException` on retry exhaustion so the projection path cannot report success.

Do not move this logic into controllers, query actors, or read models. Query endpoints and actor filtering are downstream consumers of projection state; they are not the write-conflict diagnostic surface.

### Causation Metadata Constraint

Epic AC requests causation metadata, but the current EventStore projection wire DTO explicitly excludes `CausationId`, `AggregateId`, `TenantId`, and `Domain` to maintain the security boundary. The DTO exposes `EventTypeName`, payload bytes, serialization format, sequence number, timestamp, `CorrelationId`, optional `MessageId`, and optional `UserId`.

Use available values and make unavailable values explicit. Do not:

- invent a causation ID from correlation ID or message ID,
- parse raw payloads to recover operational metadata,
- log raw payloads,
- expand the EventStore submodule contract without a separate approved story,
- add high-cardinality IDs to metric tags.

If product insists on true causation IDs in projection diagnostics, that is a cross-repo contract change in `Hexalith.EventStore`, not a narrow Tenants observability patch. [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Projections/ProjectionEventDto.cs]

### Architecture Guardrails

- EventStore remains the source of truth; query read models are projections and must not become authoritative write state. [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture]
- Projection state uses EventStore projection conventions and DAPR state abstraction; shared and concurrent projection writes must use ETag/optimistic concurrency or verified equivalent behavior. [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture]
- Epic 5 projection safety work belongs in `Contracts/Queries`, `Server/Projections`, `src/Hexalith.Tenants/Controllers`, `src/Hexalith.Tenants/Queries`, cursor/pagination utilities, projection write policy, and projection recovery tests. This story should stay on projection write diagnostics. [Source: _bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping]
- Security-sensitive logs must not contain command payloads, event payloads, tokens, secrets, or PII. [Source: _bmad-output/planning-artifacts/architecture.md#Authentication & Security]
- Metrics should use low-cardinality dimensions. Keep tenant, aggregate, correlation, message, and event ID detail in structured logs or trace tags only. [Source: _bmad-output/project-context.md#Framework-Specific Rules]

### Story Boundaries

In scope:

- Structured diagnostic enrichment for projection write conflicts and retry exhaustion.
- Metrics and/or spans for conflict attempts and terminal retry exhaustion.
- Support-safe recovery evidence for tenant detail, tenant audit, and tenant index writes.
- Tests proving recovered conflicts and exhausted conflicts are distinguishable.
- Tests proving diagnostics remain bounded and do not leak raw payloads, tokens, secrets, tenant names, configuration values, or payload user IDs.

Out of scope:

- Changing projection persistence semantics.
- Adding query endpoints or changing query response shapes.
- Query-side authorization and cursor signing changes.
- Audit retention, archival, compaction, or external storage.
- Backoff/jitter, retry budget changes, or DAPR transport exception classification.
- EventStore submodule projection contract changes.
- Phase 2 Admin UI freshness, lifecycle, or audit timeline work.

### Testing Requirements

Use xUnit v3 and Shouldly. Tests should stay in existing Server test folders and follow current naming conventions: plural test classes and `snake_case_with_PascalCase_for_type_names` for test methods where the surrounding file uses that style.

Preferred test surfaces:

- `ProjectionWriteConformanceTests`: end-to-end production path through `TenantProjectionHandler.ProjectAsync`.
- `ProjectionWriteConformanceFixture`: scripted state-store conflicts and captured structured logs.
- `TenantProjectionHandlerTests`: focused handler behavior and failure semantics.
- `TenantMetricsTests`: metric name and tag sanitization.
- `TenantActivitySourceTests`: span names and tag constants if spans are added.

Avoid tests that duplicate merge/retry logic. The conformance fixture exists to observe the production write policy.

### Previous Story Intelligence

Story 5.1 established that refreshed Epic 5 projection safety stories should reuse the existing Epic 10 machinery instead of recreating persistence. It also established the VSTest socket limitation and direct xUnit executable fallback in this sandbox.

Story 5.2 extended the same write policy to the singleton tenant index and reinforced that cross-key writes are not atomic. Replay/idempotency, not rollback, is the recovery contract.

Story 5.3 validated audit write safety and already proved:

- `TenantProjectionWritePolicy` is the canonical write path for tenant detail, tenant index, and audit.
- Audit merge is persisted-authoritative by `EventId`.
- Logs already include EventIds `100101` and `100102`, bounded `MessageIds`, bounded `EventTypes`, retry counts, reason, state store, key category, operation context, and correlation ID.
- Diagnostic tests must include negative assertions for tenant names, payload user IDs, narrative payloads, configuration values, raw payloads, tokens, and secrets.

Carry forward the same narrow-boundary discipline. This story should improve evidence, not reopen durable projection design.

### Git Intelligence

Recent history before story creation:

- `54376be feat(story-5.3): Persist the Tenant Audit Projection Without Silent Write Loss`
- `7ddd400 feat(story-5.2): Persist the Shared Tenant Index Projection Without Silent Write Loss`
- `9345bf0 feat(story-5.1): Persist Per-Tenant Detail Projections Without Silent Write Loss`
- `f67eecd docs(retro): finalize Epic 4 retrospective`
- `7f2a89e feat(story-4.6): Provide Idempotent Consumer Guidance and Sample Service`

The three immediate predecessor commits are directly relevant. Reuse their conformance fixture and telemetry test style.

### Latest Technical Specifics

No dependency upgrade is part of this story. Use the versions pinned by the repository:

- .NET SDK `10.0.300`
- DAPR SDK `1.17.9`
- OpenTelemetry `1.15.x` package family
- xUnit v3 `3.2.2`
- Shouldly `4.3.0`

Do not add new telemetry packages. `System.Diagnostics.Metrics`, `ActivitySource`, `TenantMetrics`, and `TenantActivitySource` are already present.

### Project Structure Notes

- Production projection write code belongs under `src/Hexalith.Tenants/Projections`.
- Telemetry helpers belong under `src/Hexalith.Tenants/Telemetry`.
- Server projection tests belong under `tests/Hexalith.Tenants.Server.Tests/Projections`.
- Telemetry tests belong under `tests/Hexalith.Tenants.Server.Tests/Telemetry`.
- Do not place EventStore-discovered projection types outside `Hexalith.Tenants.Server`; this story should not need new EventStore-discovered projection types.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.4: Expose Projection Write Conflict Diagnostics and Recovery Evidence]
- [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture]
- [Source: _bmad-output/planning-artifacts/architecture.md#Authentication & Security]
- [Source: _bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping]
- [Source: _bmad-output/project-context.md#Projections]
- [Source: _bmad-output/project-context.md#API Surface]
- [Source: _bmad-output/project-context.md#Testing Rules]
- [Source: _bmad-output/implementation-artifacts/5-3-persist-the-tenant-audit-projection-without-silent-write-loss.md]
- [Source: _bmad-output/implementation-artifacts/epic-10-retro-2026-05-19.md#Scope Completed]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Projections/ProjectionEventDto.cs]

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Verified existing projection write policy surface before edits: 3-attempt retry budget, reload-before-save, `ConcurrencyMode.FirstWrite`, EventIds `100101`/`100102`, state key categories, and projection keys remain the baseline.
- Red phase: focused `dotnet test` reached compile failure for missing `TenantMetrics.RecordProjectionWriteConflict`, confirming new metric test coverage before implementation.
- Validation: VSTest runner socket setup is blocked in this sandbox (`System.Net.Sockets.SocketException (13): Permission denied`), so direct xUnit v3 executable fallback was used after successful builds.
- Focused validation passed: `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --no-restore -m:1 -nr:false /p:UseSharedCompilation=false /p:NuGetAudit=false && tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -parallel none -class Hexalith.Tenants.Server.Tests.Projections.ProjectionWriteConformanceTests -class Hexalith.Tenants.Server.Tests.Projections.TenantProjectionHandlerTests -class Hexalith.Tenants.Server.Tests.Telemetry.TenantMetricsTests -class Hexalith.Tenants.Server.Tests.Telemetry.TenantActivitySourceTests -class Hexalith.Tenants.Server.Tests.Telemetry.TenantProjectionWritePolicyMetricsTests` (68 tests passed).
- Regression validation passed: `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -parallel none` (587 tests passed).
- Release validation passed: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 -nr:false /p:UseSharedCompilation=false /p:NuGetAudit=false` (0 warnings, 0 errors).

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Checklist validation completed during story creation; causation metadata conflict is called out explicitly.
- Added `ProjectionWriteDiagnosticsContext` so tenant/domain/aggregate/projection evidence flows from `ProjectionRequest` into guarded projection writes without parsing event payloads or changing the EventStore DTO.
- Enriched projection write conflict and retry-exhausted structured logs with support-safe routing fields and explicit `CausationIdStatus = "unavailable-from-projection-dto"` while preserving EventIds `100101` and `100102`.
- Added `tenants.projection.write.conflicts` metric through `TenantMetrics` with whitelisted low-cardinality dimensions only: state key category, projection type, reason, and success/failure.
- Senior review auto-fix added production-path metric coverage for recovered tenant audit conflicts so tenant detail, tenant audit, and tenant index paths are all asserted through `TenantProjectionHandler.ProjectAsync`.
- Preserved retry budget, reload-before-save, `ConcurrencyMode.FirstWrite`, retry-exhaustion `InvalidOperationException`, and cross-key replay/idempotency semantics.

### File List

- `_bmad-output/implementation-artifacts/5-4-expose-projection-write-conflict-diagnostics-and-recovery-evidence.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-5-20260601-061130.md`
- `src/Hexalith.Tenants/Projections/ProjectionWriteDiagnosticsContext.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs`
- `src/Hexalith.Tenants/Telemetry/TenantMetrics.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantMetricsTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Telemetry/TenantProjectionWritePolicyMetricsTests.cs`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after auto-fixes. No critical issues remain.

Findings fixed:

- MEDIUM: Production-path metric coverage did not assert the tenant audit write path, leaving AC4/Task 5 under-verified for one of the three required projection write paths. Fixed by adding `ProjectAsync_RecoveredTenantAuditConflict_RecordsAuditConflictMetricOnlyAsync`.
- MEDIUM: The story File List omitted actual changed files, including `TenantProjectionWritePolicyMetricsTests.cs`, test summary, and orchestration tracking. Fixed by updating the File List.
- LOW: Validation notes had stale focused/regression test counts after telemetry coverage changed. Fixed by updating the recorded commands and counts.

Checklist validation:

- [x] Story file loaded from `_bmad-output/implementation-artifacts/5-4-expose-projection-write-conflict-diagnostics-and-recovery-evidence.md`
- [x] Story Status verified as reviewable (`review`) before review and updated to `done`
- [x] Epic and Story IDs resolved as 5.4
- [x] Story Context located in `_bmad-output/project-context.md`
- [x] Epic Tech Spec located in `_bmad-output/planning-artifacts/epics.md`
- [x] Architecture/standards docs loaded from `_bmad-output/planning-artifacts/architecture.md` and `_bmad-output/project-context.md`
- [x] Tech stack detected: .NET 10, C# latest, xUnit v3, Shouldly, DAPR state store, OpenTelemetry metrics
- [x] MCP/web doc lookup not used; local project context and architecture docs were authoritative for this repository-scoped review
- [x] Acceptance Criteria cross-checked against implementation
- [x] File List reviewed and corrected for completeness
- [x] Tests identified and mapped to ACs; audit metric coverage gap fixed
- [x] Code quality review performed on changed source files
- [x] Security review performed for diagnostic payload/PII leakage and metric cardinality
- [x] Outcome decided: Approved after fixes
- [x] Review notes appended under "Senior Developer Review (AI)"
- [x] Change Log updated with review entry
- [x] Status updated to `done`
- [x] Sprint status synced
- [x] Story saved successfully

### Change Log

- 2026-06-01: Enriched projection write diagnostics and metrics for tenant detail, tenant audit, and tenant index conflict/retry outcomes.
- 2026-06-01: Senior review auto-fix added tenant audit production-path metric coverage, corrected review documentation, and marked story done.
