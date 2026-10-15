# DW-150 validation evidence — 2026-10-15

**Only DW-150 closes in the validated working tree. Release remains not ready.**

## Ownership and contract

Owning repository: Hexalith.Tenants, `main`, full entry/current HEAD
`51f404513f2657e98f949f58f0df43b99b68a7d0`. EventStore retains its external
checkout `241652627a52e47c189b2b052db407cec172f813`, while Tenants records
`24d719292b06d8a9d09771d837f8dd4d8d9b8997`. No dependency movement belongs to
DW-150. [Entry capture](dw-150-entry-state.json) records all ten repositories'
branches, full HEADs, remotes/history, working/index changes, full recorded/index
gitlinks, nested initialization states and inherited file hashes.

AGENTS.md, the permitted root-declared AI.Tools baseline and UX/state/Git rules,
owning configuration, architecture, Story 1.6/1.7 and the dedicated paging owner
Story 1.10 were inspected. [Pre-implementation contract and complete review
triage](spec-dw-150-combined-refresh-coordination.md) distinguish held navigation
from held combined refresh. Story 1.10's matrix/tasks/acceptance and reviews at
1194/1568 require authoritative rereads, independent evidence, retained rows,
truthful refreshing, exact scope and obsolete-generation rejection. Story 1.6's
review at 545 and DW-150 describe the remaining combined-refresh entry point.

A held navigation has not committed its target cursor. Supersession intentionally
rereads the displayed, last-confirmed cursor. Combined refresh already uses that
cursor. Ordinary refresh and operational failure preserve paging/history;
explicit usable page-one recovery alone resets them. Caller/tenant change
invalidates evidence, cancels work and resets paging. Notifications cannot confirm
commands or establish freshness. Persistence rules were read; no persistence
changed.

The allowed repository-local renderer ran exactly once:
`uv run --no-cache "/home/administrator/projects/hexalith/tenants/_bmad/scripts/render_skill.py" --project-root "/home/administrator/projects/hexalith/tenants" --skill "/home/administrator/projects/hexalith/tenants/.agents/skills/bmad-build"`.
Its absolute rendered workflow and small-change route were followed, including
independent Blind Hunter review. Explicit preservation/no-commit instructions
override its dirty-tree halt and commit step. No skill was discovered or loaded
from references/. The allowed Aspire skill supplied local lifecycle checks.

## Change and regression proof

`RefreshTenantDetailAsync` now reroutes when `_memberPageLoadInFlight` **or**
`_tenantUsersSnapshot.IsRefreshing` is true. It reuses `RefreshTenantReadsAsync`
and its existing shared cancellation, generation, caller binding, independent
failure mapping, cursor recovery, lifecycle observation and command nudges.
The production delta is one condition and its explanation. No subscription,
gateway, command, rendering, focus, resource or public API implementation changed.

Seventeen new cases use the actual page and Fluent components with held asynchronous
gateway reads. They cover one/three superseding detail refreshes, obsolete results
before/after the owner, a synchronously completed detail read with held members,
current page-two cursor/history and independent validators, subsequent authoritative
refresh and Previous navigation, independent detail/member/both failures, denial,
wrong-caller evidence, token-registered cooperative cancellation, tenant/caller/
pending-caller/sign-out/disposal transitions, and late obsolete cursor recovery
versus owning usable recovery. Assertions cover rendered rows/configuration/
banners/pager/recovery notices, snapshot truth/evidence and exact request scope.
Existing navigation, coalescing, command, lifecycle, focus and authorization tests
remain intact.

Independent review identified five regression-evidence gaps. Each is individually
triaged and patched in the spec: continuation ordering, cancellation timing,
pending identity interval, distinct auxiliary evidence and recovery ownership.
The independent final recheck passes all17 new cases, exit0, with no remaining
finding or deferral. No production change was needed after the initial patch.

## Exact validation and initial failures

[Commands](dw-150-command-results.json) preserve argv, cwd, exit code, duration and
outputs, including the independent run. Local Aspire login/environment/parameter
values are redacted; commands, resource state and endpoints remain.
[UI counters and failures](dw-150-ui-summary.json) retain initial XML failures,
final counters, zero skip identities, browser outputs and source digests.

| Lane | Result |
| --- | --- |
| Debug source-reference UI build, warnings gate and serialized build | Exit0; zero compiler errors; existing NU1903 advisory retained |
| Maintained Release UI build, existing FrontComposer source exception | Exit0; zero compiler errors; existing NU1903 advisory retained |
| Final detail/subscription focused classes, Debug and Release | 332/332 each; zero failures/skips |
| Final unfiltered per-project MTP UI, Debug and Release | 5004/5004 each; zero failures/skips |
| Independent final new-case recheck | 17/17; zero failures/skips |
| Maintained Chromium browser runner, Debug and Release | Exit0 both; all original scenarios and negative controls retained |
| Whitespace, ledger/index and preservation audits | Exit0 |

Build commands are `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false`
and the same maintained command using `Release` and
`-p:UseHexalithProjectReferences=false`. Final full commands are
`dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-ansi --no-progress --results-directory /tmp/hexalith-dw150/mtp-debug-final`
and its `Release` / `mtp-release-final` equivalent. Direct xUnit single-dash
`-class` selectors cover both detail and subscription classes.

Browser commands are `env TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh`
and its Release equivalent. They retain existing focus/navigation, correction,
responsive/forced-colors, admission, identity, EN/FR native-name/focus and mutation
controls. No browser real/virtual bound, test timeout, exclusion, coverage floor,
build/CI policy or dependency version changed. The fixture browser checks do not
prove authenticated topology, live provider behavior or human AT acceptance.

Initial evidence is retained: all14 original new cases failed on missing member
reissue before the guard changed. The first fixed candidate passed12/14; two new
assertions incorrectly expected a configuration table after detail failure. They
now assert the existing degraded/unknown detail with unavailable configuration.
The first review-correction run passed16/17; a recovery request has no render, so
bUnit's render-triggered wait missed it. Explicit gateway-entry signals now await
both recovery interleavings within the unchanged `DefaultWaitTimeout`. No timeout
was increased. An earlier5001-case Debug pass qualifies the earlier test set;
final5004-case runs qualify the completed regressions.

The first ledger audit passed ID preservation, unrelated-record equality, exact
open-index coverage and the single removed action row, but rejected its count
calculation:312 closed records lack an explicit disposition field. Recomputing
reconciled done from done statuses minus the preserved204 earlier closed-record
boundary yields119; no historical record received a new field or changed status.
The corrected audit passes and both outputs remain in command evidence.

Final UI skips are zero. DW-517's25 existing Server DW1 ATDD skips remain historical
and were not rerun or reclassified by this UI-only change.

## Remaining blockers and preservation

`dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false`
still exits1 with two NU1903 errors on pinned SignalR10.0.12. Strict package-only
UI, adding `-p:UseHexalithProjectReferences=false -p:HexalithFrontComposerFromSource=false`,
still exits1 with seven existing `TenantSummaryProjectionPage` projection-scope
API errors. Maintained UI success uses its unchanged source exception; it does not
qualify either blocked gate. The maintained Release graph was rebuilt successfully
after that diagnostic.

Exact historical provenance commands were rerun: B6/B8 each retain five inherited
movements; DW-517, DW-105/DW-108, DW-112 and DW-150 each retain the external
EventStore checkout movement failure. No original baseline/pointer was reset,
declared as DW-150-owned or bypassed. Raw secret-scan/CI wiring, seven authority/
recovery/governance decisions, nested-layout/provider/startup qualification,
current-candidate NFR10/performance, hosted TLS/browser, production authority/
revocation and authenticated browser/clipboard/human AT holds remain. These are
historical release holds, not newly passing gates. No nested submodule was initialized.

Isolated Aspire startup and both resource inspections succeeded with resources
Running/Healthy; exact-target stop succeeded. No production replay, migration,
deployment or data mutation occurred.

[Preservation audit](dw-150-preservation.json) verifies all original branches/HEADs,
indexes, recorded/checked-out dependency pointers, dependency diffs/status and
inherited hashes,3446 unrelated owning files, every preexisting detail test byte,
and the exact original readiness prefix. B1–B8, DW-517, DW-105/DW-108, DW-112,
external work, inherited source, archive/sprint, gates and decisions remain intact.
No staging, commit, push, branch, dependency update or umbrella gitlink edit ran.

Only DW-150 changes disposition. All643 IDs remain:323 done/320 open,285 index
rows; reconciled119 done/232 actionable/15 blocked/26 awaiting-decision/nine
accepted limitations/38 uncertain, preserving the204 earlier closed-record
boundary. [Ledger audit](dw-150-ledger-validation.json) verifies every unrelated
record/action row and exact coverage of every open ID.

**Next selected bundle: DW-164/DW-183, route-switch subscription handoff.** Both
remain open/actionable/P2 with no dependency; DW-183 duplicates DW-164. Current
setup rejects incoming-tenant subscription while old setup is held; its finally
only clears the flag, leaving current-tenant retry dependent on a later render.
Keep separate DW-166/DW-173 timeout work and the existing retry budget unchanged.
DW-121 remains Builds-owned. This closure is scoped UI evidence, not release approval.
