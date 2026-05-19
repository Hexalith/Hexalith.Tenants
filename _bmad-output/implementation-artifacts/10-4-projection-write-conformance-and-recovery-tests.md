# Story 10.4: Projection Write Conformance and Recovery Tests

Status: done

Completion note: Deterministic projection write conformance and recovery tests implemented and reviewed; post-review patches applied (12 of 17 actionable, 1 dismissed, 4 from resolved decisions). Conformance fixture refactored to drive production behavior exclusively through `ProjectAsync` after `ApplyIndexEvent` visibility reverted to `private`. Full solution test gate passes (660 passed, 1 skipped).

## Story

As a developer maintaining tenant projections,
I want focused conformance and recovery tests for projection persistence behavior,
so that future projection changes cannot reintroduce silent write loss, ordering errors, or recovery gaps.

## Acceptance Criteria

1. Given Stories 10.1 and 10.2 have selected and implemented stable projection write concurrency/retry contracts, when conformance tests run against tenant detail, tenant index, and audit projection writes, then each projection proves it preserves all successfully processed events under concurrent update and replay conditions without freezing provisional implementation details.
2. Given projection writes encounter transient guarded-persistence conflicts, when retry behavior is exercised in tests, then the tests verify the projection eventually succeeds or reports a safe observable failure according to the implemented policy.
3. Given projection writes fail after retry exhaustion, when recovery behavior is tested, then the failure path does not claim success and leaves safe diagnostic evidence sufficient for replay or repair without logging payload bodies, serialized event content, tenant display names, user-facing labels, cursor payloads, or other user-controllable values.
4. Given projection event ordering matters for cursor and audit behavior, when tests project mixed lifecycle, membership, configuration, and audit events, then tenant detail, tenant index, and audit read models preserve deterministic ordering for existing query responses.
5. Given future tenant projection implementations opt into the conformance suite, when they supply the required projection fixture contract, then the same concurrency and recovery expectations can be reused through deterministic conflict injection, retry exhaustion, per-key persisted-state inspection, write-order capture, diagnostics capture, and attempt counting without duplicating test logic.
6. Given a guarded save succeeds for one state key and a later key fails or is replayed, when recovery tests inspect the outcome, then tests assert no cross-key transactionality is claimed between tenant detail, tenant index, and audit writes, and replay/idempotency from Stories 10.1 and 10.2 prevents duplicate or lost entries.
7. Given the projection write helper or adapter exposes retry attempt boundaries, when deterministic tests simulate conflict-then-success and retry exhaustion, then exact read/save attempt counts, max-attempt behavior, and no-stale-instance reuse are verified through observable test seams such as write delegate invocations, loaded projection versions, persisted state, diagnostics, and exception/result shape.
8. Given audit entries are replayed or merged after conflict recovery, when duplicate `EventId` values are present, then tests assert the already persisted entry remains authoritative even if incoming payload content differs, the duplicate is suppressed because persisted state already contains it, and distinct same-timestamp events stay ordered by `Timestamp` then `EventId`.
9. Given prerequisite Stories 10.1 and 10.2 are verified, when this conformance story starts implementation, then the Dev Agent Record captures the exact evidence used: story implementation status or commit references, helper/adapter API names, retry limit, failure contract, and any accepted diagnostic contract.
10. Given deterministic state-store scripts drive conflict and recovery behavior, when tests execute, then scripted read/save outcomes are scoped by state key and attempt so an unexpected key, write order, stale model reuse, or extra write fails the test instead of being absorbed by a global sequence.
11. Given diagnostics are asserted in recovery tests, when failures are captured, then assertions prefer structured fields and safe categories over brittle full-message matching unless Stories 10.1 or 10.2 define exact message text as part of the accepted contract.

## Tasks / Subtasks

- [x] Confirm implementation prerequisites before writing conformance tests. (AC: 1, 2)
  - [x] Verify Story 10.1 has implemented guarded write behavior for `projection:tenants:{tenantId}` and `projection:tenant-index:singleton`.
  - [x] Verify Story 10.2 has implemented guarded write behavior and idempotent merge behavior for `audit:{tenantId}`.
  - [x] Record prerequisite evidence in the Dev Agent Record before adding conformance coverage: implementation status or commit references for Stories 10.1 and 10.2, helper/adapter API names, retry limit, failure shape, and accepted diagnostic expectations.
  - [x] If either story is not merged, implemented, or documented with a stable accepted projection persistence contract, stop before adding speculative tests and return this story to backlog or blocked with the missing prerequisite named.
  - [x] Do not create a partial conformance fixture, speculative helper surface, or skipped test suite when prerequisites are absent; leave a clear blocker instead.
  - [x] Record the concrete helper/adapter API and retry policy under test in test names or comments where the behavior would otherwise be ambiguous.
- [x] Build a reusable projection write conformance fixture in the test project. (AC: 1, 5, 7)
  - [x] Place test-only conformance helpers under `tests/Hexalith.Tenants.Server.Tests/Projections/` or a nearby test-support folder already used by this project.
  - [x] Keep the fixture internal to tests; do not add production abstractions solely for conformance reuse unless Stories 10.1 or 10.2 already introduced the internal adapter being tested.
  - [x] Model deterministic state-store reads, ETags, guarded-save results, and thrown infrastructure failures without live DAPR, Redis, Aspire, sleeps, or real parallelism.
  - [x] Let each projection scenario declare state key category, initial state, externally updated reload state, incoming event batch, expected saved state, conflict count, write ordering, and expected attempt counts.
  - [x] Scope scripted outcomes by state key and attempt number; fail fast on unexpected state keys, unexpected read/save order, extra writes after terminal failure, or attempts that reuse stale loaded state.
  - [x] Define attempt counting before assertions, including whether the initial try counts as attempt 1 for conflict-then-success and retry exhaustion cases.
  - [x] Make failure assertions reusable: no successful `ProjectionResponse`, safe structured diagnostic context, no event payload, serialized event body, tenant display name, user-facing label, cursor payload, or membership detail in captured logs, and no extra writes after the terminal failure.
- [x] Add tenant detail conformance tests. (AC: 1, 2, 3, 4, 7)
  - [x] Cover conflict-then-success for `projection:tenants:{tenantId}` and assert the final `TenantReadModel` contains the incoming lifecycle, membership, and configuration events exactly once.
  - [x] Cover existing-state ETag saves and missing-state first-write behavior as implemented by Story 10.1.
  - [x] Cover retry exhaustion and assert `ProjectAsync` fails through the existing projection failure path rather than returning success.
  - [x] Assert stale mutated model instances are not reused across retry attempts.
- [x] Add tenant index conformance tests. (AC: 1, 2, 3, 4, 6, 7)
  - [x] Cover conflict-then-success for `projection:tenant-index:singleton` where the retry reload contains another tenant update not present in the stale read.
  - [x] Assert the saved `TenantIndexReadModel` preserves previously indexed tenants plus incoming tenant, membership, removal, and role-change effects.
  - [x] Cover retry exhaustion after tenant detail success and assert the overall projection operation still fails without claiming cross-key atomic success.
  - [x] Verify deterministic query-facing ordering remains compatible with existing list/user-tenants query tests; do not change cursor format or query response DTOs.
- [x] Add audit conformance and recovery tests. (AC: 1, 2, 3, 4, 6, 8)
  - [x] Cover conflict-then-success for `audit:{tenantId}` where the retry reload contains an externally persisted audit entry.
  - [x] Assert original persisted entries, externally added entries, and incoming entries are present exactly once and sorted by `Timestamp` then `EventId`.
  - [x] Cover duplicate `EventId` collision and assert the persisted entry remains authoritative.
  - [x] If a duplicate `EventId` has mismatched incoming payload content, assert persisted state wins and record any diagnostic expectations only if Story 10.2 defines them.
  - [x] Cover replay after audit save plus later projection failure and assert audit idempotency prevents duplicate entries.
  - [x] Cover malformed payload preservation and invariant-failure no-save behavior already required by Story 10.2.
  - [x] Assert recovered audit records remain queryable through existing date-range and cursor behavior without adding new query APIs.
- [x] Keep scope boundaries explicit. (AC: 1-8)
  - [x] Do not modify `Hexalith.EventStore` or initialize/update nested submodules recursively.
  - [x] Do not add package dependencies or central package versions.
  - [x] Do not change production query routes, DTOs, cursor encoding, authorization policy, projection state key names, audit schema, or EventStore behavior.
  - [x] Do not introduce distributed locks, queue redesign, schema migrations, repair commands, admin UI, diagnostic query endpoints, a new storage abstraction, cross-key transaction support, or a broad diagnostic/logging refactor.
  - [x] Production code changes are acceptable only if needed to expose existing internal helper behavior to the test assembly in the narrowest way, for example `InternalsVisibleTo`, and only after confirming no better existing test seam is available; do not make behavioral changes or public APIs solely for test convenience.

## Dev Notes

### Dependency Gate

- This story is a conformance and recovery test story. It should not be implemented before Stories 10.1 and 10.2 have landed because the selected write policy, helper shape, retry limit, and failure contract are established there. [Source: `_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md`; `_bmad-output/implementation-artifacts/10-2-audit-projection-write-safety.md`]
- If 10.1 or 10.2 is still active, implementation may proceed only when the final projection persistence contract is explicitly documented and accepted; otherwise, avoid writing tests that reverse-engineer in-progress code. [Source: `2026-05-18 party-mode review`]
- If implementation starts while `TenantProjectionHandler.ProjectAsync` still uses plain `SaveStateAsync` for tenant detail, audit, or tenant index writes, stop and report the missing prerequisite instead of writing tests against speculative behavior. [Source: `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`]
- The prerequisite evidence should be explicit enough for review: identify the 10.1/10.2 implementation status or commits, the helper/adapter names under test, max-attempt behavior, retry-exhaustion failure shape, and any diagnostic fields that are contractually stable.
- Story 10.3B cancellation can be tested separately. This story may assert no extra writes after terminal failure or replay boundaries, but it should not add cancellation behavior unless 10.3B has already completed and the existing helper API naturally exposes it. [Source: `_bmad-output/implementation-artifacts/10-3b-cancellation-token-threading-for-tenant-projection-queries.md`]

### Current Code State

- `TenantProjectionHandler.ProjectAsync` currently builds a fresh `TenantReadModel`, saves `projection:tenants:{request.AggregateId}`, builds `TenantAuditReadModel`, saves `audit:{request.AggregateId}`, then loads and saves `projection:tenant-index:singleton`. Current code in this worktree still shows plain DAPR state calls; Stories 10.1 and 10.2 are expected to replace those with guarded retry behavior before this story is implemented. [Source: `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`]
- `TenantReadModel` owns tenant lifecycle, membership, and configuration mutation rules. Tests should assert final state through `TenantReadModel` behavior rather than duplicating projection logic in test helpers. [Source: `src/Hexalith.Tenants.Server/Projections/TenantReadModel.cs`]
- `TenantIndexReadModel` owns shared tenant index and user-to-tenant membership behavior. It ignores membership events for tenants not yet present and removes empty user membership maps. Conformance tests must preserve those semantics while asserting recovery behavior. [Source: `src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs`]
- `TenantAuditProjection.ProjectAuditEvents(...)` skips malformed JSON payloads but lets metadata invariant failures propagate through `TenantAuditReadModel.Apply(...)`. Audit conformance tests must not weaken that boundary. [Source: `src/Hexalith.Tenants.Server/Projections/TenantAuditProjection.cs`; `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`]
- `TenantAuditReadModel.SortEntries()` orders audit entries by `Timestamp` and then `EventId`. Recovery tests should assert that ordering after conflict reload and merge. [Source: `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`]
- Query behavior lives in `TenantsProjectionActor` and `TenantsQueryController`; this story should prove write-side recovery does not break existing query-facing order, cursor, pagination, authorization, or response semantics. [Source: `src/Hexalith.Tenants/Actors/TenantsProjectionActor.cs`; `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`]

### Architecture and Scope Boundaries

- Architecture mandates production-test parity and a reusable conformance test pattern for projection behavior. The conformance fixture should exercise the same projection write helper/adapter used in production, not a separate fake implementation of the retry algorithm. [Source: `_bmad-output/planning-artifacts/architecture.md#Testing Strategy Validation`; `_bmad-output/planning-artifacts/architecture.md#Gap Analysis Results`]
- Reliability requirements reinforce EventStore as source of truth and projection replay as the recovery mechanism. Tests should validate replay/idempotency behavior, not introduce compensating writes or repair endpoints. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Classification & Goals`; `_bmad-output/planning-artifacts/epics.md#Epic 10`]
- DAPR state-store ETags are per state entry. Tests must keep tenant detail, tenant index, and audit key expectations separate and must not imply cross-key transactionality. [Source: `_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md`; `_bmad-output/implementation-artifacts/10-2-audit-projection-write-safety.md`]
- Query cache ETags from `TenantsProjectionActor`/EventStore query caching are unrelated to DAPR state-store persistence ETags. Do not assert or modify query cache invalidation in this test story. [Source: `_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md`; `src/Hexalith.Tenants/Program.cs`]

### Implementation Guardrails

- Prefer testing the real internal projection write helper or DAPR state adapter introduced by Stories 10.1/10.2. If it is internal, use the repository's existing test-access pattern or the narrowest `InternalsVisibleTo` change; do not make production APIs public for test convenience.
- The conformance fixture should control state-store outcomes and observe production behavior; it must not reimplement the retry/merge algorithm in test code and then assert against its own duplicate logic.
- Build deterministic scripted state interactions. A conflict should be a controlled guarded-save `false` or implemented helper conflict result, followed by a reload with a new ETag and a newer state.
- Key scripts must be per-key, not a single global queue, so tenant detail, tenant index, and audit tests catch wrong-key writes, wrong ordering, stale reloads, and extra post-failure operations.
- For retry success tests, assert both behavior and interaction counts: exact reads, saves, retry attempts, and final saved model content.
- For retry exhaustion tests, assert no successful projection result is returned and any captured diagnostic context uses safe categories rather than full state keys, tenant names, tenant display names, user-facing labels, serialized event bodies, payload fields, cursor payloads, or membership details.
- For partial-success tests, assert the operation fails after a later key failure and that replay/idempotency prevents duplicate audit entries or tenant index loss. Do not require rollback of already saved state.
- Prefer property-level diagnostic assertions over exact log-message text unless Story 10.1 or 10.2 makes an exact message part of the contract.
- Keep pure model tests focused. Add conformance tests around `TenantProjectionHandler` and the helper/adapter; update `TenantReadModel`, `TenantIndexReadModel`, or `TenantAuditReadModel` tests only when a pure helper is actually introduced.
- Test event ordering with mixed `TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`, `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, and `TenantConfigurationRemoved` events where relevant.
- Use `ProjectionEventDto.MessageId` as audit `EventId` and keep duplicate/replay assertions explicit.
- Assert ordering per key/projection target unless Stories 10.1 or 10.2 define a broader ordering contract across multiple writes.

### Files Likely To Update

- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs`: add conformance/recovery coverage around tenant detail, tenant index, audit writes, retry success, retry exhaustion, and partial-success boundaries.
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs`: likely new test file if the shared fixture would make `TenantProjectionHandlerTests` too large.
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceFixture.cs`: optional test helper for scripted state reads, ETags, guarded saves, and expected diagnostics.
- `tests/Hexalith.Tenants.Server.Tests/Projections/TenantAuditProjectionTests.cs`: update only if a pure audit merge helper is introduced by Story 10.2 and needs direct model-level coverage.
- `src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs` or equivalent helper: update only when existing internal accessibility blocks meaningful tests and the smallest production change is required for test access.
- `src/Hexalith.Tenants/Properties/AssemblyInfo.cs` or equivalent assembly metadata file: optional only if `InternalsVisibleTo` is required and the repo already accepts that pattern.

### Testing Requirements

- Use xUnit v3, Shouldly, and NSubstitute or focused fakes, matching the existing test project. [Source: `tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj`; `Directory.Packages.props`]
- Do not use live DAPR sidecars, Redis, Aspire orchestration, wall-clock sleeps, or scheduler-dependent real parallelism for these focused tests.
- Run at minimum after implementation:
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantProjectionHandlerTests`
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~ProjectionWriteConformance`
  - `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantAudit`
- If the conformance fixture requires any production accessibility change, also run:
  - `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore`

### Latest Technical Information

- The repository pins Dapr Client `1.17.9`, Aspire `13.3.3`, Microsoft ASP.NET Core packages `10.0.8`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `6.0.0-rc.1`, Microsoft.NET.Test.Sdk `18.5.1`, and YamlDotNet `17.1.0`. This story should not update dependencies. [Source: `Directory.Packages.props`]
- Dapr guarded state behavior should be tested through the helper/adapter selected in Stories 10.1 and 10.2. Do not bind conformance tests directly to guessed overload signatures if the production helper already abstracts them. [Source: `_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md`; `_bmad-output/implementation-artifacts/10-2-audit-projection-write-safety.md`]

### Previous Story Intelligence

- Story 10.1 defines tenant read-model and singleton index retry semantics: reload fresh state, reapply incoming events exactly once, scope ETags per key, use max 3 attempts, fail observably on exhaustion, and avoid cross-key transaction claims. [Source: `_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md`]
- Story 10.2 defines audit retry semantics: idempotent merge by `EventId`, persisted entries authoritative on duplicate collisions, stable sort by `Timestamp` then `EventId`, invariant failures before guarded saves, and replay-safe behavior after partial success. [Source: `_bmad-output/implementation-artifacts/10-2-audit-projection-write-safety.md`]
- Story 10.3B is cancellation-specific and gated on EventStore API work. Keep this story focused on conformance/recovery unless cancellation support has already landed in the same helper boundary. [Source: `_bmad-output/implementation-artifacts/10-3b-cancellation-token-threading-for-tenant-projection-queries.md`]
- Stories 9.3, 9.4, and 9.5 hardened query visibility, actor guardrails, cursor scopes, and shared pagination bounds. Conformance tests must preserve query-facing behavior and should not change route, DTO, cursor, authorization, or pagination policy. [Source: `_bmad-output/implementation-artifacts/9-3-query-policy-for-disabled-tenants-and-orphan-memberships.md`; `_bmad-output/implementation-artifacts/9-4-actor-layer-query-guardrails.md`; `_bmad-output/implementation-artifacts/9-5-shared-pagination-bounds-and-cursor-utilities.md`]

### Git Intelligence

- Recent automation commits created and hardened Stories 10.1, 10.2, 10.3A, and 10.3B but did not implement their production code. Treat this story as a follow-up test story that waits for those implementation commits. [Source: `git log -5 --oneline`; `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- This run started with an active-dev-story soft warning for Story 9.5 and related source/test changes. Leave that active review work untouched when committing this story context. [Source: `_bmad-output/process-notes/predev-preflight-latest.json` timestamp `2026-05-18T08:01:33Z`]

### Project Context Reference

- Follow `AGENTS.md`: do not initialize or update nested submodules recursively, and use Conventional Commits for commits.
- Follow repository C# conventions: nullable-safe code, file-scoped namespaces, central package management, no inline package versions, source-generated logging for structured logs, xUnit and Shouldly for tests.
- No root `project-context.md` exists for this application repository; submodule project-context files are reference context only. Relevant EventStore context: do not modify EventStore from this story and keep query cache ETags separate from DAPR state persistence ETags.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-19 prerequisite check: Story 10.1 and Story 10.2 story files are both `Status: done`; sprint status lists `10-1-optimistic-concurrency-for-tenant-read-model-writes: done` and `10-2-audit-projection-write-safety: done`.
- 2026-05-19 prerequisite check: `git log --oneline` over the story artifacts and projection helper paths shows `aa4d03b` (DAPR tenant projection state store / 10.1 implementation), `a2010bf` (audit projection write safety), and `c8246f6` (initial conformance test scaffolding).
- 2026-05-19 prerequisite check: production contract under test is `TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync<TValue>`, `TenantProjectionWritePolicy.SaveMergedWithOptimisticConcurrencyAsync<TValue>`, `ITenantProjectionStateStore`, and `DaprTenantProjectionStateStore`.
- 2026-05-19 prerequisite check: retry limit is `TenantProjectionWritePolicy.MaxAttempts = 3`; guarded writes use `GetStateAndETagAsync<TValue>` plus `TrySaveStateAsync<TValue>` with `ConcurrencyMode.FirstWrite`.
- 2026-05-19 prerequisite check: retry exhaustion fails through `InvalidOperationException` after 3 attempts and emits safe structured log events `100101` (`OptimisticConcurrencyConflict`) and `100102` (`RetryExhausted`) with state store, key category, attempt counts, operation context, reason, correlation ID, bounded message IDs, and bounded event types.
- `dotnet test tests\Hexalith.Tenants.Server.Tests\Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~ProjectionWriteConformance` - red phase failed as expected before fixture implementation: 2 failed (`NotImplementedException`), 1 skipped scaffold.
- `dotnet test tests\Hexalith.Tenants.Server.Tests\Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~ProjectionWriteConformance` - passed after implementation, 6/6, 0 skipped.
- `dotnet test tests\Hexalith.Tenants.Server.Tests\Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantProjectionHandlerTests` - passed, 17/17.
- `dotnet test tests\Hexalith.Tenants.Server.Tests\Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter FullyQualifiedName~TenantAudit` - passed, 41/41.
- `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore` - passed, 0 warnings, 0 errors.
- `dotnet test Hexalith.Tenants.slnx --configuration Debug --no-restore` - passed, 640 passed, 1 skipped.

### Completion Notes List

- Confirmed Story 10.1 and 10.2 prerequisites before implementing conformance coverage; no blocker found.
- Completed the reusable deterministic conformance fixture around `ITenantProjectionStateStore` and the production `TenantProjectionWritePolicy`, including captured structured diagnostics and per-key scripted read/save outcomes.
- Exposed the existing tenant index event applier as an internal test seam with XML documentation; no public API, query route, DTO, cursor, authorization, state-key, audit schema, package, or EventStore behavior changed.
- Added live conformance coverage for tenant detail conflict reloads, singleton index conflict/retry and retry exhaustion, cross-key partial-success failure, audit conflict merge, duplicate `EventId` persisted-authoritative behavior, and audit replay idempotency after later projection failure.
- Removed the obsolete static-skip DAPR/Redis red-phase scaffold so Story 10.4 coverage remains deterministic and does not depend on live DAPR, Redis, Aspire, sleeps, or real parallelism.

### File List

- `_bmad-output/implementation-artifacts/10-4-projection-write-conformance-and-recovery-tests.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceFixture.cs`
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceIntegrationTests.cs` (deleted)
- `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs`

### Change Log

- 2026-05-19: Implemented deterministic projection write conformance and recovery tests; wired fixture to production write policy; removed obsolete DAPR/Redis skipped scaffold; moved story to review.
- 2026-05-19: Adversarial code review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) applied 12 patches + 4 patches from resolved decisions: reverted `ApplyIndexEvent` to `private`, rewrote fixture to drive everything through `ProjectAsync`, captured structured logger state for AC11 assertions, added fail-fast `MarkTerminalFailure` enforcement (AC10), added fixture contract API (AC5), strengthened duplicate-EventId test with distinct ActorId markers (AC8), expanded R-007 negative gate to inject sensitive content via configuration values, added tenant-detail retry-exhaustion test (Task line 47), added tenant-index replay idempotency test (AC6), added 9-event-type mixed-ordering test (AC4), added audit malformed-payload test (Task line 60), added audit mixed-event-type ordering test. Full solution test gate: 660 passed, 1 skipped. Story moved to done.

### Review Round

- 2026-05-18T14:22:38+02:00 - `bmad-party-mode 10-4-projection-write-conformance-and-recovery-tests; review;` completed with Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect), and John (Product Manager). Findings summary: story is valuable but depended on active 10.1/10.2 persistence contracts, needed clearer observable retry/attempt-count seams, sharper duplicate `EventId` persisted-authoritative wording, explicit safe-diagnostics exclusions, and stronger no-cross-key-transaction scope boundaries. Changes applied: tightened ACs and tasks for stable prerequisite contracts, deterministic fixture capabilities, attempt-count definition, diagnostic redaction, duplicate `EventId` mismatch handling, production-change limits, and non-goals. Findings deferred: whether duplicate `EventId` mismatch should emit warning diagnostics, whether diagnostic redaction belongs in a reusable helper, whether attempt counts are a shared fixture contract or per-test assertion, and whether audit ordering should be asserted by persisted sequence, event sequence, or invocation order when Stories 10.1/10.2 do not decide it. Final recommendation: ready-for-dev after applied clarifications, gated by 10.1/10.2 stable implementation contracts.

### Review Findings

Adversarial review 2026-05-19 — diff scope `c8246f6~1..HEAD` filtered to story files. Three reviewers (Blind Hunter, Edge Case Hunter, Acceptance Auditor) ran in parallel. After deduplication and triage: 5 decision-needed, 12 patch, 9 defer, 11 dismissed.

#### Decisions Resolved (2026-05-19)

- **D1 — Cancellation scope-creep**: ACCEPT retroactively and record under 10.3B follow-up. Code stays; cancellation work is procedurally attributable to 10.3B and a deferred-work note carries the attribution.
- **D2 — AC4 mixed event-type ordering**: ADD COVERAGE NOW. Mixed-batch tests must include `TenantUpdated`, `TenantDisabled`, `TenantEnabled`, `TenantConfigurationRemoved`.
- **D3 — Tenant-detail retry-exhaustion**: ADD TO CONFORMANCE FILE NOW. Mirror `TenantIndex_RetryExhaustion` for tenant detail.
- **D4 — Audit coverage**: ADD malformed-payload + mixed-event-type ordering NOW in the new conformance file.
- **D5 — `ApplyIndexEvent` visibility**: REVERT to `private` per spec line 67 (narrowest seam, alternative `ProjectAsync` seam already exists). Rewrite `RunSingletonIndexConformanceAsync` to drive through `ProjectAsync`.

#### Patch

- [x] ~~[Review][Patch] AC9 Debug Log References cites non-existent commit `a2010bf` for audit implementation~~ — **Dismissed**: verified `a2010bf feat: Update sprint status and implement audit projection write safety` exists; Acceptance Auditor finding was incorrect.
- [x] [Review][Patch] Stale Completion note ("Ultimate context engine analysis completed - comprehensive developer guide created") is a copy-paste artifact unrelated to this story [_bmad-output/implementation-artifacts/10-4-projection-write-conformance-and-recovery-tests.md:5]
- [x] [Review][Patch] `R-007` negative-content gate inspects only `Message` and `state?.ToString()`; for `[LoggerMessage]`-generated calls these resolve to the same formatted text. Capture the full `IReadOnlyList<KeyValuePair<string, object?>>` state in `CapturingLogger.Log` and assert structured key/value pairs directly. [tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceFixture.cs:300-318]
- [x] [Review][Patch] `BindsToProductionPolicy()` is tautological — `typeof(TenantProjectionWritePolicy).FullName == "Hexalith.Tenants.Projections.TenantProjectionWritePolicy"` is a constant. Flag is also only set in `RunSingletonIndexConformanceAsync`, so 5 of 6 conformance tests never exercise the guard. Either replace with a real invocation counter wrapping the production policy and apply to all 3 paths, or remove the guard and document removal. [tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceFixture.cs:99-104]
- [x] [Review][Patch] `ScriptedTenantProjectionStateStore` does not fail-fast on stale-model reuse or extra-writes-after-terminal-failure (AC10). Track per-key model identity across reads and emit a distinct error message when `value` of attempt N is reference-equal to attempt N-1; add a `MarkTerminalFailure(key)` toggle that throws on subsequent writes to that key. [tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceFixture.cs:320-399]
- [x] [Review][Patch] AC5 fixture contract API missing — add `GetAttemptCount(key)`, `GetSavedModelAt(key, attempt)`, `AssertNoExtraWritesAfter(key)`, `GetDiagnostic(eventId)` helpers so future projections do not duplicate the per-test LINQ boilerplate. [tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceFixture.cs]
- [x] [Review][Patch] Replace literal `exception.Message.ShouldContain("3 attempts")` with reference to `TenantProjectionWritePolicy.MaxAttempts` so the contract is not anchored to a string literal that drifts if MaxAttempts changes. [tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs:656]
- [x] [Review][Patch] AC8 duplicate-EventId test wins by suppression, not by contest — both persisted and incoming use default `ActorId="actor-1"` and `["source"] = "persisted"` is only present on persisted side. Use distinct `ActorId` values and a `["source"] = "incoming"` marker on the incoming entry to prove persisted wins on payload mismatch rather than coincidentally matching. [tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs:272-327]
- [x] [Review][Patch] AC3/R-007 negative gate probes only tenant `Name` and `userId` — extend to inject sensitive content via `narrativePayload` values, a sensitive `EventTypeName`, a sensitive `MessageId`, a sensitive `correlationId`, and a sensitive `TenantConfigurationSet` value. Today's "ZERO TOLERANCE" gate covers a trivially-empty intersection because the production log call never emits those particular fields. [tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs:622-682]
- [x] [Review][Patch] AC2 "exactly once" not provable — `externallyReloaded.Members["external-user"] = TenantRole.TenantReader` bypasses `Apply`. Build the external reload state by replaying prior projection events through the same `Apply` path so that a regression double-applying an event would surface. [tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs:481-490]
- [x] [Review][Patch] AC6 tenant-index replay idempotency uncovered — add a test where after a partial-success run that wrote the index, replaying the same projection batch does not duplicate or lose index entries. [tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs]
- [x] [Review][Patch] Dead assertion — `stateStore.PlainSaveAttempts.ShouldBeEmpty()` always holds because the production handler exclusively uses `TrySaveStateAsync`. Remove or replace with a positive cancellation-state assertion (e.g., on logger output or read-call count). [tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs:866]
- [x] [Review][Patch] (from D2) Add mixed-event-type ordering test covering `TenantCreated`, `TenantUpdated`, `TenantDisabled`, `TenantEnabled`, `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, `TenantConfigurationRemoved` under conflict reload. [tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs]
- [x] [Review][Patch] (from D3) Add `TenantDetail_RetryExhaustion_FailsObservably_WithoutClaimingSuccessAsync` test mirroring the singleton-index exhaustion shape. [tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs]
- [x] [Review][Patch] (from D4) Add `Audit_MalformedPayloadPreserved_AndInvariantFailureAbortsBeforeWritesAsync` test (Task line 60) plus an audit-mixed-ordering test covering all 9 event types. [tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs]
- [x] [Review][Patch] (from D5) Revert `ApplyIndexEvent` visibility from `internal` back to `private`. Rewrite `RunSingletonIndexConformanceAsync` to drive through `ProjectAsync` (using `RunProjectionHandlerAsync` pattern) and update the R-008 fixture guard so `_productionPolicyInvoked` is set across all three projection paths. [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:220; tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceFixture.cs:73-87]

#### Deferred (pre-existing, tracked in deferred-work.md)

- [x] [Review][Defer] In-place mutation of `read.Value` in `SaveWithOptimisticConcurrencyAsync` is latent if a future state-store adapter returns shared references [src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs:60-69] — deferred, pre-existing from Story 10.1.
- [x] [Review][Defer] Empty/whitespace `AggregateId` collides across tenants — no entry-point validation, key concatenation yields `"projection:tenants:"` [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:73,88] — deferred, pre-existing.
- [x] [Review][Defer] Out-of-order `UserAddedToTenant` before `TenantCreated` silently drops membership in `TenantIndexReadModel.Apply` [src/Hexalith.Tenants.Server/Projections/TenantIndexReadModel.cs:41-53] — deferred, pre-existing in Stories 9.x / 10.1.
- [x] [Review][Defer] `NullReferenceException` defenses missing for JSON-deserialized `null` `Entries` (and null entries inside the list) in `MergeAuditState` [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:131-141] — deferred, cross-cutting deserialization hardening.
- [x] [Review][Defer] No per-string length cap in `BuildBoundedMessageIds` / `BuildBoundedEventTypes`; a single oversize MessageId defeats the bound [src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs:209-254] — deferred, Story 10.1 logging design.
- [x] [Review][Defer] `stateKeyCategory` is a free-form parameter embedded in exception messages and logs; no whitelist or length cap [src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs:99-100,184-185] — deferred, Story 10.1 helper design.
- [x] [Review][Defer] `TenantAuditProjection.ProjectAuditEvents` is synchronous and accepts no cancellation token; long batches uninterruptible mid-build [src/Hexalith.Tenants.Server/Projections/TenantAuditProjection.cs:14-29] — deferred, 10.3B extension candidate.
- [x] [Review][Defer] No backoff between retry attempts; three conflicts in microseconds can exhaust on a hot key that 10ms jitter would have resolved [src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs:45-114] — deferred, Story 10.1 retry design.
- [x] [Review][Defer] Empty/all-null `request.Events` still triggers three full retry cycles (read+write) for zero changes [src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:59-65] — deferred, performance polish.

## Advanced Elicitation

- Date/time: 2026-05-18T18:05:07+02:00
- Selected story key: 10-4-projection-write-conformance-and-recovery-tests
- Command/skill invocation used: `/bmad-advanced-elicitation 10-4-projection-write-conformance-and-recovery-tests`
- Batch 1 method names: Pre-mortem Analysis; Failure Mode Analysis; Red Team vs Blue Team; Code Review Gauntlet; First Principles Analysis.
- Reshuffled Batch 2 method names: Self-Consistency Validation; Comparative Analysis Matrix; Security Audit Personas; Occam's Razor Application; Architecture Decision Records.
- Findings summary:
  - The story already had the right dependency gate, but implementation could still start speculative tests without recording concrete 10.1/10.2 contract evidence.
  - A single global scripted state sequence could let wrong-key writes, stale model reuse, or unexpected write order pass accidentally.
  - Diagnostic assertions needed to avoid brittle full-message matching while still proving payload, tenant-name, cursor, and membership-detail redaction.
  - The conformance fixture needed an explicit boundary: it should drive state outcomes and observe production behavior, not duplicate the retry or merge algorithm under test.
- Changes applied:
  - Added acceptance criteria and tasks requiring Dev Agent Record evidence for prerequisite story status or commits, helper/adapter APIs, retry limits, failure shape, and stable diagnostics.
  - Tightened prerequisite handling so missing 10.1/10.2 contracts block implementation instead of creating partial fixtures or skipped speculative tests.
  - Added per-key and per-attempt scripting requirements for deterministic state-store fixtures, including fail-fast behavior for unexpected keys, ordering, stale reloads, extra writes, and terminal failures.
  - Clarified that diagnostics should be asserted through safe structured fields/categories unless an exact message is part of the accepted contract.
  - Added implementation guidance to test the real production helper/adapter behavior instead of reimplementing retry/merge logic inside the fixture.
- Findings deferred:
  - Exact prerequisite commit hashes, helper names, retry-exhaustion exception/result shape, and diagnostic fields remain implementation-time evidence from completed Stories 10.1 and 10.2.
  - Whether `InternalsVisibleTo` is required remains deferred until the actual helper/adapter visibility is known.
  - Exact test file split between `TenantProjectionHandlerTests`, `ProjectionWriteConformanceTests`, and fixture files remains a developer decision once production helper shape is available.
- Final recommendation: ready-for-dev, gated by documented 10.1/10.2 implementation contracts.
