# Story 9.3: Query Policy for Disabled Tenants and Orphan Memberships

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a platform operator,
I want tenant query endpoints to apply explicit policies for disabled tenants and inconsistent projection entries,
so that query results are predictable, explainable, and do not accidentally expose stale or misleading tenant access.

## Acceptance Criteria

1. Given a tenant has `TenantStatus.Disabled`, when `get-user-tenants` is queried by self, tenant owner, or global administrator, then the response includes the disabled tenant when the caller is otherwise authorized to see the tenant or membership.
2. Given a disabled tenant appears in `get-user-tenants` or related query output, when the response is serialized, then the response clearly includes `TenantStatus.Disabled` so clients can distinguish disabled access from active access and avoid implying command capability.
3. Given a `UserTenants` projection references a tenant missing from the tenant index/read model, when the query response is built, then normal user-facing query responses filter the orphan membership before keyset ordering, page-size selection, and signed cursor generation.
4. Given an orphan membership is filtered from a normal query response, when the anomaly is observed, then the actor emits internal projection-repair telemetry or structured warning logs with correlation metadata and never adds a public DTO field, diagnostic route, or orphan list.
5. Given projection eventual consistency creates a stale self-lookup result after user removal, when the removed user queries their memberships briefly after removal, then the accepted temporary visibility window is documented and covered by tests or explicit notes, and the stale query result does not grant write capability.
6. Given all currently visible memberships on a requested page are filtered as orphans, when pagination completes, then the response is an empty page with no synthesized tenant item and no cursor derived from an orphan entry.

## Tasks / Subtasks

- [ ] Lock the disabled-tenant query policy in actor behavior and tests. (AC: 1, 2)
  - [ ] Preserve the current projection rule that `TenantDisabled` updates `TenantIndexEntry.Status` and does not remove the tenant from `TenantIndexReadModel.Tenants`.
  - [ ] Ensure `get-user-tenants` returns disabled tenants for self lookup, tenant-owner scoped cross-user lookup, and global-admin lookup when the caller is otherwise authorized.
  - [ ] Ensure any related list output that already exposes `TenantSummary` or `UserTenantMembership` serializes the existing `Status` value as `Disabled`.
  - [ ] Do not treat disabled status as authorization failure for queries. Disabled tenants still reject commands at aggregate/domain level; this story must not weaken that command invariant.
- [ ] Filter orphan memberships before creating user-facing query DTOs. (AC: 3)
  - [ ] In `TenantsProjectionActor.HandleGetUserTenantsAsync`, do not construct `UserTenantMembership` from a `UserTenants` entry when `indexModel.Tenants` has no matching `TenantIndexEntry`.
  - [ ] Remove the current fallback behavior that emits `Name = string.Empty` and `Status = TenantStatus.Active` for missing tenant entries.
  - [ ] Apply filtering before keyset ordering, `pageSize + 1` selection, and signed cursor generation so orphan entries do not consume page slots or cursor positions.
  - [ ] Treat cursor continuation against the current filtered candidate set: if the cursor's prior anchor tenant is now orphaned or otherwise filtered out, do not materialize it, expose it, or derive a next cursor from it.
  - [ ] Preserve the existing authorization filter order: self/global-admin visibility and tenant-owner scoped overlap must still be determined before pagination, and cross-user timing-uniformity behavior from R5-A2 must remain intact.
- [ ] Add observable projection-repair warning logs for filtered orphan memberships. (AC: 4)
  - [ ] Add a source-generated `LoggerMessage` warning on `TenantsProjectionActor` or a local logging helper with fields for correlation ID, query type, requester user ID, target user ID, orphan tenant ID, and stage.
  - [ ] Treat repair observability as internal telemetry/logging only unless an existing trace/activity tag convention is already available; do not add response metadata, DTO fields, routes, or a diagnostic orphan view in this story.
  - [ ] Do not log protected cursor payloads, signing material, or full serialized query payloads.
  - [ ] Follow existing repository privacy/logging conventions for identifier values; tests should assert warning category, level, event identity, and correlation linkage without requiring tenant names or serialized query payloads in logs.
  - [ ] Keep the warning informational enough for operators to repair projections, but do not expose the orphan tenant through the response body.
- [ ] Document and test accepted eventual consistency after membership removal. (AC: 5)
  - [ ] Add or update a concise note in `docs/cross-aggregate-timing.md` explaining that tenant query projections are eventually consistent after `UserRemovedFromTenant`; a stale self-lookup may briefly show the old membership until projection catch-up completes.
  - [ ] State explicitly that this query visibility does not grant write capability and does not override aggregate command authorization.
  - [ ] Add focused tests or comments proving the actor cannot infer unprocessed removals from current projection state and therefore returns exactly the current read model after normal authorization filtering.
- [ ] Add focused tests for disabled tenants, orphan filtering, and no-regression query behavior. (AC: 1-5)
  - [ ] Add actor tests in `TenantsProjectionActorTests` for disabled tenant inclusion in `get-user-tenants` self, tenant-owner scoped, and global-admin paths.
  - [ ] Add actor tests where `UserTenants[targetUser]` contains valid, orphan, and valid tenant IDs across a deterministic keyset boundary; assert the response omits the orphan, contains no blank/synthesized name item, does not page around it incorrectly, and emits the repair warning.
  - [ ] Add a global-admin ordinary `get-user-tenants` orphan test to prove diagnostic exposure is not added to normal membership list results.
  - [ ] Use explicit sortable tenant IDs and fixed timestamps/test data so orphan filtering and cursor assertions never rely on dictionary enumeration order or wall-clock timing.
  - [ ] Add a stale self-lookup regression guard that documents query-only eventual consistency after removal and proves no write authorization behavior is inferred from the stale query result.
  - [ ] Keep existing signed cursor tests green; if pagination expectations change because filtering now happens before pagination, update tests to assert the new policy directly.
  - [ ] Keep existing 401/403/404/400 ProblemDetails behavior green.

## Dev Notes

### Policy To Implement

- Disabled tenants are included in query responses when the caller is otherwise authorized to see the tenant or membership. Query responses must include `TenantStatus.Disabled`; clients must not infer command capability from query visibility. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.3`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-16.md#Story 9.3`]
- Orphan memberships are filtered from normal user-facing query responses before keyset ordering, page-size selection, and signed cursor generation. Do not return blank names, synthesized names, or default `TenantStatus.Active` for a tenant ID that is missing from `TenantIndexReadModel.Tenants`. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.3`]
- "Related query output" in the acceptance criteria means existing query responses that already serialize `TenantSummary` or `UserTenantMembership`; this story must not create new public query surfaces to expose disabled or orphan state.
- A global administrator diagnostic or repair view may surface orphan projection inconsistencies later, but this story must not add that view, must not expose the inconsistency through ordinary `get-user-tenants` results, and must keep repair observability in internal telemetry or structured warning logs. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.3`]
- Projection eventual consistency after user removal is accepted: a self-lookup can briefly show the prior membership until the tenant index projection processes `UserRemovedFromTenant`. This query result grants no write capability. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.3`; `docs/cross-aggregate-timing.md#Timing Window`]

### Current Code State

- `TenantIndexReadModel.Apply(TenantDisabled)` changes `TenantIndexEntry.Status` to `TenantStatus.Disabled` and leaves the tenant in `Tenants`; `Apply(TenantEnabled)` restores `Active`. This already supports disabled tenant inclusion in list-style query output. [Source: `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`]
- `TenantSummary` and `UserTenantMembership` already include `TenantStatus Status`, so this story should not need a public query contract change. [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantSummary.cs`; `src/Hexalith.Tenants.Contracts/Queries/UserTenantMembership.cs`]
- `HandleListTenantsAsync` enumerates `indexModel.Tenants`, filters non-admin users by `UserTenants[requester]`, and maps `TenantSummary(kvp.Key, kvp.Value.Name, kvp.Value.Status)`. Disabled tenants should already appear with disabled status when visible to the caller. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `HandleGetUserTenantsAsync` is the main bug surface. It enumerates the target user's `UserTenants` entries and currently maps a missing tenant index entry with `entry?.Name ?? string.Empty` and `entry?.Status ?? TenantStatus.Active`. That is the behavior to remove. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `GetVisibleUserTenants` enforces current self/global-admin/tenant-owner scoped visibility for `get-user-tenants`. Preserve it and add orphan filtering after visibility is determined but before pagination. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`; `_bmad-output/implementation-artifacts/post-epic-5-r5a2-get-user-tenants-scoped-authorization.md`]
- Story 9.1 added `ITenantQueryCursorCodec`, protected cursor scopes, and controller-boundary invalid cursor validation. Story 9.3 must keep signed cursor behavior and avoid reintroducing raw cursor assumptions. [Source: `_bmad-output/implementation-artifacts/9-1-opaque-signed-query-cursors.md`; `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`]

### Implementation Guardrails

- Filter orphan `get-user-tenants` entries before calling `Paginate`. If filtering happens inside the result selector or after `Take(pageSize + 1)`, an orphan can still consume a page slot, shorten pages incorrectly, or force confusing cursor behavior.
- Preserve Story 9.2 current-state pagination semantics: the cursor is a lower-bound continuation over the current authorized and orphan-filtered set, not proof that the prior anchor item must still exist in the response.
- Do not delete or mutate `UserTenants` while serving the query. The query path should report the anomaly through logging only; projection repair is a separate operational concern.
- Use `envelope.CorrelationId` for the repair warning. Query envelopes already carry correlation metadata from `TenantsQueryController`.
- Treat warning metadata as internal observability. Optional activity/trace tags are acceptable if they follow existing conventions, but this story must not add or change public response metadata.
- Preserve timing-uniformity safeguards: cross-user `get-user-tenants` still performs the global-admin check before early empty responses for missing target users.
- Do not change `PaginatedResult<T>` shape, public query DTO constructors, endpoint routes, or cursor scope strings.
- Do not add new command behavior for disabled tenants. Existing aggregate and conformance tests own command rejection for disabled tenants. [Source: `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`; `tests/Hexalith.Tenants.Testing.Tests/Fakes/InMemoryTenantServiceTests.cs`]
- Do not modify the `Hexalith.EventStore` submodule.

### Files Likely To Update

- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`: filter orphan memberships, add repair warning logging, preserve disabled status mapping and signed cursor handling.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`: add disabled tenant inclusion tests, orphan filtering/logging tests, and any pagination adjustment tests.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantIndexReadModelTests.cs`: add only if a missing disabled lifecycle assertion is discovered; existing tests already cover status transitions.
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`: optional API-level check if actor tests do not sufficiently prove serialized disabled status or invalid cursor behavior.
- `docs/cross-aggregate-timing.md`: add the concise tenant query projection note required by AC4.

### Testing Requirements

- Use xUnit and Shouldly, matching existing test style.
- Use the existing `CreateTenantIndexModel`, `SetupTenantIndexState`, and `CreateActor` helpers in `TenantsProjectionActorTests`.
- For disabled tests, create tenants with `TenantCreated`, apply `TenantDisabled`, add user memberships, then assert returned `TenantSummary` or `UserTenantMembership` carries `TenantStatus.Disabled`.
- For orphan tests, create a `TenantIndexReadModel` with `UserTenants` entries whose tenant IDs include valid, orphan, and valid items across the cursor boundary while `Tenants` omits the orphan. Assert the response omits that tenant, does not contain an item with an empty name/default active status, and generates the next cursor from the filtered candidate set.
- Add an all-orphan page case that proves an empty filtered result has no synthesized membership and no next cursor derived from an orphan.
- Add a cursor-anchor-disappeared case, if supported by existing cursor test helpers, that proves continuation is evaluated against the current filtered set without exposing or recreating the missing anchor.
- For warning-log tests, prefer a focused test logger/list logger pattern already used in the repository if available; otherwise add a small local test logger in the test file rather than introducing a logging package.
- For stale self-lookup tests, keep query visibility and command/write authorization separate; this story documents the query projection window and must not add command behavior.
- Run at minimum:
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsProjectionActorTests`
  - `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests` if controller or serialization coverage changes.

### Previous Story Intelligence

- Story 9.1 completed the signed cursor implementation but the full unfiltered solution test gate remained blocked by unrelated Aspire topology and snapshot performance failures. Do not treat those failures as Story 9.3 requirements. [Source: `_bmad-output/implementation-artifacts/9-1-opaque-signed-query-cursors.md#Dev Agent Record`]
- Story 9.2 defines current-state keyset continuation after authorization filtering. Story 9.3 should align with that policy by filtering orphan entries before pagination and by keeping current projected disabled status visible. [Source: `_bmad-output/implementation-artifacts/9-2-stable-cursor-pagination-under-role-and-membership-changes.md#Current Pagination Policy To Implement`]
- R5-A2 added tenant-owner scoped cross-user `get-user-tenants` behavior and timing-uniformity checks. Do not regress those protections while adding orphan filtering. [Source: `_bmad-output/implementation-artifacts/post-epic-5-r5a2-get-user-tenants-scoped-authorization.md`]
- R5-A3 added audit projection/query behavior. Do not broaden audit visibility or add a diagnostic orphan view in this story. [Source: `_bmad-output/implementation-artifacts/post-epic-5-r5a3-tenant-audit-projection-query.md`]

### Project Context Reference

- Follow `AGENTS.md`: do not initialize/update nested submodules recursively, and use Conventional Commits for commits.
- Follow repository C# conventions: file-scoped namespaces, nullable-safe code, central package management, no inline package versions, source-generated logging when adding structured logs, xUnit and Shouldly for tests.
- No root `project-context.md` exists for this application repository; submodule project-context files are reference context only and should not drive Tenants story scope.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

### Completion Notes List

### File List

## Party-Mode Review

- Date: 2026-05-17T11:40:38+02:00
- Selected story key: 9-3-query-policy-for-disabled-tenants-and-orphan-memberships
- Command/skill invocation used: `/bmad-party-mode 9-3-query-policy-for-disabled-tenants-and-orphan-memberships; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect), John (Product Manager)
- Findings summary:
  - Orphan `UserTenants` entries must be filtered before keyset ordering, `pageSize + 1` selection, and signed cursor generation so Story 9.1 cursor safety and Story 9.2 current-state keyset semantics are preserved.
  - Projection-repair observability must stay internal to structured logs or existing telemetry conventions; the story must not introduce public DTO fields, response metadata, diagnostic routes, or an orphan list.
  - Disabled tenants must remain visible only after the caller passes the existing self, global-admin, or tenant-owner visibility policy, and returned DTOs must preserve `TenantStatus.Disabled`.
  - The current missing-tenant fallback to empty name plus `TenantStatus.Active` is an implementation trap and must be removed.
  - Tests need deterministic sortable keys, pre-pagination orphan filtering coverage, structured repair-warning assertions, and a stale-read/no-write regression guard.
- Changes applied:
  - Split orphan filtering and repair observability into separate acceptance criteria.
  - Tightened tasks and policy notes for pre-pagination/pre-cursor orphan filtering.
  - Clarified repair warning metadata as internal telemetry/logging only.
  - Expanded testing requirements for deterministic keyset boundaries, disabled-status visibility, stale query-only eventual consistency, and no public diagnostic exposure.
- Findings deferred:
  - Whether projection repair should later become automatic, manual, or operator-triggered.
  - Whether orphan warnings should become metrics in addition to structured logs.
  - Whether a first-class diagnostic orphan view is needed later.
  - Exact warning metadata field names and any raw-vs-redacted tenant ID decision, to follow existing logging/privacy conventions during implementation.
- Final recommendation: needs-story-update

## Advanced Elicitation

- Date: 2026-05-17T12:57:06+02:00
- Selected story key: 9-3-query-policy-for-disabled-tenants-and-orphan-memberships
- Command/skill invocation used: `/bmad-advanced-elicitation 9-3-query-policy-for-disabled-tenants-and-orphan-memberships`
- Batch 1 methods: Red Team vs Blue Team; Failure Mode Analysis; Self-Consistency Validation; Socratic Questioning; Critique and Refine
- Reshuffled Batch 2 methods: Security Audit Personas; Pre-mortem Analysis; Occam's Razor Application; 5 Whys Deep Dive; Expert Panel Review
- Findings summary:
  - Cursor handling needed an explicit current-state lower-bound rule when prior anchors are now orphaned or filtered.
  - The story needed an all-orphan page expectation so implementers do not accidentally derive page occupancy or next cursors from hidden data.
  - "Related query output" was broad enough to invite accidental new public diagnostic surfaces.
  - Logging guidance needed a privacy-aware testable contract without deciding raw-versus-redacted identifier policy in the story.
- Changes applied:
  - Added AC6 for all-orphan filtered pages.
  - Clarified orphan filtering before continuation, page-size selection, and cursor generation, including disappeared cursor anchors.
  - Scoped related query output to existing DTO-bearing responses only.
  - Added privacy-aware structured logging guidance and focused test expectations for empty filtered pages and disappeared cursor anchors.
- Findings deferred:
  - Exact raw-versus-redacted identifier policy for orphan warning fields remains implementation-time alignment with existing repository logging conventions.
  - Automated projection repair, metrics, and diagnostic orphan views remain out of scope for this story.
- Final recommendation: ready-for-dev
