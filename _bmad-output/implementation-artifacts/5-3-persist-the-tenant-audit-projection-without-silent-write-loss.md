---
baseline_commit: 7ddd4008
---

# Story 5.3: Persist the Tenant Audit Projection Without Silent Write Loss

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform operator,
I want the tenant audit projection to handle concurrent writes safely,
so that audit reports remain complete and ordered under access-change concurrency.

## Acceptance Criteria

1. **Given** multiple access-change events update the audit projection close together
   **When** audit state is persisted
   **Then** every successfully processed audit event remains queryable by date range and pagination cursor
   **And** ordering remains deterministic.

2. **Given** audit projection write conformance tests run
   **When** tenant audit projection writes race
   **Then** tests prove no silent data loss, deterministic recovery behavior, and enough diagnostics for replay or repair.

3. **Given** an audit save conflicts after another writer has persisted audit entries for the same tenant
   **When** the retry reloads `audit:{tenantId}`
   **Then** the saved audit state preserves the pre-existing entries, the concurrently added entries, and the incoming access-change events
   **And** final entries are ordered by `Timestamp` then `EventId`.

4. **Given** duplicate `EventId` values appear during replay or retry
   **When** the audit merge combines persisted and incoming audit state
   **Then** the persisted entry is authoritative for that `EventId`
   **And** distinct entries with the same timestamp but different `EventId` values are preserved.

5. **Given** tenant-audit retry exhaustion occurs
   **When** the write policy gives up
   **Then** the failure is observable through structured logs and an exception
   **And** logs include support-safe state-store, key category, attempts, operation, reason, correlation, bounded message IDs, and bounded event types
   **And** logs do not include tenant names, payload user IDs, configuration values, raw payloads, tokens, or secrets.

6. **Given** replay occurs after a successful audit save or after later projection partial success
   **When** the same projection batch is processed again
   **Then** `TenantAuditReadModel` remains idempotent by `EventId`
   **And** audit entries are not duplicated or silently dropped.

7. **Given** this story validates the tenant-audit write path
   **When** implementation is complete
   **Then** tenant detail, singleton tenant index, query authorization, cursor signing, and REST response behavior remain unchanged except for test updates needed to keep existing suites passing.

## Tasks / Subtasks

- [x] Task 1: Confirm the existing tenant-audit persistence path is the only implementation surface (AC: #1, #3, #7)
  - [x] Verify `TenantProjectionHandler.ProjectAsync` writes `audit:{request.AggregateId}` through `TenantProjectionWritePolicy.SaveMergedWithOptimisticConcurrencyAsync`.
  - [x] Verify the audit write uses key category `tenant audit`, state store `statestore`, and operation context `TenantProjectionHandler.ProjectAsync:{aggregateId}`.
  - [x] Verify `DaprTenantProjectionStateStore` uses `GetStateAndETagAsync` plus guarded `TrySaveStateAsync` for the audit key and does not use plain `SaveStateAsync`.
  - [x] Do not create a second audit repository, Redis adapter, DAPR client wrapper, query-specific audit store, retention mechanism, or `CachingProjectionActor` replacement.

- [x] Task 2: Prove audit conflict retry preserves all queryable entries (AC: #1, #2, #3)
  - [x] Drive production behavior through `TenantProjectionHandler.ProjectAsync`, not through private merge helpers or duplicated test-only audit merge logic.
  - [x] Script a conflict where attempt 1 reload sees persisted audit entry A, another writer persists audit entry B, attempt 2 reload sees A+B, and incoming access-change events add C/D.
  - [x] Assert the final saved `TenantAuditReadModel.Entries` contains A, B, C, and D with no loss.
  - [x] Assert final ordering is deterministic by `Timestamp` then `EventId`.
  - [x] Assert each audit save uses `ConcurrencyMode.FirstWrite` and the ETag from that attempt's fresh reload.
  - [x] Assert the saved audit state remains queryable through the existing `get-tenant-audit` actor path by date range and protected pagination cursor where current test seams make that observable.

- [x] Task 3: Prove duplicate/replay semantics are idempotent and persisted-authoritative (AC: #2, #4, #6)
  - [x] Preserve `MergeAuditState` behavior where non-blank persisted `EventId` values win over incoming duplicates.
  - [x] Add or keep coverage where a persisted duplicate has a different `EventType`, `ActorId`, `Timestamp`, or `NarrativePayload` than the incoming duplicate and the saved duplicate remains the persisted version.
  - [x] Reprocess a projection batch after an audit save has already succeeded and prove entries are not duplicated.
  - [x] Preserve malformed JSON skip behavior in `TenantAuditProjection.ProjectAuditEvents` and invariant-failure behavior for missing `MessageId`/`UserId`.
  - [x] Preserve global-administrator audit event classification and tenant-domain audit event classification.

- [x] Task 4: Prove retry exhaustion is observable and support-safe (AC: #2, #5)
  - [x] Script `TenantProjectionWritePolicy.MaxAttempts` failed guarded saves for `audit:{tenantId}`.
  - [x] Assert the thrown `InvalidOperationException` identifies `tenant audit` and the retry limit.
  - [x] Assert conflict logs use EventId `100101` and retry-exhausted logs use EventId `100102`.
  - [x] Assert structured fields include `StateStoreName`, `StateKeyCategory`, `AttemptCount`, `MaxAttempts`, `OperationContext`, `Reason`, `CorrelationId`, `MessageIds`, and `EventTypes`.
  - [x] Assert diagnostic output excludes tenant names, narrative payload values, payload user IDs, configuration values, raw payloads, tokens, and secrets.
  - [x] Assert large event batches keep `MessageIds` and `EventTypes` bounded.

- [x] Task 5: Keep story boundaries tight (AC: #7)
  - [x] Do not add or redesign REST query endpoints, cursor codec payloads, query-side authorization, tenant-list/detail/user query behavior, UI freshness states, or global-admin query policy.
  - [x] Do not implement audit retention, archival, compaction, external blob storage, or audit timeline UX in this story; record retention pressure as deferred work if encountered.
  - [x] Do not rename `statestore`, `audit:{tenantId}`, `projection:tenants:{tenantId}`, or `projection:tenant-index:singleton`.
  - [x] If backoff/jitter, DAPR transport exception classification, per-string log length caps, or audit blob growth is discovered, record it as deferred work unless required to satisfy the ACs.

- [x] Task 6: Run focused and regression validation (AC: #1-#7)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~ProjectionWriteConformanceTests|FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~ProjectionDispatcherTests|FullyQualifiedName~TenantsProjectionActorTests"`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/`.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore`.
  - [x] If VSTest socket setup is blocked in the sandbox, run the built xUnit v3 test executable directly as established by Stories 5.1 and 5.2 and record the runner limitation separately from product failures.

## Dev Notes

### Current Implementation Posture

This story is part of the refreshed Epic 5 sequence created by the 2026-05-31 correction. The tenant-audit read model, audit query path, and durable write policy already exist from earlier Epic 5/Epic 9/Epic 10 work. Treat them as current implementation to validate and preserve, not as permission to recreate audit storage.

The canonical tenant-audit write path is:

- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs`
- `src/Hexalith.Tenants/Projections/ITenantProjectionStateStore.cs`
- `src/Hexalith.Tenants/Projections/DaprTenantProjectionStateStore.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantAuditProjection.cs`
- `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`

The queryability proof should use the existing audit query surface where needed:

- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryPaginationPolicy.cs`

Do not introduce another audit state store, direct Redis dependency, database table, repository abstraction, query-only audit projection, or public DAPR helper. `TenantProjectionWritePolicy` is intentionally internal and already load-bearing for tenant detail, singleton index, and audit projection writes. [Source: _bmad-output/implementation-artifacts/epic-10-retro-2026-05-19.md#Scope Completed]

### Current State Of Files To Touch

`TenantProjectionHandler.ProjectAsync` currently:

- Validates `ProjectionRequest`, rejects null/whitespace `AggregateId`, and returns a default `TenantReadModel` without state-store access for empty or all-null event batches.
- Builds incoming audit state before any writes so missing `MessageId`/`UserId` invariant failures abort before tenant-detail, audit, or index state commits.
- Saves per-tenant detail state at `projection:tenants:{aggregateId}` first.
- Saves audit state at `audit:{aggregateId}` second through `TenantProjectionWritePolicy.SaveMergedWithOptimisticConcurrencyAsync`.
- Saves the singleton tenant index at `projection:tenant-index:singleton` third.
- Returns the tenant-detail `TenantReadModel`, not audit state.

This ordering means tenant detail may have committed before audit retry exhaustion. Do not claim cross-key atomicity. The recovery contract is observable failure plus idempotent replay, not rollback across keys. [Source: _bmad-output/implementation-artifacts/epic-10-retro-2026-05-19.md#Key takeaways]

`TenantProjectionWritePolicy.SaveMergedWithOptimisticConcurrencyAsync` currently:

- Reads state and ETag with `GetStateAndETagAsync`.
- Merges loaded persisted state with incoming audit state.
- Saves with `TrySaveStateAsync` using `ConcurrencyMode.FirstWrite`.
- Retries up to `MaxAttempts = 3`.
- Logs EventId `100101` for conflicts and `100102` for retry exhaustion, then throws `InvalidOperationException`.
- Bounds logged message IDs and event types.

Preserve this guarded-write behavior. Do not replace it with `SaveStateAsync`, last-writer-wins, unbounded retry, or a public abstraction.

`TenantProjectionHandler.MergeAuditState` currently:

- Copies persisted entries into a new `TenantAuditReadModel` before merging, avoiding mutation of a state-store-returned instance.
- Builds a deduplication set from non-blank persisted `EventId` values.
- Skips incoming entries with blank `EventId`.
- Adds only incoming entries whose `EventId` has not already been seen.
- Sorts final entries by `Timestamp` and then `EventId`.

Preserve the persisted-authoritative duplicate behavior. It prevents replay from replacing an already persisted audit entry with a later regenerated entry that happens to carry the same `EventId`.

`TenantAuditReadModel` currently:

- Stores audit entries in `Entries`.
- Creates access entries for `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `GlobalAdministratorSet`, and `GlobalAdministratorRemoved`.
- Creates administrative entries for tenant lifecycle and configuration events.
- Throws when `MessageId` or `UserId` is missing.
- Sorts by `Timestamp` then `EventId`.
- Does not implement retention, archival, or compaction.

`TenantAuditProjection` is a static helper, not an EventStore-discoverable projection class. Keep it static unless a separate architecture decision changes projection discovery. It skips malformed JSON payloads but preserves invariant failures.

`TenantsProjectionActor.HandleGetTenantAuditAsync` already supports global-admin-only audit query behavior, date/category filtering, signed cursor scope binding, stable cursor pagination, and defense-in-depth tenant filtering for mismatched audit entries. Use this for queryability evidence if AC #1 needs an end-to-end read assertion.

### Architecture Guardrails

- EventStore events remain the source of truth; query read models are projections and must not become authoritative write state. [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture]
- Shared and concurrent projection writes must use optimistic concurrency/write policy behavior to avoid silent write loss. The current selected path for tenant audit is `SaveMergedWithOptimisticConcurrencyAsync`; do not reopen a different persistence design inside this story. [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture]
- Epic 5 query and projection safety work belongs in `Contracts/Queries`, `Server/Projections`, `src/Hexalith.Tenants/Controllers`, `src/Hexalith.Tenants/Queries`, cursor/pagination utilities, projection write policy, and projection recovery tests. This story is limited to audit projection write safety and evidence. [Source: _bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping]
- Query controllers are thin adapters. Authorization and filtering belong in projection/query handling, not controller branching. [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Technical Impact]
- Projection Apply methods trust events and update read models deterministically. Business validation belongs in aggregate `Handle` methods, not projections. [Source: _bmad-output/project-context.md#Apply Methods (State)]
- Tenants server query projections are separate from Client/local consumer projections. Do not reuse `InMemoryTenantProjectionStore` or consumer package assumptions for server-owned audit state. [Source: _bmad-output/project-context.md#Projections]

### Story Boundaries

In scope:

- Tenant audit persistence for `audit:{tenantId}`.
- ETag/optimistic-concurrency verification for audit saves.
- Reload-and-merge behavior under conflicts.
- Retry-exhaustion behavior and diagnostics for audit writes.
- Replay/idempotency proof for `TenantAuditReadModel`.
- Date-range and cursor queryability evidence for recovered audit entries through existing query surfaces.
- Focused tests that drive production code through `TenantProjectionHandler.ProjectAsync` and the existing actor query path.

Out of scope:

- Per-tenant detail write safety except where existing tests must stay green.
- Shared tenant index write safety except where existing tests must stay green.
- New REST endpoints or response shape changes.
- Query-side authorization redesign.
- Cursor signing redesign, cursor payload schema changes, or Data Protection key persistence.
- Audit retention, compaction, archival, or external storage.
- Phase 2 Admin UI audit timeline, freshness states, or command lifecycle UX.
- Public package API expansion.

### Testing Requirements

Use xUnit v3 and Shouldly. Tests should live under `tests/Hexalith.Tenants.Server.Tests/Projections/` and should follow existing conventions: plural test classes and `snake_case_with_PascalCase_for_type_names` when adding new methods near existing files.

Preferred existing test surfaces:

- `ProjectionWriteConformanceTests` for race/retry/recovery proof across production `TenantProjectionHandler.ProjectAsync`.
- `ProjectionWriteConformanceFixture` for scripted per-key state-store reads/saves and captured structured logs.
- `TenantProjectionHandlerTests` for audit merge behavior, retry exhaustion, cancellation, null deserialization hardening, and partial-success replay checks.
- `TenantAuditReadModelTests` and `TenantAuditProjectionTests` for classification, sorting, malformed payload, and invariant behavior.
- `TenantsProjectionActorTests` for date/category filtering, signed cursor pagination, global-admin audit access, and recovered-entry queryability.
- `ProjectionDispatcherTests` for DAPR call shape and guarded-save regression checks.

Do not create tests that call private `MergeAuditState` by reflection or duplicate production merge logic in test-only helpers. The conformance fixture exists to observe the production path through `ProjectAsync`.

### Previous Story Intelligence

Story 5.1 is done and established the implementation posture for refreshed Epic 5:

- Existing Epic 10 projection write-safety machinery should be reused directly.
- `TenantProjectionHandler.ProjectAsync`, `TenantProjectionWritePolicy`, and `DaprTenantProjectionStateStore` are the canonical persistence surfaces.
- VSTest may build successfully and then fail in this sandbox with a socket permission error; direct xUnit v3 executable runs are the established fallback.
- Story boundaries should stay narrow: no query endpoints, cursor signing, authorization redesign, or UI work unless the story explicitly asks for it.

Story 5.2 is done and extended the same posture to the singleton tenant index:

- Tests should drive production behavior through `ProjectAsync`.
- Retry exhaustion must be observable through EventIds `100101` and `100102`.
- Structured diagnostics must remain support-safe and bounded.
- Cross-key writes are not atomic; replay/idempotency is the recovery contract.
- The current DAPR component name is `statestore`; do not rename topology/configuration in projection-safety stories.

Epic 10 already introduced and validated the audit write-safety machinery:

- `SaveMergedWithOptimisticConcurrencyAsync`
- `MergeAuditState`
- persisted-authoritative duplicate `EventId` behavior
- stable ordering by `Timestamp` then `EventId`
- malformed JSON skip behavior
- invariant-failure no-save-before-validation
- conformance fixture coverage across tenant detail, singleton index, and audit projections

Carry forward the Epic 10 lesson: conformance fixtures must drive production behavior, not parallel it.

### Git Intelligence

Recent history before story creation:

- `7ddd400 feat(story-5.2): Persist the Shared Tenant Index Projection Without Silent Write Loss`
- `9345bf0 feat(story-5.1): Persist Per-Tenant Detail Projections Without Silent Write Loss`
- `f67eecd docs(retro): finalize Epic 4 retrospective`
- `7f2a89e feat(story-4.6): Provide Idempotent Consumer Guidance and Sample Service`
- `e396f0a feat(story-4.5): React to Tenant Access Lifecycle and Configuration Changes`

The two immediate predecessor commits are directly relevant. Reuse their test style and runner fallback instead of inventing a new validation pattern.

### Latest Technical Specifics

No package or framework upgrade is part of this story. Use the versions pinned by the repository:

- .NET SDK `10.0.300` and `Hexalith.Tenants.slnx`.
- Dapr SDK `1.17.9`.
- xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `6.0.0-rc.1`.
- Central package management only; do not add inline `Version=` metadata.

External web research was not needed because this story uses existing pinned APIs and does not introduce a new package, new external service, or upgrade path.

### Project Structure Notes

The existing source layout matches the architecture boundary:

- Runtime projection handler and DAPR state-store adapter are in `src/Hexalith.Tenants/Projections/`.
- EventStore-discovered projection/read-model types are in `src/Hexalith.Tenants.Server/Projections/`.
- Audit query contracts and DTOs are in `src/Hexalith.Tenants.Contracts/Queries/`.
- Query controller and cursor utilities are in `src/Hexalith.Tenants/Controllers/` and `src/Hexalith.Tenants/Queries/`.
- Server projection tests are in `tests/Hexalith.Tenants.Server.Tests/Projections/`.

Detected variance: planning context mentions EventStore convention state store name `tenants-eventstore`, while current AppHost, health checks, handlers, tests, and DAPR YAML consistently use the DAPR component name `statestore`. Do not rename this inside Story 5.3; topology/configuration renames require separate AppHost and test updates.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.3] - Story statement and acceptance criteria.
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 5] - Projection write safety must precede endpoint delivery.
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Proposal A: Resequence and Split Epic 5] - Resequenced Epic 5 story order and projection-safety split.
- [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture] - Tenant projections, audit projection, EventStore source-of-truth, and DAPR projection state boundary.
- [Source: _bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping] - Epic 5 file locations and projection recovery test scope.
- [Source: _bmad-output/planning-artifacts/prd.md#Requirements] - FR29 audit reporting by tenant/date range with pagination.
- [Source: _bmad-output/project-context.md#Projections] - Projection types, guarded state, and conditional fan-in guidance.
- [Source: _bmad-output/project-context.md#Testing Rules] - xUnit v3, Shouldly, and test-tier rules.
- [Source: _bmad-output/implementation-artifacts/5-1-persist-per-tenant-detail-projections-without-silent-write-loss.md#Previous Work Intelligence] - Previous story implementation posture and validation fallback.
- [Source: _bmad-output/implementation-artifacts/5-2-persist-the-shared-tenant-index-projection-without-silent-write-loss.md#Previous Story Intelligence] - Immediate predecessor story guardrails and conformance posture.
- [Source: _bmad-output/implementation-artifacts/epic-10-retro-2026-05-19.md#Scope Completed] - Existing write policy, audit merge behavior, retry diagnostics, and conformance fixture.
- [Source: _bmad-output/implementation-artifacts/deferred-work.md#Deferred from: code review of 10-2-audit-projection-write-safety] - Audit blob growth and other out-of-scope operational follow-ups.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: Confirmed canonical audit write path remains `TenantProjectionHandler.ProjectAsync` -> `TenantProjectionWritePolicy.SaveMergedWithOptimisticConcurrencyAsync` for `audit:{aggregateId}` using `statestore`, key category `tenant audit`, and operation context `TenantProjectionHandler.ProjectAsync:{aggregateId}`.
- 2026-06-01: Confirmed `DaprTenantProjectionStateStore` exposes guarded ETag reads/saves through `GetStateAndETagAsync` and `TrySaveStateAsync`; no new audit repository/store/adapter/query path was introduced.
- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~ProjectionWriteConformanceTests|FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~ProjectionDispatcherTests|FullyQualifiedName~TenantsProjectionActorTests"` was blocked by sandbox socket permissions during MSBuild/VSTest setup (`System.Net.Sockets.SocketException (13): Permission denied`).
- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Server.Tests/ --no-build --configuration Release -m:1 -nodeReuse:false` was also blocked by VSTest socket setup (`System.Net.Sockets.SocketException (13): Permission denied`).
- 2026-06-01: Built test project with `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-restore -m:1 -nodeReuse:false` successfully.
- 2026-06-01: Focused xUnit executable fallback passed: `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -class Hexalith.Tenants.Server.Tests.Projections.ProjectionWriteConformanceTests -class Hexalith.Tenants.Server.Tests.Projections.TenantProjectionHandlerTests -class Hexalith.Tenants.Server.Tests.Projections.ProjectionDispatcherTests -class Hexalith.Tenants.Server.Tests.Projections.TenantsProjectionActorTests -parallel none -noLogo` => 142 passed, 0 failed.
- 2026-06-01: Full Server.Tests xUnit executable fallback passed: `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -parallel none -noLogo` => 582 passed, 0 failed.
- 2026-06-01: Release solution build passed: `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore` => 0 warnings, 0 errors.

### Completion Notes List

- Added audit retry-exhaustion conformance coverage for `audit:{tenantId}` failed guarded saves, asserting retry exception content, EventIds `100101`/`100102`, structured support-safe fields, bounded `MessageIds`/`EventTypes`, and exclusion of tenant names, payload user IDs, configuration values, tokens, and secrets.
- Preserved the existing production audit write design: no REST/query endpoint changes, cursor codec changes, authorization redesign, retention/archival work, topology renames, or alternate audit storage were added.
- Existing audit conflict, duplicate/replay, malformed payload, invariant-failure, classification, query date-range, and protected cursor coverage remained green through focused and full Server.Tests xUnit runs.

### File List

- `_bmad-output/implementation-artifacts/5-3-persist-the-tenant-audit-projection-without-silent-write-loss.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-5-20260601-061130.md`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs`

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after auto-fix.

### Findings

- MEDIUM: The story File List omitted changed BMAD artifacts (`_bmad-output/implementation-artifacts/tests/test-summary.md` and `_bmad-output/story-automator/orchestration-5-20260601-061130.md`), making the implementation record incomplete. Fixed by adding both files to the File List.
- MEDIUM: `Audit_RetryExhaustion_FailsObservably_WithSupportSafeBoundedDiagnosticsAsync` asserted support-safe logging for envelope/user context but did not include a sensitive user ID inside an audit access-event payload, leaving the AC #5 "payload user IDs" log exclusion partially unproven. Fixed by adding a `UserAddedToTenant` payload with a sensitive target user ID and updating bounded diagnostics expectations.

### Review Notes

- Acceptance criteria #1-#7 were cross-checked against the production path: `TenantProjectionHandler.ProjectAsync` saves `audit:{tenantId}` via `TenantProjectionWritePolicy.SaveMergedWithOptimisticConcurrencyAsync`, using `statestore`, `tenant audit`, fresh ETags, and `ConcurrencyMode.FirstWrite`.
- Duplicate/replay behavior remains persisted-authoritative by `EventId`, and final audit ordering remains `Timestamp` then `EventId`.
- No REST endpoint, query authorization, cursor codec, topology, retention, archival, or alternate audit-store changes were introduced.
- External MCP/web research was not needed; this review used pinned repository APIs and existing local project context.

### Validation

- `dotnet test tests/Hexalith.Tenants.Server.Tests/ --configuration Release --no-build --filter "FullyQualifiedName~ProjectionWriteConformanceTests|FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~ProjectionDispatcherTests|FullyQualifiedName~TenantsProjectionActorTests" -m:1 -nodeReuse:false` - blocked by VSTest socket setup (`System.Net.Sockets.SocketException (13): Permission denied`).
- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-restore -m:1 -nodeReuse:false` - passed: 0 warnings, 0 errors.
- `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -class Hexalith.Tenants.Server.Tests.Projections.ProjectionWriteConformanceTests -class Hexalith.Tenants.Server.Tests.Projections.TenantProjectionHandlerTests -class Hexalith.Tenants.Server.Tests.Projections.ProjectionDispatcherTests -class Hexalith.Tenants.Server.Tests.Projections.TenantsProjectionActorTests -parallel none -noLogo` - passed: 142 total, 0 failed.
- `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -parallel none -noLogo` - passed: 582 total, 0 failed.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 -nodeReuse:false` - passed: 0 warnings, 0 errors.

### Change Log

- 2026-06-01: Added tenant-audit retry-exhaustion conformance coverage and validated existing audit conflict/idempotency/queryability evidence against the focused and full Server.Tests suites.
- 2026-06-01: Senior review auto-fixed File List documentation gaps and strengthened audit retry-exhaustion log-safety coverage for payload user IDs.
