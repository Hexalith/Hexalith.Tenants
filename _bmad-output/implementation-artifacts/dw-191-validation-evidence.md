# DW-191 validation evidence — 2026-10-21

Entry revision: `6389595745e5c3a7fec705f47f759a963109a87e`, clean main. User authorizes the next deferred item and push on main. DW-188 through DW-190 are already done; DW-191 is selected after DW-187. Earlier open work retains its recorded owning-module and qualification dependencies.

## Contract and baseline

A matching projection notification can arrive during the initial Loading render, before MemberAccessReview attaches. Restored member attempts previously copied their retained snapshot without status reconciliation. Keep a coalesced tenant/read-generation intent until the current rendered child has adopted its attempt under current membership authority. Keep mounted-flow forwarding in its existing callback context, preserving manual-refresh recursion protection. Ready, Stale and Degraded body states remain eligible; existing authoritative evidence determines whether confirmation is permitted. Replay consumes intent before yielding, checks current caller/tenant/generation and child detail/authorization version, and contains optional provider failures. No command redispatch, API, dependency, persistence, localization or topology change.

This is rendered component and state acceptance evidence; it does not establish authenticated browser, distributed functional or release qualification.

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json --non-interactive
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --severity Error --limit 5 --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
```

All exit 0. Initial ps is empty. The early describe sample has 23 resources, 10 Running and 13 Waiting; the bounded telemetry sample contains two error records. Only the app started by this task was stopped. Raw local output is under `/tmp/dw191-aspire-*`; credentials and telemetry contents are excluded from tracked evidence. These samples do not prove app health or functional acceptance.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

Baseline and initial fixed-code builds exit 0 with zero warnings/errors (`/tmp/dw191-build-baseline.log`, `/tmp/dw191-build-green.log`). The first test-only build has three CS0103 errors from a missing enum namespace; adding its existing state namespace fixes them before the reproduction (`/tmp/dw191/build-early-red.log`, `build-early-red-fixed.log`). No build gate is weakened.

## Reproduction and initial fix

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*EarlyMembershipNudge*' --minimum-expected-tests 12 --no-ansi --no-progress --results-directory /tmp/dw191/early-red --report-xunit-trx --report-xunit-trx-filename results.trx
```

Entry production exits 2: 12 failed, zero passed/skipped. All cases establish an actual subscription while the initial detail read is held, with no rendered member component and null page ref. Delivering the real matching notifier event renders the authoritative replacement, but the restored attempt stays Accepted. The matrix covers add, change-role and removal, advanced/unchanged projection versions, and immediate/held membership authority.

The same command against fixed production, using results-directory `/tmp/dw191/early-green`, exits 0: 12 passed, zero failed/skipped (`/tmp/dw191-tests-green.log`). Each case requires one retained-identity status lookup and a separate unconditional projection proof, verifies no redispatch, distinguishes Confirmed from ProjectionPending, checks tracker/admission ownership, then releases the obsolete initial read and proves it cannot replace current state.

## Provenance and commit validation

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-191-early-membership-nudge.md
node_modules/.bin/commitlint --version
node_modules/.bin/commitlint --edit /tmp/dw191-commit.txt --verbose
```

Initial own-spec gitlink guard exits 0 with no references pointer changes. Installed and locked commitlint CLI version is 21.2.3. Exact candidate `fix(ui): replay membership nudges after child attachment` followed by newline validates with zero problems/warnings (`/tmp/dw191-commitlint.log`). Remote fetch with submodule recursion disabled succeeds; initial main/origin divergence is 0/0. Final checks run after qualification.

## Initial full regression lane

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6761 --no-ansi --no-progress --results-directory /tmp/dw191/full-initial --report-xunit-trx --report-xunit-trx-filename results.trx
```

Exit 0: 6761 passed, zero failed/skipped (`/tmp/dw191-tests-full-initial.log`, `/tmp/dw191/full-initial/results.trx`). Further boundary tests and independent review follow this initial regression lane.

## Review corrections and intermediate results

The configured context-free Blind Hunter review identifies a lost-intent race when a second notification arrives after child mounting but before authority resolves. The mounted fast path now requires an authorized child; otherwise the newest read generation retains pending intent. Two advanced/unchanged regressions exercise that exact sequence. Mounted forwarding retains its original callback context to avoid recursive polling.

Review also requests fixed-reason failure observability and stronger ownership/evidence controls. A source-generated diagnostic preserves `notification-callback-failed` without provider, tenant or exception data. Status substitutes now assert exact message ID, correlation ID, tenant aggregate, domain and cancellation token. Further controls cover stale/degraded attachment with current versus non-authoritative command proof, failures before and after status publication, route/disposal boundaries and obsolete responses.

The expanded test build initially fails with two CS0103 errors from the viewport enum namespace; its existing Shell.State.Navigation namespace fixes them. A scoped formatter initially finds one whitespace issue, corrected only in the new test partial. The next Debug source build exits 0 with zero warnings/errors (`/tmp/dw191-build-reviewed-corrected.log`). The first 22-case run passes 20/fails 2, zero skips: two route controls wait for cancellation-source disposal through bUnit render notifications even though obsolete completion need not render. Replacing that test-only wait with bounded completion polling preserves the disposal assertion without depending on a render (`/tmp/dw191-tests-reviewed.log`, `/tmp/dw191/early-reviewed/results.trx`). Production cancellation behavior is unchanged.

The subsequent correction build exits 0 with zero warnings/errors (`/tmp/dw191-build-corrections.log`). Final focused/full results and independent correction recheck follow.

## Expanded focused qualification

The next 27-case run passes 25/fails 2 with zero skips (`/tmp/dw191-tests-corrections.log`): both failure-diagnostic assertions inspect NSubstitute's retained source-generated pooled log state after it has been cleared. A targeted two-case diagnostic probe confirms that Warning/MembershipNudgeFailed is emitted (`/tmp/dw191-tests-log-probe.log`), so the test switches to the existing synchronous TenantCorrectionDiagnosticLog sink and exact formatted-message assertions. No raw debug/provider output enters tracked evidence. The capture remains local to the tenant-detail logger category.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*EarlyMembershipNudge*' --minimum-expected-tests 45 --no-ansi --no-progress --results-directory /tmp/dw191/early-expanded --report-xunit-trx --report-xunit-trx-filename results.trx
```

Build exits 0 with zero warnings/errors (`/tmp/dw191-build-expanded.log`). Focused tests exit0: 45 passed, zero failed/skipped (`/tmp/dw191-tests-expanded.log`, `/tmp/dw191/early-expanded/results.trx`). The original 12 reproduction cases remain; additional cases qualify a second notification during held authority, disposed old Razor refs during a same-route reload, route/disposal before adoption, failures before/after status publication with fixed diagnostics and no automatic retry, Ready/Stale/Degraded attachment with authoritative/non-authoritative proof, denied/indeterminate/faulted authority, same-tenant caller replacement and status/proof responses after route/disposal or successor notification. The status substitute binds the exact tracking handle; all cases preserve no-redispatch and aggregate ownership/confirmation rules.

Existing MembershipRefreshIntentTests qualify nonterminal queued batches; the new in-flight notification controls qualify terminal coalescing with one status lookup and no overlapping replay. This change does not replace the existing per-flow coalescer.

Independent correction recheck accepts the original findings, then identifies a containment omission in the new optional diagnostic: service/factory/provider failure could itself escape the replay catch. A nested guard now contains the diagnostic operation, including resolution and logger construction. Two extra failure-matrix cases use a disposed logger factory and require unchanged retained evidence, renderer containment, no automatic retry and recovery on a later notification. Final independent recheck confirms the guard contains all three diagnostic failure sources and reports no unresolved finding. No new work is deferred. Final Debug source build exits 0 with zero warnings/errors (`/tmp/dw191-build-final.log`).

## Final source and closure checks

The pre-diagnostic-guard full lane (`--minimum-expected-tests 6794`, results-directory `/tmp/dw191/full-final`) exits 0: 6794 passed, zero failed/skipped (`/tmp/dw191-tests-full-final.log`). Final source adds only the diagnostic guard and its two throwing-factory controls. The maintained focused command above, using minimum 47 and results-directory `/tmp/dw191/early-final`, exits 0: 47 passed, zero failed/skipped (`/tmp/dw191-tests-final-focused.log`).

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --verify-no-changes --include tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.EarlyMembershipNudge.cs tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.EarlyMembershipNudgeBoundaries.cs src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPageLog.cs --verbosity quiet
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-191-early-membership-nudge.md
git diff --check
```

All exit0 on final sources (`/tmp/dw191-format-commit.log`). The Git whitespace command prints the existing CRLF-normalization notice for the Razor working copy, with no whitespace defect. The own-spec guard reports no references pointer changes.

Only DW-191's ledger record changes. Its original status/disposition/verification/action fields are retained with `pre-dw-191-` prefixes. A structural comparison retains every other record byte-for-byte and all 656 IDs: 405 done/251 open across 226 maintenance rows, with every open ID covered exactly once (`/tmp/dw191-ledger-validation.json`). The maintenance disposition table is refreshed from the ledger: 405 done,165 actionable,15 blocked,26 awaiting-decision,9 accepted-limitation and36 uncertain. Older closure banners remain historical. Archive, sprint tracking and dependency pointers remain untouched; existing release qualification holds persist.

The final commit scope is eight task-owned paths: page, source-generated diagnostic, two new test partials, spec, this evidence, ledger and maintenance index. The completed review retains every finding and its correction in the spec. No new deferred work is added. Final complete-project acceptance and exact-message ownership/divergence checks follow before commit/push; no deployment or publication is requested.

## Final complete-project acceptance

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6796 --no-ansi --no-progress --results-directory /tmp/dw191/full-commit --report-xunit-trx --report-xunit-trx-filename results.trx
```

Exit 0: 6796 passed, zero failed/skipped/errors, confirmed from TRX counters (`/tmp/dw191-tests-full-commit.log`, `/tmp/dw191/full-commit/results.trx`). Final focused TRX counters confirm 47/47 with zero failed/skipped. This suite includes existing membership coalescing/recursion controls, command tracking, caller isolation, authoritative confirmation and governance consumers. Final scoped formatting exits 0; independent correction recheck reports no unresolved finding.

The spec is done with its frozen intent preserved. Exact candidate commitlint is repeated with zero problems/warnings (`/tmp/dw191-commitlint-final.log`). Final remote fetch succeeds with recursion disabled and main/origin divergence 0/0. Task-owned staging, whitespace, gitlink and ownership checks run before committing; the authorized push targets origin/main. Existing release holds persist.
