---
created: 2026-06-29T15:01:29+02:00
baseline_commit: d0ece74
source_change: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-global-administrator-correction-verification.md
---

# Story 5.7: Global Administrator Correction Verification

Status: ready-for-dev

<!-- Note: Created by the BMAD correct-course and create-story workflows for the global-administrator correction gate. -->

## Story

As an authorized operator,
I want global-administrator correction enabled only after the fixed-scope correction path is verified,
so that platform authority recovery cannot be inferred from tenant-domain correction behavior.

## Acceptance Criteria

1. Given audit evidence describes a global-administrator authority outcome, when correction availability is evaluated, then `GlobalAdministratorRemoved` prepares a `SetGlobalAdministrator` correction and `GlobalAdministratorSet` prepares a `RemoveGlobalAdministrator` correction in the fixed `system` / `global-administrators` / `global-administrators` scope, and the correction path does not use tenant membership, tenant detail, tenant role selection, or tenant-domain commands as substitutes.
2. Given the fixed global-administrator read projection is current and command support is connected, when the operator starts correction from system-scope audit evidence, then the preview names the platform authority scope, target user id, intended global-administrator command, known consequences, known unknowns, audit/evidence expectation, and recovery path, and submission stays blocked if any required preview item is unavailable.
3. Given the operator confirms a global-administrator correction, when the command is submitted, then the existing command gateway sends `SetGlobalAdministrator` or `RemoveGlobalAdministrator` through `POST /api/v1/commands` with fixed global-administrator routing and one-at-a-time locking, and no backend correction, receipt, proof-link, tenant-member, or direct projection endpoint is added.
4. Given command status reaches accepted, stored, published, completed, rejected, failed, degraded, or unable-to-verify states, when the correction lifecycle updates, then each state remains distinct, localized, accessible, and support-safe, and SignalR or status lookup never proves success without authoritative global-administrator projection re-query.
5. Given the correction is a grant/restore path, when projection confirmation is evaluated, then success is shown only when `GetGlobalAdministratorsAsync` confirms the target user appears in the fixed projection, and `GlobalAdministratorAlreadyExists` remains safe rejection/already-present guidance rather than false correction success.
6. Given the correction is a remove path, when projection confirmation and last-admin safety are evaluated, then success is shown only when `GetGlobalAdministratorsAsync` confirms the target user is absent from the fixed projection, and the last global administrator is unavailable before submit and `LastGlobalAdministrator` remains a hard-blocked rejection if a race occurs.
7. Given corrective audit proof becomes available, when original and corrective records are linked, then the proof lookup uses support-safe system-scope audit rows for `GlobalAdministratorSet` or `GlobalAdministratorRemoved`, with absolute timestamps and structured narrative metadata, and raw payloads, tokens, decoded JWTs, EventStore metadata, internal correlation ids, message ids, stack traces, and PII are never rendered or copied.
8. Given authorization, read freshness, command support, audit evidence, target visibility, projection confirmation, or proof lookup is incomplete, when the global-administrator correction path renders, then it fails closed with visible localized reasons, preserves the original evidence and last-confirmed projection, and never labels the original event as undone or repaired in place.
9. Given this story is complete, when verification is run, then unit/component tests cover global-administrator correction start enablement, fixed-scope command selection, no tenant-role UI, grant/remove submission, projection-confirmed presence/absence, last-admin pre-submit blocking, `LastGlobalAdministrator`, `GlobalAdministratorAlreadyExists`, `GlobalAdministratorNotFound`, proof linking, fail-closed incomplete evidence, forbidden terminology, and no history mutation.
10. Given focused UI or integration verification is run, then keyboard complete-or-exit, focus return and terminal-state focus for this correction path, live-region politeness, forced-colors rendering, responsive safety, support-safe copy, stable selectors, and fixed command gateway routing are verified.

## Tasks / Subtasks

- [ ] Replace the hard global-administrator correction gate with a verified enablement gate (AC: 1, 2, 8, 9)
  - [ ] Update the live audit correction-intent path so global-administrator audit rows can be eligible only when fixed global-administrator read evidence, command support, authorization reflection, current freshness, and required audit fields are all present.
  - [ ] Keep `GlobalAdministratorRemoved` mapped to `SetGlobalAdministrator` and `GlobalAdministratorSet` mapped to `RemoveGlobalAdministrator`.
  - [ ] Remove `GlobalAdministratorCommandSupportUnavailable` only for the verified eligible path; stale, unauthorized, missing read support, missing command support, or unknown evidence must still fail closed.
  - [ ] Do not infer platform authority from tenant detail, tenant members, user lookup rows, claims-only state, or tenant role selection.

- [ ] Add global-administrator preview data without tenant-role coupling (AC: 1, 2, 6, 8)
  - [ ] Show fixed scope (`system`, `global-administrators`, `global-administrators`), target user id, intended command, current global-administrator projection state, current admin count for remove, last-admin impact, known consequences, known unknowns, audit expectation, and recovery path.
  - [ ] Do not render tenant role controls, tenant member current-role fields, tenant lifecycle blockers, or tenant detail projection labels for global-administrator correction.
  - [ ] Preserve the original audit receipt/row while the correction preview is open.
  - [ ] Disable submission when preview data cannot be shown honestly.

- [ ] Submit global-administrator corrections through existing command gateway methods (AC: 3, 4, 5, 6, 9, 10)
  - [ ] Use `ITenantCommandGateway.SetGlobalAdministratorAsync(new SetGlobalAdministrator(targetUserId))` for restore/grant correction.
  - [ ] Use `ITenantCommandGateway.RemoveGlobalAdministratorAsync(new RemoveGlobalAdministrator(targetUserId))` for remove correction.
  - [ ] Reuse `GetStatusAsync` for command status and one-at-a-time command locking; prevent duplicate submit, browser refresh resubmit, and simultaneous grant/remove correction.
  - [ ] Do not add backend endpoints, EventStore registrations, projection actors, generic recovery APIs, command request DTO duplicates, or direct DAPR/state-store reads.

- [ ] Confirm correction only from the fixed global-administrator projection (AC: 4, 5, 6, 8, 9)
  - [ ] Query `GetGlobalAdministratorsAsync` after command acceptance/status updates; SignalR may only trigger re-query.
  - [ ] For `SetGlobalAdministrator`, mark confirmed only when the target user appears in the fixed projection.
  - [ ] For `RemoveGlobalAdministrator`, mark confirmed only when the target user is absent from the fixed projection.
  - [ ] Keep last-confirmed projection rows visible while accepted, projection pending, rejected, failed, degraded, or unable to verify.
  - [ ] Treat `GlobalAdministratorAlreadyExists`, `GlobalAdministratorNotFound`, `InsufficientPermissions`, and `LastGlobalAdministrator` as safe localized states, never as tenant-member copy or false success.

- [ ] Preserve last-admin safety for correction remove (AC: 6, 8, 9)
  - [ ] Block a remove correction before submit when the current fixed projection has one global administrator.
  - [ ] Surface `LastGlobalAdministrator` as a hard-blocked rejection if a race reaches the backend.
  - [ ] Do not render an override, elevated friction bypass, tenant-membership retry, or completable destructive confirmation for last-admin removal.

- [ ] Link global-administrator corrective proof from system-scope audit evidence (AC: 7, 8, 9)
  - [ ] Query system-scope audit evidence for corrective `GlobalAdministratorSet` or `GlobalAdministratorRemoved` rows after projection confirmation.
  - [ ] Link original and corrective evidence with support-safe event references and absolute timestamps.
  - [ ] Keep `audit pending`, `audit delayed`, `audit unavailable`, and `missing support` distinct; do not show audit proof from command status alone.
  - [ ] Build proof links from `TenantAuditRow`, `TenantAuditReceipt`, and structured `NarrativePayload` fields only.

- [ ] Add localization, support-safety, accessibility, and responsive evidence (AC: 1-10)
  - [ ] Add or update EN/FR whole-string resource keys under `Tenants.Correction.*` for global-administrator domain labels, preview fields, unavailable reasons, lifecycle states, rejection copy, audit states, proof links, and recovery actions.
  - [ ] Keep all visible and accessible copy platform-governance-specific; no tenant-owner/member wording for global-administrator correction.
  - [ ] Add static/rendered copy guards for prohibited recovery terminology and unsafe support markers.
  - [ ] Preserve keyboard complete-or-exit, safe cancel/Escape, focus return to the launching receipt/row, terminal-state focus for this correction path, live-region politeness by state, visible focus, forced-colors support, stable dimensions, and narrow-viewport fail-closed behavior.
  - [ ] Keep stable selectors, including `data-testid="tenants-correction-start"`, `data-testid="tenants-correction-preview"`, `data-testid="tenants-correction-confirm"`, `data-testid="tenants-correction-lifecycle"`, and `data-testid="tenants-correction-proof-link"`.

- [ ] Add focused tests and validation (AC: 1-10)
  - [ ] State tests: eligible global-admin start intent with command support, missing evidence fail-closed, fixed-scope preview data, no tenant-role requirement, grant/remove lifecycle, presence/absence projection confirmation, last-admin block, and proof-link state.
  - [ ] Component tests: system-scope audit row and receipt can open eligible global-admin correction; stale/unavailable/unauthorized paths remain fail-closed; no tenant role selector appears; confirm calls the correct gateway method; duplicate submit is blocked; original evidence remains visible.
  - [ ] Gateway/status tests: existing `SetGlobalAdministratorAsync` and `RemoveGlobalAdministratorAsync` routing remains fixed to `system/global-administrators/global-administrators`, and rejection mapping stays platform-governance-specific.
  - [ ] Audit/proof tests: corrective proof lookup uses system-scope audit rows and never raw payloads or internal correlation/message ids.
  - [ ] Resource parity/static tests: EN/FR correction keys stay aligned; rendered correction copy contains no unsafe support markers or prohibited recovery wording.
  - [ ] Run focused validation first: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`.
  - [ ] If `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest issue, run the xUnit v3 executable fallback for the UI test assembly.
  - [ ] Run focused integration/API tests for global-administrator command routing if the implementation touches gateway/status behavior.

## Dev Notes

### Story Source And Epic Context

- This story comes from the 2026-06-29 Epic 5 retrospective action item and correct-course proposal. Epic 5 completed tenant-domain audit and forward correction, but global-administrator correction remains gated by missing story-level verification. [Source: `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-29.md#Action Items`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-global-administrator-correction-verification.md`]
- The changed dependency is precise: Epic 4 command primitives now exist, but tenant-domain correction success is not evidence that global-administrator correction is safe. The enabled path must prove fixed scope, last-admin safety, platform-governance rejection copy, proof lookup, and fail-closed behavior. [Source: `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-29.md#Significant Discovery`]
- FR24/FR25 require forward commands from audit evidence with preview against current state and linked proof. FR19 adds global-administrator grant/remove behavior and last-admin protection. This story composes those requirements; it does not create new domain semantics. [Source: `_bmad-output/planning-artifacts/epics.md#FR19`; `_bmad-output/planning-artifacts/epics.md#FR24`; `_bmad-output/planning-artifacts/epics.md#FR25`]

### Existing Implementation To Extend

- `TenantCorrectionStartIntent` already models global-admin audit outcomes: `GlobalAdministratorRemoved` -> `SetGlobalAdministrator`; `GlobalAdministratorSet` -> `RemoveGlobalAdministrator`; tenant scope becomes `global-administrators`, and preview inputs include `tenantId = system`, `domain = global-administrators`, `aggregateId = global-administrators`, and `userId`. Do not create a second outcome mapper. [Source: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs`]
- The live path still keeps global-admin correction fail-closed. `TenantCorrectionStartIntent.FromReceipt` and `TenantAuditPage.CreateCorrectionIntent` set `HasGlobalAdministratorCommandSupport` to `false`. The story must replace that with an evidence-based gate, not a blanket true. [Source: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs`; `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`]
- `CorrectionStartPanel` currently submits only `AddUserToTenant` and `ChangeUserRole`; all other intended commands fall through to command-support unavailable. Add explicit global-admin command handling or a focused global-admin correction panel that preserves the same lifecycle rules. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor`]
- `TenantCorrectionPreviewSnapshot` is tenant-role oriented today. Its global-admin fallback says no tenant-domain command will be submitted, and it confirms only tenant detail/member projection. Do not use tenant detail projection or tenant roles to confirm global-admin correction. [Source: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs`]
- Story 4.3 and 4.4 already implemented `ITenantCommandGateway.SetGlobalAdministratorAsync` and `RemoveGlobalAdministratorAsync` with fixed gateway routing and platform-governance rejection mapping. Reuse these methods. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `_bmad-output/implementation-artifacts/4-3-grant-global-administrator-with-projection-confirmation.md`; `_bmad-output/implementation-artifacts/4-4-remove-global-administrator-with-last-admin-hard-stop.md`]
- Story 4.3 and 4.4 state snapshots already contain projection-confirmed grant/remove semantics. Reuse their behavior or extract narrowly; do not duplicate a divergent global-admin lifecycle model. [Source: `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs`; `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRemoveCommandSnapshot.cs`]
- `TenantAuditReadModel` creates system-scope audit entries for `GlobalAdministratorSet` and `GlobalAdministratorRemoved`, including structured `userId`, `actorUserId`, and absolute timestamps. Use those rows for proof; do not inspect serialized event payloads. [Source: `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`]

### Architecture And Data Boundaries

- The global-administrator aggregate identity is fixed: tenant `"system"`, domain `"global-administrators"`, aggregate id `"global-administrators"`. Tenant ids and user ids are caller-supplied strings, not GUIDs or ULIDs. [Source: `_bmad-output/project-context.md#Identity Rules`; `src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs`]
- Corrections are forward commands only. Never edit, delete, rewrite, hide, or relabel historical events, projections, or state-store records. [Source: `_bmad-output/project-context.md#Event-Sourcing & Domain Rules`; `docs/compensating-commands.md`]
- Browser components must use server-side BFF gateways. Do not call backend APIs directly from the browser, read DAPR state stores, add EventStore server registrations, or create new correction/proof endpoints. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`; `_bmad-output/project-context.md#Host Composition & Framework Rules`]
- Projection truth is authoritative. Command status and SignalR are lifecycle evidence only; they can trigger re-query but cannot confirm correction success. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#5.4 Concurrency and recovery cases`]
- The last global administrator is protected by the backend invariant and must be reflected as unavailable/hard-blocked, not as elevated friction or override. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.4: Remove Global Administrator with Last-Admin Hard Stop`; `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs`]

### UX, Accessibility, And Localization Guardrails

- Use explicit correction language: `start correction`, `restore intended access`, `retry status lookup`, `inspect audit`, `continue read-only`, or `escalate`. Prohibited recovery terms remain forbidden in visible copy. [Source: `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#5.1 Language rule: explicit compensating-command terms, never undo`]
- Keep platform-governance copy distinct from tenant membership. A global-admin correction does not change tenant ownership, member rows, tenant roles, or tenant detail state. [Source: `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#5.4 Keep command domains distinct in recovery copy`; `docs/tenants-ui-operations-shell-spec.md#4.2 Global administrator read-only surface`]
- Use Tenants-owned resource keys and EN/FR parity. Whole-string resources with named placeholders are required; do not concatenate visible sentences or leak enum/machine tokens. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#9. Accessibility & Localization`]
- Distinguish accepted, projection pending, confirmed, rejected, failed, degraded, unable to verify, audit pending, audit delayed, audit unavailable, and missing support. No state may collapse into false Success. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md#5.4 Concurrency and recovery cases`]
- Keyboard users must be able to complete or exit the preview/command flow; close/cancel/failure/confirmation must preserve focus behavior. This story verifies the global-admin correction path specifically and should not defer its terminal-state focus behavior to the broader follow-up. [Source: `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-29.md#Action Items`]

### Previous Story Intelligence

- Story 5.5 added global-admin start-intent recognition but intentionally kept live global-admin command support unavailable. It also proved the need to avoid raw event payloads and preserve original evidence. [Source: `_bmad-output/implementation-artifacts/5-5-start-forward-correction-from-audit-evidence.md#Senior Developer Review (AI)`]
- Story 5.6 implemented tenant-domain correction preview, command submission, projection confirmation, proof linking, and support-safe copy. It explicitly kept global-admin correction fail-closed. Reuse the lifecycle/proof patterns but replace tenant projection confirmation with fixed global-admin projection confirmation. [Source: `_bmad-output/implementation-artifacts/5-6-preview-and-confirm-correction-with-linked-proof.md#Tasks / Subtasks`]
- Story 4.3 implemented projection-confirmed `SetGlobalAdministrator` with no optimistic row insertion and safe `GlobalAdministratorAlreadyExists` / `InsufficientPermissions` handling. [Source: `_bmad-output/implementation-artifacts/4-3-grant-global-administrator-with-projection-confirmation.md#Completion Notes List`]
- Story 4.4 implemented projection-confirmed `RemoveGlobalAdministrator`, last-admin pre-submit unavailability, and `LastGlobalAdministrator` / `GlobalAdministratorNotFound` handling. [Source: `_bmad-output/implementation-artifacts/4-4-remove-global-administrator-with-last-admin-hard-stop.md#Completion Notes List`]

### Latest Technical Information

- Use the repo-pinned local stack: .NET 10, Blazor InteractiveServer, Fluent UI Blazor V5 through FrontComposer, EventStore gateway/query/command contracts, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not add package versions or new dependencies. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `Directory.Packages.props`]
- External package research is not required for this story because the implementation uses existing repo-pinned APIs and local Tenants/EventStore contracts. The implementation risk is scope/domain conflation, false success, last-admin safety, proof lookup, localization, and focus behavior.

### Project Structure Notes

- Expected UI state updates: `src/Hexalith.Tenants.UI/State/TenantAudit/` and, where reuse is cleaner, `src/Hexalith.Tenants.UI/State/GlobalAdministrators/`.
- Expected component updates: `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`, `src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor` or a focused local successor under the same audit component folder, `AuditEvidenceReceipt.razor`, and `AuditDataGrid.razor` only as needed.
- Expected gateway usage: existing `ITenantCommandGateway.SetGlobalAdministratorAsync`, `RemoveGlobalAdministratorAsync`, `GetStatusAsync`, and `ITenantQueryGateway.GetGlobalAdministratorsAsync` / `GetTenantAuditAsync`.
- Expected resource updates: `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`.
- Expected tests: `tests/Hexalith.Tenants.UI.Tests/State/TenantCorrectionStartIntentTests.cs`, `TenantCorrectionPreviewSnapshotTests.cs` or new global-admin correction state tests, `CorrectionStartPanelTests.cs` or focused successor tests, `TenantAuditPageTests.cs`, `AuditEvidenceReceiptTests.cs`, `AuditDataGridCorrectionTests.cs`, `TenantCommandGatewayTests.cs`, resource parity/static support-safety tests, and focused integration tests only if gateway/API behavior changes.
- Avoid backend aggregate/event/query contract changes, `TenantAuditEntry` wire-shape changes, AppHost/Aspire plumbing, shared FrontComposer code, package metadata, submodule changes, and generic recovery infrastructure unless a compile-time integration issue proves a direct requirement.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 5.7: Global Administrator Correction Verification`
- Correct-course proposal: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-29-global-administrator-correction-verification.md`
- Epic 5 retrospective action item: `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-29.md#Action Items`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 5: Audit Evidence and Forward Recovery`
- Global-admin command context: `_bmad-output/planning-artifacts/epics.md#Epic 4: Global Administrator Governance`
- PRD: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#7.9 Compensating Recovery`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`
- Audit/recovery spec: `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#5. Compensating-Recovery Language and Flow`
- Truth-state spec: `docs/tenants-ui-truth-state-and-action-availability-spec.md#5.4 Concurrency and recovery cases`
- Operations shell spec: `docs/tenants-ui-operations-shell-spec.md#4.2 Global administrator read-only surface`
- Compensating commands: `docs/compensating-commands.md`
- Existing code: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionStartIntent.cs`; `src/Hexalith.Tenants.UI/State/TenantAudit/TenantCorrectionPreviewSnapshot.cs`; `src/Hexalith.Tenants.UI/Components/Tenants/Audit/CorrectionStartPanel.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`; `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorGrantCommandSnapshot.cs`; `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorRemoveCommandSnapshot.cs`; `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`
- Existing tests: `tests/Hexalith.Tenants.UI.Tests/State/TenantCorrectionStartIntentTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/CorrectionStartPanelTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`; `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorGrantCommandSnapshotTests.cs`; `tests/Hexalith.Tenants.UI.Tests/State/GlobalAdministratorRemoveCommandSnapshotTests.cs`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes the work to verifying and enabling global-administrator correction from audit evidence; it does not authorize broad audit/recovery refactoring.
- Main implementation risks identified: flipping command support without fixed projection confirmation, using tenant detail/member roles for global-admin correction, false success from command status or SignalR, weakening last-admin safety, unsafe platform-governance copy, proof lookup against the wrong tenant scope, duplicate submission, and hidden history mutation language.

### File List

### Change Log

- 2026-06-29T15:01:29+02:00 - Created Story 5.7 context and marked it ready for development.
