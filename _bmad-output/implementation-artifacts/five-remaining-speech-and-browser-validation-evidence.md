# Five remaining speech and browser follow-ups — 2026-10-30

The [approved spec](spec-five-remaining-speech-and-browser-followups.md) was attempted against Tenants `bd1d6ead1243237b5e84d1c1403485ee41fa9af3`. **All five follow-ups remain open. Native qualification is 0/48 windows, with zero independently accepted cells.** Neither browser investigation reached a browser. No production or capture-helper change is supported by this run.

The fresh pinned Debug/source UI-test build and the final native builds fail with **two CS0246 errors** for `RouteOperationContract` at `src/Hexalith.Tenants.Contracts/Routing/TenantCatalogDeclarations.cs:14` and `:20`, with zero warnings. The default administrator runners stop earlier at the existing **EventStore 3.128.3 below 3.128.4 build guard** in the McpCli source projects. These are distinct observed blockers; neither is a runtime reproduction.

## Independent outcomes

| Follow-up | Current result |
| --- | --- |
| DW-116 | All EN/FR S0–S3 cells on both declared pairings are blocked before capture. No new search speech, empty-entry observation or control qualifies. |
| DW-446 | All first Pending/Unavailable and lifecycle/shared Refresh observations remain unqualified. No new audit/lifecycle speech exists. |
| DW-459 | Shares DW-446's required evidence, while retaining its separate original finding and open record. |
| Administrator-grant Escape | The standalone OIDC/runtime-followups/safety-only command fails its fresh build before foreground focus, the held-subscription negative control, callback acknowledgment or distinct rendered-row observation. The historical body-focus failure remains unexplained. |
| Fluent grid disposal | The separate HOLD_GRID command fails its fresh build before connected initialization, detached release or the circuit assertion. The historical Fluent 5.0.1 external defect and owning-component blocker remain open; no new negative or positive reproduction is claimed. |

Previous [native packets](five-deferred-native-speech-validation-evidence.md) and [browser findings](five-deferred-runtime-followups-validation-evidence.md) retain their original historical scope. No before/after equality attestation for those packets was retained. Current control passes cannot explain an earlier failure, and this run supplies no replacement runtime acceptance.

## Native execution and preservation

A new disposable native Windows checkout recorded the root HEAD and nine root-declared dependency HEAD revisions using local-only Git operations. No nested dependency was initialized. Six dependencies used sparse source selections, corrected once to include `eng`. [Checkout identity and attribution limits](evidence/five-remaining-speech-and-browser/native-checkout-identity.json) distinguish recorded revisions from helper-byte comparisons and the scoped root/FrontComposer fingerprints in the fixture packets. No contemporaneous worktree fingerprints for every consumed dependency were retained; revision equality does not prove their consumed contents. No historical fingerprints have been recaptured.

The private prerequisite log reports six installed packages and no vulnerabilities; a separate locked-install success claim is withdrawn because Node/npm versions, a contemporaneous lockfile hash and the individual npm exit code were not retained. [Sanitized prerequisite/preparation evidence](evidence/five-remaining-speech-and-browser/native-preparation-evidence.json) preserves the available output and labels the reconstructed command, cwd and corrective `git sparse-checkout set --cone src Props eng` commands. The compiler paths identify SDK 10.0.403. Windows reported `10.0.26300.0`; the owned NVDA executable reported 2026.2. Chrome and Firefox never launched, so no tested browser build/version is asserted. The private observer recorded OneCore/Hortense `fr_FR`, rate 50, talk mode and effective settings in the build-failed packets. No product mode observation or positive/cancellation control ran. Every attempt reports the original NVDA settings hash unchanged. No user add-ons were copied.

| Attempt | Actual boundary and retained result |
| --- | --- |
| Chrome 1 | WSL-launched PowerShell resolved an empty fixture root. Owned Speech Viewer bound; Node/build/authentication never started. Effective settings were not observed before failure. |
| Chrome 2 | Explicit native working directory/root corrected invocation setup. Fresh build failed with MSB4019 because sparse dependency preparation omitted FrontComposer's `eng/FrontComposer.SourceReferences.targets`. |
| Firefox 1 | Same missing-`eng` build failure; no product capture. |
| Chrome 3 | After populating the tracked `eng` support once, fresh build reached the two integrated CS0246 errors. |
| Firefox 2 | Same final integrated CS0246 failure. No further runtime retry was performed. |

The [attempt index](evidence/five-remaining-speech-and-browser/native-attempt-index.json) binds all five failed packets and raw/retained hashes. The [complete 48-cell matrix](evidence/five-remaining-speech-and-browser/native-matrix.json) marks every window blocked and every observation/control/acceptance false. No trial started product capture; the retained speech arrays and timelines are empty. There is no queued product speech to assess and no evidence of audible completion. Empty transcripts from a build failure are not measured announcement omissions.

The native launch for each final pairing used a fresh output directory and an explicit native working directory:

```powershell
Set-Location -LiteralPath 'C:\Temp\tenants-five-remaining-20261030'
& '.\tests\Hexalith.Tenants.UI.Tests\Browser\capture-deferred-nvda-speech.ps1' `
  -FixtureRoot 'C:\Temp\tenants-five-remaining-20261030' `
  -OutputDirectory 'C:\Temp\tenants-five-remaining-chrome-attempt-3' -BrowserEngine chromium
# Separate subsequent invocation: firefox, output tenants-five-remaining-firefox-attempt-2.
```

Raw startup/build logs remain private; sanitized diagnostics and their exact byte hashes are in [build diagnostics](evidence/five-remaining-speech-and-browser/build-diagnostics.json). Only private user-profile log-directory paths were removed from retained collector metadata; original failure/completion flags are preserved. No authentication, credentials, network bodies or unrelated desktop speech enter the retained packets. The historical Windows checkout received no write command; bounded read-only status inspection timed out, so no complete before/after byte attestation is claimed. [Preservation limits](evidence/five-remaining-speech-and-browser/historical-checkout-preservation.json) retain that distinction.

## Commands and validation

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj \
  -c Debug -m:1 -p:UseNuGetDeps=false -p:HexalithEventStoreVersion=3.128.4 \
  -p:HexalithMcpCliFromSource=false -p:NuGetAudit=false
```

This exits 1 with the two CS0246 errors. Current-source focused MTP classes and the complete UI project could not run. Earlier assemblies were not substituted for current-source evidence.

```bash
TENANTS_BROWSER_STANDALONE_OIDC=1 TENANTS_BROWSER_RUNTIME_FOLLOWUPS=1 \
TENANTS_BROWSER_UI_SAFETY_ONLY=1 CHROMIUM_BIN=/usr/bin/google-chrome \
node tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-administrator-dialogs.mjs \
  /tmp/five-remaining-private-vz92we5e/grant-browser.json

TENANTS_BROWSER_STANDALONE_OIDC=1 TENANTS_BROWSER_HOLD_GRID=1 \
TENANTS_BROWSER_UI_SAFETY_ONLY=1 CHROMIUM_BIN=/usr/bin/google-chrome \
node tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-administrator-dialogs.mjs \
  /tmp/five-remaining-private-vz92we5e/grid-browser.json
```

The original packets omit scenario flags and use identical legacy record labels. Their [hash-bound invocation sidecar](evidence/five-remaining-speech-and-browser/browser-invocations.json) preserves the explicit grant/grid flag selections reconstructed from the tool invocations without changing those packets. Both exit 1 in `build-current-ui`, with zero automated cases and closure acceptance false. Their [grant packet](evidence/five-remaining-speech-and-browser/grant-browser-build-blocked.json) and [separate grid packet](evidence/five-remaining-speech-and-browser/grid-browser-build-blocked.json) preserve unchanged fresh-build selection and source equality through cleanup. Zero-circuit-error acceptance was not reached or weakened.

`aspire start` was invoked for the named AppHost through the local Aspire skill and its resource states were inspected. The retained metadata does not establish a restart or a before/after AppHost process identity. Initial dependent resources were waiting; a later observation reported healthy security, EventStore and Tenants UI resources. [Sanitized resource state](evidence/five-remaining-speech-and-browser/aspire-resource-state.json) contains no endpoint credentials, environment values or dashboard token. Resource health is separate from the failed fresh source builds.

Focused fallback verification passed: six Node native-driver regressions, Node syntax checks for the existing native/browser/handoff modules, PowerShell collector parsing, and Python observer syntax. [Validation results](evidence/five-remaining-speech-and-browser/validation-results.json) record exact commands and limits. No helper or production behavior changed.

The ledger appends outcomes only to the selected three numbered and two unnumbered records. All **657 IDs, 654 done/3 open statuses, 654 unselected numbered bodies and every original selected field** survive. The archive bytes are unchanged because no record closes. The [entry snapshot and hashes](evidence/five-remaining-speech-and-browser/entry-ledger-manifest.json), executable [conservation validator](evidence/five-remaining-speech-and-browser/validate-conservation.py) and [conservation result](evidence/five-remaining-speech-and-browser/conservation.json) bind this run's entry state. No historical packet equality attestation is claimed; unrelated release holds remain open.

Replay evidence checks with `python3 -B _bmad-output/implementation-artifacts/evidence/five-remaining-speech-and-browser/validate-artifacts.py` and conservation controls with `python3 -B _bmad-output/implementation-artifacts/evidence/five-remaining-speech-and-browser/test-conservation.py`. The checker verifies complete manifest coverage/hashes, source/packet links and the 48-cell matrix. It does not verify private raw logs or all consumed dependency contents. The retained collector helper contains the finally-path owned-process termination requests. Packets do not retain a timestamped per-PID termination acknowledgement or post-termination enumeration; no zero-process assertion is made.

Independent parent review, exact pinned commitlint validation, commit, divergence inspection and authorized publication remain pending with the parent workflow. This evidence handoff does not complete the blocked runtime tasks or authorize a dependency update, shim, grid workaround or production announcement redesign.

## Parent verification and publication

Three independent review layers completed. All twelve findings, including the Windows path correction exposed during recheck, are resolved and rechecked by their original reviewers. Parent reruns pass raw-byte conservation, ten conservation tamper controls, complete artifact replay, four artifact tamper controls, the story gitlink guard and whitespace checks. Targeted artifact validation and tamper checks also pass on native Windows Python 3.14. No further product runtime was attempted.

[Parent validation](evidence/five-remaining-speech-and-browser/parent-validation.json) retains commands/results and the exact message/log hashes. [Pinned commitlint evidence](evidence/five-remaining-speech-and-browser/commitlint.txt) records version 21.2.3, zero problems and zero warnings. Main was fetched without submodule recursion and showed no divergence before commit. The evidence update is being committed and pushed under the user’s explicit authorization; the resulting canonical revision is recorded by Git. All five follow-ups and both runtime tasks remain open.

The final publication check found that Git would normalize ten retained CRLF speech/timeline JSON files. A scoped evidence `.gitattributes` preserves only those exact packet paths as raw bytes. Their original hashes remain unchanged; Git clean conversion and final staged blobs are checked against retained bytes before commit.
