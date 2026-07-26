# Story 1.9 — Authoritative Memories Search with Protected Paging Evidence (2026-07-26)

Re-derived from the amended Story 1.9 contract on 2026-07-26. Commit
`a6f5801864c91733d09f29300b23d0b10a7c314a` rolled the previous review-repair delta back to the story
baseline; this report covers the re-derivation plus the repairs required by the third review pass.
It records current-source evidence only; it does not infer runtime or assistive-technology proof from
component tests.

## Environment and immutable source pins

| Item | Value |
|---|---|
| Root implementation baseline | `a6f5801864c91733d09f29300b23d0b10a7c314a` |
| .NET SDK | `10.0.302`, target `net10.0` |
| Hexalith.EventStore source | `a17cafb0ca269cadb09cfbbecbbdae9ec10bebe6` |
| Hexalith.FrontComposer source | `7870526090a8596082e3df034ecacf4c07881a04` |
| Hexalith.Memories source | `358bef352bf657bcd2a7c4e91cb0a6d298c3f7a4` |

No package, dependency, published/external backend contract, submodule, DAPR, AppHost, or file under
`references/` was changed. The public UI state surface gained appended `TenantListSnapshot` members —
`FallbackPagingRecovered`, `PagingNotice`, and `SearchPagingInvalidationPending` — plus
`TenantListSurfaceKind.SearchPageEmpty` and `TenantListReason.SearchPagingRestarted`. The story
workflow owns the controlling spec's status/baseline metadata; implementation work did not edit its
intent contract or acceptance body.

## Implemented contract

- Non-empty search uses one syntactic `tenants-index` Memories page as candidate input only. Every
  survivor is hydrated through the authorized Tenants detail seam, status is rechecked against the
  hydrated detail, and visible rows are sorted deterministically with an ordinal tenant-id
  tie-breaker. Pending state remains `Unknown` without proof from the authoritative seam.
- **Raw-page accounting is an upper bound only.** The consumed Memories server omits entries that fail
  its own required-field check while still reporting the untrimmed total, so a page carrying fewer
  hits than the requested window is an ordinary short page and stays authoritative. Only an over-full
  page (more hits than the requested window) or a page whose hits overflow the reported total is a
  contract violation. The next raw offset advances by the **requested window bounded to the reported
  total**, never by the returned hit count, so consecutive pages neither duplicate nor skip a
  candidate when the index drops one of its own entries. Rejecting short pages was the known-bad
  alternative: one unusable index entry would permanently collapse whole-set search for that query
  into the ordinary list under a misleading "temporarily unavailable" notice.
- Response validation still rejects null, contradictory, truncated, degraded, wrong-axis,
  wrong-query, oversized, over-full, and total-overflowing Memories pages. Malformed, duplicate,
  forbidden, missing, and status-filtered hits are never backfilled or disclosed.
- Hydration is cancellation-aware and bounded to `TenantQueryGateway.MaximumHydrationConcurrency`
  (8) concurrent authorized reads. Forbidden and not-found candidates disappear silently; operational
  loss yields generic partial state when verified rows remain and ordinary authorization-safe list
  fallback otherwise.
- **Cursor-failure containment is a two-set decision, not a blanket.** The surfacing set —
  `OutOfMemoryException`, `NullReferenceException`, `ObjectDisposedException`, `ArgumentNullException`
  — is excluded **before** any base-type match, because `ObjectDisposedException` derives from
  `InvalidOperationException` and `ArgumentNullException` derives from `ArgumentException`, both of
  which are contained base types. The contained set is the codec's untrusted-input failure modes:
  cryptographic, format, JSON, arithmetic, not-supported, and the argument/state failures the codec
  contract itself raises for a malformed scope or position. Contained decode failures force raw page
  zero (the failed decode's out value is never trusted); contained encode failures degrade to the
  ordinary list. A dead DataProtection provider looks like a dead provider, not like a tampered
  cursor. The same exclusion is applied in the search-availability predicate, so a surfacing defect
  raised while protecting the next cursor escapes rather than being reported as an unavailable index.
- Authoritative search that degrades to the ordinary list emits **exactly one** reason-code-only
  diagnostic (`search-index-unavailable`, `search-response-invalid`,
  `search-cursor-protection-unavailable`, `search-hydration-unavailable`) so a dead key ring stays
  distinguishable from a healthy index. Every failure path records a reason code and funnels through a
  single fallback call, so one load can never emit the signal twice and a failure inside the ordinary
  list cannot re-enter the fallback. **The signal is not emitted from the decode catch:** a contained
  cursor failure whose forced page-zero retry then succeeds authoritatively degraded nothing. No
  cursor, offset, query, or tenant id reaches that sink.
- Cursor invalidation is threaded through the fallback path, so protected search history is cleared
  even when the same load also loses Memories. When the invalidation instead lands on a terminal
  `Error`/`Unauthorized` surface — which cannot carry a notice — the clearing **and** its notice are
  withheld together as a pending decision and delivered on the next renderable load, so protected
  history is never destroyed on a surface that cannot explain it.
- Search cursors use the dedicated DataProtection-backed codec with its own purpose, seven fixed-size
  scope fields, and scoped server-circuit paging state. Tampering, cross-user, wrong-scope, key, and
  index-shrink invalidation (including the `offset == shrunken total` equality boundary) restart once
  at raw page zero with safe localized recovery. Ordinary fallback paging keeps independent history
  and recovery.
- Search cursors and raw offsets remain absent from URLs, DOM, browser storage, clipboard,
  diagnostics, and default `HttpClient` logs. Components never call Memories. Pre-existing unkeyed
  host `IQueryCursorCodec` registrations cannot replace the tenant-search purpose.
- The workspace resolves the scoped paging service as a **required** service, preserves protected
  page/history and its server-held page size only for a validated in-circuit detail return with
  matching normalized selection, anchor, and exact scope, and **suppresses restoration and its
  recovery notice on non-interactive (prerender) passes**, where the resolved scoped service is not
  the circuit's. The pending recovery decision is scope-bound and survives a superseding load for the
  same scope instead of being overwritten before it is read. A disposed workspace component issues no
  further loads, so it cannot retire a paging mode or clear protected history behind a surface no
  operator can see.
- **Paging identity lives beside paging position.** `TenantSearchPagingState.ActiveModeAuthoritative`
  holds the active mode in the same circuit-scoped service as the cursors it describes, because a
  tenant-detail return recreates the component while the circuit survives — a mode kept on the
  component would be lost exactly when the cursors are not. The crossing is therefore still detected
  after component recreation, and the retained protected cursor can never resume a deep page.
- **The crossing is resolved before the outgoing request is built.** Only the currently active mode
  may contribute a retained cursor, so a load that crosses the boundary is issued for the incoming
  mode's first page rather than against the stale cursor of the mode it is leaving. That is what makes
  the "paging restarted from the first page" notice honest about the load that carried it.
- Retiring a paging mode is never silent. In **both** directions the crossing emits the mapped,
  EN/FR-parity `Tenants.List.Notice.SearchPagingRestarted` notice.
- **Both** notice bars refuse to render without a mapped localized message and a stable non-empty
  `data-testid`, and the secondary bar is suppressed when it duplicates the primary one. Both share
  exactly **one** polite live region, and that region renders **unconditionally** on the tenant-list
  view: a live region inserted in the same render that first populates it is routinely not announced,
  so only the bars inside it are conditional.
- A malformed member collection degrades **identically** on the search and ordinary list paths: both
  keep the authorized row with unknown counts and raise the same `IsDegraded` /
  `RowEnrichmentUnavailable` signal with `Unknown` freshness. The search path carries this on a
  distinct enrichment-degraded flag rather than the operational-failure flag, so it can never trigger
  the ordinary-list fallback, and the same payload can never produce a degraded banner on one surface
  and a clean `Ready` surface on the other.
- An authoritative search page with no visible rows renders the dedicated `SearchPageEmpty` state
  rather than the filtered-empty copy, because a fully or partly omitted index window is an
  index/authorization outcome, never a verdict on the operator's filters. Its copy **splits by
  `HasMore`**: a non-final page may say later pages of the same search can still contain results; a
  final page states in distinct EN/FR copy that no further pages remain.
- Diagnostics carry an `EventId` and are emitted **after** the fallback resolves, so the message states
  the outcome the operator actually received: `AuthoritativeTenantSearchDegraded` (1901) when a usable
  ordinary list was served, `AuthoritativeTenantSearchAndListUnavailable` (1902) when it was not.

## Exact automated verification

All commands ran from the repository root on 2026-07-26.

| Command | Result |
|---|---|
| `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` | Passed — **0 warnings, 0 errors** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantSearchCursorTests -class Hexalith.Tenants.UI.Tests.Components.TenantListSurfaceTests -class Hexalith.Tenants.UI.Tests.State.TenantWorkspaceStateTests -class Hexalith.Tenants.UI.Tests.TenantsWorkspaceTests -class Hexalith.Tenants.UI.Tests.TenantsUiCompositionTests -class Hexalith.Tenants.UI.Tests.DomainUiFluentConformanceTests` | Passed — **396 total, 0 failed, 0 skipped** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Components.TenantDetailSurfaceTests` | Passed — **56 total, 0 failed, 0 skipped** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.LocalizerDoubleParityTests` | Passed — **2 total, 0 failed, 0 skipped** |
| `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` | Passed — **1,145 total, 0 failed, 0 skipped** |
| `dotnet build samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false && samples/Hexalith.Tenants.Sample.Tests/bin/Release/net10.0/Hexalith.Tenants.Sample.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Sample.Tests.Handlers.MemoriesSearchIndexEventPublisherTests` | Passed — build **0 warnings, 0 errors**; tests **7 total, 0 failed, 0 skipped** |
| `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` | Passed — **0 warnings, 0 errors** |
| `git diff --check` | Passed — no whitespace errors |
| Trailing-whitespace scan of the untracked evidence artifact and `LocalizerDoubleParityTests.cs` | Passed — no matches |

No transcript artifact of intermediate red-phase runs is retained, so any statement about failing
intermediate runs is session context, not independently auditable evidence. The reproducible final
commands above are authoritative.

## Coverage gates (observed passing)

Every test named below exists verbatim in the suite and was observed passing in the runs recorded
above.

| Gate | Where it is observed |
|---|---|
| A short non-final index page stays authoritative and its next page neither repeats nor skips a candidate | `List_search_keeps_a_short_non_final_page_authoritative_and_advances_by_the_requested_window`, `List_search_short_page_sequence_neither_repeats_nor_skips_a_candidate`, `List_search_accepts_a_valid_short_final_page_at_a_positive_offset` |
| An over-full or total-overflowing index page is still rejected | `List_search_rejects_each_unsafe_response_invariant_without_hydrating_index_candidates` (`too-many-results`, `page-exceeds-total`) |
| Each contained codec exception type is contained inside the gateway | `List_search_contains_every_cursor_decode_exception_type_as_page_one_invalidation` and `List_search_contains_every_cursor_encode_exception_type_and_degrades_to_the_ordinary_list` (cryptographic, format, invalid-operation, argument, overflow, json, not-supported) |
| Each surfacing type — including `ObjectDisposedException` and `ArgumentNullException`, which derive from contained bases — still escapes the gateway | `List_search_surfaces_every_programming_defect_from_the_cursor_codec_instead_of_mislabelling_it` and `List_search_surfaces_every_programming_defect_raised_while_protecting_the_next_cursor` (out-of-memory, null-reference, object-disposed, argument-null) |
| Cursor invalidation clears protected history on the fallback path too | `List_search_clears_protected_history_when_cursor_invalidation_and_a_memories_outage_share_one_load`, `List_search_clears_protected_history_when_the_same_load_falls_back_renderably` |
| Cursor invalidation that lands on a terminal error surface withholds both the clearing and its notice, and delivers them together on the next renderable load | `List_search_withholds_protected_history_clearing_when_the_fallback_ends_terminally`, `Invalidation_on_a_terminal_surface_withholds_the_clearing_and_its_notice_together` |
| Both notice bars refuse to render an unmapped reason | `Unmapped_notice_reasons_never_render_a_blank_or_unaddressable_message_bar` (sets both `Notice` and `PagingNotice`), `Secondary_notice_equal_to_primary_renders_only_one_live_announcement` |
| The shared polite live region pre-exists its content | `Notice_live_region_exists_before_any_notice_populates_it` |
| A load crossing the authoritative/fallback boundary leaves an honest Previous affordance, no reactivatable retained cursor, and was itself issued for the first page | `Crossing_the_authoritative_fallback_boundary_leaves_an_honest_previous_affordance` (both directions, asserts the crossing request carries no incoming-mode cursor) |
| A crossing is detected after the workspace component is recreated by a tenant-detail return | `Crossing_is_still_detected_after_a_detail_return_recreates_the_workspace_component`, `Scoped_paging_keeps_the_active_mode_beside_the_cursors_it_describes` |
| Every stubbed localizer value equals the shipped `TenantsResources.resx` value for that key, and the gate can fail | `Every_localizer_double_returns_the_exact_shipped_resource_value_for_every_stubbed_key`, `Localizer_double_parity_gate_can_fail` |
| Every new resource key is pinned in EN and in an explicitly resolved `fr` culture | `Authoritative_search_resources_resolve_complete_english_and_french_copy` (`Tenants.List.Notice.SearchPagingRestarted`, `Tenants.List.State.SearchPageEmpty.Title`, `.Message`, `.FinalMessage`); the parity gate additionally requires every stubbed key to exist in the `fr` resource set without falling back to its parents |
| Both the non-final and final empty-search-page messages are asserted | `Empty_search_page_copy_promises_later_results_only_while_paging_continues` |
| Every surface state this story introduces is rendered by a test, and no test still waits on a superseded selector | `Workspace_renders_each_distinct_list_state` (theory row `SearchPageEmpty` / `tenants-list-search-page-empty` / `status` / `polite`), `Search_returning_no_matches_renders_the_search_page_empty_surface`, `Authoritative_search_next_and_previous_use_only_server_held_search_cursors`, `Empty_final_authoritative_page_keeps_previous_only_paging_available` |
| A malformed member collection degrades identically on both hydration paths | `Malformed_member_collection_degrades_identically_on_both_hydration_paths`, `List_search_keeps_a_null_member_tenant_visible_with_unknown_counts_like_the_ordinary_list`, `List_ordinary_path_degrades_safely_for_the_same_null_member_detail_shape` |
| The degradation signal is emitted only on a load that resolved to the ordinary list, and never twice for one load | `Search_diagnostics_only_ever_emit_support_safe_reason_codes` (a contained decode failure whose page-zero retry succeeds authoritatively emits nothing; an encode failure that does resolve to the ordinary list emits exactly one), `Search_degradation_signal_reports_that_the_ordinary_list_was_also_unavailable` |
| Support-safety assertions are placed where disclosure is possible and are proven capable of failing | `Search_paging_never_reaches_browser_storage_and_never_puts_cursors_in_interop_or_the_url` asserts rendered markup, the canonical URL, and every JS-interop invocation, each with a control case in which the material genuinely appears; `Embedded_ui_module_emits_no_default_memories_http_logs_that_could_carry_query_or_offset` and `Standalone_ui_host_resolves_the_same_server_side_search_composition` each observe a control client with default logging before asserting the Memories client logs nothing. No `ToString()` substring check is offered as support-safety evidence; the diagnostic surfaces are pinned by equality in `Request_snapshot_and_paging_diagnostics_never_expose_cursors_or_search_material`, `Scoped_paging_diagnostics_omit_scope_cursor_and_reconstructable_page_depth`, and `Snapshot_diagnostics_are_pinned_and_disclose_no_row_identity` |
| Host composition proven by resolving the built provider, not by scanning source text | `Tenant_search_composition_is_purpose_isolated_scoped_and_server_configured` (single Data Protection provider, purposes vary only), `Standalone_ui_host_resolves_the_same_server_side_search_composition` (`WebApplicationFactory<Program>`) |
| Hydration concurrency bound is read from production, and a changed limit fails rather than blocks | `List_search_bounds_maximum_page_hydration_concurrency_and_keeps_deterministic_order` asserts `TenantQueryGateway.MaximumHydrationConcurrency` with fixed per-call delays instead of a barrier |
| Prerender passes neither restore nor report retained protected paging | `Non_interactive_prerender_pass_neither_restores_nor_reports_retained_protected_paging` |
| The pending recovery decision is observable across loads | `Pending_recovery_notice_survives_a_superseding_load_for_the_same_search_scope`, `Transient_error_on_a_detail_return_still_reports_recovery_on_the_same_scope_retry` |

## Acceptance-criterion verdict

| Criterion | Verdict | Current evidence |
|---|---|---|
| Candidate-only Memories input; authorized fields; status/sort/raw accounting; no disclosure | **verified** | Gateway tests cover exact request shape, malformed/duplicate/forbidden/not-found/visible candidates, null and mismatched hydration, malformed member collections on both hydration paths, stale index fields, six sort combinations, ordinal tie-breaking, raw-offset accounting without backfill, current/stale aggregation, and `Pending=Unknown`. |
| Short non-final page stays authoritative, advances by the requested window, no spurious fallback | **verified** | See the coverage-gate table's first row; the short page renders its authorized rows, keeps `Notice = None`, and produces a cursor at the window boundary. |
| Protected server-held paging and seven-field identity reset | **verified for current sources** | Cursor/state/component tests cover all scope fields, fixed-size hashes, Next/Previous in authoritative and fallback modes, page-two identity reset, host-codec purpose isolation, absence of cursors in URL/DOM/storage/interop, support-safe formatting without page depth, and suppressed default Memories HTTP logging in both host compositions. |
| Paging crossing the authoritative/fallback boundary stays honest, including the load that carries the notice | **verified** | The boundary theory drives both directions, asserts a disabled Previous, asserts both retained cursors are cleared, and asserts the crossing request carries no cursor for the mode it enters. |
| Crossing survives component recreation by a tenant-detail return | **automated verified; browser blocked** | The mode is held in the circuit-scoped paging service and asserted directly, and the round-trip component test proves the crossing is detected and the retained protected cursor never resumes a deep page. Real authenticated browser proof is blocked below. |
| Cross-user/tampering/key/scope/index-shrink recovery, including terminal-surface deferral | **verified** | Cursor and gateway tests reject cross-user and wrong-scope reuse, discard unsafe failed-decode out values, contain seven codec exception types on both decode and encode while four surfacing defect types still escape, cover the greater-than and equality shrink boundaries, request raw page zero exactly once, clear only the applicable history, withhold clearing plus notice on terminal surfaces, and render only mapped, deduplicated polite notices without exposing any protected value or rejection reason. |
| Final versus non-final empty-search-page copy | **verified** | Distinct EN/FR strings are pinned in both cultures and both messages are rendered by the surface test. |
| Symmetric malformed-member degradation | **verified** | Both hydration paths are driven with the identical payload and their `Kind`, `IsDegraded`, `Reason`, `Freshness`, and count-known state are asserted equal. |
| Partial hydration and ordinary-list fallback stay honest | **verified** | Tests cover null/not-modified/mismatched/degraded/stale detail shapes, each search failure family, silent forbidden/not-found drops, verified-row partial rendering, combined outage plus fallback-cursor recovery notices, and fallback page-local rather than whole-index semantics. |
| Blank search preserves ordinary cursor list with zero Memories calls | **verified** | Gateway and component tests prove canonical blank input issues no Memories request and continues unchanged ordinary cursor paging without loaded-page search imitation. |
| In-circuit detail round trip restores protected paging or safely restarts | **automated verified; browser blocked** | bUnit tests require matching normalized selection/anchor and exact scope, retain page two, Previous history, and page size 50 across component disposal/recreation, keep cursor/page size out of the return URL, reset direct or invalid-context visits, cover missing retained state restarting page one with an identity-bound polite localized notice, and suppress that notice on the prerender pass. Real authenticated browser focus proof is blocked below. |
| Rapid changes cancel obsolete work; maximum page is bounded and deterministic | **verified** | Component tests observe cancellation during rapid query replacement. Gateway tests propagate caller cancellation, exercise 100 hits, assert the production concurrency constant, and retain deterministic output order. |
| Dated evidence covers runtime, localization, responsive, accessibility, and support safety honestly | **partial with exact blockers** | Exact EN/FR resource strings and parity, the shipped-copy localizer gate with an explicit `fr` resolution and a self-test proving it can fail, stable selectors, Fluent controls, live regions, responsive/forced-colors/reduced-motion source rules, disclosure-capable assertions with control cases, and all specified automated lanes pass. Authenticated runtime/browser and human NVDA evidence remain open below. |

## Cross-tenant negative proof

The focused suite creates a protected cursor for one authenticated user and attempts reuse under a
different authenticated user. Decode is rejected, the gateway queries raw offset zero exactly once,
the protected page/history is cleared, and only `SearchRefreshed` is surfaced. The tests also verify
that the cursor, raw offset, user identity, candidate identity, source URI, index fields, and
cryptographic failure text do not appear in rendered markup, canonical URLs, JS-interop invocations
(including browser-storage calls), the reason-code log sink, or default Memories HTTP logging — each
alongside a control case in which the corresponding channel does carry a value that is present.

## External evidence blockers

- **MEMORIES-RUNTIME-1.9 — owner: Tenant platform runtime operator — OPEN.** No authenticated
  AppHost/Memories runtime session was available for this re-derivation. Consequence: a live
  authenticated Memories-to-Tenants search, cross-tenant denial trace, clean network/telemetry
  inspection, and runtime key-rotation recovery are not claimed. Reopen trigger: restore/build the
  AppHost successfully with its configured dependencies, start all required resources, execute
  authorized and cross-tenant fixtures, and retain dated resource, trace, log, and network evidence.
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
