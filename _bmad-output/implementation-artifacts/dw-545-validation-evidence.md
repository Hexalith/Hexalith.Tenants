# DW-545 validation evidence

**Later acceptance completed (2026-10-17):** The [authorized UI acceptance follow-up](dw-545-ui-acceptance-evidence.md) fixes the reproduced removal ordering races and a parent fixture completion gap. Independent review/recheck, zero-warning/error Debug source build and full UI6243/6243 pass with zero failures/skips, including all72 conflict cases. DW-545 is now closed; DW-648 records separate ambiguous-retry qualification. The original pending-stage body below is preserved as historical evidence; its failures are not rewritten as passes. Release remains not ready.

Entry: clean `main` at `ceac6269da3e99d4d5e33c9fa3dd163959395f0f`, 2026-10-17. The user selected DW-545 from the deferred-work ledger. **Acceptance remains pending:** the full UI completion gate is red; DW-545 stays open/actionable and its spec stays `in-review`.

## Source finding

The historical finding is preserved in the ledger. Current `GlobalAdministratorRow` is a sealed record whose only data fields are `UserId`, `Freshness` and `Lifecycle`; `GlobalAdministratorSummary` carries only `UserId`. `GlobalAdministratorsProjectionLoader.IsCurrentStablePage` validates every row before merging and requires both Current freshness and Current lifecycle. With equal ordinal IDs, two valid rows are therefore equal: every representable conflict fails before `TryAdd`. No current production defect or before/after behavior change is claimed.

The existing `LoadAsyncDeduplicatesIdentitiesWithinAndAcrossPagesWithOrdinalComparison` deliberately supports identical repeated rows while distinguishing case-sensitive IDs. It remains unchanged. The new regression matrix tests all nine defined non-current freshness/lifecycle alternatives, both row orders, same/cross-page locations, and final/nonfinal conflict pages: **72 new cases**. Conflicting-first cross-page cases stop at the first page; current-first cases consume the second page then stop. An unrelated current row verifies that the result clears the entire retained population. Assertions cover canonical incomplete state, rows, freshness, lifecycle, scope, cursor, validator, projection version, page-size reset, bounded calls, actual cancellation token and opaque continuation forwarding.

No production code, public API, persistence, dependency, markup or localization change is needed. Existing action/preview consumers independently require complete population evidence; their tests are included in the full UI project.

## Build and regression results

Commands ran from the repository root with the existing SDK and dependency pins. Builds use Debug source references; tests use the individual UI project's maintained Microsoft.Testing.Platform runner.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --nologo -v:q > /tmp/tenants-dw-545/build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GlobalAdministratorsProjectionLoaderTests' --minimum-expected-tests 43 --no-ansi --no-progress --results-directory /tmp/tenants-dw-545/loader --report-xunit-trx --report-xunit-trx-filename loader.trx > /tmp/tenants-dw-545/loader-tests.log 2>&1
```

Initial build: exit 0, zero warnings/errors, 13.85s. Initial loader run: 43 passed, zero failures/skips, exit 0, including the first 16 conflict cases against unchanged production code.

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6175 --no-ansi --no-progress --results-directory /tmp/tenants-dw-545/full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-545/full-tests.log 2>&1
```

Initial full UI: **6,172 passed, three failed, zero skipped; exit 2**, 1m52.217s. Failures are the EN/FR confirmation-change, non-retry variants of `RemoveTenantMemberFlowTests.PendingRemovalAuthorityReadReportsChangedSafety` (expected exact confirmation guidance, observed generic changed-safety guidance), plus `GlobalAdministratorsPageTests.OrdinaryRefreshAndProjectionNotificationShareCallerCancellationWithoutCancelingSiblingAuthority` (pending-timer count one versus expected zero). The runtime and those existing test methods are unchanged from entry. This result is retained, not counted as passing acceptance.

The blind reviewer found three coverage gaps: other non-current enum values, unrelated retained prefix evidence, and canonical request-page-size reset. All three were patched; read-only recheck accepted each correction and found no remaining concrete gap.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false --no-restore --nologo -v:q > /tmp/tenants-dw-545/review-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GlobalAdministratorsProjectionLoaderTests' --minimum-expected-tests 99 --no-ansi --no-progress --results-directory /tmp/tenants-dw-545/loader-final --report-xunit-trx --report-xunit-trx-filename loader.trx > /tmp/tenants-dw-545/loader-final-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*RemoveTenantMemberFlowTests.PendingRemovalAuthorityReadReportsChangedSafety' --filter-method '*GlobalAdministratorsPageTests.OrdinaryRefreshAndProjectionNotificationShareCallerCancellationWithoutCancelingSiblingAuthority' --minimum-expected-tests 9 --no-ansi --no-progress --results-directory /tmp/tenants-dw-545/timing-recheck --report-xunit-trx --report-xunit-trx-filename recheck.trx > /tmp/tenants-dw-545/timing-recheck-tests.log 2>&1
```

Final build: exit 0, zero warnings/errors, 30.97s. Expanded loader matrix: **99 passed, zero failures/skips, exit 0**. Focused recheck: **eight passed, one failed, zero skipped; exit 2**. The page test and the original two failed removal cases pass, but another removal variant (EN, confirmation-change, retry) reports the same exact-versus-generic guidance mismatch. No test or gate was disabled and no unrelated runtime/test repair was made. The observations do not establish the scheduling cause.

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6231 --no-ansi --no-progress --results-directory /tmp/tenants-dw-545/full-final --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-545/full-final-tests.log 2>&1
```

Final full UI: **6,229 passed, two failed, zero skipped; exit 2**, 2m32.540s. TRX inspection confirms **all 72 new conflict cases pass**. Both failures are unchanged `RemoveTenantMemberFlowTests.PendingRemovalAuthorityReadReportsChangedSafety` cases:

- FR, freshness-change, retry: expected the localized freshness refusal; observed the prior `Delivery outcome needs recovery.` fixture message.
- EN, confirmation-change, non-retry: expected the exact typed-confirmation refusal; observed the generic changed-safety explanation.

The initial failing page case passes in the targeted and final full runs. The three initial failures and failed focused recheck above remain part of the evidence. Their differing outcomes do not prove a root cause, and no repeated-until-green result is used for acceptance. The required Hexalith baseline states: **"All existing and new tests must pass before a story is complete."** DW-545 therefore remains open/actionable and the spec remains `in-review`, despite accepted conflict coverage. Resolving the unrelated removal-feedback failures is separate follow-up work.

## Runtime baseline and limits

Before test edits, `aspire ps --format Json` listed another workspace's FrontComposer AppHost. The isolated task-owned baseline started with `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --non-interactive --format Json`; `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` reported all 23 resources Running/Healthy (including parameter resources). `aspire otel logs tenants-gkkpmwkf --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --limit 5` succeeded. Raw local runtime results are in `/tmp/tenants-dw-545/aspire-*`; no secrets are copied into this artifact.

The owned AppHost stopped successfully with `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive`. The other workspace's FrontComposer AppHost was left alone. This test-only work does not claim a live conflicting-duplicate reproduction, browser execution, Release packaging, integration qualification or release readiness.

## Preservation

`python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-545-conflicting-administrator-evidence.md` passes at the entry revision with no `references/` pointer change. `python3 /tmp/tenants-dw-545/verify-preservation.py` passes. It compares the full entry revision against current files and confirms: only DW-545 changes among all 647 ledger bodies; every original selected field is retained either unchanged or under `pre-dw-545-*`; all other 646 records, archive/sprint and production sources are preserved. Counts remain **353 done / 294 open**, with exact one-time coverage of every open ID by **265 maintenance rows**. The test file uses CRLF, HEAD is unchanged, and nothing is staged. The local result is retained in `/tmp/tenants-dw-545/preservation.json`.

`git diff --check` passes. Git additionally reports its existing CRLF-to-LF normalization notice for the test source; the working file deliberately follows the repository CRLF rule. Exactly five task files are modified/new. Final bookkeeping review accepted the counts, identities, preservation, status and maintenance row, and requested precise cause-neutral wording. Current evidence uses “failures in unchanged existing tests” and “focused recheck”; earlier spec notes are explicitly clarified without rewriting their historical text. No review finding was deferred; the separate existing test failures and all release holds remain recorded. Task changes remain uncommitted under the explicit repository Git boundary; no staging, branch, commit, push or dependency operation ran.
