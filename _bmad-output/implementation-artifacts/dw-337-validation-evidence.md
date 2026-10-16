# DW-337 tracked dispatch ambiguity validation — 2026-10-16

Implementation and review of [the approved spec](spec-dw-337-tracked-dispatch-ambiguity.md) are complete; only DW-337 closes. Its empty context list, baseline and frozen intent are unchanged. Final Debug source/maintained Release each pass 768 focused and 5,954 full UI tests, zero failures/skips, plus both unchanged Chromium lanes. Release remains not ready; the existing broad/package/runtime and separate qualification holds below remain.

## Implementation and maintained coverage

One private classifier now applies the established global-administrator transport rule before the existing permanent mapper in all six tracked methods. Explicit retryability, HTTP 408/429/>=500, transport HTTP errors, timeouts and cancellation with an uncanceled caller retain RequestSent, original message ID, existing operation key and ambiguity. Caller cancellation and JSON/unrelated exceptions propagate. Permanent/domain mappings, public signatures, validation, untracked wrappers, resources, admission, proof, status, audit and deadlines retain their implementations.

The maintained gateway suite grows from 346 to 768 cases (422 added). Its six-operation matrix covers 21 uncertain inputs × synchronous throws/faulted tasks × six operations = 252 ambiguity cases. Gateway status inputs include 408/429/500/503/599 with null/false/true flags and recognizable domain rejection evidence, plus retryable 400/409; four transport exception types complete the matrix. Every ambiguity case verifies exact EN/FR shipped whole-message copy, safe result fields, one initial send, then an explicit successful retry with the same ID, tenant, domain, aggregate, command type and payload; no ID factory call occurs. Additional cases cover 72 caller-cancellation/JSON/unrelated propagation controls, 60 permanent HTTP controls, six operation-specific domain cases (each checks sync/faulted and null/false retryability), six invalid-request/malformed-ID controls, and 26 expanded existing global cases. Existing classification/localization assertions remain intact.

Final focused gateway runs pass 768/768 in Debug source and maintained Release. Full individual-project UI runs pass 5,954/5,954 in each configuration with zero failures or skips, exceeding the unchanged 5,532 floor. Both unchanged Chromium lanes pass their original assertions and negative controls. The last test-source-only field-casing normalization is compiled and fully rechecked again below on the exact final binaries.

## Initial failures and corrections

Before production edits, the final red run executes 768 cases: 676 pass, 92 fail, zero skipped, exit 2. Every failure belongs to `TrackedAmbiguityRetainsExactEnvelopeForExplicitRetry`: retryable 400/409 and plain cancellation/timeouts in the four target methods, plus lifecycle domain rejection taking precedence over uncertain transport statuses. This reproduces the approved defect with both synchronous and asynchronous failures. The unchanged global behavior and all terminal/cancellation/validation controls pass.

An earlier 744-case fixture run had 116 failures: the same 92 production cases and 24 harness identity checks because Shouldly's generic async-throw helper normalized cancellation into a new TaskCanceledException. Using xUnit's direct `Record.ExceptionAsync` preserves the original exception; uncanceled JSON/unrelated controls were also added. A test invocation that overlapped that test-only rebuild read the prior binary and was discarded; the serialized 768-case red run above is the authoritative pre-fix evidence. No production workaround, weakened assertion, exclusion or timeout change was used.

## Exact commands and final results

All commands run from `/home/administrator/projects/hexalith/tenants`. Build/test flags retain the repository's maintained FrontComposer-source exception. Test runs add only output/TRX reporting flags to the spec commands: `--no-ansi --no-progress --results-directory /tmp/dw-337-<lane> --report-xunit-trx --report-xunit-trx-filename <focused|full|red>.trx`.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantCommandGatewayTests' --minimum-expected-tests 1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 5532
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -p:UseHexalithProjectReferences=false -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore --filter-class '*TenantCommandGatewayTests' --minimum-expected-tests 1
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore --minimum-expected-tests 5532
env TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
env TENANTS_BROWSER_BUILD_CONFIGURATION=Release bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-337-tracked-dispatch-ambiguity.md
git diff --check
dotnet build Hexalith.Tenants.slnx -c Release -p:UseHexalithProjectReferences=false -warnaserror -m:1 -nr:false
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -p:UseHexalithProjectReferences=false -p:HexalithFrontComposerFromSource=false -warnaserror -m:1 -nr:false
```

| Lane | Exit and result | Raw session evidence |
| --- | --- | --- |
| Red Debug source build | 0; zero errors | `/tmp/dw-337-red-build.log` |
| Authoritative red gateway run | 2; 676 passed / 92 failed / zero skipped | `/tmp/dw-337-red-test.log`, `/tmp/dw-337-red-results/red.trx` |
| Debug source UI build | 0; zero errors | `/tmp/dw-337-debug-build.log` |
| Debug focused gateway | 0; 768 passed / zero failed or skipped | `/tmp/dw-337-debug-focused.log`, `/tmp/dw-337-debug-focused/focused.trx` |
| Debug full UI | 0; 5,954 passed / zero failed or skipped; 94.133s | `/tmp/dw-337-debug-full.log`, `/tmp/dw-337-debug-full/full.trx` |
| Maintained Release UI build | 0; zero errors | `/tmp/dw-337-release-build.log` |
| Release focused gateway | 0; 768 passed / zero failed or skipped | `/tmp/dw-337-release-focused.log`, `/tmp/dw-337-release-focused/focused.trx` |
| Release full UI | 0; 5,954 passed / zero failed or skipped; 94.688s | `/tmp/dw-337-release-full.log`, `/tmp/dw-337-release-full/full.trx` |
| Debug / Release Chromium | Both 0; original assertions and all negative controls pass | `/tmp/dw-337-debug-browser.log`, `/tmp/dw-337-release-browser.log` |
| Story-gitlink validation | 0; no references pointer changes relative to original baseline | `/tmp/dw-337-gitlinks.log` |
| Whitespace validation | 0 | `/tmp/dw-337-diff-check.log` |

Fresh broad and strict-package checks preserve separate blockers. The exact broad Release solution command exits 1 at restore with two NU1903 errors for pinned `Microsoft.AspNetCore.SignalR.Common` 10.0.12 (UI tests and EventStore.SignalR). The exact strict package UI command exits 1 with seven errors in unchanged `TenantSummaryProjectionPage.razor`: CS1061 for `TenantSummaryProjectionState.ScopeGeneration`, plus CS0117 for `ScopeTenantId`, `ScopeUserId` and `OriginScopeGeneration` on each of `TenantSummaryProjectionLoadRequestedAction` and `TenantSummaryProjectionLoadedAction`. Raw logs: `/tmp/dw-337-broad-build.log` and `/tmp/dw-337-strict-package-build.log`. These remain release holds; no package/audit pin, source exception or assertion changed. Maintained-lane restoration and final rechecks are recorded below. Narrow builds retain two NU1903 warnings for pinned `Microsoft.AspNetCore.SignalR.Common` 10.0.12 under existing advisory policy; no audit/dependency flag changes.

## Aspire pre-edit qualification

Using the permitted repository Aspire skill, `aspire ps --format Json` initially returns `[]`. Before production/test edits:

```bash
aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire wait tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 45
aspire logs security --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire otel logs --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json
aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj
aspire ps --format Json
```

The initial security startup wait clears. `aspire wait` exits 0 with tenants-ui healthy in 4.8 seconds. Final describe shows all 22 other resources Running/Healthy, with memories-dapr-cli Finished and no health status. Telemetry retains 69 Memories warnings and a replay-safety critical entry with `sidecar-query-exception`; this is a qualified UI baseline, not full distributed-topology acceptance. The exact owned AppHost stops successfully; final `ps` is `[]`. An initial stop invocation incorrectly supplied unsupported `--format Json` and exited 1; the corrected exact-target command above exits 0. Raw start/describe/wait/telemetry/stop artifacts remain in `/tmp/dw-337-aspire-*`; dashboard login tokens are not copied here. No AppHost/resource/dependency configuration changed.

## Exact source and dependency provenance

Tenants remains on clean-entry `main`, HEAD `5f41f089157ef077c5145f34a260004cafa5349c`. Source is the uncommitted diff over that HEAD. Pre-review normalized two-file source diff SHA-256: `5b363f5082d91a7d81508c713aa76ddcd51968ea9447330943e65263357ea4ec`. No stage/commit/push, branch/dependency/gitlink mutation, submodule initialization, deployment or data replay ran.

All nine root-declared dependency HEADs were independently rechecked against [planning provenance](dw-337-planning-evidence.json); every dependency worktree is clean:

| Root dependency | Exact HEAD |
| --- | --- |
| `references/Hexalith.AI.Tools` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` |
| `references/Hexalith.Builds` | `f9f115528e43060d5f0af7dfbd8b22addc3c9a89` |
| `references/Hexalith.Commons` | `9e874b1a3c6e7b6ff07b6f40b81d36e4e1403798` |
| `references/Hexalith.EventStore` | `746b843a24f18b17a7572be25b76b735fa8e4ca6` |
| `references/Hexalith.FrontComposer` | `b06f86d554363ea5b8192cbc149a75c820dab2b8` |
| `references/Hexalith.McpCli` | `7e99305a621e1bdf7119b6e924348857e2b5a5a4` |
| `references/Hexalith.Memories` | `99431f0fed102ea6ddbae33d28b9da4fc7f215a0` |
| `references/Hexalith.Platform` | `3a29fe039320ab13d95ff7d9801761dad72bfed9` |
| `references/Hexalith.PolymorphicSerializations` | `98de6e013840ece9f0fa7c68ab7dcdf2bba3b375` |

Pre-review checkout-byte SHA-256 (C# retains configured CRLF):

| Input | SHA-256 |
| --- | --- |
| `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs` | `dff0e01cf540f9bc90219e438fe57ff870fd8310cbcc1c2f62cc9fc12d712e37` |
| `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` | `45f5ed2bfc31296bb5d50691251a524fd111943bd09d316004a287e1b9f55419` |
| `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` | `f1ce82216a887ecb5d5849c764fdbb1ff841d0d6369d775fd0b9667a4bf07483` |
| `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` | `90720136819f66bb90207750ce4fa5daf9385731b1558c0fcfa5ff24adacd319` |
| `Directory.Build.props` | `d527484a6528bd36087a222951b7b8b3cdc46f1f886c6c7b38ae6be2f95750a0` |
| `Directory.Packages.props` | `c8351c554ea90792eae487eadd8d6a08faeb0719e62341a929d81f7f44fdcd08` |
| `global.json` | `25b36d45123efa1d03786506dadad7546069f8563cddd4b509849b5c3631e9f5` |
| `tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh` | `212978b9da7d83092809f7782a510d1f43b17294843b81668f3579e1bbac7a25` |

Only the two intended code/test hashes differ among the 19 planning-preserved inputs. The other 17—including archive, sprint, completed localization spec/evidence, the three consumer flows, build inputs and test helpers—remain byte-identical at implementation qualification. Resources, browser runner/assertions, governance gates, dependencies and release configuration are unchanged. Final compiled-output and test-evidence hashes are recorded after restoration below.

## Independent review and remaining holds

Independent review is pending the parent workflow. No ledger/backlog/readiness closure is asserted by this pre-review evidence. DW-285/DW-470 remain separate acceptance reconciliation; DW-469 audit/retry identity and DW-625 global recovery remain separate work. No completed closure, original finding/decision, archive or sprint entry is rewritten.

Release remains not ready. Preserve all historical provenance failures, authority/recovery/governance decisions, raw-secret-scan/CI requirements, upstream nested-layout/provider/startup blockers, current-candidate NFR10/performance evidence, hosted TLS/browser, production authority/revocation, authenticated browser/clipboard/denial and human assistive-technology holds. Passing local UI/gateway/Chromium checks clear none of these independent holds.

## Final restoration and exact-binary recheck

After the strict-package failure, both spec builds were rerun serially with the original maintained flags. They exit 0 with zero errors and the same two retained NU1903 warnings; logs are `/tmp/dw-337-final-debug-build.log` and `/tmp/dw-337-final-release-build.log`. This recompiles the naming-only test-field change. The exact final source binaries then pass focused and full suites again without assertions, exclusions, timeouts or gates changing:

| Final lane | Exit | Passed / total | Failed / skipped | Raw TRX |
| --- | --- | --- | --- | --- |
| Debug focused | 0 | 768 / 768 | 0 / 0 | `/tmp/dw-337-final-debug-focused/focused.trx` |
| Debug full | 0 | 5954 / 5954 | 0 / 0 | `/tmp/dw-337-final-debug-full/full.trx` |
| Release focused | 0 | 768 / 768 | 0 / 0 | `/tmp/dw-337-final-release-focused/focused.trx` |
| Release full | 0 | 5954 / 5954 | 0 / 0 | `/tmp/dw-337-final-release-full/full.trx` |

Final full-run durations are 104.516 seconds Debug and 103.223 seconds Release. Test commands are the exact shapes above, with reporting directories `/tmp/dw-337-final-<debug|release>-<focused|full>` and matching `focused.trx`/`full.trx` filenames. The final focused TRX files independently register 252 ambiguity cases, 72 propagation cases, 60 HTTP-terminal cases, six domain cases and six validation/ID cases; existing grant/remove theories each register 21 cases. All outcomes are Passed. Final gitlink and whitespace checks again exit 0; final source/config/resource/browser hashes match the table above.

SDK: `10.0.401`. The restored maintained Release UI dependency graph resolves `Hexalith.Commons.UniqueIds/2.30.1`, `Hexalith.EventStore.Client/3.121.0`, `Hexalith.EventStore.Contracts/3.121.0`, `Hexalith.Memories.Client.Rest/2.27.1`, `Hexalith.Memories.Contracts/2.27.1`; FrontComposer remains project-sourced through the unchanged maintained exception. Debug uses the explicit source-reference flag and the exact root dependency HEADs above.

| Pre-review compiled output | SHA-256 |
| --- | --- |
| `src/Hexalith.Tenants.UI/bin/Debug/net10.0/Hexalith.Tenants.UI.dll` | `3053ac1369232431afd0b029b825d95959d23689a2e4af2cbfa8de259e55c5e2` |
| `tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll` | `2d681b8ee81071e6765adc6a4b5d26269190359c8df39aaf09fa24917a064613` |
| `src/Hexalith.Tenants.UI/bin/Release/net10.0/Hexalith.Tenants.UI.dll` | `eb985fde1e6d5062b45cf490019293e4c1dd91f4e96dd05b79e768e8a7943037` |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests.dll` | `3824e924046ae1d5a18fbf799ab526e4adedc73da2b147b5574e937c2cc2bcd8` |

Raw result evidence hashes (session files retain full outputs):

| Artifact | SHA-256 |
| --- | --- |
| `/tmp/dw-337-red-test.log` | `290782b8ea399b1b8f5d39c3a4ad257ba627cd5633c74d388a89e536521288de` |
| `/tmp/dw-337-red-results/red.trx` | `e366b840ea99313a5d93f24c6aa34e697763ba1a899fba624d15ee75381d3594` |
| `/tmp/dw-337-debug-browser.log` | `b3399dd610040e78d38bcbe38a2818c9e2e7420c38061246e4ae6326cd84a5f0` |
| `/tmp/dw-337-release-browser.log` | `ece037edde022a41e0f61e0e96f470e353aa893c3d354b973a35d9079f99a4b2` |
| `/tmp/dw-337-broad-build.log` | `77deb39cf02ffc14dbf361873b035c03cc35449e8dbc8af0e7dd54f5bef5d5d7` |
| `/tmp/dw-337-strict-package-build.log` | `df7c3c7bcc24cd8ed3ff09e5f0c3d3a299ed79f63e9b583fb84642d33cdd3bb7` |
| `/tmp/dw-337-final-debug-build.log` | `5167fb417e0e56f19beae228d1ab16d7e34c8ea41b775b3ac48f73588e64a194` |
| `/tmp/dw-337-final-release-build.log` | `0fb662be869db99edb06404dbe134b1d74a556aa32dd46adf23fb6b88bff8bc6` |
| `/tmp/dw-337-final-debug-focused/focused.trx` | `9505711eb281499e505e0765a490bc4b78d10a175c1ce3ad53b1ba0f5b08a48c` |
| `/tmp/dw-337-final-debug-full/full.trx` | `b2832b28e1f184ec816c646410bcde6e18147e491b0a4819aabe2b9ea6ae297b` |
| `/tmp/dw-337-final-release-focused/focused.trx` | `fe955eaaa85b2aa679ae0bfbbbcf4a3904bb81a7f84c2a5e47046df338366bfc` |
| `/tmp/dw-337-final-release-full/full.trx` | `505b213f4702ad1ad83a167aa895b48bb6dd47b5e67b138ca51d13ef152abf76` |

The source/dependency state remains unchanged after these rechecks. Parent independent review and conditional acceptance reconciliation remain the only outstanding workflow steps; no product implementation or maintained check is left incomplete. The fresh broad/package and qualified Aspire limits above remain separate holds.

## Parent acceptance and bounded reconciliation

The parent independently read the unified diff (including untracked planning/evidence/spec files) and the final Debug/Release focused/full TRX results. All eight frozen matrix rows have executed passing coverage. The final focused results include 42 ambiguity/retry and 12 cancellation/propagation cases per operation, 60 HTTP-terminal controls, six domain cases with internal synchronous/faulted/null/false controls and six validation/identity cases. Each full suite passes all 5,954 cases, and no new case is skipped. No product defect or missing acceptance case was found. The separate workflow review is recorded in the owning spec.

After acceptance, only DW-337 changes to done. Its original finding, both September 6 decisions and pre-fix verification/action values remain. The maintenance index removes only its row and updates current counts; readiness receives an appended bounded-closure record. Totals are 643 IDs, 337 done/306 open across 274 rows, with 133 reconciled done/218 actionable/15 blocked/26 awaiting-decision/nine accepted limitations/38 uncertain. Readiness partition: seven decision holds/111 follow-up/188 qualification or triage. DW-285/DW-470 and every unrelated record remain unchanged. Archive, sprint, completed evidence, dependencies and all previously stated release holds are preserved.

Earlier pending-reconciliation statements describe the pre-review handoff; this acceptance supersedes that pending state. Reconciliation intentionally changes three documentation hashes after implementation qualification; no production/test/config/resource/browser input changes after the final maintained runs.

## Completed independent workflow review and final verification

All three context-free reviewers ran at the parent model capability. Edge-case hunter returned no findings and the verification-gap reviewer found no gaps. Blind hunter's ten suggestions were individually verified and triaged in the owning spec. One low diagnostic issue was patched: gateway ambiguity exception details now include status and retryability, preserving the hostile canary, every input and every assertion. All 252 six-operation ambiguity/retry result names, and 21 names in each legacy global ambiguity theory, are now distinct. No runtime defect, intent/spec gap, deferred finding or loopback remains. Production source is identical to the reviewed production implementation.

The implementation agent's narrow patch check passes 294 affected tests with 294 distinct names (`/tmp/dw-337-review-label-tests/labels.trx`). The parent then reran the complete spec verification serially, including fresh broad/strict-package checks, restored maintained builds, focused/full suites, both unchanged Chromium lanes and provenance/whitespace checks. The exact final commands and session outputs are:

| Lane | Exit | Result | Seconds | Raw session log |
| --- | ---: | --- | ---: | --- |
| broad-build | 1 | BLOCKED: two unchanged SignalR NU1903 errors | 2.866 | `/tmp/dw-337-post-review-broad-build.log` |
| strict-package-build | 1 | BLOCKED: seven unchanged FrontComposer projection-scope API errors | 3.688 | `/tmp/dw-337-post-review-strict-package-build.log` |
| debug-build | 0 | PASS | 12.153 | `/tmp/dw-337-post-review-debug-build.log` |
| debug-focused | 0 | 768 passed; 0 failed/skipped | 1.913 | `/tmp/dw-337-post-review-debug-focused.log` |
| debug-full | 0 | 5954 passed; 0 failed/skipped | 96.754 | `/tmp/dw-337-post-review-debug-full.log` |
| debug-browser | 0 | PASS; original assertions/negative controls | 30.885 | `/tmp/dw-337-post-review-debug-browser.log` |
| release-build | 0 | PASS | 10.151 | `/tmp/dw-337-post-review-release-build.log` |
| release-focused | 0 | 768 passed; 0 failed/skipped | 1.988 | `/tmp/dw-337-post-review-release-focused.log` |
| release-full | 0 | 5954 passed; 0 failed/skipped | 103.413 | `/tmp/dw-337-post-review-release-full.log` |
| release-browser | 0 | PASS; original assertions/negative controls | 44.868 | `/tmp/dw-337-post-review-release-browser.log` |
| gitlinks | 0 | PASS | 0.232 | `/tmp/dw-337-post-review-gitlinks.log` |
| diff-check | 0 | PASS | 0.226 | `/tmp/dw-337-post-review-diff-check.log` |

```bash
dotnet build Hexalith.Tenants.slnx -c Release -p:UseHexalithProjectReferences=false -warnaserror -m:1 -nr:false
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -p:UseHexalithProjectReferences=false -p:HexalithFrontComposerFromSource=false -warnaserror -m:1 -nr:false
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantCommandGatewayTests' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/dw-337-post-review-debug-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 5532 --no-ansi --no-progress --results-directory /tmp/dw-337-post-review-debug-full --report-xunit-trx --report-xunit-trx-filename full.trx
env TENANTS_BROWSER_BUILD_CONFIGURATION=Debug bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -p:UseHexalithProjectReferences=false -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore --filter-class '*TenantCommandGatewayTests' --minimum-expected-tests 1 --no-ansi --no-progress --results-directory /tmp/dw-337-post-review-release-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore --minimum-expected-tests 5532 --no-ansi --no-progress --results-directory /tmp/dw-337-post-review-release-full --report-xunit-trx --report-xunit-trx-filename full.trx
env TENANTS_BROWSER_BUILD_CONFIGURATION=Release bash tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-337-tracked-dispatch-ambiguity.md
git diff --check
```

The separate broad/strict-package failures are retained as blockers, not counted as passing gates. Neither policy nor dependency changed. Both maintained configurations were rebuilt after those attempts. The following post-review hashes supersede the pre-review test-source/binary hashes above; those historical runs and raw logs remain intact.

| Current final input or result | SHA-256 |
| --- | --- |
| `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs` | `dff0e01cf540f9bc90219e438fe57ff870fd8310cbcc1c2f62cc9fc12d712e37` |
| `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` | `24393eb26cbedffeb6e432d6feb852fbfb3bc9d377eaba4fd7debe84b4c3428b` |
| `src/Hexalith.Tenants.UI/bin/Debug/net10.0/Hexalith.Tenants.UI.dll` | `3053ac1369232431afd0b029b825d95959d23689a2e4af2cbfa8de259e55c5e2` |
| `tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll` | `6bd4cdeaf4bd55baf3ab6e11e2563646c9f4cb18bb3ab869ca4737d51e36bb9e` |
| `src/Hexalith.Tenants.UI/bin/Release/net10.0/Hexalith.Tenants.UI.dll` | `eb985fde1e6d5062b45cf490019293e4c1dd91f4e96dd05b79e768e8a7943037` |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests.dll` | `a953a3782476a48e974973dc70336d2bf9ff096cfce9095facc5b2b6c53144f2` |
| `/tmp/dw-337-post-review-debug-focused/focused.trx` | `d09ca0568fd7dd0aa9ea9cfa6fe8eecd0e4b9d9946847abd15e0167d9b422fdd` |
| `/tmp/dw-337-post-review-debug-full/full.trx` | `1499f05e7da9809744187e15a5a723ba74099798da3031eb12ee6935f40e4364` |
| `/tmp/dw-337-post-review-release-focused/focused.trx` | `046d6d6c1fe29b404327f9cb1a4cd1d91569015054a74eba611f9fe3e2aa53d4` |
| `/tmp/dw-337-post-review-release-full/full.trx` | `29c1decad4ff2d64370d177ab586561dab43b2f035463e4332246fb72a7f0644` |
| `/tmp/dw-337-post-review-commands.json` | `f881ead3eea9d16ca8b2f9cad0a883506a42e68371497bc374dbb7c942ec6ec6` |
| `/tmp/dw-337-parent-audit.json` | `ba1c66324ed2cea99bd9e4e86e329b9772dd3314d05261f42aaac9c44556dee4` |

Current normalized two-file source diff SHA-256: `d85e2c0bd2ce61d05b03759fbc71254afb45afa6ebd006b2f06643f32ab4af61`.

Parent preservation/count audit passes: exactly DW-337 changes among 643 ledger records; all 642 unrelated records are byte-identical; 274 maintenance rows cover exactly 306 open IDs once each. The original readiness document is an unchanged prefix with one appended DW-337 record. All nine dependency HEADs/worktrees and the index are unchanged. Among the 19 planning-preserved inputs, only the two code/test files and three authorized reconciliation documents differ; archive, sprint, earlier localization evidence, build inputs, helpers and consumer flows remain byte-identical. The earlier audit helper was corrected to count legacy closed records from their authoritative status rather than expect disposition fields they do not carry; final counts and preservation assertions pass.

No production/test/config/resource/browser change followed this final verification. The post-review result-name audit executes every frozen matrix group and passes in both full and focused configurations. Independent review and conditional reconciliation are complete; only DW-337 closes, and release remains not ready under all preserved holds.

## Accepted walkthrough follow-up — 2026-10-16

The user accepted the walkthrough recommendations after the review above. This later follow-up adds exactly one test, `TrackedDispatchPropagatesCallerCancellationWhileAwaitingTransport`, and clarifies the owning spec. The earlier review, counts and hashes remain historical evidence; the Debug test-source/binary hashes and counts below supersede those earlier Debug test inputs for this follow-up. Production gateway source and the Debug UI assembly retain their exact earlier hashes. No additional independent review is claimed for this follow-up.

The new fact holds a transport task pending through the gateway's await, verifies the exact forwarded caller token and original message ID, then cancels the caller and completes the transport task as canceled. It asserts propagated cancellation with the same token, exactly one transport call and zero ID-factory calls. Coordination uses a task completion source with asynchronous continuations; there are no sleeps, timing thresholds or expanded retry matrices. Set configuration is the representative tracked operation; the existing six-operation matrix remains unchanged.

The spec now distinguishes gateway classification and explicit caller-supplied same-envelope retries from complete UI recovery. Configuration status lookup still requires a correlation ID, and set configuration clears the entered value after submission. No new configuration redispatch action, payload retention or recovery contract is introduced. The existing private classifier, caller-owned retries, cancellation behavior and EN/FR messages remain in place.

Fresh commands from the repository root:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -warnaserror -m:1 -nr:false
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --filter-class '*TenantCommandGatewayTests' --minimum-expected-tests 769 --no-ansi --no-progress --results-directory /tmp/dw337-walkthrough-iac5_4_y/debug-focused --report-xunit-trx --report-xunit-trx-filename focused.trx
dotnet test --project tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore --minimum-expected-tests 5955 --no-ansi --no-progress --results-directory /tmp/dw337-walkthrough-iac5_4_y/debug-full --report-xunit-trx --report-xunit-trx-filename full.trx
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-337-tracked-dispatch-ambiguity.md
git -c core.safecrlf=false diff --check
```

| Follow-up check | Exit | Result |
| --- | ---: | --- |
| Debug source UI test-project build | 0 | Zero errors; the same two pinned SignalR NU1903 warnings remain under the existing advisory policy |
| Debug focused gateway | 0 | 769 passed; zero failed/skipped; 1.458 seconds |
| Debug full UI project | 0 | 5,955 passed; zero failed/skipped; 122.579 seconds |
| Story-gitlink validation | 0 | No dependency pointer changes |
| Whitespace and preservation checks | 0 | Frozen intent and all unrelated saved worktree files remain byte-identical |

Both TRX files independently contain exactly one passing result for the added fact. Raw build/test logs, TRX results, hash manifest, preservation snapshots and parsed result counters are session artifacts under `/tmp/dw337-walkthrough-iac5_4_y/`.

| Current follow-up input or binary | SHA-256 |
| --- | --- |
| `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs` | `dff0e01cf540f9bc90219e438fe57ff870fd8310cbcc1c2f62cc9fc12d712e37` |
| `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs` | `3489a8c2e2784662045e33a3abadfbd22420e14d205c42ba5f8dcdad02c2f29d` |
| `src/Hexalith.Tenants.UI/bin/Debug/net10.0/Hexalith.Tenants.UI.dll` | `3053ac1369232431afd0b029b825d95959d23689a2e4af2cbfa8de259e55c5e2` |
| `tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll` | `0699eaa285e7b46933dcac2ded9f87afab119747fd1867bf8d4357c840529726` |

The required pre-edit Aspire baseline used `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json`, `aspire wait tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --timeout 45`, final `aspire describe` and `aspire otel logs` with that same exact AppHost. All 23 resources reached Running/Healthy; UI readiness completed in 5.5 seconds. The captured 200 telemetry entries include 53 warnings and seven errors, so this is a qualified pre-edit baseline, not additional runtime acceptance. A UI-filtered telemetry call could not resolve the unsuffixed resource name; the corrected unfiltered telemetry call succeeded. The AppHost started for this check was stopped successfully with the exact-target `aspire stop`; final `aspire ps --format Json` returned `[]`.

Only the gateway test file, owning spec and this appended evidence change in the follow-up. C# CRLF is preserved. Release, browser, broad-solution and strict-package lanes were not rerun for this test/documentation-only follow-up; their earlier evidence and all separate release/recovery holds remain. No production behavior, resource, dependency, build input, gate, ledger disposition or index changes, and no stage/commit/push/deployment was performed.
