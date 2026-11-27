---
title: 'Reconcile deferred work with newer verification evidence'
type: 'chore'
created: '2026-11-27'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

DW-669, DW-712 and DW-713 are open in the deferred-work ledger. Bootstrap readiness tests and hosted EventStore scanner evidence postdate the underlying findings. Evaluate the evidence against the recorded criteria, close the entries that qualify, and report the updated open count.

</frozen-after-approval>

## Implementation Notes

- The active ledger records the three closures and preserves the original findings. The maintenance backlog links to the active count and dates the October index.
- Debug source builds on Tenants `5b97ef94de2d94f79da177817cfb8b9d371a6faa` succeeded with zero warnings and errors. The bound-command class passed 30 cases and the hosted-service dispatch class passed 21. The independent combined run passed all 51 cases. The catalog cases cover a wrong-root-to-matching-root transition and cancellation during readiness, with zero POSTs, workload assertions or receipts on cancellation.
  ```sh
  run-with-temp bash -c 'dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --artifacts-path "$TMPDIR/artifacts" -m:1 -v:minimal && dotnet "$TMPDIR/artifacts/bin/Hexalith.Tenants.Server.Tests/debug/Hexalith.Tenants.Server.Tests.dll" -class Hexalith.Tenants.Server.Tests.Bootstrap.TenantBootstrapBoundCommandTests -class Hexalith.Tenants.Server.Tests.Bootstrap.TenantBootstrapAppReadinessDispatchTests'
  ```
- The hosted EventStore workflow and job both completed successfully for the repair at `8b6d5bee5217b3f4bbd8fd689ad887aca863961b` and the Tenants pin at `5484fae6c284cd8fe70b51bf921c5b31509b6c27`. The repair uses `workflow_run_bodies` and tests later-body and compact-expression refusals. [Repair run](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/43862075403) · [Pinned revision run](https://github.com/Hexalith/Hexalith.EventStore/actions/runs/45965786177)
  ```sh
  gh run view 43862075403 --repo Hexalith/Hexalith.EventStore --json headSha,status,conclusion,name,url,jobs
  gh run view 45965786177 --repo Hexalith/Hexalith.EventStore --json headSha,status,conclusion,name,url,jobs
  ```
- Ledger validation preserved 715 unique records and all original finding text. Only the three selected records changed status. The resulting count is 666 done and 49 open. `git diff --check` passed.
- The repository-pinned commitlint CLI v21.2.4 accepted the exact commit message with zero problems and zero warnings.

## Review Triage Log

- Medium, patched. Added the passing dispatch cases to establish the no-POST, no-workload-assertion and no-receipt outcome when cancellation interrupts catalog readiness.
- Medium, patched. Verified the hosted fixture job at the EventStore revision currently pinned by Tenants and recorded that result beside the original repair run.
- Low, patched. Dated the three-open subtotal and linked it to the current ledger.
- Low, patched. Linked the three previously unnumbered notes to DW-660, DW-663 and DW-666 and synchronized their stated dispositions.
