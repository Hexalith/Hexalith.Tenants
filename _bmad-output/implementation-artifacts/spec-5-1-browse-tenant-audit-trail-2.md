---
title: 'Close Story 5.1 Audit Performance Gate'
type: 'feature'
created: '2026-09-23'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - '_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md'
  - '_bmad-output/implementation-artifacts/story-5-1-performance-decision.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.1's audit UI and functional verification are complete, but no full-stack measurement proves compliance with its approved performance contract.

**Approach:** Measure the current grid with the approved browser tier and apply the approved paging or virtualization fallback only if a valid batch misses.

**Decision:** Jérôme Piquot, Owner, approved contract revision 1 on 2026-09-23. Approval is not evidence of a measured pass.

## Boundaries & Constraints

**Always:** Follow approved revision 1 exactly. Record commands, environment, dataset hash, raw samples, percentiles, and results. Preserve server order, cursor scope, critical fields, EN/FR, and accessibility.

**Never:** Use historical results as acceptance, claim a pass without evidence, add a generic timeline, or alter `sprint-status.yaml` as a substitute for evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Baseline | Approved seed and authenticated UI | Three complete batches per action and viewport | Wrong rows or self-skip fail the run |
| Miss | A valid batch exceeds a budget | Apply 25-row fallback, then bounded rendering if needed; rerun | Keep Story 5.1 open until measured pass |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/story-5-1-performance-decision.md` -- approved revision 1 and sole performance authority; prior Story 5.1 spec holds functional evidence and status.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` and `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor` -- 50-row grid and any fallback; preserve server order and critical fields.
- `src/Hexalith.Tenants.UI/Properties/launchSettings.json` and `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json` -- fixed OIDC callback; existing integration fixture disables Keycloak and cannot prove this contract.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/IReadModelStore.cs` -- seed through the platform seam, not a domain-owned state-store call.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/implementation-artifacts/story-5-1-performance-decision.md` -- record approval of revision 1 without claiming a pass.
- [ ] `tests/performance/tenant-audit/seed.cs` -- generate the approved deterministic 500-entry projection through the EventStore read-model seam in an isolated test topology and verify the persisted end-state before timing.
- [ ] `tests/performance/tenant-audit/audit-performance.spec.ts` and `tests/performance/tenant-audit/package.json` -- pin authenticated Chromium measurement of approved actions/viewports; preserve raw samples and nearest-rank percentiles; fail on self-skip or wrong rows.
- [ ] `scripts/run-tenant-audit-performance.sh` -- start Release Aspire at the registered callback, run seed and browser tiers, capture environment metadata, and stop resources.
- [ ] `_bmad-output/implementation-artifacts/story-5-1-performance-evidence.md` -- record exact commands, environment, dataset hash, raw results, percentiles, and functional gates.
- [ ] `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` and `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs` -- on a miss, apply the approved 25-row fallback, prove paging, and remeasure; if still failing, bound rendering in `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`.
- [ ] `_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md` -- link approved decision and evidence, and update its terminal status only when the contract is satisfied.

**Acceptance Criteria:**
- Given approved revision 1, when the authenticated 500-entry grid is measured, then every batch meets its initial-render and interaction budgets or activates fallback.
- Given fallback, when remeasured, then budgets and functional gates pass together.
- Given absent or failing evidence, when status is evaluated, then Story 5.1 remains `awaiting-operator` without a measured pass claim.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `dotnet build Hexalith.Tenants.slnx --configuration Release -m:1 --no-restore` -- expected: zero warnings and errors after any fallback code change.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -class Hexalith.Tenants.UI.Tests.Components.TenantAuditPageTests -parallelMode none` -- expected: all focused audit page cases pass after any fallback code change.
- `scripts/run-tenant-audit-performance.sh` -- expected: three complete 40-sample batches per approved action and viewport, with exact evidence and no self-skip.
