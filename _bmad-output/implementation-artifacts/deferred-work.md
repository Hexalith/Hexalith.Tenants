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
status: open
decision: 2026-08-25 Add FrontComposer contract — Introduce a shared explicit tab-to-panel association API in FrontComposer and migrate Tenants.

### DW-72: Page-local tabs a11y — empty tabpanels + missing Tenants-owned bUnit assertion
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Full review (2026-06-28)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:22-30
reason: The legacy ledger defers this issue: Page-local tabs a11y — empty tabpanels + missing Tenants-owned bUnit assertion. Original context is preserved in legacy-detail.
legacy-detail: - **Page-local tabs a11y — empty tabpanels + missing Tenants-owned bUnit assertion** — extends the 2026-06-27 Group-1 "empty tabpanels" defer above. AC12/AC13 keyboard/active-tab guarantees ride entirely on the Fluent `FluentTabs` primitive with no Tenants-owned `aria-selected`/keyboard-switch bUnit assertion; the added tests assert tab presence/text and routing only. Follow-up: pair the upstream FrontComposer/UX tabpanel-association decision with a focused active-tab/keyboard bUnit test once the structure is settled. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:22-30`)
status: open
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
status: open

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
status: open

### DW-98: Dependency-boundary validation is a hardcoded per-package allowlist rather than derived from actual restore/lock output
origin: migrated from legacy ledger ("Deferred from: code review of run-all-tests-and-fix-failures-2 (2026-07-20)"), 2026-08-25
location: scripts/validate-nuget-packages.py
reason: The legacy ledger defers this issue: Dependency-boundary validation is a hardcoded per-package allowlist rather than derived from actual restore/lock output. Original context is preserved in legacy-detail.
legacy-detail: - **Dependency-boundary validation is a hardcoded per-package allowlist rather than derived from actual restore/lock output** (e.g. `dotnet list package --include-transitive`) — inherently high-maintenance; this is the second time in this file's history a submodule/package version bump has required a manual allowlist update (see the CI Restore NU1107 memory for the sibling pattern). A more dynamic validation approach would eliminate this class of recurring CI break. Architectural, out of scope for a narrowly-scoped test-fix session. (`scripts/validate-nuget-packages.py`)
status: open

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
status: open

### DW-102: Duplicated tab/scope literal constants across two files
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:219-222; src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs
reason: The legacy ledger defers this issue: Duplicated tab/scope literal constants across two files. Original context is preserved in legacy-detail.
legacy-detail: - **Duplicated tab/scope literal constants across two files** — `TenantsWorkspace.razor` declares `TenantsTabId`/`UsersTabId`/`AllTenantsScope`/`MyTenantsScope` and `TenantWorkspaceState.cs` declares `TenantsTab`/`UsersTab`/`AllScope`/`MyScope` with the same `"tenants"/"users"/"all"/"mine"` values; `ApplyWorkspaceState` compares `state.Tab` (sourced from the state file's consts) against the razor file's consts. Value-equal today, but nothing enforces it — changing one string silently breaks tab/scope routing with no compile error. DRY nit. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:219-222`, `src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs`)
status: open

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
status: open

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
status: open

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
status: open

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
status: open

### DW-171: UnableToVerify copy mentions confirming via the tenant audit trail without an in-page navigation link
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-08, loop 5 chunk 2)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:184
reason: The legacy ledger defers this issue: UnableToVerify copy mentions confirming via the tenant audit trail without an in-page navigation link. Original context is preserved in legacy-detail.
legacy-detail: - UnableToVerify copy mentions confirming via the tenant audit trail without an in-page navigation link. Audit navigation is outside this story's File List and acceptance criteria. [src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:184]
status: open

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
status: open

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
status: open

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
status: open

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

### DW-327: The untracked RemoveTenantConfigurationAsync overload silently changed its failure contract to keyed, SafeMessage-null results.
origin: spec-deferred ee1c75c4860f
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:273
source_spec: `spec-3-6-remove-configuration-key-with-complete-preview.md`
severity: low
reason: It now delegates to RemoveTenantConfigurationTrackedAsync, so it can return Ambiguous or FailedWithKey("Tenants.Commands.Unavailable.InvalidTrackingReference") with SafeMessage null. No production caller remains, and no test pins the overload, so a future caller rendering SafeMessage would show empty text.
status: open

### DW-328: Follow-up review still recommended for 3-6-remove-configuration-key-with-complete-preview after the damping cap was spent
origin: review-budget-followup
location: n/a
source_spec: `spec-3-6-remove-configuration-key-with-complete-preview.md`
severity: low
reason: The follow-up-review damping cap (limits.max_followup_reviews = 1) was spent with the story finalized (status: done, verify green) while the review pass still recommended an independent follow-up. The work was committed by bmad-loop run 20260827-234608-260c; this entry preserves the lingering recommendation for a deliberate later review.
status: open
