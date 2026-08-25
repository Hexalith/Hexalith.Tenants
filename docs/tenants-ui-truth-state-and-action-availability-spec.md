# Tenants UI Truth State, Freshness, and Action-Availability Specification

Owner: Hexalith.Tenants product and UX planning
Status: Phase 2 planning/readiness artifact with Epic 2 implementation notes
Last reviewed: 2026-06-29
Story: 9.3 — Define Truth State, Freshness, and Unavailable Action Patterns

This document is the **canonical truth/feedback contract** for the Phase 2 Tenants Admin UI. It defines the Truth State Badge vocabulary, the freshness-gating rules for access-impacting actions, the Unavailable Action Reason taxonomy, the layered command/projection/audit feedback state set, and the feedback-placement/degradation rules — plus a per-pattern consumption mapping back to the existing source-of-truth artifacts. Every Phase 2 UI implementation story must consume these patterns so that "current", "accepted", "confirmed", and "audited" are never reinterpreted per screen.

## Scope and Boundary (read first)

- **Planning/specification only.** Epic 9 is readiness/planning-only. This story produces a pattern specification, not shippable Admin UI. It must not route a Developer agent into product UI delivery until separate Phase 2 implementation stories are created from this spec. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`]
- **This document does NOT** create UI components, Blazor pages/routes, backend endpoints, commands, queries, package references, generated FrontComposer files, domain-contract annotations, generated artifacts, Phase 1 release gates, or submodule pointer changes. The custom components named here (Truth State Badge, Freshness Gate, Unavailable Action Reason, Command Lifecycle Panel) are **governed, not implemented**. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.3`]
- **Backend MVP work stays independent of UI dependency readiness.** Missing UI dependencies block or defer future Phase 2 UI rows; they never block backend package/release work. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- **Reconcile, do not duplicate.** Dependency IDs and per-row `blockedBy` arrays are owned by `docs/tenants-ui-frontcomposer-dependency-map.md` and `docs/tenants-ui-phase-2-story-backlog.md`. This spec references and copies them verbatim; it does not redefine `FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC`, and it invents no new dependency IDs, column names, or `ui-NN` keys. [Source: Story 9.1 senior review; `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`]

### Why a new artifact

No truth-state/action-availability specification existed in `docs/` before this story; the directory held only the dependency map, the operations-shell spec, and the Phase 2 backlog for UI. Story 9.2's `docs/tenants-ui-operations-shell-spec.md` §"Truth-State Vocabulary (referenced, not redefined)" **explicitly defers ownership of the full truth-state vocabulary and freshness gating to this story (9.3)**. This spec is therefore the canonical owner. It adds a truth-state/feedback **pattern layer** on top of the existing information-architecture spec rather than a parallel dependency map.

### Two fixed claims this spec must not contradict

Story 9.2's shell spec records two fixed claims that this spec makes canonical and never contradicts:

1. The freshness primitive for every read surface is the `If-None-Match` → `304 Not Modified` ETag pre-check served by the Tenants REST read endpoints through `TenantsQueryController` and in-process query handlers, using read-model ETag/freshness metadata. [Source: `docs/tenants-ui-operations-shell-spec.md#Truth-State Vocabulary (referenced, not redefined)`; `_bmad-output/project-context.md#API Surface`; `_bmad-output/implementation-artifacts/3-5-tenant-query-gateway-rest-routing.md`]
2. If freshness cannot be measured, the state is `unknown` (and `unknown` freshness fails closed for destructive actions).

### Numbering hazard (read carefully)

This story's key is the sprint-status Epic 9 UI-planning key `9-3-define-truth-state-freshness-and-unavailable-action-patterns`. The `backendEvidence` arrays inside `docs/tenants-ui-phase-2-story-backlog.md` reference a **separate Phase 2 backend backlog** that also uses `9-x`/`10-x`/`11-x`/`12-x` keys (e.g. `9-3-query-policy-for-disabled-tenants-and-orphan-memberships`). Those are not this epic's stories; do not conflate the two namespaces or mark backend rows complete based on this UI story. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`]

### Post-implementation status note

This specification remains the canonical truth-state vocabulary, but several historical backlog readiness cells have been superseded by later implementation evidence. Epic 3 delivered `ui-10` and `ui-13` with approved fallbacks, Epic 4 delivered `ui-15`, Story 2.4 delivered the tenant-member removal flow behind historical `ui-14`, and Epic 5 delivered the Tenants-owned flat audit, receipt, availability-state, and tenant-domain correction slices behind historical `ui-11`/`ui-12` audit rows. Reusable FrontComposer audit timeline, grouped-mode, consequence-preview, token, and batching work can still remain blocked independently.

## 1. Truth State Model (shared contract)

Five truth-state dimensions, each with a user-facing question and a required UI behavior. Every journey shares this contract so screens do not reinterpret the vocabulary. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Truth State Model`]

| Dimension | User-facing question | Required UI behavior |
| --- | --- | --- |
| **Freshness** | Is the displayed projection current enough to use? | Show `current`, `refreshing`, `aging`, `stale`, or `unknown` freshness before access-impacting actions. |
| **Authorization** | Is this user allowed to act in this tenant context? | Separate missing permission from stale data, blocked risk, and unavailable implementation dependency. |
| **Command lifecycle** | What happened to the submitted intent? | Distinguish `eligible`, `previewed`, `submitted`, `accepted`, `rejected`, `already applied`, `failed`, `duplicate`, `timeout`, and `unknown`. |
| **Projection confirmation** | Has the visible read model reflected the accepted command? | Preserve last-confirmed projection data and show pending confirmation separately. |
| **Audit evidence** | Is proof available and safe to cite? | Show `audit pending`, `audit available`, `delayed`, `unavailable`, or `approved fallback`. |

Stale-data thresholds are defined by **implementation stories** using timestamp, projection version, or ETag evidence available from the relevant read model. If freshness cannot be measured, the state is `unknown` and destructive action fails closed. This spec defines the vocabulary and gating rules; it does not set numeric thresholds.

## 2. Truth State Badge Vocabulary (AC1)

The Truth State Badge carries the shared truth-state vocabulary across the tenant list, tenant detail, member table, command feedback, and audit contexts. The custom component is governed here, not implemented; its readiness flows through `FC-TOK`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` (plus `FC-CMD`/`FC-CNC`/`FC-CNS`/`FC-AUD` where lifecycle, concurrency, consequence, or audit states are referenced). [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Truth State Badge`; `#Custom Components`]

### 2.1 Canonical state set (enumerated verbatim)

The AC1 badge states are exactly these thirteen: **current, refreshing, aging, stale, unknown, eligible, blocked, pending, accepted, confirmed, failed, audit pending, audit available.** [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Truth State Badge`]

### 2.2 States grouped by truth-state dimension

States are grouped by the five dimensions so implementation stories cannot reinterpret "current"/"accepted"/"confirmed"/"audited" per screen. This grouping matches the canonicalization recorded in the Story 9.2 senior review. Rows follow the verbatim AC1 enumeration order (so `failed`, a command-lifecycle state, deliberately trails the projection-confirmation `confirmed` row rather than being re-sorted); the **Dimension** column, not row adjacency, is the authoritative grouping. [Source: `_bmad-output/implementation-artifacts/9-2-specify-the-operations-shell-and-read-only-access-review-surfaces.md#Senior Developer Review (AI)`]

| Dimension | Badge states | Meaning |
| --- | --- | --- |
| Freshness | `current` | Projection is current enough to use for read-only review. |
| Freshness | `refreshing` | A query or projection update is in progress. |
| Freshness | `aging` | Projection may still be usable, but action friction may be needed. |
| Freshness | `stale` | Action is blocked or requires refresh. |
| Freshness | `unknown` | Freshness cannot be measured; destructive action fails closed. |
| Authorization | `eligible` | Authorization, freshness, and dependency gates pass; the action can be opened. |
| Authorization | `blocked` | The action is unavailable; the reason is exposed via the Unavailable Action Reason pattern (§4). |
| Command lifecycle | `pending` | Intent is in flight (submitted / projection-pending); not proof of a visible change. |
| Command lifecycle | `accepted` | Backend accepted processing; **not** proof of a visible access change. |
| Projection confirmation | `confirmed` | Projection or status reconciliation supports that the visible change occurred. |
| Command lifecycle | `failed` | Rejection, transport failure, or terminal lifecycle failure, shown with the next safe action. |
| Audit evidence | `audit pending` | Visible state is updated but audit proof is not yet available. |
| Audit evidence | `audit available` | Audit evidence is available with a support-safe reference. |

The richer command-lifecycle vocabulary (`eligible`, `previewed`, `submitted`, `accepted`, `rejected`, `already applied`, `failed`, `duplicate`, `timeout`, `unknown`) and the feedback states (§5) refine the badge's coarse `pending`/`accepted`/`confirmed`/`failed` lifecycle labels; they must not collapse `accepted`, `projected`, and `proven` into a single success state (see §5.2). [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Truth State Model`; `#Feedback Patterns`]

### 2.3 Presentation requirements (every state)

- **Text label required.** Every state carries a user-facing text label; color and icon are secondary, never primary. [Source: `#Truth State Badge`]
- **Accessible name required.** Every state exposes an accessible name independent of color.
- **Non-color-only visual treatment.** Each state pairs text with an icon or shape so it is perceivable in **forced-colors mode** and by users who cannot perceive color differences. [Source: `#Accessibility`]
- **Localizable.** All state labels, role names, timestamps, warnings, disabled reasons, and recovery actions are localizable. No runtime sentence-fragment assembly. Localization responsibility ties to `FC-L10N`; status-token semantics tie to `FC-TOK`. [Source: `#Localization`; `#Component Strategy`]

## 3. Freshness Gating for Access-Impacting Actions (AC2)

The Freshness Gate decides whether an access-impacting action can proceed from the current projection state. The custom component is governed here, not implemented. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Freshness Gate`]

### 3.1 Required Freshness Gate content

When an access-impacting action is considered, the UI must show all four of:

1. **Freshness label** — one of `current`, `refreshing`, `aging`, `stale`, `unknown` (§2.2).
2. **Timestamp or projection-version marker** — the evidence behind the freshness label.
3. **Refresh action** — a concrete way to re-query and re-evaluate freshness.
4. **Blocking reason** — when the gate blocks, the reason is stated using the Unavailable Action Reason pattern (§4).

### 3.2 Freshness measurement is read-model evidence only

Freshness is bound to read-model evidence only: a timestamp, a projection version, or an ETag. The freshness primitive is `If-None-Match` → `304 Not Modified`, served by the REST-backed Tenants read endpoints through in-process query handlers and read-model metadata. If none of these can be measured, the freshness state is `unknown`. [Source: `_bmad-output/project-context.md#Domain, Eventing & Framework Rules`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `docs/tenants-ui-operations-shell-spec.md#Truth-State Vocabulary (referenced, not redefined)`]

The Freshness Gate introduces **no backend "freshness" or "command status" endpoint**; it reads only the existing read endpoints (§8).

### 3.3 Fail-closed rule

Unknown freshness blocks destructive actions by default. More broadly, **unknown freshness, indeterminate authorization, incomplete consequence preview, or missing lifecycle support each block destructive action by default** unless an explicitly approved override path exists. Read-only discovery remains valuable while command flows are not ready. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Flow Optimization Principles`; `#Truth State Model`]

### 3.4 SignalR is a freshness nudge only

SignalR projection notifications are **freshness nudges only** — never proof of command completion or projection consistency. A nudge may move freshness toward `refreshing`/`aging` and prompt a re-query; it must never advance command lifecycle to `confirmed` or audit evidence to `audit available`. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Journey Invariants`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]

## 4. Unavailable Action Reason Pattern (AC3)

The Unavailable Action Reason pattern makes disabled or blocked high-impact actions explainable without relying only on tooltips. The custom component is governed here, not implemented. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Unavailable Action Reason`]

### 4.1 Reason categories (enumerated verbatim)

The reason categories are exactly these six: **missing permission, stale data, missing lifecycle support, missing consequence preview, missing audit proof, high-impact flow not ready.** [Source: `#Unavailable Action Reason`]

Three stable automation categories supplement that unavailable taxonomy without redefining it:
**retained attempt** identifies an already-dispatched lifecycle attempt that can be reopened for authoritative
reconciliation, and **in-flight or command surface** identifies temporary aggregate activity or disconnected command
support. **Lifecycle state already set** identifies a pure, authorized same-state lifecycle domain outcome without
mislabeling it as the canonical `None` category. They carry their own matching reason/recovery copy and never imply
that a new high-impact command is eligible. When an independently proven domain outcome supplements a different
blocker, its companion text has a stable id included in the launcher's `aria-describedby` relationship.

### 4.2 Inline-visible reason; tooltip supplements only

For a high-impact action that is disabled or blocked, the UI exposes a **visible inline reason**. A tooltip may supplement the inline reason but **cannot be the only explanation**. [Source: `#Unavailable Action Reason`; AC3]

### 4.3 Keep unavailable high-impact actions visible; separate the reasons

Unavailable high-impact actions remain visible when the reason aids safety or understanding (do not silently hide them). The pattern keeps these reasons distinct and never conflated:

- **missing permission** (authorization) — distinct from
- **stale data** (freshness) — distinct from
- **blocked risk** (high-impact flow not ready) — distinct from
- **unavailable implementation dependency** (lifecycle / consequence / audit support not yet available).

[Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Button Hierarchy`; `#Truth State Model` (Authorization dimension)]

### 4.4 Reason → evidence source mapping

Each reason category maps to the evidence source that establishes it:

| Reason category | Evidence source | Dependency tie |
| --- | --- | --- |
| missing permission | Projection/query authorization (authorization filtering lives in projection/query handling, not the UI) | Authorization dimension |
| stale data | Freshness marker (timestamp / projection version / ETag → `304`) | Freshness dimension; Freshness Gate (§3) |
| missing lifecycle support | Command lifecycle contract not yet confirmed | `FC-CMD` |
| missing consequence preview | Consequence preview component / inputs not available | `FC-CNS` |
| missing audit proof | Audit evidence path not available | `FC-AUD` |
| high-impact flow not ready | Backlog row `blockedBy` dependencies unresolved | Backlog `blockedBy` (§7) |

[Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`; `_bmad-output/project-context.md#API Surface`]

## 5. Layered Feedback State Set (AC4)

Feedback follows the truth-state model across command, projection, and audit changes. The Command Lifecycle Panel is the custom component that shows command state **without overwriting confirmed projection data**; it is governed here, not implemented. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Feedback Patterns`; `#Command Lifecycle Panel`]

### 5.1 Feedback states (enumerated distinctly)

The AC4 feedback states are distinct and must not be merged: **request sent (submitted), accepted, projection pending, confirmed, rejected, already applied, degraded, audit pending, audit available, unable to verify.** The Epic 2 implementation adds explicit internal distinctions for `previewed`, duplicate submission prevention, and `audit delayed`; these refine the same contract and do not relax the non-collapse invariant. The `#Feedback Patterns` section enumerates submitted, accepted, projection pending, confirmed, audit pending, audit available, and failed/rejected directly; `already applied`, `degraded`, and `unable to verify` are drawn from the same UX spec's command-trust vocabulary (submitted / awaiting confirmation / reflected / rejected / already applied / unable-to-verify; stale-and-degraded states) so the layered set is complete. [Source: `#Feedback Patterns`; `_bmad-output/planning-artifacts/ux-design-specification.md` command-trust and stale/degraded narrative; `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md`; `_bmad-output/implementation-artifacts/2-5-edit-tenant-metadata-with-safe-validation.md`; `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`; AC4]

| Feedback state | Meaning |
| --- | --- |
| request sent (submitted) | Request was sent; projection remains unchanged until confirmed; no outcome yet. |
| accepted | Backend accepted processing; **not** proof of a visible access change. |
| projection pending | Command accepted but the visible read model has not reconciled. |
| confirmed | Projection or status reconciliation supports the visible change. |
| rejected | Domain rejection or terminal failure; explain outcome and next safe action. |
| already applied | The requested change was already in effect (e.g. user already removed). |
| duplicate prevented | A duplicate submission was blocked before a second command could be sent. |
| degraded | A capability is unavailable; explain what is unavailable and what still works. |
| audit pending | Visible state updated but audit proof is not yet available. |
| audit delayed | Audit proof is expected but delayed; do not show audit success. |
| audit available | Support-safe proof can be opened or cited. |
| unable to verify | Status lookup / SignalR / projection confirmation is unavailable; avoid success language. |

### 5.2 Non-collapse invariant

`accepted`, `projected` (confirmed), and `proven` (audit available) states **must NOT be merged into one success state.** Preserve last-confirmed projection data separately from submitted/pending intent; never use optimistic UI to replace source-of-truth projection data. [Source: `#Feedback Patterns`; `#Journey Invariants`; AC4]

### 5.3 Worked example — `RemoveUserFromTenant` command state model

The `RemoveUserFromTenant` command state model is the worked reference for the layered states. **Do not redefine the Story 9.4 journey here** — this is illustration only. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#RemoveUserFromTenant Command State Model`]

```text
eligible -> previewed -> submitted -> accepted -> projection_pending -> confirmed | failed | unknown | audit_pending | audit_available
```

- `eligible`: authorization, freshness, and dependency gates pass, so preview can open.
- `previewed`: consequence preview shows tenant, target user, role, owner risk, freshness, known consequences, known unknowns, and recovery path.
- `submitted`: request sent; projection unchanged until confirmed.
- `accepted`: backend accepted processing; not proof of visible change.
- `projection_pending`: accepted but tenant/member projection has not reconciled.
- `confirmed`: projection/status reconciliation supports the access change.
- `failed`: rejection/transport/terminal failure visible with next action.
- `unknown`: status lookup, SignalR, or projection confirmation unavailable; avoid success language.
- `audit_pending` / `audit_available`: audit proof not yet / now available with a support-safe reference.

### 5.4 Concurrency and recovery cases

These event-sourced cases change feedback state and must be handled explicitly. Each names a concrete recovery action (refresh, wait, retry status lookup, inspect audit, continue read-only, request permission, start a compensating command, or escalate with a support-safe reference). [Source: `#Concurrency and Recovery Cases`; `#Flow Optimization Principles`]

| Case | Feedback state | Concrete recovery |
| --- | --- | --- |
| Target user already removed before submit | already applied | inspect audit; continue read-only |
| Tenant status changed while preview open | blocked / stale | refresh; re-evaluate eligibility |
| Operator lost permission mid-flow | rejected / blocked (missing permission) | request permission; escalate with support-safe reference |
| Duplicate submit or browser refresh during pending | duplicate prevented / accepted / projection pending (deduplicated) | wait; retry status lookup; do not double-apply |
| Projection lagged after acceptance | projection pending | wait; refresh; retry status lookup |
| Command accepted but audit delayed | audit pending / audit delayed | wait; inspect audit later; cite support-safe reference |
| SignalR disconnected / nudge only | unable to verify (freshness nudge only) | refresh; retry status lookup |
| Confirmation became unknown (status lookup failed) | unable to verify / unknown | retry status lookup; inspect audit; escalate |

Never label recovery as "undo"; use "start correction", "restore intended access", "retry status lookup", "inspect audit", or "escalate". [Source: `#Compensating Recovery`]

## 6. Feedback Placement and Degradation Scope (AC5)

### 6.1 Proximity placement

Feedback appears **close to the affected tenant, row, command panel, or audit context** wherever possible. Command lifecycle feedback stays near the affected row or tenant context and is never promoted into a separate primary navigation model. [Source: `#Feedback Patterns`; `#Navigation Patterns`; AC5]

### 6.2 Global message bars are reserved

Global message bars (`FluentMessageBar`) are reserved for **page-level degradation or system-wide service state only.** They are not used for row-level or command-level feedback. [Source: `#Feedback Patterns`]

### 6.3 Degraded / unable-to-verify presentation

The degraded and unable-to-verify presentations must:

- **Distinguish delayed evidence from missing implementation support** (audit unavailable: delayed vs not-yet-built). [Source: `#Empty, Loading, Stale, and Degraded States`]
- **Avoid success language.**
- Offer concrete recovery: retry, inspect audit, continue read-only, or escalate.

## 7. Per-Pattern Consumption Mapping (planning-only / blocked)

Each pattern names the consuming backlog rows and copies the relevant `blockedBy` dependency IDs **verbatim** from `docs/tenants-ui-phase-2-story-backlog.md`. This historical Story 9.3 specification does not promote rows by itself; current implementation story evidence may supersede row-level readiness while reusable FrontComposer component work remains blocked. No new dependency IDs, column names, or `ui-NN` keys are introduced. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`]

### 7.1 Read-only freshness — Truth State Badge (freshness dimension) + Freshness Gate display

Consumed by `ui-01`…`ui-06` (all `planning-only`):

| Backlog row | `blockedBy` (verbatim) |
| --- | --- |
| `ui-01-tenant-list-read-only` | `[FC-LYT, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-02-my-tenants-and-user-search-read-only` | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-03-tenant-detail-overview-read-only` | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-04-user-management-member-table` | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-05-tenant-configuration-read-only` | `[FC-LYT, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-06-global-admin-read-only` | `[FC-LYT, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |

### 7.2 Command lifecycle / feedback — Command Lifecycle Panel + Feedback Patterns + Unavailable Action Reason (lifecycle gaps)

Consumed by `ui-07`, `ui-08`, `ui-09` (`planning-only`), `ui-10`/`ui-13` (ready with approved fallback from Epic 3), `ui-14` (delivered by Story 2.4 while its reusable FrontComposer row remains blocked), and `ui-15` (implemented by Epic 4 Stories 4.3 and 4.4):

| Backlog row | Readiness | `blockedBy` (verbatim) |
| --- | --- | --- |
| `ui-07-create-tenant-command` | `planning-only` | `[FC-LYT, FC-CMD, FC-CNC, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-08-edit-tenant-metadata-command` | `planning-only` | `[FC-LYT, FC-CMD, FC-CNC, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-09-user-management-add-or-change-role` | `planning-only` | `[FC-LYT, FC-CMD, FC-CNC, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-10-tenant-configuration-edit` | `ready-with-approved-fallback` | `[]` |
| `ui-13-disable-or-enable-tenant` | `ready-with-approved-fallback` | `[]` |
| `ui-14-user-management-remove-user` | `blocked` | `[FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-15-global-admin-command-management` | `implemented` | `[]` |

### 7.3 Audit-evidence states — audit pending / audit available / delayed / unavailable / approved fallback

Consumed by `ui-11`/`ui-12` (`blocked` for reusable FrontComposer audit timeline / grouped-mode work). Do not claim an `<AuditTimeline>` component exists. The Tenants-owned first audit slice is now a flat DataGrid-backed implementation delivered by Epic 5; future reusable timeline work still needs `FC-AUD` evidence or an approved replacement path.

| Backlog row | Readiness | `blockedBy` (verbatim) |
| --- | --- | --- |
| `ui-11-audit-trail-flat-timeline` | `blocked` | `[FC-LYT, FC-AUD, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |
| `ui-12-tenant-detail-audit-tab` | `blocked` | `[FC-LYT, FC-AUD, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]` |

### 7.4 Degradation placement (cross-cutting)

Feedback proximity and global-bar reservation (§6) apply to every row above; they do not change any row's readiness or `blockedBy`.

## 8. Backend and Data Boundaries

- Truth-state evidence comes only from existing read endpoints: `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, `GET /api/tenants/{tenantId}/audit`. Cursor-based pagination only — signed, opaque, scope-bound cursors; never offset/limit. ETag `If-None-Match` → `304` is the freshness primitive. [Source: `_bmad-output/project-context.md#API Surface`]
- **Add no backend "consequence" or "command status" endpoint**, do not annotate immutable Tenants domain contracts for UI generation, and do not model SignalR nudges as proof. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- **Command endpoint route (lifecycle context only):** this spec is read-model-oriented and introduces no command rows. Where a later workflow cites a command surface for lifecycle context, the route is `POST /api/v1/commands` (FrontComposer `EventStoreOptions.CommandEndpointPath` default). `project-context.md` records the unversioned `POST /api/commands` `CommandsController` as an alias to confirm against the deployed gateway, not to assume. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Command Endpoint Route Evidence`]
- Rejection events carry structured data only; user-facing text is composed at the HTTP boundary by EventStore's domain-rejection ProblemDetails handling/catalog (RFC 7807). Domain rejections map to safe, localized UI text — never expose raw payloads, stack traces, tokens, or internal exception text. [Source: `_bmad-output/project-context.md#API Surface`; `#Form Patterns`]
- Global administrators live in the separate `global-administrators` domain (singleton aggregate); platform-governance truth-state is distinct from tenant membership. [Source: `_bmad-output/project-context.md#Identity Scheme`]

## 9. Support-Safe References, Accessibility, and Automation Contracts

Shared with the other Epic 9 stories and required by every consuming pattern.

### 9.1 Support-safe references

Support-safe references for command/audit troubleshooting **must never expose** raw payloads, bearer tokens, stack traces, internal correlation/message ids, or PII. Reference content is limited to non-sensitive, support-safe tokens (e.g. a support-safe command reference, tenant/user reference, projection version/freshness marker, accepted timestamp, audit event reference or fallback state). [Source: `#Support-Safe References`; `_bmad-output/project-context.md#Logging & Telemetry`; `_bmad-output/project-context.md#Security & Sanitization`]

### 9.2 Accessibility (ties to `FC-A11Y`)

Disabled explanations, command lifecycle changes, stale/degraded states, and audit availability must be **perceivable without color** and **announced via live status**. Keyboard users must be able to complete or exit every modal, preview, and command flow. All badge/feedback states pair text with an icon or shape and work in forced-colors mode. [Source: `#Accessibility`]

### 9.3 Automation selectors (ties to `FC-A11Y` / `FC-DOC`)

Automation of state assertions must rely on **stable selectors or component contracts**, never arbitrary row text. This ties to `FC-A11Y` (accessible names, stable roles) and `FC-DOC` (component-reference evidence). [Source: `#Search, Filtering, and Table Patterns`]

### 9.4 Localization (ties to `FC-L10N`)

All state labels, role names, timestamps, warnings, disabled reasons, and recovery actions are localizable; no sentence-fragment assembly at runtime. [Source: `#Localization`]

## 10. Implementation Story Rules

A future Phase 2 UI story may consume these patterns only when it can name **all** of the following (it fails closed otherwise). These are acceptance criteria for the UX promise, not implementation preferences. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Story Rules`]

1. **Source projection or query** used for the screen.
2. **Freshness state** shown to the user (Freshness Gate content per §3).
3. **Authorization state and unavailable-action reason** (Unavailable Action Reason categories per §4).
4. **Command lifecycle states**, if the story dispatches commands (layered feedback set per §5; non-collapse invariant).
5. **Consequence preview inputs** for access-impacting actions.
6. **Audit evidence path** or an approved read-only fallback.
7. **Support-safe observability references** (§9.1).
8. **Accessibility behavior** for focus, keyboard use, live status, and disabled explanations (§9.2).
9. **Localization responsibility** for state labels, timestamps, roles, and warnings (§9.4).

## 11. Acceptance Criteria Traceability

| AC | Requirement | Spec section |
| --- | --- | --- |
| AC1 | Truth State Badge state set (13 states), grouped by dimension; each with text label + accessible name + non-color-only treatment; localizable | §1, §2 |
| AC2 | Freshness Gate shows freshness label, timestamp/version marker, refresh action, blocking reason; ETag/304 primitive; unknown fails closed; SignalR nudge-only | §3 |
| AC3 | Unavailable Action Reason categories (6); inline visible, tooltip supplements only; reasons kept distinct; reason→evidence mapping | §4 |
| AC4 | Layered feedback state set (10 states) distinct; accepted/projected/proven not collapsed; `RemoveUserFromTenant` worked example; concurrency/recovery cases | §5 |
| AC5 | Feedback proximity placement; global message bars reserved for page-level/system-wide degradation; degraded/unable-to-verify presentation | §6 |

## 12. References

- `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`
- `_bmad-output/planning-artifacts/epics.md#Story 9.3: Define Truth State, Freshness, and Unavailable Action Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Truth State Model`
- `_bmad-output/planning-artifacts/ux-design-specification.md#RemoveUserFromTenant Command State Model`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Journey Invariants`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Flow Optimization Principles`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Concurrency and Recovery Cases`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Story Rules`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Truth State Badge`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Freshness Gate`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Unavailable Action Reason`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Command Lifecycle Panel`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Feedback Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Empty, Loading, Stale, and Degraded States`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Support-Safe References`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Accessibility`
- `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`
- `docs/tenants-ui-operations-shell-spec.md#Truth-State Vocabulary (referenced, not redefined)`
- `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`
- `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`
- `_bmad-output/implementation-artifacts/9-2-specify-the-operations-shell-and-read-only-access-review-surfaces.md`
- `_bmad-output/project-context.md#API Surface`
- `_bmad-output/project-context.md#Identity Scheme`
- `_bmad-output/project-context.md#Logging & Telemetry`
