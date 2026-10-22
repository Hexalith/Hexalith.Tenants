---
title: 'Resolve five deferred tracked-command contract records'
type: 'bugfix'
created: '2026-10-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve DW-220, DW-254, DW-319, DW-327 and DW-469 as one bounded tracked-command contract change. Configuration proof overloads must require explicit implementations. Reconcile the legacy optional-identity decision with the existing tracked API split by consistently deprecating the six identity-minting wrappers in favor of their required-identity tracked replacements, preserving callable signatures and safe compatibility messages. A failed submission that reused an unresolved identity must retain an unverifiable audit outcome; an initial failure before dispatch still means audit has not started.

Preserve authorization, exact caller-owned message identity, projection confirmation rules, current supported dispatch capabilities and unrelated deferred records. Given a retained configuration or lifecycle identity, when another dispatch fails before sending, then audit remains unavailable rather than disappearing; given a fresh identity, the same failure remains not started. Verify behavior and independent review, record evidence, close exactly these five records, then commit and push main as requested.

</frozen-after-approval>

## Implementation Notes

- Investigation: both production query gateways already implement all four configuration proof overloads; four manual test gateways need explicit unavailable implementations. No backend or dependency changes are required.
- Compatibility: DW-254's current next-action explicitly permits migration/deprecation of the tracked/untracked split. Retain each old signature, mark all six wrappers obsolete with the exact tracked replacement, and pin callable behavior in focused tests.
- DW-469: configuration trackers can return an internal resumed-identity flag atomically with BeginDispatch; lifecycle already retains the retry identity at redispatch. Use the existing retry-aware TenantCommandAuditStates helper.
- Baseline: clean main at repository HEAD; existing Aspire resource inspection reports all resources Running/Healthy. This is a reversible local change with no intent gaps; publication is explicitly authorized by the user.

- Implemented explicit configuration proof contracts and manual adapter implementations; deprecated all six legacy wrappers on interface/concrete gateways without changing callable signatures. Only legacy removal gets a safe text fallback; tracked outputs are unchanged.
- Configuration BeginDispatch now atomically reports Resumed. Rendered tests name their dispatch-only registration scope explicitly; no ambiguous-configuration resubmission behavior was added.
- Independent review exposed lifecycle retry failure terminalization. A retry failing before dispatch now keeps RequestSent, its original bounded ownership and identity. Both enable/disable are qualified through actual parent disposal/remount, with initial failures remaining terminal and audit NotStarted.
- Added registration reset checks after Forget/expiry, legacy lifecycle acceptance payload/correlation checks and exact caller-cancellation propagation. All six review findings are patched; independent recheck reports no remaining concrete findings.

## Review Triage Log

- Medium / patch: retry failure without MessageId terminalized the possibly delivered lifecycle attempt, allowing tracker Forget and lease release. Retain RequestSent through AmbiguousSubmission; rendered enable/disable tests assert tracker identity, active ownership and actual parent remount.
- Low / patch: configuration test names could imply prior delivery. Rename to registration resumption and add explicit dispatch-only/no-UI-redelivery comments.
- Low / patch: disable-only retry coverage missed the enable gateway branch. Add both operations with fresh and retry controls.
- Low / patch: Resumed flag coverage omitted ownership reset boundaries. Both trackers now assert false with a new identity after Forget and exact retention expiry.
- Low / patch: deprecation reflection omitted callable legacy lifecycle success. Direct interface calls for enable/disable now pin minted ULID, envelope, literal payload and correlation.
- Low / patch: new awaited legacy removal wrapper lacked caller-cancellation coverage. Substitute pins exact token forwarding and OperationCanceledException propagation without fallback conversion.
- Independent recheck: all six findings addressed; no remaining concrete findings and nothing deferred from this review.

- Final verification: restored Debug source build passed with zero warnings/errors; full UI suite passed 7,187/7,187 with zero failures/skips. Prior-behavior control rejected exactly four of eight cases. Ledger preservation, scoped formatting, whitespace and pinned commitlint checks pass. Evidence and run manifest are committed with this spec.
