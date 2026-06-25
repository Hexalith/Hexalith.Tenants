---
baseline_commit: 6e4b4e8
---

# Story 2.5: Edit Tenant Metadata with Safe Validation

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 2.5. -->

## Story

As an authorized tenant contributor or global administrator,
I want to edit tenant metadata through a safe confirmed command flow,
so that tenant records can be maintained without hiding validation errors or projection lag.

## Acceptance Criteria

1. Given an authorized tenant contributor or global administrator opens the edit-metadata flow, when the form renders, then editable fields use localized labels, whole-string validation messages, accessible descriptions, stable selectors, and support-safe display rules, and users without permission see inline unavailable-action reasons instead of mutation controls.
2. Given the user submits valid metadata changes, when the command is accepted, then the UI keeps the last-confirmed metadata visible until the tenant detail projection re-query confirms `TenantUpdated`, and every successful edit is treated as an emitted update with no same-state suppression assumption.
3. Given validation fails locally or in the domain, when errors are returned, then the form shows safe localized field messages, and it does not expose raw backend payloads, stack traces, metadata, tokens, internal correlation ids, or PII.
4. Given command confirmation is pending, rejected, failed, degraded, or unknown, when the lifecycle panel renders, then each state remains distinct and accessible, and the UI does not display Success until projection truth confirms the metadata value.
5. Given metadata editing is attempted on a disabled, stale, unauthorized, or unknown-freshness tenant, when eligibility is evaluated, then the action fails closed with a visible localized reason, and stale or unknown projection data cannot be used to imply a safe edit.
6. Given edit outcome evidence is available, delayed, or unavailable, when the result is shown, then the UI provides the appropriate audit/evidence handoff state, and correction remains a future forward command, never an event rewrite.
7. Given this story is complete, when verification is run, then unit/component tests cover contributor/global-admin permission reflection, validation messages, command submission, projection-confirmed metadata update, no same-state suppression assumption, stale/disabled/unauthorized gating, and audit handoff.
8. Given accessibility verification is run, then Playwright or component tests verify keyboard editing, focus return, live-region politeness, forced-colors-safe status rendering, stable selectors, support-safe errors, and no optimistic metadata overwrite.

## Tasks / Subtasks

- [x] Extend the existing Tenants command gateway for tenant metadata updates (AC: 2, 3, 4, 6)
  - [x] Add `UpdateTenantAsync(UpdateTenant request, CancellationToken cancellationToken = default)` to `ITenantCommandGateway`, `TenantCommandGateway`, and `UnavailableTenantCommandGateway`; do not add a generic command gateway, browser-side backend client, controller, or new endpoint.
  - [x] Submit the existing `UpdateTenant` domain command through the existing EventStore command path with `messageId = IUlidFactory.NewUlid()`, `tenant = "system"`, `domain = "tenants"`, `aggregateId = tenantId`, `commandType = nameof(UpdateTenant)`, and payload `new UpdateTenant(tenantId, name, description)`.
  - [x] Validate tenant id and name before submit. Tenant ids are literal caller-supplied strings; never parse or generate them as GUIDs or ULIDs. Treat description as optional and support an explicit clear-to-null/empty behavior without unsafe payload display.
  - [x] Add update-tenant-specific safe submission mappings for `InsufficientPermissionsRejection`, `TenantDisabledRejection`, and `TenantNotFoundRejection`; map gateway/local validation failures to safe field or lifecycle messages.
  - [x] Keep `GetStatusAsync` shared-status fallback command-neutral for shared rejection types. Do not reintroduce the Story 2.2/2.3 cross-command rejection-copy leak.

- [x] Add metadata-edit lifecycle state without optimistic projection mutation (AC: 2, 4, 6)
  - [x] Add `UpdateTenant` and a focused `TenantUpdateMetadataCommandSnapshot`, or reuse/generalize existing command state only if it preserves create/add/change/remove behavior and tests.
  - [x] Track tenant id, submitted name, submitted description, last-confirmed name/description, message id, correlation id, safe message, rejection code, audit handoff state, focus target, and last-confirmed tenant detail projection evidence.
  - [x] Represent request sent, accepted, projection pending, confirmed, rejected, failed, degraded, unable to verify, audit pending, audit delayed/unavailable, and missing support distinctly. Do not collapse `accepted` into `confirmed`, and do not style in-flight states as Success.
  - [x] Treat status `Completed`/`EventsStored`/`EventsPublished` only as projection pending until an authoritative tenant detail re-query proves the submitted `Name` and `Description` are present on the matching literal `TenantId`.
  - [x] Treat SignalR as a freshness nudge only. It may trigger a re-query, but it cannot set confirmed metadata or audit availability.
  - [x] Preserve last-confirmed metadata separately from submitted intent. Do not overwrite the visible tenant name/description or summary text until projection evidence proves the update.
  - [x] Do not model identical metadata as a domain NoOp or `already applied`; the backend always emits `TenantUpdated` for authorized `UpdateTenant`.

- [x] Build the edit-metadata flow from the tenant detail surface (AC: 1, 2, 3, 5, 8)
  - [x] Add a focused component under `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/`, for example `EditTenantMetadataFlow.razor` plus colocated CSS if needed, and compose it from `TenantDetailPage`.
  - [x] Launch/edit from the tenant metadata area using current tenant id, name, description, status, freshness, surface kind, authorization reflection, and command-surface availability.
  - [x] Use accessible form fields for name and description with whole-string labels, descriptions, error messages, stable selectors, and keyboard complete-or-exit behavior.
  - [x] Keep users without edit permission on a read-only detail surface with inline `UnavailableActionReason` text. Do not hide the reason behind tooltip-only copy.
  - [x] Fail closed before submit when tenant status is disabled/unknown, detail freshness is stale or unknown, authorization is missing/indeterminate, command lifecycle support is unavailable, another command is in flight, or required form data is invalid.
  - [x] Add stable selectors such as `tenants-edit-metadata-open`, `tenants-edit-metadata-flow`, `tenants-edit-metadata-name`, `tenants-edit-metadata-description`, `tenants-edit-metadata-submit`, `tenants-edit-metadata-cancel`, `tenants-edit-metadata-unavailable-reason`, `tenants-edit-metadata-lifecycle`, `tenants-edit-metadata-state`, `tenants-edit-metadata-audit`, `tenants-edit-metadata-recovery`, and `tenants-edit-metadata-refresh`.

- [x] Preserve tenant detail and existing command behavior while wiring projection confirmation (AC: 2, 4, 5, 8)
  - [x] Update `TenantDetailPage` only as needed to compose the edit flow and provide update projection evidence. Reuse the existing `RefreshTenantDetailAsync` pattern.
  - [x] Keep tenant identity, status, lifecycle, freshness, member summary, member command flows, configuration summary, configuration view, copy controls, stale/degraded messaging, and return navigation behavior unchanged.
  - [x] Use one-at-a-time locking across tenant detail command flows. While metadata update is in flight, create/add/change/remove command triggers that share the command surface are unavailable with visible reasons; do not add bulk or concurrent command batching.
  - [x] Keep narrow-width behavior fail-closed: tenant identity, editable fields, freshness, lifecycle state, unavailable reason, command state, and recovery action remain visible, or the edit action is unavailable with a visible reason.

- [x] Add honest audit handoff and recovery actions (AC: 4, 6)
  - [x] Render audit states as audit pending, audit delayed, audit unavailable, or missing implementation support until Epic 5 audit evidence surfaces exist.
  - [x] Do not render `audit available`, an Audit Evidence Receipt, or audit proof links in Story 2.5 unless the Epic 5 evidence source is actually implemented and reachable.
  - [x] Recovery copy must use explicit forward-correction language: wait, refresh, retry status lookup, inspect audit, continue read-only, request permission, start correction, or escalate. Never use "undo", "rollback", or imply event/state-store edits.
  - [x] Do not expose raw command payloads, EventStore metadata, correlation ids, bearer tokens, decoded JWTs, cursors, ETags, stack traces, problem-detail internals, raw metadata, or unsafe PII in visible copy, logs, announcements, or copy affordances.

- [x] Add localization, accessibility, and focused tests (AC: 1-8)
  - [x] Add EN/FR `Tenants.EditMetadata.*` resource parity with whole-string messages and named placeholders for tenant id, name, description, validation messages, lifecycle states, audit states, unavailable reasons, and recovery actions.
  - [x] Ensure keyboard users can open, edit, submit, cancel, recover, or close the metadata flow. Focus returns to the launching edit control after close/cancel/submit/failure.
  - [x] Use state-driven live regions: polite for submitted/accepted/projection-pending/confirmed/audit-pending; assertive for rejected/failed/degraded/unable-to-verify/blocked.
  - [x] Preserve no-color-only and forced-colors behavior with icon/shape/text for lifecycle, audit, unavailable, and validation states.
  - [x] Extend `TenantCommandGatewayTests` for `UpdateTenant` request shape, ULID-shaped message id, literal case-sensitive tenant id, payload name/description, returned correlation id capture, input validation, and safe rejection mappings.
  - [x] Add update-metadata state/model tests for request-sent, accepted, projection-pending, projection-confirmed name/description, no same-state suppression assumption, SignalR nudge only, rejected/failed/degraded/unable-to-verify, audit handoff, and no optimistic metadata overwrite.
  - [x] Add component tests for form rendering, permission reflection, stale/unknown/disabled gating, validation messages, submit/cancel/focus return, one-command-at-a-time locking, stable selectors, support-safe copy, resource parity, and preservation of existing tenant detail/member/configuration behavior.
  - [x] Update maintained verification summaries in `_bmad-output/implementation-artifacts/tests/test-summary.md` and `tests/test-summary.md` if this repository continues that practice for Story 2.5.
  - [x] Run per-project verification. Prefer `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` plus the xUnit v3 in-process executable when `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 2 Story 2.5. Epic 2 adds tenant/member mutation flows over the Epic 1 read foundation and the Story 2.1-2.4 command lifecycle foundation. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.5: Edit Tenant Metadata with Safe Validation`]
- FR14 requires tenant metadata editing for tenant contributors or global administrators, with validation errors as safe localized field messages and no same-state suppression assumption. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-14: Edit tenant metadata`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`]
- The implementation-readiness report identifies FR14 as a Phase 2b command flow and restates the key correction: RBAC is contributor/global-admin, and every successful edit emits `TenantUpdated`. [Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-03.md#FR-14`]
- Command stories use confirmed `FC-CMD` command feedback and `FC-CNC` one-at-a-time policy from Story 1.0; `FC-AUD` proof remains missing and must be represented honestly until Epic 5. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#B. FrontComposer dependency readiness`; `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#The three approved fallbacks`]

### Command Contract Details

- `UpdateTenant` already exists as `public record UpdateTenant(string TenantId, string Name, string? Description);`. Commands are plain public records with primary constructors; do not add XML docs, `sealed`, marker interfaces, or new contract fields. [Source: `src/Hexalith.Tenants.Contracts/Commands/UpdateTenant.cs`; `_bmad-output/project-context.md#C# Language & Contract Rules`]
- `TenantUpdated` already exists as `public record TenantUpdated(string TenantId, string Name, string? Description, DateTimeOffset UpdatedAt) : IEventPayload;`. Projection confirmation should compare authoritative tenant detail metadata, not the submitted intent alone. [Source: `src/Hexalith.Tenants.Contracts/Events/TenantUpdated.cs`; `src/Hexalith.Tenants.Contracts/Queries/TenantDetail.cs`]
- `TenantAggregate.Handle(UpdateTenant, ...)` rejects missing tenant, disabled/unknown tenant status, and insufficient permission; otherwise it emits `TenantUpdated`. It does not have a same-value NoOp branch. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#Handle(UpdateTenant)`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/review-domain-fidelity.md#Finding 3`]
- Domain tests already prove update success, not-found, disabled, envelope aggregate id precedence, reader/non-member rejection, and contributor/owner/global-admin success. Do not change backend semantics unless a contract defect is proven. [Source: `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs#UpdateTenant_on_active_tenant_produces_TenantUpdated`; `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs#RBAC_UpdateTenant_by_contributor_succeeds`; `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs#RBAC_UpdateTenant_by_reader_produces_InsufficientPermissionsRejection`]
- Safe command request shape mirrors prior command stories: `messageId`, `tenant = "system"`, `domain = "tenants"`, `aggregateId = tenantId`, `commandType = "UpdateTenant"`, payload with `TenantId`, `Name`, and `Description`. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `_bmad-output/project-context.md#Event-Sourcing & Domain Rules`]

### Architecture And Boundary Requirements

- Tenants UI is Blazor InteractiveServer with a server-side BFF. Browser code must not call backend services directly or hold backend access tokens. [Source: `_bmad-output/planning-artifacts/architecture.md#Selected Starter: new src/Hexalith.Tenants.UI Blazor host composing the FrontComposer Shell`]
- All backend command egress goes through `ITenantCommandGateway` / `TenantCommandGateway` / `UnavailableTenantCommandGateway`. Extend this path; do not add a controller, browser `HttpClient`, or generic command infrastructure in Tenants. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `AGENTS.md#Domain Implementation Boundary`]
- Projection truth is authoritative. Status `Completed` or `EventsPublished` is not proof that metadata changed; confirmation requires tenant detail projection re-query proving the literal tenant id, submitted name, and submitted description. [Source: `_bmad-output/planning-artifacts/architecture.md#Project Context Analysis`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#5.2 Non-collapse invariant`]
- SignalR projection notifications are freshness nudges only. A nudge can prompt refresh/re-query but cannot set confirmed metadata or audit availability. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Truth & Honesty Invariants`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`]
- Command lifecycle belongs inline, anchored to the affected panel/detail area. Do not turn command lifecycle into navigation or page-global feedback except for page-level/system degradation. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Component Patterns`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#Components`]
- Support-safety is a hard rule: no surface, label, log, toast, receipt, or copied value may expose tokens, decoded JWT contents, command payloads, serialized event bodies, raw EventStore metadata, internal correlation ids, stack traces, raw metadata, or real PII. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#10. Privacy, Security, and Support-Safety`; `_bmad-output/planning-artifacts/architecture.md#Authentication & Security`]

### Existing Files To Update And Preserve

- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`: add metadata update submission while preserving create-tenant, add-member, change-role, remove-member, and `GetStatusAsync`.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`: add `UpdateTenant` mapping and update-specific safe rejection messages; keep shared status rejection copy command-neutral.
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`: add fail-closed update behavior matching unavailable command surface.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`: currently contains create/add-member/change-role/remove-member request and snapshot models plus shared lifecycle enums. Add update models here or split into clearer files only if useful; preserve existing tests and semantics.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`: compose the metadata edit flow from the existing tenant identity/metadata summary and provide projection evidence for update confirmation; preserve detail refresh and return navigation.
- `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/`: create this folder if needed for `EditTenantMetadataFlow.razor` and colocated CSS. Architecture anticipated this component name for FR14. [Source: `_bmad-output/planning-artifacts/architecture.md#Component Organization`]
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`: add `Tenants.EditMetadata.*` resources with EN/FR parity and whole-string messages.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`: extend gateway coverage for `UpdateTenant`.
- `tests/Hexalith.Tenants.UI.Tests/State/`: add update-metadata lifecycle/model tests matching existing command snapshot tests.
- `tests/Hexalith.Tenants.UI.Tests/Components/`: add edit-metadata flow tests and extend `TenantDetailSurfaceTests` without weakening existing assertions.

### Scope Boundaries And Anti-Patterns

- Do not change `src/Hexalith.Tenants.Contracts` or `src/Hexalith.Tenants.Server` unless existing contracts are proven wrong. The update command, event, aggregate behavior, projection fields, and rejection behavior already exist.
- Do not add a backend metadata validation endpoint, preview endpoint, receipt endpoint, command status endpoint, controller, generic command bus, browser-side backend client, or shared UI scaffolding in Tenants.
- Do not parse tenant id as GUID/ULID; preserve literal, case-sensitive strings.
- Do not optimistically overwrite tenant name/description, list row title, page title, or summary copy before projection evidence proves the update.
- Do not treat identical name/description submission as a domain NoOp or `already applied`. If implementation chooses client-side dirty-check suppression, label it as an unsubmitted form state, not command success, and keep AC2's "every submitted successful edit emits" rule intact.
- Do not expose raw rejection payloads, command payloads, EventStore metadata, correlation ids, status internals, tokens, decoded JWT contents, cursors, ETags, stack traces, raw metadata, or real PII in UI copy, logs, docs, announcements, or copy affordances.

### Previous Story Intelligence

- Story 2.4 added the latest command gateway extension, command snapshot lifecycle states, projection-confirmed refresh, EN/FR resources, support-safe lifecycle copy, and focused gateway/state/component/resource tests. Reuse these patterns for metadata edit instead of introducing parallel infrastructure. [Source: `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md#Completion Notes List`]
- Story 2.4 review fixed submission-time `UserNotInTenant` reconciliation by requiring projection evidence before treating the outcome as already applied. Apply the same standard here: submitted metadata is confirmed only by authoritative projection evidence, never by command status alone. [Source: `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md#Senior Developer Review (AI)`]
- Story 2.3 review fixed a shared-status defect where shared rejection types surfaced command-specific copy in the wrong lifecycle panel. Story 2.5 must keep `GetStatusAsync` command-neutral for shared rejection types and use command-specific copy only where command context is known. [Source: `_bmad-output/implementation-artifacts/2-3-change-tenant-member-role.md#Senior Developer Review (AI)`]
- Story 2.2 left `IsCommandSurfaceAvailable` not wired from `ITenantsBffComposition.IsCommandSurfaceConnected` as a shared follow-up. Do not solve that only for metadata edit unless the story needs it end-to-end; if touched, preserve create/add/change/remove behavior or update all affected tests. [Source: `_bmad-output/implementation-artifacts/2-2-add-user-to-tenant-with-explicit-role.md#Observations (not auto-fixed - out of this story's scope)`]
- Story 1.6 and Story 1.7 established tenant detail/member/configuration read behavior, stale/degraded states, and visible unavailable action reasons. Story 2.5 turns metadata edit actionable but must preserve the surrounding read safety context. [Source: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md`; `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`]
- Story 1.8 established support-safe copy behavior. Apply the same posture to metadata edit lifecycle copy, validation text, command references, and recovery actions. [Source: `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`; `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyClassifier.cs`]

### Git Intelligence

- Recent commits are story-scoped Conventional Commits: `feat(story-2.4): Remove Tenant Member with Consequence Preview`, `feat(story-2.3): Change Tenant Member Role`, `feat(story-2.2): Add User to Tenant with Explicit Role`, `feat(story-2.1): Create Tenant with Projection-Confirmed Command Lifecycle`, and `docs(retro): record epic 1 retrospective`. If Story 2.5 is committed later, use a Conventional Commit such as `feat(story-2.5): Edit Tenant Metadata with Safe Validation`. [Source: `git log --oneline -5`]
- Story 2.4 touched the same command infrastructure Story 2.5 will touch: story artifacts, sprint status, UI command gateway/state/resources, detail/member components, UI tests, and test summaries. Keep Story 2.5 similarly focused. [Source: `git show --stat --name-only 6e4b4e8 --`]

### Latest Technical Information

- Use the repo-pinned local stack and contracts: .NET 10 SDK/package pins from `global.json` and `Directory.Packages.props`, Fluent UI Blazor v5 RC inherited by the UI project, FrontComposer command/fallback contracts, EventStore command contracts, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not introduce package versions or new libraries for Story 2.5. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- No external API/package research is required for implementation beyond the pinned local contracts. The primary risks are contract reuse, support-safe validation/rejection mapping, no same-state suppression assumption, contributor/global-admin availability reflection, one-at-a-time command locking, and projection/audit truth correctness.

### Project Structure Notes

- Source changes should stay under `src/Hexalith.Tenants.UI/`, primarily `Components/Pages/`, `Components/Tenants/Metadata/`, `State/TenantCommands/`, `Services/Gateways/`, `Resources/`, and colocated CSS.
- Tests should stay under `tests/Hexalith.Tenants.UI.Tests/` with xUnit v3, Shouldly, bUnit, and NSubstitute. Test files remain plural `{Class}Tests.cs`.
- The story should not require changes to `src/Hexalith.Tenants.Contracts`, `src/Hexalith.Tenants.Server`, `src/Hexalith.Tenants`, AppHost, IntegrationTests, submodules, package metadata, or shared technical modules unless an existing command contract is proven wrong.
- Detected conflict to manage: PRD history previously misstated metadata edits as NoOp. Current PRD/addendum/epics and source agree that `UpdateTenant` always emits `TenantUpdated`; the story and implementation must follow the corrected rule.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 2.5: Edit Tenant Metadata with Safe Validation`
- Epic context and requirements: `_bmad-output/planning-artifacts/epics.md#Epic 2: Tenant Membership and Tenant Record Management`; `_bmad-output/planning-artifacts/epics.md#FR14`
- PRD and addendum: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-14: Edit tenant metadata`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/review-domain-fidelity.md#Finding 3`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`; `_bmad-output/planning-artifacts/architecture.md#Project Context Analysis`; `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md`
- UX: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#command-lifecycle-panel`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Component Patterns`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#State Patterns`
- Truth and command specs: `docs/tenants-ui-truth-state-and-action-availability-spec.md`; `docs/compensating-commands.md`; `docs/event-contract-reference.md`
- Command/auth contracts: `src/Hexalith.Tenants.Contracts/Commands/UpdateTenant.cs`; `src/Hexalith.Tenants.Contracts/Events/TenantUpdated.cs`; `src/Hexalith.Tenants.Contracts/Queries/TenantDetail.cs`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`
- Existing UI implementation: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`; `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- Prior story evidence: `_bmad-output/implementation-artifacts/2-1-create-tenant-with-projection-confirmed-command-lifecycle.md`; `_bmad-output/implementation-artifacts/2-2-add-user-to-tenant-with-explicit-role.md`; `_bmad-output/implementation-artifacts/2-3-change-tenant-member-role.md`; `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md`; `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md`; `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md`; `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, `architecture.md`, targeted PRD/addendum/domain-fidelity/UX/fallback sections, Story 2.4, current Tenants UI command/detail/source files, `UpdateTenant` domain contracts/aggregate behavior/tests, and recent git history.
- Validation was run against `.agents/skills/bmad-create-story/checklist.md`; disaster-prevention checks are reflected in this story context.
- Dev-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Story 2.5 implementation loaded the full story, sprint status, project context, existing Tenants UI command gateway/state/detail/member components, resources, and UI tests before editing.
- Red/green validation checkpoints: initial UI build failed until test stubs implemented `UpdateTenantAsync`; new metadata component tests initially failed on event-driver mismatch and an over-broad success-copy assertion, then passed after test corrections.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --logger "console;verbosity=minimal"` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.
- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release -m:1 --no-restore` was attempted and failed on NU1900 because restricted network access prevented NuGet vulnerability data retrieval.
- QA generate E2E activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Story 2.5 QA generation auto-applied test gaps for command-surface fail-closed gating, gateway submission failure support-safe state, cancel/Escape close without submit, forced-colors/focus CSS hooks, and refreshed maintained test summaries.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 2.5 to tenant metadata editing through existing UI command infrastructure, not backend contract work or shared FrontComposer scaffolding.
- Story context identifies the key implementation risks: contributor/global-admin availability reflection, safe validation messages, existing `UpdateTenant` command reuse, no same-state NoOp assumption, projection-confirmed metadata update only after tenant detail re-query, disabled/stale/unauthorized fail-closed gates, command-neutral shared status mapping, support-safe copy, and honest audit handoff.
- Extended the existing tenant command gateway with `UpdateTenantAsync`, preserving literal tenant ids, existing EventStore command submission shape, update-specific safe rejection mappings, and command-neutral shared status fallback behavior.
- Added `UpdateTenant` and `TenantUpdateMetadataCommandSnapshot` to track submitted intent, last-confirmed metadata, tracking ids, safe lifecycle messages, rejection code, audit state, focus target, and tenant detail projection evidence without optimistic metadata mutation.
- Added `EditTenantMetadataFlow` under `Components/Tenants/Metadata/` and composed it from `TenantDetailPage`; it renders stable selectors, localized accessible fields, inline unavailable reasons, lifecycle/audit/recovery states, safe validation, clear-to-null description behavior, and projection-confirmed metadata updates only after tenant detail re-query.
- Wired metadata update in-flight activity into `TenantDetailPage` so shared member command flows receive command-surface unavailability while the metadata command is active.
- Added EN/FR `Tenants.EditMetadata.*` resources and updated maintained test summaries for Story 2.5.
- Added focused gateway, state, component, and tenant-detail composition tests covering validation, permission reflection, stale/disabled/unknown gating, support-safe copy, no same-state suppression assumption, no optimistic overwrite, SignalR nudge-only behavior, terminal lifecycle states, audit handoff, and command locking.
- Verification: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Verification: `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed with 311 total, 0 errors, 0 failed, 0 skipped.
- QA generation verification: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- QA generation verification: `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed with 315 total, 0 errors, 0 failed, 0 skipped.

### File List

- `_bmad-output/implementation-artifacts/2-5-edit-tenant-metadata-with-safe-validation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor.css`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/AddTenantMemberFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/ChangeTenantMemberRoleFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/EditTenantMetadataFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantMemberFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantUpdateMetadataCommandSnapshotTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- `tests/test-summary.md`

### Change Log

- 2026-06-06T07:16:07+02:00 - Created Story 2.5 context and marked it ready for development.
- 2026-06-06T07:21:11+02:00 - Started Story 2.5 development and marked sprint status in progress.
- 2026-06-06T07:34:11+02:00 - Implemented projection-confirmed tenant metadata editing flow, added tests/resources/summaries, completed validation, and marked story ready for review.
- 2026-06-06T07:39:47+02:00 - Executed QA generate E2E workflow, auto-applied discovered metadata-flow test gaps, completed validation, and refreshed test summaries.
- 2026-06-06T08:05:00+02:00 - Senior Developer Review (AI) completed: auto-fixed the confirmed-metadata display so a confirmed clear-to-null description no longer falls back to the ambient `Detail.Description`, added regression test `Confirmed_clear_to_null_description_shows_empty_state_and_not_the_ambient_detail_description`, verified build (0 warnings) and 316 UI tests passing, refreshed test summaries, and marked story done.

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (adversarial review on 2026-06-06)
**Outcome:** Approve (1 low correctness issue auto-fixed; 3 observations recorded — no CRITICAL/HIGH/MEDIUM)
**Verification:** `dotnet build tests/Hexalith.Tenants.UI.Tests/...csproj -c Release -m:1 --no-restore` → 0 warnings / 0 errors. `tests/.../bin/Release/net10.0/Hexalith.Tenants.UI.Tests` → 316 total, 0 failed, 0 skipped (315 pre-review + 1 added regression test).

### Scope validated

- **File List vs git:** Matches reality. The only extra git changes outside the File List are excluded automation/BMAD artifacts (`_bmad-output/story-automator/orchestration-*.md`, `sprint-status.yaml`, test-summary files). All source changes stay within `src/Hexalith.Tenants.UI/{Components/Pages,Components/Tenants/Metadata,Resources,Services/Gateways,State/TenantCommands}` and the UI test project — no scope creep, no contract/server changes, no false "changed" claims.
- **Pre-existing test edits:** 100 insertions / 0 deletions across the touched member/create/composition test files — purely the new `UpdateTenantAsync` stub method plus one added assertion in `TenantDetailSurfaceTests`. No existing assertions weakened.
- **AC coverage:** AC1–AC8 traced to implementation and tests — localized accessible fields with stable selectors + `aria-describedby` wiring; last-confirmed metadata preserved until a tenant-detail re-query proves the literal `TenantId` + submitted `Name` + `Description` (`ConfirmProjection`), with no same-state/NoOp suppression (`Completed`+`EventCount 0` still projection-pending); support-safe validation/rejection copy with no payload/token/correlation leakage (gateway `BoundSafeFailureReason` + marker filter, asserted in tests); fail-closed gating for disabled/unknown/stale/unknown-freshness/unauthorized/in-flight/missing-identity; distinct, non-Success-styled lifecycle states; honest audit pending/delayed/unavailable/missing-support handoff with forward-only recovery copy; EN/FR `Tenants.EditMetadata.*` parity (52 = 52); forced-colors/focus-visible CSS hooks and state-driven live-region politeness.
- **Tasks marked [x]:** Spot-audited against code — all verified done (gateway `UpdateTenantAsync` + update-specific safe rejection mapping with command-neutral shared-status fallback; `UpdateTenant` + `TenantUpdateMetadataCommandSnapshot`; `EditTenantMetadataFlow` composed from `TenantDetailPage`; one-direction in-flight locking of member flows via `_metadataCommandInFlight`; localization; gateway/state/component/composition tests).

### Findings

1. **[LOW — FIXED]** Confirmed-metadata display sourced the "last confirmed description" from `_snapshot.LastConfirmedDescription ?? Detail.Description`. Because `LastConfirmedDescription` is `null` both for "no confirmation yet" and for a *confirmed clear-to-null*, a successful description-clearing edit could fall back to the ambient `Detail` parameter and resurrect the old description — exactly the optimistic/ambient display AC2/AC8 prohibit. In the fully-integrated page flow the parent re-query masks it (the param is refreshed to null too), so this never reached a user, but the component's truth-display was not self-consistent. Fixed in `EditTenantMetadataFlow.razor` by deriving `ConfirmedName`/`ConfirmedDescription` from the unambiguous `LastConfirmedDetailProjection` (seeded by the `Idle` factory, advanced only on proof by `ConfirmProjection`). Added regression test that fails on the old derivation and passes on the fix (verified by temporary revert).
2. **[LOW — observation, not changed]** One-at-a-time command locking is wired one-directionally: while the metadata edit is in flight, member flows receive `IsCommandSurfaceAvailable=false` (satisfying the task as written), but the reverse is not — `MemberAccessReview` exposes no command-activity callback, so a member command in flight does not lock the metadata edit. This is consistent with the architecture and the documented Story 2.2 follow-up (command-surface availability not yet wired from `ITenantsBffComposition`); changing it would expand scope across the member flows, so it is recorded rather than auto-changed.
3. **[LOW — observation, not changed]** `EditTenantMetadataFlow.IsAuthorized` (defaulting to `true`) is not passed by `TenantDetailPage`, so the actual contributor/global-admin-vs-reader RBAC is reflected only at the surface-kind level (`Unauthorized`), not per-role for a reader who can view the detail. This mirrors the member flows (`IsAddMemberAuthorized`/`IsChangeRoleAuthorized`/`IsRemoveMemberAuthorized` are likewise unwired from the page) and the same Story 2.2 deferral; the component itself reflects permission correctly (tested via `IsAuthorized=false`).
4. **[LOW — observation, not changed]** In a submit-time `Rejected`/`Failed` result there is no tracking handle, so `CanRefresh` keeps the Refresh button disabled while the `Rejected` recovery copy still mentions refreshing projection evidence. The copy also offers request-permission/start-correction/escalate, so it is not misleading, and this matches the prior command-flow precedent.
