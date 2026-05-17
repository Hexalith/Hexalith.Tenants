# Story 9.4: Actor-Layer Query Guardrails

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

## Story

As a platform operator,
I want projection actors to reject malformed or unauthenticated query envelopes defensively,
so that authorization assumptions remain protected even if a controller or caller bypasses the normal API boundary.

## Acceptance Criteria

1. Given a query envelope reaches `TenantsProjectionActor` with an empty or missing `UserId`, when the actor handles any role-sensitive query, then the actor rejects the query with a safe authorization failure instead of relying only on controller-layer checks.
2. Given a role-sensitive query is executed through the normal controller path, when the authenticated user ID is present, then existing successful query behavior remains unchanged.
3. Given a query envelope contains a user ID that is not authorized for the requested tenant data, when the actor evaluates the query, then no tenant data is returned outside the caller's allowed scope.
4. Given actor-layer guardrails reject a query, when the failure is logged, then logs include correlation metadata but do not expose tenant membership details or sensitive payload data.
5. Given focused actor tests run, when empty-user, missing-user, unauthorized-user, and valid-user query paths are exercised, then tests verify defense-in-depth behavior without weakening existing controller authorization tests.

## Tasks / Subtasks

- [ ] Add actor-level authenticated-user validation for all role-sensitive tenant queries. (AC: 1, 2, 4)
  - [ ] In `TenantsProjectionActor.ExecuteQueryAsync`, validate `envelope.UserId` before dispatching known role-sensitive query handlers: `get-tenant`, `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit`.
  - [ ] Treat `null`, empty, and whitespace-only `UserId` as an authorization failure even though the public `QueryEnvelope` constructor already rejects blank values.
  - [ ] Return `new QueryResult(false, default, ErrorMessage: QueryAdapterFailureReason.Forbidden)` or the existing equivalent `"Forbidden"` value so `SubmitQueryHandler` maps the failure to the established safe 403 ProblemDetails path.
  - [ ] Keep unknown query-type behavior unchanged; this story is not a query-type taxonomy refactor.
- [ ] Add safe actor warning logging for rejected malformed-auth query envelopes. (AC: 4)
  - [ ] Add a source-generated `LoggerMessage` warning on `TenantsProjectionActor` with correlation ID, query type, aggregate ID, and a fixed stage such as `TenantQueryEnvelopeAuthorization`.
  - [ ] Do not log `Payload`, cursor text, protected cursor payloads, signing material, `UserTenants`, membership lists, or target-user membership details.
  - [ ] Keep the message generic, for example "Tenant query envelope rejected before authorization because authenticated user id was missing."
- [ ] Preserve existing valid-user and unauthorized-user behavior. (AC: 2, 3)
  - [ ] Do not move or weaken `IsAuthorizedForTenantAsync`, `IsGlobalAdminAsync`, `GetUserTenantIds`, or `GetVisibleUserTenants`.
  - [ ] Preserve `get-user-tenants` timing-uniformity behavior: cross-user lookups still run the global-admin check before returning an empty result for missing target users.
  - [ ] Preserve Story 9.1 signed cursor behavior and Story 9.2 current-state keyset continuation behavior.
  - [ ] Preserve Story 9.3 disabled-tenant visibility and orphan-membership filtering policy when that story is implemented.
- [ ] Add focused actor tests for malformed and valid envelopes. (AC: 1-5)
  - [ ] In `TenantsProjectionActorTests`, add tests that create a valid `QueryEnvelope` and then use record `with` initialization to set `UserId = ""`, whitespace, and `null!` for at least `get-tenant`, `list-tenants`, `get-tenant-users`, `get-user-tenants`, and `get-tenant-audit`.
  - [ ] Assert malformed-user cases return unsuccessful `QueryResult` with `ErrorMessage` equal to `QueryAdapterFailureReason.Forbidden` or `"Forbidden"` and do not return payload bytes.
  - [ ] Assert the actor does not call DAPR state reads for rejected malformed-user envelopes where possible, so the guardrail happens before projection state access.
  - [ ] Keep existing unauthorized-but-present-user tests green, such as `GetTenant_unauthorized_user_returns_forbiddenAsync`, `GetTenantAudit_non_admin_returns_forbidden_not_501Async`, non-owner `get-user-tenants`, and non-admin `list-tenants` filtering tests.
  - [ ] Add or update a logger-capture test only if the repository already has a lightweight test logger pattern; otherwise keep logging verification to "does not throw and returns Forbidden" to avoid adding test-only infrastructure.
- [ ] Keep controller and public contract scope tight. (AC: 2, 5)
  - [ ] Do not change `TenantsQueryController` normal `sub` extraction or its existing `Unauthorized()` behavior for HTTP requests missing an authenticated subject.
  - [ ] Do not change `QueryEnvelope` public constructor or DataContract shape unless implementation proves actor-side validation is impossible without it.
  - [ ] Do not add package dependencies or update package versions for this story.

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
- Use `string.IsNullOrWhiteSpace(envelope.UserId)` and handle `null` defensively despite the non-nullable property declaration.
- Add a small private helper such as `IsRoleSensitiveQuery(string? queryType)` only if it keeps the switch readable. Do not introduce a broad authorization framework for this story.
- Keep activity and metrics behavior coherent: rejected known role-sensitive queries should still record the query type duration, but should not require projection state access.
- Do not log `envelope.ToString()` for this guard. It redacts payload bytes but still contains user and entity identifiers; use explicit structured fields instead.
- Do not modify `Hexalith.EventStore` for this story. If `Forbidden` mapping is insufficient in a future EventStore version, record that as a deferred dependency rather than expanding scope here.

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

### Completion Notes List

### File List
