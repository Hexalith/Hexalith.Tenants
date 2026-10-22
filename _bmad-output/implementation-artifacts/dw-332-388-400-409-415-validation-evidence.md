# Administrator asynchronous consistency validation — 2026-10-22

Entry revision: `d287099eaba0018e59d2f4474aa36fb9b1a037b5`. Scope: exactly DW-332, DW-388, DW-400, DW-409 and DW-415. The approved intent block remains unchanged. The implementation phase performs no commit, push or remote operation; the parent workflow owns those final steps.

## Changes and controlled interleavings

The page captures action evidence once for rendered grant and removal guidance, including the deferred grid templates. Command admission continues to evaluate live evidence. Removal projection reads use their operation's cancellation token. Subscription adoption rechecks the captured authorization version after assignment and detaches obsolete ownership. Published grant completion synchronizes through exact current authorized lease/command ownership even after the mutation generation expires, then resumes status reconciliation once. A held removal read cannot release a lease from an old confirming basis after the visible snapshot advances.

All probes use the existing component harness and shared admission/subscription services. Signals hold read, setup, delivery-publication and renderer boundaries; timeout bounds only detect missing progress. Render probes delay the next notification render while changing real admission/viewport evidence during localization, inspect the intervening DOM frame, then allow the next render to adopt newer evidence.

| Matrix row | Executed regression methods | Controls |
| --- | --- | --- |
| Render transition | `AsyncConsistencyRenderUsesOneActionEvidenceSnapshot` (4 cases) | Real admission and viewport transitions in both directions; disabled state, localized reason/recovery and both referenced descriptions agree; next render adopts current evidence. |
| Obsolete removal read | `AsyncConsistencyObsoleteRemovalPopulationWalkCancelsWithoutReleasingAttempt` and `AsyncConsistencySubmittedRemovalCancelsItsProjectionRead` (5 cases) | Supersession/disposal, a held second page, actual submitted removal/status token propagation, authorization collapse, retained exact ownership and independent successor cancellation lifetime. |
| Subscription adoption | `AsyncConsistencyAuthorizationCollapseDuringSubscriptionAdoptionDetachesAndRecovers` (1 case) | Sign-out inside the final pre-assignment scope read, obsolete lease disposal, no notification read, authorized resubscription. |
| Published grant completion | `AsyncConsistencyPublishedGrantCompletionRespectsCurrentOwner` (7 cases) | Publication finishes before generation supersession and renderer synchronization; accepted/failed/rejected/ambiguous outcomes, normal/authorization-loss/replacement controls, reauthorization, exact tracking tuples and no redispatch. |
| Advanced removal attempt | `AsyncConsistencyRemovalConfirmationCannotReleaseAnAdvancedSnapshot` (2 cases) | Same-lease advancement keeps unresolved UI and lease together; a fresh status/projection refresh recovers and releases; unchanged current confirming completion also releases. |

## Commands

```bash
aspire run --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire logs security --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --tail 15
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj

dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false

dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --filter-method '*AsyncConsistency*' --minimum-expected-tests 13

dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --filter-method '*RealChromiumFocusValidatorStops*' --minimum-expected-tests 2

dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --filter-class '*GlobalAdministratorsPageTests' --minimum-expected-tests 294 --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/dw-administrator-final-global

dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --minimum-expected-tests 7277 --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/dw-administrator-final-full

python3 _bmad-output/implementation-artifacts/dw-332-388-400-409-415-verify.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-332-388-400-409-415-administrator-async-consistency.md
git diff --check
```

The pre-edit Aspire attempt started the app and `describe` succeeded. Its captured state shows security Running/Unhealthy and Tenants/UI/EventStore resources waiting on security; security logs show successful Keycloak initialization/import. No healthy distributed-runtime baseline is claimed. The task-started app stopped successfully after inspection. One initial log command used unsupported `--resource security` and returned exit 1; the corrected positional-resource command above supplied logs. A sanitized resource-state summary is retained with the raw evidence. The local component work does not require healthy identity-provider transport.

## Initial failures and original-code controls

The initial test-only build failed CS0117 because the fixture used nonexistent `ViewportTier.Mobile`; the supported `Phone` tier corrected it. The first compiled probes also exposed two fixture mistakes: Processing remains Accepted, and a seeded removal must retain its actual preview message ID. These were corrected without changing safety expectations. Before independent review, the original-code replay used the then-current 13 probes and replaced only the page with its entry-revision bytes, built from source, ran the probes, then restored the repaired bytes in `finally`. Its historical driver and tested source are preserved separately from the corrected runnable replay.

That replay fails **exactly 9 of 13 cases; four controls pass, zero skipped**, exit 2. Both render transitions fail coherent disabled-state assertions; all four obsolete-removal read cases fail cancellation observation; the obsolete subscription remains assigned; the superseded published grant stays RequestSent; and same-attempt stale removal confirmation releases its current lease. Normal grant, unauthorized grant, replacement grant and current terminal removal controls pass. Thus the three originally unverified records are reproduced by controlled tests, rather than promoted from source inspection alone.

The first repaired render probes observed no intermediate markup because the follow-up notification render coalesced the fixed output. The final harness holds that next notification until the intervening frame is inspected. A direct snapshot setter also needed an explicit renderer request before asserting its visible state. All 11 initial dedicated repaired cases then pass; two additional actual-submit cancellation cases bring final coverage to 13.

The first complete Global class run executed 294 cases: 288 passed, six failed, zero skipped. Four failures were existing reflection/source-shape consumers of the newly tokenized private method; their signatures/call arguments were updated while preserving their safety assertions. Two unchanged browser subprocess checks exceeded their existing 15-second bounds. The next class run passes 293/294, with only the Chromium-hang bound check timing out. The same two browser checks pass **2/2 unchanged** in the original-page control replay. No browser script, timeout, package or unrelated runtime code changed, and no stable browser defect is claimed.

## Initial acceptance before review

The final Debug source build passes with **zero warnings/errors**, exit 0. The final maintained Global class run passes **294/294**, zero failures/skips, exit 0. Parsed TRX confirms all 13 matrix probes executed and passed.

The first complete maintained UI run executes **7,277 cases: 7,276 pass, one fails, zero skipped**, exit 2, 3m00.273s. Its only failure is the unchanged `RealChromiumFocusValidatorStopsWithinItsBoundWhenChromiumHangs` exceeding the existing 15-second subprocess bound. All 13 matrix probes pass in that complete run as well. The same check passes unchanged in the final Global run. The complete project is rerun with supported runner option `--parallel none` to remove cross-class contention, preserving every test and every timeout.

The complete serialized rerun passes **7,277/7,277**, zero failures/errors/skips/not-executed, **exit 0**, 2m36.031s. Its command is the full maintained command above with `--parallel none` added and results directory `/tmp/dw-administrator-final-full-serialized`. Parsed TRX multisets prove it runs exactly the same 7,277 named cases as the default full run, including all 13 passing matrix probes. The source/assembly SHA-256 manifest matches the unchanged final source build and both complete runs. No timeout, assertion, test selection or repository gate was relaxed.

## Ledger preservation and review

The included verifier reconstructs each selected original record from its `pre-async-consistency-` fields, requires all 652 unrelated record bodies and the preceding narrative unchanged, and checks the exact maintenance-index edits. It passes: 657 unchanged IDs, 520 done, 137 open and 127 rows covering each remaining open ID exactly once. Archive, sprint tracking, release-readiness artifact and dependency pointers remain unchanged.

The ledger verifier, story-gitlink guard and `git diff --check` all pass with exit 0. Git emits the existing CRLF-to-index-LF normalization notices for the two source files, without whitespace errors.

[Raw logs, full TRX reports, baseline replay driver and tested-source/assembly manifest](dw-332-388-400-409-415-raw-logs.zip) preserve initial and final evidence.

## Independent review and final patch validation

All three review layers completed before triage. The verification reviewer found no gaps and the edge-case reviewer found no findings. The blind reviewer supplied ten findings, each recorded separately in the spec's triage log. Two production-risk claims were refuted by the immutable cached localization-pair implementation and the renderer's disposal guards. Eight test/diagnostic improvements were applied without changing the production page: reverse render transitions, terminal/ambiguous publication outcomes, exact status identity, reauthorization/readoption, advanced-removal recovery, authorization-triggered cancellation, complete held-operation cleanup and safe replay-output restoration.

The expanded focused run passes **19/19**, zero failures/skips; its build has zero warnings/errors. Three isolated replay-driver controls cover success, initial-build failure and restoration-build failure. The corrected runnable driver rebuilds restored source in `finally` and propagates failure; the historical driver remains preserved as `replay-administrator-baseline.historical.py`.

Parent validation rebuilds the source with the recorded Debug command: zero warnings/errors, exit 0. Its first 300-case class run passes 299 with one unchanged `OrdinaryRefreshAndProjectionNotificationShareCallerCancellationWithoutCancelingSiblingAuthority` failure at a one-second bUnit wait for notification timer retirement. That method passes unchanged in isolation (1/1), and the subsequent complete class run passes **300/300**, zero failures/skips, exit 0. No assertion or deadline changed.

The exact class command is the recorded per-project command with `--parallel none --minimum-expected-tests 300` and results directory `/tmp/dw-administrator-parent-global-final`. Final full-project validation uses `--parallel none --minimum-expected-tests 7283` and `/tmp/dw-administrator-parent-full`; it passes **7,283/7,283**, zero failures/errors/skips, exit 0, 2m02.594s. Parent TRX inspection confirms all **19** matrix probes pass in both final runs. The final source/assembly manifest and raw reports are archived under `parent/`. Original 294/7,277-case outcomes above remain historical evidence, not claims about the expanded final suite.

The parent rechecked all eight patches, preserved the initial tested source with the historical reports, and reran ledger preservation, story-gitlink and whitespace checks successfully. The exact commit-message file passes the owning repository's pinned `@commitlint/cli` 21.2.3 with exit 0; the message, validator result and its SHA-256 are retained in the raw bundle. Fetching `origin/main` found zero divergence from the entry revision before committing.

No release-readiness or browser/identity-provider transport acceptance is implied by the component regressions.
