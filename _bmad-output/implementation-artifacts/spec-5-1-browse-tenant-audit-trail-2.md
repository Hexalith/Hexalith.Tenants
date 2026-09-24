---
title: 'Close Story 5.1 Audit Performance Gate'
type: 'feature'
created: '2026-09-23'
status: 'in-review'
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
- [ ] `_bmad-output/implementation-artifacts/story-5-1-performance-evidence.md` -- record a complete audit-performance-v4 dedicated run with exact commands, environment, dataset hash, raw results, percentiles, and functional gates; retain the v3 run as historical evidence.
- [ ] `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` and `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs` -- evaluate conditional fallback from the corrected v4 50-row result; if a valid batch misses, change the UI to 25 rows, preserve paging and safety, and remeasure the full contract.
- [ ] `_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md` -- link passing v4 evidence and update its terminal status only when the contract is satisfied.

**Acceptance Criteria:**
- Given approved revision 1, when the authenticated 500-entry grid is measured, then every batch meets its initial-render and interaction budgets or activates fallback.
- Given fallback, when remeasured, then budgets and functional gates pass together.
- Given absent or failing evidence, when status is evaluated, then Story 5.1 remains `awaiting-operator` without a measured pass claim.

## Implementation Notes

The fresh 4 vCPU/8 GiB Linux VM needed Docker buildx, an explicit Release build of the Memories server excluded from the solution, and Linux development-certificate trust for the UI's HTTPS call to Tenants API. The runner now checks these prerequisites. Earlier setup and diagnostic attempts were discarded, then the v3 test completed from a fresh seed. Review found that it stopped result timing before paging became usable. Its 50-row result cannot decide fallback; the revised v4 script requires a new dedicated run.

## Spec Change Log

## Review Triage Log

| ID | Finding | Verdict | Route | Evidence and resolution |
| --- | --- | --- | --- | --- |
| VG1 | Result timing ends before paging completes | high | patch | `LoadAsync` publishes rows before supplementary reads finish, while `_pageLoadInFlight` still disables the pager. The v4 wait includes painted Ready rows and the expected usable pager. |
| VG2 | Dirty-filter Refresh lacks a behavioral assertion | medium | patch | `RefreshAsync` has a new dirty branch; removing it would reuse the prior cursor and validator. A page-two Refresh test now checks the staged category, null cursor, and null ETag. |
| VG3 | Pending-filter pager disabled state lacks an assertion | medium | patch | The handler guards alone would leave apparently active controls that do nothing. The staged-filter test now checks both disabled buttons and the post-Apply state. |
| VG4 | Notification can apply draft filters | medium | patch | `RefreshFromNotificationAsync` called `LoadAsync`, which reads edited fields before Apply. It now defers that refresh while `_filtersDirty`; a notification test waits for the callback and checks no query. |
| BH1 | `requestAnimationFrame` timestamp precedes paint | high | patch | An animation-frame callback runs before that frame paints. v4 records from the next frame after the matched rows and pager remain present. |
| BH2 | Row match ignores loading and pager completion | high | patch | A Ready row snapshot can precede completion of `NextPageAsync` or `PreviousPageAsync`. The action endpoint now requires the expected enabled pager as well as Ready rows. |
| BH3 | Fallback mode does not itself change UI page size | false | reject | The approved sequence calls for a code change to 25 rows *after* a valid miss. No valid v4 miss exists yet; `prepare-run` selects the rerun mode and intentionally fails if UI rows remain at 50. |
| BH4 | Projection notification applies pending filters | medium | patch | Same reachable notification path as VG4. The dirty-state guard and callback test cover it. |
| BH5 | Invalid draft hides the confirmed grid | false | reject | The prior Story 5.1 safety contract explicitly requires invalid local filters to hide stale success chrome and avoid a query; `TryCreateRequest` implements that behavior. |
| BH6 | Editing back to the prior value still shows pending | low | reject | A user can encounter the extra Apply step, but the request remains safe and Apply clears pending state. Canonical applied-filter comparison adds state and branches for a negligible inconvenience. |
| BH7 | Response count is not independent of displayed rows | false | reject | `_snapshot.Rows.Count` is the validated response's row count, which is the requested effective response count. The contract does not require a second independent transport counter; the browser separately checks exact references and rendered count. |
| BH8 | Browser semantic gate checks too little | medium | patch | A visible grid and field counts alone do not check named headers or live state. The gate now checks critical columnheader names, the polite Ready status, and named pager navigation. |
| BH9 | Browser run does not tamper with a protected cursor | false | reject | Caller, tenant, date-range, and category cursor isolation are exercised by `TenantQueryCursorCodecTests` and `TenantsProjectionActorTests`; the approved contract allows separate functional gates outside the timing browser script. |
| BH10 | Forced-color browser check does not inspect focus styling | false | reject | The performance change adds no color or focus styling. The original Story 5.1 browser evidence exercised keyboard focus under forced colors, and focused UI tests inspect forced-color rules; this timing tier keeps a media-mode smoke check. |
| BH11 | Browser checks only one French label | false | reject | EN/FR resource parity and localized state/action behavior are covered by the original Story 5.1 UI tests and browser evidence. This performance tier checks the active French grid, not the entire localization inventory. |
| BH12 | Safe-output regex is narrow | false | reject | The representative seed contains only synthetic approved values; hostile payload rejection is covered by the prior gateway, row, and receipt tests. The browser regex is an additional smoke check, not the sole safety gate. |
| BH13 | Generic setup failures leave no result marker | medium | patch | `set -e` exited on build or install failure without writing `setup-failure.txt`. An ERR trap now records a safe line and exit code while cleanup still stops the AppHost. |
| BH14 | Archived source patch has dirty submodule gitlinks | maybe-false | defer | The patch cannot show whether the guest's EventStore, FrontComposer, or Memories dirt was source or generated output. The next dedicated run must capture nested status/diffs or start from clean dependency trees to settle reproducibility. |
| BH15 | Decision document says no evidence exists | low | patch | Its body contradicted the new evidence link and status. The body now states that a run must measure every approved endpoint, and the status explains why v3 does not close acceptance. |
| EC1 | Pager can remain unusable after the timed row paint | high | patch | Same endpoint defect as VG1 and BH2. v4 waits for the expected usable pager after paint. |
| EC2 | Fallback mode expects 25 while UI requests 50 | false | reject | Same conditional sequence as BH3: a valid v4 miss first authorizes a UI code change and full rerun; the mode guard does not claim to edit production page size. |
| EC3 | Notification applies pending filters with an old cursor | medium | patch | Same reachable draft-filter path as VG4. Deferring the notification prevents that request until Apply clears paging. |
| EC4 | Tenant navigation retains pending state | medium | patch | The tenant-change branch cleared paging but left `_filtersDirty`, disabling the new tenant's pager. It now clears pending state; the rebinding test checks a usable new-tenant Next button. |
| EC5 | Keycloak password containing `=` is truncated | low | patch | `split("=")[1]` dropped the suffix of a possible configured password. `ltrimstr` now preserves the full value without exposing it. |
| EC6 | Unrelated refresh can satisfy action feedback | medium | patch | The old observer accepted any status or row change after a click, including a projection nudge. v4 accepts only the action's Loading state or its exact expected rows, after a painted frame. |

The four result-endpoint findings (VG1, BH1, BH2, EC1) share the incomplete paint and pager milestone. VG4, BH4, and EC3 share draft filters leaking through notification refresh. The remaining accepted findings have distinct causes. BH14 is deferred because the historical guest's nested working-tree contents are unavailable. No accepted finding requires changing the approved intent.

## Verification

**Historical v3 evidence, 2026-09-24:** The [evidence record](story-5-1-performance-evidence.md) links the command, dataset, environment, six raw 40-sample batches, and [v3 summary](story-5-1-performance-vm-authoritative-2026-09-24/summary.json). The Release Chromium command exited 0 after 33.6 minutes, but row-paint timestamps preceded the painted, usable pager required by the approved contract. These percentiles do not establish a pass or a no-fallback decision. The archived source diff also contains three unexplained dirty submodule gitlinks.

**Review corrections, 2026-09-24:** The v4 browser script waits for rows, Ready state, and the expected enabled pager through a later animation frame. Feedback is tied to the clicked action's loading or expected rows. The UI defers projection notifications while filters await Apply and clears pending state on tenant navigation. The runner preserves a safe setup-failure marker and parses Keycloak credentials containing `=`. Focused checks passed: `npm run typecheck --prefix tests/performance/tenant-audit`; `npm run test:mode --prefix tests/performance/tenant-audit` (4/4); `bash -n scripts/run-tenant-audit-performance.sh`; `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -m:1 --no-restore` (0 warnings/errors); and `tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests -class Hexalith.Tenants.UI.Tests.Components.TenantAuditPageTests -parallelMode none` (91/91).

**Earlier non-authoritative smoke, 2026-09-23:** The shared 24-CPU WSL2 smoke used one sample in one batch per viewport. Its raw samples and ten passing gates were wiring evidence only; it was never an acceptance result or fallback trigger.

**Fallback rerun guard:** The runner now accepts an explicit original-run directory only after validating a complete authoritative 3×40 baseline with a raw-sample percentile miss. It replays the exact original projection and manifest SHA-256, then expects 25 UI rows. The default remains 50 rows; `AUDIT_PERF_PAGE_SIZE=25` without the validated source is rejected. Focused mode tests pass 4/4 and the seed's replay verification reproduces the recorded 500-entry smoke hash. The shared-machine smoke cannot trigger this mode.

**Commands:**
- `dotnet build Hexalith.Tenants.slnx --configuration Release -m:1 --no-restore` -- expected: zero warnings and errors after any fallback code change.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -class Hexalith.Tenants.UI.Tests.Components.TenantAuditPageTests -parallelMode none` -- expected: all focused audit page cases pass after any fallback code change.
- `AUDIT_PERF_DEDICATED_RUNNER=1 AUDIT_PERF_RESULT_DIR="$HOME/tenants/_bmad-output/implementation-artifacts/story-5-1-performance-v4" scripts/run-tenant-audit-performance.sh` from the dedicated guest repository root -- pending: use a new empty result directory and record three complete 40-sample batches per viewport, with exact evidence and no self-skip.
