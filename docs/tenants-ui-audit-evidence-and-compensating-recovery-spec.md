# Tenants UI Audit Evidence and Compensating Recovery Specification

Owner: Hexalith.Tenants product and UX planning
Status: Phase 2 planning/readiness artifact; Epic 5 implementation complete for the Tenants-owned flat audit, receipt, availability, and tenant-domain correction slice
Last reviewed: 2026-06-29
Story: 9.5 — Specify Audit Evidence and Compensating Recovery UI Patterns

This document supplies the **audit-evidence and compensating-recovery UI patterns** that the Story 9.4 `RemoveUserFromTenant` journey explicitly deferred: the audit entry-point contract, the flat audit DataGrid fallback, the Audit Evidence Receipt content + copy-safety, the delayed/unavailable/missing-support proof distinctions, and the compensating-command recovery language and flow. It **composes — and never redefines** — the truth-state/feedback contract (Story 9.3, `docs/tenants-ui-truth-state-and-action-availability-spec.md`), the operations shell / information architecture (Story 9.2, `docs/tenants-ui-operations-shell-spec.md`), and the FrontComposer/Fluent UI dependency map (Story 9.1, `docs/tenants-ui-frontcomposer-dependency-map.md`). Its purpose is to make access changes **provable after the fact** and corrections **explicit, auditable forward commands** — never a hidden undo.

## Scope and Boundary (read first)

- **Planning/specification only.** Epic 9 is readiness/planning-only. This story produces an audit-evidence and compensating-recovery UI **pattern specification** — not shippable Admin UI. It must not route a Developer agent into product UI delivery until separate Phase 2 implementation stories are created from this spec. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`]
- **This document does NOT** create UI components, Blazor pages/routes, backend endpoints, commands, queries, package references, generated FrontComposer files, domain-contract annotations, generated artifacts, Phase 1 release gates, or submodule pointer changes. The custom components named here (Audit Evidence Receipt, Flat Audit List Fallback, Truth State Badge, Command Lifecycle Panel, Consequence Preview) are **referenced and governed, not implemented**. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.5`]
- **Backend MVP work stays independent of UI dependency readiness.** Missing UI dependencies block or defer future Phase 2 UI rows; they never block backend package/release work. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- **Reconcile, do not duplicate.** Dependency IDs, per-row `blockedBy`/`backendEvidence` arrays, the truth-state badge vocabulary, freshness gating, the unavailable-action reason taxonomy, the layered feedback states, the non-collapse invariant, and the "never label recovery as undo" rule are owned by the existing UI docs. This spec references and copies them verbatim; it does not re-enumerate or contradict them. [Source: Story 9.1 senior review; `docs/tenants-ui-truth-state-and-action-availability-spec.md`; `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`]

### Post-Epic 5 implementation status

Epic 5 implemented this pattern set as Tenants-owned UI under `src/Hexalith.Tenants.UI`: `TenantAuditPage`, `Components/Tenants/Audit/AuditDataGrid`, `AuditEvidenceReceipt`, `AuditAvailabilityState`, `CorrectionStartPanel`, and the `State/TenantAudit/TenantCorrection*` models. The implementation keeps the reusable FrontComposer `<AuditTimeline>` dependency visible as deferred, uses the approved flat DataGrid fallback, composes receipts from `TenantAuditEntry`, submits tenant-domain corrections through the existing command gateway, confirms success by projection re-query, and links original/corrective proof from support-safe audit rows. Epic 4 now supplies global-administrator grant/remove command support; Story 5.7 is the current verification gate before audit-based global-administrator correction may enable. Until that gate is complete, the live global-administrator correction path must fail closed unless fixed-scope read evidence, command support, authorization, freshness, target visibility, projection confirmation, and proof lookup are all available.

### Why a new artifact

No audit-evidence/compensating-recovery pattern specification existed in `docs/` before this story; the directory held only the dependency map (`tenants-ui-frontcomposer-dependency-map.md`), the operations-shell spec (`tenants-ui-operations-shell-spec.md`), the Phase 2 backlog (`tenants-ui-phase-2-story-backlog.md`), the truth-state spec (`tenants-ui-truth-state-and-action-availability-spec.md`), and the remove-user journey spec (`tenants-ui-remove-user-from-tenant-journey-spec.md`) for UI. Story 9.4's remove-user journey **explicitly defers** the full Audit Evidence Receipt, the flat audit DataGrid fallback, and the compensating-recovery UI patterns to this story (9.4 §6.2). This spec adds a **cross-cutting audit/recovery pattern layer** on top of the existing sources of truth rather than a parallel dependency map, navigation model, or truth-state vocabulary. Story 9.1's review penalized an unnecessary parallel artifact and reverted a wrong endpoint normalization; this spec composes the existing sources of truth and copies their identifiers verbatim to avoid repeating that mistake. [Source: `docs/tenants-ui-remove-user-from-tenant-journey-spec.md#6.2 Deferred to Story 9.5`; `_bmad-output/implementation-artifacts/9-1-map-fluent-ui-and-frontcomposer-dependencies-for-tenant-admin-screens.md#Senior Developer Review (AI)`]

### This spec composes existing patterns — it does NOT redefine them

- **Truth/feedback contract (Story 9.3).** `docs/tenants-ui-truth-state-and-action-availability-spec.md` is the canonical owner of the Truth State Badge vocabulary (§2), the layered feedback states including the audit-evidence states `audit pending` / `audit available` / `delayed` / `unavailable` / `approved fallback` (§1, §5.1), the `missing audit proof → FC-AUD` reason mapping (§4.4), the non-collapse invariant (§5.2), the concurrency/recovery table (§5.4), and the "never label recovery as undo" rule (§5.4). This spec **applies** those to the audit/recovery patterns; it must not redefine badge states, feedback states, reason categories, or the non-collapse invariant.
- **Operations shell / IA (Story 9.2).** `docs/tenants-ui-operations-shell-spec.md` §1.2/§6 owns the audit **entry points** (global nav, tenant rows, tenant detail, user lookup, command result) and the rule that audit is never the only way to reach proof but is always reachable from any access-review context. Tenant/user scope is preserved across entry points; audit and command lifecycle are never promoted into a separate primary navigation model.
- **Dependency map (Story 9.1).** `docs/tenants-ui-frontcomposer-dependency-map.md` owns the 10 fixed dependency IDs (`FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC`) and the `FC-AUD: missing` audit-timeline evidence. This spec reuses them verbatim and adds none.
- **Remove-user journey (Story 9.4).** `docs/tenants-ui-remove-user-from-tenant-journey-spec.md` references *audit expectation* (its §2.1 item 8) and *recovery path* (its §2.1 item 7, §5.3) and defers their full component patterns here. This spec keeps the receipt/recovery patterns consistent with the 9.4 journey's `audit_pending` / `audit_available` states and its support-safe command reference.

### Three critical guardrails this spec must not violate

1. **The Audit Evidence Receipt composes from the existing read model — no new backend endpoint.** `TenantAuditEntry` (`src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`) already exposes the receipt fields; the projection marker and support-safe command reference come from the read-model freshness marker and the client-tracked `FC-CMD` command lifecycle. Specifying a new backend "receipt" or "consequence" endpoint would contradict the architecture. `NarrativePayload` is a structured, support-safe narrative — **not** the raw persisted event payload. [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
2. **Corrections are forward compensating commands — there is no undo (do NOT invent one).** "Restore intended access" = a new `AddUserToTenant` (or role) command; "start correction" = a new explicit command with its own consequence preview and proof. The original event remains in the immutable audit trail. `TenantAggregate` does **NOT** enforce a "must retain ≥1 owner" invariant — last-owner removal is allowed by design; the relevant boundary is the empty-tenant bootstrap path (`AddUserToTenant` skips owner-only RBAC when `state.HasMembershipHistory == false`). Do NOT invent a "≥1 owner" backend invariant or an undo/rollback endpoint. [Source: `_bmad-output/project-context.md#Aggregates`; `_bmad-output/project-context.md#Domain Correctness`]
3. **`tenants` and `global-administrators` are distinct domains.** Tenant-membership corrections target the `tenants` domain (`AddUserToTenant` / `RemoveUserFromTenant`, `AggregateId` = managed tenant ID). Platform global-administrator corrections are a separate command on the singleton `global-administrators` domain (`SetGlobalAdministrator` / `RemoveGlobalAdministrator`, backlog `ui-15`). Recovery copy must keep these distinct; a global-admin authority correction does not edit a tenant aggregate. [Source: `_bmad-output/project-context.md#Identity Scheme`; `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories` (`ui-15`)]

### Numbering hazard (read carefully)

This story's key is the sprint-status Epic 9 UI-planning key `9-5-specify-audit-evidence-and-compensating-recovery-ui-patterns`. The `backendEvidence`/`evidenceSource` arrays inside `docs/tenants-ui-phase-2-story-backlog.md` and the dependency map (e.g. `ui-11` cites `post-epic-5-r5a3-tenant-audit-projection-query`, `10-2-audit-projection-write-safety`; `FC-AUD` is sourced from `12-2-audit-timeline-and-consequence-preview-readiness`) reference a **separate Phase 2 backend/FrontComposer backlog** that also uses `9-x`/`10-x`/`11-x`/`12-x` keys. Those are NOT this epic's stories; do not conflate the two namespaces or mark backend rows complete based on this UI story. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`; `_bmad-output/implementation-artifacts/sprint-status.yaml`]

## 1. Audit Entry Points and the Missing-Capability Fallback Rule (AC1)

### 1.1 The five audit entry points (verbatim from the shell spec)

Audit context is reachable from exactly these five entry points, owned by Story 9.2's operations shell (§1.2, §6):

1. **Global navigation** — the **Audit** area of the Operations Shell primary navigation.
2. **Tenant rows** — a tenant row in the tenant list links to audit scoped to that tenant.
3. **Tenant detail** — the tenant-detail audit section/tab, scoped to the selected tenant.
4. **User lookup** — a user-lookup context links to audit scoped to that user.
5. **Command result** — a completed/partially-completed command result links to audit for the affected tenant/user.

Audit is **always reachable from any access-review context but never the only way to reach proof.** Audit and command lifecycle are never promoted into a separate primary navigation model. [Source: `docs/tenants-ui-operations-shell-spec.md#1.2`; `#6. Audit Entry-Point Context`; `_bmad-output/planning-artifacts/ux-design-specification.md#Navigation Patterns`; AC1]

### 1.2 Each entry point carries scope (scoped query, not a global jump)

Each entry point must carry the relevant **tenant and/or user scope** into the audit context as a scoped query — not a global, unscoped jump. The tenant-row and tenant-detail entry points carry tenant scope; the user-lookup entry point carries user scope; the command-result entry point carries the affected tenant/user scope. Authorization filtering lives in projection/query handling, not in the UI. [Source: `docs/tenants-ui-operations-shell-spec.md#1.3 Navigation map`; `_bmad-output/project-context.md#API Surface`; AC1]

### 1.3 Missing audit capability surfaces a documented fallback or blocked dependency

When audit capability is missing or unproven, an entry point must surface a **documented fallback or a blocked dependency** — never a dead link, a silent no-op, or a fake-success state. The unavailable case maps to:

- Dependency tie **`FC-AUD` (`missing`)** — no verified FrontComposer `<AuditTimeline>` source path exists.
- Unavailable-action reason **`missing audit proof`** (Story 9.3 §4.1/§4.4), surfaced as a **visible inline reason** (a tooltip may supplement, but cannot be the only explanation — 9.3 §4.2).

[Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §4.1, §4.2, §4.4; `docs/tenants-ui-frontcomposer-dependency-map.md#FC-AUD`; AC1]

## 2. Flat Audit DataGrid Fallback and Visible Timeline Dependency Status (AC2)

### 2.1 The approved first-slice fallback is a flat audit DataGrid

Because the reusable `<AuditTimeline>` component is **`missing`** (`FC-AUD`; no verified FrontComposer source path), the **approved first-slice fallback is a flat audit DataGrid** (built on the available `FC-TBL` DataGrid primitive). The fallback must provide all of these distinct states:

- **stable ordering** (deterministic, cursor-stable row order);
- **date/type filters** (date range and `AuditEventCategory` — `Access` / `Administrative`);
- **loading**, **empty**, **filtered-empty**, **error** states; and
- **accessible expansion** of a row to its detail.

**Do NOT claim an `<AuditTimeline>` component exists.** The fallback must describe equivalent loading feedback and must not assert timeline availability. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Flat Audit List Fallback`; `#Empty, Loading, Stale, and Degraded States`; `docs/tenants-ui-frontcomposer-dependency-map.md#FC-AUD`; `#Audit fallback`; AC2]

### 2.2 The reusable timeline dependency status stays visible

The reusable timeline dependency status (`FC-AUD: missing`; grouped-mode fast-follow) must **remain visible in the dependency map / backlog** so the flat-DataGrid fallback is recorded as a **deferred decision with an owner and a replacement path** — never silently treated as the permanent design. Grouped audit mode (session grouping) is a sub-state of `FC-AUD`, stays **fast-follow**, and must not be promoted into the first slice unless product/UX explicitly approves it. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Story 12.2 Audit and Consequence Readiness`; `docs/tenants-ui-phase-2-story-backlog.md#Deferred Decisions`; `#Scope Boundary`; AC2]

### 2.3 Source: the existing read endpoint only

The audit list/fallback is sourced **only** from the existing read endpoint `GET /api/tenants/{tenantId}/audit` (handler `GetTenantAuditQuery`, domain `tenants`, projection `tenants`; rows shaped as `TenantAuditEntry`). Constraints:

- **Cursor-based pagination only** — signed, opaque, scope-bound cursors. **Never offset/limit.**
- **500-event evidence target** (or an approved bounded-rendering fallback — virtualization/windowing is expected unless component-test/benchmark evidence proves a simple flat render meets the target).
- ETag `If-None-Match` → `304 Not Modified` is the freshness primitive served by the Tenants REST read endpoints through `TenantsQueryController` and in-process query handlers, using read-model ETag/freshness metadata.
- **No new audit/read endpoint** is introduced.

[Source: `_bmad-output/project-context.md#API Surface`; `src/Hexalith.Tenants.Contracts/Queries/GetTenantAuditQuery.cs`; `docs/tenants-ui-phase-2-story-backlog.md` (`ui-11`); `docs/tenants-ui-frontcomposer-dependency-map.md` (500-event target)]

## 3. Audit Evidence Receipt Content and Copy-Safety (AC3)

### 3.1 Receipt content (enumerated exactly)

When a meaningful access change completes (or partially completes), the Audit Evidence Receipt presents exactly:

1. **actor** — who acted.
2. **target** — the affected user/key.
3. **tenant scope** — the managed tenant the change applies to.
4. **outcome** — what happened.
5. **timestamp** — when it was recorded.
6. **projection marker** — the read-model freshness/version marker behind the visible state.
7. **audit reference** — the support-safe audit event reference.
8. **support-safe command reference** — **where available** (from the client-tracked command lifecycle).

[Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Audit Evidence Receipt`; AC3]

### 3.2 The receipt composes from the existing read model (no new endpoint)

**Critical sourcing insight — do NOT add a backend "receipt" or "consequence" endpoint.** The receipt composes from the existing audit read model and the client command lifecycle:

| Receipt field | Source |
| --- | --- |
| actor | `TenantAuditEntry.ActorId` |
| target | `TenantAuditEntry.Target` (resolved from `NarrativePayload` `userId`/`key`, falling back to `TenantId`) |
| tenant scope | `TenantAuditEntry.Scope` / `TenantId` |
| outcome | `TenantAuditEntry.Outcome` / `EventType` (with `Category`: `Access` / `Administrative`) |
| timestamp | `TenantAuditEntry.Timestamp` |
| audit reference | `TenantAuditEntry.EventId` |
| projection marker | read-model freshness marker (timestamp / projection version / ETag → `304`) |
| support-safe command reference | client-tracked `FC-CMD` command lifecycle (where available) |

The projection marker and support-safe command reference come from the read-model freshness marker and the client-tracked `FC-CMD` command lifecycle — **not** a backend "receipt" or "consequence" endpoint; command lifecycle is client-side. [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]

### 3.3 Copy-safety rule

Copyable references must **never expose** raw command/event payloads, bearer tokens, stack traces, internal correlation/message ids, internal exception text, raw EventStore metadata, or PII. Reference content is limited to **support-safe tokens**: a support-safe command reference, a tenant/user reference, the projection version/freshness marker, the accepted timestamp, and the audit event reference or fallback state. `NarrativePayload` is a structured, **support-safe** narrative — not the raw persisted event payload — but the copy-safety rule still holds for everything surfaced in a copyable reference. [Source: `docs/tenants-ui-operations-shell-spec.md#5.3 Support-safe references`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §9.1; `_bmad-output/project-context.md#Logging & Telemetry`; `#Rejection Event Payloads`; AC3]

### 3.4 Partial-completion case

When an access change **partially** completes (accepted but projection/audit not yet reconciled), the receipt must show the **actual lifecycle state** (e.g. `audit pending`) rather than a finished receipt. This applies the Story 9.3 non-collapse invariant (§5.2): `accepted`, `projected` (confirmed), and `proven` (audit available) are never merged into a single "success". The receipt reflects whichever layered state is currently true; it does not pre-render proof that does not yet exist. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §5.2; AC3]

## 4. Delayed/Unavailable Audit-Proof States and the No-False-Success Rule (AC4)

### 4.1 Four distinct states (do not collapse)

The UI distinguishes four states explicitly and must not collapse "delayed proof" with "no implementation support":

| State | Meaning |
| --- | --- |
| **audit pending** | Visible access state is updated, but audit proof is not yet available (expected to arrive). |
| **audit delayed** | Audit proof is taking longer than expected, but the capability exists. |
| **audit unavailable** | The audit evidence path is currently unavailable (e.g. read error) — distinct from not-yet-built. |
| **missing implementation support** | Audit capability is **not built** (`FC-AUD` not ready); the entry point shows the `missing audit proof` blocked dependency (§1.3). |

These compose the Story 9.3 audit-evidence states (`audit pending` / `audit available` / `delayed` / `unavailable` / `approved fallback`) and the §5.1 layered feedback set; this spec references them, it does not redefine them. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Empty, Loading, Stale, and Degraded States` ("Audit unavailable: distinguish delayed evidence from missing implementation support"); `docs/tenants-ui-truth-state-and-action-availability-spec.md` §1, §5.1; AC4]

### 4.2 No-false-success rule

When proof cannot be verified, the UI **avoids success language** and offers concrete next actions: **wait/refresh**, **retry status lookup**, **inspect audit later**, **continue read-only**, **cite a support-safe reference**, or **escalate**. The non-collapse invariant is reused: `accepted`, `confirmed`, and `audit available` are never merged into one "success". [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §5.2, §6.3; `_bmad-output/planning-artifacts/ux-design-specification.md#Feedback Patterns`; AC4]

### 4.3 SignalR is a freshness nudge only

SignalR projection notifications are a **freshness nudge only.** A nudge may prompt a re-query but must **never** advance audit evidence to `audit available` or imply that proof exists. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §3.4; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; AC4]

## 5. Compensating-Recovery Language and Flow (AC5)

### 5.1 Language rule: explicit compensating-command terms, never undo

Recovery uses explicit **compensating-command terms** — "start correction", "restore intended access", "retry status lookup", "inspect audit", "escalate" — and is **never** labeled as undo, rollback, or hidden edit. The original event remains in the immutable audit trail; the correction is a **new** explicit command with its own consequence preview and proof. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Compensating Recovery`; `#2.5 Experience Mechanics`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §5.4; AC5]

### 5.2 Compensating-recovery flow (UX Journey 4)

From audit evidence detail, on discovering a wrong access change:

1. **Start the compensating command** ("start correction" / "restore intended access") from the audit evidence detail.
2. **Preview the correction against the *current* state** — a fresh consequence preview, not the historical state.
3. **Submit a new command** and **link both audit records** (the original event and the correction).

```text
Open audit context
  -> filter by tenant / user / event type / date
  -> review actor, target, scope, outcome, timestamp
  -> evidence complete and safe to cite?
       no  -> show delayed/unavailable proof state -> retry / adjust filter / wait / escalate
       yes -> open evidence detail
                -> wrong access change found?
                     no  -> copy support-safe reference
                     yes -> start compensating command
                              -> preview correction against current state
                              -> submit new command and link both audit records
```

Useful recovery paths: **reassign tenant owner**, **restore intended access** via a new `AddUserToTenant` command, **retry access removal**, **open audit evidence**, or **escalate with a support-safe reference**. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Journey 4: Audit Evidence and Compensating Recovery`; `#2.5 Experience Mechanics`; AC5]

### 5.3 Corrections are forward commands, not state edits

**Critical backend truth.** "Restore intended access" = a new `AddUserToTenant` (or role) command dispatched to `POST /api/v1/commands` — there is **no undo/rollback API**, and the UI must not imply one. The empty-tenant bootstrap boundary is relevant to the restore-after-last-owner-removal narrative: `AddUserToTenant` skips owner-only RBAC when `state.HasMembershipHistory == false`, and **last-owner removal is allowed by design** (`TenantAggregate` enforces no "must retain ≥1 owner" invariant). Do NOT invent a "≥1 owner" backend invariant or an undo endpoint. The command surface is `POST /api/v1/commands` (FrontComposer `EventStoreOptions.CommandEndpointPath` default); `project-context.md` records the unversioned `POST /api/commands` `CommandsController` as an **alias** to confirm against the deployed gateway, not to assume. [Source: `_bmad-output/project-context.md#Aggregates`; `#Domain Correctness`; `docs/tenants-ui-frontcomposer-dependency-map.md#Command Endpoint Route Evidence`; AC5]

### 5.4 Keep command domains distinct in recovery copy

Tenant-membership corrections target the **`tenants`** domain (`AddUserToTenant` / `RemoveUserFromTenant`, `AggregateId` = managed tenant ID). Platform global-administrator corrections are a **separate** command on the singleton **`global-administrators`** domain (`SetGlobalAdministrator` / `RemoveGlobalAdministrator`, backlog row `ui-15`). Recovery copy must keep these distinct; a global-admin authority correction does not edit a tenant aggregate. [Source: `_bmad-output/project-context.md#Identity Scheme`; `docs/tenants-ui-phase-2-story-backlog.md` (`ui-15`)]

## 6. Per-Pattern Consumption Mapping (planning-only / blocked)

Each pattern names the consuming backlog rows and copies the relevant `blockedBy`/`backendEvidence` dependency arrays **verbatim** from `docs/tenants-ui-phase-2-story-backlog.md`. This historical Story 9.5 specification does not promote rows by itself; current implementation evidence may supersede row-level readiness while reusable FrontComposer component work remains blocked. No new dependency IDs, column names, or `ui-NN` keys are introduced. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`]

### 6.1 Audit-surface consuming rows (`ui-11`, `ui-12`)

- **`ui-11-audit-trail-flat-timeline`** — status `blocked`; `fallbackDecision` `deferred`; `fallbackNotes` `DataGrid-backed-flat-audit-list`; owner `Tenants Product/UX + Hexalith.FrontComposer`.
  - `blockedBy` (verbatim): `[FC-LYT, FC-AUD, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]`
  - `backendEvidence` (verbatim): `[post-epic-5-r5a3-tenant-audit-projection-query, 9-1-opaque-signed-query-cursors, 9-2-stable-cursor-pagination-under-role-and-membership-changes, 9-4-actor-layer-query-guardrails, 10-2-audit-projection-write-safety, 10-4-projection-write-conformance-and-recovery-tests, endpoint:GET /api/tenants/{tenantId}/audit]`
- **`ui-12-tenant-detail-audit-tab`** — status `blocked`; `fallbackDecision` `deferred`; `fallbackNotes` `flat-audit-summary-policy`; owner `Tenants Product/UX + Hexalith.FrontComposer`.
  - `blockedBy` (verbatim): `[FC-LYT, FC-AUD, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]`
  - `backendEvidence` (verbatim): `[post-epic-5-r5a3-tenant-audit-projection-query, 10-2-audit-projection-write-safety, 10-4-projection-write-conformance-and-recovery-tests, endpoint:GET /api/tenants/{tenantId}/audit]`

Epic 5 Stories 5.1 through 5.4 supersede those historical blocked rows for the delivered Tenants-owned flat audit, scoped entry-point, receipt, and availability-state slices. The rows stay blocked only for reusable FrontComposer `<AuditTimeline>` / grouped-mode work and future tenant-detail audit-tab reuse.

### 6.2 Cross-cutting consuming rows (receipt + compensating recovery)

The Audit Evidence Receipt and compensating-recovery patterns are **cross-cutting** and are also consumed by the command rows `ui-13-disable-or-enable-tenant`, `ui-14-user-management-remove-user`, and `ui-15-global-admin-command-management`. Epic 3 implementation evidence now supersedes the older blocked status for `ui-13`: FR15 lifecycle disable/enable uses the approved inline consequence-preview fallback and honest audit handoff states. Story 2.4 implementation evidence supersedes the older blocked status for the delivered `ui-14` remove-member flow, while the reusable consequence-preview / batching row remains blocked for FrontComposer work. Epic 4 Stories 4.3 and 4.4 supersede the older blocked status for `ui-15` global-administrator grant/remove command management; Story 5.7 is the separate verification gate for audit-based global-administrator correction. Reference the current Phase 2 backlog before copying row arrays. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`; `_bmad-output/implementation-artifacts/sprint-status.yaml`]

### 6.3 Pattern → dependency map

| Pattern | Custom component (used, not implemented) | Dependency tie | Readiness |
| --- | --- | --- | --- |
| Audit entry points + missing-capability fallback (§1) | (navigation; shell-owned) | `FC-AUD` (`missing`) / `missing audit proof` reason | blocks audit rows via `FC-AUD` |
| Flat audit DataGrid fallback (§2) | Flat Audit List Fallback (DataGrid via `FC-TBL`) | `FC-AUD` (`missing`), `FC-TOK` (`missing` for timeline), `FC-LYT`, `FC-A11Y`, `FC-L10N`, `FC-DOC` | Delivered for the Tenants-owned first slice by Epic 5; reusable timeline/grouped-mode rows still block through `ui-11`/`ui-12` |
| Audit Evidence Receipt content + copy-safety (§3) | Audit Evidence Receipt | `FC-AUD`, `FC-CMD` (where produced from a command outcome), `FC-TOK` | Delivered for Tenants-owned audit evidence by Epic 5; future command rows must cite that story evidence or equivalent proof |
| Delayed/unavailable/missing-support proof (§4) | Truth State Badge / Command Lifecycle Panel audit states (9.3) | `FC-AUD`, `FC-TOK`, `FC-A11Y`, `FC-L10N` | referenced from 9.3, not redefined |
| Compensating-recovery language + flow (§5) | (forward command via `FC-CMD`; preview via `FC-CNS`) | `FC-CMD`, `FC-CNS` (`missing`), `FC-CNC` | Delivered for tenant-domain correction by Epic 5; global-administrator correction remains gated by Story 5.7 verification |
| Accessibility / localization / docs (§7) | (cross-cutting) | `FC-A11Y`, `FC-L10N`, `FC-DOC` | blocks all rows |

Rows that depend on reusable audit timeline evidence still stay blocked where `FC-AUD` is missing. Older command stories deliberately stopped at honest audit handoff states (`audit pending`, `audit delayed`, `audit unavailable`, or `missing implementation support`) before Epic 5 evidence sources existed; current and future stories must cite Epic 5 receipt/proof evidence before rendering receipts, and must never fabricate proof from command acceptance or SignalR. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`; `#Blocked`; `_bmad-output/implementation-artifacts/tests/test-summary.md`]

### 6.4 Implementation Story Rules (what a future UI story must satisfy)

A future Phase 2 UI story may implement these audit/recovery patterns only when it can name **all** of the following (it fails closed otherwise). These are acceptance criteria for the UX promise, not implementation preferences. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Story Rules`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §10]

1. **Source projection/query** — `GET /api/tenants/{tenantId}/audit` (`GetTenantAuditQuery`) for the audit surface.
2. **Freshness state shown** — projection marker / freshness marker (ETag → `304`); unknown fails closed.
3. **Authorization state and unavailable-action reason** — six reason categories kept distinct; `missing audit proof` for audit gaps; inline-visible reason, tooltip supplements only.
4. **Audit-evidence path or approved flat-DataGrid fallback** — the §2 flat audit DataGrid when `FC-AUD` is `missing`.
5. **Audit Evidence Receipt content + copy-safety** — the §3 content set composed from `TenantAuditEntry` + read-model freshness + client `FC-CMD` lifecycle; copy-safe.
6. **Distinct delayed/unavailable/missing-support states** — the four §4 states, with the no-false-success rule.
7. **Compensating-recovery language** — explicit compensating-command terms; never undo; forward command with its own preview and proof.
8. **Support-safe observability references** — no raw payloads, tokens, stack traces, internal correlation/message ids, or PII.
9. **Accessibility behavior** — focus, keyboard scan/expand/copy/start-exit-correction, live status, disabled explanations, no-color-only treatment.
10. **Localization responsibility** — state labels, timestamps, outcomes, audit-state words, and recovery actions localizable; no runtime sentence-fragment assembly.

### 6.5 Out-of-scope for the first slice

Grouped audit mode (session grouping), server-side anomaly scoring, and advanced analytics stay **out of the first slice (fast-follow)** unless product explicitly promotes them. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Scope Boundary`; `docs/tenants-ui-frontcomposer-dependency-map.md#Story 12.2 Audit and Consequence Readiness`]

## 7. Support-Safe References, Accessibility, and Localization Contracts

Shared with the other Epic 9 stories; required by every pattern in this spec.

### 7.1 Support-safe references

Support-safe references for audit/command troubleshooting must **never expose** raw payloads, bearer tokens, stack traces, internal correlation/message ids, or PII. Reference content is limited to non-sensitive, support-safe tokens (a support-safe command reference, tenant/user reference, projection version/freshness marker, accepted timestamp, audit event reference or fallback state). [Source: `docs/tenants-ui-operations-shell-spec.md#5.3`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §9.1; `_bmad-output/project-context.md#Logging & Telemetry`]

### 7.2 Accessibility (ties to `FC-A11Y`)

Audit availability/delay states, receipt content, flat-list row expansion, and compensating-recovery prompts must be **perceivable without color** and **announced via live status**. Keyboard users must be able to **scan the audit list, expand rows, copy a support-safe reference, and start/exit a compensating-command flow.** All badge/feedback states pair text with an icon or shape and work in forced-colors mode. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Accessibility`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §9.2; AC1-5]

### 7.3 Automation selectors (ties to `FC-A11Y` / `FC-DOC`)

Automation of audit-state and receipt assertions must rely on **stable selectors or component contracts**, never arbitrary row text. This ties to `FC-A11Y` (accessible names, stable roles) and `FC-DOC` (component-reference evidence). [Source: `docs/tenants-ui-operations-shell-spec.md#5.2`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §9.3; AC2-5]

### 7.4 Localization (ties to `FC-L10N`)

All state labels, timestamps, outcomes, audit-state words, and recovery actions are localizable; no sentence-fragment assembly at runtime. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §9.4; AC2-5]

## 8. Backend and Data Boundaries

- Audit evidence comes only from the existing read endpoint `GET /api/tenants/{tenantId}/audit` (`GetTenantAuditQuery`, rows = `TenantAuditEntry`). Cursor-based pagination only; cursors are signed, opaque, and scope-bound, never offset/limit. ETag `If-None-Match` → `304` is the freshness primitive served by the REST-backed Tenants audit read path and read-model metadata. **No new audit/receipt/consequence endpoint.** [Source: `_bmad-output/project-context.md#Domain, Eventing & Framework Rules`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- The Audit Evidence Receipt composes from `TenantAuditEntry` + the read-model freshness marker + the client-tracked `FC-CMD` command lifecycle. `NarrativePayload` is a structured, support-safe narrative — not the raw persisted event payload. [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- Compensating commands dispatch to `POST /api/v1/commands` (e.g. `command:AddUserToTenant` for "restore intended access"). Command lifecycle is tracked client-side through the Tenants BFF command gateway and command feedback state; SignalR notifications are refresh nudges only, never proof. There is no undo/rollback API. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/project-context.md#API Surface`]
- Rejection events carry structured data only; user-facing text is composed at the HTTP boundary by EventStore's domain-rejection ProblemDetails handling/catalog (RFC 7807: 404 not-found, 409 conflict, 422 other). Audit/recovery copy must map rejections to safe localized text — never raw payloads, stack traces, tokens, internal correlation/message ids, or internal exception text. [Source: `_bmad-output/project-context.md#API Surface`; `#Rejection Event Payloads`]
- Identity: use JWT `sub` (`envelope.UserId`) for actor identity, never `name`/`email`. Tenant ids and user ids are meaningful caller-supplied strings, not GUIDs or ULIDs; EventStore message/event references may be ULID-like and remain support-safe only when explicitly classified. Authorization is enforced server-side (L1 API gate + L2 domain RBAC); the UI reflects authorization state and unavailable-action reasons but is not the authorization boundary. [Source: `_bmad-output/project-context.md#Identity Scheme`; `#Authorization (RBAC)`]
- Last-owner removal is allowed by design (`TenantAggregate` enforces no "≥1 owner" invariant); the empty-tenant bootstrap path (`AddUserToTenant` skips owner-only RBAC when `state.HasMembershipHistory == false`) is the relevant boundary for restore-after-last-owner-removal. Do NOT invent a "≥1 owner" invariant. [Source: `_bmad-output/project-context.md#Aggregates`; `#Domain Correctness`]
- Preserve root-declared submodule under `references/` policy. Reading `Hexalith.FrontComposer` is allowed; never initialize nested submodules or run recursive submodule commands. [Source: `CLAUDE.md#Submodule Policy`]

## 9. Acceptance Criteria Traceability

| AC | Requirement | Spec section |
| --- | --- | --- |
| AC1 | Five audit entry points (global nav, tenant rows, tenant detail, user lookup, command result), each scoped; audit always reachable but never the only proof path; missing capability = documented fallback or blocked dependency (`FC-AUD` / `missing audit proof`) | §1 |
| AC2 | Approved flat audit DataGrid fallback (stable ordering, date/type filters, loading, empty, filtered-empty, error, accessible expansion); reusable timeline status (`FC-AUD: missing`, grouped-mode fast-follow) stays visible as a deferred decision; sourced from `GET /api/tenants/{tenantId}/audit`, cursor-based, 500-event target | §2 |
| AC3 | Receipt content set (actor, target, tenant scope, outcome, timestamp, projection marker, audit reference, support-safe command reference where available); composes from `TenantAuditEntry` + freshness + client lifecycle; copy-safe (no raw payloads/tokens/traces/internals); partial-completion shows actual lifecycle state | §3 |
| AC4 | Four distinct states — audit pending / audit delayed / audit unavailable / missing implementation support; no-false-success rule; SignalR nudge-only | §4 |
| AC5 | Explicit compensating-command terms (start correction, restore intended access, …); never undo/rollback/hidden edit; forward-command recovery flow (preview against current state, link both audit records); distinct `tenants` vs `global-administrators` domains | §5 |

## 10. References

- `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`
- `_bmad-output/planning-artifacts/epics.md#Story 9.5: Specify Audit Evidence and Compensating Recovery UI Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Journey 4: Audit Evidence and Compensating Recovery`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Audit Evidence Receipt`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Flat Audit List Fallback`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Compensating Recovery`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Empty, Loading, Stale, and Degraded States`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Feedback Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#2.5 Experience Mechanics`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Story Rules`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Navigation Patterns`
- `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`
- `docs/tenants-ui-truth-state-and-action-availability-spec.md` (§1, §4.1, §4.2, §4.4, §5.1, §5.2, §5.4, §3.4, §6.3, §9.1, §9.2, §9.3, §9.4, §10)
- `docs/tenants-ui-operations-shell-spec.md` (§1.2, §1.3, §5.2, §5.3, §6)
- `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog` (`FC-AUD`); `#Story 12.2 Audit and Consequence Readiness`; `#Command Endpoint Route Evidence`; `#Audit fallback`
- `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories` (`ui-11`, `ui-12`, `ui-13`, `ui-14`, `ui-15`); `#Deferred Decisions`; `#Scope Boundary`
- `docs/tenants-ui-remove-user-from-tenant-journey-spec.md` (audit/recovery patterns deferred to Story 9.5)
- `_bmad-output/implementation-artifacts/9-1-map-fluent-ui-and-frontcomposer-dependencies-for-tenant-admin-screens.md#Senior Developer Review (AI)`
- `_bmad-output/implementation-artifacts/9-4-specify-the-removeuserfromtenant-command-capable-journey.md`
- `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`; `src/Hexalith.Tenants.Contracts/Queries/GetTenantAuditQuery.cs`; `src/Hexalith.Tenants.Contracts/Enums/AuditEventCategory.cs`
- `_bmad-output/project-context.md#Aggregates`
- `_bmad-output/project-context.md#Domain Correctness`
- `_bmad-output/project-context.md#Identity Scheme`
- `_bmad-output/project-context.md#API Surface`
- `_bmad-output/project-context.md#Logging & Telemetry`
- `_bmad-output/project-context.md#Rejection Event Payloads`
