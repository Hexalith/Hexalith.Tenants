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

- 2026-10-01 remaining verification patches — exact Debug build, final 189-test focused run, descending-only and direction-toggle mutations, maintained MTP 3,589/3,589 run, formatting limitation, and follow-up gitlink evidence are recorded in the 2026-10-01 addendum to `story-5-4-re-review-verification-2026-09-30.md` and `spec-5-4-understand-audit-availability-and-recovery-4.md`.
- 2026-09-30 re-review closure — exact Debug build, maintained MTP, mutation, browser, gitlink, and commitlint evidence is recorded in `story-5-4-re-review-verification-2026-09-30.md` and the current `spec-5-4-understand-audit-availability-and-recovery-3.md`.
- 2026-06-06T17:53:58+02:00 - `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed.
- 2026-06-06T17:53:58+02:00 - `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build` hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.
- 2026-06-06T17:53:58+02:00 - `./tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -parallel none` passed: 570 total, 0 failed.
- 2026-06-06T17:53:58+02:00 - `dotnet build Hexalith.Tenants.slnx -c Release -m:1 --no-restore` passed.
- 2026-06-06T17:53:58+02:00 - Tier 1 xUnit executable fallback passed: Contracts 105, Client 47, Testing 181, UI 570, Sample 31; 0 failed.
- 2026-06-06T17:53:58+02:00 - Server.Tests xUnit executable fallback ran and failed 6 unrelated existing documentation/configuration expectations: missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and deployment summary missing `Story 7.6A`.

### Completion Notes List

- Closed the five remaining re-review patches and all five independent-review findings. Actual ascending Tenant/Status events now cover descending and already ascending starting states, distinct pages through page three, visible row order, and a complete cursor-history reset. Successful refresh teardown starts through the Refresh click and uses renderer-driven component disposal; live finalization exceptions retain identity; ARIA counts use a bounded populated-count wait. Final Debug source build passed with zero warnings/errors, focused tests passed 189/189, and maintained MTP passed 3,589/3,589 with zero failed/skipped. Descending-only and direction-toggle mutations were rejected, with original production source restored before final validation. The pre-existing formatter brace-policy conflict is recorded separately in deferred work; prior deferrals remain. Current status is review.
- Closed the eight 2026-09-30 re-review patches with bounded disposed-host faults, live refresh/focus exception theories, deterministic dispatcher gating, isolated template safety and a copyable positive control, accented French group naming, browser group parity, and Loading group suppression. All six independent review findings were addressed, including successful disposed completion and separate finalization dispatcher faults. The final Debug source build passed with zero warnings/errors; maintained MTP passed 3,586/3,586; nine focused code/fixture mutations and the Chrome harness's independent mutations were rejected. The current follow-up gitlink guard passed with no pointer changes; the deferred legacy baseline remains unresolved. Current status is review.
- Ultimate context engine analysis completed - comprehensive developer guide created.
- Added a Tenants-owned audit availability model that maps `TenantCommandAuditState` into explicit pending, delayed, unavailable, and missing-support states with separate canonical recovery verbs.
- Added reusable `AuditAvailabilityState` UI with visible labels, icon/shape, accessible labels, polite/assertive live-region behavior, stable `tenants-audit-availability` selector, focus-visible, forced-colors, reduced-motion, and responsive CSS.
- Refactored audit receipt and create/add/change/remove/lifecycle/configuration/metadata command surfaces to reuse the shared control while preserving command lifecycle/projection/audit separation and existing `AuditEvidenceEntryPoint` scoped links.
- Added EN/FR shared availability resources and tests for mapping, component rendering, callbacks, resource parity, selector stability, support-safe copy, no false Success, and no machine-token leakage.
- Completed the 2026-09-30 post-review hardening: renderer-dispatcher refresh finalization, teardown-safe focus/refresh completion, semantic recovery groups, and direct receipt safety/outcome/recovery-route coverage. Release build passed with 0 warnings/errors, all 3,570 UI tests passed, the Chrome 154 harness and three mutation rejections passed, and the story gitlink guard reported no pointer changes.

### File List

- _bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md
- _bmad-output/implementation-artifacts/deferred-work.md
- _bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-2.md
- _bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-3.md
- _bmad-output/implementation-artifacts/spec-5-4-understand-audit-availability-and-recovery-4.md
- _bmad-output/implementation-artifacts/story-5-4-re-review-verification-2026-09-30.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor.css
- src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditEvidenceReceipt.razor
- src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor — Fluent UI 5.0.0 sort-event compatibility prerequisite.
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
- tests/Hexalith.Tenants.UI.Tests/Components/TenantListSurfaceTests.cs — Fluent UI 5.0.0 sort-state assertion compatibility prerequisite.
- tests/Hexalith.Tenants.UI.Tests/GeneratedTenantsSurfaceTests.cs — settled data-row counts plus the header; both Fluent UI RC and stable use `TotalItemCount + 1`.
- tests/Hexalith.Tenants.UI.Tests/Browser/tenants-focus-browser-validation.html
- tests/Hexalith.Tenants.UI.Tests/Components/ChangeTenantMemberRoleFlowTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/EditTenantMetadataFlowTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantConfigurationFlowTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/SetTenantConfigurationFlowTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/TenantAuditPageTests.cs
- tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs
- tests/Hexalith.Tenants.UI.Tests/State/TenantAuditAvailabilityTests.cs
- tests/Hexalith.Tenants.UI.Tests/State/TenantAuditReceiptTests.cs
- tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs
- tests/test-summary.md

### Change Log

- 2026-10-01 - Closed the five remaining re-review patches and five independent-review findings with real ascending Tenant/Status sort events, distinct page-three/reset and visible-order coverage, renderer-driven Refresh-click disposal, live finalization exception identity, and bounded ARIA-count verification. Final Debug source build passed with zero warnings/errors; 189 focused tests and maintained MTP 3,589/3,589 passed. Descending-only and direction-toggle mutations were rejected; production source was restored before the final build. Recorded the pre-existing formatter brace-policy conflict separately and moved story/sprint status to review.
- 2026-09-30 - Closed all eight re-review patches and recorded the final zero-warning/error Debug build, maintained MTP 3,586/3,586 run, nine mutation rejections, Chrome, and follow-up gitlink evidence; corrected the existing Fluent UI 5.0.0 sort API and ARIA test expectations for the populated item count (both RC and stable count data rows plus the header).
- 2026-06-06T17:36:15+02:00 - Created Story 5.4 context and marked it ready for development.
- 2026-06-06T17:53:58+02:00 - Implemented shared audit availability state recovery model/control, wired receipts and command flows, added localized EN/FR copy and focused tests, and marked story ready for review.
- 2026-06-06 - Senior Developer Review (AI) completed: Approve. Added `tests/test-summary.md` to the File List (it carried a Story 5.4 evidence addendum but was undocumented). No code defects required fixes. Status moved review → done.
- 2026-09-30 - Completed the six reopened post-review hardening patches and verified the Release UI build, 3,570-test UI suite, Chrome accessibility/responsive harness, mutation guards, diff hygiene, and story gitlink declaration.

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

### Review Findings

- [x] [Review][Patch] Finalize refresh gating and retry bookkeeping atomically on the renderer dispatcher [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:299]
- [x] [Review][Patch] Treat renderer teardown as non-fatal during refresh completion and focus hand-off [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:220]
- [x] [Review][Patch] Give both recovery-action stacks a semantic group role so their accessible names are exposed [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:51]
- [x] [Review][Patch] Repair the unsafe-summary test fixture so it reaches localized-template validation [tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs:651]
- [x] [Review][Patch] Cover unknown and category-mismatched audit outcomes through receipt derivation and downstream availability [tests/Hexalith.Tenants.UI.Tests/State/TenantAuditReceiptTests.cs:166]
- [x] [Review][Patch] Cover receipt-local recovery routing and callbacks for every non-shared receipt state [tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs:624]
- [x] [Review][Defer] Confirm that the first mounted availability state is announced by assistive technology [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:7] — deferred: unverified and medium if true; an NVDA/VoiceOver browser check of `NotStarted` to `Pending` or `Unavailable` is needed to determine whether inserting the live region with its first content is announced.
- [x] [Review][Defer] Decide whether `TenantAuditReceipt.FromEntry` should accept authoritative lifecycle/provenance or be removed [src/Hexalith.Tenants.UI/State/TenantAudit/TenantAuditReceipt.cs:40] — deferred: pre-existing to the current Story 5.4 refinement; `TenantAuditRow.FromEntry` defaults lifecycle and provenance to `Unknown`, so this factory cannot produce `Ready`, and resolving that requires an API/authority decision.

#### Rejected

- [Rejected][low] Retry budget is keyed only to availability state — the approved contract defines the bound by unchanged state; carrying it across a rare same-state hand-off is low impact, while an evidence identity would add API/remount complexity.
- [Rejected][low] Refresh has no separate busy announcement — the approved interaction ignores merged clicks while the host refresh owns progress; adding a second progress state is not warranted for this low-impact case.
- [Rejected][false] Close can render without a callback — every production `AuditEvidenceReceipt` call site supplies `OnClose`; only isolated test renders omit it.
- [Rejected][false] Start correction can render without a callback — the only production call site that supplies a correction intent also supplies `OnStartCorrection`; the other receipt host supplies no intent.
- [Rejected][false] Receipt heading IDs collide — each production surface renders at most one receipt instance, and the two hosts are on distinct pages/flows.
- [Rejected][false] Full receipt copy omits a proven command reference — no production receipt currently carries a non-null authoritative command reference; the former caller-controlled hint was intentionally removed as uncorrelated evidence.
- [Rejected][false] Non-zero timestamp offsets are valid Ready evidence — the current receipt proof contract deliberately requires UTC input and has a focused test pinning non-UTC offsets to `Partial`.
- [Rejected][false] Culture-specific calendars corrupt UTC labels — the application supports only `en` and `fr`, both Gregorian for this fixed format.
- [Rejected][false] Receipt scope can disagree with tenant identity — the sole production row mapper derives both fields from the same authorized `TenantId`.
- [Rejected][false] Receipt target can disagree with typed narrative — the sole production row mapper derives the target directly from that sanitized narrative and no production caller constructs rows independently.
- [Rejected][false] Removed receipt members break consumers — the UI project is not a published package, and all in-repository callers compile against the current surface.
- [Rejected][low] A new same-state receipt inherits the retry count — duplicate of the state-scoped retry-budget concern; the rare impact does not justify adding identity state to the shared component.
- [Rejected][low] A never-completing host refresh strands the control — production refresh paths own their network/deadline policy; imposing a second generic timeout would add conflicting cancellation semantics.
- [Rejected][false] Close without a delegate is an enabled no-op — duplicate; all production hosts provide the delegate.
- [Rejected][false] Correction without a delegate is an enabled no-op — duplicate; the production correction host always provides the delegate.
- [Rejected][false] Empty, filtered-empty, or unknown surfaces can certify a row — valid production snapshots cannot pair an empty surface with a row, and unknown enum values are not externally deserialized here.
- [Rejected][false] An unknown command-audit enum becomes Ready — the enum is internal typed state, not untrusted input, and no current path can create an unknown value.
- [Rejected][false] A mismatched narrative target can be Ready — duplicate; production mapping derives both together.
- [Rejected][false] Receipt rendering loses target-type safety — production receipts receive a target already sanitized under its typed narrative policy before the component's defensive display check.
- [Rejected][false] Correction reasons can expose raw localization keys — every current enum member has EN/FR resources and unknown enum values cannot arise from the typed correction evaluator.
- [Rejected][false] Support-safe command references were improperly dropped — the current approved receipt contract forbids placing a caller-supplied command hint into proof without an authoritative row-to-command link.
- [Rejected][false] Story 5.4 implements correction start — Git attribution shows the correction block was introduced by the distinct `feat(story-5.5)` commit `eeb0a49d`, not Story 5.4.
- [Rejected][false] Pending and delayed must render a Wait button — the approved Story 5.4 refinement explicitly removed the no-op Wait action and conveys waiting through state/explanation text.
- [Rejected][medium] The story's recorded `baseline_commit` is invalid — `validate-story-gitlinks.py` fails because `a5ca6e3f…` is not a commit; the unambiguous parent is `a5ca6e38…`, but the workflow rejects findings whose only fix edits the spec under review.

### Review Findings (2026-09-30 re-review of `d728ff46..82b13514`)

Scope: the post-review hardening commit `82b13514` against `spec-5-4-understand-audit-availability-and-recovery-2.md`. Layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor; none failed. Baseline evidence on a clean tree at `82b13514`: a `--no-incremental` Release UI-test build had 0 warnings and 0 errors, and the suite passed 3,570/3,570. Mutation evidence, with each mutation restored and the build redone with `--no-incremental` afterwards:
- Reverting the refresh region to `d728ff46`, dropping the refresh `when (_disposed)` filters, or moving the gate reset off the dispatcher each still passes all 55 `AuditAvailabilityStateTests`.
- Deleting the template `IsSafe` check still passes `Receipt_component_rejects_an_unsafe_localized_summary`.
- Ignoring the outcome category fails 2 of 3 rows of the new outcome theory.
- Making the focus catches unconditional fails `Live_focus_handoff_does_not_hide_non_teardown_failures`.

- [x] [Review][Patch] Make the refresh-teardown test fault the host refresh after disposal. It currently passes on the pre-patch code, so the six new refresh/finalize catches are unproven [tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs:349]
- [x] [Review][Patch] Pin the live-failure path of the refresh `when (_disposed)` filters and of the focus `ObjectDisposedException`/`TaskCanceledException` filters. A live host refresh that faults must surface, count no retry, and leave Refresh re-invocable [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:312]
- [x] [Review][Patch] Add a test that distinguishes the atomic dispatcher finalization from the old off-dispatcher gate reset. Hold the dispatcher, release the host task, click again, and assert one host call and one counted retry [src/Hexalith.Tenants.UI/Components/Tenants/Audit/AuditAvailabilityState.razor:340]
- [x] [Review][Patch] Isolate the template safety check in the unsafe-summary test (`"Actor: Bearer {actor} | …"` keeps the label structure) and add a positive control. `DirectReceipt(Ready)` with the default template must render exactly one copy button [tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs:214]
- [x] [Review][Patch] Restore the accents in the now-announced French receipt group name: `Actions de reprise du reçu d’audit` [src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx:3257]
- [x] [Review][Patch] Mirror `role="group"` in the browser-harness availability fixture and pin `role` in the fixture-parity test [tests/Hexalith.Tenants.UI.Tests/Browser/tenants-focus-browser-validation.html:51]
- [x] [Review][Patch] Bound the refresh-teardown test's final await with a timeout, so a regression fails the test instead of hanging the run [tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs:368]
- [x] [Review][Patch] Assert that no receipt action group renders when a state has no actions, such as Loading [tests/Hexalith.Tenants.UI.Tests/Components/AuditEvidenceReceiptTests.cs:681]
- [x] [Review][Defer] The legacy story's `baseline_commit` is not a commit, so the gitlink guard fails on this artifact, and the 2026-09-30 note that the story guard "reported no pointer changes" overstates it [_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md:3] — deferred: the fix edits this story's frontmatter and needs an owner decision. `a5ca6e3f…` does not resolve. The intended `a5ca6e38` (Story 5.3, 2026-06-06) predates the `references/` layout, so correcting it gives 7 "absent at baseline" failures. The PASS came from `spec-…-2.md` (`d728ff46`), and the primary spec's range (`55f3dc63..HEAD`) passes with all three bumps declared.
- [x] [Review][Defer] The legacy story record is stale in several places [_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md:211] — deferred: the fix edits this story artifact's historical sections.
  - The "Senior Developer Review (AI)" header still reads "Outcome: Approve (status → done)" while Status is `review`.
  - The file:line anchors on the ticked 2026-09-30 patches are stale: `role="group"` is at `:56`, not `:51`, and the refresh logic spans `:287-376`, not `:299`.
  - There are no 2026-09-30 Debug Log References with exact commands.
  - The "three mutation rejections" are never named.

#### Rejected (2026-09-30 re-review)

- [Rejected][low] Refresh reads `AuditAuthorityRefresh`, `OwnerRefreshIncludesAuditAuthority`, and the host version probe off the dispatcher after `ConfigureAwait(false)`. This predates the diff (the lines are unchanged), and a harmful race needs a host re-render that reassigns a stable cascading delegate mid-refresh. The fix restructures the refresh continuation.
- [Rejected][false] The `TaskCanceledException` catches are no-ops, miss `OperationCanceledException`, and could let cancellation crash the circuit. The renderer's error-handled task wrapper ignores canceled tasks for both event handlers and after-render, so no bad outcome occurs.
- [Rejected][false] Delegate-absent receipt routing is untested, so Unauthorized could offer Refresh. Both production hosts wire `OnClose` (`RemoveTenantMemberFlow.razor:191`, `TenantAuditPage.razor:273`), so that fallthrough is unreachable. The reachable delegate-absent branch (Ready without inspect) is pinned by `RemoveTenantMemberFlowTests.Confirmed_removal_with_matching_audit_row_renders_wp2a_receipt`: a mutation letting Ready fall through failed exactly that test.
- [Rejected][false] The unknown-outcome theory is too thin. A mutation that ignored the category failed 2 of its 3 rows, and `IsKnownOutcome` is an ordinal exact-match switch, so case and whitespace variants already hit `_ => false`.
- [Rejected][false] The reflection-based focus teardown tests are unrealistic and fragile. They exercise the same filtered catches as the real race, removing the containment fails all 3 disposed rows, and a member rename fails loudly rather than passing vacuously.
- [Rejected][false] Stale follow-up spec bookkeeping (`review_loop_iteration: 0`, the AppHost health note). The only fix edits the spec under review.
- [Rejected][low] A host that unmounts the control during its own refresh and then throws a real `ObjectDisposedException`/`InvalidOperationException` has that exception swallowed, because `_disposed` is a lifetime proxy, not the exception's origin. This needs a host defect after the same refresh removed the control, and the fix splits host-task awaiting from dispatch awaiting.
- [Rejected][false] A live focus `TaskCanceledException` (JS interop timeout) ends the circuit. A canceled after-render task is non-fatal to the renderer.
- [Rejected][false] A concurrent session mutated `AuditAvailabilityState.razor` around 14:58. Those edits were this review's own mutation runs. They were restored, the tree is clean at `82b13514`, and the binaries were rebuilt with `--no-incremental`.

### Review Findings (2026-09-30 re-review 2 of `55fc6f91..a4a1ce13`)

Scope: the verification-gap closure commit `6a608e73` (merged as `a4a1ce13`, PR #49) against `spec-5-4-understand-audit-availability-and-recovery-3.md`. Layers: Blind Hunter, Edge Case Hunter, Verification Gap, Acceptance Auditor; none failed. All eight 2026-09-30 re-review patches are implemented, and neither `AuditAvailabilityState.razor` nor `AuditEvidenceReceipt.razor` changed. Independent evidence on a clean tree at `a4a1ce13`:
- **Build:** `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Debug -m:1 --no-incremental -p:UseHexalithProjectReferences=true` gave 0 warnings and 0 errors.
- **Full suite:** the maintained MTP run passed 3,585 of 3,586. The only failure was a `WaitForAssertion` timeout in `TenantDetailSurfaceTests.Detail_lifecycle_actions_fail_closed_while_authorization_is_pending`, which is not in the diff and passes 3 of 3 runs on its own.
- **Changed classes:** `AuditAvailabilityStateTests`, `AuditEvidenceReceiptTests`, `TenantListSurfaceTests` and `GeneratedTenantsSurfaceTests` passed 263 of 263, five times in a row.
- **Mutations:** each file was restored and rebuilt with `--no-incremental` afterwards.
  - Deleting the whole-template `IsSafe` condition fails `Receipt_component_checks_template_safety_across_summary_segments`, while `Receipt_component_rejects_an_unsafe_localized_summary` still passes.
  - Moving `_refreshInFlight = false` before the finalization dispatch fails `Dispatcher_finalization_keeps_the_gate_closed_until_the_retry_is_counted`, because the host is called twice.
- **CI:** PR #49 was merged with `ci / build-and-test` and `validate-remove-focus-in-chromium` red, so no Tenants test tier ran in CI. The verification report doesn't mention CI.

- [x] [Review][Patch] Pin the sort direction and column for a non-empty ascending sort event, including the Status column. Dropping the `Ascending` read still passes the suite, because only descending and empty sorts reach `OnTenantSortChanged` [src/Hexalith.Tenants.UI/Components/Pages/TenantsWorkspace.razor:1656] — closed 2026-10-01: both real grid ascending events assert column/direction, canonical URL, and cursor/history reset; both reject the descending-only mutation.
- [x] [Review][Patch] Drive the successful post-disposal refresh through the real Refresh click (`ClickAsync`). Otherwise no teardown test covers click → `InvokeRecoveryAsync` → `RefreshAsync`, and the verification report's "original successful-disposal case is retained" is inaccurate [tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs:388] — closed 2026-10-01: real Refresh click with bounded host completion after disposal and zero counted retries; historical report wording corrected.
- [x] [Review][Patch] Correct the `aria-rowcount` rationale here and in the verification report (line 42). `5.0.0-rc.5-26219.1` and `5.0.0` both render `TotalItemCount + 1`. What changed is that the item count is filled in by the time bUnit asserts, so the old `"1"` meant zero counted rows [tests/Hexalith.Tenants.UI.Tests/GeneratedTenantsSurfaceTests.cs:170] — closed 2026-10-01: test comment, File List, Change Log, and report now describe populated item-count timing.
- [x] [Review][Patch] In the finalization theory, assert exception identity (`ShouldBeSameAs`), not just the type, for the live `ObjectDisposedException` and `InvalidOperationException` rows, as the verification report says it does [tests/Hexalith.Tenants.UI.Tests/Components/AuditAvailabilityStateTests.cs:440] — closed 2026-10-01: non-cancellation rows assert identity; cancellation keeps exact-type verification.
- [x] [Review][Patch] Correct the new Change Log entry. It says "six mutation", while the Completion Notes and verification report record nine, and it leaves out the final 3,586/3,586 run [_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md:210] — closed 2026-10-01: the historical entry records nine mutations and 3,586/3,586; today's 3,588/3,588 run is recorded separately.
- [x] [Review][Defer] CI has not run the Tenants test tiers since at least 2026-09-29. `ci / build-and-test` fails at "Validate package consumer references" because the `Hexalith.Tenants.Server` nupkg dependency boundary includes `Hexalith.EventStore.ServiceDefaults`, so Tier 1 and Tier 2 report no counts. PR #49 was merged red (runs `36747901026` and `36747920099`) [src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj:6] — deferred: pre-existing (first failed in run `36590317356` at `e077e65e`); the cause is the source-referenced `Hexalith.EventStore.Server` graph at the bumped EventStore gitlink, not this diff
- [x] [Review][Defer] `validate-remove-focus-in-chromium` aborts in CI (Chrome 153 core dump, exit 134) on every `main` push since at least 2026-09-29; it last passed on 2026-09-22 [tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh:200] — deferred: pre-existing. Local Chrome 154 passes. A runner sandbox restriction is suspected but unverified; settle it by capturing the `.stderr` output in the workflow
- [x] [Review][Defer] The browser harness hardcodes `obj/Release` scoped-CSS inputs. Local Debug-only verification therefore needs an uncommitted `/tmp` copy, and the recorded Chrome evidence can't be reproduced from the repository [tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh:8] — deferred: pre-existing harness design
- [x] [Review][Defer] Some bUnit `WaitForAssertion` checks time out under load. Two cases so far: an unnamed metadata-confirmation test during development, and `TenantDetailSurfaceTests.Detail_lifecycle_actions_fail_closed_while_authorization_is_pending` in this review's full run [tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs:1924] — deferred: pre-existing; neither test is in the diff
- [x] [Review][Defer] `validate-story-gitlinks.py` still exits 1 on this legacy story, because `baseline_commit` `a5ca6e3f…` is not a commit [_bmad-output/implementation-artifacts/5-4-audit-availability-state-recovery.md:3] — deferred: already recorded 2026-09-30. The spec-3 range (`55fc6f9..HEAD`, no pointer changes) and the primary spec range (`55f3dc6..HEAD`, 3 declared bumps) both pass

#### Rejected (2026-09-30 re-review 2)

- [Rejected][low] The Fluent UI sort migration and test updates are bundled into `fix(audit)`. They were required, because the Builds bump in `55fc6f91` moved Fluent UI to `5.0.0` and CI failed with CS1061 (run `36726541810`). The spec and File List document them, and merged history can't be relabeled.
- [Rejected][false] The unsafe-summary test does not isolate the template check. Deleting that check fails the new cross-segment test (verified by mutation); the older test is a combined rejection test, and spec-3 documents why.
- [Rejected][fix edits the spec under review] Spec-3 bookkeeping: `review_loop_iteration: 0` and the superseded 3,580/3,580 and "six" statements.
- [Rejected][false] The 2026-09-30 deferral about Debug Log References is stale. The new Debug Log entry only points to the report, so "no … Debug Log References with exact commands" still holds.
- [Rejected][false] Commitlint evidence used a `/tmp` message file. Validating the exact candidate file before committing is the prescribed flow, and CI commitlint passed on PR #49.
- [Rejected][false] The new PascalCase test names break the file's convention. The Hexalith baseline requires PascalCase test names. The unbounded await in `Disposed_focus_handoff_contains_renderer_teardown` predates this diff and is low.
- [Rejected][low] The SSR route test still asserts `aria-rowcount="1"` (`GeneratedTenantsSurfaceTests.cs:151`). This weakness predates the diff: that test never asserted data rows, and tightening it needs prerender investigation.
- [Rejected][false] A live finalization dispatcher fault closes the Refresh gate for good. That exception escapes the click handler, which ends the circuit, so no later click exists.
- [Rejected][false] The teardown exception set is too narrow. Canceled tasks don't break the renderer (already rejected in the previous re-review), and the host refresh makes gateway calls, not JS calls. More negative cases would add tests without a demonstrated bad outcome.
- [Rejected][low] The dispatcher test has no guard proving it reached the race window. It is valid on the current framework (the off-dispatcher gate-reset mutation fails it), and a framework-change guard would add more reflection.
- [Rejected][false] Reflection-string access to private members is fragile. A rename fails loudly with `NullReferenceException`, not silently; this was already rejected in the previous re-review.
- [Rejected][low] The French receipt copy still mixes accented and unaccented spellings. This predates the diff and is already tracked (DW-270, DW-306, DW-324 and the 2026-09-28 receipt entry).
- [Rejected][low] The browser fixture's French button and badge labels are hand-written and unchecked. This predates the diff; the patch only asked for group-role parity.
- [Rejected][false] `SortColumns[0]` depends on an unpinned `SortMode`. `SortMode` defaults to `Single` and is never set, so `SortColumns` holds at most one entry.
- [Rejected][low] The fault branch of `AuditAuthorityRefresh` is untested. The same `when (_disposed)` catches are already exercised through the owner-refresh faults.
