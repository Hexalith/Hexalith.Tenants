# Deferred Work

### DW-1: Follow-up review still recommended for 1-8-support-safe-identifier-copy-and-read-experience-evidence after the damping cap was spent
origin: review-budget-followup
source_spec: `spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
severity: low
reason: The follow-up-review damping cap (limits.max_followup_reviews = 1) was spent with the story finalized (status: done, verify green) while the review pass still recommended an independent follow-up. The work was committed by bmad-loop run 20260721-185843-1016; this entry preserves the lingering recommendation for a deliberate later review.
status: done 2026-08-27
resolution: resolved by sweep bundle dw-support-safe-copy-followup
resolution-undo: e32f9ec5cfa74f6e713b0d8ce939f3393f91d53d4b83c40ea3313864db2fc699 2026-08-27 7374617475733a206f70656e

### DW-2: Global-administrator pagination >20 admins — fail-OPEN CLOSED (full paging redesign still routed)
origin: migrated from legacy ledger ("2026-07-01 Correct Course — Deferred Work (pagination fail-closed + submodule doc handoffs)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs
reason: The legacy ledger defers this issue: Global-administrator pagination >20 admins — fail-OPEN CLOSED (full paging redesign still routed). Original context is preserved in legacy-detail.
legacy-detail: - **Global-administrator pagination >20 admins — fail-OPEN CLOSED (full paging redesign still routed).** `GlobalAdministratorCorrectionSnapshot` now treats absence as conclusive only when the whole fixed projection is loaded (`!HasMore`): `EvaluateCurrentProjection` fails closed to `UnableToVerify` (`Tenants.Correction.Unavailable.CurrentProjectionUnavailable`) for a restore/revoke whose target is absent from an incomplete page, and `ConfirmProjection` proves a revoke only on `!present && !HasMore`, killing the false-`Confirmed` on a revoke of a page-2 administrator. Presence-found stays conclusive, so page-1 corrections at scale are unaffected. +3 tests + `PagedProjectionReady` helper. The multi-page load/aggregation that would let a page-2 correction actually RUN (rather than be conservatively blocked) stays routed to a dedicated projection-paging story. (`src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs`)
status: done 2026-08-27
resolution: already resolved: commit 7716f0b11423eb54d74935b6cc6e3edf405dc400; src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs:23-89 now walks and aggregates all stable cursor pages.

### DW-3: FrontComposer `FcContentLabel` single-writer dispose-clobber + server first-paint — DOCUMENTED
origin: migrated from legacy ledger ("2026-07-01 Correct Course — Deferred Work (pagination fail-closed + submodule doc handoffs)"), 2026-08-25
location: FcContentLabel
reason: The legacy ledger defers this issue: FrontComposer `FcContentLabel` single-writer dispose-clobber + server first-paint — DOCUMENTED. Original context is preserved in legacy-detail.
legacy-detail: - **FrontComposer `FcContentLabel` single-writer dispose-clobber + server first-paint — DOCUMENTED.** XML `<remarks>` on `FcContentLabel` (plus a matching sentence on `FcContentLabelCoordinator`) now record the last-writer-wins dispose-clobber and the `OnAfterRender`-only first-paint limitation, naming the shell-parameter path (`ContentLabel`/`ContentLabelledBy`) as the first-paint-correct alternative. Doc-only.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcContentLabel.razor.cs:20-33 documents the single-writer dispose-clobber and InteractiveServer first-paint limitation.

### DW-4: FrontComposer `FcPageHeader.FocusHeadingAsync` no-op→throw — DOCUMENTED
origin: migrated from legacy ledger ("2026-07-01 Correct Course — Deferred Work (pagination fail-closed + submodule doc handoffs)"), 2026-08-25
location: FcPageHeader.FocusHeadingAsync
reason: The legacy ledger defers this issue: FrontComposer `FcPageHeader.FocusHeadingAsync` no-op→throw — DOCUMENTED. Original context is preserved in legacy-detail.
legacy-detail: - **FrontComposer `FcPageHeader.FocusHeadingAsync` no-op→throw — DOCUMENTED.** An adopter-facing behavior-change note was added to the method `<remarks>` (there is no FrontComposer CHANGELOG), incl. the caveat that the `FcAggregateListPage` wrapper's `?? ValueTask.CompletedTask` guards only the null-`@ref` window, not the throw.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs:94-126 documents and enforces the no-op-to-throw behavior.

### DW-5: EventStore `StorageTreemap` SVG `<g tabindex>` cross-browser — DOCUMENTED
origin: migrated from legacy ledger ("2026-07-01 Correct Course — Deferred Work (pagination fail-closed + submodule doc handoffs)"), 2026-08-25
location: StorageTreemap
reason: The legacy ledger defers this issue: EventStore `StorageTreemap` SVG `<g tabindex>` cross-browser — DOCUMENTED. Original context is preserved in legacy-detail.
legacy-detail: - **EventStore `StorageTreemap` SVG `<g tabindex>` cross-browser — DOCUMENTED.** A Razor comment above the focusable cell records the Chromium/Edge/Firefox-vs-Safari/WebKit tab-order caveat and the `<a>`/`<foreignObject>` remedy if WebKit support is required.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Components/StorageTreemap.razor:65-76 documents the WebKit caveat and alternatives.

### DW-6: JSDisconnectedException guard on panel focus
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: CorrectionStartPanel
reason: The legacy ledger defers this issue: JSDisconnectedException guard on panel focus. Original context is preserved in legacy-detail.
legacy-detail: - **JSDisconnectedException guard on panel focus** — both `CorrectionStartPanel` and `GlobalAdministratorCorrectionPanel` `OnAfterRenderAsync` now wrap `_lifecycleElement.FocusAsync()` in `try/catch (JSDisconnectedException)` (parity with the existing `TenantAuditPage` guards).
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:322-334 and GlobalAdministratorCorrectionPanel.razor:279-291 catch JSDisconnectedException around focus.

### DW-7: Page-load global-admin query unguarded
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: TenantAuditPage.LoadAsync
reason: The legacy ledger defers this issue: Page-load global-admin query unguarded. Original context is preserved in legacy-detail.
legacy-detail: - **Page-load global-admin query unguarded** — `TenantAuditPage.LoadAsync` now wraps the supplementary global-administrator enrichment in `catch (… EventStoreGatewayException or HttpRequestException or JsonException)`; the confirm-time path (`OpenCorrectionAsync` / panel `ProjectionRefreshProvider`) keeps propagating. Regression test: `Tenant_audit_page_survives_global_administrator_projection_fault_during_load`.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:499-513 guards supplementary global-administrator loading; TenantAuditPageTests.cs:801 covers the regression.

### DW-8: Tenant panel terminal-state focus parity
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: CorrectionStartPanel.SetSnapshot
reason: The legacy ledger defers this issue: Tenant panel terminal-state focus parity. Original context is preserved in legacy-detail.
legacy-detail: - **Tenant panel terminal-state focus parity** — `CorrectionStartPanel.SetSnapshot` now focuses on all six terminal states (Confirmed/Failed/Rejected/Degraded/UnableToVerify/AlreadyApplied), matching the GA panel. Test: `Panel_rejected_terminal_state_moves_focus_to_lifecycle`.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:361-373 covers all terminal states; CorrectionStartPanelTests.cs:420 covers rejected-state focus.

### DW-9: Tenant confirm fail-closed on stale/degraded
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: TenantAuditPage.RefreshTenantProjectionAsync
reason: The legacy ledger defers this issue: Tenant confirm fail-closed on stale/degraded. Original context is preserved in legacy-detail.
legacy-detail: - **Tenant confirm fail-closed on stale/degraded** — `TenantAuditPage.RefreshTenantProjectionAsync` (the tenant confirm-time provider) returns the projection only when `Freshness is Current`, else `null`, so the existing `ConfirmProjection(null)` fails closed (parity with the GA `Freshness=Current` gate). Test: `Panel_does_not_confirm_when_projection_refresh_provider_returns_no_fresh_projection`.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1042-1062 returns confirmation evidence only for Current freshness; CorrectionStartPanelTests.cs:451 covers fail-closed behavior.

### DW-10: Tenant corrective-proof time tie-back + invariant culture
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: CorrectionStartPanel.QueryCorrectiveProofAsync
reason: The legacy ledger defers this issue: Tenant corrective-proof time tie-back + invariant culture. Original context is preserved in legacy-detail.
legacy-detail: - **Tenant corrective-proof time tie-back + invariant culture** — `CorrectionStartPanel.QueryCorrectiveProofAsync` now parses `originalTimestamp` with `InvariantCulture`+`RoundtripKind`, lower-bounds the audit query with `From: originalTimestamp`, filters `row.Timestamp > originalTimestamp`, newest-first; `ProofTimestampLabel` and `TenantCorrectionPreviewSnapshot.WithCorrectiveProof` parse with `InvariantCulture` (mirrors the GA fix). Test: `Panel_proof_lookup_ignores_audit_row_not_newer_than_the_original_event`.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:531-575 uses invariant parsing and strictly newer proof rows; CorrectionStartPanelTests.cs:478 rejects historical rows.

### DW-11: Concurrent correction opens out of order
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: OpenCorrectionAsync
reason: The legacy ledger defers this issue: Concurrent correction opens out of order. Original context is preserved in legacy-detail.
legacy-detail: - **Concurrent correction opens out of order** — `OpenCorrectionAsync` captures a `_correctionOpenGeneration` synchronously at entry and applies the active intent only if still latest. (No dedicated bUnit test — timing-deterministic two-open harness was judged more flake-prone than valuable; verified by construction + unchanged single-open tests.)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:893-925 uses _correctionOpenGeneration so only the newest concurrent open applies.

### DW-12: No story-specific 5.7 gateway-routing test
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: TenantCommandGatewayTests
reason: The legacy ledger defers this issue: No story-specific 5.7 gateway-routing test. Original context is preserved in legacy-detail.
legacy-detail: - **No story-specific 5.7 gateway-routing test** — CLOSED as already-covered: `TenantCommandGatewayTests` already pins the full `system / global-administrators / global-administrators` triple + CommandType + literal payload for both Set and Remove; the item was conditional on the gateway being touched (it wasn't).
status: done 2026-08-25
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs:31-67 pins the fixed aggregate and both command types.

### DW-13: Create-tenant freshness gate narrowed `Current or Unknown → Current`
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: TenantsWorkspace.razor
reason: The legacy ledger defers this issue: Create-tenant freshness gate narrowed `Current or Unknown → Current`. Original context is preserved in legacy-detail.
legacy-detail: - **Create-tenant freshness gate narrowed `Current or Unknown → Current`** — CLOSED as resolved: the gate is back to `Current or Unknown` (`TenantsWorkspace.razor` `CreateTenantFlow IsFresh`), matching the documented first-tenant bootstrap exception. The "restore" path was taken.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:416-425 restores Current or the documented authoritative first-tenant Unknown bootstrap case.

### DW-14: FrontComposer owner handoff
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
location: Hexalith.FrontComposer
reason: The legacy ledger defers this issue: FrontComposer owner handoff. Original context is preserved in legacy-detail.
legacy-detail: - **FrontComposer owner handoff** `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening` — **IMPLEMENTED** in `Hexalith.FrontComposer`. `FcPageHeader` no longer emits a competing `banner` (header root is `role="presentation"`); `FrontComposerShell` exposes `ContentLabel`/`ContentLabelledBy` + a new `FcContentLabel` marker so a page can name the shell `main` landmark without an orphaned page-level `aria-labelledby`; blank `Heading` now fail-safes (no dangling `<h1>`, replacing the prior throw); `FocusHeadingAsync()` fails diagnostically when the heading is not focusable. Backward-compatible (new params default to null). FrontComposer Shell suite 1962/0 failed.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:141-148 owns the named main landmark; FcPageHeader.razor:7-44 implements presentation and blank-heading outcomes.

### DW-15: EventStore owner handoff
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
location: Index.razor; Commands.razor
reason: The legacy ledger defers this issue: EventStore owner handoff. Original context is preserved in legacy-detail.
legacy-detail: - **EventStore owner handoff** `eventstore-2026-06-19-admin-ui-and-query-record-followup` — **IMPLEMENTED** (Admin.UI a11y portion) in `Hexalith.EventStore`: `Index.razor` stat cards, `ActivityChart` (`role="group"` + real `<button>` bars), `StorageTreemap` (focusable `role="button"` cells), `RelatedTypeList`, `TypeDetailPanel`, `DaprHealthHistory`, and non-functional `cursor:pointer` spans on `Commands.razor`/`Events.razor` all remediated; conformance carve-out comment updated. The retired actor-routing sub-item was already verified stale/resolved (see below). Admin.UI.Tests green except 6 pre-existing unrelated `Dw5GovernanceAtddTests` (missing DW5 evidence artifact, not introduced here).
status: done 2026-08-25
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Pages/Index.razor:25-51, Components/ActivityChart.razor:28-46, and Components/StorageTreemap.razor:65-76 contain the fixes.

### DW-16: EventStore owner handoff
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
location: IReadModelFreshness
reason: The legacy ledger defers this issue: EventStore owner handoff. Original context is preserved in legacy-detail.
legacy-detail: - **EventStore owner handoff** `eventstore-2026-06-19-read-model-freshness-metadata` — **IMPLEMENTED** in `Hexalith.EventStore.Client.Projections`: `IReadModelFreshness` (`ProjectedAt`/`ProjectionVersion`), `ReadModelFreshnessState`, `ReadModelFreshnessThresholds`, pure `ReadModelFreshness.Classify/Age`, plus `IReadModelStore.GetWithFreshnessAsync<T>()` and `ToQueryResponseMetadata()` bridges. This is the generic, persisted-timestamp replacement for the Tenants hand-rolled `TenantFreshnessState`; Tenants-side adoption is implemented by `cc-2026-06-25-tenant-read-model-freshness-adoption`. Client.Tests 462/462.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/ReadModels/IReadModelFreshness.cs:22-34, ReadModelFreshness.cs:30-48, and ReadModelFreshnessExtensions.cs:23-85 implement the shared freshness contract.

### DW-17: Epic 11 — Production Authorization Readiness (persisted DataProtection key ring)
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
location: src/Hexalith.Tenants/Program.cs; statestore.yaml
reason: The legacy ledger defers this issue: Epic 11 — Production Authorization Readiness (persisted DataProtection key ring). Original context is preserved in legacy-detail.
legacy-detail: - **Epic 11 — Production Authorization Readiness (persisted DataProtection key ring)** — **IMPLEMENTED**. A Dapr-state-store-backed `IXmlRepository` (`DaprXmlRepository`) + `AddEventStoreDataProtection(...)` live in the `Hexalith.EventStore.DomainService` host-SDK layer; backend is chosen by `statestore.yaml` (Redis in prod) so the Tenants domain package gains NO infra SDK. `src/Hexalith.Tenants/Program.cs` swaps to `AddEventStoreDataProtection(config, "Hexalith.Tenants")`; production persists to the `statestore` under the application-specific key `hexalith-tenants-dataprotection-keys`, Development stays explicitly ephemeral. DomainService.Tests 36/36 (incl. cross-replica reload + ETag concurrency).
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants/Program.cs:79-87 and EventStoreDataProtectionServiceCollectionExtensions.cs:57-93 configure Dapr-backed or explicit ephemeral DataProtection; deploy/dapr/statestore.yaml:3-8 documents the key.

### DW-18: Pending (newly discovered) — Memories-integration doc/test drift
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
location: docs/cross-aggregate-timing.md; docs/sample-consuming-service-walkthrough.md
reason: The legacy ledger defers this issue: Pending (newly discovered) — Memories-integration doc/test drift. Original context is preserved in legacy-detail.
legacy-detail: - **Pending (newly discovered) — Memories-integration doc/test drift** — **FIXED**. The committed Memories search-index integration added the local Memories app id to the AppHost `pubsub.yaml` scopes and 4 `MemoriesSearchIndexEventPublisher` handlers to the Sample program, but left 3 conformance/doc tests red on `main`. Updated `EventPublicationConfigurationTests` + `CrossAggregateTimingDocumentationTests` (local now scopes `memories`; production stays `eventstore`+`sample`), `docs/cross-aggregate-timing.md`, and `docs/sample-consuming-service-walkthrough.md`. Tenants Server.Tests back to 700/700.
status: done 2026-06-21
resolution: Legacy completion record: - **Pending (newly discovered) — Memories-integration doc/test drift** — **FIXED**. The committed Memories search-index integration added the local Memories app id to the AppHost `pubsub.yaml` scopes and 4 `MemoriesSearchIndexEventPublisher` handlers to the Sample program, but left 3 conformance/doc tests red on `main`. Updated `EventPublicationConfigurationTests` + `CrossAggregateTimingDocumentationTests` (local now scopes `memories`; production stays `eventstore`+`sample`), `docs/cross-aggregate-timing.md`, and `docs/sample-consuming-service-walkthrough.md`. Tenants Server.Tests back to 700/700.

### DW-19: Freshness is no longer derived from response `ServedAt`. The implemented direct-read rule treats a real read-model ETag/projection version as `current`; absent markers resolve to `unknown`
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
location: ServedAt
reason: The legacy ledger defers this issue: Freshness is no longer derived from response `ServedAt`. Original context is preserved in legacy-detail.
legacy-detail: - Freshness is no longer derived from response `ServedAt`. The implemented direct-read rule treats a real read-model ETag/projection version as `current`; absent markers resolve to `unknown`.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants/Queries/TenantQueryResult.cs:38-60 consumes IReadModelFreshness; TenantQueryFreshnessTests.cs:35-85 proves ProjectedAt classification and ServedAt response timing.

### DW-20: Generic projection age/version metadata is not available from the current `IReadModelStore` contract. Do not add Tenants-owned generic persistence scaffolding; the remaining threshold-based age metadata need is routed to the EventStore owner handoff below
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
location: IReadModelStore
reason: The legacy ledger defers this issue: Generic projection age/version metadata is not available from the current `IReadModelStore` contract. Original context is preserved in legacy-detail.
legacy-detail: - Generic projection age/version metadata is not available from the current `IReadModelStore` contract. Do not add Tenants-owned generic persistence scaffolding; the remaining threshold-based age metadata need is routed to the EventStore owner handoff below.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/ReadModels/IReadModelFreshness.cs:22-34, ReadModelFreshness.cs:30-48, and ReadModelFreshnessExtensions.cs:23-85 implement the shared freshness contract.

### DW-21: Null/empty read-model ETag behavior is explicit and tested: successful REST reads return 200 with no ETag, no projection-version header, no served-at header, and no 304 support
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Null/empty read-model ETag behavior is explicit and tested: successful REST reads return 200 with no ETag, no projection-version header, no served-at header, and no 304 support. Original context is preserved in legacy-detail.
legacy-detail: - Null/empty read-model ETag behavior is explicit and tested: successful REST reads return 200 with no ETag, no projection-version header, no served-at header, and no 304 support.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants/Queries/TenantQueryResult.cs:18-35 emits no metadata when the legacy ETag-only path has no usable ETag; TenantQueryFreshnessTests.cs:70-85 covers persisted freshness.

### DW-22: ETag handling is hardened and tested for weak tags, `*`, escaped strong tags, and unsupported multi-tag input
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: ETag handling is hardened and tested for weak tags, `*`, escaped strong tags, and unsupported multi-tag input. Original context is preserved in legacy-detail.
legacy-detail: - ETag handling is hardened and tested for weak tags, `*`, escaped strong tags, and unsupported multi-tag input.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:610-650 parses EntityTagHeaderValue and rejects weak, wildcard, empty, duplicate, quoted, and control-bearing validators; TenantsRestQueryClientTests.cs:1165-1469 covers them.

### DW-23: REST/handler read-model reconstruction coverage now proves a recreated controller factory can serve the persisted read model from the shared store and honor 304 through the production REST/handler path
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: REST/handler read-model reconstruction coverage now proves a recreated controller factory can serve the persisted read model from the shared store and honor 304 through the production REST/handler path. Original context is preserved in legacy-detail.
legacy-detail: - REST/handler read-model reconstruction coverage now proves a recreated controller factory can serve the persisted read model from the shared store and honor 304 through the production REST/handler path.
status: done 2026-08-25
resolution: already resolved: tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs:793-833 exercises the production conditional path and complete 304 metadata contract.

### DW-24: Live populated-correlation gateway error coverage now asserts that `correlationId`, `reasonCode`, raw payload text, stack traces, tokens, cursors, and ETags do not reach user-facing copy
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
location: correlationId
reason: The legacy ledger defers this issue: Live populated-correlation gateway error coverage now asserts that `correlationId`, `reasonCode`, raw payload text, stack traces, tokens, cursors, and ETags do not reach user-facing copy. Original context is preserved in legacy-detail.
legacy-detail: - Live populated-correlation gateway error coverage now asserts that `correlationId`, `reasonCode`, raw payload text, stack traces, tokens, cursors, and ETags do not reach user-facing copy.
status: done 2026-08-25
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs:206-237 proves raw payload, token, secret user, and correlation data never reach SafeMessage.

### DW-25: Current full-suite evidence (corrected 2026-06-21): the earlier `Server.Tests` blocker — 3 DAPR component expectation tests asserting removed `enableDeadLetter` / `deadLetterTopic` metadata — was resolved on 2026-06-20 by `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`; full `Server.Tests` now passes 700/700. `IntegrationTests` passes with DAPR/Aspire/performance skips. The old health-readiness blocker wording is no longer current evidence
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
location: Server.Tests
reason: The legacy ledger defers this issue: Current full-suite evidence (corrected 2026-06-21): the earlier `Server.Tests` blocker — 3 DAPR component expectation tests asserting removed `enableDeadLetter` / `deadLetterTopic` metadata — was resolved on 2026-06-20 by `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`; full `Server.Tests` now passes 700/700. Original context is preserved in legacy-detail.
legacy-detail: - Current full-suite evidence (corrected 2026-06-21): the earlier `Server.Tests` blocker — 3 DAPR component expectation tests asserting removed `enableDeadLetter` / `deadLetterTopic` metadata — was resolved on 2026-06-20 by `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`; full `Server.Tests` now passes 700/700. `IntegrationTests` passes with DAPR/Aspire/performance skips. The old health-readiness blocker wording is no longer current evidence.
status: done 2026-08-25
resolution: already resolved: tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs:114-172 guards removal of inert Dapr dead-letter metadata and the exact component-scope contract.

### DW-26: Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18. Original context is preserved in legacy-detail.
legacy-detail: - Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18.
status: done 2026-08-27
resolution: already resolved: _bmad-output/implementation-artifacts/spec-frontcomposer-fluent-structural-and-style-conformance-sweep.md:145 records the independent 2026-06-18 three-layer code review and reproduced verification.

### DW-27: Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18. Original context is preserved in legacy-detail.
legacy-detail: - Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18.
status: done 2026-08-27
resolution: already resolved: _bmad-output/implementation-artifacts/spec-frontcomposer-shell-and-adminui-fluent-conformance-audit.md:246-248 records the 2026-06-18 independent review resolving findings and advancing the story to done.

### DW-28: Compact non-zero spacing (e.g. `margin:0.5rem`/`padding:0.5rem`) is now flagged by the styling-ownership guard. The `(?!0)` zero-skip was replaced with a zero-token matcher that still skips genuine resets (`0`, `0 0 0 0`, `0px`, `0 !important`). No real component CSS regressed
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Compact non-zero spacing (e.g. Original context is preserved in legacy-detail.
legacy-detail: - Compact non-zero spacing (e.g. `margin:0.5rem`/`padding:0.5rem`) is now flagged by the styling-ownership guard. The `(?!0)` zero-skip was replaced with a zero-token matcher that still skips genuine resets (`0`, `0 0 0 0`, `0px`, `0 !important`). No real component CSS regressed.
status: done 2026-08-25
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:102-109 and 632-661 pin compact nonzero versus true-zero behavior.

### DW-29: The inline-style guard was widened beyond flex/grid/gap to also cover spacing (margin/padding), sizing (width/inline-size), and alignment (justify-content/align-items), and now scans both quote styles. No `.razor` carries inline `style=`
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: razor
reason: The legacy ledger defers this issue: The inline-style guard was widened beyond flex/grid/gap to also cover spacing (margin/padding), sizing (width/inline-size), and alignment (justify-content/align-items), and now scans both quote styles. Original context is preserved in legacy-detail.
legacy-detail: - The inline-style guard was widened beyond flex/grid/gap to also cover spacing (margin/padding), sizing (width/inline-size), and alignment (justify-content/align-items), and now scans both quote styles. No `.razor` carries inline `style=`.
status: done 2026-08-25
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:85-97,492-511,665-679 enforce widened inline-layout spacing, sizing, and alignment cases.

### DW-30: The `<div>`/`<span>` budget now excludes Razor (`@* *@`) and HTML (`<!-- -->`) comments before counting
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: The `<div>`/`<span>` budget now excludes Razor (`@* *@`) and HTML (`<!-- -->`) comments before counting. Original context is preserved in legacy-detail.
legacy-detail: - The `<div>`/`<span>` budget now excludes Razor (`@* *@`) and HTML (`<!-- -->`) comments before counting.
status: done 2026-08-25
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:692-700,844-847 strips comments before wrapper counting.

### DW-31: `fc-css-exception` scoping decision: kept RULE-level with documented rationale; a unit test proves a marker exempts only its own rule and does not leak to the next rule
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: `fc-css-exception` scoping decision: kept RULE-level with documented rationale; a unit test proves a marker exempts only its own rule and does not leak to the next rule. Original context is preserved in legacy-detail.
legacy-detail: - `fc-css-exception` scoping decision: kept RULE-level with documented rationale; a unit test proves a marker exempts only its own rule and does not leak to the next rule.
status: done 2026-08-25
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:561-564,705-718 scopes exceptions so markers cannot leak.

### DW-32: `:focus-visible` exemption decision: NARROWED. The blanket exemption was removed; focus-ring affordances (outline/outline-offset/outline-color) are untracked so genuine focus rules still pass, but a `:focus-visible` rule that owns layout/spacing/typography is now flagged unless documented
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: `:focus-visible` exemption decision: NARROWED. Original context is preserved in legacy-detail.
legacy-detail: - `:focus-visible` exemption decision: NARROWED. The blanket exemption was removed; focus-ring affordances (outline/outline-offset/outline-color) are untracked so genuine focus rules still pass, but a `:focus-visible` rule that owns layout/spacing/typography is now flagged unless documented.
status: done 2026-08-25
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:565-568,723-735 permits focus-visible affordances while catching layout ownership.

### DW-33: `RemoveForcedColorsMediaBlocks` now skips braces inside CSS comments and quoted strings so a stray brace cannot leak the block tail back into the scan
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: RemoveForcedColorsMediaBlocks
reason: The legacy ledger defers this issue: `RemoveForcedColorsMediaBlocks` now skips braces inside CSS comments and quoted strings so a stray brace cannot leak the block tail back into the scan. Original context is preserved in legacy-detail.
legacy-detail: - `RemoveForcedColorsMediaBlocks` now skips braces inside CSS comments and quoted strings so a stray brace cannot leak the block tail back into the scan.
status: done 2026-08-25
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:740-753,771-841 implements and tests brace-aware parsing.

### DW-34: `MemberAccessReview` gained bUnit coverage proving the change-role and remove-member `aria-controls` resolve to a rendered active-region `id` after the FluentStack migration
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: MemberAccessReview
reason: The legacy ledger defers this issue: `MemberAccessReview` gained bUnit coverage proving the change-role and remove-member `aria-controls` resolve to a rendered active-region `id` after the FluentStack migration. Original context is preserved in legacy-detail.
legacy-detail: - `MemberAccessReview` gained bUnit coverage proving the change-role and remove-member `aria-controls` resolve to a rendered active-region `id` after the FluentStack migration.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:182-195,241,262 names controlled regions; TenantDetailSurfaceTests.cs:3979-4009 verifies resolution.

### DW-35: `TenantAuditPage` renders a localized fallback (`Tenants.Audit.UnknownTenant`) for a blank/whitespace `TenantId` instead of a dangling heading
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: TenantAuditPage
reason: The legacy ledger defers this issue: `TenantAuditPage` renders a localized fallback (`Tenants.Audit.UnknownTenant`) for a blank/whitespace `TenantId` instead of a dangling heading. Original context is preserved in legacy-detail.
legacy-detail: - `TenantAuditPage` renders a localized fallback (`Tenants.Audit.UnknownTenant`) for a blank/whitespace `TenantId` instead of a dangling heading.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:269-272 supplies the localized fallback; TenantAuditPageTests.cs:783-797 covers blank IDs.

### DW-36: The claim that the styling scan is blind to declarations inside `@media` blocks was verified as a false positive in the structural/style story and is not open work
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: The claim that the styling scan is blind to declarations inside `@media` blocks was verified as a false positive in the structural/style story and is not open work. Original context is preserved in legacy-detail.
legacy-detail: - The claim that the styling scan is blind to declarations inside `@media` blocks was verified as a false positive in the structural/style story and is not open work.
status: done 2026-08-27
resolution: already resolved: _bmad-output/implementation-artifacts/spec-frontcomposer-fluent-structural-and-style-conformance-sweep.md:152-154 documents the empirical retest and dismissal as a verified false positive.

### DW-37: Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18. Original context is preserved in legacy-detail.
legacy-detail: - Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18.
status: done 2026-08-27
resolution: already resolved: _bmad-output/implementation-artifacts/spec-frontcomposer-fluent-structural-and-style-conformance-sweep.md:145 records the completed 2026-06-18 review.

### DW-38: Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18. Original context is preserved in legacy-detail.
legacy-detail: - Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18.
status: done 2026-08-27
resolution: already resolved: _bmad-output/implementation-artifacts/spec-frontcomposer-shell-and-adminui-fluent-conformance-audit.md:246-248 records the completed 2026-06-18 review.

### DW-39: Current deployment docs/YAML scan on 2026-06-19
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: docs/YAML
reason: The legacy ledger defers this issue: Current deployment docs/YAML scan on 2026-06-19. Original context is preserved in legacy-detail.
legacy-detail: - Current deployment docs/YAML scan on 2026-06-19.
status: done 2026-08-27
resolution: already resolved: _bmad-output/implementation-artifacts/cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup.md:83-91 records the current YAML/docs scan, patches, and documentation assertions.

### DW-40: DAPR v1.17 topic-scoping documentation checked on 2026-06-20
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: DAPR v1.17 topic-scoping documentation checked on 2026-06-20. Original context is preserved in legacy-detail.
legacy-detail: - DAPR v1.17 topic-scoping documentation checked on 2026-06-20.
status: done 2026-08-27
resolution: already resolved: _bmad-output/implementation-artifacts/cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup.md:81 records the DAPR v1.17/v1.18 topic-scoping documentation verification.

### DW-41: Production `deploy/dapr/pubsub.yaml` denies `sample` publishing with an empty topic list (`publishingScopes: "sample="`) and allows `sample` to subscribe to `tenants.events`, while leaving `eventstore` unlisted so it keeps unrestricted publish access (required for EventStore dynamic per-tenant topic provisioning, NFR20 — listing `eventstore` is the documented anti-pattern)
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: deploy/dapr/pubsub.yaml
reason: The legacy ledger defers this issue: Production `deploy/dapr/pubsub.yaml` denies `sample` publishing with an empty topic list (`publishingScopes: "sample="`) and allows `sample` to subscribe to `tenants.events`, while leaving `eventstore` unlisted so it keeps unrestricted publish access (required for EventStore dynamic per-tenant topic provisioning, NFR20 — listing `eventstore` is the… Original context is preserved in legacy-detail.
legacy-detail: - Production `deploy/dapr/pubsub.yaml` denies `sample` publishing with an empty topic list (`publishingScopes: "sample="`) and allows `sample` to subscribe to `tenants.events`, while leaving `eventstore` unlisted so it keeps unrestricted publish access (required for EventStore dynamic per-tenant topic provisioning, NFR20 — listing `eventstore` is the documented anti-pattern). [2026-06-20 code-review correction: an earlier explicit `eventstore=tenants.events,deadletter.tenants.events;sample=` allow-list was reverted because it violated EventStore NFR20 and would have silently denied dynamic-tenant topics.]
status: done 2026-08-25
resolution: already resolved: deploy/dapr/pubsub.yaml:13-21,38-44 encodes unrestricted EventStore publishing, sample restrictions, and exact component scopes.

### DW-42: Local AppHost pub/sub intentionally omits topic-level scopes while retaining component-level `eventstore` and `sample` scopes; the difference is documented in the component YAML and timing guide
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: eventstore
reason: The legacy ledger defers this issue: Local AppHost pub/sub intentionally omits topic-level scopes while retaining component-level `eventstore` and `sample` scopes; the difference is documented in the component YAML and timing guide. Original context is preserved in legacy-detail.
legacy-detail: - Local AppHost pub/sub intentionally omits topic-level scopes while retaining component-level `eventstore` and `sample` scopes; the difference is documented in the component YAML and timing guide.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml:10-15,29-34 explicitly omits local topic scopes and declares consumers.

### DW-43: `docs/cross-aggregate-timing.md` distinguishes subscriber redelivery on `tenants.events` from EventStore's application-level dead-letter publisher for `deadletter.tenants.events`
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: docs/cross-aggregate-timing.md
reason: The legacy ledger defers this issue: `docs/cross-aggregate-timing.md` distinguishes subscriber redelivery on `tenants.events` from EventStore's application-level dead-letter publisher for `deadletter.tenants.events`. Original context is preserved in legacy-detail.
legacy-detail: - `docs/cross-aggregate-timing.md` distinguishes subscriber redelivery on `tenants.events` from EventStore's application-level dead-letter publisher for `deadletter.tenants.events`.
status: done 2026-08-25
resolution: already resolved: docs/cross-aggregate-timing.md:80-89,129-133 distinguishes EventStore application dead-lettering from Dapr subscriber redelivery.

### DW-44: `CrossAggregateTimingDocumentationTests` guards the production topic-scope contract, local topic-scope omission, application-level dead-letter wording, and the absence of DAPR subscriber-failure-to-dead-letter wording
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: CrossAggregateTimingDocumentationTests
reason: The legacy ledger defers this issue: `CrossAggregateTimingDocumentationTests` guards the production topic-scope contract, local topic-scope omission, application-level dead-letter wording, and the absence of DAPR subscriber-failure-to-dead-letter wording. Original context is preserved in legacy-detail.
legacy-detail: - `CrossAggregateTimingDocumentationTests` guards the production topic-scope contract, local topic-scope omission, application-level dead-letter wording, and the absence of DAPR subscriber-failure-to-dead-letter wording.
status: done 2026-08-25
resolution: already resolved: tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs:114-172 guards the YAML and documentation contract together.

### DW-45: June 18 review-record contradictions are kept as routed, stale/resolved, or future-owner handoff entries instead of open Tenants implementation work
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: June 18 review-record contradictions are kept as routed, stale/resolved, or future-owner handoff entries instead of open Tenants implementation work. Original context is preserved in legacy-detail.
legacy-detail: - June 18 review-record contradictions are kept as routed, stale/resolved, or future-owner handoff entries instead of open Tenants implementation work.
status: done 2026-08-27
resolution: already resolved: _bmad-output/implementation-artifacts/cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup.md:84,92 records normalized routing/evidence records and the stale/resolved EventStore item.

### DW-46: `FrontComposerShell` exposes a shell content landmark contract that can be a native `<main>` or an equivalent role with an accessible-name parameter
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
location: FrontComposerShell
reason: The legacy ledger defers this issue: `FrontComposerShell` exposes a shell content landmark contract that can be a native `<main>` or an equivalent role with an accessible-name parameter. Original context is preserved in legacy-detail.
legacy-detail: - `FrontComposerShell` exposes a shell content landmark contract that can be a native `<main>` or an equivalent role with an accessible-name parameter.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:141-148 renders one named main; FcContentLabel.razor.cs:5-11 provides the no-markup bridge.

### DW-47: Tenants page headings can name the shell main landmark without orphaned page-level `aria-labelledby`
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Tenants page headings can name the shell main landmark without orphaned page-level `aria-labelledby`. Original context is preserved in legacy-detail.
legacy-detail: - Tenants page headings can name the shell main landmark without orphaned page-level `aria-labelledby`.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:141-148 renders one named main; FcContentLabel.razor.cs:5-11 provides the no-markup bridge.

### DW-48: `FcPageHeader` no longer creates a competing global `banner` landmark on every route page
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
location: FcPageHeader
reason: The legacy ledger defers this issue: `FcPageHeader` no longer creates a competing global `banner` landmark on every route page. Original context is preserved in legacy-detail.
legacy-detail: - `FcPageHeader` no longer creates a competing global `banner` landmark on every route page.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor:7-13 forces role=presentation.

### DW-49: `FcPageHeader` handles blank `Heading` fail-safely or documents a strict consumer contract with analyzable/tested guardrails
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
location: FcPageHeader
reason: The legacy ledger defers this issue: `FcPageHeader` handles blank `Heading` fail-safely or documents a strict consumer contract with analyzable/tested guardrails. Original context is preserved in legacy-detail.
legacy-detail: - `FcPageHeader` handles blank `Heading` fail-safely or documents a strict consumer contract with analyzable/tested guardrails.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor:34-44 suppresses blank headings.

### DW-50: `FocusHeadingAsync()` ensures the heading is focusable when used as a focus target or fails diagnostically when `HeadingTabIndex` is omitted
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
location: FocusHeadingAsync
reason: The legacy ledger defers this issue: `FocusHeadingAsync()` ensures the heading is focusable when used as a focus target or fails diagnostically when `HeadingTabIndex` is omitted. Original context is preserved in legacy-detail.
legacy-detail: - `FocusHeadingAsync()` ensures the heading is focusable when used as a focus target or fails diagnostically when `HeadingTabIndex` is omitted.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs:94-126 documents and throws for non-focusable or missing headings.

### DW-51: FrontComposer H-FC-1: rework or re-justify `FcHomeCard` against pinned `FluentCard` support
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
location: FcHomeCard
reason: The legacy ledger defers this issue: FrontComposer H-FC-1: rework or re-justify `FcHomeCard` against pinned `FluentCard` support. Original context is preserved in legacy-detail.
legacy-detail: - FrontComposer H-FC-1: rework or re-justify `FcHomeCard` against pinned `FluentCard` support.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.FrontComposer/_bmad-output/project-docs/architecture.md:130-140 documents the FcHomeCard carve-out; FluentConformanceTests.cs:120-130 pins the allowlist.

### DW-52: FrontComposer H-FC-2: consider parity guards for structural/style governance
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: FrontComposer H-FC-2: consider parity guards for structural/style governance. Original context is preserved in legacy-detail.
legacy-detail: - FrontComposer H-FC-2: consider parity guards for structural/style governance.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Tests/Governance/FluentConformanceTests.cs:120-130 enforces raw-control parity with a narrow documented exception.

### DW-53: Continue the Admin.UI audit remediation handoffs from `audit-frontcomposer-shell-adminui-fluent-2026-06-18.md`: `Index.razor` non-semantic clickable semantics, clickable-span remediation, `ActivityChart` a11y proof, `StorageTreemap` semantics/docs, and optional parity guards
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-admin-ui-and-query-record-followup`"), 2026-08-25
location: audit-frontcomposer-shell-adminui-fluent-2026-06-18.md; Index.razor
reason: The legacy ledger defers this issue: Continue the Admin.UI audit remediation handoffs from `audit-frontcomposer-shell-adminui-fluent-2026-06-18.md`: `Index.razor` non-semantic clickable semantics, clickable-span remediation, `ActivityChart` a11y proof, `StorageTreemap` semantics/docs, and optional parity guards. Original context is preserved in legacy-detail.
legacy-detail: - Continue the Admin.UI audit remediation handoffs from `audit-frontcomposer-shell-adminui-fluent-2026-06-18.md`: `Index.razor` non-semantic clickable semantics, clickable-span remediation, `ActivityChart` a11y proof, `StorageTreemap` semantics/docs, and optional parity guards.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Pages/Index.razor:25-51, Components/ActivityChart.razor:28-46, and Components/StorageTreemap.razor:65-76 contain the fixes.

### DW-54: If EventStore tests still encode the retired Tenants actor-routing assumption, update them under EventStore ownership
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-admin-ui-and-query-record-followup`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: If EventStore tests still encode the retired Tenants actor-routing assumption, update them under EventStore ownership. Original context is preserved in legacy-detail.
legacy-detail: - If EventStore tests still encode the retired Tenants actor-routing assumption, update them under EventStore ownership.
status: done 2026-08-26
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.Server/Services/DaprTenantQueryService.cs:195-215 uses the generic query endpoint without the retired Tenants projection-actor routing contract.

### DW-55: Add or expose shared read-model metadata for persisted projection timestamp/version if D6 threshold-based `aging` and `stale` states need to be computed generically
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-read-model-freshness-metadata`"), 2026-08-25
location: aging
reason: The legacy ledger defers this issue: Add or expose shared read-model metadata for persisted projection timestamp/version if D6 threshold-based `aging` and `stale` states need to be computed generically. Original context is preserved in legacy-detail.
legacy-detail: - Add or expose shared read-model metadata for persisted projection timestamp/version if D6 threshold-based `aging` and `stale` states need to be computed generically.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/ReadModels/IReadModelFreshness.cs:22-34, ReadModelFreshness.cs:30-48, and ReadModelFreshnessExtensions.cs:23-85 implement the shared freshness contract.

### DW-56: Keep the capability in `Hexalith.EventStore` (`IReadModelStore` / query metadata path) rather than adding Tenants-specific persistence scaffolding
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-read-model-freshness-metadata`"), 2026-08-25
location: Hexalith.EventStore
reason: The legacy ledger defers this issue: Keep the capability in `Hexalith.EventStore` (`IReadModelStore` / query metadata path) rather than adding Tenants-specific persistence scaffolding. Original context is preserved in legacy-detail.
legacy-detail: - Keep the capability in `Hexalith.EventStore` (`IReadModelStore` / query metadata path) rather than adding Tenants-specific persistence scaffolding.
status: done 2026-08-25
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/ReadModels/IReadModelFreshness.cs:22-34, ReadModelFreshness.cs:30-48, and ReadModelFreshnessExtensions.cs:23-85 implement the shared freshness contract.

### DW-57: Once available, Tenants can map real persisted projection age/version through configurable thresholds; until then Tenants uses the direct-read ETag/version `current` rule and fails unmarked responses closed to `unknown`
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-read-model-freshness-metadata`"), 2026-08-25
location: current
reason: The legacy ledger defers this issue: Once available, Tenants can map real persisted projection age/version through configurable thresholds; until then Tenants uses the direct-read ETag/version `current` rule and fails unmarked responses closed to `unknown`. Original context is preserved in legacy-detail.
legacy-detail: - Once available, Tenants can map real persisted projection age/version through configurable thresholds; until then Tenants uses the direct-read ETag/version `current` rule and fails unmarked responses closed to `unknown`.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants/Queries/TenantQueryResult.cs:38-60 consumes IReadModelFreshness; TenantQueryFreshnessTests.cs:35-85 proves ProjectedAt classification and ServedAt response timing.

### DW-58: ETag special-character (quote/comma) robustness — latent, non-exploitable. `NormalizeETagToken`/`Trim('"')` unquote any value that starts and ends with `"` (asymmetric vs raw store tokens) in `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs:87-107`; the client and server both reject commas with a substring check, dropping a single quoted strong tag whose content legitimately contains a comma (`src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs:25-29`); and client/server normalization disagree on quoted-whitespace/`"*"` edge inputs. These do not bite while DAPR/Redis read-model ETags remain opaque numeric strings without quotes or commas, and the emit→submit→compare round-trip is internally symmetric. Revisit if the EventStore read-model store contract ever emits special-character ETags (ties into the `eventstore-2026-06-19-read-model-freshness-metadata` handoff above)
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening (2026-06-19)"), 2026-08-25
location: src/Hexalith.Tenants/Controllers/TenantsQueryController.cs:87-107; src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs:25-29
reason: The legacy ledger defers this issue: ETag special-character (quote/comma) robustness — latent, non-exploitable. Original context is preserved in legacy-detail.
legacy-detail: - ETag special-character (quote/comma) robustness — latent, non-exploitable. `NormalizeETagToken`/`Trim('"')` unquote any value that starts and ends with `"` (asymmetric vs raw store tokens) in `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs:87-107`; the client and server both reject commas with a substring check, dropping a single quoted strong tag whose content legitimately contains a comma (`src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs:25-29`); and client/server normalization disagree on quoted-whitespace/`"*"` edge inputs. These do not bite while DAPR/Redis read-model ETags remain opaque numeric strings without quotes or commas, and the emit→submit→compare round-trip is internally symmetric. Revisit if the EventStore read-model store contract ever emits special-character ETags (ties into the `eventstore-2026-06-19-read-model-freshness-metadata` handoff above).
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:610-650 parses EntityTagHeaderValue and rejects weak, wildcard, empty, duplicate, quoted, and control-bearing validators; TenantsRestQueryClientTests.cs:1165-1469 covers them.

### DW-59: CSS ownership guard logical longhand spacing — RESOLVED (2026-06-21 hardening). `DomainUiFluentConformanceTests` now tracks the logical longhands (`margin-inline-start/-end`, `padding-block-start/-end`, etc.) alongside the physical longhands and shorthand, with `[InlineData]` coverage for both flagged and zero-reset cases
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-domain-ui-governance-and-accessibility-hardening (2026-06-19)"), 2026-08-25
location: DomainUiFluentConformanceTests
reason: The legacy ledger defers this issue: CSS ownership guard logical longhand spacing — RESOLVED (2026-06-21 hardening). Original context is preserved in legacy-detail.
legacy-detail: - CSS ownership guard logical longhand spacing — **RESOLVED (2026-06-21 hardening).** `DomainUiFluentConformanceTests` now tracks the logical longhands (`margin-inline-start/-end`, `padding-block-start/-end`, etc.) alongside the physical longhands and shorthand, with `[InlineData]` coverage for both flagged and zero-reset cases.
status: done 2026-06-21
resolution: Legacy completion record: - CSS ownership guard logical longhand spacing — **RESOLVED (2026-06-21 hardening).** `DomainUiFluentConformanceTests` now tracks the logical longhands (`margin-inline-start/-end`, `padding-block-start/-end`, etc.) alongside the physical longhands and shorthand, with `[InlineData]` coverage for both flagged and zero-reset cases.

### DW-60: Forced-colors malformed block handling — RESOLVED (2026-06-21 hardening). `RemoveForcedColorsMediaBlocks` plus a dedicated `Forced_colors_unterminated_block_does_not_hide_trailing_ownership` test now ensure an unterminated forced-colors block cannot hide trailing ownership declarations from the scan
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-domain-ui-governance-and-accessibility-hardening (2026-06-19)"), 2026-08-25
location: RemoveForcedColorsMediaBlocks
reason: The legacy ledger defers this issue: Forced-colors malformed block handling — RESOLVED (2026-06-21 hardening). Original context is preserved in legacy-detail.
legacy-detail: - Forced-colors malformed block handling — **RESOLVED (2026-06-21 hardening).** `RemoveForcedColorsMediaBlocks` plus a dedicated `Forced_colors_unterminated_block_does_not_hide_trailing_ownership` test now ensure an unterminated forced-colors block cannot hide trailing ownership declarations from the scan.
status: done 2026-06-21
resolution: Legacy completion record: - Forced-colors malformed block handling — **RESOLVED (2026-06-21 hardening).** `RemoveForcedColorsMediaBlocks` plus a dedicated `Forced_colors_unterminated_block_does_not_hide_trailing_ownership` test now ensure an unterminated forced-colors block cannot hide trailing ownership declarations from the scan.

### DW-61: Sibling query ETag special-character robustness — quote/comma ETag edge cases surfaced again because the working-tree diff includes the completed tenant-query hardening story. Keep routed under the tenant-query review / EventStore read-model freshness handoff; it is outside the domain UI governance story
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-domain-ui-governance-and-accessibility-hardening (2026-06-19)"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Sibling query ETag special-character robustness — quote/comma ETag edge cases surfaced again because the working-tree diff includes the completed tenant-query hardening story. Original context is preserved in legacy-detail.
legacy-detail: - Sibling query ETag special-character robustness — quote/comma ETag edge cases surfaced again because the working-tree diff includes the completed tenant-query hardening story. Keep routed under the tenant-query review / EventStore read-model freshness handoff; it is outside the domain UI governance story.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:610-650 parses EntityTagHeaderValue and rejects weak, wildcard, empty, duplicate, quoted, and control-bearing validators; TenantsRestQueryClientTests.cs:1165-1469 covers them.

### DW-62: Application-level vs native dead-letter framing for operators — RESOLVED (2026-06-21 hardening). `deploy/dapr/README.md:53` now carries an explicit operator note scoping the "no native dead-letter" claim to the `pubsub` component shipped here and warning that an EventStore-provided component may set its own `enableDeadLetter`/`deadLetterTopic`
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup (2026-06-20)"), 2026-08-25
location: deploy/dapr/README.md:53
reason: The legacy ledger defers this issue: Application-level vs native dead-letter framing for operators — RESOLVED (2026-06-21 hardening). Original context is preserved in legacy-detail.
legacy-detail: - Application-level vs native dead-letter framing for operators — **RESOLVED (2026-06-21 hardening).** `deploy/dapr/README.md:53` now carries an explicit operator note scoping the "no native dead-letter" claim to the `pubsub` component shipped here and warning that an EventStore-provided component may set its own `enableDeadLetter`/`deadLetterTopic`.
status: done 2026-06-21
resolution: Legacy completion record: - Application-level vs native dead-letter framing for operators — **RESOLVED (2026-06-21 hardening).** `deploy/dapr/README.md:53` now carries an explicit operator note scoping the "no native dead-letter" claim to the `pubsub` component shipped here and warning that an EventStore-provided component may set its own `enableDeadLetter`/`deadLetterTopic`.

### DW-63: Stale Server.Tests evidence line in `test-summary.md` — RESOLVED (2026-06-21 hardening). A correction note was added (`tests/test-summary.md:246`) recording that the 3-test Server.Tests blocker was resolved on 2026-06-20 and that Server.Tests passes; the old line is retained only as dated historical evidence
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup (2026-06-20)"), 2026-08-25
location: tests/test-summary.md:246; test-summary.md
reason: The legacy ledger defers this issue: Stale Server.Tests evidence line in `test-summary.md` — RESOLVED (2026-06-21 hardening). Original context is preserved in legacy-detail.
legacy-detail: - Stale Server.Tests evidence line in `test-summary.md` — **RESOLVED (2026-06-21 hardening).** A correction note was added (`tests/test-summary.md:246`) recording that the 3-test Server.Tests blocker was resolved on 2026-06-20 and that Server.Tests passes; the old line is retained only as dated historical evidence.
status: done 2026-06-21
resolution: Legacy completion record: - Stale Server.Tests evidence line in `test-summary.md` — **RESOLVED (2026-06-21 hardening).** A correction note was added (`tests/test-summary.md:246`) recording that the 3-test Server.Tests blocker was resolved on 2026-06-20 and that Server.Tests passes; the old line is retained only as dated historical evidence.

### DW-64: `<FcContentLabel>` single-writer dispose-clobber
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-21-frontcomposer-page-header-landmarks-and-contract-hardening (2026-06-25)"), 2026-08-25
location: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcContentLabelCoordinator.cs:159; FcContentLabel.razor.cs:80-84
reason: The legacy ledger defers this issue: `<FcContentLabel>` single-writer dispose-clobber. Original context is preserved in legacy-detail.
legacy-detail: - **`<FcContentLabel>` single-writer dispose-clobber** — when two `<FcContentLabel>` markers render on one page, disposing one calls `FcContentLabelCoordinator.Reset()` (→ `Set(null, null)`), wiping a still-live sibling's accessible name on `#fc-main-content` until the survivor happens to re-render (`references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcContentLabelCoordinator.cs:159` + `FcContentLabel.razor.cs:80-84`). Real but silent a11y edge case; it faithfully mirrors the accepted, documented `FcPageLayoutCoordinator` "single-writer, last-writer-wins" pattern (identical latent limitation by design) and no current consumer renders two markers. Fix path if multi-writer support is ever needed: add a writer-identity/token guard so only the current writer's dispose resets — apply to BOTH coordinators together for consistency. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the dispose-clobber + single-writer last-writer-wins limitation is now recorded in the `FcContentLabel` XML `<remarks>` and a matching sentence on `FcContentLabelCoordinator`; the writer-identity guard remains the routed follow-up if multi-writer support is ever needed.
status: done 2026-07-01
resolution: Legacy completion record: - **`<FcContentLabel>` single-writer dispose-clobber** — when two `<FcContentLabel>` markers render on one page, disposing one calls `FcContentLabelCoordinator.Reset()` (→ `Set(null, null)`), wiping a still-live sibling's accessible name on `#fc-main-content` until the survivor happens to re-render (`references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcContentLabelCoordinator.cs:159` + `FcContentLabel.razor.cs:80-84`). Real but silent a11y edge case; it faithfully mirrors the accepted, documented `FcPageLayoutCoordinator` "single-writer, last-writer-wins" pattern (identical latent limitation by design) and no current consumer renders two markers. Fix path if multi-writer support is ever needed: add a writer-identity/token guard so only the current writer's dispose resets — apply to BOTH coordinators together for consistency. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the dispose-clobber + single-writer last-writer-wins limitation is now recorded in the `FcContentLabel` XML `<remarks>` and a matching sentence on `FcContentLabelCoordinator`; the writer-identity guard remains the routed follow-up if multi-writer support is ever needed.

### DW-65: Page-driven `<FcContentLabel>` accessible name absent on server first paint
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-21-frontcomposer-page-header-landmarks-and-contract-hardening (2026-06-25)"), 2026-08-25
location: FcContentLabel.razor.cs:67-77
reason: The legacy ledger defers this issue: Page-driven `<FcContentLabel>` accessible name absent on server first paint. Original context is preserved in legacy-detail.
legacy-detail: - **Page-driven `<FcContentLabel>` accessible name absent on server first paint** — registration is `OnAfterRender`-only (`FcContentLabel.razor.cs:67-77`), so on a static-SSR/prerender pass `#fc-main-content` emits no `aria-label`/`aria-labelledby` from the page-marker path; the name appears only after interactive hydration. The shell-parameter path (`ContentLabel`/`ContentLabelledBy`) is correct on first paint. Mirrors the established `FcPageLayout` coordinator pattern and is acceptable for this InteractiveServer library; recommend documenting the limitation in the `FcContentLabel` XML remarks. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the `OnAfterRender`-only first-paint limitation is now in the `FcContentLabel` XML `<remarks>`, naming the shell-parameter path as the first-paint-correct alternative.
status: done 2026-07-01
resolution: Legacy completion record: - **Page-driven `<FcContentLabel>` accessible name absent on server first paint** — registration is `OnAfterRender`-only (`FcContentLabel.razor.cs:67-77`), so on a static-SSR/prerender pass `#fc-main-content` emits no `aria-label`/`aria-labelledby` from the page-marker path; the name appears only after interactive hydration. The shell-parameter path (`ContentLabel`/`ContentLabelledBy`) is correct on first paint. Mirrors the established `FcPageLayout` coordinator pattern and is acceptable for this InteractiveServer library; recommend documenting the limitation in the `FcContentLabel` XML remarks. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the `OnAfterRender`-only first-paint limitation is now in the `FcContentLabel` XML `<remarks>`, naming the shell-parameter path as the first-paint-correct alternative.

### DW-66: `FocusHeadingAsync()` no-op → throw is an undisclosed API behavior change
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-21-frontcomposer-page-header-landmarks-and-contract-hardening (2026-06-25)"), 2026-08-25
location: FcPageHeader.razor.cs:104-117; FcAggregateListPage.razor.cs:83-84
reason: The legacy ledger defers this issue: `FocusHeadingAsync()` no-op → throw is an undisclosed API behavior change. Original context is preserved in legacy-detail.
legacy-detail: - **`FocusHeadingAsync()` no-op → throw is an undisclosed API behavior change** (resolved `decision-needed` → defer by Administrator on 2026-06-25). Keep the diagnostic throw: it is the intended hardening (Requested outcome 5) and no live consumer regresses (`TenantsWorkspace` → `FcAggregateListPage` passes `HeadingTabIndex="-1"`, verified). Follow-up: document the no-op→throw change for external FrontComposer adopters in the changelog / `FcPageHeader.FocusHeadingAsync` remarks, and note that the `FcAggregateListPage` wrapper's `… ?? ValueTask.CompletedTask` only guards the pre-first-render null `@ref` window, not the new throw. `FcPageHeader.razor.cs:104-117`, `FcAggregateListPage.razor.cs:83-84`. FrontComposer submodule. — **DOCUMENTED 2026-07-01 (CC deferred-work):** FrontComposer has no CHANGELOG, so the adopter-facing no-op→throw behavior-change note (incl. the `FcAggregateListPage` `?? ValueTask.CompletedTask` caveat) was added to the `FcPageHeader.FocusHeadingAsync` XML `<remarks>`.
status: done 2026-07-01
resolution: Legacy completion record: - **`FocusHeadingAsync()` no-op → throw is an undisclosed API behavior change** (resolved `decision-needed` → defer by Administrator on 2026-06-25). Keep the diagnostic throw: it is the intended hardening (Requested outcome 5) and no live consumer regresses (`TenantsWorkspace` → `FcAggregateListPage` passes `HeadingTabIndex="-1"`, verified). Follow-up: document the no-op→throw change for external FrontComposer adopters in the changelog / `FcPageHeader.FocusHeadingAsync` remarks, and note that the `FcAggregateListPage` wrapper's `… ?? ValueTask.CompletedTask` only guards the pre-first-render null `@ref` window, not the new throw. `FcPageHeader.razor.cs:104-117`, `FcAggregateListPage.razor.cs:83-84`. FrontComposer submodule. — **DOCUMENTED 2026-07-01 (CC deferred-work):** FrontComposer has no CHANGELOG, so the adopter-facing no-op→throw behavior-change note (incl. the `FcAggregateListPage` `?? ValueTask.CompletedTask` caveat) was added to the `FcPageHeader.FocusHeadingAsync` XML `<remarks>`.

### DW-67: SVG `<g tabindex="0">` focusability not guaranteed cross-browser
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-21-eventstore-admin-ui-a11y-remediation (2026-06-25)"), 2026-08-25
location: references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Components/StorageTreemap.razor:72; StorageTreemap.razor
reason: The legacy ledger defers this issue: SVG `<g tabindex="0">` focusability not guaranteed cross-browser. Original context is preserved in legacy-detail.
legacy-detail: - **SVG `<g tabindex="0">` focusability not guaranteed cross-browser** (`references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Components/StorageTreemap.razor:72`) — the treemap cells became focusable via `tabindex="0"` on an SVG `<g>` group. Modern Chromium/Edge/Firefox include tabindex'd SVG container elements in the tab order; Safari/older WebKit historically do not, which would leave the treemap cells (and their `role="button"` keyboard activation) unreachable by Tab there. For an internal EventStore Admin.UI targeting Chromium/Edge the practical risk is low. Follow-up: validate against the actual supported browser matrix; if Safari/WebKit must be supported, make the focusable element an SVG `<a>` or wrap an HTML control in `<foreignObject>`. The bUnit test only asserts the attribute is present, not that the browser focuses it. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the cross-browser caveat + `<a>`/`<foreignObject>` remedy is now recorded as a Razor comment above the focusable `<g role="button" tabindex="0">` in `StorageTreemap.razor`. Validation against the actual supported browser matrix remains the routed follow-up.
status: done 2026-07-01
resolution: Legacy completion record: - **SVG `<g tabindex="0">` focusability not guaranteed cross-browser** (`references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Components/StorageTreemap.razor:72`) — the treemap cells became focusable via `tabindex="0"` on an SVG `<g>` group. Modern Chromium/Edge/Firefox include tabindex'd SVG container elements in the tab order; Safari/older WebKit historically do not, which would leave the treemap cells (and their `role="button"` keyboard activation) unreachable by Tab there. For an internal EventStore Admin.UI targeting Chromium/Edge the practical risk is low. Follow-up: validate against the actual supported browser matrix; if Safari/WebKit must be supported, make the focusable element an SVG `<a>` or wrap an HTML control in `<foreignObject>`. The bUnit test only asserts the attribute is present, not that the browser focuses it. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the cross-browser caveat + `<a>`/`<foreignObject>` remedy is now recorded as a Razor comment above the focusable `<g role="button" tabindex="0">` in `StorageTreemap.razor`. Validation against the actual supported browser matrix remains the routed follow-up.

### DW-68: Global Administrators / Audit discoverability after nav de-listing
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace (2026-06-27)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs
reason: The legacy ledger defers this issue: Global Administrators / Audit discoverability after nav de-listing. Original context is preserved in legacy-detail.
legacy-detail: - **Global Administrators / Audit discoverability after nav de-listing** — the approved 2026-06-27 IA (AC9) removed `/global-administrators` and audit from the Tenants left-menu; the routes, pages, and `GlobalAdministratorPolicy` are preserved, but the diff adds no module-internal/contextual entry point, so a global administrator can reach the surface only by typing the URL. The sprint-change-proposal explicitly defers this: GA/Audit "remain available through module-internal tabs or contextual entry points ... unless a future module-level IA decision adds them explicitly." Follow-up: when Product confirms the contextual entry-point IA, add a discoverable in-workspace path. (`src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:35-41 and AuditEvidenceEntryPoint.razor:5-18 expose authorized global-administrator and contextual audit entry points; tests cover both.

### DW-69: GlobalAdministratorPolicy now registered but unconsumed
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Group 1 re-review (2026-06-27, chunked)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs; src/Hexalith.Tenants.UI/Program.cs:33
reason: The legacy ledger defers this issue: GlobalAdministratorPolicy now registered but unconsumed. Original context is preserved in legacy-detail.
legacy-detail: - **GlobalAdministratorPolicy now registered but unconsumed** — extends the GA discoverability item above: after the nav `RequiredPolicy:` was removed, `Program.cs:33` still registers `Tenants.GlobalAdministrator` but nothing requires it (the GA page authorizes via `BffComposition` reflection). Retention is intentional pending the deferred contextual-entry-point IA decision; revisit (wire or remove) when that decision lands. (`src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`, `src/Hexalith.Tenants.UI/Program.cs:33`)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:101-106 registers GlobalAdministratorPolicy; src/Hexalith.Tenants.UI/Program.cs:76-88 applies it.

### DW-70: Create-tenant freshness gate narrowed `Current or Unknown` → `Current` — RESOLVED 2026-06-30 (CC deferred-work, verify-only)
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Group 1 re-review (2026-06-27, chunked)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor; TenantsWorkspace.razor
reason: The legacy ledger defers this issue: Create-tenant freshness gate narrowed `Current or Unknown` → `Current` — RESOLVED 2026-06-30 (CC deferred-work, verify-only). Original context is preserved in legacy-detail.
legacy-detail: - **~~Create-tenant freshness gate narrowed `Current or Unknown` → `Current`~~ — RESOLVED 2026-06-30 (CC deferred-work, verify-only)** — the "restore" path was taken: `TenantsWorkspace.razor` `CreateTenantFlow IsFresh` is back to `Freshness is Current or Unknown`, matching the documented first-tenant bootstrap exception (Unknown list freshness remains creatable). No code change this run; verified live. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:416-425 restores Current or the documented authoritative first-tenant Unknown bootstrap case.

### DW-71: Page-local tabs render empty tabpanels
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Group 1 re-review (2026-06-27, chunked)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:28-30
reason: The legacy ledger defers this issue: Page-local tabs render empty tabpanels. Original context is preserved in legacy-detail.
legacy-detail: - **Page-local tabs render empty tabpanels** — the new `FluentTabs` carry `Id`/`Header` only; active content renders in sibling `FcAggregateListPage` slots (`Body`/`Filters`/`States`), so the Fluent tab→tabpanel ARIA relationship points at empty regions. `aria-selected` is correct and tabs are keyboard reachable. This is an `FcAggregateListPage`-slot architectural nuance best owned upstream. Follow-up: FrontComposer/UX decision on associating `FcAggregateListPage` content with `FcPageToolbar`/tab tabpanels. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:28-30`)
status: done 2026-09-02
resolution: already resolved: commit 8bdf7d62; TenantsWorkspace.razor:33-46 now uses FcPageTabs with real panel content, and TenantsWorkspaceTests.cs:91-120 proves reciprocal non-empty tab panels.
decision: 2026-08-25 Add FrontComposer contract — Introduce a shared explicit tab-to-panel association API in FrontComposer and migrate Tenants.

### DW-72: Page-local tabs a11y — empty tabpanels + missing Tenants-owned bUnit assertion
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Full review (2026-06-28)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:22-30
reason: The legacy ledger defers this issue: Page-local tabs a11y — empty tabpanels + missing Tenants-owned bUnit assertion. Original context is preserved in legacy-detail.
legacy-detail: - **Page-local tabs a11y — empty tabpanels + missing Tenants-owned bUnit assertion** — extends the 2026-06-27 Group-1 "empty tabpanels" defer above. AC12/AC13 keyboard/active-tab guarantees ride entirely on the Fluent `FluentTabs` primitive with no Tenants-owned `aria-selected`/keyboard-switch bUnit assertion; the added tests assert tab presence/text and routing only. Follow-up: pair the upstream FrontComposer/UX tabpanel-association decision with a focused active-tab/keyboard bUnit test once the structure is settled. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:22-30`)
status: done 2026-09-02
resolution: already resolved: commit 8bdf7d62; TenantListSurfaceTests.cs:371-378,413-467 and TenantsWorkspaceTests.cs:91-120 cover selection changes, reciprocal tab/panel IDs, and retained content.
decision: 2026-08-25 Full workspace contract — Test initial and changed selection, tab-to-panel association, and supported keyboard transitions with the structural fix.

### DW-73: Global-administrator projection pagination ignored (>20 admins)
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification (2026-06-29)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs
reason: The legacy ledger defers this issue: Global-administrator projection pagination ignored (>20 admins). Original context is preserved in legacy-detail.
legacy-detail: - **Global-administrator projection pagination ignored (>20 admins)** — `GlobalAdministratorsRequest` defaults to PageSize=20 and `HasMore`/cursor are never read; the correction snapshot only inspects page 1's `Rows` for presence and admin count. For more than 20 global administrators: a restore of a 21st+ target is treated as not-applied and can never reach `present=true` (stuck `ProjectionPending`), and a revoke of a 21st+ target is blocked as "already removed". Pre-existing query-shape limitation reused by this story; unusual scale and most failure modes fail closed. Follow-up: design projection paging/aggregation for the fixed global-administrator projection. (`src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs`) — **UPDATE 2026-07-01 (CC deferred-work): the fail-OPEN is CLOSED.** The snapshot now reads `HasMore` and treats absence as conclusive only on a fully-loaded page: `ConfirmProjection` proves a revoke only on `!present && !HasMore` (killing the page-2 false-`Confirmed`), and `EvaluateCurrentProjection` fails closed to `UnableToVerify` (`…CurrentProjectionUnavailable`) rather than the false `AlreadyRemoved` (revoke) or a mis-armed grant (restore). Presence-found stays conclusive so page-1 corrections at scale are unaffected. The residual is now narrowed to the full multi-page load/aggregation that would let a page-2 correction actually RUN instead of being conservatively blocked — still a dedicated projection-paging story.
status: done 2026-08-27
resolution: already resolved: commit 7716f0b11423eb54d74935b6cc6e3edf405dc400; src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1093-1121 now uses the complete-projection loader for correction evidence.

### DW-74: No story-specific gateway-routing test — CLOSED 2026-06-30 (CC deferred-work) as already-covered
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification (2026-06-29)"), 2026-08-25
location: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs
reason: The legacy ledger defers this issue: No story-specific gateway-routing test — CLOSED 2026-06-30 (CC deferred-work) as already-covered. Original context is preserved in legacy-detail.
legacy-detail: - **~~No story-specific gateway-routing test~~ — CLOSED 2026-06-30 (CC deferred-work) as already-covered** — verification showed `TenantCommandGatewayTests` already pins the full `system / global-administrators / global-administrators` triple + CommandType + literal payload for both `SetGlobalAdministratorAsync` and `RemoveGlobalAdministratorAsync`. The item was explicitly conditional on the gateway being touched; it was not, so no new (near-duplicate) test was added. (`tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`)
status: done 2026-08-25
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs:31-67 pins the fixed aggregate and both command types.

### DW-75: Terminal failure states reset to a fresh submittable preview on parent re-render (HIGH) — RESOLVED 2026-06-30
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
location: GlobalAdministratorCorrectionPanel.razor:220; CorrectionStartPanel.razor:286
reason: The legacy ledger defers this issue: Terminal failure states reset to a fresh submittable preview on parent re-render (HIGH) — RESOLVED 2026-06-30. Original context is preserved in legacy-detail.
legacy-detail: - **~~Terminal failure states reset to a fresh submittable preview on parent re-render (HIGH)~~ — RESOLVED 2026-06-30** — both panels now preserve any existing snapshot when the intent is unchanged (`_snapshot is not null && !intentChanged → return`), rebuilding only on a different/first intent, so post-submission terminal states survive parent re-renders without re-arming Submit. GA panel already carried the fix + regression test; the tenant panel was fixed in the code review with a matching `Failed_correction_survives_a_parent_re_render_without_re_arming_submit` test. Full UI suite 838/838 green. (`GlobalAdministratorCorrectionPanel.razor:220`, `CorrectionStartPanel.razor:286`)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:244-261 preserves unchanged terminal snapshots; CorrectionStartPanelTests.cs:387 covers parity.

### DW-76: `ConfirmProjection` confirms off a known-Stale projection — RESOLVED 2026-06-30
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
location: GlobalAdministratorCorrectionSnapshot.cs
reason: The legacy ledger defers this issue: `ConfirmProjection` confirms off a known-Stale projection — RESOLVED 2026-06-30. Original context is preserved in legacy-detail.
legacy-detail: - **~~`ConfirmProjection` confirms off a known-Stale projection~~ — RESOLVED 2026-06-30** — two parts: (1) `ConfirmProjection` itself was hardened by the 2026-06-29 review (P2) to require `Kind Ready` + `Freshness Current`; (2) the live residual — the **pre-submit** gate `ProjectionIsReadable` still accepting `Stale`/non-current, which let a platform-authority correction be SUBMITTED against stale evidence — was fixed in the 2026-06-30 code review: `ProjectionIsReadable` now requires `Kind ∈ {Ready,Empty}` **and** `Freshness=Current`, mirroring the confirm/start gates (Empty-current kept for first-admin restore). (`GlobalAdministratorCorrectionSnapshot.cs` `ProjectionIsReadable`)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs:286-305,365-371 requires Ready/Current evidence and !HasMore before confirmation.

### DW-77: Corrective-proof lookup may link the wrong historical audit row
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
location: CorrectionStartPanel.razor
reason: The legacy ledger defers this issue: Corrective-proof lookup may link the wrong historical audit row. Original context is preserved in legacy-detail.
legacy-detail: - **Corrective-proof lookup may link the wrong historical audit row** — **GLOBAL-ADMIN RESOLVED 2026-06-30; tenant-domain residual RESOLVED 2026-06-30 (CC deferred-work, Edit F).** The global-admin path requires parseable invariant original timestamp evidence, requests system audit rows from that timestamp, filters strictly newer corrective rows, and reports audit delayed when the timestamp is missing/malformed. The tenant-domain `CorrectionStartPanel.QueryCorrectiveProofAsync` now mirrors that pattern (invariant/roundtrip parse, `From: originalTimestamp`, `Timestamp > original`, newest-first). (`CorrectionStartPanel.razor` `QueryCorrectiveProofAsync`)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:531-575 uses invariant parsing and strictly newer proof rows; CorrectionStartPanelTests.cs:478 rejects historical rows.

### DW-78: Focus call lacks `JSDisconnectedException` guard — RESOLVED 2026-06-30 (CC deferred-work, Edit A)
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
location: GlobalAdministratorCorrectionPanel.razor; CorrectionStartPanel.razor
reason: The legacy ledger defers this issue: Focus call lacks `JSDisconnectedException` guard — RESOLVED 2026-06-30 (CC deferred-work, Edit A). Original context is preserved in legacy-detail.
legacy-detail: - **~~Focus call lacks `JSDisconnectedException` guard~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit A)** — both `CorrectionStartPanel` and `GlobalAdministratorCorrectionPanel` `OnAfterRenderAsync` now wrap `_lifecycleElement.FocusAsync()` in `try/catch (JSDisconnectedException)`, matching the existing `TenantAuditPage` guards. (`GlobalAdministratorCorrectionPanel.razor`, `CorrectionStartPanel.razor`)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:322-334 and GlobalAdministratorCorrectionPanel.razor:279-291 catch JSDisconnectedException around focus.

### DW-79: Global-admin projection query unguarded in the page-load critical path — RESOLVED 2026-06-30 (CC deferred-work, Edit B)
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
location: TenantAuditPage.razor
reason: The legacy ledger defers this issue: Global-admin projection query unguarded in the page-load critical path — RESOLVED 2026-06-30 (CC deferred-work, Edit B). Original context is preserved in legacy-detail.
legacy-detail: - **~~Global-admin projection query unguarded in the page-load critical path~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit B)** — `LoadAsync` now wraps the supplementary global-administrator enrichment in `catch (… EventStoreGatewayException or HttpRequestException or JsonException)`; the confirm-time path (`OpenCorrectionAsync` / panel provider) keeps propagating. Test `Tenant_audit_page_survives_global_administrator_projection_fault_during_load`. (`TenantAuditPage.razor` `LoadAsync`)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:499-513 guards supplementary global-administrator loading; TenantAuditPageTests.cs:801 covers the regression.

### DW-80: Corrective-proof timestamp uses `CurrentCulture` instead of `InvariantCulture` — RESOLVED 2026-06-30
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
location: GlobalAdministratorCorrectionSnapshot.cs; GlobalAdministratorCorrectionPanel.razor
reason: The legacy ledger defers this issue: Corrective-proof timestamp uses `CurrentCulture` instead of `InvariantCulture` — RESOLVED 2026-06-30. Original context is preserved in legacy-detail.
legacy-detail: - **~~Corrective-proof timestamp uses `CurrentCulture` instead of `InvariantCulture`~~ — RESOLVED 2026-06-30** — the proof *display* timestamp was fixed by the 2026-06-29 review (P9 — `ProofTimestampLabel` uses `InvariantCulture`); the live residual — the `originalTimestamp` *parse* in `WithCorrectiveProof` (and the panel's proof lookup) using ambient culture — was fixed in the 2026-06-30 code review by parsing with `CultureInfo.InvariantCulture` + `DateTimeStyles.RoundtripKind`. The same review also added a time tie-back so the corrective row must be at/after the original event time. (`GlobalAdministratorCorrectionSnapshot.cs` `WithCorrectiveProof`, `GlobalAdministratorCorrectionPanel.razor` `QueryCorrectiveProofAsync`). NB: the tenant-domain `CorrectionStartPanel` (story 5.6) was likewise fixed 2026-06-30 (CC deferred-work, Edit F): `ProofTimestampLabel` and `TenantCorrectionPreviewSnapshot.WithCorrectiveProof` now parse/format with `InvariantCulture`, and the panel has the proof time tie-back.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:438-478 uses invariant timestamp parsing and a strict newer-than proof filter.

### DW-81: EventCallback→Func drops the parent re-render after confirm refresh (intentional, benign)
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
location: CorrectionStartPanel.razor:202; TenantAuditPage.razor:550
reason: The legacy ledger defers this issue: EventCallback→Func drops the parent re-render after confirm refresh (intentional, benign). Original context is preserved in legacy-detail.
legacy-detail: - **EventCallback→Func drops the parent re-render after confirm refresh (intentional, benign)** — watch-item only: the new `ProjectionRefreshProvider` Func updates the parent field without re-rendering the parent; benign today because those fields feed only the panel. Restore a parent render (or document) if other parent UI later binds the refreshed snapshots. 5.8-introduced. (`CorrectionStartPanel.razor:202`, `TenantAuditPage.razor:550`)
status: done 2026-09-06
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1283 assigns refreshed parent state inside InvokeAsync, ensuring the parent render is scheduled.

### DW-82: `CorrectionStartPanel` terminal-state focus parity (story 5.6) — RESOLVED 2026-06-30 (CC deferred-work, Edit C)
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification — committed bundle re-review (2026-06-30)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
reason: The legacy ledger defers this issue: `CorrectionStartPanel` terminal-state focus parity (story 5.6) — RESOLVED 2026-06-30 (CC deferred-work, Edit C). Original context is preserved in legacy-detail.
legacy-detail: - **~~`CorrectionStartPanel` terminal-state focus parity (story 5.6)~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit C)** — `CorrectionStartPanel.SetSnapshot` now moves keyboard focus on all six terminal states (`Confirmed`/`Failed`/`Rejected`/`Degraded`/`UnableToVerify`/`AlreadyApplied`), mirroring `GlobalAdministratorCorrectionPanel.SetSnapshot`. Test `Panel_rejected_terminal_state_moves_focus_to_lifecycle`. (`src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor`)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:361-373 covers all terminal states; CorrectionStartPanelTests.cs:420 covers rejected-state focus.

### DW-83: Already-logged (2026-06-29), re-confirmed: RESOLVED 2026-07-01 (CC deferred-work):
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification — committed bundle re-review (2026-06-30)"), 2026-08-25
location: GlobalAdministratorCorrectionSnapshot.cs
reason: The legacy ledger defers this issue: Already-logged (2026-06-29), re-confirmed: RESOLVED 2026-07-01 (CC deferred-work):. Original context is preserved in legacy-detail.
legacy-detail: - **~~Already-logged (2026-06-29), re-confirmed:~~ RESOLVED 2026-07-01 (CC deferred-work):** global-administrator projection pagination ignored (>20 admins) — the **confirm-time false-`Confirmed`** path (revoke of a page-2 admin reads `!present` ⇒ "proven"), whose raised severity was flagged here, is now closed: `ConfirmProjection` requires `!present && !HasMore` to prove a revoke, and the preview gate fails closed on an incomplete page. Only the full projection-paging redesign remains routed. (`GlobalAdministratorCorrectionSnapshot.cs`)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs:286-305,365-371 requires Ready/Current evidence and !HasMore before confirmation.

### DW-84: Already-logged (2026-06-29), re-confirmed:
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification — committed bundle re-review (2026-06-30)"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Already-logged (2026-06-29), re-confirmed:. Original context is preserved in legacy-detail.
legacy-detail: - **Already-logged (2026-06-29), re-confirmed:** no story-owned gateway-routing test. No new entry created.
status: done 2026-08-27
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs:31-70 pins system/global-administrators/global-administrators, command types, and literal payloads for set/remove.

### DW-85: Ledger-hygiene (see 5.7 patch P-9) — CLOSED 2026-06-30:
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification — committed bundle re-review (2026-06-30)"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Ledger-hygiene (see 5.7 patch P-9) — CLOSED 2026-06-30:. Original context is preserved in legacy-detail.
legacy-detail: - **~~Ledger-hygiene (see 5.7 patch P-9)~~ — CLOSED 2026-06-30:** the stale "ConfirmProjection confirms off a known-Stale projection" and "Corrective-proof timestamp uses CurrentCulture" entries were rewritten/closed after the follow-up patches landed. Global-admin stale projection and proof timestamp/parse paths are resolved; tenant-domain residuals are tracked separately below.
status: done 2026-08-27
resolution: already resolved: src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs:285-305 rejects non-Current and inconclusive paged absence; GlobalAdministratorCorrectionPanel.razor:466-478 parses timestamps invariantly with RoundtripKind.

### DW-86: Concurrent correction opens can finish projection refresh out of order — RESOLVED 2026-06-30 (CC deferred-work, Edit D)
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-30)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
reason: The legacy ledger defers this issue: Concurrent correction opens can finish projection refresh out of order — RESOLVED 2026-06-30 (CC deferred-work, Edit D). Original context is preserved in legacy-detail.
legacy-detail: - **~~Concurrent correction opens can finish projection refresh out of order~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit D)** — `OpenCorrectionAsync` now captures a `_correctionOpenGeneration` synchronously at entry and applies the active intent only if still the latest, so an earlier open whose refresh resolves last no longer wins. (`src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` `OpenCorrectionAsync`)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:893-925 uses _correctionOpenGeneration so only the newest concurrent open applies.

### DW-87: Tenant-domain correction can still confirm from stale/degraded tenant detail — RESOLVED 2026-06-30 (CC deferred-work, Edit E)
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-30)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
reason: The legacy ledger defers this issue: Tenant-domain correction can still confirm from stale/degraded tenant detail — RESOLVED 2026-06-30 (CC deferred-work, Edit E). Original context is preserved in legacy-detail.
legacy-detail: - **~~Tenant-domain correction can still confirm from stale/degraded tenant detail~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit E)** — `RefreshTenantProjectionAsync` (the tenant confirm-time provider) now returns the projection only when `Freshness is Current`, else `null`, so `ConfirmProjection(null)` fails closed instead of confirming off stale evidence (parity with the GA `Freshness=Current` gate). Test `Panel_does_not_confirm_when_projection_refresh_provider_returns_no_fresh_projection`. (`src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` `RefreshTenantProjectionAsync`)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1042-1062 returns confirmation evidence only for Current freshness; CorrectionStartPanelTests.cs:451 covers fail-closed behavior.

### DW-88: Tenant-domain corrective proof lookup can link unrelated historical rows — RESOLVED 2026-06-30 (CC deferred-work, Edit F)
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-30)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
reason: The legacy ledger defers this issue: Tenant-domain corrective proof lookup can link unrelated historical rows — RESOLVED 2026-06-30 (CC deferred-work, Edit F). Original context is preserved in legacy-detail.
legacy-detail: - **~~Tenant-domain corrective proof lookup can link unrelated historical rows~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit F)** — `QueryCorrectiveProofAsync` now parses `originalTimestamp` (`InvariantCulture`+`RoundtripKind`), lower-bounds the audit query with `From: originalTimestamp`, filters `row.Timestamp > originalTimestamp`, newest-first; missing/malformed timestamp ⇒ audit-delayed. Test `Panel_proof_lookup_ignores_audit_row_not_newer_than_the_original_event`. (`src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor` `QueryCorrectiveProofAsync`)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:531-575 uses invariant parsing and strictly newer proof rows; CorrectionStartPanelTests.cs:478 rejects historical rows.

### DW-89: Scheduled performance workflow lacks the EventStore opt-in — the shared `domain-ci.yml` performance job invokes the `Category=Performance` lane without `HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1`, while `DaprPerformanceFactAttribute` requires that variable
origin: migrated from legacy ledger ("Deferred from: run-all-tests-and-fix-failures review (2026-07-14)"), 2026-08-25
location: references/Hexalith.Builds/.github/workflows/domain-ci.yml; domain-ci.yml
reason: The legacy ledger defers this issue: Scheduled performance workflow lacks the EventStore opt-in — the shared `domain-ci.yml` performance job invokes the `Category=Performance` lane without `HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1`, while `DaprPerformanceFactAttribute` requires that variable. Original context is preserved in legacy-detail.
legacy-detail: - **Scheduled performance workflow lacks the EventStore opt-in** — the shared `domain-ci.yml` performance job invokes the `Category=Performance` lane without `HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1`, while `DaprPerformanceFactAttribute` requires that variable. Local verification explicitly enabled it and executed the 500,000-event benchmark, but the scheduled shared workflow can report a skip. Fix belongs in `Hexalith.Builds` and requires separate submodule approval; add the environment variable to the shared performance job and validate a scheduled-shaped run. (`references/Hexalith.Builds/.github/workflows/domain-ci.yml`)
status: done 2026-08-25
resolution: already resolved: references/Hexalith.Builds/.github/workflows/domain-ci.yml:569-577,587-595 enables performance tests for both VSTest and MTP lanes.

### DW-90: Zero test changes despite several identified Tenants-rendering gaps
origin: migrated from legacy ledger ("Deferred from: code review of 1-0-reverify-frontcomposer-shell-and-fluent-contracts (2026-07-19)"), 2026-08-25
location: story-1-0-frontcomposer-fluent-reverification-2026-07-19.md
reason: The legacy ledger defers this issue: Zero test changes despite several identified Tenants-rendering gaps. Original context is preserved in legacy-detail.
legacy-detail: - **Zero test changes despite several identified Tenants-rendering gaps** — Size16 vs required Size20 icons, missing `IconLabel`, unpinned freshness safety column, missing `MessageBarLayout.Notification`/`AriaLive` usage (`story-1-0-frontcomposer-fluent-reverification-2026-07-19.md` FC-TOK/FC-TBL rows). AC5's own wording is conditional ("add tests only when they guard a confirmed Tenants boundary"), so whether any of these gaps currently qualify is a judgment call for whichever story next touches badge/grid rendering, not a clear miss by this verification-only story.
status: done 2026-08-25
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs:94-108 and src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor:63-75 pin Size20 icons, IconLabel, and the freshness column.

### DW-91: No tracking ticket/issue for the FrontComposer-owned gaps
origin: migrated from legacy ledger ("Deferred from: code review of 1-0-reverify-frontcomposer-shell-and-fluent-contracts (2026-07-19)"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: No tracking ticket/issue for the FrontComposer-owned gaps. Original context is preserved in legacy-detail.
legacy-detail: - **No tracking ticket/issue for the FrontComposer-owned gaps** this story identifies (FC-CMD, FC-CNC, FC-TBL, FC-TOK) — "assign to FrontComposer" has no actual assignment mechanism in this repo's process. Matches the existing routing convention in this file's "Cross-Submodule Owner Handoffs" section, which this story's four gaps should eventually feed as new entries once an owning FrontComposer task is opened.
status: done 2026-08-25
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:642-647 is now the repository tracking mechanism for the FrontComposer handoff.

### DW-92: `sprint-status.yaml`'s flat per-story status can't represent "review with 2 of 5 sub-contracts blocked"
origin: migrated from legacy ledger ("Deferred from: code review of 1-0-reverify-frontcomposer-shell-and-fluent-contracts (2026-07-19)"), 2026-08-25
location: _bmad-output/implementation-artifacts/sprint-status.yaml:53; sprint-status.yaml
reason: The legacy ledger defers this issue: `sprint-status.yaml`'s flat per-story status can't represent "review with 2 of 5 sub-contracts blocked". Original context is preserved in legacy-detail.
legacy-detail: - **`sprint-status.yaml`'s flat per-story status can't represent "review with 2 of 5 sub-contracts blocked"** (`_bmad-output/implementation-artifacts/sprint-status.yaml:53`) — schema limitation of a shared tracking file used across the whole project; not something this story's diff introduced or can fix alone.
status: done 2026-08-26
resolution: closed by human decision: Keep sub-contract detail in story evidence and deferred work.
decision: 2026-08-26 Retain flat status — Keep sub-contract detail in story evidence and deferred work.

### DW-93: Epic 1 is marked done while most child stories remain backlog or review
origin: migrated from legacy ledger ("Deferred from: code review of 1-0-reverify-frontcomposer-shell-and-fluent-contracts (2026-07-19)"), 2026-08-25
location: _bmad-output/implementation-artifacts/sprint-status.yaml:52-64
reason: The legacy ledger defers this issue: Epic 1 is marked done while most child stories remain backlog or review. Original context is preserved in legacy-detail.
legacy-detail: - **`epic-1: done` while most of Epic 1's 12 stories remain `backlog`/`review`** (`_bmad-output/implementation-artifacts/sprint-status.yaml:52-64`) — only 3 of 12 stories under Epic 1 (1-3, 1-5, 1-7) are `done`; the rest (1-0, 1-1, 1-2, 1-4, 1-6, 1-8 through 1-11) are `backlog`/`review`, yet `epic-1` and `epic-1-retrospective` are both marked `done`, violating the file's own documented rule ("done: All stories in epic completed"). Pre-existing — the `epic-1`/`epic-1-retrospective` lines are untouched by this story's diff (only the `1-0-...` status line changed). Likely stale from the epics.md renumbering during the 2026-07-19 sprint-change-proposal rollout (see memory `prd-edit-2026-07-17-scp-0715-prd-slice`); route to a sprint-planning resync, not a fix within this story.
status: done 2026-08-25
resolution: already resolved: _bmad-output/implementation-artifacts/sprint-status.yaml:52-67 marks Epic 1 and every child story done, removing the aggregate-status conflict.

### DW-94: Reusable release caller omits required publication-authority inputs
origin: migrated from legacy ledger ("Deferred from: code review of 1-1-reverify-ui-host-bootstrap-and-canonical-workspace (2026-07-19)"), 2026-08-25
location: github/workflows/release.yml; github/workflows/release.yml:29
reason: The legacy ledger defers this issue: Reusable release caller omits required publication-authority inputs. Original context is preserved in legacy-detail.
legacy-detail: - **Reusable release caller omits required publication-authority inputs** — `.github/workflows/release.yml` already enabled container publication without `builds-execution-sha`, `release-authority-url`, or `release-owner-allowlist`; the shared `domain-release.yml` rejects their empty defaults before publication. This predates Story 1.1's UI mapping and requires a separately authorized release-governance fix. (`.github/workflows/release.yml:29`; `references/Hexalith.Builds/.github/workflows/domain-release.yml:95`)
status: done 2026-08-25
resolution: already resolved: commit 6cc9eb3a; .github/workflows/release.yml:268-296 pins the Builds caller identity and supplies required execution, source, and publication inputs.

### DW-95: Submodule pointer upgrades require their own review
origin: migrated from legacy ledger ("Deferred from: code review of 1-1-reverify-ui-host-bootstrap-and-canonical-workspace (2026-07-19)"), 2026-08-25
location: references/Hexalith.Builds; references/Hexalith.FrontComposer
reason: The legacy ledger defers this issue: Submodule pointer upgrades require their own review. Original context is preserved in legacy-detail.
legacy-detail: - **Submodule pointer upgrades require their own review** — the Builds and FrontComposer pointer changes were present before Story 1.1 implementation and alter shared build/UI inputs. Review and land those dependency changes independently rather than absorbing them into this story's patch set. (`references/Hexalith.Builds`; `references/Hexalith.FrontComposer`)
status: done 2026-08-25
resolution: already resolved: commits daf6c76c and 10db1cee subsequently moved Builds, EventStore, and FrontComposer pointers in dedicated build(deps) commits.

### DW-96: Epic 1 aggregate status conflicts with child stories
origin: migrated from legacy ledger ("Deferred from: code review of 1-1-reverify-ui-host-bootstrap-and-canonical-workspace (2026-07-19)"), 2026-08-25
location: _bmad-output/implementation-artifacts/sprint-status.yaml:52
reason: The legacy ledger defers this issue: Epic 1 aggregate status conflicts with child stories. Original context is preserved in legacy-detail.
legacy-detail: - **Epic 1 aggregate status conflicts with child stories** — `epic-1` remains `done` while Story 1.1 is in review and multiple children are backlog. The aggregate line predates this story and should be reconciled by sprint planning. (`_bmad-output/implementation-artifacts/sprint-status.yaml:52`)
status: done 2026-08-25
resolution: already resolved: _bmad-output/implementation-artifacts/sprint-status.yaml:52-67 marks Epic 1 and every child story done, removing the aggregate-status conflict.

### DW-97: `EXPECTED_DEPENDENCIES` is hand-duplicated between `scripts/validate-nuget-packages.py` and its test mirror in `CiQualityGateScriptTests.cs`
origin: migrated from legacy ledger ("Deferred from: code review of run-all-tests-and-fix-failures-2 (2026-07-20)"), 2026-08-25
location: scripts/validate-nuget-packages.py; tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs
reason: The legacy ledger defers this issue: `EXPECTED_DEPENDENCIES` is hand-duplicated between `scripts/validate-nuget-packages.py` and its test mirror in `CiQualityGateScriptTests.cs`. Original context is preserved in legacy-detail.
legacy-detail: - **`EXPECTED_DEPENDENCIES` is hand-duplicated between `scripts/validate-nuget-packages.py` and its test mirror in `CiQualityGateScriptTests.cs`**, with no single source of truth — the test file's own comment already acknowledges this ("Mirrors EXPECTED_DEPENDENCIES ... so synthetic fixtures satisfy the dependency-boundary validation"). Every future dependency-boundary change (like this session's) requires editing both files in lockstep by hand; a missed edit in one file would silently pass its own regression tests since both copies are asserted against each other, not against real restore output. Consider extracting a shared data file/fixture, or having the test import the script's dict directly, plus adding a negative-path test that asserts the boundary check actually fails when a real project gains an unexpected dependency. Pre-existing design, not introduced by this session's fix. (`scripts/validate-nuget-packages.py`, `tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs`)
status: done 2026-09-02
resolution: resolved by sweep bundle dw-package-boundary-source
resolution-undo: 3446eef21b1b160c45f226f960c64918cc4cf6db292ace5f1d0586f9c9e868d0 2026-09-02 7374617475733a206f70656e

### DW-98: Dependency-boundary validation is a hardcoded per-package allowlist rather than derived from actual restore/lock output
origin: migrated from legacy ledger ("Deferred from: code review of run-all-tests-and-fix-failures-2 (2026-07-20)"), 2026-08-25
location: scripts/validate-nuget-packages.py
reason: The legacy ledger defers this issue: Dependency-boundary validation is a hardcoded per-package allowlist rather than derived from actual restore/lock output. Original context is preserved in legacy-detail.
legacy-detail: - **Dependency-boundary validation is a hardcoded per-package allowlist rather than derived from actual restore/lock output** (e.g. `dotnet list package --include-transitive`) — inherently high-maintenance; this is the second time in this file's history a submodule/package version bump has required a manual allowlist update (see the CI Restore NU1107 memory for the sibling pattern). A more dynamic validation approach would eliminate this class of recurring CI break. Architectural, out of scope for a narrowly-scoped test-fix session. (`scripts/validate-nuget-packages.py`)
status: done 2026-09-02
resolution: resolved by sweep bundle dw-package-boundary-source
resolution-undo: 3446eef21b1b160c45f226f960c64918cc4cf6db292ace5f1d0586f9c9e868d0 2026-09-02 7374617475733a206f70656e

### DW-99: `NextPageAsync` unguarded `NextCursor==null`
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:484-487
reason: The legacy ledger defers this issue: `NextPageAsync` unguarded `NextCursor==null`. Original context is preserved in legacy-detail.
legacy-detail: - **`NextPageAsync` unguarded `NextCursor==null`** — the Next button is gated only on `!_snapshot.HasMore`; a backend contract violation (`HasMore==true` with a null `NextCursor`) would push the current cursor to history and set the cursor to null, bouncing the user to page 1 with a growing back-stack. Defensive only — the platform opaque-cursor contract guarantees a next cursor whenever `HasMore` is true. Follow-up: disable Next on `!HasMore || NextCursor is null`, or guard before consuming. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:484-487`)
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:452-458,1050-1058 requires a non-null cursor in both affordance enablement and handler execution.

### DW-100: Grid cannot return to the default `TenantId` ordering except via toolbar Reset
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor
reason: The legacy ledger defers this issue: Grid cannot return to the default `TenantId` ordering except via toolbar Reset. Original context is preserved in legacy-detail.
legacy-detail: - **Grid cannot return to the default `TenantId` ordering except via toolbar Reset** — only `tenant-id`→Name and `tenant-status`→Status are sortable; the `_ => TenantListSortColumns.TenantId` arm of `OnTenantSortChanged` is a defensive fallback (null/unknown `ColumnId`), and FluentDataGrid's 3-state "unsorted" third click cannot be represented (it re-forces Name/Status). While `SortColumn==TenantId` no visible column shows a sort indicator. UX limitation with a workaround (Reset). Follow-up: if return-to-default is desired, add an explicit affordance or map the unsorted event. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` `OnTenantSortChanged`)
status: done 2026-08-25
resolution: closed by human decision: Document toolbar Reset as the supported route back to default ordering.
decision: 2026-08-25 Accept reset-only — Document toolbar Reset as the supported route back to default ordering.

### DW-101: Brittle source-text "guard" tests + stale resource stub
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
location: tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs; tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs
reason: The legacy ledger defers this issue: Brittle source-text "guard" tests + stale resource stub. Original context is preserved in legacy-detail.
legacy-detail: - **Brittle source-text "guard" tests + stale resource stub** — several tests grep rendered/source text rather than assert behavior: `grid.ShouldNotContain("Cursor", Case.Insensitive)` (a common CSS/identifier word), `navigation.Split("Cursor = null").Length.ShouldBe(3)` (exact occurrence count), and `workspace.ShouldNotContain("ConfigureAwait(false)")` (source scan, not dispatcher-affinity proof) — they break on unrelated edits and can pass even if behavior regresses via a differently-named channel. Separately, the `TenantsWorkspaceTests` resource stub still defines the old `Tenants.List.ReturnContext` copy (containing "cursor") and removed `Tenants.List.Sort.*` keys, diverging from the corrected production resources. Test tech-debt. (`tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`, `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`)
status: done 2026-09-05
resolution: already resolved: commit d2b7ede359830c27934ac9f577e3073955c3e2c2 replaced source-text guards with behavioral tests; tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs:802-827 now reads the active production resource bundle and fails closed for unknown keys.

### DW-102: Duplicated tab/scope literal constants across two files
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:219-222; src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs
reason: The legacy ledger defers this issue: Duplicated tab/scope literal constants across two files. Original context is preserved in legacy-detail.
legacy-detail: - **Duplicated tab/scope literal constants across two files** — `TenantsWorkspace.razor` declares `TenantsTabId`/`UsersTabId`/`AllTenantsScope`/`MyTenantsScope` and `TenantWorkspaceState.cs` declares `TenantsTab`/`UsersTab`/`AllScope`/`MyScope` with the same `"tenants"/"users"/"all"/"mine"` values; `ApplyWorkspaceState` compares `state.Tab` (sourced from the state file's consts) against the razor file's consts. Value-equal today, but nothing enforces it — changing one string silently breaks tab/scope routing with no compile error. DRY nit. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:219-222`, `src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs`)
status: done 2026-09-06
resolution: already resolved: Commit 6e52036e centralizes workspace tab and scope identifiers in TenantWorkspaceState.

### DW-103: Redundant double a11y labeling on badges
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor; src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor
reason: The legacy ledger defers this issue: Redundant double a11y labeling on badges. Original context is preserved in legacy-detail.
legacy-detail: - **Redundant double a11y labeling on badges** — status/pending/truth badges set `IconLabel` **and** container `aria-label` **and** the same visible text; the host `aria-label` subsumes children so `IconLabel` is dead weight today, but if `aria-label` were later removed the icon label would surface a duplicate reading. a11y tidiness. (`src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`, `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor`)
status: open

### DW-104: Disclosed runtime-verification gaps
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
location: reasonCode
reason: The legacy ledger defers this issue: Disclosed runtime-verification gaps. Original context is preserved in legacy-detail.
legacy-detail: - **Disclosed runtime-verification gaps** — (1) the invalid-cursor page-one recovery wire-path (that the `list-tenants` query actually populates the `reasonCode` problem-details extension the gateway matches on, rather than only `detail`) rests on unit doubles; (2) AC8 per-width and forced-colors behavior is proven by grid-scoped CSS + bUnit/forced-colors conformance rather than full browser emulation, because the local Chrome lane exposes a fixed 1235px virtual viewport (window resize is a no-op). Both are disclosed in the story Debug Log and do not gate this read-only UI story per the Epic 1 convention. (`story evidence`)
status: open

### DW-105: Degenerate/exotic tenant ids on the shared detail-nav path
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs:36; src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor:17
reason: The legacy ledger defers this issue: Degenerate/exotic tenant ids on the shared detail-nav path. Original context is preserved in legacy-detail.
legacy-detail: - **Degenerate/exotic tenant ids on the shared detail-nav path** — (a) `TenantListNavigationContext.ToDetailUrl(TenantListRow)` now delegates to the new `ToDetailUrl(string tenantId, string anchor)` overload whose `ArgumentException.ThrowIfNullOrWhiteSpace(tenantId)` throws on a blank tenant id, where the pre-change inline body silently produced a `/tenants/?returnUrl=…` link; a render-time throw inside the `FluentDataGrid` template would tear down the list surface. (b) The row `id="{SelectorPrefix}-row-{context.TenantId}"` and the `tenants-my-row-{TenantId}` / `tenant-row-{TenantId}` focus anchors are built from the raw tenant id, so an id containing whitespace or CSS-significant characters produces an invalid HTML `id` and a non-resolving return-focus anchor. Both require a blank/exotic tenant id — the tenant id is the validated non-blank aggregate identifier and is slug-like in practice — and both share the pre-existing scope=all `TenantDataGrid` `id="tenant-row-{TenantId}"` pattern. Fix as cross-surface id-safety hardening (guard `DetailHrefFor`/normalize the anchor value across both grids), not a My-Tenants-only divergence. (`src/Hexalith.Tenants.UI/State/TenantList/TenantListNavigationContext.cs:36`; `src/Hexalith.Tenants.UI/Components/Users/MyTenantsDataGrid.razor:17`)
status: open

### DW-106: Audit-grid unsafe references are omitted from copying but remain visible in the rendered reference label
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: AuditDataGrid.razor
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
reason: The legacy ledger defers this issue: Audit-grid unsafe references are omitted from copying but remain visible in the rendered reference label. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md` summary: Audit-grid unsafe references are omitted from copying but remain visible in the rendered reference label. evidence: `AuditDataGrid.razor` sanitizes `EventReference` only for `SupportSafeCopyButton`; `ReferenceLabel(context)` still renders the raw reference and context, and the behavior predates this story's compatibility migration.
status: done 2026-08-27
resolution: resolved by sweep bundle dw-support-safe-copy-followup
resolution-undo: e32f9ec5cfa74f6e713b0d8ce939f3393f91d53d4b83c40ea3313864db2fc699 2026-08-27 7374617475733a206f70656e

### DW-107: Clipboard module import and write operations are not coordinated with component disposal
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: SupportSafeCopyButton.razor
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
reason: The legacy ledger defers this issue: Clipboard module import and write operations are not coordinated with component disposal. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md` summary: Clipboard module import and write operations are not coordinated with component disposal. evidence: `SupportSafeCopyButton.razor` inherited a disposal path that returns while `_module` is null and does not invalidate an import or write already in flight, allowing late interop or disposal races during navigation.
status: done 2026-08-27
resolution: resolved by sweep bundle dw-support-safe-copy-followup
resolution-undo: e32f9ec5cfa74f6e713b0d8ce939f3393f91d53d4b83c40ea3313864db2fc699 2026-08-27 7374617475733a206f70656e

### DW-108: Caller-supplied tenant identifiers are reused as raw DOM ids and return-focus anchors
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: TenantDataGrid.razor; MyTenantsDataGrid.razor
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
reason: The legacy ledger defers this issue: Caller-supplied tenant identifiers are reused as raw DOM ids and return-focus anchors. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md` summary: Caller-supplied tenant identifiers are reused as raw DOM ids and return-focus anchors. evidence: `TenantDataGrid.razor`, `MyTenantsDataGrid.razor`, and `TenantListNavigationContext.cs` embed literal identifiers in anchors, so whitespace or selector-significant characters can make focus restoration unreliable; this navigation pattern predates the copy change.
status: open

### DW-109: The audit receipt copies a hidden synthesized English composite rather than one exact visible localized literal
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: AuditEvidenceReceipt.razor
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
reason: The legacy ledger defers this issue: The audit receipt copies a hidden synthesized English composite rather than one exact visible localized literal. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md` summary: The audit receipt copies a hidden synthesized English composite rather than one exact visible localized literal. evidence: `TenantAuditReceipt.CopyableReferenceText` assembles hard-coded English labels and `AuditEvidenceReceipt.razor` approves that non-rendered multiline value, a pre-existing audit behavior exposed by the shared-component migration.
status: done 2026-08-27
resolution: resolved by sweep bundle dw-support-safe-copy-followup
resolution-undo: e32f9ec5cfa74f6e713b0d8ce939f3393f91d53d4b83c40ea3313864db2fc699 2026-08-27 7374617475733a206f70656e

### DW-110: Legacy configuration display safety remains a deny-list that can miss unrecognized secret formats
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: LegacyConfigurationDisplaySanitizer
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
reason: The legacy ledger defers this issue: Legacy configuration display safety remains a deny-list that can miss unrecognized secret formats. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md` summary: Legacy configuration display safety remains a deny-list that can miss unrecognized secret formats. evidence: `LegacyConfigurationDisplaySanitizer` preserves the pre-existing command-preview display policy by accepting every non-empty key/value pair that lacks listed fragments, so values such as unknown API-key formats may still render until Story 1.6 supplies a positive safe model.
status: done 2026-08-25
resolution: already resolved: commit 5a401654; src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationSafeComposer.cs:36-48,167-186 replaced the deny-list sanitizer with explicitly display-safe keys and rows.

### DW-111: Configuration keys that fail the legacy display-safety policy remain visible even while their paired values are redacted
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: TenantConfigurationView.razor
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
reason: The legacy ledger defers this issue: Configuration keys that fail the legacy display-safety policy remain visible even while their paired values are redacted. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md` summary: Configuration keys that fail the legacy display-safety policy remain visible even while their paired values are redacted. evidence: `TenantConfigurationView.razor` always renders `context.Key`; `LegacyConfigurationDisplaySanitizer.IsDisplayable(key, value)` only controls value replacement, so a key containing a known sensitive literal remains exposed in the DOM and accessibility label. This display behavior predates the copy-policy change.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationSafeComposer.cs:167-186 and TenantConfigurationView.razor:116-147 exclude unsafe keys from the safe model.

### DW-112: The authoritative-search status filter's visible label and accessible name can describe different scopes
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: TenantsWorkspace.razor
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The authoritative-search status filter's visible label and accessible name can describe different scopes. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The authoritative-search status filter's visible label and accessible name can describe different scopes. evidence: `TenantsWorkspace.razor` selects `StatusFilterLabelKey` for the visible label but retains the page-local `Tenants.List.StatusFilterLabel` for `aria-label`; this mismatch predates the current Story 1.9 review-repair diff.
status: open

### DW-113: An unmapped list reason renders its raw resource key as user-visible copy in the shared list-state surface
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: ListSurfaceStates.razor; TenantsResources.resx
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: An unmapped list reason renders its raw resource key as user-visible copy in the shared list-state surface. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: An unmapped list reason renders its raw resource key as user-visible copy in the shared list-state surface. evidence: `ListSurfaceStates.razor` resolves `Localizer["Tenants.List.Reason.{Reason}"]` for any non-`None` reason, but only 5 of the 10 `TenantListReason` members have a `Tenants.List.Reason.*` key in `TenantsResources.resx`; an unmapped reason therefore renders the literal key in EN and FR. No currently reachable call site passes an unmapped reason, so this is a latent pre-existing trap in the shared component rather than a live defect of this story.
status: open

### DW-114: The advance-by-requested-window paging rule rests on a Memories server premise that no test in this repository can observe
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: references/
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The advance-by-requested-window paging rule rests on a Memories server premise that no test in this repository can observe. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The advance-by-requested-window paging rule rests on a Memories server premise that no test in this repository can observe. evidence: Correctness of `nextOffset = min(rawOffset + PageSize, TotalCount)` requires the Memories search server to apply `Offset` before dropping entries that fail its required-field check and to report the untrimmed total. That is true of `SyntacticSearchService` in the consumed submodule today, but `SearchResult.TotalCount` documents only "may exceed returned results", every gateway test stubs `MemoriesClient.SearchAsync`, and the intent's Block-If bars editing anything under `references/`. Closing this needs a contract test in the Memories repository or a Tenants integration test against a live index.
status: open

### DW-115: The tenant-detail read path does not adopt the shared null-member guard, so a malformed member element crashes the detail page while both list surfaces degrade safely
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: TenantQueryGateway.HasUsableMembers
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The tenant-detail read path does not adopt the shared null-member guard, so a malformed member element crashes the detail page while both list surfaces degrade safely. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The tenant-detail read path does not adopt the shared null-member guard, so a malformed member element crashes the detail page while both list surfaces degrade safely. evidence: `TenantQueryGateway.HasUsableMembers` is applied to search hydration and ordinary-list enrichment but not to `GetTenantAsync`, which feeds the identical `TenantDetail` payload to `TenantDetailPage.OwnerCount` and `MemberAccessReview.OwnerCount`; both dereference member elements during render, so a `Members` array containing a null element throws `NullReferenceException` and tears down the circuit. `TenantConfigurationSafeComposer.SanitizeDetail` copies the collection and preserves the null element. The detail-page dereference predates Story 1.9; this story only made the asymmetry visible by guarding the two list paths.
status: done 2026-09-05
resolution: already resolved: commit 0891897533227cd63b0f6a84bd9694a17725e65b; src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:302-310,456-460 rejects a detail containing a null member as InvalidPayload before Razor, covered at TenantsRestQueryClientTests.cs:666-691.

### DW-116: The "exactly one polite live region" proof is an artefact of bUnit's missing shadow DOM
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27)"), 2026-08-25
location: TenantListSurfaceTests.cs:1866
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The "exactly one polite live region" proof is an artefact of bUnit's missing shadow DOM. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The "exactly one polite live region" proof is an artefact of bUnit's missing shadow DOM. evidence: `TenantListSurfaceTests.cs:1866` counts live regions in markup where `<fluent-message-bar>` renders as an inert custom element. The shipped Fluent v5 module sets `role="status" aria-live="polite"` on each bar's internal dialog at runtime, so a real browser nests a live region per bar inside the workspace's outer one. The helper degenerates to `0.ShouldBe(0)` on the empty-notice call. status: open — blocked on BROWSER-SEARCH-1.9 and AT-NVDA-1.9, both already open.
status: open

### DW-117: Surfacing codec exceptions escape the gateway and reach an unguarded LoadAsync, tearing down the Blazor circuit
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27)"), 2026-08-25
location: TenantQueryGateway.cs:1019; TenantsWorkspace.razor:632
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: Surfacing codec exceptions escape the gateway and reach an unguarded LoadAsync, tearing down the Blazor circuit. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: Surfacing codec exceptions escape the gateway and reach an unguarded LoadAsync, tearing down the Blazor circuit. evidence: `TenantQueryGateway.cs:1019` deliberately re-raises `ObjectDisposedException`, `NullReferenceException`, `ArgumentNullException`, `OutOfMemoryException`; `TenantsWorkspace.razor:632` catches only `OperationCanceledException`. A disposed Data Protection provider during host shutdown therefore kills the circuit where every other cursor-protection failure degrades to the ordinary list. Documented as deliberate in the source comments, and shutdown-time disposal is benign, so recorded rather than patched. status: open
status: open

### DW-118: The codec argument guard straddles the contained/surfacing partition, so its null and empty halves produce opposite outcomes
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27)"), 2026-08-25
location: TenantQueryGateway.cs:1025
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The codec argument guard straddles the contained/surfacing partition, so its null and empty halves produce opposite outcomes. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The codec argument guard straddles the contained/surfacing partition, so its null and empty halves produce opposite outcomes. evidence: `QueryCursorCodec` uses `ArgumentException.ThrowIfNullOrWhiteSpace`, which throws `ArgumentNullException` for null and `ArgumentException` for empty. `TenantQueryGateway.cs:1025` excludes `ArgumentNullException` before the `ArgumentException` base match, so null escapes to circuit teardown while empty degrades to the ordinary list. Not reachable today: `TenantSearchCursorScopes.Create` never returns null and `TenantSearchCursorPosition.Format` never returns empty. status: open
status: open

### DW-119: The pager is unmounted on every load, dropping keyboard focus from the button the operator just pressed
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27)"), 2026-08-25
location: TenantsWorkspace.razor:534
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The pager is unmounted on every load, dropping keyboard focus from the button the operator just pressed. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The pager is unmounted on every load, dropping keyboard focus from the button the operator just pressed. evidence: `TenantsWorkspace.razor:534` sets `_snapshot = TenantListSnapshot.Loading()`, making `ShowList`, `HasMore` and `HasPreviousPage` all false, so `ShowPager` (`:416`) removes the whole `<nav>` for the duration of every load. Needs the authenticated browser lane to confirm the focus consequence. status: open — needs BROWSER-SEARCH-1.9.
status: open

### DW-120: The CI package-boundary gate asserts the fixture it generates from its own allowlist
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27)"), 2026-08-25
location: scripts/validate-nuget-packages.py:64; CiQualityGateScriptTests.cs:309
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The CI package-boundary gate asserts the fixture it generates from its own allowlist. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The CI package-boundary gate asserts the fixture it generates from its own allowlist. evidence: `CiQualityGateScriptTests.cs:309` mirrors `scripts/validate-nuget-packages.py:64`; `ExpectedDependencies` is used to synthesise the `.nupkg` fixtures fed to the script, so the test verifies only that two copies of the same literal agree, never that `Microsoft.Extensions.Http.Resilience` is genuinely upstream-owned. Widening the allowlist to silence a real leak would pass. status: open
status: done 2026-09-02
resolution: resolved by sweep bundle dw-package-boundary-source
resolution-undo: 3446eef21b1b160c45f226f960c64918cc4cf6db292ace5f1d0586f9c9e868d0 2026-09-02 7374617475733a206f70656e

### DW-121: The shared release workflow documents `source-branch` as configurable even though the established publication policy accepts only `main`
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27)"), 2026-08-25
location: domain-release.yml
source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-30240946791-89897853390.md`
reason: The legacy ledger defers this issue: The shared release workflow documents `source-branch` as configurable even though the established publication policy accepts only `main`. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-30240946791-89897853390.md` summary: The shared release workflow documents `source-branch` as configurable even though the established publication policy accepts only `main`. evidence: `domain-release.yml` and the pre-existing publication preflight reject every source branch except `main`, while the reusable-workflow input description says only that it is an exact protected source branch; resolving that public contract is broader than the stale-release race fix.
status: open
decision: 2026-08-26 Main-only contract — Remove configurable-branch ambiguity and enforce and document main across the workflow and callers.
decision: 2026-08-25 Main-only contract — Remove configurable-branch ambiguity and enforce and document main across shared workflow and callers.

### DW-122: Per-page candidate dedup lets one tenant render on two consecutive authoritative search pages
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27, pass 3)"), 2026-08-25
location: spec-1-9-…-paging.md:238
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: Per-page candidate dedup lets one tenant render on two consecutive authoritative search pages. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: Per-page candidate dedup lets one tenant render on two consecutive authoritative search pages. evidence: `TenantQueryGateway.BuildAuthoritativeSearchSnapshotAsync` builds its `seen` set per raw window, so a tenant the index returns in two overlapping windows is rendered twice across consecutive pages. Closing it needs either an index uniqueness guarantee (upstream, barred by this spec's Block-If) or a cross-page seen-set carried in the protected cursor, which would place reconstructable index material into protected state and violate this story's own cursor constraints. Reclassified from patch to deferred during the 2026-07-27 pass-2 application and marked `[x]` at `spec-1-9-…-paging.md:238`, but never entered this ledger — the pass-3 review found it invisible to ledger triage. Recorded here now. status: open — blocked on an upstream index uniqueness guarantee or a cursor design that carries no reconstructable index material.
status: open
decision: 2026-08-26 Upstream uniqueness — Make Memories guarantee stable unique tenant candidates and add an owning contract test.
decision: 2026-08-25 Upstream uniqueness — Make Memories guarantee stable unique tenant candidates and add a contract test.

### DW-123: A partially hidden search window still advertises, through a live Next control beside its surviving rows, that the window held more than it rendered
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27, pass 3)"), 2026-08-25
location: TenantQueryGateway.cs:864
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: A partially hidden search window still advertises, through a live Next control beside its surviving rows, that the window held more than it rendered. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: A partially hidden search window still advertises, through a live Next control beside its surviving rows, that the window held more than it rendered. evidence: `TenantQueryGatewayTests` pins as intended behaviour a window where five of six candidates were dropped (forbidden, not-found, null detail, id mismatch, degraded) yielding one row plus `HasMore = true` and a minted cursor at offset 6. The window-collapse rule at `TenantQueryGateway.cs:864` closes the fully hidden case only. Closing the partial case means not exposing per-page authorized counts through pager state at all. Reviewed with the story owner on 2026-07-27 and accepted as out of scope for this story; tracked in the evidence report as PARTIAL-WINDOW-DISCLOSURE-1.9. status: open — accepted out of scope; reopen trigger is any requirement that a partially hidden window be indistinguishable from a complete one.
status: done 2026-08-25
resolution: closed by human decision: Retain the approved Story 1.9 residual and reopen only for a stronger privacy requirement.
decision: 2026-08-25 Accept owner decision — Retain the approved Story 1.9 residual and reopen only for a stronger privacy requirement.

### DW-124: The pass-2 finding "Seven new Lifecycle bindings unverified end-to-end on any real surface" was checked off after closing 1 of 13 binding sites
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27, pass 3)"), 2026-08-25
location: TenantDataGrid.razor:76; MyTenantsDataGrid.razor:75
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The pass-2 finding "Seven new Lifecycle bindings unverified end-to-end on any real surface" was checked off after closing 1 of 13 binding sites. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The pass-2 finding "Seven new Lifecycle bindings unverified end-to-end on any real surface" was checked off after closing 1 of 13 binding sites. evidence: `grep 'Lifecycle="'` finds 13 sites — `TenantDataGrid.razor:76`, `MyTenantsDataGrid.razor:75`, `AuditDataGrid.razor:54`, `GlobalAdministratorsPage.razor:347`, `TenantDetailPage.razor:115/144/165/186`, `TenantConfigurationView.razor:15/129`, `MemberAccessReview.razor:19/116`, `TenantLifecycleActionAvailability.razor:25`. Only `TenantDataGrid` gained a rendered-lifecycle assertion (`TenantListSurfaceTests.cs:1002-1005`), and it was mutation-verified. `truth-state-badge--*` appears nowhere else outside `TruthStateBadgeTests`, which the original finding already deemed insufficient. The 12 remaining sites are other stories' surfaces (tenant detail, configuration, member review, audit, global administrators), so covering them is not story-1.9 work. status: open — the pass-2 checkbox at `spec-1-9-…-paging.md:243` should be corrected to record partial closure.
status: done 2026-08-25
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:55-62, GlobalAdministratorsPageTests.cs:415-422, TenantDetailSurfaceTests.cs:3803-3929, MyTenantsSurfaceTests.cs:61-65, and TenantLifecycleActionAvailabilityTests.cs:122-126 cover the formerly missing rendered lifecycle surfaces.

### DW-125: A third divergent global-administrator claim parser now coexists with the existing two, so the same signed-in user can be a proven administrator for configuration and Indeterminate for tenant lifecycle
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration.md (2026-07-27)"), 2026-08-25
location: _bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md; TenantConfigurationPrincipalResolver.cs:102-194
source_spec: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md`
reason: The legacy ledger defers this issue: A third divergent global-administrator claim parser now coexists with the existing two, so the same signed-in user can be a proven administrator for configuration and Indeterminate for tenant lifecycle. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md` summary: A third divergent global-administrator claim parser now coexists with the existing two, so the same signed-in user can be a proven administrator for configuration and Indeterminate for tenant lifecycle. evidence: `TenantConfigurationPrincipalResolver.cs:102-194` vs `Services/Gateways/TenantsGlobalAdministratorClaims.cs`. Four divergences verified at `ec7ec8c` and still present at HEAD: malformed JSON role array yields Indeterminate in the new resolver but falls through to delimiter parsing in the old one; an unparseable `global_admin` yields Indeterminate vs `false`; `{`-prefixed role values yield Indeterminate vs split-parsed; claims are read across all identities in the old parser but only from the single authenticated identity in the new one. Consolidating into one three-state resolver that the boolean parser collapses would touch lifecycle and global-administrator surfaces owned by other stories. status: open — needs a cross-story owner; reopen trigger is any new surface that needs administrator evidence, or a reported disagreement between configuration and lifecycle authorization for the same user.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs:90-95 delegates to the corroborated TenantsGlobalAdministratorClaims parser.

### DW-126: Lifecycle and global-administrator authorization reflections still read `HttpContext.User` with no circuit fallback, while the new configuration path has one
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration.md (2026-07-27)"), 2026-08-25
location: _bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md
source_spec: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md`
reason: The legacy ledger defers this issue: Lifecycle and global-administrator authorization reflections still read `HttpContext.User` with no circuit fallback, while the new configuration path has one. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md` summary: Lifecycle and global-administrator authorization reflections still read `HttpContext.User` with no circuit fallback, while the new configuration path has one. evidence: `LifecycleAuthorizationReflection` and `GlobalAdministratorsAuthorizationReflection` resolve `httpContextAccessor?.HttpContext?.User` only; during interactive circuit activity there is no `HttpContext`, so these reflections can disagree with the configuration path for the same user on the same page. Pre-existing before Story 1.6 and outside its declared file scope; Story 1.6 only made the asymmetry visible by adding the circuit-aware path. status: open — pre-existing; fold into the claim-parser consolidation above or into Story 1.10/1.11 identity work.
status: open

### DW-127: Partially hidden authoritative-search windows disclose hidden candidates through paging state
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
location: TenantQueryGateway.cs:890
reason: The legacy ledger defers this issue: Partially hidden authoritative-search windows disclose hidden candidates through paging state. Original context is preserved in legacy-detail.
legacy-detail: - **Partially hidden authoritative-search windows disclose hidden candidates through paging state.** This is the already-recorded Story 1.9 `PARTIAL-WINDOW-DISCLOSURE-1.9` residual: `TenantQueryGateway.cs:890` renders surviving rows while retaining a `HasMore` value derived from the raw pre-authorization total. It is pre-existing relative to the Story 1.6 trust-boundary chunk and remains owned by Story 1.9.
status: open
decision: 2026-08-28 Await stronger requirement
decision: 2026-08-28 Await stronger requirement

### DW-128: Search hydration conflates forbidden and missing candidates when deciding whether to end paging
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
location: TenantQueryGateway.cs:1013
reason: The legacy ledger defers this issue: Search hydration conflates forbidden and missing candidates when deciding whether to end paging. Original context is preserved in legacy-detail.
legacy-detail: - **Search hydration conflates forbidden and missing candidates when deciding whether to end paging.** `TenantQueryGateway.cs:1013` classifies both 403 and 404 as `HiddenOrAbsent`; an all-404 stale-index window can therefore collapse paging and make later authorized matches unreachable. This is pre-existing relative to Story 1.6 and should be resolved with the Story 1.9 paging contract so anti-enumeration behavior remains coherent.
status: open
decision: 2026-08-26 Distinct internal results — Keep external responses identical but let only forbidden candidates terminate hidden-window paging.
decision: 2026-08-25 Distinct internal results — Keep external responses identical but let only forbidden candidates terminate hidden-window paging.

### DW-129: The release tag floor guard probes NuGet only; the container tag in registry.hexalith.com is never checked
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: The release tag floor guard probes NuGet only; the container tag in registry.hexalith.com is never checked. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: The release tag floor guard probes NuGet only; the container tag in registry.hexalith.com is never checked. evidence: publication_preflight.py fails with the same version-collision on the container repository (validate_container_absence), so a partial prior release that left a container tag can still fail the protected job after approval. The unprotected verify-source job has no registry credentials, so covering it needs a design decision.
status: done 2026-08-26
resolution: already resolved: references/Hexalith.Builds/.github/workflows/domain-release.yml:496 and scripts/publication_preflight.py:930-956,1155-1185 perform protected exact container-tag collision checks; implemented by Builds commits f271c8aa and 2a8b63d2.
decision: 2026-08-25 Protected prewrite check — Use production read credentials immediately after approval but before any package or container write.

### DW-130: The tag floor is proved in verify-source but never re-proved after the production approval gate
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: The tag floor is proved in verify-source but never re-proved after the production approval gate. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: The tag floor is proved in verify-source but never re-proved after the production approval gate. evidence: environment-name production can hold for hours; a tag deleted or added during that window reproduces the original incident with a green guard behind it. Re-asserting inside the release job, or pinning the resolved floor as a job output, would close it.
status: done 2026-08-26
resolution: already resolved: references/Hexalith.Builds/.github/workflows/domain-release.yml:811-842,889-907 re-proves live source and resolves a fresh release candidate after approval; implemented by Builds commits bd94f7fe, f271c8aa, and bf9af9cb.

### DW-131: The guard fails on any published version above the floor, even when the version semantic-release would actually propose is free, with no override
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: The guard fails on any published version above the floor, even when the version semantic-release would actually propose is free, with no override. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: The guard fails on any published version above the floor, even when the version semantic-release would actually propose is free, with no override. evidence: floor v3.2.18 with a published 3.3.0-3.15.1 band and a breaking change in range proposes 4.0.0, which is free, yet the guard exits 1. There is no workflow_dispatch acknowledgement input, so the only escape is mutating tags.
status: done 2026-08-27
resolution: already resolved: Hexalith.Builds commit bd94f7fe; references/Hexalith.Builds/.github/workflows/domain-release.yml:889-907 dry-runs semantic-release and collision-checks the exact proposed version.
decision: 2026-08-26 Check exact proposal — Calculate semantic-release's exact proposal before approval and collision-check that version.
decision: 2026-08-25 Check proposed version — Calculate semantic-release's exact proposal before approval and collision-check that version.

### DW-132: The release-published tenants container image fails to start under Production defaults, failing the container smoke test and aborting every release after packages are already pushed
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: The release-published tenants container image fails to start under Production defaults, failing the container smoke test and aborting every release after packages are already pushed. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: none summary: The release-published tenants container image fails to start under Production defaults, failing the container smoke test and aborting every release after packages are already pushed. evidence: Run 30340676669 evidence artifact, smoke-linux-amd64.log - OptionsValidationException requires Authentication:JwtBearer:Authority to be an absolute HTTPS URI (published appsettings.json has "") and requires SigningKey to be empty (it is not, in the container). amd64 exited 139, arm64 hit liveness-timeout. Only host-affecting change since the last successful release b3d01c53 is a7ca142, which moved Hexalith.EventStore.Gateway to a PackageReference on the non-source path, changing which appsettings.json wins in the container publish. Blocks release completion, not just this one.
status: done 2026-08-25
resolution: already resolved: commit 5efbbe75; .github/workflows/release.yml:281-287 pins the Builds smoke fix, whose smoke_container_platforms.py:44,213-223 uses Development hosting and explicit safe smoke authentication.

### DW-133: BMAD workflow render files (`_bmad/render/bmad-quick-dev/step-05-present.md`, `step-oneshot.md`, `workflow.md`) were modified inside the Story 1.10 diff, adding gitlink-validator instructions. Real and probably desirable, but it is tooling maintenance unrelated to Story 1.10's acceptance criteria and outside the spec's authorized doc outputs (evidence file + `tests/test-summary.md`). Should land as its own `docs` commit rather than inside a feature story (Hexalith commitlint forbids `chore`)
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-28)"), 2026-08-25
location: tests/test-summary.md; _bmad/render/bmad-quick-dev/step-05-present.md
reason: The legacy ledger defers this issue: BMAD workflow render files (`_bmad/render/bmad-quick-dev/step-05-present.md`, `step-oneshot.md`, `workflow.md`) were modified inside the Story 1.10 diff, adding gitlink-validator instructions. Original context is preserved in legacy-detail.
legacy-detail: - BMAD workflow render files (`_bmad/render/bmad-quick-dev/step-05-present.md`, `step-oneshot.md`, `workflow.md`) were modified inside the Story 1.10 diff, adding gitlink-validator instructions. Real and probably desirable, but it is tooling maintenance unrelated to Story 1.10's acceptance criteria and outside the spec's authorized doc outputs (evidence file + `tests/test-summary.md`). Should land as its own `docs` commit rather than inside a feature story (Hexalith commitlint forbids `chore`).
status: open

### DW-134: Deferred to Story 1.11
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-28)"), 2026-08-25
location: TenantConfigurationPrincipalResolver.cs:17-48
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** — Principal-resolution precedence was inverted in `TenantConfigurationPrincipalResolver.cs:17-48`: the circuit `AuthenticationStateProvider` now outranks `HttpContext.User`, where previously `HttpContext` was primary. A circuit whose provider returns an anonymous or not-yet-populated state while `HttpContext.User` is authenticated collapses to `Indeterminate` and fails every configuration grant closed. Security-relevant; must be decided against Story 1.11's acceptance criteria, not 1.10's. **CLOSED (2026-08-08, Story 1.11):** owner retained circuit-over-HTTP precedence with no request-principal fallback; see spec Scope Attribution decision 1.
status: done 2026-08-08
resolution: Legacy completion record: - **Deferred to Story 1.11** — Principal-resolution precedence was inverted in `TenantConfigurationPrincipalResolver.cs:17-48`: the circuit `AuthenticationStateProvider` now outranks `HttpContext.User`, where previously `HttpContext` was primary. A circuit whose provider returns an anonymous or not-yet-populated state while `HttpContext.User` is authenticated collapses to `Indeterminate` and fails every configuration grant closed. Security-relevant; must be decided against Story 1.11's acceptance criteria, not 1.10's. **CLOSED (2026-08-08, Story 1.11):** owner retained circuit-over-HTTP precedence with no request-principal fallback; see spec Scope Attribution decision 1.

### DW-135: Deferred to Story 1.11
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-28)"), 2026-08-25
location: docs/production-auth-claim-contract.md; TenantsGlobalAdministratorClaims.cs:36-46
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** — `TenantsGlobalAdministratorClaims.Evaluate` now requires exactly one authenticated identity carrying exactly one literal `sub` claim (`TenantsGlobalAdministratorClaims.cs:36-46`). Any handler mapping `sub` to `ClaimTypes.NameIdentifier` (the ASP.NET default), or any principal with two authenticated identities (cookie + bearer), denies a genuine global administrator. Confirm the intended claim contract against `docs/production-auth-claim-contract.md` as part of 1.11. **CLOSED (2026-08-08, Story 1.11):** owner requires exactly one *distinct* literal `sub` value; identical duplicates accepted. See spec loop-2 decision + Scope Attribution decision 2.
status: done 2026-08-08
resolution: Legacy completion record: - **Deferred to Story 1.11** — `TenantsGlobalAdministratorClaims.Evaluate` now requires exactly one authenticated identity carrying exactly one literal `sub` claim (`TenantsGlobalAdministratorClaims.cs:36-46`). Any handler mapping `sub` to `ClaimTypes.NameIdentifier` (the ASP.NET default), or any principal with two authenticated identities (cookie + bearer), denies a genuine global administrator. Confirm the intended claim contract against `docs/production-auth-claim-contract.md` as part of 1.11. **CLOSED (2026-08-08, Story 1.11):** owner requires exactly one *distinct* literal `sub` value; identical duplicates accepted. See spec loop-2 decision + Scope Attribution decision 2.

### DW-136: `EventStore:BaseAddress` already accepted Aspire compound schemes before Story 1.10, but neither the EventStore gateway client nor the tenant command client attaches `.AddServiceDiscovery()`. A compound address can therefore be marked connected and fail when sent. This is real command/status transport debt, but it predates the active direct-read change and remains outside the chunk-1 patch set
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:79
reason: The legacy ledger defers this issue: `EventStore:BaseAddress` already accepted Aspire compound schemes before Story 1.10, but neither the EventStore gateway client nor the tenant command client attaches `.AddServiceDiscovery()`. Original context is preserved in legacy-detail.
legacy-detail: - `EventStore:BaseAddress` already accepted Aspire compound schemes before Story 1.10, but neither the EventStore gateway client nor the tenant command client attaches `.AddServiceDiscovery()`. A compound address can therefore be marked connected and fail when sent. This is real command/status transport debt, but it predates the active direct-read change and remains outside the chunk-1 patch set. **Canonical open entry** for this debt (later 2026-07-30 chunk A+B item is a duplicate reaffirmation). [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:79]
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:162-167 rejects compound service-discovery schemes and accepts only exact http/https.

### DW-137: Future feature — reversible route identifiers:
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:530
reason: The legacy ledger defers this issue: Future feature — reversible route identifiers:. Original context is preserved in legacy-detail.
legacy-detail: - **Future feature — reversible route identifiers:** Define an explicit backend route contract for literal tenant/user identifiers containing `/`, then update the six direct-read endpoints and clients to round-trip that representation. Until this is delivered, the direct-read client must fail closed for this identifier class rather than issue an ambiguous encoded-slash request. Owner: future Tenants API route-contract work. Reason: future feature. [src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:530]
status: open
decision: 2026-08-27 Define reversible routes — Define and implement a reversible identifier route contract across all six API endpoints and clients, with slash and dot round-trip and traversal-safety tests.
decision: 2026-08-27 Define reversible routes — Define and implement a reversible identifier route contract across all six API endpoints and clients, with slash and dot round-trip and traversal-safety tests.

### DW-138: Deferred to Story 1.11
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29, review loop 4)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1178
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** — `ApplyAuthenticationStateChangedAsync` authorizes the page with the uncorroborated `TenantsGlobalAdministratorClaims.Evaluate` (`requireCorroboration: false`, so `sub` is never checked against `IUserContextAccessor.UserId`), then calls `LoadAsync(reuseETag: false, reauthorize: false)` to deliberately skip the corroborated path every other caller uses. A token refresh raising `AuthenticationStateChanged` with a principal whose `sub` does not match the server-side user context makes the grant/remove mutation surface reachable for the rest of the circuit. Security-relevant. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1178] **CLOSED (2026-08-08, Story 1.11):** authentication transitions use the strict BFF/circuit resolver; uncorroborated Evaluate path removed. See applied GA/workspace patches.
status: done 2026-08-08
resolution: Legacy completion record: - **Deferred to Story 1.11** — `ApplyAuthenticationStateChangedAsync` authorizes the page with the uncorroborated `TenantsGlobalAdministratorClaims.Evaluate` (`requireCorroboration: false`, so `sub` is never checked against `IUserContextAccessor.UserId`), then calls `LoadAsync(reuseETag: false, reauthorize: false)` to deliberately skip the corroborated path every other caller uses. A token refresh raising `AuthenticationStateChanged` with a principal whose `sub` does not match the server-side user context makes the grant/remove mutation surface reachable for the rest of the circuit. Security-relevant. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1178] **CLOSED (2026-08-08, Story 1.11):** authentication transitions use the strict BFF/circuit resolver; uncorroborated Evaluate path removed. See applied GA/workspace patches.

### DW-139: Deferred to Story 1.11
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29, review loop 4)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:116
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** — `ResolveSystemScopeEvidence` returns `null` (→ `Indeterminate`) when the principal carries more than one distinct `eventstore:tenant` claim value, replacing a previous any-match `HasClaim(… == "system")`. A platform administrator whose token carries both `system` and a tenant scope now loses the Global Administrators page, the workspace entry link, and — because `GlobalAdministratorPolicy` was switched to `Evaluate(...) == Authorized` — every policy-gated FrontComposer surface. Extends the already-recorded single-identity/single-`sub` concern from the 2026-07-28 entry to the scope claim. [src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:116] **CLOSED (2026-08-08, Story 1.11):** retained as intentional fail-closed contract for conflicting system-scope evidence; no longer awaiting a 1.11 decision.
status: done 2026-08-08
resolution: Legacy completion record: - **Deferred to Story 1.11** — `ResolveSystemScopeEvidence` returns `null` (→ `Indeterminate`) when the principal carries more than one distinct `eventstore:tenant` claim value, replacing a previous any-match `HasClaim(… == "system")`. A platform administrator whose token carries both `system` and a tenant scope now loses the Global Administrators page, the workspace entry link, and — because `GlobalAdministratorPolicy` was switched to `Evaluate(...) == Authorized` — every policy-gated FrontComposer surface. Extends the already-recorded single-identity/single-`sub` concern from the 2026-07-28 entry to the scope claim. [src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:116] **CLOSED (2026-08-08, Story 1.11):** retained as intentional fail-closed contract for conflicting system-scope evidence; no longer awaiting a 1.11 decision.

### DW-140: Deferred to Story 1.11
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29, review loop 4)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1235
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** — A single transient authorization-resolution fault is indistinguishable from a permanent denial: `ResolveAuthorizationReflectionAsync` swallows every exception to `Indeterminate`, `CollapseAuthorizationAsync` then pins the restricted surface, and that surface offers no Refresh, Retry or Reset while `EnsureReadRefreshLeaseAsync` and `CanRecover` are both gated on `IsAuthorized`. Nothing re-enters resolution unless an `AuthenticationStateChanged` happens to fire. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1235] **CLOSED (2026-08-08, Story 1.11):** Indeterminate Retry / RetryAuthorization path applied; see GA page patches.
status: done 2026-08-08
resolution: Legacy completion record: - **Deferred to Story 1.11** — A single transient authorization-resolution fault is indistinguishable from a permanent denial: `ResolveAuthorizationReflectionAsync` swallows every exception to `Indeterminate`, `CollapseAuthorizationAsync` then pins the restricted surface, and that surface offers no Refresh, Retry or Reset while `EnsureReadRefreshLeaseAsync` and `CanRecover` are both gated on `IsAuthorized`. Nothing re-enters resolution unless an `AuthenticationStateChanged` happens to fire. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1235] **CLOSED (2026-08-08, Story 1.11):** Indeterminate Retry / RetryAuthorization path applied; see GA page patches.

### DW-141: Deferred to Story 1.11
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29, review loop 4)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:602
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** — The workspace's Global Administrators entry link evaluates authorization uncorroborated while initial resolution uses the corroborated resolver, so the link and the page it targets desynchronize in both directions after any `AuthenticationStateChanged`: the button can render for a principal the page then refuses, or hide while the claims are in fact sufficient. [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:602] **CLOSED (2026-08-08, Story 1.11):** workspace authentication transitions resolve through the strict BFF seam before restoring the entry.
status: done 2026-08-08
resolution: Legacy completion record: - **Deferred to Story 1.11** — The workspace's Global Administrators entry link evaluates authorization uncorroborated while initial resolution uses the corroborated resolver, so the link and the page it targets desynchronize in both directions after any `AuthenticationStateChanged`: the button can render for a principal the page then refuses, or hide while the claims are in fact sufficient. [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:602] **CLOSED (2026-08-08, Story 1.11):** workspace authentication transitions resolve through the strict BFF seam before restoring the entry.

### DW-142: Retained direct-read snapshots are scoped by entity, filter and paging inputs but not by the authenticated subject, so a principal change inside one scoped circuit can expose the previous subject's authorized rows during a failure or an insensitive `304` response
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29, core transport/state follow-up)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:2075
source_spec: `_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md`
reason: The legacy ledger defers this issue: Retained direct-read snapshots are scoped by entity, filter and paging inputs but not by the authenticated subject, so a principal change inside one scoped circuit can expose the previous subject's authorized rows during a failure or an insensitive `304` response. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md` summary: Retained direct-read snapshots are scoped by entity, filter and paging inputs but not by the authenticated subject, so a principal change inside one scoped circuit can expose the previous subject's authorized rows during a failure or an insensitive `304` response. evidence: The gateway retention helpers at `src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:2075` have no subject dimension, while the scoped server-circuit user context can resolve a new principal instance. The generic retained-snapshot behavior predates Story 1.10's direct-read change. status: open — pre-existing security debt; bind retained evidence to a stable authenticated-subject identity or invalidate all retained snapshots when that identity changes.
status: open

### DW-143: `EventStore:BaseAddress` is accepted with compound service-discovery schemes (e.g. `https+http://eventstore`) by the same `TryGetHttpBaseAddress` gate used for the read side, but no service discovery is attached to the command/status clients, so such a value can only fail at send time
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-30, chunk A+B transport/gateway)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:96
source_spec: `_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md`
reason: The legacy ledger defers this issue: `EventStore:BaseAddress` is accepted with compound service-discovery schemes (e.g. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md` summary: `EventStore:BaseAddress` is accepted with compound service-discovery schemes (e.g. `https+http://eventstore`) by the same `TryGetHttpBaseAddress` gate used for the read side, but no service discovery is attached to the command/status clients, so such a value can only fail at send time. evidence: The scheme gate is shared at `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:96`, while `.AddServiceDiscovery()` is attached only to the Tenants read client at `:74`. Pre-existing: the command-side gate predates Story 1.10's read transport. status: closed-as-duplicate (2026-08-08, Story 1.11 loop 7) — reaffirmation only; keep the 2026-07-29 `EventStore:BaseAddress` / missing `.AddServiceDiscovery()` bullet as the single open entry. Resolve together with the read-side service-discovery provider decision recorded in the 2026-07-30 review findings.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:162-167 rejects compound service-discovery schemes and accepts only exact http/https.

### DW-144: Decide whether the Tenants UI BFF's six canonical reads should move from direct HTTPS to DAPR service invocation. Raised by the owner during the 1.10 review: DAPR is the intended discovery mechanism for services with sidecars. Not actionable inside 1.10 — it is a topology and security-posture change, not a base-address swap
origin: migrated from legacy ledger ("Architectural decision recorded by code review of spec-1-10 (2026-07-30) — BFF read transport vs DAPR service invocation"), 2026-08-25
location: src/Hexalith.Tenants.AppHost/Program.cs:104-106; src/Hexalith.Tenants.UI
source_spec: `_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md`
reason: The legacy ledger defers this issue: Decide whether the Tenants UI BFF's six canonical reads should move from direct HTTPS to DAPR service invocation. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md` summary: Decide whether the Tenants UI BFF's six canonical reads should move from direct HTTPS to DAPR service invocation. Raised by the owner during the 1.10 review: DAPR is the intended discovery mechanism for services with sidecars. Not actionable inside 1.10 — it is a topology and security-posture change, not a base-address swap. evidence: | Current topology (verified 2026-07-30): - `tenants-api` HAS a DAPR sidecar, `AppId = "tenants-api"` (src/Hexalith.Tenants.AppHost/Program.cs:104-106). - `tenants-ui` has NO sidecar, no app-id, and no Dapr package references anywhere in src/Hexalith.Tenants.UI. It is not a DAPR app; it is a Blazor front end / BFF. - `deploy/dapr/accesscontrol.tenants.yaml` is `defaultAction: deny` and allows exactly one caller, `appId: eventstore`, on five POST operations (/process, /project, /query, /replay-state, /admin/operational-index-metadata). None of the six `GET /api/tenants*` read routes are allowed and `tenants-ui` has no policy entry. - Because the reads go direct HTTPS to `tenants-api` (which is `.WithExternalHttpEndpoints()`), they bypass the DAPR access-control plane and mTLS entirely. That is a deviation from the documented deny-by-default posture and was not recorded anywhere in Story 1.10. What a move to DAPR invoke would require: 1. A sidecar + app-id for `tenants-ui`. 2. A `tenants-ui` policy in accesscontrol.tenants.yaml allowing GET on the six read routes, plus the route tests project-context.md requires to change alongside any app-id/topic change. 3. Base address becomes `http://localhost:{daprHttpPort}/v1.0/invoke/tenants-api/method/` + route. Route identity at the API is preserved so the six-path acceptance criterion survives, but the client's URI building, base-path retention and scheme gate all assume a direct service address. 4. Reconciling `deploy/dapr/resiliency.yaml`, which applies `defaultRetry` (constant, 3 retries), a 5s `daprSidecar` timeout and a circuit breaker to invoke targets, with Story 1.10's deliberate transport semantics: the hand-built linked deadline, the fixed support-safe failure categories, and the explicit never-silently-retry invariant (notably the invalid-cursor rule). A retried conditional GET re-sends `If-None-Match`, so 304/ETag behaviour through the sidecar must be verified, not assumed. Not verified, flagged for that work: how `%2E%2E` behaves through a DAPR invoke path. If anything it is worse than direct HTTP — a resolved `..` could traverse out of the `/v1.0/invoke/{appId}/method/` prefix — so the reject-all-dot route-value patch is required regardless of the transport chosen. status: open — owner-raised architectural decision for its own story. Story 1.10 proceeds with the resolved option (c): no discovery mechanism, consuming the AppHost-injected resolved endpoint URL, matching the EventStore command/status and Memories clients.
status: open
decision: 2026-08-26 Use Dapr invocation — Add a tenants-ui sidecar and app ID, deny-by-default GET policy, resilience reconciliation, and route and ETag tests.
decision: 2026-08-25 Use Dapr invocation — Add a tenants-ui sidecar and app-id, deny-by-default GET policy, resilience reconciliation, and route and ETag tests.

### DW-145: Deferred to Story 1.11
origin: migrated from legacy ledger ("Architectural decision recorded by code review of spec-1-10 (2026-07-30) — BFF read transport vs DAPR service invocation"), 2026-08-25
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:21-27; TenantDetailPage.razor:149
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** (owner decision, 1.10 chunk-A+B review 2026-07-30) — `LifecycleAuthorizationReflection` resolves the principal from `IHttpContextAccessor`, which is null for the whole interactive circuit, so `Evaluate(null)` returns `Indeterminate` permanently and `TenantDetailPage.razor:149` gates tenant lifecycle actions off for a signed-in global administrator for the rest of the session. Story 1.10 added `ResolveGlobalAdministratorsAuthorizationAsync` to the same type and migrated the workspace and global-administrators pages to circuit-aware resolution, leaving the tenant-detail consumer on the synchronous path. Reason for deferral: 1.11 already owns two open principal-resolution decisions on the same evaluator, so all three are settled together rather than by two stories patching it independently. [src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:21-27] **CLOSED (2026-08-08, Story 1.11):** tenant detail consumes `ResolveLifecycleAuthorizationAsync`; synchronous HttpContext-only reflection no longer gates lifecycle actions.
status: done 2026-08-08
resolution: Legacy completion record: - **Deferred to Story 1.11** (owner decision, 1.10 chunk-A+B review 2026-07-30) — `LifecycleAuthorizationReflection` resolves the principal from `IHttpContextAccessor`, which is null for the whole interactive circuit, so `Evaluate(null)` returns `Indeterminate` permanently and `TenantDetailPage.razor:149` gates tenant lifecycle actions off for a signed-in global administrator for the rest of the session. Story 1.10 added `ResolveGlobalAdministratorsAuthorizationAsync` to the same type and migrated the workspace and global-administrators pages to circuit-aware resolution, leaving the tenant-detail consumer on the synchronous path. Reason for deferral: 1.11 already owns two open principal-resolution decisions on the same evaluator, so all three are settled together rather than by two stories patching it independently. [src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:21-27] **CLOSED (2026-08-08, Story 1.11):** tenant detail consumes `ResolveLifecycleAuthorizationAsync`; synchronous HttpContext-only reflection no longer gates lifecycle actions.

### DW-146: Member evidence gate collapses lifecycle and permission reasons into "stale data"
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-30)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:496-499
reason: The legacy ledger defers this issue: Member evidence gate collapses lifecycle and permission reasons into "stale data". Original context is preserved in legacy-detail.
legacy-detail: - **Member evidence gate collapses lifecycle and permission reasons into "stale data"** — `MemberAccessReview.ResolveFailClosedReasons` gained a `!ActionsAreEvidenceBacked -> [UnavailableReason.StaleData]` arm inserted above the pre-existing `Detail.Status is Disabled or Unknown -> MissingLifecycleSupport` and `role is TenantRole.Unknown -> MissingPermission` arms. Because `ActionsAreEvidenceBacked` requires detail `Ready` + `Current` + `Current`, members `Ready|Empty` + `Current` + `Current`, and equal non-blank projection versions, a disabled tenant or an unknown-role member reports "stale data" whenever any clause is short — including the common `Unknown` freshness case. `PrimaryUnavailableReason` feeds the same value into the authorization-safe empty message, so that copy loses its permission wording too. Reason for deferral: defensible as written. Without current, version-consistent evidence the code genuinely cannot assert a lifecycle or permission conclusion, so failing to the weakest claim is the fail-closed reading. Recorded as a design choice, not a defect. Revisit if: operators report the reason as unhelpful, or AC6's distinctness requirement is ever extended from surface kinds to the action-unavailable reason enum. [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:496-499]
status: done 2026-09-02
decision: 2026-09-02 Retain weakest claim — Keep the current fail-closed reason until the recorded operator-feedback or acceptance-criteria trigger occurs.
resolution: closed by human decision: Keep the current fail-closed reason until the recorded operator-feedback or acceptance-criteria trigger occurs.
decision: 2026-09-02 Retain weakest claim — Keep the current fail-closed reason until the recorded operator-feedback or acceptance-criteria trigger occurs.

### DW-147: Mobile read-only for platform-authority mutations is enforced only by CSS
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-30)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css:200-216
reason: The legacy ledger defers this issue: Mobile read-only for platform-authority mutations is enforced only by CSS. Original context is preserved in legacy-detail.
legacy-detail: - **Mobile read-only for platform-authority mutations is enforced only by CSS** — `@media (max-width: 42rem)` sets `display: none` on `.global-admins__mutation-initiation` (both the descendant and `::deep` selectors), and the paired `FluentMessageBar` states "Grant and remove controls require a wider viewport." The `EditForm … OnSubmit="SubmitGrantAsync"`, the grant submit button and the per-row Remove `FluentButton` remain rendered and wired over the circuit; `SubmitGrantAsync`, `PreviewRemove` and `SubmitRemoveAsync` contain no viewport check, and a hidden element still dispatches events in Blazor Server. Reason for deferral: viewport is an affordance, not an authorization boundary. The server API plus the existing authorization, read-surface, freshness and completeness gates remain the real enforcement, so this is a copy-accuracy point rather than a security defect. Revisit if: the notice is ever restated as a safety guarantee, or a viewport-scoped capability becomes part of the authorization model. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css:200-216]
status: done 2026-08-28
resolution: already resolved: commit 03566fb1; src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorActionAvailabilityEvaluator.cs:84-87 and GlobalAdministratorsPage.razor:2153-2159,2410-2418 now re-evaluate runtime viewport safety before submission.

### DW-148: Deferred to Story 1.11
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-30)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1279; TenantsBffComposition.cs:30-32
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** (owner decision, 1.10 chunk-C+D+E review 2026-07-30) — `GlobalAdministratorsPage.ApplyAuthenticationStateChangedAsync:1279` re-authorizes by calling `TenantsGlobalAdministratorClaims.Evaluate(authenticationState.User)` directly, while every other path on the page (`OnInitializedAsync`, `ReauthorizeAsync`, therefore every load, `SubmitGrantAsync`, `SubmitRemoveAsync`, both status refreshes) goes through `BffComposition.ResolveGlobalAdministratorsAuthorizationAsync()`, which consults the claims property **only** when no `ITenantConfigurationPrincipalResolver` is registered (`TenantsBffComposition.cs:30-32`). With a resolver registered the two evaluators can disagree, so a principal the resolver would classify `Indeterminate`/`MissingPermission` becomes `Authorized` after any `AuthenticationStateChanged` notification and unlocks the grant/remove surfaces for the rest of that circuit, subject only to the freshness gates. `ReauthorizeAsync()` exists at `:1352-1363`, so the consistent fix is one line. Reason for deferral: the one-line fix's correctness depends entirely on how 1.11 resolves circuit-vs-`HttpContext` precedence. The chunk A+B review established that `LifecycleAuthorizationReflection` on this same type returns `Indeterminate` permanently on an interactive circuit because `HttpContext` is null; if `TenantConfigurationPrincipalResolver` shares that weakness, routing this path through it trades a fail-open for a fail-shut. Story 1.11 already owns both open principal-resolution decisions on this evaluator, and the structurally identical `TenantDetailPage` item was folded there on 2026-07-30 for the same stated reason — avoid two stories making conflicting fixes to the same evaluator. Accepted consequence until 1.11 lands: the fail-open divergence above ships in 1.10. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1279] **CLOSED (2026-08-08, Story 1.11):** transition path aligned to the strict BFF resolver; see applied auth-transition patches.
status: done 2026-08-08
resolution: Legacy completion record: - **Deferred to Story 1.11** (owner decision, 1.10 chunk-C+D+E review 2026-07-30) — `GlobalAdministratorsPage.ApplyAuthenticationStateChangedAsync:1279` re-authorizes by calling `TenantsGlobalAdministratorClaims.Evaluate(authenticationState.User)` directly, while every other path on the page (`OnInitializedAsync`, `ReauthorizeAsync`, therefore every load, `SubmitGrantAsync`, `SubmitRemoveAsync`, both status refreshes) goes through `BffComposition.ResolveGlobalAdministratorsAuthorizationAsync()`, which consults the claims property **only** when no `ITenantConfigurationPrincipalResolver` is registered (`TenantsBffComposition.cs:30-32`). With a resolver registered the two evaluators can disagree, so a principal the resolver would classify `Indeterminate`/`MissingPermission` becomes `Authorized` after any `AuthenticationStateChanged` notification and unlocks the grant/remove surfaces for the rest of that circuit, subject only to the freshness gates. `ReauthorizeAsync()` exists at `:1352-1363`, so the consistent fix is one line. Reason for deferral: the one-line fix's correctness depends entirely on how 1.11 resolves circuit-vs-`HttpContext` precedence. The chunk A+B review established that `LifecycleAuthorizationReflection` on this same type returns `Indeterminate` permanently on an interactive circuit because `HttpContext` is null; if `TenantConfigurationPrincipalResolver` shares that weakness, routing this path through it trades a fail-open for a fail-shut. Story 1.11 already owns both open principal-resolution decisions on this evaluator, and the structurally identical `TenantDetailPage` item was folded there on 2026-07-30 for the same stated reason — avoid two stories making conflicting fixes to the same evaluator. Accepted consequence until 1.11 lands: the fail-open divergence above ships in 1.10. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1279] **CLOSED (2026-08-08, Story 1.11):** transition path aligned to the strict BFF resolver; see applied auth-transition patches.

### DW-149: Read-refresh lease retry is unverified on the tenant detail page
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-31)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:394-400
reason: The legacy ledger defers this issue: Read-refresh lease retry is unverified on the tenant detail page. Original context is preserved in legacy-detail.
legacy-detail: - **Read-refresh lease retry is unverified on the tenant detail page** — every detail-page test stubs `IProjectionSubscription.SubscribeAsync` to a successful subscription, so `lease.IsSubscribed` is always true. The `if (!lease.IsSubscribed) return;` early return and the `OnAfterRenderAsync` retry that exists to recover a superseded or failed setup are both unexecuted; recording the empty lease anyway, or deleting the `OnAfterRenderAsync` override outright, survives the suite. `TenantReadRefreshSubscriptionTests` proves a failed setup returns a non-subscribed lease rather than throwing, and `GlobalAdministratorsPageTests` is exactly the retry test the detail page lacks. Reason for deferral: the shared read-refresh lease pattern is not a Story 1.6 surface — the same gap applies to every page that binds a lease, and the sibling page already carries the canonical test to copy. Revisit if: a lease-setup failure is ever observed in a running circuit, or the read-refresh pattern is consolidated. [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:394-400,441-446]
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:804-888 and TenantDetailSurfaceTests.cs:7438-7501 implement three bounded setup attempts and a fresh route budget.

### DW-150: An in-flight `RefreshTenantReadsAsync` is aborted silently by a concurrent detail refresh
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-31)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:470-482
reason: The legacy ledger defers this issue: An in-flight `RefreshTenantReadsAsync` is aborted silently by a concurrent detail refresh. Original context is preserved in legacy-detail.
legacy-detail: - **An in-flight `RefreshTenantReadsAsync` is aborted silently by a concurrent detail refresh** — the documented guard at `:478-482` reroutes only when `_memberPageLoadInFlight` is set, and the read-refresh path never sets it. A projection notification starts `RefreshTenantReadsAsync` (member snapshot → `Refreshing`); the operator then triggers a detail refresh; `BeginLoad()` cancels the shared token and clears `IsRefreshing`. The member read is dropped, the refresh indicator vanishes, the pager re-enables and the table sits on stale rows with no error and no retry — the same failure the comment above the guard says it closed, reached through the other entry point. No test triggers a refresh while a member read is outstanding. Reason for deferral: the member-paging surface is owned outside Story 1.6, and the correct fix (widening the reroute condition to any in-flight member read) changes behaviour the member story's tests pin. Revisit if: the member table is reported showing stale rows after a refresh, or the member-paging story is reopened. [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:470-482]
status: open

### DW-151: Undeclared `references/Hexalith.EventStore` gitlink bump in the working tree
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-31)"), 2026-08-25
location: references/Hexalith.EventStore; scripts/validate-story-gitlinks.py
reason: The legacy ledger defers this issue: Undeclared `references/Hexalith.EventStore` gitlink bump in the working tree. Original context is preserved in legacy-detail.
legacy-detail: - **Undeclared `references/Hexalith.EventStore` gitlink bump in the working tree** — `a40ab8a` → `e4618d9` (v3.86.0), uncommitted and named in no story File List. `scripts/validate-story-gitlinks.py` also exits 1 for Story 1.6, but every UNDECLARED pointer it reports was moved by a Story 1.9 / Epic 2 commit after this story's stale baseline; no Story 1.6 commit after `ec7ec8c` moves a gitlink, and `ec7ec8c`'s EventStore bump is declared. Separately worth noting: nine of those later bumps rode along inside `feat:`/`fix:`/`test:`/ `refactor:` commits rather than dedicated `build(deps)` commits — the exact pattern the guard was created for, now recurring under other stories' names. Reason for deferral: not Story 1.6's change. Belongs to whoever is holding the working-tree bump, as either a separate `build(deps)` commit or a revert. Revisit if: the bump is committed without declaration, or the ride-along pattern recurs a fourth time. [references/Hexalith.EventStore]
status: done 2026-08-25
resolution: already resolved: commit 10db1cee moved the EventStore pointer to 67c645ab02b21ffcb7bef9530e524e4510e36d27; the current lowercase submodule status is unrelated worktree dirt.

### DW-152: Composition availability-pair guard is logically asymmetric
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:60; Program.cs
reason: The legacy ledger defers this issue: Composition availability-pair guard is logically asymmetric. Original context is preserved in legacy-detail.
legacy-detail: - **Composition availability-pair guard is logically asymmetric** — `gatewayIsUnavailable` compares `ServiceDescriptor.ImplementationType` against `UnavailableTenantQueryGateway`, which is `internal` and is null for factory- or instance-registered services. A host declaring a truthful `IsConnected: false` alongside any other gateway is therefore rejected with the inverted message "declares IsConnected: false while the registered ITenantQueryGateway is a connected implementation", while the mismatched pairing the guard exists to catch — `UnavailableTenantQueryGateway` registered via a factory with `IsConnected: true` — passes. The check is also skipped entirely unless availability is registered as an instance. Reason for deferral: unreachable in practice. `Hexalith.Tenants.UI` ships as a container application, not a NuGet package; the only production caller is its own `Program.cs`, which pre-registers nothing; and the sole assemblies that can name the internal type are the two test projects, which use the instance form. Revisit if: `Hexalith.Tenants.UI` is ever published as a package, or a second host composes the module. [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:60]
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:57-87 handles type, instance, and unknowable factory registrations with tri-state matching.

### DW-153: Member mutation flows are outside the projection-lifecycle policy
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:223
reason: The legacy ledger defers this issue: Member mutation flows are outside the projection-lifecycle policy. Original context is preserved in legacy-detail.
legacy-detail: - **Member mutation flows are outside the projection-lifecycle policy** — `ChangeTenantMemberRoleFlow`, `RemoveTenantMemberFlow`, `AddTenantMemberFlow` and `CreateTenantFlow` have no `Lifecycle` parameter and gate on freshness and surface kind only, while `33abe27` added `Lifecycle is not Current` gates to the four configuration and metadata flows. With a rebuilding projection, editing tenant metadata is blocked but removing a member — the higher-consequence, harder-to-reverse action — is not. Reason for deferral: consequence of the open lifecycle-gate decision recorded in the story's loop-8 review findings, not an independent defect. Resolving that decision determines whether these flows should be brought into the policy or the policy narrowed. Revisit if: the lifecycle-gate decision resolves toward keeping the strict gate. [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:223]
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:511-522 requires current detail/member lifecycle and freshness evidence with matching nonblank projection versions.

### DW-154: Two global-administrator teardown paths are knowingly unverified
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:997
reason: The legacy ledger defers this issue: Two global-administrator teardown paths are knowingly unverified. Original context is preserved in legacy-detail.
legacy-detail: - **Two global-administrator teardown paths are knowingly unverified** — un-marshalling the `ResetPagingAsync` cursor-history clear off the dispatcher, and neutering the `ObjectDisposedException` catch filter on the notification-refresh teardown, both survived the full UI suite. Reason for deferral: bUnit's single-threaded renderer cannot reproduce either race, so these are untestable at the current harness level rather than merely uncovered. Revisit if: a concurrency-capable component harness lands, or either path produces a live defect. Until then the code comments should say "unverified" rather than implying coverage. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:997]
status: open

### DW-155: Paging guards widened to `internal` for direct test access
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:585
reason: The legacy ledger defers this issue: Paging guards widened to `internal` for direct test access. Original context is preserved in legacy-detail.
legacy-detail: - **Paging guards widened to `internal` for direct test access** — `MemberAccessReview`'s paging guards were promoted from private to internal so the test project could invoke them directly; the accompanying comment concedes every guard could be deleted with the suite still green. Reason for deferral: pre-existing test-design debt, not introduced behaviour. The guards remain unobservable through the rendered affordance, so the coverage they now have does not prove the control behaves correctly — but narrowing them again without a rendered-affordance test would lose coverage. Revisit if: the member pager gains bUnit tests that drive it through its rendered controls. [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:585]
status: open

### DW-156: Command `SafeMessage` values are hardcoded English literals rather than `TenantsResources.resx` entries
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRemoveCommandSnapshot.cs:189; TenantsResources.resx
reason: The legacy ledger defers this issue: Command `SafeMessage` values are hardcoded English literals rather than `TenantsResources.resx` entries. Original context is preserved in legacy-detail.
legacy-detail: - Command `SafeMessage` values are hardcoded English literals rather than `TenantsResources.resx` entries. The new page-scoped global-administrator removal message is a hardcoded literal, but so is the pre-existing "Current complete projection evidence is required…" arm it branches against, and the same shape recurs across the command snapshot types. Reason for deferral: pre-existing pattern, not introduced by this story. Converting one arm in isolation would leave the file internally inconsistent and split one message pair across two mechanisms. Revisit if: the command snapshots get a localization pass, or EN/FR parity is enforced by a governance test that reaches C# literals rather than only `.resx` keys. [src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRemoveCommandSnapshot.cs:189]
status: done 2026-08-27
resolution: already resolved: commit 7d865ce8; src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRemoveCommandSnapshot.cs:219-233 uses localizable resource keys instead of raw English SafeMessage literals.

### DW-157: `RestQueryClientAdapter` carries 13 lines of dead freshness computation that re-implement `TenantsRestQueryClient.ResolveFreshness`, discard the result, and omit the `IsDegraded == true` collapse both production implementations perform — so it will drift silently while reading as if it models the client
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:6044
reason: The legacy ledger defers this issue: `RestQueryClientAdapter` carries 13 lines of dead freshness computation that re-implement `TenantsRestQueryClient.ResolveFreshness`, discard the result, and omit the `IsDegraded == true` collapse both production implementations perform — so it will drift silently while reading as if it models the client. Original context is preserved in legacy-detail.
legacy-detail: - `RestQueryClientAdapter` carries 13 lines of dead freshness computation that re-implement `TenantsRestQueryClient.ResolveFreshness`, discard the result, and omit the `IsDegraded == true` collapse both production implementations perform — so it will drift silently while reading as if it models the client. Reason for deferral: subsumed by the open decision on the gateway test harness. Whether to delete the block or delete the whole adapter depends on which option that decision takes. Revisit if: the harness decision resolves. [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:6044]
status: done 2026-08-25
resolution: already resolved: commit 845a15e4; tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:7348-7415 confirms the dead adapter freshness ladder was removed.

### DW-158: `WaitForAsync` reports a slow agent as a raw `TaskCanceledException` from `Task.Delay` rather than as a named unmet condition, so a genuinely flaky subscription test reports as an infrastructure error
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs:317
reason: The legacy ledger defers this issue: `WaitForAsync` reports a slow agent as a raw `TaskCanceledException` from `Task.Delay` rather than as a named unmet condition, so a genuinely flaky subscription test reports as an infrastructure error. Original context is preserved in legacy-detail.
legacy-detail: - `WaitForAsync` reports a slow agent as a raw `TaskCanceledException` from `Task.Delay` rather than as a named unmet condition, so a genuinely flaky subscription test reports as an infrastructure error. Reason for deferral: diagnostics-only. No production behaviour is left unverified by it. Revisit if: the subscription tests start failing intermittently in CI and the cause needs to be readable from the failure message alone. [tests/Hexalith.Tenants.UI.Tests/Services/TenantReadRefreshSubscriptionTests.cs:317]
status: open

### DW-159: `TenantConfigurationView.StateResourcePrefix` has no arm for `TenantDetailSurfaceKind.NotFound` or `Unauthorized`, so both fall through to `Tenants.Configuration.State.Ready` ("Configuration evidence is current") and are announced assertively, because `!CanInspect` puts `LivePoliteness` in the escalated set, over a surface that has no rows
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31, loop 10 never-reviewed delta)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor:220
reason: The legacy ledger defers this issue: `TenantConfigurationView.StateResourcePrefix` has no arm for `TenantDetailSurfaceKind.NotFound` or `Unauthorized`, so both fall through to `Tenants.Configuration.State.Ready` ("Configuration evidence is current") and are announced assertively, because `!CanInspect` puts `LivePoliteness` in the escalated set, over a surface that has no rows. Original context is preserved in legacy-detail.
legacy-detail: - `TenantConfigurationView.StateResourcePrefix` has no arm for `TenantDetailSurfaceKind.NotFound` or `Unauthorized`, so both fall through to `Tenants.Configuration.State.Ready` ("Configuration evidence is current") and are announced **assertively**, because `!CanInspect` puts `LivePoliteness` in the escalated set, over a surface that has no rows. Reason for deferral: pre-existing arms, not introduced by this range, and unreachable through the only current consumer — `TenantDetailSnapshot.NotFound`/`Unauthorized` route through `Empty(...)`, which yields an unavailable safe model, so the Unavailable arm wins before the fall-through is reached. Revisit if: the second consumer the file's own comment anticipates arrives, or any caller passes those surface kinds with an available configuration model. [src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor:220]
status: open

### DW-160: Replace `CapturingGatewayClient` + `RestQueryClientAdapter` in `TenantQueryGatewayTests` with a substitute
origin: migrated from legacy ledger ("Deferred from: review repair loop 11 of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:6093
reason: The legacy ledger defers this issue: Replace `CapturingGatewayClient` + `RestQueryClientAdapter` in `TenantQueryGatewayTests` with a substitute. Original context is preserved in legacy-detail.
legacy-detail: - Replace `CapturingGatewayClient` + `RestQueryClientAdapter` in `TenantQueryGatewayTests` with a substitute of the real `ITenantsRestQueryClient`. The adapter still re-implements the generic `SubmitQueryRequest` transport Story 1.10 deleted, so failures can only be injected as `EventStoreGatewayException` — a type the real client never throws — and the roughly sixty tests it drives exercise only the success arm of `ToEventStoreResult`. Reason for deferral: this is the reason review loop 10 itself gave when it reopened the item. Replacing the harness rewrites the fixture of about sixty tests in one change, which deserves its own pass and its own review rather than riding along with unrelated repairs. The *misleading* half is already closed: the 23 inert `Request.*` assertions and the adapter's dead freshness ladder are gone, and every failure-kind mapping repaired in loops 9–11 was driven through the production seam with `FixedFailureRestQueryClient` or an `ITenantsRestQueryClient` substitute. What remains is structural test debt, not a false claim. Revisit if: a further failure-mapping change is needed at the gateway seam, or the adapter drifts from `ResolveFreshness` again. [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:6093]
status: open

### DW-161: Live socket-level proof that all six direct Tenants REST routes answer through the deployed topology
origin: migrated from legacy ledger ("Deferred from: review repair loop 11 of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs:421
reason: The legacy ledger defers this issue: Live socket-level proof that all six direct Tenants REST routes answer through the deployed topology. Original context is preserved in legacy-detail.
legacy-detail: - Live socket-level proof that all six direct Tenants REST routes answer through the deployed topology. Reason for deferral: recorded as an owned limitation under decision `spec:854`, option (b). The routes are proven in process against the real generated controllers — paths, query strings, metadata headers and the conditional `304` path — by `TenantsApiGeneratedControllerTests.Direct_rest_client_routes_match_the_generated_controllers_and_parse_their_real_headers`. A live probe driving the production `TenantsRestQueryClient` against the `tenants-api` Aspire resource was written for loop 11 and every read times out at the client's 60 s bound in the local slim-mode topology, which also intermittently fails the pre-existing command-status wait in `AspireTopologyTests`. A lane that cannot separate "the routes are broken" from "the topology is unhealthy" is not evidence, so it was not shipped. Revisit if: a reliable Aspire topology lane exists (CI or local) that can serve as an oracle for `tenants-api`. [tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs:421]
status: open

### DW-162: `scripts/validate-story-gitlinks.py` keeps no automated test and no CI wiring
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-08-01)"), 2026-08-25
location: scripts/validate-story-gitlinks.py; scripts/validate-story-gitlinks.py:264
reason: The legacy ledger defers this issue: `scripts/validate-story-gitlinks.py` keeps no automated test and no CI wiring. Original context is preserved in legacy-detail.
legacy-detail: - `scripts/validate-story-gitlinks.py` keeps no automated test and no CI wiring. Reason for deferral: owner decision D-K, option (c). Manual verification is accepted; introducing Python test infrastructure to a .NET repository is not warranted for this guard, and porting the check to C# would duplicate the script rather than test it. The evidence stands and is recorded so it is not mistaken for coverage: review loop 12 mutation-verified that replacing `stated = stated_targets(story_text)` with `stated = {}` turns a spec whose stated target SHA was corrupted to `deadbee` from FAIL/exit 1 into PASS/exit 0, while the real spec still exits 0 — so the normal workflow invocation stays green and nothing notices. The repository has no Python test infrastructure (no `conftest.py`, `pytest.ini` or `test_*.py`) and `grep -rn validate-story-gitlinks .github/` returns nothing. The script is release-gating per `project-context.md:149`, so its correctness currently rests on the operator running it and reading the output. Revisit if: the repository gains a Python test lane for any other reason, or a story ships an undeclared `references/` gitlink despite the guard. [scripts/validate-story-gitlinks.py:264]
status: done 2026-08-25
resolution: closed by human decision: Retain owner decision D-K(c) that manual mutation verification is sufficient.
decision: 2026-08-25 Accept manual evidence — Retain owner decision D-K(c) that manual mutation verification is sufficient.

### DW-163: A tenant configuration key written by a producer other than this UI, carrying an invisible separator or other untypeable character, remains permanently unremovable through the UI
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-08-01)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor:495; SetTenantConfigurationFlow.razor:430-448
reason: The legacy ledger defers this issue: A tenant configuration key written by a producer other than this UI, carrying an invisible separator or other untypeable character, remains permanently unremovable through the UI. Original context is preserved in legacy-detail.
legacy-detail: - A tenant configuration key written by a producer other than this UI, carrying an invisible separator or other untypeable character, remains permanently unremovable through the UI. Reason for deferral: owner decision D-J, option (a). `ContainsUntypeableCharacter` (`SetTenantConfigurationFlow.razor:430-448`) bounds the only producer this story owns, which is accepted as the guard's scope. Configuration keys are consumer-owned (`project-context.md:74`) and writable through `POST /api/v1/commands`; such a key renders identically to its clean twin and can never satisfy `RemoveTenantConfigurationFlow.razor:495`'s ordinal match against typed confirmation text, which offers no alternative affordance. The guard's comment is being corrected to state this scope rather than claim the exposure is closed. Revisit if: a compensating-command path for removing such a key is needed in support, or the remove flow gains a non-typed confirmation affordance. [src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor:495]
status: done 2026-08-25
resolution: closed by human decision: Retain owner decision D-J(a) that UI removability covers only UI-produced keys.
decision: 2026-08-25 Accept producer boundary — Retain owner decision D-J(a) that UI removability covers only UI-produced keys.

### DW-164: A route change while the prior tenant's refresh subscription is pending can leave the new tenant without projection auto-refresh
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-01)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:447
reason: The legacy ledger defers this issue: A route change while the prior tenant's refresh subscription is pending can leave the new tenant without projection auto-refresh. Original context is preserved in legacy-detail.
legacy-detail: - A route change while the prior tenant's refresh subscription is pending can leave the new tenant without projection auto-refresh. `EnsureReadRefreshLeaseAsync` rejects the new tenant while the old subscription owns `_readRefreshSubscriptionInFlight`; when the old attempt later disposes its lease and clears the flag, it does not schedule a render or retry for the current tenant. Reason for deferral: the race is in shared tenant-detail notification work that is outside Story 1.11's attributed implementation; this chunk included the file only to review the transferred lifecycle authorization consumer. Revisit if: the tenant-detail notification lifecycle is reviewed or the shared subscription retry logic is changed. [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:447]
status: open

### DW-165: `TenantAuditPage` is the last production consumer of the synchronous `HttpContext`-only
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review, loop 2 (2026-08-01)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1111; TenantAuditPage.razor:1009
reason: The legacy ledger defers this issue: `TenantAuditPage` is the last production consumer of the synchronous `HttpContext`-only. Original context is preserved in legacy-detail.
legacy-detail: - `TenantAuditPage` is the last production consumer of the synchronous `HttpContext`-only `GlobalAdministratorsAuthorizationReflection`. On an established interactive circuit `HttpContext` is null, so `Evaluate(null)` returns `Indeterminate` and the global-administrator correction affordances at `TenantAuditPage.razor:1009` and `:1017` are permanently unavailable. This is the same defect class the story's transferred decision 3 records as resolved for `TenantDetailPage`. Reason for deferral: the file is not in Story 1.11's File List, and the correct fix depends on how the circuit-only principal-resolution decision is settled — migrating to the async seam alone would not help while that seam also returns `Indeterminate` outside an inbound circuit activity. Revisit if: the resolver decision lands, or Epic 5 audit work reopens the correction path. [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1111]
status: open

### DW-166: `EnsureReadRefreshLeaseAsync` calls `SubscribeAsync` with `CancellationToken.None` and no timeout
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review, loop 2 (2026-08-01)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1120
reason: The legacy ledger defers this issue: `EnsureReadRefreshLeaseAsync` calls `SubscribeAsync` with `CancellationToken.None` and no timeout. Original context is preserved in legacy-detail.
legacy-detail: - `EnsureReadRefreshLeaseAsync` calls `SubscribeAsync` with `CancellationToken.None` and no timeout. If the subscription backend never answers, `_readRefreshSubscriptionInFlight` stays true and every later render, refresh-budget reset and re-authorization retry is rejected for the rest of the circuit. The bounded-budget design assumes attempts terminate; nothing enforces that. Reason for deferral: needs a timeout policy decision (value, and whether a timed-out attempt charges the budget) rather than a mechanical fix. Revisit if: notification setup is reworked, or a hung-subscribe incident is observed. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1120]
status: open
decision: 2026-08-26 Timeout charges budget — Apply a bounded timeout and count each timeout against the three-attempt recovery budget.
decision: 2026-08-25 Timeout charges budget — Apply a bounded timeout and count each timeout against the three-attempt recovery budget.

### DW-167: The grant and remove submit buttons are never disabled while a mutation is in flight
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review, loop 2 (2026-08-01)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:777
reason: The legacy ledger defers this issue: The grant and remove submit buttons are never disabled while a mutation is in flight. Original context is preserved in legacy-detail.
legacy-detail: - ~~The grant and remove submit buttons are never disabled while a mutation is in flight.~~ **WITHDRAWN by code review loop 3 (2026-08-01): the premise is false and was never true.** `IsGrantSubmitDisabled` is `!string.IsNullOrWhiteSpace(GrantUnavailableReason)`, and `GrantUnavailableReason` returns `Tenants.GlobalAdministrators.Grant.Unavailable.InFlight` whenever `IsGrantInFlight` — which is `_isGrantSubmitting || State is RequestSent or Accepted or ProjectionPending`. `IsRemoveSubmitDisabled` names `IsGrantInFlight || IsRemoveInFlight` outright. Both bindings therefore do depend on in-flight state. The real exposure was narrower and is already fixed: hoisting `ReauthorizeAsync` to be the submit handlers' first await consumed the render that would have shown the in-flight state, so the disabled attribute never reached the DOM. The marshalled `await InvokeAsync(StateHasChanged)` after the `RequestSent` write closes it. Left in the ledger as a withdrawal rather than deleted, because a future sweep reading the original entry would re-derive a defect that does not exist. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:777]
status: done 2026-08-01
resolution: Legacy completion record: - ~~The grant and remove submit buttons are never disabled while a mutation is in flight.~~ **WITHDRAWN by code review loop 3 (2026-08-01): the premise is false and was never true.** `IsGrantSubmitDisabled` is `!string.IsNullOrWhiteSpace(GrantUnavailableReason)`, and `GrantUnavailableReason` returns `Tenants.GlobalAdministrators.Grant.Unavailable.InFlight` whenever `IsGrantInFlight` — which is `_isGrantSubmitting || State is RequestSent or Accepted or ProjectionPending`. `IsRemoveSubmitDisabled` names `IsGrantInFlight || IsRemoveInFlight` outright. Both bindings therefore do depend on in-flight state. The real exposure was narrower and is already fixed: hoisting `ReauthorizeAsync` to be the submit handlers' first await consumed the render that would have shown the in-flight state, so the disabled attribute never reached the DOM. The marshalled `await InvokeAsync(StateHasChanged)` after the `RequestSent` write closes it. Left in the ledger as a withdrawal rather than deleted, because a future sweep reading the original entry would re-derive a defect that does not exist. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:777]

### DW-168: `TenantDetailPage.IsSafeReturnUrl` accepts any string with a `/tenants` prefix — including
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review, loop 2 (2026-08-01)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1173
reason: The legacy ledger defers this issue: `TenantDetailPage.IsSafeReturnUrl` accepts any string with a `/tenants` prefix — including. Original context is preserved in legacy-detail.
legacy-detail: - `TenantDetailPage.IsSafeReturnUrl` accepts any string with a `/tenants` prefix — including `/tenants-anything`, embedded control characters, and unbounded length — while the sibling `GlobalAdministratorsPage.NormalizeReturnUrl` rejects control characters, `\`, `#`, `//`, non-allow-listed query keys and repeated values, and requires an exact canonical round-trip. Reason for deferral: every admitted value stays a same-origin relative path, so there is no redirect or external-return gap today; this is convergence hardening, not a defect. Revisit if: the prefix check is relaxed, or a third return-URL validator appears. [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1173]
status: open

### DW-169: On narrow viewports the per-row Remove launcher is hidden by CSS with no per-row localized reason; the actions
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review, loop 2 (2026-08-01)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:466
reason: The legacy ledger defers this issue: On narrow viewports the per-row Remove launcher is hidden by CSS with no per-row localized reason; the actions. Original context is preserved in legacy-detail.
legacy-detail: - On narrow viewports the per-row Remove launcher is hidden by CSS with no per-row localized reason; the actions cell renders nothing where the control was, and the grant cell simultaneously renders an "available" string while its controls are hidden. Only a single page-level notice explains the read-only mode. Reason for deferral: AC5 requires the actions be visibly unavailable with a localized reason, and the page-level reason satisfies that in substance; per-row parity is polish. Revisit if: the mobile read-only surface is revisited, or accessibility review flags the actions cell. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:466] **CLOSED (2026-08-08, loop 5 APPLIED):** mobile/unavailable reason spans and mutation-initiation gating closed AC5; no longer open polish debt for Story 1.11.
status: done 2026-08-08
resolution: Legacy completion record: - On narrow viewports the per-row Remove launcher is hidden by CSS with no per-row localized reason; the actions cell renders nothing where the control was, and the grant cell simultaneously renders an "available" string while its controls are hidden. Only a single page-level notice explains the read-only mode. Reason for deferral: AC5 requires the actions be visibly unavailable with a localized reason, and the page-level reason satisfies that in substance; per-row parity is polish. Revisit if: the mobile read-only surface is revisited, or accessibility review flags the actions cell. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:466] **CLOSED (2026-08-08, loop 5 APPLIED):** mobile/unavailable reason spans and mutation-initiation gating closed AC5; no longer open polish debt for Story 1.11.

### DW-170: Multi-page populations can permanently land grant/remove confirmation in page-scoped `UnableToVerify`
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-08, loop 5 chunk 2)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:178
reason: The legacy ledger defers this issue: Multi-page populations can permanently land grant/remove confirmation in page-scoped `UnableToVerify`. Original context is preserved in legacy-detail.
legacy-detail: - Multi-page populations can permanently land grant/remove confirmation in page-scoped `UnableToVerify` because requery always loads page one. Page-scoped SafeMessages document the honesty limit; adding search-by-id or deep-link verification would widen the story past its fixed-scope review boundary. [src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:178]
status: done 2026-09-02
resolution: already resolved: commit 03566fb1; GlobalAdministratorsPage.razor:1442-1466,3008-3019,3710-3734 performs bounded complete-population loading before grant and removal confirmation.

### DW-171: UnableToVerify copy mentions confirming via the tenant audit trail without an in-page navigation link
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-08, loop 5 chunk 2)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:184
reason: The legacy ledger defers this issue: UnableToVerify copy mentions confirming via the tenant audit trail without an in-page navigation link. Original context is preserved in legacy-detail.
legacy-detail: - UnableToVerify copy mentions confirming via the tenant audit trail without an in-page navigation link. Audit navigation is outside this story's File List and acceptance criteria. [src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:184]
status: done 2026-09-02
resolution: already resolved: commit 03566fb1; GlobalAdministratorGrantCommandSnapshot.cs:337-350 accepts only complete evidence, and the legacy PageScoped audit-trail copy remains only as unreferenced resource keys.

### DW-172: A `Ready` snapshot reporting `HasMore == true` with a blank `NextCursor` is a silent dead end. Loop 2 correctly
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-01, loop 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:698
reason: The legacy ledger defers this issue: A `Ready` snapshot reporting `HasMore == true` with a blank `NextCursor` is a silent dead end. Original context is preserved in legacy-detail.
legacy-detail: - A `Ready` snapshot reporting `HasMore == true` with a blank `NextCursor` is a silent dead end. Loop 2 correctly converted the dead-but-clickable Next into a disabled Next, but `CanRecover` deliberately excludes `Ready`, so neither Retry nor Reset renders, Previous is disabled on page one, and no notice explains the condition. The surface states more administrators exist and offers no way to reach them. Reason for deferral: needs a copy/design decision on how to announce incomplete evidence on an otherwise healthy surface, not a mechanical fix; the service should not normally produce this shape. Revisit if: the query contract allows `HasMore` without a cursor, or `CanRecover` is revised. **SUPERSEDED (2026-08-08, loop 5):** owner chose option 1 — condition-gated recoverable incomplete paging (`HasMore && blank NextCursor`) with localized notice. **CLOSED (2026-08-08, loop 5 APPLIED):** `HasIncompletePagingEvidence` / recovery + localized notice shipped; no longer an open loop-5 patch. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:698]
status: done 2026-08-08
resolution: Legacy completion record: - A `Ready` snapshot reporting `HasMore == true` with a blank `NextCursor` is a silent dead end. Loop 2 correctly converted the dead-but-clickable Next into a disabled Next, but `CanRecover` deliberately excludes `Ready`, so neither Retry nor Reset renders, Previous is disabled on page one, and no notice explains the condition. The surface states more administrators exist and offers no way to reach them. Reason for deferral: needs a copy/design decision on how to announce incomplete evidence on an otherwise healthy surface, not a mechanical fix; the service should not normally produce this shape. Revisit if: the query contract allows `HasMore` without a cursor, or `CanRecover` is revised. **SUPERSEDED (2026-08-08, loop 5):** owner chose option 1 — condition-gated recoverable incomplete paging (`HasMore && blank NextCursor`) with localized notice. **CLOSED (2026-08-08, loop 5 APPLIED):** `HasIncompletePagingEvidence` / recovery + localized notice shipped; no longer an open loop-5 patch. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:698]

### DW-173: Authorization resolution is uncancellable from both consuming pages. The loop-2 patch made
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-01, loop 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1634; TenantsWorkspace.razor:564
reason: The legacy ledger defers this issue: Authorization resolution is uncancellable from both consuming pages. Original context is preserved in legacy-detail.
legacy-detail: - Authorization resolution is uncancellable from both consuming pages. The loop-2 patch made `TenantConfigurationPrincipalResolver.ResolveAsync` honour caller cancellation via `.WaitAsync(token)`, but `GlobalAdministratorsPage.ResolveAuthorizationReflectionAsync` and `TenantsWorkspace.razor:564` both call the BFF seam with no token, so `CancellationToken.None` plus an infinite timeout makes that seam inert for them. Only `TenantDetailPage` passes a token. `RetryAuthorizationAsync` additionally holds the atomic page-load gate across the resolve and releases it only in `finally`, so a hung provider leaves authorization-Retry, Retry, Reset, Previous and Next all disabled with nothing able to interrupt it. Reason for deferral: same timeout-policy decision as the existing `EnsureReadRefreshLeaseAsync` `CancellationToken.None` deferral — picking a bound is a policy call, and both should be settled together. Revisit if: a resolve/subscribe timeout policy is chosen, or a hung-provider incident is observed. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1634]
status: open
decision: 2026-08-26 Configured shared bound — Propagate page or circuit cancellation and apply one configurable finite resolver and subscription timeout.
decision: 2026-08-25 Configured finite bound — Propagate page or circuit cancellation and apply one configurable finite resolver and subscription timeout, failing closed to Indeterminate.

### DW-174: AC5 remains partially unmet while the story sits in `review`: the narrow-viewport per-row Remove reason gap
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-01, loop 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:466
reason: The legacy ledger defers this issue: AC5 remains partially unmet while the story sits in `review`: the narrow-viewport per-row Remove reason gap. Original context is preserved in legacy-detail.
legacy-detail: - ~~AC5 remains partially unmet while the story sits in `review`: the narrow-viewport per-row Remove reason gap recorded above by loop 2 was re-confirmed still open by loop 3.~~ **CLOSED (2026-08-08, loop 5 APPLIED):** mobile/unavailable reasons and mutation-initiation gating closed AC5. Cross-reference only — see the loop-2 bullet and loop-5 APPLIED patch. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:466]
status: done 2026-08-08
resolution: Legacy completion record: - ~~AC5 remains partially unmet while the story sits in `review`: the narrow-viewport per-row Remove reason gap recorded above by loop 2 was re-confirmed still open by loop 3.~~ **CLOSED (2026-08-08, loop 5 APPLIED):** mobile/unavailable reasons and mutation-initiation gating closed AC5. Cross-reference only — see the loop-2 bullet and loop-5 APPLIED patch. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:466]

### DW-175: TenantsWorkspace nests ProjectionLifecycleBadge inside polite atomic status region — deferred, pre-existing lifecycle-badge composition (not core 1.11 auth)
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:171
reason: The legacy ledger defers this issue: TenantsWorkspace nests ProjectionLifecycleBadge inside polite atomic status region — deferred, pre-existing lifecycle-badge composition (not core 1.11 auth). Original context is preserved in legacy-detail.
legacy-detail: - TenantsWorkspace nests ProjectionLifecycleBadge inside polite atomic status region — deferred, pre-existing lifecycle-badge composition (not core 1.11 auth) [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:171]
status: done 2026-08-25
resolution: already resolved: commit dc2639f0; src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:175-181 renders ProjectionLifecycleStatus outside the notices live region.

### DW-176: Workspace GA entry resolve calls ResolveGlobalAdministratorsAuthorizationAsync without CancellationToken — deferred, pre-existing fire-and-forget entry path; version/_disposed still gate apply
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:565
reason: The legacy ledger defers this issue: Workspace GA entry resolve calls ResolveGlobalAdministratorsAuthorizationAsync without CancellationToken — deferred, pre-existing fire-and-forget entry path; version/_disposed still gate apply. Original context is preserved in legacy-detail.
legacy-detail: - Workspace GA entry resolve calls ResolveGlobalAdministratorsAuthorizationAsync without CancellationToken — deferred, pre-existing fire-and-forget entry path; version/_disposed still gate apply [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:565]
status: open
decision: 2026-08-26 Replaceable bounded CTS — Use a replaceable cancellation source plus the shared configured authorization bound.
decision: 2026-08-25 Bounded workspace policy — Use a replaceable cancellation source plus the shared configured authorization bound.

### DW-177: Soft `RefreshAsync` blanks the tenant list via Loading/ShowList — deferred, pre-existing UX; workspace never had retainConfirmed
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:679
reason: The legacy ledger defers this issue: Soft `RefreshAsync` blanks the tenant list via Loading/ShowList — deferred, pre-existing UX; workspace never had retainConfirmed. Original context is preserved in legacy-detail.
legacy-detail: - Soft `RefreshAsync` blanks the tenant list via Loading/ShowList — deferred, pre-existing UX; workspace never had retainConfirmed. Distinct from the LoadAsync version-bump stale-apply window below (same approximate line region, different method). [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:679]
status: open

### DW-178: StartLifecycleAuthorizationResolution runs on every OnParametersSetAsync without TenantId short-circuit — deferred, mitigated by generation + CTS replace
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:339
reason: The legacy ledger defers this issue: StartLifecycleAuthorizationResolution runs on every OnParametersSetAsync without TenantId short-circuit — deferred, mitigated by generation + CTS replace. Original context is preserved in legacy-detail.
legacy-detail: - StartLifecycleAuthorizationResolution runs on every OnParametersSetAsync without TenantId short-circuit — deferred, mitigated by generation + CTS replace [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:339]
status: open

### DW-179: IsSafeReturnUrl accepts any /tenants-prefixed path — deferred, already deferred earlier; same-origin relative only
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1250
reason: The legacy ledger defers this issue: IsSafeReturnUrl accepts any /tenants-prefixed path — deferred, already deferred earlier; same-origin relative only. Original context is preserved in legacy-detail.
legacy-detail: - IsSafeReturnUrl accepts any /tenants-prefixed path — deferred, already deferred earlier; same-origin relative only [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1250]
status: open

### DW-180: `LoadAsync` version bump after `BeginLoad` leaves a stale-apply window — deferred, pre-existing workspace load pattern
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:679
reason: The legacy ledger defers this issue: `LoadAsync` version bump after `BeginLoad` leaves a stale-apply window — deferred, pre-existing workspace load pattern. Original context is preserved in legacy-detail.
legacy-detail: - `LoadAsync` version bump after `BeginLoad` leaves a stale-apply window — deferred, pre-existing workspace load pattern. Distinct from soft `RefreshAsync` blanking above (same approximate line region, different method). [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:679]
status: open

### DW-181: TenantDetailPage BeginLoad deferred-CTS disposal lacks a workspace-equivalent runtime test — deferred, coverage gap only
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs
reason: The legacy ledger defers this issue: TenantDetailPage BeginLoad deferred-CTS disposal lacks a workspace-equivalent runtime test — deferred, coverage gap only. Original context is preserved in legacy-detail.
legacy-detail: - TenantDetailPage BeginLoad deferred-CTS disposal lacks a workspace-equivalent runtime test — deferred, coverage gap only [tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs]
status: open

### DW-182: EditTenantMetadataFlow Lifecycle wiring not asserted via tenants-edit-metadata-open on the detail page — deferred, covered by EditTenantMetadataFlowTests
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:146
reason: The legacy ledger defers this issue: EditTenantMetadataFlow Lifecycle wiring not asserted via tenants-edit-metadata-open on the detail page — deferred, covered by EditTenantMetadataFlowTests. Original context is preserved in legacy-detail.
legacy-detail: - EditTenantMetadataFlow Lifecycle wiring not asserted via tenants-edit-metadata-open on the detail page — deferred, covered by EditTenantMetadataFlowTests [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:146]
status: done 2026-08-25
resolution: already resolved: commits a53cb979 and ad18d62c; TenantDetailPage.razor:149-160 binds lifecycle/proof inputs and TenantDetailSurfaceTests.cs:2942-2961 opens metadata through the page boundary.

### DW-183: Route change during in-flight prior-tenant subscribe can briefly miss auto-refresh — deferred, previously deferred; OnAfterRender retry partially mitigates
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:447
reason: The legacy ledger defers this issue: Route change during in-flight prior-tenant subscribe can briefly miss auto-refresh — deferred, previously deferred; OnAfterRender retry partially mitigates. Original context is preserved in legacy-detail.
legacy-detail: - Route change during in-flight prior-tenant subscribe can briefly miss auto-refresh — deferred, previously deferred; OnAfterRender retry partially mitigates [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:447]
status: open

### DW-184: ApplyAuthenticationStateChangedAsync awaits authenticationStateTask with no timeout — deferred, pre-existing; fail-closed hides entry until auth completes
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:603
reason: The legacy ledger defers this issue: ApplyAuthenticationStateChangedAsync awaits authenticationStateTask with no timeout — deferred, pre-existing; fail-closed hides entry until auth completes. Original context is preserved in legacy-detail.
legacy-detail: - ApplyAuthenticationStateChangedAsync awaits authenticationStateTask with no timeout — deferred, pre-existing; fail-closed hides entry until auth completes [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:603]
status: open
decision: 2026-08-26 Bound authentication wait — Apply the shared authorization timeout and cancellation policy and leave the entry hidden on timeout.
decision: 2026-08-25 Bound authentication wait — Apply the shared authorization timeout and cancellation policy and leave the entry hidden on timeout.

### DW-185: Member Next stays enabled when HasMore has blank NextCursor — deferred, MemberAccessReview not in this story File List; page already no-ops; pre-existing pager honesty gap
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:846
reason: The legacy ledger defers this issue: Member Next stays enabled when HasMore has blank NextCursor — deferred, MemberAccessReview not in this story File List; page already no-ops; pre-existing pager honesty gap. Original context is preserved in legacy-detail.
legacy-detail: - Member Next stays enabled when HasMore has blank NextCursor — deferred, MemberAccessReview not in this story File List; page already no-ops; pre-existing pager honesty gap [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:846]
status: done 2026-08-27
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1202-1211 refuses Next when HasMore is false or NextCursor is blank, and the rendered pager applies the same guard.

### DW-186: Coarse `StaleData` category for every non-Current projection lifecycle — deferred, pre-existing; message key is specific (`ProjectionLifecycle`) but category chip stays StaleData
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-12-projection-lifecycle-badges.md (2026-08-08)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs:64
reason: The legacy ledger defers this issue: Coarse `StaleData` category for every non-Current projection lifecycle — deferred, pre-existing; message key is specific (`ProjectionLifecycle`) but category chip stays StaleData. Original context is preserved in legacy-detail.
legacy-detail: - Coarse `StaleData` category for every non-Current projection lifecycle — deferred, pre-existing; message key is specific (`ProjectionLifecycle`) but category chip stays StaleData [`src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs:64`]
status: open
decision: 2026-09-02 Add lifecycle category — Add a dedicated projection-lifecycle category and migrate mappings, chips, resources, and tests compatibly.
decision: 2026-09-02 Add lifecycle category — Add a dedicated projection-lifecycle category and migrate mappings, chips, resources, and tests compatibly.

### DW-187: Open Set/Remove/Edit flows do not reset when lifecycle flips mid-flight (only lifecycle-action re-evals) — deferred, pre-existing command-flow pattern beyond this story's badge split
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-12-projection-lifecycle-badges.md (2026-08-08)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor
reason: The legacy ledger defers this issue: Open Set/Remove/Edit flows do not reset when lifecycle flips mid-flight (only lifecycle-action re-evals) — deferred, pre-existing command-flow pattern beyond this story's badge split. Original context is preserved in legacy-detail.
legacy-detail: - Open Set/Remove/Edit flows do not reset when lifecycle flips mid-flight (only lifecycle-action re-evals) — deferred, pre-existing command-flow pattern beyond this story's badge split [`src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor`]
status: open

### DW-188: CorrectionStartPanel still submits membership corrections without reusable messageId tracking
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: CorrectionStartPanel still submits membership corrections without reusable messageId tracking. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: CorrectionStartPanel still submits membership corrections without reusable messageId tracking. evidence: Shared gateway now accepts optional messageId, but correction UI was outside Story 2.1 membership-flow File List and still mints a new ULID per attempt.
status: open

### DW-189: Projection-version advancement is opaque inequality only, with no causal/audit-qualified branch
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Projection-version advancement is opaque inequality only, with no causal/audit-qualified branch. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Projection-version advancement is opaque inequality only, with no causal/audit-qualified branch. evidence: Confirm uses non-equal non-empty version strings; safe audit provenance newer than baseline remains unimplemented though the Always clause allows version OR audit.
status: done 2026-08-25
resolution: already resolved: commit 28d32ca8; src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs:36-70 requires exact command-event evidence and ordered causal advancement.

### DW-190: AggregateAdmissionGate falls back to a page-private instance when DI resolution fails
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: AggregateAdmissionGate falls back to a page-private instance when DI resolution fails. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: AggregateAdmissionGate falls back to a page-private instance when DI resolution fails. evidence: A private gate cannot enforce circuit-scoped AggregateIdentity admission shared with other consumers; only the DI-registered singleton does.
status: done 2026-08-25
resolution: already resolved: commit d3f74f58; src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:411-412 resolves the admission gate only from DI.

### DW-191: SignalR nudge is skipped when MemberAccessReview ref is still null
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: SignalR nudge is skipped when MemberAccessReview ref is still null. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: SignalR nudge is skipped when MemberAccessReview ref is still null. evidence: TenantDetailPage forwards only when `_memberAccessReview is not null`; early refresh before child attach can miss an in-flight nudge.
status: open

### DW-192: The mandatory story gitlink validator fails against current HEAD because seven unrelated submodule pointer bumps landed after the isolated Story 2.1 commit
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation.md (2026-08-19)"), 2026-08-25
location: scripts/validate-story-gitlinks.py; python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: The mandatory story gitlink validator fails against current HEAD because seven unrelated submodule pointer bumps landed after the isolated Story 2.1 commit. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: The mandatory story gitlink validator fails against current HEAD because seven unrelated submodule pointer bumps landed after the isolated Story 2.1 commit. evidence: `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` reports all seven `references/` pointers as undeclared between baseline `222d5ac` and HEAD `020b099`, while `222d5ac..29c4aec` changes no gitlink.
status: done 2026-08-25
resolution: already resolved: commit 44362aed; spec-2-1-projection-backed-tenant-list.md:147-161 declares all seven pointers and the default validator passes against current HEAD.

### DW-193: Story 2.4b — provenance reconciliation refinements, WP-2A removal-proof assembly (`audit_available`), proof-capability fail-closed gating, and proof-state recovery/tests
origin: migrated from legacy ledger ("Deferred from: bmad-build scope split of Story 2.4 (2026-08-08)"), 2026-08-25
location: audit_available
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Story 2.4b — provenance reconciliation refinements, WP-2A removal-proof assembly (`audit_available`), proof-capability fail-closed gating, and proof-state recovery/tests. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Story 2.4b — provenance reconciliation refinements, WP-2A removal-proof assembly (`audit_available`), proof-capability fail-closed gating, and proof-state recovery/tests. evidence: Split from Story 2.4 so the first delivery goal (2.4a eligibility, complete preview, elevated friction, destructive dialog, dispatch, AggregateIdentity lock) stays within the 900–1600 token spec budget; Story 2.4 remains incomplete until 2.4b also passes.
status: done 2026-08-25
resolution: already resolved: commit fa5ca559; spec-2-4b-wp-2a-removal-proof-and-audit-available.md:1-5 is done and RemoveTenantMemberFlow.razor:985-1080 implements proof assembly.

### DW-194: Document-level Tab trapping / inert backdrop beyond sentinel pattern for remove-member dialog
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Document-level Tab trapping / inert backdrop beyond sentinel pattern for remove-member dialog. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Document-level Tab trapping / inert backdrop beyond sentinel pattern for remove-member dialog. evidence: Spec Ask First prefers Tenants role=dialog + focus-sentinel pattern; full Fluent/FrontComposer modal primitive was not authorized in this slice.
status: open
decision: 2026-08-26 Adopt shared modal — Adopt or create a Fluent or FrontComposer modal with document-level trapping, inert backdrop, and browser tests.
decision: 2026-08-25 Adopt shared modal — Adopt or create a Fluent or FrontComposer modal with document-level trapping, inert backdrop, and browser tests.

### DW-195: JS/viewport submit-time narrow-layout guard beyond CSS fail-closed for remove-member
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: JS/viewport submit-time narrow-layout guard beyond CSS fail-closed for remove-member. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: JS/viewport submit-time narrow-layout guard beyond CSS fail-closed for remove-member. evidence: Matches established RemoveTenantConfigurationFlow CSS-only narrow gate; changing to a runtime viewport gate needs an explicit Ask First decision.
status: open
decision: 2026-08-26 Runtime viewport gate — Reuse viewport observation and refuse submit whenever measured safety is unknown or narrow.
decision: 2026-08-25 Runtime viewport gate — Reuse the high-impact viewport observation pattern and refuse submit whenever measured safety is unknown or narrow.

### DW-196: Remove-member WP-2A assembly only inspects the first audit page; matching UserRemovedFromTenant rows on later pages can leave proof pending
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: Remove-member WP-2A assembly only inspects the first audit page; matching UserRemovedFromTenant rows on later pages can leave proof pending. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: Remove-member WP-2A assembly only inspects the first audit page; matching UserRemovedFromTenant rows on later pages can leave proof pending. evidence: GetTenantAuditAsync is called once without following HasMore/NextCursor; paging loop was deferred to keep this slice within review-patch scope.
status: done 2026-08-25
resolution: already resolved: commit ad18d62c; src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:985-1080 walks bounded audit pages and detects cursor loops.

### DW-197: Proof-capability fail-closed detects only null/UnavailableTenantQueryGateway, not a live stale/unknown audit-capability probe before open
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: Proof-capability fail-closed detects only null/UnavailableTenantQueryGateway, not a live stale/unknown audit-capability probe before open. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: Proof-capability fail-closed detects only null/UnavailableTenantQueryGateway, not a live stale/unknown audit-capability probe before open. evidence: Per-row audit probes would add latency on every member render; Always clause capability language remains partially approximated until a shared capability signal exists.
status: done 2026-08-25
resolution: already resolved: commit ad18d62c; src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1650-1724 performs a live current scoped projection-backed audit-capability probe.

### DW-198: Default `validate-story-gitlinks.py` execution compares the Story 2.4 baseline to the later repository `HEAD` and reports seven undeclared `references/` pointer moves made by post-story dependency commits. Exact story-range validation with `--ref fa5ca559` passes for both 2.4 specifications with no gitlink changes. Keep the later dependency bumps outside Story 2.4 review scope; make story-end range selection durable if old stories must remain independently re-reviewable after `main` advances
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-19)"), 2026-08-25
location: validate-story-gitlinks.py; references/
reason: The legacy ledger defers this issue: Default `validate-story-gitlinks.py` execution compares the Story 2.4 baseline to the later repository `HEAD` and reports seven undeclared `references/` pointer moves made by post-story dependency commits. Original context is preserved in legacy-detail.
legacy-detail: - Default `validate-story-gitlinks.py` execution compares the Story 2.4 baseline to the later repository `HEAD` and reports seven undeclared `references/` pointer moves made by post-story dependency commits. Exact story-range validation with `--ref fa5ca559` passes for both 2.4 specifications with no gitlink changes. Keep the later dependency bumps outside Story 2.4 review scope; make story-end range selection durable if old stories must remain independently re-reviewable after `main` advances.
status: open

### DW-199: AggregateIdentity-scoped create admission lock through terminal evidence (unrelated aggregates may proceed; replace create-local submitting flag with TenantAggregateCommandAdmissionGate)
origin: migrated from legacy ledger ("Deferred from: bmad-build scope split of Story 3.1 (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: AggregateIdentity-scoped create admission lock through terminal evidence (unrelated aggregates may proceed; replace create-local submitting flag with TenantAggregateCommandAdmissionGate). Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: AggregateIdentity-scoped create admission lock through terminal evidence (unrelated aggregates may proceed; replace create-local submitting flag with TenantAggregateCommandAdmissionGate). evidence: Split from Story 3.1 so the first delivery goal (provenance-qualified confirmation, first-tenant freshness exception, messageId reuse) stays within the 900–1600 token spec budget.
status: open

### DW-200: Workspace SignalR / read-refresh nudge wiring into CreateTenantFlow (ApplySignalRNudge + authoritative re-query; never notify-alone confirm)
origin: migrated from legacy ledger ("Deferred from: bmad-build scope split of Story 3.1 (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: Workspace SignalR / read-refresh nudge wiring into CreateTenantFlow (ApplySignalRNudge + authoritative re-query; never notify-alone confirm). Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: Workspace SignalR / read-refresh nudge wiring into CreateTenantFlow (ApplySignalRNudge + authoritative re-query; never notify-alone confirm). evidence: Split from Story 3.1; membership already has the pattern, and create confirmation honesty can ship before host nudge plumbing.
status: open

### DW-201: Create-tenant mobile/unsafe-viewport fail-closed and open-existing recovery CTA affordances beyond localized Rejected copy
origin: migrated from legacy ledger ("Deferred from: bmad-build scope split of Story 3.1 (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: Create-tenant mobile/unsafe-viewport fail-closed and open-existing recovery CTA affordances beyond localized Rejected copy. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: Create-tenant mobile/unsafe-viewport fail-closed and open-existing recovery CTA affordances beyond localized Rejected copy. evidence: Split from Story 3.1; confirmation/freshness/idempotency core does not require viewport gating or interactive open-existing navigation in the first slice.
status: open

### DW-202: Create confirmation cannot correlate projection evidence to this attempt's messageId; concurrent same-id creates or unrelated list ProjectionVersion churn can still satisfy absence-then-presence + version rules
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: Create confirmation cannot correlate projection evidence to this attempt's messageId; concurrent same-id creates or unrelated list ProjectionVersion churn can still satisfy absence-then-presence + version rules. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: Create confirmation cannot correlate projection evidence to this attempt's messageId; concurrent same-id creates or unrelated list ProjectionVersion churn can still satisfy absence-then-presence + version rules. evidence: ConfirmProjection uses metadata match plus opaque list/detail version advancement only; command-specific audit provenance branch remains unused (AttemptStartedAtUtc captured but not applied).
status: done 2026-08-25
resolution: already resolved: commit b2b80941; src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:307-375 requires exact tracked-command evidence plus ordered projection advancement or first appearance.

### DW-203: Workspace IsCommandSurfaceConnected is a render-time service lookup with no subscription, so BFF disconnect may not refresh create availability until an unrelated rerender
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-08)"), 2026-08-25
location: ITenantsBffComposition.IsCommandSurfaceConnected
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: Workspace IsCommandSurfaceConnected is a render-time service lookup with no subscription, so BFF disconnect may not refresh create availability until an unrelated rerender. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: Workspace IsCommandSurfaceConnected is a render-time service lookup with no subscription, so BFF disconnect may not refresh create availability until an unrelated rerender. evidence: The `ITenantsBffComposition.IsCommandSurfaceConnected` property pre-existed, but Story 3.1 introduced the workspace-side render-time lookup and the `IsCommandSurfaceAvailable` parameter binding themselves. Corrected by code review 2026-08-21: the original wording understated what this story added.
status: done 2026-08-28
decision: 2026-08-28 Declare circuit immutable — Document connectivity as immutable for the circuit and DI scope because no supported runtime transition exists.
resolution: closed by human decision: Document connectivity as immutable for the circuit and DI scope because no supported runtime transition exists.
decision: 2026-08-28 Declare circuit immutable — Document connectivity as immutable for the circuit and DI scope because no supported runtime transition exists.

### DW-204: SignalR-elevated ProjectionPending can still confirm after unrelated projection-version advancement on metadata (and sibling create/membership) flows
origin: migrated from legacy ledger ("Deferred from: implementation of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: SignalR-elevated ProjectionPending can still confirm after unrelated projection-version advancement on metadata (and sibling create/membership) flows. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: SignalR-elevated ProjectionPending can still confirm after unrelated projection-version advancement on metadata (and sibling create/membership) flows. evidence: Story 3.2 edge-case review; SignalRNudge promotes Accepted/RequestSent to ProjectionPending without EventsStored, then ConfirmProjection may confirm on version inequality that is not command-qualified.
status: done 2026-08-25
resolution: already resolved: commits b2b80941 and a53cb979; TenantCreateCommandModels.cs:299-305 treats SignalR as a nudge and :1394-1409 requires command-specific provenance.

### DW-205: Edit metadata confirmation does not pass live audit-row provenance into ConfirmProjection (version advancement only on the live path)
origin: migrated from legacy ledger ("Deferred from: implementation of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Edit metadata confirmation does not pass live audit-row provenance into ConfirmProjection (version advancement only on the live path). Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Edit metadata confirmation does not pass live audit-row provenance into ConfirmProjection (version advancement only on the live path). evidence: Story 3.2 verification-gap review; AttemptStartedAtUtc and hasQualifyingAuditProvenance exist on the snapshot API but EditTenantMetadataFlow never supplies audit qualification (unlike remove-member WP-2A).
status: done 2026-08-25
resolution: already resolved: commit a53cb979; TenantDetailPage.razor:159-160 supplies AuditEvidenceProvider and TenantCreateCommandModels.cs:1399-1408 validates matching audit proof.

### DW-206: Metadata IsAuthorized still defaults true with no contributor/global-admin BFF authorization reflection wired from TenantDetailPage
origin: migrated from legacy ledger ("Deferred from: implementation of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Metadata IsAuthorized still defaults true with no contributor/global-admin BFF authorization reflection wired from TenantDetailPage. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Metadata IsAuthorized still defaults true with no contributor/global-admin BFF authorization reflection wired from TenantDetailPage. evidence: Story 3.2 blind-hunter review and Ask First deferral; member flows share the same default-true pattern, so inventing metadata-only reflection was out of this slice.
status: open
decision: 2026-08-26 Role-aware BFF reflection — Add tenant-scoped contributor or global-administrator reflection and fail closed while authority is indeterminate.
decision: 2026-08-25 Role-aware BFF reflection — Extend BFF composition with tenant-scoped role-aware reflection and fail closed while contributor authority is indeterminate.

### DW-207: After Confirmed/Rejected/Failed, retained MessageId can be reused on a deliberate new metadata edit instead of minting a new ULID
origin: migrated from legacy ledger ("Deferred from: implementation of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: After Confirmed/Rejected/Failed, retained MessageId can be reused on a deliberate new metadata edit instead of minting a new ULID. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: After Confirmed/Rejected/Failed, retained MessageId can be reused on a deliberate new metadata edit instead of minting a new ULID. evidence: Story 3.2 edge-case review; same reuseMessageId pattern exists on AddTenantMemberFlow and was not uniquely introduced for metadata.
status: done 2026-08-25
resolution: already resolved: commits a53cb979 and c910ea83; EditTenantMetadataFlow.razor:605-625 reuses an ID only for the same recoverable attempt.

### DW-208: Configuration filter comment claims Ordinal matching while FilteredRows uses OrdinalIgnoreCase
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-6-read-only-tenant-configuration.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration.md`
reason: The legacy ledger defers this issue: Configuration filter comment claims Ordinal matching while FilteredRows uses OrdinalIgnoreCase. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration.md` summary: Configuration filter comment claims Ordinal matching while FilteredRows uses OrdinalIgnoreCase. evidence: Pre-existing comment/code mismatch in TenantConfigurationView.razor; not introduced by the FluentStack host migration reviewed in this pass.
status: done 2026-08-25
resolution: already resolved: commit 84ad930d; src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor:177-185 documents and implements Ordinal case-sensitive matching.

### DW-209: Extend PageLayoutDeclarationTests runtime shell coverage beyond TenantsWorkspace/UserMembershipLookup to MyTenants, TenantAudit, and GlobalAdministrators full-width measure
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
reason: The legacy ledger defers this issue: Extend PageLayoutDeclarationTests runtime shell coverage beyond TenantsWorkspace/UserMembershipLookup to MyTenants, TenantAudit, and GlobalAdministrators full-width measure. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md` summary: Extend PageLayoutDeclarationTests runtime shell coverage beyond TenantsWorkspace/UserMembershipLookup to MyTenants, TenantAudit, and GlobalAdministrators full-width measure. evidence: Those three dense pages are source-scanned for FullWidth but have no executable data-fc-page-layout assertion; a Constrained regression would fail only governance text scan.
status: open

### DW-210: Add runtime FcPageHeader assertions for MyTenants and UserMembershipLookup page chrome
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
reason: The legacy ledger defers this issue: Add runtime FcPageHeader assertions for MyTenants and UserMembershipLookup page chrome. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md` summary: Add runtime FcPageHeader assertions for MyTenants and UserMembershipLookup page chrome. evidence: Surface tests assert panels/layout but not header testids; deleting FcPageHeader would leave those suites green aside from source governance.
status: open

### DW-211: Assert TenantsWorkspace post-detail FocusHeadingAsync actually moves focus to tenants-list-heading
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
reason: The legacy ledger defers this issue: Assert TenantsWorkspace post-detail FocusHeadingAsync actually moves focus to tenants-list-heading. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md` summary: Assert TenantsWorkspace post-detail FocusHeadingAsync actually moves focus to tenants-list-heading. evidence: Existing tests only assert tabindex=-1 after return query; removing FocusHeadingAsync would not fail the suite.
status: open

### DW-212: Harden FcPageHeader when PageTitle and Heading are both blank/whitespace so document title cannot resolve empty
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
reason: The legacy ledger defers this issue: Harden FcPageHeader when PageTitle and Heading are both blank/whitespace so document title cannot resolve empty. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md` summary: Harden FcPageHeader when PageTitle and Heading are both blank/whitespace so document title cannot resolve empty. evidence: FrontComposer-owned contract; Tenants callers currently supply localized titles, but the primitive still admits an empty DocumentTitle path.
status: open
decision: 2026-09-02 Reject blank titles — Throw a diagnostic parameter-contract exception when both title inputs are blank and add shared component tests.
decision: 2026-09-02 Reject blank titles — Throw a diagnostic parameter-contract exception when both title inputs are blank and add shared component tests.

### DW-213: Make FocusHeadingAsync fail closed when the heading element is not yet rendered instead of focusing a default ElementReference
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
reason: The legacy ledger defers this issue: Make FocusHeadingAsync fail closed when the heading element is not yet rendered instead of focusing a default ElementReference. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md` summary: Make FocusHeadingAsync fail closed when the heading element is not yet rendered instead of focusing a default ElementReference. evidence: FrontComposer-owned timing contract; Tenants workspace path relies on OnAfterRenderAsync ordering.
status: open

### DW-214: Align RemoveTenantMemberFlowTests StubTenantsLocalizer keys/values with shipped TenantsResources (EN/FR) so LocalizerDoubleParityTests passes
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
reason: The legacy ledger defers this issue: Align RemoveTenantMemberFlowTests StubTenantsLocalizer keys/values with shipped TenantsResources (EN/FR) so LocalizerDoubleParityTests passes. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md` summary: Align RemoveTenantMemberFlowTests StubTenantsLocalizer keys/values with shipped TenantsResources (EN/FR) so LocalizerDoubleParityTests passes. evidence: Pre-existing full UI suite failure (1997/1998) unrelated to page-layout governance patches; stub audit-receipt keys drift from resx.
status: done 2026-08-25
resolution: already resolved: commit ad18d62c; LocalizerDoubleParityTests passes and removal preview/audit keys match shipped EN/FR resources.

### DW-215: Align release-tag validation with semantic-release SemVer parsing
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: Align release-tag validation with semantic-release SemVer parsing. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: Align release-tag validation with semantic-release SemVer parsing. evidence: The pre-existing tag filter accepts leading-zero or oversized numeric tags that semantic-release may ignore, allowing the guard and release engine to select different floors.
status: open

### DW-216: Validate normalized NuGet version grammar before excluding prereleases from the registry floor
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: Validate normalized NuGet version grammar before excluding prereleases from the registry floor. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: Validate normalized NuGet version grammar before excluding prereleases from the registry floor. evidence: The pre-existing registry parser accepts malformed prerelease and non-normalized stable strings, which can turn unusable evidence into a passing drift check.
status: open

### DW-217: Reconcile publication-preflight and contributor recovery guidance with authentic-tag-only provenance policy
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: Reconcile publication-preflight and contributor recovery guidance with authentic-tag-only provenance policy. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: Reconcile publication-preflight and contributor recovery guidance with authentic-tag-only provenance policy. evidence: Existing script and CONTRIBUTING guidance still recommends restoring deleted tags or advancing via a breaking footer without the producing-commit and reachability safeguards adopted by this spec.
status: open

### DW-218: Correct stale release trigger and current release-line documentation
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: Correct stale release trigger and current release-line documentation. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: Correct stale release trigger and current release-line documentation. evidence: Existing project context and contributor docs describe automatic workflow-run publication and a 4.x current line, while release is manually dispatched and reachable history extends through 5.x.
status: open

### DW-219: Replace absolute collision wording with an accurate unproven-lineage risk statement
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: Replace absolute collision wording with an accurate unproven-lineage risk statement. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: Replace absolute collision wording with an accurate unproven-lineage risk statement. evidence: The existing guard message says semantic-release would certainly propose an occupied version even though it does not compute the proposal and the registry range may contain gaps.
status: open

### DW-220: Remove default set/remove projection-proof implementations from ITenantQueryGateway so every gateway and decorator must implement the security-sensitive proof contract explicitly
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration-2.md`
reason: The legacy ledger defers this issue: Remove default set/remove projection-proof implementations from ITenantQueryGateway so every gateway and decorator must implement the security-sensitive proof contract explicitly. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration-2.md` summary: Remove default set/remove projection-proof implementations from ITenantQueryGateway so every gateway and decorator must implement the security-sensitive proof contract explicitly. evidence: The pre-existing interface defaults allow a future implementation to omit both methods, compile successfully, and silently return unavailable proof; the concrete unavailable gateway is now covered directly, but the interface design debt remains outside this review diff.
status: open
decision: 2026-08-28 Require explicit methods — Remove the default implementations and update every gateway and stub to implement the proof contracts explicitly.
decision: 2026-08-28 Require explicit methods — Remove the default implementations and update every gateway and stub to implement the proof contracts explicitly.

### DW-221: Introduce a shape-preserving configuration schema or discriminator that distinguishes valid empty policy arrays from empty scalar declarations
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration-2.md`
reason: The legacy ledger defers this issue: Introduce a shape-preserving configuration schema or discriminator that distinguishes valid empty policy arrays from empty scalar declarations. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration-2.md` summary: Introduce a shape-preserving configuration schema or discriminator that distinguishes valid empty policy arrays from empty scalar declarations. evidence: Standard IConfiguration flattening makes JSON [] and scalar "" observationally identical, so the current provider safely withholds all approval but cannot render the scalar form as policy-unavailable without also rejecting the repository's valid-empty default.
status: open
decision: 2026-08-26 Add shape metadata — Add backward-compatible raw-shape or discriminator metadata alongside existing configuration keys.
decision: 2026-08-25 Shape metadata — Add backward-compatible raw-shape or discriminator metadata alongside existing IConfiguration keys.

### DW-222: Bind remove-member audit receipts to the exact submitted command before presenting them as attempt-specific proof
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Bind remove-member audit receipts to the exact submitted command before presenting them as attempt-specific proof. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Bind remove-member audit receipts to the exact submitted command before presenting them as attempt-specific proof. evidence: TenantAuditRow exposes event, tenant, target, actor, and time but no message or correlation identifier, so a concurrent same-target removal can currently be rendered as this attempt's receipt.
status: open
decision: 2026-08-26 Add causation identity — Extend audit projection and API contracts with causation IDs and filter receipts to the exact submitted command.

### DW-223: Page through bounded tenant-audit results while assembling remove-member proof
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Page through bounded tenant-audit results while assembling remove-member proof. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Page through bounded tenant-audit results while assembling remove-member proof. evidence: The existing proof query reads only the first audit page and ignores HasMore and NextCursor, so a qualifying event outside the first page is never found.
status: done 2026-08-25
resolution: already resolved: commit ad18d62c; RemoveTenantMemberFlow.razor:985-1080 implements the bounded multi-page audit walk required by this duplicate.

### DW-224: Require current projection lifecycle, freshness, and projection-backed provenance before promoting removal audit evidence to available proof
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Require current projection lifecycle, freshness, and projection-backed provenance before promoting removal audit evidence to available proof. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Require current projection lifecycle, freshness, and projection-backed provenance before promoting removal audit evidence to available proof. evidence: A Ready audit surface can carry unknown lifecycle or provenance, yet the current proof path accepts its rows once the surface is neither stale nor degraded.
status: done 2026-08-25
resolution: already resolved: commit ad18d62c; RemoveTenantMemberFlow.razor:1017-1054 requires current row/page freshness, lifecycle, projection provenance, and a ready receipt before AuditAvailable.

### DW-225: Replace client/server wall-clock matching for removal proof with a causally stable boundary
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Replace client/server wall-clock matching for removal proof with a causally stable boundary. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Replace client/server wall-clock matching for removal proof with a causally stable boundary. evidence: AttemptStartedAtUtc is captured on the UI clock and compared directly with server event timestamps, so ordinary clock skew can hide a legitimate event or admit an equal-time event despite strict-advancement wording.
status: open
decision: 2026-08-26 Use causation identity — Bind proof to command and event causation identity in coordination with DW-222.

### DW-226: Downgrade retained global-administrator evidence when a tenant-detail supplementary refresh fails
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Downgrade retained global-administrator evidence when a tenant-detail supplementary refresh fails. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Downgrade retained global-administrator evidence when a tenant-detail supplementary refresh fails. evidence: The existing refresh failure path keeps the previous Current and complete snapshot unchanged, allowing a removal preview to continue asserting platform standing from silently stale evidence.
status: done 2026-08-25
resolution: already resolved: commit ad18d62c; TenantDetailPage.razor:958-980 fail-closes retained GA evidence on refresh failure and :1737-1753 downgrades unsafe evidence.

### DW-227: Focus the actual remove-member controls rather than tabindex wrappers during dialog trapping and restoration
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Focus the actual remove-member controls rather than tabindex wrappers during dialog trapping and restoration. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Focus the actual remove-member controls rather than tabindex wrappers during dialog trapping and restoration. evidence: The existing focus sentinels and close restoration target noninteractive span wrappers, which can move keyboard focus away from the intended confirm, cancel, or launch button.
status: open

### DW-228: Refresh remove-member audit guidance now that the flow queries and renders audit receipts
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Refresh remove-member audit guidance now that the flow queries and renders audit receipts. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Refresh remove-member audit guidance now that the flow queries and renders audit receipts. evidence: Existing English and French preview copy still says audit evidence is unavailable until a future evidence source exists, contradicting the implemented receipt query.
status: done 2026-08-25
resolution: already resolved: commit ad18d62c; TenantsResources.resx and TenantsResources.fr.resx:2313-2318 explain that audit evidence may remain pending, delayed, or unavailable.

### DW-229: Verify the tenant-detail global-administrator evidence bridge at the page boundary
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Verify the tenant-detail global-administrator evidence bridge at the page boundary. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Verify the tenant-detail global-administrator evidence bridge at the page boundary. evidence: Existing tests inject GlobalAdministratorsSnapshot directly into MemberAccessReview, so removing the page assignment or parameter binding would not fail coverage.
status: done 2026-08-25
resolution: already resolved: commit ad18d62c; TenantDetailPage.razor:201 binds GlobalAdministrators and TenantDetailSurfaceTests.cs:239-283 verifies propagation.

### DW-230: Pre-existing flaky false-success in the global-administrator grant re-query
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation (2026-08-20)"), 2026-08-25
location: Grant_requery_does_not_confirm_from_a_superseded_snapshot
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Pre-existing flaky false-success in the global-administrator grant re-query. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Pre-existing flaky false-success in the global-administrator grant re-query. evidence: `Grant_requery_does_not_confirm_from_a_superseded_snapshot` fails in 3 of 4 clean-HEAD Release runs, rendering "Projection confirmed the target user" from a superseded snapshot. Introduced by `d0f74a48` (Story 1.11), not by Story 2.1. This is a live non-collapse violation of the same class Epic 2 exists to prevent.
status: done 2026-09-02
resolution: already resolved: commit 24e3a41d; GlobalAdministratorsPage.razor:2950-3005 rejects superseded status results and lines 3015-3039 reject superseded projection results.

### DW-231: Global-administrator command surface is not covered by the new AggregateIdentity admission gate
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation (2026-08-20)"), 2026-08-25
location: src/; tests/
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Global-administrator command surface is not covered by the new AggregateIdentity admission gate. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Global-administrator command surface is not covered by the new AggregateIdentity admission gate. evidence: `TenantCommandAggregateLock.ForGlobalAdministrators()` and `TenantAggregateCommandAdmissionGate.HasActiveLock` have zero call sites in `src/` or `tests/`; `GlobalAdministratorsPage` and `GlobalAdministratorCorrectionPanel` dispatch ungated, so one-at-a-time exclusivity holds for tenant aggregates only.
status: done 2026-08-28
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1042-1046,2175-2179,2430-2434 resolves the admission gate and acquires the fixed aggregate lease; commits 03566fb1 and ba060a2f.

### DW-232: Optional `messageId` reached create-tenant and update-tenant beyond the declared membership scope
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation (2026-08-20)"), 2026-08-25
location: messageId
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Optional `messageId` reached create-tenant and update-tenant beyond the declared membership scope. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Optional `messageId` reached create-tenant and update-tenant beyond the declared membership scope. evidence: The Code Map scopes optional `messageId` to add/change/remove, but `ITenantCommandGateway.CreateTenantAsync` and `UpdateTenantAsync` also gained `string? messageId = null` and the new ULID-canonicality rejection.
status: open

### DW-233: A missing admission-gate registration disables Epic 3 command surfaces as well as membership
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation (2026-08-20)"), 2026-08-25
location: IsCommandSurfaceAvailable
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: A missing admission-gate registration disables Epic 3 command surfaces as well as membership. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: A missing admission-gate registration disables Epic 3 command surfaces as well as membership. evidence: `IsCommandSurfaceAvailable` now requires `AggregateAdmissionGate is not null`, and that value is passed to `EditTenantMetadataFlow`, `TenantLifecycleActionAvailability`, and `TenantConfigurationManagement`. Fail-closed, so acceptable, but it widens the blast radius of a composition mistake beyond this story.
status: done 2026-08-28
resolution: already resolved: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:96 registers TenantAggregateCommandAdmissionGate, while TenantDetailPage.razor:358-362 remains intentionally fail-closed when unavailable.

### DW-234: Gateway and snapshot safe-message strings are still hard-coded English
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation (2026-08-20)"), 2026-08-25
location: TenantCommandGateway
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Gateway and snapshot safe-message strings are still hard-coded English. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Gateway and snapshot safe-message strings are still hard-coded English. evidence: `TenantCommandGateway` returns raw `SafeMessage` text and `TenantCreateCommandModels` hard-codes strings with `SafeMessageKey = null`; `DisplaySafeMessage` renders them verbatim, so French users see English on exactly the paths the `SafeMessageKey` mechanism was introduced to fix.
status: open

### DW-235: Fail membership action availability closed against live authorization reflection
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: MemberAccessReview
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Fail membership action availability closed against live authorization reflection. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Fail membership action availability closed against live authorization reflection. evidence: `MemberAccessReview` defaults add/change/remove authorization parameters to true, `TenantDetailPage` does not bind membership authorization evidence, and `BuildActionSlots` ignores the change-role and remove-member authorization values when deciding whether to render launch buttons; denial is enforced only after a flow is opened.
status: open
decision: 2026-08-26 Bind role reflection — Add tenant-scoped owner or global-administrator reflection, bind all flags, and show denial or indeterminate reasons.
decision: 2026-08-25 Role-aware BFF reflection — Add tenant-scoped owner or global-administrator reflection, bind all flags, and include denial or indeterminate reasons.

### DW-236: Serialize child membership-command lease acquisition
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: HandleCommandActivityLeaseAsync
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Serialize child membership-command lease acquisition. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Serialize child membership-command lease acquisition. evidence: `HandleCommandActivityLeaseAsync` checks `_childCommandLeaseOwner` before awaiting the parent lease without a local serialization gate, so two reentrant acquisitions can both observe no owner and dispatch under the same aggregate lock.
status: done 2026-08-25
resolution: already resolved: commit 28d32ca8; MemberAccessReview.razor:759-797 reserves _childCommandLeaseOwner before awaiting the parent lease and clears it on refusal or fault.

### DW-237: Keep an open membership command flow keyed to its captured target
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Keep an open membership command flow keyed to its captured target. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Keep an open membership command flow keyed to its captured target. evidence: Opening another row updates the active member parameters while reusing the existing change-role or remove-member component instance, whose snapshot can retain the previous intent and command identity.
status: done 2026-08-25
resolution: already resolved: commit 28d32ca8; MemberAccessReview.razor:239-280 keys flows by active user ID and :327-335 retains captured target records.

### DW-238: Give Continue read-only a stable dialog lifecycle
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: RemoveTenantMemberFlow.ContinueReadOnlyAsync
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Give Continue read-only a stable dialog lifecycle. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Give Continue read-only a stable dialog lifecycle. evidence: `RemoveTenantMemberFlow.ContinueReadOnlyAsync` resets the snapshot to Idle without dismissing the dialog; the next parameter/render cycle can immediately reconstruct the preview, leaving the operator in an ambiguous open-flow state.
status: done 2026-08-25
resolution: already resolved: commits b2b80941 and 28d32ca8; RemoveTenantMemberFlow.razor:1148-1155 marks dismissed before reset and invokes OnCloseRequested.

### DW-239: Do not initialize a pre-command removal preview as missing audit support
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: TenantRemoveMemberCommandSnapshot.Previewed
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Do not initialize a pre-command removal preview as missing audit support. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Do not initialize a pre-command removal preview as missing audit support. evidence: `TenantRemoveMemberCommandSnapshot.Previewed` assigns `AuditState = MissingSupport` before dispatch or proof lookup, so the preview can report missing support even when the parent has already proven live audit capability.
status: done 2026-08-25
resolution: already resolved: commit 28d32ca8; TenantCreateCommandModels.cs:851-871 initializes Previewed audit state as NotStarted.

### DW-240: Map command-status HTTP timeouts to a support-safe unknown result
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: TenantCommandGateway.GetStatusAsync
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Map command-status HTTP timeouts to a support-safe unknown result. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Map command-status HTTP timeouts to a support-safe unknown result. evidence: `TenantCommandGateway.GetStatusAsync` catches JSON failures but not `TaskCanceledException` from an `HttpClient` timeout, while removal status refresh calls it with `CancellationToken.None`; an operational timeout can therefore escape the UI recovery path.
status: done 2026-08-25
resolution: already resolved: commit 43ef25eb; TenantCommandGateway.cs:491-499 maps operational cancellation and HTTP faults to a support-safe retryable unknown-status result.

### DW-241: Restore focus safely when a successful removal deletes the launcher row
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: MemberAccessReview.OnAfterRenderAsync
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Restore focus safely when a successful removal deletes the launcher row. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Restore focus safely when a successful removal deletes the launcher row. evidence: `MemberAccessReview.OnAfterRenderAsync` focuses a retained row wrapper without verifying that the target still exists or providing a fallback, so a stale `ElementReference` can fault instead of returning focus after the row disappears.
status: open

### DW-242: Make remove-member focus trapping visibility-aware and verify it at the responsive breakpoint
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Make remove-member focus trapping visibility-aware and verify it at the responsive breakpoint. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Make remove-member focus trapping visibility-aware and verify it at the responsive breakpoint. evidence: The narrow-layout CSS hides the confirmation form, but initial focus and the end sentinel can still target controls inside that hidden form; current tests only inspect CSS/source structure and do not exercise computed visibility or an actual keyboard focus cycle.
status: open

### DW-243: Replace legacy Fluent/FAST CSS custom properties in the remove-member dialog
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: RemoveTenantMemberFlow.razor.css
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Replace legacy Fluent/FAST CSS custom properties in the remove-member dialog. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Replace legacy Fluent/FAST CSS custom properties in the remove-member dialog. evidence: `RemoveTenantMemberFlow.razor.css` still uses `--neutral-stroke-rest`, `--error-fill-rest`, and `--focus-stroke-outer`, contrary to the repository's Fluent UI v5 token guidance.
status: open

### DW-244: Align rejected remove-member recovery copy with actions the surface actually provides
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Align rejected remove-member recovery copy with actions the surface actually provides. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Align rejected remove-member recovery copy with actions the surface actually provides. evidence: The EN/FR rejected-state recovery text tells the operator to request permission, but the rejected flow renders no permission-request action or delegate.
status: open

### DW-245: Preserve queued projection-refresh intent in add-member and change-role flows
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Preserve queued projection-refresh intent in add-member and change-role flows. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Preserve queued projection-refresh intent in add-member and change-role flows. evidence: Both sibling flows collapse a projection refresh requested during an in-flight status-only refresh into a follow-up call with `requestProjectionRefresh: false`, so authoritative projection confirmation can remain pending.
status: open

### DW-246: Reconcile story gitlink validation with the seven post-baseline dependency pointer changes
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: Hexalith.Builds
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Reconcile story gitlink validation with the seven post-baseline dependency pointer changes. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Reconcile story gitlink validation with the seven post-baseline dependency pointer changes. evidence: The story validator reports only `Hexalith.AI.Tools`, `Hexalith.Builds`, `Hexalith.Commons`, `Hexalith.EventStore`, `Hexalith.FrontComposer`, `Hexalith.Memories`, and `Hexalith.PolymorphicSerializations`, all introduced after the preserved story baseline and outside this patch.
status: open

### DW-247: No-advancement ProjectionPending has no in-place terminal escape
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: No-advancement ProjectionPending has no in-place terminal escape. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: No-advancement ProjectionPending has no in-place terminal escape. evidence: When the postcondition matches and the command produced events but ordered provenance cannot bind the observed version to the attempt, all three membership snapshots stay ProjectionPending; TenantCommandFlowGuard.RetainsCommandActivity holds the lease and CanContinueReadOnly excludes that state, so no continue-read-only affordance renders. A fix mapping this to UnableToVerify was drafted and reverted during review: the I/O matrix permits "stay pending or unable to verify", and six tests deliberately pin ProjectionPending, making this a product decision. Recoverable today via route change or page disposal, both of which release the lease.
status: open
decision: 2026-08-26 Bound to UnableToVerify — After a configured retry or time bound, transition to UnableToVerify with status recovery and support-safe guidance.
decision: 2026-08-25 Bound to UnableToVerify — After a configured retry or time bound, transition the retained attempt to UnableToVerify with status-recovery and support-safe guidance.

### DW-248: Refresh-coalescing re-enters recursively instead of iterating
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Refresh-coalescing re-enters recursively instead of iterating. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Refresh-coalescing re-enters recursively instead of iterating. evidence: RefreshCommandStatusAsync calls itself after the finally block when a request arrived during the in-flight release window. Under a sustained nudge stream this grows the async frame chain without bound. The dropped-request windows reported by review did not reproduce on inspection. Duplicated verbatim across AddTenantMemberFlow, ChangeTenantMemberRoleFlow and RemoveTenantMemberFlow, so any rewrite should extract a shared helper.
status: open

### DW-249: AttemptStartedAtUtc and hasQualifyingAuditProvenance are dead in production for metadata; the snapshot test pins a branch no production call site can reach
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: AttemptStartedAtUtc and hasQualifyingAuditProvenance are dead in production for metadata; the snapshot test pins a branch no production call site can reach. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: AttemptStartedAtUtc and hasQualifyingAuditProvenance are dead in production for metadata; the snapshot test pins a branch no production call site can reach. evidence: Both ConfirmProjection call sites (EditTenantMetadataFlow.razor:410 and :573) use the two-argument form, so the flag is permanently false; AttemptStartedAtUtc is stamped in RequestSent and never read for metadata. The frozen "version advancement OR audit provenance" rule is satisfied by the version half, so this is dead API surface rather than a violation. Already partially recorded by this story's own deferred entry.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor:775-787 queries attempt-bound audit evidence and passes it into projection confirmation.

### DW-250: source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Nothing proves the tenant-detail read model's ProjectionVersion actually advances for a same-value update, which is the premise the whole confirmation path now rests on
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Nothing proves the tenant-detail read model's ProjectionVersion actually advances for a same-value update, which is the premise the whole confirmation path now rests on. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Nothing proves the tenant-detail read model's ProjectionVersion actually advances for a same-value update, which is the premise the whole confirmation path now rests on. evidence: TenantAggregateTests proves the aggregate always emits TenantUpdated for identical Name+Description, but not that the projection version moves. A projection that deduped or content-hashed would make every same-value "recorded update" fail closed to UnableToVerify. Requires a Server/Integration-tier test, outside this UI slice's test shape.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:121-127 stamps every applied event sequence; TenantProjectionHandlerTests.cs:333-372 proves TenantUpdated advances to tenant-sequence:12.

### DW-251: Hard-coded English strings remain on paths this story made localizable
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Hard-coded English strings remain on paths this story made localizable. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Hard-coded English strings remain on paths this story made localizable. evidence: TenantCreateCommandModels.cs:1165 default ApplyStatus arm ("Command status could not be verified.") and TenantCommandGateway.cs validation literal. Both verified as pre-existing context lines in the diff, not introduced by this story.
status: open

### DW-252: EditTenantMetadataFlow.ApplyProjectionEvidence and ApplySignalRNudge have no callers; the story threaded a projection version into a dead entry point
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: EditTenantMetadataFlow.ApplyProjectionEvidence and ApplySignalRNudge have no callers; the story threaded a projection version into a dead entry point. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: EditTenantMetadataFlow.ApplyProjectionEvidence and ApplySignalRNudge have no callers; the story threaded a projection version into a dead entry point. evidence: grep over src/ finds only the declarations; TenantDetailPage holds an @ref to _memberAccessReview only and nudges only that component. Consequence: SignalR nudges never reach the metadata flow today.
status: open

### DW-253: New defensive branches are covered only by reflection-poking private fields, so they will break silently on rename and do not exercise the real gateway path
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: New defensive branches are covered only by reflection-poking private fields, so they will break silently on rename and do not exercise the real gateway path. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: New defensive branches are covered only by reflection-poking private fields, so they will break silently on rename and do not exercise the real gateway path. evidence: EditTenantMetadataFlowTests.cs sets the private _snapshot field and invokes private RefreshStatusAsync to build an Accepted snapshot with null tracking ids. The only realistic production route to that state is a gateway returning Accepted with a blank CorrelationId, which no test drives.
status: open

### DW-254: Optional messageId is now inconsistent across ITenantCommandGateway
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Optional messageId is now inconsistent across ITenantCommandGateway. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Optional messageId is now inconsistent across ITenantCommandGateway. evidence: Create, Add, Change, Remove and Update carry it; SetTenantConfigurationAsync, RemoveTenantConfigurationAsync, SetGlobalAdministratorAsync, RemoveGlobalAdministratorAsync, EnableTenantAsync and DisableTenantAsync do not. The reconnect/idempotency contract is therefore partial. Hexalith.Tenants.UI is not a published package, so there is no external consumer break.
status: open
decision: 2026-08-26 Unify optional identity — Add optional messageId to every command method and update implementations, flows, and tests consistently.
decision: 2026-08-25 Unify optional identity — Add optional messageId to every command method and update implementations, flows, and tests with consistent retry semantics.

### DW-255: `AttemptStartedAtUtc` ships on the public `TenantCreateCommandSnapshot` record but is never read, and defaults via `DateTimeOffset.UtcNow` instead of an injected clock
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-21)"), 2026-08-25
location: AttemptStartedAtUtc
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: `AttemptStartedAtUtc` ships on the public `TenantCreateCommandSnapshot` record but is never read, and defaults via `DateTimeOffset.UtcNow` instead of an injected clock. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: `AttemptStartedAtUtc` ships on the public `TenantCreateCommandSnapshot` record but is never read, and defaults via `DateTimeOffset.UtcNow` instead of an injected clock. evidence: The audit-provenance branch it feeds (`HasQualifyingAuditProvenance`) is never called for create and is already recorded as deferred work from the 2026-08-08 review; removing or wiring the field belongs with that slice.
status: open
decision: 2026-08-27 Wire injected timing — Inject a clock, stamp create attempts deterministically, and consume AttemptStartedAtUtc in bounded retained-attempt or provenance behavior while preserving the public member.
decision: 2026-08-27 Wire injected timing — Inject a clock, stamp create attempts deterministically, and consume AttemptStartedAtUtc in bounded retained-attempt or provenance behavior while preserving the public member.

### DW-256: `CreateTenantFlow.ApplyProjectionEvidence` has no callers in `src/` or `tests/` and bypasses `SetSnapshot`, so it would not honour the assertive-focus rule if wired
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-21)"), 2026-08-25
location: src/; tests/
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: `CreateTenantFlow.ApplyProjectionEvidence` has no callers in `src/` or `tests/` and bypasses `SetSnapshot`, so it would not honour the assertive-focus rule if wired. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: `CreateTenantFlow.ApplyProjectionEvidence` has no callers in `src/` or `tests/` and bypasses `SetSnapshot`, so it would not honour the assertive-focus rule if wired. evidence: Its signature was updated for the tuple change, but the SignalR nudge wiring that would call it is itself deferred; fixing the seam in isolation has no observable effect.
status: open

### DW-257: Baseline and evidence projection versions are read from different snapshot lineages -- baseline from `_snapshot.ProjectionVersion`, evidence from `_lastConfirmedSnapshot ?? _snapshot` -- so a failed post-create reload makes a genuinely successful create report `UnableToVerify`
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-21)"), 2026-08-25
location: _snapshot.ProjectionVersion
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: Baseline and evidence projection versions are read from different snapshot lineages -- baseline from `_snapshot.ProjectionVersion`, evidence from `_lastConfirmedSnapshot ?? Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: Baseline and evidence projection versions are read from different snapshot lineages -- baseline from `_snapshot.ProjectionVersion`, evidence from `_lastConfirmedSnapshot ?? _snapshot` -- so a failed post-create reload makes a genuinely successful create report `UnableToVerify`. evidence: Fail-closed direction (false negative, not false confirm) and entangled with the open provenance-gate decision; resolving that decision determines whether this seam changes at all.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:359-363 captures and compares list and detail baselines like-for-like.

### DW-258: `TenantsWorkspace.IsCommandSurfaceConnected` is a render-time `Services.GetService` lookup with no subscription, duplicating an existing resolution in the same component and using the non-generic overload
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-21)"), 2026-08-25
location: TenantsWorkspace.IsCommandSurfaceConnected
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: `TenantsWorkspace.IsCommandSurfaceConnected` is a render-time `Services.GetService` lookup with no subscription, duplicating an existing resolution in the same component and using the non-generic overload. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: `TenantsWorkspace.IsCommandSurfaceConnected` is a render-time `Services.GetService` lookup with no subscription, duplicating an existing resolution in the same component and using the non-generic overload. evidence: Pre-existing composition pattern; the no-subscription half is already recorded in this ledger from the 2026-08-08 review. Story 3.1 added the workspace-side call site but not the pattern.
status: open

### DW-259: `ApplyProjectionEvidence` is dead code across all eight command flows
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-21)"), 2026-08-25
location: src/; tests/
reason: The legacy ledger defers this issue: `ApplyProjectionEvidence` is dead code across all eight command flows. Original context is preserved in legacy-detail.
legacy-detail: - **`ApplyProjectionEvidence` is dead code across all eight command flows.** A repo-wide search finds eight `internal void ApplyProjectionEvidence` declarations in `src/` and zero invocations in `src/` or `tests/`. Loop 2 rewrote the remove-member copy (`RemoveTenantMemberFlow.razor:628`) to fire a discarded `InvokeAsync` that calls `TryAssembleRemovalProofAsync` and `UpdateCommandActivityForSnapshotAsync` without `StateHasChanged` and outside any try/catch. The live proof path is `HandleAuthoritativeRefreshNudgeAsync` → `TryAssembleRemovalProofAsync` (`:902`), reached from `MemberAccessReview.razor:804`, so WP-2A still works — the rewritten method is simply unreachable. Pre-existing pattern, spans seven files outside story 2.4.
status: open

### DW-260: `CreateTenantFlow` never adopts the reusable `messageId` affordance this story added
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-21)"), 2026-08-25
location: TenantCommandGateway.cs:42,69; CreateTenantFlow.razor:348
reason: The legacy ledger defers this issue: `CreateTenantFlow` never adopts the reusable `messageId` affordance this story added. Original context is preserved in legacy-detail.
legacy-detail: - **`CreateTenantFlow` never adopts the reusable `messageId` affordance this story added.** `TenantCommandGateway.CreateTenantAsync` gained `string? messageId = null` and now returns `MessageId` on indeterminate failure (`TenantCommandGateway.cs:42,69`), but `CreateTenantFlow.razor:348` hard-codes `messageId: null` and `:359-368` discards `result.MessageId`. Its own tracking guard at `:307-319` therefore never engages, and a retry after an ambiguous 503 mints a fresh ULID — surfacing `TenantAlreadyExistsRejection` for a tenant the operator just created. Belongs to story `3-1-create-tenant-with-projection-confirmation` (currently `review`).
status: done 2026-08-27
resolution: already resolved: commit 24c978d4; src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:405-418 reuses the snapshot MessageId and retains accepted tracking.

### DW-261: Missing test coverage for three branches this story introduced
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-21)"), 2026-08-25
location: TenantCommandGatewayTests.cs:1137-1184; tests/
reason: The legacy ledger defers this issue: Missing test coverage for three branches this story introduced. Original context is preserved in legacy-detail.
legacy-detail: - **Missing test coverage for three branches this story introduced.** No test asserts `CreateTenantAsync`/`UpdateTenantAsync` retain the minted ULID on indeterminate failure (the three membership equivalents exist at `TenantCommandGatewayTests.cs:1137-1184`); no test hands any flow a denying `CommandActivityLease` (every stub returns `Task.FromResult(true)`, and `FromResult(false)` appears nowhere in `tests/`), so the pre-dispatch lease guard at `RemoveTenantMemberFlow.razor:742-750` can be deleted with the suite still green; and no test observes the tracking-lost submit branch at `:669-690`. Test files are chunk C of this review.
status: open

### DW-262: Legacy FAST token `--neutral-stroke-rest` survives in `RemoveTenantMemberFlow.razor.css:4`
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-21)"), 2026-08-25
location: RemoveTenantMemberFlow.razor.css:4; project-context.md
reason: The legacy ledger defers this issue: Legacy FAST token `--neutral-stroke-rest` survives in `RemoveTenantMemberFlow.razor.css:4`. Original context is preserved in legacy-detail.
legacy-detail: - **Legacy FAST token `--neutral-stroke-rest` survives in `RemoveTenantMemberFlow.razor.css:4`.** `project-context.md` bans `--neutral-*` outright, yet `DomainUiFluentConformanceTests` passes — the guard does not cover custom-property names. Noted rather than patched because this story's diff moved *off* a banned token (`--accent-fill-rest` → `--error-fill-rest` at `.css:36`), i.e. it improved the file; the residual token and the guard gap are pre-existing.
status: open

### DW-263: `RemoveTenantMemberFlow.Dispose` does not release the command-activity lease — attempted and reverted
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-21)"), 2026-08-25
location: *Flow.razor; MemberAccessReview.razor:734-738
reason: The legacy ledger defers this issue: `RemoveTenantMemberFlow.Dispose` does not release the command-activity lease — attempted and reverted. Original context is preserved in legacy-detail.
legacy-detail: - **`RemoveTenantMemberFlow.Dispose` does not release the command-activity lease — attempted and reverted.** An unmount path other than `CloseAsync` leaves `_hasRaisedCommandActivity` true, so the parent's `_childCommandLeaseOwner` and the page's aggregate key stay held. Releasing from the flow was implemented and then reverted: `CommandFlowGuardConformanceTests.Command_flows_do_not_release_page_activity_directly` forbids any `*Flow.razor` from calling `OnCommandActivityChanged.InvokeAsync(false)`, because a flow that self-releases while still Accepted/ProjectionPending would unlock sibling command surfaces before terminal evidence. The parent is the designated owner and already compensates in `MemberAccessReview.DisposeAsync` and on the authorization-teardown path (`MemberAccessReview.razor:734-738`). Any residual gap is a parent-side concern and should be closed there, not in the flow.
status: done 2026-08-28
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:720-744 releases command activity after terminal flow teardown and lines 1043-1048 release it during disposal.

### DW-264: Create availability derives `IsAuthorized` from the tenant-list surface kind rather than `ITenantsBffComposition.GlobalAdministratorsAuthorizationReflection`, so an `Indeterminate` authorization reflection still leaves create enabled -- against the Always fail-closed clause
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-21)"), 2026-08-25
location: IsAuthorized
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: Create availability derives `IsAuthorized` from the tenant-list surface kind rather than `ITenantsBffComposition.GlobalAdministratorsAuthorizationReflection`, so an `Indeterminate` authorization reflection still leaves create enabled -- against the Always fail-closed clause. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: Create availability derives `IsAuthorized` from the tenant-list surface kind rather than `ITenantsBffComposition.GlobalAdministratorsAuthorizationReflection`, so an `Indeterminate` authorization reflection still leaves create enabled -- against the Always fail-closed clause. evidence: Deferred to Story 3.3 by code-review decision D5 (2026-08-21). Story 3.3 is scoped exactly as the fail-closed availability guardrail for lifecycle and configuration; server/API/domain authorization remains the enforcement boundary, so this is UI honesty rather than a security hole. Story 3.3 must cover create availability, not only lifecycle and configuration.
status: open

### DW-265: Refresh coalescing downgrades a user-initiated projection refresh to a status-only refresh and re-enters recursively
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: AddTenantMemberFlow.razor:513-541; ChangeTenantMemberRoleFlow.razor:572-600
reason: The legacy ledger defers this issue: Refresh coalescing downgrades a user-initiated projection refresh to a status-only refresh and re-enters recursively. Original context is preserved in legacy-detail.
legacy-detail: - summary: Refresh coalescing downgrades a user-initiated projection refresh to a status-only refresh and re-enters recursively. evidence: `AddTenantMemberFlow.razor:513-541` and the verbatim duplicate at `ChangeTenantMemberRoleFlow.razor:572-600` hard-code `requestProjectionRefresh: false` on every replay, so a Refresh pressed during an in-flight nudge never re-reads the projection. The post-`finally` tail re-enters the same method rather than looping. Already recorded from the story 2.1 review; a shared helper should be extracted rather than fixing three copies.
status: open

### DW-266: `AsyncLocal<bool>` is the wrong primitive for dispatcher-bound re-entrancy state
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: AddTenantMemberFlow.razor:160; ChangeTenantMemberRoleFlow.razor:178
reason: The legacy ledger defers this issue: `AsyncLocal<bool>` is the wrong primitive for dispatcher-bound re-entrancy state. Original context is preserved in legacy-detail.
legacy-detail: - summary: `AsyncLocal<bool>` is the wrong primitive for dispatcher-bound re-entrancy state. evidence: `AddTenantMemberFlow.razor:160`, `ChangeTenantMemberRoleFlow.razor:178`. Blazor components already run serialized on the renderer dispatcher, so a plain field is correct and avoids an ExecutionContext copy-on-write per set; the AsyncLocal also fails to flow into callbacks invoked from a context it was not captured on.
status: open

### DW-267: Coalescer, submit guard, lease plumbing and `SafeMessageText` are copy-pasted across flow components
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: AddTenantMemberFlow.razor:397-416; ChangeTenantMemberRoleFlow.razor:452-471
reason: The legacy ledger defers this issue: Coalescer, submit guard, lease plumbing and `SafeMessageText` are copy-pasted across flow components. Original context is preserved in legacy-detail.
legacy-detail: - summary: Coalescer, submit guard, lease plumbing and `SafeMessageText` are copy-pasted across flow components. evidence: `AddTenantMemberFlow.razor:397-416` is byte-identical to `ChangeTenantMemberRoleFlow.razor:452-471` including its six-line comment; `SetCommandActivityRaisedAsync` duplicated at `:315-341`/`:374-400`; `SafeMessageText` duplicated at `CreateTenantFlow.razor:213-218` and `EditTenantMetadataFlow.razor:301-306`.
status: open

### DW-268: The scoped-CSS-on-a-Fluent-host trap predates this change in five other components
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: UserMembershipLookupPanel.razor.css; TenantAuditPage.razor.css
reason: The legacy ledger defers this issue: The scoped-CSS-on-a-Fluent-host trap predates this change in five other components. Original context is preserved in legacy-detail.
legacy-detail: - summary: The scoped-CSS-on-a-Fluent-host trap predates this change in five other components. evidence: Plain scoped selectors are applied to classes placed on Fluent components in `UserMembershipLookupPanel.razor.css`, `TenantAuditPage.razor.css`, `GlobalAdministratorsPage.razor.css`, `AuditEvidenceReceipt.razor.css` and `AuditEvidenceEntryPoint.razor.css`. Per Microsoft's CSS-isolation contract, scoped CSS applies to HTML elements only, so these selectors cannot match. Only the four `TenantConfigurationView` wrappers are a regression introduced by this story.
status: open

### DW-269: Inserting `Available` mid-enum shifts `MissingSupport`'s numeric value
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantAuditAvailability.cs:5-11
reason: The legacy ledger defers this issue: Inserting `Available` mid-enum shifts `MissingSupport`'s numeric value. Original context is preserved in legacy-detail.
legacy-detail: - summary: Inserting `Available` mid-enum shifts `MissingSupport`'s numeric value. evidence: `TenantAuditAvailability.cs:5-11`. Harmless today (no numeric persistence or interop), but it makes the enum unsafe to serialize by value later.
status: open

### DW-270: French resource additions are inconsistently accented against their neighbours
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantsResources.fr.resx:3141
reason: The legacy ledger defers this issue: French resource additions are inconsistently accented against their neighbours. Original context is preserved in legacy-detail.
legacy-detail: - summary: French resource additions are inconsistently accented against their neighbours. evidence: `TenantsResources.fr.resx:3141` is fully accented while `:3138` and `:3147` are deliberately unaccented; the same split appears at `:2562-2567` versus `:2134`. The same screen can render both conventions.
status: open

### DW-271: `TenantAggregateCommandAdmissionGate`'s public API changed shape without an obsolete overload
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantDetailPage.razor:1689-1703
reason: The legacy ledger defers this issue: `TenantAggregateCommandAdmissionGate`'s public API changed shape without an obsolete overload. Original context is preserved in legacy-detail.
legacy-detail: - summary: `TenantAggregateCommandAdmissionGate`'s public API changed shape without an obsolete overload. evidence: `:26-46` — same-owner `TryAcquire` now returns `false`, forcing every caller to keep its own bookkeeping (which `TenantDetailPage.razor:1689-1703` reimplements); `Release` at `:55-70` silently no-ops on owner mismatch with no return value, so a leaked lock is undetectable. The `<returns>` doc never mentions the same-owner case.
status: done 2026-08-28
resolution: already resolved: src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs:22-50 and TenantAggregateCommandLease.cs:36-62 provide owner-aware lease/result APIs while preserving compatible legacy acquisition.
decision: 2026-08-26 Add compatible outcomes — Add explicit owner-aware result APIs while retaining current signatures as compatibility wrappers.

### DW-272: The audit-capability probe has no reconnect subscription, and every read refresh briefly blocks removal
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantDetailPage.razor:823; MemberAccessReview.razor:611-613
reason: The legacy ledger defers this issue: The audit-capability probe has no reconnect subscription, and every read refresh briefly blocks removal. Original context is preserved in legacy-detail.
legacy-detail: - summary: The audit-capability probe has no reconnect subscription, and every read refresh briefly blocks removal. evidence: `TenantDetailPage.razor:823` clears `_auditProofCapabilityAvailable` before restarting the probe, and `MemberAccessReview.razor:611-613` turns that into `UnavailableReason.MissingAuditProof`, so every refresh (including SignalR-nudged ones) flips Remove to unavailable with a misleading reason until the extra round trip lands. A `BffComposition` reconnect never re-probes.
status: open

### DW-273: `messageId` remains absent from six `ITenantCommandGateway` methods
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: ITenantCommandGateway.cs:32,36,41,46,51,56
reason: The legacy ledger defers this issue: `messageId` remains absent from six `ITenantCommandGateway` methods. Original context is preserved in legacy-detail.
legacy-detail: - summary: `messageId` remains absent from six `ITenantCommandGateway` methods. evidence: `ITenantCommandGateway.cs:32,36,41,46,51,56` — configuration set/remove, global-administrator set/remove, and tenant enable/disable have no way to reuse a tracking id, so the duplicate-dispatch hazard this change closed for five commands stays open for six. Already recorded from the story 3.2 review.
status: done 2026-08-25
resolution: closed by human decision: Treat DW-254 as the canonical decision and close this duplicate without a separate change.
decision: 2026-08-25 Close as duplicate — Treat DW-254 as the canonical decision and close this duplicate without a separate change.

### DW-274: `TenantQueryGateway` dereferences `Detail!` inside the catch that exists to fail safe
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantQueryGateway
reason: The legacy ledger defers this issue: `TenantQueryGateway` dereferences `Detail!` inside the catch that exists to fail safe. Original context is preserved in legacy-detail.
legacy-detail: - summary: `TenantQueryGateway` dereferences `Detail!` inside the catch that exists to fail safe. evidence: `:2131-2152` — if reauthorization throws in the retention helper, the null-forgiving `SanitizeDetail(previous!.Detail!)` can throw from within the safety path.
status: done 2026-08-26
resolution: already resolved: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:2134-2135,2178-2182 proves the retained detail is non-null and tenant-bound before the guarded catch can access it.

### DW-275: `HasSameTenantDetail` newly compares `ConfigurationManagement.TenantId`
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: HasSameTenantDetail
reason: The legacy ledger defers this issue: `HasSameTenantDetail` newly compares `ConfigurationManagement.TenantId`. Original context is preserved in legacy-detail.
legacy-detail: - summary: `HasSameTenantDetail` newly compares `ConfigurationManagement.TenantId`. evidence: `:2166-2170` — a default-constructed `ConfigurationManagement` with a mismatched `TenantId` now makes the comparison false, so retention paths degrade instead of retaining.
status: done 2026-09-06
resolution: already resolved: src/Hexalith.Tenants.UI/State/TenantDetail/TenantDetailSnapshot.cs:179-196 constructs sanitized detail and configuration management context atomically; the former HasSameTenantDetail comparison no longer exists.

### DW-276: `_commandInFlight` is handled inconsistently across the two lease-refusal paths in one method
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantDetailPage.razor:1681-1686
reason: The legacy ledger defers this issue: `_commandInFlight` is handled inconsistently across the two lease-refusal paths in one method. Original context is preserved in legacy-detail.
legacy-detail: - summary: `_commandInFlight` is handled inconsistently across the two lease-refusal paths in one method. evidence: `TenantDetailPage.razor:1681-1686` returns `false` leaving a stale `true`; `:1705-1709` explicitly clears it first. The removed code carried a comment explaining the no-lockable-identity path; it was dropped rather than preserved or refuted.
status: open

### DW-277: A `TenantId` change does not notify non-keyed command surfaces that their lease was revoked
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantDetailPage.razor:406-418
reason: The legacy ledger defers this issue: A `TenantId` change does not notify non-keyed command surfaces that their lease was revoked. Original context is preserved in legacy-detail.
legacy-detail: - summary: A `TenantId` change does not notify non-keyed command surfaces that their lease was revoked. evidence: `TenantDetailPage.razor:406-418` releases the old aggregate key on route change, but metadata, lifecycle and configuration flows keep `_hasRaisedCommandActivity` true with no lease behind it.
status: done 2026-08-28
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:149,192 keys child surfaces by tenant, and lifecycle/configuration flows reset raised activity on tenant changes at TenantLifecycleCommandFlow.razor:480-490 and sibling flow equivalents.

### DW-278: The global-administrator aggregation loop uses `ContainsKey`+`Add` and silently drops duplicate or null `UserId` rows
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantDetailPage.razor:1435-1442
reason: The legacy ledger defers this issue: The global-administrator aggregation loop uses `ContainsKey`+`Add` and silently drops duplicate or null `UserId` rows. Original context is preserved in legacy-detail.
legacy-detail: - summary: The global-administrator aggregation loop uses `ContainsKey`+`Add` and silently drops duplicate or null `UserId` rows. evidence: `TenantDetailPage.razor:1435-1442`. `TryAdd` does one lookup; a null `UserId` would throw into the fail-closed catch rather than being handled explicitly.
status: done 2026-08-28
resolution: already resolved: commit ba060a2f; src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs:63-68 aggregates with TryAdd and lines 130-135 reject null, blank, and control-character identities.

### DW-279: Add and change-role `retryMessageId` exclude the `Rejected` state
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: AddTenantMemberFlow.razor:441-447; ChangeTenantMemberRoleFlow.razor:499-505
reason: The legacy ledger defers this issue: Add and change-role `retryMessageId` exclude the `Rejected` state. Original context is preserved in legacy-detail.
legacy-detail: - summary: Add and change-role `retryMessageId` exclude the `Rejected` state. evidence: `AddTenantMemberFlow.razor:441-447`, `ChangeTenantMemberRoleFlow.razor:499-505` reuse the id only when `State is Failed`, so a `Rejected` attempt for the same intent re-dispatches under a fresh ULID.
status: done 2026-09-06
resolution: closed by human decision: Treat rejection as conclusive and retain the current behavior in which a later deliberate submission mints a new identity.
decision: 2026-09-06 New attempt after rejection — Treat rejection as conclusive and retain the current behavior in which a later deliberate submission mints a new identity.

### DW-280: `MemberAccessReview` sets child lease ownership after the await
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: MemberAccessReview
reason: The legacy ledger defers this issue: `MemberAccessReview` sets child lease ownership after the await. Original context is preserved in legacy-detail.
legacy-detail: - summary: `MemberAccessReview` sets child lease ownership after the await. evidence: `:754-780` — `_childCommandLeaseOwner` is assigned only after `await CommandActivityLease(isActive)` returns, so two concurrent membership callers can both pass the `is not null` pre-check and both be granted; the first release then frees an aggregate whose other command is still in flight.
status: done 2026-08-25
resolution: already resolved: commit 28d32ca8; src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:768-770 reserves the child lease owner before awaiting admission.

### DW-281: `CreateTenantFlow` and `TenantsWorkspace` findings were raised against files a peer session rewrote mid-review
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: CreateTenantFlow.razor; TenantsWorkspace.razor
reason: The legacy ledger defers this issue: `CreateTenantFlow` and `TenantsWorkspace` findings were raised against files a peer session rewrote mid-review. Original context is preserved in legacy-detail.
legacy-detail: - summary: `CreateTenantFlow` and `TenantsWorkspace` findings were raised against files a peer session rewrote mid-review. evidence: A concurrent session working story 3.1 changed `CreateTenantFlow.razor` by +152/-50 and `TenantsWorkspace.razor` by +17/-5 during this review, and added `TenantCreateAttemptTracker.cs`. The raised items — fail-open absence baseline at `:435-438`, empty-string tracking ids blocking submit, a transient refresh fault downgrading a confirmed create to `UnableToVerify`, a fabricated `(null, null)` evidence tuple, and `TenantsWorkspace` asserting tenant absence from a stale empty list — must be re-reviewed against the peer's version and belong to story `3-1-create-tenant-with-projection-confirmation`.
status: done 2026-08-25
resolution: already resolved: commits 753f1ead and 24c978d4 completed the current Story 3.1 re-review and fail-closed create attempt tracking.

### DW-282: `TenantsWorkspace` resolves `ITenantsBffComposition` per render and duplicates its own absence predicate
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantsWorkspace
reason: The legacy ledger defers this issue: `TenantsWorkspace` resolves `ITenantsBffComposition` per render and duplicates its own absence predicate. Original context is preserved in legacy-detail.
legacy-detail: - summary: `TenantsWorkspace` resolves `ITenantsBffComposition` per render and duplicates its own absence predicate. evidence: `:418-420` uses the untyped `Services.GetService(typeof(...))` inside a per-render property with no caching, where `TenantDetailPage` caches the equivalent in a field; the `Empty && IsAuthorizationScopedEmpty` predicate appears at both `:413-414` and `:159` and must not be allowed to drift.
status: open

### DW-283: An eighth undeclared `references/` pointer move appeared during this review
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: references/Hexalith.EventStore; references/
reason: The legacy ledger defers this issue: An eighth undeclared `references/` pointer move appeared during this review. Original context is preserved in legacy-detail.
legacy-detail: - summary: An eighth undeclared `references/` pointer move appeared during this review. evidence: `references/Hexalith.EventStore` moved `c890235` -> `f8b514f` in the working tree while the review was running, on top of the seven `validate-story-gitlinks.py` already reports. Extends the open chunk-A gitlink decision rather than forming a new one.
status: done 2026-08-27
resolution: already resolved: commit d7329f2a; the EventStore pointer is now tracked at 2ae587024ec7dd7dfaca174bf22aa8d74b7a8dc1 and the working tree contains no undeclared pointer move.

### DW-284: Exercise the production create-attempt tracker across a real component remount
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: Remember
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Exercise the production create-attempt tracker across a real component remount. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Exercise the production create-attempt tracker across a real component remount. evidence: Existing remount coverage pre-seeds a tracker manually and does not prove the scoped registration plus production `Remember` call preserve the original intent and baseline from the first dispatched flow.
status: open

### DW-285: Guard create and membership submit flows against re-entrant snapshot replacement while a dispatch is awaiting completion
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: CreateTenantFlow
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Guard create and membership submit flows against re-entrant snapshot replacement while a dispatch is awaiting completion. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Guard create and membership submit flows against re-entrant snapshot replacement while a dispatch is awaiting completion. evidence: `CreateTenantFlow`, `AddTenantMemberFlow`, and `ChangeTenantMemberRoleFlow` can enter their unavailable branches while `_isSubmitting`, replacing the active request snapshot before the original gateway continuation applies its result.
status: open

### DW-286: Require current authoritative projection state for create and membership command confirmation evidence
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Require current authoritative projection state for create and membership command confirmation evidence. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Require current authoritative projection state for create and membership command confirmation evidence. evidence: Review found create/list and membership/detail evidence providers that can forward stale, degraded, or non-current-lifecycle payloads to confirmers that cannot recover the discarded freshness metadata.
status: open

### DW-287: Retain aggregate command admission across tenant route changes and disposal until the old command is terminal
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantDetailPage
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Retain aggregate command admission across tenant route changes and disposal until the old command is terminal. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Retain aggregate command admission across tenant route changes and disposal until the old command is terminal. evidence: `TenantDetailPage` can release the old aggregate lease while command and status operations continue with `CancellationToken.None`, allowing another surface to acquire the same tenant and dispatch concurrently.
status: open

### DW-288: Preserve the resolved create-command message ID after an indeterminate submission so an exact retry cannot mint a duplicate identity
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: CreateTenantFlow
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Preserve the resolved create-command message ID after an indeterminate submission so an exact retry cannot mint a duplicate identity. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Preserve the resolved create-command message ID after an indeterminate submission so an exact retry cannot mint a duplicate identity. evidence: `CreateTenantFlow` does not adopt `TenantCommandSubmissionResult.MessageId` on its non-accepted branch, so a failed result that may already have reached EventStore is retried with a null ID even though the gateway returned the reusable identity.
status: open

### DW-289: Define causal projection-change handling for valid opaque or content-hash version tokens across command flows
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: IReadModelFreshness.ProjectionVersion
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Define causal projection-change handling for valid opaque or content-hash version tokens across command flows. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Define causal projection-change handling for valid opaque or content-hash version tokens across command flows. evidence: `IReadModelFreshness.ProjectionVersion` explicitly permits opaque content hashes, while the shared causal helper accepts only matching prefixes with increasing numeric suffixes; valid changed tokens therefore fail closed across create and membership consumers.
status: done 2026-08-25
resolution: already resolved: src/Hexalith.Tenants/Projections/TenantProjectionVersionFormat.cs:15-18 defines tenant-sequence and TenantProjectionHandler.cs:121-127 stamps incoming event sequence numbers.

### DW-290: Preserve a queued manual projection refresh when add-member or change-role reconciliation is already processing a status-only nudge
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Preserve a queued manual projection refresh when add-member or change-role reconciliation is already processing a status-only nudge. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Preserve a queued manual projection refresh when add-member or change-role reconciliation is already processing a status-only nudge. evidence: Both membership flows return from an in-flight refresh without recording that the later caller requested projection reload, so the user-requested refresh can be dropped and confirmation delayed indefinitely.
status: open

### DW-291: Reconcile Epic 3 tracker state so an epic and retrospective are not marked done while Story 3.2 is in review and Stories 3.3 through 3.6 remain backlog
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: sprint-status.yaml
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Reconcile Epic 3 tracker state so an epic and retrospective are not marked done while Story 3.2 is in review and Stories 3.3 through 3.6 remain backlog. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Reconcile Epic 3 tracker state so an epic and retrospective are not marked done while Story 3.2 is in review and Stories 3.3 through 3.6 remain backlog. evidence: `sprint-status.yaml` currently reports `epic-3: done` and `epic-3-retrospective: done` alongside unfinished Epic 3 story entries, so aggregate status is internally contradictory.
status: done 2026-08-28
resolution: already resolved: _bmad-output/implementation-artifacts/sprint-status.yaml:98-118 now marks Epic 3, Stories 3.2 through 3.6, and the Epic 3 retrospective done consistently.

### DW-292: Add executable in-repository verification for the TEA enforcement hook assets included in the reviewed range
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof — loop 3 final verification (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Add executable in-repository verification for the TEA enforcement hook assets included in the reviewed range. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Add executable in-repository verification for the TEA enforcement hook assets included in the reviewed range. evidence: The mirrored `tea-enforce.cjs` assets advertise focused-test and registry-completeness enforcement, but no repository test imports the scanner or executes its pre/post/stop modes, so a disabled rule or an always-successful entry point would remain green.
status: open

### DW-293: Blank `CommandSurfaceUnavailableReason` maps to `UnavailableReason.AggregateLocked`'s copy, which asserts a specific ("another command is already in progress") cause that may not be true
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22)"), 2026-08-25
location: MemberAccessReview.razor:660
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: Blank `CommandSurfaceUnavailableReason` maps to `UnavailableReason.AggregateLocked`'s copy, which asserts a specific ("another command is already in progress") cause that may not be true. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: Blank `CommandSurfaceUnavailableReason` maps to `UnavailableReason.AggregateLocked`'s copy, which asserts a specific ("another command is already in progress") cause that may not be true. evidence: `ResolveFailClosedReasons` (`MemberAccessReview.razor:660`) returns `AggregateLocked` whenever `!IsCommandSurfaceAvailable`, even with an empty reason string (e.g. a missing admission-gate registration with no actual contention); there is no generic "support unavailable" `UnavailableReason` value. Fail-closed behavior is correct; only the specific wording is inaccurate. Needs a dedicated wording/UX pass (new enum value + EN/FR copy) rather than a fold-in fix.
status: open

### DW-294: Self-lock reason text — a row whose own flow currently holds `_childCommandLeaseOwner` also renders its own launcher buttons as `AggregateLocked` ("another command is already in progress"), which is imprecise for its own open flow
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22, pass 2)"), 2026-08-25
location: MemberAccessReview.razor:663
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: Self-lock reason text — a row whose own flow currently holds `_childCommandLeaseOwner` also renders its own launcher buttons as `AggregateLocked` ("another command is already in progress"), which is imprecise for its own open flow. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: Self-lock reason text — a row whose own flow currently holds `_childCommandLeaseOwner` also renders its own launcher buttons as `AggregateLocked` ("another command is already in progress"), which is imprecise for its own open flow. evidence: `ResolveFailClosedReasons` (`MemberAccessReview.razor:663`) checks `_childCommandLeaseOwner is not null` unconditionally for every row, including the row whose own flow holds the lease; not newly introduced by this diff (the row was already gated unavailable via `IsCommandSurfaceAvailable` once any child flow raises the parent lease) — same class of issue as the item above; fold into that dedicated wording/UX pass rather than treating separately.
status: open

### DW-295: New `EventId(2001, "TenantProjectionNullEventSkipped")` is an unregistered magic literal with no cross-project collision check
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22, pass 2)"), 2026-08-25
location: TenantProjectionHandler.cs:32
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: New `EventId(2001, "TenantProjectionNullEventSkipped")` is an unregistered magic literal with no cross-project collision check. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: New `EventId(2001, "TenantProjectionNullEventSkipped")` is an unregistered magic literal with no cross-project collision check. evidence: `TenantProjectionHandler.cs:32` defines the EventId inline; no EventId registry exists in this codebase to conform to, so establishing one is out of scope for this fix.
status: done 2026-08-25
resolution: closed by human decision: Retain the handler-local named EventId because no repository registry contract exists.
decision: 2026-08-25 Accept local named ID — Retain the handler-local named EventId because no repository registry contract exists.

### DW-296: New `TenantProjectionVersionFormat` type deliberately sits outside the namespaces `EventContractReferenceDocumentationTests` sweeps, setting a precedent for future non-contract public types to bypass the assembly's only doc-completeness governance check
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22, pass 2)"), 2026-08-25
location: src/Hexalith.Tenants.Contracts/Projections/TenantProjectionVersionFormat.cs
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: New `TenantProjectionVersionFormat` type deliberately sits outside the namespaces `EventContractReferenceDocumentationTests` sweeps, setting a precedent for future non-contract public types to bypass the assembly's only doc-completeness governance check. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: New `TenantProjectionVersionFormat` type deliberately sits outside the namespaces `EventContractReferenceDocumentationTests` sweeps, setting a precedent for future non-contract public types to bypass the assembly's only doc-completeness governance check. evidence: `src/Hexalith.Tenants.Contracts/Projections/TenantProjectionVersionFormat.cs`'s own XML remarks document the intentional namespace choice; `EventContractReferenceDocumentationTests.PublicContractTypes()` only sweeps namespaces ending in `.Commands`/`.Events`/`.Events.Rejections`/`.Queries`/`.Enums`. Worth a broader governance-scope discussion, not a defect in this diff.
status: done 2026-08-25
resolution: closed by human decision: Retain the current wire-contract namespace boundary because this non-wire type is already documented.
decision: 2026-08-25 Accept namespace scope — Retain the current wire-contract namespace boundary because this non-wire type is already documented.

### DW-297: The Release-build `.slnx` topology revert is validated only in review-findings prose, not captured as a reproducible command in the spec's formal Verification section
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22, pass 2)"), 2026-08-25
location: dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore; spec-2-4b-wp-2a-removal-proof-and-audit-available.md:123
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: The Release-build `.slnx` topology revert is validated only in review-findings prose, not captured as a reproducible command in the spec's formal Verification section. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: The Release-build `.slnx` topology revert is validated only in review-findings prose, not captured as a reproducible command in the spec's formal Verification section. evidence: `dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore` (0 Warning(s), 0 Error(s)) is recorded only inside a Review Findings bullet; `## Verification` at `spec-2-4b-wp-2a-removal-proof-and-audit-available.md:123` lists only a filtered `dotnet test` command, so a reader relying on Verification alone would miss the full-solution Release build check.
status: open

### DW-298: The "blank `CommandSurfaceUnavailableReason` → `AggregateLocked` copy" deferred decision is now recorded independently, with drifting prose, in both `deferred-work.md` and the spec's own Review Findings section
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22, pass 2)"), 2026-08-25
location: deferred-work.md; spec-2-4b-wp-2a-removal-proof-and-audit-available.md
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: The "blank `CommandSurfaceUnavailableReason` → `AggregateLocked` copy" deferred decision is now recorded independently, with drifting prose, in both `deferred-work.md` and the spec's own Review Findings section. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: The "blank `CommandSurfaceUnavailableReason` → `AggregateLocked` copy" deferred decision is now recorded independently, with drifting prose, in both `deferred-work.md` and the spec's own Review Findings section. evidence: Compare this file's entry above (2026-08-22) against `spec-2-4b-wp-2a-removal-proof-and-audit-available.md`'s Review Findings "DEFERRED (2026-08-22): ACCEPT CURRENT COPY FOR THIS PASS" bullet — same decision, different prose framing. Two sources of truth for one decision invite silent divergence on the next edit.
status: open

### DW-299: Add browser-level computed-visibility coverage for the narrow configuration set and remove forms
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22, pass 2)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-4-disable-or-enable-tenant-with-complete-preview.md`
reason: The legacy ledger defers this issue: Add browser-level computed-visibility coverage for the narrow configuration set and remove forms. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-4-disable-or-enable-tenant-with-complete-preview.md` summary: Add browser-level computed-visibility coverage for the narrow configuration set and remove forms. evidence: Existing tests inspect stylesheet text only; they do not prove that the forms are hidden and the safety warning is visible at 767px, then available again at 768px. This is real configuration-flow work owned by Stories 3.5 and 3.6, not the Story 3.4 lifecycle flow.
status: open

### DW-300: Nine other command flows (create, add/remove/change member, metadata, set/remove configuration, global-admin grant/remove) still construct a 2-arg `TenantCommandTrackingHandle` with no aggregate id, so they keep accepting a status response for a different command and keep treating propagation lag as terminal. Story 3.4's Boundaries fence this under "Ask First: broadening shared command infrastructure beyond the focused lifecycle seam"
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: TenantCommandTrackingHandle
reason: The legacy ledger defers this issue: Nine other command flows (create, add/remove/change member, metadata, set/remove configuration, global-admin grant/remove) still construct a 2-arg `TenantCommandTrackingHandle` with no aggregate id, so they keep accepting a status response for a different command and keep treating propagation lag as terminal. Original context is preserved in legacy-detail.
legacy-detail: - Nine other command flows (create, add/remove/change member, metadata, set/remove configuration, global-admin grant/remove) still construct a 2-arg `TenantCommandTrackingHandle` with no aggregate id, so they keep accepting a status response for a different command and keep treating propagation lag as terminal. Story 3.4's Boundaries fence this under "Ask First: broadening shared command infrastructure beyond the focused lifecycle seam".
status: open
decision: 2026-08-26 Aggregate-aware handles — Design one AggregateId-aware tracking handle and migrate all nine flows with cross-route and remount tests.
decision: 2026-08-25 Broaden shared tracking — Design one AggregateId-aware tracking handle and migrate all nine flows with cross-route and remount tests.

### DW-301: `TenantDetailPage`'s `ResetLifecycleProofScope`/`BeginLifecycleProof`/`CanApplyLifecycleProof`/`CompleteLifecycleProof` quartet is a verbatim copy of the metadata quartet, and `TenantQueryGateway.GetLifecycleProjectionProofAsync` is byte-identical to `GetMetadataProjectionProofAsync`. Extract a keyed `ProofScope` helper before a third command surface needs one
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: TenantDetailPage
reason: The legacy ledger defers this issue: `TenantDetailPage`'s `ResetLifecycleProofScope`/`BeginLifecycleProof`/`CanApplyLifecycleProof`/`CompleteLifecycleProof` quartet is a verbatim copy of the metadata quartet, and `TenantQueryGateway.GetLifecycleProjectionProofAsync` is byte-identical to `GetMetadataProjectionProofAsync`. Original context is preserved in legacy-detail.
legacy-detail: - `TenantDetailPage`'s `ResetLifecycleProofScope`/`BeginLifecycleProof`/`CanApplyLifecycleProof`/`CompleteLifecycleProof` quartet is a verbatim copy of the metadata quartet, and `TenantQueryGateway.GetLifecycleProjectionProofAsync` is byte-identical to `GetMetadataProjectionProofAsync`. Extract a keyed `ProofScope` helper before a third command surface needs one.
status: open

### DW-302: `ProjectionVersion` is threaded through four carriers (page parameter, `HighImpactEvidence`, `ResolveAvailability` override, `TenantLifecycleAvailabilityInput`). In production all four agree, so the override is untestable no-op logic that diverges only under test doubles. Pick one carrier
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: ProjectionVersion
reason: The legacy ledger defers this issue: `ProjectionVersion` is threaded through four carriers (page parameter, `HighImpactEvidence`, `ResolveAvailability` override, `TenantLifecycleAvailabilityInput`). Original context is preserved in legacy-detail.
legacy-detail: - `ProjectionVersion` is threaded through four carriers (page parameter, `HighImpactEvidence`, `ResolveAvailability` override, `TenantLifecycleAvailabilityInput`). In production all four agree, so the override is untestable no-op logic that diverges only under test doubles. Pick one carrier.
status: open
decision: 2026-08-27 Evidence is canonical — Make TenantHighImpactActionEvidence the canonical version source, remove the override path, and retain compatibility shims for public component parameters during migration.
decision: 2026-08-27 Evidence is canonical — Make TenantHighImpactActionEvidence the canonical version source, remove the override path, and retain compatibility shims for public component parameters during migration.

### DW-303: A blank `ProjectionVersion` fails closed as `UnavailableReason.StaleData`, bricking both lifecycle buttons behind "authoritative data is not current" — a cause no refresh can fix and which misdirects support. Needs a distinct reason or recovery key
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: ProjectionVersion
reason: The legacy ledger defers this issue: A blank `ProjectionVersion` fails closed as `UnavailableReason.StaleData`, bricking both lifecycle buttons behind "authoritative data is not current" — a cause no refresh can fix and which misdirects support. Original context is preserved in legacy-detail.
legacy-detail: - A blank `ProjectionVersion` fails closed as `UnavailableReason.StaleData`, bricking both lifecycle buttons behind "authoritative data is not current" — a cause no refresh can fix and which misdirects support. Needs a distinct reason or recovery key.
status: open
decision: 2026-08-27 Specific recovery key — Keep the existing unavailable-reason enum stable, detect the missing-version branch explicitly at the lifecycle component boundary, and emit dedicated EN/FR message and recovery keys with focused tests.
decision: 2026-08-27 Specific recovery key — Keep the existing unavailable-reason enum stable, detect the missing-version branch explicitly at the lifecycle component boundary, and emit dedicated EN/FR message and recovery keys with focused tests.

### DW-304: The ten-item preview renders from `_snapshot.LastConfirmedProjection ?? Detail` while the eligibility gate validates `Detail`/`ResolvedEvidence`; after an in-flow Refresh the user can be shown facts no gate validated. Fails closed at submit, so consistency rather than correctness. Fix is ambiguous (which source wins)
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: Detail
reason: The legacy ledger defers this issue: The ten-item preview renders from `_snapshot.LastConfirmedProjection ?? Original context is preserved in legacy-detail.
legacy-detail: - The ten-item preview renders from `_snapshot.LastConfirmedProjection ?? Detail` while the eligibility gate validates `Detail`/`ResolvedEvidence`; after an in-flow Refresh the user can be shown facts no gate validated. Fails closed at submit, so consistency rather than correctness. Fix is ambiguous (which source wins).
status: done 2026-08-26
resolution: already resolved: commit 43ef25eb; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor:255-256,586-643 now renders and gates from the same retained confirmed preview evidence.
decision: 2026-08-25 Retained confirmed source — Use one revalidated retained-last-confirmed snapshot for both preview and gate, failing closed when its lifecycle or tenant binding is unsafe.

### DW-305: `tenants-lifecycle-unavailable-reason` is emitted by both the launcher (per action) and the open flow; with the flow open, up to three elements share the testid with different semantics
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: `tenants-lifecycle-unavailable-reason` is emitted by both the launcher (per action) and the open flow; with the flow open, up to three elements share the testid with different semantics. Original context is preserved in legacy-detail.
legacy-detail: - `tenants-lifecycle-unavailable-reason` is emitted by both the launcher (per action) and the open flow; with the flow open, up to three elements share the testid with different semantics.
status: done 2026-08-26
resolution: already resolved: commit 94d496cf; TenantLifecycleCommandFlow.razor:32 and TenantLifecycleActionAvailability.razor:110 now emit distinct flow and action-specific test IDs.

### DW-306: The French accent repair is partial: ~30 entries fixed, but `Tenants.GlobalAdministrators.Column.Identity`/`.Availability` and `Tenants.Audit.Column.Category`/`.Outcome` remain unaccented. Key parity is clean (1346/1346). Finish in a dedicated pass
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: Availability
reason: The legacy ledger defers this issue: The French accent repair is partial: ~30 entries fixed, but `Tenants.GlobalAdministrators.Column.Identity`/`.Availability` and `Tenants.Audit.Column.Category`/`.Outcome` remain unaccented. Original context is preserved in legacy-detail.
legacy-detail: - The French accent repair is partial: ~30 entries fixed, but `Tenants.GlobalAdministrators.Column.Identity`/`.Availability` and `Tenants.Audit.Column.Category`/`.Outcome` remain unaccented. Key parity is clean (1346/1346). Finish in a dedicated pass.
status: open

### DW-307: `TenantLifecycleAttemptTracker` has no attempt expiry and never prunes `_terminalMessageByTenantId`/`_terminalAttemptStartedAtByTenantId`; both grow for the circuit's lifetime. Subsumed by the open decision on bounding a wedged attempt
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: TenantLifecycleAttemptTracker
reason: The legacy ledger defers this issue: `TenantLifecycleAttemptTracker` has no attempt expiry and never prunes `_terminalMessageByTenantId`/`_terminalAttemptStartedAtByTenantId`; both grow for the circuit's lifetime. Original context is preserved in legacy-detail.
legacy-detail: - `TenantLifecycleAttemptTracker` has no attempt expiry and never prunes `_terminalMessageByTenantId`/`_terminalAttemptStartedAtByTenantId`; both grow for the circuit's lifetime. Subsumed by the open decision on bounding a wedged attempt.
status: done 2026-08-25
resolution: already resolved: commit 43ef25eb; TenantLifecycleAttemptTracker.cs:253-272 prunes expired terminal tombstones under an injected clock and tests cover the boundary.

### DW-308: `TenantConfigurationManagementContext`'s null `authorityState` default (implicit `TenantOwner` grant) and `TenantConfigurationSafeComposer`'s `_ = tenantStatus;` discard were documented with comments rather than removed
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: TenantConfigurationManagementContext
reason: The legacy ledger defers this issue: `TenantConfigurationManagementContext`'s null `authorityState` default (implicit `TenantOwner` grant) and `TenantConfigurationSafeComposer`'s `_ = tenantStatus;` discard were documented with comments rather than removed. Original context is preserved in legacy-detail.
legacy-detail: - `TenantConfigurationManagementContext`'s null `authorityState` default (implicit `TenantOwner` grant) and `TenantConfigurationSafeComposer`'s `_ = tenantStatus;` discard were documented with comments rather than removed.
status: open

### DW-309: `_hasAdoptedRetainedAttempt` is latched before the tracker lookup, and a `Detail.TenantId` change on a mounted flow is never re-adopted. Latent only — the parent renders the flow solely for a loaded tenant
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: _hasAdoptedRetainedAttempt
reason: The legacy ledger defers this issue: `_hasAdoptedRetainedAttempt` is latched before the tracker lookup, and a `Detail.TenantId` change on a mounted flow is never re-adopted. Original context is preserved in legacy-detail.
legacy-detail: - `_hasAdoptedRetainedAttempt` is latched before the tracker lookup, and a `Detail.TenantId` change on a mounted flow is never re-adopted. Latent only — the parent renders the flow solely for a loaded tenant.
status: done 2026-08-25
resolution: already resolved: commit 43ef25eb; TenantLifecycleCommandFlow.razor:405-417 resets retained adoption and snapshots when TenantId changes.

### DW-310: `TenantLifecycleAttemptTracker.Remember` compares `AttemptStartedAtUtc` with `<=`, so two attempts within one clock tick collapse. Compare `(AttemptStartedAtUtc, MessageId)` or use a monotonic sequence
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: TenantLifecycleAttemptTracker.Remember
reason: The legacy ledger defers this issue: `TenantLifecycleAttemptTracker.Remember` compares `AttemptStartedAtUtc` with `<=`, so two attempts within one clock tick collapse. Original context is preserved in legacy-detail.
legacy-detail: - `TenantLifecycleAttemptTracker.Remember` compares `AttemptStartedAtUtc` with `<=`, so two attempts within one clock tick collapse. Compare `(AttemptStartedAtUtc, MessageId)` or use a monotonic sequence.
status: done 2026-08-25
resolution: already resolved: commit 43ef25eb; TenantLifecycleAttemptTracker.cs:163-195 orders attempts by timestamp and MessageId, with tests at TenantLifecycleAttemptTrackerTests.cs:235-249.

### DW-311: `Remember` mixes contracts: `SetSnapshot` treats `false` as a tracking mismatch, but the method throws `ArgumentException` for shape violations, which escape unhandled from a UI event handler
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: Remember
reason: The legacy ledger defers this issue: `Remember` mixes contracts: `SetSnapshot` treats `false` as a tracking mismatch, but the method throws `ArgumentException` for shape violations, which escape unhandled from a UI event handler. Original context is preserved in legacy-detail.
legacy-detail: - `Remember` mixes contracts: `SetSnapshot` treats `false` as a tracking mismatch, but the method throws `ArgumentException` for shape violations, which escape unhandled from a UI event handler.
status: done 2026-08-25
resolution: already resolved: commit 43ef25eb; TenantLifecycleAttemptTracker.cs:129-139 rejects malformed retained shapes without throwing.

### DW-312: Preserve a visible exit when the set-configuration flow is opened wide and the viewport is then narrowed
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: SetTenantConfigurationFlow.razor.css
source_spec: `_bmad-output/implementation-artifacts/spec-3-4-disable-or-enable-tenant-with-complete-preview.md`
reason: The legacy ledger defers this issue: Preserve a visible exit when the set-configuration flow is opened wide and the viewport is then narrowed. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-4-disable-or-enable-tenant-with-complete-preview.md` summary: Preserve a visible exit when the set-configuration flow is opened wide and the viewport is then narrowed. evidence: `SetTenantConfigurationFlow.razor.css` hides the entire form at 767px, including its only Cancel control, and the rule lacks the neighboring FrontComposer CSS exception comment. This belongs to Stories 3.5/3.6; lifecycle-only scope was explicitly retained for this run.
status: done 2026-08-26
resolution: already resolved: commit 424d7624; SetTenantConfigurationFlow.razor:128-144 keeps Refresh and Cancel outside the form hidden by SetTenantConfigurationFlow.razor.css:123-130.

### DW-313: `TenantLifecycleAttemptTracker` reimplements `TenantCreateAttemptTracker` without sharing
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateAttemptTracker.cs:24-70; TenantsUiServiceCollectionExtensions.cs:98
reason: The legacy ledger defers this issue: `TenantLifecycleAttemptTracker` reimplements `TenantCreateAttemptTracker` without sharing. Original context is preserved in legacy-detail.
legacy-detail: - **`TenantLifecycleAttemptTracker` reimplements `TenantCreateAttemptTracker` without sharing** — `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateAttemptTracker.cs:24-70` is the same circuit-scoped, `StringComparer.Ordinal`-keyed, lock-guarded per-tenant retention concept, but with no expiry, no terminal tombstones, and a `Forget(tenantId)` carrying exactly the late-completion race the new tracker's docs warn about. Both are registered side by side (`TenantsUiServiceCollectionExtensions.cs:98`). Pre-existing; the create flow is Story 3.1 territory and sharing a generic base is a cross-story refactor. Either share a base or file the create-flow gap explicitly — two divergent answers to one question is the worse outcome.
status: open

### DW-314: `TenantConfigurationManagementContext.Available` documents the `authorityState = null` landmine instead of removing it
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationManagementContext.cs:85-92
reason: The legacy ledger defers this issue: `TenantConfigurationManagementContext.Available` documents the `authorityState = null` landmine instead of removing it. Original context is preserved in legacy-detail.
legacy-detail: - **`TenantConfigurationManagementContext.Available` documents the `authorityState = null` landmine instead of removing it** — `src/Hexalith.Tenants.UI/State/TenantDetail/TenantConfigurationManagementContext.cs:85-92`. The `<remarks>` explains at length that the null default silently grants `TenantOwner`, exists only for tests predating the authority distinction, and "a new test should not rely on" it. A comment cannot stop the next test from taking the 5-argument overload. Second occurrence — also deferred in Loop 2. Deferred again because configuration authority is Story 3.5/3.6 territory, which Story 3.4's "Never" list excludes.
status: open

### DW-315: `TenantCommandGateway.BoundSafeFailureReason` returns most backend failure text verbatim
origin: deferred from code review of spec-3-4-disable-or-enable-tenant-with-complete-preview, 2026-08-25
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:1034-1041
source_spec: `_bmad-output/implementation-artifacts/spec-3-4-disable-or-enable-tenant-with-complete-preview.md`
reason: The existing sanitizer replaces values only when a short marker denylist matches; every other backend failure reason is returned verbatim, truncated to 160 characters. A backend detail or secret outside that marker list could therefore reach command UI. Replacing the denylist with an allow-listed support-safe mapping is shared gateway hardening beyond the focused lifecycle patch.
status: open

### DW-316: Localize all `UnavailableTenantCommandGateway` failure results
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview.md (2026-08-25)"), 2026-08-26
location: src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs
reason: All 13 members return the raw English text "Tenant command gateway configuration is missing." instead of the localized `FailedWithKey("Tenants.Lifecycle.Unavailable.CommandSurface")` default, so a French operator sees untranslated failures. Change the class in one pass to avoid split behavior.
status: open

### DW-317: Repair inert `__form` scoped CSS selectors on configuration and lifecycle flows
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
location: src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css:33,41; src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor.css:33,41; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor.css:48-54
reason: The selectors target a class placed on an `<EditForm>` component, which does not receive the CSS-isolation scope attribute, so these layout rules have never applied. Repair scope stamping across all three flows and visually verify every width because the change affects form layout.
status: open

### DW-318: Replace the legacy Fluent v4 token in `SetTenantConfigurationFlow`
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
location: src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css:49
reason: The stylesheet uses `var(--accent-fill-rest, LinkText)`, violating the project ban on legacy `--accent-*`, `--neutral-*`, `--type-ramp-*`, and `--palette-*` Fluent tokens. Replace it with the approved Fluent UI v5 styling contract while preserving the intended visual state.
status: done 2026-08-27
resolution: already resolved: commit 2e19cc8e; src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css:48-50 uses Fluent v5 --colorBrandStroke1 instead of --accent-fill-rest.

### DW-319: Retire or deprecate untracked lifecycle dispatch methods
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
location: src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs (`EnableTenantAsync` and `DisableTenantAsync`)
reason: The public, undeprecated methods have zero production callers after the tracked pair took over, but a future caller could use them to bypass `TenantLifecycleAttemptTracker`. Removal is breaking, so retire or deprecate the methods with an explicit compatibility plan.
status: open
decision: 2026-08-26 Deprecate with guidance — Mark methods obsolete, document tracked replacements, and add compatibility tests before later removal.

### DW-320: Remove the dead lifecycle `DuplicatePrevented` state path
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor
reason: Lifecycle `SubmitAsync` uses `BlockedWithTracking`, which never produces `TenantLifecycleCommandSnapshot.DuplicatePrevented`, while `HasTerminalOwnership` and the lifecycle icon switch still carry that unreachable state path. Remove the lifecycle-only dead handling without disturbing command flows that still use `DuplicatePrevented`.
status: open

### DW-321: Extract shared lifecycle lease-reclamation and focus helpers
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:560,576,593,2336; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor
reason: Four admission-owner, retained-lease, and release blocks repeat the same logic with small identity and ownership-polarity differences, while `FocusSafelyAsync` is byte-identical across the two lifecycle components. Extract shared helpers that preserve those deliberate differences.
status: open

### DW-322: Move `TenantLifecycleAttemptTracker` pruning off the render path
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantLifecycleAttemptTracker.cs; TenantLifecycleCommandFlow.OnParametersSet; TenantLifecycleActionAvailability.OnParametersSet
reason: `Find` prunes under lock and allocates `ToArray` snapshots of three dictionaries, and both lifecycle components invoke it unconditionally during parameter rendering. Prune on mutation or on a timer so routine renders do not pay the repeated lock and allocation cost.
status: open

### DW-323: Enforce or remove `PendingStatusPollCount`
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs; src/Hexalith.Tenants.UI/State/TenantCommands/TenantLifecycleAttemptTracker.cs
reason: `PendingStatusPollCount` is incremented, saturated, and merged across attempts but never read as a budget; only the five-minute deadline bounds polling. Enforce a poll cap or remove the unused counter and its `MergeSameAttempt` plumbing.
status: open
decision: 2026-08-27 Enforce poll budget — Define a tested maximum pending-poll count, transition safely to UnableToVerify when exhausted, and keep the existing public field operational.
decision: 2026-08-27 Enforce poll budget — Define a tested maximum pending-poll count, transition safely to UnableToVerify when exhausted, and keep the existing public field operational.

### DW-324: French audit resource values spell "Reference" without its accent, so a French operator reads unaccented labels where the rest of the file is correctly accented.
origin: spec-deferred 1be4f24f2760
location: src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx:3374
source_spec: `spec-deferred-work-support-safe-copy-followup.md`
severity: low
reason: TenantsResources.fr.resx uses accented French throughout ("Non connecte" is spelled "Non connecté" at line 19, "Périmé" at line 85), and line 3707 already carries "Référence d'audit d'origine". The audit block is the exception: line 3374 "Reference d'audit : {0}", line 3380 "Reference de commande", line 3389 "Reference d'audit", line 3410 "Reference indisponible". Lines 3380 and 3389 predate this story, so the cluster is pre-existing rather than introduced here.
status: open

### DW-325: TenantConfigurationManagement latches _removeCommandInFlight from a retained attempt with no reset branch, so the flag survives the tracker's autonomous expiry when the flow is unmounted.
origin: spec-deferred 1af6159500d9
location: src/Hexalith.Tenants.UI/Components/Tenants/Configuration/TenantConfigurationManagement.razor:363
source_spec: `spec-3-6-remove-configuration-key-with-complete-preview.md`
severity: low
reason: OnParametersSet sets _removeCommandInFlight = true whenever RemoveAttemptTracker.Find returns a retained attempt, and only the flow's own lease callback lowers it. Tracker expiry raised while the flow is unmounted never runs that callback. The obvious else-reset was implemented and reverted: it lowers ChildCommandInFlight at the instant of confirmation, and the management landmark then replaces both flows with the unavailable paragraph before the operator can see the terminal state (Matching_signalr_notification_reconciles_retained_remove_without_redispatch_or_nudge_success fails). Impact is limited: the page clears its own _commandInFlight on expiry, so IsCommandSurfaceAvailable still recovers. A correct fix needs a distinct "flow owns the lease" signal.
status: open

### DW-326: One of the ten mandated preview facts is a constant, and roughly two dozen enum-keyed EN/FR strings can never render.
origin: spec-deferred 3352ed6ca1f0
location: src/Hexalith.Tenants.UI/State/TenantCommands/TenantRemoveConfigurationPreview.cs:50
source_spec: `spec-3-6-remove-configuration-key-with-complete-preview.md`
severity: low
reason: TenantRemoveConfigurationPreview.IsComplete requires IsAuthoritative, which requires Freshness == Current and Lifecycle == Current. PreviewItems returns [] unless IsComplete, so the merged "Read model: {0}; projection lifecycle: {1}." fact always reads Current/Current, and only one of the Remove.Freshness.*, Remove.Lifecycle.* and Preview.CurrentState.* values is ever reachable. A degraded-freshness operator sees a block message instead of a degraded-freshness fact.
status: open
decision: 2026-08-28 Keep ten-fact contract — Keep the complete-preview contract, document Current and Current as an explicit proof fact, prune unreachable resources, and pin the intentional constant in tests.
decision: 2026-08-28 Keep ten-fact contract — Keep the complete-preview contract, document Current and Current as an explicit proof fact, prune unreachable resources, and pin the intentional constant in tests.

### DW-327: The untracked RemoveTenantConfigurationAsync overload silently changed its failure contract to keyed, SafeMessage-null results.
origin: spec-deferred ee1c75c4860f
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:273
source_spec: `spec-3-6-remove-configuration-key-with-complete-preview.md`
severity: low
reason: It now delegates to RemoveTenantConfigurationTrackedAsync, so it can return Ambiguous or FailedWithKey("Tenants.Commands.Unavailable.InvalidTrackingReference") with SafeMessage null. No production caller remains, and no test pins the overload, so a future caller rendering SafeMessage would show empty text.
status: open
decision: 2026-08-28 Deprecate compatibly — Mark the overload obsolete, document the tracked replacement, and preserve safe compatibility behavior until a later removal.
decision: 2026-08-28 Deprecate compatibly — Mark the overload obsolete, document the tracked replacement, and preserve safe compatibility behavior until a later removal.

### DW-328: Follow-up review still recommended for 3-6-remove-configuration-key-with-complete-preview after the damping cap was spent
origin: review-budget-followup
location: n/a
source_spec: `spec-3-6-remove-configuration-key-with-complete-preview.md`
severity: low
reason: The follow-up-review damping cap (limits.max_followup_reviews = 1) was spent with the story finalized (status: done, verify green) while the review pass still recommended an independent follow-up. The work was committed by bmad-loop run 20260827-234608-260c; this entry preserves the lingering recommendation for a deliberate later review.
status: open

### DW-339: Builds `package-version-audit.json` still records EventStore `3.99.0` while the catalog pin is `3.100.0`.

origin: migrated from legacy ledger (""), 2026-09-02
location: references/Hexalith.Builds/Tools/package-version-audit.json
source_spec: `_bmad-output/implementation-artifacts/spec-eventstore-3-100-0.md`
reason: At deferral time, the Builds audit still contained EventStore `selectedVersion` `3.99.0` after Builds `e1026cb` set `HexalithEventStoreVersion` to `3.100.0`; regenerating the Builds-owned audit was Ask First for the source story.
status: done 2026-09-02
resolution: already resolved: references/Hexalith.Builds commit 7e84ff1; package-version-audit.json:91301-91303 and Props/Directory.Packages.props:8 now align current EventStore evidence and catalog at 3.101.0.

### DW-340: In-progress `spec-refresh-dependencies.md` can overwrite the new EventStore `3.100.0` planning facts.

origin: migrated from legacy ledger (""), 2026-09-02
location: _bmad-output/implementation-artifacts/spec-refresh-dependencies.md
source_spec: `_bmad-output/implementation-artifacts/spec-eventstore-3-100-0.md`
reason: The dependency-refresh spec remained `in-progress` and instructed an agent to reconcile `_bmad-output/project-context.md` after a broader refresh; the source story forbade resuming it but neither closed nor retargeted that work, leaving the recorded EventStore `3.100.0` planning facts at risk of overwrite.
status: open
decision: 2026-09-02 Amend and finish spec — Human-approve an amendment that rebases the frozen dependency intent on current 3.101.0 facts, reconciles remaining work, and closes or completes the spec safely.
decision: 2026-09-02 Amend and finish spec — Human-approve an amendment that rebases the frozen dependency intent on current 3.101.0 facts, reconciles remaining work, and closes or completes the spec safely.

### DW-329: Global-administrator command retries do not retain a caller-owned message id across ambiguous transport failures.
origin: spec-deferred 9acd809c21e3
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:329
source_spec: `spec-4-1-fixed-scope-global-administrator-action-availability.md`
severity: high
reason: The existing gateway creates the message id inside SetGlobalAdministratorAsync/RemoveGlobalAdministratorAsync and catches only EventStoreGatewayException, so timeout/HttpRequestException retry semantics belong to the downstream command-lifecycle stories rather than this availability-only story.
status: done 2026-09-02
resolution: already resolved: TenantCommandGateway.cs:343-398 and 406-461 now accept caller-owned message IDs and classify ambiguous grant/removal delivery without minting a replacement identity.

### DW-330: Existing grant/remove projection confirmation does not require baseline projection-version advancement or command-specific audit provenance.
origin: spec-deferred b69ce19c6d80
location: src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:150
source_spec: `spec-4-1-fixed-scope-global-administrator-action-availability.md`
severity: high
reason: The historical downstream command snapshots can accept qualifying target presence/absence without comparing the pre-command projection version; Story 4.1 dispatches no command and owns availability, so confirmation hardening remains follow-up work for the grant/remove lifecycle owners.
status: done 2026-09-02
resolution: already resolved: commits 24e3a41d and 2eba0b50; GlobalAdministratorGrantCommandSnapshot.cs:337-379 and GlobalAdministratorRemoveCommandSnapshot.cs:393-447 require exact command-event evidence plus projection-version advancement.

### DW-331: A contradictory complete-read authorization-scope flag is not revalidated by the pure removal evaluator.
origin: spec-deferred b78a2689221d
location: src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorActionAvailabilityEvaluator.cs:46
source_spec: `spec-4-1-fixed-scope-global-administrator-action-availability.md`
severity: medium
reason: GlobalAdministratorActionAvailabilityEvaluator accepts CompleteKind Ready with non-empty complete rows without checking CompleteIsAuthorizationScopedEmpty. The bounded page loader rejects that shape and both installed production callers currently supply internally consistent snapshots, so the defect predates and is not caused by this re-drive's action-specific readiness change; a direct public evaluator caller could still construct the contradictory evidence and receive an available removal result.
status: open

### DW-332: Repeated availability evaluation during one Razor render could theoretically produce mismatched reason and recovery associations.
origin: spec-deferred cb19669a12d0
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:821
source_spec: `spec-4-1-fixed-scope-global-administrator-action-availability.md`
reason: Grant and removal availability are recomputed independently for visible copy, disabled state, and aria-describedby. Blazor serializes renderer callbacks, but the admission gate's state can change before its notification callback is rendered; a deterministic test that changes gate or viewport evidence between those synchronous property reads is needed to establish whether an inconsistent render is reachable. The evaluation pattern predates this re-drive.
status: open

### DW-333: The concurrently introduced tenant-workspace tab migration lacks browser-level proof that inactive panels are not visible or focusable.
origin: spec-deferred 55356cb8b10a
location: tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs:420
source_spec: `spec-4-2-grant-global-administrator-with-projection-confirmation.md`
severity: medium
reason: The workspace-tab change arrived in a separate concurrent commit and is outside Story 4.2. Existing component assertions cover attributes, but an authenticated browser active-element and visibility trace would settle the remaining interaction risk.
status: open

### DW-334: The concurrently introduced Memories secret-store topology lacks a deployed Aspire model and health verification.
origin: spec-deferred dd7b16301f77
location: src/Hexalith.Tenants.AppHost/Program.cs:117
source_spec: `spec-4-2-grant-global-administrator-with-projection-confirmation.md`
severity: medium
reason: The AppHost secret-store change arrived in a separate concurrent commit and is outside Story 4.2. An Aspire resource-model inspection plus a healthy deployed startup using the intended secret provider would settle the topology risk.
status: open

### DW-335: The tenant-workspace tab migration removed every inactive-panel visibility assertion inside this story's own commit, not a concurrent one.
origin: spec-deferred 36063b3ad145
location: tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs:426
source_spec: `spec-4-2-grant-global-administrator-with-projection-confirmation.md`
severity: medium
reason: Commit 8da765ad -- the same commit that carries the Story 4.2 grant work -- replaced the `hidden`/`aria-hidden` assertions on `#tenants-retained-panel`/`#users-retained-panel` with `role="tabpanel"` checks that hold for the active and inactive panel alike. Fluent UI v5 owns the panel flip client-side, so bUnit cannot observe it and no assertion anywhere distinguishes the two states. An authenticated browser trace showing the inactive panel is neither visible, focusable, nor exposed to assistive technology would settle it.
status: open

### DW-336: Focus containment, focus restoration, and viewport measurement are proven at the interop layer rather than in a real browser.
origin: spec-deferred 36c5b71ef493
location: tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs
source_spec: `spec-4-2-grant-global-administrator-with-projection-confirmation.md`
severity: medium
reason: The component tests assert which ElementReference the page asked the runtime to focus and drive the viewport by calling Observe on the observation singleton. Neither reaches document.activeElement, a real Tab cycle, `inert` semantics, or a real JS measurement, and there is no browser-driven lane in this repository. An authenticated browser trace over the grant preview -- open, Tab cycle, Escape, restore -- and a real viewport measurement would settle it.
status: open

### DW-337: Identical transport conditions are classified two different ways depending on which tracked command was dispatched.
origin: spec-deferred 61e82d34cb29
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:258
source_spec: `spec-4-2-grant-global-administrator-with-projection-confirmation.md`
severity: low
reason: `SetGlobalAdministratorTrackedAsync` now treats a retryable EventStoreGatewayException and a plain OperationCanceledException as same-identity ambiguity, while `SetTenantConfigurationTrackedAsync`, `RemoveTenantConfigurationTrackedAsync`, `EnableTenantTrackedAsync`, and `DisableTenantTrackedAsync` still key off status codes and `TaskCanceledException` alone. Those four are unchanged pre-existing behaviour outside this story's fixed-scope intent; a decision on whether the grant rule supersedes them would settle it.
status: open
decision: 2026-09-06 Unify ambiguity semantics — Extract one shared retryable-transport classifier and apply the global-administrator same-identity ambiguity rule to configuration and lifecycle tracked dispatch, with parity tests.
decision: 2026-09-06 Unify ambiguity semantics — Extract one shared retryable-transport classifier and apply the global-administrator same-identity ambiguity rule to configuration and lifecycle tracked dispatch, with parity tests.

### DW-338: Nothing in this repository pins EventStore's command-status contract, which the grant lifecycle reasons over directly.
origin: spec-deferred 0ab333618257
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:575
source_spec: `spec-4-2-grant-global-administrator-with-projection-confirmation.md`
severity: low
reason: Every status assertion is fed by a stub. This pass corrected one concrete assumption -- that EventsStored/EventsPublished carry an EventCount -- only by reading AggregateActor and CommandStatusRecord in the submodule. A contract or integration test over a real command-status response would settle the remaining assumptions the same way.
status: done 2026-09-05
resolution: already resolved: tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs:403-508 submits real commands and polls /api/v1/commands/status/{correlationId}, deserializing CommandStatusResponse and asserting real terminal statuses at lines 126-149 and 311-364.

### DW-341: `scripts/publish-partial-release.sh` reads `HEXALITH_RELEASE_PACKAGE_MANIFEST` into a `manifest` variable that no command ever consumes, so an operator override is silently ignored.
origin: spec-deferred 6dcaa0451f25
location: scripts/publish-partial-release.sh:6
source_spec: `spec-package-boundary-source.md`
severity: low
reason: `scripts/publish-partial-release.sh:6` assigns `manifest="${HEXALITH_RELEASE_PACKAGE_MANIFEST:-tools/release-packages.json}"` and no later line references `$manifest`; `grep -n manifest scripts/publish-partial-release.sh` returns line 6 only. The variable was already dead at `bbb0b11a` (the file is untouched by this change), and `scripts/pack-release-packages.py` accepts no manifest argument either, so threading the new `--manifest` flag alone would not make the override effective. Pre-existing, not caused by this story.
status: open

### DW-342: Sprint tracking marks Story 4.3 in progress while Epic 4 remains done.

origin: migrated from legacy ledger (""), 2026-09-05
location: _bmad-output/implementation-artifacts/sprint-status.yaml:121-125
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies-2.md`
reason: The concurrent unstaged edit changes Story 4.3 from backlog to in-progress without reopening Epic 4, so status consumers can report a completed epic with active work; the Story 4.3 workflow owns the correction.
status: done 2026-09-06
resolution: already resolved: _bmad-output/implementation-artifacts/sprint-status.yaml:69-73 marks Epic 4 in-progress while Story 4.3 is review, removing the completed-epic/active-story contradiction.

### DW-343: The dependency-refresh spec File List omits EventStore, FrontComposer, and Memories gitlinks plus Aspire topology test edits that sit in the same baseline range.

origin: migrated from legacy ledger (""), 2026-09-05
location: _bmad-output/implementation-artifacts/spec-refresh-dependencies-2.md; references/Hexalith.EventStore; references/Hexalith.FrontComposer; references/Hexalith.Memories; tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: These files changed after Story 4.3's baseline in later dependency and topology work rather than in the removal command surface; a dedicated refresh-spec File List update would settle the ledger without changing removal behavior.
status: open

### DW-344: `MemoriesSecretStoreResourceGraphTests` does not pin package-mode secret-store properties, so a default source-reference run may not prove the NuGet graph.

origin: migrated from legacy ledger (""), 2026-09-05
location: tests/Hexalith.Tenants.IntegrationTests/MemoriesSecretStoreResourceGraphTests.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: The test was added by later dependency work in the baseline range and is outside Story 4.3's removal intent; it does not pin the package-mode properties needed to distinguish the NuGet resource graph from a default source-reference run.
status: open

### DW-345: `focusElementById` may report a false failure when FluentButton focuses an inner native control instead of the host that owns the Cancel id.

origin: migrated from legacy ledger (""), 2026-09-05
location: src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js:23-35
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: `tenantsFocus.js` returns `document.activeElement === target` after `target.focus()`. A real-browser trace of `document.activeElement` after focusing the rendered Cancel host would settle whether a successful inner-control focus is reported as false and wrap fallback incorrectly skips Cancel.
status: open

### DW-346: Grant preview Cancel remains a raw lowercase fluent-button.

origin: code review chunk A 2026-09-06
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:633
source_spec: `_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
severity: low
reason: Story 4.3 required Fluent V5 actions on the removal dialog. The grant preview Cancel control is pre-existing KEEP grant chrome (`<fluent-button appearance="outline" @ref>`), not introduced by this removal pass.
status: open

### DW-347: Story 4.3 File List gitlink SHAs do not match the current tree.

origin: code review chunk A 2026-09-06
location: _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md:450
source_spec: `_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
severity: medium
reason: `python3 scripts/validate-story-gitlinks.py` FAIL: tree is Builds d004983, EventStore acf5c4e, FrontComposer 5cbc558, Memories fa8f531, but the File List still states 8db7459 / 070a4b6 / 0922400 / f7fef9f. Later build(deps) advanced the pointers; updating the record edits this spec, restoring the tree reverts later work. Chunk C.
status: open

Chunk A also restated DW-345 (`focusElementById` host vs inner activeElement) as maybe-false; no new ledger row.

### DW-348: Spec 5.1 remains in-review with its last operator-handoff task unchecked.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: _bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: The Story 5.1 spec remains in review because its final operator-handoff task for the awaiting-operator performance contract is unchecked; this tracking belongs to Story 5.1 rather than last-administrator removal.
status: done 2026-09-24
resolution: already resolved: commit e41a784e4fe870c2290676e60d8aaf6527a76219; spec-5-1-browse-tenant-audit-trail.md:5 is done with every task checked, and story-5-1-performance-evidence.md records the passing v4 run (126/126 percentile groups, 10/10 gates, no fallback).

### DW-349: Tenant audit filter validation can be written off the renderer and never painted.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: TenantAuditPage.TryCreateRequest mutates _fromValidationKey, _toValidationKey, and _categoryValidationKey before InvokeAsync, including from notification LoadAsync continuations that already use ConfigureAwait(false), so validation state can be written off the renderer and never painted.
status: open

### DW-350: Audit list return URLs that contain a cursor are rejected and replaced with the tenant path.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor; src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: TenantAuditPage.SafeReturnUrl validates the whole return URL through TenantAuditSupportSafety.SafeApprovedReference, whose strict fragments include cursor, so legitimate audit-list return URLs containing a cursor are replaced with the tenant path.
status: open

### DW-351: Tenant detail still matches audit/capability scope without the caller binding used by Story 5.1 reads.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor; src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: TenantDetailPage uses the caller-free MatchesScope(request) overload while TenantQueryGateway binds retained evidence with MatchesScope(request, callerScope), leaving tenant-detail matching without the caller binding used by Story 5.1 reads.
status: open

### DW-352: Audit unauthorized and unavailable recovery both navigate to the same BackHref.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: RequestPermission and Escalate both use the tenant or return link, so unauthorized and unavailable audit recovery have no distinct permission-request or support-safe escalation destination.
status: open
decision: 2026-09-06 Configurable destinations — Add explicit support-safe permission and escalation destinations through the UI composition contract, fail closed when absent, and test both navigation paths.
decision: 2026-09-06 Configurable destinations — Add explicit support-safe permission and escalation destinations through the UI composition contract, fail closed when absent, and test both navigation paths.

### DW-353: Audit timestamps that omit fractional seconds or use Z without seven digits are dropped from display.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditNarrative.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: TenantAuditNarrative.SafeTimestamp accepts only the round-trip O format with seven fractional digits, so otherwise valid timestamps that omit fractional seconds or use Z without seven digits are dropped from display.
status: open

### DW-354: A rejected audit EventId becomes an empty receipt/grid identity instead of failing closed.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRow.cs; src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: TenantAuditRow.FromEntry maps an unsafe EventId to string.Empty, and TenantAuditReceipt.FromEntry uses that mapper, producing an empty receipt or grid identity instead of failing closed.
status: open

### DW-355: Numeric audit category text "0" is coerced to Access without a field error.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: TenantAuditPage combines int.TryParse with Enum.IsDefined when parsing category text, so numeric enum value zero is accepted as Access instead of producing a field validation error.
status: open

### DW-356: A reversed audit From/To range has no page test for field-associated Range validation.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor; tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: TryCreateRequest rejects From greater than To with Tenants.Audit.Filter.Validation.Range, but TenantAuditPageTests never sets From later than To; gateway tests do not cover the page's field errors or its skipped query.
status: open

## Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-06)

- Focus containment remains interop-ID proof (`FocusedElementIds` / captured `ElementReference` ids), not `document.activeElement`, a real Tab/Shift+Tab cycle, or browser `inert`. Already DW-336; an authenticated browser trace of grant-preview open, Tab, Shift+Tab, Escape, and launcher restoration would settle it.
- Package-reference Memories secret-store AC9 is outside this grant-core chunk. Already DW-334 / operator-owned; AppHost and the published `Hexalith.Memories.Aspire` pin belong to group 3 of this review, not this pass.

## Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-07)

Grant-core follow-up (`7e88a571..d0c534ff`, 14 files). No new DW ids; later-mainline gitlinks and same-turn focus race stay out of this chunk.

- Focus containment remains interop-ID proof, including Escape from Fluent/FAST shadow DOM. Already DW-336; an authenticated browser trace of open, Tab, Shift+Tab, Escape, and launcher restoration would settle it.
- `validate-story-gitlinks.py` FAILs against HEAD because later mainline moved `references/` after grant completion `d0c534ff` (UNDECLARED `Hexalith.AI.Tools` / `Hexalith.Memories`; MISSTATED Builds/Commons/EventStore/FrontComposer vs the File List). Not caused by this 14-file chunk; belongs to group 3 / later `build(deps)`.
- Opening preview while `_focusGrantLauncherPending` is still set can focus the inert launcher. Unverified medium: settle with a same-turn cancel-then-reopen test that leaves both focus flags set before `OnAfterRenderAsync`. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1335`]

## Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)

Chunk 1 (state / gateway / admission) restated two open items; no new ledger rows.

- `focusElementById` may report a false failure when FluentButton focuses an inner native control instead of the host that owns the Cancel id. Already DW-345; a real-browser `document.activeElement` trace after focusing the rendered Cancel host would settle it.
- Story 4.3 File List gitlink SHAs still do not match the current tree. Already DW-347; `validate-story-gitlinks.py` now reports Builds `6daad3d`, EventStore `9a20c05`, FrontComposer `f0c3b6f` versus File List `39debe9` / `7b7f876` / `0a4c4ad`. Updating the record edits this spec; restoring the tree reverts later `build(deps)` work.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Restore-access correction cannot retain an ambiguous UnableToVerify grant on the aggregate lease.
  evidence: GlobalAdministratorCorrectionSnapshot.ToReconciliation allows correlationless grant only in RequestSent, so a restore snapshot that is already UnableToVerify and ambiguous returns null and cannot be adopted the way removal can. The correction panel never calls RetainAmbiguousPreflight on restore; this is Story 4.2 grant territory, not last-administrator removal.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: The pushall skill copies in the baseline range stage everything and commit with a build subject.
  evidence: Identical .agents/.claude/.github pushall skills arrived with later tooling commits in this story's baseline range. They are not the last-administrator removal command; leaving them in this File List would mix unrelated work.

## Deferred from: code review of spec-5-1-browse-tenant-audit-trail.md (2026-09-06)

- Tenant detail still uses caller-free `MatchesScope(request)` for audit/capability evidence while Story 5.1 binds retained audit reads with `MatchesScope(request, callerScope)`. Already DW-351; TenantDetailPage is outside this story's File List.
- `UserRemovedFromTenant` typed `PreviousRole`/`Role` cannot be produced because the event and audit projection emit only `userId`. Restore-access therefore cannot take an intended role from typed narrative; that is later correction-story/projection contract work, not browse-grid fail-closed.
- After projection refresh, `OpenCorrectionAsync` may assign a re-derived intent without re-checking `IsAvailable`. Unverified medium: settle by showing whether `CorrectionStartPanel` / `GlobalAdministratorCorrectionPanel` can submit when the parent intent is unavailable after refresh.

## Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-13)

Verification pass over the uncommitted fix round that closes the 2026-09-07 patch list.

- The `DispatchGrantAsync` redispatch-guard call site has no test. Filed by the verification-gap layer with a defer disposition: the window sits between an off-dispatcher `AreGrantDispatchCapabilitiesCurrent()` read and the on-dispatcher `livePrerequisites` re-read, with no injectable seam in the bUnit harness, and the deterministic sibling guard is now covered. Reverting the call to a bare `return;` fails nothing. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:3316`]
- Grant status refresh lacks the in-flight gate the remove side has. Pre-existing, not touched by this change: `CanRefreshRemoveStatus` opens with `Volatile.Read(ref _removeSubmissionInFlight) == 0 && _removeAdmissionLease?.IsReconciliationDispatchInFlight != true`, while `CanRefreshGrantStatus` has no equivalent, so a second retry click during an in-flight redispatch re-enters and can divert the returning submission result. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1124`]
- The sibling grant consumers did not adopt this round's pattern. Pre-existing and outside the grant-page file group: `GlobalAdministratorCorrectionPanel.RetryAmbiguousDeliveryAsync` still returns silently for the same grant command, and neither the correction panel nor the remove surface consumes a localization-specific unavailable reason although `Remove.Preview.Unavailable.Localization` already exists in both resx files and in `RequiredRemoveFactKeys`. [`src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:1173`]

## Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation-2.md (2026-09-21)

- Source-reference job may time out on a cold restore. Unverified medium: the new workflow has no NuGet cache, `timeout-minutes: 15`, and builds the full Debug source graph with `-m:1 -nr:false --no-incremental`. Settle by timing a cold CI run of that build plus the four-case execution; if it finishes under 15 minutes the timeout is adequate. [`.github/workflows/source-reference.yml:20`]

## Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)

Chunk A (declared production source). No new DW ids; grant KEEP and later gitlink drift stay out of this removal patch list.

- Restore-access correction still arms with `TryMarkDispatched` and cannot retain correlationless grant `RequestSent` the way removal now does. KEEP grant; this story tokenizes removal only. Already recorded as Story 4.2 restore-access lease territory. [`src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:913`]
- Grant withdrawn-retry is a snapshot bit that cannot survive adoption. Grant KEEP / Story 4.2 chrome, not last-administrator removal. [`src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:24`]
- Grant `EditForm OnSubmit` plus `fluent-button @onclick` can double-enter preview. Grant KEEP launcher, not the removal dialog. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:241`]
- Grant completion apply has no snapshot fallback when the intent/message/preview `Equals` guard fails. Grant KEEP dispatch; removal already uses `CompleteRemoveSingleFlightAsync`. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:3535`]
- `RequiredGrantFactKeys` grew to 89 keys and grant-preview gained `box-sizing: border-box` on this removal story. KEEP grant localization/chrome; belongs with Story 4.2. [`src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:25`]
- `focusElementById` may report a false failure when FluentButton focuses an inner native control instead of the Cancel host id. Already DW-345; a real-browser `document.activeElement` trace after host `.focus()` would settle it. [`src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js:23`]
- Correction outer catch may overlay Ambiguous after Accepted delivery. Unverified medium: settle by showing `RefreshStatusCoreAsync` throwing after `TryCompleteReconciliationDispatch` already published Accepted. [`src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:1082`]
- `validate-story-gitlinks.py` FAILs against HEAD: UNDECLARED Commons `6da79ae -> 9f4809d` and PolymorphicSerializations `8aeed1d -> 7e95556`; MISSTATED Builds/EventStore/FrontComposer/Memories vs File List `39debe9` / `7b7f876` / `0a4c4ad` / `f174f9c`. Already DW-347; updating the record edits this spec, restoring the tree reverts later `build(deps)` (Chunk C).

## Deferred from: oneshot review of spec-4-2-grant-global-administrator-with-projection-confirmation-2.md (2026-09-22)

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation-2.md`
  summary: MTP skipped results may satisfy the source-reference four-test floor.
  evidence: maybe-false; prove it by running `--filter-method Hexalith.Tenants.IntegrationTests.TenantsApiGeneratedControllerTests.GlobalAdministratorsRealHandlerMetadataSurvivesRouterRestClientAndUiGateway` with one forced skip and `--minimum-expected-tests 4`. If that run still exits 0, add `--fail-skips`.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation-2.md`
  summary: Source-reference job may time out on a cold restore.
  evidence: maybe-false; settle by timing a cold CI run of `dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug -p:UseHexalithProjectReferences=true -m:1 -nr:false --no-incremental` plus the four-case execution. If it finishes under 15 minutes, the timeout is adequate; if not, add the domain-ci NuGet cache and/or raise the timeout.

## Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)

- `focusElementById` may treat a successful Fluent button focus as failure. maybe-false, high if true. Settle with a browser trace of `document.activeElement` after focusing the remove-cancel Fluent button host. [`src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js:34`]
- `validate-story-gitlinks.py` FAILs against HEAD. This grant-core chunk excluded gitlinks. UNDECLARED `references/Hexalith.AI.Tools de38f78 -> 5f93d2e` and `references/Hexalith.Memories d1b95ab -> 8884933`. MISSTATED Builds `2fba349` (story says `9d77ed7`), Commons `9f4809d` (`372d715`), EventStore `66cb4ed` (`e38c125`), FrontComposer `0276424` (`c6fe14c`), PolymorphicSerializations `7e95556` (`8aeed1d`). Declare or revert in the gitlink chunk. [`scripts/validate-story-gitlinks.py`]

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
  summary: Add removal availability reason and recovery keys to the removal localization-readiness manifest.
  evidence: The removal evaluator can return keys outside `RequiredRemoveFactKeys`, so a missing translation can pass readiness; removal behavior is explicitly excluded from the grant intent.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
  summary: Reuse the cached fixed removal localization result during removal preview composition.
  evidence: `ComposeGlobalAdministratorRemovePreviewAsync` repeats the two-culture resource scan instead of using the existing per-circuit cache; this is pre-existing removal-only performance work.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
  summary: Correct ambiguous removal retry preflight and its blocked-state copy.
  evidence: A response-lost successful removal can make the target absent, causing normal preview validation to strand same-id redispatch; other mismatch branches can display affirmative availability copy as the failure reason.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
  summary: Make removal projection requery cancellable across supersession and disposal.
  evidence: `RequeryRemoveProjectionAsync` starts the bounded population walk with `CancellationToken.None`, so a replaced operation cannot stop its I/O.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
  summary: Determine whether remove-dialog JS interop can surface cancellation exceptions during browser teardown.
  evidence: This is maybe-false; reproduce `OperationCanceledException` or `TaskCanceledException` during module import, invocation, or disposal to decide whether additional containment is needed.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
  summary: Verify visual distinctness for accepted identities containing non-ASCII whitespace.
  evidence: This is maybe-false; compare accepted whitespace identities in supported browser/font combinations to determine whether literal visual rendering collides even though accessible names tokenize whitespace.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
  summary: Run authenticated browser evidence for modal focus, inert background, responsive layout, and long identity visibility.
  evidence: bUnit mocks DOM focus and does not compute CSS; a browser trace must cover both Tab directions, Escape restoration, short/mobile viewports, removal preview columns, and unclipped long identities.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
  summary: Update the quickstart for generated local Keycloak credentials.
  evidence: The realm now reads generated username/password parameters while the quickstart still submits `admin-user` and `admin-pass`, so the documented token request fails in the current local topology.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
  summary: Prove whether skipped source-reference cases satisfy the minimum expected test floor.
  evidence: This is maybe-false; force one of the four provenance cases to skip and run the current Microsoft.Testing.Platform command to determine whether `--fail-skips` is required.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
  summary: Pin build-only EventStore project-reference metadata in package governance tests.
  evidence: Current tests verify edge presence and version isolation but not `Condition`, `ReferenceOutputAssembly`, `Private`, or `IsAspireProjectResource`, allowing dependency or Aspire-resource leakage to regress unnoticed.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
  summary: Validate source-reference workflow commands structurally rather than with independent substrings.
  evidence: The current governance test can pass when required flags move to comments or unrelated steps, configurations diverge, or the YAML structure is invalid.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
  summary: Cover correction-panel grant and removal behavior when tracked dispatch capability is false.
  evidence: The pure evaluator tests false capabilities, but correction-panel stubs always return true; panel wiring can regress without disabling high-impact correction actions.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
  summary: Add real explicit-project-root tests for the BMAD customization resolver.
  evidence: Wrapper tests only inspect mocked argv; execute the resolver from a conflicting working directory and cover explicit-root precedence, fallback precedence, and warnings in the Python guard lane.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Update the quickstart for generated local Keycloak credentials.
  evidence: The realm now provisions generated per-run credentials while `docs/quickstart.md` still submits `admin-user` and `admin-pass`, so the documented token request fails against the default local topology.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Reject blank and repeated continuation cursors in tenant-audit payload validation.
  evidence: `TenantQueryGateway.IsValidTenantAuditPayload` validates rows but not the `HasMore`/cursor relationship or equality with the requested cursor, so an enabled Next action can no-op or loop on the same page.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Close the global-administrator read-refresh subscription race across authorization collapse.
  evidence: Authorization can become false after the post-subscribe guard and before `_readRefreshLease` assignment; the sign-out path can then miss and leave that callback subscribed because the post-assignment guard only rechecks disposal.

## Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)

Page and focus slice (`91d2335...c3a11fd0`). No new DW ids.

- Story gitlink guard still fails against HEAD. Already DW-347. Current tree is Builds `2fba349`, Commons `9f4809d` undeclared, EventStore `db1e9d7`, FrontComposer `00d4da4`, Memories `8884933`, PolymorphicSerializations `7e95556` undeclared, while the File List still states Builds `39debe9`, EventStore `7b7f876`, FrontComposer `0a4c4ad`, and Memories `f174f9c`. This slice does not move `references/`. Declaring edits the spec; restoring the tree reverts later `build(deps)` work.
- Grant preview `box-sizing: border-box` is in `.global-admins__grant-preview`. Already deferred as KEEP grant chrome for Story 4.2, not last-administrator removal.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Validate the Release/NuGet package graph in CI rather than relying on the Debug source-reference lane.
  evidence: `.github/workflows/source-reference.yml` explicitly builds and tests with `--configuration Debug -p:UseHexalithProjectReferences=true`, so it cannot expose packaging-only failures required by the repository CI policy; this workflow is later baseline-range integration work, not Story 4.3 removal behavior.

## Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A removal core)

Group A removal-core source slice (`91d2335..3a98efe`). Already-recorded items are listed without new DW ids.

- Story gitlink guard still fails against HEAD (`scripts/validate-story-gitlinks.py` exit 1). Already DW-347.
- `RequiredGrantFactKeys` has no guard proving rendered grant keys are listed and present in EN/FR. Already deferred as KEEP grant localization for Story 4.2.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Preserve specific grant rejection reasons instead of collapsing every code to the generic rejected key.
  evidence: `TenantCommandGateway.MapSetGlobalAdministratorGatewayException` maps `GlobalAdministratorAlreadyExists`, `InsufficientPermissions`, and other recognised codes to `Tenants.GlobalAdministrators.Grant.Submission.Rejected`, `GetStatusAsync` maps every rejected status to `Grant.Status.Rejected`, and the page never renders `RejectionCode`; introduced by Story 4.2 commit `cf0e420b`.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Cover the tenant-audit caller-change guard on faulting, invalid-cursor retry, and not-modified refetch paths.
  evidence: Only `Get_tenant_audit_discards_an_awaited_response_when_the_caller_changes` switches the caller mid-call, and it completes successfully with no retained snapshot; removing the `AuditIdentityFailure` check in the exception catches would return the original caller's retained rows with every test green. Story 5.1 code (`281e3c3c`).

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Decide how the tenant-audit payload allowlist handles server event types it does not know.
  evidence: `TenantQueryGateway.IsSupportedAuditEvent` hard-codes eleven event types, so one new server event type fails `IsValidTenantAuditPayload` for the whole page and signals `InvalidPayload`, the same signal as tampering. Story 5.1 code (`281e3c3c`).

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Verify grant completion is applied when the grant generation is superseded after the lease completion is published (unverified, would be medium).
  evidence: `DispatchGrantAsync` syncs the published completion only while `CanApplyGrantMutation(generation)` holds, and the gate handler skips grant sync while `_isGrantSubmitting` is true; settle with a test that invalidates the generation between `TryCompleteReconciliationDispatch` and the renderer sync.

## Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A production File List)

Production File List slice (`91d2335...HEAD`, 12 files). Already-recorded items are listed without new DW ids.

- Story gitlink guard still fails against HEAD (`scripts/validate-story-gitlinks.py` exit 1: Commons and PolymorphicSerializations undeclared; Builds/EventStore/FrontComposer/Memories SHAs misstated). Already DW-347.
- Grant delivery uses the removal lease-token API, correction restore still uses `TryMarkDispatched`, and `RequiredGrantFactKeys` expanded. KEEP grant / Story 4.2; already recorded on the earlier 2026-09-22 Group A pass and the iteration-6 restore-access deferral.
- `focusElementById` may report failure when focus lands on an inner Fluent control, and the Chromium harness mounts a raw `<fluent-button>` rather than the Blazor `FluentButton` host. Already DW-345; a real-browser `activeElement` trace of the rendered Cancel host would settle it.
- Correction outer catch can overlay Ambiguous after Accepted if `RefreshStatusCoreAsync` throws. Maybe-false; settle by showing that throw after `TryCompleteReconciliationDispatch` already published Accepted. Already recorded on iteration 6.

## Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)

Page-test slice (`91d2335..0f490cb`, `GlobalAdministratorsPageTests.cs`). Already-recorded items are listed without new DW ids.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Make the removal rejection page test prove localization instead of echoing the gateway's raw text.
  evidence: `Remove_rejection_keeps_last_confirmed_rows_without_success_or_member_copy` (`GlobalAdministratorsPageTests.cs:2332`) passes `Rejected(expectedText, code)`, and the stub localizer text also contains `expectedText`. `RemoveSafeMessage` falls back to raw `SafeMessage` when the key is missing, so a broken `Remove.Status.Rejected.*` mapping still passes. Pre-existing; not changed in this range.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Verify the removal requery mismatch branch cannot release a live same-attempt lease from a stale confirming basis (unverified, would be medium).
  evidence: In `RequeryRemoveProjectionAsync` (`GlobalAdministratorsPage.razor:4765-4773`), if `_removeSnapshot` advanced to the same attempt's next state during the held load, the lease still matches, so `RetainOrReleaseRemoveCompletion` can call `TryReleaseTerminal` for a terminal stale-basis projection while the UI keeps a non-terminal snapshot. Settle with a page test that advances the same attempt during a held confirming requery and asserts the lease and UI state together.

- Story gitlink guard still fails (`scripts/validate-story-gitlinks.py` exit 1). Already DW-347.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Stop the pushall per-repository procedure when its initial fetch fails.
  evidence: `.agents/skills/pushall/SKILL.md` step 1 runs `fetch --all --prune` but does not branch on failure; later merge and pruning steps can use stale remote-tracking refs.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Handle pushall checkout failure before continuing merge and final-branch steps.
  evidence: `.agents/skills/pushall/SKILL.md` step 4 checks out the chosen default without a failure path, while steps 5–11 assume the checkout succeeded.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Fail the legacy story gitlink guard when its baseline already contains story work.
  evidence: `scripts/validate-story-gitlinks.py` currently emits only a warning from `baseline_is_mid_story`; pointer moves before that baseline are excluded from the baseline-to-tree verdict. The current Story 4.3 baseline does not meet this condition.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Read unchanged legacy-mode gitlinks from the superproject index or tree when a submodule is uninitialized.
  evidence: `current_pointer(path, None)` runs `git -C <path> rev-parse HEAD`; in an uninitialized submodule directory Git can resolve the superproject HEAD and report a false pointer value. Recorded-commit mode uses `git ls-tree` and is unaffected.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Release a terminal grant lease adopted after ownerless delivery completion.
  evidence: `GlobalAdministratorsPage.AdoptRetainedReconciliation` assigns `_grantSnapshot` directly, bypassing `SetGrantSnapshot` and its terminal release; the grant replacement test completes delivery after the replacement already adopted the lease. This grant path belongs to Story 4.2 and shares the administrator aggregate lock.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
  summary: Keep tenant-audit Continue read-only linked to a rendered grid heading when filter validation is active.
  evidence: `TenantAuditPage.CanContinueReadOnlyForState` checks rows and stale/degraded/list-refreshed state while `ShouldRenderRows` also requires no filter validation, so the recovery link can target a heading omitted from the DOM. The audit surface was changed by later Story 5.1 work in the baseline range.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
  summary: Sanitize URL-supplied user and command context before showing it on the tenant audit page.
  evidence: `TenantAuditPage.ContextText` renders `targetUserId` or `supportSafeCommandReference` query values without the audit support-safety classifier. This contextual entry-point behavior predates the current Story 5.1 patch.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
  summary: Reject malformed audit date and non-string category fields in direct server query payloads.
  evidence: `TenantQueryHandlerBase.DeserializeAuditPayload` converts malformed `from`/`to` and non-string `category` to null filters; the method is unchanged from the Story 5.1 baseline, so a direct authorized query can broaden its tenant-scoped result.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
  summary: Recheck audit load generation inside the queued tenant-detail projection write.
  evidence: `TenantAuditPage.RefreshTenantProjectionForLoadAsync` checks `CanApply` before `InvokeAsync` only; a route change between those operations can replace the new route's supplementary correction evidence with the old tenant's detail snapshot. This projection path predates the current patch.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
  summary: Invalidate an in-flight correction open when the audit page changes tenant routes.
  evidence: `TenantAuditPage.OnParametersSetAsync` clears paging and projections on tenant change without incrementing `_correctionOpenGeneration`; a pending `OpenCorrectionAsync` can subsequently install its old-tenant intent. This later correction workflow predates the current audit-read patch.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
  summary: Revoke or refresh an open correction intent when its audit evidence degrades.
  evidence: `TenantAuditPage.ResolveReceiptSelection` updates the selected receipt after a degraded read but can leave `_activeCorrectionIntent` and a submission-ready child panel intact. The correction lifecycle predates the current audit-read patch.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
  summary: Keep dirty detached-HEAD submodule commits reachable from the branch that pushall pushes.
  evidence: `.agents/skills/pushall/SKILL.md` commits before checking out the default branch, so a detached-HEAD commit can be left behind. This separate Git skill is unrelated to tenant audit.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
  summary: Make pushall honor the remote's actual default branch when both main and master exist.
  evidence: `.agents/skills/pushall/SKILL.md` prefers local `main` ahead of `origin/HEAD`, so it can merge and prune against the wrong default. This separate Git skill is unrelated to tenant audit.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail-2.md`
  summary: Resolve whether the historical Story 5.1 performance guest used modified dependency source.
  evidence: The archived source patch marks EventStore, FrontComposer, and Memories gitlinks dirty without their nested status or diffs. A clean dependency checkout or preserved nested status/diffs on the next dedicated run would settle whether those markers represented source changes or generated output.

## Deferred from: code review of spec-5-2-reach-scoped-audit-evidence-from-context.md (2026-09-26)

Story range `fc147e3e..f17027ac`.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
  summary: Story 5.2 gitlink guard fails against HEAD because later commits bump submodules.
  evidence: `scripts/validate-story-gitlinks.py` exits 1 for undeclared Builds `754d2b4→2326f98`, EventStore `04682ea→4fafcb9`, and FrontComposer `2e33757→07bfc22`. The bumps come from `8431d992` (Story 5.1 closure), `04f68c60`, and `dfa78e10`. `--ref f17027ac` passes, with no `references/` change in the story range.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
  summary: Run the hosted Story 5.2 route smoke once the Aspire EventStore fixture is healthy.
  evidence: `TenantsUiRouteSmokeTests` audit-context assertions (`:110-111`) only compiled. In every review pass, fixture setup timed out on EventStore `/alive` before any assertion ran.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
  summary: Verify focus restoration does not report a false missing origin when the page replaces its own URL mid-wait (unverified, would be medium).
  evidence: `tenantsFocus.js` `check()` and its 250 ms interval call `finish(false)` on any `href` change, including same-route canonicalization or a `ListRefreshed` recovery `NavigateTo`. A browser trace of an audit return whose Back URL differs from the canonical workspace URL would settle it.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
  summary: Restore in-flight detail command panels after an audit round trip by expanding their accordion item (Option C follow-up).
  evidence: Decision 2026-09-26 (Option A) accepted heading focus plus notice for detail command-flow and cursor returns. `TenantLifecycleCommandFlow`, `SetTenantConfigurationFlow`, and `RemoveTenantConfigurationFlow` already re-adopt a `RetainsAttempt` (RequestSent/Accepted) snapshot from their circuit `*AttemptTracker` on re-init. Their `FluentAccordionItem` in `TenantDetailPage.razor` may be collapsed on return, so the launcher cannot take focus. Expand the matching item when `auditFocus` names that flow, as `CreateTenantFlow` does, then browser-verify. Circuit-local state for terminal results, the member and metadata flows, and the list cursor was rejected as over-engineering, and it conflicts with the paging no-silent-reactivation invariant.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
  summary: Invalidate retained tenant audit rows and an open receipt when the caller changes in the same UI circuit.
  evidence: `TenantAuditPage` has no authentication-state subscription and retains `_snapshot` and `_selectedReceipt` on a same-route caller change. The same behavior exists at the Story 5.2 baseline in Story 5.1's audit page; a caller-switch test and a page-lifecycle fix belong with that earlier read surface.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
  summary: Replace the historical unauthenticated hosted route smoke with an authenticated FrontComposer route fixture.
  evidence: With the repository development JWT issuer, audience, and key supplied temporarily, Aspire EventStore starts and all six `TenantsUiRouteSmokeTests` reach their assertions, but each fails because the unauthenticated shell prerenders `data-testid="fc-scope-blocked"` instead of the expected page marker. Without those temporary settings, the fixture times out at EventStore `/alive` before assertions. The route tests need a valid caller and scope to inspect the pages.

## Deferred from: code review of spec-5-2-reach-scoped-audit-evidence-from-context.md (2026-09-27)

Story range `fc147e3e..HEAD`.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
  summary: Un-defer the CI story gitlink gate so `validate-story-gitlinks.py` runs against changed story files, not only its own regression suite.
  evidence: Decision 2026-09-27 ("Declare + enable CI gate"). Story 5.2 is the fifth undeclared-bump occurrence; story commit `3ba48896` moved EventStore and FrontComposer without saying so. The fourth occurrence (Story 3.4, 2026-08-25) was the pre-agreed trigger. `.github/workflows/story-guards.yml` currently runs only `tests/scripts/test_validate_story_gitlinks.py`. The gate must compare each story's `baseline_commit..HEAD` range, because bumps often land in a later `build(deps)` commit.

## Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-27)

Story range `33d6dcca..7ac52e8d` (rebased to `051f9f30..f8223524`).

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
  summary: Revisit strict opaque audit reference classification for incidental secret-shaped substrings.
  evidence: The pre-existing gateway and reference classifier reject an otherwise valid opaque event ID when it happens to contain `jwt` or `eyj`; this story did not introduce that policy.

## Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-28)

Story range `33d6dcca..7ac52e8d` (rebased to `051f9f30..f8223524`).

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
  summary: Add accents to the remaining unaccented French receipt strings and make apostrophes consistent.
  evidence: `TenantsResources.fr.resx` still carries unaccented receipt copy from 2026-06-06 (`La preuve d'audit est prete a citer.`, `perimee`, `reessayez`, `Reference de commande`). Story 5.3 accented only the strings it rewrote, so the file now mixes `’` with ASCII `'`.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
  summary: Decide whether the absolute audit timestamp should be culture-formatted, as the epic's "culture-aware" wording suggests.
  evidence: `TenantAuditReceipt.TimestampLabel` and the grid both use the fixed `yyyy-MM-dd HH:mm:ss 'UTC'` pattern from the Story 5.1 UTC correction; the French receipt renders the same string.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
  summary: Move typed audit identifiers from a deny-list to an allow-listed character set with a confusable-skeleton check.
  evidence: Decision D5 (2026-09-28): stop adding characters one at a time; this was the seventh round. `TenantAuditSupportSafety.IsSafe` still admits phone numbers written with `.`, `/`, `tel:` or a leading space (`202.555.0100`); a fullwidth `％40` (NFKC turns it into `%40` only after the `%` check); and the dividers U+2503, U+275A, U+01C0, U+23D0, U+204F and U+061B. These values can reach the grid target, the receipt and the copied summary.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
  summary: Bind an in-flight correction open to the tenant route that initiated it.
  evidence: `OpenCorrectionAsync` captures only `_correctionOpenGeneration`; a tenant route change clears page state without incrementing that generation, so a late projection result can reuse a same-reference row from the new tenant. This behavior predates Story 5.3.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
  summary: Restore visible focus when a viewport downgrade removes an active correction panel.
  evidence: `RenderViewportChangeAsync` clears `_activeCorrectionIntent` and its focus reference without scheduling a return to the launcher. The panel-removal behavior predates Story 5.3.

## Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-29)

Re-review range `f8223524..38da46a6`.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
  summary: Return focus when `ClearPaging()` or a viewport downgrade removes an active correction panel.
  evidence: On a `ListRefreshed` or `InvalidCursor` result, `LoadAsync` runs `CaptureCorrectionAuthority` (which keeps a still-Ready source) and then `ClearPaging()`. `ClearPaging()` nulls `_activeCorrectionIntent` and `_activeCorrectionFocusReference` without setting `_pendingCorrectionFocusReference`, so focus falls to `<body>` (`TenantAuditPage.razor:914-918`, `:1379-1388`). `RenderViewportChangeAsync` has the same gap, already recorded in the 2026-09-28 viewport entry. Both predate baseline `33d6dcca`. Each needs the pending-focus assignment that `CaptureCorrectionAuthority` now uses, gated on focus actually being lost.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
  summary: Return correction focus to the control that launched the panel, not the first matching reference in document order.
  evidence: `tenantsFocus.js` `focusCorrectionLauncher` searches `[data-correction-focus-reference]` and then `[data-receipt-focus-reference]` in document order. The grid renders before the receipt (`TenantAuditPage.razor:221`, `:265`), so a panel opened from the receipt's Start correction returns focus to the grid's matching button. The fallback added in `38da46a6` likewise prefers the grid's "View receipt" over the open receipt. A fix needs the launch origin threaded from `OpenCorrectionAsync` to the JS.

## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29)

Review range `55f3dc63..working tree`.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Confirm that a screen reader announces the first audit availability state when the shared control mounts.
  evidence: Unverified (maybe-false); medium if true. The flows mount `AuditAvailabilityState` on the NotStarted → Pending/Unavailable transition, so its `aria-live` container is inserted together with its first content, the same pattern as the baseline control. Some screen readers skip content inserted with a new live region. The flows keep their own lifecycle live regions. Settle with an NVDA and browser check of a Create command reaching events-stored; if it is silent, keep the live container mounted empty while the state is NotStarted.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Stop flow-level lifecycle recovery copy from naming recovery actions that the shared availability control may not render.
  evidence: Strings such as `Tenants.EditMetadata.Recovery.Degraded`, `Tenants.Configuration.Set.Recovery.*`, `Tenants.Configuration.Remove.Recovery.*` and `Tenants.RemoveMember.Recovery.*` read "Wait, retry status lookup, inspect audit when available, or escalate." They render beside the control (for example `EditTenantMetadataFlow.razor:153`) whether or not Escalate, Inspect audit or Continue read-only exist. This flow lifecycle copy predates Story 5.4, which removed the same promise from the shared control.

## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 2)

Review range `55f3dc63..1cdcc0a9`.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Stop an unmatched RemoveMember proof walk from leaving the audit state pending indefinitely.
  evidence: `ApplyRemovalProofMatch(matched: false)` keeps `AuditPending` (`TenantCreateCommandModels.cs:1155`) when the walk reaches the last page (`!audit.HasMore`, `RemoveTenantMemberFlow.razor:1135`) without any match, weak or strong. Running out of the page budget is already Delayed (`:1157`), and so is a last page with only weak matches. The Pending recovery set has no Escalate, so once the three-retry limit withdraws Refresh, Inspect audit is the only recovery left, and the state stays pending until a host refresh changes it. The walk and its matching predate Story 5.4, and the frozen spec keeps them. RemoveMember has no retention or expiry window, so a fix would move an unmatched attempt to Delayed after a bounded number of unmatched walks, or would first add an attempt deadline to the flow.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Treat a 404 status lag as a wait in the command dimension, not an assertive unable-to-verify.
  evidence: `TenantCommandGateway.GetStatusAsync` maps a 404 to `TenantCommandStatusResult.Pending` (null `Status`, `IsPending`). The `ApplyStatus` methods for Create, AddMember, ChangeRole, RemoveMember and Metadata still send every null status to UnableToVerify, announced assertively (`TenantCreateCommandModels.cs:255` and siblings). The baseline did the same. Narrowed by the Story 5.4 Review 3 decision (option b): the audit dimension now keeps its state on the lag through `TenantCommandAuditStates.FromStatusLookup`, so only the command dimension remains. Lifecycle and the configuration snapshots already handle `IsPending` and `IsRetryableFailure` as waits in both dimensions.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Decide once for the whole repo whether Severe (and Danger) status badges use `BadgeAppearance.Filled`, as DESIGN.md specifies.
  evidence: DESIGN.md `truth-state-badge` says "Tint (default) · Filled (Danger + Severe)". `AuditAvailabilityState.razor:27` and `TruthStateBadge.razor` both render Severe with `Tint`, and Story 5.4's spec asked for Tint. Changing only the audit badge would make it diverge from the freshness badge.

## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 3)

Review range `1cdcc0a9..e077e65e` (delta only).

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Seventh undeclared-bump occurrence for the CI story gitlink gate tracked above (Story 5.2 entry, "Un-defer the CI story gitlink gate").
  evidence: Commit `e077e65e` ("update audit availability and recovery specifications and sprint status") moved `references/Hexalith.Builds` `0610f783`→`85ca19bc`, `references/Hexalith.EventStore` `801f3e52`→`cc79c03a` and `references/Hexalith.FrontComposer` `7d3af61e`→`f86fb730` without a declaration. `validate-story-gitlinks.py` on the 5.4 spec exits 1, while `.github/workflows/story-guards.yml:35` runs only the guard's own regression suite, so CI stayed green.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Hand off to Hexalith.Builds. The G-6 validator tuple pins CommunityToolkit.Aspire.Hosting.Dapr `13.5.1-beta.767`, while the central catalog pins `.770`.
  evidence: At Builds `85ca19bc`, `Tools/validate-runtime-toolchain-evidence.py:21` says `.767` and `Props/Directory.Packages.props:151` says `.770`, so a real `validate_baseline` run fails with central package pin drift. The self-test (`test-runtime-toolchain-evidence-validator.py:90-101`) builds its catalog from `EXPECTED_TUPLE` and never reads the real props, so Builds CI stays green. No Tenants workflow runs this validator.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Hand off to Hexalith.Builds. Correct the false and stale claims in the 6-1 G-4/G-6 story record.
  evidence: `6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md:2379` says the 2026-09-06 baseline "remains byte-identical at SHA-256 `525615c6…`", but `Tools/runtime-toolchain-baseline.json` hashes to `62a6a555…` at both `0610f783` and `85ca19bc` (commit `aada815` rewrote it). Line 2385 claims catalog alignment. The "review the fresh packet" and "seven bound changes remain uncommitted" statements are stale.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Hand off to Hexalith.Builds. The historical 2026-09-06 G-6 baseline and packet no longer validate, and the README still points at them.
  evidence: `validate-runtime-toolchain-evidence.py:240` requires `approvedOn == "2026-09-27"`, and the schema pins the same date, so earlier evidence fails "Baseline approval date drift". `README.md:269` still names `Tools/runtime-toolchain-baseline.json` as the recorded exception. Keep a dated-baseline registry, or retire the old file explicitly.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Hand off to Hexalith.Builds. G-6 candidate and accepted evidence cannot be told apart.
  evidence: `validate-runtime-toolchain-evidence.py:307-308` distinguishes the modes only by `packet["status"]`, and no named acceptance record (who, when, pending-packet hash) is required. The schema's `status` is `enum ["pending","accepted"]` with no mode condition. The 2026-09-27 baseline carries `approvedBy`/`approvedOn` although the record calls it a candidate pin.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Hand off to Hexalith.Builds. Strengthen the G-6 self-test mode checks.
  evidence: `test-runtime-toolchain-evidence-validator.py:289-291` asserts only `returncode == 1`, so an uncaught-exception mutant passes. The checks are bare `assert` statements, which `python -O` strips, and `:382` prints a literal "22 scenarios". No tampering scenario runs with `candidate=True`.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Hand off to Hexalith.Builds. Record a disposition for the Aspire SDK split in the 2026-09-27 baseline.
  evidence: `runtime-toolchain-baseline-2026-09-27.json:12` certifies `aspireSdk` `13.5.4` while `pinAudit.appHostProjects` lists five AppHosts at `13.5.3`, including `Hexalith.Tenants.AppHost`, with no `aspireSdk` disposition. `dispositions` also has the case-duplicate keys `dapr` (object) and `Dapr` (string).
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Withhold the shared control's Continue read-only in the member flows while an owned attempt is still in flight.
  evidence: AddMember, ChangeRole and RemoveMember bind `OnContinueReadOnly="ContinueReadOnlyAsync"` unconditionally (`AddTenantMemberFlow.razor:111`, `ChangeTenantMemberRoleFlow.razor:132`, `RemoveTenantMemberFlow.razor:181`). Their own Continue read-only button is gated by `CanContinueReadOnly` (UnableToVerify or Degraded), but an ambiguous submission leaves `RequestSent` with `AuditUnavailable`, whose verb set includes Continue read-only. Clicking it resets to `Idle()`, drops the retry MessageId and releases the per-aggregate activity lease while the attempt may be in flight. This was pre-existing: the baseline `55f3dc63` had the same binding and the same Unavailable verb. Story 5.4 Review 4 (BH2) found it. The fix mirrors the Metadata, Lifecycle and configuration gate (`!IsOwnedCommandInFlight`).

## Deferred from: code review of 5-4-audit-availability-state-recovery.md (2026-09-30)

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md`
  summary: Confirm that the first mounted availability state is announced by assistive technology.
  evidence: Unverified and medium if true. `AuditAvailabilityState` inserts its `aria-live` container together with the first `Pending` or `Unavailable` content. Settle with an NVDA/VoiceOver browser check of a `NotStarted` transition; if silent, keep the live container mounted empty before the first state.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md`
  summary: Decide whether `TenantAuditReceipt.FromEntry` should accept authoritative lifecycle/provenance or be removed.
  evidence: Pre-existing to the current Story 5.4 refinement. `TenantAuditRow.FromEntry` defaults lifecycle and provenance to `Unknown`, while receipt completeness requires `Current` and `ProjectionBacked`, so the public factory cannot produce `Ready`. Resolution requires deciding whether callers may supply authoritative metadata or whether the misleading factory should be retired.

## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery-2.md (2026-09-30)

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-2.md`
  summary: Give the legacy Story 5.4 artifact a baseline that resolves, or formally retire its guard role.
  evidence: `5-4-audit-availability-state-recovery.md:3` pins `baseline_commit: a5ca6e3f548e89b28a37826be721d9ef9f7cd51a`, which is not a commit, so `scripts/validate-story-gitlinks.py` fails closed on it. The intended `a5ca6e38` (Story 5.3, 2026-06-06) predates the `references/` layout, and correcting it gives 7 "absent at baseline" failures. The work range is audited by `spec-5-4-understand-audit-availability-and-recovery.md` (`55f3dc63..HEAD`, which passes with Builds, EventStore and FrontComposer declared) and by `spec-…-2.md` (`d728ff46..HEAD`, which passes). The 2026-09-30 completion note in the legacy story says the story guard "reported no pointer changes"; that result came from the follow-up spec. The owner should choose between repointing the baseline to `55f3dc63` and marking the legacy artifact exempt from the guard.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-2.md`
  summary: Refresh the stale parts of the legacy Story 5.4 record.
  evidence: `5-4-audit-availability-state-recovery.md:211` still reads "Outcome: Approve (status → done)" under a `Status: review` story. The ticked 2026-09-30 patch anchors point at `AuditAvailabilityState.razor:299/:220/:51`; the current locations are `:287-376`, `:236-243` and `:56`. The 2026-09-30 hardening has no Debug Log References with exact commands, and the "three mutation rejections" are never named, so the evidence cannot be reproduced.

## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery-3.md (2026-09-30)

Review range `55fc6f91..a4a1ce13`.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`
  summary: Restore CI test execution. `ci / build-and-test` fails at "Validate package consumer references", so no Tenants test tier has run in CI since at least 2026-09-29.
  evidence: "Package validation failed: Hexalith.Tenants.Server.0.0.0-ci-test.nupkg: dependency boundary includes host, samples, tests, or other forbidden projects: ['Hexalith.EventStore.ServiceDefaults']". This comes from the reusable `domain-ci.yml:350` step, after which Tier 1 and Tier 2 report no failure count. `Hexalith.Tenants.Server.csproj:6` references `Hexalith.EventStore.Server` from source at the bumped EventStore gitlink. First seen in run `36590317356` (`e077e65e`). It is still red on `main` (`36747920099`, `a4a1ce13`), and PR #49 was merged red. The fix is either in the EventStore package graph or in the boundary allow-list; decide which, then bump the gitlink.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`
  summary: Repair `validate-remove-focus-in-chromium`, which aborts in CI with Chrome 153 (core dump, exit 134) on every `main` push.
  evidence: `validate-tenants-focus-browser.sh` gets "Aborted (core dumped)" from `--headless=new --disable-dev-shm-usage --disable-gpu` in runs `36559824612` (`55f3dc63`) through `36747918865` (`a4a1ce13`). The workflow last passed on 2026-09-22 (`35782893330`), and local Chrome 154 passes. A runner sandbox restriction is suspected but unverified; settle it by uploading the `${output_path}.stderr` Chrome writes.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`
  summary: Let the browser harness run against the local Debug build, so local UI evidence can be reproduced from the repository.
  evidence: `validate-tenants-focus-browser.sh:8-11` hardcodes four `obj/Release/net10.0/scopedcss` inputs, while local work is Debug-only. Story 5.4's Chrome 154 evidence came from an uncommitted `/tmp/story-5-4-browser-debug.sh` copy with Debug paths. A configuration variable that defaults to Release would keep CI unchanged.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`
  summary: Some bUnit `WaitForAssertion` checks in the UI suite time out under load.
  evidence: `TenantDetailSurfaceTests.Detail_lifecycle_actions_fail_closed_while_authorization_is_pending` (`:1924`) failed with check count 85 and render count 86 in a full MTP run (3,585/3,586) while four review agents were running; it passes 3 of 3 runs on its own. Story 5.4's verification report also records an unnamed "metadata-confirmation" timeout (3,579/3,580) that passed on rerun. Neither test is in the reviewed diff. Name the tests, then raise their per-call timeouts or make their waits event-driven.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`
  summary: `validate-story-gitlinks.py` still exits 1 on the legacy Story 5.4 artifact. This recurs the 2026-09-30 entry above; add no separate action.
  evidence: `5-4-audit-availability-state-recovery.md:3` `baseline_commit a5ca6e3f…` is not a commit. The spec-3 range (`55fc6f9..HEAD`, no pointer changes) and the primary spec range (`55f3dc6..HEAD`, Builds, EventStore and FrontComposer declared) both pass.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-4.md`
  summary: Reconcile dotnet formatter same-line-brace expectations with the Hexalith baseline requirement for Allman braces.
  evidence: The supplementary read-only dotnet format whitespace check on the three changed UI test files exited 2 with 299 WHITESPACE diagnostics before review amendments; it requested same-line braces for existing Allman blocks and four new blocks matching that required style. The exact command and result are in the 2026-10-01 addendum to story-5-4-re-review-verification-2026-09-30.md. A policy correction belongs in separate repository maintenance; no formatting configuration or build/test gate was weakened here.

## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 5)

Review range `54ceb3e1..697697f9`.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Use the retry-aware `TenantCommandAuditStates.FromSubmission(result, retriedMessageId)` in Lifecycle, Set configuration and Remove configuration.
  evidence: Pre-existing; the 2026-10-01 fix pass wired it in AddMember, ChangeRole, RemoveMember and Metadata only. `TenantLifecycleCommandFlow.razor:1315`, `SetTenantConfigurationFlow.razor:698` and `RemoveTenantConfigurationFlow.razor:937` still call `FromSubmission(result)`. Their tracked gateways return `FailedWithKey` with no MessageId for failures before dispatch (`TenantCommandGateway.cs`, `UnavailableTenantCommandGateway.cs`), so a re-dispatch of a retained, possibly delivered identity that fails that way reports NotStarted and hides the control. Lifecycle's re-dispatch site (`:1550`) is explicit. The configuration flows first need the tracker to report whether `BeginDispatch` resumed a retained identity.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Keep the localized reason of a keyed failed submission in AddMember and ChangeRole.
  evidence: Pre-existing. The failure arm sets `SafeMessage = result.SafeMessage, SafeMessageKey = null` (`AddTenantMemberFlow.razor:558`, `ChangeTenantMemberRoleFlow.razor:617`), so `FailedWithKey("Tenants.Commands.Unavailable.InvalidTrackingReference")` renders no explanation. It is reachable only when the reused message id fails `TryResolveMessageId`. The fix is to pass `result.SafeMessageKey` through, as RemoveMember and Metadata already do.

## Deferred from: build review of spec-5-4-understand-audit-availability-and-recovery-5.md (2026-10-01, review 1)

Review range `c9cd9045..` working tree (the Review 5 patch closure).

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-5.md`
  summary: Drop a late status lookup in AddMember, ChangeRole and RemoveMember when Continue read-only has reset the attempt, as Create now does.
  evidence: Pre-existing; this spec fixed Create only. `RefreshCommandStatusCoreAsync` applies `SetSnapshot(_snapshot.ApplyStatus(status))` after the await to whatever snapshot is current (`AddTenantMemberFlow.razor:658-660`, `ChangeTenantMemberRoleFlow.razor:717-719`, `RemoveTenantMemberFlow.razor:998-1000`). Continue read-only stays clickable during the lookup: the shared control renders it for AuditUnavailable whenever the delegate exists (`AuditAvailabilityState.razor:275`), and the flow's own button shows while the state is UnableToVerify or Degraded. A held lookup that resolves `EventsStored` after Continue read-only leaves ProjectionPending/AuditPending with no Intent or MessageId on the reset panel. In AddMember, `UpdateCommandActivityForSnapshotAsync` (`:681`) then re-raises the tenant's command-activity lease, because ProjectionPending retains activity (`TenantCommandFlowGuard.cs:16-20`). No test holds a lookup across Continue read-only in these flows.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-5.md`
  summary: Do not re-apply a status lookup to a confirmed attempt in AddMember, ChangeRole and Create.
  evidence: Pre-existing. In AddMember and ChangeRole, a click merged into a running refresh is replayed through `RefreshCommandStatusCoreAsync` after that refresh has confirmed the attempt. The only check is for a non-blank MessageId and CorrelationId (`AddTenantMemberFlow.razor:599-608`), and `TenantAddMemberCommandSnapshot.ApplyStatus` / `TenantChangeRoleCommandSnapshot.ApplyStatus` (`TenantCreateCommandModels.cs:481`, `:687`) have no terminal guard; RemoveMember's has one (`:970`). Create has no in-flight refresh gate, so its form Refresh (`CreateTenantFlow.razor:88-92`) can confirm while a slower audit-control lookup is pending. Its message-id guard passes, because Confirmed keeps the id, and the late Processing reopens the attempt as Accepted after tracking was forgotten. Metadata got the equivalent guard in this spec (`IsRefreshableAttempt`).
- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-5.md`
  summary: Unverified (would be medium): Create's projection reconcile may write UnableToVerify onto the panel that Continue read-only reset.
  evidence: The message-id guard covers only the status apply. If Continue read-only lands during the reconcile's `ProjectionEvidenceProvider` await, the next `_snapshot.Intent.TenantId` throws, and the catch arm writes an Assertive `UnableToVerify` from the Idle snapshot (`CreateTenantFlow.razor:590-609`). In the only host, `TenantsWorkspace.razor:188-189`, `OnProjectionRefreshRequested` re-renders the flow to AuditPending (no Continue read-only verb) before the reconcile reads, so the path looks unreachable there. To settle it, write a host-level test, or a bUnit test without the callback, that clicks Continue read-only during a held `ProjectionEvidenceProvider` and asserts the panel stays Idle.

## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 7)

Review range `55f3dc63..fa489329`, chunk 1 (`src` minus `State/` and `Services/`).

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Give the audit availability badge `Shape="BadgeShape.Rounded"` and an `IconLabel`, as DESIGN.md's truth-state-badge anatomy requires, as part of the repo-wide badge alignment decision.
  evidence: `AuditAvailabilityState.razor:27-34` sets neither, so the Fluent default shape applies. It follows `Components/Shared/TruthStateBadge.razor`, as the spec's Code Map directs, and no badge in the repo sets a shape (DESIGN.md:59-60, 178, 188). The visible text and `aria-label` already carry the state name, so this is visual conformance only. Decide it together with the open Review 2 Tint vs Filled deferral, so every badge changes at once.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Replace the source-text pin of the seven flows' denied-read Inspect-audit wiring with a rendered theory.
  evidence: `AuditAvailabilityStateTests.cs:924-966` asserts `=> AuditReadDenied ? null : CommandAuditEntryPointTemplate;` through `File.ReadAllText`. Only Create has rendered coverage (`CreateTenantFlowTests.cs:226-256`), and `RemoveTenantMemberFlowTests.cs:67-77` checks only that the entry point is absent, which an empty `[data-recovery-verb='inspectaudit']` shell would also pass. Add one theory across AddMember, ChangeRole, RemoveMember, Metadata, Lifecycle, Set configuration and Remove configuration that asserts no `inspectaudit` recovery renders when the audit read is denied.


- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Let the existing browser validator consume the selected local build configuration instead of hardcoding Release CSS paths.
  evidence: Build completion review BH8 verified that Release-only correction, receipt, and page paths already exist at baseline 55f3dc63b6ce10bb0afdf929d026fa07cffd9105. The local Debug acceptance lane needs a temporary adapter to consume fresh CSS; no build-selection policy was changed by Story 5.4.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Preserve and print Chromium stderr when the browser validator exits before its validation-result checks.
  evidence: Build completion review BH9 traced the pre-existing set -e browser invocation and EXIT cleanup. A nonzero browser process exit bypasses stderr printing and the trap deletes the redirected diagnostic file. This behavior is present at baseline 55f3dc63b6ce10bb0afdf929d026fa07cffd9105.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Resolve or explicitly retire the legacy Story 5.4 artifact's non-resolving validation baseline.
  evidence: Build completion review BH10 verified that python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md exits 1 because a5ca6e3f548e89b28a37826be721d9ef9f7cd51a is not a commit. The historical baseline is preserved; the current owning spec's canonical baseline passes its guard.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: Split the fourteen pre-existing public command-model types into their own named C# files.
  evidence: Build completion review BH11 compared TenantCreateCommandModels.cs with baseline 55f3dc63b6ce10bb0afdf929d026fa07cffd9105 and found all fourteen public types already present. The monolithic file remains contrary to the Hexalith one-type-per-file rule; Review 7's fixes did not edit that file.

## Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 8)

Review chunk: `fa489329..78e09184` for `src` and `tests`, plus the full-story diff of the browser harness and of the composition, workspace and generated-surface tests.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
  summary: After a first-attempt activity-lease refusal in RemoveMember, a second submit shows "This attempt can no longer be tracked", although nothing was dispatched.
  evidence:
  - The refusal sets `UnableToVerify` with a null MessageId (`RemoveTenantMemberFlow.razor:853-862`).
  - The next submit enters the branch for `UnableToVerify` with a null MessageId (`:772-791`), which shows `Tenants.Members.Submit.TrackingLost`. The operator must Cancel and reopen to try again.
  - Both branches are the same at baseline `55f3dc63`, except for the audit state Story 5.4 changed.
  - `RemoveTenantMemberFlowTests.Retry_refused_by_the_activity_lease_…` (row `isRetry=false`) stops before the second submit, so no test pins this.
  - Fix direction: keep a pre-dispatch busy refusal resubmittable, for example a Blocked-style state that keeps the Intent, and add a test that the second submit dispatches once the lease is granted.

## Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)

- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
  summary: Recheck current circuit authority and membership immediately before tenant correction confirmation dispatch (high).
  evidence: R-B6; the unchanged Story 5.6 CorrectionStartPanel.SubmitAsync dispatches its stored snapshot directly. Story 5.5 refreshes start and handoff and performs no dispatch; a confirmation-time authority loss remains outside those start gates.
  resolution: Resolved by Story 5.6 commit `17538e07`; Confirm now performs a fresh authorized projection read before admission.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
  summary: Decide and expose redacted last-owner consequence facts in tenant correction previews (medium).
  evidence: R-B7; the baseline preview contains no owner count or explicit last-owner warning. The original review assumed a last-owner domain rejection; ChangeUserRole permits last-owner demotion, so the correction must warn about potentially leaving no owner, matching the member flow.
  resolution: Resolved by Story 5.6 commit `93b0b96d`; the authorized capture carries a redacted owner count. The follow-up preview copy warns that demoting the last owner can leave no owner and that the command is not blocked, matching the member flow and domain behavior.

## Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)

Review diff: `bcfc0788..f44e19e7` (story commits `cefefa26` and `f44e19e7`, merged as PR #51 `09c90f08`).

- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
  summary: A submitted, tracked tenant-correction preview is still unmounted by refresh, list-refresh, row loss and viewport narrowing (pre-existing).
  evidence:
  - These paths in `TenantAuditPage.razor` clear `_previewCorrectionIntent` without checking `HasSubmittedCorrection`:
    - `CaptureCorrectionAuthority` (`:1653`), when the source row is absent or not Ready, for example after the corrective event pushes it off page 1.
    - `ClearPaging` (`:1407`), on `ListRefreshed`/`InvalidCursor`.
    - The viewport handler (`:645`).
  - The preview is also rendered inside the `ShouldRenderRows` block (`:214`), so a Loading snapshot unmounts it.
  - The operator loses the lifecycle, tracking handle and proof link of a dispatched command.
  - At baseline `bcfc0788` the same paths unmounted the submitting `CorrectionStartPanel`. Commit `f44e19e7` retains submitted previews only on the new role and start paths.
  - Story 5.6 owns preview lifecycle retention. The Story 5.5 review settled the "submitted" signal as `CorrectionStartPanel.HasSubmitted` (set in `SubmitAsync`, never cleared). It is scoped to one panel instance, so any unmount (including these paths and Cancel) loses it; retention needs a page-owned signal.
  resolution: Resolved by Story 5.6 commit `17538e07`; the circuit-scoped correction attempt tracker retains the submitted snapshot and MessageId across remounts.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
  summary: CI still runs no Tenants test tier (already tracked above; reconfirmed).
  evidence: Run 36970095241 on `09c90f08` (`main`, after the PR #51 merge) fails `ci / build-and-test` at "Validate package consumer references". Aspire and performance tests are skipped. All Story 5.5 test evidence is local only. See the 2026-09-29 entry "Restore CI test execution".

## Deferred from: build review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)

Review range: story baseline `4f426e59` to the fix pass later committed as `31c2d2e1`. Finding IDs refer to that build review's triage log.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
  summary: Retain a submitted correction preview across audit refresh, row loss, and viewport changes (already tracked above).
  evidence: BH3/EH1 repeat the "submitted, tracked tenant-correction preview is still unmounted" entry in the code-review section above; track it there.
  resolution: Resolved by Story 5.6 commit `17538e07`; see the submitted-preview retention entry above.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
  summary: Require attempt-specific provenance before linking a corrective audit event as proof.
  evidence: BH5; the existing Story 5.6 proof search matches type, tenant, target, and time, so a separate command can produce a false proof match.
  resolution: Resolved by Story 5.6 commit `17538e07`; the authorized audit DTO has no attempt association, so the panel shows missing support and creates no paired links.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
  summary: Give the corrective audit proof link a matching destination in the audit grid.
  evidence: BH6; the existing Story 5.6 link points to an audit-reference fragment while the grid renders no matching id.
  resolution: Resolved by Story 5.6 commit `17538e07`; the unproven fragment link was removed.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
  summary: Show the original evidence timestamp in the Story 5.6 final preview.
  evidence: BH7; the preview carries originalTimestamp in its intent but filters it from visible preview fields, although the start surface shows it.
  resolution: Resolved by Story 5.6 commit `17538e07`; the preview shows the verified original evidence time.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
  summary: Verify the composed audit grid, receipt, start, and preview at narrow widths in an authenticated browser run.
  evidence: BH12; the isolated browser fixture cannot prove the composed layout. This is a medium-impact possibility without demonstrated overflow; a live authenticated browser run across measured widths would settle it when the existing query-gateway authentication limitation is resolved.

## Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)

Review diff: `9bad98d9..31c2d2e1` (re-review of the review-fix commit).

- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
  summary: Cancelling during an in-flight correction submission loses the panel's `HasSubmitted` flag, so a new attempt can be sent (pre-existing).
  evidence: In `CorrectionStartPanel.razor:162`, the Cancel button is never disabled while `_isSubmitting`. `CloseCorrectionAsync` then unmounts the preview. Because `HasSubmitted` is scoped to the panel (resolved option (a)), `TenantAuditPage.HasSubmittedCorrection` becomes false, and Start → handoff → Confirm can dispatch a second attempt with a new MessageId while the first POST is still in flight. This belongs with the Story 5.6 preview-retention item above.
  resolution: Resolved by Story 5.6 commit `17538e07`; admission retains the MessageId and lease before gateway I/O, and Cancel invalidates only an unadmitted Confirm.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
  summary: Ambiguous, FailedWithKey and RejectedWithKey correction submissions show no localized reason and offer no retry with the same message ID (pre-existing).
  evidence: `TenantCorrectionPreviewSnapshot.ApplySubmissionFailure` (`:194`) is unchanged since the story baseline. It copies neither `SafeMessageKey` nor `MessageId`. `TenantCommandSubmissionResult.Ambiguous` returns `RequestSent` with a key and a reusable MessageId, so the preview stays in "request sent" with no message. Refresh needs tracking, and Confirm is blocked by `CanSubmit`. The gateway contract says a retry with the same ID is safe, but this panel never offers one.
  resolution: Resolved by Story 5.6 commit `17538e07`; the retained attempt preserves the MessageId and safe key and Refresh retries only the same ID.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
  summary: `TenantAuditPageTests.Loading_receipt_renders_while_focus_probe_and_authoritative_read_are_pending` fails when run alone and is flaky in the full suite (pre-existing, Story 5.3 `38da46a6`).
  evidence: Running `Hexalith.Tenants.UI.Tests -method '*Loading_receipt_renders_while_focus_probe_and_authoritative_read_are_pending'` failed 3 of 3 times at `31c2d2e1` with "gateway.Requests.Count should be 2 but was 1". The verification-gap layer saw 1 failure in 4 full-suite runs. The `WaitForAssertion` at `:990` waits on a stub counter that is only re-checked on renders; use the repository's `SpinWait.SpinUntil` idiom.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Verify whether viewport observation can change concurrently between the final safety check and correction attempt admission.
  evidence: A Fluxor effect writes the circuit-scoped observation while the Razor event reads it; dispatcher scheduling evidence or a controlled concurrency test is needed to establish whether the synchronous admission section can interleave. If reachable, a command could dispatch after the viewport becomes unsafe.
  resolution: Resolved by Story 5.6 commit `93b0b96d`; the post-read admission section runs on the renderer dispatcher and rechecks viewport safety inside its critical section.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-03)

Review diff: `11e65e37..17538e07` (story commit `17538e07`).

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: The global-administrator correction still links corrective proof by event type plus a later timestamp (pre-existing; Story 5.7).
  evidence: `GlobalAdministratorCorrectionSnapshot.WithCorrectiveProof` (`:679`) builds a `TenantCorrectionProofLink` from any later row. `GlobalAdministratorCorrectionPanel.razor:114` renders it as a `#audit-…` fragment link. Story 5.6's Design Note says that kind of target/time match proves nothing, and the new `TenantCorrectionProofLink.cs:3` doc comment ("backed by attempt-specific evidence") is false for this, its only producer. Story 5.7 must replace it with deterministic attempt provenance or the truthful missing-support state.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: CI still runs no Tenants test tier (already tracked; reconfirmed).
  evidence: Main CI fails at "Validate package consumer references", so every Tier 1 step is skipped. The 3,797-test UI run cited for Story 5.6 is local evidence only. See the 2026-09-29 entry "Restore CI test execution".
- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: The Story 5.6 preview browser validation never runs in CI (pre-existing).
  evidence: Story Guards aborts with exit 134 ("Aborted (core dumped)") at the first Chromium launch in `validate-tenants-focus-browser.sh` (`:266`), before the preview fixture steps (`:330-360`). The rendered EN/FR preview, focus, forced-colors and egress checks therefore run locally only.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 2 (2026-10-03)

Review diff: `17538e07..93b0b96d` (fix pass `93b0b96d`).

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: The story gitlink validator exits 1 on the working tree because `references/Hexalith.Builds` and `references/Hexalith.FrontComposer` were moved outside this story.
  evidence: The default run reports Builds `3639c8d → c16249a` and FrontComposer `24033f7 → bf40099` as undeclared. Both are unstaged; `--ref 93b0b96d` passes and the committed range moves no pointer. Keep both pointers out of any Story 5.6 commit, or revert them and commit the bump separately as `build(deps)`.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Decide how an authorized correction result can be inspected after the submitting owner demotes themselves.
  evidence: TenantsBffComposition.ComposeTenantCorrectionProjectionsAsync returns unavailable captures for nonowners; its owner/global authorization predicate is unchanged from baseline 11e65e3. Correction confirmation properly refuses redacted postconditions, leaving self-demotion unconfirmed until another authorized actor inspects it; a result-read policy must preserve authorization.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Make terminal EventStore advisory status publication recoverable after a failed write or crash.
  evidence: At EventStore baseline b51978d and in the current tree, CompleteTerminalAsync saves terminal idempotency and removes the pipeline before WriteAdvisoryStatusAsync, which logs ordinary write failures; exact terminal duplicates return cached results without status writes. An outage can leave committed execution without an available terminal proof. Recovery must retain the command's exact range and never substitute the aggregate head.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Preserve ambiguous delivery identity and aggregate admission in the ordinary add-member and change-role flows.
  evidence: Those consumers copy the pre-existing Failed result directly and release activity even for uncertain 408/429/5xx or transport delivery; the new correction ambiguity flag does not change their behavior. Add controlled uncertain-delivery and same-ID recovery coverage when implementing this separate flow work.
- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Pin FrontComposer deferred editor-focus preservation when an abandonment warning reopens before its queued callback runs.
  evidence: fc-focus.js:160 guards restoreEditedOrigin, but form-abandonment-guard.spec.ts waits for editor focus before reopening and the bUnit tests only observe JS calls. A controlled requestAnimationFrame case should assert focus stays on the reopened warning; this is a test gap in the separately committed FrontComposer dependency update.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Validate or quarantine malformed persisted envelope ranges before EventStore command-resume publication.
  evidence: Current-head blind finding BH9 traces ResumeFromEventsStoredAsync through HasVerifiedEventRange and PublishEventsAsync. Wrong envelope tenant/domain/aggregate/sequence/correlation omits correction proof but still publishes; the baseline b51978dd1d2a3721ad239db2623e1560377c7583 already publishes that loaded range unconditionally. Define platform publication rejection/quarantine and evidence-preservation behavior separately from the optional status-proof extension.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Verify whether correction delivery/status continuations outside the renderer can corrupt visible state during remount or expiry (medium, unverified).
  evidence: Final BH4 observes ConfigureAwait(false) continuations assigning component fields. The actual page unmounts on cleared intent and the tracker rejects mismatched or terminal regression, so user-visible corruption was not established. A controlled late status response overlapped with parameter change, expiry notification, and remount must demonstrate or refute a concrete outcome before any state-management patch.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 3 (2026-10-04)

Diff reviewed: Tenants `c0afce2e..af69f36d` (code only) and EventStore `b51978dd..b0464255`, `ff2fcc9f`, `865cd9e4`.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Mark the older Story 5.6 record as superseded on audit proof linking.
  evidence: `5-6-preview-and-confirm-correction-with-linked-proof.md` is still `Status: done`, and its AC 8 and Completion Notes (`:188-194`) claim audit proof linking and support-safe proof links. The current implementation always applies `WithCorrectiveProof(null)` (`CorrectionStartPanel.razor:1031`), and the tenant confirmation copy says the audit association is unavailable. A reader of the older record gets a false picture of what shipped.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: CI still runs none of Story 5.6's pass-3 tests (already tracked; reconfirmed).
  evidence: CI at `b6a271ed` fails at package validation (`Hexalith.Tenants.Server` → forbidden `Hexalith.EventStore.ServiceDefaults`) before Tier 1. Story Guards run 37223763857 aborts at the first Chromium launch (`validate-tenants-focus-browser.sh:226`, exit 134). That is before the new recovery-focus, forced-colors and missing-tabindex checks (`:330-375`). This extends the 2026-09-29 "Restore CI test execution" entry and the 2026-10-03 Story 5.6 entries.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 4 (2026-10-05)

Diff reviewed: Tenants `e3e3af7d..560ac28f` and EventStore `865cd9e4..979de6f3` (anchored at root `88602b9b`, EventStore `f9d7dde4`).

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Show the global-administrator correction panel's delivery-retry label only when a retry is actually possible.
  evidence: `GlobalAdministratorCorrectionPanel.razor:469` (`RefreshActionText`) picks the delivery-retry text whenever `IsSubmissionAmbiguous && CorrelationId: null`, even when `CanRetryAmbiguousCorrectionDelivery` is false, and uses a null-only pattern. Story 5.6 pass 3 fixed the same defect in `CorrectionStartPanel` (`CanRefresh && no correlation`). Global-administrator correction belongs to Story 5.7.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Document `retryable`, `recoveryReasonCode` and `drainAttemptCount` in the EventStore status API reference.
  evidence: `CommandStatusResponse` (`src/Hexalith.EventStore/Models/CommandStatusResponse.cs`) serializes all three, but `docs/reference/command-api.md:308-322` lists none of them. A consumer cannot tell that an automatic retry is still armed on `PublishFailed`. The fields arrived in EventStore `86308550`, before the story baseline.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Declare the command-status endpoint's 409 in its OpenAPI metadata.
  evidence: `CommandStatusController.cs:58-64` declares 200/400/401/403/404/429 but not 409, although `CreateAmbiguityProblemDetails` returns 409 (`:133`, `:152-153`). `CommandDocumentationTransformer` does not add it either, so the generated OpenAPI disagrees with `command-api.md:389`.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Move the remaining EventStore docs from `/status/{correlationId}` to the messageId route.
  evidence: `docs/guides/security-model.md:209-210,280`, `docs/concepts/command-lifecycle.md:210`, `docs/getting-started/first-domain-service.md:277` and the brownfield docs still name the correlation ID as the status key. `CommandStatusController` routes `{messageId}`, and correlation lookup is a bounded compatibility path that can return 409.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 6 (2026-10-05)

Diff reviewed: Tenants `2306feba..2c04af17` and EventStore `f9d7dde4..7c3243e1` (rebased mid-review to `7dcc4756` on EventStore `origin/main`; root still pins `f9d7dde4`).

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Finish converting the EventStore replay reference to messageId semantics.
  evidence: `docs/reference/command-api.md:425` says `originalCorrelationId` "matches the path parameter", but `ReplayController` resolves the path value as a messageId first and returns `archivedCommand.CorrelationId ?? correlationId`. So they differ when a messageId is passed. The replay error table (`:448`) omits the correlation-ambiguity 409 that `ReplayController` returns. These rows predate the story; pass 4 converted only the polling guidance. Pass-12 BH7 reconfirmed this and notes that `OriginalCorrelationId` and `OriginalMessageId` are separate response fields; pass 13 folds that duplicate entry into this work item.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Make the command-status 409 ProblemDetails detail match every ambiguity it reports.
  evidence: `CommandStatusController.CreateAmbiguityProblemDetails` (`:209`) always says "The correlation identifier maps to multiple commands. Query again using the command MessageId." It also serves the case where one messageId matches records in several authorized tenants or several legacy records (`:152-153`), where a messageId retry cannot help. Pass-12 BH9 additionally traces a direct message match in one tenant colliding with another tenant's correlation index; pass 13 folds that duplicate entry into this work item. The updated `command-api.md` 409 row already gives the general remediation; the runtime copy is pre-existing and EventStore-owned.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 7 (2026-10-05)

Diff reviewed: Tenants `c0b6f16d..50fc6257` and EventStore `d48e1aeb..ad8fe3ba` (root `50fc6257` was pushed mid-review).

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Pin the value, not just the presence, of the tenant correction delivery outcome passed to `EndDelivery`.
  evidence: `TenantCorrectionAttemptTracker.EndDelivery` (`src/Hexalith.Tenants.UI/State/TenantCommands/TenantCorrectionAttemptTracker.cs:126`) now requires an outcome, but passing `request` or `attempt.Snapshot` instead of `next`/`unavailable` at `CorrectionStartPanel.razor:868`/`:933` still compiles and passes. `SetRetainedSnapshot` re-applies the right snapshot one statement later and bUnit is single-threaded, so no test can see the window in which another panel's `TryStartRetry` could send a duplicate same-id delivery. Closing it needs a production change, such as having `SetRetainedSnapshot` render only what `EndDelivery` retained.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 9 (2026-10-05)

Diff reviewed: Tenants `e2c297b2..680bee32` and EventStore `738da5c9..0c6bb5c3`. During the review a peer rebased `0c6bb5c3` to `2f7e044b` (identical patch) and re-pinned the root in `c3234b10`, then `6b6338f0`.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Correct the EventStore `failureReason` row, which says "PublishFailed status only".
  evidence: `docs/reference/command-api.md:325` dates from EventStore `6a901adf` (2026-03-01). `ConcurrencyConflictExceptionHandler.cs:51-60` and the `SubmitCommandHandler` coordinated-conflict writer put `FailureReason: "ConcurrencyConflict"` on `Rejected` records. Pre-existing; not caused by Story 5.6.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Normalize whitespace causation in `AggregateActor.CreateCommandProcessingIdentity` (medium, unverified).
  evidence: `CreateCommandProcessingIdentity` (`AggregateActor.cs:4704-4710`) maps only `null` to `MessageId`, while admission (`:652`) also maps whitespace. On the resume publish-failure path (`:4346`), a whitespace `CausationId` makes `IdempotencyChecker` → `identity.Validate()` throw before the drain record and reminder are written. The same call already existed at the EventStore story baseline `2c58ffda`. HTTP always sets causation = messageId. To settle it, list every non-HTTP entry point that builds a `CommandEnvelope` with a caller-supplied `CausationId`.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Give the EventStore shared-consumer authority Contracts test more headroom, or speed it up.
  evidence: `SharedConsumerAuthorityValidatorPassesForEveryTrackedMsBuildSurfaceAsync` timed out at 3m 00s in the pass-7 closure's first full Contracts run, then passed alone in 2m 49s. That is 11 s of headroom, so it is likely to flake in full or CI runs. EventStore-owned; not caused by Story 5.6.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 11 (2026-10-05)

Diff reviewed: the uncommitted Tenants working tree against root `d8558263`, and EventStore `46d7b2eb..8f34b395` (story files only).

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Carried, with no new work item; see "Restore CI test execution" above. Tenants CI never runs the strengthened same-row correction regression `RoleSelectionAndAnotherStartPreserveASubmittedPreviewAndItsTrackingHandle`.
  evidence: CI run `37298335264` at `d8558263` fails at "Validate package consumer references" and skips both Tier 1 steps. `story-guards.yml:55-59` only builds the UI test project, and `source-reference.yml` runs only the integration tests. So the pass-9 MV1 kill (`TenantAuditPage.razor:1552` forced to `competing: true`) is local evidence only. Pre-existing; not caused by Story 5.6.

## Deferred from: pass 12 workflow review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-05)

Diff reviewed: `/tmp/tenants-56-pass11-review-content-kr6d6_ah.diff` (699,840 bytes), containing the original Tenants story-baseline diff and the EventStore proof-path delta from its preserved `b51978dd` baseline. The claims spec was excluded and supplied separately only to the edge layer. Pass-12 BH7 and BH9 extend the pass-6 replay-reference and runtime-ambiguity work items above; pass 13 removed their duplicate entries here.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Restore content scanning or governed, content-bound retirement for EventStore evidence paths currently skipped by the secret guard (high).
  evidence: Pass-12 BH1: upstream EventStore 38efbefd5da65d538723f9f85eca6a186dfc0a2f added ExplicitEvidenceArtifactPathPattern; ReadTrackedText returns null for every matching evidence CTRF JSON, the named verification directory and previous-candidate.md before examining content. Bind any allowed retirement to verified artifacts and add injected-content controls; this is independent of the exact password-free GitHub SSH-identity exception.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Correct EventStore command-status documentation to include positive event counts on eventful rejections.
  evidence: Pass-12 BH6: command-api.md says eventCount is Completed-only, but CompleteTerminalAsync passes a positive count for Rejected. Both wording and behavior predate the preserved EventStore baseline; correction confirmation remains gated on Completed and verified proof.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Add real AggregateActor.GetEventsAsync count and cumulative-payload budget boundary tests.
  evidence: Pass-12 VG1: pre-verified gap in independent EventStore evolution commit 974a7fa33d70fdfb9dabb32c7d2416e4f0095853. AggregateActorGetEventsTests uses small arrays, projection tests mock the actor, and the only LegacyArrayLimit assertion calls EventStreamReader.RehydrateAsync. Invoke the real actor for excessive count and cumulative 64-MiB payload cases and require LegacyArrayLimit, so deleting arrayBudget.Add is detected.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 13 (2026-10-05)

Diff reviewed: Tenants `d8558263..5a519cd7`, and EventStore `8f34b395..55b2982e` (story file `docs/reference/command-api.md` only).

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Teach `scripts/validate-story-gitlinks.py` to bind three-column dependency-table rows, or document that tables are non-binding.
  evidence: `stated_targets()` (`:208`) matches only `ARROW_CHAIN` (`X -> Y`). For the 5.6 spec at `5a519cd7` it returns `{}`, so the run PASSes while four `| references/X | base | target |` rows are stale (EventStore `8f34b395` vs shipped `55b2982e`, FrontComposer, McpCli, Memories). The docstring says a stale pointer table is exactly what the guard catches. `tests/scripts/test_validate_story_gitlinks.py` has no pipe-table fixture. Add one that expects `[MISSTATED]`. Pre-existing; not caused by Story 5.6.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: A caller-chosen correlation ID in one tenant can force a 409 on another tenant's `messageId` status lookup.
  evidence: `CommandStatusController.GetStatus` (`:114-153`) counts a direct match in tenant A and an indexed correlation match in tenant B together, so `matches.Count == 2` returns 409. The body `correlationId` is caller-chosen, and the endpoint has no tenant selector. Tenants correction confirmation fails closed (unable to verify), so the impact is availability for multi-tenant callers. The `:3576` entry tracks only the wording, not the lookup behaviour. Pre-existing (`ddccb9b1`). EventStore-owned.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Extends the pass-6 entry "Make the command-status 409 ProblemDetails detail match every ambiguity it reports": the served error catalog has the same wrong copy.
  evidence: `src/Hexalith.EventStore/OpenApi/ErrorReferenceEndpoints.cs:94-97` describes `command-correlation-ambiguous` as "The tenant-scoped correlation identifier maps to multiple live commands" and remediates with "Use the MessageId returned by command submission". Neither covers a cross-tenant `messageId` collision. `docs/reference/problems/` has no page for this type. Pre-existing. EventStore-owned.

## Deferred from: pass 14 workflow review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-05)

Diff reviewed: `/tmp/tenants-56-review-content-3sylhkc4.diff`, 626,873 bytes; root `aeb23ad1` = `f9dc75c1`; EventStore `55b2982e` story paths.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Correct EventStore polling guidance that treats recoverable PublishFailed as terminal.
  evidence: Pass-14 BH10, extended by pass 17 at EventStore `07d1e23a6c5b06bbbb1fc8ddb5174cc3382d3d93`: command-api.md labels PublishFailed terminal at :380, :383, :400 (replay of terminal failure states), :470, :473, :475, and :477; docs/brownfield/architecture.md:152 and docs/brownfield/api-contracts.md:23 make the same claim. The runtime CommandStatusExtensions.IsTerminal also classifies PublishFailed as terminal, and CommandStatusController.cs:201-202 therefore omits Retry-After. Line :477 tells callers to stop polling, although a successful publication drain can later write Completed with command-specific sequence proof. Correcting :477 alone leaves the other documentation sites and runtime polling behavior inconsistent. Story 5.6's consumer maps PublishFailed to Degraded with refresh recovery, so it remains pending for verification rather than claiming correction success. The identical terminal-state sentence exists at preserved EventStore baseline b51978dd1d2a3721ad239db2623e1560377c7583:454, so this is pre-existing EventStore documentation work. Distinguish recoverable publication failure from exhausted recovery without duplicating the existing recovery-field documentation follow-up.

## Deferred from: spec-refresh-dependencies.md and spec-eventstore-3-117-1.md (2026-10-08)

Diff reviewed: the dependency refresh from its recorded baseline `8f6f8cb813255cc94ada95cda1a5e224c3b6bed0`, and the EventStore 3.117.1 continuation from `1846c7128cf0f6b16f9e62f38032bcab78e26e38`, with each spec's separate evidence/triage supplement. Both final evidence reports record root `1846c7128cf0f6b16f9e62f38032bcab78e26e38`; concurrent owning dependency checkouts are identified in those reports. This heading groups those existing findings, not a new combined review.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
  summary: medium; tenant index replay can overwrite newer index values while detail skips older sequences.
  evidence: BH1: TenantProjectionHandler.cs:156 has no per-aggregate sequence watermark; TenantIndexReadModel.Apply(TenantUpdated) overwrites Name. The older-replay test checks only detail against an empty index. This predates the EventStore upgrade; add persisted-index replay coverage when addressing the index watermark.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
  summary: medium; the custom Tenants /project route bypasses legacy projection evolution admission.
  evidence: BH2: Program.cs:173 calls the Tenants ProjectionDispatcher, which lacks admission checks before persistence. Published EventStore 3.115.0 already includes RequireLegacy in source revision 283b07a52c9c70e1c940164a7011ee8c3ad98b2d, so this is pre-existing. Restore equivalent rejection of unverified hints/versioned event metadata with route-level coverage.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
  summary: medium; recorded gitlink provenance rejects a valid new submodule addition.
  evidence: BH4: validate-story-gitlinks.py:428 compares current_pointer(..., baseline), which returns None for an absent pointer, to the raw addition old SHA of forty zeroes. Normalize missing pointers and cover additions/removals; this branch predates the dependency continuation.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
  summary: medium; NuGet boundary validation cannot detect mismatched dependency versions or framework groups.
  evidence: BH5: scripts/validate-nuget-packages.py reduces restore and nuspec dependencies to ID sets. An isolated nuspec containing EventStore [3.115.0] in net9.0 passes with the expected package ID. Preserve framework/version evidence and add negative fixtures in the owning release-validator work; the implementation predates this upgrade.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
  summary: medium; bootstrap token responses are buffered before the intended 64 KiB bound is enforced.
  evidence: BH7: TenantBootstrapCredentialProvider.cs:86 uses buffered PostAsync, then calls LoadIntoBufferAsync with 64 KiB; TenantBootstrapHostedService creates a default HttpClient. An oversized authority response consumes memory before this cap. Use headers-first bounded reading and an oversized-response test in the pre-existing bootstrap flow.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
  summary: medium; performance evidence records root HEAD while measuring an unrestricted working tree.
  evidence: BH8: scripts/run-tenant-audit-performance.sh:114 records gitRevision, then builds local files without a clean-tree guard or source/submodule snapshot. Distinct measured candidates can share the same revision. Capture exact source provenance or require cleanliness; this is pre-existing and does not invalidate the separately evidenced clean historical run.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
  summary: low; command models violate the required one-C#-type-per-file authoring rule.
  evidence: BH10: TenantCreateCommandModels.cs contains fourteen public records/enums for create, membership, metadata and lifecycle flows. The required Hexalith baseline explicitly requires separate type-named files. Split mechanically in the owning command-flow change; no new product code was introduced by this dependency upgrade.

- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies.md`
  summary: medium; recorded gitlink continuity lacks a regression that detects removal of the chain check.
  evidence: VG1, pre-verified: the reviewer disabled only [CHAIN GAP] in memory and all 23 guard tests still passed. Add CLI cases for a valid two-commit pointer chain and an omitted intermediate pointer-changing commit. The guard/tests predate this dependency continuation.

- source_spec: `_bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md`
  summary: Clarify the non-EventStore preservation statement as structural equality of parsed rows rather than byte equality.
  evidence: The refresh verification compares JSON package/family lists with Python equality; separate catalog byte/hash checks prove file-byte preservation. The focused review's R7 records this distinction and the one-shot workflow routes fixes to specs into deferred work.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 15 (2026-10-08)

Diff reviewed: Tenants `bfaa770e` + `5bfe0715` (story files only; HEAD `5bfe0715` = `origin/main`).

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; restore the Epic 5 rules that the 2026-10-08 regeneration dropped from `epic-5-context.md`, before Story 5.7 is specified.
  evidence: `5bfe0715` rewrote the context 9,595 → 6,688 bytes. It removed "never announce success before projection confirm" (`architecture.md:691`), McpCli-only CLI/MCP exposure (`architecture.md:66`), "5.7a/5.7b, complete only as a whole" (`epics.md:2671`), the `LastGlobalAdministrator` hard stop (`epics.md:2730`), the exclusive Set/RemoveGlobalAdministrator mapping, the `/api/global-administrators` read, the availability severity styling and "seven fields only" receipts, and it vaguened the recovery-verb, origin-state and entry-point lists. The R1–R4 additions come from the 5.5/5.6 specs, not the planning docs, so the next `compile-epic-context` run drops them again. Restore the `1cdcc0a9` content and add R1–R4 with source citations.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Carried, with no new work item; see DW-162. CI still never runs `validate-story-gitlinks.py` against real story files.
  evidence: `.github/workflows/story-guards.yml:34` runs only `tests/scripts/test_validate_story_gitlinks.py` on synthetic repositories. `5bfe0715` reached `main` green while the guard exits 1 on both 5.6 specs (Memories `c4697367`, Platform `495d1d0d`). 10th recurrence of the silent-bump pattern.

## Deferred from: code review of spec-gh-actions-28953291798-85906522208.md (2026-10-08)

Diff reviewed: `tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs` at `3d96d0aa..HEAD` (`5bfe0715`). The spec's own change is `2e82d0d6`; these entries come from later commits to the same file or from pre-existing CI state.

- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
  summary: medium; no blocking test asserts that the audit Unauthorized state is announced with `role="alert"` and `aria-live="assertive"`.
  evidence: `96bdfd8a` removed `markup.ShouldContain("role=\"alert\"")` from the audit route smoke test. The only Tier 1 check, `TenantAuditPageTests.Tenant_audit_page_renders_distinct_accessible_states` (`tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:2108-2129`), asserts only `GetAttribute("role").ShouldNotBeNull()`, so a regression of `TenantAuditPage.razor:632` `StatusRole` to `status`/`polite` passes everywhere. Fix: mirror `TenantDetailSurfaceTests.cs:2945-2947` and assert `alert` plus `assertive` for Unauthorized.

- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
  summary: low; the localized detail Unauthorized title "Tenant detail unauthorized" is not pinned exactly by any test.
  evidence: The hosted smoke assertion was removed around `2e61f57b`/`62eb3607`. Tier 1 `Detail_page_renders_distinct_safe_states` (`TenantDetailSurfaceTests.cs:2923`) matches only the case-insensitive substring `"authorized"`, which an unresolved resx key name (`Tenants.Detail.State.Unauthorized.Title`) would also satisfy.

- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
  summary: low; the detail smoke test asserts the unauthorized marker twice, and the comment above the duplicate describes a source-order form that never existed in this file.
  evidence: `TenantsUiRouteSmokeTests.cs` line 84 (`TenantsDetailUnauthorizedMarker`) and line 92 (literal `data-testid="tenants-detail-unauthorized"`) are the same check. `git log -p` on the file shows no combined `unauthorized" role=` assertion that "pinned their source order". Delete the duplicate and trim the comment.

- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
  summary: low; the audit smoke assertion "Return to tenant detail" cannot tell the focus-return hint from the NoFocus variant, and no test asserts the hint text.
  evidence: `TenantsResources.resx:3547-3552`: `Tenants.Audit.ReturnContext` = "Return to {0}. The originating control will receive focus when available."; `.NoFocus` = "Return to {0}." Both start with the asserted prefix. The test passes a `returnFocus`, so it should assert the full sentence. A search of `tests/Hexalith.Tenants.UI.Tests` finds the sentence only in a resource dictionary (`AuditEvidenceEntryPointTests.cs:514`), never in an assertion.

- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
  summary: low; the user-lookup smoke comment says the compatibility route "redirects before issuing the lookup", but the code issues the lookup first.
  evidence: `UserMembershipLookupPanel.razor:520-548` awaits `QueryGateway.GetUserTenantsAsync` and only then calls `Navigation.NavigateTo(UserLookupNavigationUrl, replace: true)`. `UserMembershipLookupPage.razor` and the server have no earlier redirect, and `UserMembershipLookupSurfaceTests.cs:218-225` shows the request is issued before the URL rewrite. Correct the comment at `TenantsUiRouteSmokeTests.cs:154-155`.

- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
  summary: medium; Aspire-lane failures are hidden because the reusable `domain-ci.yml` defaults `aspire-continue-on-error` to true.
  evidence: Run 28953291798 concluded `success` although `ci / aspire-tests` failed; Tenants `ci.yml` does not override the default. A red hosted smoke test therefore never blocks a merge. Changing this is a workflow-definition decision (Ask First in the spec).

- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
  summary: high; on `main` (`5bfe0715`, run 37782396626) `ci / build-and-test` fails, so `ci / aspire-tests` is skipped and the hosted smoke class never runs in CI.
  evidence: The job log shows two failures: `GlobalAdministratorsPageTests.RealChromiumFocusValidatorStopsBeforeChromiumWhenServerStartupFails` and `StatelessHostStateTests.TenantsHostAssembly_HasNoWritableStaticFields_HoldingInstanceLocalState` (coverage-instrumentation statics). At review time, uncommitted working-tree edits to both test files existed from a concurrent session; re-check before acting. Update from Story 5.6 pass 17 (2026-10-08): both fixes landed in root `9ee73062b7e540b69f824803d182f151c96ddb24` (browser configuration forwarding and coverage-tracker field recognition). The recorded HEAD run 37816323193 at `0ac7c126ea0654edfeaa867b108749fa463e7287` passes build-and-test, so the former skip condition is resolved. aspire-tests now runs and fails with 14 errors: fixture initialization rejects JwtBearer configuration unless exactly one of Authority or SigningKey is set, and the client-supplied actor:globalAdmin test still expects 400 although the gateway ignores the reserved key and returns 202. The lane remains masked by aspire-continue-on-error. The command-API expectation is tracked in the pass-17 defer below; these are historical review observations, not a fresh CI run by this remediation.

## Deferred from: pass 16 review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-08)

Diff reviewed: baseline `11e65e37f0fbf6649642a512052eebd37d50166d` through the pass-15 patches committed in `2b91b05be9c927319fd207b02552d1213997776b`; the spec retains the immutable review-input path and the 15-finding triage tally.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; bootstrap token retrieval buffers the response before applying its 64 KiB cap.
  evidence: `TenantBootstrapCredentialProvider` calls `HttpClient.PostAsync` and only then `LoadIntoBufferAsync`. The password grant and subject check sit on that same path. This is outside the tenant-correction story.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: high claim superseded by pass-17 gateway tracing; see the medium command-API test-expectation defer below.
  evidence: The pass-16 aggregate-only trace did not cover the gateway: EventStore ignores and strips the client-supplied reserved extension, then adds it only from the JWT claim. Pass 17 rejects the client-authority claim as false at that gateway and records the stale integration-test expectation instead; this entry creates no second authority work item.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; NuGet validation allowlists two ServiceDefaults package ids at any version.
  evidence: `scripts/validate-nuget-packages.py` names `Hexalith.EventStore.ServiceDefaults` and `Hexalith.Commons.ServiceDefaults` before the `.ServiceDefaults` fragment check. A wrong version of those ids is a dependency-governance gap, not a correction-flow change.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; duplicate global-administrator ids are dropped and the page is still marked complete.
  evidence: `GlobalAdministratorsProjectionLoader` ignores a failed `TryAdd` and sets `IsCompleteEvidence`. Conflicting administrator rows can disappear from a ready page. That loader is the global-administrator read, not this correction.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: Story 5.7; loading may drop an in-flight global-administrator correction when that story mounts the panel. Not reachable on the tenant audit page since `10c9f6f6`; unverified, high if true.
  evidence: The tenant audit page no longer mounts `GlobalAdministratorCorrectionPanel` after `10c9f6f6`, and the pass-19 read-only cleanup removes its remaining global-administrator enrichment. Re-check loading and retained-display behavior when Story 5.7 mounts the panel. Story 5.7 owns that panel.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: resolved by `10c9f6f6`; the tenant audit page no longer opens or submits a global-administrator correction.
  evidence: Pass 15 traced the former `GlobalAdministratorCorrectionPanel` audit-page branch to `5a3bc6dd` and the global-administrator projection spec. `10c9f6f6` removed the branch; the pass-19 read-only cleanup restores the remaining audit-surface copy and removes unused evidence reads.
  resolution: Resolved by Story 5.6 commit `10c9f6f6` (panel branch removed) and `ded413df` (read-only copy restored, evidence reads removed).

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 17 (2026-10-08)

Diff reviewed: Tenants `fe5f6aa2..0ac7c126`, story files only (HEAD `0ac7c126` = `origin/main`), plus EventStore `9542d3c9..07d1e23a` story paths.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; `CommandApiRuntimeIntegrationTests.Commands_endpoint_rejects_client_supplied_globalAdmin_extension_metadata` expects `400`, but EventStore deliberately ignores the reserved key and returns `202`. The test is red in the masked `ci / aspire-tests` lane.
  evidence: It fails at `CommandApiRuntimeIntegrationTests.cs:1629` in job 113448120420 (HEAD `0ac7c126`), and earlier at `416ab32c`, `390da330` and `03257338`. EventStore `CommandsController.cs:244` logs and skips `actor:globalAdmin`, and `SubmitCommandExtensions.cs:33` re-adds it only from the JWT claim. The client therefore cannot set global-admin authority through this endpoint. The pass-16 entry "Tenant command admission still treats `actor:globalAdmin=true` as authority" is a false positive at this gateway. Align the test with the ignore-and-log contract, or change the contract to reject, in the command-API owner's spec.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction-2.md`
  summary: Carried, with no new work item; see DW-198. `validate-story-gitlinks.py` exits 1 on the historical reverification `-2.md` at HEAD.
  evidence: At `0ac7c126` it reports 4× `[UNDECLARED]` (Builds, Commons, EventStore, McpCli) and 2× `[MISSTATED]` (Memories `906bc07a`, Platform `f5a0d72f`). The `-2.md` File List records `5bfe0715` values. `-3.md` calls `-2.md` historical, but the guard has no story-end ref, and `-3.md` will fail the same way at the next bump.

## Deferred from: Story 5.6 remediation workflow review, pass 18 (2026-10-08)

Diff reviewed: baseline `11e65e37` to the pre-patch working tree over root `5289b86b` (committed with its patches in `10c9f6f6`); input `/tmp/tenants-56-pass17-2kj37k80/pass18-reviewed-input.diff`, 2,657,085 bytes, SHA-256 `4692e95462d7000e9a3d850158858974fc05aef53e28434611ab4d14585330d3`, not archived. Triage: 13 findings, 2 patches, 6 defers (5 new, 1 carried), 5 rejected.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: high; bootstrap administrator credential acquisition permits an HTTP authority outside Development.
  evidence: `TenantBootstrapCredentialProvider.AcquireFromAuthorityAsync` posts the password form to `EventStore:Authentication:Authority` without validating its scheme or environment. Production host validation covers the separate `Authentication:JwtBearer:Authority` setting. The bootstrap hosted service passes the independent authority through unchanged; require production transport validation in the bootstrap owner's work.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; bootstrap authority mode hardcodes the Keycloak token endpoint despite its generic authority contract.
  evidence: `TenantBootstrapCredentialProvider` appends `/protocol/openid-connect/token`; an OIDC authority with a different advertised endpoint cannot bootstrap. The provider documentation does not restrict authority mode to Keycloak. Resolve or explicitly configure and validate the endpoint in separate bootstrap compatibility work.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; partial-release recovery consumes temporary NuGet credential lifetime while preparing artifacts.
  evidence: `recover-partial-release.yml` exchanges the documented one-hour key before `publish-partial-release.sh` builds, packs, validates, and compares package hashes. A long preparation leaves an expired key at publication. Prepare artifacts before exchanging credentials in the release recovery owner's work.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; release credential exchange executes a mutable NuGet action tag with OIDC issuance permission.
  evidence: `release.yml` and `recover-partial-release.yml` invoke `NuGet/login@v1.2.0` in jobs with `id-token: write`. Moving that tag changes credential-exchange code independently of the reviewed source and Builds checkout identity. Pin the credential action to a reviewed commit in separate release governance work.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; technical ServiceDefaults package exceptions are not scoped to published package layers.
  evidence: `scripts/validate-nuget-packages.py` allows both exact technical ServiceDefaults IDs for every package whose restore evidence includes them, including Contracts, Client, and Testing. Define and enforce the intended dependency layers in package architecture work; Story 5.6 adds executable exact-ID boundary coverage without changing that policy.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 19 (2026-10-08)

Diff reviewed: Tenants `5289b86b..10c9f6f6` (the pass-17 fix pass), HEAD `10c9f6f6` = `origin/main`. Triage: 39 raw findings → 1 decision (resolved into the first patch), 6 patches, 1 defer, 19 rejected.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; carried, updates the Chromium entry at `:3338`. Story Guards still fails at HEAD, but the 30-second bound now ends it at once instead of a 15-minute hang.
  evidence: Run 37843324360, job 113537923481, at `10c9f6f6`: "Chromium scenario profile-shipped failed (exit 124; bound 30s)" with Chrome for Testing 153.0.8010.52, preceded only by dbus connection errors. Since `9ee73062` added `--no-sandbox`, Chrome 153 hangs on its first DOM dump instead of aborting with exit 134, so the `:3338` and `:3508` descriptions are out of date. Repairing the CI Chromium launch remains runner work; the story's EN/FR browser evidence is local-only (Chrome 154).

## Deferred from: Story 5.6 pass-20 independent review (2026-10-09)

Diff reviewed: baseline `11e65e37` through the working tree over root `a0576d83` (committed with the pass-19 patches in `ded413df`); input `/tmp/tenants-56-diff-yWc71L.patch`, 3,161,925 bytes, SHA-256 `927090f9…`, not archived. Triage: 15 findings, 0 patches, 11 defers (2 new, 9 carried), 4 rejected.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; bootstrap does not retry transient credential or command failures until the host restarts.
  evidence: `TenantBootstrapHostedService.StartAsync` schedules `RunBootstrapAsync` once on `ApplicationStarted`; the run returns after a credential failure, unexpected response, or caught exception. Its log says it retries on next restart. Define a bounded in-process retry or explicit operator recovery in the bootstrap owner's work.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; manually dispatched release tests pass when the NuGet login step is disabled.
  evidence: `PackageGovernanceTests.Release_workflows_publish_to_nuget_through_trusted_publishing` checks login text and step order but not a disabling condition. A login step with `if: ${{ false }}` still passes the test; the dependent publish step receives an empty `NUGET_API_KEY` and aborts. Validate the executable login-to-publish handoff in release-owner work with the protected GitHub environment.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 21 (2026-10-09)

Diff reviewed: Tenants `a0576d83..ded413df` (the pass-19 fix pass), HEAD `ded413df` = `origin/main`; 548-line scratch diff, SHA-256 `ce074e60…`, not archived. Triage: 40 normalized findings → 0 decisions, 4 patches, 2 defers, 23 rejected.

- source_spec: `_bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md`
  summary: low; the paging spec still lists its reverted tenant-audit-page work as delivered.
  evidence: Task `:64` (`TenantAuditPage.razor` complete reads for initial enrichment, correction-open refresh and confirmation, plus refreshed-intent re-derivation) is `[x]`, and AC4 (`:75`) describes correction-open re-evaluation. Story 5.6 removed the audit-page branch in `10c9f6f6` and the enrichment in `ded413df`. The spec's own 2026-10-08 triage note explains this. Qualify `:64` and AC4 as reverted until Story 5.7 in that spec's review.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: low; Story 5.7 handoff. Story 5.6 reverted the tenant audit page's global-administrator integration, and Story 5.7 must re-derive it.
  evidence: `5a3bc6dd` holds the removed page code: the complete-evidence load, the correction-open refresh, the global-intent re-derivation, and the 8-case `GlobalAdministratorIncompleteEvidenceCannotEnableOrOpenCorrection` fail-closed matrix. `GlobalAdministratorCorrectionPanel.CorrectiveAuditEvidence` (`GlobalAdministratorCorrectionPanel.razor:201`) was added for that integration and now has no consumer and no test. When Story 5.7 enables the correction, it must re-derive the integration and its matrix from the Epic 5 rules, or remove the parameter.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 22 (2026-10-09)

Diff reviewed: baseline `11e65e37f0fbf6649642a512052eebd37d50166d` through the working tree over root `84c4e8b3dfd2fe651bac57bf89e2e988d998a7d2`; input `/tmp/tenants-56-baseline-k7kwjdc7.diff`, 3,178,732 bytes, SHA-256 `1f8675b41f9b4c0b6e2998083354297fe810c63485ab2220e5abb8a56d65d05c`, not archived. Triage: 14 findings, 0 patches, 3 new defers, 6 carried defers, 5 rejections.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; the release publish job grants OIDC issuance while dependency and build code runs before credential exchange.
  evidence: `release.yml` grants `id-token: write` at job scope (`:285`). Before `NuGet/login` (`:425`), the same job runs `npm ci` (`:369`), `dotnet restore` and `dotnet build` (`:372-375`, which execute restored packages' MSBuild targets), and the container-publisher preparation (`:381`). All of that code therefore executes with OIDC token-request permission. Separate dependency installation and build from publication authority in release-governance work.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; release jobs execute movable action tags beyond the previously recorded NuGet login action.
  evidence: `release.yml` runs `actions/checkout@v7.0.1` (`:312`, `:318`), `actions/setup-dotnet@v6.0.0` (`:340`), `actions/setup-node@v7.0.0` (`:343`), `actions/cache@v6.1.0` (`:347`) and `actions/upload-artifact@v7.0.1` (`:438`) in a job with publication authority. `recover-partial-release.yml` grants `id-token: write` (`:32`) and runs `actions/checkout@v7.0.1` (`:44`) and `actions/setup-dotnet@v6.0.0` (`:50`). Version tags can move without a reviewed workflow change. Pin reviewed action revisions in release-governance work; the prior ledger entry covers `NuGet/login` specifically.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: high; global-administrator revocation can remain authorized for every global-administrator command and query, including tenant correction, while its authority channels lag.
  evidence: `81144734` added `TenantsGlobalAdministratorVerifier` for EventStore Story 5.5 FR28 within the Story 5.6 range. Correction dispatch using global-administrator authority needs both the corroborated UI principal-claims gate (`src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs:92`, reached from `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:542,561`) and the server verifier. At the `/process` boundary, the verifier admits `actor:globalAdmin` for tenant commands (`src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs:265-272`); at the `/query` boundary, it admits `QueryEnvelope.IsGlobalAdmin` for global-administrator queries (`src/Hexalith.Tenants/Authorization/TenantsGlobalAdministratorVerifier.cs:11-18`). Both use the persisted `GlobalAdministratorReadModel`. Revoking either authority channel stops its path once observed: identity-provider revocation takes effect after token or circuit renewal, and `GlobalAdministratorRemoved` takes effect after projection, with no bound if projection stalls. The global-administrator authority owner should define an authoritative or version-bounded revocation check for all such commands and queries.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 23 (2026-10-09)

Diff reviewed: Tenants `84c4e8b3..ee4a81f3` (the pass-21 fix pass, local and unpushed; `origin/main` = `84c4e8b3`); input 266-line scratch diff, 29,357 bytes, SHA-256 `6b2037560cd12aa6c63d5e6cd20d785153d042be2541afc4541672d2bb82a1b0`, not archived. Triage: 28 normalized findings → 0 decisions, 3 patches, 1 defer (folded into the pass-22 entry), 20 rejected.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 24 (2026-10-09)

Diff reviewed: Tenants `ee4a81f3`..working tree over root `0d8cc8d8` (= `origin/main`; the user's commit carrying the pass-23 records and patches); input 158-line scratch diff, 23,870 bytes, SHA-256 `e8087f4f487ebba254d54c17eb237c0518f0ae2dd32007608a30a499f5d651ab`, not archived. Triage: 24 normalized findings → 0 decisions, 2 patches (9 findings), 1 defer (3 findings), 12 rejected.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; release builds after `NuGet/login` run restored package code with the live NuGet key in their environment.
  evidence: `release.yml` mints `NUGET_API_KEY` at `:425` and passes it to the Semantic Release step (`:431`), whose `prepareCmd` (`.releaserc.json:11`) runs `dotnet build Hexalith.Tenants.slnx` with implicit restore. Restored packages' MSBuild targets therefore execute with the key, contrary to the workflow comment that the key is "minted only after the build" (`release.yml:421-422`). `recover-partial-release.yml` logs in at `:72`, then `scripts/publish-partial-release.sh:14` builds with `NUGET_API_KEY`, `HEXALITH_ZOT_API_KEY` and `GH_TOKEN` in the environment (`recover-partial-release.yml:77-83`). The pass-22 OIDC entry covers pre-login code and `:3788` covers key expiry. In release-governance work, build and pack before credential exchange, and give the publish step only prebuilt artifacts.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: low; Tenants production authentication guidance documents global-administrator grants but omits revocation lag.
  evidence: `docs/production-auth-readiness.md:76` says `GlobalAdministratorSet` takes effect after projection, but does not say that `GlobalAdministratorRemoved` also takes effect after projection and identity-provider revocation after token or circuit renewal. Update that Tenants-owned guidance separately from the platform authority check.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 25 (2026-10-09)

Diff reviewed: baseline `11e65e37` through the working tree over root `cc17b071`; input `/tmp/story-56-diff-J8LkoHyp.patch`, 3,208,703 bytes, SHA-256 `8b551d774cd7fab5a6acbb34be921fda1e2f620f39c4518acdb15db34496a302`, not archived. Triage: 17 findings, 0 patches, 14 defers (2 new, 12 carried), 3 rejected.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; global-administrator correction proof needs a complete, current audit walk before pairing receipts.
  evidence: `GlobalAdministratorCorrectionPanel.QueryCorrectiveProofAsync` filters the rows of one `GetTenantAuditAsync` response without checking its availability/freshness or following `HasMore` and `NextCursor`. A stale or degraded page can supply a false proof candidate, while a later page's valid candidate is missed. The panel has no production mount today; Story 5.7 should enforce current, complete audit evidence and deterministic attempt association before enabling its proof link. The existing Story 5.7 entry at `deferred-work.md:3500-3502` covers the separate target/type/time association weakness.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; Trusted Publishing account guards lack executable success and failure tests.
  evidence: `PackageGovernanceTests.AssertTrustedPublishingJob` (`tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs:2209-2219`, called at `:904-905`) checks the two workflow guard blocks (`.github/workflows/release.yml:308`, `.github/workflows/recover-partial-release.yml:40`) for the `NUGET_USER` expansion text but does not execute their Bash conditions. Replacing `-z` with `-n` preserves that text and makes a configured account fail before publication. Release-governance work should execute both guards with populated and blank account values and assert the respective exit statuses.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 27 (2026-10-09)

Diff reviewed: baseline `11e65e37` through root `6e6a7f5b`; input `/tmp/story-5-6-baseline-OsK7WiL3.diff`, 1,815,498 bytes, not archived. Triage: 15 findings, 1 patch, 7 defers (1 new, 6 carried), 7 rejected.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: high; the first global-administrator bootstrap command can name a different administrator than its authenticated actor.
  evidence: `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs:11-19` handles `BootstrapGlobalAdmin` using `command.UserId` without receiving the command envelope or comparing its authenticated `UserId`. It writes `command.UserId` as both the `UserId` and `ActorUserId` of `GlobalAdministratorSet`, so the audit trail can record the target as its own actor. The Story 5.6 credential provider binds only its own hosted-service submission to the configured subject; `AspireTopologyTests.cs:136` and `:335` show that an external JWT caller can submit `BootstrapGlobalAdmin` for its own subject. The global-administrator command owner should require the bootstrap target to equal the authenticated actor and add a mismatch regression at the domain-service boundary.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 31 (2026-10-09)

Diff reviewed: Tenants `96cc6f11..cdd0c80c` (the pass-29 fix pass); input `/tmp/tenants-56-pass31-diff-IbVNh1.patch`, 89,787 bytes, SHA-256 `f726d22b3611a2146a432c1924104405e3279ab02e80d5612b3dd25ad585b923`, not archived. Triage: 41 findings, 2 decisions (both resolved to option (a), adding 2 patches), 6 patches, 1 defer (new), 14 rejected.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium if true (unverified, maybe-false); the version-derived producer ETag cannot reach audit, index or global-administrator reads, because only `TenantReadModel` is stamped with a `ProjectionVersion`.
  evidence: `src/Hexalith.Tenants/Queries/TenantQueryResult.cs:50` falls back to `readModel?.ProjectionVersion` when the store ETag is absent. Only `TenantProjectionHandler.cs:236-250` assigns a version (`tenant-sequence:N`). `TenantIndexReadModel`, `TenantAuditReadModel` and `GlobalAdministratorReadModel` declare `ProjectionVersion`, but nothing in Tenants or EventStore assigns it. When their store ETag is absent, metadata is null and those surfaces fail closed under Story 4.2's rule, which blocks audit evidence and global-administrator grant/remove previews. Pass 29's rationale ("all four read models carry a `ProjectionVersion`") was wrong. To settle: probe whether live audit or global-administrator reads ever lack a store ETag. Pass 27 reported none for tenant detail; pass 30 observed `1`; the live audit test asserts `ProjectionBacked`. If they do, stamp sequence versions in those three projections.

## Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 32 (2026-10-10)

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; Keycloak workload client authentication lacks a runtime token and authorized domain-call test.
  evidence: The AppHost model test verifies shared secret parameter wiring, and the realm declares `clientAuthenticatorType: client-secret`, but no Keycloak-enabled test obtains a `client_credentials` token with that secret and completes an authorized domain-service call. The existing Aspire fixture runs with `EnableKeycloak=false`. Add a live Keycloak test in the identity/integration owner before relying on this mode's end-to-end operation.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
  summary: medium; the standalone release workflow omits the reusable Builds workflow's governed-provenance evaluation.
  evidence: `.github/workflows/release.yml` checks the approved Builds checkout SHA but does not run the `governed-provenance` closure evaluation used by `references/Hexalith.Builds/.github/workflows/domain-release.yml`. This pre-existing release-governance difference should be resolved in the release workflow owner before publication relies on equivalent closure evidence.
