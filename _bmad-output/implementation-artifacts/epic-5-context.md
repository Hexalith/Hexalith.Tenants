# Epic 5 Context: Audit Evidence and Corrective Recovery

<!-- Generated from planning artifacts. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Enable authorized users to inspect contextual, support-safe evidence of tenant and platform-authority activity, understand when that evidence is pending or unavailable, and correct mistakes through new compensating commands whose projected outcome and audit proof remain distinct from the immutable original record.

## Stories

- Story 5.1: Browse Tenant Audit Trail
- Story 5.2: Reach Scoped Audit Evidence from Context
- Story 5.3: View a Support-Safe Audit Evidence Receipt
- Story 5.4: Understand Audit Availability and Recovery
- Story 5.5: Start a Forward Tenant Correction from Audit Evidence
- Story 5.6: Preview, Confirm, and Link a Tenant Correction
- Story 5.7: Correct Global Administrator Authority from Audit Evidence

## Requirements & Constraints

- Audit browsing is tenant-scoped, authorization-safe, stably ordered, and cursor-paginated. It supports absolute date and `AuditEventCategory` (`Access` or `Administrative`) filters. Cursor values remain opaque, protected, bound to caller and query scope, and absent from user-visible output; invalidation restarts honestly at page one.
- Audit is reached contextually from tenant, membership/user, and command surfaces rather than through a global inventory or separate shell entry. Navigation preserves safe origin state and focus; optional context is a hint unless supported by an authoritative server filter and must never imply exhaustive results.
- Evidence receipts expose only actor, target, tenant scope, outcome, absolute timestamp, projection marker, and an approved audit/command reference. Incomplete evidence never becomes proof. The canonical states `audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support` remain distinct, success-prohibited, and paired with an applicable wait, retry, inspect, read-only, permission, or escalation recovery.
- Corrections are new forward commands based on proven evidence and a fresh authoritative projection. History, events, projections, and state stores are never edited or relabeled, and product copy must not use “undo,” “rollback,” or “hidden edit.” Missing evidence, freshness, authorization, preview content, current-state certainty, command support, or responsive safety blocks the action with a visible reason.
- Command acceptance, projection confirmation, and audit availability are separate truth dimensions. SignalR is only a re-query nudge. Success requires the expected postcondition plus projection-version advancement or safe attempt-specific provenance beyond the pre-submit baseline; a pre-existing expected state is `already applied`, and missing provenance is `unable to verify`.
- The audit performance target is governed by an approved Product/Operations decision defining representative data, filters, page size, environment, percentile budgets, repeatability, and fallback trigger. No numeric performance claim may be inferred before that decision; a miss activates its approved stricter-page-size or virtualization fallback without weakening ordering, cursor, accessibility, or safety guarantees.
- All states, filters, timestamps, reasons, and recovery copy are localized as whole strings with named placeholders. UI acceptance covers keyboard and screen-reader use, focus return, live-region intent, forced colors/high contrast, reduced motion, desktop/tablet/mobile layouts, stable selectors, and no color-only meaning.

## Technical Decisions

- The Blazor InteractiveServer BFF performs server-to-server reads through the direct Tenants REST client, including fixed `GET /api/tenants/{tenantId}/audit`; it must not use the generic EventStore query route, issue browser-to-backend calls, retain backend tokens in the browser, or add receipt, preview, correction, proof-link, or audit endpoints.
- The BFF is the support-safety boundary. It allow-lists and redacts structured `NarrativePayload` into typed localized view models; raw narrative, payloads, tokens, internal correlations or message IDs, ETags, cursors, metadata, stack traces, decoded claims, and unapproved PII must be unrenderable, uncopyable, unannounced, unlogged, and absent from component state.
- The approved audit presentation is a Tenants-owned flat Fluent DataGrid fallback, not a locally invented generic timeline. It preserves authoritative order and pins or otherwise retains timestamp, actor, outcome, category, freshness/projection context, and reference across responsive layouts.
- Corrections reuse existing commands through `POST /api/v1/commands` with one retained ULID attempt identity and aggregate-scoped locking through terminal evidence. Tenant membership recovery maps current state to `AddUserToTenant` or `ChangeUserRole`; platform-authority recovery uses only the fixed `system` / `global-administrators` / `global-administrators` scope and requires a complete current administrator projection for last-administrator safety.
- Each correction refresh cycle reuses one authoritative projection snapshot for conflict evaluation, expected-postcondition confirmation, safety checks, and proof-search eligibility. Original/corrective receipt links are assembled only from deterministic attempt-specific provenance plus expected event, scope, target, timestamp boundary, and pre-submit baseline—not target/time coincidence or in-memory association.

## UX & Interaction Patterns

Use a flat audit DataGrid with distinct loading, empty, filtered-empty, error, stale, degraded, unauthorized, invalid-cursor, and unavailable treatments as applicable; filtered-empty offers reset and stale/degraded views retain only same-scope last-confirmed rows. Receipts use semantic field/value relationships and culture-aware absolute timestamps. High-impact corrections require the complete consequence preview, deliberate confirmation, safe Escape/cancel with no dispatch, focus return to the launching evidence, and lifecycle feedback that never overwrites last-confirmed projection data.

## Cross-Story Dependencies

The tenant audit list and safe row context establish the source for contextual navigation and receipt assembly; typed audit availability and recovery behavior is shared by every evidence and correction surface. Tenant correction start hands a non-submitting, current-state intent to preview/confirmation/proof linking and depends on the existing membership command flows. Global-administrator correction depends on the existing fixed-scope grant/remove flows and complete global-administrator reads, but remains isolated from tenant membership rules. Historical implementation evidence, including former Story 5.8 work folded into Stories 5.6 and 5.7, must be reverified against the current contracts rather than treated as completion proof.
