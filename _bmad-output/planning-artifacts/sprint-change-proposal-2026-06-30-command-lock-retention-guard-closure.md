---
title: "Sprint Change Proposal - Command-Lock Retention Guard Closure"
date: "2026-06-30"
project_name: "Hexalith.Tenants"
author: "Correct Course workflow"
trigger: "epic-3-retro-2026-06-29-command-lock-retention"
mode: "Batch"
scope_classification: "Minor"
status: "IMPLEMENTED_AND_TRACKING_CLOSURE"
approval_source: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-command-lock-retention-guard.md"
implementation_commit: "939bebc7a7633b4266568e24f3516e6675b90d20"
---

# Sprint Change Proposal: Command-Lock Retention Guard Closure

## 1. Issue Summary

The change trigger remains the Epic 3 retrospective action item:

```text
Promote command-lock retention through accepted/projection-pending states into a shared command-flow guard.
```

The 2026-06-29 correct-course proposal for this item was already approved. The
current issue is tracking drift: the source implementation is present on `main`,
but `_bmad-output/implementation-artifacts/sprint-status.yaml` still listed the
retrospective action item as `open`.

Evidence:

- Approved planning proposal:
  `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-command-lock-retention-guard.md`.
- Implementation commit:
  `939bebc7a7633b4266568e24f3516e6675b90d20`
  (`Implement Tenant Command Flow Guard and associated tests`).
- Source guard:
  `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCommandFlowGuard.cs`.
- Regression tests:
  `tests/Hexalith.Tenants.UI.Tests/State/TenantCommandFlowGuardTests.cs` and
  `tests/Hexalith.Tenants.UI.Tests/CommandFlowGuardConformanceTests.cs`.
- Flow usage scan found `TenantCommandFlowGuard.RetainsCommandActivity` in all
  tenant command flows that report page command activity, and no direct
  `OnCommandActivityChanged.InvokeAsync(false)` release in Tenants command-flow
  `.razor` files.

## 2. Impact Analysis

Epic impact: Epic 3 remains done. No epic is reopened and no new epic is required.

Story impact: The implementation affects the previously completed command-flow
areas for member, metadata, lifecycle, and configuration flows. The corrective
work is already in source; this proposal only closes the remaining tracking gap.

Artifact impact:

- PRD, UX, and architecture already contain the invariant that command success is
  projection-proven and that command surfaces remain unavailable through
  accepted/projection-pending work.
- Sprint status needed one tracking update: mark
  `epic-3-retro-2026-06-29-command-lock-retention` as `done`.
- No backend contracts, EventStore persistence, AppHost, DAPR topology, or
  FrontComposer shared code changes are required.

Technical impact: none in this closure step. The code and tests were already
landed by the implementation commit.

## 3. Checklist Results

| Item | Status | Notes |
|---|---|---|
| 1.1 Triggering story/action identified | [x] | Epic 3 retrospective action item `epic-3-retro-2026-06-29-command-lock-retention`. |
| 1.2 Core problem defined | [x] | Tracking remained open after the shared guard implementation landed. |
| 1.3 Supporting evidence gathered | [x] | Commit, source files, flow scan, and focused test run recorded. |
| 2.1 Current epic impact | [x] | Epic 3 stays done. |
| 2.2 Epic-level changes | [N/A] | No epic scope change needed. |
| 2.3 Remaining planned epics | [x] | No sequencing change; Epic 5 remains independently in progress. |
| 2.4 New/obsolete epics | [N/A] | None. |
| 2.5 Priority/order | [N/A] | No backlog reordering. |
| 3.1 PRD conflicts | [x] | No conflict; PRD already requires projection-confirmed success. |
| 3.2 Architecture conflicts | [x] | No conflict; architecture already names command-flow lock retention through pending states. |
| 3.3 UI/UX conflicts | [x] | No conflict; UX requires one-at-a-time commands and no false success. |
| 3.4 Secondary artifacts | [x] | Sprint status was the only required artifact update. |
| 4.1 Direct adjustment | [x] | Viable and already implemented. Effort low; risk low. |
| 4.2 Rollback | [N/A] | Rollback would remove the correct guard. |
| 4.3 MVP review | [N/A] | MVP/product scope unchanged. |
| 4.4 Path selected | [x] | Direct tracking closure. |
| 5.1 Issue summary | [x] | Included above. |
| 5.2 Impact and adjustments | [x] | Included above. |
| 5.3 Recommendation | [x] | Close the action item as done. |
| 5.4 MVP/action plan | [x] | No MVP change; action item closure only. |
| 5.5 Handoff plan | [x] | Developer action closed; Test Architect can rely on focused guard tests and optional full UI suite. |
| 6.1 Checklist review | [x] | All applicable items addressed. |
| 6.2 Proposal accuracy | [x] | Grounded in current source and sprint artifacts. |
| 6.3 User approval | [x] | Uses the approved 2026-06-29 proposal as approval source; this run verifies implementation and closes tracking. |
| 6.4 Sprint status update | [x] | `epic-3-retro-2026-06-29-command-lock-retention` moved from `open` to `done`. |
| 6.5 Handoff confirmation | [x] | No further developer handoff required for this action item. |

## 4. Detailed Change Proposals

### Proposal A - Close the Sprint Action Item

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
- id: epic-3-retro-2026-06-29-command-lock-retention
  epic: 3
  action: "Promote command-lock retention through accepted/projection-pending states into a shared command-flow guard."
  owner: "Amelia (Developer) and Murat (Test Architect)"
  status: open
```

NEW:

```yaml
- id: epic-3-retro-2026-06-29-command-lock-retention
  epic: 3
  action: "Promote command-lock retention through accepted/projection-pending states into a shared command-flow guard."
  owner: "Amelia (Developer) and Murat (Test Architect)"
  status: done
```

Rationale: The shared guard and tests exist in source, the guard is used by the
command flows, direct release is protected by a conformance test, and focused
validation passed.

### Proposal B - Preserve the Existing Implementation

Artifacts:

- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCommandFlowGuard.cs`
- tenant command-flow `.razor` components under `src/Hexalith.Tenants.UI/Components/Tenants`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantCommandFlowGuardTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/CommandFlowGuardConformanceTests.cs`

OLD:

```text
Command-flow components could each decide how to release parent command activity.
```

NEW:

```text
Command-flow components report parent activity through TenantCommandFlowGuard.
RequestSent, Accepted, and ProjectionPending retain page command activity; terminal
non-pending states release it.
```

Rationale: This keeps sibling command surfaces unavailable until projection truth
or a terminal non-pending state, matching PRD NFR-3, UX CP-3/CP-4, and the
architecture enforcement rule.

## 5. Verification

Initial package-only test attempt:

```bash
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --filter "FullyQualifiedName~TenantCommandFlowGuardTests|FullyQualifiedName~CommandFlowGuardConformanceTests"
```

Result: blocked by `NU1102` because `Hexalith.Commons.UniqueIds` `3.19.0` is not
available from the configured NuGet feed. This is the same local package-path
issue documented in Story 5.8.

Source-reference validation path:

```bash
dotnet restore tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -p:Configuration=Debug -p:UseHexalithProjectReferences=true
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -m:1 --no-restore -p:UseHexalithProjectReferences=true
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build -p:UseHexalithProjectReferences=true --filter "FullyQualifiedName~TenantCommandFlowGuardTests|FullyQualifiedName~CommandFlowGuardConformanceTests"
```

Result:

- Restore succeeded.
- Build succeeded with 0 warnings and 0 errors.
- Focused guard tests passed: 25 passed, 0 failed, 0 skipped.

## 6. Implementation Handoff

Scope classification: Minor.

Route: closed for Developer. Optional Test Architect follow-up is to run the full
UI suite with the same source-reference restore path if broader regression
evidence is desired.

Success criteria:

- Shared guard exists in its own C# file.
- `RequestSent`, `Accepted`, and `ProjectionPending` retain parent command
  activity.
- Terminal non-pending states release parent command activity.
- Tenant command-flow files no longer release page activity directly with
  `OnCommandActivityChanged.InvokeAsync(false)`.
- Focused guard tests pass.
- Sprint action item is marked `done`.
