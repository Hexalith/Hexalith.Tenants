---
baseline_commit: 7443050
created: 2026-06-06T12:43:10+02:00
---

# Story 3.4: Remove Tenant Configuration Key with Consequence Preview

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 3.4. -->

## Story

As an authorized tenant user,
I want to remove a tenant configuration key through a safe command flow,
so that obsolete configuration can be removed with clear consequence and proof.

## Acceptance Criteria

1. Given an authorized user opens remove-configuration for a visible configuration key, when validation, freshness, authorization, lifecycle, and preview data are eligible, then the UI renders a Consequence Preview naming tenant identity, namespace, key, current known state, consequence, known unknowns, audit/evidence expectation, and recovery path, and submission is blocked if any required preview item is unavailable.
2. Given the key is outside the caller's authorized namespace or prefix, when remove availability is evaluated, then the action is unavailable with visible localized reason, and unauthorized key existence is not revealed.
3. Given the target key is missing, when the backend returns `ConfigurationKeyNotFound`, then the lifecycle panel shows a safe localized rejected state, and it does not show Success or remove unrelated visible state.
4. Given the user confirms removal for an eligible key, when the command is submitted, then the command gateway uses the existing command endpoint, one-at-a-time locking, command-status polling, SignalR freshness nudges, and tenant projection re-query, and the key is shown as removed only after projection confirmation.
5. Given removal is accepted, pending, rejected, failed, degraded, unable to verify, audit pending, audit delayed, or audit unavailable, when the result renders, then the UI shows the exact state with localized text and accessible semantics, and it does not edit, delete, rewrite, or fabricate event/projection history.
6. Given the remove flow is cancelled, fails, or is unavailable, when the user exits the flow, then focus returns to the launching control and no action is committed on cancel or Escape, and copy/log/error output remains support-safe.
7. Given this story is complete, when verification is run, then unit/component tests cover preview completeness, namespace scope, hidden unauthorized keys, `ConfigurationKeyNotFound`, projection-confirmed removal, one-at-a-time locking, audit unavailable states, and no optimistic deletion.
8. Given accessibility verification is run, then Playwright or component tests verify destructive confirmation keyboard behavior, focus trap/return, live-region politeness, forced-colors status rendering, stable selectors, and support-safe errors.

## Tasks / Subtasks

- [x] Extend the existing configuration surface with a focused remove flow (AC: 1, 2, 4, 6, 8)
  - [x] Start from `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`; preserve grouping by namespace, filtering, table semantics, empty/filtered-empty states, safe copy controls, sensitive-value redaction, and the existing set-configuration flow.
  - [x] Add a focused `RemoveTenantConfigurationFlow` under `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/` and compose it from visible configuration rows; do not turn the read table or `SetTenantConfigurationFlow` into a monolithic command component.
  - [x] Add a row/action-column launch affordance only for keys already visible in the authorized projection. If the key is not visible or scope evidence is unavailable, fail closed with a localized reason and do not reveal whether an out-of-scope key exists.
  - [x] Keep last-confirmed `TenantDetail.Configuration` visible while preview is open and while a command is in flight; removal intent is not projection truth.
  - [x] Keep configuration remove unavailable on mobile/narrow layouts that cannot preserve preview, freshness, tenant identity, target key, current-state, and last-confirmed configuration context.

- [x] Add remove-configuration request, snapshot, and gateway support using existing command infrastructure (AC: 3, 4, 5)
  - [x] Add `RemoveTenantConfigurationCommandRequest(string TenantId, string Key)` to `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`.
  - [x] Add a focused `TenantRemoveConfigurationCommandSnapshot` that keeps `Previewed`, `RequestSent`, `Accepted`, `ProjectionPending`, `Confirmed`, `Rejected`, `Failed`, `Degraded`, `UnableToVerify`, duplicate prevention, and audit states distinct.
  - [x] Add `RemoveTenantConfigurationAsync` to `ITenantCommandGateway`, `TenantCommandGateway`, `UnavailableTenantCommandGateway`, and all test stubs that implement the interface.
  - [x] Submit the existing contract record `RemoveTenantConfiguration` through `POST /api/v1/commands` using the shared EventStore gateway: client-generated `messageId`, tenant `"system"`, domain `"tenants"`, aggregate id equal to the literal tenant id, command name `nameof(RemoveTenantConfiguration)`, and JSON payload serialized from the existing command record.
  - [x] Map submission-time `ConfigurationKeyNotFoundRejection`, `InsufficientPermissionsRejection`, `TenantDisabledRejection`, and `TenantNotFoundRejection` to support-safe localized copy. Add command-neutral status mapping for `ConfigurationKeyNotFound` if status lookup can return that rejection type.
  - [x] Do not surface raw problem details, raw key/value payloads, correlation ids, message ids, ETags, cursors, tokens, decoded claims, stack traces, or real PII.

- [x] Implement validation, namespace scope, and consequence preview (AC: 1, 2, 3, 6)
  - [x] Validate tenant id and selected full key before preview. Preserve the literal key and tenant id; never parse tenant ids or keys as GUID/ULID.
  - [x] Derive namespace/prefix evidence from the already-authorized visible projection, matching the Story 3.3 `SetTenantConfigurationFlow` dot-prefix convention. Do not accept free-form hidden keys.
  - [x] Use the Product/UX-approved `FC-CNS` inline structured-text fallback. Include tenant identity, namespace/prefix, exact key, current known state, intended removal effect, freshness/projection evidence, authorization/scope evidence, known consequences, known unknowns, audit/evidence expectation, and recovery path.
  - [x] Block submission when any preview item is unavailable and name the missing item with a visible localized reason.
  - [x] Treat missing target evidence before submit as a blocked/stale-data condition requiring refresh, not as success. If the backend returns `ConfigurationKeyNotFound`, render `Rejected` with safe text and keep unrelated configuration rows unchanged.

- [x] Add command execution and projection-confirmation behavior (AC: 3, 4, 5)
  - [x] After `Accepted`, `EventsStored`, `EventsPublished`, or `Completed`, re-query tenant detail and confirm only when the authoritative projection for the matching literal tenant id no longer contains the submitted full key.
  - [x] Treat SignalR only as a freshness nudge that triggers status/projection re-query; it must never delete the key, mark confirmed, or advance audit state by itself.
  - [x] If the key remains present after terminal command status, show `projection pending` or `unable to verify`, not success.
  - [x] Keep `ConfigurationKeyNotFound` as a rejected lifecycle state unless product explicitly changes the semantics; do not silently relabel backend missing-key rejection as successful removal.
  - [x] Enforce the one-at-a-time command policy across tenant detail command surfaces. While remove-configuration is in flight, set-configuration, metadata, member, and lifecycle actions are unavailable with visible reasons; preserve the Story 3.3 owned in-flight command handling.

- [x] Preserve support-safe display, localization, and accessibility (AC: 1, 3, 5, 6, 8)
  - [x] Add EN/FR `Tenants.Configuration.Remove.*` resources with parity, whole strings, and matching named placeholders. Do not assemble translated sentence fragments at runtime.
  - [x] Keep raw values out of preview copy, live regions, rejection messages, logs, copied text, test names, and lifecycle safe messages unless already allowed by `SupportSafeCopyClassifier`.
  - [x] Use stable selectors such as `tenants-config-remove-open`, `tenants-config-remove-flow`, `tenants-config-remove-preview`, `tenants-config-remove-preview-item`, `tenants-config-remove-submit`, `tenants-config-remove-cancel`, `tenants-config-remove-refresh`, `tenants-config-remove-unavailable-reason`, `tenants-config-remove-lifecycle`, `tenants-config-remove-state`, `tenants-config-remove-audit`, `tenants-config-remove-recovery`, and `tenants-config-remove-live-region`.
  - [x] Implement destructive confirmation keyboard behavior with focus trap/return and Escape/cancel as safe non-committing exits.
  - [x] Use state-driven live-region politeness: polite for previewed/submitted/accepted/projection-pending/confirmed/audit-pending; assertive for rejected/failed/degraded/unable-to-verify/blocked. Do not derive politeness from color or visual intent.

- [x] Add focused tests and update evidence (AC: 1-8)
  - [x] Add gateway tests for `RemoveTenantConfigurationAsync` payload shape, literal tenant id/key preservation, command name, message id, accepted result, unavailable gateway behavior, `ConfigurationKeyNotFound`, shared safe rejection mapping, and support-unsafe failure redaction.
  - [x] Add snapshot/model tests for preview completeness, accepted/projection-pending/confirmed/rejected/failed/degraded/unable-to-verify states, key-removal projection proof, `ConfigurationKeyNotFound` staying rejected, SignalR nudge-only behavior, and no optimistic configuration deletion.
  - [x] Add component tests for row launch, preview content, namespace/scope fail-closed behavior, missing target handling, disabled/stale/unknown/degraded gating, incomplete preview blocking, destructive confirmation keyboard behavior, cancel/Escape focus return, forced-colors/focus CSS hooks, live-region politeness, stable selectors, and no sensitive-value exposure.
  - [x] Extend `TenantDetailSurfaceTests` to prove the configuration read/filter/group/redaction behavior and Story 3.3 set flow remain intact after adding remove.
  - [x] Add or update resource parity tests for all new `Tenants.Configuration.Remove.*` keys.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` and `tests/test-summary.md` if the repository continues the current story evidence practice.
  - [x] Run `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`; if `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility, use the xUnit v3 executable fallback documented in prior stories.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 3 Story 3.4. Epic 3 covers tenant lifecycle and configuration control while preserving high-impact safety rules and projection truth. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.4: Remove Tenant Configuration Key with Consequence Preview`]
- FR17 requires an authorized user to remove a configuration key; missing key is a safe `ConfigurationKeyNotFound` rejection, and removal success is shown only after projection confirmation. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-17: Remove a configuration key`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`]
- Tenant-scoped destructive/configuration flows are fallback-eligible. The Product/UX-approved `FC-CNS` inline consequence fallback applies to FR17 and the `FC-CNC` one-at-a-time policy applies to all command FRs. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#The three approved fallbacks`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-06.md#Recommended Approach`]
- Story 3.4 follows Story 3.3's conservative configuration command posture: use the inline preview for this configuration remove flow and block if preview inputs are incomplete. [Source: `_bmad-output/implementation-artifacts/3-3-set-tenant-configuration-key-value-with-consequence-preview.md`]

### Backend And Domain Contract Facts

- `RemoveTenantConfiguration` already exists as `public record RemoveTenantConfiguration(string TenantId, string Key);`. Do not add XML docs, `sealed`, marker interfaces, or new command fields. [Source: `src/Hexalith.Tenants.Contracts/Commands/RemoveTenantConfiguration.cs`; `_bmad-output/project-context.md#C# Language & Contract Rules`]
- `TenantConfigurationRemoved` already exists as `public record TenantConfigurationRemoved(string TenantId, string Key) : IEventPayload;`, and `ConfigurationKeyNotFoundRejection` already exists as `public record ConfigurationKeyNotFoundRejection(string TenantId, string Key) : IRejectionEvent;`. [Source: `src/Hexalith.Tenants.Contracts/Events/TenantConfigurationRemoved.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/ConfigurationKeyNotFoundRejection.cs`]
- `TenantAggregate.Handle(RemoveTenantConfiguration, ...)` uses the envelope aggregate id as truth for tenant id, rejects missing tenants, rejects disabled tenants with `TenantDisabledRejection`, requires tenant owner or global administrator authority, rejects missing keys with `ConfigurationKeyNotFoundRejection`, and emits `TenantConfigurationRemoved` for existing keys. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#Handle(RemoveTenantConfiguration)`]
- Server tests already cover remove success, envelope aggregate id precedence, `TenantNotFound`, `TenantDisabled`, reader/contributor/non-member `InsufficientPermissions`, missing key `ConfigurationKeyNotFound`, exact key preservation, and global-admin bypass. Story 3.4 should consume those contracts, not rework backend behavior. [Source: `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs#RemoveTenantConfiguration_existing_key_produces_TenantConfigurationRemoved`]
- `TenantDetail.Configuration` is the authoritative projection evidence the UI can re-query for removal confirmation. Confirm only when the matching literal tenant id projection no longer contains the submitted full key. [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantDetail.cs`; `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`]
- Tenant configuration is namespaced by a consumer-owned dot-prefix. Reads are filtered by authorized prefix; the remove flow must not reveal unauthorized key existence. [Source: `_bmad-output/project-context.md#DAPR, Eventing & Consumer Rules`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-6: View tenant configuration (read-only)`]

### Existing Implementation To Extend

- `TenantConfigurationView.razor` currently renders authorized read projection, groups keys by namespace, filters visible keys, redacts unsafe values, uses `TruthStateBadge`, exposes `tenants-config-*` selectors, and composes `SetTenantConfigurationFlow`. Preserve this behavior and add remove as a focused sibling flow. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`]
- `SetTenantConfigurationFlow.razor` is the closest configuration-specific pattern for namespace/prefix evidence, complete inline preview, support-safe current value display, command-state rendering, focus behavior, live-region politeness, owned in-flight command handling, and projection re-query. Reuse the discipline, but adapt confirmation to key absence rather than key/value match. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor`; `_bmad-output/implementation-artifacts/3-3-set-tenant-configuration-key-value-with-consequence-preview.md#Senior Developer Review (AI)`]
- `TenantDetailPage.razor` composes configuration, metadata, member, and lifecycle command surfaces and owns `RefreshTenantDetailAsync` plus page-level command locking. Add remove-configuration projection evidence there rather than creating another refresh path. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`]
- `ITenantCommandGateway`, `TenantCommandGateway`, and `UnavailableTenantCommandGateway` are the only current UI command egress. Extend them; do not introduce a second command bus or browser-side backend client. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`]
- `TenantCreateCommandModels.cs` already contains shared command lifecycle, audit, focus, live-region, request, result, and configuration-set snapshot patterns. Reuse these vocabulary and state transitions for configuration remove rather than adding unrelated enums. [Source: `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`]
- `RemoveTenantMemberFlow` remains the destructive-flow precedent for preview completeness, destructive confirmation, projection-confirmed absence, honest audit handoff, and support-safe copy. Adapt this to configuration key removal while avoiding membership-specific owner/global-admin content. [Source: `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md`]

### Architecture And UX Guardrails

- Backend egress remains server-side BFF/DAPR only: `POST /api/v1/commands`, `GET /api/v1/commands/status/{correlationId}`, and query re-read. No browser backend calls and no new backend endpoints. [Source: `_bmad-output/planning-artifacts/architecture.md#API boundary`; `_bmad-output/planning-artifacts/architecture.md#Data flow (command)`]
- Command confirmation has one pattern: dispatch, status poll plus SignalR nudge, authoritative projection re-query, then `confirmed` only from projection truth. SignalR never advances lifecycle or audit. [Source: `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`]
- Unknown freshness, stale freshness, indeterminate authorization, disabled tenant state, incomplete consequence preview, or missing command lifecycle support fail closed with visible reasons. `aging` and `refreshing` may remain usable with friction only if preview and authorization facts remain complete. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#6. Core Interaction Contract`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`]
- Consequence Preview fallback is structured inline text carrying complete required content and blocking submission if incomplete. For configuration remove, adapt the 10-item set to tenant, namespace/prefix, key, current known state, intended removal effect, recovery path, audit expectation, freshness, known consequences, and known unknowns. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#2. FC-CNS - inline consequence text`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#H. Consequence Preview content set`]
- Audit proof is not part of this story. Use honest handoff states (`audit pending`, `audit delayed`, `audit unavailable`, `missing implementation support`) and do not render `audit available` or an Audit Evidence Receipt until Epic 5 evidence sources exist. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`; `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md#Senior Developer Review (AI)`]
- Support safety is mandatory: no raw payloads, tokens, correlation ids, raw metadata, stack traces, decoded JWTs, cursors, ETags, unsafe values, or real PII in rendered output, logs, docs, test summaries, or safe messages. [Source: `_bmad-output/project-context.md#Critical Don't-Miss Rules`; `_bmad-output/planning-artifacts/architecture.md#Cross-Cutting Concerns Identified`]

### Expected File Touch Points

- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor` and `.css`: add row launch/composition for remove while preserving read/set behavior.
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor` and `.css`: preferred home for the focused remove flow.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`: pass remove projection evidence and existing command lock callbacks.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`: add request/snapshot support for remove configuration.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`, `TenantCommandGateway.cs`, `UnavailableTenantCommandGateway.cs`: add remove-configuration command submission and safe rejection mapping.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`: add `Tenants.Configuration.Remove.*` resources with EN/FR parity.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`: add gateway payload/rejection/redaction tests.
- `tests/Hexalith.Tenants.UI.Tests/State/*`: add remove-configuration snapshot tests, or extend existing command snapshot tests if repository structure prefers it.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` and a new focused remove-configuration flow test file: cover composition, read/set preservation, preview/destructive/accessibility behavior, resource parity, and support-safety.

### Scope Boundaries And Anti-Patterns

- Do not modify `src/Hexalith.Tenants.Contracts`, `src/Hexalith.Tenants.Server`, validators, aggregate behavior, AppHost/Aspire plumbing, package metadata, shared FrontComposer components, or submodules unless a compile error proves the existing contract cannot be consumed.
- Do not build a generic `<ConsequencePreview>`, command framework, authorization service, redactor, shell component, or audit receipt inside Tenants. Shared infrastructure belongs in FrontComposer/EventStore/Commons, not this domain repository.
- Do not optimistically delete the visible configuration row, tenant detail summary count, copied values, or freshness state before projection evidence proves the key is absent.
- Do not reveal unauthorized key existence. The remove flow starts from visible authorized rows; a hidden or out-of-scope key must remain indistinguishable from unavailable scope evidence.
- Do not treat `ConfigurationKeyNotFound` as Success. It is a domain rejection with safe localized text; show rejected or a stale-data refresh path without removing unrelated visible state.
- Do not hide unavailable reasons in tooltips. Reasons must be inline-visible, programmatically associated, and keyboard/screen-reader reachable.
- Do not collapse `accepted`, `projection pending`, `confirmed`, `audit pending`, and `audit available`; do not use success styling or copy for `accepted`, `degraded`, `rejected`, or `unable to verify`.

### Previous Story Intelligence

- Story 3.3 implemented the adjacent set-configuration command flow and fixed degraded projection-state reason honesty, owned in-flight command lock messaging, and validation/ARIA accessibility issues. Story 3.4 must preserve those fixes while adding remove. [Source: `_bmad-output/implementation-artifacts/3-3-set-tenant-configuration-key-value-with-consequence-preview.md#Senior Developer Review (AI)`]
- Story 3.2 review fixed command lock retention through projection confirmation and focus-loop trapping for the high-impact confirmation surface. Remove-configuration should not release the page command lock while accepted/projection-pending work still lacks authoritative projection confirmation. [Source: `_bmad-output/implementation-artifacts/3-2-disable-or-enable-tenant-with-high-impact-confirmation.md#Senior Developer Review (AI)`]
- Story 3.1 established lifecycle blocked-state composition and fixed degraded/unavailable/unknown reason honesty plus live-region ARIA consistency. Preserve those fixes on the shared tenant detail surface. [Source: `_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md#Senior Developer Review (AI)`]
- Story 2.4 established the destructive preview-completeness rule, SignalR-nudge-only behavior, projection-confirmed absence, honest audit handoff, exact launching-control focus return, and support-safe destructive-flow copy. Apply the same rules to configuration removal. [Source: `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md#Senior Developer Review (AI)`]
- Story 1.6 established read-only configuration grouping, filtering, freshness, and support-safe redaction; Story 1.8 established support-safe copy boundaries. Do not regress those behaviors while adding a destructive row action. [Source: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md`; `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`]

### Git Intelligence

- Recent commits are story-scoped Conventional Commits: `feat(story-3.3): Set Tenant Configuration Key Value with Consequence Preview`, `feat(story-3.2): Disable or Enable Tenant with High-Impact Confirmation`, and `feat(story-3.3): add tenant configuration command foundations`. A compatible implementation commit would be `feat(story-3.4): Remove Tenant Configuration Key with Consequence Preview`. [Source: `git log --oneline -5`]
- The latest relevant UI command-flow commit changed the Story 3.3 file, sprint status, test summaries, `TenantDetailPage`, `SetTenantConfigurationFlow`, `TenantConfigurationView`, resources, gateway tests, component tests, and state tests. Story 3.4 should follow that shape and avoid unrelated backend/submodule churn. [Source: `git show --stat --name-only 7443050`]
- The worktree currently has an unrelated dirty `_bmad-output/story-automator/orchestration-1-20260605-153745.md` file. Do not modify or revert it as part of Story 3.4. [Source: `git status --short`]

### Latest Technical Information

- Use the repo-pinned local stack and contracts: .NET 10, Blazor InteractiveServer, Fluent UI Blazor v5 RC pinned by the UI project/FrontComposer posture, EventStore command gateway contracts, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not introduce new packages or package versions for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- No external API/package research is required beyond pinned local contracts. The material risks are unauthorized key-existence leakage, optimistic deletion, `ConfigurationKeyNotFound` being mislabeled as success, partial preview content, command-state collapse, command-specific rejection copy leaking through shared status, support-unsafe value/key exposure, and regressions to Story 3.3 set/read configuration behavior.

### Project Structure Notes

- Source changes should stay under `src/Hexalith.Tenants.UI/`, primarily `Components/Tenants/Configuration/`, `Components/Tenants/TenantConfigurationView.*`, `Components/Pages/`, `State/TenantCommands/`, `Services/Gateways/`, and `Resources/`.
- Tests should stay under `tests/Hexalith.Tenants.UI.Tests/` using xUnit v3, Shouldly, bUnit, and NSubstitute. Test files remain plural `{Class}Tests.cs`.
- Domain contracts, server aggregate behavior, validators, and server tests already exist for this command and should be consumed, not reworked.
- Detected planning risk to manage: FR17 is terse in the PRD. The implementation-ready interpretation comes from Story 3.4 epics, the addendum rejection matrix, the approved fallback record, and Story 3.3/2.4 command-flow precedent.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 3.4: Remove Tenant Configuration Key with Consequence Preview`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Lifecycle and Configuration Control`
- PRD/addendum: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-17: Remove a configuration key`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#H. Consequence Preview content set`
- Readiness and fallbacks: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-06.md`; `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05-v2.md`
- Architecture and UX: `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md`
- Current UI implementation: `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`; `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`; `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`; `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`; `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- Domain contracts/tests: `src/Hexalith.Tenants.Contracts/Commands/RemoveTenantConfiguration.cs`; `src/Hexalith.Tenants.Contracts/Events/TenantConfigurationRemoved.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/ConfigurationKeyNotFoundRejection.cs`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`; `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs`
- Prior story evidence: `_bmad-output/implementation-artifacts/3-3-set-tenant-configuration-key-value-with-consequence-preview.md`; `_bmad-output/implementation-artifacts/3-2-disable-or-enable-tenant-with-high-impact-confirmation.md`; `_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md`; `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md`; `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md`; `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, `architecture.md`, targeted PRD/addendum/UX/readiness/fallback sections, Story 3.3 previous-story intelligence, current configuration/detail/gateway/resource/test files, configuration remove domain contracts/aggregate behavior/tests, and recent git history.
- Checklist validation was run against `.agents/skills/bmad-create-story/checklist.md`; the main corrections from validation are reflected in the explicit no-optimistic-deletion rule, `ConfigurationKeyNotFound` rejected-state rule, authorized-visible-row launch rule, projection-only confirmation rule, and support-safety constraints.
- Dev-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Red/green/refactor implementation followed the existing Story 3.3 set-configuration and Story 2.4 destructive-flow patterns for command dispatch, lifecycle state, projection confirmation, support-safe copy, and one-at-a-time command activity.
- Validation: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Validation: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility; xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed 444/444.
- Regression validation: `dotnet build Hexalith.Tenants.slnx -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. Tier 1 executable tests passed: Contracts 103/103, Client 47/47, Testing 181/181, Sample 31/31.
- Broader validation note: Server.Tests executable was attempted and failed in pre-existing documentation/AppHost checks for missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` plus stale deployment-readiness evidence unrelated to Story 3.4.
- QA generation validation: Added remove-configuration launcher focus-return regression coverage and mirrored the existing keyed row-action focus restoration pattern in `TenantConfigurationView`. `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility; xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed 445/445.
- Senior Developer Review (AI): auto-fixed missing destructive exact-key confirmation, added focus-loop sentinels/modal semantics, retained page command activity while remove remains accepted/projection-pending, and added regression coverage for confirmation blocking plus projection-pending lock retention. `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors. xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed 446/446.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 3.4 to the UI/BFF configuration remove command flow over existing backend contracts.
- Story context marks Story 3.4 ready-for-dev because tenant-scoped configuration commands are fallback-eligible and the required `FC-CNS`/`FC-CMD`/`FC-CNC` gates are approved/confirmed by prior planning and implemented command-flow precedent.
- Story context identifies the key implementation risks: unauthorized key-existence leakage, optimistic deletion, missing-key rejection being mislabeled as success, partial consequence preview, command-state collapse, command-specific rejection copy leaking through shared status, support-unsafe key/value exposure, and regressions to existing read/set configuration behavior.
- Implemented a focused `RemoveTenantConfigurationFlow` composed from visible configuration rows, with complete inline consequence preview, narrow-layout blocking, cancel/Escape no-commit behavior, support-safe current-state display, stable selectors, state-driven live regions, and projection-confirmed absence before confirmed removal.
- Extended the existing tenant command gateway and command state model with `RemoveTenantConfigurationCommandRequest`, `TenantRemoveConfigurationCommandSnapshot`, `RemoveTenantConfigurationAsync`, unavailable gateway support, safe submission/status rejection mapping, and `ConfigurationKeyNotFound` rejected-state handling.
- Preserved the read-only configuration table, namespace grouping/filtering, copy/redaction behavior, Story 3.3 set-configuration flow, and page-level command lock/projection-refresh pattern.
- Added EN/FR `Tenants.Configuration.Remove.*` resources and resource parity coverage.
- Added focused gateway, state, component, and composition tests; updated both test summary files with Story 3.4 evidence.
- Added QA regression coverage for exact remove-action launcher focus return after cancel and updated Story 3.4 test evidence to 445 passing UI tests.
- Senior review fixed destructive confirmation and command-lock retention gaps; Story 3.4 now has exact-key typed confirmation, focus-loop sentinels, and projection-pending command activity coverage.

### File List

- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/implementation-artifacts/3-4-remove-tenant-configuration-key-with-consequence-preview.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/RemoveTenantConfigurationFlow.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor.css`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/RemoveTenantConfigurationFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantRemoveConfigurationCommandSnapshotTests.cs`
- `tests/test-summary.md`

### Change Log

- 2026-06-06T12:43:10+02:00 - Created Story 3.4 context and marked it ready for development.
- 2026-06-06T13:30:00+02:00 - Implemented remove-configuration consequence preview flow, gateway/state support, localization, tests, and validation evidence; marked story ready for review.
- 2026-06-06T13:10:25+02:00 - QA generation added focus-return regression coverage for configuration removal and refreshed validation evidence.
- 2026-06-06T13:21:25+02:00 - Senior Developer Review auto-fixed destructive confirmation, focus-loop, and projection-pending command-lock retention gaps; marked story done.

## Senior Developer Review (AI)

### Review Scope

- Validated acceptance criteria and completed task claims against the Story 3.4 File List plus git-discovered changes.
- Loaded project context and architecture guidance; no external package/API research was required because the story uses pinned local .NET/Blazor/EventStore contracts.
- Noted one unrelated dirty file outside the story source review surface: `_bmad-output/story-automator/orchestration-1-20260605-153745.md`.

### Findings Auto-Fixed

- HIGH: `RemoveTenantConfigurationFlow` did not require explicit destructive confirmation before submission, despite AC8 and the completed task claim for destructive confirmation keyboard behavior. Fixed by adding exact full-key confirmation input, validation, modal semantics, focus-loop sentinels, EN/FR resources, and bUnit regression coverage.
- HIGH: Remove command activity was released in `finally` even when the snapshot remained `Accepted` or `ProjectionPending`, allowing other tenant command surfaces before projection truth confirmed removal. Fixed by retaining command activity until projection confirmation or a terminal non-pending state, with component regression coverage.
- MEDIUM: Accessibility coverage claimed focus trapping for the remove flow but only cancel/Escape behavior was tested. Fixed with focus sentinels and stable selector assertions.

### Outcome

- Review outcome: approved after auto-fixes.
- Critical issues remaining: 0.
- Story status set to `done`.

### Validation

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed: 446 total, 0 errors, 0 failed, 0 skipped.
