# Story 10.3A: EventStore Projection Cancellation API Prerequisite

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a platform framework maintainer,
I want EventStore projection query and projection dispatch contracts to expose cancellation-aware signatures,
so that tenant query and projection code can observe abandoned requests without inventing Tenants-only infrastructure.

## Acceptance Criteria

1. Given `IProjectionActor.QueryAsync` is the public DAPR actor projection query contract, when cancellation support is added, then the contract or an approved companion path accepts and propagates a `CancellationToken` without breaking existing callers unexpectedly.
2. Given `CachingProjectionActor` executes projection query logic, when derived actors implement query handlers, then `ExecuteQueryAsync` or its approved replacement receives cancellation and can pass it to downstream reads.
3. Given `QueryRouter.RouteQueryAsync` already receives a `CancellationToken`, when it routes through actor proxies, then cancellation is not silently dropped before projection query execution.
4. Given projection read/write infrastructure uses DAPR state APIs, when projection state is read or written through EventStore-owned projection infrastructure, then DAPR calls receive the propagated cancellation token where supported.
5. Given `EventStoreProjection<TReadModel>.Project(...)` and `ProjectFromJson(...)` are synchronous projection replay helpers, when cancellation support is evaluated, then the story either introduces an approved cancellation-aware projection path or explicitly records why these pure replay helpers remain synchronous and where cancellation is observed instead.
6. Given existing EventStore and Tenants callers compile against non-cancellation query/projection APIs, when the prerequisite is implemented, then compatibility is preserved through overloads, default tokens, adapter methods, or a documented breaking-change decision with downstream updates in the same change.
7. Given the EventStore API change is merged or otherwise available to Tenants, when Story 10.3B starts, then the Tenants story names the exact EventStore APIs and submodule commit it depends on.
8. Given focused EventStore tests run, when cancellation is triggered before actor query routing, during query execution, and before DAPR state access, then tests verify cancellation is observed and no successful query/projection result is reported after cancellation.
9. Given existing query adapter failures have established result shapes, when cancellation is observed, then cancellation remains distinguishable from not-found actors, unsuccessful projection results, missing payloads, serialization failures, and actor invocation failures.

## Tasks / Subtasks

- [ ] Confirm the viable EventStore cancellation API shape before editing contracts. (AC: 1, 3, 5, 6)
  - [ ] Inspect Dapr actor constraints for `IProjectionActor` method signatures before adding `CancellationToken` directly to an actor interface.
  - [ ] Choose one approved path: cancellation-aware overloads, a companion internal dispatch path, an actor invocation option that carries cancellation, or an explicit documented reason actor boundary cancellation cannot be represented directly.
  - [ ] Record the chosen contract path before broad edits, including whether `IProjectionActor.QueryAsync`, `CachingProjectionActor.QueryAsync`, `CachingProjectionActor.ExecuteQueryAsync`, router/proxy dispatch, or replay helpers change.
  - [ ] Preserve existing callers with non-cancellation overloads/default token behavior unless a deliberate breaking change is approved and implemented across EventStore and dependent tests.
  - [ ] Keep `IProjectionActor.QueryAsync(QueryEnvelope envelope)` callable unless the EventStore maintainers explicitly accept a breaking actor contract change.
  - [ ] Do not invent a Tenants-only cancellation adapter to bypass EventStore query routing.
- [ ] Add cancellation propagation through projection query routing. (AC: 1, 2, 3, 6)
  - [ ] Update `QueryRouter.RouteQueryAsync(...)` so the existing route-level token remains meaningful through the projection actor invocation path.
  - [ ] Update `CachingProjectionActor.QueryAsync(...)` and `ExecuteQueryAsync(...)` or their approved replacements so derived projection actors can observe cancellation.
  - [ ] If the public actor boundary cannot carry cancellation, define the supported boundary explicitly: pre-dispatch cancellation before proxy invocation, a cancellation-aware internal execution path, or another approved compatibility path.
  - [ ] Update `EventReplayProjectionActor` and `FakeProjectionActor` to compile against the selected API shape and preserve existing behavior with default cancellation.
  - [ ] Keep query adapter failure behavior unchanged for not-found actors, unsuccessful results, missing payloads, serialization failures, and actor invocation failures.
  - [ ] Ensure legacy no-token calls route to `CancellationToken.None` or the selected equivalent compatibility behavior.
- [ ] Evaluate projection replay cancellation without corrupting pure projection semantics. (AC: 4, 5)
  - [ ] Review `EventStoreProjection<TReadModel>.Project(...)` and `ProjectFromJson(...)` call sites before changing method signatures.
  - [ ] If replay can be long-running enough to require cancellation, add cancellation-aware overloads that check the token between events and preserve the existing synchronous methods as delegating compatibility wrappers.
  - [ ] If replay helpers remain synchronous by design, document that cancellation is observed at dispatch/state I/O boundaries and leave pure `Apply` replay behavior unchanged.
  - [ ] Do not skip unknown historical event failures, malformed event failures, or projection notification failures as part of cancellation work.
- [ ] Thread cancellation into EventStore-owned DAPR state operations where the selected API exposes a token. (AC: 2, 4, 8)
  - [ ] Pass cancellation to DAPR state reads/writes in projection query or projection update infrastructure when the Dapr Client API supports it.
  - [ ] Inventory each touched DAPR state API and record whether a native cancellation parameter is available; document unsupported calls as cancellation boundaries.
  - [ ] Preserve fail-open/fail-closed decisions already present in EventStore query cache, projection replay, and query router code.
  - [ ] Ensure cancellation surfaces as cancellation, not as a successful empty result or generic projection adapter failure.
- [ ] Add focused EventStore tests. (AC: 1-8)
  - [ ] Add or update `Hexalith.EventStore.Contracts`/`Server` tests for the selected cancellation-aware query contract shape.
  - [ ] Extend `QueryRouterTests` to verify a pre-cancelled token is observed before successful actor query completion.
  - [ ] Add a router/proxy test proving a pre-cancelled token avoids proxy invocation where the selected boundary supports that behavior.
  - [ ] Extend `CachingProjectionActor` or derived actor tests so `ExecuteQueryAsync` receives and honors cancellation, if the selected API exposes the token there.
  - [ ] Extend `FakeProjectionActorTests` so testing fakes remain source-compatible and can simulate cancellation.
  - [ ] Add compatibility coverage proving legacy no-token callers still compile and use default non-cancelled behavior.
  - [ ] Add cancellation taxonomy coverage proving cancellation is not converted into existing adapter failure categories.
  - [ ] Use deterministic fakes that record invocation count and received token identity; avoid timing sleeps.
  - [ ] Add replay helper tests only if `EventStoreProjection<TReadModel>` gains cancellation-aware overloads.
  - [ ] Keep tests deterministic; do not use sleeps, live DAPR sidecars, Redis, Aspire, or real network calls.
- [ ] Prepare the Tenants handoff evidence. (AC: 7)
  - [ ] Record the exact EventStore APIs/signatures changed and the EventStore submodule commit that contains them.
  - [ ] Note the expected Tenants follow-up call sites for Story 10.3B: `TenantsProjectionActor.ExecuteQueryAsync`, its `Handle*Async` query methods, and projection read/write paths in `TenantProjectionHandler` only where EventStore APIs expose cancellation.
  - [ ] State any synchronous replay or actor-boundary limitations that Story 10.3B must respect.
  - [ ] Do not start Story 10.3B implementation in this prerequisite story.

## Dev Notes

### Dependency Status

- As of EventStore submodule commit `a1790f94`, the key projection APIs used by Tenants are not cancellation-aware at the projection actor/query-handler boundary:
  - `Hexalith.EventStore.Contracts/Queries/IProjectionActor.cs`: `Task<QueryResult> QueryAsync(QueryEnvelope envelope)`.
  - `Hexalith.EventStore.Server/Actors/CachingProjectionActor.cs`: `public async Task<QueryResult> QueryAsync(QueryEnvelope envelope)` and `protected abstract Task<QueryResult> ExecuteQueryAsync(QueryEnvelope envelope)`.
  - `Hexalith.EventStore.Client/Aggregates/EventStoreProjection.cs`: synchronous `Project(IEnumerable events)` and `ProjectFromJson(JsonElement jsonArray)`.
  - `Hexalith.EventStore.Server/Queries/QueryRouter.cs`: `RouteQueryAsync(SubmitQuery query, CancellationToken cancellationToken = default)` receives a token, but the current actor call is `proxy.QueryAsync(envelope)`.
  [Source: `Hexalith.EventStore/src/...` files listed above; `_bmad-output/planning-artifacts/epics.md#Story 10.3A`]
- `QueryRouter.RouteQueryAsync` currently calls `cancellationToken.ThrowIfCancellationRequested()` before routing and lets `OperationCanceledException` bubble, but the token does not reach the projection actor query method after the actor proxy is created. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Server/Queries/QueryRouter.cs`]
- `CachingProjectionActor` performs cache ETag lookup through `IETagService.GetCurrentETagAsync(...)`, then calls `ExecuteQueryAsync(envelope)` without a token. Derived actors such as Tenants cannot observe HTTP/request cancellation through this base signature today. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/CachingProjectionActor.cs`; `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `EventStoreProjection<TReadModel>` is a pure replay helper that iterates events and invokes `Apply` methods. If it remains synchronous, this story must explicitly document why cancellation belongs at dispatch/state I/O boundaries instead. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreProjection.cs`]

### Architecture and Scope Boundaries

- This prerequisite belongs in the `Hexalith.EventStore` submodule, not in Tenants application code. Story 10.3B depends on this story and should not implement a Tenants-only substitute. [Source: `_bmad-output/planning-artifacts/epics.md#Story 10.3A`; `_bmad-output/planning-artifacts/epics.md#Story 10.3B`]
- Follow the EventStore project context: preserve the pipeline ordering and query adapter error shapes, use source-generated logging patterns when adding logs, do not log payload data or user-controllable names, and prefer existing testing helpers in `Hexalith.EventStore.Testing`. [Source: `Hexalith.EventStore/_bmad-output/project-context.md`]
- Do not run recursive submodule initialization or update nested submodules. Work only with the root-level `Hexalith.EventStore` submodule when this story is implemented. [Source: `AGENTS.md`; `Hexalith.EventStore/_bmad-output/project-context.md`]
- Do not add package versions to project files. EventStore uses centralized package management and strict warning-as-error settings. [Source: `Hexalith.EventStore/_bmad-output/project-context.md`]
- Do not change Tenants cursor policy, query authorization policy, audit projection semantics, or read-model write concurrency behavior in this prerequisite story. Those are owned by Stories 9.3-9.5, 10.1, 10.2, and 10.3B. [Source: `_bmad-output/implementation-artifacts/9-3-query-policy-for-disabled-tenants-and-orphan-memberships.md`; `_bmad-output/implementation-artifacts/9-4-actor-layer-query-guardrails.md`; `_bmad-output/implementation-artifacts/9-5-shared-pagination-bounds-and-cursor-utilities.md`; `_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md`; `_bmad-output/implementation-artifacts/10-2-audit-projection-write-safety.md`]

### Implementation Guardrails

- Treat the Dapr actor boundary as the main design risk. Do not assume adding a `CancellationToken` parameter to `IProjectionActor.QueryAsync` is valid until verified against the Dapr actor proxy/runtime constraints used by this repo.
- Prefer source-compatible API evolution first: overloads, optional/default token parameters where valid, or internal companion dispatch methods. If a breaking actor contract change is necessary, update all EventStore actors, fakes, tests, and downstream compile points in the same implementation.
- The minimum product outcome is a documented, source-compatible EventStore cancellation path that Story 10.3B can consume. If direct actor-method token transport is invalid, the story still succeeds only when the chosen boundary is explicit and tested.
- Keep cancellation semantics explicit:
  - pre-cancelled tokens should prevent work before query dispatch or state I/O;
  - cancellation during execution should propagate as `OperationCanceledException` or the established cancellation path;
  - cancellation must not be converted into `QueryResult.Success == true`, empty successful payloads, generic "actor exception" failures, or cache hits that ignore the token.
- Preserve the existing query adapter failure taxonomy for non-cancellation failures: actor not found, actor response mismatch, unsuccessful projection result, missing payload, serialization failure, and actor invocation failure.
- If `EventStoreProjection<TReadModel>` gains cancellation-aware overloads, check the token between event applications and preserve the current `Project(...)` and `ProjectFromJson(...)` behavior for existing callers.
- If pure projection replay remains synchronous, add handoff notes for Story 10.3B that Tenants can only pass cancellation through query/state I/O paths exposed by EventStore, not through synchronous pure replay.

### Files Likely To Update

- `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/IProjectionActor.cs`: public actor query contract or companion cancellation-aware path.
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Queries/QueryRouter.cs`: route-level cancellation propagation beyond the current pre-dispatch check.
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/CachingProjectionActor.cs`: cancellation-aware query execution and derived actor hook.
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/EventReplayProjectionActor.cs`: derived actor update for the selected hook signature.
- `Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreProjection.cs`: only if replay helpers gain cancellation-aware overloads.
- `Hexalith.EventStore/src/Hexalith.EventStore.Testing/Fakes/FakeProjectionActor.cs`: keep tests/fakes compatible with the selected API.
- `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Queries/QueryRouterTests.cs`: cancellation routing tests.
- `Hexalith.EventStore/tests/Hexalith.EventStore.Testing.Tests/Fakes/FakeProjectionActorTests.cs`: fake compatibility/cancellation tests.
- `Hexalith.EventStore/tests/Hexalith.EventStore.Client.Tests/Aggregates/EventStoreProjectionTests.cs`: only if replay helper overloads are added.

### Testing Requirements

- Use xUnit v3, Shouldly, and NSubstitute/focused fakes following the EventStore test patterns.
- Run targeted EventStore tests first:
  - `dotnet test Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~QueryRouterTests`
  - `dotnet test Hexalith.EventStore/tests/Hexalith.EventStore.Testing.Tests/Hexalith.EventStore.Testing.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~FakeProjectionActorTests`
  - If replay helpers change: `dotnet test Hexalith.EventStore/tests/Hexalith.EventStore.Client.Tests/Hexalith.EventStore.Client.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~EventStoreProjectionTests`
- If contract signatures change across projects, also run:
  - `dotnet build Hexalith.EventStore/Hexalith.EventStore.slnx --configuration Debug --no-restore`
  - `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore` after the parent repository points to the updated EventStore submodule.
- Keep cancellation tests deterministic with pre-cancelled tokens or controllable fakes; do not rely on timing sleeps, live DAPR, Redis, Aspire, or network delays.
- Verify existing non-cancellation callers still compile and still receive the same success/failure behavior.

### Previous Story Intelligence

- Story 10.1 and Story 10.2 harden Tenants projection writes with guarded persistence and retry behavior. This prerequisite should not alter those policies; it only creates the EventStore cancellation surface that Story 10.3B can consume. [Source: `_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md`; `_bmad-output/implementation-artifacts/10-2-audit-projection-write-safety.md`]
- Story 9.4 hardened actor-layer query guardrails and logging privacy. Cancellation logs or traces must preserve that privacy posture and avoid tenant/user payload leakage. [Source: `_bmad-output/implementation-artifacts/9-4-actor-layer-query-guardrails.md`]
- Story 9.5 centralized pagination bounds/cursor utilities. Cancellation work must not change pagination response shape, cursor scope/signing, or result ordering. [Source: `_bmad-output/implementation-artifacts/9-5-shared-pagination-bounds-and-cursor-utilities.md`]

### Git Intelligence

- Recent commits recorded pre-dev hardening and active query-story work; no recent parent-repo commit creates the 10.3A story artifact. [Source: `git log -5 --oneline`; `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- This automation run started with an active-dev-story soft warning for Story 9.3: the 9.3 story artifact and sprint status had moved to `in-progress`, with related docs/source/test changes. Leave that work untouched. [Source: `_bmad-output/process-notes/predev-preflight-latest.json` timestamp `2026-05-17T12:19:29Z`]

### Project Context Reference

- Follow `AGENTS.md`: do not initialize or update nested submodules recursively, and use Conventional Commits for commits.
- No root `project-context.md` exists for this application repository; submodule project-context files are reference context only.
- Relevant EventStore context: target .NET SDK `10.0.103`, `net10.0`, warnings-as-errors, centralized package versions, Dapr actor/query patterns, RFC 7807 API error discipline, source-generated logging, and existing testing helpers.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

### File List

## Party-Mode Review

- Date/time: 2026-05-17T14:29:55+02:00
- Selected story key: 10-3a-eventstore-projection-cancellation-api-prerequisite
- Command/skill invocation used: `/bmad-party-mode 10-3a-eventstore-projection-cancellation-api-prerequisite; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - All reviewers found the story directionally valid and correctly scoped to `Hexalith.EventStore`.
  - The main readiness risk was ambiguous DAPR actor-boundary cancellation support and the exact compatibility shape.
  - Reviewers also flagged replay-helper boundaries, DAPR state API token inventory, cancellation/failure taxonomy, and deterministic fixture coverage.
- Changes applied:
  - Added AC coverage for preserving cancellation identity separately from existing query adapter failure categories.
  - Tightened tasks to require recording the chosen contract path before broad edits and keeping the legacy `IProjectionActor.QueryAsync(QueryEnvelope envelope)` callable unless a breaking change is explicitly accepted.
  - Added explicit fallback-boundary guidance when DAPR actor methods cannot carry cancellation directly.
  - Added DAPR state API inventory, legacy caller compatibility, deterministic fake, and cancellation taxonomy test guidance.
  - Strengthened Tenants handoff requirements to include exact signatures and synchronous/boundary limitations.
- Findings deferred:
  - Exact actor contract shape: direct actor overload, optional/default token, companion interface, internal dispatch path, or documented actor-boundary limitation.
  - Whether synchronous replay helpers gain cancellation-aware overloads or remain documented synchronous boundaries.
  - Exact behavior for DAPR state APIs that do not expose native cancellation parameters.
- Final recommendation: ready-for-dev
