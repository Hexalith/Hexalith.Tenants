---
stepsCompleted: [1]
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md
---

# tenants - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for tenants, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: A platform operator can scan, search, filter, sort, and cursor-page the complete tenant set; each row shows tenant identity, status, member count, owner count, pending state, and freshness. The list keeps `loading`, `empty`, `filtered-empty`, `error`, `stale`, and `degraded` distinct, never hides pending or stale markers during sort/page changes, and never leaks out-of-scope tenants. A non-empty Name/TenantId search performs a server round-trip to the `tenants-index` Memories syntactic/BM25 index, then hydrates authoritative rows through direct Tenants REST reads; empty search returns the normal cursor list. Search uses an authenticated-user/query/status/sort/direction/page-size-bound protected cursor, advances by raw hits consumed, restarts honestly at page 1 on invalidation, applies an exact authoritative status filter, tolerates index lag, and falls back non-blockingly to the normal cursor list when Memories is unavailable.

FR2: A user can open a deep-linkable tenant detail surface from the list and return with the previous tab, scope, search, filter, sort, selection, cursor context, and scroll position preserved.

FR3: A signed-in user can self-audit "My Tenants" by viewing only the authorized tenants they belong to, with their role and each tenant's status shown.

FR4: An operator can search for a user, open that user's authorization-scoped tenant memberships, and reach the same view from a member row; no visible memberships renders as an explicit safe empty state rather than an error, and the surface is not presented as an exhaustive all-users inventory.

FR5: A user can view a tenant overview containing status, metadata, member and configuration summaries, member and owner counts, lifecycle state, and freshness, with status/lifecycle expressed through text and icon as well as color.

FR6: A user can view tenant configuration key/value pairs in read-only mode, grouped by namespace and restricted to namespaces they own or are authorized to see; values outside the caller's prefix are hidden and sensitive-value display remains outside the read MVP.

FR7: A user can copy the complete literal caller-supplied TenantId, UserId, or support-safe reference even when the displayed value is truncated; copied content never exposes payloads, tokens, internal correlations, ETags, raw metadata, stack traces, or PII and never assumes the identifier is a ULID or GUID.

FR8: A user can review a read-only tenant member table showing identity, role, owner count, status, freshness, and orphan/disabled context; it must not imply mutation and exposes accessible headers, sort state, and row relationships.

FR9: A user can see which member actions would be available and, for unavailable actions, an inline, hover-independent, plain-language explanation drawn from the six canonical Unavailable Action Reason categories.

FR10: An authorized user can add a user directly to a tenant by caller-supplied user id with an explicit role; there is no invitation or pending-member step, and an existing member is rejected as `UserAlreadyInTenant` with safe localized text rather than treated as a NoOp.

FR11: An authorized user can change a member's role; changing to the current role is a NoOp shown as `already applied`, escalation and `TenantRole.Unknown` targets are rejected safely, and success appears only after authoritative projection confirmation.

FR12: An authorized user can remove a user's tenant access through validation, fail-closed freshness and authorization gating, a complete Consequence Preview, elevated friction, aggregate-scoped command locking, projection-confirmed lifecycle tracking, and minimum audit proof. Removing the last owner is allowed with extra friction, a target who is also a global administrator raises platform-level friction without changing command scope, duplicate/already-applied removal does not double-apply, and unverifiable outcomes are never shown as success.

FR13: An authorized operator can create a tenant; an existing tenant id is rejected as `TenantAlreadyExists`, and creation is shown as successful only after authoritative projection confirmation.

FR14: An authorized tenant contributor or global administrator can edit tenant metadata; each successful request emits `TenantUpdated` without same-state suppression, validation errors use safe localized field messages, and completion is projection-confirmed.

FR15: An authorized global administrator can disable or enable a tenant through a high-impact Consequence Preview flow; setting the already-set state is rejected as `TenantLifecycleStateAlreadySet`, disabled status is an eventually-consistent availability signal, commands against a disabled tenant are rejected as `TenantDisabled`, and success appears only after projection confirmation. This is reversible lifecycle control, not hard tenant deletion.

FR16: An authorized user can set a namespaced configuration key/value through a complete Consequence Preview; identical key/value is a NoOp shown as `already applied`, domain-limit violations are rejected as `ConfigurationLimitExceeded`, and every eligible configuration mutation requires preview in v1 with no low-risk bypass.

FR17: An authorized user can remove a configuration key through a complete Consequence Preview; a missing key is rejected as `ConfigurationKeyNotFound`, every eligible removal requires preview in v1, and success appears only after projection confirmation.

FR18: An authorized operator can review global administrators separately from tenant membership; tenant owners cannot see the surface, data comes from the fixed-identity `global-administrators` aggregate, and each row shows identity plus authoritative freshness.

FR19: An authorized operator can grant or remove a global administrator in the fixed `global-administrators` scope except the last one; removal of the last global administrator is rejected as `LastGlobalAdministrator` and shown as unavailable rather than as completable friction, and these actions are never conflated with tenant membership.

FR20: A user can browse a tenant audit trail as a flat, stably ordered, cursor-paginated DataGrid with date and `AuditEventCategory` (`Access`/`Administrative`) filters; `loading`, `empty`, `filtered-empty`, and `error` remain distinct and accessible. Performance must satisfy the Product/Operations-approved audit contract once its representative dataset, budgets, test tier, repeatability method, and paging/virtualization fallback trigger are decided; no unsupported numeric budget is claimed beforehand.

FR21: A user can reach audit evidence from contextual tenant-row, tenant-detail, user-lookup, and command-result entry points, each scoped to the relevant tenant, user, or command; audit is not a separate Tenants shell navigation entry.

FR22: A user can view a support-safe Audit Evidence Receipt containing actor, target, tenant scope, outcome, absolute timestamp, projection marker, and audit/command reference; the server-side BFF assembles and redacts it from structured `NarrativePayload` without a new receipt endpoint, rendered state never contains raw evidence fields, and partial completion shows the actual lifecycle state rather than fabricated proof.

FR23: A user can distinguish `audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support`; none is shown as success and each provides an appropriate wait, retry, continue-read-only, inspect-audit, or escalation path.

FR24: From audit evidence, an authorized user can start a forward compensating command such as `restore intended access` or `start correction`; it has its own current-state preview and proof, never edits the original event, and is never labeled `undo`, `rollback`, or `hidden edit`.

FR25: A user can preview a correction against current state, submit it in the correct tenant or `global-administrators` command scope, and link the original and corrective audit records; success appears only after authoritative projection confirmation.

### NonFunctional Requirements

NFR1: Reads use cursor pagination and conditional requests with authoritative ETag, projection-version, and read-model-freshness provenance from the six direct Tenants REST reads; `ServedAt` is never substituted for projection age. Typical warm tenant list/detail/member surfaces target interactive rendering within about one second; audit performance remains governed by an unapproved Product/Operations decision record and carries no numeric claim until approved.

NFR2: Authorization is enforced by APIs and domain logic; the UI only reflects it, fails closed when it is indeterminate, remains safe when its reflection is wrong, and prevents authorization-scoped reads or empty/error states from revealing other tenants, memberships, or global-administrator data.

NFR3: The UI is correct under eventual consistency, at-least-once delivery, projection lag, duplicate signals, and InteractiveServer reconnects; authoritative projection re-query is the source of command and audit truth, SignalR is only a refresh nudge, and optimistic or resurrected success is forbidden.

NFR4: Every interactive element and status exposes a stable automation selector/component contract that does not depend on row text, color, or incidental Fluent-generated markup.

NFR5: The UI never edits, deletes, or rewrites events, projections, read models, or state-store data to repair business state; every correction is a forward compensating command.

NFR6: Accessibility meets WCAG 2.1 AA and targets WCAG 2.2 AA only where verified against the pinned Fluent/FrontComposer stack; all workflows are keyboard-operable and completable or safely exitable, with logical and visible focus, focus traps and return, accessible table/status semantics, no-color-only meaning, reduced-motion independence, forced-colors support, and correct live-region politeness.

NFR7: Localization uses Tenants-owned, culture-aware whole resource strings with named placeholders for domain copy, including state labels, roles, timestamps, warnings, unavailable reasons, recovery actions, confirmations, rejections, and every empty/loading/error/degraded/stale/unavailable state; runtime sentence-fragment assembly is forbidden.

NFR8: Support-safety and privacy are hard boundaries: no rendered, copied, announced, logged, serialized, or telemetry-visible value may expose bearer tokens, decoded JWTs, command/event payloads, raw `NarrativePayload`, EventStore metadata, protected cursors, ETags, internal message/correlation ids, stack traces, or real PII.

NFR9: Responsive behavior is desktop-first while remaining safe at tablet and mobile widths; safety-critical identity/status/freshness/role/risk context never drops, mobile is read-only for triage/lookup/audit reference, and any viewport unable to preserve a complete high-impact context makes the action unavailable with an inline reason.

NFR10: A UI story is not ready or complete without applicable accessibility, localization, responsive, documentation/reference, and focused test evidence, or a Product/UX-approved row-specific fallback that records behavior, copy ownership, documentation evidence, replacement path, and owner approval.

### Additional Requirements

- **Starter/foundation requirement:** the selected starter is the existing `src/Hexalith.Tenants.UI` .NET 10 Blazor `InteractiveServer` web host composing FrontComposer Shell and Fluent UI Blazor V5. Epic 1 Story 1 must establish or reverify this bootstrap foundation rather than scaffold a parallel host: solution registration, one domain manifest, shell routing, authentication, BFF gateways, typed truth state, localization, tests, and SDK-container configuration. The historical `dotnet new blazor --interactivity Server -f net10.0` recipe is initialization history, not permission to recreate implemented work.
- Treat architecture AD-1 through AD-14 as the canonical precedence layer whenever older descriptive sections or historical implementation evidence conflict.
- Register exactly one Tenants shell entry at `/tenants`. Canonical workspace state uses page-local `Tenants` and lookup-backed `Users` tabs plus `scope`, `userId`, `search`, `status`, `sort`, `desc`, and `cursor`; invalid values normalize fail-safe, any tab/scope/filter/sort change resets cursor, and compatibility routes remain renderable without becoming generated navigation targets.
- Compose Razor UI with FrontComposer and Fluent UI Blazor V5 before custom markup; do not introduce raw interactive controls, duplicate page chrome, theme redefinition, generic grids/tabs/layout, or reusable command chrome inside Tenants. Tenants may own domain-specific safety, freshness, audit, and command composition; reusable infrastructure belongs in FrontComposer or an explicitly approved fallback.
- Use Blazor `InteractiveServer` with a server-side BFF. Components and browsers never call Tenants, EventStore, or Memories directly and never receive backend tokens; `ITenantQueryGateway`, `ITenantCommandGateway`, and server-side collaborators are the only backend egress.
- Route all six reads directly to Tenants REST: tenant list, tenant detail, tenant users, user tenants, tenant audit, and global administrators. Commands and status lookup remain on the EventStore command client. The generic EventStore query route and retired projection actors are not Tenants UI read paths.
- Preserve the fixed command contract `POST /api/v1/commands` and command-status lookup; add no unversioned alias and no new backend preview, receipt, correction, list-filter, or command-status endpoints.
- Bind to existing `Hexalith.Tenants.Client`/`.Contracts` queries, DTOs, commands, events, and enums; do not redefine DTOs, re-case wire fields, reshape immutable contracts, or treat caller-supplied TenantId/UserId values as GUIDs or ULIDs. Envelope `messageId` remains a ULID idempotency key.
- Implement command confirmation as one shared pattern: submit, run status polling and SignalR nudge handling, then authoritatively re-query the projection. `confirmed` requires the expected postcondition plus projection-version advancement or safe command-specific audit evidence beyond the pre-submit baseline; pre-existing expected state/NoOp is `already applied`, unavailable provenance is `unable to verify`, and unrelated projection data or live signals never confirm.
- Lock commands by `(interactive circuit, AggregateIdentity)` from submit through terminal evidence. Commands for the same aggregate cannot overlap; unrelated aggregates may proceed. Bulk submission, toast batching, multi-row command actions, and multiple simultaneous commands for one aggregate remain prohibited.
- Use shared typed immutable truth, freshness, lifecycle, authorization, and audit snapshots with a casing-faithful canonical vocabulary. Fluxor is not mandatory. Last-confirmed projection state stays separate from in-flight intent, and reconnects rederive truth rather than promoting pending state.
- Use EventStore `ReadModelFreshnessState` and authoritative read metadata. `Refreshing` is client-transient; wire claims are limited to supported `current`/`stale`/`unknown`, `aging` is not claimed until projection-time provenance supports it, and unknown/stale data fails closed where required. Thresholds are configurable and conservative, never magic numbers.
- Treat all cursors as protected, opaque, scope-bound, and support-safe. On scope mismatch, decoding failure, or invalidation, restart from page 1 with an honest localized notice; do not expose cursors in DOM attributes, visible copy, logs, telemetry tags, or copy actions.
- Treat Memories as search-index-only. Search returns ordered tenant-id candidates from `tenants-index`; the BFF deduplicates and authorization-filters them, hydrates row truth and freshness through direct Tenants reads, applies deterministic visible sorting, advances by raw hits consumed (including dropped hits), reports partial hydration as degraded, and falls back to the cursor list on Memories failure.
- Server-side support-safety services assemble and redact consequence previews, Audit Evidence Receipts, and rejection view models before render. Receipt target resolution is `userId` then `key` then `TenantId`; `NarrativePayload` is structured source metadata and never reaches component state.
- Keep tenant-domain and global-administrator command/correction paths distinct. Global-administrator operations always use the fixed `global-administrators` aggregate/domain; the last-administrator invariant is a hard stop, while last-owner removal is permitted with elevated friction.
- Use Tenants-owned `.resx` resources for domain copy and inherit only shell chrome from FrontComposer resources. Keep EN/FR key parity, dotted PascalCase concept keys, whole strings, and named placeholders.
- Use stable `data-testid="tenants-{surface}-{element}"` contracts and focused bUnit/conformance coverage for every surface; tests must not depend on row text, color, or incidental generated markup.
- Maintain distinct projects and boundaries: `src/Hexalith.Tenants.UI` is a domain-owned publishable app/container; UI tests belong in `tests/Hexalith.Tenants.UI.Tests`; reusable platform UI/hosting/test infrastructure belongs in the relevant technical module rather than Tenants.
- Ship the UI as a non-root .NET SDK container (`ContainerRepository=tenants-ui`) with no Dockerfile and no NuGet packaging for the app host.
- Distributed orchestration belongs to a platform/composing host. The repository `Hexalith.Tenants.AppHost` is transitional migration debt and must not gain shared hosting, ServiceDefaults, health, telemetry, configuration, or secrets infrastructure.
- Externalize configuration and secrets; consume shared ServiceDefaults, health endpoints, OpenTelemetry, and non-root container defaults. Keep InteractiveServer at one replica until shared DataProtection, circuit/session routing, and cursor durability are verified.
- Implement and verify prerequisite work packages without treating them as end-user stories: `PLAT-FRESH-1` (REST freshness provenance), `HOST-REF-1` (separate query/command service references), `UI-READ-1` (six direct reads), `SEARCH-CURSOR-1` (protected scoped search cursor), `WP-2A` (minimum removal proof), and `PLATFORM-OPS-1` (platform-owned topology/operations). Shared-platform packages require separately authorized work in their owning repositories.
- FrontComposer posture is hybrid: `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` have historical confirmation requiring story-specific reverification; the Tenants-specific `TenantDataGrid` resolves the local `FC-TBL` boundary; approved fallbacks are flat `AuditDataGrid` for `FC-AUD` and inline full-content Consequence Preview for `FC-CNS`; `FC-TOK` remains a missing shared contract covered only by verified Fluent semantic/icon mapping.
- Verification uses the pinned centrally consumed Fluent UI Blazor package; exact component, icon, size, token, and ARIA behavior must be checked at build time rather than assumed from earlier release candidates.
- Build with `Hexalith.Tenants.slnx`, central package versions, warnings as errors, and the repository's serialized build posture. Run focused test projects individually with xUnit v3, Shouldly, NSubstitute/bUnit as applicable, plus Playwright for end-to-end responsive/accessibility behavior.
- Architecture remediation priority is: propagate REST freshness metadata; move topology ownership and expose split service references; route the six UI reads directly; replace the plaintext Memories offset with a protected scoped cursor; add shared health/telemetry/ServiceDefaults and retain single-replica operation until scaling prerequisites are proven.
- Hard destructive tenant deletion, email invitations, dedicated owner-only screens, exhaustive all-users inventory without a scoped backend query, mobile high-impact commands, sensitive configuration display, grouped/session audit, anomaly scoring, advanced analytics, and bulk provisioning remain out of scope.

### UX Design Requirements

UX-DR1: Use Microsoft Fluent UI Blazor V5 through the FrontComposer shell as the visual authority; do not invent a bespoke palette, hard-code or restate hex colors, recreate Fluent theme primitives, or assert unverified token names.

UX-DR2: Map status meaning only to verified Fluent semantic roles `Success`, `Informative`, `Warning`, `Severe`, `Danger`, `Important`, and `Subtle`; use `Brand` only for chrome and eligible primary actions, never for status.

UX-DR3: Reserve Success exclusively for projection-proven truth (`current`, `confirmed`, `audit available`, tenant active); `refreshing`, `previewed`, `submitted`, `accepted`, `projection pending`, and `audit pending` remain Informative.

UX-DR4: Preserve the three-tier caution ramp: Warning for usable-with-friction states, Severe for blocking non-error states, and Danger for rejection, failure, or the genuine destructive/high-impact moment.

UX-DR5: Render every status with semantic role plus a cross-role-distinct icon and visible/localized text/accessibility label; no meaning may depend on color or filled appearance, including in forced-colors mode.

UX-DR6: Implement the reusable `TruthStateBadge` from the DESIGN.md state-to-role/icon table, pin verified status glyphs at Size20, reverify every glyph against the pinned package, preserve each canonical state, and reserve stable layout space.

UX-DR7: Inherit Fluent typography and shapes; use modest headings and compact body/label/caption roles, monospace for literal identifiers/support-safe references/absolute timestamps, and no hero typography or bespoke radii.

UX-DR8: Use full-width operational surfaces with constrained readable inner regions for forms, previews, panels, and dialogs; follow the 4/8/12/16/24/32px rhythm through Fluent/FrontComposer layout primitives and compact density.

UX-DR9: Reserve stable grid-cell, action, status, and reason footprints so state changes, sorting, paging, and longer localized labels do not shift row layout or action placement.

UX-DR10: Pin identity, status, freshness, role, and risk where shown using verified Fluent grid pinning; horizontal scroll and column priority must never remove safety-critical context, and pinned state must remain accessible without relying on color.

UX-DR11: Implement and consistently reuse the ten domain components: `TruthStateBadge`, `ConsequencePreview`, `CommandLifecyclePanel`, `UnavailableActionReason`, `AuditEvidenceReceipt`, `TenantDataGrid`, `MemberTable`, `AuditDataGrid`, `PrimaryCommandButton`, and `DestructiveControl`.

UX-DR12: `ConsequencePreview` uses the approved inline structured-text fallback until a shared `FC-CNS` component exists, includes all ten labeled items, separates known consequences from known unknowns, is BFF-assembled/redacted, and blocks submission while any item is unavailable.

UX-DR13: `CommandLifecyclePanel` remains inline and anchored to the affected row/panel, shows lifecycle steps distinctly, keeps confirmed projection data separate from intent, and advances to confirmation only after authoritative re-query—not from SignalR or status alone.

UX-DR14: `UnavailableActionReason` renders one of the six canonical categories as visible localized text in the expected action slot, is programmatically associated with the action/row, is keyboard/screen-reader reachable, and is never tooltip-only.

UX-DR15: `AuditEvidenceReceipt` renders only BFF-redacted actor, target, tenant scope, outcome, absolute timestamp, projection marker, and support-safe reference; pending/delayed/unavailable/not-built evidence displays its real state and never fabricated proof.

UX-DR16: `TenantDataGrid` uses cursor pagination and required tenant columns, preserves six non-collapsible list states and pending/stale visibility, performs Memories search as candidate selection followed by authoritative hydration, uses the protected scoped search cursor with honest page-1 recovery, and shows a non-blocking cursor-list fallback when search is unavailable.

UX-DR17: `MemberTable` stays read-only and non-editable in appearance, exposes complete table semantics, displays identity/role/owner count/status/freshness/orphan context, pins safety columns, and reserves a per-row action-or-reason slot.

UX-DR18: `AuditDataGrid` is the approved flat, stably ordered, cursor-paginated fallback with date and `AuditEventCategory` filters and distinct loading/empty/filtered-empty/error states; filtered-empty offers reset and unavailable MVP content uses an honest not-yet-available placeholder.

UX-DR19: `DestructiveControl` never appears as a casual or primary action; destructive/high-impact flows require eligible gates, complete preview, focus-trapped confirmation, safe non-committing escape, and elevated friction for zero-owner or target-also-global-administrator cases, while last-global-administrator removal remains unavailable.

UX-DR20: The Operations Shell exposes one Tenants left-menu entry. The workspace uses page-local `Tenants` and lookup-backed `Users` tabs; Global Administrators and Audit are module-internal/contextual routes, compatibility aliases do not become generated navigation, and command lifecycle is never navigation.

UX-DR21: Preserve canonical workspace tab/scope/search/filter/sort/selection/cursor context and scroll across navigation, especially tenant-list to detail and back; changing tab/scope/filter/sort resets the cursor safely.

UX-DR22: Microcopy is calm, precise, honest, and uses the same plain register for operators and owners. `undo`, `rollback`, and `hidden edit` are prohibited in labels, tooltips, announcements, and help copy.

UX-DR23: Use every canonical truth, freshness, lifecycle, feedback, list, audit, unavailable-reason, and recovery vocabulary verbatim and casing-significantly, including distinct spaced badge forms (`audit pending`) and underscored state-machine forms (`audit_pending`).

UX-DR24: Derive risk rather than persist it: high when owner count would reach zero or the target is also a global administrator, low otherwise; show it only in member/action and preview context, not as a tenant-grid column.

UX-DR25: Make every workflow keyboard-operable and completable or safely exitable; modals/previews trap focus, Escape/cancel never commits, focus order matches reading order, and focus returns to the launcher after close, cancel, submit, or failure.

UX-DR26: Enforce the load-bearing gating order before preview: validation, freshness, and authorization must all be eligible; stale/unknown freshness, indeterminate authorization, missing lifecycle support, or incomplete preview blocks the action with an inline reason.

UX-DR27: Enforce aggregate-scoped command locking from submit through terminal evidence: same-aggregate triggers remain unavailable with a reason, unrelated aggregates may proceed, reconnect/failure cannot leak or prematurely release locks, and bulk submission, toast batching, multi-row actions, and optimistic success remain prohibited.

UX-DR28: Drive live-region politeness from a dedicated announcement-intent field rather than color/MessageBar intent; assertive is reserved for rejection, failure, `unable to verify`, `degraded`, and destructive blockers, while resting risk/destructive controls are not assertive and success is never announced before projection truth.

UX-DR29: Provide accessible names and culture-aware absolute timestamps for freshness and audit evidence; relative-only timestamps are insufficient.

UX-DR30: Follow the responsive contract: mobile 320–767px is read-only triage/lookup/audit reference; tablet 768–1023px collapses navigation and stacks regions while retaining horizontally scrollable tables; desktop 1024px+ is the dense workstation; wide desktop 1440px+ adds room without changing safety semantics.

UX-DR31: If a viewport cannot preserve the full safety context and ten-item preview for a high-impact action, make that action unavailable with a visible localized reason instead of rendering an unsafe reduced flow.

UX-DR32: Keep layouts RTL-ready through logical start/end behavior and no hard-coded left/right assumptions, while treating RTL verification/shipping as deferred until explicitly promoted.

UX-DR33: Ready-gate evidence covers keyboard-only complete-or-exit, focus trap/escape/return, NVDA plus another browser/screen-reader pairing, forced-colors and reduced motion, contrast, live-region politeness, horizontal overflow/navigation collapse/dialog behavior, stale projection, rejected command, unknown confirmation, accepted-but-projection-pending, data unavailable versus authorization denied, aggregate-lock retention, audit unavailable, last-owner warning, permission missing, reason honesty, support safety, EN/FR parity, stable selectors, and no success before projection truth.

### FR Coverage Map

FR1: Epic 1 - Browse, search, filter, sort, and cursor-page an authorization-safe tenant list with authoritative hydration and honest degraded search behavior.

FR2: Epic 1 - Open deep-linkable tenant details and return with canonical workspace context preserved.

FR3: Epic 1 - Self-audit authorized tenant memberships and roles through My Tenants.

FR4: Epic 1 - Look up a user's authorization-scoped tenant memberships with a safe empty state.

FR5: Epic 1 - View tenant overview, counts, lifecycle state, summaries, and freshness.

FR6: Epic 1 - View authorization-filtered namespaced tenant configuration safely.

FR7: Epic 1 - Copy literal identifiers and support-safe references without exposing unsafe data.

FR8: Epic 1 - Review the accessible, read-only tenant member table and safety context.

FR9: Epic 1 - See honest action availability and inline canonical unavailable reasons.

FR10: Epic 2 - Add a user directly to a tenant with an explicit role and safe rejection handling.

FR11: Epic 2 - Change a tenant member's role with NoOp/rejection honesty and projection confirmation.

FR12: Epic 2 - Remove tenant access through fail-closed preview, elevated friction, lifecycle tracking, and minimum audit proof.

FR13: Epic 3 - Create a tenant with safe duplicate handling and projection confirmation.

FR14: Epic 3 - Edit tenant metadata with validation and always-emitted, projection-confirmed updates.

FR15: Epic 3 - Disable or enable a tenant through an honest high-impact lifecycle flow.

FR16: Epic 3 - Set namespaced configuration through mandatory preview and honest NoOp/rejection handling.

FR17: Epic 3 - Remove namespaced configuration through mandatory preview and projection confirmation.

FR18: Epic 4 - Review fixed-scope global administrators with authorization and freshness.

FR19: Epic 4 - Grant or remove global administrators while enforcing the last-administrator hard stop.

FR20: Epic 5 - Browse a filtered, cursor-paginated, accessible audit DataGrid under the approved performance contract.

FR21: Epic 5 - Reach correctly scoped audit evidence from contextual entry points.

FR22: Epic 5 - View BFF-assembled, redacted, support-safe Audit Evidence Receipts.

FR23: Epic 5 - Distinguish audit availability states and their recovery paths without false success.

FR24: Epic 5 - Start a forward compensating command from audit evidence.

FR25: Epic 5 - Preview corrections against current state and link original and corrective evidence.

## Epic List

### Epic 1: Trustworthy Tenant Discovery and Access Review

Operators, owners, and members can find authorized tenants, understand freshness, inspect tenant details and configuration, review memberships, and see honest action availability.

**FRs covered:** FR1, FR2, FR3, FR4, FR5, FR6, FR7, FR8, FR9

### Epic 2: Safe Tenant Membership Management

Authorized users can add members, change roles, and remove access through projection-confirmed, fail-closed flows with last-owner safeguards and minimum removal proof.

**FRs covered:** FR10, FR11, FR12

### Epic 3: Tenant Onboarding, Lifecycle, and Configuration

Authorized users can create and configure tenants, edit metadata, and safely enable or disable tenant operation.

**FRs covered:** FR13, FR14, FR15, FR16, FR17

### Epic 4: Global Administrator Governance

Authorized operators can review, grant, and remove platform-wide administrators while preserving the fixed aggregate scope and last-administrator invariant.

**FRs covered:** FR18, FR19

### Epic 5: Audit Evidence and Corrective Recovery

Users can inspect contextual audit evidence, understand proof availability, and correct mistakes forward through linked compensating commands.

**FRs covered:** FR20, FR21, FR22, FR23, FR24, FR25

<!-- Repeat for each epic in epics_list (N = 1, 2, 3...) -->

## Epic {{N}}: {{epic_title_N}}

{{epic_goal_N}}

<!-- Repeat for each story (M = 1, 2, 3...) within epic N -->

### Story {{N}}.{{M}}: {{story_title_N_M}}

As a {{user_type}},
I want {{capability}},
So that {{value_benefit}}.

**Acceptance Criteria:**

<!-- for each AC on this story -->

**Given** {{precondition}}
**When** {{action}}
**Then** {{expected_outcome}}
**And** {{additional_criteria}}

<!-- End story repeat -->
