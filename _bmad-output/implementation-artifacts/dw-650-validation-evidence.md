# DW-650 validation evidence — 2026-10-18

DW-650 is complete after independent review and correction recheck. The final category decoder passes 119 focused cases and all 1070 Server cases, with zero failures/skips. The final Debug source-reference build succeeds with two transient MSB3026 copy-retry warnings and zero errors. Only DW-650 closes; the pre-existing cursor/stream Unicode issue is newly open as DW-651. Entry revision: `c8f3222af1ba5d10fe2c54e41b6f6be5c84eeeb4`.

## Behavior and scope

`TenantQueryHandlerBase.DeserializeAuditPayload` decodes the category string once and contains only the decoding call's `InvalidOperationException`. A malformed escaped surrogate becomes exactly `invalid-envelope`. The unchanged handler authorizes before parsing and refuses parser errors before cursor decoding or audit reads. Other string-category semantics, date validation, pagination, streams, tenant isolation and unknown-category diagnostics remain unchanged.

The 42 new production-handler cases cover eight malformed Unicode sequences (high/low surrogate range endpoints, embedded surrogates, a separated pair and a reversed pair), each with/without a supplied invalid cursor and with administrator/non-administrator callers (32 cases). Four cases preserve escaped names, case-insensitivity, numeric strings and Unicode whitespace; two preserve ordinary unknown-category diagnostics for a valid surrogate pair and a BMP character; four preserve duplicate-property final-occurrence semantics with malformed/valid values in either order. The existing 59 cases remain included. Rejections assert no payload, zero codec calls and exactly the authority read. Positive controls assert exact row IDs from the mixed-category fixture including foreign-tenant and outside-bound rows.

These are isolated handler tests with substituted read-model storage, not live HTTP or persisted-state integration evidence.

## Original-production control

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-650/red-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GetTenantAuditQueryHandlerTests' --minimum-expected-tests 101 --no-ansi --no-progress --results-directory /tmp/tenants-dw-650/red --report-xunit-trx --report-xunit-trx-filename red.trx > /tmp/tenants-dw-650/red-tests.log 2>&1
```

Build exit 0: three transient MSB3026 copy-retry warnings for EventStore Contracts/Client/DomainService, zero errors. Tests exit 2: 18 failed/83 passed/zero skipped. All 16 administrator malformed-category variants and the two effective-final malformed duplicate cases throw `InvalidOperationException` during category decoding. The 16 unauthorized cases, successful Unicode/duplicate controls and all existing cases pass. Production had not been edited for this control.

## Repaired checks

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-650/green-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GetTenantAuditQueryHandlerTests' --minimum-expected-tests 101 --no-ansi --no-progress --results-directory /tmp/tenants-dw-650/focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-650/focused-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 1052 --no-ansi --no-progress --results-directory /tmp/tenants-dw-650/full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-650/full-tests.log 2>&1
```

All exit 0. Build: one transient MSB3026 retry for EventStore Server DLL, zero errors. Focused: 101/101. Full Server: 1052/1052. Both have zero failures/skips.

```bash
git diff --check
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-650-malformed-unicode-audit-category.md
```

Both exit 0 before review; the story guard reports PASS with no `references/` pointer changes from the full recorded baseline through the working tree. Git prints its existing CRLF-to-LF normalization notice for C#; no whitespace failure is reported.

## Runtime baseline and limits

`aspire ps --format Json` returned an empty list. Exact-target `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive --format Json` exited 0. The initial `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` found security TLS readiness unhealthy with dependent resources waiting. `aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` exited 0 and returned 168 entries. A second identical describe returned every resource Running/Healthy. `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` exited 0 before the first test/source build.

Raw runtime output remains local under `/tmp/tenants-dw-650/` because it includes operational data. The first local summary script attempted to parse Aspire start output as pure JSON and failed with `JSONDecodeError`; its corrected version extracts the JSON document after the CLI status prefix. Both describe commands and all lifecycle commands themselves succeeded. No tracked setting or runtime data was changed to hide this script error.

No live audit request, browser, Release, solution-wide or package-consumer qualification is claimed for this private parser change. Existing release holds remain. No staging, commit, branch, push or dependency update is required or performed.

## Independent review and coverage corrections

The context-free Blind Hunter reviewer returned five findings. Both remaining optional-string decoding findings share a pre-existing cause and are newly tracked as DW-651. The other three findings are patched with eight byte-preserving malformed UTF-8 authorization cases, eight final-null/blank/numeric duplicate controls and two protected-cursor paging controls. Final focused coverage is 119 cases (60 new); the final full Server target is 1070. Production remains unchanged after the category-only repair. The five individual verdicts and evidence are preserved in the spec.

Review reproduction:

```bash
dotnet fsi --exec /tmp/tenants-dw-650/review-unicode.fsx > /tmp/tenants-dw-650/review-unicode.log 2>&1
```

Exit 0. The saved script invokes both the repaired built parser and the production handler through existing fixture helpers. Authorized malformed category plus malformed cursor, and valid category plus malformed cursor/stream, throw `InvalidOperationException`. Non-administrators receive `Forbidden` for each. Malformed category plus malformed stream returns `invalid-envelope`, since category rejection precedes stream decoding. Final null, blank and `99` values overwrite an earlier malformed category successfully. Four raw malformed UTF-8 category byte sequences return `invalid-envelope` through the built parser; the new automated cases additionally pin real-handler authorization and no-read boundaries.

The cursor and stream accessor expressions are unchanged from entry source (`git show HEAD:src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`). Original-accessor controls reproduce the same exceptions; no separately built original handler is claimed. The script's first handler attempt failed to resolve an ASP.NET runtime assembly. Adding the installed ASP.NET 10.0.12 assembly resolver fixed this local fixture setup; the successful saved run is the evidence above. The original probe preceded the new private string/byte-array helper overloads; the repeatable script is updated to select the string overload for rechecks.

Supplied-invalid-cursor acceptance in DW-650 concerns validly decoded opaque cursor strings. Malformed Unicode cursor/stream decoding and mixed malformed payloads remain open under DW-651. No claim is made that every malformed audit payload is now contained.

The first review-correction build used the same Debug source command with output `/tmp/tenants-dw-650/review-build.log`. Exit 1: one CS0029 fixture error, zero warnings. `ShouldNotBeNullOrWhiteSpace` returns void; extracting the non-null cursor value and separately asserting non-whitespace corrected the test without weakening either assertion.

Final commands:

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-650/final-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GetTenantAuditQueryHandlerTests' --minimum-expected-tests 119 --no-ansi --no-progress --results-directory /tmp/tenants-dw-650/final-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-650/final-focused-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 1070 --no-ansi --no-progress --results-directory /tmp/tenants-dw-650/final-full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-650/final-full-tests.log 2>&1
```

The final build exits 0 with two transient MSB3026 copy-retry warnings (EventStore Contracts XML and Server DLL) and zero errors. Final test results and correction recheck are recorded below after execution.

Final tests both exit 0: focused 119/119 and full Server 1070/1070, zero failures/skips. Their TRX counters are checked. The independent correction recheck found no new issue and accepted all three coverage patches plus the DW-651 deferral. Its corrected reflection script re-execution exits 0 into `/tmp/tenants-dw-650/review-unicode-recheck.log`, preserving the original log and returning the same results. Earlier 101/1052 results remain above as intermediate evidence.

## Backlog reconciliation

Only DW-650 closes, with all prior changing fields preserved as `pre-dw-650-*` and the original finding retained. DW-651 records the separate pre-existing cursor/stream decoding defect from review. All 650 original IDs remain plus DW-651: 651 IDs, 360 done/291 open across 263 maintenance rows. The summary disposition table is synchronized to these current counts; unrelated ledger record bodies and index rows remain unchanged. Archive/sprint, dependency pointers, HEAD and index remain preserved. Existing release qualification holds remain.

`python3 /tmp/tenants-dw-650/verify-completion.py` exits 0, PASS: all 650 original IDs remain; only original DW-650's body changes, with its prior fields preserved; DW-651 is the sole new record. All 649 unrelated record bodies and unrelated maintenance rows are unchanged. The 263 current rows cover exactly the 291 open IDs without duplicates, and status counts are 360 done/291 open. Archive/sprint hashes, canonical entry HEAD and index remain unchanged. The checker verifies original/intermediate/final TRX counts and CRLF C#/LF Markdown. Final `git diff --check` and the story-gitlink guard both exit 0; the latter reports PASS with no pointer changes. Six task files remain unstaged/untracked.
