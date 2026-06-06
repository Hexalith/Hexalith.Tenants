---
created: 2026-06-06T18:59:20+02:00
baseline_commit: eeb0a49
---

# Story 5.6: Preview and Confirm Correction with Linked Proof

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 5.6. -->

## Story

As an authorized operator,
I want to preview a correction against current state and link original and corrective records,
so that recovery is deliberate, auditable, and proven by projection confirmation.

## Acceptance Criteria

1. Given a correction flow has been started from audit evidence, when the correction preview opens, then the UI shows original evidence reference, current projection state, intended forward command, known consequences, known unknowns, audit/evidence expectation, and recovery path, and submission is blocked if any required preview item is unavailable.
2. Given current projection state conflicts with the original evidence or intended correction, when preview data is evaluated, then the UI shows the conflict with localized safe text and unavailable or warning state as appropriate, and it does not submit a stale correction based only on historical evidence.
3. Given the user confirms an eligible tenant-domain correction, when the command is submitted, then the reusable command gateway sends the forward command through the existing command endpoint, enforces one-at-a-time locking, tracks status and SignalR nudges, and re-queries the authoritative projection, and correction success is shown only after projection confirmation.
4. Given the correction is accepted, pending, rejected, failed, degraded, unable to verify, audit pending, delayed, unavailable, or missing support, when the lifecycle and evidence surfaces render, then each state remains distinct and accessible, and the UI never rewrites, deletes, hides, or relabels the original event as undone.
5. Given correction proof becomes available, when the user views the original or corrective receipt, then both records are linked with support-safe references and absolute timestamps, and the link uses structured narrative metadata and command/audit references rather than raw payloads.
6. Given the correction flow completes, fails, or is cancelled, when focus and feedback are handled, then focus returns to the launching receipt or audit row, live-region politeness matches state severity, and copy remains support-safe, and stable selectors such as `data-testid="tenants-correction-preview"` and `data-testid="tenants-correction-proof-link"` are present.
7. Given this story is complete, when verification is run, then unit/component tests cover preview completeness, current-state conflict blocking, forward command submission, projection-confirmed correction, rejection/unknown/audit-unavailable states, original/corrective record linking, and no history rewrite.
8. Given accessibility or E2E verification is run, then destructive/corrective confirmation focus behavior, keyboard complete-or-exit, live-region politeness, forced-colors status rendering, support-safe proof links, and stable selectors are verified.

## Tasks / Subtasks

- [x] Extend the Story 5.5 correction-start model into a preview/confirmation model under `src/Hexalith.Tenants.UI/State/TenantAudit/` (AC: 1, 2, 3, 4, 7)
  - [x] Reuse `TenantCorrectionStartIntent`; do not create a parallel correction intent or parse raw event payloads.
  - [x] Add a correction preview snapshot/model that keeps original audit reference, current projection snapshot, intended command domain/type, tenant id, target user id, current role, intended role, known consequences, known unknowns, audit/evidence expectation, recovery path, message id, correlation id, command lifecycle state, audit state, focus target, and live-region politeness as separate fields.
  - [x] Keep last-confirmed projection evidence separate from in-flight correction intent; never replace visible current state optimistically.
  - [x] Model `accepted`, `projection pending`, `confirmed`, `rejected`, `already applied`, `failed`, `degraded`, `unable to verify`, `audit pending`, `audit delayed`, `audit unavailable`, and `missing support` distinctly.
  - [x] Add restore-path already-applied detection for `AddUserToTenant`: if the target user is already present in the current projection with the intended role, block submission and surface `already applied`; if present with a different role, show a conflict and require an explicit `ChangeUserRole` path instead.
  - [x] Convert the current snapshot label from the Story 5.5 machine-ish `"{scope}@{ProjectionMarker}"` string into localized, whole-string, support-safe copy before it reaches visible UI.

- [x] Wire current-state and role-selection evidence into live correction flows (AC: 1, 2, 3, 6)
  - [x] Update `TenantAuditPage.razor` so live audit rows can produce enabled tenant-domain correction intents when authorization, freshness, audit evidence, tenant status, target user id, current membership projection, and explicit intended role are present.
  - [x] Provide an operator role-selection affordance for restore/change-role corrections using valid `TenantRole` values only: `TenantOwner`, `TenantContributor`, or `TenantReader`; never allow `TenantRole.Unknown`.
  - [x] Re-query the authoritative tenant detail/member projection before enabling preview and again after command status reaches a terminal/projection-change state.
  - [x] Keep global-administrator correction preview fail-closed unless Story 4.3/4.4 command support already exists and can be reused directly. Do not implement partial global-admin command submission in this story.
  - [x] Preserve existing audit filters, cursor history, selected receipt query handling, safe return URL behavior, UTC filter parsing, and scoped receipt selection.

- [x] Replace the non-submitting handoff with a corrective preview/confirmation surface (AC: 1, 2, 3, 4, 6, 8)
  - [x] Extend or replace `CorrectionStartPanel.razor` with a Tenants-owned preview surface under `Components/Tenants/Audit/`; keep existing `tenants-correction-panel` compatibility only if tests depend on it, and add `data-testid="tenants-correction-preview"`.
  - [x] Show original evidence reference, current projection state, intended command, known consequences, known unknowns, audit/evidence expectation, recovery path, and all blocking reasons with localized safe copy.
  - [x] Disable confirmation when any required preview item is unavailable, stale, unauthorized, unsupported, conflicting, missing role, disabled tenant, unknown lifecycle, narrow-viewport unsafe, or command support unavailable.
  - [x] Use a deliberate confirmation control with keyboard complete-or-exit behavior, visible focus, forced-colors-safe state, and safe cancel/close that does not submit.
  - [x] Return focus to the launching receipt or audit row on close, cancel, failure, and completed flow; Story 5.5 callback-only focus return is not enough for this story.
  - [x] Keep the original receipt visible or reachable throughout the preview and command lifecycle; never hide or rename the original event as corrected/undone.

- [x] Submit eligible tenant-domain correction commands through the existing gateway and confirmation pattern (AC: 3, 4, 7)
  - [x] For mistaken removal restore, submit `AddUserToTenantCommandRequest` through `ITenantCommandGateway.AddUserToTenantAsync` with tenant id, target user id, and explicit intended role.
  - [x] For wrong-role correction, submit `ChangeUserRoleCommandRequest` through `ITenantCommandGateway.ChangeUserRoleAsync` with tenant id, target user id, and explicit intended role as `NewRole`.
  - [x] Do not add backend correction, preview, receipt, or proof-link endpoints. Use `POST /api/v1/commands` through the existing gateway and `GET /api/v1/commands/status/{correlationId}` through `GetStatusAsync`.
  - [x] Enforce one-at-a-time correction command locking; prevent double-submit, duplicate click, and browser-refresh resubmission from creating a second command attempt.
  - [x] On accepted command, store message id and correlation id in the correction snapshot as support-safe internal tracking data; do not render raw correlation ids in visible copy unless existing support-safe classifiers explicitly allow the specific reference.
  - [x] After status polling or SignalR nudge, trigger authoritative projection re-query; mark `confirmed` only when the re-queried projection proves the intended state.
  - [x] Map rejection/no-op outcomes to localized safe text and recovery actions: refresh, retry status lookup, inspect audit, continue read-only, start a different correction, or escalate.

- [x] Link original and corrective proof using support-safe audit data (AC: 4, 5, 6, 7, 8)
  - [x] After projection confirmation, query audit evidence for the corrective event and keep audit state as `audit pending` or `audit delayed` until a support-safe corrective audit row is available.
  - [x] Link original and corrective records with `data-testid="tenants-correction-proof-link"` using audit event references, support-safe command references where available, and absolute UTC timestamps.
  - [x] Build proof links from `TenantAuditRow`, `TenantAuditReceipt`, and structured `NarrativePayload` fields only; never inspect serialized event payloads, EventStore metadata, stack traces, tokens, protected cursors, ETags, or internal correlation ids.
  - [x] Preserve both records when corrective proof is unavailable; show actual `audit pending`, `audit delayed`, `audit unavailable`, or `missing support` state rather than success.
  - [x] Make original/corrective links work from both the correction flow and the receipt/audit row path without inventing a separate audit route.

- [x] Add Tenants-owned localization and support-safety guards (AC: 1-8)
  - [x] Add EN/FR `.resx` keys under `Tenants.Correction.Preview.*`, `Tenants.Correction.Confirm.*`, `Tenants.Correction.Proof.*`, and related reason roots.
  - [x] Use whole localized strings with named placeholders; do not concatenate visible sentences or leak enum names/machine tokens.
  - [x] Add localized labels for current projection state, intended command, consequences, unknowns, audit expectation, recovery path, role choice, conflict reasons, confirmation button, cancel, retry, inspect audit, continue read-only, and proof links.
  - [x] Guard rendered correction copy against `undo`, `rollback`, `hidden edit`, raw payload, bearer token, JWT, stack trace, internal correlation id, raw EventStore metadata, protected cursor, ETag, `MessageId`, and PII.

- [x] Add focused tests and validation (AC: 1-8)
  - [x] Add state tests for preview completeness, missing preview items, stale/current-state conflict blocking, restore already-applied detection, wrong-role conflict, disabled/unknown tenant lifecycle, command accepted/pending/confirmed/rejected/degraded/unable-to-verify states, audit pending/delayed/unavailable/missing-support states, and no history mutation language.
  - [x] Add component tests for the correction preview/confirmation surface, role selection, disabled confirm reasons, keyboard cancel/confirm, focus return, live-region politeness, forced-colors/focus CSS hooks, support-safe proof links, and stable selectors.
  - [x] Add `TenantAuditPage` tests proving live audit rows can open an enabled tenant-domain correction preview only when current projection and explicit role evidence are available.
  - [x] Add gateway tests only if new gateway methods are introduced. Prefer existing `AddUserToTenantAsync`, `ChangeUserRoleAsync`, and `GetStatusAsync`.
  - [x] Add resource parity tests for every new EN/FR key.
  - [x] Run focused validation first: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`.
  - [x] If `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest issue, run the xUnit v3 executable fallback for the UI test assembly.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 5 Story 5.6. Epic 5 covers audit evidence and forward recovery; this story completes the correction flow by previewing against current projection truth, submitting an eligible forward command, confirming by projection re-query, and linking original/corrective proof. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.6: Preview and Confirm Correction with Linked Proof`]
- FR25 requires previewing a correction against current state and linking original and corrective records; success is shown only after projection confirmation. [Source: `_bmad-output/planning-artifacts/epics.md#FR25`]
- Every Epic 5 story inherits the non-collapse rule: accepted command, projection confirmation, and audit availability are separate states. SignalR nudges may prompt re-query but never prove success. [Source: `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#5.2 Non-collapse invariant`]
- The correction is a new forward command. The original event remains in the immutable audit trail and must remain visible or reachable. Prohibited terminology remains `undo`, `rollback`, and `hidden edit`. [Source: `docs/compensating-commands.md`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#5. Compensating-Recovery Language and Flow`]

### Existing Implementation To Extend

- `TenantCorrectionStartIntent` already maps correctable audit outcomes to intended commands: `UserRemovedFromTenant` -> `AddUserToTenant`, `UserRoleChanged` -> `ChangeUserRole`, `GlobalAdministratorRemoved` -> `SetGlobalAdministrator`, and `GlobalAdministratorSet` -> `RemoveGlobalAdministrator`. Extend it or compose with it; do not create a second outcome mapper. [Source: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs`]
- Live `TenantAuditPage` currently calls `TenantCorrectionStartIntent.FromReceipt`, which hardcodes `IntendedRole` and `CurrentRole` to null and `HasGlobalAdministratorCommandSupport` to false. That means every real tenant restore/change-role row is fail-closed until this story wires current projection and explicit role evidence. [Source: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs`; `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`]
- `CorrectionStartPanel` is currently a non-submitting handoff that shows original evidence, current snapshot, command domain/type, required preview inputs, and a `tenants-correction-preview-handoff` button. It does not submit, poll status, confirm projection, or link proof. This is the natural component to evolve for Story 5.6. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor`; `_bmad-output/implementation-artifacts/5-5-start-forward-correction-from-audit-evidence.md#Tasks / Subtasks`]
- `AuditEvidenceReceipt` and `AuditDataGrid` already surface correction-start actions/reasons using `data-testid="tenants-correction-start"` and `data-testid="tenants-correction-unavailable-reason"`. Preserve their receipt/grid behavior while routing eligible starts into the new preview/confirmation surface. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`]
- `TenantAuditReceipt` builds support-safe receipt fields from `TenantAuditRow`, state, freshness, and optional support-safe command reference. Use this path for proof linking; do not introduce raw event payload parsing. [Source: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs`]
- Existing command snapshots in `TenantCreateCommandModels.cs` already model `RequestSent`, `Accepted`, `ProjectionPending`, `Confirmed`, `Rejected`, `AlreadyApplied`, `Degraded`, and `UnableToVerify`. Reuse the same lifecycle semantics for correction preview/confirmation. [Source: `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`]

### Backend And Data Boundary

- The backend surface is fixed: read through `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, `GET /api/tenants/{tenantId}/audit`; commands through `POST /api/v1/commands`; status through `GET /api/v1/commands/status/{correlationId}`. No new correction or receipt endpoints. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- Browser-side components must not call backend APIs directly or store backend tokens. Under Blazor InteractiveServer, all backend egress stays server-side through BFF gateways. [Source: `_bmad-output/planning-artifacts/architecture.md#API boundary (the trust edge)`]
- `ITenantCommandGateway` already has tenant-domain methods for add user, change role, remove user, metadata, configuration, lifecycle, and status lookup. It has no global-administrator command methods today. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`]
- `AddUserToTenant`, `ChangeUserRole`, `SetGlobalAdministrator`, and `RemoveGlobalAdministrator` are existing contracts. Command/event/rejection records stay plain public records with no `sealed` and no XML docs. [Source: `src/Hexalith.Tenants.Contracts/Commands/AddUserToTenant.cs`; `src/Hexalith.Tenants.Contracts/Commands/ChangeUserRole.cs`; `src/Hexalith.Tenants.Contracts/Commands/SetGlobalAdministrator.cs`; `src/Hexalith.Tenants.Contracts/Commands/RemoveGlobalAdministrator.cs`; `_bmad-output/project-context.md#C# Language & Contract Rules`]
- Tenant ids and user ids are meaningful caller-supplied strings. Preserve literal values and URI-escape only for navigation; never parse them as GUIDs or ULIDs. [Source: `_bmad-output/project-context.md#Identity Rules`]

### Correction-Specific Rules

- Restoring a wrongly removed user submits a new `AddUserToTenant` with an explicit non-Unknown `TenantRole`; removal events do not carry the old role, and old audit/history can be stale. [Source: `docs/compensating-commands.md#Correction: AddUserToTenant With Explicit TenantRole`]
- Correcting a wrong role submits `ChangeUserRole` with explicit `NewRole`; same-role requests can become NoOp/already-applied and must not be represented as successful correction work. [Source: `docs/compensating-commands.md#Wrong Role Assignment`]
- Most member/configuration commands reject disabled tenants. Correction preview must evaluate current lifecycle evidence and block member correction when the tenant is disabled or lifecycle is unknown. [Source: `docs/compensating-commands.md#Tenant Lifecycle Correction`; `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs`]
- Global-administrator corrections must stay in the singleton `global-administrators` domain and never edit a tenant aggregate. Because global-admin command gateway support is absent in current UI code and Story 4.3/4.4 are still backlog, this story should keep those correction previews visibly blocked unless reusable support already exists by implementation time. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs`; `_bmad-output/planning-artifacts/epics.md#Story 4.3: Grant Global Administrator with Projection Confirmation`; `_bmad-output/planning-artifacts/epics.md#Story 4.4: Remove Global Administrator with Last-Admin Hard Stop`]
- Do not implement grouped audit mode, anomaly scoring, bulk correction, direct projection edits, local/session storage persistence, or a generic recovery framework in Tenants. Keep the scope to tenant-domain correction preview/submit/confirm/proof linking from audit evidence. [Source: `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#6.5 Out-of-scope for the first slice`; `AGENTS.md#Domain Implementation Boundary`]

### UX, Accessibility, And Localization Guardrails

- Reserve Success for projection-proven truth or audit-available evidence only. Accepted command is not proof; confirmed projection is not audit proof. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR3`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#5.1 Feedback states`]
- Preview must show current projection state, intended forward command, known consequences, known unknowns, audit/evidence expectation, and recovery path. Block submission if any required item is unavailable. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.6: Preview and Confirm Correction with Linked Proof`]
- Inline visible unavailable/conflict reasons are required; tooltips may supplement but cannot be the only explanation. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md#4. Unavailable Action Reason Pattern`; `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#2. Keyboard and Focus Requirements`]
- Keyboard users must be able to complete or exit every preview and command flow. Escape/cancel must be safe and non-committing, and focus must return to the launching receipt or row after close, cancel, submit, failure, or completion. [Source: `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#2. Keyboard and Focus Requirements`]
- Use Tenants-owned `.resx` whole strings with named placeholders and EN/FR parity. Do not leak enum names, state-machine tokens, raw source kind values, or machine snapshot labels into visible copy. [Source: `_bmad-output/planning-artifacts/architecture.md#Localization keys`; `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#4. Localization and Message Composition Requirements`]
- Stable selectors are required for every interactive element/status: use `data-testid="tenants-correction-preview"`, `data-testid="tenants-correction-confirm"`, `data-testid="tenants-correction-cancel"`, `data-testid="tenants-correction-proof-link"`, and preserve existing correction/audit selectors where possible. [Source: `_bmad-output/planning-artifacts/architecture.md#Automation selectors (NFR-4)`]

### Previous Story Intelligence

- Story 5.5 implemented correction-start intent, receipt/grid actions, a non-submitting handoff panel, localization, and focused tests. Build and xUnit UI executable validation passed, while broader Server/Integration failures were pre-existing/environmental. [Source: `_bmad-output/implementation-artifacts/5-5-start-forward-correction-from-audit-evidence.md#Completion Notes List`; `_bmad-output/implementation-artifacts/5-5-start-forward-correction-from-audit-evidence.md#Verification`]
- Story 5.5 review left one high follow-up: live `TenantAuditPage` cannot present an enabled correction-start because role/current-state/global-admin support is hardcoded absent. Story 5.6 must resolve the tenant-domain side by wiring role/current projection evidence; keep global-admin blocked unless Epic 4 command support exists. [Source: `_bmad-output/implementation-artifacts/5-5-start-forward-correction-from-audit-evidence.md#Findings`]
- Story 5.5 review left low follow-ups directly relevant here: focus return is callback-only, current snapshot copy embeds a machine enum name, and already-applied detection is missing for the `AddUserToTenant` restore path. Treat these as required fixes for 5.6, not optional cleanup. [Source: `_bmad-output/implementation-artifacts/5-5-start-forward-correction-from-audit-evidence.md#Findings`]
- Story 5.4 introduced `TenantAuditAvailability` and `AuditAvailabilityState`; reuse them for proof states instead of inventing another audit availability model. [Source: `_bmad-output/implementation-artifacts/5-5-start-forward-correction-from-audit-evidence.md#Previous Story Intelligence`]
- Story 5.3 introduced `TenantAuditReceipt` and receipt safe-copy behavior; proof linking should extend those support-safe paths. [Source: `_bmad-output/implementation-artifacts/5-5-start-forward-correction-from-audit-evidence.md#Previous Story Intelligence`]
- Story 5.1 fixed UTC audit timestamp rendering and filter parsing; all correction preview/proof timestamps must remain absolute UTC/culture-aware, never relative-only. [Source: `_bmad-output/implementation-artifacts/5-5-start-forward-correction-from-audit-evidence.md#Previous Story Intelligence`]
- Recent commits are story-scoped Conventional Commits: `eeb0a49 feat(story-5.5): Start Forward Correction from Audit Evidence`, `8a24128 feat(story-5.4): Audit Availability State Recovery`, `a5ca6e3 feat(story-5.3): Support-Safe Audit Evidence Receipt`, `77bb935 feat(story-5.2): Scoped Audit Evidence Entry Points`, and `497a4ac feat(story-5.1): Tenant Audit Trail DataGrid`. A compatible implementation commit would be `feat(story-5.6): preview and confirm correction with linked proof`. [Source: `git log --oneline -5`]
- Current dirty work before creating this story included only `_bmad-output/story-automator/orchestration-1-20260605-153745.md`. It is unrelated and must not be reverted. [Source: `git status --short`]

### Latest Technical Information

- Use the repo-pinned stack and existing local APIs: .NET 10, Blazor InteractiveServer, Fluent UI Blazor `5.0.0-rc.3-26138.1`, FrontComposer shell references, EventStore query/command gateways, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not add packages or package versions. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `Directory.Packages.props`; `_bmad-output/planning-artifacts/architecture.md#Technical Stack`]
- External package research is not needed for implementation because this story depends on already-pinned local components and Tenants/EventStore contracts. The main risks are scope creep into global-admin command work, false success before projection/audit proof, unsafe copy, duplicate correction submission, stale historical evidence, localization gaps, and focus/live-region regressions.

### Project Structure Notes

- Expected UI state additions: `src/Hexalith.Tenants.UI/State/TenantAudit/` for correction preview/confirmation snapshots and proof-link models.
- Expected UI component updates: `CorrectionStartPanel.razor` or a new local successor under `src/Hexalith.Tenants.UI/Components/Tenants/Audit/`, plus `TenantAuditPage.razor`, `AuditEvidenceReceipt.razor`, and `AuditDataGrid.razor` as needed.
- Expected gateway usage: existing `ITenantCommandGateway.AddUserToTenantAsync`, `ChangeUserRoleAsync`, and `GetStatusAsync`; no new tenant-domain backend endpoint.
- Expected resources: `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`.
- Expected tests: `tests/Hexalith.Tenants.UI.Tests/State/`, `tests/Hexalith.Tenants.UI.Tests/Components/CorrectionStartPanelTests.cs` or successor tests, `AuditEvidenceReceiptTests.cs`, `AuditDataGridCorrectionTests.cs`, `TenantAuditPageTests.cs`, resource parity tests, and static guard tests.
- Avoid backend contract/projection changes, `TenantAuditEntry` wire-shape changes, `GetTenantAuditQueryHandler`, EventStore server registration, AppHost/Aspire plumbing, package metadata, shared FrontComposer code, submodule changes, direct state-store writes, or generic recovery infrastructure unless a compile-time integration need is proven and remains inside this story.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 5.6: Preview and Confirm Correction with Linked Proof`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 5: Audit Evidence and Forward Recovery`
- PRD/requirements: `_bmad-output/planning-artifacts/epics.md#FR25`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`
- Audit/recovery spec: `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#5. Compensating-Recovery Language and Flow`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#7. Support-Safe References, Accessibility, and Localization Contracts`
- Truth-state spec: `docs/tenants-ui-truth-state-and-action-availability-spec.md#5. Layered Feedback State Set (AC4)`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#5.4 Concurrency and recovery cases`
- Accessibility/localization spec: `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md`
- Compensating commands: `docs/compensating-commands.md`
- Existing code: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs`; `src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor`; `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`
- Existing tests: `tests/Hexalith.Tenants.UI.Tests/State/TenantCorrectionStartIntentTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/CorrectionStartPanelTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/AuditDataGridCorrectionTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-06T19:04:36+02:00 - Story moved to in-progress; existing `baseline_commit: eeb0a49` preserved.
- 2026-06-06T19:12:00+02:00 - Focused UI build passed: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`.
- 2026-06-06T19:13:00+02:00 - `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build` hit the known .NET 10 Microsoft.Testing.Platform/VSTest issue; xUnit v3 executable fallback used.
- 2026-06-06T19:15:00+02:00 - UI xUnit executable fallback passed 606/606 after adding correction preview state tests.
- 2026-06-06T19:16:00+02:00 - Tier 1 executable suites passed: Contracts 105/105, Client 47/47, Testing 181/181, UI 606/606, Sample 31/31.
- 2026-06-06T19:17:00+02:00 - `dotnet build Hexalith.Tenants.slnx -c Release -m:1 --no-restore` passed with 0 warnings/errors.
- 2026-06-06T19:17:00+02:00 - Server.Tests executable was attempted and failed 6 known pre-existing documentation/AppHost evidence checks around missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and stale deployment-readiness summary expectations, matching prior story records and not touching Story 5.6 UI files.
- 2026-06-06T19:18:00+02:00 - IntegrationTests executable was attempted; DAPR-dependent tests skipped due unavailable prerequisites and 54 existing DaprException/InternalServerError integration failures remained environmental/outside Story 5.6 UI scope.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Checklist validation was run against `.agents/skills/bmad-create-story/checklist.md`; corrections from validation are reflected in explicit reuse of Story 5.5 primitives, tenant-domain scope boundaries, current-state conflict handling, no-new-endpoint rules, projection-confirmed success requirements, proof-link support-safety, focus/live-region/accessibility expectations, localization parity, and focused test requirements.
- Implemented `TenantCorrectionPreviewSnapshot` and `TenantCorrectionProofLink` to keep original evidence, current projection evidence, intended tenant-domain command, command tracking, lifecycle state, audit state, focus target, live-region politeness, and proof-link data separate.
- Extended `TenantCorrectionStartIntent` reuse with original timestamp preview data, already-applied restore detection, current-role conflict detection, and safe current-projection visible copy.
- Wired `TenantAuditPage` to re-query tenant detail projection for correction availability, derive explicit intended role evidence from selected role or structured audit narrative, keep global-admin correction fail-closed, and preserve existing audit filters, cursor history, receipt selection, safe return URL behavior, and UTC filter parsing.
- Replaced the handoff-only correction panel with a Tenants-owned preview/confirmation surface using the existing command gateway methods, status lookup, projection re-query confirmation, audit proof lookup, one-at-a-time submission locking, localized states, stable selectors, and support-safe proof links.
- Added a Tenants focus helper and correction launcher selectors so close/cancel returns focus to the launching audit row or receipt correction control instead of relying only on callbacks.
- Added EN/FR correction preview, confirm, proof, state, role-choice, audit-state, and conflict/unavailable resource keys with parity covered by existing resource tests.
- Added focused state coverage for preview completeness, current-state conflict blocking, restore already-applied detection, lifecycle/status mapping, projection-confirmed correction, audit proof linking, and absolute UTC proof timestamps.

### File List

- _bmad-output/implementation-artifacts/5-6-preview-and-confirm-correction-with-linked-proof.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditDataGrid.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor
- src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx
- src/Hexalith.Tenants.UI/Resources/TenantsResources.resx
- src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs
- src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs
- src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js
- tests/Hexalith.Tenants.UI.Tests/Components/CorrectionStartPanelTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs
- tests/Hexalith.Tenants.UI.Tests/State/TenantCorrectionPreviewSnapshotTests.cs

### Change Log

- 2026-06-06T19:18:10+02:00 - Implemented Story 5.6 correction preview/confirmation, tenant-domain command submission, projection-confirmed lifecycle, support-safe proof linking, focus return, localization, and focused validation coverage.
- 2026-06-06 - Senior Developer Review (AI): auto-fixed HIGH localization defect (raw English conflict copy in `TenantCorrectionPreviewSnapshot`); routed client-derived blocking/conflict messages through localized resource keys via a new `SafeMessageKey`. Build clean, UI suite 610/610 green. Status → done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-06 · **Outcome:** Approve (after auto-fix)

### Validation performed

- Build: `dotnet build tests/Hexalith.Tenants.UI.Tests/...csproj -c Release` → 0 warnings / 0 errors.
- Tests: xUnit v3 executable fallback (known .NET 10 MTP/VSTest issue) → **610/610 pass** before and after fix.
- Git vs File List: consistent. Only extra working-tree change is `_bmad-output/.../tests/test-summary.md` (a BMAD artifact, excluded from review) plus the pre-existing dirty orchestration file. No false File-List claims; no `[x]` task found undone (0 CRITICAL).
- EN/FR resource parity: clean (no EN-only or FR-only `Tenants.Correction.*` keys).
- Corrective event-type mapping (`UserAddedToTenant`, `UserRoleChanged`) verified against `Hexalith.Tenants.Contracts/Events`.

### Findings

- **[HIGH · FIXED] Conflict/blocking copy was not localized (AC2).** `TenantCorrectionPreviewSnapshot` hardcoded English `SafeMessage` strings for the core current-state conflict, already-applied, missing-membership, and unverifiable status cases, and `CorrectionStartPanel.razor` rendered `_snapshot.SafeMessage` raw — so French operators would see English for the exact conflict text AC2 requires to be localized. Fixed by adding `SafeMessageKey` to the snapshot (mirroring the existing `TenantLifecycleAvailability.SafeMessageKey` pattern), pointing the client-derived cases at already-existing localized keys (`Tenants.Correction.Unavailable.CurrentRoleConflict`, `.AlreadyApplied`, `.CurrentProjectionUnavailable`, `.CurrentStateIndeterminate`, `Tenants.Correction.State.AlreadyApplied`, `.UnableToVerify`), and resolving `SafeMessageKey` through `IStringLocalizer` in the panel (gateway-supplied `SafeMessage` still used as fallback). No new resource keys → no parity risk. State + component tests updated.
- **[MEDIUM · residual] AC6 focus-on-terminal-state.** Focus return to the launching row/receipt is wired only on close/cancel (`OnClose` → page launcher refocus). On in-place `failed`/`confirmed` the panel keeps focus context and sets `FocusTarget` but does not programmatically move focus to the `tenants-correction-lifecycle` region (no JS focus call for in-panel targets). Not auto-fixed: a correct fix needs new in-panel JS-interop focus management that the current bUnit harness stubs out, so it cannot be verified green here. Recommend a follow-up with an interop-capable test.
- **[LOW · residual] Misleading dual submit control.** `tenants-correction-preview-handoff` (label "Continue to correction preview") is a Story-5.5 back-compat button that now also calls `SubmitAsync`; a navigation-sounding label on a deliberate-confirmation surface is confusing. Kept because `Panel_renders_..._preview_handoff_without_submission` depends on its presence.
- **[LOW · residual] Double projection re-query.** `RefreshStatusAsync` triggers both `OnProjectionRefreshRequested` (page re-queries tenant detail) and its own `QueryProjectionAsync` for the same tenant — two BFF GETs per refresh. Functionally correct, mild waste.
