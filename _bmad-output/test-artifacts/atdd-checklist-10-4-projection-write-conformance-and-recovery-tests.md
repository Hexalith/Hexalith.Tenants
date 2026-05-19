---
stepsCompleted:
  - step-01-preflight-and-context
  - step-02-generation-mode
  - step-03-test-strategy
  - step-04-generate-tests
  - step-05-validate-and-complete
lastStep: 'step-05-validate-and-complete'
lastSaved: '2026-05-19'
storyId: '10.4'
storyKey: '10-4-projection-write-conformance-and-recovery-tests'
storyFile: '_bmad-output/implementation-artifacts/10-4-projection-write-conformance-and-recovery-tests.md'
atddChecklistPath: '_bmad-output/test-artifacts/atdd-checklist-10-4-projection-write-conformance-and-recovery-tests.md'
generatedTestFiles:
  - tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceFixture.cs
  - tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs
  - tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceIntegrationTests.cs
inputDocuments:
  - _bmad-output/test-artifacts/test-design-epic-10.md
  - _bmad-output/implementation-artifacts/10-4-projection-write-conformance-and-recovery-tests.md
  - src/Hexalith.Tenants/Projections/TenantProjectionWritePolicy.cs
  - src/Hexalith.Tenants/Projections/ITenantProjectionStateStore.cs
  - tests/Hexalith.Tenants.Server.Tests/Projections/TenantProjectionHandlerTests.cs
  - .claude/skills/bmad-testarch-atdd/resources/knowledge/component-tdd.md
  - .claude/skills/bmad-testarch-atdd/resources/knowledge/test-quality.md
detected_stack: 'backend'
target_risk: 'R-001'
target_trio:
  - 'T-R001-UNIT-001 — TenantIndex_ConflictThenSuccess_PreservesAllPreviouslyIndexedTenantsAsync'
  - 'T-R001-UNIT-002 — TenantIndex_RetryExhaustion_FailsObservably_WithoutClaimingSuccessAsync'
  - 'T-R001-INT-001 — TenantIndex_RealDaprBackedConflictThenSuccess_PreservesIndexAsync'
---

# ATDD Checklist — Story 10.4 / R-001 BLOCKER Trio

## Step 1 — Preflight & Context (Completed)

### Stack Detection

- `test_stack_type`: auto → **backend** (.NET 10 + xUnit v3)
- Detected indicators: `*.csproj` everywhere; no `package.json` with frontend frameworks at root

### Prerequisites Verified

- ✅ Story 10.4 (`10-4-projection-write-conformance-and-recovery-tests.md`) has clear ACs (11 listed, includes conformance + recovery requirements)
- ✅ xUnit v3 `3.2.2` + Shouldly `4.3.0` + NSubstitute `6.0.0-rc.1` configured in `tests/Hexalith.Tenants.Server.Tests/`
- ✅ Test project compiles and runs locally per recent CI evidence (611 passed in last unfiltered run per 10-3a Dev Notes)
- ✅ Production helper `TenantProjectionWritePolicy` already shipped via Story 10.1 (`internal static partial class`, `MaxAttempts = 3`)

### Story Context Extracted

- **`story_key`**: `10-4-projection-write-conformance-and-recovery-tests`
- **`story_id`**: `10.4`
- **`story_file`**: `_bmad-output/implementation-artifacts/10-4-projection-write-conformance-and-recovery-tests.md`
- **Target risks (R-001 BLOCKER, score 9):** silent last-writer-wins on `projection:tenant-index:singleton`
- **AC coverage in scope:** AC#1 (conformance for tenant index), AC#2 (retry-then-success), AC#3 (retry exhaustion safe failure), AC#7 (attempt counts via observable seams)

### Production Surface Mapped

- **Helper under test:** `TenantProjectionWritePolicy.SaveMergedWithOptimisticConcurrencyAsync<TenantIndexReadModel>(...)` — the singleton-index merge path
- **State key:** `projection:tenant-index:singleton`
- **State store interface:** `ITenantProjectionStateStore` with `GetStateAndETagAsync<TValue>(...)` and `TrySaveStateAsync<TValue>(...)` returning bool
- **Retry budget:** `MaxAttempts = 3`
- **Exhaustion behavior:** `InvalidOperationException` with message `"{stateKeyCategory} projection write exceeded optimistic concurrency retry limit after {MaxAttempts} attempts."`
- **Source-generated logs:** `OptimisticConcurrencyConflict` (EventId `100101`, Warning) + `RetryExhausted` (EventId `100102`, Error)
- **Bounded log fields:** `MaxLoggedMessageIds = 20`, `MaxLoggedEventTypes = 8` (prevents log-size explosion under full-replay)

### Existing Test Patterns Observed

- **Test file convention:** `{ClassUnderTest}Tests.cs` in `tests/Hexalith.Tenants.Server.Tests/Projections/`
- **Namespace:** file-scoped, `Hexalith.Tenants.Server.Tests.Projections`
- **Braces:** K&R/Egyptian (opening brace on same line) — distinct from EventStore's Allman convention
- **Test naming:** `MethodOrScenario_Condition_OutcomeAsync` (PascalCase, Async suffix on async tests)
- **Existing helper:** `ScriptedTenantProjectionStateStore` (already in test project) with `EnqueueRead`, `EnqueueTrySave`, `TrySaveAttempts` properties
- **Test attribute:** `[Fact]` (xUnit) — no `[Theory]/[InlineData]` usage in projection tests
- **Assertions:** Shouldly fluent (`ShouldBe`, `ShouldNotBeNull`, `ShouldThrow`, `ShouldBeSameAs`); never raw `Assert.*`
- **Internal access:** `tests/Hexalith.Tenants.Server.Tests/` already has access to `Hexalith.Tenants.Projections.TenantProjectionWritePolicy` (proven by existing `TenantProjectionHandlerTests`)

### Knowledge Fragments Loaded (ATDD-specific)

- ✅ `component-tdd.md` — Red→Green→Refactor TDD loop discipline
- ✅ `test-quality.md` (carried from TD workflow) — Test quality DoD
- ✅ `data-factories.md` (skim) — Factory pattern adapted for projection event DTOs

### TEA Config Flags

- `tea_use_playwright_utils`: true → **N/A for .NET backend**; skipped
- `tea_use_pactjs_utils`: false → no contract testing
- `tea_pact_mcp`: none → no Pact MCP
- `tea_browser_automation`: auto → **N/A**; no browser scope
- `test_stack_type`: auto → resolved to `backend`

### File Output Plan

Three test files will be generated as red-phase scaffolds:

| File                                                                                          | Tier | Scaffolds |
|-----------------------------------------------------------------------------------------------|------|-----------|
| `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs`           | 1 (Unit) | T-R001-UNIT-001, T-R001-UNIT-002 |
| `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceIntegrationTests.cs` | 2 (DAPR/Docker integration) | T-R001-INT-001 |

Both files compile against existing `ScriptedTenantProjectionStateStore` + project conventions. Integration test gates on DAPR/Redis availability via runtime skip (per R3-A4 SCP precondition pattern).

### Open Decisions Carried into Step 2

1. **Skip mechanism for Tier 2 integration test:** use xUnit v3's built-in `Assert.Skip(reason)` for precondition gating? Or `[SkippableFact]` if the project already depends on `Xunit.SkippableFact`? — Confirm in Step 3 by checking `Directory.Packages.props`.
2. **Whether to make the new tests `[Fact]` immediately or `[Fact(Skip = "Red phase — implement to turn green")]` to honor TDD red-phase discipline:** Red phase = scaffold compiles but assertions fail (per `component-tdd.md`). I'll use unskipped `[Fact]` with assertions that **must** fail until the conformance fixture is wired — the test framework reports failure, dev implements, then tests turn green. Skipped tests defeat the red phase.
3. **Where the `ProjectionWriteConformanceFixture` lives (Story 10.4 / R-008 fixture rule):** A dedicated fixture class in the same folder, sealed, scoped per-test, internal. Tests instantiate it directly (no `IClassFixture<>` global sharing, to keep per-test state isolated).

### Next Step → Done

→ `step-02-generation-mode.md` — Confirm scaffold generation strategy.

## Step 2 — Generation Mode (Completed)

- **Mode:** AI generation
- **Rationale:** `{detected_stack}` is `backend`. Backend projects always use AI generation (no browser recording). Recording mode is N/A and skipped per the step file.
- **Generation source:** Story 10.4 acceptance criteria + Test Design `test-design-epic-10.md` R-001 row + production source `TenantProjectionWritePolicy.cs`.

### Next Step → Done

→ `step-03-test-strategy.md` — Derive scaffold structure from ACs and existing patterns.

## Step 3 — Test Strategy (Completed)

### AC Mapping → Test Trio

| AC# (Story 10.4) | Aspect                                                     | Test ID         | Test Level  | Priority |
|------------------|------------------------------------------------------------|-----------------|-------------|----------|
| AC#1             | Preserves all events under concurrent updates              | T-R001-UNIT-001 | Unit (Tier 1) | P0       |
| AC#2             | Transient persistence conflicts retry to success           | T-R001-UNIT-001 | Unit (Tier 1) | P0       |
| AC#3             | Retry exhaustion → safe observable failure (no false success) | T-R001-UNIT-002 | Unit (Tier 1) | P0       |
| AC#7             | Attempt counts via observable test seams                   | T-R001-UNIT-001 + UNIT-002 | Unit | P0      |
| AC#1 + AC#10     | Per-key, per-attempt scripted outcomes (real DAPR sanity)  | T-R001-INT-001  | Tier 2      | P0       |
| (R-007 cross-cut) | Negative log-content assertion on diagnostic shape         | T-R001-UNIT-002 | Unit (Tier 1) | P0       |

### Existing Coverage Already Delivered by Story 10.1

These tests in `TenantProjectionHandlerTests.cs` already partially cover the R-001 surface — the ATDD trio extends, not duplicates:

| Test (existing)                                                                              | Coverage      | What it does NOT yet cover            |
|----------------------------------------------------------------------------------------------|---------------|---------------------------------------|
| `ProjectAsync_TenantIndexConflictPreservesReloadedExistingTenantsAsync` (line 93)            | Handler-level conflict-then-success | Direct policy-helper invocation (R-008 fixture rule); negative log assertions |
| `ProjectAsync_IndexRetryExhaustionAfterTenantSaveThrowsAsync` (line 135)                     | Handler-level exhaustion | Direct policy-helper invocation; structured-log emission (R-016); bounded log fields; negative payload-content (R-007) |
| `ProjectAsync_RetryExhaustionThrowsAfterMaxAttemptsAsync` (line 115)                          | Tenant detail exhaustion (not index) | N/A — different key |

The ATDD trio targets the **direct policy invocation pattern** that Story 10.4's conformance fixture (R-008) is supposed to formalize.

### Test Level Selection

- **Tier 1 Unit** for UNIT-001 + UNIT-002 — pure in-process tests against `TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync<TenantIndexReadModel>`. No DAPR, no Redis, no Aspire.
- **Tier 2 Integration** for INT-001 — `DaprClient` against a real Redis state store via `dapr init` infrastructure. Gated by `Assert.Skip("...")` precondition when DAPR/Redis unavailable.
- **No Tier 3** — singleton-index conformance is a *correctness* property; Aspire E2E is wrong cost/benefit per Test Design.

### Red Phase Requirements

Story 10.1 has already shipped the singleton-index optimistic-concurrency implementation. So the scaffolds are **characterization tests** for R-001:
- Compile-able against existing types
- Run against current implementation: most assertions pass green (proves 10.1 satisfies R-001 at the policy-helper level)
- Some scaffolded assertions intentionally fail **red** until Story 10.4 wires:
  - `ProjectionWriteConformanceFixture` (R-008 — drives production helper, mechanical assertion)
  - `CapturingLogger` for structured-log emission (R-016) + negative payload-content (R-007)

This honors the "red before green" discipline by ensuring the scaffolds fail meaningfully where the conformance infrastructure is missing, while not blocking on already-shipped behavior.

### Priorities (all P0, R-001 = score 9)

Story 10.4 author MUST close all 3 tests before Epic 10 ships.

### Next Step → Done

→ `step-04-generate-tests.md` — Write the scaffolds.

## Step 4 — Generate Red-Phase Scaffolds (Completed)

### Execution Mode

- Requested: not specified → `auto` from config
- Resolved: **sequential** (single-agent, .NET backend; subagent split for API/E2E does not apply)

### Files Generated

| Path                                                                                          | Lines | Role                                                                          |
|-----------------------------------------------------------------------------------------------|-------|-------------------------------------------------------------------------------|
| `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceFixture.cs`         | ~205  | Conformance fixture: scripted state store, capturing logger, R-008 binding contract |
| `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs`           | ~140  | UNIT-001 (conflict-then-success) + UNIT-002 (retry exhaustion + R-007 negative content) |
| `tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceIntegrationTests.cs` | ~95   | INT-001 Tier 2 DAPR-backed sanity check with `Assert.Skip` precondition       |

### Build & Discovery Validation

- ✅ `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj` — **0 errors, 0 warnings**
- ✅ All 3 scaffolds discovered by xUnit v3 test runner
- ✅ All 3 scaffolds marked `[SKIP]` (red-phase respected)
- ✅ Full Tenants.Server.Tests run: **388 passed, 0 failed, 3 skipped** (no regression — pre-existing 388 tests still green)

### Red-Phase Semantics

When Story 10.4 dev removes `Skip` from a test:
1. **UNIT-001 / UNIT-002**: Test body calls `fixture.RunSingletonIndexConformanceAsync(...)` → throws `NotImplementedException` with a TODO message pointing to `TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync<TenantIndexReadModel>`. Test fails red.
2. **INT-001**: `EnsureDaprAvailableOrSkip` probes Redis at `127.0.0.1:6379`. If unreachable, `Assert.Skip` engages. If reachable, the test body throws `NotImplementedException` with the DAPR-wiring TODO. Test fails red.
3. Dev implements the fixture method bodies → tests turn green.

### Next Step

→ `step-05-validate-and-complete.md` — Validate against checklist and produce handoff summary.

## Step 5 — Validate & Complete (Completed)

### Checklist Validation

| Item | Status |
|------|--------|
| Prerequisites satisfied (Story 10.4 ACs clear, xUnit v3 configured, helper `TenantProjectionWritePolicy` shipped via 10.1) | ✅ |
| Test files created in correct location (`tests/Hexalith.Tenants.Server.Tests/Projections/`) | ✅ |
| Files compile against the existing test project (0 errors, 0 warnings) | ✅ |
| Tests generated as red-phase scaffolds with `[Fact(Skip = "...")]` | ✅ |
| Story metadata captured (`storyId: 10.4`, `storyKey: 10-4-projection-write-conformance-and-recovery-tests`, `storyFile` path, `atddChecklistPath`) | ✅ |
| `generatedTestFiles` list populated deterministically | ✅ |
| Risk linkage clear (R-001 BLOCKER, score 9; plus R-007 + R-008 cross-cuts in UNIT-002 and fixture) | ✅ |
| Temp artifacts in `_bmad-output/test-artifacts/` (not random locations) | ✅ |
| CLI sessions cleaned up (no browser sessions used — backend stack) | ✅ N/A |
| No regression in existing tests (388/388 passing post-scaffold-addition) | ✅ |

### Test ↔ Risk ↔ AC Traceability

| Test File                                            | Test Method                                                                              | Risk(s) | Story 10.4 ACs |
|------------------------------------------------------|------------------------------------------------------------------------------------------|---------|----------------|
| `ProjectionWriteConformanceTests.cs`                  | `TenantIndex_ConflictThenSuccess_PreservesAllPreviouslyIndexedTenantsAsync`              | R-001 (P0/BLOCK), R-008 | AC#1, AC#2, AC#7 |
| `ProjectionWriteConformanceTests.cs`                  | `TenantIndex_RetryExhaustion_FailsObservably_WithoutClaimingSuccessAsync`                | R-001 (P0/BLOCK), R-007, R-008, R-016 | AC#3, AC#7 |
| `ProjectionWriteConformanceIntegrationTests.cs`       | `TenantIndex_RealDaprBackedConflictThenSuccess_PreservesIndexAsync`                      | R-001 (P0/BLOCK) | AC#1, AC#10 |

### Completion Summary

**Generated test files (3):**
- [ProjectionWriteConformanceFixture.cs](tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceFixture.cs)
- [ProjectionWriteConformanceTests.cs](tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceTests.cs)
- [ProjectionWriteConformanceIntegrationTests.cs](tests/Hexalith.Tenants.Server.Tests/Projections/ProjectionWriteConformanceIntegrationTests.cs)

**Checklist path:** `_bmad-output/test-artifacts/atdd-checklist-10-4-projection-write-conformance-and-recovery-tests.md`

**Story handoff:**
- **`storyKey`**: `10-4-projection-write-conformance-and-recovery-tests`
- **`storyId`**: `10.4`
- **`storyFile`**: `_bmad-output/implementation-artifacts/10-4-projection-write-conformance-and-recovery-tests.md`

### Story 10.4 Dev Handoff Notes

The dev who picks up Story 10.4 should:

1. **Implement `ProjectionWriteConformanceFixture.RunSingletonIndexConformanceAsync`** — call `TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync<TenantIndexReadModel>` with the production `ApplyIndexEvent` (visible in `TenantProjectionHandler.ApplyIndexEvent` — that's the helper to invoke at the singleton-index level). The method must NOT re-implement retry/merge logic in test code (R-008 rule).
2. **Implement `ProjectionWriteConformanceFixture.BindsToProductionPolicy`** — mechanical assertion that the fixture invokes the production type. One approach: capture the assembly-qualified name of `TenantProjectionWritePolicy` at construction and compare. Goal: any future refactor that substitutes a test-only retry implementation must fail this check.
3. **Un-skip the 2 unit tests** (`ProjectionWriteConformanceTests.cs`) — they should turn green against current 10.1 implementation since the conformance scenario is what 10.1 was designed to satisfy.
4. **Un-skip the integration test** (`ProjectionWriteConformanceIntegrationTests.cs`) only after wiring the `DaprClient` + interleaved-writer pattern described in the file's TODO block. Keep the `Assert.Skip` precondition probe so the test skips cleanly when DAPR/Redis unavailable.
5. **Extend the conformance suite** to cover the other Epic 10 risks per `test-design-epic-10.md`: D1 (tenant detail LWW), D3 (audit entry loss), D4 (duplicate EventId), D5 (stale instance reuse), D6 (audit ordering), T2 (cache poisoning), T-R007 negative-content for cancellation path (touches Story 10.3B), etc.

### Key Assumptions

1. `TenantProjectionWritePolicy.SaveWithOptimisticConcurrencyAsync` retains its current signature `(stateStore, logger, storeName, key, stateKeyCategory, operationContext, events, defaultFactory, applyEvent, cancellationToken)` — the fixture method body uses this directly.
2. `MaxAttempts = 3` (current value) — UNIT-002 asserts on `"3 attempts"` in the exception message; if this constant changes, the assertion must change too.
3. Source-generated logger EventIds remain `100101` (Warning, OptimisticConcurrencyConflict) and `100102` (Error, RetryExhausted) — UNIT-002 asserts on these IDs.
4. `CapturingLogger<TenantProjectionHandler>` is acceptable as the logger type passed into the policy; the policy accepts `ILogger` non-generically internally so the category just determines log routing.
5. Test name conventions match the existing `TenantProjectionHandlerTests` style (PascalCase, descriptive, Async suffix on async tests).

### Next Recommended Workflow

→ **`/bmad-dev-story 10-4-projection-write-conformance-and-recovery-tests`** — Story 10.4 implementation. The dev workflow can read this checklist and the linked test scaffolds to scope the implementation.

Alternative:
- Run **`/bmad-dev-story 10-3b-cancellation-token-threading-for-tenant-projection-queries`** first if you'd rather close out the cancellation work (Story 10.3B) before tackling conformance. T-R007-UNIT-002 (cancellation diagnostic non-leak) is part of 10.3B; the structure here (CapturingLogger + negative-content pattern) carries over directly.
