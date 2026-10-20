---
title: 'DW-422: Bind audit read-only recovery to the rendered grid'
type: 'bugfix'
created: '2026-10-20'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '8224b5baf9c7d9ce51d53e90eea61d85291afa46'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-422 records a tenant audit recovery link targeting a grid heading omitted while filters are invalid. Later validation already discards the prior snapshot and invalidates pending reads, but the link still duplicates only part of the grid visibility condition and the old deferred record lacks explicit acceptance evidence.

**Approach:** Reuse the grid visibility condition for Continue read-only while preserving its existing stale, degraded and list-refreshed state restriction. Qualify actual input validation, recovery and target presence with the existing component fixtures. Close only DW-422 after focused verification and independent review; retain its historical fields, unrelated deferred work, dependency pointers, sprint and release holds. Commit and push the completed change as requested.

</frozen-after-approval>

## Implementation Notes

- This is freeform deferred maintenance; no sprint story key. No unresolved intent gaps or irreversible implementation steps; the user authorized the final push. The footprint is the audit Razor predicate, existing component tests, this spec, durable evidence, and the selected ledger/index record. The step-02 oneshot route applies.
- Code map: `TenantAuditPage.razor::ShouldRenderRows` owns heading/grid visibility; `CanContinueReadOnlyForState` repeats only row count and three kinds. `TryCreateRequest` already clears invalid snapshots, increments load generation and cancels older reads. Reuse `SnapshotFor`, `RegisterServices`, the existing input selectors and `FluentSelectInterop` in `TenantAuditPageTests.cs`.
- Investigation found commit `34c485e81bc5b00e39cb2920195ce319efe465e1` already contains invalid-filter clearing and the late-completion regression. Public-input acceptance cases are expected to pass entry production; do not claim they reproduce a current dangling link. The predicate change explicitly binds both render paths. DW-444 focus restoration remains separately scoped.
- Read repository baseline, Git/UX guidance, editor/attribute/build/test configuration, contributor guidance, architecture constraints and audit UX specification. Clean entry on `main`; the canonical revision is above. Preserve FrontComposer/Fluent components and the existing recovery vocabulary.
- Entry-production qualification: the new twelve-case input/reset theory passes 12/12 with zero failures/skips after a zero-warning/error Debug source build. This confirms the historical invalid-filter protection already works. The only production edit replaces the duplicated row-count condition with `ShouldRenderRows`; state restrictions remain explicit.
- Aspire baseline: isolated start using existing assets succeeded; inspection reported nine Running, thirteen Waiting and security Starting. `aspire wait tenants-ui --timeout 35` exited 17; bounded error logs contain Memories initialization Forbidden errors. The instance stopped successfully before rebuilding. No healthy runtime or browser acceptance is claimed.
- Final Debug source build succeeds with zero warnings/errors; focused audit page 224/224 and maintained UI 6278/6278 pass with zero failures/skips. The first focused command used an excessive minimum of 300; all 224 cases passed but the runner exited 9. The corrected exact-count command passes.
- Whole-file whitespace verification reports 22 inherited diagnostics on unchanged baseline lines, none in the new test. Scoped `git diff --check` passes. Dedicated evidence preserves exact commands and limits.
- Executed integrity/report checks confirm only DW-422 closes, all original fields remain, 656 IDs / 388 done / 268 open / 240 index rows, exact six-file scope, unchanged dependencies/archive/sprint, and distinct all-passing TRX results including all twelve new cases.
- The exact full commit message passes pinned `@commitlint/cli@21.2.3` with zero problems/warnings. Remote fetch shows no divergence from entry `main` before the commit.
- Independent Blind Hunter review found five improvements. Added three empty-result cases and three held late-completion cases for eligible recovery kinds. The reset matrix now returns and asserts a distinct event reference. Wording now distinguishes synthetic component events from ordinary browser interactions. These are six additional cases within the existing files; no production scope changed.

## Review Triage Log

Blind Hunter finding floor: 16.908 kB; min(floor(sqrt(16.908) + 1), 10) = five findings. Only the configured Blind Hunter layer ran; no layers were skipped.

| Finding | Verdict and disposition | Evidence |
| --- | --- | --- |
| Recovery snapshots always contained one row. | Low; patch boundary coverage. | Three component cases now supply empty Stale/Degraded/ListRefreshed results and require no anchor, heading or grid. |
| Reset checks started without applied filters or cursor history. | Low; reject expansion. | The new cases qualify destination recovery and the shape of its unfiltered request, not disposal of pre-existing paging history. Existing cursor/Previous/history cases remain. A new filtered second-page scenario would expand this selected acceptance boundary without qualifying the changed render predicate. Evidence no longer implies that this matrix proves clearing prior cursor history. |
| Identical reset responses did not prove new data was displayed. | Low; patch fixture and assertion. | Reset returns `event-after-reset`, and every matrix case requires that reference in the restored row. |
| Existing late-completion reference covered only Ready. | Medium; patch relevant asynchronous coverage. | Three new held-completion cases return Stale/Degraded/ListRefreshed after invalid input and require the invalid alert with no anchor, heading or grid. |
| Malformed date/unknown category events were described as real input. | Low; patch evidence wording. | The initial matrix has nine synthetic malformed-date/forged-category component-event cases and three reversed-range cases. Browser input reachability is not claimed. |
- Final review validation: zero-warning/error Debug source build, 230/230 focused and 6284/6284 maintained UI cases pass with zero failures/skips. All eighteen added cases are present exactly once in both final TRX reports. Initial twelve-case entry evidence remains historical; the strengthened reset assertions and six additional boundary/late-response cases were qualified after review.
- Independent correction recheck confirms all four accepted improvements resolved, the fifth reasonably rejected with explicit limits, and no remaining concrete issue. No finding was deferred. Final ledger/index/report integrity and own-story gitlink/whitespace checks pass; only DW-422 closes.
- A late whole-tree integrity check correctly refused five concurrent Contracts description edits. They are external to DW-422, retained unstaged and fingerprinted in the evidence checker. The final ownership check requires exactly six task files after explicitly accounting for those five byte-preserved external paths. No user changes are staged, committed or reverted by this task.
