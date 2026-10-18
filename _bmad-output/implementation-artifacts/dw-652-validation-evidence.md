# DW-652 validation evidence — 2026-10-18

Entry revision: `2143c8e51bdf37babb4f29dd5953d24fd25e754c`, clean `main` at activation. Scope is DW-652 only: contain audit-date decoding exceptions locally while preserving authorization precedence, cursor scope and ordinary date filtering.

## Initial reproduction

Added 106 handler cases before changing production. They exercise both date bounds, embedded/reversed/separated unpaired surrogate escapes, malformed UTF-8 bytes, authorized/unauthorized callers, absent/supplied invalid cursor, inclusive offset bounds with equivalent escaped spellings and protected continuation, and duplicate-date final-member selection.

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-652/red-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GetTenantAuditQueryHandlerTests' --minimum-expected-tests 261 --no-ansi --no-progress --results-directory /tmp/tenants-dw-652/red-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-652/red-focused-tests.log 2>&1
```

Build exits 0, one transient MSB3026 copy-retry warning and zero errors. Test exits 2: 261 total, 217 passed, 44 failed, zero skipped. All 44 failures throw `InvalidOperationException` through the original `TryGetDateTimeOffset` date accessor. Unauthorized cases return Forbidden. Eight malformed UTF-8 administrator cases already return InvalidEnvelope; they are compatibility controls rather than reproduced exceptions. Four final-malformed duplicate-member cases throw; valid/null final duplicates succeed.

### Durable initial-failure excerpt

The original focused log records this authorized `to` case (prefixes abbreviated to repository paths):

```text
MalformedUnicodeDateReturnsFailureBeforeCodecOrAuditReadAsync(propertyName: "to", date: "2026-05-14T10:00:00Z\\udc00", withCursor: False, userId: "admin-1")
System.InvalidOperationException: Cannot read invalid UTF-16 JSON text as string. Invalid surrogate value: '0xDC00'.
at System.Text.Json.JsonReaderHelper.TryGetEscapedDateTimeOffset(...)
at System.Text.Json.JsonDocument.TryGetValue(...)
at TenantQueryHandlerBase.TryGetDateTimeOffset(...) (TenantQueryHandlerBase.cs:415)
at TenantQueryHandlerBase.DeserializeAuditPayload(...) (TenantQueryHandlerBase.cs:311)
at GetTenantAuditQueryHandler.ExecuteCoreAsync(...) (GetTenantAuditQueryHandler.cs:38)
```

The corresponding TRX counters and repaired results are retained here independently of temporary files:

| Run | Total/executed | Passed | Failed | Not executed |
| --- | --- | --- | --- | --- |
| Original production, initial tests | 261 | 217 | 44 | 0 |
| First repaired focused | 261 | 261 | 0 | 0 |
| First repaired full Server | 1212 | 1212 | 0 | 0 |

### Recreate the baseline failure

Use a disposable workspace copy with the same root-declared dependency checkouts and pinned SDK. Copy the current handler test file into that copy, then replace only `src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs` there with the output of `git show 2143c8e51bdf37babb4f29dd5953d24fd25e754c:src/Hexalith.Tenants/Queries/Handlers/TenantQueryHandlerBase.cs`. Do not replace the repaired production file in the working workspace. Run the source Debug build and focused MTP commands above, using `--minimum-expected-tests 267` for the final test file. Embedded-date cases then exercise the uncaught baseline conversion; final tests include six additional review controls, so the historical 261-case counters describe the actual initial run, not a rerun of that expanded test file. Restore in the disposable copy as needed before the `--no-restore` build. This paragraph is reproduction guidance, not a claim that this additional disposable-copy run was executed.

## Repaired verification

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-652/green-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GetTenantAuditQueryHandlerTests' --minimum-expected-tests 261 --no-ansi --no-progress --results-directory /tmp/tenants-dw-652/green-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-652/green-focused-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 1212 --no-ansi --no-progress --results-directory /tmp/tenants-dw-652/green-full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-652/green-full-tests.log 2>&1
```

All three commands exit 0. Build has three transient MSB3026 copy-retry warnings and zero errors. Focused TRX: 261 total/passed, zero failed/skipped. Full Server TRX: 1212 total/passed, zero failed/skipped. The repair catches only InvalidOperationException at the date conversion and returns the existing invalid-date result. Earlier malformed whole-value, non-string, null, optional, category and cursor cases remain in the focused class.

`git diff --check` exits 0. `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-652-malformed-unicode-audit-dates.md` exits 0 with PASS and no references/ pointer changes. Git warns that the CRLF working-tree C# files will normalize to LF when Git next touches them; both files retain CRLF on disk.

## Runtime baseline

```bash
aspire ps --format Json
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/tenants-dw-652-aspire-baseline.json
aspire logs security --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --tail 5 --format Json > /tmp/tenants-dw-652-aspire-security.json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json > /tmp/tenants-dw-652/aspire-baseline-final.json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj > /tmp/tenants-dw-652/aspire-stop.log 2>&1
```

Initial ps exits 0 with no running AppHosts. The requested start registered this AppHost and subsequent describe/log/stop commands exit 0. The final pre-repair snapshot has application projects Running and security readiness Healthy; Dapr CLI resources are Finished. Security logs show startup completion. This establishes startup state only, without claiming successful live audit requests or healthy sidecars. Only the AppHost started for this check was stopped.

## Review and closure

Independent context-free Blind Hunter review returned five findings. None established a new production defect. All were classified low/patch: completion bookkeeping, reproducible baseline guidance, durable initial evidence, UTC/offset continuation and mixed valid/malformed bounds. The latter two added six compatibility cases; the exact initial failures and intermediate build warnings above remain preserved. No review deferrals were added.

After these changes, the following maintained checks ran:

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-652/final-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GetTenantAuditQueryHandlerTests' --minimum-expected-tests 267 --no-ansi --no-progress --results-directory /tmp/tenants-dw-652/final-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-652/final-focused-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 1218 --no-ansi --no-progress --results-directory /tmp/tenants-dw-652/final-full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-652/final-full-tests.log 2>&1
```

All exit 0. Final build has zero warnings/errors. Final focused TRX has 267 executed/passed, zero failed/not-executed; full Server TRX has 1218 executed/passed, zero failed/not-executed. There are 112 new handler cases total. Source has no post-review production edits. The independent reviewer rechecked and confirmed all five findings resolved, including the six added cases and final build/TRX evidence.

Only DW-652 changes to done/resolved, preserving original findings and prior verification fields. All 653 IDs remain: 362 done/291 open. Its one maintenance row is removed, leaving 263 rows. DW-653 and unrelated ledger bodies, archive, sprint status and existing release holds remain unchanged. A final structural preservation check compared every ledger entry against the captured entry snapshot: only DW-652 differs; every original DW-652 field is retained directly or under its pre-dw-652 prefix. Archive/sprint SHA-256 comparisons pass. Maintenance rows cover every remaining open ID exactly once. Final diff whitespace and story-gitlink checks pass; C# CRLF checks pass.

## Limits

No live HTTP audit, browser, Release, solution-wide or package-consumer qualification is claimed. DW-653 remains separate and open. Existing release holds remain. The change does not claim to contain every malformed string in every query path. Changes are left uncommitted under the user-provided Git safeguard.
