---
baseline_commit: 9345bf0b4f633b4373cf062ed6db469fd2015702
---

# Story 5.2: Persist the Shared Tenant Index Projection Without Silent Write Loss

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform operator,
I want the shared tenant index projection to handle concurrent writes safely,
so that tenant discovery does not silently lose tenant lifecycle events.

## Acceptance Criteria

1. **Given** multiple tenant events update the shared tenant index projection
   **When** the shared projection state is modified
   **Then** conflicting writes are retried or safely failed according to a documented retry policy
   **And** final index state includes all successfully processed events.

2. **Given** shared tenant index projection write conformance tests run
   **When** tenant index projection writes race
   **Then** tests prove no silent data loss, deterministic recovery behavior, and enough diagnostics for replay or repair.

3. **Given** a tenant-index save conflicts after another writer has persisted a different tenant
   **When** the retry reloads the singleton index key
   **Then** the saved index preserves the pre-existing tenants, the concurrently added tenants, and the incoming tenant event.

4. **Given** tenant-index retry exhaustion occurs
   **When** the write policy gives up
   **Then** the failure is observable through structured logs and an exception
   **And** logs include support-safe state-store, key category, attempts, operation, reason, correlation, bounded message IDs, and bounded event types
   **And** logs do not include tenant names, payload user IDs, configuration values, raw payloads, tokens, or secrets.

5. **Given** replay occurs after a successful index save or after earlier projection partial success
   **When** the same projection batch is processed again
   **Then** `TenantIndexReadModel` remains idempotent and does not duplicate or lose tenant or membership entries.

6. **Given** this story validates the shared tenant index write path
   **When** implementation is complete
   **Then** tenant detail and audit write behavior remains unchanged except for test updates needed to keep existing suites passing.

## Tasks / Subtasks

- [x] Task 1: Confirm the singleton tenant-index persistence path is the only implementation surface (AC: #1, #3, #6)
  - [x] Verify `TenantProjectionHandler.ProjectAsync` writes `projection:tenant-index:singleton` through `TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync`.
  - [x] Verify the tenant-index write uses key category `tenant index`, state store `statestore`, and operation context `TenantProjectionHandler.ProjectAsync`.
  - [x] Verify `DaprTenantProjectionStateStore` uses `GetStateAndETagAsync` plus guarded `TrySaveStateAsync` and does not use plain `SaveStateAsync` for the tenant-index key.
  - [x] Do not create a second index repository, Redis adapter, DAPR client wrapper, `CachingProjectionActor` fan-in replacement, or query-specific persistence path.

- [x] Task 2: Prove conflict retry preserves all indexed tenants (AC: #1, #2, #3)
  - [x] Drive production behavior through `TenantProjectionHandler.ProjectAsync`, not by calling private apply helpers or duplicating merge logic in tests.
  - [x] Script a conflict where attempt 1 reload sees tenant A, another writer persists tenant B, attempt 2 reload sees tenants A+B, and incoming event adds tenant C.
  - [x] Assert final saved `TenantIndexReadModel.Tenants` contains A, B, and C, with no dropped or overwritten entries.
  - [x] Assert each save uses `ConcurrencyMode.FirstWrite` and the ETag from that attempt's fresh reload.

- [x] Task 3: Prove retry exhaustion is observable and support-safe (AC: #2, #4)
  - [x] Script `TenantProjectionWritePolicy.MaxAttempts` failed guarded saves for `projection:tenant-index:singleton`.
  - [x] Assert the thrown `InvalidOperationException` identifies `tenant index` and the retry limit.
  - [x] Assert conflict logs use EventId `100101` and retry-exhausted logs use EventId `100102`.
  - [x] Assert structured fields include `StateStoreName`, `StateKeyCategory`, `AttemptCount`, `MaxAttempts`, `OperationContext`, `Reason`, `CorrelationId`, `MessageIds`, and `EventTypes`.
  - [x] Assert diagnostic output excludes tenant names, event payload bodies, payload user IDs, configuration values, tokens, and secrets.
  - [x] Preserve bounded log behavior for large `MessageIds` and distinct `EventTypes` fields.

- [x] Task 4: Prove replay and idempotency semantics for tenant-index state (AC: #2, #5)
  - [x] Reprocess a batch after a successful index save and prove tenant and membership entries are not duplicated.
  - [x] Preserve `TenantIndexReadModel.Apply(TenantCreated)` behavior that keeps an existing tenant entry instead of overwriting it with stale replay data.
  - [x] Preserve `UserAddedToTenant` dictionary overwrite semantics for role membership idempotency.
  - [x] Preserve cleanup semantics where `UserRemovedFromTenant` removes the user key when the final tenant membership is removed.

- [x] Task 5: Keep story boundaries tight (AC: #6)
  - [x] Do not add REST query endpoints, cursor signing, pagination changes, query-side authorization, audit retention, UI freshness states, or global-admin query behavior.
  - [x] Do not change `TenantProjection`, `TenantAuditProjection`, or `TenantReadModel` semantics unless a tenant-index regression test exposes a required shared helper fix.
  - [x] Do not rename the current `statestore` DAPR component or projection keys inside this story.
  - [x] If backoff/jitter, DAPR transport exception classification, or out-of-order membership retention is discovered, record it as deferred work unless required to satisfy the ACs.

- [x] Task 6: Run focused and regression validation (AC: #1-#6)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~ProjectionWriteConformanceTests|FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~ProjectionDispatcherTests"`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/`.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore`.
  - [x] If VSTest socket setup is blocked in the sandbox, run the built xUnit v3 test executable directly as established by Story 5.1 and record the runner limitation separately from product failures.

## Dev Notes

### Current Implementation Posture

This story is part of the refreshed Epic 5 sequence created by the 2026-05-31 correction. The tenant-index read model and projection shell already exist from the archived Epic 5 work, and the durable write policy already exists from Epic 10. Treat them as current implementation to validate and preserve, not as permission to recreate the index.

The canonical shared tenant-index write path is:

- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs`
- `src/Hexalith.Tenants/Projections/ITenantProjectionStateStore.cs`
- `src/Hexalith.Tenants/Projections/DaprTenantProjectionStateStore.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantIndexEntry.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantIndexProjection.cs`

Do not introduce another shared-index store, direct Redis dependency, database table, repository abstraction, or public DAPR helper. `TenantProjectionWritePolicy` is intentionally internal and already load-bearing for tenant detail, singleton index, and audit projection writes. [Source: _bmad-output/implementation-artifacts/epic-10-retro-2026-05-19.md#Scope Completed]

### Current State Of Files To Touch

`TenantProjectionHandler.ProjectAsync` currently:

- Validates the projection request and cancellation token.
- Builds incoming audit state before writes so audit invariant failures abort before tenant-detail, audit, or index state commits.
- Saves per-tenant detail state at `projection:tenants:{aggregateId}` first.
- Saves audit state at `audit:{aggregateId}` second.
- Saves the singleton tenant index at `projection:tenant-index:singleton` third through `TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync`.
- Returns the tenant-detail `TenantReadModel`, not the singleton index.

This ordering means tenant detail and audit may have committed before tenant-index retry exhaustion. Do not claim cross-key atomicity. The recovery contract is observable failure plus idempotent replay, not rollback across keys. [Source: _bmad-output/implementation-artifacts/epic-10-retro-2026-05-19.md#Key takeaways]

`TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync` currently:

- Reads state and ETag with `GetStateAndETagAsync`.
- Uses loaded state or a fresh default model.
- Applies each incoming non-null `ProjectionEventDto`.
- Saves with `TrySaveStateAsync` using `ConcurrencyMode.FirstWrite`.
- Retries up to `MaxAttempts = 3`.
- Logs EventId `100101` for conflicts and `100102` for retry exhaustion, then throws `InvalidOperationException`.
- Bounds logged message IDs and event types.

Preserve this guarded-write behavior. Do not replace it with `SaveStateAsync`, last-writer-wins, unbounded retry, or a public abstraction.

`TenantIndexReadModel` currently:

- Stores `Tenants` as `Dictionary<string, TenantIndexEntry>`.
- Stores `UserTenants` as `Dictionary<string, Dictionary<string, TenantRole>>`.
- Handles exactly seven event types: `TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`, `UserAddedToTenant`, `UserRemovedFromTenant`, and `UserRoleChanged`.
- Preserves existing tenant state on duplicate `TenantCreated`, which is required for replay over a persisted singleton index.
- Ignores user membership events for tenants that are not yet in the index. This is a known deferred risk for out-of-order event delivery; do not broaden this story unless an AC fails because of it.

`TenantIndexProjection` currently derives from `EventStoreProjection<TenantIndexReadModel>`. Its domain name is convention-derived as `tenant-index`; do not force it to `tenants`, because that collides with `TenantProjection` assembly scanning. [Source: _bmad-output/implementation-artifacts/archive/legacy-story-slugs-20260601/5-2-cross-tenant-index-projection.md#Projection Class - Shell with Domain Name Verification]

### Architecture Guardrails

- EventStore events remain the source of truth; query read models are projections and must not become authoritative write state. [Source: _bmad-output/planning-artifacts/architecture.md#Architectural Boundaries]
- Shared cross-tenant indexes must use ETag/optimistic concurrency or verified `CachingProjectionActor` fan-in behavior to avoid silent write loss. The current selected path is ETag/optimistic concurrency through `TenantProjectionWritePolicy`; do not reopen `CachingProjectionActor` fan-in unless tests prove a regression. [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture]
- Epic 5 query and projection safety work belongs in `Contracts/Queries`, `Server/Projections`, `src/Hexalith.Tenants/Controllers`, `src/Hexalith.Tenants/Queries`, cursor/pagination utilities, projection write policy, and projection recovery tests. This story is limited to singleton tenant-index write safety. [Source: _bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping]
- Projection state keys must follow existing projection policy types and tests. Do not invent ad hoc Redis/database key names inside domain code. [Source: _bmad-output/planning-artifacts/architecture.md#Code Naming Conventions]
- Projection Apply methods trust events and update read models deterministically. Business validation belongs in aggregate `Handle` methods, not projections. [Source: _bmad-output/project-context.md#Apply Methods (State)]
- Tenants server query projections are separate from Client/local consumer projections. Do not reuse `InMemoryTenantProjectionStore` or consumer package assumptions for server-owned query state. [Source: _bmad-output/story-automator/learnings.md]

### Story Boundaries

In scope:

- Shared tenant index persistence for `projection:tenant-index:singleton`.
- ETag/optimistic-concurrency verification.
- Reload-and-reapply behavior under conflicts.
- Retry-exhaustion behavior and diagnostics for the tenant-index key.
- Replay/idempotency proof for `TenantIndexReadModel`.
- Focused tests that drive production code through `TenantProjectionHandler.ProjectAsync`.

Out of scope:

- Per-tenant detail write safety except where existing tests must stay green.
- Audit merge/write-safety except where existing tests must stay green.
- Query-side authorization and isolation.
- Cursor signing, cursor scope binding, and pagination.
- REST endpoint response shape changes.
- Phase 2 Admin UI freshness or command-lifecycle UI.
- Public package API expansion.

### Testing Requirements

Use xUnit v3 and Shouldly. Tests should live under `tests/Hexalith.Tenants.Server.Tests/Projections/` and should follow existing conventions: plural test classes and `snake_case_with_PascalCase_for_type_names` when adding new methods near existing files.

Preferred existing test surfaces:

- `ProjectionWriteConformanceTests` for race/retry/recovery proof across production `TenantProjectionHandler.ProjectAsync`.
- `ProjectionWriteConformanceFixture` for scripted per-key state-store reads/saves and captured structured logs.
- `TenantIndexReadModelTests` for model-level idempotency and lifecycle behavior.
- `TenantIndexProjectionTests` for EventStore projection shell/domain behavior.
- `TenantProjectionHandlerTests` and `ProjectionDispatcherTests` for focused handler/dispatch regression checks.

Do not create tests that call private `ApplyIndexEvent` by reflection or duplicate production merge logic in test-only helpers. The conformance fixture exists to observe the production path through `ProjectAsync`.

### Previous Story Intelligence

Story 5.1 is done and established the expected implementation posture for refreshed Epic 5:

- Existing Epic 10 projection write-safety machinery should be reused directly.
- `TenantProjectionHandler.ProjectAsync`, `TenantProjectionWritePolicy`, and `DaprTenantProjectionStateStore` are the canonical persistence surfaces.
- VSTest may build successfully and then fail in this sandbox with a socket permission error; direct xUnit v3 executable runs are the established fallback.
- No production code was required for Story 5.1 after test hardening; this story may similarly be evidence/test-hardening work if current implementation already satisfies the ACs.

Archived legacy Story 5.2 already created `TenantIndexReadModel`, `TenantIndexEntry`, and `TenantIndexProjection`, and verified the `tenant-index` domain. Do not recreate those types. The refreshed Story 5.2 is narrower: prove the shared singleton index cannot silently lose writes under concurrent projection delivery.

Epic 10 already introduced and validated the core write-safety machinery:

- `TenantProjectionWritePolicy`
- `ITenantProjectionStateStore`
- `DaprTenantProjectionStateStore`
- 3-attempt reload/reapply retry behavior
- bounded structured diagnostics
- conformance tests that drive production behavior

Carry forward the Epic 10 lesson: conformance fixtures must drive production behavior, not parallel it.

### Git Intelligence

Recent history before story creation:

- `9345bf0 feat(story-5.1): Persist Per-Tenant Detail Projections Without Silent Write Loss`
- `f67eecd docs(retro): finalize Epic 4 retrospective`
- `7f2a89e feat(story-4.6): Provide Idempotent Consumer Guidance and Sample Service`
- `e396f0a feat(story-4.5): React to Tenant Access Lifecycle and Configuration Changes`
- `15e3d69 feat(story-4.4): Register Tenant Event Handlers in Under Twenty Lines`

The immediate predecessor commit is directly relevant: Story 5.1 hardened the same projection write surface for tenant detail. Reuse its test style and runner fallback instead of inventing a new validation pattern.

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
- Server projection tests are in `tests/Hexalith.Tenants.Server.Tests/Projections/`.

Detected variance: planning context mentions EventStore convention state store name `tenants-eventstore`, while current AppHost, health checks, handlers, tests, and DAPR YAML consistently use the DAPR component name `statestore`. Do not rename this inside Story 5.2; topology/configuration renames require separate AppHost and test updates.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.2] - Story statement and acceptance criteria.
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 5] - Projection write safety must precede endpoint delivery.
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Proposal A: Resequence and Split Epic 5] - Resequenced Epic 5 story order.
- [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture] - Tenant projections, EventStore source-of-truth, and DAPR projection state boundary.
- [Source: _bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping] - Epic 5 file locations and projection recovery test scope.
- [Source: _bmad-output/project-context.md#Projections] - Projection types, `CachingProjectionActor`, and conditional fan-in guidance.
- [Source: _bmad-output/project-context.md#Testing Rules] - xUnit v3, Shouldly, and test-tier rules.
- [Source: _bmad-output/implementation-artifacts/5-1-persist-per-tenant-detail-projections-without-silent-write-loss.md#Previous Work Intelligence] - Previous story implementation posture and validation fallback.
- [Source: _bmad-output/implementation-artifacts/archive/legacy-story-slugs-20260601/5-2-cross-tenant-index-projection.md] - Existing tenant-index model/projection creation and domain-name findings.
- [Source: _bmad-output/implementation-artifacts/epic-10-retro-2026-05-19.md#Scope Completed] - Existing write policy, state-store adapter, retry behavior, and conformance fixture.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~ProjectionWriteConformanceTests|FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~ProjectionDispatcherTests"` failed before test execution in sandbox with MSBuild named-pipe/socket permission error.
- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Server.Tests/ --no-restore -m:1 /nr:false` compiled successfully, then VSTest aborted during socket channel setup with `System.Net.Sockets.SocketException (13): Permission denied`.
- 2026-06-01: Direct xUnit v3 focused run passed: 69 tests, 0 failed.
- 2026-06-01: Direct xUnit v3 server suite passed: 581 tests, 0 failed.
- 2026-06-01: `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nr:false` passed with 0 warnings and 0 errors.
- 2026-06-01: Direct xUnit v3 Release server suite passed: 581 tests, 0 failed.
- 2026-06-01: Direct xUnit v3 non-integration regression passed: Contracts 101, Client 92, Testing 99, Sample 31; all 0 failed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story created from refreshed Epic 5 scope and validated against the create-story checklist.
- Confirmed the shared tenant-index persistence path remains `TenantProjectionHandler.ProjectAsync` -> `TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync` -> `DaprTenantProjectionStateStore.GetStateAndETagAsync` / guarded `TrySaveStateAsync`.
- Hardened tenant-index conformance tests to assert `statestore`, `tenant index`, `TenantProjectionHandler.ProjectAsync`, ETag reuse per reload attempt, `ConcurrencyMode.FirstWrite`, no plain `SaveStateAsync`, safe structured diagnostics, retry exhaustion, and bounded log fields.
- Added tenant-index replay/idempotency coverage for repeated `UserAddedToTenant` membership writes without duplicate entries.
- No production projection semantics, query endpoints, cursor/pagination behavior, DAPR component names, or UI/query authorization behavior changed.

### File List

- `_bmad-output/implementation-artifacts/5-2-persist-the-shared-tenant-index-projection-without-silent-write-loss.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/story-automator/orchestration-5-20260601-061130.md`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionDispatcherTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantIndexReadModelTests.cs`
- `tests/test-summary.md`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after automatic review fixes.

Findings:

- Medium: Story File List omitted actual changed files (`tests/test-summary.md` and `_bmad-output/story-automator/orchestration-5-20260601-061130.md`). Fixed by reconciling the File List.
- Low: Story review record and status sync were missing before this review pass. Fixed by adding this review section and moving the story to `done`.

Acceptance Criteria Review:

- AC1/AC3: Verified `TenantProjectionHandler.ProjectAsync` saves `projection:tenant-index:singleton` through `TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync`, which reloads state and ETag, applies the incoming events, and guarded-saves with `ConcurrencyMode.FirstWrite`.
- AC2: Verified `ProjectionWriteConformanceTests` drives the production handler path and covers tenant-index conflict recovery, retry exhaustion, structured diagnostics, and bounded log fields.
- AC4: Verified retry exhaustion throws `InvalidOperationException` and logs support-safe structured fields without tenant names, payload user IDs, configuration values, raw payloads, tokens, or secrets.
- AC5: Verified replay/idempotency coverage for duplicate `TenantCreated`, repeated `UserAddedToTenant`, role overwrite behavior, and membership cleanup.
- AC6: Verified implementation is test hardening only; tenant detail, audit, query endpoints, cursor behavior, DAPR component names, and UI/query authorization semantics remain unchanged.

Validation:

- Dapr state documentation fallback checked: ETag-based optimistic concurrency with `first-write` prevents last-writer-wins overwrite behavior when callers provide the loaded ETag.
- Focused and server-suite validation evidence is recorded in Debug Log References and `tests/test-summary.md`; reviewer reran focused direct xUnit validation after review fixes.

### Change Log

- 2026-06-01: Added story-specific tenant-index write-safety assertions and direct xUnit validation evidence; moved story to review.
- 2026-06-01: Senior Developer Review approved implementation, reconciled review bookkeeping, and moved story to done.
