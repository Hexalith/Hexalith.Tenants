---
baseline_commit: 0c62e91
---

# Story 2.3: Change Tenant Member Role

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 2.3. -->

## Story

As an authorized tenant administrator,
I want to change a tenant member's role,
so that member authority can be corrected while preserving domain safety and projection truth.

## Acceptance Criteria

1. Given an authorized user opens the change-role flow for an existing tenant member, when the role options render, then only assignable roles are offered with localized labels and accessible semantics, and unavailable role choices or blocked action reasons are explained inline rather than hover-only.
2. Given the user selects the member's current confirmed role, when the flow is submitted, then the UI shows an `already applied` NoOp state, does not call it projection-confirmed Success, and does not create duplicate or optimistic member-row mutation state.
3. Given the user selects an allowed new role, when the command is submitted, then the browser calls only the existing server-side command gateway, the gateway submits the existing `ChangeUserRole` domain command through `POST /api/v1/commands`, and a client-generated `messageId` ULID is used as the idempotency key.
4. Given the role-change command has been accepted, when status polling, SignalR projection notifications, or manual refresh occur, then SignalR is treated only as a freshness nudge, the UI re-queries the authoritative tenant detail/member projection, and success is shown only after the re-query proves the literal target user id has the requested `NewRole`.
5. Given the domain rejects role escalation, `TenantRole.Unknown`, a missing member, an unknown tenant, a disabled tenant, or insufficient permission, when the rejection is returned, then the command lifecycle displays a safe localized rejected state and exposes no raw payload, EventStore metadata, internal correlation id, bearer token, cursor, ETag, stack trace, or problem-detail internals.
6. Given freshness, authorization, tenant lifecycle, command surface availability, or command admission is unknown or unavailable, when the user attempts role change, then the action fails closed with the relevant inline `UnavailableActionReason`, and the current confirmed role remains visible without being overwritten by in-flight intent.
7. Given role change affects owner count or safety context, when the flow renders or is submitted, then owner-count context remains visible and accessible, last-owner or zero-owner risk is represented with warning semantics, and the role change is not blocked solely because it would reduce owner count to zero.
8. Given another command is in flight or confirmation is unknown, when the user attempts to submit role change again, then the command trigger is unavailable with a visible reason, focus remains recoverable, and duplicate submission does not create optimistic member state.
9. Given the role-change outcome is request sent, accepted, projection pending, confirmed, rejected, already applied, failed, degraded, unable to verify, audit pending, audit unavailable, or missing support, when the lifecycle panel renders, then each state remains visible, accessible, localized, and not collapsed into a generic success state.
10. Given verification is run, then unit/component tests cover current-role NoOp, allowed role change, `ChangeUserRole` submit shape, role escalation rejection, `TenantRole.Unknown`, `UserNotInTenantRejection`, `TenantDisabledRejection`, `InsufficientPermissionsRejection`, projection re-query confirmation, owner-count context, non-collapse lifecycle states, keyboard role selection, focus return, inline reasons, live-region politeness, forced-colors-safe status rendering, stable selectors, and EN/FR resource parity.

## Tasks / Subtasks

- [x] Extend the existing Tenants command gateway for change-role (AC: 3, 5)
  - [x] Add `ChangeUserRoleAsync(ChangeUserRoleCommandRequest request, CancellationToken cancellationToken = default)` to `ITenantCommandGateway`, `TenantCommandGateway`, and `UnavailableTenantCommandGateway`; do not add a second generic command gateway or browser-side backend client.
  - [x] Submit `ChangeUserRole` as a `SubmitCommandRequest` with `Tenant = "system"`, `Domain = "tenants"`, `AggregateId = tenantId`, `CommandType = nameof(ChangeUserRole)`, and `Payload = JsonSerializer.SerializeToElement(new ChangeUserRole(tenantId, userId, newRole))`.
  - [x] Use the already-registered `IUlidFactory.NewUlid()` for `MessageId`; never parse or generate `TenantId` or `UserId` as GUIDs or ULIDs.
  - [x] Validate tenant id, user id, and assignable `NewRole` before submit; reject `TenantRole.Unknown` and out-of-range roles client-side while still relying on backend validation/domain rejection as the gate.
  - [x] Add change-role-specific safe mappings for `RoleEscalationRejection`, `UserNotInTenantRejection`, `InsufficientPermissionsRejection`, `TenantDisabledRejection`, and `TenantNotFoundRejection`.
  - [x] Keep shared status fallback command-neutral and support-safe; do not let add-member copy appear in a change-role lifecycle.

- [x] Add change-role lifecycle state while reusing Story 2.1/2.2 command patterns (AC: 2, 4, 8, 9)
  - [x] Add `ChangeUserRoleCommandRequest` and a focused `TenantChangeRoleCommandSnapshot`, or generalize the existing member-command snapshot only if it reduces duplication without weakening add-member/create-tenant tests.
  - [x] Add an explicit `AlreadyApplied` lifecycle state or equivalent model value; same-role submission must render `already applied`, not `Confirmed`, `Rejected`, or `Failed`.
  - [x] Same-role selection should be resolved from the current confirmed member projection before gateway submission. The aggregate NoOp remains a backend guard; if backend status evidence later exposes a zero-event NoOp, map it to the same `AlreadyApplied` state.
  - [x] Store tenant id, target user id, current confirmed role, requested new role, message id, correlation id, safe message, rejection code, audit handoff state, owner-count context, and focus target.
  - [x] Preserve non-collapse: request sent, accepted, projection pending, confirmed, rejected, already applied, failed, degraded, unable to verify, audit pending, audit unavailable, and missing support remain distinct.
  - [x] Treat SignalR as a nudge only; it can request a re-query but cannot set `Confirmed`, `AlreadyApplied`, or audit availability.
  - [x] Keep last-confirmed member projection separate from submitted intent; never overwrite the row role until projection evidence proves the requested role.

- [x] Build the change-role flow from the tenant member row context (AC: 1, 2, 6, 7, 8, 9)
  - [x] Add a focused component under `src/Hexalith.Tenants.UI/Components/Tenants/Members/`, for example `ChangeTenantMemberRoleFlow.razor` plus colocated CSS if needed.
  - [x] Compose the flow from `MemberAccessReview` and keep `AddTenantMemberFlow` behavior intact.
  - [x] Use current tenant detail projection as context: tenant id, target user id, current role, tenant status, freshness, owner count, surface kind, and command admission state.
  - [x] Offer only `TenantOwner`, `TenantContributor`, and `TenantReader`; never offer `Unknown`.
  - [x] Keep the current role visible and selectable so the user can intentionally submit the NoOp path.
  - [x] Explain blocked roles/action reasons inline and associate the text with the relevant control using ARIA; tooltip-only explanation is insufficient.
  - [x] Represent owner-count risk with warning semantics when changing an owner to a non-owner would reduce owner count to zero; do not hard-block that case because the backend allows last-owner removal/role loss.
  - [x] Keep remove-member action slots visible but unavailable until Story 2.4; do not remove the existing reason catalog or member-table action semantics.
  - [x] Add stable selectors such as `tenants-change-role-flow`, `tenants-change-role-user-id`, `tenants-change-role-current-role`, `tenants-change-role-new-role`, `tenants-change-role-submit`, `tenants-change-role-unavailable-reason`, `tenants-change-role-owner-context`, `tenants-change-role-risk`, `tenants-change-role-lifecycle`, `tenants-change-role-state`, `tenants-change-role-audit`, and `tenants-change-role-refresh`.

- [x] Implement projection-confirmed role refresh (AC: 4, 6, 9)
  - [x] After command acceptance, run status lookup and tenant detail/member refresh; confirmation is earned only when the authoritative projection shows the target `UserId` with requested `NewRole`.
  - [x] If command status is terminal/completed but the member projection does not show the requested role yet, keep `projection pending`; do not show Success.
  - [x] If status lookup fails, SignalR is disconnected, projection confirmation cannot be proven, or current projection no longer contains the target user, show `unable to verify` or a safe rejection/recovery state as appropriate.
  - [x] Refresh the tenant detail/member table without losing the existing detail route, back link, overview, configuration view, add-member flow state, member table headers, or read-only safety context.
  - [x] Render audit handoff as `audit pending`, `audit unavailable`, or `missing support` until Epic 5 audit evidence surfaces are implemented; do not claim an audit receipt exists in Story 2.3.

- [x] Add safe rejection, support-safety, localization, accessibility, and responsive handling (AC: 1, 5, 6, 7, 9, 10)
  - [x] Add EN/FR resource parity for `Tenants.ChangeRole.*` keys covering labels, help text, role labels, validation, unavailable reasons, lifecycle states, rejection text, owner-count/risk text, audit handoff, and recovery actions.
  - [x] Use whole-string localized messages with named placeholders for tenant id, user id, current role, new role, owner count, and state labels where needed.
  - [x] Keep live-region politeness state-driven: polite for submitted/accepted/projection-pending/confirmed/already-applied, assertive for rejected/failed/degraded/unable-to-verify/blocked.
  - [x] Ensure keyboard users can open, complete, cancel, or exit the flow; validation moves focus to the failed field; blocked/rejected/unknown states move focus to a recoverable lifecycle region; focus returns to the launching change-role control when the flow closes or completes.
  - [x] Preserve no-color-only and forced-colors behavior with icon/shape/text for every lifecycle/status/risk state.
  - [x] At narrow widths, keep tenant identity, target user id, current role, requested role, owner-count/risk context, lifecycle state, unavailable reason, and recovery action visible or fail closed with a visible reason.
  - [x] Apply Story 1.8 support-safe discipline; do not render, log, announce, or copy raw command internals or unsafe support details.

- [x] Add focused tests and evidence updates (AC: 1-10)
  - [x] Extend `TenantCommandGatewayTests` or add change-role gateway tests proving submit request shape, ULID-shaped message id, literal case-sensitive tenant/user ids, `NewRole` serialized by name, returned correlation id capture, validation blocking for empty fields and `TenantRole.Unknown`, and safe rejection mappings.
  - [x] Add state/model tests for current-role `AlreadyApplied`, non-collapse lifecycle states, SignalR nudge only, projection-confirmed role evidence, no optimistic row mutation, one-at-a-time blocking, duplicate-submit recovery, and unknown confirmation.
  - [x] Add component tests for role options excluding `Unknown`, current-role NoOp, allowed role change, stale/unknown/disabled/unauthorized/command-surface fail-closed reasons, owner-count risk text, keyboard/focus behavior, live-region politeness, forced-colors-safe semantics, and stable selectors.
  - [x] Extend `TenantDetailSurfaceTests` / `MemberAccessReview` tests so adding the change-role flow does not break table caption, headers, row relationships, add-member flow, remove-member unavailable slots, copy buttons, or stale/degraded messaging.
  - [x] Add resource parity assertions for all new EN/FR keys.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` and `tests/test-summary.md` if verification summaries are maintained for this story.
  - [x] Use per-project test execution. If `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility, use the xUnit v3 in-process executable fallback and record the command.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 2 Story 2.3. Epic 2 adds tenant/member mutation flows over the Epic 1 read foundation and the Story 2.1/2.2 command lifecycle foundation. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.3: Change Tenant Member Role`]
- FR11 requires changing an existing member's role, with same-role NoOp shown as `already applied`, role escalation and `TenantRole.Unknown` rejected with safe localized text, and success only after projection confirmation. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-11`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`]
- ui-09 historically bundled add-member and change-role behind one shared availability gate, but sprint planning split them into Story 2.2 and Story 2.3. Reuse Story 2.2 foundations; do not rebuild command lifecycle or member-table scaffolding. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#A. Feature / FR -> backlog -> spec mapping`; `_bmad-output/implementation-artifacts/2-2-add-user-to-tenant-with-explicit-role.md`]

### Command Contract Details

- `ChangeUserRole` is `public record ChangeUserRole(string TenantId, string UserId, TenantRole NewRole);`. Commands are plain public records with primary constructors; do not add XML docs, `sealed`, marker interfaces, or new contract fields. [Source: `src/Hexalith.Tenants.Contracts/Commands/ChangeUserRole.cs`; `_bmad-output/project-context.md#C# Language & Contract Rules`]
- `UserRoleChanged` is `public record UserRoleChanged(string TenantId, string UserId, TenantRole OldRole, TenantRole NewRole) : IEventPayload;`. Projection confirmation should compare requested `NewRole` to the authoritative member projection, not to the submitted intent alone. [Source: `src/Hexalith.Tenants.Contracts/Events/UserRoleChanged.cs`; `docs/event-contract-reference.md#ChangeUserRole`]
- `TenantRole` has `Unknown = 0`, `TenantOwner`, `TenantContributor`, and `TenantReader`, serialized by `JsonStringEnumConverter<TenantRole>`. UI options must exclude `Unknown`; gateway payloads must serialize role names. [Source: `src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs`; `docs/event-contract-reference.md#TenantRole`]
- `ChangeUserRoleValidator` rejects empty tenant id, empty user id, out-of-range roles, and `TenantRole.Unknown`. Keep client validation aligned but do not trust the UI as the gate. [Source: `src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs`]
- `TenantAggregate.Handle(ChangeUserRole, ...)` rejects missing tenant, disabled tenant, insufficient permission, non-assignable role, and missing member; it returns `NoOp` when the target member already has `NewRole`; otherwise it emits `UserRoleChanged`. Authorization is tenant owner or trusted global administrator, and authorization is checked before domain NoOp so unauthorized same-role attempts are rejected rather than shown as already applied. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#Handle(ChangeUserRole)`]
- Safe command request shape is `messageId`, `tenant = "system"`, `domain = "tenants"`, `aggregateId = tenantId`, `commandType = "ChangeUserRole"`, and payload with `TenantId`, `UserId`, and explicit `NewRole`. [Source: `docs/compensating-commands.md#Wrong Role Assignment`]
- EventStore `CommandStatus` has no dedicated NoOp enum value (`Received`, `Processing`, `EventsStored`, `EventsPublished`, `Completed`, `Rejected`, `PublishFailed`, `TimedOut`). Story 2.3 must add UI-level `AlreadyApplied` handling instead of trying to parse a nonexistent `CommandStatus.NoOp`. [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandStatus.cs`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]

### Architecture And Boundary Requirements

- Tenants UI is Blazor InteractiveServer with a server-side BFF. The browser must not call backend services directly and must not hold backend access tokens. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `src/Hexalith.Tenants.UI/Program.cs`]
- All backend command egress goes through `ITenantCommandGateway` / `TenantCommandGateway`, which currently implements create-tenant, add-member, and status lookup through `IEventStoreGatewayClient.SubmitCommandAsync` plus `api/v1/commands/status/{correlationId}`. Extend this path; do not add a controller, browser `HttpClient`, or generic command infrastructure in Tenants. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `AGENTS.md#Domain Implementation Boundary`]
- Projection truth is authoritative. Status `Completed` or `EventsPublished` is not proof that the role changed; confirmation requires a tenant detail/member projection re-query that shows the literal target user id with requested `NewRole`. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#5. Layered Feedback State Set (AC4)`]
- SignalR projection notifications are freshness nudges only. A nudge can trigger re-query/refresh but cannot set confirmed success or audit availability. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#CP-4 Live signals are nudges, not proof`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Eventual-consistency signals (CP-4)`]
- The one-at-a-time command policy is already established. Do not introduce concurrent row commands, bulk role changes, toast batching, or optimistic success. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/implementation-artifacts/2-2-add-user-to-tenant-with-explicit-role.md#Senior Developer Review (AI)`]
- Command lifecycle belongs near the affected member context. Do not turn command lifecycle into primary navigation and do not use global message bars for row/form-level feedback. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#5.1 Operations Shell`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#6. Feedback Placement and Degradation Scope (AC5)`]

### Existing Files To Update And Preserve

- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`: add change-role submission while preserving create-tenant, add-member, and `GetStatusAsync`.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`: add `ChangeUserRole` mapping and safe rejection messages. Current add-member-specific mappings must not leak into change-role copy.
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`: add fail-closed change-role behavior matching the unavailable command surface.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`: currently contains create and add-member request/snapshot models plus shared lifecycle enums. Add change-role models or split into clearer files if useful, but preserve Story 2.1/2.2 tests and semantics.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor`: preserve add-member behavior, selectors, support-safety, lifecycle, and projection refresh.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`: current member table renders the add-member flow and row action slots. Make change-role actionable without breaking table caption, headers, role/status badges, owner context, reason catalog, copy buttons, stale/degraded messaging, or remove-member unavailable slots.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`: current detail page loads `TenantDetailSnapshot`, composes `MemberAccessReview`, and provides add-member projection evidence. Add change-role refresh/evidence hooks if needed while preserving safe back link, overview, configuration view, and loading/error/stale/degraded states.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`: add change-role resources with EN/FR parity and whole-string messages.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`: extend gateway coverage for `ChangeUserRole`.
- `tests/Hexalith.Tenants.UI.Tests/State/TenantAddMemberCommandSnapshotTests.cs` and related state tests: use patterns for lifecycle evidence while preserving add-member coverage.
- `tests/Hexalith.Tenants.UI.Tests/Components/AddTenantMemberFlowTests.cs`, `TenantDetailSurfaceTests.cs`, and related member/detail tests: extend without weakening existing Epic 1 and Story 2.2 assertions.

### Scope Boundaries And Anti-Patterns

- Do not change `src/Hexalith.Tenants.Contracts` or `src/Hexalith.Tenants.Server` unless existing contracts are proven wrong. The command, event, validator, aggregate, and rejection behavior already exist.
- Do not add invitations, email lookup, pending-member workflow, bulk role change, global Users navigation, generated CRUD mutation from query rows, or a new backend endpoint.
- Do not treat tenant id or user id as GUID/ULID; preserve literal, case-sensitive strings.
- Do not optimistically mutate the row role, owner count, or tenant detail projection before re-query evidence.
- Do not treat same-role NoOp as success, failure, or rejection. It is `already applied`.
- Do not overstate audit proof. Story 2.3 can hand off to audit pending/unavailable/missing support; Epic 5 owns evidence receipt/proof surfaces.
- Do not expose raw rejection payloads, command payloads, EventStore metadata, correlation ids, status internals, tokens, decoded JWT contents, cursors, ETags, stack traces, or real PII in UI copy, logs, docs, or copy affordances.

### Previous Story Intelligence

- Story 2.2 added `AddUserToTenantAsync` to the existing command gateway, `TenantAddMemberCommandSnapshot`, `AddTenantMemberFlow`, projection-confirmed refresh, EN/FR AddMember resources, and focused gateway/state/component/resource tests. Reuse these patterns instead of introducing parallel infrastructure. [Source: `_bmad-output/implementation-artifacts/2-2-add-user-to-tenant-with-explicit-role.md#Completion Notes List`]
- Story 2.2 review fixed a shared-status defect where generic rejected copy hardcoded create-tenant language and added support-safety redaction for bearer/jwt/cursor/etag markers. Story 2.3 must preserve those fixes and add change-role-specific regression tests around shared status copy. [Source: `_bmad-output/implementation-artifacts/2-2-add-user-to-tenant-with-explicit-role.md#Senior Developer Review (AI)`]
- Story 2.2 left `IsCommandSurfaceAvailable` not wired from `ITenantsBffComposition.IsCommandSurfaceConnected` as a shared follow-up. Do not solve that only for change-role unless the story needs it end-to-end; if touched, preserve create-tenant and add-member behavior or update all affected tests. [Source: `_bmad-output/implementation-artifacts/2-2-add-user-to-tenant-with-explicit-role.md#Observations (not auto-fixed - out of this story's scope)`]
- Story 1.7 established member table semantics, owner count/status/freshness context, and visible unavailable action reasons. Story 2.3 must turn only change-role into an actionable flow and preserve the surrounding safety context. [Source: `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md`; `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`]
- Story 1.8 established support-safe copy behavior. Apply the same posture to role-change command references and lifecycle copy. [Source: `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`; `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyClassifier.cs`]
- Current verification pattern: run Release builds and per-project tests. If `dotnet test` hits the .NET 10 Microsoft.Testing.Platform/VSTest incompatibility, use the xUnit v3 in-process executable fallback; Story 2.2's final verification passed 210/210 through that path. [Source: `_bmad-output/implementation-artifacts/2-2-add-user-to-tenant-with-explicit-role.md#Senior Developer Review (AI)`]

### Git Intelligence

- Recent commits are story-scoped Conventional Commits: `feat(story-2.2): Add User to Tenant with Explicit Role`, `feat(story-2.1): Create Tenant with Projection-Confirmed Command Lifecycle`, `docs(retro): record epic 1 retrospective`, `feat(story-1.8): Support-Safe Identifier Copy and Epic 1 Readiness Evidence`, and `feat(story-1.7): Tenant Member Table and Action Availability`. If Story 2.3 is committed later, use a Conventional Commit such as `feat(story-2.3): Change Tenant Member Role`. [Source: `git log --oneline -5`]
- Recent story changes stayed focused to story artifacts, sprint status, UI components/resources, gateway/state code, UI tests, and test summaries. Keep Story 2.3 similarly focused.

### Latest Technical Information

- Use the repo-pinned local stack and contracts: .NET 10 SDK/package pins from `global.json` and `Directory.Packages.props`, Fluent UI Blazor v5 RC inherited by the UI project, FrontComposer command/fallback contracts, EventStore command contracts, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not introduce package versions or new libraries for Story 2.3. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- No external API or package research is required for implementation beyond the pinned local contracts; the risk is contract reuse, NoOp semantics, support-safe status mapping, and truth-state correctness.

### Project Structure Notes

- Source changes should stay under `src/Hexalith.Tenants.UI/`, primarily `Components/Tenants/Members/`, `Components/Pages/`, `State/TenantCommands/`, `Services/Gateways/`, `Resources/`, and existing CSS colocated with affected components.
- Tests should stay under `tests/Hexalith.Tenants.UI.Tests/` with xUnit v3, Shouldly, bUnit, and NSubstitute. Test files remain plural `{Class}Tests.cs`.
- The story should not require changes to `src/Hexalith.Tenants.Contracts`, `src/Hexalith.Tenants.Server`, `src/Hexalith.Tenants`, AppHost, IntegrationTests, submodules, package metadata, or shared technical modules unless the existing command contract is proven wrong.
- Detected conflict to manage: the current `TenantCommandLifecycleState` enum lacks `AlreadyApplied`, while PRD/addendum require the same-role NoOp to render as `already applied`. Add that state or an equivalent explicit model path; do not overload `Confirmed`.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 2.3: Change Tenant Member Role`
- Epic context and command sizing: `_bmad-output/planning-artifacts/epics.md#Epic 2: Tenant Membership and Tenant Record Management`; `_bmad-output/planning-artifacts/epics.md#Command Story Sizing Guardrail`
- PRD and addendum: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-11`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#Core Interaction Principles - Truth, Safety & Recovery`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`
- UX: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Eventual-consistency signals (CP-4)`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Rejection / NoOp surfacing`
- Command/auth contracts: `src/Hexalith.Tenants.Contracts/Commands/ChangeUserRole.cs`; `src/Hexalith.Tenants.Contracts/Events/UserRoleChanged.cs`; `src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs`; `docs/event-contract-reference.md#ChangeUserRole`; `docs/compensating-commands.md#Wrong Role Assignment`
- Existing UI implementation: `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`; `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- Prior story evidence: `_bmad-output/implementation-artifacts/2-1-create-tenant-with-projection-confirmed-command-lifecycle.md`; `_bmad-output/implementation-artifacts/2-2-add-user-to-tenant-with-explicit-role.md`; `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md`; `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, `architecture.md`, targeted PRD/UX/addendum sections, Story 2.2, current Tenants UI source files, ChangeUserRole domain contracts/validator/aggregate behavior, EventStore `CommandStatus`, and recent git history.
- Validation was run against `.agents/skills/bmad-create-story/checklist.md`; disaster-prevention checks are reflected in this story context.
- Dev-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Story 2.3 was moved from `ready-for-dev` to `in-progress`; existing `baseline_commit: 0c62e91` was preserved.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.
- `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed: 244 total, 0 errors, 0 failed, 0 skipped.
- Broader executable regression signal: Contracts.Tests 103/103, Client.Tests 47/47, and Testing.Tests 181/181 passed. Server.Tests executable was attempted and failed in existing documentation/AppHost checks for missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and an unrelated deployment-readiness summary expectation.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 2.3 to changing an existing tenant member's role, reusing Story 2.1/2.2 command gateway/lifecycle/member-table foundations.
- Story context identifies the key implementation risks: same-role is `already applied` NoOp, `TenantRole.Unknown` is never assignable, current confirmed role must remain visible, success requires projection re-query evidence, owner-count risk is warning/friction rather than a hard block, and shared status/rejection copy must be command-appropriate and support-safe.
- Story context names concrete existing files to update and preservation requirements for add-member behavior, member table semantics, support-safety, localization, accessibility, responsive fail-closed behavior, and tests.
- Added `ChangeUserRoleAsync` to the existing tenant command gateway path; it submits the existing `ChangeUserRole` domain command through `POST /api/v1/commands` shape, uses `IUlidFactory.NewUlid()` for `MessageId`, preserves literal tenant/user ids, validates assignable roles, and maps change-role rejections to safe messages.
- Added explicit `AlreadyApplied` lifecycle state and `TenantChangeRoleCommandSnapshot` with current-role NoOp handling, zero-event backend NoOp handling, projection-confirmed role evidence, missing-member unable-to-verify handling, SignalR nudge-only behavior, audit handoff state, and non-collapse status transitions.
- Added `ChangeTenantMemberRoleFlow` under member row context with stable selectors, assignable roles excluding `Unknown`, current confirmed role visibility, inline unavailable reasons, owner-count risk warning without hard-blocking, duplicate-submit blocking, safe lifecycle copy, live-region politeness, close/focus recovery, and responsive/forced-colors CSS hooks.
- Updated `MemberAccessReview` and `TenantDetailPage` to compose change-role from the authoritative tenant detail projection while preserving add-member behavior, remove-member unavailable slots, member table semantics, copy buttons, stale/degraded messaging, and read-only safety context.
- Added EN/FR `Tenants.ChangeRole.*` resource parity and focused gateway, state, component, preservation, CSS, and resource tests. Updated maintained test summaries with Story 2.3 evidence.

### File List

- `_bmad-output/implementation-artifacts/2-3-change-tenant-member-role.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor.css`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/AddTenantMemberFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/ChangeTenantMemberRoleFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantChangeRoleCommandSnapshotTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- `tests/test-summary.md`

### Change Log

- 2026-06-06T05:39:54+02:00 - Created Story 2.3 context and marked it ready for development.
- 2026-06-06T06:02:52+02:00 - Implemented Story 2.3 change-role command gateway, lifecycle model, row-scoped flow, projection-confirmed refresh, localization/accessibility/support-safety handling, focused tests, and verification evidence; status set to review.
- 2026-06-06 - Senior Developer Review (AI) completed with auto-fix; resolved shared-status cross-command copy leak and member-row focus-return defect, added regression coverage, all checks green (256/256), status set to done.

## Senior Developer Review (AI)

**Reviewer:** Administrator (adversarial auto-fix review)
**Date:** 2026-06-06
**Outcome:** Approved after auto-fix — 0 Critical / 0 High remaining.

### Scope & Verification

- Reviewed every file in the Dev Agent Record File List against the 10 Acceptance Criteria and all `[x]` tasks. All ACs are genuinely implemented and every completed task has supporting code and tests — no false completion claims found.
- Git reality matches the story File List. The only additional changed file is `_bmad-output/story-automator/orchestration-1-20260605-153745.md`, which is excluded from review (automation tracking under `_bmad-output/`).
- Stable selectors, EN/FR `Tenants.ChangeRole.*` parity (placeholder counts verified), `ChangeUserRole` submit shape (`tenant=system`, `domain=tenants`, `aggregateId=tenantId`, role serialized by name, ULID message id, literal case-sensitive ids), `AlreadyApplied` NoOp, zero-event backend NoOp mapping, projection-confirmed role evidence, SignalR-nudge-only, owner-count risk-without-block, and non-collapse lifecycle states all confirmed present and tested.
- Build: `0 Warning(s) / 0 Error(s)` (Release). Tests via xUnit v3 in-process executable: **256 total, 0 failed, 0 skipped** (baseline 252 + 4 new from the regression theory).

### Findings Fixed Automatically

1. **[Medium] Cross-command rejection copy leak in the shared status path** — `TenantCommandGateway.SafeMessageForStatus`/`SafeRejectionCode` evaluated `SafeChangeRoleRejection` before `SafeAddMemberRejection`, so shared rejection types (`InsufficientPermissions`, `TenantDisabled`) surfaced change-role wording ("not authorized to change member roles", "member roles cannot be changed") inside add-member and create-tenant lifecycle panels. `GetStatusAsync` is shared and only carries a correlation id, so it cannot know the originating command. This re-introduced the class of defect fixed in the Story 2.2 review and violated this story's own task to "keep shared status fallback command-neutral; do not let one command's copy appear in another's lifecycle." Existing tests used substrings loose enough ("not authorized", "disabled") to miss it. **Fix:** introduced `SafeSharedStatusRejection` — command-UNIQUE rejection types (`TenantAlreadyExists`, `UserAlreadyInTenant`, `UserNotInTenant`) keep specific copy; SHARED types map to command-neutral copy. Submission-stage mappings (where the command is known) keep their command-specific copy. Added `Status_lookup_keeps_shared_rejection_copy_command_neutral` regression theory (fails on the old code).

2. **[Medium] Focus return targeted the wrong member row** — `MemberAccessReview` captured the per-row "Change role" launch button into a single `ElementReference` via `@ref` inside the rows `@foreach`, so it always pointed to the LAST actionable row. On close, focus returned to the wrong member's control whenever more than one member was actionable, violating AC9/task ("focus returns to the launching change-role control"). **Fix:** capture launch references into a `Dictionary<string, ElementReference>` keyed by literal user id and focus the exact launching member on close.

3. **[Low] Missing `Tenants.ChangeRole.Role.Unknown` resource** — `RoleLabel(Member.Role)` would render the raw resource key if the flow were shown for an `Unknown`-role member (the launch button gates this in normal use, but the symmetric `Tenants.Members.Role.Unknown` key already exists). **Fix:** added the EN/FR key for parity/robustness.

### Observations (not changed — out of this story's scope)

- `IsCommandSurfaceAvailable` / `IsChangeRoleAuthorized` / `IsAddMemberAuthorized` remain defaulted to `true` from `TenantDetailPage` (not wired from `ITenantsBffComposition.IsCommandSurfaceConnected`). This is the pre-existing shared follow-up carried from Story 2.2 and was intentionally not solved for change-role alone.
