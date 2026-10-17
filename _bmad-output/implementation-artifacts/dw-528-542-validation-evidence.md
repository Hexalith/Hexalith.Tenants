# DW-528/DW-542 bootstrap response limit evidence — 2026-10-17

**Completed after the authorized failure repair:** the final Debug source build has zero warnings/errors; all 31 focused documentation/configuration/owner-query checks and all 951 Server tests pass, with zero failures/skips. DW-528/DW-542 are closed and the spec is `done`. The earlier three-failure result and pending acceptance are retained below as history. No test or gate was disabled, and no runtime query policy, package, dependency pointer or release hold changed.

## Follow-up repair and final acceptance

The user explicitly requested fixing the remaining failures. The existing `ResolveTenantOwnerAuthority` request and `TenantOwnerAuthorityResult` were missing from `docs/event-contract-reference.md`; the SDK discovery fixture also lacked their already-hosted reader/clock dependencies and the exact seventh handler/metadata entry. The documentation now covers the contracts, camelCase payload, computed response metadata, failure versus non-owner verdict, runtime prerequisites, source limits and operation-specific policy ownership. The fixture retains exact discovery expectations and additionally dispatches the owner query against a complete source and a fresh unavailable source, asserting exact stream identity, payload and metadata.

The first follow-up build used the command below with output in `failure-repair/build.log` and exited 1 on MSB3248 because a shared EventStore Contracts DLL was locked. Repeating it with output in `failure-repair/retry-build.log` exited 0 after two transient MSB3026 copy retries. These logs remain available. The first focused run passed 31/31 and full Server run passed 951/951. Review then requested stronger consumer documentation and actual owner-route dispatch evidence; the final commands after those corrections are:

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-528-542/failure-repair/review-build.log 2>&1
```

Exit 0, zero warnings/errors.

```bash
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*EventContractReferenceDocumentationTests' --filter-class '*EventPublicationConfigurationTests' --filter-class '*TenantOwnerAuthorityQueryHandlerTests' --minimum-expected-tests 31 --no-ansi --no-progress --results-directory /tmp/tenants-dw-528-542/failure-repair/final-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-528-542/failure-repair/final-focused-tests.log 2>&1
```

Exit 0, 31/31 passed, zero failures/skips.

```bash
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 951 --no-ansi --no-progress --results-directory /tmp/tenants-dw-528-542/failure-repair/final-full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-528-542/failure-repair/final-full-tests.log 2>&1
```

Exit 0, 951/951 passed, zero failures/skips. TRX inspection confirms all three originally failing methods pass. All 24 added bootstrap cases remain in the full run, with their source and tests unchanged by the follow-up.

The follow-up blind reviewer reported eight findings. Seven concerned consumer details and dispatch evidence; all were patched, recorded individually in the spec and accepted on source recheck. The eighth concerns current evidence and bookkeeping, addressed by this section and the selected ledger closures. No new deferred goal was introduced.

Current ledger totals are 647 IDs, 353 done/294 open across 265 exact-covering maintenance rows. Only DW-528/DW-542 close, retaining both their original and pending-stage fields. Archive/sprint, all other 645 entry bodies, the original bootstrap source/test repair and full entry revision are preserved. Nine task files remain unstaged/untracked. The initial seven-file, open-record preservation checks below remain historical; current completion checks are recorded at the end.

## Initial acceptance hold (historical)

The token-response repair is implemented and independently reviewed, but **completion remains pending**. The final source build has zero warnings/errors; focused bootstrap/configuration tests pass 87/87. The full Server project has three failures that also reproduce with the unchanged entry provider. The required Hexalith baseline states: "All existing and new tests must pass before a story is complete." Both ledger records therefore remain open/actionable and the [spec](spec-dw-528-542-bootstrap-response-limit.md) remains `in-review`.

Entry revision: `42d2f21e2081e44e174a8afdb5dae951909308c6`, clean `main`. Temporary logs, TRX files, entry snapshots and the comparison compiler hook are under `/tmp/tenants-dw-528-542`. No staging, commit, dependency update, submodule initialization, publish or data repair ran.

## Initial repair behavior and review

`TenantBootstrapCredentialProvider` sends the existing password-grant form with `ResponseHeadersRead`. The existing bounded `LoadIntoBufferAsync` now sees the unbuffered response and enforces 65,536 bytes before parsing a token. A linked cancellation deadline starts before sending and covers the headers and body using the existing client's timeout, including infinite-timeout support. Failed HTTP status responses are disposed without consuming their bodies. Existing delegated-subject checks and support-safe hosted-service failure diagnostics are preserved.

Twenty-four new test cases cover 65,535/65,536/65,537-byte responses with declared/unknown lengths; understated length; bounded consumption of 1 MiB bodies with four chunk sizes; error bodies; cancellation with finite/infinite client timeouts; a shared header/body deadline; disposal; and complete hosted-service acceptance/rejection with exact bearer forwarding and secret-free diagnostics. The stream fixture counts consumed bytes, not process heap allocations. Bounded buffering includes normal transport/copy overhead; a universal 4 KiB overread bound is not claimed.

The workflow's blind reviewer reported five findings: four test-evidence gaps and pending ledger bookkeeping. All five were classified and addressed in the spec. The reviewer rechecked all four coverage corrections and found no additional source defect. No new goal was deferred.

## Initial repair validation commands and outcomes (historical)

Initial build with new regression tests and the original provider — exit 0, zero warnings/errors:

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --nologo -v:q > /tmp/tenants-dw-528-542/red-build.log 2>&1
```

Initial red test run — exit 2, 76 total / 68 passed / eight failed / zero skipped. The failures were two size-boundary overflows, an unknown-length overflow, three consumed error bodies and two hosted overflows reaching command dispatch:

```bash
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantBootstrapHostedServiceTests' --minimum-expected-tests 50 --no-ansi --no-progress --results-directory /tmp/tenants-dw-528-542/red --report-xunit-trx --report-xunit-trx-filename red.trx > /tmp/tenants-dw-528-542/red-tests.log 2>&1
```

The initial repaired build (`green-build.log`) passed with zero warnings/errors. Its focused run (`focused-tests.log`, `focused/focused.trx`) passed 76/76. The first full-project run (`full-tests.log`, `full/full.trx`) ran 942 cases, with 939 passed and three failed. Its minimum-expected-tests value of 1700 was an incorrect initial estimate, producing an additional runner-policy failure; no test was skipped. The final commands below use the actual discovered counts.

After all review corrections and the baseline comparison, the ordinary final build — exit 0, zero warnings/errors:

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true --no-restore --nologo -v:q > /tmp/tenants-dw-528-542/final-build.log 2>&1
```

Final focused lane — exit 0, 87 total / 87 passed / zero failed / zero skipped:

```bash
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantBootstrapHostedServiceTests' --filter-class '*BootstrapConfigurationTests' --minimum-expected-tests 85 --no-ansi --no-progress --results-directory /tmp/tenants-dw-528-542/final-focused --report-xunit-trx --report-xunit-trx-filename focused.trx > /tmp/tenants-dw-528-542/final-focused-tests.log 2>&1
```

Final complete Server project — exit 2, 951 total / 948 passed / three failed / zero skipped. The minimum-count policy is satisfied:

```bash
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 951 --no-ansi --no-progress --results-directory /tmp/tenants-dw-528-542/final-full --report-xunit-trx --report-xunit-trx-filename full.trx > /tmp/tenants-dw-528-542/final-full-tests.log 2>&1
```

The three failures are:

- `EventContractReferenceDocumentationTests.Event_contract_reference_lists_every_public_contract_type`: `ResolveTenantOwnerAuthority` is missing from `docs/event-contract-reference.md`.
- `EventContractReferenceDocumentationTests.Event_contract_reference_documents_every_public_contract_member_and_enum_value`: `ResolveTenantOwnerAuthority.ActorClass` is undocumented.
- `EventPublicationConfigurationTests.TenantsDomainServiceSdkRegistration_ReportsHandlerServedQueryTypesAndDispatchesQuery`: the fixture cannot resolve `IAuthoritativeEventStreamReader` for `TenantOwnerAuthorityQueryHandler`.

## Unchanged-provider comparison

The original provider bytes were obtained with `git show 42d2f21e2081e44e174a8afdb5dae951909308c6:src/Hexalith.Tenants/Bootstrap/TenantBootstrapCredentialProvider.cs` and written to `/tmp/tenants-dw-528-542/BaselineTenantBootstrapCredentialProvider.cs`. No repository source was reverted or overwritten for this comparison. The temporary `baseline-provider.targets` contains:

```xml
<Project>
  <Target Name="UseBaselineBootstrapProviderForComparison" BeforeTargets="CoreCompile" Condition="'$(MSBuildProjectName)' == 'Hexalith.Tenants'">
    <ItemGroup>
      <Compile Remove="Bootstrap/TenantBootstrapCredentialProvider.cs" />
      <Compile Include="/tmp/tenants-dw-528-542/BaselineTenantBootstrapCredentialProvider.cs" />
    </ItemGroup>
    <Message Text="Baseline comparison: compiling unchanged HEAD bootstrap provider from /tmp" Importance="high" />
  </Target>
</Project>
```

Comparison build — exit 0; its log confirms the original provider was selected. One transient MSB3026 EventStore file-copy retry warning occurred and recovered. The subsequent ordinary final build above has zero warnings:

```bash
dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -p:CustomBeforeMicrosoftCommonTargets=/tmp/tenants-dw-528-542/baseline-provider.targets --no-restore --nologo -v:m > /tmp/tenants-dw-528-542/baseline-build.log 2>&1
```

Comparison test run — exit 2, the same three tests fail with the same causes; zero passed/skipped:

```bash
dotnet test --project tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore --filter-method '*EventContractReferenceDocumentationTests.Event_contract_reference_lists_every_public_contract_type' --filter-method '*EventContractReferenceDocumentationTests.Event_contract_reference_documents_every_public_contract_member_and_enum_value' --filter-method '*EventPublicationConfigurationTests.TenantsDomainServiceSdkRegistration_ReportsHandlerServedQueryTypesAndDispatchesQuery' --minimum-expected-tests 3 --no-ansi --no-progress --results-directory /tmp/tenants-dw-528-542/baseline --report-xunit-trx --report-xunit-trx-filename baseline.trx > /tmp/tenants-dw-528-542/baseline-tests.log 2>&1
```

The final ordinary build removes the temporary compiler substitution, and the final focused lane proves the repaired provider is compiled. No failing test or gate was disabled.

## Runtime baseline and scope limits

Before production edits, `aspire ps --format Json` returned `[]`. `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive --format Json` succeeded, and `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` showed all 18 non-parameter resources running and healthy. Existing `aspire otel logs tenants --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json --search 'Bootstrap failed' --limit 1` reported event 2002 at `command-dispatch`, `TimeoutRejectedException`, on the unchanged runtime. This is separate from authority response acquisition; no successful live bootstrap command is claimed.

No Release, solution-wide, browser or unrelated test-project qualification is claimed for this bootstrap-only repair. Existing release holds remain unchanged. The code repair and focused evidence do not close the required full Server gate.

The owned baseline AppHost was stopped with `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` — exit 0. The subsequent `aspire ps --format Json` no longer listed the Tenants AppHost. Another workspace's running FrontComposer AppHost was left alone.

## Initial preservation checks (historical)

`python3 /tmp/tenants-dw-528-542/verify-preservation.py` — exit 0, PASS. Comparison with entry snapshots confirms exactly DW-528/DW-542 changed, all their historical fields remain either current or under `pre-dw-528-*`/`pre-dw-542-*`, and all other 645 entry bodies are byte-for-byte unchanged. All 647 IDs remain: 351 done, 296 open, with 266 index rows covering each open ID exactly once. The archive and sprint files, full entry revision and index gitlinks are unchanged. All three changed/new C# files use CRLF. TRX parsing independently confirms the 87/87 focused result and that the three final full-suite failures exactly match the unchanged-provider comparison. The checker's initial field-name expression omitted digits from preserved-field names; correcting that local checker produced the passing result without any ledger correction.

`python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-528-542-bootstrap-response-limit.md` — exit 0, PASS, no `references/` pointer changes against the full recorded entry baseline.

`git diff --check` — exit 0, no whitespace errors. Git emits its normalization warning for tracked CRLF C# worktree files; direct byte checks confirm the required worktree endings. No tracked formatting policy was changed.

The independent reviewer's final evidence/bookkeeping recheck confirmed the implemented repair and pending acceptance are consistently recorded, with no concrete contradiction. No commit message was generated or used; all seven task files remain unstaged/untracked.

## Current completion checks

`python3 /tmp/tenants-dw-528-542/failure-repair/verify-completion.py` — exit 0, PASS. The checker preserves the earlier open-record checker and its initial results. Current comparison confirms only DW-528/DW-542 changed from the original 647 entry bodies, both close with their original and pending-stage fields retained, and the remaining 294 open IDs are covered exactly once by 265 maintenance rows. Totals are 353 done/294 open. Archive/sprint, the full entry revision and index gitlinks are unchanged. The three bootstrap source/test files match their hashes from the beginning of the user-authorized failure repair; all four changed/new C# files use CRLF. Exactly nine task files are unstaged/untracked, with no staged change.

TRX checks confirm 31/31 current focused cases, 951/951 current full Server cases, all three formerly failing methods now passed and all 24 new bootstrap cases passed. The earlier 87/87 focused bootstrap/configuration evidence is retained independently. Results are recorded in `/tmp/tenants-dw-528-542/failure-repair/completion-results.json`.

`python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-528-542-bootstrap-response-limit.md` — exit 0, PASS, no pointer changes. `git diff --check` — exit 0, no whitespace errors; Git's CRLF normalization notices are unchanged and the direct byte checks pass. Existing release qualification holds remain; this closes the selected bootstrap defects and their required Server validation, without asserting a new release qualification.

The follow-up reviewer's final evidence/bookkeeping recheck resolved finding eight: evidence, spec, ledger, index and completion results agree, historical failures remain recorded and no contradiction was found. All eight follow-up findings are resolved; no new deferred entry was added.
