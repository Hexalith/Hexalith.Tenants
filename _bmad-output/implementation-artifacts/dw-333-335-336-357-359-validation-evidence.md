# Authenticated workspace and grant focus — 2026-10-23

This batch closes DW-333, DW-335, DW-336, DW-357 and DW-359 against the actual local Aspire-hosted Blazor application. The source baseline is `c1c3ca672d481aaa52eff6e20b37ec9a77b24f12`; the browser result records the UI source diff, validator and on-disk Debug assembly hashes. FrontComposer is pinned at `9b8cbf28c621cbf45429eab8db1a50cfc4bf1c38`, EventStore at `f4df781cb7247c6a5f7549106b90d3748151edba`, and Builds at `5e54e892e69c31f346d69cb79f19f04bce8b79cc`. No dependency pointer changes are part of this batch.

The browser initially exposed a real production defect: a Razor comment inside the grant `FluentTextInput` attribute list became an invalid attribute name. Chromium rejected the render batch and Blazor terminated the circuit before viewport measurement. Moving the comment above the component fixes the render without changing identity validation or its intentionally absent `maxlength`. The existing EN/FR rendered-input tests now assert that comment attributes are absent and identity input remains untruncated.

## Acceptance

The retained browser JSON contains nine passing cases in Chromium. Both real workspace panel contents remain mounted after activation; each inactive subtree has no visible box or exposed accessibility node and is skipped through 30 native Tab and 30 Shift+Tab steps. Both retained directions are exercised twice.

Grant preview uses the actual authenticated page, current singleton administrator projection, production JavaScript and live Blazor callbacks. At measured widths 768, 1024 and 1366, the test checks native keyboard cycles, an inert and accessibility-hidden background that rejects focus, Escape cancellation and launcher restoration. At 767, a live resize disables confirmation while keeping Cancel available, then disables new grant initiation. At 390 the CSS hides mutation initiation. The grant lifecycle remains Idle; the validator never invokes Confirm or dispatches a grant.

The current Fluent V5 Cancel control has a shadow root with no independently focusable inner control. Actual focus and Escape start on its custom host. The result records deepest focus and shadow structure explicitly; the validator selects and requires inner shadow focus if a future shipped control provides it. This qualifies current Fluent behavior without claiming a fabricated FAST shadow target or screen-reader acceptance.

## Reproduction and results

Start the existing AppHost with known credentials supplied through the documented `Parameters__security-eventstore-client-username` and `Parameters__security-eventstore-client-password` parameters in a fresh non-persistent local realm. Keep chosen values out of tracked configuration and transcripts. Discover the actual UI HTTPS endpoint with `aspire describe --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json`. Supply that endpoint and the same chosen credentials as `TENANTS_BROWSER_BASE_URL`, `TENANTS_BROWSER_USERNAME` and `TENANTS_BROWSER_PASSWORD`.

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -p:UseNuGetDeps=false -m:1 -v:q
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*GlobalAdministratorsPageTests' -class '*TenantsWorkspaceTests' -result-xml /tmp/tenants-five-focused.xml
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -result-xml /tmp/tenants-five-ui-full.xml
node tests/Hexalith.Tenants.UI.Tests/Browser/validate-authenticated-workspace-focus.mjs /tmp/tenants-five-authenticated-focus.json
```

Build: zero warnings/errors. Focused: **442/442**, 29.042 seconds. Full UI: **7,496/7,496**, 116.685 seconds. No errors, failures, skips or not-run tests. Assembly summaries and private raw XML hashes are retained in `dw-333-335-336-357-359-test-results.json`; successful browser measurements are in `dw-333-335-336-357-359-browser-results.json`.

The validator reuses the existing performance lane's Playwright installation (`npm ci --prefix tests/performance/tenant-audit` when needed). This run instead used the already-installed Playwright module through `TENANTS_BROWSER_PLAYWRIGHT_MODULE` and system Chromium through `CHROMIUM_BIN`; no dependency or lockfile update was needed. It uses a temporary browser context and retains no session state, cookies, tokens, credential values, network bodies or screenshots.

Initial fixture development revealed a pre-hydration tab race, the need to await asynchronous Blazor sentinel callbacks, and the distinction between the Phone safety tier and the narrower CSS hiding breakpoint. The final assertions wait for actual retained content and measured restrictions, and use the Fluent host's `disabled` attribute. A stale `--no-build` UI process was restarted after the Debug build before accepted browser qualification.

Aspire resources reached healthy state. Startup bootstrap returned HTTP 400; the browser independently verified the existing current administrator projection and authenticated authority before previewing. No bootstrap retry or stored-state repair was performed. This evidence covers current authenticated local rendering and keyboard behavior; it does not qualify production bootstrap, command submission, release readiness or assistive-technology announcements.

## Repository checks

`python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-333-335-336-357-359-authenticated-focus.md` passes with no root dependency pointer movement. `node --check` and `git diff --check` pass. A fetch of `origin main` reports zero commits of divergence before commit.

The exact full candidate in `/tmp/tenants-five-commit-message.txt` passed `node_modules/.bin/commitlint --edit /tmp/tenants-five-commit-message.txt --verbose`: **0 problems, 0 warnings**. Both the lockfile and installed CLI are version **21.2.3**.

The validator hashes the local Debug assembly; it does not attest that an arbitrary endpoint loaded those bytes. For this run Aspire launched the owning local UI project in Debug with `--no-build`, and the UI resource was explicitly restarted after the accepted build. The endpoint/process relationship was inspected separately.

## Review corrections and retained failures

Independent Blind Hunter review returned seven findings; all seven are triaged in the spec and corrected in the validator or records. The resize check now proves enabled-to-disabled confirmation; the inert check has a genuinely focusable positive control; disabled traversal rejects Confirm; control-level keyboard observations are retained. The maintenance index exactly covers 92 open IDs with 85 grouped rows.

The earlier five-second launcher-focus timeout remains in `dw-333-335-336-357-359-initial-focus-failure.json`. Focus polling now uses the same twenty-second deadline as other browser operations, allowing the application's ten-second optional circuit-setup budget, and records restoration durations and failure state. The precise cause of that earlier observation was not captured. Separately, `dw-333-335-336-357-359-auth-unavailable.json` records the later failed sign-in fixture; Aspire then showed Keycloak Exited with code 137. The existing security resource was restarted through Aspire before reviewed acceptance. Neither failed run is accepted as a passing result.

Reviewed runtime acceptance passes **9/9** scenarios with zero circuit errors. The accepted JSON records 108 native grant-dialog key observations, both disabled and enabled Confirm cycles, a genuinely focusable background positive control, and enabled-to-disabled confirmation on resize. Escape restoration took **30/33/29 milliseconds** at 1366/768/1024 pixels. Current startup network activity is awaited before clicking prerendered tab controls; all panel content/visibility assertions remain mandatory. Playwright's exact installed version and the final validator hash are retained. The earlier focus timeout's cause remains unproven and its failed result remains explicitly excluded from acceptance.

The security-resource restart could not recreate the externally missing container; restarting the owning AppHost through Aspire restored healthy services without configuration changes or stored-state edits. AppHost logs identify a missing container rather than establishing a JVM OOM cause.

Final independent recheck confirms the accepted validator hash, nine passing scenarios, zero circuit errors, 108 observations and all seven corrections. No outstanding finding or newly deferred work remains.
