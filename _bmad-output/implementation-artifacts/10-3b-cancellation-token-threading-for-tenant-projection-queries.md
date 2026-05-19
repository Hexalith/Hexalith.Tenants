# Story 10.3B: Cancellation Token Threading for Tenant Projection Queries

Status: review

Completion note: Story context updated after Story 10.3A published the EventStore cancellation handoff at submodule commit `bcccd504`. Tenants implementation can now consume the approved EventStore APIs instead of re-deciding the contract shape.

## Story

As a platform operator,
I want long-running tenant projection queries and projection writes to observe request cancellation,
so that abandoned requests do not keep consuming compute or block projection processing unnecessarily.

## Acceptance Criteria

1. Given a request to a paginated tenant query is cancelled by the client, when cancellation reaches the query endpoint and EventStore exposes a cancellation-aware projection dispatch path, then cancellation is propagated through the tenant projection query path instead of being dropped.
2. Given `HandleGetTenantAuditAsync` performs an audit read, when the caller cancels the request and the token reaches the actor execution path, then DAPR state reads and any in-memory filtering/pagination stop through cancellation rather than returning partial successful data.
3. Given `TenantProjectionHandler.ProjectAsync` performs projection write work, when the hosting pipeline supplies cancellation through the EventStore projection API named by Story 10.3A, then tenant detail, tenant index, and audit projection reads/writes observe the provided token where the DAPR client API supports it.
4. Given the current EventStore submodule does not expose cancellation-aware query or projection handler signatures, when implementation starts, then the developer verifies Story 10.3A is completed and records the exact EventStore API signatures plus submodule commit before changing Tenants code.
5. Given cancellation is observed before query state access, during query execution, or before projection persistence, when focused tests run, then tests prove no successful query/projection result is reported after cancellation and no read-model state is corrupted.
6. Given cancellation is added to tenant query/projection code, when non-cancelled callers execute the same query and projection flows, then existing authorization, cursor, pagination, audit filtering, ETag cache, and projection write-safety behavior remains unchanged.
7. Given cancellation occurs, when logs, metrics, or traces are emitted, then safe structured context distinguishes cancellation from forbidden, not-found, invalid-cursor, serialization, actor invocation, ETag conflict, and retry-exhaustion failures without logging payload bodies, tenant names, configuration values, cursor payloads, or user-controllable display names.
8. Given Story 10.3A has not yet recorded the approved EventStore cancellation-aware signatures and submodule commit, when a developer begins Story 10.3B, then implementation stops before Tenants code changes and does not introduce a Tenants-local EventStore bypass, overload shim, DTO change, route change, cursor/auth/query visibility change, audit schema change, state key change, or package dependency change.
9. Given a cancelled request also has an existing cheap validation or authorization failure with defined precedence, when tenant query handling begins, then cancellation checkpoints are placed after those existing guards but before state I/O or expensive processing so malformed-user, forbidden, cursor, and visibility behavior does not drift.
10. Given cancellation is observed during multi-key projection writes, when any state write has already completed before the cancellation point, then the story does not claim cross-key atomic rollback; replay/idempotency from Stories 10.1 and 10.2 remains the recovery mechanism and tests only assert no additional writes occur after the observed cancellation boundary.

## Tasks / Subtasks

- [x] Verify the Story 10.3A EventStore prerequisite before implementation. (AC: 1, 3, 4)
  - [x] Confirm the `Hexalith.EventStore` submodule pointer is at commit `bcccd504` or a descendant containing the approved cancellation-aware projection API.
  - [x] Record the exact EventStore signatures consumed by this story in the Dev Agent Record, including the query actor/dispatch path, projection delivery HTTP boundary, and unsupported actor write-boundary limitations.
  - [x] If the submodule pointer regresses before `bcccd504` or any handoff API named below is absent, stop implementation and return this story for prerequisite repair instead of inventing a Tenants-only bypass.
  - [x] Do not wrap synchronous EventStore APIs, shadow EventStore overloads locally, invoke DAPR/state clients outside the approved projection path, or add a Tenants-only cancellation adapter to bypass the 10.3A contract decision.
  - [x] Do not initialize or update nested submodules recursively while checking the dependency.
- [x] Thread cancellation through tenant projection query execution once EventStore exposes the token. (AC: 1, 2, 5, 6)
  - [x] Update `TenantsProjectionActor.ExecuteQueryAsync(...)` to accept and pass the token according to the EventStore API shape from Story 10.3A.
  - [x] Update `HandleGetTenantAsync`, `HandleListTenantsAsync`, `HandleGetTenantUsersAsync`, `HandleGetUserTenantsAsync`, and `HandleGetTenantAuditAsync` to accept the token and pass it to DAPR `GetStateAsync` calls.
  - [x] Check the token before expensive in-memory filtering, sorting, pagination, and audit result serialization where the operation may process a large read model.
  - [x] Preserve malformed-user precedence: role-sensitive queries with missing or whitespace `UserId` must still return forbidden before cursor validation or state reads.
  - [x] Place pre-state cancellation checkpoints after existing cheap request-shape, authorization, and identity guards whose precedence is already asserted by Stories 9.3 and 9.4; do not let a pre-cancelled token mask malformed-user or forbidden outcomes that currently return before state access.
  - [x] Preserve invalid cursor behavior for non-cancelled requests.
- [x] Thread cancellation through tenant projection write handling once EventStore exposes the token. (AC: 3, 5, 6)
  - [x] Update `TenantProjectionHandler.ProjectAsync(...)` to accept the token only through the EventStore-approved projection contract.
  - [x] Pass the token to DAPR state reads and saves for `projection:tenants:{tenantId}`, `projection:tenant-index:singleton`, and `audit:{tenantId}` where those paths are present after Stories 10.1 and 10.2.
  - [x] Preserve guarded ETag retry semantics from Stories 10.1 and 10.2; cancellation must abort retry loops, not convert cancellation into a conflict, retry exhaustion, or successful projection response.
  - [x] Do not introduce a cross-key transaction claim while adding cancellation. If cancellation occurs after one guarded state save and before another, rely on replay/idempotency and assert only that the implementation stops before the next awaited read/write boundary.
  - [x] If the current projection handler still uses plain `SaveStateAsync`/`GetStateAsync`, coordinate with the 10.1 and 10.2 implementation state before editing so cancellation does not mask existing write-safety work.
- [x] Keep cancellation taxonomy explicit. (AC: 5, 7)
  - [x] Let cancellation surface as `OperationCanceledException` or the EventStore-approved cancellation result shape.
  - [x] Do not map cancellation to `QueryResult.Success == true`, empty successful payloads, `Forbidden`, `Invalid cursor`, `Tenant not found`, generic actor failure, ETag conflict, or retry exhaustion.
  - [x] Follow the EventStore-approved cancellation result shape exactly once Story 10.3A defines it; do not invent a Tenants-local convention for best-effort, immediate, checkpoint-aware, or retry-abort behavior.
  - [x] Add safe source-generated logs or metrics only when they follow existing repository patterns and do not leak tenant/user payload data.
- [x] Add focused deterministic tests. (AC: 1-7)
  - [x] Add or extend `TenantsProjectionActorTests` with pre-cancelled and mid-flow cancellation coverage for query state reads.
  - [x] Inject cancellation deterministically with pre-cancelled tokens, controlled fake EventStore boundaries, or cancellation triggered at known awaited points; do not depend on sleeps or scheduler timing.
  - [x] Capture the received `CancellationToken` in fakes/substitutes and assert token identity or cancellation state at each supported EventStore, DAPR state, and projection helper boundary named by the 10.3A handoff.
  - [x] Verify cancellation before DAPR state access prevents state reads for at least one role-sensitive query and one audit query.
  - [x] Verify a pre-cancelled malformed-user or forbidden request preserves the existing cheap-validation result before state access when that precedence is already externally observable.
  - [x] Verify cancellation during audit/list pagination does not return a partial successful page.
  - [x] Add or extend `TenantProjectionHandlerTests` to prove projection state reads/writes receive the same token and abort cleanly when cancelled.
  - [x] Verify projection cancellation after a completed first save does not attempt later saves in the same operation and does not weaken replay/idempotency expectations from Stories 10.1 and 10.2.
  - [x] Assert cancellation is not reported as a successful empty payload, forbidden, invalid cursor, tenant not found, generic actor failure, ETag conflict, or retry exhaustion.
  - [x] Verify non-cancelled requests still pass existing authorization, cursor, pagination, audit, and projection result assertions.
  - [x] Use deterministic fakes or substitutes; do not rely on timing sleeps, live DAPR, Redis, Aspire, or real network calls.
- [x] Keep scope boundaries explicit. (AC: 4, 6)
  - [x] Do not change public query DTOs, route templates, cursor format, signed cursor scope, authorization policy, tenant visibility policy, audit entry schema, or projection state key names.
  - [x] Do not add package dependencies or package versions.
  - [x] Do not modify `Hexalith.EventStore` from this story except to update the parent submodule pointer to an already completed 10.3A commit when required and approved.
  - [x] Leave Story 10.4 reusable projection conformance coverage for its dedicated story.

## Dev Notes

### Dependency Gate

- Story 10.3A completed the EventStore cancellation handoff in submodule commit `bcccd504b5c4f1984e854aa73928ec5670f0a4e9`. Consume that API surface; do not re-open the EventStore contract decision inside Tenants.
- Approved query dispatch APIs available to Tenants:
  - `QueryRouter.RouteQueryAsync(SubmitQuery query, CancellationToken cancellationToken = default)` now carries the route-level token to DAPR actor invocation through weak `ActorProxy.InvokeMethodAsync<QueryEnvelope, QueryResult>(nameof(IProjectionActor.QueryAsync), envelope, cancellationToken)` when the generated proxy is an `ActorProxy`.
  - The source-compatible fallback remains `IProjectionActor.QueryAsync(QueryEnvelope envelope)` when a proxy is not an `ActorProxy`; that fallback cannot carry a downstream request-abort token.
  - `CachingProjectionActor.QueryAsync(QueryEnvelope envelope, CancellationToken cancellationToken)` observes cancellation before ETag lookup, cache-hit return, query execution, and cache storage.
  - `CachingProjectionActor.ExecuteQueryAsync(QueryEnvelope envelope, CancellationToken cancellationToken)` is the cancellation-aware derived-actor hook Tenants should override. The legacy `ExecuteQueryAsync(QueryEnvelope envelope)` remains for source compatibility and delegates from the token-aware hook only when a derived actor has not implemented the new path.
  - `FakeProjectionActor.QueryAsync(QueryEnvelope envelope, CancellationToken cancellationToken)` records received tokens for deterministic tests.
- Approved projection delivery and replay APIs available to Tenants:
  - `IProjectionUpdateOrchestrator.UpdateProjectionAsync(AggregateIdentity identity, CancellationToken cancellationToken = default)` carries cancellation through EventStore projection delivery, including the HTTP `/project` send and response-read boundaries.
  - `EventStoreProjection<TReadModel>.Project(IEnumerable events, CancellationToken cancellationToken)` and `ProjectFromJson(JsonElement jsonArray, CancellationToken cancellationToken)` observe cancellation between event applications.
  - `EventReplayProjectionActor.UpdateProjectionAsync(ProjectionState state, CancellationToken cancellationToken)` passes cancellation to EventStore-owned DAPR actor state and notification operations.
  - `IProjectionWriteActor.UpdateProjectionAsync(ProjectionState state)` remains the no-token DAPR actor interface for projection state writes; Story 10.3B must not claim actor-boundary write cancellation beyond the supported APIs above.
- Cancellation taxonomy from Story 10.3A: bare `OperationCanceledException` / `TaskCanceledException` is allowed to surface and must not be mapped to successful empty results, query adapter failures, not-found, forbidden, invalid cursor, ETag conflict, or retry exhaustion. EventStore intentionally documents wrapped DAPR/SignalR cancellation exceptions as fail-open/failure-boundary cases where the transport wraps the cancellation inside another exception type.
- Implementation may now start after confirming the parent repo points at `bcccd504` or a descendant. Do not initialize nested submodules while checking the dependency.

### Current Code State

- `TenantsProjectionActor` now inherits from an EventStore base that exposes `protected virtual Task<QueryResult> ExecuteQueryAsync(QueryEnvelope envelope, CancellationToken cancellationToken)`, but Tenants still overrides only `protected override async Task<QueryResult> ExecuteQueryAsync(QueryEnvelope envelope)` with no token. It dispatches to `HandleGetTenantAsync`, `HandleListTenantsAsync`, `HandleGetTenantUsersAsync`, `HandleGetUserTenantsAsync`, and `HandleGetTenantAuditAsync`, all of which call DAPR `GetStateAsync` without cancellation. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`; `Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/CachingProjectionActor.cs` at `bcccd504`]
- Query handlers perform authorization, cursor decoding, in-memory filtering, ordering, pagination, and JSON serialization. Cancellation work must not reorder authorization/cursor semantics for non-cancelled requests. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`; Stories 9.3-9.5]
- `TenantProjectionHandler.ProjectAsync(ProjectionRequest request)` currently has no token parameter and writes projection state through DAPR state APIs. Stories 10.1 and 10.2 added guarded write policy behavior; this story must thread cancellation through those helpers without weakening retry, merge, idempotency, or recovery behavior. [Source: `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`; `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs`; Stories 10.1 and 10.2]
- `ProjectionDispatcher.DispatchAsync(ProjectionRequest request)` and the `/project` endpoint in `src/Hexalith.Tenants/Program.cs` currently drop the ASP.NET request-abort token before it reaches `TenantProjectionHandler.ProjectAsync`. EventStore now carries cancellation through its projection delivery HTTP send/read boundary, so Tenants must accept `CancellationToken` at `/project`, pass it through the dispatcher, and use it in tenant/global-admin projection handlers where DAPR APIs support it. [Source: `src/Hexalith.Tenants/Program.cs`; `src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs`; `Hexalith.EventStore/src/Hexalith.EventStore.Server/Projections/ProjectionUpdateOrchestrator.cs` at `bcccd504`]
- `src/Hexalith.Tenants/Program.cs` already receives cancellation for the manual DAPR subscribe endpoint and passes it to `handler.ProcessAsync(request, cancellationToken)`. This endpoint token is separate from the projection query actor execution and `/project` projection write paths. [Source: `src/Hexalith.Tenants/Program.cs`]

### Architecture and Scope Boundaries

- Follow EventStore projection and DAPR actor patterns exactly. Do not bypass `QueryRouter`, `CachingProjectionActor`, `IProjectionActor`, or the EventStore projection dispatch contract with a Tenants-only cancellation path. [Source: `_bmad-output/planning-artifacts/architecture.md`; Story 10.3A]
- Query cache ETags and DAPR state persistence ETags are separate concerns. Cancellation must not alter cache-key behavior, projection type discovery, signed cursor compatibility, or ETag write-safety policy. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/CachingProjectionActor.cs`; Stories 9.5, 10.1, 10.2]
- Preserve the repository's privacy posture from actor guardrail work: cancellation logs may include safe operation categories, query type, stage, attempt count, and correlation/trace identifiers, but not payloads, tenant names, membership details, configuration values, cursor payloads, or user-controllable display names. [Source: Story 9.4]

### Implementation Guardrails

- Add token parameters narrowly and mechanically after the EventStore API shape is known. Avoid broad helper abstractions unless Stories 10.1/10.2 already introduced an internal projection state adapter that should receive the token.
- Prefer `CancellationToken.ThrowIfCancellationRequested()` before expensive in-memory loops and before starting retry attempts. For DAPR calls, pass the token to overloads that support it.
- Place cancellation checkpoints before state access, expensive filtering/sorting/pagination, retry attempts, and multi-item audit/projection processing; avoid noisy checks between trivial synchronous operations.
- Keep cheap validation precedence explicit. Cancellation must not become an accidental shortcut that changes malformed-user, forbidden, invalid-cursor, or tenant-visibility outcomes already hardened by Stories 9.3 and 9.4; checkpoints should still occur before DAPR state I/O and expensive processing.
- Treat multi-key projection writes as checkpoint-aware, not atomic. If cancellation is observed after a prior guarded save has completed, do not compensate, delete, or rewrite state; rely on the existing replay/idempotency contracts and stop before later awaited operations.
- Do not catch `OperationCanceledException` and convert it into a successful `QueryResult`, projection `ProjectionResponse`, or ordinary adapter failure.
- Preserve `ConfigureAwait(false)` conventions in the existing async code.
- If an EventStore compatibility wrapper uses `CancellationToken.None` for legacy callers, tests must prove no-token callers keep existing behavior.

### Files Likely To Update

- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`: token-aware query execution and DAPR read calls after EventStore exposes the token.
- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`: token-aware projection persistence once EventStore exposes the projection handler token.
- `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs` or equivalent Story 10.1/10.2 helper: pass cancellation to guarded reads/saves and retry loops if that helper exists.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`: query cancellation coverage and non-cancelled regression checks.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs`: projection write/read cancellation coverage.
- Parent submodule pointer for `Hexalith.EventStore`: only if Story 10.3A has already been completed in the submodule and this story consumes that commit.

### Testing Requirements

- Use xUnit v3, Shouldly, and NSubstitute or focused fakes, matching existing tests.
- Use pre-cancelled tokens and controllable fake/substitute callbacks to make cancellation deterministic.
- Avoid sleeps, wall-clock timing, live DAPR sidecars, Redis, Aspire orchestration, and real network calls.
- Run at minimum after implementation:
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsProjectionActorTests`
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantProjectionHandlerTests`
- If the EventStore submodule pointer changes or contract signatures changed, also run:
  - `dotnet build Hexalith.EventStore/Hexalith.EventStore.slnx --configuration Debug --no-restore`
  - `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore`

### Latest Technical Information

- The repository currently pins Dapr Client `1.17.9`, Aspire `13.3.3`, Microsoft ASP.NET Core packages `10.0.8`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and NSubstitute `6.0.0-rc.1`. This story should not update dependencies. [Source: `Directory.Packages.props`]
- The current local DAPR client usage already has async state APIs that can receive cancellation through overloads where available. Verify exact overloads against the pinned package XML or IntelliSense during implementation rather than guessing signatures.

### Previous Story Intelligence

- Story 10.3A created the prerequisite and party-mode review guidance for EventStore cancellation APIs. This story must consume that result instead of deciding the EventStore actor contract shape inside Tenants. [Source: `_bmad-output/implementation-artifacts/10-3a-eventstore-projection-cancellation-api-prerequisite.md`]
- Stories 10.1 and 10.2 own guarded ETag write safety for tenant detail/index and audit state. Cancellation must abort those operations cleanly without weakening retry, merge, idempotency, or observability guarantees. [Source: `_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md`; `_bmad-output/implementation-artifacts/10-2-audit-projection-write-safety.md`]
- Stories 9.3, 9.4, and 9.5 hardened tenant query visibility, actor guardrails, and cursor utilities. This story must not change query visibility, malformed-user precedence, cursor signing/scope, pagination semantics, or safe logging policy. [Source: `_bmad-output/implementation-artifacts/9-3-query-policy-for-disabled-tenants-and-orphan-memberships.md`; `_bmad-output/implementation-artifacts/9-4-actor-layer-query-guardrails.md`; `_bmad-output/implementation-artifacts/9-5-shared-pagination-bounds-and-cursor-utilities.md`]

### Git Intelligence

- Recent commits include active completion of Story 9.3, pre-dev hardening, Story 9.5 elicitation, and the 10.3A party-mode review. No parent-repo commit in the last five commits implements Tenants cancellation threading. [Source: `git log -5 --oneline`]
- This automation run started with an active-dev-story soft warning for Story 9.4 and related source/test changes. Leave that work untouched when committing this story context. [Source: `_bmad-output/process-notes/predev-preflight-latest.json` timestamp `2026-05-17T13:01:17Z`]

### Project Context Reference

- Follow `AGENTS.md`: do not initialize or update nested submodules recursively, and use Conventional Commits for commits.
- Follow repository C# conventions: nullable-safe code, file-scoped namespaces, central package management, no inline package versions, source-generated logging for structured logs, xUnit and Shouldly for tests.
- No root `project-context.md` exists for this application repository; submodule project-context files are reference context only. Relevant EventStore context: preserve query adapter failure taxonomy, DAPR actor compatibility, source-generated logging, and existing testing helpers.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-19: Verified `Hexalith.EventStore` submodule HEAD `bcccd504b5c4f1984e854aa73928ec5670f0a4e9`; `git merge-base --is-ancestor bcccd504b5c4f1984e854aa73928ec5670f0a4e9 HEAD` returned true. No nested submodules were initialized or updated.
- 2026-05-19: EventStore API signatures consumed: `CachingProjectionActor.QueryAsync(QueryEnvelope, CancellationToken)`, `CachingProjectionActor.ExecuteQueryAsync(QueryEnvelope, CancellationToken)`, `QueryRouter.RouteQueryAsync(SubmitQuery, CancellationToken = default)`, `IProjectionUpdateOrchestrator.UpdateProjectionAsync(AggregateIdentity, CancellationToken = default)`, `EventStoreProjection<TReadModel>.Project(IEnumerable, CancellationToken)`, `EventStoreProjection<TReadModel>.ProjectFromJson(JsonElement, CancellationToken)`, and `EventReplayProjectionActor.UpdateProjectionAsync(ProjectionState, CancellationToken)`. Unsupported boundary remains `IProjectionWriteActor.UpdateProjectionAsync(ProjectionState)` with no token.
- 2026-05-19: Focused validation passed: `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsProjectionActorTests` (78 passed).
- 2026-05-19: Focused validation passed: `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantProjectionHandlerTests` (17 passed).
- 2026-05-19: Full validation passed on rerun: `dotnet test Hexalith.Tenants.slnx --configuration Debug --no-restore` (Server.Tests 395 passed / 3 skipped; IntegrationTests 33 passed / 1 skipped; Contracts 35 passed; Client 48 passed; Testing 89 passed; Sample 17 passed). Earlier full-suite attempt had one isolated telemetry status failure that passed when rerun by fully qualified test name and passed in the subsequent full-suite rerun.

### Completion Notes List

- Implemented the EventStore-approved cancellation-aware query hook in `TenantsProjectionActor` while preserving the legacy no-token override through `CancellationToken.None`.
- Threaded cancellation tokens through tenant detail, tenant index, tenant users, user tenants, and tenant audit DAPR state reads, with checkpoints before state I/O, expensive pagination/filtering materialization, serialization, and orphan membership loops.
- Passed `/project` request cancellation through `ProjectionDispatcher` into tenant and global-administrator projection handlers.
- Threaded cancellation through `TenantProjectionHandler` and `TenantProjectionWritePolicy` guarded read/save/retry boundaries without changing ETag conflict, retry exhaustion, replay, or idempotency semantics.
- Added deterministic actor and projection handler tests for pre-cancelled requests, mid-flow cancellation, token identity propagation, no partial audit success, and no later writes after cancellation following a completed tenant save.
- No public query DTOs, routes, cursor format/scope, authorization policy, tenant visibility rules, audit schema, state key names, package dependencies, or EventStore submodule contents were changed.

### File List

- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `src/Hexalith.Tenants/Program.cs`
- `src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs`
- `src/Hexalith.Tenants/Projections/ProjectionDispatcher.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`
- `_bmad-output/implementation-artifacts/10-3b-cancellation-token-threading-for-tenant-projection-queries.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-19: Implemented cancellation token threading for tenant projection queries and projection writes; added deterministic cancellation tests; full Debug/no-restore solution test gate passed; story moved to review.

## Party-Mode Review

- Date/time: 2026-05-17T18:09:12+02:00
- Selected story key: 10-3b-cancellation-token-threading-for-tenant-projection-queries
- Command/skill invocation used: `/bmad-party-mode 10-3b-cancellation-token-threading-for-tenant-projection-queries; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - Reviewers agreed the story is product-relevant and correctly scoped, but implementation must remain gated on Story 10.3A's concrete EventStore cancellation API handoff.
  - The primary architecture risk is a Tenants-local workaround that bypasses EventStore query/projection contracts, shadows overloads, or changes cursor, authorization, audit, state-key, or write-safety behavior.
  - The primary test risk is cancellation being swallowed as a successful empty result or misclassified as forbidden, invalid cursor, not found, actor failure, ETag conflict, or retry exhaustion.
  - Deterministic tests should use pre-cancelled tokens, fake EventStore boundaries, and cancellation at known await points rather than sleeps, live DAPR, Redis, Aspire, or network timing.
- Changes applied:
  - Added an acceptance criterion making the 10.3A EventStore commit/signature handoff a hard sequencing gate before Tenants code changes.
  - Tightened prerequisite tasks to forbid Tenants-only EventStore bypasses, local overload shims, synchronous API wrapping, and direct state-client work outside the approved projection path.
  - Clarified cancellation taxonomy so Tenants follows the EventStore-approved result/exception shape instead of inventing local best-effort, checkpoint, or retry-abort semantics.
  - Expanded deterministic test guidance for pre-cancelled and mid-flow cancellation, fake EventStore boundaries, non-cancelled regressions, and cancellation-not-domain-error assertions.
  - Added implementation guidance for checkpoint placement before state access, expensive loops, retry attempts, and multi-item processing without adding noisy checks between trivial synchronous operations.
- Findings deferred:
  - Exact EventStore cancellation-aware API names, signatures, overload compatibility, and submodule commit remain Story 10.3A decisions.
  - Exact cancellation result shape, exception behavior, and retry interaction remain governed by the 10.3A handoff or a later architecture decision.
  - Integration-level validation with live DAPR, Redis, Aspire, actor hosting, or network behavior remains outside this deterministic story-hardening pass.
  - Broader cancellation propagation across unrelated tenant services remains out of scope.
- Final recommendation: needs-story-update

## Advanced Elicitation

- Date/time: 2026-05-17T20:02:43+02:00
- Selected story key: 10-3b-cancellation-token-threading-for-tenant-projection-queries
- Command/skill invocation used: `/bmad-advanced-elicitation 10-3b-cancellation-token-threading-for-tenant-projection-queries`
- Batch 1 method names: Tree of Thoughts; Red Team vs Blue Team; Failure Mode Analysis; Socratic Questioning; Critique and Refine
- Reshuffled Batch 2 method names: Self-Consistency Validation; Pre-mortem Analysis; Code Review Gauntlet; First Principles Analysis; Occam's Razor Application
- Findings summary:
  - The story already had the correct 10.3A dependency gate, but cancellation precedence could still accidentally mask existing cheap validation and authorization behavior.
  - Tests needed stronger evidence that the exact cancellation token reaches each supported EventStore, DAPR state, and projection helper boundary.
  - Projection-write cancellation needed explicit non-atomic, replay/idempotency-aware behavior so developers do not infer cross-key rollback semantics.
  - The implementation should remain checkpoint-aware and narrow rather than introducing broad abstractions or Tenants-only cancellation conventions.
- Changes applied:
  - Added ACs for preserving existing cheap-validation precedence before cancellation checkpoints and for non-atomic multi-key projection write cancellation.
  - Tightened query tasks to place cancellation after existing cheap guards but before state I/O and expensive work.
  - Added projection-write guidance to rely on replay/idempotency after a completed save and stop before later awaited operations.
  - Expanded deterministic test guidance for token identity propagation, pre-cancelled malformed-user/forbidden precedence, and no-further-save behavior after observed cancellation.
  - Added implementation notes for validation precedence and checkpoint-aware multi-key writes.
- Findings deferred:
  - Exact EventStore cancellation-aware signatures and supported boundaries remain governed by Story 10.3A.
  - Exact projection helper or state adapter call sites remain dependent on the completed 10.1 and 10.2 implementation shape.
  - Any stronger cross-key transactional guarantee remains out of scope for this story.
- Final recommendation: ready-for-dev
