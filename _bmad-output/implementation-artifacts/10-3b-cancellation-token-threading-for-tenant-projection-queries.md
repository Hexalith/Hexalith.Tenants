# Story 10.3B: Cancellation Token Threading for Tenant Projection Queries

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

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

## Tasks / Subtasks

- [ ] Verify the Story 10.3A EventStore prerequisite before implementation. (AC: 1, 3, 4)
  - [ ] Confirm the `Hexalith.EventStore` submodule commit that contains the approved cancellation-aware projection API.
  - [ ] Record the exact EventStore signatures available to Tenants, including the query actor/dispatch path and projection handler path.
  - [ ] If EventStore still only exposes `IProjectionActor.QueryAsync(QueryEnvelope envelope)`, `CachingProjectionActor.ExecuteQueryAsync(QueryEnvelope envelope)`, and `TenantProjectionHandler.ProjectAsync(ProjectionRequest request)`-style no-token paths, stop implementation and return this story for prerequisite completion instead of inventing a Tenants-only bypass.
  - [ ] Do not initialize or update nested submodules recursively while checking the dependency.
- [ ] Thread cancellation through tenant projection query execution once EventStore exposes the token. (AC: 1, 2, 5, 6)
  - [ ] Update `TenantsProjectionActor.ExecuteQueryAsync(...)` to accept and pass the token according to the EventStore API shape from Story 10.3A.
  - [ ] Update `HandleGetTenantAsync`, `HandleListTenantsAsync`, `HandleGetTenantUsersAsync`, `HandleGetUserTenantsAsync`, and `HandleGetTenantAuditAsync` to accept the token and pass it to DAPR `GetStateAsync` calls.
  - [ ] Check the token before expensive in-memory filtering, sorting, pagination, and audit result serialization where the operation may process a large read model.
  - [ ] Preserve malformed-user precedence: role-sensitive queries with missing or whitespace `UserId` must still return forbidden before cursor validation or state reads.
  - [ ] Preserve invalid cursor behavior for non-cancelled requests.
- [ ] Thread cancellation through tenant projection write handling once EventStore exposes the token. (AC: 3, 5, 6)
  - [ ] Update `TenantProjectionHandler.ProjectAsync(...)` to accept the token only through the EventStore-approved projection contract.
  - [ ] Pass the token to DAPR state reads and saves for `projection:tenants:{tenantId}`, `projection:tenant-index:singleton`, and `audit:{tenantId}` where those paths are present after Stories 10.1 and 10.2.
  - [ ] Preserve guarded ETag retry semantics from Stories 10.1 and 10.2; cancellation must abort retry loops, not convert cancellation into a conflict, retry exhaustion, or successful projection response.
  - [ ] If the current projection handler still uses plain `SaveStateAsync`/`GetStateAsync`, coordinate with the 10.1 and 10.2 implementation state before editing so cancellation does not mask existing write-safety work.
- [ ] Keep cancellation taxonomy explicit. (AC: 5, 7)
  - [ ] Let cancellation surface as `OperationCanceledException` or the EventStore-approved cancellation result shape.
  - [ ] Do not map cancellation to `QueryResult.Success == true`, empty successful payloads, `Forbidden`, `Invalid cursor`, `Tenant not found`, generic actor failure, ETag conflict, or retry exhaustion.
  - [ ] Add safe source-generated logs or metrics only when they follow existing repository patterns and do not leak tenant/user payload data.
- [ ] Add focused deterministic tests. (AC: 1-7)
  - [ ] Add or extend `TenantsProjectionActorTests` with pre-cancelled and mid-flow cancellation coverage for query state reads.
  - [ ] Verify cancellation before DAPR state access prevents state reads for at least one role-sensitive query and one audit query.
  - [ ] Verify cancellation during audit/list pagination does not return a partial successful page.
  - [ ] Add or extend `TenantProjectionHandlerTests` to prove projection state reads/writes receive the same token and abort cleanly when cancelled.
  - [ ] Verify non-cancelled requests still pass existing authorization, cursor, pagination, audit, and projection result assertions.
  - [ ] Use deterministic fakes or substitutes; do not rely on timing sleeps, live DAPR, Redis, Aspire, or real network calls.
- [ ] Keep scope boundaries explicit. (AC: 4, 6)
  - [ ] Do not change public query DTOs, route templates, cursor format, signed cursor scope, authorization policy, tenant visibility policy, audit entry schema, or projection state key names.
  - [ ] Do not add package dependencies or package versions.
  - [ ] Do not modify `Hexalith.EventStore` from this story except to update the parent submodule pointer to an already completed 10.3A commit when required and approved.
  - [ ] Leave Story 10.4 reusable projection conformance coverage for its dedicated story.

## Dev Notes

### Dependency Gate

- This story is intentionally dependent on Story 10.3A. As of `Hexalith.EventStore` submodule commit `da2f2cf3`, the key APIs still do not expose cancellation:
  - `Hexalith.EventStore.Contracts/Queries/IProjectionActor.cs`: `Task<QueryResult> QueryAsync(QueryEnvelope envelope)`.
  - `Hexalith.EventStore.Server/Actors/CachingProjectionActor.cs`: `public async Task<QueryResult> QueryAsync(QueryEnvelope envelope)` and `protected abstract Task<QueryResult> ExecuteQueryAsync(QueryEnvelope envelope)`.
  - `Hexalith.EventStore.Server/Queries/QueryRouter.cs`: `RouteQueryAsync(SubmitQuery query, CancellationToken cancellationToken = default)` receives a token before actor dispatch, but the actor call remains `proxy.QueryAsync(envelope)` in the current prerequisite analysis.
  - `Hexalith.EventStore.Client/Aggregates/EventStoreProjection.cs`: projection replay helpers are synchronous in the 10.3A analysis.
- Do not begin Tenants implementation until the completed 10.3A work names the exact API shape Story 10.3B should consume. If 10.3A concludes that a boundary cannot carry cancellation directly, implement only the supported Tenants-side cancellation boundaries and document the limitation in tests.

### Current Code State

- `TenantsProjectionActor` currently overrides `protected override async Task<QueryResult> ExecuteQueryAsync(QueryEnvelope envelope)` with no token. It dispatches to `HandleGetTenantAsync`, `HandleListTenantsAsync`, `HandleGetTenantUsersAsync`, `HandleGetUserTenantsAsync`, and `HandleGetTenantAuditAsync`, all of which call DAPR `GetStateAsync` without cancellation. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- Query handlers perform authorization, cursor decoding, in-memory filtering, ordering, pagination, and JSON serialization. Cancellation work must not reorder authorization/cursor semantics for non-cancelled requests. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`; Stories 9.3-9.5]
- `TenantProjectionHandler.ProjectAsync(ProjectionRequest request)` currently has no token parameter and writes projection state through DAPR state APIs. Active/ready Stories 10.1 and 10.2 own ETag write safety, so this story must layer cancellation onto whatever guarded write helper exists when implementation begins. [Source: `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`; Stories 10.1 and 10.2]
- `src/Hexalith.Tenants/Program.cs` already receives cancellation for the manual DAPR subscribe endpoint and passes it to `handler.ProcessAsync(request, cancellationToken)`. This endpoint token is not yet threaded into EventStore projection query actor execution or `TenantProjectionHandler.ProjectAsync`. [Source: `src/Hexalith.Tenants/Program.cs`]

### Architecture and Scope Boundaries

- Follow EventStore projection and DAPR actor patterns exactly. Do not bypass `QueryRouter`, `CachingProjectionActor`, `IProjectionActor`, or the EventStore projection dispatch contract with a Tenants-only cancellation path. [Source: `_bmad-output/planning-artifacts/architecture.md`; Story 10.3A]
- Query cache ETags and DAPR state persistence ETags are separate concerns. Cancellation must not alter cache-key behavior, projection type discovery, signed cursor compatibility, or ETag write-safety policy. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/CachingProjectionActor.cs`; Stories 9.5, 10.1, 10.2]
- Preserve the repository's privacy posture from actor guardrail work: cancellation logs may include safe operation categories, query type, stage, attempt count, and correlation/trace identifiers, but not payloads, tenant names, membership details, configuration values, cursor payloads, or user-controllable display names. [Source: Story 9.4]

### Implementation Guardrails

- Add token parameters narrowly and mechanically after the EventStore API shape is known. Avoid broad helper abstractions unless Stories 10.1/10.2 already introduced an internal projection state adapter that should receive the token.
- Prefer `CancellationToken.ThrowIfCancellationRequested()` before expensive in-memory loops and before starting retry attempts. For DAPR calls, pass the token to overloads that support it.
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

### Completion Notes List

### File List
