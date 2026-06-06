---
created: 2026-06-06T18:16:08+02:00
baseline_commit: 8a24128d
---

# Story 5.5: Start Forward Correction from Audit Evidence

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 5.5. -->

## Story

As an authorized operator,
I want to start a forward correction from audit evidence,
so that I can restore intended access or begin a correction without editing historical events.

## Acceptance Criteria

1. Given an authorized user opens an audit evidence receipt for a correctable tenant-domain outcome, when correction actions are available, then the UI offers forward recovery actions such as `restore intended access` or `start correction`, and labels, tooltips, announcements, and copy never use `undo`, `rollback`, or `hidden edit`.
2. Given the correction relates to tenant membership, when the user starts correction, then the UI prepares a new tenant-domain command such as `AddUserToTenant` or `ChangeUserRole` with an explicit intended role where required, and it does not edit the original event, projection, or state store.
3. Given the correction relates to global administrator authority, when the user starts correction, then the UI keeps the correction in the `global-administrators` scope using `SetGlobalAdministrator` or `RemoveGlobalAdministrator`, and it does not edit a tenant aggregate or tenant membership.
4. Given authorization, freshness, current state, audit evidence, or command support is indeterminate, when correction availability is evaluated, then the action fails closed with a visible unavailable reason, and the original evidence remains visible without implying correction success.
5. Given a correction action is started from audit evidence, when the correction flow opens, then it carries a link to the original evidence record, current projection snapshot, intended command type, and required preview data, and it does not submit automatically.
6. Given correction actions are rendered in a receipt or audit row, when users operate them by keyboard, screen reader, or narrow viewport, then each action has visible focus, accessible name, forced-colors support, stable footprint, and selectors such as `data-testid="tenants-correction-start"`, and unavailable actions show inline localized reasons.
7. Given this story is complete, when verification is run, then unit/component tests cover tenant correction command selection, global-administrator correction command selection, explicit role requirement, unavailable reasons, forbidden terminology, original evidence linking, and no history mutation.
8. Given accessibility or E2E verification is run, then keyboard start flow, focus behavior, screen-reader labels, forced-colors rendering, support-safe copy, and stable selectors are verified.

## Tasks / Subtasks

- [x] Add a correction-start intent model under `src/Hexalith.Tenants.UI/State/TenantAudit/` (AC: 1, 2, 3, 4, 5, 7)
  - [x] Model correctable audit outcomes from existing `TenantAuditReceipt` / `TenantAuditRow` data without parsing raw event payloads or EventStore metadata.
  - [x] Include the original audit reference, tenant scope, target user or key, outcome/event type, current projection snapshot reference, intended command domain, intended command type, and required preview inputs.
  - [x] Keep tenant membership correction intents in the `tenants` domain and global-administrator correction intents in the `global-administrators` domain.
  - [x] Require an explicit intended role for restore/change-role paths; do not infer a role from `UserRemovedFromTenant` because removal events do not carry the old role.
  - [x] Represent indeterminate authorization, freshness, current projection, audit evidence, missing command support, missing role, unsupported outcome, and global-admin command readiness as distinct unavailable reasons.
  - [x] Do not add command submission, command polling, projection confirmation, audit proof linking, or backend mutation in this model. Story 5.6 owns preview, submit, and linked proof.

- [x] Add a correction-start UI action for audit receipts and audit rows (AC: 1, 4, 5, 6, 8)
  - [x] Extend `AuditEvidenceReceipt.razor` to render correction-start actions only when the receipt is ready, support-safe, current enough, and correctable.
  - [x] Add a row-level correction entry point in `AuditDataGrid.razor` only if the same eligibility model can produce a safe unavailable reason; otherwise keep correction start on the receipt only.
  - [x] Use visible localized labels such as `restore intended access` or `start correction`; never use prohibited copy: `undo`, `rollback`, or `hidden edit`.
  - [x] Use `data-testid="tenants-correction-start"` for enabled actions and stable reason selectors such as `data-testid="tenants-correction-unavailable-reason"` for blocked actions.
  - [x] Preserve existing receipt selectors and behavior: `tenants-audit-receipt`, `tenants-audit-receipt-state`, `tenants-audit-receipt-copy`, and `tenants-audit-receipt-reference`.
  - [x] Preserve `AuditAvailabilityState` behavior for pending/delayed/unavailable/missing-support evidence; a correction action must not turn those states into Success.
  - [x] Keep focus on or return focus to the launching receipt or row when the correction start panel opens, closes, or blocks.

- [x] Open a non-submitting correction-start handoff surface (AC: 2, 3, 4, 5, 6, 8)
  - [x] Add a focused Tenants UI surface, for example `Components/Tenants/Audit/CorrectionStartPanel.razor`, or an equivalent local component under the audit surface.
  - [x] Show original evidence reference, current projection snapshot summary, intended command type/domain, required preview data, unavailable reasons, and a clear handoff to preview.
  - [x] Do not call `ITenantCommandGateway`, `POST /api/v1/commands`, or `GET /api/v1/commands/status/{correlationId}` from this story.
  - [x] If global-administrator command gateway support is absent, show a blocked global-admin correction-start state with a visible reason rather than adding a partial command path.
  - [x] If current tenant/member/global-admin projection evidence is stale, unknown, absent, or unauthorized, keep the original evidence visible and block correction start until refresh/re-query produces eligible evidence.
  - [x] Keep mobile/narrow viewport correction start read-only or fail-closed if full safety context cannot fit.

- [x] Derive tenant membership correction command selection safely (AC: 2, 4, 5, 7)
  - [x] For a mistaken removal/restoration path, prepare an `AddUserToTenant` intent only when tenant id, target user id, and explicit intended `TenantRole` are present and valid (`TenantOwner`, `TenantContributor`, or `TenantReader`, never `Unknown`).
  - [x] For a wrong-role path, prepare a `ChangeUserRole` intent only when tenant id, target user id, current projection evidence, and explicit intended new role are present.
  - [x] If the target is already in the intended state, surface a safe already-applied/unavailable reason and hand off to inspect audit or continue read-only rather than preparing a stale correction.
  - [x] If the tenant is disabled or lifecycle status is unknown, block with visible reason; most member/configuration corrections reject disabled tenants.
  - [x] Do not parse tenant or user identifiers as GUIDs or ULIDs; URI-escape only for navigation.

- [x] Keep global-administrator correction routing distinct (AC: 3, 4, 5, 7)
  - [x] Recognize global-administrator authority outcomes separately from tenant membership outcomes.
  - [x] Prepare a `SetGlobalAdministrator` or `RemoveGlobalAdministrator` intent only in the `global-administrators` domain with singleton aggregate scope, not in a tenant aggregate.
  - [x] Use existing global administrator read evidence from `GlobalAdministratorsPage` / `GlobalAdministratorsSnapshot` where available; do not infer global-admin state from tenant membership rows.
  - [x] If story 4.x command implementation remains read-only or blocked, display command-support unavailable copy and keep the original evidence visible.
  - [x] Do not implement last-admin removal preview, global-admin command submission, or platform governance confirmation in this story unless those foundations already exist and are directly reused.

- [x] Add Tenants-owned localization and support-safety coverage (AC: 1, 4, 6, 7, 8)
  - [x] Add EN/FR `.resx` keys under a `Tenants.Correction.*` or equivalent root for action labels, accessible names, unavailable reasons, original evidence labels, current-state labels, and preview-handoff copy.
  - [x] Use whole localized strings with named placeholders; do not assemble visible sentences from fragments or leak enum names/machine tokens into copy.
  - [x] Add static guards that rendered correction copy does not contain `undo`, `rollback`, `hidden edit`, raw payload, bearer token, JWT, stack trace, correlation id, raw EventStore metadata, protected cursor, ETag, MessageId, or PII.
  - [x] Keep support-safe references copyable only through existing `SupportSafeCopyButton` / `SupportSafeCopyClassifier` patterns.

- [x] Add focused tests and validation (AC: 1-8)
  - [x] Add state tests for correctable tenant membership outcomes, global-administrator outcomes, unsupported outcomes, stale/unknown projection, missing role, missing command support, and forbidden role `TenantRole.Unknown`.
  - [x] Add component tests for `AuditEvidenceReceipt` and/or the new correction-start component covering enabled and unavailable correction actions, original evidence linking, focus callbacks, accessible names, forced-colors/focus CSS hooks, stable selectors, and no automatic submission.
  - [x] Add audit grid tests if row-level correction actions are added.
  - [x] Add resource parity tests for every new EN/FR correction key.
  - [x] Add static guard tests for prohibited terminology, no raw payload/diagnostic leakage, no browser backend calls, no local/session storage usage, and no history mutation language.
  - [x] Run focused validation first: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`.
  - [x] If `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest issue, run the xUnit v3 executable fallback for the UI test assembly.

### Review Follow-ups (AI)

- [x] [AI-Review][Med] Receipt no longer renders a correction unavailable-reason for uncorrectable outcomes; mirror `AuditDataGrid` `UnsupportedOutcome` suppression so opening any normal receipt (e.g. `UserAddedToTenant`) shows no confusing "not supported for correction" copy. [`AuditEvidenceReceipt.razor`]
- [x] [AI-Review][Med] Tenant-domain membership correction now fails closed when audit evidence lacks a tenant id or target user id, instead of preparing a command against an empty aggregate (AC4). [`TenantCorrectionStartIntent.cs:170`]
- [ ] [AI-Review][High] Live `TenantAuditPage` can never present an *enabled* correction-start: `TenantCorrectionStartIntent.FromReceipt` hardcodes `IntendedRole`/`CurrentRole` to null and `HasGlobalAdministratorCommandSupport` to false, so every real audit row is fail-closed and `CorrectionStartPanel` / `OnStartCorrection` are unreachable outside unit tests. AC1/AC2/AC3/AC5 are satisfied only at the model/test layer. Resolving this requires an operator role-selection affordance and global-administrator command support, both deferred (Story 5.6 / Epic 4); confirm with the team whether 5.5 should ship with correction always fail-closed in the running UI. [`TenantCorrectionStartIntent.cs:154`, `TenantAuditPage.razor:379`]
- [ ] [AI-Review][Low] Programmatic focus return to the launching receipt/row on panel open/close/block is not implemented (callback-only). This matches the existing audit-recovery pattern and CSS `:focus-visible` is present, but the task subtask explicitly calls for focus return. [`AuditEvidenceReceipt.razor`, `CorrectionStartPanel.razor`]
- [ ] [AI-Review][Low] `FromReceipt` builds `CurrentProjectionSnapshotReference` as `"{Scope}@{ProjectionMarker}"`, embedding the `TenantFreshnessState` enum name; if the panel becomes reachable this surfaces a machine token in the snapshot field. Map to a localized whole phrase when wiring 5.6. [`TenantCorrectionStartIntent.cs:164`]
- [ ] [AI-Review][Low] `AlreadyApplied` is detected only on the `ChangeUserRole` path, not on the `AddUserToTenant` restore path; if a removed user has already been re-added, restore would not surface an already-applied reason. Unreachable today (CurrentRole is null in the live path). [`TenantCorrectionStartIntent.cs:170`]

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 5 Story 5.5. Epic 5 covers audit evidence and forward recovery; this story starts a correction from audit evidence but does not preview, submit, confirm, or link corrective proof. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.5: Start Forward Correction from Audit Evidence`]
- FR24 requires authorized users to start a compensating command from audit evidence. The correction is a new forward command with its own preview and proof, never an edit to the original event. [Source: `_bmad-output/planning-artifacts/epics.md#FR24`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-24`]
- Story 5.6 owns correction preview against current state, command submission, projection-confirmed correction, and original/corrective record linking. Do not pull Story 5.6 scope into this story. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.6: Preview and Confirm Correction with Linked Proof`]
- UX recovery vocabulary is closed and casing-sensitive: use `start correction`, `restore intended access`, `retry status lookup`, `inspect audit`, `escalate`, plus general paths such as `wait`, `refresh`, and `continue read-only`. Prohibited terms are `undo`, `rollback`, and `hidden edit`. [Source: `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#5. Compensating-Recovery Language and Flow`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#5.4 Concurrency and recovery cases`]

### Existing Implementation To Extend

- `TenantAuditRow` is the support-safe audit row projection used by the UI. It is built from `TenantAuditEntry` and whitelists narrative keys such as `userId`, `key`, `role`, `oldRole`, `newRole`, and `previousRole`; it already blocks unsafe markers including bearer/token/correlation/cursor/ETag/raw payload/metadata text. Extend this safe mapping; do not read raw event payloads. [Source: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRow.cs`]
- `TenantAuditReceipt` derives actor, target, scope, outcome, timestamp, projection marker, audit reference, optional command reference, and copyable reference text. It maps `TenantCommandAuditState` into receipt states through `TenantAuditAvailability`. Preserve this derivation and add correction-start eligibility around it. [Source: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs`]
- `AuditEvidenceReceipt.razor` renders the receipt, support-safe copy button, non-ready states, and shared `AuditAvailabilityState`. It already delegates refresh/close callbacks and must keep receipt fields visible while adding correction-start actions. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor`]
- `AuditAvailabilityState` and `TenantAuditAvailability` distinguish pending, delayed, unavailable, and missing-support audit states with recovery verbs. A correction-start control must not replace or collapse those states. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor`; `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailability.cs`]
- `TenantAuditPage` owns tenant-scoped audit loading, ETag reuse, cursor paging, UTC filter parsing, return context, safe return URLs, receipt query parameters, and selected receipt resolution. Keep those behaviors intact; add only correction-start state/handoff needed by this story. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`]
- `AuditDataGrid` currently renders flat audit rows, support-safe reference copy, and receipt open action. If row-level correction actions are added, preserve the safety-critical columns, UTC timestamp rendering, selectors, and support-safe reference behavior. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`]
- `AuditEvidenceEntryPoint` already builds scoped audit links with `tenantId`, `targetUserId`, `supportSafeCommandReference`, `source`, `returnUrl`, and `returnFocus`. Do not create a competing audit route or unsafe return-url behavior. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceEntryPoint.razor`]
- `GlobalAdministratorsPage` is currently a read-only review surface with unavailable grant/remove reasons. Treat global-admin correction start as a distinct prepared intent or blocked state unless command support already exists. [Source: `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`]

### Backend And Data Boundary

- The consume-only backend surface is fixed: `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, `GET /api/tenants/{tenantId}/audit`, `POST /api/v1/commands`, and `GET /api/v1/commands/status/{correlationId}`. This story must not add backend correction, receipt, preview, command-specific evidence, or proof-link endpoints. [Source: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- Browser-side components must not call backend APIs directly or store backend tokens; all egress stays server-side through BFF gateways. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]
- `ITenantCommandGateway` currently exposes tenant-domain command methods for create/add/change/remove/update/configuration/lifecycle and no global-admin command methods. Do not fake global-admin command readiness in UI copy or tests. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`]
- `AddUserToTenant`, `ChangeUserRole`, and `RemoveUserFromTenant` are tenant-domain commands. `SetGlobalAdministrator` and `RemoveGlobalAdministrator` are global-admin commands in the `global-administrators` domain. Keep aggregate identity and copy separate. [Source: `src/Hexalith.Tenants.Contracts/Commands/AddUserToTenant.cs`; `src/Hexalith.Tenants.Contracts/Commands/ChangeUserRole.cs`; `src/Hexalith.Tenants.Contracts/Commands/RemoveUserFromTenant.cs`; `src/Hexalith.Tenants.Contracts/Commands/SetGlobalAdministrator.cs`; `src/Hexalith.Tenants.Contracts/Commands/RemoveGlobalAdministrator.cs`]
- Tenant ids and user ids are meaningful caller-supplied strings. Preserve literal values and URI-escape only for navigation; never `Guid.TryParse` or `Ulid.TryParse` them. [Source: `_bmad-output/project-context.md#Identity Rules`]

### Correction-Specific Rules

- Corrections are forward compensating commands only. The original event, projection, and state-store entry remain immutable and visible. [Source: `docs/compensating-commands.md#Compensating Commands`; `_bmad-output/project-context.md#Event-Sourcing & Domain Rules`]
- Restoring a wrongly removed user uses a new `AddUserToTenant` with explicit `TenantRole`. Removal events do not carry the old role, and old audit/history may be stale, so the operator must provide the intended current role. [Source: `docs/compensating-commands.md#Correction: AddUserToTenant With Explicit TenantRole`]
- Correcting a wrong role uses `ChangeUserRole` with explicit `NewRole`; same-role requests can become NoOp and should not be represented as a successful correction start. [Source: `docs/compensating-commands.md#Wrong Role Assignment`]
- Tenant lifecycle correction uses `EnableTenant` after accidental disable or `DisableTenant` after accidental enable; most member/configuration commands reject disabled tenants with `TenantDisabledRejection`, so correction start must evaluate current lifecycle evidence before preparing a member/configuration correction. [Source: `docs/compensating-commands.md#Tenant Lifecycle Correction`]
- Global-administrator correction must use the singleton `global-administrators` domain and never edit a tenant aggregate or tenant membership. Last-admin and global-admin command confirmation remain platform-governance concerns from Epic 4/Story 5.6. [Source: `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#5.4 Keep command domains distinct in recovery copy`; `_bmad-output/planning-artifacts/epics.md#Story 5.5: Start Forward Correction from Audit Evidence`]

### UX, Accessibility, And Localization Guardrails

- Reserve Success for projection-proven truth or audit-available evidence only. Starting a correction is not success, confirmation, or proof. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR3`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#5.2 Non-collapse invariant`]
- Fail closed before a correction-start handoff when validation, freshness, authorization, current state, audit evidence, command support, or required preview data is indeterminate. Show inline visible reasons; tooltips may supplement but cannot be the only explanation. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR26`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#4. Unavailable Action Reason Pattern`]
- Use Tenants-owned `.resx` resources, whole strings with named placeholders, and EN/FR parity. Do not leak enum names, machine tokens, or raw source kind values into visible copy or accessible names. [Source: `_bmad-output/planning-artifacts/architecture.md#Localization keys`; `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#4. Localization and Message Composition Requirements`]
- Every correction action must include visible text, accessible name, visible focus, forced-colors support, keyboard operation, stable layout footprint, and stable selectors. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.5: Start Forward Correction from Audit Evidence`; `_bmad-output/planning-artifacts/epics.md#UX-DR25`; `_bmad-output/planning-artifacts/epics.md#UX-DR33`]
- High-impact command flows are unavailable on mobile/narrow contexts if full safety context cannot be preserved. This story may show the original evidence read-only and defer correction start with a visible reason. [Source: `_bmad-output/planning-artifacts/epics.md#NFR9`; `_bmad-output/planning-artifacts/epics.md#UX-DR31`]

### Previous Story Intelligence

- Story 5.4 added the shared `TenantAuditAvailability` model and `AuditAvailabilityState` component, then refactored receipts and command flows to reuse it. Reuse those instead of creating a second audit availability model. [Source: `_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md#Completion Notes List`]
- Story 5.4 review accepted that unavailable/missing-support audit states expose continue-read-only/escalate rather than inspect-audit. Preserve that recovery design unless this story has actual eligible correction-start evidence. [Source: `_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md#Senior Developer Review (AI)`]
- Story 5.3 added `TenantAuditReceipt`, `AuditEvidenceReceipt`, receipt safe-copy behavior, loaded-row receipt opening from `AuditDataGrid`, and query-param receipt handling from `TenantAuditPage`. Extend these rather than replacing them. [Source: `_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md#Previous Story Intelligence`]
- Story 5.2 fixed raw source-kind token leakage by mapping machine source tokens to localized whole phrases. Apply the same rule to correction-start source/outcome/domain labels. [Source: `_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md#Previous Story Intelligence`]
- Story 5.1 fixed server-local timezone dependence in audit timestamp rendering and filter parsing. Preserve UTC behavior in any correction-start timestamp or evidence label. [Source: `_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md#Previous Story Intelligence`]
- Recent commits are story-scoped Conventional Commits: `8a24128 feat(story-5.4): Audit Availability State Recovery`, `a5ca6e3 feat(story-5.3): Support-Safe Audit Evidence Receipt`, `77bb935 feat(story-5.2): Scoped Audit Evidence Entry Points`, and `497a4ac feat(story-5.1): Tenant Audit Trail DataGrid`. A compatible implementation commit would be `feat(story-5.5): start forward correction from audit evidence`. [Source: `git log --oneline -5`]
- Current dirty work before creating this story included only `_bmad-output/story-automator/orchestration-1-20260605-153745.md`. It is unrelated and must not be reverted. [Source: `git status --short`]

### Latest Technical Information

- Use the repo-pinned stack and existing local APIs: .NET 10, Blazor InteractiveServer, Fluent UI Blazor, FrontComposer shell, EventStore query/command gateways, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not add packages or package versions. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `Directory.Packages.props`]
- External package research is not required for this story because implementation relies on existing repo-pinned components and local Tenants/EventStore contracts. The primary risks are scope creep into Story 5.6, false Success, domain conflation, unsafe copy, localization gaps, accessibility regressions, and backend endpoint invention.

### Project Structure Notes

- Expected UI additions: `src/Hexalith.Tenants.UI/State/TenantAudit/` for correction-start eligibility/intent models and `src/Hexalith.Tenants.UI/Components/Tenants/Audit/` for the correction-start action/panel.
- Expected UI updates: `AuditEvidenceReceipt.razor`, possibly `AuditDataGrid.razor`, `TenantAuditPage.razor`, `TenantsResources.resx`, and `TenantsResources.fr.resx`.
- Expected tests: `tests/Hexalith.Tenants.UI.Tests/State/`, `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs`, possible `TenantAuditPageTests.cs` / `AuditDataGrid` component tests, resource parity tests, and static guard tests.
- Avoid backend contract/projection changes, `TenantAuditEntry` wire-shape changes, `GetTenantAuditQueryHandler`, EventStore server registration, AppHost/Aspire plumbing, package metadata, shared FrontComposer code, submodule changes, and command gateway submission changes unless a compile-time integration need is proven and remains in Story 5.5 scope.
- Do not add generic recovery infrastructure, an audit timeline, grouped audit mode, correction preview, command submission, projection confirmation, proof linking, or cross-domain support-safety scaffolding to Tenants.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 5.5: Start Forward Correction from Audit Evidence`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 5: Audit Evidence and Forward Recovery`
- PRD: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-24`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`; `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`; `_bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules`
- Audit/recovery spec: `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#5. Compensating-Recovery Language and Flow`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#7. Support-Safe References, Accessibility, and Localization Contracts`
- Truth-state spec: `docs/tenants-ui-truth-state-and-action-availability-spec.md#4. Unavailable Action Reason Pattern`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#5. Layered Feedback State Set (AC4)`
- Compensating commands: `docs/compensating-commands.md`
- Existing code: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditRow.cs`; `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs`; `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailability.cs`; `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`; `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`; `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`
- Existing tests: `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs`; `tests/Hexalith.Tenants.UI.Tests/State/TenantAuditAvailabilityTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`; `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-06T18:21:00+02:00 - Story and sprint status moved from ready-for-dev to in-progress; existing `baseline_commit: 8a24128d` preserved.
- 2026-06-06T18:23:00+02:00 - Red-phase UI state tests added, then focused UI build failed on missing correction intent types as expected.
- 2026-06-06T18:29:00+02:00 - `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- 2026-06-06T18:30:00+02:00 - `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build` hit the known .NET 10 Microsoft.Testing.Platform/VSTest issue.
- 2026-06-06T18:30:00+02:00 - xUnit v3 executable fallback `./tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed: 593 total, 0 failed.
- 2026-06-06T18:32:00+02:00 - All six test projects built individually in Release with `-m:1 --no-restore`; 0 warnings and 0 errors.
- 2026-06-06T18:32:00+02:00 - Contracts, Client, and Testing xUnit executable suites passed before Server.Tests stopped on 6 pre-existing documentation/configuration failures around missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and unrelated deployment-readiness summary expectations.
- 2026-06-06T18:33:00+02:00 - IntegrationTests attempted separately; 34 DAPR-dependent tests skipped for missing local DAPR prerequisites and 54 integration tests failed with DaprException/InternalServerError in the current environment.
- 2026-06-06T18:33:00+02:00 - Final focused UI validation rerun passed: 593 total, 0 failed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Checklist validation was run against `.agents/skills/bmad-create-story/checklist.md`; corrections from validation are reflected in explicit no-submit/no-proof-linking boundaries, existing audit primitive reuse, global-admin command readiness guardrails, support-safety, accessibility, localization, and focused test expectations.
- Added a Tenants-owned correction-start intent model that derives safe correction availability from `TenantAuditReceipt` and `TenantAuditRow`, keeps tenant membership commands in the `tenants` domain, keeps global-administrator commands in the `global-administrators` domain, requires explicit non-Unknown intended roles, and represents fail-closed unavailable reasons distinctly.
- Extended audit receipt and row surfaces with localized correction-start actions, inline unavailable reasons, stable selectors, accessible names, focus/forced-colors styling, and no backend command submission.
- Added a non-submitting `CorrectionStartPanel` that carries original evidence reference, current projection snapshot, intended command domain/type, required preview inputs, and blocked-state reasons while preserving Story 5.6 scope for preview/submit/proof linking.
- Added EN/FR localization keys for correction action labels, fields, preview inputs, command/domain labels, roles, and unavailable reasons.
- Added focused state, receipt, grid, panel, parity, static guard, and accessibility-style tests covering tenant membership command selection, global-admin routing/support blocking, explicit role requirements, forbidden terminology, no backend calls, no browser storage, original evidence linking, and stable selectors.
- Story implementation tasks are complete, but the story remains in-progress rather than review because full regression validation is blocked by pre-existing/non-story Server.Tests and IntegrationTests failures in this checkout.

### File List

- _bmad-output/implementation-artifacts/5-5-start-forward-correction-from-audit-evidence.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor.css
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor.css
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor.css
- src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx
- src/Hexalith.Tenants.UI/Resources/TenantsResources.resx
- src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs
- tests/Hexalith.Tenants.UI.Tests/Components/AuditDataGridCorrectionTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/CorrectionStartPanelTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs
- tests/Hexalith.Tenants.UI.Tests/State/TenantCorrectionStartIntentTests.cs

### Change Log

- 2026-06-06T18:16:08+02:00 - Created Story 5.5 context and marked it ready for development.
- 2026-06-06T18:21:00+02:00 - Marked Story 5.5 in progress.
- 2026-06-06T18:33:18+02:00 - Implemented correction-start intent, audit receipt/grid actions, non-submitting handoff panel, localization, and focused tests.
- 2026-06-06 - Senior Developer Review (AI) completed; auto-fixed 2 medium findings (receipt UnsupportedOutcome suppression, membership fail-closed on missing identifiers), added 2 regression tests, recorded 1 high + 3 low follow-ups, set status to done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (adversarial AI review)
**Date:** 2026-06-06
**Outcome:** Approve with follow-ups (0 critical; 1 high, 2 medium auto-fixed, 3 low recorded)

### Scope and method

Validated every story File List entry against git reality (all source/test files match; only `_bmad-output/` artifacts were dirty-but-undocumented, which is excluded from review). Cross-checked all 8 ACs and every `[x]` task against the implementation, confirmed the audit event-type literals in `TenantCorrectionStartIntent` (`UserRemovedFromTenant`, `UserRoleChanged`, `GlobalAdministratorSet`/`Removed`) exactly match what `TenantAuditReadModel.GetEventType` stamps into `TenantAuditEntry.EventType` (feature is correctly wired, not inert), and re-ran the focused UI suite via the xUnit v3 executable fallback.

### Verification

- `dotnet build tests/Hexalith.Tenants.UI.Tests` (Release, `-m:1 --no-restore`): **0 warnings, 0 errors**.
- UI test executable: **597 passed, 0 failed, 0 skipped** (595 pre-existing + 2 new regression tests).
- Server.Tests / IntegrationTests failures noted in the Dev Agent Record are pre-existing and environmental (missing `DaprComponents/pubsub.yaml`, DAPR prerequisites) and out of this story's UI scope.

### Findings

- **[High][Open]** Live `TenantAuditPage` never presents an *enabled* correction-start. `TenantCorrectionStartIntent.FromReceipt` hardcodes `IntendedRole`/`CurrentRole = null` and `HasGlobalAdministratorCommandSupport = false`, so every real audit row is fail-closed and the `CorrectionStartPanel` + `OnStartCorrection` handoff is unreachable outside unit tests. The fail-closed ACs (AC4/AC6) hold, but the enabled branches of AC1/AC2/AC3/AC5 are exercised only at the model/test layer. Full resolution needs an operator role-selection affordance and global-admin command support — both deferred (Story 5.6 / Epic 4) — so this is recorded as a follow-up rather than auto-fixed to avoid scope creep into 5.6. **Team confirmation requested** on shipping 5.5 with correction always fail-closed in the running UI.
- **[Medium][Fixed]** The receipt rendered a correction "not supported for correction start" reason for any non-correctable outcome, while `AuditDataGrid` deliberately suppresses `UnsupportedOutcome`. Because `UserAddedToTenant` is a real, common audit type, opening a normal receipt showed confusing copy. Aligned the receipt with the grid (`ShouldRenderCorrection` / `HasRenderableUnavailableReason`) and added a regression test.
- **[Medium][Fixed]** Membership correction (`AddUserToTenant`/`ChangeUserRole`) did not verify that tenant id and target user id are present, contrary to the task's "only when tenant id, target user id … are present and valid." Added a fail-closed guard (`AuditEvidenceUnavailable`) plus a regression test that keeps the receipt Ready (Scope present) but omits the tenant id.
- **[Low][Open]** Focus return is callback-only (no programmatic `FocusAsync`); matches the existing audit-recovery pattern and CSS `:focus-visible`.
- **[Low][Open]** `CurrentProjectionSnapshotReference` embeds the `TenantFreshnessState` enum name; map to a localized phrase when 5.6 makes the panel reachable.
- **[Low][Open]** `AlreadyApplied` is detected only on the `ChangeUserRole` path, not the `AddUserToTenant` restore path (unreachable today).

### Strengths

Clean fail-closed design with distinct localized unavailable reasons; correct tenant vs `global-administrators` domain separation with singleton aggregate scope; explicit non-`Unknown` role requirement; EN/FR resource parity (42/42 correction keys); static guards for prohibited terminology, diagnostic leakage, backend calls, and browser storage; UTC-safe timestamps preserved; no backend endpoints invented; reuses Story 5.4 `TenantAuditAvailability` instead of duplicating it.
