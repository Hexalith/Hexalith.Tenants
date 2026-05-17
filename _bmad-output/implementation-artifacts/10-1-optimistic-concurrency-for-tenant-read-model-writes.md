# Story 10.1: Optimistic Concurrency for Tenant Read-Model Writes

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a platform operator,
I want tenant read-model writes to use optimistic concurrency,
so that concurrent projection updates do not silently overwrite tenant query state.

## Acceptance Criteria

1. Given multiple tenant events update `projection:tenants:{tenantId}` concurrently, when the projection writes read-model state, then updates use an optimistic concurrency or ETag-aware write path instead of last-writer-wins state replacement.
2. Given multiple tenant events update `projection:tenant-index:singleton` concurrently, when the shared tenant index is modified, then conflicting writes are retried or safely rejected according to a documented retry policy.
3. Given a concurrency conflict occurs during read-model persistence, when the retry policy is applied, then the final read model includes all successfully processed events without silently dropping one update.
4. Given the retry limit is exceeded, when the projection cannot safely persist state, then the failure is observable through logs/metrics and does not report a successful projection update.
5. Given focused tests simulate concurrent read-model writes, when tenant projection and tenant index updates race, then tests verify no silent data loss and document the selected retry behavior.

## Tasks / Subtasks

- [ ] Add a narrow optimistic write helper for tenant projection state. (AC: 1, 3, 4)
  - [ ] Use Dapr's ETag-aware state API from `Dapr.Client` 1.17.9: `GetStateAndETagAsync<TValue>` plus `StateEntry<TValue>.TrySaveAsync(...)` or `TrySaveStateAsync<TValue>(..., etag, ...)`.
  - [ ] Use `StateOptions { Concurrency = ConcurrencyMode.FirstWrite }` for guarded writes.
  - [ ] Keep the helper internal to the server application/projection path; do not expose public contract DTOs or new NuGet package surfaces for this story.
  - [ ] Treat a failed guarded save as a concurrency conflict and retry from a fresh state read/rebuild/merge, not by blindly saving the same stale model again.
- [ ] Replace last-writer-wins projection writes in `TenantProjectionHandler.ProjectAsync`. (AC: 1-4)
  - [ ] Update `projection:tenants:{tenantId}` persistence so concurrent projection writes cannot silently overwrite a newer tenant read model.
  - [ ] Update `projection:tenant-index:singleton` persistence so the current read-modify-write merge uses the ETag from the read it is based on.
  - [ ] Keep `audit:{tenantId}` persistence unchanged unless the same helper can be used without expanding scope; Story 10.2 owns audit-specific write safety.
  - [ ] Return `ProjectionResponse` only after all required guarded writes for this story have succeeded.
- [ ] Document and enforce the retry policy in code. (AC: 2-4)
  - [ ] Use a bounded retry policy, aligned with architecture guidance of max 3 attempts for cross-tenant index conflicts.
  - [ ] On each retry, reload the current state and re-apply the incoming events so the final model includes both previously persisted changes and this request's events.
  - [ ] When retry exhaustion occurs, log a structured warning/error with state key, aggregate ID, attempt count, and correlation ID/message IDs where available.
  - [ ] Do not log tenant names, configuration values, cursor payloads, event payload bodies, or user-controllable display names.
- [ ] Preserve read model semantics while adding concurrency. (AC: 1-4)
  - [ ] Keep per-tenant projection state keyed by `projection:tenants:{aggregateId}` and shared index state keyed by `projection:tenant-index:singleton`.
  - [ ] Keep `TenantReadModel.Apply(...)` and `TenantIndexReadModel.Apply(...)` as the single mutation rules; do not duplicate projection logic in the helper.
  - [ ] Preserve existing null-event skipping and event-type dispatch behavior in `ApplyEvent` and `ApplyIndexEvent`.
  - [ ] Do not change query actor authorization, cursor, pagination, route, or response contracts as part of this story.
- [ ] Add focused tests for conflict and retry behavior. (AC: 1-5)
  - [ ] Add or extend `TenantProjectionHandlerTests` with a fake/stub Dapr state interaction that simulates an ETag conflict on the first tenant read-model save and success on retry.
  - [ ] Add or extend tests for the singleton tenant index where two event batches target the same shared state key and the retry path preserves both updates.
  - [ ] Add a retry-exhaustion test proving `ProjectAsync` does not return success when guarded persistence cannot be confirmed.
  - [ ] Assert the state options use `ConcurrencyMode.FirstWrite` for guarded writes.
  - [ ] Keep tests deterministic and in-memory; do not require a live DAPR sidecar or Redis for this story.
- [ ] Keep scope boundaries explicit. (AC: 1-5)
  - [ ] Do not modify the `Hexalith.EventStore` submodule.
  - [ ] Do not add package dependencies or package versions.
  - [ ] Do not change `TenantsProjectionActor`, query controllers, signed cursor behavior, or pagination utilities unless needed only to compile against the changed projection write helper.
  - [ ] Leave Story 10.2 audit projection write safety and Story 10.3 cancellation-token threading for their dedicated stories.

## Dev Notes

### Policy To Implement

- The current projection write path must move from plain `SaveStateAsync(...)` to guarded ETag writes for the tenant read model and shared tenant index. Plain saves can overwrite newer state because they do not assert that the state read is still current. [Source: `_bmad-output/planning-artifacts/epics.md#Story 10.1`; `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`]
- Dapr Client 1.17.9 includes `GetStateAndETagAsync<TValue>`, `TrySaveStateAsync<TValue>(..., etag, ...)`, `StateEntry<TValue>.TrySaveAsync(...)`, `StateOptions.Concurrency`, and `ConcurrencyMode.FirstWrite`. Use those APIs before inventing a custom ETag abstraction. [Source: `Directory.Packages.props`; `%USERPROFILE%/.nuget/packages/dapr.client/1.17.9/lib/net10.0/Dapr.Client.xml`]
- Use max 3 attempts for the shared tenant index conflict policy, matching the architecture guidance for `projection:tenant-index:singleton`. Apply the same limit to the per-tenant read model unless implementation proves a narrower policy is needed and documents it. [Source: `_bmad-output/planning-artifacts/epics.md#Technical Assumptions`; `_bmad-output/planning-artifacts/epics.md#Story 10.1`]
- Retry means "reload current state and re-apply the incoming projection events", not "retry the stale write." For the singleton index this is essential because every tenant batch may merge into the same model. [Source: `_bmad-output/planning-artifacts/epics.md#Story 10.1`; `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`]
- If all retries fail, the projection must fail observably and must not return a successful `ProjectionResponse`. That lets EventStore/DAPR infrastructure retry or surface failure instead of hiding data loss. [Source: `_bmad-output/planning-artifacts/epics.md#Story 10.1`]

### Current Code State

- `TenantProjectionHandler.ProjectAsync` creates a new `TenantReadModel`, applies every non-null event via `ApplyEvent`, and writes `projection:tenants:{request.AggregateId}` with plain `DaprClient.SaveStateAsync`. [Source: `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`]
- The same method builds `TenantAuditReadModel` through `TenantAuditProjection.ProjectAuditEvents(...)` and writes `audit:{request.AggregateId}` with plain `SaveStateAsync`. This story may leave that path as-is because Story 10.2 owns audit-specific concurrency guarantees. [Source: `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`; `_bmad-output/planning-artifacts/epics.md#Story 10.2`]
- The shared index path currently loads `projection:tenant-index:singleton` with `GetStateAsync<TenantIndexReadModel>`, mutates it in memory with `ApplyIndexEvent`, and writes it with plain `SaveStateAsync`. That is the highest-risk last-writer-wins path because all tenants share one state key. [Source: `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`]
- `TenantProjection` and `TenantIndexProjection` still own the pure projection model types through `EventStoreProjection<TReadModel>`. Keep projection logic aligned with those read models rather than introducing a separate state shape. [Source: `src/Hexalith.Tenants.Server/Projections/TenantProjection.cs`; `src/Hexalith.Tenants.Server/Projections/TenantIndexProjection.cs`; `src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs`; `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`]
- `TenantsProjectionActor` is query-side only. It inherits `CachingProjectionActor` and uses `IETagService` for cache invalidation, not state-store write concurrency. Do not confuse query cache ETags with Dapr state-store ETags for projection persistence. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`; `Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/CachingProjectionActor.cs`; `Hexalith.EventStore/src/Hexalith.EventStore.Server/Queries/IETagService.cs`]
- `Program.cs` registers `IETagService` only because `TenantsProjectionActor` inherits EventStore query caching. This story should not require hosting EventStore's aggregate or ETag actors inside the Tenants service. [Source: `src/Hexalith.Tenants/Program.cs`]

### Implementation Guardrails

- Prefer a small helper next to `TenantProjectionHandler`, for example an internal projection persistence helper in `src/Hexalith.Tenants/Projections/`, so write-safety code is testable without expanding public surface area.
- Keep helper inputs explicit: state store name, state key, max attempts, load function/default factory, merge/apply function, and optional correlation/log context. Avoid a generic framework that hides which model is being merged.
- For per-tenant `TenantReadModel`, a full-replay `ProjectionRequest` can rebuild from the supplied events; if the current persisted state is loaded during retry, be careful not to double-apply the same full history. If the EventStore projection contract supplies complete aggregate history, replacing with the deterministic rebuilt model under an ETag guard is acceptable; if it supplies deltas, reload-and-merge is required. Verify the contract in tests around `ProjectionRequest.Events`.
- For `TenantIndexReadModel`, load the current model and apply only the incoming events before each guarded save attempt, because the singleton state accumulates events from all tenant aggregates.
- Use source-generated logging if adding new structured log methods near existing patterns. Include machine-useful fields and avoid payload data.
- Leave cancellation-token propagation untouched unless the existing method signatures already expose a token. Story 10.3A/10.3B own projection cancellation API changes.
- Do not change `TenantReadModel`, `TenantIndexReadModel`, event contracts, route contracts, or query DTOs to solve persistence concurrency.
- If Dapr returns `false` from `TrySaveStateAsync`/`TrySaveAsync`, treat it as a conflict. If Dapr throws for state-store failures, let the operation fail after logging as appropriate; do not convert infrastructure failures into successful projection responses.

### Files Likely To Update

- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`: replace plain per-tenant and singleton-index saves with ETag-aware guarded writes and bounded retry behavior.
- `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs` or a similarly named internal helper: optional location for retry constants, `StateOptions`, and reusable guarded-save logic.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs`: extend existing tests around `ProjectAsync` to cover ETag conflict, retry success, retry exhaustion, and guarded save options.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantIndexProjectionTests.cs`: likely no direct update unless shared helper tests need pure projection assertions; keep model tests focused.
- `tests/Hexalith.Tenants.Server.Tests/Telemetry/` or projection test helpers: optional only if new structured log verification is already practical nearby.

### Testing Requirements

- Use xUnit v3 and Shouldly, matching the repository.
- Use NSubstitute or a focused fake for `DaprClient` behavior. If mocking `DaprClient` ETag methods is brittle, introduce a tiny internal adapter around Dapr state operations and test the projection handler against that adapter.
- Test a conflict sequence for `projection:tenant-index:singleton`:
  - first read returns model A with ETag `e1`;
  - first guarded save returns `false`;
  - second read returns model B with ETag `e2`;
  - second guarded save returns `true`;
  - saved model contains model B plus the incoming events.
- Test retry exhaustion with max attempts reached and assert `ProjectAsync` throws or returns a failure according to the selected implementation. Do not accept silent success.
- Test per-tenant write protection enough to prove `ConcurrencyMode.FirstWrite` and ETag are used.
- Run at minimum:
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantProjectionHandlerTests`
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantIndexProjectionTests`
- If a helper or adapter changes public/internal accessibility across assemblies, also run:
  - `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore`

### Latest Technical Information

- The repository currently pins Dapr `1.17.9`, Aspire `13.3.3`, Microsoft ASP.NET Core packages `10.0.8`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and NSubstitute `6.0.0-rc.1`. This story needs no dependency update. [Source: `Directory.Packages.props`]
- Dapr Client 1.17.9 XML docs state `ConcurrencyMode.FirstWrite` handles state operations in a first-write-wins fashion, and `TrySaveStateAsync` / `StateEntry<T>.TrySaveAsync` only save when the attached ETag matches the latest ETag in the state store. [Source: `%USERPROFILE%/.nuget/packages/dapr.client/1.17.9/lib/net10.0/Dapr.Client.xml`]

### Previous Story Intelligence

- Story 9.1 introduced signed query cursors and query cache safety; do not touch cursor or query cache behavior for this projection write story. [Source: `_bmad-output/implementation-artifacts/9-1-opaque-signed-query-cursors.md`; `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`]
- Story 9.2 and 9.5 keep query pagination deterministic and centralized. This story must not alter cursor ordering, page bounds, or query response shapes while fixing write persistence. [Source: `_bmad-output/implementation-artifacts/9-2-stable-cursor-pagination-under-role-and-membership-changes.md`; `_bmad-output/implementation-artifacts/9-5-shared-pagination-bounds-and-cursor-utilities.md`]
- Story 9.3 and 9.4 harden query visibility and actor guardrails. Projection write retries must preserve those policies by updating read models correctly, not by adding query-side filters in this story. [Source: `_bmad-output/implementation-artifacts/9-3-query-policy-for-disabled-tenants-and-orphan-memberships.md`; `_bmad-output/implementation-artifacts/9-4-actor-layer-query-guardrails.md`]

### Git Intelligence

- Recent commits created ready stories 9.3, 9.4, and 9.5 and updated Aspire package versions. No recent commit implemented ETag-aware `TenantProjectionHandler` writes, so Story 10.1 remains a valid next backlog story. [Source: `git log -5 --oneline`; `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`]

### Project Context Reference

- Follow `AGENTS.md`: do not initialize or update nested submodules recursively, and use Conventional Commits for commits.
- Follow repository C# conventions: nullable-safe code, file-scoped namespaces, central package management, no inline package versions, source-generated logging for structured logs, xUnit and Shouldly for tests.
- No root `project-context.md` exists for this application repository; submodule project-context files are reference context only. Relevant EventStore context: query cache ETags are separate from projection persistence ETags, and EventStore package changes must not be made from this Tenants story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

### File List
