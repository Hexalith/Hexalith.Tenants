# DW-247 bounded membership projection recovery — 2026-10-16

Only DW-247 closes. Release remains not ready. This pass resumed the existing
in-progress [spec and review log](spec-dw-247-bounded-membership-projection-recovery.md)
on clean Tenants entry `6de661e129ff6f6f0c19795fcb1d032abee25d1d`. The user had
already committed the implementation and its first review corrections. The original
implementation baseline `a3f43655d68370389439c7d2c212ab7939bd164e` remains in the
spec; no history or baseline was rewritten to attribute inherited work to this run.

The permitted local bmad-build renderer ran exactly once for this invocation. Its
existing-spec oneshot route was followed, including an independent Blind Hunter
review and correction recheck. Repository baseline, architecture AD-7/AD-12, tracked
build/editor/test guidance, UX instructions and project context were loaded. No
skill was discovered, loaded or executed under `references/`. The frozen scope
prohibits staging, committing, pushing and dependency/gitlink mutation; it remains
unchanged. No sprint story transition applies.

## Qualified behavior

The settled August 25/26 decision is implemented for all three retained membership
flows: add member, change role and remove member. Deployment configuration
`Tenants:Ui:MembershipProjectionTimeout` defaults to five minutes and accepts
positive invariant TimeSpan values through `49.17:02:47.294`. Invalid settings fail
before an attempt can own admission. The circuit-scoped tracker captures the setting
when constructed; changing configuration does not alter an existing tracker.

A monotonic deadline starts once at first ProjectionPending. Elapsed time includes
held status/parent/projection calls, failures, detached renderers and remounts.
Refreshes, coalescing, notifications and recovery do not restart it. Expiry presents
UnableToVerify with whole-string EN/FR retry-status, read-only and support guidance.
It performs no I/O, cancellation or redispatch, invents no terminal outcome and
retains the original intent, baseline, exact command identity, evidence and aggregate
admission. Accepted-only and delivery-ambiguous attempts keep their existing policy.

Only exact authoritative intended projection evidence confirms, including qualified
results held across or retrieved after expiry. Expiry-only comparisons permit those
late results while other evidence/caller/generation changes still reject obsolete
publication. Temporary status failures preserve the original elapsed budget; unknown,
unverified and malformed status cannot become proof. Continue read-only and surface
disposal detach rendering while keeping unresolved dispatched ownership. Other
aggregates remain available. Terminal evidence stops the timer; tracker disposal
stops timers/observer claims without fabricating outcomes or releasing unresolved
admission. Removal keeps its committed sequence, caller proof and separate audit
lifetime. DW-245/DW-265/DW-290 queued refresh-intent drains remain byte-identical.

## Review and corrections

The prior ten-finding review and its corrections remain in the spec. The fresh
context-free reviewer compared original DW-247 entry copies with current production
and tests: 103,199 bytes / 1,024 = 100.780 KiB, giving a finding floor of ten. Each
finding is triaged separately in the spec.

Three small corrections were applied and independently rechecked:

- EN/FR timeout guidance no longer asks for a command reference absent from these
  screens. Raw tracking identifiers remain hidden.
- The actual Fluent test click helper asserts both component and rendered control
  are enabled before invoking recovery callbacks.
- Quickstart gives the concrete timer limit and configuration-capture lifetime.

Seven optional coverage suggestions were rejected with inspected evidence and no
demonstrated production defect. Current locked callback ownership, elapsed-time
implementation, exact held-operation outcomes, disposal/replacement and real DI
composition are covered or directly inspected. Specialized callback/clock/thread
scaffolding would add work beyond a simple correction. No new defect was deferred.
The reviewer executed no tests; all execution results below come from the parent.

## Validation and initial failures

[Initial command records](dw-247-command-results.json) and
[completion command records](dw-247-completion-command-results.json) preserve exact
argv, cwd, UTC times, duration, exit code and output. [Test summary](dw-247-ui-summary.json)
retains direct XML hashes, identities and failures, every MTP result and browser
output hashes. Earlier compile/fixture failures and the initial 164-case run with
33 failures remain in the original records; its corrected 168-case run passed.
The final implementation contains 172 dedicated cases: 57 state and 115 actual
Fluent component cases. No prior failure was discarded or replaced by a final pass.

Initial completion Debug full UI passed 5,368/5,368. Initial maintained Release
passed 5,367/5,368, failing only
`RealChromiumFocusValidatorStopsWithinItsBoundWhenChromiumHangs` at its unchanged
15-second outer wait while the isolated app was starting. The focused recheck after
app cleanup passed 1/1 in 6.6 seconds. Corrected full Debug and Release runs then
passed with all timeouts and assertions unchanged. No causal production defect is
inferred solely from the timing failure.

The initial inherited final-input snapshot predated corrections to one test file.
It was not accepted as final evidence. The corrected qualification capture includes
RESX resources and browser HTML omitted by the earlier source-only list. All 6,753
inputs remain unchanged through final qualification. [Final hashes](dw-247-validation-inputs.json)
bind the corrected sources/configuration/resources/templates, compiled outputs and
generated Fluent/CSS/assets browser inputs. The initial completion snapshot remains
separate; the added coverage and corrected source hash are explicit.

Final qualification uses the maintained UI source exception in Release and source
references in Debug. Existing SignalR advisory warnings remain visible.

| Lane | Result |
| --- | --- |
| Corrected Debug source build / maintained Release build | Both exit 0; zero compiler errors |
| Focused membership/proof/retained/guard cases | 657/657 in each configuration; zero failures/errors/skips/not-run |
| Full individual-project MTP UI | 5,368/5,368 in each configuration; zero failures/skips |
| Unchanged maintained Chromium runner | Both configurations exit 0; original assertions and negative controls retained |
| Independent review/correction recheck | Three corrections confirmed; no concrete remaining regression |
| Current DW-247 story-gitlink validator | Pass with the original baseline; no references pointer changes |
| Ledger/index/archive/sprint preservation | Only DW-247 changes; 643 IDs, 333 done / 310 open, 278 exact-covering rows |

Exact final command shapes (complete filters, argv and output are in completion records):

```sh
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-ansi --no-progress --results-directory /tmp/hexalith-dw247-completion/mtp-qualified-debug
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-ansi --no-progress --results-directory /tmp/hexalith-dw247-completion/mtp-qualified-release
env TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
env TENANTS_BROWSER_BUILD_CONFIGURATION=Release bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-247-bounded-membership-projection-recovery.md
```

The permitted local Aspire skill supplied isolated startup, state/telemetry inspection,
a healthy tenants-ui wait, all 23 resources Running/Healthy and exact-target stop.
Startup initially showed waiting resources and unhealthy security. This health check
and rendered Chromium fixtures do not establish authenticated provider, deployment,
clipboard, denial-path or human assistive-technology acceptance. The original
implementation's earlier Aspire evidence remains preserved too.

## Remaining blockers and preservation

Original broad-gate evidence remains blocked:

```sh
dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -p:HexalithFrontComposerFromSource=false -warnaserror -m:1 -nr:false
```

The first command exits 1 with two pinned SignalR 10.0.12 NU1903 errors. The second
exits 1 with seven existing TenantSummaryProjectionPage projection-scope API errors.
These exact commands/results remain in the initial command records. Maintained UI
passes retain the existing FrontComposer source exception/advisory policy and clear
neither blocked gate. Dependencies were not updated and no audit, gate, exclusion,
coverage floor or timeout was relaxed.

Historical owning Story 2.1/2.4 and B6/B8/DW-517/DW-105/DW-108/DW-112/DW-150/
DW-164/DW-183/DW-166/DW-173 story-gitlink attribution failures remain recorded.
Current DW-247 provenance passes without changing its original baseline. No pointer
reset or false attribution was made. Raw secret-scan/CI, authority/recovery/governance
decisions, upstream/provider and hosted TLS/current-candidate performance/authenticated
browser/clipboard/denial/human AT qualification holds remain. Other project suites,
including historical Server DW1 skips, were not rerun for this UI completion.

[Completion preservation](dw-247-completion-preservation.json) verifies all ten
repository HEADs/branches, unchanged dependency worktrees/pointers, archive and sprint
bytes, the frozen intent, queued-intent drain bytes and the exact historical readiness
prefix. The original [implementation preservation](dw-247-preservation.json) remains
untouched. Only the selected ledger block and index row close; original findings,
legacy detail, settled decisions and pre-closure verification/actions remain.
[Ledger audit](dw-247-ledger-validation.json) verifies all 643 IDs: 333 done / 310 open,
with 278 index rows covering each open ID once. Reconciliation counts are 129 done,
222 actionable, 15 blocked, 26 awaiting decision, nine accepted limitations and 38
uncertain. Readiness remains seven decision holds, 115 follow-up and 188 qualification/
triage records. All prior B1–B8 and DW closures and related DW-248/DW-266/DW-267/DW-323
remain unchanged.

Next selected bundle: **DW-234/DW-251/DW-316 shared command failure/status localization**.
Current default snapshot and configured/unavailable gateway branches still return raw
English on French paths. DW-323 remains separately scoped; DW-121 remains Builds-owned.
No branch, stage, commit, push, dependency/version/gitlink update, nested initialization,
deployment, production replay or data migration ran during completion. This scoped
closure does not establish full release readiness.
