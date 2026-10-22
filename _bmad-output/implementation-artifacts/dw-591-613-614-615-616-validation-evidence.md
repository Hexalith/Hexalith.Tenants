# Tenants UI host/router closure evidence — 2026-10-22

Closes **DW-591, DW-613, DW-614, DW-615 and DW-616** against clean entry revision `db2c6961724d1084d7035533ba41732878de14de`, using the explicitly requested bmad-build workflow. The user authorized committing and pushing the finished work to main.

## Behavior qualified

- **DW-591:** Actual `WebApplicationFactory<Program>` requests render English and French HTML with the correct `html lang` and localized scope-denial heading. Unsupported German falls back to English; opposing English formatting/French UI culture requires French. HTTP fallback checks also use EN/FR `Accept-Language` without localization query parameters. These assertions parse response DOM rather than inspecting source.
- **DW-613:** `Router.NotFoundPage` points to a routed, localized page composed through the existing FrontComposer layout, page header and Fluent anchor button. Its heading is a navigation focus target and its recovery destination is fixed at `/home`. Caller-controlled paths, return URLs and token-shaped query values are not rendered in recovery content. EN/FR mounted-router checks follow the recovery target and require the same Router instance and `FocusOnNavigate` target. Unknown HTTP requests, including intercepted-link fallbacks, re-execute only 404 responses through the same route: signed-in requests get recovery, anonymous requests retain scope denial, and 404 remains the response status. A real DELETE/405 control rejects applying the missing-page body to other HTTP failures.
- **DW-614:** The production host enables its existing OIDC recipe, while the test signs a cookie with the host's own configured ticket protector. A principal with two `eventstore:tenant` authorization claims and one `eventstore:current-tenant` renders the real scoped My Tenants page. The actual `ServerCircuitUserContextAccessor` is observed at domain gateway resolution; separate alpha/beta cases require its exact selected current tenant and `sub`. Missing and conflicting current-tenant aliases or subjects render scope denial before the domain gateway resolves. No user-context accessor or authentication handler is replaced in these host tests. This qualifies host cookie/claim handling, not live Keycloak token issuance or downstream query transport.
- **DW-615:** Twelve ordinary mandatory host cases cover workspace, detail, audit, My Tenants, user lookup and global administrators with OIDC enabled and disabled, without Dapr or external services. They require scope denial, absent domain markers, no caller identifiers in visible response text and zero domain query-gateway resolutions; the enabled privileged endpoint instead requires its real configured OIDC challenge. Cached discovery metadata isolates that challenge without replacing authentication handlers. The UI test project is already in `.github/workflows/ci.yml`'s blocking unit-test list.
- **DW-616:** The actual production `Routes` component remains mounted while its NavigationManager changes `/tenants/my` → `/home` → `/tenants/my`. Assertions require the real framework `FcHomeRouteView`, `FcHomeDirectory`, the shell, removal of the old page content and stable Router identity. This is interactive component-router coverage in bUnit, not a fresh HTTP GET or browser/SignalR transport qualification.

## Investigation and corrections

Before edits, `aspire ps --format Json` and `aspire describe --format Json` confirmed the existing AppHost and all 23 resources Running/Healthy. The local Aspire skill guided this read-only inspection. No dependency pointer, topology, authentication policy or release hold changed.

Initial new tests against entry production ran 16 cases: 14 passed and the two recovery checks failed. Investigation found that the first fixture's one-segment URL matched FrontComposer's generic module route and displayed its existing unavailable surface. The final fixture uses `/missing/private/path`, which actually has no matching route. The shared generic route and its behavior are preserved. A later two-case failure exposed only whitespace emitted inside the Fluent link; the assertion now trims that component's surrounding whitespace while preserving exact localized wording and destination assertions.

Two initial Razor builds rejected an unavailable `FluentAnchor` and the obsolete `Appearance.Accent` API respectively. The final page reuses the repository's existing V5 `FluentAnchorButton` with `ButtonAppearance.Primary`. The final source build has zero warnings/errors.

The routing implementation follows the installed .NET 10 API and [Microsoft's navigation guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/navigation?view=aspnetcore-10.0), with an explicit layout on the routed recovery page.

The first full UI run reported **7203 total, two failures, zero errors/skips**: the new page needed an explicit entry in the existing layout conformance inventory, and an unchanged metadata held-authority case observed `UnableToVerify` instead of `Idle`. That metadata case failed alone (**2 total, one failure**) and still failed after all terminal expectations were placed in `WaitForAssertion` (**2 total, one failure**). The production ordering published admission cancellation before advancing the lifecycle revision; an off-renderer unwind could retain the provisional failure. The narrow repair publishes the lifecycle revision first. The existing regression retains every original Idle/no-dispatch/no-ownership/balanced-activity assertion. This verification repair closes no additional ledger record.

Independent review qualified the HTTP fallback and non-404 filter, strengthened localization and authentication controls, and rechecked the lifecycle ordering. Its initial focus concern used Router 10.0.0; the actual 10.0.13 runtime invokes `Found` for NotFoundPage, and the added test pins that framework focus path. No concrete findings remain, and no review work was deferred. Browser anchor activation and SignalR transport are not claimed. The full per-finding triage is in the spec.

The first enabled-challenge fixture configured metadata after the framework's configuration manager was initialized and attempted discovery at the intentionally invalid host. The corrected fixture supplies a static cached configuration manager while retaining the production handler. An intermediate middleware build required `ConfigureAwait(false)` under CA2007; the corrected final source build is clean. A combined class/method runner filter selected zero tests and was discarded; the final checks use class filters only.

## Executed checks

```bash
dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false

dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantsUiHostBoundaryTests' -class '*TenantsRouterTests'

python3 _bmad-output/implementation-artifacts/dw-591-613-614-615-616-verify.py
```

The final source build passed with **zero warnings/errors**, 19.68 seconds. The last test-only fixture rebuild also passed with zero warnings/errors using `--no-restore -p:BuildProjectReferences=false`; dependencies had already been built from source. The initial focused host/router set passed **16/16**. Final reviewed host/router, conformance and metadata classes pass **181/181**, zero failures/errors/skips, 6.505 seconds:

```bash
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll -class '*TenantsUiHostBoundaryTests' -class '*TenantsRouterTests' -class '*DomainUiFluentConformanceTests' -class '*EditTenantMetadataFlowTests'
```

The closure verifier passes: all **657 IDs preserved, 510 done, 147 open**, with **137** maintenance rows covering every remaining open ID exactly once. It reconstructs each selected original record from preserved fields, requires all 652 unrelated record bodies and prior preamble to remain unchanged, and verifies the exact maintenance-index edits. Dependency pointers, archive, sprint status and release-readiness artifact match the entry revision.

## Final complete verification

```bash
dotnet tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll

dotnet format whitespace tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --no-restore --verify-no-changes --include tests/Hexalith.Tenants.UI.Tests/Host/TenantsUiHostBoundaryTests.cs tests/Hexalith.Tenants.UI.Tests/Components/TenantsRouterTests.cs

python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-dw-591-613-614-615-616-ui-host-routing.md

git diff --check

node_modules/.bin/commitlint --edit /tmp/tenants-five-commit-message.txt --verbose
```

Complete UI suite: **7217 passed, zero failures/errors/skips/not-run**, 133.664 seconds. Scoped whitespace formatting exits 0 with workspace-loading warnings and no formatting findings. Git whitespace and the actual-spec gitlink guard pass; there are no reference pointer changes. The GitHub-event wrapper's initial local invocation required `GITHUB_EVENT_PATH or --event-path`; the direct story guard above is the applicable local check.

Lockfile-pinned **commitlint 21.2.3** accepted the exact full commit-message file:

```text
fix(ui): resolve five deferred host routing records
✔   found 0 problems, 0 warnings
```

[Raw logs and tested-source hashes](dw-591-613-614-615-616-raw-logs.zip) retain the initial broad/focused failures, final builds, 181-case focused pass, 7217-case complete pass and successful guard/lint outputs. The archive manifest identifies the exact baseline, commands, results and SHA-256 of each tested production/test source. Release readiness remains unchanged; these host/router and validation repairs do not qualify publication or live identity-provider/browser transport behavior.
