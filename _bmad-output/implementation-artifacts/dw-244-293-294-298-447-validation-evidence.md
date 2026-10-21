# Command explanation bundle validation — 2026-10-21

Entry revision: `73e4cbee3c3d429ee90a81562ceb9060f8a82ce5`, clean `main`, aligned with `origin/main`. The user authorizes five deferred items and pushing the completed changes on main. Selected one cohesive Tenants-owned copy goal: DW-244, DW-293, DW-294, DW-298 and DW-447. The rendered bmad-build one-shot workflow governs implementation and independent review.

## Behavior and scope

Member row gates and reason values are unchanged. The display selector now distinguishes a known owned member reservation, a supplied external explanation, and unavailable support without a known cause. Owned-command text applies accurately to both its own and sibling rows, including a parent echo of the same aggregate lock. The static terse reason catalog is unchanged. Permission, freshness and lifecycle prerequisites still take precedence.

Whole-string EN/FR resources describe preview, processing and evidence states across metadata, set/remove configuration and member removal without promising optional destinations or exhausted retries. AlreadyApplied copy describes previously observed projection evidence; UnableToVerify copy also remains accurate for blocked previews before dispatch. The four Previewed resources are neutral, although the metadata edit path retains Idle and does not need an invented Previewed fixture. All 38 existing recovery values per language in scope and the two additional member explanation keys per language preserve resource-key parity. Existing localizer doubles are aligned with these shipped values.

The historical WP-2A spec now labels its old copy decision as historical and points to the authoritative ledger. Its original frozen intent, baseline and pointer declarations remain intact. No command dispatch, proof, lease, retry, API, dependency, persisted state, stylesheet or topology behavior changes.

## Baseline and controlled reproduction

The required Aspire baseline uses the repository-local Aspire skill:

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json --non-interactive
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --severity Error --limit 5 --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive
```

All commands exit 0. Initial ps is empty; describe reports 23 Running resources. The bounded telemetry sample contains five error records, so running resources do not establish functional initialization or deployment readiness. Only the AppHost started for this task is stopped. Raw local output with runtime details is excluded from tracked evidence.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

The first build exits 1 with one CS0103 in the recovery fixture: a missing configuration namespace import. The corrected build exits 0 with zero warnings/errors (`/tmp/dw244-build-green.log`). The initial selector `--filter-class '*Command*ExplanationTests' --minimum-expected-tests 52` selects zero tests and exits 5; this is a failed selection, not acceptance evidence. The corrected selector below runs all 52 initial cases and passes.

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*ExplanationTests' --minimum-expected-tests 52 --no-ansi --no-progress --results-directory /tmp/dw244/focused --report-xunit-trx --report-xunit-trx-filename results.trx
```

Exit 0: 52 passed, zero failed/skipped. To check sensitivity, save exact feature bytes for MemberAccessReview.razor and both resource files, temporarily substitute their entry-revision bytes from Git, build with the same command, and run the same 52-case lane with results-directory `/tmp/dw244/entry`. Entry build exits 0 with zero warnings/errors; tests exit 2: **42 failed, ten passed, zero skipped**. The ten unchanged-cause and earlier-prerequisite controls pass. A finally block restores exact feature bytes and a fresh source build succeeds with zero warnings/errors. Exact argv/results are in `/tmp/dw244-entry-command-results.json`; build/test logs use `dw244-build-entry`, `dw244-focused-entry` and `dw244-build-restored` prefixes.

## Review, initial failures and correction evidence

The first full run uses the final full command below with minimum 6848 and results-directory `/tmp/dw244/full-first`: exit 2, **6846 passed, two failed, zero skipped**. The failures are the existing localizer-double parity gate and an existing real held add-member submission assertion expecting the old phrase. Aligned affected test-double resource values with shipped resources and changed that parent-driven assertion to the accurate owned-member explanation. The gate and existing admission/sibling-launcher assertions are preserved.

Blind Hunter returns ten findings; their complete verdicts and evidence are in the spec Review Triage Log. Corrections cover remaining preview action promises, pre-dispatch UnableToVerify wording, stale terminal AlreadyApplied evidence, full operation-specific localized assertions, incomplete tracking, normal parent composition, pending lease refusal/fault cleanup, and XML documentation. The request for exhaustive new rendered fixtures around every unchanged state-to-resource binding is rejected as a low-value expansion; the existing full state/flow suites and focused consequential scenarios remain the acceptance boundary. Independent read-only correction recheck accepts the bounded implementation with no further code correction.

The first expanded 84-case run exits 2: **78 passed, six failed**. These expose fixture assumptions: metadata editing stays Idle (two cases), French contracts `de le` to `du` (two cases), and set-configuration retains message-only reconciliation even without correlation (two cases). Tests are corrected to actual producer/control behavior, with no production state or control changes; the two unreachable metadata Previewed cases are removed. The final focused count is 82.

Scoped whitespace verification initially exits 2 for nested foreach indentation in the new recovery fixture; the scoped formatter corrects it. Final verification is recorded below.

## Separate inherited follow-up: DW-657

AddTenantMemberFlow.razor is unchanged. Its support refusal uses `CommandSurfaceUnavailableReason ?? fallback`, but the disabled predicate ignores empty and whitespace reasons. A temporary rendered probe enters a valid user/role, verifies submit starts enabled, then requires it to disable after parent support becomes unavailable. Build exits 0; the six-case method lane exits 2: **four empty/whitespace failures and two null controls passed**, zero skips. This proves an incorrectly enabled valid form, not a successful backend mutation.

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*UnknownSupportCauseDoesNotClaimAnotherCommand' --minimum-expected-tests 6 --no-ansi --no-progress --results-directory /tmp/dw244/add-probe --report-xunit-trx --report-xunit-trx-filename results.trx
```

The probe-only assertions are removed, exact feature test bytes are restored, and the reviewed source rebuilds cleanly. Exact commands/results are in `/tmp/dw244-add-probe-results.json`; logs are `/tmp/dw244-build-add-probe.log` and `/tmp/dw244-add-probe.log`. DW-657 records this pre-existing admission work separately from the five explanation closures.

## Final source qualification

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*ExplanationTests' --minimum-expected-tests 82 --no-ansi --no-progress --results-directory /tmp/dw244/focused-final --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6878 --no-ansi --no-progress --results-directory /tmp/dw244/full-final --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --verify-no-changes --include tests/Hexalith.Tenants.UI.Tests/Components/MemberCommandExplanationTests.cs tests/Hexalith.Tenants.UI.Tests/Components/CommandRecoveryExplanationTests.cs --verbosity quiet
```

The final Debug source build exits 0 with zero warnings/errors (`/tmp/dw244-build-final.log`). The focused lane exits 0: **82/82**, zero failures/skips. Scoped formatting exits 0 (`/tmp/dw244-format-final.log`). The full UI lane exits 0: **6878/6878**, zero failures/skips, confirmed from TRX counters (`/tmp/dw244/full-final/results.trx`).

## Inherited historical gitlink guard limitation

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-244-293-294-298-447-command-explanations.md
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md --ref b2b80941df874c2ee6772ca316841c480e0e493b
```

The new bundle spec exits 0 with no pointer changes. The historical spec exits 1 at entry HEAD for eight undeclared pointers among nine changes since its baseline. At the latest story amendment `b2b80941df874c2ee6772ca316841c480e0e493b`, it still exits 1 for six undeclared pointers among seven changes. An independent read-only audit confirms that earlier endpoints omit later story-associated pointer commits, and no later acceptable endpoint restores all undeclared paths to the frozen baseline. Following docs/story-gitlink-ci.md, the correction adds the explicit historical audit endpoint at that latest eligible amendment. The original baseline, frozen intent, path declarations and stated targets remain unchanged; the historical attribution failure is inherited. Preserve it explicitly rather than inventing ownership, moving the baseline, reverting dependencies or weakening the guard. This task does not claim Story Guards passes.

This is source/component acceptance evidence. It does not establish authenticated browser, distributed behavior, release, deployment or broader historical provenance qualification. Existing release holds remain.

## Closure, preservation and Git checks

The closure verification script (`python3 /tmp/dw244-close-ledger.py`, exit 0) checks all 656 original IDs, byte-identical bodies for all 651 unrelated original records, preserved original changed fields under `pre-command-explanations-` names, exactly five closures and the separate DW-657 addition. Archive and sprint files remain byte-identical to entry Git. The maintained index covers all 247 open IDs exactly once across 224 rows; three closed rows are removed and one DW-657 row is added. Current totals are **657 records: 410 done, 247 open** (161 actionable, 15 blocked, 26 awaiting-decision, nine accepted limitations, 36 uncertain). Machine-readable results and TRX counters are in [ledger validation](dw-244-293-294-298-447-ledger-validation.json).

The independent recheck accepts prior findings 1–3 and 6–10 as corrected, finding 5 as a bounded coverage limit and finding 4 as the inherited DW-657 deferral. No further code correction is required. The original frozen intent and baseline/pointer declarations in the historical spec remain unchanged; the explicit historical audit reference follows the repository documentation and retains its six-declaration failure.

The owning pinned commitlint CLI is version 21.2.3, matching the package lockfile. `node_modules/.bin/commitlint --edit /tmp/dw244-commit.txt --verbose` exits 0 with zero problems/warnings. The exact validated full candidate is `fix(ui): clarify command availability and recovery explanations` followed by one newline; successful output is retained in `/tmp/dw244-commitlint.log`. Fetch inspection shows main/origin/main divergence `0 0`. Working-tree whitespace checks pass; task-owned staging and push checks follow the same validated candidate without changing dependency pointers.
