# Story 9.4: Actor-Layer Query Guardrails

Status: review

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a platform operator,
I want projection actors to reject malformed or unauthenticated query envelopes defensively,
so that authorization assumptions remain protected even if a controller or caller bypasses the normal API boundary.

## Acceptance Criteria

1. Given a query envelope reaches `TenantsProjectionActor` with a null, empty, or whitespace-only `UserId`, when the actor handles one of the current role-sensitive query types (`get-tenant`, `list-tenants`, `get-tenant-users`, `get-user-tenants`, or `get-tenant-audit`), then the actor rejects the query with a safe `Forbidden` authorization failure instead of relying only on controller-layer checks.
2. Given a role-sensitive query is executed through the normal controller path, when the authenticated user ID is present, then existing successful query behavior remains unchanged.
3. Given a malformed role-sensitive query envelope is rejected because `UserId` is null, empty, or whitespace-only, when the actor evaluates the guardrail, then the rejection occurs before authorization lookup, cursor parsing, tenant lookup, membership lookup, audit projection access, or any other projection state read where the current test seams can observe that ordering.
4. Given a query envelope contains a present but unauthorized user ID, when the actor evaluates the query, then existing authorization behavior is preserved and no tenant data is returned outside the caller's allowed scope.
5. Given actor-layer guardrails reject a query, when the failure is logged, then logs include only safe structured context such as correlation ID when present, query type, failure reason, and a fixed stage, and do not expose tenant IDs, user IDs, aggregate IDs that identify tenant data, member IDs, tenant membership details, cursor values, authorization decision internals, audit content, or sensitive payload data.
6. Given an unknown query type reaches the actor with a malformed `UserId`, when the actor evaluates the envelope, then unknown query behavior remains unchanged by this story.
7. Given focused actor tests run, when null-user, empty-user, whitespace-user, unauthorized-user, valid-user, valid-cursor, invalid-cursor, and unknown-query paths are exercised where current fixtures support them, then tests verify defense-in-depth behavior without weakening existing controller authorization tests.
8. Given a known role-sensitive query has both a malformed `UserId` and malformed pagination, cursor, audit, or query payload data, when the actor evaluates the envelope, then the malformed-user guard returns the safe `Forbidden` authorization failure before any invalid-cursor, invalid-payload, or projection-state behavior can be observed.

## Tasks / Subtasks

- [x] Add actor-level authenticated-user validation for all role-sensitive tenant queries. (AC: 1, 2, 4)
  - [x] In `TenantsProjectionActor.ExecuteQueryAsync`, validate `envelope.UserId` after identifying the current known role-sensitive query handlers (`get-tenant`, `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit`) and before dispatching them.
  - [x] Treat `null`, empty, and whitespace-only `UserId` as an authorization failure even though the public `QueryEnvelope` constructor already rejects blank values.
  - [x] Return `new QueryResult(false, default, ErrorMessage: QueryAdapterFailureReason.Forbidden)` or the existing equivalent `"Forbidden"` value so `SubmitQueryHandler` maps the failure to the established safe 403 ProblemDetails path.
  - [x] Keep unknown query-type behavior unchanged; this story is not a query-type taxonomy refactor.
- [x] Add safe actor warning logging for rejected malformed-auth query envelopes. (AC: 4)
  - [x] Add a source-generated `LoggerMessage` warning on `TenantsProjectionActor` with correlation ID when present, query type, failure reason, and a fixed stage such as `TenantQueryEnvelopeAuthorization`.
  - [x] Treat missing correlation metadata as loggable safe context, not as a new failure path.
  - [x] Do not parse query payloads solely to enrich guardrail logs.
  - [x] Do not log tenant IDs, user IDs, aggregate IDs that identify tenant data, member IDs, `Payload`, cursor text, protected cursor payloads, signing material, `UserTenants`, membership lists, audit rows, or target-user membership details.
  - [x] Keep the message generic, for example "Tenant query envelope rejected before authorization because authenticated user id was missing."
- [x] Preserve existing valid-user and unauthorized-user behavior. (AC: 2, 3)
  - [x] Do not move or weaken `IsAuthorizedForTenantAsync`, `IsGlobalAdminAsync`, `GetUserTenantIds`, or `GetVisibleUserTenants`.
  - [x] Preserve `get-user-tenants` timing-uniformity behavior: cross-user lookups still run the global-admin check before returning an empty result for missing target users.
  - [x] Preserve Story 9.1 signed cursor behavior and Story 9.2 current-state keyset continuation behavior.
  - [x] Preserve Story 9.3 disabled-tenant visibility and orphan-membership filtering policy when that story is implemented.
- [x] Add focused actor tests for malformed and valid envelopes. (AC: 1-5)
  - [x] In `TenantsProjectionActorTests`, add tests that create a valid `QueryEnvelope` and then use record `with` initialization to set `UserId = ""`, whitespace, and `null!` for at least `get-tenant`, `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit`.
  - [x] Keep the null-forgiving test construction intentional and visible so the tests exercise malformed deserialized/internal envelopes rather than public constructor validation.
  - [x] Assert malformed-user cases return unsuccessful `QueryResult` with `ErrorMessage` equal to `QueryAdapterFailureReason.Forbidden` or `"Forbidden"` and do not return payload bytes.
  - [x] Assert the actor does not call DAPR state reads or membership/global-admin lookup paths for rejected malformed-user envelopes where the current actor fixture exposes those calls, so the guardrail happens before projection state access.
  - [x] Add at least one precedence test where a guarded query contains both a malformed `UserId` and an otherwise invalid cursor or malformed payload, and assert the result is the safe `Forbidden` failure rather than `"Invalid cursor."`, `"Invalid audit query payload."`, unknown parsing output, or a state-derived failure.
  - [x] Add a focused regression test that an unknown query type with a malformed `UserId` still follows the existing unknown-query result path.
  - [x] Add focused valid-cursor and invalid-cursor regression coverage for guarded query types that already have cheap actor-test seams, especially `list-tenants`, so Story 9.1 and Story 9.2 cursor semantics are not changed for valid callers.
  - [x] For valid-path coverage, assert the existing result shape and key fields remain unchanged where fixtures provide a stable expected model, not only that the query succeeds.
  - [x] Keep existing unauthorized-but-present-user tests green, such as `GetTenant_unauthorized_user_returns_forbiddenAsync`, `GetTenantAudit_non_admin_returns_forbidden_not_501Async`, non-owner `get-user-tenants`, and non-admin `list-tenants` filtering tests.
  - [x] Add or update a logger-capture test only if the repository already has a lightweight test logger pattern; otherwise keep logging verification to "does not throw and returns Forbidden" to avoid adding test-only infrastructure.
- [x] Keep controller and public contract scope tight. (AC: 2, 5)
  - [x] Do not change `TenantsQueryController` normal `sub` extraction or its existing `Unauthorized()` behavior for HTTP requests missing an authenticated subject.
  - [x] Do not change `QueryEnvelope` public constructor or DataContract shape unless implementation proves actor-side validation is impossible without it.
  - [x] Do not add package dependencies or update package versions for this story.

## Dev Notes

### Defense-In-Depth Policy

- The controller remains the normal API boundary and already rejects missing authenticated `sub` claims with `Unauthorized()`. Actor validation is a second layer for direct actor invocation, adapter misuse, deserialized envelopes, tests, or future routing paths that bypass controller assumptions. [Source: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`; `_bmad-output/planning-artifacts/epics.md#Story 9.4`]
- Use `QueryAdapterFailureReason.Forbidden` for actor-level missing-user rejection. Current EventStore `SubmitQueryHandler` maps that exact value to HTTP 403 with the standard query ProblemDetails flow; other values such as `invalid-envelope` currently fall through to a 500 unless EventStore mapping is expanded. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryAdapterFailureReason.cs`; `Hexalith.EventStore/src/Hexalith.EventStore.Server/Pipeline/SubmitQueryHandler.cs`]
- Do not add a new 401 mapping inside the actor. Authentication already happened before the actor in the normal HTTP path; this story's actor response should be a safe authorization denial for malformed internal envelopes, not a new public authentication challenge path. [Source: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`; `Hexalith.EventStore/src/Hexalith.EventStore.Server/Pipeline/SubmitQueryHandler.cs`]
- The role-sensitive query set is the current known tenant query switch cases in `TenantsProjectionActor`: `get-tenant`, `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit`. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]

### Current Code State

- `TenantsProjectionActor.ExecuteQueryAsync` currently only checks `envelope` for null, then dispatches directly to query handlers. Every handler assumes `envelope.UserId` is usable. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `HandleGetTenantAsync` and `HandleGetTenantUsersAsync` load tenant projection state before calling `IsAuthorizedForTenantAsync(envelope.UserId, model)`. A missing user should be rejected before the state read so malformed envelopes cannot probe state existence. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `HandleListTenantsAsync` calls `IsGlobalAdminAsync(envelope.UserId)`, then filters by `GetUserTenantIds(indexModel, envelope.UserId)` for non-admin callers. The new guard should keep this behavior unchanged for valid user IDs. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `HandleGetUserTenantsAsync` defaults an empty `EntityId` to `envelope.UserId` and uses the caller ID for self/global-admin/tenant-owner visibility. Missing caller identity must therefore fail before target-user resolution. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `HandleGetTenantAuditAsync` intentionally checks global-admin status before reading audit state so non-admins get 403, not capability or state information. The missing-user guard must preserve that safe behavior. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`]
- `QueryEnvelope` constructor validates `userId`, but the record has init-only data members and is serialized for DAPR actor proxy use. Actor code must not rely on constructor validation as the only invariant. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Queries/QueryEnvelope.cs`]

### Implementation Guardrails

- Put the guard near the top of `ExecuteQueryAsync`, after the null check and before any DAPR state read, global-admin lookup, cursor decoding, or payload serialization.
- Match the role-sensitive query set using the actor's existing query discriminator conventions or local constants already present in the code. Do not introduce a new public registry or shared authorization contract for this story.
- Use `string.IsNullOrWhiteSpace(envelope.UserId)` and handle `null` defensively despite the non-nullable property declaration.
- Add a small private helper such as `IsRoleSensitiveQuery(string? queryType)` only if it keeps the switch readable. Do not introduce a broad authorization framework for this story.
- Keep activity and metrics behavior coherent: rejected known role-sensitive queries should still record the query type duration, but should not require projection state access.
- Do not log `envelope.ToString()` for this guard. It redacts payload bytes but still contains user and entity identifiers; use explicit structured fields instead.
- Prefer logging query type, fixed stage, failure reason, and correlation metadata only; if `AggregateId` represents tenant data in the guarded query path, do not include it in the malformed-identity log.
- Do not modify `Hexalith.EventStore` for this story. If `Forbidden` mapping is insufficient in a future EventStore version, record that as a deferred dependency rather than expanding scope here.
- Future role-sensitive query types need an explicit opt-in/default guardrail policy in a later planning pass; this story intentionally protects the five actor-routed query types listed above and keeps unknown query behavior unchanged.
- The malformed-user guard owns failure precedence for known role-sensitive query types. If an envelope is both unauthenticated and otherwise malformed, the actor should return `Forbidden` before cursor decoding, audit payload validation, pagination payload parsing, or state access so direct actor callers cannot use malformed identity requests to probe other validation behavior.

### Files Likely To Update

- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`: add actor-level missing-user guard, source-generated warning log, and any small local helper.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`: add malformed-user envelope tests and preserve valid/unauthorized behavior tests.
- `tests/Hexalith.Tenants.IntegrationTests/TenantsQueryControllerIntegrationTests.cs`: optional only if a regression is found in the normal HTTP missing-sub path; controller behavior should already be covered and should not need story changes.

### Testing Requirements

- Use xUnit and Shouldly, matching existing tests in `TenantsProjectionActorTests`.
- Prefer existing `CreateEnvelope`, `CreateActor`, `SetupTenantState`, `SetupTenantIndexState`, `SetupGlobalAdminState`, and `SetupNoGlobalAdmin` helpers.
- For malformed-user tests, create the envelope normally and then apply `with { UserId = "" }`, `with { UserId = " " }`, and `with { UserId = null! }` so tests exercise actor defense rather than constructor validation.
- Verify rejected malformed-user envelopes return `Success == false`, no payload, and `ErrorMessage == QueryAdapterFailureReason.Forbidden` or `"Forbidden"`.
- For state-read short-circuit coverage, use `DidNotReceive()` on the DAPR substitute for the state keys associated with the selected query. Keep these assertions focused so they do not make tests brittle around unrelated DAPR calls.
- For failure-precedence coverage, combine a malformed `UserId` with a deliberately invalid cursor or malformed audit/standard pagination payload on a known role-sensitive query and assert the actor does not surface cursor or payload errors before the identity guard.
- Run at minimum:
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsProjectionActorTests`
  - If controller behavior changes unexpectedly, also run `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantsQueryControllerIntegrationTests`

### Latest Technical Information

- Microsoft Learn documents that `DataContractSerializer` can create deserialized objects without running constructors, so constructor guard clauses are not a sufficient security boundary for data contract types. [Source: Microsoft Learn, `System.Runtime.Serialization.DataContractSerializer`: https://learn.microsoft.com/dotnet/fundamentals/runtime-libraries/system-runtime-serialization-datacontractserializer#run-under-partial-trust]
- Microsoft Learn security guidance says deserialized object graphs should be treated as untrusted until validated, and `DataMemberAttribute.IsRequired` is not a complete state-safety guarantee. [Source: Microsoft Learn, Security Considerations for Data: https://learn.microsoft.com/dotnet/framework/wcf/feature-details/security-considerations-for-data#datacontractserializer]

### Previous Story Intelligence

- Story 9.1 completed Data Protection-backed signed cursors and safe invalid-cursor ProblemDetails mapping. Story 9.4 must preserve cursor decode/encode behavior and must not log cursor payloads. [Source: `_bmad-output/implementation-artifacts/9-1-opaque-signed-query-cursors.md#Completion Notes List`]
- Story 9.2 defines current-state keyset continuation after current-request authorization filtering. Actor guardrails should run before that filtering, then leave pagination semantics unchanged for valid callers. [Source: `_bmad-output/implementation-artifacts/9-2-stable-cursor-pagination-under-role-and-membership-changes.md#Current Pagination Policy To Implement`]
- Story 9.3 settles disabled-tenant and orphan-membership query behavior. Story 9.4 must not reinterpret disabled tenants as authorization failures and must not add diagnostic orphan views. [Source: `_bmad-output/implementation-artifacts/9-3-query-policy-for-disabled-tenants-and-orphan-memberships.md#Policy To Implement`]
- Story 9.1 remains `in-progress` only because unrelated full-suite gates are blocked; its focused cursor/query work is implemented and should be treated as the current code baseline. [Source: `_bmad-output/implementation-artifacts/9-1-opaque-signed-query-cursors.md#Completion Notes List`]

### Project Context Reference

- Follow `AGENTS.md`: do not initialize or update nested submodules recursively, and use Conventional Commits for commits.
- Follow repository C# conventions: nullable-safe code, file-scoped namespaces, central package management, no inline package versions, source-generated logging for structured logs, xUnit and Shouldly for tests.
- No root `project-context.md` exists for this application repository; submodule project-context files are reference context only and should not drive Tenants story scope.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-17: Added red actor tests for malformed `UserId` envelopes across `get-tenant`, `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit`; initial focused run failed as expected because the actor reached state/admin/cursor paths first.
- 2026-05-17: Added `ExecuteQueryAsync` role-sensitive query guard and source-generated warning log, then reran focused actor tests successfully.
- 2026-05-17: `dotnet test Hexalith.Tenants.slnx --configuration Debug --no-restore` built and passed non-integration test assemblies, but integration tests were blocked by local Redis/Docker prerequisites: Dapr pubsub Redis could not connect to `localhost:6379`, and Aspire reported Docker unhealthy.
- 2026-05-17: `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore` succeeded with 0 warnings and 0 errors.

### Implementation Plan

- Keep the guard local to `TenantsProjectionActor.ExecuteQueryAsync` so unknown query behavior remains unchanged and the normal controller authentication boundary stays untouched.
- Use `QueryAdapterFailureReason.Forbidden` for malformed authenticated-user values so existing EventStore adapter mapping continues to produce the safe authorization failure.
- Log only correlation ID, query type, failure reason, and fixed stage for malformed identity rejection.
- Exercise malformed, valid, unauthorized, unknown-query, cursor precedence, and safe logging behavior through focused actor tests.

### Completion Notes List

- Added actor-layer defense-in-depth validation for null, empty, and whitespace `UserId` values before dispatching the five current role-sensitive tenant query handlers.
- Added source-generated warning logging for rejected malformed query envelopes without tenant/user/member/aggregate/payload/cursor details.
- Added focused actor tests for malformed identity short-circuiting before DAPR state reads, failure precedence over invalid cursor handling, unknown-query preservation, and safe structured log contents.
- Existing valid-user, unauthorized-user, signed-cursor, stable pagination, disabled-tenant, and orphan-membership behavior remains covered by the focused actor test suite.
- Full integration validation remains environment-blocked in this workspace by missing Redis on `localhost:6379` and unhealthy Docker/Aspire prerequisites, not by story code failures.

### File List

- `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantsProjectionActorTests.cs`
- `_bmad-output/implementation-artifacts/9-4-actor-layer-query-guardrails.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-17: Implemented actor-layer query guardrails for malformed authenticated user IDs and moved story to review.
## Party-Mode Review

- Date: 2026-05-17T12:01:22+02:00
- Selected story key: 9-4-actor-layer-query-guardrails
- Command/skill invocation used: `/bmad-party-mode 9-4-actor-layer-query-guardrails; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect), John (Product Manager)
- Findings summary:
  - Define the role-sensitive query set in acceptance criteria as the current five actor-routed query types and preserve unknown-query behavior.
  - Make null, empty, and whitespace-only `UserId` cases part of the acceptance contract, not only implementation notes.
  - Require malformed-user guardrail ordering before authorization lookup, cursor parsing, projection state access, and payload work where test seams can observe it.
  - Clarify that present-but-unauthorized users still follow existing authorization behavior even when both malformed and unauthorized cases return `Forbidden`.
  - Bound safe logging to existing structured metadata and avoid payload parsing or sensitive membership/cursor detail.
  - Preserve Story 9.1 signed cursor and Story 9.2 keyset continuation behavior for valid callers.
- Changes applied:
  - Updated acceptance criteria to include exact query types, null/empty/whitespace `UserId`, pre-read guard ordering, unknown-query preservation, and cursor regression coverage.
  - Updated tasks and implementation guardrails for safe logging metadata, null-forgiving malformed-envelope tests, state-read short-circuit assertions, and use of existing actor query discriminator conventions.
- Findings deferred:
  - Future role-sensitive query types need a later opt-in/default guardrail policy; this story intentionally keeps unknown query behavior unchanged.
  - No separate adopter-facing localization or accessibility work is needed because the behavior is actor-layer only.
- Final recommendation: ready-for-dev

## Party-Mode Review Follow-Up

- Date: 2026-05-17T12:04:31+02:00
- Selected story key: 9-4-actor-layer-query-guardrails
- Command/skill invocation used: `/bmad-party-mode 9-4-actor-layer-query-guardrails; review;`
- Participating BMAD agents: Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect), John (Product Manager)
- Findings summary:
  - Independent reviewers agreed the story needed explicit null, empty, and whitespace `UserId` handling, guard ordering before state/cursor/payload work, unchanged controller behavior, and narrow `Forbidden` failure semantics.
  - Current story text already contained those guard-ordering and query-scope clarifications from the earlier 2026-05-17 party-mode trace.
  - Reviewers flagged malformed-identity logging as still too permissive if aggregate IDs can identify tenant data.
  - Reviewers also asked valid-path tests to assert preserved behavior, not only successful completion.
- Changes applied:
  - Tightened safe logging acceptance criteria and task wording to omit tenant/user/member identifiers, audit content, and tenant-identifying aggregate IDs from malformed-identity logs.
  - Added valid-path testing guidance to assert existing result shape and key fields where stable fixtures exist.
- Findings deferred:
- Whether future role-sensitive query types should be guarded by a shared default-deny policy remains a later planning decision.
- Whether `get-user-tenants` needs additional actor-layer role or target-user authorization beyond existing behavior remains outside this malformed-identity guardrail story.
- Whether EventStore should map malformed internal envelopes to a different public status remains outside this story.
- Final recommendation: needs-story-update

## Advanced Elicitation

- Date: 2026-05-17T14:14:53+02:00
- Selected story key: 9-4-actor-layer-query-guardrails
- Command/skill invocation used: `/bmad-advanced-elicitation 9-4-actor-layer-query-guardrails`
- Batch 1 methods: Security Audit Personas; Failure Mode Analysis; Self-Consistency Validation; Socratic Questioning; Critique and Refine
- Reshuffled Batch 2 methods: Pre-mortem Analysis; Occam's Razor Application; 5 Whys Deep Dive; Expert Panel Review; Challenge from Critical Perspective
- Findings summary:
  - The story already scoped the guarded query set and safe logging rules, but direct actor callers could still exploit ambiguous failure precedence if invalid cursor or payload validation happened before malformed-identity rejection.
  - Tests needed one explicit mixed-failure case so implementers preserve the intended ordering rather than only proving each failure mode separately.
  - Unknown-query preservation and normal controller authentication behavior should remain outside this actor-layer guardrail change.
- Changes applied:
  - Added AC8 to make malformed-user rejection the first observable result for known role-sensitive queries, even when the same envelope also contains invalid cursor, pagination, audit, or query payload data.
  - Added task and testing guidance for a focused failure-precedence regression test that expects `Forbidden` instead of cursor, audit-payload, parsing, or state-derived errors.
  - Added a defense-in-depth policy note clarifying that the actor must not use unauthenticated direct envelopes to probe validation or state behavior.
- Findings deferred:
  - Whether future role-sensitive query types should default to guarded behavior remains a later architecture decision.
  - Whether EventStore should introduce a distinct malformed-envelope public mapping remains outside this story.
  - Exact logging event names and test logger mechanics remain implementation details within existing repository conventions.
- Final recommendation: ready-for-dev
