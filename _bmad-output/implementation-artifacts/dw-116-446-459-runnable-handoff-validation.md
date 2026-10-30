# Runnable deferred AT handoff validation

The [handoff](../../docs/tenants-ui-deferred-at-handoff.md) now runs through opt-in modes of the existing authenticated administrator fixture. Terminal stdin selects search availability, one fallback-continuation rejection, status evidence and projection visibility independently. The existing signed OIDC issuer, production UI/cookie/circuit stack and loopback HTTP/SignalR upstream are reused. No production files, control service, domain data or dependencies changed.

Implemented over Tenants `0d469d7c8fedc383f6258cbf2798e62cf9847304`. Exact source, helper, FrontComposer, UI/Shell/Fluent assembly and served Fluent module identities are retained in each packet. The tested Fluent package is 5.0.1. Source fingerprints remained stable through build and cleanup and the three final handoff packets match the final runner/helper bytes. [Exact tested fixture inputs](evidence/dw-116-446-459-runnable-handoff/tested-fixture-inputs.zip) preserve those bytes across Git/Windows text checkout normalization; [manifest](evidence/dw-116-446-459-runnable-handoff/manifest.json) hashes refer to the local tested bytes.

| Check | Result and evidence |
| --- | --- |
| Fresh Debug/source production UI build | Each browser/manual invocation builds with `-c Debug -m:1 -p:UseNuGetDeps=false -p:HexalithEventStoreVersion=3.128.4 -p:HexalithMcpCliFromSource=false -p:NuGetAudit=false`; success recorded in its packet. |
| Chromium 154.0.8037.57 / Playwright 1.63.0 | [28/28 EN/FR cases](evidence/dw-116-446-459-runnable-handoff/chromium.json): S0–S3, first C1/C2 availability insertion, both C3 Refresh controls and independent projection confirmation. Eight fixture-only Creates, zero unexpected commands and zero circuit errors. |
| Firefox 155.0 / Playwright 1.63.0 | [28/28 EN/FR cases](evidence/dw-116-446-459-runnable-handoff/firefox.json), same eight simulated Creates and zero unexpected commands/circuit errors. This Linux managed-browser run is not a Windows/NVDA pairing qualification. |
| Existing authenticated read-safety mode | [4/4 cases](evidence/dw-116-446-459-runnable-handoff/existing-read-safety.json): authorized search, Next, Previous and Error/Retry; zero commands/circuit errors. |
| Headed terminal workflow, Linux/Xvfb | [Manual smoke packet](evidence/dw-116-446-459-runnable-handoff/manual-terminal.json): help/show, search/cursor/status/projection controls, fresh FR reset, quit and cleanup. `passed=null`, `manualSessionCompleted=true`, no automated qualification verdict. No manual Create or speech trial is claimed. |
| Review refusal regression | [Reproducible Node script](evidence/dw-116-446-459-runnable-handoff/refusal-reproduction.mjs): seven malformed, missing-auth, unsupported, null and array command/claim shapes are refused without throwing and counted exactly once. |
| Syntax and whitespace | Three changed browser modules pass `node --check`; `git diff --check` passes. Git emits its existing CRLF-to-LF normalization warning for the administrator runner. |
| Protected records | [Preservation hashes](evidence/dw-116-446-459-runnable-handoff/preservation.json): the entire ledger, archive and sprint-status files are byte-identical to baseline. DW-116/446/459 remain open; DW-137 and DW-466 are unchanged. |
| Commit message | Owning pinned commitlint CLI reports zero problems/warnings for the exact candidate; [validation output](evidence/dw-116-446-459-runnable-handoff/commitlint.txt). |

The browser checks use the same terminal-control parser as manual mode. Search goes through the actual input/Tab/debounce interaction. S3 asserts that a cursorless Refresh cannot consume the armed rejection, one continuation is rejected, its cursorless authorized recovery succeeds, and the next continuation succeeds. C1 uses the production gateway's exact-attempt EventsStored identity verification; C2 uses HTTP 200 `{}`, not propagation-lag 404. Projection hold returns immediate absent evidence. Both refresh paths retain the selected first state and one dispatch; projection release confirms C1 without changing status selection. Sanitized UTC events cover button activations, search notice changes, audit-state insertion and upstream response categories across labeled sessions.

Two independent reviewers inspected the changes: a context-free blind reviewer and a separate protocol reviewer, using the inherited model. All eight findings and fixes are recorded in the [spec triage log](spec-dw-116-446-459-runnable-handoff.md). Corrections cover refusal accounting/null shapes, actual search interaction, localized keyboard instructions, timestamp alignment, Fluent identities, session labels and honest manual verdicts. Both reviewers independently reproduced seven refusal shapes and reported no remaining findings after correction. No review finding was deferred.

The [initial rejected selector attempt](evidence/dw-116-446-459-runnable-handoff/rejected-selector-attempt.json) is retained separately. It passed S0 and then failed because its test used a nonexistent detail-link selector. The final check uses the real `tenants-list-detail-link`. It is not passing evidence.

Reproduce the browser checks from the repository root after the documented npm/browser prerequisites:

```bash
TENANTS_BROWSER_HANDOFF=check CHROMIUM_BIN=/opt/google/chrome/chrome node tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-administrator-dialogs.mjs /tmp/tenants-handoff-chromium-final.json
TENANTS_BROWSER_HANDOFF=check TENANTS_BROWSER_ENGINE=firefox node tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-administrator-dialogs.mjs /tmp/tenants-handoff-firefox.json
TENANTS_BROWSER_STANDALONE_OIDC=1 TENANTS_BROWSER_READ_SAFETY=1 CHROMIUM_BIN=/opt/google/chrome/chrome node tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-administrator-dialogs.mjs /tmp/tenants-handoff-existing-read-safety-final.json
node _bmad-output/implementation-artifacts/evidence/dw-116-446-459-runnable-handoff/refusal-reproduction.mjs
```

The documented native Windows commands use installed Chrome via `TENANTS_BROWSER_CHANNEL=chrome` instead of this machine's executable path. Headed terminal smoke used `xvfb-run -a env TENANTS_BROWSER_HANDOFF=manual CHROMIUM_BIN=/opt/google/chrome/chrome node ... /tmp/tenants-handoff-manual-final.json`; the packet records both fresh cultures and entered control modes. The baseline Aspire topology started successfully, all described resources were Running, and the AppHost was stopped successfully after validation. Standalone handoff mode itself needs no Aspire/Dapr/Docker runtime.

No native Windows/NVDA or VoiceOver speech was collected. Every handoff packet has closure eligibility disabled. The existing EN/FR, two-supported-pairing speech matrix, actual transcript, omissions/duplicates, timing/focus evidence and independent speech acceptance remain required; none of DW-116/446/459 closes on these browser results. Backend authorization, persistence, real command delivery and full-suite/release qualification are outside this packet.
