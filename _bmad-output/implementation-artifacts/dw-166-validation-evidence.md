# DW-166/DW-173 validation evidence — 2026-10-15

**Only DW-166/DW-173 close. Release remains not ready.**

## Ownership and preserved entry state

Hexalith.Tenants owns the implementation. Activation was on `main` at
`51f404513f2657e98f949f58f0df43b99b68a7d0`, with inherited B1–B8, DW-517,
DW-105/DW-108, DW-112, DW-150 and DW-164/DW-183 changes/evidence. External Git
activity began before the detailed capture completed: its owning HEAD is
`6cb020e5c4a50169df2abf973d824faec9ee2e52`. [Entry state](dw-166-entry-state.json)
records ten repositories' branches, full canonical HEADs, remotes/history,
working/index state, dependency pointers, nested initialization and file hashes;
3465 owning files are captured. The initial session observation and later capture
are deliberately distinguished.

Latest captured Tenants HEAD is `a3f43655d68370389439c7d2c212ab7939bd164e`, after seven
external commits. Some in-progress bundle files were recorded by that activity;
review therefore uses entry-relative content, rather than only the remaining dirty
diff. Original EventStore recorded pointer was
`24d719292b06d8a9d09771d837f8dd4d8d9b8997`, with inherited external checkout
`241652627a52e47c189b2b052db407cec172f813`. External activity subsequently recorded
`f359f65e130575ea52a3a8ea8511004d2adcc7dd`, then
`746b843a24f18b17a7572be25b76b735fa8e4ca6` in Tenants; its latest checked-out HEAD
also equals the latter. Builds, Commons, FrontComposer, Memories and Platform moved
externally too. Builds changed the EventStore package pin from3.120.0 to3.121.0;
final qualification uses that preserved catalog, with its hash unchanged throughout. [Full movement capture](dw-166-external-movements.json)
and [preservation audit](dw-166-preservation.json) retain all exact identities and
content deltas. None belongs to this bundle's dependency work; no reset or pointer
update was performed by this run.

AGENTS.md, the permitted root-declared AI.Tools baseline and UX/state/Git rules,
tracked build/editor/Git configuration, architecture, requested maintenance/readiness
and DW-164 records, Story1.10/1.11 specifications/decisions/reviews and related
DW-176/DW-184 were inspected. Persistence remains EventStore-owned; no domain data
or persistence changed. The allowed local skill renderer ran exactly once:
`uv run --no-cache "/home/administrator/projects/hexalith/tenants/_bmad/scripts/render_skill.py" --project-root "/home/administrator/projects/hexalith/tenants" --skill "/home/administrator/projects/hexalith/tenants/.agents/skills/bmad-build"`.
Its absolute rendered workflow's small-change route and independent Blind Hunter
review were followed. Explicit preserve-dirty-work/no-commit instructions override
its dirty-tree halt/commit step. No skill was discovered or interpreted under the
repository references/. Allowed local Aspire/browser guidance supplied validation.

## Owning contract and implementation

[Pre-production contract and full ten-finding review triage](spec-dw-166-173-finite-authorization-subscription-bounds.md)
record the settled2026-08-25/26 decisions: propagate replaceable caller/circuit
cancellation; one configured finite resolver/subscription deadline; Indeterminate
on authorization expiry; each current setup timeout charges the existing three
failed attempts. Story1.10 owns exact scope, reference-counted leases, initial
notification registration, authoritative-read nudges, generation rejection and
late cleanup. Story1.11 owns the atomic gates, safe diagnostics and recorded defects.
Reverification found uncancellable GA/workspace waits and a coordinator semaphore
held through unbounded setup/cleanup. A page-only timeout could not satisfy ownership.

`Tenants:Ui:AuthorizationAndSubscriptionTimeout` defaults to `00:00:10`, uses an
injected TimeProvider with System fallback and rejects nonpositive/invalid values
or durations outside timer limits. Production composition registers the same policy;
component-only composition has a compatible fallback. No prior numeric setting
existed. [Quickstart configuration](../../docs/quickstart.md#ui-authorization-and-notification-deadline)
documents the setting and recovery. Existing HTTP/enrichment/command bounds remain.

The waiter owns an independent deadline/caller token. Provider cancellation uses
CancelAsync, so a stalled cancellation callback cannot block a page's cancellation
path or deadline. Token disposal observes actual provider and cancellation-callback
completion. The real coordinator replaces its global semaphore with exact per-scope
ownership. Same-scope wait plus new setup share a finite deadline; other scopes
continue independently. Failed callbacks detach promptly. Since provider unsubscribe
is keyed by scope, unfinished setup/cleanup retains quarantine until its actual
lifetime completes; late cleanup cannot detach a replacement. Missing services,
failed setup and expiry return Empty, which consumers charge to their existing
budget. Last-reference/circuit cleanup is bounded; leases remain reference-counted.

GA authorization shares caller lifetime across concurrent actions, with separate
local linked tokens; a notification cannot cancel an ordinary refresh's authority
check. Reauthorization and auth-event failed collapse validate the captured caller
inside the renderer. GA setup captures the transition epoch, rejects obsolete
callbacks/adoption and hands off only after releasing its gate. Failed setup callbacks
also become inert under an unchanged caller/route. Workspace entry/create/auth-event
waits receive the same bound/cancellation; authority stays hidden on expiry.
DW-176/DW-184 remain open for separately scoped consumer acceptance reconciliation.

Detail subscription setup owns a route/caller CTS independent of loads. Route/caller
invalidation/disposal cancels setup, while refresh, member paging and command requery
preserve established notification subscriptions. DW-164/DW-183's completion-driven
latest-scope handoff and DW-150's combined reads remain. Exact projection/tenant,
caller binding, ETags, truthful refresh/freshness, cursor/history, focus and command
lifecycle/admission behavior retain their existing assertions. No notification is
presented as freshness or command confirmation. Diagnostics retain fixed safe codes.

## Meaningful regressions and independent review

64 new cases run through held asynchronous operations:35 actual Fluent GA/workspace,
three actual Fluent detail and26 policy/real-coordinator cases. They cover cooperative
and ignored cancellation, late success/fault, initialization/retry/notification
request behavior, active caller/disposal cancellation, exact shared leases, three
failed attempts/exhaustion/explicit recovery, route/caller handoff, obsolete callbacks,
held cleanup and circuit disposal. Actual create admission refuses dispatch while
authority is unknown and recovers through rendered refresh. Blocking provider
cancellation callbacks cannot own the waiter; retirement includes those callbacks.
Production registration proves a nondefault clock/deadline for authority/setup/cleanup
and invalid configuration rejection. Detail advances exactly one250ms period per
setup source, asserting exact advance counts and elapsed virtual time. Existing
32 handoff and17 combined-refresh cases remain, with ownership assertions retained.

Independent review produced ten findings, all triaged individually and patched in
the spec. It caught concurrent refresh cancellation, a stale failed-collapse race,
cancellation-callback coupling and seven concrete coverage gaps. Final independent
read-only rechecks found no unresolved concrete issue, and approved exact-clock,
no-render adoption and asynchronous composition-disposal fixture corrections.
Review does not claim to have executed tests; commands below supply that evidence.

## Exact commands and results

[Command records](dw-166-command-results.json) retain exact argv/cwd, start timestamps,
exit codes, durations and complete output for every recorded attempt. Concurrent
independent lanes append under a lock; records are in completion order, not assumed
start order. Aspire login/environment/parameter values are redacted. [UI summary](dw-166-ui-summary.json)
retains XML counters/hashes, every initial failure identity/stack, full-suite failure
identities and browser output hashes. Final qualification results:

| Lane | Result |
| --- | --- |
| Final Debug source and maintained Release UI builds | Exit0, zero compiler errors; existing pinned SignalR advisory warnings retained |
| Final focused six UI classes, Debug and Release | 815/815 each; zero errors/failures/skips/not-run |
| Final unfiltered per-project MTP UI, Debug and Release | 5100/5100 each; zero failures/skips |
| Unchanged Chromium runner, Debug and Release | Exit0 both; original scenarios/negative controls retained |
| Independent review | All ten findings patched; final rechecks clear |
| Source-input stability | 2136 relevant source/configuration hashes unchanged across final rebuild/checks |

Exact final command forms (the command JSON contains both configurations and all paths):

```sh
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -warnaserror -m:1 -nr:false
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantDetailSurfaceTests' -class '*TenantReadRefreshSubscriptionTests' -class '*TenantUiOperationPolicyTests' -class '*GlobalAdministratorsPageTests' -class '*TenantsWorkspaceTests' -class '*TenantsUiCompositionTests' -xml /tmp/hexalith-dw166/focused-debug-qualified.xml
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantDetailSurfaceTests' -class '*TenantReadRefreshSubscriptionTests' -class '*TenantUiOperationPolicyTests' -class '*GlobalAdministratorsPageTests' -class '*TenantsWorkspaceTests' -class '*TenantsUiCompositionTests' -xml /tmp/hexalith-dw166/focused-release-qualified.xml
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-ansi --no-progress --results-directory /tmp/hexalith-dw166/mtp-debug-qualified
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-ansi --no-progress --results-directory /tmp/hexalith-dw166/mtp-release-qualified
env TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
env TENANTS_BROWSER_BUILD_CONFIGURATION=Release bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
```

These use individual projects and direct xUnit v3 class filters; no solution-level
test command or unsupported MTP --filter was substituted. Browser checks retain
focus/navigation, corrections, viewport/forced-colors, admission, DOM-safe identity,
native EN/FR status names/focus and negative controls. Static/rendered fixtures do
not qualify authenticated live topology, provider authority, clipboard or human AT.

Initial failures remain visible: first focused663 failed10 (six old intermediate-route
assumptions, one cooperative retry observation, two live copied timeout callbacks and
one missing page-safe diagnostic). A build found three new fixture compile errors
(nonexistent enum and two missing imports), corrected against actual types. The next
704 failed3 because page gate release was incorrectly treated as ignored-provider
cleanup completion. Expanded724 failed1 cooperative, then2 ignoring variants due to
observing a replacement gate; the exact per-owner virtual-clock fixture resolves it.
A later724 failed1 backend-entry-versus-page-adoption assertion; it now awaits the
existing gate helper without adding a render. First full5100 failed1 synchronous
composition-scope disposal; async disposal preserves all graph assertions. A later
current-source815 failed1 DOM-wait-versus-nonrendering-gate assertion; polling uses
the unchanged default wait. Final qualified815/5100 passes include every correction.
No existing test timeout, gate, exclusion, floor or dependency version was changed
by this bundle; the preserved external Builds catalog movement is recorded above.
An investigation shell also found python unavailable(exit127); scripts use python3.

Existing UI skips are zero. Historical Server25 DW1 ATDD skips remain unchanged and
were not rerun/reclassified by this UI bundle. Earlier interim successful815/5100/
browser runs remain separately recorded. EventStore Client sources changed externally
after one Debug build, so that graph was rebuilt and requalified. [Stable inputs](dw-166-validation-inputs.json)
prove the final current-source boundary, rather than using an old binary as evidence.

## Remaining release/provenance blockers and bounded closure

`dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false`
exits1 on the two pinned SignalR10.0.12 NU1903 errors. Strict package-only UI,
`dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -p:HexalithFrontComposerFromSource=false -warnaserror -m:1 -nr:false`,
exits1 on the seven existing TenantSummaryProjectionPage scope API gaps. Maintained
UI retains its existing FrontComposer source exception and advisory policy; those
passes do not clear the blocked gates. Maintained graphs were rebuilt afterward.

Exact unchanged `python3 scripts/validate-story-gitlinks.py <spec>` commands for B6,
B8, DW-517, DW-105/DW-108, DW-112, DW-150, DW-164/DW-183 and this bundle all exit1.
Latest B6/B8 and DW ranges all report six undeclared movements each.
The earlier five-path B6/B8 and single inherited EventStore findings remain in the
historical readiness prefix. Latest Builds/Commons/EventStore/FrontComposer/Memories/Platform movements are
external, including the earlier dirty EventStore checkout subsequently recorded
by external activity.
No baseline narrowing, pointer reset, false ownership declaration or bypass ran.

Prior raw-secret-scan/CI qualification, seven authority/recovery/governance decisions,
nested-layout/startup/provider qualification, current-candidate NFR10/performance,
hosted TLS/browser, production authority/revocation and authenticated browser/
clipboard/human AT holds remain as recorded. Local isolated Aspire start/describe
reached Running with security Healthy and exact-target stop passed; initial
Waiting/Unhealthy TLS observations remain in the logs. No nested dependency was
initialized to satisfy blocked validation. Local health is not release readiness.

Canonical ledger/index audit retains all643 IDs:327 done/316 open across282 index
rows; reconciled123 done/228 actionable/15 blocked/26 awaiting-decision/nine accepted
limitations/38 uncertain. Only DW-166/DW-173 close. Related DW-176/DW-184 have current
wiring/verification notes and remain open. All other records, original findings,
settled decisions and archive/sprint tracking remain. [Ledger audit](dw-166-ledger-validation.json)
and preservation capture verify those boundaries and the exact historical readiness
prefix. No stage, commit, push, branch, dependency/version/gitlink update, deployment,
production replay or data migration was performed by this run. External commits and
movements are preserved explicitly, rather than attributed to this bundle.

**Next selected bundle: DW-176/DW-184**, separately reconcile and qualify workspace
consumer acceptance against the now implemented shared policy. DW-121 remains
Builds-owned. This closure is bounded local UI evidence, not full release approval.
