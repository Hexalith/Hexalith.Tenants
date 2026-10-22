---
title: 'Resolve five deferred lifecycle command maintenance records'
type: 'refactor'
created: '2026-10-22'
status: 'done'
route: 'oneshot'
baseline_commit: '4354b0e0687766c0995383f630ec54f9b2a29730'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Resolve exactly DW-301, DW-302, DW-321, DW-322 and DW-323 from the Tenants deferred-work ledger. Preserve lifecycle command identity, cancellation, caller isolation, retained admission and authoritative projection proof while consolidating the existing duplicated implementation. Apply the ledger's recorded decisions: typed high-impact evidence supplies the canonical projection version, public component parameters remain compatible, and the existing pending-status count enforces a bounded wait.

Verify the implementation and independent review, preserve historical ledger facts and every unrelated record, then commit and push the completed changes to `main` as requested.

</frozen-after-approval>

## Implementation Notes

- Planning route: bounded internal maintenance in the existing UI lifecycle flow, no unresolved intent gaps, migrations or deployments. Publication is the explicitly requested Git push after verification. Existing ledger decisions settle version precedence and poll-budget behavior.
- Entry revision: `4354b0e0` (full canonical identifier will be captured in validation evidence).
- DW-301: consolidate metadata/lifecycle proof generation, per-operation cancellation and literal tenant/caller scope through an internal helper used by `Components/Pages/TenantDetailPage.razor`; consolidate the identical unconditional detail proof read in `Services/Gateways/TenantQueryGateway.cs`. Preserve independently keyed surfaces, route-generation retirement, multiple simultaneous operations and cancellation-source disposal by each operation's owner.
- DW-302: in `Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor` and `TenantLifecycleCommandFlow.razor`, use supplied high-impact evidence for version capture and availability. Legacy `ProjectionVersion` may populate evidence only when typed evidence is absent. Conflicting or missing typed versions must fail closed, including preview/remount and submit paths. Keep public parameters usable.
- DW-321: consolidate repeated lifecycle/configuration lease reclamation/release in the detail page, preserving exact surface owner identity and each tracker's retained ownership polarity. Share lifecycle focus exception handling without changing focus targets or swallowing unsupported failures.
- DW-322: in `State/TenantCommands/TenantLifecycleAttemptTracker.cs`, replace global pruning on reads with constant-size requested-key expiry checks. Full cleanup runs on existing mutations. Preserve exact five-minute expiry, clock regression, terminal tombstones and rejection of stale arrivals. Add an allocation regression with many unrelated retained tenants.
- DW-323: in `State/TenantCommands/TenantLifecycleCommandSnapshot.cs`, bound inconclusive status observations to 30 polls in addition to the existing five-minute deadline. Count missing/transient and verified Received/Processing observations, retain counts across remount/merge, surface existing localized status uncertainty at exhaustion, and preserve stored-event evidence for the final authoritative projection read. Terminal results remain authoritative. Test before/at/after limits and exact identity/ownership behavior.
- Acceptance: given route or caller replacement, obsolete proof results cannot apply; given distinct proof surfaces, one reset cannot cancel another. Given conflicting typed/legacy versions, typed evidence governs. Given another surface's lease, cleanup cannot release it. Given expired requested ownership, reads return no live attempt without scanning unrelated entries. Given exhausted pending polls, uncertainty is terminal under the existing lifecycle policy; event evidence still gets its projection proof.
- Verification: Debug source build of `tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj`, focused direct xUnit cases, full UI project suite with serialized execution, ledger preservation verifier, story gitlink guard, whitespace check and pinned commitlint. Reuse existing lifecycle, detail scope, focus and lease tests; add behavioral boundary regressions.
- Before editing, Aspire start succeeded. Describe found security Running/Unhealthy and dependent Tenants/EventStore/UI Waiting; Memories was Healthy. Stopped this AppHost successfully. Distributed-runtime/browser readiness is not claimed.
- Canonical entry revision: `4354b0e0687766c0995383f630ec54f9b2a29730`. The user explicitly requested five items in one batch; these five share the existing lifecycle flow and its tests.
- Shared scope extraction retains per-surface concurrency and captured caller proof. Reset snapshots sources so reentrant cancellation completion cannot dispose a subsequently selected source or retire a new-generation read. Page lease helpers preserve nullable tracker semantics, route-only reclamation and the lifecycle active-surface exclusion.
- Initial root state verification passed 123/123. A subsequent allocation-baseline build exposed one stale page route field reference (CS0103); corrected it to the helper's locked tenant-only comparison and added an ordinal/caller/invalidation regression. This failure is retained in raw evidence.
- The expanded focused run selected 855 cases: 853 passed, two new missing-version remount cases failed because their fixture omitted the projection reader. The existing unsupported-proof policy correctly terminalized tracking. The fixture now supplies a nonterminal proof reader; production behavior is preserved. A previous mixed class/method filter selected zero tests and is retained as an invalid attempt; corrected TRX counters are explicitly checked.
- Independent Blind Hunter reviewed 112,130 bytes and returned ten findings. They are verification/maintenance gaps; no production behavior regression was established. All are simple patches within the selected scope, with no new deferred record.

## Review Triage Log

| Finding | Verdict and evidence | Resolution |
| --- | --- | --- |
| Dispatch-only expiry not covered | low: accepted snapshots remove dispatch ownership, so the initial expiry theory did not reach that branch. | Patch: dispatch-only exact-deadline and clock-regression cases for every read seam. |
| Allocation setup omits dispatch/tombstones | low: initial setup retained snapshots only. | Patch: measure 3,000 reads with 256 entries in each of all three collections. |
| Read/mutation expiry policy duplicated | low: both methods independently encoded terminal, snapshot and dispatch expiry. | Patch: mutation enumeration calls the same per-tenant expiration method used by reads. |
| Poll exemption combines time/count boundaries | low: the initial event-proof case used both limits at once. | Patch: separate before-deadline and exact-deadline cases. |
| Terminal boundary matrix incomplete | low: the initial matrix omitted degraded/timed-out and no-event completion. | Patch: add PublishFailed, TimedOut and zero/null-event completion at the count boundary. |
| Rendered exhausted event proof not covered | low: the initial mounted budget case only exercised Processing exhaustion. | Patch: retained stored-event/exhausted-count proof must still run and confirm before admission release. |
| Nullable tracker polarity unqualified | low: the initial page matrix supplied all trackers. | Patch: qualify absent trackers for ordinary cleanup, late callbacks and successor ownership. |
| Proof reset/dispose cross-thread race unqualified | low: existing overlap and reentrancy cases were synchronous. | Patch: deterministic competing reset/disposal with bounded signals and owner-only disposal. |
| Budget copy uses stub resources | low: the new exhaustion case checked its key but not shipped EN/FR copy. | Patch: render both actual resource cultures and recovery guidance. |
| Closure verifier accepts empty/missing evidence | low: mutable evidence fields were presence-checked only. | Patch: require nonempty substantive verification and existing verification-file paths. |

- Independent recheck accepts all ten review patches with no new concrete regression. Review adds dispatch/tombstone allocation coverage, nullable-tracker polarity, competing-thread disposal, resource-backed EN/FR assertions and a held final projection proof. No additional deferred work was created.
- Reviewed Debug source build passes with zero warnings/errors; expanded focused regressions pass 875/875 and unconditional gateway proofs pass 2/2. The first full suite catches the new PollBudget localizer stub using different wording than the shipped resource; corrected the stub to the exact resource value while preserving the parity gate and production copy. The complete suite will be rebuilt and rerun before closure.
- First complete UI result: 7,396 passed/two failed/zero skipped out of 7,398, exit 2. Besides the corrected new localizer stub, one unchanged global-administrator finite-bounds test hit its one-second bUnit timer-cleanup wait. Record the exact failure separately; preserve that test and all timeout gates while rerunning the final complete build.


## Final Verification

Final Debug source build: zero warnings/errors, exit 0. Focused regressions 875/875; gateway proofs 2/2; localizer parity 3/3. The final complete UI run passes 7,398/7,398 with zero failures/skips/not-executed, exit 0, 1m57.261s. Parsed final TRX confirms all 875 focused and both gateway cases passed in that same complete run. All 15 changed source/test file hashes still match the final build.

Ledger preservation passes: all 657 IDs retained, exactly five selected records changed, all 652 unrelated records unchanged; 530 done/127 open across 117 maintenance rows. Historical selected fields reconstruct losslessly. Archive, sprint, release-readiness and dependency pointers remain unchanged. Independent review/recheck clear; no new deferrals. See `dw-301-302-321-322-323-validation-evidence.md` and the raw evidence archive for commands, initial failures and final checks.
