# Sprint Change Proposal: Resolve SubmitCommandRequest Integration Test Ambiguity

Date: 2026-05-12
Project: Hexalith.Tenants
Trigger: Epic 2 retrospective readiness blocker
Scope classification: Minor direct adjustment
Owner: Development team

## 1. Issue Summary

During the Epic 2 retrospective verification pass, solution-level testing failed during integration test compilation:

```text
tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs(87,27): error CS0104:
'SubmitCommandRequest' is an ambiguous reference between
'Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest' and
'Hexalith.EventStore.Models.SubmitCommandRequest'
```

The affected integration test imports both `Hexalith.EventStore.Contracts.Commands` and `Hexalith.EventStore.Models`. EventStore now exposes `SubmitCommandRequest` in both namespaces: the contract type and a compatibility wrapper in Models. The unqualified constructor call no longer has a single compiler-resolvable target.

This is a technical integration drift issue caused by EventStore public API evolution, not a product requirement change.

## 2. Impact Analysis

### Epic Impact

- Epic 2 remains valid and implementation-complete.
- Story 2.4 is the affected story because it owns command API runtime integration tests and RFC 7807 command rejection behavior.
- No new epic is required.
- No epic sequencing change is required.

### Story Impact

- Story 2.4 verification evidence needs a small correction: command API runtime tests must explicitly construct the intended `Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest`.
- No acceptance criteria need to change.
- No PRD or MVP scope changes are required.

### Artifact Conflicts

- PRD: No conflict. The change supports FR49/FR53 validation and command API reliability.
- Epics: No structural change required.
- Architecture: No architecture decision change required. The existing EventStore command API pattern still applies.
- UX: Not applicable.
- Test artifact: `CommandApiRuntimeIntegrationTests.cs` requires a namespace-qualified type reference.

### Technical Impact

- The integration project compiles again.
- The affected non-DAPR command API runtime tests pass.
- DAPR/Aspire integration tests still require local DAPR placement service availability on `localhost:6050`; that is an environment prerequisite, not part of this compile fix.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale:

- The issue is a single ambiguous type reference in test code.
- Qualifying the intended contract type is clearer than removing broad usings that are still needed by other test types.
- No rollback is useful.
- No MVP review is needed.
- Risk is low because the API controller consumes the contract type and the Models type is only a compatibility wrapper.

## 4. Detailed Change Proposal

Story: Story 2.4 — Tenant Service, Bootstrap & Event Publishing
Section: Runtime integration test implementation

OLD:

```csharp
var request = new SubmitCommandRequest(
    Guid.NewGuid().ToString(),
    "system",
    "tenants",
    "global-administrators",
    nameof(BootstrapGlobalAdmin),
    payload);
```

NEW:

```csharp
var request = new Hexalith.EventStore.Contracts.Commands.SubmitCommandRequest(
    Guid.NewGuid().ToString(),
    "system",
    "tenants",
    "global-administrators",
    nameof(BootstrapGlobalAdmin),
    payload);
```

Rationale:

The test now documents that the command API payload uses the EventStore contract request type. This removes ambiguity while preserving the existing imports needed for `DomainServiceRequest` and related EventStore model types.

## 5. Checklist Results

| Item | Status | Notes |
| --- | --- | --- |
| 1.1 Triggering story identified | Done | Story 2.4 command API runtime integration tests. |
| 1.2 Core problem defined | Done | Technical API namespace collision after EventStore type exposure changed. |
| 1.3 Evidence gathered | Done | CS0104 compiler error from solution-level test run. |
| 2.1 Current epic assessed | Done | Epic 2 remains complete; verification blocker only. |
| 2.2 Epic-level changes | N/A | No epic scope change. |
| 2.3 Future epic review | Done | No future epic invalidated. |
| 2.4 New/obsolete epics | N/A | No new epic needed. |
| 2.5 Priority/order impact | N/A | No resequencing needed. |
| 3.1 PRD conflicts | Done | No PRD conflict. |
| 3.2 Architecture conflicts | Done | No architecture conflict. |
| 3.3 UX conflicts | N/A | No UX impact. |
| 3.4 Other artifacts | Done | Integration test source updated. |
| 4.1 Direct adjustment | Viable | Low effort, low risk. |
| 4.2 Rollback | Not viable | Rollback would not simplify the issue. |
| 4.3 MVP review | Not viable | MVP scope unaffected. |
| 4.4 Recommended path | Done | Direct adjustment selected. |
| 5.1 Issue summary | Done | Included above. |
| 5.2 Impact summary | Done | Included above. |
| 5.3 Rationale | Done | Included above. |
| 5.4 MVP impact/action plan | Done | MVP unaffected; action is test source qualification. |
| 5.5 Handoff plan | Done | Development team implements; QA verifies build and focused tests. |
| 6.1 Checklist completion | Done | Applicable items addressed. |
| 6.2 Proposal accuracy | Done | Verified against compiler error and source change. |
| 6.3 User approval | Done | User requested `bmad-correct-course fix`; treated as approval for minor direct implementation. |
| 6.4 Sprint status updates | N/A | No epics or stories added/removed/renumbered. |
| 6.5 Next steps | Done | See handoff below. |

## 6. Implementation Handoff

Development:

- Apply the explicit namespace qualification in `CommandApiRuntimeIntegrationTests.cs`.

QA:

- Verify solution build:

```powershell
dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore
```

- Verify affected command API runtime tests:

```powershell
dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~CommandApiRuntimeIntegrationTests"
```

Known environment note:

- Full integration execution still requires DAPR placement service on `localhost:6050` via `dapr init`. Without it, DAPR/Aspire fixture tests fail pre-flight even after this compile fix.

## 7. Verification

Implemented change:

- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`

Verification results:

- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build --filter "FullyQualifiedName~CommandApiRuntimeIntegrationTests"`: passed, 2/2.
- Full `Hexalith.Tenants.IntegrationTests` execution compiles but fails DAPR/Aspire prerequisite checks because DAPR placement is not reachable on `localhost:6050`.

## 8. Decision

Approve and implement as a minor direct adjustment.

Bob (Scrum Master): "This change keeps the plan intact. We corrected a verification blocker caused by public API ambiguity and left the separate DAPR environment prerequisite clearly visible."
