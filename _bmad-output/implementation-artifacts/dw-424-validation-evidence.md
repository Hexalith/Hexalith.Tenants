# DW-424 validation evidence — 2026-10-18

DW-424 is complete after independent review and correction recheck. The final Debug source-reference build passes with zero errors and one transient MSB3026 file-copy retry warning; the final focused suite passes 59/59 and full Server passes 1010/1010, both with zero failures/skips. Only DW-424 closes. Review's pre-existing category Unicode decoding defect is separately open as DW-650. Entry revision: `20a395af8086db9e62ec64fd9ce5f4b1e6e3a09e`.

## Behavior and scope

`TenantQueryHandlerBase.DeserializeAuditPayload` now rejects malformed supplied timestamp strings, non-string timestamps and non-string/non-null categories with `invalid-envelope`. The existing `GetTenantAuditQueryHandler` authorizes before parsing and refuses parser errors before cursor decoding or audit reads. Omitted/null fields, blank category strings, existing string-category parsing, valid date offsets, inclusive boundaries, tenant isolation, streams and pagination retain their established behavior.

The initial 39 production-handler cases covered both bounds with five invalid timestamp strings and five wrong JSON values each; five non-string category values; seven optional-filter cases; two valid date/category combinations; and five authorization-precedence cases. Review expanded the final suite to 59 cases, detailed below. Negative malformed-filter cases assert no response payload, no cursor invocation and exactly the current-authority read. Positive cases assert exact row IDs from a mixed-category fixture with outside-bound and foreign-tenant rows. These are isolated handler tests over substituted read-model storage, not live HTTP or persisted-state integration evidence.

## Initial failures and original-parser control

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --nologo -v:q > /tmp/tenants-dw-424/red-build.log 2>&1
```

Exit 1: three test-fixture compilation errors (CS9273, CS9258, CS1503) from naming an iterator `field` in a C# 14 property accessor; two MSB3026 copy-retry warnings were also emitted. Renaming the iterator to `propertyName` corrected the fixture.

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-424/red-build-recheck.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GetTenantAuditQueryHandlerTests' --minimum-expected-tests 39 --no-ansi --no-progress --results-directory /tmp/tenants-dw-424/red --report-xunit-trx --report-xunit-trx-filename red.trx > /tmp/tenants-dw-424/red-tests.log 2>&1
```

Build exit 0, zero errors and one transient MSB3026 copy-retry warning. Original-parser test exit 2: 39 total, 25 failed/14 passed/zero skipped. Ten wrong-type timestamp cases threw `InvalidOperationException`; fifteen malformed-string/category cases reached the deliberately unconfigured cursor substitute and returned `invalid-cursor` instead of the required earlier `invalid-envelope`. This control demonstrates the missing rejection boundary; it does not claim an HTTP result or live audit read occurred.

## Repaired checks

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-424/green-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GetTenantAuditQueryHandlerTests' --minimum-expected-tests 39 --no-ansi --no-progress --results-directory /tmp/tenants-dw-424/focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-424/focused-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 990 --no-ansi --no-progress --results-directory /tmp/tenants-dw-424/full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-424/full-tests.log 2>&1
```

All exit 0. Build: zero warnings/errors. Focused: 39/39. Full Server: 990/990. Both test runs have zero failures/skips, with their TRX counts inspected.

```bash
git diff --check
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-424-malformed-audit-filters.md
```

Both exit 0. Git prints its existing CRLF-to-LF normalization notice; no whitespace error is reported. The story validator reports PASS, with no `references/` pointer changes from the full recorded entry revision through the working tree.

## Runtime baseline and limits

`aspire ps --format Json` initially listed another workspace's Folders AppHost. `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive --format Json` succeeded. Initial exact-target describe found security readiness failing with a TLS EOF and dependent services waiting; a subsequent identical `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` succeeded with security Healthy and all non-parameter resources Running except the finished Memories Dapr command. Raw describe output remains local under `/tmp/tenants-dw-424/` and is not copied into tracked evidence because it includes operational details. `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` exited 0 and stopped the instance started for this baseline. No command targeted the Folders AppHost.

No Release, solution-wide, browser or live audit HTTP qualification is claimed for this parser-only change. Existing release holds remain unchanged. No staging, commit, branch, push or dependency update was performed.

## Independent review and expanded checks

The context-free Blind Hunter reviewer returned five findings, each triaged in the spec. The existing final-occurrence policy for duplicate JSON filter members is preserved and now pinned in both member orderings. Coverage corrections add numeric-string categories `0`/`1`/`99`, one-sided dates and active filters combined with null/blank fields, and malformed filters combined with a supplied invalid cursor. Additional lone-surrogate date cases cover the repaired date-decoding boundary. The expanded focused suite contains 59 cases.

Review also confirmed an existing exception on escaped lone-surrogate **category strings** at the unchanged `GetString()` accessor. This falls outside the selected non-string-category repair and is recorded separately as DW-650. Date surrogates are rejected by the repaired path; no fix for category string decoding is claimed. The reviewer rechecked every coverage correction and confirmed that the duplicate-member tests retain final-occurrence semantics in both orders, with no new issue found.

The first expanded-test build used the same Debug source command above with output `/tmp/tenants-dw-424/review-build.log`. It exited 1 with two CA1062 errors on the new expected-row string parameters. Explicit `ArgumentNullException.ThrowIfNull` checks corrected those fixture boundaries; no analyzer suppression was added.

Final expanded commands:

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-424/final-build.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*GetTenantAuditQueryHandlerTests' --minimum-expected-tests 59 --no-ansi --no-progress --results-directory /tmp/tenants-dw-424/final-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-424/final-focused-tests.log 2>&1
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 1010 --no-ansi --no-progress --results-directory /tmp/tenants-dw-424/final-full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-424/final-full-tests.log 2>&1
```

All exit 0. The build reports one MSB3026 transient copy retry for the source DomainService DLL and zero errors. Final focused: 59/59; final full: 1010/1010, both with zero failures/skips. The earlier zero-warning build and 39/990 results remain above as intermediate evidence.

The reviewer used `dotnet fsi --quiet --readline-` with an unsaved stdin script to invoke the built `TenantQueryTestHarness` by reflection. Its current-handler checks return `invalid-envelope` for both surrogate forms in either date and throw `InvalidOperationException` for both category forms. The baseline comparison evaluated the original accessor expressions, rather than building a second baseline handler: dates become null and category decoding throws. This limits the historical claim to the unchanged accessor behavior.

A separate minimal accessor control was saved as `/tmp/tenants-dw-424/category-surrogate.fsx`:

```fsharp
open System
open System.Text.Json
for payload in [ """{"category":"\ud800"}"""; """{"category":"\udc00"}""" ] do
    use document = JsonDocument.Parse(payload)
    try
        document.RootElement.GetProperty("category").GetString() |> ignore
        printfn "Unexpected success"
    with error -> printfn "%s" (error.GetType().FullName)
```

`dotnet fsi --exec /tmp/tenants-dw-424/category-surrogate.fsx > /tmp/tenants-dw-424/category-surrogate-script.log 2>&1` exits 0 and prints `System.InvalidOperationException` twice. An earlier interactive stdin run printed the same exceptions but exited 1; the saved script provides the repeatable successful control. Neither control modifies production code or records a baseline-handler build.

## Backlog reconciliation

All 649 original IDs and unrelated record fields remain, plus the separate DW-650 finding. Only DW-424 changes to done, retaining its original fields under `pre-dw-424-*` and its original historical finding. Totals: 650 IDs, 359 done/291 open across 263 maintenance rows. The archive, sprint file, entry revision and dependency pointers remain unchanged.

`python3 /tmp/tenants-dw-424/verify-completion.py` exits 0, PASS: it compares the original ledger records, confirms only DW-424 changed and DW-650 was added, asserts exact index coverage without duplicates, verifies archive/sprint hashes and unchanged HEAD/index, and inspects final TRX counts and CRLF source/test line endings. The initial inline version of this checker incorrectly required every done date to end its line and excluded four existing `(B7 closure)` suffixes; after inspecting those unchanged original statuses, the saved checker explicitly accepts that suffix. No tracked guard or historical record was changed to make the check pass. Final `git diff --check` and the story-gitlink command both exit 0; the latter reports PASS with no pointer changes. Six task files remain unstaged/untracked.
