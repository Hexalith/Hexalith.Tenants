---
title: 'Complete deferred command model maintenance'
type: 'refactor'
created: '2026-10-21'
status: 'done'
route: 'oneshot'
baseline_commit: '6cd7521fc0732823490c2c2f18da0120f77991b5'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete five related source-maintenance records: DW-320, DW-479, DW-530, DW-605 and DW-606. Remove the unused lifecycle-specific duplicate-state factory and handling while preserving the live refusal path, command identity, audit evidence and other flows' duplicate handling. Qualify the existing fourteen-type command model split and record the three stale records accurately. Make the two files named by DW-606 reliably check out with the repository-required CRLF endings.

Preserve historical ledger fields and unrelated records, validate the focused behavior and full UI suite, review the result, then commit and push main as requested. Keep dependency pointers, persistence, authorization and release settings intact.

</frozen-after-approval>

## Implementation Notes

- Planning found no unresolved intent choice or irreversible implementation step. The user authorizes commit and push. This small change uses the oneshot route: lifecycle snapshot/component cleanup, two existing test updates, two scoped Git attributes, ledger/index closeout and evidence. No new public interface or service is needed.
- Read-only investigation confirms `5f41f089157ef077c5145f34a260004cafa5349c` already split the fourteen public types and deleted `TenantCreateCommandModels.cs`. Requalify those files rather than repeating the split. Both DW-606 working files already contain only CRLF, but unspecified `eol` allows fresh checkouts to revert to LF; pin only those two paths.
- Reuse `TenantLifecycleCommandSnapshot.BlockedWithTracking` and existing rendered duplicate-submit tests. Keep the shared enum member, member-removal duplicate factory, common audit mapping and lifecycle refusal resources. Replace only tests that call the removed lifecycle factory.
- Runtime baseline: existing Aspire AppHost and resources report Running/Healthy. Validation will use a Debug source-reference UI test build, affected classes, full UI acceptance, real checkout byte checks and story-gitlink validation.
- Initial qualification passes: Debug source build zero warnings/errors, focused 382/382 and full UI 7115/7115 with zero failures/skips. Exact source checks reproduce the old LF checkout and the new CRLF checkout; the existing fourteen-type split is independently confirmed.
- Review corrections remove the two now-unused lifecycle state labels (keeping the live refusal message/recovery keys), expand the active-attempt refusal test to Accepted and ProjectionPending with nondefault audit evidence, and record reproducible source/ledger checks plus test-run provenance. Final validation follows these corrections.

## Review Triage Log

Blind Hunter: 24.704 kB; N = min(floor(sqrt(24.704) + 1), 10) = 5. Independent source and ledger inspection confirms the current closure conclusions and finds no implementation regression.

1. **low / patch:** Existing refusal checks begin with NotStarted audit state. Expand the active-attempt test through actual EventsPublished status and assert AuditPending evidence, exact identity and nonterminal ownership survive duplicate refusal.
2. **low / patch:** Removed lifecycle state producer leaves an unused EN/FR state label. Delete both state-label resources while preserving the still-used refusal message and recovery strings.
3. **low / patch:** Source-check results lack a reproducible invocation. Record the full one-off declaration and temporary-checkout procedure in the validation evidence.
4. **low / patch:** Ledger-check results lack a reproducible invocation. Record the full baseline, historical-field and exact maintenance-coverage comparison in the validation evidence.
5. **low / patch:** Aggregate test counts lack retained run/assembly attribution. Save a compact manifest with TRX/assembly SHA-256, run identifiers, times and counters for the final accepted runs.

- The review test initially named the audit enum member `Pending`, producing CS0117. Corrected to the existing `AuditPending`; the final Debug build has zero warnings/errors and focused 383/383 pass. The initial failed log is retained separately. No production behavior or build policy was changed to resolve it.
- Final acceptance: 7116/7116 full UI tests pass with zero failures/skips. Independent recheck resolves all five findings, verifies the run/assembly provenance and finds no introduced defect. The saved source/ledger checker, whitespace and gitlink gates pass. All 657 IDs and historical values remain, with only the five selected records closed: 435 done/222 open across 202 exact-covering rows. Pinned commitlint passes the exact commit message; fetched main has zero divergence. Commit and push proceed under the user's existing authorization.
