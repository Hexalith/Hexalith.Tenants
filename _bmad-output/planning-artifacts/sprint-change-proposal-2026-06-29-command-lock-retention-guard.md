---
title: "Sprint Change Proposal - Shared Command-Flow Guard for Command-Lock Retention"
date: "2026-06-29"
project_name: "Hexalith.Tenants"
author: "Correct Course workflow"
trigger: "epic-3-retro-2026-06-29-command-lock-retention"
mode: "Batch"
scope_classification: "Minor"
status: "APPROVED"
approval:
  approved_by: "Administrator"
  approved_at: "2026-06-29T16:54:37+02:00"
  approval_note: "Approved with 'yes'."
---

# Sprint Change Proposal: Shared Command-Flow Guard for Command-Lock Retention

## 1. Issue Summary

The active change trigger is the Epic 3 retrospective action item:

```text
Promote command-lock retention through accepted/projection-pending states into a shared command-flow guard.
```

The issue is not a missing product requirement. The PRD, UX, and architecture already require
projection-confirmed command success, one-at-a-time commands, and no optimistic success. The gap is
implementation durability: command-flow components still encode command activity locking locally, and
some flows can release the parent command-surface lock in `finally` even when the snapshot remains
`Accepted` or `ProjectionPending`.

Concrete evidence:

- `_bmad-output/implementation-artifacts/sprint-status.yaml` keeps
  `epic-3-retro-2026-06-29-command-lock-retention` open.
- `_bmad-output/implementation-artifacts/epic-3-retro-2026-06-29.md` records that Story 3.2 and
  Story 3.4 both needed review fixes to keep command activity locked through accepted/projection-
  pending states.
- `TenantLifecycleCommandFlow.razor` now has the correct local pattern: `IsOwnedCommandInFlight`
  covers `RequestSent`, `Accepted`, and `ProjectionPending`; parent activity updates through
  `UpdateCommandActivityForSnapshotAsync`.
- `RemoveTenantConfigurationFlow.razor` also has the corrected local pattern.
- `SetTenantConfigurationFlow.razor`, `AddTenantMemberFlow.razor`,
  `ChangeTenantMemberRoleFlow.razor`, `RemoveTenantMemberFlow.razor`, and
  `EditTenantMetadataFlow.razor` still contain direct
  `OnCommandActivityChanged.InvokeAsync(false)` calls in `finally`.

The risk is repeated regression: a developer can implement a command flow that passes gateway-level
tests but lets sibling tenant command surfaces re-enable before authoritative projection truth or a
terminal non-pending state.

## 2. Impact Analysis

### Epic Impact

Epic 3 remains done. This is retrospective follow-through, not a new FR15/FR16/FR17 product scope.

The behavior is cross-cutting for all command epics:

- Epic 2 tenant create/member/metadata command flows.
- Epic 3 lifecycle/configuration command flows.
- Epic 4 global-administrator command flows where sibling command surfaces share a page lock.
- Epic 5 correction command flows where command lifecycle must not imply false success.

No new epic is required.

### Story Impact

Affected completed story areas:

- Story 2.2 add tenant member.
- Story 2.3 change member role.
- Story 2.4 remove member.
- Story 2.5 edit metadata.
- Story 3.2 disable/enable tenant.
- Story 3.3 set configuration.
- Story 3.4 remove configuration.

No completed story needs product redefinition. The implementation should be a focused follow-up
artifact tied to the Epic 3 retrospective action item.

### Artifact Conflicts

PRD: no change required. PRD NFR-3 already requires projection truth and no optimistic success.

UX: no change required. `EXPERIENCE.md` already says one-at-a-time commands keep other triggers
unavailable while a command is in flight.

Architecture: a small enforcement clarification is useful. The architecture already says command-flow
tests must prove sibling command surfaces stay unavailable through `accepted` and
`projection_pending`; the proposed clarification is that command-flow code must route parent lock
retention through a shared guard instead of direct `finally` releases.

Epics/story guardrails: a small guardrail clarification is useful so future command stories require
the shared guard.

Sprint status: keep the action item open until implementation and representative tests pass. Mark it
done only after the shared guard is merged and validated.

### Technical Impact

UI-only change. No backend endpoint, command contract, EventStore persistence, projection, AppHost,
DAPR, FrontComposer shared module, or package change is proposed.

Expected code impact:

- Add one shared Tenants UI command-flow guard type, in its own `.cs` file.
- Migrate tenant detail command flows to use the guard when reporting parent command activity.
- Keep locally owned flows usable while their own command is in flight, even if the parent page lock
  has been set by that same flow.
- Add unit tests for the guard and representative component tests for accepted/projection-pending
  lock retention.
- Add or update a source-level regression check to prevent direct parent lock release in command-flow
  `finally` blocks.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale:

- The requirement already exists and is correct.
- Rollback would not help; the current issue is a repeated implementation pattern gap.
- MVP scope and product goals remain unchanged.
- A shared guard plus representative tests addresses the regression source without adding generic
  infrastructure outside the Tenants UI command-flow boundary.

Effort estimate: Medium-low. The code change is conceptually small, but it touches several command
components and tests.

Risk level: Medium-low. The main risk is accidentally holding a command lock after a terminal
non-pending state. Guard tests must cover release states explicitly.

Timeline impact: Minimal. This can be routed directly to the Developer agent with Test Architect
review expectations.

## 4. Detailed Change Proposals

### Proposal A - Add a Shared Command-Flow Guard

Artifact: `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCommandFlowGuard.cs`

OLD:

```text
No shared guard exists. Each command component decides when parent command activity is active.
Some components release parent command activity directly in finally.
```

NEW:

```text
Add a single Tenants UI command-flow guard that defines parent/sibling command-surface retention:

- active while local submission is running;
- active while snapshot state is RequestSent, Accepted, or ProjectionPending;
- inactive for Confirmed, Rejected, AlreadyApplied, DuplicatePrevented, Failed, Degraded,
  UnableToVerify, Previewed, and Idle.

All command-flow components use this guard to report parent command activity.
```

Rationale: The lock rule is a shared command-flow safety invariant, not story-local business logic.

### Proposal B - Replace Direct Parent Lock Releases

Artifacts:

- `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor`
- preserve or simplify the already-correct patterns in
  `TenantLifecycleCommandFlow.razor` and `RemoveTenantConfigurationFlow.razor`

OLD:

```csharp
finally
{
    _isSubmitting = false;
    await InvokeAsync(() => OnCommandActivityChanged.InvokeAsync(false)).ConfigureAwait(false);
}
```

NEW:

```csharp
finally
{
    _isSubmitting = false;
    await UpdateCommandActivityForSnapshotAsync().ConfigureAwait(false);
}
```

Where `UpdateCommandActivityForSnapshotAsync` uses the shared guard, not a flow-local interpretation.

Rationale: Accepted and projection-pending command work must keep sibling tenant command surfaces
unavailable until projection truth confirms or a terminal non-pending state releases the lock.

### Proposal C - Add Guard and Representative Component Tests

Artifacts:

- `tests/Hexalith.Tenants.UI.Tests/State/TenantCommandFlowGuardTests.cs`
- command component tests under `tests/Hexalith.Tenants.UI.Tests/Components/`
- optional source guard in the existing UI conformance/governance test area

OLD:

```text
Lifecycle and remove-configuration have focused lock-retention tests.
Other command-flow tests commonly assert only request wrapping or duplicate submit blocking.
```

NEW:

```text
Add guard tests proving:

- RequestSent, Accepted, and ProjectionPending retain parent command activity.
- terminal non-pending states release parent command activity.

Add representative component tests proving:

- accepted/completed status with no matching projection evidence keeps activity true;
- matching projection evidence changes state to Confirmed and emits false;
- rejected/failed/degraded/unable-to-verify release the lock and keep non-success state.

Add a source guard or equivalent review test that prevents command-flow components from directly
calling OnCommandActivityChanged.InvokeAsync(false) outside the shared guard path.
```

Rationale: The previous failures were caught in review, not by a shared pre-review guard. The new
tests make the retention behavior a reusable safety contract.

### Proposal D - Clarify Architecture Enforcement

Artifact: `_bmad-output/planning-artifacts/architecture.md`

Section: `Implementation Patterns & Consistency Rules` -> `Enforcement Guidelines`

OLD:

```markdown
**Pattern Enforcement:** bUnit asserts non-collapse + the six states + no-color-only; Playwright
asserts the required acceptance scenarios (stale projection, rejected command, unknown
confirmation, audit unavailable, last-owner warning, permission-missing, command accepted but still
projection-pending, focus escape/cancel no-commit, and data unavailable but not authorization-denied)
keyed on `data-testid`; a guard test fails any surface that references a raw state literal instead of
the Vocabulary library. Command-flow tests must prove sibling command surfaces stay unavailable
through `accepted` and `projection_pending` until projection truth or a terminal non-pending state.
Pattern changes are recorded here + in `project-context.md`.
```

NEW:

```markdown
**Pattern Enforcement:** bUnit asserts non-collapse + the six states + no-color-only; Playwright
asserts the required acceptance scenarios (stale projection, rejected command, unknown
confirmation, audit unavailable, last-owner warning, permission-missing, command accepted but still
projection-pending, focus escape/cancel no-commit, and data unavailable but not authorization-denied)
keyed on `data-testid`; a guard test fails any surface that references a raw state literal instead of
the Vocabulary library. Command-flow code must route parent/sibling command-surface activity through
the shared command-flow guard; no flow may release the parent lock directly in a `finally` block.
Command-flow tests must prove sibling command surfaces stay unavailable through `accepted` and
`projection_pending` until projection truth or a terminal non-pending state. Pattern changes are
recorded here + in `project-context.md`.
```

Rationale: This converts the already-stated behavior into a concrete implementation rule.

### Proposal E - Clarify Story Creation Guardrails

Artifact: `_bmad-output/planning-artifacts/epics.md`

Section: `Story Creation Guardrails`

OLD:

```markdown
Every story created from these epics must make the safety contract explicit in acceptance criteria and test expectations. Each story states the actor and job, names the projection truth source and staleness behavior, names the permission boundary and server-side authorization result, preserves pending/failed/denied/unknown states without false Success, consumes existing backend endpoints without adding local Tenants infrastructure, includes Tenants-owned `.resx` copy, and identifies the required accessibility, responsive, live-region, forced-colors, and stable `data-testid` evidence. Every command story also includes audit/evidence behavior, including delayed or unavailable audit states, and every story includes a test contract naming the fixture, observable state, and automation level such as unit, component, API, or Playwright.
```

NEW:

```markdown
Every story created from these epics must make the safety contract explicit in acceptance criteria and test expectations. Each story states the actor and job, names the projection truth source and staleness behavior, names the permission boundary and server-side authorization result, preserves pending/failed/denied/unknown states without false Success, consumes existing backend endpoints without adding local Tenants infrastructure, includes Tenants-owned `.resx` copy, and identifies the required accessibility, responsive, live-region, forced-colors, and stable `data-testid` evidence. Every command story also includes audit/evidence behavior, including delayed or unavailable audit states, and every story includes a test contract naming the fixture, observable state, and automation level such as unit, component, API, or Playwright.

Every command story or correction request that reports page-level command activity must use the shared command-flow guard. Representative tests must prove `accepted` and `projection_pending` command work keeps sibling command surfaces unavailable until projection truth confirms or a terminal non-pending state releases the lock.
```

Rationale: Future command stories should inherit the lock-retention rule before implementation, not
rediscover it in review.

### Proposal F - Sprint Status Closure After Implementation

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
- id: epic-3-retro-2026-06-29-command-lock-retention
  epic: 3
  action: "Promote command-lock retention through accepted/projection-pending states into a shared command-flow guard."
  owner: "Amelia (Developer) and Murat (Test Architect)"
  status: open
  source: "_bmad-output/implementation-artifacts/epic-3-retro-2026-06-29.md"
```

NEW after implementation and test verification:

```yaml
- id: epic-3-retro-2026-06-29-command-lock-retention
  epic: 3
  action: "Promote command-lock retention through accepted/projection-pending states into a shared command-flow guard."
  owner: "Amelia (Developer) and Murat (Test Architect)"
  status: done
  source: "_bmad-output/implementation-artifacts/epic-3-retro-2026-06-29.md"
```

Rationale: The status should remain open after proposal approval until the guard is implemented and
validated.

## 5. Change Analysis Checklist

Section 1 - Trigger and context:

- [x] Trigger identified: Epic 3 retrospective action item
  `epic-3-retro-2026-06-29-command-lock-retention`.
- [x] Core problem defined: repeated story-local lock-retention fixes need a shared guard.
- [x] Evidence gathered from sprint status, Epic 3 retro, architecture enforcement language, and
  current command-flow component code.

Section 2 - Epic impact:

- [x] Current epic impact assessed: Epic 3 stays done; this is retrospective follow-through.
- [x] Required epic-level changes assessed: no new epic; small cross-cutting guardrail clarification.
- [x] Remaining epics reviewed: Epics 4 and 5 keep their command/correction scope; no invalidation.
- [x] No epic removal, rollback, or priority resequencing required.

Section 3 - Artifact impact:

- [x] PRD checked: no change needed.
- [x] Architecture checked: enforcement clarification recommended.
- [x] UX checked: no change needed; existing one-at-a-time command and non-collapse rules apply.
- [x] Sprint/status impact documented: action item closes only after implementation and validation.

Section 4 - Path forward:

- [x] Direct Adjustment is viable.
- [N/A] Rollback is not useful.
- [N/A] MVP review is not needed.
- [x] Recommended path selected: direct Developer implementation with focused Test Architect review.

Section 5 - Proposal components:

- [x] Issue summary written.
- [x] Epic and artifact impact documented.
- [x] Recommended path and rationale documented.
- [x] PRD/MVP impact stated as unchanged.
- [x] Implementation handoff plan defined.

Section 6 - Final review and handoff:

- [x] User approval recorded: Administrator approved this proposal on 2026-06-29T16:54:37+02:00.
- [!] Sprint status update remains pending implementation and validation. Keep
  `epic-3-retro-2026-06-29-command-lock-retention` open until the shared guard lands and the
  representative UI tests pass.

## 6. Implementation Handoff

Scope classification: Minor, direct Developer implementation. Although several UI components are
touched, this does not require product replan or backlog reorganization.

Route to: Developer agent, with Test Architect review focus.

Implementation tasks:

- Add `TenantCommandFlowGuard` as a single-purpose C# type in its own file.
- Replace direct `OnCommandActivityChanged.InvokeAsync(false)` releases in command-flow components
  with guard-based updates.
- Preserve the corrected lifecycle and remove-configuration lock-retention behavior.
- Add `TenantCommandFlowGuardTests`.
- Add representative component tests for accepted/projection-pending retention and terminal release.
- Add a source guard or review test preventing direct parent lock release in command-flow `finally`
  blocks.
- Run focused UI tests, then the full UI test project if restore/build state allows.

Success criteria:

- `RequestSent`, `Accepted`, and `ProjectionPending` retain parent command activity.
- `Confirmed`, `Rejected`, `AlreadyApplied`, `DuplicatePrevented`, `Failed`, `Degraded`, and
  `UnableToVerify` release parent command activity.
- Sibling tenant command surfaces remain unavailable while accepted/projection-pending work lacks
  matching projection evidence.
- Matching projection evidence confirms the command and releases the parent lock.
- Terminal non-pending command outcomes release the parent lock without showing false Success.
- No command-flow component directly releases the parent page lock in a `finally` block.

Recommended validation:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore
tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none
```

If the known .NET 10 Microsoft.Testing.Platform/VSTest issue appears, use the xUnit v3 executable
fallback and record the exact result.

## 7. Approval

Approved by Administrator on 2026-06-29T16:54:37+02:00.

Route to Developer implementation with Test Architect review focus.
