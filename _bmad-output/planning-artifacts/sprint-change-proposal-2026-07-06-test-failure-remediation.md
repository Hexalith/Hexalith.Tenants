---
title: "Sprint Change Proposal - Test Failure Remediation"
date: "2026-07-06"
project_name: "Hexalith.Tenants"
author: "Correct Course workflow"
trigger: "User requested run tests and fix failed"
mode: "Batch"
scope_classification: "Minor"
status: "IMPLEMENTED_AND_VERIFIED"
approval_source: "Direct user request to run tests and fix failures"
---

# Sprint Change Proposal: Test Failure Remediation

## 1. Issue Summary

The trigger was a failing local verification run. Release build initially failed
on two issues after the current EventStore submodule/package surface changed:

- `ListTenantsQuery` carried explicit REST query-binding metadata that the
  EventStore REST generator now rejects because its entity binding source is
  `None` while an entity value is present.
- `TenantQueryResult.Metadata` hid the inherited `QueryResult.Metadata` member
  after the EventStore client base type added metadata support.

After those build failures were fixed, `Hexalith.Tenants.Server.Tests` failed at
runtime with `FileNotFoundException` for `Hexalith.EventStore.Client,
Version=3.41.0.0`. The Tenants package graph pins EventStore packages to
`3.41.0`, but the source-only `Hexalith.EventStore.Gateway` project reference
was compiling the EventStore source graph with the submodule's default assembly
version, `3.31.0`.

## 2. Impact Analysis

Epic impact: none. This is compatibility remediation against the current local
dependency surface and does not reopen, add, remove, or reorder epics.

Story impact: none. No story acceptance criteria or sprint backlog state changed.

PRD, architecture, and UX impact: none. Contracts and runtime wiring were
aligned to the existing behavior; no user flow, requirement, data model, or
route shape changed.

Technical impact:

- Query contract metadata updated for the stricter EventStore REST generator.
- Tenant query result metadata now flows through the EventStore base result type.
- The source-only EventStore gateway graph is built with the same assembly
  version as the packaged EventStore dependencies used by the default Tenants
  build.

## 3. Recommended Approach

Selected path: Direct Adjustment.

Rationale: the failures were local compatibility defects with low blast radius.
Rollback would not help, and MVP or backlog review is unnecessary because the
fix preserves existing product behavior.

Effort: Low. Risk: Low. The changes are narrow, covered by existing and updated
tests, and avoid editing the EventStore submodule.

## 4. Detailed Change Proposals

### Proposal A - Remove Redundant Query Binding

Artifact: `src/Hexalith.Tenants.Contracts/Queries/ListTenantsQuery.cs`

OLD:

```csharp
[RestRoute(RestVerb.Get, "", ApiScope = "tenants")]
[RestQueryBinding(RestQueryBindingSource.Constant, "index")]
public sealed class ListTenantsQuery : IQueryContract {
```

NEW:

```csharp
[RestRoute(RestVerb.Get, "", ApiScope = "tenants")]
public sealed class ListTenantsQuery : IQueryContract {
```

Rationale: this query has no aggregate/entity route binding. The generator's
default route metadata is sufficient and avoids invalid `None` entity metadata.

### Proposal B - Use QueryResult Metadata Support

Artifact: `src/Hexalith.Tenants/Queries/TenantQueryResult.cs`

OLD:

```csharp
: base(success, payloadBytes, errorMessage, projectionType) => Metadata = metadata;

public QueryResponseMetadata? Metadata { get; init; }
```

NEW:

```csharp
: base(success, payloadBytes, errorMessage, projectionType, metadata) {
}
```

Rationale: the EventStore client base `QueryResult` now owns metadata. Forwarding
metadata to the base type removes member hiding and keeps query responses on the
shared contract.

### Proposal C - Align Source-Only Gateway Assembly Version

Artifacts:

- `Directory.Build.props`
- `src/Hexalith.Tenants/Hexalith.Tenants.csproj`

Change:

```xml
<HexalithEventStoreSourceGatewayVersion Condition="'$(HexalithEventStoreSourceGatewayVersion)' == ''">3.41.0</HexalithEventStoreSourceGatewayVersion>

<ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Gateway\Hexalith.EventStore.Gateway.csproj"
                  AdditionalProperties="Version=$(HexalithEventStoreSourceGatewayVersion)" />
```

Rationale: `Hexalith.EventStore.Gateway` is source-only in this workspace. In
default NuGet mode, its source graph must produce assemblies with the same
identity as the packaged EventStore dependencies (`3.41.0`) so runtime binding
is deterministic.

### Proposal D - Update Metadata Tests

Artifact:
`tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryRestMetadataTests.cs`

Change: assert that `ListTenantsQuery` has no explicit
`RestQueryBindingAttribute`, while preserving the route and payload property
assertions.

Rationale: tests now reflect the valid REST metadata shape.

## 5. Checklist Results

| Item | Status | Notes |
|---|---|---|
| Trigger identified | [x] | Build/test failures from current dependency surface. |
| Core problem defined | [x] | REST metadata compatibility, inherited metadata hiding, and EventStore assembly-version mismatch. |
| Evidence gathered | [x] | Build diagnostics and Server.Tests runtime load failure. |
| Epic impact reviewed | [x] | No epic change required. |
| Artifact conflicts reviewed | [x] | No PRD, architecture, UX, or sprint-status change required. |
| Direct adjustment evaluated | [x] | Viable, low risk, implemented. |
| Rollback evaluated | [N/A] | Would not address the dependency compatibility defects. |
| MVP review evaluated | [N/A] | Product scope unchanged. |
| Handoff plan | [x] | Developer implementation complete; no further role handoff required. |

## 6. Verification

Restore and build:

```bash
dotnet restore Hexalith.Tenants.slnx
dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore
```

Result: build succeeded with 0 warnings and 0 errors.

Test projects:

```bash
DiffEngine_Disabled=true dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-build
DiffEngine_Disabled=true dotnet test tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj --configuration Release --no-build
DiffEngine_Disabled=true dotnet test tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj --configuration Release --no-build
DiffEngine_Disabled=true dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build
DiffEngine_Disabled=true dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-build
DiffEngine_Disabled=true dotnet test samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Release --no-build
DiffEngine_Disabled=true dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build
```

Result: 2,109 passed, 0 failed, 1 skipped.

- Contracts: 111 passed.
- Client: 48 passed.
- Testing: 181 passed.
- UI: 864 passed.
- Server: 738 passed.
- Sample: 39 passed.
- Integration: 128 passed, 1 skipped.

Additional check:

```bash
git diff --check -- Directory.Build.props src/Hexalith.Tenants.Contracts/Queries/ListTenantsQuery.cs src/Hexalith.Tenants/Hexalith.Tenants.csproj src/Hexalith.Tenants/Queries/TenantQueryResult.cs tests/Hexalith.Tenants.Contracts.Tests/Queries/QueryRestMetadataTests.cs
```

Result: clean.

## 7. Handoff

Scope classification: Minor.

Implementation status: complete and verified.

No sprint-status update is required because no epic, story, or deferred-work
tracking state changed.
