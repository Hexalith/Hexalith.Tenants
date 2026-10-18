---
title: 'DW-634 deterministic disabled-bootstrap registration regression'
type: 'chore'
created: '2026-10-18'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** DW-634 records that the disabled-bootstrap regression waits 50 ms for absent HTTP traffic even though enabled startup schedules command work asynchronously. A late task can escape this negative assertion.

**Approach:** Assert synchronously that disabled bootstrap never obtains the host's ApplicationStarted token for callback registration, retaining the configured administrator and disabled diagnostic checks. Add an enabled-path control that observes token access and waits for actual HTTP delivery after startup. Preserve production behavior and unrelated deferred work.

</frozen-after-approval>

## Implementation Notes

- Investigation: `src/Hexalith.Tenants/Bootstrap/TenantBootstrapHostedService.cs::StartAsync` returns immediately when disabled, before reading ApplicationStopping or ApplicationStarted. Registration is on the CancellationToken struct; an NSubstitute IHostApplicationLifetime with real cancellation sources can prove token non-access without reflection or a new production seam.
- Change `tests/Hexalith.Tenants.Server.Tests/Bootstrap/TenantBootstrapHostedServiceTests.cs::StartAsyncWithDisabledBootstrapPreservesConfiguredAdministrator` to assert no started/stopping token access before and after startup signaling. Use the existing enabled startup-wait test as a positive control with observed lifetime token access and explicit HTTP completion. Remove fixed delays in these two cases.
- Intent gaps: none. Irreversible actions: none. Footprint: one existing test file, this spec, durable validation evidence, and a DW-634-only ledger/index reconciliation. No public API or runtime change.
- Verify an ordinary Debug source build, the maintained bootstrap test class and Server project, plus an isolated compile overlay that retains the disabled log but removes the early return. The disabled regression must fail on token access. Rebuild ordinary source before final validation.
- Entry revision: `20293a86babf337299639770efb3209aac9f192e`. Initial VCS check was clean on main; external dependency submodule movements appeared during investigation and are outside this change. Preserve them.
- An existing Aspire AppHost was already running. Read-only describe/telemetry checks succeed; tenants, API, UI, security and EventStore report healthy. Do not restart or stop that existing session.
- Implemented the disabled test with substituted lifetime tokens and synchronous non-access assertions before/after startup signaling; it retains configuration and log checks. The enabled control observes both token getters, awaits an explicit HTTP completion signal and asserts startup had occurred at send time. Both fixed 50 ms delays are removed; no production source changed.
- Initial ordinary Debug source builds pass with zero warnings/errors. The initial maintained bootstrap class passes 85/85 with zero failures/skips. Final controls and review are pending.
- Concurrent external `/pushall` work advanced HEAD through `8c1e1aa5` to `132630d14e54981ef7e99bfe15ce70fd887371c3` while dependencies were refreshed; no affected work is staged, reverted or attributed to this item.
- Final ordinary Debug source build: zero warnings/errors. Final maintained bootstrap: 85/85; full Server: 1316/1316; zero failures/skips. The missing-disabled-return overlay builds cleanly and produces exactly one expected ApplicationStarted non-access assertion failure (84 passed), including a passing enabled control. Ordinary source was rebuilt before final runs.
- External merge precursor canonical identifier: `8c1e1aa5fe579884477ebfe1454b95bc0a5815fc`. Ledger/index source content remains identical to entry before this item’s edits. Only DW-634 is reconciled; original values are preserved under pre-dw-634-* and all release holds remain.
- Review corrections: the enabled control now awaits observed scope-disposal entry before tearing down the real scope, asserts one HTTP request and checks the successful diagnostic. Its name and evidence describe observed lifetime access and send ordering; this does not claim deterministic proof against every possible eager-scheduling implementation. The disabled test's synchronous token non-access proof remains unchanged. Closure banners explicitly name ApplicationStarted/ApplicationStopping. Mutation reproduction will use a finally-based normal rebuild.
- Post-review final verification: finally-based mutation/restoration wrapper exits 0 with both builds at zero warnings/errors; the only mutation failure is the disabled test (84 pass / 1 expected failure / 0 skips). Normal-source bootstrap 85/85 and full Server 1316/1316 pass with zero failures/skips. Preservation/diff checks pass. All six review findings are classified below; no new deferral remains.
- Independent correction recheck reports no remaining material problem, verifies all 15 retained artifact hashes and the documented wrapper/preservation evidence, and independently reruns the bootstrap class at 85/85 with zero failures/skips. Earlier pending-control/review notes are historical and are resolved by these final results. No configured review layer was skipped and no finding is deferred.

## Review Triage Log

- low — patched: the previous enabled control's incomplete-task observation could miss an eager worker scheduled late. Removed that observation, named the test for its measured behavior and explicitly limited the evidence to observed lifetime access/post-startup send ordering. DW-634's disabled token non-access assertion is synchronous and is not subject to that scheduling gap. No universal eager-dispatch proof is claimed.
- low — patched: SetResult could hide a duplicate request behind production exception handling. The enabled control now uses TrySetResult, waits for observed scope disposal, asserts exactly one request and checks the successful diagnostic. This covers the single observed operation; it does not claim an arbitrary future-work quiescence guarantee.
- low — patched: HTTP entry preceded response processing/scope cleanup. The enabled control now signals at the observed scope's DisposeAsync, then checks success/count before the test disposes the real scope, keeping its dependencies alive through production dependency use.
- low — patched: the closure banners overstated generic lifetime-token coverage. Both now explicitly name ApplicationStarted and ApplicationStopping; ApplicationStopped is not part of this assertion.
- medium — patched: a standalone mutation build could leave normal output folders mutated after an unexpected failure. The executable reproduction now restores normal source compilation in finally and validates expected exit/count/failure identity. A forced process kill remains outside finally guarantees; the evidence directs a normal rebuild if restoration was interrupted.
- low — rejected: raw logs under /tmp expire, but the committed evidence already retains compact counts, UTC times, exit results, build summaries, failure identity/diagnostic, hashes and reproduction commands. Full raw-log retention adds repository artifacts without improving this small change's acceptance evidence; no durable raw-log store is claimed.
