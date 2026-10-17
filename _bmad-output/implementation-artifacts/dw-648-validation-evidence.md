# DW-648 validation evidence

Entry: `18b0f54578982cb16b4170013885a8632f8f3726`, clean `main`. Scope is the separate ambiguous `RequestSent` member-removal retry path; the earlier DW-545 repair and historical evidence remain intact. All commands run from the Tenants repository. Source/test files retain CRLF; Markdown retains LF. Logs and TRX files are under `/tmp/tenants-dw-648` unless stated otherwise.

## Reproduction and correction

The new theory first performs an actual ambiguous delivery, asserting `RequestSent`, the exact message ID, null correlation, and a real attempt start time. It holds the retry's second authority read. Pending variants change/restore confirmation or freshness before releasing the provider. Queued variants hold the renderer until a successful authority helper has returned, then change/restore safety before the continuation consumes the result. A gated transient gateway resolver prevents the original off-renderer continuation dispatching before the safety transition; it also detects off-renderer gateway resolution independently of dispatch count.

Unchanged runtime: **two passed, two failed, zero skipped, exit 2**. Both queued-success variants incorrectly dispatch a second removal; both pending-authority variants refuse. This is a controlled component reproduction, not a production-incident claim.

The private retry now resumes its admission await on the renderer and rejects a mismatched admission generation before existing tracker/attempt/current-safety checks. No public API, resource string, timeout, dependency, dispatched-work cancellation, or tracking policy changes.

Initial fixed ordering theory: **4/4 passed, zero failures/skips, exit 0**. Four further cases refuse denied, indeterminate, faulted and canceled retry authority, preserve original identity and aggregate ownership, then reauthorize and reach Accepted with the original message ID. The status read remains distinct from projection confirmation; Accepted stays Accepted.

Expanded Debug source build: **zero warnings/errors, exit 0**. Full removal class: **229/229 passed, zero failures/skips, exit 0**. Initial full UI: **6251/6251 passed, zero failures/skips, exit 0**, 2m00.784s. Later review adds eleven cases, requiring a final full-UI acceptance run before closing DW-648.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --nologo -v:q > /tmp/tenants-dw-648/baseline-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*RemoveTenantMemberFlowTests.AmbiguousRemovalRetryRejectsChangedAdmissionGeneration' --minimum-expected-tests 4 --no-ansi --no-progress --results-directory /tmp/tenants-dw-648/baseline --report-xunit-trx --report-xunit-trx-filename baseline.trx > /tmp/tenants-dw-648/baseline-tests.log 2>&1
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-restore --nologo -v:q > /tmp/tenants-dw-648/fixed-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*RemoveTenantMemberFlowTests.AmbiguousRemovalRetryRejectsChangedAdmissionGeneration' --minimum-expected-tests 4 --no-ansi --no-progress --results-directory /tmp/tenants-dw-648/fixed --report-xunit-trx --report-xunit-trx-filename fixed.trx > /tmp/tenants-dw-648/fixed-tests.log 2>&1
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-restore --nologo -v:q > /tmp/tenants-dw-648/expanded-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --minimum-expected-tests 225 --no-ansi --no-progress --results-directory /tmp/tenants-dw-648/focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-648/focused-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6251 --no-ansi --no-progress --results-directory /tmp/tenants-dw-648/full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-648/full-tests.log 2>&1
```

## Runtime baseline

The Aspire skill started this task's isolated AppHost before code edits and inspected resources/telemetry. Initially 13 resources were Waiting and ten Running; the later describe reports 16 Running and seven Finished Dapr helper resources, with the security readiness check Healthy. Returned dashboard login links and ordinary telemetry remain in local logs rather than this artifact. This baseline does not establish a live browser reproduction, Release/package acceptance, or release readiness.

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --non-interactive --format Json > /tmp/tenants-dw-648-aspire-start.log 2>&1
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/tenants-dw-648-aspire-describe.json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --limit 5 > /tmp/tenants-dw-648-aspire-otel.json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/tenants-dw-648/aspire-ready.json
```

## Review and preservation

The workflow's configured Blind Hunter review returned five verified qualification gaps. All are classified medium/patch and individually recorded in the spec's Review Triage Log. No other review layer is configured. No staging, commit, branch, push, dependency update, or deployment is required or performed.

Initial `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-648-ambiguous-removal-retry-admission.md` reports **FAIL** because the skill template did not include `baseline_commit`. Adding the exact entry revision to that frontmatter resolves the failure; the identical command then reports **PASS**, no dependency-pointer changes. The initial failure is preserved here rather than treated as acceptance.

## Independent review additions and recheck

Eleven additional regression cases close the five review gaps: a genuinely suspended successful retry through Accepted, ProjectionPending and Confirmed; pending/queued provider and authorization-version replacement with restoration; pending/queued tenant/member replacement; and pending/queued component disposal followed by retained-owner adoption. They assert no late dispatch or replacement snapshot corruption, original message/intent/start-time retention, aggregate ownership until matching projection, and separate audit availability.

Initial expanded build: **zero warnings/errors, exit 0**. Initial expanded focused run: **238 passed, two failed, zero skipped, exit 2**, 6.764s. Both failures use bUnit wrapper cleanup inside a held renderer without first invoking the component disposal boundary. The fixture now explicitly calls the component's `IDisposable` before remounting, as existing disposal tests do; the runtime fix is unchanged. This preserves the disposal requirement rather than weakening its assertions.

Corrected Debug source build: **zero warnings/errors, exit 0**. Corrected full removal class: **240/240 passed, zero failures/skips, exit 0**, 4.424s. Independent read-only review rechecks all five findings and accepts their coverage; no unresolved finding or new deferral remains. Final full UI: **6262/6262 passed, zero failures/skips, exit 0**, 1m39.036s. All nineteen new cases pass. DW-648 is accepted and closed.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-restore --nologo -v:q > /tmp/tenants-dw-648/review-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --minimum-expected-tests 240 --no-ansi --no-progress --results-directory /tmp/tenants-dw-648/review-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-648/review-focused-tests.log 2>&1
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-restore --nologo -v:q > /tmp/tenants-dw-648/review-corrected-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*RemoveTenantMemberFlowTests' --minimum-expected-tests 240 --no-ansi --no-progress --results-directory /tmp/tenants-dw-648/review-corrected --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-648/review-corrected-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6262 --no-ansi --no-progress --results-directory /tmp/tenants-dw-648/full-final --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-648/full-final-tests.log 2>&1
```

## Final preservation and limits

Preservation validator: **PASS**. All 648 ledger IDs remain; only DW-648 changes among record bodies, and its replaced fields are retained under `pre-dw-648-` keys. Counts are 355 done, 206 actionable, 15 blocked, 26 awaiting-decision, nine accepted limitations and 37 uncertain: 293 open. The remaining 264 maintenance rows cover every open ID exactly once, and all unrelated rows remain byte-for-byte identical. Archive/sprint, `.gitmodules`, entry HEAD and empty staging are unchanged; source/test line endings are CRLF. Story-gitlink validation and `git diff --check` pass; Git emits its existing CRLF normalization notices without whitespace errors. Task-owned isolated Aspire stops successfully; other AppHosts are outside this lifecycle operation.

```bash
python3 /tmp/tenants-dw-648/verify-preservation.py > /tmp/tenants-dw-648/preservation.json
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-648-ambiguous-removal-retry-admission.md
git diff --check
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive > /tmp/tenants-dw-648/aspire-stop.log 2>&1
```

Only DW-648 closes. Original findings, earlier failed controls, unrelated records, archive/sprint, dependencies and release holds remain intact. This focused component repair establishes neither production-incident reproduction nor browser/Release/package qualification. Release remains not ready. No commit, staging, branch, push, dependency update or deployment ran.
