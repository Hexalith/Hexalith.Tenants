# DW-651 validation evidence

DW-651 is complete after independent review. Malformed Unicode audit cursor and stream strings return InvalidEnvelope before codec use or audit reads. The original production run failed 16 of 155 focused cases. The final focused run is 155/155 and the full Server run is 1106/1106, with zero failures and zero skips. The Debug source build succeeds with one transient MSB3026 copy-retry warning and zero errors. Only DW-651 closes. Embedded audit-date surrogates are newly open as DW-652, and malformed Unicode cursors in the standard pagination parser are newly open as DW-653. Entry revision: `66ab4a5b3f8152535ede44670b50466d24ef588b`.

## Original production failure

New handler cases were executed against unchanged cursor and stream `GetString()` calls.

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-651/red-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GetTenantAuditQueryHandlerTests' --no-ansi --no-progress --results-directory /tmp/tenants-dw-651/red-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-651/red-focused-tests.log 2>&1
```

The first build exited 1 on CS9273/CS9258 because a theory-data variable named `field` is a keyword in a C# 14 property accessor. Renaming it to `propertyName` is a test-only correction. The rebuilt source command then exited 0 with zero warnings and zero errors.

The focused test command exited 2. TRX counters are total 155, executed 155, passed 139, failed 16, skipped 0. All 16 failures are authorized payloads and throw `InvalidOperationException` from `GetString()`: cursor decoding at the pre-fix line 316, or stream decoding at the pre-fix line 373. Non-administrator cases return Forbidden. Malformed category plus malformed stream already returns InvalidEnvelope because category rejection still precedes stream decoding. Escaped `tenant` continuation and a decoded unprotected surrogate-pair cursor already pass.

## Repaired verification

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-651/green-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GetTenantAuditQueryHandlerTests' --minimum-expected-tests 155 --no-ansi --no-progress --results-directory /tmp/tenants-dw-651/green-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-651/green-focused-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 1106 --no-ansi --no-progress --results-directory /tmp/tenants-dw-651/green-full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-651/green-full-tests.log 2>&1
```

The repaired build exits 0 with one transient MSB3026 copy-retry warning for `Hexalith.Commons.UniqueIds.dll` and zero errors. Focused TRX counters are total 155, passed 155, failed 0. Full Server TRX counters are total 1106, passed 1106, failed 0. Both test commands exit 0. No skips.

`git diff --check` exits 0. `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-651-malformed-unicode-audit-cursor-stream.md` exits 0 and reports PASS with no `references/` pointer changes. Git prints CRLF conversion warnings for the two C# files; those files remain CRLF in the working tree.

## Independent review

The context-free Blind Hunter reviewer returned eight findings. Two pre-existing decoding gaps are newly tracked as DW-652 and DW-653. The missing evidence file and stale planning note are closed by this file and the spec completion note. The other findings do not change production: duplicate JSON selection stays on `TryGetProperty`, the `InvalidOperationException` catch already covers every `GetString()` failure, tenant continuation proves the decoded stream, and bare `InvalidEnvelope` is the specified result.

Date reproduction:

```bash
dotnet fsi --exec /tmp/tenants-dw-651/datecheck.fsx
```

Exit 0. A lone `\ud800` date returns false. Trailing and swapped unpaired surrogates throw `InvalidOperationException` from `JsonElement.TryGetDateTimeOffset`. This is API evidence, not a handler run. The standard pagination finding is source inspection of `DeserializeStandardPayload` and the shared `ExecuteAsync` catch. No standard-query handler execution is claimed.

## Limits

No live audit HTTP request, browser, Release, solution-wide, or package-consumer qualification is claimed. Existing release holds remain. DW-652 and DW-653 remain open. No claim is made that every malformed query string is now contained.
