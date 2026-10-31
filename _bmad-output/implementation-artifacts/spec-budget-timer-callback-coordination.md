---
title: 'Coordinate the budget timer fatal-provider test'
type: 'bugfix'
created: '2026-10-31'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '5c7b64a0aaef330bb37ea6da8dea593ea70e4628'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Tenants CI run `41183729066`, job `124486561933`, fails the fatal row of the budget timer test while awaiting its cancellation callback. The provider returns an already-faulted task immediately after cancellation is queued, allowing disposal of the linked token source to remove its registration before that callback starts.

**Approach:** Coordinate callback entry before the synchronous fatal provider returns, retaining the actual throwing callback and original fatal-exception identity assertions. Preserve production timeout/cancellation/fatal behavior, the existing five-second bounds, and pending-provider coverage. Do not commit or push.

</frozen-after-approval>

## Implementation Notes

### File Scope

- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.Deadlines.cs`
- `_bmad-output/implementation-artifacts/spec-budget-timer-callback-coordination.md`

- Owner starts clean on `main` at the full baseline above. Nonrecursive origin fetch confirms no divergence. Root/submodule dependencies and concurrent owner changes remain outside this scope.
- Read owning instructions, root-declared shared baseline, `.editorconfig`, `.gitattributes`, build/test settings, gateway deadline implementation, deterministic clock, and architecture AD-10 read-safety supersession. No nested submodule initialization or skills under `references/`.
- Exact hosted failure is `TimeoutException` at `TenantQueryGatewayTests.Deadlines.cs:63`, waiting for callback entry, after exactly five seconds; log: `/tmp/tenants-budget-timer-ci-job.log`. Cancellation callbacks are asynchronous whereas the fatal load can complete synchronously; their execution after token-source disposal is not a gateway guarantee.
- Initial isolated Release package build passed with zero warnings/errors; original targeted theory passed both rows locally, consistent with the hosted scheduling race. Logs: `/tmp/tenants-budget-timer-baseline-build.log` and `/tmp/tenants-budget-timer-baseline-test.log`.
- No live Aspire start or shared producer build is attempted during the parent's runtime work. Parent explicitly restricts validation to isolated UI.Tests; this change only coordinates a test fixture.
- Added a disposable callback-entry event in the existing two-row theory. The fatal handler waits for that signal before returning the already-faulted task, so its registration remains live until the timer actually invokes it. The callback still throws, the nonfatal pending row still expires, and the original fatal wrapper must still propagate unchanged. Existing five-second bounds and LF line endings are preserved.
- Verification: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:HexalithFrontComposerFromSource=false -warnaserror -m:1 -nr:false` passed with zero warnings/errors; `/tmp/tenants-budget-timer-fixed-build.log`.
- Verification: `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests.dll -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests -noLogo` passed 841/841, including deadline expiry, caller cancellation, late-provider retirement and fatal-wrapper cases; `/tmp/tenants-budget-timer-gateway-tests.log`.
- Verification: the same built assembly with `-method Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests.BudgetTimerObservesThrowingProviderCallbacksAndPreservesCompletedFatal -noLogo` passed 30 fresh-process iterations / 60 theory rows; `/tmp/tenants-budget-timer-repeated-tests.log`.
- Verification: `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-budget-timer-callback-coordination.md` and `git diff --check` passed. No nested initialization, production edits, dependency changes, commits or pushes.
- Independent review additionally prompted assertion ordering that exposes fixture-entry failures before waiting on the callback again, plus the existing five-second bound on the fatal load assertion. Verification above is rerun against that final patch.

## Review Triage Log

- Low, patched: awaiting callback first could mask the new entry assertion with a second timeout. The fatal load is now asserted before the callback wait, so a missing callback fails through the load assertion promptly.
- Low, patched: the pre-existing fatal load assertion had no timeout. It now uses the existing five-second bound, preserving fatal-wrapper identity checking and failing a stalled completion.
- Low, rejected as broader pre-existing coverage work: the signals prove callback execution and fatal preservation, not direct observation of the internal `CancelAsync` task fault. This bounded fixture fix retains the throwing callback and existing production fault observer. Direct fault-observation instrumentation would expand production scope; no such verification claim is made here.
