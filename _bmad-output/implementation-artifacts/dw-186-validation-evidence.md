# DW-186 validation evidence — 2026-10-21

Entry revision: `2ac783647336594787b1dc5cdde94dc4cd1b302c`, clean main. The user requests the next deferred work and explicitly authorizes commit and push on main. DW-186 is the next dependency-free actionable Tenants item after DW-181. DW-180 remains uncertain; earlier open records retain their separate contracts, external ownership and dependencies.

## Scope

The appended compatibility category distinguishes non-current projection lifecycle from unusable read freshness. It is a lifecycle presentation subtype of the shared canonical `StaleData` reason: the six shared reasons, evaluator, command eligibility, authority/read/freshness precedence and refresh recovery stay unchanged. The rendered lifecycle reason uses the existing specific EN/FR whole-string resource, formatting its tenant and operation. New EN/FR category resources preserve lookup parity; existing enum values stay in place. The truth-state specification documents this supplemental presentation contract.

This is component and state regression evidence, not distributed functional, browser, deployment or release qualification. No backend, persisted data, dependency or AppHost change is required. Existing release holds remain.

## Baseline and reproduction

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

Baseline and original-production/new-test builds exit 0 with zero warnings/errors (`/tmp/dw186-build-baseline.log`, `/tmp/dw186-build-red.log`).

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/dw186/red --filter-class '*TenantLifecycleProjectionCategoryTests' --minimum-expected-tests 65
```

Entry production exits 2: 56 failed, nine passed, zero skips. The 56 non-current lifecycle combinations reject the old generic category; all nine earlier permission/read/freshness/undefined-input controls pass. This is a deliberate failing reproduction before production edits.

## Aspire baseline

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json --non-interactive
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --severity Error --limit 5 --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
```

Initial ps is empty; start succeeds. Initial waiting resources settle to all 23 Running/Healthy before code edits. The bounded error sample contains Memories registry initialization failures. Resource health therefore does not establish successful initialization or functional acceptance. The app started by this task stops successfully. Raw local output is kept under `/tmp/dw186-aspire-*`; credentials and telemetry internals are excluded from tracked evidence.

## Initial fixed-code verification

The first production build exits 1, zero warnings/one CS0103: `ProjectionLifecycleState` needs its namespace import. The corrected build (`dw186-build-green-corrected.log`) exits 0 with zero warnings/errors. An intervening no-build focused execution (`dw186-test-focused.log`) still runs entry binaries and exits 2; it supplies no fixed-code evidence.

Run the focused command above with `/tmp/dw186/focused-corrected`: exit 0, 65 passed, zero failed/skipped. The theory covers all six defined non-current lifecycle states plus an undefined state, EN/FR localization, legacy/typed inputs, typed current/aging/refreshing-with-baseline evidence, blocked launcher events and current-state recovery. Nine controls retain earlier authority, failed-read, stale/unknown freshness, refreshing-without-baseline and undefined-input reasons. The existing adapter theory also includes an undefined lifecycle case.

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/dw186/full --minimum-expected-tests 6690
```

Initial full UI exits 0: 6690 passed, zero failed/skipped. This includes conformance, localization parity, retained-attempt, command availability and configuration consumers of the unchanged shared evaluator. Review subsequently strengthens the negative-launch fixture; final-source evidence is recorded below when that correction is qualified.

## Formatting and provenance

The first scoped formatting check exits 2 with ENDOFLINE diagnostics on the new test file. Run the identical scoped formatter without `--verify-no-changes` to apply repository CRLF policy; the subsequent check exits 0 (`dw186-format-final.log`).

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --verify-no-changes --include src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleUnavailableReasonCategory.cs tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleProjectionCategoryTests.cs tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleAvailabilityTests.cs --verbosity quiet
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-186-projection-lifecycle-category.md
```

The own-spec gitlink guard exits 0: no references pointers change. After clean entry, concurrent external archiving changes `deferred-work.md` and `deferred-work-archive.md`. Preserve those edits and stage only task-owned ledger hunks; no archiving belongs to DW-186. The whole-working-tree whitespace check reports the externally added ledger trailing blank line; task-owned staged checks are recorded separately.

The owning repository's installed commitlint CLI matches lockfile version 21.2.3. Exact candidate validation exits 0 with zero problems/warnings (`/tmp/dw186-commitlint.log`):

```bash
node_modules/.bin/commitlint --edit /tmp/dw186-commit.txt --verbose
```

The validated full input is `fix(ui): distinguish projection lifecycle availability reasons` with a trailing newline. Fetch inspection initially shows main/origin/main aligned at entry; final divergence and staged ownership are checked again before push.

## Independent review and corrections

The Blind Hunter reviewer returns five concrete findings; all are accepted and patched. The spec's Review Triage Log preserves each verdict/evidence. Matching real detail and an actual allowed launch remove the negative-launch fixture's separate render guard. Compatibility message selection now follows the category selection, so undefined freshness retains stale-freshness guidance. Already-open confirmations use the same projection-lifecycle explanation for current and captured evidence. Legacy aging rows now reach the real adapter with aging freshness, and earlier-blocker controls assert exact EN/FR reason/recovery copy. Shared canonical reasons, evaluator and recovery remain unchanged.

The revised class has 96 cases: 70 non-current lifecycle cases (including legacy aging), 18 EN/FR earlier-blocker cases and eight open-confirmation transitions spanning both lifecycle directions and typed/legacy paths. The latter qualify blocked submission, unchanged shared refresh recovery and subsequent current-state recovery. One existing open-flow assertion changes from generic stale copy to the specific projection-lifecycle explanation.

The corrected-source Debug build exits 0 with zero warnings/errors (`dw186-build-reviewed.log`). The maintained lifecycle lane exits 0: 358 passed, zero failed/skipped; explicit TRX inspection identifies all 96 dedicated cases as passed:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/dw186/focused-reviewed --filter-class '*TenantLifecycle*' --minimum-expected-tests 96
```

Independent read-only correction recheck accepts all five fixes with no unresolved concrete issue. Extending the scoped formatter to the existing changed fixture initially reports CRLF and two pre-existing conditional-indent diagnostics at its lines 914/921. The targeted formatter normalizes that file and those two whitespace-only lines; no behavior changes. The complete scoped formatting check then exits 0 (`dw186-format-accepted.log`). The full reviewed-source UI suite is repeated below before closure.

## Final qualification and ownership

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx --results-directory /tmp/dw186/full-reviewed --minimum-expected-tests 6721
```

Exit 0: 6721 passed, zero failed/skipped, confirmed from TRX counters (`/tmp/dw186-test-full-reviewed.log`). This executes the reviewed behavior, including all five corrections. The subsequent existing-fixture formatter changes only line endings and two conditional indentations; the final Debug source build verifies the normalized sources without weakening any check.

Reconciliation retains all 656 IDs: 403 done/253 open. Exactly 228 maintenance rows cover every open ID once. Only DW-186's record body changes relative to the captured external ledger; its original changed fields are preserved under `pre-dw-186-` names. The external archive is byte-identical to the captured concurrent edit. Archive and sprint maintenance are outside this task.

The closure uses a task-only unified patch prepared from repository HEAD for `deferred-work.md`. Applying that patch to the index stages only the new DW-186 closure heading and record; the unrelated external archiving hunks and archive file remain in the working tree. The other thirteen task files are staged explicitly. No dependency pointers are staged or changed. The frozen intent is preserved verbatim; the spec is done and independent review has no unresolved finding. Existing release qualification holds remain.

Final normalized-source Debug build exits 0 with zero warnings/errors (`/tmp/dw186-build-final.log`). The own-spec gitlink guard exits 0 with no pointer changes. `git diff --cached --check` exits 0. Index inspection confirms exactly fourteen task-owned paths, no archive/dependency path and only DW-186's record changed relative to HEAD. The concurrent archive remains untouched and uncommitted. No new work is deferred.
