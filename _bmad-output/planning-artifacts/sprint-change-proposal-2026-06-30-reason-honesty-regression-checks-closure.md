---
title: "Sprint Change Proposal - Command-Surface Reason-Honesty Regression Checks Closure"
date: "2026-06-30"
project_name: "Hexalith.Tenants"
author: "Correct Course workflow"
trigger: "epic-3-retro-2026-06-29-reason-honesty"
mode: "Batch"
scope_classification: "Minor"
status: "IMPLEMENTED_AND_TRACKING_CLOSURE"
approval_source: "_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-reason-honesty-regression-checks.md"
---

# Sprint Change Proposal: Command-Surface Reason-Honesty Regression Checks Closure

## 1. Issue Summary

The change trigger remains the Epic 3 retrospective action item:

```text
Add reason-honesty regression checks for degraded/unavailable/unknown projection states on command surfaces.
```

The 2026-06-29 correct-course proposal for this item was already approved. The current issue was
implementation and tracking closure: command surfaces still needed executable regression evidence
that degraded, unavailable, and unknown projection/read states do not render missing-permission copy.

## 2. Impact Analysis

Epic 3 remains done. No epic is reopened and no new story is required.

Story impact is limited to completed command surfaces in Epics 2 and 3:

- add member;
- change role;
- remove member;
- edit metadata;
- member action availability on tenant detail.

Lifecycle, configuration, global-administrator, and correction command surfaces were included in the
focused validation set because they consume the same non-collapse and fail-closed truth-state rules.

PRD, architecture, and UX artifacts already contain the invariant. No backend command contract,
projection, persistence, AppHost, DAPR topology, package, or FrontComposer shared change was needed.

## 3. Checklist Results

| Item | Status | Notes |
|---|---|---|
| 1.1 Triggering story/action identified | [x] | Epic 3 retrospective action item `epic-3-retro-2026-06-29-reason-honesty`. |
| 1.2 Core problem defined | [x] | Regression gap for reason honesty on degraded/unavailable/unknown command surfaces. |
| 1.3 Supporting evidence gathered | [x] | Approved proposal, sprint status, PRD/UX truth-state rules, affected UI components, and focused tests. |
| 2.1 Current epic impact | [x] | Epic 3 stays done. |
| 2.2 Epic-level changes | [N/A] | No epic scope change needed. |
| 2.3 Remaining planned epics | [x] | Existing command surfaces only; no resequencing. |
| 2.4 New/obsolete epics | [N/A] | None. |
| 2.5 Priority/order | [N/A] | No backlog reordering. |
| 3.1 PRD conflicts | [x] | No conflict; PRD already requires projection truth and honest unavailable reasons. |
| 3.2 Architecture conflicts | [x] | No conflict; architecture already requires fail-closed command surfaces. |
| 3.3 UI/UX conflicts | [x] | No conflict; UX requires missing permission to stay distinct from stale/degraded data. |
| 3.4 Secondary artifacts | [x] | Sprint status and test-summary evidence updated. |
| 4.1 Direct adjustment | [x] | Implemented. Effort low; risk low. |
| 4.2 Rollback | [N/A] | Rollback would restore the incorrect reason mapping. |
| 4.3 MVP review | [N/A] | Product scope unchanged. |
| 4.4 Path selected | [x] | Direct implementation and tracking closure. |
| 5.1-5.5 Proposal components | [x] | Issue, impact, approach, change details, and handoff recorded. |
| 6.1 Checklist review | [x] | Applicable checklist items addressed. |
| 6.2 Proposal accuracy | [x] | Grounded in current source and passing validation. |
| 6.3 User approval | [x] | Uses the approved 2026-06-29 proposal as approval source. |
| 6.4 Sprint status update | [x] | `epic-3-retro-2026-06-29-reason-honesty` moved from `open` to `done`. |
| 6.5 Handoff confirmation | [x] | Developer work complete; Test Architect can review the focused matrix and full UI pass. |

## 4. Detailed Change Proposals

### Proposal A - Keep Projection-State Reasons Distinct From Permission Reasons

Artifacts:

- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor`

OLD:

```text
Degraded, unavailable, and unknown detail/read states could share the same authorization branch as
true unauthorized states.
```

NEW:

```text
Only explicit unauthorized or `IsAuthorized == false` conditions render authorization copy.
Stale, unknown, degraded, and unavailable projection/read evidence render freshness/refresh copy and
keep command submission blocked.
```

Rationale: A data-availability problem must not tell the operator they lack permission. The recovery
path is refresh/retry/continue read-only/escalate, not permission request.

### Proposal B - Add Focused Regression Tests and Negative Controls

Artifacts:

- `tests/Hexalith.Tenants.UI.Tests/Components/AddTenantMemberFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/ChangeTenantMemberRoleFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/EditTenantMetadataFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`

OLD:

```text
Coverage existed for stale/unknown states and some story-local fixes, but the member and metadata
command surfaces lacked a matrix that proved data-unavailable reasons stay distinct from true
authorization failures.
```

NEW:

```text
The tests now assert:

- stale/unknown/degraded/unavailable projection states block command submission;
- those data states render refresh/freshness or stale-data copy, not authorization copy;
- true authorization failures still render permission copy;
- no blocked data state submits a command, opens a partial destructive preview, or shows success.
```

Rationale: This closes the retrospective action with executable evidence rather than planning text.

### Proposal C - Close the Sprint Action Item

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD:

```yaml
- id: epic-3-retro-2026-06-29-reason-honesty
  status: open
```

NEW:

```yaml
- id: epic-3-retro-2026-06-29-reason-honesty
  status: done
```

Rationale: The tests and fixes are implemented and validated.

## 5. Verification

Source-reference validation path:

```bash
dotnet restore tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -p:Configuration=Debug -p:UseHexalithProjectReferences=true
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -m:1 --no-restore -p:UseHexalithProjectReferences=true
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build -p:UseHexalithProjectReferences=true --filter "FullyQualifiedName~AddTenantMemberFlowTests|FullyQualifiedName~ChangeTenantMemberRoleFlowTests|FullyQualifiedName~RemoveTenantMemberFlowTests|FullyQualifiedName~EditTenantMetadataFlowTests|FullyQualifiedName~TenantDetailSurfaceTests|FullyQualifiedName~TenantLifecycleAvailabilityTests|FullyQualifiedName~SetTenantConfigurationFlowTests|FullyQualifiedName~RemoveTenantConfigurationFlowTests|FullyQualifiedName~GlobalAdministratorsPageTests|FullyQualifiedName~CorrectionStartPanelTests|FullyQualifiedName~GlobalAdministratorCorrectionPanelTests"
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build -p:UseHexalithProjectReferences=true
```

Results:

- Restore succeeded.
- Build succeeded with 0 warnings and 0 errors.
- Focused reason-honesty validation passed: 236 passed, 0 failed, 0 skipped.
- Full UI test project passed: 867 passed, 0 failed, 0 skipped.

## 6. Implementation Handoff

Scope classification: Minor.

Route: closed for Developer. Optional Test Architect follow-up is to inspect the focused matrix for
coverage sufficiency.

Success criteria:

- Degraded/unavailable/unknown projection states on command surfaces render data-availability or
  refresh reasons, not missing-permission reasons.
- Missing-permission reasons still render for true authorization failures.
- No command submission occurs while the action is blocked by data availability.
- Focused and full UI validation passed.
