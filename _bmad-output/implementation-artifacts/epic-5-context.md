# Epic 5 Context: Audit Evidence and Corrective Recovery

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Let authorized users inspect tenant and platform-authority activity through contextual, support-safe audit evidence and tell proven evidence apart from pending, delayed, unavailable, or unsupported evidence. Users correct mistakes with new forward compensating commands, and each correction's preview, projection confirmation, and linked proof stay separate from the immutable original record. The epic extends evidence and recovery across the Epic 2–4 command domains. Epic 2 still owns minimum removal proof. Existing Epic 5 code is historical evidence to reverify against the current contracts, not proof of completion.

## Stories

- Story 5.1: Browse Tenant Audit Trail
- Story 5.2: Reach Scoped Audit Evidence from Context
- Story 5.3: View a Support-Safe Audit Evidence Receipt
- Story 5.4: Understand Audit Availability and Recovery
- Story 5.5: Start a Forward Tenant Correction from Audit Evidence
- Story 5.6: Preview, Confirm, and Link a Tenant Correction
- Story 5.7: Correct Global Administrator Authority from Audit Evidence

## Requirements & Constraints

- **Audit list:** tenant-scoped, authorization-safe, and in authoritative timestamp/tie-breaker order with no client re-sort. Filters: absolute date range and `AuditEventCategory` (`Access`/`Administrative`). Paging uses cursors only. Cursors are opaque, protected, bound to caller, tenant, and filter, and never visible or logged. A scope change or invalid cursor restarts at page 1 with an honest notice.
- **List states:** loading, empty, filtered-empty, error, stale, degraded, unauthorized, invalid-cursor, and unavailable stay distinct, each with a recovery. Last-confirmed rows are reused only for the identical scope.
- **Performance:** no numeric claim until Product/Operations approves the audit-performance decision record. It covers the 500-event dataset, page size and filters, environment, percentile budgets, test tier, repeatability, and fallback trigger. Story 5.1 is not Ready without it. A miss switches to the approved stricter paging or virtualization with no loss of order, cursor, accessibility, or safety guarantees.
- **Contextual reach:** open audit from a tenant row, tenant detail, a user lookup or member row, or a command result. Add no shell navigation entry or global inventory. User context is a hint unless server-filtered, so never imply exhaustive results. Missing scope, authorization, or support fails closed with an inline reason.
- **Receipt:** seven fields only: actor, target, tenant scope, outcome, absolute timestamp with an explicit UTC offset, projection marker, and approved audit/command reference. Build it only from an authorized row in the loaded result. An absent reference shows inspect-audit/unavailable; never scan hidden pages. Copying goes through the support-safe classifier.
- **Availability:** `audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support` are distinct from each other and from proven `audit available`. None is shown as success or enables a correction. Command lifecycle, projection truth, and audit evidence are separate typed dimensions. Command status, event counts, confirmation, and SignalR never yield `audit available`.
- **Corrections:**
  - Always new forward commands. Never edit or relabel events, projections, or stores. Never use `undo`, `rollback`, or `hidden edit`.
  - Starting one requires complete evidence, current authorization, a current projection, command support, and a safe viewport. Otherwise show the canonical inline reason and its recovery.
  - Confirmed only when the expected postcondition holds plus a projection-version advance or attempt-specific provenance beyond the pre-submit baseline. A pre-existing state is `already applied` (no dispatch). Missing provenance is `unable to verify`.
  - Projection confirmation and corrective audit evidence are shown separately.
- **Readiness:** WCAG 2.1 AA, EN/FR whole-string parity with named placeholders, responsive safety, stable selectors, and focused tests. Mobile is read-only audit reference; unsafe widths disable correction visibly.

## Technical Decisions

- **Reads:** the InteractiveServer BFF reads Tenants REST directly: `GET /api/tenants/{tenantId}/audit`, plus the existing tenant member and `/api/global-administrators` reads for current state. Never the generic EventStore query route, never browser-to-backend calls or browser-held tokens, and no new audit, receipt, preview, correction, or proof endpoints.
- **Support-safety boundary:** the BFF maps allow-listed `NarrativePayload` fields into typed, localized view models. The receipt target resolves `userId`, then `key`, then `TenantId`. Raw narrative, payloads, tokens, claims, MessageIds or correlation IDs, ETags, cursors, metadata, stack traces, and PII never reach component state, copy, announcements, or logs.
- **Freshness:** use `ReadModelFreshnessState` (wire values `current`/`stale`/`unknown`). `ServedAt` never substitutes for projection time. The projection marker shows safe provenance, never a raw ETag.
- **Presentation:** approved fallbacks only: a Tenants-owned flat Fluent audit DataGrid and an inline consequence preview. No generic `<AuditTimeline>` in Tenants.
- **Tenant correction:** after a fresh membership re-query:
  - Absent target: `AddUserToTenant` with an explicit role that is not `Unknown` and not inferred from history.
  - Role differs: `ChangeUserRole`.
  - Same role: `already applied`.
  - Empty tenant: the domain bootstrap path with an explicit owner role.
- **Global-admin correction:**
  - Mapping: a `GlobalAdministratorRemoved` receipt maps only to `SetGlobalAdministrator`, and a `GlobalAdministratorSet` receipt only to `RemoveGlobalAdministrator`.
  - Scope: fixed `system` / `global-administrators` / `global-administrators`, never tenant membership.
  - Current state: page the full projection until presence and count are authoritative.
  - Blocks: one remaining administrator or an uncertain count blocks before submit, with no override.
  - Rejections: raced `GlobalAdministratorAlreadyExists` and `GlobalAdministratorNotFound` stay rejections. `LastGlobalAdministrator` is a hard stop.
- **Dispatch:** existing commands via `POST /api/v1/commands` with one retained ULID attempt ID. Lock per `(circuit, AggregateIdentity)` until terminal evidence. Refresh, reconnect, or a duplicate click never creates a second attempt.
- **Single refresh:** each refresh cycle reuses one authoritative snapshot for conflict checks, confirmation, safety and count checks, and proof search.
- **Proof linking:** link original and corrective receipts both ways only through deterministic attempt-specific provenance plus a match on event, scope, target, time boundary, and baseline. A target/time match or an in-memory association is not enough. Neither record changes.
- **Identifiers and vocabulary:** TenantId and UserId are literal strings, never parsed as a GUID or ULID. Tokens come from the shared vocabulary; `audit pending` and `audit_pending` stay distinct.
- **CLI/MCP:** any exposure goes through `Hexalith.McpCli` contract enrollment, never a module-specific CLI or MCP host.

## UX & Interaction Patterns

- **Grid:** timestamp, actor, outcome, category, freshness, and reference stay available at every width. Filtered-empty offers a reset.
- **Receipt:** field/value semantics with monospace IDs and timestamps. Success styling only for `audit available`. Pending is Informative, delayed Warning, unavailable Severe, and missing support Subtle, always icon plus text.
- **Entry points:** preserve the origin (tab, scope, search, filter, sort, cursor, selection, scroll) and return focus to the launcher. If that is impossible, focus the origin heading and show a notice. Return URLs stay under Tenants routes.
- **Correction preview:**
  - Ten items: original reference, scope, target, current state, intended command and its impact, freshness, authorization/admission readiness, recovery path, proof expectation, and known consequences versus unknowns. Any missing item blocks confirmation.
  - Escape and cancel dispatch nothing, and focus returns to the launching receipt or row.
  - The inline lifecycle panel never overwrites last-confirmed data.
- **Live regions:** polite for progress. Assertive for rejection, failure, `unable to verify`, degraded, and blockers. Never announce success before projection truth.
- **Recovery verbs:** `wait`, `refresh`, `retry status lookup`, `inspect audit`, `continue read-only`, `request permission`, `escalate`, `start correction`, and `restore intended access`.

## Cross-Story Dependencies

- 5.1's grid and safe row model feed 5.2 entry points and 5.3 receipts. 5.2 uses only the shared availability model and does not wait for 5.4. 5.4's state and recovery mapping is reused by every evidence and correction surface.
- 5.5 hands a non-submitting current-state intent to 5.6, which owns preview, dispatch, confirmation, and proof. 5.5 sends global-admin evidence to 5.7. Until 5.7 is complete, global-admin correction shows `high-impact flow not ready`.
- 5.7 reuses Epic 4 (4.1 availability; 4.2–4.3 grant/remove) plus 5.1–5.4 and 5.6's proof and single-refresh work. It may be delivered as tasks 5.7a and 5.7b but is complete only as a whole.
- Tenant correction builds on Epic 2 membership flows. Former Story 5.8 work is folded into 5.6/5.7, and no current 5.8 exists.
