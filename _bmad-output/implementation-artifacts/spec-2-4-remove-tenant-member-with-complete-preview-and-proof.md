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

### Review Findings

- [x] [Review][Patch] Fail closed on live audit-proof capability rather than gateway registration alone [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:591]
- [x] [Review][Patch] Follow audit pagination when assembling WP-2A removal proof [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:801]
- [x] [Review][Patch] Preserve the original causal lower bound when retrying with the same message ID [src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:728]
- [x] [Review][Patch] Promote audit available only from current lifecycle-backed evidence and a ready receipt [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:818]
- [x] [Review][Patch] Wire rendered audit recovery and receipt inspection actions to real behavior [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:173]
- [x] [Review][Patch] Resolve positive and paged global-administrator standing before suppressing elevated friction [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:436]
- [x] [Review][Patch] Keep the supplementary global-administrator read from blocking primary tenant detail [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:399]
- [x] [Review][Patch] Replace stale Epic 5 and unsupported access-restoration preview promises [src/Hexalith.Tenants.UI/Resources/TenantsResources.resx:2193]
- [x] [Review][Patch] Add component coverage for audit-provenance confirmation, later proof recovery, parent capability gating, and page-level GA wiring [tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs:518]
- [x] [Review][Patch] Derive remove eligibility from a tenant-scoped current authoritative audit read, with generation-safe non-blocking failure [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1470]
- [x] [Review][Patch] Bound and cancel supplementary GA pagination; degrade retained rows to incomplete unknown evidence after refresh faults [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1349]
- [x] [Review][Patch] Bound and cancel removal-proof pagination while continuing past weak matches to later current projection-backed receipts [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:896]
- [x] [Review][Patch] Preserve a coalesced projection refresh requested during a status-only refresh [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:786]
- [x] [Review][Patch] Forward audit inspection distinctly and render only recovery actions backed by real delegates [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor:71]
- [x] [Review][Patch] Remove escalation/navigation semantic substitution and align EN/FR recovery copy with rendered actions [src/Hexalith.Tenants.UI/Resources/TenantsResources.resx:2208]
- [x] [Review][Patch] Cover stale, mismatched, missing, cyclic, capped, cancelled, coalesced, late-route, and callback fail-closed paths [tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs:285]
- [x] [Review][Defer] Default HEAD-based gitlink validation includes seven post-story dependency bumps [_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md:1] — deferred, pre-existing

**Acceptance Criteria:**
- Given remove eligibility is calculated, when freshness, auth, preview completeness, or layout safety is indeterminate, then the action fails closed with localized reason and named recovery.
- Given an eligible removal opens, when the preview renders, then all ten required items plus last-owner/target-GA risk appear in a focus-trapped destructive dialog; cancel/Escape never dispatch and focus returns to the launcher.
- Given the user confirms a current complete preview, when submit runs, then `RemoveUserFromTenant` is dispatched once with retained messageId under AggregateIdentity lock, using Story 2.1 confirmation rules without optimistic removal.
- Given EN/FR resources and focused tests run, when verification completes, then elevated-risk, fail-closed, dialog, lock, and dispatch scenarios pass without asserting WP-2A/`audit_available` complete.

## Spec Change Log

- 2026-08-08: Scope split — regenerated for 2.4a; deferred 2.4b WP-2A proof/reconciliation to `deferred-work.md`.
- 2026-08-08: 2.4a implemented — dialog, ten-item preview with platform-standing, live GA wiring; adversarial review patches applied.
- 2026-08-20: Review patches implemented — live proof capability, paged/current proof and GA evidence, real recovery actions, non-blocking supplementary reads, corrected EN/FR copy, and focused regression coverage.
- 2026-08-20: Review follow-up implemented — authoritative live audit capability, fail-closed bounded/cancellable GA and proof walks, retained-evidence degradation, lossless refresh coalescing, delegate-accurate recovery actions/copy, and route-generation regressions.

## Design Notes

Platform-standing is preview item #9; known GA also raises an elevated sibling risk banner. Incomplete GA evidence stays Unknown (never invents NotReflected). Destructive confirmation uses the existing Tenants `role="dialog"` + focus-sentinel pattern; Cancel/Refresh/Continue-read-only stay outside the CSS-hidden narrow form. Honest audit handoff (no WP-2A / `audit_available`) until 2.4b.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --filter "FullyQualifiedName~RemoveTenantMember|FullyQualifiedName~TenantRemoveMember|FullyQualifiedName~TenantDetailSurfaceTests"` -- 226 passed, 0 failed, 0 skipped
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~RemoveTenantMember|FullyQualifiedName~TenantRemoveMember|FullyQualifiedName~TenantDetailSurfaceTests|FullyQualifiedName~AuditEvidenceReceiptTests|FullyQualifiedName~AuditAvailabilityStateTests"` -- 253 passed, 0 failed, 0 skipped
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore` -- 2077 passed, 0 failed, 0 skipped
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore` -- passed with 0 warnings and 0 errors
- Focused test result: 253 passed, 0 failed, 0 skipped
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` -- fails only on the seven pre-existing post-baseline dependency bumps recorded in the deferred review finding above

## Suggested Review Order

**Removal proof lifecycle**

- Entry: coalesce status work without losing authoritative projection-refresh intent.
  [`RemoveTenantMemberFlow.razor:786`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L786)

- Bound and cancel audit paging while continuing from weak to strong evidence.
  [`RemoveTenantMemberFlow.razor:896`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L896)

- Preserve retry causality and require current, receipt-ready proof before promotion.
  [`TenantCreateCommandModels.cs:988`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L988)

- Route receipt inspection with support-safe command context.
  [`RemoveTenantMemberFlow.razor:1098`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L1098)

**Authoritative eligibility evidence**

- Launch supplementary GA and audit reads without blocking primary tenant detail.
  [`TenantDetailPage.razor:453`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L453)

- Aggregate GA pages with cancellation, bounds, and projection-version consistency.
  [`TenantDetailPage.razor:1406`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L1406)

- Prove tenant-scoped audit capability only from current authoritative responses.
  [`TenantDetailPage.razor:1529`](../../src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor#L1529)

- Feed proven capability into fail-closed member action slots.
  [`MemberAccessReview.razor:611`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor#L611)

**Honest recovery actions**

- Render only recovery verbs backed by real delegates.
  [`AuditAvailabilityState.razor:108`](../../src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor#L108)

- Forward inspection distinctly and hide inoperative receipt actions.
  [`AuditEvidenceReceipt.razor:189`](../../src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor#L189)

- Localize no-escalation recovery variants in English and French.
  [`TenantsResources.resx:3138`](../../src/Hexalith.Tenants.UI/Resources/TenantsResources.resx#L3138)

**Verification**

- Exercise bounded proof walks, cancellation, coalescing, and callback semantics.
  [`RemoveTenantMemberFlowTests.cs:784`](../../tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs#L784)

- Exercise incomplete GA evidence, page caps, refresh faults, and route races.
  [`TenantDetailSurfaceTests.cs:295`](../../tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs#L295)
