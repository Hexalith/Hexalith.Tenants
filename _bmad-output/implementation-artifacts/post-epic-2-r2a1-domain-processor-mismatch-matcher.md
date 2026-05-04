# Story: post-epic-2-r2a1 — Domain Processor Fall-Through Catches MissingApplyMethodException

**Epic:** Tenants Epic 2 — Core Tenant Management & Global Administration (defect carry-forward)
**Status:** done
**Severity:** High — silent 500 on any non-empty aggregate stream when DI ordering puts the wrong processor first
**Source proposal:** `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-04-tenants-defect-carry-forward.md` §B

## Context

`Hexalith.Tenants/src/Hexalith.Tenants/DomainProcessing/DomainServiceRequestHandler.cs`
iterates registered `IDomainProcessor`s and uses substring matching on
`InvalidOperationException.Message` to identify "wrong processor, fall through":

```csharp
private static bool IsProcessorMismatch(InvalidOperationException ex)
    => ex.Message.Contains("No Handle method found for command type", ...)
    || ex.Message.Contains("No matching Apply method found on state", ...);
```

The second substring `"No matching Apply method found on state"` does not exist
in any thrown exception. EventStore migrated state-rehydration failures to a
typed `Hexalith.EventStore.Client.Aggregates.MissingApplyMethodException`
(post-Epic-1 R1-A6) whose actual message format is:

> Aggregate state 'X' has no public void Apply(Y) method...

Consequence: when DI hands the wrong processor first (e.g.,
`GlobalAdministratorsAggregate` for an aggregate whose stream contains
`TenantCreated` events), state rehydration throws `MissingApplyMethodException`,
the matcher fails to recognize it, and the request returns 500 instead of
falling through to the correct processor.

Discovered 2026-05-04 during MCP-observed E2E testing on aggregate
`system|tenants|acme-corp` (pre-existing `TenantCreated` event in stream).
Existing test `DomainServiceRequestHandlerTests.cs:20` hand-throws an
`InvalidOperationException` with the obsolete substring, so the fall-through
path is never exercised against the real production exception.

## Acceptance Criteria

1. `DomainServiceRequestHandler.ProcessAsync` falls through to the next
   processor when state rehydration throws `MissingApplyMethodException`.
2. Existing fall-through behavior for `"No Handle method found for command
   type"` is preserved (no regression for command-type mismatch).
3. Substring `"No matching Apply method found on state"` and the
   `DomainProcessorMismatchMessages.MissingApplyMethodOnState` constant are
   removed (dead code — never matched any real exception).
4. `DomainServiceRequestHandlerTests.cs` updated: replace synthetic
   `InvalidOperationException` throw with a real
   `MissingApplyMethodException` instance (constructed via its public
   constructor) and assert fall-through.
5. New unit test: two processors whose state types differ (e.g.,
   `GlobalAdministratorsAggregate` first, `TenantAggregate` second), against a
   `CurrentState` containing a `TenantCreated` event → asserts
   `TenantAggregate.Handle(CreateTenant, ...)` is reached.
6. All existing Tier 1 + Tier 2 tests still pass.

## Implementation Notes

- Recommended fix: typed catch `catch (MissingApplyMethodException) { ... continue; }`
  before the existing `catch (InvalidOperationException ex) when (IsProcessorMismatch(ex))`.
  Keeps current contract for command-type mismatch; removes brittle substring
  matching for state-rehydration mismatch.
- Do NOT introduce dependency on EventStore's internal exception text — depend
  on the public exception type only.
- This is a targeted patch. The broader concern (replacing exception-driven
  fall-through with explicit aggregate-id routing) is parked as a backlog
  improvement, not part of this story.

## Test Plan

- Tier 1: updated `DomainServiceRequestHandlerTests.cs` (typed exception fall-through);
  new unit test for two-processor multi-state scenario.
- Tier 2: optional — extend an integration test to send `CreateTenant` against
  a freshly seeded aggregate stream containing one prior `TenantCreated` event
  (synthetic insertion via `IEventStoreClient` test helper) and assert response
  is `Rejected: TenantAlreadyExistsRejection`, not 500.
- Manual: rerun MCP E2E reproducer from 2026-05-04 against `acme-corp` stream —
  expect `Rejected: TenantAlreadyExistsRejection` instead of `Failed`.

## Out of Scope

- Replacing the entire fall-through pattern with typed-dispatch routing
  (logged as future improvement; separate proposal when scheduled).

## Tasks/Subtasks

- [x] Add typed `catch (MissingApplyMethodException)` clause before the
      existing `catch (InvalidOperationException) when (IsProcessorMismatch(ex))`
      in `DomainServiceRequestHandler.ProcessAsync` (AC1).
- [x] Preserve `MissingHandleMethod` substring matching for command-type
      mismatch fall-through (AC2).
- [x] Remove `DomainProcessorMismatchMessages.MissingApplyMethodOnState`
      constant and its branch in `IsProcessorMismatch` — dead code (AC3).
- [x] Add `using Hexalith.EventStore.Client.Aggregates;` so the typed
      exception is in scope.
- [x] Update `DomainServiceRequestHandlerTests.ProcessAsync_WhenFirstProcessorHasMismatchedState_UsesNextProcessor`:
      replace synthetic `InvalidOperationException` with a real
      `MissingApplyMethodException` instance (AC4).
- [x] Add new test
      `DomainServiceRequestHandlerTests.ProcessAsync_WhenStateTypesDifferAcrossProcessors_RoutesToProcessorMatchingTheStream`
      using `GlobalAdministratorsState` (first, throws) and a second
      processor that captures the routed `CreateTenant` command (AC5).
- [x] Run Tier 1 test suites for the Tenants submodule and confirm no
      regressions (AC6).

### Review Findings

- [x] [Review][Patch] AC5 test does not exercise the real multi-state replay path [tests/Hexalith.Tenants.Server.Tests/DomainProcessing/DomainServiceRequestHandlerTests.cs:37] — patched by replacing the synthetic fake-processor throw with real `GlobalAdministratorsAggregate` → `TenantAggregate` processing over a `DomainServiceCurrentState` containing `TenantCreated`.

## Dev Agent Record

### Completion Notes

- Production fix in `DomainServiceRequestHandler.cs`: typed catch on
  `MissingApplyMethodException` is positioned before the existing
  `InvalidOperationException`-with-filter catch. Because
  `MissingApplyMethodException : InvalidOperationException`, source order
  matters — the typed catch wins for state-rehydration mismatches; the
  filtered catch continues to handle command-type mismatches via the
  `MissingHandleMethod` substring only.
- Dead substring `"No matching Apply method found on state"` and its
  constant deleted. The new contract is type-based, not text-based, so the
  handler is no longer coupled to EventStore's exception phrasing.
- Existing test `ProcessAsync_WhenRehydrationFailsForMalformedHistory_DoesNotTreatItAsMismatch`
  still passes unmodified: a plain `InvalidOperationException` carrying
  unrelated rehydration text bypasses the typed catch and fails the
  `MissingHandleMethod` filter, so it bubbles up as expected.
- Tier 1 verification: `Hexalith.Tenants.Server.Tests` 242/242,
  `Contracts.Tests` 34/34, `Client.Tests` 48/48, `Testing.Tests` 89/89 —
  total 413/413 passing. Tier 2 / Tier 3 not run (require `dapr init` +
  Docker; out of scope for a unit-level matcher fix).
- Tooling note: an orphan `Hexalith.Tenants.exe` (PID 13384, started
  15:58:49) was holding the web-host binary and blocking the rebuild;
  stopped with `Stop-Process -Force` after user confirmation.

### Debug Log

- Build error MSB3027/MSB3021: `Hexalith.Tenants.exe` locked by a running
  process during first `dotnet test` invocation. Resolved by stopping the
  orphan process; subsequent build and test run succeeded with no
  warnings on the modified files.

## File List

- `src/Hexalith.Tenants/DomainProcessing/DomainServiceRequestHandler.cs` — modified
- `tests/Hexalith.Tenants.Server.Tests/DomainProcessing/DomainServiceRequestHandlerTests.cs` — modified
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — modified (status transition)
- `_bmad-output/implementation-artifacts/post-epic-2-r2a1-domain-processor-mismatch-matcher.md` — modified (status, tasks, dev agent record)

## Change Log

- 2026-05-04 — R2-A1 carry-forward implementation. Replaced brittle
  substring-based fall-through detection with typed catch on
  `MissingApplyMethodException`. Updated existing fall-through test to
  exercise the real production exception and added a multi-state-type
  fall-through test that mirrors the MCP-observed E2E reproducer
  (`acme-corp` stream). All Tier 1 suites green.
