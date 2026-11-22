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
| 7.9 Compensating Recovery (FR-24..25) | Epic 5 Stories 5.5–5.6 (tenant) and 5.7 (global administrator) | audit-evidence-and-compensating-recovery | 2c |

The tracker records delivery of receipt assembly (5.3), tenant correction (5.5–5.6), and global-administrator correction (5.7). The two correction paths have different proof capabilities (§D). The historical ui-09 backlog row covers both FR-10 and FR-11 under a shared availability check. [Sprint tracker](../../../implementation-artifacts/sprint-status.yaml#L75)

## B. FrontComposer dependency readiness (from the dependency map)

| ID | Capability | Readiness | Notes / fallback |
|---|---|---|---|
| FC-TBL | Projection list/table (DataGrid, filter/search/empty/loading) | **available with resolved Tenants boundary** | Generated projection grid lacks Tenants-required cursor pagination, safety-column pinning, and six non-collapsing list states by itself. Story 1.2 resolves the Tenants path with a Tenants-specific `TenantDataGrid`; reusable cursor/pinning/list-state capability remains FrontComposer-owned enhancement work. |
| FC-LYT | Shell layout contract (full-width vs constrained) | **confirmed** | Confirmed by Story 1.0 spike note (2026-06-05). |
| FC-CMD | Command lifecycle feedback (three-phase, projection-confirmed) | **confirmed** | Confirmed by Story 1.0 spike note (2026-06-05). Required for ALL command FRs. |
| FC-CNC | Concurrent-command / toast batching policy | **confirmed** | AD-12 admits one active command per interactive circuit and aggregate identity. A membership projection timeout retains admission. Enable/disable and configuration set/remove may end local tracking on explicit abandonment or bounded retention expiry. The remote outcome can remain unknown. Any new write needs current evidence and fresh admission under AD-12. Unrelated aggregates may proceed. Bulk submission and toast batching remain outside this contract. The original June 3 fallback serialized all commands. |
| FC-TOK | Status/severity/timeline tokens | **missing** | Use Tenants canonical vocabulary plus verified Fluent semantic/icon mapping until a shared token contract exists. |
| FC-AUD | `<AuditTimeline>` | **missing** | **Approved fallback (Product/UX, 2026-06-03): flat audit DataGrid** (FR-20). See [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md). |
| FC-CNS | `<ConsequencePreview>` | **missing** | **Approved fallback (Product/UX, 2026-06-03): inline consequence text** (CP-5). See [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md). |
| FC-A11Y | Accessibility primitives | **confirmed** | Confirmed by Story 1.0 spike note (2026-06-05). Per-story accessibility evidence remains required. |
| FC-L10N | Localization (shell resources) | **confirmed** | Shell boundary confirmed by Story 1.0 spike note (2026-06-05); Tenants-owned domain copy remains story-owned. |
| FC-DOC | Component documentation/reference | **confirmed** | Equivalent docs exist; Storybook absent. Per-story documentation/reference evidence remains required. |

The three interim fallbacks received Product/UX approval on June 3. The July readiness review required story-specific evidence for the shell, command lifecycle and aggregate-scoped locking. Story 1.2 delivered the Tenants grid. The tracker maps FR-19 grant to Story 4.2, removal to 4.3, and audit correction to 5.7. June artifacts used 4.3 and 4.4 for grant and removal. Each attempt still requires the caller’s authority, fixed-scope routing, fresh complete evidence, and last-administrator protection. [Fallback approval](../../fallback-approval-record-2026-06-03.md) · [Story mapping](../../../implementation-artifacts/release-readiness.md#L127) · [Sprint tracker](../../../implementation-artifacts/sprint-status.yaml#L69)

FR-15 covers reversible disable/enable through the approved command and preview controls. Hard deletion is outside UI scope. Any future proposal for a CLI/MCP operation must meet contract enrollment and authorization requirements for `Hexalith.McpCli`. [September ownership decision](../../architecture.md#L66)

Use the historical per-row `blockedBy` values in the Phase 2 backlog to trace planning decisions. For delivery and release qualification, use the current sprint tracker, §I and the audit-specific owner actions in §J. Story-specific UX evidence and the `FC-TOK` fallback still apply. Shared timeline and preview components remain FrontComposer enhancements.

## C. Backend surfaces consumed (do not add/alter)

The server-side UI client sends GET requests directly to the Tenants REST API. The audit operation selects a route according to its scope. Commands and command-status lookup use the EventStore client. [Typed REST client](../../../../src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs#L47) · [Host wiring](../../../../src/Hexalith.Tenants.AppHost/Program.cs#L212)

| Read operation | GET path |
| --- | --- |
| `ListTenantsQuery` | `/api/tenants` |
| `GetTenantQuery` | `/api/tenants/{tenantId}` |
| `GetTenantUsersQuery` | `/api/tenants/{tenantId}/users` |
| `GetUserTenantsQuery` | `/api/users/{userId}/tenants` |
| `GetTenantAuditQuery` | `/api/tenants/{tenantId}/audit` for tenant evidence, `/api/global-administrators/audit` for the fixed `system` / `global-administrators` stream |
| `GetGlobalAdministratorsQuery` | `/api/global-administrators` |

**FR-6 configuration display.** The authorized detail API returns the full configuration. Before composing UI state, the BFF requires an exact display-safe key and an ordinal match to a prefix granted to the caller. Global administrators bypass only the prefix check. An unlisted key stays hidden even under a granted prefix and even for an administrator. Missing or malformed policy withholds configuration from the UI. Raw configuration is removed from composed state. [Approved DW-599 policy](../../../implementation-artifacts/dw-117-118-127-599-618-validation-evidence.md#L10) · [Safe composer](../../../../src/Hexalith.Tenants.UI/Services/Configuration/TenantConfigurationSafeComposer.cs#L177)

- **Commands** (via the command endpoint): create/edit/disable/enable tenant; `AddUserToTenant`, `ChangeUserRole`, `RemoveUserFromTenant`; set/remove configuration; set/remove global administrator.
- **Command endpoint:** `POST /api/v1/commands` (FrontComposer `EventStoreOptions.CommandEndpointPath`). **Resolved (2026-07-15):** this endpoint is preserved explicitly; no unversioned `/api/commands` alias is adopted (PRD §16.1).
- **No new backend endpoints** for consequence/receipt/command-status — the server-side BFF assembles and redacts consequence-preview, receipt, and rejection view models from already-loaded projection/read-model fields; rendered components receive only support-safe localized fields.

## D. Mechanism decisions, rejection/NoOp matrix & rationale (technical-how — not PRD body)

**Rejection / NoOp / always-emit matrix** (verified against `src/Hexalith.Tenants.Server/Aggregates/`; drives FR consequence text):

| Command / case | Backend behavior | UI reflection |
|---|---|---|
| `AddUserToTenant`, user already a member | **Rejection** `UserAlreadyInTenant` | safe localized text; **not** "already applied" |
| `AddUserToTenant`, active tenant with no membership history | The first-member bootstrap bypasses the aggregate’s owner check. Adding the first member permanently sets `HasMembershipHistory`. | Only the initial add qualifies for bootstrap. |
| `AddUserToTenant`, previously populated active tenant | A tenant owner or global administrator must authorize the add. Removing the last member leaves `HasMembershipHistory` set. | If no owner remains, a global administrator must restore an owner with an explicit role (FR-24). |
| `ChangeUserRole` on an active tenant | A tenant owner or global administrator can assign reader, contributor, or owner to an existing member. A change to the same role is a NoOp after authorization. | A reader-to-contributor promotion is valid. Show `already applied` for the authorized NoOp. |
| `ChangeUserRole`, unauthorized caller or unassignable target role | An unauthorized caller receives `InsufficientPermissions`. For an authorized caller, `Unknown` or an out-of-range target yields `RoleEscalation`. | Show the applicable localized rejection. |
| `UpdateTenant` on an active tenant | Tenant contributors, tenant owners and global administrators can edit metadata. Every accepted edit emits `TenantUpdated`, including identical values. | Confirm success after the projection reflects the edit. |
| `SetTenantConfiguration`, identical key+value | **NoOp** | `already applied` |
| `SetTenantConfiguration`, over limit | **Rejection** `ConfigurationLimitExceeded` | safe text |
| `RemoveTenantConfiguration`, missing key | **Rejection** `ConfigurationKeyNotFound` | safe text |
| `EnableTenant` or `DisableTenant` | A global administrator may enable a disabled tenant or disable an active tenant. Repeating the existing state yields `TenantLifecycleStateAlreadySet`. A caller without global-administrator authority receives `InsufficientPermissions`. | Show the rejection safely or confirm the transition against the projection. |
| Metadata, membership, role or configuration mutations on a disabled tenant | The aggregate rejects the mutation with `TenantDisabled`. | Show the reason. Consumers observe disablement asynchronously. |
| `RefreshTenantAccess` on a disabled tenant | A tenant owner or global administrator may request a bounded access snapshot for the matching tenant ID. It carries the disabled status. | Consumer access remains denied. |
| An authorized caller removes the last owner of an active tenant | The aggregate allows zero owners. | Require elevated confirmation and explain the global administrator’s role in restoring access (CP-6, FR-24). |
| Remove the **last global administrator** | **Rejection** `LastGlobalAdministrator` | UI reflects as *unavailable*, not friction (CP-6) |
| `CreateTenant`, caller is not a global administrator | **Rejection** `InsufficientPermissionsRejection` | UI shows `missing permission`; if the command is dispatched anyway, the rejection maps to `InsufficientPermissions`, and the UI renders safe localized text |
| `CreateTenant`, tenant already exists | **Rejection** `TenantAlreadyExists` | safe text |

The aggregate handlers define authorization and transition order. The state retains membership history across removals. [Tenant aggregate](../../../../src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#L40) · [Membership history](../../../../src/Hexalith.Tenants.Server/Aggregates/TenantState.cs#L42)

- **Freshness on each read.** A successful `200` read can return an empty collection. Freshness derives from projection-backed, non-degraded metadata on that response. Current or stale lifecycle, or a definite stale header when lifecycle is absent, determines `current` or `stale`. Missing, conflicting, unsupported or degraded evidence yields `unknown`. A `304` may retain the caller’s scoped snapshot only with a matching strong ETag, projection version, and supported current or stale provenance. Authorization failures withhold the data. `ServedAt` records when the server served the response. `refreshing` is transient client state, and `aging` is dormant on this wire contract. [REST metadata handling](../../../../src/Hexalith.Tenants.UI/Services/Gateways/TenantsRestQueryClient.cs#L406)
- **Live updates:** SignalR projection notifications are **freshness nudges only** — never advance command lifecycle or audit availability (PRD CP-4). Rationale: at-least-once delivery + projection lag make any optimistic confirmation unsafe.
- **Pagination.** The UI uses protected opaque cursors. The search cursor binds the caller, index, normalized search, status, sort, direction and page size. A scope mismatch or invalid cursor restarts at page one with a localized notice. Version 2 records the fixed raw-window start and the consumed position within its returned results. An operational hydration gap or exhausted scan budget switches to the labelled authorized-list fallback. Keep cursor data out of visible copy, DOM attributes, logs, telemetry and copy actions. Cross-replica durability requires deployment evidence (PRD §16.7). [Search cursor scope](../../../../src/Hexalith.Tenants.UI/Services/Gateways/TenantSearchCursorScopes.cs#L14) · [Approved search policy](../../../implementation-artifacts/dw-117-118-127-599-618-validation-evidence.md#L9)
- **Identity:** actor identity from JWT `sub` / envelope `UserId`; tenant ids and user ids are **meaningful caller-supplied strings, not ULIDs** (only envelope ids like `MessageId` may be ULIDs). See §E.
- **Rejections:** domain rejection events map to RFC 7807 Problem Details at the HTTP boundary; the server-side BFF assembles the support-safe rejection view model and the UI renders safe, localized text only (PRD §10).
- **Audit receipt:** assembled and redacted in the server-side BFF from a structured **NarrativePayload** (never the raw event payload); rendered components never receive raw `NarrativePayload`, event bodies, command payloads, tokens, internal correlations, ETags, or raw metadata. Target resolution rule is `userId` → `key` → `TenantId`; categories are `AuditEventCategory` = `Access` | `Administrative`.
- **Tenant corrections (FR-25).** Confirmation requires the expected postcondition in a fresh ordered projection that has reached the corrective command’s verified committed sequence. The tenant audit DTO exposes an event ID and cannot prove which corrective command caused it. The panel retains original evidence, clears the paired link and reports `MissingSupport`. The operator can inspect the authorized audit trail or escalate with the original support-safe reference. Matching target, event type and time alone cannot establish causation. [Story 5.6 disposition](../../../implementation-artifacts/5-6-preview-and-confirm-correction-with-linked-proof.md#L11) · [Proof handling](../../../../src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs#L402)
- **Global-administrator corrections (FR-25).** Story 5.7 binds the attempt to the caller and the original fixed-scope evidence in the same circuit. Current authority, a complete ordered projection, preview, admission and last-administrator protection govern submission. An already-applied change sends no command. Confirmation requires a fresh complete projection at the attempt’s verified committed sequence or later and the expected postcondition. Display paired receipts only after independently rechecking both authorized audit records and their causal association to that attempt. Retain the original evidence when proof is missing. Deployment owners replay a legacy singleton projection from durable history before enabling correction against it. [Story 5.7 acceptance](../../../implementation-artifacts/spec-5-7-correct-global-administrator-authority-from-audit-evidence.md#L73)
- **False-success measurement.** PRD §15 SM-6 proposes a cohort, reporting window and observation coverage measure for Product and Release to confirm. A projection-confirmed change and a verified pair of audit receipts are separate observable claims. Record only support-safe evidence for each.
- **No invitations:** `AddUserToTenant(TenantId, UserId, Role)` is a direct add; there are no invitation/pending-member events in the domain. An email-invitation flow would require new backend events (PRD §13).
- **Visual mapping:** six meaning→semantic-role mappings (tenant status, freshness, lifecycle, authorization, audit, risk) — see `tenants-ui-responsive-layout-and-visual-system-spec.md`; never hard-coded hex.
- **Pinned stack.** The UI uses Blazor InteractiveServer, a server-side BFF and typed immutable state. Fluxor is optional. The centrally imported Builds package table selects Fluent UI Blazor components and icons 5.1.0 at the reviewed revision. Validate the restored versions, component semantics, ARIA behavior, and WCAG 2.2 evidence on the qualifying build. [Central import](../../../../Directory.Packages.props#L11) · [Package versions](../../../../references/Hexalith.Builds/Props/Directory.Packages.props)

## E. Naming & ID hazards

- **Namespace:** UI keys use the `ui-NN` prefix. Backend/FrontComposer epics also use `9-x/10-x/11-x/12-x` numbering. **Never conflate the two namespaces.**
- **Identifier handling (FR-7).** Tenant and user IDs are literal caller-supplied strings. Preserve their full value for support-safe copy and avoid GUID or ULID parsing. Envelope identifiers such as `MessageId` follow their own contract. The operations-shell spec records the tenant and user rule. [Operations shell](../../../../docs/tenants-ui-operations-shell-spec.md#L186)

## F. Options considered / deferred (for downstream UX & architecture)

- **Rich timeline vs. flat audit list:** flat list is the **Product/UX-approved** fallback for the first audit slice (FR-20) (approved 2026-06-03 — see the [Fallback Approval Record](./../../fallback-approval-record-2026-06-03.md)), because `<AuditTimeline>` does not exist; revisit if/when FC-AUD lands.
- **Audit performance (FR-20, NFR-1).** The owner approved the 500-entry, 50-row-page workload on September 23. The September 24 dedicated run met all 126 percentile groups and ten functional gates. Initial render must meet p75 ≤ 2.5 s and p95 ≤ 4.0 s, filter or page results p75 ≤ 1.5 s and p95 ≤ 3.0 s, and visible feedback p75 ≤ 200 ms and p95 ≤ 500 ms. Rerun the full contract on the release candidate and after audit query, BFF mapping, grid, or paging changes. If a valid batch exceeds either percentile budget, reduce the page size to 25 rows and rerun the full contract. If the 25-row grid still exceeds a budget, use bounded rendering and rerun the full contract. The deployment index measurement in §J addresses a separate workload. [Approved contract](../../../implementation-artifacts/story-5-1-performance-decision.md) · [Accepted run](../../../implementation-artifacts/story-5-1-performance-evidence.md)
- **Optimistic UI vs. confirmed-only:** confirmed-only (non-collapse invariant) chosen deliberately over optimistic success, against the more common SaaS pattern, because correctness under eventual consistency is the product's core trust proposition.
- **Build missing components in Tenants vs. FrontComposer:** FrontComposer, per repo domain-boundary policy — Tenants must not absorb shared UI scaffolding.

## G. Canonical state sets (mirrored VERBATIM from the truth-state & audit specs — used as-written, no per-screen reinterpretation; PRD CP-10)

> Casing is significant and intentional. The badge uses space-form `audit pending`/`audit available`; the RemoveUserFromTenant state machine (truth-state §5.3) uses snake_case `projection_pending`/`audit_pending`/`audit_available`. **These are the same concepts, different tokens — do not unify.** Reason categories and feedback states are lowercase, space-separated.

**1. Truth State Badge — 13 states** *(truth-state §2.1–2.2; "exactly these thirteen")*: `current`, `refreshing`, `aging`, `stale`, `unknown`, `eligible`, `blocked`, `pending`, `accepted`, `confirmed`, `failed`, `audit pending`, `audit available`.

**2. Freshness — 5 states** *(truth-state §1/§2.2)*: `current` (usable), `refreshing` (usable nudge), `aging` (usable **with friction**), `stale` (**blocks**), `unknown` (**blocks**, fails closed). Fail-closed rule (§3.3): unknown freshness, indeterminate authorization, incomplete consequence preview, or missing lifecycle support each block destructive action by default.

**3. Command lifecycle — 10-token vocabulary** *(truth-state §1/§2.2)*: `eligible`, `previewed`, `submitted`, `accepted`, `rejected`, `already applied`, `failed`, `duplicate`, `timeout`, `unknown`. *(Spec names `duplicate` and `timeout` in the enumeration but gives them no dedicated gloss — do not invent one.)* The RemoveUserFromTenant worked state machine (§5.3) additionally uses snake_case `projection_pending`, `confirmed`, `audit_pending`, `audit_available`.

**4. Layered feedback — 10 states** *(truth-state §5.1; must not be merged — non-collapse §5.2)*: `request sent (submitted)`, `accepted`, `projection pending`, `confirmed`, `rejected`, `already applied`, `degraded` (**success-prohibited**), `audit pending`, `audit available`, `unable to verify` (**success-prohibited**). Invariant: `accepted` ≠ `confirmed` (projected) ≠ `audit available` (proven).

**5. Unavailable Action Reason — 6 categories** *(truth-state §4.1; with §4.4 evidence-source mapping)*: `missing permission` (authorization gate), `stale data` (freshness gate), `missing lifecycle support` (→ `FC-CMD`), `missing consequence preview` (→ `FC-CNS`), `missing audit proof` (→ `FC-AUD`), `high-impact flow not ready` (backlog `blockedBy` unresolved). The §4.3 grouping axis: missing permission vs. stale data vs. blocked risk vs. unavailable implementation dependency.

**6. Audit availability — 4 states** *(audit spec §4.1; do not collapse)*: `audit pending` (proof not yet available, expected), `audit delayed` (taking longer, capability exists), `audit unavailable` (path currently unavailable, e.g. read error — distinct from not-built), `missing implementation support` (the required evidence capability is absent, as with tenant correction pairing in §D). *(truth-state §1 also lists bare `delayed`/`unavailable`/`approved fallback`; the audit spec normalizes to the prefixed forms above.)*

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

The July 15 proposal introduced these prerequisites. Stories 1.9, 1.10 and 2.4 have since delivered protected search paging, direct reads with response provenance, and minimum removal proof. Local completion and the external deployment checks in §J have separate evidence. [July proposal](../../sprint-change-proposal-2026-07-15.md) · [Sprint tracker](../../../implementation-artifacts/sprint-status.yaml#L48)

| ID | Current evidence or qualification | Owner domain |
|---|---|---|
| PLAT-FRESH-1 | Story 1.10 verified generated controller and metadata behavior. Apply the per-response checks in §D, including on conditional and authorized empty reads. | EventStore/Tenants platform |
| HOST-REF-1 | Closed by owner-approved AppHost wiring. The UI receives separate Tenants API and EventStore references. [Host decision](../../../implementation-artifacts/story-1-10-direct-tenants-reads-and-authoritative-freshness-evidence-2026-07-28.md#L250) | Platform/composing host |
| UI-READ-1 | Story 1.10 delivered all six typed direct reads (§C). Commands and status lookup use EventStore. [Transport evidence](../../../implementation-artifacts/story-1-10-direct-tenants-reads-and-authoritative-freshness-evidence-2026-07-28.md#L228) | Tenants UI |
| SEARCH-CURSOR-1 | Story 1.9 delivered protected scope-bound search paging and recovery. See the subsequent v2 continuation policy in §D. | Tenants UI |
| WP-2A | Story 2.4 delivered the minimum support-safe removal proof through the existing audit read. Story 5.6’s tenant-correction pairing limitation is documented separately in §D. | Tenants UI |
| PLATFORM-OPS-1 | Migrate topology ownership to a platform/composing host; consume shared ServiceDefaults, health, OpenTelemetry, configuration, secrets, and non-root container defaults; keep InteractiveServer at one replica until DataProtection, circuit/session routing, and cursor durability are verified. | Platform/composing host |

## J. Release and deployment evidence

The November 20 audit records eight local corrections against the existing requirements. The roster and administrator-grant outcomes are in FR-8 and FR-19. NFR-2 covers Production identity. The following details define what the local checks establish and what each owner still needs to observe. [Approved audit](../../sprint-change-proposal-2026-11-20.md) · [Sprint tracker](../../../implementation-artifacts/sprint-status.yaml)

For FR-12, a service that enforces membership uses a durable checkpoint in its own shared store and authorized access to tenant history. It denies or defers protected work when the snapshot is missing, unhealthy, unreadable, or behind a known required sequence. After a detected gap, it resumes from the repaired checkpoint. Access requires an active tenant and a role that permits the operation. [Consumer walkthrough](../../../../docs/sample-consuming-service-walkthrough.md)

The sample’s in-memory access and configuration readouts are available only in Development. Its Dapr subscription and health endpoints remain available in other environments. Developers following the quickstart need PowerShell 7 and the Aspire CLI bundle. [Quickstart](../../../../docs/quickstart.md)

| Qualification check | Owner | Acceptance evidence |
| --- | --- | --- |
| Hosted release qualification | Release owner | For each candidate, run `rest` (including cold bootstrap), `persisted`, `anonymous-ui` and `authenticated-ui` on its shipping package set. All four passed for Tenants `118293e1` and `9196587b`. [Job results](../../sprint-change-proposal-2026-11-21.md) |
| First enforcement consumer | Consumer deployment owner | With its deployed shared store and history permissions, demonstrate that a delayed removal survives restart and replay of an earlier grant. |
| Production identity | Deployment owner | Verify issuer, discovery over TLS, audience, key rotation, and ingress in the target environment. Record the outcome without credentials. |
| Tenant-index capacity | Deployment owner | Measure index size, write contention, and update latency under representative load before deciding on partitioning. |

In local qualification, all four host manifests resolved EventStore.Contracts 5.4.0 and McpCli.Abstractions 3.0.0. On a fixture of 10,000 tenants and 50,000 memberships, the index fingerprint retained its digest while allocation per fingerprint fell from 31.4 to 27.2 MiB.
