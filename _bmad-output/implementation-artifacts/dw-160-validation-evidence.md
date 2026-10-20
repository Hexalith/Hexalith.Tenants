# DW-160 validation evidence — 2026-10-20

Entry revision: `84f2e8c7da6f2d7ae05412c478a94d21f83ab5c5`, clean main. The user requested the next deferred item and authorized commit/push on main. DW-159 remains an accepted limitation; DW-160 is the next dependency-free actionable entry after DW-158.

## Change and coverage

`TenantQueryGatewayTests` now configures actual NSubstitute instances of `ITenantsRestQueryClient`. Private FIFO helpers supply native `TenantsRestQueryResponse<T>` objects or explicitly arranged failure-kind/status pairs. Received-call assertions inspect the actual typed query and validator arguments. `CapturingGatewayClient`, `RestQueryClientAdapter`, `SubmittedQuery` and both generic gateway overloads are removed. No generic query envelopes or transport exceptions are manufactured. The two concurrency/cancellation substitutes call `GetTenantAsync` directly and preserve their delay, cancellation, eight-read bound and deterministic-order assertions.

The existing success fixtures retain their exact metadata defaults: unknown lifecycle, absent projection version and default `etag`, rather than adopting the stricter existing `DirectResponse` defaults. Malformed metadata remains deliberate gateway defense input. Positive member 304 retention uses matching validators; HTTP mismatch refusal remains covered by `Not_modified_requires_the_exact_strong_validator_sent_on_this_request`.

Gateway method/data inventory is unchanged: 247 async methods, 164 Fact attributes, 87 Theory attributes, 378 InlineData rows and six MemberData attributes. Native failure categories become explicit theory inputs. Two HTTP cases move the hostile `validation-failed` reason with `invalid-cursor` only in unsafe detail to its real parser boundary, covering list and global-administrator reads. Existing HTTP fixed-category cases carry the original hostile body; an existing gateway exception test carries hostile exception text and checks safe error copy. Three inherited indentation errors in the touched HTTP test file are mechanically formatted.

Only tests and maintenance artifacts change. No production, dependency, public API, browser or assistive-technology behavior qualification is claimed.

## Exact commands and results

All commands run from the owning repository root. Every build uses:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

Local `/tmp/dw160/` build logs preserve initial failures: `build-initial.log` has three missed generic enqueue rewrites and a removed CommandStatus namespace (four compile errors); `build-migrated.log` has one accidental helper signature rewrite; `build-corrected.log` has one Shouldly expression-tree `is null` error. `build-migrated-fixed.log`, `build-corrected-final.log`, `build-reviewed.log` and final `build-final.log` exit 0 with zero warnings/errors. No build gate was weakened.

Each test run has this prefix:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx
```

Add the filter/minimum below and `--results-directory /tmp/dw160/<directory>`. Each directory retains its TRX; `/tmp/dw160/results.json` records exact counters. Logs use `tests-<directory>.log`, except the explicitly named local paths below.

| Directory | Additional filter / minimum | Exit and result |
| --- | --- | --- |
| `focused-initial` | `--filter-class '*TenantQueryGatewayTests' --minimum-expected-tests 500` | 2; 705 passed, three failed, zero skipped. Both negative-cursor inputs mistakenly supplied InvalidCursor; native direct-read logging added an event that the old fallback assertion omitted. |
| `focused-corrected` | `--filter-class '*TenantQueryGatewayTests' --minimum-expected-tests 708` | 0; 708 passed, zero failed/skipped. |
| `focused-reviewed` | `--filter-class '*TenantQueryGatewayTests' --minimum-expected-tests 708` | 2; 706 passed, two failed, zero skipped. The reviewer's proposed two-call expectation for matching prior/missing-version cases was incorrect. |
| `rest-reviewed` | `--filter-class '*TenantsRestQueryClientTests' --minimum-expected-tests 100` | 0; 122 passed, zero failed/skipped, including both added hostile-body cases. |
| `focused-final` | `--filter-class '*TenantQueryGatewayTests' --minimum-expected-tests 708` | 0; 708 passed, zero failed/skipped. |
| `full-final` | `--minimum-expected-tests 6582` | 0; 6582 passed, zero failed/skipped. |

Actual focused logs are `tests-focused-initial.log`, `tests-focused-corrected.log`, `tests-focused-reviewed.log`, `tests-rest-reviewed.log`, `tests-focused-final.log` and `tests-full-final.log`. The final focused/full runs use the final rebuilt, formatted source.

Scoped formatting:

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --verify-no-changes --include tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantsRestQueryClientTests.cs -v:q
```

The initial two-file check exits 2 for three inherited HTTP-test indentation errors. After the same scoped command without `--verify-no-changes`, the final check exits 0. Logs: `format-reviewed.log`, `format-final-apply.log`, `format-final-verify.log`. `git diff --check` exits 0; Git only reports its existing CRLF-to-LF checkout normalization notice.

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-160-typed-query-fixture.md
node node_modules/@commitlint/cli/cli.js --edit /tmp/dw160/commit-message.txt --verbose
```

Own-spec guard: PASS, no reference pointer changes. Pinned commitlint CLI 21.2.3: zero problems/warnings for the exact complete message `refactor(ui): replace obsolete query gateway test transport` plus final newline; successful output is retained in `/tmp/dw160/commitlint.log`.

## Review and runtime limits

A context-free Blind Hunter reviewed the full diff and returned seven findings. Six patches are accepted: both invalid-cursor fixtures, native logging, hostile-body parser coverage, non-vacuous exception/body redaction, and valid positive 304 validators. The seventh finding claimed the missing-version cases reached queue exhaustion during refetch. The attempted correction fails exactly those two cases: actual call counts are one. Source in `ListTenantsCoreAsync` and `GetGlobalAdministratorsCoreAsync` returns retained degradation at `!HasSupportedProjectionVersion` when prior evidence matches. Final tests assert one call and consumed responses. The reviewer accepts that disproof and all six patches, with no remaining concrete issue. Rechecks are read-only; executed tests are the parent results above. Nothing is deferred.

Before edits, `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --isolated --format Json --non-interactive` starts the isolated topology. `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` reports all 23 resources Running/Healthy. `aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --severity Error --limit 5 --format Json` contains pre-existing Memories/EventStore HTTP 500 retry errors. `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` succeeds. Other pre-existing AppHosts are preserved. This is a runtime baseline, not a clean end-to-end qualification.

## Ledger closure

Only DW-160 closes. Every replaced historical field is retained under `pre-dw-160-*`; all 656 IDs remain, 399 done/257 open. The 232 remaining maintenance rows cover each open ID exactly once. Unrelated ledger bodies, archive, sprint status, dependency pointers and existing release qualification holds are preserved. Release readiness is unchanged.
