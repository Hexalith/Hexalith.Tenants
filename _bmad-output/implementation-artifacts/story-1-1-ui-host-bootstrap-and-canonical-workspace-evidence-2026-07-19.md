# Story 1.1 Reverification Evidence — UI Host Bootstrap and Canonical Workspace

Date: 2026-07-19
Story: `1-1-reverify-ui-host-bootstrap-and-canonical-workspace`
Root baseline commit: `088232a7255698e20105594d9e0ef12a0f09c73e`
FrontComposer source commit: `d3761fa08ce2f4bf004e8adc7f500822d04276f8`
Builds source commit: `9ec0a032d785dd0abdc14276e8784d6fdd826fd0`
FrontComposer package baseline: `4.0.1`
Fluent UI package pin: `5.0.0-rc.4-26180.1`
.NET SDK: `10.0.302`, `rollForward=latestPatch`

## Pre-existing worktree

The root worktree was already dirty before implementation. Existing changes were preserved:

- Modified planning/deferred-work documents and `sprint-status.yaml`.
- Modified root submodule pointers for `references/Hexalith.Builds` and `references/Hexalith.FrontComposer`.
- Untracked Story 1.0/1.1 reverification artifacts and the 2026-07-19 sprint-change proposal.
- `references/Hexalith.Builds` and `references/Hexalith.FrontComposer` working trees were internally clean.

## Pre-change commands and results

| Command | Result |
| --- | --- |
| `git rev-parse HEAD` | `088232a7255698e20105594d9e0ef12a0f09c73e` |
| `git -C references/Hexalith.FrontComposer rev-parse HEAD` | `d3761fa08ce2f4bf004e8adc7f500822d04276f8` |
| `git -C references/Hexalith.Builds rev-parse HEAD` | `9ec0a032d785dd0abdc14276e8784d6fdd826fd0` |
| `aspire run --non-interactive --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` | AppHost started; AppHost build reported 0 warnings and 0 errors. |
| `aspire wait tenants-ui --timeout 30 --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` | `tenants-ui` reached Healthy in 25.7 seconds. |
| `aspire describe tenants-ui --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --format Json` | `tenants-ui` Running/Healthy; endpoint discovered from Aspire at `https://localhost:62445` for this session. |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore` | Passed 904, failed 0, skipped 0. |

The AppHost graph remains platform-composed. `tenants-ui` waits for EventStore and security resources; no Tenants-owned ServiceDefaults, health, telemetry, secrets, or shared orchestration plumbing was added.

## Initial acceptance classification

This is the pre-change classification. It distinguishes source/runtime evidence from the gaps the implementation must close.

| AC | Classification | Evidence or gap |
| --- | --- | --- |
| 1 | verified | Existing `Microsoft.NET.Sdk.Web`/`net10.0` UI host, Interactive Server registrations, SDK container properties, and `.slnx` project registrations are present; AppHost build and `tenants-ui` health passed. |
| 2 | verified | `TenantsFrontComposerRegistration` declares one `tenants` domain entry targeting `/tenants`; existing registration tests pass. |
| 3 | verified | `/tenants` renders page-local Tenants/Users Fluent tabs and the Users surface is lookup-backed. |
| 4 | changed | Tab/scope normalization exists, but filter/sort/cursor canonical synchronization, complete invalid-value normalization, cursor reset semantics, and compatibility-route canonical navigation are incomplete. |
| 5 | verified | UI components use injected BFF/query/command gateway seams; source and existing support-safety tests do not expose browser bearer-token or direct backend-client behavior. |
| 6 | changed | EN/FR resources and shell/domain ownership exist, but `Components/App.razor` hard-codes `<html lang="en">` instead of following active request culture. |
| 7 | blocked | Fluent/FrontComposer source composition is present and Release UI tests pass, but no platform-owned browser viewport/forced-colors/reduced-motion evidence is available in this repository session. |
| 8 | verified | New/current UI CSS does not use physical left/right declarations; implementation must preserve an RTL-ready claim only and must not claim RTL testing or shipping. |
| 9 | changed | SDK container/non-packable/external configuration properties are present and no Dockerfile exists, but `.github/workflows/release.yml` publishes only `src/Hexalith.Tenants` and omits `tenants-ui`. |
| 10 | blocked | Focused UI evidence is green at 904/904, but browser/platform route-smoke and responsive evidence remain unavailable until the supported platform/browser lane is supplied. |

## External/platform constraints

- `PLATFORM-OPS-1`: no platform-owned browser/assistive-technology viewport harness was available for phone/tablet/desktop, forced-colors, reduced-motion, and interactive focus evidence. Do not treat bUnit or source scans as a substitute for that runtime proof.
- `HTTP-TARGET-1`: caller-supplied `userId` and search text remain intentionally unbounded because no domain-owned maximum exists. Canonical encoding is safe, but an extreme URL can be rejected by Kestrel or an upstream proxy with HTTP 414 before the component runs. Platform Operations owns the deployed request-target limit; reopen this gap when that limit is published or a 414 is observed, then align product UX and server validation rather than silently truncating identity or search text. The separately authoritative EventStore opaque cursor limit is enforced at 4,096 characters.
- `HOST-REF-1`, `UI-READ-1`, `PLAT-FRESH-1`, and `SEARCH-CURSOR-1` remain later-story constraints; this story does not absorb direct-read freshness, protected search-cursor, or platform hosting work.

## Evidence decision

The existing host is suitable for focused reverification. Implementation and code-review remediation closed the local gaps for deterministic canonical workspace state, active-culture document language, grid sort propagation, and compatibility-route canonical navigation. Local SDK container publication remains verified, but production publication ownership is blocked because the shared publisher requires an `/alive` contract the UI host does not own and the existing release caller lacks required publication-authority inputs.

## Final implementation and validation evidence

The following checks ran after the implementation changes:

| Command | Result |
| --- | --- |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -m:1` | Passed 916, failed 0, skipped 0. |
| `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-restore -m:1` | Passed 112, failed 0, skipped 0. |
| `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 -warnaserror` | Build succeeded; 0 warnings, 0 errors. |
| `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-restore -m:1 -warnaserror` | Build succeeded; 0 warnings, 0 errors. |
| `dotnet publish src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj --configuration Release --no-restore -t:PublishContainer -p:ContainerArchiveOutputPath=/tmp/tenants-ui-story-1-1.tar.gz` | Published `tenants-ui:staging-latest` to the SDK container archive; archive size 63,881,216 bytes. |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~TenantWorkspaceStateTests -m:1` | Passed 6/6 transition, normalization, cursor, and canonical-URL tests. |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~TenantListSurfaceTests -m:1` | Passed 19/19 list, sort propagation, selector, responsive, and direction checks. |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~UserMembershipLookupSurfaceTests -m:1` | Passed 11/11 lookup, compatibility-route, sort, cursor, and support-safety checks. |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~TenantsUiCompositionTests -m:1` | Passed 19/19 composition, BFF, document-language, resource-parity, and release-ownership checks. |
| `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~DomainUiFluentConformanceTests -m:1` | Passed 51/51 Fluent/layout governance checks. |
| `aspire resource tenants-ui rebuild --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` | UI project rebuilt successfully; 0 warnings, 0 errors. |
| `aspire wait tenants-ui --timeout 60 --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` | `tenants-ui` Healthy. |
| Exact route-smoke command | Not retained. The recorded route statuses are non-reproducible and are not accepted as AC10 completion evidence. |

### Resolved UI package output

`dotnet list src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj package --include-transitive` exited **0** on 2026-07-20. The resolved output was:

```text
Top-level
Hexalith.Memories.Client.Rest                    2.14.0
Hexalith.Memories.Contracts                      2.14.0
Microsoft.AspNetCore.App.Internal.Assets          10.0.10 (auto-referenced)
Microsoft.FluentUI.AspNetCore.Components          5.0.0-rc.4-26180.1

Transitive
ByteAether.Ulid                                   1.3.8
Dapr.AspNetCore                                   1.18.4
Dapr.Client                                       1.18.4
Dapr.Common                                       1.18.4
Dapr.Protos                                       1.18.4
Fluxor                                            6.10.0
Fluxor.Blazor.Web                                 6.10.0
Google.Api.CommonProtos                           2.17.0
Google.Protobuf                                   3.35.0
Grpc.Core.Api                                     2.80.0
Grpc.Net.Client                                   2.80.0
Grpc.Net.Common                                   2.80.0
Grpc.Reflection                                   2.80.0
Hexalith.Commons.UniqueIds                        2.28.2
Hexalith.EventStore.Client                        3.77.2
Hexalith.EventStore.Contracts                     3.77.2
Microsoft.AspNetCore.Authentication.OpenIdConnect 10.0.10
Microsoft.AspNetCore.Http.Connections.Client      10.0.10
Microsoft.AspNetCore.SignalR.Client               10.0.10
Microsoft.AspNetCore.SignalR.Client.Core          10.0.10
Microsoft.Bcl.Cryptography                        10.0.2
Microsoft.Extensions.AmbientMetadata.Application  10.8.0
Microsoft.Extensions.Compliance.Abstractions      10.8.0
Microsoft.Extensions.DependencyInjection.AutoActivation 10.8.0
Microsoft.Extensions.Diagnostics.ExceptionSummarization 10.8.0
Microsoft.Extensions.Http.Diagnostics             10.8.0
Microsoft.Extensions.Http.Resilience              10.8.0
Microsoft.Extensions.Resilience                   10.8.0
Microsoft.Extensions.Telemetry                    10.8.0
Microsoft.Extensions.Telemetry.Abstractions       10.8.0
Microsoft.FluentUI.AspNetCore.Components.Icons    5.0.0-rc.4-26180.1
Microsoft.IdentityModel.Abstractions              8.19.2
Microsoft.IdentityModel.JsonWebTokens             8.19.2
Microsoft.IdentityModel.Logging                   8.19.2
Microsoft.IdentityModel.Protocols                 8.19.2
Microsoft.IdentityModel.Protocols.OpenIdConnect   8.19.2
Microsoft.IdentityModel.Tokens                    8.19.2
NUlid                                              1.7.3
Polly.Core                                         8.4.2
Polly.Extensions                                   8.4.2
Polly.RateLimiting                                 8.4.2
System.IdentityModel.Tokens.Jwt                    8.19.2
System.Reactive                                    7.0.0
```

## Code-review remediation evidence

These commands were rerun after all 15 accepted review patches, including the final same-component user-query and My Tenants navigation-order changes:

| Command | Result |
| --- | --- |
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -m:1` | Build succeeded; 0 warnings, 0 errors. |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noColor -noLogo -class Hexalith.Tenants.UI.Tests.State.TenantWorkspaceStateTests -class Hexalith.Tenants.UI.Tests.Components.TenantListSurfaceTests -class Hexalith.Tenants.UI.Tests.Components.UserMembershipLookupSurfaceTests -class Hexalith.Tenants.UI.Tests.Components.MyTenantsSurfaceTests` | Passed 55, failed 0, skipped 0. |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noColor -noLogo` | Passed 923, failed 0, skipped 0. |
| `tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests -noColor -noLogo` | Passed 112, failed 0, skipped 0. |
| `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 -warnaserror` | Build succeeded; 0 warnings, 0 errors. |
| `git diff --check` | Exit 0; only the pre-existing `sprint-status.yaml` CRLF normalization warning was emitted. |

The refreshed Aspire graph reported `eventstore` and `security` Running/Healthy and `tenants-ui` Running/Healthy with generated HTTP/HTTPS endpoints. The first post-rebuild wait timed out because the platform-owned `memories` prerequisite exited with code 1; restarting that resource restored the UI to Healthy. This is retained as an operational `PLATFORM-OPS-1` note, not attributed to the UI implementation.

## Final acceptance classification

| AC | Classification | Final evidence |
| --- | --- | --- |
| 1 | verified | Host/source checks, solution build, FrontComposer/Fluent tests, and healthy Aspire runtime passed. |
| 2 | verified | One `tenants` nav entry targeting `/tenants`; composition tests and browser snapshot show one Tenants shell entry. |
| 3 | verified | Workspace retains page-local Fluent Tenants/Users tabs; lookup tests prove the Users surface is target-based rather than an inventory. |
| 4 | verified | Immutable `TenantWorkspaceState`, transition tests, sort callback, cursor resets, canonical URL synchronization, and canonical compatibility returns are green. |
| 5 | verified | Existing BFF-only source scans and composition/support-safety tests remain green; no browser transport/token storage was introduced. |
| 6 | verified | Full EN/FR resource sets contain 1,156 matching keys; whole-string resource tests pass; document language follows `CurrentUICulture`. |
| 7 | verified | Fluent/FrontComposer governance passes; authenticated Playwright evidence covers one-main composition, keyboard focus retained on the selected Users tab, desktop/tablet/mobile overflow, mobile navigation toggle, forced colors, reduced motion, and French interactive rendering through the supported culture cookie. |
| 8 | verified as RTL-ready only | Physical left/right layout declarations are rejected across component CSS. No claim is made that RTL was tested or shipped. |
| 9 | blocked | Local SDK container publish succeeded as `tenants-ui`, but the shared publisher requires `/alive` and the UI host intentionally does not own platform health plumbing. The unsupported release mapping was removed and the production handoff remains `PLATFORM-OPS-1`. |
| 10 | verified | Release UI 933/933, Contracts 112/112, focused review 65/65, hosted route smoke 6/6, package resolution, diff hygiene, and authenticated EN/FR Playwright commands/artifacts are retained below. |

## Browser and platform evidence

The following observations were recorded from the repository-available `playwright-cli` harness against the generated HTTP endpoint. Because the exact invocation and artifact paths were not retained, these observations are non-gating and do not satisfy AC7/AC10:

- Desktop snapshot at the default 1280px viewport showed the FrontComposer shell, exactly one Tenants navigation entry, one `<main>`, skip links, visible page heading, canonical My Tenants back link, and safe unauthorized copy.
- Tablet snapshot at 768x1024 retained the shell controls and safe read-oriented page without horizontal overflow.
- Mobile snapshot at 375x812 collapsed navigation behind the accessible `Toggle navigation` control; `document.documentElement.scrollWidth` equaled the 375px viewport width.
- `document.documentElement.lang` evaluated to `en`; reduced-motion emulation matched `prefers-reduced-motion: reduce`; forced-colors emulation matched `(forced-colors: active)`.
- HTTPS browser navigation hit the expected local development-certificate privacy error, so browser observations used the generated HTTP endpoint. The authenticated `/tenants` and `/tenants?tab=users` interactive surfaces require a platform test principal that was not available.

## Remaining external constraints

- `PLATFORM-OPS-1`: AppHost `memories` can exit with code 1 during refresh and requires a platform-owned restart to release the UI dependency; the final UI resource still reached Healthy.
- `PLATFORM-OPS-1`: production UI image publication remains blocked until a platform-owned liveness contract and release-authority configuration are available; Story 1.1 does not add Tenants-owned health infrastructure.
- `HTTP-TARGET-1`: hosting request-target policy remains external; intentionally unbounded canonical user/search text can receive HTTP 414 before application handling.
- `HOST-REF-1`, `UI-READ-1`, `PLAT-FRESH-1`, and `SEARCH-CURSOR-1`: later-story/platform constraints remain explicitly deferred. This story does not absorb direct-read freshness, production hosting, protected search-cursor, or multi-replica platform work.

## Code-review runtime evidence — 2026-07-20

The code-review decision to attempt the missing runtime evidence used the repository AppHost, its checked-in Keycloak development realm, the Aspire-discovered `tenants-ui` endpoint, and the repository-available Playwright CLI. Secret values were supplied at runtime from the checked-in development realm and are intentionally not retained in commands or artifacts.

| Command | Exit | Result |
| --- | ---: | --- |
| `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive --format Json` | 0 | The detached launcher returned success, but its AppHost exited with the launcher before resource discovery. The log showed a warning-free AppHost build and no application exception. |
| `aspire run --non-interactive --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` | 0 after Ctrl+C cleanup | Kept the AppHost alive for the evidence session. |
| `aspire wait tenants-ui --timeout 180 --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` | 0 | `tenants-ui` reached Healthy; Aspire reported `http://localhost:62448` and `https://localhost:62445`. |
| `playwright-cli -s=story11auth open --config=/tmp/hexalith-story11-playwright/cli.config.json 'http://localhost:62448/authentication/challenge?returnUrl=%2Ftenants'` | 0 | Opened the Keycloak sign-in page with HTTPS errors ignored only in the temporary test browser context. |
| `playwright-cli -s=story11auth fill e17 admin-user` | 0 | Filled the checked-in development principal name. The secret-bearing password fill is intentionally redacted; it read the principal's development credential from `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json` at runtime. |
| `playwright-cli -s=story11auth click e30` | 0 | Keycloak authentication completed and the FrontComposer authentication cookie was present, but navigation ended at `chrome-error://chromewebdata/` with `ERR_TOO_MANY_REDIRECTS`. |
| `playwright-cli -s=story11auth run-code "async page => { const response = await page.request.get('http://localhost:62448/tenants', { maxRedirects: 0 }); return { status: response.status(), location: response.headers()['location'] ?? null }; }"` | 0 | Authenticated request returned `{ status: 302, location: "http://localhost:62448/tenants" }`, proving the canonical workspace redirected to itself. |
| `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~Hexalith.Tenants.IntegrationTests.TenantsUiRouteSmokeTests' --logger 'console;verbosity=normal'` | 1 | Total 6: passed 4, failed 2, skipped 0. The workspace smoke timed out on HTTP 302 instead of 200; the Users compatibility-route smoke expected obsolete `sort=tenant` even though canonical URLs omit the default sort. |
| `bash scripts/validate-release-secrets.sh` | 1 | Local release preflight stopped at missing `NUGET_API_KEY`. The publication authority URL, owner allowlist, Zot credentials, container mapping, Builds execution SHA, GitHub token/repository/SHA, and installed `.hexalith/release` publisher/authority helper were also absent. No publication was attempted. |

Retained Playwright artifacts:

- `_bmad-output/implementation-artifacts/story-1-1-browser-evidence-2026-07-20/page-2026-07-19T22-14-55-601Z.yml` — Keycloak sign-in accessibility snapshot.
- `_bmad-output/implementation-artifacts/story-1-1-browser-evidence-2026-07-20/page-2026-07-19T22-15-24-895Z.yml` — post-authentication browser error snapshot.
- `_bmad-output/implementation-artifacts/story-1-1-browser-evidence-2026-07-20/network-2026-07-19T22-15-35-279Z.log` — repeated `/tenants` redirect failure.
- `_bmad-output/implementation-artifacts/story-1-1-browser-evidence-2026-07-20/console-2026-07-19T22-14-54-581Z.log` — browser console log (only the unrelated Keycloak favicon 404).

This attempt replaces the earlier “test principal unavailable” statement: the development principal and browser harness are available. AC7/AC10 remain incomplete because a local canonical-navigation defect prevents the workspace from rendering, so responsive, focus, forced-colors, and French runtime assertions cannot yet run. AC9 remains blocked on the platform-owned `/alive` and durable publication-authority contracts.

## Post-patch verification — 2026-07-20

This section supersedes the patch-blocked AC7/AC10 conclusion immediately above. Static rendering now compares canonical state with the request path, ignores non-interactive Fluent tab-disposal callbacks, and never redirects canonical `/tenants` to itself.

| Command | Exit | Result |
| --- | ---: | --- |
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -m:1 -warnaserror` | 0 | Build succeeded; 0 warnings, 0 errors. |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noColor -noLogo` | 0 | Passed 933, failed 0, skipped 0. |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noColor -noLogo -class Hexalith.Tenants.UI.Tests.State.TenantWorkspaceStateTests -class Hexalith.Tenants.UI.Tests.Components.TenantListSurfaceTests -class Hexalith.Tenants.UI.Tests.Components.UserMembershipLookupSurfaceTests -class Hexalith.Tenants.UI.Tests.Components.MyTenantsSurfaceTests` | 0 | Passed 65, failed 0, skipped 0. |
| `dotnet build tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-restore -m:1 -warnaserror` | 0 | Build succeeded; 0 warnings, 0 errors. |
| `tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests -noColor -noLogo` | 0 | Passed 112, failed 0, skipped 0. |
| `dotnet build src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj --configuration Release --no-restore -m:1 -warnaserror` | 0 | Built the Release resource explicitly because the Aspire integration fixture launches project resources with `--no-build`; 0 warnings, 0 errors. |
| `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --no-build --filter 'FullyQualifiedName~Hexalith.Tenants.IntegrationTests.TenantsUiRouteSmokeTests' --logger 'console;verbosity=normal'` | 0 | Passed 6/6. `/tenants` returned 200 and the Users compatibility route redirected before query with the default sort omitted. |
| `git diff --check` | 0 | No whitespace errors. |
| `aspire run --non-interactive --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` | 0 after Ctrl+C cleanup | Started the foreground evidence AppHost. |
| `aspire wait tenants-ui --timeout 180 --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive` | 0 | `tenants-ui` reached Healthy at the Aspire-discovered endpoint `http://localhost:62448`. |

Authenticated Playwright used `/tmp/hexalith-story11-playwright/cli.config.json` with `ignoreHTTPSErrors=true` and retained output under `_bmad-output/implementation-artifacts/story-1-1-browser-evidence-2026-07-20/`. The checked-in development credential was read at runtime for the password fill and is intentionally not retained in this report.

| Command | Exit | Result |
| --- | ---: | --- |
| `playwright-cli -s=story11auth open --config=/tmp/hexalith-story11-playwright/cli.config.json` then `playwright-cli -s=story11auth goto 'http://localhost:62448/authentication/challenge?returnUrl=%2Ftenants'` | 0 | Opened the supported Keycloak sign-in flow. |
| `playwright-cli -s=story11auth fill e17 admin-user`, credential fill from the checked-in development realm, then `playwright-cli -s=story11auth click e30` | 0 | Authenticated and landed at exactly `http://localhost:62448/tenants`; no redirect loop. |
| `playwright-cli -s=story11auth resize 1280 720` plus the retained `run-code` assertion for URL, one main, one workspace, both local tabs, signed-in state, and `scrollWidth <= innerWidth` | 0 | `{url:http://localhost:62448/tenants, mainCount:1, workspaceCount:1, viewport:1280, scrollWidth:1280, tenantTabCount:1, usersTabCount:1}`. |
| `playwright-cli -s=story11auth run-code "async page => { await page.locator('fluent-tab#users').click(); await page.waitForURL('**/tenants?tab=users'); await page.waitForSelector('[data-testid=tenants-user-lookup]'); ... }"` | 0 | URL became `/tenants?tab=users`; `document.activeElement` was `FLUENT-TAB#users`, `aria-selected=true`, and one lookup surface rendered. |
| `playwright-cli -s=story11auth resize 768 1024` and `playwright-cli -s=story11auth resize 375 812`, each followed by overflow/one-main assertions | 0 | Tablet `scrollWidth=768`; mobile `scrollWidth=375`; mobile exposed one `Toggle navigation` control. |
| `playwright-cli -s=story11auth run-code "async page => { await page.emulateMedia({ reducedMotion: 'reduce', forcedColors: 'active' }); ... }"` | 0 | Both `prefers-reduced-motion: reduce` and `forced-colors: active` matched. |
| `playwright-cli -s=story11auth state-save /tmp/hexalith-story11-playwright/auth-state.json`, `playwright-cli -s=story11fr state-load /tmp/hexalith-story11-playwright/auth-state.json`, and `playwright-cli -s=story11fr cookie-set .AspNetCore.Culture 'c=fr-FR\|uic=fr-FR' --domain=localhost --path=/` | 0 | Reused only the authenticated storage state and selected the supported French request culture. |
| `playwright-cli -s=story11fr reload` plus the retained post-hydration French assertion | 0 | After interactive hydration: `lang=fr`, title/heading `Locataires`, French skip link and `Utilisateurs`, and no horizontal overflow. |
| `playwright-cli -s=story11auth console error` and `playwright-cli -s=story11fr console error` | 0 | Only unrelated Keycloak/UI favicon 404s; no circuit, navigation, or application exception. |

Retained post-patch artifacts:

- `_bmad-output/implementation-artifacts/story-1-1-browser-evidence-2026-07-20/authenticated-workspace-en-2026-07-20.yml`
- `_bmad-output/implementation-artifacts/story-1-1-browser-evidence-2026-07-20/authenticated-workspace-fr-2026-07-20.yml`
- `_bmad-output/implementation-artifacts/story-1-1-browser-evidence-2026-07-20/console-2026-07-20T06-27-27-738Z.log`
- `_bmad-output/implementation-artifacts/story-1-1-browser-evidence-2026-07-20/console-2026-07-20T06-27-28-130Z.log`

AC7 and AC10 are now verified. AC9 remains conservatively blocked on platform-owned production liveness/publication authority, and `HTTP-TARGET-1` remains an explicitly recorded hosting-policy gap.
