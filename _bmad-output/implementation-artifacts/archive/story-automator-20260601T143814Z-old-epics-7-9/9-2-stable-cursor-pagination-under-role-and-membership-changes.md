# Story 9.2: Stable Cursor Pagination Under Role and Membership Changes

Status: done

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a tenant operator,
I want paginated tenant query results to remain predictable when roles or memberships change between page requests,
so that users do not silently skip or gain visibility into tenants because projection state changed mid-pagination.

## Acceptance Criteria

1. Given a user is paging through `get-user-tenants` results, when their role on a tenant is revoked between page requests, then the next page does not reveal tenants the requester is no longer allowed to see.
2. Given a user is paging through `get-user-tenants` results, when a newly visible tenant would sort before or at the submitted cursor position, then the endpoint behavior is documented and tested so the result is predictable rather than accidental.
3. Given `list-tenants` and `get-user-tenants` both use cursor-based pagination, when role or tenant state changes occur between page fetches, then both endpoints follow the same documented keyset policy where applicable: authorize and filter against the current read model first, then apply the decoded cursor as an ordinal exclusive lower bound.
4. Given a cursor references an item that is no longer visible to the requester, when the endpoint processes the request, then it continues from the decoded lower-bound position after current authorization filtering and does not look up or disclose the hidden anchor.
5. Given focused tests simulate concurrent membership and role mutation, when paginated queries continue from a prior cursor, then tests verify no cross-tenant data leak and document any accepted eventual-consistency behavior.

## Tasks / Subtasks

- [x] Document and enforce the tenant query cursor stability policy. (AC: 1-4)
  - [x] Treat the decoded cursor position as an exclusive lower bound over stable ordinal tenant IDs or user IDs after the current request's authorization filter has been applied.
  - [x] Use the same ordinal comparison semantics for sorting and cursor advancement; do not mix `StringComparer.Ordinal` ordering with culture-sensitive cursor comparisons.
  - [x] Do not implement snapshot cursors in this story; each page reflects the latest projected read model visible to the requester at the time of that request.
  - [x] If a tenant becomes newly visible and sorts before or equal to the cursor, do not backfill it into later pages; document this accepted current-state behavior, including role transitions such as admin demotion or newly granted memberships.
  - [x] If a tenant or membership becomes hidden after the cursor was issued, never return it on a later page; continue with visible items whose stable keys sort after the decoded position.
  - [x] If the cursor anchor no longer exists or is no longer visible, do not disclose that fact; continue using the opaque decoded position as a lower bound and reject only when cursor validation itself fails.
- [x] Update `TenantsProjectionActor` pagination behavior only where needed. (AC: 1-4)
  - [x] Preserve existing ordering for `list-tenants`, `get-tenant-users`, and `get-user-tenants`: `StringComparer.Ordinal` on the key selected by the endpoint.
  - [x] Keep authorization filtering before pagination for `list-tenants` non-admin users and `get-user-tenants` tenant-owner scoped cross-user lookups.
  - [x] Keep the R5-A2 timing-uniformity guard: cross-user `get-user-tenants` lookups still run the global-admin check before early empty responses.
  - [x] Reuse the Story 9.1 cursor codec/scope path if it is complete; do not introduce a second cursor format or a parallel signing abstraction.
  - [x] Keep `get-tenant-audit` ordering by timestamp then event ID; include it in tests only if shared pagination utility changes could affect it.
- [x] Add focused mutation-between-pages tests. (AC: 1-5)
  - [x] Add `get-user-tenants` self-lookup test where a tenant visible on the next page is removed between page requests and is not returned.
  - [x] Add `get-user-tenants` test where a newly visible tenant sorts before or at the cursor and is not backfilled into the next page.
  - [x] Add `get-user-tenants` test where a newly visible tenant sorts after the cursor and may appear on the next page under current-state keyset semantics.
  - [x] Add `get-user-tenants` tenant-owner cross-user test where owner visibility changes between pages and only currently owned overlapping tenants are returned.
  - [x] Keep separate owner cross-user tests for target-user membership changes and requesting-owner visibility changes so each authorization axis is proven independently.
  - [x] Add `list-tenants` non-admin test where membership changes between pages and the next page applies current membership filtering.
  - [x] Ensure mutation tests request page one, capture the signed cursor, mutate the read model, and request page two with that exact cursor.
  - [x] Preserve or update existing cursor tests such as `ListTenants_cursor_skips_deleted_tenant` so the behavior is described as "cursor anchor missing/hidden continues from lower bound", not as a deletion feature.
- [x] Update documentation or test names/comments to make the policy visible. (AC: 2, 3, 5)
  - [x] Prefer concise comments near the tests and/or a short docs note if an existing tenant query documentation file covers pagination.
  - [x] State that the API offers stable keyset continuation, not repeatable-read snapshots across page requests.
  - [x] State that authorization is re-evaluated on every page request and revoked visibility wins over cursor continuity.

## Dev Notes

### Current Pagination Policy To Implement

- Use current-state keyset continuation. The cursor's internal position is an exclusive lower bound, not a durable snapshot of all items that were visible on page one.
- The shared policy for `list-tenants` and `get-user-tenants` is: apply current authorization and visibility filtering first, order the remaining keys with ordinal semantics, then advance from keys strictly greater than the decoded cursor position.
- The allowed behavior under projection mutation is:
  - Revoked or hidden tenant access must never appear in later pages, even if the cursor was issued before revocation.
  - Newly visible items with keys less than or equal to the cursor are not returned later. This is an accepted no-backfill behavior.
  - Newly visible items with keys greater than the cursor may appear on later pages because each page uses current projection state.
  - Missing or hidden cursor anchors are not special. Do not look up the anchor to decide visibility; compare visible keys to the decoded position.
- This policy preserves NFR5: no cross-tenant query leak. It also preserves FR30's consistent ordering without promising repeatable-read pagination across projection changes. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.2`; `_bmad-output/planning-artifacts/prd.md#FR30`; `_bmad-output/planning-artifacts/architecture.md#Cross-Tenant Index Projections`]

### Current Code State

- `TenantsProjectionActor` handles all tenant query authorization and pagination. `Paginate` currently orders items by endpoint key selector with `StringComparer.Ordinal`, filters with `string.Compare(key, cursor, Ordinal) > 0`, takes `pageSize + 1`, and returns the last returned key as the next cursor. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `HandleListTenantsAsync` loads `TenantIndexReadModel`, branches global admins to all tenants, filters non-admins to tenant IDs from `indexModel.UserTenants[envelope.UserId]`, then paginates. This is the main `list-tenants` surface for membership mutation tests. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `HandleGetUserTenantsAsync` resolves target user from `EntityId`, checks global-admin status before early empty return for timing uniformity, filters cross-user visibility to requester-owned tenant IDs unless the requester is self or global admin, then paginates. Preserve this ordering. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `TenantIndexReadModel` keeps `Tenants` and `UserTenants` dictionaries. User add/remove/role-change projection events update `UserTenants`; tenant disable/enable changes `TenantIndexEntry.Status` without removing visibility. [Source: `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`]
- `UserTenantMembership` and `TenantSummary` already include `TenantStatus`, so Story 9.2 does not need a public query contract change. [Source: `src/Hexalith.Tenants.Contracts/Queries/UserTenantMembership.cs`; `src/Hexalith.Tenants.Contracts/Queries/TenantSummary.cs`]

### Story 9.1 Dependency And Guardrails

- Story 9.1 is currently in progress in `sprint-status.yaml`. This story should reuse its cursor codec and endpoint scopes once that work is complete. The current source already contains `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`, but `TenantsProjectionActor` still has raw cursor parsing at the time this story was created; verify the final 9.1 implementation before editing. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`; `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- Do not create another cursor codec, another Data Protection purpose, or a second internal cursor DTO. Extend or reuse the Story 9.1 path.
- If Story 9.1 scopes `get-user-tenants` cursors by target user rather than requester, keep authorization recomputation on every page request as the security boundary and document the rationale in tests or code comments if touched.
- If Story 9.1 changes `Paginate` to encode/decode opaque cursors, Story 9.2 should test against signed public cursors and decoded internal positions, not raw public cursor strings.
- Invalid/tampered/scope-mismatched cursors remain Story 9.1 behavior. Story 9.2 is about valid cursors whose logical position remains meaningful after read-model authorization state changes.

### Boundaries

- Do not implement Story 9.3 disabled-tenant/orphan-membership policy here. Current status values should continue to flow through query results. Story 9.3 owns any repair logging or orphan filtering changes. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.3`]
- Do not centralize all page-size policy or cursor utility behavior unless it is necessary for the mutation tests. Story 9.5 owns shared pagination bounds and utility cleanup. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.5`]
- Do not modify the `Hexalith.EventStore` submodule for this story.
- Preserve API/controller shape. `PaginatedResult<T>` remains `Items`, `Cursor`, and `HasMore`. [Source: `src/Hexalith.Tenants.Contracts/Queries/PaginatedResult.cs`]

### Files Likely To Update

- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`: ensure list/user pagination applies the policy after authorization filtering and after Story 9.1 cursor decoding.
- `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`: only if Story 9.1 left a small scope/position gap needed by this story.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`: add mutation-between-pages tests and update raw cursor assertions if Story 9.1 signed cursors are active.
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`: only if the final behavior needs API-level evidence for no leak under signed public cursors.
- Existing docs under `docs/` or tenant query docs if a pagination policy page already exists; otherwise focused test names/comments are acceptable.

### Testing Requirements

- Use xUnit and Shouldly. Keep test names PascalCase/descriptive, matching existing project style.
- Actor tests currently instantiate `TenantsProjectionActor` with substituted `DaprClient` and `NullLogger`; adjust the helper only if Story 9.1 added cursor codec dependencies.
- Test by issuing page one, mutating or substituting the in-memory `TenantIndexReadModel`, then issuing page two with the prior cursor. The important assertion is current visible results after the cursor position, not object identity of the old page.
- Include at least one non-admin `list-tenants` test and at least two `get-user-tenants` tests because Story 9.2's risk is highest in scoped user lookup.
- Keep existing 401/403/404 behavior green. This story should not change controller authorization or query error mapping.

### Previous Story Intelligence

- Story 9.1 establishes opaque signed cursors and should be treated as the cursor format dependency. Its guardrails say not to solve Story 9.2 cursor stability there; this story is that follow-up.
- R5-A2 introduced tenant-owner scoped `get-user-tenants` visibility and timing-uniformity checks for missing target users. Do not regress those protections while adding mutation tests.
- R5-A3 implemented audit query behavior and tenant-scoped audit filtering. Do not broaden audit visibility while touching shared pagination helpers.
- Recent git commits are release/preflight automation-oriented (`462b3d9`, `7f399fc`, `f43dc69`) and do not provide a better pagination pattern than the current actor tests.

### Project Context Reference

- Follow `AGENTS.md`: do not run recursive submodule initialization/update, and use Conventional Commits for any commit.
- Follow repository C# conventions: nullable-safe code, file-scoped namespaces, no inline package versions, Shouldly assertions, and focused tests.
- The persistent BMAD context files reinforce that root-level submodules are separate roots; `Hexalith.EventStore` is reference context only unless explicitly approved.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-17: Verified Story 9.1 cursor codec is active in `TenantsProjectionActor`; no second cursor format introduced.
- 2026-05-17: `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-restore` passed: 312 passed, 0 failed.
- 2026-05-17: `dotnet test Hexalith.Tenants.slnx --configuration Release --no-restore` attempted; non-integration projects and server tests passed, integration lane failed on Docker/Aspire runtime health plus pre-existing DAPR command-response configuration failures unrelated to Story 9.2.
- 2026-05-17: `docker info --format '{{json .ServerVersion}}'` timed out with Docker Desktop pipe/API 500, matching the Aspire topology fixture failures.

### Completion Notes List

- Added actor-level policy documentation that pagination callers must pass the current authorized/visible set and that cursors are ordinal exclusive lower bounds, with no hidden-anchor lookup or disclosure.
- Confirmed existing `Paginate` behavior already uses `StringComparer.Ordinal` ordering and `StringComparison.Ordinal` cursor advancement after endpoint-specific authorization filtering.
- Added focused signed-cursor mutation tests for `get-user-tenants` self lookup, newly visible tenants before and after the cursor, target-user membership removal, requester owner demotion, and non-admin `list-tenants` membership removal.
- Renamed the deleted-anchor test to describe the general missing-anchor lower-bound policy rather than a deletion-only behavior.
- No query contract, public API, cursor codec, audit ordering, or EventStore submodule changes were required.

### File List

- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`
- `_bmad-output/implementation-artifacts/9-2-stable-cursor-pagination-under-role-and-membership-changes.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`

### Change Log

- 2026-05-17: Implemented Story 9.2 cursor stability evidence and moved story to review.
- 2026-05-17: Code review (`/bmad-code-review 9.2`) completed — Acceptance Auditor cleared all 5 ACs; applied 4 review patches: actor-layer cursor `failureReason` logging (EventId 1902), `ProtectCursor` whitespace guard, scope-segment escaping in `TenantQueryCursorScopes`, and round-trip decode assertions on the cursor opacity tests. 312/312 server tests pass. 3 items deferred to Stories 9.4/9.5/11.

## Party-Mode Review

- Date: 2026-05-17T11:35:19+02:00
- Selected story key: 9-2-stable-cursor-pagination-under-role-and-membership-changes
- Command/skill invocation used: `/bmad-party-mode 9-2-stable-cursor-pagination-under-role-and-membership-changes; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect), John (Product Manager)
- Findings summary:
  - Clarify that missing or hidden cursor anchors continue from the decoded lower-bound position after current authorization filtering, with no anchor lookup or disclosure.
  - Make `list-tenants` and `get-user-tenants` parity explicit: current authorization filter first, ordinal key ordering second, exclusive lower-bound cursor advancement third.
  - Require mutation-between-pages tests to capture a signed cursor from page one, mutate projection visibility, then reuse the exact cursor for page two.
  - Expand test guidance for newly visible tenants before/equal cursor, newly visible tenants after cursor, hidden-but-existing anchors, target-user membership changes, and requester-owner visibility changes.
  - Call out Story 9.1 cursor scope verification so developers do not create a second cursor codec or accidentally rely on cursor scope instead of per-request authorization.
- Changes applied:
  - Tightened acceptance criteria 3 and 4 around shared current-state keyset semantics and hidden-anchor behavior.
  - Added task guidance for ordinal comparer consistency, no-backfill under role transitions, signed-cursor mutation sequencing, and independent owner cross-user test axes.
  - Added developer notes for the shared `list-tenants` / `get-user-tenants` policy and Story 9.1 `get-user-tenants` cursor scope rationale.
- Findings deferred:
  - Full solution/Aspire topology and snapshot-performance blockers remain outside Story 9.2 unless they prevent focused query tests.
  - Broader cursor redesign remains outside scope; reuse Story 9.1.
  - Disabled-tenant and orphan-membership policy remains Story 9.3.
  - Shared pagination utility cleanup remains Story 9.5 unless directly needed for Story 9.2 tests.
- Final recommendation: needs-story-update

### Review Findings

Date: 2026-05-17 (bmad-code-review run, diff range fdc6e9e^..a2010bf restricted to TenantsProjectionActor.cs + TenantsProjectionActorTests.cs)

Layers run: Blind Hunter (no context), Edge Case Hunter (diff + read-only project), Acceptance Auditor (full spec). Acceptance Auditor reported **no AC violations** — all five ACs are evidenced by the diff and no out-of-scope changes were introduced.

- [x] [Review][Patch] (was Decision D1) Scope helper `GetTenantAudit` uses `|` as a delimiter — collision risk if tenant IDs ever contain `|` [src/Hexalith.Tenants/Queries/TenantQueryCursorCodec.cs:158-167] — Resolved 2026-05-17 by escaping `|`, `:`, and `\` in caller-supplied segments (option a: in-place escape). All four scope helpers (`ListTenants`, `GetTenantUsers`, `GetUserTenants`, `GetTenantAudit`) now route segment values through `EscapeSegment`.
- [x] [Review][Patch] Capture and log cursor `failureReason` instead of discarding it [src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs:334,361,408,457] — Resolved 2026-05-17. Added `_logger` field, `LoggerMessage` source-gen partial `Log.InvalidCursorRejected` (EventId 1902, Warning), and a per-handler `InvalidCursorResult(queryType, endpoint, tenantId, userId, failureReason)` helper. All four sites now capture `out string? failureReason` and emit structured logs while preserving the opaque client-facing `"Invalid cursor."` message.
- [x] [Review][Patch] `ProtectCursor` will throw `ArgumentException` if a future bug ever produces an empty/whitespace inner cursor [src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs:268-272] — Resolved 2026-05-17 by changing the null-guard to `string.IsNullOrWhiteSpace(result.Cursor)`.
- [x] [Review][Patch] Cursor opacity assertions test substring absence, not cryptographic opacity [tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs] — Resolved 2026-05-17. `GetTenantAudit_paginates_after_filtering_with_stable_cursor` and `GetTenantUsers_signed_cursor_resumes_from_same_logical_position` now build an explicit `cursorCodec`, call `TryDecode(...).ShouldBeTrue()`, and assert the decoded inner position matches the expected key (`":evt-a"` suffix for audit, `"user-1"` for users).
- [x] [Review][Defer] `pageSize` parsing silently clamps and silently defaults on non-int32 values [src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs:99-119,135-148] — deferred, pre-existing (not changed by this diff); belongs to Story 9.5 (shared pagination bounds and cursor utilities).
- [x] [Review][Defer] Tests use `EphemeralDataProtectionProvider`; production cross-replica/multi-host Data Protection key sharing for the cursor codec is unverified [tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs `CreateCursorCodec`] — deferred, Story 9.1 cursor-codec design territory; Story 11 (Production Authorization Readiness) likely owns the DP key persistence configuration.
- [x] [Review][Defer] No actor-layer integration test for expired / wrong-scope / replayed signed cursor [tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs] — deferred, Story 9.1 owns codec-level coverage (story is `done`); Story 9.4 (actor-layer query guardrails) is the better home for actor-level negative cursor cases.

Dismissed as noise (9): null-check for DI-injected codec; removed `s_auditCursorRegex` (codec is the only emitter of audit cursors so internal format is invariant); `Paginate` comment as load-bearing invariant (style); ordinal-ordering encoded in test names (it IS the AC3 contract); `indexModel.Apply` shared-snapshot pattern (NSubstitute returns same ref; structurally equivalent to production projection update); audit cursor with purged event (spec explicitly de-scopes audit tests unless shared-utility changes affect them); `ProtectCursor` instance vs `Paginate` static nesting (style); opaque client-facing error (intentional non-disclosure); audit scope mismatch silent rejection (by design — filter change must invalidate cursor; telemetry covered by patch above).
