# Epic 5 Context: Audit Evidence and Corrective Recovery

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Scope and source precedence (reconciled 2026-10-21, DW-533)

This is a planning/implementation handoff, not a completion certificate. Story
5.7 is now recorded as done in its owning spec; historical "until complete"
gates below describe sequencing and must not be read as today's backlog status.
Before reuse or regeneration, carry the following source constraints forward.
Planning sources govern the handoff except where an explicitly approved,
scoped implementation decision supersedes earlier wording. In particular,
Story 5.6's approved bounded tenant-retention expiry is an exception to the
earlier terminal-evidence lock rule: it releases tenant admission while keeping
the expired attempt's recovery identity. Do not extend that exception to
global-administrator correction's separate terminal-evidence contract.

- [Architecture: approved McpCli course correction](../planning-artifacts/architecture.md#architecture-decision-document): CLI/MCP exposure uses `Hexalith.McpCli` contract enrollment; never create a module-specific CLI or MCP host.
- [Architecture: Process Patterns](../planning-artifacts/architecture.md#process-patterns): never announce success before projection confirmation. Dedicated announcement intent determines politeness; command acceptance and audit availability remain separate.
- [Epics: Story 5.7](../planning-artifacts/epics.md#story-57-correct-global-administrator-authority-from-audit-evidence): `GlobalAdministratorRemoved` maps only to `SetGlobalAdministrator(UserId)`; `GlobalAdministratorSet` maps only to `RemoveGlobalAdministrator(UserId)`, in fixed `system` / `global-administrators` / `global-administrators` scope. Read current complete authority through `GET /api/global-administrators`, not tenant membership.
- The same Story 5.7 contract makes `LastGlobalAdministrator` a hard stop, including raced rejections: no override or successful-removal announcement. Optional delivery tasks **5.7a** and **5.7b** are not story IDs; Story 5.7 completes only when both pass.
- [Epics: Story 5.3](../planning-artifacts/epics.md#story-53-view-a-support-safe-audit-evidence-receipt) governs the **seven approved presentation fields**: actor, target, tenant scope, outcome, absolute timestamp with explicit UTC offset, projection marker and audit/command reference. The older [audit/recovery specification §3.1](../../docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#31-receipt-content-enumerated-exactly) enumerates an additional optional command reference; do not import that as an eighth field into the corrected Story 5.3 presentation contract.
- [Epics: Story 5.1](../planning-artifacts/epics.md#story-51-browse-tenant-audit-trail) keeps timestamp, actor, outcome, category, freshness/projection context and reference available at every width. [Story 5.2](../planning-artifacts/epics.md#story-52-reach-scoped-audit-evidence-from-context) entry points cover tenant rows, detail, user lookup/member rows and command results. Preserve origin tab, scope, search, filters, sort, cursor/page, selection and scroll where applicable, plus launcher focus or a heading fallback with a notice.
- [DESIGN: truth-state-badge](../planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#truth-state-badge) and [audit-evidence-receipt](../planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#audit-evidence-receipt): audit available uses success styling; pending Informative, delayed Warning, unavailable Severe and missing support Subtle, always icon plus text. Preserve the named recovery vocabulary: wait, refresh, retry status lookup, inspect audit, continue read-only, request permission, escalate, start correction and restore intended access.

The retained **R1–R4 implementation refinements** below come from
[Story 5.6 reverification](spec-5-6-preview-confirm-and-link-a-tenant-correction-2.md#review-triage-log),
whose source is [the approved Story 5.6 spec](spec-5-6-preview-confirm-and-link-a-tenant-correction.md):
exact eventful committed-sequence confirmation, bounded retention expiry with
old-attempt recovery and no expired redispatch, absent-target role-change
blocking, and original timestamp/explicit intended role/owner-count preview
facts. Preserve all four during regeneration; a generic version-advance rule
must not replace the stricter tenant correction proof contract.

Global correction has its own proof requirement. The planning Story 5.7
postcondition plus advancement/attempt-provenance rule is further constrained
by the [approved Story 5.7 spec](spec-5-7-correct-global-administrator-authority-from-audit-evidence.md):
require the intended target presence/absence and a fresh complete projection
reaching verified eventful command progress beyond the preview baseline.
Presence/absence or generic projection advancement alone is insufficient;
preserve `GlobalAdministratorCorrectionSnapshot`'s exact committed-sequence
checks rather than substituting the older planning shorthand.

## Goal

Enable authorized investigation and compensating access corrections with linked proof. History is immutable; command lifecycle, projection confirmation, and audit evidence remain separate. Historical implementation requires reverification.

## Stories

- Story 5.1: Browse Tenant Audit Trail
- Story 5.2: Reach Scoped Audit Evidence from Context
- Story 5.3: View a Support-Safe Audit Evidence Receipt
- Story 5.4: Understand Audit Availability and Recovery
- Story 5.5: Start a Forward Tenant Correction from Audit Evidence
- Story 5.6: Preview, Confirm, and Link a Tenant Correction
- Story 5.7: Correct Global Administrator Authority from Audit Evidence

## Requirements & Constraints

- Audit is contextual, without another shell entry or global inventory. User context is a hint unless server-filtered.
- Preserve authoritative timestamp/tie-breaker order. Date/category (`Access`/`Administrative`) filters reset paging. Protected opaque cursors bind caller, tenant, and filters; invalidation restarts page 1 with a notice. Never expose cursors or use offset paging.
- Keep loading, empty, filtered-empty, error, stale, degraded, unauthorized, invalid-cursor, and unavailable distinct. Recover explicitly; reuse rows only within identical authorized scope.
- Audit readiness requires Product/Operations approval of dataset shape (500 events), page/filter mix, environment, percentile budgets, test tier, repeatability, and fallback. Claim no numeric budget beforehand; misses activate approved stricter paging/virtualization.
- Receipts contain actor, target, tenant scope, outcome, absolute timestamp, projection marker, and approved reference. Resolve authorized loaded rows only; absent references offer inspect-audit/paging recovery without hidden-page scans. Classify copy for support safety.
- `audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support` differ from proven `audit available`; none enables correction or success treatment. Status, event counts, SignalR, and projection confirmation cannot prove audit evidence.
- Correction requires complete evidence, current projection/lifecycle, authorization, command support, aggregate admission, and safe viewport. Otherwise fail closed visibly. Never edit events/projections/stores or use `undo`, `rollback`, or `hidden edit` copy.

## Technical Decisions

- InteractiveServer BFF gateways own backend access. Use direct Tenants REST reads (`GET /api/tenants/{tenantId}/audit`), EventStore commands/status, and no new endpoints or generic EventStore read routing.
- BFF redaction emits safe localized models. Target precedence: `userId`, `key`, `TenantId`. Raw narrative/payloads, tokens/claims, correlations/MessageIds, ETags/cursors, metadata/diagnostics, and unapproved PII cannot enter component output/serialization, copy, announcements, or logs.
- Use shared typed immutable truth/vocabulary and `ReadModelFreshnessState` (`current`/`stale`/`unknown` on wire). `ServedAt` never represents projection age. Separate confirmed data from intent; preserve badge `audit pending` versus machine `audit_pending`. TenantId/UserId remain literal strings.
- A restore intent uses current membership to select `AddUserToTenant` or `ChangeUserRole`; a role-change intent with an absent target blocks and requires an explicit supported path. Matching role means `already applied`, without dispatch. Require explicit non-`Unknown` role; empty-tenant recovery requires Owner and current global authority.
- Platform scope is fixed: `system` / `global-administrators` / `global-administrators`. Removal evidence maps to grant and vice versa. Fully page presence/count; uncertain count or last-administrator removal blocks without override. Raced rejections remain rejections.
- Dispatch through `POST /api/v1/commands` with one retained ULID attempt ID. Lock `(interactive circuit, AggregateIdentity)` across sibling flows until terminal evidence. Tenant correction's approved bounded retention expiry releases admission but retains the old attempt for available status recovery; a fresh current-state preview may replace it. Never redispatch the expired attempt; refresh/reconnect/duplicates cannot rearm submission.
- Status polling/SignalR nudges trigger re-query. Reuse one snapshot per refresh cycle for conflict, safety/count, confirmation, and proof eligibility. Tenant correction requires verified eventful completion for the exact attempt and `system`/`tenants`/tenant-aggregate scope, with a positive committed end sequence beyond the preview baseline. Only a fresh matching projection reaching that sequence can confirm; generic advancement and unrelated role cycles cannot. Pre-existing state/NoOp is `already applied`; missing/invalid proof is `unable to verify`.
- Bidirectional proof requires re-derivable attempt provenance plus event/scope/target/time-boundary/baseline matching. Coincidence or in-memory association is insufficient; neither record changes.

## UX & Interaction Patterns

- Compose FrontComposer/Fluent UI V5; generic infrastructure remains FrontComposer-owned. Fallbacks: flat audit grid and inline structured preview.
- Preview requires ten items: original reference and absolute timestamp; scope; target; current state and role; intended command/explicit role and owner-count impact; freshness; authorization/admission; recovery; proof expectation; known consequences versus unknowns. Missing items block confirmation.
- Preserve origin context and launcher focus; fallback to origin heading with a notice. Return URLs remain under Tenants routes. Escape/cancel dispatch nothing.
- Preserve safety fields at every width; mobile is read-only reference and unsafe widths visibly disable correction. Require WCAG 2.1 AA, EN/FR whole-string parity with named placeholders, stable selectors, and focused accessibility/responsive evidence.
- Use absolute offset-explicit timestamps, monospace identifiers, field pairs, icon plus text, and dedicated announcement intent: progress polite; failures/blockers/degraded/unverifiable assertive. Provide canonical named recoveries.

## Cross-Story Dependencies

- 5.1 feeds 5.2–5.3; 5.2 uses shared availability without waiting for 5.4 recovery detail.
- 5.5 supplies non-submitting intent to 5.6 using Epic 2 commands. Global-admin correction belongs to 5.7; until complete, show `high-impact flow not ready`.
- 5.7 reuses Epic 4 and Epic 5 evidence/proof foundations. Former 5.8 is folded into 5.6/5.7. Epic 2 owns minimum removal proof.
