---
baseline_commit: 8e49477
created: 2026-06-06T08:50:33+02:00
refreshed: 2026-06-06T10:54:05+02:00
---

# Story 3.2: Disable or Enable Tenant with High-Impact Confirmation

Status: done

<!-- Note: Refreshed by the BMAD correct-course/create-story flow for Story 3.2. FR15 disable/enable is approved as a reversible soft-delete / availability-control flow, not hard destructive tenant deletion. -->

## Story

As an authorized global administrator,
I want to disable or enable a tenant through high-impact confirmation and projection proof,
so that tenant availability changes are deliberate, auditable, and never shown as successful before truth is confirmed.

## Acceptance Criteria

1. Given `FC-CMD`, high-impact governance, authorization, freshness, lifecycle support, and preview support are confirmed, when an authorized global administrator starts enable or disable, then the UI opens a high-impact Consequence Preview with tenant identity, current lifecycle, intended lifecycle, known consequences, known unknowns, audit/evidence expectation, and recovery path, and submission is blocked if any required preview item is unavailable.
2. Given the lifecycle confirmation surface is open, when the user cancels, presses Escape, submits, or encounters an error, then focus is trapped while open and returns to the launching control afterward, and cancel or Escape does not commit any action.
3. Given the user confirms a valid lifecycle change, when the command is submitted, then the existing command gateway submits through `POST /api/v1/commands`, enforces one-at-a-time command policy, tracks status and SignalR nudges, and re-queries the tenant projection, and the tenant is shown as enabled or disabled only after authoritative projection confirmation.
4. Given the backend returns `TenantLifecycleStateAlreadySet` or `TenantDisabled`, when the lifecycle panel renders the result, then the UI shows safe localized rejection text and the correct non-Success lifecycle state, and it does not expose raw command payloads, metadata, stack traces, tokens, internal correlation ids, cursors, ETags, decoded claims, or PII.
5. Given the lifecycle outcome is accepted, pending, rejected, failed, degraded, unable to verify, audit pending, audit delayed, audit unavailable, or missing support, when the result is displayed, then every state remains distinct and accessible, and audit/evidence handoff is honest and never fabricated.
6. Given lifecycle disable/enable is approved as a reversible soft-delete availability control, when this story is selected for implementation, then the story is ready only when global-admin authorization reflection, complete consequence preview, one-at-a-time command policy, projection-confirmed lifecycle feedback, accessibility, localization, and responsive evidence are present, and hard destructive tenant deletion remains out of scope for this UI story and is reserved for future independent administrators-only CLI tooling.
7. Given this story is complete after gate clearance, when verification is run, then unit/component tests cover gate readiness, preview completeness, one-at-a-time locking, `TenantLifecycleStateAlreadySet`, `TenantDisabled`, projection confirmation, audit unavailable states, and no optimistic lifecycle transition, and Playwright or component tests verify destructive confirmation focus behavior, keyboard complete-or-exit, live-region politeness, forced-colors status rendering, responsive fail-closed behavior, support-safe copy, and stable selectors.

## Tasks / Subtasks

- [x] Verify the approved soft-delete correction and story-specific gates before implementation (AC: 6)
  - [x] Cite the approved 2026-06-06 Sprint Change Proposal and fallback approval record before changing lifecycle governance from unresolved to ready.
  - [x] Keep disable/enable scoped to reversible lifecycle availability control; do not implement hard delete, data purge, retention bypass, or CLI hard-delete behavior in Tenants UI.
  - [x] Proceed only when global-admin authorization reflection, complete consequence preview, one-at-a-time command policy, projection-confirmed lifecycle feedback, accessibility, localization, responsive safety, and support-safe evidence are satisfied.

- [x] Extend the existing lifecycle availability surface instead of rebuilding it (AC: 1, 3, 6)
  - [x] Start from `TenantLifecycleAvailability` and `TenantLifecycleActionAvailability`; keep same-state enable/disable represented as expected `TenantLifecycleStateAlreadySet` rejection.
  - [x] Move `TenantLifecycleGovernanceReadiness` to `Ready` only for the FR15 reversible soft-delete flow using the approved 2026-06-06 Sprint Change Proposal plus approved `FC-CNS`/`FC-CNC` fallbacks. Do not use this clearance for FR19 or hard tenant deletion.
  - [x] Only move `TenantLifecycleAuthorizationReflectionState` to `Authorized` from a proven server/BFF-reflected global-administrator source. Do not infer trusted global administrator authority from client-only claim parsing.
  - [x] Keep `stale`, `unknown`, degraded/unavailable detail, missing command surface, narrow/mobile safety context, incomplete preview, and unresolved governance as fail-closed states.
  - [x] Preserve tenant id, tenant name, current lifecycle status, freshness, and unavailable reason next to the action slot.

- [x] Add lifecycle command request, snapshot, and gateway support using existing command infrastructure (AC: 3, 4, 5)
  - [x] Add focused request/snapshot types under `src/Hexalith.Tenants.UI/State/TenantCommands/`. Prefer a single request with `TenantId` and `TenantLifecycleOperation` unless the current command-model split requires separate records.
  - [x] Add `EnableTenantAsync` and `DisableTenantAsync` to `ITenantCommandGateway`, `TenantCommandGateway`, and `UnavailableTenantCommandGateway`; do not introduce a second command bus or browser-side backend client.
  - [x] Submit existing contract records `EnableTenant` and `DisableTenant` with `messageId`, tenant `"system"`, domain `"tenants"`, aggregate id equal to the literal tenant id, command name `nameof(EnableTenant)` or `nameof(DisableTenant)`, and JSON payload serialized from the existing command record.
  - [x] Map `TenantLifecycleStateAlreadySetRejection`, `TenantDisabledRejection`, `TenantNotFoundRejection`, and `InsufficientPermissionsRejection` to command-neutral, support-safe localized text.
  - [x] Preserve shared `GetStatusAsync` as the command-status lookup; extend shared safe rejection mapping only when safe across command types.

- [x] Build the high-impact confirmation and consequence-preview flow for the approved FR15 soft-delete lifecycle operation (AC: 1, 2, 5)
  - [x] Prefer a focused component under `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/`, for example `TenantLifecycleCommandFlow.razor`, composed from `TenantDetailPage`.
  - [x] Use the Product/UX-approved inline structured-text `FC-CNS` fallback for the FR15 soft-delete flow. Do not build a generic FrontComposer `<ConsequencePreview>` replacement inside Tenants.
  - [x] Preview all required items: tenant identity, current lifecycle, intended lifecycle, known consequences, known unknowns, audit/evidence expectation, recovery path, freshness/projection evidence, authorization/governance facts, and command-surface readiness.
  - [x] Fail closed if any preview item is unavailable; name the missing item with a localized inline reason.
  - [x] Require elevated friction suitable for high-impact lifecycle action. At minimum, require explicit typed confirmation of the tenant id or exact operation phrase consistent with existing destructive flows.
  - [x] Trap focus while the confirmation surface is open; Escape and cancel must close without submitting and return focus to the launching enable/disable control.

- [x] Add lifecycle command state and projection-confirmation behavior (AC: 3, 5)
  - [x] Follow the shared snapshot pattern in `TenantCreateCommandSnapshot`, member-command snapshots, metadata snapshots, and the 3.3 set-configuration foundations: `Previewed`, `RequestSent`, `Accepted`, `ProjectionPending`, `Confirmed`, `Rejected`, `Failed`, `Degraded`, `UnableToVerify`, and audit states remain distinct.
  - [x] Enforce one-at-a-time command policy across tenant detail command surfaces. Lifecycle command submission must lock out metadata/member/configuration actions and respect existing in-flight locks.
  - [x] Treat SignalR only as a freshness nudge that triggers status/projection re-query; it must never advance lifecycle status or audit state by itself.
  - [x] Confirm `DisableTenant` only when the authoritative re-queried tenant projection shows `TenantStatus.Disabled`; confirm `EnableTenant` only when the projection shows `TenantStatus.Active`.
  - [x] Keep last-confirmed projection truth visible while the command is in flight. Do not overwrite `TenantDetail.Status`, list row status, page title, metadata, members, or configuration with intended status.
  - [x] If projection evidence is missing after terminal command status, show `projection pending` or `unable to verify`, not success.

- [x] Preserve existing tenant detail behavior and support-safe UX (AC: 1-5)
  - [x] Keep `TenantDetailPage` identity, status badge, lifecycle label, freshness badge, metadata edit flow, member access review, configuration read view, support-safe copy, stale/degraded messages, and return navigation behavior unchanged except for lifecycle command composition.
  - [x] Do not expose lifecycle command internals in browser-visible markup, logs, live regions, resource strings, copied text, or test assertions.
  - [x] Use whole-string localized EN/FR `.resx` entries under `Tenants.Lifecycle.*`; no runtime sentence-fragment assembly.
  - [x] Use stable selectors such as `tenants-lifecycle-actions`, `tenants-lifecycle-enable`, `tenants-lifecycle-disable`, `tenants-lifecycle-preview`, `tenants-lifecycle-confirm`, `tenants-lifecycle-cancel`, `tenants-lifecycle-state`, `tenants-lifecycle-audit`, `tenants-lifecycle-live-region`, and `tenants-lifecycle-unavailable-reason`.
  - [x] Keep high-impact command flows unavailable on mobile/narrow layouts that cannot preserve the full safety context.

- [x] Add focused tests and update test evidence (AC: 1-7)
  - [x] Add availability-model tests for the approved soft-delete path and fail-closed unresolved-evidence paths; missing authorization, preview, freshness, command surface, or responsive safety evidence must prevent submission.
  - [x] Add gateway tests for enable/disable payload shape, literal tenant id preservation, command name, message id, correlation id capture, unavailable gateway behavior, and safe rejection mapping.
  - [x] Add component tests for full preview completeness, missing preview item blocking, typed confirmation, cancel/Escape no commit, focus return, live-region politeness, forced-colors/no-color-only rendering, and stable selectors.
  - [x] Add lifecycle state tests for accepted/projection pending/confirmed/rejected/failed/degraded/unable-to-verify/audit states and SignalR nudge-only behavior.
  - [x] Extend `TenantDetailSurfaceTests` to prove lifecycle command composition preserves metadata/member/configuration/read behavior and last-confirmed projection truth.
  - [x] Add source or behavior tests proving no optimistic status transition and no raw support-unsafe content.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` and `tests/test-summary.md` if the repository continues the current story evidence practice.
  - [x] Run `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`; when `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility, use the xUnit v3 executable fallback documented in prior stories.

## Dev Notes

### Pre-Dev Gate

- Story 3.2 is ready-for-dev after the approved 2026-06-06 Sprint Change Proposal reclassified FR15 disable/enable as a reversible lifecycle soft-delete / availability-control operation, not hard destructive tenant deletion. The fallback approval record and UX readiness split were updated to allow FR15 under the approved `FC-CNS` inline consequence-preview fallback and `FC-CNC` one-at-a-time command policy once story-specific evidence is satisfied. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-06.md`; `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#Scope of this approval`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#FrontComposer Readiness & Fallbacks`]
- Hard destructive tenant deletion remains out of scope for this story and this repository's UI phase. It is reserved for a future independent administrators-only CLI tool, not Tenants UI. Do not add purge/delete contracts, backend endpoints, retention bypass, or any hard-delete UI behavior. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-06.md`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-15: Disable or enable a tenant`]
- Story 1.0 cleared `FC-LYT`, `FC-CMD`, `FC-CNC`, `FC-A11Y`, `FC-L10N`, and `FC-DOC`; the approved fallbacks cover `FC-CNS` and the one-at-a-time command policy for this FR15 soft-delete flow. Remaining obligations are story-specific: proven global-admin authorization reflection, complete preview content, `FC-TOK`/severity evidence for lifecycle risk, accessibility, localization, responsive safety, and no optimistic projection truth. [Source: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-05.md`; `docs/tenants-ui-phase-2-story-backlog.md#Candidate UI Stories`]
- `sprint-status.yaml` recorded `3-2-disable-or-enable-tenant-with-high-impact-confirmation: ready-for-dev` before implementation began. If implementation discovers a missing story-specific gate, fail closed in the story implementation rather than reclassifying FR15 as hard delete. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`; `_bmad-output/planning-artifacts/epics.md#Story 3.2: Disable or Enable Tenant with High-Impact Confirmation`]

### Story Source And Epic Context

- Story source is Epic 3 Story 3.2. Epic 3 covers tenant lifecycle and configuration control while preserving high-impact safety rules and projection truth. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Lifecycle and Configuration Control`]
- FR15 requires global-administrator-only disable/enable, Consequence Preview, `TenantLifecycleStateAlreadySet` for same-state requests, disabled status as an eventually-consistent availability signal, `TenantDisabled` rejection for commands targeting disabled tenants, no-color-only lifecycle status, and success only after projection confirmation. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-15: Disable or enable a tenant`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`]
- The command story sizing guardrail was applied correctly: FR15 is split into Story 3.1 availability/readiness and Story 3.2 command flow. Story 3.2 must build on Story 3.1, not replace it. [Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05-v2.md#Epic Coverage Validation`]

### Existing Implementation To Extend

- `TenantLifecycleAvailability` already evaluates `EnableTenant` and `DisableTenant` separately using current `TenantStatus`, freshness, detail surface kind, command-surface connection, governance readiness, authorization reflection, and narrow safety context. Same-state requests already name `TenantLifecycleStateAlreadySet`. Extend this model for the cleared-gate path rather than creating a separate lifecycle eligibility system. [Source: `src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs`]
- `TenantLifecycleActionAvailability.razor` already renders tenant lifecycle facts, enable/disable action slots, inline unavailable reasons, stable selectors, keyboard-reachable reasons, and live-region behavior. Story 3.2 should add the high-impact command/preview path here or compose a sibling lifecycle command component from the same action slot. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor`]
- `TenantDetailPage.razor` currently composes lifecycle actions beside tenant identity, status, lifecycle label, and freshness. It still passes `AuthorizationReflection=Indeterminate` and `GovernanceReadiness=Unresolved`; Story 3.2 may move governance to ready only for the approved FR15 soft-delete lifecycle flow, and must move authorization to authorized only from proven BFF/server-side global-administrator authority. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`]
- `ITenantCommandGateway` now has create/add/change/remove/update/set-configuration methods plus shared `GetStatusAsync`, but it still has no lifecycle methods. Add lifecycle methods there and in `TenantCommandGateway`/`UnavailableTenantCommandGateway`; do not introduce another command bus. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`]
- Story 3.3 added set-configuration command foundations to the gateway and shared command model. Treat that as useful current command infrastructure, not as evidence that Story 3.2's lifecycle-specific preview, authorization, and projection-confirmation safeguards are complete. [Source: `git show --stat --name-only 8e49477`; `_bmad-output/implementation-artifacts/3-3-set-tenant-configuration-key-value-with-consequence-preview.md`]

### Backend And Domain Contract Facts

- `EnableTenant` and `DisableTenant` already exist as plain public command records with `TenantId`. Do not add fields, XML docs, `sealed`, or marker interfaces. [Source: `src/Hexalith.Tenants.Contracts/Commands/EnableTenant.cs`; `src/Hexalith.Tenants.Contracts/Commands/DisableTenant.cs`; `_bmad-output/project-context.md#C# Language & Contract Rules`]
- `TenantAggregate.Handle(DisableTenant, ...)` requires trusted global administrator authority, rejects missing tenants, rejects already-disabled tenants with `TenantLifecycleStateAlreadySetRejection`, and emits `TenantDisabled` otherwise. `EnableTenant` mirrors this for already-active and emits `TenantEnabled` otherwise. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- `TenantLifecycleStateAlreadySetRejection` carries `TenantId`, `CurrentStatus`, `RequestedStatus`, and `CommandName`. It is a rejection, not NoOp or success. [Source: `src/Hexalith.Tenants.Contracts/Events/Rejections/TenantLifecycleStateAlreadySetRejection.cs`; `docs/event-contract-reference.md#DisableTenant`; `docs/event-contract-reference.md#EnableTenant`]
- `TenantDisabledRejection` applies to tenant-scoped commands targeting disabled tenants. Enabling a disabled tenant is the lifecycle recovery operation and should be confirmed only after projection shows `TenantStatus.Active`. [Source: `docs/compensating-commands.md#Tenant Lifecycle Correction`; `docs/idempotent-event-processing.md`]
- Tenant ids are meaningful caller-supplied strings. Preserve them literally; never parse, normalize, case-fold, or generate tenant ids as GUID/ULID. Only command `messageId` is client-generated ULID. [Source: `_bmad-output/project-context.md#Identity Rules`; `docs/production-auth-claim-contract.md#Identifier Casing Contract`]

### Command Flow And UX Guardrails

- The only command confirmation pattern is dispatch, status poll plus SignalR nudge, authoritative projection re-query, then confirmed only from the re-queried projection. SignalR alone must never advance lifecycle or audit state. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`]
- Consequence Preview opens only after validation, freshness, authorization, lifecycle support, preview support, and governance gates are eligible. If any preview item is unavailable, submission is blocked with a visible localized reason. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Component Patterns`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#destructive-control`]
- High-impact/destructive controls must not read as primary or casual actions. They require full preview, explicit confirmation, focus trap, safe non-committing Escape/cancel, focus return, no optimistic transition, and no mobile/narrow rendering when full safety context cannot be preserved. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Interaction Primitives`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#destructive-control`]
- Live-region politeness is driven by announcement intent, not badge color. Assertive is reserved for rejection, failure, unable-to-verify, degraded, or destructive-block states; never announce success before projection truth. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Accessibility Floor`]
- Use the canonical unavailable-action reason categories and command lifecycle vocabulary verbatim. Do not invent duplicate/timeout semantics; the specs explicitly say those tokens have no dedicated gloss. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#State Patterns`]

### Existing Files To Update And Preserve

- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`: compose lifecycle command flow and command activity locking; preserve current identity/status/freshness/metadata/member/configuration behavior.
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor`: extend or wrap the existing lifecycle action slot; do not lose inline reasons and stable selectors from Story 3.1.
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs`: extend availability for cleared governance and proven authorization; keep fail-closed order.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`: add lifecycle command request/snapshot types if the current shared command model file remains the repository pattern.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`, `TenantCommandGateway.cs`, `UnavailableTenantCommandGateway.cs`: add lifecycle command submission and safe rejection mapping.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`: add lifecycle command/preview/state/recovery copy with EN/FR parity.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`: add enable/disable gateway mapping and rejection tests.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs`, `TenantDetailSurfaceTests.cs`, and new lifecycle command-flow component tests: cover UI behavior, accessibility, focus, selectors, and no optimistic truth.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleAvailabilityTests.cs` and lifecycle command snapshot tests: cover state transitions and fail-closed gates.

### Scope Boundaries And Anti-Patterns

- Do not implement hard destructive tenant deletion or CLI hard-delete behavior in Tenants UI. Story 3.2 is only reversible disable/enable soft-delete / availability control with projection-confirmed status.
- Do not add backend endpoints, controllers, query contracts, command contracts, aggregate behavior, AppHost/Aspire plumbing, package versions, shared FrontComposer components, or generic technical scaffolding in Tenants.
- Do not modify submodules or move generic command/preview infrastructure into this domain repository. Shared scaffolding belongs in the relevant technical module.
- Do not treat disabled tenant enablement as an optimistic local recovery. It is confirmed only by authoritative projection re-query.
- Do not use client-only authorization guesses for global-administrator authority. UI reflects server/BFF authority and the backend/domain enforce.
- Do not show Success, "done", audit proof, or enabled/disabled status until projection or audit evidence proves it.
- Do not hide reasons in tooltips. Reasons must be inline-visible, programmatically associated, and keyboard/screen-reader reachable.
- Do not use raw state literals outside the canonical vocabulary/resource pattern where an existing typed enum/resource key exists.

### Previous Story Intelligence

- Story 3.1 established the lifecycle availability component, fail-closed governance defaults, same-state `TenantLifecycleStateAlreadySet` copy, visible unavailable reasons, and tests proving no lifecycle submission path exists. Story 3.2 may remove that no-submission invariant only for the approved FR15 reversible soft-delete flow and only when story-specific authorization, preview, responsive, and projection-confirmation evidence is present. [Source: `_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md`]
- Story 3.1 review fixed degraded/unavailable/unknown detail-surface reason mapping, live-region ARIA consistency, and hardened primary availability selection. Preserve those fixes. [Source: `_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md#Senior Developer Review (AI)`]
- Story 3.3 added set-configuration command foundations after the original blocked 3.2 story record. This means the future Story 3.2 dev agent should expect `SetTenantConfigurationAsync` and shared command snapshot additions to exist, but should not assume the configuration flow is complete because `sprint-status.yaml` still records Story 3.3 as `in-progress`. [Source: `git log --oneline -5`; `_bmad-output/implementation-artifacts/sprint-status.yaml`]
- Story 2.5 established metadata command activity locking from child flow to `TenantDetailPage`; Story 3.2 should generalize or reuse that page-level lock so lifecycle commands do not run concurrently with metadata/member/configuration commands. [Source: `_bmad-output/implementation-artifacts/2-5-edit-tenant-metadata-with-safe-validation.md`]
- Story 2.4 established destructive consequence preview, typed confirmation, projection-confirmed absence, honest audit handoff, support-safe copy, and no fabricated receipts. Story 3.2 should follow the same destructive-flow discipline while adapting preview content to tenant lifecycle. [Source: `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md`]
- Story 1.8 established support-safe identifier copy and copy/logging boundaries. Apply the same support-safety rules to lifecycle confirmation, live regions, resource strings, and tests. [Source: `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`]

### Git Intelligence

- Recent commits are story-scoped Conventional Commits: `feat(story-3.3): add tenant configuration command foundations`, `docs(story-3.2): record blocked lifecycle command story`, and `feat(story-3.1): Tenant Lifecycle Command Availability and Blocked-State Guardrail`. If this story is eventually implemented after the gate clears, a compatible commit would be `feat(story-3.2): disable or enable tenant with high-impact confirmation`. [Source: `git log --oneline -5`]
- `feat(story-3.3)` changed the command gateway interface/implementations, shared command model, and UI tests, but not lifecycle command methods or the governance gate. Account for those new method signatures in future test doubles. [Source: `git show --stat --name-only 8e49477`]
- The worktree was clean before this refresh wrote the story file. [Source: `git status --short`]

### Latest Technical Information

- Use the repo-pinned local stack and contracts: .NET 10, Blazor InteractiveServer, Fluent UI Blazor `5.0.0-rc.3-26138.1`, Aspire `13.4.2`, EventStore command gateway contracts, xUnit v3 `4.0.0-pre.128`, Shouldly, bUnit, and NSubstitute. Do not introduce new packages or package versions for this story. [Source: `Directory.Packages.props`; `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- Fluent UI Blazor MCP documentation currently targets component library version `5.0.0.26139`, while the project pins `5.0.0-rc.3-26138.1`. Treat MCP examples as potentially incompatible; verify component parameters, events, ARIA behavior, and token names against the local pinned package before implementation.
- No external API/package upgrade is required for this story. The material risks are governance bypass, client-only authority inference, optimistic lifecycle status, partial preview submission, command-state collapse, support-unsafe copy, and unintended changes to existing metadata/member/configuration flows.

### Project Structure Notes

- Source changes, when unblocked, should stay under `src/Hexalith.Tenants.UI/`, primarily `Components/Tenants/Lifecycle/`, `Components/Pages/`, `State/TenantDetail/`, `State/TenantCommands/`, `Services/Gateways/`, `Resources/`, and colocated CSS.
- Tests should stay under `tests/Hexalith.Tenants.UI.Tests/` using xUnit v3, Shouldly, bUnit, and NSubstitute; files remain plural `{Class}Tests.cs`.
- Domain contracts and server aggregate behavior already exist and are covered by existing server/testing/integration tests. Story 3.2 is a UI command-flow story, not a backend contract story.
- Planning conflict resolved by the approved 2026-06-06 Sprint Change Proposal: FR15 disable/enable is reversible soft-delete / availability control and Story 3.2 is ready-for-dev. Hard tenant deletion remains explicitly out of scope for future independent administrators-only CLI tooling.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 3.2: Disable or Enable Tenant with High-Impact Confirmation`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Lifecycle and Configuration Control`
- FR15 and shared contracts: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-15: Disable or enable a tenant`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`
- Readiness and gates: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md`; `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05-v2.md`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-05.md`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-06.md`
- Architecture and UX: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#destructive-control`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Component Patterns`
- Current UI implementation: `src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs`; `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`; `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`
- Domain contracts: `src/Hexalith.Tenants.Contracts/Commands/EnableTenant.cs`; `src/Hexalith.Tenants.Contracts/Commands/DisableTenant.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/TenantLifecycleStateAlreadySetRejection.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/TenantDisabledRejection.cs`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`
- Prior story evidence: `_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md`; `_bmad-output/implementation-artifacts/3-3-set-tenant-configuration-key-value-with-consequence-preview.md`; `_bmad-output/implementation-artifacts/2-5-edit-tenant-metadata-with-safe-validation.md`; `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md`; `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Correct-course/create-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, `architecture.md`, targeted PRD/addendum/UX/readiness/fallback sections, the approved 2026-06-06 FR15 reclassification proposal, Story 3.1 previous-story intelligence, current lifecycle UI/gateway/resource/test files, lifecycle domain contracts, recent Story 3.3 command-foundation commit data, and recent git history.
- Fluent UI Blazor MCP version check found documentation version `5.0.0.26139` is incompatible with the project pin `5.0.0-rc.3-26138.1`; story guidance requires local pinned-package verification.
- Microsoft Learn accessibility/focus research reinforced that keyboard traversal, focus return, and coherent tab order must be tested for the high-impact confirmation surface; the story keeps those checks in AC7 and component/Playwright test tasks.
- Correct-course approval was applied on 2026-06-06: FR15 disable/enable is reversible soft-delete / availability control, Story 3.2 is `ready-for-dev`, and hard tenant deletion is reserved for future independent administrators-only CLI tooling.
- Dev-story activation resolved workflow customization with no prepend/append steps and loaded `_bmad-output/project-context.md`, sprint status, and the full Story 3.2 file before implementation.
- Implementation kept hard destructive tenant deletion, purge, retention bypass, backend command contracts, and CLI hard-delete behavior out of scope.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility; validation used the documented xUnit v3 executable fallback.
- Validation passed: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`; xUnit v3 fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed 353/353; Tier 1 executable lanes passed Contracts 103/103, Client 47/47, Testing 181/181, Sample 31/31, UI 353/353.
- Senior Developer Review (AI) loaded story/workflow/checklist, project context, architecture/readiness evidence, git status/diff, story File List, implementation files, tests, and Microsoft Learn Blazor event-handling documentation for focus/default-event behavior. Review found and auto-fixed two verified issues: lifecycle command activity was released while accepted/projection-pending lifecycle commands still lacked authoritative projection confirmation, and the high-impact dialog did not provide a reliable native focus loop. Validation passed: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`; xUnit v3 fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed 370/370.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide refreshed.
- Story context scopes Story 3.2 to the approved reversible lifecycle soft-delete / availability-control flow.
- Story status is `ready-for-dev`; `sprint-status.yaml` was advanced to `ready-for-dev` after the approved 2026-06-06 correction.
- Story context identifies the key implementation risks: hard-delete scope creep, client-only authority inference, partial consequence preview, optimistic lifecycle status, command-state collapse, support-unsafe copy, and regressions to existing tenant detail command flows.
- Implemented lifecycle enable/disable through the existing tenant command gateway, using existing `EnableTenant`/`DisableTenant` command records and preserving literal tenant ids in command envelopes.
- Added `TenantLifecycleCommandRequest` and `TenantLifecycleCommandSnapshot` so lifecycle outcomes remain distinct across previewed, request sent, accepted, projection pending, confirmed, rejected, failed, degraded, unable to verify, and audit handoff states.
- Added the inline high-impact lifecycle consequence preview with tenant identity, current/intended lifecycle, consequences, unknowns, audit expectation, recovery path, projection/freshness evidence, authorization/governance facts, command-surface readiness, typed tenant-id confirmation, Escape/cancel close, focus return, and support-safe live regions.
- Wired tenant detail command locking across metadata, member, and lifecycle submissions; SignalR remains a nudge-only state transition and lifecycle success is confirmed only after refreshed tenant projection evidence shows `Disabled` or `Active`.
- Added server-side BFF global-admin authorization reflection from authenticated `HttpContext.User` plus `eventstore:tenant=system`; the real UI remains fail-closed when reflected authority is absent and tests prove the authorized reflection path.
- Added EN/FR whole-string lifecycle preview/state/audit/recovery resources and focused gateway, reducer, composition, and component tests; updated both test summary evidence files.
- Senior Developer Review (AI) fixed the one-at-a-time lifecycle lock so accepted/projection-pending lifecycle commands keep metadata/member/configuration/lifecycle actions unavailable until projection truth confirms or a terminal non-pending state releases the lock.
- Senior Developer Review (AI) added focus-loop sentinels to the high-impact lifecycle confirmation dialog and regression coverage for focus-trap structure, existing command-surface lock admission, and lock release only after projection-confirmed lifecycle truth.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-06T11:57:09+02:00

Outcome: Approved after auto-fixes. Story status moved to `done`; no critical issues remain.

Findings fixed:

- [HIGH] One-at-a-time command policy was released too early. `TenantLifecycleCommandFlow` raised `OnCommandActivityChanged(false)` in `finally` even when command status was still `Accepted` or `ProjectionPending`, allowing metadata/member/configuration/lifecycle actions before authoritative lifecycle projection confirmation. Fixed by holding command activity through `RequestSent`, `Accepted`, and `ProjectionPending`, releasing only after confirmed or terminal non-pending states, while keeping the active lifecycle refresh/result surface usable.
- [MEDIUM] The high-impact confirmation surface handled Tab with C# focus calls but did not prevent native browser tab movement, so focus trapping was not reliable. Fixed by adding focus-loop sentinels around the dialog and keeping Escape/cancel non-committing.

Verification:

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 370 total, 0 errors, 0 failed, 0 skipped.
- Documentation/reference check: Microsoft Learn ASP.NET Core Blazor event-handling docs confirmed Blazor default-event prevention/focus behavior considerations; local implementation used sentinel focus looping to avoid blocking text input key defaults.

### File List

- `_bmad-output/implementation-artifacts/3-2-disable-or-enable-tenant-with-high-impact-confirmation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleCommandSnapshotTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- `tests/test-summary.md`

### Change Log

- 2026-06-06T08:50:33+02:00 - Created Story 3.2 context and preserved blocked status pending FR15 high-impact governance clearance.
- 2026-06-06T09:18:56+02:00 - Refreshed Story 3.2 context to current head `8e49477`, incorporated Story 3.3 command-foundation context and current package pins, and preserved blocked/backlog status pending FR15 governance clearance.
- 2026-06-06T10:40:21+02:00 - Refreshed Story 3.2 after discovering draft 2026-06-06 FR15 soft-delete reclassification proposal; preserved blocked/backlog status during the pre-approval state.
- 2026-06-06T10:54:05+02:00 - Applied approved FR15 soft-delete correction, reserved hard delete for future administrators-only CLI tooling, and moved Story 3.2 to ready-for-dev.
- 2026-06-06T11:27:23+02:00 - Implemented reversible lifecycle enable/disable high-impact confirmation, command gateway support, projection-confirmed state handling, server-side BFF authorization reflection, localization, command-lock integration, and focused tests; moved Story 3.2 to review.
- 2026-06-06T11:57:09+02:00 - Senior Developer Review (AI) auto-fixed lifecycle command lock retention through projection confirmation and focus-loop trapping for the high-impact confirmation surface; validation passed 370/370 UI tests and Story 3.2 moved to done.
