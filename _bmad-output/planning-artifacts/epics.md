---
stepsCompleted: [1, 2, 3, 4]
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

FR13: An authorized operator (domain-enforced as global-administrator-only) can create a tenant; an existing tenant id is rejected as `TenantAlreadyExists`, and creation is shown as successful only after authoritative projection confirmation.

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

FR13: Epic 3 - Create a tenant (global-administrator-only) with safe duplicate handling and projection confirmation.

FR14: Epic 3 - Edit tenant metadata with validation and always-emitted, projection-confirmed updates.

FR15: Epic 3 - Disable or enable a tenant through an honest high-impact lifecycle flow.

FR16: Epic 3 - Set namespaced configuration through mandatory preview and honest NoOp/rejection handling.

FR17: Epic 3 - Remove namespaced configuration through mandatory preview and projection confirmation.

FR18: Epic 1 - Review fixed-scope global administrators with authorization and freshness as part of the complete read-only access picture.

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

**FRs covered:** FR1, FR2, FR3, FR4, FR5, FR6, FR7, FR8, FR9, FR18

**Completion boundary:** Delivers the complete Phase 2a read-only product, including authorized global-administrator review, even when search is degraded. Existing implementation is reverified rather than blindly rebuilt, and platform-owned freshness, split-reference, direct-read, protected-search-cursor, and operations work packages remain explicit external gates tied to the affected user outcomes.

### Epic 2: Safe Tenant Membership Management

Authorized users can add members, change roles, and remove access through projection-confirmed, fail-closed flows with last-owner safeguards and minimum removal proof.

**FRs covered:** FR10, FR11, FR12

**Completion boundary:** Delivers tenant membership management end to end, including minimum removal proof (`WP-2A`). It reuses the shared command lifecycle, confirmation, and aggregate-lock posture and does not depend on Epic 5 to complete FR12.

### Epic 3: Tenant Onboarding, Lifecycle, and Configuration

Authorized users can create and configure tenants, edit metadata, and safely enable or disable tenant operation.

**FRs covered:** FR13, FR14, FR15, FR16, FR17

**Completion boundary:** Delivers complete tenant onboarding and ongoing metadata, lifecycle, and configuration control through the shared command posture. Hard tenant deletion and broader audit generalization are not required for this outcome.

### Epic 4: Global Administrator Control

Authorized operators can grant and remove platform-wide administrators while preserving the fixed aggregate scope and last-administrator invariant.

**FRs covered:** FR19

**Completion boundary:** Builds on Epic 1's authorized read surface and delivers fixed-scope global-administrator mutation independently of tenant membership, including projection confirmation and the last-administrator hard stop.

### Epic 5: Audit Evidence and Corrective Recovery

Users can inspect contextual audit evidence, understand proof availability, and correct mistakes forward through linked compensating commands.

**FRs covered:** FR20, FR21, FR22, FR23, FR24, FR25

**Completion boundary:** Generalizes evidence browsing, availability, receipts, and forward correction across the completed command domains. Readiness is gated by the Product/Operations audit-performance decision, while approved fallback and historical implementation evidence still require story-specific reverification.

## Epic 1: Trustworthy Tenant Discovery and Access Review

Operators, owners, and members can find authorized tenants, understand freshness, inspect tenant details and configuration, review memberships, and see honest action availability.

### Story 1.0: Reverify FrontComposer Shell and Fluent Contracts

As a Tenants UI maintainer,
I want the shared FrontComposer and Fluent contracts reverified against the corrected architecture and pinned dependencies,
So that subsequent stories build on demonstrated capabilities and honest fallback boundaries.

**Acceptance Criteria:**

**Given** the current FrontComposer source/package baseline and the existing Story 1.0 evidence
**When** the shell integration contract is reverified
**Then** the exact supported registration, single-module navigation, full-width operational layout, and constrained-inner-region APIs are documented with their source and version
**And** any divergence from AD-1 through AD-4 is recorded as a blocker rather than hidden by Tenants-owned replacement infrastructure.

**Given** the shared command-feedback and concurrency capabilities
**When** their corrected behavior is reverified
**Then** the evidence demonstrates distinct submitted, accepted, projection-pending, confirmed, audit-pending, and audit-available states
**And** confirms that SignalR is only a re-query nudge and locking is scoped by `(interactive circuit, AggregateIdentity)` while unrelated aggregates may proceed.

**Given** the FrontComposer accessibility, localization, and documentation contracts
**When** their available APIs and reference material are inspected
**Then** `FC-A11Y`, `FC-L10N`, and `FC-DOC` are classified as verified, changed, or blocked with reproducible evidence
**And** story-specific keyboard, focus, live-region, localization, responsive, and documentation evidence remains mandatory rather than being waived by this verification.

**Given** the generated FrontComposer grid capability
**When** it is compared with Tenants' cursor pagination, safety-column pinning, stable action slots, and six-state requirements
**Then** the Tenants-specific `TenantDataGrid` boundary remains explicit
**And** reusable generic grid capability is not reimplemented inside Tenants.

**Given** `FC-AUD`, `FC-CNS`, and `FC-TOK` readiness
**When** the fallback posture is reviewed
**Then** flat `AuditDataGrid` and inline full-content Consequence Preview remain the only approved local fallbacks
**And** missing shared token capability is handled through verified Fluent semantic/icon mappings without inventing token names.

**Given** the centrally pinned Fluent UI Blazor version
**When** badge colors, Size20 status icons, grid pinning, MessageBar behavior, focus behavior, and ARIA parameters are checked against that exact version
**Then** every relied-upon name and behavior is recorded as verified or blocked
**And** no assumption from an earlier release candidate is presented as current evidence.

**Given** the repository's dependency boundaries
**When** this reverification is performed
**Then** no root-declared submodule source is modified without a separately authorized task
**And** any shared-platform gap is assigned to its owning module rather than implemented as Tenants boilerplate.

**Given** the completed reverification
**When** the focused contract and conformance checks are run
**Then** the exact commands and results are recorded
**And** any blocked check identifies its command, blocker, affected downstream stories, and approved conservative behavior.

### Story 1.1: Reverify UI Host Bootstrap and Canonical Workspace

As an authenticated Tenants user,
I want a single, stable Tenants workspace inside the platform shell,
So that I can reach authorized tenant-management capabilities through a consistent and support-safe entry point.

**Acceptance Criteria:**

**Given** the existing `src/Hexalith.Tenants.UI` project
**When** the application is built and started
**Then** it runs as a .NET 10 Blazor `InteractiveServer` application composed through FrontComposer and Fluent UI Blazor V5
**And** it remains registered in `Hexalith.Tenants.slnx` as an application/container rather than a NuGet package.

**Given** an authenticated user opens the platform shell
**When** the domain navigation is rendered
**Then** exactly one Tenants module entry targets `/tenants`
**And** All Tenants, My Tenants, Users, Global Administrators, Audit, and command lifecycle do not register additional Tenants shell entries.

**Given** the user opens `/tenants`
**When** workspace navigation is displayed
**Then** page-local `Tenants` and lookup-backed `Users` tabs are available through FrontComposer/Fluent components
**And** the Users tab is not represented as an exhaustive all-users inventory.

**Given** canonical workspace parameters `tab`, `scope`, `userId`, `search`, `status`, `sort`, `desc`, and `cursor`
**When** valid or invalid combinations are loaded
**Then** valid state is represented consistently, invalid state normalizes fail-safe, and tab/scope/filter/sort changes reset the cursor
**And** `/tenants/my` and `/tenants/users` remain renderable compatibility routes while generated navigation uses canonical `/tenants` state.

**Given** the InteractiveServer trust boundary
**When** a component needs backend data or command behavior
**Then** it can depend only on injected server-side BFF gateway/composition contracts
**And** no browser-side component holds a backend bearer token or directly calls Tenants, EventStore, or Memories.

**Given** shell chrome and Tenants domain copy
**When** the workspace renders in English or French
**Then** shell-owned text comes from FrontComposer resources and Tenants-owned text comes from parity-checked whole-string `.resx` resources
**And** no sentence is assembled from localized fragments.

**Given** desktop, tablet, and mobile viewport widths
**When** the workspace shell is rendered
**Then** navigation and layout use FrontComposer/Fluent primitives, tablet navigation can collapse, and mobile remains a safe read-oriented shell
**And** no raw interactive HTML controls, duplicate page chrome, route-level `PageTitle`, page-root `<main>`, theme redefinition, or unsupported layout CSS is introduced.

**Given** workspace layout, spacing, alignment, overflow, or directional controls are authored
**When** the shell is rendered in any supported left-to-right culture
**Then** FrontComposer/Fluent primitives and logical start/end behavior are used without hard-coded left/right assumptions that would prevent future bidirectional layout
**And** the story makes no claim that RTL verification or shipping is complete while that work remains explicitly deferred.

**Given** the UI deployment boundary
**When** publish configuration is inspected
**Then** the app uses .NET SDK container support with `ContainerRepository=tenants-ui`, shared non-root defaults, externalized configuration, and no Dockerfile
**And** the transitional repository AppHost is not expanded with shared orchestration, ServiceDefaults, health, telemetry, configuration, or secrets infrastructure.

**Given** the bootstrap and workspace composition
**When** focused route, shell, localization-parity, support-safety, Fluent-conformance, and responsive checks run
**Then** stable `data-testid="tenants-{surface}-{element}"` contracts are available where interaction begins
**And** exact commands/results and any external `PLATFORM-OPS-1` blockers are recorded without weakening the story's local checks.

### Story 1.2: Tenant List Triage and Cursor Foundation

As a platform operator,
I want to scan, filter, sort, and cursor-page authorized tenants with visible trust context,
So that I can identify the tenant requiring attention without acting on hidden or misleading state.

**Acceptance Criteria:**

**Given** an authorized operator opens the Tenants tab
**When** tenant summaries are available
**Then** a full-width Fluent `TenantDataGrid` shows tenant identity, status, member count, owner count, pending state, and a freshness-aware `TruthStateBadge`
**And** identity, status, and freshness remain pinned safety columns with stable cell and action footprints.

**Given** more tenant rows exist than fit on one page
**When** the operator moves forward or backward through results
**Then** paging uses the server-side opaque cursor contract rather than offset/limit
**And** no cursor value appears in visible copy, DOM attributes, logs, telemetry tags, or copy actions.

**Given** the operator changes status filter, sort field, sort direction, scope, or page size
**When** the new list request is issued
**Then** the cursor resets and deterministic results are returned from the first page
**And** invalid cursor state restarts at page 1 with an honest localized list-refreshed notice rather than a generic failure.

**Given** the list is loading, empty, filtered-empty, in error, stale, or degraded
**When** that state is rendered
**Then** all six states remain visually, semantically, and programmatically distinct
**And** filtered-empty offers reset, stale offers refresh, degraded explains what is unavailable and what still works, and empty/error copy reveals no out-of-scope tenant existence.

**Given** a tenant has a pending or stale marker
**When** the operator sorts, filters, pages, or horizontally scrolls the grid
**Then** the marker remains associated with the correct row and cannot be hidden by layout or state collapse
**And** a pending state is never styled, copied, or announced as confirmed success.

**Given** authoritative projection freshness cannot yet be measured for a row or response
**When** the list renders trust state
**Then** freshness is `unknown` rather than inferred from `ServedAt` or request recency
**And** the UI does not claim `current` or `aging` without supported read-model provenance.

**Given** a non-empty search term is entered while protected whole-set search is unavailable or degraded
**When** the list handles the request
**Then** the normal authorization-safe cursor list remains usable with a non-blocking localized search-unavailable notice
**And** no in-memory filter over the loaded page is misrepresented as whole-set search.

**Given** desktop, tablet, and mobile viewport widths
**When** the grid is rendered and navigated by keyboard or assistive technology
**Then** headers, sort state, row relationships, pinned-column meaning, visible focus, and horizontal overflow remain accessible
**And** safety-critical columns are never removed merely to fit a narrow viewport.

**Given** a typical tenant set on a warm projection
**When** the list interaction performance is measured in the documented reference environment
**Then** the surface targets interactive rendering within approximately one second
**And** any unmet target is recorded with reproducible measurements rather than hidden by reducing safety or freshness behavior.

**Given** the tenant-list implementation
**When** focused bUnit, gateway, localization-parity, authorization-safety, responsive, forced-colors, and conformance tests run
**Then** stable `data-testid` selectors validate each state without depending on row text, color, or incidental Fluent markup
**And** the exact test commands and results are recorded.

### Story 1.3: Tenant Detail Navigation and Overview

As an authorized Tenants user,
I want to open a tenant's trustworthy overview and return to my prior workspace context,
So that I can investigate a tenant without losing the triage state that brought me there.

**Acceptance Criteria:**

**Given** an authorized tenant row in the list
**When** the user opens the tenant
**Then** navigation targets the deep-linkable `/tenants/{tenantId}` contextual route using the literal safely encoded caller-supplied identifier
**And** the generated link carries a support-safe return context for the canonical workspace state.

**Given** an authorized user opens a valid tenant deep link directly
**When** the detail read completes
**Then** the overview shows tenant identity, lifecycle status, metadata, member and configuration summaries, member count, owner count, and authoritative or honestly unknown freshness
**And** status and freshness use canonical localized text plus verified icon/semantic-role mappings rather than color alone.

**Given** the user arrived from a tenant list with tab, scope, search, status, sort, direction, cursor context, selection, and scroll position
**When** the user returns from detail
**Then** the prior workspace state and selected row are restored without manufacturing an invalid cursor
**And** unsafe, malformed, or stale return state normalizes to a safe first-page workspace with an honest localized notice.

**Given** configuration or member detail content is not yet available
**When** the overview renders its summaries
**Then** the tenant overview remains independently useful and does not present empty placeholders as errors or fabricated complete data
**And** unavailable detail regions use honest localized read-only states without requiring a future story for the overview to function.

**Given** the tenant does not exist, is outside the caller's authorization scope, or the read fails
**When** the detail route handles the result
**Then** not-found, authorization-safe absence, and data-unavailable behavior do not disclose out-of-scope tenant existence
**And** raw Problem Details, tokens, internal correlations, ETags, cursors, payloads, and stack traces never enter rendered or announced output.

**Given** projection freshness is stale or unknown
**When** the overview is displayed
**Then** last-confirmed data remains visibly distinct from refresh state and no optimistic current claim is made
**And** refresh is available as the named recovery while SignalR can only request a re-query.

**Given** the detail surface is rendered at desktop, tablet, and mobile widths
**When** the user navigates it with keyboard, screen reader, high contrast, forced colors, or reduced motion
**Then** FrontComposer detail/page primitives and Fluent components preserve logical focus, accessible headings, status names, absolute timestamps, and safe responsive stacking
**And** no raw interactive controls, duplicate page chrome, route-level `PageTitle`, page-root `<main>`, or safety-critical hidden context is introduced.

**Given** English and French resources
**When** overview, error, empty, stale, unknown, and return-context notices render
**Then** whole-string resources remain parity-checked and culture-aware
**And** stable `data-testid` selectors identify navigation, overview, freshness, recovery, and error states without depending on localized text.

**Given** the completed tenant-detail slice
**When** focused route, gateway, authorization, bUnit, localization, responsive, accessibility, support-safety, and conformance checks run
**Then** deep-link and return-context scenarios pass, including malformed and invalidated state
**And** exact commands, results, and any authoritative-freshness external blockers are recorded.

### Story 1.4: My Tenants Self-Audit

As a signed-in user,
I want to see the tenants I belong to and my role in each,
So that I can verify my own access without asking a platform operator.

**Acceptance Criteria:**

**Given** an authenticated user selects the My Tenants scope
**When** the self-audit request is composed
**Then** the server-side BFF derives the user identity from the authenticated principal rather than a browser-controlled identity value
**And** the canonical workspace uses `tab=tenants&scope=mine` without registering a separate shell entry or owner-only application.

**Given** the caller has authorized tenant memberships
**When** the membership results render
**Then** each row shows the literal tenant identity, the caller's role, tenant status, and authoritative or honestly unknown freshness
**And** no membership or tenant outside the caller's authorized result set is rendered, copied, announced, or inferable from counts.

**Given** the caller has no visible memberships
**When** the self-audit view completes successfully
**Then** an explicit authorization-safe empty state is shown rather than an error
**And** the copy does not imply whether hidden tenants or memberships exist.

**Given** the membership read is loading, fails, is stale, or is degraded
**When** the corresponding state is displayed
**Then** it remains distinct from empty and from every other state with a named recovery where applicable
**And** unavailable provenance is shown as `unknown`, never inferred as current from request time.

**Given** a tenant owner uses the shared workspace
**When** the owner reviews My Tenants and opens an authorized tenant
**Then** the same tenant-list and detail components are used with server-enforced scope
**And** returning from detail restores the My Tenants scope, filter, selection, cursor context, and scroll position.

**Given** a tenant owner or ordinary member is not a global administrator
**When** the workspace is rendered
**Then** the self-audit capability does not expose a Global Administrators entry, hidden administrator data, or operator-only actions
**And** UI absence is treated only as reflection of server authorization, never as the enforcement boundary.

**Given** desktop, tablet, and mobile viewport widths
**When** the self-audit rows are navigated with keyboard or assistive technology
**Then** role, tenant status, freshness, headings, row relationships, focus, and horizontal overflow remain accessible
**And** status meaning remains text-and-icon complete in forced-colors and reduced-motion environments.

**Given** English and French cultures
**When** roles, statuses, freshness, loading, empty, error, stale, degraded, and recovery text render
**Then** whole-string resource parity and culture-aware formatting are preserved
**And** stable selectors do not depend on localized row text or color.

**Given** the My Tenants slice
**When** focused identity, authorization, gateway, bUnit, localization, responsive, accessibility, support-safety, and route tests run
**Then** tests cover memberships, no memberships, attempted identity substitution, out-of-scope data, invalid cursor recovery, and deep-link return
**And** exact commands, results, and external freshness blockers are recorded.

### Story 1.5: User Membership Lookup

As an authorized platform operator,
I want to look up a user and review that user's visible tenant memberships,
So that I can answer access questions without treating the UI as an exhaustive user directory.

**Acceptance Criteria:**

**Given** an authorized operator selects the Users workspace tab
**When** the surface renders
**Then** it presents a clearly labeled lookup/search-backed membership experience using Fluent/FrontComposer controls
**And** neither copy nor behavior implies that all platform users can be browsed or enumerated.

**Given** the operator enters a non-whitespace caller-supplied user identifier
**When** the lookup is submitted
**Then** the literal identifier is safely encoded and sent only through the server-side BFF user-tenants read path
**And** it is not parsed or validated as a GUID, ULID, email invitation, or other invented identity format.

**Given** authorized memberships are returned for the looked-up user
**When** results render
**Then** each visible row shows tenant identity, the user's role, tenant status, and authoritative or honestly unknown freshness
**And** results remain authorization-scoped even when the target user belongs to additional hidden tenants.

**Given** the target user has no memberships visible to the caller
**When** the lookup completes successfully
**Then** an explicit authorization-safe empty state is rendered instead of an error
**And** the response does not reveal whether the user, hidden memberships, or hidden tenants exist.

**Given** the lookup input is empty, whitespace, malformed for the transport, excessively long, or contains Unicode/reserved URL characters
**When** validation or safe encoding runs
**Then** invalid input receives localized field guidance without issuing an unsafe request, while valid meaningful strings remain supported
**And** raw exception, route, payload, token, correlation, or stack-trace details are never shown.

**Given** the read is loading, unavailable, stale, degraded, or has unknown freshness
**When** the state is rendered
**Then** it remains distinct from authorization-safe empty and offers the appropriate refresh/retry/continue-read-only recovery
**And** no unavailable state is mislabeled as authorization denied or current data.

**Given** a caller lacks operator authorization
**When** the Users lookup surface or endpoint is requested
**Then** the UI reflects the unavailable capability without exposing lookup results or target-user existence
**And** the server remains the enforcement boundary.

**Given** keyboard, screen-reader, tablet, mobile, forced-colors, and reduced-motion use
**When** the lookup and results are operated
**Then** labels, validation, focus order, submit behavior, result announcements, table semantics, and safe escape/return are accessible
**And** stable selectors identify input, submit, states, rows, and navigation without depending on localized text or color.

**Given** the completed user-membership lookup
**When** focused validation, authorization, route, gateway, encoding, bUnit, localization, responsive, accessibility, and support-safety tests run
**Then** direct entry, hidden membership, empty, error, Unicode identifier, and return-context scenarios pass
**And** exact commands, results, and external freshness blockers are recorded.

### Story 1.6: Read-Only Tenant Configuration

As an authorized tenant user,
I want to inspect the tenant configuration namespaces I am allowed to see,
So that I can understand operational configuration without exposing other consumers' or sensitive values.

**Acceptance Criteria:**

**Given** an authorized user opens a tenant detail surface
**When** the configuration region loads
**Then** visible key/value pairs are grouped by consumer-owned namespace using Fluent/FrontComposer read-only composition
**And** the region is clearly presented as inspection rather than an editable form.

**Given** configuration contains keys inside and outside the caller's authorized namespace prefixes
**When** the server-side BFF composes the view model
**Then** only authorized namespace entries reach component state and rendered output
**And** hidden namespace names, key counts, key names, values, and existence cannot be inferred from empty or summary text.

**Given** a value is classified as sensitive or its display policy is undefined
**When** configuration is rendered
**Then** the value is not displayed, copied, announced, logged, or serialized into component state
**And** no reveal control or implicit masking contract is invented in v1.

**Given** the caller has no visible configuration entries
**When** the read completes successfully
**Then** an authorization-safe localized empty state is shown rather than an error
**And** it does not imply whether hidden configuration exists.

**Given** the configuration read is loading, unavailable, stale, degraded, or has unknown freshness
**When** the state is displayed
**Then** each condition remains honest and distinct with the appropriate refresh, retry, or continue-read-only recovery
**And** parent/detail freshness is not inferred from `ServedAt` or from unrelated projection data.

**Given** the tenant detail contains multiple titled content regions
**When** configuration is composed with sibling overview or membership regions
**Then** it follows the FrontComposer page-layout contract and uses `FluentAccordion` with multi-expand behavior where the project UX rules require grouping
**And** the only primary content region is never hidden by default.

**Given** the configuration region is read-only
**When** its rendered markup is inspected
**Then** it contains no raw or Fluent mutation controls, raw forms, editable cells, or misleading command affordances
**And** semantic key/value relationships, headings, focus order, and state announcements remain accessible.

**Given** long, Unicode, or visually similar namespace keys and values
**When** the region renders at desktop, tablet, and mobile widths
**Then** literal text remains distinguishable, safely wrapped or horizontally available without dropping namespace context
**And** no layout style or truncation exposes hidden data or makes safety-critical state disappear.

**Given** English and French resources
**When** headings, namespaces, empty, loading, error, stale, degraded, unknown, and recovery text render
**Then** whole-string parity and culture-aware formatting are preserved
**And** stable selectors identify regions and states without depending on keys, values, localized text, or color.

**Given** the completed read-only configuration slice
**When** focused authorization, namespace-filtering, sensitive-value, gateway, bUnit, localization, responsive, accessibility, support-safety, and Fluent-conformance tests run
**Then** visible, hidden, empty, error, and malicious/edge-case value scenarios pass
**And** exact commands, results, and any unresolved sensitive-display policy are recorded without broadening scope.

### Story 1.7: Tenant Member Table and Action Availability

As an authorized tenant user,
I want to review members, roles, trust context, and honest action availability,
So that I can understand who has access and what could safely be changed before any mutation flow is available.

**Acceptance Criteria:**

**Given** an authorized user opens a tenant's member region
**When** membership data is available
**Then** a read-only Fluent `MemberTable` shows member identity, role, owner count, status, freshness, and orphan/disabled context
**And** identity, role, status, freshness, and risk where shown remain pinned safety context with stable row and action-slot layout.

**Given** the member table is part of the read-only MVP
**When** rows and cells render
**Then** they do not contain editable cells, mutation forms, raw interactive controls, or controls that imply a command is currently available
**And** table headers, sort state, row relationships, status names, and absolute freshness timestamps are exposed to assistive technology.

**Given** the server-side authorization-reflection service evaluates a potential member action
**When** the action is unavailable
**Then** the expected action slot renders exactly one canonical `UnavailableActionReason`: `missing permission`, `stale data`, `missing lifecycle support`, `missing consequence preview`, `missing audit proof`, or `high-impact flow not ready`
**And** the localized reason is inline-visible, programmatically associated with the row/action, keyboard/screen-reader reachable, and never tooltip-only.

**Given** authorization, freshness, lifecycle, preview, or proof eligibility is indeterminate
**When** availability is calculated
**Then** the reflected action fails closed with the most supportable canonical reason
**And** UI reflection never substitutes for API/domain authorization enforcement.

**Given** a member is the last owner, belongs to a disabled tenant, is orphaned, or also holds global-administrator authority when that fact is authorized and available
**When** risk and context are displayed
**Then** the context remains distinct: last-owner and target-also-global-admin derive high risk, disabled/orphan state is named honestly, and global authority is not conflated with tenant membership
**And** unavailable or unproven platform standing is not guessed.

**Given** the operator is reviewing a tenant member row
**When** the contextual user-membership link is activated
**Then** it opens the canonical Users tab with the safely encoded `userId` workspace state
**And** return navigation preserves the originating tenant, tab, filter, selection, cursor context, and scroll position.

**Given** membership data is loading, empty, unavailable, stale, degraded, or has unknown freshness
**When** the member region renders
**Then** the conditions remain distinct and authorization-safe with appropriate refresh, retry, or continue-read-only recovery
**And** unrelated data or SignalR notifications never promote freshness or availability.

**Given** sort, data refresh, localization, or action-availability state changes
**When** rows rerender
**Then** action/reason slots retain stable width and placement, state stays bound to the correct member, and pending/stale context is not hidden
**And** canonical vocabulary and verified semantic-role/icon mappings remain consistent across rows.

**Given** desktop, tablet, and mobile viewport widths
**When** the table is operated by keyboard, screen reader, high contrast, forced colors, or reduced motion
**Then** horizontal overflow preserves identity, role, status, freshness, and risk context, focus remains visible/logical, and no meaning depends on color or animation
**And** mobile remains a read-only access-review experience.

**Given** English and French resources
**When** roles, context, availability reasons, statuses, freshness, empty/error/degraded states, and recovery text render
**Then** whole-string parity and culture-aware formatting are preserved
**And** stable selectors identify table, row, state, risk, and unavailable-reason contracts without depending on member text or color.

**Given** the completed member access-review slice
**When** focused authorization-reflection, fail-closed, role/risk, bUnit, localization, responsive, keyboard, screen-reader, forced-colors, support-safety, and conformance tests run
**Then** every canonical unavailable reason, the member-row user-membership entry scenario, and required edge context are covered
**And** exact commands, results, and any blocked external evidence source are recorded.

### Story 1.8: Support-Safe Identifier Copy and Read-Experience Evidence

As an operator or tenant user,
I want to copy complete support-safe identifiers and rely on verified read-surface quality,
So that I can communicate about tenant access accurately without leaking internal or sensitive data.

**Acceptance Criteria:**

**Given** a visible TenantId, UserId, or approved support-safe reference is visually truncated
**When** the user activates its Fluent copy affordance
**Then** the exact complete literal caller-supplied string is copied without normalization, reparsing, or loss
**And** the value is never required to parse as a GUID, ULID, email address, or other invented identifier format.

**Given** an identifier contains Unicode, whitespace significant to the stored value, reserved URL characters, visually similar glyphs, or the maximum supported length
**When** it is displayed and copied
**Then** the monospace display and copy result preserve the authorized literal value safely
**And** tests prove no alternate hidden value, decoded form, or transport representation is copied.

**Given** a field contains or could contain a protected cursor, ETag, bearer token, decoded JWT content, command/event payload, raw `NarrativePayload`, EventStore metadata, message/correlation id, stack trace, or PII
**When** copy actions and component state are composed
**Then** no copy affordance is offered and the unsafe value is unrenderable, uncopyable, unannounceable, unloggable, and absent from serialized component state
**And** only explicitly classified support-safe references cross the BFF boundary.

**Given** a keyboard or screen-reader user activates copy
**When** the clipboard operation succeeds or fails
**Then** focus remains on the launching control and a concise localized polite announcement describes the copy result
**And** the feedback does not reuse domain command `confirmed`/Success semantics or expose the copied value unnecessarily.

**Given** copy is unavailable because of browser/circuit capability or permission
**When** the user attempts the action
**Then** an honest localized failure and safe recovery are presented without a dead end
**And** no fallback writes the value into unsafe markup, query strings, logs, or telemetry.

**Given** the read surfaces delivered by Stories 1.2 through 1.8 in English and French
**When** resource parity and whole-string usage are checked
**Then** navigation, statuses, roles, timestamps, loading/empty/error/stale/degraded/unknown states, unavailable reasons, copy feedback, and recovery text have parity
**And** no runtime sentence-fragment assembly or prohibited support-unsafe wording is present.

**Given** the read surfaces delivered by Stories 1.2 through 1.8 at desktop, tablet, mobile, high contrast, forced colors, and reduced motion
**When** responsive and accessibility evidence is collected
**Then** keyboard navigation, visible focus, screen-reader semantics, absolute timestamps, no-color-only status, navigation collapse, horizontal grid overflow, and safe mobile read behavior are demonstrated
**And** NVDA plus at least one documented browser/screen-reader pairing is included where applicable.

**Given** the automation and documentation contracts of the surfaces delivered by Stories 1.2 through 1.8
**When** focused evidence is reviewed
**Then** stable `data-testid` selectors cover every interactive/status surface without localized-text, color, or incidental-markup dependence
**And** applicable FrontComposer/Fluent reference evidence and any approved row-specific fallback are cited with owner and replacement path.

**Given** support-safety regression inputs containing token-like strings, raw payload fragments, internal ids, and stack-trace patterns
**When** DOM, announcement, clipboard, logging, telemetry-tag, and serialized-state tests run
**Then** forbidden values are absent across all channels
**And** failures identify the exact unsafe path rather than weakening the guard.

**Given** the completed Story 1.8 slice
**When** focused copy, bUnit, localization-parity, responsive, accessibility, support-safety, conformance, and relevant E2E checks run
**Then** all configured read-surface checks pass or report exact environment blockers
**And** evidence is recorded for the in-scope surfaces only; Stories 1.9 through 1.11 carry their own equivalent evidence gates, and historical completion never becomes a readiness waiver for them.

### Story 1.9: Authoritative Memories Search with Protected Paging

As a platform operator,
I want to search the complete tenant set by name or identifier without trusting stale index data as row truth,
So that I can find the right tenant quickly while preserving authorization, freshness, and paging safety.

**Acceptance Criteria:**

**Given** the operator submits a non-empty normalized search term
**When** the server-side BFF performs tenant search
**Then** it calls `MemoriesClient.SearchAsync` against `tenants-index` using the approved syntactic/BM25 mode and treats results only as ordered tenant-id candidates
**And** no browser component calls Memories or receives a Memories credential, raw response, score payload, or internal offset.

**Given** Memories returns candidate hits
**When** the BFF processes the page
**Then** tenant ids are parsed from the approved `tenant:{id}` source identity, malformed ids are dropped, duplicates are removed in first-returned order, and every surviving candidate is authorization-filtered and hydrated through the authoritative Tenants read path
**And** Memories content, score, attributes, or lag never supplies displayed tenant name, status, counts, pending state, or freshness.

**Given** hydrated authorized candidates are available
**When** the visible page is composed
**Then** the requested deterministic sort is applied to the authoritative rows within the page and exact status filtering uses the supported structured attribute when available or authoritative hydrated status in the documented interim
**And** status filtering is never implemented as fuzzy text matching.

**Given** malformed, duplicate, unauthorized, missing, or unhydrated hits occur
**When** the next search position is calculated
**Then** the internal offset advances by every raw Memories hit consumed, including dropped hits, and dropped rows are not backfilled from later offsets
**And** partial hydration produces an honest degraded state explaining what still works without revealing hidden candidate existence.

**Given** a search page has a next position
**When** the paging cursor is issued
**Then** the raw Memories offset is wrapped by the approved server-side cursor codec/DataProtection path and bound to authenticated user, normalized query, exact status, sort, direction, and page size
**And** neither the raw offset nor protected cursor appears in visible copy, DOM attributes, clipboard values, logs, telemetry tags, or client-readable state.

**Given** a cursor is tampered with, decoded by another user, used with a different search scope, expired, or invalidated
**When** the next page is requested
**Then** the server rejects cursor reuse, restarts the search from page 1, and shows an honest localized list-refreshed notice
**And** no cross-user candidates, cursor contents, cryptographic details, or generic unsafe error are disclosed.

**Given** the operator submits an empty or whitespace-only term
**When** search state is normalized
**Then** the unchanged authorization-safe cursor list from Story 1.2 is used
**And** no Memories request or in-memory loaded-page re-filter is performed.

**Given** Memories is unavailable, times out, returns invalid data, or is degraded
**When** search handles the failure
**Then** the normal cursor list remains usable with a non-blocking localized notice and distinct degraded/error behavior
**And** tenant list access is never blocked by search-index availability.

**Given** a tenant is newly created, renamed, disabled, or absent from a lagging index
**When** search results are rendered
**Then** the UI honestly tolerates eventual search-index consistency while every rendered row reflects current authorized Tenants data
**And** index lag cannot resurrect deleted/hidden row truth or suppress freshness warnings on hydrated data.

**Given** keyboard, screen-reader, responsive, localization, and support-safety requirements
**When** the search, paging, notices, loading, partial, empty, error, and fallback states render
**Then** controls use Fluent/FrontComposer primitives, focus and announcements are appropriate, safety columns remain visible, and EN/FR whole-string parity holds
**And** stable selectors do not depend on tenant text, search results, scores, or color.

**Given** the protected search implementation
**When** focused unit, gateway, cursor-codec, cross-user isolation, tampering, scope-mismatch, malformed/duplicate/drop-accounting, authorization, hydration, fallback, bUnit, localization, responsive, accessibility, and support-safety tests run
**Then** the SEARCH-CURSOR-1 contract and every edge path pass
**And** exact commands, results, and any Memories-server handoff blocker are recorded without weakening fallback behavior.

### Story 1.10: Direct Tenants Reads and Authoritative Freshness

As an authorized Tenants user,
I want every read surface to show freshness derived from the authoritative tenant projection path,
So that I can distinguish current, stale, and unmeasurable data before trusting what I see.

**Acceptance Criteria:**

**Given** the platform/composing host exposes separate Tenants-query and EventStore-command service references
**When** the UI server-side gateways are configured
**Then** query reads use the Tenants reference and commands/status lookup retain the EventStore command reference
**And** missing `HOST-REF-1` platform support is recorded as an external blocker rather than implemented by expanding the transitional repository AppHost.

**Given** the six UI read operations
**When** `ITenantQueryGateway` executes tenant list, tenant detail, tenant users, user tenants, tenant audit, or global administrators reads
**Then** each maps to its direct Tenants REST `GET` endpoint with server-side bearer relay and service discovery
**And** no operation routes through `POST /api/v1/queries`, `QueryRouter`, `HandlerAwareQueryRouter`, a projection actor, or a browser-side client.

**Given** the EventStore/Tenants platform supports the corrected REST metadata contract
**When** a read returns `200`, `304`, an empty authorized result, or an authorization-safe response
**Then** the gateway preserves supported ETag, projection version, and read-model freshness metadata without exposing those internals to rendered output
**And** missing `PLAT-FRESH-1` support is treated as an external platform gate, not silently synthesized in Tenants UI.

**Given** a prior ETag exists for an unchanged resource
**When** the BFF sends `If-None-Match` and receives `304`
**Then** the last-confirmed projection data is retained only with the freshness/projection metadata supported by the response contract
**And** a metadata-deficient `304` is not treated as proof of recovery or current projection state.

**Given** supported read-model freshness metadata
**When** the UI classifies and renders freshness
**Then** it consumes EventStore `ReadModelFreshnessState` and supported metadata rather than defining a duplicate Tenants freshness enum
**And** `ProjectedAt` represents the last projection write while `ServedAt`, request time, SignalR time, and local cache age never substitute for projection age.

**Given** the current wire contract can support `current`, `stale`, and `unknown`
**When** freshness state crosses the gateway
**Then** only supported states are claimed, `refreshing` remains a client-transient re-query state, and `aging` is not claimed until authoritative projection-time provenance supports it
**And** configurable thresholds remain conservative and contain no UI magic numbers.

**Given** freshness metadata is missing, invalid, unmeasurable, or blocked by platform work
**When** any read surface renders
**Then** freshness is `unknown`, safety-sensitive actions fail closed, and the read-only data can remain available with honest explanation
**And** no surface infers `current` from successful HTTP status, non-empty data, command status, unrelated projection changes, or live notification.

**Given** a SignalR projection notification or terminal command-status signal arrives
**When** the affected read surface handles it
**Then** it requests an authoritative Tenants REST re-query and preserves last-confirmed data separately from refreshing intent
**And** the signal alone never advances freshness, command confirmation, or audit availability.

**Given** direct Tenants reads fail, time out, return malformed metadata, or lose authorization
**When** the BFF and components handle the result
**Then** error, degraded, stale, unknown, empty, and authorization-safe absence remain distinct with appropriate recovery
**And** raw headers, ETags, cursors, tokens, Problem Details, payloads, correlations, and stack traces never enter DOM, announcements, clipboard, logs, or telemetry tags.

**Given** command submission and status behavior
**When** the read gateway is corrected
**Then** `POST /api/v1/commands` and its existing status path remain unchanged with no unversioned alias or new backend endpoint
**And** read-client separation does not alter immutable command/query contracts.

**Given** repository ownership boundaries
**When** `UI-READ-1` is implemented and verified
**Then** only Tenants-owned BFF composition, configuration, and tests are changed in this repository
**And** required EventStore or composing-host work remains separately scoped to its owning repository without root-submodule edits.

**Given** the six-read correction
**When** focused route-mapping, header/metadata propagation, 200/304/empty/auth-safe, unknown fallback, SignalR-nudge, client-separation, support-safety, and regression tests run
**Then** every read is proven to avoid the generic EventStore query route and existing command behavior remains intact
**And** exact commands, results, and `PLAT-FRESH-1`/`HOST-REF-1` blockers are recorded.

### Story 1.11: Authorized Global Administrator Review

As an authorized platform operator,
I want to review who holds global-administrator authority with trustworthy freshness,
So that I can understand platform-wide access without conflating it with tenant membership.

**Acceptance Criteria:**

**Given** an authorized operator reaches the Global Administrators contextual/module-internal surface
**When** navigation is rendered
**Then** the canonical contextual route is available without registering a separate Tenants shell entry
**And** the surface preserves a safe return path to the originating Tenants workspace context.

**Given** the global-administrator read is requested
**When** the server-side BFF composes the direct Tenants REST call
**Then** it uses `GET /api/global-administrators` and the single fixed-identity `global-administrators` aggregate scope
**And** it never routes through a tenant aggregate, tenant membership query, generic EventStore query route, or browser-side backend client.

**Given** authorized global-administrator data is returned
**When** the review surface renders
**Then** each visible row shows the administrator identity and authoritative or honestly unknown freshness using canonical text-and-icon status
**And** tenant roles, tenant owner status, and global-administrator authority remain distinct concepts in headings, rows, accessible names, and component state.

**Given** the read contract supports cursor paging or reports only a bounded page
**When** more administrators may exist than the current response contains
**Then** the UI follows the supported cursor contract or honestly labels the bounded result without implying exhaustive completeness
**And** no client-side enumeration, hidden offset, or inferred total is invented.

**Given** a tenant owner, ordinary member, or other unauthorized caller opens the workspace or direct route
**When** authorization is evaluated
**Then** the surface and its contextual entry are unavailable and no administrator identity, count, freshness, route detail, or existence signal is disclosed
**And** server-side authorization remains the enforcement boundary even if UI reflection is stale or incorrect.

**Given** the authorized result is empty, loading, unavailable, stale, degraded, or has unknown freshness
**When** the state is displayed
**Then** all applicable states remain distinct and support-safe with refresh, retry, or continue-read-only recovery
**And** empty and authorization-safe absence do not reveal whether hidden administrators exist.

**Given** a tenant member also holds global-administrator authority and the caller may see both facts
**When** the operator navigates between member and global-administrator review
**Then** the two authorities are presented as separate facts and contextual links preserve the correct fixed versus tenant scope
**And** no tenant-membership action is implied to grant or remove platform authority.

**Given** the surface is rendered at desktop, tablet, and mobile widths or used by keyboard/screen-reader/high-contrast/forced-colors users
**When** rows, states, navigation, and freshness are operated
**Then** FrontComposer/Fluent composition preserves headings, table semantics, visible focus, absolute timestamps, horizontal safety context, and no-color-only meaning
**And** mobile remains read-only with no high-impact administrator control.

**Given** English and French cultures
**When** navigation, headings, states, freshness, empty/error/degraded copy, and recovery text render
**Then** Tenants-owned whole-string resources remain parity-checked and culture-aware
**And** stable selectors identify entry, surface, rows, freshness, states, and return navigation without depending on identity text or color.

**Given** historical Stories 4.1 and 4.2 implementation evidence
**When** this corrected Story 1.11 contract is verified
**Then** historical behavior is mapped to the current FR18, direct-read, freshness, authorization, IA, and evidence requirements without blindly rebuilding it
**And** gaps are recorded as current work or external blockers rather than treated as waived by prior completion.

**Given** the completed global-administrator review slice
**When** focused fixed-scope routing, authorization, direct-read, paging/bounded-result, freshness, bUnit, localization, responsive, accessibility, support-safety, and navigation tests run
**Then** authorized, unauthorized, empty, error, stale, unknown, and contextual-return scenarios pass
**And** exact commands, results, and any projection-pagination or platform-freshness blocker are recorded.

## Epic 2: Safe Tenant Membership Management

Authorized users can add members, change roles, and remove access through projection-confirmed, fail-closed flows with last-owner safeguards and minimum removal proof.

### Story 2.1: Reverify Projection-Confirmed Membership Command Foundation

As an authorized tenant user,
I want every membership command to expose an honest, recoverable lifecycle,
So that I never mistake command acceptance or a live notification for a completed access change.

**Acceptance Criteria:**

**Given** a membership command is submitted from an InteractiveServer component
**When** the server-side command gateway constructs the request
**Then** it uses the fixed `POST /api/v1/commands` endpoint, existing command contracts, and a client-generated ULID `messageId` idempotency key
**And** no browser-side backend call, unversioned alias, new status endpoint, or reshaped command contract is introduced.

**Given** a user submits, refreshes, reconnects, or retries the same logical attempt
**When** idempotency state is available
**Then** the same attempt reuses its `messageId`/correlation tracking rather than double-dispatching
**And** a new `messageId` is generated only for a deliberate new attempt.

**Given** a command receives `202 Accepted`
**When** the lifecycle state updates
**Then** submitted, accepted, projection-pending, confirmed, audit-pending, and audit-available remain distinct typed states with casing-faithful canonical vocabulary
**And** accepted, pending, timeout, degraded, unknown, or unable-to-verify never receives success styling, copy, or announcement.

**Given** a command is in flight
**When** status polling and SignalR projection notifications run
**Then** either signal can request an authoritative direct Tenants projection re-query but neither signal directly confirms the command or audit evidence
**And** last-confirmed projection data remains separate from in-flight intent across rerender and circuit reconnect.

**Given** the authoritative projection is re-queried
**When** command reconciliation evaluates the result
**Then** `confirmed` requires the command-specific expected postcondition plus projection-version advancement or safe command-specific audit evidence beyond the pre-submit baseline
**And** a pre-existing expected state or domain NoOp becomes `already applied`, unrelated projection changes cannot confirm, and unavailable provenance becomes `unable to verify`.

**Given** a command for an `AggregateIdentity` is submitted
**When** it remains submitted, accepted, or projection-pending
**Then** the `(interactive circuit, AggregateIdentity)` lock keeps other commands for that aggregate unavailable with an inline reason through terminal evidence
**And** unrelated aggregates may proceed while bulk submission, toast batching, multi-row actions, and multiple simultaneous commands for one aggregate remain prohibited.

**Given** validation, authoritative freshness, authorization reflection, or lifecycle support is invalid or indeterminate
**When** command availability is calculated
**Then** the flow fails closed before dispatch with a canonical inline `UnavailableActionReason`
**And** UI availability remains reflection only while API/domain authorization enforces the command.

**Given** a domain rejection, NoOp, duplicate, concurrency conflict, timeout, transport failure, lost authorization, or unconfirmable result occurs
**When** the BFF maps the outcome
**Then** a structured support-safe localized state and named recovery are produced without inventing unspecified duplicate/timeout semantics
**And** raw Problem Details, payloads, tokens, internal correlations, metadata, ETags, cursors, or stack traces never reach rendered, copied, announced, logged, or serialized component output.

**Given** a lifecycle panel is rendered
**When** keyboard, screen-reader, forced-colors, reduced-motion, or responsive users operate the flow
**Then** it remains inline and anchored to the affected row/panel, focus and announcements follow dedicated intent, and assertive output is limited to rejection, failure, unable-to-verify, degraded, and destructive blockers
**And** focus returns to the launching control after close, cancel, submit, or failure.

**Given** English and French cultures
**When** lifecycle, rejection, NoOp, unavailable-reason, recovery, and validation copy renders
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders
**And** stable selectors identify triggers, locks, lifecycle steps, announcements, and recoveries without depending on localized text or color.

**Given** historical Story 2.1 create-tenant command evidence
**When** this corrected shared membership-command foundation is verified
**Then** reusable gateway/lifecycle evidence is retained while create-tenant behavior remains assigned to Epic 3
**And** historical completion does not waive aggregate-scoped locking, direct-read confirmation, reconnect, support-safety, or current evidence requirements.

**Given** the shared command foundation
**When** focused gateway, idempotency, state-transition, concurrency-lock, status/SignalR/re-query, rejection, reconnect, localization, accessibility, support-safety, and conformance tests run
**Then** every non-collapse and confirmation invariant passes with exact commands/results recorded
**And** external platform blockers preserve fail-closed or unable-to-verify behavior rather than weakening truth.

### Story 2.2: Add User to Tenant with Explicit Role

As an authorized tenant owner or operator,
I want to add a user directly to a tenant with an explicit role,
So that the user receives intended access without an unsupported invitation workflow.

**Acceptance Criteria:**

**Given** an authorized user opens the add-member flow from an eligible tenant member surface
**When** the form renders
**Then** Fluent controls collect the literal caller-supplied UserId and an explicit valid `TenantRole`
**And** no email invitation, invitation link, pending-member state, raw interactive control, or generated CRUD form is presented.

**Given** UserId is empty/whitespace, exceeds the supported boundary, contains valid Unicode/reserved characters, or the selected role is `Unknown`
**When** validation runs before dispatch
**Then** invalid values receive localized field guidance and no command is sent, while valid meaningful identifier strings are preserved without GUID/ULID parsing
**And** validation, freshness, authorization, and lifecycle eligibility fail closed with an inline reason.

**Given** valid eligible input
**When** the user submits
**Then** the server-side gateway dispatches `AddUserToTenant(TenantId, UserId, Role)` in the tenant aggregate scope with an idempotent attempt id
**And** the aggregate-scoped lock prevents overlapping commands for that tenant while unrelated aggregates remain usable.

**Given** the tenant has no remaining membership history and the caller is using the supported bootstrap/recovery path
**When** a first owner is added
**Then** the flow permits the domain-supported owner bootstrap and reflects the explicit intended role
**And** it does not invent a separate tenant-initialization or invitation contract.

**Given** the target user is already a tenant member
**When** the domain returns `UserAlreadyInTenant`
**Then** the lifecycle shows a safe localized rejection with an appropriate inspect/continue recovery
**And** it is never represented as `already applied`, success, duplicate confirmation, or a NoOp.

**Given** the tenant is disabled, authorization is lost, role escalation is refused, or another domain rejection occurs
**When** the response is mapped
**Then** the exact safe rejection category is shown without raw Problem Details or internal data
**And** confirmed tenant membership data remains unchanged with a named refresh, request-permission, or escalation path.

**Given** the command is accepted
**When** status or SignalR produces a nudge
**Then** the inline lifecycle remains accepted/projection-pending until an authoritative tenant-members re-query shows the target user with the explicit role and qualifying new provenance
**And** unrelated row changes or the pre-existing membership state cannot confirm the attempt.

**Given** the expected membership already exists before qualifying post-submit provenance, a retry is deduplicated, or confirmation cannot be established
**When** reconciliation completes
**Then** the flow renders the supportable `already applied`, duplicate, or `unable to verify` state without false success
**And** offers inspect audit, retry status lookup, refresh, continue read-only, or escalation as appropriate.

**Given** the flow is cancelled, fails validation, loses permission, encounters rejection, or completes
**When** focus handling runs
**Then** focus returns to the launching member control and announcements use the correct polite/assertive intent
**And** success is announced only after projection confirmation.

**Given** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, and reduced-motion use
**When** the add-member flow is presented
**Then** mobile or any width lacking safe command context shows an inline unavailable reason, while supported widths provide complete labels, errors, focus order, lock state, and lifecycle semantics
**And** status never depends on color or animation.

**Given** English and French resources
**When** labels, roles, validation, lifecycle, rejection, unavailable-reason, and recovery copy render
**Then** whole-string parity and culture-aware formatting hold
**And** stable selectors identify fields, trigger, submit, lock, states, and recovery without depending on UserId text or color.

**Given** the completed add-member flow
**When** focused validation, authorization, bootstrap, disabled-tenant, existing-member, role, idempotency, aggregate-lock, lifecycle, projection-confirmation, reconnect, localization, accessibility, responsive, support-safety, and E2E tests run
**Then** every success and refusal path passes with exact commands/results recorded
**And** no test accepts command acceptance alone as success.

### Story 2.3: Change Tenant Member Role

As an authorized tenant owner or operator,
I want to change a tenant member's role,
So that the member's access remains aligned with their current responsibilities.

**Acceptance Criteria:**

**Given** an authorized user opens the role-change flow from an eligible member row
**When** the control renders
**Then** it identifies the target member, displays the last-confirmed current role and freshness context, and offers only valid supported `TenantRole` values through Fluent controls
**And** `TenantRole.Unknown`, raw interactive controls, generated CRUD forms, and platform-authority options are never presented as valid choices.

**Given** the selected role is invalid, freshness is stale or unknown, authorization reflection is absent, or tenant lifecycle eligibility is indeterminate
**When** availability or validation is evaluated
**Then** submission fails closed with localized field guidance or a canonical inline unavailable reason
**And** the server remains the authorization and role-escalation enforcement boundary even when the UI believes the action is available.

**Given** valid eligible input and a role different from the last-confirmed role
**When** the user submits
**Then** the server-side gateway dispatches `ChangeUserRole(TenantId, UserId, Role)` in the tenant aggregate scope with the idempotent attempt id
**And** the aggregate-scoped lock prevents overlapping commands for that tenant while unrelated aggregates remain usable.

**Given** the requested role already equals the member's authoritative current role
**When** reconciliation or the domain identifies the unchanged state
**Then** the lifecycle reports `already applied` as a NoOp with inspect/continue recovery
**And** it is not represented as a newly confirmed success, rejection, validation error, or duplicate command outcome.

**Given** the requested role is an unsafe escalation, is `TenantRole.Unknown`, the tenant is disabled, the member is unavailable, or authorization is lost
**When** the domain or API rejects the command
**Then** the BFF maps the safe canonical rejection, including `RoleEscalation` where applicable, into localized support-safe copy
**And** raw Problem Details, internal identifiers, policy details, payloads, tokens, and correlations are not disclosed.

**Given** the command is accepted
**When** status polling or SignalR produces a nudge
**Then** the inline lifecycle remains accepted or projection-pending until an authoritative tenant-members re-query shows the target member with the requested role and qualifying new projection or safe audit provenance beyond the pre-submit baseline
**And** unrelated member changes, a matching pre-existing role, or command acceptance alone cannot confirm the attempt.

**Given** the authoritative projection shows the requested role without qualifying new provenance, a retry is deduplicated, or the expected postcondition cannot be established
**When** reconciliation completes
**Then** the flow reports `already applied`, duplicate, or `unable to verify` according to the available evidence
**And** it offers refresh, retry status lookup, inspect audit, continue read-only, request permission, or escalation as appropriate without inventing success.

**Given** a refresh, rerender, circuit reconnect, or duplicate interaction occurs while the role change is in flight
**When** the component restores state
**Then** it preserves the attempt identity, last-confirmed role, intended role, aggregate lock, and honest lifecycle without double-dispatching
**And** stale UI intent never overwrites a newer authoritative member role.

**Given** the user cancels, validation fails, permission is lost, the command is rejected, or terminal evidence is reached
**When** focus and announcement handling runs
**Then** focus returns to the launching row control and lifecycle updates use their dedicated polite or assertive intent
**And** success is announced only after projection confirmation.

**Given** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, and reduced-motion use
**When** the role-change flow is presented
**Then** any width lacking complete member, role, freshness, and lifecycle context shows an inline unavailable reason, while supported widths preserve labels, focus order, lock state, and table context
**And** status, role, availability, and recovery never depend on color, placement, or animation alone.

**Given** English and French resources
**When** labels, roles, validation, NoOp, lifecycle, rejection, unavailable-reason, and recovery copy render
**Then** Tenants-owned whole-string resources remain parity-checked and culture-aware
**And** stable selectors identify the trigger, role options, submit, lock, lifecycle states, and recoveries without depending on member text or color.

**Given** the completed role-change flow
**When** focused validation, authorization, escalation, disabled-tenant, unchanged-role, idempotency, aggregate-lock, lifecycle, projection-confirmation, reconnect, concurrency, localization, accessibility, responsive, support-safety, and E2E tests run
**Then** valid changes, NoOps, safe refusals, duplicates, and unconfirmable outcomes pass with exact commands/results recorded
**And** no test accepts command acceptance, an unrelated projection change, or a pre-existing matching role alone as success.

### Story 2.4: Remove Tenant Member with Complete Preview and Proof

As an authorized tenant owner or operator,
I want to remove a tenant member through a complete, evidence-backed flow,
So that access is withdrawn deliberately, safely, and with supportable proof.

**Acceptance Criteria:**

**Given** an authorized user reviews an eligible tenant member row
**When** the removal entry point is displayed
**Then** the target member, current tenant role, authoritative freshness, and destructive nature of the action are clear before interaction
**And** removal is never the primary row action, never offered as a bulk action, and never conflated with global-administrator authority.

**Given** validation, current membership, authoritative freshness, caller authorization, tenant lifecycle eligibility, preview completeness, or proof capability is stale, missing, unknown, or indeterminate
**When** removal availability is calculated
**Then** the action fails closed before dispatch with a canonical localized inline unavailable reason and named recovery
**And** mobile or any layout that cannot preserve the complete preview, risk context, and lifecycle also keeps removal unavailable.

**Given** the user opens an eligible removal preview
**When** the BFF assembles the consequence model from existing authorized read paths
**Then** it presents all ten required items: tenant identity, target identity, current role, owner-count impact, specific access being removed, current freshness, recovery path, audit expectation, platform-standing context, and known consequences versus unknowns
**And** the model is redacted and support-safe, introduces no new backend endpoint, and blocks confirmation when any required item is unavailable or incomplete.

**Given** the preview evaluates removal risk
**When** the target is the last tenant owner or also holds global-administrator authority
**Then** the corresponding last-owner or target-global-administrator condition is stated explicitly and adds elevated confirmation friction
**And** last-owner removal remains permitted when otherwise authorized, target global-administrator standing remains unchanged by the tenant command, and no unrelated last-global-administrator invariant is invented.

**Given** the complete preview is shown in a destructive confirmation modal
**When** the user reviews, cancels, dismisses safely, or confirms
**Then** focus is trapped within the modal, Escape and cancel perform no command, confirmation requires the specified elevated interaction when risk is high, and focus returns to the launching row control
**And** the destructive action is visually and semantically distinct without relying on color, position, or pointer interaction alone.

**Given** the complete preview remains current and the authorized user confirms removal
**When** the server-side gateway submits the attempt
**Then** it dispatches `RemoveUserFromTenant(TenantId, UserId)` in the tenant aggregate scope through `POST /api/v1/commands` with the retained idempotent attempt id
**And** the aggregate-scoped lock prevents overlapping commands for that tenant while refresh, reconnect, retry, or duplicate interaction cannot apply the removal twice.

**Given** the target was already absent before qualifying post-submit provenance or the domain reports the removal as a NoOp
**When** reconciliation evaluates the outcome
**Then** the lifecycle reports `already applied` with inspect/continue recovery
**And** pre-existing absence is not represented as a newly confirmed removal, rejection, or validation error.

**Given** the command is submitted and accepted
**When** its lifecycle advances
**Then** `submitted`, `accepted`, `projection_pending`, `confirmed`, `audit_pending`, and `audit_available` remain distinct visible states
**And** status polling or SignalR can only nudge authoritative re-query and cannot collapse acceptance, projection confirmation, and audit proof into one success state.

**Given** authoritative member data is re-queried after submission
**When** removal reconciliation runs
**Then** `confirmed` requires the target to be absent plus qualifying projection-version advancement or safe command-specific audit provenance beyond the pre-submit baseline
**And** unrelated projection changes cannot confirm, pre-existing absence becomes `already applied`, and missing provenance becomes `unable to verify`.

**Given** projection confirmation has occurred or audit evidence may become available independently
**When** the minimum WP-2A removal proof is assembled by the BFF from the existing authorized audit read path
**Then** proof state remains explicitly pending, delayed, unavailable, or available and an available proof contains only support-safe actor, target, tenant, outcome, absolute timestamp, projection marker, and reference data
**And** no new receipt endpoint is introduced and raw narrative, payload, token, internal correlation, metadata, cursor, ETag, or stack detail is rendered, copied, announced, logged, or serialized into component state.

**Given** audit proof is pending, delayed, unavailable, denied, or arrives after projection confirmation
**When** the lifecycle panel updates
**Then** the confirmed access outcome remains distinct from audit availability and displays an honest named recovery such as wait, retry status, inspect audit, request permission, or escalate
**And** missing proof is never silently treated as available or used to reverse a projection-confirmed outcome.

**Given** validation failure, stale data, authorization loss, domain rejection, concurrency conflict, transport failure, duplicate handling, timeout, unable-to-verify, or proof failure occurs
**When** the flow presents recovery
**Then** it offers the applicable refresh, wait, retry status lookup, request permission, restore intended access, inspect audit, continue read-only, or escalation path
**And** it does not promise undo, rollback, hidden editing, automatic re-addition, or any recovery the platform does not support.

**Given** a rerender, refresh, or circuit reconnect occurs during preview, submission, projection waiting, or audit waiting
**When** state is restored
**Then** the flow preserves only support-safe attempt identity, last-confirmed membership, preview baseline, aggregate lock, and honest lifecycle required for reconciliation
**And** stale preview data cannot authorize dispatch and the UI does not resubmit or disclose sensitive command/audit data.

**Given** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, high-contrast, and reduced-motion use
**When** the preview, modal, lifecycle, proof, and recovery states render
**Then** supported widths preserve complete reading order, headings, member context, focus visibility, modal semantics, absolute timestamps, dedicated live-region intent, and touch/keyboard safety
**And** destructive blockers, failures, and unable-to-verify use appropriate assertive announcements while progress and proof availability use non-disruptive intent.

**Given** English and French cultures
**When** preview labels, risk language, confirmation, lifecycle, proof, unavailable reasons, timestamps, and recovery copy render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting
**And** stable selectors identify the trigger, all ten preview items, risk conditions, confirm/cancel controls, lock, lifecycle steps, proof states, and recoveries without depending on identity text or color.

**Given** the completed member-removal flow
**When** focused completeness, stale-data, authorization, last-owner, target-global-administrator, modal, idempotency, aggregate-lock, NoOp, lifecycle, projection-confirmation, WP-2A proof, delayed/unavailable audit, reconnect, localization, accessibility, responsive, support-safety, and E2E tests run
**Then** standard, elevated-risk, already-applied, rejected, duplicate, and unconfirmable scenarios pass with exact commands/results recorded
**And** no test accepts an incomplete preview, command acceptance, pre-existing absence, unrelated projection change, or missing proof as a fully completed removal.

## Epic 3: Tenant Onboarding, Lifecycle, and Configuration

Authorized users can create and configure tenants, edit metadata, and safely enable or disable tenant operation.

### Story 3.1: Create Tenant with Projection Confirmation

As an authorized global administrator,
I want to create a tenant through a projection-confirmed command flow,
So that the tenant becomes available only after the system proves its creation.

**Acceptance Criteria:**

**Given** an authorized global administrator opens the create-tenant flow from the Tenants workspace
**When** the form renders
**Then** Fluent controls collect required tenant id and name plus optional description with Tenants-owned localized labels, accessible descriptions, field validation, and stable selectors
**And** the caller-supplied tenant id remains a literal case-sensitive string that is never generated, trimmed, normalized, slugified, or parsed as a GUID or ULID.

**Given** tenant id or name is empty/whitespace, exceeds a domain boundary, or contains otherwise invalid input
**When** client or domain validation runs
**Then** the flow displays support-safe localized field guidance and sends no invalid command
**And** valid Unicode and reserved characters supported by the domain are preserved rather than silently transformed.

**Given** authorization reflection, command connectivity, lifecycle support, or creation eligibility is missing or indeterminate
**When** action availability is evaluated
**Then** the create action fails closed with a canonical inline unavailable reason and named recovery
**And** server-side API/domain authorization remains the enforcement boundary: tenant creation is domain-enforced as global-administrator-only (`GlobalAdminRequired`), reflected for non-global-administrator callers as `missing permission` and surfaced, if dispatched anyway, as safe localized rejection text.

**Given** valid eligible input
**When** the global administrator submits a deliberate attempt
**Then** the server-side gateway dispatches `CreateTenant(TenantId, Name, Description)` through fixed `POST /api/v1/commands` using the literal tenant id as aggregate identity and a client-generated ULID `messageId` as the idempotency key
**And** no browser backend call, invitation/bootstrap alias, new endpoint, reshaped command contract, or optimistic tenant row is introduced.

**Given** a creation attempt is submitted, refreshed, reconnected, or retried
**When** its identity and admission state are restored
**Then** the same logical attempt reuses its idempotency/correlation tracking and tenant-aggregate lock without double-dispatching
**And** unrelated aggregates may proceed while bulk create, concurrent same-tenant commands, and toast batching remain prohibited.

**Given** the literal tenant id already exists
**When** the backend returns `TenantAlreadyExists`
**Then** the lifecycle shows a safe localized rejection with refresh or open-existing-tenant recovery when authorized projection data is visible
**And** it is never represented as a NoOp, `already applied`, duplicate confirmation, or successful creation.

**Given** the command is accepted
**When** command status or SignalR produces a nudge
**Then** submitted, accepted, and projection-pending remain distinct while the UI authoritatively re-queries the tenant detail or refreshed tenant list
**And** status completion, SignalR, submitted intent, or an optimistic client row cannot confirm creation.

**Given** authoritative tenant data is re-queried after submission
**When** creation reconciliation evaluates the result
**Then** `confirmed` requires the literal tenant to exist with the submitted metadata plus qualifying projection-version advancement or safe command-specific audit provenance beyond the pre-submit baseline
**And** unrelated projection changes cannot confirm and missing qualifying provenance becomes `unable to verify` rather than success.

**Given** projection confirmation refreshes the list or opens the created tenant
**When** navigation state updates
**Then** the row and detail values come only from authoritative projection data and existing search, filter, sort, cursor, selection, and scroll context remain recoverable
**And** the in-flight intent remains separate from last-confirmed projection state across rerender and reconnect.

**Given** validation failure, rejection, duplicate handling, timeout, transport failure, authorization loss, degraded status, or unconfirmable projection occurs
**When** lifecycle feedback renders
**Then** the support-safe state remains distinct with an appropriate refresh, retry status lookup, continue read-only, request permission, open existing tenant, inspect audit, or escalation path
**And** raw Problem Details, command payloads, tokens, correlations, metadata, ETags, cursors, stack traces, or PII are never rendered, copied, announced, logged, or serialized into component state.

**Given** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, and reduced-motion use
**When** the form and inline lifecycle render
**Then** supported layouts preserve complete identity, validation, focus order, lock state, lifecycle, and recovery context while unsafe narrow layouts fail closed with a visible reason
**And** focus remains recoverable, progress uses polite intent, failure/blocking uses appropriate assertive intent, and success is announced only after projection confirmation.

**Given** English and French cultures
**When** form, validation, lifecycle, rejection, unavailable-reason, and recovery copy render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting
**And** stable selectors identify entry, fields, submit/cancel, lock, lifecycle states, and recoveries without depending on tenant text or color.

**Given** historical Story 2.1 implementation evidence
**When** this corrected Story 3.1 contract is verified
**Then** the existing gateway, create flow, lifecycle model, localization, and tests are retained where they satisfy the current contract
**And** historical completion does not waive fixed endpoint, aggregate locking, provenance-qualified confirmation, reconnect, support-safety, accessibility, or current evidence requirements.

**Given** the completed create-tenant flow
**When** focused contract, validation, authorization, existing-tenant rejection, idempotency, aggregate-lock, lifecycle, projection-provenance, navigation-context, reconnect, localization, accessibility, responsive, support-safety, and E2E tests run
**Then** successful, rejected, duplicate, degraded, and unconfirmable scenarios pass with exact commands/results recorded
**And** no test accepts command acceptance, a pre-existing tenant, an unrelated projection change, or optimistic state alone as creation success.

### Story 3.2: Edit Tenant Metadata with Recorded Updates

As an authorized tenant contributor or global administrator,
I want to edit tenant metadata through a projection-confirmed command flow,
So that tenant records are maintained without hiding validation errors or recorded updates.

**Acceptance Criteria:**

**Given** an authorized tenant contributor or global administrator opens metadata editing from an eligible tenant detail surface
**When** the form renders
**Then** the literal tenant identity is fixed and visible while Fluent controls edit name and optional description using the last-confirmed metadata baseline
**And** labels, descriptions, validation relationships, freshness, cancel/submit controls, and stable selectors are complete without generated CRUD or raw interactive controls.

**Given** name or description is empty/whitespace where prohibited, exceeds a domain boundary, or otherwise fails validation
**When** local or domain validation runs
**Then** safe localized field guidance is associated with the affected control and no invalid command is sent
**And** valid caller input is preserved without silent normalization or leaking backend validation payloads.

**Given** tenant freshness is stale or unknown, the tenant is disabled, authorization reflection is missing or indeterminate, lifecycle support is unavailable, or another command owns the tenant aggregate lock
**When** metadata action availability is evaluated
**Then** editing fails closed with a canonical localized inline unavailable reason and named recovery
**And** server-side API/domain authorization remains the enforcement boundary for contributor and global-administrator access.

**Given** valid eligible metadata input
**When** the user submits
**Then** the server-side gateway dispatches `UpdateTenant(TenantId, Name, Description)` through fixed `POST /api/v1/commands` with a client-generated ULID attempt id in the tenant aggregate scope
**And** the aggregate lock prevents overlapping commands for that tenant while unrelated aggregates remain usable.

**Given** submitted name and description equal the authoritative current values
**When** the authorized command is handled
**Then** the flow expects the backend to emit `TenantUpdated` exactly as it does for any successful edit
**And** it never suppresses dispatch or labels the outcome as a NoOp, `already applied`, validation error, or unchanged-state rejection.

**Given** the tenant is disabled or missing, permission is lost, input is rejected, or another domain rejection occurs
**When** the BFF maps the response
**Then** the lifecycle displays the applicable support-safe localized rejection while keeping last-confirmed metadata unchanged
**And** raw Problem Details, command/event payloads, policy internals, tokens, correlations, metadata, stack traces, or PII are not disclosed.

**Given** the command is accepted
**When** status polling or SignalR produces a nudge
**Then** submitted, accepted, and projection-pending remain distinct while an authoritative tenant-detail re-query evaluates the literal tenant id, submitted name, and submitted description
**And** status completion, SignalR, or submitted form state cannot overwrite visible last-confirmed metadata or confirm the update.

**Given** authoritative detail data is re-queried after submission
**When** metadata reconciliation evaluates the result
**Then** `confirmed` requires the submitted metadata plus projection-version advancement or safe command-specific audit provenance beyond the pre-submit baseline
**And** this provenance requirement also applies when values are identical, unrelated projection changes cannot confirm, and missing evidence becomes `unable to verify`.

**Given** a refresh, rerender, circuit reconnect, or duplicate interaction occurs while editing or awaiting evidence
**When** the component restores state
**Then** it preserves the last-confirmed metadata, submitted intent, attempt identity, aggregate lock, validation state, and honest lifecycle without double-dispatching
**And** stale intent never overwrites newer authoritative tenant detail.

**Given** the user cancels, validation fails, permission is lost, the command is rejected, or terminal evidence is reached
**When** focus and announcement handling runs
**Then** focus returns to the launching detail control or moves to the relevant invalid/failure region without becoming stranded
**And** routine progress uses polite intent, blockers/failures use appropriate assertive intent, and success is announced only after qualified projection confirmation.

**Given** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, and reduced-motion use
**When** the metadata form and inline lifecycle render
**Then** supported widths preserve tenant identity, complete fields, freshness, focus order, lock state, lifecycle, and recovery while unsafe widths fail closed with a visible reason
**And** validation, availability, lifecycle, and recovery never depend on color, placement, or animation alone.

**Given** English and French cultures
**When** labels, validation, lifecycle, rejection, unavailable-reason, and recovery copy render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting
**And** stable selectors identify entry, fields, submit/cancel, lock, lifecycle states, and recoveries without depending on tenant metadata or color.

**Given** historical Story 2.5 implementation evidence
**When** this corrected Story 3.2 contract is verified
**Then** existing update gateway, metadata flow, state model, localization, and tests are retained where they satisfy the current contract
**And** historical completion does not waive always-emit semantics, aggregate locking, provenance-qualified same-value confirmation, reconnect, support-safety, accessibility, or current evidence requirements.

**Given** the completed metadata-edit flow
**When** focused validation, contributor/global-admin authorization, disabled/missing tenant, same-value update, idempotency, aggregate-lock, lifecycle, projection-provenance, reconnect, localization, accessibility, responsive, support-safety, and E2E tests run
**Then** changed-value, same-value, rejected, duplicate, degraded, and unconfirmable scenarios pass with exact commands/results recorded
**And** no test accepts matching values, command acceptance, status completion, or unrelated projection activity alone as update success.

### Story 3.3: Lifecycle and Configuration Availability Guardrail

As an authorized tenant operator,
I want to see whether lifecycle and configuration actions are available and why they may be blocked,
So that high-impact controls never appear casual, incomplete, or falsely available.

**Acceptance Criteria:**

**Given** a tenant detail surface has authoritative tenant, configuration, and authorization context
**When** high-impact action availability is evaluated
**Then** enable, disable, set configuration, and remove configuration are calculated separately from server-reflected permission, tenant lifecycle, projection freshness, namespace scope, command support, aggregate admission, preview readiness, and viewport safety
**And** an unknown or indeterminate input fails only the affected action closed with a canonical localized reason.

**Given** authorization reflection is missing, stale, or indeterminate
**When** lifecycle or configuration action slots render
**Then** the action remains unavailable with the visible `missing permission` reason and request-permission or escalation recovery
**And** client-side claims, visible rows, or hidden controls are never treated as authorization enforcement.

**Given** tenant or configuration provenance is stale, unknown, degraded, or being refreshed without a current qualifying baseline
**When** action availability is calculated
**Then** the affected actions remain unavailable with the visible `stale data` reason and refresh or continue-read-only recovery
**And** last-confirmed status and configuration remain visible without optimistic mutation.

**Given** the fixed command endpoint, command lifecycle, aggregate admission, or required status/re-query support is unavailable
**When** an action slot renders
**Then** it shows the visible `missing lifecycle support` reason and no command submission path
**And** no raw connectivity, endpoint, correlation, transport, or platform-internal detail is disclosed.

**Given** the BFF cannot assemble every required item of a complete support-safe consequence preview
**When** enable, disable, set configuration, or remove configuration availability is evaluated
**Then** the action remains unavailable with the visible `missing consequence preview` reason naming the missing safe input and an appropriate refresh or escalation path
**And** partial preview content never enables submission or implies lower risk.

**Given** an additional proof prerequisite is explicitly required but unavailable for an affected action
**When** availability is evaluated
**Then** the action uses the canonical `missing audit proof` reason without conflating proof capability with projection confirmation
**And** the guardrail does not invent an audit receipt, proof endpoint, or completed audit state.

**Given** an aggregate command is active, complete high-impact context cannot fit the current form factor, or another approved readiness gate is unresolved
**When** action availability is calculated
**Then** the affected action shows `high-impact flow not ready` with an inline explanation and wait, continue-read-only, or escalation recovery
**And** mobile and unsafe narrow layouts remain read-only for lifecycle and configuration mutation.

**Given** the tenant is already active or already disabled
**When** lifecycle availability is evaluated
**Then** the matching enable or disable action is unavailable and safe text identifies `TenantLifecycleStateAlreadySet` as the expected domain rejection
**And** the same-state case is never shown as success, `already applied`, a completable confirmation flow, or an optimistic lifecycle transition.

**Given** authoritative status shows the tenant is disabled
**When** action availability is evaluated
**Then** eligible global administrators may reach only the enable flow after all high-impact gates pass, while tenant-scoped metadata, membership, and configuration mutations remain unavailable because disabled-tenant commands reject as `TenantDisabled`
**And** disabled status remains explicitly an eventually-consistent availability signal rather than hard deletion.

**Given** a configuration key is outside the caller's authorized namespace, required key/value context is incomplete, or an authoritative key lookup proves the removal target is absent
**When** configuration availability is evaluated
**Then** set/remove controls fail closed with the applicable canonical permission, stale-data, or high-impact-readiness reason and safe domain-outcome context such as `ConfigurationKeyNotFound`
**And** hidden namespaces, sensitive values, exhaustive key existence, or inferred permission details are not disclosed.

**Given** all inputs for an action are current and eligible
**When** its guardrail result is rendered
**Then** the action may open its dedicated complete preview flow but Story 3.3 itself performs no lifecycle or configuration command submission
**And** it never renders submitted, accepted, confirmed, audit-available, or other attempt lifecycle states without an actual command attempt.

**Given** keyboard, screen-reader, forced-colors, high-contrast, reduced-motion, desktop, tablet, or mobile use
**When** action slots and reasons render
**Then** current tenant identity, status, freshness, action name, visible hover-independent reason, and recovery remain associated and keyboard reachable
**And** availability and risk never depend on tooltip, color, icon, disabled-control semantics, placement, or pointer interaction alone.

**Given** English and French cultures
**When** action labels, canonical reasons, domain-outcome context, and recovery copy render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting
**And** stable selectors identify every action, eligibility state, reason, freshness fact, and recovery without depending on tenant/key text or color.

**Given** historical Story 3.1 and configuration-view implementation evidence
**When** this corrected Story 3.3 guardrail is verified
**Then** the existing lifecycle availability model and visible blocked-state patterns are retained and extended to configuration where they satisfy the current contract
**And** obsolete categorical blocking of reversible lifecycle control is replaced by the current approved gate evaluation without weakening fail-closed behavior.

**Given** the completed availability guardrail
**When** focused authorization, freshness, lifecycle support, preview completeness, proof prerequisite, aggregate admission, same-state lifecycle, disabled tenant, namespace scope, missing key, responsive, localization, accessibility, support-safety, and component tests run
**Then** each action's eligible and blocked combinations pass with visible canonical reasons and no accidental submission path
**And** no test enables an action from indeterminate data, partial preview, client-only authority, or unsafe viewport context.

### Story 3.4: Disable or Enable Tenant with Complete Preview

As an authorized global administrator,
I want to disable or enable a tenant through complete high-impact confirmation,
So that tenant availability changes are deliberate, reversible, and proven by authoritative projection truth.

**Acceptance Criteria:**

**Given** Story 3.3 reports a lifecycle operation eligible
**When** the global administrator opens its consequence preview
**Then** the BFF assembles all ten required support-safe items: tenant identity, requested lifecycle state, current lifecycle state, owner-count/membership impact, specific operational availability change, authoritative freshness, recovery path, audit expectation, caller/platform scope, and known consequences versus known unknowns
**And** no new backend endpoint is introduced and any missing item blocks confirmation with a canonical inline reason.

**Given** the disable preview describes the requested operational change
**When** consequences are presented
**Then** it states that disabled status is an eventually-consistent availability signal and tenant-scoped commands against a confirmed disabled tenant are rejected as `TenantDisabled`
**And** it does not claim immediate session/token invalidation, data deletion, membership removal, retention changes, or other behavior that is not proven.

**Given** an enable preview is presented for a confirmed disabled tenant
**When** recovery consequences are described
**Then** it states that enablement is a forward reversible lifecycle operation whose availability is confirmed only after projection truth shows active
**And** it does not promise restoration of unrelated failed commands, sessions, external integrations, or state beyond the lifecycle transition.

**Given** the complete high-impact preview is shown
**When** the administrator reviews, cancels, presses Escape, or confirms
**Then** focus is trapped while open, cancel and Escape dispatch nothing, exact typed tenant identity or approved operation phrase provides elevated confirmation friction, and focus returns to the launching control
**And** disable/enable is never styled or positioned as a casual primary action and is unavailable on layouts that cannot preserve all safety context.

**Given** current authoritative state already equals the requested active or disabled state
**When** availability or the backend evaluates the request
**Then** the flow is unavailable or reports the safe `TenantLifecycleStateAlreadySet` rejection
**And** it is never treated as a NoOp, `already applied`, duplicate success, or newly confirmed lifecycle transition.

**Given** preview data remains current, global-administrator authority is reflected, and explicit confirmation succeeds
**When** the user submits
**Then** the server-side gateway dispatches `DisableTenant(TenantId)` or `EnableTenant(TenantId)` through fixed `POST /api/v1/commands` with the retained idempotent attempt id in the literal tenant aggregate scope
**And** the aggregate lock keeps metadata, membership, lifecycle, and configuration commands for that tenant unavailable while unrelated aggregates may proceed.

**Given** permission is lost, the tenant is missing, state changes concurrently, `TenantDisabled` applies to an affected non-enable command path, or another rejection occurs
**When** the BFF maps the result
**Then** it displays the precise support-safe localized outcome and keeps last-confirmed lifecycle truth unchanged
**And** raw Problem Details, payloads, tokens, correlations, claims, metadata, ETags, cursors, stack traces, or PII are not disclosed.

**Given** a lifecycle command is accepted
**When** status polling or SignalR produces a nudge
**Then** submitted, accepted, and projection-pending remain distinct while an authoritative tenant-detail re-query runs
**And** status completion, SignalR, intended state, or an optimistic badge cannot confirm the transition.

**Given** authoritative tenant detail is re-queried after submission
**When** lifecycle reconciliation evaluates the result
**Then** disable confirms only when status is disabled and enable confirms only when status is active, with projection-version advancement or safe command-specific audit provenance beyond the pre-submit baseline
**And** unrelated projection changes cannot confirm, a target state without qualifying provenance cannot become success, and unavailable evidence becomes `unable to verify`.

**Given** projection confirmation and audit evidence become available at different times
**When** the inline lifecycle panel updates
**Then** submitted, accepted, projection-pending, confirmed, audit-pending, audit-delayed, audit-unavailable, and audit-available remain honest distinct states
**And** audit handoff uses existing authorized paths without fabricating proof or making audit availability a prerequisite for truthful projection confirmation.

**Given** refresh, rerender, circuit reconnect, retry, or duplicate interaction occurs during preview or command tracking
**When** state is restored
**Then** the flow preserves the preview baseline, explicit intended operation, last-confirmed lifecycle, attempt identity, aggregate lock, and honest lifecycle without double-dispatching
**And** stale preview data blocks confirmation and never overwrites newer authoritative status.

**Given** failure, rejection, concurrency conflict, timeout, degradation, authorization loss, or unable-to-verify occurs
**When** recovery is presented
**Then** the flow offers the applicable refresh, wait, retry status lookup, continue read-only, request permission, inspect audit, retry as a deliberate new attempt, or escalation path
**And** it never promises undo, rollback, hard deletion reversal, hidden editing, or automatic replay.

**Given** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, high-contrast, and reduced-motion use
**When** preview, confirmation, lifecycle, and recovery render
**Then** supported layouts preserve tenant identity, all ten items, reading order, focus, modal semantics, absolute timestamps, no-color-only status, and dedicated live-region intent while mobile/unsafe widths fail closed visibly
**And** routine progress is polite, destructive blockers/failures are appropriately assertive, and success is announced only after qualified projection confirmation.

**Given** English and French cultures
**When** preview, risk, confirmation, lifecycle, rejection, unavailable-reason, and recovery copy render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting
**And** stable selectors identify both actions, all ten preview items, typed confirmation, cancel/confirm, lock, lifecycle states, and recoveries without depending on tenant text or color.

**Given** historical Story 3.2 implementation evidence and the approved reversible-lifecycle correction
**When** this corrected Story 3.4 contract is verified
**Then** the existing lifecycle availability, gateway, preview, command state, localization, and tests are retained where they satisfy the current contract
**And** historical completion does not waive complete preview, aggregate locking, provenance-qualified confirmation, reconnect, support-safety, accessibility, or current evidence requirements.

**Given** the completed disable/enable flow
**When** focused preview-completeness, authorization, same-state rejection, disabled-command context, typed confirmation, idempotency, aggregate-lock, lifecycle, projection-provenance, audit handoff, reconnect, localization, accessibility, responsive, support-safety, and E2E tests run
**Then** disable, enable, safe refusal, duplicate, degraded, and unconfirmable scenarios pass with exact commands/results recorded
**And** no test accepts partial preview, command acceptance, intended state, target state without provenance, or audit availability alone as lifecycle success.

### Story 3.5: Set Namespaced Configuration with Complete Preview

As an authorized tenant user,
I want to set a namespaced configuration key and value through a complete preview,
So that configuration changes remain scoped, deliberate, projection-confirmed, and safe from sensitive-value disclosure.

**Acceptance Criteria:**

**Given** an authorized user opens the set-configuration flow from an eligible tenant configuration surface
**When** the form renders
**Then** Fluent controls collect authorized namespace/prefix, key, and value while preserving last-confirmed configuration, tenant identity, status, and freshness context
**And** no out-of-scope namespace, hidden key existence, sensitive value, generated CRUD form, or raw interactive control is disclosed.

**Given** tenant id, namespace/prefix, key, or value is invalid or the full key/value exceeds domain limits
**When** validation runs
**Then** localized field guidance blocks preview or submission, including the supported maximum full-key length of 256 and value length of 1024
**And** valid literal key/value input is not silently normalized while support-unsafe values are never echoed into validation summaries, announcements, logs, or copied feedback.

**Given** server/BFF-reflected namespace ownership or authorization scope cannot be proven
**When** configuration availability is evaluated
**Then** the action fails closed with the canonical `missing permission` reason and request-permission or escalation recovery
**And** client-only claims, entered prefixes, visible rows, or absence from the authorized projection cannot establish scope or reveal whether hidden keys exist.

**Given** validation, freshness, authorization, tenant lifecycle, aggregate admission, command support, or preview support is stale, missing, unknown, or indeterminate
**When** the user prepares a change
**Then** set configuration remains unavailable with the applicable canonical inline reason and named recovery
**And** a disabled tenant is handled through the safe `TenantDisabled` outcome without overwriting last-confirmed configuration.

**Given** Story 3.3 reports the action eligible
**When** the BFF assembles the consequence preview
**Then** it supplies all ten configuration-specific items: tenant identity, authorized namespace/prefix, key, current known state, intended effect, authoritative freshness, recovery path, audit expectation, authorization/scope evidence, and known consequences versus known unknowns
**And** the preview uses redacted support-safe fields, introduces no new backend endpoint, and blocks confirmation if any required item is missing.

**Given** every eligible set operation is evaluated in v1
**When** preview policy is applied
**Then** the complete consequence preview is required regardless of an assumed low-risk key classification
**And** no low-risk bypass is added unless a later Product/UX/Architecture decision defines classification, user-facing reasons, tests, and phasing impact.

**Given** the complete preview is displayed
**When** the user reviews, cancels, presses Escape, or explicitly confirms
**Then** cancel and Escape dispatch nothing, focus remains complete and returns to the launching control, and confirmation cannot occur accidentally or through a casual primary action
**And** the raw submitted value is not repeated in preview, lifecycle, audit-handoff, recovery, or announcement copy.

**Given** the last-confirmed authoritative projection already contains the exact full key and value
**When** the flow reconciles before submission or a domain NoOp
**Then** it reports `already applied` and dispatches no unnecessary new attempt where the current baseline is sufficient
**And** it never presents the NoOp as projection-confirmed success, rejection, or an audit-proven change.

**Given** preview data remains current and the authorized user confirms a non-identical change
**When** the server-side gateway submits
**Then** it dispatches `SetTenantConfiguration(TenantId, Key, Value)` through fixed `POST /api/v1/commands` with the retained idempotent attempt id in the literal tenant aggregate scope
**And** the aggregate lock prevents overlapping metadata, membership, lifecycle, or configuration commands for that tenant while unrelated aggregates remain usable.

**Given** the domain key-count, key-length, or value-length limits are exceeded, permission is lost, the tenant is missing/disabled, or another rejection occurs
**When** the BFF maps the response
**Then** `ConfigurationLimitExceeded` or the applicable safe canonical rejection is shown with localized field/recovery guidance and last-confirmed data remains unchanged
**And** raw values, Problem Details, payloads, tokens, correlations, claims, metadata, ETags, cursors, stack traces, or PII are not disclosed.

**Given** a set command is accepted
**When** status polling or SignalR produces a nudge
**Then** submitted, accepted, and projection-pending remain distinct while the BFF authoritatively re-queries and safely compares the matching tenant, full key, and submitted value
**And** status completion, SignalR, form intent, or optimistic table content cannot confirm the change.

**Given** authoritative configuration data is re-queried after a non-NoOp submission
**When** reconciliation evaluates the result
**Then** `confirmed` requires the exact key/value postcondition plus projection-version advancement or safe command-specific audit provenance beyond the pre-submit baseline
**And** the comparison occurs without exposing the raw value, unrelated projection changes cannot confirm, and insufficient evidence becomes `unable to verify`.

**Given** command status proves zero emitted events and authoritative data still proves the exact pre-existing key/value
**When** post-submit reconciliation completes
**Then** the flow reports `already applied` rather than confirmed success
**And** if exact authoritative comparison is impossible it reports `unable to verify` instead of assuming a NoOp or success.

**Given** projection and audit evidence become available independently
**When** lifecycle feedback renders
**Then** accepted, projection-pending, confirmed, already-applied, audit-pending, audit-delayed, audit-unavailable, and missing-support states remain distinct with applicable recovery
**And** this story fabricates neither an Audit Evidence Receipt nor `audit available` without an authorized evidence source.

**Given** refresh, rerender, circuit reconnect, retry, or duplicate interaction occurs during input, preview, or command tracking
**When** state is restored
**Then** the flow preserves only the necessary support-safe preview baseline, intended key reference, last-confirmed configuration, attempt identity, aggregate lock, and honest lifecycle without double-dispatching
**And** stale preview data blocks confirmation and raw sensitive values are not leaked through persisted rendered state or diagnostics.

**Given** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, high-contrast, and reduced-motion use
**When** form, preview, lifecycle, and recovery render
**Then** supported layouts preserve tenant/scope context, all ten preview items, labels, errors, focus order, lock state, no-color-only feedback, and dedicated live-region intent while mobile/unsafe widths fail closed visibly
**And** routine progress and `already applied` are polite, blockers/failures are appropriately assertive, and success is announced only after qualified projection confirmation.

**Given** English and French cultures
**When** form, preview, validation, NoOp, rejection, lifecycle, unavailable-reason, and recovery copy render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting
**And** stable selectors identify fields, all ten preview items, confirm/cancel, lock, lifecycle states, and recoveries without depending on namespace/key/value text or color.

**Given** historical Story 3.3 implementation evidence
**When** this corrected Story 3.5 contract is verified
**Then** existing configuration read behavior, set flow, gateway, preview, state model, localization, redaction, and tests are retained where they satisfy the current contract
**And** historical completion does not waive preview-for-every-mutation, aggregate locking, provenance-qualified confirmation, reconnect, sensitive-value support safety, accessibility, or current evidence requirements.

**Given** the completed set-configuration flow
**When** focused namespace authorization, validation limits, preview completeness, identical-value NoOp, disabled tenant, `ConfigurationLimitExceeded`, idempotency, aggregate-lock, lifecycle, projection-provenance, audit handoff, reconnect, localization, accessibility, responsive, sensitive-value, support-safety, and E2E tests run
**Then** change, already-applied, safe refusal, duplicate, degraded, and unconfirmable scenarios pass with exact commands/results recorded
**And** no test accepts partial preview, raw-value disclosure, command acceptance, a pre-existing value as new success, or unrelated projection activity as completion.

### Story 3.6: Remove Configuration Key with Complete Preview

As an authorized tenant user,
I want to remove a namespaced configuration key through a complete preview,
So that obsolete configuration is removed deliberately without exposing unauthorized or sensitive data.

**Acceptance Criteria:**

**Given** an authorized user reviews a configuration key visible in their authorized projection scope
**When** the removal entry point renders
**Then** it is associated with the literal tenant, authorized namespace/prefix, selected key, current freshness, and destructive nature of removal
**And** removal cannot start from free-form hidden keys, out-of-scope namespaces, bulk selection, or an inferred exhaustive configuration inventory.

**Given** the key is outside authorized scope or namespace evidence is missing or indeterminate
**When** removal availability is evaluated
**Then** the action fails closed with the canonical `missing permission` reason and request-permission or escalation recovery
**And** the UI does not reveal whether an unauthorized key exists, its value, its namespace membership, or its removal eligibility.

**Given** the target key is absent from the current authorized projection, freshness is stale/unknown, the tenant is disabled, command support is unavailable, or another high-impact prerequisite is incomplete
**When** availability is evaluated
**Then** removal remains unavailable with the applicable canonical reason and refresh, continue-read-only, wait, or escalation recovery
**And** pre-submit absence is treated as missing/stale target evidence rather than success or an `already applied` NoOp.

**Given** Story 3.3 reports the action eligible
**When** the BFF assembles the consequence preview
**Then** it supplies all ten configuration-removal items: tenant identity, authorized namespace/prefix, selected key, current known state, intended removal effect, authoritative freshness, recovery path, audit expectation, authorization/scope evidence, and known consequences versus known unknowns
**And** the preview uses redacted support-safe fields, introduces no new backend endpoint, and blocks confirmation if any required item is missing.

**Given** every eligible configuration removal is evaluated in v1
**When** preview policy is applied
**Then** the complete consequence preview is required with no low-risk-key bypass
**And** current or sensitive values are not echoed into preview, lifecycle, audit-handoff, recovery, announcement, logging, or copied feedback.

**Given** the complete removal preview is displayed
**When** the user reviews, cancels, presses Escape, or explicitly confirms
**Then** focus is trapped while confirmation is open, cancel and Escape dispatch nothing, destructive confirmation cannot occur accidentally, and focus returns to the launching key control
**And** removal is never presented as a casual primary row action and remains unavailable on layouts that cannot preserve all safety context.

**Given** preview data remains current and the authorized user confirms removal
**When** the server-side gateway submits
**Then** it dispatches `RemoveTenantConfiguration(TenantId, Key)` through fixed `POST /api/v1/commands` with the retained idempotent attempt id in the literal tenant aggregate scope
**And** the aggregate lock prevents overlapping set/remove configuration, metadata, membership, or lifecycle commands for that tenant while unrelated aggregates remain usable.

**Given** the target key is missing when the domain handles the command
**When** `ConfigurationKeyNotFound` is returned
**Then** the lifecycle shows a safe localized rejected state with refresh or inspect-current-configuration recovery and preserves unrelated visible keys
**And** the result is never represented as success, `already applied`, a NoOp, duplicate confirmation, or proof that this attempt removed the key.

**Given** permission is lost, the tenant is missing/disabled, the key becomes unauthorized, or another rejection occurs
**When** the BFF maps the result
**Then** the applicable support-safe localized outcome appears while last-confirmed configuration remains unchanged
**And** raw values, Problem Details, payloads, tokens, correlations, claims, metadata, ETags, cursors, stack traces, or PII are not disclosed.

**Given** a removal command is accepted
**When** status polling or SignalR produces a nudge
**Then** submitted, accepted, and projection-pending remain distinct while the BFF authoritatively re-queries the matching tenant configuration
**And** status completion, SignalR, removal intent, or optimistic row deletion cannot confirm removal.

**Given** authoritative configuration is re-queried after submission
**When** removal reconciliation evaluates the result
**Then** `confirmed` requires the selected key to be absent plus projection-version advancement or safe command-specific audit provenance beyond the pre-submit baseline that proved the key present
**And** unrelated projection changes cannot confirm, absence without qualifying provenance becomes `unable to verify`, and no unrelated row is removed.

**Given** projection and audit evidence become available independently
**When** lifecycle feedback renders
**Then** accepted, projection-pending, confirmed, rejected, audit-pending, audit-delayed, audit-unavailable, and missing-support states remain distinct with applicable recovery
**And** this story fabricates neither an Audit Evidence Receipt nor `audit available` without an authorized evidence source.

**Given** refresh, rerender, circuit reconnect, retry, or duplicate interaction occurs during preview or command tracking
**When** state is restored
**Then** the flow preserves the support-safe target reference, preview baseline, last-confirmed configuration, attempt identity, aggregate lock, and honest lifecycle without double-dispatching
**And** stale preview data blocks confirmation and no sensitive value or command internals leak through rendered state or diagnostics.

**Given** failure, rejection, timeout, concurrency conflict, authorization loss, degradation, or unable-to-verify occurs
**When** recovery is presented
**Then** the flow offers the applicable refresh, wait, retry status lookup, continue read-only, request permission, inspect audit, deliberate new attempt, or escalation path
**And** it never promises undo, rollback, hidden editing, history deletion, or automatic recreation of the removed key.

**Given** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, high-contrast, and reduced-motion use
**When** entry, preview, confirmation, lifecycle, and recovery render
**Then** supported layouts preserve tenant/scope/key context, all ten preview items, reading order, focus, modal semantics, lock state, no-color-only feedback, and dedicated live-region intent while mobile/unsafe widths fail closed visibly
**And** routine progress is polite, destructive blockers/failures are appropriately assertive, and success is announced only after qualified projection confirmation.

**Given** English and French cultures
**When** preview, confirmation, rejection, lifecycle, unavailable-reason, and recovery copy render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting
**And** stable selectors identify row entry, all ten preview items, confirm/cancel, lock, lifecycle states, and recoveries without depending on namespace/key/value text or color.

**Given** historical Story 3.4 implementation evidence
**When** this corrected Story 3.6 contract is verified
**Then** existing configuration read/set behavior, remove flow, gateway, preview, state model, localization, redaction, and tests are retained where they satisfy the current contract
**And** historical completion does not waive missing-key rejection semantics, preview completeness, aggregate locking, provenance-qualified confirmation, reconnect, sensitive-value support safety, accessibility, or current evidence requirements.

**Given** the completed remove-configuration flow
**When** focused authorized-row entry, namespace isolation, preview completeness, missing-key rejection, disabled tenant, idempotency, aggregate-lock, lifecycle, projection-provenance, audit handoff, reconnect, localization, accessibility, responsive, sensitive-value, support-safety, and E2E tests run
**Then** removal, safe refusal, duplicate, degraded, and unconfirmable scenarios pass with exact commands/results recorded
**And** no test accepts partial preview, pre-existing absence, `ConfigurationKeyNotFound`, command acceptance, optimistic deletion, or unrelated projection activity as removal success.

## Epic 4: Global Administrator Control

Authorized operators can grant and remove platform-wide administrators while preserving the fixed aggregate scope and last-administrator invariant.

### Story 4.1: Fixed-Scope Global Administrator Action Availability

As an authorized global administrator,
I want to see whether grant and removal actions are available and why they may be blocked,
So that platform authority changes remain fixed-scope, fail-closed, and distinct from tenant membership.

**Acceptance Criteria:**

**Given** the authorized global-administrator review surface from Story 1.11 has loaded
**When** action availability is evaluated
**Then** grant and each row's remove action are calculated independently from server-reflected authority, direct-read freshness/provenance, command support, aggregate admission, target visibility, administrator-count completeness, preview readiness where required, and viewport safety
**And** an unknown or indeterminate input fails the affected action closed with a canonical localized inline reason.

**Given** a grant or removal action is evaluated
**When** its command scope is displayed or prepared
**Then** the UI fixes envelope tenant to `system`, domain to `global-administrators`, and aggregate identity to `global-administrators`
**And** no tenant id, tenant membership, tenant owner role, selected tenant, or row-derived aggregate can alter that scope.

**Given** the caller is a tenant owner, ordinary member, unauthorized user, or has indeterminate platform authority
**When** the review route or action slot is evaluated
**Then** mutation actions are absent or unavailable with safe `missing permission` behavior and no administrator identity, count, action, route detail, or existence signal is disclosed
**And** server-side API/domain authorization remains the enforcement boundary.

**Given** direct global-administrator projection freshness is stale, unknown, degraded, unavailable, or lacks qualifying provenance
**When** action availability is calculated
**Then** grant and removal fail closed with the visible `stale data` reason and refresh or continue-read-only recovery while last-confirmed authorized data remains separate
**And** `ServedAt`, client load time, SignalR, or tenant projection freshness cannot substitute for fixed-scope projection provenance.

**Given** command connectivity, status tracking, authoritative re-query, or shared lifecycle support is unavailable
**When** actions render
**Then** the visible `missing lifecycle support` reason blocks submission with an appropriate retry, continue-read-only, or escalation path
**And** no raw endpoint, transport, correlation, configuration, or platform-internal detail is disclosed.

**Given** a command is already active for the fixed `global-administrators` aggregate in the interactive circuit
**When** another grant or removal is considered
**Then** all global-administrator mutation actions remain unavailable with `high-impact flow not ready` until terminal evidence releases the aggregate lock
**And** unrelated tenant aggregates may remain usable while bulk grants/removals, multi-row mutation, and toast batching remain prohibited.

**Given** removal eligibility depends on the current administrator count
**When** the direct read is paged, bounded, incomplete, or cannot prove the authoritative complete count
**Then** every removal action fails closed with a visible reason and refresh, load-supported-pages, or escalation recovery
**And** a page row count, inferred total, client enumeration, or hidden administrator assumption is never used to bypass last-administrator protection.

**Given** authoritative current data proves exactly one global administrator remains
**When** removal availability is evaluated for that administrator
**Then** removal is unavailable with a safe localized last-administrator reason before any preview or confirmation can open
**And** the case is never represented as elevated friction, an override, a disabled-looking but callable control, or a completable action.

**Given** more than one administrator is authoritatively proven and a visible target is otherwise eligible
**When** removal availability is evaluated
**Then** the action may open its dedicated complete preview flow
**And** Story 4.1 itself dispatches no command and shows no submitted, accepted, confirmed, or audit state without an actual attempt.

**Given** grant is otherwise eligible
**When** its entry point renders
**Then** it clearly names platform-wide global-administrator authority and accepts no tenant context
**And** Story 4.1 does not infer whether an entered target already holds authority or reveal that fact before the authorized command/read flow resolves it.

**Given** the surface is mobile or any layout cannot preserve fixed scope, current freshness, complete-count context, target identity, preview/reason content, and lifecycle safety
**When** mutation availability is evaluated
**Then** grant and removal remain unavailable with the visible `high-impact flow not ready` reason while authorized read-only review remains usable
**And** no high-impact action is moved into an unlabeled overflow or gesture-only control.

**Given** keyboard, screen-reader, forced-colors, high-contrast, reduced-motion, desktop, tablet, or mobile use
**When** actions and unavailable reasons render
**Then** platform scope, target relationship, freshness, current count evidence, visible hover-independent reasons, focus order, and recovery are programmatically associated and keyboard reachable
**And** availability and last-administrator risk never depend on tooltip, color, icon, position, or disabled-control semantics alone.

**Given** English and French cultures
**When** action labels, platform-scope copy, unavailable reasons, last-administrator protection, and recoveries render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting
**And** stable selectors identify grant, each remove slot, aggregate scope, freshness, count completeness, lock, reasons, and recoveries without depending on administrator text or color.

**Given** historical Stories 4.1 and 4.2 implementation evidence
**When** this corrected Story 4.1 guardrail is verified
**Then** the existing fixed-scope read/navigation and action-availability patterns are retained where they satisfy the current contract, while Story 1.11 remains the sole current read-story owner
**And** historical completion does not waive direct-read freshness, complete-count safety, aggregate locking, authorization isolation, support-safety, accessibility, or current evidence requirements.

**Given** the completed fixed-scope availability guardrail
**When** focused fixed-routing, authorization, freshness, incomplete paging/count, last-administrator, command support, aggregate-lock, target visibility, responsive, localization, accessibility, support-safety, and component tests run
**Then** eligible and blocked grant/removal combinations pass with visible canonical reasons and no accidental submission path
**And** no test enables mutation from tenant scope, client-only authority, partial administrator data, stale provenance, or unsafe viewport context.

### Story 4.2: Grant Global Administrator with Projection Confirmation

As an authorized global administrator,
I want to grant platform-wide authority directly to another user,
So that the authority change is explicit, fixed-scope, and confirmed by the authoritative projection.

**Acceptance Criteria:**

**Given** Story 4.1 reports grant eligible on the authorized review surface
**When** the grant form renders
**Then** Fluent controls collect a literal caller-supplied UserId and clearly name the platform-wide `global-administrators` authority being granted
**And** no tenant id, tenant role, tenant membership, invitation, directory autocomplete, generated CRUD form, or raw interactive control is presented as part of the command.

**Given** UserId is empty or whitespace
**When** validation runs
**Then** localized field guidance blocks submission and associates the error with the input
**And** a valid meaningful identifier remains case-sensitive and is never generated, normalized, parsed, or reformatted as a GUID or ULID.

**Given** authorization, direct-read freshness, fixed-scope command support, authoritative re-query support, preview readiness, aggregate admission, or viewport safety is stale, missing, unknown, or indeterminate
**When** grant availability is evaluated
**Then** submission fails closed with the applicable canonical localized inline reason and named recovery
**And** server-side API/domain authorization remains the enforcement boundary without revealing hidden administrator data.

**Given** grant eligibility gates pass for a literal target UserId
**When** the BFF assembles the grant consequence preview
**Then** it supplies all ten platform-governance items: fixed platform scope, target UserId, current complete administrator count, resulting count impact, the specific platform authority being granted, authoritative freshness, the recovery path (deliberate removal as the forward correction), the audit expectation, caller/target platform context, and known consequences versus known unknowns
**And** the redacted support-safe preview introduces no new backend endpoint and blocks confirmation while any required item is missing.

**Given** the complete high-impact grant preview is open
**When** the user reviews, cancels, presses Escape, or explicitly confirms
**Then** focus is trapped while open, cancel and Escape dispatch nothing, deliberate confirmation friction is required before dispatch, and focus returns to the launching control
**And** grant is never a primary/casual or bulk action and remains unavailable on layouts that cannot preserve the full safety context.

**Given** the complete preview remains current and deliberate confirmation succeeds
**When** the server-side gateway dispatches the command
**Then** it sends `SetGlobalAdministrator(UserId)` through fixed `POST /api/v1/commands` with tenant `system`, domain `global-administrators`, aggregate id `global-administrators`, and a client-generated ULID `messageId` idempotency key
**And** the payload contains no tenant context and no new endpoint, browser backend call, or reshaped command contract is introduced.

**Given** the fixed global-administrator aggregate accepts a grant attempt
**When** its command lock is active
**Then** all grant and removal actions for that aggregate in the interactive circuit remain unavailable through terminal evidence
**And** unrelated tenant aggregates may proceed while bulk grant, multi-target submission, concurrent platform-authority commands, and toast batching remain prohibited.

**Given** the target already holds global-administrator authority
**When** the backend returns `GlobalAdministratorAlreadyExists`
**Then** the lifecycle shows a safe localized rejected state with refresh or inspect-current-authority recovery
**And** it is never represented as a NoOp, `already applied`, duplicate confirmation, successful grant, or tenant-membership outcome.

**Given** the caller lacks current global-administrator authority, authorization changes during submission, or another domain rejection occurs
**When** the BFF maps the response
**Then** `InsufficientPermissions` or the applicable platform-governance rejection is shown safely while last-confirmed rows remain unchanged
**And** the response does not reveal hidden administrator identities, count, policy details, raw Problem Details, payloads, claims, tokens, correlations, metadata, or stack traces.

**Given** the grant command is accepted
**When** status polling or SignalR produces a nudge
**Then** submitted, accepted, and projection-pending remain distinct while the BFF re-queries `GET /api/global-administrators` in the fixed authorized scope
**And** status completion, SignalR, submitted UserId, or an optimistic row cannot confirm the grant.

**Given** authoritative global-administrator data is re-queried after submission
**When** grant reconciliation evaluates the result
**Then** `confirmed` requires the target UserId to appear plus fixed-projection version advancement or safe command-specific audit provenance beyond the pre-submit baseline
**And** unrelated row/count changes cannot confirm, a pre-existing/concurrent target without qualifying provenance cannot become success, and unavailable evidence becomes `unable to verify`.

**Given** projection confirmation and audit evidence become available independently
**When** the inline lifecycle panel updates
**Then** accepted, projection-pending, confirmed, rejected, audit-pending, audit-delayed, audit-unavailable, and missing-support states remain distinct with applicable recovery
**And** the story fabricates neither an Audit Evidence Receipt nor `audit available` without an authorized evidence source.

**Given** refresh, rerender, circuit reconnect, retry, or duplicate interaction occurs while the grant is in flight
**When** state is restored
**Then** the flow preserves the literal target, last-confirmed rows, attempt identity, fixed-aggregate lock, and honest lifecycle without double-dispatching
**And** stale intent never inserts or overwrites authoritative administrator data.

**Given** validation failure, rejection, timeout, transport failure, authorization loss, degradation, or unable-to-verify occurs
**When** recovery is presented
**Then** the flow offers the applicable refresh, wait, retry status lookup, continue read-only, request permission, inspect audit, deliberate new attempt, or escalation path
**And** it never promises tenant-role changes, invitation completion, undo, rollback, hidden editing, or automatic grant replay.

**Given** the user cancels, submission fails, permission is lost, or terminal evidence is reached
**When** focus and announcements are handled
**Then** focus returns to the grant launcher or moves to the relevant invalid/failure region without becoming stranded
**And** routine progress uses polite intent, blockers/rejections/failures use appropriate assertive intent, and success is announced only after qualified projection confirmation.

**Given** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, high-contrast, and reduced-motion use
**When** form, lifecycle, and recovery render
**Then** supported layouts preserve target, fixed platform scope, freshness, focus order, aggregate lock, no-color-only feedback, and stable reading context while mobile/unsafe widths fail closed visibly
**And** no target identity or authority state is conveyed solely through truncation, color, icon, placement, or animation.

**Given** English and French cultures
**When** labels, validation, platform-scope copy, rejection, lifecycle, unavailable-reason, and recovery copy render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting
**And** stable selectors identify entry, UserId, submit/cancel, fixed scope, lock, lifecycle states, and recoveries without depending on target text or color.

**Given** historical Story 4.3 implementation evidence
**When** this corrected Story 4.2 contract is verified
**Then** the existing grant gateway, form, lifecycle model, direct fixed-scope re-query, localization, and tests are retained where they satisfy the current contract
**And** historical completion does not waive fixed routing, aggregate locking, duplicate-rejection semantics, provenance-qualified confirmation, reconnect, support-safety, accessibility, or current evidence requirements.

**Given** the completed global-administrator grant flow
**When** focused fixed-payload, literal-identity, validation, authorization, complete-preview, confirmation-friction, existing-target rejection, idempotency, aggregate-lock, lifecycle, projection-provenance, audit handoff, reconnect, localization, accessibility, responsive, support-safety, and E2E tests run
**Then** grant, safe refusal, duplicate, degraded, and unconfirmable scenarios pass with exact commands/results recorded
**And** no test accepts tenant context, command acceptance, optimistic insertion, a pre-existing target, or unrelated projection activity as grant success.

### Story 4.3: Remove Global Administrator with Last-Administrator Hard Stop

As an authorized global administrator,
I want to remove another user's platform authority only when the last-administrator invariant remains safe,
So that governance access is withdrawn deliberately without ever leaving the platform with no global administrator.

**Acceptance Criteria:**

**Given** the authorized fixed-scope review has current complete administrator data
**When** removal availability is evaluated for a visible target
**Then** it uses authoritative freshness, target presence, complete current count, caller authority, fixed-scope command support, preview readiness, aggregate admission, and viewport safety
**And** any stale, unknown, incomplete, paged-without-total, unauthorized, or indeterminate input fails closed with a canonical localized inline reason.

**Given** authoritative complete data proves exactly one global administrator remains
**When** removal availability is calculated for that target
**Then** removal is unavailable with the safe last-global-administrator reason before any preview or confirmation can open
**And** it is never represented as elevated friction, override, warning-only, `already applied`, or a disabled-looking but callable action.

**Given** more than one administrator is authoritatively proven and the visible target is eligible
**When** the BFF assembles the removal consequence preview
**Then** it supplies all ten platform-governance items: fixed platform scope, target UserId, current complete administrator count, resulting count/last-administrator impact, specific authority removed, authoritative freshness, recovery path, audit expectation, caller/target platform context, and known consequences versus known unknowns
**And** the redacted support-safe preview introduces no new backend endpoint and blocks confirmation if any required item is missing.

**Given** the removal target is also the current caller
**When** the complete preview renders
**Then** it explicitly identifies self-removal and the potential loss of future platform-governance access as a known consequence
**And** it does not claim immediate session/token invalidation, tenant-membership loss, or other runtime effects that are not proven.

**Given** the complete high-impact preview is open
**When** the user reviews, cancels, presses Escape, or explicitly confirms
**Then** focus is trapped while open, cancel and Escape dispatch nothing, exact typed target identity or approved operation phrase supplies deliberate confirmation friction, and focus returns to the launching row control
**And** removal is never a primary/casual or bulk action and remains unavailable on layouts that cannot preserve the full safety context.

**Given** preview data remains current, more than one administrator remains, and confirmation succeeds
**When** the server-side gateway submits
**Then** it dispatches `RemoveGlobalAdministrator(UserId)` through fixed `POST /api/v1/commands` with tenant `system`, domain `global-administrators`, aggregate id `global-administrators`, and the retained ULID attempt id
**And** the payload contains no tenant context while the fixed-aggregate lock blocks every grant/removal action through terminal evidence.

**Given** a race leaves the target as the last global administrator before domain handling
**When** `LastGlobalAdministrator` is returned
**Then** the lifecycle shows a safe localized hard-blocked rejection with refresh/continue-read-only recovery and leaves last-confirmed rows unchanged
**And** it never offers override, elevated retry, tenant-member removal, success, NoOp, or `already applied` semantics.

**Given** the target is no longer a global administrator when the command is handled
**When** `GlobalAdministratorNotFound` is returned
**Then** the lifecycle shows a safe localized rejected state with refresh or inspect-current-authority recovery
**And** pre-existing absence is never represented as successful removal, a NoOp, duplicate confirmation, or proof that this attempt changed authority.

**Given** caller authority is lost or another platform-governance rejection occurs
**When** the BFF maps the result
**Then** `InsufficientPermissions` or the applicable safe rejection is shown without disclosing hidden administrator identities, count, policy internals, or authority facts
**And** raw Problem Details, payloads, claims, tokens, correlations, metadata, ETags, cursors, stack traces, or PII are not disclosed.

**Given** the removal command is accepted
**When** status polling or SignalR produces a nudge
**Then** submitted, accepted, and projection-pending remain distinct while the BFF re-queries `GET /api/global-administrators` in the fixed authorized scope
**And** status completion, SignalR, removal intent, or optimistic row deletion cannot confirm removal.

**Given** authoritative global-administrator data is re-queried after submission
**When** removal reconciliation evaluates the result
**Then** `confirmed` requires the target UserId to be absent plus fixed-projection version advancement or safe command-specific audit provenance beyond the pre-submit baseline that proved the target present
**And** unrelated row/count changes cannot confirm, absence without qualifying provenance becomes `unable to verify`, and no unrelated administrator is removed.

**Given** projection confirmation and audit evidence become available independently
**When** the inline lifecycle panel updates
**Then** accepted, projection-pending, confirmed, rejected, audit-pending, audit-delayed, audit-unavailable, and missing-support states remain distinct with applicable recovery
**And** the story fabricates neither an Audit Evidence Receipt nor `audit available` without an authorized evidence source.

**Given** refresh, rerender, circuit reconnect, retry, or duplicate interaction occurs during preview or command tracking
**When** state is restored
**Then** the flow preserves the support-safe target, preview/count baseline, last-confirmed rows, attempt identity, fixed-aggregate lock, and honest lifecycle without double-dispatching
**And** stale preview data blocks confirmation and never deletes or overwrites authoritative rows.

**Given** validation failure, hard-stop rejection, target-not-found, timeout, transport failure, authorization loss, degradation, or unable-to-verify occurs
**When** recovery is presented
**Then** the flow offers the applicable refresh, wait, retry status lookup, continue read-only, request permission, inspect audit, deliberate new attempt where safe, or escalation path
**And** it never promises undo, rollback, hidden editing, history deletion, tenant-role restoration, or automatic authority recreation.

**Given** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, high-contrast, and reduced-motion use
**When** entry, preview, confirmation, lifecycle, and recovery render
**Then** supported layouts preserve fixed scope, target, complete count, all ten preview items, reading order, focus, modal semantics, lock state, no-color-only feedback, and dedicated live-region intent while mobile/unsafe widths fail closed visibly
**And** routine progress is polite, last-admin blockers/rejections/failures are appropriately assertive, and success is announced only after qualified projection confirmation.

**Given** English and French cultures
**When** preview, hard-stop, confirmation, rejection, lifecycle, unavailable-reason, and recovery copy render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting
**And** stable selectors identify each row action, all ten preview items, confirm/cancel, fixed scope, count, lock, lifecycle states, and recoveries without depending on target text or color.

**Given** historical Story 4.4 implementation evidence
**When** this corrected Story 4.3 contract is verified
**Then** the existing remove gateway, hard-stop availability, preview, lifecycle model, direct fixed-scope re-query, localization, and tests are retained where they satisfy the current contract
**And** historical completion does not waive complete-count protection, complete preview, fixed-aggregate locking, rejection semantics, provenance-qualified confirmation, reconnect, support-safety, accessibility, or current evidence requirements.

**Given** the completed global-administrator removal flow
**When** focused complete-count, last-admin pre-block/race, self-removal, preview completeness, target-not-found, fixed-payload, authorization, idempotency, aggregate-lock, lifecycle, projection-provenance, audit handoff, reconnect, localization, accessibility, responsive, support-safety, and E2E tests run
**Then** eligible removal, hard stop, safe refusal, duplicate, degraded, and unconfirmable scenarios pass with exact commands/results recorded
**And** no test accepts partial administrator data, elevated last-admin friction, tenant scope, command acceptance, pre-existing absence, optimistic deletion, or unrelated projection activity as removal success.

## Epic 5: Audit Evidence and Corrective Recovery

Users can inspect contextual audit evidence, understand proof availability, and correct mistakes forward through linked compensating commands.

### Story 5.1: Browse Tenant Audit Trail

As an authorized Tenants user,
I want to browse a tenant's audit trail as a flat, filtered, cursor-paginated list,
So that I can inspect recorded activity without relying on hidden event or command payloads.

**Acceptance Criteria:**

**Given** an authorized user opens `/tenants/{TenantId}/audit` with explicit tenant scope
**When** the audit list loads
**Then** the InteractiveServer BFF queries fixed `GET /api/tenants/{tenantId}/audit` through the direct Tenants read client and renders the approved flat DataGrid fallback
**And** no browser backend call, backend token storage, generic EventStore query route, new audit endpoint, global audit inventory, or separate shell navigation entry is introduced.

**Given** authorized audit entries are returned
**When** rows render
**Then** they are stably ordered by the authoritative timestamp and tie-breaker contract and cursor-paginated without client-side resorting that changes server order
**And** the grid preserves tenant scope, absolute timestamp, actor, target, `AuditEventCategory`, outcome, projection marker/freshness, and support-safe reference context.

**Given** the user sets an absolute from/to date or category filter
**When** `Access`, `Administrative`, or the unfiltered category is applied
**Then** the BFF restarts at page 1 using the authorized server filter contract and clears incompatible cursor history
**And** invalid date ranges or category values receive localized validation without issuing an unsafe or ambiguous query.

**Given** cursor paging is used
**When** next, previous, refresh, filter change, caller change, tenant change, or cursor invalidation occurs
**Then** cursors remain opaque, protected, authenticated-scope/filter-bound, absent from visible copy/logs/telemetry/selectors, and never converted to offset/limit
**And** an invalidated or rejected cursor restarts honestly at page 1 with a localized list-refreshed notice rather than a generic failure or silent data jump.

**Given** the audit result is loading, empty, filtered-empty, error, stale, degraded, unauthorized, invalid-cursor, or unavailable
**When** the list state renders
**Then** every applicable state remains distinct, accessible, localized, and paired with reset, refresh, continue-read-only, request-permission, or escalation recovery
**And** none is styled, copied, or announced as success or fabricated evidence.

**Given** a stale or degraded refresh occurs for the same authorized tenant/filter/cursor scope
**When** last-confirmed rows remain safe to show
**Then** those rows stay visually distinct from refresh/error state with authoritative freshness/projection provenance
**And** rows are never reused across a different tenant, caller, date range, category, or incompatible cursor scope.

**Given** an audit row contains structured `NarrativePayload` and reference fields
**When** the BFF maps the row model
**Then** it emits only the approved support-safe actor, target, tenant, category, outcome, absolute timestamp, projection marker, and reference presentation fields
**And** raw `NarrativePayload`, event bodies, command payloads, tokens, internal correlations, protected cursors, ETags, raw metadata, stack traces, decoded claims, or unapproved PII never reach rendered/copied/announced/logged component output.

**Given** the flat audit grid is Story 5.1's scope
**When** a row is displayed
**Then** it provides sufficient safe context for later receipt and correction entry stories without claiming that a row itself is an Audit Evidence Receipt
**And** Story 5.1 submits no correction command, fabricates no proof, and does not expose later Story 5.2–5.7 actions prematurely.

**Given** the Product/Operations audit-performance decision record is not approved
**When** Story 5.1 readiness is evaluated
**Then** the story remains not Ready and makes no numeric render, interaction, percentile, throughput, or event-count performance claim
**And** historical measurements or typical tenant-list budgets cannot substitute for the missing decision.

**Given** Product/Operations prepares the audit-performance decision
**When** it is approved
**Then** it names the representative dataset shape, page size and filter mix, reference environment/network assumptions, initial-render and interaction percentile budgets, authoritative test tier, repeatability method, and fallback trigger
**And** the approved record—not an inferred implementation target—becomes the acceptance authority for performance evidence.

**Given** approved representative-load testing runs
**When** the flat grid meets or misses the approved contract
**Then** exact commands, environment, dataset, repetitions, percentiles, and results are recorded, and a miss activates the approved stricter-page-size or virtualization fallback
**And** fallback preserves stable order, cursor truth, safety-critical fields, accessibility, and support safety without building a generic `<AuditTimeline>` inside Tenants.

**Given** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, high-contrast, and reduced-motion use
**When** filters, grid, paging, states, references, and return navigation render
**Then** timestamp, actor, outcome, category, freshness/projection context, and reference remain available through pinning, stable widths, wrapping, or safe horizontal overflow with complete table semantics and visible focus
**And** the read-only mobile audit reference never hides critical context or introduces high-impact correction controls.

**Given** English and French cultures
**When** title, filters, columns, list states, validation, timestamps, paging, invalidation notices, and recoveries render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware absolute timestamp formatting
**And** stable selectors identify route scope, filters, grid, rows, safety-critical fields, states, paging, refresh, and recoveries without depending on audit text or color.

**Given** historical Story 5.1 implementation evidence
**When** this corrected Story 5.1 contract is verified
**Then** the existing tenant audit page, flat grid, cursor/filter state, localization, support-safe mapping, and tests are retained where they satisfy the current contract
**And** historical completion does not waive direct REST routing, authoritative provenance, corrected performance governance, accessibility, responsive safety, or current evidence requirements.

**Given** the completed tenant audit trail slice and an approved performance decision
**When** focused direct-read, authorization, stable-order, date/category-filter, opaque-cursor, invalidation, state, support-safety, performance-contract, fallback, localization, accessibility, responsive, and E2E tests run
**Then** authorized, empty, filtered-empty, stale, degraded, unauthorized, invalid-cursor, error, and representative-load scenarios pass with exact evidence recorded
**And** no test accepts a raw payload, offset paging, scope-leaking cursor, fabricated proof, unapproved numeric budget, or unsafe responsive omission.

### Story 5.2: Reach Scoped Audit Evidence from Context

As an authorized user,
I want to reach audit evidence from the tenant, user, and command context where I am working,
So that proof remains connected to the activity I need to understand rather than hidden in a separate tool.

**Acceptance Criteria:**

**Given** an authorized user views a tenant row, tenant detail, user-membership lookup result, member row, or command lifecycle result
**When** the current context has safe tenant scope and audit capability
**Then** a contextual audit entry point is available for the relevant tenant plus safe user or command context where supported
**And** Audit remains a contextual/module-internal route with no separate shell navigation entry or unscoped global audit inventory.

**Given** an entry point is considered
**When** tenant scope, authorization reflection, audit-read support, or safe context is missing, stale beyond safe use, unknown, or indeterminate
**Then** the entry point fails closed with a visible localized inline reason and refresh, continue-read-only, request-permission, or escalation recovery
**And** no dead link, hidden error, route-discovery signal, or scope-leaking query is produced.

**Given** the user opens audit from a tenant-list row
**When** `/tenants/{TenantId}/audit` loads
**Then** the audit route uses that authorized literal tenant scope and the return context preserves workspace tab/scope, search, status filter, sort, direction, cursor, selection, scroll, and launching focus anchor where applicable
**And** the row component performs no audit query itself and does not disturb existing detail/copy/pending/freshness behavior.

**Given** the user opens audit from tenant detail
**When** the audit page loads and later returns
**Then** tenant identity, safe detail return URL, originating focus control, and detail context are preserved
**And** audit data still comes only from the Story 5.1 direct tenant-audit read path.

**Given** the user opens audit from a user-membership lookup or member row
**When** context is carried to the audit page
**Then** the authorized tenant scope and support-safe target UserId context are identified without promoting Users to shell navigation or conflating the target with a global administrator
**And** lookup query, sort, cursor/page context, selected row, return URL, and focus anchor are restored where applicable.

**Given** the backend audit contract supports tenant/date/category filtering but not a complete user-specific filter
**When** user context is shown
**Then** it is labeled as a contextual target hint and never as an exhaustive server-filtered result
**And** client-side filtering of one page is not used to imply that no other matching evidence exists.

**Given** a command lifecycle result has a safe tenant and command context
**When** audit evidence is pending, delayed, unavailable, unsupported, or available
**Then** the entry point and adjacent copy show the actual canonical audit state and route only through an authorized support-safe reference when one exists
**And** command acceptance, status completion, projection confirmation, SignalR, message id, or internal correlation never fabricates command-specific proof.

**Given** no approved support-safe command reference exists
**When** a command-result audit entry renders
**Then** it may link only to the tenant-scoped audit list with an honest context banner and availability state
**And** it does not place raw MessageId, CorrelationId, payload, EventStore metadata, cursor, ETag, token, or internal identifier into URLs, DOM, copy, logs, or announcements.

**Given** optional source, target-user, command-reference, return, or focus context is accepted by the audit route
**When** routing values are parsed
**Then** literal identifiers are correctly URI-encoded, safe return URLs remain under approved Tenants routes, and invalid/external/protocol-relative values fail safe
**And** changing tenant/date/category context resets incompatible cursor history while display-only hints do not alter unsupported backend semantics.

**Given** the user navigates back from audit
**When** the origin can be restored
**Then** focus returns to the launching control and prior list/detail/lookup/command-panel state is preserved
**And** when exact restoration is impossible, focus moves to the origin heading with a visible localized return-context notice rather than disappearing or selecting a different row.

**Given** Story 5.2 entry points render in dense grids, row actions, headers, or command panels
**When** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, high-contrast, or reduced-motion users operate them
**Then** each has a complete accessible name, visible focus, stable layout, sufficient touch target, hover-independent unavailable reason, and no-color-only availability/audit state
**And** contextual links do not displace safety-critical row fields or become gesture-only overflow actions.

**Given** English and French cultures
**When** entry labels, source-context banners, audit states, unavailable reasons, return notices, and recoveries render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting
**And** stable selectors identify each source kind, route scope, context banner, audit state, return/focus data, and unavailable recovery without depending on identity text or color.

**Given** Story 5.2 is limited to audit reachability
**When** contextual entry points are complete
**Then** they reuse the Story 5.1 grid and preserve Story 5.3 receipt, Story 5.4 recovery-detail, and Stories 5.5–5.7 correction boundaries
**And** no entry point edits history, submits a command, fabricates a receipt, or claims audit evidence is available before the evidence source proves it.

**Given** historical Story 5.2 implementation evidence
**When** this corrected Story 5.2 contract is verified
**Then** existing scoped entry components, context routing, return restoration, command audit handoffs, localization, and tests are retained where they satisfy the current contract
**And** obsolete primary Audit navigation behavior is removed or kept unavailable in favor of the current contextual-route IA without weakening authorization or support safety.

**Given** the completed scoped audit entry slice
**When** focused tenant-row, tenant-detail, user-lookup, member-row, command-result, authorization, scope, unsupported-filter honesty, safe-routing, return-context, localization, accessibility, responsive, support-safety, and E2E tests run
**Then** every required source reaches the correct authorized audit context or a visible safe unavailable state
**And** no test accepts unscoped navigation, false exhaustive filtering, unsafe URL context, command acceptance as proof, or lost return focus.

### Story 5.3: View a Support-Safe Audit Evidence Receipt

As an authorized user,
I want to view a support-safe receipt for a recorded action,
So that I can cite what happened without exposing raw evidence data or fabricating proof.

**Acceptance Criteria:**

**Given** an authorized audit row has complete structured evidence
**When** the user opens its receipt
**Then** the receipt presents exactly the required support-safe facts: actor, target, tenant scope, outcome, absolute timestamp, projection marker, and audit/command reference
**And** every label/value uses Tenants-owned localized copy, support-safe formatting, and a semantic field/value relationship.

**Given** the server-side BFF receives an authorized `TenantAuditEntry`
**When** it assembles the receipt view model
**Then** actor derives from the approved actor field, target resolves from structured narrative `userId` then `key` then `TenantId`, scope derives from tenant scope, outcome derives from the structured event/category result, and timestamp/projection/reference derive from approved read provenance
**And** the target precedence is deterministic, explicitly allow-listed, and covered without interpreting arbitrary narrative keys.

**Given** receipt derivation uses structured `NarrativePayload`
**When** the BFF finishes mapping and redaction
**Then** rendered component inputs contain only the seven approved presentation fields plus typed availability/copy eligibility state
**And** raw `NarrativePayload`, dictionary dumps, event bodies, command payloads, tokens, internal correlations, MessageIds, ETags, protected cursors, raw metadata, stack traces, decoded claims, or unapproved PII never enter rendered/copied/announced/logged component state.

**Given** a receipt is requested from a selected audit row or support-safe receipt reference
**When** the matching authorized row is loaded in the current tenant-scoped result
**Then** the receipt opens from that BFF-built safe view model while preserving Story 5.2 source and return context
**And** no new receipt endpoint, direct state-store/event read, browser backend request, or tenant/global scope substitution is introduced.

**Given** a requested reference is absent from the current authorized loaded result
**When** receipt resolution runs
**Then** the surface shows an honest inspect-audit/unavailable state with refresh or paging recovery
**And** it does not scan hidden pages client-side, fabricate a receipt, reveal whether an unauthorized event exists, or query a separate evidence source.

**Given** evidence is partial, audit-pending, audit-delayed, audit-unavailable, missing implementation support, stale, degraded, unauthorized, invalid-cursor, or errored
**When** receipt state renders
**Then** the actual typed state remains distinct with applicable wait, refresh, retry, inspect-audit, continue-read-only, request-permission, or escalation recovery
**And** no incomplete state receives proof wording, success styling, a fabricated timestamp/reference, or an enabled correction action.

**Given** command status, projection confirmation, or SignalR context is present without an authorized matching audit row
**When** receipt availability is evaluated
**Then** those signals may invite refresh or inspect audit but cannot produce `audit available` or a receipt
**And** accepted/confirmed command state remains separate from audit evidence state.

**Given** receipt actor, target, scope, or references contain identifiers
**When** they display
**Then** complete literal values appear only within authorized receipt context with truncation-safe accessible presentation and classifier-approved copy controls
**And** identifier text is never parsed as GUID/ULID, used as a selector, or exposed beyond its authorized scope.

**Given** the user copies receipt evidence
**When** the support-safe classifier evaluates the requested copy
**Then** only approved safe fields/references are included in a bounded receipt summary and unsafe/empty fields are omitted or block copy with explicit feedback
**And** no raw narrative, payload, token, internal correlation, MessageId, ETag, cursor, metadata, stack trace, decoded claim, or unsafe PII is silently copied.

**Given** an available receipt displays its time and projection marker
**When** formatting runs
**Then** the timestamp is absolute and culture-aware with an unambiguous UTC/offset representation, and the projection marker is a support-safe provenance/freshness presentation rather than a raw ETag or aggregate-local sequence
**And** relative-only time, server-local conversion, or `ServedAt` is not substituted for evidence time/projection truth.

**Given** the receipt is opened, closed, unavailable, or copy-blocked
**When** keyboard, screen-reader, forced-colors, high-contrast, reduced-motion, desktop, tablet, or mobile users interact
**Then** focus order/return, headings, semantic field pairs, visible focus, no-color-only state, complete safety fields, and dedicated live-region intent are preserved or the receipt fails closed visibly
**And** routine open/close/copy success is polite while blocking/unavailable/unsafe-copy failures are appropriately assertive.

**Given** English and French cultures
**When** receipt fields, states, absolute timestamps, copy feedback, unavailable reasons, and recovery copy render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting
**And** stable selectors identify receipt, each of the seven fields, state, copy, close, and recovery without depending on evidence text or color.

**Given** Story 5.3 is limited to evidence presentation
**When** an available receipt is rendered
**Then** it can serve as the evidence source for later correction stories while retaining immutable original-event context
**And** it does not start correction, submit commands, edit history, link corrective proof, or introduce grouped timeline/analytics scope.

**Given** historical Story 5.3 implementation evidence
**When** this corrected Story 5.3 contract is verified
**Then** the existing receipt selection, safe field derivation, copy classifier, localization, accessibility, and tests are retained where they satisfy the current contract
**And** any raw narrative parsing in rendered component state is moved behind the server-side BFF safe-view-model boundary without weakening target precedence or evidence availability honesty.

**Given** the completed Audit Evidence Receipt slice
**When** focused seven-field derivation, target precedence, BFF redaction, loaded-reference authorization, partial-state, safe-copy, timestamp/projection, context preservation, localization, accessibility, responsive, support-safety, and E2E tests run
**Then** available and unavailable receipt scenarios pass with exact safe evidence recorded
**And** no test accepts raw `NarrativePayload`, fabricated proof, command confirmation as audit evidence, unsafe copy, or a receipt from an unauthorized/unloaded row.

### Story 5.4: Understand Audit Availability and Recovery

As an authorized user,
I want incomplete or unavailable audit evidence states to be explicit,
So that I know whether to wait, retry, inspect audit, continue read-only, or escalate without mistaking uncertainty for proof.

**Acceptance Criteria:**

**Given** audit evidence is not available as a complete authorized receipt
**When** its availability is evaluated
**Then** the typed model distinguishes `audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support`
**And** `audit available` remains a separate proven state while none of the four incomplete/unavailable states receives success styling, copy, announcement, or correction eligibility.

**Given** evidence is expected but has not arrived
**When** `audit pending` renders
**Then** the surface explains that proof is still expected and offers `wait`, refresh/status retry, or `inspect audit` when an authorized scoped path exists
**And** routine pending updates use polite announcement intent without claiming delay, failure, or proof.

**Given** the evidence capability exists but is taking longer than its defined state transition permits
**When** `audit delayed` renders
**Then** the surface explains the delay and offers `wait`, retry status/refresh, `inspect audit`, or `escalate` with a support-safe reference as appropriate
**And** delay is distinct from an unavailable path, missing implementation, projection lag, or command failure.

**Given** the authorized evidence path exists but is currently inaccessible or fails
**When** `audit unavailable` renders
**Then** the surface explains the runtime unavailability and offers retry, `continue read-only`, `inspect audit` when another authorized path remains, or `escalate`
**And** it does not imply the capability is unbuilt, the underlying action failed, or evidence never will exist.

**Given** the required evidence capability is not implemented
**When** `missing implementation support` renders
**Then** the surface names the missing capability honestly and offers `continue read-only` or `escalate` with a support-safe reference
**And** it is never mapped to empty audit data, a transient error, retry-as-success, or a fabricated fallback receipt.

**Given** a command is submitted, accepted, projection-pending, projection-confirmed, rejected, failed, degraded, or unable-to-verify
**When** audit availability is derived
**Then** command lifecycle, projection truth, and audit evidence remain separate typed dimensions in state/reducers and last-confirmed receipt data remains separate from in-flight intent
**And** command status, event-count hints, projection confirmation, or SignalR cannot advance audit availability to proven.

**Given** a user invokes wait, retry/status lookup, refresh, inspect audit, continue read-only, or escalate
**When** the recovery executes
**Then** it reuses the existing authorized status/read/context path, preserves scope and return focus, and performs only the named action
**And** it does not submit a correction, redispatch the original command, edit history, add an endpoint, or use prohibited `undo`, `rollback`, or `hidden edit` language.

**Given** recovery produces new authorized audit evidence
**When** the BFF refreshes the state
**Then** transition to `audit available` occurs only from a complete redacted receipt/evidence source and preserves the absolute timestamp/projection marker of that evidence
**And** stale, partial, mismatched-scope, unauthorized, or unrelated audit rows cannot satisfy the transition.

**Given** recovery does not change the evidence state
**When** the user remains pending, delayed, unavailable, or missing-support
**Then** the current state and next valid recovery remain visible without looped success announcements, focus loss, or replacement of last-confirmed projection/receipt data
**And** repeated retry is bounded and never leaks raw diagnostics or internal identifiers.

**Given** the shared availability presentation appears in command panels, audit entry points, audit lists, receipts, or later correction surfaces
**When** it renders
**Then** all surfaces use the same typed state/recovery mapping while retaining their source-specific safe context
**And** no flow-local string or machine enum token changes canonical meaning, casing, or recovery behavior.

**Given** an unavailable or escalation message is assembled
**When** support-safe copy is produced
**Then** it includes only the minimum approved context and classifier-approved reference needed for recovery
**And** raw diagnostics, narrative/payloads, tokens, claims, internal correlations, MessageIds, ETags, cursors, metadata, stack traces, or PII are not rendered, copied, announced, logged, or serialized into component state.

**Given** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, high-contrast, and reduced-motion use
**When** state, explanation, and recovery controls render
**Then** visible text plus icon/shape, accessible labels, focus order/return, stable dimensions, no-color-only meaning, and complete responsive context are preserved
**And** pending/delayed use polite intent while unavailable/missing-support or other blocking failures use appropriate assertive intent without repeated interruption.

**Given** English and French cultures
**When** state labels, explanations, accessible names, recovery verbs, live-region copy, and escalation text render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting
**And** stable selectors identify availability control, typed state, each recovery, reference, and source context without depending on localized text or color.

**Given** Story 5.4 is limited to availability and recovery signaling
**When** the shared contract is applied
**Then** existing command submission, projection confirmation, consequence preview, receipt field derivation, audit paging, and aggregate locking remain unchanged except for consuming the shared typed view model
**And** correction start, correction preview/dispatch, and linked proof remain Stories 5.5–5.7.

**Given** historical Story 5.4 implementation evidence
**When** this corrected Story 5.4 contract is verified
**Then** the existing shared availability model/control, command/receipt integrations, localization, accessibility, and tests are retained where they satisfy the current contract
**And** historical completion does not waive exact state semantics, BFF evidence authority, canonical recovery, non-collapse, support-safety, or current evidence requirements.

**Given** the completed audit availability and recovery slice
**When** focused four-state, available-transition, recovery mapping, command/projection non-collapse, repeated retry, support-safe escalation, cross-surface, localization, accessibility, responsive, and E2E tests run
**Then** every state and transition exposes only valid recovery with exact evidence recorded
**And** no test accepts false success, machine-token leakage, command/projection state as audit proof, missing support as runtime error, or an unavailable state as a correction-ready receipt.

### Story 5.5: Start a Forward Tenant Correction from Audit Evidence

As an authorized tenant operator,
I want to start a forward membership correction from proven audit evidence with a verified current-state intent,
So that a mistaken access change can be corrected forward — submission, confirmation, and linked proof completing in Story 5.6 — without editing or relabeling the original event.

**Acceptance Criteria:**

**Given** an authorized user opens a complete support-safe receipt for a supported tenant-membership outcome
**When** correction eligibility is evaluated
**Then** the surface offers `restore intended access` or `start correction` only when audit evidence, literal tenant/target scope, current authorization, direct projection freshness, current tenant lifecycle, command support, and responsive safety are complete
**And** every label, accessible name, tooltip, announcement, selector context, and recovery avoids `undo`, `rollback`, and `hidden edit` terminology.

**Given** the receipt is pending, delayed, unavailable, missing implementation support, partial, unauthorized, stale, degraded, or lacks a support-safe original reference
**When** correction availability renders
**Then** start remains unavailable with the applicable canonical inline reason and Story 5.4 recovery
**And** an incomplete receipt, command confirmation, projection state, or SignalR nudge cannot become correction authority.

**Given** a correctable membership receipt is selected
**When** the start flow opens
**Then** the BFF authoritatively re-queries the current tenant detail/member projection before preparing an intent and keeps the immutable original receipt visible or directly reachable
**And** no raw event payload, state-store read, historical role assumption, or client-cached membership replaces current projection truth.

**Given** the correction follows a mistaken member removal and the target is currently absent
**When** the operator selects the intended role
**Then** the flow prepares a new `AddUserToTenant(TenantId, UserId, Role)` intent only for explicit `TenantOwner`, `TenantContributor`, or `TenantReader`
**And** it never infers the removed role from history, permits `TenantRole.Unknown`, creates an invitation, or submits automatically.

**Given** the tenant has no remaining membership history and restore access is required
**When** the bootstrap path is prepared
**Then** the flow requires the explicit domain-supported owner role and clearly identifies the recovery/bootstrap context
**And** it does not invent a tenant initialization, invitation, or bypass of server authorization/domain behavior.

**Given** the target is already a current member with a role different from the intended role
**When** correction intent is derived
**Then** the flow prepares an explicit `ChangeUserRole(TenantId, UserId, Role)` path instead of an add command that would reject as `UserAlreadyInTenant`
**And** the operator must review the current role and deliberately confirm the intended role before handing off to preview.

**Given** the target already has the exact intended current role
**When** correction eligibility is reconciled
**Then** the flow reports `already applied` with continue-read-only or inspect-audit recovery and prepares no command intent
**And** it is not represented as newly corrected, projection-confirmed, or audit-proven work.

**Given** the receipt describes a wrong-role outcome and the target is currently present
**When** the operator chooses a valid intended role different from current
**Then** the flow prepares `ChangeUserRole(TenantId, UserId, Role)` with explicit current/intended role context
**And** current-state conflict, escalation risk, or `Unknown` role remains visibly blocked for Story 5.6 preview resolution.

**Given** the target is absent for a role-change correction, the tenant is disabled, lifecycle is unknown, permission is lost, or current projection evidence conflicts with the historical outcome
**When** correction eligibility is evaluated
**Then** the original evidence remains visible while the action fails closed with refresh, choose-supported-path, continue-read-only, request-permission, inspect-audit, or escalation recovery
**And** stale historical intent is never prepared as a submittable command.

**Given** a tenant correction start is eligible
**When** the handoff panel opens
**Then** it carries the support-safe original audit reference and absolute timestamp, literal tenant/target, current projection summary and provenance, selected intended role, derived tenant-domain command type, and required complete-preview inputs
**And** opening the panel dispatches no command, polls no command status, marks no success, and creates no corrective proof.

**Given** audit evidence describes global-administrator authority or another unsupported outcome
**When** Story 5.5 evaluates it
**Then** it routes global-administrator correction ownership to Story 5.7 or shows a localized unsupported/high-impact-not-ready reason
**And** it never maps platform authority into tenant membership or prepares a `global-administrators` command in this story.

**Given** the original and current projection contain identifiers, roles, or references
**When** correction-start state is assembled
**Then** only BFF-redacted support-safe fields enter rendered state and literal TenantId/UserId values remain unparsed
**And** raw narrative, payloads, tokens, claims, internal correlations, MessageIds, ETags, cursors, metadata, stack traces, or unapproved PII are not rendered, copied, announced, logged, or serialized into component state.

**Given** the user opens, changes role selection, cancels, closes, or encounters a blocked start
**When** keyboard, screen-reader, forced-colors, high-contrast, reduced-motion, desktop, tablet, or mobile interaction occurs
**Then** accessible names, labels, role relationships, visible focus, focus return to the receipt/row launcher, stable layout, no-color-only reasons, and complete safety context are preserved
**And** mobile/unsafe widths keep the receipt readable but correction start visibly unavailable.

**Given** English and French cultures
**When** action labels, original/current-state fields, role choices, command/domain copy, unavailable reasons, handoff, and recovery text render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware absolute timestamps
**And** stable selectors identify start, role selection, original reference, current projection, intended command, preview handoff, cancel/close, reasons, and focus return without depending on identity text or color.

**Given** Story 5.5 is a non-submitting correction-start slice
**When** the handoff is complete
**Then** Story 5.6 owns complete preview, command submission, projection confirmation, and linked corrective proof
**And** Story 5.5 never edits/deletes/relabels the original event, modifies projections/state stores directly, or adds correction/preview/proof endpoints.

**Given** historical Story 5.5 implementation evidence
**When** this corrected Story 5.5 contract is verified
**Then** the existing intent mapper, receipt/grid entry, start panel, localization, and tests are retained where they satisfy the current contract
**And** the historical always-blocked live path is corrected by wiring authoritative current membership, explicit role selection, real focus return, localized snapshot text, and already-applied detection before claiming completion.

**Given** the completed tenant correction-start slice
**When** focused receipt eligibility, current-state re-query, restore/add selection, change-role selection, bootstrap owner, already-applied, disabled/conflict, unsupported/global scope, no-submit, immutable-history, localization, accessibility, responsive, support-safety, and E2E tests run
**Then** supported evidence opens a genuinely usable Story 5.6 handoff and every blocked path has explicit recovery
**And** no test accepts inferred role, stale history, global-admin conflation, automatic submission, machine-token copy, or a model-only path unreachable from the live audit surface.

### Story 5.6: Preview, Confirm, and Link a Tenant Correction

As an authorized tenant operator,
I want to preview a membership correction against current state and link its proof to the original evidence,
So that recovery is deliberate, projection-confirmed, and auditable without rewriting history.

**Acceptance Criteria:**

**Given** Story 5.5 has produced an eligible non-submitting tenant correction intent
**When** the correction preview opens
**Then** the BFF presents all ten required items: original audit reference, tenant identity, target UserId, current membership/role, intended forward command/role and access/owner-count impact, authoritative freshness/provenance, authorization/aggregate-admission readiness, recovery path, audit/proof expectation, and known consequences versus known unknowns
**And** any missing, stale, unauthorized, unsafe, unsupported, or conflicting item blocks confirmation with a visible canonical reason.

**Given** preview data is assembled
**When** current projection differs from the historical evidence or Story 5.5 baseline
**Then** the preview explains the current-state conflict and re-derives, blocks, or requires an explicit supported path based on current truth
**And** historical outcome, role, or target state never overrides the authoritative re-query.

**Given** the target is absent for an add/restore intent or present for a role-change intent
**When** preview eligibility is evaluated
**Then** the intended explicit non-Unknown role and expected postcondition remain clear and valid for the selected command
**And** target-present add conflict switches to an explicit role-change path, target-absent role-change blocks, and exact current intended role reports `already applied` without dispatch.

**Given** the tenant is disabled, lifecycle is unknown, permission is lost, escalation is unsafe, the aggregate is locked, or the viewport cannot preserve complete correction context
**When** confirmation availability is calculated
**Then** the flow fails closed with refresh, choose-supported-path, continue read-only, request permission, inspect audit, or escalation recovery
**And** the complete original receipt remains visible or directly reachable.

**Given** the complete preview is displayed
**When** the user reviews, changes the explicit intended role, cancels, presses Escape, or deliberately confirms
**Then** any changed role triggers current-state re-evaluation, cancel/Escape dispatch nothing, focus remains complete and returns to the launching receipt/row, and confirmation cannot occur accidentally
**And** no control or copy calls the correction `undo`, `rollback`, `hidden edit`, or historical replacement.

**Given** preview data remains current and the user confirms an eligible restore or role correction
**When** the server-side gateway submits
**Then** it dispatches the existing `AddUserToTenant(TenantId, UserId, Role)` or `ChangeUserRole(TenantId, UserId, Role)` through fixed `POST /api/v1/commands` with the retained ULID attempt id in the tenant aggregate scope
**And** no correction/preview/proof endpoint, browser backend call, contract reshape, event edit, projection edit, or state-store mutation is introduced.

**Given** the correction command is active
**When** aggregate admission is enforced
**Then** membership, metadata, lifecycle, and configuration commands for that tenant remain unavailable through terminal evidence while unrelated aggregates may proceed
**And** refresh, reconnect, retry, duplicate click, or rerender cannot double-dispatch or create a second logical attempt.

**Given** the domain returns `UserAlreadyInTenant`, `RoleEscalation`, an unchanged-role NoOp, `TenantDisabled`, `InsufficientPermissions`, or another rejection
**When** the BFF maps the result
**Then** the exact support-safe rejection or `already applied` state appears with an appropriate current-state/alternate-path recovery and the original evidence remains unchanged
**And** no rejection/NoOp is styled or announced as a completed correction.

**Given** the forward command is accepted
**When** status polling or SignalR produces a nudge
**Then** submitted, accepted, and projection-pending remain distinct while one authoritative tenant projection refresh is scheduled for that correction refresh cycle
**And** command status, SignalR, correction intent, or optimistic membership changes cannot confirm recovery.

**Given** a correction status refresh requires projection evidence
**When** the page/panel refreshes current state
**Then** exactly one authoritative refreshed tenant projection snapshot is reused for conflict evaluation, expected-postcondition confirmation, and subsequent proof-search eligibility in that cycle
**And** no parent-callback-plus-second-direct-query duplication occurs and deduplication never weakens freshness, failure, or unable-to-verify behavior.

**Given** authoritative tenant projection is re-queried after submission
**When** correction reconciliation evaluates the result
**Then** add/restore confirms only when the target has the explicit intended role, role correction confirms only when the target's role matches, and both require projection-version advancement or safe command-specific audit provenance beyond the pre-submit baseline
**And** a pre-existing intended state becomes `already applied`, unrelated projection changes cannot confirm, and missing provenance becomes `unable to verify`.

**Given** projection confirmation occurs before corrective audit evidence
**When** lifecycle feedback renders
**Then** the correction remains projection-confirmed with `audit pending`, `audit delayed`, `audit unavailable`, or `missing implementation support` shown separately as applicable
**And** the original event remains immutable/visible and audit delay cannot retroactively turn projection truth into failure or full proof.

**Given** the existing authorized tenant-audit path returns a potential corrective row
**When** proof association is evaluated
**Then** the row must match the correction attempt through safe command-specific provenance plus expected event, tenant, target, and post-submit baseline constraints
**And** matching only target, role, event type, timestamp proximity, list position, or unrelated projection activity is insufficient.

**Given** deterministic corrective audit association is proven
**When** the original or corrective receipt is viewed
**Then** the BFF assembles a paired support-safe proof view exposing bidirectional navigation between original and corrective audit references with absolute timestamps and projection markers
**And** the UI link does not mutate either audit record and uses only redacted structured narrative/approved command-audit references.

**Given** deterministic association cannot be re-derived from existing authorized evidence
**When** proof link availability is evaluated
**Then** the flow shows the actual pending, delayed, unavailable, or missing-support state with retry, inspect-audit, continue-read-only, or escalation recovery
**And** it never persists or fabricates a bidirectional relationship from in-memory target/time coincidence alone.

**Given** correction state survives refresh, rerender, or circuit reconnect
**When** the panel is reconstructed
**Then** support-safe original reference, preview baseline, explicit role, attempt identity, aggregate lock, last-confirmed projection, lifecycle, and proven link state are restored without rearming a terminal submit
**And** raw tracking identifiers, payloads, narrative, tokens, claims, ETags, cursors, metadata, or stack traces are not rendered, copied, announced, logged, or serialized into unsafe component output.

**Given** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, high-contrast, and reduced-motion use
**When** preview, confirmation, lifecycle, proof states, links, and recovery render
**Then** supported layouts preserve all ten preview items, original evidence access, focus trap/return, lifecycle-region terminal focus, no-color-only meaning, absolute timestamps, and dedicated live-region intent while mobile/unsafe widths fail closed visibly
**And** progress/confirmation is polite while conflicts, rejection, failure, degraded, and unable-to-verify are appropriately assertive.

**Given** English and French cultures
**When** preview, role choice, consequences, conflicts, confirmation, lifecycle, proof link/state, absolute timestamps, and recovery copy render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting
**And** stable selectors identify preview, all ten items, role, confirm/cancel, lock, lifecycle, original/corrective links, audit state, and recoveries without depending on identity text or color.

**Given** historical Stories 5.6 and 5.8 implementation evidence
**When** this corrected Story 5.6 contract is verified
**Then** the existing preview/confirmation snapshot, membership gateway reuse, projection truth gate, proof-link UI, single-refresh provider, focus handling, localization, and tests are retained where they satisfy the current contract
**And** historical completion does not waive complete preview, provenance-qualified confirmation/linking, terminal-state reconnect safety, support-safety, accessibility, or current evidence requirements.

**Given** the completed tenant correction slice
**When** focused preview-completeness, current-state conflict, add/role selection, already-applied/rejection, idempotency, aggregate-lock, single authoritative refresh, projection-provenance, deterministic proof association, bidirectional receipt navigation, reconnect, immutable-history, localization, accessibility, responsive, support-safety, and E2E tests run
**Then** eligible correction, safe refusal, duplicate, degraded, unconfirmable, audit-delayed, and linked-proof scenarios pass with exact commands/results recorded
**And** no test accepts stale history, partial preview, command acceptance, optimistic state, duplicate projection queries, target/time coincidence, or in-memory-only association as completed linked correction proof.

### Story 5.7: Correct Global Administrator Authority from Audit Evidence

As an authorized global administrator,
I want to issue a fixed-scope forward correction from proven platform-authority evidence,
So that mistaken global-administrator changes can be corrected safely without treating them as tenant membership or rewriting history.

**Acceptance Criteria:**

**Given** an authorized operator opens complete support-safe evidence for `GlobalAdministratorRemoved` or `GlobalAdministratorSet`
**When** global-administrator correction eligibility is evaluated
**Then** removal evidence maps only to `SetGlobalAdministrator(UserId)` and grant evidence maps only to `RemoveGlobalAdministrator(UserId)` in fixed tenant `system`, domain `global-administrators`, aggregate `global-administrators`
**And** no caller-selected tenant, tenant detail, tenant membership, tenant role, owner-count rule, or tenant-domain command participates in this correction.

**Given** system-scope evidence is pending, delayed, unavailable, missing implementation support, partial, unauthorized, stale, degraded, unrecognized, or lacks a support-safe original reference
**When** correction availability renders
**Then** start remains unavailable with the applicable canonical reason and Story 5.4 recovery while the immutable original evidence remains visible or directly reachable
**And** claims alone, command status, tenant-domain projection, SignalR, or historical target state cannot become platform-correction authority.

**Given** a supported system-scope receipt is selected
**When** correction preparation begins
**Then** the BFF re-queries the complete current fixed global-administrator projection, following its authorized opaque cursor until target presence and administrator count are authoritative, before assembling any preview
**And** an incomplete page set, stale/non-current freshness, read failure, or uncertain count fails closed rather than inferring absence or last-administrator safety from the first page.

**Given** the fixed projection and required evidence are current and complete
**When** the correction preview opens
**Then** it presents all ten required items: original audit reference, fixed platform scope, target UserId, current target presence, complete current administrator count plus resulting authority/count effect, authoritative freshness/provenance, authorization/fixed-aggregate admission readiness, recovery path, audit/proof expectation, and known consequences versus known unknowns
**And** any missing, unsafe, unsupported, conflicting, or unverifiable item blocks confirmation with a visible canonical reason.

**Given** the intended command is `SetGlobalAdministrator` and the target is already present, or the intended command is `RemoveGlobalAdministrator` and the target is already absent
**When** current-state reconciliation completes before submission
**Then** the flow reports `already applied`, offers continue-read-only or inspect-audit recovery, and dispatches no command
**And** it does not label the historical event corrected, projection-confirmed by this attempt, or linked to new corrective proof.

**Given** the intended correction removes a currently present global administrator
**When** the complete current projection contains one administrator or the complete count cannot be established
**Then** confirmation is unavailable before submission with an explicit last-administrator or unable-to-verify reason
**And** no override, elevated-friction bypass, self-removal exception, tenant-membership fallback, or completable destructive confirmation is offered.

**Given** the target is the current operator and removal would leave at least one other proven global administrator
**When** self-removal consequences render
**Then** the preview explicitly names the loss-of-authority risk and safe continuation/recovery without promising immediate token, session, or claim invalidation
**And** the same fixed-scope authorization, complete-count, confirmation, and proof rules apply without a hidden bypass.

**Given** the complete preview is current and the operator cancels, presses Escape, or deliberately confirms
**When** the interaction completes
**Then** cancel/Escape dispatch nothing, focus returns to the launching receipt/row, and only explicit confirmation can submit the displayed fixed-scope command
**And** no control or copy calls the action `undo`, `rollback`, `hidden edit`, historical repair, or replacement.

**Given** the operator confirms an eligible platform-authority correction
**When** the server-side gateway submits it
**Then** it uses the existing `SetGlobalAdministrator(UserId)` or `RemoveGlobalAdministrator(UserId)` command through fixed `POST /api/v1/commands` routing with one retained ULID attempt id and the `system` / `global-administrators` / `global-administrators` aggregate identity
**And** no correction, receipt, proof-link, direct-projection, tenant-member, or browser-to-backend endpoint is added and no event/projection/state-store record is mutated.

**Given** a global-administrator command or correction is active
**When** fixed-aggregate admission is enforced
**Then** every grant, removal, and corrective command for the global-administrator aggregate remains unavailable through terminal evidence while unrelated tenant aggregates may proceed
**And** refresh, reconnect, retry, duplicate click, rerender, or a second audit receipt cannot create another logical attempt or release the lock early.

**Given** `GlobalAdministratorAlreadyExists` or `GlobalAdministratorNotFound` is returned after a concurrent change
**When** the BFF reconciles the attempt
**Then** the submitted command remains a support-safe rejected result while separately presenting the newly queried current state and valid inspect-audit or retry-from-current-state recovery
**And** raced domain rejection is never rewritten as NoOp, `already applied`, projection-confirmed correction, or successful proof for this attempt.

**Given** `LastGlobalAdministrator` is returned after a corrective removal races with another authority change
**When** the result renders
**Then** it remains a hard-blocked platform-governance rejection, the last-confirmed projection and original evidence remain visible, and current fixed projection is re-queried
**And** it is never downgraded to friction, made overridable, presented as tenant-role guidance, or announced as successful removal.

**Given** the command is submitted, accepted, stored, published, completed, rejected, failed, degraded, or unable-to-verify
**When** lifecycle feedback updates
**Then** submitted, accepted, projection-pending, projection-confirmed, rejected, failed, degraded, and unable-to-verify remain distinct typed states with exact support-safe recovery
**And** command status, event-count hints, SignalR, optimistic authority state, or HTTP success cannot prove projection truth or corrective audit evidence.

**Given** a correction status refresh requires projection evidence
**When** the page/panel refreshes platform authority
**Then** exactly one complete authoritative fixed-projection snapshot is reused for current conflict evaluation, expected-postcondition confirmation, last-administrator/count safety, and subsequent proof-search eligibility in that refresh cycle
**And** no parent-callback-plus-second-direct-query duplication occurs and deduplication never weakens cursor completeness, freshness, failure, or unable-to-verify behavior.

**Given** the fixed projection is authoritatively re-queried after submission
**When** correction confirmation is evaluated
**Then** `SetGlobalAdministrator` confirms only when the target is present and `RemoveGlobalAdministrator` confirms only when the target is absent, with fixed-projection version advancement or safe attempt-specific command/audit provenance beyond the pre-submit baseline
**And** a pre-existing expected state, unrelated administrator change, incomplete paging, or target presence/absence alone cannot confirm the attempt and missing provenance becomes unable to verify.

**Given** projection confirmation occurs before corrective audit evidence
**When** the lifecycle renders
**Then** the correction remains projection-confirmed while `audit pending`, `audit delayed`, `audit unavailable`, or `missing implementation support` is shown separately as applicable
**And** delayed audit neither converts projection confirmation into full proof nor retroactively marks the immutable original event repaired in place.

**Given** the existing authorized system-scope audit path returns a possible corrective record
**When** proof association is evaluated
**Then** a restore requires `GlobalAdministratorSet` and a corrective removal requires `GlobalAdministratorRemoved`, matched through safe attempt-specific provenance plus fixed scope, target UserId, original/post-submit timestamp boundary, and pre-submit projection baseline
**And** target, event type, timestamp proximity, first-page position, current presence/absence, or another administrator's projection activity alone is insufficient.

**Given** deterministic corrective association is proven from existing authorized evidence
**When** the original or corrective receipt is viewed
**Then** the BFF assembles a paired support-safe proof view with bidirectional navigation between original and corrective audit references, absolute timestamps, and approved projection markers
**And** neither record is mutated and rendered components never receive raw narrative/payloads, tokens, decoded claims, EventStore metadata, internal correlations, MessageIds, cursors, stack traces, or unapproved PII.

**Given** deterministic association cannot be re-derived
**When** proof-link availability is evaluated
**Then** the actual pending, delayed, unavailable, or missing-support state remains visible with retry, inspect-audit, continue-read-only, or escalate recovery
**And** no bidirectional relationship is fabricated, persisted, or restored from in-memory target/time coincidence.

**Given** correction state survives refresh, rerender, paging, or circuit reconnect
**When** the platform-correction panel is reconstructed
**Then** the support-safe original reference, fixed scope, complete preview baseline, attempt identity, aggregate lock, last-confirmed complete projection, lifecycle, and proven link state are restored without rearming a terminal submit
**And** stale cached authority, partial pages, raw tracking values, or unsafe component state cannot replace a fresh BFF-owned re-query.

**Given** desktop, tablet, mobile, keyboard, screen-reader, forced-colors, high-contrast, and reduced-motion use
**When** platform correction preview, confirmation, lifecycle, proof, links, and recovery render
**Then** supported layouts preserve all ten preview items, original evidence access, complete-or-exit keyboard behavior, focus return, lifecycle-region terminal focus, no-color-only meaning, absolute timestamps, and dedicated live-region intent while unsafe widths fail closed visibly
**And** progress/confirmation is polite while last-admin blocks, conflicts, rejection, failure, degraded, and unable-to-verify states are appropriately assertive without repeated interruption.

**Given** English and French cultures
**When** fixed-scope labels, preview items, consequences, confirmation, lifecycle, last-admin/rejection copy, audit proof, timestamps, and recoveries render
**Then** Tenants-owned whole-string resources remain parity-checked with named placeholders and culture-aware formatting using platform-governance language
**And** stable selectors identify start, all ten preview items, confirm/cancel, aggregate lock, lifecycle, original/corrective links, audit state, recoveries, and focus return without depending on identity text, localized text, or color.

**Given** Story 5.7 composes completed Epic 4 authority commands with Epic 5 audit/correction foundations
**When** the fixed-scope slice is implemented in one development session
**Then** it reuses Story 4.1 availability, Stories 4.2–4.3 grant/remove command and projection behavior, Stories 5.1–5.4 evidence/recovery, and Story 5.6's proof plus folded historical Story 5.8 single-refresh foundations
**And** it introduces no generic recovery API, new authority semantics, tenant-domain coupling, or second lifecycle/proof architecture.

**Given** historical Stories 5.7 and 5.8 implementation evidence
**When** this corrected Story 5.7 contract is verified
**Then** the existing global-administrator intent mapping, fixed gateway routing, correction snapshot/panel, complete projection handling, last-admin block, single-refresh provider, proof-link UI, localization, accessibility, and tests are retained where they satisfy the current contract
**And** historical completion does not waive complete paging, current freshness, ten-item preview, pre-submit already-applied semantics, raced-rejection truth, provenance-qualified confirmation/linking, reconnect safety, or immutable history.

**Given** the completed global-administrator correction slice
**When** focused fixed-scope mapping, eligibility, complete-projection paging, ten-item preview, restore/remove, already-applied, self-removal, last-admin pre-block/race, fixed-aggregate lock, duplicate/reconnect, single-refresh, projection-provenance, deterministic proof association, bidirectional receipt navigation, audit-delay, localization, accessibility, responsive, support-safety, and E2E tests run
**Then** eligible correction, safe refusal, concurrent duplicate/absence, last-admin race, degraded, unconfirmable, audit-delayed, and linked-proof scenarios pass with exact commands/results recorded
**And** no test accepts tenant-role coupling, partial paging, stale evidence, incomplete preview, command acceptance, optimistic authority, duplicate projection queries, target/time coincidence, false rejection success, or in-memory-only association as completed linked correction proof.
