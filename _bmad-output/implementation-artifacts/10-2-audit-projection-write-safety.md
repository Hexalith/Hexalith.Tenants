# Story 10.2: Audit Projection Write Safety

Status: done

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a global administrator,
I want tenant audit projection writes to avoid losing or overwriting distinct committed access-change events,
so that audit reports remain complete even when many tenant membership changes are processed at the same time.

## Acceptance Criteria

1. Given multiple committed access-change events are applied to `audit:{tenantId}` concurrently, when `TenantAuditProjection` persists audit read-model state, then the write path uses guarded ETag-aware persistence and prevents silent last-writer-wins loss of distinct audit entries.
2. Given audit events for the same tenant arrive close together, when the projection updates the audit timeline, then persisted audit records remain queryable by the existing date-range and pagination-cursor behavior after processing completes, with no query contract, cursor, route, DTO, or authorization changes.
3. Given an ETag conflict occurs while saving audit state, when the projection retries, then it reloads the latest persisted audit state and ETag, merges the same incoming audit entries idempotently by `EventId`, sorts the merged timeline, and retries with the fresh ETag instead of re-saving a stale instance.
4. Given audit write safety cannot be guaranteed after the Story 10.1-aligned maximum of 3 attempts, when the projection reports failure, then the failure is observable and does not falsely mark the projection update as complete.
5. Given focused audit projection tests run, when concurrent add/remove/change-role events are projected, then tests verify exact audit entry membership, de-duplication by `EventId`, and deterministic ordering by `Timestamp` then `EventId`.
6. Given full-history or delta projection requests replay audit events already present in the reloaded audit state, when merge logic builds the final audit model, then duplicate `EventId` values are collapsed to one audit record while distinct events with the same timestamp are preserved.
7. Given malformed JSON payloads appear in the incoming audit event batch, when retry/reload behavior is exercised, then malformed payloads are still skipped exactly as today while valid audit events are preserved.
8. Given missing `MessageId` or `UserId` invariant failures occur while constructing audit entries, when guarded persistence is introduced, then those invariant failures still fail through the existing projection failure path and are not masked as successful writes or ordinary ETag conflicts.
9. Given retry exhaustion is logged or traced, when operators inspect the failure, then safe structured context includes state store, audit key category, tenant ID or aggregate ID when allowed by existing logging policy, audit event ID/type when available, attempt count, max attempts, conflict/exhaustion reason, and correlation or trace ID when available, without logging event payload bodies, tenant names, membership details, cursor payloads, configuration values, or user-controllable display names.
10. Given an audit guarded save succeeds but a later required projection write fails, when `ProjectAsync` returns or throws, then it still reports the overall projection failure and does not imply cross-key atomicity; if the same audit events are replayed later, `EventId` idempotency prevents duplicate audit records.
11. Given the reloaded audit state already contains an entry with the same `EventId` as an incoming audit entry but different audit details, when merge logic runs, then the persisted entry is treated as authoritative for that `EventId`, no overwrite occurs, and any diagnostic context remains payload-free.
12. Given an incoming batch contains valid audit events followed by a missing `MessageId` or `UserId` invariant failure, when guarded persistence is introduced, then incoming audit entries are constructed or validated before any guarded audit save is attempted so no partial audit timeline update is committed for that failed batch.

## Tasks / Subtasks

- [x] Move audit persistence from plain save to guarded ETag-aware writes. (AC: 1, 3, 4)
  - [x] Update the `audit:{tenantId}` persistence path in `TenantProjectionHandler.ProjectAsync`.
  - [x] Reuse the Story 10.1 internal projection write helper or adapter if it exists; otherwise create the narrow helper needed for audit writes without introducing a public package abstraction.
  - [x] Use Dapr Client state ETag APIs pinned by the repository, not a custom concurrency token format.
  - [x] Use `GetStateAndETagAsync<TenantAuditReadModel>` plus `TrySaveStateAsync<TenantAuditReadModel>(..., etag, ...)` or `StateEntry<TenantAuditReadModel>.TrySaveAsync(...)` for existing audit state.
  - [x] Use first-write semantics only for the missing-state/no-ETag path; existing audit state writes must use the loaded ETag.
  - [x] Keep state store name `statestore` and audit key prefix `audit:` unchanged.
- [x] Preserve audit projection semantics while adding retry behavior. (AC: 1, 2, 3, 5)
  - [x] Keep `TenantAuditProjection.ProjectAuditEvents(...)` and `TenantAuditReadModel.Apply(...)` as the single audit entry construction rules.
  - [x] Keep malformed JSON payload handling unchanged: malformed payloads are skipped, while missing `MessageId` or `UserId` invariant violations still propagate.
  - [x] Construct or validate the incoming audit entries before attempting a guarded save so invariant failures cannot leave a partially merged audit model in state.
  - [x] Preserve stable audit ordering by `Timestamp` and then `EventId`.
  - [x] Do not change `TenantAuditEntry`, `GetTenantAuditQuery`, query routes, cursor encoding, authorization, or response DTOs for this story.
- [x] Define conflict retry semantics for audit state. (AC: 1, 3, 4)
  - [x] On conflict, discard the stale mutated audit instance, reload current audit state and ETag, rebuild or merge the incoming audit entries exactly once for that attempt, sort entries, and retry the guarded save.
  - [x] If the EventStore projection request provides full aggregate history, avoid duplicating audit entries already present in the reloaded audit state; identify entries by `EventId`.
  - [x] If the projection request provides a delta batch, merge incoming entries into the reloaded audit state without dropping or double-counting existing entries.
  - [x] If a duplicate `EventId` already exists in persisted audit state, preserve the persisted entry and do not overwrite it with incoming audit details from a replayed or conflicting batch.
  - [x] Use max 3 attempts, matching Story 10.1, unless implementation proves audit needs a narrower limit and documents why in code/tests.
  - [x] On retry exhaustion, fail the projection operation through the existing failure path and emit safe structured logs.
  - [x] If audit persistence succeeds but a later required projection write fails, keep the overall projection operation failed and rely on audit `EventId` idempotency for replay rather than claiming cross-key transactionality.
  - [x] Preserve enough safe failure context for existing replay/retry diagnostics; do not add new recovery commands or admin repair tooling.
- [x] Add focused deterministic tests for audit write safety. (AC: 1-5)
  - [x] Extend `TenantProjectionHandlerTests` or add audit-specific projection handler tests using a deterministic fake/adapter for Dapr state reads, ETags, and save outcomes.
  - [x] Simulate first-save conflict and second-save success for `audit:{tenantId}`, where the reload contains an externally persisted audit event not present in the original read, proving the handler reloads state before merging incoming audit entries.
  - [x] Verify the final saved audit model contains original persisted entries, externally persisted reload entries, and incoming access-change events exactly once.
  - [x] Verify duplicate `EventId` values across stale model, reloaded model, and incoming batch collapse to one audit entry.
  - [x] Verify ordering remains deterministic by timestamp and event id after retry, including same-timestamp events with different IDs.
  - [x] Verify read/save attempt counts for conflict-then-success and retry-exhaustion sequences.
  - [x] Verify retry exhaustion does not return a successful `ProjectionResponse` or otherwise report a saved projection update.
  - [x] Verify malformed incoming payloads remain skipped during conflict/retry while valid incoming events are preserved.
  - [x] Verify invariant failures such as missing `MessageId` or `UserId` are not converted into successful partial writes, attempted guarded saves, or ordinary ETag conflicts.
  - [x] Verify a duplicate `EventId` with different incoming audit details preserves the persisted audit entry and does not overwrite it.
  - [x] Verify a replay after an earlier audit save plus later projection failure does not duplicate audit entries.
  - [x] Add a focused regression that audit records preserved through conflict recovery remain visible through existing date-range and pagination-cursor query behavior.
  - [x] Keep `TenantAuditProjectionTests` and `TenantAuditReadModelTests` focused on pure audit classification/sorting behavior unless small assertions are needed to support merge tests.
- [x] Keep scope boundaries explicit. (AC: 1-5)
  - [x] Do not modify the `Hexalith.EventStore` submodule.
  - [x] Do not add package dependencies or package versions.
  - [x] Do not change query actor cache ETags, signed cursor behavior, page bounds, or tenant visibility policy.
  - [x] Do not add distributed locks, queue redesign, schema migrations, new admin UI, recovery commands, diagnostic query endpoints, or EventStore behavior changes.
  - [x] Do not claim or implement cross-key transactionality between tenant read-model, audit, and singleton-index state entries.
  - [x] Leave Story 10.3A/10.3B cancellation-token threading and Story 10.4 reusable conformance coverage for their dedicated stories.

## Dev Notes

### Policy To Implement

- The current audit write path uses plain `DaprClient.SaveStateAsync` for `audit:{request.AggregateId}`. Plain saves can overwrite a newer audit timeline when concurrent projection deliveries update the same tenant audit key. [Source: `_bmad-output/planning-artifacts/epics.md#Story 10.2`; `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`]
- Dapr Client is pinned to 1.17.9 and provides ETag-aware state APIs. Use `GetStateAndETagAsync<TValue>`, `TrySaveStateAsync<TValue>(..., etag, ...)` or `StateEntry<TValue>.TrySaveAsync(...)`, plus first-write options for missing-state paths. [Source: `Directory.Packages.props`; Story 10.1 dev notes]
- Audit retry must be reload-and-merge, not retry-the-same-stale-instance. The reloaded audit model may already contain entries persisted by another writer, and the incoming entries must be added exactly once. [Source: `_bmad-output/planning-artifacts/epics.md#Story 10.2`; `_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md`]
- Use max 3 attempts for audit guarded writes to match Story 10.1. If implementation proves a narrower audit-specific limit is needed, document that in code and focused tests rather than leaving the behavior implicit.
- Audit merge idempotency is based on `TenantAuditEntry.EventId`. If a duplicate `EventId` is already present after reload, preserve the persisted entry and do not append a second copy. Distinct events with the same timestamp must all remain present and sorted by `Timestamp` then `EventId`.
- Treat persisted audit entries as authoritative for duplicate `EventId` collisions, even if a replayed incoming entry would deserialize to different audit details. Do not overwrite persisted audit details during merge; safe diagnostics may identify duplicate/conflict categories without logging payload bodies or membership details.
- Build or validate the incoming audit model before attempting a guarded audit save. Malformed payloads may still be skipped according to existing `TenantAuditProjection` behavior, but `MessageId`/`UserId` invariant failures must abort before any partial merged audit state is saved.
- Audit write safety does not create cross-key atomicity. If audit state is saved and a later required tenant read-model or singleton-index write fails, `ProjectAsync` must still fail through the existing projection failure path; replayed audit entries must remain idempotent by `EventId`.
- Retry exhaustion should fail through the existing `TenantProjectionHandler.ProjectAsync` failure contract. Exact throw-versus-failed-result mechanics remain an implementation decision, but silent success is not acceptable.
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
- Keep retry and concurrency tests deterministic through scripted fake state outcomes by operation count, ETag, or key version. Do not rely on scheduler timing to produce a conflict.
- Queryability checks must use existing audit query behavior only; this story must not create new public surfaces to prove records are present.
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
  - second read returns a newer audit state and ETag `e2`, including at least one event added by another writer after the original read;
  - second guarded save returns `true`;
  - saved model contains original persisted entries, externally added entries, and incoming entries exactly once and sorted.
- Include an access-event mix covering `UserAddedToTenant`, `UserRemovedFromTenant`, and `UserRoleChanged`.
- Include a mixed-batch case where one incoming event is already persisted and two are new.
- Include same-timestamp events with different IDs to prove ordering by `Timestamp` then `EventId`.
- Include a malformed incoming payload in a conflict/retry sequence and assert it remains skipped.
- Include an invariant-failure case for missing `MessageId` or `UserId` and assert it fails through the existing projection failure path before any guarded audit save is attempted.
- Include a duplicate `EventId` conflict where the persisted entry differs from the incoming replayed entry, and assert the persisted entry remains authoritative.
- Include a replay scenario after audit persistence succeeded but a later projection write failed, and assert replay does not duplicate audit records.
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

- `dotnet test tests\Hexalith.Tenants.Server.Tests\Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantProjectionHandlerTests` - passed, 14/14.
- `dotnet test tests\Hexalith.Tenants.Server.Tests\Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantAudit` - passed, 38/38.
- `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore` - passed, 0 warnings, 0 errors.
- `dotnet test tests\Hexalith.Tenants.Server.Tests\Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore` - passed on rerun, 388/388.
- `dotnet test Hexalith.Tenants.slnx --configuration Debug --no-restore` - passed, 606 passed, 5 skipped.

### Completion Notes List

- Implemented guarded audit read-model persistence using the Story 10.1 ETag-aware state-store adapter and `TrySaveStateAsync` retry policy.
- Added audit-specific reload-and-merge behavior that prebuilds incoming audit entries, preserves persisted entries by `EventId`, collapses duplicates, sorts by `Timestamp` then `EventId`, and retries with the fresh ETag.
- Preserved existing audit construction rules and malformed JSON skip behavior while keeping missing `MessageId`/`UserId` invariant failures on the existing failure path before any guarded audit save is attempted.
- Verified retry exhaustion, conflict reloads with externally persisted entries, duplicate collision behavior, replay after later projection failure, and existing audit date-range/cursor queryability.

### File List

- `_bmad-output/implementation-artifacts/10-2-audit-projection-write-safety.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionDispatcherTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`

### Change Log

- 2026-05-18: Implemented audit projection guarded ETag persistence with idempotent reload-and-merge retry behavior; added deterministic projection handler and actor query regressions; moved story to review.

## Party-Mode Review

- Date: 2026-05-17T13:04:43+02:00
- Selected story key: 10-2-audit-projection-write-safety
- Command/skill invocation used: `/bmad-party-mode 10-2-audit-projection-write-safety; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect and Quality Advisor), John (Product Manager)
- Findings summary:
  - The story needed a tighter product promise: audit projection writes should preserve distinct committed access-change events under projection-write concurrency, not guarantee every infrastructure failure mode.
  - Reviewers identified ETag boundary behavior and reload-and-merge retry semantics as the main architecture risk. Retrying a stale audit instance would still allow lost or duplicated audit entries.
  - Audit merge behavior needed explicit idempotency by `EventId`, preservation of distinct same-timestamp events, and sorting by `Timestamp` then `EventId` after conflict recovery.
  - Test guidance needed to cover conflict reloads that include externally persisted events, duplicate event IDs across stale/reloaded/incoming state, malformed payload preservation, invariant failure boundaries, retry exhaustion, and date/cursor queryability through existing behavior.
  - Scope boundaries needed to exclude public query/API changes, distributed locks, queue redesign, schema migrations, new recovery tooling, package changes, and submodule changes.
- Changes applied:
  - Reworded the story and acceptance criteria around distinct committed access-change events and existing queryability.
  - Added acceptance criteria for guarded ETag persistence, reload-and-merge retry behavior, 3-attempt retry alignment with Story 10.1, idempotent audit merge by `EventId`, malformed payload preservation, invariant failure behavior, and safe retry-exhaustion observability.
  - Expanded tasks and testing requirements for first-write versus existing-state ETag paths, deterministic fake state-store sequences, externally added reload entries, exact read/save attempt counts, duplicate collapse, same-timestamp ordering, malformed payloads, invariant failures, and existing date/cursor query regressions.
  - Added explicit non-goals for query/API/cursor/authorization changes, distributed locks, queue redesign, schema migrations, admin UI, recovery commands, EventStore behavior, package dependencies, and submodule changes.
- Findings deferred:
  - Exact throw-versus-failed-result mechanics for retry exhaustion should follow the existing `TenantProjectionHandler.ProjectAsync` contract once implementation confirms the method shape.
  - Exact logging event names, levels, and optional metrics/traces remain implementation decisions within existing observability patterns.
  - Any automatic projection repair, manual recovery command, diagnostic query endpoint, distributed locking, queue redesign, schema migration, or EventStore change remains out of scope.
- Final recommendation: needs-story-update

## Advanced Elicitation

- Date: 2026-05-17T17:03:41+02:00
- Selected story key: `10-2-audit-projection-write-safety`
- Command/skill invocation used: `/bmad-advanced-elicitation 10-2-audit-projection-write-safety`
- Batch 1 method names: Red Team vs Blue Team; Failure Mode Analysis; Architecture Decision Records; Code Review Gauntlet; Self-Consistency Validation.
- Reshuffled Batch 2 method names: Pre-mortem Analysis; First Principles Analysis; Comparative Analysis Matrix; Security Audit Personas; Occam's Razor Application.
- Findings summary:
  - The story already captured guarded ETag retry and idempotent merge behavior, but could still be implemented as if audit persistence made the whole projection operation atomic.
  - A replay after an audit save followed by a later projection failure needed explicit idempotency expectations so duplicate audit records are not introduced.
  - Duplicate `EventId` collisions with conflicting details needed a deterministic rule that persisted audit entries remain authoritative.
  - Invariant failures after valid audit events needed a stronger no-partial-save boundary before guarded persistence is attempted.
- Changes applied:
  - Added acceptance criteria for cross-key partial-success failure semantics, duplicate `EventId` conflict handling, and invariant-failure no-save behavior.
  - Tightened retry tasks to preserve persisted entries for duplicate `EventId` values and avoid claiming cross-key transactionality.
  - Expanded deterministic test guidance for no guarded save on invariant failures, persisted-entry-authoritative duplicate conflicts, and replay after later projection failure.
  - Added dev-note guidance to build or validate incoming audit entries before saving and to keep diagnostics payload-free.
- Findings deferred:
  - Exact throw-versus-failed-result mechanics still belong to the existing `TenantProjectionHandler.ProjectAsync` contract.
  - Any transactional outbox, multi-key transaction, compensating write, automatic repair command, or diagnostic query endpoint remains out of scope.
  - Exact structured logging event names, levels, and optional metric dimensions remain implementation decisions inside existing observability conventions.
- Final recommendation: ready-for-dev

## Review Findings

- Date: 2026-05-18
- Reviewer: `/bmad-code-review 10.2`
- Diff scope: `git diff HEAD` over the story File List (5 files, +425/-28)
- Layers run: Blind Hunter, Edge Case Hunter, Acceptance Auditor
- Totals: 6 patches, 0 decision-needed (1 resolved → patch), 4 deferred, 18 dismissed

### Patches

- [x] [Review][Patch] (was D1) Reorder `ProjectAsync` to build and validate `incomingAuditModel` via `TenantAuditProjection.ProjectAuditEvents` BEFORE the tenant `TrySaveStateAsync` — today the tenant read-model commits before audit invariant validation, so an event with missing `MessageId`/`UserId` leaves tenant updated while audit/index stay stale. AC12 calls for incoming audit entries to be constructed before any guarded audit save; the same fail-fast principle should keep the tenant save from preceding audit validation. Tighten `ProjectAsync_AuditInvariantFailureDoesNotAttemptGuardedAuditSaveAsync` to also assert the tenant key was never written. Sources: Blind B6, EdgeCase E5. [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:55-103, tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs:507-528]
- [x] [Review][Patch] `MergeAuditState` mutates the reloaded `persisted` instance in place — `persisted.Entries.Add(entry)` and `persisted.SortEntries()` modify the just-loaded model and the return value `_ =`-discards the post-merge view. Safe for the current Dapr store (deserializes fresh per call), but fragile against any caching/in-memory state-store implementation and obscures intent. Build and return a new `TenantAuditReadModel` instead. Sources: Blind B1, EdgeCase E9, Auditor A2. [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:111-128]
- [x] [Review][Patch] `ProjectAsync_AuditDuplicateEventIdKeepsPersistedEntryAuthoritativeAsync` asserts `saved.Entries.Single().ShouldBe(persisted)` — passes by reference equality because `MergeAuditState` returns the mutated persisted instance. Cannot detect a regression where merge overwrites with a semantically-different incoming entry that compares equal. Assert by explicit fields (`EventId`, `NarrativePayload`, `Category`, `ActorId`) sourced from the persisted entry, not via `ShouldBe` reference. Source: Blind B5. [tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs:553]
- [x] [Review][Patch] `MergeAuditState` builds `seenEventIds` via `persisted.Entries.Select(e => e.EventId).ToHashSet(StringComparer.Ordinal)` — a null `EventId` (which System.Text.Json can round-trip into a non-nullable `string` property under default options) throws `ArgumentNullException` and permanently wedges the projection for that tenant. Filter null/whitespace `EventId` before building the set, or reject malformed persisted rows up front. Source: EdgeCase E1. [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:115-118]
- [x] [Review][Patch] `ProjectionDispatcherTests` audit-save assertion uses `Arg.Any<string>()` for ETag and `Arg.Any<Dapr.Client.StateOptions>()` for options — `Received(1)` passes even if the policy stops sending `ConcurrencyMode.FirstWrite` or stops sending the loaded ETag. Tighten to a specific ETag value and `Arg.Is<StateOptions>(o => o.Concurrency == ConcurrencyMode.FirstWrite)`. Source: Blind B11. [tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionDispatcherTests.cs:293-318]
- [x] [Review][Patch] AC9 lists "audit event ID/type when available" among safe retry-exhaustion log fields, but the `RetryExhausted` source-generated message emits only `correlationId` and `messageIds` — no `EventTypeName`. Thread a bounded distinct event-type list (or first event type) into the structured log fields alongside `MessageIds`. Source: Auditor A1. [src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs:160-172]

### Deferred (pre-existing)

- [x] [Review][Defer] Empty/whitespace ETag returned by Dapr for an existing key wedges the projection under `FirstWrite` concurrency — `read.ETag ?? string.Empty` plus hard-coded `FirstWrite` means an empty ETag is interpreted as create-only-fail-if-exists, exhausting all 3 retries. Pre-existing in Story 10.1's `SaveWithOptimisticConcurrencyAsync`; inherited by the new `SaveMergedWithOptimisticConcurrencyAsync`. Source: EdgeCase E6. [src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs:150,190-193] — deferred, pre-existing
- [x] [Review][Defer] `TenantAuditReadModel` blob grows unboundedly — no archival/pagination of the persisted audit state; eventually hits the Dapr state-store value-size limit (Redis 512 MB; less on other components) and every retry rewrites a large blob. Pre-existing audit-projection design. Source: EdgeCase E15. [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:73-86] — deferred, pre-existing
- [x] [Review][Defer] `ProjectAsync` has no `CancellationToken` parameter; all policy calls receive `default(CancellationToken)`. Pre-existing handler signature; explicitly owned by Story 10-3a (EventStore projection cancellation API) and 10-3b (token threading). Source: EdgeCase E7+E13. [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:55-86] — deferred, pre-existing (covered by 10-3a/10-3b)
- [x] [Review][Defer] `AggregateId` is concatenated directly into state-store keys (`audit:{aggregateId}`, `projection:tenants:{aggregateId}`) without escape/validation. Already deferred from Story 10.1's review (input-contract gap belongs upstream in `ProjectionRequest` validation or the `/project` endpoint, not in the projection helper). Source: Blind B4. [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:78] — deferred, pre-existing
