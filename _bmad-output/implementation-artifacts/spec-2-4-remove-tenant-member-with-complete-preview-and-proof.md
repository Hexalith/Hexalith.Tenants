---
title: 'Remove Tenant Member with Complete Preview and Proof'
type: 'feature'
created: '2026-08-08'
status: 'done'
baseline_commit: '29c4aec965e9cba4165a8844a86edc67ba7d756b'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Remove-member has a consequence preview and command path, but eligibility is not fully fail-closed, platform-standing/GA friction is unwired, and the flow is an inline section rather than a focus-trapped destructive dialog — so deliberate removal remains unsafe to operate.

**Approach (2.4a only):** Close eligibility, complete ten-item preview (incl. platform-standing), elevated last-owner/target-GA friction, focus-trapped destructive dialog, AggregateIdentity-locked dispatch, and existing Story 2.1 projection-confirmation lifecycle. WP-2A / `audit_available` deferred to 2.4b (`deferred-work.md`).

## Boundaries & Constraints

**Always:**
- BFF-only: `RemoveUserFromTenantAsync` → `POST /api/v1/commands`; no new endpoints.
- Ten-item preview complete before confirm; missing item blocks dispatch.
- Fail closed on stale/missing/unknown validation, freshness, auth, lifecycle, preview completeness, or narrow unsafe layout.
- Last-owner removal allowed with elevated friction; tenant removal never changes GA standing.
- Reuse `messageId` per attempt; AggregateIdentity lock; keep Story 2.1 non-collapse lifecycle.
- EN/FR parity; `data-testid="tenants-remove-member-*"`.

**Ask First:**
- Replacing Tenants `role="dialog"` + focus-sentinel pattern with a different Fluent/FrontComposer dialog primitive.

**Never:**
- WP-2A proof assembly, `audit_available`, or proof-capability gating (deferred 2.4b).
- New preview/receipt/status endpoints; browser-direct calls; reshaped remove contracts.
- Confirming from acceptance/SignalR alone; optimistic row removal; editing events/projections.
- Promising undo/rollback/`restore intended access`; inventing last-GA hard-stops on membership removal.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eligible open | Fresh + authorized + preview complete | Focus-trapped destructive dialog; ten items + GA/platform-standing | Incomplete item blocks confirm |
| Last-owner / target-GA | OwnerCount==1 or target on GA list | Elevated friction + explicit risk; still allowed when authorized | GA standing unchanged |
| Confirm + dispatch | Complete preview; AggregateIdentity free | Submit once with retained messageId; lock held; Story 2.1 lifecycle | Surface down → fail closed |
| Overlap / fail-closed | In-flight sibling or stale/narrow layout | Unavailable with lock or localized reason | No dispatch |
| Cancel / Escape | Dialog open | No command; focus returns to launcher | N/A |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs` (+ interface/unavailable) -- remove submit + messageId reuse
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` -- `GetGlobalAdministratorsAsync` for live target-GA standing
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` -- `TenantRemoveMemberCommandSnapshot` (reuse ConfirmProjection; no WP-2A here)
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs` -- AggregateIdentity lock via `TenantDetailPage.SetCommandActivityAsync`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor` (+ `.css`) -- focus-trapped `role="dialog"`; ten preview items incl. platform-standing + consequences-versus-unknowns; elevated last-owner/GA friction; narrow form hide; dismiss/recovery outside form
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor` -- launch + focus return; resolves target GA friction from `GlobalAdministratorsSnapshot.IsCompleteEvidence`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` -- loads GA snapshot with detail/members (soft-fail); passes to `MemberAccessReview`
- Reuse: `RemoveTenantConfigurationFlow.razor` dialog/focus-sentinel pattern
- Resources/tests: `TenantsResources*.resx` `Tenants.RemoveMember.*`; `RemoveTenantMemberFlowTests.cs`; `TenantDetailSurfaceTests` / gateway suites

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor` (+ `.css`) -- focus-trapped destructive dialog; ten preview items + platform-standing; elevated friction; Escape/cancel no-dispatch + focus return; dispatch via existing gateway/snapshot
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` (+ `MemberAccessReview.razor`) -- wire live target-GA standing; fail-closed on incomplete preview/narrow layout; keep AggregateIdentity lock / SignalR nudge-only
- [x] `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` (+ `.fr.resx`) -- friction/unavailable/dialog copy parity as needed
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs` (+ surface/gateway as needed) -- incomplete preview, friction, Escape/focus, lock, dispatch; do not assert WP-2A complete
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- advance `2-4-remove-tenant-member-with-complete-preview-and-proof` with this slice (incomplete until 2.4b)

**Acceptance Criteria:**
- Given remove eligibility is calculated, when freshness, auth, preview completeness, or layout safety is indeterminate, then the action fails closed with localized reason and named recovery.
- Given an eligible removal opens, when the preview renders, then all ten required items plus last-owner/target-GA risk appear in a focus-trapped destructive dialog; cancel/Escape never dispatch and focus returns to the launcher.
- Given the user confirms a current complete preview, when submit runs, then `RemoveUserFromTenant` is dispatched once with retained messageId under AggregateIdentity lock, using Story 2.1 confirmation rules without optimistic removal.
- Given EN/FR resources and focused tests run, when verification completes, then elevated-risk, fail-closed, dialog, lock, and dispatch scenarios pass without asserting WP-2A/`audit_available` complete.

## Spec Change Log

- 2026-08-08: Scope split — regenerated for 2.4a; deferred 2.4b WP-2A proof/reconciliation to `deferred-work.md`.
- 2026-08-08: 2.4a implemented — dialog, ten-item preview with platform-standing, live GA wiring; adversarial review patches applied.

## Design Notes

Platform-standing is preview item #9; known GA also raises an elevated sibling risk banner. Incomplete GA evidence stays Unknown (never invents NotReflected). Destructive confirmation uses the existing Tenants `role="dialog"` + focus-sentinel pattern; Cancel/Refresh/Continue-read-only stay outside the CSS-hidden narrow form. Honest audit handoff (no WP-2A / `audit_available`) until 2.4b.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --filter "FullyQualifiedName~RemoveTenantMember|FullyQualifiedName~TenantRemoveMember|FullyQualifiedName~TenantDetailSurfaceTests"` -- expected: matching tests pass

## Suggested Review Order

**Destructive dialog + complete preview**

- Entry: `role="dialog"` / `aria-modal` remove flow with focus sentinels.
  [`RemoveTenantMemberFlow.razor:12`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L12)

- Ten-item consequence preview including platform-standing and merged consequences/unknowns.
  [`RemoveTenantMemberFlow.razor:354`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L354)

- Elevated last-owner / known-GA friction drives confirm label, help, and validation.
  [`RemoveTenantMemberFlow.razor:284`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L284)

- Cancel / Refresh / Continue-read-only stay outside the CSS-hidden narrow form.
  [`RemoveTenantMemberFlow.razor:129`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L129)

- Narrow layouts hide the form and surface the unavailable reason.
  [`RemoveTenantMemberFlow.razor.css:121`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor.css#L121)

**Live target-GA standing**

- Detail page loads/retains global-administrator evidence for member actions.
  [`TenantDetailPage.razor:404`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L404)

- Complete+current GA evidence resolves friction; incomplete stays Unknown.
  [`MemberAccessReview.razor:433`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor#L433)

- Friction and standing flags are passed into the open remove flow.
  [`MemberAccessReview.razor:268`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor#L268)

**Verification**

- Last-owner elevated copy, help, and mismatch validation before dispatch.
  [`RemoveTenantMemberFlowTests.cs:58`](../../tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs#L58)

- Live GA standing wired through MemberAccessReview (known vs incomplete).
  [`TenantDetailSurfaceTests.cs:2048`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs#L2048)

- Narrow CSS asserts the form is `display: none` under 767px.
  [`RemoveTenantMemberFlowTests.cs:157`](../../tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs#L157)
