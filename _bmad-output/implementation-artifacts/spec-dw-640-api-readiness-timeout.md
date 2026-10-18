---
title: 'DW-640: Prove the API gateway readiness timeout'
type: 'chore'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
baseline_commit: '1532aef62d4532562a5837447463297e64358333'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The public Tenants API registers a three-second EventStore gateway readiness timeout, but its existing tests return immediately and cannot detect removal of the timeout. DW-640 tracks this regression coverage gap.

**Approach:** Add a test that resolves the effective health-check registration from the real API host and asserts its three-second timeout. Prove the test rejects removal of the production timeout, run the related readiness coverage, and close only DW-640 with reproducible evidence and independent review. Preserve runtime behavior, unrelated deferred records, dependency revisions, sprint tracking and release qualification holds.

</frozen-after-approval>

## Implementation Notes

- No unresolved intent gaps, irreversible operations or public API changes. Small footprint: the readiness test file, this specification, validation evidence, and the DW-640 ledger/index entries. The user selected DW-640 explicitly. The oneshot route requires independent review and a validated local Conventional Commit; no push is requested.
- Reuse `TenantsApiReadinessTests` and its `WebApplicationFactory<TenantsApi::Program>` setup. Resolve `IOptions<HealthCheckServiceOptions>` through the host service provider, select the `eventstore-gateway` registration by name, and assert the exact timeout. Existing HTTP tests preserve gateway failure and liveness behavior. `Program.cs` and `EventStoreGatewayReadinessHealthCheck.cs` already implement the intended behavior and need no permanent edits.
- Validation plan: repository-required Aspire baseline attempt; focused IntegrationTests Debug source build; readiness class through the maintained project runner where available, with documented direct-assembly fallback if required; isolated source-overlay mutation removing the production timeout, followed by a normal rebuild and passing regression; exact ledger/index preservation and whitespace checks. Capture ordinary gate blockers separately from environment-pinned fallback evidence. Do not claim external-topology or complete integration-suite qualification from focused host tests.

- Implemented one real-host registration test, with no production change. All four audit-enabled Debug source builds succeed with zero warnings/errors. The maintained project readiness class passes 6/6 before and after the mutation; a temporary compile overlay removing the timeout makes the new case fail on the infinite default (exit 2, one expected assertion failure). The overlay leaves repository source untouched. [Exact commands and limits](dw-640-validation-evidence.md) retain the incomplete Aspire baseline separately from passing focused tests.
- The DW-640-only preservation check passes: all 655 IDs remain, with 376 done/279 open, 192 actionable and 251 exactly covering maintenance rows. All historical fields, unrelated records, runtime source, dependencies, archive, sprint and release holds remain preserved. Independent review is pending.

Historical handoff record: the pending-review sentence above describes the initial review handoff. It is superseded by the completed review and correction recheck below; it does not describe the final state.

- Review corrections: the registration fixture now pins a valid loopback Dapr endpoint and passes with both ambient endpoint and port deliberately invalid (1/1). It explicitly checks `FailureStatus.Unhealthy`. A Degraded compile overlay fails the new assertion and the two existing 503-response cases (3 passed/3 failed); this refutes the review's assertion that the older HTTP tests all stayed green. The direct assertion is retained for a precise configuration diagnostic. The checker now rejects unrelated untracked files, and evidence retains the exact timeout assertion excerpt.

- Completion: all review findings are triaged, nothing is deferred, and no new deferred ID is added. The final ordinary Debug source build has zero warnings/errors; the maintained readiness class passes 6/6 with zero failures/skips after normal compilation is restored. Evidence retains the review controls and all seven build/test receipts. Pinned commitlint 21.2.3 accepts the exact local candidate with zero problems/warnings. A correction recheck is requested before the workflow commit.

- Independent correction recheck verified all 15 artifact hashes and the preservation checker, reran all six readiness cases with invalid ambient endpoint/port (6/6, exit 0), retracted the original Degraded-coverage claim, and found no code/evidence issues. Its remaining wording correction is recorded in the triage log and addressed by the historical label.

## Review Triage Log

- **low / patch — ambient Dapr configuration:** verified in `DaprHttpEndpointResolver.Resolve` and the reviewer's hostile-environment reproduction. Pinning the endpoint follows the existing fixtures; the corrected test passes with invalid ambient endpoint and port.
- **false — alleged missing failure-status coverage:** the Degraded mutation fails both existing ServiceUnavailable HTTP cases as well as the new explicit assertion, refuting the claim that all existing cases remain green. The extra direct assertion is retained as a small diagnostic improvement; production remains unchanged.
- **low / patch — untracked scope-check omission:** the evidence checker now includes `git ls-files --others --exclude-standard`; it rejects an exclusively created untracked probe, passes after cleanup and preserves only the allowed five paths.
- **low / patch — ephemeral diagnostic detail:** evidence now includes the exact failing method, expected three-second timeout, infinite actual timeout and result counters in a durable excerpt alongside hashes, commands and receipts.
- **false — incomplete review metadata at review time:** `in-review` and a pending note correctly represented the initial review handoff. Completion metadata is finalized after triage and verification; iteration zero remains correct because this oneshot run never entered a dispatch review loop.

- **low / patch — historical pending-review wording:** correction recheck found the initial pending sentence could be read as current. An explicit historical label now preserves the append-only notes while identifying the completed review as the final state.
