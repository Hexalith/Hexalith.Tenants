---
title: 'Close Story 5.1 Audit Performance Gate'
type: 'feature'
created: '2026-09-23'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - '_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.1's audit trail implementation and repository-controlled verification are complete, but its Product/Operations performance contract remains unapproved. Without that decision, the story cannot claim performance acceptance or leave its recorded `awaiting-operator` state.

**Approach:** Record the approved contract, measure the existing flat audit grid against it using the approved test tier, and apply the approved paging or virtualization fallback only if the measurements trigger it. Preserve the existing Story 5.1 safety and accessibility guarantees.

## Boundaries & Constraints

**Always:** The approved decision must specify the representative 500-event dataset shape, page size and filter mix, environment and network assumptions, initial-render and interaction percentile budgets, authoritative test tier, repeatability method, and fallback trigger. Record exact commands, dataset, repetitions, percentiles, and results. Keep server order, protected cursor semantics, safety-critical fields, and accessible EN/FR behavior.

**Never:** Infer a budget or fallback from historical measurements or the separate warm tenant-read assumption; claim acceptance without approved criteria and passing evidence; add a generic audit timeline; alter `sprint-status.yaml` as a substitute for the decision.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Approved contract | Complete Product/Operations decision | Repeatable measurement of the current grid against its exact criteria | Missing criteria keep Story 5.1 `awaiting-operator` |
| Performance miss | Approved fallback trigger is met | Apply and verify the approved stricter page size or virtualization | Preserve order, cursor truth, support safety, and accessibility |

</frozen-after-approval>

## Open Questions

- Approved Product/Operations contract — provide the approved decision record covering all fields named above (enables measurement and any triggered fallback), or defer the decision (leaves the existing Story 5.1 spec `awaiting-operator` with no performance claim). The repository cannot choose these criteria on Product/Operations' behalf.

## Code Map

- `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md` §16.14 and `_bmad-output/planning-artifacts/epics.md` Story 5.1 -- decision ownership and acceptance boundary; do not substitute the unrelated warm-read assumption.
- `_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md` -- completed implementation, exact verification, and current operator action; update only after an approved contract and evidence exist.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` and `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor` -- current flat grid and 50-row UI page; preserve ordering and critical context.
- `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRequest.cs` and `src/Hexalith.Tenants/Queries/TenantQueryPaginationPolicy.cs` -- current UI and server page-size defaults; change only if the approved fallback requires it.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs` and `tests/Hexalith.Tenants.IntegrationTests/SnapshotPerformanceTests.cs` -- existing functional UI and unrelated snapshot performance coverage; neither proves the missing audit-render contract.

## Tasks & Acceptance

**Execution:**
- [ ] `_bmad-output/implementation-artifacts/story-5-1-performance-decision.md` -- record the approved Product/Operations contract and its authority before setting numeric targets.
- [ ] `_bmad-output/implementation-artifacts/story-5-1-performance-evidence.md` -- record the approved-tier procedure, representative dataset, exact measurements, and pass/fail result.
- [ ] `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` and related focused tests -- implement the approved fallback if the evidence meets its trigger, then rerun measurement and functional checks.
- [ ] `_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md` -- link approved decision and evidence, and update its terminal status only when the contract is satisfied.

**Acceptance Criteria:**
- Given an approved Product/Operations contract, when representative audit performance is measured, then the recorded repeatable results satisfy its initial-render and interaction percentile budgets or activate its specified fallback.
- Given a triggered fallback, when the audit grid is rerun, then the measured result meets the approved budget while ordering, cursor scope, support safety, and accessible responsive behavior still pass.
- Given no approved decision or failed acceptance evidence, when Story 5.1 status is evaluated, then it remains `awaiting-operator` and makes no numeric performance claim.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `dotnet build Hexalith.Tenants.slnx --configuration Release -m:1 --no-restore` -- expected: zero warnings and errors after any fallback code change.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -class Hexalith.Tenants.UI.Tests.Components.TenantAuditPageTests -parallelMode none` -- expected: all focused audit page cases pass after any fallback code change.
- Approved performance procedure, to be specified by the Product/Operations decision -- expected: repeatable measurements and explicit pass/fail evidence.
