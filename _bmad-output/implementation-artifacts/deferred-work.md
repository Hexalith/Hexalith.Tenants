# Deferred Work

Updated: 2026-06-21 by Correct Course (deferred + pending work implementation).
Source proposals:
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19-deferred-work.md` (original triage),
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-21-deferred-and-pending-work-implementation.md` (this run).

This file is now a routing index. Original review detail remains in the source story/spec artifacts; open items here must point to a Tenants story, a FrontComposer owner handoff, an EventStore owner handoff, or a stale/resolved record.

## 2026-07-01 Correct Course — Deferred Work (pagination fail-closed + submodule doc handoffs)

Proposal: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-01-deferred-work-pagination-and-submodule-docs.md`
(scope = the global-administrator pagination correctness fix + the three cross-submodule doc handoffs;
incremental review; Administrator-approved, incl. explicit approval to edit the FrontComposer and
EventStore submodules for the doc-only handoffs). Classified **Minor** (Developer-executable). This run
picked up the highest-severity remaining Tenants-owned item — the >20-admin false-`Confirmed` fail-OPEN —
plus the documentation-only cross-submodule follow-ups; the rest of the open items are design-level (full
projection paging), IA/Product-blocked (the trio), or benign watch-items, and stay routed.

Resolved / documented this run:

- **Global-administrator pagination >20 admins — fail-OPEN CLOSED (full paging redesign still routed).**
  `GlobalAdministratorCorrectionSnapshot` now treats absence as conclusive only when the whole fixed
  projection is loaded (`!HasMore`): `EvaluateCurrentProjection` fails closed to `UnableToVerify`
  (`Tenants.Correction.Unavailable.CurrentProjectionUnavailable`) for a restore/revoke whose target is
  absent from an incomplete page, and `ConfirmProjection` proves a revoke only on `!present && !HasMore`,
  killing the false-`Confirmed` on a revoke of a page-2 administrator. Presence-found stays conclusive, so
  page-1 corrections at scale are unaffected. +3 tests + `PagedProjectionReady` helper. The multi-page
  load/aggregation that would let a page-2 correction actually RUN (rather than be conservatively blocked)
  stays routed to a dedicated projection-paging story.
  (`src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs`)
- **FrontComposer `FcContentLabel` single-writer dispose-clobber + server first-paint — DOCUMENTED.**
  XML `<remarks>` on `FcContentLabel` (plus a matching sentence on `FcContentLabelCoordinator`) now record
  the last-writer-wins dispose-clobber and the `OnAfterRender`-only first-paint limitation, naming the
  shell-parameter path (`ContentLabel`/`ContentLabelledBy`) as the first-paint-correct alternative. Doc-only.
- **FrontComposer `FcPageHeader.FocusHeadingAsync` no-op→throw — DOCUMENTED.** An adopter-facing
  behavior-change note was added to the method `<remarks>` (there is no FrontComposer CHANGELOG), incl. the
  caveat that the `FcAggregateListPage` wrapper's `?? ValueTask.CompletedTask` guards only the null-`@ref`
  window, not the throw.
- **EventStore `StorageTreemap` SVG `<g tabindex>` cross-browser — DOCUMENTED.** A Razor comment above the
  focusable cell records the Chromium/Edge/Firefox-vs-Safari/WebKit tab-order caveat and the
  `<a>`/`<foreignObject>` remedy if WebKit support is required.

Verification: `UI.Tests` **874/874** (871 + 3); all Tenants library + Tier-1/Tier-2 test projects build 0/0;
FrontComposer Shell 0/0; EventStore Admin.UI 0/0. **Pre-existing/environmental (NOT this change):** the
`Hexalith.Tenants.AppHost` + `IntegrationTests` Debug build fails because the `Hexalith.Commons` submodule was
fast-forwarded to `3666203` by an external `git pull --tags origin main` (per its reflog; not run this session)
and `Hexalith.Commons.Aspire` no longer resolves for the AppHost — confirmed the AppHost fails in isolation too.
Release `-warnaserror` remains blocked locally by `NU1102` (`Hexalith.Commons.UniqueIds 3.19.0` unpublished; CI has it).

**UNCOMMITTED** — Tenants repo (`GlobalAdministratorCorrectionSnapshot.cs` + its test) and the FrontComposer +
EventStore submodules (doc-only). Kept deferred: full projection paging (dedicated story), the IA/Product-blocked
trio (GA/Audit discoverability, unconsumed `GlobalAdministratorPolicy`, page-local empty tabpanels), and the
benign `EventCallback→Func` + latent ETag watch-items.

## 2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented

Proposal: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-30-deferred-work-tenants-owned.md`
(scope = Tenants-owned, actionable; batch review; classified Minor). Administrator-approved. The
global-administrator correction path (story 5.7) was hardened on 06-29/06-30; this run brought the
**tenant-domain** correction path (5.6/5.8) and two audit-page lifecycle paths to parity. All changes are
in `Hexalith.Tenants.UI` + its bUnit tests; full `UI.Tests` green at **871/871**; Debug build (root
`TreatWarningsAsErrors=true`) 0/0. Release `-warnaserror` could not run locally — restore fails `NU1102`
on the pinned `Hexalith.Commons.UniqueIds 3.19.0` (unpublished in this environment; CI has it). **UNCOMMITTED.**

Resolved / closed this run (the inline records below are annotated `RESOLVED 2026-06-30 (CC)`):

- **JSDisconnectedException guard on panel focus** — both `CorrectionStartPanel` and
  `GlobalAdministratorCorrectionPanel` `OnAfterRenderAsync` now wrap `_lifecycleElement.FocusAsync()` in
  `try/catch (JSDisconnectedException)` (parity with the existing `TenantAuditPage` guards).
- **Page-load global-admin query unguarded** — `TenantAuditPage.LoadAsync` now wraps the supplementary
  global-administrator enrichment in `catch (… EventStoreGatewayException or HttpRequestException or
  JsonException)`; the confirm-time path (`OpenCorrectionAsync` / panel `ProjectionRefreshProvider`) keeps
  propagating. Regression test: `Tenant_audit_page_survives_global_administrator_projection_fault_during_load`.
- **Tenant panel terminal-state focus parity** — `CorrectionStartPanel.SetSnapshot` now focuses on all six
  terminal states (Confirmed/Failed/Rejected/Degraded/UnableToVerify/AlreadyApplied), matching the GA panel.
  Test: `Panel_rejected_terminal_state_moves_focus_to_lifecycle`.
- **Tenant confirm fail-closed on stale/degraded** — `TenantAuditPage.RefreshTenantProjectionAsync` (the
  tenant confirm-time provider) returns the projection only when `Freshness is Current`, else `null`, so the
  existing `ConfirmProjection(null)` fails closed (parity with the GA `Freshness=Current` gate). Test:
  `Panel_does_not_confirm_when_projection_refresh_provider_returns_no_fresh_projection`.
- **Tenant corrective-proof time tie-back + invariant culture** — `CorrectionStartPanel.QueryCorrectiveProofAsync`
  now parses `originalTimestamp` with `InvariantCulture`+`RoundtripKind`, lower-bounds the audit query with
  `From: originalTimestamp`, filters `row.Timestamp > originalTimestamp`, newest-first; `ProofTimestampLabel`
  and `TenantCorrectionPreviewSnapshot.WithCorrectiveProof` parse with `InvariantCulture` (mirrors the GA fix).
  Test: `Panel_proof_lookup_ignores_audit_row_not_newer_than_the_original_event`.
- **Concurrent correction opens out of order** — `OpenCorrectionAsync` captures a `_correctionOpenGeneration`
  synchronously at entry and applies the active intent only if still latest. (No dedicated bUnit test —
  timing-deterministic two-open harness was judged more flake-prone than valuable; verified by construction +
  unchanged single-open tests.)
- **No story-specific 5.7 gateway-routing test** — CLOSED as already-covered: `TenantCommandGatewayTests`
  already pins the full `system / global-administrators / global-administrators` triple + CommandType + literal
  payload for both Set and Remove; the item was conditional on the gateway being touched (it wasn't).
- **Create-tenant freshness gate narrowed `Current or Unknown → Current`** — CLOSED as resolved: the gate is
  back to `Current or Unknown` (`TenantsWorkspace.razor` `CreateTenantFlow IsFresh`), matching the documented
  first-tenant bootstrap exception. The "restore" path was taken.

Kept deferred (out of this run's scope): global-admin projection **pagination >20 admins** (design-level —
needs projection paging/aggregation; route to a dedicated story); **EventCallback→Func** parent-re-render
watch-item (benign); **ETag special-character** robustness (latent); the IA-blocked trio (GA/Audit
discoverability, `GlobalAdministratorPolicy` registered-but-unconsumed, page-local empty tabpanels); and the
cross-submodule handoffs (FrontComposer `FcContentLabel`/`FocusHeadingAsync` docs, EventStore SVG `<g tabindex>`).

## 2026-06-21 Correct Course — Deferred + Pending Work Implemented

Administrator approved implementing every remaining deferred/pending item, including crossing the
submodule boundary for the owner handoffs. Outcome of this run (all changes verified building/green
and **committed + pushed**: FrontComposer `main` → `c6c3c39`, EventStore `main` → `5613fed4`,
Tenants branch `correct-course/2026-06-21-deferred-and-pending-work` → `62a94b0`):

- **FrontComposer owner handoff** `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening` — **IMPLEMENTED** in `Hexalith.FrontComposer`. `FcPageHeader` no longer emits a competing `banner` (header root is `role="presentation"`); `FrontComposerShell` exposes `ContentLabel`/`ContentLabelledBy` + a new `FcContentLabel` marker so a page can name the shell `main` landmark without an orphaned page-level `aria-labelledby`; blank `Heading` now fail-safes (no dangling `<h1>`, replacing the prior throw); `FocusHeadingAsync()` fails diagnostically when the heading is not focusable. Backward-compatible (new params default to null). FrontComposer Shell suite 1962/0 failed.
- **EventStore owner handoff** `eventstore-2026-06-19-admin-ui-and-query-record-followup` — **IMPLEMENTED** (Admin.UI a11y portion) in `Hexalith.EventStore`: `Index.razor` stat cards, `ActivityChart` (`role="group"` + real `<button>` bars), `StorageTreemap` (focusable `role="button"` cells), `RelatedTypeList`, `TypeDetailPanel`, `DaprHealthHistory`, and non-functional `cursor:pointer` spans on `Commands.razor`/`Events.razor` all remediated; conformance carve-out comment updated. The retired actor-routing sub-item was already verified stale/resolved (see below). Admin.UI.Tests green except 6 pre-existing unrelated `Dw5GovernanceAtddTests` (missing DW5 evidence artifact, not introduced here).
- **EventStore owner handoff** `eventstore-2026-06-19-read-model-freshness-metadata` — **IMPLEMENTED** in `Hexalith.EventStore.Client.Projections`: `IReadModelFreshness` (`ProjectedAt`/`ProjectionVersion`), `ReadModelFreshnessState`, `ReadModelFreshnessThresholds`, pure `ReadModelFreshness.Classify/Age`, plus `IReadModelStore.GetWithFreshnessAsync<T>()` and `ToQueryResponseMetadata()` bridges. This is the generic, persisted-timestamp replacement for the Tenants hand-rolled `TenantFreshnessState`; Tenants-side adoption is implemented by `cc-2026-06-25-tenant-read-model-freshness-adoption`. Client.Tests 462/462.
- **Epic 11 — Production Authorization Readiness (persisted DataProtection key ring)** — **IMPLEMENTED**. A Dapr-state-store-backed `IXmlRepository` (`DaprXmlRepository`) + `AddEventStoreDataProtection(...)` live in the `Hexalith.EventStore.DomainService` host-SDK layer; backend is chosen by `statestore.yaml` (Redis in prod) so the Tenants domain package gains NO infra SDK. `src/Hexalith.Tenants/Program.cs` swaps to `AddEventStoreDataProtection(config, "Hexalith.Tenants")`; production persists to the `statestore` under the application-specific key `hexalith-tenants-dataprotection-keys`, Development stays explicitly ephemeral. DomainService.Tests 36/36 (incl. cross-replica reload + ETag concurrency).
- **Pending (newly discovered) — Memories-integration doc/test drift** — **FIXED**. The committed Memories search-index integration added the local Memories app id to the AppHost `pubsub.yaml` scopes and 4 `MemoriesSearchIndexEventPublisher` handlers to the Sample program, but left 3 conformance/doc tests red on `main`. Updated `EventPublicationConfigurationTests` + `CrossAggregateTimingDocumentationTests` (local now scopes `memories`; production stays `eventstore`+`sample`), `docs/cross-aggregate-timing.md`, and `docs/sample-consuming-service-walkthrough.md`. Tenants Server.Tests back to 700/700.

The previously-open Tenants-owned code-review-deferred items (CSS logical-longhand guard, forced-colors
unterminated block, DLQ operator-scope note, stale `test-summary.md` line) were already closed in the
2026-06-21 hardening pass; the records below are updated to reflect that.

## Tenants-Owned Work Routed to Ready-for-Dev Stories

### `cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`

Status: `done` (sprint-status.yaml) — closed after the 2026-06-19 implementation and review.
Story artifact: `_bmad-output/implementation-artifacts/cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening.md`.
Primary source: code review of `3-5-tenant-query-gateway-rest-routing` on 2026-06-07.

Resolution summary:

- Freshness is no longer derived from response `ServedAt`. The implemented direct-read rule treats a real read-model ETag/projection version as `current`; absent markers resolve to `unknown`.
- Generic projection age/version metadata is not available from the current `IReadModelStore` contract. Do not add Tenants-owned generic persistence scaffolding; the remaining threshold-based age metadata need is routed to the EventStore owner handoff below.
- Null/empty read-model ETag behavior is explicit and tested: successful REST reads return 200 with no ETag, no projection-version header, no served-at header, and no 304 support.
- ETag handling is hardened and tested for weak tags, `*`, escaped strong tags, and unsupported multi-tag input.
- REST/handler read-model reconstruction coverage now proves a recreated controller factory can serve the persisted read model from the shared store and honor 304 through the production REST/handler path.
- Live populated-correlation gateway error coverage now asserts that `correlationId`, `reasonCode`, raw payload text, stack traces, tokens, cursors, and ETags do not reach user-facing copy.
- Current full-suite evidence (corrected 2026-06-21): the earlier `Server.Tests` blocker — 3 DAPR component expectation tests asserting removed `enableDeadLetter` / `deadLetterTopic` metadata — was resolved on 2026-06-20 by `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`; full `Server.Tests` now passes 700/700. `IntegrationTests` passes with DAPR/Aspire/performance skips. The old health-readiness blocker wording is no longer current evidence.

### `cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`

Status: `done` (sprint-status.yaml) — closed after the 2026-06-19 implementation and review.
Story artifact: `_bmad-output/implementation-artifacts/cc-2026-06-19-domain-ui-governance-and-accessibility-hardening.md`.
Primary sources:

- Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18.
- Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18.

Resolution summary (2026-06-19):

- Compact non-zero spacing (e.g. `margin:0.5rem`/`padding:0.5rem`) is now flagged by the styling-ownership guard. The `(?!0)` zero-skip was replaced with a zero-token matcher that still skips genuine resets (`0`, `0 0 0 0`, `0px`, `0 !important`). No real component CSS regressed.
- The inline-style guard was widened beyond flex/grid/gap to also cover spacing (margin/padding), sizing (width/inline-size), and alignment (justify-content/align-items), and now scans both quote styles. No `.razor` carries inline `style=`.
- The `<div>`/`<span>` budget now excludes Razor (`@* *@`) and HTML (`<!-- -->`) comments before counting.
- `fc-css-exception` scoping decision: kept RULE-level with documented rationale; a unit test proves a marker exempts only its own rule and does not leak to the next rule.
- `:focus-visible` exemption decision: NARROWED. The blanket exemption was removed; focus-ring affordances (outline/outline-offset/outline-color) are untracked so genuine focus rules still pass, but a `:focus-visible` rule that owns layout/spacing/typography is now flagged unless documented.
- `RemoveForcedColorsMediaBlocks` now skips braces inside CSS comments and quoted strings so a stray brace cannot leak the block tail back into the scan.
- `MemberAccessReview` gained bUnit coverage proving the change-role and remove-member `aria-controls` resolve to a rendered active-region `id` after the FluentStack migration.
- `TenantAuditPage` renders a localized fallback (`Tenants.Audit.UnknownTenant`) for a blank/whitespace `TenantId` instead of a dangling heading.

Still-open sibling candidates (not regressions, future hardening): route pages with `<FcPageLayout>` but no `Mode`, logical longhand `-start`/`-end` spacing that the ownership regex never tracked, pseudo-class root selectors, undocumented new structural tags, and unclosed forced-colors blocks. Single-quoted inline layout `style` was closed as part of AC2.

Dismissed record retained:

- The claim that the styling scan is blind to declarations inside `@media` blocks was verified as a false positive in the structural/style story and is not open work.

### `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`

Status: `done` (sprint-status.yaml) — closed after the 2026-06-20 implementation and review.
Story artifact: `_bmad-output/implementation-artifacts/cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup.md`.
Primary sources:

- Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18.
- Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18.
- Current deployment docs/YAML scan on 2026-06-19.
- DAPR v1.17 topic-scoping documentation checked on 2026-06-20.

Current resolution summary:

- Production `deploy/dapr/pubsub.yaml` denies `sample` publishing with an empty topic list (`publishingScopes: "sample="`) and allows `sample` to subscribe to `tenants.events`, while leaving `eventstore` unlisted so it keeps unrestricted publish access (required for EventStore dynamic per-tenant topic provisioning, NFR20 — listing `eventstore` is the documented anti-pattern). [2026-06-20 code-review correction: an earlier explicit `eventstore=tenants.events,deadletter.tenants.events;sample=` allow-list was reverted because it violated EventStore NFR20 and would have silently denied dynamic-tenant topics.]
- Local AppHost pub/sub intentionally omits topic-level scopes while retaining component-level `eventstore` and `sample` scopes; the difference is documented in the component YAML and timing guide.
- `docs/cross-aggregate-timing.md` distinguishes subscriber redelivery on `tenants.events` from EventStore's application-level dead-letter publisher for `deadletter.tenants.events`.
- `CrossAggregateTimingDocumentationTests` guards the production topic-scope contract, local topic-scope omission, application-level dead-letter wording, and the absence of DAPR subscriber-failure-to-dead-letter wording.
- June 18 review-record contradictions are kept as routed, stale/resolved, or future-owner handoff entries instead of open Tenants implementation work.

## Cross-Submodule Owner Handoffs

### FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`

Status: **IMPLEMENTED 2026-06-21** in the `Hexalith.FrontComposer` submodule under Administrator approval (see the run summary at the top). Committed + pushed (FrontComposer `main` → `c6c3c39`).
Source proposal section: `5.5 FrontComposer Owner Handoff`.

Requested outcomes:

- `FrontComposerShell` exposes a shell content landmark contract that can be a native `<main>` or an equivalent role with an accessible-name parameter.
- Tenants page headings can name the shell main landmark without orphaned page-level `aria-labelledby`.
- `FcPageHeader` no longer creates a competing global `banner` landmark on every route page.
- `FcPageHeader` handles blank `Heading` fail-safely or documents a strict consumer contract with analyzable/tested guardrails.
- `FocusHeadingAsync()` ensures the heading is focusable when used as a focus target or fails diagnostically when `HeadingTabIndex` is omitted.

Related prior audit handoffs:

- FrontComposer H-FC-1: rework or re-justify `FcHomeCard` against pinned `FluentCard` support.
- FrontComposer H-FC-2: consider parity guards for structural/style governance.

### EventStore owner: `eventstore-2026-06-19-admin-ui-and-query-record-followup`

Status: **IMPLEMENTED 2026-06-21** (Admin.UI a11y portion) in the `Hexalith.EventStore` submodule under Administrator approval. The actor-routing sub-item was already verified stale/resolved (see Stale or Resolved Records). Committed + pushed (EventStore `main` → `5613fed4`).
Source proposal section: `5.6 EventStore Owner Handoff`.

Requested outcomes:

- Continue the Admin.UI audit remediation handoffs from `audit-frontcomposer-shell-adminui-fluent-2026-06-18.md`: `Index.razor` non-semantic clickable semantics, clickable-span remediation, `ActivityChart` a11y proof, `StorageTreemap` semantics/docs, and optional parity guards.
- If EventStore tests still encode the retired Tenants actor-routing assumption, update them under EventStore ownership.

### EventStore owner: `eventstore-2026-06-19-read-model-freshness-metadata`

Status: **IMPLEMENTED 2026-06-21** in the `Hexalith.EventStore.Client.Projections` namespace under Administrator approval (`IReadModelFreshness` + `ReadModelFreshness*` types + `IReadModelStore.GetWithFreshnessAsync<T>()`/`ToQueryResponseMetadata()`). Committed + pushed (EventStore `main` → `5613fed4`). **Tenants-side adoption IMPLEMENTED 2026-06-25** by Tenants-owned story `cc-2026-06-25-tenant-read-model-freshness-adoption` (Correct Course, "Server-side, 3-state" option approved by Administrator): read models implement `IReadModelFreshness` (persisted `ProjectedAt`), query handlers classify via `ToQueryResponseMetadata` (current/stale/unknown), and the UI retires the hand-rolled `TenantFreshnessState` for the shared `ReadModelFreshnessState`. **Residual (not this story):** `aging` is not producible end-to-end because `QueryResponseMetadata` has no `ProjectedAt` field and `ToQueryResponseMetadata` collapses `aging`→`current`; surfacing real `aging` would need a `QueryResponseMetadata.ProjectedAt` wire addition (a NEW EventStore owner handoff) so the UI can classify with its own thresholds — deferred, not in scope.
Source story: `cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`.

Requested outcomes:

- Add or expose shared read-model metadata for persisted projection timestamp/version if D6 threshold-based `aging` and `stale` states need to be computed generically.
- Keep the capability in `Hexalith.EventStore` (`IReadModelStore` / query metadata path) rather than adding Tenants-specific persistence scaffolding.
- Once available, Tenants can map real persisted projection age/version through configurable thresholds; until then Tenants uses the direct-read ETag/version `current` rule and fails unmarked responses closed to `unknown`.

## Stale or Resolved Records

### EventStore Admin retired actor-routing entry

Status: stale/resolved as of 2026-06-19; re-verified on 2026-06-20.

Previous record said `references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.Server/Services/DaprTenantQueryService.cs` still assigned `ProjectionActorType: TenantProjectionRouting.ActorTypeName`.

Verification command:

```bash
rg -n "ProjectionActorType|TenantProjectionRouting|TenantsProjectionActor" references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.Server/Services/DaprTenantQueryService.cs
```

Result on 2026-06-19 and 2026-06-20: no matches. Do not carry this as open Tenants work.

### Inert DAPR component dead-letter metadata

Status: resolved on 2026-06-18; only the timing-diagram wording remains open under `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`.

The misleading Redis pub/sub component keys `enableDeadLetter` and `deadLetterTopic` were removed from local and production component files. EventStore's application-level dead-letter publisher remains the documented mechanism.

### Per-commit history and commit-scope hygiene

Status: not scheduled as implementation work.

Records about intermediate non-building commits, co-mingled story diffs, and bundled DAPR/health changes are history hygiene notes. The current approved path is to keep completed story states intact and create focused follow-up stories for real runtime, governance, deployment, and documentation work.

## Deferred from: code review of cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening (2026-06-19)

- ETag special-character (quote/comma) robustness — latent, non-exploitable. `NormalizeETagToken`/`Trim('"')` unquote any value that starts and ends with `"` (asymmetric vs raw store tokens) in `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs:87-107`; the client and server both reject commas with a substring check, dropping a single quoted strong tag whose content legitimately contains a comma (`src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs:25-29`); and client/server normalization disagree on quoted-whitespace/`"*"` edge inputs. These do not bite while DAPR/Redis read-model ETags remain opaque numeric strings without quotes or commas, and the emit→submit→compare round-trip is internally symmetric. Revisit if the EventStore read-model store contract ever emits special-character ETags (ties into the `eventstore-2026-06-19-read-model-freshness-metadata` handoff above).

## Deferred from: code review of cc-2026-06-19-domain-ui-governance-and-accessibility-hardening (2026-06-19)

- CSS ownership guard logical longhand spacing — **RESOLVED (2026-06-21 hardening).** `DomainUiFluentConformanceTests` now tracks the logical longhands (`margin-inline-start/-end`, `padding-block-start/-end`, etc.) alongside the physical longhands and shorthand, with `[InlineData]` coverage for both flagged and zero-reset cases.
- Forced-colors malformed block handling — **RESOLVED (2026-06-21 hardening).** `RemoveForcedColorsMediaBlocks` plus a dedicated `Forced_colors_unterminated_block_does_not_hide_trailing_ownership` test now ensure an unterminated forced-colors block cannot hide trailing ownership declarations from the scan.
- Sibling query ETag special-character robustness — quote/comma ETag edge cases surfaced again because the working-tree diff includes the completed tenant-query hardening story. Keep routed under the tenant-query review / EventStore read-model freshness handoff; it is outside the domain UI governance story.

## Deferred from: code review of cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup (2026-06-20)

- Application-level vs native dead-letter framing for operators — **RESOLVED (2026-06-21 hardening).** `deploy/dapr/README.md:53` now carries an explicit operator note scoping the "no native dead-letter" claim to the `pubsub` component shipped here and warning that an EventStore-provided component may set its own `enableDeadLetter`/`deadLetterTopic`.
- Stale Server.Tests evidence line in `test-summary.md` — **RESOLVED (2026-06-21 hardening).** A correction note was added (`tests/test-summary.md:246`) recording that the 3-test Server.Tests blocker was resolved on 2026-06-20 and that Server.Tests passes; the old line is retained only as dated historical evidence.

## Deferred from: code review of cc-2026-06-21-frontcomposer-page-header-landmarks-and-contract-hardening (2026-06-25)

_Reviewed FrontComposer commit `c6c3c39` (already merged to FrontComposer `main`). Both items live in the `Hexalith.FrontComposer` submodule; any fix is a new follow-up commit, not an amendment._

- **`<FcContentLabel>` single-writer dispose-clobber** — when two `<FcContentLabel>` markers render on one page, disposing one calls `FcContentLabelCoordinator.Reset()` (→ `Set(null, null)`), wiping a still-live sibling's accessible name on `#fc-main-content` until the survivor happens to re-render (`references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcContentLabelCoordinator.cs:159` + `FcContentLabel.razor.cs:80-84`). Real but silent a11y edge case; it faithfully mirrors the accepted, documented `FcPageLayoutCoordinator` "single-writer, last-writer-wins" pattern (identical latent limitation by design) and no current consumer renders two markers. Fix path if multi-writer support is ever needed: add a writer-identity/token guard so only the current writer's dispose resets — apply to BOTH coordinators together for consistency. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the dispose-clobber + single-writer last-writer-wins limitation is now recorded in the `FcContentLabel` XML `<remarks>` and a matching sentence on `FcContentLabelCoordinator`; the writer-identity guard remains the routed follow-up if multi-writer support is ever needed.
- **Page-driven `<FcContentLabel>` accessible name absent on server first paint** — registration is `OnAfterRender`-only (`FcContentLabel.razor.cs:67-77`), so on a static-SSR/prerender pass `#fc-main-content` emits no `aria-label`/`aria-labelledby` from the page-marker path; the name appears only after interactive hydration. The shell-parameter path (`ContentLabel`/`ContentLabelledBy`) is correct on first paint. Mirrors the established `FcPageLayout` coordinator pattern and is acceptable for this InteractiveServer library; recommend documenting the limitation in the `FcContentLabel` XML remarks. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the `OnAfterRender`-only first-paint limitation is now in the `FcContentLabel` XML `<remarks>`, naming the shell-parameter path as the first-paint-correct alternative.
- **`FocusHeadingAsync()` no-op → throw is an undisclosed API behavior change** (resolved `decision-needed` → defer by Administrator on 2026-06-25). Keep the diagnostic throw: it is the intended hardening (Requested outcome 5) and no live consumer regresses (`TenantsWorkspace` → `FcAggregateListPage` passes `HeadingTabIndex="-1"`, verified). Follow-up: document the no-op→throw change for external FrontComposer adopters in the changelog / `FcPageHeader.FocusHeadingAsync` remarks, and note that the `FcAggregateListPage` wrapper's `… ?? ValueTask.CompletedTask` only guards the pre-first-render null `@ref` window, not the new throw. `FcPageHeader.razor.cs:104-117`, `FcAggregateListPage.razor.cs:83-84`. FrontComposer submodule. — **DOCUMENTED 2026-07-01 (CC deferred-work):** FrontComposer has no CHANGELOG, so the adopter-facing no-op→throw behavior-change note (incl. the `FcAggregateListPage` `?? ValueTask.CompletedTask` caveat) was added to the `FcPageHeader.FocusHeadingAsync` XML `<remarks>`.

## Deferred from: code review of cc-2026-06-21-eventstore-admin-ui-a11y-remediation (2026-06-25)

_Reviewed EventStore commit `d6d3ea69` (`feat(admin-ui): keyboard-accessible interactive semantics across Admin.UI`, already on EventStore `main`). The item lives in the `Hexalith.EventStore` submodule; any fix is a new follow-up commit, not an amendment._

- **SVG `<g tabindex="0">` focusability not guaranteed cross-browser** (`references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Components/StorageTreemap.razor:72`) — the treemap cells became focusable via `tabindex="0"` on an SVG `<g>` group. Modern Chromium/Edge/Firefox include tabindex'd SVG container elements in the tab order; Safari/older WebKit historically do not, which would leave the treemap cells (and their `role="button"` keyboard activation) unreachable by Tab there. For an internal EventStore Admin.UI targeting Chromium/Edge the practical risk is low. Follow-up: validate against the actual supported browser matrix; if Safari/WebKit must be supported, make the focusable element an SVG `<a>` or wrap an HTML control in `<foreignObject>`. The bUnit test only asserts the attribute is present, not that the browser focuses it. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the cross-browser caveat + `<a>`/`<foreignObject>` remedy is now recorded as a Razor comment above the focusable `<g role="button" tabindex="0">` in `StorageTreemap.razor`. Validation against the actual supported browser matrix remains the routed follow-up.

## Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace (2026-06-27)

_Adversarial review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) over the uncommitted working tree. All 14 ACs verified met; this is the single deferred follow-up._

- **Global Administrators / Audit discoverability after nav de-listing** — the approved 2026-06-27 IA (AC9) removed `/global-administrators` and audit from the Tenants left-menu; the routes, pages, and `GlobalAdministratorPolicy` are preserved, but the diff adds no module-internal/contextual entry point, so a global administrator can reach the surface only by typing the URL. The sprint-change-proposal explicitly defers this: GA/Audit "remain available through module-internal tabs or contextual entry points ... unless a future module-level IA decision adds them explicitly." Follow-up: when Product confirms the contextual entry-point IA, add a discoverable in-workspace path. (`src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`)

## Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Group 1 re-review (2026-06-27, chunked)

_Chunked post-commit re-review (`ba14356..HEAD`), Group 1 = UI workspace & panels. Groups 2 (server freshness + gateway), 3 (tests), 4 (docs) pending separate runs._

- **GlobalAdministratorPolicy now registered but unconsumed** — extends the GA discoverability item above: after the nav `RequiredPolicy:` was removed, `Program.cs:33` still registers `Tenants.GlobalAdministrator` but nothing requires it (the GA page authorizes via `BffComposition` reflection). Retention is intentional pending the deferred contextual-entry-point IA decision; revisit (wire or remove) when that decision lands. (`src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`, `src/Hexalith.Tenants.UI/Program.cs:33`)
- **~~Create-tenant freshness gate narrowed `Current or Unknown` → `Current`~~ — RESOLVED 2026-06-30 (CC deferred-work, verify-only)** — the "restore" path was taken: `TenantsWorkspace.razor` `CreateTenantFlow IsFresh` is back to `Freshness is Current or Unknown`, matching the documented first-tenant bootstrap exception (Unknown list freshness remains creatable). No code change this run; verified live. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`)
- **Page-local tabs render empty tabpanels** — the new `FluentTabs` carry `Id`/`Header` only; active content renders in sibling `FcAggregateListPage` slots (`Body`/`Filters`/`States`), so the Fluent tab→tabpanel ARIA relationship points at empty regions. `aria-selected` is correct and tabs are keyboard reachable. This is an `FcAggregateListPage`-slot architectural nuance best owned upstream. Follow-up: FrontComposer/UX decision on associating `FcAggregateListPage` content with `FcPageToolbar`/tab tabpanels. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:28-30`)

## Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Full review (2026-06-28)

_Full review (`ba14356..HEAD`, Blind Hunter + Edge Case Hunter + Acceptance Auditor) completing Groups 2 (server freshness + gateway), 3 (tests), 4 (docs). The decision-needed and patch findings are in the story file's `Review Findings — Full review 2026-06-28` section; this is the single deferred follow-up._

- **Page-local tabs a11y — empty tabpanels + missing Tenants-owned bUnit assertion** — extends the 2026-06-27 Group-1 "empty tabpanels" defer above. AC12/AC13 keyboard/active-tab guarantees ride entirely on the Fluent `FluentTabs` primitive with no Tenants-owned `aria-selected`/keyboard-switch bUnit assertion; the added tests assert tab presence/text and routing only. Follow-up: pair the upstream FrontComposer/UX tabpanel-association decision with a focused active-tab/keyboard bUnit test once the structure is settled. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:22-30`)

## Deferred from: code review of 5-7-global-administrator-correction-verification (2026-06-29)

_Adversarial review (Blind Hunter + Edge Case Hunter + Acceptance Auditor). The patch and HIGH findings are in the story file's `Review Findings — BMAD Code Review (2026-06-29)` section; these are the deferred follow-ups._

- **Global-administrator projection pagination ignored (>20 admins)** — `GlobalAdministratorsRequest` defaults to PageSize=20 and `HasMore`/cursor are never read; the correction snapshot only inspects page 1's `Rows` for presence and admin count. For more than 20 global administrators: a restore of a 21st+ target is treated as not-applied and can never reach `present=true` (stuck `ProjectionPending`), and a revoke of a 21st+ target is blocked as "already removed". Pre-existing query-shape limitation reused by this story; unusual scale and most failure modes fail closed. Follow-up: design projection paging/aggregation for the fixed global-administrator projection. (`src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs`) — **UPDATE 2026-07-01 (CC deferred-work): the fail-OPEN is CLOSED.** The snapshot now reads `HasMore` and treats absence as conclusive only on a fully-loaded page: `ConfirmProjection` proves a revoke only on `!present && !HasMore` (killing the page-2 false-`Confirmed`), and `EvaluateCurrentProjection` fails closed to `UnableToVerify` (`…CurrentProjectionUnavailable`) rather than the false `AlreadyRemoved` (revoke) or a mis-armed grant (restore). Presence-found stays conclusive so page-1 corrections at scale are unaffected. The residual is now narrowed to the full multi-page load/aggregation that would let a page-2 correction actually RUN instead of being conservatively blocked — still a dedicated projection-paging story.
- **~~No story-specific gateway-routing test~~ — CLOSED 2026-06-30 (CC deferred-work) as already-covered** — verification showed `TenantCommandGatewayTests` already pins the full `system / global-administrators / global-administrators` triple + CommandType + literal payload for both `SetGlobalAdministratorAsync` and `RemoveGlobalAdministratorAsync`. The item was explicitly conditional on the gateway being touched; it was not, so no new (near-duplicate) test was added. (`tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`)

## Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)

Adversarial code review of the story 5.8 projection-refresh-provider cleanup. These items are real but pre-existing relative to the 5.8 change — they live in the story 5.7 (global-admin) / 5.6 (tenant) correction panel and snapshot logic that 5.8's File List bundled in. Route to the owning story's review.

- **~~Terminal failure states reset to a fresh submittable preview on parent re-render (HIGH)~~ — RESOLVED 2026-06-30** — both panels now preserve any existing snapshot when the intent is unchanged (`_snapshot is not null && !intentChanged → return`), rebuilding only on a different/first intent, so post-submission terminal states survive parent re-renders without re-arming Submit. GA panel already carried the fix + regression test; the tenant panel was fixed in the code review with a matching `Failed_correction_survives_a_parent_re_render_without_re_arming_submit` test. Full UI suite 838/838 green. (`GlobalAdministratorCorrectionPanel.razor:220`, `CorrectionStartPanel.razor:286`)
- **~~`ConfirmProjection` confirms off a known-Stale projection~~ — RESOLVED 2026-06-30** — two parts: (1) `ConfirmProjection` itself was hardened by the 2026-06-29 review (P2) to require `Kind Ready` + `Freshness Current`; (2) the live residual — the **pre-submit** gate `ProjectionIsReadable` still accepting `Stale`/non-current, which let a platform-authority correction be SUBMITTED against stale evidence — was fixed in the 2026-06-30 code review: `ProjectionIsReadable` now requires `Kind ∈ {Ready,Empty}` **and** `Freshness=Current`, mirroring the confirm/start gates (Empty-current kept for first-admin restore). (`GlobalAdministratorCorrectionSnapshot.cs` `ProjectionIsReadable`)
- **Corrective-proof lookup may link the wrong historical audit row** — **GLOBAL-ADMIN RESOLVED 2026-06-30; tenant-domain residual RESOLVED 2026-06-30 (CC deferred-work, Edit F).** The global-admin path requires parseable invariant original timestamp evidence, requests system audit rows from that timestamp, filters strictly newer corrective rows, and reports audit delayed when the timestamp is missing/malformed. The tenant-domain `CorrectionStartPanel.QueryCorrectiveProofAsync` now mirrors that pattern (invariant/roundtrip parse, `From: originalTimestamp`, `Timestamp > original`, newest-first). (`CorrectionStartPanel.razor` `QueryCorrectiveProofAsync`)
- **~~Focus call lacks `JSDisconnectedException` guard~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit A)** — both `CorrectionStartPanel` and `GlobalAdministratorCorrectionPanel` `OnAfterRenderAsync` now wrap `_lifecycleElement.FocusAsync()` in `try/catch (JSDisconnectedException)`, matching the existing `TenantAuditPage` guards. (`GlobalAdministratorCorrectionPanel.razor`, `CorrectionStartPanel.razor`)
- **~~Global-admin projection query unguarded in the page-load critical path~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit B)** — `LoadAsync` now wraps the supplementary global-administrator enrichment in `catch (… EventStoreGatewayException or HttpRequestException or JsonException)`; the confirm-time path (`OpenCorrectionAsync` / panel provider) keeps propagating. Test `Tenant_audit_page_survives_global_administrator_projection_fault_during_load`. (`TenantAuditPage.razor` `LoadAsync`)
- **~~Corrective-proof timestamp uses `CurrentCulture` instead of `InvariantCulture`~~ — RESOLVED 2026-06-30** — the proof *display* timestamp was fixed by the 2026-06-29 review (P9 — `ProofTimestampLabel` uses `InvariantCulture`); the live residual — the `originalTimestamp` *parse* in `WithCorrectiveProof` (and the panel's proof lookup) using ambient culture — was fixed in the 2026-06-30 code review by parsing with `CultureInfo.InvariantCulture` + `DateTimeStyles.RoundtripKind`. The same review also added a time tie-back so the corrective row must be at/after the original event time. (`GlobalAdministratorCorrectionSnapshot.cs` `WithCorrectiveProof`, `GlobalAdministratorCorrectionPanel.razor` `QueryCorrectiveProofAsync`). NB: the tenant-domain `CorrectionStartPanel` (story 5.6) was likewise fixed 2026-06-30 (CC deferred-work, Edit F): `ProofTimestampLabel` and `TenantCorrectionPreviewSnapshot.WithCorrectiveProof` now parse/format with `InvariantCulture`, and the panel has the proof time tie-back.
- **EventCallback→Func drops the parent re-render after confirm refresh (intentional, benign)** — watch-item only: the new `ProjectionRefreshProvider` Func updates the parent field without re-rendering the parent; benign today because those fields feed only the panel. Restore a parent render (or document) if other parent UI later binds the refreshed snapshots. 5.8-introduced. (`CorrectionStartPanel.razor:202`, `TenantAuditPage.razor:550`)

## Deferred from: code review of 5-7-global-administrator-correction-verification — committed bundle re-review (2026-06-30)

_Second adversarial review (Blind + Edge + Acceptance) of the **committed** commit `939bebc` (`671c282..939bebc`), which bundles 5.7 + 5.8 + the `TenantCommandFlowGuard`. The decision-needed + patch findings (incl. a HIGH stale-projection-submit gate and a command-flow lock-leak) are in the 5.7 story file's `Review Findings — BMAD Code Review (2026-06-30 …)` section. Only the genuinely-new defer is recorded here; the other two are already logged above (2026-06-29)._

- **~~`CorrectionStartPanel` terminal-state focus parity (story 5.6)~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit C)** — `CorrectionStartPanel.SetSnapshot` now moves keyboard focus on all six terminal states (`Confirmed`/`Failed`/`Rejected`/`Degraded`/`UnableToVerify`/`AlreadyApplied`), mirroring `GlobalAdministratorCorrectionPanel.SetSnapshot`. Test `Panel_rejected_terminal_state_moves_focus_to_lifecycle`. (`src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor`)
- **~~Already-logged (2026-06-29), re-confirmed:~~ RESOLVED 2026-07-01 (CC deferred-work):** global-administrator projection pagination ignored (>20 admins) — the **confirm-time false-`Confirmed`** path (revoke of a page-2 admin reads `!present` ⇒ "proven"), whose raised severity was flagged here, is now closed: `ConfirmProjection` requires `!present && !HasMore` to prove a revoke, and the preview gate fails closed on an incomplete page. Only the full projection-paging redesign remains routed. (`GlobalAdministratorCorrectionSnapshot.cs`)
- **Already-logged (2026-06-29), re-confirmed:** no story-owned gateway-routing test. No new entry created.
- **~~Ledger-hygiene (see 5.7 patch P-9)~~ — CLOSED 2026-06-30:** the stale "ConfirmProjection confirms off a known-Stale projection" and "Corrective-proof timestamp uses CurrentCulture" entries were rewritten/closed after the follow-up patches landed. Global-admin stale projection and proof timestamp/parse paths are resolved; tenant-domain residuals are tracked separately below.

## Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-30)

_Adversarial review of committed diff `939bebc..02e4dfb`._

- **~~Concurrent correction opens can finish projection refresh out of order~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit D)** — `OpenCorrectionAsync` now captures a `_correctionOpenGeneration` synchronously at entry and applies the active intent only if still the latest, so an earlier open whose refresh resolves last no longer wins. (`src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` `OpenCorrectionAsync`)
- **~~Tenant-domain correction can still confirm from stale/degraded tenant detail~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit E)** — `RefreshTenantProjectionAsync` (the tenant confirm-time provider) now returns the projection only when `Freshness is Current`, else `null`, so `ConfirmProjection(null)` fails closed instead of confirming off stale evidence (parity with the GA `Freshness=Current` gate). Test `Panel_does_not_confirm_when_projection_refresh_provider_returns_no_fresh_projection`. (`src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` `RefreshTenantProjectionAsync`)
- **~~Tenant-domain corrective proof lookup can link unrelated historical rows~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit F)** — `QueryCorrectiveProofAsync` now parses `originalTimestamp` (`InvariantCulture`+`RoundtripKind`), lower-bounds the audit query with `From: originalTimestamp`, filters `row.Timestamp > originalTimestamp`, newest-first; missing/malformed timestamp ⇒ audit-delayed. Test `Panel_proof_lookup_ignores_audit_row_not_newer_than_the_original_event`. (`src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor` `QueryCorrectiveProofAsync`)

## Deferred from: run-all-tests-and-fix-failures review (2026-07-14)

- **Scheduled performance workflow lacks the EventStore opt-in** — the shared `domain-ci.yml` performance job invokes the `Category=Performance` lane without `HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1`, while `DaprPerformanceFactAttribute` requires that variable. Local verification explicitly enabled it and executed the 500,000-event benchmark, but the scheduled shared workflow can report a skip. Fix belongs in `Hexalith.Builds` and requires separate submodule approval; add the environment variable to the shared performance job and validate a scheduled-shaped run. (`references/Hexalith.Builds/.github/workflows/domain-ci.yml`)

## Deferred from: code review of 1-0-reverify-frontcomposer-shell-and-fluent-contracts (2026-07-19)

_Adversarial review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) over the uncommitted diff (story file + evidence report + `sprint-status.yaml` flip). The decision-needed and patch findings are in the story file's `Review Findings — BMAD Code Review (2026-07-19)` section; these four are the deferred follow-ups._

- **Zero test changes despite several identified Tenants-rendering gaps** — Size16 vs required Size20 icons, missing `IconLabel`, unpinned freshness safety column, missing `MessageBarLayout.Notification`/`AriaLive` usage (`story-1-0-frontcomposer-fluent-reverification-2026-07-19.md` FC-TOK/FC-TBL rows). AC5's own wording is conditional ("add tests only when they guard a confirmed Tenants boundary"), so whether any of these gaps currently qualify is a judgment call for whichever story next touches badge/grid rendering, not a clear miss by this verification-only story.
- **No tracking ticket/issue for the FrontComposer-owned gaps** this story identifies (FC-CMD, FC-CNC, FC-TBL, FC-TOK) — "assign to FrontComposer" has no actual assignment mechanism in this repo's process. Matches the existing routing convention in this file's "Cross-Submodule Owner Handoffs" section, which this story's four gaps should eventually feed as new entries once an owning FrontComposer task is opened.
- **`sprint-status.yaml`'s flat per-story status can't represent "review with 2 of 5 sub-contracts blocked"** (`_bmad-output/implementation-artifacts/sprint-status.yaml:53`) — schema limitation of a shared tracking file used across the whole project; not something this story's diff introduced or can fix alone.
- **`epic-1: done` while most of Epic 1's 12 stories remain `backlog`/`review`** (`_bmad-output/implementation-artifacts/sprint-status.yaml:52-64`) — only 3 of 12 stories under Epic 1 (1-3, 1-5, 1-7) are `done`; the rest (1-0, 1-1, 1-2, 1-4, 1-6, 1-8 through 1-11) are `backlog`/`review`, yet `epic-1` and `epic-1-retrospective` are both marked `done`, violating the file's own documented rule ("done: All stories in epic completed"). Pre-existing — the `epic-1`/`epic-1-retrospective` lines are untouched by this story's diff (only the `1-0-...` status line changed). Likely stale from the epics.md renumbering during the 2026-07-19 sprint-change-proposal rollout (see memory `prd-edit-2026-07-17-scp-0715-prd-slice`); route to a sprint-planning resync, not a fix within this story.

## Deferred from: code review of 1-1-reverify-ui-host-bootstrap-and-canonical-workspace (2026-07-19)

- **Reusable release caller omits required publication-authority inputs** — `.github/workflows/release.yml` already enabled container publication without `builds-execution-sha`, `release-authority-url`, or `release-owner-allowlist`; the shared `domain-release.yml` rejects their empty defaults before publication. This predates Story 1.1's UI mapping and requires a separately authorized release-governance fix. (`.github/workflows/release.yml:29`; `references/Hexalith.Builds/.github/workflows/domain-release.yml:95`)
- **Submodule pointer upgrades require their own review** — the Builds and FrontComposer pointer changes were present before Story 1.1 implementation and alter shared build/UI inputs. Review and land those dependency changes independently rather than absorbing them into this story's patch set. (`references/Hexalith.Builds`; `references/Hexalith.FrontComposer`)
- **Epic 1 aggregate status conflicts with child stories** — `epic-1` remains `done` while Story 1.1 is in review and multiple children are backlog. The aggregate line predates this story and should be reconciled by sprint planning. (`_bmad-output/implementation-artifacts/sprint-status.yaml:52`)

## Deferred from: code review of run-all-tests-and-fix-failures-2 (2026-07-20)

_Adversarial review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) of the 2-file allowlist diff (`scripts/validate-nuget-packages.py`, `tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs`) that fixed the "Validate package consumer references" CI gate. The acceptance auditor found zero violations (scope, gate strength, and the spec's Results section all check out against the real extracted upstream nuspecs). The edge case hunter's one substantive claim — that `Hexalith.Tenants.Client`'s `EXPECTED_DEPENDENCIES` entry is missing 12 ids that `Hexalith.EventStore.Client` 3.78.0 itself pins — was checked against this session's own already-executed `validate-nuget-packages.py` runs (twice, including once freshly packed at the exact 3.78.0 pin) and rejected as a false positive: both runs validated all 5 real packed packages, including `Hexalith.Tenants.Client`, with zero missing/unexpected dependencies, meaning `Hexalith.Tenants.Client`'s real packed nuspec does not flatten `EventStore.Client`'s transitive deps the way `Hexalith.Tenants.Server`/`.Aspire` do (those two are pulled in from source, unconditionally, by the AppHost project — `Hexalith.EventStore.Client` is not). Only the pre-existing test-design points below are genuinely deferred._

- **`EXPECTED_DEPENDENCIES` is hand-duplicated between `scripts/validate-nuget-packages.py` and its test mirror in `CiQualityGateScriptTests.cs`**, with no single source of truth — the test file's own comment already acknowledges this ("Mirrors EXPECTED_DEPENDENCIES ... so synthetic fixtures satisfy the dependency-boundary validation"). Every future dependency-boundary change (like this session's) requires editing both files in lockstep by hand; a missed edit in one file would silently pass its own regression tests since both copies are asserted against each other, not against real restore output. Consider extracting a shared data file/fixture, or having the test import the script's dict directly, plus adding a negative-path test that asserts the boundary check actually fails when a real project gains an unexpected dependency. Pre-existing design, not introduced by this session's fix. (`scripts/validate-nuget-packages.py`, `tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs`)
- **Dependency-boundary validation is a hardcoded per-package allowlist rather than derived from actual restore/lock output** (e.g. `dotnet list package --include-transitive`) — inherently high-maintenance; this is the second time in this file's history a submodule/package version bump has required a manual allowlist update (see the CI Restore NU1107 memory for the sibling pattern). A more dynamic validation approach would eliminate this class of recurring CI break. Architectural, out of scope for a narrowly-scoped test-fix session. (`scripts/validate-nuget-packages.py`)

## Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)

_Adversarial review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) over the committed diff `088232a..9166f7e` (src + tests + scripts). AC1–AC7 + AC10 and all Preserve constraints verified met against live source. The 1 decision-needed + 6 patch findings are in the story file's `Review Findings — BMAD Code Review (2026-07-20)` section; these six are the deferred low-severity follow-ups._

- **`NextPageAsync` unguarded `NextCursor==null`** — the Next button is gated only on `!_snapshot.HasMore`; a backend contract violation (`HasMore==true` with a null `NextCursor`) would push the current cursor to history and set the cursor to null, bouncing the user to page 1 with a growing back-stack. Defensive only — the platform opaque-cursor contract guarantees a next cursor whenever `HasMore` is true. Follow-up: disable Next on `!HasMore || NextCursor is null`, or guard before consuming. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:484-487`)
- **Grid cannot return to the default `TenantId` ordering except via toolbar Reset** — only `tenant-id`→Name and `tenant-status`→Status are sortable; the `_ => TenantListSortColumns.TenantId` arm of `OnTenantSortChanged` is a defensive fallback (null/unknown `ColumnId`), and FluentDataGrid's 3-state "unsorted" third click cannot be represented (it re-forces Name/Status). While `SortColumn==TenantId` no visible column shows a sort indicator. UX limitation with a workaround (Reset). Follow-up: if return-to-default is desired, add an explicit affordance or map the unsorted event. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` `OnTenantSortChanged`)
- **Brittle source-text "guard" tests + stale resource stub** — several tests grep rendered/source text rather than assert behavior: `grid.ShouldNotContain("Cursor", Case.Insensitive)` (a common CSS/identifier word), `navigation.Split("Cursor = null").Length.ShouldBe(3)` (exact occurrence count), and `workspace.ShouldNotContain("ConfigureAwait(false)")` (source scan, not dispatcher-affinity proof) — they break on unrelated edits and can pass even if behavior regresses via a differently-named channel. Separately, the `TenantsWorkspaceTests` resource stub still defines the old `Tenants.List.ReturnContext` copy (containing "cursor") and removed `Tenants.List.Sort.*` keys, diverging from the corrected production resources. Test tech-debt. (`tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`, `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`)
- **Duplicated tab/scope literal constants across two files** — `TenantsWorkspace.razor` declares `TenantsTabId`/`UsersTabId`/`AllTenantsScope`/`MyTenantsScope` and `TenantWorkspaceState.cs` declares `TenantsTab`/`UsersTab`/`AllScope`/`MyScope` with the same `"tenants"/"users"/"all"/"mine"` values; `ApplyWorkspaceState` compares `state.Tab` (sourced from the state file's consts) against the razor file's consts. Value-equal today, but nothing enforces it — changing one string silently breaks tab/scope routing with no compile error. DRY nit. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:219-222`, `src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs`)
- **Redundant double a11y labeling on badges** — status/pending/truth badges set `IconLabel` **and** container `aria-label` **and** the same visible text; the host `aria-label` subsumes children so `IconLabel` is dead weight today, but if `aria-label` were later removed the icon label would surface a duplicate reading. a11y tidiness. (`src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`, `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor`)
- **Disclosed runtime-verification gaps** — (1) the invalid-cursor page-one recovery wire-path (that the `list-tenants` query actually populates the `reasonCode` problem-details extension the gateway matches on, rather than only `detail`) rests on unit doubles; (2) AC8 per-width and forced-colors behavior is proven by grid-scoped CSS + bUnit/forced-colors conformance rather than full browser emulation, because the local Chrome lane exposes a fixed 1235px virtual viewport (window resize is a no-op). Both are disclosed in the story Debug Log and do not gate this read-only UI story per the Epic 1 convention. (`story evidence`)

## Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)

_Adversarial review (Blind Hunter + Edge Case Hunter + Acceptance Auditor, all Opus 4.8) over committed diff `21a3ce5..41e047e`. The AC5 drill-in + return-context and AC7 focus-anchor code changes verified sound and honest. 2 decision-needed (submodule/evidence integrity; AC5 parity sign-off) and 1 patch (`NormalizeContextValue` length cap) are tracked in the story's `Review Findings — BMAD Code Review (2026-07-21)` section; 6 dismissed as noise/parity/false-premise. This is the one low-severity follow-up._

- **Degenerate/exotic tenant ids on the shared detail-nav path** — (a) `TenantListNavigationContext.ToDetailUrl(TenantListRow)` now delegates to the new `ToDetailUrl(string tenantId, string anchor)` overload whose `ArgumentException.ThrowIfNullOrWhiteSpace(tenantId)` throws on a blank tenant id, where the pre-change inline body silently produced a `/tenants/?returnUrl=…` link; a render-time throw inside the `FluentDataGrid` template would tear down the list surface. (b) The row `id="{SelectorPrefix}-row-{context.TenantId}"` and the `tenants-my-row-{TenantId}` / `tenant-row-{TenantId}` focus anchors are built from the raw tenant id, so an id containing whitespace or CSS-significant characters produces an invalid HTML `id` and a non-resolving return-focus anchor. Both require a blank/exotic tenant id — the tenant id is the validated non-blank aggregate identifier and is slug-like in practice — and both share the pre-existing scope=all `TenantDataGrid` `id="tenant-row-{TenantId}"` pattern. Fix as cross-surface id-safety hardening (guard `DetailHrefFor`/normalize the anchor value across both grids), not a My-Tenants-only divergence. (`src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs:36`; `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor:17`)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
  summary: Audit-grid unsafe references are omitted from copying but remain visible in the rendered reference label.
  evidence: `AuditDataGrid.razor` sanitizes `EventReference` only for `SupportSafeCopyButton`; `ReferenceLabel(context)` still renders the raw reference and context, and the behavior predates this story's compatibility migration.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
  summary: Clipboard module import and write operations are not coordinated with component disposal.
  evidence: `SupportSafeCopyButton.razor` inherited a disposal path that returns while `_module` is null and does not invalidate an import or write already in flight, allowing late interop or disposal races during navigation.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
  summary: Caller-supplied tenant identifiers are reused as raw DOM ids and return-focus anchors.
  evidence: `TenantDataGrid.razor`, `MyTenantsDataGrid.razor`, and `TenantListNavigationContext.cs` embed literal identifiers in anchors, so whitespace or selector-significant characters can make focus restoration unreliable; this navigation pattern predates the copy change.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
  summary: The audit receipt copies a hidden synthesized English composite rather than one exact visible localized literal.
  evidence: `TenantAuditReceipt.CopyableReferenceText` assembles hard-coded English labels and `AuditEvidenceReceipt.razor` approves that non-rendered multiline value, a pre-existing audit behavior exposed by the shared-component migration.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
  summary: Legacy configuration display safety remains a deny-list that can miss unrecognized secret formats.
  evidence: `LegacyConfigurationDisplaySanitizer` preserves the pre-existing command-preview display policy by accepting every non-empty key/value pair that lacks listed fragments, so values such as unknown API-key formats may still render until Story 1.6 supplies a positive safe model.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
  summary: Configuration keys that fail the legacy display-safety policy remain visible even while their paired values are redacted.
  evidence: `TenantConfigurationView.razor` always renders `context.Key`; `LegacyConfigurationDisplaySanitizer.IsDisplayable(key, value)` only controls value replacement, so a key containing a known sensitive literal remains exposed in the DOM and accessibility label. This display behavior predates the copy-policy change.

### DW-1: Follow-up review still recommended for 1-8-support-safe-identifier-copy-and-read-experience-evidence after the damping cap was spent
origin: review-budget-followup
source_spec: `spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
severity: low
reason: The follow-up-review damping cap (limits.max_followup_reviews = 1) was spent with the story finalized (status: done, verify green) while the review pass still recommended an independent follow-up. The work was committed by bmad-loop run 20260721-185843-1016; this entry preserves the lingering recommendation for a deliberate later review.
status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
  summary: The authoritative-search status filter's visible label and accessible name can describe different scopes.
  evidence: `TenantsWorkspace.razor` selects `StatusFilterLabelKey` for the visible label but retains the page-local `Tenants.List.StatusFilterLabel` for `aria-label`; this mismatch predates the current Story 1.9 review-repair diff.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
  summary: An unmapped list reason renders its raw resource key as user-visible copy in the shared list-state surface.
  evidence: `ListSurfaceStates.razor` resolves `Localizer["Tenants.List.Reason.{Reason}"]` for any non-`None` reason, but only 5 of the 10 `TenantListReason` members have a `Tenants.List.Reason.*` key in `TenantsResources.resx`; an unmapped reason therefore renders the literal key in EN and FR. No currently reachable call site passes an unmapped reason, so this is a latent pre-existing trap in the shared component rather than a live defect of this story.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
  summary: The advance-by-requested-window paging rule rests on a Memories server premise that no test in this repository can observe.
  evidence: Correctness of `nextOffset = min(rawOffset + PageSize, TotalCount)` requires the Memories search server to apply `Offset` before dropping entries that fail its required-field check and to report the untrimmed total. That is true of `SyntacticSearchService` in the consumed submodule today, but `SearchResult.TotalCount` documents only "may exceed returned results", every gateway test stubs `MemoriesClient.SearchAsync`, and the intent's Block-If bars editing anything under `references/`. Closing this needs a contract test in the Memories repository or a Tenants integration test against a live index.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
  summary: The tenant-detail read path does not adopt the shared null-member guard, so a malformed member element crashes the detail page while both list surfaces degrade safely.
  evidence: `TenantQueryGateway.HasUsableMembers` is applied to search hydration and ordinary-list enrichment but not to `GetTenantAsync`, which feeds the identical `TenantDetail` payload to `TenantDetailPage.OwnerCount` and `MemberAccessReview.OwnerCount`; both dereference member elements during render, so a `Members` array containing a null element throws `NullReferenceException` and tears down the circuit. `TenantConfigurationSafeComposer.SanitizeDetail` copies the collection and preserves the null element. The detail-page dereference predates Story 1.9; this story only made the asymmetry visible by guarding the two list paths.

## Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
  summary: The "exactly one polite live region" proof is an artefact of bUnit's missing shadow DOM.
  evidence: `TenantListSurfaceTests.cs:1866` counts live regions in markup where `<fluent-message-bar>` renders as an inert custom element. The shipped Fluent v5 module sets `role="status" aria-live="polite"` on each bar's internal dialog at runtime, so a real browser nests a live region per bar inside the workspace's outer one. The helper degenerates to `0.ShouldBe(0)` on the empty-notice call.
  status: open — blocked on BROWSER-SEARCH-1.9 and AT-NVDA-1.9, both already open.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
  summary: Surfacing codec exceptions escape the gateway and reach an unguarded LoadAsync, tearing down the Blazor circuit.
  evidence: `TenantQueryGateway.cs:1019` deliberately re-raises `ObjectDisposedException`, `NullReferenceException`, `ArgumentNullException`, `OutOfMemoryException`; `TenantsWorkspace.razor:632` catches only `OperationCanceledException`. A disposed Data Protection provider during host shutdown therefore kills the circuit where every other cursor-protection failure degrades to the ordinary list. Documented as deliberate in the source comments, and shutdown-time disposal is benign, so recorded rather than patched.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
  summary: The codec argument guard straddles the contained/surfacing partition, so its null and empty halves produce opposite outcomes.
  evidence: `QueryCursorCodec` uses `ArgumentException.ThrowIfNullOrWhiteSpace`, which throws `ArgumentNullException` for null and `ArgumentException` for empty. `TenantQueryGateway.cs:1025` excludes `ArgumentNullException` before the `ArgumentException` base match, so null escapes to circuit teardown while empty degrades to the ordinary list. Not reachable today: `TenantSearchCursorScopes.Create` never returns null and `TenantSearchCursorPosition.Format` never returns empty.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
  summary: The pager is unmounted on every load, dropping keyboard focus from the button the operator just pressed.
  evidence: `TenantsWorkspace.razor:534` sets `_snapshot = TenantListSnapshot.Loading()`, making `ShowList`, `HasMore` and `HasPreviousPage` all false, so `ShowPager` (`:416`) removes the whole `<nav>` for the duration of every load. Needs the authenticated browser lane to confirm the focus consequence.
  status: open — needs BROWSER-SEARCH-1.9.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
  summary: The CI package-boundary gate asserts the fixture it generates from its own allowlist.
  evidence: `CiQualityGateScriptTests.cs:309` mirrors `scripts/validate-nuget-packages.py:64`; `ExpectedDependencies` is used to synthesise the `.nupkg` fixtures fed to the script, so the test verifies only that two copies of the same literal agree, never that `Microsoft.Extensions.Http.Resilience` is genuinely upstream-owned. Widening the allowlist to silence a real leak would pass.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-30240946791-89897853390.md`
  summary: The shared release workflow documents `source-branch` as configurable even though the established publication policy accepts only `main`.
  evidence: `domain-release.yml` and the pre-existing publication preflight reject every source branch except `main`, while the reusable-workflow input description says only that it is an exact protected source branch; resolving that public contract is broader than the stale-release race fix.

## Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27, pass 3)

- source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
  summary: Per-page candidate dedup lets one tenant render on two consecutive authoritative search pages.
  evidence: `TenantQueryGateway.BuildAuthoritativeSearchSnapshotAsync` builds its `seen` set per raw window, so a tenant the index returns in two overlapping windows is rendered twice across consecutive pages. Closing it needs either an index uniqueness guarantee (upstream, barred by this spec's Block-If) or a cross-page seen-set carried in the protected cursor, which would place reconstructable index material into protected state and violate this story's own cursor constraints. Reclassified from patch to deferred during the 2026-07-27 pass-2 application and marked `[x]` at `spec-1-9-…-paging.md:238`, but never entered this ledger — the pass-3 review found it invisible to ledger triage. Recorded here now.
  status: open — blocked on an upstream index uniqueness guarantee or a cursor design that carries no reconstructable index material.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
  summary: A partially hidden search window still advertises, through a live Next control beside its surviving rows, that the window held more than it rendered.
  evidence: `TenantQueryGatewayTests` pins as intended behaviour a window where five of six candidates were dropped (forbidden, not-found, null detail, id mismatch, degraded) yielding one row plus `HasMore = true` and a minted cursor at offset 6. The window-collapse rule at `TenantQueryGateway.cs:864` closes the fully hidden case only. Closing the partial case means not exposing per-page authorized counts through pager state at all. Reviewed with the story owner on 2026-07-27 and accepted as out of scope for this story; tracked in the evidence report as PARTIAL-WINDOW-DISCLOSURE-1.9.
  status: open — accepted out of scope; reopen trigger is any requirement that a partially hidden window be indistinguishable from a complete one.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
  summary: The pass-2 finding "Seven new Lifecycle bindings unverified end-to-end on any real surface" was checked off after closing 1 of 13 binding sites.
  evidence: `grep 'Lifecycle="'` finds 13 sites — `TenantDataGrid.razor:76`, `MyTenantsDataGrid.razor:75`, `AuditDataGrid.razor:54`, `GlobalAdministratorsPage.razor:347`, `TenantDetailPage.razor:115/144/165/186`, `TenantConfigurationView.razor:15/129`, `MemberAccessReview.razor:19/116`, `TenantLifecycleActionAvailability.razor:25`. Only `TenantDataGrid` gained a rendered-lifecycle assertion (`TenantListSurfaceTests.cs:1002-1005`), and it was mutation-verified. `truth-state-badge--*` appears nowhere else outside `TruthStateBadgeTests`, which the original finding already deemed insufficient. The 12 remaining sites are other stories' surfaces (tenant detail, configuration, member review, audit, global administrators), so covering them is not story-1.9 work.
  status: open — the pass-2 checkbox at `spec-1-9-…-paging.md:243` should be corrected to record partial closure.

## Deferred from: code review of 1-6-read-only-tenant-configuration.md (2026-07-27)

- source_spec: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md`
  summary: A third divergent global-administrator claim parser now coexists with the existing two, so the same signed-in user can be a proven administrator for configuration and Indeterminate for tenant lifecycle.
  evidence: `TenantConfigurationPrincipalResolver.cs:102-194` vs `Services/Gateways/TenantsGlobalAdministratorClaims.cs`. Four divergences verified at `ec7ec8c` and still present at HEAD: malformed JSON role array yields Indeterminate in the new resolver but falls through to delimiter parsing in the old one; an unparseable `global_admin` yields Indeterminate vs `false`; `{`-prefixed role values yield Indeterminate vs split-parsed; claims are read across all identities in the old parser but only from the single authenticated identity in the new one. Consolidating into one three-state resolver that the boolean parser collapses would touch lifecycle and global-administrator surfaces owned by other stories.
  status: open — needs a cross-story owner; reopen trigger is any new surface that needs administrator evidence, or a reported disagreement between configuration and lifecycle authorization for the same user.

- source_spec: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md`
  summary: Lifecycle and global-administrator authorization reflections still read `HttpContext.User` with no circuit fallback, while the new configuration path has one.
  evidence: `LifecycleAuthorizationReflection` and `GlobalAdministratorsAuthorizationReflection` resolve `httpContextAccessor?.HttpContext?.User` only; during interactive circuit activity there is no `HttpContext`, so these reflections can disagree with the configuration path for the same user on the same page. Pre-existing before Story 1.6 and outside its declared file scope; Story 1.6 only made the asymmetry visible by adding the circuit-aware path.
  status: open — pre-existing; fold into the claim-parser consolidation above or into Story 1.10/1.11 identity work.

## Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)

- **Partially hidden authoritative-search windows disclose hidden candidates through paging state.** This is the already-recorded Story 1.9 `PARTIAL-WINDOW-DISCLOSURE-1.9` residual: `TenantQueryGateway.cs:890` renders surviving rows while retaining a `HasMore` value derived from the raw pre-authorization total. It is pre-existing relative to the Story 1.6 trust-boundary chunk and remains owned by Story 1.9.
- **Search hydration conflates forbidden and missing candidates when deciding whether to end paging.** `TenantQueryGateway.cs:1013` classifies both 403 and 404 as `HiddenOrAbsent`; an all-404 stale-index window can therefore collapse paging and make later authorized matches unreachable. This is pre-existing relative to Story 1.6 and should be resolved with the Story 1.9 paging contract so anti-enumeration behavior remains coherent.

- source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
  summary: The release tag floor guard probes NuGet only; the container tag in registry.hexalith.com is never checked.
  evidence: publication_preflight.py fails with the same version-collision on the container repository (validate_container_absence), so a partial prior release that left a container tag can still fail the protected job after approval. The unprotected verify-source job has no registry credentials, so covering it needs a design decision.

- source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
  summary: The tag floor is proved in verify-source but never re-proved after the production approval gate.
  evidence: environment-name production can hold for hours; a tag deleted or added during that window reproduces the original incident with a green guard behind it. Re-asserting inside the release job, or pinning the resolved floor as a job output, would close it.

- source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
  summary: The guard fails on any published version above the floor, even when the version semantic-release would actually propose is free, with no override.
  evidence: floor v3.2.18 with a published 3.3.0-3.15.1 band and a breaking change in range proposes 4.0.0, which is free, yet the guard exits 1. There is no workflow_dispatch acknowledgement input, so the only escape is mutating tags.

- source_spec: none
  summary: The release-published tenants container image fails to start under Production defaults, failing the container smoke test and aborting every release after packages are already pushed.
  evidence: Run 30340676669 evidence artifact, smoke-linux-amd64.log - OptionsValidationException requires Authentication:JwtBearer:Authority to be an absolute HTTPS URI (published appsettings.json has "") and requires SigningKey to be empty (it is not, in the container). amd64 exited 139, arm64 hit liveness-timeout. Only host-affecting change since the last successful release b3d01c53 is a7ca142, which moved Hexalith.EventStore.Gateway to a PackageReference on the non-source path, changing which appsettings.json wins in the container publish. Blocks release completion, not just this one.

## Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-28)

- BMAD workflow render files (`_bmad/render/bmad-quick-dev/step-05-present.md`, `step-oneshot.md`, `workflow.md`) were modified inside the Story 1.10 diff, adding gitlink-validator instructions. Real and probably desirable, but it is tooling maintenance unrelated to Story 1.10's acceptance criteria and outside the spec's authorized doc outputs (evidence file + `tests/test-summary.md`). Should land as its own `chore`/`docs` commit rather than inside a feature story.
- **Deferred to Story 1.11** — Principal-resolution precedence was inverted in `TenantConfigurationPrincipalResolver.cs:17-48`: the circuit `AuthenticationStateProvider` now outranks `HttpContext.User`, where previously `HttpContext` was primary. A circuit whose provider returns an anonymous or not-yet-populated state while `HttpContext.User` is authenticated collapses to `Indeterminate` and fails every configuration grant closed. Security-relevant; must be decided against Story 1.11's acceptance criteria, not 1.10's.
- **Deferred to Story 1.11** — `TenantsGlobalAdministratorClaims.Evaluate` now requires exactly one authenticated identity carrying exactly one literal `sub` claim (`TenantsGlobalAdministratorClaims.cs:36-46`). Any handler mapping `sub` to `ClaimTypes.NameIdentifier` (the ASP.NET default), or any principal with two authenticated identities (cookie + bearer), denies a genuine global administrator. Confirm the intended claim contract against `docs/production-auth-claim-contract.md` as part of 1.11.
