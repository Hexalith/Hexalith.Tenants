# Causal correction proof and validation — 2026-10-24

This batch resolves DW-225, DW-372, DW-382, DW-623 and DW-642 against source baseline `c157c394b8fca5f392b5f8a6a5ff1f51f4ac77a0`. Dependencies are unchanged. UI checks use Debug/source references.

## Behavior

Administrator audit proof requires exact correlation and committed aggregate sequence, a complete stable current projection walk, the correct scope/action/target, uniqueness and ordering after the original producer event. It now starts at the original event timestamp and never compares producer time with BFF submission time. The snapshot's second submission-time gate is also removed. A ten-second linked cancellation deadline bounds the complete walk instead of the inherited 50-page cap, allowing older receipts across larger histories. Stalled reads that ignore cancellation are bounded by a cancellable task wait; caller cancellation still propagates. BFF time remains retained attempt metadata. These changes tolerate BFF clocks ahead of and behind EventStore; they do not establish a universal producer-clock monotonicity guarantee.

Member removal already uses an unfiltered audit walk and exact correlation/sequence. Its regression now exercises event times both two hours before and two hours after the UI attempt, requires an absent `From` filter and checks that rendered receipts redact tracking provenance. The older membership timestamp helper remains used by metadata updates; this batch does not claim to change that separate contract.

For administrator corrections, verified `Completed` plus positive event count and unusable committed-sequence proof is a permanent proof failure. Both grant/removal page flows present `UnableToVerify`, retain no false proof link and release the exact owned lease. Intermediate `EventsStored`/`EventsPublished` keep their lease and may later reconcile to a valid completion. Ordinary grant/removal flows retain their existing behavior.

At pinned EventStore `f4df781cb7247c6a5f7549106b90d3748151edba`, normal accepted eventful producers in `AggregateActor.CompleteTerminalAsync` and verified event publication/recovery emit an end sequence. `CommandStatusRecord` and its legacy JSON round-trip test explicitly permit nullable proof. `AggregateActor.CommandRecovery.cs` permits missing proof in terminal advisories and repairs the compatible status; Tenants `TenantCommandGateway` verifies identity independently and strips unusable sequence proof. The regression represents that supported compatibility case; no live corrupt-status incident is claimed.

## Hosted cold-source timing

[Source-reference job 120756122949](https://github.com/Hexalith/Hexalith.Tenants/actions/runs/40055061868/job/120756122949) ran at the exact baseline revision on a fresh Ubuntu hosted runner with no workflow package-cache restore. Its full 266-second duration includes root submodules, source restore/build (133.87 seconds), and all four required provenance tests (4/4, zero failures/skips). This fits the configured 900-second limit and settles duplicate DW-372/DW-382 without changing cache/timeout policy. Package-directory emptiness was not separately instrumented and one timing sample is not a guarantee. Exact commands, step timestamps, source/workflow hashes and public job-log hash are retained in [hosted results](dw-225-372-382-623-642-hosted-source-results.json); a [sanitized public log excerpt](dw-225-372-382-623-642-hosted-log-excerpt.txt) preserves the runner/cache/build/test lines against log expiration. That successful source lane does not qualify the separate package-consumer or Aspire lanes.

## Verification

Final [test results](dw-225-372-382-623-642-test-results.json) record a zero-warning/error Debug/source build, focused **1566/1566** and maintained MTP full UI **7549/7549**, zero failures/skips/not-run. The full run completes in 1m55.896s. Development failures included incorrect nullable theory literal types, a transient first implementation that conflated intermediate event evidence with completion, and a missing fully qualified enum name; these were corrected before acceptance. The saved intermediate-failure XML is qualified separately from final acceptance.

Commands:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*GlobalAdministratorAuditProofLookupTests' -class '*GlobalAdministratorCorrectionSnapshotTests' -class '*GlobalAdministratorsPageTests' -class '*RemoveTenantMemberFlowTests' -class '*GlobalAdministratorCorrectionPanelTests' -class '*TenantQueryGatewayTests' -result-xml /tmp/tenants-proof-final-focused.xml
dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build -p:UseNuGetDeps=false --minimum-expected-tests 7500 --fail-skips on
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-225-372-382-623-642-proof-and-validation.md
```

Aspire inspection before editing found the Tenants/EventStore applications and sidecars healthy; optional graph/vector/security containers were Exited/Unknown. These unit/component checks do not qualify live authentication, full topology health, deployment clock skew, screen-reader behavior or release readiness. No application data, credentials or topology configuration was changed. Broader current CI package-consumer failures remain separate.

## Record integrity

Exactly these five records change disposition. All 657 identifiers, historical fields, decisions and unrelated records remain. The maintenance index exactly covers 72 open records across 67 rows, with 585 done records. Unnumbered prior review follow-ups remain unmodified. No dependency pointer, archive, sprint or release gate changes are part of this batch.

Independent Blind Hunter review returned eight findings, all individually triaged and patched in the spec. No finding was deferred. Retained same-attempt sequence evidence survives compatible later status records; obsolete status callbacks can release only their own terminally unverifiable attempt, and actual replacement ownership remains protected. Both restore and revoke proof paths are exercised.

The first full candidate failed one gateway expectation that still pinned the old submission boundary; its replacement tests the producer boundary and both clock directions. A later pre-final full run passed 7548/7549 but hit `TenantDetailSurfaceTests.EarlyMembershipNudgeDiscardsObsoleteAuthorization` (`change`, `route`): `NullReferenceException` while inspecting a disposed cancellation token in unchanged test code. That failure and the passing final rerun are separately hashed in the JSON. No test was disabled and no unrelated test source was changed.

The final independent recheck caught and resolved one additional superseded-response presentation issue: after exact ownership and identity checks, releasing a terminally unverifiable lease also publishes its `UnableToVerify` snapshot. Tests require terminal copy, cleared in-flight state, and an enabled subsequent action; replacement ownership remains intact. All nine findings are resolved, with none deferred.

The narrow `dotnet format whitespace ... --verify-no-changes` command exits **2** with **100** existing whitespace diagnostics, **zero on changed lines**. Its exact command and output hash are in the JSON. Modified C# files use CRLF. `git diff --check`, the story gitlink guard and ledger/index integrity pass; all 657 IDs and unrelated record bodies are preserved. The exact candidate passes pinned/installed commitlint **21.2.3** with zero problems/warnings. No dependency pointers changed.
