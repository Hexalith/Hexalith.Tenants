# Next bundle selection — 2026-10-16

Selected **DW-337 alone: consistent tracked dispatch ambiguity**, at Tenants `5f41f089157ef077c5145f34a260004cafa5349c`, clean `main`, remote `origin` = `https://github.com/Hexalith/Hexalith.Tenants.git`. This is planning evidence; the [spec](spec-dw-337-tracked-dispatch-ambiguity.md) remains draft pending approval. No production/test source changed. No ledger closure, dependency update, branch/stage/commit/push, nested initialization, deployment or data mutation ran.

The requested maintenance backlog, deferred-work ledger and release-readiness report were inspected, including their historical closure evidence and settled decisions. Root guidance, required Hexalith baseline/UX rules, canonical architecture AD-1–AD-14, build/editor/Git attributes, project context, UI tests and the completed localization spec informed this selection. Existing repository configuration governs maintained validation where older context narratives differ.

## Selection and current source verification

| Candidate | Current finding | Disposition for this plan |
| --- | --- | --- |
| DW-337 | Four tracked configuration/lifecycle methods omit `Retryable == true`, plain `OperationCanceledException` and `TimeoutException` handling supplied by global dispatch. September 6's unified-classifier decision is already settled. | Select: P2 command recovery correctness, Tenants-owned, no external implementation dependency or unresolved decision. |
| DW-285 | Create/Add/Change now guard `_disposed`, `_isSubmitting` and authority resolution before unavailable-state mutation. Completion uses captured snapshot plus attempt/generation publication guards. | Original production defect is stale. Do not implement another guard or close the entry here. |
| DW-470 | Both initial and retry nonaccepted Add/Change paths preserve `result.SafeMessageKey`; display prefers the key. | Original production defect is stale. Do not implement another propagation fix or close the entry here. |
| DW-449 | All five split snapshot types handle pending/retryable waits explicitly and politely; malformed combinations fail closed. | Historical description no longer matches source; separate acceptance reconciliation. |
| DW-331 | Public removal evaluator still omits contradictory complete-scope flag validation; installed loaders already reject the shape. | Valid defensive gap, lower urgency than dispatch ambiguity. |
| DW-469 | Lifecycle/configuration audit mappings still omit retained retry identity; configuration tracker must expose resumed identity. | Valid, separate audit/tracker goal; avoid coupling it to classification. |

The remaining P1 index entries require tool/Git policy work or unresolved authority/revocation decisions. This choice does not assert a new P1 severity or settle DW-625 global recovery.

DW-337 anchors in `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`:

- Set/remove configuration catches at 256/312 classify status codes and task cancellation/HTTP transport only.
- Enable/disable catches at 500/558 delegate gateway errors to lifecycle mapping, which also omits the explicit retryability flag.
- Global grant/remove catches at 369/432 include the full approved ambiguity rule. Reuse this precedence before operation-specific permanent rejection mapping, including a recognizable rejection carrying retryable evidence.
- `TenantCommandSubmissionResult.Ambiguous` produces RequestSent + original MessageId + operation key, with null correlation/rejection/literal and ambiguity true. Keep all four target keys: `Tenants.Configuration.Set.SubmissionEvidence.Ambiguous`, `Tenants.Configuration.Remove.SubmissionEvidence.Ambiguous`, and shared `Tenants.Lifecycle.SubmissionEvidence.Ambiguous` for enable/disable. Global keys remain unchanged. No new localization is needed.
- Untracked wrappers delegate to tracked methods and inherit the fix while retaining existing mint/validation conventions. Create/member/metadata classifiers and status lookup remain outside this bundle.

## Highlighted candidates: preserved findings and proof limits

DW-285 guards are at `CreateTenantFlow.razor:642–649`, `Members/AddTenantMemberFlow.razor:739–746` and `Members/ChangeTenantMemberRoleFlow.razor:793–800` under `src/Hexalith.Tenants.UI/Components/Tenants/`. Captured completion/publication appears at 808/923/978 respectively. `RestoreRetainedAttempt` preserves same-target eligibility transitions and detaches changed targets with a new generation. Existing Add/Change held duplicate-submit tests and `TenantRetainedCommandFlowTests.HeldDeliverySurvivesReplacementBeforeOrAfterCompletion` cover related ownership behavior. A dedicated already-dispatched held gateway + same-target eligibility denial + programmatic resubmit + eventual completion remains a narrower coverage gap, especially for Create. No incident or failing current guard was reproduced.

DW-470 propagation is at Add lines 947/1017 and Change lines 1002/1072; `DisplaySafeMessage` resolves nonblank keys at 489/524. `TryResolveMessageId` can still return keyed-only InvalidTrackingReference, and both shipped resource sets contain it. Retry tests inject this exact failure but assert state/identity/audit rather than its visible explanation. `SupportSafetyBoundaryTests.MemberFlowsRenderRealGatewayRejectionsAndAnnounceLocalizedState` covers real EN/FR member rejection messages with both key and fallback literal. A dedicated two-flow × two-culture × initial/reused-identity keyed-only DOM matrix remains a useful separate proof task. Neither historical ledger block nor its disposition/count changes in this plan.

## Executed planning checks

Exact source build:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
```

Exit 0, 12.96 seconds, zero errors. Two NU1903 warnings remain for pinned `Microsoft.AspNetCore.SignalR.Common` 10.0.12, under the existing advisory policy. No audit flag or dependency changed. Raw session log: `/tmp/tenants-next-bundle-debug-build.log`.

Exact maintained baseline test run:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-restore --no-ansi --no-progress --filter-class '*TenantCommandGatewayTests' --filter-class '*CreateTenantFlowTests' --filter-class '*AddTenantMemberFlowTests' --filter-class '*ChangeTenantMemberRoleFlowTests' --filter-class '*TenantRetainedCommandFlowTests' --filter-class '*SupportSafetyBoundaryTests' --minimum-expected-tests 1 --results-directory /tmp/tenants-next-bundle-baseline --report-xunit-trx --report-xunit-trx-filename baseline.trx
```

Exit 0: **935 passed, zero failed/skipped**, 31.410 seconds. TRX: `/tmp/tenants-next-bundle-baseline/baseline.trx`. This confirms existing covered behavior; it does not close missing negative cases. Configuration/lifecycle transport theory data currently includes only HTTP transport and `TaskCanceledException`; global data includes retryable HTTP400, plain cancellation and timeout.

A temporary reflection console also reused the freshly built test assembly's existing private `CreateLifecycleGateway` and `SubmitOperation` fixtures, without modifying repository source. It selected the retryable HTTP400, plain cancellation and timeout inputs from `AmbiguousGrantExceptions`, used uncanceled caller tokens, and called each of the six tracked operations through the concrete production gateway:

```bash
dotnet run --project /tmp/tenants-next-bundle-probe/Probe.csproj -- /home/administrator/projects/hexalith/tenants/tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll
```

The harness exits 0 after recording observations; this is a diagnostic, not a passing acceptance suite. All six global observations retain RequestSent/ambiguity/original ID. For each of the four target operations, retryable HTTP400 instead returns Failed/nonambiguous, and plain cancellation/timeout escape as their exception types: **12 contradictory observations**. All 18 results, fixture/harness hashes, preserved input hashes and compiled assembly hash are retained in [planning data](dw-337-planning-evidence.json). Temporary harness/log paths are session artifacts; the saved results and source anchors are durable evidence. Promote these inputs to maintained tests after approval.

## Implementation validation

Run the spec's Debug source build, focused gateway matrix and full individual-project UI MTP run, then these maintained Release equivalents:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore --filter-class '*TenantCommandGatewayTests' --minimum-expected-tests 1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-restore --minimum-expected-tests 5532
env TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
env TENANTS_BROWSER_BUILD_CONFIGURATION=Release bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-337-tracked-dispatch-ambiguity.md
git diff --check
```

Expected: relevant focused and full UI tests pass with zero failures/skips; original browser assertions/negative controls and entry-relative pointer check pass. Verify the new matrix exercises every operation/input; a nonempty filter alone does not establish matrix completeness. Full-run floor preserves the current 5,532-case baseline and must include new tests. Maintain the existing FrontComposer source exception in the maintained Release lane. Before production code edits, inspect local Aspire resource health using the owning skill and stop any owned instance by exact target.

Retain separate broad and strict package checks, with exact blockers reported rather than softened flags:

```bash
dotnet build Hexalith.Tenants.slnx --configuration Release -p:UseHexalithProjectReferences=false -warnaserror -m:1 -nr:false
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -p:HexalithFrontComposerFromSource=false -warnaserror -m:1 -nr:false
```

These broad checks, Release tests, browser and runtime checks were **not rerun during planning**. The latest retained readiness evidence records two pinned SignalR NU1903 broad-build errors and seven strict package projection-scope API errors; this plan does not claim current execution of those failures. Fresh implementation validation must distinguish focused success from broad holds and restore/rebuild the maintained lane before using its binaries after a strict-package attempt.

## Preservation and approval boundary

All 643 ledger IDs stay **336 done / 307 open**, with 275 maintenance rows; reconciled counts stay 132 done/219 actionable/15 blocked/26 awaiting-decision/nine accepted limitations/38 uncertain. Current readiness partition remains seven decision holds/112 follow-up/188 qualification or triage. No completed B1–B8 or DW closure is reopened, no stale candidate is silently marked done, and no source index or release-readiness text is rewritten. Archive, sprint and completed localization evidence hashes remain unchanged.

Release remains **not ready**. Preserve historical provenance failures, authority/recovery/governance decisions, raw-secret-scan/CI, upstream nested-layout/provider/startup, current-candidate NFR10/performance, hosted TLS/browser, production authority/revocation, authenticated browser/clipboard/denial and human assistive-technology holds. Focused UI evidence clears none of them.

The spec is complete enough for development with no open design questions. Its `draft` status records pending human approval, as required by the explicit user checkpoint. Stop here; implementation and DW-337 closure await approval.

Final planning checks pass: all 19 recorded input hashes, Tenants HEAD and all nine root dependency heads remain unchanged; tracked and staged diffs are empty. Markdown links/fragments and new-file LF/whitespace checks pass. `git diff --check` exits 0. The exact new-spec gitlink command above exits 0 with `RESULT: PASS` and no reference pointer changes. This entry-relative result does not repair or supersede any historical provenance failure. Only the spec and its Markdown/JSON planning evidence are new.
