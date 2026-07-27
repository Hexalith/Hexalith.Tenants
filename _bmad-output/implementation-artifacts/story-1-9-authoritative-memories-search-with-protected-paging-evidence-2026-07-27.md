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
| Revision this report describes | uncommitted working tree on top of `5fdbc80` (pass-3 review repairs) |
| .NET SDK | `10.0.302`, target `net10.0` |
| `references/Hexalith.EventStore` | `73589770b14888b703d78d37325b066befa0689c` |
| `references/Hexalith.Memories` | `6e6d3fb9cb04678a0a1994f43c00804523ed1a26` |
| `references/Hexalith.FrontComposer` | `7870526090a8596082e3df034ecacf4c07881a04` |
| `references/Hexalith.Builds` | `cca42924caf86342fa4e23a9d9fefde40d30cd17` |
| `references/Hexalith.PolymorphicSerializations` | `f3b23304283b0e7a35ffa66bf8d9bf2499e35e66` |

Every value above was read from `git ls-tree HEAD references/` at the revision named, not carried over
from an earlier draft. The pass-3 code review found that the previous edition of this table pinned
`Hexalith.EventStore` at `56acc078` and `Hexalith.Builds` at `4e5c2a3e` — neither of which was the
gitlink under review — and that it described revision `d59dd59` while four later commits had already
shipped. That is the same defect this report was written to correct, committed a second time.

**Submodules and the published package boundary did change, twice.** Commit `1af283b` moved four
gitlinks — `Hexalith.Builds`, `Hexalith.EventStore`, `Hexalith.Memories`,
`Hexalith.PolymorphicSerializations` — under a `test:` subject that mentions none of them. The
`Hexalith.Memories` move was unrelated CI/CD work. As a downstream consequence,
`Microsoft.Extensions.Http.Resilience` entered the expected dependency sets for
`Hexalith.Tenants.Server` and `Hexalith.Tenants.Testing` (`scripts/validate-nuget-packages.py`,
mirrored in `CiQualityGateScriptTests.cs`). Then `feb71e0` — the commit that added the previous
edition of *this report*, under the subject "re-derive the evidence report from shipped source" —
moved `Hexalith.Builds` and `Hexalith.EventStore` again, alongside `.github/workflows/release.yml`
and a 234-line `PackageGovernanceTests.cs` change. Commits `332e26f` and `de2ded0` carry the same
unrelated release/packaging work under story-1.9 subjects. No file under `references/` was edited.

The public UI state surface gained `TenantListSnapshot.FallbackPagingRecovered`, `.PagingNotice` and
`.Lifecycle`, plus `TenantListSurfaceKind.SearchPageEmpty` and
`TenantListReason.SearchPagingRestarted`. There is no `SearchPagingInvalidationPending` member; the
previous report named one, which was residue of an abandoned design.

## Implemented contract

- Non-empty search uses one syntactic `tenants-index` Memories page as candidate input only. Every
  survivor is hydrated through the authorized Tenants detail seam, status is rechecked against the
  hydrated detail, and visible rows are sorted deterministically with an ordinal tenant-id
  tie-breaker. Pending state remains `Unknown` without proof from the authoritative seam.
- **A window emptied *by hiding* ends paging; a window emptied any other way keeps advancing.**
  `HasMore` was originally derived from `TotalCount`, the raw pre-authorization index total, which made
  a fully unauthorized window distinguishable from a genuine no-match: the former advertised a live
  Next control, the latter did not, disclosing both the existence and a page-granular count of tenants
  the caller may not see. The first repair replaced it with `rows.Count > 0 && nextOffset < TotalCount`,
  which over-corrected: a window is also emptied by the operator's own status recheck, by a dropped
  unrenderable record, and by malformed or duplicate index hits, none of which is a secret. Ending
  paging there made accessible matches past the window unreachable while the surface claimed nothing
  matched at all. `HasMore` is now `!windowHiddenOnly && nextOffset < TotalCount`, where
  `windowHiddenOnly` requires every hydrated candidate to have been refused with 403 or 404. The drop
  reason is carried out of hydration for exactly this decision.
  **This closes the fully hidden window only.** A partially hidden window still renders its surviving
  rows beside a live Next, which continues to advertise that the window held more than it showed. That
  was reviewed and accepted as out of scope for this story — see the open risk below.
- **The empty-search-page surface renders one message for both causes** and no longer claims that
  rows failed verification — a statement that is false for the dominant mistyped-term case. It carries
  the same reset affordance as the filtered-empty verdict rather than being a dead end. The split
  final/non-final copy was removed along with `Tenants.List.State.SearchPageEmpty.FinalMessage`.
  Two follow-on defects in that change were found by the pass-3 review and are fixed here: the reset
  button was rendered with `OnClick="OnReset"` while the only call site never passed `OnReset`, so it
  bound an unset `EventCallback` and every click was a silent no-op; and the call site passed `Reason`,
  which `ListSurfaceStates` prefers over the state message, so a window emptied with a dropped record
  attached rendered different copy than a genuine no-match and re-disclosed that candidates had
  existed. `Reason` is no longer passed to that surface, and the reset button is wired and driven by a
  test that clicks it.
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
  The claimed agreement between the two surfaces only actually holds because of the window-collapse
  rule above: `Unknown` is the rare sentinel, so its matches almost never sit in the first raw window,
  and collapsing paging on any empty window reinstated the same confident zero-result answer through
  the other door. Both halves are now pinned by tests, including that no attribute filter is sent.
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
  `Error`/`Unauthorized` surface carries the copy that explains the clearing.
  A *separate* deferral mechanism does exist and is deliberate: `TenantSearchPagingState`
  `PendingRecoveryScope` holds a page-one recovery notice owed for one exact search scope, which
  survives the component disposal a tenant-detail navigation causes. The previous edition of this report
  denied any such mechanism while the code shipped it. It now also records which paging mode owes the
  notice (`PendingRecoveryAuthoritative`), because explaining an ordinary-list discard with
  protected-search copy asserts a protected search page that never existed. `Dispose` deliberately does
  not clear the pending scope, and that is now pinned by a test that disposes the component and
  re-renders on the same scope; the redundant clear in `ApplyWorkspaceState` was removed, having been a
  guaranteed no-op on the one path that needed it — it ran before `SearchPaging` was ever resolved.
  The `SearchRefreshed` copy was reworded: "the protected search page was no longer available" is false
  on the browser-Back path, where the page was available and was discarded because the return context
  could not be validated.
- **A terminal fallback surface now also carries a search-unavailable notice, but not the ordinary
  one.** The terminal copy explains only that the ordinary list failed, so without any notice an
  operator whose ordinary list also failed was never told that whole-set search had failed
  independently. Reusing `SearchUnavailable` there was wrong in the other direction: its copy invites
  the operator to "continue browsing the authorized tenant list", which is exactly what did not load,
  and on the `Unauthorized` surface it sat under "Sign in required" telling them to browse anyway. A
  distinct `SearchAndListUnavailable` reason carries honest EN/FR copy; the `Error` and `Unauthorized`
  paths are both pinned.
- **A search term longer than 256 characters is reported, not silently dropped.** Normalization returns
  `null` past the bound, so the term was discarded, the input the operator was typing into was blanked,
  the canonical URL lost the parameter, and the unfiltered list loaded with no explanation — the only
  silent degradation in the feature. The input now carries `MaxLength`, so the interactive path cannot
  reach the rejection at all, and a URL-supplied over-length term raises a localized
  `SearchTermTooLong` notice. The bound is one constant shared by the workspace and the gateway rather
  than two literals said to mirror each other, and the 257 boundary is pinned.
- **Next is offered only when the click can advance.** Enablement read `snapshot.HasMore` while both
  branches of `NextPageAsync` require a non-null cursor, and the ordinary list path passes the server's
  `HasMore` and `Cursor` through independently with no consistency check. `HasMore = true` beside a null
  cursor produced a live button whose every click did nothing: no load, no re-render, no notice.
  Enablement now also requires the paging cursor.
- **Retained paging history is capped without stranding the operator.** The cap dropped the oldest
  back-step, which is the page-one sentinel, so Previous walked back only as far as page two and then
  reported no previous page — the signal that everywhere else means "you are on page one" — leaving page
  one unreachable without retyping the search. The cap now retires a middle step and keeps the sentinel.
- Authoritative search that degrades to the ordinary list emits exactly one reason-code-only
  diagnostic. No cursor, offset, query, or tenant id reaches that sink.
- Search cursors use a dedicated Data Protection purpose, seven fixed-size scope fields, and scoped
  server-circuit paging state, and remain absent from URLs, DOM, browser storage, clipboard,
  diagnostics, and default `HttpClient` logs. Components never call Memories.

## Exact automated verification

All commands ran from the repository root on 2026-07-27 against the working tree described above. Every
number below was read from the run output at that revision; none is carried over from an earlier draft.

| Command | Result |
|---|---|
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` | Passed — **0 warnings, 0 errors** |
| Focused seven-class UI executable run (`TenantQueryGatewayTests`, `TenantSearchCursorTests`, `TenantListSurfaceTests`, `TenantWorkspaceStateTests`, `TenantsWorkspaceTests`, `TenantsUiCompositionTests`, `DomainUiFluentConformanceTests`) | Passed — **479 total, 0 failed, 0 skipped** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` | Passed — **1,256 total, 0 failed, 0 skipped** |
| `tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests -noLogo -noColor -parallel none` | Passed — **114 total, 0 failed, 0 skipped** |
| `samples/Hexalith.Tenants.Sample.Tests/bin/Release/net10.0/Hexalith.Tenants.Sample.Tests -noLogo -noColor -parallel none` | Passed — **39 total, 0 failed, 0 skipped** |
| `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` | Passed — **0 warnings, 0 errors** |
| `git diff --check` | Passed — no whitespace errors |

No transcript of intermediate red-phase runs is retained, so no claim is made about them. The
reproducible commands above are the authoritative record.

The pass-3 review found the previous edition of this table recording **452** focused and **1,222** full
against a revision four commits behind the branch tip, and stating that **21** patch findings were
unapplied when the controlling spec recorded seven. Those numbers, and the parallel sets in
`spec-1-9-…-paging.md` and `tests/test-summary.md`, are reconciled to the run above.

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
| That same test's "hidden" arm | It fed a **zero-hit** index page, so no candidate was ever hydrated or refused. It compared "the index omitted this window" against "nothing matched" — neither an authorization outcome — while this report cited it as the proof of the hidden-vs-no-match equivalence | The hidden arm now enqueues two real hits and answers both hydrations `Forbidden`/`NotFound`, so the window is emptied by authorization. **Mutation-verified**: reverting `HasMore` to the `TotalCount` rule fails it |
| The reset affordance on the empty-search page | Both tests were `Find(...).ShouldNotBeNull()`, and `Find` already throws when the element is absent, so they asserted nothing beyond presence. Nothing clicked it, and the control was unwired | A test clicks it and asserts the search term leaves both gateway state and the URL. **Mutation-verified**: removing `OnReset` from the call site fails it |
| The mid-load pager guard | `_pagingInFlight` had no test at all; every pager interaction in the suite is followed by a wait before the next one | A callback captured before the load is invoked twice, the second while the first is outstanding, asserting one gateway call and one back-step. **Mutation-verified** |
| The prerender guard on Previous | The one static-render test asserted requests and cursors, never the button's disabled state | The static-render test asserts `disabled` on the Previous control. **Mutation-verified** |
| The pending recovery notice across disposal | Not clearing it in `Dispose` was unpinned — re-adding the clear was green, because no test disposed the component between arming and observing | A test errors the detail-return load, disposes the component, and re-renders on the same scope. **Mutation-verified** |
| Support-safety scanner, second rule | The stringified-local rule had **no** planted-failure case: the single planted occurrence was a one-line Rule 1 match, so deleting the whole rule and its regex left both gate tests green. The rule was also evaded by `string?`, by assignment to an already-declared local, by a formatter-wrapped chain, by `$"{x}".ShouldNotContain(`, and by `Contains(...).ShouldBeFalse()` | The scan is statement-based, and a seven-row theory plants one occurrence per evadable spelling. A third argument pins *which* rule fired, so a row cannot be silently absorbed by Rule 1. A control case keeps legitimate markup assertions legal |
| "Components never call Memories" | The scan inspected only `[Inject]` property types and constructor parameters. `TenantsWorkspace` injects `IServiceProvider` and resolves through it, so `Services.GetRequiredService<MemoriesClient>()` in any component passed, as would `Lazy<MemoriesClient>` | A source scan over every component file rejects the token outright. **Mutation-verified**: adding that exact resolution to a component fails the test, naming the file |

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

- **PARTIAL-WINDOW-DISCLOSURE-1.9 — owner: Tenant UI product owner — OPEN, accepted out of scope.**
  A partially hidden search window renders its surviving rows beside a live Next control, which
  advertises that the window held more than it showed. `TenantQueryGatewayTests` pins this as intended
  behaviour for a window where five of six candidates were dropped. The window-collapse rule closes the
  *fully* hidden case only. Closing the partial case means not exposing per-page authorized counts
  through pager state at all, which is a change to how search paging advertises depth. Reviewed on
  2026-07-27 and accepted as out of scope for this story; reopen trigger is any requirement that a
  partially hidden window be indistinguishable from a complete one.

## Outstanding review findings

The pass-3 code review raised its own 32 merged findings over the pass-2 repair delta. Two were decisions
resolved by the story owner and are recorded in the controlling spec: the window-collapse rule (collapse
only on hidden or absent candidates, so no spec amendment is required) and partial-window disclosure (out
of scope, recorded as the open risk above). Twenty-six patches were applied and are checked off there.

**The seven pass-2 patch findings that were left unchecked remain unchecked and unapplied.** They were
not in the pass-3 review's scope — that review examined the repair delta, not the pass-2 backlog — and
nothing in this pass addressed them. They are, verbatim from the controlling spec:

- The crossing test's headline assertion cannot fail in either direction.
- Log-sink non-disclosure assertions are blind to the `Exception` argument.
- The pending-recovery scope binding is not distinguished from clear-on-state-change. (Pass 3 added a
  disposal-survival test for the same mechanism, which narrows but does not close this.)
- The secondary notice bar's message and testid guards are unreachable in their test.
- The standalone-host composition test asserts no codec identity or round trip, while `Program.cs`
  hand-duplicates the module registration.
- The JS-interop identifier scan has no control case and misses module-call spellings.
- The localizer parity gate silently skips doubles it cannot construct.

All seven are test-efficacy rather than behaviour, and none is blocked.

Two items are deferred and recorded in `deferred-work.md`: the pass-2 finding on end-to-end `Lifecycle`
binding coverage, checked off after closing 1 of 13 binding sites (the other 12 belong to other stories'
surfaces), and the per-page candidate dedup reclassification, which had never entered the ledger at all.
The story is `in-progress`, not `review`.

No unavailable proof was inferred from Story 1.8 evidence, bUnit, CSS or source scans, or a different
story's runtime artifacts.
