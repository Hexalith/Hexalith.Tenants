---
title: 'Close Story 5.4 audit state review findings'
type: 'bugfix'
created: '2026-10-01'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 73a5e61a1973eea40d4b70eac44c1d1202f98cdb
context:
  - _bmad-output/implementation-artifacts/epic-5-context.md
  - _bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.4 Review 6 leaves four audit-state derivation patches open. Lifecycle and configuration snapshots retain misleading audit states after readable status recovery or failed reads, collapse missing event-count evidence into runtime unavailability, and retain pending or delayed audit after a rejected configuration removal.

**Approach:** Route the affected audit assignments through the existing canonical helpers, keeping command lifecycle, stored-event evidence, projection confirmation, attempt ownership, and recovery behavior intact. Pin the distinctions with focused snapshot regressions: a pending lookup keeps the audit state; a retryable failed lookup becomes unavailable; a readable Received/Processing clears earlier uncertainty when no events were stored; a zero-event completion implies no audit unless earlier events exist; a missing count stays audit-pending; and rejection implies audit not started even when the command's stronger lifecycle evidence is retained. Close the parent spec's four Review 6 patches with current verification evidence.

</frozen-after-approval>

## Implementation Notes

- Investigation: all four Review 6 findings are present. The work changes audit assignments in `TenantLifecycleCommandSnapshot.ApplyStatus` within `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` and in `TenantSetConfigurationCommandSnapshot.ApplyStatus` / `TenantRemoveConfigurationCommandSnapshot.ApplyStatus`, with regressions in their three `tests/Hexalith.Tenants.UI.Tests/State/*CommandSnapshotTests.cs` files. Reuse `TenantCommandAuditStates.FromStatusLookup` and `FromCommandStatus`; do not change either helper.
- Planning facts: no unresolved intent gaps or irreversible operations; three existing reducer files plus their three existing test files, the parent review record, this follow-up spec, and sprint tracking. No new public API or dependency changes.
- Existing tests require command lifecycle to stay unable-to-verify for missing/invalid event counts where it does today, and removal to preserve projection-pending/degraded lifecycle after a delayed rejection. Correct only their independent audit dimension.
- The already-running Aspire topology was inspected before editing; the Tenants backend reports Running/Healthy. This change does not require AppHost edits.
- Implemented the four audit-state fixes through the existing helpers. Both configuration reducers also recompute audit on their guarded Received/Processing arms, allowing readable status to recover after a failed lookup without changing stronger command lifecycle evidence.
- Added 41 snapshot regression cases across the three snapshot test files, and corrected the two lifecycle/removal rows plus the comment in `TenantCommandAuditStatesTests.A_completed_status_with_zero_events_never_claims_a_pending_audit_record`. Existing source line endings are preserved.
- Regression evidence before reducer edits: Debug build succeeded with zero warnings/errors; the four-class run found 29 failures out of 291 tests, all on the targeted audit mismatches. After the reducer edits, the same 291 tests pass with zero failed/skipped.
- Build: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseNuGetDeps=false --no-restore -m:1` passed with zero warnings/errors. The initial baseline build used the same command without `--no-restore` and also passed. Debug uses available Hexalith source project references.
- Focused tests: `tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests -noLogo -class '*TenantLifecycleCommandSnapshotTests' -class '*TenantSetConfigurationCommandSnapshotTests' -class '*TenantRemoveConfigurationCommandSnapshotTests' -class '*TenantCommandAuditStatesTests'` passed 291/291.
- Full UI suite: `dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseNuGetDeps=false --no-build --no-restore` passed 3659/3659, zero failed/skipped. The parent spec's four Review 6 patches are closed, and the parent and legacy story now await review.
- Review amendments: expanded both configuration readable-recovery theories to Accepted, ProjectionPending, and Degraded, each with and without earlier stored events where applicable. Twelve additional rows bring the new regression count to 53. All newly added public test methods use PascalCase and have XML summaries and parameter documentation.
- Completion policy is unchanged: the existing canonical helper maps every readable Completed status except zero-without-earlier-events to AuditPending, including a null or malformed negative count. Those counts never establish HasCommandEventEvidence; the existing fail-closed command-verification branches remain intact. Review 6 explicitly requests the helper result. Review 5's stopped-tracking exception is specifically the terminal Lifecycle ProjectionUnverified path, which is unchanged. The Lifecycle panel cannot requery a terminal missing-count attempt but still offers its authorized Inspect audit navigation; no receipt or projection confirmation is fabricated.

- Final verification after review amendments: the Debug/source-reference build above passed with zero warnings/errors; the same focused four-class command passed 303/303; the full UI command passed 3671/3671, zero failed/skipped, in 45.954 seconds. No new deferred work remains from this review.
- Tracking: this follow-up is done; the parent spec is in-review, the legacy story is review, and sprint-status marks `5-4-understand-audit-availability-and-recovery` review. The existing last_updated date is 10-01-2026.
- Commit-message validation: `./node_modules/.bin/commitlint --edit /tmp/tenants-story-5-4-commit-message.txt --verbose` exited 0 using the owning repository's installed, lockfile-pinned CLI and `commitlint.config.mjs`. The exact full candidate below produced `found 0 problems, 0 warnings`; the successful output is preserved in `/tmp/tenants-story-5-4-commitlint.log`.

```text
fix(audit): align lifecycle and configuration audit states

Close Story 5.4 Review 6 findings without changing command lifecycle safeguards.
Add 53 regression cases and record focused and full UI verification.
```

## Review Triage Log

- BH1 — false as a regression defect: terminal Lifecycle Completed(null/-1) does keep AuditPending without automatic reconciliation, as Review 6 explicitly requires while preserving command lifecycle. The user-recorded Review 5 exception is limited to terminal ProjectionUnverified. Applying AfterTrackingEnds to Completed would introduce a second exception and contradict the current completion mapping. The lifecycle flow still supplies authorized Inspect audit navigation. The observation is recorded in Implementation Notes, including the absence of in-panel requery.
- BH2 — medium, patched: the first recovery theories exercised only ProjectionPending and left the changed configuration Degraded guards unpinned. Both theories now cover Received/Processing after PublishFailed and a failed lookup, with and without prior stored-event evidence, as well as the Accepted/ProjectionPending cases.
- BH3 — low, patched: new public test methods used underscore names contrary to the Hexalith baseline. Renamed all new methods to PascalCase.
- BH4 — low, patched: new public test methods lacked required XML documentation. Added summaries and parameter documentation to every newly introduced method.
- BH5 — false as unfinished work: sprint-status synchronization is the prescribed post-review finalization step; it had not run when the reviewer inspected the tree. The final story status is review and last_updated remains the current date.
- BH6 — false as unfinished work: the follow-up spec remains in-progress until this review's findings are classified, then the workflow marks it done. The parent story remains in-review for the later full-story review.
- BH7 — false as a new policy gap: no helper or negative-count policy changed. The intent requires the existing canonical derivation, and its Completed fallback already covers negative counts; command verification still fails closed where it did before. Implementation Notes explicitly record that distinction. No frozen intent amendment is needed.
