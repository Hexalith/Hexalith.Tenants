# DW-653 validation evidence — 2026-10-18

Entry revision: `57d564b4b38a05ba3444ed7093d8330a1a597bfa`, clean `main` at activation. Scope is DW-653 only: contain Unicode decoding failures in standard pagination cursors while preserving decoded cursors, existing parsing defaults and page-size bounds.

## Initial reproduction

Added 64 production-handler cases before changing production. They cover every standard pagination consumer (`list-tenants`, `get-tenant-users`, `get-user-tenants`, `get-global-administrators`), eight escaped unpaired/embedded/reversed surrogate forms, four raw invalid UTF-8 sequences, identity/cancellation and global-route precedence, cross-user pre-parsing reads, final duplicate cursors, and protected continuation/page-size/scope controls.

```bash
mkdir -p /tmp/tenants-dw-653
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-653/red-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*StandardTenantQueryPaginationTests' --minimum-expected-tests 64 --no-ansi --no-progress --results-directory /tmp/tenants-dw-653/red-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-653/red-focused-tests.log 2>&1
```

Build exits 0 with two transient MSB3026 copy-retry warnings and zero errors. Test exits 2: 64 executed, 54 failed, 10 passed, zero skipped. All 54 failures throw `InvalidOperationException` from the original cursor accessor: 32 escaped-surrogate, 16 raw UTF-8, two cross-user and four final-malformed-duplicate cases. The identity/route and valid-pagination controls pass.

Durable actual failure excerpts (repository path prefixes abbreviated):

```text
MalformedUnicodeCursorReturnsInvalidEnvelopeAsync(queryType: "list-tenants", cursor: "\\udc00\\ud800")
System.InvalidOperationException: Cannot read invalid UTF-16 JSON text as string. Invalid surrogate value: '0xDC00'.
at System.Text.Json.JsonDocument.GetString(...)
at TenantQueryPaginationPayloadParser.DeserializeStandardPayload(...) (TenantQueryPaginationPayloadParser.cs:27)
at TenantQueryHandlerBase.DeserializePaginationPayload(...) (TenantQueryHandlerBase.cs:187)
at ListTenantsQueryHandler.ExecuteCoreAsync(...) (ListTenantsQueryHandler.cs:31)
at TenantQueryHandlerBase.ExecuteAsync(...) (TenantQueryHandlerBase.cs:129)

MalformedUtf8CursorReturnsInvalidEnvelopeAsync(queryType: "get-tenant-users", hex: "80")
System.InvalidOperationException: Cannot transcode invalid UTF-8 JSON text to UTF-16 string.
---- System.Text.DecoderFallbackException: Unable to translate bytes [80] at index 6 from specified code page to Unicode.
```

To reproduce after this patch, use a disposable workspace copy with the pinned SDK and the same root-declared dependency checkouts. In that copy, use the production query files from the canonical entry revision above and copy in the current `StandardTenantQueryPaginationTests.cs`; leave the original parser-test file at that revision because the new parser tests depend on the new result property. Restore that disposable project if necessary, then run the two commands above. Do not replace repaired files in the working workspace. This is reproduction guidance, not a claim of an additional disposable-copy execution; the counters above describe the actual initial run.

## Repaired verification

Cursor `GetString()` alone catches `InvalidOperationException` and returns the optional pagination validation error. All four standard handlers consume that error at their existing parsing points, returning bare `InvalidEnvelope`, null payload and no cursor-codec calls. The result record moves to its own named file to follow the single-type rule.

Existing ordering is preserved: missing authenticated identity wins before any read; invalid global tenant/aggregate wins before parsing; get-user-tenants reads its index and evaluates cross-user administrator visibility before parsing; list-tenants and get-tenant-users still parse before model reads. Persisted authorization and cursor-codec behavior are unchanged.

Added 14 direct parser cases for malformed cursors, valid Unicode pairs/control characters/empty strings, final duplicates, wrong-kind final cursor and page-size defaults/overflow/clamping. Existing parser cases now also assert that no validation error is introduced. The 64 handler cases plus 14 parser additions total 78 new test cases.

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-653/green-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*StandardTenantQueryPaginationTests' --filter-class '*TenantQueryPaginationPayloadParserTests' --minimum-expected-tests 92 --no-ansi --no-progress --results-directory /tmp/tenants-dw-653/green-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-653/green-focused-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 1296 --no-ansi --no-progress --results-directory /tmp/tenants-dw-653/green-full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-653/green-full-tests.log 2>&1
```

All three commands exit 0. Build has zero warnings/errors. Actual TRX counters, preserved independently of temporary logs:

| Run | Total/executed | Passed | Failed | Not executed |
| --- | --- | --- | --- | --- |
| Original production, handler cases | 64 | 10 | 54 | 0 |
| Repaired focused parser/handlers | 92 | 92 | 0 | 0 |
| Repaired full Server | 1296 | 1296 | 0 | 0 |

`git diff --check` exits 0. `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-653-malformed-unicode-standard-cursors.md` exits 0 with PASS and no references/ pointer changes. Modified C# files retain CRLF; Git's informational normalization warnings do not indicate whitespace defects.

## Runtime baseline

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/tenants-dw-653/aspire-baseline.json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/tenants-dw-653/aspire-baseline-final.json
aspire logs security --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --tail 5 --format Json > /tmp/tenants-dw-653/aspire-security.json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj > /tmp/tenants-dw-653/aspire-stop.log 2>&1
```

All commands exit 0. Initial ps reports no running AppHosts. The initial snapshot includes resources waiting for security; the final snapshot reports all 18 non-parameter application/sidecar resources Running/Healthy. Captured security logs show startup completion. Stopped only the AppHost started for this pre-edit check. Startup evidence does not claim successful live query requests.

## Review and closure

Independent context-free Blind Hunter review returned six missing compatibility controls and no new production defect. All six were classified low/patch and addressed: valid Unicode through production handlers, malformed JSON containing an undecodable cursor, valid-then-malformed duplicates without codec use, present/filtered cross-user targets, ignored malformed Unicode fields, and downstream codec exception propagation. No new review deferral was added; production code is unchanged after review.

The review additions add 18 handler and two parser cases. Final additions total 98 cases (82 handler, 16 parser); focused execution includes the 14 existing parser cases.

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-653/final-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*StandardTenantQueryPaginationTests' --filter-class '*TenantQueryPaginationPayloadParserTests' --minimum-expected-tests 112 --no-ansi --no-progress --results-directory /tmp/tenants-dw-653/final-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-653/final-focused-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 1316 --no-ansi --no-progress --results-directory /tmp/tenants-dw-653/final-full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-653/final-full-tests.log 2>&1
```

All commands exit 0. Final build has one transient MSB3026 copy-retry warning for EventStore.DomainService and zero errors; the earlier repaired build's zero-warning result remains above. Final TRX counters:

| Run | Total/executed | Passed | Failed | Not executed |
| --- | --- | --- | --- | --- |
| Final focused parser/handlers | 112 | 112 | 0 | 0 |
| Final full Server | 1316 | 1316 | 0 | 0 |

Only DW-653 closes, with all original findings and prior verification fields retained. All 653 ledger IDs remain: 363 done/290 open. The maintenance index removes only its row, leaving 262 rows; its summary is recomputed from the ledger (the entry summary had not yet included DW-652's already-recorded closure). Unrelated ledger record bodies, archive, sprint status, dependency pointers and release holds remain preserved.

Final structural verification compares all ledger entry bodies against the captured entry snapshot: only DW-653 differs. Every original DW-653 nonblank line remains directly or under its `pre-dw-653-` prefix. The maintenance row list equals the entry row list with only DW-653 removed; every remaining open ID is covered exactly once, and the disposition table matches the ledger: 363 done, 203 actionable, 15 blocked, 26 awaiting-decision, nine accepted-limitation and 37 uncertain. SHA-256 checks confirm unchanged archive/sprint files. All 13 changed/new files have no trailing whitespace, every changed/new C# file has CRLF, and the full canonical entry revision is unchanged. Final `git diff --check` and story-gitlink validation both pass.

A final test-only naming correction makes the private query-type field follow the repository's `_camelCase` convention. Recheck commands and results:

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-653/recheck-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*StandardTenantQueryPaginationTests' --filter-class '*TenantQueryPaginationPayloadParserTests' --minimum-expected-tests 112 --no-ansi --no-progress --results-directory /tmp/tenants-dw-653/recheck-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-653/recheck-focused-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 1316 --no-ansi --no-progress --results-directory /tmp/tenants-dw-653/recheck-full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-653/recheck-full-tests.log 2>&1
```

All commands exit 0. The latest build has one transient MSB3026 copy-retry warning for EventStore.Server and zero errors. Latest focused/full TRX counters remain 112/112 and 1316/1316 executed/passed, zero failed/not-executed. The previous initial, green and final results above remain historical evidence; their logs are not overwritten.

The independent reviewer rechecked all six findings and confirmed them resolved, with no unresolved findings. The read-only recheck verified latest TRX counters, DW-653-only ledger closure with original evidence preserved, the exact one-row index removal, unchanged archive/sprint hashes and unchanged dependency gitlinks.

## Limits

No live HTTP, browser, Release, solution-wide or package-consumer qualification is claimed. No audit parser or unrelated deferred work is changed. Existing release holds remain. Changes are left uncommitted under the user-provided Git safeguard.
