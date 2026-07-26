# Story 1.9 — Authoritative Memories Search with Protected Paging Evidence (2026-07-21)

Implementation and verification completed against the revised Story 1.9 contract on 2026-07-26.
This report records current-source evidence only; it does not infer runtime or assistive-technology
proof from component tests.

## Environment and immutable source pins

| Item | Value |
|---|---|
| Root implementation baseline | `85838fbbb4efcd131a44d4ac4535110b1a9d3217` |
| .NET SDK | `10.0.302`, target `net10.0` |
| Hexalith.EventStore source | `a17cafb0ca269cadb09cfbbecbbdae9ec10bebe6` |
| Hexalith.Memories source | `358bef352bf657bcd2a7c4e91cb0a6d298c3f7a4` |

No package, dependency, published/external backend contract, submodule, DAPR, AppHost, or file under
`references/` was changed. The public UI state surface did gain the appended
`TenantListSnapshot.PagingNotice` property so simultaneous safe search and paging notices remain
distinct. The story workflow updates the controlling spec's status/baseline metadata; implementation
code did not edit its intent contract or acceptance body.

## Implemented contract

- Non-empty search uses one syntactic `tenants-index` Memories page as candidate input only. Every
  survivor is hydrated through the authorized Tenants detail seam, status is rechecked against the
  hydrated detail, and visible rows are sorted deterministically with an ordinal tenant-id
  tie-breaker. Pending state remains `Unknown` without proof from the authoritative seam.
- Response validation rejects null, contradictory, truncated, incomplete, degraded, wrong-axis,
  wrong-query, oversized, and otherwise unsafe Memories pages. Every returned raw hit consumes the
  protected next offset; malformed, duplicate, forbidden, missing, and filtered hits are never
  backfilled or disclosed.
- Hydration is cancellation-aware and bounded to eight concurrent requests. Forbidden and not-found
  candidates disappear silently; operational loss yields generic partial state when verified rows
  remain and ordinary authorization-safe list fallback otherwise.
- Search cursors use the dedicated DataProtection-backed codec, seven fixed-size scope fields, and
  scoped server-circuit paging state. Cryptographic, tampering, user, scope, key, and index-shrink
  invalidation restart once at raw page zero with safe localized recovery. Ordinary fallback paging
  has independent history and recovery.
- Search cursors and raw offsets remain absent from URLs, DOM, browser storage, clipboard,
  diagnostics, and default `HttpClient` logs. Components do not call Memories. Existing host
  `IQueryCursorCodec` registrations cannot replace the tenant-search purpose.
- The workspace preserves protected page/history and its server-held page size only for a validated
  in-circuit detail return with matching selection, anchor, and exact scope. It resets direct visits
  and query identity changes to page one, restores safe return focus context, exposes
  sparse/partial/empty/fallback/recovery states through Fluent controls and polite notices, and keeps
  whole-set language conditional on `IsAuthoritativeSearch`.

## Exact automated verification

All commands ran from the repository root on 2026-07-26.

| Command | Result |
|---|---|
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` | Passed — **0 warnings, 0 errors** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantSearchCursorTests -class Hexalith.Tenants.UI.Tests.Components.TenantListSurfaceTests -class Hexalith.Tenants.UI.Tests.State.TenantWorkspaceStateTests -class Hexalith.Tenants.UI.Tests.TenantsWorkspaceTests -class Hexalith.Tenants.UI.Tests.TenantsUiCompositionTests -class Hexalith.Tenants.UI.Tests.DomainUiFluentConformanceTests` | Passed — **349 total, 0 failed, 0 skipped** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.TenantDetailSurfaceTests` | Passed — **56 total, 0 failed, 0 skipped** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` | Passed — **1,096 total, 0 failed, 0 skipped** |
| `dotnet build samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false && samples/Hexalith.Tenants.Sample.Tests/bin/Release/net10.0/Hexalith.Tenants.Sample.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Sample.Tests.Handlers.MemoriesSearchIndexEventPublisherTests` | Passed — build **0 warnings, 0 errors**; tests **7 total, 0 failed, 0 skipped** |
| `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` | Passed — **0 warnings, 0 errors** |
| `git diff --check` | Passed — no whitespace errors |
| `git diff --no-index --check /dev/null _bmad-output/implementation-artifacts/story-1-9-authoritative-memories-search-with-protected-paging-evidence-2026-07-21.md` | Passed whitespace inspection — exit 1 is expected for the untracked content diff; **no whitespace diagnostics** |

The implementation session reported an initial focused build failure before exact-scope matching,
independent fallback recovery, and the secondary safe notice existed; it also reported cryptographic
codec and workspace failures before their fixes. No accessible transcript or log artifact was
retained for those red-phase runs, so this paragraph is session context rather than independently
auditable evidence. The reproducible final commands above are the authoritative results.

## Acceptance-criterion verdict

| Criterion | Verdict | Current evidence |
|---|---|---|
| Candidate-only Memories input; authorized fields; status/sort/raw accounting; no disclosure | **verified** | Gateway tests cover exact request shape, malformed/duplicate/forbidden/not-found/visible candidates, null and mismatched hydration, stale index fields, six sort combinations, ordinal tie-breaking, raw offset accounting, no backfill, current/stale aggregation, and `Pending=Unknown`. |
| Protected server-held paging and seven-field identity reset | **verified for current sources** | Cursor/state/component tests cover all scope fields, fixed-size hashes, Next/Previous in authoritative and fallback modes, page-two identity reset, host-codec isolation, no URL/DOM/storage cursor, support-safe formatting, and default Memories HTTP-log suppression. |
| Cross-user/tampering/key/scope/index-shrink recovery | **verified** | Cursor and gateway tests reject cross-user and wrong-scope reuse, discard unsafe failed-decode out values, cover invalid/tampered/key-failure paths plus both greater-than and equality shrink boundaries, accept valid short final pages, request raw page zero exactly once, clear only the applicable history, and render only generic mapped/deduplicated polite notices. No protected value or rejection reason is exposed. |
| Partial hydration and ordinary-list fallback stay honest | **verified** | Tests cover null/not-modified/mismatched/degraded/stale detail shapes, each search failure family, silent forbidden/not-found drops, verified-row partial rendering, dual outage plus fallback-recovery notices, and fallback page-local rather than whole-index semantics. |
| Blank search preserves ordinary cursor list with zero Memories calls | **verified** | Gateway and component tests prove canonical blank input issues no Memories request and continues unchanged ordinary cursor paging without loaded-page search imitation. |
| In-circuit detail round trip restores protected paging or safely restarts | **automated verified; browser blocked** | bUnit tests require matching normalized selection/anchor and exact scope, retain page two, Previous history, and page size 50 across component disposal/recreation, keep cursor/page size out of the return URL, reset fresh or invalid-context visits, and cover missing retained state restarting page one with an identity-bound polite localized notice. Real authenticated browser focus proof is blocked below. |
| Rapid changes cancel obsolete work; maximum page is bounded and deterministic | **verified** | Component tests observe cancellation during rapid query replacement. Gateway tests propagate caller cancellation, exercise 100 hits, assert at most eight concurrent hydrations, and retain deterministic output order. |
| Dated evidence covers runtime, localization, responsive, accessibility, and support safety honestly | **partial with exact blockers** | Exact EN/FR resource strings and parity, stable selectors, Fluent controls, live regions, responsive/forced-colors/reduced-motion source rules, source safety scans, and all specified automated lanes pass. Authenticated runtime/browser and human NVDA evidence remain open below. |

## Cross-tenant negative proof

The focused suite creates a protected cursor for one authenticated user and attempts reuse under a
different authenticated user. Decode is rejected, the gateway queries raw offset zero exactly once,
the protected page/history is cleared, and only `SearchRefreshed` is surfaced. The tests also verify
that the cursor, raw offset, user identity, candidate identity, source URI, index fields, and
cryptographic failure do not appear in snapshot or paging diagnostics, rendered markup, canonical
URLs, browser-storage calls, or default Memories HTTP logging configuration.

## External evidence blockers

- **MEMORIES-RUNTIME-1.9 — owner: Tenant platform runtime operator — OPEN.** The required AppHost
  precheck found no running host. Two attempts, including one after the warning-clean solution build,
  ran `aspire start --apphost src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj --non-interactive --format Json`.
  Both timed out after 120 seconds while build output remained at `Determining projects to restore`
  and exited 2; the retry also reported that the native certificate was not trusted by OpenSSL.
  `aspire ps --format Json` then returned `[]`, proving that no resources started. Retry logs:
  `/home/administrator/.aspire/logs/cli_20260726T172759_ca5e9a1d.log` and
  `/home/administrator/.aspire/logs/cli_20260726T172759714_detach-child_c0dd43a7cb8941cea0e02e17be4faa4d.log`.
  Consequence: a live authenticated Memories-to-Tenants search, cross-tenant denial trace, clean
  network/telemetry inspection, and runtime key-rotation recovery are not claimed. Reopen trigger:
  restore/build the AppHost successfully with its configured dependencies, start all required
  resources, execute authorized and cross-tenant fixtures, and retain dated resource, trace, log, and
  network evidence.
- **BROWSER-SEARCH-1.9 — owner: Tenant UI QA — OPEN.** No dated authenticated EN/FR browser session
  was available. Consequence: real-browser page-two/detail-return focus, keyboard paging,
  320/375/430/768/1024/1366/1440 px layout, forced colors, reduced motion, and clean console/network
  behavior are not claimed. Reopen trigger: run the authenticated AppHost fixtures in EN and FR,
  exercise whole-set Next/Previous, sparse/partial/empty/fallback and recovery states at the listed
  widths and accessibility modes, and retain screenshots plus console/network logs and before/after
  focus locators.
- **AT-NVDA-1.9 — owner: Accessibility QA — OPEN.** No dated human NVDA session exists for this
  implementation. Consequence: automated ARIA/live-region checks cannot certify spoken ordering or
  real assistive-technology focus behavior. Reopen trigger: record browser and NVDA versions,
  keyboard steps, spoken search semantics/partial/fallback/recovery announcements, and focus results
  across paging and detail return.

No unavailable proof was inferred from Story 1.8 evidence, bUnit, CSS/source scans, or a different
story's runtime artifacts.
