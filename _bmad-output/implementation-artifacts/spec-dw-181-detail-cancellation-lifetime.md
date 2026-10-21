---
title: 'DW-181: Qualify tenant-detail cancellation lifetime'
type: 'chore'
created: '2026-10-21'
status: 'in-progress'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '8ca1db8d5662d94272811951803974b8ed1c3e0d'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-181 records a missing runtime regression for tenant-detail load cancellation. The page already assigns cancellation-source disposal to each completed load, but no focused test establishes that a superseded or disposed page keeps an outstanding read's token usable until that read finishes.

**Approach:** Drive real rendered detail loads through the existing refresh/parameter callbacks and page disposal with explicit completion gates. Prove cancellation occurs immediately, token wait-handle and registration access remain usable while held, disposal occurs after completion, and obsolete results cannot replace the current detail. Preserve existing runtime behavior, dependencies and unrelated deferred work. Close only DW-181 after focused/full UI checks and independent review; commit and push on main as requested.

</frozen-after-approval>

## Implementation Notes

- Clean main entry and full canonical baseline are recorded above; this freeform maintenance has no sprint story key. No unresolved user-visible choice, public API, migration or unrequested external action is needed. Small test/evidence change uses the oneshot route.
- Read root AGENTS, permitted LLM/Git/UX baseline, editor/attributes/build/test settings, architecture AD-7/8/11, ledger and maintenance index. Independent investigation confirms a coverage gap without an evident production defect. DW-180 stays uncertain and open.
- Code map: TenantDetailPage.razor BeginLoad / ReplaceActiveCancellation / RetireCancellationCore cancel superseded reads; PendingOperation.Dispose invokes CompleteCancellation after completion. DisposeAsync cancels active reads without owning their disposal. TenantDetailSurfaceTests.cs supplies RegisterServices, Detail, ReadyWithSafeConfiguration and real EditTenantMetadataFlow refresh callbacks; the fixture is already partial. TenantsWorkspaceTests.cs contains the equivalent held-read test.
- Given a held read, when replaced or the page is disposed, then cancellation is observed before release, wait-handle and registration access remain valid until completion, and wait-handle access throws ObjectDisposedException after the owning callback completes. Given a replacement completes first, when the held obsolete read completes, then its result does not overwrite the replacement.
- Baseline Debug source build passes with zero warnings/errors. Start isolated Aspire baseline and inspect resource state/telemetry before test edits; retain exact baseline outcome in validation evidence and stop the started app afterwards.
