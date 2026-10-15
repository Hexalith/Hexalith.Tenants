# DW-112 validation evidence — 2026-10-15

**Only DW-112 closes in the validated working tree. Release remains not ready.**

## Ownership and settled contract

The owning repository is Hexalith.Tenants, `main`, full entry/current HEAD
`51f404513f2657e98f949f58f0df43b99b68a7d0`. EventStore stays on `main` at external
checkout `241652627a52e47c189b2b052db407cec172f813`, with recorded Tenants pointer
`24d719292b06d8a9d09771d837f8dd4d8d9b8997`. [Entry capture](dw-112-entry-state.json)
records all ten repositories' branches, full HEADs, remotes/history, working/index
changes, recorded/checked-out pointers and nested initialization states. Inherited
B1–B8, DW-517 and DW-105/DW-108 work belongs to its original bundles.

AGENTS.md, the permitted root-declared AI.Tools baseline and UX/state/Git
instructions, owning guidance/configuration, architecture and referenced Story
1.2/1.9 specifications, review records and search evidence were read. The allowed
repository-local bmad-build renderer ran exactly once:
`uv run --no-cache "/home/administrator/projects/hexalith/tenants/_bmad/scripts/render_skill.py" --project-root "/home/administrator/projects/hexalith/tenants" --skill "/home/administrator/projects/hexalith/tenants/.agents/skills/bmad-build"`.
It returned its absolute rendered workflow, whose small-change route and independent
Blind Hunter review were followed. Explicit user preservation/no-commit instructions
override workflow dirty-tree and commit steps. No skill was discovered or loaded
from references/. The baseline persistence rules were read; no persistence changed.

The [pre-implementation contract and complete review triage](spec-dw-112-status-filter-labeling.md)
records the owning choice before implementation: `snapshot.IsAuthoritativeSearch`
selects both names. Authoritative search uses **Status across indexed candidates** /
**Statut parmi les candidats indexés**; ordinary browsing and nonempty-search
fallback use **Status on current page** / **Statut sur la page actuelle**. Both
existing resource keys and values are preserved. Query presence alone grants no
authoritative semantics; authorization/hydration, filtering, sorting, paging,
commands and protected search state retain their existing contracts.

## Final change and independent review

The visible label and host accessible name now use the same scope-aware key.
Consumed Fluent 5.0.0 initializes its actual light-DOM combobox button name once
from the field label. The workspace therefore synchronizes that existing button's
name when its localized label changes or the selector reappears. The ten-line
`tenantsStatusFilter.js` helper reads the owning host name; it never changes value,
focus, selection, requests or other controls. It adds no observer, timer, new
component API or dependency. Successful updates are cached; disconnect/cancellation
is handled through the workspace's existing browser-operation pattern. Both Debug
and Release static-asset manifests include the helper.

Six initial independent findings plus a correction finding are all individually
triaged and resolved. Review proved that the one-expression fix leaves the native
name stale, while the interim scope-key fix loses focus. Final code keeps the same
native button and updates its name in place. Independent final rechecks pass all
16 new rendering cases and all 30 browser states with no unresolved finding.

Regressions use the actual workspace, Fluent components and shipped EN/FR resources.
They cover ordinary browsing, nonempty-query fallback, authoritative ready/partial/
empty results, search/fallback/recovery/clear transitions, delayed initial loading,
and a held authoritative Active-to-Disabled status change through loading and
completion. Selected status, selector identity, gateway request state and actual
label-sync invocations are asserted. Chromium replays actual successive rendered
fields and interop counts with shipped Fluent startup/first-render initialization
and the shipped helper. Its accessibility tree must report the expected combobox
name; the native button and focus remain identical during the held status change.
Both cultures' deliberately stale native-name controls are rejected.

## Executed checks and initial failures

[Exact argv, cwd, exit codes, durations and outputs](dw-112-command-results.json)
retain all parent-run checks. [Counters, initial XML failures, zero skip identities
and static-asset manifests](dw-112-ui-summary.json) distinguish final qualification
from intermediate results.

| Lane | Final result |
| --- | --- |
| Debug UI build, source references, `-warnaserror -m:1 -nr:false` | Exit0; zero compiler errors; existing NU1903 warnings retained |
| Maintained Release UI build, package defaults and existing FrontComposer source exception | Exit0; zero compiler errors; existing NU1903 warnings retained |
| Focused rendering/list/localizer classes, Debug and Release | 131/131 each; zero failures/skips |
| Unfiltered per-project Microsoft.Testing.Platform UI, Debug and Release | 4987/4987 each; zero failures/skips |
| Maintained Chromium browser runner, Debug and Release | Exit0 both; original scenarios and controls plus 30 scope-name states pass |
| Independent corrected rendering / Chromium recheck | 16/16 cases; 30 EN/FR states; native focus retained; stale names rejected |
| Shell/JavaScript syntax, whitespace and preservation checks | Exit0 |

The final full commands are `dotnet test --project
tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration
Debug --no-build --no-ansi --no-progress --results-directory
/tmp/hexalith-dw112/mtp-debug-final` and the same command with `Release` and
`/tmp/hexalith-dw112/mtp-release-final`. These are maintained MTP project runs;
direct xUnit single-dash class selectors supply focused evidence. Browser commands
are `env TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash
tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` and its
Release equivalent. Chromium is Google Chrome154.0.8037.57; Node26.4.0 uses built-in
WebSocket without new packages. Existing browser real/virtual bounds, exclusions,
coverage floors, CI/build configuration and scenarios remain unchanged.

Initial failures are preserved: two 12-case runs failed on new-fixture assumptions
about Fluent's tag/label owner; after correcting the actual field structure, four
page-local cases passed and eight authoritative/transition cases failed on the
original stale host name. The browser initially hit its unchanged30s bound because
the harness omitted Fluent startup registration, then exposed an empty name until
actual `Select.Initialize` was replayed. The old host-mutation control failed because
that host does not name Fluent's native button; it now mutates the actual native
name. The one-expression implementation failed computed-name transition coverage;
the interim key passed naming but independent instrumentation demonstrated BODY
focus after replacement. Two later fixture builds failed CS8600 on a nullable
selector ID; a nonnull assertion corrected that fixture. None of these failures was
hidden by changing a gate, timeout or exclusion. The earlier4985-case pass qualifies
only its interim candidate; final4987-case runs qualify the stable-control fix.

Final UI runs have zero skips. The inherited DW-517 Server25 named DW1 ATDD skips
are preserved and were not rerun or reclassified by this UI-only bundle.

## Remaining release blockers and preservation

`dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1
-nr:false` still exits1 with two NU1903 errors for the pinned
Microsoft.AspNetCore.SignalR.Common10.0.12 advisory. The strict package-only UI
command adding `-p:UseHexalithProjectReferences=false
-p:HexalithFrontComposerFromSource=false` exits1 on seven existing projection-scope
API gaps in unchanged `TenantSummaryProjectionPage`. Maintained UI success uses its
existing source exception and does not qualify either broad/package-only gate.

B6/B8 story-gitlink commands retain five inherited movements each; DW-517,
DW-105/DW-108 and DW-112 retain the external EventStore checkout movement failure.
The first DW-112 provenance call also rejected a missing `baseline_commit` field;
adding the exact full entry HEAD fixed metadata without hiding the inherited
movement. No original baseline, pointer or ownership attribution was changed.
Raw secret-scan/CI wiring, seven authority/recovery/governance decisions, nested
layout/provider/startup qualification, current-candidate NFR10/performance, hosted
TLS/browser, production authority/revocation and authenticated browser/clipboard/
human assistive-technology holds remain. These historical blockers are preserved,
not claimed as newly rerun gates. No nested submodule was initialized.

The isolated local Aspire start first reported waiting/unhealthy startup resources,
then all resources running and healthy; exact-target stop succeeded. No production
replay, migration or mutation occurred. Browser fixtures substitute gateway services;
they establish the real Fluent DOM/name/focus contract, not authenticated topology,
live Memories/provider behavior or human screen-reader acceptance.

[Preservation audit](dw-112-preservation.json) verifies original branches/HEADs,
indexes, pointers, dependency state/diffs, all unrelated owning entry bytes, all1371
inherited EventStore historical artifact hashes and the seven inherited dirty files
against preserved prior-entry hashes. Original readiness bytes and the maintained
browser runner are exact prefixes of their updated files. B1–B8, DW-517,
DW-105/DW-108, external commits, archive, sprint tracking, resource/build/CI contracts
and unresolved decisions remain intact. No staging, commit, push, branch, dependency
update, umbrella gitlink change or deployment ran.

Only DW-112 closes:643 IDs remain,322 done/321 open,286 index rows covering every
open ID exactly once. Reconciled counts are118 done/233 actionable/15 blocked/
26 awaiting-decision/nine accepted limitations/38 uncertain, preserving the204
earlier closed-record boundary. [Ledger/index audit](dw-112-ledger-validation.json)
verifies every unrelated record and action row is unchanged.

**Next selected Tenants-owned bundle: DW-150 alone.** Current detail-refresh logic
still reroutes only `_memberPageLoadInFlight`; a combined `RefreshTenantReadsAsync`
can be cancelled by detail refresh without reissuing its member read. It is open/
actionable with no dependency or policy decision. DW-121 remains open in its owning
Hexalith.Builds repository. DW-112 closure establishes this scoped UI fix, not full
release readiness.
