---
title: 'Resolve five deferred command recovery safety records'
type: 'bugfix'
created: '2026-10-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'e7d851763f4e3a13edb7f294b1c88e6992cddc84'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve exactly DW-248, DW-255, DW-269, DW-276 and DW-313 as the requested five-record maintenance batch. Membership status recovery must drain queued work iteratively, preserving manual projection intent, generation ownership, removal audit recovery and failure visibility. Reuse a tenant command refresh helper across the three membership flows.

Create submission and status observation must use the host's optional TimeProvider, with the system clock as fallback. Consume the original attempt timestamp in a five-minute pending-status wait bound. Exhaustion exposes recoverable uncertainty, preserving intent, message/correlation identity, original timing and unresolved aggregate admission; later authoritative status can recover. Preserve public compatibility and the existing rule that dispatched ownership does not expire.

Pin the currently observable audit-availability numeric values. Qualify invalid-identity/competing-surface refusal while an existing lease is held, without clearing its activity. Qualify the existing shared complete create-attempt store, legacy Forget isolation and stale delivery protection rather than duplicating lifecycle retention policies. Preserve original findings, unrelated ledger entries, dependency pointers and release holds. Verify affected behavior and the maintained UI suite, run independent review, then commit and push to main as requested.

</frozen-after-approval>

## Implementation Notes

- Investigation found iterative drains already in Add/Change, recursive handoff in Remove, and a shared complete create tracker already replacing the historical handle-only implementation. The five-minute bound follows existing lifecycle waiting policy and never releases unresolved command admission.
- No unresolved intent gaps, migrations or data operations. The bounded local change uses existing gateway, localization and testing seams; no dependencies or public services are added. The user's explicit five-item request defines the maintenance batch scope.
- Read repository baseline, Git/UX guidance, editor/build/test configuration, architecture AD-7/AD-12 and current validation evidence. The pre-change Aspire AppHost started successfully; security was Running/Unhealthy and dependent services Waiting. Stopped the task-started AppHost after inspection. No healthy distributed baseline is claimed.

- Implemented shared membership drain, clock-based create status recovery and explicit availability values; added ownership rationale and focused tests. Initial build failed on two test setup mistakes (init-only gateway property and the audit-state alias), then passed with zero warnings/errors after correction. The first focused MTP filter selected zero tests; direct xUnit repeated `-method` selectors execute the intended cases. An initial 23/24 pass exposed a stub resource omission; final shipped EN/FR localizer coverage brings the dedicated run to 25/25 passing, zero failures/skips.

- The first maintained run executed 7,307 cases with four existing create compatibility failures and zero skips. Preserved the one-argument snapshot API; the live flow opts into timing via the explicit observation overload. No old fixture/assertion changed. Added Received/Processing deadline cases; final suite count is 7,313.

- Independent review completed before triage. Applied its ten concrete corrections/coverage improvements: generation-bound callbacks, documented/tested shared failure policy, accurate outcome copy, bounded failure assertions, held disposal, terminal removal recovery, create remount/ambiguous retry/late rejection and public overload documentation. No additional deferred record is required. The pre-review corrected complete run passed 7,313/7,313 with zero failures/skips; the expanded final suite is revalidated after these patches.

- Final Debug source build has zero warnings/errors. Final focused 42/42 and complete UI 7,324/7,324 pass with zero failures/skips. TRX verifies all dedicated cases in the complete run. Ledger preservation, story gitlinks and whitespace checks pass; the pinned commitlint validates the exact message. Independent recheck has no remaining concrete issue. Raw logs/reports and source/assembly hashes are archived with the validation evidence. Exactly five records close: 657 IDs, 525 done, 132 open across 122 index rows.

## Review Triage Log

1. **medium / patch:** Generation can change after the drain's locked read and before the callback captures attempt state. The callback now receives the consumed generation; every membership core checks it before capture and retains it through continuations. A deterministic handoff probe rejects old projection intent while a new current request still runs.
2. **low / patch:** Drain-wide failure ownership was implicit. Documented and tested the existing policy: joined callers wait for all explicit queued work and observe its first failure, including successful replacement work after an older fault. No failure is silently swallowed and no automatic retry is introduced.
3. **low / patch:** Received/Processing can carry verified status without outcome proof. EN/FR expiry copy now describes the unresolved creation outcome, preserving that distinction.
4. **low / patch:** The new failure assertions had unbounded waits. Added existing five-second WaitAsync bounds so a stalled completion fails diagnostically.
5. **low / patch:** The new disposal assertion only exercised idle disposal. Added a held batch plus queued manual request; disposal suppresses queued callbacks and retires both waiters.
6. **low / patch:** Helper tests did not qualify removal's distinct terminal audit recovery after a parent callback fault. Added rendered Confirmed/Rejected cases proving the queued manual callback runs, both callers observe the failure, and identity/lifecycle remain without redispatch.
7. **low / patch:** The clock test lacked actual replacement rendering. Added EN/FR unmount, clock advancement and remount under a different supplied baseline; original deadline/intent/identity/baseline and admission remain.
8. **low / patch:** The clock test lacked ambiguous delivery retry. Added held submission, elapsed waiting, ambiguous response and same-message retry; pending status uses the original timestamp and dispatch count reflects only the explicit retry.
9. **low / patch:** Later EventsStored did not qualify terminal rejection release. Added EN/FR authoritative rejection recovery, exact original identity, no redispatch and preservation of another tenant's held admission.
10. **low / patch:** The compatibility/clock-aware overload contract was only in a private comment. Public XML documentation now identifies the legacy behavior and the explicit observation-time opt-in used by hosts.

- The first expanded focused run passed 34/42; all eight failures were the new create fixture querying a lease with the legacy owner predicate. Corrected the fixture to assert the exact acquired lease and its CurrentOwner. Final focused 42/42 passes with zero failures/skips; Debug source build has zero warnings/errors. Independent recheck accepts all ten patches, with no remaining concrete code issue. Final complete run expects 7,324 cases.
