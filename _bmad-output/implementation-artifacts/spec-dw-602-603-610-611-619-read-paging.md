---
title: 'Bound and clarify tenant read paging'
type: 'bugfix'
created: '2026-10-22'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '165bd76c908003fa92008632360ff8335cfd997a'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five open deferred records identify unbounded tenant summary enrichment (DW-602), stalled refresh subscribers blocking notifications (DW-603), repeated membership Next events corrupting paging history (DW-610), and sorting labels that imply ordering across membership/search pages (DW-611/DW-619).

**Approach:** Reject tenant-list responses exceeding the requested normalized page size before hydration; isolate overdue refresh callbacks using the existing shared deadline without overlapping a still-running registration; gate membership page transitions; disclose current-page sorting in the actual English/French sort controls and nearby explanatory copy. Both sorting records explicitly permit this scoped presentation instead of a global-ordering contract redesign. Complete all five items and push reviewed changes on main, as requested.

## Boundaries & Constraints

**Always:** Preserve caller/tenant authorization, opaque cursor scope, authoritative rereads, conditional 304 behavior, retained last-confirmed evidence, and localized support-safe UI. Notification alone never proves command success. Preserve callback lease/disposal ownership and observe late failures. Preserve unrelated ledger records and historical selected-record fields.

**Never:** Change server ordering contracts, add unbounded page aggregation, alter technical dependencies/gitlinks, edit skills or shared instruction files, or publish a release.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Oversized tenant list | More summaries than requested normalized page size | Invalid payload; zero detail enrichment | Existing unavailable/degraded retention policy |
| Valid tenant list | Exact page size or smaller; conditional 304 | Existing payload/validator semantics | No false rejection |
| Stalled subscriber | One callback remains pending beyond shared deadline | Healthy siblings and later nudges progress; stalled registration never overlaps itself | Safe fixed diagnostic; late faults observed |
| Detached subscriber | Callback captured then lease/circuit removed | No detached callback starts or restarts | Bounded disposal remains |
| Duplicate Next | Next invoked twice while held page read is pending | Exactly one cursor advance/read/history entry in each membership panel | No-op repeated event |
| Exhausted paging | Missing HasMore/cursor or loading/error state | Next cannot advance | No-op handler and matching disabled control |
| Scoped sorting | Two lookup/search pages with globally interleaved names/statuses | Each page retains existing local order; actual EN/FR controls explain page scope | No implied global ordering |

</frozen-after-approval>

## Code Map

- `../../src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs` — `SendAsync` and payload shape validation; pass requested size from `ListTenantsAsync`. Keep its CRLF checkout policy.
- `../../src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` — `ListByCursorCoreAsync` must bound enrichment even with a custom typed query client; reuse normalization/retention semantics.
- `../../src/Hexalith.Tenants.UI/Services/TenantReadRefreshSubscription.cs` — `RunRefreshLoopAsync` serially awaits callbacks. Reuse `TenantUiOperationPolicy`, preserve lock-protected invocation/detachment, and quarantine each still-running callback until actual completion.
- `../../src/Hexalith.Tenants.UI/Components/Users/{MyTenantsPanel,UserMembershipLookupPanel}.razor` — cursor history and Next handlers; loading and arrival validation must precede cursor mutation.
- `../../src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`, list grid and `Resources/TenantsResources*.resx` — actual search sort controls and scoped helper copy; user lookup has its own sort selector.
- `../../tests/Hexalith.Tenants.UI.Tests/Services/` and `Components/` — reuse transport/gateway fixtures, held-read component tests and existing `ManualTimeProvider`; add focused regressions, avoiding wall-clock timeout sleeps.
- `deferred-work.md` and its maintenance index — update only these five closures after passing verification, retain history, record exact evidence separately.

## Tasks & Acceptance

**Execution:**
- [x] REST client and query gateway — enforce normalized summary count before hydration, with valid-boundary and retained/304 controls.
- [x] Refresh subscription — bound stalled callback waits, prevent same-registration overlap, preserve detachment, and test repeated nudges plus late success/failure.
- [x] Both membership panels — gate arrived-page Next transitions and test held duplicate events plus Previous recovery/exhaustion.
- [x] Lookup/workspace/grid resources and tests — localize visible current-page sorting and qualify two-page behavior in EN/FR.
- [x] Ledger and validation evidence — close exactly these five IDs while preserving every unrelated record, archive, sprint, dependency pointers and release holds.

**Acceptance Criteria:**
- Given malformed oversized summaries, when listing tenants, then no detail fanout occurs.
- Given a hung subscriber, when the shared deadline and later notifications occur, then healthy subscribers continue and no overlapping invocation of the hung registration occurs.
- Given a held membership page read, when a captured Next event repeats, then Previous returns directly to the original page.
- Given scoped membership/search sorting, when either supported language renders it, then the sorting control and explanation explicitly describe current-page ordering.

## Implementation Notes

- Initial worktree is clean on main at the full baseline revision above. User authorized implementation and main push.
- Repository guidance and UX baseline read. Aspire start succeeded; initial describe showed dependent services waiting for security/eventstore. Runtime health is not yet qualified.
- Follow repository-local build settings; use Debug project references for development and the narrow UI Microsoft.Testing.Platform lane. Do not change dependencies to bypass a failure.
- Implementation handoff covers code/tests only. Parent will finalize ledger/evidence, review, commit and push. Do not commit, stage, push, or modify the ledger/spec during the implementation handoff. Return exact commands/results and residual concerns.

## Verification

- Build UI test project in Debug/source mode, then run focused changed classes through maintained `dotnet test`; run the full UI suite once focused checks pass.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-602-603-610-611-619-read-paging.md` — no undeclared gitlink moves.
- Validate full candidate message with pinned local commitlint before committing; fetch and inspect divergence before pushing main.

- Implementation produced 39 new cases; all six focused classes passed 1,065/1,065. Parent corrected the old lookup localizer fixture/announcement expectations after the first complete run and clears detached callback bookkeeping. The first full run completed 7,256 cases with four failures: fixture parity, two existing subprocess bounds and one existing notification-render timing check. Those initial failures were subsequently resolved and qualified by final acceptance below.
- Matrix audit: REST/gateway boundary and 304 tests cover rows 1–2; manual-clock subscription cases cover rows 3–4; held/exhausted/error member panel cases cover rows 5–6; EN/FR rendered and gateway interleaved-page cases cover row 7. All covering focused cases ran without skips.

## Review Triage Log

| Finding | Verdict / action | Evidence |
| --- | --- | --- |
| Blind 1: obsolete transition ownership | medium / patch | Both flags remain true after `_loadVersion` supersession. Replace with current load/snapshot ownership and qualify replacement paging while the obsolete read stays held. |
| Blind 2: synchronous callback blocking | low / contract clarification | The policy bounds returned Task work; synchronous invocation remains serialized with detachment. All owned subscriptions dispatch through `InvokeAsync` and return Tasks without synchronous blocking I/O. Explicitly document that existing callback requirement; the stalled returned-task regression remains the qualified DW-603 case. |
| Blind 3: sorting resets page | medium / patch | Existing sort handlers clear cursor/history. The changed current-page presentation needs EN/FR reset disclosure. |
| Blind 4: catch-up refreshes healthy siblings | low / rejected | A late registration rearms its scope and can cause a redundant authoritative read for healthy siblings. This is an allowed optional refresh nudge, never command dispatch or confirmation; multiple simultaneous overdue callbacks are unusual. Per-registration queues add state/branches for negligible normal-use benefit. |
| Blind 5: real sort control interaction absent | low / patch | New real-resource tests initially selected sort only through parameters. Exercise actual controls on page two with reset assertions. |
| Blind 6: name/status fixture correlation | low / patch | The new fixtures produced the same order for name and status. Use opposing orders so the status cases detect an always-name implementation. |
| Blind 7: detached completion barrier | low / patch | The negative assertion could precede the asynchronous late continuation. Make retirement deterministically observable before asserting no restart. |
| Blind 8: coordinator disposal case missing | low / patch | Existing lease and lifecycle coverage does not directly qualify the new pending catch-up bookkeeping through coordinator disposal. Add late success/fault controls. |
| Blind 9: summary count predicate order | low / patch | Count rejection followed item validation. Move the cheap null-safe bound before item traversal. |
| Blind 10: provisional evidence | low / finalization | Review inspected explicitly provisional evidence while final checks were pending. Replace pending statements with exact final outcomes before commit. |
| Edge 1: synchronous callback blocking | low / contract clarification | Same documented returned-Task boundary and owned nonblocking dispatcher callbacks as Blind 2. |
| Edge 2: obsolete lookup transition | medium / patch | Same reproduced superseding-load ownership root cause as Blind 1. |
| Edge 3: obsolete My Tenants transition | medium / patch | Same reproduced superseding-load ownership root cause as Blind 1, in the second panel. |
| Verification 1: obsolete transition ownership | medium / patch | Existing tests released the old read before recovery assertions; new held supersession cases must page replacement results before releasing it. |

The edge-case reviewer rechecked the patches: newer `_loadVersion` values bypass obsolete transition ownership, and each `finally` clears only its own stored version. Replacement paging and obsolete completion during a newer held read are explicitly exercised. The prompt-return callback contract and returned-Task deadline boundary now agree with owned dispatcher callbacks and evidence limits. No concrete prior finding remains; recheck made no edits and ran no tests.

Post-review broad verification qualified all 7,264 cases once without test failures, but the agent's inferred minimum 7,266 made the runner exit 9. Multiset TRX reconciliation proves exactly eight added cases and none removed. The next run with minimum 7,264 exposed an immediate timer-retirement assertion after visible notification state. `GlobalAdministratorsPageTests.FiniteBounds.cs` now waits through the existing assertion helper for the identical zero-timer requirement, matching the asynchronous callback boundary. No cancellation, authority, ownership or timeout assertion was weakened; the test-only rebuild has zero warnings/errors.

| Verification follow-up | Verdict / action | Evidence |
| --- | --- | --- |
| Notification timer retirement | low / patch, rechecked | The outer bounded callback can retire its timer after renderer-visible state changes. Waiting through the existing assertion helper preserves the exact zero-timer requirement; the isolated case passes, and the reviewer confirms no cancellation, authority, ownership, count or timeout check was weakened. |

## Final Acceptance

The final maintained complete UI run passes 7,264/7,264, zero failures/errors/skips/not-executed, with minimum count 7,264 and exit 0. This includes 47 new cases against the 7,217-case entry baseline. The review set passes 1,079 cases and the timer repair passes its isolated case. All review patches are complete; no new work was deferred. Ledger preservation, story gitlink and whitespace checks pass. The exact candidate message passed lockfile-pinned commitlint 21.2.3 with zero problems/warnings. See the companion validation evidence and raw-log archive for commands, initial failures, accepted source hashes and results.
