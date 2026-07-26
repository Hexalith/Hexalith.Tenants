---
title: 'Story 1.9: Authoritative Memories Search with Protected Paging'
type: 'feature'
created: '2026-07-21'
status: 'in-review'
baseline_revision: '85838fbbb4efcd131a44d4ac4535110b1a9d3217'
review_loop_iteration: 1
followup_review_recommended: true
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md'
warnings:
  - oversized
---

<intent-contract>

## Intent

**Problem:** Non-empty tenant search currently ignores the term, reloads the ordinary cursor list, and reports search unavailable. Operators need whole-set Memories search without allowing indexed, hidden, or cursor data to become authoritative or client-readable.

**Approach:** Use server-side `MemoriesClient.SearchAsync` only for ordered `tenant:{id}` candidates, hydrate and authorize every survivor through the existing Tenants query seam, and page raw hits with the platform-protected cursor codec held only in the Blazor server circuit. Preserve ordinary cursor-list fallback and Story 1.10's honestly `unknown` freshness boundary.

## Boundaries & Constraints

**Always:** Count every raw hit toward the next offset; keep first-occurrence candidate order before authoritative filtering; recheck exact status and deterministically sort hydrated visible rows with ordinal tenant-id tie-breaking; bind cursors to authenticated user, canonical search, status, sort, direction, and page size; treat every decode failure as invalid/expired/invalidation recovery; use Fluent/FrontComposer, stable selectors, EN/FR whole strings, and support-safe state. Forbidden/not-found candidates are silently indistinguishable from absent results; only operational hydration loss may add a generic degraded state.

**Block If:** The consumed Memories package cannot perform the specified syntactic offset search or structured status filtering, the consumed EventStore package cannot provide `IQueryCursorCodec`/`QueryCursorScope`, or authorization-correct hydration requires a new public/platform contract or any edit under `references/`.

**Never:** Render or serialize Memories content, scores, attributes, source URIs, raw offsets, protected search cursors, credentials, cryptographic failures, or dropped identities; put search cursors in URLs/DOM/browser storage; call Memories from a component/browser; hand-roll cryptography; backfill dropped hits; add a tenant-search endpoint; imitate whole-set search by filtering the ordinary loaded page; claim direct-REST provenance or current freshness before Story 1.10.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Whole-set search | Canonical non-empty query, optional exact status, sort, direction, page size | One syntactic `tenants-index` request; valid unique candidates are authorized, hydrated, status-rechecked, and page-sorted from Tenants truth | Unsafe index fields never enter the snapshot |
| Sparse raw page | Malformed, duplicate, forbidden, missing, and valid hits | Consume all raw hits, retain only authorized hydrated rows, do not backfill, protect the raw next offset | Hidden/not-found drops create no distinguishable notice or count; operational hydration loss is generically degraded |
| Cursor misuse | Tampered, cross-user, wrong-scope, expired, rotated-key, or invalid cursor | Reject reuse, query raw offset zero exactly once, clear search history, announce localized list refresh | Expose no failure reason, scope, cursor, or candidate data |
| Index lag | Indexed fields disagree with hydrated tenant | Render only current authorized Tenants fields and preserve returned freshness state | Stale index data cannot resurrect or alter a row |
| Search outage | Timeout, invalid response, unavailable service, or `Degraded=true` | Run the ordinary authorization-safe list and show the non-blocking search-unavailable notice | Search availability never blocks tenant browsing |
| Empty search | Empty or whitespace-only canonical state | Use unchanged Story 1.2 list paging with zero Memories calls | No loaded-page search filtering |

</intent-contract>

## Code Map

- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` -- current BFF gateway and deliberate search-unavailable fallback; existing detail reads are the authoritative hydration seam.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantSearchCursorScopes.cs`, `ITenantSearchCursorCodec.cs`, `TenantSearchCursorCodec.cs`, and `TenantSearchCursorPosition.cs` -- fixed-size collision-safe scope values and a purpose-isolated wrapper over the platform codec.
- `src/Hexalith.Tenants.UI/State/TenantList/` -- request, snapshot, reason, and canonical URL state; search cursors must remain outside URL state.
- `src/Hexalith.Tenants.UI/State/TenantList/TenantSearchPagingState.cs` -- scoped server-circuit paging continuity across workspace/detail navigation.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- cancellation, server-held search paging, notices, focus, and list rendering.
- `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs` and `Program.cs` -- Memories and DataProtection-backed query-cursor registration for embedded and standalone hosts.
- `tests/Hexalith.Tenants.UI.Tests/` -- gateway, state, outer-component, localization, accessibility, and support-safety evidence.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantSearchCursorScopes.cs`, `src/Hexalith.Tenants.UI/Services/Gateways/ITenantSearchCursorCodec.cs`, `src/Hexalith.Tenants.UI/Services/Gateways/TenantSearchCursorCodec.cs`, and `src/Hexalith.Tenants.UI/Services/Gateways/TenantSearchCursorPosition.cs` -- build the seven-field scope with fixed-length SHA-256 values for unbounded user/search inputs; wrap a dedicated `QueryCursorCodec` purpose so pre-existing unkeyed `IQueryCursorCodec` registrations cannot replace search protection; accept only non-negative canonical raw offsets and do not invent a TTL.
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListRequest.cs`, `src/Hexalith.Tenants.UI/State/TenantList/TenantListSnapshot.cs`, and `src/Hexalith.Tenants.UI/State/TenantList/TenantListReason.cs` -- represent search paging/recovery and operational partial hydration, and provide support-safe diagnostic formatting that omits ordinary/search cursors and index material.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` -- replace the quarantine branch with exact Memories request/response validation, including axes/truncation/null/count invariants; recover once at raw page zero when a formerly valid offset exceeds a shrunken result set; parse/dedupe raw hits; hydrate with bounded concurrency while preserving raw order; silently drop forbidden/not-found candidates; reject null/mismatched details; preserve authoritative status/freshness and `Pending=Unknown` absent proof; sort with an ordinal tenant-id tie-breaker; protect the consumed raw offset; and sanitize operational partial/fallback behavior while propagating caller cancellation.
- `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs` and `src/Hexalith.Tenants.UI/Program.cs` -- register the dedicated search codec and scoped paging state in both host compositions; retain server-side Memories endpoint/token configuration and call `RemoveAllLoggers()` on its `IHttpClientBuilder` so raw query/offset values cannot enter default HttpClient logs.
- `src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs` -- keep canonical search/status/sort state and ordinary cursors, but reject/remove any `cursor` query value whenever non-empty search is active.
- `src/Hexalith.Tenants.UI/State/TenantList/TenantSearchPagingState.cs` and `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` -- retain search next/previous state in a scoped circuit service across detail navigation, reset it on every query-identity change, recover honestly when retained state is invalid, cancel/dispose obsolete loads, pass cancellation through Memories and hydration, base semantics on `snapshot.IsAuthoritativeSearch` so fallback never claims whole-index behavior, and preserve Fluent pager, focus, announcements, truth markers, safety columns, and selectors.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx` -- add exact-parity whole strings for partial search and refreshed search while retaining the existing search-unavailable fallback copy.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs` and `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantSearchCursorTests.cs` -- cover the full matrix, exact request, response invariant failures, null/mismatched/degraded/stale hydration, raw-hit/no-backfill accounting, authoritative pending/freshness/status, every sort direction, all scope mismatches, pre-existing codec isolation, cross-user denial, key invalidation, index-shrink page-one recovery, each fallback exception family, caller cancellation, bounded concurrency, zero-call blank search, and support-safe diagnostic/logging behavior.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`, `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`, `tests/Hexalith.Tenants.UI.Tests/State/TenantWorkspaceStateTests.cs`, `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`, `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`, and `tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs` -- exercise authoritative and fallback Next/Previous paging, query-identity reset after page two, server-only cursor state, detail round-trip continuity, sparse/partial/empty/fallback/recovery states, fallback-versus-search semantics, EN/FR parity, keyboard/live-region behavior, responsive safety columns, stable selectors, and support-safety scans.
- `_bmad-output/implementation-artifacts/story-1-9-authoritative-memories-search-with-protected-paging-evidence-2026-07-21.md` and `tests/test-summary.md` -- record immutable revisions, per-criterion evidence, exact commands/results, cross-tenant negative proof, and honest browser/NVDA or runtime blockers.

**Acceptance Criteria:**
- Given a non-empty search with lagging, duplicate, malformed, hidden, and visible candidates, when the tenant list renders, then every visible field comes from an authorized Tenants hydration, exact status and requested stable sort apply within the page, all raw hits advance paging without backfill, and no output reveals hidden candidates or index payload data.
- Given active search paging, when Next or Previous is invoked or any user/query/status/sort/direction/page-size scope changes, then paging uses only server-held protected cursors, scope changes restart page one, and no search cursor or offset appears in URL, DOM, clipboard, logs, telemetry, or serialized client state.
- Given another user, tampering, scope mismatch, expiry, or key invalidation, when a search cursor is reused, then page one is requested exactly once, paging history is cleared, and a polite localized list-refreshed notice appears without unsafe diagnostics.
- Given Memories or authoritative hydration is operationally degraded, when the surface resolves, then authorized rows remain honest where possible, otherwise the ordinary list stays usable under the localized non-blocking fallback, and freshness remains `unknown` unless the existing Tenants seam proves otherwise.
- Given empty search, when the list loads, then Story 1.2 authorization-safe cursor paging runs unchanged with no Memories request and no page-local whole-set imitation.
- Given an authoritative search page is opened before tenant detail, when the operator returns within the same server circuit, then the protected page/history and safe focus context are restored without putting cursor material in the URL; if retained paging cannot be validated, page one loads with a polite localized refresh notice.
- Given rapid query changes or a maximum-size raw page, when search loads, then obsolete work is canceled, hydration concurrency is bounded, authoritative result order remains deterministic, and canceled caller requests are not converted into fallback results.
- Given the Story 1.9 evidence lanes, when focused gateway/cursor, cross-user denial, bUnit, localization, responsive, accessibility, support-safety, and relevant runtime checks run, then every criterion has dated passing evidence or an exact owner, consequence, and reopen trigger; Story 1.8 history and Story 1.10 work are not treated as waivers.

## Spec Change Log

### 2026-07-21 — Review repair 1
- Triggering findings: the first four-layer review found that component-owned paging lost context across detail navigation; unkeyed `IQueryCursorCodec` registration could silently defeat the intended search purpose; serial uncancelled hydration could turn a 100-hit search into a request storm; cursor/raw-offset diagnostic and default HttpClient logging channels were not closed; pending state was asserted as `None` without proof; unbounded scope material could exceed cursor limits; index shrink, contradictory/truncated/null responses, and detail-shape failures were underspecified; and fallback semantics plus the verification matrix did not cover the changed branches precisely enough.
- Amendment: expanded the Code Map, execution tasks, acceptance, verification coverage, and design notes to require a dedicated codec wrapper, fixed-size scope values, scoped circuit paging continuity, bounded/cancellable hydration, support-safe diagnostic formatting and default HTTP-log suppression, conservative pending truth, explicit response/invalidation recovery, snapshot-driven copy, and named outer-surface tests.
- Known-bad state avoided: valid search cursors being protected by the wrong service purpose, stale/unsafe paging and diagnostic leakage, page-one loss after detail navigation, false no-pending claims, unbounded serial work, unrelated ordinary-list fallback after normal index shrink, and a green suite that missed those behaviors.
- KEEP: Memories remains index-only; candidate order, raw-hit accounting, no backfill, silent authorization drops, authoritative Tenants hydration/status/freshness, seven-field cursor scope, server-only search cursor state, ordinary-list fallback, Fluent/FrontComposer composition, EN/FR parity, stable selectors, and Story 1.10's unknown-freshness boundary must survive re-derivation.

## Review Triage Log

### 2026-07-21 — Review pass
- intent_gap: 0
- bad_spec: 23: (high 7, medium 16, low 0)
- patch: 0
- defer: 0
- reject: 5: (high 0, medium 4, low 1)
- addressed_findings:
  - `[medium]` `[bad_spec]` Added outer fallback Next/Previous paging coverage.
  - `[medium]` `[bad_spec]` Added query-identity reset coverage after protected page-two state exists.
  - `[medium]` `[bad_spec]` Named each handled search failure family and caller-cancellation behavior for verification.
  - `[medium]` `[bad_spec]` Required every malformed pagination-response guard to be observed.
  - `[medium]` `[bad_spec]` Required null, not-modified, mismatched, degraded, stale, and failed detail hydration coverage.
  - `[medium]` `[bad_spec]` Required all primary sort columns, directions, and tenant-id tie-breakers to be asserted.
  - `[medium]` `[bad_spec]` Required rendered active-search and fallback semantics/accessibility copy tests.
  - `[medium]` `[bad_spec]` Moved protected paging continuity from component-only fields into scoped circuit state across detail navigation.
  - `[high]` `[bad_spec]` Required `Pending=Unknown` when authoritative search hydration has no pending-state proof.
  - `[medium]` `[bad_spec]` Required null Memories elements to degrade safely.
  - `[medium]` `[bad_spec]` Required null authoritative members to degrade safely.
  - `[medium]` `[bad_spec]` Required index-shrink offsets to recover once from page zero with an honest notice.
  - `[medium]` `[bad_spec]` Required fixed-length scope values for unbounded authenticated-user and search inputs.
  - `[high]` `[bad_spec]` Replaced the unkeyed shared codec registration with a dedicated purpose-isolated search wrapper.
  - `[medium]` `[bad_spec]` Required direct gateway inputs to be safely canonicalized or rejected before Memories/cursor use.
  - `[high]` `[bad_spec]` Closed raw Memories offset/query exposure through default HttpClient logging.
  - `[high]` `[bad_spec]` Required cursor-bearing request and snapshot diagnostic formatting to omit cursor values.
  - `[high]` `[bad_spec]` Required bounded authoritative hydration rather than up to 100 serial network calls.
  - `[high]` `[bad_spec]` Required cancellation of obsolete UI loads and propagation of caller cancellation.
  - `[medium]` `[bad_spec]` Required contradictory Memories axis/index metadata to fail safe.
  - `[medium]` `[bad_spec]` Required truncation/omission metadata to preserve raw-consumption guarantees or fail safe.
  - `[medium]` `[bad_spec]` Made fallback semantics depend on authoritative-search state rather than the non-empty query alone.
  - `[high]` `[bad_spec]` Narrowed evidence claims and required runtime-relevant sentinel guards for cursor/index/log/telemetry safety.

### 2026-07-26 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 17: (high 1, medium 11, low 5)
- defer: 1: (high 0, medium 1, low 0)
- reject: 6: (high 1, medium 5, low 0)
- addressed_findings:
  - `[medium]` `[patch]` Forced every unsuccessful search-cursor decode to raw page zero regardless of the codec's out value.
  - `[medium]` `[patch]` Recovered the equality boundary when a positive retained offset lands on an empty shrunken result set.
  - `[medium]` `[patch]` Restricted retained paging restoration to an exact, normalized detail-return context instead of fresh same-scope visits.
  - `[medium]` `[patch]` Bound pending recovery notices to the active scope and cleared them across cancellation, identity changes, disposal, and terminal results.
  - `[low]` `[patch]` Suppressed secondary notice rendering when no mapped localized message and stable test id exist.
  - `[medium]` `[patch]` Suppressed duplicate primary and secondary notice ids and polite announcements.
  - `[high]` `[patch]` Removed exact search/fallback history counts from support diagnostics because page depth reconstructs protected raw offsets.
  - `[medium]` `[patch]` Corrected purpose-isolation evidence to use one Data Protection provider and vary only the codec purposes.
  - `[medium]` `[patch]` Added positive nonzero-cursor coverage for a valid short final Memories page.
  - `[medium]` `[patch]` Added previous-only pager coverage for an empty final authoritative page.
  - `[medium]` `[patch]` Added outer workspace coverage proving recovery clears only the applicable paging mode.
  - `[medium]` `[patch]` Proved protected Previous history survives detail navigation and component recreation.
  - `[low]` `[patch]` Corrected the evidence inventory to distinguish workflow-owned spec metadata edits from implementation edits.
  - `[low]` `[patch]` Replaced the inaccurate no-public-contract claim with the exact public UI state-surface addition and unchanged published/backend contracts.
  - `[low]` `[patch]` Added a separate whitespace check for the untracked evidence artifact.
  - `[low]` `[patch]` Qualified red-phase history as an implementation-session report rather than an auditable retained artifact.
  - `[medium]` `[patch]` Added direct-visit and validated return-context tests so retained hidden paging cannot attach to an unrelated navigation.

## Design Notes

Search state remains canonical in the URL, but search paging state does not. `TenantWorkspaceState.Cursor` continues to serve ordinary list/My Tenants/users paging only; active search owns scoped server-circuit state that survives in-circuit detail navigation and canonicalizes away incoming `cursor=` values. The search wrapper constructs its own `QueryCursorCodec` purpose instead of competing for the host's unkeyed `IQueryCursorCodec`; fixed-size SHA-256 scope values bind unbounded canonical user/search inputs without exposing them or risking oversized cursors. Platform codec rejection covers tampered, mismatched, expired, and invalidated inputs; this story does not invent a wall-clock TTL absent from the platform contract. Hydration uses a named maximum concurrency while retaining the raw candidate ordinal for final stable ordering, and cancellation never becomes a search-unavailable result.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings/errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantQueryGatewayTests -class Hexalith.Tenants.UI.Tests.Services.Gateways.TenantSearchCursorTests -class Hexalith.Tenants.UI.Tests.Components.TenantListSurfaceTests -class Hexalith.Tenants.UI.Tests.State.TenantWorkspaceStateTests -class Hexalith.Tenants.UI.Tests.TenantsWorkspaceTests -class Hexalith.Tenants.UI.Tests.TenantsUiCompositionTests -class Hexalith.Tenants.UI.Tests.DomainUiFluentConformanceTests` -- expected: all focused tests pass.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` -- expected: full UI suite passes.
- `dotnet build samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj --configuration Release --no-restore -warnaserror -m:1 -nr:false && samples/Hexalith.Tenants.Sample.Tests/bin/Release/net10.0/Hexalith.Tenants.Sample.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Sample.Tests.Handlers.MemoriesSearchIndexEventPublisherTests` -- expected: index handoff tests pass.
- `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -warnaserror -m:1 -nr:false` -- expected: zero warnings/errors.

**Manual checks (if no CLI):**
- Retain authenticated EN/FR browser evidence for whole-set next/previous paging, page-one recovery, partial/fallback states, keyboard focus/announcements, narrow widths, forced colors, reduced motion, and clean console/network output; record human NVDA or Memories-runtime proof as blocked unless a dated session is available.

## Auto Run Result

Status: done

### Summary

Implemented authoritative whole-set tenant search using Memories only for ordered candidates, authoritative Tenants hydration for every rendered row, Data Protection-backed server-circuit paging, honest sparse/partial/fallback behavior, deterministic status/sort handling, bounded cancellation-aware hydration, and support-safe EN/FR UI states. The review pass hardened invalid-cursor/index-shrink recovery, detail-return continuity, paging-mode isolation, notice behavior, and diagnostic safety.

### Files Changed

- `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` — workflow state, review triage, verification, and final run result.
- `_bmad-output/implementation-artifacts/deferred-work.md` — one pre-existing accessible-label mismatch deferred for separate attention.
- `_bmad-output/implementation-artifacts/story-1-9-authoritative-memories-search-with-protected-paging-evidence-2026-07-21.md` — dated acceptance evidence, exact gates, negative proof, and external blockers.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` — protected paging continuity, cancellation, scoped recovery, honest notices, and sparse pager behavior.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs` — validated Memories search, authoritative hydration, bounded concurrency, sorting, recovery, and ordinary-list fallback.
- `src/Hexalith.Tenants.UI/State/TenantList/TenantListSnapshot.cs` — separate support-safe paging notice state.
- `src/Hexalith.Tenants.UI/State/TenantList/TenantSearchPagingState.cs` — scoped search/fallback history with mode-specific recovery and non-reconstructable diagnostics.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs` — outer paging, continuity, cancellation, notice, and safety coverage.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs` — full response, hydration, sort, cursor, concurrency, cancellation, and fallback matrix.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantSearchCursorTests.cs` — scope, purpose, invalidation, and paging-state evidence.
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs` — host composition, purpose isolation, logging guards, and EN/FR copy.
- `tests/test-summary.md` — Story 1.9 test-evidence addendum.

### Review Findings

- Patches applied: 17 (high 1, medium 11, low 5).
- Items deferred: 1 pre-existing accessible-name mismatch recorded in `deferred-work.md`.
- Items rejected: 6 duplicate, already-disclosed, or non-actionable findings.
- Follow-up review recommendation: true. Patch score = `3 × 11 + 1 × 5 = 38`; the pass also contained one high-severity patch.

### Verification

- UI Release build: passed with 0 warnings and 0 errors.
- Exact seven-class focused UI executable: 349 passed, 0 failed, 0 skipped.
- Full UI executable: 1,096 passed, 0 failed, 0 skipped.
- Memories index-handoff lane: 7 passed, 0 failed, 0 skipped after a warning-clean Release build.
- Full `Hexalith.Tenants.slnx` Release build: passed with 0 warnings and 0 errors.
- `git diff --check`: passed.
- Untracked evidence whitespace check: expected content-diff exit 1 with no whitespace diagnostics.
- Matrix audit: every intent-contract row is covered by an enabled test in the passing focused/full lanes.

### Residual Risks

- Live authenticated AppHost/Memories runtime proof remains blocked by AppHost restore startup timeout and the local untrusted OpenSSL certificate; the evidence report records owner, consequence, logs, and reopen trigger.
- Authenticated EN/FR browser evidence and human NVDA evidence remain unavailable and are recorded with exact owners and reopen triggers.
- The pre-existing visible/accessibility status-label mismatch is deferred; it was not introduced by this review-repair diff.
