# DW-234/DW-251/DW-316 command localization — 2026-10-16

The implementation closes only the three selected localization IDs. Release remains
not ready. The existing spec is the sole source of intent; its context list is
empty and its frozen section is unchanged. Independent acceptance and three review
layers completed; all accepted corrections and their verification are recorded
in the owning spec.

Eighty paired whole-message resources cover configured validation,
submission, recognized rejections and all status/unavailable/default paths.
The fourteen former model types have separate type-named files with public
signatures intact. Three keyed status factories are additive. Five unknown-status
arms keep the current key or the localized unverified fallback; zero-event role
completion uses localized already-applied copy. Progress clears old failure copy.
Existing consumer overrides and stronger membership-expiry guidance survive.
English compatibility strings remain fixed and safe; backend details never become
visible copy or resource keys. Dispatch/retry, classification, identity, flags,
admission, proof, audit and timeout conditions remain unchanged.

[Exact command records](dw-234-251-316-command-results.json) retain argv, cwd, UTC times,
duration, exit codes, full output and output hashes, including initial failures.
[Test summary](dw-234-251-316-ui-summary.json) includes XML/output hashes, all 80 paired
keys and the fourteen-type body comparison. [Final inputs](dw-234-251-316-validation-inputs.json)
bind 697 source/config/browser inputs, compiled outputs, repository HEADs/branches and
preserved archive, sprint and unchanged browser-runner bytes. All nine dependency
worktrees were clean at capture. No stage/commit/push, dependency/gitlink update,
recursive initialization, deployment, replay or migration ran.

| Final lane | Result |
| --- | --- |
| Debug source and maintained Release individual UI builds | Both exit 0; zero compiler errors |
| Built focused gateway/state/rendering/parity/consumer/proof/audit suites | 1,825/1,825 each; zero failures/errors/skips/not-run |
| Full individual-project UI MTP | 5,532/5,532 each; zero failures/skips |
| Unchanged maintained Chromium runner | Both exit 0; original assertions and negative controls |
| Current story-gitlink validator | Exit 0 against the original spec baseline |

The 164 new cases cover every concrete unavailable and interface-default entry,
operation-specific submission classifications with retained identity/flags,
recognized shared neutral rejections, transport/timeout/malformed/mismatch/unknown
status, stale-key replacement, progress clearing, keyed factory defaults, membership
expiry precedence and actual EN/FR DOM/live regions. Snapshot-only unknown-status
fallback and zero-event role completion have direct real-resource rendering cases.

Initial split/import and fixture compile failures were corrected without changing
gates. An invalid multiple-middle-wildcard xUnit filter was rejected before any
case ran; final filters use only supported prefix/suffix wildcards. The first
focused 1,280-case run passed 1,278 and failed two new unavailable/default checks.
They exposed a missing newly added grant-configuration resource and the inherited
missing tracked-removal resource; both now have paired EN/FR messages. The initial
XML remains under `/tmp/hexalith-dw234-251-316/focused-debug-initial-failures.xml`
with its hash retained in the summary. The earlier 137-case addition was expanded
with six direct snapshot-rendering cases before initial qualification (143 new, 5,511 total).
Review corrections then added 21 cases for plain transport/JSON/cancellation and actual
member-flow rendering, giving 164 added cases over the inherited 5,368-case suite.
Pre-review qualification and its original input/output hashes remain in the JSON records.

Exact final command shapes (complete class filters are in command records):

```sh
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release -p:UseHexalithProjectReferences=false -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug --no-build --no-ansi --no-progress --results-directory /tmp/hexalith-dw234-251-316/review-mtp-debug
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-build --no-ansi --no-progress --results-directory /tmp/hexalith-dw234-251-316/review-mtp-release
env TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
env TENANTS_BROWSER_BUILD_CONFIGURATION=Release bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-234-251-316-command-localization.md
```

The parent used the permitted local Aspire skill before edits. The isolated
AppHost reached healthy tenants-ui in 17.7 seconds, with 22 resources Running/Healthy;
memories-dapr-cli finished after its Dapr watcher reported no space left on device.
The owned instance was stopped by exact target. Raw startup/state/telemetry/stop
outputs remain in `/tmp/hexalith-dw234-251-316/aspire-*`. This qualified baseline
requires no localization-scope orchestration change and establishes no deployment,
authenticated provider, clipboard, denial or human-AT acceptance.

The broad Release SignalR 10.0.12 NU1903 errors and strict package-only UI
projection-scope API blockers remain in [DW-247 evidence](dw-247-validation-evidence.md).
They were not rerun here. Maintained Release retains its existing FrontComposer
source exception/advisory policy; dependencies and all gates remain unchanged.
Historical provenance, raw security-scan/CI, authority/recovery/governance decisions,
upstream/provider and hosted TLS/current-candidate performance/authenticated browser/
clipboard/denial/human-AT qualification holds remain. Other project suites were not
rerun for this UI scope.

Only DW-234/DW-251/DW-316 and their index rows close. All 643 IDs remain: 336 done/
307 open, 275 exact-covering rows; 132 reconciled done/219 actionable/15 blocked/
26 awaiting-decision/nine accepted limitations/38 uncertain. Readiness remains seven
decision holds, 112 follow-up and 188 qualification/triage records. All unrelated
ledger blocks, original findings/pre-closure history, archive, sprint and historical
readiness prefix remain intact. DW-323 stays separately scoped; mechanical model
splitting claims no unrelated deferred-work closure. Full release readiness is not
established by these maintained passes.

## Independent review and corrected qualification

Three context-free review layers completed. All eleven filed findings were triaged
individually in the spec: nine Blind Hunter corrections and the overlapping transport
verification gap were accepted; the blank-key guard suggestion was rejected as low
impact because configured producers use checked nonblank constants. Edge Case Hunter
found no issue. No spec loopback, unrelated closure, or deferred entry was needed.

Corrections restore the unavailable-status compatibility sentence, retain fixed safe
unknown-status fallbacks, give configuration gateway failures two matching whole-message
resources, pin exact per-operation/default keys, add transport/JSON/cancellation and
EN/FR member-flow checks, preserve the existing role-state announcement policy, and
document the extracted status DTO. The correction-only Debug build and affected suites
passed 654/654 before parent qualification. Its initial fixture compile correction is
retained in the command records.

The parent repeated the exact required builds, focused suites, full per-project MTP
and unchanged browser runners for Debug and maintained Release. All passed: 1,825
focused and 5,532 full UI cases per configuration, no failures/errors/skips/not-run.
The first corrected Debug Chromium attempt completed its assertions, then exited 1
while Node removed its temporary profile (`ENOTEMPTY` for `profile/Default`).
The exact unchanged rerun passed; both outputs and exit codes remain recorded.
No browser assertion, cleanup code, timeout, or gate was changed.

The 697 corrected source/config/browser inputs and all six compiled output hashes
were verified unchanged through qualification. Both prior and corrected runs, inputs
and output hashes remain recorded; only corrected results qualify the final tree.

The independent reviewer rechecked every accepted correction and found no remaining
concrete defect. The spec is done; the explicit no-commit constraint is preserved.
