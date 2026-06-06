---
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05.md
  - _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-03.md
  - _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-02.md
  - _bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md
  - _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-03.md
  - _bmad-output/planning-artifacts/architecture.md
  - _bmad-output/planning-artifacts/epics.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/.decision-log.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-frontcomposer-depmap.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-phase-2-backlog.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-remove-user-journey.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-a11y-l10n.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-audit-recovery.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-operations-shell.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-responsive-visual.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-truth-state.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/review-adversarial.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/review-domain-fidelity.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/review-downstream-readiness.md
  - _bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/review-rubric.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/.decision-log.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/.working/prd-ux-digest.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/review-accessibility.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/review-rubric.md
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/mockups/mock-tenant-list.html
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/mockups/mock-command-lifecycle.html
  - _bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/mockups/mock-consequence-preview.html
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

FR16: An authorized user can set a namespaced tenant configuration key/value; an identical key/value is a NoOp shown as `already applied`, values over domain limits are rejected as `ConfigurationLimitExceeded`, and configuration edits require Consequence Preview unless Product/UX later narrows the scope.

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
- Include a Story 1.0 shell-integration spike before bootstrap implementation. The spike is complete as of 2026-06-05 and verifies `AddHexalithFrontComposer*` registration APIs, manifest registration, projection routing, and FrontComposer readiness gates; see `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`.
- Compose the UI through the Hexalith.FrontComposer Shell and Fluent UI Blazor v5. `FC-LYT`, `FC-CMD`, and `FC-CNC` are confirmed by Story 1.0; tenant-list implementation still requires the Story 1.2 `FC-TBL` decision.
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
- Implement a receipt source contract from `TenantAuditEntry`: actor from `ActorId`, target from `NarrativePayload.userId` then `key` then `TenantId`, scope from `TenantId`, outcome from event type and `AuditEventCategory`, timestamp from the audit entry, projection marker from the freshness marker, and reference from the audit or command reference. `NarrativePayload` is structured narrative metadata, not a raw backend type or raw event body.
- Keep tenant and global-administrator correction paths distinct: tenant membership recovery uses tenant-domain commands; global-administrator recovery uses `SetGlobalAdministrator` or `RemoveGlobalAdministrator` in the `global-administrators` domain and does not edit a tenant aggregate.
- Use Tenants-owned `.resx` resources for domain UI copy, with dotted PascalCase keys under a `Tenants.` root; inherit only shell chrome strings from FrontComposer shell resources.
- Use stable automation selectors in the form `data-testid="tenants-{surface}-{element}"` for all interactive controls and statuses.
- Organize the new UI project by surface (`Components/Tenants`, `Components/Users`, `Components/GlobalAdministrators`, `Components/Audit`, `Components/Shared`), with `State/`, `Services/`, `Vocabulary/`, `Resources/`, and `wwwroot/css/` as described by architecture.
- Add a separate `tests/Hexalith.Tenants.UI.Tests` project for bUnit/xUnit v3/Shouldly tests and a Playwright E2E tier for the required acceptance scenarios. Follow Tenants test conventions: plural test class files, Shouldly assertions, per-project `dotnet test`.
- Product/UX-approved fallbacks are available for `FC-AUD` as flat audit DataGrid, `FC-CNS` as inline consequence text, and `FC-CNC` as one-at-a-time command policy. Missing shared UI capabilities still belong in FrontComposer, not Tenants.
- FR15 disable/enable is a reversible lifecycle soft-delete / availability-control operation, not hard destructive tenant deletion, and may proceed under approved command and preview fallbacks once story-specific evidence is satisfied. FR19 remains categorically blocked. Hard destructive tenant deletion is future administrators-only CLI tooling and out of scope for this phase; tenant-scoped destructive flows (`FR12`, `FR16`, `FR17`) remain fallback-eligible.
- Build sequencing is externally gated: shell-integration spike, bootstrap, read surfaces after `FC-LYT`, first commands after `FC-CMD` plus one-at-a-time policy, then high-impact/audit/recovery on approved fallbacks and remaining contract confirmations.

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

FR1: Epic 1 - Tenant workspace triage with searchable, sortable, cursor-paged tenant list and safe freshness/status visibility.
FR2: Epic 1 - Tenant detail navigation with preserved list context and deep-link support.
FR3: Epic 1 - "My Tenants" self-audit membership view.
FR4: Epic 1 - User membership lookup with authorization-scoped results.
FR5: Epic 1 - Tenant overview with status, metadata, counts, lifecycle, and freshness.
FR6: Epic 1 - Read-only namespaced tenant configuration visibility.
FR7: Epic 1 - Support-safe copy behavior for identifiers and references.
FR8: Epic 1 - Read-only tenant member table with accessible table semantics.
FR9: Epic 1 - Action availability and inline unavailable-action reasons.
FR10: Epic 2 - Add user to tenant with explicit role and safe rejection handling.
FR11: Epic 2 - Change tenant member role with NoOp and rejection handling.
FR12: Epic 2 - Remove tenant member with fail-closed gating, preview, lifecycle tracking, and proof.
FR13: Epic 2 - Create tenant with projection-confirmed success.
FR14: Epic 2 - Edit tenant metadata with safe validation handling.
FR15: Epic 3 - Disable or enable tenant with high-impact controls and projection confirmation.
FR16: Epic 3 - Set tenant configuration key/value with preview and NoOp/rejection handling.
FR17: Epic 3 - Remove tenant configuration key with projection-confirmed outcome.
FR18: Epic 4 - Review global administrators separately from tenant membership.
FR19: Epic 4 - Grant or remove global administrators with last-global-admin protection.
FR20: Epic 5 - Browse flat, filtered, cursor-paginated tenant audit trail.
FR21: Epic 5 - Reach audit evidence from navigation, rows, detail, lookup, and command results.
FR22: Epic 5 - View support-safe Audit Evidence Receipt assembled from structured narrative data.
FR23: Epic 5 - Distinguish audit pending, delayed, unavailable, and missing-support states.
FR24: Epic 5 - Start forward compensating commands from audit evidence.
FR25: Epic 5 - Preview correction against current state and link original/corrective records.

## Epic List

### Cross-Cutting Quality Bar

Every epic inherits the shared quality bar from the requirements inventory: projection truth is authoritative, Success is reserved for projection-proven or audit-proven states, command and audit lifecycle states do not collapse into each other, authorization and support-safety are server-enforced, copy is Tenants-owned and localized through `.resx`, accessibility/responsive evidence is required for readiness, and stable `data-testid` selectors are used for all interactive controls and statuses.

### Story Creation Guardrails

Every story created from these epics must make the safety contract explicit in acceptance criteria and test expectations. Each story states the actor and job, names the projection truth source and staleness behavior, names the permission boundary and server-side authorization result, preserves pending/failed/denied/unknown states without false Success, consumes existing backend endpoints without adding local Tenants infrastructure, includes Tenants-owned `.resx` copy, and identifies the required accessibility, responsive, live-region, forced-colors, and stable `data-testid` evidence. Every command story also includes audit/evidence behavior, including delayed or unavailable audit states, and every story includes a test contract naming the fixture, observable state, and automation level such as unit, component, API, or Playwright.

### Command Story Sizing Guardrail

Command stories may remain single stories only when the shared command lifecycle, TruthState/vocabulary, one-at-a-time admission, consequence-preview fallback, BFF command gateway, projection re-query confirmation, localization, and ready-gate evidence patterns already exist. If those foundations are absent, split the command story into:

1. availability and fail-closed preview,
2. submit/status/projection confirmation,
3. audit/evidence handoff or proof.

Do not split platform-wide destructive actions in a way that bypasses their explicit blocked/governance status.

### Epic 1: Tenant Workspace Triage and Read-Only Insight

Users can open the Tenants workspace, browse and inspect tenants, self-audit memberships, look up user memberships, inspect read-only tenant details/configuration/members, and understand action availability without unsafe mutation.

**FRs covered:** FR1, FR2, FR3, FR4, FR5, FR6, FR7, FR8, FR9

**Implementation notes:** Includes the shell-integration spike, new `Hexalith.Tenants.UI` host, FrontComposer/Fluent foundation, BFF read gateway, TruthState/Vocabulary foundation, freshness handling, selectors, accessibility/localization baseline.

### Epic 2: Tenant Membership and Tenant Record Management

Authorized users can create tenants, update tenant metadata, and manage tenant membership with safe command lifecycle tracking and projection-confirmed outcomes.

**FRs covered:** FR10, FR11, FR12, FR13, FR14

**Implementation notes:** Establishes reusable command gateway, one-at-a-time command policy, fail-closed gating, consequence preview fallback, command lifecycle panel, rejection mapping, projection re-query confirmation, and honest audit-proof handoff for later command and audit epics.

### Epic 3: Tenant Lifecycle and Configuration Control

Authorized users can safely control tenant lifecycle and tenant configuration while preserving high-impact safety rules and projection truth.

**FRs covered:** FR15, FR16, FR17

**Implementation notes:** Depends on command confirmation from Epic 2; platform-wide destructive lifecycle work remains gated by FrontComposer/governance confirmation, while tenant-scoped configuration flows are fallback-eligible.

### Epic 4: Global Administrator Governance

Authorized operators can review and manage global administrator authority separately from tenant membership, including last-global-admin safety.

**FRs covered:** FR18, FR19

**Implementation notes:** Keeps `global-administrators` scope distinct from tenant aggregates and treats last-global-admin removal as unavailable, not as completable elevated friction.

### Epic 5: Audit Evidence and Forward Recovery

Users can inspect audit evidence, distinguish incomplete proof states, and launch forward corrective actions with their own preview and projection-confirmed proof.

**FRs covered:** FR20, FR21, FR22, FR23, FR24, FR25

**Implementation notes:** Uses flat audit DataGrid fallback, support-safe receipt assembly from `NarrativePayload`, explicit audit availability states, correction flows that reuse the command confirmation foundation, and forward recovery that never edits history.

## Epic 1: Tenant Workspace Triage and Read-Only Insight

Users can open the Tenants workspace, browse and inspect tenants, self-audit memberships, look up user memberships, inspect read-only tenant details/configuration/members, and understand action availability without unsafe mutation.

### Story 1.0: FrontComposer Shell Integration Spike

**Story type/status:** Timeboxed enabler spike; completed 2026-06-05. Evidence: `_bmad-output/implementation-artifacts/story-1-0-spike-note-2026-06-05.md`. This story closes or records the build-start contract gates; it does not deliver user-facing MVP value.

As a Tenants platform maintainer,
I want to verify the FrontComposer shell integration contracts before building the Tenants UI host,
So that the first UI implementation uses shared Hexalith composition patterns instead of adding generic shell infrastructure inside Hexalith.Tenants.

**Requirements:** Supports FR1-FR9 readiness; Additional requirements for Story 1.0, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-TBL`; NFR2-NFR4, NFR6-NFR10.

**Acceptance Criteria:**

**Given** the Hexalith.FrontComposer source is available as a root-level submodule and the Tenants planning artifacts identify `FC-LYT`, `FC-CMD`, `FC-CNC`, and `FC-TBL` as integration gates
**When** the spike inspects the current FrontComposer shell registration APIs, manifest registration pattern, projection routing, and table/DataGrid contract
**Then** the spike records which APIs and contracts are confirmed for Tenants use, which remain blocked, and which approved fallbacks apply
**And** the spike does not modify shared FrontComposer files unless explicitly approved in a separate task.

**Given** the Tenants UI must compose through FrontComposer and Fluent UI Blazor v5
**When** the spike evaluates `AddHexalithFrontComposer*`, `AddHexalithDomain<TMarker>()`, shell layout composition, and domain manifest expectations
**Then** the spike documents the minimum Tenants bootstrap path for a future `src/Hexalith.Tenants.UI` project
**And** it identifies any missing shared capability as a FrontComposer dependency rather than Tenants-local boilerplate.

**Given** the Tenants UI must consume existing backend APIs through a server-side BFF
**When** the spike reviews route and command-status assumptions including `/api/v1/commands` versus any `/api/commands` alias
**Then** it records the confirmed route contract, any unresolved dependency, and whether command stories may proceed under the approved one-at-a-time fallback
**And** it does not add backend endpoints for previews, receipts, or command status.

**Given** story creation guardrails require implementation-ready evidence
**When** the spike is complete
**Then** the output names the projection truth source, permission boundary assumptions, staleness/freshness implications, localization ownership, accessibility and responsive obligations, and stable selector expectations for later UI stories
**And** it records the current gate verdicts. As of the completed 2026-06-05 spike, `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC` are confirmed; `FC-TBL` is available with caveats that must be resolved before tenant-list implementation.

**Test Contract:**

**Given** this is an investigation story with no product UI behavior yet
**When** the dev agent completes the spike
**Then** verification is by documented evidence in the planning artifact or story notes, plus any compile-safe API probe if a throwaway probe is needed
**And** the story identifies whether later verification belongs to unit, bUnit component, API, or Playwright coverage.

### Story 1.1: Tenants UI Host Bootstrap

As a platform operator,
I want to open the Tenants workspace inside the shared FrontComposer shell,
So that Tenants read workflows start from a real hosted UI surface rather than local scaffolding or mock screens.

**Requirements:** Supports FR1-FR9 foundation; Additional requirements for `src/Hexalith.Tenants.UI`, AppHost wiring, BFF composition, container support; NFR2-NFR4, NFR6-NFR10; UX-DR1, UX-DR20, UX-DR25, UX-DR28, UX-DR30.

**Acceptance Criteria:**

**Given** Story 1.0 has confirmed the minimum FrontComposer shell composition path in `story-1-0-spike-note-2026-06-05.md`
**When** the Tenants UI host is created
**Then** `src/Hexalith.Tenants.UI` exists as a .NET 10 Blazor InteractiveServer web project using `Microsoft.NET.Sdk.Web`
**And** it is added to `Hexalith.Tenants.slnx` without creating a `.sln` file.

**Given** the UI must compose through shared Hexalith shell infrastructure
**When** the application starts
**Then** the root Tenants workspace renders through the FrontComposer shell and Fluent UI Blazor v5 conventions
**And** no generic shell, DI, serialization, or event-store boilerplate is duplicated inside Hexalith.Tenants.

**Given** the browser must not call backend services directly
**When** the UI host is configured
**Then** it contains server-side composition points for future BFF query and command gateways
**And** the browser has no backend access token storage and no direct backend API client.

**Given** the Tenants UI must run in local Aspire development
**When** the existing `Hexalith.Tenants.AppHost` is started after the project is added
**Then** the UI host is registered as a resource with the existing Tenants service dependencies and auth configuration path
**And** `EnableKeycloak=false` remains compatible with the existing symmetric-key JWT development mode.

**Given** the UI host must be package/release compatible
**When** the project is built for container output
**Then** it uses .NET SDK container support with `ContainerRepository=tenants-ui`
**And** no Dockerfile is added.

**Given** no tenant read story has been implemented yet
**When** an authorized or unauthenticated user reaches the Tenants workspace route
**Then** the page shows an honest unavailable/not-yet-connected state rather than mock tenant data or fabricated success
**And** the state uses Tenants-owned `.resx` copy, accessible semantics, visible focus, forced-colors-safe styling, and stable selectors such as `data-testid="tenants-shell-status"`.

**Test Contract:**

**Given** the bootstrap is complete
**When** tests run for the UI project
**Then** component or smoke tests verify shell rendering, the no-mock-data unavailable state, key selectors, and localization resource lookup
**And** the story identifies any Playwright smoke coverage needed to verify the route in Aspire once the UI host is discoverable.

### Story 1.2: Tenant List Triage

As a platform operator,
I want to browse, search, filter, sort, and cursor-page tenants from the Tenants workspace,
So that I can quickly understand tenant state and risk without changing anything.

**Requirements:** FR1; NFR1-NFR4, NFR6-NFR9; UX-DR2-UX-DR6, UX-DR10, UX-DR16, UX-DR23, UX-DR28-UX-DR31.

**Pre-build gate:** Resolve the `FC-TBL` caveat from Story 1.0 before implementation. FrontComposer's generated projection grid does not satisfy Tenants' cursor pagination, safety-column pinning, or six-state list-surface requirements. The default decision is to compose a Tenants-specific `TenantDataGrid` from Fluent/FrontComposer primitives for tenant-specific columns and safety states, while filing any reusable cursor/pinning/list-state capability as a FrontComposer enhancement.

**Acceptance Criteria:**

**Given** the Tenants UI host is available through the FrontComposer shell
**When** an authorized operator opens the Tenants workspace
**Then** the UI queries tenants through a server-side BFF query gateway bound to `Hexalith.Tenants.Client` and `Hexalith.Tenants.Contracts` types
**And** the browser never calls `GET /api/tenants` directly and never stores backend access tokens.

**Given** tenant projection data is available from `GET /api/tenants`
**When** the tenant list renders
**Then** each row shows tenant identity, lifecycle/status, member count, owner count, pending state, and a Truth State Badge with freshness
**And** tenant ids remain caller-supplied strings and are not parsed or displayed as ULIDs or GUIDs.

**Given** the operator searches, filters, sorts, or cursor-pages the tenant list
**When** new results are loaded
**Then** the list preserves distinct pending and stale markers on every affected row
**And** sorting or paging never hides safety-critical columns or truth-state indicators.

**Given** the list is loading, empty, filtered-empty, stale, degraded, or errored
**When** that state occurs
**Then** the UI renders a distinct accessible state with Tenants-owned localized copy
**And** no state is collapsed into generic failure or false Success.

**Given** freshness information is measurable
**When** the BFF receives unchanged tenant data or a freshness marker
**Then** the UI reflects current, stale, degraded, or unknown freshness honestly
**And** conditional read behavior such as `If-None-Match`/`304` is handled server-side without exposing backend mechanics to the browser.

**Given** the caller is not authorized to see some or all tenants
**When** the list result is authorization-scoped
**Then** unauthorized tenants are not rendered
**And** an authorized-empty result is shown as an explicit empty state, not as an error or hidden failure.

**Given** the list is rendered at desktop, tablet, or mobile widths
**When** available width changes
**Then** safety-critical columns and status meaning remain visible or the view fails closed with a visible reason
**And** color is never the only state signal; icon, text, semantic label, forced-colors support, and stable layout are preserved.

**Given** the tenant list contains interactive controls and statuses
**When** bUnit or Playwright tests inspect the UI
**Then** controls and statuses expose stable selectors such as `data-testid="tenants-list-grid"`, `data-testid="tenants-list-refresh"`, and `data-testid="tenants-list-truth-state"`
**And** tests do not depend on row text or color.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit or component tests cover the BFF query gateway mapping, cursor/freshness states, authorization-safe empty states, and no-direct-browser-backend behavior
**And** bUnit or Playwright coverage verifies loading, empty, filtered-empty, stale, degraded, error, keyboard navigation, forced-colors-safe status rendering, and stable selectors.

### Story 1.3: Tenant Detail Navigation and Overview

As a tenant operator,
I want to open tenant details from the list and return without losing context,
So that I can inspect a tenant while preserving my triage workflow.

**Requirements:** FR2, FR5, FR7; NFR1-NFR4, NFR6-NFR9; UX-DR5, UX-DR7, UX-DR15, UX-DR21, UX-DR23, UX-DR29-UX-DR31.

**Acceptance Criteria:**

**Given** an authorized user is viewing the tenant list with filters, sorting, paging, selection, and scroll context
**When** the user opens a tenant detail surface and then returns to the list
**Then** the prior filter, sort, cursor, selection, and context are preserved
**And** the return path does not reload into a misleading default list state.

**Given** a tenant detail deep link is opened directly
**When** the tenant exists and the caller is authorized to view it
**Then** the detail surface loads through the server-side BFF query gateway using `GET /api/tenants/{tenantId}`
**And** the browser does not call the backend directly.

**Given** tenant detail projection data is available
**When** the overview renders
**Then** it shows tenant status, lifecycle, metadata, member summaries, configuration summaries, member count, owner count, and freshness
**And** status and lifecycle are shown with text, icon, semantic badge, and accessible label rather than color alone.

**Given** the detail projection is stale, degraded, unknown, unavailable, or unauthorized
**When** the detail surface renders
**Then** the UI shows the actual state with localized copy and recovery action where applicable
**And** it does not render stale or unauthorized data as current.

**Given** the tenant id or support-safe reference is visually truncated in detail
**When** the user inspects the value
**Then** the full literal caller-supplied identifier remains available through accessible text and future copy affordances
**And** the UI does not parse or reformat the identifier as a GUID or ULID.

**Given** the detail surface is rendered at desktop, tablet, or mobile widths
**When** layout changes
**Then** safety-critical state, freshness, lifecycle, and identity context remain visible or the view fails closed with a visible reason
**And** focus order and keyboard navigation remain predictable.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** component tests cover deep-link loading, list-context preservation, overview fields, unauthorized/not-found/stale states, and localization keys
**And** Playwright or component coverage verifies keyboard navigation, focus return, stable selectors, and responsive safety behavior.

### Story 1.4: My Tenants Self-Audit View

As a signed-in user,
I want to view the tenants I belong to and my role in each,
So that I can self-audit my tenant access without needing operator intervention.

**Requirements:** FR3; NFR1-NFR4, NFR6-NFR9; UX-DR5, UX-DR16, UX-DR20, UX-DR23, UX-DR25, UX-DR28-UX-DR31.

**Acceptance Criteria:**

**Given** a signed-in user opens the My Tenants view
**When** membership data is available
**Then** the UI shows only tenants the caller is authorized to see, with tenant identity, role, tenant status, lifecycle context, and freshness per row
**And** roles and statuses use localized whole-string labels.

**Given** the user belongs to no visible tenants
**When** the view loads successfully
**Then** the UI renders an explicit empty state
**And** it does not treat the absence of visible memberships as an error.

**Given** membership data is loading, stale, degraded, unauthorized, or unavailable
**When** the view renders
**Then** each state remains distinct and accessible
**And** stale or unknown data is not represented as current.

**Given** the My Tenants view is for self-audit
**When** a row is displayed
**Then** no mutation affordance is rendered from this story
**And** any unavailable future action slot uses a visible reason rather than hover-only explanation.

**Given** the view contains tenant identifiers and role/status badges
**When** the page is operated by keyboard or screen reader
**Then** row relationships, headers, badge labels, and focus order are programmatically clear
**And** stable selectors such as `data-testid="tenants-my-list"` and `data-testid="tenants-my-role"` are present.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** component tests cover authorized memberships, empty state, scoped visibility, stale/degraded/error states, role/status localization, and selector stability
**And** accessibility checks cover keyboard traversal, row semantics, no-color-only status, and forced-colors rendering.

### Story 1.5: User Membership Lookup

As a platform operator,
I want to look up a user's tenant memberships and navigate there from member rows,
So that I can understand a user's tenant access without exposing unauthorized memberships.

**Requirements:** FR4; NFR1-NFR4, NFR6-NFR9; UX-DR5, UX-DR14, UX-DR20-UX-DR21, UX-DR23, UX-DR25, UX-DR28-UX-DR31.

**Acceptance Criteria:**

**Given** an authorized operator enters a caller-supplied user id in the contextual user lookup flow
**When** the lookup runs
**Then** the UI queries memberships through the server-side BFF using `GET /api/users/{userId}/tenants`
**And** the browser never calls the backend directly or treats the user id as a GUID or ULID.

**Given** the lookup returns authorized membership results
**When** the result table renders
**Then** each row shows tenant identity, user role, tenant status, lifecycle context, and freshness
**And** results are scoped to what the caller is authorized to see.

**Given** no memberships are visible to the caller
**When** the lookup completes successfully
**Then** the UI shows an explicit authorization-safe empty state
**And** it does not reveal whether hidden memberships exist.

**Given** a user membership view is reached from a tenant member row
**When** the target user id is passed to the lookup route or surface
**Then** the same authorization-scoped lookup behavior is used
**And** Users remains contextual rather than becoming a primary navigation tab.

**Given** lookup data is loading, stale, degraded, unauthorized, invalid, or unavailable
**When** the view renders
**Then** each state has distinct localized copy and accessible semantics
**And** no state collapses into false Success.

**Given** the lookup form and results are used with keyboard, screen reader, or narrow viewport
**When** the user submits, clears, sorts, pages, or navigates results
**Then** focus, status announcement, safety-critical columns, and stable selectors such as `data-testid="tenants-user-lookup"` are preserved
**And** copy remains support-safe and free of raw backend payloads or stack traces.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** component or API-adapter tests cover user-id handling, authorization-scoped result mapping, empty/hidden-membership behavior, stale/error states, and contextual navigation
**And** bUnit or Playwright tests verify keyboard submission, focus behavior, live-region announcement, responsive safety, and selector stability.

### Story 1.6: Read-Only Tenant Configuration View

As an authorized tenant user,
I want to inspect tenant configuration values grouped by namespace,
So that I can understand tenant setup without exposing or changing configuration outside my scope.

**Requirements:** FR6, FR7; NFR1-NFR4, NFR6-NFR9; UX-DR5, UX-DR7, UX-DR14, UX-DR23, UX-DR25, UX-DR28-UX-DR31.

**Acceptance Criteria:**

**Given** an authorized user opens the configuration section for a tenant
**When** configuration data is available from the tenant detail projection
**Then** the UI shows read-only key/value entries grouped by namespace
**And** values outside the caller's authorized namespace or prefix are not shown.

**Given** configuration values include sensitive-value candidates or unknown sensitivity
**When** the configuration section renders
**Then** sensitive-value display remains outside the read MVP
**And** the UI fails closed with localized unavailable text rather than exposing payloads or secrets.

**Given** the user filters or scans namespaces
**When** results change
**Then** grouped context, freshness, and authorization scope remain visible
**And** empty and filtered-empty states remain distinct.

**Given** configuration projection data is stale, degraded, unavailable, or unauthorized
**When** the configuration section renders
**Then** the actual state is displayed with no false Success and no mutation affordance
**And** stale or unknown freshness blocks any future command preview entry point.

**Given** configuration keys or values are visually truncated
**When** the user inspects the row
**Then** only support-safe values are eligible for copy behavior
**And** the UI never exposes raw metadata, tokens, internal correlation ids, stack traces, or real PII.

**Given** the configuration section is used on desktop, tablet, or mobile
**When** layout changes
**Then** namespace, key, value safety state, and freshness remain understandable
**And** keyboard navigation, row headers, focus order, and stable selectors such as `data-testid="tenants-config-table"` are preserved.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** component tests cover namespace grouping, prefix filtering, sensitive-value fail-closed behavior, empty/filtered-empty/stale/degraded states, and no mutation affordance
**And** accessibility or Playwright coverage verifies keyboard traversal, forced-colors-safe state rendering, localization, and stable selectors.

### Story 1.7: Tenant Member Table and Action Availability

As an authorized tenant user,
I want to review tenant members and see which actions are available or unavailable,
So that I can understand membership state and safety constraints before any mutation workflow exists.

**Requirements:** FR8, FR9; NFR1-NFR4, NFR6-NFR9; UX-DR5, UX-DR11, UX-DR14, UX-DR17, UX-DR23, UX-DR25, UX-DR28-UX-DR31.

**Acceptance Criteria:**

**Given** an authorized user opens a tenant's member section
**When** member data is available from `GET /api/tenants/{tenantId}/users`
**Then** the member table shows each member's user id, role, owner count context, status, freshness, and orphan or disabled context where applicable
**And** the table remains read-only for this story.

**Given** the member table renders
**When** assistive technology reads it
**Then** headers, sort state, row relationships, role labels, status badges, and freshness are programmatically exposed
**And** the table does not rely on color alone.

**Given** a member action is unavailable because of permissions, freshness, lifecycle, platform support, conflict, or safety policy
**When** the row renders
**Then** an inline `UnavailableActionReason` shows one of the six canonical localized categories
**And** the reason is visible, keyboard reachable, screen-reader associated with the relevant action slot, and not hover-only.

**Given** future member actions are represented as unavailable or not-yet-supported
**When** the current story is complete
**Then** no Add, Change Role, or Remove command is submitted
**And** the UI does not imply mutation success or in-flight command state.

**Given** the tenant is disabled, stale, unknown, degraded, or authorization-indeterminate
**When** member action availability is evaluated
**Then** action slots fail closed with visible localized reasons
**And** indeterminate authorization is not treated as allowed.

**Given** the member table is rendered across supported viewport widths
**When** width changes
**Then** identity, role, owner count, status, freshness, and reason slots remain visible or the action area fails closed
**And** stable selectors such as `data-testid="tenants-member-table"` and `data-testid="tenants-member-unavailable-reason"` are present.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** component tests cover member row mapping, role/status labels, read-only behavior, all unavailable reason categories, disabled/stale/unknown authorization handling, and selector stability
**And** accessibility tests cover table semantics, keyboard reachability, screen-reader association, forced-colors rendering, and no-color-only status.

### Story 1.8: Support-Safe Identifier Copy and Epic 1 Readiness Evidence

As a support-aware Tenants user,
I want to copy safe identifiers and references from read-only Tenants surfaces,
So that I can share precise troubleshooting context without exposing secrets, payloads, or personal data.

**Requirements:** FR7 and Epic 1 readiness for FR1-FR9; NFR4, NFR6-NFR10; UX-DR5, UX-DR7, UX-DR23, UX-DR25, UX-DR28-UX-DR33.

**Acceptance Criteria:**

**Given** a tenant id, user id, or support-safe reference is visually truncated on an Epic 1 surface
**When** the user copies it
**Then** the copied value is the literal caller-supplied string or approved support-safe reference
**And** it is not parsed, normalized, shortened, enriched, or reformatted as a GUID or ULID.

**Given** a value may expose payloads, tokens, decoded JWT contents, raw EventStore metadata, internal correlation ids, stack traces, or real PII
**When** copy eligibility is evaluated
**Then** the UI blocks or redacts the unsafe value with localized explanation
**And** unsafe values are not copied, logged, announced, or displayed in a toast.

**Given** copy succeeds, fails, or is unavailable
**When** feedback is rendered
**Then** the feedback uses Tenants-owned localized copy, appropriate live-region politeness, and no false Success for unavailable or failed copy
**And** focus remains on or returns to the launching control.

**Given** copy controls appear in list, detail, My Tenants, user lookup, configuration, or member table surfaces
**When** the user navigates by keyboard or screen reader
**Then** each control has an accessible name, stable footprint, visible focus, forced-colors support, and stable selectors such as `data-testid="tenants-copy-reference"`
**And** the control does not cause row layout shift.

**Given** Epic 1 stories are ready for development
**When** readiness evidence is reviewed
**Then** the evidence maps `FR1` through `FR9` and applicable `UX-DR` items to stories `1.0` through `1.8`
**And** it records remaining gates, including `FC-LYT`, read BFF integration assumptions, accessibility/localization/responsive obligations, and test coverage expectations.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit or component tests cover safe-copy eligibility, blocked unsafe values, localized feedback states, focus behavior, live-region politeness, and selector stability
**And** Playwright or component coverage verifies copy controls across at least tenant list, tenant detail, and member table surfaces with keyboard and forced-colors checks.

## Epic 2: Tenant Membership and Tenant Record Management

Authorized users can create tenants, update tenant metadata, and manage tenant membership with safe command lifecycle tracking and projection-confirmed outcomes.

### Story 2.1: Create Tenant with Projection-Confirmed Command Lifecycle

As an authorized platform operator,
I want to create a tenant through a projection-confirmed command flow,
So that new tenant records become visible only when the system has proven the outcome.

**Requirements:** FR13; NFR2-NFR8, NFR10; Additional command confirmation requirements; UX-DR3, UX-DR11-UX-DR13, UX-DR22-UX-DR28, UX-DR33.

**Acceptance Criteria:**

**Given** the Tenants UI host and read workspace exist
**When** an authorized operator opens the create tenant flow
**Then** the form uses Tenants-owned localized labels, validation copy, accessible field semantics, stable selectors, and the shared FrontComposer/Fluent command surface
**And** the tenant id remains a caller-supplied string and is not parsed or generated as a GUID or ULID.

**Given** the operator submits a valid create tenant request
**When** the command is sent
**Then** the browser calls only the server-side command gateway, the gateway submits through `POST /api/v1/commands`, and a client-generated `messageId` ULID is used as the idempotency key
**And** no backend endpoint is added for preview, receipt, or command status.

**Given** the command has been submitted
**When** command-status polling or SignalR projection notifications occur
**Then** the UI treats SignalR only as a freshness nudge, re-queries the authoritative tenant projection, and marks the command `confirmed` only after the re-query proves the tenant exists
**And** accepted, pending, confirmed, rejected, failed, degraded, and unable-to-verify states remain distinct.

**Given** the tenant id already exists
**When** the backend returns `TenantAlreadyExists`
**Then** the command lifecycle shows a rejection with safe localized text
**And** the UI does not show Success, create duplicate client-side state, or expose raw payloads, metadata, stack traces, tokens, or correlation internals.

**Given** authorization, freshness, validation, or lifecycle support is indeterminate
**When** the operator attempts to submit the create tenant command
**Then** submission fails closed with a visible inline unavailable reason
**And** focus remains recoverable and the one-at-a-time command policy prevents concurrent command submission.

**Given** the command outcome is confirmed or cannot be verified
**When** the lifecycle panel renders the result
**Then** it provides an honest audit/evidence handoff state, including audit pending, delayed, unavailable, or missing-support where applicable
**And** the state is not collapsed into command success.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit and component tests cover command gateway mapping, idempotency key creation, lifecycle reducers, SignalR-as-nudge behavior, projection re-query confirmation, `TenantAlreadyExists`, authorization/freshness gating, and no false Success
**And** Playwright or component tests verify keyboard submission, focus behavior, live-region politeness, forced-colors-safe command state, stable selectors, and audit handoff state.

### Story 2.2: Add User to Tenant with Explicit Role

As an authorized tenant administrator,
I want to add a user directly to a tenant with an explicit role,
So that tenant access can be granted without invitation or pending-member ambiguity.

**Requirements:** FR10; NFR2-NFR8, NFR10; Additional command confirmation requirements; UX-DR3, UX-DR11-UX-DR14, UX-DR22-UX-DR28, UX-DR33.

**Acceptance Criteria:**

**Given** an authorized user opens the add-member flow from a tenant member table
**When** the form renders
**Then** it requires a caller-supplied user id and explicit tenant role
**And** it does not offer invitation, pending-member, bulk-add, or global Users navigation behavior.

**Given** validation, freshness, authorization, and tenant lifecycle are eligible
**When** the add-member command is submitted
**Then** the server-side command gateway submits the existing domain command through `POST /api/v1/commands`
**And** success is shown only after command status plus authoritative member projection re-query confirms the user is present with the requested role.

**Given** the target user is already a member
**When** the backend returns `UserAlreadyInTenant`
**Then** the lifecycle panel shows a rejected state with safe localized text
**And** the UI does not treat the response as a NoOp or show Success.

**Given** the requested role is unavailable, unknown, or would violate authorization rules
**When** the user attempts to submit
**Then** the flow fails closed with inline validation or unavailable-action reason
**And** role escalation and `TenantRole.Unknown` are surfaced as safe localized rejection text when returned by the domain.

**Given** another command is in flight or confirmation is unknown
**When** the user tries to submit add-member again
**Then** the command trigger is unavailable with a visible reason
**And** duplicate submission does not create optimistic row state.

**Given** the add-member outcome is accepted, rejected, confirmed, degraded, or audit unavailable
**When** the lifecycle panel renders
**Then** each state is visible, accessible, localized, and linked to any available audit/evidence handoff
**And** raw command payloads, tokens, stack traces, metadata, and internal correlation ids are never shown or copied.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover explicit user id and role validation, command gateway submission, `UserAlreadyInTenant`, role rejection paths, one-at-a-time locking, projection-confirmed success, and audit handoff
**And** Playwright or component tests verify keyboard complete-or-exit behavior, focus return, live-region announcements, forced-colors rendering, stable selectors, and no optimistic member row.

### Story 2.3: Change Tenant Member Role

As an authorized tenant administrator,
I want to change a tenant member's role,
So that member authority can be corrected while preserving domain safety and projection truth.

**Requirements:** FR11; NFR2-NFR8, NFR10; Additional command confirmation requirements; UX-DR3, UX-DR11-UX-DR14, UX-DR22-UX-DR28, UX-DR33.

**Acceptance Criteria:**

**Given** an authorized user opens the change-role flow for an existing tenant member
**When** the role options render
**Then** only eligible roles are offered with localized labels and accessible semantics
**And** unavailable roles are explained inline rather than hidden behind hover-only copy.

**Given** the user selects the member's current role
**When** the flow is submitted
**Then** the UI shows `already applied` as a NoOp state
**And** it does not show projection-confirmed Success or submit duplicate client-side mutation state.

**Given** the user selects an allowed new role
**When** the command is submitted
**Then** command status and SignalR nudges lead to an authoritative member projection re-query
**And** the role is shown as changed only after the re-query confirms the new role.

**Given** the domain rejects role escalation or `TenantRole.Unknown`
**When** the rejection is returned
**Then** the command lifecycle displays a safe localized rejected state
**And** no raw payload, internal metadata, correlation id, token, or stack trace is exposed.

**Given** freshness, authorization, lifecycle, or preview support is unknown
**When** the user attempts role change
**Then** the action fails closed with the relevant unavailable-action reason
**And** the current confirmed role remains visible without being overwritten by in-flight intent.

**Given** role change affects owner count or safety context
**When** the command is previewed or submitted
**Then** owner-count context remains visible and accessible
**And** any high-risk condition is represented with warning semantics, not false Success.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover current-role NoOp, allowed role change, role escalation rejection, `TenantRole.Unknown`, projection re-query confirmation, owner-count context, and non-collapse lifecycle states
**And** accessibility or Playwright coverage verifies keyboard role selection, focus return, inline reasons, live-region politeness, forced-colors status rendering, and stable selectors.

### Story 2.4: Remove Tenant Member with Consequence Preview

As an authorized tenant administrator,
I want to remove a user from a tenant through a consequence preview and confirmed command lifecycle,
So that access removal is deliberate, projection-confirmed, and never shown as audit-proven before evidence exists.

**Requirements:** FR12; NFR2-NFR8, NFR10; Additional consequence preview, command confirmation, and audit handoff requirements; UX-DR3, UX-DR11-UX-DR15, UX-DR19, UX-DR22-UX-DR28, UX-DR33.

**Acceptance Criteria:**

**Given** an authorized user opens remove-member for a tenant member
**When** validation, freshness, authorization, and lifecycle support are all eligible
**Then** the UI opens a Consequence Preview using the approved inline structured-text fallback
**And** submission is blocked if any required preview item is unavailable.

**Given** the preview renders
**When** the target removal would leave zero owners or the target also holds global-administrator authority
**Then** the UI shows elevated friction and risk context with visible localized text, icon, and accessible label
**And** last-owner removal remains allowed with extra friction while last-global-admin rules remain outside tenant membership.

**Given** the user confirms removal
**When** the command is submitted
**Then** the command gateway uses the existing command endpoint, enforces one-at-a-time submission, tracks command status and SignalR nudges, and re-queries the member projection
**And** the member is shown as removed only after authoritative projection confirmation.

**Given** the removal is duplicate, already applied, rejected, failed, degraded, or unable to verify
**When** the lifecycle panel renders
**Then** the UI shows the exact safe state and does not display Success
**And** last-confirmed member data is not overwritten by in-flight intent.

**Given** audit evidence is pending, delayed, unavailable, missing implementation support, or not yet implemented by Epic 5
**When** the command reaches a terminal or unverifiable state
**Then** the UI provides only an honest audit/evidence handoff state and appropriate recovery action such as wait, retry, inspect audit, continue read-only, or escalate
**And** it shows `audit available` or renders an Audit Evidence Receipt only when the Epic 5 evidence source is implemented and available
**And** the original event is not edited, deleted, or rewritten.

**Given** the destructive flow uses a modal, panel, or preview surface
**When** the user cancels, presses Escape, submits, or encounters failure
**Then** focus remains trapped while open and returns to the launching control afterward
**And** no action commits on cancel or Escape.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover fail-closed gating, complete preview content, last-owner elevated friction, target-global-admin friction, duplicate/already-applied handling, projection confirmation, audit unavailable states, no optimistic removal, and the rule that audit proof/receipt UI is not asserted before the Epic 5 evidence source exists
**And** Playwright or component tests verify destructive confirmation focus behavior, keyboard complete-or-exit, live-region announcements, forced-colors status, stable selectors, and one-command-at-a-time locking.

### Story 2.5: Edit Tenant Metadata with Safe Validation

As an authorized tenant contributor or global administrator,
I want to edit tenant metadata through a safe confirmed command flow,
So that tenant records can be maintained without hiding validation errors or projection lag.

**Requirements:** FR14; NFR2-NFR8, NFR10; Additional command confirmation requirements; UX-DR3, UX-DR11-UX-DR14, UX-DR22-UX-DR28, UX-DR33.

**Acceptance Criteria:**

**Given** an authorized tenant contributor or global administrator opens the edit-metadata flow
**When** the form renders
**Then** editable fields use localized labels, whole-string validation messages, accessible descriptions, stable selectors, and support-safe display rules
**And** users without permission see inline unavailable-action reasons instead of mutation controls.

**Given** the user submits valid metadata changes
**When** the command is accepted
**Then** the UI keeps the last-confirmed metadata visible until the tenant detail projection re-query confirms `TenantUpdated`
**And** every successful edit is treated as an emitted update with no same-state suppression assumption.

**Given** validation fails locally or in the domain
**When** errors are returned
**Then** the form shows safe localized field messages
**And** it does not expose raw backend payloads, stack traces, metadata, tokens, internal correlation ids, or PII.

**Given** command confirmation is pending, rejected, failed, degraded, or unknown
**When** the lifecycle panel renders
**Then** each state remains distinct and accessible
**And** the UI does not display Success until projection truth confirms the metadata value.

**Given** metadata editing is attempted on a disabled, stale, unauthorized, or unknown-freshness tenant
**When** eligibility is evaluated
**Then** the action fails closed with a visible localized reason
**And** stale or unknown projection data cannot be used to imply a safe edit.

**Given** edit outcome evidence is available, delayed, or unavailable
**When** the result is shown
**Then** the UI provides the appropriate audit/evidence handoff state
**And** correction remains a future forward command, never an event rewrite.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover contributor/global-admin permission reflection, validation messages, command submission, projection-confirmed metadata update, no same-state suppression assumption, stale/disabled/unauthorized gating, and audit handoff
**And** Playwright or component tests verify keyboard editing, focus return, live-region politeness, forced-colors status, stable selectors, support-safe errors, and no optimistic metadata overwrite.

## Epic 3: Tenant Lifecycle and Configuration Control

Authorized users can safely control tenant lifecycle and tenant configuration while preserving high-impact safety rules and projection truth.

### Story 3.1: Tenant Lifecycle Command Availability and Blocked-State Guardrail

As an authorized global administrator,
I want to see whether tenant enable or disable actions are available and why they may be blocked,
So that high-impact lifecycle controls never render as casual or falsely available actions.

**Requirements:** FR15 readiness and availability guardrail; NFR2-NFR10; Additional platform-wide destructive action gate; UX-DR3, UX-DR11, UX-DR14, UX-DR19, UX-DR22-UX-DR28, UX-DR30-UX-DR33.

**Acceptance Criteria:**

**Given** a tenant detail surface is loaded for a caller who may have global administrator authority
**When** lifecycle action availability is evaluated
**Then** the UI uses server-side authorization reflection, tenant lifecycle state, freshness, and FrontComposer/governance gate status to determine availability
**And** indeterminate authorization, unknown freshness, missing lifecycle support, or unresolved `FC-CMD`/high-impact governance fails closed.

**Given** tenant lifecycle control is blocked by platform-wide destructive-action policy
**When** the enable or disable action slot renders
**Then** the action is unavailable with a visible localized `UnavailableActionReason`
**And** no command can be submitted from the UI.

**Given** the tenant is already active or already disabled
**When** lifecycle action availability is computed
**Then** same-state commands are represented as unavailable or expected rejection states
**And** the UI names `TenantLifecycleStateAlreadySet` as the safe localized domain outcome when applicable.

**Given** the tenant projection is stale, degraded, unknown, unauthorized, or disabled
**When** lifecycle controls are displayed
**Then** the current projection truth remains visible with no optimistic transition
**And** the UI does not imply the tenant is enabled or disabled until the projection proves it.

**Given** lifecycle controls are high-impact
**When** the page is viewed on mobile or any viewport that cannot preserve full safety context
**Then** enable and disable actions are unavailable with visible reason
**And** safety-critical status, freshness, and tenant identity remain visible.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover authorization reflection, `FC-CMD`/governance gate blocking, same-state availability, stale/unknown freshness, mobile fail-closed behavior, and no command submission while blocked
**And** accessibility or Playwright coverage verifies inline reasons, keyboard reachability, forced-colors rendering, live-region behavior, and stable selectors such as `data-testid="tenants-lifecycle-unavailable-reason"`.

### Story 3.2: Disable or Enable Tenant with High-Impact Confirmation

As an authorized global administrator,
I want to disable or enable a tenant through high-impact confirmation and projection proof,
So that tenant availability changes are deliberate, auditable, and never shown as successful before truth is confirmed.

**Requirements:** FR15; NFR2-NFR10; Additional command confirmation and high-impact lifecycle-control requirements; UX-DR3, UX-DR11-UX-DR15, UX-DR19, UX-DR22-UX-DR28, UX-DR30-UX-DR33.

**Acceptance Criteria:**

**Given** `FC-CMD`, high-impact governance, authorization, freshness, and lifecycle support are confirmed
**When** an authorized global administrator starts enable or disable
**Then** the UI opens a high-impact Consequence Preview with tenant identity, current lifecycle, intended lifecycle, known consequences, known unknowns, audit/evidence expectation, and recovery path
**And** submission is blocked if any required preview item is unavailable.

**Given** the lifecycle confirmation surface is open
**When** the user cancels, presses Escape, submits, or encounters an error
**Then** focus is trapped while open and returns to the launching control afterward
**And** cancel or Escape does not commit any action.

**Given** the user confirms a valid lifecycle change
**When** the command is submitted
**Then** the existing command gateway submits through `POST /api/v1/commands`, enforces one-at-a-time command policy, tracks status and SignalR nudges, and re-queries the tenant projection
**And** the tenant is shown as enabled or disabled only after authoritative projection confirmation.

**Given** the backend returns `TenantLifecycleStateAlreadySet` or `TenantDisabled`
**When** the lifecycle panel renders the result
**Then** the UI shows safe localized rejection text and the correct non-Success lifecycle state
**And** it does not expose raw command payloads, metadata, stack traces, tokens, internal correlation ids, or PII.

**Given** the lifecycle outcome is accepted, pending, rejected, failed, degraded, unable to verify, audit pending, delayed, unavailable, or missing support
**When** the result is displayed
**Then** every state remains distinct and accessible
**And** audit/evidence handoff is honest and never fabricated.

**Given** lifecycle disable/enable is approved as a reversible soft-delete availability control
**When** this story is selected for implementation
**Then** the story is ready only when global-admin authorization reflection, complete consequence preview, one-at-a-time command policy, projection-confirmed lifecycle feedback, accessibility, localization, and responsive evidence are present
**And** hard destructive tenant deletion remains out of scope for this UI story and is reserved for future independent administrators-only CLI tooling.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover gate readiness, preview completeness, one-at-a-time locking, `TenantLifecycleStateAlreadySet`, `TenantDisabled`, projection confirmation, audit unavailable states, and no optimistic lifecycle transition
**And** Playwright or component tests verify destructive confirmation focus behavior, keyboard complete-or-exit, live-region politeness, forced-colors status rendering, responsive fail-closed behavior, and stable selectors.

### Story 3.3: Set Tenant Configuration Key Value with Consequence Preview

As an authorized tenant user,
I want to set a namespaced tenant configuration key/value through a safe command flow,
So that tenant configuration can be changed within my scope with proof and without leaking sensitive data.

**Requirements:** FR16; NFR2-NFR10; Additional consequence preview and command confirmation requirements; UX-DR3, UX-DR11-UX-DR15, UX-DR19, UX-DR22-UX-DR28, UX-DR30-UX-DR33.

**Acceptance Criteria:**

**Given** an authorized user opens the set-configuration flow for a tenant
**When** the form renders
**Then** it requires namespace, key, and value input within domain limits and authorized prefix scope
**And** labels, validation, warnings, and unavailable reasons use Tenants-owned localized whole strings.

**Given** validation, freshness, authorization, lifecycle, and preview support are eligible
**When** the user prepares a configuration change
**Then** the UI renders the approved inline Consequence Preview fallback unless a confirmed shared `FC-CNS` component exists
**And** the preview blocks submission when required content, freshness, authorization, or scope data is unavailable.

**Given** the submitted key/value is identical to the current projection value
**When** the command flow resolves
**Then** the UI shows `already applied` as a NoOp state
**And** it does not show projection-confirmed Success or create optimistic local state.

**Given** the value exceeds domain limits or violates namespace scope
**When** validation or the domain returns `ConfigurationLimitExceeded` or a scoped rejection
**Then** the UI shows safe localized field or rejection text
**And** it does not expose the raw value, backend payload, metadata, stack trace, token, or internal correlation id.

**Given** the user submits an eligible configuration change
**When** command status or SignalR notification arrives
**Then** SignalR is treated only as a freshness nudge and the configuration is shown as changed only after authoritative tenant projection re-query
**And** accepted, pending, confirmed, rejected, failed, degraded, unable-to-verify, and audit states remain distinct.

**Given** the tenant is disabled, stale, unknown, degraded, or authorization-indeterminate
**When** set-configuration availability is evaluated
**Then** the action fails closed with visible inline reason
**And** the last-confirmed configuration remains visible without being overwritten by in-flight intent.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover namespace authorization, validation limits, preview blocking, identical-value NoOp, `ConfigurationLimitExceeded`, disabled/stale/unknown gating, projection-confirmed update, and audit handoff
**And** Playwright or component tests verify keyboard form operation, focus return, live-region politeness, forced-colors status rendering, stable selectors, and no sensitive-value exposure.

### Story 3.4: Remove Tenant Configuration Key with Consequence Preview

As an authorized tenant user,
I want to remove a tenant configuration key through a safe command flow,
So that obsolete configuration can be removed with clear consequence and proof.

**Requirements:** FR17; NFR2-NFR10; Additional consequence preview and command confirmation requirements; UX-DR3, UX-DR11-UX-DR15, UX-DR19, UX-DR22-UX-DR28, UX-DR30-UX-DR33.

**Acceptance Criteria:**

**Given** an authorized user opens remove-configuration for a visible configuration key
**When** validation, freshness, authorization, lifecycle, and preview data are eligible
**Then** the UI renders a Consequence Preview naming tenant identity, namespace, key, current known state, consequence, known unknowns, audit/evidence expectation, and recovery path
**And** submission is blocked if any required preview item is unavailable.

**Given** the key is outside the caller's authorized namespace or prefix
**When** remove availability is evaluated
**Then** the action is unavailable with visible localized reason
**And** unauthorized key existence is not revealed.

**Given** the target key is missing
**When** the backend returns `ConfigurationKeyNotFound`
**Then** the lifecycle panel shows a safe localized rejected state
**And** it does not show Success or remove unrelated visible state.

**Given** the user confirms removal for an eligible key
**When** the command is submitted
**Then** the command gateway uses the existing command endpoint, one-at-a-time locking, command-status polling, SignalR freshness nudges, and tenant projection re-query
**And** the key is shown as removed only after projection confirmation.

**Given** removal is accepted, pending, rejected, failed, degraded, unable to verify, audit pending, audit delayed, or audit unavailable
**When** the result renders
**Then** the UI shows the exact state with localized text and accessible semantics
**And** it does not edit, delete, rewrite, or fabricate event/projection history.

**Given** the remove flow is cancelled, fails, or is unavailable
**When** the user exits the flow
**Then** focus returns to the launching control and no action is committed on cancel or Escape
**And** copy/log/error output remains support-safe.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover preview completeness, namespace scope, hidden unauthorized keys, `ConfigurationKeyNotFound`, projection-confirmed removal, one-at-a-time locking, audit unavailable states, and no optimistic deletion
**And** Playwright or component tests verify destructive confirmation keyboard behavior, focus trap/return, live-region politeness, forced-colors status rendering, stable selectors, and support-safe errors.

## Epic 4: Global Administrator Governance

Authorized operators can review and manage global administrator authority separately from tenant membership, including last-global-admin safety.

### Story 4.1: Global Administrators Navigation and Read Contract Readiness

As an authorized platform operator,
I want the Global Administrators area to appear only when its authorization and read contract are safe,
So that platform authority is never confused with tenant membership or exposed through an unsupported route.

**Requirements:** FR18, FR19 readiness; NFR2-NFR4, NFR6-NFR10; Additional fixed `global-administrators` scope requirements; UX-DR3, UX-DR14, UX-DR20, UX-DR22-UX-DR23, UX-DR25, UX-DR28-UX-DR31, UX-DR33.

**Acceptance Criteria:**

**Given** the Operations Shell navigation is rendered
**When** the caller is an authorized platform operator
**Then** the primary navigation includes Global Administrators after Tenants and before Audit
**And** Users remains contextual and is not promoted to primary navigation.

**Given** the caller is a tenant owner without platform authority
**When** the Operations Shell navigation is rendered
**Then** the Global Administrators area is hidden or unavailable according to server-side authorization reflection
**And** hidden authority data is not revealed through labels, counts, routes, or empty states.

**Given** global administrator data belongs to the fixed `global-administrators` aggregate
**When** the UI read contract is evaluated
**Then** the story records the confirmed query/API route or marks the read surface as blocked by missing implementation support
**And** it does not route global administrator data through tenant-domain list, member, or user membership endpoints.

**Given** no confirmed global-administrator read route is available
**When** an authorized operator opens the area
**Then** the UI shows an honest missing-implementation-support state with localized copy and support-safe recovery guidance
**And** the UI does not add a Tenants-local backend endpoint or fabricate administrator rows.

**Given** the area is unavailable because of authorization, stale freshness, missing read support, or FrontComposer gate status
**When** the area renders
**Then** the unavailable state uses visible text, icon, accessible label, live-region behavior, forced-colors-safe styling, and stable selectors
**And** no state is shown as Success.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** component tests cover navigation ordering, platform-operator visibility, tenant-owner hiding, fixed-aggregate routing guard, missing-read-support state, localization, and stable selectors
**And** accessibility or Playwright coverage verifies keyboard navigation, screen-reader labeling, forced-colors rendering, and support-safe unavailable copy.

### Story 4.2: Review Global Administrators from Fixed Aggregate

As an authorized platform operator,
I want to review current global administrators from the fixed platform authority scope,
So that I can understand who holds platform-level governance power without conflating it with tenant membership.

**Requirements:** FR18; NFR1-NFR4, NFR6-NFR10; Additional fixed `global-administrators` scope requirements; UX-DR3, UX-DR5, UX-DR14, UX-DR20, UX-DR23, UX-DR25, UX-DR28-UX-DR31, UX-DR33.

**Acceptance Criteria:**

**Given** a confirmed global-administrator read contract is available
**When** an authorized operator opens Global Administrators
**Then** the UI reads from the fixed `global-administrators` aggregate scope through the server-side BFF
**And** it does not use tenant membership, tenant detail, or user membership endpoints as a substitute.

**Given** global administrator projection data is available
**When** the review surface renders
**Then** each row shows administrator identity and freshness
**And** the surface clearly communicates platform authority scope without calling it tenant ownership.

**Given** the read result is empty, stale, degraded, unauthorized, or unavailable
**When** the review surface renders
**Then** the UI shows the exact state with localized copy and accessible semantics
**And** it does not reveal hidden administrators to unauthorized callers or show false Success.

**Given** freshness is unknown or the projection is stale
**When** action availability is computed from the review surface
**Then** grant and remove actions fail closed with visible unavailable reasons
**And** last-confirmed administrator data remains visible without being overwritten by in-flight intent.

**Given** administrator identifiers are visible or truncated
**When** the operator inspects or copies them
**Then** only support-safe literal identifiers or approved references are exposed
**And** no tokens, decoded JWT contents, raw metadata, stack traces, internal correlation ids, or PII are shown or copied.

**Given** the review surface is used at supported desktop, tablet, or mobile widths
**When** layout changes
**Then** identity, platform authority context, freshness, and action/reason slots remain visible or fail closed
**And** stable selectors such as `data-testid="tenants-global-admins-list"` are present.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover fixed-aggregate read mapping, authorization-scoped visibility, empty/stale/degraded/unavailable states, no tenant-membership conflation, support-safe identifiers, and stable selectors
**And** accessibility or Playwright coverage verifies keyboard navigation, row semantics, forced-colors status rendering, responsive safety, and localized state copy.

### Story 4.3: Grant Global Administrator with Projection Confirmation

As an authorized global administrator,
I want to grant global administrator authority to another user through a confirmed command flow,
So that platform authority changes are explicit, scoped to `global-administrators`, and auditable.

**Requirements:** FR19; NFR2-NFR10; Additional `SetGlobalAdministrator` command confirmation requirements; UX-DR3, UX-DR11-UX-DR15, UX-DR19, UX-DR22-UX-DR28, UX-DR30-UX-DR33.

**Acceptance Criteria:**

**Given** an authorized global administrator opens the grant flow
**When** the form renders
**Then** it accepts a caller-supplied user id, uses Tenants-owned localized labels and validation copy, and names the platform authority scope
**And** it does not model the target as a tenant member or route the action through a tenant aggregate.

**Given** authorization, freshness, read support, command support, and one-at-a-time policy are eligible
**When** the grant command is submitted
**Then** the command gateway submits `SetGlobalAdministrator` through the existing command endpoint with a client-generated `messageId` ULID idempotency key
**And** the UI shows success only after authoritative projection re-query confirms the user appears in the fixed `global-administrators` scope.

**Given** the target user is already a global administrator
**When** the backend returns `GlobalAdministratorAlreadyExists`
**Then** the command lifecycle shows a safe localized rejected state
**And** the UI does not treat the response as a NoOp or show Success.

**Given** the caller lacks global-administrator authority or authorization is indeterminate
**When** the grant action is evaluated or submitted
**Then** the flow fails closed with a visible unavailable reason or safe `InsufficientPermissions` rejection
**And** hidden platform authority data is not revealed.

**Given** command confirmation is accepted, pending, rejected, failed, degraded, unable to verify, audit pending, delayed, unavailable, or missing support
**When** the lifecycle panel renders
**Then** each state remains distinct, accessible, localized, and support-safe
**And** SignalR is only a freshness nudge and never the source of command success.

**Given** the grant flow completes, fails, or is cancelled
**When** focus and feedback are handled
**Then** focus returns to the launching control, live-region politeness matches the state, and no raw payloads or internal correlation ids are exposed
**And** stable selectors such as `data-testid="tenants-global-admin-grant"` are present.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover command payload mapping, fixed-scope routing, idempotency key creation, `GlobalAdministratorAlreadyExists`, `InsufficientPermissions`, projection-confirmed grant, audit unavailable states, one-at-a-time locking, and no tenant membership conflation
**And** Playwright or component tests verify keyboard submission, focus return, live-region behavior, forced-colors status rendering, support-safe copy, and stable selectors.

### Story 4.4: Remove Global Administrator with Last-Admin Hard Stop

As an authorized global administrator,
I want to remove global administrator authority only when it is safe and domain-allowed,
So that the platform never loses its last global administrator and the UI never treats that case as completable friction.

**Requirements:** FR19; NFR2-NFR10; Additional `RemoveGlobalAdministrator`, `LastGlobalAdministrator`, and fixed-scope command requirements; UX-DR3, UX-DR11-UX-DR15, UX-DR19, UX-DR22-UX-DR28, UX-DR30-UX-DR33.

**Acceptance Criteria:**

**Given** the Global Administrators review surface has current projection data
**When** remove availability is computed for each administrator
**Then** removing the last global administrator is unavailable with a visible localized reason before submission
**And** the UI does not render it as elevated friction, override, or completable confirmation.

**Given** a remove action is available for a non-last global administrator
**When** an authorized global administrator opens the remove flow
**Then** the UI renders a high-impact Consequence Preview naming platform authority scope, target user id, current admin count, known consequences, known unknowns, audit/evidence expectation, and recovery path
**And** submission is blocked if any required preview item is unavailable.

**Given** the user confirms removal
**When** the command is submitted
**Then** the command gateway submits `RemoveGlobalAdministrator` through the existing command endpoint, enforces one-at-a-time locking, tracks command status and SignalR nudges, and re-queries the fixed global-administrators projection
**And** the user is shown as removed only after projection confirmation.

**Given** a race condition makes the target the last global administrator before processing
**When** the backend returns `LastGlobalAdministrator`
**Then** the lifecycle panel shows a safe localized hard-blocked rejection
**And** the UI does not show Success or retry as tenant-membership removal.

**Given** the target is not a global administrator
**When** the backend returns `GlobalAdministratorNotFound`
**Then** the lifecycle panel shows safe localized rejection text
**And** the review surface remains based on last-confirmed projection truth.

**Given** the remove flow is cancelled, fails, is rejected, or cannot be verified
**When** result and audit/evidence state are rendered
**Then** each state remains distinct, support-safe, accessible, and localized
**And** the action never edits tenant aggregates, tenant membership, events, projections, or state-store history.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover last-admin pre-submit unavailability, race-returned `LastGlobalAdministrator`, `GlobalAdministratorNotFound`, fixed-scope command mapping, projection-confirmed removal, audit unavailable states, one-at-a-time locking, and no tenant membership command
**And** Playwright or component tests verify destructive confirmation focus trap/return, keyboard complete-or-exit, live-region politeness, forced-colors rendering, responsive fail-closed behavior, support-safe errors, and stable selectors.

## Epic 5: Audit Evidence and Forward Recovery

Users can inspect audit evidence, distinguish incomplete proof states, and launch forward corrective actions with their own preview and projection-confirmed proof.

### Story 5.1: Tenant Audit Trail DataGrid

As an authorized Tenants user,
I want to browse a tenant audit trail as a flat, filtered, cursor-paginated list,
So that I can inspect recorded tenant activity without relying on hidden event payloads.

**Requirements:** FR20; NFR1-NFR10; Additional audit and cursor requirements; UX-DR3, UX-DR5, UX-DR11, UX-DR18, UX-DR22-UX-DR23, UX-DR25, UX-DR28-UX-DR31, UX-DR33.

**Acceptance Criteria:**

**Given** an authorized user opens a tenant audit surface
**When** the audit list loads
**Then** the UI queries `GET /api/tenants/{tenantId}/audit` through the server-side BFF and renders a flat AuditDataGrid
**And** the browser never calls the backend directly or stores backend access tokens.

**Given** audit entries are available
**When** the audit grid renders
**Then** entries are stably ordered, cursor-paginated, and filterable by absolute date range and `AuditEventCategory`
**And** the surface targets about 500 events without unacceptable degradation.

**Given** audit list data is loading, empty, filtered-empty, stale, degraded, unauthorized, invalid-cursor, or errored
**When** that state occurs
**Then** the UI renders a distinct localized, accessible state
**And** no state is collapsed into false Success or fabricated proof.

**Given** an audit cursor becomes invalid or scope-bound data changes
**When** the user refreshes or pages
**Then** the BFF re-queries page 1 and the UI shows an honest list-refreshed notice
**And** invalidation is not treated as a generic error.

**Given** audit rows contain actor, target, tenant scope, outcome, timestamp, projection marker, category, and reference data
**When** the row renders
**Then** only support-safe fields derived from structured audit data are shown
**And** raw event payloads, serialized command payloads, raw EventStore metadata, stack traces, tokens, internal correlation ids, and PII are never displayed or copied.

**Given** the audit grid is viewed at desktop, tablet, or mobile widths
**When** layout changes
**Then** safety-critical columns, category, timestamp, outcome, freshness, and reference context remain visible or the surface fails closed with visible reason
**And** stable selectors such as `data-testid="tenants-audit-grid"` and `data-testid="tenants-audit-filter-category"` are present.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover BFF audit query mapping, date/category filters, cursor paging, invalid cursor refresh, empty/filtered-empty/stale/degraded/error states, support-safe row mapping, and stable selectors
**And** Playwright or component tests verify keyboard filtering, paging, forced-colors-safe statuses, live-region announcements, responsive safety, and no raw payload exposure.

### Story 5.2: Scoped Audit Evidence Entry Points

As an authorized user,
I want to reach audit evidence from the places where I discover or perform tenant work,
So that proof is attached to tenant, user, and command context instead of buried in a separate tool.

**Requirements:** FR21, FR23; NFR2-NFR10; Additional audit availability and navigation context requirements; UX-DR3, UX-DR15, UX-DR20-UX-DR23, UX-DR25, UX-DR28-UX-DR31, UX-DR33.

**Acceptance Criteria:**

**Given** the user is viewing tenant list, tenant detail, user membership lookup, member table, command lifecycle result, or primary Audit navigation
**When** audit evidence is available for the current context
**Then** the UI exposes a scoped path to audit evidence for the relevant tenant, user, or command
**And** the path preserves authorization, tenant scope, filters, and return context.

**Given** an audit entry point is opened from a tenant row or tenant detail
**When** the audit surface loads
**Then** audit filtering is scoped to that tenant
**And** unauthorized tenant audit data is not revealed.

**Given** an audit entry point is opened from user lookup or a member row
**When** the audit surface loads
**Then** the evidence context identifies the target user and tenant scope where available
**And** it does not promote Users to primary navigation or conflate user lookup with global administrator governance.

**Given** an audit entry point is opened from a command lifecycle result
**When** command-specific evidence is available, pending, delayed, unavailable, or unsupported
**Then** the UI shows the actual audit availability state
**And** it does not fabricate proof from command acceptance or SignalR notification alone.

**Given** the user navigates to audit evidence and back
**When** they return to the originating surface
**Then** filters, sort, cursor, selected row, focus target, and contextual state are preserved where applicable
**And** recovery from missing audit support is explicit and localized.

**Given** audit entry points render in dense rows or command panels
**When** keyboard or screen-reader users operate them
**Then** each has an accessible name, visible focus, stable layout, forced-colors support, and selectors such as `data-testid="tenants-audit-entrypoint"`
**And** entry points are unavailable with visible reason when scope or authorization is indeterminate.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** component tests cover entry points from tenant row, tenant detail, user lookup, member row, command result, and Audit navigation, including scope preservation, authorization blocking, return context, and audit availability states
**And** Playwright or component tests verify keyboard navigation, focus restoration, live-region behavior, responsive safety, forced-colors rendering, and stable selectors.

### Story 5.3: Support-Safe Audit Evidence Receipt

As an authorized user,
I want to view a support-safe Audit Evidence Receipt for a recorded action,
So that I can cite what happened without exposing raw event data or fabricating proof.

**Requirements:** FR22, FR23; NFR2-NFR10; Additional `TenantAuditEntry` and `NarrativePayload` receipt-source requirements; UX-DR3, UX-DR11, UX-DR15, UX-DR22-UX-DR23, UX-DR25, UX-DR28-UX-DR31, UX-DR33.

**Acceptance Criteria:**

**Given** a user opens an audit evidence receipt for a recorded action
**When** structured audit data is available
**Then** the receipt shows actor, target, tenant scope, outcome, absolute timestamp, projection marker, and audit or command reference
**And** all labels and values use Tenants-owned localized copy and support-safe formatting.

**Given** receipt data is assembled from `TenantAuditEntry`
**When** fields are resolved
**Then** actor comes from `ActorId`, target comes from `NarrativePayload.userId` then `key` then `TenantId`, scope comes from `TenantId`, outcome comes from event type and `AuditEventCategory`, timestamp comes from the audit entry, projection marker comes from freshness, and reference comes from audit or command reference
**And** `NarrativePayload` is treated as structured narrative metadata, not raw event body.

**Given** receipt evidence is partial, pending, delayed, unavailable, or unsupported
**When** the receipt renders
**Then** the actual state is shown with wait, retry, inspect-audit, continue-read-only, or escalate options where appropriate
**And** the UI does not fabricate proof or show Success.

**Given** receipt values include identifiers or references
**When** the user copies receipt content
**Then** only approved support-safe references are copied
**And** tokens, decoded JWT contents, raw metadata, serialized payloads, internal correlation ids, stack traces, and PII are blocked or redacted.

**Given** the receipt is used with keyboard, screen reader, forced-colors mode, or narrow viewport
**When** it renders
**Then** field labels, value relationships, focus order, visible focus, no-color-only state, and responsive safety are preserved
**And** stable selectors such as `data-testid="tenants-audit-receipt"` and `data-testid="tenants-audit-receipt-reference"` are present.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover receipt field derivation, `NarrativePayload` fallback ordering, partial/unavailable states, safe-copy eligibility, redaction, localization, and stable selectors
**And** accessibility or Playwright coverage verifies keyboard operation, screen-reader field relationships, live-region politeness, forced-colors rendering, responsive safety, and no raw event payload exposure.

### Story 5.4: Audit Availability State Recovery

As an authorized user,
I want audit pending, delayed, unavailable, and missing-support states to be explicit,
So that I know whether to wait, retry, continue read-only, inspect audit, or escalate.

**Requirements:** FR23; NFR3-NFR10; Additional audit availability vocabulary and non-collapse requirements; UX-DR3, UX-DR13, UX-DR15, UX-DR22-UX-DR23, UX-DR25, UX-DR28, UX-DR33.

**Acceptance Criteria:**

**Given** command or audit evidence is not immediately available
**When** the UI evaluates the evidence state
**Then** it distinguishes `audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support`
**And** none of those states is shown as Success.

**Given** audit is pending or delayed
**When** the user sees the evidence state
**Then** the UI provides appropriate wait, retry, or inspect-audit actions based on the state
**And** live-region announcements remain polite unless the state blocks, fails, or becomes unable to verify.

**Given** audit is unavailable or implementation support is missing
**When** the state renders
**Then** the UI offers continue-read-only or escalate paths with localized support-safe copy
**And** raw diagnostics, stack traces, internal correlation ids, payloads, tokens, or PII are not exposed.

**Given** evidence state changes after refresh, command lifecycle update, or projection re-query
**When** the state transitions
**Then** audit availability, command acceptance, and projection confirmation remain separate tokens in state and reducers
**And** in-flight intent never overwrites last-confirmed projection or receipt data.

**Given** an audit availability control appears in list, detail, command lifecycle, receipt, or correction surfaces
**When** keyboard or screen-reader users operate it
**Then** the control has visible text, icon, accessible label, focus behavior, forced-colors-safe status, and stable selectors such as `data-testid="tenants-audit-availability"`
**And** recovery verbs use the canonical vocabulary and casing.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover all audit availability tokens, state transitions, recovery verb mapping, support-safe unavailable copy, non-collapse with command lifecycle states, and selector stability
**And** Playwright or component tests verify keyboard recovery actions, focus return, live-region politeness, forced-colors status rendering, and no false Success.

### Story 5.5: Start Forward Correction from Audit Evidence

As an authorized operator,
I want to start a forward correction from audit evidence,
So that I can restore intended access or begin a correction without editing historical events.

**Requirements:** FR24; NFR2-NFR10; Additional tenant/global-administrator correction distinction requirements; UX-DR3, UX-DR12-UX-DR15, UX-DR19, UX-DR22-UX-DR28, UX-DR33.

**Acceptance Criteria:**

**Given** an authorized user opens an audit evidence receipt for a correctable tenant-domain outcome
**When** correction actions are available
**Then** the UI offers forward recovery actions such as `restore intended access` or `start correction`
**And** labels, tooltips, announcements, and copy never use `undo`, `rollback`, or `hidden edit`.

**Given** the correction relates to tenant membership
**When** the user starts correction
**Then** the UI prepares a new tenant-domain command such as `AddUserToTenant` or `ChangeUserRole` with an explicit intended role where required
**And** it does not edit the original event, projection, or state store.

**Given** the correction relates to global administrator authority
**When** the user starts correction
**Then** the UI keeps the correction in the `global-administrators` scope using `SetGlobalAdministrator` or `RemoveGlobalAdministrator`
**And** it does not edit a tenant aggregate or tenant membership.

**Given** authorization, freshness, current state, audit evidence, or command support is indeterminate
**When** correction availability is evaluated
**Then** the action fails closed with a visible unavailable reason
**And** the original evidence remains visible without implying correction success.

**Given** a correction action is started from audit evidence
**When** the correction flow opens
**Then** it carries a link to the original evidence record, current projection snapshot, intended command type, and required preview data
**And** it does not submit automatically.

**Given** correction actions are rendered in a receipt or audit row
**When** users operate them by keyboard, screen reader, or narrow viewport
**Then** each action has visible focus, accessible name, forced-colors support, stable footprint, and selectors such as `data-testid="tenants-correction-start"`
**And** unavailable actions show inline localized reasons.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover tenant correction command selection, global-administrator correction command selection, explicit role requirement, unavailable reasons, forbidden terminology, original evidence linking, and no history mutation
**And** accessibility or Playwright coverage verifies keyboard start flow, focus behavior, screen-reader labels, forced-colors rendering, support-safe copy, and stable selectors.

### Story 5.6: Preview and Confirm Correction with Linked Proof

As an authorized operator,
I want to preview a correction against current state and link original and corrective records,
So that recovery is deliberate, auditable, and proven by projection confirmation.

**Requirements:** FR25; NFR2-NFR10; Additional forward correction, command confirmation, and audit proof requirements; UX-DR3, UX-DR12-UX-DR15, UX-DR19, UX-DR22-UX-DR28, UX-DR33.

**Acceptance Criteria:**

**Given** a correction flow has been started from audit evidence
**When** the correction preview opens
**Then** the UI shows original evidence reference, current projection state, intended forward command, known consequences, known unknowns, audit/evidence expectation, and recovery path
**And** submission is blocked if any required preview item is unavailable.

**Given** current projection state conflicts with the original evidence or intended correction
**When** preview data is evaluated
**Then** the UI shows the conflict with localized safe text and unavailable or warning state as appropriate
**And** it does not submit a stale correction based only on historical evidence.

**Given** the user confirms an eligible correction
**When** the command is submitted
**Then** the reusable command gateway sends the forward command through the existing command endpoint, enforces one-at-a-time locking, tracks status and SignalR nudges, and re-queries the authoritative projection
**And** correction success is shown only after projection confirmation.

**Given** the correction is accepted, pending, rejected, failed, degraded, unable to verify, audit pending, delayed, unavailable, or missing support
**When** the lifecycle and evidence surfaces render
**Then** each state remains distinct and accessible
**And** the UI never rewrites, deletes, hides, or relabels the original event as undone.

**Given** correction proof becomes available
**When** the user views the original or corrective receipt
**Then** both records are linked with support-safe references and absolute timestamps
**And** the link uses structured narrative metadata and command/audit references rather than raw payloads.

**Given** the correction flow completes, fails, or is cancelled
**When** focus and feedback are handled
**Then** focus returns to the launching receipt or audit row, live-region politeness matches state severity, and copy remains support-safe
**And** stable selectors such as `data-testid="tenants-correction-preview"` and `data-testid="tenants-correction-proof-link"` are present.

**Test Contract:**

**Given** this story is complete
**When** verification is run
**Then** unit/component tests cover preview completeness, current-state conflict blocking, forward command submission, projection-confirmed correction, rejection/unknown/audit-unavailable states, original/corrective record linking, and no history rewrite
**And** Playwright or component tests verify destructive/corrective confirmation focus behavior, keyboard complete-or-exit, live-region politeness, forced-colors status rendering, support-safe proof links, and stable selectors.
