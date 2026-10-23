# Bootstrap compatibility and administrator handoff validation

Date: 2026-10-23. Entry: `870d677cbac5bb54d8a542a289421da8a9243b53` on `main`.
Spec: [bootstrap handoff](spec-dw-551-556-575-576-597-bootstrap-handoff.md); its frontmatter context list is empty. This record covers implementation verification before the parent build workflow's independent review and Git publication steps.

## Behavior and scope

`EventStore:Authentication:TokenEndpoint` selects an explicitly operator-trusted credential destination. Only an absent override retains the Keycloak endpoint. Invalid/blank/relative/non-HTTP(S)/userinfo/query/fragment and insecure non-Development destinations fail closed; an override cannot bypass authority validation or activate symmetric token fallback. The existing credential, exact delegated-subject, bounded-response, timeout/cancellation and support-safe diagnostic protections remain. The aggregate's authenticated actor/target binding is unchanged.

The hosted service retains one attempt after application startup. Its accepted, unexpected, failure and already-bootstrapped diagnostics direct operators to status and projected-membership verification. The guides document deliberate recovery with the same subject and original correlation identity, and distinguish confirmed non-dispatch from ambiguous delivery. A restart is an explicit new attempt, not automatic failure recovery.

The UI recognizes exact `system` membership among individually valid tenant claims. Resolver/reflection/policy tests cover duplicate values, wrong case, absent system membership, malformed whitespace/control values, multiple identities and independently corroborated subjects, including the shipped `system`/`tenant-a`/`tenant-b` principal. Unrelated authorization and revocation policy is unchanged.

The quickstart removes the wrong-subject bootstrap-write probe. Its operator-selected secret parameters avoid extracting generated credentials; it authenticates the fixed configured subject, pages projected administrator membership, grants a provisioned human using `SetGlobalAdministrator`, waits for command completion and projected membership, then authenticates the human. Mutation functions retain attempt IDs in the interactive shell after uncertainty. Existing persistent Keycloak imports do not update existing credentials; the guides explicitly require identity repair and prohibit state deletion or subject substitution.

## Build and maintained test evidence

Commands ran from the repository root with the existing dependency checkouts. No dependency or submodule update occurred.

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1 > /tmp/bootstrap-server-build.log 2>&1
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1 > /tmp/bootstrap-ui-build.log 2>&1
dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class '*TenantBootstrapHostedServiceTests' -class '*QuickstartDocumentationTests' -noLogo > /tmp/bootstrap-final-focused.log 2>&1
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantConfigurationReadPolicyTests' -noLogo > /tmp/bootstrap-ui-focused.log 2>&1
dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -noLogo > /tmp/bootstrap-server-full.log 2>&1
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -noLogo > /tmp/bootstrap-ui-full.log 2>&1
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -method '*OrdinaryRefreshAndProjectionNotificationShareCallerCancellationWithoutCancelingSiblingAuthority' -noLogo > /tmp/bootstrap-ui-rerun.log 2>&1
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -noLogo -parallelMode none > /tmp/bootstrap-ui-full-serial.log 2>&1
```

Both Debug source builds exit 0 with zero warnings/errors. Final focused Server bootstrap/documentation: **125 passed, zero failed/skipped** (118 bootstrap, seven documentation). Focused UI claim/policy: **94 passed, zero failed/skipped**. Final full Server: **1,474 passed, zero failed/skipped**, 3.851 seconds. Final serialized full UI: **7,448 passed, zero failed/skipped**, 145.685 seconds.

Initial failures remain distinguished from final results:

- The first full Server run passed 1,470/1,471; its quickstart test still required the old “generated for each run” text. Updating that obsolete assertion to the operator-override contract passed focused documentation 7/7 and full Server 1,471/1,471. Three further Development no-authority/fallback controls then brought final Server to 1,474/1,474.
- The first default-parallel UI run passed **7,447/7,448**, zero skips, 136.982 seconds. `GlobalAdministratorsPageTests.OrdinaryRefreshAndProjectionNotificationShareCallerCancellationWithoutCancelingSiblingAuthority` timed out at `GlobalAdministratorsPageTests.FiniteBounds.cs:381`: expected `clock.PendingTimerCount=0`, observed 2. Its isolated rerun passed 1/1; the final entire serialized suite passed. The initial failure is retained in `/tmp/bootstrap-ui-full.log`. No production page/timer code or that fixture was changed. These reruns establish a passing final lane; an unchanged-baseline reproduction was not performed, so they do not independently establish the initial failure's cause.

## Executable documentation and preservation checks

The tracked [documentation checker](dw-551-556-575-576-597-doc-check.py) executes locally without contacting an IdP or submitting a live command:

```bash
python3 _bmad-output/implementation-artifacts/dw-551-556-575-576-597-doc-check.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-551-556-575-576-597-bootstrap-handoff.md
git -c core.safecrlf=false diff --check
```

All exit 0. The checker validates **28 Bash fences**, **five JSON fences**, **two embedded Python programs**, and **23 local links**. Ten helper fixtures exercise paged exact membership, wrong case/missing membership, HTTP and malformed-response failures, invalid cursors, completed/rejected/pending/unavailable status, plus a generated ULID format check. Ten additional fixtures execute the documented human-grant/bootstrap-recovery functions and inspect exact payload scope/target/IDs: each submits once; status/projection success completes; a 202 followed by transport failure preserves IDs and stops; rejected/pending status stops before projection; absent projected membership prevents completion. All fixture responses and identities are local test data. No real credentials or tokens are used or printed.

The same checker independently compares the ledger/index against the exact entry baseline. [Machine-readable accounting](dw-551-556-575-576-597-ledger-validation.json) records **657 retained IDs, 550 done, 107 open, 97 exact-covering maintenance rows**. Exactly DW-551, DW-556, DW-575, DW-576 and DW-597 change from open to done. Every original selected field is retained unchanged or under `pre-bootstrap-handoff-*`; all unrelated ledger records and index rows remain identical. Original historical summaries, dependency fields, archive, sprint tracking, revocation records and release holds are preserved. No references pointer movement occurs.

## Evidence limits

The approved spec records the prior Aspire baseline: resources Running/Healthy while bootstrap telemetry returned HTTP 400. This implementation did not obtain generated credentials, perform live token acquisition, submit a live grant/recovery, or change AppHost wiring. Passing deterministic tests and executable documentation fixtures do not establish live IdP provisioning or distributed handoff success; operators must run the documented status and projected-membership checks. The existing revocation limitation remains open. Independent review, final commitlint/divergence validation, commit and authorized push are subsequent parent workflow steps, not results claimed here.

## Review patch qualification — 2026-10-23

The following supersedes the initial shell-checker layout above; earlier execution results remain historical evidence. Reusable shell checks now live in `tests/scripts/test_bootstrap_handoff.py` and run from the existing `story-guards.yml` workflow. The artifact checker now performs only historical ledger/index preservation. Numbered records terminate at actual Markdown headings, so a later unnumbered follow-up is excluded from the final numbered record; an in-memory appended-section control qualifies that boundary. The exact-five accounting remains unchanged.

The quickstart has one credential-aware AppHost launch. All shared helpers precede the initial membership check, which keeps the authenticated shell open on failure. Actual API and token calls have connection/request deadlines; status and projection phases share elapsed budgets across requests/pages. The membership helper requires canonical projection-backed provenance, qualifying current freshness and a nonempty consistent version across every page, rejecting stale/degraded/malformed/missing or mixed-version evidence. It follows the existing REST producer/client treatment of the optional degraded header: ordinary current projections may omit it, while an explicit malformed/degraded value refuses verification. This does not change the existing revocation policy.

New mutation attempts refuse any retained message/correlation handle before overwriting IDs or target. Human grants recheck configured identity and qualifying administrator membership; bootstrap additionally requires operator-confirmed unbootstrapped state. Read-only `resume_handoff`/`resume_bootstrap` query the same retained handles after uncertainty. Human authentication checks `HANDOFF_PROJECTED` before prompts or requests. `PublishFailed` explicitly stops unresolved, preserving IDs because events already persisted; the guides defer publication recovery to existing EventStore policy and prohibit blind replacement.

The hosted service emits support-safe event `2008` immediately before dispatch with the generated `MessageId` and `CorrelationId` ULIDs. No administrator, token or credential values are included. The new ambiguous-delivery test observes the event inside the attempted HTTP call, compares both structured values with the serialized command, validates ULID shape and verifies support-safe diagnostics after response loss. The recovery guide uses this attempt event to recover startup handles and distinguishes an attempt from proven delivery.

Only affected tests were run for this patch:

```bash
python3 tests/scripts/test_bootstrap_handoff.py > /tmp/bootstrap-handoff-patch-python-final.log 2>&1
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true --no-restore -m:1 > /tmp/bootstrap-patch-build.log 2>&1
dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class '*TenantBootstrapHostedServiceTests' -class '*QuickstartDocumentationTests' -noLogo > /tmp/bootstrap-patch-focused-final.log 2>&1
dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class '*QuickstartDocumentationTests' -noLogo > /tmp/bootstrap-patch-doc-final.log 2>&1
python3 _bmad-output/implementation-artifacts/dw-551-556-575-576-597-doc-check.py
git -c core.safecrlf=false diff --check
```

All final commands exit 0. The Debug source build has zero warnings/errors. Focused bootstrap/documentation tests pass **126/126**, zero failures/skips; the documentation-only final recheck passes **7/7**. The maintained Python suite passes **18 tests** with additional parameterized cases, in 27.929 seconds. It executes the actual documented `platform_api`, `request_token`, `token_subject`, descriptor-based credentials and mutations against loopback dummy HTTP responses. It qualifies canonical/legacy current metadata, absent optional degraded metadata, stale/degraded/missing/duplicate/mixed-version refusals, paginated exact membership, malformed payloads, HTTP failures, invalid cursors, repeated unresolved invocation, preserved IDs/target, startup-event handle recovery, `PublishFailed`, pending/rejected/unknown status, absent projection and failed-grant-to-human-auth sequencing. Local servers that send headers but stall their bodies exercise the actual API/token/status/projection timeout paths; each one-second fixture budget returns in under 3.5 seconds. Fixture tokens have inert non-authenticating headers/signatures; no live IdP credentials or domain mutations are used.

Initial patch failures are retained separately: `/tmp/bootstrap-patch-focused.log` records a 125/126 result because scope disposal preceded the outer ambiguous-delivery failure diagnostic. The one-attempt fixture now waits for that expected diagnostic before asserting, preserving the request-count assertion; final focused execution passes. `/tmp/bootstrap-handoff-patch-python.log` records a paged positive fixture exhausting its one-second ordinary test budget during a concurrent source build (15/16). Ordinary fixture budgets are now three seconds, while dedicated stalled-response controls retain their one-second budget. The final maintained lane passes all 18 tests. No full Server/UI suite was rerun by the patch session; final broad verification remains with the parent workflow.

## Final parent verification and review

All actionable review findings are qualified. The general reviewer independently confirmed the nine original actionable fixes and passed the actual loopback suite and startup-ID case. The verification reviewer confirmed CI execution and actual transport coverage, then identified the missing direct bootstrap-projection negative. `test_bootstrap_completed_without_projection_stays_unresolved` now asserts failure after Completed with no projected administrator, false completion state, unchanged retained IDs and exactly one POST; a replacement attempt is refused. Removing only the projection wait from extracted helpers in memory makes that test fail as expected, without editing tracked source. The pre-existing handler redirect-policy question remains unverified in the appended, unnumbered ledger follow-up; no redirect policy changed.

```bash
dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -noLogo > /tmp/bootstrap-parent-final-server-full.log 2>&1
python3 tests/scripts/test_bootstrap_handoff.py > /tmp/bootstrap-parent-final-python-19.log 2>&1
python3 _bmad-output/implementation-artifacts/dw-551-556-575-576-597-doc-check.py
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-551-556-575-576-597-bootstrap-handoff.md
node_modules/.bin/commitlint --edit /tmp/tenants-bootstrap-commit-message.txt --verbose
```

All exit 0: final full Server **1,475/1,475**, zero failures/skips, 3.753 seconds; maintained shell/HTTP **19/19**, 27.840 seconds; exact-five preservation and gitlinks pass. The in-memory expected-failure control is retained in `/tmp/bootstrap-parent-projection-guard-control.log`. The earlier complete serialized UI lane remains **7,448/7,448**; neither its source/test changes nor its dependency graph changed during the bootstrap/docs review patches, so it was not repeated. The initial parallel timer-cleanup failure remains recorded above without a claimed cause.

Pinned commitlint is **21.2.3**, matching package-lock; the exact complete message file passes with zero problems/warnings, with successful evidence in `/tmp/tenants-bootstrap-commitlint.log`. Fetch confirmed main and origin/main at the same full entry revision before committing. No dependency or submodule pointer changed. The parent-started Aspire AppHost stopped successfully after inspection. Live IdP provisioning and grant/recovery execution remain operator checks; the fixture responses are dummy local data.
