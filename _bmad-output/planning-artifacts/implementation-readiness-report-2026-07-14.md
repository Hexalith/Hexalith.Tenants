---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
status: complete
overallReadiness: NOT_READY
inputDocuments:
  prd:
    - prds/prd-tenants-2026-06-02/prd.md
    - prds/prd-tenants-2026-06-02/addendum.md
  architecture:
    - architecture.md
  epics:
    - epics.md
    - sprint-change-proposal-2026-06-30-epic-1-retro-follow-through.md
    - sprint-change-proposal-2026-06-30-epic-2-retro-follow-through.md
    - sprint-change-proposal-2026-06-29-epic-3-doc-drift-audit.md
    - sprint-change-proposal-2026-06-30-epic-4-retro-follow-through.md
    - sprint-change-proposal-2026-06-29-epic-5-retro-follow-through.md
  ux:
    - ux-designs/ux-tenants-2026-06-02/DESIGN.md
    - ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md
supportingDocuments:
  architecture:
    - architecture/architecture-tenants-2026-06-25/ARCHITECTURE-SPINE.md
  prd:
    - prds/prd-tenants-2026-06-02/reconcile-a11y-l10n.md
    - prds/prd-tenants-2026-06-02/reconcile-audit-recovery.md
    - prds/prd-tenants-2026-06-02/reconcile-frontcomposer-depmap.md
    - prds/prd-tenants-2026-06-02/reconcile-operations-shell.md
    - prds/prd-tenants-2026-06-02/reconcile-phase-2-backlog.md
    - prds/prd-tenants-2026-06-02/reconcile-remove-user-journey.md
    - prds/prd-tenants-2026-06-02/reconcile-responsive-visual.md
    - prds/prd-tenants-2026-06-02/reconcile-truth-state.md
    - prds/prd-tenants-2026-06-02/review-adversarial.md
    - prds/prd-tenants-2026-06-02/review-domain-fidelity.md
    - prds/prd-tenants-2026-06-02/review-downstream-readiness.md
    - prds/prd-tenants-2026-06-02/review-rubric.md
  ux:
    - ux-designs/ux-tenants-2026-06-02/review-accessibility.md
    - ux-designs/ux-tenants-2026-06-02/review-rubric.md
    - ux-designs/ux-tenants-2026-06-02/mockups/mock-command-lifecycle.html
    - ux-designs/ux-tenants-2026-06-02/mockups/mock-consequence-preview.html
    - ux-designs/ux-tenants-2026-06-02/mockups/mock-tenant-list.html
---

# Implementation Readiness Assessment Report

**Date:** 2026-07-14
**Project:** tenants

## Document Discovery

### Documents Selected for Assessment

- **PRD:** `prds/prd-tenants-2026-06-02/prd.md` and `addendum.md`
- **Architecture:** root `architecture.md`
- **Epics and stories:** `epics.md` plus the five epic-specific sprint-change proposals discovered by the required filename pattern
- **UX:** `DESIGN.md` and `EXPERIENCE.md`

### Supporting Material

- The dated `ARCHITECTURE-SPINE.md` bundle is supporting material; root `architecture.md` is authoritative.
- PRD reconciliation/review files and UX reviews/mockups are supporting material rather than competing primary documents.

### Discovery Notes

- PRD, dated architecture, and UX bundles do not contain the standard sharded-document `index.md`.
- No unresolved duplicate document sources remain after the authoritative selections above.

## PRD Analysis

### Functional Requirements

#### FR-1: Browse and triage the tenant list

A platform operator can scan, search, filter, sort, and page through tenants. Realizes UJ-1.

- The list paginates via cursor, never offset/limit.
- Each row shows tenant identity, status, member count, owner count, pending state, and a Truth State Badge with freshness.
- The list renders loading, empty, filtered-empty, error, stale, and degraded as distinct states.
- Sorting or paging never hides a pending or stale marker.
- All states are authorization-safe and do not leak out-of-scope tenants.
- Search matches Name or TenantId across the entire tenant set through Hexalith.Memories syntactic/BM25 search against `tenants-index`.
- A non-empty search term causes a server round-trip; empty or whitespace search returns the unchanged cursor list.
- Search matches are hydrated through the authoritative ETag/freshness read path; the search index never supplies row truth.
- Status filtering is exact rather than fuzzy.
- Search is eventually consistent, while hydrated rows must always be authoritative.
- Memories failure must not block the list: the cursor view remains available with a non-blocking, support-safe notice.

#### FR-2: Open a tenant and return with context preserved

A user can open a tenant's detail and return to the list with prior selection and filters intact. Realizes UJ-1.

- Returning restores the prior filter, sort, and selection.
- Deep-linking to tenant detail is supported.

#### FR-3: Self-audit "My Tenants"

A signed-in user can view the tenants they belong to and their role in each. Realizes UJ-5.

- Only memberships the caller is authorized to see are shown.
- Role and tenant status are shown per row.

#### FR-4: Look up a user's memberships

An operator can search for a user, view that user's tenant memberships, and reach a user from a member row. Realizes UJ-2.

- Results are authorization-scoped.
- A user with no visible memberships produces an explicit empty state rather than an error.

#### FR-5: View tenant overview

A user can view a tenant's status, metadata, and member/configuration summaries on one surface.

- Lifecycle status uses no-color-only encoding and includes freshness.
- Member and owner counts are shown.

#### FR-6: View tenant configuration (read-only)

A user can view a tenant's configuration key/value pairs, grouped by namespace and filtered to namespaces the user owns or is authorized for.

- Values outside the caller's prefix are not shown.
- Sensitive-value display is outside the read MVP scope.

#### FR-7: Copy support-safe identifiers

A user can copy a full identifier, even when it is visually truncated, and any support-safe reference.

- Copied identifiers are complete caller-supplied strings and are not assumed to be ULIDs.
- Payloads, tokens, correlation IDs, and PII are never exposed.

#### FR-8: Review the member table

A user can review a tenant's members with role, owner count, status, freshness, and orphan context in a read-only table.

- The table must not imply mutation.
- It exposes accessible header, sort-state, and row-relationship semantics.
- Freshness uses the Truth State Badge.
- Orphan and disabled context is flagged.

#### FR-9: See action availability and reasons

A user can see, per member, which actions would be available and, when one is unavailable, a plain-language Unavailable Action Reason. This is reflective in MVP; actions arrive later. Realizes UJ-2.

- Reasons use the six canonical categories from the addendum.
- Reasons are visible inline and are never hover-only; tooltips may only supplement them.

#### FR-10: Add a user to a tenant

An authorized user can add a user to a tenant with an explicit role, by user ID. Realizes UJ-6 and UJ-5.

- This is a direct add by caller-supplied user ID, with no invitation or pending step.
- Adding an existing member is rejected as `UserAlreadyInTenant`, rendered as safe localized text; it is not a NoOp.
- A corrective add states the explicit intended role.

#### FR-11: Change a member's role

An authorized user can change a member's role. Realizes UJ-5.

- Changing to the current role is a NoOp shown as `already applied`.
- Role escalation and `Unknown` targets are rejected with safe localized text.
- Success appears only after projection confirmation.

#### FR-12: Remove a user from a tenant

An authorized user can remove a user's tenant access with Consequence Preview, fail-closed gating, elevated-friction handling, and audit proof. Realizes UJ-3.

- Target, tenant, current role, freshness, and authorization are validated before preview; incomplete preview inputs block submission.
- Preview states owner-count impact, access revoked, recovery path, audit expectation, and known unknowns.
- Reducing owner count to zero produces elevated friction but is not blocked.
- A target who also holds global-administrator authority raises platform-level friction.
- The action is not presented as a primary or casual button.
- An already-applied removal reads `already applied`, and duplicate submissions are deduplicated.
- Lifecycle remains distinct across `submitted`, `accepted`, `projection_pending`, `confirmed`, `audit_pending`, and `audit_available`.
- An unconfirmable result is `unable to verify`, never success.
- Every failure mode maps to a stated recovery.

#### FR-13: Create a tenant

An authorized operator can create a new tenant. Realizes UJ-6.

- An existing tenant ID is rejected as `TenantAlreadyExists` with safe text.
- Success appears only after projection confirmation.

#### FR-14: Edit tenant metadata

An authorized tenant contributor or global administrator can edit tenant metadata.

- Every successful edit emits `TenantUpdated`; there is no same-state suppression.
- Validation failures appear as safe localized field messages.

#### FR-15: Disable or enable a tenant

An authorized global administrator can disable or enable a tenant as a high-impact, platform-wide operation with Consequence Preview.

- Requesting the existing state is rejected as `TenantLifecycleStateAlreadySet`.
- Preview states that disabled status is eventually consistent and that commands targeting a disabled tenant are rejected as `TenantDisabled`.
- Success appears only after projection confirmation.
- Lifecycle status uses no-color-only encoding.
- This is a reversible soft-delete/availability-control operation, not hard tenant deletion; hard deletion is outside scope.

#### FR-16: Set a configuration value

An authorized user can set a namespaced configuration key/value with Consequence Preview required for every eligible configuration mutation in v1.

- An identical key/value is a NoOp shown as `already applied`.
- Values over the domain limit are rejected as `ConfigurationLimitExceeded` with safe text.
- There is no low-risk-key bypass in v1.
- Any future narrowing requires a Product/UX/Architecture decision defining classification, user-facing reasons, tests, and phasing impact.

#### FR-17: Remove a configuration key

An authorized user can remove a configuration key with Consequence Preview required for every eligible removal in v1.

- A missing key is rejected as `ConfigurationKeyNotFound` with safe text.
- Success appears only after projection confirmation.
- There is no low-risk-key bypass in v1.

#### FR-18: Review global administrators

An authorized operator can review global-administrator access separately from tenant membership. Realizes UJ-2.

- Only authorized operators can see the surface; tenant owners never see it.
- Data comes from the fixed-identity `global-administrators` aggregate and is not tenant-routed.
- Rows show identity and freshness.

#### FR-19: Grant or remove a global administrator

An authorized operator can grant a global administrator or remove one except the last.

- Removing the last global administrator is rejected by the domain as `LastGlobalAdministrator`.
- The UI presents that operation as unavailable with a safe reason, not as completable friction.
- These operations use the `global-administrators` scope and are never conflated with tenant membership.

#### FR-20: Browse a tenant's audit trail

A user can browse tenant audit entries as a flat, stably ordered list with date and `AuditEventCategory` (`Access` or `Administrative`) filters. Realizes UJ-4.

- The list uses cursor pagination.
- It targets approximately 500 events without unacceptable degradation.
- Loading, empty, filtered-empty, and error states remain distinct and accessible.
- The first delivered slice uses the approved flat audit DataGrid fallback; a reusable FrontComposer timeline is deferred and is not a prerequisite.

#### FR-21: Reach audit from context

A user can reach audit evidence from navigation, tenant row, tenant detail, user lookup, and command result.

- Each entry point lands scoped to its relevant tenant, user, or command.

#### FR-22: View an Audit Evidence Receipt

A user can view a support-safe receipt containing actor, target, tenant scope, outcome, timestamp, projection marker, and an audit or command reference.

- The receipt is assembled client-side from structured `NarrativePayload`; there is no new receipt endpoint.
- Raw payloads, tokens, correlation IDs, raw event metadata, and PII are never exposed.
- Partial completion shows the true lifecycle state, such as `audit pending`, and never pre-renders proof.

#### FR-23: Distinguish audit availability states

A user can distinguish `audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support`, each with a stated recovery.

- None is displayed as success.
- Each offers retry, wait, or escalation.
- `missing implementation support` denotes a missing capability rather than a data error.

#### FR-24: Start a compensating command

From audit evidence, an authorized user can start a correction using language such as `restore intended access` or `start correction`.

- A correction is a new forward command with its own Consequence Preview and proof.
- The UI never calls it `undo`; the original event remains untouched.
- The correction previews current state because re-adding an existing member is rejected.
- Restoring access after all membership history is gone relies on the empty-tenant bootstrap path.

#### FR-25: Preview and link the correction

A user can preview a correction against current state and have the original and corrective records linked.

- Preview reflects current state, which may differ from the original effect.
- Both audit records reference each other.
- Success appears only after projection confirmation.

**Total functional requirements: 25**

### Non-Functional Requirements

#### Explicitly numbered NFRs

**NFR-1: Performance and freshness.** Reads use cursor pagination and conditional requests so unchanged data is cheap, and freshness is visible rather than hidden. A typical tenant list, detail, or member surface targets interactive rendering within approximately one second on a warm projection. Audit targets approximately 500 events without unacceptable latency. Exact budgets remain an implementation-time assumption.

**NFR-2: Security and authorization.** Authorization is server-enforced at both API and domain layers. The UI reflects authorization and never enforces it; server protection must remain safe when the UI misjudges authorization. Tenant-owner versus global-administrator scope is enforced by projection/query handling.

**NFR-3: Reliability and consistency.** The system is eventually consistent. The UI treats projections as the source of truth, re-queries before confirmation, and remains correct under at-least-once delivery and projection lag.

**NFR-4: Observability and testability.** Every interactive element and status has a stable automation selector or component contract. Tests never key on row text or color.

**NFR-5: No data-store edits.** The UI never edits, deletes, or rewrites events, projections, or state to repair data. Corrections are compensating commands only.

#### Unnumbered source NFRs normalized for traceability

The PRD leaves the following requirements unnumbered. `NFR-X*` identifiers are assessment-local traceability labels, not new product identifiers.

**NFR-X1: Audit rendering capacity.** Audit rendering must meet the approximately 500-event target. If a flat render cannot meet it, virtualization or a stricter page size is required before the feature is ready.

**NFR-X2: Accessibility conformance.** Baseline conformance is WCAG 2.1 AA, with WCAG 2.2 AA targeted where the selected Fluent UI Blazor and FrontComposer stack supports it; the 2.2 target remains conditional.

**NFR-X3: Keyboard and focus.** All interactive elements are keyboard reachable with logical order and visible focus in normal, high-contrast, and forced-colors modes. Modals trap focus, offer a safe escape that cannot commit a destructive action, and return focus to the launching control after close, cancel, submission, or failure. Keyboard users must be able to complete or exit every modal, preview, table, and command workflow.

**NFR-X4: Screen reader and status semantics.** Statuses, badges, freshness indicators, and actions have accessible names; timestamps are absolute rather than relative-only; tables expose headers, sort state, and row relationships; live-region politeness is appropriate, with assertive announcements reserved for rejection, failure, destructive blockers, and unverifiable outcomes. Success is never announced before projection confirmation.

**NFR-X5: Non-color and reduced-motion operation.** Color is never the only signal, and reduced-motion users are never dependent on animation.

**NFR-X6: Localization.** State labels, roles, timestamps, warnings, disabled reasons, recovery actions, confirmation copy, and empty/loading/error/degraded/stale/unavailable copy are localizable with culture-aware formatting. Runtime sentence-fragment assembly is prohibited; complete resource strings use named placeholders. Resource ownership and RTL support remain unresolved.

**NFR-X7: Acceptance evidence.** UI work requires evidence for keyboard-only navigation, screen-reader use, automated accessibility, forced colors/high contrast, reduced motion, contrast, live-region announcements, focus return, hover-free disabled explanations, command-lock retention, reason honesty, and the named failure/safety scenarios. Responsive evidence spans desktop, tablet, mobile, and narrow-width overflow/navigation/dialog behavior.

**NFR-X8: Story readiness evidence.** A UI story cannot be ready until it cites applicable accessibility, localization, responsive, and `FC-DOC` evidence or records an approved row-specific fallback covering keyboard, focus, live regions, copy ownership, documentation evidence, replacement path, and owner approval.

**NFR-X9: Support safety.** No UI surface, label, log, toast, receipt, or copied value exposes bearer tokens, decoded JWT contents, command payloads, serialized event bodies, raw EventStore metadata, internal correlation IDs, stack traces, or real PII. Domain rejections are rendered as safe localized text. Only support-safe references are shareable; receipts use structured narrative data rather than raw payloads.

**NFR-X10: Privacy.** The UI displays only data the caller is authorized to see. Empty and error states do not reveal out-of-scope tenants, members, or memberships.

**NFR-X11: Responsive safety.** Breakpoints are mobile 320–767px, tablet 768–1023px, desktop 1024px+, and wide desktop 1440px+. Desktop is primary; tablet collapses navigation and stacks regions while preserving tables through scroll/column priority; mobile is read-only for triage, lookup, and audit. Identity, status, freshness, role, and risk columns never disappear. When width cannot preserve high-impact safety context, the action is unavailable with a visible reason.

**NFR-X12: Visual-system integrity.** Microsoft Fluent UI semantic theme roles are the visual authority. Meaning never relies on hard-coded colors; text plus icon or shape accompanies every status in light, dark, high-contrast, and forced-color modes. Layout reserves space to avoid shift, warning/destructive styling is restrained, and the initial slice excludes decorative or bespoke visual treatments unless promoted by Product/UX.

**Total non-functional requirements extracted: 17 (5 source-numbered and 12 assessment-normalized)**

### Additional Requirements

#### Cross-cutting interaction and business rules

**CP-1: Five truth dimensions.** Every actionable surface reasons over freshness, authorization, command lifecycle, projection confirmation, and audit evidence, surfaced through the 13-state Truth State Badge.

**CP-2: Fail closed.** Stale or unknown freshness, indeterminate authorization, incomplete Consequence Preview, or missing lifecycle support blocks access-impacting or destructive action unless an explicitly approved override exists. Aging data remains usable with friction. The user sees the specific Unavailable Action Reason.

**CP-3: Non-collapse invariant.** Accepted, projection-confirmed, and audit-available are distinct. Degraded and unable-to-verify are distinct success-prohibited states. Styling, copy, and announcements must never claim unconfirmed success.

**CP-4: Live signals are nudges.** Real-time projection notifications may trigger refresh but never prove projection confirmation or audit availability.

**CP-5: Consequence Preview.** Destructive and high-impact actions require a preview containing known consequences and known unknowns; missing inputs block submission.

**CP-6: Asymmetric high-risk behavior.** Removing the last tenant owner is allowed with elevated friction. Removing the last global administrator is domain-rejected and presented as unavailable. A target who also holds global-administrator authority raises separate platform-level friction. Tenant membership and global-administrator authority are never conflated.

**CP-7: Correct forward.** Recovery uses a new compensating command with its own preview and proof. Original history remains immutable. The words `undo`, `rollback`, and `hidden edit` are prohibited.

**CP-8: Recovery is explicit.** Stale data maps to refresh; pending to wait; status lookup failure to retry; permission failure to request/escalate; an incorrect change to correction/restoration; and unverifiable state to escalation with a support-safe reference. Workflows must not dead-end.

**CP-9: UI authorization is reflective.** Enforcement remains server-side.

**CP-10: Canonical state sets are verbatim contracts.** Surfaces must not reinterpret or merge the canonical Truth State Badge, freshness, lifecycle, layered-feedback, unavailable-reason, audit-availability, or recovery vocabularies. The deliberate space-form versus snake-case lifecycle tokens remain distinct.

#### Canonical state and recovery vocabularies

- **Truth State Badge (13):** `current`, `refreshing`, `aging`, `stale`, `unknown`, `eligible`, `blocked`, `pending`, `accepted`, `confirmed`, `failed`, `audit pending`, `audit available`.
- **Freshness (5):** `current`, `refreshing`, `aging`, `stale`, `unknown`; only `stale` and `unknown` block by default.
- **Command lifecycle (10):** `eligible`, `previewed`, `submitted`, `accepted`, `rejected`, `already applied`, `failed`, `duplicate`, `timeout`, `unknown`; the remove-user state machine additionally uses `projection_pending`, `confirmed`, `audit_pending`, and `audit_available`.
- **Layered feedback (10):** `request sent (submitted)`, `accepted`, `projection pending`, `confirmed`, `rejected`, `already applied`, `degraded`, `audit pending`, `audit available`, `unable to verify`. Degraded and unable-to-verify prohibit success presentation.
- **Unavailable Action Reasons (6):** `missing permission`, `stale data`, `missing lifecycle support`, `missing consequence preview`, `missing audit proof`, `high-impact flow not ready`.
- **Audit availability (4):** `audit pending`, `audit delayed`, `audit unavailable`, `missing implementation support`.
- **Permitted recovery language:** `start correction`, `restore intended access`, `retry status lookup`, `inspect audit`, `escalate`, `refresh`, `wait`, `continue read-only`, `request permission`, `start a compensating command`.

#### Consequence Preview content

Preview must cover owner-count impact including zero-owner state, the access changed or revoked, recovery path, expected audit evidence, input freshness, target platform standing such as global-administrator authority, and explicit known consequences versus known unknowns. Incomplete preview inputs block submission.

#### Backend and integration constraints

- Reads consume the existing tenant, tenant-user, user-tenant, and tenant-audit query surfaces. Commands use the existing command gateway; no new consequence, receipt, or command-status endpoints are introduced.
- The planned command route is `POST /api/v1/commands`, but deployed-gateway confirmation versus `/api/commands` remains open.
- Tenant and user identifiers are meaningful caller-supplied strings, not ULIDs. Only envelope identifiers such as `MessageId` may be ULIDs.
- Query cursors are signed, opaque, scope-bound, and never implemented as offset/limit. Durability across replicas/restarts is deferred; the UI must treat them as session-scoped until resolved.
- SignalR is a refresh nudge only. Projection re-query is required for confirmation.
- Domain rejections map to support-safe localized text; raw Problem Details internals and event data are not shown.
- Audit receipts are assembled client-side from structured `NarrativePayload`, not raw events.
- Direct add-by-user-ID is the only v1 membership-add mechanism; invitation workflows require future backend events.
- Shared UI capability belongs in FrontComposer. Tenants owns composition, columns, and routing, not reusable platform scaffolding.
- Confirmed FrontComposer capabilities are `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`. `FC-TBL` has an approved Tenants-specific boundary. `FC-AUD` and `FC-CNS` use approved flat-grid and inline-preview fallbacks. `FC-TOK` remains a missing shared capability with a Tenants canonical-vocabulary/Fluent-mapping fallback.

#### Domain rejection, NoOp, and emission rules

| Case | Required behavior |
|---|---|
| Add existing member | Reject as `UserAlreadyInTenant`; never present as already applied |
| Add member to tenant with no membership history | Allow owner bootstrap for last-owner recovery |
| Change to current role | NoOp; present as `already applied` |
| Role escalation or `TenantRole.Unknown` | Reject safely |
| Update tenant metadata | Always emit `TenantUpdated` after successful validation |
| Set identical configuration value | NoOp; present as `already applied` |
| Configuration over limit | Reject as `ConfigurationLimitExceeded` |
| Remove missing configuration key | Reject as `ConfigurationKeyNotFound` |
| Disable/enable to current state | Reject as `TenantLifecycleStateAlreadySet` |
| Command targets disabled tenant | Reject as `TenantDisabled` |
| Remove last tenant owner | Allow with elevated friction |
| Remove last global administrator | Reject as `LastGlobalAdministrator` and present as unavailable |
| Create existing tenant | Reject as `TenantAlreadyExists` |

#### Scope, phasing, and non-goals

- Phase 2a/MVP is the read-only foundation: FR-1 through FR-9 and FR-18, with truth/freshness, action-reason reflection, accessibility/localization/responsive evidence, and support safety.
- Phase 2b introduces FR-10, FR-11, FR-13, and FR-14.
- Phase 2c introduces high-impact, audit, and recovery requirements FR-12, FR-15 through FR-17, and FR-19 through FR-25.
- Mobile remains read-only for triage, lookup, and audit reference; high-impact mobile command flows are excluded.
- V1 excludes invitations, owner-specific screens/onboarding, hard tenant deletion, direct data-store repair, UI-side authorization enforcement, sensitive configuration display, grouped/session audit, anomaly scoring, advanced analytics, and bulk provisioning.
- Missing shared FrontComposer capability must not be implemented as Tenants-owned reusable infrastructure.

#### Assumptions and unresolved decisions

- Owner self-service uses shared authorization-scoped surfaces without dedicated owner screens.
- Programmatic integration continues through APIs rather than the human UI.
- Fluent semantic roles are the sole visual authority.
- Warm-projection performance and success-metric targets are provisional.
- WCAG 2.2 support is conditional; RTL and localization-resource ownership remain undecided.
- The audit area in the MVP remains undecided between hidden and a not-yet-available placeholder.
- Freshness cutoffs require product input.
- Cursor invalidation behavior across restart/replica changes is unresolved.
- Future display and authorization rules for sensitive configuration values are unresolved.
- Dedicated owner UX remains unfunded/unphased.
- Source UI specifications still require correction where they incorrectly call tenant or user IDs ULIDs.

### PRD Completeness Assessment

The PRD is structurally strong: it provides 25 globally numbered functional requirements, five explicitly numbered cross-cutting NFRs, canonical terminology and state vocabularies, testable consequences, phasing, non-goals, dependency readiness, domain behavior, and a technical addendum. The requirements make eventual-consistency honesty, support safety, authorization boundaries, and compensating recovery unusually explicit.

Completeness is reduced by several planning-contract issues that must remain visible during traceability:

- Twelve material quality requirements are specified outside the numbered NFR section. They have been assigned assessment-local `NFR-X*` identifiers so coverage cannot silently omit them.
- Several requirements retain assumptions rather than approved targets, especially performance budgets, WCAG 2.2 support, mobile scope, visual authority, and success metrics.
- The command endpoint, localization ownership, RTL scope, cursor invalidation behavior, audit-area MVP treatment, freshness thresholds, sensitive configuration display, and owner-specific UX remain open.
- The source-spec tenant/user ID discrepancy is an implementation hazard until the referenced specs are corrected.
- The document mixes completed implementation updates with forward requirements and retains resolved items in its Open Questions section, which weakens status clarity even though the individual notes usually identify their resolution.
- `FC-TOK` remains an unavailable shared component contract; its interim mapping discipline must be represented in story acceptance criteria.

The PRD is complete enough for coverage analysis, but it is not a blanket implementation-ready approval. Epic/story traceability must demonstrate coverage for both the 25 FRs and all 17 extracted NFR entries, and must preserve the cross-cutting CP rules.

## Epic Coverage Validation

### Epic FR Coverage Extracted

- **Epic 1 — Tenant Workspace Triage and Read-Only Insight:** FR-1 through FR-9
- **Epic 2 — Tenant Membership and Tenant Record Management:** FR-10 through FR-14
- **Epic 3 — Tenant Lifecycle and Configuration Control:** FR-15 through FR-17
- **Epic 4 — Global Administrator Governance:** FR-18 and FR-19
- **Epic 5 — Audit Evidence and Forward Recovery:** FR-20 through FR-25

The epic source spells identifiers as `FR1` through `FR25`; these correspond directly to PRD identifiers `FR-1` through `FR-25`.

### Coverage Matrix

| FR | PRD requirement | Epic and story coverage | Status |
|---|---|---|---|
| FR-1 | Browse, search, filter, sort, and cursor-page the authorization-safe tenant list with complete truth/list states | Epic 1, Story 1.2 | ✓ Covered |
| FR-2 | Open tenant detail and return with list context preserved; support deep links | Epic 1, Story 1.3 | ✓ Covered |
| FR-3 | Self-audit My Tenants with scoped roles and statuses | Epic 1, Story 1.4 | ✓ Covered |
| FR-4 | Look up a user's authorization-scoped tenant memberships | Epic 1, Story 1.5 | ✓ Covered |
| FR-5 | View tenant overview, metadata, counts, lifecycle, and freshness | Epic 1, Story 1.3 | ✓ Covered |
| FR-6 | View namespaced configuration read-only within authorization scope | Epic 1, Story 1.6 | ✓ Covered |
| FR-7 | Copy complete support-safe identifiers and references | Epic 1, Stories 1.3, 1.6, and 1.8 | ✓ Covered |
| FR-8 | Review accessible, read-only tenant member table | Epic 1, Story 1.7 | ✓ Covered |
| FR-9 | See action availability and inline canonical reasons | Epic 1, Story 1.7 | ✓ Covered |
| FR-10 | Add a user directly with explicit role and safe rejection handling | Epic 2, Story 2.2 | ✓ Covered |
| FR-11 | Change member role with NoOp, rejection, and projection confirmation | Epic 2, Story 2.3 | ✓ Covered |
| FR-12 | Remove a member with fail-closed preview, elevated friction, lifecycle tracking, and proof | Epic 2, Story 2.4 | ✓ Covered |
| FR-13 | Create a tenant with projection-confirmed outcome | Epic 2, Story 2.1 | ✓ Covered |
| FR-14 | Edit tenant metadata with emitted update and safe validation | Epic 2, Story 2.5 | ✓ Covered |
| FR-15 | Disable or enable a tenant through high-impact, projection-confirmed lifecycle control | Epic 3, Stories 3.1 and 3.2 | ✓ Covered |
| FR-16 | Set a configuration value with required preview, NoOp, and safe rejection | Epic 3, Story 3.3 | ✓ Covered |
| FR-17 | Remove a configuration key with required preview and confirmed outcome | Epic 3, Story 3.4 | ✓ Covered |
| FR-18 | Review global administrators from the fixed platform aggregate | Epic 4, Stories 4.1 and 4.2 | ✓ Covered |
| FR-19 | Grant/remove global administrators with fixed scope and last-admin protection | Epic 4, Stories 4.1, 4.3, and 4.4; correction verification in Story 5.7 | ✓ Covered |
| FR-20 | Browse flat, filtered, cursor-paginated tenant audit history | Epic 5, Story 5.1 | ✓ Covered |
| FR-21 | Reach scoped audit evidence from all required contexts | Epic 5, Story 5.2 | ✓ Covered |
| FR-22 | View a support-safe Audit Evidence Receipt from structured narrative data | Epic 5, Story 5.3 | ✓ Covered |
| FR-23 | Distinguish audit availability states with recovery | Epic 5, Stories 5.2, 5.3, and 5.4 | ✓ Covered |
| FR-24 | Start forward compensating correction from audit evidence | Epic 5, Stories 5.5, 5.7, and 5.8 | ✓ Covered |
| FR-25 | Preview correction against current state and link proof | Epic 5, Stories 5.6, 5.7, and 5.8 | ✓ Covered |

### Missing Requirements

No PRD functional requirement is missing from the epic coverage map or story-level `Requirements` fields.

No extra FR identifier appears in the epics without a matching PRD requirement. Story 3.5 is explicitly a defect-fix readiness record rather than a new functional requirement, and Story 5.8 supports existing FR-24/FR-25 rather than introducing a new FR.

### Coverage Statistics

- **Total PRD FRs:** 25
- **FRs claimed in the epic coverage map:** 25
- **FRs linked to concrete stories:** 25
- **Missing FRs:** 0
- **Extra epic-only FR identifiers:** 0
- **Functional-requirement coverage:** 100%

The epic-specific change proposals preserve this coverage map. They record retrospective closure, documentation reconciliation, or follow-up implementation evidence; none changes the set of functional requirements.

## UX Alignment Assessment

### UX Document Status

**Found.** The authoritative UX bundle contains:

- `ux-designs/ux-tenants-2026-06-02/DESIGN.md` — visual system and component appearance
- `ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md` — behavior, state, flow, accessibility, responsiveness, and readiness gates

The bundle has no `index.md`, but the two documents explicitly establish precedence: `EXPERIENCE.md` owns behavior and `DESIGN.md` owns visuals.

### UX ↔ PRD Alignment

Alignment is strong across the main product contract:

- UX implements all six PRD journeys and lands every required information-architecture surface.
- Tenant/operator/owner scope, authorization reflection, and the separate global-administrator domain match the PRD.
- Projection-confirmed success, SignalR nudge-only behavior, lifecycle non-collapse, fail-closed gating, and forward-only recovery match CP-1 through CP-10.
- The ten UX components cover tenant/member/audit grids, truth state, consequence preview, lifecycle feedback, unavailable reasons, receipts, and command controls required by the FRs.
- Responsive breakpoints, mobile read-only behavior, safety-column preservation, no-color-only status, localization, keyboard/focus behavior, live-region rules, and support safety align with PRD §§5, 8, 9, and 10.
- Approved `FC-AUD`, `FC-CNS`, and `FC-CNC` fallbacks align with the PRD and addendum.

### UX ↔ Architecture Alignment

The architecture materially supports the UX:

- Blazor InteractiveServer with a server-side BFF keeps tokens and unsafe backend data out of the browser.
- The BFF, Fluxor truth-state model, canonical `Vocabulary/` library, projection re-query confirmation, authorization-reflection service, and server-side support-safety layer directly implement the honesty contract.
- The proposed source tree gives every FR and all ten UX components an architectural home.
- Cursor pagination, ETag/304 freshness, opaque server-held cursors, list-state handling, responsive layout, Fluent/FrontComposer composition, `.resx` localization, stable selectors, bUnit, and Playwright are explicitly addressed.
- Architecture carries the UX performance targets, but leaves exact budgets and threshold tuning deferred.

### Alignment Issues

#### High: Architecture retains a superseded navigation model

The PRD and current UX require one Tenants module entry opening a workspace with **Tenants** and lookup-backed **Users** tabs; Global Administrators and Audit are contextual/module-internal, not separate left-menu entries.

The architecture's `Frontend Architecture > Shell composition` still says **Tenants / Global Administrators / Audit are primary and Users is contextual**, and the directory annotations repeat `nav area` language for Users and Global Administrators. This directly contradicts the current PRD/UX Option A model and could cause incorrect shell manifest or route implementation.

**Required correction:** update the architecture shell decision, directory annotations, route/manifest guidance, and any tests derived from them to the one-module-entry, two-tab workspace model.

#### High: UX requires `aging`, while architecture says the wire cannot currently produce it

PRD and UX define five freshness states and require `aging` to remain usable with friction. Architecture D6 states that the current wire emits only `current`, `stale`, and `unknown`; client `refreshing` is transient, and `aging` collapses to `current` until `QueryResponseMetadata.ProjectedAt` exists on the wire.

This is an explicit implementation gap, not merely terminology: the designed `aging` experience cannot occur from current metadata.

**Required resolution:** either add an owned platform handoff/story that exposes sufficient projection timestamp evidence, or record a Product/UX-approved temporary reduction with acceptance criteria that prevent the UI from claiming full five-state support.

#### Medium: Canonical recovery vocabulary differs between PRD and UX

The PRD addendum's canonical recovery set does not include `reassign tenant owner` or `retry access removal`, while `EXPERIENCE.md` adds both and still invokes CP-10's verbatim-vocabulary rule. A single vocabulary guard cannot treat both documents as canonical.

**Required resolution:** add the two verbs to the PRD/addendum canonical list or remove them from UX and use existing canonical terms.

#### Medium: Audit MVP behavior is chosen in UX but remains open in the PRD

PRD Open Question 9 asks whether the audit area should be hidden or show a not-yet-available placeholder. UX and architecture choose the placeholder. The product decision is therefore implemented downstream but still formally open upstream.

**Required resolution:** close the PRD question and record the placeholder decision, or revise UX/architecture if hiding is preferred.

#### Medium: Localization ownership is resolved in architecture but still open in UX/PRD

Architecture D4 assigns domain copy to Tenants-owned whole-string `.resx` resources while inheriting only shell chrome from FrontComposer. UX still labels resource ownership as open, and the PRD retains it as an open question.

**Required resolution:** synchronize the PRD and UX with D4 or reopen D4 explicitly.

#### Medium: Runtime and readiness status language is stale

`EXPERIENCE.md` still contains a lead section framed as a Blazor Auto lifecycle constraint, followed by a reconciliation note saying architecture supersedes it with InteractiveServer. The behavior invariants remain valid, but the obsolete runtime framing increases implementation ambiguity.

Architecture also continues to state in its final readiness assessment that build start is gated on `FC-LYT`/`FC-CMD`/`FC-CNC`, while earlier sections record those gates as closed by Story 1.0.

**Required correction:** promote InteractiveServer to the primary UX runtime statement and update the architecture readiness verdict to reflect closed gates and only current residual dependencies.

#### Medium: Command-route authority is inconsistent

Architecture fixes command submission at `POST /api/v1/commands`; the PRD retains confirmation versus `/api/commands` as an open question. This should have one authoritative answer before any future command-path work.

### Warnings

- Performance targets remain assumptions without final budgets or acceptance thresholds.
- Freshness thresholds remain configurable but product/operations tuning is unresolved.
- WCAG 2.2 AA remains conditional on the pinned Fluent/FrontComposer stack.
- UX requires RTL-ready logical layout while formal RTL shipping/testing remains deferred; acceptance ownership should be explicit.
- `FC-TOK` remains a missing shared capability. The Tenants canonical vocabulary plus verified Fluent mapping is the interim path, but unlike the three recorded fallbacks it does not have the same explicit approval record.
- The UX documents retain the older Fluent RC pin while the current FrontComposer project context reports a newer RC. Exact component, icon, token, and ARIA behavior must be verified against the dependency version actually consumed by Tenants.

### UX Alignment Verdict

The product experience is comprehensively designed and architecturally supportable, but the artifacts are **not fully aligned**. The navigation conflict and dormant `aging` state are the most implementation-significant gaps. The remaining issues are primarily authority/status synchronization, but they affect canonical vocabulary and downstream story interpretation and should be corrected before treating the planning set as cleanly implementation-ready.

## Epic Quality Review

### Epic Structure Validation

| Epic | User-value focus | Independence | Verdict |
|---|---|---|---|
| Epic 1 — Tenant Workspace Triage and Read-Only Insight | Clear read-only operational and self-service value | Delivers useful tenant discovery/review without later epics | Pass, with story-level sizing/enabler concerns |
| Epic 2 — Tenant Membership and Tenant Record Management | Clear tenant creation, metadata, and membership value | Uses Epic 1 foundations as allowed, but Story 2.4 depends on Epic 5 proof capability | **Fail independence for FR-12 as currently sliced** |
| Epic 3 — Tenant Lifecycle and Configuration Control | Clear lifecycle/configuration outcomes | Uses prior command foundation; no dependency on later epics | Pass |
| Epic 4 — Global Administrator Governance | Clear platform-governance outcome | Uses existing/prior foundations and gates its read contract before dependent stories | Pass |
| Epic 5 — Audit Evidence and Forward Recovery | Clear evidence and recovery outcome | Builds only on earlier audit/command/global-admin capabilities | Pass, with one technical-cleanup story concern |

No epic is merely a technical milestone. All five epic titles and goals describe capabilities a user can exercise.

### Story-by-Story Assessment

| Story | User value / independence / sizing | Acceptance criteria |
|---|---|---|
| 1.0 FrontComposer Shell Integration Spike | Timeboxed technical enabler with no direct user-facing value; explicit best-practice exception | Structured and evidence-oriented, but verification may be documentary rather than executable |
| 1.1 Tenants UI Host Bootstrap | Correct starter/bootstrap story; independently produces an honest shell state | Clear BDD, error/unavailable state, container, auth, and test coverage |
| 1.2 Tenant List Triage | User valuable but oversized: base grid, whole-set Memories search, authoritative hydration, filtering, fallback, indexing lag, cursor/freshness, authorization, responsive, and accessibility concerns are combined | Detailed and testable; breadth indicates multiple independently shippable slices |
| 1.3 Tenant Detail Navigation and Overview | Clear independent user slice using previous list foundation | Pass |
| 1.4 My Tenants Self-Audit View | Clear independent self-service slice | Pass |
| 1.5 User Membership Lookup | Clear independent operator slice | Pass |
| 1.6 Read-Only Tenant Configuration View | Clear independent read slice | Pass |
| 1.7 Member Table and Action Availability | Clear independent access-review slice | Pass |
| 1.8 Support-Safe Copy and Epic 1 Readiness Evidence | Mixes user-facing FR-7 behavior with process/readiness certification; depends on all prior Epic 1 surfaces | BDD is testable, but product and governance concerns should be separated |
| 2.1 Create Tenant with Projection-Confirmed Lifecycle | User-valued, but combines first command, BFF gateway, idempotency, lifecycle state, SignalR, re-query, rejection, audit handoff, localization, and accessibility foundations | Strong BDD and failure coverage; story is oversized without already-proven foundation evidence |
| 2.2 Add User with Explicit Role | Clear command slice using prior command foundation | Pass |
| 2.3 Change Member Role | Clear command slice with NoOp and rejection behavior | Pass |
| 2.4 Remove Member with Consequence Preview | **Not independently complete:** it claims FR-12 proof but explicitly waits for Epic 5 evidence source | Strong criteria otherwise; forward dependency is stated directly |
| 2.5 Edit Tenant Metadata | Clear command slice using prior command foundation | Pass |
| 3.1 Lifecycle Availability Guardrail | User sees honest availability and reasons; useful independently | Pass |
| 3.2 Disable/Enable Tenant | Clear high-impact command slice using prior guard/foundation | Pass |
| 3.3 Set Configuration | Clear command slice using prior foundation | Pass |
| 3.4 Remove Configuration | Clear command slice using prior foundation | Pass |
| 3.5 Query Gateway REST Routing record | Legitimate completed defect fix, but the canonical epic document contains only a summary and external evidence reference rather than a full story specification | Canonical epics lack its own BDD/test contract |
| 4.1 Global-Admin Navigation/Read Readiness | User-valued safe availability slice; properly gates the following read story | Pass |
| 4.2 Review Global Administrators | Clear user slice depending only on preceding contract verification | Pass |
| 4.3 Grant Global Administrator | Clear fixed-scope command slice | Pass |
| 4.4 Remove Global Administrator | Clear high-impact command slice with last-admin hard stop | Pass |
| 5.1 Tenant Audit Trail DataGrid | Clear evidence-browsing slice | Pass |
| 5.2 Scoped Audit Entry Points | Clear navigation/context slice using prior audit surface | Pass |
| 5.3 Audit Evidence Receipt | Clear support-safe evidence slice | Pass |
| 5.4 Audit Availability Recovery | Clear state/recovery slice | Pass |
| 5.5 Start Forward Correction | Clear recovery initiation slice using prior evidence | Pass |
| 5.6 Preview/Confirm Correction | Clear correction completion slice using prior start flow | Pass |
| 5.7 Global-Administrator Correction Verification | User-valued platform correction, large but focused and based on prior generic/fixed-scope foundations | Strong BDD; acceptable only because prior foundation evidence is explicit |
| 5.8 Correction Projection Refresh Cleanup | Pure technical efficiency/cleanup story with indirect value, not a user-capability story | Testable and bounded, but should be typed as technical debt/enabler rather than presented as a normal user story |

### Dependency Analysis

#### 🔴 Critical violation: Story 2.4 has a forward dependency on Epic 5

Story 2.4 states that audit evidence may be “not yet implemented by Epic 5,” only shows `audit available` or a receipt once Epic 5 exists, and instructs tests not to assert proof before the Epic 5 evidence source exists. Yet it claims full FR-12, whose requirement includes proof via audit.

This violates both story independence and the rule that Epic 2 must function from Epic 1 output without needing Epic 5.

**Remediation options:**

1. Move the audit-proof-complete portion of FR-12 into Epic 5 and narrow Story 2.4's claimed requirement to a formally split prerequisite requirement; or
2. Deliver the minimum support-safe evidence source inside Story 2.4 so FR-12 completes without future work; or
3. Move the whole FR-12 story to Epic 5 if audit proof is indivisible from the user outcome.

An honest `audit unavailable` state is good runtime behavior, but it does not remove a planning-time forward dependency when the story claims proof as delivered value.

#### 🟠 Major issue: Story 1.2 has an unowned cross-repository readiness dependency

Architecture states that full Memories-backed search is gated on the Memories server handoff for ingestion, attribute indexing/filtering, and `tenants-index` registration. Story 1.2 nevertheless includes the full search behavior without a prerequisite status, owner, or independently tracked dependency in the epic.

**Remediation:** split the cursor-list/grid slice from Memories search/hydration/fallback, and attach a verifiable external prerequisite with owner and completion evidence to the search slice.

#### Backward dependencies that comply

- Epic 2 consumes Epic 1 shell/read foundations.
- Epic 3 consumes Epic 2 command lifecycle.
- Epic 4 Story 4.2 follows Story 4.1 read-contract verification.
- Epic 5 audit/correction stories build sequentially from audit list → entry points/receipt → recovery → correction.
- Story 5.7 consumes completed Epic 4 fixed-scope global-administrator commands.
- No story depends on a higher-numbered story within its own epic.

### Story Sizing and Structure Findings

#### 🟠 Major: Oversized foundation stories

- **Story 1.2** combines list composition, cross-domain search integration, hydration, filtering, degradation, freshness, authorization, responsive behavior, accessibility, and extensive test infrastructure.
- **Story 2.1** combines the first user command with most of the reusable command platform. Its retrospective evidence says the foundation was ultimately delivered, but the planning slice remains larger than a normal independently reviewable story.

**Recommendation:** separate reusable foundations only when they can be paired with a thin vertical user slice; otherwise divide by observable outcome, such as base list versus whole-set search, or command submission/status versus projection confirmation/evidence handoff.

#### 🟠 Major: Technical stories presented as normal user stories

- Story 1.0 is explicitly a technical spike with no user-facing value.
- Story 5.8 is a projection-refresh efficiency cleanup.

These may be legitimate backlog items, but should be labeled and governed as enabler/technical-debt work, with the user stories retaining independent product value.

#### 🟡 Minor: Mixed product and governance concern

Story 1.8 combines support-safe copy behavior with Epic 1 readiness certification. The readiness-evidence work should be a completion gate or separate governance task, not part of the user-facing story's identity.

#### 🟡 Minor: Story 3.5 is not fully represented in the canonical epics artifact

The defect-fix is referenced as completed, but its story, BDD criteria, and test contract live only in an external implementation artifact. Include a canonical story summary or explicit link contract if `epics.md` is expected to be self-contained.

### Acceptance Criteria Quality

Overall acceptance-criteria quality is high:

- All 30 explicit story sections use Given/When/Then structure.
- Happy, rejection, authorization, freshness, degraded, support-safety, accessibility, responsive, and test scenarios are unusually complete.
- Test contracts identify unit/component/API/Playwright coverage and observable outcomes.
- Stable selectors, focus, forced-colors, live-region, and non-collapse behavior are consistently included.

Concerns:

- Story 1.0 allows documentary evidence without a consistently reproducible verification command.
- Several stories use broad prerequisite phrases such as “support is confirmed” without naming one machine-checkable gate in the story itself.
- The epic artifact mixes completed implementation evidence, historical status, defect records, and future-plan language, weakening its role as a clean canonical implementation handoff.

### Requirements Traceability Quality

FR traceability is complete at 25/25. NFR traceability is not clean:

- The PRD explicitly defines only NFR-1 through NFR-5.
- The epics artifact introduces NFR6 through NFR10 for accessibility, localization, support safety/privacy, responsive behavior, and readiness evidence.
- The PRD also contains additional unnumbered quality requirements, normalized in this assessment as `NFR-X1` through `NFR-X12`.

Story references such as `NFR2-NFR10` therefore use an epic-local numbering scheme that is not canonical in the PRD.

**Remediation:** establish one canonical NFR registry in the PRD/addendum and update epic/story references, or include an explicit mapping table from epic NFR6–NFR10 to their source sections and the remaining unnumbered requirements.

### Starter, Brownfield, and Data Checks

- **Starter requirement:** Architecture specifies a manual `.NET 10` Blazor InteractiveServer starter. Story 1.1 correctly creates `src/Hexalith.Tenants.UI`, adds it to `.slnx`, composes FrontComposer/Fluent, wires AppHost/auth/BFF seams, and uses SDK container support. Story 1.0 precedes it only as a timeboxed integration spike.
- **Brownfield integration:** Existing API, DAPR, SignalR, Keycloak/JWT, FrontComposer, AppHost, package, and domain boundaries are named explicitly.
- **Data creation timing:** No UI-owned database or new table is planned; the application consumes projections, so database/entity timing violations do not apply.
- **CI/test ownership gap:** Architecture requires a new `UI.Tests` project and Playwright tier, but no story clearly owns creating both test projects and registering their CI lanes. Test contracts assume that infrastructure exists.

### Best-Practices Verdict

- **Epics delivering user value:** 5/5 pass
- **Epic independence:** 4/5 pass; Epic 2 fails because Story 2.4 needs Epic 5 proof
- **Explicit stories with BDD criteria:** 30/30
- **FR traceability:** 25/25
- **Forward dependencies:** 1 confirmed critical violation; 1 major unowned external dependency
- **Oversized stories:** 2 clear cases
- **Technical/mixed-concern stories:** 3 concerns
- **Database timing:** not applicable; no violation
- **Starter/bootstrap coverage:** present
- **CI/test-infrastructure ownership:** incomplete

The epic set is substantially better than average, but it does not meet strict implementation-readiness standards until the Story 2.4 forward dependency, Memories prerequisite ownership, and NFR traceability namespace are corrected.

## Summary and Recommendations

### Overall Readiness Status

## NOT READY

The planning set is not ready to serve as an unambiguous Phase 4 implementation contract.

This is not a requirements-coverage failure: all 25 functional requirements map to epics and concrete stories, the UX is comprehensive, the architecture has strong trust/safety foundations, and acceptance criteria are unusually detailed. The failure is **contract coherence and executable sequencing**. A team following the current artifacts can implement the wrong navigation, cannot produce one required freshness state, and cannot complete Story 2.4 without a later epic that the story explicitly depends on.

### Critical Issues Requiring Immediate Action

1. **Repair FR-12 / Story 2.4 sequencing.** Story 2.4 claims audit proof while explicitly waiting for Epic 5. Move proof into the same independently complete slice, formally split the requirement, or relocate the story so it has no forward dependency.
2. **Correct architecture navigation.** Replace the superseded Tenants/Global-Administrators/Audit primary-navigation model with the current one-module-entry Tenants workspace containing Tenants and lookup-backed Users tabs, with Global Administrators and Audit contextual/module-internal.
3. **Resolve the dormant `aging` freshness state.** Expose sufficient projection timestamp evidence through an owned platform story, or obtain Product/UX approval for a temporary reduced state model and stop claiming full five-state behavior.

### Other High-Priority Corrections

4. **Own and gate the Memories search prerequisite.** Split Story 1.2 so whole-set search/hydration has a named cross-repository owner, prerequisite status, and completion evidence separate from the base cursor-list slice.
5. **Create one canonical NFR registry.** Number the PRD's accessibility, localization, responsive, privacy, support-safety, visual, and readiness requirements, then update all epic/story references. Do not retain competing PRD, epic-local, and assessment-local namespaces.
6. **Unify canonical recovery vocabulary.** Decide whether `reassign tenant owner` and `retry access removal` are canonical, then synchronize PRD/addendum, UX, vocabulary guards, resources, and stories.
7. **Close downstream-decided PRD questions.** Record the audit placeholder decision and Tenants-owned localization boundary upstream; reconcile command route authority and cursor/freshness behavior.
8. **Clean runtime and gate status.** Make InteractiveServer the primary UX statement and remove architecture claims that build start is still blocked by FrontComposer contracts already recorded as closed.
9. **Reslice oversized stories.** Separate Story 1.2's base list from Memories search and split Story 2.1's reusable command foundation where evidence does not already exist.
10. **Classify technical work honestly.** Mark Stories 1.0 and 5.8 as enabler/technical-debt items; separate Story 1.8's product copy behavior from readiness certification.
11. **Assign CI/test infrastructure ownership.** Add an explicit story/task for creating `Hexalith.Tenants.UI.Tests`, Playwright infrastructure, and their CI lanes before downstream test contracts rely on them.
12. **Restore canonical story completeness.** Represent Story 3.5's BDD/test contract in `epics.md` or define an explicit canonical link contract to its implementation artifact.

### Recommended Next Steps

1. Run a focused planning correction covering the three blocking contracts: FR-12 slicing, navigation IA, and freshness-state capability.
2. Update PRD/addendum first for canonical NFRs, recovery vocabulary, audit behavior, localization ownership, and command-route authority.
3. Update UX and architecture from that corrected product contract, removing superseded runtime/navigation/readiness text.
4. Update `epics.md`: repair Story 2.4, split Story 1.2, normalize NFR references, classify enablers, and assign Memories/CI prerequisites.
5. Re-run implementation readiness and require zero critical dependency violations plus one coherent navigation, freshness, NFR, and recovery vocabulary contract.

### Final Note

This assessment identifies **16 actionable issues across four categories**:

- artifact authority and PRD/UX/architecture alignment;
- epic/story independence and sizing;
- requirements/vocabulary traceability;
- delivery, dependency, and test-infrastructure ownership.

The strongest evidence is the 100% FR coverage and detailed safety-focused acceptance criteria. The blocking evidence is the confirmed forward dependency and contradictory canonical contracts. Correct those before using these artifacts to authorize new implementation work.

**Assessment date:** 2026-07-14  
**Assessor:** Codex — BMAD Implementation Readiness workflow
