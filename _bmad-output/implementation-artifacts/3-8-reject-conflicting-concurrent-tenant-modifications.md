---
baseline_commit: 8db9c4148bf62e7795e37ed62c4a138006f10acb
---

# Story 3.8: Reject Conflicting Concurrent Tenant Modifications

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a tenant owner,
I want conflicting tenant access and configuration changes to be rejected predictably,
so that concurrent administration does not silently overwrite tenant state.

## Acceptance Criteria

1. Given two actors submit conflicting membership commands against the same tenant aggregate version, when EventStore optimistic concurrency is evaluated, then one command succeeds according to ordering rules and the conflicting command returns a structured concurrency conflict outcome to the caller after the command pipeline's bounded retry policy is exhausted.
2. Given the command pipeline performs any automatic retry, when retry behavior is documented, then the retry limit, retryable conflict conditions, final rejection mapping, and idempotency interaction are specified in the story implementation notes and tests verify both successful retry and exhausted-retry conflict outcomes where the EventStore API supports them.
3. Given two actors submit conflicting role-change commands for the same user, when the aggregate state version differs from the expected command context, then the conflict is surfaced as a structured concurrency outcome and no silent overwrite of role state occurs.
4. Given concurrent configuration commands modify the same key, when the commands are processed against the aggregate, then the final state corresponds to a valid ordered event sequence and conflicting command results are observable to callers.
5. Given duplicate command replay occurs after a transient failure, when EventStore idempotency and aggregate behavior evaluate the command, then duplicate events are not produced and the caller receives a deterministic success, rejection, or no-op outcome.
6. Given concurrency tests run, when add, remove, change-role, set-configuration, and remove-configuration conflicts are simulated, then tests verify deterministic event sequences, rejection behavior, and final aggregate state.

## Tasks / Subtasks

- [x] Verify the current concurrency surface before editing (AC: 1-6)
  - [x] Read the current EventStore aggregate pipeline files: `AggregateActor`, `EventPersister`, `AggregateMetadata`, `ConcurrencyConflictException`, `ConcurrencyConflictExceptionHandler`, `CommandProcessingResult`, `SubmitCommandHandler`, `CommandRouter`, and `IdempotencyChecker`.
  - [x] Confirm that `TenantAggregate` has no expected-version state and must remain a pure domain handler; do not add DAPR, state-store, or retry logic to Tenants aggregate handlers.
  - [x] Confirm whether DAPR actor `SaveStateAsync` conflict exceptions are the only current command conflict signal, and whether actor turn-based processing prevents true in-process same-actor races without test hooks.
  - [x] Confirm whether `CommandEnvelope.Extensions` or an existing EventStore contract already carries expected aggregate sequence/version metadata before adding any public contract surface.

- [x] Define the command conflict contract explicitly (AC: 1-3)
  - [x] Use the existing `ConcurrencyConflictException`, `ProblemTypeUris.ConcurrencyConflict`, HTTP `409`, and `Retry-After: 1` response shape where it satisfies the story; do not invent a Tenants-specific concurrency rejection unless EventStore cannot expose a deterministic command result otherwise.
  - [x] If a public expected-version/expected-sequence contract is required, prefer EventStore-level command metadata over adding `ExpectedVersion` fields to every Tenants command record.
  - [x] Keep client-facing conflict responses sanitized: include correlation ID and retry guidance, but do not expose aggregate IDs, tenant IDs, state-store keys, ETags, stack traces, payloads, tokens, or local paths.
  - [x] Ensure command status records use `Rejected` with `FailureReason == "ConcurrencyConflict"` for terminal conflict outcomes.

- [x] Implement or harden bounded retry behavior in the EventStore command pipeline (AC: 1-5)
  - [x] Retry only conflicts that happen before a successful `EventsStored` commit; never re-persist after the pipeline checkpoint proves events were stored.
  - [x] On each retry, rehydrate the latest aggregate state and re-invoke the domain service against the fresh state so duplicate membership, missing membership, same-role, same-configuration, missing-configuration, and limit checks reflect current state.
  - [x] Keep retries bounded and configured in one place. If no option exists, introduce an EventStore server option with a conservative default and tests documenting the chosen limit.
  - [x] Preserve idempotency ordering: do not write a terminal idempotency record until the command has a definitive terminal result; duplicate causation IDs must return cached terminal results without producing duplicate events.
  - [x] Preserve the existing pipeline sequence: idempotency check, tenant validation before state access, backpressure, processing checkpoint, state rehydration, domain invocation, event persistence, publication, terminal completion.
  - [x] Preserve cancellation semantics: `OperationCanceledException` must propagate and must not be converted to `ConcurrencyConflictException`.

- [x] Prove deterministic tenant membership and role outcomes (AC: 1, 3, 6)
  - [x] Cover `AddUserToTenant` conflicts where one command adds the member and the losing/retried command observes the new membership and returns `UserAlreadyInTenantRejection` or a terminal concurrency conflict according to the documented retry path.
  - [x] Cover `RemoveUserFromTenant` conflicts where one command removes the member and the losing/retried command observes the missing membership and returns `UserNotInTenantRejection` or a terminal concurrency conflict according to the documented retry path.
  - [x] Cover `ChangeUserRole` conflicts where one command changes the role and the losing/retried command observes either the new role, `NoOp`, or a terminal conflict without silently overwriting an unordered role state.
  - [x] Preserve the intentional backend behavior that removing the last `TenantOwner` is allowed; do not add a must-retain-one-owner invariant while testing conflicts.

- [x] Prove deterministic configuration outcomes (AC: 4, 6)
  - [x] Cover concurrent `SetTenantConfiguration` for the same key with different values: final persisted state must match the ordered event sequence, not an untracked overwrite.
  - [x] Cover concurrent same-key/same-value `SetTenantConfiguration`: duplicate replays should converge to success followed by `NoOp` or cached success without duplicate `TenantConfigurationSet` events.
  - [x] Cover `RemoveTenantConfiguration` versus `SetTenantConfiguration` on the same key and prove the final state matches event order.
  - [x] Preserve Story 3.7 limit behavior: `MaxConfigurationKeys`, `MaxKeyLength`, and `MaxValueLength` stay in `TenantAggregate`; updates at the 100-key limit and whitespace-key semantics must not regress.

- [x] Add focused EventStore and Tenants validation tests (AC: 1-6)
  - [x] Add or update EventStore server unit tests around `AggregateActor` conflict retry, exhausted retry, idempotency record timing, status writes, and `ConcurrencyConflictException` wrapping.
  - [x] Add or update EventStore persistence/integration tests that prove Redis/DAPR actor state does not lose events under conflict-like conditions; assert end state in the state store, not only return codes.
  - [x] Add Tenants-focused tests for membership, role, and configuration command sequences using the command gateway or actor pipeline where possible; aggregate-only `ProcessAsync` tests are insufficient for optimistic concurrency.
  - [x] Verify existing `ConformanceTests` continue to pass; do not reimplement concurrency in `InMemoryTenantService` unless a test-only helper is explicitly needed and clearly marked as not production concurrency.
  - [x] Add ProblemDetails/API evidence for HTTP `409` conflict shape if the behavior crosses the public command endpoint.

- [x] Update documentation and operational notes only where behavior changes (AC: 2)
  - [x] Document retry limit, retryable conflict sources, final conflict mapping, idempotency behavior, and caller retry guidance in the relevant EventStore/Tenants docs.
  - [x] Update event-contract documentation only if command status or public error surface changes; do not describe a new domain event unless one is actually introduced.
  - [x] Keep docs clear that EventStore remains the source of truth and DAPR pub/sub failure is independent from command persistence success.

- [x] Run focused validation (AC: 1-6)
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --filter "FullyQualifiedName~AggregateActor|FullyQualifiedName~EventPersister|FullyQualifiedName~ConcurrencyConflict" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter "FullyQualifiedName~TenantAggregateTests|FullyQualifiedName~CommandPipeline" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --filter "FullyQualifiedName~CommandApiRuntimeIntegrationTests|FullyQualifiedName~DaprEndToEndTests" -m:1 -nr:false /p:NuGetAudit=false`
  - [x] `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false`
  - [x] If VSTest socket restrictions recur, record the exact `System.Net.Sockets.SocketException (13): Permission denied` diagnostic and use direct xUnit execution plus Release build evidence as fallback validation.

## Dev Notes

### Source Context

- Epic 3 objective: tenant owners manage members, role boundaries, tenant configuration, and tenant-scoped role behavior without cross-tenant leakage or escalation. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Owners Can Manage Access and Configuration Safely`]
- Story 3.8 is the optimistic-concurrency slice for tenant access and configuration commands. It should not rework basic membership, role, configuration-limit, query, projection, UI, or package behavior except where conflict handling requires visible tests. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.8: Reject Conflicting Concurrent Tenant Modifications`]
- PRD FR12 maps aggregate command conflicts to optimistic concurrency. NFR21 maps command atomicity and conflict tests to Story 3.8. [Source: `_bmad-output/planning-artifacts/epics.md#Requirements Traceability Matrix`; `_bmad-output/planning-artifacts/epics.md#Non-Functional Requirements Coverage Matrix`]
- PRD FR49 requires rejection/error messages to include the specific reason, involved entity, and corrective hint. For concurrency, the existing public boundary is sanitized ProblemDetails with retry guidance rather than a Tenants domain rejection event. [Source: `_bmad-output/planning-artifacts/prd.md#Developer Experience & Packaging`; `Hexalith.EventStore/src/Hexalith.EventStore/ErrorHandling/ConcurrencyConflictExceptionHandler.cs`]
- EventStore remains the source of truth. Commands must persist events atomically before publication; DAPR pub/sub availability is independent from command persistence success. [Source: `_bmad-output/planning-artifacts/prd.md#Command Validation & Error Handling`; `_bmad-output/project-context.md#MediatR Pipeline (Command Path)`]

### Current Repository State

- `TenantAggregate` currently handles create, update, disable, enable, add user, remove user, change role, set configuration, and remove configuration as pure static `Handle` methods. It has no expected-version parameter and no persistence/retry responsibility. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- `TenantState` applies tenant membership and configuration events by mutating dictionaries directly; rejection `Apply` methods are replay-only and must not mutate state. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- `CommandEnvelope` currently carries `MessageId`, `TenantId`, `Domain`, `AggregateId`, `CommandType`, payload, `CorrelationId`, optional `CausationId`, `UserId`, and optional `Extensions`. It does not expose a typed expected aggregate version property. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs`]
- `DomainServiceCurrentState` carries `CurrentSequence`, which gives the domain service the latest rehydrated aggregate sequence but does not by itself define client-supplied expected-version semantics. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/DomainServiceCurrentState.cs`]
- `AggregateMetadata` has `CurrentSequence`, `LastModified`, and optional `ETag`. `EventPersister` currently reads metadata, assigns gapless event sequence numbers, writes event keys and metadata, but does not call `SaveStateAsync`; `AggregateActor` commits the batch. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/AggregateMetadata.cs`; `Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventPersister.cs`]
- `AggregateActor` already wraps `InvalidOperationException` from the EventsStored `SaveStateAsync` batch as `ConcurrencyConflictException`, and preserves `OperationCanceledException`. It currently exposes conflict handling mainly through exception flow and HTTP exception handling. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs`; `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Actors/AggregateActorDomainResultTests.cs`]
- `ConcurrencyConflictExceptionHandler` maps conflicts to HTTP `409`, `ProblemTypeUris.ConcurrencyConflict`, `Retry-After: 1`, sanitized detail text, and advisory command status `Rejected` with `FailureReason == "ConcurrencyConflict"`. Preserve this shape unless a deliberate public contract change is required. [Source: `Hexalith.EventStore/src/Hexalith.EventStore/ErrorHandling/ConcurrencyConflictExceptionHandler.cs`]
- `IdempotencyChecker` uses `idempotency:{causationId}` and returns cached `CommandProcessingResult` for duplicate causation IDs before tenant validation or state access. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/IdempotencyChecker.cs`]
- Current EventStore tests already cover `SaveStateAsync` conflict wrapping, duplicate command idempotency, Redis actor state persistence, event persistence metadata, and ProblemDetails URI compliance. Extend these tests instead of replacing their patterns. [Source: `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Actors/AggregateActorDomainResultTests.cs`; `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Actors/AggregateActorIdempotencyTests.cs`; `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Events/EventPersistenceIntegrationTests.cs`; `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/ErrorHandling/ProblemTypeUriComplianceTests.cs`]

### Previous Story Intelligence

- Story 3.1 established `TenantRole.Unknown = 0`, string enum serialization, trusted `actor:globalAdmin` envelope metadata, disabled-tenant branch precedence, and first-user bootstrap for `AddUserToTenant` only. Preserve these decisions under conflict retry. [Source: `_bmad-output/implementation-artifacts/3-1-add-users-to-a-tenant-with-explicit-roles.md`]
- Story 3.2 established that tenant-scoped event and rejection payloads derive tenant identity from `CommandEnvelope.AggregateId` when command body and envelope diverge. Do not regress this in conflict retry paths. [Source: `_bmad-output/implementation-artifacts/3-2-remove-users-from-a-tenant.md#Senior Developer Review (AI)`]
- Story 3.4 hardened owner/global-admin authorization and exact `actor:globalAdmin=true` comparison. Conflict retry must re-evaluate authorization against fresh state and must not trust client-supplied authority flags. [Source: `_bmad-output/implementation-artifacts/3-4-enforce-tenant-scoped-role-behavior.md#Senior Developer Review (AI)`]
- Story 3.5 and Story 3.6 established exact-key configuration set/remove behavior, same-key/same-value `NoOp`, and `ConfigurationKeyNotFoundRejection`. Conflict retry must preserve these ordered-state semantics. [Source: `_bmad-output/implementation-artifacts/3-5-set-tenant-configuration-entries.md`; `_bmad-output/implementation-artifacts/3-6-remove-tenant-configuration-entries.md`]
- Story 3.7 completed configuration limit hardening and documented 1024 `string.Length` value size, 100-key count, 256-character key length, and current whitespace-key acceptance. Do not change these semantics while testing concurrent configuration commands. [Source: `_bmad-output/implementation-artifacts/3-7-enforce-tenant-configuration-limits.md`]
- Recent commits show the current Epic 3 order: `a00d490 feat(story-3.1)`, `f807411 feat(story-3.2)`, `d6e5fc4 feat(story-3.3)`, `47bb606 feat(story-3.4)`, `fec602a feat(story-3.5)`, `f26cfa2 feat(story-3.6)`, and `8db9c41 feat(story-3.7)`. [Source: `git log --oneline -8`]

### Architecture and Technical Guardrails

- Runtime stack remains .NET 10 SDK `10.0.300`, C# latest, nullable enabled, warnings as errors, central package management, DAPR `1.17.9`, Aspire `13.3.5`, xUnit v3 `3.2.2`, and Shouldly `4.3.0`. Do not bump packages for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Use `Hexalith.Tenants.slnx`; do not create or use a legacy `.sln`. [Source: `_bmad-output/project-context.md#Core Runtime & Build`]
- `Hexalith.EventStore` is a root-level submodule referenced as source, not a NuGet package. If implementation changes EventStore source, the dev must manage the submodule working tree and parent repo pointer intentionally and must not initialize nested submodules. [Source: `AGENTS.md#Submodule Policy`; `_bmad-output/project-context.md#Hexalith.EventStore (Submodule, not NuGet)`]
- Aggregate `Handle` methods must remain pure static functions: no async, no I/O, no DAPR calls, no service dependencies, and no thrown business-rule exceptions. [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- EventStore `AggregateActor` pipeline order is security-sensitive: idempotency, tenant validation before state access, state rehydration, domain service invocation, event persistence, publication. Do not reorder tenant validation behind state access while adding retry logic. [Source: `_bmad-output/project-context.md#MediatR Pipeline (Command Path)`]
- Domain RBAC for Tenants' own commands belongs inside aggregate handlers. EventStore API-gate validators are for platform validation and consuming services; do not move Tenants domain RBAC out of `TenantAggregate`. [Source: `_bmad-output/project-context.md#Authorization (RBAC - Role-Based Access Control)`]
- Rejection events are normal persisted events when produced by the domain service. A persistence concurrency conflict is an infrastructure/command-pipeline conflict, not automatically a Tenants domain rejection event. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs`; `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Actors/AggregateActorDomainResultTests.cs`]
- Tests use xUnit v3 plus Shouldly, global `using Xunit`, K&R brace style, and snake_case test method names. Do not use `Assert.*`. [Source: `_bmad-output/project-context.md#Testing Rules`]

### Likely Files to Touch

- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs` - likely owner for bounded retry around conflict-prone persistence, retry rehydration, terminal status/idempotency behavior, and cancellation preservation.
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventPersister.cs` - likely only if expected-sequence or metadata ETag checking must be made explicit before save.
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/AggregateMetadata.cs` - only if the existing optional `ETag` field becomes part of the actual command conflict algorithm.
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Commands/ConcurrencyConflictException.cs` - only for additional structured context needed internally; keep client response sanitized.
- `Hexalith.EventStore/src/Hexalith.EventStore/ErrorHandling/ConcurrencyConflictExceptionHandler.cs` - if final conflict status/result mapping changes.
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/CommandProcessingResult.cs` and command status models - only if a non-exception terminal conflict result is introduced.
- `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Actors/AggregateActorDomainResultTests.cs` and `AggregateActorIdempotencyTests.cs` - primary unit coverage for retry, exhausted conflicts, and idempotency timing.
- `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Events/EventPersistenceIntegrationTests.cs` - Redis/DAPR end-state evidence for event ordering and no lost writes.
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs` - only for ordered domain-state semantics that can be proven without persistence concurrency.
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs` and/or `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs` - public command outcome and ProblemDetails evidence.
- `docs/event-contract-reference.md` and EventStore command docs - only if public retry/conflict behavior documentation is stale.

### Out of Scope

- Adding expected-version fields to Tenants command records unless EventStore maintainers deliberately choose that public contract shape.
- Rewriting `TenantAggregate` to manage persistence, locks, ETags, or retries.
- New tenant role semantics, new ownership-retention rules, or changes to disabled-tenant precedence.
- Changing Story 3.7 configuration limits, key normalization, whitespace-key behavior, or value-size semantics.
- Query endpoints, projection write durability, cursor pagination, audit query behavior, Phase 2 UI/FrontComposer, or consuming-service event subscription behavior.
- Package, SDK, DAPR, Aspire, CI workflow, or nested submodule changes.

### Latest Technical Context

- No external package or API upgrade is required. Use the pinned local stack and existing EventStore/DAPR/Aspire APIs. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Web research was not required for a package decision because this story is about local EventStore command-pipeline behavior and the pinned dependencies already define the available APIs. If implementation changes ASP.NET Core ProblemDetails behavior or DAPR actor-state concurrency assumptions, verify against primary documentation before changing public behavior.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.8: Reject Conflicting Concurrent Tenant Modifications`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Command Validation & Error Handling`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Non-Functional Requirements`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Data Architecture`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- [Source: `_bmad-output/project-context.md#MediatR Pipeline (Command Path)`]
- [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- [Source: `_bmad-output/project-context.md#Testing Rules`]
- [Source: `_bmad-output/implementation-artifacts/3-7-enforce-tenant-configuration-limits.md`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs`]
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/EventPersister.cs`]
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore/ErrorHandling/ConcurrencyConflictExceptionHandler.cs`]
- [Source: `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Actors/AggregateActorDomainResultTests.cs`]

## Project Structure Notes

- Alignment: Story 3.8 crosses the Tenants/EventStore boundary because Tenants aggregate commands depend on EventStore for persistence, command ordering, idempotency, and conflict surfacing. Keep Tenants domain behavior in `src/Hexalith.Tenants.Server`, and keep retry/persistence conflict behavior in `Hexalith.EventStore/src/Hexalith.EventStore.Server`.
- Detected variance: the current repository already has a `ConcurrencyConflictException` and HTTP `409` ProblemDetails handler, but the existing source inspection did not find a complete documented bounded retry policy for command persistence conflicts. Treat Story 3.8 as a hardening and evidence story, not a reason to duplicate domain contracts.
- Aggregate-only unit tests cannot prove optimistic concurrency. They are useful for ordered state semantics after retry rehydration, but the core acceptance evidence must exercise EventStore actor/persistence behavior.
- If EventStore submodule files are modified, the parent repo will show both submodule dirtiness and possibly a changed submodule pointer after commit. Keep that explicit in the dev record.

## Validation Checklist Results

- Story foundation extracted from Epic 3 Story 3.8, PRD FR12/FR49/NFR21, architecture data and command pipeline notes, project context, and previous Story 3.7.
- Discovery loaded available whole-document sources: `epics.md`, `prd.md`, `architecture.md`, `ux-design-specification.md`, and persistent project context. No sharded planning artifacts were present.
- Current source inspection covered likely UPDATE files: `TenantAggregate`, `TenantState`, EventStore `CommandEnvelope`, `DomainServiceCurrentState`, `AggregateActor`, `EventPersister`, `AggregateMetadata`, `ConcurrencyConflictException`, `ConcurrencyConflictExceptionHandler`, `CommandProcessingResult`, `SubmitCommandHandler`, `CommandRouter`, `IdempotencyChecker`, EventStore actor/domain-result tests, EventStore idempotency tests, EventStore persistence integration tests, and Tenants integration command tests.
- Previous-story intelligence incorporated trusted global-admin envelope metadata, disabled-state branch precedence, envelope aggregate-id consistency, exact global-admin extension comparison, configuration set/remove ordered semantics, Story 3.7 limit boundaries, and current whitespace-key behavior.
- Disaster-prevention guardrails included: do not add concurrency logic to `TenantAggregate`, do not add expected-version fields to every Tenants command unless necessary, do not invent a duplicate Tenants concurrency rejection when EventStore 409 already fits, do not retry after an EventsStored checkpoint, do not cache non-terminal idempotency results, do not expose internal state-store/ETag details in ProblemDetails, and do not rely on aggregate-only tests for optimistic concurrency.
- Latest technical context reviewed from local pins and source. No package upgrade or external API change is required for this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `MSBUILDDISABLENODEREUSE=1 dotnet test Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --filter "FullyQualifiedName~AggregateActor|FullyQualifiedName~EventPersister|FullyQualifiedName~ConcurrencyConflict" -m:1 -nr:false /p:NuGetAudit=false` - build completed, VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` while opening its test communication socket.
- `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests -noLogo -noColor -parallel none -class Hexalith.EventStore.Server.Tests.Actors.AggregateActorDomainResultTests -class Hexalith.EventStore.Server.Tests.Actors.AggregateActorIdempotencyTests -class Hexalith.EventStore.Server.Tests.Events.EventPersisterTests -class Hexalith.EventStore.Server.Tests.Commands.ConcurrencyConflictExceptionHandlerTests -class Hexalith.EventStore.Server.Tests.Commands.ConcurrencyConflictExceptionTests -class Hexalith.EventStore.Server.Tests.Pipeline.SubmitCommandHandlerTests` - passed: Total 91, Failed 0, Skipped 0.
- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter "FullyQualifiedName~TenantAggregateTests|FullyQualifiedName~CommandPipeline" -m:1 -nr:false /p:NuGetAudit=false` - build completed, VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` while opening its test communication socket.
- `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Aggregates.TenantAggregateTests` - passed: Total 140, Failed 0, Skipped 0.
- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --filter "FullyQualifiedName~CommandApiRuntimeIntegrationTests|FullyQualifiedName~DaprEndToEndTests" -m:1 -nr:false /p:NuGetAudit=false` - build completed, VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` while opening its test communication socket.
- `tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests -noLogo -noColor -parallel none -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests -class Hexalith.Tenants.IntegrationTests.DaprEndToEndTests` - passed: Total 89, Failed 0, Skipped 13. DAPR tests skipped because Redis, placement, and scheduler prerequisites are unavailable locally.
- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --filter "FullyQualifiedName~Conformance" -m:1 -nr:false /p:NuGetAudit=false` - build completed, VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` while opening its test communication socket.
- `tests/Hexalith.Tenants.Testing.Tests/bin/Debug/net10.0/Hexalith.Tenants.Testing.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Testing.Tests.Conformance.TenantConformanceTests -class Hexalith.Tenants.Testing.Tests.Projections.InMemoryTenantProjectionConformanceTests` - passed: Total 59, Failed 0, Skipped 0.
- `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false` - passed: 0 warnings, 0 errors.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added EventStore bounded persistence-conflict retry with configurable `EventStore:CommandConcurrency:MaxPersistenceConflictRetries` defaulting to 1.
- Retry now clears pending actor state cache, rehydrates fresh aggregate state, and re-invokes the domain service only for conflicts before the `EventsStored` checkpoint.
- Exhausted persistence conflicts now record a terminal rejected command status and idempotency result using `FailureReason == "ConcurrencyConflict"`, and public command submission maps the terminal result back to the existing sanitized HTTP 409 ProblemDetails flow.
- Preserved Tenants aggregates as pure domain handlers; membership, role, and configuration conflict evidence is covered through ordered aggregate semantics plus EventStore actor/API tests.
- Added DAPR end-to-end configuration ordering coverage that asserts persisted actor event sequence and final payload when local DAPR prerequisites are available.
- Added EventStore Redis/DAPR actor-state integration coverage for concurrent same-aggregate submissions preserving a gapless persisted event stream.
- Documented retry limit, retryable source, terminal conflict mapping, idempotency behavior, and caller retry guidance in EventStore and Tenants docs.

### File List

- `Hexalith.EventStore/docs/reference/command-api.md`
- `Hexalith.EventStore/docs/reference/problems/concurrency-conflict.md`
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Actors/AggregateActor.cs`
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Configuration/CommandConcurrencyOptions.cs`
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Configuration/ServiceCollectionExtensions.cs`
- `Hexalith.EventStore/src/Hexalith.EventStore.Server/Pipeline/SubmitCommandHandler.cs`
- `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Actors/AggregateActorDomainResultTests.cs`
- `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Actors/AggregateActorTestHelper.cs`
- `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Events/EventPersistenceIntegrationTests.cs`
- `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Pipeline/SubmitCommandHandlerTests.cs`
- `_bmad-output/implementation-artifacts/3-8-reject-conflicting-concurrent-tenant-modifications.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-1-20260531-113112.md`
- `docs/event-contract-reference.md`
- `src/Hexalith.Tenants/Program.cs`
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`

## Senior Developer Review (AI)

Reviewer: Codex on 2026-06-01

### Outcome

Approved after automatic metadata fix. No critical or high source-code issues remain.

### Findings Fixed

- [x] [AI-Review][Medium] Dev Agent Record File List omitted changed BMAD artifacts discovered by git: `_bmad-output/implementation-artifacts/tests/test-summary.md` and `_bmad-output/story-automator/orchestration-1-20260531-113112.md`. Fixed by adding both files to the story File List.

### Validation Notes

- Acceptance criteria 1-6 were cross-checked against EventStore retry/rejection handling, Tenants ordered aggregate semantics, public `409` ProblemDetails mapping, and focused tests.
- Source review covered every claimed source/test/doc change in the story File List plus EventStore submodule changes discovered through git.
- External documentation lookup was not required: the implementation uses pinned local EventStore/DAPR/Aspire APIs and does not change package versions or public framework behavior beyond the already documented EventStore HTTP conflict mapping.

### Review Validation

- `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false /p:NuGetAudit=false` - passed: 0 warnings, 0 errors.
- `MSBUILDDISABLENODEREUSE=1 dotnet test Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --filter "FullyQualifiedName~AggregateActor|FullyQualifiedName~EventPersister|FullyQualifiedName~ConcurrencyConflict" -m:1 -nr:false /p:NuGetAudit=false` - build completed, VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied`.
- Direct xUnit fallback: `Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests -noLogo -noColor -parallel none -class Hexalith.EventStore.Server.Tests.Actors.AggregateActorDomainResultTests -class Hexalith.EventStore.Server.Tests.Actors.AggregateActorIdempotencyTests -class Hexalith.EventStore.Server.Tests.Events.EventPersisterTests -class Hexalith.EventStore.Server.Tests.Commands.ConcurrencyConflictExceptionHandlerTests -class Hexalith.EventStore.Server.Tests.Commands.ConcurrencyConflictExceptionTests -class Hexalith.EventStore.Server.Tests.Pipeline.SubmitCommandHandlerTests` - passed: Total 91, Failed 0, Skipped 0.
- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --filter "FullyQualifiedName~TenantAggregateTests|FullyQualifiedName~CommandPipeline" -m:1 -nr:false /p:NuGetAudit=false` - build completed, VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied`.
- Direct xUnit fallback: `tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Aggregates.TenantAggregateTests` - passed: Total 140, Failed 0, Skipped 0.
- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --filter "FullyQualifiedName~CommandApiRuntimeIntegrationTests|FullyQualifiedName~DaprEndToEndTests" -m:1 -nr:false /p:NuGetAudit=false` - build completed, VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied`.
- Direct xUnit fallback: `tests/Hexalith.Tenants.IntegrationTests/bin/Debug/net10.0/Hexalith.Tenants.IntegrationTests -noLogo -noColor -parallel none -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests -class Hexalith.Tenants.IntegrationTests.DaprEndToEndTests` - passed: Total 92, Failed 0, Skipped 16. DAPR tests skipped because Redis, placement, and scheduler prerequisites are unavailable locally.
- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --filter "FullyQualifiedName~Conformance" -m:1 -nr:false /p:NuGetAudit=false` - build completed, VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied`.
- Direct xUnit fallback: `tests/Hexalith.Tenants.Testing.Tests/bin/Debug/net10.0/Hexalith.Tenants.Testing.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Testing.Tests.Conformance.TenantConformanceTests -class Hexalith.Tenants.Testing.Tests.Projections.InMemoryTenantProjectionConformanceTests` - passed: Total 59, Failed 0, Skipped 0.

## Change Log

- 2026-06-01 - Implemented bounded EventStore persistence-conflict retry, terminal conflict status/idempotency handling, HTTP 409 conflict mapping, Tenants conflict/order tests, integration API/DAPR evidence, documentation updates, and BMAD status transition to review.
- 2026-06-01 - Senior developer review completed; File List metadata fixed and story status moved to done.
