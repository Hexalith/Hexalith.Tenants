---
baseline_commit: 1118f18
---

# Story 2.4: Remove Tenant Member with Consequence Preview

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 2.4. -->

## Story

As an authorized tenant administrator,
I want to remove a user from a tenant through a consequence preview and confirmed command lifecycle,
so that access removal is deliberate, projection-confirmed, and never shown as audit-proven before evidence exists.

## Acceptance Criteria

1. Given an authorized user opens remove-member for a tenant member, when validation, freshness, authorization, and lifecycle support are all eligible, then the UI opens a Consequence Preview using the approved inline structured-text fallback, and submission is blocked if any required preview item is unavailable.
2. Given the preview renders, when the target removal would leave zero owners or the target also holds global-administrator authority, then the UI shows elevated friction and risk context with visible localized text, icon, and accessible label, and last-owner removal remains allowed with extra friction while last-global-administrator rules remain outside tenant membership.
3. Given the user confirms removal, when the command is submitted, then the command gateway uses the existing command endpoint, enforces one-at-a-time submission, tracks command status and SignalR nudges, and re-queries the member projection, and the member is shown as removed only after authoritative projection confirmation.
4. Given the removal is duplicate, already applied, rejected, failed, degraded, or unable to verify, when the lifecycle panel renders, then the UI shows the exact safe state and does not display Success, and last-confirmed member data is not overwritten by in-flight intent.
5. Given audit evidence is pending, delayed, unavailable, missing implementation support, or not yet implemented by Epic 5, when the command reaches a terminal or unverifiable state, then the UI provides only an honest audit/evidence handoff state and appropriate recovery action such as wait, retry, inspect audit, continue read-only, or escalate, and it shows `audit available` or renders an Audit Evidence Receipt only when the Epic 5 evidence source is implemented and available, and the original event is not edited, deleted, or rewritten.
6. Given the destructive flow uses a modal, panel, or preview surface, when the user cancels, presses Escape, submits, or encounters failure, then focus remains trapped while open and returns to the launching control afterward, and no action commits on cancel or Escape.
7. Given this story is complete, when verification is run, then unit/component tests cover fail-closed gating, complete preview content, last-owner elevated friction, target-global-admin friction, duplicate/already-applied handling, projection confirmation, audit unavailable states, no optimistic removal, and the rule that audit proof/receipt UI is not asserted before the Epic 5 evidence source exists.
8. Given accessibility verification is run, then Playwright or component tests verify destructive confirmation focus behavior, keyboard complete-or-exit, live-region announcements, forced-colors-safe status rendering, stable selectors, and one-command-at-a-time locking.

## Tasks / Subtasks

- [x] Extend the existing Tenants command gateway for tenant-member removal (AC: 3, 4, 5)
  - [x] Add `RemoveUserFromTenantAsync(RemoveUserFromTenant request, CancellationToken cancellationToken = default)` to `ITenantCommandGateway`, `TenantCommandGateway`, and `UnavailableTenantCommandGateway`; do not add a generic command gateway, browser-side backend client, controller, or new endpoint.
  - [x] Submit the existing `RemoveUserFromTenant` domain command through the existing EventStore command path with `messageId = IUlidFactory.NewUlid()`, `tenant = "system"`, `domain = "tenants"`, `aggregateId = tenantId`, `commandType = nameof(RemoveUserFromTenant)`, and payload `new RemoveUserFromTenant(tenantId, userId)`.
  - [x] Validate tenant id and user id before submit. Tenant ids and user ids are literal caller-supplied strings; never parse or generate them as GUIDs or ULIDs.
  - [x] Add remove-member-specific safe submission mappings for `UserNotInTenantRejection`, `InsufficientPermissionsRejection`, `TenantDisabledRejection`, and `TenantNotFoundRejection`.
  - [x] Keep `GetStatusAsync` shared-status fallback command-neutral for shared rejection types. Do not reintroduce the Story 2.2/2.3 cross-command rejection-copy leak.

- [x] Add remove-member consequence preview and lifecycle state without optimistic projection mutation (AC: 1, 3, 4, 5)
  - [x] Add `RemoveUserFromTenant` and a focused `TenantRemoveMemberCommandSnapshot`, or reuse/generalize existing member-command state only if it preserves create/add/change-role behavior and tests.
  - [x] Track tenant id, target user id, current confirmed role, owner count, target-global-admin friction flag if available, preview completeness, message id, correlation id, safe message, rejection code, audit handoff state, focus target, and last-confirmed projection evidence.
  - [x] Represent `previewed`, request sent, accepted, projection pending, confirmed, rejected, already applied, duplicate/prevented duplicate, failed, degraded, unable to verify, audit pending, audit delayed, audit unavailable, and missing support distinctly in the remove flow. If the shared enum cannot express one of these safely, add a remove-specific state or extend the shared model with regression coverage.
  - [x] Treat "target already absent from the current confirmed projection before submit" as already applied/continue read-only and do not submit a new destructive command.
  - [x] Treat backend `UserNotInTenantRejection` after submit as safe already-applied only when an authoritative projection re-query confirms the target user is absent; otherwise show rejected or unable-to-verify without success language.
  - [x] Treat SignalR as a freshness nudge only. It may trigger a re-query, but it cannot set confirmed removal or audit availability.
  - [x] Preserve last-confirmed member data separately from submitted intent. Do not remove or hide the member row until the projection re-query proves the literal target `UserId` is absent from the tenant detail/member projection.

- [x] Build the destructive remove-member flow from the existing member row slot (AC: 1, 2, 6)
  - [x] Turn only the existing remove-member action slot into an actionable destructive flow when all fail-closed gates pass; preserve Add Member and Change Role behavior.
  - [x] Add a focused component under `src/Hexalith.Tenants.UI/Components/Tenants/Members/`, for example `RemoveTenantMemberFlow.razor` plus colocated CSS if needed.
  - [x] Launch from the row context in `MemberAccessReview`: tenant id, target user id, current role, tenant status, freshness, owner count, surface kind, authorization reflection, and command-surface availability.
  - [x] Use the approved `FC-CNS` inline structured-text fallback: constrained inner region, no partial preview, no new FrontComposer component implementation inside Tenants.
  - [x] Include all 10 preview items: tenant, target user, current role, owner count, affected access path, freshness, recovery path, audit expectation, known consequences, and known unknowns.
  - [x] Block preview open and submit if target user, tenant, role context, freshness, authorization, command lifecycle support, or any required preview item is unavailable; surface the specific inline `UnavailableActionReason`.
  - [x] Destructive action must not look like a casual/default primary button. It must require explicit confirmation, have a safe escape path, and explain blocked states inline rather than tooltip-only.
  - [x] Add stable selectors such as `tenants-remove-member-open`, `tenants-remove-member-flow`, `tenants-remove-member-preview`, `tenants-remove-member-preview-item`, `tenants-remove-member-target-user-id`, `tenants-remove-member-current-role`, `tenants-remove-member-owner-context`, `tenants-remove-member-global-admin-risk`, `tenants-remove-member-confirm`, `tenants-remove-member-cancel`, `tenants-remove-member-unavailable-reason`, `tenants-remove-member-lifecycle`, `tenants-remove-member-state`, `tenants-remove-member-audit`, `tenants-remove-member-recovery`, and `tenants-remove-member-refresh`.

- [x] Implement elevated friction and command-domain separation (AC: 2, 5)
  - [x] Last-owner removal (`ownerCount == 1` and target role is `TenantOwner`) remains allowed. Show warning/elevated friction and require deliberate confirmation; do not add a backend "must retain one owner" invariant.
  - [x] If target global-administrator authority can be reflected from available data, show platform-level friction. If it cannot be proven in this story, show it as a known unknown or unavailable evidence item; do not over-claim.
  - [x] Never dispatch `RemoveGlobalAdministrator` from this flow. This story removes tenant membership only: `RemoveUserFromTenant`, domain `tenants`, aggregate id = managed tenant id.
  - [x] Keep last-global-administrator hard-stop rules out of tenant membership removal. Those belong to the global-administrators aggregate and Story 4.4.
  - [x] Do not present session revocation, downstream enforcement, or token invalidation as known consequences unless backend evidence exists; list them as known unknowns.

- [x] Add honest audit handoff and recovery actions (AC: 5)
  - [x] Render audit states as audit pending, audit delayed, audit unavailable, or missing implementation support until Epic 5 audit evidence surfaces exist.
  - [x] Do not render `audit available`, an Audit Evidence Receipt, or audit proof links in Story 2.4 unless the Epic 5 evidence source is actually implemented and reachable.
  - [x] Recovery copy must use explicit forward-correction language: wait, refresh, retry status lookup, inspect audit, continue read-only, request permission, start correction, restore intended access, or escalate. Never use "undo" or imply event/state-store edits.
  - [x] Do not expose raw command payloads, EventStore metadata, correlation ids, bearer tokens, decoded JWTs, cursors, ETags, stack traces, problem-detail internals, or unsafe PII in visible copy, logs, announcements, or copy affordances.

- [x] Preserve existing member/detail behavior and wire projection confirmation (AC: 3, 4, 6)
  - [x] Update `MemberAccessReview` to compose the remove flow while preserving table caption, headers, row relationships, reason catalog, Add Member flow, Change Role flow, copy buttons, stale/degraded messaging, and focus return to the exact launching row.
  - [x] Update `TenantDetailPage` only as needed to provide remove-member projection evidence. Reuse the existing tenant detail refresh pattern; confirmation requires re-query evidence where the target user is absent.
  - [x] Use one-at-a-time locking across member command flows. While remove is in flight, other command triggers are unavailable with visible reasons; do not add bulk removal or multi-row command batching.
  - [x] Keep narrow-width behavior fail-closed: tenant identity, target user id, role, owner/global-admin risk, freshness, lifecycle state, unavailable reason, and recovery action remain visible, or the action is unavailable with a visible reason.

- [x] Add localization, accessibility, and focused tests (AC: 1-8)
  - [x] Add EN/FR `Tenants.RemoveMember.*` resource parity with whole-string messages and named placeholders for tenant id, user id, role, owner count, freshness, preview items, warnings, lifecycle states, audit states, unavailable reasons, and recovery actions.
  - [x] Ensure keyboard users can open, inspect, confirm, cancel, escape, recover, or close the destructive flow. Focus must stay inside the modal/panel/preview surface while open and return to the launching remove control afterward.
  - [x] Use state-driven live regions: polite for previewed/submitted/accepted/projection-pending/confirmed/audit-pending; assertive for rejected/failed/degraded/unable-to-verify/blocked.
  - [x] Preserve no-color-only and forced-colors behavior with icon/shape/text for lifecycle, audit, warning, and unavailable states.
  - [x] Extend `TenantCommandGatewayTests` for `RemoveUserFromTenant` request shape, ULID-shaped message id, literal case-sensitive tenant/user ids, returned correlation id capture, input validation, and safe rejection mappings.
  - [x] Add remove-member state/model tests for preview completeness, already-absent before submit, `UserNotInTenantRejection` reconciliation, projection-confirmed absence, no optimistic row removal, SignalR nudge only, duplicate-submit prevention, owner/global-admin friction, audit handoff, and non-collapse states.
  - [x] Add component tests for preview open/blocked gates, 10 preview items, last-owner warning-without-block, global-admin known-unknown/friction handling, destructive confirmation focus behavior, cancel/Escape no-op, one-command-at-a-time locking, inline reasons, stable selectors, resource parity, and support-safe copy.
  - [x] Extend member/detail preservation tests so remove-member activation does not regress Add Member, Change Role, member table semantics, copy controls, unavailable reason catalog, stale/degraded states, or focus return.
  - [x] Update maintained verification summaries in `_bmad-output/implementation-artifacts/tests/test-summary.md` and `tests/test-summary.md` if this repository continues that practice for Story 2.4.
  - [x] Run per-project verification. Prefer `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` plus the xUnit v3 in-process executable when `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 2 Story 2.4. Epic 2 adds tenant/member mutation flows over the Epic 1 read foundation and the Story 2.1/2.2/2.3 command lifecycle foundation. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.4: Remove Tenant Member with Consequence Preview`]
- FR12 requires fail-closed gating, Consequence Preview, elevated-friction handling, command lifecycle tracking, projection confirmation, and audit proof honesty for tenant membership removal. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR12`; `_bmad-output/planning-artifacts/epics.md#Story 2.4: Remove Tenant Member with Consequence Preview`]
- The sprint-status build-readiness note explicitly calls out Story 2.4: remove member gets audit handoff only until Epic 5 evidence source exists. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml#BUILD-READINESS NOTE`]
- The fallback approval record approved the `FC-CNS` inline structured-text fallback and the `FC-CNC` one-at-a-time command policy. Use those approved fallbacks; do not implement rich shared FrontComposer components inside Tenants. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#The three approved fallbacks`]

### Command Contract Details

- `RemoveUserFromTenant` already exists as `public record RemoveUserFromTenant(string TenantId, string UserId);`. Commands are plain public records with primary constructors; do not add XML docs, `sealed`, marker interfaces, or new contract fields. [Source: `src/Hexalith.Tenants.Contracts/Commands/RemoveUserFromTenant.cs`; `_bmad-output/project-context.md#C# Language & Contract Rules`]
- `UserRemovedFromTenant` already exists as `public record UserRemovedFromTenant(string TenantId, string UserId) : IEventPayload;`. Projection confirmation should compare the authoritative member projection, not the submitted intent. [Source: `src/Hexalith.Tenants.Contracts/Events/UserRemovedFromTenant.cs`; `docs/event-contract-reference.md`]
- `TenantAggregate.Handle(RemoveUserFromTenant, ...)` rejects missing tenant, disabled tenant, insufficient permission, and missing target member; otherwise it emits `UserRemovedFromTenant`. It does not enforce "at least one owner remains." [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#Handle(RemoveUserFromTenant)`]
- Domain tests already prove owner/global-admin authorization, last-owner removal by global administrator, duplicate retry observing removed membership as `UserNotInTenantRejection`, and envelope aggregate id winning over command body tenant id. Do not change these backend semantics unless a contract defect is proven. [Source: `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs#RemoveUserFromTenant_when_user_is_member_produces_UserRemovedFromTenant`; `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs#RBAC_RemoveUserFromTenant_allows_globalAdmin_to_remove_last_owner`; `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs#Concurrent_RemoveUserFromTenant_retry_observes_missing_member_and_rejects_duplicate`]
- Safe command request shape mirrors prior command stories: `messageId`, `tenant = "system"`, `domain = "tenants"`, `aggregateId = tenantId`, `commandType = "RemoveUserFromTenant"`, payload with `TenantId` and `UserId`. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `_bmad-output/project-context.md#Event-Sourcing & Domain Rules`]

### Consequence Preview Requirements

- The preview must present all 10 items in scoped order: tenant, target user, current role, owner count, affected access path, freshness, recovery path, audit expectation, known consequences, and known unknowns. The PRD reconciliation explicitly flags dropping any of these as a high-severity implementation risk. [Source: `docs/tenants-ui-remove-user-from-tenant-journey-spec.md#2.1 Preview content (enumerated exactly)`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-remove-user-journey.md#Gap 1`]
- Compose preview inputs from existing read-model evidence only: tenant detail/member projection and already-loaded row context. Do not add backend consequence or receipt endpoints. [Source: `docs/tenants-ui-remove-user-from-tenant-journey-spec.md#2.2 Compose the preview from read-model evidence only`; `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- Incomplete preview inputs block submit. Do not render a partial preview that could mislead. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#2. FC-CNS -> inline consequence text`; `docs/tenants-ui-remove-user-from-tenant-journey-spec.md#2.4 Fail-closed: incomplete inputs block submit`]
- Do not over-claim consequences. Session revocation, downstream enforcement, and token invalidation are known unknowns unless backend evidence proves them. [Source: `docs/tenants-ui-remove-user-from-tenant-journey-spec.md#2.3 Do not over-claim consequences`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-remove-user-journey.md#Gap 7`]

### Architecture And Boundary Requirements

- Tenants UI is Blazor InteractiveServer with a server-side BFF. Browser code must not call backend services directly or hold backend access tokens. [Source: `_bmad-output/planning-artifacts/architecture.md#Selected Starter: new src/Hexalith.Tenants.UI Blazor host composing the FrontComposer Shell`]
- All backend command egress goes through `ITenantCommandGateway` / `TenantCommandGateway` / `UnavailableTenantCommandGateway`. Extend this path; do not add a controller, browser `HttpClient`, or generic command infrastructure in Tenants. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `AGENTS.md#Domain Implementation Boundary`]
- Projection truth is authoritative. Status `Completed` or `EventsPublished` is not proof that the member was removed; confirmation requires tenant detail/member projection re-query proving the literal target `UserId` is absent. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Context Analysis`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#5.2 Non-collapse invariant`]
- SignalR projection notifications are freshness nudges only. A nudge can prompt refresh/re-query but cannot set confirmed success or audit availability. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md#3.4 SignalR is a freshness nudge only`]
- The one-at-a-time command fallback is approved and already established by prior command stories. Do not introduce concurrent row commands, bulk removal, toast batching, or optimistic success. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#3. FC-CNC -> one-at-a-time command policy`]
- Command lifecycle belongs near the affected member row/panel. Do not turn command lifecycle into navigation or page-global feedback except for page-level/system degradation. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md#6. Feedback Placement and Degradation Scope`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#command-lifecycle-panel`]

### Existing Files To Update And Preserve

- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`: add remove-member submission while preserving create-tenant, add-member, change-role, and `GetStatusAsync`.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`: add `RemoveUserFromTenant` mapping and remove-member-specific safe rejection messages; keep shared status rejection copy command-neutral.
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`: add fail-closed remove-member behavior matching unavailable command surface.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`: currently contains create/add-member/change-role request and snapshot models plus shared lifecycle enums. Add remove models here or split into clearer files only if useful; preserve existing tests and semantics.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`: current member table renders add/change role flows and a remove-member unavailable slot. Make remove actionable only when eligible, and preserve table semantics, reason catalog, exact-row focus return, and Add/Change behavior.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor` and `ChangeTenantMemberRoleFlow.razor`: use as implementation patterns for gateway submission, status refresh, projection evidence, live-region behavior, and focus recovery; do not break them.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`: provide remove-member projection evidence if needed, reusing current detail refresh and projection-evidence patterns.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`: add remove-member resources with EN/FR parity and whole-string messages.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`: extend gateway coverage for `RemoveUserFromTenant`.
- `tests/Hexalith.Tenants.UI.Tests/State/`: add remove-member lifecycle/model tests matching existing add/change-role tests.
- `tests/Hexalith.Tenants.UI.Tests/Components/`: add remove-member flow tests and extend `TenantDetailSurfaceTests` / member tests without weakening existing assertions.

### Scope Boundaries And Anti-Patterns

- Do not change `src/Hexalith.Tenants.Contracts` or `src/Hexalith.Tenants.Server` unless existing contracts are proven wrong. The remove command, event, aggregate behavior, and rejection behavior already exist.
- Do not add invitations, email lookup, pending-member workflow, bulk removal, generated CRUD mutation from query rows, or a global Users navigation tab.
- Do not parse tenant id or user id as GUID/ULID; preserve literal, case-sensitive strings.
- Do not optimistically remove the member row, decrement owner count, or mutate tenant detail state before projection evidence proves the target user is absent.
- Do not treat last-owner removal as a backend block. It is a UI warning/elevated-friction path and remains allowed by the domain.
- Do not dispatch `RemoveGlobalAdministrator`, edit the `global-administrators` aggregate, or apply last-global-administrator hard-stop rules in tenant membership removal.
- Do not overstate audit proof. Story 2.4 can hand off to audit pending/delayed/unavailable/missing support; Epic 5 owns audit evidence receipt/proof surfaces.
- Do not expose raw rejection payloads, command payloads, EventStore metadata, correlation ids, status internals, tokens, decoded JWT contents, cursors, ETags, stack traces, or real PII in UI copy, logs, docs, announcements, or copy affordances.

### Previous Story Intelligence

- Story 2.3 added `ChangeUserRoleAsync`, `TenantChangeRoleCommandSnapshot`, `ChangeTenantMemberRoleFlow`, projection-confirmed refresh, EN/FR ChangeRole resources, and focused gateway/state/component/resource tests. Reuse these patterns for remove-member instead of introducing parallel infrastructure. [Source: `_bmad-output/implementation-artifacts/2-3-change-tenant-member-role.md#Completion Notes List`]
- Story 2.3 review fixed a shared-status defect where shared rejection types surfaced command-specific copy in the wrong lifecycle panel. Story 2.4 must keep `GetStatusAsync` command-neutral for shared rejection types and use command-specific copy only where command context is known. [Source: `_bmad-output/implementation-artifacts/2-3-change-tenant-member-role.md#Senior Developer Review (AI)`]
- Story 2.3 review fixed member-row focus return by keying launch `ElementReference`s by literal user id. Use the same exact-row focus-return pattern for remove-member. [Source: `_bmad-output/implementation-artifacts/2-3-change-tenant-member-role.md#Senior Developer Review (AI)`]
- Story 2.2 left `IsCommandSurfaceAvailable` not wired from `ITenantsBffComposition.IsCommandSurfaceConnected` as a shared follow-up. Do not solve that only for remove-member unless the story needs it end-to-end; if touched, preserve create/add/change behavior or update all affected tests. [Source: `_bmad-output/implementation-artifacts/2-2-add-user-to-tenant-with-explicit-role.md#Observations (not auto-fixed - out of this story's scope)`]
- Story 1.7 established member table semantics, owner count/status/freshness context, and visible unavailable action reasons. Story 2.4 turns remove-member actionable but must preserve the surrounding safety context. [Source: `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md`; `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`]
- Story 1.8 established support-safe copy behavior. Apply the same posture to remove-member command references, lifecycle copy, preview text, and recovery actions. [Source: `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`; `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyClassifier.cs`]

### Git Intelligence

- Recent commits are story-scoped Conventional Commits: `feat(story-2.3): Change Tenant Member Role`, `feat(story-2.2): Add User to Tenant with Explicit Role`, `feat(story-2.1): Create Tenant with Projection-Confirmed Command Lifecycle`, `docs(retro): record epic 1 retrospective`, and `feat(story-1.8): Support-Safe Identifier Copy and Epic 1 Readiness Evidence`. If Story 2.4 is committed later, use a Conventional Commit such as `feat(story-2.4): Remove Tenant Member with Consequence Preview`. [Source: `git log --oneline -5`]
- Story 2.3 touched the same likely areas Story 2.4 will touch: story artifacts, sprint status, UI member components/resources, gateway/state code, UI tests, and test summaries. Keep Story 2.4 similarly focused. [Source: `git show --stat --name-only 1118f18 --`]

### Latest Technical Information

- Use the repo-pinned local stack and contracts: .NET 10 SDK/package pins from `global.json` and `Directory.Packages.props`, Fluent UI Blazor v5 RC inherited by the UI project, FrontComposer command/fallback contracts, EventStore command contracts, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not introduce package versions or new libraries for Story 2.4. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- No external API/package research is required for implementation beyond the pinned local contracts. The primary risks are contract reuse, destructive-flow preview completeness, last-owner/global-admin friction semantics, support-safe status mapping, and projection/audit truth correctness.

### Project Structure Notes

- Source changes should stay under `src/Hexalith.Tenants.UI/`, primarily `Components/Tenants/Members/`, `Components/Pages/`, `State/TenantCommands/`, `Services/Gateways/`, `Resources/`, and colocated CSS.
- Tests should stay under `tests/Hexalith.Tenants.UI.Tests/` with xUnit v3, Shouldly, bUnit, and NSubstitute. Test files remain plural `{Class}Tests.cs`.
- The story should not require changes to `src/Hexalith.Tenants.Contracts`, `src/Hexalith.Tenants.Server`, `src/Hexalith.Tenants`, AppHost, IntegrationTests, submodules, package metadata, or shared technical modules unless an existing command contract is proven wrong.
- Detected conflict to manage: existing `TenantCommandLifecycleState` lacks explicit `Previewed`, `Duplicate`, and `AuditDelayed` values, while remove-member UX needs those states to remain distinct. Add remove-specific states or safely extend the shared model with regression tests; do not overload `Confirmed` or `Failed`.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 2.4: Remove Tenant Member with Consequence Preview`
- Epic context and command sizing: `_bmad-output/planning-artifacts/epics.md#Epic 2: Tenant Membership and Tenant Record Management`; `_bmad-output/planning-artifacts/epics.md#Command Story Sizing Guardrail`
- PRD and addendum: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR12`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/reconcile-remove-user-journey.md`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`; `_bmad-output/planning-artifacts/architecture.md#Project Context Analysis`; `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md`
- UX: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#consequence-preview`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#destructive-command-trigger`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#command-lifecycle-panel`
- Remove journey and truth specs: `docs/tenants-ui-remove-user-from-tenant-journey-spec.md`; `docs/tenants-ui-truth-state-and-action-availability-spec.md`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md`
- Command/auth contracts: `src/Hexalith.Tenants.Contracts/Commands/RemoveUserFromTenant.cs`; `src/Hexalith.Tenants.Contracts/Events/UserRemovedFromTenant.cs`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`; `docs/event-contract-reference.md`; `docs/compensating-commands.md`
- Existing UI implementation: `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`; `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- Prior story evidence: `_bmad-output/implementation-artifacts/2-1-create-tenant-with-projection-confirmed-command-lifecycle.md`; `_bmad-output/implementation-artifacts/2-2-add-user-to-tenant-with-explicit-role.md`; `_bmad-output/implementation-artifacts/2-3-change-tenant-member-role.md`; `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md`; `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, `architecture.md`, targeted PRD/UX/remove-user/audit/truth/fallback sections, Story 2.3, current Tenants UI source files, RemoveUserFromTenant domain contracts/aggregate behavior/tests, and recent git history.
- Validation was run against `.agents/skills/bmad-create-story/checklist.md`; disaster-prevention checks are reflected in this story context.
- Dev-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Initial `dotnet test` attempt hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility, so validation used build plus xUnit v3 in-process executables.
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed: 283 total, 0 errors, 0 failed, 0 skipped.
- Broader executable regression signal: Contracts.Tests 103/103, Client.Tests 47/47, Testing.Tests 181/181, and Sample.Tests 31/31 passed.
- Server.Tests executable was attempted and failed in existing documentation/AppHost checks for missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` plus stale deployment-readiness evidence.
- IntegrationTests executable was attempted; DAPR-dependent tests reported unavailable prerequisites and 54 tests failed with DaprException/InternalServerError behavior in this environment.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 2.4 to tenant membership removal through existing UI command infrastructure, not backend contract or shared FrontComposer component work.
- Story context identifies the key implementation risks: full 10-item preview, no partial destructive preview, last-owner allowed with friction, global-admin target friction without `RemoveGlobalAdministrator`, projection-confirmed absence before row removal, no audit proof before Epic 5, support-safe lifecycle/recovery copy, and preservation of Story 2.1-2.3 command patterns.
- Extended the existing Tenants command gateway with `RemoveUserFromTenantAsync`, preserving the existing EventStore command endpoint and command-neutral shared status lookup.
- Added `RemoveUserFromTenant` and `TenantRemoveMemberCommandSnapshot` with explicit previewed, duplicate-prevented, projection-pending, confirmed, already-applied, degraded, unable-to-verify, audit-delayed, audit-unavailable, and missing-support states.
- Added `RemoveTenantMemberFlow` with the approved inline structured-text consequence preview, all 10 required preview items, explicit target-user confirmation, last-owner friction, global-admin known/unknown risk copy, honest audit/recovery handoff, and stable selectors.
- Wired remove-member activation from `MemberAccessReview` and projection evidence from `TenantDetailPage`, preserving add-member, change-role, member table semantics, copy controls, stale/degraded messaging, and exact-row focus return.
- Added EN/FR `Tenants.RemoveMember.*` resources and focused gateway, state, component, preservation, resource parity, and summary tests.

### File List

- `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor.css`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/AddTenantMemberFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/ChangeTenantMemberRoleFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantRemoveMemberCommandSnapshotTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- `tests/test-summary.md`

### Change Log

- 2026-06-06T06:29:59+02:00 - Created Story 2.4 context and marked it ready for development.
- 2026-06-06T06:51:39+02:00 - Implemented remove-member consequence preview, projection-confirmed lifecycle, honest audit handoff, localization, focused tests, and marked story ready for review.
- 2026-06-06T07:08:03+02:00 - Senior Developer Review (AI) completed: auto-fixed submission-time `UserNotInTenant` reconciliation gap in `RemoveTenantMemberFlow`, added regression test, verified build (0 warnings) and 286 UI tests passing, and marked story done.

## Senior Developer Review (AI)

**Reviewer:** Administrator (adversarial review on 2026-06-06)
**Outcome:** Approve (1 medium issue auto-fixed; 2 low observations recorded)
**Verification:** `dotnet build tests/Hexalith.Tenants.UI.Tests/...csproj -c Release -m:1 --no-restore` → 0 warnings / 0 errors. `tests/.../bin/Release/net10.0/Hexalith.Tenants.UI.Tests` → 286 total, 0 failed, 0 skipped.

### Scope validated

- **File List vs git:** Matches. The only extra git change (`_bmad-output/story-automator/orchestration-*.md`) is an excluded automation artifact.
- **AC coverage:** AC1–AC8 traced to implementation and tests — fail-closed gating and no partial preview (`IsPreviewComplete` + `UnavailableReason`), complete 10-item consequence preview, last-owner elevated friction without hard-block, target-global-admin known/known-unknown handling without dispatching `RemoveGlobalAdministrator`, projection-confirmed absence before confirmation (no optimistic row removal), `UserNotInTenant`-after-submit reconciliation only after absent projection, SignalR nudge-only, honest audit pending/delayed/unavailable/missing-support handoff with no receipt/`audit available` before Epic 5, support-safe redaction in the gateway, and EN/FR `Tenants.RemoveMember.*` parity (81 = 81).
- **Tasks marked [x]:** Spot-audited against code — all verified done (gateway extension, snapshot model, destructive flow component, MemberAccessReview/TenantDetailPage wiring, localization, and the four test suites).

### Findings

1. **[MEDIUM — FIXED]** Submission-time `UserNotInTenant` rejection (gateway maps a synchronous `EventStoreGatewayException` → `Rejected`/`UserNotInTenant` with no tracking handle) could not be reconciled to `AlreadyApplied` through the UI: `RefreshStatusAsync` early-returned when `MessageId`/`CorrelationId` were null and `CanRefresh` disabled the Refresh button, so the AC4 projection re-query was unreachable and the Rejected recovery copy pointed at a disabled control. Fixed in `RemoveTenantMemberFlow.razor`: refresh is now gated on `Intent` (not the tracking handle), the status lookup is skipped when no handle exists, and the projection re-query + `ConfirmProjection` still run so an absent target reconciles to `AlreadyApplied`. Added regression test `Submission_time_user_not_in_tenant_rejection_reconciles_to_already_applied_after_absent_projection`. Change is remove-flow-specific (no effect on create/add/change-role).
2. **[LOW — observation, not changed]** On a successful removal, once `TenantDetailPage` re-queries and the target leaves the member projection, `MemberAccessReview.ActiveRemoveMember` becomes null and the `RemoveTenantMemberFlow` panel unmounts, so the `Confirmed` lifecycle + audit-pending handoff is not durably shown for the happy path. This is consistent with the row disappearing on confirmation; changing it is a product/UX decision, so it is recorded rather than auto-changed.
3. **[LOW — observation, not changed]** The destructive flow is an inline panel with focus-on-open, Escape-to-close, and exact-row focus return, but no hard keyboard focus *trap* (Tab can leave the panel while open). This matches the Story 2.3 change-role precedent and is not a regression.
