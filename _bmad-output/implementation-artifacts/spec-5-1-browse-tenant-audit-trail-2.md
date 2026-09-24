---
title: 'Close Story 5.1 Audit Performance Gate'
type: 'feature'
created: '2026-09-23'
status: 'in-progress'
route: 'dispatch'
baseline_commit: '2729deefdde44dd89ded1e410abb9ef767b0fb9d'
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
- [x] `tests/performance/tenant-audit/seed.cs` -- generate the approved deterministic 500-entry projection through the EventStore read-model seam in an isolated test topology and verify the persisted end-state before timing.
- [x] `tests/performance/tenant-audit/audit-performance.spec.ts` and `tests/performance/tenant-audit/package.json` -- pin authenticated Chromium measurement of approved actions/viewports; preserve raw samples and nearest-rank percentiles; fail on self-skip or wrong rows.
- [x] `scripts/run-tenant-audit-performance.sh` -- start Release Aspire at the registered callback, run seed and browser tiers, capture environment metadata, and stop resources.
- [x] `_bmad-output/implementation-artifacts/story-5-1-performance-evidence.md` -- record exact commands, environment, dataset hash, raw results, percentiles, and functional gates.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` and `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs` -- conditional fallback evaluated: the complete authoritative 50-row baseline passed every budget, so no miss authorized a 25-row change or remeasurement. The valid-miss guard and unchanged-manifest fallback mode passed focused tests.
- [x] `_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md` -- link approved decision and evidence, and update its terminal status only when the contract is satisfied.

**Acceptance Criteria:**
- Given approved revision 1, when the authenticated 500-entry grid is measured, then every batch meets its initial-render and interaction budgets or activates fallback.
- Given fallback, when remeasured, then budgets and functional gates pass together.
- Given absent or failing evidence, when status is evaluated, then Story 5.1 remains `awaiting-operator` without a measured pass claim.

## Implementation Notes

The fresh 4 vCPU/8 GiB Linux VM needed Docker buildx, an explicit Release build of the Memories server excluded from the solution, and Linux development-certificate trust for the UI's HTTPS call to Tenants API. The runner now checks these prerequisites. Earlier setup and diagnostic attempts were discarded, then the entire approved contract was rerun from a fresh seed. The 50-row baseline passed; no fallback UI change was made.

## Spec Change Log

## Review Triage Log

## Verification

**Authoritative evidence, 2026-09-24:** The [evidence record](story-5-1-performance-evidence.md) links the exact command, source diff and hash, 500-entry dataset manifest and hash, 4 vCPU/8 GiB VM environment, container image IDs, six raw 40-sample batches, and [final summary](story-5-1-performance-vm-authoritative-2026-09-24/summary.json). The Release full-stack Chromium command exited 0 after 33.6 minutes: all 5,040 observations, 126 percentile groups, and ten functional gates passed with no setup or sample failure. The largest p95 values were 1,245.4 ms for initial render, 389.0 ms for result completion, and 85.7 ms for feedback, below the respective 4,000/3,000/500 ms budgets. Independent raw-sample recomputation passed. The 25-row fallback condition was false.

**Earlier non-authoritative smoke, 2026-09-23:** The shared 24-CPU WSL2 smoke used one sample in one batch per viewport. Its raw samples and ten passing gates were wiring evidence only; it was never an acceptance result or fallback trigger.

**Fallback rerun guard:** The runner now accepts an explicit original-run directory only after validating a complete authoritative 3×40 baseline with a raw-sample percentile miss. It replays the exact original projection and manifest SHA-256, then expects 25 UI rows. The default remains 50 rows; `AUDIT_PERF_PAGE_SIZE=25` without the validated source is rejected. Focused mode tests pass 4/4 and the seed's replay verification reproduces the recorded 500-entry smoke hash. The shared-machine smoke cannot trigger this mode.

**Commands:**
- `dotnet build Hexalith.Tenants.slnx --configuration Release -m:1 --no-restore` -- expected: zero warnings and errors after any fallback code change.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -class Hexalith.Tenants.UI.Tests.Components.TenantAuditPageTests -parallelMode none` -- expected: all focused audit page cases pass after any fallback code change.
- `AUDIT_PERF_DEDICATED_RUNNER=1 AUDIT_PERF_RESULT_DIR="$HOME/tenants/_bmad-output/implementation-artifacts/story-5-1-performance-vm-authoritative-2026-09-24" scripts/run-tenant-audit-performance.sh` from the guest repository root -- passed: three complete 40-sample batches per viewport, with exact evidence and no self-skip.
