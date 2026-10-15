# DW-164/DW-183 validation evidence — 2026-10-15

**Only DW-164/DW-183 close in the validated working tree. Release remains not ready.**

## Ownership, entry state and contract

Owning repository: Hexalith.Tenants, `main`, full entry/current HEAD
`51f404513f2657e98f949f58f0df43b99b68a7d0`. EventStore retains the inherited external
checkout `241652627a52e47c189b2b052db407cec172f813`, while Tenants records
`24d719292b06d8a9d09771d837f8dd4d8d9b8997`. No movement belongs to this bundle.
[Entry capture](dw-164-entry-state.json) records all ten repositories' branches,
full HEADs, history/remotes, working/index changes, recorded/index dependency
pointers, nested initialization states and inherited file hashes. It captures
3458 owning files before this bundle's artifacts/source edits.

AGENTS.md, the permitted root-declared AI.Tools baseline and UX/state/Git rules,
effective build/editor/Git configuration, architecture, both ledger records,
maintenance/readiness and DW-150 evidence were inspected. The allowed repository
skill renderer ran exactly once:
`uv run --no-cache "/home/administrator/projects/hexalith/tenants/_bmad/scripts/render_skill.py" --project-root "/home/administrator/projects/hexalith/tenants" --skill "/home/administrator/projects/hexalith/tenants/.agents/skills/bmad-build"`.
The absolute rendered workflow's small-change route and independent Blind Hunter
review were followed. Explicit preserve-dirty-work/no-commit instructions override
its dirty-tree halt and commit step. No skill was discovered or loaded under the
repository's references/. The allowed local Aspire skill supplied lifecycle checks.
Persistence rules were read; no domain persistence changed.

[Pre-implementation contract and complete review triage](spec-dw-164-183-refresh-subscription-handoff.md)
record the decisions before production edits. DW-183 explicitly duplicates DW-164;
both current records describe the same flag/wakeup defect. Story 1.10's exact
projection/scope, reference-counted lease, captured generation, late-setup cleanup,
coalescing and three-failure retry contract owns this work. Story 1.11 reviews
at80/427 record its original/repeated deferral;412–416 preserve atomic ownership,
Empty cleanup and fixed-code diagnostics. The source still had that defect.

The corrected pre-fix Fluent fixture holds setup owned by OnAfterRenderAsync,
whose completion does not automatically render. Twelve of14 cases fail against
exact captured entry source: missing incoming-route wakeups, returning-route
callback acceptance and caller transitions. Invalid-route/disposal already pass.
This is executable evidence in addition to source inspection. The exact entry
source was used only for this red check; DW-150's inherited source was preserved.

## Implementation and regressions

Only subscription ownership/invalidation in TenantDetailPage changes. Setup,
adoption and completion handoff run on the renderer, retaining the atomic gate.
After an obsolete attempt completes and releases that gate, the latest authorized
scope is evaluated directly. A route epoch and captured caller version reject
old setup and callbacks, including A→B→A and caller return. Caller invalidation
detaches only this page's reference-counted lease. Old success, Empty and thrown
completion clean up; old failures do not spend the incoming tenant's budget.
Successful setup is single-instance; disposal initiates no new setup/read.

The existing three-failed-attempt budget, route/explicit-refresh recovery, exact
`tenants` projection and tenant literal, setup `CancellationToken.None`, support-safe
cleanup/diagnostics and coordinator implementation remain intact. Current failures
still wait for later renders or explicit recovery; this adds no eager failure loop.
DW-166/DW-173 finite timeout/cancellation work stays open and separate. DW-150's
combined reads, independent evidence/ETags, cursor/history, commands/lifecycle and
focus behavior remain unchanged.

Thirty-two new actual Fluent cases use real TenantReadRefreshSubscription and
held asynchronous backend setup. They cover obsolete success/Empty/throw,
repeated/returning routes, two successively obsolete held owners, same-route member
paging/combined/detail refresh, exact current failure exhaustion/recovery,
pending/changed/returned callers, sign-out, invalid route and disposal for all
three completion outcomes. Shared-consumer and held-unsubscribe cases verify
reference ownership, immediate private-row removal, current reads during cleanup
and safe replacement. Assertions include callback registries/installed leases,
exact cleanup, inert copied obsolete callbacks, independent ETag-bearing requests,
retained Refreshing evidence, actual rendered member/configuration changes and
final snapshots. The copied-callback check compares relevant reads and unchanged
snapshot references within one renderer operation, avoiding supplementary-read races.

Independent review found six evidence gaps; every one is individually triaged and
patched in the spec. Final independent recheck passes32/32, zero errors/failures/
skips/not-run, with no remaining finding or deferral. Production needed no further
change after the original patch.

## Commands, results and initial failures

[Command results](dw-164-command-results.json) retain exact argv/cwd, exit codes,
durations and outputs, including independent runs. Early wrapper timestamps are
labelled completed_utc; subsequent started_utc timestamps record command start.
Aspire parameter/environment/login values are redacted; resource state/health and
endpoints remain. [UI summary](dw-164-ui-summary.json) retains all initial XML
failure identities, final counters, zero skip identities, browser output and hashes.

| Lane | Result |
| --- | --- |
| Debug source-reference and maintained Release UI builds | Exit0; zero compiler errors; unchanged SignalR advisory warnings retained |
| Final focused detail/subscription classes, Debug and Release | 364/364 each, zero failures/errors/skips/not-run |
| Final unfiltered per-project MTP UI, Debug and Release | 5036/5036 each, zero failures/skips |
| Independent final new-case run | 32/32, zero failures/errors/skips/not-run |
| Unchanged maintained Chromium runner, Debug and Release | Exit0 both, original scenarios and negative controls retained |
| Whitespace, ledger/index and preservation audits | Exit0 |

Builds use `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false`
and the maintained Release equivalent with `-p:UseHexalithProjectReferences=false`.
Focused runs use `dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantDetailSurfaceTests' -class '*TenantReadRefreshSubscriptionTests' -xml /tmp/hexalith-dw164/focused-debug-complete.xml`
and the Release equivalent. Full commands use
`dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-ansi --no-progress --results-directory /tmp/hexalith-dw164/mtp-debug-complete`
and the Release / mtp-release-complete equivalent. These are individual projects,
not solution-level dotnet test or unsupported MTP --filter selectors.

Browser commands are `env TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh`
and the Release equivalent. They retain focus/navigation, correction, viewport/
forced-colors, admission, identity, native EN/FR names/focus and negative controls.
Gates, exclusions, coverage floors, real/virtual timeouts and package versions are
unchanged. Static Fluent fixtures do not establish authenticated topology, live
provider, clipboard or human AT qualification.

All initial failures remain visible. The first9 cases held initial lifecycle setup
and passed6 because OnParametersSet completion can render automatically; three
exposed return-route budget behavior. The next9-case fixture exhausted setup's
budget before holding and failed all9. One fixture build used an incorrect component
name (two CS0246 errors); it was corrected to the existing MemberAccessReview
callback. The corrected14-case baseline fails12. The first fixed16-case run fails
three cleanup assertions that included initial failure cleanup; scoped exact release
assertions correct them. An initial full5020-case run and later5036-case run pass
before the final synchronization refinement; they are retained as interim evidence.

Independent review's first32-case correction run fails one pending/Empty copied-
callback assertion; its unchanged18-case repeat passes. A focused364-case run also
fails one suspended-cleanup assertion. Both counted all gateway calls across an
asynchronous hop, permitting supplementary reads to enter the comparison. Renderer-
atomic detail/member counts plus unchanged snapshot assertions correct the fixture.
Final independent/focused/full qualification includes that correction. The initial
shell inspection also found `python` unavailable (exit127); maintained scripts use
`python3`. No timeout, gate or production policy was weakened for a failure.

Existing UI skips are zero. Historical DW-517 Server25 DW1 ATDD skips remain
unchanged, and were not rerun or reclassified by this UI-only bundle.

## Remaining blockers, preservation and closure

`dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false`
still exits1 with two NU1903 errors on pinned SignalR10.0.12. Strict package-only UI,
adding `-p:UseHexalithProjectReferences=false -p:HexalithFrontComposerFromSource=false`,
still exits1 with seven existing TenantSummaryProjectionPage scope API gaps.
Maintained UI uses its unchanged FrontComposer source exception; it does not qualify
these blocked gates. The maintained graph was rebuilt successfully afterward.

Exact historical story-gitlink commands retain B6/B8's five inherited failures each;
DW-517, DW-105/DW-108, DW-112, DW-150 and this bundle retain the inherited external
EventStore checkout movement failure. No baseline narrowing, pointer reset, false
ownership declaration or validator bypass ran. Raw secret-scan/CI wiring, seven
authority/recovery/governance decisions, nested-layout/startup/provider qualification,
current-candidate NFR10/performance, hosted TLS/browser, production authority/
revocation and authenticated browser/clipboard/human AT holds remain. Local isolated
Aspire startup/resources reached Running with security health Healthy and exact-target
stop passed; initial Waiting/Unhealthy TLS startup observations are retained. No
nested dependency was initialized, and local health does not establish release readiness.

[Preservation](dw-164-preservation.json) checks every original repository identity,
index, recorded/checked-out dependency pointer, inherited dependency diff/status/hash,
unrelated owning hash, every preexisting detail-test byte and inherited page content
outside the subscription edit, plus the exact original readiness prefix. B1–B8,
DW-517, DW-105/DW-108, DW-112, DW-150, external commits/work, archive/sprint, gates and
unresolved decisions remain intact. No stage, commit, push, branch, dependency update,
umbrella gitlink edit, deployment, production replay or data migration ran.

Only DW-164/DW-183 change disposition. All643 IDs remain:325 done/318 open,
284 maintenance rows; reconciled121 done/230 actionable/15 blocked/26 awaiting-
decision/nine accepted limitations/38 uncertain. [Ledger/index audit](dw-164-ledger-validation.json)
retains the204 earlier closed-record boundary, all original findings/duplicate
relationship, every unrelated record/action row and exact open-ID coverage.

**Next selected bundle: DW-166/DW-173**, shared configured finite resolver/subscription
bounds and replaceable cancellation, preserving their settled decisions and
three-failure accounting. Current GlobalAdministratorsPage still subscribes with
CancellationToken.None and resolves authorization with optional/default cancellation;
workspace authority resolution remains unbounded. DW-176/DW-184 stay separately
tracked dependent consumers. DW-121 remains Builds-owned. This handoff closure is
bounded local evidence, not full release readiness.
