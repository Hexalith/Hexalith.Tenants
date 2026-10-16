# DW-245/DW-265/DW-290 queued membership projection-refresh intent — 2026-10-16

This is bounded maintenance evidence, not full release readiness.

## Ownership, entry and preservation

Hexalith.Tenants owns the change. Entry branch is `main`, full HEAD
`a3f43655d68370389439c7d2c212ab7939bd164e`.
[Entry state](dw-245-entry-state.json) captures all ten repositories' branches,
canonical full HEADs, remotes/history, index/worktree state, recorded/checked-out
root dependency pointers, nonrecursive nested status and root file hashes. It
predates production/test changes. [Preservation](dw-245-preservation.json) compares
entry bytes, current repository state and dependency validation inputs.

The entry tree contains inherited DW-166/DW-173 and DW-176/DW-184 work: ledger,
archive, index, readiness, evidence, workspace production changes and tests. Those
remain preserved. Historical EventStore/Builds movements recorded by DW-166 remain
external work; this bundle neither resets nor claims them. Builds still supplies
EventStore 3.121.0. No dependency version or umbrella gitlink was changed.

Followed AGENTS.md, the permitted root-declared AI.Tools baseline and UX/Git
instructions, tracked build/editor/Git/test configuration, project context,
canonical architecture AD-1–AD-14, maintenance/readiness and the owning records.
The allowed local bmad-build renderer ran exactly once:

```sh
uv run --no-cache "/home/administrator/projects/hexalith/tenants/_bmad/scripts/render_skill.py" --project-root "/home/administrator/projects/hexalith/tenants" --skill "/home/administrator/projects/hexalith/tenants/.agents/skills/bmad-build"
```

The rendered small-change route, investigation and independent Blind Hunter review
were followed. Explicit preservation/no-commit authorization overrides default
dirty-tree/commit steps. No skill was discovered or loaded under references/.
Local Aspire guidance supplied the pre-change isolated start, resource inspection,
telemetry and exact-target stop. No sprint story transition applies.

## Reverified acceptance and implementation

[Owning contract and full review triage](spec-dw-245-265-290-queued-membership-refresh.md)
were recorded before production changes. Story 2.1's frozen intent, settled
ordered-provenance decision, matrix, acceptance and review own confirmation,
identity and admission. Story 2.4's removal acceptance and Loop 3 chunk B explicitly
fix queued removal intent while deferring the same sibling gap. DW-290's metadata
source reference describes that same membership issue; metadata's review preserves
the unconditional parent callback. Story 3.4's review demonstrates why pending
intent must be consumed without short-circuiting. No product decision is reopened.

All three records reproduced against current AddTenantMemberFlow and
ChangeTenantMemberRoleFlow: only a pending flag survived and replays forced parent
projection refresh false. Only those two production files change:

- Enqueue, OR of work/projection intent, batch consumption and idle handoff share
  a short local lock. No await occurs under it. Each batch consumes both bits once;
  later status-only work cannot clear a manual request. One owner serially drains
  requests arriving during status, parent reload or evidence retrieval.
- Pending work carries its attempt generation. Replacement requests supersede
  obsolete queued intent and survive the older owner's completion. Core publication
  continues to enforce exact retained identity, evidence, attachment and generation.
- Failed batches are consumed without an automatic retry. Only additional explicit
  requests receive another turn. Thrown failures reach owner/manual awaiters after
  draining; terminal/disposed work retires pending intent and the gate deliberately.
- The AsyncLocal callback guard and core proof/status/lease/caller/cancellation
  semantics remain intact. Notification callback self-nudges do not poll again.
  Status-only work may inspect projection evidence; it does not request parent
  reload. Neither status nor notifications confirm without authoritative evidence.

No generic extraction, new API, command redispatch, provider policy, lifecycle
budget or disposal-ownership change is included. DW-248/DW-266/DW-267 remain open,
unchanged and separately tracked. Local iterative draining does not claim closure
of their shared three-flow design/extraction recommendations.

## Meaningful regressions and review

62 new cases render actual Fluent components, submit real flow commands and hold
asynchronous status, parent-reload and evidence operations with causal task barriers.
They cover both flows, status-only work then manual refresh, both mixed orders,
multiple requests, projection-first/status-only batches, arrivals at every replay
stage, callback self-nudge suppression, failure results and thrown initial/replay
failures, no-pending failure recovery, terminal retirement, actual renderer unmount,
late success/fault, replacement ownership and same-instance tenant replacement.
Sixteen concurrent release/arrival rounds per flow hold the successor and queue
further manual/nudge work through idle handoffs. No test timeout was increased.

Assertions distinguish exact status lookups, parent reloads and evidence reads,
pin one dispatch per logical attempt, exact domain/aggregate/message/correlation,
CancellationToken.None for dispatched work, stable intent/baseline and retained
aggregate admission. Monotonic overlap detection plus stage counts before releasing
held work protect serialization beyond status alone. Matching evidence with only
Received status stays unconfirmed. Qualified late evidence can legitimately finish
a retained attempt after unmount and be adopted by the replacement; obsolete owner
work cannot restart its queued drain or release foreign admission.

Independent context-free Blind Hunter returned seven findings, all individually
triaged and patched in the spec. Its read-only recheck found no unresolved concrete
issue. The reviewer did not run tests; execution evidence is recorded separately.

## Exact commands, initial failures and limits

[Command records](dw-245-command-results.json) retain exact argv/cwd, UTC times,
durations, exit codes and output. Local Aspire environment, parameter and login
values are redacted. [UI summary](dw-245-ui-summary.json) retains XML hashes/counters,
every initial test identity/message/stack, MTP totals and browser output hashes.
[Final inputs](dw-245-validation-inputs.json) bind source/configuration/browser hashes
and compiled outputs; earlier entry/source hashes remain separate evidence.

Initial failures are preserved. The first build found missing freshness import,
bUnit render-argument errors and CA1062/CA1822; the second found nullable dictionary
attributes. Corrected without changing gates. The first 48-case run rejected a
direct cascading parameter. The corrected pre-production run passed 10/48 and
failed 38, including the actual dropped parent reload and fixture/failure/late-state
expectations. The first implementation passed 30/48. Failure tests now invoke the
actual Fluent OnClick callback because FluentButton handles DOM-click faults itself;
happy-path tests retain actual DOM clicks. Unknown-status evidence starts from
Received, avoiding legitimate confirmation from a previously retained Completed
status. Callback fixtures capture their exact rendering flow.

Four review-added same-instance replacement cases failed with two status reads
instead of three, then passed after generation-tagged batches. bUnit child-handle
Dispose did not actually unmount the flow (12/54 failed); a rendered host now removes
its child and asserts absent controls. Its first rerender omitted required Value
(12/62 failed), corrected without weakening assertions. All failures remain in XML
summaries. The narrow dotnet whitespace formatter emitted non-Allman braces; only
the new file was normalized with the SDK's existing Roslyn parser, then rebuilt and
requalified. No dependency/configuration change was needed.

UI skips are zero in the completed UI lanes. Historical Server 25 DW1 ATDD skips
remain unchanged and were not rerun. Maintained Chromium fixtures retain their
original assertions and negative controls for DOM identity/focus, contextual returns,
corrections, measured viewport/forced colors and native EN/FR status names/selection.
They do not establish authenticated provider/clipboard/denial or human AT acceptance.

Isolated Aspire initially showed Waiting resources and unhealthy security during
startup, then all resources Running/Healthy. Telemetry retains workload assertion
400/denial and pending projection-retry warnings. Exact-target stop passed. This
local health check does not qualify upstream/provider or production boundaries.

## Release blockers carried forward

Broad `dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false`
still exits 1 on two pinned SignalR 10.0.12 NU1903 errors. Strict package-only UI,
`dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -p:HexalithFrontComposerFromSource=false -warnaserror -m:1 -nr:false`,
still exits 1 on seven existing TenantSummaryProjectionPage scope API gaps.
Maintained UI uses the existing FrontComposer source exception and advisory policy;
its passes clear neither blocked gate.

The entry-relative bundle and DW-176 gitlink checks pass. Unchanged B6/B8, DW-517,
DW-105/DW-108, DW-112, DW-150, DW-164/DW-183 and DW-166/DW-173 historical ranges
still fail with six undeclared external/historical movements each. Owning Story
2.1/2.4 historical range checks also fail. Exact commands/output remain recorded;
no baseline narrowing, false ownership declaration, reset or validator bypass ran.

Raw secret-scan/CI qualification, seven authority/recovery/governance decision holds,
upstream nested-layout/startup/provider qualification, hosted API TLS/browser,
dedicated current-candidate NFR10/performance, production authority/revocation and
authenticated six-route/browser/clipboard/denial/human AT holds remain.

Next selected bundle: DW-247, bounded membership ProjectionPending recovery under
the settled August 25/26 UnableToVerify decision. Current source still retains
unqualified-advancement pending snapshots without the configured bound. Related
DW-323 remains separately scoped. DW-121 remains Builds-owned.

## Final qualification and bounded closure

| Lane | Final result |
| --- | --- |
| Debug source / maintained Release builds | Exit0, zero compiler errors; existing SignalR advisory warnings retained |
| Focused membership/proof/retained/guard classes, both configurations | 462/462 each; zero failures/errors/skips/not-run |
| Unfiltered per-project MTP UI, both configurations | 5196/5196 each; zero failures/skips |
| Unchanged maintained Chromium runner, both configurations | Exit0; original assertions and negative controls retained |
| Independent review/recheck | Seven findings patched; no unresolved concrete issue |
| Validation inputs / preservation | 2151 inputs stable across final rebuilds/checks;48 compiled DLL hashes plus generated browser inputs; all ten branches/HEADs/gitlinks stable |
| Ledger/index/archive/sprint audit | Only245/265/290 changed;643 IDs,332 done/311 open,279 exact-covering index rows; archive/sprint bytes preserved |

Final build/test/browser commands (full argv, times, output and prior attempts remain
in the command records):

```sh
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -warnaserror -m:1 -nr:false
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*MembershipRefreshIntentTests' -class '*AddTenantMemberFlowTests' -class '*ChangeTenantMemberRoleFlowTests' -class '*RemoveTenantMemberFlowTests' -class '*TenantRetainedCommandFlowTests' -class '*TenantMembershipCommandProvenanceTests' -class '*TenantAddMemberCommandSnapshotTests' -class '*TenantChangeRoleCommandSnapshotTests' -class '*CommandFlowGuardConformanceTests' -xml /tmp/hexalith-dw245/focused-qualified-debug.xml
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*MembershipRefreshIntentTests' -class '*AddTenantMemberFlowTests' -class '*ChangeTenantMemberRoleFlowTests' -class '*RemoveTenantMemberFlowTests' -class '*TenantRetainedCommandFlowTests' -class '*TenantMembershipCommandProvenanceTests' -class '*TenantAddMemberCommandSnapshotTests' -class '*TenantChangeRoleCommandSnapshotTests' -class '*CommandFlowGuardConformanceTests' -xml /tmp/hexalith-dw245/focused-qualified-release.xml
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-ansi --no-progress --results-directory /tmp/hexalith-dw245/mtp-qualified-debug
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-ansi --no-progress --results-directory /tmp/hexalith-dw245/mtp-qualified-release
env TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
env TENANTS_BROWSER_BUILD_CONFIGURATION=Release bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
```

Whole-tree `git diff --check` exits2 only for the inherited archive EOF blank line.
The archive's exact entry bytes are preserved; changed-source/index/readiness and
new-file checks pass. Historical owning Story2.1 range reports two undeclared later
root additions; Story2.4 reports eight undeclared movements/additions. The earlier
six-movement ranges above remain unchanged. These historical findings are not this
bundle's work, and no validator reset/revert suggestion was executed.

[Ledger audit](dw-245-ledger-validation.json): all643 IDs remain,332 done/311 open;
reconciled128 done/223 actionable/15 blocked/26 awaiting-decision/nine accepted
limitations/38 uncertain. Only DW-245/DW-265/DW-290 close.279 maintenance rows exactly
cover every open ID once. Current readiness partition is seven decision holds,
116 follow-up and188 qualification/triage records. Every unrelated ledger block,
including DW-248/DW-266/DW-267, remains unchanged. All historical readiness bytes
remain an exact prefix. The inherited archive retains323 IDs and its exact hash;
sprint tracking remains unchanged.

B1–B8, DW-517, DW-105/DW-108, DW-112, DW-150, DW-164/DW-183, DW-166/DW-173 and
DW-176/DW-184 remain preserved. No dependency/version/gitlink update, nested
initialization, branch/stage/commit/push, deploy, production replay or data migration
was performed. Inherited files and historical external work were neither replaced
nor attributed to this bundle. No new external movement occurred during this run.

**Next bundle: DW-247 bounded membership ProjectionPending recovery**, keeping
DW-323 separately scoped and DW-121 Builds-owned. This closure is not full release
readiness.
