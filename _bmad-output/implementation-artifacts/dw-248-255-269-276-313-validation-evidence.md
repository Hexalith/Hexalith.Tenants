# Command recovery safety validation — 2026-10-22

Entry revision: `e7d851763f4e3a13edb7f294b1c88e6992cddc84`. Scope: exactly DW-248, DW-255, DW-269, DW-276 and DW-313.

## Behavior and evidence

- DW-248: all three membership flows consume the shared `TenantMembershipStatusRefreshDrain`. Batches retain projection intent with generation-tagged OR/consume semantics; enqueue and idle handoff share one lock. Explicit queued work still drains after a failure, then every waiter observes it. Removal's confirmed/rejected audit refresh remains eligible. Eight helper cases cover held faults, generation replacement, generation-to-callback handoff, cross-generation failure visibility, held disposal, retirement and 10,000 synchronous nudges; two rendered terminal removal cases qualify faulted parent reload plus queued manual recovery; existing rendered membership recovery cases run in the maintained suite.
- DW-255: create dispatch/status observations use the optional host `TimeProvider`, falling back to the system clock. The original public timestamp bounds missing/transient or Received/Processing status evidence to five minutes. Exhaustion exposes recoverable uncertainty, never ownership expiry. Thirteen snapshot cases pin exact boundaries, repeated waits, absent/future timing, stored-event preservation and later authoritative recovery; eight rendered cases use actual shipped EN/FR resources and prove retained identity/admission through mounted recovery, real unmount/remount, held ambiguous delivery and same-identity retry, and authoritative late rejection. Another tenant’s exact lease survives rejection release.
- DW-269: explicit numeric values preserve current observable 0/1/2/3/4 values. Five tests pin them; the historical shift remains in the ledger, without claiming a persistence migration.
- DW-276: five rendered held-lease cases cover same, mismatched, blank, whitespace and null refused identities. The current flag, holder and gate owner survive refusal; the refusing surface cannot release another's lease. The actual holder still releases normally. Production behavior is preserved and its rationale documented.
- DW-313: the already-shared complete create store is qualified directly. Legacy Forget cannot discard complete intent/timing/baseline or admission; a replacement adopts exact evidence. An older completion/update cannot replace a successor or release its lease. Lifecycle-specific bounded ownership remains a distinct policy; create dispatched ownership does not expire.

## Executed validation

The initial dedicated run passes **25/25**. After review expansion, the final dedicated run passes **42/42**, zero errors/failures/skips/not-run, exit 0. The Debug source build passes with **zero warnings/errors**, exit 0.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false

dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll \
  -method '*PendingWaitUsesOriginalTimestampAndKeepsRecoverableIdentity' \
  -method '*RepeatedPendingObservationsCannotRenewCreateWaitOrExpireStoredEventEvidence' \
  -method '*HostClockBoundsPendingCreateWithoutRenewingOrReleasingItsRetainedAttempt' \
  -method '*LegacyForgetCannotDiscardCompleteCreateAndStaleDeliveryCannotReleaseReplacement' \
  -method '*AvailabilityNumbersRemainStable' \
  -method '*QueuedProjectionIntentDrainsAfterHeldFailureAndAllWaitersObserveIt' \
  -method '*ReplacementDropsOnlyObsoleteProjectionIntentAndContinuesItsOwnStatus' \
  -method '*SustainedSynchronousNudgesUseOneIterativeOwnerAndStopWhenDisposed' \
  -method '*A_second_command_surface_cannot_share_or_release_the_aggregate_lease_of_another' \
  -method '*GenerationChangeAtHandoffCannotApplyOldProjectionIntentToReplacement' \
  -method '*FailedOlderGenerationStillReportsFailureToJoinedReplacementAfterServingItsWork' \
  -method '*DisposalDuringHeldBatchRetiresEveryWaiterWithoutRunningQueuedProjection' \
  -method '*TerminalRemovalDrainsQueuedManualRecoveryAfterParentRefreshFailure' \
  -parallelMode none -result-trx /tmp/tenants-five-reviewed-focused.trx

dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --parallel none --minimum-expected-tests 7324 --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/tenants-five-reviewed-full

python3 _bmad-output/implementation-artifacts/dw-248-255-269-276-313-verify.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-248-255-269-276-313-command-recovery-safety.md
git diff --check
```

The pre-review corrected complete UI run passed 7,313/7,313, zero failures/skips, exit 0, 2m04.384s. The expanded final maintained run passes **7,324/7,324**, zero errors/failures/skips/not-executed, **exit 0**, 2m09.887s. Parsed TRX confirms all 42 dedicated cases pass in that same complete run. The ledger verifier and story gitlink guard pass. The preservation verifier requires all 657 IDs, all 652 unrelated record bodies and the preceding ledger narrative unchanged; selected historical fields are reconstructed from `pre-command-recovery-` fields. The maintenance index exactly covers 132 open IDs across 122 rows; disposition counts are 525 done, 66 actionable, 15 blocked, 23 awaiting-decision, 6 accepted-limitation and 22 uncertain. Archive, sprint, release-readiness artifact and dependency pointers remain unchanged.

## Initial failures and runtime limits

The first new-test build failed CS8852 (assignment to the stub gateway's init-only status) and CS0117 (using the public audit-state enum rather than its supported alias). Corrected the fixture to use a captured asynchronous status provider and the existing alias. No production gate changed.

The first project-run command used `--filter-method` with pipe-separated patterns; it executed zero cases and failed the minimum-test policy, exit 5. Direct xUnit repeated `-method` arguments select the cases correctly. That first selected run passed 23/24: the create component's stub localizer lacked the new whole-string resource. The corrected test uses actual shipped English and French resources, producing 25 passing cases. Original failures remain in the raw evidence.

Before editing, `aspire ps --format Json` returned no running app. `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` succeeded; `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` found security Running/Unhealthy and dependent Tenants/EventStore/UI resources Waiting. Memories resources were Running/Healthy. `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` succeeded. No healthy distributed-runtime baseline, authenticated-browser acceptance or release readiness is claimed.

## Compatibility correction during maintained validation

The first complete UI run executed 7,307 cases: 7,303 passed, four failed, zero skipped, exit 2, 2m00.082s. All failures were existing create temporary-status/safe-message contracts: the newly applied system-clock deadline incorrectly expired old callers with absent or fixed-epoch timestamps. The correction preserves the one-argument `ApplyStatus` API and opts the live flow into timing through the explicit clock-aware overload. Existing fixtures and their whole-record assertions remain unchanged. The boundary matrix also now includes verified Received and Processing status at before/exact/after deadline, adding six cases. That corrected pre-review complete run passed 7,313/7,313 with zero failures/skips. Review expansion adds eleven cases, so final validation expects at least 7,324 cases.

## Independent review and recheck

The configured Blind Hunter reviewed the complete diff and untracked source/tests/artifacts. All ten findings were triaged individually in the spec. The consumed refresh generation now reaches each production core, which refuses mismatches before capturing attempt state. The existing first-failure policy for all joined callers is explicitly documented and tested. EN/FR copy describes unresolved outcome rather than unverified status. Tests now cover bounded fault assertions, disposal with queued work, rendered terminal removal replay, actual create remount, held ambiguous retry and authoritative rejection release; public overload documentation names timing opt-in.

The first expanded focused run executed 42 cases: 34 passed and eight failed because the new fixture used the legacy gate owner predicate on an ownership-safe lease. The fixture now checks the exact lease object plus its CurrentOwner; no production behavior or ownership assertion was relaxed. The rebuilt expanded focused run passes 42/42, zero failures/skips/not-run, exit 0. Its Debug source build has zero warnings/errors. Independent recheck accepts all ten corrections and identifies no remaining concrete code issue. No additional deferred record was created.

## Final evidence and Git checks

The final Debug source build has zero warnings/errors; dedicated 42/42 and complete 7,324/7,324 pass with zero failures/skips. Independent recheck accepts all ten patches. The ledger preservation verifier, story gitlink guard and whitespace checks pass. Git emits its existing CRLF-to-index-LF normalization notices without whitespace errors. Fetch of origin/main found zero divergence from the entry revision before committing. The exact full commit-message file passes the pinned @commitlint/cli 21.2.3 with zero problems/warnings; its bytes and SHA-256 are preserved.

[Raw build/test logs, initial failures, full TRX reports, sanitized Aspire state, exact focused command, ledger/story checks, source/assembly hashes and commit-message validation](dw-248-255-269-276-313-raw-logs.zip) preserve the executed evidence. This archive uses an explicit list of this batch's artifacts. No dependency pointers, archive/sprint records, release holds or unrelated deferred work changed.
