---
baseline_commit: 7854e41
created: 2026-06-06T08:57:54+02:00
---

# Story 3.3: Set Tenant Configuration Key Value with Consequence Preview

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 3.3. -->

## Story

As an authorized tenant user,
I want to set a namespaced tenant configuration key/value through a safe command flow,
so that tenant configuration can be changed within my scope with proof and without leaking sensitive data.

## Acceptance Criteria

1. Given an authorized user opens the set-configuration flow for a tenant, when the form renders, then it requires namespace, key, and value input within domain limits and authorized prefix scope, and labels, validation, warnings, and unavailable reasons use Tenants-owned localized whole strings.
2. Given validation, freshness, authorization, lifecycle, and preview support are eligible, when the user prepares a configuration change, then the UI renders the approved inline Consequence Preview fallback unless a confirmed shared `FC-CNS` component exists, and the preview blocks submission when required content, freshness, authorization, or scope data is unavailable.
3. Given the submitted key/value is identical to the current projection value, when the command flow resolves, then the UI shows `already applied` as a NoOp state, and it does not show projection-confirmed Success or create optimistic local state.
4. Given the value exceeds domain limits or violates namespace scope, when validation or the domain returns `ConfigurationLimitExceeded` or a scoped rejection, then the UI shows safe localized field or rejection text, and it does not expose the raw value, backend payload, metadata, stack trace, token, or internal correlation id.
5. Given the user submits an eligible configuration change, when command status or SignalR notification arrives, then SignalR is treated only as a freshness nudge and the configuration is shown as changed only after authoritative tenant projection re-query, and accepted, pending, confirmed, rejected, failed, degraded, unable-to-verify, and audit states remain distinct.
6. Given the tenant is disabled, stale, unknown, degraded, or authorization-indeterminate, when set-configuration availability is evaluated, then the action fails closed with visible inline reason, and the last-confirmed configuration remains visible without being overwritten by in-flight intent.
7. Given this story is complete, when verification is run, then unit/component tests cover namespace authorization, validation limits, preview blocking, identical-value NoOp, `ConfigurationLimitExceeded`, disabled/stale/unknown gating, projection-confirmed update, and audit handoff.
8. Given accessibility verification is run, then Playwright or component tests verify keyboard form operation, focus return, live-region politeness, forced-colors status rendering, stable selectors, and no sensitive-value exposure.

## Tasks / Subtasks

- [x] Extend the existing configuration surface instead of replacing it (AC: 1, 2, 5, 6, 8)
  - [x] Start from `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`; preserve grouping by namespace, filtering, table semantics, read-only empty/filtered-empty states, support-safe copy controls, and current sensitive-value redaction.
  - [x] Add a focused set-configuration flow under `Components/Tenants/Configuration/` or as a focused child of `TenantConfigurationView`; do not turn the existing read view into a monolithic command component.
  - [x] Compose the set flow from `TenantDetailPage`/`TenantConfigurationView` using existing detail refresh and projection evidence patterns; do not add backend endpoints, controllers, browser-side backend clients, or generic FrontComposer substitutes.
  - [x] Keep last-confirmed `TenantDetail.Configuration` visible while the form is open and while a command is in flight; submitted namespace/key/value are intent, not projection truth.
  - [x] Keep configuration commands unavailable on mobile/narrow layouts that cannot preserve preview, freshness, tenant identity, and last-confirmed configuration context.

- [x] Add set-configuration request, snapshot, and gateway support using existing command infrastructure (AC: 3, 4, 5)
  - [x] Add `SetTenantConfigurationCommandRequest` with literal `TenantId`, full configuration `Key`, and `Value` to `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs` unless a clearer existing command-model split exists.
  - [x] Add a focused `TenantSetConfigurationCommandSnapshot` that represents `Previewed`, `RequestSent`, `Accepted`, `ProjectionPending`, `Confirmed`, `AlreadyApplied`, `Rejected`, `Failed`, `Degraded`, `UnableToVerify`, and audit states distinctly.
  - [x] Add `SetTenantConfigurationAsync` to `ITenantCommandGateway`, `TenantCommandGateway`, and `UnavailableTenantCommandGateway`.
  - [x] Submit the existing contract record `SetTenantConfiguration` through the shared command endpoint with a client-generated `messageId`, tenant `"system"`, domain `"tenants"`, aggregate id equal to the literal tenant id, command name `nameof(SetTenantConfiguration)`, and JSON payload serialized from the existing command record.
  - [x] Preserve command-neutral `GetStatusAsync` shared rejection mapping; add configuration-specific submission-time safe mapping only where command context is known.
  - [x] Map `ConfigurationLimitExceededRejection`, `InsufficientPermissionsRejection`, `TenantDisabledRejection`, and `TenantNotFoundRejection` to support-safe localized copy. Do not surface raw problem details, correlation ids, payloads, tokens, ETags, cursors, stack traces, decoded claims, or raw values.

- [x] Implement validation, namespace scope, and consequence preview (AC: 1, 2, 4, 6)
  - [x] Validate tenant id, namespace/prefix, key, and value before preview. Use domain limits: max key length 256 and max value length 1024 from `TenantAggregate.MaxKeyLength`/`MaxValueLength`; do not duplicate unrelated backend validation rules.
  - [x] Build the submitted full key from namespace/prefix plus key using the repository's existing dot-prefix convention. Preserve the literal key and tenant id; never parse or generate tenant ids as GUID/ULID.
  - [x] Require authorized namespace/prefix evidence from server/BFF-reflected facts or already-authorized projection scope. If prefix ownership cannot be proven, fail closed with `missing permission` or a specific localized scope reason; do not reveal out-of-scope key existence.
  - [x] Use the Product/UX-approved `FC-CNS` inline structured-text fallback. Include tenant identity, namespace/prefix, key, current known state, intended effect, freshness/projection evidence, authorization/scope evidence, known consequences, known unknowns, audit/evidence expectation, and recovery path.
  - [x] Treat the PRD's unresolved preview-scope question conservatively: require the preview for every configuration set command in this story unless a newer Product/UX record narrows the scope.
  - [x] Block submission when any preview input is unavailable and name the missing item with a visible localized reason.

- [x] Add command execution and projection-confirmation behavior (AC: 3, 5, 6)
  - [x] Detect identical key/value against the last-confirmed projection before submission and show `already applied` without dispatching a command.
  - [x] Also treat a completed command with `EventCount == 0` as `already applied` only after the last-confirmed projection still proves the submitted key/value; do not show projection-confirmed Success for a NoOp.
  - [x] After `Accepted`, `EventsStored`, `EventsPublished`, or `Completed`, re-query tenant detail and confirm only when the authoritative projection for the matching literal tenant id contains the submitted full key with the submitted value.
  - [x] Treat SignalR as a freshness nudge that triggers status/projection re-query only; it must never set the configuration value, command state, or audit state by itself.
  - [x] If projection evidence is missing after terminal command status, show `projection pending` or `unable to verify`, not success.
  - [x] Enforce one-at-a-time command policy across tenant detail command surfaces. While configuration set is in flight, metadata/member/configuration actions are unavailable with visible reasons; preserve the current one-direction metadata lock and broaden only as needed for this story.

- [x] Preserve support-safe display and localization (AC: 1, 4, 6, 8)
  - [x] Add EN/FR `Tenants.Configuration.Set.*` resources with parity, using whole strings and matching placeholders. Do not assemble translated sentence fragments at runtime.
  - [x] Keep raw values out of live regions, rejection messages, logs, copied text, test assertion names, and command lifecycle safe messages unless they have passed the existing `SupportSafeCopyClassifier` rules.
  - [x] For values classified as sensitive or unsafe, render a safe placeholder such as the existing unavailable value copy. The preview may name that a value will change without echoing the raw value.
  - [x] Use stable selectors such as `tenants-config-set-open`, `tenants-config-set-flow`, `tenants-config-set-namespace`, `tenants-config-set-key`, `tenants-config-set-value`, `tenants-config-set-preview`, `tenants-config-set-preview-item`, `tenants-config-set-submit`, `tenants-config-set-cancel`, `tenants-config-set-refresh`, `tenants-config-set-unavailable-reason`, `tenants-config-set-lifecycle`, `tenants-config-set-state`, `tenants-config-set-audit`, `tenants-config-set-recovery`, and `tenants-config-set-live-region`.
  - [x] Use state-driven live-region politeness: polite for previewed/submitted/accepted/projection-pending/confirmed/audit-pending/already-applied; assertive for rejected/failed/degraded/unable-to-verify/blocked. Do not derive politeness from badge color or visual intent.

- [x] Add focused tests and update evidence (AC: 1-8)
  - [x] Add gateway tests for `SetTenantConfigurationAsync` payload shape, literal tenant id preservation, command name, message id, accepted result, unavailable gateway behavior, `ConfigurationLimitExceeded`, shared safe rejection mapping, and support-unsafe failure redaction.
  - [x] Add snapshot/model tests for preview completeness, pre-submit identical key/value `AlreadyApplied`, completed `EventCount == 0` reconciliation, accepted/projection-pending/confirmed/rejected/failed/degraded/unable-to-verify states, audit handoff, SignalR nudge-only behavior, and no optimistic configuration overwrite.
  - [x] Add component tests for form rendering, namespace/key/value validation, prefix/scope fail-closed behavior, disabled/stale/unknown/degraded gating, incomplete preview blocking, keyboard cancel/Escape focus return, forced-colors/focus CSS hooks, live-region politeness, stable selectors, and no sensitive-value exposure.
  - [x] Extend `TenantDetailSurfaceTests` to prove existing configuration read/filter/group/redaction behavior remains intact and the set flow does not regress metadata/member/lifecycle composition.
  - [x] Add or update resource parity tests for all new `Tenants.Configuration.Set.*` keys.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` and `tests/test-summary.md` if the repository continues the current story evidence practice.
  - [x] Run `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`; when `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility, use the xUnit v3 executable fallback documented in prior stories.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 3 Story 3.3. Epic 3 covers tenant lifecycle and configuration control while preserving high-impact safety rules and projection truth. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.3: Set Tenant Configuration Key Value with Consequence Preview`]
- FR16 requires an authorized user to set a namespaced configuration key/value, with identical key/value as NoOp `already applied`, over-limit values as `ConfigurationLimitExceeded`, and projection-confirmed success only. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-16: Set a configuration value`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`]
- Tenant-scoped destructive/configuration flows are fallback-eligible. The Product/UX-approved `FC-CNS` inline consequence fallback applies to FR16/FR17; FR15 lifecycle disable/enable remains categorically blocked and should not be used as a reason to block Story 3.3. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#The three approved fallbacks`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-03.md#Section 2 - Impact Analysis`; `_bmad-output/implementation-artifacts/3-2-disable-or-enable-tenant-with-high-impact-confirmation.md`]
- The PRD leaves open whether every configuration edit needs a preview or only high-risk keys. Until a newer decision narrows scope, implement the safer interpretation: every set-configuration command gets the inline preview. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#16. Assumptions & Open Questions`]

### Backend And Domain Contract Facts

- `SetTenantConfiguration` already exists as `public record SetTenantConfiguration(string TenantId, string Key, string Value);`. Do not add XML docs, `sealed`, marker interfaces, or new command fields. [Source: `src/Hexalith.Tenants.Contracts/Commands/SetTenantConfiguration.cs`; `_bmad-output/project-context.md#C# Language & Contract Rules`]
- `TenantAggregate.Handle(SetTenantConfiguration, ...)` uses the envelope aggregate id as truth for tenant id, rejects missing tenants, rejects non-active tenants with `TenantDisabledRejection`, requires tenant owner or global administrator authority, rejects key/value/key-count limits with `ConfigurationLimitExceededRejection`, returns `DomainResult.NoOp()` for identical existing key/value, and emits `TenantConfigurationSet` otherwise. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#Handle(SetTenantConfiguration)`]
- Limits are already covered by backend validation and aggregate tests: key max 256, value max 1024, max 100 distinct configuration keys; overwriting an existing key at 100 keys is allowed; same-value retry is NoOp. [Source: `src/Hexalith.Tenants.Server/Validators/SetTenantConfigurationValidator.cs`; `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`; `tests/Hexalith.Tenants.Server.Tests/Validators/SetTenantConfigurationValidatorTests.cs`]
- `TenantDetail.Configuration` is the authoritative projection evidence this UI can re-query for confirmation. Confirmation must compare the matching literal tenant id, submitted full key, and submitted value. [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantDetail.cs`; `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- Tenant configuration is namespaced by a consumer-owned dot-prefix; consuming services filter by their prefix and ignore other namespaces. UI scope evidence must not reveal unauthorized key existence. [Source: `_bmad-output/project-context.md#DAPR, Eventing & Consumer Rules`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-6: View tenant configuration (read-only)`]

### Existing Implementation To Extend

- `TenantConfigurationView.razor` currently renders the authorized read projection, groups keys by namespace, filters visible keys, redacts unsafe values, uses `TruthStateBadge`, and exposes `tenants-config-*` selectors. Preserve this behavior and extend it with a focused command child. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`; `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`]
- `TenantDetailPage.razor` currently composes metadata edit, lifecycle availability, member review, and read-only configuration. It has `RefreshTenantDetailAsync` and projection evidence providers for existing commands; add configuration projection evidence there rather than creating another refresh path. [Source: `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`]
- `ITenantCommandGateway`, `TenantCommandGateway`, and `UnavailableTenantCommandGateway` are the only current UI command egress. Extend them; do not introduce a second command bus. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`]
- `TenantCreateCommandModels.cs` already contains shared command lifecycle, audit, focus, live-region, request, result, and snapshot patterns. Reuse these vocabulary and state transitions for configuration rather than adding unrelated enums. [Source: `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`]
- `EditTenantMetadataFlow.razor` is the closest non-destructive form pattern for validation, submit, command status refresh, projection re-query, focus return, live-region politeness, support-safe messages, and page-level command activity callback. Configuration set should reuse the same discipline while adding consequence preview and NoOp handling. [Source: `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor`; `_bmad-output/implementation-artifacts/2-5-edit-tenant-metadata-with-safe-validation.md`]
- `RemoveTenantMemberFlow` is the precedent for inline consequence preview, preview completeness, destructive confirmation, projection-only confirmation, and honest audit handoff. Reuse its safety posture, but adapt preview content to configuration instead of member removal. [Source: `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md`]

### Architecture And UX Guardrails

- Backend egress remains server-side BFF/DAPR only: `POST /api/v1/commands`, `GET /api/v1/commands/status/{correlationId}`, and query re-read. No browser backend calls and no backend access tokens in the browser. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- Command confirmation has one pattern: dispatch, status poll plus SignalR nudge, authoritative projection re-query, then `confirmed` only from projection truth. SignalR never advances lifecycle or audit. [Source: `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`]
- Unknown freshness, stale freshness, indeterminate authorization, incomplete consequence preview, missing command lifecycle support, or disabled tenant state fail closed with visible reasons. `aging` and `refreshing` may remain usable with friction only if preview and authorization facts remain complete. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#6. Core Interaction Contract`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`]
- Consequence Preview fallback is structured inline text carrying complete required content and blocking submission if incomplete. For configuration, adapt the 10-item set to tenant, namespace/prefix, key, current known state, intended effect, recovery path, audit expectation, freshness, known consequences, and known unknowns. [Source: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md#2. FC-CNS - inline consequence text`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#H. Consequence Preview content set`]
- Audit proof is not part of this story. Use honest handoff states (`audit pending`, `audit delayed`, `audit unavailable`, `missing implementation support`) and do not render `audit available` or an Audit Evidence Receipt until Epic 5 evidence sources exist. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`; `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md#Senior Developer Review (AI)`]

### Existing Files To Update And Preserve

- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`: compose or host set-configuration flow; preserve current read grouping/filter/redaction/table semantics and `tenants-config-*` selectors.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor.css`: add focused form/preview/lifecycle styling while preserving forced-colors/read-table behavior.
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/*`: preferred home for a new focused `SetTenantConfigurationFlow.razor` and CSS if the component is split from the read view.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`: pass command activity, refresh callback, and set-configuration projection evidence; preserve existing identity/status/freshness/metadata/lifecycle/member/configuration behavior.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`: add request/snapshot/state helpers if the shared-file pattern remains current.
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`, `TenantCommandGateway.cs`, `UnavailableTenantCommandGateway.cs`: add set-configuration command submission and safe rejection mapping.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`: add `Tenants.Configuration.Set.*` resources with EN/FR parity.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`: add gateway payload/rejection/redaction tests.
- `tests/Hexalith.Tenants.UI.Tests/State/*`: add set-configuration snapshot tests, or extend existing command snapshot tests if repository structure prefers it.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` and a new focused configuration set flow test file: cover composition, configuration read preservation, preview/form/accessibility behavior, resource parity, and support-safety.

### Scope Boundaries And Anti-Patterns

- Do not modify `src/Hexalith.Tenants.Contracts`, `src/Hexalith.Tenants.Server`, validators, aggregate behavior, AppHost/Aspire plumbing, package metadata, shared FrontComposer components, or submodules unless a compile error proves the existing contract cannot be consumed.
- Do not build a generic `<ConsequencePreview>`, command framework, authorization service, redactor, shell component, or audit receipt inside Tenants. Shared infrastructure belongs in FrontComposer/EventStore/Commons, not this domain repository.
- Do not infer trusted authorization or namespace ownership from client-only claim parsing. UI reflects server/BFF/projection facts and the backend/domain enforce.
- Do not optimistically change the visible configuration table, tenant detail summary, copied values, or freshness state before projection evidence proves the update.
- Do not show `already applied` for an unverified failed command. Same-value can be pre-submit NoOp only when the last-confirmed projection already contains the exact key/value; post-submit NoOp must be reconciled with projection evidence.
- Do not reveal unauthorized key existence, sensitive values, raw submitted values, raw backend failure text, command payloads, correlation ids, JWTs, ETags, cursors, stack traces, decoded claims, or real PII.
- Do not hide unavailable reasons in tooltips. Reasons must be inline-visible, programmatically associated, and keyboard/screen-reader reachable.
- Do not collapse `accepted`, `projection pending`, `confirmed`, `audit pending`, and `audit available`; do not use success styling or copy for `accepted`, `already applied`, `degraded`, or `unable to verify`.

### Previous Story Intelligence

- Story 3.2 was superseded by the approved 2026-06-06 FR15 correction: lifecycle disable/enable is now reversible soft-delete / availability control, not hard destructive tenant deletion, and may proceed under approved fallbacks. This does not change Story 3.3's configuration-specific safeguards or imply hard-delete UI scope. [Source: `_bmad-output/implementation-artifacts/3-2-disable-or-enable-tenant-with-high-impact-confirmation.md`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-06.md`]
- Story 3.1 established lifecycle blocked-state composition and fixed degraded/unavailable/unknown reason honesty plus live-region ARIA consistency. Preserve these fixes while adding configuration flow near the same tenant detail surface. [Source: `_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md#Senior Developer Review (AI)`]
- Story 2.5 established metadata command activity, last-confirmed projection preservation, gateway extension, support-safe lifecycle messages, resource parity, and the current one-direction page-level command lock. Story 3.3 should generalize or reuse that pattern without breaking metadata/member flows. [Source: `_bmad-output/implementation-artifacts/2-5-edit-tenant-metadata-with-safe-validation.md#Senior Developer Review (AI)`]
- Story 2.4 established the preview-completeness rule, SignalR-nudge-only behavior, projection-confirmed mutation proof, honest audit handoff, and support-safe destructive-flow copy. Apply the same rules to configuration set, especially because configuration values may be sensitive. [Source: `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md#Senior Developer Review (AI)`]
- Story 1.6 established read-only configuration grouping, filtering, freshness, and support-safe redaction; Story 1.8 established support-safe copy boundaries. Do not regress those behaviors while adding mutation. [Source: `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md`; `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`; `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`]

### Git Intelligence

- Recent commits are story-scoped Conventional Commits: `docs(story-3.2): record blocked lifecycle command story`, `feat(story-3.1): Tenant Lifecycle Command Availability and Blocked-State Guardrail`, and `feat(story-2.5): Edit Tenant Metadata with Safe Validation`. A compatible implementation commit would be `feat(story-3.3): Set Tenant Configuration Key Value with Consequence Preview`. [Source: `git log --oneline -5`]
- The latest relevant UI command-flow commit (`feat(story-2.5)`) changed the story file, sprint status, test summaries, `TenantDetailPage`, focused flow component/CSS, resources, command gateways, command models, gateway tests, component tests, composition tests, and UI composition tests. Story 3.3 should follow that shape and avoid unrelated backend/submodule churn. [Source: `git show --stat --name-only f130cef`]
- The worktree had an unrelated dirty `_bmad-output/story-automator/orchestration-1-20260605-153745.md` file during story creation. Do not modify or revert it as part of Story 3.3. [Source: `git status --short`]

### Latest Technical Information

- Use the repo-pinned local stack and contracts: .NET 10, Blazor InteractiveServer, Fluent UI Blazor v5, EventStore command gateway contracts, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not introduce new packages or package versions for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `_bmad-output/planning-artifacts/architecture.md#Technology Stack & Infrastructure`]
- No external API/package research is required beyond pinned local contracts. The material risks are preview-scope ambiguity, namespace-scope leakage, sensitive-value exposure, NoOp vs confirmed-success confusion, optimistic configuration mutation, command-state collapse, command-specific rejection copy leaking through shared status, and regressions to existing configuration read behavior.

### Project Structure Notes

- Source changes should stay under `src/Hexalith.Tenants.UI/`, primarily `Components/Tenants/Configuration/` or `Components/Tenants/TenantConfigurationView.*`, `Components/Pages/`, `State/TenantCommands/`, `Services/Gateways/`, and `Resources/`.
- Tests should stay under `tests/Hexalith.Tenants.UI.Tests/` using xUnit v3, Shouldly, bUnit, and NSubstitute. Test files remain plural `{Class}Tests.cs`.
- Domain contracts, server aggregate behavior, validators, and server tests already exist for this command and should be consumed, not reworked.
- Detected planning risk to manage: FR16 preview scope is unresolved. This story chooses the conservative implementation-ready interpretation and requires preview for every set-configuration command until Product/UX records a narrower rule.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 3.3: Set Tenant Configuration Key Value with Consequence Preview`
- Epic context: `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Lifecycle and Configuration Control`
- PRD/addendum: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-16: Set a configuration value`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#H. Consequence Preview content set`
- Readiness and fallbacks: `_bmad-output/planning-artifacts/fallback-approval-record-2026-06-03.md`; `_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-03.md`; `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05-v2.md`
- Architecture and UX: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md`
- Current UI implementation: `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`; `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`; `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`; `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- Domain contracts/tests: `src/Hexalith.Tenants.Contracts/Commands/SetTenantConfiguration.cs`; `src/Hexalith.Tenants.Contracts/Events/TenantConfigurationSet.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/ConfigurationLimitExceededRejection.cs`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `src/Hexalith.Tenants.Server/Validators/SetTenantConfigurationValidator.cs`; `tests/Hexalith.Tenants.Server.Tests/Aggregates/TenantAggregateTests.cs`; `tests/Hexalith.Tenants.Server.Tests/Validators/SetTenantConfigurationValidatorTests.cs`
- Prior story evidence: `_bmad-output/implementation-artifacts/3-2-disable-or-enable-tenant-with-high-impact-confirmation.md`; `_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md`; `_bmad-output/implementation-artifacts/2-5-edit-tenant-metadata-with-safe-validation.md`; `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md`; `_bmad-output/implementation-artifacts/1-6-read-only-tenant-configuration-view.md`; `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, `architecture.md`, targeted PRD/addendum/UX/readiness/fallback sections, Stories 3.2/3.1/2.5/2.4 previous-story intelligence, current configuration/detail/gateway/resource/test files, configuration domain contracts/aggregate behavior/tests, and recent git history.
- Checklist validation was run against `.agents/skills/bmad-create-story/checklist.md`; the main corrections from validation are reflected in the explicit preview-for-every-set-command rule, projection-only confirmation rule, NoOp vs Success separation, and sensitive-value support-safety constraints.
- Dev-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Existing command request/snapshot/gateway support for set configuration was present in source; implementation focused on the Blazor command flow, tenant-detail composition, resources, and tests.
- Validation: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Validation: `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.
- Validation: xUnit v3 executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed: 411 total, 0 errors, 0 failed, 0 skipped.
- Broader regression signal: Contracts.Tests 103/103, Client.Tests 47/47, and Testing.Tests 181/181 passed via xUnit v3 executable fallback.
- Broader regression signal: `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- Known pre-existing broader failure: Server.Tests executable still fails 6 documentation/AppHost evidence tests for missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` and stale Story 7.6A deployment-readiness summary expectations; no failure touches Story 3.3 UI files.
- Senior Developer Review (AI) loaded the story-automator-review workflow, checklist, story file, sprint status, project context, architecture evidence, git status/diff, story File List, implementation files, resources, and focused tests. Review found and auto-fixed three verified issues: degraded detail was reported as an authorization failure, the configuration flow treated its own in-flight command as generic command-surface unavailability when the parent lock updated, and completed previews left `aria-describedby` pointing at a non-rendered preview-blocked element while validation focus always returned to namespace. Validation passed: `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore`; xUnit v3 fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed 417/417.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 3.3 to the UI/BFF configuration set command flow over existing backend contracts.
- Story context marks Story 3.3 ready-for-dev because tenant-scoped configuration commands are fallback-eligible and the required `FC-CNS`/`FC-CMD`/`FC-CNC` gates are approved/confirmed by prior planning and Story 1.0 evidence.
- Story context identifies the key implementation risks: namespace-scope leakage, sensitive value exposure, partial consequence preview, identical-value NoOp being mislabeled as Success, optimistic configuration mutation, command-state collapse, command-specific rejection copy leaking through shared status, and regressions to existing read-only configuration behavior.
- Implemented a focused `SetTenantConfigurationFlow` under `Components/Tenants/Configuration/`, composed from `TenantConfigurationView` and `TenantDetailPage` using existing command gateway, activity lock, refresh, and projection evidence patterns.
- Preserved the existing configuration read table, grouping/filtering, empty states, safe copy controls, and sensitive-value redaction while adding the mutation flow as a separate child surface.
- Added conservative namespace-scope validation from already-authorized visible projection prefixes, dot-prefix full-key construction, key/value limit checks, complete inline consequence preview, mobile/narrow command blocking hooks, and support-safe current-value display.
- Added command lifecycle behavior for pre-submit identical-value `AlreadyApplied`, zero-event NoOp reconciliation only after projection proof, accepted/projection-pending/confirmed/rejected/failed/degraded/unable-to-verify states, SignalR nudge-only reducer behavior, and audit handoff states.
- Added EN/FR `Tenants.Configuration.Set.*` resources with parity and focused tests/evidence for gateway payloads, safe rejection mapping, snapshot transitions, component behavior, accessibility selectors, resource parity, and read-surface preservation.
- Senior Developer Review (AI) fixed degraded/unavailable/unknown projection-state reason honesty, owned in-flight command reason handling under the page-level command lock, and validation/ARIA accessibility issues in the focused set-configuration flow.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-06T12:36:59+02:00

Outcome: Approved after auto-fixes. Story status moved to `done`; no critical issues remain.

Findings fixed:

- [HIGH] Degraded tenant detail surfaced the authorization unavailable copy. `SetTenantConfigurationFlow` grouped `Degraded`, `Unavailable`, and `Unknown` detail states with authorization failure, which made AC6 fail-closed reasoning inaccurate and could send an operator down the wrong recovery path. Fixed with a distinct localized projection-state unavailable reason in EN/FR resources.
- [MEDIUM] The configuration flow treated its own in-flight submission as generic command-surface unavailability after the parent one-at-a-time lock updated. Fixed by recognizing owned `RequestSent`/`Accepted`/`ProjectionPending` activity so the visible reason remains the in-flight command reason while other command surfaces stay locked.
- [MEDIUM] Accessibility wiring was stale after preview completion and validation focus was too coarse. Completed previews left `aria-describedby` pointing to a non-rendered `tenants-config-set-preview-blocked` element, and every validation failure queued namespace focus even for key/value errors. Fixed by only referencing rendered descriptive elements and queuing field-specific focus for namespace, key, or value validation.

Verification:

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.
- `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests` passed: 417 total, 0 errors, 0 failed, 0 skipped.
- Documentation/reference check: no external API or package research was required; Story 3.3 uses the repo-pinned local .NET 10, Blazor, bUnit, xUnit v3, Fluent UI, and EventStore contracts.

### File List

- `_bmad-output/implementation-artifacts/3-3-set-tenant-configuration-key-value-with-consequence-preview.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Configuration/SetTenantConfigurationFlow.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `tests/Hexalith.Tenants.UI.Tests/Components/SetTenantConfigurationFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantSetConfigurationCommandSnapshotTests.cs`
- `tests/test-summary.md`

### Change Log

- 2026-06-06T08:57:54+02:00 - Created Story 3.3 context and marked it ready for development.
- 2026-06-06T12:13:00+02:00 - Implemented set-configuration UI command flow with consequence preview, projection-confirmed lifecycle handling, support-safe resources, focused tests, and validation evidence.
- 2026-06-06T12:36:59+02:00 - Senior Developer Review (AI) auto-fixed degraded projection-state reason honesty, owned in-flight command lock messaging, and validation/ARIA accessibility issues; validation passed 417/417 UI tests and Story 3.3 moved to done.
