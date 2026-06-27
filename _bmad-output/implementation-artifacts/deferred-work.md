# Deferred Work

Updated: 2026-06-21 by Correct Course (deferred + pending work implementation).
Source proposals:
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-19-deferred-work.md` (original triage),
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-21-deferred-and-pending-work-implementation.md` (this run).

This file is now a routing index. Original review detail remains in the source story/spec artifacts; open items here must point to a Tenants story, a FrontComposer owner handoff, an EventStore owner handoff, or a stale/resolved record.

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

- **`<FcContentLabel>` single-writer dispose-clobber** — when two `<FcContentLabel>` markers render on one page, disposing one calls `FcContentLabelCoordinator.Reset()` (→ `Set(null, null)`), wiping a still-live sibling's accessible name on `#fc-main-content` until the survivor happens to re-render (`references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcContentLabelCoordinator.cs:159` + `FcContentLabel.razor.cs:80-84`). Real but silent a11y edge case; it faithfully mirrors the accepted, documented `FcPageLayoutCoordinator` "single-writer, last-writer-wins" pattern (identical latent limitation by design) and no current consumer renders two markers. Fix path if multi-writer support is ever needed: add a writer-identity/token guard so only the current writer's dispose resets — apply to BOTH coordinators together for consistency.
- **Page-driven `<FcContentLabel>` accessible name absent on server first paint** — registration is `OnAfterRender`-only (`FcContentLabel.razor.cs:67-77`), so on a static-SSR/prerender pass `#fc-main-content` emits no `aria-label`/`aria-labelledby` from the page-marker path; the name appears only after interactive hydration. The shell-parameter path (`ContentLabel`/`ContentLabelledBy`) is correct on first paint. Mirrors the established `FcPageLayout` coordinator pattern and is acceptable for this InteractiveServer library; recommend documenting the limitation in the `FcContentLabel` XML remarks.
- **`FocusHeadingAsync()` no-op → throw is an undisclosed API behavior change** (resolved `decision-needed` → defer by Administrator on 2026-06-25). Keep the diagnostic throw: it is the intended hardening (Requested outcome 5) and no live consumer regresses (`TenantsWorkspace` → `FcAggregateListPage` passes `HeadingTabIndex="-1"`, verified). Follow-up: document the no-op→throw change for external FrontComposer adopters in the changelog / `FcPageHeader.FocusHeadingAsync` remarks, and note that the `FcAggregateListPage` wrapper's `… ?? ValueTask.CompletedTask` only guards the pre-first-render null `@ref` window, not the new throw. `FcPageHeader.razor.cs:104-117`, `FcAggregateListPage.razor.cs:83-84`. FrontComposer submodule.

## Deferred from: code review of cc-2026-06-21-eventstore-admin-ui-a11y-remediation (2026-06-25)

_Reviewed EventStore commit `d6d3ea69` (`feat(admin-ui): keyboard-accessible interactive semantics across Admin.UI`, already on EventStore `main`). The item lives in the `Hexalith.EventStore` submodule; any fix is a new follow-up commit, not an amendment._

- **SVG `<g tabindex="0">` focusability not guaranteed cross-browser** (`references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Components/StorageTreemap.razor:72`) — the treemap cells became focusable via `tabindex="0"` on an SVG `<g>` group. Modern Chromium/Edge/Firefox include tabindex'd SVG container elements in the tab order; Safari/older WebKit historically do not, which would leave the treemap cells (and their `role="button"` keyboard activation) unreachable by Tab there. For an internal EventStore Admin.UI targeting Chromium/Edge the practical risk is low. Follow-up: validate against the actual supported browser matrix; if Safari/WebKit must be supported, make the focusable element an SVG `<a>` or wrap an HTML control in `<foreignObject>`. The bUnit test only asserts the attribute is present, not that the browser focuses it.

## Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace (2026-06-27)

_Adversarial review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) over the uncommitted working tree. All 14 ACs verified met; this is the single deferred follow-up._

- **Global Administrators / Audit discoverability after nav de-listing** — the approved 2026-06-27 IA (AC9) removed `/global-administrators` and audit from the Tenants left-menu; the routes, pages, and `GlobalAdministratorPolicy` are preserved, but the diff adds no module-internal/contextual entry point, so a global administrator can reach the surface only by typing the URL. The sprint-change-proposal explicitly defers this: GA/Audit "remain available through module-internal tabs or contextual entry points ... unless a future module-level IA decision adds them explicitly." Follow-up: when Product confirms the contextual entry-point IA, add a discoverable in-workspace path. (`src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`)

## Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Group 1 re-review (2026-06-27, chunked)

_Chunked post-commit re-review (`ba14356..HEAD`), Group 1 = UI workspace & panels. Groups 2 (server freshness + gateway), 3 (tests), 4 (docs) pending separate runs._

- **GlobalAdministratorPolicy now registered but unconsumed** — extends the GA discoverability item above: after the nav `RequiredPolicy:` was removed, `Program.cs:33` still registers `Tenants.GlobalAdministrator` but nothing requires it (the GA page authorizes via `BffComposition` reflection). Retention is intentional pending the deferred contextual-entry-point IA decision; revisit (wire or remove) when that decision lands. (`src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`, `src/Hexalith.Tenants.UI/Program.cs:33`)
- **Create-tenant freshness gate narrowed `Current or Unknown` → `Current`** (decision deferred, reason: _check in next review_) — `TenantsWorkspace.razor:117` now gates Create off whenever list freshness is `Unknown` (header absent / degraded / possible first-tenant empty-list bootstrap). Bundled freshness-behavior change overlapping `cc-2026-06-25`. Revisit alongside the Group 2 server-freshness header review: either confirm the empty/bootstrap path resolves to `Current` (keep the tightening + add a regression test) or restore `Current or Unknown`. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:117`)
- **Page-local tabs render empty tabpanels** — the new `FluentTabs` carry `Id`/`Header` only; active content renders in sibling `FcAggregateListPage` slots (`Body`/`Filters`/`States`), so the Fluent tab→tabpanel ARIA relationship points at empty regions. `aria-selected` is correct and tabs are keyboard reachable. This is an `FcAggregateListPage`-slot architectural nuance best owned upstream. Follow-up: FrontComposer/UX decision on associating `FcAggregateListPage` content with `FcPageToolbar`/tab tabpanels. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:28-30`)
