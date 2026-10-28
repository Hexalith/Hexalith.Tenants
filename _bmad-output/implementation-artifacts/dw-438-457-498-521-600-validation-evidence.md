# DW-438 / DW-457 / DW-498 / DW-521 / DW-600 validation

The user approved the four proposed policy changes on 2026-10-28 and retained
the five-item batch and explicit main push request. The approved intent and
individual review findings are preserved in
[the spec](spec-dw-438-457-498-521-600-deferred-policies.md).

## Source identity and limits

Implementation began at Tenants
`983ba9d51027a287db0efa6860058b56dcb8ff44`, EventStore
`717a06e16c3afd81b904eaf4de646b74b13262d2` and Builds
`2574bf2f3da5367d8801f0d7661d66155e5d90b0`.

This batch qualifies source changes and their tests. It does not qualify a new
published EventStore package, package-mode adoption, deployment, complete hosted
CI, or screen-reader/browser behavior. Existing runtime/release holds remain.
No migration, manual package publication, external contact or cross-caller
attempt handoff is performed.

## Initial implementation checks

Commands below run from the named owning repository. Output logs retain actual
results; later correction runs are recorded separately rather than replacing
historical failures.

| Repository | Command | Result | Retained log |
| --- | --- | --- | --- |
| Tenants | `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true` | Zero warnings/errors | `tenants-ui-build.log` |
| Tenants | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll` | 7,872 passed; zero failed/errors/skipped/not-run; 101.342 seconds | `tenants-ui-full.log` |
| Tenants | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantAuditSupportSafetyTests' -class '*GlobalAdministratorsProjectionLoaderTests' -class '*TenantQueryGatewayTests' -class '*TenantsBffCompositionTests' -class '*CorrectionStartPanelTests' -class '*TenantAuditNavigationSafetyTests' -class '*SupportSafeCopyButtonTests'` | Final focused 1,309 passed; zero failed/errors/skipped/not-run | `tenants-ui-focused-final.log` |
| EventStore | `dotnet build tests/Hexalith.EventStore.Server.Tests/Hexalith.EventStore.Server.Tests.csproj --configuration Debug -m:1 -p:UseHexalithProjectReferences=true` | Zero warnings/errors | `eventstore-build.log` |
| EventStore | `dotnet tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll -class 'Hexalith.EventStore.Server.Tests.Commands.*'` | Final post-cleanup 239 passed; zero failed/errors/skipped/not-run | `eventstore-command-suite.log` |
| Builds | `python3 Tools/validate-runtime-toolchain-disposition.py` | Exit 0; superseded, unqualified SDK split and release holds retained | `builds-disposition-validation.log` |
| Builds | `python3 Tools/test-runtime-toolchain-disposition.py` | 3 tests; 21 negative controls | `builds-disposition-tests.log` |
| Builds | `python3 Tools/test-runtime-toolchain-evidence-validator.py` | 30 packet scenarios for each of 3 baselines; 48 drift controls; 165 authority controls; 2 historical hash pins; nested current controls passed | `builds-historical-tests.log` |

The initial UI focused run passed 1,307/1,309: two newly added clipboard cases
used the wrong selector. Its failure output remains in `tenants-ui-focused.log`.
The corrected cases pass in the focused and full runs. The full run preceded
only final using-order cleanup; the final successful build and focused run
followed that cleanup. These initial runs precede independent-review corrections.

## Review and correction requirements

Three independent reviewers traced correctness, edge cases and verification.
Every finding, including corroborating duplicates, has its own verdict in the
spec. They identified actual receipt-summary rejection of the new canonical
references, incomplete clearing of cached current preview inputs, capture reads
skipped on noncompleted status paths, unintended recovery-path broadening,
contradictory status/replay documentation and missing CI registration of the
new negative controls. The focused recovery instruction also needed an
accessible description association. Current audit queries require global
administrator authority, so guidance must distinguish owner state inspection
from separately authorized audit inspection.

Parent verification reproduced the newline mismatch: a CRLF-equivalent copy of
the historical baseline has the established normalized-LF SHA-256 but was
rejected by the initial raw-byte disposition validator. The correction must
match the existing historical hash contract while rejecting substantive drift.

## Current runtime observation

The implementation session did not run an AppHost before editing. Parent
started the existing transitional AppHost afterward with
`aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --no-build --non-interactive --format Json`.
The UI health wait and fresh describe succeeded: all 28 resources were
Running/Healthy. Structured telemetry captured four errors from Memories,
including provisioning activity and HTTP 500 retry errors. This is current
no-build runtime health evidence, not a before-edit baseline, zero-error
telemetry result or changed-code browser acceptance. The owned isolated
AppHost was stopped successfully with the exact targeted stop command.
Only sanitized health/command observations are retained; raw runtime output
and authentication-bearing URLs are excluded from the evidence archive.

## Conservation baseline

The original ledger contains all 657 numbered IDs, with 639 done and 18 open.
Its SHA-256 is
`61a0ff040addfb6876f1583f9b26378e51f1a0eac56e69e39d2c0b717bb04d67`.
Acceptance requires preserving all 652 unselected numbered bodies and original
selected findings/fields. Historical toolchain baseline normalized-LF SHA-256
remains
`b9aa6791effe78cbb9bf405cd8384b81a6d750f996fabf8f79b7cd1c3bedad01`.

## Review correction verification

All recorded implementation findings are corrected. The independent recheck
reports no remaining concrete issues. Fresh parent verification after those
corrections passed UI 7,925/7,925, Server 1,584/1,584, EventStore Commands
239/239 and Builds disposition 4/4, all with zero failed/errors/skipped/not-run
where applicable. The existing Builds historical validator suite also passed.

The first correction-focused UI run had 12 failures: eight prior no-read
assertions were superseded by required current-authority refresh, and four new
clipboard cases had a JS handler matcher mismatch. Those failures remain in
`tenants-ui-review-focused.log`. Updated cases retain no-confirmation, exact
identity, lease and exact clipboard-payload assertions. The corrected focused
run passes 303/303, the production audit handler class passes 269/269, and the
EventStore status controller passes 43/43. Raw earlier failures are preserved.

## Upstream main continuity

The final fetch found four EventStore and two Builds commits published during
implementation. The status fix was rebased onto EventStore
`df11852aadcee814bba094c78a60dd2a1fdb934b`, preserving catalog completion,
source-version mapping and released-package bridge removal. The historical
disposition was rebased onto Builds
`d1931b1e803144048d927fff16cb8119afde541e`, preserving the published Tenants
5.11.1 catalog/audit refresh. Both range-diff checks report unchanged patches.

Owning source commits after those clean rebases:

- EventStore: `be4aba873d09736abb4caacf1fae8ad870f0b3b6`.
- Builds: `505230a24339070aa2dbf5ac927cd430050bb902`.

These upstream changes affect build/catalog inputs; post-rebase rebuilding and
qualification are recorded separately from the pre-rebase passing evidence.

## Final post-rebase acceptance

All three rebuilt Debug/source test projects have zero warnings/errors. Final
post-rebase assembly runs pass without failures, errors, skips or unrun cases:

| Owning repository | Command | Result | Log |
| --- | --- | --- | --- |
| Tenants | `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll` | 7,925 passed; 95.564 seconds | `post-rebase-ui-full.log` |
| Tenants | `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll` | 1,584 passed | `post-rebase-server-full.log` |
| EventStore | `dotnet tests/Hexalith.EventStore.Server.Tests/bin/Debug/net10.0/Hexalith.EventStore.Server.Tests.dll -class 'Hexalith.EventStore.Server.Tests.Commands.*'` | 239 passed | `post-rebase-eventstore-commands.log` |
| Builds | `python3 Tools/validate-runtime-toolchain-disposition.py` | Exit 0 | `post-rebase-builds-validation.log` |
| Builds | `python3 Tools/test-runtime-toolchain-disposition.py` | 4 tests pass; CRLF-equivalent positive and substantive-drift/forgery/qualification controls retained | `post-rebase-builds-disposition.log` |
| Builds | `python3 Tools/test-runtime-toolchain-evidence-validator.py` | Full historical/current control suite passes, including both original hash pins | `post-rebase-builds-historical.log` |

The three build commands are the corresponding owning test-project paths with
`dotnet build ... --configuration Debug -m:1 -p:UseHexalithProjectReferences=true`.
Their exact outputs are `post-rebase-ui-build.log`,
`post-rebase-server-build.log` and `post-rebase-eventstore-build.log`.

Exactly DW-438/457/498/521/600 close: all 657 IDs remain, 644 done and 13 open.
Conservation verifies all 652 other numbered bodies byte-for-byte and preserves
original selected fields/findings. The maintenance index exactly covers the 13
open IDs in 12 unchanged remaining rows. The new story's production gitlink
guard passes with both exact dependency targets declared. No review finding
was deferred. Broader runtime, assistive-technology and release holds remain.

The owning EventStore and Builds commits listed above were pushed to their
respective main branches; live remote advertisement matched each full ID.
Exact messages pass their pinned commitlint CLIs (EventStore 21.1.0, Builds
21.2.4, Tenants 21.2.3). Their successful logs and exact candidate files are
retained with the verification records. Tenants publication follows this
accepted source and the user's explicit main push instruction.

Retained logs, conservation scripts/results, sanitized runtime observations,
range-diff continuity and source/binary hashes are in
[evidence manifest](evidence/dw-438-457-498-521-600/manifest.json) and
[verification archive](evidence/dw-438-457-498-521-600/validation-records.tar.gz).
Raw Aspire output and authentication-bearing URLs are deliberately excluded.
