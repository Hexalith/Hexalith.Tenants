# Command recovery verification: DW-253 / DW-261 / DW-284 / DW-475 / DW-512

Date: 2026-10-22. Entry revision: `772fd4d8fb995c95efea5288edcc9d06b6cfe5f0`.

## Scope and behavior

The metadata and removal fixtures submit through typed gateway substitutes with null, empty, whitespace and valid correlation values. They invoke rendered submission/recovery controls and check exact identity, original intent/baseline, retained admission, activity and absence of invalid status lookups or redispatch. The old metadata resubmit test's private snapshot injection is replaced; separate tests for renderer evidence corruption remain. Create gateway cases omit the optional message ID, inject four indeterminate transport failures, and prove the minted ULID is reused with an identical payload on retry. Existing update-identity and lease-denial regressions remain unchanged and run in the focused classes.

The create remount test uses the production module registration and checks scoped lifetimes and shared tracker identity. It dispatches the first rendered component, removes it through the parent's ChildContent, verifies actual renderer removal, then inserts a distinct replacement within the same service scope. It qualifies changed-draft refusal, matching original-attempt recovery, original intent/baseline/identities, projection confirmation and independent-circuit isolation. It does not manually seed a tracker.

The initial fourteen audit cases, expanded after review to twenty-one, drive seven real command components through gateway submission and verified PublishFailed status. Denied reads remove the entire Inspect-audit recovery shell and entry point; allowed controls expose an enabled scoped audit link, then lose it on same-component revocation without another mutation/status lookup. Indeterminate access retains a disabled control with no href. Degraded lifecycle intentionally offers no audit refresh; the other six components do. Only the denial-specific source-string assertion is removed from the existing conformance test.

Correction completion now renders the value atomically retained by EndDelivery. Its status-read update path remains unchanged. The old completion helper's second candidate update could conceal an incorrect EndDelivery argument. Five new cases qualify accepted, ambiguous, failed, rejected and missing-after-capture gateway results, including retained identities, retry/admission and no premature proof. Before releasing a held automatic status read, the accepted case pins the retained delivery fields and rendered snapshot; final per-outcome assertions also pin audit/message/rejection/focus/announcement values. Existing tracker atomic-outcome tests remain in the focused run. No prior duplicate dispatch is claimed to have been reproduced. Ambiguous delivery preserves the existing RequestSent policy.

This is component/gateway verification, not authenticated-browser, distributed-delivery or deployment acceptance. Dependencies, release holds, unrelated deferred records and the existing local `.mcp.json` edit remain outside the commit.

## Baseline and commands

Aspire CLI inspection of the already-running AppHost reported all 23 resources Running/Healthy:

```bash
aspire ps --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
```

Raw state is only in `/tmp/tenants-command-recovery-aspire-{ps,describe}.json`; no host was restarted or edited.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*CorrectionStartPanelTests' -class '*CommandFlowAuditDenialTests' -class '*CreateTenantFlowTests' -class '*EditTenantMetadataFlowTests' -class '*RemoveTenantMemberFlowTests' -class '*TenantCommandGatewayTests' -class '*TenantCorrectionAttemptTrackerTests' -class '*AuditAvailabilityStateTests' -result-xml /tmp/tenants-command-recovery-focused-final.xml
```

The initial two correction-fixture failures exposed incorrect assumptions: Ambiguous returns RequestSent, and missing gateway support must disappear after initial availability checks. Both fixtures were corrected; the correction class then passed 123/123. The initial audit run passed 12/14, with both lifecycle cases failing an incorrect blanket refresh assertion; that assertion now follows the existing lifecycle policy. The first combined build passed with zero warnings/errors; its focused run passed 1362/1364, with only the initial remount fixture failing because direct component-wrapper disposal did not establish renderer removal. The fixture now removes/restores parent ChildContent. Two subsequent fixture rechecks caught missing required CascadingValue parameters on parent rerender and inspection of an already-disposed wrapper; supplying Value/Name on each render and saving the original instance before disposal corrected those errors. The dedicated real-remount cases then passed 2/2. These initial failures are retained as fixture development evidence, not application defects or release blockers.

The direct assembly runner prints help but exits 2 for `--help`; its documented single-dash `-class`, `-method` and `-result-xml` arguments were used for actual runs. No solution-wide test command was used.

## Deliberately wrong controls

Each control built successfully in Debug source mode (`--no-restore`) and ran the built assembly. Original production bytes were restored in `finally` before the final build.

| Temporary source change | Exact test filter | Result |
| --- | --- | --- |
| EndDelivery's normal `next` argument replaced by `request` | `-method '*DeliveryCompletionRendersTheAtomicallyRetainedOutcome'` | 4/4 fail as required |
| EndDelivery's `unavailable` argument replaced by `attempt.Snapshot` | `-method '*GatewayLostAfterCaptureRendersTheAtomicallyRetainedUnavailableOutcome'` | 1/1 fails as required |
| All seven existing-tenant `AuditReadDenied ? null : CommandAuditEntryPointTemplate` conditions bypassed | `-class '*CommandFlowAuditDenialTests'` | 14/21 fail: 7 initial-denial and 7 mounted-revocation cases; all 7 indeterminate controls pass |

The temporary runners were `/tmp/tenants-command-recovery-mutations.py`, `/tmp/tenants-command-recovery-audit-mutation.py` and `/tmp/tenants-command-recovery-tracking-mutation.py`. The tracking runner removes the complete UnableToVerify/Degraded submission guard between its `if` and the next `RemoveUserFromTenant request` declaration; its `-method '*UncertainStateWithoutTrackingRemainsFailClosed'` run rejects all 6/6 cases. Their modifications are fully specified above; they invoke the same build/assembly commands with the listed filters. Raw logs/XML use the `/tmp/tenants-command-recovery-{wrong-next,wrong-unavailable,audit-bypass}` prefix. No mutation remains in the worktree.

## Commit validation

Exact full candidate (one trailing newline):

```text
test(ui): qualify five deferred command recovery records
```

The lockfile-installed `@commitlint/cli` 21.2.3 was run with the owning repository configuration:

```bash
node_modules/.bin/commitlint --verbose < /tmp/tenants-command-recovery-commit-message.txt
```

Complete successful output:

```text
⧗   --- input ---
test(ui): qualify five deferred command recovery records
✔   found 0 problems, 0 warnings
```

## Pre-review acceptance and conservation

Final focused run: **1364/1364 passed**, zero failures, errors, skips or not-run cases (6.234 seconds). The restored Debug source build passed with zero warnings/errors. The full maintained UI suite passed **7151/7151**, zero failures, errors, skips or not-run cases (97.858 seconds). Commands and counters, XML hashes and the common accepted assembly identity are retained in `dw-253-261-284-475-512-run-manifest.json`. Both final runs used that same restored assembly without a build between them.

Full-suite command:

```bash
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -result-xml /tmp/tenants-command-recovery-full.xml
```

The standalone `dw-253-261-284-475-512-verify.py` checker compares against the full entry revision. Exactly five open records close; all original selected fields are preserved, using `pre-command-recovery-verification-` for superseded fields. All 652 unrelated record bodies and unrelated maintenance rows remain unchanged. All 657 IDs remain: **480 done / 177 open**, with exact open-record coverage across **167 maintenance rows**. Three duplicate-heading and two invalid-current-field controls are rejected. Dependency gitlinks and the SHA-256 of the pre-existing local MCP edit are unchanged. Archive, sprint status and release holds remain unchanged.

```bash
python3 _bmad-output/implementation-artifacts/dw-253-261-284-475-512-verify.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-253-261-284-475-512-command-recovery-verification.md
git diff --check
```

The first story gitlink check rejected the new spec because its baseline_commit frontmatter was missing. The full entry revision above is now recorded verbatim in frontmatter; the corrected check passes without a dependency-pointer change.

Review qualification: DW-261 combines one newly filled create minted-identity gap with existing update/lease/orphan-state coverage. The new gateway-driven removal cases reach owned-attempt duplicate prevention; they do not reach the separate TrackingLost block. The six existing `UncertainStateWithoutTrackingRemainsFailClosed` cases are present at the entry revision (introduced by `e667d5391dae306e631dec23acd191863e54130c`) and deliberately inject orphaned defensive state. No production path into that orphan state is established here.

The conservation checker defaults to committed evidence only, so a clean historical checkout can run it. During this work, `--verify-local-edit` additionally verifies the uncommitted original MCP edit. Both invocation modes pass locally; the run manifest retains that exact edit digest.

Review also added a distinct second-scope admission gate and independent acquisition/release checks. The existing `TenantRetainedCommandFlowTests.HeldDeliverySurvivesReplacementBeforeOrAfterCompletion` create variants qualify outstanding delivery completed before/after adoption; those tests complement the new real renderer-unmount and production-registration checks. The reviewed focused run includes that entire class. Its first run passed 1577/1578; the single failure corrected an expected post-status pending message/focus (distinct from the separately pinned Accepted delivery fields), without changing application behavior.

The checker now compares historical closure summaries and the complete maintenance narrative as well as record bodies/rows, rejecting two historical-summary deletion controls. The TrackingLost block removal control builds and fails all six existing defensive cases, and production bytes are restored. The exact normal-command filters are retained below for final reviewed acceptance.

## Independent review

All ten findings and individual verdicts are recorded in the spec's Review Triage Log. Nine corrections are implemented; the held-create-delivery finding is refuted by the complementary existing create variants, which are included in final focused acceptance. No new deferred record is introduced. This review does not lift browser, deployment, authorization-model or release holds.

## Final reviewed acceptance commands

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*CorrectionStartPanelTests' -class '*CommandFlowAuditDenialTests' -class '*CreateTenantFlowTests' -class '*EditTenantMetadataFlowTests' -class '*RemoveTenantMemberFlowTests' -class '*TenantCommandGatewayTests' -class '*TenantCorrectionAttemptTrackerTests' -class '*AuditAvailabilityStateTests' -class '*TenantRetainedCommandFlowTests' -result-xml /tmp/tenants-command-recovery-reviewed-final-focused.xml
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -result-xml /tmp/tenants-command-recovery-reviewed-final-full.xml
python3 _bmad-output/implementation-artifacts/dw-253-261-284-475-512-verify.py --verify-local-edit
```

The first reviewed full-suite run exposed a fixture synchronization error in the held Accepted test: its bUnit WaitForAssertion checked once and received no further render while status was intentionally held. The failure report showed one status handle had arrived after that initial check. The fixture now awaits an explicit status-entry TaskCompletionSource signal, then inspects the delivery outcome before releasing status. This changes only test synchronization; it does not increase render timeouts or alter application behavior. The failed reviewed run remains in the manifest alongside the final recheck.

Final reviewed acceptance: **1578/1578 focused** (28.367 seconds) and **7158/7158 full UI** (104.575 seconds), zero failures, errors, skips or not-run tests. The restored Debug source build has zero warnings/errors. Both runs use the same final assembly without an intervening build. The manifest distinguishes this accepted source/assembly identity from pre-review acceptance and initial failing reviewed fixture runs. Independent code/checker recheck accepts all corrections with no remaining concrete findings; no work is newly deferred.

Final evidence-only independent recheck confirms current source, assembly, build-log and XML hashes, exact commands/counters, historical-run separation and done spec status. No remaining findings.
