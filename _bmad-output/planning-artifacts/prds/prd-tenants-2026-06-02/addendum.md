# Addendum — Tenants Management UI PRD

Technical-how and downstream bridge material kept out of the PRD body. This maps PRD features/FRs to the existing Epic 9 specs, the `ui-NN` backlog, FrontComposer dependencies, backend surfaces, the rejection/NoOp matrix, and the canonical state sets. **Source of truth for mechanics is the `docs/tenants-ui-*` specs and the domain code**; this is a navigational bridge, not a re-spec.

## A. Feature / FR → backlog → spec mapping

| PRD feature (FRs) | Backlog id(s) | Primary spec(s) | Phase |
|---|---|---|---|
| 7.1 Tenant Discovery & Triage (FR-1..4) | ui-01, ui-02 | operations-shell, truth-state | 2a (MVP) |
| 7.2 Tenant Detail & Config View (FR-5..7) | ui-03, ui-05 | operations-shell | 2a (MVP) |
| 7.3 Member & Access Review (FR-8..9) | ui-04 | operations-shell, truth-state | 2a (MVP) |
| 7.4 Member & Role Mgmt (FR-10..12) | ui-09 (add+role), ui-14 (remove) | remove-user-journey, truth-state | 2b / 2c |
| 7.5 Tenant Lifecycle (FR-13..15) | ui-07, ui-08, ui-13 | phase-2-backlog, truth-state | 2b / 2c |
| 7.6 Configuration Mgmt (FR-16..17) | ui-10 | phase-2-backlog, truth-state | 2c |
| 7.7 Global Admin Governance (FR-18..19) | ui-06, ui-15 | operations-shell | 2a read / 2c cmd |
| 7.8 Audit Trail & Evidence (FR-20..23) | ui-11, ui-12; Epic 5 Story 5.3 (FR-22, `epics.md`) | audit-evidence-and-compensating-recovery | 2c |
| 7.9 Compensating Recovery (FR-24..25) | Epic 5 Stories 5.5 and 5.6 (`epics.md`) | audit-evidence-and-compensating-recovery | 2c |

> Scope-honesty note: FR-22/FR-24/FR-25 now have explicit Epic 5 story coverage in `epics.md` (Stories 5.3, 5.5, and 5.6). Backend/evidence readiness still needs validation before those stories are build-ready. ui-09 is one historical backlog row covering both add-user and change-role behind a shared availability gate; the PRD splits it into FR-10/FR-11.

## B. FrontComposer dependency readiness (from the dependency map)

| ID | Capability | Readiness | Notes / fallback |
|---|---|---|---|
| FC-TBL | Projection list/table (DataGrid, filter/search/empty/loading) | **available with resolved Tenants boundary** | Generated projection grid lacks Tenants-required cursor pagination, safety-column pinning, and six non-collapsing list states by itself. Story 1.2 resolves the Tenants path with a Tenants-specific `TenantDataGrid`; reusable cursor/pinning/list-state capability remains FrontComposer-owned enhancement work. |
| FC-LYT | Shell layout contract (full-width vs constrained) | **confirmed** | Confirmed by Story 1.0 spike note (2026-06-05). |
| FC-CMD | Command lifecycle feedback (three-phase, projection-confirmed) | **confirmed** | Confirmed by Story 1.0 spike note (2026-06-05). Required for ALL command FRs. |
| FC-CNC | Concurrent-command / toast batching policy | **confirmed** | Lock scope is aggregate-scoped per AD-12: one active command per (interactive circuit, AggregateIdentity) from submit through terminal evidence; unrelated aggregates may proceed; bulk submission, toast batching, and multiple simultaneous commands for one aggregate remain prohibited. Story 1.0's global one-at-a-time policy is superseded historical evidence, and the AD-12-scoped behavior requires reverification under the 2026-07-15 correction. Applies to all command FRs. |
| FC-TOK | Status/severity/timeline tokens | **missing** | Use Tenants canonical vocabulary plus verified Fluent semantic/icon mapping until a shared token contract exists. |
| FC-AUD | `<AuditTimeline>` | **missing** | **Approved fallback (Product/UX, 2026-06-03): flat audit DataGrid** (FR-20). See [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md). |
| FC-CNS | `<ConsequencePreview>` | **missing** | **Approved fallback (Product/UX, 2026-06-03): inline consequence text** (CP-5). See [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md). |
| FC-A11Y | Accessibility primitives | **confirmed** | Confirmed by Story 1.0 spike note (2026-06-05). Per-story accessibility evidence remains required. |
| FC-L10N | Localization (shell resources) | **confirmed** | Shell boundary confirmed by Story 1.0 spike note (2026-06-05); Tenants-owned domain copy remains story-owned. |
| FC-DOC | Component documentation/reference | **confirmed** | Equivalent docs exist; Storybook absent. Per-story documentation/reference evidence remains required. |

A story promotes to `ready` only when its `blockedBy` set empties or a **Product/UX-approved** fallback is recorded. The three interim fallbacks (`FC-AUD`/`FC-CNS`/`FC-CNC`) are approved (2026-06-03 — see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md)); Story 1.0 confirms `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` (2026-06-05). Story 1.2 resolves the Tenants `FC-TBL` boundary with a Tenants-specific `TenantDataGrid`; reusable cursor/pinning/list-state capability remains FrontComposer-owned enhancement work. The 2026-06-06 correction reclassifies FR15 as a high-impact reversible lifecycle control eligible under approved command and preview fallbacks. FR-19 is not categorically blocked: it is gated on fixed-scope routing, freshness, last-administrator protection, and evidence gates. Epic 4 Stories 4.3 and 4.4 delivered grant/remove global-administrator command flows with fixed-scope routing, aggregate-scoped locking (AD-12), projection confirmation, and last-admin safety — historical evidence subject to reverification against the corrected contracts; Story 5.7 owns the complete global-administrator correction slice. Hard destructive tenant deletion remains out of scope for this phase and belongs to a future independent administrators-only CLI tool. Remaining gates are story-specific evidence, `FC-TOK` fallback discipline, reusable FrontComposer extraction for `FC-AUD`/`FC-CNS`, and live deferred audit/proof follow-ups tracked in `_bmad-output/implementation-artifacts/deferred-work.md`. See `tenants-ui-phase-2-story-backlog.md` for historical per-row `blockedBy`; `sprint-status.yaml` and `epics.md` are the current implementation handoff source after the 2026-06-05 correction. Confirmation and completion statements in this section are historical evidence, not readiness waivers: per the 2026-07-15 correction, affected completed work must be reverified against the corrected story contracts, and readiness claims require the §I prerequisite work packages.

## C. Backend surfaces consumed (do not add/alter)

- **Read queries — the authoritative six-read inventory (UI-READ-1):** `ListTenantsQuery`, `GetTenantQuery`, `GetTenantUsersQuery`, `GetUserTenantsQuery`, `GetTenantAuditQuery`, `GetGlobalAdministratorsQuery` → `GET /api/tenants`, `/api/tenants/{tenantId}`, `/api/tenants/{tenantId}/users`, `/api/users/{userId}/tenants`, `/api/tenants/{tenantId}/audit`, `/api/global-administrators`. All six UI reads route directly to Tenants REST; commands and status lookup stay on the EventStore command client; the generic EventStore query route is not a Tenants read path. This is the target contract; until UI-READ-1, PLAT-FRESH-1, and HOST-REF-1 are verified, the current transport normalizes provenance (§D, §I).
- **Commands** (via the command endpoint): create/edit/disable/enable tenant; `AddUserToTenant`, `ChangeUserRole`, `RemoveUserFromTenant`; set/remove configuration; set/remove global administrator.
- **Command endpoint:** `POST /api/v1/commands` (FrontComposer `EventStoreOptions.CommandEndpointPath`). **Resolved (2026-07-15):** this endpoint is preserved explicitly; no unversioned `/api/commands` alias is adopted (PRD §16.1).
- **No new backend endpoints** for consequence/receipt/command-status — the server-side BFF assembles and redacts consequence-preview, receipt, and rejection view models from already-loaded projection/read-model fields; rendered components receive only support-safe localized fields.

## D. Mechanism decisions, rejection/NoOp matrix & rationale (technical-how — not PRD body)

**Rejection / NoOp / always-emit matrix** (verified against `src/Hexalith.Tenants.Server/Aggregates/`; drives FR consequence text):

| Command / case | Backend behavior | UI reflection |
|---|---|---|
| `AddUserToTenant`, user already a member | **Rejection** `UserAlreadyInTenant` | safe localized text; **not** "already applied" |
| `AddUserToTenant`, empty tenant (`HasMembershipHistory == false`) | bootstrap: owner-only RBAC skipped | enables restore-after-last-owner (FR-24) |
| `ChangeUserRole` to the current role | **NoOp** (no event) | `already applied` |
| `ChangeUserRole`, escalation or `TenantRole.Unknown` | **Rejection** (`RoleEscalation`) | safe localized text |
| `UpdateTenant` (edit metadata) | **Always emits `TenantUpdated`** (no same-state suppression); RBAC = contributor or global admin | success only after projection confirm |
| `SetTenantConfiguration`, identical key+value | **NoOp** | `already applied` |
| `SetTenantConfiguration`, over limit | **Rejection** `ConfigurationLimitExceeded` | safe text |
| `RemoveTenantConfiguration`, missing key | **Rejection** `ConfigurationKeyNotFound` | safe text |
| Disable/Enable to a state already set | **Rejection** `TenantLifecycleStateAlreadySet`; RBAC = global admin only | safe text |
| Any command targeting a disabled tenant | **Rejection** `TenantDisabled` | safe text; disabled is an eventually-consistent signal |
| `RemoveUserFromTenant` of the **last owner** | **Allowed** (no ≥1-owner invariant) | elevated friction, never blocked (CP-6) |
| Remove the **last global administrator** | **Rejection** `LastGlobalAdministrator` | UI reflects as *unavailable*, not friction (CP-6) |
| `TenantAlreadyExists` on create | **Rejection** | safe text |

- **Freshness primitive:** server-side conditional requests (`If-None-Match` → `304`) over the six direct Tenants REST reads, with ETag, projection version, and read-model freshness propagated through the supported REST response contract on 200, 304, empty, and authorization-safe responses (PLAT-FRESH-1). `ServedAt` is never a substitute for projection age. The Truth State Badge derives `current/stale/unknown` from this provenance; `refreshing` remains client-transient, and `aging` is not claimed on the wire until authoritative projection-time provenance supports it. Until PLAT-FRESH-1, HOST-REF-1, and UI-READ-1 are verified, the current generic EventStore query route normalizes provenance to `unknown` freshness and freshness-dependent stories carry `blockedBy` metadata. Numeric thresholds deferred to implementation.
- **Live updates:** SignalR projection notifications are **freshness nudges only** — never advance command lifecycle or audit availability (PRD CP-4). Rationale: at-least-once delivery + projection lag make any optimistic confirmation unsafe.
- **Pagination:** signed, opaque, scope-bound cursors; never offset/limit. Memories search paging is no exception (SEARCH-CURSOR-1): the raw Memories offset is protected by the approved server-side cursor codec/DataProtection path, bound to (authenticated user, normalized query, status, sort, direction, page size), and kept out of visible copy, DOM attributes, logs, telemetry tags, and copy actions. On scope mismatch, decode failure, or invalidation, the list restarts from page 1 with an honest localized notice; the internal offset advances by raw hits consumed, including dropped malformed, duplicate, unauthorized, or unhydrated hits (AD-10). Cursor durability across replicas/restarts is a deferred backend epic (PRD §16.7, R-3).
- **Identity:** actor identity from JWT `sub` / envelope `UserId`; tenant ids and user ids are **meaningful caller-supplied strings, not ULIDs** (only envelope ids like `MessageId` may be ULIDs). See §E.
- **Rejections:** domain rejection events map to RFC 7807 Problem Details at the HTTP boundary; the server-side BFF assembles the support-safe rejection view model and the UI renders safe, localized text only (PRD §10).
- **Audit receipt:** assembled and redacted in the server-side BFF from a structured **NarrativePayload** (never the raw event payload); rendered components never receive raw `NarrativePayload`, event bodies, command payloads, tokens, internal correlations, ETags, or raw metadata. Target resolution rule is `userId` → `key` → `TenantId`; categories are `AuditEventCategory` = `Access` | `Administrative`.
- **No invitations:** `AddUserToTenant(TenantId, UserId, Role)` is a direct add; there are no invitation/pending-member events in the domain. An email-invitation flow would require new backend events (PRD §13).
- **Visual mapping:** six meaning→semantic-role mappings (tenant status, freshness, lifecycle, authorization, audit, risk) — see `tenants-ui-responsive-layout-and-visual-system-spec.md`; never hard-coded hex.
- **Pinned stack:** Blazor **InteractiveServer** with a server-side BFF (normative runtime — Blazor Auto is not); Fluent UI Blazor `5.0.0-rc.4-26180.1`, consumed centrally — exact component/icon/ARIA behavior must be verified at build time against the pinned package; do not assert a token name as available. Typed immutable state is required; Fluxor is not a mandatory architecture constraint.

## E. Naming & ID hazards

- **Namespace:** UI keys use the `ui-NN` prefix. Backend/FrontComposer epics also use `9-x/10-x/11-x/12-x` numbering. **Never conflate the two namespaces.**
- **ID-scheme spec discrepancy (must correct in the specs):** the operations-shell spec (and several others' technical notes) state tenant/user ids "are ULIDs." This contradicts the authoritative domain rule in `project-context.md`: **tenant ids and user ids are meaningful caller-supplied strings, NOT ULIDs**; only envelope ids such as `MessageId` may be ULIDs; do **not** `Guid.TryParse`/`Ulid.TryParse` a `TenantId`/`UserId`. The PRD follows the domain rule (PRD §4, §12 R-6, §16.12). The specs should be corrected so the "copy full id" affordance (FR-7) and any parsing logic do not assume a ULID.

## F. Options considered / deferred (for downstream UX & architecture)

- **Rich timeline vs. flat audit list:** flat list is the **Product/UX-approved** fallback for the first audit slice (FR-20) (approved 2026-06-03 — see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md)), because `<AuditTimeline>` does not exist; revisit if/when FC-AUD lands.
- **Optimistic UI vs. confirmed-only:** confirmed-only (non-collapse invariant) chosen deliberately over optimistic success, against the more common SaaS pattern, because correctness under eventual consistency is the product's core trust proposition.
- **Build missing components in Tenants vs. FrontComposer:** FrontComposer, per repo domain-boundary policy — Tenants must not absorb shared UI scaffolding.

## G. Canonical state sets (mirrored VERBATIM from the truth-state & audit specs — used as-written, no per-screen reinterpretation; PRD CP-10)

> Casing is significant and intentional. The badge uses space-form `audit pending`/`audit available`; the RemoveUserFromTenant state machine (truth-state §5.3) uses snake_case `projection_pending`/`audit_pending`/`audit_available`. **These are the same concepts, different tokens — do not unify.** Reason categories and feedback states are lowercase, space-separated.

**1. Truth State Badge — 13 states** *(truth-state §2.1–2.2; "exactly these thirteen")*: `current`, `refreshing`, `aging`, `stale`, `unknown`, `eligible`, `blocked`, `pending`, `accepted`, `confirmed`, `failed`, `audit pending`, `audit available`.

**2. Freshness — 5 states** *(truth-state §1/§2.2)*: `current` (usable), `refreshing` (usable nudge), `aging` (usable **with friction**), `stale` (**blocks**), `unknown` (**blocks**, fails closed). Fail-closed rule (§3.3): unknown freshness, indeterminate authorization, incomplete consequence preview, or missing lifecycle support each block destructive action by default.

**3. Command lifecycle — 10-token vocabulary** *(truth-state §1/§2.2)*: `eligible`, `previewed`, `submitted`, `accepted`, `rejected`, `already applied`, `failed`, `duplicate`, `timeout`, `unknown`. *(Spec names `duplicate` and `timeout` in the enumeration but gives them no dedicated gloss — do not invent one.)* The RemoveUserFromTenant worked state machine (§5.3) additionally uses snake_case `projection_pending`, `confirmed`, `audit_pending`, `audit_available`.

**4. Layered feedback — 10 states** *(truth-state §5.1; must not be merged — non-collapse §5.2)*: `request sent (submitted)`, `accepted`, `projection pending`, `confirmed`, `rejected`, `already applied`, `degraded` (**success-prohibited**), `audit pending`, `audit available`, `unable to verify` (**success-prohibited**). Invariant: `accepted` ≠ `confirmed` (projected) ≠ `audit available` (proven).

**5. Unavailable Action Reason — 6 categories** *(truth-state §4.1; with §4.4 evidence-source mapping)*: `missing permission` (authorization gate), `stale data` (freshness gate), `missing lifecycle support` (→ `FC-CMD`), `missing consequence preview` (→ `FC-CNS`), `missing audit proof` (→ `FC-AUD`), `high-impact flow not ready` (backlog `blockedBy` unresolved). The §4.3 grouping axis: missing permission vs. stale data vs. blocked risk vs. unavailable implementation dependency.

**6. Audit availability — 4 states** *(audit spec §4.1; do not collapse)*: `audit pending` (proof not yet available, expected), `audit delayed` (taking longer, capability exists), `audit unavailable` (path currently unavailable, e.g. read error — distinct from not-built), `missing implementation support` (capability not built, `FC-AUD`). *(truth-state §1 also lists bare `delayed`/`unavailable`/`approved fallback`; the audit spec normalizes to the prefixed forms above.)*

**7. Recovery verbs — canonical allowed terms** *(audit §5.1; truth-state §5.4)*: `start correction`, `restore intended access`, `retry status lookup`, `inspect audit`, `escalate` (with a support-safe reference); plus general paths `refresh`, `wait`, `continue read-only`, `request permission`, `start a compensating command`. **Prohibited:** `undo`, `rollback`, `hidden edit`.

## H. Consequence Preview content set (mirrored from remove-user-journey §2.1)

The Consequence Preview must present (canonical 10-item set in **remove-user-journey §2.1**; key items):
- owner-count impact (incl. the last-owner / zero-owner case);
- the specific access being revoked / changed;
- the recovery path available afterward;
- the audit expectation (what evidence will exist);
- current freshness of the inputs;
- target's platform standing (e.g. also a global administrator);
- explicit **known consequences** vs. **known unknowns** (no over-claiming — e.g. session/token invalidation is a known-unknown unless proven).

Incomplete inputs to this set **block submission** (fail-closed, CP-5). The preview's inline rendering (the `FC-CNS` fallback) is **Product/UX-approved** (2026-06-03 — see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md); §B). Preview assembly and redaction occur in the server-side BFF (§C, §D); the inline rendering receives only the support-safe view model.

## I. Prerequisite work packages (2026-07-15 readiness correction)

Introduced by the approved [Sprint Change Proposal 2026-07-15](./../../sprint-change-proposal-2026-07-15.md); architecture authority is AD-1..AD-14 in `architecture.md`. These are technical prerequisites, not user stories; freshness-, search-, and production-dependent stories carry `blockedBy` metadata until the relevant packages are verified. The proposal authorizes no implementation inside root-declared submodules — each shared-platform package needs a separately scoped task and owner.

| ID | Scope | Owner domain |
|---|---|---|
| PLAT-FRESH-1 | Propagate ETag, projection version, and read-model freshness through the supported Tenants REST response contract (200, 304, empty, authorization-safe); never substitute `ServedAt` for projection age. | EventStore/Tenants platform |
| HOST-REF-1 | Platform/composing host exposes separate Tenants-query and EventStore-command service references; the repository AppHost is not expanded with shared orchestration capability. | Platform/composing host |
| UI-READ-1 | Route all six reads (§C) directly to Tenants; keep commands and status lookup on the EventStore command client; remove the generic EventStore query route from Tenants UI reads. | Tenants UI |
| SEARCH-CURSOR-1 | Protect the Memories search offset with the server-side cursor codec/DataProtection path, scope-bound and support-safe, with page-1 recovery and cross-user isolation tests (§D Pagination). | Tenants UI |
| WP-2A | Minimum removal audit proof: BFF-assembled, redacted removal-proof view model over the existing audit read path (no new receipt endpoint), covering pending/delayed/unavailable/available audit states without false success. | Tenants UI |
| PLATFORM-OPS-1 | Migrate topology ownership to a platform/composing host; consume shared ServiceDefaults, health, OpenTelemetry, configuration, secrets, and non-root container defaults; keep InteractiveServer at one replica until DataProtection, circuit/session routing, and cursor durability are verified. | Platform/composing host |
