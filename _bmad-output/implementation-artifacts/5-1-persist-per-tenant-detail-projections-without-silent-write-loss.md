---
baseline_commit: f67eecd6c9ff43e6ab4f07974a8dc726e486217e
---

# Story 5.1: Persist Per-Tenant Detail Projections Without Silent Write Loss

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform operator,
I want per-tenant detail projections to handle concurrent writes safely,
so that tenant detail and user query results do not silently lose tenant events.

## Acceptance Criteria

1. **Given** multiple tenant events update the per-tenant detail projection close together
   **When** projection state is persisted
   **Then** the write path uses optimistic concurrency, ETag-aware writes, or verified `CachingProjectionActor` fan-in behavior
   **And** no successful event update is silently overwritten.

2. **Given** per-tenant detail projection write conformance tests run
   **When** tenant detail projection writes race
   **Then** tests prove no silent data loss, deterministic recovery behavior, and enough diagnostics for replay or repair.

## Tasks / Subtasks

- [x] Task 1: Confirm the existing per-tenant projection persistence path is the single implementation surface (AC: #1)
  - [x] Verify `TenantProjectionHandler.ProjectAsync` writes `projection:tenants:{aggregateId}` only through `TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync`.
  - [x] Verify `DaprTenantProjectionStateStore` uses `GetStateAndETagAsync` plus `TrySaveStateAsync` for guarded writes and does not use plain `SaveStateAsync` for tenant detail.
  - [x] Preserve the current `statestore` DAPR component name unless a separate architecture/configuration story changes the AppHost and tests together.

- [x] Task 2: Validate optimistic-concurrency behavior for tenant detail writes (AC: #1, #2)
  - [x] Prove the first write attempt reads the current `TenantReadModel` and ETag, applies the incoming projection events, and saves with `ConcurrencyMode.FirstWrite`.
  - [x] Prove a failed guarded save reloads fresh persisted state, reapplies the same incoming events, and retries without losing externally persisted members or configuration.
  - [x] Prove retry exhaustion throws an observable failure and does not continue to audit or index writes after the tenant-detail key has failed.

- [x] Task 3: Preserve deterministic projection semantics while adding or adjusting tests (AC: #1, #2)
  - [x] Keep `TenantReadModel.Apply(...)` methods deterministic and idempotent enough for full-history replay plus reload-and-reapply behavior.
  - [x] Do not introduce authorization, REST endpoint branching, cursor pagination, or audit/index-specific behavior into `TenantReadModel` or `TenantProjection`.
  - [x] Keep event deserialization behavior consistent with existing `TenantProjectionHandler.ApplyEvent` behavior unless a failing test proves a defect.

- [x] Task 4: Ensure diagnostics are support-safe and useful (AC: #2)
  - [x] Verify conflict and retry-exhausted logs include state store name, key category, attempt count, max attempts, operation context, reason, correlation ID, bounded message IDs, and bounded event types.
  - [x] Verify diagnostics do not log event payload bodies, tenant names, configuration values, user IDs from payloads, or other sensitive business content.
  - [x] Preserve EventIds `100101` for optimistic concurrency conflicts and `100102` for retry exhaustion unless an existing telemetry contract is deliberately migrated.

- [x] Task 5: Run focused and regression validation (AC: #1, #2)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~ProjectionWriteConformanceTests|FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~ProjectionDispatcherTests"`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/`.
  - [x] If the sandbox blocks VSTest listener/socket setup, run the project through the repo's established direct xUnit fallback and record the runner limitation separately from product failures.

## Dev Notes

### Current Implementation Posture

This story is part of the refreshed Epic 5 sequence created by the 2026-05-31 sprint change proposal. The source tree already contains projection and write-safety code from earlier Epic 5/Epic 10 work. Treat that as existing implementation to audit and preserve, not as permission to create a parallel path.

The canonical per-tenant detail write path is:

- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs`
- `src/Hexalith.Tenants/Projections/ITenantProjectionStateStore.cs`
- `src/Hexalith.Tenants/Projections/DaprTenantProjectionStateStore.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs`
- `src/Hexalith.Tenants.Server/Projections/TenantProjection.cs`

Do not create another projection state store abstraction, Redis adapter, repository, database table, or `CachingProjectionActor` fan-in replacement for this story. The existing internal `TenantProjectionWritePolicy` is intentionally load-bearing for tenant detail, tenant index, and audit projection writes. [Source: _bmad-output/implementation-artifacts/epic-10-retro-2026-05-19.md#Scope Completed]

### Current State Of Files To Touch

`TenantProjectionHandler.ProjectAsync` currently:

- Validates `ProjectionRequest`, aggregate ID, and cancellation.
- Builds audit state first so missing `MessageId`/`UserId` audit invariants abort before any tenant-detail write commits.
- Saves per-tenant detail state at key `projection:tenants:{request.AggregateId}` using category `tenant read-model`.
- Saves audit and singleton index after tenant detail succeeds.
- Returns a `ProjectionResponse` containing the tenant detail `TenantReadModel`.

Preserve that ordering unless changing it is necessary to satisfy a failing AC test. In particular, if tenant-detail retry exhaustion occurs, audit and index writes must not occur after that terminal failure.

`TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync` currently:

- Reads state and ETag with `GetStateAndETagAsync`.
- Uses the loaded state or a fresh default model.
- Applies each incoming non-null `ProjectionEventDto`.
- Saves through `TrySaveStateAsync` with `ConcurrencyMode.FirstWrite`.
- Retries up to `MaxAttempts = 3`.
- Logs Warning on conflicts and Error on retry exhaustion, then throws `InvalidOperationException`.

This is the behavior to preserve or tighten. Do not replace it with last-writer-wins `SaveStateAsync`.

`TenantReadModel` currently applies `TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`, `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, and `TenantConfigurationRemoved`. The dictionaries are mutable and are initialized defensively for JSON-deserialized models. Do not add aggregate-only fields such as `HasMembershipHistory`; read models are query views, not invariant state.

### Architecture Guardrails

- EventStore events remain the source of truth; query read models are projections and must not become authoritative write state. [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture]
- Epic 5 query and projection work belongs in `Contracts/Queries`, `Server/Projections`, `src/Hexalith.Tenants/Controllers`, `src/Hexalith.Tenants/Queries`, cursor/pagination utilities, projection write policy, and projection recovery tests. This story is limited to per-tenant detail projection write safety. [Source: _bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping]
- Projection state keys must follow existing projection policy types and tests; do not invent ad hoc Redis/database key names inside domain code. [Source: _bmad-output/planning-artifacts/architecture.md#Code Naming Conventions]
- Projection Apply methods trust events and update read models deterministically. Business validation belongs in aggregate `Handle` methods, not projections. [Source: _bmad-output/planning-artifacts/architecture.md#State Management Patterns]
- Keep consumer-local projections and Tenants server query projections separate. Do not reuse `InMemoryTenantProjectionStore` or Client package projection assumptions for server-owned query state. [Source: _bmad-output/implementation-artifacts/epic-4-retro-2026-06-01.md#Preparation For Epic 5]

### Story Boundaries

In scope:

- Per-tenant detail projection persistence for `projection:tenants:{tenantId}`.
- ETag/optimistic-concurrency verification.
- Retry exhaustion behavior for tenant-detail writes.
- Support-safe conflict/retry diagnostics.
- Focused unit/conformance tests that drive production code through `TenantProjectionHandler.ProjectAsync`.

Out of scope:

- Shared tenant index write safety except where existing tests must be kept passing.
- Audit projection merge/write-safety except where existing tests must be kept passing.
- Query-side authorization and isolation.
- Cursor signing, cursor scope binding, and pagination.
- REST endpoint response shape changes.
- Phase 2 Admin UI freshness or command-lifecycle UI.
- Public package API expansion.

### Testing Requirements

Use xUnit v3 and Shouldly. Tests should live under `tests/Hexalith.Tenants.Server.Tests/Projections/` and should mirror existing naming conventions: test classes plural, method names in snake_case_with_PascalCase_for_type_names where adding new tests near existing files permits it.

Preferred existing test surfaces:

- `ProjectionWriteConformanceTests` for race/retry/recovery proof across production `TenantProjectionHandler.ProjectAsync`.
- `ProjectionWriteConformanceFixture` for scripted per-key state-store reads/saves and captured structured logs.
- `TenantProjectionHandlerTests` for focused handler behavior.
- `ProjectionDispatcherTests` for DAPR call shape and guarded-save regression checks.

Do not create tests that call private event-apply helpers by reflection or duplicate production merge logic in the test. The conformance fixture exists specifically to avoid testing a parallel implementation.

### Previous Work Intelligence

Earlier archived Story 5.1 created `TenantReadModel`, `TenantProjection`, `GlobalAdministratorReadModel`, and `GlobalAdministratorProjection`; that old story is archived under `_bmad-output/implementation-artifacts/archive/legacy-story-slugs-20260601/`. The refreshed Story 5.1 is not asking to recreate those types. It narrows the current story to durable per-tenant detail writes without silent loss.

Epic 10 already introduced the core write-safety machinery:

- `TenantProjectionWritePolicy`
- `ITenantProjectionStateStore`
- `DaprTenantProjectionStateStore`
- 3-attempt reload/reapply retry behavior
- bounded structured diagnostics
- conformance tests that drive production behavior

Use those artifacts directly. If they already satisfy the ACs, the implementation work should be limited to evidence, test naming/status cleanup, or small corrective patches discovered during validation.

Recent git history before this story shows Epic 4 completion and documentation/consumer integration work:

- `docs(retro): finalize Epic 4 retrospective`
- `feat(story-4.6): Provide Idempotent Consumer Guidance and Sample Service`
- `feat(story-4.5): React to Tenant Access Lifecycle and Configuration Changes`
- `feat(story-4.4): Build Local Consumer Projection from Tenant Events`
- `feat(story-4.3): Register Tenant Event Handlers in Under Twenty Lines`

Carry forward the Epic 4 lesson: Client/local projections are not the server query model.

### Latest Technical Specifics

No package or framework upgrade is part of this story. Use the versions pinned by the repository:

- .NET SDK `10.0.300` and `Hexalith.Tenants.slnx`.
- Dapr SDK `1.17.9`.
- xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `6.0.0-rc.1`.
- Central package management only; do not add inline `Version=` metadata.

External web research was not needed because this story uses existing pinned APIs and does not introduce a new package or upgrade path.

### Project Structure Notes

The existing source layout matches the architecture boundary:

- Runtime projection handler and DAPR state-store adapter are in `src/Hexalith.Tenants/Projections/`.
- EventStore-discovered projection/read-model types are in `src/Hexalith.Tenants.Server/Projections/`.
- Server projection tests are in `tests/Hexalith.Tenants.Server.Tests/Projections/`.

Detected variance: planning context mentions the EventStore convention state store name `tenants-eventstore`, while current AppHost, health checks, handlers, tests, and DAPR YAML consistently use the DAPR component name `statestore`. Do not rename this inside Story 5.1; a rename would be a separate topology/configuration change touching AppHost components and multiple test suites.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 5.1] - Story statement and acceptance criteria.
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 5] - Projection write safety must precede endpoint delivery.
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Proposal A: Resequence and Split Epic 5] - Resequenced Epic 5 story order.
- [Source: _bmad-output/planning-artifacts/architecture.md#Data Architecture] - Tenant projections, EventStore source-of-truth, and DAPR projection state boundary.
- [Source: _bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping] - Epic 5 file locations and projection recovery test scope.
- [Source: _bmad-output/project-context.md#Projections] - Projection types, `CachingProjectionActor`, and conditional fan-in guidance.
- [Source: _bmad-output/project-context.md#Testing Rules] - xUnit v3, Shouldly, and test-tier rules.
- [Source: _bmad-output/implementation-artifacts/epic-4-retro-2026-06-01.md#Preparation For Epic 5] - Client-local versus server-owned projection boundary.
- [Source: _bmad-output/implementation-artifacts/epic-10-retro-2026-05-19.md#Scope Completed] - Existing write policy, state-store adapter, retry behavior, and conformance fixture.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-01: `dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~ProjectionWriteConformanceTests|FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~ProjectionDispatcherTests"` initially failed before test execution with MSBuild named-pipe `System.Net.Sockets.SocketException (13): Permission denied`.
- 2026-06-01: `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 -nr:false /p:UseSharedCompilation=false /p:NuGetAudit=false` first exposed test-code compile errors from the new assertions; after fixing, it passed with 0 warnings and 0 errors.
- 2026-06-01: `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Projections.ProjectionWriteConformanceTests -class Hexalith.Tenants.Server.Tests.Projections.TenantProjectionHandlerTests -class Hexalith.Tenants.Server.Tests.Projections.ProjectionDispatcherTests` passed: Total 46, Errors 0, Failed 0, Skipped 0.
- 2026-06-01: `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~ProjectionWriteConformanceTests|FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~ProjectionDispatcherTests" -m:1 -nr:false /p:NuGetAudit=false` built successfully, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` while opening its communication socket.
- 2026-06-01: `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/ -m:1 -nr:false /p:NuGetAudit=false` built successfully, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` while opening its communication socket.
- 2026-06-01: `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none` passed: Total 578, Errors 0, Failed 0, Skipped 0.
- 2026-06-01: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 -nr:false /p:UseSharedCompilation=false /p:NuGetAudit=false` passed with 0 warnings and 0 errors.
- 2026-06-01 review: `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 -nr:false /p:UseSharedCompilation=false /p:NuGetAudit=false` passed with 0 warnings and 0 errors.
- 2026-06-01 review: `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Projections.ProjectionWriteConformanceTests -class Hexalith.Tenants.Server.Tests.Projections.TenantProjectionHandlerTests -class Hexalith.Tenants.Server.Tests.Projections.ProjectionDispatcherTests` passed: Total 47, Errors 0, Failed 0, Skipped 0.
- 2026-06-01 review: `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/ --filter "FullyQualifiedName~ProjectionWriteConformanceTests|FullyQualifiedName~TenantProjectionHandlerTests|FullyQualifiedName~ProjectionDispatcherTests" -m:1 -nr:false /p:NuGetAudit=false` built successfully, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` while opening its communication socket.
- 2026-06-01 review: `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none` passed: Total 579, Errors 0, Failed 0, Skipped 0.
- 2026-06-01 review: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 -nr:false /p:UseSharedCompilation=false /p:NuGetAudit=false` passed with 0 warnings and 0 errors.

### Completion Notes List

- Story context created from refreshed Epic 5 scope.
- Existing projection write-safety implementation detected and called out to prevent duplicate implementation.
- Checklist validation applied during story creation.
- Confirmed the existing production implementation remains the single tenant-detail write path: `TenantProjectionHandler.ProjectAsync` writes `projection:tenants:{aggregateId}` through `TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync`, and the DAPR adapter uses ETag reads plus guarded `TrySaveStateAsync`.
- Tightened dispatcher and conformance coverage for tenant-detail writes: current `TenantReadModel` plus ETag are read, saves use `ConcurrencyMode.FirstWrite`, conflicts reload and reapply incoming events, externally persisted members/configuration survive, and plain tenant-detail `SaveStateAsync` is not used.
- Tightened retry-exhaustion diagnostics coverage for tenant-detail writes: conflict and retry-exhausted logs preserve EventIds `100101`/`100102`, include support fields, bound message IDs/event types, and exclude tenant names, payload user IDs, and configuration values.
- No production code changes were required; the existing write policy and read-model semantics satisfied the story after test hardening and validation.
- Senior review added explicit overflow coverage proving diagnostic `MessageIds` and `EventTypes` fields are bounded when tenant-detail retry exhaustion logs large batches.

### File List

- `_bmad-output/implementation-artifacts/5-1-persist-per-tenant-detail-projections-without-silent-write-loss.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionDispatcherTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs`

### Change Log

- 2026-06-01: Added baseline commit and moved story to in progress.
- 2026-06-01: Hardened tenant-detail projection write conformance tests for ETag guarded writes, retry reload/reapply behavior, no plain saves, and safe diagnostics.
- 2026-06-01: Ran focused and full Server validations via direct xUnit fallback after VSTest socket startup was blocked; Release solution build passed.
- 2026-06-01: Marked all tasks complete and moved story to review.
- 2026-06-01: Senior review fixed missing bounded-diagnostics overflow proof and moved story to done.

## Senior Developer Review (AI)

Reviewer: Codex on 2026-06-01

### Outcome

Approved after auto-fix. Status moved to `done`; sprint tracking synced.

### Findings Fixed

- [MEDIUM] Task 4 claimed bounded diagnostic message IDs/event types were verified, but the test coverage only asserted small ordinary batches. Added `TenantDetail_RetryExhaustion_BoundsLoggedMessageIdsAndEventTypesAsync` to force overflow and prove `MessageIds` and `EventTypes` are sampled and suffixed instead of logging the entire batch.

### Notes

- Non-source BMAD/story-automator artifacts and archived legacy story renames were present in the working tree during review. They were excluded from application code review per the workflow guardrail; story-source documentation was updated for the files touched by this review.

### Validation Checklist

- [x] Story file loaded from `_bmad-output/implementation-artifacts/5-1-persist-per-tenant-detail-projections-without-silent-write-loss.md`
- [x] Story Status verified as reviewable (`review`)
- [x] Epic and Story IDs resolved (`5.1`)
- [x] Story Context located or warning recorded
- [x] Epic Tech Spec located or warning recorded
- [x] Architecture/standards docs loaded
- [x] Tech stack detected and documented
- [x] MCP doc search/web fallback not needed; story uses pinned local APIs and no package/framework upgrade
- [x] Acceptance Criteria cross-checked against implementation
- [x] File List reviewed and validated for completeness
- [x] Tests identified and mapped to ACs; bounded-diagnostics gap fixed
- [x] Code quality review performed on changed files
- [x] Security review performed on changed files and diagnostics
- [x] Outcome decided: Approve
- [x] Review notes appended under `Senior Developer Review (AI)`
- [x] Change Log updated with review entry
- [x] Status updated to `done`
- [x] Sprint status synced
- [x] Story saved successfully
