# DW-349 / DW-353 / DW-355 / DW-407 / DW-535 validation

Date: 2026-10-21. Baseline: `be14af75cbabe05c9500475c7163f55606dd618d` on clean `main`.

## Change and limits

The five records share the audit evidence surface. `TenantAuditPage.LoadAsync` now captures its prior snapshot, filters, cursor and validator, validates local input and begins the operation on the renderer. Its entry-generation/tenant/disposal guard rejects obsolete queued preparation. Invalid filters explicitly render their local refusal. Category input must equal an actual supported enum name; numeric, padded, combined and lowercase aliases cannot submit a query.

`TenantAuditNarrative` accepts seconds with explicit uppercase `T` and `Z` or an offset of `±HH:mm`, and an optional decimal fraction of 1–7 digits. The private bounded shape check compensates for the runtime parser's permissive shortened offsets and empty fraction. Parsed offsets/precision survive in typed evidence; display remains canonical UTC round-trip text. Only the existing narrative dictionary keys `timestamp` and `occurredAt` are affected. The typed audit entry timestamp, server narrative-key inventory and projection/authorization/command semantics do not change. Compact offset variants are deliberately outside this bounded UI grammar.

DW-407 adds six held first-page retry failure cases after explicit invalid-cursor transport results: caller replacement, sign-out and unchanged-caller controls, each with typed Forbidden and unexpected-exception variants. They assert result kind, no rows/validator/next cursor/HasMore, current caller scope, exactly two calls and preserved nonnull stream, filters and page size with cleared retry cursor/validator. The caller-bound transport and outer publication guard already protect this path; this is end-to-end recovery coverage and does not prove each inner retry-catch guard independently necessary.

DW-535 strengthens all ten existing rendered state cases to exact role/live pairs, including Unauthorized `alert`/`assertive`. Component evidence does not claim browser or screen-reader acceptance.

## Startup and builds

Run from the repository root:

```bash
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
```

Startup succeeds. Initial inspection shows Memories running/healthy, security running/unhealthy and downstream resources waiting. No distributed-health acceptance is claimed; raw runtime configuration remains outside the repository.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
```

Initial, corrected, original-control and restored reviewed builds succeed with zero warnings/errors. The final restored build takes 13.90 seconds. No dependencies or pointers are changed.

## Focused checks and initial failures

All test commands use the repository's maintained Microsoft.Testing.Platform lane. The base command is:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --no-ansi --no-progress --report-xunit-trx --report-xunit-trx-filename results.trx
```

Append one row's arguments per invocation:

| Stage | Additional arguments | Result |
| --- | --- | --- |
| Initial page | `--filter-class '*TenantAuditPageTests' --minimum-expected-tests 240 --results-directory /tmp/tenants-audit-five/page` | 246/246 pass, zero failures/skips |
| Initial gateway | `--filter-class '*TenantQueryGatewayTests' --minimum-expected-tests 700 --results-directory /tmp/tenants-audit-five/gateway` | 714/714 pass, zero failures/skips |
| Initial narrative | `--filter-class '*TenantAuditNarrativeTests' --minimum-expected-tests 44 --results-directory /tmp/tenants-audit-five/narrative` | 21 pass/23 fail/zero skips; exit 2 |
| Corrected narrative | `--filter-class '*TenantAuditNarrativeTests' --minimum-expected-tests 44 --results-directory /tmp/tenants-audit-five/narrative-corrected` | 44/44 pass, zero failures/skips |

The initial parser used mixed `fFFFFFF` tokens, which reject the valid fractional forms, and `zzz` alone, which accepts compact/shortened offsets. The corrected implementation uses `FFFFFFF` plus a bounded shape check. Two later canonical numeric-UTC/trailing-zero compatibility cases increase the narrative count to 46.

## Original-code controls

Save patched source bytes outside the repository, replace only `TenantAuditPage.razor` and `TenantAuditNarrative.cs` with the baseline's exact source contents, and run the same Debug source build. Keep the final tests. Start the following no-build controls against that original-source assembly, then restore the saved patched source bytes before rebuilding:

| Additional arguments to the base command | Original-code result |
| --- | --- |
| `--filter-class '*TenantAuditNarrativeTests' --minimum-expected-tests 46 --results-directory /tmp/tenants-audit-five/original-narrative` | 24 pass/22 fail/zero skips; exit 2 |
| `--filter-method '*AuthenticationReloadPublishesLocalFilterErrorsAsync' --minimum-expected-tests 3 --results-directory /tmp/tenants-audit-five/original-auth` | 0 pass/3 fail/zero skips; exit 2 |
| `--filter-method '*QueuedAuditLoadCapturesAndValidatesOnlyAfterRendererEntryAsync' --minimum-expected-tests 1 --results-directory /tmp/tenants-audit-five/original-queue` | 0 pass/1 fail/zero skips; exit 2 |
| `--filter-method '*CategoryRequiresAnExactNamedValue' --minimum-expected-tests 8 --results-directory /tmp/tenants-audit-five/original-category` | 1 pass/7 fail/zero skips; exit 2 |

The authentication cases use the real delayed authentication notification, with reflection only to seed raw filter input because normal input handlers already validate/render and would mask the load-path repaint gap. The queued case holds the renderer with explicit synchronization, invokes the private load entry from a worker, then proves snapshot identity, load generation, validation keys and query count remain unchanged until renderer execution. It changes the raw input before releasing the renderer, so capturing stale filter values also fails. Its `finally` always releases the held renderer. The baseline fails on the changed snapshot before release. No sleep-based scheduling or private field writes stand in for the gateway recovery behavior.

## Review and formatting

Blind Hunter calculated its five-finding floor from 22.97 kB. All five findings receive simple corrections: the queued-dispatch proof, honest caller-guard coverage wording, a nonnull stream selector, canonical server timestamp controls and exact grammar documentation. The independent recheck confirms all five are resolved with no remaining issue. Full per-finding classification is retained in the owning spec; nothing is newly deferred.

```bash
dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --include src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditNarrative.cs tests/Hexalith.Tenants.UI.Tests/State/TenantAuditNarrativeTests.cs tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs --verify-no-changes --verbosity quiet
```

The first scoped formatter run exits 2 with 22 existing `WHITESPACE` diagnostics in `TenantAuditPageTests`. Mapping diagnostic lines to equal blocks of a full-baseline/current line comparison proves every diagnostic falls on an exact unchanged line and none touches added/changed lines. Unrelated formatting is preserved.

## Final reviewed validation

After all five review corrections and restoration of patched source, the maintained focused runs append these arguments to the base command above:

| Additional arguments | Result |
| --- | --- |
| `--filter-class '*TenantAuditPageTests' --minimum-expected-tests 247 --results-directory /tmp/tenants-audit-five/page-reviewed` | 247/247 pass |
| `--filter-class '*TenantQueryGatewayTests' --minimum-expected-tests 714 --results-directory /tmp/tenants-audit-five/gateway-reviewed` | 714/714 pass |
| `--filter-class '*TenantAuditNarrativeTests' --minimum-expected-tests 46 --results-directory /tmp/tenants-audit-five/narrative-reviewed` | 46/46 pass |

All 1,007 focused cases pass with zero failures/skips. The repeated final formatter has the same 22 unchanged-line diagnostics and zero on changed lines.

The lockfile-pinned and installed `@commitlint/cli` is 21.2.3. The exact full intended message, `fix(ui): harden audit validation and recovery evidence`, passes this command with zero problems/warnings:

```bash
./node_modules/.bin/commitlint --edit /tmp/tenants-audit-five/commit-message.txt --verbose
```

A pre-push fetch of `origin main` succeeds, and `git rev-list --left-right --count HEAD...origin/main` reports `0 0` before the new commit.

The full final reviewed UI lane uses this command:

```bash
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 6949 --no-ansi --no-progress --results-directory /tmp/tenants-audit-five/full-reviewed --report-xunit-trx --report-xunit-trx-filename results.trx
```

It passes 6,949/6,949 with zero failures/skips/errors in 1m 58s 745ms (exit 0), including all final review corrections. Production source bytes match the reviewed snapshots restored after the original-code controls.

Only after that result, `python3 /tmp/tenants-audit-five/close-ledger.py` checks TRX totals, compares every parsed ledger record with the saved entry snapshot, preserves each replaced historical value under `pre-audit-safety-*` and verifies exact unique open-ID coverage in the maintenance index. Exactly DW-349/DW-353/DW-355/DW-407/DW-535 change disposition. All 657 IDs remain: 420 done/237 open across 216 maintenance rows. See [retained ledger validation](dw-349-353-355-407-535-ledger-validation.json). The archive, sprint tracking, unrelated records, dependency pointers and existing release holds remain intact.

```bash
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-349-353-355-407-535-audit-safety.md
git diff --check
```

Both pass; the story has no references pointer changes. This qualifies local Debug source/component/gateway behavior. No Release package, distributed-health, browser, screen-reader or production-release acceptance is claimed.
