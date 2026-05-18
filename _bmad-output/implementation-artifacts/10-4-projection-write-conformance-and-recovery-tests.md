# Story 10.4: Projection Write Conformance and Recovery Tests

Status: ready-for-dev

Completion note: Ultimate context engine analysis completed - comprehensive developer guide created.

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

## Tasks / Subtasks

- [ ] Confirm implementation prerequisites before writing conformance tests. (AC: 1, 2)
  - [ ] Verify Story 10.1 has implemented guarded write behavior for `projection:tenants:{tenantId}` and `projection:tenant-index:singleton`.
  - [ ] Verify Story 10.2 has implemented guarded write behavior and idempotent merge behavior for `audit:{tenantId}`.
  - [ ] If either story is not merged, implemented, or documented with a stable accepted projection persistence contract, stop before adding speculative tests and return this story to backlog or blocked with the missing prerequisite named.
  - [ ] Record the concrete helper/adapter API and retry policy under test in test names or comments where the behavior would otherwise be ambiguous.
- [ ] Build a reusable projection write conformance fixture in the test project. (AC: 1, 5, 7)
  - [ ] Place test-only conformance helpers under `tests/Hexalith.Tenants.Server.Tests/Projections/` or a nearby test-support folder already used by this project.
  - [ ] Keep the fixture internal to tests; do not add production abstractions solely for conformance reuse unless Stories 10.1 or 10.2 already introduced the internal adapter being tested.
  - [ ] Model deterministic state-store reads, ETags, guarded-save results, and thrown infrastructure failures without live DAPR, Redis, Aspire, sleeps, or real parallelism.
  - [ ] Let each projection scenario declare state key category, initial state, externally updated reload state, incoming event batch, expected saved state, conflict count, write ordering, and expected attempt counts.
  - [ ] Define attempt counting before assertions, including whether the initial try counts as attempt 1 for conflict-then-success and retry exhaustion cases.
  - [ ] Make failure assertions reusable: no successful `ProjectionResponse`, safe diagnostic context, no event payload, serialized event body, tenant display name, user-facing label, cursor payload, or membership detail in captured logs, and no extra writes after the terminal failure.
- [ ] Add tenant detail conformance tests. (AC: 1, 2, 3, 4, 7)
  - [ ] Cover conflict-then-success for `projection:tenants:{tenantId}` and assert the final `TenantReadModel` contains the incoming lifecycle, membership, and configuration events exactly once.
  - [ ] Cover existing-state ETag saves and missing-state first-write behavior as implemented by Story 10.1.
  - [ ] Cover retry exhaustion and assert `ProjectAsync` fails through the existing projection failure path rather than returning success.
  - [ ] Assert stale mutated model instances are not reused across retry attempts.
- [ ] Add tenant index conformance tests. (AC: 1, 2, 3, 4, 6, 7)
  - [ ] Cover conflict-then-success for `projection:tenant-index:singleton` where the retry reload contains another tenant update not present in the stale read.
  - [ ] Assert the saved `TenantIndexReadModel` preserves previously indexed tenants plus incoming tenant, membership, removal, and role-change effects.
  - [ ] Cover retry exhaustion after tenant detail success and assert the overall projection operation still fails without claiming cross-key atomic success.
  - [ ] Verify deterministic query-facing ordering remains compatible with existing list/user-tenants query tests; do not change cursor format or query response DTOs.
- [ ] Add audit conformance and recovery tests. (AC: 1, 2, 3, 4, 6, 8)
  - [ ] Cover conflict-then-success for `audit:{tenantId}` where the retry reload contains an externally persisted audit entry.
  - [ ] Assert original persisted entries, externally added entries, and incoming entries are present exactly once and sorted by `Timestamp` then `EventId`.
  - [ ] Cover duplicate `EventId` collision and assert the persisted entry remains authoritative.
  - [ ] If a duplicate `EventId` has mismatched incoming payload content, assert persisted state wins and record any diagnostic expectations only if Story 10.2 defines them.
  - [ ] Cover replay after audit save plus later projection failure and assert audit idempotency prevents duplicate entries.
  - [ ] Cover malformed payload preservation and invariant-failure no-save behavior already required by Story 10.2.
  - [ ] Assert recovered audit records remain queryable through existing date-range and cursor behavior without adding new query APIs.
- [ ] Keep scope boundaries explicit. (AC: 1-8)
  - [ ] Do not modify `Hexalith.EventStore` or initialize/update nested submodules recursively.
  - [ ] Do not add package dependencies or central package versions.
  - [ ] Do not change production query routes, DTOs, cursor encoding, authorization policy, projection state key names, audit schema, or EventStore behavior.
  - [ ] Do not introduce distributed locks, queue redesign, schema migrations, repair commands, admin UI, diagnostic query endpoints, a new storage abstraction, cross-key transaction support, or a broad diagnostic/logging refactor.
  - [ ] Production code changes are acceptable only if needed to expose existing internal helper behavior to the test assembly in the narrowest way, for example `InternalsVisibleTo`, and only after confirming no better existing test seam is available; do not make behavioral changes or public APIs solely for test convenience.

## Dev Notes

### Dependency Gate

- This story is a conformance and recovery test story. It should not be implemented before Stories 10.1 and 10.2 have landed because the selected write policy, helper shape, retry limit, and failure contract are established there. [Source: `_bmad-output/implementation-artifacts/10-1-optimistic-concurrency-for-tenant-read-model-writes.md`; `_bmad-output/implementation-artifacts/10-2-audit-projection-write-safety.md`]
- If 10.1 or 10.2 is still active, implementation may proceed only when the final projection persistence contract is explicitly documented and accepted; otherwise, avoid writing tests that reverse-engineer in-progress code. [Source: `2026-05-18 party-mode review`]
- If implementation starts while `TenantProjectionHandler.ProjectAsync` still uses plain `SaveStateAsync` for tenant detail, audit, or tenant index writes, stop and report the missing prerequisite instead of writing tests against speculative behavior. [Source: `src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs`]
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
- Build deterministic scripted state interactions. A conflict should be a controlled guarded-save `false` or implemented helper conflict result, followed by a reload with a new ETag and a newer state.
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

### Completion Notes List

### File List

### Review Round

- 2026-05-18T14:22:38+02:00 - `bmad-party-mode 10-4-projection-write-conformance-and-recovery-tests; review;` completed with Winston (System Architect), Amelia (Senior Software Engineer), Murat (Master Test Architect), and John (Product Manager). Findings summary: story is valuable but depended on active 10.1/10.2 persistence contracts, needed clearer observable retry/attempt-count seams, sharper duplicate `EventId` persisted-authoritative wording, explicit safe-diagnostics exclusions, and stronger no-cross-key-transaction scope boundaries. Changes applied: tightened ACs and tasks for stable prerequisite contracts, deterministic fixture capabilities, attempt-count definition, diagnostic redaction, duplicate `EventId` mismatch handling, production-change limits, and non-goals. Findings deferred: whether duplicate `EventId` mismatch should emit warning diagnostics, whether diagnostic redaction belongs in a reusable helper, whether attempt counts are a shared fixture contract or per-test assertion, and whether audit ordering should be asserted by persisted sequence, event sequence, or invocation order when Stories 10.1/10.2 do not decide it. Final recommendation: ready-for-dev after applied clarifications, gated by 10.1/10.2 stable implementation contracts.
