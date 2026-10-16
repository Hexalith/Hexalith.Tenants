# DW-176/DW-184 workspace consumer acceptance — 2026-10-15

**Only DW-176/DW-184 close. Release remains not ready.**

## Entry, ownership and preservation

Hexalith.Tenants owns this bundle. Entry and final branch is `main`, HEAD
`a3f43655d68370389439c7d2c212ab7939bd164e`. The working tree already contained
DW-166/DW-173 ledger/archive/index/readiness/evidence and GA finite-bound test
changes. [Entry capture](dw-176-entry-state.json) records all ten repositories'
branches, full canonical HEADs, remotes/history, index/worktree state, recorded and
checked-out dependency pointers, nonrecursive nested status and file hashes.
[Preservation](dw-176-preservation.json) compares actual entry bytes and states.
All ten branches/HEADs/gitlinks remain stable; dependency sources and inherited
files remain preserved. No new external movement occurred during this bundle.

Prior external commits and dependency movements belong to their historical runs,
including the EventStore movement and Builds catalog change recorded in
[DW-166 evidence](dw-166-validation-evidence.md) and its movement capture. They
are preserved, never reset or attributed to this bundle. The current Builds catalog
still supplies EventStore3.121.0. No dependency version or umbrella gitlink changed.

Read AGENTS.md, the permitted root-declared AI.Tools baseline/UX/Git documentation,
tracked build/editor/Git/test configuration, architecture, maintenance/readiness,
the requested DW-166 records and Story1.11 specifications, settled decisions and
review records. The allowed local bmad-build renderer ran exactly once:

```sh
uv run --no-cache "/home/administrator/projects/hexalith/tenants/_bmad/scripts/render_skill.py" --project-root "/home/administrator/projects/hexalith/tenants" --skill "/home/administrator/projects/hexalith/tenants/.agents/skills/bmad-build"
```

Its rendered small-change route and independent Blind Hunter review were followed.
Explicit preservation/no-commit instructions override the dirty-tree halt and commit
steps. Allowed local Aspire/browser guidance supplied validation; no skill was
discovered or loaded under repository references/. This maintenance bundle does not
transition sprint stories or alter persistence.

## Reconciled contract and proven consumer gaps

[Owning acceptance contract and complete review triage](spec-dw-176-184-workspace-consumer-acceptance.md)
were recorded before production edits. DW-176's August25/26 decisions require
replaceable cancellation plus the shared configured authorization bound. DW-184
requires the same bound/cancellation on authentication events, keeping entry hidden
on expiry. Story1.11 owns circuit precedence, corroborated BFF authority, optional
resolution independent of list loading, fail-closed pending/faulty evidence and
guarded current-caller application. No identity/recovery policy decision is reopened.

DW-173 already supplied the necessary entry/create/authentication-event policy wiring.
This bundle reuses TenantUiOperationPolicy and
`Tenants:Ui:AuthorizationAndSubscriptionTimeout` unchanged: default10s, configured
finite bound and injected TimeProvider. It does not reimplement shared provider
setup, cleanup, cancellation-callback isolation or subscription ownership.

Production changes remain in TenantsWorkspace:

- Entry, audit availability and proven-denial presentation now bind the same resolved
  current caller as create. An accessor change cannot expose A's entry or mislabel B
  as proven denied. Create preflight cannot query authority while authentication is
  pending, expired or unresolved; the rendered action stays Indeterminate/unavailable.
- Create authority requires the active tenant-list view. A changed workspace generation
  cancels only undispatched create admission; final application checks view, caller,
  generation, snapshot and linked cancellation. Retained dispatched command work keeps
  its independent delivery/tracking lifetime.
- Explicit recovery reacquires current provider evidence even while an older event is
  pending. Still-held/faulty evidence remains closed under the same bound. Synchronous
  acquisition failure cancels previous authority sources too. Disposed authority
  callbacks cannot start new work; source replacement also respects disposal.
- Actual Router tests exposed literal-alias navigation skipping parameter delivery
  while FrontComposer tabs follow the URI independently. A workspace LocationChanged
  listener reconciles tab transitions on the renderer from the captured destination
  URI, ignores non-workspace routes, rejects superseded destinations and unsubscribes
  on disposal. It uses existing normalization, leaves query-only paging to its original
  lifecycle, suppresses duplicate tab work and does not canonicalize stale query fields.

No subscription coordinator, other production component, gateway, command lifecycle,
dependency, gate, exclusion, coverage floor or unrelated timeout policy changed.

## Meaningful acceptance and independent review

34 new cases use actual Fluent components:24 consumer cases and10 actual
Router/RouteView lifecycle cases, plus a stateful authentication-provider fixture.
They exercise genuinely held initial authentication and event tasks, configured
deterministic expiry, explicit recovery while pending/expired/faulty, synchronous
provider throws, same-caller entry replacement via the actual audit callback, caller
accessor mismatch, active replacement/disposal before expiry, and late success/fault.
Captured consumer CTS disposal provides a causal waiter-retirement barrier; create
also awaits the actual Submit task. Late view outcomes arrive after the replacement
dispatch, preserving its exact accepted message/correlation and authority.

Router cases cover both initial and final pre-dispatch authorization. The latter
proves message allocation, undispatched lease exclusion/release and exact explicit
retry. Users retains the same flow; mine unmounts/disposes it. Held dispatched gateway
responses preserve CancellationToken.None, exact intent/message/correlation,
baseline/timestamp, marked lease/tracker ownership, remount adoption, unrelated
aggregate admission and status recovery with one delivery. Exact reads/commands are
asserted; clocks remain at epoch for before-expiry transitions. Existing1s virtual
bound/default bUnit waits/5s task guards remain unchanged.

Independent context-free Blind Hunter produced seven findings; all were individually
confirmed, classified and patched in the spec's triage table. Read-only rechecks found
no unresolved concrete issue, including the later proven alias gap and fixture
correction. Review does not claim test execution; command records supply that proof.

## Exact validation and initial failures

[Command records](dw-176-command-results.json) retain exact argv/cwd, timestamps,
exit codes, durations and output for each recorded attempt. Local Aspire environment,
parameter and login values are redacted. [UI summary](dw-176-ui-summary.json) retains
XML counters/hashes, every initial failed test identity/message/stack, command results
and browser output hashes. [Final inputs](dw-176-validation-inputs.json) bind2150
source/configuration/browser inputs unchanged across final rebuilds/checks, plus12
compiled output hashes. The earlier85-case entry-binary pass is baseline evidence,
distinct from current-source qualification.

| Final lane | Result |
| --- | --- |
| Debug source and maintained Release UI builds | Exit0, zero compiler errors; existing SignalR advisory warnings retained |
| Focused six UI classes, Debug and Release | 849/849 each, zero errors/failures/skips/not-run |
| Unfiltered per-project MTP UI, Debug and Release | 5134/5134 each, zero failures/skips |
| Unchanged Chromium runner, both configurations | Exit0, original assertions and negative controls retained |
| Independent review and final source checks | No unresolved concrete issue |
| Preservation and ledger/index audit | Pass; only two scoped records close |

Exact final command forms:

```sh
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -warnaserror -m:1 -nr:false
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantDetailSurfaceTests' -class '*TenantReadRefreshSubscriptionTests' -class '*TenantUiOperationPolicyTests' -class '*GlobalAdministratorsPageTests' -class '*TenantsWorkspaceTests' -class '*TenantsUiCompositionTests' -xml /tmp/hexalith-dw176/focused-final-debug.xml
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantDetailSurfaceTests' -class '*TenantReadRefreshSubscriptionTests' -class '*TenantUiOperationPolicyTests' -class '*GlobalAdministratorsPageTests' -class '*TenantsWorkspaceTests' -class '*TenantsUiCompositionTests' -xml /tmp/hexalith-dw176/focused-final-release.xml
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-ansi --no-progress --results-directory /tmp/hexalith-dw176/mtp-final-debug
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-ansi --no-progress --results-directory /tmp/hexalith-dw176/mtp-final-release
env TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
env TENANTS_BROWSER_BUILD_CONFIGURATION=Release bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
```

Initial failures remain recorded. The first test build had CS8600/CS8604 on a nullable
recovery delegate, corrected with ShouldNotBeNull. New17-case red failed11: nine real
consumer failures plus two empty-state selector errors. First green passed15/17:
the mine fixture read a disposed bUnit handle; capture the instance before unmount
instead. Expanded lifecycle build found CS4014/missing freshness import, corrected
without assertion changes. First849 Debug failed6, Release failed5: five real Users
alias failures; Debug also caught an inherited detail-handoff scheduling assertion.
That fixture now awaits finalEntered, asserts exactly3 before releasing obsolete beta,
and retains all final scope/lease/read checks. Final36-case check and all qualification
include these corrections. A temporary input-audit helper first lacked an argv entry
(IndexError); its corrected invocation passes. No test timeout was increased.

UI skips remain zero. Historical Server25 DW1 ATDD skips remain unchanged/unrerun.
Browser checks preserve DOM identity/focus, contextual returns, corrections, measured
viewport/forced-colors admission, native EN/FR status names/selection and all negative
controls. Local/rendered fixtures do not prove authenticated provider/clipboard or
human AT acceptance. Isolated Aspire start/describe reached Running/Healthy, including
security and Tenants UI; telemetry retained local connection retries. Exact-target
stop passed. No nested initialization or deployed operation ran.

## Remaining release blockers and bounded closure

Broad command `dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false`
still exits1 on two pinned SignalR10.0.12 NU1903 errors. Strict package-only UI,
`dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -p:HexalithFrontComposerFromSource=false -warnaserror -m:1 -nr:false`,
still exits1 on seven existing TenantSummaryProjectionPage scope API gaps. Maintained
UI passes retain the existing FrontComposer source exception and advisory policy;
they do not clear either blocked gate.

This bundle's entry-relative story-gitlink validator passes with no pointer movement.
Unchanged B6/B8, DW-517, DW-105/DW-108, DW-112, DW-150, DW-164/DW-183 and DW-166/DW-173
range commands still exit1 with six undeclared historical/external movements each.
All earlier findings and ranges remain intact; no reset, baseline narrowing, false
ownership declaration or bypass was used. Whole-worktree `git diff --check` exits2
on the inherited archive EOF blank line. Focused changed-source/new-file checks pass;
the archive's exact entry bytes remain preserved.

Raw secret-scan/CI qualification, seven authority/recovery/governance decisions,
upstream nested-layout/startup/provider qualification, hosted API TLS/browser,
dedicated current-candidate NFR10/performance, production authority/revocation and
authenticated six-route/browser/clipboard/denial/human AT holds remain. Existing
successful local lanes do not qualify those release boundaries.

[Ledger audit](dw-176-ledger-validation.json) retains all643 IDs:329 done/314 open,
280 exact-covering maintenance rows; reconciled125 done/226 actionable/15 blocked/
26 awaiting-decision/nine accepted limitations/38 uncertain. Current readiness
partition is seven decision holds/119 follow-up/188 qualification-triage records.
Only DW-176/DW-184 close. Historical findings, old verification/actions, B1–B8,
DW-517, DW-105/DW-108, DW-112, DW-150, DW-164/DW-183 and DW-166/DW-173 remain.
The current inherited archive has323 IDs and is unchanged; sprint and every unrelated
ledger block remain unchanged. Historical readiness is preserved as an exact prefix.

No dependency/version/gitlink update, nested initialization, branch/stage/commit/push,
deployment, production replay or data migration was performed.

**Next selected bundle: DW-245/DW-265/DW-290**, the duplicate add/change-role refresh
intent coalescing findings. Current source retains only a pending flag and replays
with projection intent false; retain/OR the queued intent with held status-read proof.
DW-121 remains Builds-owned. This closure is not full release readiness.
