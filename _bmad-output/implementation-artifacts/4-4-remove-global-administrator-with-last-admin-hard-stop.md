---
baseline_commit: c1b3f4c
created: 2026-06-06T15:17:52+02:00
---

# Story 4.4: Remove Global Administrator with Last-Admin Hard Stop

Status: backlog

<!-- Note: Created by the BMAD create-story workflow for Story 4.4. -->

## Story

As an authorized global administrator,
I want to remove global administrator authority only when it is safe and domain-allowed,
so that the platform never loses its last global administrator and the UI never treats that case as completable friction.

## Acceptance Criteria

1. Given the Global Administrators review surface has current projection data, when remove availability is computed for each administrator, then removing the last global administrator is unavailable with a visible localized reason before submission, and the UI does not render it as elevated friction, override, or completable confirmation.
2. Given a remove action is available for a non-last global administrator, when an authorized global administrator opens the remove flow, then the UI renders a high-impact Consequence Preview naming platform authority scope, target user id, current admin count, known consequences, known unknowns, audit/evidence expectation, and recovery path, and submission is blocked if any required preview item is unavailable.
3. Given the user confirms removal, when the command is submitted, then the command gateway submits `RemoveGlobalAdministrator` through the existing command endpoint, enforces one-at-a-time locking, tracks command status and SignalR nudges, and re-queries the fixed global-administrators projection, and the user is shown as removed only after projection confirmation.
4. Given a race condition makes the target the last global administrator before processing, when the backend returns `LastGlobalAdministrator`, then the lifecycle panel shows a safe localized hard-blocked rejection, and the UI does not show Success or retry as tenant-membership removal.
5. Given the target is not a global administrator, when the backend returns `GlobalAdministratorNotFound`, then the lifecycle panel shows safe localized rejection text, and the review surface remains based on last-confirmed projection truth.
6. Given the remove flow is cancelled, fails, is rejected, or cannot be verified, when result and audit/evidence state are rendered, then each state remains distinct, support-safe, accessible, and localized, and the action never edits tenant aggregates, tenant membership, events, projections, or state-store history.
7. Given this story is complete, when verification is run, then unit/component tests cover last-admin pre-submit unavailability, race-returned `LastGlobalAdministrator`, `GlobalAdministratorNotFound`, fixed-scope command mapping, projection-confirmed removal, audit unavailable states, one-at-a-time locking, and no tenant membership command.
8. Given accessibility or E2E verification is run, then destructive confirmation focus trap/return, keyboard complete-or-exit, live-region politeness, forced-colors rendering, responsive fail-closed behavior, support-safe errors, and stable selectors are verified.

## Tasks / Subtasks

- [x] Resolve the Story 4.4 governance readiness conflict before source changes (AC: 1-8)
  - [x] Verify whether a separate Product/UX governance decision has cleared FR19 global-administrator grant/remove command management after 2026-06-06.
  - [x] If no newer clearance exists, stop source implementation, record the blocker, and return this story to backlog/blocked tracking. Do not silently implement `RemoveGlobalAdministrator`.
  - [x] If implementation proceeds, keep this story remove-only. Do not implement `SetGlobalAdministrator`, grant flows, bulk revocation, tenant membership removal, audit recovery, or event/projection repair in Story 4.4.

- [ ] Add a focused global-administrator remove command request and gateway method (AC: 3, 4, 5, 6, 7)
  - [ ] Add a request model such as `RemoveGlobalAdministratorCommandRequest(string UserId)` under `src/Hexalith.Tenants.UI/State/GlobalAdministrators/` or the existing `State/TenantCommands/` command models area.
  - [ ] Add `RemoveGlobalAdministratorAsync` to `ITenantCommandGateway`, `TenantCommandGateway`, and `UnavailableTenantCommandGateway`.
  - [ ] Submit through `IEventStoreGatewayClient.SubmitCommandAsync` with `tenant = "system"`, `domain = "global-administrators"`, `aggregateId = "global-administrators"`, `commandType = nameof(RemoveGlobalAdministrator)`, and payload `{ UserId }` only.
  - [ ] Generate the command `messageId` with the existing `IUlidFactory`; never parse the target user id as a GUID or ULID.
  - [ ] Map `LastGlobalAdministratorRejection` to hard-blocked safe copy, not elevated friction, override, NoOp, retry-as-member-removal, or Success.
  - [ ] Map `GlobalAdministratorNotFoundRejection` to safe rejected copy that keeps the last-confirmed projection visible and asks for refresh before any further action.
  - [ ] Map `InsufficientPermissionsRejection` to command-neutral platform-governance copy without leaking hidden administrator data.
  - [ ] Keep shared `GetStatusAsync` rejection mapping command-neutral where rejection types are shared, and add global-admin-specific handling for unique global-admin rejection types.

- [ ] Extend the Global Administrators review page with remove availability and consequence preview (AC: 1, 2, 6, 8)
  - [ ] Use the Story 4.2 fixed read surface in `GlobalAdministratorsPage.razor` as the source of row truth. Do not read tenant membership, user membership, claims, tenant detail, events, DAPR state stores, or projections directly.
  - [ ] Compute last-admin availability from current projection rows. When row count is one, render remove unavailable with a visible localized reason and no confirmation affordance.
  - [ ] For row count greater than one and current freshness, render a destructive/high-impact remove launcher per row with stable selectors such as `data-testid="tenants-global-admin-remove"`.
  - [ ] Fail closed when authorization is not authorized, authorization is indeterminate, freshness is stale/unknown/degraded, read support is unavailable, command support is unavailable, the target row is missing, required preview fields are unavailable, or any command is in flight.
  - [ ] Build a Consequence Preview with platform authority scope (`system` / `global-administrators` / `global-administrators`), target user id, current admin count, last-admin impact, access being revoked, projection freshness, recovery path, audit expectation, known consequences, and known unknowns.
  - [ ] Submission must remain blocked if any preview item cannot be shown honestly.
  - [ ] Preserve last-confirmed administrator rows while removal is pending, rejected, failed, degraded, or unable to verify; never optimistically remove the target row.

- [ ] Implement projection-confirmed lifecycle behavior (AC: 3, 4, 5, 6, 7)
  - [ ] Track lifecycle states distinctly: previewed, submitted, accepted, projection pending, confirmed, rejected, failed, degraded, unable to verify, audit pending, audit delayed, audit unavailable, and missing support.
  - [ ] After command acceptance or status completion, re-query `GetGlobalAdministratorsAsync` against the fixed projection and confirm only when the target `UserId` is absent from returned rows.
  - [ ] Treat status completion without projection evidence as projection pending, degraded, or unable to verify. Never show Success from command status alone.
  - [ ] Treat SignalR or notification callbacks as re-query nudges only; they must never advance lifecycle to confirmed.
  - [ ] If the backend returns `LastGlobalAdministrator`, keep the target row visible and show a hard-blocked rejected state.
  - [ ] If the backend returns `GlobalAdministratorNotFound`, keep last-confirmed projection rows and show rejected copy; do not infer that removal is already applied unless the projection re-query proves absence.
  - [ ] Keep audit copy honest: audit pending, delayed, unavailable, and missing support are distinct states and do not fabricate proof.

- [ ] Update resources, styling, focus, and support-safety evidence (AC: 1, 2, 4, 5, 6, 8)
  - [ ] Add EN/FR `Tenants.GlobalAdministrators.Remove.*` resource keys for labels, validation, unavailable reasons, preview items, lifecycle states, hard-blocked last-admin copy, not-found copy, audit states, and recovery copy.
  - [ ] Use whole localized strings, not assembled fragments.
  - [ ] Add visible text plus icon/shape/state semantics for every lifecycle, preview, and unavailable state. Do not rely on color alone.
  - [ ] Bind live-region politeness to state semantics, not badge color or MessageBar intent: assertive for rejected, failed, degraded, unable-to-verify, missing-support, or destructive-block states; polite or non-live for routine pending/current states.
  - [ ] Destructive confirmation must support keyboard complete-or-exit: focus trap while open, Escape/cancel as safe non-committing exit, and focus return to the launching row/control on close, cancellation, rejection, failure, or confirmation.
  - [ ] Preserve forced-colors, focus-visible, responsive wrapping, and horizontal-overflow safety in `GlobalAdministratorsPage.razor.css` or a focused global-admin command CSS file.
  - [ ] Do not render or copy raw payloads, bearer tokens, decoded JWTs, claims, stack traces, cursors, ETags, aggregate metadata, message ids, internal correlation ids, raw problem details, or production tenant/user data.

- [ ] Add focused backend/UI regression tests (AC: 1-8)
  - [ ] Gateway tests prove `RemoveGlobalAdministratorAsync` submits `tenant = "system"`, `domain = "global-administrators"`, `aggregateId = "global-administrators"`, `commandType = "RemoveGlobalAdministrator"`, a ULID `messageId`, and payload `UserId` only.
  - [ ] Gateway tests cover blank user id validation, `LastGlobalAdministratorRejection`, `GlobalAdministratorNotFoundRejection`, `InsufficientPermissionsRejection`, gateway unavailable/failure mapping, support-unsafe detail redaction, and shared status mapping for global-admin rejections.
  - [ ] Component tests cover authorized remove availability, last-admin pre-submit unavailability, stale/degraded/read-unavailable fail-closed behavior, one-at-a-time locking, full preview blocking when any required item is unavailable, no optimistic row removal, projection-confirmed removal, race-returned last-admin rejection, not-found rejection, failed/degraded/unable-to-verify states, focus return, live-region politeness, stable selectors, and no tenant/member substitute markers.
  - [ ] Resource tests cover EN/FR parity for every new `Tenants.GlobalAdministrators.Remove.*` key.
  - [ ] Static/CSS or component tests cover forced-colors hooks, visible focus, responsive safety, and support-safe rendered text.
  - [ ] Re-run focused validation: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`.
  - [ ] Run per-project tests, using the xUnit v3 executable fallback if the known .NET 10 Microsoft.Testing.Platform/VSTest issue appears.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 4 Story 4.4. Epic 4 covers global administrator governance and requires global administrator authority to stay separate from tenant membership. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.4: Remove Global Administrator with Last-Admin Hard Stop`]
- FR19 covers grant/remove global-administrator operations in the fixed `global-administrators` scope. Story 4.4 is the remove slice and owns the last-global-administrator hard stop. [Source: `_bmad-output/planning-artifacts/epics.md#FR19`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-19: Grant or remove a global administrator`]
- The last global administrator case is asymmetric with tenant last-owner removal. Tenant last-owner flows can raise friction; global last-admin removal is unavailable/hard-blocked and must not become a completable destructive confirmation. [Source: `_bmad-output/planning-artifacts/epics.md#Story 4.4: Remove Global Administrator with Last-Admin Hard Stop`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#State Patterns`]
- The active sprint file had `4-4-remove-global-administrator-with-last-admin-hard-stop: backlog` before this create-story run and Epic 4 was already `in-progress`. [Source: `_bmad-output/implementation-artifacts/sprint-status.yaml`]

### Readiness Conflict To Preserve

- There is an explicit planning conflict. The sprint asks for Story 4.4, but the current fallback/readiness records still state that FR19 global-administrator grant/remove remains categorically blocked unless a separate governance decision clears it. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#Scope of this approval`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-06.md#Recommended Approach`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Readiness and Gates`]
- This story file is generated under the user's `#YOLO` instruction. If the dev workflow treats the still-blocked FR19 record as authoritative, the correct source implementation behavior is to stop and record the blocker rather than implement an unauthorized platform-governance command.
- Story 4.3 was already returned to backlog after its dev-story readiness check found the same FR19 clearance gap. Treat that as actionable prior-story intelligence, not as noise. [Source: `_bmad-output/implementation-artifacts/4-3-grant-global-administrator-with-projection-confirmation.md#Debug Log References`]

### Confirmed Backend Facts

- `RemoveGlobalAdministrator` already exists as a plain public command record with one field: `UserId`. It must remain a command record without `sealed` or XML docs. [Source: `src/Hexalith.Tenants.Contracts/Commands/RemoveGlobalAdministrator.cs`; `_bmad-output/project-context.md#C# Language & Contract Rules`]
- `GlobalAdministratorsAggregate.Handle(RemoveGlobalAdministrator, state, envelope)` requires the actor user id in the command envelope to already be present in `GlobalAdministratorsState.Administrators`. It returns `GlobalAdministratorNotFoundRejection` when no state exists or the target is absent, `InsufficientPermissionsRejection` for unauthorized actors, `LastGlobalAdministratorRejection` when only one admin remains, and `GlobalAdministratorRemoved` for success. [Source: `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs`]
- The global administrator identity is fixed: tenant `"system"`, domain `"global-administrators"`, aggregate id `"global-administrators"`. Global-admin commands do not include a `TenantId` payload field; success events carry tenant `"system"`. [Source: `docs/event-contract-reference.md#GlobalAdministratorsAggregate`; `src/Hexalith.Tenants.Contracts/Identity/TenantIdentity.cs`]
- `GlobalAdministratorRemoved` removes the target from the read model, and `GetGlobalAdministratorsQuery` / `GET /api/global-administrators` provide the fixed projection read path implemented by Story 4.2. [Source: `docs/event-contract-reference.md#RemoveGlobalAdministrator`; `_bmad-output/implementation-artifacts/4-2-review-global-administrators-from-fixed-aggregate.md#Completion Notes List`]
- Integration and aggregate tests already cover backend `RemoveGlobalAdministrator` success, not-found, unauthorized, and last-admin rejection behavior. Story 4.4 should not change aggregate semantics unless a failing source-level integration proves a direct bug. [Source: `tests/Hexalith.Tenants.Server.Tests/Aggregates/GlobalAdministratorsAggregateTests.cs`; `tests/Hexalith.Tenants.Testing.Tests/Conformance/TenantConformanceTests.cs`]

### Existing Implementation To Update

- `GlobalAdministratorsPage.razor` currently renders the Story 4.2 review surface and read-only grant/remove unavailable reasons. It injects `ITenantQueryGateway`, not `ITenantCommandGateway`. Story 4.4 should replace the remove placeholder with a real remove flow only if FR19 is cleared, while preserving review rows, paging, refresh, fixed-scope facts, and fail-closed authorization behavior. [Source: `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`]
- `GlobalAdministratorsSnapshot` stores rows, cursor, ETag, freshness, authorization-scoped empty state, and reason. It has no command lifecycle data today. Add a focused remove command snapshot/model rather than overloading read freshness state. [Source: `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsSnapshot.cs`]
- `TenantQueryGateway.GetGlobalAdministratorsAsync` already submits the fixed global-admin query and preserves previous rows for stale/degraded/not-modified states. Reuse that path for projection confirmation after remove. [Source: `_bmad-output/implementation-artifacts/4-2-review-global-administrators-from-fixed-aggregate.md#Completion Notes List`]
- `TenantCommandGateway` currently submits tenant-domain commands only and has no global-administrator command path. Story 4.4 must add a separate fixed-scope global-admin path with `domain = "global-administrators"` and `aggregateId = "global-administrators"`. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`]
- `ITenantCommandGateway` and `UnavailableTenantCommandGateway` currently have no global-admin remove method. Update both consistently, and keep the unavailable gateway support-safe. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`]
- Current EN/FR resources contain `Tenants.GlobalAdministrators.*` review keys but no remove-flow key family. Add remove resources with parity. [Source: `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`; `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`]

### Architecture And UX Guardrails

- The BFF gateways are the only backend egress. Do not read DAPR state stores, events, projections, tenant detail rows, user lookup rows, or member tables directly to infer global administrator data. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`; `_bmad-output/project-context.md#DAPR, Eventing & Consumer Rules`]
- Command dispatch uses `POST /api/v1/commands` with a client-generated ULID `messageId`, status polling by `correlationId`, SignalR as a nudge, then authoritative projection re-query. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- One-at-a-time command policy is the approved interim policy: no concurrent remove/grant/bulk command submission and no toast batching in v1. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#3. FC-CNC one-at-a-time command policy`]
- Truth-state states must not collapse: accepted, projection pending, confirmed, rejected, failed, degraded, unable to verify, audit pending/delayed/unavailable, and missing support need distinct user-facing states. [Source: `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#State Patterns`]
- SignalR is a freshness nudge only. It must not turn in-flight remove intent into confirmed platform authority removal. [Source: `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Foundation`]
- Authorization is server-enforced. UI reflection controls disclosure and availability only, and must fail closed when indeterminate. [Source: `_bmad-output/project-context.md#Host Composition & Framework Rules`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#NFR-2 Security & authorization`]

### Previous Story Intelligence

- Story 4.2 implemented the fixed global-administrator read contract and page. Continue that fixed-scope read path; do not fall back to tenant membership, user membership lookup, claims, or tenant detail endpoints. [Source: `_bmad-output/implementation-artifacts/4-2-review-global-administrators-from-fixed-aggregate.md#Completion Notes List`]
- Story 4.2 tests verified that grant/remove actions are currently read-only placeholders and that last-confirmed rows remain visible on stale/degraded refresh states. Story 4.4 must preserve confirmed rows while remove is pending and only remove the target after projection confirmation. [Source: `_bmad-output/implementation-artifacts/4-2-review-global-administrators-from-fixed-aggregate.md#Senior Developer Review (AI)`]
- Story 4.3 context created a grant implementation plan but the dev-story readiness check halted source implementation because FR19 remained blocked. Do not assume grant command support exists. Story 4.4 may add remove-specific command plumbing only after governance clearance. [Source: `_bmad-output/implementation-artifacts/4-3-grant-global-administrator-with-projection-confirmation.md#Completion Notes List`]
- Stories 3.2 through 3.4 established reusable command-flow expectations: one-at-a-time locking, no optimistic success, projection confirmation, EN/FR parity, focus return, live-region politeness, forced-colors CSS, support-safe rejection mapping, and xUnit v3 executable fallback when `dotnet test` hits the .NET 10 runner issue. [Source: `_bmad-output/implementation-artifacts/3-2-disable-or-enable-tenant-with-high-impact-confirmation.md`; `_bmad-output/implementation-artifacts/3-3-set-tenant-configuration-key-value-with-consequence-preview.md`; `_bmad-output/implementation-artifacts/3-4-remove-tenant-configuration-key-with-consequence-preview.md`]

### Git Intelligence

- Recent commits are story-scoped Conventional Commits: `docs(story-4.3): record blocked global administrator grant story`, `feat(story-4.2): Review Global Administrators from Fixed Aggregate`, and `feat(story-4.1): Global Administrators Navigation and Read Contract Readiness`. A compatible implementation commit, if FR19 is cleared, would be `feat(story-4.4): Remove Global Administrator with Last-Admin Hard Stop`. [Source: `git log --oneline -5`]
- Current dirty work before creating this story included only `_bmad-output/story-automator/orchestration-1-20260605-153745.md`. It is unrelated to the story file and sprint-status update; do not revert it. [Source: `git status --short`]

### Latest Technical Information

- Use the repo-pinned local stack: .NET 10, Blazor InteractiveServer, EventStore gateway/query/command contracts, FrontComposer shell, Fluent UI Blazor, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not add package versions or new dependencies. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `Directory.Packages.props`]
- External package/API research is not required for Story 4.4 because the implementation uses existing repo-pinned APIs and local contracts. The risk is governance/scope confusion, last-admin safety, and false confirmation, not third-party API drift.

### Project Structure Notes

- Expected source changes, if cleared: `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor(.css)` or `src/Hexalith.Tenants.UI/Components/GlobalAdministrators/`, `src/Hexalith.Tenants.UI/State/GlobalAdministrators/`, `src/Hexalith.Tenants.UI/State/TenantCommands/`, `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`, `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`, `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`, resources, and UI tests.
- Do not modify `GlobalAdministratorsAggregate`, `RemoveGlobalAdministrator`, `GlobalAdministratorRemoved`, rejection contracts, projection handlers, query contracts, controller read route, EventStore server registrations, AppHost/Aspire plumbing, package metadata, shared FrontComposer code, or submodules unless a compile-time integration break proves a direct requirement.
- Keep command-flow abstractions local unless there is an already-established shared FrontComposer helper. Do not add generic command lifecycle infrastructure to Tenants if it belongs in `Hexalith.FrontComposer`.
- Detected conflict: FR19 global-administrator governance changes remain blocked in current planning artifacts. Story 4.4 implementation must either cite a newer governance clearance or stop.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 4.4: Remove Global Administrator with Last-Admin Hard Stop`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 4: Global Administrator Governance`
- PRD/UX/readiness: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-19: Grant or remove a global administrator`; `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-06.md`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`
- Architecture/specs: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Frontend Architecture`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`; `docs/event-contract-reference.md#RemoveGlobalAdministrator`
- Backend code: `src/Hexalith.Tenants.Contracts/Commands/RemoveGlobalAdministrator.cs`; `src/Hexalith.Tenants.Contracts/Events/GlobalAdministratorRemoved.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/LastGlobalAdministratorRejection.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/GlobalAdministratorNotFoundRejection.cs`; `src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs`
- UI code/tests: `src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor`; `src/Hexalith.Tenants.UI/State/GlobalAdministrators/GlobalAdministratorsSnapshot.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, architecture, PRD/UX/readiness records, fallback/sprint-change records, Story 4.2 and 4.3 previous-story intelligence, current global-administrator read UI/query files, current command gateway/status files, backend global-admin command/aggregate contracts, focused tests, and recent git history.
- Checklist validation was run against `.agents/skills/bmad-create-story/checklist.md`; corrections from validation are reflected in explicit FR19 readiness-conflict handling, remove-only scope, fixed-domain command routing, last-admin hard-stop handling, no tenant-membership conflation, rejection-not-NoOp behavior, projection-confirmed success, one-at-a-time locking, support-safety rules, and story-specific accessibility/localization/test evidence tasks.
- Dev-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Dev-story readiness check found no separate governance clearance for FR19 after 2026-06-06. `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-06.md` still says FR19 global-administrator grant/remove remains categorically blocked unless a separate governance decision clears it, `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md` says FR19 remains categorically blocked with no fallback, and `docs/tenants-ui-phase-2-story-backlog.md` still marks `ui-15-global-admin-command-management` blocked/deferred.
- Source implementation halted before code changes and Story 4.4 was returned to backlog tracking.
- Validation: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Validation: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore` hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.
- Validation fallback: `./tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed 470/470 tests with 0 failed and 0 skipped.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 4.4 to removing global administrator authority through `RemoveGlobalAdministrator` with last-admin hard stop and projection confirmation.
- Story context identifies the main implementation risks: FR19 governance remains blocked in current planning records, accidentally routing through the tenant domain, treating `LastGlobalAdministrator` as completable friction, optimistic row removal, leaking hidden administrator data, tenant-command-specific status copy, and confirming from SignalR or command status alone.
- Story context does not implement source changes and does not assume Story 4.3 grant support exists.
- Dev-story readiness conflict resolved by treating the existing FR19 blocked record as authoritative. No remove command gateway, UI flow, resources, styling, or regression tests were implemented.
- Story returned to backlog pending a separate governance clearance for FR19 global-administrator grant/remove command management.
- Focused UI test-project build passed, and xUnit v3 executable fallback passed all 470 UI tests after `dotnet test` hit the known .NET 10 runner incompatibility.

### File List

- `_bmad-output/implementation-artifacts/4-4-remove-global-administrator-with-last-admin-hard-stop.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-06-06T15:17:52+02:00 - Created Story 4.4 context and marked it ready for development.
- 2026-06-06T15:22:58+02:00 - Dev-story readiness check found FR19 still categorically blocked; halted source implementation and returned Story 4.4 to backlog.
- 2026-06-06T15:22:58+02:00 - Ran focused UI build and xUnit v3 executable validation; all executable UI tests passed.
