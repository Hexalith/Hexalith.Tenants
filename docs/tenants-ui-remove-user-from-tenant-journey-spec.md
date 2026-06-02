# Tenants UI RemoveUserFromTenant Command-Capable Journey Specification

Owner: Hexalith.Tenants product and UX planning
Status: Phase 2 planning/readiness artifact (planning-only)
Last reviewed: 2026-06-02
Story: 9.4 — Specify the RemoveUserFromTenant Command-Capable Journey

This document is the **worked, end-to-end specification for the first command-capable Phase 2 Tenants Admin UI journey**: removing a user's access to a tenant. It composes — and never redefines — the truth-state/feedback contract (Story 9.3, `docs/tenants-ui-truth-state-and-action-availability-spec.md`), the operations shell / information architecture (Story 9.2, `docs/tenants-ui-operations-shell-spec.md`), and the FrontComposer/Fluent UI dependency map (Story 9.1, `docs/tenants-ui-frontcomposer-dependency-map.md`) into a single remove-user flow spec. Its purpose is to make access changes **previewed, submitted, reconciled, and proven without false success**.

## Scope and Boundary (read first)

- **Planning/specification only.** Epic 9 is readiness/planning-only. This story produces a `RemoveUserFromTenant` command-capable **journey specification** — not shippable Admin UI. It must not route a Developer agent into product UI delivery until separate Phase 2 implementation stories are created from this spec. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`]
- **This document does NOT** create UI components, Blazor pages/routes, backend endpoints, commands, queries, package references, generated FrontComposer files, domain-contract annotations, generated artifacts, Phase 1 release gates, or submodule pointer changes. The custom components named here (Consequence Preview, Command Lifecycle Panel, Truth State Badge, Freshness Gate, Unavailable Action Reason, Audit Evidence Receipt) are **referenced and governed, not implemented**. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.4`]
- **Backend MVP work stays independent of UI dependency readiness.** Missing UI dependencies block or defer future Phase 2 UI rows; they never block backend package/release work. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`]
- **Reconcile, do not duplicate.** Dependency IDs, per-row `blockedBy` arrays, the truth-state badge vocabulary, freshness gating, unavailable-action reasons, layered feedback states, and the `RemoveUserFromTenant` command state model are owned by the three existing UI docs. This journey spec references and copies them verbatim; it does not re-enumerate or contradict them. [Source: Story 9.1 senior review; `docs/tenants-ui-truth-state-and-action-availability-spec.md`; `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`]

### Why a new artifact

No command-capable journey specification existed in `docs/` before this story; the directory held only the dependency map (`tenants-ui-frontcomposer-dependency-map.md`), the operations-shell spec (`tenants-ui-operations-shell-spec.md`), the Phase 2 backlog (`tenants-ui-phase-2-story-backlog.md`), and the truth-state spec (`tenants-ui-truth-state-and-action-availability-spec.md`) for UI. Those artifacts each own a *layer* (dependencies, navigation/IA, backlog rows, truth/feedback vocabulary). None of them works a *single journey end to end*. This spec adds a **journey layer** on top — the worked first command journey — rather than a parallel dependency map, navigation model, or truth-state vocabulary. Story 9.1's review penalized an unnecessary parallel artifact and reverted a wrong endpoint normalization; this spec composes the existing sources of truth and copies their identifiers verbatim to avoid repeating that mistake. [Source: `_bmad-output/implementation-artifacts/9-1-map-fluent-ui-and-frontcomposer-dependencies-for-tenant-admin-screens.md#Senior Developer Review (AI)`]

### This journey composes existing patterns — it does NOT redefine them

- **Truth/feedback contract (Story 9.3).** `docs/tenants-ui-truth-state-and-action-availability-spec.md` is the canonical owner of the Truth State Badge vocabulary (§2), freshness gating (§3, ETag `If-None-Match` → `304` via `CachingProjectionActor`; unmeasurable = `unknown`, fail-closed), the Unavailable Action Reason taxonomy (§4, six categories), the layered feedback state set (§5), and the `RemoveUserFromTenant` command state model worked example (§5.3). This journey **applies** those patterns to the remove-user flow; it never re-enumerates badge states, freshness primitives, reason categories, feedback states, or the command state model.
- **Operations shell / IA (Story 9.2).** `docs/tenants-ui-operations-shell-spec.md` owns the navigation/IA and the tenant-detail → members surface this journey launches from. Tenant context is preserved across overview ↔ members ↔ configuration ↔ command state ↔ audit, and **command lifecycle is never promoted into a separate primary navigation model** (§1.2). [Source: `docs/tenants-ui-operations-shell-spec.md#Operations Shell Information Architecture and Navigation`]
- **Dependency map (Story 9.1).** `docs/tenants-ui-frontcomposer-dependency-map.md` owns the 10 fixed dependency IDs (`FC-TBL`, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-AUD`, `FC-CNS`, `FC-TOK`, `FC-A11Y`, `FC-L10N`, `FC-DOC`). This journey reuses them verbatim and adds none.

### Two critical guardrails this spec must not violate

1. **Last-owner removal is allowed by design.** `TenantAggregate` does **NOT** enforce a "must retain ≥1 owner" invariant. The UI surfaces `ownerCount == 0` as a **warning with elevated friction**, never as a backend block. A future implementer must not add a backend prohibition or model the UI as if the backend rejects last-owner removal — that would break legitimate ownership-transfer flows. AC3's "elevated friction" is a UI-friction/warning requirement, not a backend invariant. [Source: `_bmad-output/project-context.md#Aggregates`]
2. **`RemoveUserFromTenant` ≠ `RemoveGlobalAdministrator`.** This journey removes **tenant membership** (`RemoveUserFromTenant`, domain `tenants`, `AggregateId` = managed tenant ID). Removing **platform global-administrator authority** is a separate command (`RemoveGlobalAdministrator`) on the singleton `global-administrators` domain — backlog row `ui-15`, not this journey. AC3's "global administrator removal" friction means: when the *target user also holds global-admin authority*, raise platform-level risk friction; it does **not** mean this command edits the global-administrators aggregate. [Source: `_bmad-output/project-context.md#Identity Scheme`; `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories` (`ui-15`)]

### Numbering hazard (read carefully)

This story's key is the sprint-status Epic 9 UI-planning key `9-4-specify-the-removeuserfromtenant-command-capable-journey`. The `backendEvidence` arrays inside `docs/tenants-ui-phase-2-story-backlog.md` (e.g. `ui-14` cites `9-3-query-policy-for-disabled-tenants-and-orphan-memberships`) reference a **separate Phase 2 backend backlog** that also uses `9-x`/`10-x`/`11-x`/`12-x` keys. Those are not this epic's stories; do not conflate the two namespaces or mark backend rows complete based on this UI story. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`]

## 1. Journey Launch Context and Scope (AC1)

### 1.1 First command-capable journey

`RemoveUserFromTenant` is the **first** command-capable journey and the recommended first command — not `DisableTenant`, which has a broader blast radius (a whole tenant rather than one membership). Selecting remove-user first lets the platform prove the full preview → submit → reconcile → prove loop on the smallest safe destructive surface before exposing wider-impact commands. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Defining Experience`; `#Platform Strategy`]

### 1.2 Launch surface: a specific tenant membership row

The journey launches from a **specific tenant membership row** in tenant detail → members (the member table surface owned by Story 9.2's operations shell). It is an **access-evidence journey, not a button**: before the remove action can open, the row must make visible —

- **tenant** (the managed tenant context),
- **target user** (identity by JWT `sub` / `envelope.UserId`, never `name`/`email`),
- **current role** (the membership role being removed),
- **freshness** (the Truth State Badge freshness dimension + Freshness Gate marker; §2.2/§3 of the 9.3 spec), and
- **authority / who-can-act** (whether the current operator is authorized to remove this member, reflected — never enforced — by the UI).

Command lifecycle stays inside this workflow, close to the affected row; it is never promoted into a separate primary navigation model. [Source: `docs/tenants-ui-operations-shell-spec.md#Operations Shell Information Architecture and Navigation`; `_bmad-output/planning-artifacts/ux-design-specification.md#Experience Principles`; `#Journey 3: Remove User From Tenant`; AC1]

### 1.3 Launch context sources (read endpoints only)

Launch and preview context is sourced **only from existing read endpoints** — no new read endpoint is introduced:

- `GET /api/tenants/{tenantId}/users` (`GetTenantUsersQuery`) — membership rows + roles for the member table and the target row.
- `GET /api/tenants/{tenantId}` — tenant status, owner count, and the freshness marker.

Both are cursor-paginated with signed, opaque, scope-bound cursors (never offset/limit), and ETag `If-None-Match` → `304` is the freshness primitive served by `CachingProjectionActor`. Authorization filtering lives in projection/query handling, not in the UI. [Source: `_bmad-output/project-context.md#API Surface`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §3.2, §8]

## 2. Consequence Preview Content and the Incomplete-Input Block (AC2)

### 2.1 Preview content (enumerated exactly)

When the remove-user action is prepared, the Consequence Preview must present all of the following, in scoped order:

1. **tenant** — the managed tenant context.
2. **target user** — the membership being removed (by `sub`/`UserId`).
3. **current role** — the role held in this tenant.
4. **owner count** — current tenant owner count, used to detect last-owner risk (§3).
5. **affected access path** — what access this removal ends (this tenant's membership and role-derived access).
6. **freshness** — the freshness label + timestamp/projection-version/ETag marker behind it.
7. **recovery path** — how an erroneous removal would be corrected (a compensating command; full pattern is Story 9.5, referenced not specified here).
8. **audit expectation** — that the removal is expected to produce audit evidence (`audit pending` → `audit available`; full audit-evidence-receipt pattern is Story 9.5).
9. **known consequences** — the effects backend evidence supports (membership ends; role-derived tenant access ends).
10. **known unknowns** — effects **not** proven by backend evidence (e.g. session revocation, downstream enforcement, token invalidation), classified as unknowns rather than claimed as consequences.

[Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Consequence Preview`; `#Experience Mechanics`; AC2]

### 2.2 Compose the preview from read-model evidence only

The preview composes entirely from already-loaded projection/read-model data (`GET /api/tenants/{tenantId}` and `GET /api/tenants/{tenantId}/users`). **Do NOT add a backend "consequence" or "command status" endpoint.** Owner count and affected access path derive from the tenant/users read models; the Consequence Preview component (`FC-CNS`) is recorded `missing`. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/project-context.md#API Surface`; `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`]

### 2.3 Do not over-claim consequences

The UI must not present session revocation, downstream enforcement, or token invalidation as known consequences unless backend evidence exists. These are **known unknowns** (item 10). Overstating downstream effects is itself a false-success risk. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Flow Optimization Principles`]

### 2.4 Fail-closed: incomplete inputs block submit

**Incomplete consequence inputs block submit** unless product and UX approve a **named** fallback. The UI must not silently submit on a partial preview. This is the fail-closed rule from the 9.3 spec applied to this journey: unknown freshness, indeterminate authorization, incomplete consequence preview, or missing lifecycle support each block destructive action by default unless an explicitly approved override path exists. The block reason surfaces via the Unavailable Action Reason pattern (category **missing consequence preview**, evidence tie `FC-CNS`). Because `FC-CNS` is `missing` and the consuming backlog row's `fallbackDecision` is `deferred` (`inline-consequence-preview-not-approved`), this journey is **not** implementation-ready. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §3.3, §4.1, §4.4; `_bmad-output/planning-artifacts/ux-design-specification.md#Consequence Preview`; AC2]

## 3. High-Risk Access Cases and Elevated Friction (AC3)

### 3.1 Last-owner removal is a warning, never a backend block

When `ownerCount → 0` (removing the last owner), the UI raises **elevated friction** — affected scope, evidence freshness, audit consequence, and an intentional confirmation — and surfaces `ownerCount == 0` as a **warning**. It must **never** present this as a backend prohibition. `TenantAggregate` does not enforce a "must retain ≥1 owner" invariant; last-owner removal is allowed by design (it enables legitimate ownership-transfer flows). Modeling the UI as if the backend rejects last-owner removal would contradict the domain and block valid flows. [Source: `_bmad-output/project-context.md#Aggregates`; AC3]

### 3.2 Cases requiring elevated friction

Require elevated friction (affected scope + evidence freshness + audit consequence + intentional confirmation) for:

- **last-owner removal** (`ownerCount → 0`) — surfaced as a warning, not a block (§3.1);
- **removal of a user who also holds global-administrator authority** — raise **platform-level** risk friction (see §3.3); and
- **tenant-wide impact cases** — where the removal materially changes who can operate the tenant.

Destructive actions must **not appear as casual primary actions**: the remove control is not a default/primary button, the high-risk path requires intentional confirmation, and the inline Unavailable Action Reason (not a tooltip alone) explains any block. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Flow Optimization Principles`; `#Button Hierarchy`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §4.2; AC3]

### 3.3 Global-administrator authority is a platform-level flag, not a domain switch

When the target user **also** holds platform global-administrator authority, elevate friction with platform-level consequence copy — but do **not** conflate this with editing the `global-administrators` aggregate. Removing platform global-admin authority is a separate command (`RemoveGlobalAdministrator`, `global-administrators` singleton domain, backlog `ui-15`). This journey removes only tenant membership (`RemoveUserFromTenant`, `tenants` domain). The friction flag tells the operator "this person is also a platform admin — consider platform impact"; it does not change which command is dispatched. [Source: `_bmad-output/project-context.md#Identity Scheme`; `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories` (`ui-15`)]

## 4. Command Submission: Local Hints Without Replacing Confirmed Truth (AC4)

### 4.1 Required-field validation before preview or submit

Required fields are validated **before** the Consequence Preview opens or the command submits: target user, tenant, and role context resolved, and the freshness and authorization gates passed (`eligible`). An action that cannot resolve these fails closed and shows the relevant Unavailable Action Reason. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Story Rules`; AC4]

### 4.2 Local pending/confirming hints kept separate from projection truth

On confirm, the UI records **local pending/confirming hints** (`submitted` / `accepted` / `projection_pending`) tracked client-side via the FrontComposer command/feedback services (`FC-CMD`, `needs-confirmation`). It must **never overwrite last-confirmed membership projection data with optimistic intent**. The non-collapse invariant from the 9.3 spec applies: `accepted`, `projected` (confirmed), and `proven` (audit available) must not be merged into a single success state; last-confirmed projection data is preserved separately from in-flight intent. The Command Lifecycle Panel shows command state near the affected row without replacing the member table's source-of-truth data. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §5.1, §5.2; `_bmad-output/planning-artifacts/ux-design-specification.md#Journey Invariants`; `#Command Lifecycle Panel`; AC4]

### 4.3 Command state model (referenced, not redefined)

The `RemoveUserFromTenant` command state model is owned by the UX spec and the 9.3 truth-state spec (§5.3). It is referenced here, not redefined:

```text
eligible -> previewed -> submitted -> accepted -> projection_pending -> confirmed | failed | unknown | audit_pending | audit_available
```

Each state requires visible UI copy, enabled/disabled actions, retry behavior where applicable, and a support-safe reference where available. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#RemoveUserFromTenant Command State Model`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §5.3]

### 4.4 Command surface

The command is dispatched to `POST /api/v1/commands` (FrontComposer `EventStoreOptions.CommandEndpointPath` default), with `command:RemoveUserFromTenant`. `project-context.md` records the unversioned `POST /api/commands` `CommandsController` as an **alias** to confirm against the deployed gateway, not to assume. Use `/api/v1/commands` when citing the command surface. No backend "consequence" or "command status" endpoint is added; command lifecycle is tracked client-side. [Source: `docs/tenants-ui-frontcomposer-dependency-map.md#Command Endpoint Route Evidence`; `_bmad-output/project-context.md#API Surface`]

## 5. Command Reconciliation and Recovery Paths (AC5)

### 5.1 Every backend outcome handled distinctly

The UI handles each backend outcome distinctly and **preserves tenant/user context** across all of them (mapped to the layered feedback states of the 9.3 spec §5.1):

| Backend outcome | Feedback state | Required UI behavior |
| --- | --- | --- |
| Rejected | rejected (`failed`) | Map the domain rejection to safe localized text; preserve context; offer recovery (§5.3). |
| Accepted | accepted | Show acceptance as processing, **not** as a visible access change; keep projection truth until confirmed. |
| Already applied | already applied | The user was already removed; inspect audit / continue read-only; no double-apply. |
| Projection delayed | projection pending (`projection_pending`) | Wait / refresh / retry status lookup; do not assert the change yet. |
| Unable to verify | unable to verify (`unknown`) | Avoid success language; retry status lookup / inspect audit / escalate. |

[Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Journey 3: Remove User From Tenant`; `#Feedback Patterns`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §5; AC5]

### 5.2 Safe localized rejection text; no internal leakage

Domain rejections are mapped to **safe localized UI text** composed at the HTTP boundary by EventStore's domain-rejection ProblemDetails handling/catalog (RFC 7807: 404 not-found, 409 conflict, 422 other domain rejections). Rejection events carry structured data only. The UI must **never expose** raw command payloads, serialized command bodies, stack traces, bearer tokens, internal correlation IDs, internal exception text, raw EventStore metadata, or PII. [Source: `_bmad-output/project-context.md#API Surface`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §9.1; AC5]

### 5.3 Concrete recovery choices

Each outcome names concrete recovery actions (never labeled "undo"): **refresh**, **retry status lookup**, **status review**, **inspect audit**, **continue read-only**, **request permission**, and **escalate with a support-safe reference**. For an erroneous removal, the recovery path is to **start a compensating command** ("start correction" / "restore intended access"), whose full UI pattern is **Story 9.5** — referenced here, not specified. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Concurrency and Recovery Cases`; `#Journey 4: Audit Evidence and Compensating Recovery`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §5.4; AC5]

### 5.4 Event-sourced concurrency and recovery cases

These event-sourced cases change feedback state and must be handled explicitly (composing the 9.3 spec §5.4). **SignalR is a freshness nudge only — never proof of command completion.**

| Case | Feedback state | Concrete recovery |
| --- | --- | --- |
| Target already removed before submit | already applied | inspect audit; continue read-only |
| Tenant status changed while preview open | blocked / stale | refresh; re-evaluate eligibility |
| Operator lost permission mid-flow | rejected / blocked (missing permission) | request permission; escalate with support-safe reference |
| Duplicate submit / browser refresh during pending | accepted / projection pending (deduplicated) | wait; retry status lookup; do not double-apply |
| Projection lagged after acceptance | projection pending | wait; refresh; retry status lookup |
| Command accepted but audit delayed | audit pending | wait; inspect audit later; cite support-safe reference |
| SignalR disconnected / nudge only | unable to verify (nudge only) | refresh; retry status lookup |
| Status lookup failed → confirmation unknown | unable to verify / unknown | retry status lookup; inspect audit; escalate |

[Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Concurrency and Recovery Cases`; `#Journey Invariants`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §5.4]

## 6. Per-Pattern Consumption Mapping (planning-only / blocked)

The journey binds to a single consuming backlog row and copies its arrays **verbatim** from `docs/tenants-ui-phase-2-story-backlog.md`. No new dependency IDs, column names, or `ui-NN` keys are introduced. The row stays `blocked`; it does **not** become implementation-ready in this story.

- **Consuming row:** `ui-14-user-management-remove-user`
- **Readiness:** `blocked`
- **`fallbackDecision`:** `deferred` (`inline-consequence-preview-not-approved`)
- **Decision owner:** Tenants Product/UX + `Hexalith.FrontComposer`
- **`blockedBy` (verbatim):** `[FC-LYT, FC-CMD, FC-CNC, FC-CNS, FC-TOK, FC-A11Y, FC-L10N, FC-DOC]`
- **`backendEvidence` (verbatim):** `[3-1-user-role-management, 3-2-role-behavior-enforcement, 5-3-query-endpoints-and-authorization, 9-3-query-policy-for-disabled-tenants-and-orphan-memberships, command:RemoveUserFromTenant, endpoint:POST /api/v1/commands]`

### 6.1 Pattern → dependency map

| Journey pattern | Custom component (used, not implemented) | Dependency tie | Readiness |
| --- | --- | --- | --- |
| Member-row launch context + member re-query | Truth State Badge / member table | `FC-TBL` (available), `FC-LYT`, `FC-TOK` | blocks via `FC-LYT`/`FC-TOK` |
| Consequence Preview content + incomplete-input block | Consequence Preview | `FC-CNS` (`missing`) | **blocks the row** |
| Command submission + local hints | Command Lifecycle Panel | `FC-CMD` (`needs-confirmation`), `FC-CNC` (`missing`) | blocks via `FC-CMD`/`FC-CNC` |
| High-risk friction + freshness gate | Freshness Gate / Unavailable Action Reason | `FC-TOK`, `FC-A11Y` | blocks via `FC-TOK` |
| Audit expectation + recovery path (referenced) | Audit Evidence Receipt (Story 9.5) | `FC-AUD` (referenced; full pattern deferred) | deferred to 9.5 |
| Accessibility / localization / docs | (cross-cutting) | `FC-A11Y`, `FC-L10N`, `FC-DOC` | blocks the row |

The row is `blocked` (not merely `planning-only`) on two branches: `FC-CNS` is `missing`, and the row carries a `deferred` fallback for a destructive workflow. [Source: `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories` (`ui-14`), `#Blocked`; `docs/tenants-ui-frontcomposer-dependency-map.md#High-Risk Workflow Dependency Map`]

### 6.2 Deferred to Story 9.5

The **audit-evidence-receipt** and **compensating-recovery** UI patterns (and the flat audit DataGrid fallback) are deferred to Story 9.5. This journey references *audit expectation* (§2.1 item 8) and *recovery path* (§2.1 item 7, §5.3) but does not specify their full component patterns. [Source: `_bmad-output/planning-artifacts/epics.md#Story 9.5`]

## 7. Implementation Story Rules (what a future UI story must satisfy)

A future Phase 2 UI story may implement this journey only when it can name **all** of the following (it fails closed otherwise). These are acceptance criteria for the UX promise, not implementation preferences. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Story Rules`; `docs/tenants-ui-truth-state-and-action-availability-spec.md` §10]

1. **Source projection/query** — `GET /api/tenants/{tenantId}/users` and `GET /api/tenants/{tenantId}` for launch/preview; member re-query after reconciliation.
2. **Freshness state shown** — Freshness Gate content (label, timestamp/version marker, refresh action, blocking reason); unknown fails closed.
3. **Authorization state and unavailable-action reason** — six reason categories kept distinct; inline-visible reason, tooltip supplements only.
4. **Command lifecycle states** — the layered feedback set with the non-collapse invariant; `command:RemoveUserFromTenant` to `POST /api/v1/commands`.
5. **Consequence preview inputs** — the ten-item preview content set (§2.1), composed from read models; fail-closed on incomplete inputs.
6. **Audit evidence path** or an approved read-only fallback — referencing the Story 9.5 audit-evidence-receipt pattern.
7. **Support-safe observability references** — no raw payloads, tokens, stack traces, internal correlation IDs, or PII.
8. **Accessibility behavior** — focus, keyboard completion/exit of every preview/confirmation/command flow, live status, disabled explanations, no-color-only treatment.
9. **Localization responsibility** — state labels, timestamps, roles, warnings, disabled reasons, and recovery actions localizable; no runtime sentence-fragment assembly.

## 8. Support-Safe References, Accessibility, and Automation Contracts

Shared with the other Epic 9 stories; required by this journey.

### 8.1 Support-safe references

Support-safe references for command/audit troubleshooting **must never expose** raw payloads, bearer tokens, stack traces, internal correlation IDs, or PII. Reference content is limited to non-sensitive, support-safe tokens (a support-safe command reference, tenant/user reference, projection version/freshness marker, accepted timestamp, audit event reference or fallback state). [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §9.1; `_bmad-output/project-context.md#Logging & Telemetry`; `_bmad-output/project-context.md#Support-Safe References`]

### 8.2 Accessibility (ties to `FC-A11Y`)

Disabled/unavailable explanations, command lifecycle changes, stale/degraded states, and audit availability must be **perceivable without color** and **announced via live status**. Keyboard users must be able to complete or exit every preview, confirmation, and command flow. All badge/feedback states pair text with an icon or shape and work in forced-colors mode. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §9.2; `_bmad-output/planning-artifacts/ux-design-specification.md#Accessibility`]

### 8.3 Automation selectors (ties to `FC-A11Y` / `FC-DOC`)

Automation of journey-state assertions must rely on **stable selectors or component contracts**, never arbitrary row text. This ties to `FC-A11Y` (accessible names, stable roles) and `FC-DOC` (component-reference evidence). [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §9.3]

### 8.4 Localization (ties to `FC-L10N`)

All state labels, role names, timestamps, warnings, disabled reasons, and recovery actions are localizable; no sentence-fragment assembly at runtime. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md` §9.4]

## 9. Acceptance Criteria Traceability

| AC | Requirement | Spec section |
| --- | --- | --- |
| AC1 | `RemoveUserFromTenant` is the first command-capable journey, launched from a specific membership row with tenant/user/role/freshness/authority context visible | §1 |
| AC2 | Consequence Preview content set (10 items); incomplete inputs block submit unless product+UX approve a named fallback; composed from read models only | §2 |
| AC3 | Elevated friction for last-owner (warning, not backend block), global-admin authority (platform flag, not domain switch), and tenant-wide impact; destructive actions are not casual primary actions | §3 |
| AC4 | Local pending/confirming hints kept separate from confirmed projection truth; required-field validation before preview/submit; command state model referenced | §4 |
| AC5 | Reconciliation across rejected/accepted/already-applied/projection-delayed/unable-to-verify; safe localized rejections; recovery paths; no internal leakage; concurrency cases; SignalR nudge-only | §5 |

## 10. Backend and Data Boundaries (summary)

- Launch/preview/freshness/audit evidence comes only from existing read endpoints: `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, and `GET /api/tenants/{tenantId}/audit` for audit-evidence states. Cursor-based pagination only (signed, opaque, scope-bound cursors; never offset/limit). ETag `If-None-Match` → `304` is the freshness primitive via `CachingProjectionActor`. [Source: `_bmad-output/project-context.md#API Surface`]
- The command is dispatched to `POST /api/v1/commands` (`command:RemoveUserFromTenant`). **Add no backend "consequence" or "command status" endpoint**; consequence preview composes from read models, command lifecycle is tracked client-side (`FC-CMD`), and SignalR notifications are refresh nudges only, never proof. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/project-context.md#API Surface`]
- Domain rejections map to safe localized UI text at the HTTP boundary (EventStore domain-rejection ProblemDetails handling/catalog, RFC 7807); never expose raw payloads, stack traces, tokens, internal correlation IDs, or internal exception text. [Source: `_bmad-output/project-context.md#API Surface`]
- Identity uses JWT `sub` (`envelope.UserId`), never `name`/`email`; IDs are ULIDs, not GUIDs. Authorization is enforced server-side (L1 API gate + L2 domain RBAC); the UI reflects authorization state and unavailable-action reasons but is not the authorization boundary. [Source: `_bmad-output/project-context.md#Identity Scheme`; `#Authorization (RBAC)`]

## 11. References

- `_bmad-output/planning-artifacts/epics.md#Epic 9: Administrators Can Plan Phase 2 UI Access Operations Safely`
- `_bmad-output/planning-artifacts/epics.md#Story 9.4: Specify the RemoveUserFromTenant Command-Capable Journey`
- `_bmad-output/planning-artifacts/epics.md#Story 9.5: Specify Audit Evidence and Compensating Recovery UI Patterns`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Journey 3: Remove User From Tenant`
- `_bmad-output/planning-artifacts/ux-design-specification.md#RemoveUserFromTenant Command State Model`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Consequence Preview`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Command Lifecycle Panel`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Flow Optimization Principles`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Concurrency and Recovery Cases`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Journey Invariants`
- `_bmad-output/planning-artifacts/ux-design-specification.md#Implementation Story Rules`
- `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`
- `docs/tenants-ui-truth-state-and-action-availability-spec.md`
- `docs/tenants-ui-operations-shell-spec.md`
- `docs/tenants-ui-frontcomposer-dependency-map.md#Dependency ID Catalog`
- `docs/tenants-ui-frontcomposer-dependency-map.md#Command Endpoint Route Evidence`
- `docs/tenants-ui-frontcomposer-dependency-map.md#High-Risk Workflow Dependency Map`
- `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`
- `_bmad-output/implementation-artifacts/9-1-map-fluent-ui-and-frontcomposer-dependencies-for-tenant-admin-screens.md#Senior Developer Review (AI)`
- `_bmad-output/implementation-artifacts/9-3-define-truth-state-freshness-and-unavailable-action-patterns.md`
- `_bmad-output/project-context.md#Aggregates`
- `_bmad-output/project-context.md#Identity Scheme`
- `_bmad-output/project-context.md#API Surface`
- `_bmad-output/project-context.md#Support-Safe References`
