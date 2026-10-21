# DW-270 / DW-306 / DW-324 / DW-536 / DW-538 validation

Date: 2026-10-21. Baseline: `bf5d790749215cc8cd6f1271b0dd309a268fd0d7` on clean `main`.

## Change and scope

The overlapping French editorial records cover the global-administrator and audit resource cluster. The pass corrects accents and uses the existing curly-apostrophe convention throughout that cluster, including the historical column `Availability` now named `Column.Actions`. Audit receipt command/reference labels were already accented; the remaining column, copy-action and unavailable-reference labels are corrected. A separate editorial pass checked all 480 cluster values and context-sensitive noun/verb forms. Independent review also corrects the adjacent `Tenants.Detail.AuditAccessibleLabel` accent, semicolon spacing and eight explanatory values' wording; this is a small extension of the same copy pass. No English resources, resource keys, placeholders, authorization, command handling or navigation implementation change.

DW-536 now has exact English/French unauthorized-heading assertions rendered with the shipped resources through `CultureBoundTenantsLocalizer`; the existing denial-state assertions remain. DW-538's focus and focusless return cases now assert complete English/French hints while retaining exact safe return URL and stripped-cursor checks. These tests qualify rendered copy, not browser focus or screen-reader behavior.

## Commands and results

Run from the repository root:

```bash
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
```

Startup succeeds. Initial inspection shows security Running and dependent services Waiting; no distributed-health acceptance is claimed. The raw state remains outside the repository because it contains runtime configuration.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

Initial build succeeds with zero warnings/errors. Each focused run below uses the maintained Microsoft.Testing.Platform lane:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*UnauthorizedDetailTitleUsesExactLocalizedCopy' --minimum-expected-tests 2 --no-ansi --no-progress --results-directory /tmp/tenants-five/title --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*Matching_list_anchor_and_explicit_return_focus_restore_the_audit_launcher' --minimum-expected-tests 2 --no-ansi --no-progress --results-directory /tmp/tenants-five/focus-return --report-xunit-trx --report-xunit-trx-filename results.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-method '*Focusless_detail_return_preserves_a_stripped_cursor_notice' --minimum-expected-tests 2 --no-ansi --no-progress --results-directory /tmp/tenants-five/focusless --report-xunit-trx --report-xunit-trx-filename results.trx
```

All three pass 2/2: six passed, zero failures/skips. An earlier invocation joining these method patterns with `|` matched zero tests and correctly failed the six-test minimum (exit 5); it is not acceptance evidence.

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6882 --no-ansi --no-progress --results-directory /tmp/tenants-five/full --report-xunit-trx --report-xunit-trx-filename results.trx
```

Initial full UI: 6,882 total, 6,839 passed, 43 failed, zero skipped; exit 2. One composition case expected the old unaccented administrator titles; 42 tracked-grant ambiguity cases expected a straight apostrophe. Updated only those three French literals in the existing fixtures; assertion strength and gateway behavior remain unchanged.

Resource inspection with Python's XML parser confirms all 1,720 English/French keys match the original sets; every placeholder list is unchanged. The final pass changes 209 French values, all in `Tenants.GlobalAdministrators.*` / `Tenants.Audit.*` except the explicitly reviewed detail audit-launcher label. Normalizing accents, apostrophe glyphs and semicolon spacing reproduces the original text for every changed value except the eight reviewed wording corrections (action column, mobile description, three authorization descriptions, removal-preview scope and two audit no-payload descriptions).

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-270-306-324-536-538-localized-copy.md
git diff --check
```

Both pass before closeout; the story has no `references/` pointer changes. Historical unrelated gitlink provenance and release holds remain outside this change.

## Final closeout

Final rebuild, full UI result, independent review triage and ledger-preservation results:

- The first corrected full rerun (`/tmp/tenants-five/full-final`) passes 6,882/6,882 with zero failures/skips. It precedes the final review's small editorial corrections, so a further complete run qualifies those final resource bytes.
- The rebuild after all six independent findings (`/tmp/tenants-five/build-reviewed.log`) succeeds with zero warnings/errors. Full reviewed results use the same full command above with `--results-directory /tmp/tenants-five/full-reviewed`.
- Exact intended commit message was validated with the lockfile-pinned `@commitlint/cli` 21.2.3: `./node_modules/.bin/commitlint --edit /tmp/tenants-five/commit-message.txt --verbose`; zero problems/warnings. Full message: `fix(ui): finish French audit and administrator copy`.
- Final reviewed full UI passes 6,882/6,882, zero failures/skips/errors (1m 47s 942ms). This run includes all six focused EN/FR cases and the existing localization/parity/conformance/gateway tests.
- Blind Hunter computed a six-finding floor from 31.227 kB. All six editorial observations receive simple corrections; no behavior/schema regression is identified and nothing is newly deferred. Its independent recheck confirms every finding resolved, valid XML, unchanged keys/placeholders and preserved authorization/scope/audit meanings. Per-finding triage is retained in the spec.
- `python3 /tmp/tenants-five/close-ledger.py` checks the final TRX counters before closing records, compares each parsed record with the pre-edit ledger and asserts exact unique open-ID index coverage. Only the five selected records change. All historical field values remain under `pre-localized-copy-*`; all 657 IDs remain, 415 done/242 open in 221 maintenance rows. The script's results are retained in [ledger validation](dw-270-306-324-536-538-ledger-validation.json).
- The final story-gitlink and whitespace checks pass. No `references/` pointer, archive or sprint changes belong to this batch. This is local Debug source and UI evidence; no Release package, live focus, screen-reader or production-release qualification is claimed.

The contributor-requested formatter check is distinct from `git diff --check`:

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --include tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs --verify-no-changes --verbosity quiet
```

It exits 2 with 122 existing `WHITESPACE` diagnostics: 22 audit-page, 69 detail-surface and 31 command-gateway occurrences. A line mapping against the full baseline proves every diagnostic is on an exact unchanged line, with zero on added/changed lines. For example, command-gateway line 2606 requests replacing a space with a CRLF/indentation sequence. No unrelated formatting is rewritten. This existing formatting limitation does not negate the clean build, complete UI test result, record preservation or `git diff --check`.
