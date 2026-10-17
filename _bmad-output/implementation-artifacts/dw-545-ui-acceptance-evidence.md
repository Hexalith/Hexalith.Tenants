# DW-545 UI acceptance follow-up

Entry: clean `main` at `43ecec769267309f9801fb772fcad711403a2301`, 2026-10-17. This follows the preserved [DW-545 conflict-coverage evidence](dw-545-validation-evidence.md). The original full UI failures remain historical failing results. Acceptance is complete after final full UI and independent review/recheck.

## Reproduction and correction

The existing eight-case `PendingRemovalAuthorityReadReportsChangedSafety` run reproduces three failures (five passed, zero skipped, exit 2): FR freshness/retry shows the original delivery message; FR confirmation first/retry shows the generic changed-admission message. The initial Debug source build succeeds with zero warnings/errors. These reproduce the previously unexplained results without a loader or test change.

Two source orderings account for the failure shapes. `OnConfirmationInput` cancels admission before storing new confirmation text; a cancellation continuation can inspect the old matching value. `SubmitAsync` resumes outside the renderer after held authority and cleanup, so its snapshot reads/publication can race `OnParametersSet` and retained-attempt restoration. The retry tracker intentionally restores the original failed evidence on detachment; unsynchronized restoration can overwrite current refusal feedback.

The strengthened theory adds immediate/held release to the original eight combinations, checks actual renderer access in the release callback, and asserts settled snapshot feedback before the exact EN/FR DOM and live-region checks. Held cleanup also proves pending ownership, locked aggregate and duplicate suppression, then completes from a worker. **All 16 cases fail on unchanged runtime** at the dispatcher-access assertion (zero passed/skipped, exit 2). This controlled failure establishes the renderer violation; it does not claim every historical message race was deterministically forced independently.

The repair stores confirmation before cancellation and preserves renderer context on the four admission/cleanup awaits. Consuming initial and final authority results also rechecks admission generation after resuming. Existing attempt/disposal guards, cancellation, exact refusal logic, late-result rejection, current authority reads and same-ID retry recovery remain. The loader implementation and its already accepted 72 conflict cases remain unchanged.

## Exact commands and results

Commands run from the repository root. Build and test logs/TRX are retained under `/tmp/tenants-dw-545-followup/`.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --nologo -v:q > /tmp/tenants-dw-545-followup/baseline-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*RemoveTenantMemberFlowTests.PendingRemovalAuthorityReadReportsChangedSafety' --minimum-expected-tests 8 --no-ansi --no-progress --results-directory /tmp/tenants-dw-545-followup/baseline --report-xunit-trx --report-xunit-trx-filename baseline.trx > /tmp/tenants-dw-545-followup/baseline-tests.log 2>&1
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-restore --nologo -v:q > /tmp/tenants-dw-545-followup/control-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*RemoveTenantMemberFlowTests.PendingRemovalAuthorityReadReportsChangedSafety' --minimum-expected-tests 16 --no-ansi --no-progress --results-directory /tmp/tenants-dw-545-followup/control --report-xunit-trx --report-xunit-trx-filename control.trx > /tmp/tenants-dw-545-followup/control-tests.log 2>&1
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-restore --nologo -v:q > /tmp/tenants-dw-545-followup/fixed-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --filter-class '*GlobalAdministratorsProjectionLoaderTests' --minimum-expected-tests 290 --no-ansi --no-progress --results-directory /tmp/tenants-dw-545-followup/focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-545-followup/focused-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6239 --no-ansi --no-progress --results-directory /tmp/tenants-dw-545-followup/full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-545-followup/full-tests.log 2>&1
```

The three builds succeed with zero warnings/errors. Initial existing theory: 5/8 passed, three failed, zero skipped. Strengthened theory before runtime fix: 0/16 passed, 16 failed, zero skipped. Post-fix focused flow/loader: **316/316 passed, zero failures/skips, exit 0**, including all 16 safety variants and loader 99/99.

## Runtime baseline and limits

Before code changes, the Aspire skill started an isolated task-owned AppHost and inspected resources/logs:

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --non-interactive --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --limit 5
```

Initial describe observed startup (13 Waiting, nine Running, one Finished). Subsequent inspection found 16 Running resources and seven Finished Dapr CLI helper resources; the reported security readiness check was Healthy. Ordinary logs and lifecycle results stay local, without secrets copied into this artifact. An initial local health-summary parser assumed an array instead of a dictionary and raised AttributeError; corrected inspection read the actual dictionary. No runtime health blocker was hidden. This baseline does not claim a live/browser reproduction of the held-authority interleaving, Release/package qualification or release readiness.

## Review and preservation

Workflow Blind Hunter review, correction recheck and final bookkeeping checks pass; all four findings and their dispositions are recorded in the follow-up spec. No staging, commit, branch, push or dependency mutation is required or performed. Prior failure evidence, archive/sprint, unrelated records and release holds are preserved.

## Independent review corrections and controls

The first post-fix full UI run reports **6,238 passed, one failed, zero skipped, exit 2**, 2m18.765s. The sole failure is the unchanged `MemberReviewCancelReopenPreservesRefusedRemovalRetryIdentity(true)` immediate-dismissal assertion. Independent source review confirms synchronous Click starts CloseAsync, which awaits activity release and a renderer callback before the parent removes the flow. The fixture now awaits ClickAsync; all removal/reopen/identity assertions remain. This failed full run is preserved and is not acceptance.

Review also identified missing successful-result coverage. Four new first-authority cases hold the renderer while an Authorized result completes, then change and restore confirmation/freshness before submission resumes. They prove no admission, dispatch, status query or snapshot/identity mutation, then safe explicit recovery (including failed-delivery same-ID reuse). The first-phase wrapper completion flag is the ordering witness; no arbitrary sleep or test rerun establishes success. First-phase invalidation preserves original evidence before any new admission. Exact localized refusal claims concern the original regression's leased second authority read. The separate RequestSent ambiguous-delivery path remains a follow-up coverage gap, not a reproduced production defect.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-restore --nologo -v:q > /tmp/tenants-dw-545-followup/review-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*RemoveTenantMemberFlowTests.QueuedInitialRemovalAuthorityCannotAdmitChangedGeneration' --filter-method '*TenantDetailSurfaceTests.MemberReviewCancelReopenPreservesRefusedRemovalRetryIdentity' --minimum-expected-tests 6 --no-ansi --no-progress --results-directory /tmp/tenants-dw-545-followup/review-focused --report-xunit-trx --report-xunit-trx-filename review.trx > /tmp/tenants-dw-545-followup/review-focused-tests.log 2>&1
```

Review build: zero warnings/errors, exit 0. Four new cases plus both corrected parent cases: **6/6 passed, zero failures/skips, exit 0**.

A controlled temporary mutation removes only `admissionGeneration != Volatile.Read(ref _admissionGeneration)` from the initial post-await guard. Its Debug source build succeeds (zero warnings/errors); the exact four-case test invocation below yields **zero passed, four failed, zero skipped, exit 2**. All four fail `authorityReads.ShouldBe(1)` because the obsolete generation is admitted (observed two). A Python finally block restores the exact original runtime bytes before the final rebuild. No mutation remains in the working tree.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-restore --nologo -v:q > /tmp/tenants-dw-545-followup/mutation-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*RemoveTenantMemberFlowTests.QueuedInitialRemovalAuthorityCannotAdmitChangedGeneration' --minimum-expected-tests 4 --no-ansi --no-progress --results-directory /tmp/tenants-dw-545-followup/mutation --report-xunit-trx --report-xunit-trx-filename mutation.trx > /tmp/tenants-dw-545-followup/mutation-tests.log 2>&1
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-restore --nologo -v:q > /tmp/tenants-dw-545-followup/final-build.log 2>&1
```

The second-read generation check has source-level and cancelled-read coverage; no independent successful-second-read mutation result is claimed. The workflow review records every finding and disposition in the follow-up spec; the ambiguous-delivery path is recorded separately as DW-648.

## Final acceptance and preservation

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6243 --no-ansi --no-progress --results-directory /tmp/tenants-dw-545-followup/full-final --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-545-followup/full-final-tests.log 2>&1
python3 /tmp/tenants-dw-545-followup/finalize.py
python3 /tmp/tenants-dw-545-followup/verify-preservation.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-545-ui-acceptance-followup.md
git diff --check
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
```

Restored-source final build: **zero warnings/errors, exit 0**, 15.07s. Final full UI: **6,243 passed, zero failures/skips, exit 0**, 1m47.623s. TRX inspection includes all 16 immediate/held-cleanup cases, four queued-successful-authority cases and loader99/99 (including the original72 conflict cases). The final test run followed concrete runtime/test corrections; none of the earlier failures is counted as acceptance or suppressed.

Independent correction recheck finds no remaining concrete issue in the repaired scope. The four original findings are retained in the spec: parent fixture completion and initial-successful-authority coverage are corrected; authority-phase behavior is explicitly bounded; separate ambiguous RequestSent retry qualification is recorded as DW-648 with no production defect reproduction claim. Only DW-545 closes and both related specs are done.

Preservation check: **PASS**. All647 original IDs/fields remain; only DW-545 changes among original ledger bodies, with every replaced value retained under pre-dw-545-acceptance fields. DW-648 is the sole new record. Recomputed totals: **648 IDs,354 done/294 open,265 exact-covering maintenance rows**. All unrelated original ledger bodies/index rows, archive/sprint, original loader source/tests and dependency pointers are preserved. The runtime and both test files use required CRLF; tracked and untracked change scope is verified. The original spec's frozen intent is unchanged. Story-gitlink validation and git diff --check pass; Git prints its existing CRLF normalization notices without whitespace errors. HEAD is unchanged and nothing is staged.

The isolated task-owned AppHost stops successfully (exit0). The other workspaces' pre-existing AppHosts are left alone. Changes remain uncommitted; Release/package, live-browser and full integration qualification were not run or claimed. Existing release-readiness holds remain unchanged.
