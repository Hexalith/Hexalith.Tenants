# Sprint Change Proposal: DAPR Integration Test Prerequisite Gating

Date: 2026-05-12
Project: Hexalith.Tenants
Trigger: Epic 3 retrospective verification
Scope classification: Minor direct adjustment

## 1. Issue Summary

Full solution verification was run after the Epic 3 retrospective:

```powershell
dotnet test Hexalith.Tenants.slnx --configuration Release --no-restore
```

The command built and executed the non-DAPR test suites successfully, but `Hexalith.Tenants.IntegrationTests` reported failures when local DAPR infrastructure was unavailable.

Evidence:

- DAPR CLI is not installed in the current workspace environment.
- DAPR placement service is not reachable on `localhost:6050`.
- DAPR scheduler service is not reachable on `localhost:6060`.
- Redis may also be required on `localhost:6379` for DAPR-backed command pipeline tests.

The failing tests were not failing because Epic 3 logic regressed. They failed during fixture initialization because infrastructure prerequisites were absent.

## 2. Impact Analysis

### Epic Impact

- Epic 3 remains implementation-complete.
- Epic 4 remains directionally valid; it depends on Epic 3 membership/configuration events and will continue to need DAPR-backed verification when runtime infrastructure is available.
- No epic needs to be removed, resequenced, or redefined.

### Story Impact

- No completed story acceptance criteria need to change.
- The correction belongs to test infrastructure behavior: DAPR-backed integration tests should report unavailable local prerequisites as skipped rather than failed.

### Artifact Conflicts

- PRD: no MVP requirement change.
- Epics: no scope change.
- Architecture: no architecture decision change.
- Test/verification guidance: update behavior so local infrastructure absence is visible but not misclassified as code failure.

## 3. Recommended Approach

Selected path: Direct Adjustment.

Change the DAPR integration tests so DAPR-backed fixtures record missing local infrastructure as an unavailable test environment instead of failing during collection initialization. DAPR-backed test methods then skip explicitly when the fixture reports unavailable prerequisites.

Rationale:

- The issue is environmental, not a product behavior defect.
- xUnit v3 supports conditional skip metadata via `FactAttribute.SkipUnless` and `SkipType`.
- xUnit collection fixtures can still initialize before skipped tests are reported, so fixture startup must be tolerant of missing prerequisites.
- Runtime skip guards inside the test methods prevent unavailable local infrastructure from being misclassified as a code failure.
- This keeps `dotnet test` signal useful on developer machines that do not have DAPR initialized.

Rollback is not useful because no product code needs to be reverted.

MVP review is not needed because core project goals remain unchanged.

## 4. Detailed Change Proposals

### Test Attribute: DaprFactAttribute

Behavior:

```csharp
public sealed class DaprFactAttribute : FactAttribute {
    public DaprFactAttribute(
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
        : base(sourceFilePath, sourceLineNumber) {
        Skip = DaprTestPrerequisites.SkipReason;
        SkipUnless = nameof(DaprTestPrerequisites.IsAvailable);
        SkipType = typeof(DaprTestPrerequisites);
    }
}
```

Rationale: missing Redis, placement, or scheduler infrastructure is a test environment condition, not a product failure. The attribute documents and reports DAPR prerequisite gating at the test level.

### Fixture Availability State

New behavior:

- `AspireTopologyFixture` and `TenantsDaprTestFixture` probe local DAPR prerequisites during initialization.
- When prerequisites are absent, fixtures set `PrerequisitesAvailable = false`, store a diagnostic `SkipReason`, and return without booting Aspire, Kestrel, or daprd.
- Each fixture exposes `SkipIfUnavailable()` for tests and guarded endpoint/client properties.

Rationale: xUnit collection fixture initialization must not throw for an environment skip to be reported cleanly.

### Test Methods

Replace `[Fact]` with `[DaprFact]` for tests in the `AspireTopology` and `TenantsDaprTest` collections, and call `_fixture.SkipIfUnavailable()` at the start of each DAPR-backed test.

Rationale: only tests that require local DAPR runtime infrastructure should be skipped. Runtime-free integration tests continue to run normally.

## 5. Implementation Handoff

Owner: Developer agent.

Implementation tasks:

- Add `DaprFactAttribute` with cached prerequisite detection.
- Add fixture-level prerequisite availability state and skip diagnostics.
- Mark DAPR-backed integration tests with `[DaprFact]`.
- Add explicit `_fixture.SkipIfUnavailable()` guards to DAPR-backed test methods.
- Keep startup, health, sidecar, and runtime failures as failures once prerequisites are available.
- Run `dotnet test Hexalith.Tenants.slnx --configuration Release --no-restore` to verify skipped integration tests no longer make the full run red in this environment.

Success criteria:

- Non-DAPR test suites still pass.
- DAPR-dependent tests are reported as skipped when local infrastructure is missing.
- No product code or epic scope changes are required.

## 6. Decision

Approved for direct implementation by user request: "fix this".
