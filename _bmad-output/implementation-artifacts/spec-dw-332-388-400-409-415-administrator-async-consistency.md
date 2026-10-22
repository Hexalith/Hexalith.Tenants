---
title: 'Keep administrator actions consistent across asynchronous transitions'
type: 'bugfix'
created: '2026-10-22'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'd287099eaba0018e59d2f4474aa36fb9b1a037b5'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Global Administrators page has five open records concerning overlapping render, authorization, delivery and projection activity: DW-332, DW-388, DW-400, DW-409 and DW-415. Obsolete work must not contradict visible action guidance, retain unauthorized subscriptions, lose accepted completion or release ownership still needed by an active attempt.

**Approach:** Qualify these exact interleavings using the existing component harness, then make the smallest page changes needed to keep displayed evidence and aggregate ownership consistent. Complete exactly these five records, preserving their original findings and recording reproducible validation. Commit and push the completed work on `main` as requested.

## Boundaries & Constraints

**Always:** Preserve authorization checks, exact attempt/message/lease identity, accepted/projection-confirmed/audit-available distinctions, last-administrator protection, localized recovery and submit-time revalidation. Retain published delivery evidence when a renderer generation expires. Cancellation ends obsolete reads without implying command cancellation or releasing unresolved delivery ownership. Use existing FrontComposer/Fluent controls and shared admission/subscription services.

**Never:** Change platform dependencies, public gateway contracts, persistence, deployment, unrelated deferred records or release holds. Do not claim the three unverified risks are reproduced defects unless controlled tests establish them. Do not weaken safety guards to satisfy tests.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Render transition | Admission or viewport evidence changes between availability reads | One rendered action uses coherent disabled state, reason, recovery and descriptions; next render adopts newer evidence | Submission checks current authority independently |
| Obsolete removal read | Held projection walk overlaps supersession or disposal | Its token cancels; obsolete results cannot update the page or successor lease | Cancellation preserves unresolved attempt evidence |
| Subscription adoption | Authorization changes between pre-adoption validation and lease assignment | Obsolete lease is disposed and cannot initiate reads; authorized recovery can subscribe | Existing bounded setup retry remains intact |
| Published grant completion | Completion reaches the lease before generation supersession and renderer synchronization | Same authorized owner adopts completion and resumes reconciliation once | Replacement owner and authorization loss reject stale visible writes |
| Advanced removal attempt | Held confirming read returns after same-lease snapshot advancement | UI and lease remain consistent; stale basis cannot release an unresolved current attempt | Genuine current terminal completion still releases correctly |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`: `GrantAvailability`, `RemoveAvailability`, reason/recovery/description readers; `ActionEvidence` captures live gate and viewport state. Preserve fresh evaluations on command admission.
- Same page: `EnsureReadRefreshLeaseOnRendererAsync` already captures authorization version and checks `IsReadRefreshScopeCurrent` before assignment; its post-assignment guard checks disposal only.
- Same page: `DispatchGrantAsync` publishes through `TryCompleteReconciliationDispatch`, then gates renderer synchronization by the previous mutation generation. Reuse owned reconciliation/adoption helpers and retain delivery evidence.
- Same page: `RequeryRemoveProjectionAsync` passes `CancellationToken.None`; existing `_removeMutationCancellation`, operation lifetime and `InvalidateRemoveMutation` provide cancellation seams. Its stale-result branch calls `RetainOrReleaseRemoveCompletion` using the captured basis.
- `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`: reuse queued projection responses, authorization-transition fixtures, `BlockRendererAsync`, private snapshot/lease assertions and superseded completion tests. Hold exact boundaries with signals, avoiding timing-only assertions.
- `_bmad-output/implementation-artifacts/deferred-work.md` and `docs/maintenance-backlog.md`: preserve history and reconcile exactly five closures after verification. Current baseline has 657 IDs, 515 done and 142 open across 132 index rows.

## Tasks & Acceptance

**Execution:**
- [x] `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs` — establish all five exact interleavings and normal/replacement controls; assert rendered state and ownership together.
- [x] `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor` — stabilize rendering, cancel obsolete removal reads, reject obsolete subscription adoption and reconcile completion against current ownership; fix only demonstrated gaps.
- [x] `_bmad-output/implementation-artifacts/dw-332-388-400-409-415-validation-evidence.md` — record exact commands, initial controls, final results, review and limitations.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` and `docs/maintenance-backlog.md` — close the selected five, retaining original fields and unrelated records; verify ID inventory, totals and exact index coverage against the baseline.

**Acceptance Criteria:**
- Given changing admission/viewport evidence, when an action renders, then its availability, guidance and descriptions agree (DW-332).
- Given a held removal projection read, when its operation is invalidated or disposed, then I/O cancels and current ownership remains correct (DW-388).
- Given authorization collapse during subscription adoption, when assignment completes, then no obsolete active lease or callback remains (DW-400).
- Given published grant completion and later generation supersession, when renderer processing resumes, then current authorized ownership adopts the completion without redispatch (DW-409).
- Given same-attempt advancement during a held removal read, when the old read completes, then an unresolved visible attempt cannot lose its lease to stale confirmation (DW-415).

## Implementation Notes

- 2026-10-22: Parent acceptance inspected the staged baseline diff, independently parsed the 294-case and both 7,277-case TRX reports, and confirmed all 13 matrix probes passed. Serialized full UI acceptance has zero failures/skips and the exact same named case multiset as the initial full run. Original-page replay establishes nine failures and four passing controls; source build has zero warnings/errors. Ledger preservation, dependency-pointer guard and whitespace checks pass. Runtime and intermittent browser limits remain in the evidence; independent workflow review follows.
- 2026-10-22: All three review layers completed. Eight test/diagnostic patches were applied and independently rechecked; two production-risk claims were refuted. Final parent source build has zero warnings/errors; Global Administrators 300/300 and complete serialized UI 7,283/7,283 pass with zero failures/skips, including all 19 expanded matrix probes. The initial unchanged one-second notification-wait failure and passing isolated/class rechecks remain preserved. Ledger/gitlink/whitespace checks and pinned commitlint pass; fetched remote main has no divergence. No production source changed during review.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| Blind 1: localization override pairs can change during render | false | The production `TenantsBffComposition` derives each pair from a readonly principal resolver and the same cached fixed-localization result; compiled resources and the instance localizer do not change during its lifetime. A dynamic test double does not establish the claimed production transition. |
| Blind 2: render transition coverage is one-directional | low | The new probe changes busy/unsafe evidence to available only. Add the inverse interleavings so future changes cannot regress coherent blocking guidance in deferred row templates; test-only patch. |
| Blind 3: publication controls use Accepted only | low | The widened synchronization branch receives terminal and ambiguous completion too; existing terminal/replacement tests cover outcomes but do not hold this exact publication boundary. Add explicit outcome controls to the existing barrier test; test-only patch. |
| Blind 4: resumed status identity lacks exact assertions | low | The new test checks one handle only. Existing notification tests assert the full tuple, but a future error in the new resumption route could evade its regression. Add message, correlation and fixed aggregate assertions; direct test patch. |
| Blind 5: authorization-loss control stops before recovery | low | The current barrier test verifies restricted UI and a locked aggregate, without exercising authorized readoption of its published completion. Assert retained exact evidence and recovery without redispatch; test-only patch. |
| Blind 6: original disposed page might resume status | false | `InvokeRendererSafelyAsync` checks `_disposed` before dispatch and inside the renderer callback, so the disposed original cannot set `appliedCompletion`; its subsequent status arm is unreachable. `DisposeAsync` also retains ownership for replacement and clears the original lease. |
| Blind 7: advanced removal regression stops before recovery | low | The test establishes preserved unresolved ownership but does not exercise a fresh status/projection refresh afterward. Extend it to prove the retained attempt remains recoverable and releases on new qualifying evidence; test-only patch. |
| Blind 8: held removal read lacks authorization-collapse case | low | `CollapseAuthorizationState` calls `InvalidateAllMutations`, but the new cancellation probes exercise explicit invalidation/disposal only. Add the actual authorization transition and assert hidden UI plus retained exact ownership; test-only patch. |
| Blind 9: held-test cleanup starts after entry await | low | An entry timeout can leave a queued task unresolved before the existing finally is entered. Move the held operation and entry awaits under cleanup that releases and observes it; direct fixture patch. |
| Blind 10: archived baseline replay leaves baseline build outputs | medium | Its finally restores source bytes only, so invoking the helper alone followed by `--no-build` can execute baseline binaries. Preserve the historical driver and repair the runnable replay to rebuild restored source in finally; diagnostic patch. |

The verification-gap layer reported no gaps; the edge-case layer reported no findings. All active layers completed before triage. No intent or specification changes are required; the eight surviving entries are test/diagnostic patches with no public or runtime surface change.

Parent patch recheck: all eight entries are resolved. Reverse guidance, terminal/ambiguous delivery, exact tracking identity, authorized readoption, fresh removal recovery and authorization-triggered cancellation pass in the 19-case matrix and both final parent runs. Held signals are released and tasks observed from finally; the corrected replay rebuilds restored source and passes three isolated controls, retaining the historical driver.

## Verification

- Before code edits, attempt the repository-required Aspire baseline and retain any exact environment blocker.
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false` — successful source build, warnings treated as errors.
- `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --filter-class '*GlobalAdministratorsPageTests' --minimum-expected-tests 1` — focused cases pass using the established Microsoft.Testing.Platform arguments.
- Same per-project test command without the class filter — full UI suite passes; preserve exact failures/blockers and follow the baseline fallback ladder when required.
- `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-332-388-400-409-415-administrator-async-consistency.md` and `git diff --check` — pass with dependency pointers preserved.
- Validate the exact commit-message file with the pinned local commitlint CLI, fetch and inspect divergence, then commit and push completed work to `origin/main`.
