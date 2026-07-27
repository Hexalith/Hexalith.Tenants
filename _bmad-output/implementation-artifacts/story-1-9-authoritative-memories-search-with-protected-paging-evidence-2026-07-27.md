# Story 1.9 — Authoritative Memories Search with Protected Paging Evidence (2026-07-27)

Supersedes the 2026-07-26 report, which was withdrawn. That report certified two coverage-gate tests
that do not exist in the suite, described a terminal-surface design that had been abandoned before
merge, named a public state member that was never added, denied submodule and dependency changes the
branch made, pinned source revisions that were not the ones under review, and recorded test totals
from a pre-merge revision. Those claims are enumerated in the Review Findings section of
`spec-1-9-authoritative-memories-search-with-protected-paging.md`. This report is derived from the
shipped source at the revision named below; every command in it was executed, and every test named
in it was verified to exist by name before being cited.

## Environment and immutable source pins

| Item | Value |
|---|---|
| Revision this report describes | `d59dd59a15556d9c3baf0439366eb53009200fe6` |
| .NET SDK | `10.0.302`, target `net10.0` |
| `references/Hexalith.EventStore` | `56acc0788e00388038eb1889f3d77c7730a65c94` |
| `references/Hexalith.Memories` | `6e6d3fb9cb04678a0a1994f43c00804523ed1a26` |
| `references/Hexalith.FrontComposer` | `7870526090a8596082e3df034ecacf4c07881a04` |
| `references/Hexalith.Builds` | `4e5c2a3ea6510f38121f718fa122e7b92489821c` |
| `references/Hexalith.PolymorphicSerializations` | `f3b23304283b0e7a35ffa66bf8d9bf2499e35e66` |

**Submodules and the published package boundary did change.** Commit `1af283b` moved four gitlinks —
`Hexalith.Builds`, `Hexalith.EventStore`, `Hexalith.Memories`, `Hexalith.PolymorphicSerializations` —
under a `test:` subject that mentions none of them. The `Hexalith.Memories` move was unrelated CI/CD
work. As a downstream consequence, `Microsoft.Extensions.Http.Resilience` entered the expected
dependency sets for `Hexalith.Tenants.Server` and `Hexalith.Tenants.Testing`
(`scripts/validate-nuget-packages.py`, mirrored in `CiQualityGateScriptTests.cs`). No file under
`references/` was edited. The previous report's blanket denial of all of this was false.

The public UI state surface gained `TenantListSnapshot.FallbackPagingRecovered`, `.PagingNotice` and
`.Lifecycle`, plus `TenantListSurfaceKind.SearchPageEmpty` and
`TenantListReason.SearchPagingRestarted`. There is no `SearchPagingInvalidationPending` member; the
previous report named one, which was residue of an abandoned design.

## Implemented contract

- Non-empty search uses one syntactic `tenants-index` Memories page as candidate input only. Every
  survivor is hydrated through the authorized Tenants detail seam, status is rechecked against the
  hydrated detail, and visible rows are sorted deterministically with an ordinal tenant-id
  tie-breaker. Pending state remains `Unknown` without proof from the authoritative seam.
- **A window that yields no authorized row ends paging.** `HasMore` was previously derived from
  `TotalCount`, the raw pre-authorization index total, which made a fully unauthorized window
  distinguishable from a genuine no-match: the former advertised further pages and a live Next
  control, the latter did not. That difference disclosed both the existence and a page-granular count
  of tenants the caller is not permitted to see. `HasMore` is now `rows.Count > 0 && nextOffset <
  TotalCount`. The accepted cost is that a deep result set whose leading window is entirely hidden is
  not reachable by paging forward.
- **The empty-search-page surface renders one message for both causes** and no longer claims that
  rows failed verification — a statement that is false for the dominant mistyped-term case. Because
  the page is now always terminal for the current search, it carries the same reset affordance as the
  filtered-empty verdict rather than being a dead end. The split final/non-final copy was removed
  along with `Tenants.List.State.SearchPageEmpty.FinalMessage`.
- Raw-page accounting is an upper bound only: a page carrying fewer hits than the requested window is
  an ordinary short page and stays authoritative; only an over-full page or one whose hits overflow
  the reported total is a contract violation. The next raw offset advances by the requested window
  bounded to the reported total. **This rule depends on a Memories server property that no test in
  this repository can observe** — see the open blocker below.
- Response validation rejects null, contradictory, truncated, degraded, wrong-axis, wrong-query,
  oversized, over-full, and total-overflowing pages. Malformed, duplicate, forbidden, missing, and
  status-filtered hits are never backfilled or disclosed.
- **A successfully read projection whose `Name` is null is one malformed record, not an outage.** It
  previously set the operational-failure flag, so a single bad row replaced whole-set search for the
  query that matched it with the entire unfiltered tenant list under a "search unavailable" notice.
  It now raises the ordinary enrichment-degraded signal, which cannot reach the fallback path.
- **`TenantStatus.Unknown` is no longer pushed down as an index attribute filter.** The index
  publisher coerces `Unknown` to the event's concrete fallback and never writes `status=Unknown`, so
  the push-down matched nothing and reported zero tenants while the same filter on the ordinary list
  listed them. The authoritative recheck against the hydrated detail enforces the filter either way.
- Hydration is cancellation-aware and bounded to `TenantQueryGateway.MaximumHydrationConcurrency`
  (8). Forbidden and not-found candidates disappear silently; operational loss yields generic partial
  state when verified rows remain and ordinary authorization-safe list fallback otherwise.
- Cursor-failure containment is a two-set decision. The surfacing set (`OutOfMemoryException`,
  `NullReferenceException`, `ObjectDisposedException`, `ArgumentNullException`) is excluded before any
  base-type match, because `ObjectDisposedException` derives from `InvalidOperationException` and
  `ArgumentNullException` from `ArgumentException`. Contained decode failures force raw page zero;
  contained encode failures degrade to the ordinary list.
- **Cursor invalidation clears protected history on the load that reports it, terminal surfaces
  included.** Both notice bars render from the notice reasons alone and never consult `Kind`, so an
  `Error`/`Unauthorized` surface carries the copy that explains the clearing. No deferral or pending
  mechanism exists. The previous report described the exact opposite.
- **A terminal fallback surface now also carries the search-unavailable notice.** The terminal copy
  explains only that the ordinary list failed, so without it an operator whose ordinary list also
  failed was never told that whole-set search had failed independently.
- Authoritative search that degrades to the ordinary list emits exactly one reason-code-only
  diagnostic. No cursor, offset, query, or tenant id reaches that sink.
- Search cursors use a dedicated Data Protection purpose, seven fixed-size scope fields, and scoped
  server-circuit paging state, and remain absent from URLs, DOM, browser storage, clipboard,
  diagnostics, and default `HttpClient` logs. Components never call Memories.

## Exact automated verification

All commands ran from the repository root on 2026-07-27 at `d59dd59`.

| Command | Result |
|---|---|
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` | Passed — **0 warnings, 0 errors** |
| Focused seven-class UI executable run (`TenantQueryGatewayTests`, `TenantSearchCursorTests`, `TenantListSurfaceTests`, `TenantWorkspaceStateTests`, `TenantsWorkspaceTests`, `TenantsUiCompositionTests`, `DomainUiFluentConformanceTests`) | Passed — **452 total, 0 failed, 0 skipped** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` | Passed — **1,222 total, 0 failed, 0 skipped** |
| `tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests -noLogo -noColor -parallel none` | Passed — **114 total, 0 failed, 0 skipped** |
| Sample index-handoff lane (`MemoriesSearchIndexEventPublisherTests`) after a warning-clean Release build | Passed — **7 total, 0 failed, 0 skipped** |
| `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` | Passed — **0 warnings, 0 errors** |
| `git diff --check` | Passed — no whitespace errors |

No transcript of intermediate red-phase runs is retained, so no claim is made about them. The
reproducible commands above are the authoritative record.

## Guards proven capable of failing

A passing assertion is not evidence unless it could have failed. The following were repaired in this
pass because they could not.

| Guard | Why it certified nothing | What now proves it |
|---|---|---|
| Hydration concurrency bound | Both assertions compared the observed maximum to `MaximumHydrationConcurrency`, so raising the constant to the 100 page-size ceiling — no bound at all — still passed | The literal `8` is pinned independently, and the observed maximum is compared to that literal |
| Support-safety `ToString()` ban | The scanner matched one literal spelling and was evaded seven times inside the directory it scans — four by `(x.ToString() ?? string.Empty).ShouldNotContain(`, three by a stringified local asserted on a later line | Two rules: same-statement match, and stringified-local dataflow. The three local-variable sites were found by the new rule, not by review |
| The absence assertions themselves | `TenantDetailSnapshot` and `TenantConfigurationProjectionProof` are classes with no `ToString` override, so every assertion ran against a bare type name and could not fail for any payload | Both carry a support-safe fixed-shape override omitting tenant identity, configuration keys and values, and ETag; every site is pinned by equality |
| Codec purpose isolation | The test asserted the *container's* Data Protection provider identity, never the provider the codec received; a codec building its own `EphemeralDataProtectionProvider` passed while making cursors undecodable across restart and replicas | A cursor produced by the registered codec must decode under an independent codec over the same injected provider and must not under a different one. **Mutation-verified**: the self-built-provider variant fails this test |
| Fully hidden vs no-match window | Nothing compared the two | `List_search_renders_a_fully_hidden_window_identically_to_a_genuine_no_match` drives both through the gateway and compares every operator-visible field, including the pinned diagnostic |

## Open blockers

- **MEMORIES-RUNTIME-1.9 — owner: Tenant platform runtime operator — OPEN.** No authenticated
  AppHost/Memories runtime session was available. A live authenticated Memories-to-Tenants search,
  cross-tenant denial trace, clean network/telemetry inspection, and runtime key-rotation recovery are
  not claimed. This blocker also covers the raw-page advancement rule, whose correctness rests on the
  Memories server applying `Offset` before dropping entries that fail its required-field check while
  reporting the untrimmed total. Every gateway test stubs `MemoriesClient.SearchAsync`, so no test
  here witnesses that property.
- **BROWSER-SEARCH-1.9 — owner: Tenant UI QA — OPEN.** No dated authenticated EN/FR browser session.
  Real-browser focus, keyboard paging, 320/375/430/768/1024/1366/1440 px layout, forced colors,
  reduced motion, and clean console/network behaviour are not claimed. This blocker also covers the
  single-polite-live-region claim: bUnit renders `<fluent-message-bar>` as an inert custom element, so
  the in-suite count is an artefact and cannot speak to what a real browser nests.
- **AT-NVDA-1.9 — owner: Accessibility QA — OPEN.** No dated human NVDA session exists. Automated
  ARIA and live-region checks cannot certify spoken ordering or real assistive-technology focus.
- **TELEMETRY-SEARCH-1.9 — owner: Tenant platform runtime operator — OPEN.** The no-telemetry
  guarantee is narrowed to the channel actually closed. `RemoveAllLoggers()` suppresses the
  caller-side `HttpClient` logger only. The raw search term and raw protected offset still travel in
  the Memories request URL, where the Memories server records them in its own request scope, and any
  host composing `AddServiceDefaults` gets `AddHttpClientInstrumentation()`, which stamps the full URL
  onto every outgoing client span. Closing those two channels requires either a Tenants-side span
  filter or moving the search to a request body, the latter being a Memories API change this story's
  Block-If bars.

## Outstanding review findings

This story carries 21 unapplied patch findings from the 2026-07-27 code review, recorded unchecked in
the controlling spec with `file:line` anchors. The story is `in-progress`, not `review`.

No unavailable proof was inferred from Story 1.8 evidence, bUnit, CSS or source scans, or a different
story's runtime artifacts.
