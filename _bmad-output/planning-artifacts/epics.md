---
stepsCompleted: [1]
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md
---

# Hexalith.Tenants - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Hexalith.Tenants, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Requirements Inventory

### Functional Requirements

FR1: A platform operator can browse and triage the tenant list by scanning, searching, filtering, sorting, and cursor-paging tenants; each row must show tenant identity, status, member count, owner count, pending state, and a Truth State Badge with freshness; loading, empty, filtered-empty, error, stale, and degraded states must remain distinct; sorting or paging must never hide pending or stale markers; results must remain authorization-safe.

FR2: A user can open a tenant detail surface from the list and return with prior filter, sort, selection, and context preserved; tenant detail must also support deep links.

FR3: A signed-in user can self-audit "My Tenants" by viewing the tenants they belong to and their role in each; only memberships the caller is authorized to see are shown, with role and tenant status per row.

FR4: An operator can look up a user's memberships by search and can reach a user membership view from a member row; results are authorization-scoped, and no visible memberships must render as an explicit empty state rather than an error.

FR5: A user can view a tenant overview containing status, metadata, member summaries, configuration summaries, member counts, owner counts, lifecycle status, and a freshness indicator, with lifecycle/status represented using no-color-only encoding.

FR6: A user can view tenant configuration key/values in read-only mode, grouped by namespace and filtered to namespaces they own or are authorized for; values outside the caller's prefix are not shown, and sensitive-value display is outside the read MVP.

FR7: A user can copy a full identifier or support-safe reference even when visually truncated; copied identifiers are literal caller-supplied strings, not assumed ULIDs or GUIDs, and copied values must never expose payloads, tokens, internal correlation ids, raw metadata, stack traces, or PII.

FR8: A user can review a tenant member table showing each member's role, owner count, status, freshness, and orphan or disabled context; the table is read-only, must not imply mutation, and must expose accessible table semantics including headers, sort state, and row relationships.

FR9: A user can see which member actions would be available and, when an action is unavailable, a plain-language inline Unavailable Action Reason using the six canonical categories; reasons are not hover-only.

FR10: An authorized user can add a user directly to a tenant by caller-supplied user id with an explicit role; there is no invitation or pending-member step; adding an existing member is rejected as `UserAlreadyInTenant` and surfaced as safe localized text, not as a NoOp.

FR11: An authorized user can change a tenant member's role; changing to the current role is a NoOp shown as `already applied`; role escalation and `TenantRole.Unknown` targets are rejected with safe localized text; success is shown only after projection confirmation.

FR12: An authorized user can remove a user from a tenant through fail-closed gating, Consequence Preview, elevated-friction handling, command lifecycle tracking, projection confirmation, and audit proof; last-owner removal is allowed with extra friction, target-also-global-admin raises platform friction, duplicate or already-applied removal is deduplicated, and unverifiable outcomes are never shown as success.

FR13: An authorized operator can create a tenant; creating an existing tenant id is rejected as `TenantAlreadyExists`, and success is shown only after projection confirmation.

FR14: An authorized tenant contributor or global administrator can edit tenant metadata; every successful edit emits `TenantUpdated` with no same-state suppression, and validation errors surface as safe localized field messages.

FR15: An authorized global administrator can disable or enable a tenant through a high-impact flow with Consequence Preview; setting a tenant to an already-set lifecycle state is rejected as `TenantLifecycleStateAlreadySet`, disabled status is represented as an eventually-consistent availability signal, commands targeting a disabled tenant are rejected as `TenantDisabled`, and success is shown only after projection confirmation.

FR16: An authorized user can set a namespaced tenant configuration key/value; an identical key/value is a NoOp shown as `already applied`, values over domain limits are rejected as `ConfigurationLimitExceeded`, and the scope of Consequence Preview for configuration edits must be resolved.

FR17: An authorized user can remove a tenant configuration key; removing a missing key is rejected as `ConfigurationKeyNotFound`, and success is shown only after projection confirmation.

FR18: An authorized operator can review global administrators separately from tenant membership; the surface is hidden from tenant owners, reads the fixed-identity `global-administrators` aggregate, and shows identity plus freshness.

FR19: An authorized operator can grant or remove a global administrator except the last one; removing the last global administrator is rejected as `LastGlobalAdministrator` and reflected as unavailable, not as completable friction; global administrator operations stay in the `global-administrators` scope and are never conflated with tenant membership.

FR20: A user can browse a tenant audit trail as a flat, stably ordered, cursor-paginated list with date and `AuditEventCategory` filters; loading, empty, filtered-empty, and error states are distinct and accessible; the surface targets about 500 events without unacceptable degradation.

FR21: A user can reach audit evidence from navigation, tenant row, tenant detail, user lookup, and command result entry points, each scoped to the relevant tenant, user, or command.

FR22: A user can view a support-safe Audit Evidence Receipt for a recorded action, including actor, target, tenant scope, outcome, absolute timestamp, projection marker, and audit or command reference; it is assembled from structured `NarrativePayload`, never from raw event payloads, and partial completion shows the actual lifecycle state rather than fabricated proof.

FR23: A user can distinguish `audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support`; none is shown as success, and each offers an appropriate retry, wait, continue-read-only, inspect-audit, or escalate path.

FR24: From audit evidence, an authorized user can start a compensating command such as "restore intended access" or "start correction"; the correction is a new forward command with its own preview and proof, never called "undo", and the original event remains untouched.

FR25: A user can preview a correction against current state and link the original and corrective records; success is shown only after projection confirmation.

### NonFunctional Requirements

NFR1: Reads must use cursor pagination and conditional requests so unchanged data is cheap; freshness is surfaced, not hidden; tenant list/detail/member surfaces target interactive rendering within about one second on a warm projection, and the audit view targets about 500 events without unacceptable latency.

NFR2: Authorization is server-enforced at the API and domain layers; the UI reflects authorization but never enforces it, must stay safe if it misjudges authorization, and role-scoped reads must prevent owners from seeing other tenants or global administrator surfaces.

NFR3: The UI must be correct under eventual consistency, at-least-once delivery, projection lag, and reconnects; the projection is the source of truth, commands and audit facts are confirmed by re-querying, and the UI never resurrects or displays optimistic success.

NFR4: Every interactive element and status must carry a stable automation selector or component contract, never keyed on row text or color, so acceptance and E2E tests are robust.

NFR5: The UI must never edit, delete, or rewrite events, projections, or state to fix data; corrections are forward compensating commands only.

NFR6: Accessibility must meet WCAG 2.1 AA, target WCAG 2.2 AA where the pinned Fluent UI Blazor and FrontComposer stack supports it, and include keyboard-only operation, complete-or-exit behavior, visible focus, focus return, modal focus traps, screen-reader semantics, no-color-only status, reduced-motion support, forced-colors support, and live-region correctness.

NFR7: Localization must use culture-aware, whole-string resources with named placeholders for state labels, role names, timestamps, warnings, unavailable reasons, recovery actions, confirmations, empty/loading/error/degraded/stale/unavailable copy, and rejection text; runtime sentence-fragment assembly is forbidden.

NFR8: Support-safety and privacy are hard requirements: no surface, copy action, log, receipt, toast, or error may expose bearer tokens, decoded JWT contents, command payloads, serialized event bodies, raw EventStore metadata, internal correlation ids, stack traces, or real PII.

NFR9: Responsive behavior is desktop-first but must preserve safety at tablet and mobile widths; safety-critical columns never drop, high-impact command flows are unavailable on mobile, and widths that cannot preserve full safety context fail closed with a visible reason.

NFR10: A UI story cannot be marked ready until applicable accessibility, localization, responsive, and documentation/reference evidence is cited, or a Product/UX-approved fallback records keyboard/focus/live-region behavior, copy ownership, documentation evidence, replacement path, and owner approval.

### Additional Requirements

- Starter requirement: create a new `src/Hexalith.Tenants.UI` Blazor web host as the first implementation story. No UI host exists today; `frontcomposer` is an inspect/migrate tool, not a scaffolder. The host uses .NET 10, `Microsoft.NET.Sdk.Web`, Blazor InteractiveServer, and is added to `Hexalith.Tenants.slnx`.
- Compose the UI through the Hexalith.FrontComposer Shell and Fluent UI Blazor v5. `FC-LYT` must be confirmed before read MVP build-start; `FC-CMD` and `FC-CNC` must be confirmed or covered by approved policy before command flows.
- Use the architecture's InteractiveServer plus server-side BFF model. The browser must never call the backend directly and must never hold backend access tokens.
- Wire the UI host through the existing `Hexalith.Tenants.AppHost`, Keycloak/JWT configuration, service references, and SignalR client. In local dev, `EnableKeycloak=false` uses the existing symmetric-key JWT path.
- The UI host ships as a container image using .NET SDK container support with `ContainerRepository=tenants-ui`; do not add Dockerfiles.
- Consume existing backend APIs only: `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, `GET /api/tenants/{tenantId}/audit`, `POST /api/v1/commands`, and `GET /api/v1/commands/status/{correlationId}`. Do not add backend endpoints for previews, receipts, or command status.
- Confirm the deployed command route (`/api/v1/commands` vs `/api/commands` alias) before command stories.
- Bind to `Hexalith.Tenants.Client` and `Hexalith.Tenants.Contracts` DTOs and enums; never redeclare DTOs, re-case wire fields, or parse `TenantId`/`UserId` as ULIDs or GUIDs.
- Implement a typed BFF query gateway for the five read endpoints and a command gateway for command submission and status lookup. All external backend egress goes through these gateways.
- Implement the single command confirmation pattern: submit command with a client-generated `messageId` ULID idempotency key; run command-status polling and SignalR nudge handling concurrently; trigger authoritative projection re-query; mark `confirmed` only from the re-queried projection.
- Treat SignalR projection notifications as freshness nudges only. They may prompt re-query but never advance command lifecycle or audit availability by themselves.
- Implement one shared Fluxor TruthState feature and a casing-faithful canonical `Vocabulary/` library for all truth, freshness, command lifecycle, layered feedback, unavailable reason, audit availability, and recovery verb tokens.
- Enforce the non-collapse invariant in state and reducers: `accepted`, `confirmed`, and `audit available` are distinct; `degraded` and `unable to verify` are success-prohibited; in-flight intent never overwrites last-confirmed projection data.
- Implement server-side conditional freshness reads (`If-None-Match` to `304`) and configurable freshness thresholds; unmeasurable freshness becomes `unknown` and fails closed.
- Keep cursors opaque, signed, scope-bound, and server-held; on invalidation re-query page 1 and show an honest list-refreshed notice rather than treating invalidation as a generic error.
- Implement a server-side authorization reflection service that maps claims and projection facts to action availability plus the six Unavailable Action Reason categories; indeterminate authorization fails closed.
- Implement support-safety services server-side: redaction, NarrativePayload receipt assembly, consequence-preview assembly, and rejection-code to localized safe-text mapping.
- Use Tenants-owned `.resx` resources for domain UI copy, with dotted PascalCase keys under a `Tenants.` root; inherit only shell chrome strings from FrontComposer shell resources.
- Use stable automation selectors in the form `data-testid="tenants-{surface}-{element}"` for all interactive controls and statuses.
- Organize the new UI project by surface (`Components/Tenants`, `Components/Users`, `Components/GlobalAdministrators`, `Components/Audit`, `Components/Shared`), with `State/`, `Services/`, `Vocabulary/`, `Resources/`, and `wwwroot/css/` as described by architecture.
- Add a separate `tests/Hexalith.Tenants.UI.Tests` project for bUnit/xUnit v3/Shouldly tests and a Playwright E2E tier for the required acceptance scenarios. Follow Tenants test conventions: plural test class files, Shouldly assertions, per-project `dotnet test`.
- Product/UX-approved fallbacks are available for `FC-AUD` as flat audit DataGrid, `FC-CNS` as inline consequence text, and `FC-CNC` as one-at-a-time command policy. Missing shared UI capabilities still belong in FrontComposer, not Tenants.
- Build sequencing is externally gated: bootstrap first, then read surfaces after `FC-LYT`, then first commands after `FC-CMD` plus one-at-a-time policy, then high-impact/audit/recovery on approved fallbacks and remaining contract confirmations.

### UX Design Requirements

UX-DR1: Use Microsoft Fluent UI Blazor v5 through the FrontComposer shell as the visual authority; do not invent a bespoke palette, hard-code hex colors, or assert unverified Fluent token names.

UX-DR2: Map status meaning only to verified Fluent `BadgeColor` semantic roles: Success, Informative, Warning, Severe, Danger, Important, Subtle, and Brand. Brand is chrome/primary-action accent only and is never a status.

UX-DR3: Reserve Success exclusively for projection-proven truth (`current`, `confirmed`, `audit available`, tenant active). In-flight, accepted, submitted, previewed, projection pending, audit pending, and refreshing states are Informative, never Success.

UX-DR4: Preserve the three-tier caution ramp: Warning for usable-with-friction states, Severe for blocking non-error states, and Danger for refusal, failure, or destructive/high-impact moments.

UX-DR5: Render every status as badge color plus icon plus visible text/accessibility label. Color is never the sole signal, and forced-colors users must retain the meaning from icon and text.

UX-DR6: Implement `TruthStateBadge` as a reusable component using the verified Size20 Fluent icon names from DESIGN.md, including distinct glyphs for clock-related states, audit states, rejection/failure, blocked authorization, and unable-to-verify.

UX-DR7: Inherit Fluent typography and shapes; use modest headings, compact body/label/caption roles, and monospace rendering for TenantId, UserId, support-safe references, and absolute timestamps.

UX-DR8: Use full-width operational surfaces with constrained inner regions for forms, previews, panels, and dialogs; follow the 4px spacing rhythm (`4/8/12/16/24/32px`) and compact density.

UX-DR9: Reserve stable cell/action/status footprints so badges, action availability, longer localized labels, and state changes do not shift row layout.

UX-DR10: Pin safety-critical columns (`identity`, `status`, `freshness`, `role`, and `risk` where shown) with `DataGridColumnPin.Start`; horizontal scroll or column priority must never hide safety context.

UX-DR11: Implement the ten domain components named in the UX specs: `TruthStateBadge`, `ConsequencePreview`, `CommandLifecyclePanel`, `UnavailableActionReason`, `AuditEvidenceReceipt`, `TenantDataGrid`, `MemberTable`, `AuditDataGrid`, `PrimaryCommandButton`, and `DestructiveControl`.

UX-DR12: `ConsequencePreview` must use the approved inline structured-text fallback until `FC-CNS` exists, carry the full 10-item content set, separate known consequences from known unknowns, and block submission when any required item is unavailable.

UX-DR13: `CommandLifecyclePanel` must be inline and anchored to the affected row or panel, display each lifecycle step distinctly, never overwrite confirmed projection data, and never advance from SignalR alone.

UX-DR14: `UnavailableActionReason` must render one of the six canonical categories as inline visible localized text, programmatically associated with the relevant row/action, keyboard/screen-reader reachable, and stable in layout.

UX-DR15: `AuditEvidenceReceipt` must render support-safe actor, target, tenant scope, outcome, absolute timestamp, projection marker, and reference fields; unavailable or partial evidence shows the actual audit availability state, never fabricated proof.

UX-DR16: `TenantDataGrid` must use cursor pagination, required tenant columns, six non-collapsible list states, pending/stale visibility under sort/page, authorization-safe empty states, and a refresh path for stale state.

UX-DR17: `MemberTable` must remain read-only, expose accessible table semantics, show member identity/role/owner count/status/freshness/orphan context, and reserve a per-row action or reason slot.

UX-DR18: `AuditDataGrid` must be flat, stably ordered, cursor-paginated, filtered by date and `AuditEventCategory`, and able to render an honest MVP not-yet-available placeholder.

UX-DR19: `DestructiveControl` must never read as a primary or casual action; destructive/high-impact flows require Consequence Preview plus confirmation, focus trap, safe non-committing escape, and elevated friction for zero-owner or target-also-global-admin cases.

UX-DR20: Operations Shell IA must use primary navigation in this order: Tenants, Global Administrators, Audit. Users is contextual from a member row and global search, not a co-equal nav tab. Command lifecycle is never navigation.

UX-DR21: Preserve selection, filters, and scroll across navigation, especially tenant list to detail and back.

UX-DR22: Microcopy must be calm, precise, honest, and shared across operators and owners. The words `undo`, `rollback`, and `hidden edit` are prohibited in labels, tooltips, announcements, and copy.

UX-DR23: Use the canonical state sets and recovery verbs verbatim and casing-significantly, including the distinction between badge space-form tokens (`audit pending`) and state-machine snake_case tokens (`audit_pending`).

UX-DR24: Treat risk as derived rather than stored: `high` when an action would drop owner count to zero or the target also holds global-administrator authority, `low` otherwise.

UX-DR25: Enforce keyboard-first behavior: every workflow is keyboard-operable; modals/previews trap focus; Escape/cancel does not commit; focus returns to the launching control on close, cancel, submit, or failure.

UX-DR26: Enforce fail-closed gating order before preview opens: validation plus freshness plus authorization must all be eligible; missing lifecycle support, stale or unknown freshness, indeterminate authorization, or incomplete preview blocks the action with inline reason.

UX-DR27: Enforce the one-at-a-time command fallback: no concurrent command submission, toast batching, multi-row bulk action, or optimistic success in v1; command triggers are unavailable with a stated reason while a command is in flight.

UX-DR28: Implement live-region politeness from a dedicated announcement-intent field, not from color or MessageBar intent. Assertive announcements are reserved for rejection, failure, unable-to-verify, degraded, or destructive-block; success is never announced before projection truth.

UX-DR29: Provide absolute, culture-aware timestamps for freshness and audit times; relative-only timestamps are not acceptable.

UX-DR30: Responsive behavior must follow the specified breakpoints: mobile 320-767px read-only triage/lookup/audit reference, tablet 768-1023px with collapsed nav and stacked regions, desktop 1024px+ as the primary dense workstation, wide desktop 1440px+ with more horizontal room.

UX-DR31: If a viewport cannot preserve full safety context for a high-impact action, that action becomes unavailable with a visible reason instead of rendering unsafely.

UX-DR32: Keep RTL direction support layout-ready through logical start/end patterns, while RTL verification remains deferred unless promoted into v1 scope.

UX-DR33: Ready-gate evidence must cover stale projection, rejected command, unknown confirmation, audit unavailable, last-owner warning, permission-missing, keyboard-only complete-or-exit, consequence-preview/destructive-confirmation focus behavior, forced-colors rendering, and live-region politeness.

### FR Coverage Map

{{requirements_coverage_map}}

## Epic List

{{epics_list}}

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
