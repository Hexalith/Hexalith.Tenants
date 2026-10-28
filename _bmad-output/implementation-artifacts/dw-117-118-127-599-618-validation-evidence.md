# DW-117/118/127/599/618 read-safety validation — 2026-10-28

The approved spec is [spec-dw-117-118-127-599-618-read-safety.md](spec-dw-117-118-127-599-618-read-safety.md). Its immutable workflow baseline remains `d490461f4d6baeff4b2373ef9c54edfcd5e363a2`. Implementation entry/current HEAD is `6deb5d5dbaad4e26405910b347d45b82deae1057`, which contains the independent committed build-selection correction. That change and all dependency pins/gitlinks were preserved. This is the implementation handoff; workflow review, commit and push belong to the root agent.

## Approved outcomes

- DW-117: nonfatal provider/programming read failures render workspace Error/Retry and emit fixed diagnostic categories without exception/request material. Cancellation, superseding loads, scope changes and disposal keep their existing retirement guards. Direct and wrapped fatal failures propagate. A fixed internal signal preserves create's existing UnableToVerify behavior when a contained list read was its projection proof.
- DW-118: nonblank wrapper inputs and versioned numeric positions are qualified. Gateway null-versus-empty exception classification stays deliberate. This closes an approved contract decision, not a demonstrated reachable null-input production defect.
- DW-127/618: authorized rows fill each displayed page; one authorized lookahead enables Next. Each load examines at most 1,000 raw positions including replay/lookahead. Short/omitted returned windows advance by the fixed raw width. Protected v2 continuations carry a fixed-window start and consumed returned-result count; partial windows replay without treating returned ordinals as raw offsets. Budget exhaustion or any operational hydration gap discards incomplete search for the existing labelled authorized-list fallback. Existing per-page sorting remains.
- DW-599: FR-6 remains a server-side UI display policy. Authorized API consumers keep full configuration. UI policy requires exact display-safe keys and ordinal prefix grants; administrators bypass prefixes only. Missing/malformed policy fails UI configuration closed and raw configuration is stripped from composed state.

Architecture/project context record the approved supersession separately from the historical Story 1.9 no-backfill rule. Frozen stories were not edited.

## Final verification

All commands ran from the Tenants repository unless explicitly shown otherwise. Debug source builds retained the existing dependency selections. The narrow `HexalithMcpCliFromSource=false` validation override consumes the unchanged centrally pinned McpCli 1.1.0 package while the affected EventStore/FrontComposer/Memories projects remain source references. See the environment controls below.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -p:HexalithEventStoreVersion=3.128.4 -p:HexalithMcpCliFromSource=false -p:NuGetAudit=false -m:1

dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantQueryGatewayTests' -class '*TenantSearchCursorTests' -class '*TenantConfigurationReadPolicyTests' -class '*TenantsWorkspaceTests'

HexalithEventStoreVersion=3.128.4 dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug --no-build --no-restore -p:UseHexalithProjectReferences=true -p:HexalithEventStoreVersion=3.128.4 -p:HexalithMcpCliFromSource=false -p:NuGetAudit=false

dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -t:Rebuild -c Debug -p:UseHexalithProjectReferences=true -p:HexalithEventStoreVersion=3.128.4 -p:HexalithMcpCliFromSource=false -p:NuGetAudit=false -m:1

dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Debug --no-build --no-restore -p:UseHexalithProjectReferences=true -p:HexalithEventStoreVersion=3.128.4 -p:HexalithMcpCliFromSource=false -p:NuGetAudit=false

TENANTS_BROWSER_STANDALONE_OIDC=1 TENANTS_BROWSER_READ_SAFETY=1 node tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-administrator-dialogs.mjs /tmp/dw-read-safety-root-browser.json

node --check tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-administrator-dialogs.mjs
python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-117-118-127-599-618-read-safety.md
git diff --check
```

| Check | Result | Retained evidence |
| --- | --- | --- |
| UI Debug/source review build | Zero warnings/errors | [ui-review-build.log](evidence/dw-117-118-127-599-618/ui-review-build.log) |
| Initial gateway/cursor/config/workspace focus | 1,129 passed; zero failures/errors/skips/not-run | [ui-focused.log](evidence/dw-117-118-127-599-618/ui-focused.log) |
| Review gateway/cursor/workspace focus | 1,034 passed; zero failures/skips/not-run | [ui-review-focused.log](evidence/dw-117-118-127-599-618/ui-review-focused.log) |
| Final maintained full UI project lane | 7,986 passed; zero failures/skips | [ui-final-suite.log](evidence/dw-117-118-127-599-618/ui-final-suite.log) |
| Server Debug/source rebuild | Zero warnings/errors | [server-build.log](evidence/dw-117-118-127-599-618/server-build.log) |
| Maintained full Server project lane | 1,586 passed; zero failures/skips | [server-suite.log](evidence/dw-117-118-127-599-618/server-suite.log) |
| Authenticated production UI browser | Four cases pass: authorized first page, authorized lookahead/final page, Previous replay and Error/Retry; zero circuit errors or command attempts; freshly rebuilt and source-stable | [browser.json](evidence/dw-117-118-127-599-618/browser.json) |
| Ledger preservation | All 657 IDs; 649 done/8 open; seven exact-cover maintenance rows; all 652 unselected bodies byte-identical and selected historical fields preserved | [ledger-preservation.json](evidence/dw-117-118-127-599-618/ledger-preservation.json) |
| Story gitlinks / whitespace / browser syntax | Pass; no references pointer changes from frozen baseline through current HEAD and working tree | Commands above |

## Matrix coverage

Every matrix row ran in the passing focused/full lanes. `WorkspaceReadSafetyContainsNonfatalFailuresAndRetryRecovers` covers provider/programming/argument/cancellation categories, support-safe logging and recovery. `WorkspaceReadSafetyRetiresFailuresAfterReplacementOrDisposal`, existing authority-invalidation tests and gateway caller/cancellation tests cover stale/scope/disposal ownership. `WorkspaceReadSafetyPropagatesFatalFailures` covers direct and wrapped fatal controls. Cursor tests cover nonblank inputs, canonical versioned positions, old purpose/key/tamper rejection and complete query/caller binding. `SearchReadSafetyReplaysReturnedOrdinalsInsideFixedRawWindows` and `SearchReadSafetyEqualAuthorizedRowsHaveIdenticalRenderedPaging` cover sparse omitted/malformed/missing/hidden prefixes/suffixes, overflow/replay and equal visible paging. Budget tests cover sizes 1/3/20/100, exact 1,000 versus 1,001 positions, replay consumption, index-growth cap, hydration-gap fallback and cancellation during backfill. Existing invalid/index-shrink tests and new invalid window/consumed-position cases prove named page-one recovery. Configuration policy tests cover missing/malformed declarations, administrator rules and exact safe-key/prefix selection; `Authorized_configuration_api_preserves_full_contract_independently_of_ui_policy` qualifies the API contract for members and current administrators.

## Initial failures and environment controls

The initial command was:

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseHexalithProjectReferences=true -p:NuGetAudit=false -m:1
```

It failed in McpCli Abstractions/Analyzers with `EventStore package selection '3.128.3' must be at least 3.128.4 for completion recovery`. The environment-only `-p:HexalithEventStoreVersion=3.128.4` resolved that floor. Subsequent broad runs exposed inconsistent source assembly identities: Tenants.Contracts embeds McpCli 1.1.0 while restore/runtime outputs selected the source default 1.0.0. Server reported `FileNotFoundException` for McpCli Abstractions 1.1.0. Unchanged command tests therefore displayed unavailable support or failed reflection.

A clean `git archive HEAD` copy at `/tmp/dw-read-safety-clean-baseline` was built with the same source references/properties (explicit canonical dependency roots avoided symlink-path build differences). Its full built UI assembly ran 7,925 cases with 534 failures. Current pre-repair maintained UI ran 7,968 cases with the exact same 534 failing case identities. The [environment packet](evidence/dw-117-118-127-599-618/environment-controls.json) retains counts, equality proof and a hash of the sorted identities. No baseline/current sources or dependency pins were changed to obtain that comparison.

A global `-p:Version=1.1.0` experiment aligned McpCli but broke precompiled EventStore references requiring Contracts 3.128.4; its 114/293 support-test failures were discarded as invalid environment evidence. The narrow supported `-p:HexalithMcpCliFromSource=false` override instead restored the unchanged pinned package, followed by serialized rebuilds. The runtime McpCli DLL matches the package asset SHA-256 `166b8bace8c80aaea3e495cfcd1ef9c1e5199d279a472ba53446ce0bac8c27dd`. All 293 support-safety cases then passed. No dependency manifest, gitlink, test skip, timeout or gate was weakened.

The first repaired full UI run passed 7,968/7,969: the sole remaining test launches a child restore without inheriting command-line properties and hit the same EventStore floor. Its exact output is retained in [ui-child-restore-failure.log.gz](evidence/dw-117-118-127-599-618/ui-child-restore-failure.log.gz). Exporting `HexalithEventStoreVersion=3.128.4` into the test process environment also reaches that child; the unchanged full lane then passed all 7,969 cases. Earlier focused controls also identified obsolete raw-window test assumptions, corrected fixture wire enum encoding, pooled-log-state capture and the create proof regression; final source/tests preserve the approved policy and existing command truth state.

## Runtime and limits

Before code edits, `aspire ps --format Json` returned no running app. `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` and `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` captured the baseline. DAPR mTLS and Memories were healthy; security was running/unhealthy and dependent Tenants/UI/EventStore resources were waiting at capture. Telemetry was inspected locally. Only sanitized resource names/state/health are retained in the environment packet; raw environment, telemetry, authentication cookies/tokens and private UI logs are excluded.

The browser uses the repository's test-only loopback OIDC issuer and controlled read-only upstream, with production authentication/Razor components and rebuilt binaries. It qualifies rendered search/Next/Previous/Error-Retry behavior, not deployed backend authorization, stored domain data or injected fatal exceptions. No command is forwarded. Privacy covers rendered rows/pager within the raw scan budget; timing, work, fallback-mode independence and completeness beyond budget are excluded. Offset/index mutations retain their existing limits; no snapshot isolation is claimed. No AT/NVDA, multi-replica, deployment, vulnerability-audit, broader runtime or release qualification is claimed. Historical holds and unselected work remain unchanged.

The baseline AppHost started by this task was stopped after verification. This Aspire CLI rejected `aspire stop ... --format Json`; the supported `aspire stop --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` then succeeded and `aspire ps --format Json` returned `[]`. No pre-existing topology was stopped.

## Independent review and final root verification

Three independent review layers reported 14 findings; each verdict and refutation is recorded in the spec. Corrections preserve fatal inner exceptions through codec/index/hydration containment and preserve a fatal sibling from a combined hydration task. Nonfatal diagnostic service/provider faults cannot prevent Error/Retry. Ordinary/nested fatal wrappers, diagnostic failures, concurrent mixed hydration failures and a later authorized status match have passing regressions. Browser acceptance explicitly refuses any unexpected/simulated command or circuit error. Touched code follows Allman formatting.

After these patches, the root's maintained full UI command above passes **7,986/7,986**. The Server source/test files were unchanged by the UI-only review patches; its retained full **1,586/1,586** qualification still applies. A fresh root browser rebuild passes **4/4**, source-stable, with zero unexpected/simulated commands and circuit errors. The initial four-case packet remains separately as [browser-initial.json](evidence/dw-117-118-127-599-618/browser-initial.json). Final syntax, whitespace, story-gitlink and evidence-link checks pass. The exact Conventional Commit candidate passes pinned commitlint 21.2.3 with zero problems/warnings; see [validation](evidence/dw-117-118-127-599-618/commitlint-validation.log).

Two unnumbered follow-ups remain separately recorded: the pre-existing caller-retirement finally/outer-guard paths can replace a concurrent fatal dependency error, and whole-load latency under a slow upstream needs measurement before an elapsed deadline is chosen. Fatal-propagation qualification here covers the selected workspace/search paths with a current caller; it does not close that older cancellation/authority precedence boundary. Duplicate-candidate counterexamples were rejected using the adopted curated-index deterministic upsert/unique-candidate contract; no corrupt-index snapshot guarantee is added. Numbered totals remain 649 done/8 open, with all 652 unselected bodies byte-identical.
