---
baseline_commit: 6b573a7
created: 2026-06-06T08:50:33+02:00
---

# Story 3.2: Disable or Enable Tenant with High-Impact Confirmation

Status: blocked

<!-- Note: Created by the BMAD create-story workflow for Story 3.2. This story is intentionally not ready-for-dev while the FR-15 platform-wide destructive-action governance gate remains blocked. -->

## Story

As an authorized global administrator,
I want to disable or enable a tenant through high-impact confirmation and projection proof,
so that tenant availability changes are deliberate, auditable, and never shown as successful before truth is confirmed.

## Acceptance Criteria

1. Given `FC-CMD`, high-impact governance, authorization, freshness, and lifecycle support are confirmed, when an authorized global administrator starts enable or disable, then the UI opens a high-impact Consequence Preview with tenant identity, current lifecycle, intended lifecycle, known consequences, known unknowns, audit/evidence expectation, and recovery path, and submission is blocked if any required preview item is unavailable.
2. Given the lifecycle confirmation surface is open, when the user cancels, presses Escape, submits, or encounters an error, then focus is trapped while open and returns to the launching control afterward, and cancel or Escape does not commit any action.
3. Given the user confirms a valid lifecycle change, when the command is submitted, then the existing command gateway submits through `POST /api/v1/commands`, enforces one-at-a-time command policy, tracks status and SignalR nudges, and re-queries the tenant projection, and the tenant is shown as enabled or disabled only after authoritative projection confirmation.
4. Given the backend returns `TenantLifecycleStateAlreadySet` or `TenantDisabled`, when the lifecycle panel renders the result, then the UI shows safe localized rejection text and the correct non-Success lifecycle state, and it does not expose raw command payloads, metadata, stack traces, tokens, internal correlation ids, or PII.
5. Given the lifecycle outcome is accepted, pending, rejected, failed, degraded, unable to verify, audit pending, delayed, unavailable, or missing support, when the result is displayed, then every state remains distinct and accessible, and audit/evidence handoff is honest and never fabricated.
6. Given platform policy still blocks lifecycle commands, when this story is selected for implementation, then the story is not ready for development until the blocking gate is cleared or Product/UX explicitly records an approved fallback, and blocked status is preserved in sprint planning rather than bypassed inside Tenants.
7. Given this story is complete, when verification is run, then unit/component tests cover gate readiness, preview completeness, one-at-a-time locking, `TenantLifecycleStateAlreadySet`, `TenantDisabled`, projection confirmation, audit unavailable states, and no optimistic lifecycle transition, and Playwright or component tests verify destructive confirmation focus behavior, keyboard complete-or-exit, live-region politeness, forced-colors status rendering, responsive fail-closed behavior, and stable selectors.

## Tasks / Subtasks

- [ ] Verify the pre-dev governance gate before implementation (AC: 6)
  - [ ] Confirm a newer planning record explicitly clears FR-15 platform-wide destructive-action governance or grants a Product/UX-approved fallback for disable/enable.
  - [ ] If no such record exists, stop implementation, keep `sprint-status.yaml` story `3-2-disable-or-enable-tenant-with-high-impact-confirmation` as `backlog`, and do not add lifecycle submission code.
  - [ ] If the gate is cleared, cite the exact record in this story before changing status to `ready-for-dev`.

- [ ] Extend the existing lifecycle availability surface instead of rebuilding it (AC: 1, 3, 6)
  - [ ] Start from `TenantLifecycleAvailability` and `TenantLifecycleActionAvailability`; keep same-state enable/disable represented as `TenantLifecycleStateAlreadySet`.
  - [ ] Wire only proven server/BFF-reflected global-administrator authority and high-impact governance readiness. Do not infer global administrator authority from client-only claim parsing.
  - [ ] Keep `stale`, `unknown`, degraded/unavailable detail, missing command surface, narrow/mobile safety context, and incomplete preview as fail-closed states.
  - [ ] Preserve tenant id, tenant name, current lifecycle status, freshness, and unavailable reason next to the action slot.

- [ ] Add lifecycle command request and gateway support using the existing command gateway pattern (AC: 3, 4)
  - [ ] Add focused request models for lifecycle operations under `src/Hexalith.Tenants.UI/State/TenantCommands/`; a single request with `TenantId` and `TenantLifecycleOperation` is preferred unless existing patterns require separate request records.
  - [ ] Add `EnableTenantAsync` and `DisableTenantAsync` to `ITenantCommandGateway`, `TenantCommandGateway`, and `UnavailableTenantCommandGateway`.
  - [ ] Submit existing contract records `EnableTenant` and `DisableTenant` with `messageId`, tenant `"system"`, domain `"tenants"`, aggregate id equal to the literal tenant id, command name `nameof(EnableTenant)` or `nameof(DisableTenant)`, and JSON payload serialized from the existing command record.
  - [ ] Map `TenantLifecycleStateAlreadySetRejection`, `TenantDisabledRejection`, `TenantNotFoundRejection`, and `InsufficientPermissionsRejection` to command-neutral, support-safe text; never show raw problem details, correlation ids, tokens, payloads, ETags, cursors, stack traces, or decoded claims.
  - [ ] Preserve `GetStatusAsync` as the shared status lookup and extend shared safe rejection mapping only where safe across command types.

- [ ] Build the high-impact confirmation and consequence-preview flow (AC: 1, 2, 5)
  - [ ] Prefer a focused component under `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/`, for example `TenantLifecycleCommandFlow.razor`, composed from `TenantDetailPage`.
  - [ ] Use the Product/UX-approved inline structured-text `FC-CNS` fallback only if the FR-15 governance gate is explicitly cleared for this story; do not build a generic FrontComposer `<ConsequencePreview>` replacement inside Tenants.
  - [ ] Preview all required items: tenant identity, current lifecycle, intended lifecycle, known consequences, known unknowns, audit/evidence expectation, recovery path, freshness/projection evidence, authorization/governance facts, and command-surface readiness.
  - [ ] Fail closed if any preview item is unavailable; name the missing item with a localized inline reason.
  - [ ] Require elevated friction suitable for high-impact lifecycle action. At minimum, require an explicit typed confirmation of the tenant id or exact operation phrase consistent with existing destructive flows.
  - [ ] Trap focus while the confirmation surface is open; Escape and cancel must close without submitting and return focus to the launching enable/disable control.

- [ ] Add lifecycle command state and projection-confirmation behavior (AC: 3, 5)
  - [ ] Follow the existing snapshot pattern in `TenantCreateCommandSnapshot`, `TenantRemoveMemberCommandSnapshot`, and `TenantUpdateMetadataCommandSnapshot`: `Previewed`, `RequestSent`, `Accepted`, `ProjectionPending`, `Confirmed`, `Rejected`, `Failed`, `Degraded`, `UnableToVerify`, and audit states remain distinct.
  - [ ] Enforce one-at-a-time command policy across tenant detail command surfaces. Lifecycle command submission must lock out metadata/member lifecycle submissions and respect existing metadata/member in-flight locks.
  - [ ] Treat SignalR only as a freshness nudge that triggers status/projection re-query; it must never advance lifecycle status or audit state by itself.
  - [ ] Confirm `DisableTenant` only when the authoritative re-queried tenant projection shows `TenantStatus.Disabled`; confirm `EnableTenant` only when projection shows `TenantStatus.Active`.
  - [ ] Keep last-confirmed projection truth visible while the command is in flight. Do not overwrite `TenantDetail.Status`, list row status, page title, metadata, members, or configuration with intended status.
  - [ ] If projection evidence is missing after a terminal command status, show `unable to verify` or `projection pending`, not success.

- [ ] Preserve existing tenant detail behavior and support-safe UX (AC: 1-5)
  - [ ] Keep `TenantDetailPage` identity, status badge, lifecycle label, freshness badge, metadata edit flow, member access review, configuration read view, support-safe copy, stale/degraded messages, and return navigation behavior unchanged except for lifecycle command composition.
  - [ ] Do not expose lifecycle command internals in browser-visible markup, logs, live regions, resource strings, copied text, or test assertions.
  - [ ] Use whole-string localized EN/FR `.resx` entries under `Tenants.Lifecycle.*`; no runtime sentence-fragment assembly.
  - [ ] Use stable selectors such as `tenants-lifecycle-actions`, `tenants-lifecycle-enable`, `tenants-lifecycle-disable`, `tenants-lifecycle-preview`, `tenants-lifecycle-confirm`, `tenants-lifecycle-cancel`, `tenants-lifecycle-state`, `tenants-lifecycle-audit`, `tenants-lifecycle-live-region`, and `tenants-lifecycle-unavailable-reason`.
  - [ ] Keep high-impact command flows unavailable on mobile/narrow layouts that cannot preserve the full safety context.

- [ ] Add focused tests and update test evidence (AC: 1-7)
  - [ ] Add availability-model tests for the cleared-gate path and still-blocked path; blocked governance must prevent submission.
  - [ ] Add gateway tests for enable/disable payload shape, literal tenant id preservation, command name, message id, correlation id capture, unavailable gateway behavior, and safe rejection mapping.
  - [ ] Add component tests for full preview completeness, missing preview item blocking, typed confirmation, cancel/Escape no commit, focus return, live-region politeness, forced-colors/no-color-only rendering, and stable selectors.
  - [ ] Add lifecycle state tests for accepted/projection pending/confirmed/rejected/failed/degraded/unable-to-verify/audit states and SignalR nudge-only behavior.
  - [ ] Extend `TenantDetailSurfaceTests` to prove lifecycle command composition preserves metadata/member/configuration/read behavior and last-confirmed projection truth.
  - [ ] Add source or behavior tests proving no optimistic status transition and no raw support-unsafe content.
  - [ ] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` and `tests/test-summary.md` if the repository continues the current story evidence practice.
  - [ ] Run `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`; when `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility, use the xUnit v3 executable fallback documented in prior stories.

## Dev Notes

### Pre-Dev Gate

- Story 3.2 is intentionally blocked at story-creation time. The fallback approval record explicitly says FR-15 disable/enable remains categorically blocked with no fallback by design, and the 2026-06-05 readiness report says platform-wide destructive stories 3.2, 4.3, and 4.4 must stay blocked rather than bypassing governance inside Tenants. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#The three approved fallbacks`; `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05-v2.md#External Build-Start Gates`]
- Story 1.0 cleared `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`; this removes command-infrastructure blockers, but it does not clear the separate FR-15 platform-wide destructive-action governance block. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-05.md#4.3 Architecture - replace stale readiness paragraph`; `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#2026-06-05 supersession note`]
- Do not update `sprint-status.yaml` to `ready-for-dev` until a newer explicit governance-clearing record exists. The current status should remain backlog/blocked-by-planning rather than using Tenants implementation to bypass policy. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.2: Disable or Enable Tenant with High-Impact Confirmation`]

### Story Source And Epic Context

- Story source is Epic 3 Story 3.2. Epic 3 covers tenant lifecycle and configuration control while preserving high-impact safety rules and projection truth. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Lifecycle and Configuration Control`]
- FR15 requires global-administrator-only disable/enable, Consequence Preview, `TenantLifecycleStateAlreadySet` for same-state requests, disabled status as eventually-consistent availability signal, `TenantDisabled` rejection for commands targeting disabled tenants, no-color-only lifecycle status, and success only after projection confirmation. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-15: Disable or enable a tenant`]
- The command story sizing guardrail was applied correctly: FR15 is split into Story 3.1 availability/readiness and Story 3.2 command flow. Story 3.2 must build on Story 3.1, not replace it. [Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05-v2.md#Story Sizing & Dependencies`]

### Existing Implementation To Extend

- `TenantLifecycleAvailability` already evaluates `EnableTenant` and `DisableTenant` separately using current `TenantStatus`, freshness, detail surface kind, command-surface connection, governance readiness, authorization reflection, and narrow safety context. Same-state requests already name `TenantLifecycleStateAlreadySet`. Extend this model for the cleared-gate path rather than creating a separate lifecycle eligibility system. [Source: `src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs`]
- `TenantLifecycleActionAvailability.razor` already renders tenant lifecycle facts, enable/disable action slots, inline unavailable reasons, stable selectors, keyboard-reachable reasons, and live-region behavior. Story 3.2 should add the high-impact command/preview path here or compose a sibling lifecycle command component from the same action slot. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor`]
- `TenantDetailPage.razor` currently composes lifecycle actions beside tenant identity, status, lifecycle label, and freshness. It hardcodes authorization reflection as `Indeterminate` and governance readiness as `Unresolved`; Story 3.2 must only change those inputs when a proven BFF/server-side source and governance-clearing record exist. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`]
- `ITenantCommandGateway` has create/add/change/remove/update methods plus shared `GetStatusAsync`, but no lifecycle methods. Add lifecycle methods there and in `TenantCommandGateway`/`UnavailableTenantCommandGateway`; do not introduce a second command bus or browser-side backend client. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`]

### Backend And Domain Contract Facts

- `EnableTenant` and `DisableTenant` already exist as plain public command records with `TenantId`. Do not add fields, XML docs, `sealed`, or marker interfaces. [Source: `src/Hexalith.Tenants.Contracts/Commands/EnableTenant.cs`; `src/Hexalith.Tenants.Contracts/Commands/DisableTenant.cs`; `_bmad-output/project-context.md#C# Language & Contract Rules`]
- `TenantAggregate.Handle(DisableTenant, ...)` requires trusted global administrator authority, rejects missing tenants, rejects already-disabled tenants with `TenantLifecycleStateAlreadySetRejection`, and emits `TenantDisabled` otherwise. `EnableTenant` mirrors this for already-active and emits `TenantEnabled` otherwise. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#Handle(DisableTenant)`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#Handle(EnableTenant)`]
- `TenantLifecycleStateAlreadySetRejection` carries `TenantId`, `CurrentStatus`, `RequestedStatus`, and `CommandName`. It is a rejection, not NoOp or success. [Source: `src/Hexalith.Tenants.Contracts/Events/Rejections/TenantLifecycleStateAlreadySetRejection.cs`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`]
- `TenantDisabledRejection` applies to tenant-scoped commands targeting disabled tenants. Enabling a disabled tenant is the lifecycle recovery operation and should be confirmed only after projection shows `TenantStatus.Active`. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-15: Disable or enable a tenant`]
- Tenant ids are meaningful caller-supplied strings. Preserve them literally; never parse, normalize, case-fold, or generate tenant ids as GUID/ULID. Only the command `messageId` is client-generated ULID. [Source: `_bmad-output/project-context.md#Identity Rules`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]

### Command Flow And UX Guardrails

- The only command confirmation pattern is dispatch, status poll plus SignalR nudge, authoritative projection re-query, then confirmed only from the re-queried projection. SignalR alone must never advance lifecycle or audit state. [Source: `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`]
- Consequence Preview opens only after validation, freshness, authorization, lifecycle support, preview support, and governance gates are eligible. If any preview item is unavailable, submission is blocked with a visible localized reason. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Component Patterns`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#consequence-preview`]
- High-impact/destructive controls must not read as primary or casual actions. They require full preview, explicit confirmation, focus trap, safe non-committing Escape/cancel, focus return, no optimistic transition, and no mobile/narrow rendering when full safety context cannot be preserved. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#destructive-control`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Responsive & Platform`]
- Live-region politeness is driven by announcement intent, not badge color. Assertive is reserved for rejection, failure, unable-to-verify, degraded, or destructive-block states; never announce success before projection truth. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Accessibility Floor`]
- Use the canonical unavailable-action reason categories and command lifecycle vocabulary verbatim. Do not invent new duplicate/timeout semantics; the specs explicitly say those tokens have no dedicated gloss. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#State Patterns`]

### Existing Files To Update And Preserve

- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`: compose lifecycle command flow and command activity locking; preserve current identity/status/freshness/metadata/member/configuration behavior.
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor`: extend or wrap the existing lifecycle action slot; do not lose inline reasons and stable selectors from Story 3.1.
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs`: extend availability for cleared governance and proven authorization; keep fail-closed order.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`: add lifecycle command request/snapshot types here only if the existing shared command model file remains the repository pattern.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`, `TenantCommandGateway.cs`, `UnavailableTenantCommandGateway.cs`: add lifecycle command submission and safe rejection mapping.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`: add lifecycle command/preview/state/recovery copy with EN/FR parity.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`: add enable/disable gateway mapping and rejection tests.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs`, `TenantDetailSurfaceTests.cs`, and new lifecycle command-flow component tests: cover UI behavior, accessibility, focus, selectors, and no optimistic truth.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleAvailabilityTests.cs` and lifecycle command snapshot tests: cover state transitions and fail-closed gates.

### Scope Boundaries And Anti-Patterns

- Do not implement Story 3.2 while FR-15 remains blocked. The only acceptable code change before gate clearance is a planning/status correction explicitly requested by a human.
- Do not add backend endpoints, controllers, query contracts, command contracts, aggregate behavior, AppHost/Aspire plumbing, package versions, shared FrontComposer components, or generic technical scaffolding in Tenants.
- Do not modify submodules or move generic command/preview infrastructure into this domain repository. Shared scaffolding belongs in the relevant technical module.
- Do not treat disabled tenant enablement as an optimistic local recovery. It is confirmed only by authoritative projection re-query.
- Do not use client-only authorization guesses for global-administrator authority. UI reflects server/BFF authority and the backend/domain enforce.
- Do not show Success, "done", audit proof, or enabled/disabled status until projection or audit evidence proves it.
- Do not hide reasons in tooltips. Reasons must be inline-visible, programmatically associated, and keyboard/screen-reader reachable.
- Do not use raw state literals outside the canonical vocabulary/resource pattern where an existing typed enum/resource key exists.

### Previous Story Intelligence

- Story 3.1 established the lifecycle availability component, fail-closed governance defaults, same-state `TenantLifecycleStateAlreadySet` copy, visible unavailable reasons, and tests proving no lifecycle submission path exists. Story 3.2 must remove that no-submission invariant only after the gate clears and only for the eligible target operation. [Source: `_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md`]
- Story 3.1 review fixed degraded/unavailable/unknown detail-surface reason mapping, live-region ARIA consistency, and hardened primary availability selection. Preserve those fixes. [Source: `_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md#Senior Developer Review (AI)`]
- Story 2.5 established metadata command activity locking from child flow to `TenantDetailPage`; Story 3.2 should generalize or reuse that page-level lock so lifecycle commands do not run concurrently with metadata/member commands. [Source: `_bmad-output/implementation-artifacts/2-5-edit-tenant-metadata-with-safe-validation.md`]
- Story 2.4 established destructive consequence preview, typed confirmation, projection-confirmed absence, honest audit handoff, support-safe copy, and no fabricated receipts. Story 3.2 should follow the same destructive-flow discipline while adapting preview content to tenant lifecycle. [Source: `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md`]
- Story 1.8 established support-safe identifier copy and copy/logging boundaries. Apply the same support-safety rules to lifecycle confirmation, live regions, resource strings, and tests. [Source: `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`]

### Git Intelligence

- Recent story-scoped commits show the repository convention: `feat(story-3.1): Tenant Lifecycle Command Availability and Blocked-State Guardrail`, `feat(story-2.5): Edit Tenant Metadata with Safe Validation`, and `feat(story-2.4): Remove Tenant Member with Consequence Preview`. If this story is eventually implemented after the gate clears, a compatible commit would be `feat(story-3.2): Disable or Enable Tenant with High-Impact Confirmation`. [Source: `git log --oneline -5`]
- The worktree had an unrelated dirty `_bmad-output/story-automator/orchestration-1-20260605-153745.md` file during story creation. Do not modify or revert it as part of Story 3.2. [Source: `git status --short`]

### Latest Technical Information

- Use the repo-pinned local stack and contracts: .NET 10, Blazor InteractiveServer, Fluent UI Blazor v5 pinned by the UI project, EventStore command gateway contracts, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not introduce new packages or package versions for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- No external API/package research is required beyond the pinned local contracts. The material risks are governance bypass, client-only authority inference, optimistic lifecycle status, partial preview submission, command-state collapse, support-unsafe copy, and unintended changes to existing metadata/member/configuration flows.

### Project Structure Notes

- Source changes, when unblocked, should stay under `src/Hexalith.Tenants.UI/`, primarily `Components/Tenants/Lifecycle/`, `Components/Pages/`, `State/TenantDetail/`, `State/TenantCommands/`, `Services/Gateways/`, `Resources/`, and colocated CSS.
- Tests should stay under `tests/Hexalith.Tenants.UI.Tests/` using xUnit v3, Shouldly, bUnit, and NSubstitute; files remain plural `{Class}Tests.cs`.
- Domain contracts and server aggregate behavior already exist and are covered by existing server/testing/integration tests. Story 3.2 is a UI command-flow story, not a backend contract story.
- Detected planning conflict to manage: the requested story file exists, but the canonical planning sources still block FR-15. This story therefore serves as implementation context for the future cleared-gate work and as a guardrail against accidental implementation today.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 3.2: Disable or Enable Tenant with High-Impact Confirmation`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Lifecycle and Configuration Control`
- FR15 and shared contracts: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-15: Disable or enable a tenant`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`
- Readiness and gates: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md`; `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05-v2.md#External Build-Start Gates`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-05.md`
- Architecture and UX: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#consequence-preview`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#destructive-control`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Component Patterns`
- Current UI implementation: `src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs`; `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`; `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`
- Domain contracts: `src/Hexalith.Tenants.Contracts/Commands/EnableTenant.cs`; `src/Hexalith.Tenants.Contracts/Commands/DisableTenant.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/TenantLifecycleStateAlreadySetRejection.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/TenantDisabledRejection.cs`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`
- Prior story evidence: `_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md`; `_bmad-output/implementation-artifacts/2-5-edit-tenant-metadata-with-safe-validation.md`; `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md`; `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, `architecture.md`, targeted PRD/addendum/UX/readiness sections, fallback approval record, Story 3.1 previous-story intelligence, current lifecycle UI/gateway/resource/test files, lifecycle domain contracts, and recent git history.
- Checklist validation was run against `.agents/skills/bmad-create-story/checklist.md`; the main correction from validation was to keep this story `blocked` instead of `ready-for-dev` because canonical planning sources still block FR-15 platform-wide destructive actions.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 3.2 to the future lifecycle command flow after high-impact governance is explicitly cleared.
- Story status remains `blocked`; `sprint-status.yaml` was intentionally not advanced to `ready-for-dev`.
- Story context identifies the key implementation risks: governance bypass, client-only authority inference, partial consequence preview, optimistic lifecycle status, command-state collapse, support-unsafe copy, and regressions to existing tenant detail command flows.

### File List

- `_bmad-output/implementation-artifacts/3-2-disable-or-enable-tenant-with-high-impact-confirmation.md`

### Change Log

- 2026-06-06T08:50:33+02:00 - Created Story 3.2 context and preserved blocked status pending FR-15 high-impact governance clearance.
