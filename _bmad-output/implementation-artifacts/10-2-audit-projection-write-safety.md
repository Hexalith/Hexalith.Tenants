# Story 10.2: Audit Projection Write Safety

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a global administrator,
I want tenant audit projection writes to preserve every access-change event,
so that audit reports remain complete even when many tenant membership changes are processed at the same time.

## Acceptance Criteria

1. Given multiple access-change events are applied to `audit:{tenantId}` concurrently, when `TenantAuditProjection` persists audit read-model state, then the write path prevents silent last-writer-wins loss of audit entries.
2. Given audit events for the same tenant arrive close together, when the projection updates the audit timeline, then each event remains queryable by date range and pagination cursor after processing completes.
3. Given a concurrency conflict occurs while saving audit state, when the projection retries or reloads state, then the final audit read model preserves all events that were successfully processed.
4. Given audit write safety cannot be guaranteed after retry exhaustion, when the projection reports failure, then the failure is observable and does not falsely mark the projection update as complete.
5. Given focused audit projection tests run, when concurrent add/remove/change-role events are projected, then tests verify that audit entry count and ordering remain correct.

## Tasks / Subtasks

- [ ] Move audit persistence from plain save to guarded ETag-aware writes. (AC: 1, 3, 4)
  - [ ] Update the `audit:{tenantId}` persistence path in `TenantProjectionHandler.ProjectAsync`.
  - [ ] Reuse the Story 10.1 internal projection write helper or adapter if it exists; otherwise create the narrow helper needed for audit writes without introducing a public package abstraction.
  - [ ] Use Dapr Client state ETag APIs pinned by the repository, not a custom concurrency token format.
  - [ ] Keep state store name `statestore` and audit key prefix `audit:` unchanged.
- [ ] Preserve audit projection semantics while adding retry behavior. (AC: 1, 2, 3, 5)
  - [ ] Keep `TenantAuditProjection.ProjectAuditEvents(...)` and `TenantAuditReadModel.Apply(...)` as the single audit entry construction rules.
  - [ ] Keep malformed JSON payload handling unchanged: malformed payloads are skipped, while missing `MessageId` or `UserId` invariant violations still propagate.
  - [ ] Preserve stable audit ordering by `Timestamp` and then `EventId`.
  - [ ] Do not change `TenantAuditEntry`, `GetTenantAuditQuery`, query routes, cursor encoding, authorization, or response DTOs for this story.
- [ ] Define conflict retry semantics for audit state. (AC: 1, 3, 4)
  - [ ] On conflict, reload current audit state, rebuild or merge the incoming audit entries exactly once for that attempt, sort entries, and retry the guarded save.
  - [ ] If the EventStore projection request provides full aggregate history, avoid duplicating audit entries already present in the reloaded audit state; identify entries by `EventId`.
  - [ ] If the projection request provides a delta batch, merge incoming entries into the reloaded audit state without dropping or double-counting existing entries.
  - [ ] Use a bounded retry policy aligned with Story 10.1 unless implementation proves audit needs a narrower limit and documents why.
  - [ ] On retry exhaustion, fail the projection operation through the existing failure path and emit safe structured logs.
- [ ] Add focused deterministic tests for audit write safety. (AC: 1-5)
  - [ ] Extend `TenantProjectionHandlerTests` or add audit-specific projection handler tests using a deterministic fake/adapter for Dapr state reads, ETags, and save outcomes.
  - [ ] Simulate first-save conflict and second-save success for `audit:{tenantId}`, proving the handler reloads state before merging incoming audit entries.
  - [ ] Verify the final saved audit model contains previously persisted entries plus the incoming access-change events exactly once.
  - [ ] Verify ordering remains deterministic by timestamp and event id after retry.
  - [ ] Verify retry exhaustion does not return a successful `ProjectionResponse`.
  - [ ] Keep `TenantAuditProjectionTests` and `TenantAuditReadModelTests` focused on pure audit classification/sorting behavior unless small assertions are needed to support merge tests.
- [ ] Keep scope boundaries explicit. (AC: 1-5)
  - [ ] Do not modify the `Hexalith.EventStore` submodule.
  - [ ] Do not add package dependencies or package versions.
  - [ ] Do not change query actor cache ETags, signed cursor behavior, page bounds, or tenant visibility policy.
  - [ ] Leave Story 10.3A/10.3B cancellation-token threading and Story 10.4 reusable conformance coverage for their dedicated stories.

## Dev Notes

### Policy To Implement

- The current audit write path uses plain `DaprClient.SaveStateAsync` for `audit:{request.AggregateId}`. Plain saves can overwrite a newer audit timeline when concurrent projection deliveries update the same tenant audit key. [Source: `_bmad-output/planning-artifacts/epics.md#Story 10.2`; `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`]
- Dapr Client is pinned to 1.17.9 and provides ETag-aware state APIs. Use `GetStateAndETagAsync<TValue>`, `TrySaveStateAsync<TValue>(..., etag, ...)` or `StateEntry<TValue>.TrySaveAsync(...)`, plus first-write options for missing-state paths. [Source: `Directory.Packages.props`; Story 10.1 dev notes]
- Audit retry must be reload-and-merge, not retry-the-same-stale-instance. The reloaded audit model may already contain entries persisted by another writer, and the incoming entries must be added exactly once. [Source: `_bmad-output/planning-artifacts/epics.md#Story 10.2`; `_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md`]
- Retry exhaustion must be observable and must not return a successful projection update. Use safe structured fields such as state store, key category, attempt count, max attempts, operation context, and conflict/exhaustion reason; do not log tenant payloads, user-controllable names, configuration values, cursor payloads, or event payload bodies. [Source: `_bmad-output/planning-artifacts/prd.md#FR54-FR58`; `Hexalith.EventStore/_bmad-output/project-context.md#Code Quality & Style Rules`]

### Current Code State

- `TenantProjectionHandler.ProjectAsync` currently builds `TenantReadModel`, saves `projection:tenants:{aggregateId}`, builds `TenantAuditReadModel`, saves `audit:{aggregateId}`, loads/updates `projection:tenant-index:singleton`, and returns a `ProjectionResponse`. [Source: `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`]
- `TenantAuditProjection` is a static helper, not an EventStore-discoverable projection. It builds a fresh `TenantAuditReadModel`, applies non-null events, skips malformed JSON payloads, sorts entries, and returns the model. Keep this shape unless implementation proves the merge helper needs a small pure function. [Source: `src/Hexalith.Tenants.Server/Projections/TenantAuditProjection.cs`; `tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditProjectionTests.cs`]
- `TenantAuditReadModel.Apply(...)` creates entries for access and administrative events. Access events include `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `GlobalAdministratorSet`, and `GlobalAdministratorRemoved`. It requires `MessageId` and `UserId`; those invariants are upstream contract failures and should still fail. [Source: `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`; `tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditReadModelTests.cs`]
- `TenantAuditReadModel.SortEntries()` orders by `Timestamp` then `EventId`. Audit merge logic must call it after combining existing and incoming entries so date-range and cursor queries see deterministic ordering. [Source: `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`]
- `TenantsProjectionActor` and `TenantsQueryController` own query-time filtering, authorization, pagination, and cursor behavior. This story fixes write safety only and should not alter query response semantics. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`; `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`]

### Implementation Guardrails

- Prefer one shared internal write policy/helper for Story 10.1 and 10.2 if Story 10.1 has already introduced it. If Story 10.1 has not landed, create the smallest audit-friendly internal adapter around Dapr ETag reads/saves in `src/Hexalith.Tenants/Projections/`.
- Keep helper inputs explicit: state store name, state key, key category, max attempts, model factory, merge function, and safe log context. Avoid a broad Dapr state framework.
- Use `EventId` as the audit entry idempotency key when merging reloaded and incoming audit entries. If a duplicate `EventId` is already present after a reload, preserve the persisted entry and do not append a second copy.
- Preserve full-history projection behavior. If `ProjectionRequest.Events` represents full aggregate history, rebuilding incoming audit entries from the request can produce entries already in persisted audit state; the merge must still remain idempotent by `EventId`.
- Keep malformed payload behavior consistent with `TenantAuditProjection.ProjectAuditEvents(...)`: malformed JSON payloads do not poison rebuild, but metadata invariant violations still fail.
- Do not add event contract fields, query contract fields, or new public APIs to solve persistence concurrency.
- Do not rely on real parallelism, sleeps, live DAPR, Redis, or Aspire for the focused tests.

### Files Likely To Update

- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`: replace plain `audit:{tenantId}` save with guarded ETag retry behavior.
- `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs` or the Story 10.1 helper file: reuse or extend internal write helper logic if present.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs`: add audit ETag conflict, retry success, retry exhaustion, and final entry ordering/count assertions.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditProjectionTests.cs`: update only if a new pure merge helper is added beside the projection.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditReadModelTests.cs`: update only if entry idempotency helpers move into the read model.

### Testing Requirements

- Use xUnit v3, Shouldly, and NSubstitute or a focused fake, matching existing tests.
- If direct `DaprClient` mocking is brittle for ETag APIs, introduce a tiny internal projection state adapter and test the handler through that adapter.
- Script deterministic state-store sequences:
  - first read returns existing audit state and ETag `e1`;
  - first guarded save returns `false`;
  - second read returns a newer audit state and ETag `e2`;
  - second guarded save returns `true`;
  - saved model contains all expected entries exactly once and sorted.
- Include an access-event mix covering `UserAddedToTenant`, `UserRemovedFromTenant`, and `UserRoleChanged`.
- Include a retry-exhaustion test that asserts the projection does not report success.
- Run at minimum:
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantProjectionHandlerTests`
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantAudit`
- If the helper affects shared projection code, also run:
  - `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore`

### Latest Technical Information

- The repository currently pins Dapr `1.17.9`, Aspire `13.3.3`, Microsoft ASP.NET Core packages `10.0.8`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and NSubstitute `6.0.0-rc.1`. This story should not update dependencies. [Source: `Directory.Packages.props`]
- Dapr's ETag write APIs are the relevant current mechanism for guarded state-store persistence. Use the pinned package's XML/API surface and Story 10.1 helper guidance before inventing another persistence contract. [Source: `Directory.Packages.props`; `_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md`]

### Previous Story Intelligence

- Story 10.1 established the tenant read-model and singleton-index concurrency policy. Story 10.2 should align with the same bounded retry and safe logging patterns while keeping audit-specific merge/idempotency rules explicit. [Source: `_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md`]
- Story 9.5 centralized pagination bounds and cursor utility guidance. Do not change audit cursor ordering, page-size handling, or query response shape while fixing audit write persistence. [Source: `_bmad-output/implementation-artifacts/9-5-shared-pagination-bounds-and-cursor-utilities.md`]
- Story 9.3 and 9.4 hardened query visibility and actor guardrails. This story must preserve those policies by storing complete audit state, not by changing query-side filters or actor error behavior. [Source: `_bmad-output/implementation-artifacts/9-3-query-policy-for-disabled-tenants-and-orphan-memberships.md`; `_bmad-output/implementation-artifacts/9-4-actor-layer-query-guardrails.md`]

### Git Intelligence

- Recent commits recorded the 10.1 party-mode review and release 1.8.0, but no commit has yet created the 10.2 story artifact. [Source: `git log -5 --oneline`; `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- The active worktree had Story 9.2 moving out of `ready-for-dev` plus source/test changes when this story was created. Treat those as active implementation work outside this pre-dev story operation. [Source: `_bmad-output/process-notes/predev-preflight-latest.json` timestamp `2026-05-17T10:47:22Z`]

### Project Context Reference

- Follow `AGENTS.md`: do not initialize or update nested submodules recursively, and use Conventional Commits for commits.
- Follow repository C# conventions: nullable-safe code, file-scoped namespaces, central package management, no inline package versions, source-generated logging for structured logs, xUnit and Shouldly for tests.
- No root `project-context.md` exists for this application repository; submodule project-context files are reference context only. Relevant EventStore context: query cache ETags are separate from projection persistence ETags, and EventStore package/submodule changes must not be made from this Tenants story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

### File List
