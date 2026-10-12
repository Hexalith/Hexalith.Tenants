### DW-1: Follow-up review still recommended for 1-8-support-safe-identifier-copy-and-read-experience-evidence after the damping cap was spent
origin: review-budget-followup
source_spec: `spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
severity: low
reason: The follow-up-review damping cap (limits.max_followup_reviews = 1) was spent with the story finalized (status: done, verify green) while the review pass still recommended an independent follow-up. The work was committed by bmad-loop run 20260721-185843-1016; this entry preserves the lingering recommendation for a deliberate later review.
status: done 2026-08-27
archived: 2026-10-12
resolution: resolved by sweep bundle dw-support-safe-copy-followup
resolution-undo: e32f9ec5cfa74f6e713b0d8ce939f3393f91d53d4b83c40ea3313864db2fc699 2026-08-27 7374617475733a206f70656e

### DW-2: Global-administrator pagination >20 admins — fail-OPEN CLOSED (full paging redesign still routed)
origin: migrated from legacy ledger ("2026-07-01 Correct Course — Deferred Work (pagination fail-closed + submodule doc handoffs)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs
reason: The legacy ledger defers this issue: Global-administrator pagination >20 admins — fail-OPEN CLOSED (full paging redesign still routed). Original context is preserved in legacy-detail.
legacy-detail: - **Global-administrator pagination >20 admins — fail-OPEN CLOSED (full paging redesign still routed).** `GlobalAdministratorCorrectionSnapshot` now treats absence as conclusive only when the whole fixed projection is loaded (`!HasMore`): `EvaluateCurrentProjection` fails closed to `UnableToVerify` (`Tenants.Correction.Unavailable.CurrentProjectionUnavailable`) for a restore/revoke whose target is absent from an incomplete page, and `ConfirmProjection` proves a revoke only on `!present && !HasMore`, killing the false-`Confirmed` on a revoke of a page-2 administrator. Presence-found stays conclusive, so page-1 corrections at scale are unaffected. +3 tests + `PagedProjectionReady` helper. The multi-page load/aggregation that would let a page-2 correction actually RUN (rather than be conservatively blocked) stays routed to a dedicated projection-paging story. (`src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs`)
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: commit 7716f0b11423eb54d74935b6cc6e3edf405dc400; src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs:23-89 now walks and aggregates all stable cursor pages.

### DW-3: FrontComposer `FcContentLabel` single-writer dispose-clobber + server first-paint — DOCUMENTED
origin: migrated from legacy ledger ("2026-07-01 Correct Course — Deferred Work (pagination fail-closed + submodule doc handoffs)"), 2026-08-25
location: FcContentLabel
reason: The legacy ledger defers this issue: FrontComposer `FcContentLabel` single-writer dispose-clobber + server first-paint — DOCUMENTED. Original context is preserved in legacy-detail.
legacy-detail: - **FrontComposer `FcContentLabel` single-writer dispose-clobber + server first-paint — DOCUMENTED.** XML `<remarks>` on `FcContentLabel` (plus a matching sentence on `FcContentLabelCoordinator`) now record the last-writer-wins dispose-clobber and the `OnAfterRender`-only first-paint limitation, naming the shell-parameter path (`ContentLabel`/`ContentLabelledBy`) as the first-paint-correct alternative. Doc-only.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcContentLabel.razor.cs:20-33 documents the single-writer dispose-clobber and InteractiveServer first-paint limitation.

### DW-4: FrontComposer `FcPageHeader.FocusHeadingAsync` no-op→throw — DOCUMENTED
origin: migrated from legacy ledger ("2026-07-01 Correct Course — Deferred Work (pagination fail-closed + submodule doc handoffs)"), 2026-08-25
location: FcPageHeader.FocusHeadingAsync
reason: The legacy ledger defers this issue: FrontComposer `FcPageHeader.FocusHeadingAsync` no-op→throw — DOCUMENTED. Original context is preserved in legacy-detail.
legacy-detail: - **FrontComposer `FcPageHeader.FocusHeadingAsync` no-op→throw — DOCUMENTED.** An adopter-facing behavior-change note was added to the method `<remarks>` (there is no FrontComposer CHANGELOG), incl. the caveat that the `FcAggregateListPage` wrapper's `?? ValueTask.CompletedTask` guards only the null-`@ref` window, not the throw.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs:94-126 documents and enforces the no-op-to-throw behavior.

### DW-5: EventStore `StorageTreemap` SVG `<g tabindex>` cross-browser — DOCUMENTED
origin: migrated from legacy ledger ("2026-07-01 Correct Course — Deferred Work (pagination fail-closed + submodule doc handoffs)"), 2026-08-25
location: StorageTreemap
reason: The legacy ledger defers this issue: EventStore `StorageTreemap` SVG `<g tabindex>` cross-browser — DOCUMENTED. Original context is preserved in legacy-detail.
legacy-detail: - **EventStore `StorageTreemap` SVG `<g tabindex>` cross-browser — DOCUMENTED.** A Razor comment above the focusable cell records the Chromium/Edge/Firefox-vs-Safari/WebKit tab-order caveat and the `<a>`/`<foreignObject>` remedy if WebKit support is required.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Components/StorageTreemap.razor:65-76 documents the WebKit caveat and alternatives.

### DW-6: JSDisconnectedException guard on panel focus
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: CorrectionStartPanel
reason: The legacy ledger defers this issue: JSDisconnectedException guard on panel focus. Original context is preserved in legacy-detail.
legacy-detail: - **JSDisconnectedException guard on panel focus** — both `CorrectionStartPanel` and `GlobalAdministratorCorrectionPanel` `OnAfterRenderAsync` now wrap `_lifecycleElement.FocusAsync()` in `try/catch (JSDisconnectedException)` (parity with the existing `TenantAuditPage` guards).
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:322-334 and GlobalAdministratorCorrectionPanel.razor:279-291 catch JSDisconnectedException around focus.

### DW-7: Page-load global-admin query unguarded
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: TenantAuditPage.LoadAsync
reason: The legacy ledger defers this issue: Page-load global-admin query unguarded. Original context is preserved in legacy-detail.
legacy-detail: - **Page-load global-admin query unguarded** — `TenantAuditPage.LoadAsync` now wraps the supplementary global-administrator enrichment in `catch (… EventStoreGatewayException or HttpRequestException or JsonException)`; the confirm-time path (`OpenCorrectionAsync` / panel `ProjectionRefreshProvider`) keeps propagating. Regression test: `Tenant_audit_page_survives_global_administrator_projection_fault_during_load`.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:499-513 guards supplementary global-administrator loading; TenantAuditPageTests.cs:801 covers the regression.

### DW-8: Tenant panel terminal-state focus parity
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: CorrectionStartPanel.SetSnapshot
reason: The legacy ledger defers this issue: Tenant panel terminal-state focus parity. Original context is preserved in legacy-detail.
legacy-detail: - **Tenant panel terminal-state focus parity** — `CorrectionStartPanel.SetSnapshot` now focuses on all six terminal states (Confirmed/Failed/Rejected/Degraded/UnableToVerify/AlreadyApplied), matching the GA panel. Test: `Panel_rejected_terminal_state_moves_focus_to_lifecycle`.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:361-373 covers all terminal states; CorrectionStartPanelTests.cs:420 covers rejected-state focus.

### DW-9: Tenant confirm fail-closed on stale/degraded
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: TenantAuditPage.RefreshTenantProjectionAsync
reason: The legacy ledger defers this issue: Tenant confirm fail-closed on stale/degraded. Original context is preserved in legacy-detail.
legacy-detail: - **Tenant confirm fail-closed on stale/degraded** — `TenantAuditPage.RefreshTenantProjectionAsync` (the tenant confirm-time provider) returns the projection only when `Freshness is Current`, else `null`, so the existing `ConfirmProjection(null)` fails closed (parity with the GA `Freshness=Current` gate). Test: `Panel_does_not_confirm_when_projection_refresh_provider_returns_no_fresh_projection`.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1042-1062 returns confirmation evidence only for Current freshness; CorrectionStartPanelTests.cs:451 covers fail-closed behavior.

### DW-10: Tenant corrective-proof time tie-back + invariant culture
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: CorrectionStartPanel.QueryCorrectiveProofAsync
reason: The legacy ledger defers this issue: Tenant corrective-proof time tie-back + invariant culture. Original context is preserved in legacy-detail.
legacy-detail: - **Tenant corrective-proof time tie-back + invariant culture** — `CorrectionStartPanel.QueryCorrectiveProofAsync` now parses `originalTimestamp` with `InvariantCulture`+`RoundtripKind`, lower-bounds the audit query with `From: originalTimestamp`, filters `row.Timestamp > originalTimestamp`, newest-first; `ProofTimestampLabel` and `TenantCorrectionPreviewSnapshot.WithCorrectiveProof` parse with `InvariantCulture` (mirrors the GA fix). Test: `Panel_proof_lookup_ignores_audit_row_not_newer_than_the_original_event`.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:531-575 uses invariant parsing and strictly newer proof rows; CorrectionStartPanelTests.cs:478 rejects historical rows.

### DW-11: Concurrent correction opens out of order
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: OpenCorrectionAsync
reason: The legacy ledger defers this issue: Concurrent correction opens out of order. Original context is preserved in legacy-detail.
legacy-detail: - **Concurrent correction opens out of order** — `OpenCorrectionAsync` captures a `_correctionOpenGeneration` synchronously at entry and applies the active intent only if still latest. (No dedicated bUnit test — timing-deterministic two-open harness was judged more flake-prone than valuable; verified by construction + unchanged single-open tests.)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:893-925 uses _correctionOpenGeneration so only the newest concurrent open applies.

### DW-12: No story-specific 5.7 gateway-routing test
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: TenantCommandGatewayTests
reason: The legacy ledger defers this issue: No story-specific 5.7 gateway-routing test. Original context is preserved in legacy-detail.
legacy-detail: - **No story-specific 5.7 gateway-routing test** — CLOSED as already-covered: `TenantCommandGatewayTests` already pins the full `system / global-administrators / global-administrators` triple + CommandType + literal payload for both Set and Remove; the item was conditional on the gateway being touched (it wasn't).
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs:31-67 pins the fixed aggregate and both command types.

### DW-13: Create-tenant freshness gate narrowed `Current or Unknown → Current`
origin: migrated from legacy ledger ("2026-06-30 Correct Course — Deferred Work (Tenants-Owned, Actionable) Implemented"), 2026-08-25
location: TenantsWorkspace.razor
reason: The legacy ledger defers this issue: Create-tenant freshness gate narrowed `Current or Unknown → Current`. Original context is preserved in legacy-detail.
legacy-detail: - **Create-tenant freshness gate narrowed `Current or Unknown → Current`** — CLOSED as resolved: the gate is back to `Current or Unknown` (`TenantsWorkspace.razor` `CreateTenantFlow IsFresh`), matching the documented first-tenant bootstrap exception. The "restore" path was taken.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:416-425 restores Current or the documented authoritative first-tenant Unknown bootstrap case.

### DW-14: FrontComposer owner handoff
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
location: Hexalith.FrontComposer
reason: The legacy ledger defers this issue: FrontComposer owner handoff. Original context is preserved in legacy-detail.
legacy-detail: - **FrontComposer owner handoff** `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening` — **IMPLEMENTED** in `Hexalith.FrontComposer`. `FcPageHeader` no longer emits a competing `banner` (header root is `role="presentation"`); `FrontComposerShell` exposes `ContentLabel`/`ContentLabelledBy` + a new `FcContentLabel` marker so a page can name the shell `main` landmark without an orphaned page-level `aria-labelledby`; blank `Heading` now fail-safes (no dangling `<h1>`, replacing the prior throw); `FocusHeadingAsync()` fails diagnostically when the heading is not focusable. Backward-compatible (new params default to null). FrontComposer Shell suite 1962/0 failed.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:141-148 owns the named main landmark; FcPageHeader.razor:7-44 implements presentation and blank-heading outcomes.

### DW-15: EventStore owner handoff
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
location: Index.razor; Commands.razor
reason: The legacy ledger defers this issue: EventStore owner handoff. Original context is preserved in legacy-detail.
legacy-detail: - **EventStore owner handoff** `eventstore-2026-06-19-admin-ui-and-query-record-followup` — **IMPLEMENTED** (Admin.UI a11y portion) in `Hexalith.EventStore`: `Index.razor` stat cards, `ActivityChart` (`role="group"` + real `<button>` bars), `StorageTreemap` (focusable `role="button"` cells), `RelatedTypeList`, `TypeDetailPanel`, `DaprHealthHistory`, and non-functional `cursor:pointer` spans on `Commands.razor`/`Events.razor` all remediated; conformance carve-out comment updated. The retired actor-routing sub-item was already verified stale/resolved (see below). Admin.UI.Tests green except 6 pre-existing unrelated `Dw5GovernanceAtddTests` (missing DW5 evidence artifact, not introduced here).
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Pages/Index.razor:25-51, Components/ActivityChart.razor:28-46, and Components/StorageTreemap.razor:65-76 contain the fixes.

### DW-16: EventStore owner handoff
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
location: IReadModelFreshness
reason: The legacy ledger defers this issue: EventStore owner handoff. Original context is preserved in legacy-detail.
legacy-detail: - **EventStore owner handoff** `eventstore-2026-06-19-read-model-freshness-metadata` — **IMPLEMENTED** in `Hexalith.EventStore.Client.Projections`: `IReadModelFreshness` (`ProjectedAt`/`ProjectionVersion`), `ReadModelFreshnessState`, `ReadModelFreshnessThresholds`, pure `ReadModelFreshness.Classify/Age`, plus `IReadModelStore.GetWithFreshnessAsync<T>()` and `ToQueryResponseMetadata()` bridges. This is the generic, persisted-timestamp replacement for the Tenants hand-rolled `TenantFreshnessState`; Tenants-side adoption is implemented by `cc-2026-06-25-tenant-read-model-freshness-adoption`. Client.Tests 462/462.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/ReadModels/IReadModelFreshness.cs:22-34, ReadModelFreshness.cs:30-48, and ReadModelFreshnessExtensions.cs:23-85 implement the shared freshness contract.

### DW-17: Epic 11 — Production Authorization Readiness (persisted DataProtection key ring)
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
location: src/Hexalith.Tenants/Program.cs; statestore.yaml
reason: The legacy ledger defers this issue: Epic 11 — Production Authorization Readiness (persisted DataProtection key ring). Original context is preserved in legacy-detail.
legacy-detail: - **Epic 11 — Production Authorization Readiness (persisted DataProtection key ring)** — **IMPLEMENTED**. A Dapr-state-store-backed `IXmlRepository` (`DaprXmlRepository`) + `AddEventStoreDataProtection(...)` live in the `Hexalith.EventStore.DomainService` host-SDK layer; backend is chosen by `statestore.yaml` (Redis in prod) so the Tenants domain package gains NO infra SDK. `src/Hexalith.Tenants/Program.cs` swaps to `AddEventStoreDataProtection(config, "Hexalith.Tenants")`; production persists to the `statestore` under the application-specific key `hexalith-tenants-dataprotection-keys`, Development stays explicitly ephemeral. DomainService.Tests 36/36 (incl. cross-replica reload + ETag concurrency).
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants/Program.cs:79-87 and EventStoreDataProtectionServiceCollectionExtensions.cs:57-93 configure Dapr-backed or explicit ephemeral DataProtection; deploy/dapr/statestore.yaml:3-8 documents the key.

### DW-18: Pending (newly discovered) — Memories-integration doc/test drift
origin: migrated from legacy ledger ("2026-06-21 Correct Course — Deferred + Pending Work Implemented"), 2026-08-25
location: docs/cross-aggregate-timing.md; docs/sample-consuming-service-walkthrough.md
reason: The legacy ledger defers this issue: Pending (newly discovered) — Memories-integration doc/test drift. Original context is preserved in legacy-detail.
legacy-detail: - **Pending (newly discovered) — Memories-integration doc/test drift** — **FIXED**. The committed Memories search-index integration added the local Memories app id to the AppHost `pubsub.yaml` scopes and 4 `MemoriesSearchIndexEventPublisher` handlers to the Sample program, but left 3 conformance/doc tests red on `main`. Updated `EventPublicationConfigurationTests` + `CrossAggregateTimingDocumentationTests` (local now scopes `memories`; production stays `eventstore`+`sample`), `docs/cross-aggregate-timing.md`, and `docs/sample-consuming-service-walkthrough.md`. Tenants Server.Tests back to 700/700.
status: done 2026-06-21
archived: 2026-10-12
resolution: Legacy completion record: - **Pending (newly discovered) — Memories-integration doc/test drift** — **FIXED**. The committed Memories search-index integration added the local Memories app id to the AppHost `pubsub.yaml` scopes and 4 `MemoriesSearchIndexEventPublisher` handlers to the Sample program, but left 3 conformance/doc tests red on `main`. Updated `EventPublicationConfigurationTests` + `CrossAggregateTimingDocumentationTests` (local now scopes `memories`; production stays `eventstore`+`sample`), `docs/cross-aggregate-timing.md`, and `docs/sample-consuming-service-walkthrough.md`. Tenants Server.Tests back to 700/700.

### DW-19: Freshness is no longer derived from response `ServedAt`. The implemented direct-read rule treats a real read-model ETag/projection version as `current`; absent markers resolve to `unknown`
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
location: ServedAt
reason: The legacy ledger defers this issue: Freshness is no longer derived from response `ServedAt`. Original context is preserved in legacy-detail.
legacy-detail: - Freshness is no longer derived from response `ServedAt`. The implemented direct-read rule treats a real read-model ETag/projection version as `current`; absent markers resolve to `unknown`.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants/Queries/TenantQueryResult.cs:38-60 consumes IReadModelFreshness; TenantQueryFreshnessTests.cs:35-85 proves ProjectedAt classification and ServedAt response timing.

### DW-20: Generic projection age/version metadata is not available from the current `IReadModelStore` contract. Do not add Tenants-owned generic persistence scaffolding; the remaining threshold-based age metadata need is routed to the EventStore owner handoff below
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
location: IReadModelStore
reason: The legacy ledger defers this issue: Generic projection age/version metadata is not available from the current `IReadModelStore` contract. Original context is preserved in legacy-detail.
legacy-detail: - Generic projection age/version metadata is not available from the current `IReadModelStore` contract. Do not add Tenants-owned generic persistence scaffolding; the remaining threshold-based age metadata need is routed to the EventStore owner handoff below.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/ReadModels/IReadModelFreshness.cs:22-34, ReadModelFreshness.cs:30-48, and ReadModelFreshnessExtensions.cs:23-85 implement the shared freshness contract.

### DW-21: Null/empty read-model ETag behavior is explicit and tested: successful REST reads return 200 with no ETag, no projection-version header, no served-at header, and no 304 support
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Null/empty read-model ETag behavior is explicit and tested: successful REST reads return 200 with no ETag, no projection-version header, no served-at header, and no 304 support. Original context is preserved in legacy-detail.
legacy-detail: - Null/empty read-model ETag behavior is explicit and tested: successful REST reads return 200 with no ETag, no projection-version header, no served-at header, and no 304 support.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants/Queries/TenantQueryResult.cs:18-35 emits no metadata when the legacy ETag-only path has no usable ETag; TenantQueryFreshnessTests.cs:70-85 covers persisted freshness.

### DW-22: ETag handling is hardened and tested for weak tags, `*`, escaped strong tags, and unsupported multi-tag input
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: ETag handling is hardened and tested for weak tags, `*`, escaped strong tags, and unsupported multi-tag input. Original context is preserved in legacy-detail.
legacy-detail: - ETag handling is hardened and tested for weak tags, `*`, escaped strong tags, and unsupported multi-tag input.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:610-650 parses EntityTagHeaderValue and rejects weak, wildcard, empty, duplicate, quoted, and control-bearing validators; TenantsRestQueryClientTests.cs:1165-1469 covers them.

### DW-23: REST/handler read-model reconstruction coverage now proves a recreated controller factory can serve the persisted read model from the shared store and honor 304 through the production REST/handler path
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: REST/handler read-model reconstruction coverage now proves a recreated controller factory can serve the persisted read model from the shared store and honor 304 through the production REST/handler path. Original context is preserved in legacy-detail.
legacy-detail: - REST/handler read-model reconstruction coverage now proves a recreated controller factory can serve the persisted read model from the shared store and honor 304 through the production REST/handler path.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs:793-833 exercises the production conditional path and complete 304 metadata contract.

### DW-24: Live populated-correlation gateway error coverage now asserts that `correlationId`, `reasonCode`, raw payload text, stack traces, tokens, cursors, and ETags do not reach user-facing copy
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
location: correlationId
reason: The legacy ledger defers this issue: Live populated-correlation gateway error coverage now asserts that `correlationId`, `reasonCode`, raw payload text, stack traces, tokens, cursors, and ETags do not reach user-facing copy. Original context is preserved in legacy-detail.
legacy-detail: - Live populated-correlation gateway error coverage now asserts that `correlationId`, `reasonCode`, raw payload text, stack traces, tokens, cursors, and ETags do not reach user-facing copy.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs:206-237 proves raw payload, token, secret user, and correlation data never reach SafeMessage.

### DW-25: Current full-suite evidence (corrected 2026-06-21): the earlier `Server.Tests` blocker — 3 DAPR component expectation tests asserting removed `enableDeadLetter` / `deadLetterTopic` metadata — was resolved on 2026-06-20 by `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`; full `Server.Tests` now passes 700/700. `IntegrationTests` passes with DAPR/Aspire/performance skips. The old health-readiness blocker wording is no longer current evidence
origin: migrated from legacy ledger ("`cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening`"), 2026-08-25
location: Server.Tests
reason: The legacy ledger defers this issue: Current full-suite evidence (corrected 2026-06-21): the earlier `Server.Tests` blocker — 3 DAPR component expectation tests asserting removed `enableDeadLetter` / `deadLetterTopic` metadata — was resolved on 2026-06-20 by `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`; full `Server.Tests` now passes 700/700. Original context is preserved in legacy-detail.
legacy-detail: - Current full-suite evidence (corrected 2026-06-21): the earlier `Server.Tests` blocker — 3 DAPR component expectation tests asserting removed `enableDeadLetter` / `deadLetterTopic` metadata — was resolved on 2026-06-20 by `cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`; full `Server.Tests` now passes 700/700. `IntegrationTests` passes with DAPR/Aspire/performance skips. The old health-readiness blocker wording is no longer current evidence.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs:114-172 guards removal of inert Dapr dead-letter metadata and the exact component-scope contract.

### DW-26: Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18. Original context is preserved in legacy-detail.
legacy-detail: - Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18.
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: _bmad-output/implementation-artifacts/spec-frontcomposer-fluent-structural-and-style-conformance-sweep.md:145 records the independent 2026-06-18 three-layer code review and reproduced verification.

### DW-27: Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18. Original context is preserved in legacy-detail.
legacy-detail: - Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18.
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: _bmad-output/implementation-artifacts/spec-frontcomposer-shell-and-adminui-fluent-conformance-audit.md:246-248 records the 2026-06-18 independent review resolving findings and advancing the story to done.

### DW-28: Compact non-zero spacing (e.g. `margin:0.5rem`/`padding:0.5rem`) is now flagged by the styling-ownership guard. The `(?!0)` zero-skip was replaced with a zero-token matcher that still skips genuine resets (`0`, `0 0 0 0`, `0px`, `0 !important`). No real component CSS regressed
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Compact non-zero spacing (e.g. Original context is preserved in legacy-detail.
legacy-detail: - Compact non-zero spacing (e.g. `margin:0.5rem`/`padding:0.5rem`) is now flagged by the styling-ownership guard. The `(?!0)` zero-skip was replaced with a zero-token matcher that still skips genuine resets (`0`, `0 0 0 0`, `0px`, `0 !important`). No real component CSS regressed.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:102-109 and 632-661 pin compact nonzero versus true-zero behavior.

### DW-29: The inline-style guard was widened beyond flex/grid/gap to also cover spacing (margin/padding), sizing (width/inline-size), and alignment (justify-content/align-items), and now scans both quote styles. No `.razor` carries inline `style=`
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: razor
reason: The legacy ledger defers this issue: The inline-style guard was widened beyond flex/grid/gap to also cover spacing (margin/padding), sizing (width/inline-size), and alignment (justify-content/align-items), and now scans both quote styles. Original context is preserved in legacy-detail.
legacy-detail: - The inline-style guard was widened beyond flex/grid/gap to also cover spacing (margin/padding), sizing (width/inline-size), and alignment (justify-content/align-items), and now scans both quote styles. No `.razor` carries inline `style=`.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:85-97,492-511,665-679 enforce widened inline-layout spacing, sizing, and alignment cases.

### DW-30: The `<div>`/`<span>` budget now excludes Razor (`@* *@`) and HTML (`<!-- -->`) comments before counting
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: The `<div>`/`<span>` budget now excludes Razor (`@* *@`) and HTML (`<!-- -->`) comments before counting. Original context is preserved in legacy-detail.
legacy-detail: - The `<div>`/`<span>` budget now excludes Razor (`@* *@`) and HTML (`<!-- -->`) comments before counting.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:692-700,844-847 strips comments before wrapper counting.

### DW-31: `fc-css-exception` scoping decision: kept RULE-level with documented rationale; a unit test proves a marker exempts only its own rule and does not leak to the next rule
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: `fc-css-exception` scoping decision: kept RULE-level with documented rationale; a unit test proves a marker exempts only its own rule and does not leak to the next rule. Original context is preserved in legacy-detail.
legacy-detail: - `fc-css-exception` scoping decision: kept RULE-level with documented rationale; a unit test proves a marker exempts only its own rule and does not leak to the next rule.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:561-564,705-718 scopes exceptions so markers cannot leak.

### DW-32: `:focus-visible` exemption decision: NARROWED. The blanket exemption was removed; focus-ring affordances (outline/outline-offset/outline-color) are untracked so genuine focus rules still pass, but a `:focus-visible` rule that owns layout/spacing/typography is now flagged unless documented
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: `:focus-visible` exemption decision: NARROWED. Original context is preserved in legacy-detail.
legacy-detail: - `:focus-visible` exemption decision: NARROWED. The blanket exemption was removed; focus-ring affordances (outline/outline-offset/outline-color) are untracked so genuine focus rules still pass, but a `:focus-visible` rule that owns layout/spacing/typography is now flagged unless documented.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:565-568,723-735 permits focus-visible affordances while catching layout ownership.

### DW-33: `RemoveForcedColorsMediaBlocks` now skips braces inside CSS comments and quoted strings so a stray brace cannot leak the block tail back into the scan
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: RemoveForcedColorsMediaBlocks
reason: The legacy ledger defers this issue: `RemoveForcedColorsMediaBlocks` now skips braces inside CSS comments and quoted strings so a stray brace cannot leak the block tail back into the scan. Original context is preserved in legacy-detail.
legacy-detail: - `RemoveForcedColorsMediaBlocks` now skips braces inside CSS comments and quoted strings so a stray brace cannot leak the block tail back into the scan.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/DomainUiFluentConformanceTests.cs:740-753,771-841 implements and tests brace-aware parsing.

### DW-34: `MemberAccessReview` gained bUnit coverage proving the change-role and remove-member `aria-controls` resolve to a rendered active-region `id` after the FluentStack migration
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: MemberAccessReview
reason: The legacy ledger defers this issue: `MemberAccessReview` gained bUnit coverage proving the change-role and remove-member `aria-controls` resolve to a rendered active-region `id` after the FluentStack migration. Original context is preserved in legacy-detail.
legacy-detail: - `MemberAccessReview` gained bUnit coverage proving the change-role and remove-member `aria-controls` resolve to a rendered active-region `id` after the FluentStack migration.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:182-195,241,262 names controlled regions; TenantDetailSurfaceTests.cs:3979-4009 verifies resolution.

### DW-35: `TenantAuditPage` renders a localized fallback (`Tenants.Audit.UnknownTenant`) for a blank/whitespace `TenantId` instead of a dangling heading
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: TenantAuditPage
reason: The legacy ledger defers this issue: `TenantAuditPage` renders a localized fallback (`Tenants.Audit.UnknownTenant`) for a blank/whitespace `TenantId` instead of a dangling heading. Original context is preserved in legacy-detail.
legacy-detail: - `TenantAuditPage` renders a localized fallback (`Tenants.Audit.UnknownTenant`) for a blank/whitespace `TenantId` instead of a dangling heading.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:269-272 supplies the localized fallback; TenantAuditPageTests.cs:783-797 covers blank IDs.

### DW-36: The claim that the styling scan is blind to declarations inside `@media` blocks was verified as a false positive in the structural/style story and is not open work
origin: migrated from legacy ledger ("`cc-2026-06-19-domain-ui-governance-and-accessibility-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: The claim that the styling scan is blind to declarations inside `@media` blocks was verified as a false positive in the structural/style story and is not open work. Original context is preserved in legacy-detail.
legacy-detail: - The claim that the styling scan is blind to declarations inside `@media` blocks was verified as a false positive in the structural/style story and is not open work.
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: _bmad-output/implementation-artifacts/spec-frontcomposer-fluent-structural-and-style-conformance-sweep.md:152-154 documents the empirical retest and dismissal as a verified false positive.

### DW-37: Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18. Original context is preserved in legacy-detail.
legacy-detail: - Code review of `spec-frontcomposer-fluent-structural-and-style-conformance-sweep` on 2026-06-18.
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: _bmad-output/implementation-artifacts/spec-frontcomposer-fluent-structural-and-style-conformance-sweep.md:145 records the completed 2026-06-18 review.

### DW-38: Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18. Original context is preserved in legacy-detail.
legacy-detail: - Code review of `spec-frontcomposer-shell-and-adminui-fluent-conformance-audit` on 2026-06-18.
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: _bmad-output/implementation-artifacts/spec-frontcomposer-shell-and-adminui-fluent-conformance-audit.md:246-248 records the completed 2026-06-18 review.

### DW-39: Current deployment docs/YAML scan on 2026-06-19
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: docs/YAML
reason: The legacy ledger defers this issue: Current deployment docs/YAML scan on 2026-06-19. Original context is preserved in legacy-detail.
legacy-detail: - Current deployment docs/YAML scan on 2026-06-19.
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: _bmad-output/implementation-artifacts/cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup.md:83-91 records the current YAML/docs scan, patches, and documentation assertions.

### DW-40: DAPR v1.17 topic-scoping documentation checked on 2026-06-20
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: DAPR v1.17 topic-scoping documentation checked on 2026-06-20. Original context is preserved in legacy-detail.
legacy-detail: - DAPR v1.17 topic-scoping documentation checked on 2026-06-20.
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: _bmad-output/implementation-artifacts/cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup.md:81 records the DAPR v1.17/v1.18 topic-scoping documentation verification.

### DW-41: Production `deploy/dapr/pubsub.yaml` denies `sample` publishing with an empty topic list (`publishingScopes: "sample="`) and allows `sample` to subscribe to `tenants.events`, while leaving `eventstore` unlisted so it keeps unrestricted publish access (required for EventStore dynamic per-tenant topic provisioning, NFR20 — listing `eventstore` is the documented anti-pattern)
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: deploy/dapr/pubsub.yaml
reason: The legacy ledger defers this issue: Production `deploy/dapr/pubsub.yaml` denies `sample` publishing with an empty topic list (`publishingScopes: "sample="`) and allows `sample` to subscribe to `tenants.events`, while leaving `eventstore` unlisted so it keeps unrestricted publish access (required for EventStore dynamic per-tenant topic provisioning, NFR20 — listing `eventstore` is the… Original context is preserved in legacy-detail.
legacy-detail: - Production `deploy/dapr/pubsub.yaml` denies `sample` publishing with an empty topic list (`publishingScopes: "sample="`) and allows `sample` to subscribe to `tenants.events`, while leaving `eventstore` unlisted so it keeps unrestricted publish access (required for EventStore dynamic per-tenant topic provisioning, NFR20 — listing `eventstore` is the documented anti-pattern). [2026-06-20 code-review correction: an earlier explicit `eventstore=tenants.events,deadletter.tenants.events;sample=` allow-list was reverted because it violated EventStore NFR20 and would have silently denied dynamic-tenant topics.]
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: deploy/dapr/pubsub.yaml:13-21,38-44 encodes unrestricted EventStore publishing, sample restrictions, and exact component scopes.

### DW-42: Local AppHost pub/sub intentionally omits topic-level scopes while retaining component-level `eventstore` and `sample` scopes; the difference is documented in the component YAML and timing guide
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: eventstore
reason: The legacy ledger defers this issue: Local AppHost pub/sub intentionally omits topic-level scopes while retaining component-level `eventstore` and `sample` scopes; the difference is documented in the component YAML and timing guide. Original context is preserved in legacy-detail.
legacy-detail: - Local AppHost pub/sub intentionally omits topic-level scopes while retaining component-level `eventstore` and `sample` scopes; the difference is documented in the component YAML and timing guide.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml:10-15,29-34 explicitly omits local topic scopes and declares consumers.

### DW-43: `docs/cross-aggregate-timing.md` distinguishes subscriber redelivery on `tenants.events` from EventStore's application-level dead-letter publisher for `deadletter.tenants.events`
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: docs/cross-aggregate-timing.md
reason: The legacy ledger defers this issue: `docs/cross-aggregate-timing.md` distinguishes subscriber redelivery on `tenants.events` from EventStore's application-level dead-letter publisher for `deadletter.tenants.events`. Original context is preserved in legacy-detail.
legacy-detail: - `docs/cross-aggregate-timing.md` distinguishes subscriber redelivery on `tenants.events` from EventStore's application-level dead-letter publisher for `deadletter.tenants.events`.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: docs/cross-aggregate-timing.md:80-89,129-133 distinguishes EventStore application dead-lettering from Dapr subscriber redelivery.

### DW-44: `CrossAggregateTimingDocumentationTests` guards the production topic-scope contract, local topic-scope omission, application-level dead-letter wording, and the absence of DAPR subscriber-failure-to-dead-letter wording
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: CrossAggregateTimingDocumentationTests
reason: The legacy ledger defers this issue: `CrossAggregateTimingDocumentationTests` guards the production topic-scope contract, local topic-scope omission, application-level dead-letter wording, and the absence of DAPR subscriber-failure-to-dead-letter wording. Original context is preserved in legacy-detail.
legacy-detail: - `CrossAggregateTimingDocumentationTests` guards the production topic-scope contract, local topic-scope omission, application-level dead-letter wording, and the absence of DAPR subscriber-failure-to-dead-letter wording.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs:114-172 guards the YAML and documentation contract together.

### DW-45: June 18 review-record contradictions are kept as routed, stale/resolved, or future-owner handoff entries instead of open Tenants implementation work
origin: migrated from legacy ledger ("`cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: June 18 review-record contradictions are kept as routed, stale/resolved, or future-owner handoff entries instead of open Tenants implementation work. Original context is preserved in legacy-detail.
legacy-detail: - June 18 review-record contradictions are kept as routed, stale/resolved, or future-owner handoff entries instead of open Tenants implementation work.
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: _bmad-output/implementation-artifacts/cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup.md:84,92 records normalized routing/evidence records and the stale/resolved EventStore item.

### DW-46: `FrontComposerShell` exposes a shell content landmark contract that can be a native `<main>` or an equivalent role with an accessible-name parameter
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
location: FrontComposerShell
reason: The legacy ledger defers this issue: `FrontComposerShell` exposes a shell content landmark contract that can be a native `<main>` or an equivalent role with an accessible-name parameter. Original context is preserved in legacy-detail.
legacy-detail: - `FrontComposerShell` exposes a shell content landmark contract that can be a native `<main>` or an equivalent role with an accessible-name parameter.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:141-148 renders one named main; FcContentLabel.razor.cs:5-11 provides the no-markup bridge.

### DW-47: Tenants page headings can name the shell main landmark without orphaned page-level `aria-labelledby`
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Tenants page headings can name the shell main landmark without orphaned page-level `aria-labelledby`. Original context is preserved in legacy-detail.
legacy-detail: - Tenants page headings can name the shell main landmark without orphaned page-level `aria-labelledby`.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:141-148 renders one named main; FcContentLabel.razor.cs:5-11 provides the no-markup bridge.

### DW-48: `FcPageHeader` no longer creates a competing global `banner` landmark on every route page
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
location: FcPageHeader
reason: The legacy ledger defers this issue: `FcPageHeader` no longer creates a competing global `banner` landmark on every route page. Original context is preserved in legacy-detail.
legacy-detail: - `FcPageHeader` no longer creates a competing global `banner` landmark on every route page.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor:7-13 forces role=presentation.

### DW-49: `FcPageHeader` handles blank `Heading` fail-safely or documents a strict consumer contract with analyzable/tested guardrails
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
location: FcPageHeader
reason: The legacy ledger defers this issue: `FcPageHeader` handles blank `Heading` fail-safely or documents a strict consumer contract with analyzable/tested guardrails. Original context is preserved in legacy-detail.
legacy-detail: - `FcPageHeader` handles blank `Heading` fail-safely or documents a strict consumer contract with analyzable/tested guardrails.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor:34-44 suppresses blank headings.

### DW-50: `FocusHeadingAsync()` ensures the heading is focusable when used as a focus target or fails diagnostically when `HeadingTabIndex` is omitted
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
location: FocusHeadingAsync
reason: The legacy ledger defers this issue: `FocusHeadingAsync()` ensures the heading is focusable when used as a focus target or fails diagnostically when `HeadingTabIndex` is omitted. Original context is preserved in legacy-detail.
legacy-detail: - `FocusHeadingAsync()` ensures the heading is focusable when used as a focus target or fails diagnostically when `HeadingTabIndex` is omitted.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcPageHeader.razor.cs:94-126 documents and throws for non-focusable or missing headings.

### DW-51: FrontComposer H-FC-1: rework or re-justify `FcHomeCard` against pinned `FluentCard` support
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
location: FcHomeCard
reason: The legacy ledger defers this issue: FrontComposer H-FC-1: rework or re-justify `FcHomeCard` against pinned `FluentCard` support. Original context is preserved in legacy-detail.
legacy-detail: - FrontComposer H-FC-1: rework or re-justify `FcHomeCard` against pinned `FluentCard` support.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.FrontComposer/_bmad-output/project-docs/architecture.md:130-140 documents the FcHomeCard carve-out; FluentConformanceTests.cs:120-130 pins the allowlist.

### DW-52: FrontComposer H-FC-2: consider parity guards for structural/style governance
origin: migrated from legacy ledger ("FrontComposer owner: `frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: FrontComposer H-FC-2: consider parity guards for structural/style governance. Original context is preserved in legacy-detail.
legacy-detail: - FrontComposer H-FC-2: consider parity guards for structural/style governance.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.FrontComposer/tests/Hexalith.FrontComposer.Tests/Governance/FluentConformanceTests.cs:120-130 enforces raw-control parity with a narrow documented exception.

### DW-53: Continue the Admin.UI audit remediation handoffs from `audit-frontcomposer-shell-adminui-fluent-2026-06-18.md`: `Index.razor` non-semantic clickable semantics, clickable-span remediation, `ActivityChart` a11y proof, `StorageTreemap` semantics/docs, and optional parity guards
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-admin-ui-and-query-record-followup`"), 2026-08-25
location: audit-frontcomposer-shell-adminui-fluent-2026-06-18.md; Index.razor
reason: The legacy ledger defers this issue: Continue the Admin.UI audit remediation handoffs from `audit-frontcomposer-shell-adminui-fluent-2026-06-18.md`: `Index.razor` non-semantic clickable semantics, clickable-span remediation, `ActivityChart` a11y proof, `StorageTreemap` semantics/docs, and optional parity guards. Original context is preserved in legacy-detail.
legacy-detail: - Continue the Admin.UI audit remediation handoffs from `audit-frontcomposer-shell-adminui-fluent-2026-06-18.md`: `Index.razor` non-semantic clickable semantics, clickable-span remediation, `ActivityChart` a11y proof, `StorageTreemap` semantics/docs, and optional parity guards.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Pages/Index.razor:25-51, Components/ActivityChart.razor:28-46, and Components/StorageTreemap.razor:65-76 contain the fixes.

### DW-54: If EventStore tests still encode the retired Tenants actor-routing assumption, update them under EventStore ownership
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-admin-ui-and-query-record-followup`"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: If EventStore tests still encode the retired Tenants actor-routing assumption, update them under EventStore ownership. Original context is preserved in legacy-detail.
legacy-detail: - If EventStore tests still encode the retired Tenants actor-routing assumption, update them under EventStore ownership.
status: done 2026-08-26
archived: 2026-10-12
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.Server/Services/DaprTenantQueryService.cs:195-215 uses the generic query endpoint without the retired Tenants projection-actor routing contract.

### DW-55: Add or expose shared read-model metadata for persisted projection timestamp/version if D6 threshold-based `aging` and `stale` states need to be computed generically
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-read-model-freshness-metadata`"), 2026-08-25
location: aging
reason: The legacy ledger defers this issue: Add or expose shared read-model metadata for persisted projection timestamp/version if D6 threshold-based `aging` and `stale` states need to be computed generically. Original context is preserved in legacy-detail.
legacy-detail: - Add or expose shared read-model metadata for persisted projection timestamp/version if D6 threshold-based `aging` and `stale` states need to be computed generically.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/ReadModels/IReadModelFreshness.cs:22-34, ReadModelFreshness.cs:30-48, and ReadModelFreshnessExtensions.cs:23-85 implement the shared freshness contract.

### DW-56: Keep the capability in `Hexalith.EventStore` (`IReadModelStore` / query metadata path) rather than adding Tenants-specific persistence scaffolding
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-read-model-freshness-metadata`"), 2026-08-25
location: Hexalith.EventStore
reason: The legacy ledger defers this issue: Keep the capability in `Hexalith.EventStore` (`IReadModelStore` / query metadata path) rather than adding Tenants-specific persistence scaffolding. Original context is preserved in legacy-detail.
legacy-detail: - Keep the capability in `Hexalith.EventStore` (`IReadModelStore` / query metadata path) rather than adding Tenants-specific persistence scaffolding.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.EventStore/src/Hexalith.EventStore.Client/ReadModels/IReadModelFreshness.cs:22-34, ReadModelFreshness.cs:30-48, and ReadModelFreshnessExtensions.cs:23-85 implement the shared freshness contract.

### DW-57: Once available, Tenants can map real persisted projection age/version through configurable thresholds; until then Tenants uses the direct-read ETag/version `current` rule and fails unmarked responses closed to `unknown`
origin: migrated from legacy ledger ("EventStore owner: `eventstore-2026-06-19-read-model-freshness-metadata`"), 2026-08-25
location: current
reason: The legacy ledger defers this issue: Once available, Tenants can map real persisted projection age/version through configurable thresholds; until then Tenants uses the direct-read ETag/version `current` rule and fails unmarked responses closed to `unknown`. Original context is preserved in legacy-detail.
legacy-detail: - Once available, Tenants can map real persisted projection age/version through configurable thresholds; until then Tenants uses the direct-read ETag/version `current` rule and fails unmarked responses closed to `unknown`.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants/Queries/TenantQueryResult.cs:38-60 consumes IReadModelFreshness; TenantQueryFreshnessTests.cs:35-85 proves ProjectedAt classification and ServedAt response timing.

### DW-58: ETag special-character (quote/comma) robustness — latent, non-exploitable. `NormalizeETagToken`/`Trim('"')` unquote any value that starts and ends with `"` (asymmetric vs raw store tokens) in `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs:87-107`; the client and server both reject commas with a substring check, dropping a single quoted strong tag whose content legitimately contains a comma (`src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs:25-29`); and client/server normalization disagree on quoted-whitespace/`"*"` edge inputs. These do not bite while DAPR/Redis read-model ETags remain opaque numeric strings without quotes or commas, and the emit→submit→compare round-trip is internally symmetric. Revisit if the EventStore read-model store contract ever emits special-character ETags (ties into the `eventstore-2026-06-19-read-model-freshness-metadata` handoff above)
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-tenant-query-freshness-etag-and-coverage-hardening (2026-06-19)"), 2026-08-25
location: src/Hexalith.Tenants/Controllers/TenantsQueryController.cs:87-107; src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs:25-29
reason: The legacy ledger defers this issue: ETag special-character (quote/comma) robustness — latent, non-exploitable. Original context is preserved in legacy-detail.
legacy-detail: - ETag special-character (quote/comma) robustness — latent, non-exploitable. `NormalizeETagToken`/`Trim('"')` unquote any value that starts and ends with `"` (asymmetric vs raw store tokens) in `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs:87-107`; the client and server both reject commas with a substring check, dropping a single quoted strong tag whose content legitimately contains a comma (`src/Hexalith.Tenants.UI/Services/Gateways/TenantsQueryApiClient.cs:25-29`); and client/server normalization disagree on quoted-whitespace/`"*"` edge inputs. These do not bite while DAPR/Redis read-model ETags remain opaque numeric strings without quotes or commas, and the emit→submit→compare round-trip is internally symmetric. Revisit if the EventStore read-model store contract ever emits special-character ETags (ties into the `eventstore-2026-06-19-read-model-freshness-metadata` handoff above).
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:610-650 parses EntityTagHeaderValue and rejects weak, wildcard, empty, duplicate, quoted, and control-bearing validators; TenantsRestQueryClientTests.cs:1165-1469 covers them.

### DW-59: CSS ownership guard logical longhand spacing — RESOLVED (2026-06-21 hardening). `DomainUiFluentConformanceTests` now tracks the logical longhands (`margin-inline-start/-end`, `padding-block-start/-end`, etc.) alongside the physical longhands and shorthand, with `[InlineData]` coverage for both flagged and zero-reset cases
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-domain-ui-governance-and-accessibility-hardening (2026-06-19)"), 2026-08-25
location: DomainUiFluentConformanceTests
reason: The legacy ledger defers this issue: CSS ownership guard logical longhand spacing — RESOLVED (2026-06-21 hardening). Original context is preserved in legacy-detail.
legacy-detail: - CSS ownership guard logical longhand spacing — **RESOLVED (2026-06-21 hardening).** `DomainUiFluentConformanceTests` now tracks the logical longhands (`margin-inline-start/-end`, `padding-block-start/-end`, etc.) alongside the physical longhands and shorthand, with `[InlineData]` coverage for both flagged and zero-reset cases.
status: done 2026-06-21
archived: 2026-10-12
resolution: Legacy completion record: - CSS ownership guard logical longhand spacing — **RESOLVED (2026-06-21 hardening).** `DomainUiFluentConformanceTests` now tracks the logical longhands (`margin-inline-start/-end`, `padding-block-start/-end`, etc.) alongside the physical longhands and shorthand, with `[InlineData]` coverage for both flagged and zero-reset cases.

### DW-60: Forced-colors malformed block handling — RESOLVED (2026-06-21 hardening). `RemoveForcedColorsMediaBlocks` plus a dedicated `Forced_colors_unterminated_block_does_not_hide_trailing_ownership` test now ensure an unterminated forced-colors block cannot hide trailing ownership declarations from the scan
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-domain-ui-governance-and-accessibility-hardening (2026-06-19)"), 2026-08-25
location: RemoveForcedColorsMediaBlocks
reason: The legacy ledger defers this issue: Forced-colors malformed block handling — RESOLVED (2026-06-21 hardening). Original context is preserved in legacy-detail.
legacy-detail: - Forced-colors malformed block handling — **RESOLVED (2026-06-21 hardening).** `RemoveForcedColorsMediaBlocks` plus a dedicated `Forced_colors_unterminated_block_does_not_hide_trailing_ownership` test now ensure an unterminated forced-colors block cannot hide trailing ownership declarations from the scan.
status: done 2026-06-21
archived: 2026-10-12
resolution: Legacy completion record: - Forced-colors malformed block handling — **RESOLVED (2026-06-21 hardening).** `RemoveForcedColorsMediaBlocks` plus a dedicated `Forced_colors_unterminated_block_does_not_hide_trailing_ownership` test now ensure an unterminated forced-colors block cannot hide trailing ownership declarations from the scan.

### DW-61: Sibling query ETag special-character robustness — quote/comma ETag edge cases surfaced again because the working-tree diff includes the completed tenant-query hardening story. Keep routed under the tenant-query review / EventStore read-model freshness handoff; it is outside the domain UI governance story
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-domain-ui-governance-and-accessibility-hardening (2026-06-19)"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Sibling query ETag special-character robustness — quote/comma ETag edge cases surfaced again because the working-tree diff includes the completed tenant-query hardening story. Original context is preserved in legacy-detail.
legacy-detail: - Sibling query ETag special-character robustness — quote/comma ETag edge cases surfaced again because the working-tree diff includes the completed tenant-query hardening story. Keep routed under the tenant-query review / EventStore read-model freshness handoff; it is outside the domain UI governance story.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:610-650 parses EntityTagHeaderValue and rejects weak, wildcard, empty, duplicate, quoted, and control-bearing validators; TenantsRestQueryClientTests.cs:1165-1469 covers them.

### DW-62: Application-level vs native dead-letter framing for operators — RESOLVED (2026-06-21 hardening). `deploy/dapr/README.md:53` now carries an explicit operator note scoping the "no native dead-letter" claim to the `pubsub` component shipped here and warning that an EventStore-provided component may set its own `enableDeadLetter`/`deadLetterTopic`
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup (2026-06-20)"), 2026-08-25
location: deploy/dapr/README.md:53
reason: The legacy ledger defers this issue: Application-level vs native dead-letter framing for operators — RESOLVED (2026-06-21 hardening). Original context is preserved in legacy-detail.
legacy-detail: - Application-level vs native dead-letter framing for operators — **RESOLVED (2026-06-21 hardening).** `deploy/dapr/README.md:53` now carries an explicit operator note scoping the "no native dead-letter" claim to the `pubsub` component shipped here and warning that an EventStore-provided component may set its own `enableDeadLetter`/`deadLetterTopic`.
status: done 2026-06-21
archived: 2026-10-12
resolution: Legacy completion record: - Application-level vs native dead-letter framing for operators — **RESOLVED (2026-06-21 hardening).** `deploy/dapr/README.md:53` now carries an explicit operator note scoping the "no native dead-letter" claim to the `pubsub` component shipped here and warning that an EventStore-provided component may set its own `enableDeadLetter`/`deadLetterTopic`.

### DW-63: Stale Server.Tests evidence line in `test-summary.md` — RESOLVED (2026-06-21 hardening). A correction note was added (`tests/test-summary.md:246`) recording that the 3-test Server.Tests blocker was resolved on 2026-06-20 and that Server.Tests passes; the old line is retained only as dated historical evidence
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-19-dapr-deployment-docs-and-deferred-record-cleanup (2026-06-20)"), 2026-08-25
location: tests/test-summary.md:246; test-summary.md
reason: The legacy ledger defers this issue: Stale Server.Tests evidence line in `test-summary.md` — RESOLVED (2026-06-21 hardening). Original context is preserved in legacy-detail.
legacy-detail: - Stale Server.Tests evidence line in `test-summary.md` — **RESOLVED (2026-06-21 hardening).** A correction note was added (`tests/test-summary.md:246`) recording that the 3-test Server.Tests blocker was resolved on 2026-06-20 and that Server.Tests passes; the old line is retained only as dated historical evidence.
status: done 2026-06-21
archived: 2026-10-12
resolution: Legacy completion record: - Stale Server.Tests evidence line in `test-summary.md` — **RESOLVED (2026-06-21 hardening).** A correction note was added (`tests/test-summary.md:246`) recording that the 3-test Server.Tests blocker was resolved on 2026-06-20 and that Server.Tests passes; the old line is retained only as dated historical evidence.

### DW-64: `<FcContentLabel>` single-writer dispose-clobber
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-21-frontcomposer-page-header-landmarks-and-contract-hardening (2026-06-25)"), 2026-08-25
location: references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcContentLabelCoordinator.cs:159; FcContentLabel.razor.cs:80-84
reason: The legacy ledger defers this issue: `<FcContentLabel>` single-writer dispose-clobber. Original context is preserved in legacy-detail.
legacy-detail: - **`<FcContentLabel>` single-writer dispose-clobber** — when two `<FcContentLabel>` markers render on one page, disposing one calls `FcContentLabelCoordinator.Reset()` (→ `Set(null, null)`), wiping a still-live sibling's accessible name on `#fc-main-content` until the survivor happens to re-render (`references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcContentLabelCoordinator.cs:159` + `FcContentLabel.razor.cs:80-84`). Real but silent a11y edge case; it faithfully mirrors the accepted, documented `FcPageLayoutCoordinator` "single-writer, last-writer-wins" pattern (identical latent limitation by design) and no current consumer renders two markers. Fix path if multi-writer support is ever needed: add a writer-identity/token guard so only the current writer's dispose resets — apply to BOTH coordinators together for consistency. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the dispose-clobber + single-writer last-writer-wins limitation is now recorded in the `FcContentLabel` XML `<remarks>` and a matching sentence on `FcContentLabelCoordinator`; the writer-identity guard remains the routed follow-up if multi-writer support is ever needed.
status: done 2026-07-01
archived: 2026-10-12
resolution: Legacy completion record: - **`<FcContentLabel>` single-writer dispose-clobber** — when two `<FcContentLabel>` markers render on one page, disposing one calls `FcContentLabelCoordinator.Reset()` (→ `Set(null, null)`), wiping a still-live sibling's accessible name on `#fc-main-content` until the survivor happens to re-render (`references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Layout/FcContentLabelCoordinator.cs:159` + `FcContentLabel.razor.cs:80-84`). Real but silent a11y edge case; it faithfully mirrors the accepted, documented `FcPageLayoutCoordinator` "single-writer, last-writer-wins" pattern (identical latent limitation by design) and no current consumer renders two markers. Fix path if multi-writer support is ever needed: add a writer-identity/token guard so only the current writer's dispose resets — apply to BOTH coordinators together for consistency. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the dispose-clobber + single-writer last-writer-wins limitation is now recorded in the `FcContentLabel` XML `<remarks>` and a matching sentence on `FcContentLabelCoordinator`; the writer-identity guard remains the routed follow-up if multi-writer support is ever needed.

### DW-65: Page-driven `<FcContentLabel>` accessible name absent on server first paint
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-21-frontcomposer-page-header-landmarks-and-contract-hardening (2026-06-25)"), 2026-08-25
location: FcContentLabel.razor.cs:67-77
reason: The legacy ledger defers this issue: Page-driven `<FcContentLabel>` accessible name absent on server first paint. Original context is preserved in legacy-detail.
legacy-detail: - **Page-driven `<FcContentLabel>` accessible name absent on server first paint** — registration is `OnAfterRender`-only (`FcContentLabel.razor.cs:67-77`), so on a static-SSR/prerender pass `#fc-main-content` emits no `aria-label`/`aria-labelledby` from the page-marker path; the name appears only after interactive hydration. The shell-parameter path (`ContentLabel`/`ContentLabelledBy`) is correct on first paint. Mirrors the established `FcPageLayout` coordinator pattern and is acceptable for this InteractiveServer library; recommend documenting the limitation in the `FcContentLabel` XML remarks. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the `OnAfterRender`-only first-paint limitation is now in the `FcContentLabel` XML `<remarks>`, naming the shell-parameter path as the first-paint-correct alternative.
status: done 2026-07-01
archived: 2026-10-12
resolution: Legacy completion record: - **Page-driven `<FcContentLabel>` accessible name absent on server first paint** — registration is `OnAfterRender`-only (`FcContentLabel.razor.cs:67-77`), so on a static-SSR/prerender pass `#fc-main-content` emits no `aria-label`/`aria-labelledby` from the page-marker path; the name appears only after interactive hydration. The shell-parameter path (`ContentLabel`/`ContentLabelledBy`) is correct on first paint. Mirrors the established `FcPageLayout` coordinator pattern and is acceptable for this InteractiveServer library; recommend documenting the limitation in the `FcContentLabel` XML remarks. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the `OnAfterRender`-only first-paint limitation is now in the `FcContentLabel` XML `<remarks>`, naming the shell-parameter path as the first-paint-correct alternative.

### DW-66: `FocusHeadingAsync()` no-op → throw is an undisclosed API behavior change
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-21-frontcomposer-page-header-landmarks-and-contract-hardening (2026-06-25)"), 2026-08-25
location: FcPageHeader.razor.cs:104-117; FcAggregateListPage.razor.cs:83-84
reason: The legacy ledger defers this issue: `FocusHeadingAsync()` no-op → throw is an undisclosed API behavior change. Original context is preserved in legacy-detail.
legacy-detail: - **`FocusHeadingAsync()` no-op → throw is an undisclosed API behavior change** (resolved `decision-needed` → defer by Administrator on 2026-06-25). Keep the diagnostic throw: it is the intended hardening (Requested outcome 5) and no live consumer regresses (`TenantsWorkspace` → `FcAggregateListPage` passes `HeadingTabIndex="-1"`, verified). Follow-up: document the no-op→throw change for external FrontComposer adopters in the changelog / `FcPageHeader.FocusHeadingAsync` remarks, and note that the `FcAggregateListPage` wrapper's `… ?? ValueTask.CompletedTask` only guards the pre-first-render null `@ref` window, not the new throw. `FcPageHeader.razor.cs:104-117`, `FcAggregateListPage.razor.cs:83-84`. FrontComposer submodule. — **DOCUMENTED 2026-07-01 (CC deferred-work):** FrontComposer has no CHANGELOG, so the adopter-facing no-op→throw behavior-change note (incl. the `FcAggregateListPage` `?? ValueTask.CompletedTask` caveat) was added to the `FcPageHeader.FocusHeadingAsync` XML `<remarks>`.
status: done 2026-07-01
archived: 2026-10-12
resolution: Legacy completion record: - **`FocusHeadingAsync()` no-op → throw is an undisclosed API behavior change** (resolved `decision-needed` → defer by Administrator on 2026-06-25). Keep the diagnostic throw: it is the intended hardening (Requested outcome 5) and no live consumer regresses (`TenantsWorkspace` → `FcAggregateListPage` passes `HeadingTabIndex="-1"`, verified). Follow-up: document the no-op→throw change for external FrontComposer adopters in the changelog / `FcPageHeader.FocusHeadingAsync` remarks, and note that the `FcAggregateListPage` wrapper's `… ?? ValueTask.CompletedTask` only guards the pre-first-render null `@ref` window, not the new throw. `FcPageHeader.razor.cs:104-117`, `FcAggregateListPage.razor.cs:83-84`. FrontComposer submodule. — **DOCUMENTED 2026-07-01 (CC deferred-work):** FrontComposer has no CHANGELOG, so the adopter-facing no-op→throw behavior-change note (incl. the `FcAggregateListPage` `?? ValueTask.CompletedTask` caveat) was added to the `FcPageHeader.FocusHeadingAsync` XML `<remarks>`.

### DW-67: SVG `<g tabindex="0">` focusability not guaranteed cross-browser
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-21-eventstore-admin-ui-a11y-remediation (2026-06-25)"), 2026-08-25
location: references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Components/StorageTreemap.razor:72; StorageTreemap.razor
reason: The legacy ledger defers this issue: SVG `<g tabindex="0">` focusability not guaranteed cross-browser. Original context is preserved in legacy-detail.
legacy-detail: - **SVG `<g tabindex="0">` focusability not guaranteed cross-browser** (`references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Components/StorageTreemap.razor:72`) — the treemap cells became focusable via `tabindex="0"` on an SVG `<g>` group. Modern Chromium/Edge/Firefox include tabindex'd SVG container elements in the tab order; Safari/older WebKit historically do not, which would leave the treemap cells (and their `role="button"` keyboard activation) unreachable by Tab there. For an internal EventStore Admin.UI targeting Chromium/Edge the practical risk is low. Follow-up: validate against the actual supported browser matrix; if Safari/WebKit must be supported, make the focusable element an SVG `<a>` or wrap an HTML control in `<foreignObject>`. The bUnit test only asserts the attribute is present, not that the browser focuses it. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the cross-browser caveat + `<a>`/`<foreignObject>` remedy is now recorded as a Razor comment above the focusable `<g role="button" tabindex="0">` in `StorageTreemap.razor`. Validation against the actual supported browser matrix remains the routed follow-up.
status: done 2026-07-01
archived: 2026-10-12
resolution: Legacy completion record: - **SVG `<g tabindex="0">` focusability not guaranteed cross-browser** (`references/Hexalith.EventStore/src/Hexalith.EventStore.Admin.UI/Components/StorageTreemap.razor:72`) — the treemap cells became focusable via `tabindex="0"` on an SVG `<g>` group. Modern Chromium/Edge/Firefox include tabindex'd SVG container elements in the tab order; Safari/older WebKit historically do not, which would leave the treemap cells (and their `role="button"` keyboard activation) unreachable by Tab there. For an internal EventStore Admin.UI targeting Chromium/Edge the practical risk is low. Follow-up: validate against the actual supported browser matrix; if Safari/WebKit must be supported, make the focusable element an SVG `<a>` or wrap an HTML control in `<foreignObject>`. The bUnit test only asserts the attribute is present, not that the browser focuses it. — **DOCUMENTED 2026-07-01 (CC deferred-work):** the cross-browser caveat + `<a>`/`<foreignObject>` remedy is now recorded as a Razor comment above the focusable `<g role="button" tabindex="0">` in `StorageTreemap.razor`. Validation against the actual supported browser matrix remains the routed follow-up.

### DW-68: Global Administrators / Audit discoverability after nav de-listing
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace (2026-06-27)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs
reason: The legacy ledger defers this issue: Global Administrators / Audit discoverability after nav de-listing. Original context is preserved in legacy-detail.
legacy-detail: - **Global Administrators / Audit discoverability after nav de-listing** — the approved 2026-06-27 IA (AC9) removed `/global-administrators` and audit from the Tenants left-menu; the routes, pages, and `GlobalAdministratorPolicy` are preserved, but the diff adds no module-internal/contextual entry point, so a global administrator can reach the surface only by typing the URL. The sprint-change-proposal explicitly defers this: GA/Audit "remain available through module-internal tabs or contextual entry points ... unless a future module-level IA decision adds them explicitly." Follow-up: when Product confirms the contextual entry-point IA, add a discoverable in-workspace path. (`src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:35-41 and AuditEvidenceEntryPoint.razor:5-18 expose authorized global-administrator and contextual audit entry points; tests cover both.

### DW-69: GlobalAdministratorPolicy now registered but unconsumed
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Group 1 re-review (2026-06-27, chunked)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs; src/Hexalith.Tenants.UI/Program.cs:33
reason: The legacy ledger defers this issue: GlobalAdministratorPolicy now registered but unconsumed. Original context is preserved in legacy-detail.
legacy-detail: - **GlobalAdministratorPolicy now registered but unconsumed** — extends the GA discoverability item above: after the nav `RequiredPolicy:` was removed, `Program.cs:33` still registers `Tenants.GlobalAdministrator` but nothing requires it (the GA page authorizes via `BffComposition` reflection). Retention is intentional pending the deferred contextual-entry-point IA decision; revisit (wire or remove) when that decision lands. (`src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs`, `src/Hexalith.Tenants.UI/Program.cs:33`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:101-106 registers GlobalAdministratorPolicy; src/Hexalith.Tenants.UI/Program.cs:76-88 applies it.

### DW-70: Create-tenant freshness gate narrowed `Current or Unknown` → `Current` — RESOLVED 2026-06-30 (CC deferred-work, verify-only)
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Group 1 re-review (2026-06-27, chunked)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor; TenantsWorkspace.razor
reason: The legacy ledger defers this issue: Create-tenant freshness gate narrowed `Current or Unknown` → `Current` — RESOLVED 2026-06-30 (CC deferred-work, verify-only). Original context is preserved in legacy-detail.
legacy-detail: - **~~Create-tenant freshness gate narrowed `Current or Unknown` → `Current`~~ — RESOLVED 2026-06-30 (CC deferred-work, verify-only)** — the "restore" path was taken: `TenantsWorkspace.razor` `CreateTenantFlow IsFresh` is back to `Freshness is Current or Unknown`, matching the documented first-tenant bootstrap exception (Unknown list freshness remains creatable). No code change this run; verified live. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:416-425 restores Current or the documented authoritative first-tenant Unknown bootstrap case.

### DW-71: Page-local tabs render empty tabpanels
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Group 1 re-review (2026-06-27, chunked)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:28-30
reason: The legacy ledger defers this issue: Page-local tabs render empty tabpanels. Original context is preserved in legacy-detail.
legacy-detail: - **Page-local tabs render empty tabpanels** — the new `FluentTabs` carry `Id`/`Header` only; active content renders in sibling `FcAggregateListPage` slots (`Body`/`Filters`/`States`), so the Fluent tab→tabpanel ARIA relationship points at empty regions. `aria-selected` is correct and tabs are keyboard reachable. This is an `FcAggregateListPage`-slot architectural nuance best owned upstream. Follow-up: FrontComposer/UX decision on associating `FcAggregateListPage` content with `FcPageToolbar`/tab tabpanels. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:28-30`)
status: done 2026-09-02
archived: 2026-10-12
resolution: already resolved: commit 8bdf7d62; TenantsWorkspace.razor:33-46 now uses FcPageTabs with real panel content, and TenantsWorkspaceTests.cs:91-120 proves reciprocal non-empty tab panels.
decision: 2026-08-25 Add FrontComposer contract — Introduce a shared explicit tab-to-panel association API in FrontComposer and migrate Tenants.

### DW-72: Page-local tabs a11y — empty tabpanels + missing Tenants-owned bUnit assertion
origin: migrated from legacy ledger ("Deferred from: code review of cc-2026-06-27-tenants-module-tabbed-workspace — Full review (2026-06-28)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:22-30
reason: The legacy ledger defers this issue: Page-local tabs a11y — empty tabpanels + missing Tenants-owned bUnit assertion. Original context is preserved in legacy-detail.
legacy-detail: - **Page-local tabs a11y — empty tabpanels + missing Tenants-owned bUnit assertion** — extends the 2026-06-27 Group-1 "empty tabpanels" defer above. AC12/AC13 keyboard/active-tab guarantees ride entirely on the Fluent `FluentTabs` primitive with no Tenants-owned `aria-selected`/keyboard-switch bUnit assertion; the added tests assert tab presence/text and routing only. Follow-up: pair the upstream FrontComposer/UX tabpanel-association decision with a focused active-tab/keyboard bUnit test once the structure is settled. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:22-30`)
status: done 2026-09-02
archived: 2026-10-12
resolution: already resolved: commit 8bdf7d62; TenantListSurfaceTests.cs:371-378,413-467 and TenantsWorkspaceTests.cs:91-120 cover selection changes, reciprocal tab/panel IDs, and retained content.
decision: 2026-08-25 Full workspace contract — Test initial and changed selection, tab-to-panel association, and supported keyboard transitions with the structural fix.

### DW-73: Global-administrator projection pagination ignored (>20 admins)
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification (2026-06-29)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs
reason: The legacy ledger defers this issue: Global-administrator projection pagination ignored (>20 admins). Original context is preserved in legacy-detail.
legacy-detail: - **Global-administrator projection pagination ignored (>20 admins)** — `GlobalAdministratorsRequest` defaults to PageSize=20 and `HasMore`/cursor are never read; the correction snapshot only inspects page 1's `Rows` for presence and admin count. For more than 20 global administrators: a restore of a 21st+ target is treated as not-applied and can never reach `present=true` (stuck `ProjectionPending`), and a revoke of a 21st+ target is blocked as "already removed". Pre-existing query-shape limitation reused by this story; unusual scale and most failure modes fail closed. Follow-up: design projection paging/aggregation for the fixed global-administrator projection. (`src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs`) — **UPDATE 2026-07-01 (CC deferred-work): the fail-OPEN is CLOSED.** The snapshot now reads `HasMore` and treats absence as conclusive only on a fully-loaded page: `ConfirmProjection` proves a revoke only on `!present && !HasMore` (killing the page-2 false-`Confirmed`), and `EvaluateCurrentProjection` fails closed to `UnableToVerify` (`…CurrentProjectionUnavailable`) rather than the false `AlreadyRemoved` (revoke) or a mis-armed grant (restore). Presence-found stays conclusive so page-1 corrections at scale are unaffected. The residual is now narrowed to the full multi-page load/aggregation that would let a page-2 correction actually RUN instead of being conservatively blocked — still a dedicated projection-paging story.
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: commit 7716f0b11423eb54d74935b6cc6e3edf405dc400; src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1093-1121 now uses the complete-projection loader for correction evidence.

### DW-74: No story-specific gateway-routing test — CLOSED 2026-06-30 (CC deferred-work) as already-covered
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification (2026-06-29)"), 2026-08-25
location: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs
reason: The legacy ledger defers this issue: No story-specific gateway-routing test — CLOSED 2026-06-30 (CC deferred-work) as already-covered. Original context is preserved in legacy-detail.
legacy-detail: - **~~No story-specific gateway-routing test~~ — CLOSED 2026-06-30 (CC deferred-work) as already-covered** — verification showed `TenantCommandGatewayTests` already pins the full `system / global-administrators / global-administrators` triple + CommandType + literal payload for both `SetGlobalAdministratorAsync` and `RemoveGlobalAdministratorAsync`. The item was explicitly conditional on the gateway being touched; it was not, so no new (near-duplicate) test was added. (`tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs:31-67 pins the fixed aggregate and both command types.

### DW-75: Terminal failure states reset to a fresh submittable preview on parent re-render (HIGH) — RESOLVED 2026-06-30
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
location: GlobalAdministratorCorrectionPanel.razor:220; CorrectionStartPanel.razor:286
reason: The legacy ledger defers this issue: Terminal failure states reset to a fresh submittable preview on parent re-render (HIGH) — RESOLVED 2026-06-30. Original context is preserved in legacy-detail.
legacy-detail: - **~~Terminal failure states reset to a fresh submittable preview on parent re-render (HIGH)~~ — RESOLVED 2026-06-30** — both panels now preserve any existing snapshot when the intent is unchanged (`_snapshot is not null && !intentChanged → return`), rebuilding only on a different/first intent, so post-submission terminal states survive parent re-renders without re-arming Submit. GA panel already carried the fix + regression test; the tenant panel was fixed in the code review with a matching `Failed_correction_survives_a_parent_re_render_without_re_arming_submit` test. Full UI suite 838/838 green. (`GlobalAdministratorCorrectionPanel.razor:220`, `CorrectionStartPanel.razor:286`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:244-261 preserves unchanged terminal snapshots; CorrectionStartPanelTests.cs:387 covers parity.

### DW-76: `ConfirmProjection` confirms off a known-Stale projection — RESOLVED 2026-06-30
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
location: GlobalAdministratorCorrectionSnapshot.cs
reason: The legacy ledger defers this issue: `ConfirmProjection` confirms off a known-Stale projection — RESOLVED 2026-06-30. Original context is preserved in legacy-detail.
legacy-detail: - **~~`ConfirmProjection` confirms off a known-Stale projection~~ — RESOLVED 2026-06-30** — two parts: (1) `ConfirmProjection` itself was hardened by the 2026-06-29 review (P2) to require `Kind Ready` + `Freshness Current`; (2) the live residual — the **pre-submit** gate `ProjectionIsReadable` still accepting `Stale`/non-current, which let a platform-authority correction be SUBMITTED against stale evidence — was fixed in the 2026-06-30 code review: `ProjectionIsReadable` now requires `Kind ∈ {Ready,Empty}` **and** `Freshness=Current`, mirroring the confirm/start gates (Empty-current kept for first-admin restore). (`GlobalAdministratorCorrectionSnapshot.cs` `ProjectionIsReadable`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs:286-305,365-371 requires Ready/Current evidence and !HasMore before confirmation.

### DW-77: Corrective-proof lookup may link the wrong historical audit row
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
location: CorrectionStartPanel.razor
reason: The legacy ledger defers this issue: Corrective-proof lookup may link the wrong historical audit row. Original context is preserved in legacy-detail.
legacy-detail: - **Corrective-proof lookup may link the wrong historical audit row** — **GLOBAL-ADMIN RESOLVED 2026-06-30; tenant-domain residual RESOLVED 2026-06-30 (CC deferred-work, Edit F).** The global-admin path requires parseable invariant original timestamp evidence, requests system audit rows from that timestamp, filters strictly newer corrective rows, and reports audit delayed when the timestamp is missing/malformed. The tenant-domain `CorrectionStartPanel.QueryCorrectiveProofAsync` now mirrors that pattern (invariant/roundtrip parse, `From: originalTimestamp`, `Timestamp > original`, newest-first). (`CorrectionStartPanel.razor` `QueryCorrectiveProofAsync`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:531-575 uses invariant parsing and strictly newer proof rows; CorrectionStartPanelTests.cs:478 rejects historical rows.

### DW-78: Focus call lacks `JSDisconnectedException` guard — RESOLVED 2026-06-30 (CC deferred-work, Edit A)
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
location: GlobalAdministratorCorrectionPanel.razor; CorrectionStartPanel.razor
reason: The legacy ledger defers this issue: Focus call lacks `JSDisconnectedException` guard — RESOLVED 2026-06-30 (CC deferred-work, Edit A). Original context is preserved in legacy-detail.
legacy-detail: - **~~Focus call lacks `JSDisconnectedException` guard~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit A)** — both `CorrectionStartPanel` and `GlobalAdministratorCorrectionPanel` `OnAfterRenderAsync` now wrap `_lifecycleElement.FocusAsync()` in `try/catch (JSDisconnectedException)`, matching the existing `TenantAuditPage` guards. (`GlobalAdministratorCorrectionPanel.razor`, `CorrectionStartPanel.razor`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:322-334 and GlobalAdministratorCorrectionPanel.razor:279-291 catch JSDisconnectedException around focus.

### DW-79: Global-admin projection query unguarded in the page-load critical path — RESOLVED 2026-06-30 (CC deferred-work, Edit B)
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
location: TenantAuditPage.razor
reason: The legacy ledger defers this issue: Global-admin projection query unguarded in the page-load critical path — RESOLVED 2026-06-30 (CC deferred-work, Edit B). Original context is preserved in legacy-detail.
legacy-detail: - **~~Global-admin projection query unguarded in the page-load critical path~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit B)** — `LoadAsync` now wraps the supplementary global-administrator enrichment in `catch (… EventStoreGatewayException or HttpRequestException or JsonException)`; the confirm-time path (`OpenCorrectionAsync` / panel provider) keeps propagating. Test `Tenant_audit_page_survives_global_administrator_projection_fault_during_load`. (`TenantAuditPage.razor` `LoadAsync`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:499-513 guards supplementary global-administrator loading; TenantAuditPageTests.cs:801 covers the regression.

### DW-80: Corrective-proof timestamp uses `CurrentCulture` instead of `InvariantCulture` — RESOLVED 2026-06-30
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
location: GlobalAdministratorCorrectionSnapshot.cs; GlobalAdministratorCorrectionPanel.razor
reason: The legacy ledger defers this issue: Corrective-proof timestamp uses `CurrentCulture` instead of `InvariantCulture` — RESOLVED 2026-06-30. Original context is preserved in legacy-detail.
legacy-detail: - **~~Corrective-proof timestamp uses `CurrentCulture` instead of `InvariantCulture`~~ — RESOLVED 2026-06-30** — the proof *display* timestamp was fixed by the 2026-06-29 review (P9 — `ProofTimestampLabel` uses `InvariantCulture`); the live residual — the `originalTimestamp` *parse* in `WithCorrectiveProof` (and the panel's proof lookup) using ambient culture — was fixed in the 2026-06-30 code review by parsing with `CultureInfo.InvariantCulture` + `DateTimeStyles.RoundtripKind`. The same review also added a time tie-back so the corrective row must be at/after the original event time. (`GlobalAdministratorCorrectionSnapshot.cs` `WithCorrectiveProof`, `GlobalAdministratorCorrectionPanel.razor` `QueryCorrectiveProofAsync`). NB: the tenant-domain `CorrectionStartPanel` (story 5.6) was likewise fixed 2026-06-30 (CC deferred-work, Edit F): `ProofTimestampLabel` and `TenantCorrectionPreviewSnapshot.WithCorrectiveProof` now parse/format with `InvariantCulture`, and the panel has the proof time tie-back.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:438-478 uses invariant timestamp parsing and a strict newer-than proof filter.

### DW-81: EventCallback→Func drops the parent re-render after confirm refresh (intentional, benign)
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-29)"), 2026-08-25
location: CorrectionStartPanel.razor:202; TenantAuditPage.razor:550
reason: The legacy ledger defers this issue: EventCallback→Func drops the parent re-render after confirm refresh (intentional, benign). Original context is preserved in legacy-detail.
legacy-detail: - **EventCallback→Func drops the parent re-render after confirm refresh (intentional, benign)** — watch-item only: the new `ProjectionRefreshProvider` Func updates the parent field without re-rendering the parent; benign today because those fields feed only the panel. Restore a parent render (or document) if other parent UI later binds the refreshed snapshots. 5.8-introduced. (`CorrectionStartPanel.razor:202`, `TenantAuditPage.razor:550`)
status: done 2026-09-06
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1283 assigns refreshed parent state inside InvokeAsync, ensuring the parent render is scheduled.

### DW-82: `CorrectionStartPanel` terminal-state focus parity (story 5.6) — RESOLVED 2026-06-30 (CC deferred-work, Edit C)
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification — committed bundle re-review (2026-06-30)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
reason: The legacy ledger defers this issue: `CorrectionStartPanel` terminal-state focus parity (story 5.6) — RESOLVED 2026-06-30 (CC deferred-work, Edit C). Original context is preserved in legacy-detail.
legacy-detail: - **~~`CorrectionStartPanel` terminal-state focus parity (story 5.6)~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit C)** — `CorrectionStartPanel.SetSnapshot` now moves keyboard focus on all six terminal states (`Confirmed`/`Failed`/`Rejected`/`Degraded`/`UnableToVerify`/`AlreadyApplied`), mirroring `GlobalAdministratorCorrectionPanel.SetSnapshot`. Test `Panel_rejected_terminal_state_moves_focus_to_lifecycle`. (`src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:361-373 covers all terminal states; CorrectionStartPanelTests.cs:420 covers rejected-state focus.

### DW-83: Already-logged (2026-06-29), re-confirmed: RESOLVED 2026-07-01 (CC deferred-work):
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification — committed bundle re-review (2026-06-30)"), 2026-08-25
location: GlobalAdministratorCorrectionSnapshot.cs
reason: The legacy ledger defers this issue: Already-logged (2026-06-29), re-confirmed: RESOLVED 2026-07-01 (CC deferred-work):. Original context is preserved in legacy-detail.
legacy-detail: - **~~Already-logged (2026-06-29), re-confirmed:~~ RESOLVED 2026-07-01 (CC deferred-work):** global-administrator projection pagination ignored (>20 admins) — the **confirm-time false-`Confirmed`** path (revoke of a page-2 admin reads `!present` ⇒ "proven"), whose raised severity was flagged here, is now closed: `ConfirmProjection` requires `!present && !HasMore` to prove a revoke, and the preview gate fails closed on an incomplete page. Only the full projection-paging redesign remains routed. (`GlobalAdministratorCorrectionSnapshot.cs`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs:286-305,365-371 requires Ready/Current evidence and !HasMore before confirmation.

### DW-84: Already-logged (2026-06-29), re-confirmed:
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification — committed bundle re-review (2026-06-30)"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Already-logged (2026-06-29), re-confirmed:. Original context is preserved in legacy-detail.
legacy-detail: - **Already-logged (2026-06-29), re-confirmed:** no story-owned gateway-routing test. No new entry created.
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs:31-70 pins system/global-administrators/global-administrators, command types, and literal payloads for set/remove.

### DW-85: Ledger-hygiene (see 5.7 patch P-9) — CLOSED 2026-06-30:
origin: migrated from legacy ledger ("Deferred from: code review of 5-7-global-administrator-correction-verification — committed bundle re-review (2026-06-30)"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: Ledger-hygiene (see 5.7 patch P-9) — CLOSED 2026-06-30:. Original context is preserved in legacy-detail.
legacy-detail: - **~~Ledger-hygiene (see 5.7 patch P-9)~~ — CLOSED 2026-06-30:** the stale "ConfirmProjection confirms off a known-Stale projection" and "Corrective-proof timestamp uses CurrentCulture" entries were rewritten/closed after the follow-up patches landed. Global-admin stale projection and proof timestamp/parse paths are resolved; tenant-domain residuals are tracked separately below.
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs:285-305 rejects non-Current and inconclusive paged absence; GlobalAdministratorCorrectionPanel.razor:466-478 parses timestamps invariantly with RoundtripKind.

### DW-86: Concurrent correction opens can finish projection refresh out of order — RESOLVED 2026-06-30 (CC deferred-work, Edit D)
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-30)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
reason: The legacy ledger defers this issue: Concurrent correction opens can finish projection refresh out of order — RESOLVED 2026-06-30 (CC deferred-work, Edit D). Original context is preserved in legacy-detail.
legacy-detail: - **~~Concurrent correction opens can finish projection refresh out of order~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit D)** — `OpenCorrectionAsync` now captures a `_correctionOpenGeneration` synchronously at entry and applies the active intent only if still the latest, so an earlier open whose refresh resolves last no longer wins. (`src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` `OpenCorrectionAsync`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:893-925 uses _correctionOpenGeneration so only the newest concurrent open applies.

### DW-87: Tenant-domain correction can still confirm from stale/degraded tenant detail — RESOLVED 2026-06-30 (CC deferred-work, Edit E)
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-30)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
reason: The legacy ledger defers this issue: Tenant-domain correction can still confirm from stale/degraded tenant detail — RESOLVED 2026-06-30 (CC deferred-work, Edit E). Original context is preserved in legacy-detail.
legacy-detail: - **~~Tenant-domain correction can still confirm from stale/degraded tenant detail~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit E)** — `RefreshTenantProjectionAsync` (the tenant confirm-time provider) now returns the projection only when `Freshness is Current`, else `null`, so `ConfirmProjection(null)` fails closed instead of confirming off stale evidence (parity with the GA `Freshness=Current` gate). Test `Panel_does_not_confirm_when_projection_refresh_provider_returns_no_fresh_projection`. (`src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor` `RefreshTenantProjectionAsync`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1042-1062 returns confirmation evidence only for Current freshness; CorrectionStartPanelTests.cs:451 covers fail-closed behavior.

### DW-88: Tenant-domain corrective proof lookup can link unrelated historical rows — RESOLVED 2026-06-30 (CC deferred-work, Edit F)
origin: migrated from legacy ledger ("Deferred from: code review of 5-8-correction-projection-refresh-cleanup (2026-06-30)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
reason: The legacy ledger defers this issue: Tenant-domain corrective proof lookup can link unrelated historical rows — RESOLVED 2026-06-30 (CC deferred-work, Edit F). Original context is preserved in legacy-detail.
legacy-detail: - **~~Tenant-domain corrective proof lookup can link unrelated historical rows~~ — RESOLVED 2026-06-30 (CC deferred-work, Edit F)** — `QueryCorrectiveProofAsync` now parses `originalTimestamp` (`InvariantCulture`+`RoundtripKind`), lower-bounds the audit query with `From: originalTimestamp`, filters `row.Timestamp > originalTimestamp`, newest-first; missing/malformed timestamp ⇒ audit-delayed. Test `Panel_proof_lookup_ignores_audit_row_not_newer_than_the_original_event`. (`src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor` `QueryCorrectiveProofAsync`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor:531-575 uses invariant parsing and strictly newer proof rows; CorrectionStartPanelTests.cs:478 rejects historical rows.

### DW-89: Scheduled performance workflow lacks the EventStore opt-in — the shared `domain-ci.yml` performance job invokes the `Category=Performance` lane without `HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1`, while `DaprPerformanceFactAttribute` requires that variable
origin: migrated from legacy ledger ("Deferred from: run-all-tests-and-fix-failures review (2026-07-14)"), 2026-08-25
location: references/Hexalith.Builds/.github/workflows/domain-ci.yml; domain-ci.yml
reason: The legacy ledger defers this issue: Scheduled performance workflow lacks the EventStore opt-in — the shared `domain-ci.yml` performance job invokes the `Category=Performance` lane without `HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1`, while `DaprPerformanceFactAttribute` requires that variable. Original context is preserved in legacy-detail.
legacy-detail: - **Scheduled performance workflow lacks the EventStore opt-in** — the shared `domain-ci.yml` performance job invokes the `Category=Performance` lane without `HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS=1`, while `DaprPerformanceFactAttribute` requires that variable. Local verification explicitly enabled it and executed the 500,000-event benchmark, but the scheduled shared workflow can report a skip. Fix belongs in `Hexalith.Builds` and requires separate submodule approval; add the environment variable to the shared performance job and validate a scheduled-shaped run. (`references/Hexalith.Builds/.github/workflows/domain-ci.yml`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: references/Hexalith.Builds/.github/workflows/domain-ci.yml:569-577,587-595 enables performance tests for both VSTest and MTP lanes.

### DW-90: Zero test changes despite several identified Tenants-rendering gaps
origin: migrated from legacy ledger ("Deferred from: code review of 1-0-reverify-frontcomposer-shell-and-fluent-contracts (2026-07-19)"), 2026-08-25
location: story-1-0-frontcomposer-fluent-reverification-2026-07-19.md
reason: The legacy ledger defers this issue: Zero test changes despite several identified Tenants-rendering gaps. Original context is preserved in legacy-detail.
legacy-detail: - **Zero test changes despite several identified Tenants-rendering gaps** — Size16 vs required Size20 icons, missing `IconLabel`, unpinned freshness safety column, missing `MessageBarLayout.Notification`/`AriaLive` usage (`story-1-0-frontcomposer-fluent-reverification-2026-07-19.md` FC-TOK/FC-TBL rows). AC5's own wording is conditional ("add tests only when they guard a confirmed Tenants boundary"), so whether any of these gaps currently qualify is a judgment call for whichever story next touches badge/grid rendering, not a clear miss by this verification-only story.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs:94-108 and src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor:63-75 pin Size20 icons, IconLabel, and the freshness column.

### DW-91: No tracking ticket/issue for the FrontComposer-owned gaps
origin: migrated from legacy ledger ("Deferred from: code review of 1-0-reverify-frontcomposer-shell-and-fluent-contracts (2026-07-19)"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: No tracking ticket/issue for the FrontComposer-owned gaps. Original context is preserved in legacy-detail.
legacy-detail: - **No tracking ticket/issue for the FrontComposer-owned gaps** this story identifies (FC-CMD, FC-CNC, FC-TBL, FC-TOK) — "assign to FrontComposer" has no actual assignment mechanism in this repo's process. Matches the existing routing convention in this file's "Cross-Submodule Owner Handoffs" section, which this story's four gaps should eventually feed as new entries once an owning FrontComposer task is opened.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: _bmad-output/implementation-artifacts/deferred-work.md:642-647 is now the repository tracking mechanism for the FrontComposer handoff.

### DW-92: `sprint-status.yaml`'s flat per-story status can't represent "review with 2 of 5 sub-contracts blocked"
origin: migrated from legacy ledger ("Deferred from: code review of 1-0-reverify-frontcomposer-shell-and-fluent-contracts (2026-07-19)"), 2026-08-25
location: _bmad-output/implementation-artifacts/sprint-status.yaml:53; sprint-status.yaml
reason: The legacy ledger defers this issue: `sprint-status.yaml`'s flat per-story status can't represent "review with 2 of 5 sub-contracts blocked". Original context is preserved in legacy-detail.
legacy-detail: - **`sprint-status.yaml`'s flat per-story status can't represent "review with 2 of 5 sub-contracts blocked"** (`_bmad-output/implementation-artifacts/sprint-status.yaml:53`) — schema limitation of a shared tracking file used across the whole project; not something this story's diff introduced or can fix alone.
status: done 2026-08-26
archived: 2026-10-12
resolution: closed by human decision: Keep sub-contract detail in story evidence and deferred work.
decision: 2026-08-26 Retain flat status — Keep sub-contract detail in story evidence and deferred work.

### DW-93: Epic 1 is marked done while most child stories remain backlog or review
origin: migrated from legacy ledger ("Deferred from: code review of 1-0-reverify-frontcomposer-shell-and-fluent-contracts (2026-07-19)"), 2026-08-25
location: _bmad-output/implementation-artifacts/sprint-status.yaml:52-64
reason: The legacy ledger defers this issue: Epic 1 is marked done while most child stories remain backlog or review. Original context is preserved in legacy-detail.
legacy-detail: - **`epic-1: done` while most of Epic 1's 12 stories remain `backlog`/`review`** (`_bmad-output/implementation-artifacts/sprint-status.yaml:52-64`) — only 3 of 12 stories under Epic 1 (1-3, 1-5, 1-7) are `done`; the rest (1-0, 1-1, 1-2, 1-4, 1-6, 1-8 through 1-11) are `backlog`/`review`, yet `epic-1` and `epic-1-retrospective` are both marked `done`, violating the file's own documented rule ("done: All stories in epic completed"). Pre-existing — the `epic-1`/`epic-1-retrospective` lines are untouched by this story's diff (only the `1-0-...` status line changed). Likely stale from the epics.md renumbering during the 2026-07-19 sprint-change-proposal rollout (see memory `prd-edit-2026-07-17-scp-0715-prd-slice`); route to a sprint-planning resync, not a fix within this story.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: _bmad-output/implementation-artifacts/sprint-status.yaml:52-67 marks Epic 1 and every child story done, removing the aggregate-status conflict.

### DW-94: Reusable release caller omits required publication-authority inputs
origin: migrated from legacy ledger ("Deferred from: code review of 1-1-reverify-ui-host-bootstrap-and-canonical-workspace (2026-07-19)"), 2026-08-25
location: github/workflows/release.yml; github/workflows/release.yml:29
reason: The legacy ledger defers this issue: Reusable release caller omits required publication-authority inputs. Original context is preserved in legacy-detail.
legacy-detail: - **Reusable release caller omits required publication-authority inputs** — `.github/workflows/release.yml` already enabled container publication without `builds-execution-sha`, `release-authority-url`, or `release-owner-allowlist`; the shared `domain-release.yml` rejects their empty defaults before publication. This predates Story 1.1's UI mapping and requires a separately authorized release-governance fix. (`.github/workflows/release.yml:29`; `references/Hexalith.Builds/.github/workflows/domain-release.yml:95`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 6cc9eb3a; .github/workflows/release.yml:268-296 pins the Builds caller identity and supplies required execution, source, and publication inputs.

### DW-95: Submodule pointer upgrades require their own review
origin: migrated from legacy ledger ("Deferred from: code review of 1-1-reverify-ui-host-bootstrap-and-canonical-workspace (2026-07-19)"), 2026-08-25
location: references/Hexalith.Builds; references/Hexalith.FrontComposer
reason: The legacy ledger defers this issue: Submodule pointer upgrades require their own review. Original context is preserved in legacy-detail.
legacy-detail: - **Submodule pointer upgrades require their own review** — the Builds and FrontComposer pointer changes were present before Story 1.1 implementation and alter shared build/UI inputs. Review and land those dependency changes independently rather than absorbing them into this story's patch set. (`references/Hexalith.Builds`; `references/Hexalith.FrontComposer`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commits daf6c76c and 10db1cee subsequently moved Builds, EventStore, and FrontComposer pointers in dedicated build(deps) commits.

### DW-96: Epic 1 aggregate status conflicts with child stories
origin: migrated from legacy ledger ("Deferred from: code review of 1-1-reverify-ui-host-bootstrap-and-canonical-workspace (2026-07-19)"), 2026-08-25
location: _bmad-output/implementation-artifacts/sprint-status.yaml:52
reason: The legacy ledger defers this issue: Epic 1 aggregate status conflicts with child stories. Original context is preserved in legacy-detail.
legacy-detail: - **Epic 1 aggregate status conflicts with child stories** — `epic-1` remains `done` while Story 1.1 is in review and multiple children are backlog. The aggregate line predates this story and should be reconciled by sprint planning. (`_bmad-output/implementation-artifacts/sprint-status.yaml:52`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: _bmad-output/implementation-artifacts/sprint-status.yaml:52-67 marks Epic 1 and every child story done, removing the aggregate-status conflict.

### DW-97: `EXPECTED_DEPENDENCIES` is hand-duplicated between `scripts/validate-nuget-packages.py` and its test mirror in `CiQualityGateScriptTests.cs`
origin: migrated from legacy ledger ("Deferred from: code review of run-all-tests-and-fix-failures-2 (2026-07-20)"), 2026-08-25
location: scripts/validate-nuget-packages.py; tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs
reason: The legacy ledger defers this issue: `EXPECTED_DEPENDENCIES` is hand-duplicated between `scripts/validate-nuget-packages.py` and its test mirror in `CiQualityGateScriptTests.cs`. Original context is preserved in legacy-detail.
legacy-detail: - **`EXPECTED_DEPENDENCIES` is hand-duplicated between `scripts/validate-nuget-packages.py` and its test mirror in `CiQualityGateScriptTests.cs`**, with no single source of truth — the test file's own comment already acknowledges this ("Mirrors EXPECTED_DEPENDENCIES ... so synthetic fixtures satisfy the dependency-boundary validation"). Every future dependency-boundary change (like this session's) requires editing both files in lockstep by hand; a missed edit in one file would silently pass its own regression tests since both copies are asserted against each other, not against real restore output. Consider extracting a shared data file/fixture, or having the test import the script's dict directly, plus adding a negative-path test that asserts the boundary check actually fails when a real project gains an unexpected dependency. Pre-existing design, not introduced by this session's fix. (`scripts/validate-nuget-packages.py`, `tests/Hexalith.Tenants.Contracts.Tests/CiQualityGateScriptTests.cs`)
status: done 2026-09-02
archived: 2026-10-12
resolution: resolved by sweep bundle dw-package-boundary-source
resolution-undo: 3446eef21b1b160c45f226f960c64918cc4cf6db292ace5f1d0586f9c9e868d0 2026-09-02 7374617475733a206f70656e

### DW-98: Dependency-boundary validation is a hardcoded per-package allowlist rather than derived from actual restore/lock output
origin: migrated from legacy ledger ("Deferred from: code review of run-all-tests-and-fix-failures-2 (2026-07-20)"), 2026-08-25
location: scripts/validate-nuget-packages.py
reason: The legacy ledger defers this issue: Dependency-boundary validation is a hardcoded per-package allowlist rather than derived from actual restore/lock output. Original context is preserved in legacy-detail.
legacy-detail: - **Dependency-boundary validation is a hardcoded per-package allowlist rather than derived from actual restore/lock output** (e.g. `dotnet list package --include-transitive`) — inherently high-maintenance; this is the second time in this file's history a submodule/package version bump has required a manual allowlist update (see the CI Restore NU1107 memory for the sibling pattern). A more dynamic validation approach would eliminate this class of recurring CI break. Architectural, out of scope for a narrowly-scoped test-fix session. (`scripts/validate-nuget-packages.py`)
status: done 2026-09-02
archived: 2026-10-12
resolution: resolved by sweep bundle dw-package-boundary-source
resolution-undo: 3446eef21b1b160c45f226f960c64918cc4cf6db292ace5f1d0586f9c9e868d0 2026-09-02 7374617475733a206f70656e

### DW-99: `NextPageAsync` unguarded `NextCursor==null`
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:484-487
reason: The legacy ledger defers this issue: `NextPageAsync` unguarded `NextCursor==null`. Original context is preserved in legacy-detail.
legacy-detail: - **`NextPageAsync` unguarded `NextCursor==null`** — the Next button is gated only on `!_snapshot.HasMore`; a backend contract violation (`HasMore==true` with a null `NextCursor`) would push the current cursor to history and set the cursor to null, bouncing the user to page 1 with a growing back-stack. Defensive only — the platform opaque-cursor contract guarantees a next cursor whenever `HasMore` is true. Follow-up: disable Next on `!HasMore || NextCursor is null`, or guard before consuming. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:484-487`)
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:452-458,1050-1058 requires a non-null cursor in both affordance enablement and handler execution.

### DW-100: Grid cannot return to the default `TenantId` ordering except via toolbar Reset
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor
reason: The legacy ledger defers this issue: Grid cannot return to the default `TenantId` ordering except via toolbar Reset. Original context is preserved in legacy-detail.
legacy-detail: - **Grid cannot return to the default `TenantId` ordering except via toolbar Reset** — only `tenant-id`→Name and `tenant-status`→Status are sortable; the `_ => TenantListSortColumns.TenantId` arm of `OnTenantSortChanged` is a defensive fallback (null/unknown `ColumnId`), and FluentDataGrid's 3-state "unsorted" third click cannot be represented (it re-forces Name/Status). While `SortColumn==TenantId` no visible column shows a sort indicator. UX limitation with a workaround (Reset). Follow-up: if return-to-default is desired, add an explicit affordance or map the unsorted event. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor` `OnTenantSortChanged`)
status: done 2026-08-25
archived: 2026-10-12
resolution: closed by human decision: Document toolbar Reset as the supported route back to default ordering.
decision: 2026-08-25 Accept reset-only — Document toolbar Reset as the supported route back to default ordering.

### DW-101: Brittle source-text "guard" tests + stale resource stub
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
location: tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs; tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs
reason: The legacy ledger defers this issue: Brittle source-text "guard" tests + stale resource stub. Original context is preserved in legacy-detail.
legacy-detail: - **Brittle source-text "guard" tests + stale resource stub** — several tests grep rendered/source text rather than assert behavior: `grid.ShouldNotContain("Cursor", Case.Insensitive)` (a common CSS/identifier word), `navigation.Split("Cursor = null").Length.ShouldBe(3)` (exact occurrence count), and `workspace.ShouldNotContain("ConfigureAwait(false)")` (source scan, not dispatcher-affinity proof) — they break on unrelated edits and can pass even if behavior regresses via a differently-named channel. Separately, the `TenantsWorkspaceTests` resource stub still defines the old `Tenants.List.ReturnContext` copy (containing "cursor") and removed `Tenants.List.Sort.*` keys, diverging from the corrected production resources. Test tech-debt. (`tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs`, `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`)
status: done 2026-09-05
archived: 2026-10-12
resolution: already resolved: commit d2b7ede359830c27934ac9f577e3073955c3e2c2 replaced source-text guards with behavioral tests; tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs:802-827 now reads the active production resource bundle and fails closed for unknown keys.

### DW-102: Duplicated tab/scope literal constants across two files
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:219-222; src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs
reason: The legacy ledger defers this issue: Duplicated tab/scope literal constants across two files. Original context is preserved in legacy-detail.
legacy-detail: - **Duplicated tab/scope literal constants across two files** — `TenantsWorkspace.razor` declares `TenantsTabId`/`UsersTabId`/`AllTenantsScope`/`MyTenantsScope` and `TenantWorkspaceState.cs` declares `TenantsTab`/`UsersTab`/`AllScope`/`MyScope` with the same `"tenants"/"users"/"all"/"mine"` values; `ApplyWorkspaceState` compares `state.Tab` (sourced from the state file's consts) against the razor file's consts. Value-equal today, but nothing enforces it — changing one string silently breaks tab/scope routing with no compile error. DRY nit. (`src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:219-222`, `src/Hexalith.Tenants.UI/State/TenantList/TenantWorkspaceState.cs`)
status: done 2026-09-06
archived: 2026-10-12
resolution: already resolved: Commit 6e52036e centralizes workspace tab and scope identifiers in TenantWorkspaceState.

### DW-106: Audit-grid unsafe references are omitted from copying but remain visible in the rendered reference label
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: AuditDataGrid.razor
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
reason: The legacy ledger defers this issue: Audit-grid unsafe references are omitted from copying but remain visible in the rendered reference label. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md` summary: Audit-grid unsafe references are omitted from copying but remain visible in the rendered reference label. evidence: `AuditDataGrid.razor` sanitizes `EventReference` only for `SupportSafeCopyButton`; `ReferenceLabel(context)` still renders the raw reference and context, and the behavior predates this story's compatibility migration.
status: done 2026-08-27
archived: 2026-10-12
resolution: resolved by sweep bundle dw-support-safe-copy-followup
resolution-undo: e32f9ec5cfa74f6e713b0d8ce939f3393f91d53d4b83c40ea3313864db2fc699 2026-08-27 7374617475733a206f70656e

### DW-107: Clipboard module import and write operations are not coordinated with component disposal
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: SupportSafeCopyButton.razor
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
reason: The legacy ledger defers this issue: Clipboard module import and write operations are not coordinated with component disposal. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md` summary: Clipboard module import and write operations are not coordinated with component disposal. evidence: `SupportSafeCopyButton.razor` inherited a disposal path that returns while `_module` is null and does not invalidate an import or write already in flight, allowing late interop or disposal races during navigation.
status: done 2026-08-27
archived: 2026-10-12
resolution: resolved by sweep bundle dw-support-safe-copy-followup
resolution-undo: e32f9ec5cfa74f6e713b0d8ce939f3393f91d53d4b83c40ea3313864db2fc699 2026-08-27 7374617475733a206f70656e

### DW-109: The audit receipt copies a hidden synthesized English composite rather than one exact visible localized literal
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: AuditEvidenceReceipt.razor
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
reason: The legacy ledger defers this issue: The audit receipt copies a hidden synthesized English composite rather than one exact visible localized literal. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md` summary: The audit receipt copies a hidden synthesized English composite rather than one exact visible localized literal. evidence: `TenantAuditReceipt.CopyableReferenceText` assembles hard-coded English labels and `AuditEvidenceReceipt.razor` approves that non-rendered multiline value, a pre-existing audit behavior exposed by the shared-component migration.
status: done 2026-08-27
archived: 2026-10-12
resolution: resolved by sweep bundle dw-support-safe-copy-followup
resolution-undo: e32f9ec5cfa74f6e713b0d8ce939f3393f91d53d4b83c40ea3313864db2fc699 2026-08-27 7374617475733a206f70656e

### DW-110: Legacy configuration display safety remains a deny-list that can miss unrecognized secret formats
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: LegacyConfigurationDisplaySanitizer
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
reason: The legacy ledger defers this issue: Legacy configuration display safety remains a deny-list that can miss unrecognized secret formats. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md` summary: Legacy configuration display safety remains a deny-list that can miss unrecognized secret formats. evidence: `LegacyConfigurationDisplaySanitizer` preserves the pre-existing command-preview display policy by accepting every non-empty key/value pair that lacks listed fragments, so values such as unknown API-key formats may still render until Story 1.6 supplies a positive safe model.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 5a401654; src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationSafeComposer.cs:36-48,167-186 replaced the deny-list sanitizer with explicitly display-safe keys and rows.

### DW-111: Configuration keys that fail the legacy display-safety policy remain visible even while their paired values are redacted
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: TenantConfigurationView.razor
source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md`
reason: The legacy ledger defers this issue: Configuration keys that fail the legacy display-safety policy remain visible even while their paired values are redacted. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-8-support-safe-identifier-copy-and-read-experience-evidence.md` summary: Configuration keys that fail the legacy display-safety policy remain visible even while their paired values are redacted. evidence: `TenantConfigurationView.razor` always renders `context.Key`; `LegacyConfigurationDisplaySanitizer.IsDisplayable(key, value)` only controls value replacement, so a key containing a known sensitive literal remains exposed in the DOM and accessibility label. This display behavior predates the copy-policy change.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationSafeComposer.cs:167-186 and TenantConfigurationView.razor:116-147 exclude unsafe keys from the safe model.

### DW-115: The tenant-detail read path does not adopt the shared null-member guard, so a malformed member element crashes the detail page while both list surfaces degrade safely
origin: migrated from legacy ledger ("Deferred from: code review of 1-4-my-tenants-self-audit (2026-07-21)"), 2026-08-25
location: TenantQueryGateway.HasUsableMembers
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The tenant-detail read path does not adopt the shared null-member guard, so a malformed member element crashes the detail page while both list surfaces degrade safely. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The tenant-detail read path does not adopt the shared null-member guard, so a malformed member element crashes the detail page while both list surfaces degrade safely. evidence: `TenantQueryGateway.HasUsableMembers` is applied to search hydration and ordinary-list enrichment but not to `GetTenantAsync`, which feeds the identical `TenantDetail` payload to `TenantDetailPage.OwnerCount` and `MemberAccessReview.OwnerCount`; both dereference member elements during render, so a `Members` array containing a null element throws `NullReferenceException` and tears down the circuit. `TenantConfigurationSafeComposer.SanitizeDetail` copies the collection and preserves the null element. The detail-page dereference predates Story 1.9; this story only made the asymmetry visible by guarding the two list paths.
status: done 2026-09-05
archived: 2026-10-12
resolution: already resolved: commit 0891897533227cd63b0f6a84bd9694a17725e65b; src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs:302-310,456-460 rejects a detail containing a null member as InvalidPayload before Razor, covered at TenantsRestQueryClientTests.cs:666-691.

### DW-120: The CI package-boundary gate asserts the fixture it generates from its own allowlist
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27)"), 2026-08-25
location: scripts/validate-nuget-packages.py:64; CiQualityGateScriptTests.cs:309
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The CI package-boundary gate asserts the fixture it generates from its own allowlist. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The CI package-boundary gate asserts the fixture it generates from its own allowlist. evidence: `CiQualityGateScriptTests.cs:309` mirrors `scripts/validate-nuget-packages.py:64`; `ExpectedDependencies` is used to synthesise the `.nupkg` fixtures fed to the script, so the test verifies only that two copies of the same literal agree, never that `Microsoft.Extensions.Http.Resilience` is genuinely upstream-owned. Widening the allowlist to silence a real leak would pass. status: open
status: done 2026-09-02
archived: 2026-10-12
resolution: resolved by sweep bundle dw-package-boundary-source
resolution-undo: 3446eef21b1b160c45f226f960c64918cc4cf6db292ace5f1d0586f9c9e868d0 2026-09-02 7374617475733a206f70656e

### DW-123: A partially hidden search window still advertises, through a live Next control beside its surviving rows, that the window held more than it rendered
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27, pass 3)"), 2026-08-25
location: TenantQueryGateway.cs:864
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: A partially hidden search window still advertises, through a live Next control beside its surviving rows, that the window held more than it rendered. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: A partially hidden search window still advertises, through a live Next control beside its surviving rows, that the window held more than it rendered. evidence: `TenantQueryGatewayTests` pins as intended behaviour a window where five of six candidates were dropped (forbidden, not-found, null detail, id mismatch, degraded) yielding one row plus `HasMore = true` and a minted cursor at offset 6. The window-collapse rule at `TenantQueryGateway.cs:864` closes the fully hidden case only. Closing the partial case means not exposing per-page authorized counts through pager state at all. Reviewed with the story owner on 2026-07-27 and accepted as out of scope for this story; tracked in the evidence report as PARTIAL-WINDOW-DISCLOSURE-1.9. status: open — accepted out of scope; reopen trigger is any requirement that a partially hidden window be indistinguishable from a complete one.
status: done 2026-08-25
archived: 2026-10-12
resolution: closed by human decision: Retain the approved Story 1.9 residual and reopen only for a stronger privacy requirement.
decision: 2026-08-25 Accept owner decision — Retain the approved Story 1.9 residual and reopen only for a stronger privacy requirement.

### DW-124: The pass-2 finding "Seven new Lifecycle bindings unverified end-to-end on any real surface" was checked off after closing 1 of 13 binding sites
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-9-authoritative-memories-search-with-protected-paging (2026-07-27, pass 3)"), 2026-08-25
location: TenantDataGrid.razor:76; MyTenantsDataGrid.razor:75
source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md`
reason: The legacy ledger defers this issue: The pass-2 finding "Seven new Lifecycle bindings unverified end-to-end on any real surface" was checked off after closing 1 of 13 binding sites. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-9-authoritative-memories-search-with-protected-paging.md` summary: The pass-2 finding "Seven new Lifecycle bindings unverified end-to-end on any real surface" was checked off after closing 1 of 13 binding sites. evidence: `grep 'Lifecycle="'` finds 13 sites — `TenantDataGrid.razor:76`, `MyTenantsDataGrid.razor:75`, `AuditDataGrid.razor:54`, `GlobalAdministratorsPage.razor:347`, `TenantDetailPage.razor:115/144/165/186`, `TenantConfigurationView.razor:15/129`, `MemberAccessReview.razor:19/116`, `TenantLifecycleActionAvailability.razor:25`. Only `TenantDataGrid` gained a rendered-lifecycle assertion (`TenantListSurfaceTests.cs:1002-1005`), and it was mutation-verified. `truth-state-badge--*` appears nowhere else outside `TruthStateBadgeTests`, which the original finding already deemed insufficient. The 12 remaining sites are other stories' surfaces (tenant detail, configuration, member review, audit, global administrators), so covering them is not story-1.9 work. status: open — the pass-2 checkbox at `spec-1-9-…-paging.md:243` should be corrected to record partial closure.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs:55-62, GlobalAdministratorsPageTests.cs:415-422, TenantDetailSurfaceTests.cs:3803-3929, MyTenantsSurfaceTests.cs:61-65, and TenantLifecycleActionAvailabilityTests.cs:122-126 cover the formerly missing rendered lifecycle surfaces.

### DW-125: A third divergent global-administrator claim parser now coexists with the existing two, so the same signed-in user can be a proven administrator for configuration and Indeterminate for tenant lifecycle
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration.md (2026-07-27)"), 2026-08-25
location: _bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md; TenantConfigurationPrincipalResolver.cs:102-194
source_spec: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md`
reason: The legacy ledger defers this issue: A third divergent global-administrator claim parser now coexists with the existing two, so the same signed-in user can be a proven administrator for configuration and Indeterminate for tenant lifecycle. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md` summary: A third divergent global-administrator claim parser now coexists with the existing two, so the same signed-in user can be a proven administrator for configuration and Indeterminate for tenant lifecycle. evidence: `TenantConfigurationPrincipalResolver.cs:102-194` vs `Services/Gateways/TenantsGlobalAdministratorClaims.cs`. Four divergences verified at `ec7ec8c` and still present at HEAD: malformed JSON role array yields Indeterminate in the new resolver but falls through to delimiter parsing in the old one; an unparseable `global_admin` yields Indeterminate vs `false`; `{`-prefixed role values yield Indeterminate vs split-parsed; claims are read across all identities in the old parser but only from the single authenticated identity in the new one. Consolidating into one three-state resolver that the boolean parser collapses would touch lifecycle and global-administrator surfaces owned by other stories. status: open — needs a cross-story owner; reopen trigger is any new surface that needs administrator evidence, or a reported disagreement between configuration and lifecycle authorization for the same user.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationPrincipalResolver.cs:90-95 delegates to the corroborated TenantsGlobalAdministratorClaims parser.

### DW-129: The release tag floor guard probes NuGet only; the container tag in registry.hexalith.com is never checked
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: The release tag floor guard probes NuGet only; the container tag in registry.hexalith.com is never checked. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: The release tag floor guard probes NuGet only; the container tag in registry.hexalith.com is never checked. evidence: publication_preflight.py fails with the same version-collision on the container repository (validate_container_absence), so a partial prior release that left a container tag can still fail the protected job after approval. The unprotected verify-source job has no registry credentials, so covering it needs a design decision.
status: done 2026-08-26
archived: 2026-10-12
resolution: already resolved: references/Hexalith.Builds/.github/workflows/domain-release.yml:496 and scripts/publication_preflight.py:930-956,1155-1185 perform protected exact container-tag collision checks; implemented by Builds commits f271c8aa and 2a8b63d2.
decision: 2026-08-25 Protected prewrite check — Use production read credentials immediately after approval but before any package or container write.

### DW-130: The tag floor is proved in verify-source but never re-proved after the production approval gate
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: The tag floor is proved in verify-source but never re-proved after the production approval gate. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: The tag floor is proved in verify-source but never re-proved after the production approval gate. evidence: environment-name production can hold for hours; a tag deleted or added during that window reproduces the original incident with a green guard behind it. Re-asserting inside the release job, or pinning the resolved floor as a job output, would close it.
status: done 2026-08-26
archived: 2026-10-12
resolution: already resolved: references/Hexalith.Builds/.github/workflows/domain-release.yml:811-842,889-907 re-proves live source and resolves a fresh release candidate after approval; implemented by Builds commits bd94f7fe, f271c8aa, and bf9af9cb.

### DW-131: The guard fails on any published version above the floor, even when the version semantic-release would actually propose is free, with no override
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md`
reason: The legacy ledger defers this issue: The guard fails on any published version above the floor, even when the version semantic-release would actually propose is free, with no override. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-release-version-floor-drift.md` summary: The guard fails on any published version above the floor, even when the version semantic-release would actually propose is free, with no override. evidence: floor v3.2.18 with a published 3.3.0-3.15.1 band and a breaking change in range proposes 4.0.0, which is free, yet the guard exits 1. There is no workflow_dispatch acknowledgement input, so the only escape is mutating tags.
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: Hexalith.Builds commit bd94f7fe; references/Hexalith.Builds/.github/workflows/domain-release.yml:889-907 dry-runs semantic-release and collision-checks the exact proposed version.
decision: 2026-08-26 Check exact proposal — Calculate semantic-release's exact proposal before approval and collision-check that version.
decision: 2026-08-25 Check proposed version — Calculate semantic-release's exact proposal before approval and collision-check that version.

### DW-132: The release-published tenants container image fails to start under Production defaults, failing the container smoke test and aborting every release after packages are already pushed
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-28)"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: The release-published tenants container image fails to start under Production defaults, failing the container smoke test and aborting every release after packages are already pushed. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: none summary: The release-published tenants container image fails to start under Production defaults, failing the container smoke test and aborting every release after packages are already pushed. evidence: Run 30340676669 evidence artifact, smoke-linux-amd64.log - OptionsValidationException requires Authentication:JwtBearer:Authority to be an absolute HTTPS URI (published appsettings.json has "") and requires SigningKey to be empty (it is not, in the container). amd64 exited 139, arm64 hit liveness-timeout. Only host-affecting change since the last successful release b3d01c53 is a7ca142, which moved Hexalith.EventStore.Gateway to a PackageReference on the non-source path, changing which appsettings.json wins in the container publish. Blocks release completion, not just this one.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 5efbbe75; .github/workflows/release.yml:281-287 pins the Builds smoke fix, whose smoke_container_platforms.py:44,213-223 uses Development hosting and explicit safe smoke authentication.

### DW-134: Deferred to Story 1.11
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-28)"), 2026-08-25
location: TenantConfigurationPrincipalResolver.cs:17-48
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** — Principal-resolution precedence was inverted in `TenantConfigurationPrincipalResolver.cs:17-48`: the circuit `AuthenticationStateProvider` now outranks `HttpContext.User`, where previously `HttpContext` was primary. A circuit whose provider returns an anonymous or not-yet-populated state while `HttpContext.User` is authenticated collapses to `Indeterminate` and fails every configuration grant closed. Security-relevant; must be decided against Story 1.11's acceptance criteria, not 1.10's. **CLOSED (2026-08-08, Story 1.11):** owner retained circuit-over-HTTP precedence with no request-principal fallback; see spec Scope Attribution decision 1.
status: done 2026-08-08
archived: 2026-10-12
resolution: Legacy completion record: - **Deferred to Story 1.11** — Principal-resolution precedence was inverted in `TenantConfigurationPrincipalResolver.cs:17-48`: the circuit `AuthenticationStateProvider` now outranks `HttpContext.User`, where previously `HttpContext` was primary. A circuit whose provider returns an anonymous or not-yet-populated state while `HttpContext.User` is authenticated collapses to `Indeterminate` and fails every configuration grant closed. Security-relevant; must be decided against Story 1.11's acceptance criteria, not 1.10's. **CLOSED (2026-08-08, Story 1.11):** owner retained circuit-over-HTTP precedence with no request-principal fallback; see spec Scope Attribution decision 1.

### DW-135: Deferred to Story 1.11
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-28)"), 2026-08-25
location: docs/production-auth-claim-contract.md; TenantsGlobalAdministratorClaims.cs:36-46
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** — `TenantsGlobalAdministratorClaims.Evaluate` now requires exactly one authenticated identity carrying exactly one literal `sub` claim (`TenantsGlobalAdministratorClaims.cs:36-46`). Any handler mapping `sub` to `ClaimTypes.NameIdentifier` (the ASP.NET default), or any principal with two authenticated identities (cookie + bearer), denies a genuine global administrator. Confirm the intended claim contract against `docs/production-auth-claim-contract.md` as part of 1.11. **CLOSED (2026-08-08, Story 1.11):** owner requires exactly one *distinct* literal `sub` value; identical duplicates accepted. See spec loop-2 decision + Scope Attribution decision 2.
status: done 2026-08-08
archived: 2026-10-12
resolution: Legacy completion record: - **Deferred to Story 1.11** — `TenantsGlobalAdministratorClaims.Evaluate` now requires exactly one authenticated identity carrying exactly one literal `sub` claim (`TenantsGlobalAdministratorClaims.cs:36-46`). Any handler mapping `sub` to `ClaimTypes.NameIdentifier` (the ASP.NET default), or any principal with two authenticated identities (cookie + bearer), denies a genuine global administrator. Confirm the intended claim contract against `docs/production-auth-claim-contract.md` as part of 1.11. **CLOSED (2026-08-08, Story 1.11):** owner requires exactly one *distinct* literal `sub` value; identical duplicates accepted. See spec loop-2 decision + Scope Attribution decision 2.

### DW-136: `EventStore:BaseAddress` already accepted Aspire compound schemes before Story 1.10, but neither the EventStore gateway client nor the tenant command client attaches `.AddServiceDiscovery()`. A compound address can therefore be marked connected and fail when sent. This is real command/status transport debt, but it predates the active direct-read change and remains outside the chunk-1 patch set
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:79
reason: The legacy ledger defers this issue: `EventStore:BaseAddress` already accepted Aspire compound schemes before Story 1.10, but neither the EventStore gateway client nor the tenant command client attaches `.AddServiceDiscovery()`. Original context is preserved in legacy-detail.
legacy-detail: - `EventStore:BaseAddress` already accepted Aspire compound schemes before Story 1.10, but neither the EventStore gateway client nor the tenant command client attaches `.AddServiceDiscovery()`. A compound address can therefore be marked connected and fail when sent. This is real command/status transport debt, but it predates the active direct-read change and remains outside the chunk-1 patch set. **Canonical open entry** for this debt (later 2026-07-30 chunk A+B item is a duplicate reaffirmation). [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:79]
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:162-167 rejects compound service-discovery schemes and accepts only exact http/https.

### DW-138: Deferred to Story 1.11
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29, review loop 4)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1178
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** — `ApplyAuthenticationStateChangedAsync` authorizes the page with the uncorroborated `TenantsGlobalAdministratorClaims.Evaluate` (`requireCorroboration: false`, so `sub` is never checked against `IUserContextAccessor.UserId`), then calls `LoadAsync(reuseETag: false, reauthorize: false)` to deliberately skip the corroborated path every other caller uses. A token refresh raising `AuthenticationStateChanged` with a principal whose `sub` does not match the server-side user context makes the grant/remove mutation surface reachable for the rest of the circuit. Security-relevant. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1178] **CLOSED (2026-08-08, Story 1.11):** authentication transitions use the strict BFF/circuit resolver; uncorroborated Evaluate path removed. See applied GA/workspace patches.
status: done 2026-08-08
archived: 2026-10-12
resolution: Legacy completion record: - **Deferred to Story 1.11** — `ApplyAuthenticationStateChangedAsync` authorizes the page with the uncorroborated `TenantsGlobalAdministratorClaims.Evaluate` (`requireCorroboration: false`, so `sub` is never checked against `IUserContextAccessor.UserId`), then calls `LoadAsync(reuseETag: false, reauthorize: false)` to deliberately skip the corroborated path every other caller uses. A token refresh raising `AuthenticationStateChanged` with a principal whose `sub` does not match the server-side user context makes the grant/remove mutation surface reachable for the rest of the circuit. Security-relevant. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1178] **CLOSED (2026-08-08, Story 1.11):** authentication transitions use the strict BFF/circuit resolver; uncorroborated Evaluate path removed. See applied GA/workspace patches.

### DW-139: Deferred to Story 1.11
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29, review loop 4)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:116
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** — `ResolveSystemScopeEvidence` returns `null` (→ `Indeterminate`) when the principal carries more than one distinct `eventstore:tenant` claim value, replacing a previous any-match `HasClaim(… == "system")`. A platform administrator whose token carries both `system` and a tenant scope now loses the Global Administrators page, the workspace entry link, and — because `GlobalAdministratorPolicy` was switched to `Evaluate(...) == Authorized` — every policy-gated FrontComposer surface. Extends the already-recorded single-identity/single-`sub` concern from the 2026-07-28 entry to the scope claim. [src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:116] **CLOSED (2026-08-08, Story 1.11):** retained as intentional fail-closed contract for conflicting system-scope evidence; no longer awaiting a 1.11 decision.
status: done 2026-08-08
archived: 2026-10-12
resolution: Legacy completion record: - **Deferred to Story 1.11** — `ResolveSystemScopeEvidence` returns `null` (→ `Indeterminate`) when the principal carries more than one distinct `eventstore:tenant` claim value, replacing a previous any-match `HasClaim(… == "system")`. A platform administrator whose token carries both `system` and a tenant scope now loses the Global Administrators page, the workspace entry link, and — because `GlobalAdministratorPolicy` was switched to `Evaluate(...) == Authorized` — every policy-gated FrontComposer surface. Extends the already-recorded single-identity/single-`sub` concern from the 2026-07-28 entry to the scope claim. [src/Hexalith.Tenants.UI/Services/Gateways/TenantsGlobalAdministratorClaims.cs:116] **CLOSED (2026-08-08, Story 1.11):** retained as intentional fail-closed contract for conflicting system-scope evidence; no longer awaiting a 1.11 decision.

### DW-140: Deferred to Story 1.11
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29, review loop 4)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1235
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** — A single transient authorization-resolution fault is indistinguishable from a permanent denial: `ResolveAuthorizationReflectionAsync` swallows every exception to `Indeterminate`, `CollapseAuthorizationAsync` then pins the restricted surface, and that surface offers no Refresh, Retry or Reset while `EnsureReadRefreshLeaseAsync` and `CanRecover` are both gated on `IsAuthorized`. Nothing re-enters resolution unless an `AuthenticationStateChanged` happens to fire. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1235] **CLOSED (2026-08-08, Story 1.11):** Indeterminate Retry / RetryAuthorization path applied; see GA page patches.
status: done 2026-08-08
archived: 2026-10-12
resolution: Legacy completion record: - **Deferred to Story 1.11** — A single transient authorization-resolution fault is indistinguishable from a permanent denial: `ResolveAuthorizationReflectionAsync` swallows every exception to `Indeterminate`, `CollapseAuthorizationAsync` then pins the restricted surface, and that surface offers no Refresh, Retry or Reset while `EnsureReadRefreshLeaseAsync` and `CanRecover` are both gated on `IsAuthorized`. Nothing re-enters resolution unless an `AuthenticationStateChanged` happens to fire. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1235] **CLOSED (2026-08-08, Story 1.11):** Indeterminate Retry / RetryAuthorization path applied; see GA page patches.

### DW-141: Deferred to Story 1.11
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-29, review loop 4)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:602
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** — The workspace's Global Administrators entry link evaluates authorization uncorroborated while initial resolution uses the corroborated resolver, so the link and the page it targets desynchronize in both directions after any `AuthenticationStateChanged`: the button can render for a principal the page then refuses, or hide while the claims are in fact sufficient. [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:602] **CLOSED (2026-08-08, Story 1.11):** workspace authentication transitions resolve through the strict BFF seam before restoring the entry.
status: done 2026-08-08
archived: 2026-10-12
resolution: Legacy completion record: - **Deferred to Story 1.11** — The workspace's Global Administrators entry link evaluates authorization uncorroborated while initial resolution uses the corroborated resolver, so the link and the page it targets desynchronize in both directions after any `AuthenticationStateChanged`: the button can render for a principal the page then refuses, or hide while the claims are in fact sufficient. [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:602] **CLOSED (2026-08-08, Story 1.11):** workspace authentication transitions resolve through the strict BFF seam before restoring the entry.

### DW-143: `EventStore:BaseAddress` is accepted with compound service-discovery schemes (e.g. `https+http://eventstore`) by the same `TryGetHttpBaseAddress` gate used for the read side, but no service discovery is attached to the command/status clients, so such a value can only fail at send time
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-30, chunk A+B transport/gateway)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:96
source_spec: `_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md`
reason: The legacy ledger defers this issue: `EventStore:BaseAddress` is accepted with compound service-discovery schemes (e.g. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-10-direct-tenants-reads-and-authoritative-freshness.md` summary: `EventStore:BaseAddress` is accepted with compound service-discovery schemes (e.g. `https+http://eventstore`) by the same `TryGetHttpBaseAddress` gate used for the read side, but no service discovery is attached to the command/status clients, so such a value can only fail at send time. evidence: The scheme gate is shared at `src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:96`, while `.AddServiceDiscovery()` is attached only to the Tenants read client at `:74`. Pre-existing: the command-side gate predates Story 1.10's read transport. status: closed-as-duplicate (2026-08-08, Story 1.11 loop 7) — reaffirmation only; keep the 2026-07-29 `EventStore:BaseAddress` / missing `.AddServiceDiscovery()` bullet as the single open entry. Resolve together with the read-side service-discovery provider decision recorded in the 2026-07-30 review findings.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:162-167 rejects compound service-discovery schemes and accepts only exact http/https.

### DW-145: Deferred to Story 1.11
origin: migrated from legacy ledger ("Architectural decision recorded by code review of spec-1-10 (2026-07-30) — BFF read transport vs DAPR service invocation"), 2026-08-25
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:21-27; TenantDetailPage.razor:149
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** (owner decision, 1.10 chunk-A+B review 2026-07-30) — `LifecycleAuthorizationReflection` resolves the principal from `IHttpContextAccessor`, which is null for the whole interactive circuit, so `Evaluate(null)` returns `Indeterminate` permanently and `TenantDetailPage.razor:149` gates tenant lifecycle actions off for a signed-in global administrator for the rest of the session. Story 1.10 added `ResolveGlobalAdministratorsAuthorizationAsync` to the same type and migrated the workspace and global-administrators pages to circuit-aware resolution, leaving the tenant-detail consumer on the synchronous path. Reason for deferral: 1.11 already owns two open principal-resolution decisions on the same evaluator, so all three are settled together rather than by two stories patching it independently. [src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:21-27] **CLOSED (2026-08-08, Story 1.11):** tenant detail consumes `ResolveLifecycleAuthorizationAsync`; synchronous HttpContext-only reflection no longer gates lifecycle actions.
status: done 2026-08-08
archived: 2026-10-12
resolution: Legacy completion record: - **Deferred to Story 1.11** (owner decision, 1.10 chunk-A+B review 2026-07-30) — `LifecycleAuthorizationReflection` resolves the principal from `IHttpContextAccessor`, which is null for the whole interactive circuit, so `Evaluate(null)` returns `Indeterminate` permanently and `TenantDetailPage.razor:149` gates tenant lifecycle actions off for a signed-in global administrator for the rest of the session. Story 1.10 added `ResolveGlobalAdministratorsAuthorizationAsync` to the same type and migrated the workspace and global-administrators pages to circuit-aware resolution, leaving the tenant-detail consumer on the synchronous path. Reason for deferral: 1.11 already owns two open principal-resolution decisions on the same evaluator, so all three are settled together rather than by two stories patching it independently. [src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs:21-27] **CLOSED (2026-08-08, Story 1.11):** tenant detail consumes `ResolveLifecycleAuthorizationAsync`; synchronous HttpContext-only reflection no longer gates lifecycle actions.

### DW-146: Member evidence gate collapses lifecycle and permission reasons into "stale data"
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-30)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:496-499
reason: The legacy ledger defers this issue: Member evidence gate collapses lifecycle and permission reasons into "stale data". Original context is preserved in legacy-detail.
legacy-detail: - **Member evidence gate collapses lifecycle and permission reasons into "stale data"** — `MemberAccessReview.ResolveFailClosedReasons` gained a `!ActionsAreEvidenceBacked -> [UnavailableReason.StaleData]` arm inserted above the pre-existing `Detail.Status is Disabled or Unknown -> MissingLifecycleSupport` and `role is TenantRole.Unknown -> MissingPermission` arms. Because `ActionsAreEvidenceBacked` requires detail `Ready` + `Current` + `Current`, members `Ready|Empty` + `Current` + `Current`, and equal non-blank projection versions, a disabled tenant or an unknown-role member reports "stale data" whenever any clause is short — including the common `Unknown` freshness case. `PrimaryUnavailableReason` feeds the same value into the authorization-safe empty message, so that copy loses its permission wording too. Reason for deferral: defensible as written. Without current, version-consistent evidence the code genuinely cannot assert a lifecycle or permission conclusion, so failing to the weakest claim is the fail-closed reading. Recorded as a design choice, not a defect. Revisit if: operators report the reason as unhelpful, or AC6's distinctness requirement is ever extended from surface kinds to the action-unavailable reason enum. [src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:496-499]
status: done 2026-09-02
archived: 2026-10-12
decision: 2026-09-02 Retain weakest claim — Keep the current fail-closed reason until the recorded operator-feedback or acceptance-criteria trigger occurs.
resolution: closed by human decision: Keep the current fail-closed reason until the recorded operator-feedback or acceptance-criteria trigger occurs.
decision: 2026-09-02 Retain weakest claim — Keep the current fail-closed reason until the recorded operator-feedback or acceptance-criteria trigger occurs.

### DW-147: Mobile read-only for platform-authority mutations is enforced only by CSS
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-30)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css:200-216
reason: The legacy ledger defers this issue: Mobile read-only for platform-authority mutations is enforced only by CSS. Original context is preserved in legacy-detail.
legacy-detail: - **Mobile read-only for platform-authority mutations is enforced only by CSS** — `@media (max-width: 42rem)` sets `display: none` on `.global-admins__mutation-initiation` (both the descendant and `::deep` selectors), and the paired `FluentMessageBar` states "Grant and remove controls require a wider viewport." The `EditForm … OnSubmit="SubmitGrantAsync"`, the grant submit button and the per-row Remove `FluentButton` remain rendered and wired over the circuit; `SubmitGrantAsync`, `PreviewRemove` and `SubmitRemoveAsync` contain no viewport check, and a hidden element still dispatches events in Blazor Server. Reason for deferral: viewport is an affordance, not an authorization boundary. The server API plus the existing authorization, read-surface, freshness and completeness gates remain the real enforcement, so this is a copy-accuracy point rather than a security defect. Revisit if: the notice is ever restated as a safety guarantee, or a viewport-scoped capability becomes part of the authorization model. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor.css:200-216]
status: done 2026-08-28
archived: 2026-10-12
resolution: already resolved: commit 03566fb1; src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorActionAvailabilityEvaluator.cs:84-87 and GlobalAdministratorsPage.razor:2153-2159,2410-2418 now re-evaluate runtime viewport safety before submission.

### DW-148: Deferred to Story 1.11
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-30)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1279; TenantsBffComposition.cs:30-32
reason: The legacy ledger defers this issue: Deferred to Story 1.11. Original context is preserved in legacy-detail.
legacy-detail: - **Deferred to Story 1.11** (owner decision, 1.10 chunk-C+D+E review 2026-07-30) — `GlobalAdministratorsPage.ApplyAuthenticationStateChangedAsync:1279` re-authorizes by calling `TenantsGlobalAdministratorClaims.Evaluate(authenticationState.User)` directly, while every other path on the page (`OnInitializedAsync`, `ReauthorizeAsync`, therefore every load, `SubmitGrantAsync`, `SubmitRemoveAsync`, both status refreshes) goes through `BffComposition.ResolveGlobalAdministratorsAuthorizationAsync()`, which consults the claims property **only** when no `ITenantConfigurationPrincipalResolver` is registered (`TenantsBffComposition.cs:30-32`). With a resolver registered the two evaluators can disagree, so a principal the resolver would classify `Indeterminate`/`MissingPermission` becomes `Authorized` after any `AuthenticationStateChanged` notification and unlocks the grant/remove surfaces for the rest of that circuit, subject only to the freshness gates. `ReauthorizeAsync()` exists at `:1352-1363`, so the consistent fix is one line. Reason for deferral: the one-line fix's correctness depends entirely on how 1.11 resolves circuit-vs-`HttpContext` precedence. The chunk A+B review established that `LifecycleAuthorizationReflection` on this same type returns `Indeterminate` permanently on an interactive circuit because `HttpContext` is null; if `TenantConfigurationPrincipalResolver` shares that weakness, routing this path through it trades a fail-open for a fail-shut. Story 1.11 already owns both open principal-resolution decisions on this evaluator, and the structurally identical `TenantDetailPage` item was folded there on 2026-07-30 for the same stated reason — avoid two stories making conflicting fixes to the same evaluator. Accepted consequence until 1.11 lands: the fail-open divergence above ships in 1.10. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1279] **CLOSED (2026-08-08, Story 1.11):** transition path aligned to the strict BFF resolver; see applied auth-transition patches.
status: done 2026-08-08
archived: 2026-10-12
resolution: Legacy completion record: - **Deferred to Story 1.11** (owner decision, 1.10 chunk-C+D+E review 2026-07-30) — `GlobalAdministratorsPage.ApplyAuthenticationStateChangedAsync:1279` re-authorizes by calling `TenantsGlobalAdministratorClaims.Evaluate(authenticationState.User)` directly, while every other path on the page (`OnInitializedAsync`, `ReauthorizeAsync`, therefore every load, `SubmitGrantAsync`, `SubmitRemoveAsync`, both status refreshes) goes through `BffComposition.ResolveGlobalAdministratorsAuthorizationAsync()`, which consults the claims property **only** when no `ITenantConfigurationPrincipalResolver` is registered (`TenantsBffComposition.cs:30-32`). With a resolver registered the two evaluators can disagree, so a principal the resolver would classify `Indeterminate`/`MissingPermission` becomes `Authorized` after any `AuthenticationStateChanged` notification and unlocks the grant/remove surfaces for the rest of that circuit, subject only to the freshness gates. `ReauthorizeAsync()` exists at `:1352-1363`, so the consistent fix is one line. Reason for deferral: the one-line fix's correctness depends entirely on how 1.11 resolves circuit-vs-`HttpContext` precedence. The chunk A+B review established that `LifecycleAuthorizationReflection` on this same type returns `Indeterminate` permanently on an interactive circuit because `HttpContext` is null; if `TenantConfigurationPrincipalResolver` shares that weakness, routing this path through it trades a fail-open for a fail-shut. Story 1.11 already owns both open principal-resolution decisions on this evaluator, and the structurally identical `TenantDetailPage` item was folded there on 2026-07-30 for the same stated reason — avoid two stories making conflicting fixes to the same evaluator. Accepted consequence until 1.11 lands: the fail-open divergence above ships in 1.10. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1279] **CLOSED (2026-08-08, Story 1.11):** transition path aligned to the strict BFF resolver; see applied auth-transition patches.

### DW-149: Read-refresh lease retry is unverified on the tenant detail page
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-31)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:394-400
reason: The legacy ledger defers this issue: Read-refresh lease retry is unverified on the tenant detail page. Original context is preserved in legacy-detail.
legacy-detail: - **Read-refresh lease retry is unverified on the tenant detail page** — every detail-page test stubs `IProjectionSubscription.SubscribeAsync` to a successful subscription, so `lease.IsSubscribed` is always true. The `if (!lease.IsSubscribed) return;` early return and the `OnAfterRenderAsync` retry that exists to recover a superseded or failed setup are both unexecuted; recording the empty lease anyway, or deleting the `OnAfterRenderAsync` override outright, survives the suite. `TenantReadRefreshSubscriptionTests` proves a failed setup returns a non-subscribed lease rather than throwing, and `GlobalAdministratorsPageTests` is exactly the retry test the detail page lacks. Reason for deferral: the shared read-refresh lease pattern is not a Story 1.6 surface — the same gap applies to every page that binds a lease, and the sibling page already carries the canonical test to copy. Revisit if: a lease-setup failure is ever observed in a running circuit, or the read-refresh pattern is consolidated. [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:394-400,441-446]
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:804-888 and TenantDetailSurfaceTests.cs:7438-7501 implement three bounded setup attempts and a fresh route budget.

### DW-151: Undeclared `references/Hexalith.EventStore` gitlink bump in the working tree
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration (2026-07-31)"), 2026-08-25
location: references/Hexalith.EventStore; scripts/validate-story-gitlinks.py
reason: The legacy ledger defers this issue: Undeclared `references/Hexalith.EventStore` gitlink bump in the working tree. Original context is preserved in legacy-detail.
legacy-detail: - **Undeclared `references/Hexalith.EventStore` gitlink bump in the working tree** — `a40ab8a` → `e4618d9` (v3.86.0), uncommitted and named in no story File List. `scripts/validate-story-gitlinks.py` also exits 1 for Story 1.6, but every UNDECLARED pointer it reports was moved by a Story 1.9 / Epic 2 commit after this story's stale baseline; no Story 1.6 commit after `ec7ec8c` moves a gitlink, and `ec7ec8c`'s EventStore bump is declared. Separately worth noting: nine of those later bumps rode along inside `feat:`/`fix:`/`test:`/ `refactor:` commits rather than dedicated `build(deps)` commits — the exact pattern the guard was created for, now recurring under other stories' names. Reason for deferral: not Story 1.6's change. Belongs to whoever is holding the working-tree bump, as either a separate `build(deps)` commit or a revert. Revisit if: the bump is committed without declaration, or the ride-along pattern recurs a fourth time. [references/Hexalith.EventStore]
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 10db1cee moved the EventStore pointer to 67c645ab02b21ffcb7bef9530e524e4510e36d27; the current lowercase submodule status is unrelated worktree dirt.

### DW-152: Composition availability-pair guard is logically asymmetric
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:60; Program.cs
reason: The legacy ledger defers this issue: Composition availability-pair guard is logically asymmetric. Original context is preserved in legacy-detail.
legacy-detail: - **Composition availability-pair guard is logically asymmetric** — `gatewayIsUnavailable` compares `ServiceDescriptor.ImplementationType` against `UnavailableTenantQueryGateway`, which is `internal` and is null for factory- or instance-registered services. A host declaring a truthful `IsConnected: false` alongside any other gateway is therefore rejected with the inverted message "declares IsConnected: false while the registered ITenantQueryGateway is a connected implementation", while the mismatched pairing the guard exists to catch — `UnavailableTenantQueryGateway` registered via a factory with `IsConnected: true` — passes. The check is also skipped entirely unless availability is registered as an instance. Reason for deferral: unreachable in practice. `Hexalith.Tenants.UI` ships as a container application, not a NuGet package; the only production caller is its own `Program.cs`, which pre-registers nothing; and the sole assemblies that can name the internal type are the two test projects, which use the instance form. Revisit if: `Hexalith.Tenants.UI` is ever published as a package, or a second host composes the module. [src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:60]
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:57-87 handles type, instance, and unknowable factory registrations with tri-state matching.

### DW-153: Member mutation flows are outside the projection-lifecycle policy
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:223
reason: The legacy ledger defers this issue: Member mutation flows are outside the projection-lifecycle policy. Original context is preserved in legacy-detail.
legacy-detail: - **Member mutation flows are outside the projection-lifecycle policy** — `ChangeTenantMemberRoleFlow`, `RemoveTenantMemberFlow`, `AddTenantMemberFlow` and `CreateTenantFlow` have no `Lifecycle` parameter and gate on freshness and surface kind only, while `33abe27` added `Lifecycle is not Current` gates to the four configuration and metadata flows. With a rebuilding projection, editing tenant metadata is blocked but removing a member — the higher-consequence, harder-to-reverse action — is not. Reason for deferral: consequence of the open lifecycle-gate decision recorded in the story's loop-8 review findings, not an independent defect. Resolving that decision determines whether these flows should be brought into the policy or the policy narrowed. Revisit if: the lifecycle-gate decision resolves toward keeping the strict gate. [src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:223]
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:511-522 requires current detail/member lifecycle and freshness evidence with matching nonblank projection versions.

### DW-156: Command `SafeMessage` values are hardcoded English literals rather than `TenantsResources.resx` entries
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRemoveCommandSnapshot.cs:189; TenantsResources.resx
reason: The legacy ledger defers this issue: Command `SafeMessage` values are hardcoded English literals rather than `TenantsResources.resx` entries. Original context is preserved in legacy-detail.
legacy-detail: - Command `SafeMessage` values are hardcoded English literals rather than `TenantsResources.resx` entries. The new page-scoped global-administrator removal message is a hardcoded literal, but so is the pre-existing "Current complete projection evidence is required…" arm it branches against, and the same shape recurs across the command snapshot types. Reason for deferral: pre-existing pattern, not introduced by this story. Converting one arm in isolation would leave the file internally inconsistent and split one message pair across two mechanisms. Revisit if: the command snapshots get a localization pass, or EN/FR parity is enforced by a governance test that reaches C# literals rather than only `.resx` keys. [src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRemoveCommandSnapshot.cs:189]
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: commit 7d865ce8; src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRemoveCommandSnapshot.cs:219-233 uses localizable resource keys instead of raw English SafeMessage literals.

### DW-157: `RestQueryClientAdapter` carries 13 lines of dead freshness computation that re-implement `TenantsRestQueryClient.ResolveFreshness`, discard the result, and omit the `IsDegraded == true` collapse both production implementations perform — so it will drift silently while reading as if it models the client
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-07-31)"), 2026-08-25
location: tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:6044
reason: The legacy ledger defers this issue: `RestQueryClientAdapter` carries 13 lines of dead freshness computation that re-implement `TenantsRestQueryClient.ResolveFreshness`, discard the result, and omit the `IsDegraded == true` collapse both production implementations perform — so it will drift silently while reading as if it models the client. Original context is preserved in legacy-detail.
legacy-detail: - `RestQueryClientAdapter` carries 13 lines of dead freshness computation that re-implement `TenantsRestQueryClient.ResolveFreshness`, discard the result, and omit the `IsDegraded == true` collapse both production implementations perform — so it will drift silently while reading as if it models the client. Reason for deferral: subsumed by the open decision on the gateway test harness. Whether to delete the block or delete the whole adapter depends on which option that decision takes. Revisit if: the harness decision resolves. [tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:6044]
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 845a15e4; tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantQueryGatewayTests.cs:7348-7415 confirms the dead adapter freshness ladder was removed.

### DW-162: `scripts/validate-story-gitlinks.py` keeps no automated test and no CI wiring
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-08-01)"), 2026-08-25
location: scripts/validate-story-gitlinks.py; scripts/validate-story-gitlinks.py:264
reason: The legacy ledger defers this issue: `scripts/validate-story-gitlinks.py` keeps no automated test and no CI wiring. Original context is preserved in legacy-detail.
legacy-detail: - `scripts/validate-story-gitlinks.py` keeps no automated test and no CI wiring. Reason for deferral: owner decision D-K, option (c). Manual verification is accepted; introducing Python test infrastructure to a .NET repository is not warranted for this guard, and porting the check to C# would duplicate the script rather than test it. The evidence stands and is recorded so it is not mistaken for coverage: review loop 12 mutation-verified that replacing `stated = stated_targets(story_text)` with `stated = {}` turns a spec whose stated target SHA was corrupted to `deadbee` from FAIL/exit 1 into PASS/exit 0, while the real spec still exits 0 — so the normal workflow invocation stays green and nothing notices. The repository has no Python test infrastructure (no `conftest.py`, `pytest.ini` or `test_*.py`) and `grep -rn validate-story-gitlinks .github/` returns nothing. The script is release-gating per `project-context.md:149`, so its correctness currently rests on the operator running it and reading the output. Revisit if: the repository gains a Python test lane for any other reason, or a story ships an undeclared `references/` gitlink despite the guard. [scripts/validate-story-gitlinks.py:264]
status: done 2026-08-25
archived: 2026-10-12
resolution: closed by human decision: Retain owner decision D-K(c) that manual mutation verification is sufficient.
decision: 2026-08-25 Accept manual evidence — Retain owner decision D-K(c) that manual mutation verification is sufficient.

### DW-163: A tenant configuration key written by a producer other than this UI, carrying an invisible separator or other untypeable character, remains permanently unremovable through the UI
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-10-direct-tenants-reads-and-authoritative-freshness (2026-08-01)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor:495; SetTenantConfigurationFlow.razor:430-448
reason: The legacy ledger defers this issue: A tenant configuration key written by a producer other than this UI, carrying an invisible separator or other untypeable character, remains permanently unremovable through the UI. Original context is preserved in legacy-detail.
legacy-detail: - A tenant configuration key written by a producer other than this UI, carrying an invisible separator or other untypeable character, remains permanently unremovable through the UI. Reason for deferral: owner decision D-J, option (a). `ContainsUntypeableCharacter` (`SetTenantConfigurationFlow.razor:430-448`) bounds the only producer this story owns, which is accepted as the guard's scope. Configuration keys are consumer-owned (`project-context.md:74`) and writable through `POST /api/v1/commands`; such a key renders identically to its clean twin and can never satisfy `RemoveTenantConfigurationFlow.razor:495`'s ordinal match against typed confirmation text, which offers no alternative affordance. The guard's comment is being corrected to state this scope rather than claim the exposure is closed. Revisit if: a compensating-command path for removing such a key is needed in support, or the remove flow gains a non-typed confirmation affordance. [src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor:495]
status: done 2026-08-25
archived: 2026-10-12
resolution: closed by human decision: Retain owner decision D-J(a) that UI removability covers only UI-produced keys.
decision: 2026-08-25 Accept producer boundary — Retain owner decision D-J(a) that UI removability covers only UI-produced keys.

### DW-167: The grant and remove submit buttons are never disabled while a mutation is in flight
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review, loop 2 (2026-08-01)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:777
reason: The legacy ledger defers this issue: The grant and remove submit buttons are never disabled while a mutation is in flight. Original context is preserved in legacy-detail.
legacy-detail: - ~~The grant and remove submit buttons are never disabled while a mutation is in flight.~~ **WITHDRAWN by code review loop 3 (2026-08-01): the premise is false and was never true.** `IsGrantSubmitDisabled` is `!string.IsNullOrWhiteSpace(GrantUnavailableReason)`, and `GrantUnavailableReason` returns `Tenants.GlobalAdministrators.Grant.Unavailable.InFlight` whenever `IsGrantInFlight` — which is `_isGrantSubmitting || State is RequestSent or Accepted or ProjectionPending`. `IsRemoveSubmitDisabled` names `IsGrantInFlight || IsRemoveInFlight` outright. Both bindings therefore do depend on in-flight state. The real exposure was narrower and is already fixed: hoisting `ReauthorizeAsync` to be the submit handlers' first await consumed the render that would have shown the in-flight state, so the disabled attribute never reached the DOM. The marshalled `await InvokeAsync(StateHasChanged)` after the `RequestSent` write closes it. Left in the ledger as a withdrawal rather than deleted, because a future sweep reading the original entry would re-derive a defect that does not exist. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:777]
status: done 2026-08-01
archived: 2026-10-12
resolution: Legacy completion record: - ~~The grant and remove submit buttons are never disabled while a mutation is in flight.~~ **WITHDRAWN by code review loop 3 (2026-08-01): the premise is false and was never true.** `IsGrantSubmitDisabled` is `!string.IsNullOrWhiteSpace(GrantUnavailableReason)`, and `GrantUnavailableReason` returns `Tenants.GlobalAdministrators.Grant.Unavailable.InFlight` whenever `IsGrantInFlight` — which is `_isGrantSubmitting || State is RequestSent or Accepted or ProjectionPending`. `IsRemoveSubmitDisabled` names `IsGrantInFlight || IsRemoveInFlight` outright. Both bindings therefore do depend on in-flight state. The real exposure was narrower and is already fixed: hoisting `ReauthorizeAsync` to be the submit handlers' first await consumed the render that would have shown the in-flight state, so the disabled attribute never reached the DOM. The marshalled `await InvokeAsync(StateHasChanged)` after the `RequestSent` write closes it. Left in the ledger as a withdrawal rather than deleted, because a future sweep reading the original entry would re-derive a defect that does not exist. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:777]

### DW-169: On narrow viewports the per-row Remove launcher is hidden by CSS with no per-row localized reason; the actions
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review, loop 2 (2026-08-01)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:466
reason: The legacy ledger defers this issue: On narrow viewports the per-row Remove launcher is hidden by CSS with no per-row localized reason; the actions. Original context is preserved in legacy-detail.
legacy-detail: - On narrow viewports the per-row Remove launcher is hidden by CSS with no per-row localized reason; the actions cell renders nothing where the control was, and the grant cell simultaneously renders an "available" string while its controls are hidden. Only a single page-level notice explains the read-only mode. Reason for deferral: AC5 requires the actions be visibly unavailable with a localized reason, and the page-level reason satisfies that in substance; per-row parity is polish. Revisit if: the mobile read-only surface is revisited, or accessibility review flags the actions cell. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:466] **CLOSED (2026-08-08, loop 5 APPLIED):** mobile/unavailable reason spans and mutation-initiation gating closed AC5; no longer open polish debt for Story 1.11.
status: done 2026-08-08
archived: 2026-10-12
resolution: Legacy completion record: - On narrow viewports the per-row Remove launcher is hidden by CSS with no per-row localized reason; the actions cell renders nothing where the control was, and the grant cell simultaneously renders an "available" string while its controls are hidden. Only a single page-level notice explains the read-only mode. Reason for deferral: AC5 requires the actions be visibly unavailable with a localized reason, and the page-level reason satisfies that in substance; per-row parity is polish. Revisit if: the mobile read-only surface is revisited, or accessibility review flags the actions cell. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:466] **CLOSED (2026-08-08, loop 5 APPLIED):** mobile/unavailable reason spans and mutation-initiation gating closed AC5; no longer open polish debt for Story 1.11.

### DW-170: Multi-page populations can permanently land grant/remove confirmation in page-scoped `UnableToVerify`
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-08, loop 5 chunk 2)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:178
reason: The legacy ledger defers this issue: Multi-page populations can permanently land grant/remove confirmation in page-scoped `UnableToVerify`. Original context is preserved in legacy-detail.
legacy-detail: - Multi-page populations can permanently land grant/remove confirmation in page-scoped `UnableToVerify` because requery always loads page one. Page-scoped SafeMessages document the honesty limit; adding search-by-id or deep-link verification would widen the story past its fixed-scope review boundary. [src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:178]
status: done 2026-09-02
archived: 2026-10-12
resolution: already resolved: commit 03566fb1; GlobalAdministratorsPage.razor:1442-1466,3008-3019,3710-3734 performs bounded complete-population loading before grant and removal confirmation.

### DW-171: UnableToVerify copy mentions confirming via the tenant audit trail without an in-page navigation link
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-08, loop 5 chunk 2)"), 2026-08-25
location: src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:184
reason: The legacy ledger defers this issue: UnableToVerify copy mentions confirming via the tenant audit trail without an in-page navigation link. Original context is preserved in legacy-detail.
legacy-detail: - UnableToVerify copy mentions confirming via the tenant audit trail without an in-page navigation link. Audit navigation is outside this story's File List and acceptance criteria. [src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:184]
status: done 2026-09-02
archived: 2026-10-12
resolution: already resolved: commit 03566fb1; GlobalAdministratorGrantCommandSnapshot.cs:337-350 accepts only complete evidence, and the legacy PageScoped audit-trail copy remains only as unreferenced resource keys.

### DW-172: A `Ready` snapshot reporting `HasMore == true` with a blank `NextCursor` is a silent dead end. Loop 2 correctly
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-01, loop 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:698
reason: The legacy ledger defers this issue: A `Ready` snapshot reporting `HasMore == true` with a blank `NextCursor` is a silent dead end. Original context is preserved in legacy-detail.
legacy-detail: - A `Ready` snapshot reporting `HasMore == true` with a blank `NextCursor` is a silent dead end. Loop 2 correctly converted the dead-but-clickable Next into a disabled Next, but `CanRecover` deliberately excludes `Ready`, so neither Retry nor Reset renders, Previous is disabled on page one, and no notice explains the condition. The surface states more administrators exist and offers no way to reach them. Reason for deferral: needs a copy/design decision on how to announce incomplete evidence on an otherwise healthy surface, not a mechanical fix; the service should not normally produce this shape. Revisit if: the query contract allows `HasMore` without a cursor, or `CanRecover` is revised. **SUPERSEDED (2026-08-08, loop 5):** owner chose option 1 — condition-gated recoverable incomplete paging (`HasMore && blank NextCursor`) with localized notice. **CLOSED (2026-08-08, loop 5 APPLIED):** `HasIncompletePagingEvidence` / recovery + localized notice shipped; no longer an open loop-5 patch. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:698]
status: done 2026-08-08
archived: 2026-10-12
resolution: Legacy completion record: - A `Ready` snapshot reporting `HasMore == true` with a blank `NextCursor` is a silent dead end. Loop 2 correctly converted the dead-but-clickable Next into a disabled Next, but `CanRecover` deliberately excludes `Ready`, so neither Retry nor Reset renders, Previous is disabled on page one, and no notice explains the condition. The surface states more administrators exist and offers no way to reach them. Reason for deferral: needs a copy/design decision on how to announce incomplete evidence on an otherwise healthy surface, not a mechanical fix; the service should not normally produce this shape. Revisit if: the query contract allows `HasMore` without a cursor, or `CanRecover` is revised. **SUPERSEDED (2026-08-08, loop 5):** owner chose option 1 — condition-gated recoverable incomplete paging (`HasMore && blank NextCursor`) with localized notice. **CLOSED (2026-08-08, loop 5 APPLIED):** `HasIncompletePagingEvidence` / recovery + localized notice shipped; no longer an open loop-5 patch. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:698]

### DW-174: AC5 remains partially unmet while the story sits in `review`: the narrow-viewport per-row Remove reason gap
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review (2026-08-01, loop 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:466
reason: The legacy ledger defers this issue: AC5 remains partially unmet while the story sits in `review`: the narrow-viewport per-row Remove reason gap. Original context is preserved in legacy-detail.
legacy-detail: - ~~AC5 remains partially unmet while the story sits in `review`: the narrow-viewport per-row Remove reason gap recorded above by loop 2 was re-confirmed still open by loop 3.~~ **CLOSED (2026-08-08, loop 5 APPLIED):** mobile/unavailable reasons and mutation-initiation gating closed AC5. Cross-reference only — see the loop-2 bullet and loop-5 APPLIED patch. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:466]
status: done 2026-08-08
archived: 2026-10-12
resolution: Legacy completion record: - ~~AC5 remains partially unmet while the story sits in `review`: the narrow-viewport per-row Remove reason gap recorded above by loop 2 was re-confirmed still open by loop 3.~~ **CLOSED (2026-08-08, loop 5 APPLIED):** mobile/unavailable reasons and mutation-initiation gating closed AC5. Cross-reference only — see the loop-2 bullet and loop-5 APPLIED patch. [src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:466]

### DW-175: TenantsWorkspace nests ProjectionLifecycleBadge inside polite atomic status region — deferred, pre-existing lifecycle-badge composition (not core 1.11 auth)
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:171
reason: The legacy ledger defers this issue: TenantsWorkspace nests ProjectionLifecycleBadge inside polite atomic status region — deferred, pre-existing lifecycle-badge composition (not core 1.11 auth). Original context is preserved in legacy-detail.
legacy-detail: - TenantsWorkspace nests ProjectionLifecycleBadge inside polite atomic status region — deferred, pre-existing lifecycle-badge composition (not core 1.11 auth) [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:171]
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit dc2639f0; src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:175-181 renders ProjectionLifecycleStatus outside the notices live region.

### DW-182: EditTenantMetadataFlow Lifecycle wiring not asserted via tenants-edit-metadata-open on the detail page — deferred, covered by EditTenantMetadataFlowTests
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:146
reason: The legacy ledger defers this issue: EditTenantMetadataFlow Lifecycle wiring not asserted via tenants-edit-metadata-open on the detail page — deferred, covered by EditTenantMetadataFlowTests. Original context is preserved in legacy-detail.
legacy-detail: - EditTenantMetadataFlow Lifecycle wiring not asserted via tenants-edit-metadata-open on the detail page — deferred, covered by EditTenantMetadataFlowTests [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:146]
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commits a53cb979 and ad18d62c; TenantDetailPage.razor:149-160 binds lifecycle/proof inputs and TenantDetailSurfaceTests.cs:2942-2961 opens metadata through the page boundary.

### DW-185: Member Next stays enabled when HasMore has blank NextCursor — deferred, MemberAccessReview not in this story File List; page already no-ops; pre-existing pager honesty gap
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:846
reason: The legacy ledger defers this issue: Member Next stays enabled when HasMore has blank NextCursor — deferred, MemberAccessReview not in this story File List; page already no-ops; pre-existing pager honesty gap. Original context is preserved in legacy-detail.
legacy-detail: - Member Next stays enabled when HasMore has blank NextCursor — deferred, MemberAccessReview not in this story File List; page already no-ops; pre-existing pager honesty gap [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:846]
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1202-1211 refuses Next when HasMore is false or NextCursor is blank, and the rendered pager applies the same guard.

### DW-189: Projection-version advancement is opaque inequality only, with no causal/audit-qualified branch
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Projection-version advancement is opaque inequality only, with no causal/audit-qualified branch. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Projection-version advancement is opaque inequality only, with no causal/audit-qualified branch. evidence: Confirm uses non-equal non-empty version strings; safe audit provenance newer than baseline remains unimplemented though the Always clause allows version OR audit.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 28d32ca8; src/Hexalith.Tenants.UI/State/TenantCommands/TenantMembershipCommandProvenance.cs:36-70 requires exact command-event evidence and ordered causal advancement.

### DW-190: AggregateAdmissionGate falls back to a page-private instance when DI resolution fails
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: AggregateAdmissionGate falls back to a page-private instance when DI resolution fails. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: AggregateAdmissionGate falls back to a page-private instance when DI resolution fails. evidence: A private gate cannot enforce circuit-scoped AggregateIdentity admission shared with other consumers; only the DI-registered singleton does.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit d3f74f58; src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:411-412 resolves the admission gate only from DI.

### DW-192: The mandatory story gitlink validator fails against current HEAD because seven unrelated submodule pointer bumps landed after the isolated Story 2.1 commit
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation.md (2026-08-19)"), 2026-08-25
location: scripts/validate-story-gitlinks.py; python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: The mandatory story gitlink validator fails against current HEAD because seven unrelated submodule pointer bumps landed after the isolated Story 2.1 commit. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: The mandatory story gitlink validator fails against current HEAD because seven unrelated submodule pointer bumps landed after the isolated Story 2.1 commit. evidence: `python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` reports all seven `references/` pointers as undeclared between baseline `222d5ac` and HEAD `020b099`, while `222d5ac..29c4aec` changes no gitlink.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 44362aed; spec-2-1-projection-backed-tenant-list.md:147-161 declares all seven pointers and the default validator passes against current HEAD.

### DW-193: Story 2.4b — provenance reconciliation refinements, WP-2A removal-proof assembly (`audit_available`), proof-capability fail-closed gating, and proof-state recovery/tests
origin: migrated from legacy ledger ("Deferred from: bmad-build scope split of Story 2.4 (2026-08-08)"), 2026-08-25
location: audit_available
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Story 2.4b — provenance reconciliation refinements, WP-2A removal-proof assembly (`audit_available`), proof-capability fail-closed gating, and proof-state recovery/tests. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Story 2.4b — provenance reconciliation refinements, WP-2A removal-proof assembly (`audit_available`), proof-capability fail-closed gating, and proof-state recovery/tests. evidence: Split from Story 2.4 so the first delivery goal (2.4a eligibility, complete preview, elevated friction, destructive dialog, dispatch, AggregateIdentity lock) stays within the 900–1600 token spec budget; Story 2.4 remains incomplete until 2.4b also passes.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit fa5ca559; spec-2-4b-wp-2a-removal-proof-and-audit-available.md:1-5 is done and RemoveTenantMemberFlow.razor:985-1080 implements proof assembly.

### DW-196: Remove-member WP-2A assembly only inspects the first audit page; matching UserRemovedFromTenant rows on later pages can leave proof pending
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: Remove-member WP-2A assembly only inspects the first audit page; matching UserRemovedFromTenant rows on later pages can leave proof pending. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: Remove-member WP-2A assembly only inspects the first audit page; matching UserRemovedFromTenant rows on later pages can leave proof pending. evidence: GetTenantAuditAsync is called once without following HasMore/NextCursor; paging loop was deferred to keep this slice within review-patch scope.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit ad18d62c; src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor:985-1080 walks bounded audit pages and detects cursor loops.

### DW-197: Proof-capability fail-closed detects only null/UnavailableTenantQueryGateway, not a live stale/unknown audit-capability probe before open
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: Proof-capability fail-closed detects only null/UnavailableTenantQueryGateway, not a live stale/unknown audit-capability probe before open. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: Proof-capability fail-closed detects only null/UnavailableTenantQueryGateway, not a live stale/unknown audit-capability probe before open. evidence: Per-row audit probes would add latency on every member render; Always clause capability language remains partially approximated until a shared capability signal exists.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit ad18d62c; src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1650-1724 performs a live current scoped projection-backed audit-capability probe.

### DW-202: Create confirmation cannot correlate projection evidence to this attempt's messageId; concurrent same-id creates or unrelated list ProjectionVersion churn can still satisfy absence-then-presence + version rules
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: Create confirmation cannot correlate projection evidence to this attempt's messageId; concurrent same-id creates or unrelated list ProjectionVersion churn can still satisfy absence-then-presence + version rules. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: Create confirmation cannot correlate projection evidence to this attempt's messageId; concurrent same-id creates or unrelated list ProjectionVersion churn can still satisfy absence-then-presence + version rules. evidence: ConfirmProjection uses metadata match plus opaque list/detail version advancement only; command-specific audit provenance branch remains unused (AttemptStartedAtUtc captured but not applied).
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit b2b80941; src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:307-375 requires exact tracked-command evidence plus ordered projection advancement or first appearance.

### DW-203: Workspace IsCommandSurfaceConnected is a render-time service lookup with no subscription, so BFF disconnect may not refresh create availability until an unrelated rerender
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-08)"), 2026-08-25
location: ITenantsBffComposition.IsCommandSurfaceConnected
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: Workspace IsCommandSurfaceConnected is a render-time service lookup with no subscription, so BFF disconnect may not refresh create availability until an unrelated rerender. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: Workspace IsCommandSurfaceConnected is a render-time service lookup with no subscription, so BFF disconnect may not refresh create availability until an unrelated rerender. evidence: The `ITenantsBffComposition.IsCommandSurfaceConnected` property pre-existed, but Story 3.1 introduced the workspace-side render-time lookup and the `IsCommandSurfaceAvailable` parameter binding themselves. Corrected by code review 2026-08-21: the original wording understated what this story added.
status: done 2026-08-28
archived: 2026-10-12
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
archived: 2026-10-12
resolution: already resolved: commits b2b80941 and a53cb979; TenantCreateCommandModels.cs:299-305 treats SignalR as a nudge and :1394-1409 requires command-specific provenance.

### DW-205: Edit metadata confirmation does not pass live audit-row provenance into ConfirmProjection (version advancement only on the live path)
origin: migrated from legacy ledger ("Deferred from: implementation of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Edit metadata confirmation does not pass live audit-row provenance into ConfirmProjection (version advancement only on the live path). Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Edit metadata confirmation does not pass live audit-row provenance into ConfirmProjection (version advancement only on the live path). evidence: Story 3.2 verification-gap review; AttemptStartedAtUtc and hasQualifyingAuditProvenance exist on the snapshot API but EditTenantMetadataFlow never supplies audit qualification (unlike remove-member WP-2A).
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit a53cb979; TenantDetailPage.razor:159-160 supplies AuditEvidenceProvider and TenantCreateCommandModels.cs:1399-1408 validates matching audit proof.

### DW-207: After Confirmed/Rejected/Failed, retained MessageId can be reused on a deliberate new metadata edit instead of minting a new ULID
origin: migrated from legacy ledger ("Deferred from: implementation of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: After Confirmed/Rejected/Failed, retained MessageId can be reused on a deliberate new metadata edit instead of minting a new ULID. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: After Confirmed/Rejected/Failed, retained MessageId can be reused on a deliberate new metadata edit instead of minting a new ULID. evidence: Story 3.2 edge-case review; same reuseMessageId pattern exists on AddTenantMemberFlow and was not uniquely introduced for metadata.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commits a53cb979 and c910ea83; EditTenantMetadataFlow.razor:605-625 reuses an ID only for the same recoverable attempt.

### DW-208: Configuration filter comment claims Ordinal matching while FilteredRows uses OrdinalIgnoreCase
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-6-read-only-tenant-configuration.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration.md`
reason: The legacy ledger defers this issue: Configuration filter comment claims Ordinal matching while FilteredRows uses OrdinalIgnoreCase. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-1-6-read-only-tenant-configuration.md` summary: Configuration filter comment claims Ordinal matching while FilteredRows uses OrdinalIgnoreCase. evidence: Pre-existing comment/code mismatch in TenantConfigurationView.razor; not introduced by the FluentStack host migration reviewed in this pass.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 84ad930d; src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor:177-185 documents and implements Ordinal case-sensitive matching.

### DW-214: Align RemoveTenantMemberFlowTests StubTenantsLocalizer keys/values with shipped TenantsResources (EN/FR) so LocalizerDoubleParityTests passes
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
reason: The legacy ledger defers this issue: Align RemoveTenantMemberFlowTests StubTenantsLocalizer keys/values with shipped TenantsResources (EN/FR) so LocalizerDoubleParityTests passes. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md` summary: Align RemoveTenantMemberFlowTests StubTenantsLocalizer keys/values with shipped TenantsResources (EN/FR) so LocalizerDoubleParityTests passes. evidence: Pre-existing full UI suite failure (1997/1998) unrelated to page-layout governance patches; stub audit-receipt keys drift from resx.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit ad18d62c; LocalizerDoubleParityTests passes and removal preview/audit keys match shipped EN/FR resources.

### DW-223: Page through bounded tenant-audit results while assembling remove-member proof
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Page through bounded tenant-audit results while assembling remove-member proof. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Page through bounded tenant-audit results while assembling remove-member proof. evidence: The existing proof query reads only the first audit page and ignores HasMore and NextCursor, so a qualifying event outside the first page is never found.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit ad18d62c; RemoveTenantMemberFlow.razor:985-1080 implements the bounded multi-page audit walk required by this duplicate.

### DW-224: Require current projection lifecycle, freshness, and projection-backed provenance before promoting removal audit evidence to available proof
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Require current projection lifecycle, freshness, and projection-backed provenance before promoting removal audit evidence to available proof. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Require current projection lifecycle, freshness, and projection-backed provenance before promoting removal audit evidence to available proof. evidence: A Ready audit surface can carry unknown lifecycle or provenance, yet the current proof path accepts its rows once the surface is neither stale nor degraded.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit ad18d62c; RemoveTenantMemberFlow.razor:1017-1054 requires current row/page freshness, lifecycle, projection provenance, and a ready receipt before AuditAvailable.

### DW-226: Downgrade retained global-administrator evidence when a tenant-detail supplementary refresh fails
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Downgrade retained global-administrator evidence when a tenant-detail supplementary refresh fails. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Downgrade retained global-administrator evidence when a tenant-detail supplementary refresh fails. evidence: The existing refresh failure path keeps the previous Current and complete snapshot unchanged, allowing a removal preview to continue asserting platform standing from silently stale evidence.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit ad18d62c; TenantDetailPage.razor:958-980 fail-closes retained GA evidence on refresh failure and :1737-1753 downgrades unsafe evidence.

### DW-228: Refresh remove-member audit guidance now that the flow queries and renders audit receipts
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Refresh remove-member audit guidance now that the flow queries and renders audit receipts. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Refresh remove-member audit guidance now that the flow queries and renders audit receipts. evidence: Existing English and French preview copy still says audit evidence is unavailable until a future evidence source exists, contradicting the implemented receipt query.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit ad18d62c; TenantsResources.resx and TenantsResources.fr.resx:2313-2318 explain that audit evidence may remain pending, delayed, or unavailable.

### DW-229: Verify the tenant-detail global-administrator evidence bridge at the page boundary
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Verify the tenant-detail global-administrator evidence bridge at the page boundary. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Verify the tenant-detail global-administrator evidence bridge at the page boundary. evidence: Existing tests inject GlobalAdministratorsSnapshot directly into MemberAccessReview, so removing the page assignment or parameter binding would not fail coverage.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit ad18d62c; TenantDetailPage.razor:201 binds GlobalAdministrators and TenantDetailSurfaceTests.cs:239-283 verifies propagation.

### DW-230: Pre-existing flaky false-success in the global-administrator grant re-query
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation (2026-08-20)"), 2026-08-25
location: Grant_requery_does_not_confirm_from_a_superseded_snapshot
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Pre-existing flaky false-success in the global-administrator grant re-query. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Pre-existing flaky false-success in the global-administrator grant re-query. evidence: `Grant_requery_does_not_confirm_from_a_superseded_snapshot` fails in 3 of 4 clean-HEAD Release runs, rendering "Projection confirmed the target user" from a superseded snapshot. Introduced by `d0f74a48` (Story 1.11), not by Story 2.1. This is a live non-collapse violation of the same class Epic 2 exists to prevent.
status: done 2026-09-02
archived: 2026-10-12
resolution: already resolved: commit 24e3a41d; GlobalAdministratorsPage.razor:2950-3005 rejects superseded status results and lines 3015-3039 reject superseded projection results.

### DW-231: Global-administrator command surface is not covered by the new AggregateIdentity admission gate
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation (2026-08-20)"), 2026-08-25
location: src/; tests/
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Global-administrator command surface is not covered by the new AggregateIdentity admission gate. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Global-administrator command surface is not covered by the new AggregateIdentity admission gate. evidence: `TenantCommandAggregateLock.ForGlobalAdministrators()` and `TenantAggregateCommandAdmissionGate.HasActiveLock` have zero call sites in `src/` or `tests/`; `GlobalAdministratorsPage` and `GlobalAdministratorCorrectionPanel` dispatch ungated, so one-at-a-time exclusivity holds for tenant aggregates only.
status: done 2026-08-28
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1042-1046,2175-2179,2430-2434 resolves the admission gate and acquires the fixed aggregate lease; commits 03566fb1 and ba060a2f.

### DW-233: A missing admission-gate registration disables Epic 3 command surfaces as well as membership
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation (2026-08-20)"), 2026-08-25
location: IsCommandSurfaceAvailable
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: A missing admission-gate registration disables Epic 3 command surfaces as well as membership. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: A missing admission-gate registration disables Epic 3 command surfaces as well as membership. evidence: `IsCommandSurfaceAvailable` now requires `AggregateAdmissionGate is not null`, and that value is passed to `EditTenantMetadataFlow`, `TenantLifecycleActionAvailability`, and `TenantConfigurationManagement`. Fail-closed, so acceptable, but it widens the blast radius of a composition mistake beyond this story.
status: done 2026-08-28
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Extensions/TenantsUiServiceCollectionExtensions.cs:96 registers TenantAggregateCommandAdmissionGate, while TenantDetailPage.razor:358-362 remains intentionally fail-closed when unavailable.

### DW-236: Serialize child membership-command lease acquisition
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: HandleCommandActivityLeaseAsync
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Serialize child membership-command lease acquisition. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Serialize child membership-command lease acquisition. evidence: `HandleCommandActivityLeaseAsync` checks `_childCommandLeaseOwner` before awaiting the parent lease without a local serialization gate, so two reentrant acquisitions can both observe no owner and dispatch under the same aggregate lock.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 28d32ca8; MemberAccessReview.razor:759-797 reserves _childCommandLeaseOwner before awaiting the parent lease and clears it on refusal or fault.

### DW-237: Keep an open membership command flow keyed to its captured target
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Keep an open membership command flow keyed to its captured target. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Keep an open membership command flow keyed to its captured target. evidence: Opening another row updates the active member parameters while reusing the existing change-role or remove-member component instance, whose snapshot can retain the previous intent and command identity.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 28d32ca8; MemberAccessReview.razor:239-280 keys flows by active user ID and :327-335 retains captured target records.

### DW-238: Give Continue read-only a stable dialog lifecycle
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: RemoveTenantMemberFlow.ContinueReadOnlyAsync
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Give Continue read-only a stable dialog lifecycle. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Give Continue read-only a stable dialog lifecycle. evidence: `RemoveTenantMemberFlow.ContinueReadOnlyAsync` resets the snapshot to Idle without dismissing the dialog; the next parameter/render cycle can immediately reconstruct the preview, leaving the operator in an ambiguous open-flow state.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commits b2b80941 and 28d32ca8; RemoveTenantMemberFlow.razor:1148-1155 marks dismissed before reset and invokes OnCloseRequested.

### DW-239: Do not initialize a pre-command removal preview as missing audit support
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: TenantRemoveMemberCommandSnapshot.Previewed
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Do not initialize a pre-command removal preview as missing audit support. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Do not initialize a pre-command removal preview as missing audit support. evidence: `TenantRemoveMemberCommandSnapshot.Previewed` assigns `AuditState = MissingSupport` before dispatch or proof lookup, so the preview can report missing support even when the parent has already proven live audit capability.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 28d32ca8; TenantCreateCommandModels.cs:851-871 initializes Previewed audit state as NotStarted.

### DW-240: Map command-status HTTP timeouts to a support-safe unknown result
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof (2026-08-20)"), 2026-08-25
location: TenantCommandGateway.GetStatusAsync
source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md`
reason: The legacy ledger defers this issue: Map command-status HTTP timeouts to a support-safe unknown result. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md` summary: Map command-status HTTP timeouts to a support-safe unknown result. evidence: `TenantCommandGateway.GetStatusAsync` catches JSON failures but not `TaskCanceledException` from an `HttpClient` timeout, while removal status refresh calls it with `CancellationToken.None`; an operational timeout can therefore escape the UI recovery path.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 43ef25eb; TenantCommandGateway.cs:491-499 maps operational cancellation and HTTP faults to a support-safe retryable unknown-status result.

### DW-249: AttemptStartedAtUtc and hasQualifyingAuditProvenance are dead in production for metadata; the snapshot test pins a branch no production call site can reach
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: AttemptStartedAtUtc and hasQualifyingAuditProvenance are dead in production for metadata; the snapshot test pins a branch no production call site can reach. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: AttemptStartedAtUtc and hasQualifyingAuditProvenance are dead in production for metadata; the snapshot test pins a branch no production call site can reach. evidence: Both ConfirmProjection call sites (EditTenantMetadataFlow.razor:410 and :573) use the two-argument form, so the flag is permanently false; AttemptStartedAtUtc is stamped in RequestSent and never read for metadata. The frozen "version advancement OR audit provenance" rule is satisfied by the version half, so this is dead API surface rather than a violation. Already partially recorded by this story's own deferred entry.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor:775-787 queries attempt-bound audit evidence and passes it into projection confirmation.

### DW-250: source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Nothing proves the tenant-detail read model's ProjectionVersion actually advances for a same-value update, which is the premise the whole confirmation path now rests on
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-2-edit-tenant-metadata-with-recorded-updates.md (2026-08-21)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Nothing proves the tenant-detail read model's ProjectionVersion actually advances for a same-value update, which is the premise the whole confirmation path now rests on. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Nothing proves the tenant-detail read model's ProjectionVersion actually advances for a same-value update, which is the premise the whole confirmation path now rests on. evidence: TenantAggregateTests proves the aggregate always emits TenantUpdated for identical Name+Description, but not that the projection version moves. A projection that deduped or content-hashed would make every same-value "recorded update" fail closed to UnableToVerify. Requires a Server/Integration-tier test, outside this UI slice's test shape.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants/Projections/TenantProjectionHandler.cs:121-127 stamps every applied event sequence; TenantProjectionHandlerTests.cs:333-372 proves TenantUpdated advances to tenant-sequence:12.

### DW-257: Baseline and evidence projection versions are read from different snapshot lineages -- baseline from `_snapshot.ProjectionVersion`, evidence from `_lastConfirmedSnapshot ?? _snapshot` -- so a failed post-create reload makes a genuinely successful create report `UnableToVerify`
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-1-create-tenant-with-projection-confirmation.md (2026-08-21)"), 2026-08-25
location: _snapshot.ProjectionVersion
source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md`
reason: The legacy ledger defers this issue: Baseline and evidence projection versions are read from different snapshot lineages -- baseline from `_snapshot.ProjectionVersion`, evidence from `_lastConfirmedSnapshot ?? Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md` summary: Baseline and evidence projection versions are read from different snapshot lineages -- baseline from `_snapshot.ProjectionVersion`, evidence from `_lastConfirmedSnapshot ?? _snapshot` -- so a failed post-create reload makes a genuinely successful create report `UnableToVerify`. evidence: Fail-closed direction (false negative, not false confirm) and entangled with the open provenance-gate decision; resolving that decision determines whether this seam changes at all.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs:359-363 captures and compares list and detail baselines like-for-like.

### DW-260: `CreateTenantFlow` never adopts the reusable `messageId` affordance this story added
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-21)"), 2026-08-25
location: TenantCommandGateway.cs:42,69; CreateTenantFlow.razor:348
reason: The legacy ledger defers this issue: `CreateTenantFlow` never adopts the reusable `messageId` affordance this story added. Original context is preserved in legacy-detail.
legacy-detail: - **`CreateTenantFlow` never adopts the reusable `messageId` affordance this story added.** `TenantCommandGateway.CreateTenantAsync` gained `string? messageId = null` and now returns `MessageId` on indeterminate failure (`TenantCommandGateway.cs:42,69`), but `CreateTenantFlow.razor:348` hard-codes `messageId: null` and `:359-368` discards `result.MessageId`. Its own tracking guard at `:307-319` therefore never engages, and a retry after an ambiguous 503 mints a fresh ULID — surfacing `TenantAlreadyExistsRejection` for a tenant the operator just created. Belongs to story `3-1-create-tenant-with-projection-confirmation` (currently `review`).
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: commit 24c978d4; src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor:405-418 reuses the snapshot MessageId and retains accepted tracking.

### DW-263: `RemoveTenantMemberFlow.Dispose` does not release the command-activity lease — attempted and reverted
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md (2026-08-21)"), 2026-08-25
location: *Flow.razor; MemberAccessReview.razor:734-738
reason: The legacy ledger defers this issue: `RemoveTenantMemberFlow.Dispose` does not release the command-activity lease — attempted and reverted. Original context is preserved in legacy-detail.
legacy-detail: - **`RemoveTenantMemberFlow.Dispose` does not release the command-activity lease — attempted and reverted.** An unmount path other than `CloseAsync` leaves `_hasRaisedCommandActivity` true, so the parent's `_childCommandLeaseOwner` and the page's aggregate key stay held. Releasing from the flow was implemented and then reverted: `CommandFlowGuardConformanceTests.Command_flows_do_not_release_page_activity_directly` forbids any `*Flow.razor` from calling `OnCommandActivityChanged.InvokeAsync(false)`, because a flow that self-releases while still Accepted/ProjectionPending would unlock sibling command surfaces before terminal evidence. The parent is the designated owner and already compensates in `MemberAccessReview.DisposeAsync` and on the authorization-teardown path (`MemberAccessReview.razor:734-738`). Any residual gap is a parent-side concern and should be closed there, not in the flow.
status: done 2026-08-28
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:720-744 releases command activity after terminal flow teardown and lines 1043-1048 release it during disposal.

### DW-271: `TenantAggregateCommandAdmissionGate`'s public API changed shape without an obsolete overload
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantDetailPage.razor:1689-1703
reason: The legacy ledger defers this issue: `TenantAggregateCommandAdmissionGate`'s public API changed shape without an obsolete overload. Original context is preserved in legacy-detail.
legacy-detail: - summary: `TenantAggregateCommandAdmissionGate`'s public API changed shape without an obsolete overload. evidence: `:26-46` — same-owner `TryAcquire` now returns `false`, forcing every caller to keep its own bookkeeping (which `TenantDetailPage.razor:1689-1703` reimplements); `Release` at `:55-70` silently no-ops on owner mismatch with no return value, so a leaked lock is undetectable. The `<returns>` doc never mentions the same-owner case.
status: done 2026-08-28
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/State/TenantCommands/TenantAggregateCommandAdmissionGate.cs:22-50 and TenantAggregateCommandLease.cs:36-62 provide owner-aware lease/result APIs while preserving compatible legacy acquisition.
decision: 2026-08-26 Add compatible outcomes — Add explicit owner-aware result APIs while retaining current signatures as compatibility wrappers.

### DW-273: `messageId` remains absent from six `ITenantCommandGateway` methods
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: ITenantCommandGateway.cs:32,36,41,46,51,56
reason: The legacy ledger defers this issue: `messageId` remains absent from six `ITenantCommandGateway` methods. Original context is preserved in legacy-detail.
legacy-detail: - summary: `messageId` remains absent from six `ITenantCommandGateway` methods. evidence: `ITenantCommandGateway.cs:32,36,41,46,51,56` — configuration set/remove, global-administrator set/remove, and tenant enable/disable have no way to reuse a tracking id, so the duplicate-dispatch hazard this change closed for five commands stays open for six. Already recorded from the story 3.2 review.
status: done 2026-08-25
archived: 2026-10-12
resolution: closed by human decision: Treat DW-254 as the canonical decision and close this duplicate without a separate change.
decision: 2026-08-25 Close as duplicate — Treat DW-254 as the canonical decision and close this duplicate without a separate change.

### DW-274: `TenantQueryGateway` dereferences `Detail!` inside the catch that exists to fail safe
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantQueryGateway
reason: The legacy ledger defers this issue: `TenantQueryGateway` dereferences `Detail!` inside the catch that exists to fail safe. Original context is preserved in legacy-detail.
legacy-detail: - summary: `TenantQueryGateway` dereferences `Detail!` inside the catch that exists to fail safe. evidence: `:2131-2152` — if reauthorization throws in the retention helper, the null-forgiving `SanitizeDetail(previous!.Detail!)` can throw from within the safety path.
status: done 2026-08-26
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs:2134-2135,2178-2182 proves the retained detail is non-null and tenant-bound before the guarded catch can access it.

### DW-275: `HasSameTenantDetail` newly compares `ConfigurationManagement.TenantId`
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: HasSameTenantDetail
reason: The legacy ledger defers this issue: `HasSameTenantDetail` newly compares `ConfigurationManagement.TenantId`. Original context is preserved in legacy-detail.
legacy-detail: - summary: `HasSameTenantDetail` newly compares `ConfigurationManagement.TenantId`. evidence: `:2166-2170` — a default-constructed `ConfigurationManagement` with a mismatched `TenantId` now makes the comparison false, so retention paths degrade instead of retaining.
status: done 2026-09-06
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/State/TenantDetail/TenantDetailSnapshot.cs:179-196 constructs sanitized detail and configuration management context atomically; the former HasSameTenantDetail comparison no longer exists.

### DW-277: A `TenantId` change does not notify non-keyed command surfaces that their lease was revoked
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantDetailPage.razor:406-418
reason: The legacy ledger defers this issue: A `TenantId` change does not notify non-keyed command surfaces that their lease was revoked. Original context is preserved in legacy-detail.
legacy-detail: - summary: A `TenantId` change does not notify non-keyed command surfaces that their lease was revoked. evidence: `TenantDetailPage.razor:406-418` releases the old aggregate key on route change, but metadata, lifecycle and configuration flows keep `_hasRaisedCommandActivity` true with no lease behind it.
status: done 2026-08-28
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:149,192 keys child surfaces by tenant, and lifecycle/configuration flows reset raised activity on tenant changes at TenantLifecycleCommandFlow.razor:480-490 and sibling flow equivalents.

### DW-278: The global-administrator aggregation loop uses `ContainsKey`+`Add` and silently drops duplicate or null `UserId` rows
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: TenantDetailPage.razor:1435-1442
reason: The legacy ledger defers this issue: The global-administrator aggregation loop uses `ContainsKey`+`Add` and silently drops duplicate or null `UserId` rows. Original context is preserved in legacy-detail.
legacy-detail: - summary: The global-administrator aggregation loop uses `ContainsKey`+`Add` and silently drops duplicate or null `UserId` rows. evidence: `TenantDetailPage.razor:1435-1442`. `TryAdd` does one lookup; a null `UserId` would throw into the fail-closed catch rather than being handled explicitly.
status: done 2026-08-28
archived: 2026-10-12
resolution: already resolved: commit ba060a2f; src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorsProjectionLoader.cs:63-68 aggregates with TryAdd and lines 130-135 reject null, blank, and control-character identities.

### DW-279: Add and change-role `retryMessageId` exclude the `Rejected` state
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: AddTenantMemberFlow.razor:441-447; ChangeTenantMemberRoleFlow.razor:499-505
reason: The legacy ledger defers this issue: Add and change-role `retryMessageId` exclude the `Rejected` state. Original context is preserved in legacy-detail.
legacy-detail: - summary: Add and change-role `retryMessageId` exclude the `Rejected` state. evidence: `AddTenantMemberFlow.razor:441-447`, `ChangeTenantMemberRoleFlow.razor:499-505` reuse the id only when `State is Failed`, so a `Rejected` attempt for the same intent re-dispatches under a fresh ULID.
status: done 2026-09-06
archived: 2026-10-12
resolution: closed by human decision: Treat rejection as conclusive and retain the current behavior in which a later deliberate submission mints a new identity.
decision: 2026-09-06 New attempt after rejection — Treat rejection as conclusive and retain the current behavior in which a later deliberate submission mints a new identity.

### DW-280: `MemberAccessReview` sets child lease ownership after the await
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: MemberAccessReview
reason: The legacy ledger defers this issue: `MemberAccessReview` sets child lease ownership after the await. Original context is preserved in legacy-detail.
legacy-detail: - summary: `MemberAccessReview` sets child lease ownership after the await. evidence: `:754-780` — `_childCommandLeaseOwner` is assigned only after `await CommandActivityLease(isActive)` returns, so two concurrent membership callers can both pass the `is not null` pre-check and both be granted; the first release then frees an aggregate whose other command is still in flight.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 28d32ca8; src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor:768-770 reserves the child lease owner before awaiting admission.

### DW-281: `CreateTenantFlow` and `TenantsWorkspace` findings were raised against files a peer session rewrote mid-review
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: CreateTenantFlow.razor; TenantsWorkspace.razor
reason: The legacy ledger defers this issue: `CreateTenantFlow` and `TenantsWorkspace` findings were raised against files a peer session rewrote mid-review. Original context is preserved in legacy-detail.
legacy-detail: - summary: `CreateTenantFlow` and `TenantsWorkspace` findings were raised against files a peer session rewrote mid-review. evidence: A concurrent session working story 3.1 changed `CreateTenantFlow.razor` by +152/-50 and `TenantsWorkspace.razor` by +17/-5 during this review, and added `TenantCreateAttemptTracker.cs`. The raised items — fail-open absence baseline at `:435-438`, empty-string tracking ids blocking submit, a transient refresh fault downgrading a confirmed create to `UnableToVerify`, a fabricated `(null, null)` evidence tuple, and `TenantsWorkspace` asserting tenant absence from a stale empty list — must be re-reviewed against the peer's version and belong to story `3-1-create-tenant-with-projection-confirmation`.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commits 753f1ead and 24c978d4 completed the current Story 3.1 re-review and fail-closed create attempt tracking.

### DW-283: An eighth undeclared `references/` pointer move appeared during this review
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: references/Hexalith.EventStore; references/
reason: The legacy ledger defers this issue: An eighth undeclared `references/` pointer move appeared during this review. Original context is preserved in legacy-detail.
legacy-detail: - summary: An eighth undeclared `references/` pointer move appeared during this review. evidence: `references/Hexalith.EventStore` moved `c890235` -> `f8b514f` in the working tree while the review was running, on top of the seven `validate-story-gitlinks.py` already reports. Extends the open chunk-A gitlink decision rather than forming a new one.
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: commit d7329f2a; the EventStore pointer is now tracked at 2ae587024ec7dd7dfaca174bf22aa8d74b7a8dc1 and the working tree contains no undeclared pointer move.

### DW-289: Define causal projection-change handling for valid opaque or content-hash version tokens across command flows
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: IReadModelFreshness.ProjectionVersion
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Define causal projection-change handling for valid opaque or content-hash version tokens across command flows. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Define causal projection-change handling for valid opaque or content-hash version tokens across command flows. evidence: `IReadModelFreshness.ProjectionVersion` explicitly permits opaque content hashes, while the shared causal helper accepts only matching prefixes with increasing numeric suffixes; valid changed tokens therefore fail closed across create and membership consumers.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: src/Hexalith.Tenants/Projections/TenantProjectionVersionFormat.cs:15-18 defines tenant-sequence and TenantProjectionHandler.cs:121-127 stamps incoming event sequence numbers.

### DW-291: Reconcile Epic 3 tracker state so an epic and retrospective are not marked done while Story 3.2 is in review and Stories 3.3 through 3.6 remain backlog
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md — loop 3 chunk B (2026-08-21)"), 2026-08-25
location: sprint-status.yaml
source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md`
reason: The legacy ledger defers this issue: Reconcile Epic 3 tracker state so an epic and retrospective are not marked done while Story 3.2 is in review and Stories 3.3 through 3.6 remain backlog. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md` summary: Reconcile Epic 3 tracker state so an epic and retrospective are not marked done while Story 3.2 is in review and Stories 3.3 through 3.6 remain backlog. evidence: `sprint-status.yaml` currently reports `epic-3: done` and `epic-3-retrospective: done` alongside unfinished Epic 3 story entries, so aggregate status is internally contradictory.
status: done 2026-08-28
archived: 2026-10-12
resolution: already resolved: _bmad-output/implementation-artifacts/sprint-status.yaml:98-118 now marks Epic 3, Stories 3.2 through 3.6, and the Epic 3 retrospective done consistently.

### DW-295: New `EventId(2001, "TenantProjectionNullEventSkipped")` is an unregistered magic literal with no cross-project collision check
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22, pass 2)"), 2026-08-25
location: TenantProjectionHandler.cs:32
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: New `EventId(2001, "TenantProjectionNullEventSkipped")` is an unregistered magic literal with no cross-project collision check. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: New `EventId(2001, "TenantProjectionNullEventSkipped")` is an unregistered magic literal with no cross-project collision check. evidence: `TenantProjectionHandler.cs:32` defines the EventId inline; no EventId registry exists in this codebase to conform to, so establishing one is out of scope for this fix.
status: done 2026-08-25
archived: 2026-10-12
resolution: closed by human decision: Retain the handler-local named EventId because no repository registry contract exists.
decision: 2026-08-25 Accept local named ID — Retain the handler-local named EventId because no repository registry contract exists.

### DW-296: New `TenantProjectionVersionFormat` type deliberately sits outside the namespaces `EventContractReferenceDocumentationTests` sweeps, setting a precedent for future non-contract public types to bypass the assembly's only doc-completeness governance check
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-4b-wp-2a-removal-proof-and-audit-available (2026-08-22, pass 2)"), 2026-08-25
location: src/Hexalith.Tenants.Contracts/Projections/TenantProjectionVersionFormat.cs
source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md`
reason: The legacy ledger defers this issue: New `TenantProjectionVersionFormat` type deliberately sits outside the namespaces `EventContractReferenceDocumentationTests` sweeps, setting a precedent for future non-contract public types to bypass the assembly's only doc-completeness governance check. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-4b-wp-2a-removal-proof-and-audit-available.md` summary: New `TenantProjectionVersionFormat` type deliberately sits outside the namespaces `EventContractReferenceDocumentationTests` sweeps, setting a precedent for future non-contract public types to bypass the assembly's only doc-completeness governance check. evidence: `src/Hexalith.Tenants.Contracts/Projections/TenantProjectionVersionFormat.cs`'s own XML remarks document the intentional namespace choice; `EventContractReferenceDocumentationTests.PublicContractTypes()` only sweeps namespaces ending in `.Commands`/`.Events`/`.Events.Rejections`/`.Queries`/`.Enums`. Worth a broader governance-scope discussion, not a defect in this diff.
status: done 2026-08-25
archived: 2026-10-12
resolution: closed by human decision: Retain the current wire-contract namespace boundary because this non-wire type is already documented.
decision: 2026-08-25 Accept namespace scope — Retain the current wire-contract namespace boundary because this non-wire type is already documented.

### DW-304: The ten-item preview renders from `_snapshot.LastConfirmedProjection ?? Detail` while the eligibility gate validates `Detail`/`ResolvedEvidence`; after an in-flow Refresh the user can be shown facts no gate validated. Fails closed at submit, so consistency rather than correctness. Fix is ambiguous (which source wins)
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: Detail
reason: The legacy ledger defers this issue: The ten-item preview renders from `_snapshot.LastConfirmedProjection ?? Original context is preserved in legacy-detail.
legacy-detail: - The ten-item preview renders from `_snapshot.LastConfirmedProjection ?? Detail` while the eligibility gate validates `Detail`/`ResolvedEvidence`; after an in-flow Refresh the user can be shown facts no gate validated. Fails closed at submit, so consistency rather than correctness. Fix is ambiguous (which source wins).
status: done 2026-08-26
archived: 2026-10-12
resolution: already resolved: commit 43ef25eb; src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor:255-256,586-643 now renders and gates from the same retained confirmed preview evidence.
decision: 2026-08-25 Retained confirmed source — Use one revalidated retained-last-confirmed snapshot for both preview and gate, failing closed when its lifecycle or tenant binding is unsafe.

### DW-305: `tenants-lifecycle-unavailable-reason` is emitted by both the launcher (per action) and the open flow; with the flow open, up to three elements share the testid with different semantics
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: n/a
reason: The legacy ledger defers this issue: `tenants-lifecycle-unavailable-reason` is emitted by both the launcher (per action) and the open flow; with the flow open, up to three elements share the testid with different semantics. Original context is preserved in legacy-detail.
legacy-detail: - `tenants-lifecycle-unavailable-reason` is emitted by both the launcher (per action) and the open flow; with the flow open, up to three elements share the testid with different semantics.
status: done 2026-08-26
archived: 2026-10-12
resolution: already resolved: commit 94d496cf; TenantLifecycleCommandFlow.razor:32 and TenantLifecycleActionAvailability.razor:110 now emit distinct flow and action-specific test IDs.

### DW-307: `TenantLifecycleAttemptTracker` has no attempt expiry and never prunes `_terminalMessageByTenantId`/`_terminalAttemptStartedAtByTenantId`; both grow for the circuit's lifetime. Subsumed by the open decision on bounding a wedged attempt
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: TenantLifecycleAttemptTracker
reason: The legacy ledger defers this issue: `TenantLifecycleAttemptTracker` has no attempt expiry and never prunes `_terminalMessageByTenantId`/`_terminalAttemptStartedAtByTenantId`; both grow for the circuit's lifetime. Original context is preserved in legacy-detail.
legacy-detail: - `TenantLifecycleAttemptTracker` has no attempt expiry and never prunes `_terminalMessageByTenantId`/`_terminalAttemptStartedAtByTenantId`; both grow for the circuit's lifetime. Subsumed by the open decision on bounding a wedged attempt.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 43ef25eb; TenantLifecycleAttemptTracker.cs:253-272 prunes expired terminal tombstones under an injected clock and tests cover the boundary.

### DW-309: `_hasAdoptedRetainedAttempt` is latched before the tracker lookup, and a `Detail.TenantId` change on a mounted flow is never re-adopted. Latent only — the parent renders the flow solely for a loaded tenant
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: _hasAdoptedRetainedAttempt
reason: The legacy ledger defers this issue: `_hasAdoptedRetainedAttempt` is latched before the tracker lookup, and a `Detail.TenantId` change on a mounted flow is never re-adopted. Original context is preserved in legacy-detail.
legacy-detail: - `_hasAdoptedRetainedAttempt` is latched before the tracker lookup, and a `Detail.TenantId` change on a mounted flow is never re-adopted. Latent only — the parent renders the flow solely for a loaded tenant.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 43ef25eb; TenantLifecycleCommandFlow.razor:405-417 resets retained adoption and snapshots when TenantId changes.

### DW-310: `TenantLifecycleAttemptTracker.Remember` compares `AttemptStartedAtUtc` with `<=`, so two attempts within one clock tick collapse. Compare `(AttemptStartedAtUtc, MessageId)` or use a monotonic sequence
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: TenantLifecycleAttemptTracker.Remember
reason: The legacy ledger defers this issue: `TenantLifecycleAttemptTracker.Remember` compares `AttemptStartedAtUtc` with `<=`, so two attempts within one clock tick collapse. Original context is preserved in legacy-detail.
legacy-detail: - `TenantLifecycleAttemptTracker.Remember` compares `AttemptStartedAtUtc` with `<=`, so two attempts within one clock tick collapse. Compare `(AttemptStartedAtUtc, MessageId)` or use a monotonic sequence.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 43ef25eb; TenantLifecycleAttemptTracker.cs:163-195 orders attempts by timestamp and MessageId, with tests at TenantLifecycleAttemptTrackerTests.cs:235-249.

### DW-311: `Remember` mixes contracts: `SetSnapshot` treats `false` as a tracking mismatch, but the method throws `ArgumentException` for shape violations, which escape unhandled from a UI event handler
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: Remember
reason: The legacy ledger defers this issue: `Remember` mixes contracts: `SetSnapshot` treats `false` as a tracking mismatch, but the method throws `ArgumentException` for shape violations, which escape unhandled from a UI event handler. Original context is preserved in legacy-detail.
legacy-detail: - `Remember` mixes contracts: `SetSnapshot` treats `false` as a tracking mismatch, but the method throws `ArgumentException` for shape violations, which escape unhandled from a UI event handler.
status: done 2026-08-25
archived: 2026-10-12
resolution: already resolved: commit 43ef25eb; TenantLifecycleAttemptTracker.cs:129-139 rejects malformed retained shapes without throwing.

### DW-312: Preserve a visible exit when the set-configuration flow is opened wide and the viewport is then narrowed
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-25, loop 2)"), 2026-08-25
location: SetTenantConfigurationFlow.razor.css
source_spec: `_bmad-output/implementation-artifacts/spec-3-4-disable-or-enable-tenant-with-complete-preview.md`
reason: The legacy ledger defers this issue: Preserve a visible exit when the set-configuration flow is opened wide and the viewport is then narrowed. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-3-4-disable-or-enable-tenant-with-complete-preview.md` summary: Preserve a visible exit when the set-configuration flow is opened wide and the viewport is then narrowed. evidence: `SetTenantConfigurationFlow.razor.css` hides the entire form at 767px, including its only Cancel control, and the rule lacks the neighboring FrontComposer CSS exception comment. This belongs to Stories 3.5/3.6; lifecycle-only scope was explicitly retained for this run.
status: done 2026-08-26
archived: 2026-10-12
resolution: already resolved: commit 424d7624; SetTenantConfigurationFlow.razor:128-144 keeps Refresh and Cancel outside the form hidden by SetTenantConfigurationFlow.razor.css:123-130.

### DW-318: Replace the legacy Fluent v4 token in `SetTenantConfigurationFlow`
origin: migrated from legacy ledger ("Deferred from: code review of spec-3-4-disable-or-enable-tenant-with-complete-preview (2026-08-26)"), 2026-08-26
location: src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css:49
reason: The stylesheet uses `var(--accent-fill-rest, LinkText)`, violating the project ban on legacy `--accent-*`, `--neutral-*`, `--type-ramp-*`, and `--palette-*` Fluent tokens. Replace it with the approved Fluent UI v5 styling contract while preserving the intended visual state.
status: done 2026-08-27
archived: 2026-10-12
resolution: already resolved: commit 2e19cc8e; src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css:48-50 uses Fluent v5 --colorBrandStroke1 instead of --accent-fill-rest.

### DW-339: Builds `package-version-audit.json` still records EventStore `3.99.0` while the catalog pin is `3.100.0`.

origin: migrated from legacy ledger (""), 2026-09-02
location: references/Hexalith.Builds/Tools/package-version-audit.json
source_spec: `_bmad-output/implementation-artifacts/spec-eventstore-3-100-0.md`
reason: At deferral time, the Builds audit still contained EventStore `selectedVersion` `3.99.0` after Builds `e1026cb` set `HexalithEventStoreVersion` to `3.100.0`; regenerating the Builds-owned audit was Ask First for the source story.
status: done 2026-09-02
archived: 2026-10-12
resolution: already resolved: references/Hexalith.Builds commit 7e84ff1; package-version-audit.json:91301-91303 and Props/Directory.Packages.props:8 now align current EventStore evidence and catalog at 3.101.0.

### DW-329: Global-administrator command retries do not retain a caller-owned message id across ambiguous transport failures.
origin: spec-deferred 9acd809c21e3
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:329
source_spec: `spec-4-1-fixed-scope-global-administrator-action-availability.md`
severity: high
reason: The existing gateway creates the message id inside SetGlobalAdministratorAsync/RemoveGlobalAdministratorAsync and catches only EventStoreGatewayException, so timeout/HttpRequestException retry semantics belong to the downstream command-lifecycle stories rather than this availability-only story.
status: done 2026-09-02
archived: 2026-10-12
resolution: already resolved: TenantCommandGateway.cs:343-398 and 406-461 now accept caller-owned message IDs and classify ambiguous grant/removal delivery without minting a replacement identity.

### DW-330: Existing grant/remove projection confirmation does not require baseline projection-version advancement or command-specific audit provenance.
origin: spec-deferred b69ce19c6d80
location: src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs:150
source_spec: `spec-4-1-fixed-scope-global-administrator-action-availability.md`
severity: high
reason: The historical downstream command snapshots can accept qualifying target presence/absence without comparing the pre-command projection version; Story 4.1 dispatches no command and owns availability, so confirmation hardening remains follow-up work for the grant/remove lifecycle owners.
status: done 2026-09-02
archived: 2026-10-12
resolution: already resolved: commits 24e3a41d and 2eba0b50; GlobalAdministratorGrantCommandSnapshot.cs:337-379 and GlobalAdministratorRemoveCommandSnapshot.cs:393-447 require exact command-event evidence plus projection-version advancement.

### DW-338: Nothing in this repository pins EventStore's command-status contract, which the grant lifecycle reasons over directly.
origin: spec-deferred 0ab333618257
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs:575
source_spec: `spec-4-2-grant-global-administrator-with-projection-confirmation.md`
severity: low
reason: Every status assertion is fed by a stub. This pass corrected one concrete assumption -- that EventsStored/EventsPublished carry an EventCount -- only by reading AggregateActor and CommandStatusRecord in the submodule. A contract or integration test over a real command-status response would settle the remaining assumptions the same way.
status: done 2026-09-05
archived: 2026-10-12
resolution: already resolved: tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs:403-508 submits real commands and polls /api/v1/commands/status/{correlationId}, deserializing CommandStatusResponse and asserting real terminal statuses at lines 126-149 and 311-364.

### DW-342: Sprint tracking marks Story 4.3 in progress while Epic 4 remains done.

origin: migrated from legacy ledger (""), 2026-09-05
location: _bmad-output/implementation-artifacts/sprint-status.yaml:121-125
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-refresh-dependencies-2.md`
reason: The concurrent unstaged edit changes Story 4.3 from backlog to in-progress without reopening Epic 4, so status consumers can report a completed epic with active work; the Story 4.3 workflow owns the correction.
status: done 2026-09-06
archived: 2026-10-12
resolution: already resolved: _bmad-output/implementation-artifacts/sprint-status.yaml:69-73 marks Epic 4 in-progress while Story 4.3 is review, removing the completed-epic/active-story contradiction.

### DW-348: Spec 5.1 remains in-review with its last operator-handoff task unchecked.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: _bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: The Story 5.1 spec remains in review because its final operator-handoff task for the awaiting-operator performance contract is unchecked; this tracking belongs to Story 5.1 rather than last-administrator removal.
status: done 2026-09-24
archived: 2026-10-12
resolution: already resolved: commit e41a784e4fe870c2290676e60d8aaf6527a76219; spec-5-1-browse-tenant-audit-trail.md:5 is done with every task checked, and story-5-1-performance-evidence.md records the passing v4 run (126/126 percentile groups, 10/10 gates, no fallback).

### DW-103: Redundant double a11y labeling on badges
origin: migrated from legacy ledger ("Deferred from: code review of 1-2-tenant-list-triage-and-cursor-foundation (2026-07-20)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor; src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor
reason: The legacy ledger defers this issue: Redundant double a11y labeling on badges. Original context is preserved in legacy-detail.
legacy-detail: - **Redundant double a11y labeling on badges** — status/pending/truth badges set `IconLabel` **and** container `aria-label` **and** the same visible text; the host `aria-label` subsumes children so `IconLabel` is dead weight today, but if `aria-label` were later removed the icon label would surface a duplicate reading. a11y tidiness. (`src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor`, `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor`)
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor and Components/Shared/TruthStateBadge.razor now use FluentBadge IconStart plus one container aria-label; neither sets IconLabel. The redundant icon-name finding is removed.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor; src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor
evidence-kind: resolved
priority: P3
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.

### DW-126: Lifecycle and global-administrator authorization reflections still read `HttpContext.User` with no circuit fallback, while the new configuration path has one
origin: migrated from legacy ledger ("Deferred from: code review of 1-6-read-only-tenant-configuration.md (2026-07-27)"), 2026-08-25
location: _bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md
source_spec: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md`
reason: The legacy ledger defers this issue: Lifecycle and global-administrator authorization reflections still read `HttpContext.User` with no circuit fallback, while the new configuration path has one. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration.md` summary: Lifecycle and global-administrator authorization reflections still read `HttpContext.User` with no circuit fallback, while the new configuration path has one. evidence: `LifecycleAuthorizationReflection` and `GlobalAdministratorsAuthorizationReflection` resolve `httpContextAccessor?.HttpContext?.User` only; during interactive circuit activity there is no `HttpContext`, so these reflections can disagree with the configuration path for the same user on the same page. Pre-existing before Story 1.6 and outside its declared file scope; Story 1.6 only made the asymmetry visible by adding the circuit-aware path. status: open — pre-existing; fold into the claim-parser consolidation above or into Story 1.10/1.11 identity work.
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor now calls ResolveLifecycleAuthorizationAsync with a replaceable token; GlobalAdministratorsPage.razor and TenantsWorkspace.razor call ResolveGlobalAdministratorsAuthorizationAsync. Repository search finds synchronous reflection property usages only in tests; production consumers use the circuit-aware async seam.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor; src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor; src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.

### DW-165: `TenantAuditPage` is the last production consumer of the synchronous `HttpContext`-only
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review, loop 2 (2026-08-01)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1111; TenantAuditPage.razor:1009
reason: The legacy ledger defers this issue: `TenantAuditPage` is the last production consumer of the synchronous `HttpContext`-only. Original context is preserved in legacy-detail.
legacy-detail: - `TenantAuditPage` is the last production consumer of the synchronous `HttpContext`-only `GlobalAdministratorsAuthorizationReflection`. On an established interactive circuit `HttpContext` is null, so `Evaluate(null)` returns `Indeterminate` and the global-administrator correction affordances at `TenantAuditPage.razor:1009` and `:1017` are permanently unavailable. This is the same defect class the story's transferred decision 3 records as resolved for `TenantDetailPage`. Reason for deferral: the file is not in Story 1.11's File List, and the correct fix depends on how the circuit-only principal-resolution decision is settled — migrating to the async seam alone would not help while that seam also returns `Indeterminate` outside an inbound circuit activity. Revisit if: the resolver decision lands, or Epic 5 audit work reopens the correction path. [src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor:1111]
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor now calls ResolveGlobalAdministratorsAuthorizationAsync for availability, correction opening and proof paths, and subscribes to AuthenticationStateChanged. No production call to the synchronous GlobalAdministratorsAuthorizationReflection property remains.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.

### DW-168: `TenantDetailPage.IsSafeReturnUrl` accepts any string with a `/tenants` prefix — including
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review, loop 2 (2026-08-01)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1173
reason: The legacy ledger defers this issue: `TenantDetailPage.IsSafeReturnUrl` accepts any string with a `/tenants` prefix — including. Original context is preserved in legacy-detail.
legacy-detail: - `TenantDetailPage.IsSafeReturnUrl` accepts any string with a `/tenants` prefix — including `/tenants-anything`, embedded control characters, and unbounded length — while the sibling `GlobalAdministratorsPage.NormalizeReturnUrl` rejects control characters, `\`, `#`, `//`, non-allow-listed query keys and repeated values, and requires an exact canonical round-trip. Reason for deferral: every admitted value stays a same-origin relative path, so there is no redirect or external-return gap today; this is convergence hardening, not a defect. Revisit if: the prefix check is relaxed, or a third return-URL validator appears. [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1173]
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor no longer defines IsSafeReturnUrl; State/TenantAudit/TenantAuditNavigationSafety.cs SafeReturnUrlForTenant canonicalizes allowed routes/keys, rejects unsafe values and strips cursors explicitly. Prefix-only admission was removed.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor; src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditNavigationSafety.cs
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: DW-168, DW-179
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.

### DW-179: IsSafeReturnUrl accepts any /tenants-prefixed path — deferred, already deferred earlier; same-origin relative only
origin: migrated from legacy ledger ("Deferred from: code review of spec-1-11-authorized-global-administrator-review.md (2026-08-08, loop 6 chunk 3)"), 2026-08-25
location: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1250
reason: The legacy ledger defers this issue: IsSafeReturnUrl accepts any /tenants-prefixed path — deferred, already deferred earlier; same-origin relative only. Original context is preserved in legacy-detail.
legacy-detail: - IsSafeReturnUrl accepts any /tenants-prefixed path — deferred, already deferred earlier; same-origin relative only [src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor:1250]
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor no longer defines IsSafeReturnUrl; State/TenantAudit/TenantAuditNavigationSafety.cs SafeReturnUrlForTenant canonicalizes allowed routes/keys, rejects unsafe values and strips cursors explicitly. Prefix-only admission was removed.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor; src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditNavigationSafety.cs
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: DW-168, DW-179
duplicate-of: DW-168
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.

### DW-188: CorrectionStartPanel still submits membership corrections without reusable messageId tracking
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation.md (2026-08-08)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: CorrectionStartPanel still submits membership corrections without reusable messageId tracking. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: CorrectionStartPanel still submits membership corrections without reusable messageId tracking. evidence: Shared gateway now accepts optional messageId, but correction UI was outside Story 2.1 membership-flow File List and still mints a new ULID per attempt.
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor now resolves TenantCorrectionAttemptTracker, retains exact attempts and passes attempt.MessageId to AddUserToTenantAsync and ChangeUserRoleAsync; ambiguous delivery and retry use the same retained identity. The old mint-per-correction behavior is removed.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.

### DW-210: Add runtime FcPageHeader assertions for MyTenants and UserMembershipLookup page chrome
origin: migrated from legacy ledger ("Deferred from: code review of spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md (2026-08-09)"), 2026-08-25
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md`
reason: The legacy ledger defers this issue: Add runtime FcPageHeader assertions for MyTenants and UserMembershipLookup page chrome. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-frontcomposer-fluent-layout-page-layout-conformance-sweep.md` summary: Add runtime FcPageHeader assertions for MyTenants and UserMembershipLookup page chrome. evidence: Surface tests assert panels/layout but not header testids; deleting FcPageHeader would leave those suites green aside from source governance.
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs now asserts tenants-my-page-header; Components/UserMembershipLookupSurfaceTests.cs asserts tenants-user-lookup-page-header on the rendered page. Both missing runtime chrome assertions now exist.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/MyTenantsSurfaceTests.cs; tests/Hexalith.Tenants.UI.Tests/Components/UserMembershipLookupSurfaceTests.cs
evidence-kind: resolved
priority: P3
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.

### DW-232: Optional `messageId` reached create-tenant and update-tenant beyond the declared membership scope
origin: migrated from legacy ledger ("Deferred from: code review of spec-2-1-reverify-projection-confirmed-membership-command-foundation (2026-08-20)"), 2026-08-25
location: messageId
source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md`
reason: The legacy ledger defers this issue: Optional `messageId` reached create-tenant and update-tenant beyond the declared membership scope. Original context is preserved in legacy-detail.
legacy-detail: - source_spec: `_bmad-output/implementation-artifacts/spec-2-1-reverify-projection-confirmed-membership-command-foundation.md` summary: Optional `messageId` reached create-tenant and update-tenant beyond the declared membership scope. evidence: The Code Map scopes optional `messageId` to add/change/remove, but `ITenantCommandGateway.CreateTenantAsync` and `UpdateTenantAsync` also gained `string? messageId = null` and the new ULID-canonicality rejection.
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: _bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md and spec-3-2-edit-tenant-metadata-with-recorded-updates.md explicitly adopt message-id reuse for create/metadata; Services/Gateways/ITenantCommandGateway.cs supplies their optional identity parameter. Later owning stories now declare the earlier out-of-membership-scope change.
verification-files: _bmad-output/implementation-artifacts/spec-3-1-create-tenant-with-projection-confirmation.md; _bmad-output/implementation-artifacts/spec-3-2-edit-tenant-metadata-with-recorded-updates.md; src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs
evidence-kind: resolved
priority: P3
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.

### DW-340: In-progress `spec-refresh-dependencies.md` can overwrite the new EventStore `3.100.0` planning facts.

origin: migrated from legacy ledger (""), 2026-09-02
location: _bmad-output/implementation-artifacts/spec-refresh-dependencies.md
source_spec: `_bmad-output/implementation-artifacts/spec-eventstore-3-100-0.md`
reason: The dependency-refresh spec remained `in-progress` and instructed an agent to reconcile `_bmad-output/project-context.md` after a broader refresh; the source story forbade resuming it but neither closed nor retargeted that work, leaving the recorded EventStore `3.100.0` planning facts at risk of overwrite.
status: done 2026-10-12
archived: 2026-10-12
decision: 2026-09-02 Amend and finish spec — Human-approve an amendment that rebases the frozen dependency intent on current 3.101.0 facts, reconciles remaining work, and closes or completes the spec safely.
decision: 2026-09-02 Amend and finish spec — Human-approve an amendment that rebases the frozen dependency intent on current 3.101.0 facts, reconciles remaining work, and closes or completes the spec safely.
disposition: done
verification-date: 2026-10-12
verification: _bmad-output/implementation-artifacts/spec-refresh-dependencies.md frontmatter is now done; its 2026-10-08 user-authorized continuation and final evidence explicitly retarget EventStore 3.117.0 and supersede historical in-progress blockers. It can no longer be resumed as the frozen 3.100.0 plan without ignoring its completed status.
verification-files: _bmad-output/implementation-artifacts/spec-refresh-dependencies.md
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.

### DW-347: Story 4.3 File List gitlink SHAs do not match the current tree.

origin: code review chunk A 2026-09-06
location: _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md:450
source_spec: `_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
severity: medium
reason: `python3 scripts/validate-story-gitlinks.py` FAIL: tree is Builds d004983, EventStore acf5c4e, FrontComposer 5cbc558, Memories fa8f531, but the File List still states 8db7459 / 070a4b6 / 0922400 / f7fef9f. Later build(deps) advanced the pointers; updating the record edits this spec, restoring the tree reverts later work. Chunk C.
status: done 2026-10-12
archived: 2026-10-12

Chunk A also restated DW-345 (`focusElementById` host vs inner activeElement) as maybe-false; no new ledger row.
disposition: done
verification-date: 2026-10-12
verification: python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md now exits 0 (2026-10-12 inspection run). The artifact records three full Story-Owned Gitlink Commits and their target declarations; the historical mismatched range is reconciled without reverting later pointers.
verification-files: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: DW-363, DW-380, DW-401, DW-404, DW-410, DW-416
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.

### DW-350: Audit list return URLs that contain a cursor are rejected and replaced with the tenant path.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor; src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditSupportSafety.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: TenantAuditPage.SafeReturnUrl validates the whole return URL through TenantAuditSupportSafety.SafeApprovedReference, whose strict fragments include cursor, so legitimate audit-list return URLs containing a cursor are replaced with the tenant path.
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor now uses State/TenantAudit/TenantAuditNavigationSafety.cs SafeReturnUrlForTenant, which drops cursor explicitly, preserves supported list/filter route state and reports partial restoration rather than rejecting the whole return URL. tests/Hexalith.Tenants.UI.Tests/State/TenantAuditNavigationSafetyTests.cs covers cursor-bearing returns.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor; src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditNavigationSafety.cs; tests/Hexalith.Tenants.UI.Tests/State/TenantAuditNavigationSafetyTests.cs
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.

### DW-352: Audit unauthorized and unavailable recovery both navigate to the same BackHref.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: RequestPermission and Escalate both use the tenant or return link, so unauthorized and unavailable audit recovery have no distinct permission-request or support-safe escalation destination.
status: done 2026-10-12
archived: 2026-10-12
decision: 2026-09-06 Configurable destinations — Add explicit support-safe permission and escalation destinations through the UI composition contract, fail closed when absent, and test both navigation paths.
decision: 2026-09-06 Configurable destinations — Add explicit support-safe permission and escalation destinations through the UI composition contract, fail closed when absent, and test both navigation paths.
disposition: done
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor now uses BffComposition.AuditPermissionRecoveryHref and AuditEscalationRecoveryHref through SafeRecoveryHref, and withholds unsafe/missing destinations. tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs Recovery_links_fail_closed_without_safe_composition_destinations covers that contract.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor; tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.

### DW-354: A rejected audit EventId becomes an empty receipt/grid identity instead of failing closed.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRow.cs; src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: TenantAuditRow.FromEntry maps an unsafe EventId to string.Empty, and TenantAuditReceipt.FromEntry uses that mapper, producing an empty receipt or grid identity instead of failing closed.
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRow.cs FromEntry now throws ArgumentException when SafeApprovedReference rejects EventId instead of creating an empty identity; TenantAuditReceipt.FromEntry goes through that mapper and gateway validation rejects unsafe rows.
verification-files: src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRow.cs
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.

### DW-356: A reversed audit From/To range has no page test for field-associated Range validation.

origin: migrated from legacy ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-09-06
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor; tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: TryCreateRequest rejects From greater than To with Tenants.Audit.Filter.Validation.Range, but TenantAuditPageTests never sets From later than To; gateway tests do not cover the page's field errors or its skipped query.
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs Reversed_date_range_marks_both_fields_and_does_not_query sets From after To, asserts both aria-describedby associations, suppresses the grid and verifies no extra query. The requested page-level regression now exists.
verification-files: tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs
evidence-kind: resolved
priority: P3
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.

### DW-361: Opening preview while `_focusGrantLauncherPending` is still set can focus the inert launcher. Unverified medium: settle with a same-turn cancel-then-reopen test that leaves both focus flags set before `OnAfterRenderAsync`. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1335`]

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-07)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Opening preview while `_focusGrantLauncherPending` is still set can focus the inert launcher. Unverified medium: settle with a same-turn cancel-then-reopen test that leaves both focus flags set before `OnAfterRenderAsync`. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1335`]"
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): GlobalAdministratorsPage.razor:3285-3288 explicitly clears _focusGrantLauncherPending when installing a new grant preview before setting _focusGrantAcknowledgementPending. The same-turn stale launcher flag described by the finding no longer survives preview installation.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-363: Story 4.3 File List gitlink SHAs still do not match the current tree. Already DW-347; `validate-story-gitlinks.py` now reports Builds `6daad3d`, EventStore `9a20c05`, FrontComposer `f0c3b6f` versus File List `39debe9` / `7b7f876` / `0a4c4ad`. Updating the record edits this spec; restoring the tree reverts later `build(deps)` work.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-10-12
location: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Story 4.3 File List gitlink SHAs still do not match the current tree. Already DW-347; `validate-story-gitlinks.py` now reports Builds `6daad3d`, EventStore `9a20c05`, FrontComposer `f0c3b6f` versus File List `39debe9` / `7b7f876` / `0a4c4ad`. Updating the record edits this spec; restoring the tree reverts later `build(deps)` work."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): Executed python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md on 2026-10-12: exit 0, RESULT: PASS. The spec now records cf31675b2e860c4be23e22355e361ce977da57c3, a3321266b61fc98f98f700901bd2cc5166eff4b0, and de5784ca751f51a4cfe282f67e11b13dd1ed4b45 under Story-Owned Gitlink Commits with matching declarations. Later committed dependency changes are explicitly outside that ownership range.
verification-files: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-347
duplicate-of: DW-347
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-364: Restore-access correction cannot retain an ambiguous UnableToVerify grant on the aggregate lease.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-06)"), 2026-10-12
location: src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs; src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor; tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Restore-access correction cannot retain an ambiguous UnableToVerify grant on the aggregate lease.\n  evidence: GlobalAdministratorCorrectionSnapshot.ToReconciliation allows correlationless grant only in RequestSent, so a restore snapshot that is already UnableToVerify and ambiguous returns null and cannot be adopted the way removal can. The correction panel never calls RetainAmbiguousPreflight on restore; this is Story 4.2 grant territory, not last-administrator removal."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs:120-151 now retains a complete grant preview with correlationless ambiguous RequestSent or UnableToVerify. GlobalAdministratorCorrectionPanel.razor:1478 calls RetainAmbiguousPreflight for restore access. Components/GlobalAdministratorCorrectionPanelTests.cs::RestoreUnableToVerifyIsRetainedAndAdoptedByReplacementPanel covers retention and adoption.
verification-files: src/Hexalith.Tenants.UI/State/TenantAudit/GlobalAdministratorCorrectionSnapshot.cs; src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor; tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorCorrectionPanelTests.cs
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-373
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-368: After projection refresh, `OpenCorrectionAsync` may assign a re-derived intent without re-checking `IsAvailable`. Unverified medium: settle by showing whether `CorrectionStartPanel` / `GlobalAdministratorCorrectionPanel` can submit when the parent intent is unavailable after refresh.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-1-browse-tenant-audit-trail.md (2026-09-06)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor; src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- After projection refresh, `OpenCorrectionAsync` may assign a re-derived intent without re-checking `IsAvailable`. Unverified medium: settle by showing whether `CorrectionStartPanel` / `GlobalAdministratorCorrectionPanel` can submit when the parent intent is unavailable after refresh."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): Pages/TenantAuditPage.razor::OpenCorrectionAsync re-derives the intent after projection refresh and installs it only when refreshed?.IsAvailable is true (:2124). GlobalAdministratorCorrectionPanel.razor:320 independently disables unavailable intent. TenantAuditPageTests::Pending_correction_open_renders_the_refreshed_unavailable_reason proves no panel is installed after the refreshed intent becomes unavailable.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor; src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-370: Grant status refresh lacks the in-flight gate the remove side has. Pre-existing, not touched by this change: `CanRefreshRemoveStatus` opens with `Volatile.Read(ref _removeSubmissionInFlight) == 0 && _removeAdmissionLease?.IsReconciliationDispatchInFlight != true`, while `CanRefreshGrantStatus` has no equivalent, so a second retry click during an in-flight redispatch re-enters and can divert the returning submission result. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1124`]

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-13)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Grant status refresh lacks the in-flight gate the remove side has. Pre-existing, not touched by this change: `CanRefreshRemoveStatus` opens with `Volatile.Read(ref _removeSubmissionInFlight) == 0 && _removeAdmissionLease?.IsReconciliationDispatchInFlight != true`, while `CanRefreshGrantStatus` has no equivalent, so a second retry click during an in-flight redispatch re-enters and can divert the returning submission result. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:1124`]"
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): GlobalAdministratorsPage.razor::CanRetryAmbiguousGrantDelivery now requires _grantAdmissionLease?.IsReconciliationDispatchInFlight == false and CanRefreshGrantStatus delegates ambiguous retry to it. InitialGrantDeliveryCompletionSurvivesDisposalAndReplacement asserts the replacement grant refresh is disabled while the durable dispatch token is in flight. The specified overlapping ambiguous redispatch entry is gated.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-373: Restore-access correction still arms with `TryMarkDispatched` and cannot retain correlationless grant `RequestSent` the way removal now does. KEEP grant; this story tokenizes removal only. Already recorded as Story 4.2 restore-access lease territory. [`src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:913`]

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Restore-access correction still arms with `TryMarkDispatched` and cannot retain correlationless grant `RequestSent` the way removal now does. KEEP grant; this story tokenizes removal only. Already recorded as Story 4.2 restore-access lease territory. [`src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:913`]"
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): GlobalAdministratorCorrectionPanel.razor:1144-1160 now arms restore-access delivery with TryBeginInitialReconciliationDispatch and a completion token. GlobalAdministratorCorrectionSnapshot.ToReconciliation retains correlationless ambiguous grant RequestSent/UnableToVerify. Tests::RestoreUnableToVerifyIsRetainedAndAdoptedByReplacementPanel and ::Replacing_grant_panel_during_dispatch_retains_exact_request_and_completion cover the former missing behavior.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-364, DW-411
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-375: Grant `EditForm OnSubmit` plus `fluent-button @onclick` can double-enter preview. Grant KEEP launcher, not the removal dialog. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:241`]

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Grant `EditForm OnSubmit` plus `fluent-button @onclick` can double-enter preview. Grant KEEP launcher, not the removal dialog. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:241`]"
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): GlobalAdministratorsPage.razor:289-298 uses @onclick:preventDefault="true" on the fluent-button launcher, and SubmitGrantAsync:3172 acquires _grantPreviewInFlight through Interlocked.CompareExchange before any awaited preview work. IsGrantSubmitDisabled also reads this flag. Form/click double-entry can no longer start two preview compositions.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-376: Grant completion apply has no snapshot fallback when the intent/message/preview `Equals` guard fails. Grant KEEP dispatch; removal already uses `CompleteRemoveSingleFlightAsync`. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:3535`]

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Grant completion apply has no snapshot fallback when the intent/message/preview `Equals` guard fails. Grant KEEP dispatch; removal already uses `CompleteRemoveSingleFlightAsync`. [`src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor:3535`]"
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): GlobalAdministratorsPage.razor::DispatchGrantAsync now publishes a completion through the lease token and synchronizes owned durable reconciliation; ::PublishGrantDeliveryCompletionAsync supplies RetainOrReleaseGrantCompletion when the current snapshot/lease/generation no longer matches. InitialGrantDeliveryCompletionSurvivesDisposalAndReplacement and Superseded_terminal_submission_completion_releases_aggregate_lease inspect exact attempt retention/release. The old intent/message/preview Equals-only apply path is gone.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-409, DW-421
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-379: Correction outer catch may overlay Ambiguous after Accepted delivery. Unverified medium: settle by showing `RefreshStatusCoreAsync` throwing after `TryCompleteReconciliationDispatch` already published Accepted. [`src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:1082`]

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Correction outer catch may overlay Ambiguous after Accepted delivery. Unverified medium: settle by showing `RefreshStatusCoreAsync` throwing after `TryCompleteReconciliationDispatch` already published Accepted. [`src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor:1082`]"
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): GlobalAdministratorCorrectionPanel.razor stores completedIoSnapshot before publishing Accepted and its outer catch now checks completedIoSnapshot first, retaining/releasing that completed result instead of creating an Ambiguous overlay (:1358-1369). Thus a later RefreshStatusCoreAsync exception cannot run the pre-completion ambiguity branch.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-413
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-380: `validate-story-gitlinks.py` FAILs against HEAD: UNDECLARED Commons `6da79ae -> 9f4809d` and PolymorphicSerializations `8aeed1d -> 7e95556`; MISSTATED Builds/EventStore/FrontComposer/Memories vs File List `39debe9` / `7b7f876` / `0a4c4ad` / `f174f9c`. Already DW-347; updating the record edits this spec, restoring the tree reverts later `build(deps)` (Chunk C).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
location: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- `validate-story-gitlinks.py` FAILs against HEAD: UNDECLARED Commons `6da79ae -> 9f4809d` and PolymorphicSerializations `8aeed1d -> 7e95556`; MISSTATED Builds/EventStore/FrontComposer/Memories vs File List `39debe9` / `7b7f876` / `0a4c4ad` / `f174f9c`. Already DW-347; updating the record edits this spec, restoring the tree reverts later `build(deps)` (Chunk C)."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): Executed python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md on 2026-10-12: exit 0, RESULT: PASS. The spec now records cf31675b2e860c4be23e22355e361ce977da57c3, a3321266b61fc98f98f700901bd2cc5166eff4b0, and de5784ca751f51a4cfe282f67e11b13dd1ed4b45 under Story-Owned Gitlink Commits with matching declarations. Later committed dependency changes are explicitly outside that ownership range.
verification-files: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-347
duplicate-of: DW-347
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-385: Add removal availability reason and recovery keys to the removal localization-readiness manifest.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`\n  summary: Add removal availability reason and recovery keys to the removal localization-readiness manifest.\n  evidence: The removal evaluator can return keys outside `RequiredRemoveFactKeys`, so a missing translation can pass readiness; removal behavior is explicitly excluded from the grant intent."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): TenantsBffComposition.cs::RequiredRemoveFactKeys now lists the removal availability reasons and recoveries, including LastAdministrator, MissingConsequencePreview, and TargetMissing. TenantsBffCompositionTests::RemovalPreviewReadinessAndSelfRemovalHoldAgainstShippedEnglishAndFrenchResources pins the canonical list and checks real EN/FR strings; ::RemovalPreviewFailsClosedWhenAnAvailabilityResourceIsUnresolved covers omission.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-389: Determine whether remove-dialog JS interop can surface cancellation exceptions during browser teardown.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`\n  summary: Determine whether remove-dialog JS interop can surface cancellation exceptions during browser teardown.\n  evidence: This is maybe-false; reproduce `OperationCanceledException` or `TaskCanceledException` during module import, invocation, or disposal to decide whether additional containment is needed."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): GlobalAdministratorsPage.razor::FocusElementByIdSafelyAsync and ::DisposeFocusModuleSafelyAsync now explicitly catch OperationCanceledException, covering import/invocation/disposal teardown. GlobalAdministratorsPageTests::RemoveStartSentinelContainsRealFocusInteropFailure and ::RemoveFocusModuleDisposalContainsCircuitTeardown include TaskCanceledException data.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-392: Update the quickstart for generated local Keycloak credentials.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: docs/quickstart.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-2-grant-global-administrator-with-projection-confirmation.md`\n  summary: Update the quickstart for generated local Keycloak credentials.\n  evidence: The realm now reads generated username/password parameters while the quickstart still submits `admin-user` and `admin-pass`, so the documented token request fails in the current local topology."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): docs/quickstart.md:118-134 now states that local service administrator credentials are generated per run, explicitly retires the admin-user/admin-pass pair for token requests, prompts for LOCAL_PLATFORM_USERNAME/LOCAL_PLATFORM_PASSWORD, and submits them using --data-urlencode. This matches AppHost/Program.cs generated realm parameters.
verification-files: docs/quickstart.md
evidence-kind: resolved
priority: P3
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-392, DW-398
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-398: Update the quickstart for generated local Keycloak credentials.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: docs/quickstart.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Update the quickstart for generated local Keycloak credentials.\n  evidence: The realm now provisions generated per-run credentials while `docs/quickstart.md` still submits `admin-user` and `admin-pass`, so the documented token request fails against the default local topology."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): docs/quickstart.md:118-134 now states that local service administrator credentials are generated per run, explicitly retires the admin-user/admin-pass pair for token requests, prompts for LOCAL_PLATFORM_USERNAME/LOCAL_PLATFORM_PASSWORD, and submits them using --data-urlencode. This matches AppHost/Program.cs generated realm parameters.
verification-files: docs/quickstart.md
evidence-kind: resolved
priority: P3
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-392
duplicate-of: DW-392
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-399: Reject blank and repeated continuation cursors in tenant-audit payload validation.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-2-grant-global-administrator-with-projection-confirmation.md (2026-09-22)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Reject blank and repeated continuation cursors in tenant-audit payload validation.\n  evidence: `TenantQueryGateway.IsValidTenantAuditPayload` validates rows but not the `HasMore`/cursor relationship or equality with the requested cursor, so an enabled Next action can no-op or loop on the same page."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): TenantQueryGateway.cs::IsValidTenantAuditPayload:2796-2803 now rejects HasMore/cursor mismatch, empty continuation pages, and repeated requested cursor. TenantQueryGatewayTests::Get_tenant_audit_rejects_inconsistent_next_cursor_metadata and ::Get_tenant_audit_rejects_nonadvancing_or_empty_continuation_pages cover the specified cursor cases.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantQueryGateway.cs
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-401: Story gitlink guard still fails against HEAD. Already DW-347. Current tree is Builds `2fba349`, Commons `9f4809d` undeclared, EventStore `db1e9d7`, FrontComposer `00d4da4`, Memories `8884933`, PolymorphicSerializations `7e95556` undeclared, while the File List still states Builds `39debe9`, EventStore `7b7f876`, FrontComposer `0a4c4ad`, and Memories `f174f9c`. This slice does not move `references/`. Declaring edits the spec; restoring the tree reverts later `build(deps)` work.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
location: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Story gitlink guard still fails against HEAD. Already DW-347. Current tree is Builds `2fba349`, Commons `9f4809d` undeclared, EventStore `db1e9d7`, FrontComposer `00d4da4`, Memories `8884933`, PolymorphicSerializations `7e95556` undeclared, while the File List still states Builds `39debe9`, EventStore `7b7f876`, FrontComposer `0a4c4ad`, and Memories `f174f9c`. This slice does not move `references/`. Declaring edits the spec; restoring the tree reverts later `build(deps)` work."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): Executed python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md on 2026-10-12: exit 0, RESULT: PASS. The spec now records cf31675b2e860c4be23e22355e361ce977da57c3, a3321266b61fc98f98f700901bd2cc5166eff4b0, and de5784ca751f51a4cfe282f67e11b13dd1ed4b45 under Story-Owned Gitlink Commits with matching declarations. Later committed dependency changes are explicitly outside that ownership range.
verification-files: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-347
duplicate-of: DW-347
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-403: Validate the Release/NuGet package graph in CI rather than relying on the Debug source-reference lane.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22)"), 2026-10-12
location: references/Hexalith.Builds/.github/workflows/domain-ci.yml
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md`\n  summary: Validate the Release/NuGet package graph in CI rather than relying on the Debug source-reference lane.\n  evidence: `.github/workflows/source-reference.yml` explicitly builds and tests with `--configuration Debug -p:UseHexalithProjectReferences=true`, so it cannot expose packaging-only failures required by the repository CI policy; this workflow is later baseline-range integration work, not Story 4.3 removal behavior."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): .github/workflows/ci.yml delegates to Hexalith.Builds/domain-ci.yml with run-consumer-validation: true. The checked-in reusable workflow builds Release, packs, and invokes package consumer validation; PackageGovernanceTests::Ci_workflow_runs_package_consumer_validation_after_release_build_and_pack pins that configuration. The separate Debug source lane no longer constitutes the only CI graph evidence.
verification-files: references/Hexalith.Builds/.github/workflows/domain-ci.yml
evidence-kind: resolved
priority: P3
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-404: Story gitlink guard still fails against HEAD (`scripts/validate-story-gitlinks.py` exit 1). Already DW-347.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A removal core)"), 2026-10-12
location: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Story gitlink guard still fails against HEAD (`scripts/validate-story-gitlinks.py` exit 1). Already DW-347."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): Executed python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md on 2026-10-12: exit 0, RESULT: PASS. The spec now records cf31675b2e860c4be23e22355e361ce977da57c3, a3321266b61fc98f98f700901bd2cc5166eff4b0, and de5784ca751f51a4cfe282f67e11b13dd1ed4b45 under Story-Owned Gitlink Commits with matching declarations. Later committed dependency changes are explicitly outside that ownership range.
verification-files: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-347
duplicate-of: DW-347
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-405: `RequiredGrantFactKeys` has no guard proving rendered grant keys are listed and present in EN/FR. Already deferred as KEEP grant localization for Story 4.2.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A removal core)"), 2026-10-12
location: n/a
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- `RequiredGrantFactKeys` has no guard proving rendered grant keys are listed and present in EN/FR. Already deferred as KEEP grant localization for Story 4.2."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): TenantsBffCompositionTests::GrantPreviewReadinessCoversEveryFactKeyARealPreviewRenders enumerates rendered fact/chrome/availability keys against the 104-key manifest; ::GrantPreviewReadinessHoldsAgainstTheShippedEnglishAndFrenchResources resolves the real EN/FR localizer, and ::GrantPreviewRequiresEveryExplicitEnglishAndFrenchResource exercises omissions.
evidence-kind: resolved
priority: P3
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-377
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-410: Story gitlink guard still fails against HEAD (`scripts/validate-story-gitlinks.py` exit 1: Commons and PolymorphicSerializations undeclared; Builds/EventStore/FrontComposer/Memories SHAs misstated). Already DW-347.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A production File List)"), 2026-10-12
location: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Story gitlink guard still fails against HEAD (`scripts/validate-story-gitlinks.py` exit 1: Commons and PolymorphicSerializations undeclared; Builds/EventStore/FrontComposer/Memories SHAs misstated). Already DW-347."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): Executed python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md on 2026-10-12: exit 0, RESULT: PASS. The spec now records cf31675b2e860c4be23e22355e361ce977da57c3, a3321266b61fc98f98f700901bd2cc5166eff4b0, and de5784ca751f51a4cfe282f67e11b13dd1ed4b45 under Story-Owned Gitlink Commits with matching declarations. Later committed dependency changes are explicitly outside that ownership range.
verification-files: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-347
duplicate-of: DW-347
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-411: Grant delivery uses the removal lease-token API, correction restore still uses `TryMarkDispatched`, and `RequiredGrantFactKeys` expanded. KEEP grant / Story 4.2; already recorded on the earlier 2026-09-22 Group A pass and the iteration-6 restore-access deferral.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A production File List)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Grant delivery uses the removal lease-token API, correction restore still uses `TryMarkDispatched`, and `RequiredGrantFactKeys` expanded. KEEP grant / Story 4.2; already recorded on the earlier 2026-09-22 Group A pass and the iteration-6 restore-access deferral."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): GlobalAdministratorCorrectionPanel.razor now tokenizes initial grant restore delivery through TryBeginInitialReconciliationDispatch and TryCompleteReconciliationDispatch; GlobalAdministratorsPage uses the same durable grant token. RequiredGrantFactKeys now has 104 keys with rendered-key/resource tests. The old correction restore TryMarkDispatched asymmetry is removed.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-364, DW-373, DW-405
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-413: Correction outer catch can overlay Ambiguous after Accepted if `RefreshStatusCoreAsync` throws. Maybe-false; settle by showing that throw after `TryCompleteReconciliationDispatch` already published Accepted. Already recorded on iteration 6.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-22, Group A production File List)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Correction outer catch can overlay Ambiguous after Accepted if `RefreshStatusCoreAsync` throws. Maybe-false; settle by showing that throw after `TryCompleteReconciliationDispatch` already published Accepted. Already recorded on iteration 6."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): GlobalAdministratorCorrectionPanel.razor::SubmitAsync records completedIoSnapshot before publishing the grant completion, and the outer catch retains/reconciles that known result before its Ambiguous branch. A later refresh exception therefore cannot overwrite an already completed Accepted delivery with ambiguity.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-379
duplicate-of: DW-379
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-416: Story gitlink guard still fails (`scripts/validate-story-gitlinks.py` exit 1). Already DW-347.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- Story gitlink guard still fails (`scripts/validate-story-gitlinks.py` exit 1). Already DW-347."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): Executed python3 scripts/validate-story-gitlinks.py _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md on 2026-10-12: exit 0, RESULT: PASS. The spec now records cf31675b2e860c4be23e22355e361ce977da57c3, a3321266b61fc98f98f700901bd2cc5166eff4b0, and de5784ca751f51a4cfe282f67e11b13dd1ed4b45 under Story-Owned Gitlink Commits with matching declarations. Later committed dependency changes are explicitly outside that ownership range.
verification-files: scripts/validate-story-gitlinks.py; _bmad-output/implementation-artifacts/spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-347
duplicate-of: DW-347
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-423: Sanitize URL-supplied user and command context before showing it on the tenant audit page.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`\n  summary: Sanitize URL-supplied user and command context before showing it on the tenant audit page.\n  evidence: `TenantAuditPage.ContextText` renders `targetUserId` or `supportSafeCommandReference` query values without the audit support-safety classifier. This contextual entry-point behavior predates the current Story 5.1 patch."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): TenantAuditPage.razor::ContextText now renders SafeTargetUserId or SafeCommandReference instead of raw query values, and uses GlobalAdministratorIdentityDisplay.Encode for the approved target. TenantAuditPageTests::RestoredAuditContextRejectsUnsafeIdentity and ::Tenant_audit_page_keeps_the_safe_reference_but_omits_unsafe_narrative_context_and_correction cover unsafe contexts.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-425: Recheck audit load generation inside the queued tenant-detail projection write.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`\n  summary: Recheck audit load generation inside the queued tenant-detail projection write.\n  evidence: `TenantAuditPage.RefreshTenantProjectionForLoadAsync` checks `CanApply` before `InvokeAsync` only; a route change between those operations can replace the new route's supplementary correction evidence with the old tenant's detail snapshot. This projection path predates the current patch."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): TenantAuditPage.razor::RefreshTenantProjectionForLoadAsync now checks CanApply(tenantId, generation) and projectionGeneration inside both queued InvokeAsync writes (:2467 and :2485), before clearing/installing captures or refreshing active intents. The pre-dispatch-only guard described by the finding is gone.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-426, DW-442
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-426: Invalidate an in-flight correction open when the audit page changes tenant routes.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`\n  summary: Invalidate an in-flight correction open when the audit page changes tenant routes.\n  evidence: `TenantAuditPage.OnParametersSetAsync` clears paging and projections on tenant change without incrementing `_correctionOpenGeneration`; a pending `OpenCorrectionAsync` can subsequently install its old-tenant intent. This later correction workflow predates the current audit-read patch."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): TenantAuditPage.razor::ClearPaging now increments _correctionOpenGeneration (:1608); route changes call ClearPaging. OpenCorrectionAsync additionally captures tenantId/loadGeneration and checks CanApply inside the installation callback (:2114), independently rejecting an old-tenant result.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-425, DW-442
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-427: Revoke or refresh an open correction intent when its audit evidence degrades.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail.md`\n  summary: Revoke or refresh an open correction intent when its audit evidence degrades.\n  evidence: `TenantAuditPage.ResolveReceiptSelection` updates the selected receipt after a degraded read but can leave `_activeCorrectionIntent` and a submission-ready child panel intact. The correction lifecycle predates the current audit-read patch."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): TenantAuditPage.razor::ResolveReceiptSelection calls CaptureCorrectionAuthority, which locates the original row and requires TenantAuditReceipt.FromRow(..., surfaceKind: snapshot.Kind).State == Ready. An unsubmitted stale/missing source increments open generation, clears active/preview intents, and schedules focus. Submitted/retained attempts intentionally keep lifecycle recovery. TenantAuditPageTests::Invalidated_correction_with_an_open_receipt_falls_back_to_the_receipt_heading covers invalidation.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-430: Resolve whether the historical Story 5.1 performance guest used modified dependency source.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-4-3-remove-global-administrator-with-last-administrator-hard-stop.md (2026-09-23, Group B1 page tests)"), 2026-10-12
location: _bmad-output/implementation-artifacts/story-5-1-performance-evidence.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail-2.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-1-browse-tenant-audit-trail-2.md`\n  summary: Resolve whether the historical Story 5.1 performance guest used modified dependency source.\n  evidence: The archived source patch marks EventStore, FrontComposer, and Memories gitlinks dirty without their nested status or diffs. A clean dependency checkout or preserved nested status/diffs on the next dedicated run would settle whether those markers represented source changes or generated output."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): story-5-1-performance-evidence.md explicitly preserves the v3 dirty-source uncertainty and excludes v3 from acceptance. Its accepted clean v4 rerun uses commit 697cdb58c33ddeb94690d8e5a5716da407dae4bf; story-5-1-performance-v4-authoritative-2026-09-24/source-status.txt records clean root/root-declared submodules, and summary.json/runner-output.txt supply replacement full-contract evidence. The exact old v3 dirt is not asserted to be known; the practical evidence gap was closed by a clean accepted run.
verification-files: _bmad-output/implementation-artifacts/story-5-1-performance-evidence.md
evidence-kind: resolved
priority: P3
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-433: Verify focus restoration does not report a false missing origin when the page replaces its own URL mid-wait (unverified, would be medium).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-2-reach-scoped-audit-evidence-from-context.md (2026-09-26)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`\n  summary: Verify focus restoration does not report a false missing origin when the page replaces its own URL mid-wait (unverified, would be medium).\n  evidence: `tenantsFocus.js` `check()` and its 250 ms interval call `finish(false)` on any `href` change, including same-route canonicalization or a `ListRefreshed` recovery `NavigateTo`. A browser trace of an audit return whose Back URL differs from the canonical workspace URL would settle it."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): tenantsFocus.js::restoreAuditFocus/restoreDetailFocus now uses equivalentOrigin to accept same-origin canonicalization and returns cancelled rather than a false missing result on actual route changes. Browser/tenants-focus-browser-validation.html:526-535 explicitly changes a lookup URL to canonical query form during a held focus wait and asserts the delayed exact launcher is focused.
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-435: Invalidate retained tenant audit rows and an open receipt when the caller changes in the same UI circuit.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-2-reach-scoped-audit-evidence-from-context.md (2026-09-26)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-2-reach-scoped-audit-evidence-from-context.md`\n  summary: Invalidate retained tenant audit rows and an open receipt when the caller changes in the same UI circuit.\n  evidence: `TenantAuditPage` has no authentication-state subscription and retains `_snapshot` and `_selectedReceipt` on a same-route caller change. The same behavior exists at the Story 5.2 baseline in Story 5.1's audit page; a caller-switch test and a page-lifecycle fix belong with that earlier read surface."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): TenantAuditPage.razor now subscribes to AuthenticationStateProvider.AuthenticationStateChanged, immediately marks a transition pending and increments load/open/proof generations, then clears rows, receipt/correction/proof state and replaces subscriptions before reauthorization (:664-737). TenantAuditPageTests::Principal_change_clears_off_page_global_receipt_and_pair_before_reauthorization and ::Authentication_transition_replaces_system_read_subscription cover same-circuit changes.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-442: Bind an in-flight correction open to the tenant route that initiated it.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-28)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`\n  summary: Bind an in-flight correction open to the tenant route that initiated it.\n  evidence: `OpenCorrectionAsync` captures only `_correctionOpenGeneration`; a tenant route change clears page state without incrementing that generation, so a late projection result can reuse a same-reference row from the new tenant. This behavior predates Story 5.3."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): TenantAuditPage.razor::OpenCorrectionAsync captures tenantId and loadGeneration, then checks CanApply(tenantId, loadGeneration) inside the installation callback alongside open/projection generations. ClearPaging increments _correctionOpenGeneration on route change. An old tenant result cannot install against the new route.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-425, DW-426
duplicate-of: DW-426
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-443: Restore visible focus when a viewport downgrade removes an active correction panel.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-28)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`\n  summary: Restore visible focus when a viewport downgrade removes an active correction panel.\n  evidence: `RenderViewportChangeAsync` clears `_activeCorrectionIntent` and its focus reference without scheduling a return to the launcher. The panel-removal behavior predates Story 5.3."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): TenantAuditPage.razor::RenderViewportChangeAsync now calls ScheduleCorrectionFocus(_activeCorrectionFocusReference, _activeCorrectionOrigin) before clearing an active unsubmitted start/preview (:801-809). It captures receipt-only focus before rerender and guards submitted/retained attempts. TenantAuditPageTests::NarrowViewportReturnsFocusedReceiptCorrectionToItsHeadingWithoutAnOpenPanel and ::NarrowViewportIgnoresStaleCorrectionReferenceAfterRoleSelectionClosesPanel cover focus handoffs.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: DW-444, DW-445
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-445: Return correction focus to the control that launched the panel, not the first matching reference in document order.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-3-view-a-support-safe-audit-evidence-receipt.md (2026-09-29)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-3-view-a-support-safe-audit-evidence-receipt.md`\n  summary: Return correction focus to the control that launched the panel, not the first matching reference in document order.\n  evidence: `tenantsFocus.js` `focusCorrectionLauncher` searches `[data-correction-focus-reference]` and then `[data-receipt-focus-reference]` in document order. The grid renders before the receipt (`TenantAuditPage.razor:221`, `:265`), so a panel opened from the receipt's Start correction returns focus to the grid's matching button. The fallback added in `38da46a6` likewise prefers the grid's \"View receipt\" over the open receipt. A fix needs the launch origin threaded from `OpenCorrectionAsync` to the JS."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): TenantAuditPage.razor separates grid/receipt starts with OpenCorrectionAsync(intent, "grid"/"receipt"), stores _activeCorrectionOrigin, and passes origin to focusCorrectionLauncher. tenantsFocus.js filters data-correction-origin; Browser/tenant-correction-start-browser-validation.html asserts exact grid and receipt launcher restoration. TenantAuditPageTests::ClosingReceiptLaunchedStartReturnsFocusToReceiptLauncher covers the receipt-specific caller.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-452: Hand off to Hexalith.Builds. The G-6 validator tuple pins CommunityToolkit.Aspire.Hosting.Dapr `13.5.1-beta.767`, while the central catalog pins `.770`.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 3)"), 2026-10-12
location: references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py; references/Hexalith.Builds/Tools/runtime_toolchain_v2.py
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Hand off to Hexalith.Builds. The G-6 validator tuple pins CommunityToolkit.Aspire.Hosting.Dapr `13.5.1-beta.767`, while the central catalog pins `.770`.\n  evidence: At Builds `85ca19bc`, `Tools/validate-runtime-toolchain-evidence.py:21` says `.767` and `Props/Directory.Packages.props:151` says `.770`, so a real `validate_baseline` run fails with central package pin drift. The self-test (`test-runtime-toolchain-evidence-validator.py:90-101`) builds its catalog from `EXPECTED_TUPLE` and never reads the real props, so Builds CI stays green. No Tenants workflow runs this validator."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py no longer defines EXPECTED_TUPLE or a single .767 pin: it validates exact tuple values from the supplied dated baseline; runtime_toolchain_v2.py adds current baseline support. Historical .757/.767 packets are explicitly superseded against later catalogs. Builds README.md:682-706 documents current catalog drift separately, so the original hard-coded validator/catalog inconsistency is removed.
verification-files: references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py; references/Hexalith.Builds/Tools/runtime_toolchain_v2.py
evidence-kind: resolved
priority: P3
owner: Hexalith.Builds
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-453: Hand off to Hexalith.Builds. Correct the false and stale claims in the 6-1 G-4/G-6 story record.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 3)"), 2026-10-12
location: references/Hexalith.Builds/Tools/runtime-toolchain-baseline.json
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Hand off to Hexalith.Builds. Correct the false and stale claims in the 6-1 G-4/G-6 story record.\n  evidence: `6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md:2379` says the 2026-09-06 baseline \"remains byte-identical at SHA-256 `525615c6…`\", but `Tools/runtime-toolchain-baseline.json` hashes to `62a6a555…` at both `0610f783` and `85ca19bc` (commit `aada815` rewrote it). Line 2385 claims catalog alignment. The \"review the fresh packet\" and \"seven bound changes remain uncommitted\" statements are stale."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): references/Hexalith.Builds/_bmad-output/implementation-artifacts/6-1-p0-deliver-g4-persisted-runner-and-evidence-tooling.md:2385 now marks the rebase note corrected and explains both rewritten hashes; :2391-2402 restores the bound historical baselines, supersedes the pending packet, and preserves pending owner acceptance. sha256sum of Tools/runtime-toolchain-baseline.json executed 2026-10-12 returns 525615c65ada8cabf5a6911a374bb22b62f6a3c1bfa6c91f7245312da9720265, matching the restored bound bytes. Original dated observations remain history rather than silently overwritten.
verification-files: references/Hexalith.Builds/Tools/runtime-toolchain-baseline.json
evidence-kind: resolved
priority: P3
owner: Hexalith.Builds
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-454: Hand off to Hexalith.Builds. The historical 2026-09-06 G-6 baseline and packet no longer validate, and the README still points at them.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 3)"), 2026-10-12
location: references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Hand off to Hexalith.Builds. The historical 2026-09-06 G-6 baseline and packet no longer validate, and the README still points at them.\n  evidence: `validate-runtime-toolchain-evidence.py:240` requires `approvedOn == \"2026-09-27\"`, and the schema pins the same date, so earlier evidence fails \"Baseline approval date drift\". `README.md:269` still names `Tools/runtime-toolchain-baseline.json` as the recorded exception. Keep a dated-baseline registry, or retire the old file explicitly."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): Builds/Tools/validate-runtime-toolchain-evidence.py::baseline_approval_date now validates each baseline date rather than hard-coding 2026-09-27; README.md:682-706 defines the dated-baseline registry and says historical packets do not provide current qualification. The Builds 6-1 record:2391-2397 documents restored historical hashes and current catalog drift as a separate reason historical packets fail.
verification-files: references/Hexalith.Builds/Tools/validate-runtime-toolchain-evidence.py
evidence-kind: resolved
priority: P3
owner: Hexalith.Builds
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-455: Hand off to Hexalith.Builds. G-6 candidate and accepted evidence cannot be told apart.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-09-29, review 3)"), 2026-10-12
location: references/Hexalith.Builds/Tools/runtime_toolchain_v2.py; references/Hexalith.Builds/Tools/test_runtime_toolchain_v2.py
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Hand off to Hexalith.Builds. G-6 candidate and accepted evidence cannot be told apart.\n  evidence: `validate-runtime-toolchain-evidence.py:307-308` distinguishes the modes only by `packet[\"status\"]`, and no named acceptance record (who, when, pending-packet hash) is required. The schema's `status` is `enum [\"pending\",\"accepted\"]` with no mode condition. The 2026-09-27 baseline carries `approvedBy`/`approvedOn` although the record calls it a candidate pin."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by current source/artifact inspection (tests cited were inspected, not rerun): references/Hexalith.Builds/Tools/runtime_toolchain_v2.py:525-535 now requires pending candidates to have acceptance=None and usableAsPrerequisite=False; accepted v2 packets must carry approvedBy/approvedAtUtc/decision/reviewedPacketSha256 and bind the reconstructed pending packet hash with post-capture approval. Tools/test_runtime_toolchain_v2.py exercises pending-acceptance and named accepted-packet paths. The weaker v1 schema is retained for historical evidence, not current acceptance.
verification-files: references/Hexalith.Builds/Tools/runtime_toolchain_v2.py; references/Hexalith.Builds/Tools/test_runtime_toolchain_v2.py
evidence-kind: resolved
priority: P3
owner: Hexalith.Builds
dependencies: none
next-action: No remaining action for this finding.
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-463: Restore CI test execution. `ci / build-and-test` fails at "Validate package consumer references", so no Tenants test tier has run in CI since at least 2026-09-29.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery-3.md (2026-09-30)"), 2026-10-12
location: scripts/validate-nuget-packages.py; _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`\n  summary: Restore CI test execution. `ci / build-and-test` fails at \"Validate package consumer references\", so no Tenants test tier has run in CI since at least 2026-09-29.\n  evidence: \"Package validation failed: Hexalith.Tenants.Server.0.0.0-ci-test.nupkg: dependency boundary includes host, samples, tests, or other forbidden projects: ['Hexalith.EventStore.ServiceDefaults']\". This comes from the reusable `domain-ci.yml:350` step, after which Tier 1 and Tier 2 report no failure count. `Hexalith.Tenants.Server.csproj:6` references `Hexalith.EventStore.Server` from source at the bumped EventStore gitlink. First seen in run `36590317356` (`e077e65e`). It is still red on `main` (`36747920099`, `a4a1ce13`), and PR #49 was merged red. The fix is either in the EventStore package graph or in the boundary allow-list; decide which, then bump the gitlink."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: scripts/validate-nuget-packages.py:35-38 now explicitly allows exact Hexalith.EventStore.ServiceDefaults and Hexalith.Commons.ServiceDefaults IDs. .github/workflows/ci.yml includes UI Tier 1; spec-5-7-correct-global-administrator-authority-from-audit-evidence.md records a later build-and-test success at 6b6a35f0. The specific package-boundary-before-tests blocker is removed; this does not assert current hosted job health. Current-head CI run 38269521776 at 7a2a126f confirms build-and-test job 114880391875 succeeds; the independent advisory Aspire failure is tracked separately.
verification-files: scripts/validate-nuget-packages.py; _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md
evidence-kind: resolved
priority: P1
owner: unassigned
dependencies: none
next-action: none
related: DW-484,DW-495,DW-516
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-465: Let the browser harness run against the local Debug build, so local UI evidence can be reproduced from the repository.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery-3.md (2026-09-30)"), 2026-10-12
location: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh; .github/workflows/story-guards.yml
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md`\n  summary: Let the browser harness run against the local Debug build, so local UI evidence can be reproduced from the repository.\n  evidence: `validate-tenants-focus-browser.sh:8-11` hardcodes four `obj/Release/net10.0/scopedcss` inputs, while local work is Debug-only. Story 5.4's Chrome 154 evidence came from an uncommitted `/tmp/story-5-4-browser-debug.sh` copy with Debug paths. A configuration variable that defaults to Release would keep CI unchanged."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh:7-19 selects TENANTS_BROWSER_BUILD_CONFIGURATION and defaults to Debug when Debug assets exist; all scoped CSS inputs use that variable. .github/workflows/story-guards.yml supplies Release explicitly. The committed validator can consume fresh local Debug CSS.
verification-files: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh; .github/workflows/story-guards.yml
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: DW-476
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-476: Let the existing browser validator consume the selected local build configuration instead of hardcoding Release CSS paths.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 7)"), 2026-10-12
location: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh; .github/workflows/story-guards.yml
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Let the existing browser validator consume the selected local build configuration instead of hardcoding Release CSS paths.\n  evidence: Build completion review BH8 verified that Release-only correction, receipt, and page paths already exist at baseline 55f3dc63b6ce10bb0afdf929d026fa07cffd9105. The local Debug acceptance lane needs a temporary adapter to consume fresh CSS; no build-selection policy was changed by Story 5.4."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh:7-19 selects TENANTS_BROWSER_BUILD_CONFIGURATION and defaults to Debug when Debug assets exist; all scoped CSS inputs use that variable. .github/workflows/story-guards.yml supplies Release explicitly. The committed validator can consume fresh local Debug CSS.
verification-files: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh; .github/workflows/story-guards.yml
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: DW-465
duplicate-of: DW-465
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-477: Preserve and print Chromium stderr when the browser validator exits before its validation-result checks.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-4-understand-audit-availability-and-recovery.md (2026-10-01, review 7)"), 2026-10-12
location: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery.md`\n  summary: Preserve and print Chromium stderr when the browser validator exits before its validation-result checks.\n  evidence: Build completion review BH9 traced the pre-existing set -e browser invocation and EXIT cleanup. A nonzero browser process exit bypasses stderr printing and the trap deletes the redirected diagnostic file. This behavior is present at baseline 55f3dc63b6ce10bb0afdf929d026fa07cffd9105."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh:266-278 wraps every Chromium invocation with a bounded if/else and prints redirected stderr before returning a nonzero exit; all scenarios call run_chromium. The former set-e bypass no longer suppresses failure diagnostics.
verification-files: tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: DW-464,DW-555
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-481: Recheck current circuit authority and membership immediately before tenant correction confirmation dispatch (high).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`\n  summary: Recheck current circuit authority and membership immediately before tenant correction confirmation dispatch (high).\n  evidence: R-B6; the unchanged Story 5.6 CorrectionStartPanel.SubmitAsync dispatches its stored snapshot directly. Story 5.5 refreshes start and handoff and performs no dispatch; a confirmation-time authority loss remains outside those start gates.\n  resolution: Resolved by Story 5.6 commit `17538e07`; Confirm now performs a fresh authorized projection read before admission."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: CorrectionStartPanel.razor:771-777 refreshes authorized capture before renderer admission, and AdmitAndDispatchAsync revalidates it. CorrectionStartPanelTests includes Panel_confirm_time_authority_loss_refuses_dispatch. Existing resolution commit 17538e07 is preserved.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
evidence-kind: resolved
priority: P1
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-482: Decide and expose redacted last-owner consequence facts in tenant correction previews (medium).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs; src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`\n  summary: Decide and expose redacted last-owner consequence facts in tenant correction previews (medium).\n  evidence: R-B7; the baseline preview contains no owner count or explicit last-owner warning. The original review assumed a last-owner domain rejection; ChangeUserRole permits last-owner demotion, so the correction must warn about potentially leaving no owner, matching the member flow.\n  resolution: Resolved by Story 5.6 commit `93b0b96d`; the authorized capture carries a redacted owner count. The follow-up preview copy warns that demoting the last owner can leave no owner and that the command is not blocked, matching the member flow and domain behavior."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: TenantsBffComposition.cs:562-573 emits redacted OwnerCount after current authorization; CorrectionStartPanel.razor:438-452 renders the last-owner consequence. CorrectionStartPanelTests:467 pins “Demoting the last owner can leave this tenant with no owner”; the domain still permits that demotion. Existing resolution 93b0b96d is preserved.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs; src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-483: A submitted, tracked tenant-correction preview is still unmounted by refresh, list-refresh, row loss and viewport narrowing (pre-existing).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`\n  summary: A submitted, tracked tenant-correction preview is still unmounted by refresh, list-refresh, row loss and viewport narrowing (pre-existing).\n  evidence:\n  - These paths in `TenantAuditPage.razor` clear `_previewCorrectionIntent` without checking `HasSubmittedCorrection`:\n    - `CaptureCorrectionAuthority` (`:1653`), when the source row is absent or not Ready, for example after the corrective event pushes it off page 1.\n    - `ClearPaging` (`:1407`), on `ListRefreshed`/`InvalidCursor`.\n    - The viewport handler (`:645`).\n  - The preview is also rendered inside the `ShouldRenderRows` block (`:214`), so a Loading snapshot unmounts it.\n  - The operator loses the lifecycle, tracking handle and proof link of a dispatched command.\n  - At baseline `bcfc0788` the same paths unmounted the submitting `CorrectionStartPanel`. Commit `f44e19e7` retains submitted previews only on the new role and start paths.\n  - Story 5.6 owns preview lifecycle retention. The Story 5.5 review settled the \"submitted\" signal as `CorrectionStartPanel.HasSubmitted` (set in `SubmitAsync`, never cleared). It is scoped to one panel instance, so any unmount (including these paths and Cancel) loses it; retention needs a page-owned signal.\n  resolution: Resolved by Story 5.6 commit `17538e07`; the circuit-scoped correction attempt tracker retains the submitted snapshot and MessageId across remounts."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: TenantCorrectionAttemptTracker stores admitted snapshots and MessageIds; TenantAuditPage.razor:251-271 renders retained previews outside the row grid and exposes resume on remount. TenantAuditPageTests.Pending_correction_can_resume_after_cancel_row_loss_and_narrow_viewport and Remount_restores_pending_attempt_on_a_stale_authorized_audit_surface cover recovery. Existing resolution 17538e07 is preserved.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: resolved
priority: P1
owner: unassigned
dependencies: none
next-action: none
related: DW-485
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-484: CI still runs no Tenants test tier (already tracked above; reconfirmed).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
location: scripts/validate-nuget-packages.py; _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`\n  summary: CI still runs no Tenants test tier (already tracked above; reconfirmed).\n  evidence: Run 36970095241 on `09c90f08` (`main`, after the PR #51 merge) fails `ci / build-and-test` at \"Validate package consumer references\". Aspire and performance tests are skipped. All Story 5.5 test evidence is local only. See the 2026-09-29 entry \"Restore CI test execution\"."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: scripts/validate-nuget-packages.py:35-38 now explicitly allows exact Hexalith.EventStore.ServiceDefaults and Hexalith.Commons.ServiceDefaults IDs. .github/workflows/ci.yml includes UI Tier 1; spec-5-7-correct-global-administrator-authority-from-audit-evidence.md records a later build-and-test success at 6b6a35f0. The specific package-boundary-before-tests blocker is removed; this does not assert current hosted job health. Current-head CI run 38269521776 at 7a2a126f confirms build-and-test job 114880391875 succeeds; the independent advisory Aspire failure is tracked separately.
verification-files: scripts/validate-nuget-packages.py; _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md
evidence-kind: resolved
priority: P1
owner: unassigned
dependencies: none
next-action: none
related: DW-463,DW-495,DW-516
duplicate-of: DW-463
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-485: Retain a submitted correction preview across audit refresh, row loss, and viewport changes (already tracked above).

origin: migrated from unnumbered ledger ("Deferred from: build review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`\n  summary: Retain a submitted correction preview across audit refresh, row loss, and viewport changes (already tracked above).\n  evidence: BH3/EH1 repeat the \"submitted, tracked tenant-correction preview is still unmounted\" entry in the code-review section above; track it there.\n  resolution: Resolved by Story 5.6 commit `17538e07`; see the submitted-preview retention entry above."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: TenantCorrectionAttemptTracker stores admitted snapshots and MessageIds; TenantAuditPage.razor:251-271 renders retained previews outside the row grid and exposes resume on remount. TenantAuditPageTests.Pending_correction_can_resume_after_cancel_row_loss_and_narrow_viewport and Remount_restores_pending_attempt_on_a_stale_authorized_audit_surface cover recovery. Existing resolution 17538e07 is preserved.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
evidence-kind: resolved
priority: P1
owner: unassigned
dependencies: none
next-action: none
related: DW-483
duplicate-of: DW-483
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-486: Require attempt-specific provenance before linking a corrective audit event as proof.

origin: migrated from unnumbered ledger ("Deferred from: build review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`\n  summary: Require attempt-specific provenance before linking a corrective audit event as proof.\n  evidence: BH5; the existing Story 5.6 proof search matches type, tenant, target, and time, so a separate command can produce a false proof match.\n  resolution: Resolved by Story 5.6 commit `17538e07`; the authorized audit DTO has no attempt association, so the panel shows missing support and creates no paired links."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: CorrectionStartPanel.razor:1044 applies WithCorrectiveProof(null) for tenant correction and never infers pairing from target/time. CorrectionStartPanelTests.Panel_provider_confirmed_correction_reports_missing_audit_association pins honest missing support. Existing resolution 17538e07 is preserved.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
evidence-kind: resolved
priority: P1
owner: unassigned
dependencies: none
next-action: none
related: DW-487,DW-504
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-487: Give the corrective audit proof link a matching destination in the audit grid.

origin: migrated from unnumbered ledger ("Deferred from: build review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`\n  summary: Give the corrective audit proof link a matching destination in the audit grid.\n  evidence: BH6; the existing Story 5.6 link points to an audit-reference fragment while the grid renders no matching id.\n  resolution: Resolved by Story 5.6 commit `17538e07`; the unproven fragment link was removed."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: CorrectionStartPanel.razor no longer builds the unproven tenant corrective fragment link; its projection-confirmation path applies WithCorrectiveProof(null). The original evidence receipt remains available without fabricating a destination. Existing resolution 17538e07 is preserved.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: DW-486
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-488: Show the original evidence timestamp in the Story 5.6 final preview.

origin: migrated from unnumbered ledger ("Deferred from: build review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`\n  summary: Show the original evidence timestamp in the Story 5.6 final preview.\n  evidence: BH7; the preview carries originalTimestamp in its intent but filters it from visible preview fields, although the start surface shows it.\n  resolution: Resolved by Story 5.6 commit `17538e07`; the preview shows the verified original evidence time."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: CorrectionStartPanel.razor:67 renders tenants-correction-original-time from OriginalTimestampLabel; that property at :409-412 formats the verified OriginalTimestampUtc. The generic-input list excludes the duplicate originalTimestamp because the dedicated field now exists. Existing resolution 17538e07 is preserved.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-490: Cancelling during an in-flight correction submission loses the panel's `HasSubmitted` flag, so a new attempt can be sent (pre-existing).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`\n  summary: Cancelling during an in-flight correction submission loses the panel's `HasSubmitted` flag, so a new attempt can be sent (pre-existing).\n  evidence: In `CorrectionStartPanel.razor:162`, the Cancel button is never disabled while `_isSubmitting`. `CloseCorrectionAsync` then unmounts the preview. Because `HasSubmitted` is scoped to the panel (resolved option (a)), `TenantAuditPage.HasSubmittedCorrection` becomes false, and Start → handoff → Confirm can dispatch a second attempt with a new MessageId while the first POST is still in flight. This belongs with the Story 5.6 preview-retention item above.\n  resolution: Resolved by Story 5.6 commit `17538e07`; admission retains the MessageId and lease before gateway I/O, and Cancel invalidates only an unadmitted Confirm."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: TenantCorrectionAttemptTracker.TryBegin retains aggregate admission and MessageId before DispatchAttemptAsync; CorrectionStartPanel.razor:844-858 admits before gateway I/O. Cancel invalidates unadmitted confirmation but cannot create a second admitted identity. Retained-preview tests cover cancel/resume. Existing resolution 17538e07 is preserved.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
evidence-kind: resolved
priority: P1
owner: unassigned
dependencies: none
next-action: none
related: DW-483
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-491: Ambiguous, FailedWithKey and RejectedWithKey correction submissions show no localized reason and offer no retry with the same message ID (pre-existing).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`\n  summary: Ambiguous, FailedWithKey and RejectedWithKey correction submissions show no localized reason and offer no retry with the same message ID (pre-existing).\n  evidence: `TenantCorrectionPreviewSnapshot.ApplySubmissionFailure` (`:194`) is unchanged since the story baseline. It copies neither `SafeMessageKey` nor `MessageId`. `TenantCommandSubmissionResult.Ambiguous` returns `RequestSent` with a key and a reusable MessageId, so the preview stays in \"request sent\" with no message. Refresh needs tracking, and Confirm is blocked by `CanSubmit`. The gateway contract says a retry with the same ID is safe, but this panel never offers one.\n  resolution: Resolved by Story 5.6 commit `17538e07`; the retained attempt preserves the MessageId and safe key and Refresh retries only the same ID."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: CorrectionStartPanel.razor:940-946 preserves the attempt MessageId and SafeMessageKey for nonaccepted outcomes; RefreshStatusAsync uses TenantCorrectionAttemptTracker.TryStartRetry with the same retained ID. CorrectionStartPanelTests.AmbiguousDeliveryRecoveryNamesRetryAndReusesOnlyTheRetainedMessageId covers the recovery. Existing resolution 17538e07 is preserved.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-492: `TenantAuditPageTests.Loading_receipt_renders_while_focus_probe_and_authoritative_read_are_pending` fails when run alone and is flaky in the full suite (pre-existing, Story 5.3 `38da46a6`).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md`\n  summary: `TenantAuditPageTests.Loading_receipt_renders_while_focus_probe_and_authoritative_read_are_pending` fails when run alone and is flaky in the full suite (pre-existing, Story 5.3 `38da46a6`).\n  evidence: Running `Hexalith.Tenants.UI.Tests -method '*Loading_receipt_renders_while_focus_probe_and_authoritative_read_are_pending'` failed 3 of 3 times at `31c2d2e1` with \"gateway.Requests.Count should be 2 but was 1\". The verification-gap layer saw 1 failure in 4 full-suite runs. The `WaitForAssertion` at `:990` waits on a stub counter that is only re-checked on renders; use the repository's `SpinWait.SpinUntil` idiom."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: TenantAuditPageTests.Loading_receipt_renders_while_focus_probe_and_authoritative_read_are_pending now observes gateway.Requests directly with SpinWait.SpinUntil(..., TimeSpan.FromSeconds(5)) at :991; the analogous focus-probe test does likewise at :1031. The render-dependent counter wait was replaced.
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-493: Verify whether viewport observation can change concurrently between the final safety check and correction attempt admission.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md (2026-10-02)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Verify whether viewport observation can change concurrently between the final safety check and correction attempt admission.\n  evidence: A Fluxor effect writes the circuit-scoped observation while the Razor event reads it; dispatcher scheduling evidence or a controlled concurrency test is needed to establish whether the synchronous admission section can interleave. If reachable, a command could dispatch after the viewport becomes unsafe.\n  resolution: Resolved by Story 5.6 commit `93b0b96d`; the post-read admission section runs on the renderer dispatcher and rechecks viewport safety inside its critical section."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: CorrectionStartPanel.razor:777 routes post-read admission through InvokeAsync; AdmitAndDispatchAsync rechecks IsViewportBlocked inside lock(_confirmSync) at :844-847. CorrectionStartPanelTests.Width_narrows_during_current_read_and_blocks_dispatch_until_safe_again covers loss before admission. Existing resolution 93b0b96d is preserved.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-494: The global-administrator correction still links corrective proof by event type plus a later timestamp (pre-existing; Story 5.7).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-03)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorAuditProofLookup.cs; src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: The global-administrator correction still links corrective proof by event type plus a later timestamp (pre-existing; Story 5.7).\n  evidence: `GlobalAdministratorCorrectionSnapshot.WithCorrectiveProof` (`:679`) builds a `TenantCorrectionProofLink` from any later row. `GlobalAdministratorCorrectionPanel.razor:114` renders it as a `#audit-…` fragment link. Story 5.6's Design Note says that kind of target/time match proves nothing, and the new `TenantCorrectionProofLink.cs:3` doc comment (\"backed by attempt-specific evidence\") is false for this, its only producer. Story 5.7 must replace it with deterministic attempt provenance or the truthful missing-support state."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: GlobalAdministratorAuditProofLookup.cs checks exact CommandCorrelationId and AggregateSequence == committedEventSequence plus authorized current scope/target/type/time, then returns a redacted typed GlobalAdministratorAuditProof. GlobalAdministratorCorrectionSnapshot.WithCorrectiveProof accepts that internal proof, and GlobalAdministratorCorrectionPanel.razor:584-586 links a real receiptReference URL. GlobalAdministratorAuditProofLookupTests cover mismatched provenance.
verification-files: src/Hexalith.Tenants.UI/Services/Gateways/GlobalAdministratorAuditProofLookup.cs; src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
evidence-kind: resolved
priority: P1
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-495: CI still runs no Tenants test tier (already tracked; reconfirmed).

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-03)"), 2026-10-12
location: scripts/validate-nuget-packages.py; _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: CI still runs no Tenants test tier (already tracked; reconfirmed).\n  evidence: Main CI fails at \"Validate package consumer references\", so every Tier 1 step is skipped. The 3,797-test UI run cited for Story 5.6 is local evidence only. See the 2026-09-29 entry \"Restore CI test execution\"."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: scripts/validate-nuget-packages.py:35-38 now explicitly allows exact Hexalith.EventStore.ServiceDefaults and Hexalith.Commons.ServiceDefaults IDs. .github/workflows/ci.yml includes UI Tier 1; spec-5-7-correct-global-administrator-authority-from-audit-evidence.md records a later build-and-test success at 6b6a35f0. The specific package-boundary-before-tests blocker is removed; this does not assert current hosted job health. Current-head CI run 38269521776 at 7a2a126f confirms build-and-test job 114880391875 succeeds; the independent advisory Aspire failure is tracked separately.
verification-files: scripts/validate-nuget-packages.py; _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md
evidence-kind: resolved
priority: P1
owner: unassigned
dependencies: none
next-action: none
related: DW-463,DW-484,DW-516
duplicate-of: DW-463
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-497: The story gitlink validator exits 1 on the working tree because `references/Hexalith.Builds` and `references/Hexalith.FrontComposer` were moved outside this story.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 2 (2026-10-03)"), 2026-10-12
location: _bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: The story gitlink validator exits 1 on the working tree because `references/Hexalith.Builds` and `references/Hexalith.FrontComposer` were moved outside this story.\n  evidence: The default run reports Builds `3639c8d → c16249a` and FrontComposer `24033f7 → bf40099` as undeclared. Both are unstaged; `--ref 93b0b96d` passes and the committed range moves no pointer. Keep both pointers out of any Story 5.6 commit, or revert them and commit the bump separately as `build(deps)`."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: Current git status --short reports no references/ pointer modifications. The previously unstaged Builds/FrontComposer moves are historical, and the user adopted later committed pins in spec-5-6-preview-confirm-and-link-a-tenant-correction.md:296. Historical-range guard behavior is independently retained by DW-549; the prior keep-unstaged-pointers-out instruction no longer requires work.
verification-files: _bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: DW-549
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-501: Pin FrontComposer deferred editor-focus preservation when an abandonment warning reopens before its queued callback runs.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 2 (2026-10-03)"), 2026-10-12
location: references/Hexalith.FrontComposer/tests/e2e/specs/form-abandonment-guard.spec.ts
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Pin FrontComposer deferred editor-focus preservation when an abandonment warning reopens before its queued callback runs.\n  evidence: fc-focus.js:160 guards restoreEditedOrigin, but form-abandonment-guard.spec.ts waits for editor focus before reopening and the bUnit tests only observe JS calls. A controlled requestAnimationFrame case should assert focus stays on the reopened warning; this is a test gap in the separately committed FrontComposer dependency update."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: references/Hexalith.FrontComposer/tests/e2e/specs/form-abandonment-guard.spec.ts:131-159 now contains deferred edited-origin return preserves a reopened warning action, invokes restoreEditedOrigin while Stay owns focus, awaits requestAnimationFrame and asserts document.activeElement === stay. Source is from FrontComposer commit 7668d040.
verification-files: references/Hexalith.FrontComposer/tests/e2e/specs/form-abandonment-guard.spec.ts
evidence-kind: resolved
priority: P2
owner: Hexalith.FrontComposer
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-515: Give the EventStore shared-consumer authority Contracts test more headroom, or speed it up.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 9 (2026-10-05)"), 2026-10-12
location: references/Hexalith.EventStore/tests/Hexalith.EventStore.Contracts.Tests/Packaging/ContractsPackageDependencyTests.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Give the EventStore shared-consumer authority Contracts test more headroom, or speed it up.\n  evidence: `SharedConsumerAuthorityValidatorPassesForEveryTrackedMsBuildSurfaceAsync` timed out at 3m 00s in the pass-7 closure's first full Contracts run, then passed alone in 2m 49s. That is 11 s of headroom, so it is likely to flake in full or CI runs. EventStore-owned; not caused by Story 5.6."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: references/Hexalith.EventStore/tests/Hexalith.EventStore.Contracts.Tests/Packaging/ContractsPackageDependencyTests.cs:11 now sets _consumerAuthorityValidationTimeout = TimeSpan.FromMinutes(8); the process timeout uses that value at :480-490. The prior three-minute headroom limit has been removed.
verification-files: references/Hexalith.EventStore/tests/Hexalith.EventStore.Contracts.Tests/Packaging/ContractsPackageDependencyTests.cs
evidence-kind: resolved
priority: P2
owner: Hexalith.EventStore
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-516: Carried, with no new work item; see "Restore CI test execution" above. Tenants CI never runs the strengthened same-row correction regression `RoleSelectionAndAnotherStartPreserveASubmittedPreviewAndItsTrackingHandle`.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 11 (2026-10-05)"), 2026-10-12
location: scripts/validate-nuget-packages.py; _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Carried, with no new work item; see \"Restore CI test execution\" above. Tenants CI never runs the strengthened same-row correction regression `RoleSelectionAndAnotherStartPreserveASubmittedPreviewAndItsTrackingHandle`.\n  evidence: CI run `37298335264` at `d8558263` fails at \"Validate package consumer references\" and skips both Tier 1 steps. `story-guards.yml:55-59` only builds the UI test project, and `source-reference.yml` runs only the integration tests. So the pass-9 MV1 kill (`TenantAuditPage.razor:1552` forced to `competing: true`) is local evidence only. Pre-existing; not caused by Story 5.6."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: scripts/validate-nuget-packages.py:35-38 now explicitly allows exact Hexalith.EventStore.ServiceDefaults and Hexalith.Commons.ServiceDefaults IDs. .github/workflows/ci.yml includes UI Tier 1; spec-5-7-correct-global-administrator-authority-from-audit-evidence.md records a later build-and-test success at 6b6a35f0. The specific package-boundary-before-tests blocker is removed; this does not assert current hosted job health. Current-head CI run 38269521776 at 7a2a126f confirms build-and-test job 114880391875 succeeds; the independent advisory Aspire failure is tracked separately.
verification-files: scripts/validate-nuget-packages.py; _bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md
evidence-kind: resolved
priority: P1
owner: unassigned
dependencies: none
next-action: none
related: DW-463,DW-484,DW-495
duplicate-of: DW-463
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-537: low; the detail smoke test asserts the unauthorized marker twice, and the comment above the duplicate describes a source-order form that never existed in this file.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-gh-actions-28953291798-85906522208.md (2026-10-08)"), 2026-10-12
location: tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs
source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`\n  summary: low; the detail smoke test asserts the unauthorized marker twice, and the comment above the duplicate describes a source-order form that never existed in this file.\n  evidence: `TenantsUiRouteSmokeTests.cs` line 84 (`TenantsDetailUnauthorizedMarker`) and line 92 (literal `data-testid=\"tenants-detail-unauthorized\"`) are the same check. `git log -p` on the file shows no combined `unauthorized\" role=` assertion that \"pinned their source order\". Delete the duplicate and trim the comment."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs was rewritten as hosted shell scope-denial tests and contains neither TenantsDetailUnauthorizedMarker nor the duplicated literal page marker assertion/comment. The former duplicate is gone.
verification-files: tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs
evidence-kind: resolved
priority: P3
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-539: low; the user-lookup smoke comment says the compatibility route "redirects before issuing the lookup", but the code issues the lookup first.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-gh-actions-28953291798-85906522208.md (2026-10-08)"), 2026-10-12
location: tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs
source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`\n  summary: low; the user-lookup smoke comment says the compatibility route \"redirects before issuing the lookup\", but the code issues the lookup first.\n  evidence: `UserMembershipLookupPanel.razor:520-548` awaits `QueryGateway.GetUserTenantsAsync` and only then calls `Navigation.NavigateTo(UserLookupNavigationUrl, replace: true)`. `UserMembershipLookupPage.razor` and the server have no earlier redirect, and `UserMembershipLookupSurfaceTests.cs:218-225` shows the request is issued before the URL rewrite. Correct the comment at `TenantsUiRouteSmokeTests.cs:154-155`."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs now checks UserLookupRouteBlocksAnonymousScope and has no claim that the compatibility route redirects before lookup. The stale route comment was removed.
verification-files: tests/Hexalith.Tenants.IntegrationTests/TenantsUiRouteSmokeTests.cs
evidence-kind: resolved
priority: P3
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-541: high; on `main` (`5bfe0715`, run 37782396626) `ci / build-and-test` fails, so `ci / aspire-tests` is skipped and the hosted smoke class never runs in CI.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-gh-actions-28953291798-85906522208.md (2026-10-08)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-gh-actions-28953291798-85906522208.md`\n  summary: high; on `main` (`5bfe0715`, run 37782396626) `ci / build-and-test` fails, so `ci / aspire-tests` is skipped and the hosted smoke class never runs in CI.\n  evidence: The job log shows two failures: `GlobalAdministratorsPageTests.RealChromiumFocusValidatorStopsBeforeChromiumWhenServerStartupFails` and `StatelessHostStateTests.TenantsHostAssembly_HasNoWritableStaticFields_HoldingInstanceLocalState` (coverage-instrumentation statics). At review time, uncommitted working-tree edits to both test files existed from a concurrent session; re-check before acting. Update from Story 5.6 pass 17 (2026-10-08): both fixes landed in root `9ee73062b7e540b69f824803d182f151c96ddb24` (browser configuration forwarding and coverage-tracker field recognition). The recorded HEAD run 37816323193 at `0ac7c126ea0654edfeaa867b108749fa463e7287` passes build-and-test, so the former skip condition is resolved. aspire-tests now runs and fails with 14 errors: fixture initialization rejects JwtBearer configuration unless exactly one of Authority or SigningKey is set, and the client-supplied actor:globalAdmin test still expects 400 although the gateway ignores the reserved key and returns 202. The lane remains masked by aspire-continue-on-error. The command-API expectation is tracked in the pass-17 defer below; these are historical review observations, not a fresh CI run by this remediation."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: The original build-and-test skip causes are fixed: GlobalAdministratorsPageTests forwards TENANTS_BROWSER_BUILD_CONFIGURATION, and StatelessHostStateTests uses constrained coverage tracker recognition. AppHost Program.cs:280 clears JwtBearer Authority before symmetric SigningKey, fixture ExtraAppArgs disables Keycloak, and CommandsEndpointIgnoresClientSuppliedGlobalAdminExtensionMetadata now expects 202. Existing recorded resolution 9ee73062 and later successful build-and-test evidence are preserved; no claim that every current hosted lane passes. Current-head run 38269521776 build-and-test job 114880391875 succeeds and Aspire executes 184 tests (183 pass, one independent /alive timeout); the former skip/auth/test-expectation causes are not present.
evidence-kind: resolved
priority: P1
owner: unassigned
dependencies: none
next-action: none
related: DW-548,DW-540
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-543: high claim superseded by pass-17 gateway tracing; see the medium command-API test-expectation defer below.

origin: migrated from unnumbered ledger ("Deferred from: pass 16 review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-08)"), 2026-10-12
location: tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: high claim superseded by pass-17 gateway tracing; see the medium command-API test-expectation defer below.\n  evidence: The pass-16 aggregate-only trace did not cover the gateway: EventStore ignores and strips the client-supplied reserved extension, then adds it only from the JWT claim. Pass 17 rejects the client-authority claim as false at that gateway and records the stale integration-test expectation instead; this entry creates no second authority work item."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: CommandsEndpointIgnoresClientSuppliedGlobalAdminExtensionMetadata at tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs:1601-1640 asserts 202, IsGlobalAdmin false, and removal of client actor:globalAdmin while retaining other safe metadata. EventStore CommandsController strips the reserved key and adds authority from authenticated claims; the former aggregate-only gateway claim was correctly superseded.
verification-files: tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs
evidence-kind: resolved
priority: P1
owner: unassigned
dependencies: none
next-action: none
related: DW-548
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-546: Story 5.7; loading may drop an in-flight global-administrator correction when that story mounts the panel. Not reachable on the tenant audit page since `10c9f6f6`; unverified, high if true.

origin: migrated from unnumbered ledger ("Deferred from: pass 16 review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-08)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: Story 5.7; loading may drop an in-flight global-administrator correction when that story mounts the panel. Not reachable on the tenant audit page since `10c9f6f6`; unverified, high if true.\n  evidence: The tenant audit page no longer mounts `GlobalAdministratorCorrectionPanel` after `10c9f6f6`, and the pass-19 read-only cleanup removes its remaining global-administrator enrichment. Re-check loading and retained-display behavior when Story 5.7 mounts the panel. Story 5.7 owns that panel."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: GlobalAdministratorCorrectionPanel.razor:2215-2228 and Dispose at :2383 retain reconciliation through the shared aggregate lease; matching replacement panels can adopt it. GlobalAdministratorCorrectionPanelTests.InitialRemoveDeliveryCompletionSurvivesDisposalAndReplacement, RestoreUnableToVerifyIsRetainedAndAdoptedByReplacementPanel and NullOrChangedIntentReturnsAcceptedTrackingToMatchingReplacement pin the resumed attempt. Loading can unmount UI but now preserves admitted identity/reconciliation.
verification-files: src/Hexalith.Tenants.UI/Components/Tenants/Audit/GlobalAdministratorCorrectionPanel.razor
evidence-kind: resolved
priority: P1
owner: unassigned
dependencies: none
next-action: none
related: none
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-547: resolved by `10c9f6f6`; the tenant audit page no longer opens or submits a global-administrator correction.

origin: migrated from unnumbered ledger ("Deferred from: pass 16 review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md (2026-10-08)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor; _bmad-output/implementation-artifacts/sprint-status.yaml
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: resolved by `10c9f6f6`; the tenant audit page no longer opens or submits a global-administrator correction.\n  evidence: Pass 15 traced the former `GlobalAdministratorCorrectionPanel` audit-page branch to `5a3bc6dd` and the global-administrator projection spec. `10c9f6f6` removed the branch; the pass-19 read-only cleanup restores the remaining audit-surface copy and removes unused evidence reads.\n  resolution: Resolved by Story 5.6 commit `10c9f6f6` (panel branch removed) and `ded413df` (read-only copy restored, evidence reads removed)."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: Historical Story 5.6 commits 10c9f6f6/ded413df removed the out-of-scope global correction branch and restored read-only copy. Current TenantAuditPage.razor:262-269 deliberately mounts the authorized global panel under completed Story 5.7; sprint-status.yaml marks 5.7 done, and the panel now has fixed-scope proof/admission tests. Later scoped Story 5.7 functionality does not reinstate the old Story 5.6 ownership defect.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor; _bmad-output/implementation-artifacts/sprint-status.yaml
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: DW-494,DW-546
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-548: medium; `CommandApiRuntimeIntegrationTests.Commands_endpoint_rejects_client_supplied_globalAdmin_extension_metadata` expects `400`, but EventStore deliberately ignores the reserved key and returns `202`. The test is red in the masked `ci / aspire-tests` lane.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 17 (2026-10-08)"), 2026-10-12
location: tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; `CommandApiRuntimeIntegrationTests.Commands_endpoint_rejects_client_supplied_globalAdmin_extension_metadata` expects `400`, but EventStore deliberately ignores the reserved key and returns `202`. The test is red in the masked `ci / aspire-tests` lane.\n  evidence: It fails at `CommandApiRuntimeIntegrationTests.cs:1629` in job 113448120420 (HEAD `0ac7c126`), and earlier at `416ab32c`, `390da330` and `03257338`. EventStore `CommandsController.cs:244` logs and skips `actor:globalAdmin`, and `SubmitCommandExtensions.cs:33` re-adds it only from the JWT claim. The client therefore cannot set global-admin authority through this endpoint. The pass-16 entry \"Tenant command admission still treats `actor:globalAdmin=true` as authority\" is a false positive at this gateway. Align the test with the ignore-and-log contract, or change the contract to reject, in the command-API owner's spec."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12 by source/test inspection; tests not rerun: tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs:1601-1640 is now CommandsEndpointIgnoresClientSuppliedGlobalAdminExtensionMetadata and asserts HttpStatusCode.Accepted, IsGlobalAdmin=false and no reserved extension on the routed command. It matches EventStore’s ignore-and-log contract and validates no client authority.
verification-files: tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: DW-543,DW-541
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-559: low; Story 5.7 handoff. Story 5.6 reverted the tenant audit page's global-administrator integration, and Story 5.7 must re-derive it.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 21 (2026-10-09)"), 2026-10-12
location: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor; _bmad-output/implementation-artifacts/sprint-status.yaml
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: low; Story 5.7 handoff. Story 5.6 reverted the tenant audit page's global-administrator integration, and Story 5.7 must re-derive it.\n  evidence: `5a3bc6dd` holds the removed page code: the complete-evidence load, the correction-open refresh, the global-intent re-derivation, and the 8-case `GlobalAdministratorIncompleteEvidenceCannotEnableOrOpenCorrection` fail-closed matrix. `GlobalAdministratorCorrectionPanel.CorrectiveAuditEvidence` (`GlobalAdministratorCorrectionPanel.razor:201`) was added for that integration and now has no consumer and no test. When Story 5.7 enables the correction, it must re-derive the integration and its matrix from the Epic 5 rules, or remove the parameter."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12: TenantAuditPage.razor mounts GlobalAdministratorCorrectionPanel, performs complete projection refresh and derives global intent; TenantAuditPageTests.TenantAuditPageOffersGlobalAdministratorCorrectionWithAuthority and GlobalAdministratorAuditCannotArmCorrectionEvenThroughADirectCallback inspect integration/fail-closed paths. Story 5.7 is done in sprint-status.yaml. Tests inspected, not rerun.
verification-files: src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor; _bmad-output/implementation-artifacts/sprint-status.yaml
evidence-kind: resolved
priority: P3
owner: unassigned
dependencies: none
next-action: none
related: DW-558,DW-565
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-565: medium; global-administrator correction proof needs a complete, current audit walk before pairing receipts.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-6-preview-confirm-and-link-a-tenant-correction.md, pass 25 (2026-10-09)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-6-preview-confirm-and-link-a-tenant-correction.md`\n  summary: medium; global-administrator correction proof needs a complete, current audit walk before pairing receipts.\n  evidence: `GlobalAdministratorCorrectionPanel.QueryCorrectiveProofAsync` filters the rows of one `GetTenantAuditAsync` response without checking its availability/freshness or following `HasMore` and `NextCursor`. A stale or degraded page can supply a false proof candidate, while a later page's valid candidate is missed. The panel has no production mount today; Story 5.7 should enforce current, complete audit evidence and deterministic attempt association before enabling its proof link. The existing Story 5.7 entry at `deferred-work.md:3500-3502` covers the separate target/type/time association weakness."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12: GlobalAdministratorCorrectionPanel.QueryCorrectiveProofAsync delegates to GlobalAdministratorAuditProofLookup.FindAsync, which walks up to 50 current same-version audit pages and requires exact correlation/sequence, target/type and unique matching row. GlobalAdministratorAuditProofLookupTests covers duplicate exact rows across pages. Source/tests inspected, not rerun.
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: DW-559
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-624: unverified medium; `GlobalAdministratorProjectionHandler` takes the projection and `audit:system` version from the last event iterated, not the highest sequence. Owner: Tenants global-administrator projection.

origin: migrated from unnumbered ledger ("Deferred from: code review of spec-5-7-correct-global-administrator-authority-from-audit-evidence.md (2026-10-11)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: unverified medium; `GlobalAdministratorProjectionHandler` takes the projection and `audit:system` version from the last event iterated, not the highest sequence. Owner: Tenants global-administrator projection.\n  evidence:\n  - **Code:** `src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs:71-76` overwrites `ProjectionVersion` on every event. `TenantProjectionHandler.StampAggregateProjectionVersion` instead keeps a monotonic maximum.\n  - **Consequence:** if `ProjectionRequest.Events` ever arrives out of sequence order, both versions understate progress. Story 5.7 confirmation and the audit-proof walk would then stay pending.\n  - **To settle:** find out whether EventStore guarantees sequence order for the full-history projection payload. If it does not, stamp the maximum."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12: current EventStore ProjectionUpdateOrchestrator builds handler payload through ProjectionEventWireBuilder.BuildAsync, which rejects nonpositive/noncontiguous/out-of-order sequences before constructing Events in the same order. Last iteration therefore equals highest sequence on the supported producer path. ProjectionEventWireBuilderTests.BuildAsync_LaterSequenceGapRefusesWholeBatch inspected; tests not rerun.
evidence-kind: resolved
priority: P2
owner: Tenants global-administrator projection
dependencies: none
next-action: none
related: DW-568,DW-638
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-632: Add authentication tests for the independently committed manifest configuration fallbacks.

origin: migrated from unnumbered ledger ("Deferred from: independent module manifest review of `711e100858e59cf8de2b626ea21790d59f969023` (Review 6, 2026-10-11)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: Add authentication tests for the independently committed manifest configuration fallbacks.\n  evidence: Review 6 found no tests for `Oidc:Authority` or `Jwt:SigningKey` when the old `EventStore:Authentication:*` keys are absent; these fallbacks were added by independent commit `711e100858e59cf8de2b626ea21790d59f969023`."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12: TenantsApiGeneratedControllerTests includes Public_api_accepts_jwt_signing_key_alias_without_eventstore_authentication_keys and Public_api_accepts_oidc_authority_alias_without_eventstore_authentication_keys. Current HEAD CI 38269521776/job 114882247420 ran 184 nonperformance tests, 183 passed/0 skipped; its sole failure is the separate live Generated topology test. Existing CI result inspected, no new run.
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: DW-633,DW-641
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-633: Reconcile the independent authentication fallback deferred entry with its now-present alias-only API tests.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.7 Review 8 fix-pass triage (2026-10-11)"), 2026-10-12
location: n/a
source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: Reconcile the independent authentication fallback deferred entry with its now-present alias-only API tests.\n  evidence: `tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs` contains `Public_api_accepts_jwt_signing_key_alias_without_eventstore_authentication_keys` and `Public_api_accepts_oidc_authority_alias_without_eventstore_authentication_keys`, while the earlier Review 6 ledger entry still requests those tests. Run the owning integration lane and update that entry's resolution in a separate manifest/API follow-up."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12: both alias-only normal Fact tests exist in TenantsApiGeneratedControllerTests and are included in HEAD 7a2a126f CI run 38269521776 Aspire job 114882247420 (184 executed, 183 passed, zero skipped; sole failed test is Generated_tenants_api_get_tenant_reads_verified_redis_state_with_projection_authority). Earlier missing-test record DW-632 is reconciled.
evidence-kind: resolved
priority: P3
owner: unassigned
dependencies: none
next-action: none
related: DW-632,DW-594
duplicate-of: DW-632
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


### DW-638: unverified medium; a global-administrator event persisted at sequence 0 cannot be paired from audit evidence.

origin: migrated from unnumbered ledger ("Deferred from: Story 5.7 Review 10 triage (2026-10-12)"), 2026-10-12
location: n/a
source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`
reason: Original finding and any prior resolutions are preserved losslessly in legacy-detail; current disposition is recorded in verification.
legacy-detail: "- source_spec: `/home/administrator/projects/hexalith/tenants/_bmad-output/implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md`\n  summary: unverified medium; a global-administrator event persisted at sequence 0 cannot be paired from audit evidence.\n  evidence: `src/Hexalith.Tenants/Projections/GlobalAdministratorProjectionHandler.cs` stamps `ProjectionVersion` for `SequenceNumber >= 0`, while `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs` stores `AggregateSequence` only when `SequenceNumber > 0`. Whether EventStore can persist a global-administrator event at sequence 0 is unverified; that fact would settle whether the proof walk can miss a real event."
status: done 2026-10-12
archived: 2026-10-12
disposition: done
verification-date: 2026-10-12
verification: Verified 2026-10-12: EventStore EventEnvelope documents 1-based sequences; EventPersister allocates currentSequence+1+index and rejects invalid persisted metadata; ProjectionEventWireBuilder rejects SequenceNumber<=0 before domain projection. Sequence zero cannot reach this supported persisted global-event path. Source/test contracts inspected, not rerun.
evidence-kind: resolved
priority: P2
owner: unassigned
dependencies: none
next-action: none
related: DW-624
resolution: verified resolved 2026-10-12; concrete current evidence is recorded in verification and verification-files.


