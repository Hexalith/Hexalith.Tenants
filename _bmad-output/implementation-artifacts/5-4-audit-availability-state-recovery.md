---
created: 2026-06-06T17:36:15+02:00
baseline_commit: a5ca6e3f548e89b28a37826be721d9ef9f7cd51a
---

# Story 5.4: Audit Availability State Recovery

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 5.4. -->

## Story

As an authorized user,
I want audit pending, delayed, unavailable, and missing-support states to be explicit,
so that I know whether to wait, retry, continue read-only, inspect audit, or escalate.

## Acceptance Criteria

1. Given command or audit evidence is not immediately available, when the UI evaluates the evidence state, then it distinguishes `audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support`, and none of those states is shown as Success.
2. Given audit is pending or delayed, when the user sees the evidence state, then the UI provides appropriate wait, retry, or inspect-audit actions based on the state, and live-region announcements remain polite unless the state blocks, fails, or becomes unable to verify.
3. Given audit is unavailable or implementation support is missing, when the state renders, then the UI offers continue-read-only or escalate paths with localized support-safe copy, and raw diagnostics, stack traces, internal correlation ids, payloads, tokens, or PII are not exposed.
4. Given evidence state changes after refresh, command lifecycle update, or projection re-query, when the state transitions, then audit availability, command acceptance, and projection confirmation remain separate tokens in state and reducers, and in-flight intent never overwrites last-confirmed projection or receipt data.
5. Given an audit availability control appears in list, detail, command lifecycle, receipt, or correction surfaces, when keyboard or screen-reader users operate it, then the control has visible text, icon, accessible label, focus behavior, forced-colors-safe status, and stable selectors such as `data-testid="tenants-audit-availability"`, and recovery verbs use the canonical vocabulary and casing.
6. Given this story is complete, when verification is run, then unit/component tests cover all audit availability tokens, state transitions, recovery verb mapping, support-safe unavailable copy, non-collapse with command lifecycle states, and selector stability.
7. Given accessibility or E2E verification is run, then keyboard recovery actions, focus return, live-region politeness, forced-colors status rendering, and no false Success are verified.

## Tasks / Subtasks

- [x] Add a shared Tenants-owned audit availability model and recovery mapping (AC: 1, 2, 3, 4, 6)
  - [x] Add a focused model under `src/Hexalith.Tenants.UI/State/TenantAudit/`, for example `TenantAuditAvailability`, that maps existing `TenantCommandAuditState` values to the four user-facing availability states without stringly typed state tokens.
  - [x] Preserve the existing command state source: `TenantCommandAuditState.NotStarted`, `AuditPending`, `AuditDelayed`, `AuditUnavailable`, and `MissingSupport` live in `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`; do not create a parallel command lifecycle enum.
  - [x] Model the recovery verb separately from the displayed state: wait, refresh/retry, inspect audit, continue read-only, and escalate. Do not label any path as `undo`, `rollback`, or hidden edit.
  - [x] Keep `accepted`, `projection pending`/`confirmed`, and `audit available`/availability states as separate fields in snapshots and derived view models. Do not derive audit success from `Accepted`, `Confirmed`, SignalR, or a status-poll terminal result alone.
  - [x] Treat `audit available` as separate from this story's four incomplete/unavailable states. This story makes unavailable/recovery states explicit; it does not invent proof.

- [x] Add a reusable audit availability control (AC: 1, 2, 3, 5, 7)
  - [x] Add a local component such as `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor` with CSS, or an equivalent Tenants-owned audit component. Keep it local to Tenants UI; do not add generic FrontComposer status, timeline, or recovery scaffolding.
  - [x] Render visible state text, a non-color-only icon/shape, accessible label, recovery action buttons/links, and `data-testid="tenants-audit-availability"`.
  - [x] Use polite live-region behavior for pending/delayed informational states. Use assertive only for blocking, failure, degraded, unable-to-verify, unavailable, missing-support, or unsafe states.
  - [x] Include focus-visible, forced-colors, reduced-motion-safe, and responsive CSS hooks. Preserve stable dimensions so labels/actions do not shift layout in dense command panels or receipts.
  - [x] Keep action callbacks explicit: wait can be non-submitting/passive, retry/refresh invokes the existing refresh/status lookup path, inspect audit opens the existing audit entry point, continue-read-only closes or returns to the projection surface, and escalate exposes only a support-safe reference.

- [x] Reuse the control from audit receipts and command audit handoffs (AC: 1, 2, 3, 4, 5)
  - [x] Refactor `AuditEvidenceReceipt.razor` to use the shared availability control for `Pending`, `Delayed`, `Unavailable`, and `MissingSupport` states while preserving its existing receipt fields, safe-copy behavior, selectors, and ready/partial/stale/degraded/unauthorized/invalid-reference behavior.
  - [x] Keep `TenantAuditReceipt.FromRow` and `TenantAuditReceipt.FromEntry` receipt derivation from `TenantAuditRow`/`TenantAuditEntry`; do not add a backend receipt endpoint or bypass the existing support-safe allow-list.
  - [x] Replace repeated flow-local audit paragraphs where practical with the shared control in existing command surfaces: create tenant, add member, change role, remove member, lifecycle enable/disable, set configuration, remove configuration, and edit metadata.
  - [x] Preserve each flow's existing `AuditEvidenceEntryPoint` behavior and query parameters (`targetUserId`, `supportSafeCommandReference`, `returnUrl`, `returnFocus`, `source`). The inspect-audit action should use the existing scoped path, not a new route.
  - [x] Do not change command submission, projection confirmation, command status polling, one-at-a-time locking, consequence previews, or receipt field derivation unless required to pass through the shared availability view model.

- [x] Normalize localized copy and canonical state wording (AC: 1, 2, 3, 5, 6)
  - [x] Add Tenants-owned EN/FR resource keys for shared availability states, recovery verbs, accessible names, live-region text, unavailable reasons, and escalation text, for example under `Tenants.Audit.Availability.*`.
  - [x] Keep whole localized strings with named placeholders. Do not assemble visible sentences from fragments and do not leak machine tokens such as `AuditPending`, `audit_pending`, raw `SourceKind`, or enum names into visible copy or accessible labels.
  - [x] Preserve existing flow-specific resource keys as compatibility wrappers only where needed by tests or layout; prefer the shared copy source for new UI.
  - [x] Ensure support-safe unavailable and escalation copy never includes raw diagnostics, raw EventStore metadata, protected cursors, ETags, stack traces, tokens, internal correlation ids, MessageIds, serialized payloads, or PII.

- [x] Preserve audit page and receipt boundaries (AC: 3, 4, 5)
  - [x] Keep `GET /api/tenants/{tenantId}/audit` through `ITenantQueryGateway.GetTenantAuditAsync` as the only audit data source. Browser components must not call backend routes directly or store backend tokens.
  - [x] Keep `TenantAuditPage` cursor, filter, ETag, invalid-cursor, return-context, and loaded-row receipt-selection behavior intact.
  - [x] If `?receiptReference=` is requested and the row is not in the current tenant-scoped audit result, keep the honest invalid-reference/unavailable state. Do not query a separate source to fabricate a receipt.
  - [x] Treat SignalR as a freshness nudge only: it may trigger refresh/re-query, never move audit evidence to available or proven by itself.

- [x] Add focused tests and validation (AC: 1-7)
  - [x] Add state/model tests for every `TenantCommandAuditState` to availability-state/recovery-verb mapping, including `NotStarted` handling and no false Success.
  - [x] Add component tests for the shared availability control covering text+icon rendering, accessible labels, `tenants-audit-availability`, recovery action callbacks, live-region politeness, forced-colors/focus CSS hooks, and support-safe escalation copy.
  - [x] Update existing `AuditEvidenceReceiptTests` and `TenantAuditReceiptTests` so pending/delayed/unavailable/missing-support receipt states use the shared mapping while preserving safe-copy and field-derivation behavior.
  - [x] Update representative command-flow tests for at least one membership flow, one lifecycle flow, and one configuration/metadata flow to prove command acceptance/projection confirmation/audit availability remain non-collapsed.
  - [x] Add EN/FR resource parity tests for every new `Tenants.Audit.Availability.*` key.
  - [x] Add or update static guard tests for stable selectors, no raw state literal leakage in rendered copy, no browser backend calls, no storage use, and no raw payload/diagnostic text.
  - [x] Run focused validation first: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`.
  - [x] If `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest issue, run the xUnit v3 executable fallback for the UI test assembly.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 5 Story 5.4. Epic 5 covers audit evidence and forward recovery; this story makes incomplete audit proof states explicit across audit and command surfaces. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.4: Audit Availability State Recovery`]
- FR23 requires users to distinguish `audit pending`, `audit delayed`, `audit unavailable`, and `missing implementation support`; none is Success, and each maps to wait, retry, continue-read-only, inspect-audit, or escalate. [Source: `_bmad-output/planning-artifacts/epics.md#FR23`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#4. Delayed/Unavailable Audit-Proof States and the No-False-Success Rule`]
- The shared truth-state contract requires command acceptance, projection confirmation, and audit proof to stay distinct. SignalR is only a freshness nudge. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md#5. Layered Feedback State Set (AC4)`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- Story 5.5 owns starting forward correction from audit evidence, and Story 5.6 owns correction preview/confirmation with linked proof. Do not implement correction command dispatch, correction previews, or original/corrective record linking here. [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.5: Start Forward Correction from Audit Evidence`; `_bmad-output/planning-artifacts/epics.md#Story 5.6: Preview and Confirm Correction with Linked Proof`]

### Existing Implementation To Extend

- `TenantCommandAuditState` already exists in `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`, and command snapshots across create/add/change/remove/lifecycle/configuration/metadata flows already carry `AuditState`. Extend this rather than introducing a second lifecycle source. [Source: `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`]
- Current command flows render flow-local audit copy through resource prefixes such as `Tenants.Create.Audit.*`, `Tenants.AddMember.Audit.*`, `Tenants.ChangeRole.Audit.*`, `Tenants.RemoveMember.Audit.*`, `Tenants.Lifecycle.Audit.*`, `Tenants.Configuration.Set.Audit.*`, `Tenants.Configuration.Remove.Audit.*`, and `Tenants.EditMetadata.Audit.*`. This story should consolidate the shared availability UI without breaking flow-specific context. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/*/*Flow.razor`; `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`]
- `AuditEvidenceReceipt` already renders receipt states and recovery actions; it currently owns its own action mapping and selector set (`tenants-audit-receipt`, `tenants-audit-receipt-state`, `tenants-audit-receipt-copy`, `tenants-audit-receipt-reference`). Preserve those selectors and safe-copy behavior while extracting shared availability state rendering. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor`]
- `TenantAuditReceipt` maps `TenantCommandAuditState.AuditPending`, `AuditDelayed`, `AuditUnavailable`, and `MissingSupport` to receipt states. Reuse this mapping intent, but avoid making receipt state the only place where audit availability rules live. [Source: `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs`]
- `TenantAuditPage` owns tenant-scoped loading, filters, cursor paging, ETag reuse, invalid-cursor handling, context banners, safe return URLs, loaded-row receipt selection, and UTC parsing/rendering. Keep those behaviors intact. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`]
- `AuditEvidenceEntryPoint` already builds scoped audit links with tenant, target user, support-safe command reference, source, return URL, and return focus. The shared availability control should use or wrap this path for inspect-audit rather than constructing a competing route. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceEntryPoint.razor`]

### Backend And Data Boundary

- The consume-only backend surface is fixed: `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, `GET /api/tenants/{tenantId}/audit`, `POST /api/v1/commands`, and `GET /api/v1/commands/status/{correlationId}`. Do not add backend audit availability, receipt, consequence, preview, command-specific evidence, correction, or escalation endpoints. [Source: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#8. Backend and Data Boundaries`]
- Backend egress stays server-side through BFF gateway services. Browser-side components must not call backend routes directly and must not store backend tokens. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]
- Support-safe references must never expose raw payloads, bearer tokens, decoded JWT contents, stack traces, internal correlation IDs, raw EventStore metadata, protected cursors, ETags, MessageIds, or PII. [Source: `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#7.1 Support-safe references`; `_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- Tenant ids and user ids are meaningful caller-supplied strings. Preserve literal values and URI-escape only for navigation; never parse them as GUIDs or ULIDs. [Source: `_bmad-output/project-context.md#Identity Rules`]

### UX, Accessibility, And Localization Guardrails

- Reserve Success for projection-proven truth or audit-available evidence only. Pending, delayed, unavailable, missing-support, stale, degraded, rejected, unable-to-verify, and unsupported states are not Success. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR3`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#5.2 Non-collapse invariant`]
- Recovery verbs must use canonical wording and casing: wait, retry/status lookup, inspect audit, continue read-only, escalate, start correction, and restore intended access where later stories need them. This story should use only the recovery paths it owns and must not use `undo`, `rollback`, or `hidden edit`. [Source: `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#5. Compensating-Recovery Language and Flow`]
- Use Tenants-owned `.resx` resources, whole strings with named placeholders, and EN/FR parity. Do not leak enum names or machine tokens into visible copy or accessible names. [Source: `_bmad-output/planning-artifacts/architecture.md#Localization keys`; `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#4. Localization and Message Composition Requirements`]
- Every state must include visible text plus icon/shape, accessible label, visible focus, forced-colors support, and stable selectors. Color alone is not sufficient. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md#2.3 Presentation requirements (every state)`; `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#5. Reduced Motion and Visual Accessibility Requirements`]
- Absolute timestamps remain required where timestamps are shown. Preserve the Story 5.1/5.3 UTC timestamp behavior and do not use server-local `ToLocalTime()` or relative-only labels. [Source: `_bmad-output/planning-artifacts/epics.md#UX-DR29`; `_bmad-output/implementation-artifacts/5-3-support-safe-audit-evidence-receipt.md#Previous Story Intelligence`]

### Previous Story Intelligence

- Story 5.3 added `TenantAuditReceipt`, `AuditEvidenceReceipt`, receipt safe-copy behavior, loaded-row receipt opening from `AuditDataGrid`, and query-param receipt handling from `TenantAuditPage`. Extend these rather than replacing them. [Source: `_bmad-output/implementation-artifacts/5-3-support-safe-audit-evidence-receipt.md#Completion Notes List`]
- Story 5.3 review observed that command-audit states were model/component-tested but `TenantAuditPage` passes `TenantCommandAuditState.NotStarted` because the read-only audit query carries no command-audit state. Story 5.4 should not force command state into the audit query; it should expose a reusable availability control that command surfaces can pass their existing audit state into. [Source: `_bmad-output/implementation-artifacts/5-3-support-safe-audit-evidence-receipt.md#Senior Developer Review (AI)`]
- Story 5.2 fixed raw source-kind token leakage by mapping machine source tokens to localized whole phrases. Apply the same rule to audit availability state and recovery action labels. [Source: `_bmad-output/implementation-artifacts/5-3-support-safe-audit-evidence-receipt.md#Previous Story Intelligence`]
- Story 5.1 fixed server-local timezone dependence in audit timestamp rendering and filter parsing. Preserve UTC behavior in any audit availability reference text or receipt integration. [Source: `_bmad-output/implementation-artifacts/5-3-support-safe-audit-evidence-receipt.md#Previous Story Intelligence`]
- Recent commits are story-scoped Conventional Commits: `a5ca6e3 feat(story-5.3): Support-Safe Audit Evidence Receipt`, `77bb935 feat(story-5.2): Scoped Audit Evidence Entry Points`, and `497a4ac feat(story-5.1): Tenant Audit Trail DataGrid`. A compatible implementation commit would be `feat(story-5.4): add audit availability state recovery`. [Source: `git log --oneline -5`]
- Current dirty work before creating this story included only `_bmad-output/story-automator/orchestration-1-20260605-153745.md`. It is unrelated and must not be reverted. [Source: `git status --short`]

### Latest Technical Information

- Use the repo-pinned stack and existing local APIs: .NET 10, Blazor InteractiveServer, Fluent UI Blazor, FrontComposer shell, EventStore query/command gateways, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not add packages or package versions. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `Directory.Packages.props`]
- External package research is not required for this story because implementation relies on existing repo-pinned components and local contracts. The primary risks are non-collapse, support-safety, localization, accessibility, selectors, and avoiding backend or shared FrontComposer scope creep.

### Project Structure Notes

- Expected UI additions: `src/Hexalith.Tenants.UI/State/TenantAudit/` for the shared availability model and `src/Hexalith.Tenants.UI/Components/Tenants/Audit/` for the shared availability control and CSS.
- Expected UI updates: `AuditEvidenceReceipt.razor`, `AuditEvidenceReceipt.razor.css`, existing command flows that currently render flow-local audit paragraphs, and `TenantsResources.resx` / `.fr.resx`.
- Expected tests: `tests/Hexalith.Tenants.UI.Tests/State/`, `tests/Hexalith.Tenants.UI.Tests/Components/`, existing command-flow component tests, receipt tests, resource parity tests, and static guard tests.
- Avoid backend contract/projection changes, `TenantAuditEntry` wire-shape changes, `GetTenantAuditQueryHandler`, audit projection storage, EventStore server registration, AppHost/Aspire plumbing, package metadata, shared FrontComposer code, and submodule changes unless a compile-time break proves a direct integration need.
- Do not add generic availability infrastructure, an audit timeline, grouped audit mode, correction actions, correction preview, command submission changes, or cross-domain support-safety scaffolding to Tenants.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 5.4: Audit Availability State Recovery`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 5: Audit Evidence and Forward Recovery`
- Audit/recovery spec: `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#4. Delayed/Unavailable Audit-Proof States and the No-False-Success Rule`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md#7. Support-Safe References, Accessibility, and Localization Contracts`
- Truth-state spec: `docs/tenants-ui-truth-state-and-action-availability-spec.md#5. Layered Feedback State Set (AC4)`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#8. Backend and Data Boundaries`
- Accessibility/localization spec: `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#3. Screen Reader, Status, and Live-Region Requirements`; `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#4. Localization and Message Composition Requirements`; `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md#6. UI Acceptance Evidence Matrix`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules`; `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`
- Existing code: `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`; `src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs`; `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantAuditPage.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceEntryPoint.razor`
- Existing tests: `tests/Hexalith.Tenants.UI.Tests/State/TenantAuditReceiptTests.cs`; `tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs`; command-flow component tests under `tests/Hexalith.Tenants.UI.Tests/Components/`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-06T17:53:58+02:00 - `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed.
- 2026-06-06T17:53:58+02:00 - `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build` hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.
- 2026-06-06T17:53:58+02:00 - `./tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -parallel none` passed: 570 total, 0 failed.
- 2026-06-06T17:53:58+02:00 - `dotnet build Hexalith.Tenants.slnx -c Release -m:1 --no-restore` passed.
- 2026-06-06T17:53:58+02:00 - Tier 1 xUnit executable fallback passed: Contracts 105, Client 47, Testing 181, UI 570, Sample 31; 0 failed.
- 2026-06-06T17:53:58+02:00 - Server.Tests xUnit executable fallback ran and failed 6 unrelated existing documentation/configuration expectations: missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and deployment summary missing `Story 7.6A`.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added a Tenants-owned audit availability model that maps `TenantCommandAuditState` into explicit pending, delayed, unavailable, and missing-support states with separate canonical recovery verbs.
- Added reusable `AuditAvailabilityState` UI with visible labels, icon/shape, accessible labels, polite/assertive live-region behavior, stable `tenants-audit-availability` selector, focus-visible, forced-colors, reduced-motion, and responsive CSS.
- Refactored audit receipt and create/add/change/remove/lifecycle/configuration/metadata command surfaces to reuse the shared control while preserving command lifecycle/projection/audit separation and existing `AuditEvidenceEntryPoint` scoped links.
- Added EN/FR shared availability resources and tests for mapping, component rendering, callbacks, resource parity, selector stability, support-safe copy, no false Success, and no machine-token leakage.

### File List

- _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor.css
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor
- src/Hexalith.Tenants.UI/Components/Tenants/CreateTenantFlow.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleCommandFlow.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Members/ChangeTenantMemberRoleFlow.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Members/RemoveTenantMemberFlow.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor
- src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx
- src/Hexalith.Tenants.UI/Resources/TenantsResources.resx
- src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditAvailability.cs
- src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs
- tests/Hexalith.Tenants.UI.Tests/Components/AddTenantMemberFlowTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/ChangeTenantMemberRoleFlowTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/EditTenantMetadataFlowTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantConfigurationFlowTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/SetTenantConfigurationFlowTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs
- tests/Hexalith.Tenants.UI.Tests/State/TenantAuditAvailabilityTests.cs
- tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs
- tests/test-summary.md

### Change Log

- 2026-06-06T17:36:15+02:00 - Created Story 5.4 context and marked it ready for development.
- 2026-06-06T17:53:58+02:00 - Implemented shared audit availability state recovery model/control, wired receipts and command flows, added localized EN/FR copy and focused tests, and marked story ready for review.
- 2026-06-06 - Senior Developer Review (AI) completed: Approve. Added `tests/test-summary.md` to the File List (it carried a Story 5.4 evidence addendum but was undocumented). No code defects required fixes. Status moved review → done.

## Senior Developer Review (AI)

**Reviewer:** Administrator
**Date:** 2026-06-06
**Outcome:** Approve (status → done)

### Verification performed

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` — passed.
- xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -parallel none` — **572 total, 0 failed** (the documented .NET 10 `dotnet test` MTP/VSTest issue still applies; fallback used).
- Reviewed every File List entry against `git diff`/`git status`, all 7 ACs against implementation, and all `[x]` subtasks against code/tests.

### Acceptance Criteria audit

- **AC1 (four distinct non-success states):** Met. `TenantAuditAvailability.FromCommandAuditState` maps `AuditPending/AuditDelayed/AuditUnavailable/MissingSupport` to distinct states; `NotStarted` and any "available" case render nothing; `IsAuditAvailable` is always false; component asserts no "Success" copy.
- **AC2 (pending/delayed wait/retry/inspect + polite):** Met. Pending → Wait/Refresh/InspectAudit (polite); Delayed → Refresh/InspectAudit (polite). This also corrects the Story 5.3 receipt behavior where pending/delayed were assertive.
- **AC3 (unavailable/missing-support continue-read-only/escalate + support-safe):** Met. Unavailable → ContinueReadOnly/Refresh/Escalate (assertive); MissingSupport → ContinueReadOnly/Escalate (assertive). Reason copy is localized EN/FR and contains no diagnostics/tokens/PII (guarded by composition test).
- **AC4 (separate tokens, no collapse):** Met. Audit availability is derived from the separate `TenantCommandAuditState` field; command lifecycle and projection freshness remain independent. `Projection_evidence_confirms_without_exposing_internal_correlation_id` proves a Confirmed command can still show audit pending (non-collapse).
- **AC5 (control surfaces, a11y, selectors, canonical verbs):** Met. Visible text + non-color glyph + `aria-label` + `data-testid="tenants-audit-availability"` + focus-visible/forced-colors CSS; recovery verbs use canonical wording/casing; no `undo`/`rollback`/`hidden edit`.
- **AC6 (unit/component coverage):** Met. `TenantAuditAvailabilityTests` (all tokens + verbs + NotStarted), `AuditAvailabilityStateTests`, receipt/flow tests, EN/FR parity + no-machine-token guard.
- **AC7 (a11y/E2E behaviors):** Met. Native keyboard buttons, focus-return via `OnClose`, live-region politeness per state, forced-colors/reduced-motion CSS, no false Success.

### Findings

- **[Medium → Fixed] File List incomplete.** `tests/test-summary.md` was modified (Story 5.4 evidence addendum) but omitted from the Dev Agent Record File List. Added during review.
- **[Low → Accept, by design] Command-flow `InspectAudit` entry point only renders for Pending/Delayed.** For `AuditUnavailable`/`MissingSupport` the shared control intentionally offers ContinueReadOnly/Refresh/Escalate instead of inspect-audit (nothing to inspect), so the wrapped `AuditEvidenceEntryPoint` is not shown in those two states. Consistent with the recovery-verb design and covered by passing tests; no change made.
- **[Low → Accept] Receipt nests an `aria-live` availability section inside the `aria-live` receipt region.** Politeness matches between inner/outer per state, so screen readers attribute inner-content changes to the innermost region; not a double-announcement defect. Behavior is encoded by the updated receipt tests; no change made.

### Notes

- `_bmad-output/story-automator/orchestration-1-20260605-153745.md` remains dirty as expected (pre-existing, unrelated) and was not touched, per the story's Previous Story Intelligence.
