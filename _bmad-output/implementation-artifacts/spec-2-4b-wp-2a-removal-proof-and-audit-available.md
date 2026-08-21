---
title: 'Remove Tenant Member WP-2A Proof and audit_available'
type: 'feature'
created: '2026-08-08'
status: 'in-progress'
baseline_commit: 'bb85fadb149fed1fa00dfd9c8d3315df541566e8'
review_loop_iteration: 3
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** After remove-member projection confirmation, audit stays frozen at `AuditPending`, `IsAuditAvailable` is hard-false, proof-capability fail-closed is unwired, and WP-2A proof is never assembled — so FR12/WP-2A remains incomplete after 2.4a.

**Approach (2.4b only):** Refine remove reconciliation provenance, assemble minimum WP-2A proof from the existing authorized audit read path, surface distinct `audit_available` only on matching evidence, fail closed when proof capability is indeterminate, and cover proof-state recovery with tests. Keep 2.4a dialog/preview/dispatch unchanged.

## Boundaries & Constraints

**Always:**
- Reuse `ITenantQueryGateway.GetTenantAuditAsync` + `TenantAuditReceipt` / `TenantAuditSupportSafety`; no new preview/receipt/status endpoints.
- Keep `confirmed` distinct from `audit_available`; SignalR/status polling only nudge re-query; never invent available without matching `UserRemovedFromTenant` evidence (tenant + target + causal lower bound).
- Available proof fields only: support-safe actor, target, tenant, outcome, absolute timestamp, projection marker, reference — no raw narrative/payload/token/correlation/ETag/cursor/stack in UI, copy, announcements, logs, or component state.
- Wire `UnavailableReason.MissingAuditProof` into remove eligibility when proof capability is stale/missing/unknown; incomplete proof never silently upgrades.
- Confirmed access outcome survives pending/delayed/unavailable/denied audit; honest named recoveries (wait, refresh, inspect audit, escalate, continue read-only).
- EN/FR parity; `data-testid="tenants-remove-member-*"`.

**Ask First:**
- Extending audit-provenance confirmation beyond remove-member into shared add/change helpers.
- Rendering a different receipt primitive than existing `AuditEvidenceReceipt` / `TenantAuditReceipt`.

**Never:**
- Re-open 2.4a dialog/preview/friction/GA-standing work; new command contracts; browser-direct calls.
- Claiming Epic 5 browse UI is required for FR12; promising undo/rollback/`restore intended access` without correction capability.
- Confirming from acceptance/SignalR alone; treating pre-existing absence as newly confirmed success; editing events/projections.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Proof capability missing | Audit query/capability indeterminate before open | Remove unavailable with `MissingAuditProof` + recovery | No dispatch |
| Confirmed, proof match | Absence + provenance; matching removal audit row | Lifecycle `confirmed`; audit `audit_available` + WP-2A receipt | Keep confirmed if later audit flap |
| Confirmed, no match yet | Absence confirmed; audit empty/unmatched | Stay `audit_pending` (or delayed/unavailable per truth) + recovery | Never invent available |
| Already applied / UTV | Pre-existing absence or missing baseline | `already applied` / `unable to verify`; no fake available | MissingSupport / AuditUnavailable |
| Audit denied/fail | Query unauthorized/error after confirm | Honest unavailable/delayed + recoveries; confirmed intact | No silent success |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` — `TenantCommandAuditState.AuditAvailable`; `TenantRemoveMemberCommandSnapshot` AttemptStartedAtUtc, ConfirmProjection(version OR audit provenance), ApplyRemovalProofMatch / ApplyRemovalProofQueryFailure, FindMatchingRemovalProof
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs` — version inequality + HasQualifyingAuditProvenance (>= attempt start)
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailability.cs` — Available state; `IsAuditAvailable` only when Available
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs` — WP-2A field set + support-safe redaction
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantQueryGateway.cs` (+ `TenantQueryGateway.GetTenantAuditAsync`) — existing audit read; command gateway stays submit+status only
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor` — QueryCorrectiveProofAsync match pattern adapted for `UserRemovedFromTenant`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor` — TryAssembleRemovalProofAsync; receipt when Available; MissingAuditProof fail-closed
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor` — RemoveMember emits MissingAuditProof when query gateway unavailable
- `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor` (+ `AuditAvailabilityState.razor`) — available proof UI
- Resources/tests: `TenantsResources*.resx` AuditAvailable keys; snapshot + flow matrix coverage; TenantDetailSurfaceTests registers capable query gateway
- Continuity: done 2.4a spec; sprint key `2-4-remove-tenant-member-with-complete-preview-and-proof`

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` (+ provenance helper as needed) -- add `AuditAvailable`; refine remove confirm/reconcile so absence + version OR safe audit-provenance can confirm; add post-confirm proof-match → `AuditAvailable` without collapsing lifecycle states
- [x] `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailability.cs` (+ receipt/availability components if needed) -- map Available; `IsAuditAvailable` true only with matching evidence; keep other command flows from inventing available
- [x] `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor` (+ `MemberAccessReview.razor`) -- post-confirm `GetTenantAuditAsync` WP-2A assembly; wire `MissingAuditProof` fail-closed; render receipt only when available; honest pending/delayed/unavailable recoveries
- [x] `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` (+ `.fr.resx`) -- AuditAvailable + proof/recovery/unavailable copy parity
- [x] `tests/Hexalith.Tenants.UI.Tests/...` (`TenantRemoveMemberCommandSnapshotTests`, `RemoveTenantMemberFlowTests`, availability/gateway as needed) -- matrix: MissingAuditProof, confirmed≠available, match→available, unmatched stays pending, already-applied/UTV, SignalR cannot invent available
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- advance Story 2.4 after 2.4b verification (canonical key incomplete until both 2.4a+2.4b pass)

**Acceptance Criteria:**
- Given proof capability is stale/missing/unknown, when remove availability is calculated, then the action fails closed with `MissingAuditProof` (or equivalent localized reason) and named recovery, without dispatch.
- Given projection-confirmed removal, when matching WP-2A audit evidence is assembled from `GetTenantAuditAsync`, then audit becomes `audit_available` with support-safe receipt fields only; without a match, confirmed remains distinct and audit stays pending/delayed/unavailable with honest recovery.
- Given already-applied, unable-to-verify, audit deny/error, or SignalR nudge, when reconciliation runs, then none invent `audit_available` or collapse acceptance/projection/proof into one success.
- Given EN/FR resources and focused tests, when verification completes, then matrix scenarios pass and Story 2.4 WP-2A completion criteria are met without new endpoints.

## Spec Change Log

## Design Notes

Mirror `CorrectionStartPanel.QueryCorrectiveProofAsync`: query audit with a causal lower bound from the attempt, match `UserRemovedFromTenant` + tenant + target, take newest qualifying row, map through `TenantAuditReceipt`. Prefer extending remove snapshot + flow first; only touch shared `TenantMembershipCommandProvenance` for a remove-safe audit-provenance confirm branch if version inequality alone cannot satisfy the Always clause. Do not flip `IsAuditAvailable` globally to true — gate on evidence-backed Available state.

## Verification

**Commands:**
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --filter "FullyQualifiedName~RemoveTenantMember|FullyQualifiedName~TenantRemoveMember|FullyQualifiedName~TenantAuditAvailability"` -- expected: matching tests pass

## Suggested Review Order

**Proof assembly entry**

- Post-confirm audit re-query + match → Available + receipt.
  [`RemoveTenantMemberFlow.razor:769`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L769)

- Render support-safe receipt only when evidence-backed Available.
  [`RemoveTenantMemberFlow.razor:189`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor#L189)

**Snapshot / provenance**

- Confirm with version advancement OR qualifying audit provenance.
  [`TenantCreateCommandModels.cs:822`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L822)

- Promote Available only on match; keep confirmed across flaps.
  [`TenantCreateCommandModels.cs:916`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L916)

- Causal lower-bound match for UserRemovedFromTenant rows.
  [`TenantCreateCommandModels.cs:980`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs#L980)

- Shared audit-provenance helper (>= attempt start).
  [`TenantMembershipCommandProvenance.cs:29`](../../src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs#L29)

**Fail-closed + availability**

- List remove emits MissingAuditProof when query gateway unavailable.
  [`MemberAccessReview.razor:591`](../../src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor#L591)

- IsAuditAvailable true only for Available state.
  [`TenantAuditAvailability.cs:28`](../../src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailability.cs#L28)

**Peripherals**

- Matrix coverage for pending / available / unauthorized audit paths.
  [`RemoveTenantMemberFlowTests.cs:518`](../../tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs#L518)
