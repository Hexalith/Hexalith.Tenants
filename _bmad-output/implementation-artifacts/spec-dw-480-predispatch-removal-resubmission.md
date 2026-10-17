---
title: 'DW-480 allow removal resubmission after pre-dispatch lease refusal'
type: 'bugfix'
created: '2026-10-16'
status: 'done'
route: 'oneshot'
baseline_commit: 'e91aef5d9a6ffee045f40957594010db61b36e5d'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-480 records that a first member-removal submission refused by the activity lease enters UnableToVerify without dispatching anything. The next submission reports tracking loss, forcing the operator to close and reopen the flow.

**Approach:** Keep a first pre-dispatch lease refusal in a resubmittable preview state with the existing busy explanation, intact intent and preview, null command identifiers and no audit attempt. Once admission is available, recheck the existing safety gates and dispatch exactly once. Preserve same-identity retries for potentially delivered commands and fail-closed handling of genuine tracking loss. Reconcile only DW-480 after regression verification and independent review.

</frozen-after-approval>

## Implementation Notes

- Entry is clean `main` at the canonical baseline above. This is one deferred maintenance goal, not an epic story. No intent gaps, irreversible operations, external configuration triggers or new APIs. The footprint is one component, its existing tests, this spec, validation evidence and two backlog documents. No Git/dependency mutation is authorized.
- Investigation: `Components/Tenants/Members/RemoveTenantMemberFlow.razor`, `SubmitAsync` first `!admitted` branch sets UnableToVerify/null IDs; the earlier UnableToVerify/Degraded guard then reports `Tenants.Members.Submit.TrackingLost`. Keep that guard and `RefuseRetry` intact. Change only the undispatched first refusal to Previewed, preserving existing safe message, evidence, audit/focus/live-region fields. `TenantRemoveMemberCommandSnapshot.Blocked` also uses UnableToVerify and cannot solve this defect.
- `State/TenantCommands/TenantCommandAttemptTracker.cs`, `Detach`, already abandons/removes undispatched attempts; preserve its behavior and aggregate admission. Do not change shared tracker policy or allow untracked dispatched commands to start a new attempt.
- Test map: `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs`, extend existing first/refused-retry theory and add actual Fluent regression cases for repeated first refusal then admission, intact preview/intent, no pre-admission gateway/audit/status calls, current safety gate refusal, and duplicate submission during held delivery. Reuse the local gateway and existing service registration. Keep the existing same-ID refused-retry case as a control.
- Validation: reproduce a failing second-submit regression before the runtime fix; build the UI test project in Debug with `UseHexalithProjectReferences=true`, warnings as errors and serialized MSBuild; run focused removal tests and full UI project through maintained MTP. Record exact commands, results and limits in `dw-480-validation-evidence.md`. Required pre-edit isolated Aspire inspection is running; other hosts remain untouched. Snapshot `/tmp/dw-480-entry-state.json` preserves 643 ledger records, tracked hashes, full HEAD and root gitlinks.
- After review, update only DW-480 and its index row, preserve historical fields and all unrelated record bodies, archive/sprint, dependency pins and release holds. Validate exact remaining-index coverage/counts, story-gitlinks and whitespace. No broad backend/package/browser rerun is planned for this component state correction unless evidence requires it.

- Pre-edit Aspire inspection reached all 23 resources Running/Healthy; isolated Tenants host stopped. Five new component cases fail before the runtime correction (83 passes/five failures), including zero second-submit dispatch under real foreign-lease contention. The one-branch correction returns an undispatched first refusal to Previewed and leaves RefuseRetry/tracking-loss guards intact. The existing refusal theory now expects this accurate pre-dispatch state. Debug source revalidation is underway; exact evidence is in the validation document.

- Pre-review full UI passes 5,993/5,993, zero failures/skips. Independent review returned five findings. The existing live-region content now announces the localized busy explanation while the visible lifecycle still reports the undispatched preview. Added EN/FR shipped-resource checks, fresh-provider denial/indeterminate checks, and held-reacquisition freshness/confirmation interleavings. The foreign-lease case now returns Accepted, retains its actual identity through status/projection confirmation, and proves final lease release. No structure, CSS, API or tracker policy changes were needed.
- Review also verified an older independent gap: submitting while stale uses Blocked/UnableToVerify with no identity, and restoring Current still encounters TrackingLost. This is outside the changed lease-refusal branch and remains deferred as DW-644 with an exact reproduction path; it is not silently included in DW-480 closure. Final ledger/index counts must include this new open record.

- Final reviewed verification passes: Debug source build,94/94 focused removal cases and5,999/5,999 full UI cases, zero failures/skips. The single existing NU1903 advisory is emitted twice; no dependency/audit-policy change ran. The initial reviewed fixture build and two EN/FR service-registration failures are retained with their fixes in the evidence. The independent reviewer rechecked all five findings and confirmed the four patches plus the separate DW-644 deferral, with no remaining implementation correction.
- Final reconciliation closes only DW-480 and adds open DW-644. Integrity passes with644 IDs,342 done/302 open and270 exact-covering maintenance rows; every other original record body, historical fields, archive/sprint, tracked files outside the four planned existing paths, HEAD and root gitlinks are unchanged. Story-gitlink and whitespace checks pass; source/test CRLF is retained. No staging, commit, push, branch or dependency mutation ran, following the user-provided repository instructions.

## Review Triage Log

- `medium / patch`: Previewed replaced uncertainty copy but left the busy reason outside the live region. The existing live region now selects the existing whole localized safe message for a preview with refusal feedback; EN/FR actual-resource tests verify meaningful content and assertive politeness. Other lifecycle announcements retain their original state text. This checks rendered semantics, not repeated screen-reader speech timing.
- `medium / defer (DW-644)`: An existing non-lease pre-dispatch stale-evidence refusal creates UnableToVerify/null IDs and remains stuck after freshness recovers. Verified SubmitAsync's Blocked branch and tracking-loss guard; the change does not create either branch. Record a separate recoverable-pre-dispatch task, preserving genuine dispatched tracking-loss safety. No fix is claimed here.
- `medium / patch`: Initial safety tests only changed parameters before reacquisition. Two held-callback cases now change freshness or confirmation during reacquisition, then grant it and require zero delivery/status calls plus no retained ownership/aggregate lock.
- `medium / patch`: Cached IsAuthorized gating did not prove a new authority read. New provider-denial and indeterminate cases leave rendered parameters authorized, require a fresh provider call, and prove no further lease acquisition or gateway delivery.
- `low / patch`: The accepted lifecycle after lease recovery lacked direct combined coverage. The real foreign-lease regression now returns Accepted with the actual admitted ID, checks ProjectionPending and duplicate suppression, applies qualified committed-sequence projection evidence, and requires Confirmed with released aggregate/attempt ownership.
